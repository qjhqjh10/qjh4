# B94-R-I · 220 条 `n<3` 无落点死代码 —— B93_Q3 三档判定落账

- 会话：第九十四会话 · 波 1.5
- 输入：`判据/B93_Q3_无落点220.tsv`（220 行，2026-10-28 21:44）
- 产出：`账/_parts/B94_RI.tsv`（**130 行** + 表头）
- 红线遵守：⛔ 未改源码 · ⛔ 未改账主表 · ⛔ 未改正本/所有权表 · ⛔ 未跑 Unity · ⛔ 未动 git

---

## 一、结论（一句话）

**220 条全部定位到主表唯一一行（0 条「已不存在」）；按 Q3 三档回填 `状态 / 我们的落点 / 差异 / 备注 / 轮次` 写了 130 行，另 90 行按证据**不写**（59 条 Q3 判定与主表冲突、25 条泛型历史重复行、4 条已带 Q3 判定、2 条已被别的 B94 补丁覆盖）。**

| 口径 | 数字 |
|---|---:|
| Q3 总行 | 220 |
| 定位到主表唯一一行 | **220**（0 条落空、0 条歧义） |
| 回填（写进补丁） | **130** = A 110 · D 14 · C 6 |
| 未写 · 冲突（Q3 档 ≠ 主表状态） | **59** |
| 未写 · 泛型历史重复行（「已并入主题表」） | **25** |
| 未写 · 该行已带 Q3 判定 | **4** |
| 未写 · 同键已在别的 `B94_*` 补丁里 | **2**（`B94_S2` / `B94_S4`） |
| **状态变更** | **0**（无一条需要改状态，见 §五） |
| Q3 账行号已漂 | **78 / 220** |

> 🔴 **与派工简报的出入（必须先看）**：简报说「这批里只有 12 条已落账」。实测**不是**：
> `161 / 220` 的主表**状态已经等于 Q3 的档位**，`179 / 220` 的 `我们的落点` 已经带 `.cs:行号` ——
> 也就是说这批大多已被 **B88–B93 的后续轮次补过**，真正的缺口是「**判定记录没写进去**」，不是「没落点」。
> 只有 **5 行**正文真的引用过 Q3（`B93_E15` 落过一次）。⇒ 我按「补记录」执行，**没有把 220 条当 208 条新活重做**。

---

## 二、回填规则（三档 → 三格）

| Q3 档位 | 含义 | 本批做法 |
|---|---|---|
| **A**（142，写 110） | 其实有等价实现 | 状态保持 A；`差异` 追加 `B93_Q3 三档 = A：等价实现已落点；残留 = <Q3「还差什么」>`；`我们的落点` **不动**（110 条已全部带 `.cs:行号`） |
| **B/C**（65，写 6） | 休眠但要做 / 未实现 | **⛔ 不判 D**；`差异` 追加 `三档 = B/C：<休眠/远端/未实现>；还差 = <Q3 还差什么>`；`我们的落点` 只在**纯占位符**（`—（无）`/`—（不复刻）`）时补成 `—（未实现/未接线）· Q3 还差：…` |
| **D**（22，写 14） | 不复刻 | `我们的落点` = **`不复刻 —— <证据>`**（14 条里 13 条原来是 `—（不复刻）`/`—（无）` 这类无证据占位）；`差异` 追加 **`原版这条路不可观测`** |
| 全部 130 行 | —— | `轮次` 追加 `· B94-R-I`；`备注` 追加 `B94-R-I 2026-10-29：按 判据/B93_Q3_无落点220.tsv 第 N 行三档复核 = <档>` |

「休眠」判法（Q3 文本里出现 `0 调用点 / 不可达 / 0 消费者 / 全量反编译 0` 即判休眠）——
例：`Extensions.AlmostEquals(Quaternion…)` 主表 C + 已写「原版 0 调用点、⛔ 别新建没人调的静态类」，本批补上「休眠（⛔ 不判 D）」这句留痕。

---

## 三、130 行改动清单（按主表分）

