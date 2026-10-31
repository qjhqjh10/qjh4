# B109_R12 · `EnvironmentalCondition Sororitas Raging Storm` 尾部偏暗 —— 根因查证

> 只读查判据代理 `B109_R12` · 2026-10-31（第一百零九会话）· 工作根 `d:/4`。
> ⛔ 本代理**没有**改任何源码 / 资产 / 账 / 正本 / 其它代理的 `判据/B109_*`；**没有跑 Unity**（只做了 python 只读分析与现读文件）。
> 上游：`判据/B109_R3_oracle真红.md` §3（本条的现场与三步施工单）。

---

## §1 现象复核（把 R3 的读数重算一遍，并补一件 R3 没做过的分解）

### 1·1 五份历史 exp 基线的 `|ln|`（口径已复核 = **8 个时刻的 `|ln(exp/orig)|` 取中位**）

| exp 基线 | 配的 orig 基线 | `\|ln\|` | 方向 | 我复算 |
|---|---|---|---|---|
| `sweep_exp.tsv`（10-31 03:39） | `sweep_orig.tsv`（10-31 01:12） | **0.348** | 我们更暗 | ✅ 0.3487 |
| `sweep_exp_baseline_1031.tsv` | 同上 | **0.519** | 我们更暗 | ✅ 0.5186 |
| `sweep_exp_normal_1021.tsv` | 同上 | **0.527** | 我们更暗 | ✅ 0.5265 |
| `sweep_exp_preferorig_1031.tsv`（注意文件名是 **prefer**orig，不是 R3 简写的 `_preforig_1031`） | 同上 | **0.446** | 我们更暗 | ✅ 0.4458 |
| `sweep_exp_builtin_1021.tsv`（**`WFBIND_FORCE_BUILTIN=1`**） | 同上 | **0.255** | 我们更暗 | ✅ 0.254 |
| `sweep_exp_builtin_1021.tsv` | **`sweep_orig_1021.tsv`（10-21，同一天）** | **0.031** | 我们更暗 | ✅ 0.031 |

⚠️ **R3 表里「0.255」配的是 10-31 的 orig；配同一天的 10-21 orig 则是 0.031** —— 这两个数差 8 倍，而 exp 是**同一份文件**。
⇒ **orig 基线自己就在漂**（见 1·3），任何「拿某一份 sweep 当绝对判据」的做法在这条上都不成立。

### 1·2 🔴 新发现（本条的**形态**比幅度更有判别力）：**差异是一个恒定绝对量，不是乘法偏暗**

| t | 0.15 | 0.30 | 0.50 | 0.75 | 1.00 | 1.50 | 2.00 | 3.00 |
|---|---|---|---|---|---|---|---|---|
| `sweep_orig.sum` | 1356 | 1413 | 786 | 81 | 84 | 90 | 95 | 103 |
| `sweep_exp.sum` | 1321 | 1378 | 750 | 47 | 51 | 59 | 65 | 75 |
| **差（orig−exp）** | **35** | **35** | **36** | **34** | **33** | **31** | **30** | **28** |
| `orig.lit − exp.lit` | 84 | 86 | 99 | 83 | 75 | 67 | 61 | 68 |

**这个「恒定 ≈35」在每一对基线里都成立**（我逐对算过）：

| 配对 | 8 个时刻的绝对差 |
|---|---|
| `orig(10-31)` − `exp(10-31)` | −35 −35 −36 −34 −33 −31 −30 −28 |
| `orig(10-31)` − `exp_baseline_1031` | −33 −35 −38 −38 −38 −40 −40 −40 |
| `orig(10-31)` − `exp_builtin_1021` | −15 −17 −19 −19 −19 −21 −22 −23 |
| `orig(10-21)` − `exp_normal_1021` | −21 −21 −21 −21 −21 −22 −24 −25 |

