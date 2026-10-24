# T13 · HUD 显隐条件总表（2026-10-15 · 只读普查）

- 判据顺序：① 反编译 `d:/2/tools/decomp_full/`（方法体）② 字段偏移 `d:/2/tools/il2cpp_out/dump.cs`
  ③ 场景静态值 `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena2/GameObject/*.json`（**一律 arena2**）
  ④ 运行期实况 `d:/4/Unity/资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive_0912.tsv`
  ⑤ 我们现状 `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/*.cs`
- ⛔ 未跑 Unity · 未跑原版 · 未改任何源码/正本。只写了本文件。
- ⚠️ **实况 dump 的列用法**（本表据此读）：`path` 是**父路径**、`name` 才是本节点 ⇒ 全路径 = `path + "/" + name`。
  **同一 path 会出现两个相反 activeSelf 的行**（那是两个不同子件，例：`…/PlayerMana` 下 True 的是 `QuestPointsHolder`、False 的是 `FaithHolder`）。

## 结论
1. 原版 HUD 的显隐**没有集中在一处**：散在 **14 个组件的约 60 处 `SetActive`** 里 ——
   `BattleManager`(49) · `ActiveSkillDisplay`(11) · `MulliganManager`(10) · `CemeteryManager`(9) ·
   `ClockManager`(5) · `BattleHud`/`DeckManager`/`ReplayHud`/`EndBattleDoors`/`ManaTypeHolder`(各 4) ·
   `OvertimeUi`/`PlayerProfileUIController`/`YourTurnUI`(各 2~3) · `BattleTipController`(2)。
   本表覆盖 HUD 那一棵树的主要件；列表命令：`grep -l "GameObject__SetActive" d:/2/tools/decomp_full/*.c | sed 's/__.*//' | sort | uniq -c`。
2. **绝大多数件没有「规则条件」，只有「阶段/交互条件」**（换牌 / 选牌 / 结算 / 回放 / 教程 / 按住某张牌）。
   真正由**数据或模式**决定的只有 **4 组**：三阵营资源（按军 id）· 能量累积（按 LiveOps 变量）·
   回放条（按 `matchType`）· 牌库/手牌张数（教程 stage 位 + 是否按住牌）。
3. 🔴 **场景 JSON 的 `m_IsActive` ≠ 运行时生效值**，至少 4 处不等，全部是 `Awake()` 里再关一次：
   `OvertimeIndicator`（场景 True → `OvertimeUi.Awake` 关）· `ReplayButtons/Holder`（场景 True → 按 `matchType`）·
   `TitleBackground`（场景 True → `PlayerProfileUIController.Awake` 关）· `Cardback Container`（场景 False，`DeckManager.SetupDeck` 再关一次）。
   ⇒ **照场景 JSON 建件 = 必然做出「该藏没藏」**（本工程踩过）。
4. 原版**恒不显示**的三处有确凿判据（见总表末三行）：`DebugButtons` · `BattleSettingsPanel/Debug Buttons` · `BattleSettingsPanel/Match Skulls`。
5. `Balance` 类参数（`manaAccumulation` / `overtimeTurn`）是 **LiveOps 服务端下发**、本地拿不到值 ⇒
   判据能查到、**数值查不到**（我们默认 0），这一类必须按「状态 → 参数」写，别写死一个值。

## 总表

> 路径省略前缀 `/BattlePrefab/BattlePrefab/BattleHud/`（除末三行的设置窗/结算门外）。
> 「默认 activeSelf」= **arena2 场景序列化值**；标「实况」的是 `runtime_ui_dump_drive_0912.tsv` 现场值。

