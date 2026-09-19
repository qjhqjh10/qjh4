# E 组 151 条 · 共享资产筛查 + `_EMISSION` 线索（2026-09-18）

> **状态：筛查结论已成立；「锅是谁」**仍然只是候选** —— 下面前三节是数出来的事实，
> 第四节是候选（**带一条已否证的普遍性**），第五、六节是要跑的实验。**别把第四节当结论**。

---

## 〇 · 一句话

E 组 151 条的**方向**（偏亮/偏暗）**高度聚在少数「被多个 prefab 共用的资产」上**
——偏亮侧 7 个资产覆盖 **66/84 = 79%**、偏暗侧 4 个资产覆盖 **53/67 = 79%**。
最强单点是 `Chestrays.mat`：87 个使用者里 **42 个落 E 组（E 率 48.3%，基线 15.8%）、其中 38 个偏暗**。

⇒ 不必普查 151 条：**先摁住这 11 个资产对应的族**。

---

## 一 · 方法：为什么换这个维度

前一轮按 **shader** 筛（`Sprites/Default` 等），三次落空；教训是真正的混淆维度是**效果家族**。
`shader` 和「`fam` 标签」都只是家族的**粗代理**。本轮的切块维度是
**「prefab 里引用了哪个具体资产（材质/贴图/网格的 guid）」** —— 一个材质被十几个 prefab 共用，
用户多半同源，**这是更细的家族切片**。

判据三条（缺一条就会误判，`Sprites/Mask` 就是范例）：
① **E 率** = 用它的效果里落 E 组的比例，基线 **151/957 = 15.8%**
② **方向** = 落 E 的那些里偏亮/偏暗各多少，基线 **84/151 = 55.6% 偏亮**
③ **族内对照** = 同一个族里「用它的」vs「不用的」比方向 —— 只看 ①② 会把家族标记当成锅

**工具**：`工具/screen_asset_impact.py`（`--min N` 收窄、`--family Buff_` 做族内对照、
`--build-index` 重建原版材质索引）。数据：`资料/特效还原台账.tsv` + `数据/游戏数据/effect_index.json`
+ `Assets/WarpforgeVFX/**.meta`。

**方法自证**（关键）：同一套筛查**原样重现了上一轮手工核过的三个否定结论** ——
`Sprites-Mask.mat` E 率 28.7% 但方向 **20亮/15暗（不偏）**、`Sprites/Default.mat` 同族同理、
`Glow Additive.mat` E 率 17.1% 且 **48亮/43暗 ≈ 基线**。
⇒ 尺子没换，换的只是自变量。

---

## 二 · 强信号资产（E 组内 ≥8 条；全表见工具输出）

| 资产 | E 内 | 全池用处 | 偏亮 | 偏暗 | 偏亮率 | 偏离基线 |
|---|---|---|---|---|---|---|
| `Smoke Sprite Sheet Extra Blend.mat` | 34 | 120 | 32 | 2 | **94.1%** | +38.5% |
| `Sphere.asset` ※ | 42 | 305 | 36 | 6 | 85.7% | +30.1% |
| `Smoke Sprite Sheet.png` | 58 | 278 | 49 | 9 | 84.5% | +28.9% |
| `Glow Additive Extra Color.mat` | 35 | 254 | 29 | 6 | 82.9% | +27.2% |
| `spark_blend.mat` | 22 | 67 | 20 | 2 | 90.9% | +35.3% |
| `flak1.png` | 24 | 101 | 22 | 2 | 91.7% | +36.1% |
| `RippleSubtle Distort.mat` | 28 | 205 | 25 | 3 | 89.3% | +33.7% |
| `LightningTrail.png` | 24 | 181 | 21 | 3 | 87.5% | +31.9% |
| **`Chestrays.mat`** | **42** | **87** | **4** | **38** | **9.5%** | **−46.1%** |
| `chestrays_mat.png` | 49 | 139 | 5 | 44 | 10.2% | −45.4% |
| `Glow Sphere 01.mat` | 38 | 224 | 16 | 22 | 42.1% | −13.5% |
| `SmokePuff01.png` | 14 | 57 | 4 | 10 | 28.6% | −27.0% |

※ `Sphere.asset` / `Quad.asset` 是**我们自己生成的网格**（`Assets/WarpforgeVFX/Meshes/`），
不是原版资产 ⇒ 它们只能是**家族标记**（Mesh 粒子那一族），**不可能是锅**。

**并集覆盖率**：偏亮侧 7 资产 → **66/84**；偏暗侧 4 资产 → **53/67**。

---

## 三 · 族内对照：Chestrays 不是纯粹的家族标记

E 组里 `Buff_*` 共 24 条。**只在族内**按含不含 `Chestrays.mat` 拆：

| 仅 `Buff_*` 族内 | 含 Chestrays | 不含 |
|---|---|---|
| 条数 | **0 亮 / 19 暗** | **4 亮 / 1 暗** |
| 亮度比中位 | 全挤在 **0.40–0.69** | — |

⇒ 同族内方向**翻转**，排掉了「它只是家族的另一个名字」这个解释。
`BulletImpact*` 族同理：25 条**全部偏亮 ×1.5–2.9、时段完全相同**（纯亮度，不是时序）。

---

## 四 · 候选：`_EMISSION` 与其「正解未实现」

> ✅ **本节已由 §十 的四组对照实验定案** —— 先读 §十，再回来看这里的代码位置与背景。

### 4.1 已建立的事实

- 材质字段**逐条与原版一致**（`Chestrays` `_Blend 0 / _SrcBlend 5 / _DstBlend 10`、
  `Glow Additive` `2 / 5 / 1`、`Ring_Warped_add` `2 / 5 / 1`、`Glow Sphere 01` `0 / 5 / 10` …）
  ⇒ **「导出串了属性」否掉**。
  ⚠️ **读这两个字段时别把枚举记反**（我本轮读反过一次）：
  `_DstBlend` 是 Unity 的 `BlendMode` 枚举 —— **`0=Zero · 1=One · 2=DstColor · 3=SrcColor ·
  4=OneMinusDstColor · 5=SrcAlpha · 6=OneMinusSrcColor · 10=OneMinusSrcAlpha`**。
  所以 **`5/1`（SrcAlpha/One）才是加法**（`Glow Additive` 正是 5/1，名副其实）；
  **`5/10`（SrcAlpha/OneMinusSrcAlpha）是普通 alpha 混合**（`Chestrays` 是 5/10）。
  `WarpforgeShaderMap.InferBlend`（`WarpforgeShaderMap.cs:161-177`）里的两组值**是对的**，别去"修"它。
- 原版材质用的 shader（`bundle_shaders_assets_all/Shader/Shader_8918850207803687531.json`）
  属性表 = `_BaseMap/_BaseColor/_Cutoff/_BumpMap/_EmissionColor/_EmissionMap/_SoftParticles*`
  ⇒ **就是 `Universal Render Pipeline/Particles/Unlit`**，**和我们 `.mat` 用的是同一个**。
- 该 shader 的 29 个关键字里**声明了 `_EMISSION`**，`_EmissionColor` 是它的正式 HDR 属性（默认黑）
  —— 见 `资料/普查产出_0917/shader属性表_块4.md:347`。⇒ **不是残留死值**。
- 贴图导入设置**全部一致**（`sRGBTexture 1` / `textureType 0` / `spriteMode 0` / `alphaIsTransparency 1`），
  含基线 `Glow.png` ⇒ 也不是贴图。
- **508 个能对上的材质里 355 个（70%）丢了原版的关键词**：`_SURFACE_TYPE_TRANSPARENT` 223 ·
  `_FADING_ON` 36 · `_SOFTPARTICLES_ON` 36 · `_EMISSION` 33 · `_COLORADDSUBDIFF_ON` 21 …

### 4.2 写入点

🔴 **`EffectExporter.cs:388`（`StripGlobalKeywords`）逐材质把 `_EMISSION` 从 keywords 里剔除**：

```csharp
if (d.keywords[i] != "_EMISSION") d.keywords[n++] = d.keywords[i];
```

它剔除的**理由是实测的**（`:382`）：「把 `_EMISSION` 从导出里剔掉，**12 个效果的中位逐像素 L1
从 0.11 掉到 0.0003**」⇒ 因为 `_EMISSION` 在 URP 里是**全局**关键字，烘成局部会让材质**无条件**发光。

### 4.3 🔴 正解写在**上一份（已被挤掉的）摘要**里，**没有实现**

