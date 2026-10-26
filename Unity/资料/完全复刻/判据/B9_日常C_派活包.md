# B9 · 日常/奖励域 C 类（282 条）派活包

> 2026-10-26 · 只读判据代理 **K2DL**（第八十四会话）。
> 输入 = `账/日常_奖励.tsv` 里 **状态=C 的 282 行**（现读，不是估）。
> 产出配套：`判据/B9_日常C_分类.tsv`（282 行逐条分类）· `判据/B9_日常C_施工单.tsv`（**① 类 258 行**，12 列）· `报告/B9_K2DL_日常C施工单.md`。
> 三分类结果：**① 可真做 258 · ② P3 候选 7 · ③ 判不了/别域/非缺口 17**。

---

## 〇 一页速览

| 包 | 工作单元 | ① 行数 | 独占文件（**一个文件只有一个写手**） | 前置 |
|---|---|---:|---|---|
| **P1** | U01 事件过滤/触发/冷却/状态机 | 25 | `Shell/LiveOpsEvent.cs` ＋ 新建 `Shell/LiveOpsFilter.cs` | — |
| **P2** | U02 活动资产装载层 | 15 | 新建 `Shell/LiveOpsAssetUtility.cs` ＋ `Shell/LiveOpsLocalCatalog.cs` ＋ 新建 `工具/gen_liveops_components.py` | — |
| **P3** | U15 活动资产图导入 + 接线 | 4 | 新建 `Shell/EventImageSelectorController.cs` ＋ `工具/import_original_art.py` ＋ `Resources/Art/` | —（与活动线 L04 同一批） |
| **P4** | U04 Scoring/里程碑/进度条/领奖钮基类 | 18 | 新建 `Shell/ScoringEvent.cs`、`EventProgressBarController.cs`、`RewardProgressBarPanel.cs`、`BaseEventProgressClaimButtonController.cs` | — |
| **P5** | U05 Expansion Pass | 43 | `Shell/ExpansionPassEventWindow.cs` ＋ 新建 `Shell/ExpansionPassEvent.cs` | P4 |
| **P6** | U06 Raid | 32 | `Shell/RaidEventWindow.cs` ＋ 新建 `Shell/RaidEvent.cs` | P4 |
| **P7** | U07 TwoSides | 8 | `Shell/ParentEventWindow.cs` ＋ 新建 `Shell/TwoSidesEvent.cs` | P4（里程碑） |
| **P8** | U03 主菜单活动容器 | 12 | `Shell/LiveOpsManager.cs`、`Shell/TopBar.cs`、`Shell/MainMenuRuntime.cs` | P1（filter）· P2（容器资产） |
| **P9** | U11 每日/连登/任务页/自动弹窗 | 16 | `Shell/{DailyRewardPopup,DailyStreakPopup,MissionsTab,DailyData,ToastNotifications,MainMenuRuntime}.cs` | P2（弹窗引用）· 与 P8 抢 `MainMenuRuntime` |
| **P10** | U12 通用活动 UI 件 | 17 | 新建 `Shell/{EventBadge,ShowEventArmiesPanel,BoostTimerPanel}.cs` ＋ `Shell/LiveOpsEventWindow.cs` | P1（信号）· P2（组件 GUID） |
| **P11** | U09 挑战引擎 | 18 | 新建 `Shell/ChallengeEngine.cs` ＋ `Battle/BattleDriver.cs` | P1（信号总线）· **与战斗域串行** |
| **P12** | U10 好友挑战 / 练习邀约（P2P） | 15 | 新建 `Shell/ChallengeManager.cs` ＋ `Shell/{ChatPanel,DuelPopupWindow}.cs` ＋ `Net/{NetChat,NetMatchmaking}.cs` ＋ `Shell/TopBar.cs` | P1 · 与 P8 抢 `TopBar` |
| **P13** | U08 FastMode / Play / Practice | 21 | 新建 `Shell/PlayEvent.cs`、`Shell/FastModeBaseEvent.cs` ＋ `Shell/PracticeModePopup.cs` | P4（里程碑） |
| **P14** | U14 Energy / Skirmish / Ranked 三窗 | 11 | `Shell/{EnergySinglePlayerOnlyEventWindow,SkirmishEventWindow,RankedEventWindow}.cs` | P4 |
| **P15** | U13 问卷条件显隐 ＋ 战斗结算奖励行 | 3 | `Shell/RewardWindow.cs` ＋ 问卷窗 ＋ 战斗结算面板 | **与战斗域串行** |

