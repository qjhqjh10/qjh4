# E 组「加色发光/发光球/闪电」族 · 三件事（shader 替换 / 混合状态 / 纹理导入）对照

> 2026-09-19 · 只读子代理产出。数据源：原版 bundle 直读（UnityPy py312，**未启动 Unity**）·
> 我们的 `.mat`（`MyGame/Assets/WarpforgeVFX/Materials/`）· `screen_asset_impact.py --min 2`（只读）· 台账 `资料/特效还原台账.tsv`。
> E 组 46 条，`亮度比中位` 中位 **1.585**（即**典型 ~1.6× 偏亮**，多数落在 ×2 附近）。基线偏亮率 82.6%，本表资产 100% 偏亮。

## 一 · 主表

| 资产 | E内 | 原版 shader | 我们的 shader | 原版混合 (Src/Dst/ZWrite) | 我们的混合 | 关键差异（一句话） | 最可能的偏亮原因 | 置信度 |
|---|---|---|---|---|---|---|---|---|
| `ring_warp.png` | 22 | （贴图，随宿主）`Everguild/FX/Extra Color` + URP `Particles/Unlit` | `WFParticlesExtraColor` + URP `ParticlesUnlit` | 5/1/0（Extra Color 宿主）/ 5/1/0（URP 宿主） | 5/1/0 / 5/1/0 | **像素逐字节差 ≤1/255**（512²）；采样器设置逐项相同（filter=1 aniso=1 wrap=0 mips=10）；`cs=1`=sRGB 与原版一致 | **三件事都对上了**；`_MainTex`=ring_warp 的两个材质（`Ring_Warped_extra color` / `Ring_Warped_add`）的 shader 算式读不到（原版 DXBC 无 RDEF）⇒ **推**：残留偏亮在宿主材质算式层，不在这三件事 | 读到的 |
| `glowsphere01.png` | 16 | （贴图）URP `Particles/Unlit` | URP `ParticlesUnlit` | 5/10/0 | 5/10/0 | 像素 DG ≤1/255；`cs=1`、mips=9、aniso=1 全对 | 同上；⚠️ 宿主 `Glow Sphere 01` 丢了 `_COLORADDSUBDIFF_ON` | 读到的 |
| `Ring_Warped_extra color.mat` | 16 | `Everguild/FX/Extra Color`(PID -418309281451410444) | `WFParticlesExtraColor` | **5/1/0** · `_Color=(4,4,4,1)` · `_Cull=0` | **5/1/0** · `_Color=(4,4,4,1)` · `_Cull=0` | 只差 `_EmissionColor`：原版 **(0,0,0,1)** → 我们 **(1,1,1,1)**（导出器补默认白） | **`_EmissionColor` 白值**是唯一读到的差异；`_EMISSION` 闸门（`WarpforgeEffectBinder.cs:235`）当前应关着 ⇒ 若闸门漏了就每像素 +1×贴图。**首选 A/B**：把这个材质的 `_EmissionColor` 改回黑复扫 | 读到的（闸门是否漏=推） |
| `flak1.png` | 14 | （贴图）= `spark_blend.mat._MainTex` | 同 `spark_blend` | — | — | 像素 DG ≤1/255；`cs=1`、mips=1 | **归入 `spark_blend` 那一行**，贴图本身无差异 | 读到的 |
| `Glow Sphere 01.mat` | 13 | URP `Particles/Unlit` | URP `ParticlesUnlit`（**同一个 shader**） | **5/10/0** · `_ColorMode=1` · KW=`_COLORADDSUBDIFF_ON,_SURFACE_TYPE_TRANSPARENT` | **5/10/0** · `_ColorMode=0` · KW=`_SURFACE_TYPE_TRANSPARENT`（`_COLORADDSUBDIFF_ON` 落到 `m_InvalidKeywords`） | 🔴 **丢了 `_COLORADDSUBDIFF_ON`**（+ `_ColorMode` 1→0）。URP `MixParticleColor`（`ShaderLibrary/Particles.hlsl:63-84`）：有该关键字⇒`rgb=base+particleColor.rgb`、`a*=particleColor.a`；没有⇒`return baseColor*particleColor` | **方向不定、大概率不是偏亮源**（`_BaseColorAddSubDiff=(1,0,0,0)` 时原版是「base+顶点色」，一般比我们的乘法**更亮**）⇒ 必须 A/B | 读到的（方向=推） |
| `Ring_Warped_add.mat` | 13 | URP `Particles/Unlit` | URP `ParticlesUnlit`（同一个） | 5/1/0 · `_EmissionColor=(0,0,0,1)` · KW=`_SURFACE_TYPE_TRANSPARENT` | 5/1/0 · `_EmissionColor=(0,0,0,1)` · KW=`_SURFACE_TYPE_TRANSPARENT` | **逐属性一致**（只少了 `_BumpMap/_EmissionMap` 空槽与 Standard 残留死值） | **三件事全对上** ⇒ 残留偏亮不在这里（往粒子数据/prefab 找） | 读到的 |
| `Glow Additive Extra Color.mat` | 12 | `Everguild/FX/Extra Color` | `WFParticlesExtraColor` | 5/1/0 · `_Color=(4,4,4,1)` · `_Cull=2` | 5/1/0 · `_Color=(4,4,4,1)` · `_Cull=2` | 原版带 **孤儿属性** `_ExtraColor=1.5`（属性表 21 项里**没有**它 ⇒ 原版也不读）；我们无此属性 | 无差异可归因；`_ExtraColor` 是**死值**（已排除） | 读到的 |
| `LightningTrail.png` | 12 | （贴图）= `LightningTrail.mat._MainTex` | 同 | — | — | 像素 DG ≤1/255；**`cs=1` sRGB**；mips=8；aniso=1 | 归入 `LightningTrail` 材质行 | 读到的 |
| `spark_blend.mat` | 12 | `Mobile/Particles/Alpha Blended` · **材质上是死值 1/0/1** | `WFParticlesExtraColor` | 真实 pass = `SrcAlpha→OneMinusSrcAlpha` · ZWrite Off（`块4.md:135-141`） | **5/10/0** | 我们**没有**照抄死值（1/0/1），而是复现了真实 pass ⇒ **这一项已对齐** | 对齐。⚠️ 原版老 shader 的 frag **无 RDEF 读不到** ⇒ 若原版带 `2.0f*i.color`，我们就是 **2× 偏暗**（方向相反）⇒ 唯一未排除项，需单变量 A/B | 混合=读到的；frag 算式=**推** |
| `Lightning_Burst_Random.png` | 11 | （贴图）= `Lightning Burst Random add._MainTex` | 同 | — | — | 像素 DG ≤1/255；`cs=1`、mips=10 | 归入下一行 | 读到的 |
| `ExplosionFlames Add.mat` | 11 | `Mobile/Particles/Additive` | `WFParticlesExtraColor` | 真实 pass = `SrcAlpha→One` · ZWrite Off · **Cull Off** | **5/1/0**，但 `_Cull=2`(Back) | 混合已对齐；`_TintColor=(0.376,0.376,0.376,0.502)` 是**死值**（属性表只有 `_MainTex`）；`_InvFade` 同样死 | 🔴 **`_MainTex` = `DoubleFlames`，原版 `cs=0`(线性)** ⇒ 我们的 PNG 被 `ReadableCopy` 的 sRGB RT **多编了一次 gamma**（见 §三）。Linear 工程里被导入器解码抵消 ⇒ 净中性 | 读到的 |
| `DoubleFlames.png` | 11 | 同上 | 同 | — | — | 🔴 **原版 `m_ColorSpace=0`（线性）· fmt=4(RGBA32) · 无压缩**；我们的 PNG = 原值做了一次 **LinearToSRGB**：bundle (1,1,1,1)/(7,7,7,7)/(25,25,25,25) → 我们 (13,13,13,1)/(46,46,46,7)/(88,88,88,25)，均值 65→**104** | 见 §三；当前工程 Linear ⇒ 抵消（**推**），若切 Gamma 立刻 ~2× 偏亮 | 像素=**读到的**；抵消=**推** |
| `Lightning Burst Random add.mat` | 9 | `Everguild/FX/Extra Color` | `WFParticlesExtraColor` | 5/1/0 · `_Color=(2,2,2,1)` | 5/1/0 · `_Color=(2,2,2,1)` | `_EmissionColor` 原版 (1.2627,…) → 我们 (1,1,1,1)；`_SoftParticlesEnabled=1`/`_CameraFadingEnabled=1` 在原版**无对应关键字**⇒死值 | 无实质差异；`_EmissionColor` 闸门同上 | 读到的 |
| `GlowPalet.png` | 9 | （贴图）= `GlowPalet Add._MainTex` | 同 | — | — | 🔴 **原版 `cs=0`（线性）· fmt=3(RGB24，无 alpha)**；我们的 PNG 同样被 LinearToSRGB：bundle (0,1,0)/(1,2,1)/(15,16,15) → 我们 (0,13,0)/(13,22,13)/(69,71,69) | 同 `DoubleFlames` | 像素=读到的 |
| `GlowPalet Add.mat` | 9 | `Mobile/Particles/Additive`（同 ExplosionFlames） | `WFParticlesExtraColor` | `SrcAlpha→One` · ZWrite Off · Cull Off（`_TintColor` 死值） | 5/1/0，`_Cull=2` | 混合已对齐（`_SrcBlend/_DstBlend/_ZWrite` 都是**我们新写的**，不是抄死值） | 同 `ExplosionFlames Add`：贴图 cs=0 那一问 | 读到的 |
| `Smoke Cloud.png` | 9 | （贴图）= `Smoke Cloud._BaseMap` | 同 | — | — | 像素 **逐字节完全相同**；`cs=1`、mips=9、aniso=1 | 贴图无差异 | 读到的 |
| `Smoke Cloud.mat` | 9 | URP `Particles/Unlit` | URP `ParticlesUnlit`（同一个） | **5/10/0** · `_EmissionColor=**(0, 10.6907, 19.9308, 1)**` · KW=`_SURFACE_TYPE_TRANSPARENT` | **5/10/0** · `_EmissionColor=**(0, 10.6907, 19.9308, 1)**`（照抄成功）· `_BaseColorAddSubDiff` (-1,1,0,0)→(1,0,0,0) | 🔴 混合/shader/贴图全对齐；唯一隐患是**原版这个材质就带着 HDR 量级 `_EmissionColor`**（0,10.69,19.93）。原版没 `_EMISSION` 关键字⇒不采；我们靠 `hadEmissionKeyword` 闸门挡 | **闸门一旦漏开 ⇒ 每像素 +10~+20 的青蓝加法，必然爆亮**。这是本族里唯一天然的「高倍率开关」，建议**单独核一次闸门**（`WarpforgeEffectBinder.cs:235-239`） | 材质值=读到的；闸门=推 |
| `Glow Noise.png` | 9 | （贴图）= `Glow Noise add._BaseMap` | 同 | — | — | 像素 DG ≤1/255；`cs=1`、mips=8 | 贴图无差异 | 读到的 |
| `Glow_Rays_Ring.png` | 9 | （贴图，宿主未核） | 宿主 `Glow Rays Ring Additive Extra Color.mat` | 未核 | 未核 | 像素 DG ≤1/255；`cs=1`、mips=9、aniso=1 | 贴图无差异 | 读到的（宿主未查） |
| `Smoke5.png` | 8 | （贴图，宿主 `Smoke5_blend.mat`） | 宿主 | — | — | 像素 DG ≤1/255；`cs=1`、mips=11 | 贴图无差异 | 读到的 |
| `LightningTrail 1.mat` | 8 | `Everguild/FX/Extra Color` | `WFParticlesExtraColor` | 5/1/0 · `_Color=(8,8,8,0.6588)` | 5/1/0 · `_Color=(7.999999,…)` | 逐属性一致；`_EmissionColor` (0,0,0,1)→(1,1,1,1) | 同 `Ring_Warped_extra color` 行 | 读到的 |

