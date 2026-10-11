# Q-卡底小圆图标 —— 原版「模式卡底部那一排小圆图标」是什么

> 只读诊断。判据 = `d:/2/新解包资源/assets_full/bundle_menus_assets_all/`（GO/RT/MB 逐 pid 直读）+
> `d:/2/tools/decomp_full/`（反编译） + 参考图逐像素量。⛔ 本轮未跑 Unity、未改任何代码。

## 结论（一句话）

**那是【排位卡】的「段位印玺（seal）进度条」** —— 父链
`Ranked Game Mode Container 1x2`（或 `Skirmish Ranked Game Mode Container 1x2`）→ `Rating` →
**`Mission Milestones Progress`**（MB **`MilestonesStepDisplay`**）→ **`steps`**（HorizontalLayoutGroup）→
**N 个 `RankedSealStep`**；每颗 = 两个**互斥**的 Image 子件 **`Empty`**（sprite **`Rank Skull Empty`**）/
**`Fill`**（sprite **`Rank Skull`**）。
**颗数 = `RankedDivisionV2.TotalSeals`（远端配置）· 点亮数 = `SealCount`（玩家进度）** ⇒ 参考图那 4 颗 = 该段位 `TotalSeals=4`、玩家 `SealCount=0`（4 颗全是灰的 `Rank Skull Empty`）。
参考图里那张中卡**就是排位卡**：卡图 = **`Container Image Ranked`**（蓝极限战士链锯剑 vs 绿兽人、兽人肩膀上**黑白棋盘格**护肩 —— 逐点核过）。
⛔ `Base Game Mode Container 1x1` / `1x2` 这两棵通用卡树里**完全没有这一排**（下面第一节）。

## 那排小圆的节点表

### A. `Base Game Mode Container 1x1` 与 `1x2` —— **没有这一排**（两档都实测过）
直读全子树（`工具/menu_dump.py bundle_menus_assets_all "<名>" --depth 9 --relative`）：
- `1x1`：root（`LiveopMenuContainer`）4 子 = `Background Image` / `Foreground Image` / `TextDarkening`(→`Event Title`+`Timer With Time Description`) / `Border`。
- `1x2`：同 4 子，尺寸 535×848.8，`TextDarkening` 落 y 740.4~846.3。
- `1x1 Claim` / `1x1 Claim AutoCollect` / `1x2 No Timer` / `1x2 Raid or Army Release` / `Reward Event Container 1x1` 也**都没有**这一排。

### B. `Ranked Game Mode Container 1x2`（**有**；`Skirmish Ranked Game Mode Container 1x2` 同构）
| 深 | 名字 | 原始 RT（anc / piv / ap / sz）| 布局后绝对矩形（卡内，相对卡左上）| 图 / 文本 | 出厂 `m_IsActive` |
|---|---|---|---|---|---|
| 0 | `Ranked Game Mode Container 1x2` | — | 535×848.8 | — | **True**（MB `RankedMenuContainer`）|
| 1 | `Rating` | a(.5,0)-(.5,0) p(.5,0) ap(0,**106.4005**) sz(**494.61×109.52**)| x 20.2~514.8 · **y 632.9~742.4** | — | **True** |
| 2 | `Mission Milestones Progress` | a(0,1)-(0,1) p(.5,.5) ap(247.3059,−54.7622) sz(**494.61×86.51**) | x 20.2~514.8 · **y 644.4~730.9** | MB **`MilestonesStepDisplay`**（`stepsHolder`→`steps`，`stepPrefab`=[`RankedSealStep`] 一条）| **True** |
| 3 | `steps` | a(0,0)-(1,1) p(.5,.5) ap(0,0) sz(**0×0**) | 同父件 | **`HorizontalLayoutGroup`**：spacing **3.2** · align **3(MiddleLeft)** · pad 0 · ctrlW/H **1** · expandW/H **0** | **True** |
| 4 | `RankedSealStep Animation Disabled`（出厂**模板 1 个**，运行时被 `DestroyAllChildren` 后按 N 重建）| a(0,0)-(0,0) ap(0,0) sz(**0×0**) | 单颗 100×86.51（**`LayoutElement` preferred 100×100**）| MB **`MilestoneStep`**（`checkMark` / `emptyMark` / `alwaysShowEmptyMark`=**0** / `doEnterAnimation`=**0** / `animationTime` 0.8 / `animationBetweenStepsDelay` 0.3 / `animationStartDelay` 0.5 / `animationScaleInitialMultiplier` 3.0）· 自己那颗 Image sprite=`Rank Skull` **alpha=0**（不画）| **True** |
| 5 | `Empty` | a(0,0)-(1,1) p(.5,.5) sz(0×0) | 铺满 100×86.51 | Image **`Rank Skull Empty`**（pid `-7214020762604782334`）· `preserveAspect` · color 白 α1 —— **= `emptyMark`** | **True** |
| 5 | `Fill` | a(0,0)-(1,1) p(.5,.5) sz(0×0) | 铺满 100×86.51 | Image **`Rank Skull`**（pid `-300467167438017286`）· `preserveAspect` · color 白 α1 —— **= `checkMark`** | **True** |

