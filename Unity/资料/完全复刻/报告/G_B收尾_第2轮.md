# 报告 · G B 收尾 第 2 轮（剩余 136 条 B → 只剩 2 条）

> 代理 G · 2026-10-25 · 承接「B→A 第 1 轮」的收尾批
> 编译：`TMPDIR=/tmp/wf_g bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
> ⛔ 你没让做的两条（`PlayEvent.StartMatch` 错误弹窗 / `PlayEvent.GetEventDeck`）与 `RefreshExpiredBorders` **没做**，账里留着。

## 一、数字

| 域 | 行数 | A | B | C | D | E | 本批 B |
|---|---:|---:|---:|---:|---:|---:|---:|
| 教程/引导 | 155 | **141** | **0** | 3 | 11 | 0 | 25 → 0 |
| 社交/聊天 | 444 | **175** | **0** | 21 | 248 | 0 | 16 → 0 |
| 个人资料/成就/排行 | 341 | **239** | **2** | 40 | 60 | 0 | 45 → 2 |
| 泛型与启动链 | 866 | **163** | **0** | 195 | 506 | 2 | 50 → 0 |
| **合计** | **1806** | **718** | **2** | **259** | **825** | **2** | **136 → 2** |

**唯一剩下的 B = `RankedDivisions.GetDivisionData/GetRankData`** —— 按你上一轮明确「运行时模型留在账里、不进本轮」。其余每一条都落到了 A/C/D 并写了依据。

## 二、本批改的代码（3 处，全部有第一手判据）

### ① `TutorialPointerUi`：补 `ClickAndHold` / `ClickAndDrag`（`Shell/TutorialPointerUi.cs`）
- 判据：`decomp_full/TutorialPointer._ClickAndHold_d__24__MoveNext.c` · `_ClickAndDrag_d__25__MoveNext.c` + 常量按 RVA 从 `GameAssembly.dll` 读出。
- **Hold** = 起手 → 等 `waitBeforeClick`(0.5) → 点击态 → 等 `waitAfterClick`(0.5) → 循环；与原版**可见行为相同**（差别只在 `clickAnim`，本 prefab 为 null ⇒ 不实例化）。
- **Drag** = 起手(0.5) → **点击态停在起点** → `waitAfterClick × 0.5`（`DAT_1834b2bb4 = 0.5` ⇒ **0.25s**）→ `DOMove(endPos, moveDuration)`（Menu/Wildcards prefab `moveDuration = 0` ⇒ **瞬移**）→ 等 `moveDuration + 0.1`（`DAT_1834b2dc4 = 0.1`）→ 回起点、循环。
- 新增 `PointerAction` 枚举（0/1/2 照原版 `TutorialPointerActions`）、`Show(..., action, endPx)`、`TickDrag()`、`MoveTo()`；自检读口 `Action/StartPointPx/EndPointPx`。

### ② `TutorialOverlay`：补淡**出**（`Battle/TutorialOverlay.cs`）
- 判据：`TutorialTipScript.FadeElements(targetAlpha)` 是 `DOFade(fadeDuration = 0.2)`；我们**淡入早就有**（`Tick` 的 `MoveTowards` + `FadeDuration = 0.2f`），缺的是**关提示时 `SetActive(false)` 立即隐藏**。
- 改法：`SetTip(on:false)` 只清 `_wantVisible`；`Tick` 把 alpha 推到 0 后由 `ApplyFade` 末尾 `SetActive(false)`（原版语义）。

### ③ `FriendsTab`：补两条排序 + 列表生命周期（`Shell/FriendsTab.cs`）
- 判据（`FriendsTab.cs:223/234/320/328`）：
  - `SortByName = string.Compare(b.name, a.name, OrdinalIgnoreCase)`（**b 在前 = Z→A**）；
  - `SortByConnected`：`Connected` 排前，其余 0；原版读 `ChatGlobalManager.GetPlayerStatus`，本地等价物 = `Friend.Online`；
  - 原版 `FillInFriendsContainer` **先 SortByName 再 SortByConnected**（`List.Sort` 不稳定，我们照做）。
- 落地：`SortedFriendsForTest()`（先名字后在线）、`SortByName/SortByConnected`（public static，自检可直调）、`ClearList/FillInFriendsContainer/Reset/OnClose/CreateEntryAt`；`BuildRows` 改走排序副本 + `CreateEntryAt`。

## 三、B 的终局判定（每条都有依据）

### 落 A（实现与判据对上）
- **教程 25**：Wildcard 8（**C 已按签名接线** `CollectionWindow.cs:1197`）· LevelUpCard 4（通用阶段路径 + 等价完成判据）· ClaimForge 3 · MainTutorial 2 · 两个新动画/淡出。
- **社交 16**：FriendsTab 7（本批落地）· 其余见「落 D」。
- **成就 45**：账号页 4（`SettingsWindow.cs:932` 字段表）· 边框/装饰品/容器 20（`ProfileData`/`ProfileTab`/`ItemDrawer`）· 排位菜单/段位显示/奖杯弹窗 15 · 结算与赛季 5（`RankedData` + 已接线）· `RankedMatchResult` 2（`BattleContext.Winner`：0/1,2/3）· `LeaderboardConfig`（`LeaderboardData.LeaderboardDef`）· `AchievementsManager.Initialize`（`EnsureSave` 惰性载入）。
- **泛型 50**：ItemDrawer 5（并入 `DrawView`/`ItemSpec.Label`）· MissionEvent 7（`DailyData` 三态/CollectDaily/进度写点）· 筛选 lambda 5 · Draft 3（选包池/候选择将）· LoginManager 语言 2（`Loc.AvailableLanguages` 12 项 + 回退）· BaseEvent 两族控制器 4 · CollectionDisplay.Select 1。

### 落 D（不适用 / 架构不需要，**都写了替代物**）
| 项 | 替代物 |
|---|---|
| `LoadableReference` 6（GetReference/GetTargetId/op_Implicit×4） | 资产已导入、直接引用 ⇒ `CardArt` / `CardDatabase` 按名/按 id 取 |
| `CollectionItem` 拖拽/事件 9（OnEnable/OnDisable/ToggleDrag/On*Drag/op_Implicit） | 收藏卡格不提供拖拽；卡组/卡池拖拽走 `DeckRuntime` + `Core/DraggableController`（自有链，无 cell 静态事件） |
| `CollectionDisplay.ResetRelayTarget` / `GetItemObject` | 无 EventRelay 层；页面自持 `CardsCells` 按序对应 |
| `SingletonBehaviour` 2 | 各 Manager 自持 `static Instance` + 各自 OnDestroy 清理 |
| `BaseProgressBarScaleAndPositionController` 2 | 进度条按状态直接建/摆节点（`CampaignTab` / `AllianceEventScorePanel`） |
| `LoginManager.CheckPlayerDeck` | 无登录；`DeckRules.Validate` 在进对局前跑 |
| `TutorialPointer.SetupCursor` | 判据不可及（`CursorByArmySO` 只有 MonoScript）⇒ 固定默认光标 |
| `MainTutorialManager.OnDisable` / `TutorialManager.AddStageToUpdate/RemoveStageToUpdate` | 无对应订阅/更新表；`Flow.Tick` 统一驱动 |
| LevelUpCard `DoingUpgrade/HasAlreadyUpgradedCard/HasNoCardToUpgrade` | 升级系统在本工程停用（判据不可及）；完成判据换成「打开过卡详情」 |
| `AlliancesTrophyController` 3 + `AllianceTrophy` 2 | 服务器 controller；替代 = `AllianceTrophyModel`（档位/进度/可见性） |
| 编译生成的服务器动作 lambda 5 | 对应动作本身已判 D（PlayFab/Photon） |
| `AvatarTab.IsAvatarLocked` | 用户 2026-09-17 拍板「头像全解锁」⇒ 恒不锁 |

### 落 C（本批复核出的真缺口）
- `TutorialManager.ToggleTutorial`（**C P2**：缺运行期开关教程；我们只有「跳过整链」）
- `AlliancesTrophyController.GetTierFrame/UnloadTierFrame`（**C P2**：缺奖杯档位框绘制/卸载，资产在 Addressables）
- `CollectionItem.OnPointerEnter`（**C P2**：原版 `eventData.pointerDrag == null` 才悬停，我们无此闸 ⇒ 拖动中也会亮描边）

## 四、按你的指示**未做**（账里留着）
- `PlayEvent.StartMatch` 的两条错误弹窗（**C P1**）· `PlayEvent.GetEventDeck` / `GetEventDeck(CardArmy)`（**C P2**）—— 活动线，用户已暂停。
- `PlayerAvatarDataManager.RefreshExpiredBorders`（**C P2**）—— 同上。
- `RankedDivisions` 运行时模型（**B**，你上一轮明确不进）。

## 五、编译与验证
- `TMPDIR=/tmp/wf_g bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（本批改的 3 个 `.cs` 都在内）。
- 自检：本批**没有**新增断言（仍按你的口径「待自检恢复时跑」）；新增的三个可测口：
  `TutorialPointerUi.Action/StartPointPx/EndPointPx` · `FriendsTab.SortedFriendsForTest()/SortByName/SortByConnected` · 淡出走 `TutorialOverlay.SetTip(false)` + `Tick(dt)`（旧的淡入断言链不变）。
- 判据三份已按新账重生成（§五·b / §六·b 手写段原样保留）。

## 六、没查清
1. `ClickAndHold` 与 `ClickInPosition` 在本 prefab 上**可见行为完全一致**（`clickAnim = null`）—— 我按伪码实现了独立状态机，但「Hold 到底想表达什么」在数据里读不出（只有 clickAnim 那一条差别）。
2. `FriendsTab` 的 `CreateEntry` 原版返回 `RectTransform` 并以 index 插到容器；我们的 `CreateEntryAt` 仍按「先算矩形再建」走（build 全表时用），**单条插入**的调用点没有。
3. 成就域弱件判 A 是「页面/引擎已建 + 类级语义核对」，**没有逐句读每个容器组件的方法体**（件很小、只建节点）。