## 二 · 跨资产共性

**读出来的结论：这一族在「shader 替换 + 混合状态 + 纹理导入」上基本是**对齐的**，共性不是「同一个错」，而是「同一个**未被读到的黑盒**」。**

1. **混合状态整体对齐（读到的，非推的）。** 12 个资产里 11 个的 `(Src,Dst,ZWrite)` 与「原版真实 pass 状态」逐项相同：
   - `5/1/0`：`Ring_Warped_extra color`、`Ring_Warped_add`、`Glow Additive Extra Color`、`Lightning Burst Random add`、`ExplosionFlames Add`、`GlowPalet Add`、`LightningTrail 1`
   - `5/10/0`：`Glow Sphere 01`、`Smoke Cloud`、`spark_blend`
   ⇒ **偏亮侧 38 条不是「混合写错」造成的**。**别再往混合上查**。
   - ⚠️ 混合有**两个写入点**：磁盘上的 `.mat`，以及运行时 `WarpforgeEffectBinder.Build` 里 `InferBlend(原版 shader 名)` **无条件覆盖**（`WarpforgeEffectBinder.cs:211-220`）。本族全部原版 shader 名在 `InferBlend` 里**返回 null** ⇒ 不覆盖。**这是一条要守住的边界**。

2. **贴图导入设置整体对齐（读到的）。** 原版 `filter=1 / aniso=1 / wrap=0 / mipBias=0`、mips 8–11；我们的 `.meta` 逐项相同。

