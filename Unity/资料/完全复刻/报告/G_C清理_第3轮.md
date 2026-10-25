# 报告 · G C 清理 第 3 轮

> 代理 G · 2026-10-25 · 承接「B 收尾」批
> 编译：`TMPDIR=/tmp/wf_g bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**

## 一、数字（三块 C）

| 域 | C 前 | C 后 | 说明 |
|---|---:|---:|---|
| 教程/引导 | 3 | **0** | ToggleTutorial 落地；TutorialEvent 2 条判 D（我们直接开教程局，无事件对象） |
| 社交/聊天 | 21 | **16** | 奖杯档位框 3 + 计分条 2 已做；剩 **16 条全是收件箱**（`BaseInboxEvent`/`Inbox`/`LocalInboxMessage`/`MessageDisplay`）= **LiveOpsHandler/LiveOpsEvent 族，活动线暂停** |
| 个人资料/成就/排行 | 40 | **1** | 只剩 `RefreshExpiredBorders`（你点名**不做**） |
| 泛型与启动链 | 195 | 195 | ⛔ 按你的指示未动（LiveOps/PlayEvent 族） |
| **合计** | **259** | **212** | |

四域总盘：**A 745 · B 11 · C 212 · D 836 · E 2**（1806 行）。

## 二、按你指定的顺序做完的四件

### ① `CollectionItem.OnPointerEnter` —— 拖动中不悬停（**优先件**）
- 判据：原版 `OnPointerEnter` 第一句 `if (eventData.pointerDrag != null) return;`（有拖拽就不亮悬停）。
- 落地：`Shell/CollectionWindow.cs` 的 `AttachCardHover` 前加 `HoverSuppressed`：
  `_hoverSuppressedForTest || Input.GetMouseButton(0)`（**等价物=主键按住**：任何拖拽期间必然按住；代价=按下当帧移入不亮，松手后下次 enter 恢复），并给 `SetHoverSuppressedForTest(bool)` 供自检强开。
- ⚠️ 为什么不用真拖拽标志：本工程拖拽状态在 `Core/DraggableController` 的**实例**上（`Dragging`），没有全局口；收藏卡格本身不参与拖拽。**没有为此改 Core/**。

### ② `TutorialManager.ToggleTutorial(bool)`
- 判据：原版只做 `tutorialEnabled = option; enabled = option;`（不写存档）。
- 落地：`Shell/MenuTutorialFlow.cs` 新增 `ToggleTutorial(bool)`：关 ⇒ `_active=false` + 收指针/聚光灯；开 ⇒ `FirstPlayableStage()` → `Enter(first)`，无可玩 stage 则 `FinishAll()`。**不落 PlayerPrefs**（照原版）；「永久跳过」仍走 `SkipAll()`（= `ForceCompleteAll`）。

### ③ 奖杯档位框（`GetTierFrame` / `UnloadTierFrame` / `SetTierFrame`）
- 判据（伪码可读）：`idx = Math.Max(0, tier - 1)` → `tierFrames[idx].Load()`；**原版越界会 panic**；`UnloadTierFrame` 只 `LiveOpsAssetUtility.Unload`（释放 Addressables 句柄）。
- 资产：`Alliance_Trophies_TIER1..4`（4 张）**本地有**，本批复制进 `Resources/Art/ui_menu/`（`CardArt.MenuUi` 的取图根）。
- 落地：
  - 新建 `Shell/AllianceTrophyArt.cs`：`TierIndexOf`（夹 1..4，越界出声）/ `TierFrameName` / `GetTierFrame`（取不到出声）/ `UnloadTierFrame`（**本工程直接引用、无句柄 ⇒ 空实现并出声一次**）/ `SelfCheck`（含灭自证：TIER1 与 TIER4 必须不同名）。
  - `AllianceTrophyModel.VisibleAt(i)`（第 i 格 → 可见定义）。
  - `AllianceMemberTab.BuildTrophyCell`：`TrophyDisplay/BadgeDrawer/Frame` 按**该格奖杯的当前档**画框（`CellQ(LC_Slot)` 层，与进度槽同档但几何不重叠）；**没有定义表（本地默认）⇒ 节点照建、不画**（⛔ 不猜一档）。

### ④ 计分条两条 —— **复核后是「早已实现」**
- `AllianceScoreBarLine.Initialize(int)` = 逐行 `Score.SetText(threshold)` + 重排对齐 → 已在 `Shell/AllianceEventScoreInfo.cs:SetMilestones`（账里此前漏记）。
- `AllianceScoreBarLine.ShowChestOpen()` = 已达阈值换开箱图 → 已在 `AllianceEventScoreInfo.SwapChestOpen`（开箱图本仓缺 ⇒ 保持闭箱并出声）。
- ⇒ 两条 **C→A**（自我纠偏）。

## 三、为清 40 条成就 C 补的 `RankedData` 能力（`Shell/RankedData.cs`）
- `RankedArmyFlags`（None0/Boosted1/Shielded2/Featured4，**枚举值照抄**）+ `RankedBoostEvent`（Affects/Includes/WinsFor/BoostsLeftFor）。
- `GetBoostedFactionCount` / `IsFactionFeatured` / `GetBoostsFor`（可注入）。
- `ArmyState(army, extraBoosts)`（`!IsLegendaryElo⇒None`；Boosted/Featured/Shielded 三 flag 次序照伪码；⚠️ 两个 predicate 体 ILSpy 丢类型 ⇒ 条件式按可读片段复刻，已在 §六·b 登记）。
- `DivisionOfV2(points)`：**同一张 `rankedRanks` 表**，但 v2 的传奇线是**写死的 2500**（方法体可读），不读 `LegendaryElo`。
- `HasReachedLegendaryDivision(highestElo)`、`NoteOpponent(name)`（= `LastOpponentInfo` 的本地等价）。

## 四、C → D（判据不可及 / 架构不需要，**都写替代物**）

| 项 | 为什么 | 替代 |
|---|---|---|
| `RankedV3Event.GetMatchmakingFilter` / `RankedV2Event.GetMatchmakingFilter` | 过滤串是交给 **PlayFab 匹配服务**的；我们 P2P 直连不过滤（同类 `GetSqlFilter` 早判 D） | 无（进 P2P 匹配时再议） |
| `RankedEventSeasonEndedWindow(Old).GetEndSeasonPlayerPercentage` | 服务端百分位（要榜数据） | **不显示百分位**（用户口径「排名可空着」；本地不发明数字） |
| `Achievement.GetStoreId` | 原版走 store 兑换；我们的成就数据只有 `q` 数量、无 store id | 直接发点数（`AchievementPointsOf`） |
| `Achievement.IsHidden` | 依赖远程 `FeatureConfig` | 全部显示 |
| `AvatarDisplay.DoShine` | 原版复制 material 做 `_Color` 脉冲；我们的 `ImageQuad` 共享材质 + tint，整格一起变色（`_EmissionColor` 同类教训） | 静态立绘 |
| `LeaderboardManager.GetLeaderboard` ×2 | PlayFab 拉榜（后端） | 本地缓存 `LeaderboardData.GetCached/LocalScore` |
| `TutorialEvent.StartMatch/OnMatchFinished` | 原版是 `PlayEvent` 子类；我们直接开教程局 | `TutorialModePopup.PlayTutorial → BattleDriver.BeginTutorial` |

## 五、两条「没查清」按你的口径落账
- **ClickAndHold 的已知边界**：已写进该行备注 ——「原版 Hold 想表达什么数据读不出，唯一差别 `clickAnim=null`；可见行为同 ClickInPosition ⇒ 不再挖」。
- **成就弱件 A 的如实标注**：凡是只做到「页面/引擎已建」的，备注统一加「**类级核，未逐句**」；
  其中 **9 行只到类级的容器件**（`AchievementContainer` / `CampaignProfileContainer`×2 / `ProfileContainer` / `ProfileEventSection` / `PlayerProfileTrophiesDisplay` / `LegendaryRankAllTrophiesDisplay` / `RankedArmyBonusContainer`×2）按 A 的定义**退回 B**，差异栏写「未逐句核（页面 builder 内建该节点）」。
  ⇒ 成就域现在 **B 11**（9 条容器 + `RankedDivisions` 2）。

## 六、剩余 C（212）——全是你点名不要动的
- 社交 16：收件箱（`BaseInboxEvent` 9 / `Inbox` 4 / `LocalInboxMessage` 1 / `MessageDisplay` 2）—— **LiveOpsHandler/LiveOpsEvent 族，活动线暂停**。
- 成就 1：`RefreshExpiredBorders`（你点名不做）。
- 泛型 195：LiveOps/PlayEvent/MissionEvent/FastAction 族 + 其它（本轮未动）。

## 七、纪律
- 本批动了 `Battle/TutorialOverlay.cs` 是上一轮**你明确授权**的「提示淡出」；**以后动 `Battle/**` 会先报**。（本轮没有再动它。）
- 本批改的 `.cs`：`Shell/CollectionWindow.cs` · `Shell/MenuTutorialFlow.cs` · `Shell/AllianceTrophyArt.cs`（新）· `Shell/AllianceTrophyModel.cs` · `Shell/AllianceMemberTab.cs` · `Shell/RankedData.cs`；另导入 4 张 PNG 到 `Resources/Art/ui_menu/`。
- 判据三份已按新账重生成（§五·b / §六·b 手写段原样保留）。

## 八、没查清
1. `ArmyState` 的两个 predicate 体（ILSpy 丢类型）⇒ Boosted/Shielded 的**精确条件**仍属签名级（已登记 §六·b）。
2. 奖杯 `Badge`（阵营徽标图）仍不画：`AllianceTrophyData.reward` 的 id → 22 张 `Alliance_Trophies_<阵营>` 的对应表没有（本轮只做了 Frame）。
3. 9 条容器件退回 B：需要逐条读容器方法体才能升 A（本轮未做）。
