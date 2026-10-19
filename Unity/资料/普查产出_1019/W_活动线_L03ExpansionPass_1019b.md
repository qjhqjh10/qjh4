# W · 活动线 L03 · Expansion Pass 战令页 —— 写手报告（2026-10-19）

> 本轮做 **L03 的两页**：Pass 页（`Expansion Pass Tab`）+ Missions 页（`Expansion Pass Missions Tab`，协调台追加要求）。
> 验证口径：只跑了秒级类型检查（`TMPDIR=/tmp/wf_l03b bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**）。
> **未跑** Unity 批处理、任何 `*.Run` 自检、player、真机点击；未动 git；未改任何正本。
> ⚠️ 本轮**没有**出现「别人文件半成品」的假错（typecheck 四次都是 0/0）。

---

## 一、结论（建了什么 / 原版判据）

### 1·1 建了什么

**新窗口 `ExpansionPassEventWindow`（承载窗外壳 = `Parent Event Window` 那一族 + 恰好一个 Pass 页）**：

| 区块 | 内容 |
| --- | --- |
| 承载窗外壳 | `Menu Dark Background`（点暗底关窗）· `Content Area/Background`（沿用 `ParentEventWindow` 的渐变常量）· `Tab Buttons` 左栏（底板四段渐变 + 阴影 + 母版页签[关] + 唯一运行期页签「Pass / 通行证」）· `Tabs` 容器 · `Shadow (1)`（出厂关）· 吸收层 |
| 页根 `Expansion Pass Tab` | 根矩形 **x 163.7875..1920.2125 · y 0.1795..1079.8205**（与 `Gacha Tab` / `Raid Progress Tab` 逐位相同） |
| `Background Black` / `Background Image` | 纯黑满页底 + liveops 背景图节点（**无图不画**；原版取不到 sprite 时把 `Image` 组件 `set_enabled(false)`，GO 仍 active —— 我们同态） |
| `Purchase premium Panel`（**付费档入口**） | `Background`（`WF_Campaign_Info_Background` 九宫 335,0,395,0）· `Last/Fisrt Premium Reward`（两组各带旋转与 `Card Drawer` 空容器）· **`IsNotPremium`**：Title（Expansion Pass）/ Subtitle（Get **Exclusive** cards!）/ `Generic Simplified UI Button`（`UI_Button_Mulligan` + `Border Line FX` 高亮 + `Get now!`）· **`Is Premium`**（出厂关）：Title（You are Premium!）/ `Extra points message`（`ExpansionPassPointDrawer` 空容器 + `+--% Extra Points`） |
| `Rewards Scroll View`（= `ExpansionPassRewardSelector`） | `Viewport`（`ViewportClip`，`UIMask` alpha=0 不画）+ `Rewards Content` + **奖励容器模板 `Expansion Pass Reward Container`（关着）** |
| `Expansion Pass Reward Container`（模板） | `Level up`（底 + 箭头 + 升级价签钮 + `Level up now:`）· `Premium Glow`（节点无图）· `Collect Highlight Effect`（图 α=0）· `RewardTransform` + `Icon Extra rewards` · **`Personal Progression`**：`Expansion Pass Progress Bar`（底/填充 0 宽/端帽 + `Points --/--`）+ `Claim Button Container`（`Claim Button` + 高亮框 + `Buy premium`[关] + `Level`/`Lock`[关]）· `CollisionButton` · `Culling Reference Object` |
| `Header`（= `RaidHeaderController`） | `Title`（prefab 出厂原文 `Event title`）· `Help Icon Player`（= `eventHelpButton`，点击出声）· `Army Icon`（无图）· `Time Remaining`（`Ends in:`）→ 时钟 + `Time --`（`TimerDisplay` 挂上） |
| `Where get cards` | `Generic Simplified UI Button (1)` + 词条 `Event_Description/BattlePass/WhereButton`，**真接** `WindowsManager.OpenWhereToGetCards()` |
| `Debug Add points` | **出厂 INACT** ⇒ 建出来、关着 |
| 开窗入口 | `WindowsManager.PrefabRefExpansionPassEvent = "Expansion Pass Tab"` + `WindowsManager.OpenExpansionPassEvent()`（照 `OpenGachaEvent` / `OpenRaidEvent` 的 `OpenByRef` 形状） |
| **第二页 `Expansion Pass Missions Tab`** | `Event Image`（= `warlordImage` · liveops `LoadAsset(事件,2)`，无图）· `Header`（纯黑 `Background` + `Title` + INACT `Help Icon Player (1)` + `Army Icon` + `Ends in:`/时钟/`Time --`）· `Daily Missions`（`Mission Header` 渐变条 + `name` = `Missions/Title/WeeklyChallenges` + INACT `info` + **周常卡模板 `Daily Mission Container Ref`（关着）**：底图/描述/计时/奖励抽屉/进度条/Collect/重掷/调试钮）· `Daily Skulls Mission Container`（旋转卡底 + 渐变条 + `Missions/Title/DailySkulls` + ⓘ tooltip `Missions/Info/DailySkulls` + 骷髅图/计数 + 5 个里程碑点 + 奖励抽屉/Collect/Timer + INACT `debug_buttons`） |
| 左页签栏（两页） | 两颗页签：**Pass**（`Event_Description/ExpansionPass/TabTitle`）与 **Missions**（`MainMenu/General/Missions` —— 我们挑的兜底）；点击互斥切页（`ShowPage`），选中高亮跟随 |

### 1·2 原版判据（逐条可复跑）

- **类桩**：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/{ExpansionPassEventTab,ExpansionPassRewardSelector,ExpansionPassRewardItemContainer,ExpansionPassProgressClaimButtonController,ExpansionPassPurchasePremiumPanel,ExpansionPassRewardDrawerController,ExpansionPassPointDrawer,ExpansionPassPremiumDrawer}.cs`（字段表逐条读过）。
- **行为（反编译方法体）**：
  - `BaseParentEventWindow__SetupTabs.c` / `__CreateEventTab.c` —— Expansion Pass 是 `Parent Event Window` 里由 liveops 事件列表动态建的一个 tab（与 Gacha / Raid 同构，不是独立窗口）。
  - `ExpansionPassEventTab__OnOpen/__Initialize/__GetCardsClick/__Reset.c` —— `Initialize` 逐句见文件头 ②（`rewardSelector.Initialize` → `backgroundImage.sprite = LoadAsset(事件,1)` 且 `enabled = sprite != null` → `premiumPanel.Initialize` → `headerController.Initialize` → `getCardsButton` 监听 + 一个远端配置布尔决定它显不显示）。
  - `ExpansionPassPurchasePremiumPanel__Initialize.c` —— `HasPremium` 二选一开 `IsNotPremium` / `Is Premium`；清空两个卡锚点后按 `Milestones.First/Last` 的 premium 奖励各 `ItemDrawer.Draw` 一件；已买时只画 Last，并把 `ExtraPercentaje` 按 `PremiumPercentageBonus` 格式化、`DrawForArmy(army)`。
  - `ExpansionPassPurchasePremiumPanel__PurchasePremiumClick.c` —— `GetComponentReference(premiumOfferData, 0xc)` → `WindowsManager.OpenWindow(...)`（**远端引用**）。
  - `ExpansionPassRewardSelector__CreateRewards.c` —— 按 `Milestones` 逐件 `Instantiate(rewardItemContainer, contentAnchor)`；**里程碑为空 ⇒ 一件都不建**。
  - `ExpansionPassRewardItemContainer__Initialize.c` —— `highlightReward.SetActive(false)`；`premiumGlow.enabled = milestone.IsPremium`；进度条/领取钮/奖励抽屉依次 `Initialize`；`levelUpContainer.SetActive(GetCostToLevelUp(...).amount > 0)`。
  - `ExpansionPassProgressClaimButtonController` 的 `stateDefinitions`（**4 档**，target PPtr 逐个解成节点名）：0 → Level+Lock；1 → ClaimButton + BuyPremium（高亮关）；2 → ClaimButton；3 → Level。出厂 = ClaimButton active、Level/BuyPremium INACT。
  - `RaidHeaderController` 复用：`Awake` 给 `eventHelpButton` 挂 `ClickEventHelpButton`（`HasComponent(事件, 0x1e)` 才开远端帮助窗）；`Initialize` = `GetLabel(事件)` / `LoadAsset(事件,5)` / `UpdateText` / 计时协程；`UpdateText` 按服务器时间切 `TimeEndsIn` / `TimeStartsIn` 并控制 `gracePeriodTooltip`。