⇒ 三条结论：
1. **「前 0.5 s 只差 2.5%」不是「头部对得上」，是 35 这个固定量被 1300 的大数淹没了。** 头部的「对」证明不了尾部的东西是对的。
2. **差的是一个「全程恒定」的成分** ⇒ 只可能是**唯一常驻的那一组发射器**（见 §2 H1：只有 4 个 `Smoke Traily Background` 是 `looping=1`，其余 12 个全是 `looping=0` 的瞬发）。
3. 恒定量的**幅度随跑次变**（15 / 21 / 33 / 35 / 38 / 40）而**方向恒为「我们更暗」** ⇒ 有一个真的、很小的系统性差异，再被**尺子放大/漂移**。

### 1·3 orig 基线自己在漂（同一份原件、不同跑次）

`sweep_orig_1021` 尾部 3.0 s = **87** · `sweep_orig.tsv`(10-31) = **103**（+18%）；0.75 s 是 **64 → 81**（+27%）。
exp 侧同步漂（`exp_normal_1021` 62 → `exp` 75）。两侧一起变 ⇒ **不是「原件变了」，是尺子变了**（见 §2 H8）。

---

## §2 根因假设表

> 先说**已经排除掉的**（本代理新做的，比 R3 更进一步）：
> - **结构**：18 个 GameObject / 18 个 Transform / 16 个 `ParticleSystem` / 16 个 `ParticleSystemRenderer`，**逐路径比两侧一致**（我按 `路径 → 组件` 建表逐字段 diff，不是按名字次序 —— AssetRipper 会重排兄弟）。
> - **变换**：16 个节点的 `m_LocalPosition` / `m_LocalScale` / `m_IsActive` **逐值相同**（唯一的「差异」是同名对象的配对次序，属重排，不是值）。
> - **Renderer 键值**：`m_RenderMode(全 0=Billboard)` / `m_SortMode(0)` / `m_SortingFudge(0)` / `m_MinParticleSize(0)` / `m_MaxParticleSize(500)` / `m_LengthScale(2)` / `m_SortingLayerID(0)` / `m_SortingOrder(0)` / `m_RendererPriority(0)` / `m_Pivot` / `m_Flip` **逐项相同**。
> - **材质属性值**：把 binder 里 5 份 `WFMatDef` 的 37 个 float + 6 个 color + 2 张图，与原件 `d:/2/tools/assetripper_out/ExportedProject/Assets/Sand Storm.mat` **逐字段比**：`_Alpha_Modifier 0.459` / `_Alpha_Modifier_Multiply 5` / `_SrcBlend 5` / `_DstBlend 10` / `_ZWrite 0` / `_ZTest 4` / `_Cull 2` / `_Color_Mutliplier 1` / `_EmissionColor` 全黑 / `customRenderQueue 3000` / keywords `_SURFACE_TYPE_TRANSPARENT` —— **全部相同**。
> - **时程**：`lengthInSec` / `looping` / `prewarm` 16/16 相同；**只有 4 个 `Smoke Traily Background` 是 looping（len 5 · prewarm 1）**，其余 12 个 loop=0（len 0.5 / 2）⇒ **t ≥ 0.75 s 的全部画面只由这 4 个系统产生**。

