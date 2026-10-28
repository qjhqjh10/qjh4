# B89-RF · 落点闸「失败 63」清零

- 代理：**B89 波 1 收尾代理 `RF`**（2026-10-28）
- 范围：`check_ledger_points.py --all-A` 报的 **63 条「失败」**
- 结论：**63 条全部改成指向真声明行 · 档位 0 变动 · 判不了 0 条**
- 交付：`账/_parts/B89_RF.tsv`（63 行 · 11 列 · LF · 裸 CR 0）
- 自查：`merge_ledger_parts.py --dry --only B89_RF.tsv` ⇒ **合计：更新 63 · 新增 0 · 无变化 0**（无报错）
- 复算：把补丁合并进**主表副本**后跑同一套闸 ⇒ **A 4790 · 失败 0 · 漂移 41**（漂移条数与合并前一致，且我改的 63 条里没有一条变成漂移）

> 本报告只改 `我们的落点`（少数行追加 `差异` 注记）。⛔ 没动 `.cs`、没动主表、没动 git / 正本 / `d:/2`。

---

## 0. 失败主因分布（现读 63 条）

| 失败原因 | 条数 |
|---|---|
| 落点里没有 `文件:行`（缺口 prose / `—` / 只写文件名） | **21** |
| 括号里写了声明名，但文件里找不到这份声明（`_shiftX` / `_dragQuad`） | **2** |
| 括号里的名字与那一行声明的名字不符（`GetEvents`↔`ClearEvents` · `spacing`↔`cell`） | **2** |
| 只有括号/分隔符（`{` / `}`） | **2** |
| 目标行是**注释行 / 空行 / 语句行 / 不像声明**（"`)` 后接的是语句"等） | **36** |
| **合计** | **63** |

> 关键结论：**这 63 条里没有一条是「该判 A 但其实没实现」**。全部都是「实现（或等价物）在，但落点写成了散文 / 注释行 / 语句中间 / 旧行号」。因此 **0 降档**。

---

## 1. 逐条（域 | 方法 | 原落点 | 失败原因 | 改成什么 | 档位动没动）

