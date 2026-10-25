# HUD 域「缺注解」第 1 轮（P1 执行代理）

账本：`Unity/资料/完全复刻/账/战斗界面_HUD.tsv`（只改这一份账；未动任何 `.cs`）

---

## ① 结论

**377 条缺注解全部清零**（`list_missing_annot.py 战斗界面_HUD` = 0；`verify_annot.py` 报 HUD 域 0 条）。

| 处置 | 条数 | 说明 |
|---|---|---|
| ① 确为等价实现，落点末尾补 `（等价实现 = <声明名>：<理由>）` | **341** | 每条理由写「原版这个方法做什么 / 我们这段代码怎么做到同一件事」，尽量带可观察量（哪一层被改写 / 谁被重算 / 发什么事件）。⚠️ 这 342 里**有相当一部分同时改了落点**（原落点是 grep 撞到的属性 get / 自检口 / 读口，只加注解会把错落点钉死）⇒ 一律「先改指到真声明、再补注解」 |
| ② 原落点指错，改指到**与原版同名**的声明（因此不需要注解） | **17** | 见 §二 的「改指」表；改完逐条过形态闸 |
| ③ 找不到对应实现 ⇒ 退 `B`（原落点原文保留，差异列写 `语义闸 2026-10-25：落点无对应实现 ⇒ 退 B`） | **17** | 见 §二 的「退 B」表 |
| D（另两批裁定的死代码） | **1**（`PracticeModePopup.LoadSavedDeck`，G 建议）+ **10**（`TipPrebattle` 整类，协调台裁定） | 见 §三 |

**377 条的最终状态：A 358 · B 18 · D 1。**（第 2 轮跟进后：`PracticeModePopup.IsSavedDeck` 由 A 退 B）
账本总数（收工读数）：**A 396 / B 30 / C 46 / D 207**（A 从 415 降到 396 = 18 条退 B + 1 条退 D，其余全是注解/改指）。

> 🔴 **一条重要的过程发现（影响所有账）**：本轮进行中 `Battle/ BattleDriver.cs` **被另一个代理持续改写**，行号整体漂移（例：`void UpdateHud()` 从 30480 → **30567**、`public void Begin(` 从 4057 → **4848**，实测位移 +87 / +791）。
> 因此我把本域所有「等价注解」行的落点**统一改成抗漂移形状**：
> `路径:行（\`声明名\`）（等价实现 = 声明名：理由）` —— **第一个括号只放裸声明名**，行号范围一律挪进第二个括号。
> `check_ledger_points.py` 的 `decl_name_in` 只认「括号里恰好一个标识符」，这个形状能**按名字重定位** ⇒ 将来某人再改动 `BattleDriver.cs`，跑一次 `--fix` 就能修回来（旧形状 `（\`Name\` 的X段 :a-b）` 会被判「不像声明」直接失败）。

## ② 判据变更明细（带 文件:行号）

### ②-1 改指（原落点不是实现）

