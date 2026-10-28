# B93_DG2 · `ShellScene.Run` 16 条红 —— 逐条定性

> **角色**：只读诊断代理 `B93_DG2`（⛔ 未跑 Unity · 未动 git · 未改任何 `.cs` / 账 / 正本）
> **输入**：`d:/4/_tmp_view/shell.log`（现读 `grep -n "✗"` = **16 条**，与派单一致）
> **定性档**：**(α) 断言错 · (β) 实现缺陷 · (γ) 本批回归 · (δ) 夹具前提不成立**
>
> **一句话结论**：**16 条里 0 条是 (β) 实现缺陷** —— **4 条 (α) 断言写错**（期望值/读法错了）·
> **12 条 (δ) 夹具前提没建立**（窗只 `Create(null)` 没 `Open()` / 没给 `WindowsManager` / 事件没给 `dates`）·
> **0 条 (γ)**（本批代码改动没有把原来绿的东西弄红）。
> 全部 16 条的**被测实现都逐句对得上原版判据**（每条下面都给了 `.c` / ILSpy / prefab 的反证）。

---

## ① E12 · 账 415 灭自证 / 灭自证②（2 条）

**失败**：`Priority 9 排在 5 **前面**`（实 `[5,?]`）· `Priority 换成 1 后顺序翻过来`（实 `[5,?]`）
—— `实 [5,?]` 里的 `?` = `menu2.Count == 1`（第二条根本没进表）。

- **定性档**：**δ（夹具前提不成立）** —— 夹具给 `b`（`L_main`）**只翻了 `showInMainMenu`，没给 `dates`**，
  而它是 `activationType = OnDate` ⇒ `EventState = NotAvailable(-1)` ⇒ **被 state∈{1,2,3} 那道闸筛掉**。
- **证据（两侧）**：
  - 断言/夹具：`Unity/MyGame/Assets/CardPresentation/Shell/LiveOpsFilter.cs:1600-1602`
    （`new FixtureEvent("L_main")` + `location=Main` + `priority=9`，**全块没有一处给 `b.D.dates` 赋值**）·
    `:1634`（`b.D.showInMainMenu = true;`）· `:1635-1643`（两条断言的期望 `[9,5]` / `[5,1]`）
  - 夹具 ctor 默认：`LiveOpsFilter.cs:1192`（`activationType = LiveOpsActivationType.OnDate`，`dates` 不赋值 = null）
  - 我们实现：`Shell/LiveOpsManager.cs:557-580`（依次 `ShowInMainMenu` 闸（**:564**）→ `state∈[OnPreview..Finished]` 闸（**:565-566**）→ 取 `MainMenuData` → 稳定降序排序）·
    `Shell/LiveOpsEvent.cs:368-375`（`EventState = GetBaseData().GetState(Now)`）·
    `Shell/LiveOpsEventData`/`LiveOpsEvent.cs:240-252`（`dates == null` ⇒ 出声 + `NotAvailable`）·
    枚举 `LiveOpsEvent.cs:40-48`（`NotAvailable = -1`，**不在 1..3 内**）
  - **日志铁证**（每次调用都刷）：`shell.log` 里紧跟在这条 ✗ 之后 ——
    `[LiveOps] 活动 'L_main' 是 OnDate 但没有 dates ⇒ 原版会 NRE，我们按 NotAvailable 处理`
    （栈 = `LiveOpsManager:GetEventsForDisplayingInMenu (LiveOpsManager.cs:565)` ← `LiveOpsEvent:get_EventState` ← `LiveOpsEventData:GetState`）
  - 原版形状：`d:/2/tools/decomp_full/LiveOpsManager__GetEventsForDisplayingInMenu.c:27-60`
    —— `GetAllEvents` 之后**两道 `Enumerable.Where`**（= `b__19_0` ShowInMainMenu / `b__19_1` state）再 `Select` 再 `OrderByDescending`
- **该改什么**：**改夹具，不改断言口径、不改实现** —— 在 `LiveOpsFilter.cs:1600-1603` 给 `b` 补一段覆盖 `now` 的 `dates`
  （照 `:1596-1600` 里 `a` 那一份写法：`startDate = now-1h` / `endDate = now+1h` / `expirable = false`），
  让它 `EventState = Active(2)`。补完后 `menu2` = `[b(9), a(5)]`、`menu3` = `[a(5), b(1)]`，两条断言原样成立。
  ⚠️ **别改成「把 state 闸放宽」** —— 那会把账 415 的判据改坏。
