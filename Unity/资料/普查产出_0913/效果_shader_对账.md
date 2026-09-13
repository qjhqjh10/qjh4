# 效果 → 用了哪些原版 shader · 对账表（958 行）

> 2026-09-13 生成 · 纯 Python 解析（未运行 Unity）· 数据源 `Assets/WarpforgeVFX/导出报告.tsv`

## 〇、映射表在哪（判据来源）

| 用途 | 路径 | 条数 |
|---|---|---|
| **运行时解析链（权威）** | `d:/4/Unity/MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs` → `Replacements` | 28 键 |
| 导出期（占位材质选谁 + 报告标不标「近似」） | `d:/4/Unity/MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs` → `ShaderMap` | 35 键 |
| 兜底实际名单 | `Assets/StreamingAssets/WarpforgeVFX/wf_shaders.bundle`（UnityPy 实读 45 个）+ `wf_shaders_extra.bundle`（42 个，重叠 1） | 87 |


运行时 `WarpforgeShaderMap.TryResolve` 的链：**`Replacements` → `Shader.Find(原名)` → `WarpforgeShaderLoader`（两个 bundle）→ 全落空则 `WarpforgeEffectBinder` 返回 null、该材质槽保留导出时的占位材质**。⚠️ 两份映射表**必须同步改**（`WarpforgeShaderMap.cs` 与 `EffectExporter.cs` 各有注释互相点名）。另 `工具/extract_missing_shaders.py:BUILTIN_PREFIX` 给出「算工程自带」的 9 个前缀。

## 一、数据源分段结构

| 事实 | 值 |
|---|---|
| 字节 | 196,683（UTF-8 BOM） |
| 换行切分 | 960 段 → 末段空串，即 **959 行** |
| 表头 | `# 效果名 ⇥ 状态/原因` —— **只有 2 格**，第 3 格无表头 |
| 数据行 | **958 行 × 3 格**（第 3 格 = 文档里说的「第 3 段」） |
| 状态列 | 958 行**全 OK**，无 FAIL |
| 第 3 格内部（`；` 切） | ① `材质定义N个` ② `原 shader: A, B, C` ③ `近似替代 N 处`（可缺：292 行无 ③ / 666 行有） |
| 校验 | 958 行都有 ②，无空 shader 列表；材质数 ≥ shader 名数恒成立 |


⚠️ **②段里混进了非原版名**：`EffectExporter.cs:298` 对 **trailMaterial 槽**写的是**替换后的目标名**（`usedShaders.Add(to)`），普通材质槽写的才是原名（`:286`）。所以 `WarpforgeVFX/Particles/Extra Color`(304 条) / `WarpforgeVFX/FX/Distortion`(6) / `WarpforgeVFX/Matcap/Matcap`(1) 是**自建 shader 名**，不是原版 shader —— 本表已把它们标出并从「原版清单」里剔除。

## 二、总账

| 指标 | 值 |
|---|---|
| 效果行 | 958 |
| 材质定义总数 | 9028 个（每效果 1–35） |
| 第 3 段 shader 名去重 | **70**（`Everguild/` 42 · 自建 `WarpforgeVFX/` 3 · 其他 25） |
| 原版 shader（剔自建） | **67** |
| 映射表命中（A 自建替代 + B 标准） | 32 |
| **未映射**（两张表都没有） | **38** |
| ├ 仍可由工程/包同名解析（C） | 5 |
| ├ 只能靠原版 bundle 兜底（D+E） | 32 |
| └ 🔴 完全未解析（F） | 1 |
| 效果级：含未映射 shader | **414 / 958** |
| 效果级：仅 bundle 兜底 | 413 |
| 效果级：含 🔴 | 1（`CardPrefab`） |
| 效果级：全干净（无未映射） | 544 |
| 标了「近似替代」 | 666 条效果 / 2468 个材质槽 |


### ⚠️ 与 `可并行任务清单.md` ④ 的口径出入（派活前务必用本表口径）

④ 写「导出报告里 107 个 shader 名 / 44 个 `Everguild/`」—— **复现不出来**。严格解析（①栏切 `；`、②栏切 `,`，与 `工具/extract_missing_shaders.py:used_shader_names()` 同一刀法）得 **70 名 / 42 个 Everguild**。别的切法也凑不出 107：报告∪主包=95、∪两包=101、∪两包∪两映射表=103。**而 ④ 的「77 个」是对的**：主包 45 + 补充包 33 − 1 重叠 = 77（现测补充包已 42 名，主+补去重 86）。且 77（或 86）里**只有 59 个真的在这 958 条效果里出现过**（= 原版名里既非标准也非工程自带的那批），其中 33 个连自建替代都没有，见 §四。


## 三、解析状态分类

| 类 | 含义 | 名数 | 覆盖效果数（去重） |
|---|---|---|---|
| A自建替代 | 自建替代（已映射） | 26 | 886 |
| B标准自带 | 标准 shader（映射到自身） | 6 | 949 |
| C工程同名 | 未映射·工程/包内同名 | 5 | 311 |
| D主包兜底 | 未映射·原版主包兜底 | 6 | 12 |
| E补充包兜底 | 未映射·原版补充包兜底 | 26 | 131 |
| F未解析 | 🔴未映射且未解析 | 1 | 1 |

**要点**：26 个走自建替代的里有 20 个是**近似**（`EffectExporter.ShaderMap` 值带 `*`，见 `WFMatDef`/报告里的「近似替代」计数）；32 个只能靠**原版 bundle 兜底**——能渲染，但按 `WarpforgeShaderMap.cs:118` 的实测注释，bundle 里的 shader **在编辑器下渲染会出故障**（同名的工程 shader 优先），而且**是原版编译字节码，不能进发布版本**（交接文档红线）。所以真正「安全可发布」的覆盖 = A+B+C 那一半。


### 未映射（两张映射表都查不到）的 38 个名字

