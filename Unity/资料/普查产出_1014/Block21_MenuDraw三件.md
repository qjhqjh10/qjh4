# Block21 · `MenuDraw` 三件（A798 · A796 · A797）

> 写手代理 Block21（2026-10-14）· **独占三个文件**：`Shell/MenuDraw.cs` · `Shell/PurchasePremiumWindow.cs` · `Shell/CampaignRewardWindow.cs`
> 判据 = `资料/普查产出_1014/RO_文字半边与压暗层.md`（逐字读过）+ `资料/普查产出_1013/批次计划_1013.md` §六·B 的 A798/A796/A797。
> ⛔ 本件**没跑 Unity** · **没动 git** · **没改正本 / 没改别的 `.cs`**；行号是**改完这一批之后**的。

---

## 〇 · 总表

| 账 | 改了什么（文件:行） | 判据 | 做完没有 |
|---|---|---|---|
| **A798** | `Shell/MenuDraw.cs:1597`（`Text` 入口）+ `:1656`（`TextBox` 入口）各加一行闸；两处 doc 就地订正（`:1563` / `:1640`） | RO §一 · A798「最小改法」 | ✅ **代码侧做完**；⛔ **断言侧没做**（白名单外，见 §五·1） |
| **A796** | `Shell/MenuDraw.cs:2183–2251` 新增 `CheckShadeClickRule(…)`（= RO 推荐落点 (a) 的**新口**） | RO §三 · (a) | ✅ **新口做完**；⛔ **24 个宿主的调用点一个没接**（简报明令「本件只做新口本身」） |
| **A797** | `Shell/PurchasePremiumWindow.cs:595–608`（那段「仍然如实标一处缺口」就地订正）· `Shell/CampaignRewardWindow.cs:873–879`（同一句过期的**第二处**补一笔） | 铁律 5 · A797 | ✅ 两处都订正、都留了更正痕迹 |

---

## 一 · A798 —— `MenuDraw.Text` / `TextBox` 不做「整块在框外 ⇒ 不建」

**改了什么**

1. `Shell/MenuDraw.cs:1597`（`Text` 的 `{` 之后、`TextCore` 调用**之前**）：
   ```csharp
   if (!Visible(r, _st.RenderClip)) return null;
   ```
2. `Shell/MenuDraw.cs:1656`（`TextBox` 同位置）。⇒ **两个入口各一行**，与 RO 的「最小改法」逐字一致。
3. `Shell/MenuDraw.cs:1563–1576` **就地订正**：那段原来写「本方法**不做**那道闸 …… 那一条差异**仍然开着**（另立账），⛔ 别在这儿顺手加」——**已过期**（这一批就把它加了）。订正里逐条核了它当时给的两条理由：
   - ① 「返回契约会从『总有标签』变成『可能 null』」——**契约本来就是「可能 null」**（同函数下一句 `if (lb == null) return null` 早在）；RO 的普查 = **199 个调用点**（`Text` 146 / `TextBox` 53）· **141 个赋值点里 `0` 处**在解引用前无守卫 ⇒ **改 0 个调用点**。
   - ② 「只裁不建在画面上等价」——**这条是对的** ⇒ **语义代价 = 0 画面差**；收益 = 与同族四个静态件（`Rect` / `Nine` / `Tiled` / `Hit`）一致。
4. `Shell/MenuDraw.cs:1640`：`TextBox` 里那句「以及**为什么不做**『整块在外 ⇒ 不建』那道闸 → `Text` 的注释」改成「那道闸（**A798 起两个入口都有**）」。

**两条刻意的选择（都写进注释了）**

- ⛔ **没加进 `TextCore`**：内层被两个入口共用（RO 点名），且 `TextBox` 的重排（`SetWrapWidth` / `SetAutoFitBox`）在闸之后 —— 闸判的是**调用方给的 `r`**（重排不改 `r`）⇒ 判一次就够。
- ⛔ **用 `Visible` 而不是 `ClipRect`**：`ClipRect` 多一条「退化矩形（宽/高 ≤ 0.01）在有裁切时一律不可见」的守卫，那是**图**那一路的（那种尺寸建不出 quad）；文字建得出来 —— 这正是 `Visible` 的注释里写的分工。⇒ 那条守卫**没有**被引进来（收口不许顺手改行为）。
- 位置在**建节点之前**：建完再 `return null` = 节点留在树里没人管（漏节点）。

**做完没有**：代码 ✅。断言 ⛔ —— 见 §五·1。

---

## 二 · A796 —— 压暗层「点了会不会关」的新口

