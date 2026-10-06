# D1013 · 诊断块 4（`ShopScene` 31 条 + `RewardsScene` 3 条）

> 诊断时刻：2026-10-13（第四轮收口后，`_tmp_view/shop.log` 12:59 · `_tmp_view/rewards.log` 12:58）
> 只读诊断。**⛔ 没改任何工程文件**；本文件是唯一产出。
> 日志合计：`shop.log:48431` = **2106 通过 / 31 失败** · `rewards.log:34069` = **1825 通过 / 3 失败**。
> 逐条红的「宿主行号」= 取该行下面那段 Unity 堆栈里的 `ShopScene:Run () (at …/ShopScene.cs:NNNN)`
> —— 已逐条与**现读的源文件行号**对上（文件与日志同版，例：日志 `Run:4095` = 现文件 `:4095` 那条 `Promote` 断言，逐字相同）。

---

## 一、总结论

**34 条，分 8 族。11 条 α（断言错）· 23 条 β（实现缺陷）· 0 条 γ（本批回归 —— 这批红全部落在本批新增/新改的件上，没有一条是「原来绿、现在红」）· 0 条 δ。**

| 族 | 条数 | 档 | 一句话根因 |
|---|---|---|---|
| **F1 `PurchasePremiumWindow` 从头到尾没调过 `Build()`** | **20** | **β** | `Open()` 里只有 `Initialize()`（另五扇同族窗第一句都是 `Build()`）⇒ 整棵树 0 个子件，`ppw` 段 20 条连锁红 |
| **F2 `BaseOfferPopup.BuildTexts` 里 `AlignLeft` 排在 `SetAutoFitBox` 之前** | **3** | **β** | `SetAutoFitBox` 末句 `RefreshBounds()` 把整块重新居中 ⇒ 左对齐被抹掉，实测偏 **+23.20px** |
| **F3 缺图断言过期**（三张图已在 12:31 被手拷进 `Resources/Art/ui_menu/`） | 4 纯 α + 2 β连带 | **α** | 断言仍钉 `MissingArt == 1`；实测 `MissingArt == 0`、且日志里**没有**对应的「图取不到」告警 |
| **F4 `KidNames` 期望串漏了出厂关着的 `*` 前缀** | 2 | **α** | 期望串与紧随其后的「XX 关着」断言**自相矛盾** |
| **F5 A517 用 `TmpEdgePx`（mesh 顶点）配 `edgePx`（`textBounds`）口径的期望值** | 2 | **α** | 两把尺子量的是两个东西（墨迹起点 vs 整块框），差一个首字左边距 |
| **F6 夹具喂的数据与断言自己的前提不符** | 1 | **α** | `Role=0 / MyRole=3` ⇒ `outrank=true`，而断言文案写「`role < MyRole` 不满足」 |
| **F7 夹具持有已被重建销毁的引用** | 1 | **α** | `MissionsTab.Build()` 会 `DestroyImmediate` 整棵子树（助手注释自己写着「旧引用是已销毁对象」） |
| **F8 把「只出声、不当缺点断」的全局表当成缺点断** | 1 | **α** | `WindowButton.MissingPressedArt` 的注释明写「这份表只出声、不当缺点断」 |

### 🔴 先做哪两件（20 + 3 条，改两处地方）

1. **`Shell/PurchasePremiumWindow.cs:298-302` 的 `Open()` 里补一句 `Build();`**（放在 `LastInitialise`… 前面，见 §三·7 的最小改法）⇒ **一次消掉 20 条**。
2. **`Shell/BaseOfferPopup.cs:804-805 / 811-813` 把 `MenuDraw.AlignLeft(...)` 挪到 `SetAutoFitBox(...)` 之后** ⇒ **一次消掉 3 条**。

---

## 二、🔴 优先判：`Editor/ShopScene.cs:4086` 那条（A780 预告的坐标档位）

> ⚠️ **行号已经漂了**：`批次计划_1013.md:467` 说的 `:4086` 是**当时**的行号（那批报告写完之后又插进了 L1/L2/L4/L3 四段）。
> 按内容对，它今天 = **`Editor/ShopScene.cs:4163-4164`**：
> `CheckAt(ppw.Containers.Count > 0 ? ppw.Containers[0] : null, 226.055f, 757.285f, 100.57f, 263.89f, "★ 第 1 个容器的框（`Content` 顶 + 25 起排）")`
> —— 它就是本 run 的 **`shop.log:46257`（`Run:4163`）**。

**结论：**