| 件名 | 路径 | 默认 activeSelf | 显示条件 | 隐藏条件 | 判据（文件:行号） |
|---|---|---|---|---|---|
| BattleHud 根 / Canvas | `/BattleHud`、`/Canvas`、`/BackCanvas`、`/FrontCanvas` | True | 常显（`BackCanvas` 出人名牌与能量，`FrontCanvas` 出弹窗层） | — | `GameObject/BattleHud.json`（`m_IsActive:true`） |
| 已出牌三枚 | `…/LeftArea/CardsPlayedInTurnHolder(/1/2/3)` | False | 本回合出到第 n 张时**前 n 枚** | 每回合开头三枚全关 | `BattleHud__SetTurnPlayedCards.c:24,45`；调用点 `BattleManager._NextTurn_d__395__MoveNext.c`、`…_ResolvePlayCardFromHand_d__447__MoveNext.c` |
| 头衔底条 | `…/PlayerInfo/PlayerName/TitleBackground`、`…/EnemyName/TitleBackground` | True（**实况 True**） | **称号非空** | 称号为空 | `PlayerProfileUIController__SetProfileTitle.c:12`（`SetActive(titleGO, !IsNullOrEmpty(title))`）· `…__Awake.c`（初始关） |
| 头像上的小名 | `…/Avatar Item Small/Avatar Name` | **False** | **没查到**（反编译零赋值点） | 静态 False | `GameObject/Avatar Name.json`；搜过 `Avatar Name`/`AvatarName`/`avatarName`（decomp 全库 0 命中） |
| 里程碑骷髅 + 分数 | `…/PlayerInfo/Milestones/MatchSkulls Icon`、`/MatchSkulls Score` | True | 常显（分数由 `BattleScoreUiManager` 写字，不是 SetActive） | — | 场景静态 True；实况 True（本表 §四 顺手发现 ③） |
| 聊天/表情钮 | `…/LeftArea/PlayerInfo/ChatButton` | True（**实况 True**） | 教程局 `stage+0x2c == 0` | `SetupInitialState` **开局无条件关**；教程局 `stage+0x2c != 0` 关 | `BattleManager__SetupInitialState.c:67-70` · `BattleManager__TutorialSetup.c:59-60` · 闸门 `BattleManager__CanDisplayChatInTutorial.c:43` |
| 墓地日志面板 | `…/LeftArea/CemeteryLogPanel` | False（实况 False） | 点墓地钮 → `CemeteryManager.ShowCemeteryLogBtn`（`showingSliders(+0x90)` 取反 + 移入） | `HideCemeteryLogBtn`（`+0x90` 取反 + 移出）；开局 `SetupInitialState` 关 | `CemeteryManager__ShowCemeteryLogBtn.c:25,59,63` · `…__HideCemeteryLogBtn.c:21,25` · `BattleManager__SetupInitialState.c:105-111` |
| 墓地钮 | `…/LeftArea/EnemyInfo/ShowCemeteryBtn` | True（**实况 True**） | 常显 | **教程局无条件关** | `BattleManager__TutorialSetup.c:63-64`；`BattleManager.CanShowCemetery`（`stage+0x29`）**只闸「点开后日志能不能展开」**（`CemeteryLogManager__ClickCemeterySlider.c:75`），⛔ 不管这颗钮的显隐 |
| 进攻卡钮 | `…/LeftArea/Left Anchor/OffensiveButton` | True（实况 True） | 「进攻卡」存在 **且** 选中的卡 id ≠ 空卡 id ⇒ true | `BattleHud.Initialize` 开局关；之后随选卡态 | `BattleHud__Initialize.c:42-45` · `BattleManager._ApplyOffensiveAndDefensiveEffects_d__337__MoveNext.c:146-155` |
| 本回合已出（进攻卡） | `BattleHud+0x78` `offensiveCardTurnCardsHolder` | — | `HasPlayedThirdCardInTurn()` 为真 | 否则 | `BattleHud__SetOffensiveButtonVisible.c:45`（同族：`BattleHud.SetTurnPlayedCards`） |
| 相机复位钮 | `…/Left Anchor/CenterCameraButton` | True | `ToggleResetAutoCameraZoom(active)` 主动开 | `BattleHud.Initialize` 里 `SetActive(false)` + `DOTween.Kill` | `BattleHud__Initialize.c:68-75` · `BattleHud__ToggleResetAutoCameraZoom.c:19` |
| 设置钮 | `…/RightArea/SettingsBtn` | True | 常显（只开窗） | — | 场景静态 True；实况 True |
| 时钟 / 结束回合钮 | `…/Energy And turn holder/Clock`、`/Clock/TurnBtn` | True | 常显 | — | 场景静态 True；`ClockManager` 只改 `interactable`/文字 |
| 「没行动了」提示 | `ClockManager+0xA0` `noActionsAnim`（**运行时实例化**） | 生成物 | 非回放局 **且** `noActionsTriggered==0` **且** `clockCountdownFlag==0` 时生成并 `SetActive(true)` | 已触发过 / 在倒计时 / 回放局 | `ClockManager__SetNoActionsUi.c:21-24,38`（`matchType != 0xA0`）· 字段名 `dump.cs` `ClockManager` |
| 任务点 | `…/{PlayerMana,EnemyMana}/QuestPointsHolder` | True（实况 True） | **军 id（`RawCardScript+0x2c`）== 0x6e(110)** | 否则 | `RawCardScript__UsesQuestPoints.c` · `PlayerManager__ResetMana.c:135-140` → `ManaManager__ToggleQuestPoints.c` → `ManaTypeHolder__Toggle.c:15`（整个 GO `SetActive(bool)`，**0 值也显示**） |
| 信仰 | `…/{PlayerMana,EnemyMana}/FaithHolder` | **False**（实况 False） | 军 id == **0x50(80)** | 否则 | `RawCardScript__UsesFaith.c` · `PlayerManager__ResetMana.c:111-113` · `ManaTypeHolder__Toggle.c:15` |
| 灵魂石 | `…/{PlayerMana,EnemyMana}/SpiritStoneHolder` | **False**（实况 False） | 军 id == **0x1e(30)** | 否则 | `RawCardScript__UsesSpiritStone.c` · `PlayerManager__ResetMana.c:99-107` · `ManaTypeHolder__Toggle.c:15` |
| 能量球 | `…/{Player,Enemy}Mana/ManaHolder/Energy Player` | True | 常显 | — | 场景静态 True；实况 True |
| 能量累积（组） | `…/ManaHolder/Energy Accumulation`（+ `ON`/`OFF` + 两支 VFX） | True（实况 True） | **`0 < GameplayVariablesData.manaAccumulation`**（`vars+0x34`，LiveOps 下发） | 该值 ≤ 0 | `BattleManager__SetupBoardPhase.c:179,194` → `PlayerManager/ManaManager/ManaTypeHolder__SetAccumulationMana.c:16,20`（`manaAccumulationObj` + `manaAccumulationImageOn` 两个 GO 都 `SetActive(bool)`） |
| 加时标记 | `…/Energy And turn holder/OvertimeIndicator` | True（场景）/ **False（实况）** | 进加时那一下（`turnCounter >= overtimeTurn`） | `OvertimeUi.Awake` 里 alpha=0 + `SetActive(false)`；闪完再关 | `OvertimeUi__Awake.c:23,27,31` · `OvertimeUi__DisplayOvertime.c:19,23` · 触发 `BattleManager._NextTurn_d__395__MoveNext.c:203-207` |
| 全屏加时 splash | `FrontCanvas/Safe area FrontCanvas/AboveShader/OvertimeSplashText` | **False** | 与标记**同一句**里一起开（淡入 1s / 停 1s / 淡出 1s） | 同序列收尾 | `OvertimeUi__DisplayOvertime.c:19,23` + `__DisplayOvertime_b__5_0.c`（收尾关） |
| 回合高亮（我方/敌方） | `…/PlayerDeck/DeckAndEnergyImage/{YourTurnImage,NotYourTurnImage}` | True（**实况两枚都 True**） | `DeckManager.SetYourTurn(isYourTurn)`：`yourTurnIndicator=param` 且 `enemyTurnIndicator=!param` | 同上（互斥） | `DeckManager__SetYourTurn.c:6,8`；调用点 `BattleManager._NextTurn_d__395__MoveNext.c`。⚠️ 实况两枚都 True ⇒ 该时刻这组开关**不在 SetYourTurn 的状态上**（待核） |
| 牌库张数（我/敌） | `…/{PlayerDeck,EnemyDeck}/Player Deck Size Container(+Tex)` | True | **教程 `hideCardsLeftInDeck==0`** 且 `duringBattleFlag` 且 `UIstate != choosingResolutionCard(14)` 且 **按住某张牌** | 松手（或上面任一条不成立） | `BattleManager__CanDisplayDeckSize.c:43-46` · `BattleManager__ShowDeckSize.c:16,24,26-36` · `DeckManager__DisplayDeckSize.c:41,66,70,73` · 调用点 `CardScript__OnTouchDown.c`；收起在 `BattleManager__Update.c:140-150`（`+0x598` 标志） |
| 手牌张数 | `…/{UpperAnchor/EnemyArea,BottomAnchor/PlayerArea}/HandArea/CardsInHandText` | **False**（实况 False） | 与上一行**同一个闸**（`PlayerHand.ShowHandSize`） | 同上一行 | `PlayerHand__ShowHandSize.c:36,48`（内部先判 `BattleManager.CanDisplayDeckSize()`） |
| 牌背预览 | `…/{PlayerDeck,EnemyDeck}/Cardback Container` | **False** | 点牌堆（`DeckManager.ToggleCardbackPreview`） | `DeckManager.SetupDeck` 里 `SetActive(false)` | `DeckManager__SetupDeck.c:42`（`cardbackContainer`）+ `DeckManager__ToggleCardbackPreview.c` |
| 牌背参考件 | `…/PlayerDeck/CardBack For Reference(/CardBackShadow)` | True / **False** | 参考件（`CardBackShadow` 恒关） | — | 场景静态值 |
| 回放条 | `…/ReplayButtons`（开关做在其子件 `Holder` 上） | True（场景）/ 实况 `Holder` True | **`matchType == 0xA0`（回放局）** | 非回放局 | `ReplayHud__Setup.c:76`（`objHolder.SetActive(matchType == 0xA0)`）；`BattleHud+0x88` = `replayHud` |
| 回放四钮 | `…/ReplayButtons/Holder/{Replay,Play,Pause,StepPlay}` | `Replay/Play/StepPlay`=True、`Pause`=**False** | `ReplayHud.Initialize()` 跑完 = **关 Play、开 Pause** | 场景里那份（`Play=T/Pause=F`）是「进战场前」的样子 | 工程已核 → `资料/普查产出_0927/回放_界面真值.md`（本表复核场景值一致） |
| 手牌上限参考线 | `…/Safe area BackCanvas/HandLimitArea` | True（实况 True） | ⚠️ **它不是可见件** —— 1×1 的空节点，只被当**一条 y 参考线**读 `transform.position` | — | `GameObject/HandLimitArea_141.json`（1×1）· 消费点 `BattleManager__Update.c:153-155`；工程已核 → `Hand/CardInteraction.cs:267-291` |
| 等待对手（换牌后） | `…/Safe area BackCanvas/WaitText` | **False**（实况 False） | 我换牌确认完、对手还没确认（`mulliganWaitText`，`BattleManager+0x1F0`）⇒ `SetActive(true)` | 对手确认后（`ReceiveEnemyMulliganAndEnvEffect`/开局初始化）关 | `BattleManager__ClickChosenCardDone.c:95-98` · `BattleManager__SetupInitialState.c:189-193` · `MulliganManager__SetWaitingForEnemy.c:16`（同时把换牌文字打开） |
| 压暗层 | `FrontCanvas/ShadePanel` / `BattleManager.shade(+0x230)` | **False**（实况 False） | 只查到 `Shade.SwitchShade(bool)` 这条路（选牌/结算那一段） | — | `BattleManager__ClickChosenCardDone.c`（`0x230`）· 静态 False。⚠️ 我们**没有**建 `ShadePanel`（见「我们现状」） |
| 头顶提示 | `FrontCanvas/Safe area FrontCanvas/AboveShader/TipText` | **False** | 提示队列非空 ⇒ 播一条（`BattleTipController._DisplayTip` 里 `SetActive(true)`） | 播完 / 队列空 `SetActive(false)` | `BattleTipController._DisplayTip_d__14__MoveNext.c:67,108`（`tipsGameObject`=`BattleTipController+0x28`）· 入队口 `BattleTipController__ShowHeadsUpMessage.c` |
| 攻击类型选择器 | `FrontCanvas/Safe area FrontCanvas/Drag Attack Selector(/Select Attack Background)` | 根 True、背景 **False** | 选中场上单位后出现（近战/远程/技能/技能钮四枚） | 取消选中 | `Select Attack Background` 静态 False；工程侧对账 → `Battle/AttackSelector.cs` |
| 技能条三态 | `FrontCanvas/Safe area FrontCanvas/ActiveSkillDesc` + `Lights/{LightActing,LightAvailable,LightPressed}` + `AbilityContainer` + `TargetsAvailableText` | 根 True；`AbilityContainer`/三枚 Light/`TargetsAvailableText` **全 False**（实况 `ActiveSkillDesc` False） | `bgLight`(LightAvailable) = `BattleManager.CanUseActiveAbility(卡, true)`；`actingLight`(LightActing) = `ShowActingLight()` 时开；`pressingLight`(LightPressed) = 按下时开、抬起/移出/FadeOut 关 | `InitialSetup()` 关 `pressingLight` + `hideSkillBtn`；`FadeOut()` 关 `pressingLight` | `ActiveSkillDisplay__InitialSetup.c:11,15` · `…__SetupActiveSkill.c`（`CanUseActiveAbility` 那一段）· `…__ShowActingLight.c:25` · `…__FadeOut.c:25,41` · `…__FadeIn.c` · `BattleManager__SetupInitialState.c:75-80` |
| 单位语音条 | `FrontCanvas/Safe area FrontCanvas/Unit Chat/{PlayerChatDisplay,EnemyChatDisplay}` | True | 有台词/语音时（工程侧 `Battle/UnitChatPanel.cs`） | — | 场景静态 True；实况 True |
| 表情弹窗 | `…/Unit Chat/ChatPopup` | **False** | 点 ChatButton ⇒ `VoiceLinesController.ClickChat`（受 `UIstate` 闸：`inactive/finishBattleSequence/postBattle/mulligan*` 等不开） | 关窗 | `BattleManager__ClickChat.c`（`UIstate` 判据在其头部） |
| 电台聊天 | `…/Unit Chat/RadioChat` | **False** | **没查到**（反编译零赋值点） | 静态 False | `GameObject/RadioChat.json`；搜过 `RadioChat`（decomp 只有 `RadioChatAudioSource`） |
| 换牌面板 | `FrontCanvas/Safe area FrontCanvas/Mulligan(/MulliganText,/ButtonsGroup)` | True（场景） | `ActivateMulligan(goFirst)` → `buttonsGroup=true` + `mulliganTextObj=true`；`ShowMulliganElements(true)` 全开 | `Setup()` / `StopMulligan()` → 两者 false | `MulliganManager__Setup.c:10,12` · `…__ActivateMulligan.c:30,42` · `…__ShowMulliganElements.c:10,12` · `…__StopMulligan.c:11,15` |
| 一次摊开多张 | `…/Generic Multi Card Display Combat` | **False** | 卡效果一次摊多张时（`BattleManager+0xA0` `multiCardDisplay`） | 点 Continue | 静态 False；`BattleHud__DisplayMultiCardView.c` |
| 选牌菜单 | `…/ChooseCardMenu` | **False** | 效果要求「替对手/自己选一张」时 | 选完 | `BattleManager__SetupInitialState.c:184-188`（开局关）· `BattleManager__ChoiceOfCardPlayer.c` |
| 大卡展示窗 | `…/Card Display Window` | **False** | `BattleManager.DisplayCard()`；**教程 `stage+0x2b != 0` ⇒ 直接 return（不开）** | `ClosePopupsAfterBattleEnd` / 关窗 | `BattleManager__SetupInitialState.c:71-74` · `BattleManager__DisplayCard.c:48,59-70` |
| 教程指针/提示 | `…/Tutorial/*`、`Tutorial/{InitTutorialTip,TutorialTip}` 下的 `TipText` | True（`InitTutorialTip`）/ True | 教程局对应 stage | 非教程局 / stage 走完 | `BattleManager__SetupInitialState.c:81-85`（开局关 `tutorialTip`）· `BattleManager__TutorialSetup.c:33,44` |
| 结算门（视频+奖励） | `Canvas/BattleDoors(/Canvas/EndBattlePanel)` | **False**（实况 False） | 结算序列里 `_CloseBattleDoors` `SetActive(true)` | `_BasicBattleEndSequence` / 战斗结束收场关 | `BattleManager._CloseBattleDoors_d__393__MoveNext.c`（`SetActive(lVar5,1,0)`）· `…_BasicBattleEndSequence_d__391__MoveNext.c`（关） |
| 加时/胜负额外部件 | `BattleDoors/Canvas/EndBattlePanel/{AllRewardsHolder,Video Image,...}` | True | 门一开全在（`EndBattleDoors.HideRewardsObjects`/`ShowRewards` 再逐件收放） | — | `EndBattleDoors__Awake.c:7-10` · `__HideRewardsObjects.c` · `__ShowRewards.c` |
| ⛔ Debug Buttons | `FrontCanvas/Safe area FrontCanvas/DebugButtons` | **False** | **原版恒不显示** | 两处**无条件** `SetActive(false)` | `BattleManager__SetupTestButtons.c:6`（整个方法只有这一句）· `BattleManager__StartBattleManager.c:76-77`（开局无条件关）· 场景静态 False |
| ⛔ 设置窗 Debug Buttons | `FrontCanvas/…/BattleSettingsPanel/Debug Buttons` | True（**但父窗 `BattleSettingsPanel` = False**） | **恒不显示** | 同上（父窗关着） | `GameObject/Battle Settings Panel.json`（外层 `m_IsActive=false`）· 反编译里 `Debug Buttons` 零独立赋值点（搜过 `DebugButtons`/`devButtons`/`+0x238`） |
| ⛔ 设置窗 Match Skulls 那一行 | `FrontCanvas/…/BattleSettingsPanel/Match Skulls` | **False**（子件 `Match Skulls` 是 True） | **恒不显示** | 静态 False + 反编译零赋值点 | `GameObject/Match Skulls.json`（外层 False/内层 True）· 搜过 `MatchSkulls`/`Match Skulls`/`MatchSkullsScore`（decomp 0 命中 SetActive） |