- **节距 = 100 + 3.2 = 103.2 单位**；4 颗 ⇒ 20.2 + 3×103.2 + 100 = **429.8**，落在 494.61 宽的行里（`align=3` 左对齐）。
- 同一父件 `Rating` 下**还有一支互斥的另一支**：`Global Rating`（MB `AllianceRatingDisplay`；子件 `Main Icon`/`Secondary Icon` sprite `40k_UI_icon_ranked_Skirmish` · `Position #`/`Position Value` TMP · `Individual rating value`）。运行时**二选一**显示。
- 参考图逐像素复核（`C:/Users/qjh36/Desktop/warpforge参考图/主界面参考图.png`，1503×883）：中卡 y **149.5~845**（宽 397）、那排 4 颗落在 y **699~750** ⇒ 归一化 **0.790~0.863**；
  上表 `steps` 带归一化 = **0.759~0.861**（`Rating` 带 0.746~0.875）⇒ **吻合**（这一段是尺子无关的比值，避开了「卡被竖向拉高」这个未定的缩放）。

## 显隐判据（赋值点）

1. **出厂**：`Rating` / `Mission Milestones Progress` / `steps` / 模板步 / `Empty` / `Fill` **全是 `m_IsActive = True`**（逐个 GO 直读）。
2. 🔴 **赋值点 ①（整排开关）** = `RankedMenuContainer__RefreshContentDisplay.c`（`d:/2/tools/decomp_full/`）：
   · 事件为空 ⇒ `milestonesStepDisplay.gameObject.SetActive(false)`；
   · 否则 `SetActive(go, X == 0)` —— `X` 来自 `FUN_1800021f0(3, <事件接口类>, event)`。
   ⛔ **那个泛型调用的方法名没解出来**（`FUN_1800021f0` 在本仓反编译里的调用约定 = 「(泛型实例序号, 类句柄, 实参…)」，615 个 `.c` 里都这么出现）⇒ **如实写「方法名读不到」**，但它决定的是「显示 seal 行 / 显示 `Global Rating` 那一支」这两条互斥路。
3. 🔴 **赋值点 ②（逐颗点亮）** = `MilestonesStepDisplay__Initialize.c`：
   `SupportMethods.DestroyAllChildren(stepsHolder)` → 循环 `i in [0, param_3)`：`Instantiate(stepPrefab.ElementAtOrDefault(i) ?? Last(), steps)` → `MilestoneStep.Toggle(step, i < param_2, i, …)`。
   · `param_3` = **颗数** · `param_2` = **已达成数**（序列即下面第 4 条的 `(SealCount, TotalSeals)`）。
4. **赋值点 ③（颜色/图）** = `MilestoneStep__Toggle.c`：
   `checkMark.enabled = on`；`emptyMark.enabled = (on ? alwaysShowEmptyMark : true)`。
   · 本卡 prefab 里 **`alwaysShowEmptyMark = 0`** ⇒ **达成的只画金色 `Rank Skull`、未达成的只画灰色 `Rank Skull Empty`**。
   · 注意走的是 **`Behaviour.set_enabled`（组件级）**，⛔ 不是 `GameObject.SetActive`（两个子件 GO 恒 active）。
5. 调用上游 = `RankedMenuContainer__Refresh.c` → `RefreshContentDisplay(GetActiveEvent<…>())`。

## 内容来源（本地固定 / 远端）

