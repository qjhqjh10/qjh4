# D1013 · 诊断块 2（`ShellScene` 15 条 + `CollectionScene` 5 条）

> 诊断时刻：2026-10-13 · 只读诊断代理（块 2）· 判据 = `_tmp_view/{shell,collection}.log` 的**实得值** + 现读源码 + 旁证（`shop.log`）
> 输入日志：`d:/4/_tmp_view/shell.log`（`=== 合计：821 通过 / 15 失败 ===`，:15398）· `d:/4/_tmp_view/collection.log`（`=== 结束：1185/1190 通过，5 条失败 ❌ ===`，:30354）
> 逐条编号 = 日志里**从上到下**的顺序（shell 15 条 = #1–#15，collection 5 条 = #16–#20）。

## 一、总结论

| 档 | 条数 | 是哪几条 |
|---|---|---|
| **(α) 断言/量法/夹具错** | **12** | #1 #2（聊天两态）· #5（RankedTab）· #6（`RewardsWindow` 前提）· #7–#12（榜单庚 6 条）· #16 #17（Collection A773 两条） |
| **(β) 实现缺陷** | **5** | #3 #4（A743 态三/态四 —— 文字裁切框被「裁完又挪」推走）· #13 #14 #15（`PurchasePremiumWindow.Build()` **零调用点**） |
| **(γ) 本批回归** | **3** | #18 #19 #20（Collection 卡牌页三条「前提」——B1/A781 让**视口外的 Label 不再建**） |
| **(δ) 夹具前提不成立** | 0 | #6/#7–#12 的「窗口压根没打开」按 (α) 记 —— 是**夹具漏调用**，不是被测实现的前提被推翻 |

**要改生产代码的实现缺陷只有两处根因**：
1. **`Shell/PurchasePremiumWindow.cs:301`** —— `Open()` 只调 `Initialize()`、**从不调 `Build()`**（`Build()` 全仓零调用点）⇒ 开出来的是一棵空树。**跨宿主**：`ShellScene` 3 条（#13–#15）+ `ShopScene` ~18 条（`shop.log:45928` 起「实得 []」那一族）同一个根因。
2. **`Battle/Label.cs:862/872`（`AlignLeftOn`/`AlignRightOn`）** —— 把标签**平移在最后一次重裁之后**、平移完**不再重裁** ⇒ 文字裁切框被推走（实得位移 **+123.16px**）。

**另有一条「不是这批的错」但要记的**：`LeaderboardWindow.Create` / `RewardsWindow.Create` **不打开窗口**，而 `PurchasePremiumWindow.Create` **会**打开 —— 庚段 / 己②③ 段照着「会打开」那套写了夹具 ⇒ 那 7 条红（#6–#12）**全是夹具少一行 `OpenWindow(...)`**，实现侧一行都不用改。

---

## 二、逐条

### 1. ★★ 态二：同一批内容，边界跟着【节点】挪了 100（`shell.log:12276`）

- 判定：**(α) 断言错（量法错）**
- 证据：
  - 实得 `856.000 ≈ 811.000±0.600` 判红；**856 = 第 9 行条底的天然下沿**：`Shell/ChatPanel.cs:461` `PadT=5 / DefaultRowH=60 / Spacing=10`（`:576,588,594` 现读）⇒ 第 i 行 = `[166+70i, 226+70i]` ⇒ **i=9 时下沿正好 856**。
  - 态一实得 `911.00` = 视口下沿（第 10 行 866..926 被 `ClipNineChildren` 截到 911）⇒ **裁切本身是好的**；态二那一格量到的是「第 9 行的**底边条**已 `SetActive(false)`、却还被算进去」。
  - 机制：`Shell/ChatPanel.cs:684` 行底图走 **`MenuDraw.Nine`**（九宫 62,62,62,62）；`Shell/MenuDraw.cs:1381` `partial` ⇒ `ClipNineChildren`；`Shell/MenuDraw.cs:1403` **整块在框外 ⇒ `q.gameObject.SetActive(false)`**（**不删节点**，注释里明说）。而本条读数器 `Editor/ShellScene.cs:3431 MaxQuadBottomPx` 的循环（`:3435`）是 `GetComponentsInChildren<ImageQuad>(true)` 且**不判 `activeInHierarchy`** ⇒ 把那一块**不画**的子块算成了「画出来的下沿」。
  - 反证（同一文件里的对照）：庚段 `GUnion`（`:4448-4455`）**明确写着**「⚠️ 跳过 `!activeInHierarchy` 的件（画面上没有的东西不算「画出来的并集」）」⇒ 两份同类读数器口径不一致。