- **反证（为什么不是 β）**：`menu.Count == 1` 与 `menu[0].Priority == 5` 两条**同一条实现**的断言**已经绿**
  （`LiveOpsFilter.cs:1631-1632`）⇒ 排序/筛选路径本身是通的；红的只有「把第二条塞进表」那一步，
  而它被日志点名的正是 `dates == null`。

---

## ② E12 · 账 162 `GetAssets()[0]` 就是第一个事件的 data 本身（1 条）

**失败**：`assets.Count == 3` **绿**，`assets[0] == a.D` **红**。

- **定性档**：**α（断言错）** —— `GetAssets()` 的次序**不是**夹具传入序 `[a,b,c2]`，
  而是 **`SetupEvents` 的 `OrderByDescending(priority)`** 之后 `[c2(99), b(9), a(5)]` ⇒ `assets[0] == c2.D`。
- **证据（两侧）**：
  - 断言：`LiveOpsFilter.cs:1655`（`assets[0] == a.D`）· 夹具优先级 `:1597-1610`（`a=5` / `b=9` / `c2=99`）
  - 我们实现：`Shell/LiveOpsHandler.cs:264-283` —— `foreach (var e in ops.OrderByDescending(PriorityOf)) events.Add(e);`（**:275**）·
    `Shell/LiveOpsManager.cs:164-175`（`GetAllEvents` = 各 handler 的 `GetBaseEvents` 顺次拼）·
    `Shell/LiveOpsLocalCatalog.cs:1199-1208`（`GetAssets` = `GetAllEvents().Select(GetBaseData())`，逐项保序）
  - 原版判据：`d:/2/tools/decomp_full/LiveOpsHandler._SetupEvents_d__18_object___MoveNext.c`
    （`ops.Cast<TEvent>().OrderByDescending(x => x.GetBaseData().priority)` —— 工程内已有逐句转录 `LiveOpsHandler.cs:251-260`）
  - 同一性判据（`==` 这一半是对的）：`Shell/LiveOpsEvent.cs:749`（`GetBaseData() { return Data; }` ⇒ 同一引用）
- **该改什么**：把断言换成**不依赖夹具输入序**的那条真不变量 ——
  `GetAssets()` 与 `GetAllEvents().Select(e => e.GetBaseData())` **逐项同序同引用**；
  或至少改成 `assets.Count == 3 && assets.Contains(a.D) && ReferenceEquals(assets[0], <GetAllEvents()[0].GetBaseData()>)`。
  ⛔ **别改成「夹具里让 a 优先级最高」** —— 那会让这条断言对「GetAssets 乱序」失去鉴别力。
- **反证（为什么不是 β）**：`assets.Count == 3` 绿 ⇒ 收集路径对；红的是**位置**，
  而位置由 `SetupEvents` 的降序决定，那条降序本身有 `.c` 判据（见上）。

---

## ③ U1 · ⑤ `GetWorldRect` 中心 x / y（2 条）

**失败**：`wr.center.x = 61.603`（期望 `0.000`）· `wr.center.y = 93.301`（期望 `0.000`）；**宽/高两条是绿的**。

- **定性档**：**α（断言错）** —— 原版 `GetWorldRect` 写进 `Rect` 的 **`m_XMin` / `m_YMin` 就是「中心」**
  （不是 Unity 习惯的「左下角」），所以读 `Rect.center` 会**再把半宽半高加一次**。
  我们的实现**逐字照抄了原版这个写法**（这属于「原版自己就这么写」，⛔ 不是我们的设计）。