| 项 | 来源 | 判据 |
|---|---|---|
| **图**（`Rank Skull` / `Rank Skull Empty`）| **本地固定 2 张** | `bundle_liveopsicons_assets_all`（另有 `bundle_menus_assets_all`/`sharedassets0` 同名副本），128×128 |
| **颗数** | 🔴 **远端** | `RankedDivisionV2.TotalSeals`（0x54）——`MilestonesStepDisplay.Initialize` 的第 3 实参；`RankedDivisionV2` 由 `RankedFastMode__GetDivisionByPoints.c` 用 **`Everguild.LiveOps.Config.ConfigManager.GetConfig<…>()`**（远端配置）构造 |
| **点亮数** | 🔴 **远端 / 玩家存档** | `RankedDivisionV2.SealCount`（0x50）—— 第 2 实参 |
| 是否播进场动画 | 运行时 | `MainMenuMatchResultData.HasToDoSealsAnim()`（`Initialize` 的 param_5/param_6，`RefreshContentDisplay` 传入）|

⇒ **「4」不是原版写死的常数**，是那个段位的 `TotalSeals`。**本地推不出 4**（配置在远端）⇒ 复刻时只能照「N = 段位配置、点亮 = 玩家进度」建，**N 取什么要另定**（本地兜底值）。

## 搜过哪几个包 / 搜过哪些词

- **包**：`bundle_menus_assets_all`（16768 GO / 16510 RT / 34905 MB 全量扫）、`bundle_liveopsmenuimages_assets_all`、`bundle_liveopsicons_assets_all`、`bundle_atlasindividual_assets_0_mainmenu`；切片缓存 `d:/2/Warpforge_tools/data/ui_extract/*`（25 个子目录）；反编译 `d:/2/tools/decomp_full/`（`wc -l`/`grep -rl`）；`d:/2/tools/il2cpp_out/dump.cs`。
- **词**：`GameMode` / `game mode` / `Ranked` / `Skirmish` / `Rank Skull` / `Rank Skull Empty` / `Milestone` / `MilestonesStepDisplay` / `MilestoneStep` / `MissionMilestones` / `RankedDivisionV2` / `RankedMenuContainer` / `RankedArmyBonusContainer` / `RankedGameMode` / `icon` / `rewardIcon` / `Seal`（在 GO 名、MB 字段名、类名、Sprite 名四个面上各扫了一遍）。
- **筛法**（补一条常规搜法搜不到的）：扫全包 1269 个「边长 20~90」的小节点，再按「同一父件下 ≥3 个同尺寸兄弟」聚类 —— 得 `steps`/`Counters`/`Buttons` 等 15 组，逐组排除后只剩排位族的 `steps`。

## 顺手发现

1. 🔴 **我们的缺口**：`Shell/MainMenuRuntime.cs:1464` 那张 `RANKED` 卡（`Base Game Mode Container 1x2 - Ranked`）**只建了底图 + 标题**，没有 `Rating`/`Mission Milestones Progress`/`steps` 这一排 —— 与参考图逐点比就是差这一排（4 颗）。同族的 `Skirmish`（`:1456`）**原版那棵 `Base Game Mode Container 1x2` 本来就没有这一排**，⛔ 别顺手给它也加。
2. **同一父件下两支互斥**：`Rating` 里 `Mission Milestones Progress`（seal 行）与 `Global Rating`（`AllianceRatingDisplay`：大段位图 + `Position #`/`Position Value` + `Individual rating value`）**运行时二选一** —— 复刻只做一支会露馅。
3. **`Draft` 卡上有一颗长得很像的东西，别搞混**：`Draft Game Mode Container 1x2` 的 `State` 子件 = `Victory Counter Icon`（sprite 也是 **`Rank Skull`**，97.45×103.33）+ `Victories` TMP `2/12`（fs 99.69）—— 那是**单个**胜场图标，不是这一排。
4. **`Skirmish Ranked Game Mode Container 1x2` 与 `Ranked …` 的差异**只有：前者 root 多一个 `GameObjectUniqueIdentifier`（字段 `id`）；**`RankedMenuContainer` 的字段表逐字相同**（`backgroundButton/title/timer/backgroundImage/foregroundImage/milestonesStepDisplay/legendaryRankDivisionInfoObjects/rankedPositionValue/ratingDisplay`）。
5. **`legendaryRankDivisionInfoObjects` 名字唬人，不是那排小圆** —— 实读只指向 `Position #` 与 `Position Value` 两颗（`Ranked` 卡实测，fileID=0）。
6. ⚠️ **我一度把它读成「艺术图里烘死的 4 个徽章」**：`Container Image Ranked.png` 里没有那 4 颗（已逐张看过 `Ranked`/`Draft`/`Skirmish`/`Tutorial`/`DailyStreak`/`Beast Snagga`/`Guilliman`/`Space Wolves`/`Battle For Warpforge`/`TwoSides` 十张 1024² 原图）⇒ **是 UI 节点，不是画进图里的**。