- 根因：读数器把**已关掉（不画）**的九宫子块当成可见几何。
- 最小改法：`Editor/ShellScene.cs:3435` 那句 `foreach` 体内加 `if (q == null || !q.gameObject.activeInHierarchy) continue;`（照 `GUnion:4453` 抄）。
- 置信度：**高**（856 = 第 9 行条底，逐位吻合；态一 911 已证明裁切没问题）

### 2. ★ …两条合起来才钉住「那 100px 只可能来自节点」（`shell.log:12290`）

- 判定：**(α) 同 #1**（`b0 − b1 = 911 − 856 = 55.00 < 99`）
- 证据：同 #1 —— 本条的 `b1` 就是 #1 那个被污染的 856。
- 根因：同 #1（级联）。
- 最小改法：不必单独改（#1 修完 `b1 = 811.00` ⇒ `b0−b1 = 100 > 99` 自动转绿）。
- 置信度：**高**

### 3. ★★★ A743 态三：节点那个框真的作用在文字网格上（`shell.log:13690`）

- 判定：**(β) 实现缺陷**（`Label.Align*On` 的平移在重裁之后、平移完不重裁）**＋ (α) 那一半**（本条期望值建立在「态一 = 未裁值」这个错前提上）
- 证据（**三个数的关系是硬证据**）：
  - 夹具（`Editor/ShellScene.cs:3760-3850`）：`MsgList = 99.57,211.98 → 728.28,990.77`（`Shell/InboxWindow.cs:54`）；态三把**节点**右沿切到 `cutX = (aMinX+aMaxX)/2 = (222.73+851.44)/2 = 537.085`，**期望右沿 537.08**；**实得 660.24**。
  - `660.240 − 537.085 = 123.155`，而态一 `851.438 − 728.28 = 123.158` ⇒ **两次位移逐位相同（+123.16）** ⇒ 不是「没裁」，是**裁切框整体平移了 123.16px**。
  - 态四（`:3846`）：显式 `Clip` = `(aMinX−200, …, aMaxX+200)` = `22.73 … 1051.44`，**形参赢**（`ViewportClip.Resolve` 第 1 支）⇒ 期望右沿回到「未切值」`aMaxX = 851.44`，**实得 954.83**。用同一条 +123.16 平移解释：该框在这套坐标系里是 `145.9 … 1174.6`，**954.83 落在框内 ⇒ 未裁** ⇒ 自洽 ⇒ **954.83 才是这段字的真·自然右沿**（态一的 851.44 是**被裁过**的值，拿它当「未切值」是错的）。
  - 位移来源：`Shell/WindowsManager.cs:300-303`（B1，本批）→ `MenuDraw.Text`（A781，`Shell/MenuDraw.cs:1578` 最后一步 `ClipText`）；调用方 `Shell/InboxWindow.cs:547 RowText` 建完**又调** `lb.SetWrapping(false)` 与 `MenuDraw.AlignLeft(lb, r)`；而 `Battle/Label.cs:862 AlignLeftOn` 的实现是 **`RefreshBounds()`（→ 守卫重裁，`:821-822`）→ 之后才改 `transform.localPosition`** ⇒ **最后一刀落在【挪之前】的位置上**，挪完没有任何一次重裁。
  - 反证（同族夹具对照）：`A464·B2/B3` 那一组（`shell.log` 4250–4400，全绿）量到的夹切值**逐位贴在框沿上**（`100.00..500.00 ⊆ 100..500` · 新右沿 `300.00 ≤ 300` · 左沿 `100.00 没动`）—— 那一组**没有「裁完再对齐」这一步** ⇒ 证明**读数器与裁切框是同一套坐标系**（不是量法偏移），位移只可能来自那一句平移。
