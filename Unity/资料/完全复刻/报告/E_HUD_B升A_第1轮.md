# E · 战斗界面 / HUD —— B 升 A 第 1 轮（2026-10-25）

> 范围：`资料/完全复刻/账/战斗界面_HUD.tsv` 里 **101 条 B**（66 类 / 679 条中的那一批）。
> 判据：🥇 `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/**` → 🥈 `D:/2/tools/decomp_full/**.c` → 🥉 `assets_full` 字段（只在判「死类/零实例」时用）。
> 做法：**逐条核原版方法体 → 找我们的实现 → A / C+等级 / B（写清缺什么）**。101 条按类切 4 块并行核，**关键结论我抽查过**（见 §⑤）。
> 本轮**没跑 Unity**；typecheck 见 §① 末（属于本代理的文件 0/0；个别轮次编辑器程序集的红点在 `Editor/ShellScene.cs`，是别的代理正在改的）。

---

## ① 结论（数字）

| 项 | 数 |
|---|---:|
| 本批要核的 B | **101** |
| → **A 已对齐** | **69** |
| → **C 该做没做** | **30**（**P1 × 5 · P2 × 25**） |
| → **B 仍待核** | **2**（都是「判据在服务端」） |
| 顺带解掉账里最后 1 条 **E** | `CardDisplayWindow.CardUIClicked` → **A**（`decomp_full` 有方法体，B 报的 ILSpy `Invalid jump target` 只是 Cpp2IL 的问题） |

**账现状（战斗界面/HUD）**：**A 415 · C 46（P0 4 / P1 10 / P2 32）· B 22 · D 196 · E 0 = 679**。
（2026-10-25 晚二改：按 `G_A档审计_跨账.md` 收紧 A 的落点 ⇒ 10 条降 B；`SetTurnPlayedCards` 现核已实现 ⇒ C→A；
按调度台 P0 口径复核 ⇒ 17 条升 P0、时钟三行统一 P1。**§⑧ §⑨ §⑩ 是这一轮的增补**；
**晚三改**：光标 448-450 → D、`SetupDoor` 455 → A（A 已实现）、`UpdateBastionr` 167 → D（原版空转）、
`TipPrebattle` 10 条 → B（件已建、待接线）、选牌倒计时 366/369/375 面板侧做完 ⇒ **现状见 §⑪**。）

**本轮 5 条 P1 的去向（详见 §⑧）**：
1. `ClockManager.PauseForAnim/UnpauseForAnim`（388/401/414）—— **P1 统一**：逐句读 `ClockManager` 后确认该标记**只影响 15 s 倒计时**、不影响回合表（§8.1），因此**不满足「每局每回合」**；补丁见 §8.4①；
2. 178 / 185 `BattleErrorUIManager` 本机掉线分支 —— **P1**（只在联机掉线时可见）；
3. 182 `OpponentDesyncCancelMatch` —— **P1**（只在指纹不一致时可见）；
4. 258 `BattleSettingsWindow.Open` —— **面板侧已做完**（`SettingsPanel.SetMatchType/SkipWanted/ResignTermNow` + `BattleScene` 4 条断言），**剩驱动传 `matchType`（A 域一行）**。

**三件顺手落地的代码/口径**（全在 `Battle/**` + `Core/**`，见 §③）：
- **`Tooltip.ShowAtMouse`**（Core）：原版 `GetTooltipPosition/Show` = `ScreenToWorldPoint(mouse.x, mouse.y **+10**, text.z)`；
- **`CardView.TooltipsEnabled`**（逐卡开关）+ `CardDisplayWindow` 两处设置点（原版 `:965 index==0` / `:1181 false`、`:1184 true`）；
- 上一批的 **`Core/Tooltip` 抑制计数**（按 owner、重复 Release 安全）已带上断言。

---

## ② 判据（`文件:行号`）

### 2.1 「原版发行版是死代码」的一族（**以后别再当缺口**）

判据 = **全量反编译 0 调用点** + **`assets_full` 全量 25 万 JSON 里独有序列化字段 0 命中 / 只剩 MonoScript**：

| 类 | 证据 | 我们的等价物 |
|---|---|---|
| `CemeteryLogGroup`（5 条：319-323） | `grep '"targetCard1"' / '"centralCemetery"'` 全 0；只剩 `MonoScript_2187` | 活路是 `BattleDriver.cs:11612 TickLogCard`（一行字 + 单张悬停卡） |
| `HoverManager`（7 条：511-517） | `"miniTipAsset"` / `"hightLight"` / `"noTipsModeOn"` 全 0 | `Core/Tooltip`（全局悬停）+ 按 owner 抑制 |
| `BattleScoreIcon.Config/Check`（234/235） | 全量反编译 0 调用；运行期 UI 树只有 `MatchSkulls Icon/Score` 两件 | `BattleDriver.cs:30428`（单骷髅 + `x{n}`） |
| `BattleCardUI.SetVanguardStealth` 等（145/155/158/161） | `vanguardFrame(+0x278)` 全 C# 零写入、`stealthPrefab/reflectedPrefab` 序列化为空 | 无框可画 ⇒ 等价 |
| `BasicCardUI.SetCardRarityStars` / `SetupCollider` / `OnDescriptionShow`（41/53/58/65） | private/零调用/零 persistent call | 无星形层 / 手搓命中 / 不展开描述 |
| `CardDisplayWindow.TryInitializeCard`（312） | 全库零调用；真路径 `InitializeRelatedCards` | `CardDisplayWindow.cs:1406` 同形 |