| 主表 | 行数 | 其中 A / C / D |
|---|---:|---|
| 未分类_其他 | 42 | 29 / 1 / 12 |
| 其他_基础设施 | 26 | 25 / 1 / 0 |
| 菜单_外壳 | 23 | 23 / 0 / 0 |
| 日常_奖励 | 22 | 21 / 0 / 1 |
| 商店 | 7 | 7 / 0 / 0 |
| 战斗逻辑驱动 | 6 | 1 / 4 / 1 |
| 收藏 | 2 | 2 / 0 / 0 |
| 特效动画 | 1 | 1 / 0 / 0 |
| 轮抽 | 1 | 1 / 0 / 0 |

按列统计（与主表现读逐列比对）：`差异` 130 · `轮次` 130 · `备注` 130 · `我们的落点` **14**；`状态 / 判据出处 / 判据要点 / 等级` **0 变动**。

**校验**
- `merge_ledger_parts.py --dry --only B94_RI`：**更新 130 · 新增 0 · 无变化 0**（无 >1 处歧义、无被拒行）。
- `check_patch_conflicts.py`：跨补丁重复键 **1 条 = `PracticeEvent.Initialize()`（`B94_S4:22` vs `B94_W1:2`）**，**与 `B94_RI` 无关** ⇒ 本补丁可与任意顺序合并。

---

## 四、90 条未写 —— 逐条

### 4.1 冲突 59 条：Q3 档 ≠ 主表状态 ⇒ **一律以主表为准，不动**

**为什么不按 Q3 改**：逐条读下来，59 条里主表那一侧**都带着比 Q3 更晚或更深的证据**（`B90-N3/N4/N6` · `B91-*` · `B92_N5/N6b` · `B93_D1/D2/D4/U1/U1c/N5/E13/E15/L3` · 调度台复核），
而 Q3 那一侧的「证据」多处**引用的是已被后续轮次推翻的旧句**。抽 5 条回源核过（见 §六），主表全对。

分布：`Q3 C→主表 D` 14 · `C→B` 13 · `B→A` 10 · `C→A` 9 · `D→B` 3 · `A→B` 3 · `D→C` 3 · `D→A` 2 · `A→D` 1 · `A→C` 1。

