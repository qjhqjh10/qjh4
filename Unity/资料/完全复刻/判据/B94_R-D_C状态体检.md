# B94 · R-D · 全表 60 条 `C` 状态体检

> 只读体检代理 `R-D`（第九十四会话 · 波 1.5）· 2026-10-29
> 数据源：`Unity/资料/完全复刻/账/*.tsv` 全部 21 份，`状态 == C` 的行（现读 **60 条**）。
> 口径：只判「**今天还是不是 C**」。⛔ 未改任何源码/账/正本；⛔ 未跑 Unity。
> 每条判定四档：**A** 已有等价实现（落点 `文件:行号` + 关键句）· **B** 部分实现、缺口明确 · **C** 确实没有/判据仍缺 · **D** 原版不跑（本批 **0 条**判 D）。

---

## 〇 · 一句话结论

**60 条 C 里，有 16 条已经不该算 C 了** —— 它们在第九十四会话波 1（`B93_S1` / `B93_S3` / `B93_S4` / `B94_S3` / `B94-S4` / `B93_F4`）**已经把等价实现写进盘**，但**账没回填**：

- **11 条建议升 A**（方法已逐句落地，多数还有断言）：`商店:296` · `未分类_其他:41/180/181/182/183/184/185/186/191/516`
- **5 条建议升 B**（实现已在盘，但有明确本地残留让功能今天不生效）：`战斗逻辑驱动:1138/1367` · `未分类_其他:220/667/708`
- **44 条维持 C**。

> 🔴 **附带发现（比升档更重要）**：新写的三个断言文件 **`B93S3Assert.cs` / `B93S4Assert.cs` / `B93F5Assert.cs` 全部没有宿主接线**（`grep "…\.Run("` 除自身注释外 0 命中 ⇒ 从没被执行过）；`RuleEngineTest_EngineB94F4.cs` 的 `TestB94F4EngineGaps()` 也没进 `RuleEngineTest.Run()`。这正是 CLAUDE.md 反复记的「**断言文件建了但没人接线**」那一类（只有商店那两份 `ShopFamilyAssertB93S1/B94S1` 真接上了 `ShopScene.cs:5867/5870`）。

---

## 一 · 汇总表（按账文件）

| 账文件 | C 条数 | 建议 A | 建议 B | 维持 C | 判 D |
|---|---|---|---|---|---|
| 个人资料_成就_排行.tsv | 1 | 0 | 0 | 1 | 0 |
| 其他_基础设施.tsv | 3 | 0 | 0 | 3 | 0 |
| 商店.tsv | 1 | **1** | 0 | 0 | 0 |
| 战斗逻辑驱动.tsv | 30 | 0 | **2** | 28 | 0 |
| 日常_奖励.tsv | 3 | 0 | 0 | 3 | 0 |
| 未分类_其他.tsv | 21 | **10** | **3** | 8 | 0 |
| 特效动画.tsv | 1 | 0 | 0 | 1 | 0 |
| **合计** | **60** | **11** | **5** | **44** | **0** |

---

## 二 · 逐条体检（60 条）

> 「现账」列 = 账里记的状态；「体检」列 = 本次结论。证据列给「文件:行号 / grep 结果」。
> 维持 C 的 44 条，若账里已有 2026-10-28 当日现核且本次 grep 未见实现，证据写「账内 B93/92_xx 当日核 + 本次 grep 0 命中」。

### 2.1 个人资料_成就_排行.tsv（1 条）

| 账文件:行号 | 原版类.方法 | 现账 | 体检 | 关键证据 | 建议改账 |
|---|---|---|---|---|---|
| 个人资料_成就_排行.tsv:197 | RankedArmyBonusContainer.Clean | C | **C** | 5 张模式卡（`MainMenuRuntime.cs:2559-2597`）无 `Reward Event Container 1x1`（`grep "Reward Event Container"` 只命中注释）；`Clean()` 无宿主，内容 = liveop 阵营加成（服务端）⇒ 建出来只能是空卡 | 维持 C（阻塞在 liveop 数据，非文件归属） |

### 2.2 其他_基础设施.tsv（3 条）

| 账文件:行号 | 原版类.方法 | 现账 | 体检 | 关键证据 | 建议改账 |
|---|---|---|---|---|---|
| 其他_基础设施.tsv:160 | Extensions.AlmostEquals(Quaternion,…) | C | **C** | `grep Quaternion.Dot` 全仓 0；`AlmostEquals(Quat,Quat,maxAngle)` 这个封装 0 处；最接近的等价只有 `CardView.cs:9975` 的角阈判。调度台裁定 C（死代码本身不足以判 D） | 维持 C（缺 inline 等价；⛔ 别新建没人调的 helper） |
| 其他_基础设施.tsv:381 | PlayerDataManager.FixSkirmishDeck | C | **C** | `grep FixSkirmishDeck` 全仓 **0 命中**；遭遇模式本身在（`GameplayVariables`），副自动修复未做 | 维持 C |
| 其他_基础设施.tsv:779 | GameModes.get_CurrentSeasonNumber | C | **C** | `grep CurrentSeasonNumber` 全仓 **0 命中**；`IRankedEvent` 接口未建 ⇒ 双重阻塞 | 维持 C（N2 交付 `RankedEventBase` 前不可能动） |