### 2.2 真缺口（C）—— 每条都能指出「缺的那一件」

- **P1**：389 时钟 `PauseForAnim/UnpauseForAnim`（`ClockManager.cs:342/347` + `Update.c` 的 `timeInAnimation` 扣减）；178/185 本机掉线分支（`ShowDisconnectionPopUp.c:20-53`）；182 `CancelMatch(MatchWon)`（`OpponentDesyncCancelMatch.c:33-50`）；258 Skip 钮显隐 + 回放文案（`BattleSettingsWindow__Open.c:41-121`）。
- **P2（25 条）**分五族：
  · 技能面板（5/15/20）：拖拽不亮 acting 层、目标数应拼进 descText；
  · 卡面/展示窗（43/92/108/153）：抬牌态不该开窗、**战斗侧异画**、**传说档溶解材质**、残骸里露出原卡（`ShouldShowOriginalRemnantCard`）；
  · 日志/选牌（339/364/378）：陷阱 drawTrap 行（引擎无陷阱）、回放选牌面板 2 s 自动确认、`EndSelectionWithDelay` 等；
  · 时钟（397/409/412/413）：教程倒计时 10 s（我们写死 15）、回放 `Battle/HUD/Replay` 文案、StopClock 销毁 countdownAnim；
  · 其它（189 换语言重刷整棵 HUD、195 抽牌终点锚、456 结算收窗、537/544/548 存档能量显示 + 任务点弹跳、573/582 练习窗阵营高亮/换卡组重筛、584 SaveSelectedDeck）。
- **2.3 两条 B（判据在服务端）**：570（`RankedV3Event` 活动存在与否 ⇒ `gameModeSelector` 显隐）、581（`FeatureConfig.IsArmyHidden` + `GetActiveEvent(mode).GetEventDeck()`）。

---

## ③ 改动清单

| 文件 | 改什么 | 判据 |
|---|---|---|
| `Core/CardView.cs` | 新增 **`public bool TooltipsEnabled = true`**（逐卡开关；与「全局抑制计数」分工） | 原版 `Card2DController.ToggleTooltips`：`CardDisplayWindow.cs:965`（只前台开）/`:1181,1184`（换位旧关新开）/`BattleCardManager:128` |
| `Core/Tooltip.cs` | 新增 **`ShowAtMouse(body, mouseWorld, textWorldZ, pxToWorld, anchor)`** + `MouseTipDyPx = 10f` | `decomp_full/TextTooltipController__Show.c`：`ScreenToWorldPoint(mouse.x, mouse.y + 10, text.z)`（`DAT_1834b2da8 = 10.0f`）；`EverguildTraitTooltipData` 第一参 = Position |
| `Battle/CardDisplayWindow.cs` | `BuildFan`：`v.TooltipsEnabled = (i == 0)`；`SwapToFront`：旧前台 `false` / 新前台 `true`（**在两条补间之前**，与原版同位置） | `CardDisplayWindow.cs:965` / `:1181`+`:1184` |
| （上一批）`Core/Tooltip.cs` | 按 owner 的抑制计数 `SuppressTooltips/ReleaseTooltips`（正在显示的立刻淡出） | 原版 6 个 `ToggleTooltips` 调用点（BattleCardManager/CardDisplayWindow/CardInBoosterPack/CardScript） |

**没有改**：`BattleDriver.cs`（A 域）· `Deck/**`（C 域）· 其余 `Core/**`。

---

## ④ 断言

- 已在（上一批）：`CardBaseDemo.AssertTooltipSuppression`（抑制期不弹 / 同 owner 不叠加 / 重复 Release 幂等 / 放开能弹）。
- **待接（给出确切位置，交给 A / 下一批）**：
  1. `BattleDriver.TickKeywordTooltip` / `TickTooltipAt` 弹之前先判 **`cardView.TooltipsEnabled`**（一行）—— 否则「相关卡不该弹」这一条只落了开关、没有消费者；
  2. 同上把 `Tooltip.Show(body, v.KeywordAnchor)` 换成 **`Tooltip.ShowAtMouse(body, BattleDriver.MouseWorld, tmp.transform.position.z, 1f/TipPx)`**（`MouseWorld` = A 新加，含「屏幕外归零」守卫）；
  3. `BattleCardManager:128` 的等价物（新卡先关、摆好后开）在我们这边是建卡入口（`BattleDriver`/`BattleScene`），按同一条开关接；
  4. 建议断言：`CardView.TooltipsEnabled` 默认 true；`CardDisplayWindow` 建扇后 `SlotViews[0].TooltipsEnabled && !SlotViews[1].TooltipsEnabled`；换位后反过来。