- 根因：`Label.AlignLeftOn/AlignRightOn` 在最后一次重裁之后平移标签，且**平移后不重裁** ⇒ 文字裁切框被推走 123.16px（**画面上 = 这段字越过视口右沿约 123px 没被切**，左边也少切一截）。
- 最小改法：
  1. 生产：`Battle/Label.cs:868-869`（`AlignLeftOn` 写完 `transform.localPosition` 之后）与 `:878-879`（`AlignRightOn`）各补一次重裁 —— 走现成的口：`var g = GetComponent<ClippedTextGuard>(); if (g != null) g.Reclip();`（与 `RefreshBounds:822` 同一句）。
  2. 断言：修完之后态一会变成 `728.28`（态一本来就压边），态四那条**不能再拿 `aMaxX` 当未切值** —— 改成「态四 = 另测的未裁宽度」或把态一/态三的对照改成**相对差**（`态一右沿 − 态三右沿` 应 = `MsgList.x2 − cutX`）。
- 置信度：**高**（位移在三个态上逐位一致；A464 对照组证明读数坐标系没问题）· 唯一没算术验算的是「位移量 = 对齐平移量」那一步（见 §四·1）。

### 4. ★ A743 态四（实参态对照）（`shell.log:13730`）

- 判定：**(β) 同 #3** ＋ **(α)**：本条把态一的读数当「未切值」用（`aMaxX = 851.44`）—— 那个数**本身就是被裁过的**。
- 证据：同 #3（实测 954.83 = 自然右沿；`1051.44 + 123.16 = 1174.6 > 954.83` ⇒ 未裁，与「形参赢」一致 ⇒ **「形参赢」这件事本身是对的**）。
- 根因：同 #3。
- 最小改法：同 #3（修完实现后本条期望要换成真·未裁值；别继续写 851.44）。
- 置信度：**高**

### 5. ★ RankedTab（`Ranking Tab/AllFactions/scroll rect/viewport`）：`ClipNode` 就是本页那颗 `viewport` 节点（`shell.log:14810`）

- 判定：**(α) 断言/夹具错**（节点名在同页里**不唯一**，按名取件取到了第一个同名的）
- 证据：
  - 报错文案（`Editor/ShellScene.cs:4300-4303` 的 `Vp4`）打印「`FindAbove` 找 = **null**；`ClipNode` = viewport」⇒ `ClipNode`（`Shell/RankedTab.cs:414`）**是对的**，是 `FindAbove(content)` 找不到 ⇒ **取到的 `content` 不在 `viewport` 之下**。
  - `Shell/RankedTab.cs` 里叫 `content` 的节点**有两个**：`:285`（`Top4/content`）与 `:417`（`AllFactions/scroll rect/viewport/content`，**要找的就是它**，`_content = Node(vp, "content", …)`）。
  - 夹具用的是 `FindChildIn(pgRk2.transform, "content")`（`Editor/ShellScene.cs:4361`），而 `FindChildIn`（`:229-235`）是 `GetComponentsInChildren` **取第一个**；`Top4` 先建 ⇒ 命中 `Top4/content` ⇒ 它的父链上没有 `ViewportClip` ⇒ `null`。
  - 同族另外四条（AvatarTab / TitleTab / AchievementsMenu / BattleLogTab）**全绿** ⇒ 只有这一条的取法有歧义。
- 根因：夹具按名取件、而该页有重名节点（**不是**实现缺陷：`ClipNode` 与父链两处都自洽）。
- 最小改法：`Editor/ShellScene.cs:4361` 把内容件的取法限定到 `AllFactions` 那一支，例如 `FindChildIn(FindChildIn(pgRk2.transform, "AllFactions"), "content")`（该子树里 `content` 唯一）。
- 置信度：**高**

### 6. （前提）`RewardsWindow` 的页都建出来了（`shell.log:14975`）