| # | 域 | 方法 | 原落点 | 失败原因 | 改成 | 档位变动 |
|---|---|---|---|---|---|---|
| 1 | 其他_基础设施 | GameModes.CombatFinished(RankedMatchResult result, Action<OnlineMatchResult> callback) | CardPresentation/Shell/GameModesHandler.cs（GameModes 的等价类） | 落点里没有 `文件:行` | CardPresentation/Shell/GameModesHandler.cs:163（CombatFinished） | 否 |
| 2 | 其他_基础设施 | GameModes.DeserializeSharedData(JObject sharedData) | CardPresentation/Shell/GameModesHandler.cs（GameModes 的等价类） | 落点里没有 `文件:行` | CardPresentation/Shell/GameModesHandler.cs:187（DeserializeSharedData） | 否 |
| 3 | 其他_基础设施 | GameModes.InitializeHandler() | CardPresentation/Shell/GameModesHandler.cs（GameModes 的等价类） | 落点里没有 `文件:行` | CardPresentation/Shell/GameModesHandler.cs:398（InitializeHandler） | 否 |
| 4 | 其他_基础设施 | GameModes.SetAsCurrentlyPlayingEvent(IPlayEvent playEvent) | CardPresentation/Shell/GameModesHandler.cs（GameModes 的等价类） | 落点里没有 `文件:行` | CardPresentation/Shell/GameModesHandler.cs:315（SetAsCurrentlyPlayingEvent） | 否 |
| 5 | 战役 | CampaignTrack.GetNodesOffset(List<CampaignNodeInfo> nodes) | Shell/CampaignTab.cs:456（`_shiftX`） | 括号里写了声明名 `_shiftX`，但文件里找不到这份声明 | Shell/CampaignTab.cs:453（BuildTrack） | 否（差异列已订正「缺」那句） |
| 6 | 战斗界面_HUD | BattleManager.BasicBattleEndSequence() | BattleDriver.cs `BuildEndBattleDestroySequence()` :36132…（散文 + 无 `文件:行`） | 落点里没有 `文件:行` | CardPresentation/Battle/BattleDriver.cs:36125（BuildEndBattleDestroySequence） | 否 |
| 7 | 战斗界面_HUD | BattleManager.DestroyUnitsAfterBattleEnd(bool isPlayer, bool skipDeathSound) | BattleDriver.cs `BuildEndBattleDestroySequence()` 的 `t += EndSeqMinionStep`（散文） | 落点里没有 `文件:行` | CardPresentation/Battle/BattleDriver.cs:36125（BuildEndBattleDestroySequence） | 否 |
| 8 | 战斗界面_HUD | RemnantBody.DoDestroyByAttack() | CardPresentation/Core/CardFeel.cs:2921（SpawnRemnantDestroy 的 pos）· :2927 | 该行是赋值语句 | CardPresentation/Core/CardFeel.cs:2899（SpawnRemnantDestroy） | 否 |
| 9 | 战斗逻辑驱动 | BattleManager.BroadcastCollectedSpiritStone(CardScript unitCollected) | RuleCore.cs:12403 / :12404（对象初始化器语句） | 不像声明 | RuleEngine/Core/RuleCore.cs:12358（CollectWaystoneCore） | 否 |
| 10 | 战斗逻辑驱动 | BattleManager.IsValidActiveAbilityTarget(CardScript actingCard, CardScript target) | RuleCore.cs:11354（CanUseAbilityTarget 的 ② 闸） | 语句中间 | RuleEngine/Core/RuleCore.cs:11351（CanUseAbilityTarget） | 否 |
| 11 | 战斗逻辑驱动 | BattleManager.ResolveTransformIntoRemnant(BattleAction battleAction) | RuleCore.cs:9535（CleanupDeaths；改动在 :9859…） | 注释行 | RuleEngine/Core/RuleCore.cs:9513（CleanupDeaths） | 否 |
| 12 | 战斗逻辑驱动 | BattleManager.WaitOpponentLastActions() | NetBattle.cs:1315-1362（…）+ :2096（收尾支） | 区间首行是注释行 | CardPresentation/Net/NetBattle.cs:1346（BeginFinishWait）· CardPresentation/Net/NetBattle.cs:2081（DeferForPeerLastActions 收尾支） | 否 |
| 13 | 战斗逻辑驱动 | CardScript.ActivateBloodThirst() | RuleCore.cs:12461（EmitBloodThirst 的闸） | 注释行 | RuleEngine/Core/RuleCore.cs:12441（EmitBloodThirst） | 否 |
| 14 | 战斗逻辑驱动 | CardScript.ActivateMinion(bool isPlayerTurn) | BattleDriver.cs:26706（SyncBoard 里的 v.SetCanAct） | 语句中间 | CardPresentation/Battle/BattleDriver.cs:26426（SyncBoard） | 否 |
| 15 | 战斗逻辑驱动 | CardScript.EnoughPendingHealingToFull() | RuleCore.cs:14686（谓词）＋ :9320 ＋ ConditionEval.cs:233 | 注释行 | RuleEngine/Core/RuleCore.cs:14670（EnoughPendingHealingToFull）＋ :9320 ＋ ConditionEval.cs:233 | 否 |
| 16 | 战斗逻辑驱动 | CardScript.HasPackBuffTrait() | RuleCore.cs:5025-5034（FieldAttack 的 pack 支…） | 空行 | RuleEngine/Core/RuleCore.cs:4996（FieldAttack） | 否 |
| 17 | 战斗逻辑驱动 | CardScript.NonTargetSpellPlayed() | EffectResolver.cs:2776（TryFireCombo 调用点）· :17980 | 语句中间 | RuleEngine/Core/EffectResolver.cs:2737（PlayTacticBodyDeferred）· RuleEngine/Core/EffectResolver.cs:17988（TryFireCombo） | 否 |
| 18 | 战斗逻辑驱动 | CardScript.RecordPendingDamage(DamageInfo newDamage) | RuleCore.cs:7536（立即落地 + 现算谓词） | 语句中间 | RuleEngine/Core/RuleCore.cs:7534（WouldKill） | 否 |
| 19 | 战斗逻辑驱动 | CardScript.TacticPlayed(CardScript actingCard, CardScript targetCard) | EffectResolver.cs:2580（FireTriggerOnSide(Artifice)）· RuleCore.cs:12933-12976 | 注释行 | RuleEngine/Core/EffectResolver.cs:2524（PlayTacticCore）· RuleEngine/Core/RuleCore.cs:13359（FireTriggerOnSide） | 否 |
| 20 | 战斗逻辑驱动 | CardScript.TargetedSpellPlayed(CardScript actingCard, CardScript targetCard) | EffectResolver.cs:2776（同一处 TryFireCombo）· :17980 | 语句中间 | RuleEngine/Core/EffectResolver.cs:2737（PlayTacticBodyDeferred）· RuleEngine/Core/EffectResolver.cs:17988（TryFireCombo） | 否 |
| 21 | 战斗逻辑驱动 | CardScript.UndissolveToDeck() | RuleCore.cs:3073（ReshuffleCemeteryIntoDeck，数据级） | 语句行 | RuleEngine/Core/RuleCore.cs:14112（ReshuffleCemeteryIntoDeck） | 否 |
| 22 | 战斗逻辑驱动 | PlayerHand.RemoveFromGameCard(CardScript removedCard) | RuleCore.cs:16017（RemoveCardFromHandToRemoved，public）· 调用点 :2934 | 注释行 | RuleEngine/Core/RuleCore.cs:16023（RemoveCardFromHandToRemoved）· RuleEngine/Core/RuleCore.cs:2934（SweepEphemeral 调用点） | 否 |
| 23 | 日常_奖励 | LiveOpsAssetUtility.GetComponentReference(ILiveOps liveops, EventComponentReferenceType type) | LiveOpsHandler.cs:1493（LiveOpsLocalBoot.OpenEventPopup）（等价实现 = …） | 注释行 | Shell/LiveOpsHandler.cs:1561（OpenEventPopup） | 否 |
| 24 | 日常_奖励 | LiveOpsHandler.GetEvent(string id) | LiveOpsHandler.cs:696（GetEvent(string)） | 语句中间 | Shell/LiveOpsHandler.cs:764（GetEvent） | 否 |
| 25 | 日常_奖励 | LiveOpsHandler.GetEvent<T>(string id) | LiveOpsHandler.cs:709（GetEvent<T>） | 注释行 | Shell/LiveOpsHandler.cs:777（GetEvent） | 否 |
| 26 | 日常_奖励 | LiveOpsHandler.GetEvents(bool includeBlockedEvents) | LiveOpsHandler.cs:665（GetEvents(bool)） | 注释行 | Shell/LiveOpsHandler.cs:733（GetEvents） | 否 |
| 27 | 日常_奖励 | LiveOpsHandler.GetEvents<T>(bool includeBlockedEvents) | LiveOpsHandler.cs:676（GetEvents<T>） | 该行声明的是 `ClearEvents` | Shell/LiveOpsHandler.cs:744（GetEvents） | 否 |
| 28 | 日常_奖励 | LiveOpsHandler.RefreshHandler() | LiveOpsHandler.cs:460（RefreshHandler()） | 只有 `}` | Shell/LiveOpsHandler.cs:528（RefreshHandler） | 否 |
| 29 | 日常_奖励 | ChallengeLogMgr.TroopKilled(BattleAction) | RuleCore.cs:6195（TryCreditKillCore；…）· AchievementTally.cs:302 | 语句中间 | RuleEngine/Core/RuleCore.cs:6182（TryCreditKillCore）· RuleEngine/Core/AchievementTally.cs:302（AddTroopKilled） | 否 |
| 30 | 日常_奖励 | ChallengeLogMgr.LogHeal(BattleAction) | RuleCore.cs:9273（HealUnit；…）· AchievementTally.cs:308 | 语句行 | RuleEngine/Core/RuleCore.cs:9251（HealUnit）· RuleEngine/Core/AchievementTally.cs:308（AddHealDone） | 否 |
| 31 | 未分类_其他 | CollectSpiritStone.OnCollectSpiritStone(CollectSpiritStoneSignal obj) | BattleDriver.cs:19564（事件 drain 里按座位累计）· … | 语句中间 | CardPresentation/Battle/BattleDriver.cs:19462（PlaySignals）· :36899 · :32519 · :36903 · CardPresentation/Shell/ProfileData.cs:682 | 否 |
| 32 | 未分类_其他 | CustomRaycaster.Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) | — | 落点里没有 `文件:行` | CardPresentation/Shell/RaycastFilter.cs:141（AllowsHit） | 否（差异列已订正「没有这条过滤」那句） |
| 33 | 未分类_其他 | UIResourceCounter.AddQuantity(int amount, bool animated) | TopBar.cs:612（StepCounters 末尾那一拍调）· :1052 · … | 语句中间 | CardPresentation/Shell/TopBar.cs:601（StepCounters）· :1052 · :1092 · :1102 | 否 |
| 34 | 特效动画 | BattleManager.RemoveVoiceLinesControllerListeners() | BattleDriver.cs:5934（ChatShow cue）· :5912 · … | 语句中间 | CardPresentation/Battle/BattleDriver.cs:5906（ClickChatHud） | 否 |
| 35 | 特效动画 | BattleManager.StopHandAnim(float delay) | — | 落点里没有 `文件:行` | CardPresentation/Battle/BattleDriver.cs:35622（TickPendingBerzerkPointer）· :35551（常量） | 否 |
| 36 | 特效动画 | CardScript.CanContinueAnim() | AnimGate.cs:1（`AnimGate.CanContinue`）+ BattleDriver.cs 8 处已接（…） | `:1` 是文件头注释行 | CardPresentation/Battle/AnimGate.cs:30（CanContinue） | 否 |
| 37 | 特效动画 | CardScript.ResolveAttackAnimationEffects(Transform anchor, Vector3 attackDirection) | BattleDriver.cs:21629（命中音，单条 cue）· :22404 · BattleSfx.cs:78-88 | 注释行 | CardPresentation/Battle/BattleDriver.cs:22362（ScheduleAttackPresentation）· CardPresentation/Battle/BattleSfx.cs:78 | 否 |
| 38 | 社交_聊天 | AllianceMemberList.HandleMemberClick(GroupMember member, Transform memberTransform) | AllianceMemberTab.cs:1892（行 Hit ⇒ OpenMemberOptions）· `:1917` · `:1940` · … | 注释行 | Shell/AllianceMemberTab.cs:1914（OpenMemberOptions）· :1889（行 Hit 那一跳） | 否 |
| 39 | 菜单_外壳 | AdaptFlexibleGridLayoutByAspectRatio.ModifyFlexibleLayout() | MainMenuRuntime.cs:2465（void BuildGameModes(Transform root)）（…） | 注释行 | Shell/MainMenuRuntime.cs:2453（BuildGameModes） | 否 |
| 40 | 菜单_外壳 | AdaptFlexibleGridLayoutByAspectRatio.OnDisable() | 同上 | 注释行 | Shell/MainMenuRuntime.cs:2453（BuildGameModes） | 否 |
| 41 | 菜单_外壳 | AdaptFlexibleGridLayoutByAspectRatio.OnEnable() | 同上 | 注释行 | Shell/MainMenuRuntime.cs:2453（BuildGameModes） | 否 |
| 42 | 菜单_外壳 | AdaptFlexibleGridLayoutByAspectRatio.ResolutionHasChanged() | 同上 | 注释行 | Shell/MainMenuRuntime.cs:2453（BuildGameModes） | 否 |
| 43 | 菜单_外壳 | DragAndDropItem.Awake() | DeckRuntime.cs:1164（_dragQuad）/ :1196（_dragRow）/ :5341（拖起） | `_dragQuad` 是**无访问修饰符的字段**，闸既不认声明也不认成员 | CardPresentation/Deck/DeckRuntime.cs:5339（StartRowDrag）· :1164 · :1166 | 否 |
| 44 | 菜单_外壳 | DragAndDropItem.CheckScrollDrag() | Deck/DeckRuntime.cs（拖拽段：_dragRow/_dragQuad） | 落点里没有 `文件:行` | CardPresentation/Deck/DeckRuntime.cs:9214（DragAutoScroll）· :9167（DragEdgeScrollFor） | 否 |
| 45 | 菜单_外壳 | DragAndDropItem.OnEndDrag(PointerEventData eventData) | Deck/DeckRuntime.cs（拖拽段：…） | 落点里没有 `文件:行` | CardPresentation/Deck/DeckRuntime.cs:5349（EndDrag）· :9187（EndDragAt） | 否（差异列已订正「缺」那句） |
| 46 | 菜单_外壳 | DragAndDropItem.Update() | Deck/DeckRuntime.cs（拖拽段：…） | 落点里没有 `文件:行` | CardPresentation/Deck/DeckRuntime.cs:1205（Update） | 否 |
| 47 | 菜单_外壳 | EverguildLayoutGroup.CalculateSpacing() | Shell/MissionsTab.cs:1437（里程碑行 spacing） | 该行声明的是 `cell` | Shell/MissionsTab.cs:1106（BuildSkullsCard）· Shell/MissionsTab.cs:1303（BuildWeekly） | 否 |
| 48 | 菜单_外壳 | LayoutGroupContentFixer.FixLayout() | MenuDraw.cs（Node/Nine/Text/Tiled 建件即算绝对 px）+ 各窗 Build()/Rebuild*ForTest() | 落点里没有 `文件:行` | CardPresentation/Shell/MenuDraw.cs:228（ApplyPxRect） | 否（差异列加了说明） |
| 49 | 菜单_外壳 | LiveopHelpWindow.Open() | BaseEventPopup.cs:558（SetEventImageRef = Open 里那一跳） | 语句中间 | CardPresentation/Shell/BaseEventPopup.cs:546（AutoAttachEventImage） | 否 |
| 50 | 菜单_外壳 | LiveopMenuContainer.Awake() | MainMenuRuntime.cs:2819（BuildModeCard…） | 语句中间 | CardPresentation/Shell/MainMenuRuntime.cs:2807（BuildModeCard） | 否 |
| 51 | 菜单_外壳 | LiveopMenuContainer.OnClick() | MainMenuRuntime.cs:3086-3101（…）+ :1396 | 语句中间 | CardPresentation/Shell/MainMenuRuntime.cs:2807（BuildModeCard） | 否 |
| 52 | 菜单_外壳 | LiveopMenuContainer.OnLocalize() | Loc.cs:3697（SetLanguage 末尾发广播）· … | 语句中间 | CardPresentation/Core/Loc.cs:3685（SetLanguage）· CardPresentation/Shell/MainMenuRuntime.cs:543 · :4124 · :4176 | 否 |
| 53 | 菜单_外壳 | LiveopMenuContainer.Release(EventAssetReferenceType eventAssetReferenceType) | 无对应物（MainMenuRuntime 不持有 liveops 资产句柄） | 落点里没有 `文件:行` | CardPresentation/Shell/BaseEventPopup.cs:583（Close） | 否（落点是**等价出口**，见 §3） |
| 54 | 菜单_外壳 | LiveopMenuContainer.ReleaseLoadedContent() | 无对应物（同 L413） | 落点里没有 `文件:行` | CardPresentation/Shell/BaseEventPopup.cs:583（Close） | 否（同上） |
| 55 | 菜单_外壳 | LiveopUIDrawer_GameModeIcon.Initialize(ILiveOps liveopEvent) | MainMenuRuntime.cs:2819（art 入参）+ CardArt.cs（静态模式图） | 语句中间 | CardPresentation/Shell/MainMenuRuntime.cs:2807（BuildModeCard） | 否 |
| 56 | 菜单_外壳 | PromptPopup.InstantiateInput(PromptContext.InputData data) | —（缺 `TMP_InputField`；本仓外壳没有文字输入系统） | 落点里没有 `文件:行` | CardPresentation/Shell/PromptPopup.cs:442（InstantiateInput） | 否 |
| 57 | 菜单_外壳 | TMProWarpText.Awake() | —（共用件 `Battle/Label.cs` 的字形顶点层） | 落点里没有 `文件:行` | CardPresentation/Core/TMProWarpText.cs:82（Awake） | 否 |
| 58 | 菜单_外壳 | TMProWarpText.OnDisable() | —（同上） | 落点里没有 `文件:行` | CardPresentation/Core/TMProWarpText.cs:95（OnDisable） | 否 |
| 59 | 菜单_外壳 | TMProWarpText.OnEnable() | —（同上） | 落点里没有 `文件:行` | CardPresentation/Core/TMProWarpText.cs:88（OnEnable） | 否 |
| 60 | 菜单_外壳 | TMProWarpText.ReactToTextChanged(UnityEngine.Object obj) | —（同上） | 落点里没有 `文件:行` | CardPresentation/Core/TMProWarpText.cs:103（ReactToTextChanged） | 否 |
| 61 | 菜单_外壳 | TMProWarpText.Reset() | —（同上） | 落点里没有 `文件:行` | CardPresentation/Core/TMProWarpText.cs:113（Reset） | 否 |
| 62 | 菜单_外壳 | TMProWarpText.WarpText() | —（同上） | 落点里没有 `文件:行` | CardPresentation/Core/TMProWarpText.cs:135（WarpText） | 否 |
| 63 | 菜单_外壳 | WindowHeaderWithBackButton.Initialize(ILiveOps liveopEvent, Action OnBackButtonPressed) | WindowHeaderWithBackButton.cs:132（Initialize(ILiveOps, Action)） | `:132` 只有 `{` | CardPresentation/Shell/WindowHeaderWithBackButton.cs:129（Initialize） | 否 |