| 主表 | 类.方法 | Q3 档 | 主表状态 |
|---|---|---|---|
| 其他_基础设施 | `PlayerDataManager._003CRecordTutorialFinished_003Eg__HandleSuccess_007C464_0(...)` | C | D |
| 其他_基础设施 | `PlayerDataManager._003CRecordTutorialPassed_003Eg__HandleSuccess_007C465_0()` | C | D |
| 其他_基础设施 | `PlayerDataManager.showDevTools()` | B | A |
| 商店 | `FixedDateDailyStoreItem.OnAfterWrite(JToken rawJson)` | C | D |
| 商店 | `GachaEventData._003Cget_Items_003Eb__5_0(LoadableReference<ContainerOfferData> o)` | C | D |
| 商店 | `MainMenuOfferContainer.OnInitialize(LiveOpsEvent playEvent)` | C | B |
| 战役 | `CampaignPointDrawer.DrawForArmy(CardArmy army)` | C | A |
| 战斗逻辑驱动 | `BattleManager.ReplaceHeroPower(RawCardScript newPower, bool isPlayer, int priority)` | C | D |
| 战斗逻辑驱动 | `CardScript.SetHealthToZero()` | A | D |
| 战斗逻辑驱动 | `CardScript.ToggleCombatState(bool state)` | B | A |
| 战斗逻辑驱动 | `RawCardScript.PreloadAssets()` | D | B |
| 日常_奖励 | `ChallengeManager.CancelDueling(bool fromClick)` | C | B |
| 日常_奖励 | `ChallengeManager.CancelPracticeMatchAndRefresh(GameObject source)` | B | A |
| 日常_奖励 | `EventBadge.ShowNotification(ToastNotificationContext context)` | A | B |
| 日常_奖励 | `EventBadgeController.Awake()` | A | B |
| 日常_奖励 | `EventImageSelectorController.Initialize(List<EventAsset> eventAssets)` | C | A |
| 日常_奖励 | `EventImageSelectorController.TryDraw(List<EventAsset> eventAssets)` | C | A |
| 日常_奖励 | `LiveOpsAssetUtility.LoadComponent<T>(ILiveOps liveops, EventComponentReferenceType type)` | C | B |
| 日常_奖励 | `LiveOpsAssetUtility.ReleaseEventAssets(ILiveOps liveOps)` | D | A |
| 日常_奖励 | `LiveopUIDrawer_EventAssetSprites.EventAssetImages.Initialize(ILiveOps liveopEvent)` | C | A |
| 日常_奖励 | `LiveopUIDrawer_EventAssetSprites.LiveopUIDrawer_EventAssetSprites.Initialize(ILiveOps)` | C | A |
| 日常_奖励 | `MissionEvent.ReleaseContainer()` | D | B |
| 日常_奖励 | `MissionsConfig.CleanUp()` | D | B |
| 日常_奖励 | `OnQuestionAnswered.OnAnswered(FeedbackQuestionItem item)` | C | A |
| 日常_奖励 | `OnQuestionAnswered.Start()` | C | A |
| 日常_奖励 | `OpenEventButton.Initialize(ILiveOps liveOps)` | B | A |
| 日常_奖励 | `PracticeEvent.OnMatchFinished(float matchWon, RankedMatchResult result, Action<OnlineMatchResult> callback)` | B | A |
| 日常_奖励 | `PracticeEvent.OnMatchWon()` | B | A |
| 日常_奖励 | `RaidOrArmyReleaseMenuContainer.OnEnable()` | C | B |
| 日常_奖励 | `SurvivalEventWindowEnemyMarker.Awake()` | C | B |
| 日常_奖励 | `SurvivalEventWindowEnemyMarker.SetEnemyName(string name)` | C | B |
| 日常_奖励 | `SurvivalEventWindowEnemyMarker.SetHiddenEnemy()` | C | B |
| 日常_奖励 | `TimerDisplay.LocalizationChange()` | B | A |
| 日常_奖励 | `TwoSidesEvent.PurchaseMilestone(int milestoneIndex, Action<RewardInfo[]> onComplete, Action<string> onError)` | C | D |
| 日常_奖励 | `TwoSidesEvent.TryColllect(Action<RewardInfo[]> onComplete, Action<string> onError)` | C | D |
| 日常_奖励 | `TwoSidesEvent_OnGoingTab.CollectClicked()` | C | B |
| 未分类_其他 | `CrappyDate.IsvalidDate()` | C | D |
| 未分类_其他 | `CustomRaycaster.Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)` | B | A |
| 未分类_其他 | `FactionScore.Initialize(CardArmy faction, int currentScore, int highestScore)` | C | B |
| 未分类_其他 | `GameObjectUniqueIdentifier.FindComponent(string id)` | C | B |
| 未分类_其他 | `UIMessageController.OnDestroy()` | A | C |
| 未分类_其他 | `UIPreferedSizeFixer.CalculateLayoutInputHorizontal()` | A | B |
| 未分类_其他 | `UnitSwarmCount.OnInitialize()` | C | A |
| 未分类_其他 | `VarsDevice.GetCardInHandYOffsetTouchInput()` | B | A |
| 泛型与启动链_补读 | `CollectionFilter<T>.FillToggleList(List<CollectionFilterToggle> currentList, int maxElements)` | D | A |
| 泛型与启动链_补读 | `ToggleCollectionFilter<T,>.FillToggleList(List<CollectionFilterToggle> currentList, int listLimit)` | B | A |
| 未分类_其他 | `ContainerBuilder<TCont>.MakeContainer(IContainerProvider evt, Transform parent)` | **D** | **C** |
| 未分类_其他 | `ContainerBuilder<TCont>.MakeContainer(IContainerProvider evt, EventComponentReferenceType, Transform parent)` | **D** | **C** |
| 未分类_其他 | `ContainerBuilder<TCont>.MakeContainer(IContainerProvider evt, UiContainer container, Transform parent)` | **D** | **C** |
| 泛型与启动链_补读 | `DynamicList<T>.ListType()` | C | D |
| 泛型与启动链_补读 | `DynamicList<T>.GetIndexType(int index)` | C | D |
| 特效动画 | `BattleManager.StopTargetedSpellAnim()` | C | D |
| 菜单_外壳 | `BoostMenu.ShowAllBoosters()` | C | D |
| 菜单_外壳 | `BoostMenu.UpdateAllPanelsTimers()` | C | D |
| 菜单_外壳 | `CardDrawer.ToggleBanned(PlayModes mode)` | C | B |
| 菜单_外壳 | `MenuSprites.GetWorldSprite(int playerTier)` | C | D |
| 菜单_外壳 | `ParentConstraint.Sync()` | C | A |
| 轮抽 | `DraftModeMenuPayStateTimed.OnOpen()` | C | B |
| 轮抽 | `DraftModeWarlordSelectionStateTimed.OnOpen()` | C | B |