| # | 假设 | 判据（文件:行 / 字段） | 支持 / 反对 | 怎么验 | 结论 |
|---|---|---|---|---|---|
| **H1** | 粒子数 / 发射率少 | `EmissionModule.rateOverTime` / `maxNumParticles`（两侧同） | **反对**：ps 逐字段同；iso 逐槽 `contrib` 原版 54.2/153.2/40.8/18.8 vs 我们 52.1/149.2/39.2/18.6（比值 0.962/0.974/0.959/0.992） | 已验（`iso_stats_*.tsv` 10-25 那一趟） | **排除**（差异 <4%，且方向≈头部 0.97，不是尾部 0.65） |
| **H2** | lifetime/曲线的**采样口径**（`fixedTimeStep` / `Simulate` 序列） | `EffectSweepBatch.cs:558-566`（`SeedAndReset`= Stop→钉种子→`Simulate(0,restart:true,fixed:true)` 再 `Simulate(t,restart:false,fixed:true)`）vs `EffectIso.cs:1310-1315`（一次 `Simulate(SimTime,restart:true,fixed:false)`） | **中立**：两条路径**两侧都用同一份代码**；但两侧 prefab 的**组件遍历次序**可能不同（导出器重排子节点）⇒ `withChildren:true` 的重复推进可能不对称 | 同一效果分别用两种 `fixedTimeStep` 跑 sweep，看 `sum` 变不变 | **未排除**，优先级中（它是唯一解释「iso 相同 / sweep 不同」的非资产原因之一） |
| **H3** | `simulationSpace` | `ParticleSystem.moveWithTransform: 1`（16/16 两侧同）、`scalingMode: 1` | **反对** | — | **排除** |
| **H4** | shader blend / `_EMISSION` | 2026-10-21 受控 A/B：`WFBIND_FORCE_BUILTIN=1` ⇒ `\|ln\|` **0.349 → 0.031**（`普查产出_1021/W60_真机与Unity_结果.md:51`）。本效果只用 3 个 shader（`普查产出_0913/效果_shader_对账.md:738`）；其中**两个在 `UseOriginal` 白名单**：`Everguild/FX/Alpha Mask One Layer`（`WarpforgeShaderMap.cs:100`，只挂在 4 个 smoke 上 = `Sand Storm`）与 `Everguild/FX/Extra Color`（`:238`，只挂在 flash 上 = `…_flash`/`…_floor`，而 flash 全是 `looping=0` 的瞬发、t≥0.75 s 已熄灭）；第三个 `URP/Particles/Unlit` 不受该开关影响 ⇒ **能在 `FORCE_BUILTIN` 前后只改「尾部」的变量只有一个 = `Alpha Mask One Layer`**（头部在两档里都是 0.97~0.99，若变的是 `Extra Color` 头部就该动） | **支持（强）**：唯一能「只动尾部」的单变量。**反对（强）**：10-25 的逐槽隔离图里，**原件路径下两侧逐像素几乎全同**（见 H7 那行的数字） | `WFBIND_FORCE_BUILTIN="Alpha Mask One Layer"` 单跑一趟（§4 第 3 条） | **最可疑，但与 H7 的读数直接冲突 ⇒ 必须做单变量 A/B 才能裁** |
| **H5** | `renderQueue` / 透明排序（4 层 smoke 的**合成次序**） | 4 份 smoke 材质都 `renderQueue 3000`、`ZWrite 0`、`SortMode 0`；4 个系统的世界位置是 (−17.6/18.25, 8.08, 59.4) 与 (−11.95/23.9, 8.08, 26.3)，**两两到相机（z=−167）的距离只差 0.4** ⇒ 同队列 + 近似等距 ⇒ 次序靠 tie-break | **支持**：能解释「每一层单独渲都一模一样、4 层一起渲就不一样」（alpha `over` 不满足交换律）。**反对**：两侧变换逐值相同 ⇒ 距离相同 ⇒ 理论上次序也该相同 | iso 里把 4 个 smoke 逐层 `contrib` 之和与「4 层合成」比；或临时把 `Sand Storm` 的 `renderQueue` 改成 3001/3002/3003 逐层错开再量 | **未排除**，优先级中高 |
| **H6** | 后处理（`AnimFXModulePostProcess` / LUT） | 原件 prefab 上有 `AnimFXModulePostProcess`（`Lightning Main Start`）；我们的 prefab 没有（模块走 `WarpforgeEffectLibrary` 数据） | **反对**：sweep/iso 里都**不跑**模块（只 `binder.Apply()`，`EffectSweepBatch.cs:575-576`），两侧等价；且 `Update` 在 edit 模式不跑 | — | **排除**（对尺子无影响）；见 §5·1 的**真机**缺口另记 |
| **H7** | 材质实例化 / `material` vs `sharedMaterial` / 贴图重导入 | `EffectIso.RenderIsolated` 的交叉档（`_tmp_view/iso/…_cross00~03_*.png`）：`exp + 原版材质` 与 `orig_plain` 的逐像素差 = **sum\|d\| 5~39 / 262144 像素**（`pix>6` = **0**）；`orig + 我们的材质` 差 2336~2959（85~101 像素）。逐槽 orig vs exp 直比：**99.9% 像素差 = 0**，最大差 ≤3/255，**无符号偏置**（逐通道均值差 ≤0.0015） | **反对「亮度/材质有差」**：4 个 smoke 槽**在原件路径下逐像素完全一致** | 已验（10-25 的 iso 图，本代理用 numpy 现算） | **排除**（至少覆盖 SimTime=1.2 s 的原件路径） |
| **H8** | 🔴 **尺子的「位置依赖」**：`EffectSweepBatch` 的**原版侧读数**随「同一进程里前面跑过多少效果」变；导出侧不随（13/13 逐位相同） | `EffectSweepBatch.cs:312-327`（`StrikeEffect` 单独 437 / 垫一个 435 / 小批里排第 6 **344**）+ 正本原话 `资料/特效还原_进度与交接.md:1820`：**「拿单效果/小批的 `WFSWEEP_SIDE=both` 重测 5 条，2 条与全量基线吻合、3 条不吻合 —— `Sororitas Raging Storm` **0.939 vs 0.783**（同条件连跑两次逐条稳定 ⇒ 不是随机噪声）」** | **支持（最强）**：**同一条效果、同一套代码、只改扫描位置，比值从 0.783 变成 0.939**（`\|ln\|` 0.245 → **0.063**，低于 0.30 阈值）。`尺子不稳定名单.tsv:2` 它排**第 1**（`最大_dln 0.412`，两次判定「偏暗」/「对得上」） | §4 第 1 条（单效果两跑） | **成立且量级吻合**：0.783→0.939 的 20% 摆动与 `\|ln\|` 0.348→0.06 是同一个量级 ⇒ **我们看到的「真红」里，有相当一部分就是这个** |

