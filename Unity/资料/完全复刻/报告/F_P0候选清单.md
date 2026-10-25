# P0 候选清单（主链功能点下的 P1 行）

> **判据（调度台一句话）**：**每一局都会遇到、且玩家能看见 ⇒ 应该是 P0。**
> 数据来源 = `资料/完全复刻/账/*.tsv` 的 `状态 = C` 且 `等级 = P1` 的行，再按功能点索引的 `主链` 过滤。
> ⛔ **本文件不改账**，只出清单，交给 owner 复核。

## §0 数字

- 全账 `P1` 行：**369** 条（其中主链功能点下 **236** 条 · 边角 71 · 暂停 62）
- 本清单：**主链下的 236 条** —— 优先域（战斗/收藏）**19** 条 + `存档与玩家数据` **217** 条（活动线/暂停线已跳过）
- 判定分布：不确定 170 · 否 61 · 是 5

## §1 优先：战斗 / 收藏（19 条，逐行判过）

| # | 账 | 类.方法 | 判据要点 | 现在等级 | **每一局都会遇到吗** | 理由 |
|---|---|---|---|---|---|---|
| 1 | 战斗界面_HUD.tsv | `BattleCardUI.UpdateBastionr` | 在场时显示堡垒值（>0 才显示） | **P1** | 否 | 只有带「堡垒」词条的卡在场且值 >0 时才显示 |
| 2 | 战斗界面_HUD.tsv | `BattleHud.SetTurnPlayedCards` | 本回合已出牌数 → 逐个点亮 offensiveCardTurnCardsObjs | **P1** | 🔴 **是** | 每回合打出的牌都会逐个点亮「本回合已出牌」那一排 |
| 3 | 战斗界面_HUD.tsv | `ClockManager.ClockRunningAnim` | 表在走 = !paused && !forAnim | **P1** | 🔴 **是** | **E 报的那条**：每回合表都在走；「演出期间停表」缺 ⇒ 玩家实际回合时长变短 |
| 4 | 战斗界面_HUD.tsv | `ChooseCardMenu.CountdownTrigger` | 每跳秒：未选则随机选一张、按钮显示 0:xx，<3 秒自动确认 | **P1** | 🔴 **是** | 每次要选卡的技能/计策都会走这条倒计时（每局少则几次） |
| 5 | 战斗界面_HUD.tsv | `ChooseCardMenu.EndSelectionWithDelay` | 停 0.5s → 未选则随机选 → 写 0:xx → timer<3 则直接确认 | **P1** | 🔴 **是** | 同上：倒计时结束的自动确认 |
| 6 | 战斗界面_HUD.tsv | `ChooseCardMenu.SetTimer` | 继续钮文案 = 0:xx | **P1** | 🔴 **是** | 同上：倒计时文案 0:xx 直接显示给玩家 |
| 7 | 泛型与启动链_补读.tsv | `CollectionDisplay<T>.OnEnable` | 挂 DragAndDropItem.OnEndDragEvent → OnEndDragEvent | **P1** | 否 | 开收藏页才走，不是每一局 |
| 8 | 泛型与启动链_补读.tsv | `CollectionDisplay<T>.OnDisable` | 摘同一个回调 | **P1** | 否 | 同上 |
| 9 | 泛型与启动链_补读.tsv | `CollectionDisplay<T>.OnEndDragEvent` | 自身 activeInHierarchy 时 ReorderCollectionByHierarchy(target) | **P1** | 否 | 玩家手动拖拽排序才走 |
| 10 | 泛型与启动链_补读.tsv | `CollectionDisplay<T>.ReorderCollectionByHierarchy` | 按 transform 的 siblingIndex 反推新下标… | **P1** | 否 | 同上 |
| 11 | 泛型与启动链_补读.tsv | `Inventory<T>.UpdateItemCollection` | 把实例列表重建成 id→IItemInfo 字典（增删后调用） | **P1** | 不确定 | 增删物品后重建字典 —— 每局结束发卡会走，但玩家看不见这一步本身 |
| 12 | 泛型与启动链_补读.tsv | `Inventory<T>.OnItemCountModifyInternal` | 数量变化内部处理：更新 dict、通知子集/界面 | **P1** | 不确定 | 同上 |
| 13 | 泛型与启动链_补读.tsv | `Inventory<T>.OnItemCountModify` | 对外入口：按 T+amount 记一笔数量变化 | **P1** | 不确定 | 同上（对外入口） |
| 14 | 泛型与启动链_补读.tsv | `Inventory<T>.Create` | 新建实例（item+instanceId+customData） | **P1** | 不确定 | 新建实例 —— 首次获得某张卡才走 |
| 15 | 泛型与启动链_补读.tsv | `Inventory<T>.Add` | 加数量：可指定 instanceId/customData/overrideAmount；无则合并到已有实例 | **P1** | 不确定 | 加数量 —— 每局结束发卡会走 |
| 16 | 泛型与启动链_补读.tsv | `Inventory<T>.TryGetItem` | 按 id 取 IItemInfo | **P1** | 否 | 查询，玩家看不见 |
| 17 | 泛型与启动链_补读.tsv | `Inventory<T>.GetItems` | 全部 IItemInfo（含数量） | **P1** | 否 | 查询，玩家看不见 |
| 18 | 泛型与启动链_补读.tsv | `Inventory<T>.Get` | 按物品取 info；autoCreate 时缺则建 | **P1** | 否 | 查询，玩家看不见 |
| 19 | 泛型与启动链_补读.tsv | `Inventory<T>.Get` | 按 id 取（内部转字典） | **P1** | 否 | 查询，玩家看不见 |