- 判定：**(α) 夹具错**（窗口压根没打开）
- 证据：`Editor/ShellScene.cs:4377` `var rw2 = RewardsWindow.Create(shell.Windows);` —— **紧接着没有 `shell.Windows.OpenWindow(rw2)`**；而 `Shell/RewardsWindow.cs:210 Create`（与 `Shell/LeaderboardWindow.cs:210` 一样）只 `new GameObject` + 挂组件 + `AttachToAnchor`、**不调 `mgr.OpenWindow`** ⇒ `CurrentState == Closed` ⇒ `Open()` 从不跑 ⇒ 树是空的 ⇒ `tabHolder2 = FindChildIn(rw2.transform, "Tabs")` = null ⇒ `ct2/ft2` 全 null。
  - 同文件里的对照：己段 ①/② 都是「`Create` **+ `shell.Windows.OpenWindow(...)`**」（`:4335` 有 · `:4370` 有）—— 只有 `:4377` 这一处漏了。
- 根因：夹具少一行 `OpenWindow`。
- 最小改法：`Editor/ShellScene.cs:4377` 之后插 `shell.Windows.OpenWindow(rw2);`。
- 置信度：**高**

### 7. （前提·不静默）榜单军种条三件都在（`shell.log:15000`）

- 判定：**(α) 夹具错**（同 #6：`LeaderboardWindow` 没被打开）
- 证据：`Editor/ShellScene.cs:4543` `var lbG = LeaderboardWindow.Create(shell.Windows, LeaderboardKind.Skirmish);` —— **没有 `OpenWindow`**；`Shell/LeaderboardWindow.cs:210-224 Create` 只 `AttachToAnchor` 就 `return` ⇒ `Build()` 没跑 ⇒ `armSelG = FindChildIn(lbG.transform, "Army Selector")` = null ⇒ `armContentG` / `armVcG` 全 null。
  - 己段同一个窗口**是绿的**：`:4369-4371` 是 `Create` **+ `shell.Windows.OpenWindow(lb2)`** ⇒ 树建出来了 ⇒ `Vp4` 全过。
  - 旁证：本段兄弟「★★ A769（反向·判别式）」**绿**（`armSelG.Find("Army Content") == null` 在空树上也成立）⇒ 与「树是空的」自洽。
- 根因：夹具少一行 `OpenWindow`。
- 最小改法：`Editor/ShellScene.cs:4543` 之后插 `shell.Windows.OpenWindow(lbG);`。
- 置信度：**高**

### 8. ★★ A769（结构）：`Army Content` 是那颗 `Viewport` 的子件（`shell.log:15026`）

- 判定：**(α) 级联（同 #7）** —— 报错文案自陈「现读父件 = （取不到）」，即 `armContentG == null`
- 证据：同 #7。另：实现侧**是对的** —— `Shell/LeaderboardWindow.cs:488` 已改成 `_armyContent = Node(vpVc.transform, "Army Content", …)`（父 = 那颗 `viewport`）。
- 根因：同 #7。
- 最小改法：同 #7（打开窗口 ⇒ 本条自动有判别力）。
- 置信度：**高**

### 9. ★ 独立判据：从条目那一层（`Army Content`）沿父链找到的节点（`shell.log:15052`）

- 判定：**(α) 级联（同 #7）**（`armContentG`/`armVcG` 都是 null ⇒ 表达式恒 false）
- 证据：同 #7。
- 最小改法：同 #7。
- 置信度：**高**

### 10. ★ A465 那一行在榜单也补上了：`ArmyScroll.ClipNode` = 这颗节点（`shell.log:15065`）

- 判定：**(α) 级联（同 #7）**（`armVcG == null` ⇒ 恒 false）。**实现侧不用改**：那一行在 `Shell/LeaderboardWindow.cs:498`（`_armyScroll.ClipNode = vpVc;`，刻意写在 `if/else` 之后，因为 `_armyScroll` 复用而节点每次 `Build()` 重建）。
- 证据：同 #7。
- 最小改法：同 #7。
- 置信度：**高**

### 11. （前提·不静默）第 1 颗军种项的 `Hit` 节点拿得到（`shell.log:15078`）