| 原版 | 原落点（错） | 现落点 | 为什么 |
|---|---|---|---|
| `BasicCardUI.GetRarity` | `CardArt.cs:135 TierOf` | `Core/RarityColors.cs:70（Tier）` | `CardArt.TierOf` **恒返回 MaxTier=4**（本作所有卡都用最高档框），不是稀有度档号；`RarityColors.Tier` 才是 `common1/rare2/epic3/legendary4/special5` |
| `BasicCardUI.SetLayer` | `CardView.cs:2218 FaceShowsArmy` | `CardView.cs:2248（SetLayer）` | 2218 是**判据谓词**；2248 才是 `SetLayerRec` 递归设层（与原版 `SetStaticLayerRecursively` 同构） |
| `BasicCardUI.RemoveHealthAndAttack` / `ToggleStats(bool)` | `CardView.cs:13315 FaceStatVisible` | `CardView.cs:12949（StatShownFor）` | `FaceStatVisible` 是**自检读口**；显隐唯一判据 = `StatShownFor`，由 `SyncFaceStatLayers`(:13056)/`SyncFaceStatTexts`(:13151) 应用 |
| `BasicCardUI.ToggleOpenCardDisplayOnTouch` | `BattleDriver.cs:8433 OnMulliganCardSeen` | `Hand/CardInteraction.cs:560（CardDisplaySuppressedFor）` | 原版这个开关只是把 `CardClicked` 从 `uiEventCatcher.OnClick` 挂上/摘下；我们的等价判据 = 抬牌态抑制开窗（`OnCardTapped` 的闸） |
| `BasicCardUI.SetCardImage()` / `SetImageSprite(...)` | `CardView.cs:7045 SetCardImageTo3DBase` | `CardView.cs:4176（Build）` / `CardView.cs:2136（Create）` | 7045 是 `BattleCardUI.SetCardImageTo3DBase`（**另一个类**、3D 卡体材质）；2D 立绘在建卡那一趟从 `CardData` 取（`ArtTexture(d)` → `_art` 层），运行期换图口只有 `Create(..., artOverride:)` |
| `BasicCardUI.ChangedLanguage` / `BattleCardUI.ChangedLanguage` | `Core/CardText.cs:129 KeywordDisplay` | `CardView.cs:4861（BuildTextLayers）` | `KeywordDisplay` 是查词；重设卡面四行文字 = `BuildTextLayers` |
| `BasicCardUI.SetCardImage(Sprite)` / `SetImageSprite` | 同上 | `CardView.cs:2136（Create）` | 对应口 = 建卡时的 `artOverride` |
| `BattleCardManager.GetCardFromUniqueId` / `RegisterCardInPlay` | `CardView.cs:332（Vector2 属性 get）` | `BattleDriver.cs:23716 → 现名（ViewAt）` | 332 是 `BoardMeleeGlowAt` 那个 `new Vector2(...)` 属性；反查视图的唯一口 = `FindInstanceById` + `ViewAt(owner, slot)` |
| `BattleCardManager.AddCardToCemetery`（类 `CemeteryManager`） | `PlayerState.cs:19 Deck` | `PlayerState.cs:21（Discard）` | Deck 是牌库；墓地/弃牌堆是 `Discard` |
| `CemeteryManager.ClickCardLink` / `DisplayCard` | `BattleDriver.cs:11250 LogTerm` | `BattleDriver.cs:11741（ShowLogCard）` | `LogTerm` 只取词条；点卡名弹卡 = `ShowLogCard` |
| `CemeteryManager.Hide/ShowCemeteryLogBtn` | `BattleDriver.cs:712 RecBegin` | `BattleDriver.cs:16357（SetVis(HudPiece,bool)）` | RecBegin 是联机重连入口；显隐那一句就是 `SetVis` |
| `CemeteryManager.GetCardsPlayed` | `BattleContext.cs:798 HandCardOrNull` | `BattleContext.cs:1316（NotePlayed）` | 原版从墓地日志筛 playCard；我们记独立「本局打出过」流水（`PlayedCards` / `NotePlayed`） |
| `BattleHud.DisplayMultiCardView` | `BattleDriver.cs:2139 FindInstanceById` | `BattleDriver.cs:13079（OpenShowCardsWindow）` | 开多卡窗那扇唯一入口 |
| `BattleHud.DisplayOvertime` | `SetMulliganWaitStateForTest` | `ShowOvertime` | 自检口 ≠ 实现 |
| `BattleHud.GetUIAnchor/TryGetAnchor` | `HookEventTimingCardSource` | `UiAnimAnchor` | 4 颗 Anim Anchors 的表在 `UiAnimAnchor` |
| `BattleHud.InitializeProfile/SetProfileInfo` | `ShowAsk` | `RefreshHudTitles` | 名牌重填 |
| `BattleHud.Pause/Play/Replay/StepButtonClicked`（BattleHud + ReplayHud 共 8 条） | `ResolvePlayTargetByUnit` | `OnReplayButtonClicked(ReplayBar.Btn)` | 回放条四钮共用一个分派口（原版 4 方法 → 我们 1 方法，已给 which 分派说明） |
| `BattleHud.CloseAction` / `Update` | `ClickCameraResetHud` | `HandleSettingsEsc` | ESC 开/关设置窗的唯一判据 |
| `BattleHud.ToggleResetAutoCameraZoom` | `FindInstanceById` | `ToggleCameraResetButton` | 显隐 + 0.5s punch 那条 |
| `BattleHud.ToggleWithAnimation` | `NetAfterTurnStart` | `ApplyMulliganHudVisibility` | 显隐翻转播 8 条 clip 中的两条 |
| `BattleHud.SetTurnPlayedCards` | `BuildHud`（只建件） | `UpdateHud` | 三枚 pip 在 BuildHud 建、UpdateHud 逐枚点亮 |
| `BattleHud.OffensiveButtonTransform` | `HudButtonPieceForTest`（纯自检取名口） | — | **协调台点名 ⇒ 退 B**（进攻钮没有别的读取口） |
| `BattleScore*` / `Mana*` / `ManaType*`（约 20 条） | `FindInstanceById` / `Vector2(SettingsBtnHitPx)` / `QuestPointsVisible` / `ShowAsk` / `SetMulliganWaitStateForTest` / `NextAsk` / `ReanchorHud` | `UpdateHud` / `SetFactionResourceVisible` / `RefreshEnergyAccumulation` / `DeckRules.SkullsFor` | 原落点全是 grep 撞到的**属性 get / 自检口 / 读口**；真实现按资源分块给了行号（能量 / 信仰 / 魂石 / 任务点 / 骷髅） |
| `ClockManager` 8 条 | `BattleDriver.cs:13581（Update）`（6 条同指）+ `RecBegin` / `FindInstanceById` / `ApplyTutorialActionView` / `DoPlay` | `TickClock` / `UpdateClockLabel` / `EndPlayerTurn` / `ResetClock` / `AutoEndTurnIfStuck` / `ClickEndTurnHud` / `PlayCountdownAnim` / `TurnSecondsForThisMatch`→`ResetClock` | **G 点名「6 条同指一行」** ⇒ 按走表/文案/超时/开表四个真身拆开（每行的理由写了它落在哪一段） |
| `CardDisplayWindow` 8 条 | 一律 `Show/Toggle/PlayOpenAnimation` | `Show(1221)` / `SwapToFront(1569)` / `BuildFan(1399)` / `SetEffectRows(915)` / `RefreshLore(1676)` / `SlotView(391)` / `HideCore(1315)` / `Hide(1307)` | 换位/关窗/效果行/风味/前台卡各自有真身，别都挂在「开关窗」上 |
| `ChooseCardMenu` 6 条 | `SpacingPx` / `ClearCardDecor` / `OnUguiDone` / `OnUguiEye` / `ChoosePanel.Open` | `LayOutCards(826)` / `TogglePick(1003)` / `ToggleEye(1187)` / `HandleClick(1200)` / `ChoosePanel.cs:202（ResolveTitle）` | G 点名 `GetTittleText → :202` 已照改 |
| `MulliganManager` 6 条 | `PointerOverUi` / `IllegalActionText` / `SetScriptFont` / `ToggleMark` / `HandleReplaySessionExit` / `Open` | `HandleClick(1200)` / `SetDoneText(131)` / `Visible(251)` / `HandleMulligan(8416)` / `SetVisible(973)` / `ClearCardButtons` | 含 G 点名的「Setup 方向反了」（现指 `SetVisible(false)` 的初始隐藏用法） |
| `BattleSettingsWindow` 8 条 | 一律 `OnSkipTutorialClicked` | `OnShadeClicked(1348)` / `OnCloseClicked(1328)` / `Hide(2502)` / `OnResignClicked(1304)` / `OnAutoZoomClicked(1332)` / `OnMuteChatClicked(1335)` / `EscPressed(1955)` | 面板每个钮都有自己的 handler；`ESCPressed` 用同名 `EscPressed`（大小写不同，已注明） |
| `BattleTipController` 8 条 | `TickTargetingLean` / `ViewOwnedByLiveUnit` / `SpeakConcede` / `UpdateReticle` | `SetHint(15644)` / `TickTipBanner(15680)` / `NotifyCantDoAction(7576)` | 提示条队列/去重/上限/逐条播各自的真身 |
| `TargetReticleController` 5 条 | `Hide` / `Deactivate` / `CrossPosition` / `Build` | `Awake(361)` / `Initialize→Build(363)` / `TryPresetOf(235)` | 颜色表、真 Awake、真 Build |
| `CombatCameraZoom` 5 条 | `Awake` / `ClampShiftLimits` / `TickInjected` | `OnDestroy(340)`（G 点名）/ `LateUpdate(405)` / `ApplyZoomAt(757)` / `GetCameraFrustumWorldBoundsWithShift(918)` / `HandleAutomaticControl(667)` | 同名实现就在文件里，原落点指的是调用方 |
| `BattleCameraSreenSize` 3 条 | `Initialize` / `NotifyScreenResolutionChanged` | `Start(167)` / `ResolutionHasChanged(184)` / `Initialize(208)` | 三个真身分得很清 |
| `FlyingCardController` 5 条 | 构造函数行 | `State(105)`（两条过渡态）/ `IsFlying(107)` / `ChangeState(135)` / `Toggle(120)` | 原落点是类声明/构造函数 |
| `ReplayHud` 6 条 | `BeginFromPendingCore` / `ResolvePlayTargetByUnit` / `Begin` | `ReplayBar.Create(162)` / `ReplayBar.Setup(330)` / `OnReplayButtonClicked` | 收紧到回放条自己的件 |
| `HoverManager` / `EverguildTooltipManager` 11 条 | `ShowEndPanelAndFreezeInput` / `Show` / `Build` | `Tooltip.Ensure(231)` / `Build(252)` / `Show(307)` / `Hide(392)` / `Wrap(615)` / `SuppressTooltips(360)` / `ReleaseTooltips(366)` / `Update(269)` | 收/发两向分清楚（`DisposeTooltip` 与 `Hide` 原指 `Show`，方向反了） |
| `RadioMessageScript` 6 条 | `BattleDriver.SpeakWarlordLine` / `EnsureLoaded` / `HideBubble` | `Speak(963)` / `OfficialChatText(918)` / `MakeBubble(606)` / `TickBubble(1216)` / `HideBubble(1161)` | 弹消息真身是 `UnitChatPanel.Speak` |
| `BattleScorer` / `BattleScoreUiManager` / `ManaTypeQPHolder` / `UIPCSkinManager` / `TouchInputManager` / `SceneCameraManager` | 属性 get / 自检口 / 类声明行 | `UpdateHud` / `HudCanvas.Ensure(90)` / `TouchInputManager.Toggle(296)` / `BuildHud` | 同上 |

