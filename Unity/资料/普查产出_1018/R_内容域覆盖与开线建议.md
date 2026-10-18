# R · 原版内容域 × 现有任务线 —— 覆盖矩阵与开线建议

> 2026-10-18 · **只读普查**（未跑 Unity、未跑生成器、未进游戏、未动 git、未改任何正本）。
> 触发 = 用户原话：「**派一个子代理去查一下解包资料，看看除了当前的这些任务线，是否还需要新增新的任务线？当前的任务线是否就包含了原版游戏的全部内容？**」
> 本文只回答两件事：**原版有哪些内容域** · **每一条现在有没有人管**。
> ⛔ 不评价「值不值得做」（铁律 11）；「不建议开线」只写**原版没有 / 用户拍板 / 判据在远端**三种理由。

---

## 一、结论（一句话）

**现有 12 条线已覆盖原版全部「主玩法 + 主界面 + 底层系统」；但还缺 2 个玩家可见的功能域没有任何线在管 —— ① 活动/运营页（Gacha 抽卡 · Raid · Expansion Pass 战令）② 新手引导全流程（菜单侧指针/高亮/阶段推进）。建议新开 2 条线，另有约 6 个域属于「实现已建、但当前台账里没有归属项」的记账缺口，补指针即可、不必开线。**

- **必须开线**：`活动·LiveOps 线` · `新手引导线（FTUE）`
- **不必开线（补指针 / 挂远端 / 用户拍板不做）**：见 §五
- **覆盖矩阵**：§三（37 行，每行带载体证据）
- **缺口清单**：§四

---

## 二、搜索面（⛔ 报「没有」之前先看这里）

### 2·1 查过的目录（全部实读/实列，非凭印象）

