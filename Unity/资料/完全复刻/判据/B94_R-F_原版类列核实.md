# B94_R-F —— 「原版类列残缺」448 行核实（可直接照改清单）

> 2026-10-29 · 只读账务代理 R-F（第九十四会话 · 波1.5）· **零源码/账/正本改动**。
> 数据源 = `判据/B93_Q4_原版类列正确值.tsv`（448 行；Q4 于 2026-10-28 21:39 生成）。
> 核实对象 = 主表现读 `账/泛型与启动链_补读.tsv`（Q4 生成后主表又被 `b93_shrink.py`（缩表）删过行 + 合过补丁 ⇒ **行号已漂**，本清单**全部按当前主表现读行号**给）。
> 判据独立复核源：`d:/2/tools/il2cpp_out/dump.cs`（类型定义正本）＋ `d:/2/Warpforge_code/Scripts/*`（ILSpy 签名桩，21 个真实泛型类交叉验证）。

---

## §一 汇总

| 分类 | 行数 | 说明 |
|---|---:|---|
| **可改（现账仍残缺，本清单 §二 逐行给出）** | **415** | 真实泛型 181 行 + 匿名类型 234 行 |
| **本来正确（清单误报）** | **0** | 见 §三 |
| **判不了** | **0** | 见 §四 |
| （另）**行已被删、无需改** | **33** | Q4 之后 `缩表` 删掉的重复行；现账已不存在 ⇒ 不在 §二。见 §三·补 |

> 明细：可改 415 行 = **真实泛型类 181 行**（现值形如 `FastModeBaseEvent<TS,`，缺第二/第三个类型参数或 `>`）＋ **编译器匿名类型 234 行**（现值形如 `<>f__AnonymousType75<<DeckId>j__TPar,`）。
> **全部 415 行都在同一份主表 `账/泛型与启动链_补读.tsv`**（已扫全部 21 份主表：残缺 `原版类` 只此一份，合计正好 415 ⇒ 与 448−33(已删) 吻合）。

### §一·附 正确值已独立复核（不是照抄 Q4）

- **21 个真实泛型类**：`dump.cs` 类声明行 **21/21 一致** ＋ ILSpy 签名桩类声明 **21/21 一致**（双源）。
- **78 个匿名类型族**：`dump.cs` 类声明逐行按平衡尖括号解析 **78/78 一致**（并用「每条账行的 依据 行号确实指向该族声明」逐行验过 **234/234**）。
- 因此 §二 的 `正确值` 列可直接采用；`判据` 列 = `dump.cs:<行>`（泛型类另附 ILSpy `文件:行`）。

---

## §二 可执行修改清单（415 行 · 账文件 = `账/泛型与启动链_补读.tsv`）

> 改法：把该行 **第 2 列 `原版类`** 的现值整格替换为 `正确值`。其余 10 列**不动**。
> ⚠️ 行号是**当前主表行号**（含表头，表头=第 1 行）。改前请按「方法签名 + 现值」再对一眼那行内容（防再漂）。

### §二·A 按类聚合（99 个不同值 → 415 行落点）