`EffectExporter.cs:360-369` 还留着**另一份 `<summary>`**（被 `StripGlobalKeywords` 的注释挤成孤儿）：

> 「…结果就是导出的材质丢了 Emission，凡是靠 Emission 发光的效果（Back Glow 之类）整个变暗甚至看不见。
> **这里按 Emission 模块的状态把它写进材质定义，运行时 binder 会重新打开。**」

**全工程搜 `_EMISSION` 的运行时侧只有 `BuildArena1.cs:407`（那是战场重建，不是特效 binder）**
⇒ **「运行时按 Emission 模块状态重开」这件事没有落地**。这正好是「剔除」修好的那 12 条之外
**另一半**没被照顾到的地方。

### 4.4 ⚠️ 但它的**普遍性已被否证**（别再拿它当全局结论）

| E 组 151 条 | 偏亮 | 偏暗 | 偏亮率 |
|---|---|---|---|
| 丢 `_EMISSION`（79，含 Chestrays 42） | 25 | 54 | 31.6% |
| **其中 · 不含 Chestrays 的 37 条** | **21** | **16** | **56.8% ≈ 基线** |
| 没丢 `_EMISSION`（72） | 59 | 13 | 81.9% |
| 丢 `_EMISSION` **且** 原版 `_EmissionColor` 非黑（76，去掉 Chestrays 剩 34） | 22 | 54 | 28.9% |

⇒ **信号全部来自 Chestrays**。`_EMISSION` / `_SOFTPARTICLES_ON` / `_FADING_ON` 这三个
**不能当普遍原因**；它只是 `Chestrays.mat` 那条线的机理候选之一
（`Chestrays` 原版 `_EmissionColor = 0.5` **非黑**，三个关键词全丢）。
`_SOFTPARTICLES_ON` / `_FADING_ON` **丢掉可能是对的**（`WarpforgeEffectBinder.cs:164-177`：
`_SoftParticleFadeParams` 默认 0 会把整个发射器渲成黑，所以有一套「派生属性兜底」）。

---

## 五 · 尺子：**已实测，结论是「稳定」**（2026-09-18 晚）

`EffectSweepBatch.cs:206` 自己记过：「原版侧重渲同一个效果，数值会随它前面跑过什么而变
（`StrikeEffect` 单独跑 437 / 前面垫 Bore Intense 435 / 小批里排第 6 是 344，
而**导出侧 13/13 逐位相同**）⇒ 怀疑是**全局关键字跨效果泄漏**」。
探针 `WFSWEEP_GLOBALS=1`（`:211`）**已写好但默认不开** ⇒ 本轮把它跑了。

**实验**（16 个目标 · 三个批次：全量 958 那趟 / 小批 A（`StrikeEffect` 第 1 位）/
小批 B（`Bore Through Intense` 垫在前面））：

| 观察 | 结果 |
|---|---|
| **`_EMISSION` 是不是全局开着** | **16/16 目标的探针都显示 `_EMISSION` 已开**（进第一个目标前就开着）|
| 全局关键字总数 | 进第 1 个目标时 **59 个**，从第 2 个起 **132 个** ⇒ 确实有跨目标累积 |
| **绝对值**（`lit`）稳定性 | **15/16 逐位相同**；只有 `StrikeEffect` 漂（`494→517`，orig +4.6% / exp +4.5%）|
| **台账口径（`exp/orig` 比值中位）稳定性** | **16/16 几乎不动**：15 个 `|ln|` 差 **0.0000**，最差 `StrikeEffect` 差 **0.0005** |

⇒ **两条结论**：
1. **「尺子要修」这条可以降级**（原记在 `资料/特效还原_进度与交接.md:63-67`）：
   绝对值漂移是真的，但它是**共模的**（两边同向同幅），而台账用的是**比值** ⇒
   判定不受影响。`analyze_sweep.py` 的 `|ln|` 口径**经得起这个扰动**。
2. ⚠️ **但扫描态下 `_EMISSION` 是全局开的** ⇒ **扫描测的不是游戏里的样子**：
   游戏里我们的材质没开 `_EMISSION`（见 §4.2），而扫描时它是开的。
   ⇒ 这一条**会把「Emission 到底该不该开」这个差异从台账里抹掉**，
   即 §4 那条线索**不能靠 sweep 来验**，要用**单效果 A/B + 关掉全局关键字**（§六 实验 2）。

🔴 **跑批注意**：`EffectSweepBatch` 的输出路径写死是 `资料/比对基线/sweep_{orig,exp}.tsv`
+ `sweep_frames.tsv` —— **跑任何批次（哪怕十几个目标）都会整份覆盖这三个文件**，
且**目标清单文件不存在时 = 全量 958**（很长）。跑小批前先备份，跑完还原。
本轮的备份在 `_tmp_view/sweep_backup_0918_preE/`（带 `_md5.txt`）。

---

## 六 · 下一步实验（都便宜、都可回退；**要 Unity 串行**）

| # | 做什么 | 判据 | 代价 |
|---|---|---|---|
| ✅ **1** | 挑 16 个目标、**两趟都带 `WFSWEEP_GLOBALS=1`** 跑小批（A/B 两种顺序） | ✅ **已做（2026-09-18 晚）**：尺子稳定（比值 `|ln|` 差 ≤0.0005）；但 **`_EMISSION` 在扫描态下是全局开的** ⇒ 见 §五 | 已完成 |
| **2** | 在 `RenderAt` 前 `Shader.DisableKeyword("_EMISSION")` 并**按 `ParticleSystem.emission.enabled` 重设**，只扫 Chestrays 族 42 条 + 对照 | 偏暗那批的 `|ln|` 掉不掉。⚠️ **必须先把全局关键字按效果重置**，否则测不出差异（§五） | 改一处、可回退 |
| **3** | 正式修法：`WarpforgeEffectBinder` 重建材质时按 Emission 模块状态决定 `_EMISSION`（= 4.3 那份摘要写的正解），**保留** `EffectExporter` 的剔除（别回退它修好的 12 条） | 全量重扫后 E 组计数 + `|ln|` 中位 | 改代码 + 一趟 25 分钟 |

⚠️ 跑 sweep 前**先备份** `资料/比对基线/sweep_*.tsv`（`EffectSweepBatch.cs:193/198` 是
`File.WriteAllText`，整份覆盖）。

---

## 七 · 复跑办法

```bash
PYTHONIOENCODING=utf-8 python "D:/4/Unity/工具/screen_asset_impact.py" --build-index      # 重建原版材质索引
PYTHONIOENCODING=utf-8 python "D:/4/Unity/工具/screen_asset_impact.py" --min 8             # 全量筛
PYTHONIOENCODING=utf-8 python "D:/4/Unity/工具/screen_asset_impact.py" --family Buff_      # 族内对照

# 重扫（两趟顺序不能反、分进程；orig 那趟带 WFSWEEP_GLOBALS=1 开探针）
WFSWEEP_SIDE=orig "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod EffectSweepBatch.Run -logFile "d:/4/_tmp_view/sweep_o.log"
"$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod EffectSweepBatch.Run -logFile "d:/4/_tmp_view/sweep_e.log"
"D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/analyze_sweep.py"
```

台账（唯一数字出处）：`资料/特效还原台账.tsv`（957 行 / E151 · Z580 · W194 · D29 · C3）。

---

## 八 · 与既有结论的关系

- **不复用**前一轮否掉的三个（`Particle Dissolve Mask` / `Sprites|Mask` / `Matcap Full Options`）
  —— 本轮第三条判据把它们**又独立重现了一次**（方向不偏）。
- **不冲突**锅 H₁（`_EmissionColor` 那条）：那是**自建** `WFParticlesExtraColor.shader` 里的加法项；
  本节讲的是**导入材质的关键词**，两回事。
- 本轮把「E 组 = 一两个锅」这个假设**改写成**：**偏亮、偏暗各有一撮共享资产**，
  先各自摁掉最大的一撮（`Chestrays.mat` 一族 42 条 / `spark_blend`+`Smoke Sprite Sheet` 一族约 50 条）。

---

## 九 · 附：**甲路**（已不采用）的预切块，留着备查

丙路取代了甲路，但甲路已经把块切好了，**别重复劳动**：