- 本批 30 条 C 的断言随各条落地时补（逐条判据已在账的「备注」列给出）。

---

## ⑤ 没查清 / 抽查记录

- **2 条 B（570/581）**：判据都在服务端（RankedV3 活动表 / `FeatureConfig` / 活动卡组），本地无来源 ⇒ 留 B（⛔ 不编）。
- **抽查**（防「代理说查不到就查不到」）：
  · `CemeteryLogGroup` / `HoverManager` 用 `assets_full` 全量 grep 独有序列化字段 ⇒ **确认 0 命中、只剩 MonoScript**；
  · `artOverride` 全仓赋值点 = Shell 两处 + `BattleDriver` 的进攻卡 ⇒ **确认战斗建卡没有异画替换**（92 的 C 成立）；
  · `BattleDriver` 全篇 `grep ForAnim|timeInAnimation` = 0 ⇒ **确认 389 的 P1 缺口成立**；
  · `PracticeModePopup.PickArmy`（2580）只走 `SelectDefaultDeck`、`RebuildArmyCells` 只在 Build/滚动回调里调 ⇒ **573 成立**；
  · `CardView` 无「Legendary 溶解材质」分支 ⇒ **108 成立**。
- **30 条 C 的等级**由核的人给、我按「玩家可见性」复核过一遍；其中 389 从 B 里挑出来升为 **P1**（原版有明确停表机制）。

---

## ⑥ 顺手发现（⛔ 都不在本次可改范围，记账）

1. 🔴 **`manaAccumulation` 挂错了原版字段**（`BattleDriver.cs:2776`）：原版 `BattleManager__SetupBoardPhase.c:181/196` 读的是 `GameplayVariablesData **+0x34**`，而 `dump.cs` 里 **+0x34 = `addedManaSecond`**（`manaAccumulation` 在 **+0x40**）。⇒ 「能量累积」整组亮灭的判据应是 `addedManaSecond`（后手一次性补贴），**`UpdateManaLeftAccumulation` 那道闸才是 manaAccumulation**。
2. 🔴 **我们的 `public int manaAccumulation = 0` 从来没被赋值**（全仓无写点）⇒ 遭遇模式（`SkirmishManaAccumulation=1`）这盏灯永远不亮；且它只在 `BuildHudExtras` 算一次、不随局变。落点 = `BattleDriver`（A 域）。
3. 🔴 **`BattleDriver.cs:5797` 那句注释是错的**（「原版那颗 Skip 钮常驻」）—— 原版 `BattleSettingsWindow__Open.c` **第一句**就是 `SetActive(matchType == Tutorial(100))`。请 A 按铁律 5 就地订正（连同 258 的 C 一起修）。
4. **账里第 242 行的判据要点已订正**：原版 `OnReachMilestone` **只改文本 `x{index+1}`、不点图标不播动画**（`ScoreUpAnimation` 全库零调用）。
5. **`Battle/HUD/Replay` 这条键**已在 `Core/Loc.cs:1271`，但**全仓 0 消费点** —— 409 的 C 落地时正好用它。
6. 本批确认了**一整族发行版死代码**（`CemeteryLogGroup` · `HoverManager` · `BattleScoreIcon` 的 Config/Check · `BasicCardUI` 的星标/SetupCollider/OnDescriptionShow · `CardDisplayWindow.TryInitializeCard` · `BattleCardUI` 的 vanguard/stealth/reflected 框那一族）—— 以后审查**别再把这批当缺口**。

---

## ⑦ 跨账

- **120 `GetTooltipPosition`（卡牌面账）**：判 C（不等价）已完成判据、Core 侧 `ShowAtMouse` 已备；**调用点一行在 `BattleDriver.cs:30975`（A 域）**，见 §④。
- **卡牌面 95/97（`CardTrait._hide`）→ A**：亲核 5 处生产调用点（`CardScript.cs:14377/14965/18117/20179` + 定义）**全部 `hideTrait=false`** ⇒ `_hide` 零可达，我们无该字段但 `ShowingTrait` 行为等价。
- **卡牌面 61/65（CraftCard/UpgradeCard）→ A**：亲核 `decomp_full/CardServices__CraftCard.c:44-46`（库存无 item ⇒ 静默 return）与 `__UpgradeCard.c:28-33`（原卡 null ⇒ 崩/断言）；CloudScript 900/901 ⇒ 我们把服务端判定在本地**模拟了一层**（更严、出声），不是少分支。
- **`ToggleInvalid` 改 alpha 0.5**：落点 `Deck/DeckRuntime.cs` **本轮仍由 C 使用**（`Deck/DeckRuntime.cs`/`DeckEditorState.cs` 均 M）⇒ 未改；Core 侧 `CardView.SetAlpha` 已有，归还后一行接上。
- **`Core/FilterPanelModel.cs`**：C 已归还（chip 行做完），本批**未碰**。
- **`Loc` 纪律**：`工具/check_loc_dupes.py` = **753 条 / 唯一键 753 / 重复 0**（本轮复核）。