| shader | 影响效果数 | 解析状态 | 运行时落到哪 |
|---|---|---|---|
| `WarpforgeVFX/Particles/Extra Color` | 304 | 未映射·工程/包内同名 | 自建同名 shader（拖尾槽写入的替换名，不是漏配） |
| `Everguild/FX/Rays For Trail` | 42 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Spine/Special/HiddenPass` | 23 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/Particle Premultiply` | 12 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Shader Graphs/Fx_ParticleDissolve_apb` | 11 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/ShieldVfx` | 10 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/Spiral Trail FX` | 9 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `WarpforgeVFX/FX/Distortion` | 6 | 未映射·工程/包内同名 | 自建同名 shader（拖尾槽写入的替换名，不是漏配） |
| `Everguild/FX/Glow Shader` | 5 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/Cards/Gem Crystal Glitter` | 4 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/Burning` | 4 | 未映射·原版主包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/FX Shine For Animation` | 4 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Shader Graphs/Fx_RockDissolve` | 4 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/Burning Dissolve` | 3 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/Specific/Necrons Rays` | 3 | 未映射·原版主包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/Matcap/Matcap Full Options VAT` | 3 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Shader Graphs/Eclipse Tau` | 3 | 未映射·原版主包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `TextMeshPro/Distance Field` | 3 | 未映射·工程/包内同名 | 占位材质保留 → 工程/包内同名 shader |
| `Everguild/Cards/3D Card Explosion` | 2 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/Cards/BlobShadow` | 2 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/MarkerLight` | 2 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/Wind Matcap` | 2 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `GlassRefraction` | 2 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Universal Render Pipeline/Particles/Lit` | 2 | 未映射·工程/包内同名 | 占位材质保留 → 工程/包内同名 shader |
| `Custom/EditorIcon` | 1 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/Cards/3D Card` | 1 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/Cards/Card Swarm Effect` | 1 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/Cards/Gem Crystal Glitter Explosion` | 1 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/Cards/Necrons Base Death` | 1 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/Cards/Shatter Inner Pieces` | 1 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/Card Highlight And Shadow` | 1 | 未映射·原版主包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/Card Remnant Death Icon` | 1 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/Halo UV scroll` | 1 | 未映射·原版主包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/Particle Dissolve Premultiply` | 1 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/Specific/Pray Glow` | 1 | 未映射·原版补充包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `Everguild/FX/Vortex` | 1 | 未映射·原版主包兜底 | 原版 bundle（编辑器下渲染有故障，不可发布） |
| `TextMeshPro/Distance Field Offset` | 1 | 🔴未映射且未解析 | **占位材质保留** |
| `WarpforgeVFX/Matcap/Matcap` | 1 | 未映射·工程/包内同名 | 自建同名 shader（拖尾槽写入的替换名，不是漏配） |

### 🔴 唯一「未解析」：`TextMeshPro/Distance Field Offset`（1 条效果 `CardPrefab`）

工程 TMP 里没有这个同名 shader（TMP 现有 13 个名字，只有 `Distance Field` / `Distance Field Overlay` / `Mobile/…`），两张映射表也没有。**但这是有意豁免**：`Assets/WarpforgeArena1/Editor/BlendProbe.cs:119 NotOurBusiness()` 明确写「TMP 文字 shader … 是文字不是特效材质，不该由我们接管」。⚠️ 豁免只写在 `BlendProbe` 里 —— `WarpforgeEffectBinder` / `WarpforgeShaderMap.TryResolve` **没有这条豁免**，运行时该槽仍会保留占位材质。另注：`CardPrefab` 严格说是卡牌预制体而非 VFX 效果，958 行里混了这一行。


## 四、副产品：原版 shader 清单（按影响效果数降序）

> 计数 = 958 行里有多少行的第 3 段出现该名。**已剔除 3 个自建 `WarpforgeVFX/*`**（拖尾槽写入的替换名）。原版名共 **67** 个。

| # | shader | 影响效果数 | 解析状态 | 映射到 |
|---|---|---|---|---|
| 1 | `Universal Render Pipeline/Particles/Unlit` | 947 | 标准 shader（映射到自身） | Universal Render Pipeline/Particles/Unlit |
| 2 | `Everguild/FX/Extra Color` | 741 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 3 | `Mobile/Particles/Additive` | 423 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 4 | `Everguild/FX/Particle Distortion Affect Transparents` | 233 | 自建替代（已映射） | WarpforgeVFX/FX/Distortion |
| 5 | `Mobile/Particles/Alpha Blended` | 159 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 6 | `Sprites/Mask` | 122 | 标准 shader（映射到自身） | Sprites/Mask |
| 7 | `Everguild/Matcap/Matcap Full Options` | 120 | 自建替代（已映射） | WarpforgeVFX/Matcap/Matcap |
| 8 | `Sprites/Default` | 117 | 标准 shader（映射到自身） | Sprites/Default |
| 9 | `Everguild/Matcap/Matcap With Texture` | 70 | 自建替代（已映射） | WarpforgeVFX/Matcap/Matcap |
| 10 | `Everguild/FX/Rays For Trail` | 42 | 未映射·原版补充包兜底 | — |
| 11 | `Everguild/FX/Unlit UV scroll` | 39 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 12 | `Everguild/FX/Multi Ray` | 33 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 13 | `Everguild/FX/Alpha Mask One Layer` | 29 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 14 | `Everguild/FX/Particle Dissolve Mask` | 29 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 15 | `Everguild/FX/Particle Shine Custom Vertex Streams` | 26 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 16 | `Everguild/FX/Particle Premultiply Greyscale Coloring` | 25 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 17 | `Spine/Special/HiddenPass` | 23 | 未映射·原版补充包兜底 | — |
| 18 | `Legacy Shaders/Particles/Additive` | 19 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 19 | `Particles/Standard Unlit` | 17 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 20 | `Universal Render Pipeline/Lit` | 16 | 标准 shader（映射到自身） | Universal Render Pipeline/Lit |
| 21 | `Everguild/FX/Particle Premultiply` | 12 | 未映射·原版补充包兜底 | — |
| 22 | `Shader Graphs/Fx_ParticleDissolve_apb` | 11 | 未映射·原版补充包兜底 | — |
| 23 | `Everguild/FX/ShieldVfx` | 10 | 未映射·原版补充包兜底 | — |
| 24 | `Everguild/FX/Alpha Masks Two Layer` | 9 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 25 | `Everguild/FX/Spiral Trail FX` | 9 | 未映射·原版补充包兜底 | — |
| 26 | `Legacy Shaders/Particles/Alpha Blended Premultiply` | 9 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 27 | `Everguild/FX/TrailShader_1` | 8 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 28 | `Everguild/UnlitAmbient` | 8 | 自建替代（已映射） | WarpforgeVFX/UnlitAmbient |
| 29 | `Legacy Shaders/Particles/Anim Alpha Blended` | 8 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 30 | `UI/Additive` | 7 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 31 | `Universal Render Pipeline/Particles/Simple Lit` | 6 | 标准 shader（映射到自身） | Universal Render Pipeline/Particles/Simple Lit |
| 32 | `Everguild/FX/Glow Shader` | 5 | 未映射·原版补充包兜底 | — |
| 33 | `Everguild/Cards/Gem Crystal Glitter` | 4 | 未映射·原版补充包兜底 | — |
| 34 | `Everguild/FX/Burning` | 4 | 未映射·原版主包兜底 | — |
| 35 | `Everguild/FX/FX Shine For Animation` | 4 | 未映射·原版补充包兜底 | — |
| 36 | `Legacy Shaders/Particles/Alpha Blended` | 4 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 37 | `Shader Graphs/Fx_RockDissolve` | 4 | 未映射·原版补充包兜底 | — |
| 38 | `Everguild/FX/Burning Dissolve` | 3 | 未映射·原版补充包兜底 | — |
| 39 | `Everguild/FX/Specific/Necrons Rays` | 3 | 未映射·原版主包兜底 | — |
| 40 | `Everguild/Matcap/Matcap Full Options VAT` | 3 | 未映射·原版补充包兜底 | — |
| 41 | `Mobile/Particles/Multiply` | 3 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 42 | `Shader Graphs/Doomweaver effect` | 3 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 43 | `Shader Graphs/Eclipse Tau` | 3 | 未映射·原版主包兜底 | — |
| 44 | `TextMeshPro/Distance Field` | 3 | 未映射·工程/包内同名 | — |
| 45 | `Everguild/Cards/3D Card Explosion` | 2 | 未映射·原版补充包兜底 | — |
| 46 | `Everguild/Cards/BlobShadow` | 2 | 未映射·原版补充包兜底 | — |
| 47 | `Everguild/FX/MarkerLight` | 2 | 未映射·原版补充包兜底 | — |
| 48 | `Everguild/FX/TrailShader_Fading` | 2 | 自建替代（已映射） | WarpforgeVFX/Particles/Extra Color |
| 49 | `Everguild/Wind Matcap` | 2 | 未映射·原版补充包兜底 | — |
| 50 | `GlassRefraction` | 2 | 未映射·原版补充包兜底 | — |
| 51 | `Universal Render Pipeline/Particles/Lit` | 2 | 未映射·工程/包内同名 | — |
| 52 | `Custom/EditorIcon` | 1 | 未映射·原版补充包兜底 | — |
| 53 | `Everguild/Cards/3D Card` | 1 | 未映射·原版补充包兜底 | — |
| 54 | `Everguild/Cards/Card Swarm Effect` | 1 | 未映射·原版补充包兜底 | — |
| 55 | `Everguild/Cards/Gem Crystal Glitter Explosion` | 1 | 未映射·原版补充包兜底 | — |
| 56 | `Everguild/Cards/Necrons Base Death` | 1 | 未映射·原版补充包兜底 | — |
| 57 | `Everguild/Cards/Shatter Inner Pieces` | 1 | 未映射·原版补充包兜底 | — |
| 58 | `Everguild/FX/Card Highlight And Shadow` | 1 | 未映射·原版主包兜底 | — |
| 59 | `Everguild/FX/Card Remnant Death Icon` | 1 | 未映射·原版补充包兜底 | — |
| 60 | `Everguild/FX/Halo UV scroll` | 1 | 未映射·原版主包兜底 | — |
| 61 | `Everguild/FX/Particle Dissolve Premultiply` | 1 | 未映射·原版补充包兜底 | — |
| 62 | `Everguild/FX/Specific/Pray Glow` | 1 | 未映射·原版补充包兜底 | — |
| 63 | `Everguild/FX/Vortex` | 1 | 未映射·原版主包兜底 | — |
| 64 | `Everguild/Sprites/Sprite Additive` | 1 | 自建替代（已映射） | WarpforgeVFX/Sprites/Additive |
| 65 | `Everguild/UnlitAmbient Emissive Flickker` | 1 | 自建替代（已映射） | WarpforgeVFX/UnlitAmbient |
| 66 | `TextMeshPro/Distance Field Offset` | 1 | 🔴未映射且未解析 | — |
| 67 | `Universal Render Pipeline/Unlit` | 1 | 标准 shader（映射到自身） | Universal Render Pipeline/Unlit |

### ④ 真正要建属性表的：33 个（非标准、非工程自带、且 **连自建替代都没有**）

1. `Everguild/FX/Rays For Trail` —— 42 条效果（未映射·原版补充包兜底）
2. `Spine/Special/HiddenPass` —— 23 条效果（未映射·原版补充包兜底）
3. `Everguild/FX/Particle Premultiply` —— 12 条效果（未映射·原版补充包兜底）
4. `Shader Graphs/Fx_ParticleDissolve_apb` —— 11 条效果（未映射·原版补充包兜底）
5. `Everguild/FX/ShieldVfx` —— 10 条效果（未映射·原版补充包兜底）
6. `Everguild/FX/Spiral Trail FX` —— 9 条效果（未映射·原版补充包兜底）
7. `Everguild/FX/Glow Shader` —— 5 条效果（未映射·原版补充包兜底）
8. `Everguild/Cards/Gem Crystal Glitter` —— 4 条效果（未映射·原版补充包兜底）
9. `Everguild/FX/Burning` —— 4 条效果（未映射·原版主包兜底）
10. `Everguild/FX/FX Shine For Animation` —— 4 条效果（未映射·原版补充包兜底）
11. `Shader Graphs/Fx_RockDissolve` —— 4 条效果（未映射·原版补充包兜底）
12. `Everguild/FX/Burning Dissolve` —— 3 条效果（未映射·原版补充包兜底）
13. `Everguild/FX/Specific/Necrons Rays` —— 3 条效果（未映射·原版主包兜底）
14. `Everguild/Matcap/Matcap Full Options VAT` —— 3 条效果（未映射·原版补充包兜底）
15. `Shader Graphs/Eclipse Tau` —— 3 条效果（未映射·原版主包兜底）
16. `Everguild/Cards/3D Card Explosion` —— 2 条效果（未映射·原版补充包兜底）
17. `Everguild/Cards/BlobShadow` —— 2 条效果（未映射·原版补充包兜底）
18. `Everguild/FX/MarkerLight` —— 2 条效果（未映射·原版补充包兜底）
19. `Everguild/Wind Matcap` —— 2 条效果（未映射·原版补充包兜底）
20. `GlassRefraction` —— 2 条效果（未映射·原版补充包兜底）
21. `Custom/EditorIcon` —— 1 条效果（未映射·原版补充包兜底）
22. `Everguild/Cards/3D Card` —— 1 条效果（未映射·原版补充包兜底）
23. `Everguild/Cards/Card Swarm Effect` —— 1 条效果（未映射·原版补充包兜底）
24. `Everguild/Cards/Gem Crystal Glitter Explosion` —— 1 条效果（未映射·原版补充包兜底）
25. `Everguild/Cards/Necrons Base Death` —— 1 条效果（未映射·原版补充包兜底）
26. `Everguild/Cards/Shatter Inner Pieces` —— 1 条效果（未映射·原版补充包兜底）
27. `Everguild/FX/Card Highlight And Shadow` —— 1 条效果（未映射·原版主包兜底）
28. `Everguild/FX/Card Remnant Death Icon` —— 1 条效果（未映射·原版补充包兜底）
29. `Everguild/FX/Halo UV scroll` —— 1 条效果（未映射·原版主包兜底）
30. `Everguild/FX/Particle Dissolve Premultiply` —— 1 条效果（未映射·原版补充包兜底）
31. `Everguild/FX/Specific/Pray Glow` —— 1 条效果（未映射·原版补充包兜底）
32. `Everguild/FX/Vortex` —— 1 条效果（未映射·原版主包兜底）
33. `TextMeshPro/Distance Field Offset` —— 1 条效果（🔴未映射且未解析）

另有一档：**26 个已有自建替代**（`Everguild/FX/Extra Color` 等），其中 20 个是**近似**——若 ④ 要的是「原版属性表」而不是「能不能渲染」，这 26 个也该进清单（但属性表可由自建 shader 反推，优先级低）。标准/工程自带的 8 个不必建。


## 五、完整对账表（958 行）

> 机器可读版：`效果_shader_对账.tsv`（同 6 列）·「未映射明细」列的类别见 §三。

| 效果名 | 材质数 | 用到的原版 shader 列表 | 是否含未映射/未解析 shader | 近似替代处数 | 未映射明细 |
|---|---|---|---|---|---|
| Acid Rain Damage Target | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| AcidSpraySweepAttack | 12 | Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Air to Ground Big | 13 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Shader Graphs/Fx_ParticleDissolve_apb, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 9 | 原版补充包兜底 Shader Graphs/Fx_ParticleDissolve_apb；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| AlphaWarriorEffect | 9 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| AmbushEffect | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Antimatter Explosion | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Arco-Flagellant Strike | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| ArdAsNailsEffect | 8 | Everguild/Sprites/Sprite Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Armor_Necron | 4 | Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| ArmourEffect | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| ArmourEffect_Bladeguard_Shield | 10 | Everguild/FX/Extra Color, Everguild/FX/ShieldVfx, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/ShieldVfx |
| ArmourEffect_Nurgle | 13 | Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| ArtificeEffect | 8 | Everguild/FX/Extra Color, Particles/Standard Unlit, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Artillery Ground | 12 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Shader Graphs/Fx_ParticleDissolve_apb, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Shader Graphs/Fx_ParticleDissolve_apb；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Artillery Manticore down 1x | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Artillery Manticore down 2x spread | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Artillery Manticore down 4x | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 8 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Artillery Manticore down 4x spread | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 8 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Artillery Manticore up 2x | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Artillery Manticore up 4x | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 12 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Atk_BulletImpact_Vox-pulse | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Atk_BulletImpact_Vox-pulse digital | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Atk_BulletImpact_Vox-pulse purple | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Atk_ChemVial_2 | 14 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap Full Options, GlassRefraction, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/Rays For Trail；原版补充包兜底 GlassRefraction |
| Atk_ExileGlaiveThrow Movement | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Rays For Trail, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 5 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| Atk_GrotGrenade | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Rays For Trail, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| Atk_GrotGrenade_big | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Particle Premultiply Greyscale Coloring, Everguild/FX/Rays For Trail, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 4 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| Atk_GrotRock | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| Atk_HelspearThrow Movement | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Particle Premultiply Greyscale Coloring, Everguild/FX/Rays For Trail, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 5 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| Atk_JainasMor | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Atk_KrootJavelinThrow Movement | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| Atk_Rokkit-Harpoon big | 20 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Particle Premultiply, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Everguild/FX/Particle Premultiply |
| Atk_Rokkit-Harpoon small | 19 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Particle Premultiply, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Everguild/FX/Particle Premultiply |
| Atk_StikkaThrow Movement | 12 | Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Particle Premultiply Greyscale Coloring, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap Full Options VAT, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 5 | 原版补充包兜底 Everguild/FX/Rays For Trail；原版补充包兜底 Everguild/Matcap/Matcap Full Options VAT |
| Attack_Stomp | 6 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| AttackHitSmall | 2 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Premultiply | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/Particle Dissolve Premultiply |
| AttackSquigEffect | 3 | Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Simple Lit, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Aura_WhileInPlay_1 | 4 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Aura_WhileInPlay_2 | 6 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Axe_Slash_User_SW | 7 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/ShieldVfx, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/ShieldVfx |
| BacklashEffect | 4 | Everguild/FX/Particle Dissolve Mask, Everguild/FX/Particle Premultiply Greyscale Coloring, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Backstab Dagger | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Backstab Dagger Intense | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BansheeHowl | 2 | Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BansheeHowl_blood | 3 | Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BansheeHowl_blood glow | 4 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BansheeHowl_warlord | 2 | Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_BL | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_DA | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_DA_Intense | 9 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_DA_Intense_Secret | 11 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_DA_NoIcon | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_GSC | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_GSC_intense | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_GSC_Jackal | 13 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_GSC_shake | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_Guard | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_Guard Board | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_Guard Digital | 7 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Basic_army_Guard Intense | 9 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_Guard_quick | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_Guard_quick Master of Ordnance | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_UM | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_UM_Chainsword | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_UM_Chant | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_UM_chaplain | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_UM_Guilliman | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Basic_army_UM_Hammer | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Basic_army_UM_Pre-Effect | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Basic_army_UM_quick | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BeastSnagga_Summon | 8 | Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BigKrumpaKlawEffect | 10 | Mobile/Particles/Alpha Blended, Mobile/Particles/Multiply, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Birth of a Saga | 6 | Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| BlackCrusade_Battleground | 7 | Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| BlackCrusade_Battleground repeat | 8 | Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Blacksword_Missile_offscreen | 22 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Blacksword_Missile_volley | 19 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Blast_Big_Ork | 11 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 8 |  |
| Blast_Big_Ork_Missiles | 16 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Blast_Plasma | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Blast_Plasma_Helbrute | 22 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Blast_Plasma_MegaBlasta | 21 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Blast_Plasma_Sentinel | 23 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Blast_Plasma_Sentinel_twinlink | 23 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BlastEffect | 7 | Shader Graphs/Fx_ParticleDissolve_apb, Shader Graphs/Fx_RockDissolve, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Shader Graphs/Fx_ParticleDissolve_apb；原版补充包兜底 Shader Graphs/Fx_RockDissolve |
| Blind_continuous_effect | 3 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| BlindEffect | 7 | Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BloodThirstEffect | 7 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 5 |  |
| BloodThirstEffect_continuous | 5 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| BloodThirstEffect_Gain | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| BloodThirstEffect_Idle | 5 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| BloodThirstEffect_Lose | 5 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| BloodySplash | 3 | Mobile/Particles/Multiply, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| BlueSummonCircle | 7 | Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Bolter Casing_Imperial | 1 | Everguild/Matcap/Matcap With Texture | 无 |  |  |
| Bolter Casing_Orks | 1 | Everguild/Matcap/Matcap With Texture | 无 |  |  |
| Bolter Casing_Traitor | 1 | Everguild/Matcap/Matcap With Texture | 无 |  |  |
| BolterSweep | 10 | Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 5 |  |
| BolterSweep_2 | 14 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Bore Through | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Bore Through Intense | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff EC On Warlord When Creating Card | 12 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff EC Pink Twirl | 13 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff EC Pink Twirl Intense | 15 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff EC Stimulate | 13 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff EC Stimulate passive | 4 | Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_BL_Intense | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Blue | 4 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Blue_SW | 5 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Buff_Blue_SW_Intense | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Buff_Blue_SW_Wolf | 5 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Buff_Blue_Tau | 6 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Buff_Blue_Tau_quick | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Chaos_Star | 10 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_DA_Forest_Self | 7 | Everguild/FX/Extra Color, Everguild/FX/Spiral Trail FX, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Spiral Trail FX |
| Buff_DA_Forest_Target | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_DaemonicFrenzy | 7 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_DaemonicPact | 9 | Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_DaemonicPact_troop | 5 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_DarkOratory | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_DiabolicStrength | 6 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Drach'nyen | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Eldar_Warp Spider | 10 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Eldar_Warp Spider_intense | 9 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Buff_EnemyHand_GSC | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Flies | 3 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_FriendlyHand_GSC | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Green | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Green_Agenda | 9 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Green_DA | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Green_Intense | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_GSC | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Hand_1 | 3 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Hand_UM | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_HideousMutation | 8 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_HideousMutation_EC | 7 | Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_HideousMutation_rage | 8 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_LegacyOfVengeance | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Mana | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Mana_2 | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Mana_3 Holy | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Mana_Blood | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_MarkOfKhorne | 5 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_MarkOfNurgle | 9 | Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Buff_MarkOfSlaanesh | 12 | Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_MarkOfTzeentch | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Orange | 4 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Ork_Mek | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Buff_Purple | 5 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Red | 4 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Red_Intense | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Red_ork | 5 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Buff_Sautekh_Intense | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Sororitas | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Sororitas_Intense | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Sororitas_quick | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Sororitas_quick_fire | 7 | Everguild/FX/Alpha Masks Two Layer, Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 8 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Sororitas_quick_fire_intense | 7 | Everguild/FX/Alpha Masks Two Layer, Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 9 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_SW_Blizzard | 8 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_SW_Blizzard_Board | 8 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_SW_Lightning | 10 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_SW_Lightning_quick | 9 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_SW_Rune_Blizzard | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_SW_Rune_Lightning | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_SW_Rune_Tornado | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_SW_Runes 1 | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_SW_Runes_Blizzard OLD | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Tau_Air | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Tau_Earth | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Tau_Fire | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Tau_GrislyFeast | 5 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Tau_Intense | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Tau_Kroot | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Tau_Kroot_FriendlyBoard | 6 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Tau_Kroot_intense | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Buff_Tau_Water | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Tyranid | 7 | Everguild/Matcap/Matcap With Texture, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Tyranid Armor | 9 | Everguild/Matcap/Matcap With Texture, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Tyranid Atk | 7 | Everguild/Matcap/Matcap With Texture, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_Tyranid HP | 7 | Everguild/Matcap/Matcap With Texture, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Buff_UM_Intense | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_1hit_trail_ork | 13 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 7 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_1shot | 3 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BulletImpact_1shot_pointblank | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 5 |  |
| BulletImpact_1shot_random_chaos | 14 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 7 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_1shot_random_EC | 13 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 7 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_1shot_trail | 12 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_1shot_trail_EC Daemon | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_1shotSniper | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_1shotSniper_trail | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 7 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_2shot | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_2shot_autocannon | 15 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 12 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_2shot_ork | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_2shot_pointblank_chaos | 6 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_2shot_pointblank_EC | 6 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_2shot_Titus_AA | 11 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_2shot_trail | 14 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_2shot_trail_chaos | 13 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 8 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_2shot_trail_EC | 13 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 8 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_2shot_trail_EC Daemon | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_2shot_trail_nurgle | 14 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 8 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_2shot_trail_ork_NEW | 13 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 10 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_2shot_trail_twinlink | 14 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 13 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_3shot | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_3shot_pointblank | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 5 |  |
| BulletImpact_3shot_trail | 14 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_3shot_trail_chaos | 13 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 8 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_3shot_trail_EC | 13 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 8 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_3shot_trail_ork | 13 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 10 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_4shot | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_4shot_trail | 14 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_4shot_trail_chaos | 13 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 8 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_4shot_trail_EC | 13 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 8 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_4shot_trail_nurgle | 14 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 8 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_4shot_trail_twinlink | 14 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 12 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_5shot | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_5shot_trail | 14 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_5shot_trail_chaos | 13 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 8 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_5shot_trail_EC | 13 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 8 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_5shot_trail_ork | 13 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 10 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_5shot_trail_ork_low | 13 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 10 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_6shot_autocannon_EC | 16 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Legacy Shaders/Particles/Alpha Blended Premultiply, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 16 |  |
| BulletImpact_6shot_trail_EC Daemon | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_9shot | 5 | Legacy Shaders/Particles/Alpha Blended Premultiply, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| BulletImpact_9shot_trail | 17 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Legacy Shaders/Particles/Alpha Blended Premultiply, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 7 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_9shot_trail_chaos | 15 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Legacy Shaders/Particles/Alpha Blended Premultiply, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 9 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_9shot_trail_EC | 15 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Legacy Shaders/Particles/Alpha Blended Premultiply, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 9 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_9shot_trail_EC_twinlink | 14 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Legacy Shaders/Particles/Alpha Blended Premultiply, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 16 |  |
| BulletImpact_Abaddon_AA_StormBolter | 17 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 11 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_AcidSpit | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BulletImpact_artillery_1shot | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_artillery_arc | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_artillery_big | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_artillery_manticore | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 9 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_artillery_manticore_spread | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autocannon | 15 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autocannon_Firestrike | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply Greyscale Coloring, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 10 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autocannon_Hydra | 14 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply Greyscale Coloring, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 28 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autocannon_rapid | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply Greyscale Coloring, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autogun_1shot | 12 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autogun_1shotSniper | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 7 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autogun_2shot | 12 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autogun_3shot | 12 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autogun_4shot | 12 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autogun_6shot | 12 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autogun_6shot_twinlink | 12 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 12 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autogun_8shot | 12 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autogun_Automatic | 12 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autogun_liberator_1shot | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 7 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autogun_liberator_repeat | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 14 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_autogun_Shotgun | 12 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Bolter_Large Area | 14 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Bolter_Wide Area | 14 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_BulletHole | 3 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BulletImpact_Chaos_Melta | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_ChaosMissile | 20 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 7 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_ChemVial | 12 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, GlassRefraction, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 GlassRefraction |
| BulletImpact_Combi_Flamer_Infernus | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_DA_MissileVolley_Ballistus | 21 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Unlit UV scroll, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_DCannon | 14 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 6 |  |
| BulletImpact_DCannon_2 | 14 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_DCannon_2_twinlink | 14 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 10 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_DCannon_twinlink | 14 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 12 |  |
| BulletImpact_DeathSpinner | 11 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 7 |  |
| BulletImpact_DeathSpinner Test | 6 | Everguild/FX/TrailShader_1, Universal Render Pipeline/Particles/Unlit | 无 | 10 |  |
| BulletImpact_deathspinner_arc | 15 | Everguild/FX/Extra Color, Everguild/FX/TrailShader_1, Mobile/Particles/Additive, Shader Graphs/Doomweaver effect, Universal Render Pipeline/Particles/Unlit | 无 | 18 |  |
| BulletImpact_deathspinner_arc_alt | 13 | Everguild/FX/Extra Color, Everguild/FX/TrailShader_1, Mobile/Particles/Additive, Shader Graphs/Doomweaver effect, Universal Render Pipeline/Particles/Unlit | 无 | 16 |  |
| BulletImpact_DeathSpinner_spider | 12 | Everguild/FX/Extra Color, Mobile/Particles/Additive, UI/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_DeathSpinner_spider_twinlink | 12 | Everguild/FX/Extra Color, Mobile/Particles/Additive, UI/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 12 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_DeathSpinner_twinlink | 11 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 12 |  |
| BulletImpact_DeathSpinner_twinlink_large | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 10 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_DeathSpinner_twinlink_large parabolic | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_DesolationMissile | 18 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 7 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_DesolationMissile_AOE | 18 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 14 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_DynamiteBundle_Throw | 11 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_Eldar_Lasblaster | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Flamethrower_ClearanceIncinerator | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BulletImpact_Flamethrower_HandFlamer | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| BulletImpact_FlamethrowerSororitas | 9 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| BulletImpact_FlamethrowerSororitas_long | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| BulletImpact_FlamethrowerSororitas_twinlink | 9 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| BulletImpact_FusionGun | 14 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_gatling_1s | 13 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_gatling_1s_invader | 13 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Ghazghkull_AA | 15 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 17 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Gordrang | 13 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 9 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Grenade_ThumpGun | 15 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 5 |  |
| BulletImpact_GrenadeLauncher | 15 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 5 |  |
| BulletImpact_GrenadierGauntlet | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| BulletImpact_Kroot_KrootGun OLD | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Kroot_KrootPistol | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Kroot_KrootRifle | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Kroot_KrootScattergun | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Kroot_RepeaterCannon | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Kroot_RepeaterCannon OLD | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Lascannon | 17 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply Greyscale Coloring, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Lascannon_2shots | 17 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply Greyscale Coloring, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Lascannon_twinlink | 17 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply Greyscale Coloring, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 10 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Lascannon_twinlink_dread | 17 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply Greyscale Coloring, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 10 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_LongRifle_trail | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_MagicMissile_EC | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_MagicMissile_EC_Fire | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_MissileToTarget | 17 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 7 |  |
| BulletImpact_MissileToTarget_Orks | 17 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 7 |  |
| BulletImpact_Morgrim | 13 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 9 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Needle gun_1shot | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_noisemarine | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 5 |  |
| BulletImpact_Ork Heavy Lobba | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Particle Premultiply, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/Particle Premultiply |
| BulletImpact_Ork Missile Big | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/Particle Premultiply |
| BulletImpact_Ork Missiles 1 | 18 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 7 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Ork Missiles 2 | 18 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Ork Snakebite Squig Hook | 16 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 7 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Psychophage | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| BulletImpact_Pyrovore | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply, Everguild/FX/Rays For Trail, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/Particle Premultiply；原版补充包兜底 Everguild/FX/Rays For Trail；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Pyrovore OLD | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply, Everguild/FX/Rays For Trail, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/Particle Premultiply；原版补充包兜底 Everguild/FX/Rays For Trail；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_RandomShitGo | 35 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Particle Premultiply, Everguild/FX/Particle Premultiply Greyscale Coloring, Everguild/Matcap/Matcap Full Options, Everguild/Matcap/Matcap Full Options VAT, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 17 | 原版补充包兜底 Everguild/FX/Particle Premultiply；原版补充包兜底 Everguild/Matcap/Matcap Full Options VAT；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_RiftCannon | 13 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| BulletImpact_Rippergun | 13 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Saboteur_DetonatorOnly | 4 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BulletImpact_SaboteurExplosive | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_Screamer-Killer | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BulletImpact_Shuriken_Long | 12 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 8 |  |
| BulletImpact_Shuriken_Long_twinlink | 12 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 16 |  |
| BulletImpact_Shuriken_Pistol | 11 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 8 |  |
| BulletImpact_Shuriken_Short | 11 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 8 |  |
| BulletImpact_Shuriken_Short_twinlink | 11 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 16 |  |
| BulletImpact_Sonic Weapon 1 | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 5 |  |
| BulletImpact_Sonic Weapon 1 Dual Pistol | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 10 |  |
| BulletImpact_Sonic Weapon 1 Dual Pistol OLD | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 10 |  |
| BulletImpact_Sonic Weapon 2 | 20 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Sonic Weapon 2 Pistol | 20 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/FX/Distortion, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/FX/Distortion；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Sonic Weapon 2 Twinlink | 22 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/FX/Distortion, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 7 | 工程/包内同名 WarpforgeVFX/FX/Distortion；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Sonic Weapon 2 variant | 20 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Sororitas_Melta | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/FX/Distortion, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/FX/Distortion；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Sororitas_MissileVolley | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Spiral Trail FX, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 5 | 原版补充包兜底 Everguild/FX/Spiral Trail FX |
| BulletImpact_Sororitas_MissileVolley_board_down | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Spiral Trail FX, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 4 | 原版补充包兜底 Everguild/FX/Spiral Trail FX |
| BulletImpact_Sororitas_MissileVolley_board_up | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Spiral Trail FX, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 5 | 原版补充包兜底 Everguild/FX/Spiral Trail FX |
| BulletImpact_Sororitas_MissileVolley_MoveAnim | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Spiral Trail FX, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 9 | 原版补充包兜底 Everguild/FX/Spiral Trail FX |
| BulletImpact_Sororitas_MultiMelta | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/FX/Distortion, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/FX/Distortion；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Sororitas_MultiMelta_Junith | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/FX/Distortion, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/FX/Distortion；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Sororitas_SaintCelestine | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/FX/Distortion | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/FX/Distortion |
| BulletImpact_Spore Launch | 17 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Unlit UV scroll, Everguild/Wind Matcap, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/Wind Matcap |
| BulletImpact_Spore Launch 1 | 17 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Unlit UV scroll, Everguild/Wind Matcap, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/Wind Matcap |
| BulletImpact_stormbolter_4shots | 14 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 12 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_stormbolter_4shots_Logan_AA | 11 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 12 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_stormbolter_6shots | 14 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 12 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_tank_baneblade | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_tank_big | 14 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_tank_huge | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_tank_small | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Burst Cannon | 13 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Burst Cannon_short | 13 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_FusionBlaster | 12 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 4 | 原版补充包兜底 Spine/Special/HiddenPass |
| BulletImpact_Tau_FusionBlaster Dual | 12 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 8 | 原版补充包兜底 Spine/Special/HiddenPass |
| BulletImpact_Tau_GravPulse | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| BulletImpact_Tau_IonAccelerator | 20 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 14 | 原版补充包兜底 Spine/Special/HiddenPass |
| BulletImpact_Tau_Missile 10x | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Spiral Trail FX, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Everguild/FX/Spiral Trail FX |
| BulletImpact_Tau_Missile 3x | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Spiral Trail FX, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Everguild/FX/Spiral Trail FX |
| BulletImpact_Tau_Plasma Cannon | 19 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Pulse Blaster | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply Greyscale Coloring, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Pulse Bomb | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Pulse Carbine | 10 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Pulse Carbine Dual | 10 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Pulse Carbine Dual OLD | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply Greyscale Coloring, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Pulse Carbine OLD | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply Greyscale Coloring, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Pulse Drone Dual | 10 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Pulse Pistol | 10 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Pulse Rifle | 10 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Pulse Rifle Dual | 10 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_PulseARCcannon | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Rail Rifle | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Tau_Sniper Pulse Rifle | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpact_Thunderfire | 14 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_Tyranid_Barblauncher | 11 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_Tyranid_Big | 12 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_Tyranid_Cannon | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Rays For Trail, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_Tyranid_Small | 12 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_Tyranid_Small_Dual | 12 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 12 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_Tyranid_Stranglethorn | 13 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/FX/Unlit UV scroll, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 7 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_Tyranid_Stranglethorn_Dual | 12 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Everguild/FX/Unlit UV scroll, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 16 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| BulletImpact_ValkyrieMissile | 14 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| BulletImpact_Webber | 12 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletImpactBurst_Blood | 3 | Legacy Shaders/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| BulletImpactBurst_crowd | 3 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| BulletTrail | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Shader Graphs/Fx_ParticleDissolve_apb, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 10 | 原版补充包兜底 Shader Graphs/Fx_ParticleDissolve_apb |
| BulletTrail_Single | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Shader Graphs/Fx_ParticleDissolve_apb, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Shader Graphs/Fx_ParticleDissolve_apb |
| BulletTrail_Stormraven | 10 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| BulletTrail_Stormraven_Single | 10 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| CamouflageEffect | 4 | Everguild/FX/Unlit UV scroll, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| CamouflageEffect New | 5 | Everguild/FX/Extra Color, Everguild/FX/Unlit UV scroll, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| CantAttackEffect | 6 | Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Card_Buff_Hand | 3 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Card_Buff_Hand_Ork | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Card_Buff_Hand_SW | 5 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Card_Buff_Sabotage_trigger | 3 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Card_Buff_Tyranid | 2 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| CardDisappear | 5 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| CardPrefab | 11 | Everguild/Cards/3D Card, Everguild/Cards/BlobShadow, Everguild/FX/Card Highlight And Shadow, Everguild/FX/Halo UV scroll, Sprites/Default, TextMeshPro/Distance Field, TextMeshPro/Distance Field Offset, Universal Render Pipeline/Particles/Unlit | 是 · 含🔴未解析 | 18 | 原版补充包兜底 Everguild/Cards/3D Card；原版补充包兜底 Everguild/Cards/BlobShadow；原版主包兜底 Everguild/FX/Card Highlight And Shadow；原版主包兜底 Everguild/FX/Halo UV scroll；工程/包内同名 TextMeshPro/Distance Field；🔴未映射且未解析 TextMeshPro/Distance Field Offset |
| Chaos_Destroy_Brutal | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Alpha Blended, Mobile/Particles/Alpha Blended, Particles/Standard Unlit, Universal Render Pipeline/Particles/Simple Lit, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Chronomancer_Buff | 5 | Everguild/FX/Extra Color, Everguild/FX/TrailShader_1, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| CodexEffect | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| CodexIdleEffect | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| ComboEffect | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| CompanionEffect | 9 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Particle Shine Custom Vertex Streams, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| ConcussiveEffect | 9 | Mobile/Particles/Additive, Shader Graphs/Fx_ParticleDissolve_apb, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Shader Graphs/Fx_ParticleDissolve_apb |
| CoordinatedEngagement | 22 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Cosmic Serpent | 12 | Everguild/Cards/Gem Crystal Glitter, Everguild/FX/Extra Color, Everguild/FX/Unlit UV scroll, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/Cards/Gem Crystal Glitter；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Cosmic Serpent_Secondary | 12 | Everguild/Cards/Gem Crystal Glitter, Everguild/FX/Extra Color, Everguild/FX/Unlit UV scroll, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/Cards/Gem Crystal Glitter；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| CreateCard | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| CreateCard BlackLegion | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| CreateCard DA | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Everguild/FX/Spiral Trail FX, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/Spiral Trail FX |
| CreateCard EC | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| CreateCard EC Elixir | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| CreateCard Gold | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| CreateCard GSC | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| CreateCard GSC 5x | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit | 无 | 5 |  |
| CreateCard Necron | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| CreateCard Orks | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| CreateCard Orks 1 | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| CreateCard Sabotage | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| CreateCard_Tau | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| CrueltyEffect | 8 | Everguild/FX/Particle Shine Custom Vertex Streams, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Cryptek_Stun | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Cut Axe SW | 6 | Everguild/FX/Extra Color, Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Cut EC | 6 | Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Cut EC 3x | 6 | Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Cut EC Reverse | 6 | Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Cut EC sword 7x warlord | 6 | Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit | 无 | 8 |  |
| Cut Termie Lightning Claws | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit | 无 | 16 |  |
| Cut Wulfen SW | 6 | Everguild/FX/Extra Color, Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Cut Wulfen SW Double | 6 | Everguild/FX/Extra Color, Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit | 无 | 12 |  |
| Cut Wulfen SW Reverse | 6 | Everguild/FX/Extra Color, Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Cuts Brutal | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Cuts Brutal_Warp Spider | 4 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| DaIrongobEffect | 9 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Death Cult Strike | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Deathspinner Cut Effect | 1 | Shader Graphs/Doomweaver effect | 无 | 1 |  |
| Debuff EC | 11 | Everguild/FX/Particle Shine Custom Vertex Streams, Particles/Standard Unlit, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Debuff_Mana_Tyranid | 6 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| DeckBuff Grey | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| DeckBuff_BlackLegion | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| DeckBuff_Green | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| DeckBuff_Green 2 | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| DeckBuff_GSC | 4 | UI/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| DeckBuff_MoveToTop | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| DeckBuff_Orks | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| DeckBuff_SW | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| DeckBuff_Tau | 4 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| DeckBuff1 | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Default Tap Floor | 3 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Delightful Agonies Launch Projectile | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Unlit UV scroll, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Delightful_Agonies_Projectile | 8 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Duty Reset | 8 | Everguild/FX/Extra Color, Everguild/FX/FX Shine For Animation, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/FX Shine For Animation |
| DutyEffect | 7 | Everguild/FX/Extra Color, Everguild/FX/FX Shine For Animation, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/FX Shine For Animation |
| EarthCrack_Single | 3 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Earthquake Rockfall Board | 5 | Everguild/Matcap/Matcap Full Options, Legacy Shaders/Particles/Anim Alpha Blended, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Earthquake Rockfall Target | 4 | Everguild/Matcap/Matcap Full Options, Legacy Shaders/Particles/Anim Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| EC Heldrake Attack | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply, Everguild/Matcap/Matcap Full Options, UI/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/Particle Premultiply；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| EC Heldrake Rally | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply, Everguild/Matcap/Matcap Full Options, UI/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/Particle Premultiply；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| EC Sword Cut Board DMC Style | 1 | Everguild/FX/Rays For Trail | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| EC Target | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Particles/Standard Unlit, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| EC_Sonic Shock | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| EC_Sonic Stomp | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| EC_Target_user | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| EC_Target_user Lucius | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| EcstasyEffect | 10 | Everguild/FX/Particle Shine Custom Vertex Streams, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Emperors Children Generic Buff | 10 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Emperors Children Generic Buff Record | 10 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Emperors Children Scenario Bombardment | 9 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Emperors Children Summon | 14 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Environmental Condition Astra Militarum Dawn Attack | 6 | Everguild/FX/Extra Color, Everguild/UnlitAmbient, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Environmental Condition Astra Militarum Factory Overdrive | 7 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Environmental Condition Astra Militarum Planetary Invasion | 25 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Spiral Trail FX, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 12 | 原版补充包兜底 Everguild/FX/Spiral Trail FX；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Environmental Condition Black Legion Daemonic Feast | 23 | Everguild/FX/Alpha Masks Two Layer, Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Environmental Condition Black Legion Helfire Outburst | 16 | Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Particle Premultiply Greyscale Coloring, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Environmental Condition Black Legion Warp Storm | 27 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Environmental Condition Dark Angels Asteroid Zone | 10 | Everguild/FX/Alpha Masks Two Layer, Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Environmental Condition Dark Angels Orbiting | 4 | Everguild/FX/Extra Color, Everguild/UnlitAmbient, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Environmental Condition Dark Angels Void Combat | 18 | Everguild/FX/Alpha Masks Two Layer, Everguild/FX/Extra Color, Everguild/UnlitAmbient, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 17 | 原版补充包兜底 Spine/Special/HiddenPass；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Environmental Condition Emperor's Children 1 Green | 11 | Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Environmental Condition Emperor's Children 1 Pink REJECTED | 10 | Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Environmental Condition Emperor's Children 2 Fumes | 9 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Environmental Condition Emperor's Children Empyric Rift | 18 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Environmental Condition Emperor's Children Empyric Rift OLD | 19 | Everguild/FX/Extra Color, Everguild/FX/Unlit UV scroll, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Environmental Condition GSC Mining Tremors | 18 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Legacy Shaders/Particles/Anim Alpha Blended, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 10 |  |
| Environmental Condition GSC Sump Overspill | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Lit, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 Universal Render Pipeline/Particles/Lit |
| Environmental Condition GSC Sump Overspill OLD | 10 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Lit, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 |  | 工程/包内同名 Universal Render Pipeline/Particles/Lit |
| Environmental Condition GSC Toxic Fumes | 10 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Environmental Condition Necrons Earthquake | 23 | Everguild/FX/Extra Color, Everguild/FX/Specific/Necrons Rays, Everguild/Matcap/Matcap Full Options, Legacy Shaders/Particles/Anim Alpha Blended, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 9 | 原版主包兜底 Everguild/FX/Specific/Necrons Rays；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Environmental Condition Necrons Immortal Beams | 20 | Everguild/FX/Extra Color, Everguild/FX/Specific/Necrons Rays, Mobile/Particles/Additive, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 原版主包兜底 Everguild/FX/Specific/Necrons Rays；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Environmental Condition Necrons Solar Storm | 21 | Everguild/FX/Extra Color, Everguild/FX/Specific/Necrons Rays, Everguild/FX/Unlit UV scroll, Mobile/Particles/Additive, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 原版主包兜底 Everguild/FX/Specific/Necrons Rays；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Environmental Condition Space Wolves Everstorm | 15 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Environmental Condition Space Wolves First Light | 16 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Everguild/UnlitAmbient, Universal Render Pipeline/Particles/Unlit | 无 | 12 |  |
| Environmental Condition Space Wolves Full Moon | 9 | Everguild/FX/Alpha Mask One Layer, Everguild/UnlitAmbient, Universal Render Pipeline/Particles/Unlit | 无 | 5 |  |
| EnvironmentalCondition Leviathan Acid Rain | 12 | Everguild/FX/Unlit UV scroll, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| EnvironmentalCondition Leviathan Blazing Biomatter | 11 | Everguild/FX/Unlit UV scroll, Everguild/UnlitAmbient, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| EnvironmentalCondition Leviathan Sweeping Infestation | 12 | Everguild/FX/Unlit UV scroll, Everguild/UnlitAmbient, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| EnvironmentalCondition Saim Hann Blackout | 9 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| EnvironmentalCondition Saim Hann Infinity Circuit Overload | 15 | Everguild/FX/Alpha Masks Two Layer, Everguild/FX/Extra Color, Everguild/FX/Vortex, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 原版主包兜底 Everguild/FX/Vortex；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| EnvironmentalCondition Saim Hann Webway Rift | 24 | Everguild/FX/Alpha Masks Two Layer, Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, Universal Render Pipeline/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| EnvironmentalCondition Sororitas Disrupted Ceremony | 18 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Everguild/UnlitAmbient Emissive Flickker, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| EnvironmentalCondition Sororitas Raging Storm | 5 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| EnvironmentalCondition Sororitas Shrine Bombardment | 17 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Legacy Shaders/Particles/Anim Alpha Blended, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 8 |  |
| EnvironmentalCondition Tau Electro-Static Interference | 14 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Shader Graphs/Eclipse Tau, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 原版主包兜底 Shader Graphs/Eclipse Tau；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| EnvironmentalCondition Tau Radiation Storm | 19 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Alpha Blended, Shader Graphs/Eclipse Tau, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 原版主包兜底 Shader Graphs/Eclipse Tau；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| EnvironmentalCondition Tau Solar Eclipse | 4 | Everguild/FX/Extra Color, Shader Graphs/Eclipse Tau, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版主包兜底 Shader Graphs/Eclipse Tau |
| EnvironmentalCondition Ultramarines Aerial Clash | 33 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Everguild/UnlitAmbient, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Shader Graphs/Fx_ParticleDissolve_apb, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 7 | 原版补充包兜底 Shader Graphs/Fx_ParticleDissolve_apb；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| EnvironmentalCondition Ultramarines Bombardment | 20 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Rays For Trail, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| EnvironmentalCondition Ultramarines Thunderstorm | 9 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Execution_BlackLegion | 10 | Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Explosion Fenrisian Monstrosities | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Explosion Fenrisian Monstrosities Extra Damage | 9 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Explosion Hand UI | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Explosion Hand UI 0,5s delay | 5 | Everguild/FX/Extra Color, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Explosion Hive Fleet Arrival Explosions only | 20 | Everguild/FX/Burning, Everguild/FX/Burning Dissolve, Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 16 | 原版主包兜底 Everguild/FX/Burning；原版补充包兜底 Everguild/FX/Burning Dissolve |
| Explosion Hive Fleet Arrival Tendrils | 17 | Everguild/FX/Burning, Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版主包兜底 Everguild/FX/Burning |
| Explosion Hive Fleet Arrival Tendrils OLD | 17 | Everguild/FX/Burning, Everguild/FX/Burning Dissolve, Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 5 | 原版主包兜底 Everguild/FX/Burning；原版补充包兜底 Everguild/FX/Burning Dissolve |
| Explosion_BombQuick | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Explosion_BombQuick_triple | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Explosion_ConcealedExplosives | 9 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 15 |  |
| Explosion_Debris | 5 | Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Explosion_Debris_UI | 5 | Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Explosion_gauss | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Explosion_gauss_yellow | 6 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Explosion_Ground | 1 | Everguild/FX/Particle Premultiply | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Particle Premultiply |
| Explosion_GroundBreak | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Explosion_Possession | 7 | Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Explosion_Short | 4 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Factory Overdrive Damage | 10 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Faith UI Gain | 5 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Faith UI Trigger | 5 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Faith_trigger_unit | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| FastEffect | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| FeedingFrenzy | 10 | Universal Render Pipeline/Particles/Simple Lit, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Ferocity_RecallToDeck | 9 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| FerocityEffect | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Fire_Board | 4 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| FlameBurst_Sororitas | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| FlamethrowerBurna | 9 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| FlamethrowerSingleAttack_1 | 7 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| FlamethrowerSingleAttack_2 | 6 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| FlamethrowerSweepAttack_1 | 4 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| FlamethrowerSweepAttack_sororitas | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| FlankEffect | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Flayed One Summon | 4 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| FlyingEffect | 7 | Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Forewarned_Board | 3 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| FrenziedEffect | 5 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Gain_Sentry | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| GainShuriken | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| GatlingSweep | 13 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap With Texture, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Generic Tap Material | 2 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Godspear Warhead Full | 27 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Alpha Masks Two Layer, Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Goff_GreatestWarboss | 7 | Everguild/Matcap/Matcap Full Options, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Goff_ProperKilly | 8 | Everguild/FX/Glow Shader, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 11 | 原版补充包兜底 Everguild/FX/Glow Shader |
| Goff_ProphetOfDaWaaagh | 6 | Everguild/Matcap/Matcap Full Options, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Goff_Rok_Invasion | 4 | Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Mobile/Particles/Multiply, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Goff_UnbridledCarnageEffect | 7 | Universal Render Pipeline/Particles/Simple Lit, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Goff_Waaagh | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Goff_WaaaghRed | 3 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Goff_WaaaghRed_skulls | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| GreenSummonCircle | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| GSC RecallToHand | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| GSC Rockfall | 6 | Everguild/Matcap/Matcap Full Options, Legacy Shaders/Particles/Anim Alpha Blended, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 6 |  |
| GSC Sabotage Use | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| GSC Summon | 4 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| GuardSummon | 2 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Hammer_Slam_SW | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/ShieldVfx, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/ShieldVfx；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Hammer_Throw_SW | 14 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/ShieldVfx, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/ShieldVfx |
| Healing_Circles | 3 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Healing_Projectile | 5 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Healing_Projectile_Frost | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Healing_Projectile_Green | 5 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Healing_Projectile_Ork | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| HeldrakeStrike | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Particle Premultiply；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Helfire Burst Target | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Helspear_Assault_Buff | 8 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| HeroOfTheEmpire OLD | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| HeroRespiteEffect | 6 | Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Hexmark_Artifice_Deal1 | 9 | Everguild/FX/Extra Color, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Spine/Special/HiddenPass；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Hive Fleet Ground Growth | 7 | Everguild/FX/Extra Color, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Hunta Rig Harpoon Hand | 12 | Everguild/FX/Unlit UV scroll, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 5 |  |
| HuntMark_DamageEffect | 23 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 7 |  |
| HuntMark_EndEffect | 9 | Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| HuntMark_IdleEffect | 11 | Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Sprites/Default, Sprites/Mask, TextMeshPro/Distance Field, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 TextMeshPro/Distance Field |
| HuntMark_StackEffect | 9 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| HuntMark_StartEffect | 12 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| HuntMark_User | 5 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Immortal Beam | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Impact_blunt_lightning | 7 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Impact_blunt_lightning_Big | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Impact_blunt_lightning_Big_Recoil | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Impact_Ground_Stomp | 6 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Impact_Ground_Stomp_Big | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Infernal_Gaze | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Infinity Circuit | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| InspiredRetributionEffect | 11 | Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Invoke Minion Card Fade Default | 4 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Invoke Minion Card Fade Legendary | 2 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Invoke Minion Hits Ground | 5 | Shader Graphs/Fx_ParticleDissolve_apb, Shader Graphs/Fx_RockDissolve, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 5 | 原版补充包兜底 Shader Graphs/Fx_ParticleDissolve_apb；原版补充包兜底 Shader Graphs/Fx_RockDissolve |
| Invoke Minion Hits Ground Legendary | 12 | Everguild/FX/Extra Color, Shader Graphs/Fx_ParticleDissolve_apb, Shader Graphs/Fx_RockDissolve, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 原版补充包兜底 Shader Graphs/Fx_ParticleDissolve_apb；原版补充包兜底 Shader Graphs/Fx_RockDissolve；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Invoke Minion Legendary ALT | 7 | Everguild/FX/Extra Color, Shader Graphs/Fx_ParticleDissolve_apb, Shader Graphs/Fx_RockDissolve, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 5 | 原版补充包兜底 Shader Graphs/Fx_ParticleDissolve_apb；原版补充包兜底 Shader Graphs/Fx_RockDissolve |
| InvulnerableEffect | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| InvulnerableEffectDark | 8 | Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| InvulnerableEffectGSC | 7 | Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| InvulnerableEffectGuard | 6 | Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| InvulnerableEffectSW | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| InvulnerableEffectTau | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| InvulnerableEffectTyranid | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Jammed Communications damage | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| KhaineBuff | 6 | Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Kroot_Summon_quick | 3 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Laser_Red | 9 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Everguild/FX/Particle Premultiply Greyscale Coloring, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Spine/Special/HiddenPass |
| Lasrifle Offscreen Target | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_3shots | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Spine/Special/HiddenPass；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_auto | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_auto OLD | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Spine/Special/HiddenPass；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_basic | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_basic OLD | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Spine/Special/HiddenPass；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_hellpistol | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Spine/Special/HiddenPass；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_Hotshot_2shots | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_Hotshot_3shots | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_Hotshot_auto_burst | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_Hotshot_basic | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_Hotshot_pistol | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_laspistol | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_laspistol OLD | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Spine/Special/HiddenPass；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_laspistol_quick | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_laspistol_Ursula_AA | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_offscreen | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_offscreen_1x | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lasrifle_offscreen_5x | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lightning burst webway | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Lightning_Green | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| LongRange_GainTrait | 6 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| LongRange_LoseTrait | 4 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| MarkerlightExpire | 6 | Everguild/FX/Extra Color, Everguild/FX/MarkerLight, Everguild/FX/Unlit UV scroll, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/MarkerLight |
| MarkerlightIdle | 8 | Everguild/FX/Extra Color, Everguild/FX/MarkerLight, Everguild/FX/Unlit UV scroll, TextMeshPro/Distance Field, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 5 | 原版补充包兜底 Everguild/FX/MarkerLight；工程/包内同名 TextMeshPro/Distance Field |
| MarkerlightProcEffect | 15 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| MarkerlightSource | 9 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Everguild/FX/Unlit UV scroll, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| MarkerlightTriggerDamage | 14 | Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| MarkOfKhorne_enchantment | 2 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| MarkOfNurgle_enchantment | 4 | Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| MarkOfSlaanesh_enchantment | 3 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| MarkOfTzeentch_enchantment | 4 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| MasterOfExecutions | 5 | Legacy Shaders/Particles/Alpha Blended, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Simple Lit, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| MegaBlasta | 17 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Spine/Special/HiddenPass；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| MegaBlasta_Small | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Particle Premultiply Greyscale Coloring, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Meteor Angled | 5 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Mining_Laser | 11 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Everguild/FX/Particle Premultiply Greyscale Coloring, Mobile/Particles/Additive, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Spine/Special/HiddenPass；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| MobEffect | 8 | Everguild/FX/Particle Shine Custom Vertex Streams, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| MoreDakkaEffect | 5 | Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Necron_Regenerate | 4 | Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| NecronGauss | 12 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Spine/Special/HiddenPass |
| NecronGauss_Damaged Hexmark | 9 | Everguild/FX/Extra Color, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 原版补充包兜底 Spine/Special/HiddenPass；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| NecronGauss_Deathmark | 11 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Spine/Special/HiddenPass |
| NecronGauss_Double | 12 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Spine/Special/HiddenPass |
| NecronGauss_Hexmark | 9 | Everguild/FX/Extra Color, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Spine/Special/HiddenPass；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| NecronGauss_Imotekh_AA | 15 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| NecronGauss_Large | 12 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Spine/Special/HiddenPass |
| NecronGauss_Tesla | 8 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| NecronGauss_Tesla_Annihilation Barge | 9 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| NecronGauss_Trilinked | 12 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 9 | 原版补充包兜底 Spine/Special/HiddenPass |
| NecronGauss_Twinlinked | 12 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Spine/Special/HiddenPass |
| Necrons death explosion | 9 | Everguild/Cards/3D Card Explosion, Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/Cards/3D Card Explosion；原版补充包兜底 Everguild/FX/Rays For Trail |
| NephilimShadow_Flyover | 1 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Njal Blizzard Attack | 15 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| NoMukkinAboutEffect | 14 | Everguild/FX/Burning, Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Legacy Shaders/Particles/Additive, Legacy Shaders/Particles/Alpha Blended, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 7 | 原版主包兜底 Everguild/FX/Burning；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Oath_Effect | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, UI/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Offensive_Sweeping_Infestation_Target | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Offensive_Thunderstorm Target | 10 | Everguild/FX/Extra Color, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Orbital 3 repeat | 10 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| Orbital 4 repeat | 10 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| Orbital 5 repeat  | 10 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| Orbital Bombardment Enemy Warlord | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| Orbital Bombardment Target | 18 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Rays For Trail, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| Ork Environmental Condition Dust | 2 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Ork Environmental Condition Spore Clouds | 9 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Ork_Rocket | 4 | Everguild/FX/Extra Color, Legacy Shaders/Particles/Alpha Blended Premultiply, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Ork_RocketDown | 10 | Everguild/FX/Extra Color, Legacy Shaders/Particles/Alpha Blended Premultiply, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Ork_RocketUp | 6 | Everguild/FX/Extra Color, Legacy Shaders/Particles/Alpha Blended Premultiply, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Ork_Squiggoth_Charge | 6 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Ork_StormboyzStrike | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Ork_Target | 7 | Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Ork_Trampla_Charge | 6 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| OrkBuff_KrumpDaGitz | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 7 |  |
| OrkBuff_Recall | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| OrkBuff_Snakebitez_1 | 9 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| OrkBuff_Snakebitez_1_2D_SFX | 9 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| OrkBuff_Snakebitez_1_NoIcon | 8 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| OrkBuff_Snakebitez_1_Quick | 8 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| OrkBuff_Snakebitez_2 | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| OrkBuff_Snakebitez_2_Intense | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| OrkBuffGreenWaaagh | 9 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 6 |  |
| Pack_WhileInPlay | 5 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| PathOfCommand_buff | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Everguild/FX/Unlit UV scroll, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| PathOfTheSeer | 6 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| PathOfTheSeer_BG-effect | 6 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| PathOfTheSeerGreen | 6 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| PathOfTheSeerRed | 6 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| PathOfTheWarrior_Projectile | 9 | Everguild/FX/Particle Shine Custom Vertex Streams, Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| PenitenceEffect | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Everguild/FX/Particle Premultiply Greyscale Coloring, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| PinDownEffect | 5 | Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Plasma_basic OLD | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Plasma_basic_blue | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Plasma_basic_blue_double | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Plasma_basic_blue_triple | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Plasma_basic_blue_twinlink | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Plasma_basic_burst_turqoise | 17 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Plasma_basic_pink | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Plasma_basic_pink OLD | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Plasma_basic_red | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Plasma_basic_turqoise | 17 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Plasma_basic_yellow | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Legacy Shaders/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Plasma_blue_pointblank | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Plasma_blue_pointblank_double | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Plasma_triple_Azrael_AA | 15 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Lit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Plasmancer_Damage | 5 | Everguild/FX/Extra Color, Everguild/FX/TrailShader_1, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Possession_self | 8 | Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Pray_Exit | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Pray_Idle | 5 | Everguild/FX/Extra Color, Everguild/FX/Specific/Pray Glow, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Specific/Pray Glow；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Pray_Start | 9 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Pray_Success | 13 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Protocol Base | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Psychic_Lightning | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_blast | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_blast_chaos | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_chaos | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_down | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_down_Intense_white | 11 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_down_Waaagh_Green | 12 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_Genestealer | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_Genestealer_Benefictus | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_NornEmissary | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_Purple | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_Red | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_Swarmlord | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_Tyranid | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_Waaagh | 13 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_Waaagh_Board | 12 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_warlord_chaos | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_warlord_OLD | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychic_Lightning_white | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Psychomancer_Stun | 6 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply Greyscale Coloring, Everguild/FX/TrailShader_1, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Pulse Onslaught | 10 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Quest UI Gain | 6 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Quest UI Trigger | 6 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Railgun turret | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| RallyEffect | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Rapacious_Hunger | 7 | Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Unlit UV scroll, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Rapturous Ruination Board | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Recall_Eldar | 7 | Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Recall_Eldar_2 | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Recall_Eldar_2_quick | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Recall_Eldar_2_quick_ground | 9 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Recall_Eldar_2_quick_ground_warlord | 9 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Recall_Eldar_2_quick_UI | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Recall_Eldar_2_reverse | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Recall_Generic | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Recon Enemy Hand | 5 | Everguild/FX/Alpha Mask One Layer, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Recon Enemy Hand Purple | 6 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Recon Hit | 8 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Everguild/FX/Unlit UV scroll, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Recon Hit Purple | 8 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Everguild/FX/Unlit UV scroll, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Recon Scan | 9 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Everguild/FX/Unlit UV scroll, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Recon Scan 2 | 6 | Everguild/FX/Extra Color, Everguild/FX/Unlit UV scroll, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Recon Scan Purple | 8 | Everguild/FX/Extra Color, Everguild/FX/Unlit UV scroll, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| RegimentEffect | 6 | Everguild/FX/FX Shine For Animation, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/FX Shine For Animation |
| Relentless Fusillade | 10 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Relentless Fusillade sniper | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 3 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| RelentlessEffect | 5 | Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| RemnantBody3D Aeldari | 19 | Everguild/Cards/3D Card Explosion, Everguild/Cards/Gem Crystal Glitter, Everguild/Cards/Gem Crystal Glitter Explosion, Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Rays For Trail, Everguild/FX/Unlit UV scroll, Sprites/Default, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 5 | 原版补充包兜底 Everguild/Cards/3D Card Explosion；原版补充包兜底 Everguild/Cards/Gem Crystal Glitter；原版补充包兜底 Everguild/Cards/Gem Crystal Glitter Explosion；原版补充包兜底 Everguild/FX/Rays For Trail |
| RemnantBody3D Necrons | 15 | Everguild/Cards/Necrons Base Death, Everguild/Cards/Shatter Inner Pieces, Everguild/FX/Card Remnant Death Icon, Everguild/FX/Extra Color, Everguild/FX/Unlit UV scroll, Sprites/Default, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 6 | 原版补充包兜底 Everguild/Cards/Necrons Base Death；原版补充包兜底 Everguild/Cards/Shatter Inner Pieces；原版补充包兜底 Everguild/FX/Card Remnant Death Icon |
| RiftCannon_Laser | 14 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Particle Premultiply Greyscale Coloring, Mobile/Particles/Additive, Spine/Special/HiddenPass, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 原版补充包兜底 Spine/Special/HiddenPass；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| RocketTrail | 10 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Rockfall | 5 | Everguild/Matcap/Matcap Full Options, Legacy Shaders/Particles/Anim Alpha Blended, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| SAU_CardDraw | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Sau_CurseOfThePhaeron | 4 | Everguild/FX/Particle Premultiply Greyscale Coloring, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Sau_DamageProtocol | 9 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Sau_DamageProtocol_OLD | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Sau_DamageProtocol_Quick | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Sau_Dimensional_Breach_Battleground | 9 | Everguild/FX/Extra Color, Everguild/FX/TrailShader_Fading, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Sau_Dimensional_Breach_Spawn | 12 | Everguild/FX/Extra Color, Everguild/FX/TrailShader_Fading, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Sau_HealProtocol | 9 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Sau_HealProtocol_OLD | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Sau_IndomitableWill | 5 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Sau_LordOfTheStorm | 10 | Everguild/FX/Extra Color, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Sau_MasterChronomancer | 9 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 6 |  |
| Sau_MethodicalDestruction | 11 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Sau_ParticleWhip | 15 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Sau_ReconstitutionProtocol | 9 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Sau_ResurrectionOrb | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/TrailShader_1, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Sau_SolarPulse | 4 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Scarab Explosion | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Premultiply Greyscale Coloring, Everguild/FX/Rays For Trail, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| Scrapcode_aimed_target | 9 | Everguild/FX/Multi Ray, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 4 |  |
| Scream_gold | 2 | Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Scream_purple | 2 | Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Scream_purple_nid | 3 | Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Scream_purple_Quick | 2 | Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Scream_Ragnar_Target | 4 | Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Scream_SW_Ragnar | 6 | Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Screen_Gold | 2 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Screen_Red | 2 | Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Screen_Top_Green | 1 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| ScytheAssault | 9 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 8 |  |
| ScytheAssaultShadow | 1 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sentry_Effect | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Shadow_Projectile | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Shadow_Projectile_EC | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Shadow_Projectile_GSC_Invulnerable | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| ShieldTraitEffect_Idle | 9 | Everguild/FX/Extra Color, Everguild/FX/ShieldVfx, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/ShieldVfx |
| ShieldTraitEffect_Idle Emperor's Children Variant | 12 | Everguild/FX/Extra Color, Everguild/FX/ShieldVfx, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/ShieldVfx |
| ShieldTraitEffect_Idle Necron Variant | 9 | Everguild/FX/Extra Color, Everguild/FX/ShieldVfx, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/ShieldVfx |
| ShieldTraitEffect_Idle Sororitas Variant | 11 | Everguild/FX/Extra Color, Everguild/FX/ShieldVfx, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/ShieldVfx |
| ShieldTraitEffect_Idle TAU Variant | 9 | Everguild/FX/Extra Color, Everguild/FX/ShieldVfx, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/ShieldVfx |
| Shuriken_Short_Trait | 14 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Everguild/FX/Particle Shine Custom Vertex Streams, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 8 |  |
| ShurikenEffect | 6 | Everguild/FX/Particle Shine Custom Vertex Streams, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| SkorchaAttackEffect | 10 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Slash Repeating 2x | 6 | Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Slash Repeating 5x | 6 | Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| SlayEffect | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Smoke_Board | 4 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Smoke_Explosion | 5 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sniper Target | 3 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| SniperPreEffect | 4 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Solar Beam Target | 8 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Sororitas Saint Custom summon | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sororitas Summon Basic | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sororitas Summon Resurrect | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sororitas_BladeOfFaith | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sororitas_Destroy_Flames | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sororitas_Divine Intervention_ Battleground | 6 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sororitas_Divine Intervention_Target | 12 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Space Wolves Summon | 9 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Space Wolves Summon OLD | 10 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Space Wolves Summon Wolves | 9 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Spore Explosion | 7 | Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Spore Mine Explosion | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Unlit UV scroll, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Squig_BigChompaJaws | 4 | Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Simple Lit, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Squig_Explosion | 7 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Stealth_Proc | 7 | Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Stealth_Proc_Eldar | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Stealth_Proc_UM | 7 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| StealthEffect | 2 | Everguild/FX/Unlit UV scroll, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| StimulateEffect | 11 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| StompEffect | 11 | Everguild/FX/Extra Color, Everguild/FX/FX Shine For Animation, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/FX Shine For Animation |
| StrikeEffect | 5 | Everguild/FX/Unlit UV scroll, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| StunEffect | 5 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| StunEffect_idle | 3 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| StunEffect_proc | 2 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Summon_Generic | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| SW_Axe_Buff | 9 | Everguild/FX/Extra Color, Everguild/FX/ShieldVfx, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/FX/ShieldVfx |
| SW_Tornado | 6 | Everguild/FX/Extra Color, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Swarm Card Merge VFX | 5 | Everguild/Cards/BlobShadow, Everguild/Cards/Card Swarm Effect, Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/Cards/BlobShadow；原版补充包兜底 Everguild/Cards/Card Swarm Effect |
| Swarm_Trigger_OnTarget | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sword_10x_EC | 14 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Sword_Slash_DA | 8 | Everguild/FX/Extra Color, Everguild/FX/Glow Shader, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Glow Shader |
| Sword_Slash_DA_Heavy | 8 | Everguild/FX/Extra Color, Everguild/FX/Glow Shader, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Glow Shader |
| Sword_Slash_EC | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sword_Slash_EC 3x | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sword_Slash_EC_User | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sword_Slash_EC_User 3x | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sword_Slash_EC_User 7x | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sword_Slash_EC_User Inverted | 9 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Sword_Slash_Simple | 7 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Unlit UV scroll, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Sword_Slash_User_DA | 5 | Everguild/FX/Glow Shader, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Glow Shader |
| Sword_Slash_User_DA No Tween | 5 | Everguild/FX/Glow Shader, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Everguild/FX/Glow Shader |
| SynapseEffect | 11 | Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Unlit UV scroll, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| SynapseEffect_NoIcon | 11 | Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Unlit UV scroll, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Talent_Warmaster | 6 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| TalentEffect | 5 | Everguild/FX/Particle Shine Custom Vertex Streams, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Tank_Shot_Small | 4 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Blue Glow | 2 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Blue Glow Blink | 2 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Fire Eldar | 4 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Tap Firepit | 3 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Firepit 2 | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Floor Grille | 2 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Green Glow | 2 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Hit Metal | 2 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Hit Metal Red | 3 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Hit Metal Small | 2 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Monolith Left Glow | 3 | Everguild/FX/Extra Color, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Monolith Right Glow | 3 | Everguild/FX/Extra Color, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Plasma generator | 4 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Red Glow Eye | 2 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Rock | 3 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Squig | 1 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap squishy | 3 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Toxic Sludge | 4 | Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Toxic Sludge green | 3 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tap Webway Portal | 5 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tau Buff Ranged | 11 | Everguild/FX/Alpha Mask One Layer, Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Tau Coordinated Engagement | 17 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Tau_AerialBombardment | 16 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Tau_RailBombardment UNUSED | 30 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 6 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Tau_SummonCircle | 7 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Tau_Vespid_laser | 7 | Everguild/FX/Multi Ray, Spine/Special/HiddenPass, UI/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Spine/Special/HiddenPass |
| Technomancer_Buff | 3 | Everguild/FX/Extra Color, Everguild/FX/TrailShader_1, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Telepathic_Control_target | 4 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Telepathic_Control_User | 5 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Teleport Recall | 7 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Teleport Trait Summon | 11 | Everguild/FX/Extra Color, Everguild/FX/Multi Ray, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Teleport Trigger Effect Quick | 9 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Thunderhawk Flyover | 3 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| TideEffect | 7 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Tidewall Gunrig | 19 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| TinyFlames | 1 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Toxic Vapors Damage | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Toxic Vapors Damage quick | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Toxic_Entanglement | 7 | Custom/EditorIcon, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 1 | 原版补充包兜底 Custom/EditorIcon |
| ToxicMiasma_Board | 3 | Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| ToxicMiasma_Venomthrope | 2 | Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| ToxicMiasma_Warlord | 5 | Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Trait_GainQuest | 11 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| TraitorsHate_Board | 5 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| TraitorsHate_Target | 4 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tyranid Spawn | 6 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Tyranid_Burrow | 4 | Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| TyranidInvasion_Spawn | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Unlit UV scroll, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| UM_AvengingSon | 6 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 6 |  |
| UM_Buff | 8 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| UM_CardDraw | 6 | Everguild/FX/Extra Color, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| UM_DeathFromAbove | 11 | Everguild/FX/Extra Color, Mobile/Particles/Alpha Blended, Shader Graphs/Fx_ParticleDissolve_apb, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Shader Graphs/Fx_ParticleDissolve_apb；工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| UM_MasterOfArcanaEffect | 18 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| UM_MightOfHeroesEffect | 6 | Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| UM_StormravenShadow | 4 | Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| UM_StormravenShadow_Perpendicular | 4 | Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| UM_TacticalInsight | 7 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 5 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| UM_Target_LockOn_Crosshair | 2 | Everguild/FX/Extra Color | 无 |  |  |
| Unit Freezing | 6 | Legacy Shaders/Particles/Anim Alpha Blended, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| UnstableEffect | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Alpha Blended, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| UnstableEffect_ExplosionExtra | 10 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| UnstableEffect_WaaaghEnergy | 17 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Uprising Trait | 8 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Valkyrie Flyover Hover Dust | 3 | Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Valkyrie_MultiLaser_Hover | 8 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 |  | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Vanguard Onslaught Board | 17 | Everguild/FX/Extra Color, Everguild/FX/Rays For Trail, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 4 | 原版补充包兜底 Everguild/FX/Rays For Trail |
| VanguardIdleEffect | 9 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options VAT, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/Matcap/Matcap Full Options VAT |
| Vortex Explosion Massive | 12 | Everguild/FX/Alpha Masks Two Layer, Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Vortex Warhead Impact | 13 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/Matcap/Matcap Full Options, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| VulnerableEffect | 4 | Everguild/FX/Unlit UV scroll, Mobile/Particles/Additive, Sprites/Mask | 无 | 3 |  |
| VulnerableEffectEnd | 3 | Everguild/FX/Unlit UV scroll, Mobile/Particles/Additive, Sprites/Mask | 无 | 2 |  |
| Waaagh_Lightning | 7 | Everguild/FX/Extra Color, Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| WailingDoom | 15 | Everguild/FX/Extra Color, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Particle Premultiply Greyscale Coloring, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Warp Storm Target | 11 | Everguild/FX/Extra Color, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Waystone UI Gain | 3 | Everguild/FX/Particle Shine Custom Vertex Streams, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| Waystone UI Spend | 5 | Everguild/FX/Extra Color, Everguild/FX/Particle Shine Custom Vertex Streams, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| WaystoneCollect | 5 | Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| WaystoneTrigger | 7 | Everguild/Cards/Gem Crystal Glitter, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 2 | 原版补充包兜底 Everguild/Cards/Gem Crystal Glitter |
| WhileInPlay_Nurgle | 7 | Mobile/Particles/Alpha Blended, Universal Render Pipeline/Particles/Unlit | 无 | 1 |  |
| WhileInPlay_Tzeentch | 5 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Particles/Extra Color | 是 · 仅 bundle 兜底 | 1 | 工程/包内同名 WarpforgeVFX/Particles/Extra Color |
| Whip Attack EC Daemon | 9 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 无 |  |  |
| Whip Attack EC Lucius | 13 | Everguild/FX/Extra Color, Everguild/FX/Unlit UV scroll, Everguild/Matcap/Matcap Full Options, Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit | 无 | 3 |  |
| Whip Attack EC Maulerfiend | 10 | Everguild/FX/Extra Color, Everguild/Matcap/Matcap Full Options, Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit, WarpforgeVFX/Matcap/Matcap | 是 · 仅 bundle 兜底 | 4 | 工程/包内同名 WarpforgeVFX/Matcap/Matcap |
| Whip Attack EC Normal | 8 | Everguild/Matcap/Matcap Full Options, Particles/Standard Unlit, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Wild Rider Chieftain | 7 | Everguild/FX/Unlit UV scroll, Mobile/Particles/Additive, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |
| Will Of Gork | 14 | Everguild/FX/Burning Dissolve, Everguild/FX/Extra Color, Everguild/FX/Particle Dissolve Mask, Everguild/FX/Particle Distortion Affect Transparents, Everguild/FX/Particle Premultiply, Everguild/Matcap/Matcap Full Options, Universal Render Pipeline/Particles/Unlit | 是 · 仅 bundle 兜底 | 3 | 原版补充包兜底 Everguild/FX/Burning Dissolve；原版补充包兜底 Everguild/FX/Particle Premultiply |
| WorstTemperEffect | 6 | Everguild/FX/Extra Color, Mobile/Particles/Additive, Sprites/Default, Sprites/Mask, Universal Render Pipeline/Particles/Unlit | 无 | 2 |  |

---
> 生成脚本（纯 Python，不碰 Unity）：`d:/4/_tmp_view/shader_audit_0913.py`