- **本 run 判不了它本身**：那一条报的是「（节点不在）」—— 因为前置的 F1（`Build()` 没跑 ⇒ `ppw.Containers.Count == 0`），容器**根本没建出来**，`CheckAt` 拿到 `null` 直接红。**这条红没有携带任何坐标读数。**
- **静态判读：A780 的推导成立，是【真实现缺陷】（β）。** 证据三条，都是现读的：
  1. **模板那一份用的是绝对框**：`PurchasePremiumWindow.cs:526-527`
     `_containerTemplate = MenuDraw.Node(_content, "Army Container", Abs(ContainerR));` / `BuildContainer(_containerTemplate, Abs(ContainerR), null);`
     —— **带 `Abs`**；而 `RebuildContainers`（`:603-607`）传的是 `var r = ContainerRect(i); var on = _scroll != null ? _scroll.Shift(r) : r;` —— **不带 `Abs`**。
  2. **`MenuDraw` 的矩形实参是【绝对画布框】**（`MenuDraw.Node → ApplyPxRect → Local(parent,…)` = `矩形中心 − 父件设计位置`，`Shell/MenuDraw.cs:30-31 / 119-125`）⇒ 传「相对根」的框，节点就会落在「相对根」那一档，**整棵子树差一个根原点**。
     本窗 `Abs()` 加的就是 `(167.175, 70.94)`（`PurchasePremiumWindow.cs:411-421` 注释与算式），与断言里那四个数逐位吻合：
     `58.88+167.175=226.055` · `590.11+167.175=757.285` · `29.63+70.94=100.57` · `192.95+70.94=263.89` ✓
     ⇒ **断言期望的就是 `Abs(ContainerRect(0))`，没错。**
  3. **子件反而在绝对档上**：`BuildContainer` 里 `Sub(abs) = r.x1 + (abs.x1 − ContainerR.x1)`（`:672-673`），`r.x1` 与 `ContainerR.x1` 相消后正好把根原点**加回去** ⇒ 容器**节点**在相对档、它的**子件**在绝对档 —— 这就是 A780 那句「容器节点与子件不在同一档坐标」。按 `MenuDraw.Local` 算，容器节点今天落在 `(324.495, 111.29)`（= 相对框的矩形中心），而它**应该**在 `(491.67, 182.23)`。

**最小改法（一处改两半，缺一半会把子件再加一次根原点）：**

- `Shell/PurchasePremiumWindow.cs:603-609`：把 `on` 统一搬到绝对档 ——
  `var on = _scroll != null ? _scroll.Shift(r) : r;` → 后面 **`MenuDraw.Node(_content, name, Abs(on))` / `BuildContainer(cn, Abs(on), …)` / `MenuDraw.Hit(cn, "Hit", Abs(on), …)`**，
  并确认 `_scroll.Intersects/Abs` 也吃同一档（`_scroll.ContentX1/2` 那一处 `:598-599` 今天用的就是 `Abs(ViewportR)`，照它）。
- `Shell/PurchasePremiumWindow.cs:672`：`Sub` 的基准从 `ContainerR` 改成 **`Abs(ContainerR)`**：
  `PxRect Sub(PxRect abs) { var c = Abs(ContainerR); return new PxRect(r.x1 + (abs.x1 - c.x1), r.y1 + (abs.y1 - c.y1), r.x1 + (abs.x2 - c.x1), r.y1 + (abs.y2 - c.y1)); }`
  —— 顺手把**模板那一份今天多算一次根原点**（`Sub(Abs(X))` 在 `r == Abs(ContainerR)` 时 = `X + 2×根原点`）也修掉。
- ⚠️ 这是**跨两处、且改了共用算式**，**建议先只改 F1 再跑一次**看 `:4163` 那条报出的**实得坐标**，用它反证上面第 3 步（那才是本次 run 拿不到的那一个读数）。**置信度：中高**（推导每一步都有现读出处，缺的是「跑起来量一次」）。

---

## 三、逐条（34 节）

> 说明：`Run:NNNN` = 日志堆栈里的宿主行号；括号里那条 = 现读源文件行号，**同一行**。

### 1. `Title` 左沿（`shop.log:43158` · `Run:3804`）
- 判定：**(β)**（F2）
- 证据：期望 `976.00±2.00`，**实得 `999.20`**（+23.20）；上一行「（前提）`Title` 量得到矩形」✓、「（前提）`Title` 是真 TMP」✓
  ⇒ 读数可信。`Shell/BaseOfferPopup.cs:800-806`：`MenuDraw.Text(...)` → **`MenuDraw.AlignLeft(title, G.Title)`（:804）** → **`title.SetAutoFitBox(...)`（:805）**。
- 根因：`SetAutoFitBox` 末句是 **`RefreshBounds()`**（`Battle/Label.cs:694`，注释逐字写着「字号变了 ⇒ 尺寸/摆位都要重算」），而 `RefreshBounds` 把整块**重新摆回锚点中心**（`:806-809`）⇒ 前面那次 `AlignLeftOn` 被抹掉，文字回到「框心居中」。
  **数值自证**：框 `976.00→1492.72`（宽 516.72，中心 1234.36）；`1234.36 − 999.20 = 235.16` ⇒ 渲染宽 `470.32` —— 正是「整块居中」的解。