**合计 ① = 258 行**（另有 ② 9 行 / ③ 15 行，见 §五，不进施工单）。

---

## 一 共享瓶颈文件（派活前必须点名，否则两个写手会撞）

| 文件 | 谁会写 | 处置 |
|---|---|---|
| `Shell/TopBar.cs` | **P8**（活动钮标题/倒计时/反选关窗）· **P12**（挑战钮五件 + 注释订正） | **串行**：先 P8 后 P12（或合并成一个写手）。P12 的注释订正落点 = `TopBar.cs:42-51/267-270/367-370` |
| `Shell/MainMenuRuntime.cs` | **P8**（活动容器注入）· **P9**（自动弹窗那一拍） | **串行**：P9 的 ⑧ 会改 `MainMenuRuntime.Build` 首段 ⇒ 先做 P9 再做 P8（P9 玩家可见度更高） |
| `Shell/ParentEventWindow.cs` | **P7** 独占写；P8/P10 只读 | 派活时写明「P8/P10 不许改这份，需要改就写改动请求」 |
| `Battle/BattleDriver.cs` | **P9**（一处 `ShowMission` 调用）· **P11**（治疗/阵亡/血变信号）· **P15**（结算奖励行） | **合并成一张「战斗域改动清单」**交给战斗域写手，本线不并行改 |
| `Net/NetProtocol.cs` | **P12**（邀约四消息）· **P13**（`MsgStart` 的 eventId/ELO，若调度台决定做） | 串行；P13 那半属 ② 里被拆出的「① 部分」，见 §五 |
| `工具/import_original_art.py` ＋ `Resources/` 索引 | **P3** 独占 | 与**特效线 / 卡面线**的导入批次**必须串行**（导入器会重写索引） |
| `Shell/LiveOpsHandler.cs` | **不许任何人改**（Q3 刚落，`MenuActions`/`LiveOpsPopupPresenter` 都在里面） | 需要改就写改动请求给调度台 |

---

## 二 依赖分层（按这个顺序开波，别一次全放）

**Wave 0（7 路，互不相交，可立刻开）**
`P1`（U01）· `P4`（U04）· `P2`（U02）· `P3`（U15）· `P10`（U12）· `P14`（U14）· `P15`（U13）
> P10/P14 在 U04 未落前先做**不依赖里程碑**的那几件（EventBadge / BoostTimerPanel / ShowEventArmiesPanel / TimerDisplay 本地化 / Ranked 空卡组拦截），依赖项等 Wave 1。

**Wave 1（5 路）**
`P5`（U05，需 P4）· `P6`（U06，需 P4）· `P7`（U07，需 P4）· `P9`（U11，需 P2）· `P13`（U08，需 P4）

**Wave 2（2 路，都要串行等瓶颈文件）**
`P8`（U03，等 P9 放掉 `MainMenuRuntime`）· `P12`（U10，等 P8 放掉 `TopBar`）

**单独排（不占并发位，由调度台按别的域的节奏插）**
`P11`（U09）与 `P15` 的结算奖励行都要动 `Battle/BattleDriver.cs` ⇒ 交给战斗域那一批的写手。

---

## 三 每个包的判据真身（照这些文件读，⛔ 别自己找）

> 判据根：**Ghidra 真身** `d:/2/tools/decomp_full/`；**门型类**补 `d:/2/tools/decomp_generic_trial/out/`（641 个产物）；**骨架** `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`。
> 🔴 读 `.c` 之前先查 `G4_文件名与内容不符_全量.tsv`；首行不是自己的函数名 ⇒ 先分清「内容错位」还是「ICF 折叠」。

### P1 · U01 事件过滤/触发/冷却/状态机（25 行）
- 真身：`decomp_full/Everguild.LiveOps.{EventFilter,EventGroup,EventLocation,SignalEventTrigger}__*.c`
  （EventFilter 六个体全在：`AddOrRemoveFromGroups` / `CheckEventGroups` / `CleanupTriggers` / `RegisterTriggers` / `TryActivateEvent` / `Validate`）