- **新账切块**：`d:/4/_tmp_view/E0918_new/B*.tsv`（21:04，合计 **151**）——
  B1 纯精灵图 **83** · B2 Matcap **21** · B3 抓屏扭曲 **19**（不含 Matcap）· B4 拖尾 **4** ·
  B5 Mesh 粒子 **23** · B46 杂项 **1**。
  每行第 6 列「技术构成」形如 `Mesh粒子 精灵图 多发射器(21) Matcap/抓屏扭曲`
  —— **已带族标签 + 发射器数**，缺的仍只是第三维「可见主导槽 shader」。
- ⚠️ **没有生成脚本**：`D:/4/Unity` 与 `D:/4/_tmp_view` 全树搜 `E0918_new` **命中 0**、
  `Unity/工具/` 搜 `E0918` 也是 0 ⇒ 那次切块是**内联跑的**，要补列/重切得自己写脚本。
- ⚠️ **旧账 `_tmp_view/E0918/B*.tsv`（20:18）的「技术构成」写法是「多发射器(10)」，
  新账台账是「10发射器」** —— 同一个东西的两种写法，**别当成两个口径**。

---

## 十 · `_EMISSION` 四组对照实验（2026-09-18 晚，本轮的定案手段）

**背景**：§四 那条线索的两个方向都只是推断，本轮把它做成了**四组对照**。
所有「比」= `exp/orig` 在**原版有内容时段**上的中位（与台账 `analyze_sweep.py:178` 同口径）。

### 10.1 四组条件

| 组 | 条件 | 怎么跑 |
|---|---|---|
| **X** | 基线（`_EMISSION` 被导出器剔掉） | 不设环境变量 |
| **Y** | binder 里**无条件**开 `_EMISSION` | `WFBIND_EMISSION=1` |
| **Z** | binder 里**按粒子系统 Emission 模块**逐渲染器开 | 新默认（`WarpforgeEffectBinder.Apply`） |
| **N** | **原版侧**渲染前强制关掉 bundle 材质的 `_EMISSION` | `WFORIG_NOEMIT=1` |

Z 与 Y 都改在 `WarpforgeEffectBinder`；N 改在 `EffectSweepBatch.RenderAt`（都在本轮加了开关）。

### 10.2 结果（13 个目标）

| 效果 | X 基线 | Y 无条件 | Z 按模块 | **N 原版关掉后，原版自己的数变不变** |
|---|---|---|---|---|
| `Buff_Red` | 0.484 | **1.0000** | 1.0000 | 914 → **434** ⇒ **原版在用** |
| `Buff_Green` | 0.624 | **1.0000** | 1.0000 | 967 → 605 ⇒ 在用 |
| `KhaineBuff` | 0.498 | **1.0000** | 1.0000 | 1045 → 515 ⇒ 在用 |
| `Buff_DiabolicStrength` | 0.399 | **1.0000** | 1.0000 | 935 → 361 ⇒ 在用 |
| `Basic_army_DA` | 0.401 | 1.0253 | 1.0253 | 260 → 102 ⇒ 在用 |
| `DeckBuff_Green` | 0.416 | 1.0195 | 1.0195 | 5295 → 1987 ⇒ 在用 |
| `BulletImpact_DeathSpinner Test` | 0.930 | 1.0009 | 1.0036 | 914 → 184 ⇒ 在用 |
| ⚠️ `BlindEffect` | 1.176 | **1.739（变坏）** | 1.739 | 885 → **884** ⇒ **原版几乎没在用** |
| `Air to Ground Big` 等 5 个 | 1.01–1.42 | 不变 | 不变 | 不变 ⇒ 没在用 |

### 10.3 三条结论

1. **✅ `_EMISSION` 就是偏暗那半边的根因，而且是精确的**：开回来之后
   **五个效果正好落在 `1.0000`**（不是「接近」，是**同一位小数完全相等**）⇒ 差的就是这一项，没有别的。
2. **⚠️ 但「无条件开」是错的**：`BlindEffect` 1.176 → 1.739。而 N 组显示**原版对它几乎没在用 emission**
   ⇒ 判据必须能区分「原版用 / 不用」。
3. **❌ 「按 Emission 模块判」在这批上不具区分力**：Z ≡ Y（**13/13 完全相同**，含仍变坏的 `BlindEffect`）
   ⇒ `ParticleSystem.emission.enabled` **不是**原版那个判据（对这些渲染器它都是 true）。
   ⇒ **别再拿它当判据**（这正是那份孤儿摘要里写的那句，实测不成立）。

### 10.4 由此得到的判据：**逐效果量「原版到底用没用」**

唯一可靠的判据是 **N 组本身** —— 拿「原版基线」与「原版·关掉 `_EMISSION`」两份数据逐效果比：
**数变了 = 原版在用**。这就是 ground truth，不需要任何推断。

📌 **全量 N 趟已跑**（`WFORIG_NOEMIT=1`，输出 `_tmp_view/E6_orig_noemit_full.tsv`），
与原版基线逐效果比即可得到**全池 957 个效果的「原版用没用 emission」名单**。
落地方式：把它做成**逐材质**的标记，在导出侧给 `WFMatDef` 补一个字段（而不是靠运行时猜）。

### 10.5 复现命令

```bash
# Y（无条件开）
WFBIND_EMISSION=1 "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod EffectSweepBatch.Run -logFile "d:/4/_tmp_view/E3_Y_exp.log"
# N（原版侧强制关）
WFSWEEP_SIDE=orig WFORIG_NOEMIT=1 "$UNITY" ... -executeMethod EffectSweepBatch.Run -logFile "d:/4/_tmp_view/E6_orig_noemit_full.log"
```
⚠️ 每次都会整份覆盖 `资料/比对基线/sweep_{orig,exp}.tsv` + `sweep_frames.tsv` ⇒ 先备份、跑完还原。

---

## 十一 · 偏亮侧：**自建 shader 里一行凭空来的加法**（2026-09-18 已删）

### 11.1 根因

`WarpforgeVFX/Particles/Extra Color`（自建，替换原版 `Everguild/FX/Extra Color`）里那行：

```hlsl
col.rgb += tex.rgb * _EmissionColor.rgb * IN.color.rgb;
```

三条一起才成立：

1. **原版 `Everguild/FX/Extra Color` 的属性表里根本没有 `_EmissionColor`**
   （21 项逐字见 `资料/普查产出_0917/shader属性表_块1.md:278`）⇒ **原版从不做这项加法**。
   `CLAUDE.md` 三那条规矩正是「**属性表里没有的，一律硬编码**」。
2. 它的**默认值是白**（`[HDR] _EmissionColor(..., Color) = (1,1,1,1)`），而 def 里没记这个属性的材质
   会**吃默认白** ⇒ frag 这行**多加一整份贴图（≈2×）**。shader 自己的旧注释就量到过
   「B1a 块 50/50 份 def 都是 `(1,1,1,1)`（=默认值、不是原版值）」—— 当时没把「默认值写错」这一步点破。
3. **§十 的 ground truth 与它完全吻合**：E 组偏亮那 84 条里 **74 条**是「原版根本没用 emission」。

### 11.2 实测（删掉这一行，其它一切不动）

| 效果 | 改前比 | 改后比 |
|---|---|---|
| `DeckBuff_Orks` | 4.46 | **1.30** |
| `Buff_Tau_Kroot_FriendlyBoard` | 5.08 | **4.78** |
| 其余 **13 个**（含 4 个「对得上」的对照） | — | **完全不动** |

⇒ **变好 2 · 变坏 0 · 没动 13**。改动是安全的；但偏亮侧**不止这一个原因**
（`Vortex Explosion Massive` 29.0 · `FrenziedEffect` 3.48 纹丝不动 —— 它们不用这个 shader）。

### 11.3 与「09-13 那 11 条偏暗」的关系

shader 旧注释写「当初加它是为了修 11 条偏暗」—— **那批的真实原因是另一个机制**
（URP `Particles/Unlit` 的 `_EMISSION` 关键字被导出器剔掉，见 §十），与这行加法无关
⇒ 删掉它不会把那批打回去（§十 的 N 组数据已经证明那批要的是关键字，不是加法）。

---

## 十二 · 落地形态（2026-09-18）

| 改哪 | 改成什么 |
|---|---|
| `WFParticlesExtraColor.shader` | **删掉** `+= _EmissionColor` 那行（§十一） |
| `WarpforgeEffectBinder` | 新增**逐 prefab** 的 `public bool emissionOn;`（**不是逐材质、也不是按 Emission 模块** —— 三种推断判据全被实测推翻，见字段注释）；`Apply()` 按它决定 `_EMISSION` |
| `EffectExporter` | 导出时按效果名查表写进 `binder.emissionOn` |
| **数据** | `数据/游戏数据/emission_flag.json`（957 条，由 `工具/gen_emission_flag.py` 从两趟 sweep 生成） |

