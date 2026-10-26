# B9 · K2DL —— 日常/奖励域 C 类 282 条分类与施工单

> 2026-10-26（第八十四会话）· **只读判据代理 K2DL**。
> 范围：`Unity/资料/完全复刻/账/日常_奖励.tsv` 里**状态 = C 的 282 行**（现读，不是估；C 是全项目最大的一坨）。
> 产出：`判据/B9_日常C_分类.tsv`（282 行）· `判据/B9_日常C_施工单.tsv`（① 类 258 行 · 12 列，表头与 `B9_剩余C_施工单.tsv` **逐字一致**）· `判据/B9_日常C_派活包.md`（15 个写手包 + 共享瓶颈 + 依赖分层）。
> **本轮一行 `.cs` / 一个资产 / 一条账都没改**（红线）；没跑 Unity、没动 git。

---

## ① 结论（含三类计数）

### 1·1 三类计数（282 = 全覆盖，无遗漏）

| 分类 | 条数 | 占比 | 含义 |
|---|---:|---:|---|
| **① 可真做** | **258** | 91.5% | 原版有可复刻的客户端行为（算法/状态机/UI 流程/本地存档），离线可做，落点已定位或能新建 ⇒ 已全部进施工单 |
| **② P3 候选** | **7** | 2.5% | 全部内容就是「调服务器 / 查远端配置」或消费方在服务器层；**只给证据 + 时间限定，⛔ 没判 D** |
| **③ 判不了 / 别域 / 非缺口** | **17** | 6.0% | 含 **4 行账已过期（已落地但仍记 C）**、3 行别域、3 行架构差异、2 行已定口径排除、1 行连带已判 D、1 行抽象声明、2 行编辑器专用、1 行判据未定 |

按等级分布（等级取自账本，不是新判）：**① P1 33 · P2 225**｜② P1 5 · P2 2｜③ P1 3 · P2 14。

### 1·2 为什么 91.5% 是「可真做」—— 三个判断依据

1. **活动线框架已经在了**：`Shell/LiveOpsEvent.cs`(745) · `LiveOpsHandler.cs`(1407) · `LiveOpsManager.cs`(356) · `LiveOpsEventFactory.cs`(183) · `LiveOpsSharedData.cs`(414) 由 E2 建完（38 条 P1 → A 20/B 18/C 0）。所以这一批 C 不再是「整层没有」，而是**框架上的接线与窗内数据层**。
2. **判据真身齐**：本批涉及的类在 `d:/2/tools/decomp_full/` 里**几乎全有方法体**（ExpansionPassEvent 12 个、RaidEvent 10 个、EventFilter 6 个、LiveOpsAssetUtility 11 个、ScoringEvent 4 个…）；门型类（`FastModeBaseEvent<,>`）在 `d:/2/tools/decomp_generic_trial/out/` 有 25 个产物。
3. **「服务器事务」都有既定的本地等价口径**：`PurchaseMilestone` / `Score` / `SelectTeam` / `SaveGame` 一律换本地存档（`LiveOpsSaveStore`）+ 出声，**不是 P3**（先例：C2 的 `Missions.CollectChallenge` 判 A 就是这条口径）。

### 1·3 施工单元（15 个）与行数

| 单元 | 名称 | 行数 |
|---|---|---:|
| U01 | 事件过滤/触发/冷却/状态机（C1-W2 收尾） | 25 |
| U02 | 活动资产装载层（C1-W3） | 15 |
| U03 | 主菜单活动容器（C1-W4） | 12 |
| U04 | Scoring/里程碑/进度条/领奖钮基类 | 18 |
| U05 | Expansion Pass（实例 + 两页 + 奖励格） | 43 |
| U06 | Raid（实例 + 奖励格 5 档 + 页头） | 32 |
| U07 | TwoSides 双阵营 | 8 |
| U08 | FastMode / Play / Practice | 21 |
| U09 | 挑战引擎（日志计数 + 发奖族 + 血变信号） | 18 |
| U10 | 好友挑战 / 练习邀约（P2P） | 15 |
| U11 | 每日奖励/连登/任务页/自动弹窗 | 16 |
| U12 | 通用活动 UI 件 | 17 |
| U13 | 问卷条件显隐 + 战斗结算奖励行 | 3 |
| U14 | Energy / Skirmish / Ranked 三窗 | 11 |
| U15 | 活动资产图导入 + 接线 | 4 |
| | **合计** | **258** |

