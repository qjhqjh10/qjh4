# B94_R-E · 重复账核实（353 组 → 可直接照着删的清单）

> 会话：第九十四会话 · 波 1.5 · 只读账务代理 R-E
> 数据源：`判据/B93_Q13_重复账清单.tsv`（353 组 / 706 行，2026-10-28 22:16 生成）
> 主表：`账/*.tsv`（21 份，11 列）。**每一组均已回主表现读**：706/706 行在原行号逐字命中（class+method 全等）⇒ **未见行号漂移**，本文件行号 = 当前行号，可直接用。
> ⛔ 本文件只做判定并给出可执行删除清单；**未改动任何账/源码/正本**。

## §一 汇总

| 档 | 组数 | 说明 |
|---|---|---|
| **真重复（可删）** | **335** | 同一原版方法、(语义上)同一件事 ⇒ **共删 335 行**（每组删 1 行，保留域表「主题行」）。 |
| 　├ 纯删（无内容损失） | 168 | 两行状态一致、被删行无独有 差异/备注 ⇒ 直接删。 |
| 　└ 回填后删 | 167 | 两行状态不一致，或被删行带独有/更新的 差异·备注·状态修正 ⇒ **先把它并入保留行，再删**。 |
| **不是重复** | **0** | 见 §三（三类假重复来源逐一排除后为 0）。 |
| **判不了** | **18** | 同一原版方法的重复，但两行状态互斥（A↔D / A↔B / B↔D）且 ledger 无明确新旧 ⇒ **暂不删，待调度台仲裁**。见 §四。 |

删除落点分布：`泛型与启动链_补读.tsv` **332 行** · `日常_奖励.tsv` **3 行**（G041:783 · G042:782 · G043:42）。

**为什么这么多「域表行 + 泛型补读行」同方法两记**：349/353 组是「某域表 ↔ `泛型与启动链_补读.tsv`」。补读表按设计把泛型类方法另抄一份，绝大多数本身在 `备注` 里就写着「已并入主题表，本行为历史记账」。

## §二 可执行删除清单（335 行，已按 账文件 + 行号降序 排好，可照此顺序执行）

列义：`删除行` = 要删掉的行（泛型补读行/旧行）；`保留行` = 同一方法的域表「主题行」，删后只留它。「回填后删」的，**先把删除行的最新状态/差异/备注并进保留行（内容见理由列），再删**。