- **prefab 节点树**：
  - `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Expansion Pass Tab" --depth 14 --relative` → 落地副本 `d:/4/_tmp_view/expansion_pass_tab_dump.txt`（229 行）
  - `… "Expansion Pass Reward Container" --depth 16 --relative` → `d:/4/_tmp_view/ep_Expansion_Pass_Reward_Container.txt`
  - `… "Expansion Pass Progress Bar" --depth 16 --relative` → `d:/4/_tmp_view/ep_Expansion_Pass_Progress_Bar.txt`
  - **第二页**：`… "Expansion Pass Missions Tab" --depth 16 --relative` → `d:/4/_tmp_view/ep_Expansion_Pass_Missions_Tab.txt`（240 行）
  - 另用临时脚本逐节点读了 **原始 RectTransform 五元组 / `Image.m_Color` / `Localize.m_Term` / `Gradient2._effectGradient` / 组件字段 PPtr**（`d:/4/_tmp_view/ep{,_m}_{walk,colors,terms,fields,header,extra,grad,grad2}.py`）。
- **Missions 页判据**：类桩 `ExpansionPassMissionsTab.cs`（`headerController` / `dailyTickets` / `dailyMissionAnchor` / `missionContainerReference` / `warlordImage`）；
  反编译 `ExpansionPassMissionsTab__{Initialize,OnOpen,ToFocus,Reset,SetEventTab,RefreshMissions}.c`：
  `SetEventTab` = 存 event + `LiveOpsManager.GetHandler<Missions>()` + `headerController.Initialize` + `warlordImage.sprite = LoadAsset(事件,2)`；
  `OnOpen`/`ToFocus` = 基类 `OnOpen()` + `RefreshMissions()`；`RefreshMissions` = 过滤可做任务，**空 ⇒ `CloseTab`**，
  否则 `dailyTickets.Initialize(first)` + 最多 3 条 `Instantiate(missionContainerReference, dailyMissionAnchor)`，末尾关模板。