- 最小改法：`Shell/BaseOfferPopup.cs` 把 `:804` 的 `MenuDraw.AlignLeft(title, G.Title);` **整行挪到 `:805` 之后**。
  📌 **同文件里的正确写法就在隔壁**：`Descripton`（`:816-818`）走 `MenuDraw.TextBox`（它自己内部先 `SetWrapWidth`+`SetAutoFitBox`）**再** `AlignLeft` ⇒ 它是对的；同一条纪律也写在 `MenuDraw.cs:1630`（「裁切/对齐必须落在那两步**之后**」）。
- 置信度：**高**

### 2. `Category` 左沿（`shop.log:43199` · `Run:3812`）
- 判定：**(β)**（F2，同根因）
- 证据：期望 `976.00±2.00`，**实得 `1036.49`**（+60.49）；同样「居中」自洽：`1234.36 − 1036.49 = 197.87` ⇒ 渲染宽 `395.74`。
  `Shell/BaseOfferPopup.cs:808-814`：`AlignLeft`（:811）→ `SetCharSpacing(-1.8f)`（:812，只重排 mesh、不挪节点）→ `SetAutoFitBox`（:813）。
- 根因：同 #1。
- 最小改法：把 `:811` 那一行挪到 `:813` 之后（`SetCharSpacing` 保持在前，它对节点位置无影响）。
- 置信度：**高**

### 3. 大档 `Title` 左沿（`shop.log:43942` · `Run:3902`）
- 判定：**(β)**（F2，同根因）
- 证据：期望 `1120.00±1.50`，**实得 `1143.20`** —— 与 #1 **同一个 +23.20**（大档框 `1120.00→1636.72` 宽同为 516.72、中心 1378.36 ⇒ 实得宽 470.32 = 同一段文字）
  ⇒ 与「大档是不是整棵平移」**无关**，纯粹是同一颗 `Title` 的居中问题。
- 最小改法：同 #1（改一次，三条一起绿）。
- 置信度：**高**

### 4. `gop` 缺图 1 张（`shop.log:45161` · `Run:4044`）
- 判定：**(α)**（F3）
- 证据：`Check(gop.MissingArt.Count, 1, …)` 期望 `1`、**实得 `0`**。
  🔴 **判据是「日志里没有告警」**：`Tex()` 取不到一定会 `Debug.LogWarning("[OptionsPanel] 图取不到：…")`（`Shell/GenericOptionsPanel.cs:479-489`）；
  `grep "\[OptionsPanel\]" shop.log` **只有 2 行**，两行都是「开了…」，**一条「图取不到」都没有**。
  而 `Resources/Art/ui_menu/OctagonUI_Filled_SDF.png` **确实在盘上**（mtime `2026-10-06 12:31`，早于本 run 的 12:58）。
- 根因：`WL2_四扇小窗.md:275` 自己写着「**还欠主对话一次 `工具/import_original_art.py` 的腿**（把 #1 #2 #3 三张导进 `Resources/Art/ui_menu/`）」—— 那次导入**做了**（12:31），**断言没跟着改**。
- 最小改法：`Editor/ShopScene.cs:4044` → `Check(gop.MissingArt.Count, 0, "★ 本窗一张图都不缺（`OctagonUI Filled SDF` 已进 `Resources/Art/ui_menu/`）");`
- 置信度：**高**

### 5. `gop` 缺的是 `OctagonUI_Filled_SDF`（`shop.log:45173` · `Run:4045`）
- 判定：**(α)**（F3，与 #4 同一处、同一原因）
- 证据：`Check(gop.MissingArt.Count > 0 ? gop.MissingArt[0] : "-", "OctagonUI_Filled_SDF", …)` 期望那个名字、**实得 `-`**（因为 Count==0 走了三元表达式的 `"-"` 支）。
- 最小改法：**删掉 `Editor/ShopScene.cs:4045-4046` 这两行**（它只在「真的缺」时才成立；#4 改成 0 之后它已经没有可断言的对象）。
- 置信度：**高**

### 6. `amop` 两态·`Promote` 关着（`shop.log:45773` · `Run:4095`）
- 判定：**(α)**（F6）
- 证据：夹具 `Editor/ShopScene.cs:4087-4088`
  `{ Name = "Tester", Role = 0, IsSelf = true, IsFriend = false, SameGroup = true, MyRole = 3 }`；
  原版规则（`Shell/AllianceMemberOptionsPopup.cs:26` 文件头 + `:179` + `:396`，逐条标了出处）
  `outrank = sameGroup && role < myRole` · `Promote = outrank && role < 3` ⇒ 喂进去 `0 < 3` **成立** ⇒ `outrank=true` ⇒ **`Promote` 本来就该开着**。
  实测确实是开的（该断言唯一失败的分支 = `activeSelf == true`，因为 `BtnNode(3) != null` 那一半在上一行 `Promote` 换字断言里已经被证明非空）。