⚠️ **这张表是数据、不是活的推断** —— 重导特效之前若它比 sweep 旧，**先重跑生成器**。
（生成器用法：`python 工具/gen_emission_flag.py <原版基线.tsv> <原版·关EMISSION.tsv> --write`）

**为什么粒度是「逐效果」**：N 实验量到的本来就是「**这个效果的画面**有多依赖 emission」
（`Material.DisableKeyword` 一关，整份实例都受影响）⇒ 逐效果既是实测的粒度、又零归并损耗。
试过归并到逐材质：阈值 0.7 时逐效果准确率只有 90.8%，会漏 48 条、误 40 条。

---

## 十三 · 第二轮：两个条件取交集 + 阈值提到 15%（**E 组 151 → 62**）

### 13.1 第一轮（只用逐效果标记）的教训

第一版只按 `emissionOn`（阈值 5%，211 条）开：**修好 88 条，却把 48 条原本「对得上」的打成偏亮**
（检查那 48 条：**47 条都是「效果的 `emissionOn` 为真、但材质本身没这个关键字」**）⇒ E 组只降到 112。

### 13.2 最终判据（两个条件取交集）

| 条件 | 含义 | 从哪来 |
|---|---|---|
| `binder.emissionOn` | **逐效果**：这个效果的画面在原版里响不响应 emission | N 实验实测（阈值 **15%**） |
| `WFMatDef.hadEmissionKeyword` | **逐材质**：原版这个材质自己带不带 `_EMISSION` | 导出时从 bundle 材质的 `m_ValidKeywords` 读（`EffectExporter.StripGlobalKeywords` 里顺手记下） |

`WarpforgeEffectBinder.Build`：两者**同时成立**才开。

### 13.3 结果（第二趟全量重扫）

| 判定 | 旧 | 新 | 变化 |
|---|---|---|---|
| **对得上** | 580 | **670** | **+90** |
| 偏亮 | 84 | 53 | −31 |
| **偏暗** | 67 | **9** | **−58** |
| 其余（W/D/C） | 226 | 225 | −1 |
| **E 组合计** | **151** | **62** | **−89（−59%）** |

**修好 91 条 · 新进只有 2 条**（对比第一版的「修好 88 / 新进 49」）。
全池 E 率从 15.8% 降到 **6.5%**。

⚠️ **`资料/比对基线/sweep_{orig,exp}.tsv` + `特效还原台账.tsv` 已按这一版重生**
（导出日志 `_tmp_view/export_0918c.log` 为 958/958 成功 0 失败）。

### 13.4 剩下 62 条：**换了一批资产、换了一个机制**

E 率基线降到 6.5% 之后再筛，剩下的**不再是 Chestrays 族**，而是
**走我们自己写的两个 shader 的那一族**（全部 **100% 偏亮**）：

| 资产 | E 内 | 指向的 shader |
|---|---|---|
| `RippleSubtle Distort.mat`（+ `.png`） | 18 | **`WFDistortion`**（自建，替换 `Everguild/FX/Particle Distortion Affect Transparents`）|
| `spark_blend.mat` | 12 | `WFParticlesExtraColor` |
| `flak1.png` · `LightningTrail.png` · `Smoke Sprite Sheet Extra Blend.mat` · `ExplosionFlames Add.mat` | 9–14 各 | `WFParticlesExtraColor` |
| ~~`BulletHoleMetalThrough.mat`~~ | — | 🔴 **2026-09-19 更正：它不用我们的 shader** —— 工程里那张 `.mat` 的 `m_Shader` guid 解析出 URP 包内 `ParticlesUnlit.shader`，与原版材质的 pathID 解析（`Universal Render Pipeline/Particles/Unlit`）**两条独立证据互证**。它挂在 `_EmissionColor=(0,10.69,19.93)`（原版残留死值）那条**风险闸门**上，见 `E组_自建shader_WFParticlesExtraColor_逐属性.md` §2·③ |
| `Sphere.asset` | 18 | 家族标记（我们生成的网格）|

⇒ **下一件 = 拿同样的手法（A/B 干预 + 直接量）去查 `WFDistortion` 与 `WFParticlesExtraColor`
还比原版多做了什么**。它们是**自建 shader**，原版那张属性表就是尺子 ——
已经确定原版 `Everguild/FX/Extra Color` 的 21 项里没有 `_EmissionColor`（§十一），
**同一条规矩要把这两个 shader 的每个属性都过一遍**。

剩下 9 条偏暗里，6 条的 N 响应 <5%（与 emission 无关），
还有 `CardPrefab`(0.04) / `Invoke Minion Hits Ground`(0.23) / `BlastEffect`(0.33) 三条大偏暗没动。

## 十四 · 第三轮（2026-09-19）：两个自建 shader 逐属性过筛 + 落地

**做法**：拿 §十一 那套手法（逐属性对原版属性表 + 原版 DXBC 资源名），把
`WFDistortion` 与 `WFParticlesExtraColor` **各写一份逐属性对照**（只读子代理产出，两份都在本目录）：

- `E组_自建shader_WFDistortion_逐属性.md`
- `E组_自建shader_WFParticlesExtraColor_逐属性.md`

### 14.1 已落地（2026-09-19）

| 改动 | 判据 | 影响面 |
|---|---|---|
| **`WFParticlesExtraColor` 补 `#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON`** | 原版关键字表里有它；而我们的 shader **从没声明过** ⇒ 段 `col.rgb *= col.a` **是死代码**，同时那 9 个 `_SrcBlend=One + _DstBlend=OneMinusSrcAlpha` 材质**照样写着预乘混合** ⇒ 按未预乘输出（偏亮，alpha 越小倍数越大） | 316 个材质里的 **9 个**（`Explosion_Color`/`FireRed`/`FireBlack`/`Flames Loop *`/`Waterfall_ExtraColor`/`Explosion_big_ground`/`Stealth_Icon`/`Default-Particle`） |
| **`EffectExporter.SetBlend` 按混合状态开关这个关键字** | 判据只看 `(src,dst) == (One, OneMinusSrcAlpha)`，**不看 shader 名** —— 原版 `URP/Particles/Unlit` 自己也是这么开的 | 同上（导出时写进材质定义，运行时 binder 生效） |
| **导出器补 `textureSheetAnimation` 的 sprite 列表** | 与 `SpriteRenderer`/`SpriteMask` 同一个病（bundle 资产不导 ⇒ guid 全 0 伪引用）。实测导出后 **383 个** tsa 列表由空变实（与原版 VFX 包带非空列表的 383 个 PS 吻合），新增 106 张 `*_sprite.png` | 143 PS / 99 效果（另见 `孤儿待办_五条查证.md` §一） |

⚠️ **`WFDistortion` 这一次没动** —— 它的候选都是「推的」（原版 DXBC 没有 RDEF ⇒ 公式读不到），
要按 §五 那条方法论**先做单变量 A/B**再改，别拍脑袋：

1. **`_DistortTex` 取的是 `.rg` 而原版是 `Normal` 型贴图**（DX11/DXT5nm 下 X 在 `.a`、Y 在 `.g`）——
   若真，`offset.x` 恒为 `+_DistortionStrength`（**只往一个方向偏**）而不是零均值噪声场。
   最便宜的 A/B：把 `RippleSubtle Distort.mat` 的 `_DistortionStrength` 由 0.1 改 **0**（只影响偏移、不影响 alpha），
   复扫看那 18 条动不动。
   ✅ **2026-09-19 已做这个 A/B（小批，18 条）—— 结论：不是主因。**
   做法：备份 `sweep_{orig,exp,frames}.tsv`（存 md5）→ `_DistortionStrength` 改 0 → 写
   `_tmp_view/sweep_targets.txt`（18 个效果名，`EffectSweepBatch` 会读它）→ 跑 `orig`/`exp` 两趟 →
   按 `analyze_sweep.py` 同款口径（逐时刻 `exp_sum/orig_sum` 取中位）比。
   **结果：18 条里中位变化 −0.011**（4 条基本不动、`Screamer-Killer`/`Tau_Missile`/`Tap *`/`Vortex` 都只差 ≤0.03）；
   只有 `BulletImpact_Sonic Weapon` 那 5 条明显降（−0.13 ~ −0.47，仍在 1.4–2.2），另两条 `Spore Launch` **反升 +0.6**（更像尺子噪声）。
   ⇒ **偏移那一项确实参与了一部分（Sonic 族），但不是这一族 1.4–2.5× 的主因**；
   **`.mat` 与三个基线文件都已还原（md5 逐个对上）**，实验脚本 `_tmp_view/ab_distortion.py`。
   ⚠️ 这就是「**先量再改**」的价值：这条候选是上一轮排第一的「推的」，实测直接否掉了。