### ②-2 退 B（17 条，原落点原文保留）

| 原版 | 为什么没有对应实现 |
|---|---|
| `BasicCardUI.ColliderBigSize(bool)` | 原版在「存档尺寸 ↔ 20×20」之间切碰撞体，且**全库零调用**；我们只有 `SyncHitCollider` 写的两档（手牌 1.97×3.0502 / 常态 1.97×3.02），没有 20×20 这一档 |
| `BasicCardUI.SetEmptyRarityStars()` | 原版 `SetCardRarity(0,5)` 取第 0 张「空星」图；我们建层恒按 rarity 取 1..5（default 落 common），没有「清空宝石」这条路径 |
| `BattleCardUI.ShowAvailableTargets()` | 原版把「N available」（或唯一目标卡名）斜体拼进**手牌描述**；我们全库没有这条（`CardText.TargetsAvailable` 只服务技能面板） |
| `CemeteryManager.AddPlayDropSupplyAction(...)` | 我们这一 build 没有「空投补给」发牌机制 ⇒ 无行可记（上游缺，不是日志层漏） |
| `PracticeModePopup.ChangePlayText()` | 原版按「这套卡组存没存过」改 `Battle!` 文案；我们没有「存/未存卡组」这一轴 |
| `PracticeModePopup.OnDestroy()` | 原版反注册回调；我们这扇窗没有反注册层（静态事件随对象销毁） |
| `RadioMessageScript.FadeElements(float)` | 原版整条电台消息淡入淡出；我们气泡是**显/隐硬切**（`UnitChatPanel` 全文无 alpha 通道） |
| `BattleCardManager.CreateCard(...)` | **G 判「类级命中」**：`Create`/`Build` 是整卡构建器，「卡背/埋伏态」不在这两行 |
| `BattleCardUI.BattleCardSetup(...)` | 同上 |
| `BattleCardUI.GetTraitListForCardIcons(...)` | **G 判「名字假精确」= 真缺口**：`Badges.For` 要求 `isInPlayIcon`（更严的 in-play 口径），原版只要 `hasIcon + traitIcon` |
| `BattleSettingsWindow.Awake()` | G：`Build` 是整面板构建+绑件 |
| `CardDisplayWindow.AddCardsListeners()` | G：`Build` 是整窗构建器 |
| `CardDisplayWindow.Open()` | G：`Create` 建窗，首次开窗接线/兄弟序在别处 |
| `CemeteryManager.Setup()` | G：`Create` 建整面板 |
| `MulliganManager.ProcessMulliganDone()` | G：`ClickMulliganDone` 那条不在 `Open`；最接近的 `Close` 仍是类级 |
| `TargetReticleController.Initialize()` | G：`Build` 是整件构建器 |
| `BattleHud.OffensiveButtonTransform()` | 协调台点名：`HudButtonPieceForTest` 是纯自检取名口，进攻钮没有别的读取口 |