> 三个 `ContainerBuilder` 行加粗：**Q3 判 D 是误读**（见 §六·2 / §七·4），主表 C 正确。

### 4.2 25 条：`泛型与启动链_补读.tsv` 的「已并入主题表」历史行

这些行正文自己写着「**已并入主题表** `X.tsv:NNN`（同键同判）… 本行为历史记账，⛔ 别再单独动」⇒ **按该行的指示不写**。
覆盖（25 = 8 + 2 + 1 + 13 + 1）：`LoadableReference<T>` 的 `SetReferenceById` / `SetReferenceByObject` / `Equals`×2 / `GetHashCode` / `OnBeforeWrite` / `OnBeforeRead` / `WriteJson`（8）· `CollectionDisplay<T>` 的 `OnEndDragEvent` / `ReorderCollectionByHierarchy`（2）· `CollectionItem<T>.OnPointerEnter`（1）· `LiveOpsHandler<TEvent>` 的 13 个方法（`UpdateCachedSave` / `RegisterManager` / `ValidateData` / `SetupEvents` / `DeserializeSharedData` / `TryScheduleHandlerRefresh` / `ScheduleHandlerRefresh` / `RefreshHandler` / `RefreshHandlerLocally` / `HandleBeforeRefresh` / `InitializeHandler` / `ResetEvents` / `GetBlockedEvents`）· `Decorator<T>.OnObjectUpdated`（1）。

### 4.3 4 条：该行正文已带 Q3 判定（前一批 `B93_E15` 落过）

- `日常_奖励` `PracticeEvent.Initialize()`（Q3 B / 主表 B）
- `未分类_其他` `CollectSpiritStone.OnInitialize()`（A/A）
- `未分类_其他` `PlayerInfo.SetBotInfo(string botName, int botElo)`（C/C）
- `未分类_其他` `ScriptedAction.GetTextReference()`（A/A）

### 4.4 2 条：同键已在别的 `B94_*` 补丁里（避免跨补丁重复键）

- `日常_奖励` `MissionsTab.InstantiateGridMissions(IEnumerable<IMission> missions)` → `B94_S2.tsv`
- `未分类_其他` `ArmyProgressBar.SetArmies(CardArmy leftArmy, CardArmy rightArmy)` → `B94_S4.tsv`

---

## 五、为什么**一条状态都不用改**

Q3 的三档本来就是把主表逐行重判一遍（160/220 同档就是证据）。真正不一致的 59 条，主表一侧全部带更晚轮次的证据；抽 5 条回源实读（§六），**主表 5/5 对、Q3 5/5 过期或误读**。
⇒ 强行按 Q3 改会**推翻 30+ 条后续轮次结论**（正是 `CLAUDE.md` 反复警告的那类事故）。所以本批只补「判定留痕」，不改判定本身。

---

## 六、抽查与反证（实读过的 5 处）