- **本地化键**（`官方本地化_en+zh-CN.json` 的 `terms`；运行时 `Resources/official_loc_13lang.json` **逐条命中**）：
  `Event_Description/ExpansionPass/{TabTitle,Title,Subtitle,GetNow,YouArePremium,ExtraPercentaje,LevelUp,BuyPremiumToClaim}` ·
  `Event_Description/General/EndsIn` · `Event_Description/BattlePass/WhereButton` · `MainMenu/General/Claim`。
- **承载窗外壳**：直接复用 `Shell/ParentEventWindow.cs` 的 **public 常量**（矩形 / 渐变 / 页签几何 / 颜色 / art 名），⛔ 没有抄第二套判据。

### 1·3 有意偏离（照实记录，⛔ 不冒充原版）

1. **独立窗口 vs tab 页**：原版 Expansion Pass 是 `BaseParentEventWindow` 里由 liveops 事件列表动态建的一个 tab；本地**没有 liveops 事件列表** ⇒ 建「承载窗外壳 + 恰好一个 Pass 页」的等价形态。根名沿用原版**页** prefab 的 `Expansion Pass Tab`。
2. **空数据显示**：原版 `ExpansionPassRewardSelector.CreateRewards` 在里程碑为 0 时**一件奖励容器都不建**。本任务验收口径 = 「页面能打开 + 空态出声」⇒ 我们**只建一份模板 `Expansion Pass Reward Container` 并关着**，数值 `--`，并在 `Open()` 里逐条 `Debug.LogWarning`。⛔ 没有拿 prefab 里的示例值（`300/400` / `300,00` / `19d 15h` / `+25 %` / 示例卡）当真实数据。
3. **`HasPremium` 未知 ⇒ 默认展示 `IsNotPremium` 一支**（未购买态，`Get now!` 付费入口可达），`Is Premium` 建成关着。原版此刻显示哪一支由服务器数据决定 —— 这是**我们挑的默认**，已写进文件头与 `WarnOnce`。
4. **`Title`**：原版运行期被 `GetLabel(事件)` 覆盖；本件照 prefab 出厂原文 `Event title` 画并出声（同 Raid）。
5. **`Title` / `Time Remaining` 的框**：原版该两节点 **rect 宽 = 0**（`ContentSizeFitter(Max)` / 右侧 PreferredSize）。本壳没有 CSF 运行时 ⇒ `Title` 用 1312 宽左对齐框（左缘 = 原版 133）、`Time Remaining` 用 300 宽右对齐框（右缘 = 原版 pivot 1582.4）—— **这两处框是我们挑的**。
6. **`Rewards Scroll View` 无滚动运行时**：只建结构（视口裁切 + 内容轨）。原版 `ScrollRect`（`m_Horizontal=1` · `m_Vertical=0` · `m_MovementType=1`）在数据接上后需要另接。
7. **`ToBackground` 多关一次物体**：本壳是扁平 quad + 显式队列；本窗 3690–3729 而 `WhereToGetCardsPopup`（3479–3487）更低 ⇒ 只写 `state` 会被本窗盖住。处置照 `GachaEventWindow.ToBackground`（**我们挑的**，不是原版 base 行为）。
8. **两张卡预览只建空容器**：`Card Drawer` 的运行期卡面来自奖励数据；本地没有 ⇒ 空容器 + 出声。组与抽屉的旋转照 prefab `m_LocalRotation`；⚠️ dump 明确警告「旋转件下面的绝对坐标没按父级旋转重算」⇒ 空容器不受影响，**接数据时必须回头按 prefab 的 localPosition 重算**。
9. **两处 `UIScaleToFit` 按视觉框建**：`Premium Glow` ×0.3562 → 408.90×506.71；点数图标 `Content` ×0.05848 → 63.16×63.16（本壳没有 USF 运行时，⛔ 别照 1080×1080 的布局框）。
10. **`Level up` / `Collect Highlight Effect` 的出厂 ACTIVE**：我们照 prefab 建（模板容器整体关着 ⇒ 不画）。⚠️ 运行期 `highlightReward` 会被强制关、`levelUpContainer` 只在升级价 > 0 时开 —— 接数据时要实现这两条分支（见 §四·3）。
11. **`GetCardsButton` 的远端配置开关**：反编译只解到「某个配置布尔」这一层，⛔ 不拿我们自己的常量冒充；本件恒建出按钮。
12. **`Expansion Pass Missions Tab`**：已按协调台追加要求建为**第二页 + 第二颗页签**（原版同一个承载窗里的第二个 tab）。
    原版 `RefreshMissions` 在**没有任何可做任务时直接 `WindowTabBase.CloseTab`**（没数据就没有这个 tab）；
    本任务口径 = **两页都能打开 + 空态出声** ⇒ 保留 Missions 页、任务卡数值一律 `--`（周常卡模板关着、原版也会在末尾关它）。
    Missions 页的 `Help Icon Player (1)` 出厂 INACT（tooltip 原文是**跨域的 `Event_Description/Raid/IndividualTooltip`**，照建不「修正」）；
    其 `eventHelpButton` 也是 NULL（与 Pass 页不同）。

