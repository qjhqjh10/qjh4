# B148 · SECOND（次要落点）重分类

> 工具：`python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --warn-getters`（纯只读）＋ 本表用的重分类脚本（复用同一个模块的判定函数，口径一致）。
> ⚠️ **只重分类、⛔ 不重钉**：本文件一行账都没改；`--fix` 未跑（它只管 A 行主落点，而且现在有别的写手在飞、行号还在漂）。

## 一、总数

| 项 | 数 |
|---|---:|
| **A 档 SECOND 条数（本项口径）** | **402** |
| B 档 SECOND（只报告，不进本项） | 1 |

> 🕒 生成时刻 = **2026-11-05 23:34**（本机）。⚠️ 本波 W2/W3/W4 正在改 `.cs`，**同一天内重跑会漂**（2026-11-05 实测 15 分钟内 389 → 390 → 383）⇒ 引用本表时以「生成时刻」为准。

## 二、四类分布

| 类 | 含义 | 条数 |
|---|---|---:|
| A | 名字**就在那一行**（告警是散文误报）⇒ 无需动 | 0 |
| B | **全文件唯一一处声明行** ⇒ **可机械重钉**（唯一能自动改的一类） | 311 |
| C | 多处声明候选（歧义）⇒ 必须人读 | 9 |
| D | 不可解析 / 无声明行 / 名字 0 命中 ⇒ 必须人读（多为原版源码引用） | 82 |
| **合计** | | **402** |

## 三、B 类逐条（**真正可机械重钉**）