---

## ⑧ P0 复核（调度台 2026-10-25 口径：**每一局都会遇到 + 玩家能看见 ⇒ P0**）

### 8.1 🔴 先订正一条前提：时钟那个缺口**只影响 15 秒倒计时，不影响回合表**

调度台的初判是「原版在演出/结算期间停表（把动画时间从已用时里扣掉），我们照走 ⇒ 每局每回合都遇到」。**逐句读完 `ClockManager` 后不成立**，证据（🥇 ILSpy，`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ClockManager.cs`）：

| 事实 | 出处 |
|---|---|
| `PauseForAnim` 只置一位 `clockForAnimFlag`；`UnpauseForAnim` 清它 | `:340-348` |
| `Update` 里 `elapsed = (now − start) − timePausedOrDiscounted + timeInBackground − timeInAnimation` | `:663-672` |
| `timeInAnimation += dt` 那一句在 **`if (clockForAnimFlag)`** 里，而它**排在「还没进倒计时」的整块 `return` 之后** ⇒ **只有 `clockCountdownFlag == true`（那 15 秒倒计时）时才累计** | `:684-742` |
| 回合表（`timePercentagePassed = elapsed / totalTime`）在**进倒计时之前就 return 了**，`clockForAnimFlag` 根本没参与 | `:686-700` |
| `ClockRunning()` / `ClockRunningAnim()` 全量 ILSpy **0 个调用者**（死 API；我们的账 387 行判 A 也是因为它没有消费方） | 全仓 grep 仅 `ClockManager.cs` 自身 |
| 原版 3 个 `PauseForAnim` 调用点 = `ResolveAction` 演出期间 / `_ChooseUnitTarget` 选目标期间 / 换牌结束那一刻 | `BattleManager.cs:1838 / 22405 / 25803`（`UnpauseForAnim` 在 `_ChooseUnitTarget.MoveNext:39` / `ChoiceOfCardPlayer:18` / `FinishResolvingAction:133`） |

⇒ **真实缺口 = 「15 秒倒计时遇到演出时不顺延」**，不是「回合表不停」。它**不是每局都遇到**（要先把自己的回合计时走完进倒计时、且倒计时期间有演出），玩家能看见的也只是倒计时那串数字停不停。
⇒ 三行（388 `ClockRunningAnim` / 401 `PauseForAnim` / 414 `UnpauseForAnim`）**统一 P1**（不再是 P1/P2/P2 三个等级，也不是 P0）；备注里写清了这条判据。

### 8.2 五条 P1 逐条判（含「不是每局都遇到，因为…」）

| # | 方法 | 判 | 理由（这句话本身就是判据） |
|---|---|---|---|
| 388/401/414 | 时钟 for-anim | **P1** | 不是每局都遇到：只在「计时走完 → 15 s 倒计时」且倒计时期间有演出时可见 |
| 178/185 | 本机掉线分支 | **P1** | 不是每局都遇到：单机永不进入；只在**联机 + 本机掉线**时可见 |
| 182 | `OpponentDesyncCancelMatch` | **P1** | 不是每局都遇到：只在联机**指纹不一致**时可见 |
| 258 | `Skip` 钮显隐 + 回放文案 | **P1（面板侧已做）** | 不是每局都遇到：只在「打开设置面板 + 非教程局」；**已完成** `Battle/SettingsPanel.cs:2468 SetMatchType` / `:2475 ApplyMatchTypeUI` / `:2482 SetResignText`（Skip 闸 = `matchType==Tutorial(100)`，`SetActive`/`Show` 两处都读 `SkipWanted`），断言在 `Editor/BattleScene.cs`（Tutorial 显示 / Replay 隐藏 + `Battle/Settings/Exit` / Practice 回 `ResignButton` / 复位） |
| 167 | `UpdateBastionr` | **P1** | 不是每局都遇到：只在场上单位带 `bastion` 时才有那颗计数 |
| 366/369/375 | 选牌面板倒计时 | **P1** | 不是每局都遇到：只在 `choose` 类效果触发时才有这块面板 |

### 8.3 复核时**升成 P0** 的（每局都遇到 + 玩家能看见）

> ⚠️ **本节是复核那一刻的快照**；其中 4 项已落地/改判（455 已由 A 实现 ⇒ A；167 与光标 ⇒ D；`TipPrebattle` 10 条 ⇒ B）。**以 §11.1 为现状**。

| # | 方法 | 缺什么 | 落点（谁能改） |
|---|---|---|---|
| 42 | `BasicCardUI.OnEnable` | 抬牌（`ShowCardInHand`）期间**不该**开放大窗 | `Hand/CardInteraction.cs` / `BattleDriver`（A 域） |
| 194 | `BattleHud.GetCardDrawPosition` | 抽牌补间的**终点锚** | `BattleDriver` 抽牌补间（A 域） |
| 543/552 | `ManaTypeHolder.DoTextAnim/TryDOVfx` | 能量数字变化时的 `DOPunchScale` 弹跳 | `BattleDriver.UpdateHud`（A 域） |
| 455 | `EndBattleDoors.SetupDoor` | 结算时**收掉所有开着的窗** | `BattleDriver` 收局链（A 域） |
| 653-662 | `TipPrebattle`（10 条） | **战前提示**整件没建 | 新件 `Battle/TipPrebattle.cs`（未分配） |

