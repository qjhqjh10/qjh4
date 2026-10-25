# 报告 · G B→A 第 1 轮（教程 → 泛型 → 成就 → 社交）

> 代理 G · 2026-10-25 · 承接执行批之后的 B 档转 A
> 编译：`TMPDIR=/tmp/wf_g bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（本轮只改账/判据，代码未动）
> 口径：**只升「能点到一个具体函数/属性、且语义对得上」的行**；核不动的留 B 并写清差什么。⛔ 没有为数字好看改 A。

## 一、数字

| 域 | 行数 | A | B | C | D | E | 本批 B→A |
|---|---:|---:|---:|---:|---:|---:|---:|
| 教程/引导 | 155 | **121** | 25 | 5 | 4 | 0 | +118（121−3） |
| 泛型与启动链 | 866 | **136** | 50 | 194 | 484 | 2 | +92（136−44） |
| 个人资料/成就/排行 | 341 | **197** | 45 | 40 | 59 | 0 | +177 |
| 社交/聊天 | 444 | **168** | 16 | 21 | 239 | 0 | +164（168−4） |
| **四域合计** | **1806** | **622** | **136** | **260** | **786** | **2** | **+551** |

> B 从约 695 → **136**（教程 25 / 泛型 50 / 成就 45 / 社交 16）。剩下的每一类都写了「差什么」，不是数字没上去。

## 二、重点抽查（你点的四条）

### ① 教程的 stage 守卫与顺序 ✅
- 顺序：`MenuTutorialStages.All[]` = 原版 `stages[]` 序列化序（1 SelectArmy → 2 PlayRanked → 3 GetMission → 4 GetCampaignNode → 5 LevelUpCard → 6 ClaimForgeFirstLevel → 7 TutorialFinished）；**1–4 与 7 实读、5–6 推断位**（SO 本地缺）——文件头已标。
- 守卫：`CheckCompletion` 逐条照原版「当前窗是 X 时**不算完成**」——`GetMission` 在 `RewardWindow` 里返回 false；`GetCampaignNode` 在 `CampaignRewardWindow`/`RewardWindow` 里 false；`PlayRanked` 必须回主菜单才置完成；`ClaimForge` 在 `RewardWindow` 里 false。**逐条对上了**。
- 推进：`Advance` = 原版 `Complete()` 的「从数组头重找第一个未完成/未满足」；`FirstPlayableStage` 跳过「条件已满足但不标完成」的 stage（原版同）。✅
- **抽查里新发现 3 条 C**（见 §四）。

### ② 泛型容器的边界（空表/负数/同 id 多实例）✅（抽查两处）
- `SetAchievementValueAtLeast`：`value <= cur` **直接 return**（不累加、不写盘）⇒ 反复调用不会重复计数（幂等）。
  `AddAchievementValue`：`delta <= 0` 不写；>0 才累加（这是累加语义，不是幂等语义 —— **调用方不得重复触发同一事件**，已在账里注明）。
- `BiDirectionalDictionary`：原版 `Add` 重复 key 抛 `ArgumentException`、`get_Item` 缺 key 抛 `KeyNotFoundException`；我们的 `WindowsManager._openByRef` 用 `Dictionary<string,GameWindow>`（同样抛/同样缺省），基础 4 个方法判 A；「按 value 反查 key」「按 key+value 删」仍 **C**（原版有、我们没有）。
- 空表/负数：`LeaderboardData.ApplyScoringMode` 未知模式返回旧值（原版 `return score`）；`RankedData` 积分夹 `>=0`（**我们选的**，原版在服务端，已注释）；`AllianceTrophyModel` 空定义返回 **−1**（调用方退回旧计数，不静默变 0 格）。

### ③ 成就幂等 ✅（见上）
- `GainAchievement`：`delta<=0` / 未知 id / 模式闸不过 ⇒ 不写；`AchievementTierOf` 用阈值推档（不落「已发奖」标记）。
- ⚠️ **一个如实记录的口径差**：原版达标是 `OnReachMilestone` **发奖**；我们是**按阈值派生积分**（`AchievementPointsOf`）⇒ 同一玩家不会被重复发（派生值天然幂等），但「发奖动作」本身不存在。账里 `Achievement.OnReachMilestone` 判 **A**（派生等价），并注明这一差异。

### ④ 社交 UI 状态机 ✅（抽查 ChatPanel / FriendsTab）
- `ChatPanel`：`Open → Build → tabButtons.Click(0)（Global）→ RefreshHighlights`；两频道按 `ChatRoom` 枚举建、消息行读 `SocialData.ChatMessages`（本地恒空）——与原版「按已订阅频道建页签、无消息即空」一致（差别 = 建页签依据，已如实标）。
- `FriendsTab`：`Setup/OnOpen/BuildRows` 在；**`SortByConnected` / `SortByName` 确实没有** ⇒ 判 **C P2**；`ClearList/Reset/FillInFriendsContainer/CreateEntry/OnClose` 无同名函数（逻辑并进 BuildRows）⇒ 保持 B 并写明。

## 三、四域的升级口径（各自升了哪些类）

- **教程 121 A**：`TutorialManager` 21/26 · `MainTutorialManager` 9/12 · `TutorialTipScript` 11/12 · `TutorialPointer` 8/11 · `FlowTutorialStage` 9 · 五个实体 stage 全 A · `TutorialModePopup` 9 · `TutorialMessageWindowPopUp` 4（**`ShowCampaignPointsDrawer` 此前误记 C ⇒ 已实现，订正 A**）· FTUE 窗/格 7 · `BattleTutorialManager` 4 · `TutorialV2SaveData` 4。
- **泛型 140 A**：`DraftEventBase` 19/22 · `CollectionDisplay` 7/11 · `CollectionItem` 6/15 · 筛选四类 22 · `MissionEvent` 5/12 · `ShopStore` 4 · `PlayEvent` 2（另 3 条复核后改 C，见 §四）· `LiveOpsEvent/Window/Handler` 8 · `ComponentReference` 5 · `BiDirectionalDictionary` 5 · `Points` 2 · `Draggable/Controller` 4 · `AlternateArt/ArmyItem` 2。
- **成就 198 A**：头像页/格 25 · 成就引擎 16 · 档案窗/头部/页 30 · 排行榜控制器/行 25 · 排位窗与七扇衍生窗 70 · 其它构建件。
- **社交 168 A**：聊天窗/页/行/预览/快捷钮 40 · 好友页/行 5 · 公会族（视图/成员/搜索/加入/建会/行族/奖杯/积分/榜行）115 · 收件箱 8。

## 四、本批新增的 C / D（都是核出来的真缺口，不是降级凑数）

| 类.方法 | 判定 | 理由 |
|---|---|---|
| `TutorialTipScript.FadeElements` | **C P2** | 原版 0.2s 淡入淡出；我们直接显隐（`TutorialOverlay` 无补间） |
| `TutorialPointer.ClickAndHold` | **C P2** | 原版三种指针动画，我们只做「点击」 |
| `TutorialPointer.ClickAndDrag` | **C P2** | 同上 |
| `FriendsTab.SortByConnected` | **C P2** | 缺在线优先排序（全表示在线状态色已有） |
| `FriendsTab.SortByName` | **C P2** | 缺按名字排序 |
| `TutorialManager.OnDestroy` | **D** | 原版只在编辑器按调试开关保留 ScriptableObject ⇒ 纯编辑器行为 |
| `ChatPanel.ValidateInput` | **D** | 用户 2026-09-26 拍板「网络聊天暂时不做」⇒ 输入框不可输入、发送只出声 |
| `PlayEvent.StartMatch` | **C P1** | 缺两条错误弹窗文案（`MenuDeck/Error/InvalidDeck` · `MenuDeck/Error/HasNotAssignedDefensiveCard`） |
| `PlayEvent.GetEventDeck` | **C P2** | 缺「上次用的卡组」记忆（我们让玩家选） |
| `PlayEvent.GetEventDeck(CardArmy)` | **C P2** | 缺按阵营记上次卡组 |
| `PlayerAvatarDataManager.RefreshExpiredBorders` | **C P2** | 缺限时边框到期自动卸下 |

## 五、留给别的线的两处接线（你要的接口签名）

### ① 排位结算 → 战斗线（`BattleDriver`）
```csharp
// 对局结束时（拿到 BattleLogData.Outcome 的那一处）：
RankedMatchScore score = RankedData.ApplyMatchResult(outcome, suspicious);
//   outcome   : BattleLogData.Outcome（Victory/Defeat/Draw）；内部按 1/0/2 记 matchWon
//   suspicious: 原版 PlayerDataManager.matchData.suspiciousGame（没有就传 false）
// 返回：MatchWon(1/0/2) · LossStreak · PointsBefore/After · DeltaKnown · Suspicious
// 副作用：写本地存档（persistentDataPath/WarpforgeRanked/ranked.json）——不需要调用方再存
// 之后若要刷 UI：RankedDivisionInfo / RankedTab 读 RankedData.DisplayDivisionNumber 即是新值
```
⚠️ 只在**排位局**调（`RankedData.IsRanked == true`；原版 `RecordMatchFinished` 也带 `isRanked` 标记）。

### ② 万能牌教程 → 收藏窗（`CollectionWindow`）
```csharp
// 1) 实现 6 个成员：
public sealed class WildcardBindings : IWildcardTutorialBindings {
    public Transform FilterToggleNode { get; }   // 卡池页 Header 的 "Filter Toggle"
    public Transform OwnedToggleNode  { get; }   // 筛选抽屉里的 "Owned Toggle"
    public Transform CardsTabNode     { get; }   // 左栏 "CardsTab"
    public bool FilterOn { get; }                // 当前筛选抽屉开没开
    public bool OwnedOn  { get; }                // Owned 筛有没有开
    public void SetFilterOn(bool on);            // 原版 filterToggle.isOn = false
}
// 2) 卡池页建好时：
WildcardTutorial.TryStart(transform /* 全屏根 */, bindings);   // 已做过返回 null（不重启）
// 3) 玩家点开卡详情时：
WildcardTutorial.NotifyCardDisplayOpened();                   // 只收指针，不改状态
// 4) 调试/回归：WildcardTutorial.ResetProgressForTest()
```
> 三个偏移与节点名都是实读（`Intro.unity:7694-7696` / `.ctor` 字典）；文案 `Tips/WildcardTutorial1|2`。

## 六、仍留 B 的（136 条）——按「差什么」归类

| 类别 | 条数 | 差什么 |
|---|---:|---|
| `WildcardTutorialManager` 8 | 8 | 代码已落地（状态机+偏移实读），**接线未做**（CollectionWindow 白名单外） |
| 两个 SO 缺失的 stage（LevelUpCard 7 / ClaimForge 3） | 10 | 指针/文案按阶段表；`DoingUpgrade`/`HasAlreadyUpgradedCard` 无对象可查（升级链停用） |
| 其余教程 7 | 7 | `AddStageToUpdate`/`RemoveStageToUpdate`/`ToggleTutorial`（无更新表/无开关）、`SetupCursor`（CursorByArmySO 本地没解）、MainTutorial 3 条无独立入口 |
| `CollectionItem` 拖拽/事件 9 | 9 | 无 `EventRelay`/`DragAndDropItem` 静态事件；拖拽只在卡组编辑那条链上 |
| `CollectionDisplay` 选中/继电器 4 | 4 | `ResetRelayTarget`/`Select`×2/`GetItemObject` 无同名口（选中并进卡格点击） |
| `ItemDrawer` 5 | 5 | `SetItemName`/`GetItemName`/`Draw(T)`/`Refresh`/`Awake` 未逐条核 |
| `MissionEvent` 7 | 7 | 无「按挑战领取/激活」的独立 API（领取在 UI 行内联） |
| `LoadableReference` 6 | 6 | 没有统一引用类型（语义散在 CardArt/CardDatabase） |
| 筛选族 5 + 其它容器/控制器 15 | 20 | 填充上限、回调 lambda、BaseEvent 两族控制器、SingletonBehaviour 基类 |
| 成就域弱件 45 | 45 | 容器/装饰品小件（`ProfileContainer`/`AvatarBorderDrawer`/`RankedDeltaData`…）只建了件、未逐句核 |
| 社交 16 | 16 | 服务器动作的回调 lambda、奖杯 controller 订阅、FriendsTab 建行内部 |

## 七、没查清
1. 教程 5–6 两个 stage 的原版 SO 本地缺 ⇒ 顺序是实读+推断（文件头已标），**逐字段无法核**。
2. `SetupCursor` 依赖 `CursorByArmySO`（本地只有 MonoScript）⇒ 阵营→光标表无判据。
3. 成就域 45 条弱件保持 B：本轮只核了引擎与主要窗口，**容器/装饰品小件没逐句**。
4. 社交域 16 条：回调 lambda 与 controller 订阅无对应实现对象。
5. 本轮只改账与判据、**未改代码**，故没有新增断言；编译 0/0 是复跑确认。

## 八、判据文件同步
- `判据/泛型与启动链.md`：§一 状态表按新账重算；§四 C 表重生成（§五·b / §六·b 两段手写内容已原样保留）。
- `判据/社交与成就.md` · `判据/教程_音频_本地化.md`：按新账重生成，并补回「§六·b 签名级清扫」指针。