**改了什么**：`Shell/MenuDraw.cs:2183–2251` 新增

```csharp
public static void CheckShadeClickRule(MenuCheck chk, string what, Transform winRoot,
                                       Transform darkHit, System.Func<WindowState> state)
```

（签名 = RO 推荐落点 (a) 的**原样**；`MenuCheck` 是既有的 `delegate void MenuCheck(bool, string)`，各宿主直接传 `CheckTrue` 方法组。）

**它断几条**（4 个 `chk`，其中两条是**互为对照**的行为断言）

| # | 断什么 | 性质 |
|---|---|---|
| ⓪ | `winRoot` / `darkHit` / `state` **三样都不是 null**（缺哪样报哪样） | 前置；不成立 ⇒ **报红并早退**（⛔ 不静默） |
| ① | `darkHit.IsChildOf(winRoot)` —— 这颗命中区**属于这一扇窗** | 前置；**报红但不早退**（下面照跑） |
| ② | 那颗上有 `WindowButton`（缺 ⇒ 报红早退）**且 `!absorbOnly`** | **结构**（灭自证那条，见下） |
| ③ | 点**之前** `state() == WindowState.Open` | **负向态**；不成立 ⇒ 早退（⛔ 不硬点） |
| ④ | `wb.Click()` 之后 `state() == WindowState.Closed` | **正向态** —— 走 `WindowButton.Click()` = `PointerLayer` 唯一的派发口（`Shell/PromptPopup.cs`） |

**判别力（改坏法，按 RO 的设计，我逐条对过）**

- 把实现里 `MenuDraw.ShadeHit(…, () => Close())` 换成 `MenuDraw.Absorb(…)`（或给那颗 `WindowButton` 置 `absorbOnly`）⇒ `Click()` 在 `PromptPopup.cs:1100` **第一句就早退** ⇒ **④必红**。
- **「两边一起改」也躲不掉**：若有人把实现换成吸收层、再顺手把 `Click()` 里那句 `absorbOnly` 早退删掉，③④ 会一起变绿 —— 而 **②「不是吸收层」照样红**（它与「实现是吸收层」**结构上不可能同时满足**，`CLAUDE.md` §三「灭自证」那一族）。
- 若那颗的 `onClick` 被换成空动作 / 换成别的窗的动作 ⇒ ④红。
- 若那颗 `WindowButton` 被整颗删掉 ⇒ ②红并早退（并说明「点了什么都不会发生」）。

**它【不】答什么**（写进注释了，⛔ 免得下一批误当它盖住了）：几何 / 档 = `CheckShadeRule` 的职责；**屏幕坐标**点得到吗（真路径 `PointerLayer.ClickAt`）= `CheckAbsorbRule` 的职责。本口走**派发口直调** ⇒ 不看坐标、不看遮挡（那是有意的 —— 否则每扇窗都得先裁一个钉死的点）。

**做完没有**：新口 ✅。**24 个宿主的调用点一个都没接**（`Editor/{CollectionScene,MainMenuScene,RewardsScene,SettingsScene,ShopScene}.cs` —— 简报明令本件只做口本身，且那些是热点文件）⇒ 见 §五·2。

---

## 三 · A797 —— 两处过期注释（铁律 5）

**① `Shell/PurchasePremiumWindow.cs:595–608`（原 `:585-589`，行号已漂）**
原文（我**引原文再订正**，痕迹保留）：「⚠️ **仍然如实标一处缺口**（报告 §九③，本件没修）：`MenuDraw.Text` / `MenuDraw.TextBox` **没有 `clip` 形参、也不走 `ViewportClip.Resolve`** ⇒ 视口里那条文字（`Army Name` / `Premium Text`）**不吃裁切** …… 要真修得动 `Shell/MenuDraw.cs` 那两个共用件（另立账）」。
**订正**：**错因 = 它写于 A781 之前，那一刻是真的**；**A781（2026-10-13）当天就把那两个共用件修了**（「另立的那笔账」正是 A781），但**没人回来销这一句**。
**现在的事实**（我逐行核过父链）：`MenuDraw.Text` / `TextBox` 各带 `clip` / `clipSoftness` 两个**可选**形参、走**同一份** `ViewportClip.Resolve`，**并且 A798 起**多一道「整块在框外 ⇒ 连节点一起不建」的闸；⇒ 滚动的那些容器里的两条文字（`Army Name`(`BuildContainer:698`) / `Premium Text`(`BuildContainer:711`)）的父链经 `_content`(`:517`，挂在 `vpVc.transform`) 上行到 `Build()` 里那句 `ViewportClip.Hang`(`:512`) ⇒ **已经吃裁切**。
⚠️ 同时写明**别读成「本窗文字全在视口里」**：`Army Info` 那一棵（`Build()` 里的 `BuildArmyInfo(root)`，`:494`）直接挂在**窗根**下、不在任何视口里 ⇒ 它不裁（照旧，那是对的）。