2. **软粒子淡出公式**：我们写 `saturate((sceneEye-partEye)/_Depth_And_Fallof.y)`，材质值 `(16.31,0.39)`
   ⇒ 我们几乎不淡出；若原版是 `pow(saturate(diff/.x), .y)`，量级差 1.6–3.0×（与观测 ×1.5–2.9 吻合）。
3. **`_USEMASK`/`_SOFTPARTICLES` 只认关键字、不认 float**：6/11 份 Distort 材质 `float=1` 而关键字空
   ⇒ 我们整段跳过「乘 mask / 乘 fade」（**只会偏亮**）。原版 CB 里这两个是**活值**。
4. **`_Cutoff`/`_EmissionColor`/多出来的 `_ST`** 等「我们多出来的属性」：当前**没人读**或材质值恒等于默认 ⇒ 无影响，但**属性表里留着就是隐患**（`_EmissionColor` 还被 `WarpforgeEffectBinder` 当 `_EMISSION` 的开关键用）。
5. **四参数 `Blend`（α 通道分离）**：原版有、我们没有；**RGB 逐位相同** ⇒ 与亮度无关，暂不动。

### 14.2 第三轮之后的账（**2026-09-19 全量重扫，两趟分进程**）· 第四轮见 §14.2e

| 判定 | 第二轮 | 第三轮 | **第四轮**（09-19 晚，`renderQueue` 修完） |
|---|---|---|---|
| **对得上 Z** | 670 | 687 | **693** |
| **亮度/密度不对 E** | 62 | 46 | **41** |
| 只有导出有 D | 28 | 28 | 28 |
| 两边全程空 W | 195 | 195 | 195 |
| **只有原版有 C** | 2 | 1 | — |

**全池 E 率 6.5% → 4.8%**；E 内 **偏亮 38 / 偏暗 8**；`|ln|` 中位：全部 0.023 · Z 0.019 · E 0.514。
⚠️ **尺子边界提醒**（`analyze_sweep.py` 自己打的）：带宽挪 0.05 就能让 Z 摆 22–54 个 ⇒
**+17 这个量级要跟它比着看**（不算大，但方向与「补了预乘 + 补了 tsa 精灵」一致）。

**这一轮改了什么、能不能对上**（按资产筛 `工具/screen_asset_impact.py`）：
- ✅ **那 9 个预乘材质的效果已经不在 E 里**（`Explosion_Color` / `FireRed` / `FireBlack` / `Flames Loop *` / `Explosion_big_ground` / `Stealth_Icon` …）——
  与「补 `_ALPHAPREMULTIPLY_ON`」自洽（⚠️ **只能说自洽**：没有第三轮之前的逐资产名单可比，所以这不是因果证明）。
- 🔴 **`RippleSubtle Distort.mat` 那 18 条一条没动**（仍 94.4% 偏亮）⇒ §14.1 那些 `WFDistortion` 候选**还没动过**，正是下一轮要 A/B 的。
- 🔴 现在 E 组前几名全是**加色发光族**：`ring_warp.png` 22 · `glowsphere01.png` 16 · `Ring_Warped_extra color.mat` 16 ·
  `flak1.png` 14 · `Glow Sphere 01.mat` 13 · `Glow Additive Extra Color.mat` 12 · `LightningTrail.png` 12 · `spark_blend.mat` 12
  —— **全部 100% 偏亮**。⇒ 下一轮的主战场从「Distort」换成了「**加色/发光贴图那一族**」。

### 14.2b ⚠️ E 组**头部**有「分母近零」的假信号（2026-09-19 直接看图发现）

`Vortex Explosion Massive` 挂在 E 组第一名（**29.54×**）。用 `EffectCompare` 渲了原版/我们两张并排图
（`_tmp_view/cmp/Vortex Explosion Massive__{orig,exp}.png`）：

- **原版那一帧几乎是全黑**（一个像素点的残迹）；**我们这一帧也几乎是黑**（一块很暗的扭曲方片）。
- ⇒ 29.54 这个比值是 **「我们 29 × 近零」** 出来的，**不是「我们画得比原版亮 29 倍」**。
  同族的 `Tap Plasma generator`（2.04×）并排看是「几颗光点，我们的略大略软」——差异真实但很小。

**⇒ 下一轮读台账时要先看绝对值**：`亮度比中位` 大的条目里，凡**两侧的总亮度都接近 0** 的，
先怀疑**采样时刻/寿命/取景**（W/D 那一类），别当成亮度锅去调 shader。
`analyze_sweep.py` 的带宽判定对「双方都空」有 W 分组，但**「一方近零」落在哪**取决于阈值 —— 这是它的边界质量。

### 14.2c 🔴 把 **46 条 E 的绝对值**排一遍：**头部几条根本不是亮度锅**（2026-09-19）

口径：`sweep_{orig,exp}.tsv` 的 `sum` 列取**逐效果中位**（8 个时刻）——「原版有多少、我们有多少」。
判据：**两侧都接近 0** ⇒ 比值（哪怕 29×）**没有意义**。

| 效果 | 原版 sum 中位 | 我们 sum 中位 | 说明 |
|---|---|---|---|
| `EnvironmentalCondition Ultramarines Thunders` | **0** | **0** | 两边都空 ⇒ 判定无效（该进 W 那一类） |
| `Tap Webway Portal` | **0** | **0** | 同上（A/B 里比值 1.59 也纹丝不动，正是这原因） |
| `Vortex Explosion Massive` | **0** | 33 | 直方图那种「29×」= **近零分母**（并排图两张都近黑） |
| `Psychic_Lightning_down_Waaagh_Green` | **0** | 175 | 同上（原版那一侧采不到） |
| …（中间 30 条：两侧都只有几十~几百，比值可信但噪声大） | | | |
| **`CardPrefab`** | **30504** | **1162** | 🔴 **这一条是真的**：**我们比原版暗 26 倍**，绝对值也大 ⇒ **优先级应该排第一** |

**⇒ 下一轮动手的顺序建议**（按「绝对值 × 比值」而不是只按比值）：
① `CardPrefab`（真·大偏暗）· ② `Invoke Minion Hits Ground` / `Invoke Minion Legendary ALT` / `BlastEffect` / `StunEffect_proc`
（**我们比原版暗**的那几条）· ③ 大幅偏亮且**两侧都有量**的（`Buff_Tau_Kroot_FriendlyBoard` 92→502 ·
`Hammer_Slam_SW` 68→156 · `AcidSpraySweepAttack` 51→102 这一族）。
⚠️ 头部那几条近零的**别去调 shader**，要查的是**取景/时刻/寿命**（这一类归 W/D）。

### 14.2d ✅ `CardPrefab` 结案（2026-09-19 晚）—— **它不是特效，是原版的「卡牌 prefab」；「暗 26 倍」是对比口径造成的假信号**

§14.2c 把这一条排成「唯一真锅、优先级第一」。**2026-09-19 晚直接查证 ⇒ 它根本不该在 E 组里**：

1. **它那 10 个组件全是卡牌控制器**（`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/CardPrefab.json`
   逐个 PathID 找到类型）：`RawCardScript`（`cardImage`/`ExtraTraits`/`cardCollider2D`/`timeToLand`…）·
   `Card2DController`（`cardImageController`/`cardTextsController`/`card2DController`/`cardRarityHolderController`…）·
   **5 个同字段的数值跳动控制器**（`textsToSet`/`cardTextBumpSize`/`cardTextBumpTime`/`textColorsSO`/`originalScales`…）·
   **`CardHighlight`**（`CardHighlightAnimTime`/`CardBodyToScale`/`ScaleFactor`/`FrameHighlight`…）
   —— **一个特效控制器都没有**。
2. **子树里有 2 个 `RectTransform`（UI）** ⇒ **卡面是 UI、运行时灌图**（同 `3DBody` 的 `_CardImage` 那套路子），
   我们的导出器**不导 UI** ⇒ **我们那侧必然缺卡面**。