| 现值（残缺） | 正确值 | 行数 | 判据 |
|---|---|---:|---|
| `BaseInboxEvent<TSave,` | `BaseInboxEvent<TSave, TData>` | 8 | dump.cs:129192 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/BaseInboxEvent.cs:5 |
| `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | 14 | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | 26 | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| `FancyCell<TItemData,` | `FancyCell<TItemData, TContext>` | 5 | dump.cs:692497 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyCell.cs:3 |
| `FancyCellGroup<TItemData,` | `FancyCellGroup<TItemData, TContext>` | 5 | dump.cs:692832 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyCellGroup.cs:3 |
| `FancyGridView<TItemData,` | `FancyGridView<TItemData, TContext>` | 11 | dump.cs:692982 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridView.cs:7 |
| `FancyGridViewCell<TItemData,` | `FancyGridViewCell<TItemData, TContext>` | 1 | dump.cs:693152 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridViewCell.cs:3 |
| `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | 18 | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| `FancyScrollRectCell<TItemData,` | `FancyScrollRectCell<TItemData, TContext>` | 2 | dump.cs:693828 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRectCell.cs:3 |
| `FancyScrollView<TItemData,` | `FancyScrollView<TItemData, TContext>` | 9 | dump.cs:692628 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollView.cs:5 |
| `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | 14 | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| `GameNewsEvent<TPersistence,` | `GameNewsEvent<TPersistence, TData>` | 4 | dump.cs:128851 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/GameNewsEvent.cs:3 |
| `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | 14 | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| `LocalizeTargetDesc_Type<T,` | `LocalizeTargetDesc_Type<T, G>` | 2 | dump.cs:139790 ／ ILSpy Assembly-CSharp/I2/Loc/LocalizeTargetDesc_Type.cs:5 |
| `MissionEvent<TS,` | `MissionEvent<TS, TD>` | 11 | dump.cs:60754 ／ ILSpy Assembly-CSharp/MissionEvent.cs:8 |
| `MultiKeyDictionary<T1,` | `MultiKeyDictionary<T1, T2, T3>` | 1 | dump.cs:120233 ／ ILSpy Assembly-CSharp/MultiKeyDictionary.cs:3 |
| `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | 19 | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| `RankedEventBase<TSave,` | `RankedEventBase<TSave, TData>` | 3 | dump.cs:130887 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/RankedEventBase.cs:6 |
| `ScoringEvent<TS,` | `ScoringEvent<TS, TD>` | 3 | dump.cs:132490 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/ScoringEvent.cs:5 |
| `ShopStore<TSave,` | `ShopStore<TSave, TData>` | 3 | dump.cs:134319 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/ShopStore.cs:6 |
| `ToggleCollectionFilter<T,` | `ToggleCollectionFilter<T, TOption>` | 8 | dump.cs:75518 ／ ILSpy Assembly-CSharp/ToggleCollectionFilter.cs:4 |
| `<>f__AnonymousType1<<Id>j__TPar,` | `<>f__AnonymousType1<<Id>j__TPar, <TotalPoints>j__TPar>` | 3 | dump.cs:210 |
| `<>f__AnonymousType2<<id>j__TPar,` | `<>f__AnonymousType2<<id>j__TPar, <value>j__TPar>` | 3 | dump.cs:297 |
| `<>f__AnonymousType5<<eventId>j__TPar,` | `<>f__AnonymousType5<<eventId>j__TPar, <teamId>j__TPar>` | 3 | dump.cs:530 |
| `<>f__AnonymousType6<<eventID>j__TPar,` | `<>f__AnonymousType6<<eventID>j__TPar, <nodeId>j__TPar, <rewardTier>j__TPar>` | 3 | dump.cs:617 |
| `<>f__AnonymousType8<<Action>j__TPar,` | `<>f__AnonymousType8<<Action>j__TPar, <EventId>j__TPar, <EventState>j__TPar, <ClientTime>j__TPar>` | 3 | dump.cs:791 |
| `<>f__AnonymousType9<<eventId>j__TPar,` | `<>f__AnonymousType9<<eventId>j__TPar, <handler>j__TPar, <active>j__TPar, <localState>j__TPar, <clientTime>j__TPar>` | 3 | dump.cs:906 |
| `<>f__AnonymousType10<<eventId>j__TPar,` | `<>f__AnonymousType10<<eventId>j__TPar, <Handler>j__TPar>` | 3 | dump.cs:1035 |
| `<>f__AnonymousType14<<dlcId>j__TPar,` | `<>f__AnonymousType14<<dlcId>j__TPar, <steamId>j__TPar>` | 3 | dump.cs:1341 |
| `<>f__AnonymousType15<<ItemId>j__TPar,` | `<>f__AnonymousType15<<ItemId>j__TPar, <StoreId>j__TPar, <Expired>j__TPar>` | 3 | dump.cs:1428 |
| `<>f__AnonymousType16<<eventID>j__TPar,` | `<>f__AnonymousType16<<eventID>j__TPar, <nodeId>j__TPar, <starterDeckId>j__TPar>` | 3 | dump.cs:1529 |
| `<>f__AnonymousType17<<eventID>j__TPar,` | `<>f__AnonymousType17<<eventID>j__TPar, <nodeId>j__TPar>` | 3 | dump.cs:1630 |
| `<>f__AnonymousType19<<eventID>j__TPar,` | `<>f__AnonymousType19<<eventID>j__TPar, <cards>j__TPar>` | 3 | dump.cs:1790 |
| `<>f__AnonymousType20<<eventID>j__TPar,` | `<>f__AnonymousType20<<eventID>j__TPar, <opponentInfo>j__TPar>` | 3 | dump.cs:1877 |
| `<>f__AnonymousType21<<eventID>j__TPar,` | `<>f__AnonymousType21<<eventID>j__TPar, <matchWon>j__TPar>` | 3 | dump.cs:1964 |
| `<>f__AnonymousType22<<eventID>j__TPar,` | `<>f__AnonymousType22<<eventID>j__TPar, <pack>j__TPar, <cards>j__TPar>` | 3 | dump.cs:2051 |
| `<>f__AnonymousType24<<eventID>j__TPar,` | `<>f__AnonymousType24<<eventID>j__TPar, <warlord>j__TPar>` | 3 | dump.cs:2225 |
| `<>f__AnonymousType25<<eventID>j__TPar,` | `<>f__AnonymousType25<<eventID>j__TPar, <isTen>j__TPar, <warlords>j__TPar>` | 3 | dump.cs:2312 |
| `<>f__AnonymousType26<<eventID>j__TPar,` | `<>f__AnonymousType26<<eventID>j__TPar, <warlords>j__TPar>` | 3 | dump.cs:2413 |
| `<>f__AnonymousType27<<eventID>j__TPar,` | `<>f__AnonymousType27<<eventID>j__TPar, <opponentInfo>j__TPar, <deckId>j__TPar>` | 3 | dump.cs:2500 |
| `<>f__AnonymousType28<<eventID>j__TPar,` | `<>f__AnonymousType28<<eventID>j__TPar, <matchResult>j__TPar, <opponentId>j__TPar>` | 3 | dump.cs:2601 |
| `<>f__AnonymousType29<<eventId>j__TPar,` | `<>f__AnonymousType29<<eventId>j__TPar, <hasWon>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <ownName>j__TPar, <isSuspicious>j__TPar, <opponentId>j__TPar, <playerHeroId>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | 3 | dump.cs:2702 |
| `<>f__AnonymousType30<<eventID>j__TPar,` | `<>f__AnonymousType30<<eventID>j__TPar, <addPoints>j__TPar>` | 3 | dump.cs:2929 |
| `<>f__AnonymousType32<<leaderboardKey>j__TPar,` | `<>f__AnonymousType32<<leaderboardKey>j__TPar, <overrideValues>j__TPar>` | 3 | dump.cs:3039 |
| `<>f__AnonymousType33<<EventId>j__TPar,` | `<>f__AnonymousType33<<EventId>j__TPar, <ChallengeId>j__TPar>` | 3 | dump.cs:3126 |
| `<>f__AnonymousType34<<EventId>j__TPar,` | `<>f__AnonymousType34<<EventId>j__TPar, <ChallengeId>j__TPar, <NewSave>j__TPar>` | 3 | dump.cs:3213 |
| `<>f__AnonymousType35<<EventId>j__TPar,` | `<>f__AnonymousType35<<EventId>j__TPar, <ChallengeId>j__TPar, <CurrentArmy>j__TPar>` | 3 | dump.cs:3314 |
| `<>f__AnonymousType37<<Action>j__TPar,` | `<>f__AnonymousType37<<Action>j__TPar, <dlcsToRedeem>j__TPar, <dlcsRedeemed>j__TPar>` | 3 | dump.cs:3488 |
| `<>f__AnonymousType38<<eventId>j__TPar,` | `<>f__AnonymousType38<<eventId>j__TPar, <deckId>j__TPar>` | 3 | dump.cs:3589 |
| `<>f__AnonymousType40<<eventID>j__TPar,` | `<>f__AnonymousType40<<eventID>j__TPar, <opponentInfo>j__TPar, <isRanked>j__TPar, <season>j__TPar>` | 3 | dump.cs:3749 |
| `<>f__AnonymousType41<<eventID>j__TPar,` | `<>f__AnonymousType41<<eventID>j__TPar, <matchResult>j__TPar, <isRanked>j__TPar, <opponentId>j__TPar>` | 3 | dump.cs:3864 |
| `<>f__AnonymousType42<<eventId>j__TPar,` | `<>f__AnonymousType42<<eventId>j__TPar, <hasWon>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <ownName>j__TPar, <isRanked>j__TPar, <isSuspicious>j__TPar, <Season>j__TPar, <opponentId>j__TPar, <playerHeroId>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | 3 | dump.cs:3979 |
| `<>f__AnonymousType43<<eventId>j__TPar,` | `<>f__AnonymousType43<<eventId>j__TPar, <milestoneIndex>j__TPar, <isRewardUnlocked>j__TPar>` | 3 | dump.cs:4234 |
| `<>f__AnonymousType44<<eventId>j__TPar,` | `<>f__AnonymousType44<<eventId>j__TPar, <milestoneIndex>j__TPar>` | 3 | dump.cs:4335 |
| `<>f__AnonymousType45<<EventId>j__TPar,` | `<>f__AnonymousType45<<EventId>j__TPar, <RewardIndex>j__TPar, <RewardUnlocked>j__TPar>` | 3 | dump.cs:4422 |
| `<>f__AnonymousType48<<eventID>j__TPar,` | `<>f__AnonymousType48<<eventID>j__TPar, <level>j__TPar>` | 3 | dump.cs:4669 |
| `<>f__AnonymousType49<<ItemId>j__TPar,` | `<>f__AnonymousType49<<ItemId>j__TPar, <InstanceId>j__TPar>` | 3 | dump.cs:4756 |
| `<>f__AnonymousType50<<Action>j__TPar,` | `<>f__AnonymousType50<<Action>j__TPar, <FunctionName>j__TPar, <ApiRequestCount>j__TPar, <HttpRequestCount>j__TPar, <Revision>j__TPar, <Error>j__TPar, <StackTrace>j__TPar, <Logs>j__TPar>` | 3 | dump.cs:4843 |
| `<>f__AnonymousType51<<Action>j__TPar,` | `<>f__AnonymousType51<<Action>j__TPar, <FunctionName>j__TPar, <ApiRequestCount>j__TPar, <HttpRequestCount>j__TPar, <Revision>j__TPar, <Error>j__TPar, <CustomError>j__TPar, <StackTrace>j__TPar, <Logs>j__TPar>` | 3 | dump.cs:5014 |
| `<>f__AnonymousType52<<Action>j__TPar,` | `<>f__AnonymousType52<<Action>j__TPar, <FunctionName>j__TPar, <ApiRequestCount>j__TPar, <HttpRequestCount>j__TPar, <Revision>j__TPar, <CustomError>j__TPar, <Error>j__TPar, <StackTrace>j__TPar, <Logs>j__TPar>` | 3 | dump.cs:5199 |
| `<>f__AnonymousType53<<Action>j__TPar,` | `<>f__AnonymousType53<<Action>j__TPar, <FunctionName>j__TPar, <Error>j__TPar, <HttpCode>j__TPar, <CustomError>j__TPar, <APIEndpoint>j__TPar, <Details>j__TPar>` | 3 | dump.cs:5384 |
| `<>f__AnonymousType54<<SessionId>j__TPar,` | `<>f__AnonymousType54<<SessionId>j__TPar, <Version>j__TPar, <Override>j__TPar, <Data>j__TPar>` | 3 | dump.cs:5541 |
| `<>f__AnonymousType55<<Cards>j__TPar,` | `<>f__AnonymousType55<<Cards>j__TPar, <WarlordId>j__TPar>` | 3 | dump.cs:5656 |
| `<>f__AnonymousType56<<playerId>j__TPar,` | `<>f__AnonymousType56<<playerId>j__TPar, <botIndex>j__TPar>` | 3 | dump.cs:5743 |
| `<>f__AnonymousType58<<hasWon>j__TPar,` | `<>f__AnonymousType58<<hasWon>j__TPar, <matchInfo>j__TPar, <battleResult>j__TPar>` | 3 | dump.cs:5903 |
| `<>f__AnonymousType59<<recordName>j__TPar,` | `<>f__AnonymousType59<<recordName>j__TPar, <battleRecord>j__TPar, <cancelled>j__TPar>` | 3 | dump.cs:6004 |
| `<>f__AnonymousType61<<recordName>j__TPar,` | `<>f__AnonymousType61<<recordName>j__TPar, <targetPlayerId>j__TPar, <enemyName>j__TPar>` | 3 | dump.cs:6178 |
| `<>f__AnonymousType62<<recordIndex>j__TPar,` | `<>f__AnonymousType62<<recordIndex>j__TPar, <pinned>j__TPar>` | 3 | dump.cs:6279 |
| `<>f__AnonymousType63<<eventId>j__TPar,` | `<>f__AnonymousType63<<eventId>j__TPar, <tutorialIndex>j__TPar>` | 3 | dump.cs:6366 |
| `<>f__AnonymousType65<<hasWon>j__TPar,` | `<>f__AnonymousType65<<hasWon>j__TPar, <ownName>j__TPar, <opponentInfo>j__TPar, <isSuspicious>j__TPar, <ReplaysVersion>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <eventID>j__TPar>` | 3 | dump.cs:6526 |
| `<>f__AnonymousType66<<eventId>j__TPar,` | `<>f__AnonymousType66<<eventId>j__TPar, <hasWon>j__TPar, <ownName>j__TPar, <opponentID>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerHeroId>j__TPar, <ReplaysVersion>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | 3 | dump.cs:6697 |
| `<>f__AnonymousType67<<eventId>j__TPar,` | `<>f__AnonymousType67<<eventId>j__TPar, <hasWon>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <ownName>j__TPar, <isRanked>j__TPar, <Season>j__TPar, <opponentId>j__TPar, <playerHeroId>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | 3 | dump.cs:6924 |
| `<>f__AnonymousType68<<opponentId>j__TPar,` | `<>f__AnonymousType68<<opponentId>j__TPar, <errorLog>j__TPar, <gameState>j__TPar>` | 3 | dump.cs:7165 |
| `<>f__AnonymousType69<<opponentId>j__TPar,` | `<>f__AnonymousType69<<opponentId>j__TPar, <gameState>j__TPar>` | 3 | dump.cs:7266 |
| `<>f__AnonymousType71<<sunsetData>j__TPar,` | `<>f__AnonymousType71<<sunsetData>j__TPar, <playerId>j__TPar>` | 3 | dump.cs:7426 |
| `<>f__AnonymousType72<<Handlers>j__TPar,` | `<>f__AnonymousType72<<Handlers>j__TPar, <Configs>j__TPar>` | 3 | dump.cs:7513 |
| `<>f__AnonymousType73<<PlayerPlayfabId>j__TPar,` | `<>f__AnonymousType73<<PlayerPlayfabId>j__TPar, <Deck>j__TPar>` | 3 | dump.cs:7600 |
| `<>f__AnonymousType74<<CharacterId>j__TPar,` | `<>f__AnonymousType74<<CharacterId>j__TPar, <Deck>j__TPar, <DeckId>j__TPar, <GameMode>j__TPar>` | 3 | dump.cs:7687 |
| `<>f__AnonymousType75<<DeckId>j__TPar,` | `<>f__AnonymousType75<<DeckId>j__TPar, <GameMode>j__TPar>` | 3 | dump.cs:7802 |
| `<>f__AnonymousType76<<DeckIds>j__TPar,` | `<>f__AnonymousType76<<DeckIds>j__TPar, <GameMode>j__TPar>` | 3 | dump.cs:7889 |
| `<>f__AnonymousType77<<badge>j__TPar,` | `<>f__AnonymousType77<<badge>j__TPar, <cardback>j__TPar, <warcry>j__TPar>` | 3 | dump.cs:7976 |
| `<>f__AnonymousType78<<CardIds>j__TPar,` | `<>f__AnonymousType78<<CardIds>j__TPar, <bundlesVersion>j__TPar, <ItemInstanceIds>j__TPar>` | 3 | dump.cs:8077 |
| `<>f__AnonymousType82<<StoreId>j__TPar,` | `<>f__AnonymousType82<<StoreId>j__TPar, <ItemId>j__TPar, <SkuIstanceId>j__TPar, <SkuId>j__TPar>` | 3 | dump.cs:8397 |
| `<>f__AnonymousType83<<Action>j__TPar,` | `<>f__AnonymousType83<<Action>j__TPar, <TutorialStage>j__TPar, <Reason>j__TPar, <ClientTime>j__TPar>` | 3 | dump.cs:8512 |
| `<>f__AnonymousType86<<tag>j__TPar,` | `<>f__AnonymousType86<<tag>j__TPar, <value>j__TPar>` | 3 | dump.cs:8773 |
| `<>f__AnonymousType89<<OrderBy>j__TPar,` | `<>f__AnonymousType89<<OrderBy>j__TPar, <StartAt>j__TPar>` | 3 | dump.cs:9006 |
| `<>f__AnonymousType90<<GroupName>j__TPar,` | `<>f__AnonymousType90<<GroupName>j__TPar, <PlayerId>j__TPar>` | 3 | dump.cs:9093 |
| `<>f__AnonymousType91<<GroupName>j__TPar,` | `<>f__AnonymousType91<<GroupName>j__TPar, <Description>j__TPar, <BadgeId>j__TPar, <Region>j__TPar, <Privacy>j__TPar, <MinElo>j__TPar>` | 3 | dump.cs:9180 |
| `<>f__AnonymousType92<<GroupName>j__TPar,` | `<>f__AnonymousType92<<GroupName>j__TPar, <PlayerId>j__TPar, <NewRole>j__TPar>` | 3 | dump.cs:9323 |
| `<>f__AnonymousType93<<GroupId>j__TPar,` | `<>f__AnonymousType93<<GroupId>j__TPar, <GroupData>j__TPar>` | 3 | dump.cs:9424 |
| `<>f__AnonymousType94<<desc>j__TPar,` | `<>f__AnonymousType94<<desc>j__TPar, <privacy>j__TPar, <region>j__TPar, <badgeId>j__TPar, <minElo>j__TPar>` | 3 | dump.cs:9511 |
| `<>f__AnonymousType95<<GroupId>j__TPar,` | `<>f__AnonymousType95<<GroupId>j__TPar, <PlayerId>j__TPar>` | 3 | dump.cs:9640 |
| `<>f__AnonymousType98<<amount>j__TPar,` | `<>f__AnonymousType98<<amount>j__TPar, <playerId>j__TPar, <groupId>j__TPar, <eventId>j__TPar>` | 3 | dump.cs:9873 |
| `<>f__AnonymousType101<<CardID>j__TPar,` | `<>f__AnonymousType101<<CardID>j__TPar, <WildcardID>j__TPar>` | 3 | dump.cs:10134 |
| `<>f__AnonymousType102<<ItemId>j__TPar,` | `<>f__AnonymousType102<<ItemId>j__TPar, <WildcardID>j__TPar>` | 3 | dump.cs:10221 |
| `<>f__AnonymousType103<<ids>j__TPar,` | `<>f__AnonymousType103<<ids>j__TPar, <reset>j__TPar, <clearSharedData>j__TPar>` | 3 | dump.cs:10308 |
| `<>f__AnonymousType104<<eventID>j__TPar,` | `<>f__AnonymousType104<<eventID>j__TPar, <seed>j__TPar, <warlords>j__TPar>` | 3 | dump.cs:10409 |
| `<>f__AnonymousType107<<extraArgs>j__TPar,` | `<>f__AnonymousType107<<extraArgs>j__TPar, <entryCountToFetch>j__TPar>` | 3 | dump.cs:10656 |
| `<>f__AnonymousType108<<Index>j__TPar,` | `<>f__AnonymousType108<<Index>j__TPar, <Value>j__TPar>` | 3 | dump.cs:10743 |

### §二·B 逐行落点（按当前主表行号升序）

| # | 账文件 | 行号 | 方法签名 | 现值 | 正确值 | 判据 |
|---:|---|---:|---|---|---|---|
| 1 | 泛型与启动链_补读.tsv | 2 | `GetArmyState(CardArmy army, IReadOnlyList<RankedScoreBoostEvent> extraBoosts)` | `RankedEventBase<TSave,` | `RankedEventBase<TSave, TData>` | dump.cs:130887 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/RankedEventBase.cs:6 |
| 2 | 泛型与启动链_补读.tsv | 3 | `GetBaseData()` | `RankedEventBase<TSave,` | `RankedEventBase<TSave, TData>` | dump.cs:130887 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/RankedEventBase.cs:6 |
| 3 | 泛型与启动链_补读.tsv | 4 | `ToggleRankedMode(bool isRanked)` | `RankedEventBase<TSave,` | `RankedEventBase<TSave, TData>` | dump.cs:130887 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/RankedEventBase.cs:6 |
| 4 | 泛型与启动链_补读.tsv | 24 | `Equals(object value)` | `<>f__AnonymousType75<<DeckId>j__TPar,` | `<>f__AnonymousType75<<DeckId>j__TPar, <GameMode>j__TPar>` | dump.cs:7802 |
| 5 | 泛型与启动链_补读.tsv | 25 | `GetHashCode()` | `<>f__AnonymousType75<<DeckId>j__TPar,` | `<>f__AnonymousType75<<DeckId>j__TPar, <GameMode>j__TPar>` | dump.cs:7802 |
| 6 | 泛型与启动链_补读.tsv | 26 | `ToString()` | `<>f__AnonymousType75<<DeckId>j__TPar,` | `<>f__AnonymousType75<<DeckId>j__TPar, <GameMode>j__TPar>` | dump.cs:7802 |
| 7 | 泛型与启动链_补读.tsv | 27 | `Equals(object value)` | `<>f__AnonymousType76<<DeckIds>j__TPar,` | `<>f__AnonymousType76<<DeckIds>j__TPar, <GameMode>j__TPar>` | dump.cs:7889 |
| 8 | 泛型与启动链_补读.tsv | 28 | `GetHashCode()` | `<>f__AnonymousType76<<DeckIds>j__TPar,` | `<>f__AnonymousType76<<DeckIds>j__TPar, <GameMode>j__TPar>` | dump.cs:7889 |
| 9 | 泛型与启动链_补读.tsv | 29 | `ToString()` | `<>f__AnonymousType76<<DeckIds>j__TPar,` | `<>f__AnonymousType76<<DeckIds>j__TPar, <GameMode>j__TPar>` | dump.cs:7889 |
| 10 | 泛型与启动链_补读.tsv | 30 | `Equals(object value)` | `<>f__AnonymousType82<<StoreId>j__TPar,` | `<>f__AnonymousType82<<StoreId>j__TPar, <ItemId>j__TPar, <SkuIstanceId>j__TPar, <SkuId>j__TPar>` | dump.cs:8397 |
| 11 | 泛型与启动链_补读.tsv | 31 | `GetHashCode()` | `<>f__AnonymousType82<<StoreId>j__TPar,` | `<>f__AnonymousType82<<StoreId>j__TPar, <ItemId>j__TPar, <SkuIstanceId>j__TPar, <SkuId>j__TPar>` | dump.cs:8397 |
| 12 | 泛型与启动链_补读.tsv | 32 | `ToString()` | `<>f__AnonymousType82<<StoreId>j__TPar,` | `<>f__AnonymousType82<<StoreId>j__TPar, <ItemId>j__TPar, <SkuIstanceId>j__TPar, <SkuId>j__TPar>` | dump.cs:8397 |
| 13 | 泛型与启动链_补读.tsv | 33 | `GetBaseData()` | `ShopStore<TSave,` | `ShopStore<TSave, TData>` | dump.cs:134319 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/ShopStore.cs:6 |
| 14 | 泛型与启动链_补读.tsv | 34 | `TryPurchase(IShopOffer offer, Action onComplete, Action onError)` | `ShopStore<TSave,` | `ShopStore<TSave, TData>` | dump.cs:134319 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/ShopStore.cs:6 |
| 15 | 泛型与启动链_补读.tsv | 35 | `GetPurchaseCount(string id)` | `ShopStore<TSave,` | `ShopStore<TSave, TData>` | dump.cs:134319 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/ShopStore.cs:6 |
| 16 | 泛型与启动链_补读.tsv | 131 | `SetupFilter(CollectionFilterController<T> controller)` | `ToggleCollectionFilter<T,` | `ToggleCollectionFilter<T, TOption>` | dump.cs:75518 ／ ILSpy Assembly-CSharp/ToggleCollectionFilter.cs:4 |
| 17 | 泛型与启动链_补读.tsv | 132 | `OnFilterChange()` | `ToggleCollectionFilter<T,` | `ToggleCollectionFilter<T, TOption>` | dump.cs:75518 ／ ILSpy Assembly-CSharp/ToggleCollectionFilter.cs:4 |
| 18 | 泛型与启动链_补读.tsv | 133 | `Filter(List<T> collection)` | `ToggleCollectionFilter<T,` | `ToggleCollectionFilter<T, TOption>` | dump.cs:75518 ／ ILSpy Assembly-CSharp/ToggleCollectionFilter.cs:4 |
| 19 | 泛型与启动链_补读.tsv | 134 | `FilterItem(T card)` | `ToggleCollectionFilter<T,` | `ToggleCollectionFilter<T, TOption>` | dump.cs:75518 ／ ILSpy Assembly-CSharp/ToggleCollectionFilter.cs:4 |
| 20 | 泛型与启动链_补读.tsv | 135 | `ClearFilters()` | `ToggleCollectionFilter<T,` | `ToggleCollectionFilter<T, TOption>` | dump.cs:75518 ／ ILSpy Assembly-CSharp/ToggleCollectionFilter.cs:4 |
| 21 | 泛型与启动链_补读.tsv | 136 | `FillToggleList(List<CollectionFilterToggle> currentList, int listLimit)` | `ToggleCollectionFilter<T,` | `ToggleCollectionFilter<T, TOption>` | dump.cs:75518 ／ ILSpy Assembly-CSharp/ToggleCollectionFilter.cs:4 |
| 22 | 泛型与启动链_补读.tsv | 137 | `b__5_0(bool b)` | `ToggleCollectionFilter<T,` | `ToggleCollectionFilter<T, TOption>` | dump.cs:75518 ／ ILSpy Assembly-CSharp/ToggleCollectionFilter.cs:4 |
| 23 | 泛型与启动链_补读.tsv | 138 | `b__7_2(T rc)` | `ToggleCollectionFilter<T,` | `ToggleCollectionFilter<T, TOption>` | dump.cs:75518 ／ ILSpy Assembly-CSharp/ToggleCollectionFilter.cs:4 |
| 24 | 泛型与启动链_补读.tsv | 139 | `Equals(object value)` | `<>f__AnonymousType33<<EventId>j__TPar,` | `<>f__AnonymousType33<<EventId>j__TPar, <ChallengeId>j__TPar>` | dump.cs:3126 |
| 25 | 泛型与启动链_补读.tsv | 140 | `GetHashCode()` | `<>f__AnonymousType33<<EventId>j__TPar,` | `<>f__AnonymousType33<<EventId>j__TPar, <ChallengeId>j__TPar>` | dump.cs:3126 |
| 26 | 泛型与启动链_补读.tsv | 141 | `ToString()` | `<>f__AnonymousType33<<EventId>j__TPar,` | `<>f__AnonymousType33<<EventId>j__TPar, <ChallengeId>j__TPar>` | dump.cs:3126 |
| 27 | 泛型与启动链_补读.tsv | 142 | `Equals(object value)` | `<>f__AnonymousType34<<EventId>j__TPar,` | `<>f__AnonymousType34<<EventId>j__TPar, <ChallengeId>j__TPar, <NewSave>j__TPar>` | dump.cs:3213 |
| 28 | 泛型与启动链_补读.tsv | 143 | `GetHashCode()` | `<>f__AnonymousType34<<EventId>j__TPar,` | `<>f__AnonymousType34<<EventId>j__TPar, <ChallengeId>j__TPar, <NewSave>j__TPar>` | dump.cs:3213 |
| 29 | 泛型与启动链_补读.tsv | 144 | `ToString()` | `<>f__AnonymousType34<<EventId>j__TPar,` | `<>f__AnonymousType34<<EventId>j__TPar, <ChallengeId>j__TPar, <NewSave>j__TPar>` | dump.cs:3213 |
| 30 | 泛型与启动链_补读.tsv | 145 | `Equals(object value)` | `<>f__AnonymousType35<<EventId>j__TPar,` | `<>f__AnonymousType35<<EventId>j__TPar, <ChallengeId>j__TPar, <CurrentArmy>j__TPar>` | dump.cs:3314 |
| 31 | 泛型与启动链_补读.tsv | 146 | `GetHashCode()` | `<>f__AnonymousType35<<EventId>j__TPar,` | `<>f__AnonymousType35<<EventId>j__TPar, <ChallengeId>j__TPar, <CurrentArmy>j__TPar>` | dump.cs:3314 |
| 32 | 泛型与启动链_补读.tsv | 147 | `ToString()` | `<>f__AnonymousType35<<EventId>j__TPar,` | `<>f__AnonymousType35<<EventId>j__TPar, <ChallengeId>j__TPar, <CurrentArmy>j__TPar>` | dump.cs:3314 |
| 33 | 泛型与启动链_补读.tsv | 151 | `Equals(object value)` | `<>f__AnonymousType45<<EventId>j__TPar,` | `<>f__AnonymousType45<<EventId>j__TPar, <RewardIndex>j__TPar, <RewardUnlocked>j__TPar>` | dump.cs:4422 |
| 34 | 泛型与启动链_补读.tsv | 152 | `GetHashCode()` | `<>f__AnonymousType45<<EventId>j__TPar,` | `<>f__AnonymousType45<<EventId>j__TPar, <RewardIndex>j__TPar, <RewardUnlocked>j__TPar>` | dump.cs:4422 |
| 35 | 泛型与启动链_补读.tsv | 153 | `ToString()` | `<>f__AnonymousType45<<EventId>j__TPar,` | `<>f__AnonymousType45<<EventId>j__TPar, <RewardIndex>j__TPar, <RewardUnlocked>j__TPar>` | dump.cs:4422 |
| 36 | 泛型与启动链_补读.tsv | 165 | `GetArmyStats(CardArmy army)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 37 | 泛型与启动链_补读.tsv | 166 | `GetArmyState(CardArmy army, IReadOnlyList<RankedScoreBoostEvent> extraBoosts)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 38 | 泛型与启动链_补读.tsv | 167 | `GetEventDeck(CardArmy army)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 39 | 泛型与启动链_补读.tsv | 168 | `GetBaseData()` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 40 | 泛型与启动链_补读.tsv | 169 | `GetEventDeck()` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 41 | 泛型与启动链_补读.tsv | 170 | `SetCustomMatchOptions(RoomOptions options, int currentElo)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 42 | 泛型与启动链_补读.tsv | 171 | `GetSqlFilter(int maxRating, int minRating, string guildId, bool isRanked, IEnumerable<string> blacklistedUsers, int gamesLost)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 43 | 泛型与启动链_补读.tsv | 172 | `GetBaseSave()` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 44 | 泛型与启动链_补读.tsv | 173 | `RecordMatchFinished(int matchWon, int targetMilestone, int enemyMilestone, Action<OnlineMatchResult> callbackOnSuccess)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 45 | 泛型与启动链_补读.tsv | 174 | `OnMatchWon()` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 46 | 泛型与启动链_补读.tsv | 175 | `OnMatchLost()` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 47 | 泛型与启动链_补读.tsv | 176 | `OnMatchDraw()` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 48 | 泛型与启动链_补读.tsv | 177 | `b__12_0(CardDeck d)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 49 | 泛型与启动链_补读.tsv | 178 | `n__0(Action onComplete)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` | dump.cs:57363 ／ ILSpy Assembly-CSharp/FastModeBaseEvent.cs:5 |
| 50 | 泛型与启动链_补读.tsv | 179 | `IsRead()` | `GameNewsEvent<TPersistence,` | `GameNewsEvent<TPersistence, TData>` | dump.cs:128851 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/GameNewsEvent.cs:3 |
| 51 | 泛型与启动链_补读.tsv | 180 | `StartDate()` | `GameNewsEvent<TPersistence,` | `GameNewsEvent<TPersistence, TData>` | dump.cs:128851 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/GameNewsEvent.cs:3 |
| 52 | 泛型与启动链_补读.tsv | 181 | `GetBaseData()` | `GameNewsEvent<TPersistence,` | `GameNewsEvent<TPersistence, TData>` | dump.cs:128851 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/GameNewsEvent.cs:3 |
| 53 | 泛型与启动链_补读.tsv | 182 | `GetBaseSave()` | `GameNewsEvent<TPersistence,` | `GameNewsEvent<TPersistence, TData>` | dump.cs:128851 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/GameNewsEvent.cs:3 |
| 54 | 泛型与启动链_补读.tsv | 183 | `GetID()` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 55 | 泛型与启动链_补读.tsv | 184 | `GetBaseData()` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 56 | 泛型与启动链_补读.tsv | 185 | `GetBaseSave()` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 57 | 泛型与启动链_补读.tsv | 186 | `SelectEvent()` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 58 | 泛型与启动链_补读.tsv | 187 | `Initialize()` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 59 | 泛型与启动链_补读.tsv | 188 | `LoadEventData(LiveOpsEventData data, LiveOpsEventPersistence saveGame, ILiveOpsHandler handler)` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 60 | 泛型与启动链_补读.tsv | 189 | `ValidateEventData(LiveOpsEventData data, LiveOpsEventPersistence save, out TData formatedData, out TSave formatedSave)` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 61 | 泛型与启动链_补读.tsv | 190 | `Validate(TSave save)` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 62 | 泛型与启动链_补读.tsv | 191 | `CreateNewSave(TData data)` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 63 | 泛型与启动链_补读.tsv | 192 | `UpdateData(LiveOpsEventData data)` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 64 | 泛型与启动链_补读.tsv | 193 | `UpdateSaveGame(LiveOpsEventPersistence saveGame)` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 65 | 泛型与启动链_补读.tsv | 194 | `ResetEventProgress()` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 66 | 泛型与启动链_补读.tsv | 195 | `SaveEventProgress(Action onSuccess, Action onFail)` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 67 | 泛型与启动链_补读.tsv | 196 | `GetEndDate(bool useExpiration = False)` | `LiveOpsEvent<TSave,` | `LiveOpsEvent<TSave, TData>` | dump.cs:126282 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/LiveOpsEvent.cs:124 |
| 68 | 泛型与启动链_补读.tsv | 227 | `LocalizedDescription(MissionChallenge challenge)` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` | dump.cs:60754 ／ ILSpy Assembly-CSharp/MissionEvent.cs:8 |
| 69 | 泛型与启动链_补读.tsv | 228 | `GetProgressFor(MissionChallenge challenge)` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` | dump.cs:60754 ／ ILSpy Assembly-CSharp/MissionEvent.cs:8 |
| 70 | 泛型与启动链_补读.tsv | 229 | `IsActive(MissionChallenge challenge)` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` | dump.cs:60754 ／ ILSpy Assembly-CSharp/MissionEvent.cs:8 |
| 71 | 泛型与启动链_补读.tsv | 230 | `DisableChallenge(MissionChallenge challenge)` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` | dump.cs:60754 ／ ILSpy Assembly-CSharp/MissionEvent.cs:8 |
| 72 | 泛型与启动链_补读.tsv | 231 | `GetNextRefreshTime()` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` | dump.cs:60754 ／ ILSpy Assembly-CSharp/MissionEvent.cs:8 |
| 73 | 泛型与启动链_补读.tsv | 232 | `RerollChallenge(MissionChallenge challenge, Action onComplete)` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` | dump.cs:60754 ／ ILSpy Assembly-CSharp/MissionEvent.cs:8 |
| 74 | 泛型与启动链_补读.tsv | 233 | `GetBaseData()` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` | dump.cs:60754 ／ ILSpy Assembly-CSharp/MissionEvent.cs:8 |
| 75 | 泛型与启动链_补读.tsv | 234 | `GetBaseSave()` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` | dump.cs:60754 ／ ILSpy Assembly-CSharp/MissionEvent.cs:8 |
| 76 | 泛型与启动链_补读.tsv | 235 | `UpdateSaveGame(LiveOpsEventPersistence save)` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` | dump.cs:60754 ／ ILSpy Assembly-CSharp/MissionEvent.cs:8 |
| 77 | 泛型与启动链_补读.tsv | 236 | `ReleaseContainer()` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` | dump.cs:60754 ／ ILSpy Assembly-CSharp/MissionEvent.cs:8 |
| 78 | 泛型与启动链_补读.tsv | 237 | `Expire(Action onComplete)` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` | dump.cs:60754 ／ ILSpy Assembly-CSharp/MissionEvent.cs:8 |
| 79 | 泛型与启动链_补读.tsv | 239 | `ChangeState(PlayEventSaveGame.JoinStates stateToChange)` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 80 | 泛型与启动链_补读.tsv | 240 | `GetBaseSave()` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 81 | 泛型与启动链_补读.tsv | 241 | `GetBaseData()` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 82 | 泛型与启动链_补读.tsv | 242 | `Initialize()` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 83 | 泛型与启动链_补读.tsv | 243 | `StartMatch()` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 84 | 泛型与启动链_补读.tsv | 244 | `OnMatchFound(string otherPlayerName)` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 85 | 泛型与启动链_补读.tsv | 245 | `CancelMatch()` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 86 | 泛型与启动链_补读.tsv | 246 | `OnMatchFinished(float matchWon, RankedMatchResult result, Action<OnlineMatchResult> callback)` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 87 | 泛型与启动链_补读.tsv | 247 | `MatchFinished(RankedMatchResult result, Action<OnlineMatchResult> onCompleted)` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 88 | 泛型与启动链_补读.tsv | 248 | `ValidateEventDeck(CardDeck deck, bool validateOwnership = True)` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 89 | 泛型与启动链_补读.tsv | 249 | `OnMatchWon()` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 90 | 泛型与启动链_补读.tsv | 250 | `OnMatchLost()` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 91 | 泛型与启动链_补读.tsv | 251 | `OnMatchDraw()` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 92 | 泛型与启动链_补读.tsv | 252 | `SetCustomMatchOptions(RoomOptions options, int currentElo)` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 93 | 泛型与启动链_补读.tsv | 253 | `GetEventDeck(CardArmy army)` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 94 | 泛型与启动链_补读.tsv | 254 | `SetDefaultDeck(CardDeck deck, bool upload = True)` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 95 | 泛型与启动链_补读.tsv | 255 | `UploadDefaultDeck()` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 96 | 泛型与启动链_补读.tsv | 256 | `b__17_0()` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 97 | 泛型与启动链_补读.tsv | 257 | `b__26_0(CardDeck d)` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` | dump.cs:129759 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/PlayEvent.cs:5 |
| 98 | 泛型与启动链_补读.tsv | 258 | `GetBaseData()` | `ScoringEvent<TS,` | `ScoringEvent<TS, TD>` | dump.cs:132490 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/ScoringEvent.cs:5 |
| 99 | 泛型与启动链_补读.tsv | 259 | `IsMilestoneCollected(ScoringMilestone milestone)` | `ScoringEvent<TS,` | `ScoringEvent<TS, TD>` | dump.cs:132490 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/ScoringEvent.cs:5 |
| 100 | 泛型与启动链_补读.tsv | 260 | `GetState(ScoringMilestone milestone)` | `ScoringEvent<TS,` | `ScoringEvent<TS, TD>` | dump.cs:132490 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/ScoringEvent.cs:5 |
| 101 | 泛型与启动链_补读.tsv | 268 | `Equals(object value)` | `<>f__AnonymousType101<<CardID>j__TPar,` | `<>f__AnonymousType101<<CardID>j__TPar, <WildcardID>j__TPar>` | dump.cs:10134 |
| 102 | 泛型与启动链_补读.tsv | 269 | `GetHashCode()` | `<>f__AnonymousType101<<CardID>j__TPar,` | `<>f__AnonymousType101<<CardID>j__TPar, <WildcardID>j__TPar>` | dump.cs:10134 |
| 103 | 泛型与启动链_补读.tsv | 270 | `ToString()` | `<>f__AnonymousType101<<CardID>j__TPar,` | `<>f__AnonymousType101<<CardID>j__TPar, <WildcardID>j__TPar>` | dump.cs:10134 |
| 104 | 泛型与启动链_补读.tsv | 271 | `Equals(object value)` | `<>f__AnonymousType102<<ItemId>j__TPar,` | `<>f__AnonymousType102<<ItemId>j__TPar, <WildcardID>j__TPar>` | dump.cs:10221 |
| 105 | 泛型与启动链_补读.tsv | 272 | `GetHashCode()` | `<>f__AnonymousType102<<ItemId>j__TPar,` | `<>f__AnonymousType102<<ItemId>j__TPar, <WildcardID>j__TPar>` | dump.cs:10221 |
| 106 | 泛型与启动链_补读.tsv | 273 | `ToString()` | `<>f__AnonymousType102<<ItemId>j__TPar,` | `<>f__AnonymousType102<<ItemId>j__TPar, <WildcardID>j__TPar>` | dump.cs:10221 |
| 107 | 泛型与启动链_补读.tsv | 274 | `Equals(object value)` | `<>f__AnonymousType103<<ids>j__TPar,` | `<>f__AnonymousType103<<ids>j__TPar, <reset>j__TPar, <clearSharedData>j__TPar>` | dump.cs:10308 |
| 108 | 泛型与启动链_补读.tsv | 275 | `GetHashCode()` | `<>f__AnonymousType103<<ids>j__TPar,` | `<>f__AnonymousType103<<ids>j__TPar, <reset>j__TPar, <clearSharedData>j__TPar>` | dump.cs:10308 |
| 109 | 泛型与启动链_补读.tsv | 276 | `ToString()` | `<>f__AnonymousType103<<ids>j__TPar,` | `<>f__AnonymousType103<<ids>j__TPar, <reset>j__TPar, <clearSharedData>j__TPar>` | dump.cs:10308 |
| 110 | 泛型与启动链_补读.tsv | 277 | `Equals(object value)` | `<>f__AnonymousType104<<eventID>j__TPar,` | `<>f__AnonymousType104<<eventID>j__TPar, <seed>j__TPar, <warlords>j__TPar>` | dump.cs:10409 |
| 111 | 泛型与启动链_补读.tsv | 278 | `GetHashCode()` | `<>f__AnonymousType104<<eventID>j__TPar,` | `<>f__AnonymousType104<<eventID>j__TPar, <seed>j__TPar, <warlords>j__TPar>` | dump.cs:10409 |
| 112 | 泛型与启动链_补读.tsv | 279 | `ToString()` | `<>f__AnonymousType104<<eventID>j__TPar,` | `<>f__AnonymousType104<<eventID>j__TPar, <seed>j__TPar, <warlords>j__TPar>` | dump.cs:10409 |
| 113 | 泛型与启动链_补读.tsv | 286 | `Equals(object value)` | `<>f__AnonymousType107<<extraArgs>j__TPar,` | `<>f__AnonymousType107<<extraArgs>j__TPar, <entryCountToFetch>j__TPar>` | dump.cs:10656 |
| 114 | 泛型与启动链_补读.tsv | 287 | `GetHashCode()` | `<>f__AnonymousType107<<extraArgs>j__TPar,` | `<>f__AnonymousType107<<extraArgs>j__TPar, <entryCountToFetch>j__TPar>` | dump.cs:10656 |
| 115 | 泛型与启动链_补读.tsv | 288 | `ToString()` | `<>f__AnonymousType107<<extraArgs>j__TPar,` | `<>f__AnonymousType107<<extraArgs>j__TPar, <entryCountToFetch>j__TPar>` | dump.cs:10656 |
| 116 | 泛型与启动链_补读.tsv | 289 | `Equals(object value)` | `<>f__AnonymousType108<<Index>j__TPar,` | `<>f__AnonymousType108<<Index>j__TPar, <Value>j__TPar>` | dump.cs:10743 |
| 117 | 泛型与启动链_补读.tsv | 290 | `GetHashCode()` | `<>f__AnonymousType108<<Index>j__TPar,` | `<>f__AnonymousType108<<Index>j__TPar, <Value>j__TPar>` | dump.cs:10743 |
| 118 | 泛型与启动链_补读.tsv | 291 | `ToString()` | `<>f__AnonymousType108<<Index>j__TPar,` | `<>f__AnonymousType108<<Index>j__TPar, <Value>j__TPar>` | dump.cs:10743 |
| 119 | 泛型与启动链_补读.tsv | 292 | `Equals(object value)` | `<>f__AnonymousType10<<eventId>j__TPar,` | `<>f__AnonymousType10<<eventId>j__TPar, <Handler>j__TPar>` | dump.cs:1035 |
| 120 | 泛型与启动链_补读.tsv | 293 | `GetHashCode()` | `<>f__AnonymousType10<<eventId>j__TPar,` | `<>f__AnonymousType10<<eventId>j__TPar, <Handler>j__TPar>` | dump.cs:1035 |
| 121 | 泛型与启动链_补读.tsv | 294 | `ToString()` | `<>f__AnonymousType10<<eventId>j__TPar,` | `<>f__AnonymousType10<<eventId>j__TPar, <Handler>j__TPar>` | dump.cs:1035 |
| 122 | 泛型与启动链_补读.tsv | 304 | `Equals(object value)` | `<>f__AnonymousType14<<dlcId>j__TPar,` | `<>f__AnonymousType14<<dlcId>j__TPar, <steamId>j__TPar>` | dump.cs:1341 |
| 123 | 泛型与启动链_补读.tsv | 305 | `GetHashCode()` | `<>f__AnonymousType14<<dlcId>j__TPar,` | `<>f__AnonymousType14<<dlcId>j__TPar, <steamId>j__TPar>` | dump.cs:1341 |
| 124 | 泛型与启动链_补读.tsv | 306 | `ToString()` | `<>f__AnonymousType14<<dlcId>j__TPar,` | `<>f__AnonymousType14<<dlcId>j__TPar, <steamId>j__TPar>` | dump.cs:1341 |
| 125 | 泛型与启动链_补读.tsv | 307 | `Equals(object value)` | `<>f__AnonymousType15<<ItemId>j__TPar,` | `<>f__AnonymousType15<<ItemId>j__TPar, <StoreId>j__TPar, <Expired>j__TPar>` | dump.cs:1428 |
| 126 | 泛型与启动链_补读.tsv | 308 | `GetHashCode()` | `<>f__AnonymousType15<<ItemId>j__TPar,` | `<>f__AnonymousType15<<ItemId>j__TPar, <StoreId>j__TPar, <Expired>j__TPar>` | dump.cs:1428 |
| 127 | 泛型与启动链_补读.tsv | 309 | `ToString()` | `<>f__AnonymousType15<<ItemId>j__TPar,` | `<>f__AnonymousType15<<ItemId>j__TPar, <StoreId>j__TPar, <Expired>j__TPar>` | dump.cs:1428 |
| 128 | 泛型与启动链_补读.tsv | 310 | `Equals(object value)` | `<>f__AnonymousType16<<eventID>j__TPar,` | `<>f__AnonymousType16<<eventID>j__TPar, <nodeId>j__TPar, <starterDeckId>j__TPar>` | dump.cs:1529 |
| 129 | 泛型与启动链_补读.tsv | 311 | `GetHashCode()` | `<>f__AnonymousType16<<eventID>j__TPar,` | `<>f__AnonymousType16<<eventID>j__TPar, <nodeId>j__TPar, <starterDeckId>j__TPar>` | dump.cs:1529 |
| 130 | 泛型与启动链_补读.tsv | 312 | `ToString()` | `<>f__AnonymousType16<<eventID>j__TPar,` | `<>f__AnonymousType16<<eventID>j__TPar, <nodeId>j__TPar, <starterDeckId>j__TPar>` | dump.cs:1529 |
| 131 | 泛型与启动链_补读.tsv | 313 | `Equals(object value)` | `<>f__AnonymousType17<<eventID>j__TPar,` | `<>f__AnonymousType17<<eventID>j__TPar, <nodeId>j__TPar>` | dump.cs:1630 |
| 132 | 泛型与启动链_补读.tsv | 314 | `GetHashCode()` | `<>f__AnonymousType17<<eventID>j__TPar,` | `<>f__AnonymousType17<<eventID>j__TPar, <nodeId>j__TPar>` | dump.cs:1630 |
| 133 | 泛型与启动链_补读.tsv | 315 | `ToString()` | `<>f__AnonymousType17<<eventID>j__TPar,` | `<>f__AnonymousType17<<eventID>j__TPar, <nodeId>j__TPar>` | dump.cs:1630 |
| 134 | 泛型与启动链_补读.tsv | 319 | `Equals(object value)` | `<>f__AnonymousType19<<eventID>j__TPar,` | `<>f__AnonymousType19<<eventID>j__TPar, <cards>j__TPar>` | dump.cs:1790 |
| 135 | 泛型与启动链_补读.tsv | 320 | `GetHashCode()` | `<>f__AnonymousType19<<eventID>j__TPar,` | `<>f__AnonymousType19<<eventID>j__TPar, <cards>j__TPar>` | dump.cs:1790 |
| 136 | 泛型与启动链_补读.tsv | 321 | `ToString()` | `<>f__AnonymousType19<<eventID>j__TPar,` | `<>f__AnonymousType19<<eventID>j__TPar, <cards>j__TPar>` | dump.cs:1790 |
| 137 | 泛型与启动链_补读.tsv | 322 | `Equals(object value)` | `<>f__AnonymousType1<<Id>j__TPar,` | `<>f__AnonymousType1<<Id>j__TPar, <TotalPoints>j__TPar>` | dump.cs:210 |
| 138 | 泛型与启动链_补读.tsv | 323 | `GetHashCode()` | `<>f__AnonymousType1<<Id>j__TPar,` | `<>f__AnonymousType1<<Id>j__TPar, <TotalPoints>j__TPar>` | dump.cs:210 |
| 139 | 泛型与启动链_补读.tsv | 324 | `ToString()` | `<>f__AnonymousType1<<Id>j__TPar,` | `<>f__AnonymousType1<<Id>j__TPar, <TotalPoints>j__TPar>` | dump.cs:210 |
| 140 | 泛型与启动链_补读.tsv | 325 | `Equals(object value)` | `<>f__AnonymousType20<<eventID>j__TPar,` | `<>f__AnonymousType20<<eventID>j__TPar, <opponentInfo>j__TPar>` | dump.cs:1877 |
| 141 | 泛型与启动链_补读.tsv | 326 | `GetHashCode()` | `<>f__AnonymousType20<<eventID>j__TPar,` | `<>f__AnonymousType20<<eventID>j__TPar, <opponentInfo>j__TPar>` | dump.cs:1877 |
| 142 | 泛型与启动链_补读.tsv | 327 | `ToString()` | `<>f__AnonymousType20<<eventID>j__TPar,` | `<>f__AnonymousType20<<eventID>j__TPar, <opponentInfo>j__TPar>` | dump.cs:1877 |
| 143 | 泛型与启动链_补读.tsv | 328 | `Equals(object value)` | `<>f__AnonymousType21<<eventID>j__TPar,` | `<>f__AnonymousType21<<eventID>j__TPar, <matchWon>j__TPar>` | dump.cs:1964 |
| 144 | 泛型与启动链_补读.tsv | 329 | `GetHashCode()` | `<>f__AnonymousType21<<eventID>j__TPar,` | `<>f__AnonymousType21<<eventID>j__TPar, <matchWon>j__TPar>` | dump.cs:1964 |
| 145 | 泛型与启动链_补读.tsv | 330 | `ToString()` | `<>f__AnonymousType21<<eventID>j__TPar,` | `<>f__AnonymousType21<<eventID>j__TPar, <matchWon>j__TPar>` | dump.cs:1964 |
| 146 | 泛型与启动链_补读.tsv | 331 | `Equals(object value)` | `<>f__AnonymousType22<<eventID>j__TPar,` | `<>f__AnonymousType22<<eventID>j__TPar, <pack>j__TPar, <cards>j__TPar>` | dump.cs:2051 |
| 147 | 泛型与启动链_补读.tsv | 332 | `GetHashCode()` | `<>f__AnonymousType22<<eventID>j__TPar,` | `<>f__AnonymousType22<<eventID>j__TPar, <pack>j__TPar, <cards>j__TPar>` | dump.cs:2051 |
| 148 | 泛型与启动链_补读.tsv | 333 | `ToString()` | `<>f__AnonymousType22<<eventID>j__TPar,` | `<>f__AnonymousType22<<eventID>j__TPar, <pack>j__TPar, <cards>j__TPar>` | dump.cs:2051 |
| 149 | 泛型与启动链_补读.tsv | 337 | `Equals(object value)` | `<>f__AnonymousType24<<eventID>j__TPar,` | `<>f__AnonymousType24<<eventID>j__TPar, <warlord>j__TPar>` | dump.cs:2225 |
| 150 | 泛型与启动链_补读.tsv | 338 | `GetHashCode()` | `<>f__AnonymousType24<<eventID>j__TPar,` | `<>f__AnonymousType24<<eventID>j__TPar, <warlord>j__TPar>` | dump.cs:2225 |
| 151 | 泛型与启动链_补读.tsv | 339 | `ToString()` | `<>f__AnonymousType24<<eventID>j__TPar,` | `<>f__AnonymousType24<<eventID>j__TPar, <warlord>j__TPar>` | dump.cs:2225 |
| 152 | 泛型与启动链_补读.tsv | 340 | `Equals(object value)` | `<>f__AnonymousType25<<eventID>j__TPar,` | `<>f__AnonymousType25<<eventID>j__TPar, <isTen>j__TPar, <warlords>j__TPar>` | dump.cs:2312 |
| 153 | 泛型与启动链_补读.tsv | 341 | `GetHashCode()` | `<>f__AnonymousType25<<eventID>j__TPar,` | `<>f__AnonymousType25<<eventID>j__TPar, <isTen>j__TPar, <warlords>j__TPar>` | dump.cs:2312 |
| 154 | 泛型与启动链_补读.tsv | 342 | `ToString()` | `<>f__AnonymousType25<<eventID>j__TPar,` | `<>f__AnonymousType25<<eventID>j__TPar, <isTen>j__TPar, <warlords>j__TPar>` | dump.cs:2312 |
| 155 | 泛型与启动链_补读.tsv | 343 | `Equals(object value)` | `<>f__AnonymousType26<<eventID>j__TPar,` | `<>f__AnonymousType26<<eventID>j__TPar, <warlords>j__TPar>` | dump.cs:2413 |
| 156 | 泛型与启动链_补读.tsv | 344 | `GetHashCode()` | `<>f__AnonymousType26<<eventID>j__TPar,` | `<>f__AnonymousType26<<eventID>j__TPar, <warlords>j__TPar>` | dump.cs:2413 |
| 157 | 泛型与启动链_补读.tsv | 345 | `ToString()` | `<>f__AnonymousType26<<eventID>j__TPar,` | `<>f__AnonymousType26<<eventID>j__TPar, <warlords>j__TPar>` | dump.cs:2413 |
| 158 | 泛型与启动链_补读.tsv | 346 | `Equals(object value)` | `<>f__AnonymousType27<<eventID>j__TPar,` | `<>f__AnonymousType27<<eventID>j__TPar, <opponentInfo>j__TPar, <deckId>j__TPar>` | dump.cs:2500 |
| 159 | 泛型与启动链_补读.tsv | 347 | `GetHashCode()` | `<>f__AnonymousType27<<eventID>j__TPar,` | `<>f__AnonymousType27<<eventID>j__TPar, <opponentInfo>j__TPar, <deckId>j__TPar>` | dump.cs:2500 |
| 160 | 泛型与启动链_补读.tsv | 348 | `ToString()` | `<>f__AnonymousType27<<eventID>j__TPar,` | `<>f__AnonymousType27<<eventID>j__TPar, <opponentInfo>j__TPar, <deckId>j__TPar>` | dump.cs:2500 |
| 161 | 泛型与启动链_补读.tsv | 349 | `Equals(object value)` | `<>f__AnonymousType28<<eventID>j__TPar,` | `<>f__AnonymousType28<<eventID>j__TPar, <matchResult>j__TPar, <opponentId>j__TPar>` | dump.cs:2601 |
| 162 | 泛型与启动链_补读.tsv | 350 | `GetHashCode()` | `<>f__AnonymousType28<<eventID>j__TPar,` | `<>f__AnonymousType28<<eventID>j__TPar, <matchResult>j__TPar, <opponentId>j__TPar>` | dump.cs:2601 |
| 163 | 泛型与启动链_补读.tsv | 351 | `ToString()` | `<>f__AnonymousType28<<eventID>j__TPar,` | `<>f__AnonymousType28<<eventID>j__TPar, <matchResult>j__TPar, <opponentId>j__TPar>` | dump.cs:2601 |
| 164 | 泛型与启动链_补读.tsv | 352 | `Equals(object value)` | `<>f__AnonymousType29<<eventId>j__TPar,` | `<>f__AnonymousType29<<eventId>j__TPar, <hasWon>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <ownName>j__TPar, <isSuspicious>j__TPar, <opponentId>j__TPar, <playerHeroId>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | dump.cs:2702 |
| 165 | 泛型与启动链_补读.tsv | 353 | `GetHashCode()` | `<>f__AnonymousType29<<eventId>j__TPar,` | `<>f__AnonymousType29<<eventId>j__TPar, <hasWon>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <ownName>j__TPar, <isSuspicious>j__TPar, <opponentId>j__TPar, <playerHeroId>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | dump.cs:2702 |
| 166 | 泛型与启动链_补读.tsv | 354 | `ToString()` | `<>f__AnonymousType29<<eventId>j__TPar,` | `<>f__AnonymousType29<<eventId>j__TPar, <hasWon>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <ownName>j__TPar, <isSuspicious>j__TPar, <opponentId>j__TPar, <playerHeroId>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | dump.cs:2702 |
| 167 | 泛型与启动链_补读.tsv | 355 | `Equals(object value)` | `<>f__AnonymousType2<<id>j__TPar,` | `<>f__AnonymousType2<<id>j__TPar, <value>j__TPar>` | dump.cs:297 |
| 168 | 泛型与启动链_补读.tsv | 356 | `GetHashCode()` | `<>f__AnonymousType2<<id>j__TPar,` | `<>f__AnonymousType2<<id>j__TPar, <value>j__TPar>` | dump.cs:297 |
| 169 | 泛型与启动链_补读.tsv | 357 | `ToString()` | `<>f__AnonymousType2<<id>j__TPar,` | `<>f__AnonymousType2<<id>j__TPar, <value>j__TPar>` | dump.cs:297 |
| 170 | 泛型与启动链_补读.tsv | 358 | `Equals(object value)` | `<>f__AnonymousType30<<eventID>j__TPar,` | `<>f__AnonymousType30<<eventID>j__TPar, <addPoints>j__TPar>` | dump.cs:2929 |
| 171 | 泛型与启动链_补读.tsv | 359 | `GetHashCode()` | `<>f__AnonymousType30<<eventID>j__TPar,` | `<>f__AnonymousType30<<eventID>j__TPar, <addPoints>j__TPar>` | dump.cs:2929 |
| 172 | 泛型与启动链_补读.tsv | 360 | `ToString()` | `<>f__AnonymousType30<<eventID>j__TPar,` | `<>f__AnonymousType30<<eventID>j__TPar, <addPoints>j__TPar>` | dump.cs:2929 |
| 173 | 泛型与启动链_补读.tsv | 364 | `Equals(object value)` | `<>f__AnonymousType32<<leaderboardKey>j__TPar,` | `<>f__AnonymousType32<<leaderboardKey>j__TPar, <overrideValues>j__TPar>` | dump.cs:3039 |
| 174 | 泛型与启动链_补读.tsv | 365 | `GetHashCode()` | `<>f__AnonymousType32<<leaderboardKey>j__TPar,` | `<>f__AnonymousType32<<leaderboardKey>j__TPar, <overrideValues>j__TPar>` | dump.cs:3039 |
| 175 | 泛型与启动链_补读.tsv | 366 | `ToString()` | `<>f__AnonymousType32<<leaderboardKey>j__TPar,` | `<>f__AnonymousType32<<leaderboardKey>j__TPar, <overrideValues>j__TPar>` | dump.cs:3039 |
| 176 | 泛型与启动链_补读.tsv | 367 | `Equals(object value)` | `<>f__AnonymousType37<<Action>j__TPar,` | `<>f__AnonymousType37<<Action>j__TPar, <dlcsToRedeem>j__TPar, <dlcsRedeemed>j__TPar>` | dump.cs:3488 |
| 177 | 泛型与启动链_补读.tsv | 368 | `GetHashCode()` | `<>f__AnonymousType37<<Action>j__TPar,` | `<>f__AnonymousType37<<Action>j__TPar, <dlcsToRedeem>j__TPar, <dlcsRedeemed>j__TPar>` | dump.cs:3488 |
| 178 | 泛型与启动链_补读.tsv | 369 | `ToString()` | `<>f__AnonymousType37<<Action>j__TPar,` | `<>f__AnonymousType37<<Action>j__TPar, <dlcsToRedeem>j__TPar, <dlcsRedeemed>j__TPar>` | dump.cs:3488 |
| 179 | 泛型与启动链_补读.tsv | 370 | `Equals(object value)` | `<>f__AnonymousType38<<eventId>j__TPar,` | `<>f__AnonymousType38<<eventId>j__TPar, <deckId>j__TPar>` | dump.cs:3589 |
| 180 | 泛型与启动链_补读.tsv | 371 | `GetHashCode()` | `<>f__AnonymousType38<<eventId>j__TPar,` | `<>f__AnonymousType38<<eventId>j__TPar, <deckId>j__TPar>` | dump.cs:3589 |
| 181 | 泛型与启动链_补读.tsv | 372 | `ToString()` | `<>f__AnonymousType38<<eventId>j__TPar,` | `<>f__AnonymousType38<<eventId>j__TPar, <deckId>j__TPar>` | dump.cs:3589 |
| 182 | 泛型与启动链_补读.tsv | 379 | `Equals(object value)` | `<>f__AnonymousType40<<eventID>j__TPar,` | `<>f__AnonymousType40<<eventID>j__TPar, <opponentInfo>j__TPar, <isRanked>j__TPar, <season>j__TPar>` | dump.cs:3749 |
| 183 | 泛型与启动链_补读.tsv | 380 | `GetHashCode()` | `<>f__AnonymousType40<<eventID>j__TPar,` | `<>f__AnonymousType40<<eventID>j__TPar, <opponentInfo>j__TPar, <isRanked>j__TPar, <season>j__TPar>` | dump.cs:3749 |
| 184 | 泛型与启动链_补读.tsv | 381 | `ToString()` | `<>f__AnonymousType40<<eventID>j__TPar,` | `<>f__AnonymousType40<<eventID>j__TPar, <opponentInfo>j__TPar, <isRanked>j__TPar, <season>j__TPar>` | dump.cs:3749 |
| 185 | 泛型与启动链_补读.tsv | 382 | `Equals(object value)` | `<>f__AnonymousType41<<eventID>j__TPar,` | `<>f__AnonymousType41<<eventID>j__TPar, <matchResult>j__TPar, <isRanked>j__TPar, <opponentId>j__TPar>` | dump.cs:3864 |
| 186 | 泛型与启动链_补读.tsv | 383 | `GetHashCode()` | `<>f__AnonymousType41<<eventID>j__TPar,` | `<>f__AnonymousType41<<eventID>j__TPar, <matchResult>j__TPar, <isRanked>j__TPar, <opponentId>j__TPar>` | dump.cs:3864 |
| 187 | 泛型与启动链_补读.tsv | 384 | `ToString()` | `<>f__AnonymousType41<<eventID>j__TPar,` | `<>f__AnonymousType41<<eventID>j__TPar, <matchResult>j__TPar, <isRanked>j__TPar, <opponentId>j__TPar>` | dump.cs:3864 |
| 188 | 泛型与启动链_补读.tsv | 385 | `Equals(object value)` | `<>f__AnonymousType42<<eventId>j__TPar,` | `<>f__AnonymousType42<<eventId>j__TPar, <hasWon>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <ownName>j__TPar, <isRanked>j__TPar, <isSuspicious>j__TPar, <Season>j__TPar, <opponentId>j__TPar, <playerHeroId>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | dump.cs:3979 |
| 189 | 泛型与启动链_补读.tsv | 386 | `GetHashCode()` | `<>f__AnonymousType42<<eventId>j__TPar,` | `<>f__AnonymousType42<<eventId>j__TPar, <hasWon>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <ownName>j__TPar, <isRanked>j__TPar, <isSuspicious>j__TPar, <Season>j__TPar, <opponentId>j__TPar, <playerHeroId>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | dump.cs:3979 |
| 190 | 泛型与启动链_补读.tsv | 387 | `ToString()` | `<>f__AnonymousType42<<eventId>j__TPar,` | `<>f__AnonymousType42<<eventId>j__TPar, <hasWon>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <ownName>j__TPar, <isRanked>j__TPar, <isSuspicious>j__TPar, <Season>j__TPar, <opponentId>j__TPar, <playerHeroId>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | dump.cs:3979 |
| 191 | 泛型与启动链_补读.tsv | 388 | `Equals(object value)` | `<>f__AnonymousType43<<eventId>j__TPar,` | `<>f__AnonymousType43<<eventId>j__TPar, <milestoneIndex>j__TPar, <isRewardUnlocked>j__TPar>` | dump.cs:4234 |
| 192 | 泛型与启动链_补读.tsv | 389 | `GetHashCode()` | `<>f__AnonymousType43<<eventId>j__TPar,` | `<>f__AnonymousType43<<eventId>j__TPar, <milestoneIndex>j__TPar, <isRewardUnlocked>j__TPar>` | dump.cs:4234 |
| 193 | 泛型与启动链_补读.tsv | 390 | `ToString()` | `<>f__AnonymousType43<<eventId>j__TPar,` | `<>f__AnonymousType43<<eventId>j__TPar, <milestoneIndex>j__TPar, <isRewardUnlocked>j__TPar>` | dump.cs:4234 |
| 194 | 泛型与启动链_补读.tsv | 391 | `Equals(object value)` | `<>f__AnonymousType44<<eventId>j__TPar,` | `<>f__AnonymousType44<<eventId>j__TPar, <milestoneIndex>j__TPar>` | dump.cs:4335 |
| 195 | 泛型与启动链_补读.tsv | 392 | `GetHashCode()` | `<>f__AnonymousType44<<eventId>j__TPar,` | `<>f__AnonymousType44<<eventId>j__TPar, <milestoneIndex>j__TPar>` | dump.cs:4335 |
| 196 | 泛型与启动链_补读.tsv | 393 | `ToString()` | `<>f__AnonymousType44<<eventId>j__TPar,` | `<>f__AnonymousType44<<eventId>j__TPar, <milestoneIndex>j__TPar>` | dump.cs:4335 |
| 197 | 泛型与启动链_补读.tsv | 400 | `Equals(object value)` | `<>f__AnonymousType48<<eventID>j__TPar,` | `<>f__AnonymousType48<<eventID>j__TPar, <level>j__TPar>` | dump.cs:4669 |
| 198 | 泛型与启动链_补读.tsv | 401 | `GetHashCode()` | `<>f__AnonymousType48<<eventID>j__TPar,` | `<>f__AnonymousType48<<eventID>j__TPar, <level>j__TPar>` | dump.cs:4669 |
| 199 | 泛型与启动链_补读.tsv | 402 | `ToString()` | `<>f__AnonymousType48<<eventID>j__TPar,` | `<>f__AnonymousType48<<eventID>j__TPar, <level>j__TPar>` | dump.cs:4669 |
| 200 | 泛型与启动链_补读.tsv | 403 | `Equals(object value)` | `<>f__AnonymousType49<<ItemId>j__TPar,` | `<>f__AnonymousType49<<ItemId>j__TPar, <InstanceId>j__TPar>` | dump.cs:4756 |
| 201 | 泛型与启动链_补读.tsv | 404 | `GetHashCode()` | `<>f__AnonymousType49<<ItemId>j__TPar,` | `<>f__AnonymousType49<<ItemId>j__TPar, <InstanceId>j__TPar>` | dump.cs:4756 |
| 202 | 泛型与启动链_补读.tsv | 405 | `ToString()` | `<>f__AnonymousType49<<ItemId>j__TPar,` | `<>f__AnonymousType49<<ItemId>j__TPar, <InstanceId>j__TPar>` | dump.cs:4756 |
| 203 | 泛型与启动链_补读.tsv | 406 | `Equals(object value)` | `<>f__AnonymousType50<<Action>j__TPar,` | `<>f__AnonymousType50<<Action>j__TPar, <FunctionName>j__TPar, <ApiRequestCount>j__TPar, <HttpRequestCount>j__TPar, <Revision>j__TPar, <Error>j__TPar, <StackTrace>j__TPar, <Logs>j__TPar>` | dump.cs:4843 |
| 204 | 泛型与启动链_补读.tsv | 407 | `GetHashCode()` | `<>f__AnonymousType50<<Action>j__TPar,` | `<>f__AnonymousType50<<Action>j__TPar, <FunctionName>j__TPar, <ApiRequestCount>j__TPar, <HttpRequestCount>j__TPar, <Revision>j__TPar, <Error>j__TPar, <StackTrace>j__TPar, <Logs>j__TPar>` | dump.cs:4843 |
| 205 | 泛型与启动链_补读.tsv | 408 | `ToString()` | `<>f__AnonymousType50<<Action>j__TPar,` | `<>f__AnonymousType50<<Action>j__TPar, <FunctionName>j__TPar, <ApiRequestCount>j__TPar, <HttpRequestCount>j__TPar, <Revision>j__TPar, <Error>j__TPar, <StackTrace>j__TPar, <Logs>j__TPar>` | dump.cs:4843 |
| 206 | 泛型与启动链_补读.tsv | 409 | `Equals(object value)` | `<>f__AnonymousType51<<Action>j__TPar,` | `<>f__AnonymousType51<<Action>j__TPar, <FunctionName>j__TPar, <ApiRequestCount>j__TPar, <HttpRequestCount>j__TPar, <Revision>j__TPar, <Error>j__TPar, <CustomError>j__TPar, <StackTrace>j__TPar, <Logs>j__TPar>` | dump.cs:5014 |
| 207 | 泛型与启动链_补读.tsv | 410 | `GetHashCode()` | `<>f__AnonymousType51<<Action>j__TPar,` | `<>f__AnonymousType51<<Action>j__TPar, <FunctionName>j__TPar, <ApiRequestCount>j__TPar, <HttpRequestCount>j__TPar, <Revision>j__TPar, <Error>j__TPar, <CustomError>j__TPar, <StackTrace>j__TPar, <Logs>j__TPar>` | dump.cs:5014 |
| 208 | 泛型与启动链_补读.tsv | 411 | `ToString()` | `<>f__AnonymousType51<<Action>j__TPar,` | `<>f__AnonymousType51<<Action>j__TPar, <FunctionName>j__TPar, <ApiRequestCount>j__TPar, <HttpRequestCount>j__TPar, <Revision>j__TPar, <Error>j__TPar, <CustomError>j__TPar, <StackTrace>j__TPar, <Logs>j__TPar>` | dump.cs:5014 |
| 209 | 泛型与启动链_补读.tsv | 412 | `Equals(object value)` | `<>f__AnonymousType52<<Action>j__TPar,` | `<>f__AnonymousType52<<Action>j__TPar, <FunctionName>j__TPar, <ApiRequestCount>j__TPar, <HttpRequestCount>j__TPar, <Revision>j__TPar, <CustomError>j__TPar, <Error>j__TPar, <StackTrace>j__TPar, <Logs>j__TPar>` | dump.cs:5199 |
| 210 | 泛型与启动链_补读.tsv | 413 | `GetHashCode()` | `<>f__AnonymousType52<<Action>j__TPar,` | `<>f__AnonymousType52<<Action>j__TPar, <FunctionName>j__TPar, <ApiRequestCount>j__TPar, <HttpRequestCount>j__TPar, <Revision>j__TPar, <CustomError>j__TPar, <Error>j__TPar, <StackTrace>j__TPar, <Logs>j__TPar>` | dump.cs:5199 |
| 211 | 泛型与启动链_补读.tsv | 414 | `ToString()` | `<>f__AnonymousType52<<Action>j__TPar,` | `<>f__AnonymousType52<<Action>j__TPar, <FunctionName>j__TPar, <ApiRequestCount>j__TPar, <HttpRequestCount>j__TPar, <Revision>j__TPar, <CustomError>j__TPar, <Error>j__TPar, <StackTrace>j__TPar, <Logs>j__TPar>` | dump.cs:5199 |
| 212 | 泛型与启动链_补读.tsv | 415 | `Equals(object value)` | `<>f__AnonymousType53<<Action>j__TPar,` | `<>f__AnonymousType53<<Action>j__TPar, <FunctionName>j__TPar, <Error>j__TPar, <HttpCode>j__TPar, <CustomError>j__TPar, <APIEndpoint>j__TPar, <Details>j__TPar>` | dump.cs:5384 |
| 213 | 泛型与启动链_补读.tsv | 416 | `GetHashCode()` | `<>f__AnonymousType53<<Action>j__TPar,` | `<>f__AnonymousType53<<Action>j__TPar, <FunctionName>j__TPar, <Error>j__TPar, <HttpCode>j__TPar, <CustomError>j__TPar, <APIEndpoint>j__TPar, <Details>j__TPar>` | dump.cs:5384 |
| 214 | 泛型与启动链_补读.tsv | 417 | `ToString()` | `<>f__AnonymousType53<<Action>j__TPar,` | `<>f__AnonymousType53<<Action>j__TPar, <FunctionName>j__TPar, <Error>j__TPar, <HttpCode>j__TPar, <CustomError>j__TPar, <APIEndpoint>j__TPar, <Details>j__TPar>` | dump.cs:5384 |
| 215 | 泛型与启动链_补读.tsv | 418 | `Equals(object value)` | `<>f__AnonymousType54<<SessionId>j__TPar,` | `<>f__AnonymousType54<<SessionId>j__TPar, <Version>j__TPar, <Override>j__TPar, <Data>j__TPar>` | dump.cs:5541 |
| 216 | 泛型与启动链_补读.tsv | 419 | `GetHashCode()` | `<>f__AnonymousType54<<SessionId>j__TPar,` | `<>f__AnonymousType54<<SessionId>j__TPar, <Version>j__TPar, <Override>j__TPar, <Data>j__TPar>` | dump.cs:5541 |
| 217 | 泛型与启动链_补读.tsv | 420 | `ToString()` | `<>f__AnonymousType54<<SessionId>j__TPar,` | `<>f__AnonymousType54<<SessionId>j__TPar, <Version>j__TPar, <Override>j__TPar, <Data>j__TPar>` | dump.cs:5541 |
| 218 | 泛型与启动链_补读.tsv | 421 | `Equals(object value)` | `<>f__AnonymousType55<<Cards>j__TPar,` | `<>f__AnonymousType55<<Cards>j__TPar, <WarlordId>j__TPar>` | dump.cs:5656 |
| 219 | 泛型与启动链_补读.tsv | 422 | `GetHashCode()` | `<>f__AnonymousType55<<Cards>j__TPar,` | `<>f__AnonymousType55<<Cards>j__TPar, <WarlordId>j__TPar>` | dump.cs:5656 |
| 220 | 泛型与启动链_补读.tsv | 423 | `ToString()` | `<>f__AnonymousType55<<Cards>j__TPar,` | `<>f__AnonymousType55<<Cards>j__TPar, <WarlordId>j__TPar>` | dump.cs:5656 |
| 221 | 泛型与启动链_补读.tsv | 424 | `Equals(object value)` | `<>f__AnonymousType56<<playerId>j__TPar,` | `<>f__AnonymousType56<<playerId>j__TPar, <botIndex>j__TPar>` | dump.cs:5743 |
| 222 | 泛型与启动链_补读.tsv | 425 | `GetHashCode()` | `<>f__AnonymousType56<<playerId>j__TPar,` | `<>f__AnonymousType56<<playerId>j__TPar, <botIndex>j__TPar>` | dump.cs:5743 |
| 223 | 泛型与启动链_补读.tsv | 426 | `ToString()` | `<>f__AnonymousType56<<playerId>j__TPar,` | `<>f__AnonymousType56<<playerId>j__TPar, <botIndex>j__TPar>` | dump.cs:5743 |
| 224 | 泛型与启动链_补读.tsv | 430 | `Equals(object value)` | `<>f__AnonymousType58<<hasWon>j__TPar,` | `<>f__AnonymousType58<<hasWon>j__TPar, <matchInfo>j__TPar, <battleResult>j__TPar>` | dump.cs:5903 |
| 225 | 泛型与启动链_补读.tsv | 431 | `GetHashCode()` | `<>f__AnonymousType58<<hasWon>j__TPar,` | `<>f__AnonymousType58<<hasWon>j__TPar, <matchInfo>j__TPar, <battleResult>j__TPar>` | dump.cs:5903 |
| 226 | 泛型与启动链_补读.tsv | 432 | `ToString()` | `<>f__AnonymousType58<<hasWon>j__TPar,` | `<>f__AnonymousType58<<hasWon>j__TPar, <matchInfo>j__TPar, <battleResult>j__TPar>` | dump.cs:5903 |
| 227 | 泛型与启动链_补读.tsv | 433 | `Equals(object value)` | `<>f__AnonymousType59<<recordName>j__TPar,` | `<>f__AnonymousType59<<recordName>j__TPar, <battleRecord>j__TPar, <cancelled>j__TPar>` | dump.cs:6004 |
| 228 | 泛型与启动链_补读.tsv | 434 | `GetHashCode()` | `<>f__AnonymousType59<<recordName>j__TPar,` | `<>f__AnonymousType59<<recordName>j__TPar, <battleRecord>j__TPar, <cancelled>j__TPar>` | dump.cs:6004 |
| 229 | 泛型与启动链_补读.tsv | 435 | `ToString()` | `<>f__AnonymousType59<<recordName>j__TPar,` | `<>f__AnonymousType59<<recordName>j__TPar, <battleRecord>j__TPar, <cancelled>j__TPar>` | dump.cs:6004 |
| 230 | 泛型与启动链_补读.tsv | 436 | `Equals(object value)` | `<>f__AnonymousType5<<eventId>j__TPar,` | `<>f__AnonymousType5<<eventId>j__TPar, <teamId>j__TPar>` | dump.cs:530 |
| 231 | 泛型与启动链_补读.tsv | 437 | `GetHashCode()` | `<>f__AnonymousType5<<eventId>j__TPar,` | `<>f__AnonymousType5<<eventId>j__TPar, <teamId>j__TPar>` | dump.cs:530 |
| 232 | 泛型与启动链_补读.tsv | 438 | `ToString()` | `<>f__AnonymousType5<<eventId>j__TPar,` | `<>f__AnonymousType5<<eventId>j__TPar, <teamId>j__TPar>` | dump.cs:530 |
| 233 | 泛型与启动链_补读.tsv | 442 | `Equals(object value)` | `<>f__AnonymousType61<<recordName>j__TPar,` | `<>f__AnonymousType61<<recordName>j__TPar, <targetPlayerId>j__TPar, <enemyName>j__TPar>` | dump.cs:6178 |
| 234 | 泛型与启动链_补读.tsv | 443 | `GetHashCode()` | `<>f__AnonymousType61<<recordName>j__TPar,` | `<>f__AnonymousType61<<recordName>j__TPar, <targetPlayerId>j__TPar, <enemyName>j__TPar>` | dump.cs:6178 |
| 235 | 泛型与启动链_补读.tsv | 444 | `ToString()` | `<>f__AnonymousType61<<recordName>j__TPar,` | `<>f__AnonymousType61<<recordName>j__TPar, <targetPlayerId>j__TPar, <enemyName>j__TPar>` | dump.cs:6178 |
| 236 | 泛型与启动链_补读.tsv | 445 | `Equals(object value)` | `<>f__AnonymousType62<<recordIndex>j__TPar,` | `<>f__AnonymousType62<<recordIndex>j__TPar, <pinned>j__TPar>` | dump.cs:6279 |
| 237 | 泛型与启动链_补读.tsv | 446 | `GetHashCode()` | `<>f__AnonymousType62<<recordIndex>j__TPar,` | `<>f__AnonymousType62<<recordIndex>j__TPar, <pinned>j__TPar>` | dump.cs:6279 |
| 238 | 泛型与启动链_补读.tsv | 447 | `ToString()` | `<>f__AnonymousType62<<recordIndex>j__TPar,` | `<>f__AnonymousType62<<recordIndex>j__TPar, <pinned>j__TPar>` | dump.cs:6279 |
| 239 | 泛型与启动链_补读.tsv | 448 | `Equals(object value)` | `<>f__AnonymousType63<<eventId>j__TPar,` | `<>f__AnonymousType63<<eventId>j__TPar, <tutorialIndex>j__TPar>` | dump.cs:6366 |
| 240 | 泛型与启动链_补读.tsv | 449 | `GetHashCode()` | `<>f__AnonymousType63<<eventId>j__TPar,` | `<>f__AnonymousType63<<eventId>j__TPar, <tutorialIndex>j__TPar>` | dump.cs:6366 |
| 241 | 泛型与启动链_补读.tsv | 450 | `ToString()` | `<>f__AnonymousType63<<eventId>j__TPar,` | `<>f__AnonymousType63<<eventId>j__TPar, <tutorialIndex>j__TPar>` | dump.cs:6366 |
| 242 | 泛型与启动链_补读.tsv | 451 | `Equals(object value)` | `<>f__AnonymousType65<<hasWon>j__TPar,` | `<>f__AnonymousType65<<hasWon>j__TPar, <ownName>j__TPar, <opponentInfo>j__TPar, <isSuspicious>j__TPar, <ReplaysVersion>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <eventID>j__TPar>` | dump.cs:6526 |
| 243 | 泛型与启动链_补读.tsv | 452 | `GetHashCode()` | `<>f__AnonymousType65<<hasWon>j__TPar,` | `<>f__AnonymousType65<<hasWon>j__TPar, <ownName>j__TPar, <opponentInfo>j__TPar, <isSuspicious>j__TPar, <ReplaysVersion>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <eventID>j__TPar>` | dump.cs:6526 |
| 244 | 泛型与启动链_补读.tsv | 453 | `ToString()` | `<>f__AnonymousType65<<hasWon>j__TPar,` | `<>f__AnonymousType65<<hasWon>j__TPar, <ownName>j__TPar, <opponentInfo>j__TPar, <isSuspicious>j__TPar, <ReplaysVersion>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <eventID>j__TPar>` | dump.cs:6526 |
| 245 | 泛型与启动链_补读.tsv | 454 | `Equals(object value)` | `<>f__AnonymousType66<<eventId>j__TPar,` | `<>f__AnonymousType66<<eventId>j__TPar, <hasWon>j__TPar, <ownName>j__TPar, <opponentID>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerHeroId>j__TPar, <ReplaysVersion>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | dump.cs:6697 |
| 246 | 泛型与启动链_补读.tsv | 455 | `GetHashCode()` | `<>f__AnonymousType66<<eventId>j__TPar,` | `<>f__AnonymousType66<<eventId>j__TPar, <hasWon>j__TPar, <ownName>j__TPar, <opponentID>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerHeroId>j__TPar, <ReplaysVersion>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | dump.cs:6697 |
| 247 | 泛型与启动链_补读.tsv | 456 | `ToString()` | `<>f__AnonymousType66<<eventId>j__TPar,` | `<>f__AnonymousType66<<eventId>j__TPar, <hasWon>j__TPar, <ownName>j__TPar, <opponentID>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerHeroId>j__TPar, <ReplaysVersion>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | dump.cs:6697 |
| 248 | 泛型与启动链_补读.tsv | 457 | `Equals(object value)` | `<>f__AnonymousType67<<eventId>j__TPar,` | `<>f__AnonymousType67<<eventId>j__TPar, <hasWon>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <ownName>j__TPar, <isRanked>j__TPar, <Season>j__TPar, <opponentId>j__TPar, <playerHeroId>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | dump.cs:6924 |
| 249 | 泛型与启动链_补读.tsv | 458 | `GetHashCode()` | `<>f__AnonymousType67<<eventId>j__TPar,` | `<>f__AnonymousType67<<eventId>j__TPar, <hasWon>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <ownName>j__TPar, <isRanked>j__TPar, <Season>j__TPar, <opponentId>j__TPar, <playerHeroId>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | dump.cs:6924 |
| 250 | 泛型与启动链_补读.tsv | 459 | `ToString()` | `<>f__AnonymousType67<<eventId>j__TPar,` | `<>f__AnonymousType67<<eventId>j__TPar, <hasWon>j__TPar, <milestone>j__TPar, <enemymilestone>j__TPar, <ownName>j__TPar, <isRanked>j__TPar, <Season>j__TPar, <opponentId>j__TPar, <playerHeroId>j__TPar, <opponentName>j__TPar, <opponentHeroId>j__TPar, <playerClan>j__TPar, <enemyClan>j__TPar>` | dump.cs:6924 |
| 251 | 泛型与启动链_补读.tsv | 460 | `Equals(object value)` | `<>f__AnonymousType68<<opponentId>j__TPar,` | `<>f__AnonymousType68<<opponentId>j__TPar, <errorLog>j__TPar, <gameState>j__TPar>` | dump.cs:7165 |
| 252 | 泛型与启动链_补读.tsv | 461 | `GetHashCode()` | `<>f__AnonymousType68<<opponentId>j__TPar,` | `<>f__AnonymousType68<<opponentId>j__TPar, <errorLog>j__TPar, <gameState>j__TPar>` | dump.cs:7165 |
| 253 | 泛型与启动链_补读.tsv | 462 | `ToString()` | `<>f__AnonymousType68<<opponentId>j__TPar,` | `<>f__AnonymousType68<<opponentId>j__TPar, <errorLog>j__TPar, <gameState>j__TPar>` | dump.cs:7165 |
| 254 | 泛型与启动链_补读.tsv | 463 | `Equals(object value)` | `<>f__AnonymousType69<<opponentId>j__TPar,` | `<>f__AnonymousType69<<opponentId>j__TPar, <gameState>j__TPar>` | dump.cs:7266 |
| 255 | 泛型与启动链_补读.tsv | 464 | `GetHashCode()` | `<>f__AnonymousType69<<opponentId>j__TPar,` | `<>f__AnonymousType69<<opponentId>j__TPar, <gameState>j__TPar>` | dump.cs:7266 |
| 256 | 泛型与启动链_补读.tsv | 465 | `ToString()` | `<>f__AnonymousType69<<opponentId>j__TPar,` | `<>f__AnonymousType69<<opponentId>j__TPar, <gameState>j__TPar>` | dump.cs:7266 |
| 257 | 泛型与启动链_补读.tsv | 466 | `Equals(object value)` | `<>f__AnonymousType6<<eventID>j__TPar,` | `<>f__AnonymousType6<<eventID>j__TPar, <nodeId>j__TPar, <rewardTier>j__TPar>` | dump.cs:617 |
| 258 | 泛型与启动链_补读.tsv | 467 | `GetHashCode()` | `<>f__AnonymousType6<<eventID>j__TPar,` | `<>f__AnonymousType6<<eventID>j__TPar, <nodeId>j__TPar, <rewardTier>j__TPar>` | dump.cs:617 |
| 259 | 泛型与启动链_补读.tsv | 468 | `ToString()` | `<>f__AnonymousType6<<eventID>j__TPar,` | `<>f__AnonymousType6<<eventID>j__TPar, <nodeId>j__TPar, <rewardTier>j__TPar>` | dump.cs:617 |
| 260 | 泛型与启动链_补读.tsv | 472 | `Equals(object value)` | `<>f__AnonymousType71<<sunsetData>j__TPar,` | `<>f__AnonymousType71<<sunsetData>j__TPar, <playerId>j__TPar>` | dump.cs:7426 |
| 261 | 泛型与启动链_补读.tsv | 473 | `GetHashCode()` | `<>f__AnonymousType71<<sunsetData>j__TPar,` | `<>f__AnonymousType71<<sunsetData>j__TPar, <playerId>j__TPar>` | dump.cs:7426 |
| 262 | 泛型与启动链_补读.tsv | 474 | `ToString()` | `<>f__AnonymousType71<<sunsetData>j__TPar,` | `<>f__AnonymousType71<<sunsetData>j__TPar, <playerId>j__TPar>` | dump.cs:7426 |
| 263 | 泛型与启动链_补读.tsv | 475 | `Equals(object value)` | `<>f__AnonymousType72<<Handlers>j__TPar,` | `<>f__AnonymousType72<<Handlers>j__TPar, <Configs>j__TPar>` | dump.cs:7513 |
| 264 | 泛型与启动链_补读.tsv | 476 | `GetHashCode()` | `<>f__AnonymousType72<<Handlers>j__TPar,` | `<>f__AnonymousType72<<Handlers>j__TPar, <Configs>j__TPar>` | dump.cs:7513 |
| 265 | 泛型与启动链_补读.tsv | 477 | `ToString()` | `<>f__AnonymousType72<<Handlers>j__TPar,` | `<>f__AnonymousType72<<Handlers>j__TPar, <Configs>j__TPar>` | dump.cs:7513 |
| 266 | 泛型与启动链_补读.tsv | 478 | `Equals(object value)` | `<>f__AnonymousType73<<PlayerPlayfabId>j__TPar,` | `<>f__AnonymousType73<<PlayerPlayfabId>j__TPar, <Deck>j__TPar>` | dump.cs:7600 |
| 267 | 泛型与启动链_补读.tsv | 479 | `GetHashCode()` | `<>f__AnonymousType73<<PlayerPlayfabId>j__TPar,` | `<>f__AnonymousType73<<PlayerPlayfabId>j__TPar, <Deck>j__TPar>` | dump.cs:7600 |
| 268 | 泛型与启动链_补读.tsv | 480 | `ToString()` | `<>f__AnonymousType73<<PlayerPlayfabId>j__TPar,` | `<>f__AnonymousType73<<PlayerPlayfabId>j__TPar, <Deck>j__TPar>` | dump.cs:7600 |
| 269 | 泛型与启动链_补读.tsv | 481 | `Equals(object value)` | `<>f__AnonymousType74<<CharacterId>j__TPar,` | `<>f__AnonymousType74<<CharacterId>j__TPar, <Deck>j__TPar, <DeckId>j__TPar, <GameMode>j__TPar>` | dump.cs:7687 |
| 270 | 泛型与启动链_补读.tsv | 482 | `GetHashCode()` | `<>f__AnonymousType74<<CharacterId>j__TPar,` | `<>f__AnonymousType74<<CharacterId>j__TPar, <Deck>j__TPar, <DeckId>j__TPar, <GameMode>j__TPar>` | dump.cs:7687 |
| 271 | 泛型与启动链_补读.tsv | 483 | `ToString()` | `<>f__AnonymousType74<<CharacterId>j__TPar,` | `<>f__AnonymousType74<<CharacterId>j__TPar, <Deck>j__TPar, <DeckId>j__TPar, <GameMode>j__TPar>` | dump.cs:7687 |
| 272 | 泛型与启动链_补读.tsv | 484 | `Equals(object value)` | `<>f__AnonymousType77<<badge>j__TPar,` | `<>f__AnonymousType77<<badge>j__TPar, <cardback>j__TPar, <warcry>j__TPar>` | dump.cs:7976 |
| 273 | 泛型与启动链_补读.tsv | 485 | `GetHashCode()` | `<>f__AnonymousType77<<badge>j__TPar,` | `<>f__AnonymousType77<<badge>j__TPar, <cardback>j__TPar, <warcry>j__TPar>` | dump.cs:7976 |
| 274 | 泛型与启动链_补读.tsv | 486 | `ToString()` | `<>f__AnonymousType77<<badge>j__TPar,` | `<>f__AnonymousType77<<badge>j__TPar, <cardback>j__TPar, <warcry>j__TPar>` | dump.cs:7976 |
| 275 | 泛型与启动链_补读.tsv | 487 | `Equals(object value)` | `<>f__AnonymousType78<<CardIds>j__TPar,` | `<>f__AnonymousType78<<CardIds>j__TPar, <bundlesVersion>j__TPar, <ItemInstanceIds>j__TPar>` | dump.cs:8077 |
| 276 | 泛型与启动链_补读.tsv | 488 | `GetHashCode()` | `<>f__AnonymousType78<<CardIds>j__TPar,` | `<>f__AnonymousType78<<CardIds>j__TPar, <bundlesVersion>j__TPar, <ItemInstanceIds>j__TPar>` | dump.cs:8077 |
| 277 | 泛型与启动链_补读.tsv | 489 | `ToString()` | `<>f__AnonymousType78<<CardIds>j__TPar,` | `<>f__AnonymousType78<<CardIds>j__TPar, <bundlesVersion>j__TPar, <ItemInstanceIds>j__TPar>` | dump.cs:8077 |
| 278 | 泛型与启动链_补读.tsv | 502 | `Equals(object value)` | `<>f__AnonymousType83<<Action>j__TPar,` | `<>f__AnonymousType83<<Action>j__TPar, <TutorialStage>j__TPar, <Reason>j__TPar, <ClientTime>j__TPar>` | dump.cs:8512 |
| 279 | 泛型与启动链_补读.tsv | 503 | `GetHashCode()` | `<>f__AnonymousType83<<Action>j__TPar,` | `<>f__AnonymousType83<<Action>j__TPar, <TutorialStage>j__TPar, <Reason>j__TPar, <ClientTime>j__TPar>` | dump.cs:8512 |
| 280 | 泛型与启动链_补读.tsv | 504 | `ToString()` | `<>f__AnonymousType83<<Action>j__TPar,` | `<>f__AnonymousType83<<Action>j__TPar, <TutorialStage>j__TPar, <Reason>j__TPar, <ClientTime>j__TPar>` | dump.cs:8512 |
| 281 | 泛型与启动链_补读.tsv | 508 | `Equals(object value)` | `<>f__AnonymousType86<<tag>j__TPar,` | `<>f__AnonymousType86<<tag>j__TPar, <value>j__TPar>` | dump.cs:8773 |
| 282 | 泛型与启动链_补读.tsv | 509 | `GetHashCode()` | `<>f__AnonymousType86<<tag>j__TPar,` | `<>f__AnonymousType86<<tag>j__TPar, <value>j__TPar>` | dump.cs:8773 |
| 283 | 泛型与启动链_补读.tsv | 510 | `ToString()` | `<>f__AnonymousType86<<tag>j__TPar,` | `<>f__AnonymousType86<<tag>j__TPar, <value>j__TPar>` | dump.cs:8773 |
| 284 | 泛型与启动链_补读.tsv | 511 | `Equals(object value)` | `<>f__AnonymousType89<<OrderBy>j__TPar,` | `<>f__AnonymousType89<<OrderBy>j__TPar, <StartAt>j__TPar>` | dump.cs:9006 |
| 285 | 泛型与启动链_补读.tsv | 512 | `GetHashCode()` | `<>f__AnonymousType89<<OrderBy>j__TPar,` | `<>f__AnonymousType89<<OrderBy>j__TPar, <StartAt>j__TPar>` | dump.cs:9006 |
| 286 | 泛型与启动链_补读.tsv | 513 | `ToString()` | `<>f__AnonymousType89<<OrderBy>j__TPar,` | `<>f__AnonymousType89<<OrderBy>j__TPar, <StartAt>j__TPar>` | dump.cs:9006 |
| 287 | 泛型与启动链_补读.tsv | 514 | `Equals(object value)` | `<>f__AnonymousType8<<Action>j__TPar,` | `<>f__AnonymousType8<<Action>j__TPar, <EventId>j__TPar, <EventState>j__TPar, <ClientTime>j__TPar>` | dump.cs:791 |
| 288 | 泛型与启动链_补读.tsv | 515 | `GetHashCode()` | `<>f__AnonymousType8<<Action>j__TPar,` | `<>f__AnonymousType8<<Action>j__TPar, <EventId>j__TPar, <EventState>j__TPar, <ClientTime>j__TPar>` | dump.cs:791 |
| 289 | 泛型与启动链_补读.tsv | 516 | `ToString()` | `<>f__AnonymousType8<<Action>j__TPar,` | `<>f__AnonymousType8<<Action>j__TPar, <EventId>j__TPar, <EventState>j__TPar, <ClientTime>j__TPar>` | dump.cs:791 |
| 290 | 泛型与启动链_补读.tsv | 517 | `Equals(object value)` | `<>f__AnonymousType94<<desc>j__TPar,` | `<>f__AnonymousType94<<desc>j__TPar, <privacy>j__TPar, <region>j__TPar, <badgeId>j__TPar, <minElo>j__TPar>` | dump.cs:9511 |
| 291 | 泛型与启动链_补读.tsv | 518 | `GetHashCode()` | `<>f__AnonymousType94<<desc>j__TPar,` | `<>f__AnonymousType94<<desc>j__TPar, <privacy>j__TPar, <region>j__TPar, <badgeId>j__TPar, <minElo>j__TPar>` | dump.cs:9511 |
| 292 | 泛型与启动链_补读.tsv | 519 | `ToString()` | `<>f__AnonymousType94<<desc>j__TPar,` | `<>f__AnonymousType94<<desc>j__TPar, <privacy>j__TPar, <region>j__TPar, <badgeId>j__TPar, <minElo>j__TPar>` | dump.cs:9511 |
| 293 | 泛型与启动链_补读.tsv | 523 | `Equals(object value)` | `<>f__AnonymousType98<<amount>j__TPar,` | `<>f__AnonymousType98<<amount>j__TPar, <playerId>j__TPar, <groupId>j__TPar, <eventId>j__TPar>` | dump.cs:9873 |
| 294 | 泛型与启动链_补读.tsv | 524 | `GetHashCode()` | `<>f__AnonymousType98<<amount>j__TPar,` | `<>f__AnonymousType98<<amount>j__TPar, <playerId>j__TPar, <groupId>j__TPar, <eventId>j__TPar>` | dump.cs:9873 |
| 295 | 泛型与启动链_补读.tsv | 525 | `ToString()` | `<>f__AnonymousType98<<amount>j__TPar,` | `<>f__AnonymousType98<<amount>j__TPar, <playerId>j__TPar, <groupId>j__TPar, <eventId>j__TPar>` | dump.cs:9873 |
| 296 | 泛型与启动链_补读.tsv | 529 | `Equals(object value)` | `<>f__AnonymousType9<<eventId>j__TPar,` | `<>f__AnonymousType9<<eventId>j__TPar, <handler>j__TPar, <active>j__TPar, <localState>j__TPar, <clientTime>j__TPar>` | dump.cs:906 |
| 297 | 泛型与启动链_补读.tsv | 530 | `GetHashCode()` | `<>f__AnonymousType9<<eventId>j__TPar,` | `<>f__AnonymousType9<<eventId>j__TPar, <handler>j__TPar, <active>j__TPar, <localState>j__TPar, <clientTime>j__TPar>` | dump.cs:906 |
| 298 | 泛型与启动链_补读.tsv | 531 | `ToString()` | `<>f__AnonymousType9<<eventId>j__TPar,` | `<>f__AnonymousType9<<eventId>j__TPar, <handler>j__TPar, <active>j__TPar, <localState>j__TPar, <clientTime>j__TPar>` | dump.cs:906 |
| 299 | 泛型与启动链_补读.tsv | 551 | `TryGetKey(TValue value, out TKey key)` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 300 | 泛型与启动链_补读.tsv | 552 | `GetKey(TValue value)` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 301 | 泛型与启动链_补读.tsv | 553 | `GetValue(TKey key)` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 302 | 泛型与启动链_补读.tsv | 554 | `Add(TKey key, TValue value)` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 303 | 泛型与启动链_补读.tsv | 555 | `Add(KeyValuePair<TKey, TValue> item)` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 304 | 泛型与启动链_补读.tsv | 556 | `Clear()` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 305 | 泛型与启动链_补读.tsv | 557 | `Contains(KeyValuePair<TKey, TValue> item)` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 306 | 泛型与启动链_补读.tsv | 558 | `ContainsKey(TKey key)` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 307 | 泛型与启动链_补读.tsv | 559 | `ContainsValue(TValue value)` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 308 | 泛型与启动链_补读.tsv | 560 | `CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 309 | 泛型与启动链_补读.tsv | 561 | `GetEnumerator()` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 310 | 泛型与启动链_补读.tsv | 562 | `Remove(TKey key)` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 311 | 泛型与启动链_补读.tsv | 563 | `Remove(KeyValuePair<TKey, TValue> item)` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 312 | 泛型与启动链_补读.tsv | 564 | `TryGetValue(TKey key, out TValue value)` | `BiDirectionalDictionary<TKey,` | `BiDirectionalDictionary<TKey, TValue>` | dump.cs:98846 ／ ILSpy Assembly-CSharp/BiDirectionalDictionary.cs:7 |
| 313 | 泛型与启动链_补读.tsv | 600 | `ContainsKey(T1 key1, T2 key2)` | `MultiKeyDictionary<T1,` | `MultiKeyDictionary<T1, T2, T3>` | dump.cs:120233 ／ ILSpy Assembly-CSharp/MultiKeyDictionary.cs:3 |
| 314 | 泛型与启动链_补读.tsv | 618 | `GetTitle()` | `BaseInboxEvent<TSave,` | `BaseInboxEvent<TSave, TData>` | dump.cs:129192 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/BaseInboxEvent.cs:5 |
| 315 | 泛型与启动链_补读.tsv | 619 | `GetMessage()` | `BaseInboxEvent<TSave,` | `BaseInboxEvent<TSave, TData>` | dump.cs:129192 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/BaseInboxEvent.cs:5 |
| 316 | 泛型与启动链_补读.tsv | 620 | `GetMessageContent()` | `BaseInboxEvent<TSave,` | `BaseInboxEvent<TSave, TData>` | dump.cs:129192 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/BaseInboxEvent.cs:5 |
| 317 | 泛型与启动链_补读.tsv | 621 | `RedeemableAt()` | `BaseInboxEvent<TSave,` | `BaseInboxEvent<TSave, TData>` | dump.cs:129192 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/BaseInboxEvent.cs:5 |
| 318 | 泛型与启动链_补读.tsv | 622 | `GetRewards()` | `BaseInboxEvent<TSave,` | `BaseInboxEvent<TSave, TData>` | dump.cs:129192 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/BaseInboxEvent.cs:5 |
| 319 | 泛型与启动链_补读.tsv | 623 | `IsRead()` | `BaseInboxEvent<TSave,` | `BaseInboxEvent<TSave, TData>` | dump.cs:129192 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/BaseInboxEvent.cs:5 |
| 320 | 泛型与启动链_补读.tsv | 624 | `StartDate()` | `BaseInboxEvent<TSave,` | `BaseInboxEvent<TSave, TData>` | dump.cs:129192 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/BaseInboxEvent.cs:5 |
| 321 | 泛型与启动链_补读.tsv | 625 | `7_0(CollectedRewardsResult rewards)` | `BaseInboxEvent<TSave,` | `BaseInboxEvent<TSave, TData>` | dump.cs:129192 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/BaseInboxEvent.cs:5 |
| 322 | 泛型与启动链_补读.tsv | 626 | `SetContext(TContext context)` | `FancyCell<TItemData,` | `FancyCell<TItemData, TContext>` | dump.cs:692497 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyCell.cs:3 |
| 323 | 泛型与启动链_补读.tsv | 627 | `Initialize()` | `FancyCell<TItemData,` | `FancyCell<TItemData, TContext>` | dump.cs:692497 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyCell.cs:3 |
| 324 | 泛型与启动链_补读.tsv | 628 | `SetVisible(bool visible)` | `FancyCell<TItemData,` | `FancyCell<TItemData, TContext>` | dump.cs:692497 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyCell.cs:3 |
| 325 | 泛型与启动链_补读.tsv | 629 | `UpdateContent(TItemData itemData)` | `FancyCell<TItemData,` | `FancyCell<TItemData, TContext>` | dump.cs:692497 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyCell.cs:3 |
| 326 | 泛型与启动链_补读.tsv | 630 | `UpdatePosition(float position)` | `FancyCell<TItemData,` | `FancyCell<TItemData, TContext>` | dump.cs:692497 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyCell.cs:3 |
| 327 | 泛型与启动链_补读.tsv | 632 | `InstantiateCells()` | `FancyCellGroup<TItemData,` | `FancyCellGroup<TItemData, TContext>` | dump.cs:692832 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyCellGroup.cs:3 |
| 328 | 泛型与启动链_补读.tsv | 633 | `Initialize()` | `FancyCellGroup<TItemData,` | `FancyCellGroup<TItemData, TContext>` | dump.cs:692832 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyCellGroup.cs:3 |
| 329 | 泛型与启动链_补读.tsv | 634 | `UpdateContent(TItemData[] contents)` | `FancyCellGroup<TItemData,` | `FancyCellGroup<TItemData, TContext>` | dump.cs:692832 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyCellGroup.cs:3 |
| 330 | 泛型与启动链_补读.tsv | 635 | `UpdatePosition(float position)` | `FancyCellGroup<TItemData,` | `FancyCellGroup<TItemData, TContext>` | dump.cs:692832 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyCellGroup.cs:3 |
| 331 | 泛型与启动链_补读.tsv | 636 | `b__4_0(int _)` | `FancyCellGroup<TItemData,` | `FancyCellGroup<TItemData, TContext>` | dump.cs:692832 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyCellGroup.cs:3 |
| 332 | 泛型与启动链_补读.tsv | 637 | `Initialize()` | `FancyGridView<TItemData,` | `FancyGridView<TItemData, TContext>` | dump.cs:692982 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridView.cs:7 |
| 333 | 泛型与启动链_补读.tsv | 638 | `SetupCellTemplate()` | `FancyGridView<TItemData,` | `FancyGridView<TItemData, TContext>` | dump.cs:692982 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridView.cs:7 |
| 334 | 泛型与启动链_补读.tsv | 639 | `Setup(FancyCell<TItemData, TContext> cellTemplate)` | `FancyGridView<TItemData,` | `FancyGridView<TItemData, TContext>` | dump.cs:692982 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridView.cs:7 |
| 335 | 泛型与启动链_补读.tsv | 640 | `UpdateContents(IList<TItemData> items)` | `FancyGridView<TItemData,` | `FancyGridView<TItemData, TContext>` | dump.cs:692982 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridView.cs:7 |
| 336 | 泛型与启动链_补读.tsv | 641 | `JumpTo(int itemIndex, float alignment = 0.5)` | `FancyGridView<TItemData,` | `FancyGridView<TItemData, TContext>` | dump.cs:692982 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridView.cs:7 |
| 337 | 泛型与启动链_补读.tsv | 642 | `ScrollTo(int itemIndex, float duration, float alignment = 0.5, Action onComplete)` | `FancyGridView<TItemData,` | `FancyGridView<TItemData, TContext>` | dump.cs:692982 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridView.cs:7 |
| 338 | 泛型与启动链_补读.tsv | 643 | `ScrollTo(int itemIndex, float duration, Ease easing, float alignment = 0.5, Action onComplete)` | `FancyGridView<TItemData,` | `FancyGridView<TItemData, TContext>` | dump.cs:692982 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridView.cs:7 |
| 339 | 泛型与启动链_补读.tsv | 644 | `b__13_0()` | `FancyGridView<TItemData,` | `FancyGridView<TItemData, TContext>` | dump.cs:692982 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridView.cs:7 |
| 340 | 泛型与启动链_补读.tsv | 645 | `b__13_1()` | `FancyGridView<TItemData,` | `FancyGridView<TItemData, TContext>` | dump.cs:692982 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridView.cs:7 |
| 341 | 泛型与启动链_补读.tsv | 646 | `b__13_2()` | `FancyGridView<TItemData,` | `FancyGridView<TItemData, TContext>` | dump.cs:692982 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridView.cs:7 |
| 342 | 泛型与启动链_补读.tsv | 647 | `b__16_1(ValueTuple<TItemData, int> x)` | `FancyGridView<TItemData,` | `FancyGridView<TItemData, TContext>` | dump.cs:692982 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridView.cs:7 |
| 343 | 泛型与启动链_补读.tsv | 648 | `UpdatePosition(float normalizedPosition, float localPosition)` | `FancyGridViewCell<TItemData,` | `FancyGridViewCell<TItemData, TContext>` | dump.cs:693152 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyGridViewCell.cs:3 |
| 344 | 泛型与启动链_补读.tsv | 650 | `Initialize()` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 345 | 泛型与启动链_补读.tsv | 651 | `OnScrollerValueChanged(float p)` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 346 | 泛型与启动链_补读.tsv | 652 | `ShrinkScrollbar(float offset)` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 347 | 泛型与启动链_补读.tsv | 653 | `Refresh()` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 348 | 泛型与启动链_补读.tsv | 654 | `Relayout()` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 349 | 泛型与启动链_补读.tsv | 655 | `RefreshScroller()` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 350 | 泛型与启动链_补读.tsv | 656 | `UpdateContents(IList<TItemData> items)` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 351 | 泛型与启动链_补读.tsv | 657 | `UpdatePosition(float position)` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 352 | 泛型与启动链_补读.tsv | 658 | `JumpTo(int itemIndex, float alignment = 0.5)` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 353 | 泛型与启动链_补读.tsv | 659 | `ScrollTo(int index, float duration, float alignment = 0.5, Action onComplete)` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 354 | 泛型与启动链_补读.tsv | 660 | `ScrollTo(int index, float duration, Ease easing, float alignment = 0.5, Action onComplete)` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 355 | 泛型与启动链_补读.tsv | 661 | `UpdateScrollbarSize(float viewportLength)` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 356 | 泛型与启动链_补读.tsv | 662 | `ToFancyScrollViewPosition(float position)` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 357 | 泛型与启动链_补读.tsv | 663 | `ToScrollerPosition(float position)` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 358 | 泛型与启动链_补读.tsv | 664 | `ToScrollerPosition(float position, float alignment = 0.5)` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 359 | 泛型与启动链_补读.tsv | 665 | `AdjustCellIntervalAndScrollOffset()` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 360 | 泛型与启动链_补读.tsv | 666 | `OnValidate()` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 361 | 泛型与启动链_补读.tsv | 667 | `b__19_0()` | `FancyScrollRect<TItemData,` | `FancyScrollRect<TItemData, TContext>` | dump.cs:693579 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRect.cs:8 |
| 362 | 泛型与启动链_补读.tsv | 668 | `UpdatePosition(float position)` | `FancyScrollRectCell<TItemData,` | `FancyScrollRectCell<TItemData, TContext>` | dump.cs:693828 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRectCell.cs:3 |
| 363 | 泛型与启动链_补读.tsv | 669 | `UpdatePosition(float normalizedPosition, float localPosition)` | `FancyScrollRectCell<TItemData,` | `FancyScrollRectCell<TItemData, TContext>` | dump.cs:693828 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollRectCell.cs:3 |
| 364 | 泛型与启动链_补读.tsv | 671 | `Initialize()` | `FancyScrollView<TItemData,` | `FancyScrollView<TItemData, TContext>` | dump.cs:692628 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollView.cs:5 |
| 365 | 泛型与启动链_补读.tsv | 672 | `UpdateContents(IList<TItemData> itemsSource)` | `FancyScrollView<TItemData,` | `FancyScrollView<TItemData, TContext>` | dump.cs:692628 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollView.cs:5 |
| 366 | 泛型与启动链_补读.tsv | 673 | `Relayout()` | `FancyScrollView<TItemData,` | `FancyScrollView<TItemData, TContext>` | dump.cs:692628 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollView.cs:5 |
| 367 | 泛型与启动链_补读.tsv | 674 | `Refresh()` | `FancyScrollView<TItemData,` | `FancyScrollView<TItemData, TContext>` | dump.cs:692628 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollView.cs:5 |
| 368 | 泛型与启动链_补读.tsv | 675 | `UpdatePosition(float position)` | `FancyScrollView<TItemData,` | `FancyScrollView<TItemData, TContext>` | dump.cs:692628 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollView.cs:5 |
| 369 | 泛型与启动链_补读.tsv | 676 | `UpdatePosition(float position, bool forceRefresh)` | `FancyScrollView<TItemData,` | `FancyScrollView<TItemData, TContext>` | dump.cs:692628 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollView.cs:5 |
| 370 | 泛型与启动链_补读.tsv | 677 | `ResizePool(float firstPosition)` | `FancyScrollView<TItemData,` | `FancyScrollView<TItemData, TContext>` | dump.cs:692628 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollView.cs:5 |
| 371 | 泛型与启动链_补读.tsv | 678 | `UpdateCells(float firstPosition, int firstIndex, bool forceRefresh)` | `FancyScrollView<TItemData,` | `FancyScrollView<TItemData, TContext>` | dump.cs:692628 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollView.cs:5 |
| 372 | 泛型与启动链_补读.tsv | 679 | `CircularIndex(int i, int size)` | `FancyScrollView<TItemData,` | `FancyScrollView<TItemData, TContext>` | dump.cs:692628 ／ ILSpy Assembly-CSharp-firstpass/UnityEngine/UI/Extensions/FancyScrollView.cs:5 |
| 373 | 泛型与启动链_补读.tsv | 683 | `CanLocalize(Localize cmp)` | `LocalizeTargetDesc_Type<T,` | `LocalizeTargetDesc_Type<T, G>` | dump.cs:139790 ／ ILSpy Assembly-CSharp/I2/Loc/LocalizeTargetDesc_Type.cs:5 |
| 374 | 泛型与启动链_补读.tsv | 684 | `CreateTarget(Localize cmp)` | `LocalizeTargetDesc_Type<T,` | `LocalizeTargetDesc_Type<T, G>` | dump.cs:139790 ／ ILSpy Assembly-CSharp/I2/Loc/LocalizeTargetDesc_Type.cs:5 |
| 375 | 泛型与启动链_补读.tsv | 702 | `Equals(object value)` | `<>f__AnonymousType90<<GroupName>j__TPar,` | `<>f__AnonymousType90<<GroupName>j__TPar, <PlayerId>j__TPar>` | dump.cs:9093 |
| 376 | 泛型与启动链_补读.tsv | 703 | `GetHashCode()` | `<>f__AnonymousType90<<GroupName>j__TPar,` | `<>f__AnonymousType90<<GroupName>j__TPar, <PlayerId>j__TPar>` | dump.cs:9093 |
| 377 | 泛型与启动链_补读.tsv | 704 | `ToString()` | `<>f__AnonymousType90<<GroupName>j__TPar,` | `<>f__AnonymousType90<<GroupName>j__TPar, <PlayerId>j__TPar>` | dump.cs:9093 |
| 378 | 泛型与启动链_补读.tsv | 705 | `Equals(object value)` | `<>f__AnonymousType91<<GroupName>j__TPar,` | `<>f__AnonymousType91<<GroupName>j__TPar, <Description>j__TPar, <BadgeId>j__TPar, <Region>j__TPar, <Privacy>j__TPar, <MinElo>j__TPar>` | dump.cs:9180 |
| 379 | 泛型与启动链_补读.tsv | 706 | `GetHashCode()` | `<>f__AnonymousType91<<GroupName>j__TPar,` | `<>f__AnonymousType91<<GroupName>j__TPar, <Description>j__TPar, <BadgeId>j__TPar, <Region>j__TPar, <Privacy>j__TPar, <MinElo>j__TPar>` | dump.cs:9180 |
| 380 | 泛型与启动链_补读.tsv | 707 | `ToString()` | `<>f__AnonymousType91<<GroupName>j__TPar,` | `<>f__AnonymousType91<<GroupName>j__TPar, <Description>j__TPar, <BadgeId>j__TPar, <Region>j__TPar, <Privacy>j__TPar, <MinElo>j__TPar>` | dump.cs:9180 |
| 381 | 泛型与启动链_补读.tsv | 708 | `Equals(object value)` | `<>f__AnonymousType92<<GroupName>j__TPar,` | `<>f__AnonymousType92<<GroupName>j__TPar, <PlayerId>j__TPar, <NewRole>j__TPar>` | dump.cs:9323 |
| 382 | 泛型与启动链_补读.tsv | 709 | `GetHashCode()` | `<>f__AnonymousType92<<GroupName>j__TPar,` | `<>f__AnonymousType92<<GroupName>j__TPar, <PlayerId>j__TPar, <NewRole>j__TPar>` | dump.cs:9323 |
| 383 | 泛型与启动链_补读.tsv | 710 | `ToString()` | `<>f__AnonymousType92<<GroupName>j__TPar,` | `<>f__AnonymousType92<<GroupName>j__TPar, <PlayerId>j__TPar, <NewRole>j__TPar>` | dump.cs:9323 |
| 384 | 泛型与启动链_补读.tsv | 711 | `Equals(object value)` | `<>f__AnonymousType93<<GroupId>j__TPar,` | `<>f__AnonymousType93<<GroupId>j__TPar, <GroupData>j__TPar>` | dump.cs:9424 |
| 385 | 泛型与启动链_补读.tsv | 712 | `GetHashCode()` | `<>f__AnonymousType93<<GroupId>j__TPar,` | `<>f__AnonymousType93<<GroupId>j__TPar, <GroupData>j__TPar>` | dump.cs:9424 |
| 386 | 泛型与启动链_补读.tsv | 713 | `ToString()` | `<>f__AnonymousType93<<GroupId>j__TPar,` | `<>f__AnonymousType93<<GroupId>j__TPar, <GroupData>j__TPar>` | dump.cs:9424 |
| 387 | 泛型与启动链_补读.tsv | 714 | `Equals(object value)` | `<>f__AnonymousType95<<GroupId>j__TPar,` | `<>f__AnonymousType95<<GroupId>j__TPar, <PlayerId>j__TPar>` | dump.cs:9640 |
| 388 | 泛型与启动链_补读.tsv | 715 | `GetHashCode()` | `<>f__AnonymousType95<<GroupId>j__TPar,` | `<>f__AnonymousType95<<GroupId>j__TPar, <PlayerId>j__TPar>` | dump.cs:9640 |
| 389 | 泛型与启动链_补读.tsv | 716 | `ToString()` | `<>f__AnonymousType95<<GroupId>j__TPar,` | `<>f__AnonymousType95<<GroupId>j__TPar, <PlayerId>j__TPar>` | dump.cs:9640 |
| 390 | 泛型与启动链_补读.tsv | 742 | `LoadEventData(LiveOpsEventData data, LiveOpsEventPersistence saveGame, ILiveOpsHandler handler)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 391 | 泛型与启动链_补读.tsv | 743 | `SelectEvent()` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 392 | 泛型与启动链_补读.tsv | 744 | `ResetCollectedDraft(Action onComplete)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 393 | 泛型与启动链_补读.tsv | 745 | `Validate(TS save)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 394 | 泛型与启动链_补读.tsv | 746 | `GetCardsFromPacks(DraftEventSaveGame desiredSaveGame)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 395 | 泛型与启动链_补读.tsv | 747 | `SaveCards(TS desiredSaveGame, List<LoadableReference<RawCardScript>> cardsToSave, string eventIDToUse, Action onSuccess, Action onFail)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 396 | 泛型与启动链_补读.tsv | 748 | `OnMatchFound(string otherPlayerName)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 397 | 泛型与启动链_补读.tsv | 749 | `OnMatchFinished(float matchWon, RankedMatchResult result, Action<OnlineMatchResult> callback)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 398 | 泛型与启动链_补读.tsv | 750 | `OnMatchProcessSuccess(OnlineMatchResult result)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 399 | 泛型与启动链_补读.tsv | 751 | `OnMatchWon()` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 400 | 泛型与启动链_补读.tsv | 752 | `GetRandomPack(RawCardScript warlord, int packCount)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 401 | 泛型与启动链_补读.tsv | 753 | `PickPack(PrebuiltPack pack, Action onSuccess, Action onFail)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 402 | 泛型与启动链_补读.tsv | 754 | `ReRollPack(Action onSuccess, Action onFail)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 403 | 泛型与启动链_补读.tsv | 755 | `GetBaseData()` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 404 | 泛型与启动链_补读.tsv | 756 | `GetBaseSave()` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 405 | 泛型与启动链_补读.tsv | 757 | `UpdateState()` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 406 | 泛型与启动链_补读.tsv | 758 | `GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 407 | 泛型与启动链_补读.tsv | 759 | `SetCustomMatchOptions(RoomOptions options, int currentELO)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 408 | 泛型与启动链_补读.tsv | 760 | `PayEventEntrance(bool is10X, Action onSuccess, Action onError)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 409 | 泛型与启动链_补读.tsv | 761 | `FreeEventEntrance(Action onSuccess, Action onError)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 410 | 泛型与启动链_补读.tsv | 762 | `FinishRun(Action onFinish)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 411 | 泛型与启动链_补读.tsv | 763 | `CollectRewards(Action<RewardInfo[]> onSuccess, Action onError)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 412 | 泛型与启动链_补读.tsv | 764 | `RefundRun(Action<RewardInfo[]> onComplete, Action onError)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 413 | 泛型与启动链_补读.tsv | 765 | `b__9_0()` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 414 | 泛型与启动链_补读.tsv | 766 | `b__19_1(RawCardScript wl)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |
| 415 | 泛型与启动链_补读.tsv | 767 | `b__20_1(RawCardScript warlord)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` | dump.cs:127547 ／ ILSpy Assembly-CSharp/Everguild/LiveOps/DraftEventBase.cs:6 |

---

## §三 清单误报（本来正确 / 不该改）

**0 条。** 逐项核过，448 行里没有一行是「现值其实完整」的误报：

1. **现值 == 正确值 的行 = 0**（若相等即是误报；全表比对为 0）。
2. **415 个现存行的现值全部仍是残缺**（`原版类` 以 `, ` 截断，或缺 `>`）—— 与独立「全 21 份主表扫残缺」结果完全一致（共 415，全在泛型表）。
3. **非泛型的匿名类型未混入**（Q4 md 特别提醒的 `<>f__AnonymousType31`=非泛型、现值本身完整 **不补**）：本 448 行的 234 个匿名类型行**全部**以 `, ` 截断或尖括号不配平，无一条是「无参数完整名」。
4. 未被 Q4 收进 TSV 的其它形态（空 / `—` / 把方法名写进类名列）在本批 448 行里 **0 例**：现值一律是「泛型类名被截断」，没有空值、没有 `—`、没有把方法名塞进类名列。

### §三·补 —— 33 行「行已被删、无需改」（不是误报，是账变了）

Q4（10-28 21:39）之后，`_parts/_lists/B93_Q4_缩表.tsv` 驱动 `b93_shrink.py` 把一批重复行从主表删掉。下 33 行的 `(方法, 判据出处)` 现读在主表里**已不存在**（且用「方法清单名前缀」再搜也无残留 ⇒ 确认删除，不是改了出处）：

| 方法签名 | 原现值 | 正确值（若将来复现再用） |
|---|---|---|
| `GetDivisionByPoints(long points)` | `RankedEventBase<TSave,` | `RankedEventBase<TSave, TData>` |
| `Initialize()` | `ShopStore<TSave,` | `ShopStore<TSave, TData>` |
| `GetBaseSave()` | `ShopStore<TSave,` | `ShopStore<TSave, TData>` |
| `RemoveFinishedOffer()` | `ShopStore<TSave,` | `ShopStore<TSave, TData>` |
| `GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` |
| `OnMatchFound(string otherPlayerName)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` |
| `OnMatchFinished(float matchWon, RankedMatchResult result, Action<OnlineMatchResult> callback)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` |
| `OnMatchProcessSuccess(OnlineMatchResult result)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` |
| `Expire(Action onComplete)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` |
| `TryCollect(Action onComplete)` | `FastModeBaseEvent<TS,` | `FastModeBaseEvent<TS, TD>` |
| `SelectEvent()` | `GameNewsEvent<TPersistence,` | `GameNewsEvent<TPersistence, TData>` |
| `GetTitle()` | `GameNewsEvent<TPersistence,` | `GameNewsEvent<TPersistence, TData>` |
| `GetMessage()` | `GameNewsEvent<TPersistence,` | `GameNewsEvent<TPersistence, TData>` |
| `Initialize()` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` |
| `InitializeChallenge(MissionChallenge challenge)` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` |
| `Collect(Action onComplete)` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` |
| `Collect(MissionChallenge challenge, Action onComplete)` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` |
| `UpdateChallengeProgress(ChallengeUpdatePayload payload)` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` |
| `TryCollect(Action onComplete)` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` |
| `GetContainer()` | `MissionEvent<TS,` | `MissionEvent<TS, TD>` |
| `CheckPreviousMatch()` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` |
| `StartMatchNoCheck(CardDeck playerDeck)` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` |
| `MakeOpponentInfo(MatchData matchData)` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` |
| `GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost)` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` |
| `GetEventDeck()` | `PlayEvent<TSave,` | `PlayEvent<TSave, TData>` |
| `PurchaseMilestone(int milestoneIndex, Action<RewardInfo[]> onComplete, Action<string> onError)` | `ScoringEvent<TS,` | `ScoringEvent<TS, TD>` |
| `ClaimRewards()` | `BaseInboxEvent<TSave,` | `BaseInboxEvent<TSave, TData>` |
| `GetRandomWarlords()` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` |
| `GetPossibleWarlords()` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` |
| `PickWarlord(RawCardScript warlord, Action onSuccess, Action onFail)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` |
| `GetEventDeck()` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` |
| `ValidateEventDeck(CardDeck deck, bool validateOwnership = True)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` |
| `Expire(Action onComplete)` | `DraftEventBase<TS,` | `DraftEventBase<TS, TD>` |