### 2.3 商店.tsv（1 条）—— **升 A**

| 账文件:行号 | 原版类.方法 | 现账 | 体检 | 关键证据 | 建议改账 |
|---|---|---|---|---|---|
| 商店.tsv:296 | ShopStore.RemoveFinishedOffer | C | **A** | ✅ **已实现**：`Shell/ShopData.cs:624` `RemoveFinishedOffer(Func<ShopOffer,bool> isActive)` —— 逐句对 `.c`（倒序遍历、`!IsActive` 摘除、顺序不变），另有纯函数版 `PruneFinished`（`:648`）。**断言已接宿主**：`Editor/ShopFamilyAssertB93S1.cs:186-209`（D1..D6），由 `Editor/ShopScene.cs:5867` 调。`IsActive` 判据由调用方注入（无非 = 一个都不摘，同原版今天无在期活动） | **A** · 落点 `Shell/ShopData.cs:624` · 差异改「`IsActive` 由调用方注入；无调用方/无报价起止（同原版只由 LiveOps 刷新链调）；断言 D 段已接 ShopScene」 |

### 2.4 战斗逻辑驱动.tsv（30 条）—— 2 条升 B

| 账文件:行号 | 原版类.方法 | 现账 | 体检 | 关键证据 | 建议改账 |
|---|---|---|---|---|---|
| 战斗逻辑驱动.tsv:48 | BotConfigFTUE.GetBotDecks | C | **C** | `grep BotListFTUE\|AIBotsConfig\|skipChance` 数据侧 0 命中（账内 R4 现搜可复跑）；FTUE bot 池未建，数据在远端 | 维持 C |
| 战斗逻辑驱动.tsv:89 | AiScripted.CheckIfWaitingToClickCard | C | **C** | 谓词无实现（原版唯一调用方在缺失的 `CardScript.OnTouchDown` 体里）；F5 已补轻点推进口 ⇒ 无消费者、零可观测 | 维持 C |
| 战斗逻辑驱动.tsv:94 | AiScripted.ExecuteAction | C | **C** | 六支已逐句落 `Battle/CampaignScriptRunner.cs:536`，缺 `ICampaignScriptHost` 真实实现（只有 Null 出声兜底）；数据源本地不存在 | 维持 C |
| 战斗逻辑驱动.tsv:172 | BattleManager.AddTransformCardWithBuff | C | **C** | `transformUnit` 机制未实现：`grep transformUnit` 仅注释（`ActionPump.cs:299/1211/1467`），`EffectResolver` 无动词、`RuleCore` 无换卡函数；池内 0 可达 | 维持 C（移交 B01+B02+B05） |
| 战斗逻辑驱动.tsv:178 | BattleManager.AddTriggerCourtesy | C | **C** | 入队/出队两段已落（`ActionPump.EnqueueGrantedTrigger`；`CardDef.Courtesy` 前缀已闭），**仅剩** `chosenAbility 400` 的 op 分派（`EffectResolver`）；池内 0 格 | 维持 C（仅剩 EffectResolver 那半） |
| 战斗逻辑驱动.tsv:183 | BattleManager.AddTriggerRelentless | C | **C** | 同 178（同一入口两族）；仅剩 `chosenAbility 370` op 分派；池内 0 张 | 维持 C |
| 战斗逻辑驱动.tsv:248 | CheckForHeroDeckTransform | C | **C** | `grep DeckMeetsCriteria` 全仓 **0 实现**（只在 `BattleDriver.cs:34947` 注释）；`CheckForHeroDeckTransform` 仅注释 | 维持 C |
| 战斗逻辑驱动.tsv:372 | GetPremulliganScriptedData | C | **C** | `grep GetPremulliganScriptedData` 除注释 **0**；只有数据字段 `TutorialStageData.preMulligan`（6 关 `scriptedTurn` 全 false） | 维持 C |
| 战斗逻辑驱动.tsv:393 | BattleManager.GetWarcry | C | **C** | 取文两半已补（`DeckRules.WarcryId` + `Loc.WarcryKey/Warcry:3934`），但 `BattleDriver.cs:34981` 的 `PlayerBattleMeta.Warcry` 仍「恒空 + 出声」 | 维持 C（只差一行接线，F3） |
| 战斗逻辑驱动.tsv:422 | IsPremulliganScriptAvailable | C | **C** | `grep` 只命中 `TutorialScript.cs:338-341` 注释；谓词未建、执行跳未建（6 关恒假） | 维持 C |
| 战斗逻辑驱动.tsv:439 | BattleManager.LeaveBattle | C | **C** | 主链对（`BattleDriver.LeaveBattle:12778` + 无条件 `LeaveNetRoom`）；仅缺②「教程前两局重开」——`FirstTwoTutorialCompleted` 谓词已于 B93_S4 落地，但 `grep FirstTwoTutorial` 在 `BattleDriver` **0 命中**（未接进战斗侧） | 维持 C（教程重开支未接；卡背借还=外观线） |
| 战斗逻辑驱动.tsv:455 | PayCardPlayedCostOath | C | **C** | `grep chosenAbility 360` 无 handler；`OathCost`/`CanPayOathChoice`/`oathDouble` 重放是 `PayActiveAbilityCostOath`（能力激活）那一支，**不是**「打出战术卡付 oath」这一支 | 维持 C |
| 战斗逻辑驱动.tsv:482 | RemoveExtrinsicEffectsFrom | C | **C** | 统一入口 `Auras.RemoveExtrinsicBySource` 已备（B88_E4），残留 = 「每个动作结束」触发面（ActionPump） | 维持 C |
| 战斗逻辑驱动.tsv:559 | ResolveTriggerSacrifice | C | **C** | 225 独立广播未实现；B93_F7 的 C⇒B 已被 B93_FX1 反证撤回（`leftPlay` 那支对「真死」不可达）；池内 sacrifice 0 张 | 维持 C（三层差异已记） |
| 战斗逻辑驱动.tsv:573 | SetNextCardAsDeckDrawing | C | **C** | `grep SetNextCardAsDeckDrawing` 全仓 **0**；`inDeckDrawing` 呈现态未实现，视觉消费方（`card2DController`）是空壳 | 维持 C |
| 战斗逻辑驱动.tsv:636 | BroadcastRemoveEffectsFrom | C | **C** | 无统一入口的「动作类型 47 一拍」；手牌半仍缺 | 维持 C |
| 战斗逻辑驱动.tsv:637 | BroadcastRemoveFromGame | C | **C** | 无「移出游戏」状态位（cardState 11）；`UnitState`/`PlayerState` 都无该区 | 维持 C |
| 战斗逻辑驱动.tsv:647 | BroadcastUnitDamaged | C | **C** | 缺「攻击者必收一次」支：需 `DamageInfo.damageType==Attack(4)` 进广播载荷（`WhenEvent` 无此列） | 维持 C |
| 战斗逻辑驱动.tsv:676 | BoardAnalysis.MeetsCriteria | C | **C** | ④⑤ 已落；残余 ③`RandomChance(8)` 掷法、⑥卡级 `conditionsFilter`、`selectFromChooseResolution`（88 行/78 张，80 行静默走普通筛） | 维持 C |
| 战斗逻辑驱动.tsv:804 | CardScript.MinionPlayedIntoField | C | **C** | `grep OnDisplaySummonMessage\|召唤延迟` **0 命中**；四段时序已对，缺召唤延迟动画 AssetReference + 表现出口 | 维持 C |
| 战斗逻辑驱动.tsv:835 | ReactToUnitPoisoned | C | **C** | `BroadcastUnitPoisoned` 已建（`RuleCore.cs:16816`），监听端表驱动 `abilityTriggers==290` 池内 **0 张** ⇒ 休眠 | 维持 C（休眠 ≠ 不做） |
| 战斗逻辑驱动.tsv:921 | ShowCardBeingDrawn | C | **C** | 表现件在（`CardFeel.cs:3770`）；缺引擎发带 kind 的事件（EvtKind 无 Discard/Steal） | 维持 C |
| 战斗逻辑驱动.tsv:993 | CardTraitCollection.GetTraitForActiveAbilityRawCard | C | **C** | 六档推导 + 断言已落（`RuleEngineTest_EngineB94F4.cs`），但**表现层未接**（主动技能钮角标，18 张 Ultramarines oath 系恒空） | 维持 C（缺表现层接线，另立新账） |
| 战斗逻辑驱动.tsv:1037 | EntityScript.GetThisCardPlayedDamage | C | **C** | `grep GetThisCardPlayedDamage\|PlayedDamageAgainst` 除 `RuleCore.cs:7570` 注释 0；预览探针只挂棋盘卡 ⇒ 结构不可达 | 维持 C |
| 战斗逻辑驱动.tsv:1122 | MinionManager.GetNextSlotToCard | C | **C** | 算法已落（`EffectResolverPartial_B88E2.cs:255`）；③ `DoDeployOnce` 仍不读 `op.CreateAtPos`、不传 `requestSlot`（`grep CreateAtPos` 无生产命中） | 维持 C（剩 ③ 一处） |
| 战斗逻辑驱动.tsv:1138 | MinionManager.PositionWillBeAtLeft | C | **B** | ✅ **本次新实现**（2026-10-29 `F4`）：`RuleEngine/Core/TutorialPositioning.cs:60` `PositionWillBeAtLeft(targetSigned,refSigned,refIsHero)` 逐句对 `.c`，且已接进 `TutorialScript.cs:1292`（`ActingSlot.HasValue` 时按原版、否则回退旧近似），断言在 `RuleEngineTest_EngineB94F4.cs:366-382`。**残留**：`ActingSlot` **0 生产者**（`new TutorialAttempt` = 0 处）⇒ 仍走回退旧近似、行为零变化 | **B** · 落点 `RuleEngine/Core/TutorialPositioning.cs:60` + `TutorialScript.cs:1292` · 差异「谓词已逐句落地 + 已接（有条件）；缺 `ActingSlot` 生产者（`PermitsPlayerAction` 调用方填值）」 |
| 战斗逻辑驱动.tsv:1254 | RawCardScript.IsCumulativeMutation | C | **C** | 整套变异机制缺失；`mutationType` SO 4,392 次值全 0、`spellType 130` 0 张、trait 780 0 张 ⇒ 三重不可达；`cards_truth` 不带 `mutationType` | 维持 C（休眠；⛔ 不判 D） |
| 战斗逻辑驱动.tsv:1280 | RawCardScript.ReleaseFullHeroSprite | C | **C** | 函数体已复刻（`Core/CardArt.cs:1348`），`grep ReleaseFullHeroSprite` 调用 **0 处**（三宿主 DeckInfoPopup/DraftModeWindow/ItemDrawer 全在别批）⇒ 缺接线 | 维持 C |
| 战斗逻辑驱动.tsv:1367 | WarlordMasteryContainer.OnInitialize | C | **B** | ✅ **本次新实现**（2026-10-29）：`ProfileTab.cs:1833 ResolveWarlordMastery`（②③④ 三句纯逻辑）+ `:1875 FillWarlordMasteryContainer`（UI 半），**已接**（`ProfileTab.cs:1220`），断言 `B93S4Assert.cs:213`。**残留**：`VictoryCountersStore.RecordVictory` **0 调用方**（结算不写 key44）⇒ 战将精通格今天显示空 | **B** · 落点 `Shell/ProfileTab.cs:1833/1875` · 差异「OnInitialize 本体已落地+已接+断言；卡在 RecordVictory 结算写入（F3 的 BattleDriver）」 |
| 战斗逻辑驱动.tsv:1379 | CardScript.set_hasActiveAbility | C | **C** | 静态读法（`CardDef.HasActiveAbility:235`，装载赋值 `:1019`）；45 张有 `activeAbilityLogic` 却 `hasActiveAbility=0`；⛔ 不「好心点亮」 | 维持 C |