## ③ 改账清单

- 只写 `资料/完全复刻/账/战斗界面_HUD.tsv`（LF / 无 BOM / 11 列 / 表头不动 / 原子替换）。
- 只改：状态 · 我们的落点 · 差异 · 等级 · 轮次(→R3) · 备注。**判据出处与判据要点一列没动。**
- 每批用完 `ledger_progress.py` 看形状（全程列数 ≠ 11 的行 = 0）。
- 落点统一形状：`路径:行（\`声明名\`）（等价实现 = 声明名：理由）`（理由见 §① 那条发现）。
- 轮次标 `R3`；备注追加标记 `缺注解闸 2026-10-25（P1）…`（G 批与协调台点名批各有自己的标记）。

## ④ 核验命令与结果

```
python -I -X utf8 工具/list_missing_annot.py 战斗界面_HUD   →  # 战斗界面_HUD : 0 条缺注解
python -I -X utf8 工具/verify_annot.py                      →  同名 468 · 异名已注解 1388 · 缺注解 1（唯一 1 条在「泛型与启动链_补读」域，不归我）
python -I -X utf8 工具/check_ledger_points.py 资料/完全复刻/账/战斗界面_HUD.tsv
                                                            →  A=397  通过=397  漂移=0  失败=0
python -I -X utf8 工具/ledger_progress.py                   →  战斗界面_HUD 679 行 / A397 B29 C46 D207 E0 / P0 4 · P1 10 · P2 32
```