**② `Shell/CampaignRewardWindow.cs:873–879`（原 `:867` 一带）**
那一处 2026-10-11 已有一次 A302 的订正，但它只覆盖了「A302 **绕开**了『`MenuDraw.Text` 没有裁切形参』」这**一半**。补一笔：**A781（2026-10-13）把形参本身补上了**（+ A798 的闸）⇒ 原句「`MenuDraw.Text` 没有裁切形参」**双料过期**；本窗文字仍走 `ItemDrawer.ClippedText`（它自己那层收口没变），但**判据的出处**从此是那两个共用件，⛔ 别再照抄那句。

**做完没有**：✅ 两处都订正、都带日期 + 原文 + 错因（铁律 5 的「保留更正痕迹」）。

---

## 四 · 牵动面（`Shell/MenuDraw.cs` 是共用件 —— 全壳都走它）

`MenuDraw.Text` / `TextBox` 共 **199 个调用点 / 45 个文件**。新闸**只有在「解析出的裁切框非空」时才可能改变行为**：

1. **无裁切 ⇒ 逐位不变（绝大多数）**：`clip == null` 且父链上没有 `ViewportClip` ⇒ `_st.RenderClip == null`（`PaddedClip(null, ·)` 首句早退）⇒ `Visible` 首句 `return true` ⇒ **一个字都不变**。
2. **父链上有 `ViewportClip` 节点的调用点 = RO 查出的 8 处**（A799：3 处账上已点名 + 5 处新查出）：
   `PurchasePremiumWindow` 的 `Army Name` / `Premium Text`（都在 `BuildContainer`）· `RankedRewardEventWindow.cs:530`(`Army Text`) · `LeaderboardRow.cs:163/206/213/223` · `MatchLogRow.cs:153`。
   这 8 处**今天就已经被 A781 裁**（那是 A781 的账），新闸只把「整块在框外」这一格从**「建一个被夹成零面积的 label」**换成**「连节点都不建」** ⇒ **0 画面差**；但这些点从此**可能拿到 `null`** —— 拿 `transform` 当父件的写法会落到**兜底分支**（**层级 / 节点名会变**，RO 记的那处 = `DailyStreakPopup`；那些点都有守卫，不炸）。
   其中三类的宿主**已经先按整行挡过一次**（`LeaderboardWindow.cs:737` 与 `BattleLogPopup` 的 `MenuScroll.Intersects(rr)`、`RankedRewardEventWindow.cs:500`）⇒ 新闸再咬得到的只剩「行与视口有交集、但行内某条文字**整条**落在框外」这种窄带，同样是 0 画面差。
3. **显式传 `clip` 的调用点**：全仓只有 `Editor/MainMenuScene.cs` 的 A781 夹具态四那两条（`:8765` / `:8772`），**我逐行核过它不会红** —— 夹具三块矩形 `a781Cell(0,0,600,60)` · `a781Vp(0,0,120,60)` · `a781Ovr(0,0,300,60)` **两两有交集**（两轴都比过）⇒ 新闸一律 `return true`，态一/二/三/四 的实测值一个都不变。
4. **双层闸的过渡态（新查出的一处细节，只记不改）**：`Shell/MenuWindowBase.cs:338-343` 那条包装**自己已有一道同样判据**、再转调 `MenuDraw.TextBox` —— 但它**没把 `RenderClip` / `ClipSoftness` 传下去**（`:340` 只传前十个参数）⇒ **外层按「本窗字段」判、内层按「父链节点」判**。两者只在「窗字段与节点**同时**存在」的迁移过渡态下可能不同，而那正是 `ViewportClip.NodeShadowedByParam` 那个**进度计数**盯的状态；生产代码今天**不派生**喂 `st.Clip`（A435 甲的派生写入已删，见 `Shell/ItemDrawer.cs` 那段注释）⇒ **不构成新风险**，记一笔备查。

---

## 五 · 没做完的（+为什么）