3. **⇒ 能一次修一批的做法（按性价比排序）**
   - **(A) 先把「`_EMISSION` 闸门」单独核死**（覆盖 5 个材质）：原版 `_EmissionColor` 是 `(0,0,0,1)` 或 HDR `(0,10.69,19.93,1)`，我们导出器**统一补成 (1,1,1,1)**。闸门一旦漏 ⇒「+整份贴图」或「+10~20 加法」，**正好 ×2 量级**。
   - **(B) `_EmissionColor` 别补白**（一次改一批，零风险）：照抄原版值（黑就写黑），与「属性表里没有的一律硬编码」同源。
   - **(C) `WFParticlesExtraColor` 的 `_Color` 语义**（4 个材质 + 12 个贴图资产）：`shader:176` 是 `tex * _Color * IN.color`，原版 frag 读不到 —— **本族最大的单点黑盒**，一次 A/B 能覆盖一片。
   - **(D) `Mobile/Particles/*` 的 `2.0f` 一问**（3 个材质）：见 §四。

## 三 · 纹理 sRGB 那一问（**能读到，已读到**）

**结论：⛔ 原版这些贴图就是 sRGB；我们的 `sRGBTexture: true` 是对的 ——「原版线性 / 我们 sRGB」这条假设对本族不成立。**