## 我们现状（逐件：有这个开关吗？行为一样吗？）

> 下标一律 `Battle/BattleDriver.cs`（行号 = 2026-10-15 现读），其余文件另注。

| 件 | 我们有吗 | 行为一样吗 |
|---|---|---|
| 已出牌三枚 | ✅ `_playedPips`（`:1286`，`:13653` 逐枚关） | ✅ 一样（每回合全关→按张数开） |
| 头衔底条 | ✅ `:13984`（`SetActive(titleGO, 非空)`，连底条一起关） | ✅ 一样 |
| 头像上的小名 `Avatar Name` | ❓ 未见对应件 | — 原版就是恒 False，**无行为差异** |
| 聊天钮 | ✅ `_chatBtn`（`:1168`，教程闸 `:9614`） | ⚠️ **部分**：我们按 `hideChat` 走；**原版那条「普通局谁把它重新打开」我们也没查到** ⇒ 两边口径都欠 |
| 墓地日志面板 | ✅ `_logPanel`（`BattleLogPanel`，`:872`，`ShadeActive` 在 `BattleLogPanel.cs:288`） | ✅ 一样（默认关、点开） |
| 墓地钮 | ✅ `_cemeteryBtn`（`:874`，`:9617`） | ✅ 一样（教程局无条件藏） |
| 进攻卡钮 | ✅ `_offensiveBtn`（`:1173`、`:2911` 开局关、`:5341` 按选卡） | ✅ 一样 |
| 相机复位钮 | ✅ `_cameraResetBtn`（`:1181`、`:14166` 开局关；命中带 `activeSelf` 守卫 `:3559`） | ✅ 一样 |
| 设置钮 / 时钟 / 回合钮 | ✅ `_settingsBtn`（`:1162`）等 | ✅ 一样（`Clock/TurnBtn` 对账见 `B1:97`） |
| 三阵营资源 | ✅ `_myQuestIcon/_myQuestJoin/_myFaith*/_myStone*`（`:13385`、`:6585`、`:14286`） | ✅ 一样（按军 id；0 值也显示） |
| 能量累积 | ⚠️ 建了但**恒显示**：`:14323-14325` 只按 `manaAccumulation` **换 ON/OFF 那张图**，**没做整组 `SetActive`** | 🔴 **不一样**：原版该值 ≤ 0 时**整组不显示**；我们默认 0 ⇒ 原版默认看不见、我们默认看得见（贴 `40k_battle_energy_empty`）。判据见上表该行 |
| 加时标记 / splash | ✅ `_overtime`（`:1071`、`:14342` 默认关、`:14455` 开）· `_overtimeSplashRoot`（`:1073`、`:14425`/`:14459`） | ✅ 一样（Awake 关 + 加时那一下闪） |
| 回合高亮 | ✅ 有（`:10266-10270` 逐侧互斥） | ✅ 语义一样；⚠️ **原版实况两枚同时 True**（上表已记「待核」），我们互斥 —— **判据未定，暂不改** |
| 牌库/手牌张数 | ✅ `_pileLabel/_foePileLabel`（`:1272`）+ `_handLabel/_handPlate`（`:9611`），教程闸已接 | 🔴 **不一样**：「**按住牌才显示**」那一段我们**没做**（我们常显）。原版判据见上表（`CardScript__OnTouchDown` → `ShowDeckSize`；`BattleManager__Update.c:140-150` 收起） |
| 牌背预览 | ✅（收藏窗卡背格 + 战斗牌堆，见 `战斗UI_原版对账表.md` 三·〇） | ✅ 一样（默认关） |
| 回放条 | ✅ `ReplayBar.cs` + `:13202`/`:13702` 的 `matchType == 0xA0` 口径 | ✅ 一样（开关做在 `Holder` 那一层） |
| 手牌上限参考线 | ❌ **未建**（我们只用了 `BoardLayout.lineY` 那条自定线，见 `Hand/CardInteraction.cs:283-291`） | ⚠️ 视觉无差（原版是 1×1 空节点）；**但拖拽判据用的 y 不同**（原版距顶 780 px vs 我们 372 px）—— 已挂账 `A2104①` |
| 等待对手 | ✅ `_waitBanner`（`:1050`、`:15280`） | ⚠️ **触发时机是我们挑的**（原版 `WaitText` 的驱动在反编译里只查到 `ClickChosenCardDone` 那一处 ⇒ 原版叫 **`mulliganWaitText`**；我们是「不是我的回合就显示」）。见 `Battle/WaitBanner.cs:11-13` |
| 📌 **2026-10-24 更正** | — | 🔴 **本表 `:64` 与 `:106` 打架，以 `:64` 为唯一出处**：`mulliganWaitText` 的判据逐字是「**我确认完换牌、对手还没确认 ⇒ `SetActive(true)`**」（`BattleManager__ClickChosenCardDone.c:55,93-98`；关闭四处 `ReceiveEnemyMulligan…` / `_SetupEnviromentalEffectPhase` / `_StartBattlePhase` / `SetupInitialState`）；**单机局恒不出现**（`_SetupEnviromentalEffectPhase:32-46` 在非联网支把 `receivedEnemyMulligan` 强置 1）。⇒ 上一行「触发时机是我们挑的」**作废**；判据全文 → `普查产出_1024d/N1_敌方回合横幅与压暗.md`，已由 `W1` 落地（静态纯函数 `ShowWaitBanner`）。 |
| 压暗层 `ShadePanel` | ❌ **没有 HUD 级 shade**（只有 `BattleLogPanel` 自己那颗 `_shade`） | ⚠️ **缺件**：原版 `FrontCanvas/ShadePanel` + `BattleManager.shade(+0x230)` 在选牌/结算那一段压暗；我们靠别的层 |
| 头顶提示 `TipText` | ✅ `_tipText`（`:1019`、`:9112`）+ 队列（`:9085` 段注释） | ✅ 一样（队列非空开、播完关） |
| 攻击类型选择器 | ✅ `AttackSelector.cs` | ✅ 一样 |
| 技能条三态 | ✅ `SkillPanel.cs` / `ActiveSkillDisplay` 对应件 | ✅ 三态语义已接（工程侧另有逐格对账 `B2`/`C3`） |
| 单位语音条 | ✅ `UnitChatPanel.cs` | ✅ 一样 |
| 表情弹窗 | ✅ `_chatPopup`（`:1193`、`:3906`） | ✅ 一样 |
| 电台聊天 `RadioChat` | ❓ 未见对应件 | — 原版静态 False、零赋值点 ⇒ **无行为差异** |
| 换牌面板 | ✅ `MulliganPanel.cs` | ✅ 一样（`Setup`/`Activate`/`Stop` 三段都在） |
| 一次摊开多张 / 选牌 / 大卡展示 | ✅ `_multiCards`（`:1205`）· `ChoosePanel.cs` · `_cardDisplay`（`:1195`） | ✅ 一样（含教程 `hideLargeCardDisplay` 闸 `:9638-9640`） |
| 结算门 | ✅ `BattleDoors.cs` | ✅ 一样（默认关、结算时开） |
| Debug Buttons / 设置窗 Debug Buttons / 设置窗 Match Skulls | ✅ 未建（`Battle/SettingsPanel.cs:748` 明记「没建」） | ✅ **一致**（原版恒不显示） |