- 覆盖账行：`319,320,322,342,345,346,347,355`（LiveOpsEvent 本体）+ `162,163,164,166,167`（EventFilter）+ `170,171,172,173,174,175`（EventGroup 4 + AssetProvider 1 + Config 1）+ `180`（EventLocation）+ `701,702,703,704`（SignalEventTrigger）+ `550`（ParentEventHandler）
- 施工顺序：**信号总线 → filter → eventGroup → location → LiveOpsEvent 各调用点**（总线必须先行，它是 filter 与 RaidEvent.Initialize 的唯一依赖）
- 退出条件：`ShellScene.Run` 四条断言（见施工单 `怎么验`）+ `tools/typecheck.sh` 0/0；`ruleprobe check` 指纹不变
- ⚠️ `355` 已被调度台 2026-10-26 裁定「重新降回 C」⇒ **只做内容，不动等级**

### P2 · U02 活动资产装载层（15 行）
- 真身：`decomp_full/Everguild.LiveOps.LiveOpsAssetUtility__{GetComponentReference,GetAssetReference,LoadAsset_object_,LoadComponent_object_,TryLoadComponent_object_,ReleaseEventAssets,Unload,Unload_Int32Enum_,HasComponent,HasAsset}.c`（11 个体全在）
- **GUID→prefab 两跳（本次核实；判据里原来只写了第一跳 —— 见 `报告/Q3_第3轮.md` §七「下一轮」第 1 条、`判据/LiveOps_C1W2_C1W3_第3轮.md` §三·改动请求②）**：
  ① `Unity/数据/索引/guid_map.tsv`（10,325 行）只给 **GUID → bundle 名**；
  ② 打开 `D:/2/新解包资源/assets_full/bundle_<bundle>/AssetBundle/AssetBundle_1.json` 的 `m_Container` 才拿到 **asset 名 / PathID**。
  🔴 **别用** `资料/普查产出_第二十七会话/guid_map.tsv`（卡牌专用 4,998 行，对 `liveops` 与那颗图标 GUID **均 0 命中**，本次实测）。
- 覆盖账行：`297,306,307,308,309,310,314,315,316,317,318`（LiveOpsAssetUtility）+ `328,348`（LiveOpsEvent 容器）+ `362,367`（LiveOpsEventData 容器）
- 退出条件：13 份 SO 的 `EventComponents` 每格都解出非空 prefab 名（或明确出声）；`LiveOpsPopupPresenter.Open` 从恒 null 变非 null；输出里带「**是否已导入我们工程**」列
- ⚠️ 只解到名字 ≠ 能开窗。⛔ 别把「解析成功」写成「已接通」。

### P3 · U15 活动资产图导入 + 接线（4 行）
- 判据全链已查实（本次复核）：GUID `3a8a1016497754066b40d88caeee722b` → `guid_map.tsv` 给 `liveopsicons_assets_all` → `AssetBundle_1.json` 的 `m_Container` → `Sprite/40k_campaign_Expansion Pass.json`（PathID `947306652608088988`）
- 覆盖账行：`176,179`（EventImageSelectorController）+ `422,423`（LiveopUIDrawer_EventAssetSprites 内外两层同名重载）
- **必须同一批**：导图（`Resources/Art/`）+ 接线。只接不导 = 空转（N17 已踩过这条判断）
- 与**活动线 `L04`**（主菜单第 6 颗 `40k_main_bt_event`）同一批做，⛔ 别分两次导

### P4 · U04 Scoring/里程碑/进度条/领奖钮基类（18 行）
- 真身：`decomp_full/Everguild.LiveOps.{ScoringEvent__*,ScoringEventHandler__CollectPoints}.c` + 解包 prefab（Event Progress Bar / Reward Progress Bar Panel / Collect Button 的 `stateDefinitions`）
- 覆盖账行：`691,692,693,694`（ScoringEvent）+ `695`（ScoringEventHandler）+ `671,672,673`（RewardProgressBarPanel）+ `181..186`（EventProgressBarController）+ `5,6,7,8`（BaseEventProgressClaimButtonController）
- 🔴 **这是 P5/P6/P7/P13/P14 的共同前置** —— 不先落，五个包会各写一份「进度条 + 四档状态机」（两处同一条规则 = 迟早不一致）
- 退出条件：合成 3 档里程碑（已领/可领/未达）三态各一；端帽 p=0/1 不显、p=0.5 显；四档领奖钮各造一次

