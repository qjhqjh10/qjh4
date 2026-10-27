# B9_C3T4e · 未分类/其他线 —— B 行核账 + 落点重钉

> 子代理 **C3T4**（查判据型 · 只读）· 2026-10-27
> 范围：`d:/4/Unity/资料/完全复刻/账/未分类_其他.tsv` 的 **B 行全部 101 条**（现读：754 行，A 124 / B 101 / C 47 / D 482）。核账与落点重钉一次做完。
> 产出：`判据/B行逐条核_未分类.tsv`（8 列 × 101）· `判据/落点重钉_未分类.tsv`（6 列 × 101）· `账/_parts/B9_C3T4e.tsv`（11 列 × 101）· 本报告。

## 一、方法

1. 逐行读 `判据出处` → `decomp_full`，`我们的落点` → 打开该行看是不是方法声明。
2. 判 A / B / 落点失效 / 判据缺；**落点失效的当场按方法名到 `Assets/**/*.cs` 找真身重钉**（找到 ⇒ 新落点 `路径:行（裸名）（等价实现 = …）`；找不到 ⇒ 写「无落点：<为什么>」，⛔ 不硬指字段/注释）。
3. 🔴 **每条重钉的都实际打开目标行、抄该行原文进本报告 §三**。
4. 产出前按「方法名 + 旧落点」对了一眼当前主表；`check_patch_conflicts.py` → 无跨补丁重复键（5 份补丁）。

## 二、结果分布

| 建议等级 | 条数 | 说明 |
|---|---|---|
| **A（等价，已重钉）** | 6 | 行为等价且找到真实方法 |
| **B（真偏离，已重钉）** | 5 | 找到真实方法但有机制差 |
| **落点失效** | 88 | 原落点非方法级（无 / 字段 / 属性 / 文件级 / 描述），且**找不到同名/同形方法**（多为「机制不同 / 无逐方法实现 / 活动线暂停」）⇒ 新落点写「无落点：原因」，**旧落点不改** |
| **判据缺** | 2 | #27 判据空（描述全空）· #96 方法体撞名丢失 |
| 合计 | 101 | |

## 三、11 条已重钉（每条都实际打开该行，原文前 80 字符见下）

| # | 原版方法 | 新落点（文件:行（裸名）） | 该行原文（前 80 字符） | 等价 |
|---|---|---|---|---|
| 1 | `ActionScheduler.Release(ScheduledAction)` | `CardPresentation/Shell/LiveOpsHandler.cs:81（Release）` | `public static void Release(LiveOpsScheduledRefresh handle)` | 是 → **A** |
| 2 | `ActionScheduler.Update()` | `CardPresentation/Shell/LiveOpsHandler.cs:90（Tick）` | `public static int Tick(DateTime utcNow)` | 部分 → **B**（原版每帧最多弹 1 条，我们扫全表全弹） |
| 12 | `CatalogItemContainer.Refresh()` | `CardPresentation/Shell/ShopTabPage.cs:456（RebuildForScroll）` | `void RebuildForScroll()` | 部分 → **B** |
| 32 | `Countdown.Update()` | `CardPresentation/Shell/TimerDisplay.cs:116（Update）` | `void Update()` | 部分 → **B**（缺 speedFactor 累加 + <5 秒 Play()） |
| 35 | `CreateSecret.OnAddCard(CreateSecretSignal)` | `RuleEngine/Core/AchievementTally.cs:225（AddSecret）` | `public void AddSecret(int makerSeat) { if (SeatOk(makerSeat)) SecretsMade[makerS…` | 是 → **A** |
| 41 | `EphemeralState.AddDuration(long)` | `CardPresentation/Shell/Inventory.cs:797（AddDuration）` | `public void AddDuration(long duration) { DurationMs = DurationMs + duration; }` | 是 → **A** |
| 42 | `EphemeralState.FormatTime()` | `CardPresentation/Shell/Inventory.cs:803（FormatTime）` | `public string FormatTime()` | 是 → **A**（`日` 显示口径自设计，如实标注） |
| 56 | `HuntMarkController.UpdateValue(int)` | `CardPresentation/Core/HuntMarkController.cs:89（UpdateValue）` | `public override void UpdateValue(int newValue)` | 是 → **A**（已接线 `TraitFrames.cs:1112`） |
| 67 | `OffsetMovementCurvesByAxis.GetRandomOffsetValues()` | `CardPresentation/Battle/BattleDriver.cs:23808（RandomOffsetAmp）` | `Vector3 RandomOffsetAmp(CardAnimBeat beatData)` | 是 → **A**（旧落点 :23087 是错行，实为 :23808） |
| 69 | `PlayerInfoDisplay.Awake()` | `CardPresentation/Shell/ProfileTab.cs:608（BuildPlayerInfo）` | `void BuildPlayerInfo()` | 部分 → **B** |
| 70 | `ProgressBar.SetProgress(ScoringDataInfo)` | `CardPresentation/Shell/AllianceEventScoreInfo.cs:153（SetProgress）` | `public void SetProgress(float v)` | 部分 → **B**（无 ScoringDataInfo 档） |