## 没查清的 / 判不出（⛔ 不许猜）

1. **`ChatButton` 在普通（非教程）局被谁打开** —— 反编译里对 `BattleManager.chatButton`(`+0x140`) 只有两处：
   `SetupInitialState.c:67-70`（关）与 `TutorialSetup.c:59-60`（按 `stage+0x2c`）。
   **但实况 dump 里它是 True**（`runtime_ui_dump_drive_0912.tsv`，全路径 `…/LeftArea/PlayerInfo/ChatButton`）。
   ⇒ 一定还有一条打开路径没找到。
   **我搜过**：`BattleManager__*.c` 里 `0x140` 的全部命中（3 个文件）· `param_1 + 0x140)`/`0x140)` ·
   方法名含 `Chat` 的全部（`ClickChat`/`DisplayBigChat`/`CanDisplayChatInTutorial`/`AiScripted__CloseChatBoxes`）·
   `BattleCommsManager__*`· `VoiceLinesController__*`· `ChatPopup`（那是**表情弹窗**不是那颗钮）·
   `PlayerProfileUIController`（它的 5 个字段里没有 chat 钮）。
2. **`FUN_180617800()`（收起牌库/手牌张数的那半个条件）是哪个方法** —— `BattleManager__Update.c:140`
   用它，但 `d:/2/tools/decomp_full/` 里没有以它为名的 `.c`，`dump.cs` 里也没有 `RVA: 0x617800` 这一条
   ⇒ **它没被反编译出来**。语义（「松手/不再持有」）是**推断**，⛔ 我不当判据用。