### 1.1 其中「是」的（建议 owner 优先复核成 P0）

- 🔴 `BattleHud.SetTurnPlayedCards`（`BattleHud.cs:267`）—— 每回合打出的牌都会逐个点亮「本回合已出牌」那一排
- 🔴 `ChooseCardMenu.CountdownTrigger`（`ChooseCardMenu.cs:797`）—— 每次要选卡的技能/计策都会走这条倒计时（每局少则几次）
- 🔴 `ChooseCardMenu.EndSelectionWithDelay`（`ChooseCardMenu.cs:651`）—— 同上：倒计时结束的自动确认
- 🔴 `ChooseCardMenu.SetTimer`（`ChooseCardMenu.cs:1224`）—— 同上：倒计时文案 0:xx 直接显示给玩家
- 🔴 `ClockManager.ClockRunningAnim`（`ClockManager.cs:415`）—— **E 报的那条**：每回合表都在走；「演出期间停表」缺 ⇒ 玩家实际回合时长变短

> ⚠️ **E 报的那条就在这一批里**：`ClockManager.ClockRunningAnim`（`战斗界面_HUD.tsv:389`，现 P1）—— 账里 `判据要点` 已经写明「结算演出期间表照走、玩家实际时长变短」。
> 另外 `PauseForAnim` / `UnpauseForAnim` 两行现在是 **P2**（`战斗界面_HUD.tsv:402/415`），它们和上一条是**同一个缺口的三行**，等级却不一致 ⇒ 复核时**三行一起改**。

### 1.2 四条「本该在这里、却落在边角功能点」的（⛔ 不藏）

| 账 | 类.方法 | 判据要点 | 现在等级 | 每一局都会遇到吗 | 说明 |
|---|---|---|---|---|---|
| 战斗界面_HUD.tsv | `BattleErrorUIManager.ConnectionStatusChanged` | 连接状态变化 ⇒ 断线/重连弹窗 | **P1** | 否 | 只在断线时出现（出现时玩家一定看见，但不是每局） |
| 战斗界面_HUD.tsv | `BattleErrorUIManager.OpponentDesyncCancelMatch` | 对手不同步 ⇒ 取消对局 | **P1** | 否 | 同上 |
| 战斗界面_HUD.tsv | `BattleErrorUIManager.ShowDisconnectionPopUp` | 断线弹窗 + PauseClock | **P1** | 否 | 同上 |
| 战斗界面_HUD.tsv | `BattleSettingsWindow.Open` | 开窗：跳过教程钮/回放改 Exit/三滑块/语言/触摸输入 | **P1** | 否 | 玩家主动点设置才开 |