**怎么读到的**：`assets_full` 里确实 0 个 `Texture2D/*.json`，但**原始 bundle 在**
`d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/*.bundle`，
用 UnityPy 直接 `read_typetree()` 就能拿到全部字段：`m_ColorSpace` · `m_TextureFormat` · `m_MipCount` ·
`m_TextureSettings{...}` · `m_PlatformBlob` · `m_StreamData`。
**`m_ColorSpace` 语义（功能对照）**：`_BumpMap` 槽 → **5/5 全是 cs=0** 且都叫 `*Normal`；`_MainTex` cs=1 204/cs=0 5
⇒ **cs=0 = Linear，cs=1 = sRGB**。

| 贴图 | 包 | fmt | **cs** | 尺寸 | mips | 我们 PNG 像素差 |
|---|---|---|---|---|---|---|
| `ring_warp` | duplicateassetisolation | 12(DXT5) | **1 = sRGB** | 512² | 10 | DG mean 0.146 |
| `glowsphere01` | duplicateassetisolation | 12 | **1** | 256² | 9 | 0.112 |
| `flak1` | battleprefabs + boosterpacks | 12 | **1** | 128² | 1 | 0.025 |
| `LightningTrail` | duplicateassetisolation | 12 | **1** | 128² | 8 | 0.266（max 20） |
| `Lightning_Burst_Random` | duplicateassetisolation | 12 | **1** | 512² | 10 | 0.022 |
| `Smoke Cloud` | duplicateassetisolation | 25(BC7) | **1** | 256² | 9 | **完全相同** |
| `Smoke5` | duplicateassetisolation | 12 | **1** | 1024² | 11 | 0.012 |
| `Glow Noise` | battleprefabs | 12 | **1** | 128² | 8 | 0.073 |
| `Glow_Rays_Ring` | battlesharedresources | 12 | **1** | 256² | 9 | 0.183 |
| `Glow_Rays_Stylized` | battleprefabs | 12 | **1** | 256² | 9 | 0.086 |
| **`DoubleFlames`** | battleprefabs | 4(RGBA32) | 🔴**0 = Linear** | 64² | 7 | **mean 29.2** |
| **`GlowPalet`** | battleprefabs | 3(RGB24) | 🔴**0 = Linear** | 32² | 6 | **mean 大** |