---

## §3 结论

### 3·1 定得了的

1. **不是「资产没复刻好」**：PS 参数 / Renderer 键值 / 变换 / 层级 / 材质指派 / 材质属性值 / 着色器指派，**逐项已排除**（§2 开头那一段）。R3 的「PS 16/16 同」我复核成立并推到了 Renderer 与材质属性层。
2. **不是「少了一个发射器」**：t ≥ 0.75 s 的画面**只由 4 个 looping 的 `Smoke Traily Background` 产生**，而这 4 个**逐槽隔离后逐像素一致**（99.9% 像素差 0）。
3. **形态是「一个恒定的绝对差 ≈ 15~40」，不是「整体乘 0.65」**（§1·2 六对基线全成立）⇒ 「头部 0.97」不能当作尾部没问题的证据。
4. **尺子本身在这条上确定不可靠**：同一条效果在**全量扫描里 0.783 / 单独跑 0.939**（正本已记，确定性不是噪声）；且 orig 侧读数两次跑差 27%（§1·3）。

### 3·2 定不了的（**根因未闭合**，缺一步）

现在的证据形成**一对直接冲突**，必须用实验裁：

- **H4（有支持）**：10-21 的 `FORCE_BUILTIN` A/B 说「原件 shader 路径偏暗、自建那份才对」（0.349→0.031），而且**只有 `Alpha Mask One Layer` 这一个变量能解释「只有尾部变」**。
- **H7（有支持）**：10-25 的逐槽隔离说「原件 shader 路径下，4 个 smoke 槽与原件**逐像素相同**」。