### P5 · U05 Expansion Pass（43 行）
- 真身：`decomp_full/Everguild.LiveOps.ExpansionPassEvent__{GetBaseData,GetBaseSave,Score,GetCurrentMilestone,GetCurrentMilestoneIndex,GetScoringInfoFromCurrentMilestone,GetState,IsMilestoneCollected,PurchaseMilestone,PurchaseLevelUp,GetCostToLevelUp,Expire}.c`（12 体全在）+ `…__c__DisplayClass13_0___Expire_b__0.c`
- 覆盖账行：`205..218`（事件实例 14）· `248..257`（RewardSelector 10）· `242..247`（RewardItemContainer 6）· `235..238`（ProgressClaimButton 4）· `223,226`（EventTab）· `229,230,232,233,234`（MissionsTab 5）· `240,241`（RewardDrawer 2）
- ⚠️ 本窗已有 **1,796 行**静态骨架 ⇒ 每改一段就 `check_ledger_points 日常_奖励`，落点（`:950/:1070/:1134/:1203`）会漂
- ⚠️ **不做真实经济**：`PurchaseLevelUp`/`PurchaseMilestone` 的本地等价 = 扣本地钱包（固定 2000 口径）+ 写本地存档，⛔ 不接真实支付、不接 CloudScript

### P6 · U06 Raid（32 行）
- 真身：`decomp_full/Everguild.LiveOps.RaidEvent__{Dispose,Initialize,Expire,GetBaseData,GetBaseSave,GetCurrentMilestone,GetCurrentMilestoneIndex,GetScoringInfoFromCurrentMilestone,PurchaseMilestone,OnRewardCollected}.c` + `…__c__DisplayClass13_0___Expire_b__0.c` / `…__c___OnRewardCollected_b__14_0.c`
- 覆盖账行：`589..601`（13）· `602..606`（5）· `612,618,624`（3）· `625,626,627`（3）· `629..636`（8）

### P7 · U07 TwoSides（8 行）
- 真身：`decomp_full/Everguild.LiveOps.TwoSidesEvent__{Expire,PurchaseMilestone,SelectTeam,TryColllect}.c`
- 覆盖账行：`745,748,749,750` · `753` · `765,766` · `770`
- 底座已有：`Shell/ParentEventWindow.cs` 把 TwoSides 生命周期建完（10 A / 7 B）⇒ 本包只补选边落地 + 里程碑领取
- ⚠️ 页面当前**不可达**（EVENT 钮有意隐藏）⇒ 与 `C1-W6` 一起恢复；自检用现有 TwoSides 夹具强制打开

### P8 · U03 主菜单活动容器（12 行）
- 真身：`decomp_full/Everguild.LiveOps.{LiveOpsManager__GetAllEvents,LiveOpsEvent__GetEventPriorityForMainMenu,LiveOpsEvent__GetMainMenuData}.c`
- 覆盖账行：`331,334`（LiveOpsEvent 主菜单数据）· `409,414`（LiveOpsManager）· `620..623`（RaidOrArmyReleaseMenuContainer 4）· `545,548,549`（OpenEventButton 3）· `494`（MissionMenuContainer）
- **数据已就位**：`priority` / `showInMainMenu` / `location.type` / `location.parent` 已由 R14 搬进 `LiveOpsLocalEvent` ⇒ `OrderByDescending(priority)` 今天能真生效
- ⚠️ `548` 是 P1 且玩家常显：标题仍在硬编码 `EVENT`（`LiveOpsLocalCatalog` 已有 13 份 SO 的标题键）；倒计时传 `null` target ⇒ 那行恒 `--`
- ⚠️ 与 `C1-W6`「无在期 Parent 事件 ⇒ 关 EVENT 灯」的口径要一起定，⛔ 别一边隐藏一边注入

### P9 · U11 每日/连登/任务页/自动弹窗（16 行）
- 真身：`decomp_full/{DailyRewardPopup__*,DailyRewardTrackPanel__*,DailyRewardItemContainer__*,DailyStreakWindow__*,LoginBonusRewardDisplay__*,MissionMilestoneStep__DisplayCheckmark,WeeklyMissionMilestone__DisplayCheckmark,Missions__TryShowToastNotification,MissionsTab__*,MainMenuMission__*}.c`
- 覆盖账行：`86` · `91,92` · `121` · `426,427` · `428,429,430,431,432` · `495` · `518` · `525,530` · `778`
- **`430`（MainMenuMission.TrySchedulePopup）是本域唯一「玩家每天都会看到」的那条**：原版进主菜单有可领挑战就自动弹每日奖励/连登窗，我们今天恒不弹
- `MenuActions` 已由 Q3 建好（`Shell/LiveOpsHandler.cs:1205` 类、`:1262 ReleaseAction`）⇒ **前置已通**，不要再等
- ⚠️ 会牵动 `Editor/MainMenuScene.cs` 里 A384 那 4 条红点断言（N17 刚改过）⇒ 改完必须复跑 `MainMenuScene.Run`