| # | 账（文件:行） | 原版类.方法 | 旧 ref | 符号 | 新落点 | 交叉核 |
|---:|---|---|---|---|---|---|
| 1 | 个人资料_成就_排行.tsv:162 | `PlayerProfileUIController.OnCollectorLevelUpdate(CollectorLevelChangeSignal sig)` | `Shell/ProfileTab.cs:1624` | `RefreshIdentity` | `ProfileTab.cs:1631` | 新行有/旧行无 |
| 2 | 个人资料_成就_排行.tsv:198 | `RankedArmyBonusContainer.OnInitialize(LiveOpsEvent evt)` | `Shell/MainMenuRuntime.cs:5171` | `PlaceRankedArmyBonusCard` | `MainMenuRuntime.cs:5198` | 新行有/旧行无 |
| 3 | 其他_基础设施.tsv:340 | `PlayerDataManager.AddCollectorXp(int grantedXp, bool showToast)` | `CardPresentation/Shell/CardTierConfig.cs:205` | `PlayerLevel` | `CardTierConfig.cs:206` | 新行有/旧行无 |
| 4 | 其他_基础设施.tsv:578 | `PlayerManager.GetUnusedMana()` | `RuleEngine/Core/RuleCore.cs:13082` | `SpendNormalMana` | `RuleCore.cs:13284` | 新行有/旧行无 |
| 5 | 其他_基础设施.tsv:586 | `PlayerManager.SetQuestPointsWithDelay(float delay, Action finishCallback)` | `CardPresentation/Battle/BattleDriver.cs:33993` | `TickManaHolderValue` | `BattleDriver.cs:34034` | 新行有/旧行无 |
| 6 | 其他_基础设施.tsv:587 | `PlayerManager.SetSavedMana(int value, bool useNumberChange)` | `RuleEngine/Core/RuleCore.cs:1786` | `BeginTurnCore` | `RuleCore.cs:1705` | 新行有/旧行无 |
| 7 | 卡牌面_渲染.tsv:44 | `CardHighlight.Start()` | `Core/CardView.cs:8189` | `SetHighlight` | `CardView.cs:8432` | 新行有/旧行无 |
| 8 | 卡牌面_渲染.tsv:44 | `CardHighlight.Start()` | `Core/CardView.cs:10482` | `BaseScale` | `CardView.cs:10520` | 新行有/旧行无 |
| 9 | 卡组编辑.tsv:16 | `CardDeck.DeserializeDeckString(string deckString)` | `CardPresentation/Shell/CollectionData.cs:637` | `ParseDeck` | `CollectionData.cs:639` | 新行有/旧行无 |
| 10 | 卡组编辑.tsv:113 | `DeckEditingWindow.TryOpen(object data, GameWindowOptions options)` | `CardPresentation/Deck/DeckRuntime.cs:1397` | `Build` | `DeckRuntime.cs:1276` | 新行有/旧行无 |
| 11 | 卡组编辑.tsv:131 | `DeckEnergyCostDrawer.Initialize(IEnumerable<RawCardScript> cards)` | `Shell/DraftModeWindow.cs:3059` | `BuildDeckInfoContent` | `DraftModeWindow.cs:3075` | 新行有/旧行无 |
| 12 | 卡组编辑.tsv:170 | `DeckInfoPopup._003CDeleteDeck_003Eb__27_3(string _)` | `Shell/CollectionData.cs:553` | `RestoreDeck` | `CollectionData.cs:555` | 新行有/旧行无 |
| 13 | 卡组编辑.tsv:328 | `DeckRuntime.UpdateButtonHover()` | `Shell/PointerLayer.cs:1663` | `CollectHits` | `PointerLayer.cs:1681` | 新行有/旧行无 |
| 14 | 卡组编辑.tsv:328 | `DeckRuntime.UpdateButtonHover()` | `Shell/PointerLayer.cs:1769` | `YieldToHost` | `PointerLayer.cs:1787` | 新行有/旧行无 |
| 15 | 商店.tsv:37 | `CampaignPremiumPointsPanel.CollectButtonClicked()` | `Shell/WindowsManager.cs:1263` | `OpenPurchasePremiumWindow` | `WindowsManager.cs:1286` | 新行有/旧行无 |
| 16 | 商店.tsv:203 | `PurchasePremiumWindow.TryPurchase(Action<string> onPurchase, Action<string> onFa` | `Shell/ShopData.cs:472` | `Buy` | `ShopData.cs:479` | 新行有/旧行无 |
| 17 | 商店.tsv:203 | `PurchasePremiumWindow.TryPurchase(Action<string> onPurchase, Action<string> onFa` | `Shell/ShopTabPage.cs:1278` | `OnPurchaseFailed` | `ShopTabPage.cs:1345` | 新行有/旧行无 |
| 18 | 商店.tsv:203 | `PurchasePremiumWindow.TryPurchase(Action<string> onPurchase, Action<string> onFa` | `Shell/ShopData.cs:708` | `BuyNotFound` | `ShopData.cs:724` | 新行有/旧行无 |
| 19 | 商店.tsv:297 | `ShopStore.TryPurchase(IShopOffer offer, Action onComplete, Action onError)` | `Shell/ShopData.cs:472` | `Buy` | `ShopData.cs:479` | 新行有/旧行无 |
| 20 | 商店.tsv:297 | `ShopStore.TryPurchase(IShopOffer offer, Action onComplete, Action onError)` | `Shell/ShopTabPage.cs:714` | `Buy` | `ShopTabPage.cs:781` | 新行有/旧行无 |
| 21 | 商店.tsv:344 | `PriceDisplayButton.set_Interactable(bool value)` | `CardPresentation/Shell/WindowButton.cs:863` | `SetSoftDisabled` | `WindowButton.cs:969` | 新行有/旧行无 |
| 22 | 商店.tsv:349 | `PriceDisplayButton.get_Interactable()` | `CardPresentation/Shell/WindowButton.cs:844` | `Interactable` | `WindowButton.cs:946` | 新行有/旧行无 |
| 23 | 战斗界面_HUD.tsv:9 | `ActiveSkillDisplay.FinishActing()` | `CardPresentation/Battle/BattleDriver.cs:14853` | `CancelTargetChoosingByInvalidClick` | `BattleDriver.cs:14911` | 新行有/旧行无 |
| 24 | 战斗界面_HUD.tsv:34 | `BasicCardUI.ColliderBigSize(bool value)` | `CardPresentation/Core/CardView.cs:9406` | `HitRect` | `CardView.cs:9656` | 新行有/旧行无 |
| 25 | 战斗界面_HUD.tsv:102 | `BattleCardUI.CardHandToBoardAnimationFinished()` | `CardPresentation/Core/CardFeel.cs:4480` | `ResetHandToBoardResidue` | `CardFeel.cs:4485` | 新行有/旧行无 |
| 26 | 战斗界面_HUD.tsv:108 | `BattleCardUI.ChooseDissolveMaterial()` | `CardPresentation/Core/CardView.cs:7173` | `BeginDissolve` | `CardView.cs:7199` | 新行有/旧行无 |
| 27 | 战斗界面_HUD.tsv:182 | `BattleErrorUIManager.OpponentDesyncCancelMatch(bool MatchWon)` | `CardPresentation/Net/NetBattle.cs:1824` | `LeaveWithoutResult` | `NetBattle.cs:1892` | 新行有/旧行无 |
| 28 | 战斗界面_HUD.tsv:396 | `ClockManager.GetTotalTime()` | `CardPresentation/Battle/BattleDriver.cs:42161` | `TurnSecondsFor` | `BattleDriver.cs:42202` | 新行有/旧行无 |
| 29 | 战斗界面_HUD.tsv:537 | `ManaManager.SetManaSaved(int savedMana, int unUsedMana, int totalMana, bool useN` | `CardPresentation/Battle/BattleDriver.cs:36449` | `SyncSavedManaToHud` | `BattleDriver.cs:36585` | 新行有/旧行无 |
| 30 | 战斗界面_HUD.tsv:537 | `ManaManager.SetManaSaved(int savedMana, int unUsedMana, int totalMana, bool useN` | `CardPresentation/Battle/BattleDriver.cs:36475` | `EnergyShownOf` | `BattleDriver.cs:36611` | 新行有/旧行无 |
| 31 | 战斗界面_HUD.tsv:693 | `BattleManager.CanDisplayHandCard(CardScript card)` | `CardPresentation/Battle/BattleDriver.cs:13317` | `OnCardTapped` | `BattleDriver.cs:13324` | 新行有/旧行无 |
| 32 | 战斗界面_HUD.tsv:698 | `BattleManager.CheckForTutorialPointer()` | `CardPresentation/Battle/TutorialOverlay.cs:925` | `SetTip` | `TutorialOverlay.cs:918` | 新行有/旧行无 |
| 33 | 战斗界面_HUD.tsv:700 | `BattleManager.CloseBattleDoors(MatchResultData matchResultData)` | `CardPresentation/Battle/BattleDoors.cs:434` | `Play` | `BattleDoors.cs:440` | 新行有/旧行无 |
| 34 | 战斗界面_HUD.tsv:712 | `BattleManager.GetUIState()` | `CardPresentation/Battle/BattleDriver.cs:36373` | `ChatHotkeyUiState` | `BattleDriver.cs:36414` | 新行有/旧行无 |
| 35 | 战斗界面_HUD.tsv:734 | `BattleManager.StartSpellFrom(CardScript card, Vector2 pos)` | `CardPresentation/Battle/BattleDriver.cs:8234` | `HighlightPlayTargets` | `BattleDriver.cs:8247` | 新行有/旧行无 |
| 36 | 战斗界面_HUD.tsv:734 | `BattleManager.StartSpellFrom(CardScript card, Vector2 pos)` | `CardPresentation/Battle/BattleDriver.cs:36229` | `UpdateSpellDragReticle` | `BattleDriver.cs:36374` | 新行有/旧行无 |
| 37 | 战斗界面_HUD.tsv:741 | `BattleManager.Update()` | `CardPresentation/Hand/CardInteraction.cs:2705` | `TickDeployCancel` | `CardInteraction.cs:2706` | 新行有/旧行无 |
| 38 | 战斗界面_HUD.tsv:765 | `CardScript.ResolveRangedAttack(CardScript target)` | `CardPresentation/Core/CardFeel.cs:2264` | `AttackRecover` | `CardFeel.cs:2277` | 新行有/旧行无 |
| 39 | 战斗界面_HUD.tsv:780 | `CardScript.UpdateMinionInPlayPosition(Vector3 pos, float reassembleTime, bool do` | `CardPresentation/Battle/BattleDriver.cs:26358` | `PoseFor` | `BattleDriver.cs:25844` | 新行有/旧行无 |
| 40 | 战斗界面_HUD.tsv:790 | `MinionManager.SetCardShadow(Vector2 pos, bool showY, bool boardPosition)` | `CardPresentation/Hand/CardInteraction.cs:1092` | `ShowDropIndicator` | `CardInteraction.cs:1117` | 新行有/旧行无 |
| 41 | 战斗界面_HUD.tsv:792 | `MinionScaleByPositionSO.GetScale(bool isPlayer, Vector3 localPosition, Vector3 i` | `CardPresentation/Battle/Card3DAnimationController.cs:99` | `DepthScaleByBoardPosition` | `Card3DAnimationController.cs:96` | 新行有/旧行无 |
| 42 | 战斗界面_HUD.tsv:798 | `RawCardScript.IsAlternateArt()` | `CardPresentation/Deck/DeckRuntime.cs:4237` | `IsAltArtCard` | `DeckRuntime.cs:4238` | 新行有/旧行无 |
| 43 | 战斗界面_HUD.tsv:820 | `（无原版对应 · 我们的督军变身演出）.BattleDriver.PlayWarlordTransformCore(BattleEvent e, bool fo` | `CardPresentation/Battle/BattleDriver.cs:41881` | `PlayWarlordTransformFeel` | `BattleDriver.cs:42012` | 新行有/旧行无 |
| 44 | 战斗逻辑驱动.tsv:6 | `AI.ExecuteAction(BattleManager manager, AvailableAction action)` | `RuleEngine/Data/SimpleAI.cs:1454` | `ExecuteAction` | `SimpleAI.cs:1647` | 新行有/旧行无 |
| 45 | 战斗逻辑驱动.tsv:19 | `AI.ScoreFromBuffedAttack(CardScript actingCard, CardScript targetCard, CardEffec` | `RuleEngine/Data/AiScoring.cs:542` | `BestAttackScore` | `AiScoring.cs:541` | 新行有/旧行无 |
| 46 | 战斗逻辑驱动.tsv:77 | `AbilityLogic.GetValueFromCriteria(BattleManager manager, int minValue, int maxVa` | `RuleEngine/Core/EffectResolver.cs:14323` | `IsMinPlusMaxTimesMultiplier` | `EffectResolver.cs:18205` | 新行有/旧行无 |
| 47 | 战斗逻辑驱动.tsv:94 | `AiScripted.ExecuteAction(BattleManager manager, ScriptedActionCampaignData actio` | `CardPresentation/Battle/BattleDriver.cs:9126` | `BeginBattleAfterSetup` | `BattleDriver.cs:9145` | 新行有/旧行无 |
| 48 | 战斗逻辑驱动.tsv:132 | `BattleManager.AddDrawCard(bool isPlayer, int priority, bool checkIfCasterInPlay,` | `CardPresentation/Battle/BattleDriver.cs:41532` | `DrawStaggerDelay` | `BattleDriver.cs:41662` | 新行有/旧行无 |
| 49 | 战斗逻辑驱动.tsv:138 | `BattleManager.AddFollowAfterResolution(CardScript actingCard, CardScript targetC` | `RuleEngine/Core/EffectResolver.cs:20601` | `RunFollowAfterResolutionRow` | `EffectResolver.cs:20719` | 新行有/旧行无 |
| 50 | 战斗逻辑驱动.tsv:138 | `BattleManager.AddFollowAfterResolution(CardScript actingCard, CardScript targetC` | `RuleEngine/Core/RuleCore.cs:12912` | `FireTriggerDeferred` | `RuleCore.cs:12951` | 新行有/旧行无 |
| 51 | 战斗逻辑驱动.tsv:148 | `BattleManager.AddNewCardToHand(RawCardScript createdCard, CardScript actingCard,` | `RuleEngine/Core/EnchantmentExecutor_B115.cs:1573` | `RunCreateCardInHandRowOfEnchantment` | `EnchantmentExecutor_B115.cs:1550` | 新行有/旧行无 |
| 52 | 战斗逻辑驱动.tsv:152 | `BattleManager.AddRecallToDeck(CardScript recalledCard, bool targetPlayer, bool s` | `Core/EffectResolver.cs:6606` | `DoReturn` | `EffectResolver.cs:6716` | 新行有/旧行无 |
| 53 | 战斗逻辑驱动.tsv:157 | `BattleManager.AddResetActions(CardScript targetCard, int priority, bool checkIfC` | `RuleEngine/Core/RuleCore.cs:14540` | `RemoveAttackThisTurn` | `RuleCore.cs:14742` | 新行有/旧行无 |
| 54 | 战斗逻辑驱动.tsv:158 | `BattleManager.AddResetDuty(CardScript resetCard, bool targetPlayer, int priority` | `RuleEngine/Core/RuleCore.cs:15895` | `ResolveResetDuty` | `RuleCore.cs:15934` | 新行有/旧行无 |
| 55 | 战斗逻辑驱动.tsv:165 | `BattleManager.AddStealFromDeck(CardScript stolenCard, CardScript actingCard, int` | `RuleEngine/Core/EffectResolver.cs:6481` | `TakeFromZone` | `EffectResolver.cs:6591` | 新行有/旧行无 |
| 56 | 战斗逻辑驱动.tsv:166 | `BattleManager.AddStealFromHand(CardScript stolenCard, CardScript actingCard, int` | `RuleEngine/Core/EffectResolver.cs:6423` | `FindZoneInstance` | `EffectResolver.cs:6533` | 新行有/旧行无 |
| 57 | 战斗逻辑驱动.tsv:169 | `BattleManager.AddSummonUnit(RawCardScript summonedCard, int summonPos, bool isPl` | `RuleEngine/Core/EnchantmentExecutor_B115.cs:1515` | `RunSummonUnitRowOfEnchantment` | `EnchantmentExecutor_B115.cs:1492` | 新行有/旧行无 |
| 58 | 战斗逻辑驱动.tsv:184 | `BattleManager.AddTriggerRequiem(CardScript targetCard, CardScript actingCard, in` | `RuleEngine/Core/EffectResolver.cs:18527` | `RunActiveRowOpsOrGrantedTrigger` | `EffectResolver.cs:18822` | 新行有/旧行无 |
| 59 | 战斗逻辑驱动.tsv:201 | `BattleManager.BattleFinished()` | `CardPresentation/Battle/BattleDriver.cs:894` | `RecFinish` | `BattleDriver.cs:913` | 新行有/旧行无 |
| 60 | 战斗逻辑驱动.tsv:209 | `BattleManager.BroadcastHuntMarkResolved(CardScript unitMarked)` | `RuleEngine/Core/EffectResolver.cs:10483` | `PreCallBoardListener` | `EffectResolver.cs:10706` | 新行有/旧行无 |
| 61 | 战斗逻辑驱动.tsv:209 | `BattleManager.BroadcastHuntMarkResolved(CardScript unitMarked)` | `RuleEngine/Core/EffectResolver.cs:7504` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 62 | 战斗逻辑驱动.tsv:210 | `BattleManager.BroadcastMarkResolved(CardScript unitMarked)` | `RuleEngine/Core/EffectResolver.cs:10987` | `BroadcastKeywordEvent` | `EffectResolver.cs:11271` | 新行有/旧行无 |
| 63 | 战斗逻辑驱动.tsv:210 | `BattleManager.BroadcastMarkResolved(CardScript unitMarked)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 64 | 战斗逻辑驱动.tsv:213 | `BattleManager.BroadcastSabotageCreated(CardScript unitCollected)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 65 | 战斗逻辑驱动.tsv:215 | `BattleManager.BroadcastSecretCreated(CardScript unitCollected)` | `RuleEngine/Core/EffectResolver.cs:4604` | `DoChooseCard` | `EffectResolver.cs:4890` | 新行有/旧行无 |
| 66 | 战斗逻辑驱动.tsv:215 | `BattleManager.BroadcastSecretCreated(CardScript unitCollected)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 67 | 战斗逻辑驱动.tsv:218 | `BattleManager.BroadcastUnitCodex(CardScript unitCodex)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 68 | 战斗逻辑驱动.tsv:220 | `BattleManager.BroadcastUnitDuty(CardScript unitCodex)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 69 | 战斗逻辑驱动.tsv:221 | `BattleManager.BroadcastUnitFerocity(CardScript unitCodex)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 70 | 战斗逻辑驱动.tsv:222 | `BattleManager.BroadcastUnitHealed(CardScript unitHealed)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 71 | 战斗逻辑驱动.tsv:223 | `BattleManager.BroadcastUnitLanded(CardScript unitLanded)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 72 | 战斗逻辑驱动.tsv:225 | `BattleManager.BroadcastUnitMob(CardScript unitMob, CardScript unitMobCaster)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 73 | 战斗逻辑驱动.tsv:225 | `BattleManager.BroadcastUnitMob(CardScript unitMob, CardScript unitMobCaster)` | `RuleEngine/Core/RuleCore.cs:10465` | `TakeExtraTrigger` | `RuleCore.cs:10665` | 新行有/旧行无 |
| 74 | 战斗逻辑驱动.tsv:226 | `BattleManager.BroadcastUnitPray(CardScript unitCodex)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 75 | 战斗逻辑驱动.tsv:228 | `BattleManager.BroadcastUnitRequiem(CardScript unitRequiem, CardScript unitRequie` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 76 | 战斗逻辑驱动.tsv:230 | `BattleManager.BroadcastUnitSwarm(CardScript playedUnit, CardScript buffedunit)` | `RuleEngine/Core/RuleCore.cs:4595` | `SwarmMergeNow` | `RuleCore.cs:4712` | 新行有/旧行无 |
| 77 | 战斗逻辑驱动.tsv:230 | `BattleManager.BroadcastUnitSwarm(CardScript playedUnit, CardScript buffedunit)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 78 | 战斗逻辑驱动.tsv:240 | `BattleManager.CancelBattleActionFromQueue(List<BattleAction> queue, int actionIn` | `CardPresentation/Battle/BattleDriver.cs:38654` | `OnCancelCreateHandCard` | `BattleDriver.cs:38907` | 新行有/旧行无 |
| 79 | 战斗逻辑驱动.tsv:243 | `BattleManager.CancelMatch(bool MatchWon)` | `CardPresentation/Net/NetBattle.cs:1824` | `LeaveWithoutResult` | `NetBattle.cs:1892` | 新行有/旧行无 |
| 80 | 战斗逻辑驱动.tsv:254 | `BattleManager.ChoiceOfCardEnemy(CardScript actingCard, CardScript targetCard, in` | `CardPresentation/Battle/BattleDriver.cs:38930` | `SharedChoiceOptionsFor` | `BattleDriver.cs:38975` | 新行有/旧行无 |
| 81 | 战斗逻辑驱动.tsv:254 | `BattleManager.ChoiceOfCardEnemy(CardScript actingCard, CardScript targetCard, in` | `CardPresentation/Battle/BattleDriver.cs:11236` | `ShowAsk` | `BattleDriver.cs:11256` | 新行有/旧行无 |
| 82 | 战斗逻辑驱动.tsv:254 | `BattleManager.ChoiceOfCardEnemy(CardScript actingCard, CardScript targetCard, in` | `RuleEngine/Core/EffectResolver.cs:20360` | `PeekChoiceOptions` | `EffectResolver.cs:20472` | 新行有/旧行无 |
| 83 | 战斗逻辑驱动.tsv:255 | `BattleManager.ChoiceOfCardPlayer(CardScript actingCard, CardScript targetCard, i` | `CardPresentation/Battle/BattleDriver.cs:11210` | `ShowAsk` | `BattleDriver.cs:11256` | 新行有/旧行无 |
| 84 | 战斗逻辑驱动.tsv:279 | `BattleManager.DealMultiDamageAndDestroySelf(DamageInfo damageInfo, List<CardScri` | `RuleEngine/Core/RuleCore.cs:15395` | `UseActiveAbilityCore` | `RuleCore.cs:15587` | 新行有/旧行无 |
| 85 | 战斗逻辑驱动.tsv:306 | `BattleManager.ExecuteNextAction(List<PlayerAction> actionsLog, ref int actionsIn` | `CardPresentation/Battle/BattleDriver.cs:1196` | `ApplyReplayActionAt` | `BattleDriver.cs:1215` | 新行有/旧行无 |
| 86 | 战斗逻辑驱动.tsv:312 | `BattleManager.FinishResolvingAction(BattleActionType actionType, bool playerManu` | `CardPresentation/Battle/BattleDriver.cs:18631` | `ContinueTutorialScript` | `BattleDriver.cs:18713` | 新行有/旧行无 |
| 87 | 战斗逻辑驱动.tsv:325 | `BattleManager.GetAvailableAttackActions(CardScript actingCard, bool allowAutoAct` | `RuleEngine/Core/RuleCore.cs:5770` | `CanAttackNow` | `RuleCore.cs:5868` | 新行有/旧行无 |
| 88 | 战斗逻辑驱动.tsv:372 | `BattleManager.GetPremulliganScriptedData()` | `CardPresentation/Battle/BattleDriver.cs:5825` | `Begin` | `BattleDriver.cs:4924` | 新行有/旧行无 |
| 89 | 战斗逻辑驱动.tsv:384 | `BattleManager.GetUnitSizeInPlay(CardTypeOptions cardType, bool isPlayer)` | `CardPresentation/Board/ArenaSlots.cs:253` | `InPlayScale` | `ArenaSlots.cs:252` | 新行有/旧行无 |
| 90 | 战斗逻辑驱动.tsv:419 | `BattleManager.IsPlayerTurn()` | `RuleEngine/Core/BattleContext.cs:846` | `ActivePlayer` | `BattleContext.cs:850` | 新行有/旧行无 |
| 91 | 战斗逻辑驱动.tsv:420 | `BattleManager.IsPlayingCardState()` | `CardPresentation/Battle/BattleDriver.cs:36859` | `IsPlayingCardState` | `BattleDriver.cs:36900` | 新行有/旧行无 |
| 92 | 战斗逻辑驱动.tsv:427 | `BattleManager.IsThereDoubleOathEffect(bool isPlayer)` | `RuleEngine/Core/EffectResolver.cs:9191` | `ResolveOathAbility` | `EffectResolver.cs:9387` | 新行有/旧行无 |
| 93 | 战斗逻辑驱动.tsv:451 | `BattleManager.ParseEnemyAction(PlayerAction playerAction, bool isPlayer)` | `CardPresentation/Net/NetBattle.cs:1479` | `Dispatch` | `NetBattle.cs:1504` | 新行有/旧行无 |
| 94 | 战斗逻辑驱动.tsv:454 | `BattleManager.PayCardCost(CardScript card, CardScript targetCard)` | `RuleEngine/Core/RuleCore.cs:13245` | `SpendNormalMana` | `RuleCore.cs:13284` | 新行有/旧行无 |
| 95 | 战斗逻辑驱动.tsv:454 | `BattleManager.PayCardCost(CardScript card, CardScript targetCard)` | `RuleEngine/Core/RuleCore.cs:1003` | `CurrentCostOf` | `RuleCore.cs:1002` | 新行有/旧行无 |
| 96 | 战斗逻辑驱动.tsv:478 | `BattleManager.RegisterPlayerActionCancelled(BattleAction battleAction)` | `CardPresentation/Battle/BattleDriver.cs:36896` | `RegisterPlayerActionCancelled` | `BattleDriver.cs:36937` | 新行有/旧行无 |
| 97 | 战斗逻辑驱动.tsv:480 | `BattleManager.RemoveDestroyableAnims(CardScript targetCard, CardEffect cardEffec` | `CardPresentation/Battle/BattleDriver.cs:34530` | `RemoveDestroyableAnims` | `BattleDriver.cs:34820` | 新行有/旧行无 |
| 98 | 战斗逻辑驱动.tsv:510 | `BattleManager.ResolveDrawCard(BattleAction battleAction)` | `RuleEngine/Core/RuleCore.cs:3056` | `MoveDeckTopToHand` | `RuleCore.cs:3173` | 新行有/旧行无 |
| 99 | 战斗逻辑驱动.tsv:510 | `BattleManager.ResolveDrawCard(BattleAction battleAction)` | `RuleEngine/Core/RuleCore.cs:14318` | `ReshuffleCemeteryIntoDeck` | `RuleCore.cs:14356` | 新行有/旧行无 |
| 100 | 战斗逻辑驱动.tsv:530 | `BattleManager.ResolveRecallToDeck(BattleAction battleAction)` | `RuleEngine/Core/EffectResolver.cs:6826` | `ReturnFromCemetery` | `EffectResolver.cs:6936` | 新行有/旧行无 |
| 101 | 战斗逻辑驱动.tsv:530 | `BattleManager.ResolveRecallToDeck(BattleAction battleAction)` | `RuleEngine/Core/EffectResolver.cs:4824` | `DoChooseCard` | `EffectResolver.cs:4890` | 新行有/旧行无 |
| 102 | 战斗逻辑驱动.tsv:532 | `BattleManager.ResolveRemoveEffect(CardScript targetCard, CardEffect cardEffect)` | `CardPresentation/Battle/BattleDriver.cs:22957` | `PlaySignal` | `BattleDriver.cs:23062` | 新行有/旧行无 |
| 103 | 战斗逻辑驱动.tsv:534 | `BattleManager.ResolveRemoveFromCemetery(BattleAction battleAction)` | `RuleEngine/Core/EffectResolver.cs:19589` | `RunRemoveFromCemeteryRow` | `EffectResolver.cs:19625` | 新行有/旧行无 |
| 104 | 战斗逻辑驱动.tsv:534 | `BattleManager.ResolveRemoveFromCemetery(BattleAction battleAction)` | `RuleEngine/Core/RuleCore.cs:17107` | `ExileFromCemetery` | `RuleCore.cs:17113` | 新行有/旧行无 |
| 105 | 战斗逻辑驱动.tsv:534 | `BattleManager.ResolveRemoveFromCemetery(BattleAction battleAction)` | `RuleEngine/Core/EffectResolver.cs:20146` | `ResolveCemeteryTargets` | `EffectResolver.cs:20182` | 新行有/旧行无 |
| 106 | 战斗逻辑驱动.tsv:546 | `BattleManager.ResolveTransformFromRemnant(BattleAction battleAction)` | `RuleEngine/Core/EffectResolver.cs:15812` | `DoReanimate` | `EffectResolver.cs:15848` | 新行有/旧行无 |
| 107 | 战斗逻辑驱动.tsv:573 | `BattleManager.SetNextCardAsDeckDrawing(bool isPlayer)` | `RuleEngine/Core/RuleCore.cs:2965` | `Draw` | `RuleCore.cs:3059` | 新行有/旧行无 |
| 108 | 战斗逻辑驱动.tsv:601 | `BattleManager.ShowFinishScreen(bool skipDelay)` | `Battle/EndPanel.cs:613` | `Show` | `EndPanel.cs:617` | 新行有/旧行无 |
| 109 | 战斗逻辑驱动.tsv:617 | `BattleManager.TryUsingActiveAbility()` | `RuleEngine/Core/RuleCore.cs:11751` | `UseAbilityCore` | `RuleCore.cs:11789` | 新行有/旧行无 |
| 110 | 战斗逻辑驱动.tsv:618 | `BattleManager.TryUsingActiveAbilityOfCard(CardScript card, bool forceSelectCard)` | `RuleEngine/Core/RuleCore.cs:11751` | `UseAbilityCore` | `RuleCore.cs:11789` | 新行有/旧行无 |
| 111 | 战斗逻辑驱动.tsv:624 | `BattleManager.WaitOpponentLastActions()` | `CardPresentation/Net/NetBattle.cs:2081` | `DeferForPeerLastActions` | `NetBattle.cs:2142` | 新行有/旧行无 |
| 112 | 战斗逻辑驱动.tsv:632 | `BattleManagerSupport.BroadcastCollectQuestPoints(BattleManager manager, CardScri` | `RuleEngine/Core/EffectResolver.cs:13205` | `QuestPointThreshold` | `EffectResolver.cs:13491` | 新行有/旧行无 |
| 113 | 战斗逻辑驱动.tsv:632 | `BattleManagerSupport.BroadcastCollectQuestPoints(BattleManager manager, CardScri` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 114 | 战斗逻辑驱动.tsv:634 | `BattleManagerSupport.BroadcastDeadUnit(BattleManager manager, BattleAction battl` | `RuleEngine/Core/RuleCore.cs:10109` | `SweepExtrinsicEffectsOnSourceDeath` | `RuleCore.cs:15457` | 新行有/旧行无 |
| 115 | 战斗逻辑驱动.tsv:635 | `BattleManagerSupport.BroadcastEarnedSkull(BattleManager manager, CardScript earn` | `RuleEngine/Core/RuleCore.cs:9118` | `Hurt` | `RuleCore.cs:9225` | 新行有/旧行无 |
| 116 | 战斗逻辑驱动.tsv:640 | `BattleManagerSupport.BroadcastTurnBeforeEnd(BattleManager manager, bool isPlayer` | `RuleEngine/Core/EffectResolver.cs:8631` | `EmitAtTurnTriggerPass` | `EffectResolver.cs:8826` | 新行有/旧行无 |
| 117 | 战斗逻辑驱动.tsv:641 | `BattleManagerSupport.BroadcastTurnEnd(BattleManager manager, bool isPlayerTurn)` | `RuleEngine/Core/EffectResolver.cs:8631` | `EmitAtTurnTriggerPass` | `EffectResolver.cs:8826` | 新行有/旧行无 |
| 118 | 战斗逻辑驱动.tsv:644 | `BattleManagerSupport.BroadcastTurnStarted(BattleManager manager, bool isPlayerTu` | `RuleEngine/Core/EffectResolver.cs:18909` | `BroadcastTurnStartedHandHops` | `EffectResolver.cs:19260` | 新行有/旧行无 |
| 119 | 战斗逻辑驱动.tsv:652 | `BattleManagerSupport.BroadcastUnitLanded(BattleManager manager, CardScript unitL` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 120 | 战斗逻辑驱动.tsv:654 | `BattleManagerSupport.BroadcastUnitMob(BattleManager manager, CardScript unitMob,` | `RuleEngine/Core/EffectResolver.cs:9842` | `BroadcastWhen` | `EffectResolver.cs:10103` | 新行有/旧行无 |
| 121 | 战斗逻辑驱动.tsv:657 | `BattleManagerSupport.BroadcastUnitObtainedMarkerlight(BattleManager manager, Car` | `RuleEngine/Core/EffectResolver.cs:10987` | `BroadcastKeywordEvent` | `EffectResolver.cs:11271` | 新行有/旧行无 |
| 122 | 战斗逻辑驱动.tsv:658 | `BattleManagerSupport.BroadcastUnitPoisoned(BattleManager manager, CardScript poi` | `RuleEngine/Core/EffectResolver.cs:19236` | `BroadcastUnitPoisonedHandHops` | `EffectResolver.cs:19272` | 新行有/旧行无 |
| 123 | 战斗逻辑驱动.tsv:665 | `BattleManagerSupport.BroadcastUnitSummoned(BattleManager manager, CardScript act` | `RuleEngine/Core/RuleCore.cs:3920` | `PlayCardCore` | `RuleCore.cs:4037` | 新行有/旧行无 |
| 124 | 战斗逻辑驱动.tsv:668 | `BattleManagerSupport.BroadcastUnitUsedSacrifice(BattleManager manager, CardScrip` | `RuleEngine/Core/RuleCore.cs:12880` | `FireTriggerAlways` | `RuleCore.cs:13108` | 新行有/旧行无 |
| 125 | 战斗逻辑驱动.tsv:668 | `BattleManagerSupport.BroadcastUnitUsedSacrifice(BattleManager manager, CardScrip` | `RuleEngine/Core/EffectResolver.cs:16627` | `BoardJumpIncludesPendingDeath` | `EffectResolver.cs:16770` | 新行有/旧行无 |
| 126 | 战斗逻辑驱动.tsv:677 | `CardAbility.CanTriggerAbility(AbilityTrigger trigger, BattleManager manager, Car` | `RuleEngine/Core/EffectResolver.cs:20730` | `CostWhenAbilityCriteriaAllows` | `EffectResolver.cs:20766` | 新行有/旧行无 |
| 127 | 战斗逻辑驱动.tsv:677 | `CardAbility.CanTriggerAbility(AbilityTrigger trigger, BattleManager manager, Car` | `RuleEngine/Core/EffectResolver.cs:10867` | `EmitListenerTrigger` | `EffectResolver.cs:8109` | 新行有/旧行无 |
| 128 | 战斗逻辑驱动.tsv:686 | `CardScript.AddEffect(CardEffect newEffect, int enchantmentCardId)` | `RuleEngine/Core/EffectResolver.cs:12170` | `ApplyOneGain` | `EffectResolver.cs:12239` | 新行有/旧行无 |
| 129 | 战斗逻辑驱动.tsv:687 | `CardScript.AddEnchantment(CardEffect newEffect)` | `RuleEngine/Core/Enchantment.cs:236` | `Attach` | `Enchantment.cs:228` | 新行有/旧行无 |
| 130 | 战斗逻辑驱动.tsv:707 | `CardScript.CardSetup(BattleManager mgr, bool isPlayer, CardArmy mainArmy, bool k` | `RuleEngine/Core/RuleCore.cs:3668` | `ApplyDeployTurnState` | `RuleCore.cs:3775` | 新行有/旧行无 |
| 131 | 战斗逻辑驱动.tsv:707 | `CardScript.CardSetup(BattleManager mgr, bool isPlayer, CardArmy mainArmy, bool k` | `RuleEngine/Core/RuleCore.cs:4295` | `SpawnTideCopies` | `RuleCore.cs:4402` | 新行有/旧行无 |
| 132 | 战斗逻辑驱动.tsv:707 | `CardScript.CardSetup(BattleManager mgr, bool isPlayer, CardArmy mainArmy, bool k` | `RuleEngine/Core/RuleCore.cs:4374` | `PlayCompanions` | `RuleCore.cs:4481` | 新行有/旧行无 |
| 133 | 战斗逻辑驱动.tsv:725 | `CardScript.ClickMulliganSelected(bool fromClick)` | `RuleEngine/Core/RuleCore.cs:444` | `Mulligan` | `RuleCore.cs:453` | 新行有/旧行也有 |
| 134 | 战斗逻辑驱动.tsv:729 | `CardScript.DisplaySwarm()` | `Core/CardView.cs:8743` | `BuildSwarm` | `CardView.cs:8986` | 新行有/旧行无 |
| 135 | 战斗逻辑驱动.tsv:736 | `CardScript.EnoughPendingDamageToDie()` | `RuleEngine/Core/RuleCore.cs:7720` | `EnoughPendingDamageToDie` | `RuleCore.cs:7920` | 新行有/旧行无 |
| 136 | 战斗逻辑驱动.tsv:742 | `CardScript.FinishAfterAttack()` | `RuleEngine/Core/RuleCore.cs:12647` | `EmitBloodThirst` | `RuleCore.cs:12685` | 新行有/旧行无 |
| 137 | 战斗逻辑驱动.tsv:748 | `CardScript.GetAdjustedDamage(DamageInfo grossDamage, bool shouldCountBastion)` | `RuleEngine/Core/EffectResolver.cs:17878` | `StampRollPerTarget` | `EffectResolver.cs:18226` | 新行有/旧行无 |
| 138 | 战斗逻辑驱动.tsv:767 | `CardScript.GoToCemetery()` | `RuleEngine/Core/EffectResolverPartial_B88E2.cs:749` | `DiscardHandCardCore` | `EffectResolverPartial_B88E2.cs:767` | 新行有/旧行无 |
| 139 | 战斗逻辑驱动.tsv:774 | `CardScript.HasEnoughPendingDamageAndIsRemnant()` | `RuleEngine/Core/RuleCore.cs:9589` | `CleanupDeaths` | `RuleCore.cs:9734` | 新行有/旧行无 |
| 140 | 战斗逻辑驱动.tsv:804 | `CardScript.MinionPlayedIntoField(float delay, int unitInPlayCounter, bool doTran` | `CardPresentation/Battle/BattleDriver.cs:25640` | `PlayAiDeployLandingVfx` | `BattleDriver.cs:25876` | 新行有/旧行无 |
| 141 | 战斗逻辑驱动.tsv:804 | `CardScript.MinionPlayedIntoField(float delay, int unitInPlayCounter, bool doTran` | `CardPresentation/Core/VfxMap.cs:601` | `CardPick` | `VfxMap.cs:649` | 新行有/旧行无 |
| 142 | 战斗逻辑驱动.tsv:804 | `CardScript.MinionPlayedIntoField(float delay, int unitInPlayCounter, bool doTran` | `CardPresentation/Core/VfxMap.cs:603` | `CardPick` | `VfxMap.cs:649` | 新行有/旧行无 |
| 143 | 战斗逻辑驱动.tsv:806 | `CardScript.MoveBackToHand()` | `CardPresentation/Core/CardFeel.cs:3709` | `MoveBackToHand` | `CardFeel.cs:3718` | 新行有/旧行无 |
| 144 | 战斗逻辑驱动.tsv:808 | `CardScript.NeedsToChooseFromPool(CardScript targetCard)` | `RuleEngine/Core/EffectResolver.cs:17488` | `NeedsToChooseFromPool` | `EffectResolver.cs:17921` | 新行有/旧行无 |
| 145 | 战斗逻辑驱动.tsv:809 | `CardScript.NonTargetSpellPlayed()` | `RuleEngine/Core/EffectResolver.cs:18264` | `TryFireCombo` | `EffectResolver.cs:18382` | 新行有/旧行无 |
| 146 | 战斗逻辑驱动.tsv:810 | `CardScript.OnTouchDown(Vector2 touchPosition)` | `CardPresentation/Battle/BattleDriver.cs:34295` | `BoardCardTouchDown` | `BattleDriver.cs:34591` | 新行有/旧行无 |
| 147 | 战斗逻辑驱动.tsv:810 | `CardScript.OnTouchDown(Vector2 touchPosition)` | `CardPresentation/Battle/BattleDriver.cs:34332` | `BoardTouchInput` | `BattleDriver.cs:34628` | 新行有/旧行无 |
| 148 | 战斗逻辑驱动.tsv:810 | `CardScript.OnTouchDown(Vector2 touchPosition)` | `CardPresentation/Core/CardView.cs:14461` | `ApplyBoardHighlightState` | `CardView.cs:14686` | 新行有/旧行无 |
| 149 | 战斗逻辑驱动.tsv:810 | `CardScript.OnTouchDown(Vector2 touchPosition)` | `CardPresentation/Battle/BattleDriver.cs:17224` | `CardHoldPeek` | `BattleDriver.cs:17306` | 新行有/旧行无 |
| 150 | 战斗逻辑驱动.tsv:811 | `CardScript.OnTouchDrag(PointerEventData eventData, Vector2 originalTouchPosition` | `CardPresentation/Battle/BattleDriver.cs:32552` | `TapBoardCard` | `BattleDriver.cs:34156` | 新行有/旧行无 |
| 151 | 战斗逻辑驱动.tsv:812 | `CardScript.OnTouchEnter(PointerEventData eventData)` | `CardPresentation/Battle/BattleDriver.cs:14887` | `DrivePlayerTurn` | `BattleDriver.cs:14952` | 新行有/旧行无 |
| 152 | 战斗逻辑驱动.tsv:812 | `CardScript.OnTouchEnter(PointerEventData eventData)` | `CardPresentation/Hand/CardInteraction.cs:722` | `UpdateHover` | `CardInteraction.cs:723` | 新行有/旧行无 |
| 153 | 战斗逻辑驱动.tsv:813 | `CardScript.OnTouchExit(Vector2 originalTouchPosition)` | `CardPresentation/Battle/BattleDriver.cs:12714` | `TickCardWinPointerExit` | `BattleDriver.cs:13373` | 新行有/旧行无 |
| 154 | 战斗逻辑驱动.tsv:813 | `CardScript.OnTouchExit(Vector2 originalTouchPosition)` | `CardPresentation/Hand/CardInteraction.cs:1893` | `ApplyShowInHand` | `CardInteraction.cs:1968` | 新行有/旧行无 |
| 155 | 战斗逻辑驱动.tsv:815 | `CardScript.OnTurnEnd(bool playerTurn)` | `RuleEngine/Core/RuleCore.cs:15872` | `EmitTurnEndCourtesy` | `RuleCore.cs:16064` | 新行有/旧行无 |
| 156 | 战斗逻辑驱动.tsv:815 | `CardScript.OnTurnEnd(bool playerTurn)` | `RuleEngine/Core/RuleCore.cs:15896` | `EmitTurnEndResolution` | `RuleCore.cs:16088` | 新行有/旧行无 |
| 157 | 战斗逻辑驱动.tsv:819 | `CardScript.OutFromPlayingAreaAnim()` | `CardPresentation/Hand/CardInteraction.cs:2726` | `CancelDeployFlight` | `CardInteraction.cs:2727` | 新行有/旧行无 |
| 158 | 战斗逻辑驱动.tsv:823 | `CardScript.QuestResolved(CardScript actingCard)` | `RuleEngine/Core/EffectResolver.cs:12838` | `DoFactionResource` | `EffectResolver.cs:13124` | 新行有/旧行无 |
| 159 | 战斗逻辑驱动.tsv:825 | `CardScript.ReactToCollectFaith(CardScript unitCollected)` | `RuleEngine/Core/RuleCore.cs:1636` | `GrantPrayEnchantmentFaith` | `RuleCore.cs:1635` | 新行有/旧行无 |
| 160 | 战斗逻辑驱动.tsv:832 | `CardScript.ReactToUnitJammed(CardScript disenchantedCard)` | `RuleEngine/Core/UnitState.cs:3607` | `ContainsExtrinsicEffectsFrom` | `UnitState.cs:3609` | 新行有/旧行无 |
| 161 | 战斗逻辑驱动.tsv:832 | `CardScript.ReactToUnitJammed(CardScript disenchantedCard)` | `RuleEngine/Core/UnitState.cs:3669` | `RevertGrantsCastBy` | `UnitState.cs:3671` | 新行有/旧行无 |
| 162 | 战斗逻辑驱动.tsv:847 | `CardScript.RemoveBuffHandCostEffects()` | `RuleEngine/Core/RuleCore.cs:10712` | `ExpireHandBuffs` | `RuleCore.cs:10887` | 新行有/旧行无 |
| 163 | 战斗逻辑驱动.tsv:853 | `CardScript.RemoveEffect(CardEffect removedEffect, bool fromJam)` | `RuleEngine/Core/EffectResolver.cs:18134` | `RemoveEffectRecordFrom` | `EffectResolver.cs:18430` | 新行有/旧行无 |
| 164 | 战斗逻辑驱动.tsv:853 | `CardScript.RemoveEffect(CardEffect removedEffect, bool fromJam)` | `RuleEngine/Core/ActionPump.cs:2233` | `EnqueueRemoveEffect` | `ActionPump.cs:2235` | 新行有/旧行无 |
| 165 | 战斗逻辑驱动.tsv:853 | `CardScript.RemoveEffect(CardEffect removedEffect, bool fromJam)` | `RuleEngine/Core/UnitState.cs:3029` | `RemoveTraitEffect` | `UnitState.cs:3031` | 新行有/旧行无 |
| 166 | 战斗逻辑驱动.tsv:860 | `CardScript.RemoveSwarm()` | `Core/CardView.cs:8586` | `SwarmOffFadeTime` | `CardView.cs:8830` | 新行有/旧行无 |
| 167 | 战斗逻辑驱动.tsv:861 | `CardScript.RemoveTrait(DefinedTrait newTrait)` | `RuleEngine/Core/RuleCore.cs:3531` | `RecomputeActAndAttack` | `RuleCore.cs:3648` | 新行有/旧行无 |
| 168 | 战斗逻辑驱动.tsv:863 | `CardScript.RemoveTraitSilently(DefinedTrait traitToRemove)` | `RuleEngine/Core/UnitState.cs:2532` | `ClearAllKeywords` | `UnitState.cs:2533` | 新行有/旧行无 |
| 169 | 战斗逻辑驱动.tsv:867 | `CardScript.ResolveActiveAbilityPlayed(CardScript actingCard, CardScript targetCa` | `RuleEngine/Core/RuleCore.cs:15520` | `CanStartActiveAbility` | `RuleCore.cs:15526` | 新行有/旧行无 |
| 170 | 战斗逻辑驱动.tsv:867 | `CardScript.ResolveActiveAbilityPlayed(CardScript actingCard, CardScript targetCa` | `RuleEngine/Core/RuleCore.cs:15341` | `OathCostTrait1275` | `RuleCore.cs:17645` | 新行有/旧行无 |
| 171 | 战斗逻辑驱动.tsv:869 | `CardScript.ResolveCardPlayed(CardScript cardPlayed, CardScript targetCard)` | `RuleEngine/Core/EffectResolver.cs:18264` | `TryFireCombo` | `EffectResolver.cs:18382` | 新行有/旧行无 |
| 172 | 战斗逻辑驱动.tsv:875 | `CardScript.ResolveUnitAttacked(CardScript actingCard, CardScript targetCard, Att` | `RuleEngine/Core/UnitState.cs:3323` | `ExpireUntilNextAttack` | `UnitState.cs:3325` | 新行有/旧行无 |
| 173 | 战斗逻辑驱动.tsv:878 | `CardScript.ReturnToOriginalPos()` | `Core/CardView.cs:9792` | `KillPoseTween` | `CardView.cs:10042` | 新行有/旧行无 |
| 174 | 战斗逻辑驱动.tsv:881 | `CardScript.SendToCemetery()` | `RuleEngine/Core/RuleCore.cs:14441` | `AddToCemetery` | `RuleCore.cs:14643` | 新行有/旧行无 |
| 175 | 战斗逻辑驱动.tsv:881 | `CardScript.SendToCemetery()` | `CardPresentation/Battle/BattleDriver.cs:25433` | `PlayDeathFeel` | `BattleDriver.cs:25671` | 新行有/旧行无 |
| 176 | 战斗逻辑驱动.tsv:882 | `CardScript.SetAmbush(bool activateAmbush)` | `RuleEngine/Core/RuleCore.cs:8985` | `DeclareAttackCore` | `RuleCore.cs:6444` | 新行有/旧行无 |
| 177 | 战斗逻辑驱动.tsv:888 | `CardScript.SetAsUnitDestroyed()` | `RuleEngine/Core/BattleContext.cs:1764` | `DyingThisAction` | `BattleContext.cs:1766` | 新行有/旧行无 |
| 178 | 战斗逻辑驱动.tsv:888 | `CardScript.SetAsUnitDestroyed()` | `CardPresentation/Battle/BattleDriver.cs:25433` | `PlayDeathFeel` | `BattleDriver.cs:25671` | 新行有/旧行无 |
| 179 | 战斗逻辑驱动.tsv:889 | `CardScript.SetAsUnitInHand(bool setVisibility)` | `CardPresentation/Battle/BattleDriver.cs:26141` | `SyncHand` | `BattleDriver.cs:27573` | 新行有/旧行无 |
| 180 | 战斗逻辑驱动.tsv:892 | `CardScript.SetBaseCardTraits()` | `RuleEngine/Core/UnitState.cs:1247` | `RemoveAll` | `UnitState.cs:1282` | 新行有/旧行无 |
| 181 | 战斗逻辑驱动.tsv:894 | `CardScript.SetCardStateInCemetery()` | `RuleEngine/Core/RuleCore.cs:9904` | `MinionDeathBookkeepingDeferred` | `RuleCore.cs:10426` | 新行有/旧行无 |
| 182 | 战斗逻辑驱动.tsv:895 | `CardScript.SetCardTurnDrawn()` | `RuleEngine/Core/RuleCore.cs:1680` | `BeginTurnCore` | `RuleCore.cs:1705` | 新行有/旧行无 |
| 183 | 战斗逻辑驱动.tsv:904 | `CardScript.SetRemoveMarkerlight()` | `RuleEngine/Core/RuleCore.cs:2276` | `EndTurnCore` | `RuleCore.cs:2410` | 新行有/旧行无 |
| 184 | 战斗逻辑驱动.tsv:905 | `CardScript.SetSortingLayer(string layerName)` | `Core/CardView.cs:1885` | `SubtreeQueues` | `CardView.cs:1945` | 新行有/旧行无 |
| 185 | 战斗逻辑驱动.tsv:907 | `CardScript.SetStateAsMulligan(bool toBeDiscarded)` | `CardPresentation/Battle/BattleDriver.cs:8970` | `OnMulliganCardSeen` | `BattleDriver.cs:9001` | 新行有/旧行无 |
| 186 | 战斗逻辑驱动.tsv:908 | `CardScript.SetStateInHand()` | `CardPresentation/Core/CardView.cs:3572` | `CreatedByVisible` | `CardView.cs:3632` | 新行有/旧行无 |
| 187 | 战斗逻辑驱动.tsv:908 | `CardScript.SetStateInHand()` | `CardPresentation/Battle/BattleDriver.cs:27611` | `PushCreatedByStates` | `BattleDriver.cs:27857` | 新行有/旧行无 |
| 188 | 战斗逻辑驱动.tsv:913 | `CardScript.SetupInDeck()` | `Core/CardView.cs:3183` | `SetBody3DVisible` | `CardView.cs:3243` | 新行有/旧行无 |
| 189 | 战斗逻辑驱动.tsv:930 | `CardScript.StopAnimsInPlay()` | `CardPresentation/Core/CardView.cs:14715` | `StopInPlayAnims` | `CardView.cs:14730` | 新行有/旧行无 |
| 190 | 战斗逻辑驱动.tsv:935 | `CardScript.TargetedSpellPlayed(CardScript actingCard, CardScript targetCard)` | `RuleEngine/Core/EffectResolver.cs:18264` | `TryFireCombo` | `EffectResolver.cs:18382` | 新行有/旧行无 |
| 191 | 战斗逻辑驱动.tsv:938 | `CardScript.TransformFromRemnant(Action onComplete)` | `RuleEngine/Core/EffectResolver.cs:16627` | `ReanimateRemnantInPlace` | `EffectResolver.cs:16975` | 新行有/旧行无 |
| 192 | 战斗逻辑驱动.tsv:939 | `CardScript.TransformIntoRemnant()` | `RuleEngine/Core/UnitState.cs:2532` | `ClearAllKeywords` | `UnitState.cs:2533` | 新行有/旧行无 |
| 193 | 战斗逻辑驱动.tsv:948 | `CardScript.TriggerOtherCardCodex(CardScript codexUnit)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 194 | 战斗逻辑驱动.tsv:950 | `CardScript.TriggerOtherCardMob(CardScript mobUnit)` | `RuleEngine/Core/EffectResolver.cs:7502` | `TriggerNameOfWhenKind` | `EffectResolver.cs:7644` | 新行有/旧行无 |
| 195 | 战斗逻辑驱动.tsv:950 | `CardScript.TriggerOtherCardMob(CardScript mobUnit)` | `RuleEngine/Core/RuleCore.cs:10465` | `TakeExtraTrigger` | `RuleCore.cs:10665` | 新行有/旧行无 |
| 196 | 战斗逻辑驱动.tsv:952 | `CardScript.TriggerOtherCardRequiem(CardScript requiemUnit)` | `RuleEngine/Core/EffectResolver.cs:16283` | `BroadcastUnitRequiem` | `EffectResolver.cs:16319` | 新行有/旧行无 |
| 197 | 战斗逻辑驱动.tsv:952 | `CardScript.TriggerOtherCardRequiem(CardScript requiemUnit)` | `RuleEngine/Core/RuleCore.cs:14481` | `ResolveTriggerRequiem` | `RuleCore.cs:14487` | 新行有/旧行无 |
| 198 | 战斗逻辑驱动.tsv:955 | `CardScript.TriggerSacrifice()` | `RuleEngine/Core/RuleCore.cs:9696` | `CleanupDeaths` | `RuleCore.cs:9734` | 新行有/旧行无 |
| 199 | 战斗逻辑驱动.tsv:969 | `CardScript.TryDraggingFromBoard(PointerEventData eventData, Vector2 originalTouc` | `RuleEngine/Core/RuleCore.cs:5365` | `IsAttackingOnBoard` | `RuleCore.cs:5482` | 新行有/旧行无 |
| 200 | 战斗逻辑驱动.tsv:972 | `CardScript.UnDissolveWholeCard()` | `CardPresentation/Core/CardFeel.cs:3535` | `DissolveCardTime` | `CardFeel.cs:3552` | 新行有/旧行无 |
| 201 | 战斗逻辑驱动.tsv:973 | `CardScript.UndissolveToDeck()` | `CardPresentation/Battle/BattleDriver.cs:41807` | `PlayReshuffleToDeckFeel` | `BattleDriver.cs:41938` | 新行有/旧行无 |
| 202 | 战斗逻辑驱动.tsv:1025 | `EntityScript.EnemyReturnsAttack(CardScript attackedCard, bool isAttacker, Attack` | `RuleEngine/Core/CounterAttack.cs:176` | `EnemyReturnsAttack` | `CounterAttack.cs:159` | 新行有/旧行无 |
| 203 | 战斗逻辑驱动.tsv:1028 | `EntityScript.GetCombatDamage(AttackTypes attackType)` | `RuleEngine/Core/CounterAttack.cs:129` | `CombatDamage` | `CounterAttack.cs:112` | 新行有/旧行无 |
| 204 | 战斗逻辑驱动.tsv:1033 | `EntityScript.GetCurrentTraitValueWithModifiers(DefinedTrait trait)` | `RuleEngine/Core/UnitState.cs:2504` | `RawKw` | `UnitState.cs:2462` | 新行有/旧行无 |
| 205 | 战斗逻辑驱动.tsv:1037 | `EntityScript.GetThisCardPlayedDamage(CardScript targetCard)` | `CardPresentation/Battle/BattleDriver.cs:34447` | `BattleBoardPreview` | `BattleDriver.cs:34695` | 新行有/旧行无 |
| 206 | 战斗逻辑驱动.tsv:1044 | `EntityScript.HasEnchantmentWhileInPlay()` | `RuleEngine/Core/CardDef.cs:8987` | `WhileInPlayAnim` | `CardDef.cs:8988` | 新行有/旧行无 |
| 207 | 战斗逻辑驱动.tsv:1044 | `EntityScript.HasEnchantmentWhileInPlay()` | `RuleEngine/Data/CardDatabase.cs:3567` | `EnchantmentTemplatesResourcePath` | `CardDatabase.cs:3650` | 新行有/旧行无 |
| 208 | 战斗逻辑驱动.tsv:1044 | `EntityScript.HasEnchantmentWhileInPlay()` | `RuleEngine/Core/BattleContext.cs:2375` | `EnchantmentTemplatesProvider` | `BattleContext.cs:2377` | 新行有/旧行无 |
| 209 | 战斗逻辑驱动.tsv:1066 | `MatchData.GetFTUEBotConfig()` | `CardPresentation/Battle/BattleDriver.cs:41413` | `AssignAiDifficultyForThisMatch` | `BattleDriver.cs:41458` | 新行有/旧行无 |
| 210 | 战斗逻辑驱动.tsv:1066 | `MatchData.GetFTUEBotConfig()` | `CardPresentation/Battle/BattleDriver.cs:41363` | `ModeVictoriesForBotConfig` | `BattleDriver.cs:41492` | 新行有/旧行无 |
| 211 | 战斗逻辑驱动.tsv:1071 | `MatchData.GetNormalBotConfig()` | `CardPresentation/Battle/BattleDriver.cs:41413` | `AssignAiDifficultyForThisMatch` | `BattleDriver.cs:41458` | 新行有/旧行无 |
| 212 | 战斗逻辑驱动.tsv:1071 | `MatchData.GetNormalBotConfig()` | `CardPresentation/Battle/BattleDriver.cs:41363` | `ModeVictoriesForBotConfig` | `BattleDriver.cs:41492` | 新行有/旧行无 |
| 213 | 战斗逻辑驱动.tsv:1074 | `MatchData.IsInFTUEState()` | `CardPresentation/Battle/BattleDriver.cs:41413` | `AssignAiDifficultyForThisMatch` | `BattleDriver.cs:41458` | 新行有/旧行无 |
| 214 | 战斗逻辑驱动.tsv:1121 | `MinionManager.GetNextSlotPos(Vector2 pos, int nextSlotRequired)` | `CardPresentation/Battle/BattleDriver.cs:3930` | `EnsureMinionSpotAnchor` | `BattleDriver.cs:3948` | 新行有/旧行无 |
| 215 | 战斗逻辑驱动.tsv:1139 | `MinionManager.ReassembleMinionsWhilePlayingUnit(Vector2 newUnitPos)` | `CardPresentation/Battle/BattleDriver.cs:27114` | `TickShufflePreview` | `BattleDriver.cs:27227` | 新行有/旧行无 |
| 216 | 战斗逻辑驱动.tsv:1139 | `MinionManager.ReassembleMinionsWhilePlayingUnit(Vector2 newUnitPos)` | `CardPresentation/Battle/BattleDriver.cs:26951` | `EndPreviewReturn` | `BattleDriver.cs:27181` | 新行有/旧行无 |
| 217 | 战斗逻辑驱动.tsv:1150 | `PlayerHand.AddDrawnCardToHand(CardScript card)` | `CardPresentation/Battle/BattleDriver.cs:41651` | `PlayHandFullDrawFeel` | `BattleDriver.cs:41696` | 新行有/旧行无 |
| 218 | 战斗逻辑驱动.tsv:1170 | `PlayerHand.HandCardMovedToNewIndex(CardScript card, int newIndex)` | `CardPresentation/Battle/BattleDriver.cs:17389` | `CheckForTutorialPointer` | `BattleDriver.cs:17471` | 新行有/旧行无 |
| 219 | 战斗逻辑驱动.tsv:1186 | `PlayerHand.RemoveFromGameCard(CardScript removedCard)` | `RuleEngine/Core/RuleCore.cs:2934` | `SweepEphemeral` | `RuleCore.cs:3006` | 新行有/旧行无 |
| 220 | 战斗逻辑驱动.tsv:1232 | `RawCardScript.GetLocalizedCardDesc()` | `RuleEngine/Core/CardDef.cs:6775` | `CardDescriptionTextReferenceId` | `CardDef.cs:6776` | 新行有/旧行无 |
| 221 | 战斗逻辑驱动.tsv:1241 | `RawCardScript.GetLocalizedTargetted()` | `CardPresentation/Battle/BattleDriver.cs:36025` | `ShowPlayTargetPrompt` | `BattleDriver.cs:36297` | 新行有/旧行无 |
| 222 | 战斗逻辑驱动.tsv:1243 | `RawCardScript.GetRangedAttackParticles()` | `CardPresentation/Battle/BattleDriver.cs:22379` | `ScheduleAttackPresentation` | `BattleDriver.cs:22484` | 新行有/旧行无 |
| 223 | 战斗逻辑驱动.tsv:1243 | `RawCardScript.GetRangedAttackParticles()` | `CardPresentation/Battle/BattleDriver.cs:22620` | `PlanRetaliation` | `BattleDriver.cs:22725` | 新行有/旧行无 |
| 224 | 战斗逻辑驱动.tsv:1250 | `RawCardScript.HasCustomRangedAttackParticles()` | `CardPresentation/Battle/BattleDriver.cs:22620` | `PlanRetaliation` | `BattleDriver.cs:22725` | 新行有/旧行无 |
| 225 | 战斗逻辑驱动.tsv:1280 | `RawCardScript.ReleaseFullHeroSprite()` | `Shell/DraftModeWindow.cs:1037` | `Close` | `DraftModeWindow.cs:1029` | 新行有/旧行无 |
| 226 | 战斗逻辑驱动.tsv:1280 | `RawCardScript.ReleaseFullHeroSprite()` | `Shell/PracticeModePopup.cs:781` | `RefreshCharacterImage` | `PracticeModePopup.cs:772` | 新行有/旧行无 |
| 227 | 战斗逻辑驱动.tsv:1282 | `RawCardScript.ResolveActiveAbility(BattleManager manager, CardScript thisCard, C` | `RuleEngine/Core/EffectResolver.cs:20633` | `RunSetReferenceCardRow` | `EffectResolver.cs:20928` | 新行有/旧行无 |
| 228 | 战斗逻辑驱动.tsv:1282 | `RawCardScript.ResolveActiveAbility(BattleManager manager, CardScript thisCard, C` | `RuleEngine/Core/EffectResolver.cs:20833` | `RunHighlightBloodThirstRow` | `EffectResolver.cs:20976` | 新行有/旧行无 |
| 229 | 战斗逻辑驱动.tsv:1285 | `RawCardScript.TriggerAbilities(AbilityTrigger trigger, BattleManager manager, Ca` | `RuleEngine/Core/EffectResolver.cs:13829` | `TryStructuredMultiTrigger` | `EffectResolver.cs:14083` | 新行有/旧行无 |
| 230 | 战斗逻辑驱动.tsv:1285 | `RawCardScript.TriggerAbilities(AbilityTrigger trigger, BattleManager manager, Ca` | `RuleEngine/Core/EffectResolver.cs:20668` | `FireTriggerByNumberWithSink` | `EffectResolver.cs:21113` | 新行有/旧行无 |
| 231 | 战斗逻辑驱动.tsv:1286 | `RawCardScript.TriggerAbility(CardAbility ability, BattleManager manager, CardScr` | `RuleEngine/Core/RuleCore.cs:12726` | `FireTriggerDeferred` | `RuleCore.cs:12951` | 新行有/旧行无 |
| 232 | 战斗逻辑驱动.tsv:1286 | `RawCardScript.TriggerAbility(CardAbility ability, BattleManager manager, CardScr` | `RuleEngine/Core/EffectResolver.cs:9086` | `SpiritAbilityDeferred` | `EffectResolver.cs:9319` | 新行有/旧行无 |
| 233 | 战斗逻辑驱动.tsv:1288 | `RawCardScript.UpdateTriggerOnce(BattleManager manager, CardScript thisCard)` | `RuleEngine/Core/EffectResolver.cs:18754` | `FireTriggerOnce` | `EffectResolver.cs:18790` | 新行有/旧行无 |
| 234 | 战斗逻辑驱动.tsv:1308 | `SupportMethods.GetRandomOrder(int numElements, MatchData matchData, List<int> st` | `RuleEngine/Core/RuleCore.cs:15263` | `ShuffleByRandomOrder` | `RuleCore.cs:15300` | 新行有/旧行无 |
| 235 | 战斗逻辑驱动.tsv:1320 | `SupportMethods.IsAeldariRemnant(CardScript cardScript)` | `RuleEngine/Core/RuleCore.cs:12536` | `CanCollectWaystone` | `RuleCore.cs:12574` | 新行有/旧行无 |
| 236 | 战斗逻辑驱动.tsv:1360 | `TroopsKilledCount.OnTroopKilled(TroopKilledSignal obj)` | `CardPresentation/Battle/BattleDriver.cs:38830` | `FeedMissionChallengeSignals` | `BattleDriver.cs:39083` | 新行有/旧行无 |
| 237 | 战斗逻辑驱动.tsv:1373 | `EnergySpentSignal.Raise()` | `CardPresentation/Battle/BattleDriver.cs:38830` | `FeedMissionChallengeSignals` | `BattleDriver.cs:39083` | 新行有/旧行无 |
| 238 | 战斗逻辑驱动.tsv:1375 | `BattleManagerSupport.BroadcastUnitUsedSacrifice(CardScript unitSacrifice)` | `RuleEngine/Core/RuleCore.cs:12880` | `FireTriggerAlways` | `RuleCore.cs:13108` | 新行有/旧行无 |
| 239 | 战斗逻辑驱动.tsv:1380 | `RawCardScript.ResolveActiveAbility(BattleManager manager, CardScript thisCard, A` | `RuleEngine/Core/RuleCore.cs:15440` | `UseActiveAbilityCore` | `RuleCore.cs:15587` | 新行有/旧行无 |
| 240 | 战斗逻辑驱动.tsv:1380 | `RawCardScript.ResolveActiveAbility(BattleManager manager, CardScript thisCard, A` | `RuleEngine/Core/EffectResolver.cs:20633` | `RunSetReferenceCardRow` | `EffectResolver.cs:20928` | 新行有/旧行无 |
| 241 | 战斗逻辑驱动.tsv:1380 | `RawCardScript.ResolveActiveAbility(BattleManager manager, CardScript thisCard, A` | `RuleEngine/Core/EffectResolver.cs:20833` | `RunHighlightBloodThirstRow` | `EffectResolver.cs:20976` | 新行有/旧行无 |
| 242 | 战斗逻辑驱动.tsv:1386 | `EffectText.TryGiveInner(string low, string src)` | `RuleEngine/Core/EffectText.cs:10602` | `TailIsTimedGiveClause` | `EffectText.cs:10644` | 新行有/旧行无 |
| 243 | 战斗逻辑驱动.tsv:1387 | `EffectText.ApplyGiveDuration(EffectOp op, ref string payload, ref string targetT` | `RuleEngine/Core/EffectText.cs:10635` | `ApplyGiveDuration` | `EffectText.cs:10677` | 新行有/旧行无 |
| 244 | 战斗逻辑驱动.tsv:1387 | `EffectText.ApplyGiveDuration(EffectOp op, ref string payload, ref string targetT` | `RuleEngine/Core/EffectText.cs:10647` | `SplitMidPayloadDuration` | `EffectText.cs:10689` | 新行有/旧行无 |
| 245 | 战斗逻辑驱动.tsv:1387 | `EffectText.ApplyGiveDuration(EffectOp op, ref string payload, ref string targetT` | `RuleEngine/Core/EffectText.cs:10671` | `IndexOutsideQuotes` | `EffectText.cs:10713` | 新行有/旧行无 |
| 246 | 战斗逻辑驱动.tsv:1388 | `EffectText.ApplyDataOutlierDurations(CardDef card, List<EffectOp> ops)` | `RuleEngine/Core/EffectText.cs:10722` | `ApplyDataOutlierDurations` | `EffectText.cs:10743` | 新行有/旧行无 |
| 247 | 收藏.tsv:15 | `ArmyItemContainer.Initialize(CardArmy army, ToggleGroup toggleGroup, ArmySelecto` | `CardPresentation/Shell/ForgeTab.cs:1300` | `BuildArmyBadge` | `ForgeTab.cs:1329` | 新行有/旧行无 |
| 248 | 收藏.tsv:38 | `CardCollectionDisplay.SetCell(ICell cell, int index)` | `Shell/CollectionWindow.cs:320` | `CardsCounterText` | `CollectionWindow.cs:324` | 新行有/旧行无 |
| 249 | 收藏.tsv:188 | `FilterMethods.CheckIfMeetsCriteria(CardScript thisCard, CardScript evaluatedCard` | `RuleEngine/Core/EffectResolver.cs:20767` | `ApplyAbilityFamilyGateUnique` | `EffectResolver.cs:20803` | 新行有/旧行无 |
| 250 | 收藏.tsv:317 | `ToggleCollectionFilter._003CFilter_003Eb__7_2(T rc)` | `CardPresentation/Shell/CollectionWindow.cs:2087` | `FilteredCosmoNames` | `CollectionWindow.cs:2091` | 新行有/旧行无 |
| 251 | 收藏.tsv:348 | `WildcardDisplay.OnEnable()` | `CardPresentation/Deck/DeckRuntime.cs:9033` | `WildcardShineAfterTab` | `DeckRuntime.cs:9027` | 新行有/旧行无 |
| 252 | 收藏.tsv:348 | `WildcardDisplay.OnEnable()` | `CardPresentation/Deck/DeckRuntime.cs:1397` | `Build` | `DeckRuntime.cs:1276` | 新行有/旧行无 |
| 253 | 收藏.tsv:367 | `CollectionFilterController<T>.FillToggleList(List<T> optionsFinal, int listLimit` | `CardPresentation/Deck/DeckRuntime.cs:10308` | `RefreshHeaderChips` | `DeckRuntime.cs:10307` | 新行有/旧行无 |
| 254 | 教程_引导.tsv:37 | `TutorialArmySelectionButton.Initialize(CardArmy army, int indexNumber, DemoDeckI` | `Shell/TutorialModePopup.cs:607` | `BuildStageCell` | `TutorialModePopup.cs:560` | 新行有/旧行无 |
| 255 | 日常_奖励.tsv:24 | `ChallengeButton.Awake()` | `CardPresentation/Shell/ChallengeEngine.cs:111` | `Attach` | `ChallengeEngine.cs:123` | 新行有/旧行无 |
| 256 | 日常_奖励.tsv:26 | `ChallengeButton.OnButtonClick()` | `Shell/ChallengeManager.cs:317` | `OnChallengeButtonClick` | `ChallengeManager.cs:337` | 新行有/旧行无 |
| 257 | 日常_奖励.tsv:47 | `ChallengeManager.CancelPracticeMatchAndRefresh(GameObject source)` | `Shell/ChallengeManager.cs:227` | `CancelPracticeMatchAndRefresh` | `ChallengeManager.cs:247` | 新行有/旧行无 |
| 258 | 日常_奖励.tsv:92 | `DailyRewardPopup.RefreshOnArmyChange(CardArmy army)` | `Shell/DailyRewardPopup.cs:715` | `BuildArmySelector` | `DailyRewardPopup.cs:765` | 新行有/旧行无 |
| 259 | 日常_奖励.tsv:93 | `DailyRewardPopup.SetArmy(CardArmy army)` | `Shell/DailyRewardPopup.cs:752` | `BuildArmyCell` | `DailyRewardPopup.cs:801` | 新行有/旧行无 |
| 260 | 日常_奖励.tsv:264 | `FastModeBaseEvent.GetEventDeck()` | `Shell/LiveOpsEventWindow.cs:2475` | `EventDeckIndexFromEventSave` | `LiveOpsEventWindow.cs:2477` | 新行有/旧行无 |
| 261 | 日常_奖励.tsv:424 | `LiveopUIDrawer_EventAssetSprites.LiveopUIDrawer_EventAssetSprites.Initialize(ILi` | `CardPresentation/Shell/DraftModeWindow.cs:4751` | `BuildEventImagesHeader` | `DraftModeWindow.cs:4762` | 新行有/旧行无 |
| 262 | 日常_奖励.tsv:428 | `LoginBonusRewardDisplay.Setup(IMission mission, MissionChallengeProgress progres` | `Shell/DailyData.cs:2531` | `PreviewRewards` | `DailyData.cs:2592` | 新行有/旧行无 |
| 263 | 日常_奖励.tsv:510 | `MissionTimerDisplay.Update()` | `Shell/TimerDisplay.cs:366` | `UpdateTimer` | `TimerDisplay.cs:375` | 新行有/旧行无 |
| 264 | 日常_奖励.tsv:531 | `MissionsTab.RefreshRefillCount()` | `Shell/DailyData.cs:2587` | `RefillCounterVisible` | `DailyData.cs:2648` | 新行有/旧行无 |
| 265 | 日常_奖励.tsv:700 | `ShowEventArmiesPanel.Initialize(CardArmy[] cardArmies)` | `Shell/EnergySinglePlayerOnlyEventWindow.cs:1501` | `ShowEventArmies` | `EnergySinglePlayerOnlyEventWindow.cs:1521` | 新行有/旧行无 |
| 266 | 日常_奖励.tsv:701 | `ShowEventArmiesPanel.Reset()` | `Shell/EnergySinglePlayerOnlyEventWindow.cs:1538` | `ResetEventArmies` | `EnergySinglePlayerOnlyEventWindow.cs:1558` | 新行有/旧行无 |
| 267 | 日常_奖励.tsv:724 | `SkirmishEventWindow.OnDeckSelected(CardDeck deck, CardArmy army)` | `Shell/LiveOpsEventWindow.cs:2394` | `SetBattleInteractable` | `LiveOpsEventWindow.cs:2398` | 新行有/旧行无 |
| 268 | 日常_奖励.tsv:779 | `WeeklyMissionMilestone.DisplayCheckmark(int value, int currentValue, int lastRea` | `Shell/DailyData.cs:2560` | `WeeklyRewardSpecs` | `DailyData.cs:2621` | 新行有/旧行无 |
| 269 | 未分类_其他.tsv:99 | `BoardCriteria.DeepCopy()` | `RuleEngine/Data/CardDatabase.cs:2426` | `Parse` | `CardDatabase.cs:1126` | 新行有/旧行无 |
| 270 | 未分类_其他.tsv:312 | `GameObjectUniqueIdentifier.FindComponent(string id)` | `CardPresentation/Shell/MenuTutorialFlow.cs:1157` | `RegisterUnique` | `MenuTutorialFlow.cs:1163` | 新行有/旧行无 |
| 271 | 未分类_其他.tsv:430 | `MilestoneScoreBar.Initialize(int currentScore, GenericScoringMilestone[] scoring` | `CardPresentation/Shell/EnergySinglePlayerOnlyEventWindow.cs:429` | `SetScore` | `EnergySinglePlayerOnlyEventWindow.cs:1208` | 新行有/旧行无 |
| 272 | 未分类_其他.tsv:504 | `RectExtensions.Overlaps(RectTransform rect, RectTransform otherRect)` | `Deck/DeckRuntime.cs:4925` | `CheckFocusedArmy` | `DeckRuntime.cs:4926` | 新行有/旧行无 |
| 273 | 未分类_其他.tsv:509 | `RectSizeLimiter.SetLayoutHorizontal()` | `Shell/PracticeModePopup.cs:3769` | `AttachWarlordNameLimiter` | `PracticeModePopup.cs:3784` | 新行有/旧行无 |
| 274 | 本地化.tsv:7 | `LanguageSelector.OnLanguageChanged(int option)` | `Core/Loc.cs:3685` | `SetLanguage` | `Loc.cs:3708` | 新行有/旧行无 |
| 275 | 泛型与启动链_补读.tsv:232 | `MissionEvent<TS, TD>.RerollChallenge(MissionChallenge challenge, Action onComple` | `Shell/MissionRerollPopup.cs:487` | `Confirm` | `MissionRerollPopup.cs:479` | 新行有/旧行无 |
| 276 | 特效动画.tsv:10 | `AnimFXInstanceParticleAdjacent.ExecuteEffect()` | `CardPresentation/Battle/BattleDriver.cs:3127` | `PlayAdjacentVfx` | `BattleDriver.cs:3330` | 新行有/旧行无 |
| 277 | 特效动画.tsv:90 | `BattleParticleColliderManager.GetColliderTransform(BattleCollider colliderType)` | `WarpforgeVFX/Runtime/WFModuleCollisions.cs:396` | `LookupCollider` | `WFModuleCollisions.cs:686` | 新行有/旧行无 |
| 278 | 特效动画.tsv:177 | `CardDisplayLevelUpAnimationController.AnimationFinished()` | `CardPresentation/Shell/CardDetailPopup.cs:2869` | `PlayUpgradeAnimation` | `CardDetailPopup.cs:2958` | 新行有/旧行无 |
| 279 | 特效动画.tsv:230 | `MarkerlightController.AimingAttack(bool option)` | `Core/TraitFrames.cs:1186` | `AimingAttack` | `TraitFrames.cs:1221` | 新行有/旧行无 |
| 280 | 特效动画.tsv:230 | `MarkerlightController.AimingAttack(bool option)` | `Battle/BattleDriver.cs:33643` | `AimAttackingStatusPair` | `BattleDriver.cs:33909` | 新行有/旧行无 |
| 281 | 特效动画.tsv:403 | `SwarmVFXController.Initialize(CardScript card, Action onFinish)` | `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:921` | `SetMatCap` | `WarpforgeEffectPlayer.cs:975` | 新行有/旧行无 |
| 282 | 特效动画.tsv:493 | `BattleManager.ResolveBloodThirstAnim(BattleAction battleAction)` | `RuleEngine/Core/RuleCore.cs:12483` | `EmitBloodThirst` | `RuleCore.cs:12685` | 新行有/旧行无 |
| 283 | 特效动画.tsv:495 | `BattleManager.ResolveUnitShake(UnitShake unitShake)` | `CardPresentation/Battle/BattleDriver.cs:35706` | `SchedulePieceUnitShakes` | `BattleDriver.cs:36040` | 新行有/旧行无 |
| 284 | 特效动画.tsv:507 | `CardScript.CardToMinionAnim()` | `CardPresentation/Core/CardFeel.cs:2052` | `HandToBoard` | `CardFeel.cs:2065` | 新行有/旧行无 |
| 285 | 特效动画.tsv:521 | `EntityScript.GetCardEffectsToShow()` | `RuleEngine/Core/UnitState.cs:4274` | `RemovedTraitSeq` | `UnitState.cs:4278` | 新行有/旧行无 |
| 286 | 特效动画.tsv:522 | `RemnantBody.GetDeathParticlePosition()` | `CardPresentation/Core/CardView.cs:2939` | `TryGetRemnantNode` | `CardView.cs:2999` | 新行有/旧行无 |
| 287 | 特效动画.tsv:523 | `UnitShake.UnitShake(UnitShake.UnitShakeInfo shakeInfo, CardScript actingCard, Ca` | `CardPresentation/Battle/BattleDriver.cs:35925` | `ResolveUnitShakeTargetUnit` | `BattleDriver.cs:36074` | 新行有/旧行无 |
| 288 | 特效动画.tsv:528 | `CardScript.get_AttackStepTime()` | `CardPresentation/Core/CardFeel.cs:4436` | `MeleeAttackDurationOf` | `CardFeel.cs:4416` | 新行有/旧行无 |
| 289 | 社交_聊天.tsv:93 | `AllianceView.Draw(Group currentAlliance)` | `Shell/AllianceMemberTab.cs:1704` | `Apply` | `AllianceMemberTab.cs:1710` | 新行有/旧行无 |
| 290 | 社交_聊天.tsv:101 | `AllianceView.Start()` | `Shell/AllianceMemberTab.cs:2220` | `Create` | `AllianceMemberTab.cs:2407` | 新行有/旧行无 |
| 291 | 社交_聊天.tsv:229 | `ChatGlobalManager.GetCachedChatMessages(string channelId)` | `Shell/ChatTab.cs:142` | `Channel` | `ChatTab.cs:32` | 新行有/旧行无 |
| 292 | 社交_聊天.tsv:347 | `ChatPreview.Show()` | `CardPresentation/Shell/MainMenuRuntime.cs:3738` | `StepChatPreview` | `MainMenuRuntime.cs:3764` | 新行有/旧行无 |
| 293 | 社交_聊天.tsv:347 | `ChatPreview.Show()` | `CardPresentation/Shell/MainMenuRuntime.cs:2283` | `BuildChatPreview` | `MainMenuRuntime.cs:2289` | 新行有/旧行无 |
| 294 | 社交_聊天.tsv:347 | `ChatPreview.Show()` | `CardPresentation/Battle/ImageQuad.cs:711` | `SetFillAmount` | `ImageQuad.cs:668` | 新行有/旧行无 |
| 295 | 社交_聊天.tsv:387 | `FriendsTab.StartChallengeWithUser(string userName, string playfabId)` | `CardPresentation/Shell/FriendsTab.cs:640` | `OnInstantDuelClicked` | `FriendsTab.cs:647` | 新行有/旧行无 |
| 296 | 社交_聊天.tsv:476 | `ChatPreviewMessage.Text(m_overflowMode=1)` | `CardPresentation/Battle/Label.cs:2591` | `OverflowNow` | `Label.cs:2593` | 新行有/旧行无 |
| 297 | 联机.tsv:81 | `NetworkCustomManager.AttemptReconnect()` | `Net/NetMatchmaking.cs:61` | `InLobbyMatch` | `NetMatchmaking.cs:68` | 新行有/旧行无 |
| 298 | 菜单_外壳.tsv:100 | `DragAndDropItem.Awake()` | `CardPresentation/Deck/DeckRuntime.cs:5339` | `StartRowDrag` | `DeckRuntime.cs:5340` | 新行有/旧行无 |
| 299 | 菜单_外壳.tsv:102 | `DragAndDropItem.CreateEmptyObject()` | `CardPresentation/Deck/DeckRuntime.cs:5028` | `MoveDragPieces` | `DeckRuntime.cs:9335` | 新行有/旧行无 |
| 300 | 菜单_外壳.tsv:106 | `DragAndDropItem.OnBeginDrag(PointerEventData eventData)` | `CardPresentation/Deck/DeckRuntime.cs:5028` | `MoveDragPieces` | `DeckRuntime.cs:9335` | 新行有/旧行无 |
| 301 | 菜单_外壳.tsv:107 | `DragAndDropItem.OnDestroy()` | `CardPresentation/Deck/DeckRuntime.cs:5028` | `MoveDragPieces` | `DeckRuntime.cs:9335` | 新行有/旧行无 |
| 302 | 菜单_外壳.tsv:108 | `DragAndDropItem.OnDrag(PointerEventData eventData)` | `CardPresentation/Deck/DeckRuntime.cs:5028` | `MoveDragPieces` | `DeckRuntime.cs:9335` | 新行有/旧行无 |
| 303 | 菜单_外壳.tsv:110 | `DragAndDropItem.Toggle(bool option)` | `CardPresentation/Deck/DeckRuntime.cs:5028` | `MoveDragPieces` | `DeckRuntime.cs:9335` | 新行有/旧行无 |
| 304 | 菜单_外壳.tsv:211 | `EverguildInput.IsPointerOverUIObject(int pointerId)` | `CardPresentation/Battle/BattleDriver.cs:37262` | `PointerOverAnyUi` | `BattleDriver.cs:37303` | 新行有/旧行无 |
| 305 | 菜单_外壳.tsv:215 | `EverguildLayoutGroup.CalculateSpacing()` | `Shell/MissionsTab.cs:1303` | `BuildWeekly` | `MissionsTab.cs:1363` | 新行有/旧行无 |
| 306 | 菜单_外壳.tsv:378 | `GraphicsTab.CardsTextInHandDropdownChange(int chosenCardTextsInHand)` | `Shell/MainMenuRuntime.cs:3865` | `ReadTextInHandFormatAtBoot` | `MainMenuRuntime.cs:3958` | 新行有/旧行无 |
| 307 | 菜单_外壳.tsv:412 | `LiveopMenuContainer.OnLocalize()` | `CardPresentation/Shell/MainMenuRuntime.cs:543` | `Build` | `MainMenuRuntime.cs:401` | 新行有/旧行也有 |
| 308 | 菜单_外壳.tsv:477 | `NavigationPanelController.OnEventsRefreshed()` | `CardPresentation/Shell/MainMenuRuntime.cs:4235` | `RebuildNavForLiveOpsRefresh` | `MainMenuRuntime.cs:4255` | 新行有/旧行无 |
| 309 | 菜单_外壳.tsv:590 | `ScreenAspectRatioController.Start()` | `Shell/ShellRuntime.cs:1424` | `EnsureAspectClamp` | `ShellRuntime.cs:1426` | 新行有/旧行无 |
| 310 | 轮抽.tsv:19 | `DraftEventBase.OnMatchFound(string otherPlayerName)` | `CardPresentation/Shell/PlayEvent.cs:548` | `OnMatchFound` | `PlayEvent.cs:584` | 新行有/旧行无 |
| 311 | 锻造.tsv:23 | `BoosterPackVisualItem.Initialize(string localizedArmyText, BoosterPackOpenWindow` | `Shell/BoosterPackOpenWindow.cs:1508` | `BuildVisualItemLayer` | `BoosterPackOpenWindow.cs:1550` | 新行有/旧行无 |

**交叉核（B 类 311 条）**：新行有/旧行无 = 309 · 新行有/旧行也有 = 2

## 四、C 类逐条（多处候选 · 需人读）

共 9 条。候选行号列在「新落点」列（**不是**建议值，只是全部候选）。

| # | 账（文件:行） | 原版类.方法 | 旧 ref | 符号 | 候选行 |
|---:|---|---|---|---|---|
| 1 | 商店.tsv:57 | `CurrencyDrawer.Draw(Currency item, int quantity)` | `Shell/ItemDrawer.cs:1297` | `Icon` | 356/1295 |
| 2 | 战斗界面_HUD.tsv:99 | `BattleCardManager.RegisterCardInPlay(CardScript card)` | `CardPresentation/Battle/BattleDriver.cs:25180` | `ViewAt` | 19035/23444/23554/25221/25558 |
| 3 | 战斗界面_HUD.tsv:749 | `CardScript.DisplayEnemyCardInCenter(float timeToDraw, float stepTime, bool chang` | `CardPresentation/Battle/CardDisplayWindow.cs:1221` | `Show` | 1235/1238/1241/1256 |
| 4 | 战斗逻辑驱动.tsv:665 | `BattleManagerSupport.BroadcastUnitSummoned(BattleManager manager, CardScript act` | `RuleEngine/Core/RuleCore.cs:4715` | `DeployFreeCore` | 4884/5104 |
| 5 | 战斗逻辑驱动.tsv:934 | `CardScript.TacticPlayed(CardScript actingCard, CardScript targetCard)` | `RuleEngine/Core/RuleCore.cs:13359` | `FireTriggerOnSide` | 13603/13622 |
| 6 | 战斗逻辑驱动.tsv:1028 | `EntityScript.GetCombatDamage(AttackTypes attackType)` | `RuleEngine/Core/CounterAttack.cs:81` | `CurrentCombatAttack` | 64/233 |
| 7 | 菜单_外壳.tsv:101 | `DragAndDropItem.CheckScrollDrag()` | `Deck/DeckRuntime.cs:9214` | `DragAutoScroll` | 9212/9213 |
| 8 | 菜单_外壳.tsv:109 | `DragAndDropItem.OnEndDrag(PointerEventData eventData)` | `CardPresentation/Deck/DeckRuntime.cs:5349` | `EndDrag` | 5351/5353 |
| 9 | 锻造.tsv:26 | `BoosterShopTab.TryOpenContainerArmy(ShopWindow.OpenData context)` | `Shell/ShopWindow.cs:291` | `OpenData` | 292/299/300 |

## 五、D 类（不可解析 / 无声明行）

共 82 条，按原因分组：

- 名字在文件里但不构成声明行 —— **43** 条
- 路径不可解析 —— **33** 条
- 名字在整个文件里 0 命中 —— **6** 条

## 六、A 类（告警误报，名字就在那一行）

共 0 条：


## 七、按账本分布

| 账 | A | B | C | D | 合计 |
|---|---:|---:|---:|---:|---:|
| 个人资料_成就_排行.tsv | 0 | 2 | 0 | 0 | 2 |
| 其他_基础设施.tsv | 0 | 4 | 0 | 3 | 7 |
| 卡牌面_渲染.tsv | 0 | 2 | 0 | 2 | 4 |
| 卡组编辑.tsv | 0 | 6 | 0 | 0 | 6 |
| 商店.tsv | 0 | 8 | 1 | 2 | 11 |
| 战斗界面_HUD.tsv | 0 | 21 | 2 | 3 | 26 |
| 战斗逻辑驱动.tsv | 0 | 203 | 3 | 60 | 266 |
| 收藏.tsv | 0 | 7 | 0 | 1 | 8 |
| 教程_引导.tsv | 0 | 1 | 0 | 0 | 1 |
| 日常_奖励.tsv | 0 | 14 | 0 | 1 | 15 |
| 未分类_其他.tsv | 0 | 5 | 0 | 1 | 6 |
| 本地化.tsv | 0 | 1 | 0 | 0 | 1 |
| 泛型与启动链_补读.tsv | 0 | 1 | 0 | 0 | 1 |
| 特效动画.tsv | 0 | 13 | 0 | 3 | 16 |
| 社交_聊天.tsv | 0 | 8 | 0 | 0 | 8 |
| 联机.tsv | 0 | 1 | 0 | 0 | 1 |
| 菜单_外壳.tsv | 0 | 12 | 2 | 3 | 17 |
| 轮抽.tsv | 0 | 1 | 0 | 1 | 2 |
| 锻造.tsv | 0 | 1 | 1 | 1 | 3 |
| 音频_语音.tsv | 0 | 0 | 0 | 1 | 1 |

## 八、口径与复核

1. **总数对得上闸**：本表 A 档 **402** 条，与 `--warn-getters` 自己印的 `[SECOND] …402 条` 一致（另有 B 档 2 条，不进本项）。
2. **「可机械重钉」的口径** = 该符号在这个文件里**恰好一处「声明形状」的行**（`find_decl_candidates`，与 `--fix` 的最终闸同一套正则）。
   ⛔ **不是**「符号只出现一次」 —— 实测 `RefreshIdentity` 在 `ProfileTab.cs` 出现 **4** 次、`SpendNormalMana` 在 `RuleCore.cs` 出现 **13** 次，但**声明各只有 1 处**。
   ⇒ 这解释了旧账「192 条可机械重钉 / 161 条多处候选」为什么复算不出来：那一版把「出现多次」当成歧义（把调用点也算进候选）。按「唯一声明行」量，是 311 条。
3. **交叉核**（逐条回读源码）：**311** 条 B 里 **309** 条「新落点那一行有这个名字、旧 ref 那一行没有」= 真漂移；**2** 条旧行也有名字（重载 / 调用点同名）⇒ 重钉前人看一眼；**0** 条「新落点没有名字」（=0 才算健康）。
4. 🔴 **时效**：本表的新落点是 **2026-11-05 快照**，而本波 W2/W3/W4 正在改 `.cs`（`RuleCore.cs` / `BattleDriver.cs` / `Label.cs` …）⇒ **重钉前必须现读**（铁律 19「查完到用掉不得超过 2 天」），⛔ 别拿本表行号直接批量写账。
5. ⛔ **本表没有改任何账**：`--fix` 未跑（它只管 A 行主落点，而且必须在「没有 `.cs` 写手在飞」时串行做）。