- 判定：**(α) 级联（同 #7）**（军种项根本没建 ⇒ `FindChildIn(armContentG, CampaignData.Armies[0])` = null ⇒ `h0G` = null）
- 证据：同 #7（`armContentG == null` 是上游）。
- 最小改法：同 #7。
- 置信度：**高**

### 12. ★★★ A768①（条目级·态一）：第 1 颗军种项的命中区（`shell.log:15091`）

- 判定：**(α) 级联（同 #7）**（`okHit = false` ⇒ 期望值无从谈起；报错文案「（取不到），期望 248.99,147.64→385.35,258.59」）
- 证据：同 #7。
- 最小改法：同 #7。
- 置信度：**高**

### 13. ★ A768②：`Purchase Premium Window/Scroll View/Viewport` 那颗节点上挂了 `ViewportClip`（`shell.log:15158`）

- 判定：**(β) 实现缺陷**（`Build()` 零调用点 ⇒ 整棵树不存在）
- 证据：
  - `Shell/PurchasePremiumWindow.cs:298-302` `Open()` = `LastOpened = this; Initialize(); Debug.Log(...)` —— **没有 `Build()`**；`Initialize()`（`:312-322`）只做 `ClearArmyContainers / RebuildContainers / 关模板 / ApplyArmyInfo / FocusOnArmy`。
  - 全仓 `grep PurchasePremiumWindow`（除自身文件）只命中 `WindowsManager.cs`（`OpenPurchasePremiumWindow:1322-1327` = `Create` + `OpenEx`，**两条都不调 `Build`**）与测试；文件内 `grep "Build()"` 只命中 `:436` 的定义与注释 ⇒ **`Build()` 没有任何调用点**。
  - 同族**六扇**窗口的 `Open()` 全都调 `Build()`（`GenericOptionsPanel.cs:243` · `AllianceMemberOptionsPopup.cs:229` · `RankedRewardEventWindow.cs:255` · `ReferralPopupWindow.cs:307` · `BaseOfferPopup.cs:643` · `EnergySinglePlayerOnlyEventWindow.cs:250`）⇒ **只有这一扇漏了**。
  - 运行时证据：日志 `:15105-15120` 显示 `Create → OpenWindow → OpenByState → Open → Initialize`（**没有 Build 那一段**），随后就是这条红；而紧跟的「★ A768②：两个字段 = …」**绿**是**空过**（断言写成 `ppwVcG == null || …`）。
- 根因：`Open()` 漏了 `Build()`（**本批新加的文件**，`git status` = `?? untracked`）。
- 最小改法：`Shell/PurchasePremiumWindow.cs:301` 把那句 `Initialize();` 改成 `Build(); Initialize();`（形状照 `Shell/InboxWindow.cs:235` 的 `Open() { Build(); }`）。
- 置信度：**高**

### 14. ★ A465 那一行在这一扇补上了：`Scroll.ClipNode` = 这颗节点（`shell.log:15184`）

- 判定：**(β) 级联（同 #13）**（`ppwVcG == null`）
- 证据：实现侧那一行**在**（`Shell/PurchasePremiumWindow.cs:515 _scroll.ClipNode = vpVc;`）—— 只是 `Build()` 从没跑过 ⇒ 那一句从没执行。
- 最小改法：同 #13。
- 置信度：**高**

### 15. （前提·不静默）第 1 个 `Army Container` 的命中区量得到（`shell.log:15197`）

- 判定：**(β) 级联（同 #13）** —— `ppwG.Containers.Count == 0` ⇒ `c0G = null`
- 证据：日志 `:15160` 那句 `[Premium] … ⚠️ 本地没有任何报价 … ⇒ Content 下一个容器都不建` 是 `Open()` 里**写死的文案**；真正原因是 `_content == null`（`Build()` 没跑）。夹具给了 2 条报价且 `Create` 确实存进了 `_offers`（`:281-282`）⇒ 只要 `Build()` 一跑，这里就该有 2 个容器（对照 `shop.log:46219` 同一条断言写的就是 `期望 [2]`）。
- 最小改法：同 #13。
- 置信度：**中高**（`Build()` 零调用点是硬事实；「2 条报价 ⇒ 2 个容器」没在 ShellScene 实测过，但 ShopScene 的期望值就是 2）