### 2.5 日常_奖励.tsv（3 条）

| 账文件:行号 | 原版类.方法 | 现账 | 体检 | 关键证据 | 建议改账 |
|---|---|---|---|---|---|
| 日常_奖励.tsv:155 | EndBattleRewardHolder.SetReward | C | **C** | `grep EndBattleRewardHolder` 只命中 `Shell/ItemDrawer.cs:29/1872` 注释；`EndPanel` 仍不建奖励行；两处硬阻塞（uGUI 画布 vs ÷108 世界单位；奖励数据来自服务器）未解 | 维持 C |
| 日常_奖励.tsv:515 | Missions.GetPointsPerArmyOnDailyLogin | C | **C** | 方法体已解；`MissionsConfig` 实例 SO 本地 0 份（服务端 LiveOps config）⇒ 数拿不到 | 维持 C（等远端） |
| 日常_奖励.tsv:526 | MissionsTab.InstantiateGridMissions | C | **C** | `MissionsTab.cs` 已是**故意为空**实现 + 出声；① 本地 0 条网格任务（服务端）② `FlexibleGridLayout` 组件本工程没有 | 维持 C |

### 2.6 未分类_其他.tsv（21 条）—— 10 升 A、3 升 B

| 账文件:行号 | 原版类.方法 | 现账 | 体检 | 关键证据 | 建议改账 |
|---|---|---|---|---|---|
| 未分类_其他.tsv:41 | ArmyProgressBar.SetArmies | C | **A** | ✅ **本次新实现**（2026-10-29 `B94-S4`）：`Shell/DraftModeWindow.cs:4349 SetArmies(bar,leftArmy,rightArmy,leftRect,rightRect,q)` 逐句对 `.c`（10 行），**已在生产接**（`:4204`/`:4322` 调用），断言 `B93S4Assert.cs:258`（SectionF） | **A** · 落点 `Shell/DraftModeWindow.cs:4349` · 差异「两格阵营图标已接；⛔ 军队来自 draft 远端数据（本地模拟）」 |
| 未分类_其他.tsv:180 | ContainerBuilder.MakeContainer(evt,type,parent) | C | **A** | ✅ **本次新实现**（`B94_S3`）：`Shell/OfferContainer.cs:1860`；断言 `B93S3Assert.cs:194`（SectionC） | **A** · 落点 `Shell/OfferContainer.cs:1860` · 差异「`LiveOpsAssetUtility.LoadComponent` 走注入点 `LiveOpsContainers.Load`（未装 = null + 出声）」 |
| 未分类_其他.tsv:181 | ContainerBuilder.MakeContainer(evt,parent) | C | **A** | ✅ `OfferContainer.cs:1879`（转调 `evt.GetContainer()` → 第三重载）；断言同 SectionC | **A** · 落点 `Shell/OfferContainer.cs:1879` |
| 未分类_其他.tsv:182 | ContainerBuilder.MakeContainer(evt,container,parent) | C | **A** | ✅ `OfferContainer.cs:1886`（`Instantiate` 注入 + 一次虚调用 `Initialize` 注入）；断言同 SectionC | **A** · 落点 `Shell/OfferContainer.cs:1886` |
| 未分类_其他.tsv:183 | ContainerHolder.Clear() | C | **A** | ✅ `OfferContainer.cs:1805`（`destroyAllChildren` 注入 + 清表 + 按 maxAmount 预留容量）；断言 `B93S3Assert.cs:155`（SectionB） | **A** · 落点 `Shell/OfferContainer.cs:1805` |
| 未分类_其他.tsv:184 | ContainerHolder.GetContainers() | C | **A** | ✅ `OfferContainer.cs:1815`（返回本体 List，非副本）；断言 SectionB | **A** · 落点 `Shell/OfferContainer.cs:1815` |
| 未分类_其他.tsv:185 | ContainerHolder.TryAdd(provider) | C | **A** | ✅ `OfferContainer.cs:1821`（转调泛型支；⚠ 原版此重载 `void`，我们返回 bool 是便利）；断言 SectionB | **A** · 落点 `Shell/OfferContainer.cs:1821` |
| 未分类_其他.tsv:186 | ContainerHolder.TryAdd<T>(provider,out T) | C | **A** | ✅ `OfferContainer.cs:1824`（`maxAmount>0 && <=Count ⇒ false`；否则 `MakeContainer` 委派）；断言 SectionB | **A** · 落点 `Shell/OfferContainer.cs:1824` |
| 未分类_其他.tsv:191 | ContainerService.OpenOfferContainer | C | **A** | ✅ `OfferContainer.cs:1944`（三跳闭包链：onUnlock→onCollect→onOpen；`PlayfabWrapper.UnlockContainer` 服务端 ⇒ 默认走 `onError` + 出声，§18·0）；断言 `B93S3Assert.cs:256`（SectionD） | **A** · 落点 `Shell/OfferContainer.cs:1944` · 差异「三跳链 + 注入点；服务端开箱关服后本地走 onError（🔶 出声），⛔ 不假装成功」 |
| 未分类_其他.tsv:220 | CursorByArmySO.GetCursor | C | **B** | ✅ 逻辑实现（`Shell/TutorialPointerUi.cs:493`，11 阵营表 `:474`，已接 `ApplyCursor:425`），断言 `B93S3Assert.cs:354`（SectionF）。**残留**：`Resources/Art/ui_menu/` 只有 `Cursor_Space_Marine_01[Glow]` **一对**，其余 10 阵营 20 张图**未 import** ⇒ 10 阵营指针视觉不生效（`GetCursor` 返回真名但图解析不到） | **B** · 落点 `Shell/TutorialPointerUi.cs:493` · 差异「表+逻辑已落地；缺 20 张图 Unity import（资源侧，⛔ 本代理不跑 Unity/不手写 .meta）」 |
| 未分类_其他.tsv:301 | FeatureChecker.OnEnable | C | **C** | `grep FeatureChecker` **0 命中**；开关判据在远端 `FeatureConfig`，本地拿不到该显该隐 | 维持 C（等远端 / §29·d） |
| 未分类_其他.tsv:359 | IconRotation.Start | C | **C** | `grep IconRotation` 只在 `LoadingMenu.cs`（那处是 `SimpleRotation`，非本件）；竞技场 `Background_Ring` 自转件缺驱动 | 维持 C（战场线） |
| 未分类_其他.tsv:360 | IconRotation.Update | C | **C** | 同上（周期定步长，与 `SimpleRotation` 相反；竞技场件缺驱动） | 维持 C |
| 未分类_其他.tsv:423 | MaterialChangerByQuality.Initialize | C | **C** | `grep MaterialChangerByQuality` **0 命中**；5 个实例全在战场侧（归战斗线），画质档只走 `QualitySettings.SetQualityLevel` | 维持 C（战场线） |
| 未分类_其他.tsv:424 | MaterialChangerByQuality.TryApply | C | **C** | 同上（预设内部那一层） | 维持 C |
| 未分类_其他.tsv:479 | PlayerInfo.SetBotInfo | C | **C** | `grep SetBotInfo` **0 命中**；`PlayerProfileWindow`/`StrangerProfilePage` 无 `IsBot`/`botName` bot 支（整支缺） | 维持 C |
| 未分类_其他.tsv:516 | ReferralContainer.Refresh | C | **A** | ✅ **本次新实现**（`B94_S3`）：`Shell/WindowsManager.cs:1650 ReferralContainer.Refresh(Action<string>)` —— 取词 `Loc.T("Demo/DraftMode/FreeButton")`（字面量 `0x4276890`），价签 `Setup` 由注入回调；断言 `B93S3Assert.cs:83`（SectionK）。残留 = `PriceDisplay` 类 + LiveOps 容器入口（外部，同原版靠容器链） | **A** · 落点 `Shell/WindowsManager.cs:1650` · 差异「取词+价签跳已落地（价签注入）；无 `PriceDisplay` 类/容器入口（原版靠 LiveOps 容器链）」 |
| 未分类_其他.tsv:584 | SimpleRotation.Update | C | **C** | `LoadingMenu.cs:388` 已有 `SimpleRotation` 类（B94_S3，齿轮 −20°/s）；但**竞技场 39 个 Highlight 自转件（100°/s）仍缺驱动** —— `grep SimpleRotation` 除 LoadingMenu/ShellRuntime 外 0 | 维持 C（竞技场件；菜单齿轮那处已单独复刻） |
| 未分类_其他.tsv:667 | UICornersGradient.ModifyMesh | C | **B** | ✅ 类已建（`Core/UICornersGradient.cs:52`，`ModifyMesh:63` 逐句对 `.c`：双线性四角色 × 顶点色 × 截断），断言 `B93F5Assert.cs:176`。**残留**：`grep AddComponent<UICornersGradient>` 生产 **0 命中** ⇒ 6 个原版实例（`MainMenuScene`/`DailyStreakPopup`/`GachaEventWindow`…）**未挂载**，我们这些位置仍走「建贴图时烘」 | **B** · 落点 `Core/UICornersGradient.cs:63` · 差异「类+算法已落地；缺 6 处挂载点（界面线取舍：哪几件改运行时 ModifyMesh）」 |
| 未分类_其他.tsv:689 | UIMessageController.OnDestroy | C | **C** | `ErrorMessageBanner`（`Battle/ErrorMessageBanner.cs:205`）只有 `Create/ShowError/ShowMessage/Advance`，**无单例 `Instance`、无 `Awake`/`OnDestroy`**（`grep class/void Awake/void OnDestroy/Instance` 无类成员命中） | 维持 C（缺单例守卫 + OnDestroy 收尾） |
| 未分类_其他.tsv:708 | UIResourceCounter.SetQuantity | C | **B** | ✅ 数字滚动（0.5s ceil 递增，`MainMenuRuntime.cs:2257`）+ `/max` 文案（`CounterText:2177`）+ 断言 `B93S4Assert.cs:82`（SectionB，仅测 qty/max 两态）。**残留** = `layoutGroupContentFixer.FixLayout()`（药丸/格宽重算）—— 真修要重写 `BuildResourcesBar`；`RewardProgressBarPanel.FixLayout` 是钩子不可复用 | **B** · 落点 `Shell/MainMenuRuntime.cs:2177/2257` · 差异「数字滚动+qty/max 已对齐；残差 = 药丸/格宽 FixLayout（ContentSizeFitter PreferredSize）」 |