> 这 33 行**当前无可改对象**；若日后有人把它们重新加回主表，按上表「正确值」建行即可。

---

## §四 判不了

**0 条。** 全部 448 行的正确值均可从 `dump.cs`（并 21 类的 ILSpy 双源）确定，无「差什么」的悬空项。

---

## §五 ⚠️ 被在飞补丁引用的行（改前须等补丁合完）

**合并键机制**：`工具/merge_ledger_parts.py` 用 `(原版类, 方法)` 全量主表定位补丁行（`merge_ledger_parts.py:195/221`）。
⇒ 若先把本批某行的 `原版类` 改成完整名，而某在飞补丁仍用**残缺现值**作键 ⇒ 合并时**键对不上**，会被当成**新行追加**（主表里冒出重复行）。

现读 `账/_parts/B94_*.tsv`（7 份：F1/F3/F4/F5/S1/S2/S4）后，**命中本批的行 = 2 行，全在 `B94_S4.tsv`**：

| 补丁文件 | 补丁键 (原版类, 方法) | 对应主表行 | 处理 |
|---|---|---|---|
| `账/_parts/B94_S4.tsv` | ( `ToggleCollectionFilter<T,` , `b__5_0(bool b)` ) | `泛型与启动链_补读.tsv:137`（状态 B） | ⚠️ **等 B94_S4 合完再改此格** |
| `账/_parts/B94_S4.tsv` | ( `ToggleCollectionFilter<T,` , `b__7_2(T rc)` ) | `泛型与启动链_补读.tsv:138`（状态 B） | ⚠️ **等 B94_S4 合完再改此格** |