- 根因：**断言文案的前提与夹具不符** —— 它说「`role < MyRole` 不满足那一支关着」，而夹具给的正是**满足**的那一档。
- 最小改法（二选一，推荐前者，保留断言的判别力）：
  ① `Editor/ShopScene.cs:4088` 把 `MyRole = 3` 改成 **`MyRole = 0`** ⇒ `Role(0) < MyRole(0)` 为假 ⇒ `outrank=false` ⇒ `Promote` 关，文案也读得通了（同一行的 `Quit` 断言只看 `isSelf`，不受影响）；
  ② 或把 `:4095` 的期望改成 `true`（但那样就不再检验 `outrank` 那道闸）。
- 置信度：**高**

### 7. `ppw` 根的直接子件（`shop.log:45928` · `Run:4108`）—— 🔴 F1 族头
- 判定：**(β)**
- 证据：`KidNames(ppw.transform)` 期望 9 件、**实得 `` （空串 = `transform.childCount == 0`）**。
  本段前面 6 条（`name` / `type` / `placement` / `closeOnESC` / `extraScaleSmallScreen` / `FocusArmy`）**全绿** ⇒ 窗口对象建出来了、字段对，**只是没铺内容**。
- 根因：**`Shell/PurchasePremiumWindow.cs` 全文件没有一处调用 `Build()`**（`grep "Build()"` 只有 `:436` 的定义与两处注释）。
  `Open()`（`:298-302`）第一句是 `LastOpened = this;` 然后**直接 `Initialize()`**；
  而**同族的另五扇全都是 `Build()` 打头**：`GenericOptionsPanel.cs:243` · `AllianceMemberOptionsPopup.cs:229` · `BaseOfferPopup.cs:643/663` · `ReferralPopupWindow.cs:307` · `RankedRewardEventWindow.cs:255`。
  `Initialize()` 只动 `_content` / `_containerTemplate` / `_armyInfo`（`Build()` 才建它们）⇒ 全程对着 `null` 干活。
- 最小改法：`Shell/PurchasePremiumWindow.cs:298-301` 改成
  `public override void Open() { LastOpened = this; Build(); Initialize(); Debug.Log(…); }`
  ⚠️ **顺序不能反**：`Build()` 末句是 `StartPop()`（`:564`）而 `Build()` 头一句把 `_popT = -1f`（`:449`）—— 反了会把开场动画的起点抹掉。
- 置信度：**高**

### 8–26. `ppw` 段其余 19 条（同一根因 F1，逐条列实得值）
> 每条的「期望 / 实得」都取自日志原文；判定一律 **(β)**，根因一律「`Build()` 从没跑 ⇒ 那一件根本没建/没画」；最小改法一律 **同 #7**（一处 `Build();` 全消）。置信度一律 **高**。**⛔ 不合并**，逐条列出以便同步点核对「修完是否恰好这 20 条转绿」。

| # | 日志行 | `Run:` | 断言（缩写） | 期望 → 实得 |
|---|---|---|---|---|
| 8 | 45940 | 4113 | `Scrollbar Collection` 是 `Scroll View` 的子件 | 节点不在（`FindChild` 返回 null） |
| 9 | 45954 | 4115 | 关窗钮的直系子件 | `[节点不在] · 缺 [Image\|Hit\|Background\|Icon]` |
| 10 | 45968 | 4118 | `SubTitle` 出厂关着 | True → **False** |
| 11 | 45981 | 4121 | 价签钮 `Button Text` 出厂关着 | True → **False** |
| 12 | 45994 | 4124 | `Scrollbar Collection` 出厂关着 | True → **False** |
| 13 | 46007 | 4127 | （反例）`Purchased Text` 开着 | True → **False** |
| 14 | 46044 | 4133 | `Army Container` 模板关着 | True → **False** |
| 15 | 46057 | 4135 | `Content` 的直系子件只有模板 | `[节点不在] · 缺 [Army Container]` |
| 16 | 46071 | 4137 | `Title` 的框 | 节点不在 |
| 17 | 46085 | 4138 | `Army Info` 的框 | 节点不在 |
| 18 | 46099 | 4139 | 窗体底 `Generic Window Red Background Big` 的框 | 节点不在 |
| 19 | 46113 | 4141 | `Content` 的框 | 节点不在 |
| 20 | 46127 | 4144 | 缺图 **1** 张（`Hightlight` 那一格） | 1 → **0**（ⓐβ ⓑα 双因，见下） |
| 21 | 46139 | 4145 | 缺的是 `UI_HIghlight Internal` | `UI_HIghlight_Internal` → **`-`**（同上） |
| 22 | 46151 | 4148 | 刚开出来动画**还没跑完**（`PopDone == false`） | False → **True** |
| 23 | 46219 | 4158 | 喂 2 条报价 ⇒ `Content` 下 2 个容器 | 2 → **0** |
| 24 | 46231 | 4159 | （灭自证）建完容器后模板仍关着 | True → **False** |
| 25 | 46244 | 4161 | （反例）第 1 个容器开着 | True → **False** |
| 26 | 46257 | 4163 | 第 1 个容器的框 | 节点不在（**= A780 那条**，见 §二） |