两者不可能同时为真。可能的解释有三，**都必须靠重测分开**：
(a) 10-21 那两趟 FORCE_BUILTIN A/B 与全量基线**不是同一次导出 / 不是同一种跑法**（跨进程 vs `both`）⇒ 那个 0.031 是尺子假象；
(b) 10-25 的 iso 是在 **1.2 s、单层**下比，饱和区掩盖了差异，而 sweep 是 **0.75~3.0 s、4 层叠加**；
(c) H8（位置依赖）同时污染了这两次。

> ⚠️ **⛔ 现在不要按任何一个假设去改 shader 或白名单** —— `特效还原_进度与交接.md:1848` 那条「`Asteroid Zone`/`Void Combat` 评估从 `UseOriginal` 移除」也是**基于同一批被污染的数**，同批待重验（本条只报，不改账）。

### 3·3 缺哪一步（按优先级）

1. **单效果两跑**（H8 的直接判据，也是 R3 §3 第 1 步）—— 看 `|ln|` 是否 ≤0.30；同时给出一条**不掺位置污染**的 orig 真值。
2. **`WFISO_SIMT=0.75 / 3.0` 逐槽隔离**（R3 §3 第 2 步；`WFISO_SIMT` 已由 `B109_E13` 今天加进 `EffectIso.cs:115`，**可以直接用**）。
3. 只有 (1)(2) 仍指向「smoke 真的暗」时，才做 **单变量 shader A/B**：`WFBIND_FORCE_BUILTIN="Alpha Mask One Layer"`。

### 3·4 若最终落在 H4，修法（**现在不要动手**）

- 若「原件 暗 / 自建 对」**被判实**：把 `Sand Storm` 用的 `Everguild/FX/Alpha Mask One Layer` 从 `WarpforgeShaderMap.UseOriginal`（`:100`）暂时移出（一句话可回滚，白名单是"单开关"设计），并对照 `WFAlphaMaskOneLayer.shader` 头部的 7 个活属性（`_MainTex` / `_SecondaryTex` / `_PrimaryTex_Scale_XY_Speed_ZW` / `_SecondaryTex_Scale_XY_Speed_ZW` / `_Color_Mutliplier` / `_Alpha_Modifier` / `_Alpha_Modifier_Multiply`）。
  ⚠️ **它影响 29 个效果**（白名单注释行就是 `// 29`）⇒ 必须全量复跑，不能只看这一条。
- 若「位置依赖」被判实（更可能）：**不改任何资产**，改的是尺子 —— 把 `WFSWEEP_OUT`（`EffectSweepBatch.cs:61`）与「单效果跑也得同位置」写进 `analyze_sweep.py` 的口径说明，并把本条在规格表 `备注` 里记成「尺子位置依赖，需单效果复核」。

---

## §4 给调度台的隔离实验命令（逐字，**主对话跑；子代理不许跑 Unity**）

> 🔴 前置：`EffectSweepBatch.cs:61` 的 `WFSWEEP_OUT` **仍然没有加**（R3 §4 的施工单未落）⇒ **单效果两跑会整份覆盖 962 行基线**。所以前三条备份是**必须**的。

```bash
# 0) 备份（三个基线 + 目标清单）
cd d:/4/Unity
cp 资料/比对基线/sweep_orig.tsv   资料/比对基线/_bak_${B109R12}_sweep_orig.tsv
cp 资料/比对基线/sweep_exp.tsv    资料/比对基线/_bak_${B109R12}_sweep_exp.tsv
cp 资料/比对基线/sweep_frames.tsv 资料/比对基线/_bak_${B109R12}_sweep_frames.tsv
cp _tmp_view/sweep_targets.txt    _tmp_view/sweep_targets.bak_before_B109R12.txt

# 1) 单效果两跑（orig→exp 同进程；两侧同条件，最干净）
printf '%s\n' "EnvironmentalCondition Sororitas Raging Storm" > _tmp_view/sweep_targets.txt
unset ELECTRON_RUN_AS_NODE
WFSWEEP_SIDE=both "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod EffectSweepBatch.Run \
  -logFile d:/4/_tmp_view/b109r12_sweep_both.log
# 日志末行应是「成功 1，跳过 0」；随后把这两份 8 行留下来：
cp 资料/比对基线/sweep_orig.tsv d:/4/_tmp_view/b109r12_sweep_orig_1fx.tsv
cp 资料/比对基线/sweep_exp.tsv  d:/4/_tmp_view/b109r12_sweep_exp_1fx.tsv
# 还原
cp 资料/比对基线/_bak_${B109R12}_sweep_orig.tsv   资料/比对基线/sweep_orig.tsv
cp 资料/比对基线/_bak_${B109R12}_sweep_exp.tsv    资料/比对基线/sweep_exp.tsv
cp 资料/比对基线/_bak_${B109R12}_sweep_frames.tsv 资料/比对基线/sweep_frames.tsv
cp _tmp_view/sweep_targets.bak_before_B109R12.txt _tmp_view/sweep_targets.txt
# 判据：|ln(exp/orig)| 的 8 点中位 ≤0.30 ⇒ 本条结案为「尺子位置依赖」（FALSE RED）
```