🔴 **机制（读到的代码）**：`EffectExporter.cs:996-998`
```csharp
var rt = RenderTexture.GetTemporary(src.width, src.height, 0,
    RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
Graphics.Blit(src, rt);
```
- 源是 **sRGB** 贴图：解码 + 写入 sRGB RT 再编码 = **恒等** ⇒ 逐字节一致。
- 源是 **Linear** 贴图：采样**不解码**、写入 sRGB RT **编码** ⇒ PNG = `LinearToSRGB(v)`（`DoubleFlames` 均值 65 → **104**）。

⚠️ **但当前工程里这一条被抵消（推的）**：导入器写死 `sRGBTexture = true`，而工程是**线性**（`ProjectSettings.asset:50 m_ActiveColorSpace: 1`）
⇒ 采样时 `sRGBToLinear(LinearToSRGB(v)) = v`，**净中性**。
⇒ **真正的影响**：① 盘上的 PNG 与原版不一致（**任何绕过导入器 sRGB 解码的消费方**都会看到 ~2× 偏亮）② **工程一旦切 Gamma** 立刻变实打实的偏亮 ③ **不是当前 46 条的偏亮源**。
**便宜的修法**：`RenderTextureReadWrite` 按源贴图的 `m_ColorSpace`（C# 侧 `Texture2D.linear`）分别取。

## 四 · 查不到的 / 排除过的

**排除（带理由）**
- ❌ **「混合状态写错」**：11/12 逐项等于原版真实 pass。**排除**。
- ❌ **「sRGB / 线性」**：9/11 原版就是 sRGB 且与我们逐字节相同；剩 2 张确实 Linear 但被导入器抵消。**对本族 46 条不成立**。
- ❌ **采样器设置**：`filter/aniso/wrap/mipBias/mip 数` 与原版**逐项相同**。**排除**。
- ❌ **`_TintColor` / `_InvFade`（原版死值）**：两个 `Mobile/Particles/*` 的属性表**只有 `_MainTex`**。**排除**。
- ❌ **`_ExtraColor`（`Glow Additive Extra Color` = 1.5）**：`Everguild/FX/Extra Color` 的 21 项属性表里**没有**它 ⇒ 原版也是死值。**排除**。
- ❌ **`_SrcBlend=1/_DstBlend=0/_ZWrite=1`（`spark_blend` 材质上的死值）**：我们**没有**照抄，得到的是 `5/10/0` = 真实 pass。**排除**。
- ❌ **`Glow Sphere 01` 丢 `_COLORADDSUBDIFF_ON`**：真丢了，但按 URP `MixParticleColor` 那个关键字开着是「base **+** 顶点色」，**原版更亮** ⇒ 当前不作为偏亮源（仍需 A/B）。
- ❌ **`_SoftParticlesFadeDistance`（我们多出来的属性）**：本表材质里只有 `Ring_Warped_extra color` / `Glow Additive Extra Color Soft` 开着该关键字。**排除**（对 `Ring_Warped_extra color` 仍待 A/B）。
- ❌ **`_Color` HDR 值**：`Ring_Warped_extra color`(4,4,4,1) · `Glow Additive Extra Color`(4,4,4,1) · `Lightning Burst Random add`(2,2,2,1) · `LightningTrail`(8,8,8,0.6588) —— **原版与我们逐位相同**。**排除**。