---

## 2. 档位变化汇总

**无。63 条全部保持 `状态 = A`，`等级` 一列也一个字没改。**

理由（逐类给出证据，不是"找不到就当没有"）：

| 看上去像"该降档"的 | 实查结论 |
|---|---|
| #32 `CustomRaycaster.Raycast` 落点 `—` | **实现真的在**：`Shell/RaycastFilter.cs` 整类已建（`Register` / `UnRegister` / `ClearRaycastFilters` / `RegisterObjectAndChildrenGraphics` / `Allows` / `AllowsHit`，`:53` 起），消费口 = `PointerLayer.HitButton` 排序前调 `AllowsHit`。**原判词「我们的教程层没有这条点击过滤」已过期**，已在 `差异` 列就地订正（铁律 5）。 |
| #56 `PromptPopup.InstantiateInput` 落点 `—` | **实现真的在**：`Shell/TextInputField.cs` 新建 + `Shell/PromptPopup.cs:442 InstantiateInput(...)`（`差异` 列 B89-R3b 已记）。 |
| #57–#62 `TMProWarpText` 六条落点 `—` | **实现真的在**：`Core/TMProWarpText.cs` 整类已建，六个方法全在（`:82/#57 · :88/#59 · :95/#58 · :103/#60 · :113/#61 · :135/#62`）。 |
| #53/#54 `LiveopMenuContainer.Release*` 落点「无对应物」 | **确实没有同名实现**（我们没有 Addressables 引用计数层），但 `差异` 列的理由（"没有可释放的句柄 ⇒ 无副作用"）**成立**，不是"该做没做"。⇒ 不改档；落点改指 `BaseEventPopup.Close`（我们唯一的"把 liveop 资产句柄清空"那一跳），**只作等价出口记录**，已在 `差异` 列写明"⛔ 不等于「已卸载」"。 |
| #5 `CampaignTrack.GetNodesOffset` 差异列写"缺 offset 口径" | **已落地**：`Shell/CampaignTab.cs:490` `_shiftX = _vpR.x1 + (StartingOffsetX − minX) * Ratio`，`StartingOffsetX` = 原版 `startingOffset.x` = 330。⇒ 落点改指 `BuildTrack`（offset 口径就在它里面），并在 `差异` 列订正，同时保留真残差（"多加了视口左缘 `_vpR.x1`"）。 |
| #43/#45 `DragAndDropItem` 差异列写"缺" | **已落地**：`StartRowDrag`(:5339) / `EndDrag`(:5349) / `EndDragAt`(:9187) / `DragAutoScroll`(:9214) / `DragEdgeScrollFor`(:9167) 都在。已在 #45 的 `差异` 列订正"缺"那句。 |
| #48 `LayoutGroupContentFixer.FixLayout` | 本仓无 uGUI LayoutGroup ⇒ 无 `ForceRebuildLayoutImmediate` 这个动作；等价物 = **建件那一刻**按原版矩形写死绝对 px（`MenuDraw.ApplyPxRect`）。属"结构等价"，不是缺行为。 |