> 账现状（复核后 + 光标改判 D）：**A 414 · C 58（P0 15 / P1 11 / P2 32）· B 12 · D 195 = 679**。
> ⚠️ 我名下**没有**这些 P0 的文件（都在 `BattleDriver.cs` / 未分配的新件）⇒ **没改代码**，落点与改法见 §8.4。

### 8.4 可直接照抄的补丁（给 A，`BattleDriver.cs`）

**① 倒计时遇到演出不顺延**（388/401/414 的唯一 live 语义）

```csharp
// 字段（放在 `_clockLeft` 附近，约 BattleDriver.cs:2862）
int _clockInCountdownAnimDepth;   // 原版 `ClockManager.clockForAnimFlag` 的等价物：只在倒计时阶段生效

// 公开口（与 `NetClockPaused` 并列，约 :254）
public void PauseClockForAnim() { _clockInCountdownAnimDepth++; }
public void UnpauseClockForAnim() { if (_clockInCountdownAnimDepth > 0) _clockInCountdownAnimDepth--; }

// TickClock（`BattleDriver.cs:13977`，减 `_clockLeft` **之前**）
if (_clockInCountdown && (_clockInCountdownAnimDepth > 0 || WaitingForBlockingAction)) return;
```

判据：`ClockManager.cs:340-348`（只置位）+ `:736-742`（累计 `timeInAnimation`）+ `:663-672`（从 elapsed 扣）⇒ 只有倒计时阶段受影响；`WaitingForBlockingAction` 正是我们这边「演出没演完」的同一道闸（`TickClock:14017` 已经用它挡超时结束回合），**用它就不必在演出各处手工 Pause/Unpause**（少 6 个调用点 = 少 6 个漏接的机会）。
⛔ 别把这条放进「回合表」分支：那会造出一条原版没有的行为。

**② 结算收窗**（455）

```csharp
// 结算面板 ShowEndPanelAndFreezeInput 入口第一句
WindowsManager.CloseAllWindows?.Invoke();   // 判据 EndBattleDoors__SetupDoor.c（我们已有的等价口见 Shell/WindowsManager）
```

**③ 抽牌终点锚**（194）：把抽牌补间的目标点从现用锚改成 `GetCardDrawPosition` 的原版节点（🔴 **别把任何 `(x, y)` 写死**：D 用两个反例（`CenterCameraButton` 1108+568.17 vs `OffensiveButton` 987+446.86）证明树里的 y 是**各自父节点的 `m_AnchoredPosition`**，「一律翻转」算不出来 ⇒ 必须逐节点带 anchor/pivot + 父链换算） —— `LeftArea/CardDrawPositions/{PlayerDrawnPos, EnemyDrawnPos}`（`CardScript__DrawCard.c:76-80`），几何值见 `资料/说明书/01_战斗_对战/` 对应战场树。

**④ 设置面板两行接线**（258 的驱动那一半 + 260）—— 面板侧**已经做完并带断言**，只差 `BattleDriver.cs:27846` 这一行：

```csharp
// 原：_settingsPanel = SettingsPanel.Create(root, Forfeit);
_settingsPanel = SettingsPanel.Create(root, Forfeit, Ctx != null ? Ctx.MatchType : (RuleEngine.MatchType?)null);
_settingsPanel.OnSkipTutorial = SkipTutorialFromSettings;   // 不接 ⇒ 点「跳过教程」只打 LogWarning、没有效果（R260）
```
判据：`BattleSettingsWindow__Open.c:41-121`（`SetActive(matchType - 100 == 0)` + 回放局 `Battle/Settings/Exit`）；`SkipTutorialButtonOnClick` 两行 = `Hide()` + 语义回调（我们的语义口 `BattleDriver.SkipTutorialFromSettings`）。
⚠️ 传 `null` 时面板会**出声一次**（`SettingsPanel.Show` 的 `_matchTypeWarned`），所以漏传不会静默。

> ⑤ `TipPrebattle`（653-662）是**新件**，不是一行补丁：判据 = 原版 `DisplayTips(PlayerDataManager)` 轮播 + `openTip/closeTip` 两态；建议派一个执行代理，⛔ 不要在 `BattleDriver` 里塞。
> （自绘光标 448-450 **已改判 D**，见 §⑩ #11 —— 原版那件是死代码。）

---

## ⑨ A 档落点收紧（四份账，2026-10-25，按 `报告/G_A档审计_跨账.md`）

**改法**：把落点从「文件 / 类.方法 / 行为描述 / 注释行 / 函数中间」改成 **`文件:行`（方法声明行，或那段逻辑的开头）**；🟢 类（审计已算好声明行）直接抄，🟡/🔴 类逐条回代码找；**找不到真实落点的一律降 B**（⛔ 不硬凑行号）。