3. **`Avatar Name` / `RadioChat` 的显示条件**：两件**静态 False + 反编译零赋值点**。
   我搜过 `Avatar Name`/`AvatarName`/`avatarName`/`RadioChat`/`Radio`（decomp 全库）。
   ⇒ 结论是「**原版恒不显示**」还是「**驱动在别处（服务端/被裁代码）**」，**判不出**（本表按「原版恒不显示」写，但与 `ChatButton` 那条同族，建议一起真机核）。
4. **`DeckManager.SetYourTurn` 的实况**：实况 dump 里 `YourTurnImage` 与 `NotYourTurnImage` **两枚都 True**，
   与 `SetYourTurn`（互斥）对不上。**没查到**是 dump 时刻的问题还是真有第二处赋值 ——
   搜过 `SetYourTurn`（decomp 只有 `DeckManager__SetYourTurn.c` + `BattleManager._NextTurn_d__395__MoveNext.c` 两个命中）。
5. **`Match Skulls`（设置窗那一行）为什么外层静态就是 False** —— 全量反编译零赋值点，
   但外层父窗 `BattleSettingsPanel` 也是 False ⇒ **无法区分「本来就不显示」与「父窗关着所以看不见」**。
6. **`Shade`/`ShadePanel` 的打开条件**未逐条追（原版有 `Shade.SwitchShade`，调用点散在选牌/结算里）；
   本表只记「静态 False + 我们不建」。