### P10 · U12 通用活动 UI 件（17 行）
- 真身：`decomp_full/{EventBadge__*,EventBadgeController__Awake,ShowEventArmiesPanel__*,BoostTimerPanel__*,TimerDisplay__LocalizationChange,LiveOpsEventWindow__OnRefresh}.c`
- 覆盖账行：`12,13`（BaseParentEventWindow）· `19..22`（BoostTimerPanel）· `155..159`（EventBadge 4 + Controller）· `335,336`（LiveOpsEvent 状态文案）· `374`（LiveOpsEventWindow.OnRefresh）· `699,700`（ShowEventArmiesPanel）· `739`（TimerDisplay）
- ⚠️ `336` 的四个 static string 字面量：**本次新证** —— 官方本地化 `Event_Description/General/` 恰好有 `TimeStartsIn` / `TimeEndsIn` / `TimeFinished` 三键 + 空串 = 四档，与 N17 的推测逐一吻合 ⇒ 可先按这四档落，VA 复核（`read_literal.py` 读 `DAT_1842aa898/aaa88/aa990/b80e0`）留成 TODO
- ⚠️ `739` 要改 `GameWindow.OnLanguageChanged` 基类（跨全工程窗）⇒ 单独小批，别和别的改动混

### P11 · U09 挑战引擎（18 行）
- 真身：`decomp_full/{ChallengeLogMgr__LogHeal,ChallengeLogMgr__TroopKilled,BattleEventsController__CheckHealth,BattleEventsController__Update,RewardCollected__*,RewardEvent__*,RewardEventHandler__*,MultiplyRewardsFunction__Preview}.c`
- 覆盖账行：`16,18` · `36,41` · `535` · `653..659` · `660,662,663` · `666,667` · `668`
- 底座：N17 说「挑战引擎整层未复刻」；信号总线由 P1 提供 ⇒ **本包排在 P1 之后**
- ⚠️ 治疗量 / 单位阵亡 / 通用加效果三格今天**无数据消费者**（102 条成就里没有这三类）⇒ 建了先只有自检在用，如实标「等消费者」，⛔ 不许说成可见收益
- ⚠️ `Battle/BattleDriver.cs` 是战斗域共享瓶颈 ⇒ 与战斗域串行

### P12 · U10 好友挑战 / 练习邀约（15 行）
- 真身：`decomp_full/ChallengeManager__{AcceptPracticeChallenge,AcceptedFriendDuelNotification,CancelDueling,CancelPracticeInvitation,CancelPracticeMatchAndRefresh,OnRemoveFriendDuel,SendPracticeInvitation,Start,TryChallengeMatchStart}.c`
- 覆盖账行：`42,43,44,45,46,49,51,52,54,56`（邀约链 10）+ `23,24,25,27,28`（ChallengeButton 5）
- **口径**：走**我们自己的 P2P**（`Net/NetChat`），不是 PlayFab PrivateMessages；「按玩家名找人」那 4 行已归 ②（见 §五），本包只做地址/房间维
- 词条已核：`Demo/FriendsMenu/ReceiveChallenge` · `WaitingOpponent` · `EnterPlayerName` · `InvalidName` · `CantFind` 全在官方本地化 10,854 条里
- 退出条件：loopback 邀约→接受→双方开局且先手一致；**两台机器真连一次**（顺带补 `真Play待验清单` E3）

### P13 · U08 FastMode / Play / Practice（21 行）
- 真身：**门型类** ⇒ `decomp_generic_trial/out/FastModeBaseEvent_object__object___*.c`（Expire/GetArmyState/GetArmyStats/GetBaseData/GetBaseSave/GetEventDeck×2/GetMatchmakingFilter/GetSqlFilter…）；**非门型** ⇒ `decomp_full/Everguild.LiveOps.{PlayEvent,PracticeEvent}__*.c`
- 覆盖账行：`258..262,267,268,270,271,272,273,275`（12）· `562,568,574`（3）· `575,576`（2）· `579`（1）· `582,583,584`（3）
- ⚠️ `265` / `274` / `558` / `567` **不在这里**（Photon 匹配过滤与 RoomOptions ⇒ ②，见 §五）
- ⚠️ 本条会改「轮抽恒可进」的现状（`PracticeEvent.OnMatchWon` 的首胜 gate `canEnterDraftMode`）⇒ 报告里必须点名