3. **我们代码对它零引用**（`grep -rn "CardPrefab" MyGame/Assets --include=*.cs` **无命中**），
   也不在任何 `.json` 索引里 ⇒ 它是个**死的导出物**。
4. **并排图直接印证**（`_tmp_view/cmp/CardPrefab__{orig,exp}.png`，`EffectCompare` 小批跑）：
   原版那一帧是**一整张卡**（卡框 · `-5`/`11`/`12`/`30` 数值 · 卡名 `Sanguine Vanguard` · 效果文字），
   我们那一帧**只有粒子**（雪花、红溅、三个圆点）—— **粒子部分两边都在**，差的正是**卡面那一层**。

⇒ **「原版 30504 → 我们 1162」= 原版那侧多渲了一整张卡面**，**不是我们画暗了**。
**处理：从 E 组排除、不再作为待办** —— E 的**有效**条数 **46 → 45**
（⚠️ 这是**人工排除**：台账由 `工具/analyze_sweep.py` 生成，重跑它这个数会**自己变回去**）。

⚠️ **由此得一条判据（下一轮读台账要带上）**：凡「技术构成」含 **`Sprite/UI/文字`**、
或原版 GameObject 带 **`RectTransform`** 的，**先查它是不是卡牌/UI prefab 被误收** ——
`EffectExporter` / `EffectCompare` 的收录口径是「**子树里有 `ParticleSystemRenderer`**」，
**卡牌 prefab 会因为自带装饰粒子被整棵收进来**，对比时原版那侧就多出一整张卡面。

### 14.2e 🔴 **E 组第一条真锅定案（2026-09-19 晚）：`renderQueue` 被记成了不透明队列** —— 只挂在「走自建/bundle shader」的材质上

`Invoke Minion Hits Ground`（台账 **0.23**，全组最暗）的根因**不是** shader 算式、**不是**贴图、**不是** `_EMISSION`，
而是**渲染队列**：

| 渲染器 | 材质（shader） | 原版 queue | 我们 queue |
|---|---|---|---|
| `Dust` / `Embers` / `SandParticle` | `Dust` / `Embers 1` / `SandParticle`（走 `URP/Particles/Unlit`，**工程自带**） | 3000 | 3000 ✅ |
| `FX_EarthSkill_Hit_floor_01` · `crack` · `crack/crack Additive` · `particle_splash` | `Mat_Fx_ParticleSet_apb`（`Shader Graphs/Fx_ParticleDissolve_apb`，**只在 bundle 里**） | **3000** | **2000** ❌ |
| `rock` | `Mat_Fx_Rock`（`Shader Graphs/Fx_RockDissolve`，同上） | **2450** | **2000** ❌ |

**机理**：这两个材质的 `m_CustomRenderQueue` **都是 −1**（原版没有 override）⇒ 队列本该由 **shader 自己**决定；
而 `Material.renderQueue` 在「无 override」时返回的**正是 shader 的默认队列** —— 但
**只在 bundle 里的 shader 在编辑器环境下解析不出队列** ⇒ 返回兜底的 **2000 = 不透明队列**
⇒ `EffectExporter` 把它当真值记进 `WFMatDef.renderQueue` ⇒ binder 忠实设回 2000
⇒ **粒子被当不透明排**（渲染顺序错 · 看起来暗）。

**A/B（单变量：只手改一个 prefab 的那两个值 2000 → −1）**：
`Invoke Minion Hits Ground` 的并排图从「**几乎看不见的暗云**」变回「**与原版一致的亮云**」——
`_tmp_view/cmp/Invoke Minion Hits Ground__exp{,_before}.png`（11612 → 15115 字节）。

**修法（已落 `EffectExporter.cs`，⚠️ 两版，第一版是错的）**：

- ❌ **第一版（错，重导一遍才发现）**：`AssetDatabase.Contains(om.shader)` 判「shader 是不是工程资产」。
  **bundle 实例的 `Contains` 恒为 `true`** ⇒ 条件永远成立、**等于没改**（`IsForeignAsset` /
  `IsMainAsset` / `GetAssetPath` 同样判不出来 —— bundle 实例的 `AssetPath` 也是空串）。
  **实测工具 = `RQProbe.cs`**（走导出器同一条取数路径：bundle → GameObject → renderer → sharedMaterial；
  ⚠️ `LoadAllAssets<Material>()` 拿不到材质，它靠跨包依赖解析）。**教训：判据要先量，别猜。**
- ✅ **第二版（对）**：**判「材质到底有没有 override」**——
  `renderQueue = (om.shader != null && om.renderQueue != om.shader.renderQueue) ? om.renderQueue : -1`
  · **不等** ⇒ 材质自己 override 过（如 `Glow Additive` 3000 vs shader 2000）⇒ 那个值可信，照记
  · **相等** ⇒ 读到的**就是** `shader.renderQueue`，而 bundle shader 在编辑器里解析不出队列
    ⇒ 记 **−1**（运行时用重建后那个 shader 的默认队列；白名单走原件 ⇒ 正好对）
  · edge case：材质恰好 override 成与 shader 同值 ⇒ 记 −1 ⇒ 运行时用 shader 默认 **＝同一个值** ⇒ 无误伤
  · **单条验证过了**（`Resume=true` + `NameFilter` 单条 + 先把报告里那行删掉）：
    `Mat_Fx_ParticleSet_apb` / `Mat_Fx_Rock` 记成 **−1** ✓，走工程自带 shader 的三个**保持 3000** ✓

⏭ **还欠「全量重导」才能生效到 958 条**（prefab 是**烘出来**的，改导出器不重导 ⇒ 老值还在）。
⚠️ **重导的安全前提**：`EffectExporter.Run` 在 `Resume=false` 时会先跑 **`ClearGenerated()` ——
它删掉 `Prefabs/`（还有 Mat/Tex/Mesh）下的全部资产** ⇒ **跑小批验证必须 `Resume=true`**
**并且先把报告里那几行删掉**（否则 `IsDone()` 当「已完成」跳过），否则 957 个 prefab 会被清掉只剩你过滤的那几条。

**重导后怎么算修好**：① 抽 `Invoke Minion Hits Ground` / `BlastEffect` 用 `EffectCompare` 并排看
（配 `WFCMP_SIMT` 取台账时段中点）② `WFCMP_MATDUMP=1` + `工具/diff_matdump.py` 里那条
`__queue` 差异应消失 ③ 全量 sweep 看「偏暗那侧」条数。

#### ✅ 2026-09-19 全量重导后的实测结果（958 条零失败）

| 效果 | 重导前 | 重导后 |
|---|---|---|
| `Invoke Minion Hits Ground`（0.23，全组最暗） | 5 处（**全是 `__queue`**） | **0 处** ✓ · 并排图 = 明亮的云，与原版一致 |
| `Invoke Minion Hits Ground Legendary` | 31 处 | 26 处（`__queue` 那 5 处没了） |
| `BlastEffect` | 0 处 | **2 处（新出现）** ← 见下面的局限 |

⚠️ **第二版判据的已知局限（实测确认）**：「**材质 override 成与 shader 同值**」这种情况判不出来 ——
因为 `shader.renderQueue` 在编辑器里读到的**本来就是那个假值（2000）**。
实例：`BlastEffect` 的 `CrackedGround/Rock`（原版 **2000**）—— 那是「材质 override 成 2000」，
而 `Fx_RockDissolve` 的**真实**默认是 **2450**；我们两边都读成 2000 ⇒ 判「无 override」⇒ 记 −1
⇒ 运行时变 2450 ⇒ 这一位从对变错。
**净账是正的**（修好了全组最暗那条、5 处 → 0 处；代价是个别材质这一位）。
**更根本的修法**：读原版材质的 `m_CustomRenderQueue` **原始值**（解包目录 `Material/*.json` 里有）
—— 要一张「材质名 → 原始 queue」的表，**留给下一轮**。

#### 第五轮（同日继续）：`WFDistortion` 的**默认队列**对齐原版

`Buff_Tau_Kroot_FriendlyBoard`（4.70×，偏亮族领头的）的材质 diff 显示：
`Blood splat/Distort R` / `Distort L`（材质 `Heat Distortion`）的 queue **原版 2000 / 我们 3000**
—— 因为自建 `WFDistortion.shader` 在 SubShader Tags 里**写死 `Queue = Transparent`(3000)**，
而原版 `Everguild/FX/Particle Distortion Affect Transparents` 的默认是 **2000**
（原版材质 `m_CustomRenderQueue` 是 −1 ⇒ 队列由 shader 决定）。