- **#22 单独补一句证据**（它不是「顺带」）：`PopDone { get { return _popT < 0f; } }`（`:732`），而 `_popT` 只在 `Build()` 里被写成 `-1f`（`:449`）以后由 `StartPop()`（`:564`，`Build()` 末句）写成 `0f` ⇒ 没 `Build` 就是「没在跑」= `true`。补上 `Build()` 之后这一条自然转绿。

### 20–21. `ppw` 缺图 1 张 / 缺的是 `UI_HIghlight Internal`（`shop.log:46127`/`46139` · `Run:4144`/`4145`）
- 判定：**(β) 为主因 + (α) 期望值也过期**（F1 × F3）
- 证据：期望 `1`、**实得 `0`**；第二条期望 `UI_HIghlight_Internal`、**实得 `-`**。
  两条**同时**成立：① F1（没 `Build` ⇒ `Tex()` 一次都没被调过，`MissingArt` 空是**平凡**的）；
  ② F3 —— 修好 `Build()` 之后 `BuildContainer` 会真的去 `Tex(ArtHightlight, …)`（`:680-685`，模板那一份在 `Build()` 里就建，见 `:527`），
  而 `Resources/Art/ui_menu/UI_HIghlight_Internal.png` **在盘上**（12:31 导的）⇒ 仍然 `MissingArt == 0` ⇒ **这条断言修完 `Build()` 还会红**。
  ③ 反证：日志里只有 `[Premium] Army Icon 的图取不到`（另一张、另一因），**没有** `UI_HIghlight Internal` 那一条告警。
- 最小改法：**两件一起做** —— `Editor/ShopScene.cs:4144` 期望改 `0`；`:4145-4146` 两行删掉。（与 #7 的最小改法合起来才收口。）
- 置信度：**高**（对「期望值过期」）；**中高**（对「修完 `Build` 后它仍是 0」—— 依赖 `Resources/Art/ui_menu/UI_HIghlight_Internal.png` 被 Unity 正常导入，本 run 无法直接证）

### 27. `rre` 的 `window` 直系子件（`shop.log:46407` · `Run:4179`）
- 判定：**(α)**（F4）
- 证据：期望 `…|Title|Description|Timer|Bonus points|Scroll View`，
  **实得 `…|*Title|*Description|*Timer|Bonus points|Scroll View`**（`*` = `KidNames` 给「出厂关着」加的前缀，`Editor/ShopScene.cs:459-472`）。
- 根因：这一段是 **no-data 那一档**（`:4168` 开窗、`view == null`），紧随其后的三条断言**自己**就在钉「`Title` 关着 / `Description` 关着 / `Timer` 整件关着」（`:4185-4190`，日志里那三条**全绿**）
  ⇒ **期望串漏了三个 `*`**，同一节里自相矛盾。
- 最小改法：`Editor/ShopScene.cs:4180` 改成
  `"Generic Window Red Background Big|Generic Close Button Orange|*Title|*Description|*Timer|Bonus points|Scroll View"`。
- 置信度：**高**

### 28. `rre` 缺图 1 张（`shop.log:46603` · `Run:4207`）
- 判定：**(α)**（F3）
- 证据：期望 `1`、**实得 `0`**；`Resources/Art/ui_menu/40k_UI_Banner_BW.png` 在盘上（12:31）。
  🔴 **反证**：同一扇窗**确有**一条「图取不到」告警，但名字是 **`40K_shop_offer_bg_Sororitas_0`**（`Shell/RankedRewardEventWindow.cs:171`，日志 `:46627`）——
  也就是说 `Tex()` 这条口**是活的**、**没对横幅报缺** ⇒ 横幅**取到了**。
- 最小改法：`Editor/ShopScene.cs:4207` 期望改 `0`（并把文案里的「缺 `40k_UI_Banner BW`」改掉）。
  ⚠️ 注意这**不影响** #29 之后那一段：`:4211` 的 `SetBoost(...)` 会喂数据，`40K_shop_offer_bg_Sororitas_0` 那时才进 `MissingArt`（那条**是真缺**，别顺手改成 0）。
- 置信度：**高**