### P14 · U14 Energy / Skirmish / Ranked 三窗（11 行）
- 真身：`decomp_full/{SinglePlayerOnlyEnergyEventWindow__*,SinglePlayerScoringEvent__*,SkirmishEventWindow__*,RankedEventWindow__*}.c`
- 🔴 `decomp_full/SkirmishEventWindow__StartBattle.c` 的**函数体逐字等于 `BattleButtonClick`**、`__CloseButtonClick.c` 里装的是 `TutorialTipScript__StopTipPermanent` ⇒ 这两条以 ILSpy 为准（文件名与内容不符的第 5/6 例）
- 覆盖账行：`706,712,713,714,715`（5）· `716,717`（2）· `721,727`（2）· `646,649`（2）
- ⚠️ `RankedEventWindow` 本机有 V1/V2 两个独立 prefab，本工程画的是 **V2**，而 `646/649` 两行的原版类是 **V1** ⇒ 施工前先定「V1 是否仍可开」，⛔ 别按 V2 的树去补 V1 的空卡组拦截

### P15 · U13 问卷条件显隐 + 战斗结算奖励行（3 行）
- 真身：`decomp_full/{OnQuestionAnswered__OnAnswered,OnQuestionAnswered__Start,EndBattleRewardHolder__SetReward}.c`
- 覆盖账行：`154`（结算奖励行）· `538,540`（问卷）
- 🔴 `154` 的旧裁定**已过期**：2026-09-17 用户拍板「结算不做」，但 2026-10-18 / 10-25 两次改口径「全部要做」⇒ 本条按**要做**处理；请调度台把旧裁定的撤销写进正本，⛔ 别让两个口径并存
- ⚠️ 问卷窗**无活入口**（`OnQuestionAnswered` 只在问卷弹窗里生效）⇒ 本包只能做到「逻辑 + 夹具」，如实报

---

## 四 派活简报里必须贴的四段（缺一样就会拿回没用的东西）

1. **红线**：不改 `账/*.tsv` 主表 · 不动 git · 不跑 Unity 批处理（typecheck 可以，但要独立 `TMPDIR`）· 不改正本（`CLAUDE.md` / `项目任务.md`）· 卡住就停手写报告，⛔ 不许自己发明口径。
2. **判据**：只读 §三 给的那几个 `.c` / prefab；`.c` 首行不是自己的函数名要先分清「内容错位」还是「ICF 折叠」。
3. **不许踩的三条既有裁定**：`LiveOpsEvent.ToggleEventActivation` 保持 C（2026-10-26 调度台）· 轮抽**不做奖励**（2026-10-24）· 不做真实经济（钱包固定 2000）。
4. **顺手发现也要报**（不改）：现场看到只读代理看不见的东西（命中区裸节点、断言空转、账落点已漂）一律写进报告。

---

## 五 不进施工单的 24 行（② 7 · ③ 17）

### ② P3 候选（7 行）—— **只给证据 + 时间限定，⛔ 不判 D，裁定权在调度台**

| 账行 | 方法 | 证据 | 时间限定（断在哪） |
|---|---|---|---|
| 48 | `ChallengeManager.CheckIfPlayerOnline` | PlayFab 账户在线查询 | `ChallengeManager.cs:349` |
| 53 | `ChallengeManager.StartChallengeWithUser` | 按玩家名查 PlayFabId（前半步） | `ChallengeManager.cs:69` |
| 55 | `_003CCheckIfPlayerOnline_003Eb__14_0` | 同上回调 | `ChallengeManager.cs:527` |
| 57 | `_003CStartChallengeWithUser_003Eb__8_1` | DisplayName→PlayFabId 回调 | `ChallengeManager.cs:513` |
| 265 | `FastModeBaseEvent.GetMatchmakingFilter` | 服务器房间列表的匹配过滤串；P2P 无房间列表层 | `FastModeBaseEvent.cs:856`（体在 `decomp_generic_trial/out/…__GetMatchmakingFilter__1a10120.c`） |
| 274 | `FastModeBaseEvent.SetCustomMatchOptions` | Photon `RoomOptions.CustomProperties` | `FastModeBaseEvent.cs:817` |
| 514 | `Missions.GetPointsPerArmyOnDailyLogin` | 数据 = 远端 `MissionsConfig.dailyLoginRef` 首档奖励；`assets_full` 0 命中 | `Missions.cs:2067` |