| # | 删除行(账文件:行号) | 原版类 | 方法 | 处置 | 保留行(账文件:行号) | 理由/回填内容 |
|---|---|---|---|---|---|---|
| 1 | 日常_奖励.tsv:783 | ChallengeLogMgr | LogAddEffect(CardEffect battleActionCardEffect) | 回填后删 | 日常_奖励.tsv:34 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 2 | 日常_奖励.tsv:782 | ChallengeLogMgr | LogHeal(BattleAction battleAction) | 回填后删 | 日常_奖励.tsv:37 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 3 | 日常_奖励.tsv:42 | ChallengeLogMgr | TroopKilled(BattleAction) | 回填后删 | 日常_奖励.tsv:781 | O(日常:42) 带 B93_E2 A→B 降档，回填 B 后删 O |
| 4 | 泛型与启动链_补读.tsv:799 | LoginManager | _003CThrowCustomError_003Eb__43_0() | 回填后删 | 其他_基础设施.tsv:339 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 5 | 泛型与启动链_补读.tsv:798 | LoginManager | _003CProcessLoginResult_003Eb__29_2() | 回填后删 | 其他_基础设施.tsv:338 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 6 | 泛型与启动链_补读.tsv:797 | LoginManager | _003CProcessLoginResult_003Eb__29_1() | 回填后删 | 其他_基础设施.tsv:337 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 7 | 泛型与启动链_补读.tsv:796 | LoginManager | _003CProcessLoginResult_003Eb__29_0() | 回填后删 | 其他_基础设施.tsv:336 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 8 | 泛型与启动链_补读.tsv:795 | LoginManager | _003CCheckInternetConnection_003Eb__25_0() | 回填后删 | 其他_基础设施.tsv:335 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 9 | 泛型与启动链_补读.tsv:794 | LoginManager | ThrowCustomError(CustomError customError, Action<string> actionAfterError, string cloudscriptTrace) | 回填后删 | 其他_基础设施.tsv:331 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 10 | 泛型与启动链_补读.tsv:793 | LoginManager | DuplicateConnection() | 回填后删 | 其他_基础设施.tsv:313 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 11 | 泛型与启动链_补读.tsv:792 | LoginManager | TryCompleteLogin() | 回填后删 | 其他_基础设施.tsv:332 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 12 | 泛型与启动链_补读.tsv:791 | LoginManager | CheckPlayerDeck() | 回填后删 | 其他_基础设施.tsv:309 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 13 | 泛型与启动链_补读.tsv:790 | LoginManager | DownloadInAppProducts() | 回填后删 | 其他_基础设施.tsv:312 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 14 | 泛型与启动链_补读.tsv:789 | LoginManager | DownloadFinishCallback(AssetBundleManager.DownloadInfo downloadInfo) | 回填后删 | 其他_基础设施.tsv:311 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 15 | 泛型与启动链_补读.tsv:788 | LoginManager | ProgressUpdateCallback(LoginState state, float progress) | 回填后删 | 其他_基础设施.tsv:323 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 16 | 泛型与启动链_补读.tsv:787 | LoginManager | ProcessData() | 回填后删 | 其他_基础设施.tsv:320 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 17 | 泛型与启动链_补读.tsv:786 | LoginManager | ReloadGame() | 回填后删 | 其他_基础设施.tsv:325 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 18 | 泛型与启动链_补读.tsv:785 | LoginManager | RetryLogin() | 回填后删 | 其他_基础设施.tsv:326 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 19 | 泛型与启动链_补读.tsv:784 | LoginManager | SetProgress(LoginState loginState) | 回填后删 | 其他_基础设施.tsv:329 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 20 | 泛型与启动链_补读.tsv:783 | LoginManager | ProceedSuccessfulLogin(LoginResult loginResult) | 回填后删 | 其他_基础设施.tsv:319 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 21 | 泛型与启动链_补读.tsv:782 | LoginManager | IsAccountLoginError(PlayFabErrorCode errorCode) | 回填后删 | 其他_基础设施.tsv:315 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 22 | 泛型与启动链_补读.tsv:781 | LoginManager | SetBackupLogin() | 回填后删 | 其他_基础设施.tsv:327 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 23 | 泛型与启动链_补读.tsv:780 | LoginManager | ProcessLoginResult(bool success, LoginResult loginResult, PlayFabError error, string gpgsError) | 回填后删 | 其他_基础设施.tsv:321 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 24 | 泛型与启动链_补读.tsv:779 | LoginManager | ProcessNormalLoginResult(bool success, LoginResult loginResult, PlayFabError error) | 回填后删 | 其他_基础设施.tsv:322 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 25 | 泛型与启动链_补读.tsv:778 | LoginManager | TryLoginToPlayfab() | 回填后删 | 其他_基础设施.tsv:333 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 26 | 泛型与启动链_补读.tsv:777 | LoginManager | LoadPlugins() | 回填后删 | 其他_基础设施.tsv:317 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 27 | 泛型与启动链_补读.tsv:776 | LoginManager | CheckInternetConnection() | 回填后删 | 其他_基础设施.tsv:306 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 28 | 泛型与启动链_补读.tsv:775 | LoginManager | UnpackPlayerData() | 回填后删 | 其他_基础设施.tsv:334 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 29 | 泛型与启动链_补读.tsv:774 | LoginManager | CheckNewVersionSucceed() | 回填后删 | 其他_基础设施.tsv:308 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 30 | 泛型与启动链_补读.tsv:773 | LoginManager | DoNewVersionOperations(System.Version oldVersion) | 回填后删 | 其他_基础设施.tsv:310 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 31 | 泛型与启动链_补读.tsv:772 | LoginManager | CheckNewVersion() | 回填后删 | 其他_基础设施.tsv:307 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 32 | 泛型与启动链_补读.tsv:771 | LoginManager | RegisterPushNotifications() | 回填后删 | 其他_基础设施.tsv:324 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 33 | 泛型与启动链_补读.tsv:770 | LoginManager | CheckDataLoaded() | 回填后删 | 其他_基础设施.tsv:305 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 34 | 泛型与启动链_补读.tsv:769 | LoginManager | OnDestroy() | 回填后删 | 其他_基础设施.tsv:318 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 35 | 泛型与启动链_补读.tsv:768 | LoginManager | Initialize() | 回填后删 | 其他_基础设施.tsv:314 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 36 | 泛型与启动链_补读.tsv:764 | DraftEventBase | RefundRun(Action<RewardInfo[]> onComplete, Action onError) | 纯删 | 轮抽.tsv:26 | T(A, B88-B2 可达档逐字等价) 较 O(R1 D 不做奖励) 新，保留 T |
| 37 | 泛型与启动链_补读.tsv:763 | DraftEventBase | CollectRewards(Action<RewardInfo[]> onSuccess, Action onError) | 回填后删 | 轮抽.tsv:5 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 38 | 泛型与启动链_补读.tsv:762 | DraftEventBase | FinishRun(Action onFinish) | 纯删 | 轮抽.tsv:7 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 39 | 泛型与启动链_补读.tsv:761 | DraftEventBase | FreeEventEntrance(Action onSuccess, Action onError) | 回填后删 | 轮抽.tsv:8 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 40 | 泛型与启动链_补读.tsv:760 | DraftEventBase | PayEventEntrance(bool is10X, Action onSuccess, Action onError) | 回填后删 | 轮抽.tsv:22 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 41 | 泛型与启动链_补读.tsv:759 | DraftEventBase | SetCustomMatchOptions(RoomOptions options, int currentELO) | 回填后删 | 轮抽.tsv:30 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 42 | 泛型与启动链_补读.tsv:757 | DraftEventBase | UpdateState() | 纯删 | 轮抽.tsv:31 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 43 | 泛型与启动链_补读.tsv:756 | DraftEventBase | GetBaseSave() | 纯删 | 轮抽.tsv:10 | T(A) 较 O(B87-R20 D) 更新，保留 T |
| 44 | 泛型与启动链_补读.tsv:755 | DraftEventBase | GetBaseData() | 纯删 | 轮抽.tsv:9 | T(B, R1 域表) 较 O(B87-R20 D 陈旧) 更新，保留 T |
| 45 | 泛型与启动链_补读.tsv:754 | DraftEventBase | ReRollPack(Action onSuccess, Action onFail) | 纯删 | 轮抽.tsv:25 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 46 | 泛型与启动链_补读.tsv:753 | DraftEventBase | PickPack(PrebuiltPack pack, Action onSuccess, Action onFail) | 纯删 | 轮抽.tsv:23 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 47 | 泛型与启动链_补读.tsv:752 | DraftEventBase | GetRandomPack(RawCardScript warlord, int packCount) | 回填后删 | 轮抽.tsv:15 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 48 | 泛型与启动链_补读.tsv:751 | DraftEventBase | OnMatchWon() | 回填后删 | 轮抽.tsv:21 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 49 | 泛型与启动链_补读.tsv:750 | DraftEventBase | OnMatchProcessSuccess(OnlineMatchResult result) | 回填后删 | 轮抽.tsv:20 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 50 | 泛型与启动链_补读.tsv:749 | DraftEventBase | OnMatchFinished(float matchWon, RankedMatchResult result, Action<OnlineMatchResult> callback) | 纯删 | 轮抽.tsv:18 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 51 | 泛型与启动链_补读.tsv:747 | DraftEventBase | SaveCards(TS desiredSaveGame, List<LoadableReference<RawCardScript>> cardsToSave, string eventIDToUse, Action onSuccess, Action onFail) | 纯删 | 轮抽.tsv:28 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 52 | 泛型与启动链_补读.tsv:746 | DraftEventBase | GetCardsFromPacks(DraftEventSaveGame desiredSaveGame) | 回填后删 | 轮抽.tsv:11 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 53 | 泛型与启动链_补读.tsv:744 | DraftEventBase | ResetCollectedDraft(Action onComplete) | 纯删 | 轮抽.tsv:27 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 54 | 泛型与启动链_补读.tsv:743 | DraftEventBase | SelectEvent() | 纯删 | 轮抽.tsv:29 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 55 | 泛型与启动链_补读.tsv:742 | DraftEventBase | LoadEventData(LiveOpsEventData data, LiveOpsEventPersistence saveGame, ILiveOpsHandler handler) | 回填后删 | 轮抽.tsv:17 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 56 | 泛型与启动链_补读.tsv:741 | WindowTabBase | WindowTabBase<T>.Setup(GameWindow window) | 回填后删 | 菜单_外壳.tsv:833 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 57 | 泛型与启动链_补读.tsv:736 | DynamicList | RemoveAt(int index) | 回填后删 | 菜单_外壳.tsv:155 | 同 G142 |
| 58 | 泛型与启动链_补读.tsv:735 | DynamicList | Remove(T element) | 回填后删 | 菜单_外壳.tsv:154 | 同 G142 |
| 59 | 泛型与启动链_补读.tsv:734 | DynamicList | Clear() | 回填后删 | 菜单_外壳.tsv:143 | 同 G142 |
| 60 | 泛型与启动链_补读.tsv:733 | DynamicList | IndexOf(T element) | 回填后删 | 菜单_外壳.tsv:150 | 同 G142 |
| 61 | 泛型与启动链_补读.tsv:732 | DynamicList | Inser(T element, int index) | 回填后删 | 菜单_外壳.tsv:151 | 同 G142 |
| 62 | 泛型与启动链_补读.tsv:731 | DynamicList | Add(T element) | 回填后删 | 菜单_外壳.tsv:142 | O B91-Q2「升A + 具体落点 LiveOpsEvent.cs:135」晚于 T 的 B90，采纳 A（回填时须清掉 T 内『状态回D/同判A』双句） |
| 63 | 泛型与启动链_补读.tsv:730 | DynamicList | GetAll<T1>() | 回填后删 | 菜单_外壳.tsv:146 | 同 G142 |
| 64 | 泛型与启动链_补读.tsv:729 | DynamicList | Get<T1>(int index) | 回填后删 | 菜单_外壳.tsv:145 | 同 G142 |
| 65 | 泛型与启动链_补读.tsv:728 | DynamicList | Get<T1>(T element) | 回填后删 | 菜单_外壳.tsv:144 | 同 G142 |
| 66 | 泛型与启动链_补读.tsv:727 | DynamicList | DynamicList<T>.GetIndexType(int index) | 回填后删 | 菜单_外壳.tsv:147 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 67 | 泛型与启动链_补读.tsv:726 | DynamicList | DynamicList<T>.ListType() | 回填后删 | 菜单_外壳.tsv:152 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 68 | 泛型与启动链_补读.tsv:725 | DynamicList | GetList() | 回填后删 | 菜单_外壳.tsv:149 | 同 G142 |
| 69 | 泛型与启动链_补读.tsv:723 | DraggableController | OnEndDrag(PointerEventData eventData) | 纯删 | 菜单_外壳.tsv:126 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 70 | 泛型与启动链_补读.tsv:722 | DraggableController | OnDrag(PointerEventData eventData) | 纯删 | 菜单_外壳.tsv:125 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 71 | 泛型与启动链_补读.tsv:721 | DraggableController | OnBeginDrag(PointerEventData eventData) | 纯删 | 菜单_外壳.tsv:124 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 72 | 泛型与启动链_补读.tsv:720 | DraggableController | SetDraggable(T content) | 纯删 | 菜单_外壳.tsv:127 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 73 | 泛型与启动链_补读.tsv:719 | Draggable | OnRelease() | 纯删 | 菜单_外壳.tsv:123 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 74 | 泛型与启动链_补读.tsv:718 | Draggable | OnDrag() | 纯删 | 菜单_外壳.tsv:120 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 75 | 泛型与启动链_补读.tsv:717 | Draggable | Initialize(T item) | 纯删 | 菜单_外壳.tsv:118 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 76 | 泛型与启动链_补读.tsv:624 | BaseInboxEvent | StartDate() | 回填后删 | 社交_聊天.tsv:183 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 77 | 泛型与启动链_补读.tsv:623 | BaseInboxEvent | IsRead() | 回填后删 | 社交_聊天.tsv:181 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 78 | 泛型与启动链_补读.tsv:622 | BaseInboxEvent | GetRewards() | 回填后删 | 社交_聊天.tsv:179 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 79 | 泛型与启动链_补读.tsv:621 | BaseInboxEvent | RedeemableAt() | 回填后删 | 社交_聊天.tsv:182 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 80 | 泛型与启动链_补读.tsv:620 | BaseInboxEvent | GetMessageContent() | 回填后删 | 社交_聊天.tsv:178 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 81 | 泛型与启动链_补读.tsv:619 | BaseInboxEvent | GetMessage() | 回填后删 | 社交_聊天.tsv:177 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 82 | 泛型与启动链_补读.tsv:618 | BaseInboxEvent | GetTitle() | 回填后删 | 社交_聊天.tsv:180 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 83 | 泛型与启动链_补读.tsv:617 | UiContainer | OnClick() | 回填后删 | 未分类_其他.tsv:718 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 84 | 泛型与启动链_补读.tsv:616 | UiContainer | HandleClick() | 回填后删 | 未分类_其他.tsv:714 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 85 | 泛型与启动链_补读.tsv:615 | UiContainer | OnInitialize(T liveOps) | 回填后删 | 未分类_其他.tsv:720 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 86 | 泛型与启动链_补读.tsv:614 | UiContainer | OnDestroy() | 回填后删 | 未分类_其他.tsv:719 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 87 | 泛型与启动链_补读.tsv:613 | UiContainer | Initialize(T e) | 回填后删 | 未分类_其他.tsv:715 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 88 | 泛型与启动链_补读.tsv:612 | UiContainer | Initialize(object payload) | 回填后删 | 未分类_其他.tsv:717 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 89 | 泛型与启动链_补读.tsv:611 | SingletonInitializableBehaviour | OnDestroy() | 纯删 | 未分类_其他.tsv:588 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 90 | 泛型与启动链_补读.tsv:610 | SingletonInitializableBehaviour | Awake() | 纯删 | 未分类_其他.tsv:587 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 91 | 泛型与启动链_补读.tsv:609 | SingletonInitializableBehaviour | OnInitializationComplete() | 纯删 | 未分类_其他.tsv:589 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 92 | 泛型与启动链_补读.tsv:608 | SingletonBehaviour | OnDestroy() | 回填后删 | 未分类_其他.tsv:586 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 93 | 泛型与启动链_补读.tsv:607 | SingletonBehaviour | Awake() | 回填后删 | 未分类_其他.tsv:585 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 94 | 泛型与启动链_补读.tsv:605 | ReferenceList | LoadReferences() | 回填后删 | 未分类_其他.tsv:513 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 95 | 泛型与启动链_补读.tsv:602 | Points | GetID() | 纯删 | 未分类_其他.tsv:491 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 96 | 泛型与启动链_补读.tsv:600 | MultiKeyDictionary | ContainsKey(T1 key1, T2 key2) | 回填后删 | 未分类_其他.tsv:450 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 97 | 泛型与启动链_补读.tsv:599 | ItemInfo | AssignEphemeralState(EphemeralState rewardEphemeralState) | 纯删 | 未分类_其他.tsv:374 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 98 | 泛型与启动链_补读.tsv:598 | ItemInfo | SetAmount(int amount) | 纯删 | 未分类_其他.tsv:376 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 99 | 泛型与启动链_补读.tsv:597 | ItemInfo | Add(int amount, string instanceId) | 纯删 | 未分类_其他.tsv:373 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 100 | 泛型与启动链_补读.tsv:596 | ItemInfo | SetInfo(LoadableReference<T> setItem, int setQty, string instanceId) | 纯删 | 未分类_其他.tsv:377 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 101 | 泛型与启动链_补读.tsv:595 | ItemInfo | Convert<TOut>() | 纯删 | 未分类_其他.tsv:375 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 102 | 泛型与启动链_补读.tsv:593 | IdentifiableWrapper | GetID() | 回填后删 | 未分类_其他.tsv:362 | O B87-C4 给出逐句体=等价，采纳 A |
| 103 | 泛型与启动链_补读.tsv:583 | Decorator | OnObjectUpdated(T decorable) | 纯删 | 未分类_其他.tsv:245 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 104 | 泛型与启动链_补读.tsv:582 | Decorator | OnDisable() | 回填后删 | 未分类_其他.tsv:243 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 105 | 泛型与启动链_补读.tsv:581 | Decorator | OnEnable() | 回填后删 | 未分类_其他.tsv:244 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 106 | 泛型与启动链_补读.tsv:580 | Decorator | OnDecorableUpdated(object sender, EventArgs e) | 回填后删 | 未分类_其他.tsv:242 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 107 | 泛型与启动链_补读.tsv:579 | Decorator | Awake() | 回填后删 | 未分类_其他.tsv:241 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 108 | 泛型与启动链_补读.tsv:578 | ConfigProvider | OnDestroy() | 回填后删 | 未分类_其他.tsv:178 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 109 | 泛型与启动链_补读.tsv:577 | ConfigProvider | UnpackInternal(Dictionary<string, string> titleData) | 回填后删 | 未分类_其他.tsv:179 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 110 | 泛型与启动链_补读.tsv:576 | ConfigProvider | Initialize(Dictionary<string, string> titleData) | 回填后删 | 未分类_其他.tsv:177 | O B91-Q2「升A 等价实现」，采纳 A |
| 111 | 泛型与启动链_补读.tsv:575 | ComponentReference | Load() | 回填后删 | 未分类_其他.tsv:171 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 112 | 泛型与启动链_补读.tsv:574 | ComponentReference | ReleaseAsset() | 回填后删 | 未分类_其他.tsv:174 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 113 | 泛型与启动链_补读.tsv:573 | ComponentReference | Release(AddressableCacheGroup cacheGroup) | 回填后删 | 未分类_其他.tsv:173 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 114 | 泛型与启动链_补读.tsv:572 | ComponentReference | ValidateAsset(string path) | 回填后删 | 未分类_其他.tsv:176 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 115 | 泛型与启动链_补读.tsv:571 | ComponentReference | ValidateAsset(UnityEngine.Object obj) | 回填后删 | 未分类_其他.tsv:175 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 116 | 泛型与启动链_补读.tsv:570 | ComponentReference | GameObjectReady(AsyncOperationHandle<GameObject> arg) | 回填后删 | 未分类_其他.tsv:168 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 117 | 泛型与启动链_补读.tsv:569 | ComponentReference | LoadAssetAsync() | 回填后删 | 未分类_其他.tsv:172 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 118 | 泛型与启动链_补读.tsv:568 | ComponentReference | InstantiateAsync(Transform parent, bool instantiateInWorldSpace) | 回填后删 | 未分类_其他.tsv:169 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 119 | 泛型与启动链_补读.tsv:567 | ComponentReference | InstantiateAsync(Vector3 position, Quaternion rotation, Transform parent) | 回填后删 | 未分类_其他.tsv:170 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 120 | 泛型与启动链_补读.tsv:565 | Cache | ForceChangedValueNotification() | 回填后删 | 未分类_其他.tsv:105 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 121 | 泛型与启动链_补读.tsv:564 | BiDirectionalDictionary | TryGetValue(TKey key, out TValue value) | 回填后删 | 未分类_其他.tsv:89 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 122 | 泛型与启动链_补读.tsv:563 | BiDirectionalDictionary | Remove(KeyValuePair<TKey, TValue> item) | 回填后删 | 未分类_其他.tsv:86 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 123 | 泛型与启动链_补读.tsv:562 | BiDirectionalDictionary | Remove(TKey key) | 回填后删 | 未分类_其他.tsv:87 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 124 | 泛型与启动链_补读.tsv:561 | BiDirectionalDictionary | GetEnumerator() | 回填后删 | 未分类_其他.tsv:83 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 125 | 泛型与启动链_补读.tsv:560 | BiDirectionalDictionary | CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) | 回填后删 | 未分类_其他.tsv:82 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 126 | 泛型与启动链_补读.tsv:559 | BiDirectionalDictionary | ContainsValue(TValue value) | 回填后删 | 未分类_其他.tsv:81 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 127 | 泛型与启动链_补读.tsv:558 | BiDirectionalDictionary | ContainsKey(TKey key) | 回填后删 | 未分类_其他.tsv:80 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 128 | 泛型与启动链_补读.tsv:557 | BiDirectionalDictionary | Contains(KeyValuePair<TKey, TValue> item) | 回填后删 | 未分类_其他.tsv:79 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 129 | 泛型与启动链_补读.tsv:556 | BiDirectionalDictionary | Clear() | 回填后删 | 未分类_其他.tsv:78 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 130 | 泛型与启动链_补读.tsv:555 | BiDirectionalDictionary | Add(KeyValuePair<TKey, TValue> item) | 回填后删 | 未分类_其他.tsv:76 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 131 | 泛型与启动链_补读.tsv:554 | BiDirectionalDictionary | Add(TKey key, TValue value) | 回填后删 | 未分类_其他.tsv:77 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 132 | 泛型与启动链_补读.tsv:553 | BiDirectionalDictionary | GetValue(TKey key) | 回填后删 | 未分类_其他.tsv:85 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 133 | 泛型与启动链_补读.tsv:552 | BiDirectionalDictionary | GetKey(TValue value) | 回填后删 | 未分类_其他.tsv:84 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 134 | 泛型与启动链_补读.tsv:551 | BiDirectionalDictionary | TryGetKey(TValue value, out TKey key) | 回填后删 | 未分类_其他.tsv:88 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 135 | 泛型与启动链_补读.tsv:550 | BaseProgressBarScaleAndPositionController | SetPositionAndScale(Vector2 positionToModify, Vector2 scaleToModify, TState stateToApply, Vector2 positionOffset, Vector2 scaleOffset) | 回填后删 | 未分类_其他.tsv:75 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 136 | 泛型与启动链_补读.tsv:549 | BaseProgressBarScaleAndPositionController | Initialize(TState currentState, TState previousState, int level) | 回填后删 | 未分类_其他.tsv:74 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 137 | 泛型与启动链_补读.tsv:548 | AssetReferenceTyped | Release(AddressableCacheGroup cacheGroup, bool releaseAll) | 回填后删 | 未分类_其他.tsv:45 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 138 | 泛型与启动链_补读.tsv:547 | AssetReferenceTyped | Load(AddressableCacheGroup cacheGroup) | 回填后删 | 未分类_其他.tsv:44 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 139 | 泛型与启动链_补读.tsv:546 | AssetReferenceTyped | ValidateAsset(string path) | 回填后删 | 未分类_其他.tsv:47 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 140 | 泛型与启动链_补读.tsv:545 | AssetReferenceTyped | ValidateAsset(UnityEngine.Object obj) | 回填后删 | 未分类_其他.tsv:46 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 141 | 泛型与启动链_补读.tsv:544 | AssetProvider | GetAssets() | 回填后删 | 未分类_其他.tsv:43 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 142 | 泛型与启动链_补读.tsv:543 | AssetProvider | Awake() | 回填后删 | 未分类_其他.tsv:42 | O 行 B91-Q2「升A 等价实现」晚于 T 的 R1/D，采纳 A |
| 143 | 泛型与启动链_补读.tsv:542 | ArmyItem | TryRelease(CardArmy armyToRelease) | 回填后删 | 未分类_其他.tsv:40 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 144 | 泛型与启动链_补读.tsv:541 | ArmyItem | Release(AddressableCacheGroup cacheGroup) | 回填后删 | 未分类_其他.tsv:38 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 145 | 泛型与启动链_补读.tsv:540 | ArmyItem | TryGetItem(CardArmy cardArmy, out T returnItem) | 纯删 | 未分类_其他.tsv:39 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 146 | 泛型与启动链_补读.tsv:539 | AlternateArtStyleItem | TryRelease(AlternateArtSyle alternateArtSyle) | 回填后删 | 未分类_其他.tsv:29 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 147 | 泛型与启动链_补读.tsv:538 | AlternateArtStyleItem | Release(AddressableCacheGroup cacheGroup) | 回填后删 | 未分类_其他.tsv:27 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 148 | 泛型与启动链_补读.tsv:537 | AlternateArtStyleItem | TryGetItem(AlternateArtSyle alternateArtSyle, out T returnItem) | 回填后删 | 未分类_其他.tsv:28 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 149 | 泛型与启动链_补读.tsv:264 | SignalEventTrigger | Unregister() | 纯删 | 日常_奖励.tsv:704 | 同 G332 |
| 150 | 泛型与启动链_补读.tsv:263 | SignalEventTrigger | ValidateSignal(T signal) | 纯删 | 日常_奖励.tsv:705 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 151 | 泛型与启动链_补读.tsv:262 | SignalEventTrigger | TryTrigger(T signal) | 纯删 | 日常_奖励.tsv:703 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 152 | 泛型与启动链_补读.tsv:261 | SignalEventTrigger | Register(Action callback) | 纯删 | 日常_奖励.tsv:702 | T(B, 含 B93_N2「四源查不到体」最新判定) 较 O(B91-Q2 A) 新，保留 T |
| 153 | 泛型与启动链_补读.tsv:260 | ScoringEvent | GetState(ScoringMilestone milestone) | 纯删 | 日常_奖励.tsv:693 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 154 | 泛型与启动链_补读.tsv:259 | ScoringEvent | IsMilestoneCollected(ScoringMilestone milestone) | 纯删 | 日常_奖励.tsv:694 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 155 | 泛型与启动链_补读.tsv:258 | ScoringEvent | GetBaseData() | 纯删 | 日常_奖励.tsv:692 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 156 | 泛型与启动链_补读.tsv:255 | PlayEvent | UploadDefaultDeck() | 回填后删 | 日常_奖励.tsv:572 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 157 | 泛型与启动链_补读.tsv:254 | PlayEvent | SetDefaultDeck(CardDeck deck, bool upload) | 纯删 | 日常_奖励.tsv:569 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 158 | 泛型与启动链_补读.tsv:253 | PlayEvent | GetEventDeck(CardArmy army) | 纯删 | 日常_奖励.tsv:558 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 159 | 泛型与启动链_补读.tsv:252 | PlayEvent | SetCustomMatchOptions(RoomOptions options, int currentElo) | 纯删 | 日常_奖励.tsv:568 | T(A, B87-C1 原版基类空体) 较 O(R1 D) 新，保留 T |
| 160 | 泛型与启动链_补读.tsv:251 | PlayEvent | OnMatchDraw() | 纯删 | 日常_奖励.tsv:563 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 161 | 泛型与启动链_补读.tsv:250 | PlayEvent | OnMatchLost() | 回填后删 | 日常_奖励.tsv:566 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 162 | 泛型与启动链_补读.tsv:249 | PlayEvent | OnMatchWon() | 回填后删 | 日常_奖励.tsv:567 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 163 | 泛型与启动链_补读.tsv:248 | PlayEvent | ValidateEventDeck(CardDeck deck, bool validateOwnership) | 纯删 | 日常_奖励.tsv:573 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 164 | 泛型与启动链_补读.tsv:247 | PlayEvent | MatchFinished(RankedMatchResult result, Action<OnlineMatchResult> onCompleted) | 纯删 | 日常_奖励.tsv:562 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 165 | 泛型与启动链_补读.tsv:245 | PlayEvent | CancelMatch() | 回填后删 | 日常_奖励.tsv:552 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 166 | 泛型与启动链_补读.tsv:244 | PlayEvent | OnMatchFound(string otherPlayerName) | 回填后删 | 日常_奖励.tsv:565 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 167 | 泛型与启动链_补读.tsv:243 | PlayEvent | StartMatch() | 回填后删 | 日常_奖励.tsv:570 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 168 | 泛型与启动链_补读.tsv:242 | PlayEvent | Initialize() | 回填后删 | 日常_奖励.tsv:560 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 169 | 泛型与启动链_补读.tsv:240 | PlayEvent | GetBaseSave() | 回填后删 | 日常_奖励.tsv:556 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 170 | 泛型与启动链_补读.tsv:239 | PlayEvent | ChangeState(PlayEventSaveGame.JoinStates stateToChange) | 回填后删 | 日常_奖励.tsv:553 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 171 | 泛型与启动链_补读.tsv:238 | MultiKeyLiveOpsHandler | ExtractKey(string key, string value) | 回填后删 | 日常_奖励.tsv:533 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 172 | 泛型与启动链_补读.tsv:237 | MissionEvent | Expire(Action onComplete) | 纯删 | 日常_奖励.tsv:477 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 173 | 泛型与启动链_补读.tsv:236 | MissionEvent | ReleaseContainer() | 纯删 | 日常_奖励.tsv:487 | T(B, 含 B93 显式「记B不判D」) 较 O(B87-C2 D) 新，保留 T |
| 174 | 泛型与启动链_补读.tsv:235 | MissionEvent | UpdateSaveGame(LiveOpsEventPersistence save) | 纯删 | 日常_奖励.tsv:491 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 175 | 泛型与启动链_补读.tsv:234 | MissionEvent | GetBaseSave() | 回填后删 | 日常_奖励.tsv:479 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 176 | 泛型与启动链_补读.tsv:231 | MissionEvent | GetNextRefreshTime() | 回填后删 | 日常_奖励.tsv:481 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 177 | 泛型与启动链_补读.tsv:230 | MissionEvent | DisableChallenge(MissionChallenge challenge) | 回填后删 | 日常_奖励.tsv:476 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 178 | 泛型与启动链_补读.tsv:229 | MissionEvent | IsActive(MissionChallenge challenge) | 纯删 | 日常_奖励.tsv:485 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 179 | 泛型与启动链_补读.tsv:228 | MissionEvent | GetProgressFor(MissionChallenge challenge) | 回填后删 | 日常_奖励.tsv:482 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 180 | 泛型与启动链_补读.tsv:227 | MissionEvent | LocalizedDescription(MissionChallenge challenge) | 回填后删 | 日常_奖励.tsv:486 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 181 | 泛型与启动链_补读.tsv:225 | LiveOpsHandler | GetEvent(string id) | 纯删 | 日常_奖励.tsv:386 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 182 | 泛型与启动链_补读.tsv:224 | LiveOpsHandler | GetBlockedEvents() | 纯删 | 日常_奖励.tsv:385 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 183 | 泛型与启动链_补读.tsv:223 | LiveOpsHandler | GetEvents(bool includeBlockedEvents) | 纯删 | 日常_奖励.tsv:388 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 184 | 泛型与启动链_补读.tsv:222 | LiveOpsHandler | ResetEvents() | 纯删 | 日常_奖励.tsv:397 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 185 | 泛型与启动链_补读.tsv:221 | LiveOpsHandler | ClearEvents() | 纯删 | 日常_奖励.tsv:381 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 186 | 泛型与启动链_补读.tsv:220 | LiveOpsHandler | InitializeHandler() | 纯删 | 日常_奖励.tsv:391 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 187 | 泛型与启动链_补读.tsv:219 | LiveOpsHandler | SetupEvents(List<TEvent> allEvents) | 回填后删 | 日常_奖励.tsv:401 | O B91-Q2「升A + 落点 LiveOpsHandler.cs:287」，采纳 A |
| 188 | 泛型与启动链_补读.tsv:218 | LiveOpsHandler | BeforeRefresh() | 回填后删 | 日常_奖励.tsv:380 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 189 | 泛型与启动链_补读.tsv:217 | LiveOpsHandler | HandleBeforeRefresh() | 纯删 | 日常_奖励.tsv:390 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 190 | 泛型与启动链_补读.tsv:213 | LiveOpsHandler | ScheduleHandlerRefresh(DateTime refreshTime) | 纯删 | 日常_奖励.tsv:399 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 191 | 泛型与启动链_补读.tsv:212 | LiveOpsHandler | TryScheduleHandlerRefresh(long milis) | 纯删 | 日常_奖励.tsv:403 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 192 | 泛型与启动链_补读.tsv:211 | LiveOpsHandler | DeserializeSharedData(JObject sharedData) | 纯删 | 日常_奖励.tsv:382 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 193 | 泛型与启动链_补读.tsv:210 | LiveOpsHandler | QueueEventPopup(ILiveOps newEvent, PopupOptions options, Action<Action> eventAction) | 纯删 | 日常_奖励.tsv:392 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 194 | 泛型与启动链_补读.tsv:209 | LiveOpsHandler | ExpireEvent(ILiveOps newEvent, PopupOptions options, bool showExpirePopup, Action onComplete) | 纯删 | 日常_奖励.tsv:383 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 195 | 泛型与启动链_补读.tsv:208 | LiveOpsHandler | ShowTriggerEventPopup(ILiveOps newEvent, PopupOptions options, Action onComplete) | 纯删 | 日常_奖励.tsv:402 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 196 | 泛型与启动链_补读.tsv:207 | LiveOpsHandler | ScheduleEventPopups() | 纯删 | 日常_奖励.tsv:398 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 197 | 泛型与启动链_补读.tsv:206 | LiveOpsHandler | SetupEvents(List<ILiveOps> ops, List<ILiveOps> blockedEvents, JObject sharedData) | 纯删 | 日常_奖励.tsv:400 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 198 | 泛型与启动链_补读.tsv:205 | LiveOpsHandler | ValidateData(LiveopsHandlerData data) | 纯删 | 日常_奖励.tsv:405 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 199 | 泛型与启动链_补读.tsv:204 | LiveOpsHandler | RegisterManager(LiveOpsManager manager) | 纯删 | 日常_奖励.tsv:396 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 200 | 泛型与启动链_补读.tsv:203 | LiveOpsHandler | UpdateCachedSave(LiveOpsEventPersistence saveGame) | 纯删 | 日常_奖励.tsv:404 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 201 | 泛型与启动链_补读.tsv:202 | LiveOpsEventWindowWithTabs | InitializeWindow(ILiveOps liveOpsEvent) | 纯删 | 日常_奖励.tsv:377 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 202 | 泛型与启动链_补读.tsv:201 | LiveOpsEventWindowWithTabs | SetupData(object data, GameWindowOptions options) | 纯删 | 日常_奖励.tsv:379 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 203 | 泛型与启动链_补读.tsv:200 | LiveOpsEventWindow | SetupData(object data, GameWindowOptions options) | 纯删 | 日常_奖励.tsv:376 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 204 | 泛型与启动链_补读.tsv:199 | LiveOpsEventWindow | Close() | 纯删 | 日常_奖励.tsv:372 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 205 | 泛型与启动链_补读.tsv:198 | LiveOpsEventWindow | OnRefresh() | 纯删 | 日常_奖励.tsv:375 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 206 | 泛型与启动链_补读.tsv:197 | LiveOpsEventWindow | InitializeWindow(ILiveOps liveOpsEvent) | 纯删 | 日常_奖励.tsv:373 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 207 | 泛型与启动链_补读.tsv:196 | LiveOpsEvent | LiveOpsEvent<TSave, TData>.GetEndDate(bool useExpiration) | 回填后删 | 日常_奖励.tsv:330 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 208 | 泛型与启动链_补读.tsv:194 | LiveOpsEvent | LiveOpsEvent<TSave, TData>.ResetEventProgress() | 纯删 | 日常_奖励.tsv:352 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 209 | 泛型与启动链_补读.tsv:193 | LiveOpsEvent | LiveOpsEvent<TSave, TData>.UpdateSaveGame(LiveOpsEventPersistence saveGame) | 纯删 | 日常_奖励.tsv:359 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 210 | 泛型与启动链_补读.tsv:192 | LiveOpsEvent | UpdateData(LiveOpsEventData data) | 纯删 | 日常_奖励.tsv:357 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 211 | 泛型与启动链_补读.tsv:191 | LiveOpsEvent | CreateNewSave(TData data) | 纯删 | 日常_奖励.tsv:322 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 212 | 泛型与启动链_补读.tsv:190 | LiveOpsEvent | Validate(TSave save) | 回填后删 | 日常_奖励.tsv:360 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 213 | 泛型与启动链_补读.tsv:189 | LiveOpsEvent | ValidateEventData(LiveOpsEventData data, LiveOpsEventPersistence save, out TData formatedData, out TSave formatedSave) | 纯删 | 日常_奖励.tsv:361 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 214 | 泛型与启动链_补读.tsv:188 | LiveOpsEvent | LiveOpsEvent<TSave, TData>.LoadEventData(LiveOpsEventData data, LiveOpsEventPersistence saveGame, ILiveOpsHandler handler) | 纯删 | 日常_奖励.tsv:342 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 215 | 泛型与启动链_补读.tsv:187 | LiveOpsEvent | LiveOpsEvent<TSave, TData>.Initialize() | 纯删 | 日常_奖励.tsv:339 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 216 | 泛型与启动链_补读.tsv:186 | LiveOpsEvent | LiveOpsEvent<TSave, TData>.SelectEvent() | 回填后删 | 日常_奖励.tsv:355 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 217 | 泛型与启动链_补读.tsv:185 | LiveOpsEvent | LiveOpsEvent<TSave, TData>.GetBaseSave() | 纯删 | 日常_奖励.tsv:328 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 218 | 泛型与启动链_补读.tsv:182 | GameNewsEvent | GetBaseSave() | 回填后删 | 日常_奖励.tsv:282 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 219 | 泛型与启动链_补读.tsv:181 | GameNewsEvent | GetBaseData() | 回填后删 | 日常_奖励.tsv:281 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 220 | 泛型与启动链_补读.tsv:180 | GameNewsEvent | StartDate() | 回填后删 | 日常_奖励.tsv:287 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 221 | 泛型与启动链_补读.tsv:179 | GameNewsEvent | IsRead() | 回填后删 | 日常_奖励.tsv:285 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 222 | 泛型与启动链_补读.tsv:176 | FastModeBaseEvent | OnMatchDraw() | 纯删 | 日常_奖励.tsv:268 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 223 | 泛型与启动链_补读.tsv:175 | FastModeBaseEvent | OnMatchLost() | 纯删 | 日常_奖励.tsv:271 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 224 | 泛型与启动链_补读.tsv:174 | FastModeBaseEvent | OnMatchWon() | 纯删 | 日常_奖励.tsv:273 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 225 | 泛型与启动链_补读.tsv:173 | FastModeBaseEvent | RecordMatchFinished(int matchWon, int targetMilestone, int enemyMilestone, Action<OnlineMatchResult> callbackOnSuccess) | 纯删 | 日常_奖励.tsv:274 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 226 | 泛型与启动链_补读.tsv:172 | FastModeBaseEvent | GetBaseSave() | 纯删 | 日常_奖励.tsv:263 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 227 | 泛型与启动链_补读.tsv:171 | FastModeBaseEvent | GetSqlFilter(int maxRating, int minRating, string guildId, bool isRanked, IEnumerable<string> blacklistedUsers, int gamesLost) | 回填后删 | 日常_奖励.tsv:267 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 228 | 泛型与启动链_补读.tsv:170 | FastModeBaseEvent | SetCustomMatchOptions(RoomOptions options, int currentElo) | 纯删 | 日常_奖励.tsv:275 | T(A, 含 B93_N7 落点) 较 O(R1 D) 新，保留 T |
| 229 | 泛型与启动链_补读.tsv:169 | FastModeBaseEvent | GetEventDeck() | 纯删 | 日常_奖励.tsv:264 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 230 | 泛型与启动链_补读.tsv:168 | FastModeBaseEvent | GetBaseData() | 纯删 | 日常_奖励.tsv:262 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 231 | 泛型与启动链_补读.tsv:167 | FastModeBaseEvent | GetEventDeck(CardArmy army) | 纯删 | 日常_奖励.tsv:265 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 232 | 泛型与启动链_补读.tsv:166 | FastModeBaseEvent | GetArmyState(CardArmy army, IReadOnlyList<RankedScoreBoostEvent> extraBoosts) | 纯删 | 日常_奖励.tsv:260 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 233 | 泛型与启动链_补读.tsv:165 | FastModeBaseEvent | GetArmyStats(CardArmy army) | 纯删 | 日常_奖励.tsv:261 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 234 | 泛型与启动链_补读.tsv:164 | BaseEventProgressRewardDrawerController | ChangeState(TState currentState) | 回填后删 | 日常_奖励.tsv:10 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 235 | 泛型与启动链_补读.tsv:163 | BaseEventProgressRewardDrawerController | OverridePremiumBadge(Sprite sprite) | 纯删 | 日常_奖励.tsv:12 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 236 | 泛型与启动链_补读.tsv:161 | BaseEventProgressClaimButtonController | CollectionClick() | 纯删 | 日常_奖励.tsv:8 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 237 | 泛型与启动链_补读.tsv:160 | BaseEventProgressClaimButtonController | Initialize(TState currentState, CurrencyValue normalPrice, Action onCollect) | 纯删 | 日常_奖励.tsv:9 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 238 | 泛型与启动链_补读.tsv:136 | ToggleCollectionFilter | FillToggleList(List<CollectionFilterToggle> currentList, int listLimit) | 纯删 | 收藏.tsv:312 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 239 | 泛型与启动链_补读.tsv:135 | ToggleCollectionFilter | ClearFilters() | 回填后删 | 收藏.tsv:311 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 240 | 泛型与启动链_补读.tsv:133 | ToggleCollectionFilter | Filter(List<T> collection) | 回填后删 | 收藏.tsv:313 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 241 | 泛型与启动链_补读.tsv:131 | ToggleCollectionFilter | SetupFilter(CollectionFilterController<T> controller) | 回填后删 | 收藏.tsv:316 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 242 | 泛型与启动链_补读.tsv:130 | ItemDrawer | ItemDrawer<T>.HandlePointerClick() | 纯删 | 收藏.tsv:271 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 243 | 泛型与启动链_补读.tsv:129 | ItemDrawer | OnPointerClick(PointerEventData eventData) | 纯删 | 收藏.tsv:273 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 244 | 泛型与启动链_补读.tsv:124 | ItemDrawer | ItemDrawer<T>.OverrideOnClickAction(Action onClick) | 纯删 | 收藏.tsv:276 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 245 | 泛型与启动链_补读.tsv:122 | ItemDrawer | ItemDrawer<T>.TogglePremiumHighlight(bool toggle) | 回填后删 | 收藏.tsv:295 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 246 | 泛型与启动链_补读.tsv:121 | ItemDrawer | ItemDrawer<T>.OverridePremiumBadge(Sprite sprite) | 纯删 | 收藏.tsv:278 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 247 | 泛型与启动链_补读.tsv:120 | ItemDrawer | ItemDrawer<T>.ToggleClaimedWarning(bool toggle) | 纯删 | 收藏.tsv:289 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 248 | 泛型与启动链_补读.tsv:119 | ItemDrawer | ItemDrawer<T>.SetEphemeralDisplay(EphemeralState state) | 纯删 | 收藏.tsv:286 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 249 | 泛型与启动链_补读.tsv:118 | ItemDrawer | ItemDrawer<T>.SetConvertedItem(RewardInfo convertedTo) | 纯删 | 收藏.tsv:284 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 250 | 泛型与启动链_补读.tsv:117 | ItemDrawer | SetItemName(string newName) | 回填后删 | 收藏.tsv:287 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 251 | 泛型与启动链_补读.tsv:116 | ItemDrawer | Draw(ObtainableItem item, int amount, ItemDrawerOptions options) | 回填后删 | 收藏.tsv:265 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 252 | 泛型与启动链_补读.tsv:115 | ItemDrawer | ItemDrawer<T>.Refresh() | 纯删 | 收藏.tsv:280 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 253 | 泛型与启动链_补读.tsv:113 | Inventory | GetWhere(Func<T, bool> predicate) | 纯删 | 收藏.tsv:226 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 254 | 泛型与启动链_补读.tsv:112 | Inventory | Get(Func<T, bool> predicate) | 纯删 | 收藏.tsv:221 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 255 | 泛型与启动链_补读.tsv:111 | Inventory | Get(string id) | 纯删 | 收藏.tsv:223 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 256 | 泛型与启动链_补读.tsv:110 | Inventory | Get(IIdentifiable item, bool autoCreate) | 纯删 | 收藏.tsv:222 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 257 | 泛型与启动链_补读.tsv:109 | Inventory | GetItemInstances(bool useQuantity) | 纯删 | 收藏.tsv:224 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 258 | 泛型与启动链_补读.tsv:108 | Inventory | GetItems() | 纯删 | 收藏.tsv:225 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 259 | 泛型与启动链_补读.tsv:107 | Inventory | TryGetItem(Func<T, bool> predicate, out IItemInfo<T> item) | 纯删 | 收藏.tsv:231 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 260 | 泛型与启动链_补读.tsv:106 | Inventory | TryGetItem(string id, out IItemInfo<T> item) | 纯删 | 收藏.tsv:232 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 261 | 泛型与启动链_补读.tsv:105 | Inventory | Add(IIdentifiable item, int amount, string instanceId, Dictionary<string, string> customData, bool overrideAmount) | 纯删 | 收藏.tsv:219 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 262 | 泛型与启动链_补读.tsv:104 | Inventory | OnFinishUnpack() | 回填后删 | 收藏.tsv:227 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 263 | 泛型与启动链_补读.tsv:103 | Inventory | Create(IIdentifiable item, string instanceId, Dictionary<string, string> customData) | 纯删 | 收藏.tsv:220 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 264 | 泛型与启动链_补读.tsv:102 | Inventory | RegisterSubset(Type type, InventorySubset set) | 纯删 | 收藏.tsv:230 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 265 | 泛型与启动链_补读.tsv:101 | Inventory | OnItemCountModify(T item, int amount) | 纯删 | 收藏.tsv:228 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 266 | 泛型与启动链_补读.tsv:100 | Inventory | OnItemCountModifyInternal(IIdentifiable itemChanged, int amountChanged) | 纯删 | 收藏.tsv:229 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 267 | 泛型与启动链_补读.tsv:99 | Inventory | UpdateItemCollection() | 纯删 | 收藏.tsv:233 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 268 | 泛型与启动链_补读.tsv:90 | CollectionTab | GetCollection() | 纯删 | 收藏.tsv:179 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 269 | 泛型与启动链_补读.tsv:89 | CollectionTab | OnItemSelected(CollectionItem<T> item, PointerEventData.InputButton button) | 纯删 | 收藏.tsv:181 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 270 | 泛型与启动链_补读.tsv:88 | CollectionTab | OnOpen() | 纯删 | 收藏.tsv:182 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 271 | 泛型与启动链_补读.tsv:87 | CollectionTab | Setup(GameWindow window) | 纯删 | 收藏.tsv:183 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 272 | 泛型与启动链_补读.tsv:86 | CollectionTab | GetItems() | 纯删 | 收藏.tsv:180 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 273 | 泛型与启动链_补读.tsv:84 | CollectionItem | OnPointerEnter(PointerEventData eventData) | 纯删 | 收藏.tsv:159 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 274 | 泛型与启动链_补读.tsv:83 | CollectionItem | OnPointerExit(PointerEventData eventData) | 纯删 | 收藏.tsv:160 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 275 | 泛型与启动链_补读.tsv:82 | CollectionItem | SetSortMode(bool state) | 纯删 | 收藏.tsv:164 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 276 | 泛型与启动链_补读.tsv:81 | CollectionItem | HoverHighlight(bool state) | 纯删 | 收藏.tsv:150 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 277 | 泛型与启动链_补读.tsv:80 | CollectionItem | SelectedHighlight(bool state) | 纯删 | 收藏.tsv:163 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 278 | 泛型与启动链_补读.tsv:79 | CollectionItem | OnDeselect() | 回填后删 | 收藏.tsv:152 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 279 | 泛型与启动链_补读.tsv:78 | CollectionItem | OnSelect() | 回填后删 | 收藏.tsv:161 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 280 | 泛型与启动链_补读.tsv:77 | CollectionItem | OnEndDrag(PointerEventData eventData) | 纯删 | 收藏.tsv:156 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 281 | 泛型与启动链_补读.tsv:76 | CollectionItem | OnDrag(PointerEventData eventData) | 纯删 | 收藏.tsv:154 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 282 | 泛型与启动链_补读.tsv:75 | CollectionItem | OnBeginDrag(PointerEventData eventData) | 纯删 | 收藏.tsv:151 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 283 | 泛型与启动链_补读.tsv:74 | CollectionItem | OnPointerClick(PointerEventData eventData) | 纯删 | 收藏.tsv:158 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 284 | 泛型与启动链_补读.tsv:73 | CollectionItem | AnimateIn() | 纯删 | 收藏.tsv:147 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 285 | 泛型与启动链_补读.tsv:72 | CollectionItem | Config(T item, GameplayVariablesData gameplayVariablesData) | 纯删 | 收藏.tsv:149 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 286 | 泛型与启动链_补读.tsv:71 | CollectionItem | ToggleListenToPointerEvents(bool option) | 回填后删 | 收藏.tsv:167 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 287 | 泛型与启动链_补读.tsv:70 | CollectionItem | OnDisable() | 回填后删 | 收藏.tsv:153 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 288 | 泛型与启动链_补读.tsv:69 | CollectionItem | OnEndDragEvent(Transform obj) | 纯删 | 收藏.tsv:157 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 289 | 泛型与启动链_补读.tsv:68 | CollectionItem | OnStartDragEvent(Transform obj) | 纯删 | 收藏.tsv:162 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 290 | 泛型与启动链_补读.tsv:67 | CollectionItem | ToggleDrag(bool state) | 纯删 | 收藏.tsv:166 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 291 | 泛型与启动链_补读.tsv:66 | CollectionItem | OnEnable() | 回填后删 | 收藏.tsv:155 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 292 | 泛型与启动链_补读.tsv:65 | CollectionItem | Config(T item, CollectionDisplay<T> display, bool animate, bool isSelected) | 纯删 | 收藏.tsv:148 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 293 | 泛型与启动链_补读.tsv:63 | CollectionFilterController | ClearFilters() | 纯删 | 收藏.tsv:137 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 294 | 泛型与启动链_补读.tsv:62 | CollectionFilterController | Filter(List<T> collection) | 纯删 | 收藏.tsv:138 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 295 | 泛型与启动链_补读.tsv:61 | CollectionFilterController | NotifyFilterChange() | 纯删 | 收藏.tsv:139 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 296 | 泛型与启动链_补读.tsv:60 | CollectionFilterController | SetupFilters() | 纯删 | 收藏.tsv:140 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 297 | 泛型与启动链_补读.tsv:59 | CollectionFilterController | Toggle(bool option, bool withAnimation) | 纯删 | 收藏.tsv:141 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 298 | 泛型与启动链_补读.tsv:58 | CollectionFilter | FillToggleList(List<CollectionFilterToggle> currentList, int maxElements) | 纯删 | 收藏.tsv:132 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 299 | 泛型与启动链_补读.tsv:57 | CollectionFilter | ClearFilters() | 纯删 | 收藏.tsv:131 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 300 | 泛型与启动链_补读.tsv:56 | CollectionFilter | Filter(List<T> collection) | 纯删 | 收藏.tsv:133 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 301 | 泛型与启动链_补读.tsv:55 | CollectionFilter | NotifyChange() | 纯删 | 收藏.tsv:134 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 302 | 泛型与启动链_补读.tsv:54 | CollectionFilter | OnFilterChange() | 纯删 | 收藏.tsv:135 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 303 | 泛型与启动链_补读.tsv:53 | CollectionFilter | SetupFilter(CollectionFilterController<T> controller) | 纯删 | 收藏.tsv:136 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 304 | 泛型与启动链_补读.tsv:52 | CollectionDisplay | OnListDrag(Vector2 listPos) | 纯删 | 收藏.tsv:121 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 305 | 泛型与启动链_补读.tsv:51 | CollectionDisplay | GetItemObject(T item) | 纯删 | 收藏.tsv:115 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 306 | 泛型与启动链_补读.tsv:50 | CollectionDisplay | Select(T item) | 回填后删 | 收藏.tsv:127 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 307 | 泛型与启动链_补读.tsv:49 | CollectionDisplay | Select(CollectionItem<T> collectable, PointerEventData.InputButton button) | 纯删 | 收藏.tsv:126 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 308 | 泛型与启动链_补读.tsv:48 | CollectionDisplay | RefreshCells() | 纯删 | 收藏.tsv:122 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 309 | 泛型与启动链_补读.tsv:47 | CollectionDisplay | ReorderCollectionByHierarchy(Transform transformToChangeIndex) | 纯删 | 收藏.tsv:124 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 310 | 泛型与启动链_补读.tsv:46 | CollectionDisplay | SetCell(ICell cell, int index) | 纯删 | 收藏.tsv:128 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 311 | 泛型与启动链_补读.tsv:45 | CollectionDisplay | GetItemCount() | 纯删 | 收藏.tsv:114 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 312 | 泛型与启动链_补读.tsv:44 | CollectionDisplay | RefreshCollection() | 纯删 | 收藏.tsv:123 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 313 | 泛型与启动链_补读.tsv:43 | CollectionDisplay | ToggleFilters(bool state) | 纯删 | 收藏.tsv:129 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 314 | 泛型与启动链_补读.tsv:42 | CollectionDisplay | UpdateCollection(List<T> collection) | 纯删 | 收藏.tsv:130 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 315 | 泛型与启动链_补读.tsv:41 | CollectionDisplay | ResetRelayTarget() | 回填后删 | 收藏.tsv:125 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 316 | 泛型与启动链_补读.tsv:40 | CollectionDisplay | OnEndDragEvent(Transform target) | 纯删 | 收藏.tsv:120 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 317 | 泛型与启动链_补读.tsv:39 | CollectionDisplay | OnDisable() | 回填后删 | 收藏.tsv:118 | O B91-Q2「升A 等价实现」，采纳 A |
| 318 | 泛型与启动链_补读.tsv:38 | CollectionDisplay | OnEnable() | 回填后删 | 收藏.tsv:119 | O B91-Q2「升A 等价实现」，采纳 A |
| 319 | 泛型与启动链_补读.tsv:37 | CollectionDisplay | GetItems() | 纯删 | 收藏.tsv:116 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 320 | 泛型与启动链_补读.tsv:36 | CollectionDisplay | Initialize(List<T> collection, GameplayVariablesData gameplayVariablesData) | 纯删 | 收藏.tsv:117 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 321 | 泛型与启动链_补读.tsv:35 | ShopStore | GetPurchaseCount(string id) | 纯删 | 商店.tsv:294 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 322 | 泛型与启动链_补读.tsv:34 | ShopStore | TryPurchase(IShopOffer offer, Action onComplete, Action onError) | 回填后删 | 商店.tsv:297 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 323 | 泛型与启动链_补读.tsv:20 | LoadableReference | SetReferenceObjEditor(UnityEngine.Object obj) | 回填后删 | 其他_基础设施.tsv:300 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 324 | 泛型与启动链_补读.tsv:19 | LoadableReference | OnBeforeRead(JToken rawJson) | 纯删 | 其他_基础设施.tsv:296 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 325 | 泛型与启动链_补读.tsv:18 | LoadableReference | WriteJson() | 纯删 | 其他_基础设施.tsv:303 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 326 | 泛型与启动链_补读.tsv:17 | LoadableReference | OnBeforeWrite() | 纯删 | 其他_基础设施.tsv:297 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 327 | 泛型与启动链_补读.tsv:16 | LoadableReference | GetHashCode() | 纯删 | 其他_基础设施.tsv:290 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 328 | 泛型与启动链_补读.tsv:15 | LoadableReference | Equals(object obj) | 纯删 | 其他_基础设施.tsv:289 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 329 | 泛型与启动链_补读.tsv:14 | LoadableReference | Equals(LoadableReference<T> other) | 纯删 | 其他_基础设施.tsv:288 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 330 | 泛型与启动链_补读.tsv:7 | LoadableReference | ValidateReferences() | 回填后删 | 其他_基础设施.tsv:302 | 状态一致(D/D)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 331 | 泛型与启动链_补读.tsv:6 | LoadableReference | SetReferenceByObject(T reference) | 纯删 | 其他_基础设施.tsv:299 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 332 | 泛型与启动链_补读.tsv:5 | LoadableReference | SetReferenceById(string id) | 纯删 | 其他_基础设施.tsv:298 | 状态一致(A/A),generic 行为纯副本(Q13=删) |
| 333 | 泛型与启动链_补读.tsv:4 | RankedEventBase | ToggleRankedMode(bool isRanked) | 回填后删 | 个人资料_成就_排行.tsv:213 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 334 | 泛型与启动链_补读.tsv:3 | RankedEventBase | GetBaseData() | 回填后删 | 个人资料_成就_排行.tsv:211 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |
| 335 | 泛型与启动链_补读.tsv:2 | RankedEventBase | GetArmyState(CardArmy army, IReadOnlyList<RankedScoreBoostEvent> extraBoosts) | 回填后删 | 个人资料_成就_排行.tsv:210 | 状态一致(A/A)但 generic 行有独有 差异/备注,先并入 T 再删(Q13=并) |