- **证据（两侧）**：
  - 断言：`Editor/ShellScene.cs:10772-10776`（`:10773` 读 `wr.center.x`、`:10774` 读 `wr.center.y`；
    `:10775-10776` 读 `wr.width/height` —— 这两条绿，正好说明 `w/h` 存的是尺寸）
  - 我们实现：`Core/UguiRect.cs:345-352`（`new Rect(c[0].x + w*0.5f, c[0].y + h*0.5f, w, h)`）
    ＋ **它自己的 doc 就在 :341-343 写着**「字段序一致（**x, y = 中心**；width, height = 尺寸）」
  - 原版判据：`d:/2/tools/decomp_full/RectExtensions__GetWorldRect.c:20-29`
    （`*param_1 = c0.x + w*0.5` · `param_1[1] = c0.y + h*0.5` · `[2]=w` · `[3]=h`）·
    ILSpy `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RectExtensions.cs:246-277`
    （`((Rect*)&rect2)->m_XMin = xMin; m_YMin = yMin; m_Width = …; m_Height = …` —— **中心写进 XMin/YMin**）
  - 原版自己的消费者也是这么读的：`out_cpp2il_dev_ilspy/UICulling.cs:182-200`（比的是 `worldRect.m_XMin`）
  - **数值反证**：探针已按调度台 A 方案归零（`ShellScene.cs:10744`，`anchoredPosition = Vector2.zero`），
    `c0 = (-61.603,-93.301)` / `c2 = (61.603,93.301)` ⇒ `(c0+c2)/2 = 0`；
    实际读到 `61.603 / 93.301` **正好等于 `c2.x / c2.y`** = `x + w` = 「XMin(中心) 再加半宽」——
    这就是 `Rect.center` 的算式，误差不是量出来的偏移。
- **该改什么**：断言读 **`wr.x` / `wr.y`**（= `wr.xMin` / `wr.yMin`），文案改成
  「`GetWorldRect` 的 **x/y 槽 = 中心**（原版写 `m_XMin`，⛔ 不是 Unity 习惯的左下角）」，宽/高两条不动。
  ⚠️ **别改实现去写 `m_XMin = 中心 − 半宽`** —— 那才偏离原版（且 `UICulling` 那条读法也会跟着歪）。
- **反证（为什么不是 β）**：`.c` 与 ILSpy **两份独立源**都写着「中心 → `m_XMin`」，且宽/高两条断言绿。

---

## ④ D1 · ① 前提 `ChangeRankedToggle` 节点存在（1 条）

**失败**：`FindChildIn(rk.transform, "ChangeRankedToggle")` = `null`。

- **定性档**：**δ（夹具前提不成立）** —— 节点名**没错**，是**树根本没建**：
  `RankedEventWindow.Create(null)` 只做 `Init`（挂锚点），**建树在 `Open()` 里**，而 D1 这一段**没有 `Open()`**。
- **证据（两侧）**：
  - 断言：`Editor/ShellScene.cs:10812`（`RankedEventWindow.Create(null)`）· `:10822-10823`（`FindChildIn(...,"ChangeRankedToggle")`）
  - 实现（建树点）：`Shell/RankedEventWindow.cs:66-70`（`Create` = `new GameObject` + `AddComponent` + `Init`）·
    `Shell/LiveOpsEventWindow.cs:538-548`（`Init`：赋 `type/placement/closeOnEsc/Manager` + `AttachToAnchor`，**不 Build**）·
    `Shell/LiveOpsEventWindow.cs:549-580`（`Open()` 里那句 `Build();`（**:564**）← **建树唯一入口**）·
    `LiveOpsEventWindow.cs:585-605`（`Build()` = Backdrop→DeckSelection→ArmySelector→LeftColumn→Header→**ToBattle**→Help→Extras）
  - 节点名核对（**名字是对的**）：`Shell/RankedEventWindow.cs:165`（`MenuDraw.Node(col, "ChangeRankedToggle", …)`）·
    子件 `"Image"` 在 `:187`（`MenuDraw.Rect(tg, …, "Image", …)`）· `"UnrankedText"/"RankedText"` 在 `:170/:177`
  - **同文件反证**：`ShellScene.cs` 里别的窗 fixture **一律 `Create(...)` 之后立刻 `Open()`**
    （`:5811/5817` · `:6442/6446` · `:6508/6512` · `:9316/9330` · `:9360/9381/9394`）；
    而 `MainMenuScene.cs:9881/9937` 能拿到这颗节点，是因为那边的主菜单**真的开过这扇窗**（`RankedEventWindow.LastOpened`）
- **该改什么**：`var rk = RankedEventWindow.Create(shell.Windows); rk.Open();`（`shell` 在 `RunCore` 里可用，
  `:1030` 建、`:1039-1041` 已断三个锚点都在）—— 照 `ShellScene.cs:9316-9317` 的既有范式。
  ⚠️ 同一小段收尾的 `RewardsWindow.DestroySafe(rk.gameObject)` 保留即可。