```bash
# 2) 逐槽隔离（尾部两个时刻）—— 备份 iso 产物（它会被覆盖）
cd d:/4/Unity
mkdir -p _tmp_view/iso_bak_B109R12
cp 资料/比对基线/iso_stats_orig.tsv 资料/比对基线/iso_stats_exp.tsv 资料/比对基线/iso_frames.tsv _tmp_view/iso_bak_B109R12/
cp _tmp_view/iso/*Raging\ Storm* _tmp_view/iso_bak_B109R12/ 2>/dev/null
unset ELECTRON_RUN_AS_NODE
WFISO_FX="Raging Storm" WFISO_SIMT=0.75 bash d:/4/Unity/工具/run_iso.sh both
python -I -X utf8 d:/4/Unity/工具/analyze_iso.py "EnvironmentalCondition Sororitas Raging Storm"
# 再跑 3.0 s（同一个入口，只换 WFISO_SIMT）
WFISO_FX="Raging Storm" WFISO_SIMT=3.0 bash d:/4/Unity/工具/run_iso.sh both
python -I -X utf8 d:/4/Unity/工具/analyze_iso.py "EnvironmentalCondition Sororitas Raging Storm"
# 判据：若某个 smoke 槽 ratio 掉到 0.6~0.75 ⇒ 该槽真的暗（进第 3 条）；若仍 ≈0.96~1.00 ⇒ H4 被推翻
```

```bash
# 3) 单变量 shader A/B（**只在第 2 条仍指向 smoke 真的暗时**才跑）
cd d:/4/Unity
unset ELECTRON_RUN_AS_NODE
WFBIND_FORCE_BUILTIN="Alpha Mask One Layer" WFISO_FX="Raging Storm" WFISO_SIMT=0.75 \
  bash d:/4/Unity/工具/run_iso.sh exp
python -I -X utf8 d:/4/Unity/工具/analyze_iso.py "EnvironmentalCondition Sororitas Raging Storm"
# 与第 2 条的 exp 数字逐槽比：只翻这一个 shader，若 smoke 槽 contrib 变化 <2% ⇒ 「原件 vs 自建」无差
```

（跑完记得 `cp _tmp_view/iso_bak_B109R12/* 回原位`，否则后面别的效果会读到这一条的 iso 数字。）

---

## §5 顺手发现（⛔ 本代理不改，交调度台分流）