### 2.7 特效动画.tsv（1 条）

| 账文件:行号 | 原版类.方法 | 现账 | 体检 | 关键证据 | 建议改账 |
|---|---|---|---|---|---|
| 特效动画.tsv:492 | BattleManager.ResolveAnimInfoRangeAttack | C | **C** | 4 态已对齐；唯一残差 = `animInfo+0x80` 的 `UnitTweenSO` 序列进链长 —— `grep TweenName\|TweenWait` 在 `CardAnimClips.cs`/`CardTween.cs` **0 字段**；`cardanim_clips.json` 无该映射列 | 维持 C（缺一条数据列 + 生成器 + 运行期解析，工具线） |

---

## 三 · 可直接改账的清单（16 条 · 对齐 11 列）

> 列序 = `域	原版类	方法	判据出处	判据要点	状态	我们的落点	差异	等级	轮次	备注`
> 等级/轮次/判据那三列保持原值不动（本次未重判等级）；下表只给「**要改的三格**：状态 / 我们的落点 / 差异」＋ 建议追加轮次标记。
> 落点格式照 `check_ledger_points` 要求：`方法级 —— 文件:行号`，行号以 2026-10-29 现读为准。

### 3.1 升 A（11 条）

| 账行 | 状态 | 我们的落点（新） | 差异（新，含关键句） | 建议轮次追加 |
|---|---|---|---|---|
| 商店:296 ShopStore.RemoveFinishedOffer | **A** | `Shell/ShopData.cs:624`（`RemoveFinishedOffer(Func<ShopOffer,bool>)`；纯函数版 `PruneFinished:648`） | 已逐句复刻倒序遍历摘 `!IsActive`（顺序不变）；`IsActive` 由调用方注入（无数据源 ⇒ 一个都不摘，同原版今天无在期活动）；断言 D1..D6 在 `Editor/ShopFamilyAssertB93S1.cs:186-209`（已接 `ShopScene.cs:5867`）。无调用方（同原版只由 LiveOps 刷新链调） | B94_R-D |
| 未分类_其他:41 ArmyProgressBar.SetArmies | **A** | `Shell/DraftModeWindow.cs:4349 SetArmies(bar,leftArmy,rightArmy,leftRect,rightRect,q)` | 两格阵营图标已接（生产 `:4204`/`:4322`）；判据 `decomp_full/ArmyProgressBar__SetArmies.c` 全文 10 行；断言 SectionF（`B93S4Assert.cs:258`）。⛔ 军队/比分来自 draft 远端（本地模拟） | B94_R-D |
| 未分类_其他:180 ContainerBuilder.MakeContainer(evt,type,parent) | **A** | `Shell/OfferContainer.cs:1860` | `evt as ILiveOps` → `LiveOpsContainers.Load`（注入点）→ 转调第三重载；断言 SectionC（`B93S3Assert.cs:194`）；`Load` 未装 = null + 出声 | B94_R-D |
| 未分类_其他:181 ContainerBuilder.MakeContainer(evt,parent) | **A** | `Shell/OfferContainer.cs:1879` | `evt.GetContainer()` → 第三重载；断言 SectionC | B94_R-D |
| 未分类_其他:182 ContainerBuilder.MakeContainer(evt,container,parent) | **A** | `Shell/OfferContainer.cs:1886` | `Instantiate`（注入）→ 失败出声 + null（原版 throw）→ 一次虚调用 `Initialize`（注入，未装不做）；断言 SectionC | B94_R-D |
| 未分类_其他:183 ContainerHolder.Clear() | **A** | `Shell/OfferContainer.cs:1805` | `destroyAllChildren`（注入）→ 清表 → 按 maxAmount 预留容量；断言 SectionB（`B93S3Assert.cs:155`） | B94_R-D |
| 未分类_其他:184 ContainerHolder.GetContainers() | **A** | `Shell/OfferContainer.cs:1815` | 返回本体 List（原版 return 那个表本身，非副本）；断言 SectionB | B94_R-D |
| 未分类_其他:185 ContainerHolder.TryAdd(provider) | **A** | `Shell/OfferContainer.cs:1821` | 转调泛型支并丢 out（原版此重载 `void`，我们返回 bool 是便利）；断言 SectionB | B94_R-D |
| 未分类_其他:186 ContainerHolder.TryAdd<T>(provider,out T) | **A** | `Shell/OfferContainer.cs:1824` | `maxAmount>0 && maxAmount<=Count ⇒ false`；否则 `MakeContainer` 委派 → 写 out → 入表 → true；断言 SectionB | B94_R-D |
| 未分类_其他:191 ContainerService.OpenOfferContainer | **A** | `Shell/OfferContainer.cs:1944` | 三跳闭包链 onUnlock→onCollect→onOpen；`PlayfabWrapper.UnlockContainer` 服务端 ⇒ 默认走 onError + 出声（§18·0）；三注入点可断言；断言 SectionD（`B93S3Assert.cs:256`） | B94_R-D |
| 未分类_其他:516 ReferralContainer.Refresh | **A** | `Shell/WindowsManager.cs:1650` | 取词 `Loc.T("Demo/DraftMode/FreeButton")`（字面量 `0x4276890`）→ 价签 `Setup`（注入回调）；无 `PriceDisplay` 类/容器入口（原版靠 LiveOps 容器链）；断言 SectionK（`B93S3Assert.cs:83`） | B94_R-D |