| 账 | 改前 A / 问题条数 | 改后 A | 其中**方法级落点** | 降 B | 依据 |
|---|---|---:|---:|---:|---|
| `卡牌面_渲染.tsv` | 108 / 108 | **100** | 99（+1 条指在**属性/数据表项**行，属审计允许的「数据表项」） | 8 | 卡面组件是**程序化建层**（没有原版那套 MonoBehaviour），uGUI 逐事件回调无对应 |
| `轮抽.tsv` | 99 / 99 | **98** | 96（+2 指在 `ReRollCost` **属性声明行**） | 1 | 同一个机制被拆在 12 个原版类里，我们收在一个 `DraftModeWindow`；有 1 条纯生命周期 |
| `战役.tsv` | 68 / 68 | **60** | **60** | 8 | 全是 uGUI `OnEnable/OnDisable/OnDestroy` 的**解绑**路径（我们没有事件绑定） |
| `战斗界面_HUD.tsv` | 423 / 342 | **414**（另 1 条 C→A） | 全部（含 111 条原来用反斜杠路径的 `BattleDriver` 落点已规范成 `/`） | 10 | 见下 |

**HUD 的做法**：① 281 条原来就带 `文件:行` ⇒ 机械解析到**所在方法的声明行**（68 条落在方法中间/注释、原行号因别人改文件而失效的都据此归位）；② 82 条只有文件名的逐条手工给方法；③ 指不到方法的老老实实降 B（10 条，理由逐条写进账的「差异说明」列）。
**顺带修掉的两处**：`BattleCardManager` 一族里有 3 条原行号已因 `BattleDriver` 被编辑而**指向了别的行为**（`GetAbilityListInPlayForIcons` / `ShowEphemeral` / `AlliancePanelOpenButton`），已人工改指并注明「旧行号已失效」。

**机检**（`python -I -X utf8 Unity/工具/check_ledger_points.py <账>`，2026-10-25 收口）：
| 账 | A | 通过 | 失败 |
|---|---:|---:|---:|
| `卡牌面_渲染.tsv` | 100 | **100** | 0 |
| `轮抽.tsv` | 98 | **98** | 0 |
| `战役.tsv` | 60 | **60** | 0 |
| `战斗界面_HUD.tsv` | 414 | **414** | 0 |

⚠️ **`BattleDriver.cs` / `DeckRules.cs` 此刻正被别的代理编辑**（同一会话里我就遇到过两次行号漂移：`UpdateHud` 从 30456 挪走、`SkullsFor` 从 529 挪到 536）⇒ 这批 `文件:行` 是**快照**；**交活前再跑一次机检**（它只验「行号形态」，判不出「指到了另一个方法的声明」—— 后者靠人看）。

---

## ⑩ 原版**发行版死代码**（单独成节，逐条带证据）

> 判据三件套：**① 全量反编译 0 个代码调用点**（`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/**` 2,404 文件 grep）· **② `assets_full` 全量 JSON 里该类的**独有序列化字段** 0 命中 / 只剩 `MonoScript_*.json`** · **③ 预制体/场景里 0 个组件实例**。
> ⛔ **以后审查别再把这批当缺口**；我们的等价物写在最后一列。

| # | 原版类.方法（账里的行号） | ① 代码侧 | ② 资源侧 | 我们的等价物 |
|---|---|---|---|---|
| 1 | `TraitDescriptionUI` / `TraitDescItem` / `TraitDefinitionDisplay` 全族（27 个方法，卡牌面账 D 区） | ILSpy 全库 **0 调用** | 独有字段 `traitDescriptionItem` / `traitDefinitionList` **0 命中**；只剩 `MonoScript_*` | 词条说明走 `Core/Tooltip.TipText.Trait`（两栏版式）+ `Resources/trait_tips.json` |
| 2 | `CemeteryLogGroup`（319-323） | 0 调用 | `"targetCard1"` / `"centralCemetery"` 全 0；只剩 `MonoScript_2187` | `BattleDriver.cs:11612 TickLogCard`（一行字 + 单张悬停卡） |
| 3 | `HoverManager`（511-517） | 0 调用 | `"miniTipAsset"` / `"hightLight"` / `"noTipsModeOn"` 全 0 | `Core/Tooltip`（全局悬停）+ 按 owner 的抑制计数 |
| 4 | `BattleScoreIcon.Config/Check`（234/235） | 0 调用 | 运行期 UI 树只有 `MatchSkulls Icon/Score` 两件 | `BattleDriver.cs:30428`（单骷髅 + `x{n}`） |
| 5 | `BattleCardUI.SetVanguardStealth` / `StartSummonSicknessAnim` 的框那一族（145/155/158/161） | `vanguardFrame(+0x278)` 全 C# **零写入** | `stealthPrefab` / `reflectedPrefab` 序列化为空 | 无框可画 ⇒ 等价（`TraitFrames` 只画状态框） |
| 6 | `BasicCardUI.SetCardRarityStars` / `SetEmptyRarityStars` / `SetupCollider` / `OnDescriptionShow`（41/53/58/65） | private + 0 调用 / 0 persistent call | 预制体无星形层 | 无星形层 / 手搓命中（`CardView.EnsureHitCollider`）/ 不展开描述（与原版同） |
| 7 | `CardDisplayWindow.TryInitializeCard`（312） | 0 调用 | —— | 真路径 `InitializeRelatedCards` → `Battle/CardDisplayWindow.cs:1406` |
| 8 | `CemeteryManager.DisplayCardTraits` / `SetEmptyText`（348/357） | 空方法体 / 0 调用 | `emptyText` 字段**13/13 场全 null** | 无（不需要） |
| 9 | `BattleCardUI.DestroyActivateAbilityAnim` / `StopReflected` / `StopShowingStealth` / `SetEnemySecret`（112/157/160/139） | 0 调用 / 恒 null 判空 | 对应预制体字段为空 | 无（不需要） |
| 10 | `ClockManager.ClockRunning()` / `ClockRunningAnim()`（387/388 的一半） | 全库 **0 调用者**（只有 `ClockManager.cs` 自身） | —— | 我们的 `Ctx.Active == _me && !IsOver && !NetClockPaused`（同一判据，写在 `TickClock` 头部） |