## 顺手发现（现场看到、不在本任务范围内 —— 照实报，⛔ 没改任何东西）

1. 🔴 **实况 dump 的 `path` 列是【父路径】、`name` 才是本节点** ——
   `runtime_ui_dump_*.tsv` 里同一 `path` 会连着出现两个相反 `activeSelf` 的行（例：
   `…/Energy And turn holder/PlayerMana` 下 True 的是 `QuestPointsHolder`、False 的是 `FaithHolder`）。
   **只按 `path` 读 activeSelf ⇒ 会得到「同一节点既是 True 又是 False」的假矛盾**。
   建议把这一条写进 `资料/已知的坑.md`（本表已按正确读法取值）。
2. 🔴 **`Energy Accumulation` 判据已查实，但我们的代码只用了它的一半**（换图、没换显隐）——
   见「我们现状」那一行。这是一条**能直接验收的「该藏没藏」**（默认 `manaAccumulation = 0` ⇒
   原版看不见、我们看得见）。判据全在 `BattleDriver.cs:14314-14325` 的注释里，**只差把 `SetActive` 补上**。
3. **`Milestones`（里程碑骷髅+分数）在原版是「常显 + 只改字」**（静态 True、零 SetActive），
   而 `资料/战斗UI_原版对账表.md:195-196` 的表格把它和几个「本回合已出牌」的件排在一起 ⇒
   容易让人以为它也要按回合开关。⛔ 别把它加进「按回合藏」那一族。
4. **`CemeteryManager` 那对 `ShowCemeteryLogBtn`/`HideCemeteryLogBtn` 改的是「日志面板」不是「那颗钮」** ——
   方法名带 `Btn` 但 `+0x50`=`shade`、`+0x58`=`showingCardUI`。**照名字理解会接错**，
   钮本身(`ShowCemeteryBtn`)的显隐只在 `TutorialSetup` 里出现一次。
5. **`BattleHud.Initialize` 与 `BattleManager.SetupInitialState` 是两份「开局归零」清单**，件不重合：
   前者关 `offensiveCardButton`+`resetCameraZoomButton`，后者关 `chatButton`/`cardToDisplay`/`activeSkillDesc`/
   `tutorialTip`/`mulliganManager`/`cemeteryManager`/`chooseCardManager`/`mulliganWaitText`。
   ⇒ 查「某件开局该不该关」时**两份都要看**（只查一份会漏）。