**另有一条域级事实**：`商店`（54 条 C）与 `卡组编辑`（18 条 C）**一条 P1 都没有，全是 P2**；`收藏` 13 条 P1 **全部在本清单 §1**（`CollectionDisplay<T>` 4 条 + `Inventory<T>` 9 条）。⇒ 这两个高频域如果真有问题，问题**目前全被压在 P2**，值得 owner 抽看。

## §2 `存档与玩家数据` 的 217 条 P1（单独一节，原因见 2.1）

### 2.1 先说一处**账质量问题**（拼清单时发现）

这 **217** 行的 `判据出处` 各不相同（217 个行号），但 **`判据要点` 只有 1 种**—— 全是同一句**类级摘要**：「中央存档（玩家档案 / 等级 / 货币 / 库存重建 / 每个活动一份 `TSave`）」。
⇒ **账里没有逐方法的行为描述**，所以「每一局都会遇到吗」**无法从账判**。本节的判定只能靠**方法名**：名字属低频族的判「否」，其余一律「不确定」。

🔴 **建议**：这 217 行要么补逐方法要点（owner 才知道每个方法干什么），要么把它们**按行为族合并成十几行**（例如「货币增减」「卡牌持有」「活动存档读写」），否则这份账在这一块**不可复核**。

### 2.2 名字级判定（「否」的 = 明显不是每局必经）

| # | 类.方法 | 每一局都会遇到吗 | 理由 |
|---|---|---|---|
| 1 | `AddCollectorXp` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 2 | `AddFriendToList` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 4 | `AddNewsAfterLevelUp` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 5 | `AddPracticeMatch` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 6 | `AddTapTranslation` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 9 | `AllTutorialsCompleted` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 18 | `CheckAccountUserInNewerVersion` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 21 | `CheckShowRateGame` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 27 | `CreateTutorialResult` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 30 | `DeactivateAccountProcess` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 31 | `DeleteDeck` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 32 | `DeleteFriendFromList` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 36 | `FinishLogin` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 38 | `FinishShareScreenPicture` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 39 | `FinishShareURL` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 40 | `FirstTwoTutorialCompleted` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 43 | `ForceCompleteFirstTutorials` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 44 | `FullyQuitApplication` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 75 | `GrantItemsCrate` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 81 | `IsBannedFromChat` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 85 | `IsTutorialIndexCompleted` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 89 | `LoadSharedDefaultValues` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 92 | `Logout` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 93 | `LogoutPlayfab` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 95 | `NotLinkedSteamAccount` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 101 | `OnReportPlayer` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 120 | `ProcessUpdateUserLevelNews` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 121 | `QuitApplication` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 128 | `RecordPracticeMatchFinished` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 130 | `RecordTutorialFinished` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 131 | `RecordTutorialPassed` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 134 | `RegisterToPlayFab` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 135 | `RemovePracticeMatch` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 137 | `ResetPassword` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 146 | `SendAccountConfirmationEmail` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 156 | `SetTargetFramerate` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 158 | `SetTutorialState` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 165 | `ShareScreenPicture` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 166 | `ShareURL` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 167 | `ShouldSellCardsFromCrate` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 168 | `ShouldShowRatePopup` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 170 | `ShowGrantedXpToast` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 171 | `ShowNewLevelToast` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 172 | `TestResetTutorial` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 173 | `UnlinkDeviceAccount` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 183 | `UpdateLevelNews` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 189 | `UpdateUserLevelNews` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 191 | `UploadOnlineFriendsData` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 194 | `UploadTutorialV2SaveData` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 205 | `_003CRecordTutorialFinished_003Eg__HandleSuccess_007C464_0` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 206 | `_003CRecordTutorialPassed_003Eg__HandleSuccess_007C465_0` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |
| 215 | `_003CUpdateUserLevelNews_003Eb__621_0` | 否 | 名字属低频族（好友/收件箱/聊天/教程/成就/社交…），不是每局必经 |