## ⑤ 没查清的部分 / 改动请求（⛔ 我都没动代码）

1. **`Core/Badges.cs` 的 in-play 图标筛选器（G 判的真缺口）** —— 请求：在 `Badges` 里补一条**较松**的筛选（只要 `hasIcon + traitIcon`，不要求 `isInPlayIcon`），供 `BattleCardUI.GetTraitListForCardIcons` 这条路用；`Core/` 不在本代理白名单，已退 B 留账。
2. **`BattleCardUI.HideExtraEffects` 的「伤害计数」那一件** —— 我们只有飘字（`PopNumber`），没有常驻伤害计数件；注解里已如实写「单独关它无对应」。要不要补那件，属 P2。
3. **`BattleCardUI.ToggleStats(bool, bool activateArmour)` 的 `activateArmour`** —— 原版护甲框是**独立开关**；我们护甲显隐跟「护甲=0」那一条走，没有这个参数。已在注解里如实标。
4. **`RadioMessageScript.FadeElements` 的淡入淡出** —— 我们的气泡没有 α 通道（要么显要么隐）。要做需要在 `UnitChatPanel` 加一层 α（不在白名单）。
5. **`PracticeModePopup.LoadSavedDeck / IsSavedDeck` 的「原版零调用」** —— `LoadSavedDeck` 按 G 建议退 D；`IsSavedDeck` 我保留了 A（指向 `ShowYourDeckCell`，即卡组格上「Has Player Deck」火印的显隐）。若 G 认为两个都该 D，请一并裁。
6. **`TipPrebattle` 我改了 10 行不是 5 行** —— 协调台点名的是 L654–658 那 5 个方法（`CloseTip/DeactivateTip/DisplayTip/DisplayTips/GetNextTip`），但同一类的另外 5 行（`HideTips/OnTouchUpAsButton/OpenTip/RenewTip/SetupClosed`，L659–663）用的是**同一份类级证据**（整类 0 实例）。为免同一份账里出现两套说法（铁律 6），10 行一起改成 `D` + 同一段差异文字。**若裁定只该改 5 行，请回退 L659–663 的 5 行。**