- **反证（为什么不是 β）**：`（前提）排位窗建出来了` 绿（对象在）、`ToggleKnobArt` 两条纯函数断言绿、
  **而所有需要树的断言同时红** —— 「对象在但树空」只有「没 `Open()`」一种解释；不可能是节点改名（见上面的名字核对）。

---

## ⑤ D1 · ② 三个同名入口（`ChangeDeckButtonClick` / `ViewDeckButtonClick` / `HelpButtonClick`）（3 条）

**失败**：三条 `Last*Popup != null` 全红。

- **定性档**：**δ（夹具前提不成立）** —— 夹具传 `Manager = null`（`Create(null)`），
  实现**按设计**早退并出声；日志里三条警告就是证据。
- **证据（两侧）**：
  - 断言：`Editor/ShellScene.cs:10842`（`Create(null)`）· `:10843-10851`
  - 我们实现（三条同名入口 → 三条链）：`Shell/LiveOpsEventWindow.cs:2302`（`ChangeDeckButtonClick → OpenDeckSelection`）·
    `:2306`（`ViewDeckButtonClick → OpenDeckInfo`）· `:2295`（`HelpButtonClick → OpenHelp`）·
    `:1886-1888`（`OpenDeckSelection`：`if (Manager == null) { LogWarning("…没有 `WindowsManager`，开不了 `Deck Selection Popup`"); return null; }`）·
    `:1930-1933`（`OpenDeckInfo` 同形）· `:1529-1537`（`OpenHelp` 同形）
  - **日志铁证**（`shell.log`，紧跟三条 ✗ 之前各一条）：
    `[Event] 没有 \`WindowsManager\`，开不了 \`Deck Selection Popup\`` ·
    `[Event] 没有 \`WindowsManager\`，开不了 \`Deck info Popup\`` ·
    `[Event] \`Help Button\`：**没有 \`WindowsManager\`**，开不了事件说明弹窗`
  - **同宿主的可用件**：`ShellScene.cs:1030` 的 `shell`（`:1554` 的注释写明「这里有一台**真的** `WindowsManager`（`shell.Windows`）」），
    且 `:1372-1395` 已经在用它开/关窗
- **该改什么**：`RankedEventWindow.Create(shell.Windows)`（而不是 `null`）。
  ⚠️ 若担心残留窗污染后面的断言，收尾补 `shell.Windows.CloseAllWindows()`（`:1339` 已有先例）。
- **反证（为什么不是 β）**：三条 `Open*` 的首句就是 `Manager == null ⇒ 出声 + return null`（不是静默、不是空体），
  而警告文案与 `.c` 那三条原版链（`RankedDeckSelector.ChangeDeckButtonClick` / `ViewDeckButtonClick` / `HelpButtonClick` → `WindowsManager.OpenWindow`）
  的落点一致 ⇒ 实现没写错，只是**没有可用的 Manager**。

---

## ⑥ D1 · ③ `Battle!` 前提 / `Banned card in deck/Text` 前提 / 置假不可点（3 条）

**失败**：`（前提）Battle! 那颗 WindowButton 拿得到` · `（前提）Banned card in deck/Text 在` · `★ 置假 ⇒ Battle! 不可点`。

- **定性档**：**δ（夹具前提不成立）** —— 同一个根因：**`SkirmishEventWindow.Create(null)` 之后没有 `Open()` ⇒ 树是空的**。
  （第三条 `置假 ⇒ 不可点` 是**前提红的级联**：`BattleInteractable` 在「没有按钮」时按实现返回 `true`（已出声），
  于是 `!true = false` 判红 —— 不是「interactable 写不进去」。）