- 其余 413 行：**没有任何在飞 B94 补丁引用**，可直接改。
- 补充：这 2 行对应的逻辑方法在 `账/收藏.tsv:317/318` 有另一套记录，但那边 `原版类=ToggleCollectionFilter`（无泛型参数）、`方法`带 `_003C…_003E` 转义前缀 ⇒ 键与补丁不一致，**不构成合并键冲突**；本清单只处理泛型表这 2 行。
- 另核：**改完后不会与其它主表产生 `(正确值, 方法)` 重复键**（全 21 表比对 = 0 冲突），也不会在泛型表内自撞（0）。所以除上面 2 行外无后续合并风险。

---

## 附：核实方法与证据（可复查）

- **行号来源**：Q4 的 `行号` = `账/_parts/_lists/B93_Q4_泛型表_bak.tsv`（Q4 当时的泛型表快照，840 行）的行号 —— 逐行核 **448/448 现值完全一致**（0 不符）。当前主表经缩表后为 799 行，故按 `(方法, 判据出处)` 唯一键回主表现读（该键在当前泛型表内 **798/798 唯一**）。
- **正确值源**：dump.cs 声明行见 §一·附与 §二·A 的 `判据` 列；ILSpy 桩路径见 §二·A 泛型类那几行（`Assembly-CSharp[-firstpass]/…`）。
- **删除判定**：33 行 `(方法, 判据出处)` 现读不在主表；再按方法清单名前缀搜「现值/正确值 类」也无残留 ⇒ 不是改出处而是删行；且 33 行**恰好是** `B93_Q4_缩表.tsv` 里 `操作=删` 的行（交叉核对 33/33 吻合）。