### 16. ★ A773 态①：挂了 `ViewportClip` ⇒ 数量数字被截在框内（`collection.log:9658`）

- 判定：**(α) 夹具错**（`ViewportClip` 挂在**兄弟**节点上，不在那段字的父链上）
- 证据：
  - 实得 `2212.1`，而**对照组（不挂节点）也是 `2212.1`**（`collection.log:9641`「态② 越界 2212.1 > 2151.9」）⇒ **两态一字不差 = 裁切一次都没发生**。
  - 夹具（`Editor/CollectionScene.cs:3174-3177`）：`var fx1s = new GameObject("A773 探针（有节点）").transform;` → **`ViewportClip.Hang(fx1s, "Viewport", a773Vp, …);`** → **`ItemDrawer.SetConverted(fx1s, a773Box, a773St);`**
    ⇒ 组件长在 **`fx1s/Viewport`**，而 `SetConverted`（`Shell/ItemDrawer.cs:949-953`）第一句就是 `MenuDraw.Node(drawer, NodeConvertedDrawer, strip)`、`drawer` = 传进来的 `fx1s` ⇒ 抽屉是 `fx1s/Converted Drawer`、与 `Viewport` 是**兄弟**。
  - `ViewportClip.Resolve`（`Shell/ViewportClip.cs:196-205`）/ `FindAbove`（`:213-220`）**只沿父链往上走** ⇒ 兄弟上的节点**永远命不中**。
- 根因：夹具把「视口节点」挂成了被测子树的兄弟（注释自陈「探针上挂一颗 `ViewportClip`」，实际挂在探针的**子节点**上、且被测件不在它下面）。
- 最小改法：`Editor/CollectionScene.cs:3175-3176` 改成「把抽屉建在节点下」：
  `var a773Vc = ViewportClip.Hang(fx1s, "Viewport", a773Vp, Vector4.zero, Vector2Int.zero);` 然后 `ItemDrawer.SetConverted(a773Vc.transform, a773Box, a773St);`（下面用 `FindChild(fx1s, …)` 的取件是递归的 ⇒ 不用改）。
- 置信度：**高**（实得值 = 对照组值，逐位相同；父链语义是硬判据）

### 17. ★ A773 态①：`Already Owned` 那条路也夹在同一条框沿（`collection.log:9684`）

- 判定：**(α) 同 #16**
- 证据：实得 `2394.9`（= 对照组 `collection.log:9655` 的同一个数）⇒ 同样一次都没裁。
- 最小改法：同 #16。
- 置信度：**高**

### 18. （前提）卡牌页搜索框的 `Input Text` 在（`collection.log:19217`）

- 判定：**(γ) 本批回归**（B1/A781 之后，**落在视口外的 Label 不再建**；容器节点照建 ⇒ 只有 Label 消失）
- 证据：
  - 同一段里**异画页**（抽屉刚展开、`DrawerSettled(1)`）的同类节点**全在**：`:19032`（`Input Text` ✓）· `:19085`（`Owned only` 的 Label ✓）· `:19138`（`Title Army` ✓）；而**卡牌页**（整段里**从未展开** —— 段末才 `styleFltBtn.Click()` 关异画页抽屉，`:19265`）这三件**恰好是同一个「Label 缺失」形状**。
  - 三条红的**共同点**：`Input Text` / `Cell_owned/Label` / `Title Army` —— **全是 `Label`**；它们的**容器节点都在**（`（前提）卡牌页的 Card Filters 节点在` **绿**，`:19212`）。
  - 机制：`Shell/CollectionWindow.cs:1420-1421` 抽屉里就是 `ViewportClip.Hang(panel.Find("Scroll View"), "Viewport", FltView, …)`；本批把 `Shell/WindowsManager.cs:300-364`（B1 · `GameWindow.Text`）与 `Shell/MenuDraw.cs:1578-1581`（A781）两处取状态改成走 `ViewportClip.Resolve` ⇒ `MenuDraw.Visible(r, _st.RenderClip)` 里 `_st.RenderClip` 非 null（迁移前 `Clip == null` ⇒ 一律判「可见」）⇒ **视口外那一格文字从「建出来」变成「不建」**。
  - 旁证（同一段里的既有纪律）：`Title Type` 在异画页被「视口外 ⇒ 不建节点，跳过」（`:19180`，`Editor/CollectionScene.cs:5040-5046` 那段注释）是**早已承认**的行为；本批把这条纪律**推广到了 Label**，而这三条「前提」是照**旧行为**写的。