- **证据（两侧）**：
  - 断言：`Editor/ShellScene.cs:10859`（`Create(null)`）· `:10860-10861`（BattleButtonWb 前提）·
    `:10862-10864`（Banned 前提）· `:10870-10874`（置假/置真两条）
  - 我们实现：`Shell/LiveOpsEventWindow.cs:2313-2326`（`BattleButtonWb` 走 `transform.Find("To Battle Button/BattleHit")`，
    建在 `BuildToBattle`：`:1377-1432`，`BattleHit` 在 `:1430`）·
    `:2328`（`BattleInteractable { get { var wb = BattleButtonWb; return wb == null || wb.Interactable; } }` —— 注释已写明「没有按钮 ⇒ true」）·
    `:2333-2352`（`SetBattleInteractable`：`wb == null` ⇒ `LogWarning("[Event] \`Battle!\` 没有 \`WindowButton\` ⇒ \`interactable\` **没写进去**")`）
    · `Shell/SkirmishEventWindow.cs:731`（`MenuDraw.Node(root, "Banned card in deck", …)`）· `:745`（子件名 `"Text"`）· `:748`（建完刷一次）
  - **日志**：`[Event] \`Battle!\` 没有 \`WindowButton\` ⇒ \`interactable\` **没写进去**` **恰好出现 2 次**
    = 夹具自己那两次 `sk.SetBattleInteractable(false/true)`；**建窗时 `BuildExtras → RefreshBannedLine → SetBattleInteractable` 那次没有出现**
    ⇒ 反过来证明 **`Build()` 根本没跑**（不是「跑了但节点被裁掉」）
  - 节点名核对（**名字全对**）：`"To Battle Button"` `:1381` · `"BattleHit"` `:1430` · `"Banned card in deck"` `:731` · `"Text"` `:745`
- **该改什么**：`var sk = SkirmishEventWindow.Create(shell.Windows); sk.Open();`（同 ④⑤）。
  `置假/置真` 两条保持原样即可（它们断的是 `BattleInteractable` 的两态，实现已对照 `.c`：`SkirmishEventWindow__OnDeckSelected.c` 的 `battleButton.interactable = valid`）。
- **反证（为什么不是 β）**：节点名逐个核对无误（上面条条给了行号）；`BattleHit` 的 `WindowButton` 由 `MenuDraw.Hit`
  无条件 `AddComponent<WindowButton>()`（`Shell/MenuDraw.cs:2256`）⇒ 只要节点建了就拿得到；
  而日志显示建窗那一次 `SetBattleInteractable` **没被调到** ⇒ 树没建。

---

## ⑦ D1 · ⑤ `CollectHit` 前提 / 一档都没解锁不可点 / 有一档可领可点（3 条）

**失败**：`（前提）活动期形态建出了 CollectHit` · `一档都没解锁 ⇒ Collect 不可点` · `有一档可领 ⇒ Collect 可点`。

- **定性档**：**δ（夹具前提不成立）** —— 同上：`Create(null)` + **没有 `Open()`** ⇒ 记分条/`Collect` 那颗钮没建。
  （后两条是**前提红的级联**：`wA == null` / `wB == null` 使 `wA != null && !wA.Interactable` 直接假。）
- **证据（两侧）**：
  - 断言：`Editor/ShellScene.cs:10892-10914`（`:10895` `ForceActiveEventForTest = true` · `:10897/:10907` `Create(null)` ·
    `:10898-10900` CollectHit 前提 · `:10901-10904` 不可点 · `:10911-10912` 可点）
  - 我们实现：`Shell/SkirmishEventWindow.cs:567-580` ——
    `var ch = MenuDraw.Hit(collect, "CollectHit", …);`（**:574**）＋
    `if (ch != null) { var cw = ch.GetComponent<WindowButton>(); if (cw != null) cw.Interactable = HasMilestoneToCollect(ForceMilestoneStatesForTest); }`（**:578**）
    ⇒ **判据接线本身是对的**（正是断言想锁的那一句）；它属于 `BuildScoreBar` → `BuildLeftColumn`（活动期形态），
    整条链只在 `Build()`/`Open()` 里跑
  - 形态闸：`Shell/SkirmishEventWindow.cs:182/187-191/210-218`（`ForceActiveEventForTest` 覆盖 `IsActiveEvent`）
- **该改什么**：`Create(shell.Windows)` + `Open()`（⑥同）。
  两条 interactable 断言**原样保留** —— 它们正是这条接线的灭自证。
- **反证（为什么不是 β）**：`:578` 那一行就是断言要锁的对象，写法与断言期望逐字一致；
  纯函数那三条（`MilestoneStates` / `HasMilestoneToCollect`）在同一段里**已经绿**（`ShellScene.cs:10878-10887`）
  ⇒ 逻辑侧没问题，缺的只是节点。