---

## 3. 判不了的

**0 条。** 但下面 5 条的落点是「最贴近的声明」，**不等于同名方法**，请复核时优先看它们（都已在 `差异` 列写清）：

| # | 方法 | 落点 | 为什么是「近似」 |
|---|---|---|---|
| 5 | `CampaignTrack.GetNodesOffset` | `CampaignTab.cs:453（BuildTrack）` | offset 的算式是 `BuildTrack` **体内的语句**（:490），没有独立方法。⛔ 语句行不能当落点（闸判"`)` 后接的是语句"）。 |
| 43 | `DragAndDropItem.Awake` | `DeckRuntime.cs:5339（StartRowDrag）` | 原版 Awake 的 autoInitialize 支在出货 5/5 实例里 `=0`（`差异` 列已证），真机制在运行时 `Initialize+Togg(true)`；我们的等价物是拖拽状态机，而它的建/清在 `StartRowDrag`/`EndDrag`。 |
| 48 | `LayoutGroupContentFixer.FixLayout` | `MenuDraw.cs:228（ApplyPxRect）` | 无 uGUI 布局管线 ⇒ 无"重建布局"这个动作。 |
| 53 | `LiveopMenuContainer.Release` | `BaseEventPopup.cs:583（Close）` | 无引用计数层，**没有同名实现**；只是"句柄清空"这一处最接近。 |
| 54 | `LiveopMenuContainer.ReleaseLoadedContent` | `BaseEventPopup.cs:583（Close）` | 同上（无加载器）。 |