- 根因：B1/A781 让「父链上有视口节点」的**文字**也吃「整块在框外 ⇒ 不建」⇒ 收起抽屉里的 Label 不再存在（**画面等价、结构不等价** —— 原版 `RectMask2D` 不删节点、只裁像素）。
- 最小改法（两条，**建议先做 1**）：
  1. **夹具侧**（不减功能、一行级）：对照组查这三件之前先把卡牌页抽屉展开到位（同异画页那套：点该页筛选钮 + `DrawerSettled(1)`），段末再关回去 —— 在 `Editor/CollectionScene.cs:5075` 之前插入展开即可，三条断言（`:5079/:5090/:5096`）一个字不用改。
  2. **实现侧**（原版语义，按铁律 11 属于「要做」）：把「整块在框外 ⇒ 不建」从**文字**这一路去掉（A781 的注释把这道闸留在 `MenuWindowBase.Text`/`GameWindow.Text` 那一层、并写「那一条差异仍然开着（另立账）」）⇒ 收起抽屉里的 Label 重新存在（画面被裁成零面积，同原版）。**这笔要单独立账，别在收口批里顺手做。**
- 置信度：**中高**（「三条全是 Label、容器都在、且全在**未展开**那一页」的形状很硬；但没有逐字确认抽屉当时的展开状态 —— 见 §四·2）

### 19. （前提）卡牌页 `Owned only` 那一格的标签在（`collection.log:19230`）

- 判定：**(γ) 同 #18**
- 证据：同 #18（同为 Label；异画页同名件 `:19085` 绿）。
- 最小改法：同 #18。
- 置信度：**中高**

### 20. （前提）卡牌页小标题 `Title Army` 在（`collection.log:19243`）

- 判定：**(γ) 同 #18**
- 证据：同 #18。旁证：异画页同名的 `Title Army` **在**（`:19138` 绿、字号 36 对得上）。
- 最小改法：同 #18。
- 置信度：**中高**

---

## 三、按档汇总

**(α) 12 条 —— 都可一行级修**
- #1 #2：`Editor/ShellScene.cs:3435` 读数器要跳过 `!activeInHierarchy`（照 `GUnion:4453`）。
- #5：`Editor/ShellScene.cs:4361` 的 `content` 要限定到 `AllFactions`（同页有两颗同名节点）。
- #6：`Editor/ShellScene.cs:4377` 后补 `shell.Windows.OpenWindow(rw2);`
- #7–#12（榜单 6 条）：`Editor/ShellScene.cs:4543` 后补 `shell.Windows.OpenWindow(lbG);`
- #16 #17：`Editor/CollectionScene.cs:3175-3176` 把抽屉建在视口节点**之下**。

**(β) 5 条 —— 两个根因，都要改生产代码**
- `Shell/PurchasePremiumWindow.cs:301`：`Open()` 补 `Build();`（#13 #14 #15 + ShopScene 一片）
- `Battle/Label.cs:862/872`：`AlignLeftOn/AlignRightOn` 平移之后补一次重裁（#3 #4）

**(γ) 3 条 —— 一个根因**
- B1（`Shell/WindowsManager.cs:300-364`）+ A781（`Shell/MenuDraw.cs:1578`）让**视口外的 Label 不再建** ⇒ 收起抽屉里的三条「前提」红（#18 #19 #20）。夹具侧一行可解；实现侧是「原版语义 vs 我们建时不建」那笔老账，**单独立账**。

---

## 四、判不了的（如实列）