> 派活包把这 15 个单元归并成 **P1–P15 十五个写手包**，标了 **7 个共享瓶颈文件**与 **Wave 0/1/2 三层依赖**（见 `B9_日常C_派活包.md` §〇/§一/§二）。

### 1·4 玩家可见度排序（本域**没有 P0**）

活动全部过期 + EVENT 导航钮有意隐藏 ⇒ 本域今天**没有 P0**。可见度最高的 8 行（建议排最前）：

| 账行 | 方法 | 为什么最显眼 |
|---|---|---|
| **430** | `MainMenuMission.TrySchedulePopup` | 原版**进主菜单有可领挑战就自动弹每日奖励/连登窗**，我们恒不弹（玩家每天都会碰到） |
| **548** | `OpenEventButton.SetButtonInfo` | 主菜单常显件：标题仍硬编码 `EVENT`（SO 标题键已有）、倒计时恒 `--` |
| **297** | `LiveOpsAssetUtility.GetComponentReference` | **活动弹窗的唯一前置**（GUID→prefab 未解 ⇒ 弹窗永不开） |
| 176 / 179 | `EventImageSelectorController.{Initialize,TryDraw}` | 活动图标/背景**留空** |
| 422 / 423 | `LiveopUIDrawer_EventAssetSprites` 内外两层 | 同上 |
| **518** | `Missions.TryShowToastNotification` | 任务 toast **一次都不弹**（`ShowMission` 已写但全仓 0 调用点） |

---

## ② 逐条证据

### 2·1 账本自身的体检（本轮现做，不是转述）