---

## 4. 顺手发现

1. 🔴 **`check_ledger_points.py --root DIR` 不改"读哪份账"** —— `--root` 只进 `roots`（用来 `find_file` 找 `.cs`），**账本目录仍然是硬编码的 `LEDGER_DIR`**。
   实测：`check_ledger_points.py --all-A --root d:/4/_tmp_view/rf/ledgersim` **仍然读主表、报 63 条**。
   ⇒ **要对副本跑闸必须显式传账本路径**：`check_ledger_points.py d:/4/_tmp_view/rf/ledgersim/*.tsv`（或按文件传）。
   本次复算就是这么做的（`check_file(副本路径)` × 20 份）⇒ 失败 0。
2. 🔴 **无访问修饰符的私有字段 / 常量行，永远不能当 A 档落点**（这是 #43 的真因，且会反复发生）：
   `DeckRuntime.cs:1164` `GameObject _dragQuad;` ⇒ `looks_like_decl` 判"既无 `(` 也无 `=>`/`{`"失败；
   `member_decl_kind` 又要求**以 `public/private/protected/internal` 开头** ⇒ 也返 None。
   同样形状：`BattleDriver.cs:36824` `const float EndSeqMinionStep = 0.3f;`（`const` 不在 `MODS` 里）。
   ⇒ 建议给 `member_decl_kind` 的 `_MEMBER_MOD` 补 `const` 与"无修饰符字段"（或至少加一条 `const` 支），否则凡是"复刻成常量/私有字段"的等价物都只能绕。