### 29. `rre` 缺的是 `40k_UI_Banner_BW`（`shop.log:46615` · `Run:4208`）
- 判定：**(α)**（F3，与 #28 同处同因）
- 证据：期望 `40k_UI_Banner_BW`、**实得 `-`**（Count==0 走三元 `"-"` 支）。
- 最小改法：删 `Editor/ShopScene.cs:4208-4209` 两行。
- 置信度：**高**

### 30. `rp` 的 `content` 直系子件（`shop.log:46912` · `Run:4272`）
- 判定：**(α)**（F4）
- 证据：期望 `…|Referred View|…|counter`，**实得 `…|*Referred View|…|*counter`**。
  紧随其后的两条断言（日志 `:46956` 附近，**全绿**）逐字写着：
  「✓ ★ 出厂：`Referred View` **关着**（prefab `act = F`）」「✓ ★ **`counter` 关着**（prefab `act = F`）」
  —— 与 `WL3_ReferralPopup.md:78`（`Referred View` = **F** · 建成关着）与 `:86`（`counter` = **F** · 建成关着）逐条吻合 ⇒ **实现是对的，期望串漏了两个 `*`**。
- 最小改法：`Editor/ShopScene.cs:4273` 改成
  `"Referral Title|Input View|*Referred View|Divisor line members|spacing (1)|Title|Descripton|counter number|*counter"`。
- 置信度：**高**

### 31. 按下图一张都不缺（`shop.log:47826` · `Run:4425`）
- 判定：**(α)**（F8）
- 证据：`CheckTrue(WindowButton.MissingPressedArt.Count == 0, …)`，实得 `false`；日志把表打出来了 ——
  **`（缺的会列在这里： → ）`**，即表里**恰好 1 条、内容是 `" → "`**（同一个读数另有一处：`shop.log:41147` 的「[按下图] 取不到的是 **1** 条（例： → ）」）。
- 根因：`Shell/PromptPopup.cs:619-621` 记的 key 是 `art + " → " + pn`；`Shell/InboxWindow.cs:339`
  `hit.Bind(baseQ, null, "40k_general_bt_yellow_hover");` 把 **`art` 传成 `null`** ⇒ `PressedNameFor(null) = null` ⇒ 记下 `" → "`。
  🔴 **而这张表的注释自己写着「这份表只出声、不当缺点断」**（`Shell/PromptPopup.cs:589-591`：「要断它得先有『我们这一颗 → 原版哪一颗』的映射 = A15 那笔账」）
  ⇒ 本条断言与该共用件的**已定口径直接打架**；`ShopScene` 自己在别处也只把它**打进日志**（`Editor/ShopScene.cs:3229-3233`，注释逐字「这里只把它打进日志」）。
  另：`InboxWindow` 那一颗**有**悬停图（`_hoverTex`），`Press()` 会退回高亮图（`PromptPopup.cs:697` 那句 `??`）⇒ 按下**画面会变**，属注释里说的「多数是合法的」那一档。
- 最小改法：删掉 `Editor/ShopScene.cs:4425-4426` 两行（改成 `Debug.Log` 也行 —— 但**同一条信息 `:3229` 已经打过了**，重复；建议直接删）。
  次要选项（**若**要保留这条闸）：改成只断「**有高亮图却没有按下图**」的那一类，而不是整张表空 —— 但那要动共用件的记录口径，**不在本批白名单**，另立账。
- 置信度：**高**

### 32. 勾画在方框之上（`rewards.log:9747` · `RewardsScene.cs:2502`）
- 判定：**(α)**（F7）
- 证据：`CheckTrue(qck != null && qbx != null && qck.RenderQueue > qbx.RenderQueue, …)`。
  · 同段**上一行**用的是现取的引用：`lq = FindChild(skB, "Step Text")`，而 `qck` 来自 `ckB = named389(skB, "CheckMark")` —— 都取自 `skB`；紧跟着那条 `qck.RenderQueue < lq.RenderQueue` **✓ 通过**（`QContent=3010 < QText=3011`，`Shell/MenuWindowBase.cs:72`）。
  · 反面这一半用的是 **`boxA`**：`Editor/RewardsScene.cs:2422` 从 `skA` 取，而 `:2469` 又跑了一次 **`mt389.Build()`** ⇒ `MissionsTab.Build()` 会把 `_root` 的子件**整棵 `DestroySafe`**（`Shell/MissionsTab.cs:248-252`），`DestroySafe` 在编辑器非播放态走 **`DestroyImmediate`**（`Shell/MenuWindowBase.cs:170-176`）
  ⇒ `boxA[0]` 是**已销毁对象**，`quad389` 的 `t != null ? … : null` 回 `null` ⇒ `qbx == null` ⇒ 红。
  · 🔴 **写这段代码的人自己把这条坑写在上面了**：`:2377` 的注释逐字「按名收子树（**每次现取** —— `Build()` 会重建整棵子树，**旧引用是已销毁对象**）」。而 `:2499` 用的正是**唯一那处**旧引用。
  · **反证「不是队列真的反了」**：`MissionsTab.cs:1237/1244/1254` 三处队列是 `QContent-1 = 3009`（方框）/ `QContent-2 = 3008`（描边）/ `QContent = 3010`（勾），日间那一支**逐条写着收口值**（`:1252`）；若 `qbx` 有效，`3010 > 3009` 必真。