1. **`ContainerBuilder` ×3（Q3 判 D → 主表 C）**：Q3 的依据是「调度台 C1 2026-10-27 已裁定用自设计口替代（`Shell/LiveOpsEvent.cs:1057-1060`）」。
   **实读该处原文**：`/// 🔶 自己设计（非原版）：UiContainer 整族（IUiContainer / UiContainer<T> / ContainerBuilder<T>）在我们仓库里**还是 C**（属别的批：容器/窗口线）… ⛔ 别把它当掉 IUiContainer 的复刻。`
   ⇒ 原文说的是「**还是 C**」，Q3 读成了「已自设计替代 ⇒ 不复刻」。**主表 C 正确**。
2. **`UIMessageController.OnDestroy`（Q3 判 A → 主表 C）**：Q3 的等价物写 `Battle/ErrorMessageBanner.cs:66（Awake 单例守卫）`。
   **实读**：`:66` 是 **XML 注释**（`/// ⑤ Awake（UIMessageController__Awake.c 逐句）…`）；类本身在 `:205`，`grep -n "Awake|Instance"` **该类无 Awake、无 Instance**。⇒ Q3 把注释当成了实现。**主表 C 正确**。
3. **`CustomRaycaster.Raycast`（Q3 判 B → 主表 A）**：`Shell/RaycastFilter.cs:141` **确有** `public static bool AllowsHit(GameObject button, GameObject hitGraphic)`，消费口 = `PointerLayer.HitButton` 排序前逐条问。Q3 引的是已被 `B89-RF` 就地订正的旧句。**主表 A 正确**。
4. **`EventImageSelectorController.Initialize/TryDraw` 与 `LiveopUIDrawer_EventAssetSprites.*`（Q3 判 C → 主表 A）**：`Shell/EventImageSelector.cs` **确有** `:133 Initialize(List<ImageEventDrawData>, List<EventAsset>)` · `:109 TryDraw` · `:164 EventAssetImages.Initialize` · `:182` 外层 `Initialize`。**主表 A 正确**。
5. **`BattleManager.ReplaceHeroPower`（Q3 判 C → 主表 D）**：按 `CLAUDE.md`「0 调用点可能是内联假象」复核 —— `decomp_full` 全树 `grep -rl ReplaceHeroPower` **只命中它自己那一份**，`BattleManager__ResolveAction.c` 里 `ReplaceHeroPower / 0x208 / heroPower` **0 命中** ⇒ 不是被 91 路 switch 内联进去，**主表 D（原版不可达）成立**。

---

## 七、顺手发现（⛔ 都没改，只报）

1. 🔴 **「只落了 12 条」这个前提是错的**：`161/220` 状态已与 Q3 同档，`179/220` 落点已带 `.cs:行号`。真正的缺口是「判定没写进账」。后续若再派同类「回填」活，应先跑一次 `状态 vs 判定档` 的比对，而不是按「未落点」名单重做。
2. 🔴 **Q3 的证据普遍比主表旧**：多处引用的句子已被 `B89-RF`（`CustomRaycaster`「教程层没有过滤」）、`B92_N5`（`TimerDisplay.LocalizationChange` 已实现接线、`ParentConstraint.Sync` 已逐句等价）、`B93_D2`（`EventImageSelector` 已建）、`B93_U1c`（`ErrorMessageBanner` 无 Awake）**就地推翻**。⇒ **Q3 只适合当「三档索引」，不能当判据本体**。
3. 🔴 **Q3 有把「注释行」当落点的现象**（`ErrorMessageBanner.cs:66`）。凡 Q3「等价物」指向的行，用前必须现读该行**是不是代码**。
4. 🔴 **`LiveOpsEvent.cs:1057-1060` 的 C1 注释被读反了**：原文是「`UiContainer` 整族**还是 C**、属别的批、⛔ 别把它当掉」，Q3（与更早的 `B91-Q2`）读成「已用自设计口替代 ⇒ 不复刻」。
   ⇒ **受影响的 6 行**（`泛型与启动链_补读` `UiContainer<T>.{Initialize(object),Initialize(T),OnDestroy,OnInitialize,HandleClick,OnClick}`）现判 **D**，按注释原文它们应当是 **C（别的批要做）**。**本批未改**（要动是 6 行状态变更，超出「按 Q3 回填」的授权，且需先定「别的批」是谁）。→ 列为待裁。