---

## 二、改动清单（文件:行号）

| 文件 | 行号 | 改动 |
| --- | --- | --- |
| `Unity/MyGame/Assets/CardPresentation/Shell/ExpansionPassEventWindow.cs` | **新建 · 1–1651** | 文件头判据 1–154；队列档 169–199；根/窗参 213–234；页几何 235–286；奖励容器几何 287–332；图/色 333–368；本地化键 370–399；状态/自检读口 401–476；`Create` 477–493；`Open`/`ToBackground`/`WarnOnce` 495–556；`Build` 558–582；`BuildShell` 584；`BuildShellTail` 605；`BuildTabBar`（**两颗页签**）618；`ShowPage` 663；`BuildTabButton` 672；`BuildPage` 737；`BuildPageBackground` 758；`BuildPremiumPanel` 774；`BuildPremiumCardPreview` 796；`BuildNotPremiumBranch` 806；`BuildPremiumBranch` 856；`BuildRewardsScroll` 914；`BuildHeader` 934；`BuildWhereGetCardsButton` 990；`BuildDebugAddPoints` 1016；`BuildRewardContainer` 1034；`BuildPersonalProgression` 1098；**Missions 页**：`BuildMissionsPage` 1167；`BuildMissionsHeader` 1180；`BuildDailyMissions` 1235；`BuildWeeklyMissionCard` 1269；`BuildDailySkulls` 1355；`MissionHeaderGradient` 1468；`BuildMissionTooltipIcon` 1476；`BuildMissionPointsDrawer` 1499；`BuildMissionCurrencyDrawer` 1516；`BuildMissionProgressBar` 1532；`BuildMissionCollectButton` 1561；`BuildMissionDebugGroup` 1582；`Note` 1631；`Tex` 1639 |
| `Unity/MyGame/Assets/CardPresentation/Shell/ExpansionPassEventWindow.cs.meta` | 新建 | `guid: 6324ce5cdbb645d480ac89ee54d51c05`（类名 == 文件名，避免 `!u!115` 存根） |
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs` | `:736-746` | 新增 `PrefabRefExpansionPassEvent = "Expansion Pass Tab"`（含「原版没有独立窗口」与「第二个 Missions tab 本轮未建」说明） |
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs` | `:1022-1030` | 新增 `OpenExpansionPassEvent()`（`OpenByRef` + `ExpansionPassEventWindow.Create`） |