- 最小改法：`Editor/RewardsScene.cs:2499` 改成从**新容器**现取：
  `var qbx = quad389(named389(skB, "Box").Count > 0 ? named389(skB, "Box")[0] : null);`
  （或把 `:2502` 整条挪到 `:2469` 那次 `Build()` **之前**那一处 `boxA` 还活着的地方 —— 但 `ckB` 是第二次 Build 才出的，所以**前一种更小**。）
- 附注（加强 α 的判定）：这一整段（`mt389` / 「A389 里程碑格」）在 `git show HEAD:…/RewardsScene.cs` 里 **0 命中** ⇒ **本批新增**，从未绿过 ⇒ 不是回归，就是新写的断言自己拿错了引用。
- 置信度：**高**

### 33. A517 `Current Streak` 真渲染左缘（`rewards.log:23568` · `RewardsScene.cs:6709`）
- 判定：**(α)**（F5）
- 证据：期望 `43.00±1.50`、**实得 `45.22`**（+2.22）。
  🔴 **同一颗、同一个期望值，本 run 里另一条断言刚刚以 `edgePx` 量过并全绿**：`rewards.log:23459`「✓ ★ A493#1：`Current Streak` 的真渲染左缘 = 框左沿 **43.0** …（**43.00 ≈ 43.00±1.50**）」（源 `Editor/RewardsScene.cs:6644`）。
  两条的**唯一差别是尺子**：A493 走 `edgePx`（TMP 的 **`textBounds`**，`:6451-6464`）、A517 走 `TmpEdgePx`（**mesh 顶点**最小 x，`:6440-6448`）。
- 根因：**两把尺子量的不是同一个东西**。`Label.RefreshBounds` 用的是 **`_tmp.textBounds`**（`Battle/Label.cs:802-809`）⇒ `WorldW` 是「整块框」宽，`AlignLeftOn` 把**框的左沿**钉在 43.00；
  而 mesh 顶点是**字形墨迹**，起点比框左沿多一个**首字左边距**（`2.22px @ fs70 ≈ 0.032em`）。
  文件里那句「两条在本工程的口径下**应当逐值相同**」（`Editor/RewardsScene.cs:6433-6435`）**是错的** —— 它把「网格的 `b.min.x`」与「TMP 的 `textBounds`」当成同一个量了（前者是墨迹、后者是排版框）。
  **数值自证**：`Current Streak Value`（1 个字符）两条尺子只差 **1.35px**（`547.26` vs `545.91`，落在 1.50 容差内 ⇒ **它绿**）—— 字符越少墨迹越贴近框沿，正是「首字左边距」的形状。
- 最小改法：`Editor/RewardsScene.cs:6709` 把 `TmpEdgePx(csNodes[i].node, false)` 换成 **`edgePx(csNodes[i].node, true)`**（与 A493 那批同一把尺子；判别力不变 —— 牙口自报的「次序反了会偏 ≈24.5px」远大于 1.5 容差）。
  顺手订正 `:6433-6435` 那句「应当逐值相同」（铁律 5）。
- 置信度：**高**

### 34. A517 `Window Title` 真渲染左缘（`rewards.log:23622` · `RewardsScene.cs:6709`）
- 判定：**(α)**（F5，与 #33 同一个 `CheckNear`、同一个根因）
- 证据：期望 `155.00±1.50`、**实得 `156.92`**（+1.92）。
  同 run 里 `edgePx` 那一把量的是 **155.00**（`rewards.log:23529`「✓ ★ A493#6：… `Window Title` 的真渲染左缘 = **155.0**（155.00 ≈ 155.00±1.50）」）⇒ **实现没偏，是尺子换了**。
- 最小改法：同 #33（一处改，两条一起绿）。
- 置信度：**高**

---

## 四、按档汇总

| 档 | 条数 | 明细（本节编号） |
|---|---|---|
| **(α) 断言错** | **11** | 4 · 5 · 6 · 27 · 28 · 29 · 30 · 31 · 32 · 33 · 34 |
| **(β) 实现缺陷** | **23** | 1 · 2 · 3（F2）＋ 7–26 共 20 条（F1；其中 20/21 两条 = ⓐβ ⓑα 双因） |
| **(γ) 本批回归** | **0** | —— 这 34 条全部落在**本批新增的件与新增的断言**上（`BaseOfferPopup` / `PurchasePremiumWindow` 是 untracked 新文件；L1/L2/L4/L3 四段断言是 2026-10-13 新增）⇒ 没有「原来绿、现在红」 |
| **(δ) 夹具前提不成立** | **0** | —— 最接近的是 #6（夹具与断言文案不符），但那一条的**实现是对的**（照原版规则），所以归 α 更准 |

