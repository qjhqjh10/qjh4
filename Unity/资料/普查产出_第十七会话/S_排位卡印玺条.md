# S-排位卡印玺条 —— 给 `RANKED` 模式卡补上原版那一排「段位印玺」

> 执行代理 `S-排位卡印玺条`。**只改一个文件** `Unity/MyGame/Assets/CardPresentation/Shell/MainMenuRuntime.cs`
> （LF · 1776 → 1944 行 · 我的改动**只加不删**）。⛔ 未跑 Unity、未动 git、未碰 `Editor/`、未改正本。
> 判据 = `资料/普查产出_第十七会话/Q_卡底小圆图标.md`（**已查实，本轮按简报未复重查**）。

## 结论（一句话）

`RANKED` 卡（闸口 `modeKind == "ranked"`）现在按原版父子链
`Rating ▸ Mission Milestones Progress ▸ steps ▸ RankedSealStep ×4`（每颗两图 `Empty`/`Fill`）**建出来了**；
4 颗点亮数 0 ⇒ 只画灰 `Rank Skull Empty`、金色那颗建了但**藏着**；
「4 / 0 是我们挑的兜底、原版取自远端 `RankedDivisionV2`」已 `Debug.Log` 出声。
`Skirmish` / `Draft` / 通用卡树**一张都没碰**。

## 逐值对照表

| 件 | 判据值（`Q_卡底小圆图标.md`） | 我们代码值 | 对否 |
|---|---|---|---|
| `Rating` | a(.5,0) p(.5,0) ap(0,**106.4005**) sz(**494.61×109.52**) ⇒ 卡内 x **20.20~514.81** · y **632.88~742.40** | `:1841` `RatingL/R/T/B = 20.20/514.81/632.88/742.40`；`:1852` `New(card,"Rating", 494.61, 109.52)` | ✅ 宽高 = `514.81−20.20` / `742.40−632.88` 逐位吻合 |
| `Mission Milestones Progress` | a(0,1)（挂 `Rating`）ap(**247.3059**,**−54.7622**) sz(**494.61×86.51**) ⇒ 卡内 x 20.20~514.81 · y **644.39~730.90** | `:1842` `MmpT/MmpB = 644.39/730.90`；`:1853` `New(rating,…, 494.61, 86.51)` | ✅ 高 = `730.90−644.39` 吻合 |
| `steps` | a(0,0)-(1,1) `sz(0×0)` + **HLG** spacing **3.2** · align **3(MiddleLeft)** · pad 0 · ctrlW/H **1** · expandW/H **0** | `:1857` `New(mmp,"steps", 494.61, 86.51)`；`StepGap 3.2` · `StepPitch = 100+3.2 = 103.2`（:1846） | ✅ 节距/对齐照值；⚠️ `sizeDelta`**有意不同**（见「没做到」2） |
| 单颗 `RankedSealStep` | `LayoutElement` preferred **100×100**（高被布局组压到父件高）= 布后 **100×86.51** | `:1871` `New(steps, "RankedSealStep "+i, 100, 86.51)` | ✅ |
| 4 颗的 x（第 0 颗贴左缘） | `20.2 + 3×103.2 + 100 = **429.8**` 右端 | step0 `20.20~120.20` · step1 `123.40` · step2 `226.60` · step3 `329.80~**429.80**` | ✅ 逐值吻合 |
| `Empty` / `Fill` 两张图 | sprite **`Rank Skull Empty`** / **`Rank Skull`** · 各 128² · `preserveAspect` ⇒ 内接进 100×86.51 = **实绘 86.51² 居中** | `:1878`/`:1880` `RectTex(…, keepAspect: true)` ⇒ 86.51×86.51、左偏 6.745 | ✅ 128² 已二进制核；两张 md5 不同（非同一张） |
| 图在本地的位置 | `bundle_liveopsicons_assets_all` 等包 | **早已在**：`Resources/Art/ui_menu/Rank_Skull{,_Empty}.png`（与 `Container_Image_Ranked.png` 同目录 ⇒ 同一条取图路） | ✅ **不需新导图** |
| 整排显隐（原版） | `RankedMenuContainer__RefreshContentDisplay.c`：`SetActive(go, X == 0)`，**`X` 那个泛型方法名读不到**（`FUN_1800021f0`） | 口径 = 「**排位卡就显示这一排**」，恒亮 | ⚠️ **判据只到这里**（注释里如实写「按实拍/推断、⛔ 不是原版判据」） |
| 逐颗显隐（原版） | `MilestoneStep__Toggle.c`：`checkMark.enabled = on` · `emptyMark.enabled = on ? alwaysShowEmptyMark : true`；本卡 `alwaysShowEmptyMark = **0**` | `SealToggle`（:1910）逐字照抄那两行；`lit = i < (0)` ⇒ **4 颗全灰、金的全藏** | ✅ 语义吻合 |
| 显隐落在哪一层 | 原版 = **组件级 `set_enabled`**（两个子件 GO `m_IsActive` 恒 True） | 我们 = **GO 级 `activeSelf`**（`ImageQuad` 无 `enabled` 一档） | ⚠️ **有意不同**，代码注释写明 |
| 归一化复核 | 参考图那排卡内 **0.790~0.863** · `steps` 带设计值 **0.759~0.861** | 现算 `644.39/848.8 = 0.7592` ~ `730.90/848.8 = 0.8611` | ✅ 逐位吻合 |