## 四、2 条判据缺

- **#27 `CosmeticItem.GetLocalizedDescription()`**：判据空 —— 现读 1195 个装饰品 `descriptionTextReference` **全空**、官方表 `Cosmetic_Description/*` **0 条** ⇒ 原版该方法对全部物品输出空；我们 `Item` 也不设描述字段。按铁律 11 例外①（判据空）可入 D（时间限定）。
- **#96 `UnitSwarmCount.OnInitialize()`**：原版方法体在反编译产物里**撞名丢失**；我们取「真的合并了才算」（`RuleCore.SwarmMergeNow`），与原版「按广播」是否同义**未证实**。需 `-readOnly` 泛型/撞名重导复核。

## 五、88 条落点失效（新落点=「无落点：原因」；全表原因见 `判据/落点重钉_未分类.tsv`）

按成因归五类（每条的「无落点：…」都写了具体原因）：
- **机制不同 / BCL 替代**（最多）：`ListExtensions`（Shuffle/RandomElement/SpliceWhere/GetMax/GetMin/GetIndexOf → System.Linq/List）、`RectExtensions`/`UIExtension`/`UguiRect`（PxRect 同空间，无世界矩形换算）、`GameObjectUniqueIdentifier`（过程化建、持直接引用，无 id 注册表）、`ContentSizeFitterMinMax`/`RectSizeLimiter`/`UIPreferedSizeFixer`（无通用 ILayout 组件）、`UICornersGradient`/`EmptyGraphic`（命中/渐变走 MenuDraw/CardArt 烘焙）。
- **无逐方法实现（计数随对局 Tally，无信号总线）**：`AddSabotageCard` / `CardPlayedCount` / `CollectFaith` / `GamesPlayed` / `GiveHuntsMark` / `GiveMarkOfChaos` / `SkullsCount` / `UnitReanimated` / `UpgradeCard` / `ContainersOpen` 的 `OnInitialize/OnReset/OnTrigger`（原版订阅信号，我们无信号总线）。
- **类级命中 / 方法级未核**：`Countdown` 的 Activate/ChangeSpeed/GetSecondsLeft/Stop、`CrappyDate`、`FactionScore`、`GenericArmyItem`、`MessageList`、`MilestoneScoreBar`、`MilestonesStepDisplay`、`ScriptedAction`、`VarsDevice` 等（落点只到文件/类，无同名方法）。
- **活动线暂停（通用容器构建器未做）**：#15–#21 `ContainerBuilder` / `ContainerHolder`（落点 `—`；`OfferContainer` 只覆盖商店）。
- **属性/字段 / 数据侧**：`AlternateArtStyleIconsSO`/`AlternateArtStylesUtilities`（等价物是属性 `CollectionWindow.AltStyles`）、`CosmeticItem`（判据空，见 §四）、`AvailableAction.SetParameters`（我们构造 AiAction 时直接赋字段，无 SetParameters）、`BoardCriteria`/`HealCriteria.DeepCopy`（卡定义只读不克隆）。

> 🔴 **「落点失效 / 无落点」≠ 判 D / 不做**：这些行 `状态` 仍保持原值 `B`。后续要么补方法（让落点可指），要么把内联逻辑抽成方法；`ListExtensions`/`RectExtensions` 那类「我们本就不用这套工具」的，可在主表按「原版工具类、无对应物」留证并考虑归 D（需调度台裁）。

## 六、验证与残余（如实）

- 11 条重钉**逐条打开目标行读过原文**（§三）；均为真实方法声明。
- 88 条「无落点」：搜过落点提示文件 + 全仓按方法名；原因取自 `差异`（历轮代理逐句读原版体写下的机制对比）。⚠️ 少数可能被重命名到我未猜到的名字（如 `RebuildForScroll` 之外的货架刷新、`ScriptedAction` 取词），若掌握别名可再指定。
- 补丁：`状态` 全 B 不动、`轮次`=B9_C3T4e、`我们的落点` 只对 11 条已重钉行改写（其余保留）；11 列无坏行。
- 生成器脚本：`d:/4/Unity/_tmp_view/gen_c3t4e.py`。