✅ **已改**（`WFDistortion.shader` 的 Tags，硬编码 2000 + 注释带出处）—— **不用重导**（shader 是工程资产）。
判据：`WFCMP_MATDUMP` 的 `__queue` 差异**全部消失**（25 → 23 处，只剩 `_EMISSION` 与属性表那些差异）。

⚠️ **但全量 sweep 的净账是 E 41 → 43**（偏亮 36 → 38）：
新进那 2 条**都是小幅跨阈值**（`Rapturous Ruination Board` 1.37→**1.42** ·
`Teleport Trait Summon` 1.31→**1.43**），而关键几条**几乎没动**
（`Buff_Tau_Kroot_FriendlyBoard` 4.70→4.78 · `BulletImpact_Sonic Weapon 1` 2.37→2.42 ·
`SAU_CardDraw` / `UM_CardDraw` / `AcidSpraySweepAttack` **一模一样**）
⇒ **队列不是这一族偏亮的主因**；那 2 条是 `|ln|` 的阈值效应。**改动保留**（它照原版）。

⏭ **下一步得换工具**：`Buff_Tau_Kroot_FriendlyBoard` 的**材质层**（`diff_matdump.py`：只剩自建
shader 的属性表差异 + `_EMISSION`）与**粒子模块层**（`ParticleModuleProbe`：干净）**都没有实质差异**
⇒ 要用 **`EffectIso` 逐槽隔离**找「**哪个渲染器**贡献了多余的亮度」。并排图上看得很清楚：
同几个团块，**原版是暗灰、我们是暖黄**（`_tmp_view/cmp/Buff_Tau_Kroot_FriendlyBoard__{orig,exp}.png`）。

#### 🔬 2026-09-19 晚：**逐渲染器隔离**把 `Buff_Tau_Kroot_FriendlyBoard` 的偏亮**定位到单个材质**

**新工具**：`EffectCompare` 加 **`WFCMP_ISO=all`** —— 每个效果额外渲 N 对图，
第 i 对**只开第 i 个渲染器**（改 `Renderer.enabled`，不动层级）。

⚠️ **判据必须用「增量」**：算 `图亮度和 − 背景`（背景恒为 `512×512×20`）。
这个效果的画面**极淡**（整体增量只占背景的 **2%**），拿绝对亮度比会被背景淹掉、全是 1.00。

结果（9 个渲染器，其余 7 个两侧都是 ~0）：

| iso | 渲染器 | 材质 | shader | 原版增量 | 我们增量 | 比值 |
|---|---|---|---|---|---|---|
| **4** | `Blood splat/Glow R` | `Glow Additive Extra Color Soft` | `Everguild/FX/Extra Color` | 14926 | **132969** | **8.91** |
| **5** | `Blood splat/Glow L` | 同上（同一个材质定义） | 同上 | 13481 | **121957** | **9.05** |

⇒ **多余的亮度全部来自这两个渲染器**，而它们的**材质逐属性完全一致**
（`keys=[_SOFTPARTICLES,_SURFACE_TYPE_TRANSPARENT]` · `_Color=(4,4,4,1)` · `_MainTex=Glow` ·
`_SrcBlend=5` / `_DstBlend=1` / `_SrcBlendAlpha=1` / `_DstBlendAlpha=1` —— **全同**）
⇒ **只可能是自建 `WarpforgeVFX/Particles/Extra Color` 的算式**。

**这正好命中已知的那个黑盒**：`Everguild/FX/Extra Color` 的 **`_Color` / `IN.color` 语义读不到**。
⚠️ 注意 §14.2「已排除」表里那条 `_Color` HDR 值 —— 它只说**值与我们逐位相同**，
**不等于用法相同**，别拿它当"这条已排除"。

⏭ **下一步（有现成路子）**：**反汇编 `Everguild/FX/Extra Color` 的 DXBC** ——
§14.2 已查明它「**20 个变体都带 `SHDR` 指令段**，只是没有 RDEF ⇒ **可反汇编**」，
工具链现成（`工具/dump_shader_blob.py` + `工具/disasm_va.py`）；
或按 §五 方法论做**单变量 A/B**（本族最大单点黑盒，380 材质量级）。

#### 🎯 第六轮（同日）：**反汇编 DXBC 解开 `Extra Color` 黑盒 ⇒ E 43 → 19（−24）**

**新工具**：`工具/disasm_dxbc.py` —— **ctypes 调 Windows 自带的 `d3dcompiler_47.dll`** 的
`D3DDisassemble`（+ COM `ID3DBlob` 取结果）把 DXBC **反汇编成文本**，**不用下载任何东西**
（比装 RenderDoc / dxc 轻得多）。复用 `dump_shader_blob.py` 的 `blob_of()` 取字节码，
并且**名字要从 `sh.m_ParsedForm.m_Name` 取**（⚠️ 不是 `sh.m_Name` —— 我第一次就栽在这儿、命中 0）。

**解出来的东西** —— `Everguild/FX/Extra Color` 的 **20 个 ps 变体**，核心算式**只有这一族**：

```
sample_b r0.xyzw, v1.xyxx, t0.xyzw, s0, cb0[4].x    ← cb0[4].x（_Color）只当 mip LOD
mul o0.xyzw, r0.xyzw, v2.xyzw                        ← 颜色 = 贴图 × 顶点色 v2
mul o0.xyz,  r0.xyzx, v2.xyzx                           （不透明变体：alpha 写死 1）
mov o0.w, l(1.000000)
```

⇒ **`_Color` 从不参与颜色计算**（只出现在 `sample_b` 的 LOD 位置上）。

**我们错在哪**：`WFParticlesExtraColor.shader` 写的是 `tex * _Color * IN.color` —— **白乘一份**。
741 个材质里 **499 个 `_Color` 是白**（无影响），其余 100+ 个是 **2–767 倍**
（盘上有 `{r: 766.9961,…}`；`Glow Additive Extra Color Soft` 是 `(4,4,4,1)`）。

**怎么定位到的**：**逐渲染器隔离**（`WFCMP_ISO=all`）——
`Buff_Tau_Kroot_FriendlyBoard` 多出来的亮度 **100% 来自两个用这个材质的渲染器**，
而它们的材质**逐属性完全一致** ⇒ 只可能是 shader 算式。

**改法与验收**：删掉那个乘法（注释带全部出处）。
- 单效果：整体比值 **2.11 → 0.90**（iso4 8.91 → 2.91 · iso5 9.05 → 3.61）
- **全量：E 43 → 19**（E 率 **4.3% → 2.0%**）· Z 691 → **714** · 偏亮 **38 → 7** · D 28 → 24 · W 195 → 199
- ⚠️ **副作用如实记**：**偏暗 5 → 12**（+7）—— 有些材质的 `_Color` 本来就 < 1 或有别的用途，
  不乘之后**变暗** ⇒ **过度修正**。净账仍是大改善，但**那 7 条下一轮要看一眼**。

**回归验收（同日做的，别跳过）**：拿 `HEAD~1` 的台账逐条比 —— 12 条偏暗里 **只有 1 条是新进**（`Psychic_Lightning_down_Intense_white`，Z/1.35 → E/0.61），其余 11 条**本来就在 E 组**，
其中 **7 条是「从偏亮翻到偏暗」**：
`AcidSpraySweepAttack` 2.00→**0.63** · `BulletImpact_AcidSpit` 2.01→**0.68** ·
`Buff_SW_Runes 1` 1.48→0.63 · `CreateCard EC Elixir` 1.50→0.67 ·
`EnvironmentalCondition Ultramarines Thunders` 1.50→0.56 · `Pulse Onslaught` 1.42→0.67 ·
`Relentless Fusillade` 1.44→0.67。
⇒ **`_Color` 不是对谁都该去掉**：那些材质**本来就该乘**（`|ln|` 有的改善有的略变差，都在 E 内）。
**根因是「多个原版 shader 映射到同一个自建 shader」**（`EffectExporter.ShaderMap` 里
`Everguild/FX/Extra Color` / `Mobile/Particles/*` / `Legacy Shaders/*` / `Particles/*` … 全指向
`WarpforgeVFX/Particles/Extra Color`）——**要彻底对齐得给它们各自的自建 shader**，`下一轮`。