| 检查 | 方法 | 结果 |
|---|---|---|
| 重复键 | 282 行按 `(原版类, 方法)` 计数 | **0 组重复** |
| 出处文件是否存在 | 每条 `判据出处` 里的 `*.cs` 到 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/` 全树找 | **280/280 命中 · 0 缺失**（2 行无 `.cs:行号` 形态，跳过） |
| 出处行号是否落在该方法上 | 读该文件第 N 行 ±30 行内找方法名 | **280/280 命中**（23 条初判「未命中」全是我的名字归一化把 `_003C…_003E` 解成 `<…>` 造成的假阴性，实际第 N 行就是方法声明） |
| 三分类覆盖 | 每条必须落一类 | **258 + 7 + 17 = 282**，脚本断言通过 |

### 2·2 ② P3 候选的 7 条（证据 + 时间限定，⛔ 未判 D）

| 账行 | 原版方法 | 证据（判据出处） | 时间限定（断在哪） |
|---|---|---|---|
| 48 | `ChallengeManager.CheckIfPlayerOnline` | `ChallengeManager.cs:349` | PlayFab 账户在线查询；本机无账户目录（后端关服） |
| 53 | `ChallengeManager.StartChallengeWithUser` | `ChallengeManager.cs:69` | 按玩家名查 PlayFabId（**前半步**）；后半步开局已归 U10 |
| 55 | `_003CCheckIfPlayerOnline_003Eb__14_0` | `ChallengeManager.cs:527` | 同 48 的回调体 |
| 57 | `_003CStartChallengeWithUser_003Eb__8_1` | `ChallengeManager.cs:513` | DisplayName→PlayFabId 查询回调 |
| 265 | `FastModeBaseEvent.GetMatchmakingFilter` | `decomp_generic_trial/out/FastModeBaseEvent_object__object___GetMatchmakingFilter__1a10120.c` | 产出给服务器房间列表的过滤串；我们的 P2P 没有房间列表层（`Net/NetMatchmaking` 是直连/大厅）⇒ 无消费方 |
| 274 | `FastModeBaseEvent.SetCustomMatchOptions` | `FastModeBaseEvent.cs:817` | 写 Photon `RoomOptions.CustomProperties` |
| 514 | `Missions.GetPointsPerArmyOnDailyLogin` | `Missions.cs:2067` | 数据 = 远端 `MissionsConfig.dailyLoginRef` 首档奖励；`assets_full` 全库 0 命中（N17 §⑤-5 已核） |

**建议调度台拆一条**：`274` 的「Photon 房间属性」属服务器匹配（②），但**同一条里「`MsgStart` 缺 eventId/ELO 字段」可以在 `Net/NetProtocol.cs` 加**（我方协议，属 ①）⇒ 拆开后 ① 会多 1 条。

### 2·3 ③ 的 17 条（逐条理由已在 `B9_日常C_分类.tsv`）

| 类别 | 行号 | 关键证据 |
|---|---|---|
| **账已过期（4）** | 382, 391, 397, 401 | Q3 第 3 轮已落地：`Shell/LiveOpsHandler.cs:419`（ExpireEvent）· `:334`（QueueEventPopup）· `:274`（ScheduleEventPopups，VA `0x181018D60`）· `:378`（ShowTriggerEventPopup）。账仍记 C ⇒ **应重判 A/B，不是新工作** |
| 别域（3） | 731, 732, 733 | `SurvivalEventWindowEnemyMarker` 属**战斗内表现**（战斗界面_HUD / 战斗线）；`判据/日常与活动.md:23` 原判「域外 → 战斗线」 |
| 架构差异（3） | 775, 776, 777 | `UIGenericEventCatcher` 转发 prefab 里序列化的 UnityEvent（84 包 **160 处**挂接）；我们全部直接 C# 接线 ⇒ 架构里不存在这一层。**建议判 A，需调度台确认** |
| 已定口径排除（2） | 103, 106 | `PremiumOffer.TryPurchaseOffer` = 真实商店购买；「不做真实经济」（用户边界②）已拍板 |
| 连带已判 D（1） | 696 | 机制已在（`Shell/LiveOpsSharedData.cs:368`）；唯一缺的 `EnergyContext` 已按不做真实经济判 D（F3 第4轮） |
| 抽象声明（1） | 558 | `PlayEvent.GetMatchmakingFilter` 是 `abstract`（cpp2il dev `PlayEvent.cs:1092`）；实现在子类，两个子类的体都在 |
| 编辑器专用（2） | 224, 610 | `*RefreshTestEditorOnly`，正式版不调用；等价物 = `Editor/ShellScene.cs` 重建断言 |
| **判据未定（1）** | 567 | `PlayEvent.SetCustomMatchOptions` 在 cpp2il dev 是 `virtual { }` 空体，而 `decomp_full` / `decomp_generic_trial` **都没有 `PlayEvent` 产物** ⇒ 空体与静默丢体分不开，差一次 RVA 反汇编 |

### 2·4 判据真身的落点（施工单里逐行给了，这里给总表）

| 单元 | Ghidra 真身（`decomp_full/`） | 门型补（`decomp_generic_trial/out/`） |
|---|---|---|
| U01 | `Everguild.LiveOps.{EventFilter,EventGroup,EventLocation,SignalEventTrigger}__*.c` | — |
| U02 | `Everguild.LiveOps.LiveOpsAssetUtility__*.c`（11） | — |
| U03 | `Everguild.LiveOps.{LiveOpsManager__GetAllEvents,LiveOpsEvent__GetEventPriorityForMainMenu,LiveOpsEvent__GetMainMenuData}.c` | — |
| U04 | `Everguild.LiveOps.{ScoringEvent__*,ScoringEventHandler__CollectPoints}.c` | — |
| U05 | `Everguild.LiveOps.ExpansionPassEvent__*.c`（12）+ `…__c__DisplayClass13_0___Expire_b__0.c` | — |
| U06 | `Everguild.LiveOps.RaidEvent__*.c`（10）+ 两个 lambda | — |
| U07 | `Everguild.LiveOps.TwoSidesEvent__*.c`（4） | — |
| U08 | `Everguild.LiveOps.{PracticeEvent,PlayEvent}__*.c` | **`FastModeBaseEvent_object__object___*.c`（25）** |
| U09 | `ChallengeLogMgr__*` · `BattleEventsController__*` · `RewardCollected__*` · `RewardEvent__*` · `RewardEventHandler__*` · `MultiplyRewardsFunction__Preview` | — |
| U10 | `ChallengeManager__*.c`（9） | — |
| U11 | `DailyRewardPopup__*` · `DailyStreakWindow__*` · `LoginBonusRewardDisplay__*` · `MissionMilestoneStep__DisplayCheckmark` · `WeeklyMissionMilestone__DisplayCheckmark` · `Missions__TryShowToastNotification` · `MissionsTab__*` · `MainMenuMission__*` | — |
| U12 | `EventBadge__*` · `EventBadgeController__Awake` · `ShowEventArmiesPanel__*` · `BoostTimerPanel__*` · `TimerDisplay__LocalizationChange` · `LiveOpsEventWindow__OnRefresh` | — |
| U13 | `OnQuestionAnswered__*` · `EndBattleRewardHolder__SetReward` | — |
| U14 | `SinglePlayerOnlyEnergyEventWindow__*` · `SinglePlayerScoringEvent__*` · `SkirmishEventWindow__*` · `RankedEventWindow__*` | — |
| U15 | 无（纯资源 + 接线；判据 = GUID→bundle→`m_Container` 两跳） | — |

---

## ③ 没查清（⛔ 没拿猜测填空）

1. **`PlayEvent.SetCustomMatchOptions` 到底是空钩子还是被 Cpp2IL 丢了体**（账行 567）—— 差一次 RVA 反汇编。`dump.cs` 里能取到 RVA，但本轮是只读判据代理、不跑 Ghidra。
2. **`EventComponents` 的 GUID 到底能解出多少个 prefab 名** —— 两跳路径已证（GUID→bundle→`m_Container`），但**没有逐个跑完 13 份 SO × 全部 key**（那是 P2 的第一步产出）。所以施工单里写的是「解到非空名或明确出声」，不是「一定能解到」。
3. **解出来的 prefab 有多少已经在我们的工程里** —— 本轮**没有查**。`LiveOpsAssetUtility.GetComponentReference` 的通路建成 ≠ 能开窗（prefab 未导入/未重建成 `GameWindow` 的仍开不了）。施工单已把这条写成必报列。
4. **`ExpansionPassData.ScoringMilestones` / `RaidEventData` 的里程碑表到底有没有本地实例** —— N17 记「本地 0 实例」，本轮**没有重新核对** `assets_full` 里是否还有别的 Activity SO 副本。所以 U05/U06 一律按「合成数据 + 出声」写。
5. **`ChallengeManager` 一族的方法体**（`判据/日常与活动.md:92` 记「方法体没逐个读」）—— 本轮只按账本口径判「邀约链可走 P2P、账户目录那 4 行归 ②」，**没有逐句读 `ChallengeManager__*.c` 的 9 个方法体**。U10 的配方是照账本 `差异` 列 + 官方词条表推的，施工时须以 `.c` 为准。
6. **`SurvivalEventWindowEnemyMarker` 归哪个域** —— 我判「别域（战斗内表现）」，依据是 `判据/日常与活动.md:23`；但 G2/D1 第 2 轮把它从 D 改回了 C 并写「域外改判 C」。**是否要移账到战斗界面域，需调度台定**，我没动账。
7. **`UIGenericEventCatcher` 该判 A 还是 C** —— 我倾向 A（架构里不存在这一层），但这推翻 E13 刚补的 C/P1，需调度台确认。
8. **`B9_剩余C_施工单.tsv` 里已有哪些包覆盖到本域** —— 本轮没逐行比对（那份是别的代理按另一批 ① 出的），派活前请调度台扫一眼**别重复派**。

---

## ④ 改动清单

### 4·1 写了什么（只有 3 个判据件 + 本报告）

| 文件 | 内容 | 行数 |
|---|---|---:|
| `资料/完全复刻/判据/B9_日常C_分类.tsv` | 282 行逐条：账行号 / 原版类 / 方法 / 分类 / 一句理由 / 证据 | 283（含表头） |
| `资料/完全复刻/判据/B9_日常C_施工单.tsv` | **① 类 258 行 × 12 列**，表头与 `B9_剩余C_施工单.tsv` **逐字一致** | 259 |
| `资料/完全复刻/判据/B9_日常C_派活包.md` | 15 个写手包 + 7 个共享瓶颈文件 + Wave 0/1/2 依赖分层 + 每包判据真身与退出条件 | 218 |
| `资料/完全复刻/报告/B9_K2DL_日常C施工单.md` | 本报告 | — |

- 编码与行尾：**UTF-8 无 BOM · LF**（与 `B9_剩余C_施工单.tsv` / `B9_剩余C_派活包.md` 一致，已按二进制核过）。
- 列数自检：分类表 282 行**全 6 列**；施工单 258 行**全 12 列**（脚本断言，非目测）。

### 4·2 明确**没**动的

- ⛔ `账/日常_奖励.tsv` **一个字节没改**（分类与施工单都在 `判据/`，账的改判由调度台在同步点做）。
- ⛔ 没有 `.cs` / 资产 / 场景 / prefab 改动；`git status` 里本域零新增改动。
- ⛔ 没跑 Unity、没跑 `typecheck`（没有 `.cs` 改动 ⇒ 按铁律 12 判据 3「纯文档零条」）。
- ⛔ 没动 `d:/2/`。

### 4·3 给调度台的三个后续动作建议

1. **先核 4 行账已过期**（382/391/397/401）：Q3 已落地但仍记 C ⇒ 同步点重判 A/B，别当新工作派。
2. **派活前扫一眼 `B9_剩余C_施工单.tsv`** 有没有本域的行（避免同一文件两个包）。
3. **`Shell/TopBar.cs` 与 `Shell/MainMenuRuntime.cs` 是全项目共享瓶颈** —— 本域两个包要它们，界面线/战斗线可能也要 ⇒ 派活时由调度台显式排顺序，⛔ 不让代理自己按 `git status` 判「没人写」。

---

## ⑤ 🔴 顺手发现（**只报不改**）

> 以下全部**没动**任何文件，交调度台分流。

### 5·1 【账过期】4 行已落地却仍记 C
`LiveOpsHandler.ExpireEvent/QueueEventPopup/ScheduleEventPopups/ShowTriggerEventPopup`（账行 382/391/397/401）已由 Q3 第 3 轮按真身落地（`Shell/LiveOpsHandler.cs:419/334/274/378`，`报告/Q3_第3轮.md:53-56`）⇒ 账 C **应重判 A/B**。这是「账填完了 ≠ 复刻完了」的反向：**复刻完了 ≠ 账填完了**。

### 5·2 【判据路径】`guid_map.tsv` 有两条，混用会停在半路
- `资料/普查产出_第二十七会话/guid_map.tsv`（4,998 行，**卡牌专用**）：对 `liveops` 与那颗图标 GUID `3a8a1016…` **均 0 命中**（实测）。
- `Unity/数据/索引/guid_map.tsv`（10,325 行）：给 **GUID→bundle 名**，含 12 条 `liveopsicons_assets_all`。
- 🔴 判据把这步写成「把 `EventComponents` 的 GUID 经 `Unity/数据/索引/guid_map.tsv` 解成 prefab 名」（`判据/LiveOps_C1W2_C1W3_第3轮.md` §三·改动请求②；`报告/Q3_第3轮.md` §七「下一轮」第 1 条同义）—— **那句话只说了第一跳**：那张表里没有资产名，只有 bundle。第二跳（打开该 bundle 的 `AssetBundle_1.json` 的 `m_Container` 按 GUID 拿 asset 名/PathID）**任何文档里都没写**，照它做会停在 bundle 名上。已在施工单 U02 配方里补全两跳并给了 L04 那颗图标的实例路径。

### 5·3 【判据可解】`GetTimeDescriptionTextKey` 的四字面量其实有唯一候选
N17 §⑤-4 记「四个 static string 字面量没解出」。本轮在 `Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 里查到：`Event_Description/General/` 恰好有 **`TimeStartsIn`（开始于：）/ `TimeEndsIn`（结束于：）/ `TimeFinished`（完成）** 三键 **+ 空串 = 四档**，与 N17 的推测**逐一吻合**。
⇒ 判据状态从「查不到」升级为「**有唯一候选、一步可证**」（还差 `read_literal.py` 读 `DAT_1842aa898/aaa88/aa990/b80e0` 的 VA 复核）。已在账行 336 的施工步骤里写明。