### 3.2 升 B（5 条）

| 账行 | 状态 | 我们的落点（新） | 差异（新，含缺口） | 建议轮次追加 |
|---|---|---|---|---|
| 战斗逻辑驱动:1138 PositionWillBeAtLeft | **B** | `RuleEngine/Core/TutorialPositioning.cs:60`（`PositionWillBeAtLeft`）+ `TutorialScript.cs:1292`（已接，有条件） | 谓词已逐句对 `.c`（左 = 自身−1 / 右 = 自身，照抄不修好）；`ActingSlot` 已接时按原版、否则回退旧近似；断言 `RuleEngineTest_EngineB94F4.cs:366-382`。**缺**：`ActingSlot` 0 生产者（`new TutorialAttempt` = 0 处）⇒ 今天行为零变化 | B94_R-D |
| 战斗逻辑驱动:1367 WarlordMasteryContainer.OnInitialize | **B** | `Shell/ProfileTab.cs:1833 ResolveWarlordMastery`（②③④）+ `:1875 FillWarlordMasteryContainer`（UI 半），已接 `:1220` | ②OrderByDescending 取最高 ③日志优先/HighestCardId ④UM47 兜底 已落地并接 UI；断言 SectionE（`B93S4Assert.cs:213`）。**缺**：`VictoryCountersStore.RecordVictory` 0 调用方（结算不写 key44）⇒ 格今天显示空 | B94_R-D |
| 未分类_其他:220 CursorByArmySO.GetCursor | **B** | `Shell/TutorialPointerUi.cs:493`（`CursorByArmy.GetCursor`，表 `:474`；已接 `ApplyCursor:425`） | 11 阵营表照 21 个 sprite 名排；扫完没有⇒false+两个 out null；DarkAngels 无 Glow 出声；断言 SectionF（`B93S3Assert.cs:354`）。**缺**：20 张图未 import（只 SpaceMarine 一对）⇒ 10 阵营视觉不生效 | B94_R-D |
| 未分类_其他:667 UICornersGradient.ModifyMesh | **B** | `Core/UICornersGradient.cs:63`（`ModifyMesh`） | 类已建（`BaseMeshEffect` 派生、类名==文件名），逐句对 `.c`：双线性四角色 × 顶点原色 × Clamp01 × 截断；断言 SectionC（`B93F5Assert.cs:176`）。**缺**：6 处实例未挂载（生产 `AddComponent` 0） | B94_R-D |
| 未分类_其他:708 UIResourceCounter.SetQuantity | **B** | `Shell/MainMenuRuntime.cs:2177 CounterText` + `:2257`（0.5s ceil 滚动） | 数字滚动 + qty/max 已对齐（断言 SectionB `B93S4Assert.cs:82`）。**缺**：`layoutGroupContentFixer.FixLayout()`（药丸/格宽重算，要重写 `BuildResourcesBar`；`RewardProgressBarPanel.FixLayout` 是钩子不可复用） | B94_R-D |