> 与 #10 相关的一条**更正**：账 387（`ClockRunning`）判 A 是对的（判据等价），**但 388 不是死代码那一半** —— `clockForAnimFlag` 的 live 效果在 `Update` 的倒计时分支里（见 §8.1），所以要按 P1 补，⛔ 不要因为「`ClockRunning` 没调用者」把整条划成 D。
| 11 | `CursorManager` 全类（448/449/450） | `Start/Update/OnGUI` 只有组件自己调；**唯一实例的 `m_IsActive = 0`**（`Intro.unity` `&118`） | `cursorTextureNormal` / `cursorTextureClicked` **序列化为 null**，且**全库 grep `cursorTextureNormal` = 0 命中**（84 包 / 24.7 万文件） | **系统光标**：`Cursor.visible=false` 那句的闸是 `SteamUtils.IsSteamRunningOnSteamDeck() && touchInput`（只有 Steam Deck + 触屏才藏）⇒ PC 原版就是系统光标，我们**一致** |

---

## ⑪ 下一轮：HUD 的 C 按**落点**分类（P0 → P1 → P2）；已完成的部分

### 11.1 15 条 P0 的去向（分类后只剩 **3 条**要动）

| 原 # | 落点 | 处理 |
|---|---|---|
| 455 `EndBattleDoors.SetupDoor` | `BattleDriver.ShowEndPanelAndFreezeInput` | ✅ **A 已经做了**（`BattleDriver.cs:30803-30806` 第一句 `WindowsManager.CloseAllWindows()`）⇒ 账 **C→A**（铁律 5：旧记录过期） |
| 167 `BattleCardUI.UpdateBastionr` | 原版 UI 件 | ✅ **D**：`bastionText` 全量 24.7 万 JSON **只 1 处命中且 `m_PathID = 0`（null）** ⇒ 出货包里没有那颗节点，原版自己也不显示；我们引擎侧 `UnitState.Bastion` 已实现 |
| 448/449/450 `CursorManager` | — | ✅ **D**（代理 D 三条证据，见 §⑩ #11） |
| 653-662 `TipPrebattle`（10 条） | 新件 `Battle/TipPrebattle.cs` | ✅ **C→B**：件已建好，**接线未做**（几何/文案池由调用方传；时机判据不可及） |
| **42** `BasicCardUI.OnEnable`（抬牌态不开窗） | `Hand/CardInteraction.cs` | ✅ **判据侧已落地**：`CardInteraction.CardDisplaySuppressedFor(card)`（`:549-566`，判据 = `CardScript.ShowCardInHand:10484 → :10597 false` / `StopShowingCardInHand:11001 → :11034 true`）。**驱动一行给 A**：`OnCardTapped:12633` 里 `SetHint("")` 之后加 `if (interaction != null && interaction.CardDisplaySuppressedFor(card)) return;` |
| **194** `BattleHud.GetCardDrawPosition` | `BattleDriver` 抽牌补间 | ⏳ **A**（补丁见 §8.4③；⛔ 坐标别写死） |
| **543/552** `ManaTypeHolder.DoTextAnim/TryDOVfx` | `BattleDriver.UpdateHud` | ⏳ **A**：见下方 11.2② |

### 11.2 可直接照抄的三段补丁（给 A，`BattleDriver.cs`）

**① 抬牌态不开窗**（42）
```csharp
void OnCardTapped(CardView card)
{
    SetHint("");
    if (interaction != null && interaction.CardDisplaySuppressedFor(card)) return;  // 原版 ShowCardInHand 期间 OnClick 已摘
    ...
```
判据：`BasicCardUI.ToggleOpenCardDisplayOnTouch:700-710`（`RemoveListener` + `if (option) AddListener`）。