> 🔴 **建议调度台拆一条**：`274` 里「Photon 房间属性」那半属服务器匹配（②），但**同一条里「`MsgStart` 缺 eventId/ELO 字段」那一半可以在 `Net/NetProtocol.cs` 加**（我方协议，属 ①）⇒ 拆开后 ① 会多 1 条。

### ③ 判不了 / 别域 / 非缺口（17 行）

| 账行 | 方法 | 归类 | 理由 |
|---|---|---|---|
| 382 | `LiveOpsHandler.ExpireEvent` | **账已过期** | Q3 第 3 轮已落地（`Shell/LiveOpsHandler.cs:419`）⇒ 应重判 A/B，不是新工作 |
| 391 | `LiveOpsHandler.QueueEventPopup` | 账已过期 | 同上（`:334`） |
| 397 | `LiveOpsHandler.ScheduleEventPopups` | 账已过期 | 同上（`:274`，VA `0x181018D60`） |
| 401 | `LiveOpsHandler.ShowTriggerEventPopup` | 账已过期 | 同上（`:378`） |
| 224 | `ExpansionPassEventTab.RefreshTestEditorOnly` | **非缺口** | 编辑器专用钩子，正式版不调用；等价物 = `Editor/ShellScene.cs` 重建断言 |
| 610 | `RaidEventTab.RefreshTestEditorOnly` | 非缺口 | 同上 |
| 731/732/733 | `SurvivalEventWindowEnemyMarker.*` | **别域** | 生存事件窗的敌方标记属**战斗内表现**（战斗界面_HUD / 战斗线） |
| 775/776/777 | `UIGenericEventCatcher.*` | **判不了（架构差异）** | 原版把 prefab 里序列化的 UnityEvent 转发到 C# 委托（84 包 **160 处**挂接）；我们全部直接 C# 接线 ⇒ 架构里不存在这一层。**建议判 A，需调度台确认** |
| 558 | `PlayEvent.GetMatchmakingFilter` | **非缺口（抽象声明）** | 基类里是 `abstract`（cpp2il dev `PlayEvent.cs:1092`），没有方法体可复刻；实现在子类，两个子类的体都在（`PracticeEvent__GetMatchmakingFilter.c` / `FastModeBaseEvent_object__object___GetMatchmakingFilter__1a10120.c`） |
| 567 | `PlayEvent.SetCustomMatchOptions` | **判不了（判据未定）** | cpp2il dev 显示 `virtual { }` 空体，而 `decomp_full` 与 `decomp_generic_trial` **都没有 `PlayEvent` 的产物**（泛型类未导出）⇒「本来就是空钩子」与「Cpp2IL 静默丢体」分不开。**差一步：按 `dump.cs` 的 RVA 反汇编一次** |
| 103/106 | `DailyRewardTrackPanel.HandlePurchase{,回调}` | **非缺口（已定口径）** | `PremiumOffer.TryPurchaseOffer` = 真实商店购买；「不做真实经济」（用户边界②）已拍板。若要改成「本地钱包 2000 购买」属新裁定 |
| 696 | `SharedEventDataManager.GetContext<T>` | **非缺口（连带已判 D）** | 机制已在（`Shell/LiveOpsSharedData.cs:368`）；唯一缺的 `EnergyContext` 已按「不做真实经济」判 D（F3 第4轮） |

---

## 六 收尾口径

- 每包交回时必须带：**改了哪些文件（逐行数）· typecheck 结果 · 新增断言清单 · 未接通的部分（如实）· 顺手发现**。
- 本域**没有任何 P0**（活动全部过期、EVENT 钮隐藏）⇒ 玩家可见度最高的是 **`430`（进主菜单自动弹每日奖励）+ `548`（活动钮标题/倒计时）+ `297/176/179/422/423`（图标与弹窗）** 这 8 行，建议排在 Wave 0/1 的最前面。
- 改完 `.cs` 一律 `bash 工具/typecheck.sh`；改动落在 `Shell/*` ⇒ 至少复跑 `ShellScene.Run` + `MainMenuScene.Run`（动了 `DailyData`/`MainMenuRuntime` 再加 `RewardsScene.Run`）。