### 5·4 【产物缺口】`PlayEvent<T>` 在 Ghidra 侧 0 产物
- `decomp_full/` grep `PlayEvent__` = **仅 2 个 `IPlayEvent__get_*` getter**；
- `decomp_generic_trial/out/` grep `playevent` = **0**；
- 同族的 `FastModeBaseEvent<T>` 在 `decomp_generic_trial/out/` 有 **25 个产物**。
⇒ **G7 那批泛型全量导出的名单漏了 `PlayEvent<T>`**（与 CLAUDE.md 记的「按 `// RVA:` 非负筛名单会静默丢掉全部泛型类方法」是同族）。本批 5 行 PlayEvent 判据只能靠 cpp2il dev 的半可读体。

### 5·5 【潜在丢体】`PlayEvent` 的两个 `virtual { }`
`SetCustomMatchOptions`（`:1088`）与 `OnMatchFinished`（`:972`）在 cpp2il dev 里是**空体**。子类有体（`FastModeBaseEvent__SetCustomMatchOptions`（generic trial）· `PracticeEvent__OnMatchFinished`（decomp_full））⇒ 很可能是「基类空钩子」。但按铁律「凡 Cpp2IL 说空，一律用 RVA 复核」，**没复核前不能写成「空钩子」**。已把账行 567 归 ③（判据未定）而不是 ②。