| 目录 | 数量（现读） | 用途 |
|---|---|---|
| `d:/2/新解包资源/assets_full/` | **84 个 `bundle_*` 目录**（其中 **15 个 `bundle_scenes_scenes_*`**） | 按功能分包枚举内容域 |
| `bundle_menus_assets_all/GameObject/` | **16,768 个 JSON** | 原版全部 UI 预制体（窗口/页签/行族） |
| `bundle_menus_assets_all/MonoBehaviour/` | **35,014 个** | 原版窗口组件实例 |
| `d:/2/Warpforge_code/Scripts/Assembly-CSharp/*.cs` | **1,870 个签名桩**（⚠️ 不是 `CLAUDE.md` 里写的 2294 —— 那是含嵌套类的旧口径） | 原版类名全集 |
| `d:/2/tools/decomp_full/*.c` | **25,096 个方法体** | 行为判据（调用点 / 显隐 / 开窗链） |
| `d:/4/Unity/资料/说明书/04_界面UI/` | `菜单全树.md`（16,768 GO 全树）· `主菜单全树.md` · `界面索引.md` | 原版 UI 根节点全集 |
| `d:/4/Unity/资料/任务线/` | **63 份 md / 12 个线文件夹** + README §四 | 现有线清单（唯一权威） |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/*.cs` | **127 个类** | 我们已建了哪些窗/类 |
| `d:/4/Unity/资料/普查产出_1017/UI/` | `U1`–`U4` 四份逐屏普查 + `R1`–`R7` 判据册 | 已有普查的结论（不重复查） |

### 2·2 查过的关键词（类名 grep / 任务线 grep / 代码 grep）

`Achievement · Trophy · Rank · Leaderboard · Alliance · Guild · Clan · Friend · Chat · Social · Mail · Inbox · Message · Notification · News · Announcement · Referral · Feedback · Rate · Report · Support · Legal · Privacy · Terms · Maintenance · Save · Profile · Account · Login · Register · Ads · Ad · IAP · Purchase · XSolla · Steam · Webshop · Offer · Price · Draft · Forge · Craft · Upgrade · Gacha · Crate · Raid · Survival · TwoSides · ExpansionPass · BattlePass · LiveOps · Mission · Daily · Streak · Campaign · Shop · Booster · Gold · VIP · Tutorial · FTUE · Video · Music · BGM · Localization · Analytics · Firebase · PlayFab`

### 2·3 关键判据路径

- 原版窗口类全集 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`（`grep -iE 'menu|window|panel|page|tab|dialog|popup|modal'` 命中 **约 150 个类**）
- 原版事件窗继承链 → `decomp_full` 的 class 声明：`GachaEventTab/RaidEventTab/ExpansionPassEventTab : WindowTabBase<BaseParentEventWindow>, ILiveOpsTab` · `TwoSidesEventWindow : LiveOpsEventWindowWithTabs<TwoSidesEvent>` · `DraftModeMenuDemo : LiveOpsEventWindowWithTabs<IDraftEvent>`
- 现有线边界 → `资料/任务线/README.md` §四 + 各线 `任务总表.md` + `界面线/准则与做法.md:13`（明确把「多人界面」写进本线范围）

---

## 三、矩阵：原版内容域 × 现有任务线

> **状态定义**：`已覆盖` = 有一条线在**持续维护**它（有 T 项或明确写进该线准则/施工图）·
> `部分覆盖` = 主体有人管、**关键一半没人管** · `无人覆盖` = 没有任何线的总表/准则提到它。
> ⚠️ **「已覆盖」不等于「已做完」** —— 大部分 T 项本身还是 `✗/◐`，那属于正常待办。

### 3·1 核心玩法

| # | 内容域 | 载体证据 | 现有线 | 状态 | 说明 |
|---|---|---|---|---|---|
| 1 | **战斗规则引擎** | `RuleEngine/Core/*` · `decomp_full/BattleManager__*` · `AbilityLogic` · `SimpleAI` | **引擎线 T01–T11** | ✅ 已覆盖 | 伤害/条件/时机/关键词/AI/逐卡对账 |
| 2 | **战斗 HUD · 输入 · 表现 · 动画/演出** | `bundle_scenes_scenes_battlearena*`（13 场）· `BattleDriver.cs`（1.7 万行）· `bundle_battlesharedresources` | **战斗线 T01–T21** | ✅ 已覆盖 | 含逐卡演出 T21、镜头 T12、换牌 T10、教程 T11 |
| 3 | **战场环境 / 光照 / 后处理 / 战场 prefab** | `bundle_battleprefabs_vfxandmisc` · 13 个场景包 · `资料/战场场景线_交接.md` | **战场线**（⏸ 用户 2026-10-12 暂停）+ 战斗线 | ✅ 已覆盖（暂停中） | 四层搬运链已做完，剩验收 |
| 4 | **特效 / shader / 粒子** | `bundle_shaders_assets_all` · `bundle_tweenandshakes` · `WarpforgeVFX` · `特效还原台账.tsv` | **特效线 T01–T14** | ✅ 已覆盖 | 逐 Effect 对拍 |
| 5 | **卡牌数据 / 卡面 / 图标 / 关键词** | `bundle_*cardassets`+`*cardanims`（13 阵营 × 2）· `d:/2/Warpforge部队卡片` · `resources.assets` | **卡牌线 T01–T08** | ✅ 已覆盖 | 数据尺子 = PnP 卡图 |
| 6 | **卡牌制作知识（SO / id / 动画特效链）** | allcards SO 快照（**1,351 张，引自 `卡牌ID逆推线` 的既有普查，本轮未重数**）· `资料/卡牌制作_正本.md` | **卡牌制作知识线 K02–K12** | ✅ 已覆盖 | 2026-10-18 由「逆推」升级为常驻知识线 |
| 7 | **预组卡组 / 起始卡组** | `bundle_prebuiltdecks_assets_all`（**236 个 MonoBehaviour**，现数）· `bundle_deckselectionbuttons` | 卡牌线 T06 + 界面线 T26 | ✅ 已覆盖 | — |
| 8 | **音频（战斗 SFX / BGM / 语音 / 壳侧音效 / 菜单音乐）** | `bundle_soundcollection` · `bundle_audiocontrol` · `bundle_menumusic`（`Main Theme`/`Menu Idle Theme`）· `bundle_tutorialwarlordchats` | **音效线 S01–S03**（壳侧）+ **战斗线 T06/T18**（战斗 BGM + 语音） | ✅ 已覆盖 | 菜单音乐已在 `ShellRuntime.cs:802-811` 接线（⚠️ 无显式 T 项，见 §四·B） |
| 9 | **联机 P2P / 大厅 / 匹配 / 掉线重连 / 录像** | `CardPresentation/Net/*` · `decomp_full/MatchMakerManager__*` | **联机线 T01–T07** | ✅ 已覆盖 | 真网络层已通，欠两台机器真连（挂起 P01） |
| 10 | **本地录像 / 回放** | `Battle/ReplayStore.cs` · `资料/联机P2P_设计与交接.md` | 联机线 T04 | ✅ 已覆盖 | — |

### 3·2 主界面与功能页

| # | 内容域 | 载体证据 | 现有线 | 状态 | 说明 |
|---|---|---|---|---|---|
| 11 | **主菜单 / 外壳 / 左竖导航 / 安全区 / 加载黑场 / 压暗层 / 指针层** | `bundle_scenes_scenes_mainmenuwarpforge` · `bundle_mainmenualwaysloaded` · `资料/阶段二_Shell_原版规格.md` | **界面线 T02 / T12 / T16** | ✅ 已覆盖 | — |
| 12 | **收藏（卡组/卡牌/卡背/异画/风格）+ 卡牌详情窗** | `bundle_cosmeticscardbacksimages` · `bundle_flavourframes` · `资料/阶段二_卡组线_原版规格.md` | 界面线 **T03 / T05 / T25** | ✅ 已覆盖 | 卡背点击 → `ItemDisplayWindow` 已通 |
| 13 | **卡组编辑（含导入 / 导出 / 分享）** | `Deck Editing Menu` prefab · `bundle_deckselectionbuttons` | 界面线 **T01 / T23** | ✅ 已覆盖 | — |
| 14 | **商店（卡包 / 每日 / 物品 / VIP / 网页商店按钮）** | `Shop Menu Variant` · `Card/Daily/Item Shop Tab` · `bundle_specialofferscontentlocal` | 界面线 **T04** | ◐ 部分覆盖 | **页签表由服务端 store 列表驱动**（`ShopWindow__SetupTabs.c`）；本地另有 `Gold Tab` / `Card Shop VIP Tab Variant` 两份页 prefab **没建** —— 判据在远端（见 §五） |
| 15 | **锻造厂（Forge）** | `Forge Tab` · `bundle_campaignrewardbackgrounds`（借用）· `ForgePersistence` | 界面线 **T04 / T21** | ✅ 已覆盖 | — |
| 16 | **战役（Campaign 地图 / 47 节点 / 连线）** | `Campaign Tab` · `CampaignNode/CampaignGraph` · `bundle_campaignrewardbackgrounds` | 界面线 **T04 / T20** | ✅ 已覆盖 | — |
| 17 | **日常任务 / 里程碑 / 周常 / 重摇** | `Missions Tab` · `Mission*` 40+ 类 · `资料/日常_原版规格.md` | 界面线 **T04** | ✅ 已覆盖 | — |
| 18 | **奖励 / 每日奖励 / 连续登录** | `Reward Window` · `Daily Reward Popup` · `Daily Streak Popup`/`Window` | 界面线 **T04 / T19** | ✅ 已覆盖 | — |
| 19 | **卡包开包（翻牌 / 稀有度演出 / 快速开包）** | `bundle_boosterpacks_assets_all` · `BoosterPackOpenWindow` | 界面线 **T19** | ✅ 已覆盖 | 尾巴：开包音效归音效线 |
| 20 | **玩家档案窗六页**（Profile / Avatar / Title / Achievements / Ranked / Battle Log） | `Player Profile Window` · `bundle_staticgeneralassets`（`ACH1`–`ACH102` + `AllAchievements`） | 界面线（**早批次实现，台账无显式 T 项**） | ✅ 已覆盖（账目缺口） | `AchievementsMenu` = Trophies 页；102 条成就数据**本地真有** |
| 21 | **社交（联盟 / 好友 / 聊天）** | `Social Submenu Variant` · `bundle_alliancesbadgesimages` · `Alliance*/Chat*/Friends*` 60+ 类 | 界面线（早批次 + N56/N60 新账） | ✅ 已覆盖（账目缺口） | 服务器数据恒空是既定口径 |
| 22 | **排行榜**（遭遇 / 经典 / 轮抽 / 嵌入版） | `RankedSkirmishLeaderboardPopup` 等四根 · `bundle_rankeddivisionicons` | 界面线（早批次） | ✅ 已覆盖（账目缺口） | 轮抽榜的 `Armies` 钮原版点不动，已照原版处理 |
| 23 | **收件箱 / 公告** | `Inbox Menu` · `bundle_inboxmessagesheaderslocal` · `NewsPieceData`/`BalanceNewsData` | 界面线（早批次） | ✅ 已覆盖（账目缺口） | 空态照原版建（消息由服务端下发） |
| 24 | **推荐人（Referral）** | `Referral Popup`（注意：在 `bundle_generalgamewindows_assets_all`） | 界面线（早批次） | ✅ 已覆盖（账目缺口） | — |
| 25 | **设置（画面 / 音频 / 语言 / 账户 / 手柄 / 反馈问卷）** | `Settings Menu` · `AccountTab` · `GraphicsTab` · `MediaTab` · `BasicLoginWithEmailWindow` | 界面线 **T04 / T17 / T18** | ✅ 已覆盖 | Account 页六态、登录弹窗均已建；外链只记日志 |
| 26 | **字体 / 本地化 / 13 语言** | `bundle_fonts` · `localization_assets_all` · `官方本地化_en+zh-CN.json` | 界面线 **T07 / T08** | ✅ 已覆盖 | — |
| 27 | **滚动 / 裁切 / 命中 / 指针层** | `资料/阶段二_滚动与指针_原版规格.md` · `Shell/PointerLayer.cs` | 界面线 **T09 / T16** + 战斗线 T16 | ✅ 已覆盖 | — |
| 28 | **战斗入口四窗（练习 / 遭遇 / 排位 / 找对手）+ 12 秒匹配链** | `Practice Mode Menu` · `SkirmishEventWindow` · `RankedEventWindowV2` · `Searching Opponent` | 界面线 **T04 / T22** + 联机线 | ✅ 已覆盖 | — |
| 29 | **排位 / 段位 / 赛季三窗** | `Ranked Division Change/New Season/Season Ended Window` · `bundle_rankeddivisionicons` | 界面线 **T18** | ✅ 已覆盖 | 赛季倒计时按用户口径不建 |
| 30 | **活动承载窗（BaseEventPopup / ParentEvent / TwoSides / Energy / DailyStreak）** | `BaseParentEventWindow` · `Two Sides Event Window` · `EnergySinglePlayerOnlyEventWindow` · `Liveop Event Help` | 界面线 **T15 / T18 / T28** | ✅ 已覆盖 | ParentEvent = TwoSides 两页（Select Team + On Going） |
| 31 | **FTUE 选阵营送起始卡组** | `Choose Army Deck FTUE Window` · `bundle_prebuiltdecks`（4 副 Starter） | 界面线 **T26** | ✅ 已覆盖 | — |
| 32 | **反馈问卷** | `Give Feedback Popup menu` · `FeedbackQuestionItem` | 界面线 T18 | ✅ 已覆盖 | — |

### 3·3 🔴 缺口（无人覆盖 / 部分覆盖）

| # | 内容域 | 载体证据 | 现有线 | 状态 | 说明 |
|---|---|---|---|---|---|
| 33 | **新手引导全流程（菜单侧）** —— 指针 / 聚光灯 / 阶段推进 | `decomp_full/MainTutorialManager__*.c`（14 个方法）· `TutorialManager__*.c` · **7 个 `FlowTutorialStage` 子类**（`TutorialStageGetMission`/`PlayRanked`/`SelectArmy`/`LevelUpCard`/`GetCampaignNode`/`ClaimForgeFirstLevel`/`TutorialFinished`）· `bundle_generalgamewindows/GameObject/TutorialPointer{Base,Menu,Combat,Wildcards}` · `菜单全树.md` 的 `Tutorial highlight`（`ScreenHighlightPosition`）· `bundle_tutorialso`（**7 个 Stage SO**：`Warpforge_TutorialStage1..6` + `Scenary_Test`） | 战斗线 **T11**（只覆盖战斗内）+ 界面线 **T18/T26**（只覆盖弹窗/选卡组） | 🔴 **部分覆盖（主体无人管）** | 战斗内教程已接（`TutorialScript.cs` 1523 行 + `TutorialOverlay.cs` 573 行，只剩 7 条缺口）；**「菜单 → 战役 → 任务 → 排位 → 锻造 → 升级卡」这一整条阶段链、4 个指针 prefab、全屏聚光灯，全仓 0 实现、0 账** |
| 34 | **Gacha（抽卡 / 扭蛋）活动页** | `decomp_full`：`GachaEventTab : WindowTabBase<BaseParentEventWindow>, ILiveOpsTab` · prefab 根 `Gacha Tab`（`菜单全树.md`）· `GachaCratePanel` / `GachaRewardsPanel` / `GachaRewardContainer` / `GachaDrawerHolder` · `bundle_menus_assets_all/VideoClip/Gacha Crate Opening.mp4` · 抽卡特效 `<FRAC>_CardDraw`（特效线 T09：13 阵营只有 2 件） | **无** | 🔴 **无人覆盖** | 页面 prefab 本地有、数据在 LiveOps 远端。我们代码里 `Gacha` 只作为**货币图标**出现（`MainMenuRuntime.cs:1676,1701`） |
| 35 | **Raid 活动页** | `RaidEventTab : WindowTabBase<BaseParentEventWindow>, ILiveOpsTab` · prefab 根 `Raid Progress Tab` · `Raid Progress Bar` / `Raid Reward Container` / `RaidEventRewardItemContainer` / `RaidRewardSelector` / `RaidOrArmyReleaseMenuContainer` | **无** | 🔴 **无人覆盖** | 同上；`Raid` 在我们代码里只有 `raidMedals` 货币 |
| 36 | **Expansion Pass（战令 / 扩展通行证）** | `ExpansionPassEventTab` / `ExpansionPassMissionsTab : ILiveOpsTab` · prefab 根 `Expansion Pass Tab` / `Expansion Pass Missions Tab` / `Expansion Pass Progress Bar` · `ExpansionPassPurchasePremiumPanel`（`Purchase Premium Window` 本身已建）· `ExpansionPassRewardContainer` | **无** | 🔴 **无人覆盖** | 我们只建了**零件**（`ExpansionPassPointDrawer`/`PremiumDrawer` 在 `ItemDrawer`，活动词条两条已接）——**整页没有** |
| 37 | **广告（看广告得宝石）** | `AdsHandler`（`ShowRewardedAd`/`IsAdReady`/`AdsEnabledByPlayer`）· `AdSettingsData`（`adsEnabled`/`gemsPerAd`/`maxDailyAds`）· `AdsPlayerData` | **无** | 🔴 无人覆盖（但**不该开线**，见 §五） | 需要广告 SDK + 远端配置 + 账号体系 |

### 3·4 已确认「原版有、但我们不做」的（详见 §五）

| 内容域 | 载体证据 | 理由 |
|---|---|---|
| 内购 / 真实经济（Steam / XSolla / PlayFab 商店 / Webshop） | `InAppPurchase*` · `XSolla*` · `WebShopOpenButton` · `PurchaseableOfferContainer` | 🔴 **用户拍板**：资源固定 9999、不做真实经济（项目三条边界②）。按钮已建、点击出声 |
| 轮抽 / 选秀模式（Draft） | `DraftModeMenuDemo : LiveOpsEventWindowWithTabs<IDraftEvent>` · `bundle_draftpacks_assets_all` | 🔴 **用户拍板不做**（`远端挂起线/P03`）；模式卡与弹窗仍保留 |
| 过期活动 / 关服公告 | `GameSunsetPopup` · `Sunset Popup` | 🔴 **用户拍板**：过期活动不做（`远端挂起线/P04`） |
| 评分弹窗 / 愿望单 | `RatePopup`（`ShouldShowRatePopup` **0 调用者**）· `LaunchWishlistPopup` | ⚪ **铁律 11 例外①**：原版自己就没有可达入口 |
| 调试窗 | `DebugPlayWindow` / `DebugEventTogglerWindow` / `DebugRewardWindow` | ⚪ 正式版调试用，非玩家功能 |
| 玩家等级 / XP / 成就进度 / 督军精通 / 阵营分 | `PlayerLevel` · `WarlordMasteryContainer` · `FactionScore` · `Achievement.CurrentValue` | ⚪ **服务端 profile** ⇒ 应进 `远端挂起线`，不是新线 |
| 卡牌创建副本 / 升级「机制」 | `CraftingPanel` · `UpgradePanel` | 🔴 **用户拍板**：全解锁、一律最高档；两块面板**照原版恢复显示**但不可交互（2026-10-12 口径） |

---

## 四、缺口清单（按「玩家可见」分档）

### A 档 · 玩家可见、且**现在就能做**（判据在本地）—— **建议开新线**

#### A1 · 活动 · LiveOps 线（建议新开，3 件）

| 件 | 玩家看到什么 | 判据在哪 | 粗估工作量 | 优先级 |
|---|---|---|---|---|
| **L01 Gacha 抽卡活动页** | 活动窗里多一个「抽卡」页：抽卡券消耗、开箱动画、稀有度演出、抽卡奖励领取 | prefab：`Gacha Tab`（`bundle_menus_assets_all/GameObject/`）+ `GachaCratePanel`/`GachaRewardsPanel`/`GachaDrawerHolder`；行为：`decomp_full/GachaEventTab__*.c` + `GachaCratePanel__*.c`；视频：`Gacha Crate Opening.mp4`（同包 `VideoClip/`）；特效：`<FRAC>_CardDraw`（特效线 T09，13 阵营缺 11） | **大**（整页 + 开箱动画 + 抽卡特效依赖） | 🔴 高（三类活动里最显眼） |
| **L02 Raid 活动页** | 活动窗里多一个「Raid」页：进度条、任务/分数、奖励档领取 | prefab：`Raid Progress Tab` + `Raid Progress Bar` / `Raid Reward Container` / `RaidRewardSelector`；行为：`decomp_full/RaidEventTab__*.c` / `Raid*Controller__*.c` | **中**（页 + 进度条 + 奖励抽屉，无独立动画） | 🟡 中 |
| **L03 Expansion Pass（战令）** | 活动窗里多一个「通行证」页：等级进度、免费/付费双轨奖励、买 Premium | prefab：`Expansion Pass Tab` / `Expansion Pass Missions Tab` / `Expansion Pass Progress Bar` / `ExpansionPassPurchasePremiumPanel`；行为：`decomp_full/ExpansionPass*__*.c`；零件已有（`ItemDrawer` 两个 Drawer、`PurchasePremiumWindow`、两条活动词条） | **大**（两页 + 双轨 + 购买态） | 🟡 中 |

> **为什么单开一条线而不是塞进界面线**：
> ① 界面线已 **11 件 / 129 笔**（README §四现读），再加 3 个整页会把它彻底压垮；
> ② 这三个域的判据**同源**（LiveOps 远端事件数据 + `ILiveOpsTab` 基类），与界面线现有的「静态 prefab 逐值复刻」不是同一种活；
> ③ 它们与**特效线**（抽卡特效）、**音效线**（开箱音效）有明确跨线依赖，需要一条线做收口。
> **不新开也可以**：退而求其次 = 在界面线 `T04 功能窗族` 下加 3 个 T 项；但需要同时把 L01 的抽卡特效依赖挂到特效线 T09。

#### A2 · 新手引导线（FTUE）（建议新开，1 件大活）

| 件 | 玩家看到什么 | 判据在哪 | 粗估工作量 | 优先级 |
|---|---|---|---|---|
| **F01 新手引导全流程** | 第一次进游戏：菜单上的**聚光灯 + 指针 + 文字提示**一步步指「打一场 → 领任务 → 打排位 → 领锻造奖励 → 升一张卡 → 完成」；中途可跳过 | **阶段链**：`decomp_full/MainTutorialManager__{ctor,AddPointer,AddPointerWithDelay,ChangedWindowState,EnterLevelUpCard,GetState,SetSelectedArmy,ShouldGetInitialDeck,…}.c` + `TutorialManager__*.c`；**7 个阶段类**：`TutorialStageGetMission` / `TutorialStagePlayRanked` / `TutorialStageSelectArmy` / `TutorialStageLevelUpCard` / `TutorialStageGetCampaignNode` / `TutorialStageClaimForgeFirstLevel` / `TutorialStageTutorialFinished`；**指针 prefab**：`bundle_generalgamewindows_assets_all/GameObject/TutorialPointer{Base,Menu,Combat,Wildcards}`；**聚光灯**：`菜单全树.md` 的 `Tutorial highlight`（`ScreenHighlightPosition`，整屏黑 α.8235 + 挖洞，`fadeTime=.1`）；**教程战斗 SO**：`bundle_tutorialso_assets_all`（Stage1–6 + Scenary_Test） | **大**（阶段状态机 + 4 个指针 prefab + 聚光灯 + 跨窗驱动） | 🔴 高（玩家第一小时唯一的引导） |

> **现状**：战斗内教程**早就接了**（`RuleEngine/Core/TutorialScript.cs` + `BattleDriver` 教程段 + `TutorialOverlay.cs`，战斗线 T11 只剩 7 条缺口）；`TutorialModePopup`、`TutorialMessagePopup` 也已建。
> **缺口 = 菜单侧那一半**：指针 prefab（4 个）、`Tutorial highlight` 聚光灯、以及 `MainTutorialManager` 那 7 个阶段的推进状态机 —— **全仓 0 实现、0 账**。
> **为什么不并进战斗线**：它 90% 的触发点在**壳/主菜单/各功能窗**（`ChangedWindowState`），主体文件不在 `CardPresentation/Battle/**`。

### B 档 · 实现已建、但台账里没有归属项（**不开线，补指针**）

> 判据 = 「有没有一条线在持续维护它」。这些**都落在界面线范围内**（`准则与做法.md:13` 明写「多人界面」），
> 但 `任务总表.md` 的【未完成】和【已完成】两节**都没有它们的行** —— 新会话读表会以为它们不存在。

| # | 域 | 我们的实现 | 建议 |
|---|---|---|---|
| B1 | 玩家档案窗六页 | `Shell/PlayerProfileWindow.cs` + `ProfileTab/AvatarTab/TitleTab/AchievementsMenu/RankedTab/BattleLogTab` | 在界面线加一条 **📌 常驻指针**（不是待办）：「档案窗族已建，判据 → `资料/普查产出_0927/档案窗_*.md`」 |
| B2 | 社交（联盟/好友/聊天） | `Shell/SocialWindow.cs` + `AlliancesTab`/`FriendsTab`/`ChatTab`/`ChatPanel` | 同上，判据 → `资料/普查产出_0927/社交_联盟与好友页.md` · `资料/阶段二_多人界面_原版规格.md` |
| B3 | 排行榜四扇 | `Shell/LeaderboardWindow.cs` + `LeaderboardRow` | 同上，判据 → `资料/普查产出_0927/排行榜_*.md` |
| B4 | 收件箱 | `Shell/InboxWindow.cs` | 同上，判据 → `资料/日常_原版规格.md` §六 |
| B5 | 推荐人窗 | `Shell/ReferralPopupWindow.cs` | 同上 |
| B6 | 创建副本 / 升级两块面板 | `Shell/CardDetailPopup.cs`（照原版恢复显示、不可交互） | 同上（判据 → `资料/待办判据_卡面卡池与双语.md` 的 2026-10-12 订正段） |
| B7 | 菜单音乐 / 开场视频 | `Shell/ShellRuntime.cs:669`（intro）· `:802-811`（main_theme / menu_idle_theme） | 音效线的边界写着「只收壳侧 UI 音效」⇒ 音乐播放严格说**没有归属**。建议在音效线加一条 📌 常驻口径：「菜单音乐 + 开场/结算视频的**播放**已建；本线只管 cue」 |

### C 档 · 玩家可见、但**判据在远端 / 需外部服务**（→ `远端挂起线`，不开线）

| 域 | 缺什么 | 已查到哪一步 |
|---|---|---|
| 广告（看广告得宝石） | 广告 SDK + `AdSettingsData` 远端配置 + 账号 | `AdsHandler`/`AdSettingsData` 字段语义已可读（`adsEnabled`/`gemsPerAd`/`maxDailyAds`/`adsWatched`），但**没有任何本地配置实例**（`grep AdSettingsData` 只命中它自己的 ctor） |
| 玩家等级 / XP / 督军精通 / 阵营分 / 成就进度 | 服务端 profile 字段 | UI 已建；进度一律按 0 / `--` 渲染并如实标 |
| 商店页签族（`Gold Tab` / `Card Shop VIP Tab Variant`） | 服务端 store 列表（`ShopWindow__SetupTabs.c` 明确「按存档里的 store 列表逐个建」） | 三页是我们挑的（商店规格 §一·二 自陈）；两份未建页 prefab 本地有 |
| LiveOps 活动数据（Gacha/Raid/Pass/公告文案） | 远端事件数据 | prefab 与类结构全在本地；**页面可以先建壳 + 数据留空 + 出声**（先例 = T28 ParentEventWindow、T18 WhereToGetCards） |
| 多人后端（PlayFab / Firebase / 真匹配服务） | 服务端 | 已用自建 P2P 替代；`联机线` 负责 |

---

## 五、不建议开线的（逐条写清为什么）

| 候选线 | 为什么不开 |
|---|---|
| **内购 / 经济线** | 🔴 **用户拍板不做真实经济**（项目目标三条边界②：资源固定 9999、全解锁）。原版 UI 已有部分建成、按钮点击出声；这是**有意偏离**，不是缺漏 |
| **轮抽 / 选秀线** | 🔴 **用户拍板不做**（`远端挂起线/P03`）。模式卡、弹窗、`Draft Mode` 文案仍在；`bundle_draftpacks` 也已读 |
| **过期活动 / 运营公告线** | 🔴 **用户拍板**：过期活动不做（`远端挂起线/P04`）。`GameSunsetPopup` 保持不建 |
| **成就线** | ❌ 已有线覆盖：成就 = **档案窗第 5 页**（`AchievementsMenu`），102 条成就数据本地齐（`bundle_staticgeneralassets` 的 `ACH1..102` + `AllAchievements`）。只缺 §四·B1 那条指针 |
| **社交 / 好友 / 联盟 / 聊天线** | ❌ 已有线覆盖：全部是壳侧窗口（`Shell/SocialWindow.cs` 一族），属界面线；服务器数据恒空是既定口径 |
| **排行榜线** | ❌ 已有线覆盖：四扇全建（`LeaderboardWindow.cs`），属界面线 |
| **邮件 / 收件箱线** | ❌ 已有线覆盖：`InboxWindow.cs` 空态照原版建，属界面线（日常层） |
| **视频线** | ❌ 不够一条线：`bundle_videos` 6 条里 —— `Victory/Defeat/Draw` = 战斗结算（战斗线 T09 已接）；`Warpforge Intro` = 开场（`ShellRuntime.cs:669` 已接）；`Main Menu Practice/Multiplayer Video` 由 **`GameModeButtonDemoVersion__PrepareVideo`**（**demo 版**专用模式卡）使用 ⇒ 正式版无用；`Gacha Crate Opening` 归 L01。**只剩 `Menu_Chronomancer_Animation.mp4` 用途未查清**（见 §六） |
| **音频 / 音乐线** | ❌ 已有线覆盖：音效线 + 战斗线 T06/T18；只差 §四·B7 那条口径 |
| **战场线** | ⏸ 已存在、**用户暂停** —— ⛔ 不要再新开一条 |
| **语言 / 本地化线** | ❌ 已有线覆盖：界面线 T08（13 语言 + 官方词条） |
| **账号 / 登录线** | ❌ 已有线覆盖：`SettingsWindow` 的 `AccountTab` 六态 + `BasicLoginWithEmailWindow` 已建（判据 `AccountTab__Refresh.c` 的 6 处 `SetActive`） |
| **调试 / 工具线** | ❌ 已有线覆盖：工具·文档线；调试窗不是玩家功能 |
| **广告线** | ⚪ 判据在远端 + 外部 SDK ⇒ 进 `远端挂起线`，不是新线 |

---

## 六、没查清的部分（⛔ 不猜）

1. **`Menu_Chronomancer_Animation.mp4` 用在哪** —— `bundle_videos_assets_all/VideoClip/` 有这条（1920×1080），但 `bundle_menus` / `bundle_generalgamewindows` / `mainmenuwarpforge` 场景里 **`grep` 该文件名 0 命中**，也没有 `VideoPlayer` 组件引用它。可能是活动/商店的宣传片，也可能是废弃资源。**判据缺口 = 需要跨全部 84 个 bundle grep `m_VideoClip` 的 PPtr**（本轮没做）。
2. **`Main Menu Multiplayer Video` 是否真的只在 demo 版用** —— 唯一证据是 `GameModeButtonDemoVersion__PrepareVideo` 这个类名 + 方法名；**没有读到方法体实参**（`.c` 里是 `DAT_` 间接引用）。如果正式版也用它，那它属于界面线 T02 的一条尾巴。
3. **Gacha / Raid / Expansion Pass 的页面 prefab 是否 1:1 可用** —— 只从 `菜单全树.md` 的根节点确认了名字与矩形（`Gacha Tab` / `Raid Progress Tab` / `Expansion Pass Tab` / `Expansion Pass Missions Tab`），**没有逐节点 dump**。开工前必须照 `menu_dump.py` 重新取真值（铁律 3）。
4. **`ILiveOpsTab` 的注册机制** —— `BaseParentEventWindow__SetupTabs.c` 遍历 liveops 事件 → `TabButtons.AddTabButton`，但**「哪种事件实例化哪个 Tab prefab」的映射表**没读到（可能在 `LiveOpsEventData` 的 `tabPrefab` AssetReference 里，未逐条解）。这决定 L01–L03 能否「先建壳」。
5. **`Survival` 是否有独立活动窗** —— 类全集里只有 `SurvivalEventWindowEnemyMarker`（`MonoBehaviour`，战场标记），没有 `SurvivalEventTab`/`Window`。**「生存模式」是不是只作为战斗修饰存在**，没查到实据。
6. **`TutorialPointer*` 四个 prefab 的归属** —— 在 `bundle_generalgamewindows_assets_all` 里确认存在（`TutorialPointerBase/Menu/Combat/Wildcards`），但**它们各自在哪种教程阶段被用**没查（需要读 `MainTutorialManager` 的方法体，本轮只看了方法名清单）。
7. **`任务线/README.md` §四 的「11 件 / 129 笔」等件数** —— 那是 2026-10-15 建线读数，本轮**没有重算**（各线总表表尾才是现读）。

---

## 七、附：本次普查的「不重复查」交回（下一轮直接用）

- 原版窗口类全集 ≈ **150 个**（`grep -iE 'menu|window|panel|page|tab|dialog|popup|modal'`，1,870 个桩里）。
- 原版**事件页三件**（Gacha / Raid / Expansion Pass）的**类结构已确认**：
  `XxxEventTab : WindowTabBase<BaseParentEventWindow>, ILiveOpsTab`（`ExpansionPassMissionsTab` 同）。
- 原版**没有** `SurvivalEventWindow` / `Tournament` / `Guild`（用 `Alliance`）/ `Spectate` 这些类（`grep` 全 0）。
- `bundle_staticgeneralassets_assets_all` 里 **只有 2 类非成就资产**：`AllAchievements` + `CardFramesByArmy`。
- `bundle_menumusic_assets_all` 只有 2 条曲子（`Main Theme` / `Menu Idle Theme`），**都已在 `ShellRuntime.cs:802-811` 接线**。
- `bundle_videos_assets_all` = 6 条 mp4（胜负平 3 条 + 主菜单 2 条 + Chronomancer 1 条）；`bundle_menus_assets_all/VideoClip` = `Gacha Crate Opening.mp4`（1 条）。
- 教程侧资产：`bundle_tutorialso` = **7 个 SO**（`Warpforge_TutorialStage1..6` + `Scenary_Test`，字段 `playerStarts/bg/hideCemetery/hidePlayerDeck/playerDeck`）；`bundle_tutorialwarlordchats` = 纯 `AudioClip`（语音）。