5. ⚠️ **`泛型与启动链_补读.tsv` 有 415 行的 `原版类` 在第一个逗号处被截断**：`RankedEventBase<TSave,` / `ToggleCollectionFilter<T,` / `MultiKeyDictionary<T1,` / `<>f__AnonymousType75<<DeckId>j__TPar,`。
   与调度台日志 21·6 里挂着的「`原版类` 列残缺 448」大概率同源（口径差异见 §八）。**合并键 `(原版类, 方法)` 仍唯一** ⇒ 不影响合并，但**按类名 grep 会漏**。
6. ⚠️ **泛型文件行号漂移最大**：Q3 记的 `609/610/611` 实际在 `未分类_其他.tsv:180/181/182`；`645/646` 实际在 `泛型…:612/613`。又一次印证「**账行号别信，按方法名认**」（本次 220 条里 **78 条**行号已漂）。
7. ⚠️ **Q3 有 2 条是「同一个方法写了两遍」**：`LiveopUIDrawer_EventAssetSprites.LiveopUIDrawer_EventAssetSprites.Initialize(ILiveOps)` 这段类名被重复拼接（真实类名只有一层）。只影响显示，不影响合并。
8. ℹ️ `B93_Q3` 的域值与主表文件名不同源：Q3 的 `账域` 是 `其他_基础设施`（下划线），主表 `域` 列是 `其他/基础设施`（斜杠）、文件名是下划线。**按域硬映射会把泛型表的行错并进别的表** —— 本批改成「**先在本域文件按方法名找，找不到再跨域按方法名找**」，9 条靠这条规则救回来（含 3 个 `ContainerBuilder`）。

---

## 八、没查清 / 还差什么

1. **`Q3 C → 主表 D` 那 14 条「D 是否都站得住」**：只回源核了 2 条（`CrappyDate.IsvalidDate`：全量反编译 0 调用点 + 我们存档不会写出 0/0/0；`TwoSidesEvent.PurchaseMilestone/TryColllect`：体是 PlayFab 云脚本 + 用户边界②不做真实经济）。其余 12 条**只读了现有证据、未逐条回源 `.c`** ⇒ 「没发现破绽」≠「已核完」。
2. **`UiContainer<T>` 族 6 行该 D 还是 C**（可确定性存疑，见 §七·4）⇒ 需调度台裁一次。
3. **25 条泛型历史重复行的「主题表那一行」是否也与 Q3 同判**：只核了目标行存在，未逐条比对状态。
4. **`原版类` 截断 415 行 vs 调度台记的 448**：口径差 33 行（我数的是「以逗号结尾」，那边可能还含别的残缺形态）⇒ 修这条时以调度台的清单为准。
5. **`B94_RI` 与其他在飞补丁的时间差**：波 1 的 9 个执行代理仍在跑，若之后有人再改这 130 行中的某行，按 `合并顺序 = 后写赢` 处理（本补丁只动 `差异/轮次/备注` + 14 行 `落点`，冲突时以内容更全的一方为准）。

---

## 九、合并建议

1. `check_patch_conflicts.py` 现读（2026-10-29 收工前最后一次）：**跨补丁重复键 2 条** —— `BattleManager.GetTargetInPos(Vector3)`（`B94_F5:18` vs `B94_X2:4`）· `PracticeEvent.Initialize()`（`B94_S4:22` vs `B94_W1:2`）。
   **两条都与 `B94_RI` 无关**（RI 的 130 个键在别的 `B94_*` 补丁里 0 命中）⇒ **`B94_RI` 可任意顺序合并**（建议按 `--only B94_RI` 单独一次）。
2. 合并前 `--dry` 已验：**更新 130 · 新增 0 · 无"命中 >1"、无不合规行**。
3. 本补丁 **状态零变更、A 行落点零变更** ⇒ **不会引入 `check_ledger_points` 漂移**，合完按 21·7 的顺序走第 5 步即可（`--all-A --fix` 仍然只在「无 `.cs` 写手 + 无账写手」窗口跑）。