### 5·6 【分堆覆盖率】N17 的工作项标签只覆盖了 46%
282 行里 **130 行**带 `工作项 W1..W9`，**152 行**备注只有「N17 第1轮分堆：P2」没有工作项。⇒ 那 152 条当时没进任何工作项，所以「C3 其余 141 条逐方法缺口」那句是笼统概括。本次用 **U01–U15 覆盖 282/282**（脚本断言），可作为下一轮派活的索引。

### 5·7 【已解锁但账没跟上】`MainMenuMission` 那 5 行现在可做了
账行 428–432 判 C 时 `MenuActions` 还不存在（Q3 之后才建：`Shell/LiveOpsHandler.cs:1205` 类 / `:1262 ReleaseAction`）⇒ 其中 **430（TrySchedulePopup）是本域唯一「玩家每天都会看到」的那条**（进主菜单自动弹每日奖励/连登窗），今天恒不弹。本次归 U11 并排在本域最前。

### 5·8 【正本冲突】活动线到底开没开，两份正本说法相反
- `资料/任务线/活动线/任务总表.md` §一 顶部：「🔴🔴 **2026-10-25 用户【暂停本线】**（原话：活动 / 新手引导 先暂停）」+ 末段「本线按用户 2026-10-23 暂停」。
- `资料/完全复刻/调度台日志.md:2185`：「🔴 用户三条新决定（2026-10-25）… 原话『**日常/活动线 779 条…要不要开？开吧。**』⇒ ✅ **日常/活动线开线**：原「只审不动工」**作废**」。

两份都写 2026-10-25，**结论相反**。本报告按**调度台日志的「开吧」**处理（它更具体，且明写「原『只审不动工』作废」）。若实际是暂停，这 258 行的施工单仍可当**待命方案**用。⛔ 请调度台就地订正其中一处（铁律 5/6：两份说法打架比没有更糟）。

### 5·9 【文档过期】`判据/日常与活动.md` 的「恢复顺序」前两步已完成
该文件 §四 写「1. 存档归并 → 2. `LiveOpsHandler` + `LiveOpsEvent<TSave>` → 3. 用 LocalCatalog 当种子 → 4. 逐窗换真事件实例」。其中 **1（E3 存档 Step 3/4 机制）与 2（E2 五文件 2,184 行）已落地**，但该文件头部仍写「本轮一行 `.cs` 都没改（用户已暂停活动线）」+ §一「A 已对齐 = 0」。⇒ 它的数字与状态已过期，建议就地补一行订正痕迹（铁律 5），⛔ 别让下个会话拿它当现状。