## 改动清单（全部在 `Shell/MainMenuRuntime.cs`）

| 行 | 改了什么 | 判据出处 |
|---|---|---|
| `:1705~1712` | 新增调用点 `if (modeKind == "ranked") BuildRankedSealRow(card, x, y);`（④·b 步，`Border` 之后、⑤ 倒计时之前） | 简报 + `Q_…md` §「顺手发现 1」 |
| `:1760~1816` | 新增**判据段注释**（父子链 · 三条「不许扩散」· 组件级 vs GO 级显隐 · 远端颗数/点亮数 · `Global Rating` 另一支 · 归一化复核），逐句带出处 | `Q_卡底小圆图标.md` |
| `:1818` / `:1822` | `public const int RankedSealFallbackCount = 4;` / `RankedSealFallbackLit = 0;`（注释写死「**都不是原版常数**」） | `Q_…md` §「内容来源」 |
| `:1828~1898` | 新增 `BuildRankedSealRow`：建 `Rating`/`Mission Milestones Progress`/`steps`/`RankedSealStep ×4`/每颗 `Empty`+`Fill`，逐颗 `SealToggle`，末尾 `Debug.Log` 出声 | `Q_…md` 节点表 + 赋值点 ①②③ |
| `:1900~1916` | 新增 `public static void SealToggle(empty, fill, lit)` —— 原版 `MilestoneStep.Toggle` 的**唯一一份**实现 | `Q_…md` 赋值点③ |
| `:1918~1922` | 新增 `static void SetQuadVisible(ImageQuad, bool)`（GO 级显隐口，与 `Shell/CollectionWindow.cs` 同形） | 本仓口径（`ImageQuad` 无 `enabled`） |

## 颗数与点亮数的兜底口径（怎么出声的）

- 两个**显名常量**，注释写死「这不是原版常数」，并点名原版来源（`RankedDivisionV2.TotalSeals` 0x54 / `SealCount` 0x50，由 `ConfigManager.GetConfig<>()` 远端构造）。
- `BuildModeCard` 每建一次排位卡就 `Debug.Log` 一条（`:1887`）：「建了 4 颗 · 点亮 0 颗 …… 原版**来自远端** ⇒ **本地推不出来**；4 / 0 是**我们挑的兜底**（参考图那排 4 颗全灰 —— ⚠️ 那是**推断**，视频压暗也可能把金色压成灰）⇒ ⛔ 不是原版数据」。
- ✅ **没静默、没拿别的图顶替**：两张图各自判空，缺哪张就 `LogWarning` 点名是哪张、去哪个路径补，**只不画那一颗**；两张全缺 ⇒ 整排不建、直接 `return`。实测两张都在 ⇒ 这两条告警本轮不会响。

## 没做到的（为什么）

1. **`Global Rating` 那一支没做**：原版 `Rating` 下 seal 行与 `Global Rating`（`AllianceRatingDisplay`）**互斥二选一**，但那个条件的方法名反编译读不到（`FUN_1800021f0`）⇒ **判据只到这里**，按简报只做一支（「排位卡就显示这一排」），代码注释 + 本报告都如实标「**按实拍/推断、不是原版判据**」。⇒ **在「该显示 `Global Rating`」那种状态下会露馅**，请调度台排。
2. **`steps` 的 `sizeDelta` 与原版不一致（有意）**：原版 `sz(0×0)` + **全拉伸锚点**（真实 rect = 父件 494.61×86.51）；本文件 `New` 工厂**只写 `sizeDelta`、表达不了拉伸锚点**（`Shell/MenuDraw.cs:155` `SetPxSize` 把锚点写成 (0.5,0.5)）⇒ 我写**真实矩形**。将来拿原版字段对账会看到「我们 494.61×86.51 vs 原版 0×0」——**那是有意的**，注释已标。（`Rating` / `Mission Milestones Progress` 原版自己就是各自的 `sizeDelta`，没这个问题。）
3. **没做「播进场动画」**：原版模板带 `doEnterAnimation=0` / `animationTime 0.8` / `animationBetweenStepsDelay 0.3` / `animationStartDelay 0.5` / `animationScaleInitialMultiplier 3.0`，且是否播由 `MainMenuMatchResultData.HasToDoSealsAnim()`（远端/运行时）决定 ⇒ 本批**不做、也不假装做了**。

## 我建议加的断言（逐条含「改回什么 ⇒ 红」；我一条都没写）

> ⚠️ 加在 `Editor/MainMenuScene.cs`（我不许碰）。**期望值请写死**、⛔ 别引 `RankedSealFallback*` 那两个常量（同式自证）。