⛔ 未碰：`ParentEventWindow.cs` / `LiveOpsEventWindow.cs` / `GachaEventWindow.cs` / `RaidEventWindow.cs` /
`PurchasePremiumWindow.cs` / `MainMenuRuntime.cs` / `MenuTutorial*.cs` / `TutorialPointerUi.cs` /
`Battle/**` / `RuleEngine/**` / `WarpforgeVFX/**` / `Core/CardView.cs` / `Scenes/*.unity` / 正本 / `d:/2`（只读）/ git。
（`WindowsManager.cs` 的 L01 / L02 两段**一字未动** —— 本轮只在 `PrefabRefRaidEvent` 之后、`OpenRaidEvent` 之后各追加一段。）

---

## 三、没查清（远端数据缺什么）

以下全部来自 **LiveOps（远端 PlayFab / CCD）**，本地离线包体里**没有**（逐条已确认，⛔ 未猜）：

1. `ExpansionPassEvent.Milestones` —— 奖励轨道逐档数据（里程碑数量、每档 `RewardInfo` 列表、`IsPremium`、等级）。
2. `ExpansionPassEvent.PlayerScore` / `ExpansionPassData.XpPerGold` / `ScoringMilestone.State` —— 进度条两端与领取状态。
3. `ExpansionPassEvent.HasPremium` / `PremiumPercentageBonus` —— 决定 `IsNotPremium` / `Is Premium` 哪一支显示、`+{0}%` 是多少。
4. `ExpansionPassEvent.PremiumOfferData` 的 `GetComponentReference(..., 0xc)` —— 买付费档要打开的那个窗口（**连它是哪个 prefab 都读不到**）。
5. `LiveOpsAssetUtility.GetLabel(事件)` —— 页签文字 + 页内 `Title`（两处同一来源）。
6. `LiveOpsAssetUtility.LoadAsset(事件, 1)` —— `Background Image`（Tiled · ppuMul 1.12）。
7. `LiveOpsAssetUtility.LoadAsset(事件, 5)` —— `Army Icon`。
8. `LiveOpsAssetUtility.LoadAsset(事件, 0)` —— 页签图标（`BaseParentEventWindow.CreateEventTab` 的实参）。
9. 事件起止时刻 —— `RaidHeaderController.UpdateText` 的 `LiveOpsUtility.GetDateForEventStage` + `PlayerDataManager.currentServerTime`（`Ends in:` / `Starts in:` 的切换与宽限期判定都靠它）。
10. 每档的**货币图标**（`PriceDisplay.icon` / `EventProgressBarController.ScoreObject.iconHolder` 的 `ObtainableItem`）。
11. `ClickEventHelpButton` 需要的 liveops 组件 **0x1e**（远端帮助窗）；本 prefab 的 `eventHelpButton` 非空，但本地没有该组件 ⇒ 点击只出声。
12. **Missions 页**：`Missions` handler 的任务列表 / 每日任务挑战 / 周常进度 / 骷髅里程碑 / 每档奖励 / 计时器 —— 全部远端；
    原版**一条任务都没有 ⇒ 直接 `CloseTab`**，本地按任务口径保留空态。`warlordImage` = `LoadAsset(事件,2)` 也缺。