## §三 不是重复（0 组）

按要求逐一排除了本仓踩过的三类「假重复」，**均未命中**：

1. **泛型表跨域同名**（`泛型与启动链_补读.tsv` 装着 10 个域的行）：349 组「域表↔泛型表」里，两行的 **原版类名（去 `<T>` 后）+ 方法名 + 参数个数 全等**，绝大多少 `判据出处` 指向**同一 `文件:行`**。⇒ 同一方法被两表各记一次，**不是**跨域同名两方法。（含 LoginManager 等**非泛型类**混进补读表的情形，已抽验同 `class+method+源码行`，确认是重复。）
2. **重载被误并**：按 `/参数个数` 分组。逐一比对 50 处「签名写得不完全一样」的组，**全是格式差异**（有无 `= 默认值` / `UnityEngine.Object` vs `Object` / 类名带不带 `ItemDrawer<T>.` 前缀 / 泛型方法 `Get<T1>` vs `Get`），**无一处真·不同参数类型**。两个 `DynamicList.Get` 重载（按元素 行228 / 按下标 行269）分属 **G144 / G145 两个不同组**，未被并到一起。
3. **同名类同时有泛型版与非泛型版**（`DynamicList`·`UiContainer`·`ItemDrawer`·`LiveOpsEvent`·`ItemInfo`·`Points`·`WindowTabBase`）：回反编译核实——**非泛型那支是抽象基类/空壳**（如 `DynamicList` 抽象基类在 `out_cpp2il_dev_ilspy/DynamicList.cs:642` 只声明 `abstract ListType/GetIndexType`；具体 `Get<T1>` 在 `DynamicList<T>` 行 228/269）。域表行类名写成短名 `DynamicList`，指的**仍是 `<T>` 类同一具体方法** ⇒ 真重复。