## ⑥ 顺手发现（不属于本批，未改）

1. 🔴 **`BattleDriver.cs` 正在被另一个代理持续改写**（本轮实测两处位移：`UpdateHud` 30480→30567、`Begin` 4057→4848）。⇒ 建议调度台**在全部域收口后再统一跑一次 `check_ledger_points.py --fix`**，并把「落点第一个括号只放裸声明名」写进方案 §四的格式要求（否则 `--fix` 对散文括号无能为力）。
2. 🔴 **旧落点里有大量「属性 get 行」**（`BattleDriver.cs:6290` = `SettingsBtnHitPx` 的 `get { return new Vector2(...); }`、`CardView.cs:332` = `BoardMeleeGlowAt`）。这些是上一轮 grep 撞到的，**形态闸认它们「像声明」**（一行成员规则），所以一直没人发现 —— 建议形态闸补一条：`get { return new ...` 这种纯 getter **不算方法声明**。
3. `TargetReticle.cs` / `MulliganPanel.cs` / `CardChoicePanel.cs` 里有多处「落点指向方法中间一行」的旧账（例如 `:1200` 下方 `? _fxCardBtns[…]` 那一行）；本轮已把 A 行都修干净，**但 C 行可能还有同类**（C 行不过闸，不会被报出来）。
4. `ClockManager` 6 条同指一行这件事**顺便证明**：上一轮「落点收紧」只把编号改对了，**没有核方法对不对** —— 同一个方法被 6 个不同原版方法共用却全判 A。


---

# 第 2 轮跟进（2026-10-25 晚 · 协调台裁定 + `--warn-getters`）

## A. 两处裁定落地

1. **`TipPrebattle` 10 行保留 `D`**（协调台裁定：同一份类级证据覆盖整类，一份账不能两套说法）。**未回退。**
2. **`PracticeModePopup` 两条按证据收紧**
   - **`LoadSavedDeck` 仍判 `D`，但把「断在哪」补齐了**：它是 `private`，且
     · **cpp2il 全量（2,404 文件 / ≈97.7% 方法体）**：`LoadSavedDeck` / `IsSavedDeck` 两个符号**除定义行外 0 处引用**（唯一例外是 `PlayerDeckChanged` —— 它在 `PracticeModePopup.cs:709` 被当 `Action<CardDeck>` 传给 `deckInfo.SetContent`，是**活的**，两个 `*Saved*` 不是）；
     · **`decomp_full`**：本类那 12 份方法体（`Awake` / `Open` / `Close` / `DeckSelected` / `BattleButtonOnClick` / `ChangePlayText` / `OnDestroy` / `ResetContent` / `SaveSelectedDeck` / `SetArmyButtons` / `ChangeSelectedArmy` / `ConfigureArmyFilterButtons`）里**没有一处**调它。
     ⇒ 断点 = **没有任何调用者**（连本类自己都不调）；我们**不需要代替物**（原版不产生行为）。
     ⚠️ **残余风险已写进备注**：cpp2il 里本类那一段方法体**有错位**（`IsSavedDeck` 的函数体内容是 `DeckSelected` 的），所以结论**不靠单读那段文本**，靠全树符号搜索 + `decomp_full` 交叉。
     ⚠️ **另一条本想用的证据被我自己撤掉了**：我试过用「UnityEvent 的 `m_MethodName` 是落盘字符串」去搜 `assets_full`（24.7 万文件），`LoadSavedDeck` 0 命中 —— 但**对照组同样 0 命中**（`BattleButtonOnClick` / `GameModeValueChange` / `ConfigureArmyFilterButtons` 全部搜不到）⇒ **这个搜法对本类无效**（本类 prefab 的按钮不是资产里挂的），不能当第二条证据。
   - **`IsSavedDeck` → 退 `B`**：原落点 `ShowYourDeckCell` 是**UI 构建器**（卡组格 + `Has Player Deck` 火印），不是谓词实现；我们的练习窗**没有「已存卡组」这一轴**（卡组来自预组 + 编辑器），真正的等价谓词还没核清 ⇒ 按协调台口径 **B（不写 C）**。