**② 能量数字变化弹跳**（543/552）
```csharp
// `UpdateHud` 里每次把能量数字 SetText 之后：
if (_energyText != null && value != _energyShownLast)
{
    _energyShownLast = value;
    var tf = _energyText.transform;
    tf.localScale = Vector3.one;                                        // 原版 DoTextAnim 先复位
    CardTween.Use(tf.DOPunchScale(Vector3.one * 0.2f, 0.5f, 10, 1f), Ease.Linear, this);   // ×0.2 / 0.5s / vibrato 10 / elasticity 1.0
}
```
判据：原版 `ManaTypeHolder.DoTextAnim`（复位 `localScale` → `DOPunchScale(基向量 × 0.2, 0.5s, vibrato 10, elasticity 1.0)`）。

**③ 选牌面板倒计时接线**（366/369/375，面板侧我已做完）
```csharp
// `TickClock` 里（`_clockInCountdown` 那一支）：
if (_choosePanel != null && _choosePanel.Visible && _clockInCountdown)
{
    _choosePanel.TickOpenDelay((int)_clockLeft);            // 开面板那一刻 counter<=1 的 0.5 s 延迟档
    if (Mathf.CeilToInt(_clockLeft) != _lastChoiceTick)
    { _lastChoiceTick = Mathf.CeilToInt(_clockLeft); _choosePanel.CountdownTrigger(_lastChoiceTick); }
}
```
判据：`BattleManager.cs:25350` 把 `clockManager.counter` 当 `timerCounter` 传给 `ChooseCardMenu.Setup`；`ChooseCardMenu.CountdownTrigger:797`（随机选 + `0:{ss:00}` + `counter<3` 确认）。

### 11.3 P1（10 条）现状

| # | 状态 |
|---|---|
| 388/401/414 时钟 for-anim | **P1**，补丁 §8.4①（只影响 15 s 倒计时，见 §8.1） |
| 177/181/184 联机掉线/不同步 | **P1 · 联机线**（`Net/NetBattle.cs`，不在我域） |
| 257 设置面板 | **面板侧已做完 + 4 条断言**；驱动两行见 §8.4④ |
| 366/369/375 选牌倒计时 | **面板侧已做完 + 5 条断言**（`CardChoicePanel.SetTimer/CountdownTrigger/TickOpenDelay/ConfirmNow`，`BattleScene` 断言：文案 `0:07`/空选不提交/`counter>=3` 不确认/每次刷新/关面板还原）；驱动一行见 11.2③ |
| 167 bastion | **D**（见 11.1） |

### 11.4 P2（32 条）按落点分派

| 落点 | 条 | 内容 |
|---|---|---|
| `Battle/SkillPanel.cs`（我） | 4 · 8 · 14 · 19 | 拖拽亮 acting 层 / FinishActing 淡出 / 目标数拼进 descText |
| `Core/CardView.cs`（我） | 107 · 149 · 151 · 152 · 158 · 159 | 传说档溶解材质 / codex·oath 持久框 / 残骸露原卡 |
| `Battle/CardDisplayWindow.cs`（我） | 305 · 310 | 教程指向物 / `showCardTextButton` 切换 |
| `Battle/BattleLogPanel.cs`（我） | 316 · 323 | 行内小卡 + 伤害角标 / 行首动作类型图标 |
| `Battle/ChoosePanel`（我） | 363 · 377 | 回放自动确认那半 / `Setup` 里 `timerCounter<2` 的 0.5 s 随机选（已随倒计时落地，待核） |
| `Core/Loc` + `Battle/SettingsPanel`（我） | 188 | 换语言重刷整棵 HUD |
| `BattleDriver.cs`（A） | 338 · 396 · 398 · 408 · 411 · 412 · 536 · 547 · 242 | 陷阱 drawTrap 行 / 教程倒计时 10 s / 切后台补时 / 回放 `Battle/HUD/Replay` 文案 / StopClock 两半 / 存档能量显示两件 / 分数动画 |
| `Net/**`（联机线） | 179 · 183 | `SetupErrors` 里 `OnOpponentDesync` 那条订阅 |
| `Shell/PracticeModePopup.cs`（外壳线） | 572 · 581 · 583 | 换阵营后重算高亮 / 换卡组重筛 / SaveSelectedDeck |

> ⚠️ `BattleDriver.cs` 那 9 条**每一条都同时指向一处具体行为**（账里「落点」列给了行号+方法名），A 照抄即可；我这边下一轮先做 `SkillPanel` / `CardView` / `CardDisplayWindow` / `BattleLogPanel` 那 13 条。

### 11.5 机检（含新能力）

`check_ledger_points.py` 的**按方法名重定位**已经就绪，我拿 `--fix` 跑过一遍：四份账 **漂移 82 条全部就地改正**，现在 **100/100 · 98/98 · 60/60 · 415/415，失败 0、漂移 0**。
（`BattleDriver.cs` 仍被别人频繁编辑 ⇒ 交活前再跑一次 `--fix` 是标准动作；这条已经写进账的备注。）