**另**：353 组里**没有**「方法名带 IL2CPP 转义 `_003C_…_003E_` 的 lambda、且父方法不同」那种假重复 —— 同名 lambda（如 `_003CThrowCustomError_003Eb__43_0`）两行父方法+序号+源码行全等 = 同一 lambda。

## §四 判不了（18 组 · 差什么）

以下**都是同一原版方法的两行重复**（方法身份已核实），但**两行等级相反/互斥**、且 ledger 轮次无法判谁更晚 ⇒ **删任一都会把另一等级焊死**，暂不删，需调度台裁定保留行采用哪个等级。

| 组 | 原版类.方法 | 域表行(状态) | 泛型/旧行(状态) | 差什么 |
|---|---|---|---|---|
| G123 | DraftEventBase.OnMatchFound | 轮抽.tsv:19（D） | 泛型与启动链_补读.tsv:748（A） | T(D『断在服务端』) vs O(A B87-C2 给出 base.OnMatchFound 体)，两边都有据、无明确新旧 |
| G186 | ItemDrawer.Awake | 收藏.tsv:264（A） | 泛型与启动链_补读.tsv:114（D） | T(A R5『按 style 才建,出厂态相同』) vs O(D B87-R20『实读=setter 传 null』)，A/D 冲突 |
| G188 | ItemDrawer.Draw | 收藏.tsv:267（D） | 泛型与启动链_补读.tsv:126（A） | T(D『abstract Draw,行为散在23子类』) vs O(A『一致』)—— abstract/空体该记 A 还是 D 属口径，需仲裁 |
| G198 | ItemDrawer.ToggleHighlight | 收藏.tsv:291（D） | 泛型与启动链_补读.tsv:127（A） | T(D『0 调用点死代码』) vs O(A『一致,已落 ItemDrawerView』)，死代码该记 A 还是 D 需口径 |
| G199 | ItemDrawer.ToggleLock | 收藏.tsv:293（D） | 泛型与启动链_补读.tsv:128（A） | 同 G198 |
| G201 | ItemDrawer.ToggleShowInfoOnClick | 收藏.tsv:296（D） | 泛型与启动链_补读.tsv:123（A） | 同 G198 |
| G208 | LiveOpsEvent.GetBaseData | 日常_奖励.tsv:326（A） | 泛型与启动链_补读.tsv:184（D） | T(A『等价』) vs O(D『替代…退B』)，A/D/B 三说并存 |
| G211 | LiveOpsEvent.GetID | 日常_奖励.tsv:334（A） | 泛型与启动链_补读.tsv:183（D） | 同 G208 |
| G215 | LiveOpsEvent.SaveEventProgress | 日常_奖励.tsv:353（D） | 泛型与启动链_补读.tsv:195（A） | T(D『基类空虚方法』) vs O(A『子类 override,本地 LocalLiveOpsEvent 写』)，D/A 冲突 |
| G291 | MissionEvent.GetBaseData | 日常_奖励.tsv:478（A） | 泛型与启动链_补读.tsv:233（D） | 同 G208 |
| G298 | MissionEvent.RerollChallenge | 日常_奖励.tsv:488（B） | 泛型与启动链_补读.tsv:232（A） | T(B『别名,本体在 RerollDaily』) vs O(A『一致』)，B/A 冲突 |
| G304 | PlayEvent.GetBaseData | 日常_奖励.tsv:555（A） | 泛型与启动链_补读.tsv:241（D） | 同 G208 |
| G310 | PlayEvent.OnMatchFinished | 日常_奖励.tsv:564（D） | 泛型与启动链_补读.tsv:246（A） | T(D『基类空虚方法』) vs O(A『原版 virtual 空体』)，两者同述空体却 A/D 相反 |
| G319 | Points.Collect | 未分类_其他.tsv:490（A） | 泛型与启动链_补读.tsv:601（D） | T(A『VA 反汇编=等价实现』) vs O(D『不做真实经济』)，A/D 冲突 |
| G321 | QuantityBoundItemInfo.Add | 未分类_其他.tsv:498（D） | 泛型与启动链_补读.tsv:603（A） | T(D) vs O(A『原版 Add=空方法,账原来写错』)—— 空方法该记 A 还是 D 属口径，需仲裁 |
| G329 | ShopStore.GetBaseData | 商店.tsv:292（A） | 泛型与启动链_补读.tsv:33（D） | 同 G208 |
| G344 | ToggleCollectionFilter.FilterItem | 收藏.tsv:314（A） | 泛型与启动链_补读.tsv:134（D） | T(A『同类多选已对齐』) vs O(D『原版本行无体,子类有判据』)，A/D 冲突 |
| G346 | TutorialPointer.StartPointer | 教程_引导.tsv:94（A） | 特效动画.tsv:524（B） | 跨域(教程 vs 特效)；O(特效:524, B93) 更新且状态 B 带新发现(双点口未建),与 T(A) 冲突 ⇒ 需定归属 |