### 2.3 其余 217 行（一律「不确定」—— 要读方法体）

| # | 类.方法 | 每一局都会遇到吗 |
|---|---|---|
| 1 | `AddMoreDuplicatesToSell` | 不确定 |
| 2 | `AllExpansionCards` | 不确定 |
| 3 | `AllLegionCards` | 不确定 |
| 4 | `ApplyGraphicsQuality` | 不确定 |
| 5 | `ApplySettingsOptions` | 不确定 |
| 6 | `AreDecksEqual` | 不确定 |
| 7 | `AsyncInitialization` | 不确定 |
| 8 | `Awake` | 不确定 |
| 9 | `BlockGame` | 不确定 |
| 10 | `CancelMatch` | 不确定 |
| 11 | `ChangePlayerName` | 不确定 |
| 12 | `CheckForTutoriaDoneAndDataFix` | 不确定 |
| 13 | `CheckLastOpponentInfo` | 不确定 |
| 14 | `CheckUpdateNeeded` | 不确定 |
| 15 | `CheckUpdateRequired` | 不确定 |
| 16 | `ClaimSeasonPrize` | 不确定 |
| 17 | `ClearLocalPlayerData` | 不确定 |
| 18 | `CreatePlayerBattleData` | 不确定 |
| 19 | `CurrentMillis` | 不确定 |
| 20 | `CurrentServerDateTime` | 不确定 |
| 21 | `DownloadInAppPurchases` | 不确定 |
| 22 | `ExpansionCardsObtained` | 不确定 |
| 23 | `ExtractDeckInfo` | 不确定 |
| 24 | `FinishSellingBulk` | 不确定 |
| 25 | `FixNewVersionConflicts` | 不确定 |
| 26 | `FixSkirmishDeck` | 不确定 |
| 27 | `GetAllPublisherPlayerData` | 不确定 |
| 28 | `GetClosedDeckByCards` | 不确定 |
| 29 | `GetClosedDeckById` | 不确定 |
| 30 | `GetClosedDeckByPackIds` | 不确定 |
| 31 | `GetCosmeticItem` | 不确定 |
| 32 | `GetDeckIdWithPacks` | 不确定 |
| 33 | `GetEnviromentString` | 不确定 |
| 34 | `GetFactionEventsRatingPlayfabID` | 不确定 |
| 35 | `GetFilteredSpareCardsToSell` | 不确定 |
| 36 | `GetGuildSearch` | 不确定 |
| 37 | `GetHeroCard` | 不确定 |
| 38 | `GetLastUserLocation` | 不确定 |
| 39 | `GetLeadeboardClans` | 不确定 |
| 40 | `GetLeaderboard` | 不确定 |
| 41 | `GetLeaderboardPlayfabIDGuild` | 不确定 |
| 42 | `GetLeaderboardPlayfabIDGuildSpots` | 不确定 |
| 43 | `GetLegionRankedMatchesWon` | 不确定 |
| 44 | `GetMD5Hash` | 不确定 |
| 45 | `GetMatchInfo` | 不确定 |
| 46 | `GetOnlineData` | 不确定 |
| 47 | `GetOnlineData` | 不确定 |
| 48 | `GetPackById` | 不确定 |
| 49 | `GetPlayerCampaignHero` | 不确定 |
| 50 | `GetPreviousVersion` | 不确定 |
| 51 | `GetRandomPlayerLeaderboardAroundPlayfabID` | 不确定 |
| 52 | `GetRandomTip` | 不确定 |
| 53 | `GetSteamAuthTicket` | 不确定 |
| 54 | `GetSuspiciousIds` | 不确定 |
| 55 | `GetTopLeaderboard` | 不确定 |
| 56 | `GetXPFromMatch` | 不确定 |
| 57 | `InitializeLiveOpsAndConfig` | 不确定 |
| 58 | `InitializeNotifications` | 不确定 |
| 59 | `InitializeSteam` | 不确定 |
| 60 | `InitializeTitleData` | 不确定 |
| 61 | `InitializeVariables` | 不确定 |
| 62 | `IsBattleLogCreated` | 不确定 |
| 63 | `IsEventDeckFromWarlord` | 不确定 |
| 64 | `IsTherePendingMatch` | 不确定 |
| 65 | `LegionCardsObtained` | 不确定 |
| 66 | `LoadAllCards` | 不确定 |
| 67 | `LoadPlayerData` | 不确定 |
| 68 | `LoadStarterData` | 不确定 |
| 69 | `LoginToPlayFab` | 不确定 |
| 70 | `ModifyBattleLogBotAltId` | 不确定 |
| 71 | `OnChangeCosmetics` | 不确定 |
| 72 | `OnCheckDisplayNameInvite` | 不确定 |
| 73 | `OnPfAndroidReg` | 不确定 |
| 74 | `OnPfFail` | 不确定 |
| 75 | `OnPfIosReg` | 不确定 |
| 76 | `OrderDecks` | 不确定 |
| 77 | `PinMatchRecording` | 不确定 |
| 78 | `PreloadLeaderboards` | 不确定 |
| 79 | `ProcessBattleLog` | 不确定 |
| 80 | `ProcessCampaignDataObject` | 不确定 |
| 81 | `ProcessCardsSold` | 不确定 |
| 82 | `ProcessChangedCosmetics` | 不确定 |
| 83 | `ProcessClaimedSeasonPrize` | 不确定 |
| 84 | `ProcessLastOpponentInfo` | 不确定 |
| 85 | `ProcessMatchEndResult` | 不确定 |
| 86 | `ProcessMatchRecording` | 不确定 |
| 87 | `ProcessMatchResult` | 不确定 |
| 88 | `ProcessNewLogin` | 不确定 |
| 89 | `ProcessNewLoginResult` | 不确定 |
| 90 | `ProcessPublisherPlayerData` | 不确定 |
| 91 | `ProcessReferralData` | 不确定 |
| 92 | `ProcessReturnedItemsData` | 不确定 |
| 93 | `ProcessTitleDataAssetBundleList` | 不确定 |
| 94 | `ReadDataFromFile` | 不确定 |
| 95 | `RecordBossFactionPoints` | 不确定 |
| 96 | `RecordEventMatchFinished` | 不确定 |
| 97 | `RecordFactionPoints` | 不确定 |
| 98 | `RecordMatchWonByCancel` | 不确定 |
| 99 | `RecordMatchWonByCancelEnemy` | 不确定 |
| 100 | `RecordRankedMatchFinished` | 不确定 |
| 101 | `RecoverMatchRecording` | 不确定 |
| 102 | `RegisterForPush` | 不确定 |
| 103 | `ResetFactionPoints` | 不确定 |
| 104 | `ResetStats` | 不确定 |
| 105 | `SaveMatchRecording` | 不确定 |
| 106 | `SaveMatchRecording` | 不确定 |
| 107 | `SaveMatchWinnerInServer` | 不确定 |
| 108 | `SavePlayerData` | 不确定 |
| 109 | `SellBulkCard` | 不确定 |
| 110 | `SellSpareCards` | 不确定 |
| 111 | `SellSpareCardsRecursive` | 不确定 |
| 112 | `SetAltPorts` | 不确定 |
| 113 | `SetCosmeticTitle` | 不确定 |
| 114 | `SetDemoDeck` | 不确定 |
| 115 | `SetLastLoginNow` | 不确定 |
| 116 | `SetLocalStartTime` | 不确定 |
| 117 | `SetLoginData` | 不确定 |
| 118 | `SetMusicAndFxVolumes` | 不确定 |
| 119 | `SetPlayfabDisplayName` | 不确定 |
| 120 | `SetPromotion` | 不确定 |
| 121 | `SetTestData` | 不确定 |
| 122 | `SetupAllFcm` | 不确定 |
| 123 | `SetupFcm` | 不确定 |
| 124 | `SetupHelpshift` | 不确定 |
| 125 | `SetupLogs` | 不确定 |
| 126 | `SetupOverrides` | 不确定 |
| 127 | `SetupPlayfabPublisherVariables` | 不确定 |
| 128 | `ShowEarlyCampaignMode` | 不确定 |
| 129 | `UnlockAll` | 不确定 |
| 130 | `UnpackAddressables` | 不确定 |
| 131 | `UnpackConnectionSettings` | 不确定 |
| 132 | `UnpackInventory` | 不确定 |
| 133 | `UnpackOnlineVersionData` | 不确定 |
| 134 | `UnpackRawUserData` | 不确定 |
| 135 | `UnpackUserInfo` | 不确定 |
| 136 | `UnpackUserProfile` | 不确定 |
| 137 | `UpdateCardCollection` | 不确定 |
| 138 | `UpdateOnlinePublisherUserData` | 不确定 |
| 139 | `UpdateOnlinePublisherUserDataQueue` | 不确定 |
| 140 | `UpdateOnlineUserData` | 不确定 |
| 141 | `UpdateOnlineUserDataQueue` | 不确定 |
| 142 | `UpdateSunsetData` | 不确定 |
| 143 | `UploadOnlineDemoData` | 不确定 |
| 144 | `UploadOnlineTipsStatus` | 不确定 |
| 145 | `UploadOnlineWarpforgePlayed` | 不确定 |
| 146 | `UploadUnsyncedDeck` | 不确定 |
| 147 | `ValidatePlayerDeck` | 不确定 |
| 148 | `_003CCancelMatch_003Eb__478_1` | 不确定 |
| 149 | `_003CCheckUpdateNeeded_003Eb__635_1` | 不确定 |
| 150 | `_003CGetAllPublisherPlayerData_003Eg__OnFailure_007C515_1` | 不确定 |
| 151 | `_003CGetAllPublisherPlayerData_003Eg__OnSucess_007C515_0` | 不确定 |
| 152 | `_003CGetPlayerCampaignHero_003Eb__624_0` | 不确定 |
| 153 | `_003CRecordBossFactionPoints_003Eb__472_0` | 不确定 |
| 154 | `_003CRecordMatchWonByCancelEnemy_003Eb__480_1` | 不确定 |
| 155 | `_003CRecordMatchWonByCancel_003Eb__479_1` | 不确定 |
| 156 | `_003CSellSpareCardsRecursive_003Eb__602_0` | 不确定 |
| 157 | `_003CSetPromotion_003Eb__607_0` | 不确定 |
| 158 | `_003CUnlockAll_003Eb__451_0` | 不确定 |
| 159 | `_003CUnlockAll_003Eb__451_1` | 不确定 |
| 160 | `_003CUnlockAll_003Eb__451_2` | 不确定 |
| 161 | `_003CUnlockAll_003Eb__451_3` | 不确定 |
| 162 | `_003CUnlockAll_003Eb__451_4` | 不确定 |
| 163 | `_003CUnlockAll_003Eb__451_5` | 不确定 |
| 164 | `showDevTools` | 不确定 |
| 165 | `unlockAllCards` | 不确定 |

## §3 给 owner 的复核建议（按收益排序）

1. **先把 §1.1 那几条「是」的核准成 P0**（尤其 `ClockManager` 三行 —— 现 P1/P2/P2 不一致，它们说的是同一件事）。
2. **`Inventory<T>.{Add,OnItemCountModify,UpdateItemCollection,Create}`（4 条）**按判据应该是「每局结束发卡都会走」—— 但「玩家能看见」这一半要靠 owner 判定（收藏里多了一张卡算不算「看见」）。
3. **§2 那 217 条**先解决账的粒度问题，再谈 P0 —— 现在逐方法判不了。

## §4 本文件没做的

- **没判边角功能点下的 71 条 P1**（不在「主链」范围；需要时同法可出）。
- **没判暂停线的 62 条 P1**（按调度台要求跳过）。
- **没改账**：等级一律保留原值。