1. 🔴 **我们的 prefab 只保留了原件 8 个脚本里的 1 个**。原件根/子节点上的 MonoBehaviour（按 `m_Script` guid 反查）：`ScenarioParticleSystemBlender`(根) · `ShakeAndSoundObject`(根) · `ScenarioParticleSpawnerBlender`(根) · `ParticleSystemAreaSpawner`(`Psychic_Lightning_down`) · `ParticleSystemPoolable`(`Lightning Main`) · `AnimFXController`(`Lightning Main`/`Lightning Main Start`) · `AnimFXModuleScreenShake`(同上) · `AnimFXModulePostProcess`(`Lightning Main Start`)。
   我们只有 `ParticleSystemPoolable` + `WarpforgeEffectBinder`。
   ✅ **对尺子无影响**（`ScenarioParticleSystemBlender.Update` 是纯运行时循环、edit 模式不跑；`DoScenarioBlend` 只在有 scenario 时被调 —— 判据 `decomp_full/ScenarioParticleSystemBlender__{Update,DoScenarioBlend}.c`）。
   ⚠️ **但对真机可能是缺口**：`数据/游戏数据/env_blendables.json` 命中「Raging Storm」**23 处**、`animfx_components.json` **9 处** ⇒ 看起来已由数据侧覆盖；**这 23/9 处到底装出哪些 kind、有没有 `ScenarioParticleSystemBlender` 这一族，本代理没核**（点名要 `WFModuleFactory` 侧对账）。
2. ⚠️ **`m_Mesh` 差异（`Lightning Main` / `Lightning Main Start`）**：原件是**内置 `Quad`**（`fileID: 10210`），我们是一份工程网格（guid `89f0959…`）。两者 `m_RenderMode: 0`（Billboard）⇒ **惰性**，但记一笔（同族坑：`10210 = 内置 Quad`，⛔ 别当「跨文件引用所以本地没有」）。
3. ⚠️ **`m_StitchLightmapSeams` 1（我们）vs 0（原件），16 处** —— 纯编辑器导入设置，运行时无影响。
4. ⚠️ **`ParticleSystemPoolable` 会把 `Lightning Main` 的 `main.stopAction` 设成 `Callback`**（`CardPresentation/Battle/ParticleSystemPoolable.cs:28-32`）；**原件也挂着它** ⇒ 这是我们**复刻对了**，不是差异。
5. 🔴 **`EffectSweepBatch.cs:61` 的 `WFSWEEP_OUT` 仍未加**（R3 §4 施工单未落），任何人照 §4 第 1 条跑都会整份覆盖基线 ⇒ **本条已按「先备份」写命令**，但**根治仍需加那一行**。
6. 📌 `资料/任务线/特效线/任务总表.md:17`「`Raging Storm` / 不稳定名单那几条需 Unity 单效果两跑」**仍开**，与本文件 §4 是同一件事（别另开账）。


---

## §6 实测结论（2026-10-31 · 调度台亲跑 · 判据 #1）

`WFSWEEP_SIDE=both` 单效果两跑（orig/exp **同进程同条件**，目标清单只留这一条），8 个时刻的亮度比值：

| t | orig lit | exp lit | ratio | abs(ln) |
|---|---:|---:|---:|---:|
| 0.15 | 974 | 948 | 0.973 | 0.027 |
| 0.30 | 1015 | 985 | 0.970 | 0.030 |
| 0.50 | 996 | 962 | 0.966 | 0.035 |
| 0.75 | 292 | 269 | 0.921 | 0.082 |
| 1.00 | 302 | 276 | 0.914 | 0.090 |
| 1.50 | 313 | 298 | 0.952 | 0.049 |
| 2.00 | 318 | 298 | 0.937 | 0.065 |
| 3.00 | 321 | 297 | 0.925 | 0.078 |

**均值 abs(ln) = 0.057，最大 0.090**（阈值 0.30）。

⇒ **FP-VFX-07 的 0.348 判为「尺子位置依赖」造成的假红（H8 成立）**，与正本 `资料/特效还原_进度与交接.md:1820` 记的「全量 0.783 / 单跑 0.939」同一量级。
⇒ 本条**结案**：⛔ 不动 shader、⛔ 不动 `UseOriginal` 白名单；**不需要**再跑 §4 第 2/3 条。
⇒ 四条 oracle 真红（FP-VFX-01/02/07/09）**全部为假红**：01/02/09 是基线过期（已补跑 5 效果 40 行、基线 957 → 962 效果），07 是尺子位置依赖。