根因三类：
- **空/抽象/死方法的 A-vs-D 口径**（G123/G186/G188/G198/G199/G201/G215/G310/G321/G344）：原版方法本身为空体/抽象/0 调用点。「原版死代码但我们有等价实现」记 **A** 还是 **D**，全仓口径未统一。
- **本地替代物算 A 还是 D**（G208/G211/G291/G298/G304/G319/G329）：原版是远端数据 getter，本地有替代表。域表记 A（等价）、泛型表记 D（离线不适用/退B）——「本地模拟」该记 A 还是 D 需口径。
- **跨域归属**（G346）：`TutorialPointer.StartPointer` 同时被 `教程_引导`（A）与 `特效动画`（B，B93 带「双点口未建」新发现）两域记录，需定归哪张表。

## §五 删序提醒（动手时按此顺序）

1. 🔴 **先做全部「回填」编辑，再做「删除」**。回填目标是**保留行**（多在别的域表），只要同一份文件里既有回填目标又有删除，删除就会移动行号。稳妥 = ① 先把 §二「回填后删」内容并进各自保留行；② 再按下面降序删。
2. 🔴 **同一账文件内【从大行号往小删】**（否则删完小行号，后面的大行号就过期了）。

### `日常_奖励.tsv` —— 需删 3 行