1. **#3/#4 的位移量没逐位复算**：「文字裁切框被平移 +123.16px」由**三个态的差值逐位相同**钉死，来源由「A464 对照组贴边 / 本条不贴边」的对照指到 `Label.Align*On`；但 `AlignLeftOn` 那一句里的 `WorldW`（TMP `textBounds`，自检里取不到）没量到 ⇒ 「位移量 = 对齐平移量」这一步**没能算术验算**。要钉死：在 `AlignLeftOn` 里临时打一行 `Debug.Log`（改前/改后的 `transform.localPosition.x`）跑一次。
2. **#18–#20 少一个证据**：没有确认那三条 Label 判定时用的是**哪一个**框（抽屉视口节点 vs 页面级节点）。要钉死：夹具先展开卡牌页抽屉跑一次 —— **若三条立刻转绿，就等价证明**（但不能排除「Label 变不建」这件事本身）。
3. **#15** 的期望值（2 条报价 ⇒ 2 个容器）来自 ShopScene 的同名断言，**没在 ShellScene 实测过**；`Build()` 补上之后跑一次即确认。
4. 我**没有跑 Unity**、**没有改任何工程文件**（只读）—— 全部「最小改法」都是**未验证的建议**。

---

## 五、顺手发现（⛔ 别自己顺手改）

1. **跨宿主**：`Shell/PurchasePremiumWindow.cs` 是**本批新加的文件**（`?? untracked`），`Build()` 从来没有调用点 ⇒ **`ShopScene.Run` 里同一颗雷炸了 ~18 条**（`shop.log:45928` 起「实得 []」那一族：根的直接子件空 · `Scroll View` 的直系子件空 · `Title`/`Army Info` 的框「节点不在」· `Content` 的 CSF 高量不到 · 「喂 2 条报价 ⇒ 期望 [2]，实得 [0]」…）。**块 2 之外的红里可能有很大一半是它** ⇒ 建议 `:301` 一行修完，两个宿主一起复跑。
2. **夹具 API 不一致（值得写进纪律）**：`LeaderboardWindow.Create` / `RewardsWindow.Create` / `InboxWindow.Create` **不自开窗**（调用方要补 `OpenWindow`），而 `PurchasePremiumWindow.Create` / 走 `WindowsManager.OpenXxx` 那一族**会开窗**。本批两处红（#6 · #7–#12）正是照错了那一套写的。⇒ 建议记一条「**`Create` 到底开不开窗：逐窗现读，别按族推**」。
3. **同一文件里两个同类读数器口径不同**：`Editor/ShellScene.cs` 的 `MaxQuadBottomPx:3431`（不跳过未激活）vs `GUnion:4448`（跳过）。这一族读数器建议**收口成一份**，否则下次还会有人踩。
4. **`ClipTmpMesh` 的「裁到零面积」在网格上仍可见**：`Shell/MenuDraw.cs:1109-1180` 把框外的角**夹到框沿 + 把 alpha 写 0**，而 `TmpSpanPx` / `TextMeshWidthPx` 只看 `isVisible` 与顶点位置 ⇒ 「被裁没了」与「没裁」在某些位置上读数接近（`Editor/CollectionScene.cs:3199` 那条 `px2 >= a773Edge − 0.5f` 就是这种「弱夹切」）。要断「真被裁过」得**同时看 `meshInfo[].colors32` 的 alpha**。
5. **探针形状要统一**：`Editor/RewardsScene.cs` 的 A302 探针是「无父 `GameObject`」（节点即根、天然在父链里），而 A773 这一份是「节点挂成子件」⇒ 与 A302 不同形。⛔ 别顺手把 A773 改成挂根上（会与 A302 的既有形状打架）—— 由调度台定一套。
6. **别把 #3 甩给 `PosInDesignSpace`**：`Shell/MenuDraw.cs:71-95` 自陈「只在窗根在世界原点时精确」，但那种误差是**缩放型**；本次实测位移是**纯平移**（+123.16 在两个不同框位上逐位相同）⇒ 与 A294/A298 那条不是同一族，按那条去修会是错的修法。