---

## 四、顺手发现（交调度台分流）

1. ✅ **`Expansion Pass` 的两个 tab 都已建**：`Expansion Pass Tab`（Pass 页）与 `Expansion Pass Missions Tab`（任务页），
   两者都是 `WindowTabBase<BaseParentEventWindow>, ILiveOpsTab`、根都 1756.43×1079.64，由左栏两颗页签互斥切换。
   ⚠️ **页签文字**：原版是 `LiveOpsAssetUtility.GetLabel(事件)`（远端）；Pass 用官方 `…/TabTitle`，
   Missions 用官方通用词 `MainMenu/General/Missions`（Missions / 布道所）—— **两者都是我们挑的兜底**。
2. ⚠️ **`RaidHeaderController` 是「共用组件、不共用节点树」**：本页与 Raid 都用它，但
   `gracePeriodTooltip` 在 **Expansion Pass Tab 里是 NULL**、Raid 里非空 ⇒ ⛔ 别整体照抄 Raid 的 Header 节点表。
3. 🔴 **`ExpansionPassRewardItemContainer` 的三个出厂显隐全是「运行期被覆盖」的**（铁律 5·c）：
   `highlightReward` 恒关；`premiumGlow.enabled` = 里程碑 `IsPremium`；`levelUpContainer` 仅在
   `GetCostToLevelUp(...).amount > 0` 时开。**接数据时必须实现这三条**，⛔ 别把 prefab 出厂态当运行时态。
4. ⚠️ **`ExpansionPassProgressClaimButtonController` 只有 4 档 state（0..3）**，没有 Raid 那种第 5 档「已领取」；
   `state 1` 会**同时显示 `Claim Button` 与 `Buy premium`**（买 Premium 才能领的那一档）。
   ⇒ ⛔ 别把 Raid 的 5 档状态机整套搬过来。
5. ⚠️ **两张图在本地磁盘上有、但没进 `Resources/`**（导入器 `工具/import_original_art.py` 的 `MENU_IMAGES` 未收）：
   `Assets/CardPresentation/Art/原版/0_mainmenu/40K_general_icon_Expansion_Pass_XP.png`（点数图标）·
   `.../40k_Rewards_icon_base_card.png`（`Premium Glow` 的源；prefab 用的是 `..._card SDF` 变体，**本地没有同名 SDF PNG**）。
   ⇒ 现在这两件是「没画 + 出声」；把前者补进导入器即可消掉一条，后者要先确认 SDF 变体该怎么落盘（⛔ 不拿普通变体冒充 SDF）。