3. 🔴 **`日常_奖励.tsv` 的 `LiveOpsHandler` 那 5 条是【整段 +68 行】的旧快照**：
   `GetEvents(bool)` 账 665 / 真身 733 · `GetEvents<T>` 676 / 744 · `GetEvent(string)` 696 / 764 · `GetEvent<T>` 709 / 777 · `RefreshHandler` 460 / 528 —— **差值全是 +68**。
   ⇒ 那不是"个别写错"，是 `LiveOpsHandler.cs` 在这几行**上方**净增了 68 行（本次已逐条改成真身行号）。同类页面（按旧快照整批落点的域）建议整份复核一次，别等闸一条条报。
4. ⚠️ **"落在 `{` 上"是同一族的残留形态**：#63 `WindowHeaderWithBackButton.cs:132` 是 `{`（真身 129）、#28 `LiveOpsHandler.cs:460` 是 `}`（真身 528）。
   `--fix` 对这两类会报 `NEED-HUMAN`（目标行解析不出声明名）⇒ 每次都要人读。若能在 `--fix` 里加一条"目标行是纯 `{`/`}` ⇒ 往上/往下找最近的声明"，可省掉这一类。
5. ✅ **本次没有把任何一条改成"歧义漂移"**：我特意选了**能过闸的那一处**（如 `FieldAttack` 有 4996/6556 两处，取 4996 —— 账里写的那一行本身在候选里 ⇒ 判通过；`FireTriggerOnSide` 13359/13378 同理取 13359；`CanContinue` 30/39 取 30；`Initialize` 115/129 取 129）。
   ⇒ 合并后**漂移仍是 41 条**（条数与合并前一致，且这 63 条里 0 条进漂移）。
6. 📌 **剩余 41 条漂移不在本任务范围**（它们是 getter / 同名歧义 / `--fix` 需人读那一类），本次**一条都没动**，以免和正在跑的账写手撞车。

---

## 5. 复现步骤（照抄即可）

```bash
# 1) 干跑（必须看到「更新 63 · 无报错」）
python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry --only B89_RF.tsv

# 2) 真合并（合并完请把 B89_RF.tsv 移进 _parts/_merged_B89_RF/，防"旧内容定时炸弹"）
python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B89_RF.tsv

# 3) 复跑闸（期望：失败 0 · 漂移 41）
python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A
```