3. **`BattleCardUI.GetTraitListForCardIcons` 保持 `B` 不动** —— P2 正在 `Core/Badges.cs` 补「较松的 in-play 图标筛选器」，等它回话再升 A（协调台明令现在别动）。

## B. 工具活：`check_ledger_points.py --warn-getters`

**做了什么**（只动 `工具/check_ledger_points.py`，账本一行没碰）：

| 项 | 内容 |
|---|---|
| 用法 | `python -I -X utf8 工具/check_ledger_points.py [账本...] --warn-getters`（可与 `--all-A` / `--fix` 组合） |
| 判据 | 对**每条 A 行**，把落点解析出的那一行（`文件:行`）判「属性 getter」：① 一行式 `public X Y { get { return …; } }`；② 表达式体属性 `public X Y => …;`；③ **getter 体那一行** `            get { return …`（名字往上最多 30 行找属性声明）。⚠️ 表达式体**方法**（`public int F() => 1;`）不报 |
| 输出 | `账名 \| 类.方法 \| 落点 \| getter \`名\``，末尾给**按账分布**与合计 |
| 退出码 | **不变**（getter 候选不算失败）。实测：HUD 账带 `--warn-getters` 跑 → `rc=0`；不带 → `rc=0` |
| 文件头 | 已写清「**只报候选，判据仍要人读**」—— 有些原版方法**本身就是取值器**（`IsFlying` / `get_Rarity`），它们的落点就该是 getter，不能一刀切判错 |
| 顺带修 | `apply_fixes()` 原来用 `io.open(p,'wb').write(表达式)`（先截断再求值，CLAUDE.md §二 点过名的坑）⇒ 改成**临时文件 + `os.replace`** 原子替换 |

**全账跑出来的 getter 候选：20 条**（写这份报告时的读数；别的代理正在改各自的账，数字会变）：

| 账 | 条数 | 候选（属性名） |
|---|---|---|
| 战斗逻辑驱动 | 9 | `ActivePlayer`×2 · `IsUnit`×2 · `StartHand` · `SecondSeat` · `Keywords` · `FaithOps` · `SpiritOps` |
| 卡组编辑 | 3 | `DeckCount` · `SlotsLeft` · `Decks` |
| 战斗界面_HUD | 3 | `State`×2 · `IsFlying`（**人读结论：保留** —— 原版 `IsFlying` / 两个过渡态本来就是取值器，落点是对应属性，`State` 属性返回的枚举逐值照原版） |
| 收藏 | 2 | `CardsVisibleCount`×2 |
| 泛型与启动链_补读 | 2 | `EndUtc` · `NextResetAt` |
| 未分类_其他 | 1 | `HudAvatarMeArt` |
| **合计** | **20** | 除 HUD 那 3 条（判定保留）外，其余 **17 条需要人读** |

> 顺带：`--warn-getters` 还把两处「结构性盲点」暴露出来 —— 形态闸原有判据把「一行式成员」当合法声明，所以
> `get { return new Vector2(…` 这种行能当 A 的落点。我在本轮**主线上**已把 HUD 这类落点全部改掉（`SettingsBtnHitPx`、
> `BoardMeleeGlowAt` 等），所以 HUD 现在只剩 3 条**合情合理**的 getter。