**查不到的（写清试过什么）**
- ✅ **2026-09-19 晚 主对话补课：「`Mobile/Particles/*` 的 frag 算式」这条已经查到了**（原文写「读不到」，**只对了一半**）：
  · **源码就在本地**：`D:/Unity/Hub/Editor/2022.3.62t15/Editor/Data/CGIncludes/builtin_shaders.zip`
    → `DefaultResourcesExtra/Mobile/Mobile-Particle-Add.shader` / `Mobile-Particle-Alpha.shader`。
    它们**根本不是 HLSL**，是**固定功能 shader**：
    ```shader
    Shader "Mobile/Particles/Additive" {
      Properties { _MainTex ("Particle Texture", 2D) = "white" {} }
      Category { Blend SrcAlpha One   Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
        BindChannels { Bind "Color", color  Bind "Vertex", vertex  Bind "TexCoord", texcoord }
        SubShader { Pass { SetTexture [_MainTex] { combine texture * primary } } } } }
    ```
    ⇒ 算式就是 **`col = tex * primary（顶点色）`**，**没有 `_Color`、没有 `_TintColor`、没有 `_InvFade`、没有软粒子**；
    混合 **`SrcAlpha One`**（Additive）/ **`SrcAlpha OneMinusSrcAlpha`**（Alpha Blended）、**Cull Off**。
    **「原版多乘一个 ×2」这条假设被否掉**（源码里是 `combine texture * primary`，**没有 `double`**）。
  · **反证（读了游戏里的编译产物）**：`Warpforge_unitybuiltinassets.bundle` 里那两个 shader 各 **2 个 DXBC 变体、
    SHDR 合计 652 字节**，扫立即数 **`2.0f` 出现 0 次**（`1.0f` 也 0 次）—— 固定功能编出来本来就不带常量。
  · **和我们的对照**：我们 `WFParticlesExtraColor` 是 `tex * _Color * IN.color`，而这三个材质的 **`_Color` 都是 `(1,1,1,1)`**
    （`ExplosionFlames Add` / `GlowPalet Add` / `spark_blend` 逐个读过）⇒ **两边算式等价** ⇒
    这一族的残留偏亮**不是 shader 算式差**，要往**粒子数据/顶点色/贴图**找。
  · ⚠️ **版本差异已核**：本地 zip 是 **2022.3**，游戏是 **6000.2.6f2**（从 bundle 头读到的 `UnityFS` 版本串）——
    但① 源码的 Properties 只有 `_MainTex`，与游戏里那份编译产物的 RDEF 一致 ② 字节码扫描独立佐证 ⇒ **采信**。
- 🔴 **仍然读不到的只剩一个：`Everguild/FX/Extra Color`**（游戏自己的 Shader Graph）——
  但**「读不到」也不准确**：它的 DXBC **有 20 个变体，每个都带 `SHDR` 指令段**（`ISGN/OSGN/SHDR`，只是**没有 `RDEF` 反射表**）。
  ⇒ 三条补救路线（按性价比）：
  1. **写一个 SM4/SM5 token 解码器**（`SHDR` 只是一串 dword，opcode 在低位、长度在高位）——
     **尺子可自检**：拿我们自己的 `WFParticlesExtraColor` 编译产物解一遍，结果必须与它的源码一致；
  2. **拿 `dxc`/RenderDoc 之类现成反汇编器**（RenderDoc 顺带能抓原版实况那一帧，最权威）；
  3. 继续 A/B（本轮已跑通，但一次只能问一个变量，且带宽噪声 ±22–54 条）。
- ⚠️ **未核**：`Glow_Rays_Ring.png` 的宿主材质 · `Smoke5.png` 的宿主 `Smoke5_blend.mat`。
- ⚠️ **尺子提醒**：这份名单里**每个资产的偏亮率都是 100%**，而基线已 82.6% ⇒ **区分力很低**；`E 内` 条数大 ≠ 它是锅。真判据仍是**单变量 A/B + 复扫**（`|ln|` 中位 0.514，带宽 0.05 能让 Z 摆 22–54 条）。