6. ⚠️ **`Expansion Pass Tab` 的 `getCardsButton` 显隐由一个远端配置布尔控制**（`ConfigManager.GetConfig` 的
   `+0x28 → +0x18 → +0x18`），**配置项名字没解出**；本件恒显示。若将来接上配置，要在 `Initialize` 的对应位置补这一跳。
7. ⚠️ **`ExpansionPassPurchasePremiumPanel.PurchasePremiumClick` 打开的窗口引用在 `premiumOfferData` 里**，
   本地**既没有报价数据、也无法确认它是不是 `Purchase Premium Window` / 某个 `BaseOfferPopup` 变体**
   ⇒ 本件点击只出声，**没有**把按钮接到 `PurchasePremiumWindow`（避免「看起来接了、其实开错窗」）。
8. ⚠️ **`Expansion Pass Reward Container` 与 `Expansion Pass Progress Bar` 两个 prefab 的层级不同**：
   前者里的 `Expansion Pass Progress Bar` 实例把 `Points` 的 text 出厂示例写成 `300/400`，
   而独立的 `Expansion Pass Progress Bar` prefab 里同一格也是 `300/400` —— 两处都是示例值，⛔ 别当数据。
9. ⚠️ **`Purchase premium Panel` 的两张卡预览在旋转件下面**（组 −17.50°/+4.83°、抽屉 +9.94°/−1.98°），
   `menu_dump` 明确标注「旋转件下面 70 个节点的绝对坐标没按父级旋转重算」⇒ 本件只建空容器；
   **将来接卡面时，位置要按 prefab 的 `m_LocalPosition` 而不是 dump 的绝对矩形重算**。
10. ⚠️ **简报里写的「现有零件 `Shell/PurchasePremiumPanel.cs`」在本仓不存在** —— 实际文件是
   `Shell/PurchasePremiumWindow.cs`（原版 `PurchasePremiumWindow`，买「高级战役」的整窗，1087 行，界面线已建）。
   两者**不是同一个东西**（Expansion Pass 的 `ExpansionPassPurchasePremiumPanel` 是**页内一块面板**，
   已被本件建进 `Purchase premium Panel`；`PurchasePremiumWindow` 是**另一扇弹窗**）。
   本轮**没有改** `PurchasePremiumWindow.cs`（不在文件所有权里），也没有把它接到 `Get now!`（见 §四·7）。
11. ⚠️ **Missions 页的 `Help Icon Player (1)` tooltip 用的是 `Event_Description/Raid/IndividualTooltip`**
   （prefab `EverguildTooltipTrigger.text` / `localize=1` 实读）—— **跨域原文，⛔ 别「修正」成 ExpansionPass 前缀**；
   而且它**出厂 INACT**（`RaidHeaderController.eventHelpButton` 在本 prefab 里也是 NULL）。
12. ⚠️ **`Mission Header` / `Daily Skulls header` 的 `Gradient2`**：`_gradientType=0`(Horizontal) · `_blendMode=2`；
   色键 (0.247,0.188,0.380)→(0.475,0.306,0.153)，**alpha 键 0.2706 / 0.9059 → 1.0 / 0.098**
   （ctime 0/65535、atime 17733/59367 实读）。本件用 `CardArt.GradientKeys` 按同一组常量重建
   （与 `Shell/MissionsTab.HeaderGradient()` 同源；那份是 private，没法直接调）。
13. ⚠️ **`Mission Debug Buttons` / `debug_buttons` 两族都在**：Prefab 出厂 ACTIVE，但原版 `MissionDebugButtons.Setup`
   会 `SetActive(false)`（判据 → `Shell/MissionsTab.cs` 文件头 ① 与 `all_methods.txt:9716`）⇒ 本件逐一建出来、
   **Missions 页的竖排 `debug_buttons` 关着**；周常卡模板整体关着（原版 `RefreshMissions` 末尾也关模板）。