---

## 四 · 顺手发现（不阻塞改账，但请调度台处置）

1. **三个断言文件从未被执行**：`B93S3Assert.cs` / `B93S4Assert.cs` / `B93F5Assert.cs` 的 `Run()` 除自身注释外 **0 处宿主接线**（`grep "…\.Run("` 全空）。它们覆盖本清单里 A/B 的 9 条（容器族 / 指针图 / 价签 / 阵营进度条 / 战将精通 / 资源计数 / 四角渐变）。⇒ 就算账升了 A，**这些断言今天一条也没跑过**（对照：`ShopFamilyAssertB93S1/B94S1` 已接 `ShopScene.cs:5867/5870`）。
2. **`RuleEngineTest_EngineB94F4.TestB94F4EngineGaps()` 未进 `Run()`**：文件头自述「调度台在 Run() 里加一行 `Step(TestB94F4EngineGaps)`」，`grep TestB94F4EngineGaps` 除自身外 0 命中 ⇒ 也还没跑。它覆盖 `GetTraitForActiveAbilityRawCard`（账 993）与 `PositionWillBeAtLeft`（账 1138）等。
3. **本批升 A/B 的实现全部落在「别的代理」的文件里**（`Shell/OfferContainer.cs`、`Shell/DraftModeWindow.cs`、`Shell/ProfileTab.cs`、`Shell/WindowsManager.cs`、`Shell/TutorialPointerUi.cs`、`Shell/ShopData.cs`、`Core/UICornersGradient.cs`、`RuleEngine/Core/TutorialPositioning.cs`）—— 账未回填是因为**写手改完没同步主表**，不是判据问题。改账照本文件 §三 即可，无需重查。
4. **`D:/2` 未动、`git` 未动、未跑 Unity**：本次仅读代码 + grep，所有结论可复跑（grep 关键词已写进证据列）。

---

## 五 · 判 D = 0 的说明

60 条里**没有一条**够判 D：`Ext:160`（原版有方法，死代码不足以判 D，调度台已裁定 C）· `战斗:1254/835/178/183/559`（池内 0 张但「休眠 ≠ 不可达」，⛔ 不判 D）· 服务器相关（`ContainerService:191`、`Missions:515`、`MissionsTab:526`、`FeatureChecker:301`）按 §18·0 记「本地模拟」候选，不判 D。