## 五 · ✅ `_EMISSION` 闸门核验（2026-09-19 主对话，**结论：闸门是好的**）

上面 §二·3(A) 把「闸门漏开」列为第一嫌疑。主对话逐条核过了，**否掉**：

| 核什么 | 怎么核 | 结果 |
|---|---|---|
| 闸门代码 | 读 `WarpforgeEffectBinder.cs:234-241` | `if (!Disable && ((emissionOn && d.hadEmissionKeyword) || Enable)) Enable else Disable` —— **两个条件取交集**，逻辑与注释一致 ✅ |
| `hadEmissionKeyword` 记对没有 | 在导出后的 prefab 里解析 `WFMatDef` 块（`  - name: <材质>` … `hadEmissionKeyword: N`） | 本族 10 个材质逐个看：`Smoke Cloud`/`Glow Sphere 01`/`Ring_Warped_extra color`/`Ring_Warped_add`/`spark_blend`/`ExplosionFlames Add`/`GlowPalet Add`/`Lightning Burst Random add` = **0**（原版确实没有这个关键字）· `LightningTrail_environment Soft`/`_blend` = **1**（原版确实有）✅ 与原版一致 |
| 「`_EmissionColor` 补白」是不是凭空造数据 | 直接读原版材质 JSON（`assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Material/`） | 全库 **107 个** `hadEmissionKeyword=1` 且 `_EmissionColor=(1,1,1,1)` 的材质定义；抽查走 **URP `Particles/Unlit`**（`_EMISSION` 真会生效）的那两个：原版 `Explosion_big` / `Explosion_big_add` **本来就是 `(1,1,1,1)` 且关键字里有 `_EMISSION`** ⇒ **我们是照抄，不是造数据** ✅ |
| 我们自建 shader 上这个关键字有没有副作用 | 读 `WFParticlesExtraColor.shader` 的 pragma | 它**没有 `_EMISSION` 变体** ⇒ 对那 380 个 `Extra Color` 材质，开不开都是空操作 ✅ |

⇒ **本族偏亮不是「`_EMISSION` 闸门漏」造成的。** 这个方向可以放下了
（⚠️ 仍留着一条**卫生问题**：导出器对「原版没有这个属性」的材质会落到默认白，
影响面只到「关键字恰好开着 + 材质没有该属性」那种组合 —— 目前**没有实测到受害例**）。

## 六 · 给下一轮的排序（把这一份 + 台账绝对值合起来看）

1. **先做 `CardPrefab`**（台账绝对值最大、我们比原版**暗 26 倍**）—— 它不在本表范围内，是**真·大偏暗**。
2. ~~**`Mobile/Particles/*` 那三个材质的 `2.0f` 一问**~~ ✅ **2026-09-19 晚已答**（见 §四）：源码是固定功能
   `combine texture * primary`、编译产物里**没有 2.0f 立即数** ⇒ **这一族算式与我们等价**（我们的 `_Color` 三个都是白），
   要往**粒子数据/顶点色/贴图**找。⇒ 下一步改做下面第 3 条。
3. **`WFParticlesExtraColor` 的 `_Color` 语义**（同族 4 个材质 + 12 个贴图资产）：`tex * _Color * IN.color` 里的 `IN.color` 是否该乘，一次 A/B 覆盖一片。
4. **`DoubleFlames` / `GlowPalet` 的 Linear→sRGB 那一问**：当前被导入器抵消（净中性），但**盘上的 PNG 与原版不一致** ——
   修法便宜（`RenderTextureReadWrite` 按 `Texture2D.linear` 取），改了能让「任何绕过导入器的消费方」看到真值。
5. ⚠️ 别再查：混合状态 · 采样器设置 · `_TintColor`/`_InvFade`/`_ExtraColor` 死值 · 原版贴图 sRGB（**已实测：原版就是 sRGB**）。