降序执行：`783`, `782`, `42`

### `泛型与启动链_补读.tsv` —— 需删 332 行

降序执行：`799`, `798`, `797`, `796`, `795`, `794`, `793`, `792`, `791`, `790`, `789`, `788`, `787`, `786`, `785`, `784`, `783`, `782`, `781`, `780`, `779`, `778`, `777`, `776`, `775`, `774`, `773`, `772`, `771`, `770`, `769`, `768`, `764`, `763`, `762`, `761`, `760`, `759`, `757`, `756`, `755`, `754`, `753`, `752`, `751`, `750`, `749`, `747`, `746`, `744`, `743`, `742`, `741`, `736`, `735`, `734`, `733`, `732`, `731`, `730`, `729`, `728`, `727`, `726`, `725`, `723`, `722`, `721`, `720`, `719`, `718`, `717`, `624`, `623`, `622`, `621`, `620`, `619`, `618`, `617`, `616`, `615`, `614`, `613`, `612`, `611`, `610`, `609`, `608`, `607`, `605`, `602`, `600`, `599`, `598`, `597`, `596`, `595`, `593`, `583`, `582`, `581`, `580`, `579`, `578`, `577`, `576`, `575`, `574`, `573`, `572`, `571`, `570`, `569`, `568`, `567`, `565`, `564`, `563`, `562`, `561`, `560`, `559`, `558`, `557`, `556`, `555`, `554`, `553`, `552`, `551`, `550`, `549`, `548`, `547`, `546`, `545`, `544`, `543`, `542`, `541`, `540`, `539`, `538`, `537`, `264`, `263`, `262`, `261`, `260`, `259`, `258`, `255`, `254`, `253`, `252`, `251`, `250`, `249`, `248`, `247`, `245`, `244`, `243`, `242`, `240`, `239`, `238`, `237`, `236`, `235`, `234`, `231`, `230`, `229`, `228`, `227`, `225`, `224`, `223`, `222`, `221`, `220`, `219`, `218`, `217`, `213`, `212`, `211`, `210`, `209`, `208`, `207`, `206`, `205`, `204`, `203`, `202`, `201`, `200`, `199`, `198`, `197`, `196`, `194`, `193`, `192`, `191`, `190`, `189`, `188`, `187`, `186`, `185`, `182`, `181`, `180`, `179`, `176`, `175`, `174`, `173`, `172`, `171`, `170`, `169`, `168`, `167`, `166`, `165`, `164`, `163`, `161`, `160`, `136`, `135`, `133`, `131`, `130`, `129`, `124`, `122`, `121`, `120`, `119`, `118`, `117`, `116`, `115`, `113`, `112`, `111`, `110`, `109`, `108`, `107`, `106`, `105`, `104`, `103`, `102`, `101`, `100`, `99`, `90`, `89`, `88`, `87`, `86`, `84`, `83`, `82`, `81`, `80`, `79`, `78`, `77`, `76`, `75`, `74`, `73`, `72`, `71`, `70`, `69`, `68`, `67`, `66`, `65`, `63`, `62`, `61`, `60`, `59`, `58`, `57`, `56`, `55`, `54`, `53`, `52`, `51`, `50`, `49`, `48`, `47`, `46`, `45`, `44`, `43`, `42`, `41`, `40`, `39`, `38`, `37`, `36`, `35`, `34`, `20`, `19`, `18`, `17`, `16`, `15`, `14`, `7`, `6`, `5`, `4`, `3`, `2`