---

## ⑧ D4 · `ignoreDuplicates = true ⇒ 每条算 1 ⇒ 2`（1 条，实得 7）

**失败**：第二趟读到 `last == 7`（期望 2）；而「灭自证：与第一趟结果不同」那条**假绿**（7 ≠ 5 ≠ 2）。

- **定性档**：**α（断言错）** —— `ProgressSink` 拿到的**不是「本次触发算出来的和」**，而是
  **`UpdateProgress` 之后的累计值**；`shouldOverride = false` 的那一支按原版**累加**：
  第一趟 `0 + 5 = 5`，第二趟 `5 + 2 = 7`。**7 正是正确答案**。
- **证据（两侧）**：
  - 断言：`Editor/ShellScene_B93D4SelfCheck.cs:111-118`（同一个 `rc` 实例连跑两趟，第二趟直接断 `last == 2`）
  - 我们实现：`Shell/RewardCollected.cs:78-87` ——
    `int next = shouldOverride ? value : Progress + value;`（**:81**）· `if (sum != 0) UpdateProgress(sum, false);`（**:140**，`OnTrigger` 传的正是 `shouldOverride:false`）
    · 求和那半是对的：`:138` `sum += ignoreDuplicates ? 1 : filtered[i].Quantity`
  - 原版判据（**累加是原版语义，不是我们编的**）：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/MissionChallenge.cs:337-360`
    —— `if (shouldOverride) currentValue = value; else currentValue = value + progress.CurrentValue;`（**逐句**）
  - **数值反证**：`5`（第一趟绿）+ `2` ⇒ `7`；如果实现错成「恒按数量求和」第二趟会给 5（而不是 7），
    错成「恒按条数」第一趟会给 2（而第一趟是绿的）⇒ 7 唯一对应「累计 + 本趟 2」。
- **该改什么**：第二趟**换一个新实例**（`var rc2 = new RewardCollected { ignoreDuplicates = true };`
  —— `Progress` 是实例字段 `RewardCollected.cs:72`，`ResetForTest()` 只清 `ProgressSink`/警告闩、**不动实例进度**，见 `:341`），
  或改成断增量 `last2 - last1 == 2`。⛔ **别改 `UpdateProgress` 去「覆盖」** —— 那会偏离原版。
  ⚠️ 顺带修「灭自证：`last != 5`」那条 —— 它今天靠 7≠5 假绿，换成新实例后应改成「`lastB != lastA`（2 vs 5）」。
- **反证（为什么不是 β）**：原版 `UpdateProgress` 的 `.c`/ILSpy 两支写法都是累加；且 `OnTrigger` 传的 `shouldOverride = false` 一句在 `RewardCollected.cs:141` 明写。

---

## 汇总

| # | 批次 | 断言 | 档 | 根因（一句话） | 改哪一侧 |
|---|---|---|---|---|---|
| ① | E12 | 账415 灭自证 ×2 | **δ** | 夹具 `L_main` 没给 `dates` ⇒ `NotAvailable` 被 state 闸筛掉 | 夹具（补 `dates`） |
| ② | E12 | 账162 `GetAssets()[0]` | **α** | 排序是 `SetupEvents` 的 **Priority 降序**，不是传入序 | 断言 |
| ③ | U1 | ⑤ `GetWorldRect` 中心 x/y ×2 | **α** | 原版把**中心**写进 `m_XMin/m_YMin`，断言读 `Rect.center` 又加了半宽 | 断言（读 `wr.x/wr.y`） |
| ④ | D1 | ① `ChangeRankedToggle` 前提 | **δ** | `Create(null)` 后**没 `Open()`** ⇒ 树空 | 夹具（`shell.Windows` + `Open()`） |
| ⑤ | D1 | ② 三个入口 ×3 | **δ** | `Manager == null` ⇒ 三条 `Open*` 按设计早退+出声 | 夹具（传 `shell.Windows`） |
| ⑥ | D1 | ③ Battle 前提 / Banned 前提 / 置假 ×3 | **δ** | 同④（树空）+ 前提红级联 | 夹具（+ `Open()`） |
| ⑦ | D1 | ⑤ Collect 前提 + 两态 ×3 | **δ** | 同④（树空）+ 前提红级联 | 夹具（+ `Open()`） |
| ⑧ | D4 | `ignoreDuplicates` 求和 | **α** | 读到的是**累计进度**（5+2=7），原版 `shouldOverride=false` 就是累加 | 断言（换新实例 / 断增量） |

- **合计**：16 条 = **α 4 条**（② 1 + ③ 2 + ⑧ 1）· **δ 12 条**（① 2 + ④ 1 + ⑤ 3 + ⑥ 3 + ⑦ 3）· **β 0** · **γ 0**。
- **按根因去重 = 5 个**：E12 夹具缺 `dates` · E12 断言依赖传入序 · U1 读 `Rect.center` · D1 十项同根（`Create(null)` 且无 `Open()`）· D4 累计进度。
- **本批代码（E12 / U1 / D1 / D4 的实现侧）未发现需要改的地方** —— 每条都给了原版判据的反证。

## 修复顺序建议

1. **D1（10 条，一次改完收益最大）**：把 `ShellScene.cs:10812/10842/10859/10897/10907` 五处
   `Create(null)` 换成 `Create(shell.Windows)`，并在 `Create` 之后补 `Open()`；收尾补 `shell.Windows.CloseAllWindows()` 防残留。
   —— 这一处修完，④⑤⑥⑦ 四组 10 条**一起验**（都是同一个根因）。
2. **U1 两条**（`ShellScene.cs:10773-10774` 改读 `wr.x / wr.y`）—— 一行改动，风险最低，顺手可做。
3. **E12 账162**（`LiveOpsFilter.cs:1655` 改成「与 `GetAllEvents().Select(GetBaseData())` 逐项同序同引用」）。
4. **E12 账415 两条**（`LiveOpsFilter.cs:1600-1603` 给 `b` 补 `dates`）。
5. **D4 一条**（`ShellScene_B93D4SelfCheck.cs:111-118` 第二趟换新实例 + 修那条假绿的灭自证）。
   —— **②③⑧ 是断言侧**，可以交给同一个写手一次改完（三处互不相邻）；**①④⑤⑥⑦ 是夹具侧**，改完必须重跑 `ShellScene.Run` 才知真假。

## 没查清

1. **D1 补 `Open()` 之后的真实结果没验**（⛔ 我没跑 Unity）—— 我只核到「节点名对、建树在 `Open()` 里、同文件别处也是这么做的」。
   潜在风险两处（都需重跑才知道）：① `RankedEventWindow.Open()` 会顺手 `MainMenuRuntime.SetNavPanelVisible(false,true)`
   与 `SearchingMatchPopup.Attach/ScheduleExpiryRefresh()`，可能给后续批次留状态；② ⑤ 里 `ForceActiveEventForTest=true` 时
   `BuildScoreBar` 会取 `Crate_Border_Highlight` 等图，若 `CardArt` 缓存里没有会进 `MissingArt`（同一宿主别的断言可能因此红/绿）。
2. **`OpenDeckSelection` / `OpenDeckInfo` / `OpenHelp` 在 `shell.Windows` 下能否真的开出那三扇窗**：
   我核到锚点已注册（`ShellScene.cs:1039-1041`）、三条链的实现完整，但**没有实测**；若 `DeckSelectionPopup.Create`
   在批处理里另需卡组数据，可能仍拿不到 `LastDeckSelection`（那时就要判「断言该改成验 `Open*` 的返回值/分支」）。
   —— 这一条是**唯一可能把 ⑤ 从 δ 翻成 α 的地方**，重跑时优先看它。
3. **`RunCore` 里 `shell` 变量在 10812 行那一带的可见性**：我按 `":1030 var shell = Build(...)"` 与 `:1334` 的用法推断
   **同一方法作用域内可用**（未逐行确认 `RunCore` 中途没有换作用域/遮蔽）—— 动手前请现读一眼。
4. **E12 的两条 `灭自证` 若补了 `dates` 之后会不会与「`L_planned` 是 Planned」那条前提冲突**：按我的推演不会
   （`c2` 的 `dates` 在 `now+1h` 之后 ⇒ 仍是 `Planned(0)`），但**未实跑**。
5. **本报告未覆盖**：这批 16 条之外，`shell.log` 里是否还有别的「❌ 但不带 `✗` 前缀」的失败形态（如异常/`LogError` 非断言）
   —— 只按派单口径核了 16 条 `✗`。