## C. 没处理的自检口 / 散文落点（下一步的输入）

1. **自检口当落点**（`*ForTest` / `*ForTest` 转发）：我扫到且只修了 HUD 域的 `OffensiveButtonTransform→HudButtonPieceForTest`（退 B）。**其余域没扫** —— 建议下一步用 `grep -n "ForTest" 账/*.tsv` 先出一张清单再逐条读。
2. **散文落点**（第一个括号里不是裸标识符 ⇒ `--fix` 修不了）：`卡组编辑` 53 条失败 + `收藏` 6 条 + `战役` 4 条 + `社交_聊天` 2 条 + `泛型与启动链_补读` 1 条，**共 60 条**（写这份报告时的读数）。它们的形态是 `Deck/DeckRuntime.cs:4189（等价实现 = DeckEntries：…）` —— 括号里直接写了 `等价实现` 而**没有裸声明名**，所以既不能按名重定位、行号又是旧的。
   ⇒ **修法**：套用本域同一手法（第一个括号只放裸声明名，理由挪第二个括号），再用 `--fix` 校正行号。本域已全量改成这个形状。
3. **`战役.tsv` 17 条漂移 + 4 条歧义**（`CampaignTab.cs` 多处同名声明：`BuildArmySlider` / `BuildTrack` / `ClearTrackContent`）—— 歧义那几条工具**故意不自动改**，要人判。

## D. 下一步该跑什么

```bash
# 1) 先看全账骨架（哪里在红）
python -I -X utf8 d:/4/Unity/工具/ledger_progress.py

# 2) getter 候选（只报，不改；退出码不变）
python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --warn-getters

# 3) 漂移就地修正（⚠️ 只在「全部域收口后」跑；跑前先 git status 看有没有别人在写同一批账）
python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --fix

# 4) 语义闸（缺注解/同名/异名三档）
python -I -X utf8 d:/4/Unity/工具/verify_annot.py
```

## E. 踩过的坑（下一个代理必读）

1. 🔴 **`BattleDriver.cs` / `CardView.cs` 本轮一直在被别人改，行号会漂**（实测 `UpdateHud` +87 行、`Begin` +791 行、`CardView` 一批 +23 行）。⇒ **落点一律写成** `路径:行（裸声明名）（等价实现 = 声明名：理由）`，**行号只当快照**，随时 `--fix`。
2. 🔴 **`write(<表达式>)` 会先截断文件**：本域一切写账都走「读 → 改字符串 → 临时文件 → `os.replace`」。工具里那处旧写法本轮已修。
3. 🔴 **`check_ledger_points` 的「像声明」判据会放行一行式成员**（`get { return …`）—— 那是本轮 `--warn-getters` 的由来；但**别把它当「一定是错」**，原版确实有属性型方法。
4. ⚠️ **`--fix` 只在「括号里恰好一个标识符」时才按名字重定位**；散文括号（`（等价实现 = …）` 直接写在第一个括号里）**它修不了**，会一直报「不像声明」。本轮 60 条失败全是这个形态。
5. ⚠️ **`find_decl_candidates` 会把「方法体内单独一行的方法调用」当候选**（`Awake() { Build(); }` 里的 `Build` 就是）。⇒ 规范化行号时若挑到距离为 0 的假候选，要人工核（本域遇到过 3 次：`SkillPanel.Build`、`CombatAutoZoom.ResetCameraZoomUIAction`、`BattleDriver.Begin`）。
6. ⚠️ **回归本身不够**：`verify_annot.py` 只查「注释在不在」，不查**注解与落点是不是同一个方法**。我加了一条自查脚本（`d:/4/_tmp_view/selfcheck_annot.py`，一次性）专门对「注解里的名字 vs 落点那一行的声明名」，跑出过 106 条不一致 —— **建议把这条并进 `verify_annot.py`**（下一个代理做）。

## F. 派子代理

本轮**没有派**子代理（协调台授权时我已在收尾，且账本单写手不宜并行）。全部改动都是本代理亲手做、逐批核过形状闸。