### 特殊：`日常_奖励.tsv` 的 3 行（G041/G042/G043）

- **G043 = 回填后删**：先把 `日常_奖励.tsv:42` 更新的 `B93_E2 A→B` 降档内容并进保留行 `日常_奖励.tsv:781`，**再**删 `:42`。
- 顺序：**删 `783`（G041，保留 `:34`）→ 删 `782`（G042，保留 `:37`）→ 回填 `:781`（G043）→ 删 `42`（G043）**。
　（`:783`/`:782` 都 > `:781`，先删不影响 `:781` 行号；回填 `:781` 必须在删 `:42` 之前，因为 `:42 < :781`。）

---

### 附：本次核实的方法论与取证
- **行号**：706 行全部回主表现读、706/706 在原行号命中 ⇒ 无漂移。若日后主表再动，需重跑本核实。
- **方法身份**：比对 `原版类(去泛型) + 方法名 + 参数个数 + 判据出处`；50 处签名差异全为格式；跨表同名经反编译核实为同一具体方法（见 §三）。
- **等级冲突**：逐组读两行的 `状态/判据要点/差异/备注/轮次`，按「谁带显式改判标记、谁更晚」定型；无法定型者入 §四，**不猜**。
- **未核实**：本代理**未**逐组重读反编译方法体核「判据要点」内容对错（那是判据审查线的活）；此处只核「是不是同一方法的重复 + 删哪个不丢状态」。