**修完那两处（§一末的 1 / 2），预计 34 条里的 23 条一次转绿**；剩下 11 条 α 各自是一两行的事。

---

## 五、判不了的（如实列）

1. **`Editor/ShopScene.cs:4163`（= 旧编号 `:4086`）的实得坐标** —— 本 run 因 F1 拿了 `null`，**没有读数**。
   静态判读 = 真缺陷（§二），但**「跑一次看它报什么」这一步没法省**（改完 `Build()` 后跑一次，实得值应当落在 `(58.88, 29.63)` 那一档 ⇒ 反证 §二 第 3 步）。
2. **`ppw` 的 `MissingArt` 在「修好 `Build()` 之后」到底是不是 0** —— 依赖 `Resources/Art/` 那一份**被 Git 忽略**的位图被 Unity 正常导入。本 run 判不了（那一段根本没跑到 `Tex()`）。
3. **`Resources/Art/ui_menu/` 那 18 张的来源** —— 我只查到 `mtime = 2026-10-06 12:31`、`.meta` 由 Unity 在 `12:40` 生成；**没有找到写它们的工具**（见 §六·1）。谁拷的、从哪个源拷的，判不了。

---

## 六、顺手发现（⛔ 我没改任何东西）

1. 🔴 **那 18 张手拷图两边导入器都没登记 ⇒ 下次一跑导入器就没了。**
   `Resources/Art/ui_menu/` 下 mtime = `2026-10-06 12:31` 的 18 张：`40k_Crate_Tier{1..5}_*_open` · `40k_UI_Banner_BW` · `40k_topmarquee_currency_{blackstone,crystal,energy,ticket}` · `Glow` · `Laser_Wave_2` · `LightningTrail` · `Noise_Combined` · **`OctagonUI_Filled_SDF`** · `Shine_trail` · **`UI_HIghlight_Internal`** · `Up_Rays`。
   · `工具/import_original_art.py` 本批只加了 **5 条 `40k_Crate_*_open`**（`git diff` 可见），**另外 13 条一条都没登记**；
   · `工具/sync_battle_ui_art.py` 的 `NAMES_MENU` 里也**没有**这 13 个名字（`grep Octagon` / `grep 40k_UI_Banner` 在 `工具/*.py` 里只命中 `OctagonUI Border SDF` 与 `OctagonUI Filled Fade SDF` 两条**别的**图）。
   · 而 `Resources/Art/` **整个目录被 Git 忽略**（`.gitignore:91`）⇒ 只要有人跑一次导入器（或按 `CLAUDE.md` 那条「删目录退回占位美术」），这 13 张就没了、**#4/#5/#28/#29 那四条会静默翻回红**。
   —— 这与 `sync_battle_ui_art.py:240-246` 已记的旧账同形（「2026-10-07 有写手往 `ui_menu/` 手拷进去」「以后往 `ui_menu/` 加图，**只挑一处登记**」）。
2. **`Shell/InboxWindow.cs:339` 的 `Bind(baseQ, null, "40k_general_bt_yellow_hover")` 把常态图名传成 `null`** ——
   后果 = `MissingPressedArt` 里多一条**认不出是谁的** `" → "`（两格都是空）。即使按 #31 的处置（删断言），这条脏记录仍会让下一次「按下图」审计的日志**指不出是哪一颗**。建议顺手给它一个真名字（或让 `Bind` 跳过空 `art` 的那一条）。
3. **`Shell/PurchasePremiumWindow.cs` 里有第二处「相对/绝对混用」**：`BuildContainer` 的 `Sub()`（`:672-673`）以 `ContainerR`（相对根）为基准，于是**模板那一份的子件被多加了整整一个根原点**（`r == Abs(ContainerR)` 时 `Sub(Abs(X)) = X + 2×根原点`）。今天看不出来（模板 `Initialize()` 之后就被关掉、也没有几何断言），但它是 §二 那处修复的**同一笔**。
4. **`Editor/RewardsScene.cs:6433-6435` 的那句注释是错的**（「两条…应当逐值相同」）—— 它是 #33/#34 的**成因**，不止是笔误：它让后来的人以为两把尺子可以互换。建议连同 #33 的最小改法一起订正。
5. **`ShopScene` 那两处 `Check(gop.MissingArt.Count, 1, …)` 与 `:3229` 的日志/`CheckNoMissingSwapArt` 三处口径不同**：同一件事（缺图）在三个地方各写了一遍处理方式。若真要收口，`MissingArt` 的断言应当**统一成「0 才绿」**（图都在工程里了），而不是「恰好缺 1 张」—— 后者每导一张图就要改一次断言。