14. ⚠️ **`Daily Skulls Mission Container/background/background Rotated`**：`40K_missions_display_Daily vertical`
   绕 z 转 90°；dump 的 unrotated rect 与 AABB 中心不重合（pivot 未重算）⇒ 本件按**中心枢轴**转（**我们挑的枢轴**，
   视觉上盖住整张卡；接数据/量版面时要回 prefab 读 raw pivot）。
15. ⚠️ **Missions 页只有 `Event Image`、没有满页黑底**：页根直接子件是 `Event Image` → `Header` → `Daily Missions` →
   `Daily Skulls Mission Container`；底色由承载窗的 `Content Area/Background` 渐变透出（本件同态，未额外铺底）。

---

## 五、增补：第二页 `Expansion Pass Missions Tab`（协调台追加要求 · 同日）

**建了什么**：`ExpansionPassEventWindow` 现在有**两页 + 两颗页签**（`ShowPage(0/1)` 互斥 `SetActive`）。
第二页按 `Expansion Pass Missions Tab` dump 逐节点建：`Event Image`（`warlordImage` · liveops `LoadAsset(事件,2)`，无图）·
`Header`（纯黑条 + `Event title` + INACT `Help Icon Player (1)` + `Army Icon` + `Ends in:`/时钟/`--`）·
`Daily Missions`（`Mission Header` 的 `Gradient2` 渐变条 + `Missions/Title/WeeklyChallenges` 词条 + 周常卡模板 `Daily Mission Container Ref` **关着**）·
`Daily Skulls Mission Container`（旋转卡底 + 渐变条 + `Missions/Title/DailySkulls` + ⓘ tooltip `Missions/Info/DailySkulls` + 骷髅图/`--` 计数 +
5 个 `Mission Milestones Step` + 奖励抽屉/`Collect`/`Timer --` + INACT `debug_buttons`）。
周常卡模板内含：底图、`title`[INACT]/`description`/`timer`、`Rewards`（`ExpansionPassPointDrawer` 62×62 空架）、
`Mission Milestones Progress Bar`（底 + 0 填充 + INACT 手柄）、`--/--`、`Generic UI Button`（`MainMenu/General/Collect`）、
INACT `Generic Round Button`、`Mission Debug Buttons`（Reset/Re-Roll/+1/+5/Complete/00:00）。

**原版判据**：`ExpansionPassMissionsTab__{OnOpen,ToFocus,SetEventTab,RefreshMissions,Reset}.c` + `ExpansionPassMissionsTab.cs` +
`_tmp_view/ep_Expansion_Pass_Missions_Tab.txt`（240 行）+ `Gradient2` MB 实读（色键/alpha 键）。

**有意偏离（照实记录）**：
1. 原版 `RefreshMissions` 在**没有可做任务时直接 `WindowTabBase.CloseTab()`**；本件按任务口径保留页面、卡内数值 `--`（周常卡模板关着，原版末尾也会关它）。
2. 页签文字：Pass = `Event_Description/ExpansionPass/TabTitle`；Missions = `MainMenu/General/Missions`（**我们挑的兜底**；原版是远端 `GetLabel(事件)`）。
3. `Daily Skulls/background Rotated` 的旋转枢轴：dump 的 AABB 与 rect 中心不重合（pivot 未重算）⇒ 本件按**中心枢轴**（我们挑的）。
4. `Mission Header/info` 出厂 INACT（prefab 里是 `localize=0` 的示例英文）⇒ 只建节点、关着。
5. 任务领取 / 重掷 / 调试钮都不接服务器：领取钮点击只 `Debug.LogWarning`；调试钮不接点击（原版 `MissionDebugButtons.Setup()` 会整组关）。

**没查清**：`Missions` handler 的任务列表/挑战/进度/骷髅里程碑/计时/奖励、`LoadAsset(事件,2)`、任务表刷新周期 —— 全在远端。
**验证**：`TMPDIR=/tmp/wf_l03b bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
**顺手发现**：见 §四·11–15（跨域 tooltip 原文 · Gradient2 色键 · 两族 debug 按钮 · 旋转卡底枢轴 · Missions 页无满页黑底）。