1. **结构三层在**：排位卡下 `FindChild "Rating"` → `"Mission Milestones Progress"` → `"steps"` 都在。🧨 删掉调用点 ⇒ 红。
2. **rect**：`Rating.rect`×108 = **494.61 / 109.52**；`steps.rect` = **494.61 / 86.51**。🧨 `MmpB−MmpT` 写成 `RatingB−RatingT`（109.52）⇒ 红。
3. **颗数 = 4**：`steps.childCount == 4`。🧨 改循环上界 ⇒ 红。
4. **节距 103.2**：step0 左缘 = 卡左 + **20.20** · step3 右缘 = 卡左 + **429.80**。🧨 `StepPitch` 写成 `StepW`（丢 3.2）、或第 0 颗改居中 ⇒ 红。
5. 🔴 **「该亮的那颗真的亮」（本批最关键）**：4 颗逐个断 `Empty.activeSelf == true && Fill.activeSelf == false`。🧨 `SealToggle` 两行**对调** ⇒ 红；`lit` 算反 ⇒ 红。🔴 **写法两个坑**：① 取件**必须带 `true`**（缺省那档不含 inactive，`Fill` 取不到 ⇒「`Fill == null` 就是对的」会**两种状态都绿**）；② 断的必须是 **`activeSelf`**（找得到 ≠ 显示得出来）。
6. **「灭自证」断言（结构上不可能同时满足的一条）**：每颗 `Empty.activeSelf != Fill.activeSelf`（**恰好一颗亮**）。🧨 挡第 5 条挡不住的改坏法：`SealToggle` 改成「两颗都设成 `lit`」或空实现 ⇒ 两颗一起亮 / 一起灭 ⇒ 红。
7. **图是那两张**：`Empty.Texture.name == "Rank_Skull_Empty"` · `Fill` 的 `== "Rank_Skull"`。🧨 两张图名对调 ⇒ 红。
8. **队列夹层**：每颗两张都满足 `QCardArt < RenderQueue < QOverlay`（现取 2920）。🧨 改回 `QCardArt`（被卡图盖）/ `QOverlay+1` ⇒ 红。
9. 🔴 **反面断言（防「顺手扩散」）**：`Skirmish` / `Draft` / `Tutorial` / `Practice` 四张卡的子树里**一律没有** `"Rating"` 节点。🧨 闸口从 `modeKind == "ranked"` 改成无条件 ⇒ 红（**正是简报点名不许做的**）。
10. ⛔ **不建议**加「`RankedSealFallbackCount == 4`」—— 期望值来自同一个常量 = 自证。

## 顺手发现（按红线**没有**顺手改）

1. 🔴 **`Global Rating` 那一支**（简报点名要报）：`Rating` 下另一半是 MB `AllianceRatingDisplay`（`Main Icon`/`Secondary Icon` sprite `40k_UI_icon_ranked_Skirmish` + `Position #`/`Position Value`/`Individual rating value`），与 seal 行**运行时互斥**；判据卡在读不到的方法名上 ⇒ **只做一支会在另一种状态下露馅**，请调度台排。
2. 🔴 **「字段表逐字相同、树里却没有」的又一实例**：`Skirmish Ranked Game Mode Container 1x2` 的 `RankedMenuContainer` **有** `milestonesStepDisplay` 这个引用，但**它的树里没挂这一排** ⇒ 将来**任何人「按字段建树」都会在 `Skirmish` 上多建一排**（= 真偏离）。本条按「**字段 ≠ 树**」处理；建议进 `项目任务.md` §三。
3. ⚠️ `New` 建的三个容器（`Rating` / `Mission Milestones Progress` / `steps`）`localPosition` **都是 0**、不是各自矩形中心 —— 本文件 `New` 的既有不变式（`MainMenuRuntime.cs:199` 明写「只写尺寸、故意不写位置」）⇒ 断言**只断 `rect` 宽高、⛔ 别断位置**（断位置会红，但那是断言写错、不是实现错）。
4. ⚠️ 这轮之后全工程有 **4 处**本排的 `Rank Skull`（`Fill`）+ **1 处** `Draft` 的 `Victory Counter Icon`（**没碰**）⇒ 按 sprite 名全局搜时要分清。
5. 🔵 印玺行卡内 `644.39~730.90` vs 暗带 `740.33~846.20` ⇒ **不相交**，两者同用 `QContent`(2920) 也**不会**有「谁盖谁」的不确定。⚠️ 这条**是我实算的**（`848.8−2.6−105.8657 = 740.33`，判据 = 本文件 `BuildModeCard` ② 那三个数），不是原版文档给的。

## 类型检查读数

```
TMPDIR=/tmp/wf_s bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

行尾：改后 `MainMenuRuntime.cs` = `crlf 0 / lf 1944`（改前 `crlf 0 / lf 1776`，**仍是纯 LF**）。
`git diff --numstat` = `352 added / 15 deleted` —— ⚠️ 那 **15 条删除全部是本次会话之前就存在的未提交改动**（first-fit 排布 / 视差那几处），**我这一轮只加不删**（逐条过了 `git diff | grep '^-'` 确认）。