1. **A798 的断言**（RO 建议落 `Editor/MainMenuScene.cs` 的 A781 段旁）：**本件白名单外**（简报点名 `Editor/{ShellScene,CollectionScene}.cs` 有别的写手，`MainMenuScene.cs` 同为多写手共用的热点宿主）⇒ **没做**。
   给下一批的现成夹具（我实读过，**不要改**现有那三块 —— 它们是「图/字切在同一条边界」的对照组）：`MainMenuScene.cs:8684-8687` 的 `a781` 探针根（**不在任何窗的父链上**）+ `a781Vp(0,0,120,60)` / `a781Cell(0,0,600,60)` / `a781Ovr(0,0,300,60)`；新加一块**整块在框外**的格子（把 cell 挪到 `x1 > vp.x2`，或高度 0）即可断「返回 `null` + 那个节点不存在」。**改坏法**：把那一行闸删掉 ⇒ 返回非 null ⇒ 红。
2. **A796 的 24 个调用点**：简报明令「**本件只做「新口本身」**」⇒ 一个都没接（⛔ 那些宿主文件不是我的白名单）。接法写在新口注释里：`MenuDraw.CheckShadeClickRule(CheckTrue, "<窗名>", <窗根>, <压暗层命中区>, () => <窗>.CurrentState);`
   ⚠️ 下一批要注意：多数调用点传的是 `FindChild(pop.transform, "CloseHit")` 这种**再找一次**的节点（`ShadeHit` 的返回值多数没存字段）—— `winRoot` 取**那一扇窗的根**即可；`MainMenuScene` 里有**嵌套窗**（`SkirmishEventWindow` 里嵌 `Searching Oponent Popup`）⇒ 传错根会在 ①「属于这一扇」红（**非致命**，③④照跑）。
3. **`CheckAbsorbRule` 的 5 份宿主副本**（`CollectionScene.cs:78` / `MainMenuScene.cs:73` / `RewardsScene.cs:191` / `SettingsScene.cs:100` / `ShopScene.cs:837`）该收进 `MenuDraw`：RO 记为「**另一笔账**、A796 里只记不做」⇒ 我没动。

---

## 六 · 顺手发现（只报不改，⛔ 都不是我的白名单）

1. **同一句过期话还有两处活的**（我只改了 A797 点名的那两处）：
   - `Shell/ItemDrawer.cs:186`：「……仍不吃裁切** —— `MenuDraw.Text` 没有裁切形参，而本库没走 `MenuDraw.ClipText`（那是**另一件**欠账）」。
   - `Shell/ItemDrawer.cs:1242`（`ClippedText` 的类注释里）：「三层文字（阵营名 / 短名 / 数量）走的是裸 `MenuDraw.Text` ⇒ **图被裁、字照画**」—— 而**同一段**的 `:1248` 已有 A435 甲的订正（「守卫改成沿父链解析」）⇒ **一段之内自相矛盾**（同 A797 的情形，但不在我的白名单里）。
2. `Editor/RewardsScene.cs` 的 A302 探针仍靠 `ItemDrawerStyle.Clip` 这个**显式覆盖**口子（`CampaignRewardWindow.cs:881` 那句「唯一还用它的是……A302 探针」）—— 也就是说那条自检**吃的是形参那一支、不是父链节点那一支**；若哪天有人把 `st.Clip` 那个字段删了/改成别的语义，这条探针会**静默**失去覆盖。只报。
3. `Shell/MenuWindowBase.cs:340` 转调时没把 clip 形参传下去（详见 §四·4）。

---

## 七 · 自检

- **秒级类型检查**（`TMPDIR=/tmp/wf_b21 bash d:/4/Unity/工具/typecheck.sh`）：
  - 第 1 次：**运行时 1 个错** —— `Assets\CardPresentation\Shell\ChatPanel.cs(627,44): error CS0103: 当前上下文中不存在名称"vpR"` ⇒ **全部落在不是我负责的文件上**（`Shell/ChatPanel.cs` 是别的在飞写手的活，简报里就点了名）⇒ 按纪律**没去改它**；
  - 等 45 秒**重跑**：**运行时 0 / 编辑器 0（全绿）** ⇒ 我这一批三个文件编得过。
- ⛔ **没跑 Unity**（简报红线）· ⛔ **没动 git** · ⛔ **没改正本**。
- **行尾**：三个文件**改前改后都是纯 LF**（`CRLF=0`，逐个用二进制数过），全程用 Edit 工具。
- `git diff --numstat`（对 HEAD）：`MenuDraw.cs` **+109/−5** · `CampaignRewardWindow.cs` **+6/−0** · `PurchasePremiumWindow.cs` **+38/−8**（⚠️ 后者含**主对话今天已改的** A803(`Build();`) / A806(绝对档 ×2) / 805 同族的 `Abs(…)` —— 我这一处只是那段 `<para>` doc，**已逐行核过没碰它们**）。