❌ **同日试错记录（别重蹈）**：反汇编里 `cb0[4].x` 出现在 **`sample_b` 的最后一位** ⇒
推断它是 **LOD bias**，于是把采样改成 `SAMPLE_TEXTURE2D_BIAS(..., _Color.x)`。
**A/B 证明是错的、当天回滚**：全量 sweep **E 19 → 42 · D 24 → 41**（偏亮 7→22 · 偏暗 12→20）；
**回滚后逐位回到 E 19 / Z 714 / D 24 / W 199**（数字一模一样 ⇒ 回滚干净）。
推测：那 499 个 `_Color=(1,1,1,1)` 的材质被推成 bias 1（无谓地采模糊 mip）。
⇒ **`cb0[4].x` 到底是什么仍未定**（反汇编只证明它**不参与颜色计算** —— 那一半成立、已保留）。
⚠️ **教训：反汇编读出来的东西也要先量，别凭"看起来像"就下结论。**

**追 `cb0[4].x` 追到哪了（2026-09-19 晚，供下一轮接手）**：
- **我们**那份 shader 的 `CBUFFER` 顺序是 `_Color`(`cb0[0]`) → `_EmissionColor`(`[1]`) → `_MainTex_ST`(`[2]`)
  → `_Cutoff`+3 个(`[3]`) → **`_SrcBlend`/`_DstBlend`/`_SrcBlendAlpha`/`_DstBlendAlpha`(`[4]`)**
  ⇒ **我们的 `cb0[4]` 根本不是 `_Color`** —— 这解释了那次 A/B 为什么会崩。
- **原版**属性表（21 项，`资料/普查产出_0917/shader属性表_块1.md` 第 24 条）顺序是
  `_Color`(Color, HDR, def=(1,1,1,0)) → `_MainTex`(Texture, **NoScaleOffset**) → 一串 float
  （`_SOFTPARTICLES` `_CastShadows` `_Surface` `_Blend` `_AlphaClip` `_SrcBlend` …）
  ⇒ 按 4 个 float 一组排下去，`cb0[4]` 落在**哪一组**取决于 **`_MainTex_ST` 到底生不生成**
  （它 `NoScaleOffset`）——**这一点本地定不了**，所以**别急着按 `cb0[4]` 改算式**。
- ⇒ **下一步若要继续**：先把原版 Pass0 的**精确 CB 布局**解出来（SHDR 段里 `dcl_constantbuffer CB0[5]`
  只说明**用到 5 个 float4**，列不出名字），或者干脆**对 `sample_b` 的 bias 值做一次扫描 A/B**
  （但要如实标注「这是量出来的、不是原版值」）。

#### 第七轮（同日）：`Buff_DA_Forest_Self` 2.20× —— 又一条**队列**锅，而且这次在「走原件」那一侧

**逐渲染器隔离**（`WFCMP_ISO=all`）：整体 1.90，而 **`iso3` 一个就 3.18 倍**（原版 44734 / 我们 142162），
iso4/5 反而 0.82 / 0.78 ⇒ 元凶锁定 `Sparks/Smoke Trails`。

`ParticleModuleProbe` 报的实质差异只有一处：**`Renderer.trailMaterial`** ——
它的材质 `Spiral Trail FX Smoke` → shader **`Everguild/FX/Spiral Trail FX`**，
**两边 shader 同名**（都走**兜底原件**：它**未映射**、工程里也没有这个 shader），
**但 queue 原版 2000 / 我们 3000**。

查 prefab：`Spiral Trail FX Smoke` **有两个定义**（`Everguild/FX/Spiral Trail FX` 与
`URP/Particles/Unlit`），**都是 `renderQueue: -1`**。
⇒ 运行时按 −1 走「重建后那个 shader 的默认队列」，而那个 shader 是**从 `wf_shaders*.bundle` 加载的原件**，
**它的默认队列（3000）与 `battleprefabs*` 里那份（2000）不一样** ——
**两份 shader 来自不同 bundle、默认队列不同**。

⏭ **修法方向（下一轮，别忘）**：**「走原件」这条路不能靠 shader 的默认队列兜底** ——
要在导出时**把原版渲染时的真实队列记下来**（逐材质实例，不是逐材质名），
`binder` 重建时对 `renderQueue == -1` 的槽**显式设**那个值。
（`工具/gen_mat_renderqueue.py` 那张表是**按材质名**的、且只收 `!= -1` 的 ⇒ 对这条不适用。）

#### 第八轮（同日）：**渲染队列改用「原始 JSON 真值」**（第三版判据）+ 暴露出的新问题

**为什么还要改**（第七轮查出来的）：`Buff_DA_Forest_Self` 的 `trailMaterial` 队列对不上，
**而且「走原件」那条路上的 shader 默认队列也可能与原版那份不同**（两份 shader 来自不同 bundle）。
⇒ **不能靠 shader 默认队列兜底**，也不能靠 `Material.renderQueue`（编辑器里读不准）。

**做法**：新脚本 `工具/gen_renderqueue_truth.py` 从 `assets_full` 的**原始 JSON** 建两张表
（`数据/游戏数据/{mat,shader}_renderqueue.tsv` + `renderqueue_truth.json`）：
- `mat`：材质名 → `m_CustomRenderQueue`（977 个；**同名多值只有 2 个**：`SandParticle` [3000,3001] · `Campaign Points` [-1,3000]）
- `shader`：shader 名 → SubShader `QUEUE` 数值（197 个，其中 133 个有值）

**生效队列 = `mat[材质名]` 若 ≥0，否则 `shader[shader名]`；都没有 ⇒ −1**（运行时用 shader 默认）。
`EffectExporter.DefIndex` 换成 `TruthQueue(...)`。
**验证**：`Fx_RockDissolve` 的 QUEUE 是 **`AlphaTest` = 2450** —— 正是 `Invoke Minion Hits Ground`
里 `rock` 实测到的原版值 ✓；`BlastEffect` 重导后 `Mat_Fx_Rock` 记 2450、`Mat_Fx_ParticleSet_apb` 记 3000 ✓

**影响面**：**197 个 shader 里 29 个的 QUEUE 不是 3000**（`Everguild/Matcap/*` = 2000 ·
`Everguild/Cards/*` = 2000/2450 · `Fx_RockDissolve` = 2450 · `UnlitAmbient` 系 = 2000/2450 …）。

⚠️ **全量重导后的账：E 19 → 23**（偏亮 7 → **6** · 偏暗 12 → **17**）——
出组 1 条（`Tap Plasma generator`）、新进 5 条**全是变暗**：
`BulletImpact_tank_{big,huge,baneblade}` 0.71/0.74/0.74 → **0.48/0.48/0.49** · `BulletImpact_Screamer-Killer` 0.79→0.69 ·
`Environmental Condition Emperor's Children` 0.73→0.67。

**逐条查明**：这三条 tank 效果里**只有 `RockDebris`（`Everguild/Matcap/Matcap Full Options`）
的队列从 3000 变成了真值 2000**，提前渲染 ⇒ 被其余 3000 的粒子遮挡 ⇒ 变暗。
⇒ **真值是对的（照原版）**，变差说明**我们那边还有别的地方与原版不同**（同一队列下表现不同）
—— **这次改动是「把掩盖着的问题暴露出来」而不是「改坏」**。
**保留真值**（照原版，且有 JSON 依据）；⏭ **下一轮要查的是**：为什么同一队列下我们的
`RockDebris` 遮挡比原版强（是它更实？还是别的粒子更透？）。

### 14.3 两条顺带纠正

- §13.4 那张表里的 **`BulletHoleMetalThrough.mat` 不指向我们的 shader**（用 URP `Particles/Unlit`）—— 已就地改。
- ✅ **2026-09-19 更正：原版纹理的 sRGB/导入设置「读不到」是错的** —— 原版 bundle 直读（UnityPy `read_typetree()`）就能拿到
  `m_ColorSpace` / `m_TextureFormat` / `m_TextureSettings`，**不必先导 JSON**。实测结论见
  `普查产出_0919/E组_发光族_三件事对照.md` §三：**发光族 9/11 张原版就是 sRGB 且与我们逐字节相同**；
  只有 `DoubleFlames`/`GlowPalet` 是线性，被导出器的 `RenderTextureReadWrite.sRGB` 多编了一次 gamma
  （盘上偏亮 ~60%，但在「线性工程 + `sRGBTexture=true`」下**净中性**；⚠️ 切 Gamma 或绕过导入器就是 ~2×）。

