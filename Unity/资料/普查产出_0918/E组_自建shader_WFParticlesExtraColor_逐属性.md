# 自建 `WFParticlesExtraColor` 逐属性 / 逐运算对照（2026-09-19，只读子代理产出）

> 出处：待办第 19 行第三轮。**尺子**：原版 `Everguild/FX/Extra Color` 属性表 21 项逐字 =
> `资料/普查产出_0917/shader属性表_块1.md:278`（§24），pass 状态在紧邻几行。
> 原版 shader 原件 = `d:/2/新解包资源/assets_full/bundle_shaders_assets_all/Shader/Shader_-418309281451410444.json`
> （与 `wf_shaders.bundle` 里那份同名同字节码，`dump_shader_blob.py` 实测 55576 字节）。
> **我们**：`Unity/MyGame/Assets/WarpforgeVFX/Shaders/WFParticlesExtraColor.shader`（下面 `shader:N`）。
> **我们的材质**：`Unity/MyGame/Assets/WarpforgeVFX/Materials/*.mat` 里 **316 个**用本 shader。

## 〇 · 三条一句话

1. 属性表层面我们**多 5 项**、**少 3 项**（`unity_Lightmaps*`，不受光 shader 无影响）；**默认值不同 6 项**。
2. 真正可疑的不是属性，是**三处运算/状态**：`_ALPHAPREMULTIPLY_ON` 分支是死代码、软粒子是原版做不到的额外一项、alpha 通道用错系数。
3. 🔴 **正本 §13.4 的 6 条资产里，`BulletHoleMetalThrough.mat` 根本不用我们的 shader**（用 URP `Particles/Unlit`）——见 §3·④，那条分组要改。

## 一 · 逐属性对照表

| 属性名 | 原版有没有 | 原版类型·默认值 | 我们有没有 | 我们默认值 | 我们在哪用它（shader:行 + 原文） | 判定 | 影响面 |
|---|---|---|---|---|---|---|---|
| `_Color` | 有 | Color **HDR** def=(1,1,1,**0**) | 有 | (1,1,1,1) | `shader:91` `float4 _Color;` · `shader:168` `half4 col = tex * _Color * IN.color;` | ⚠默认值不同（原 a=0 我 a=1） | 316 材质实测：302 个 a=1、12 个 a=0.659、1 个 a=0.639、1 个 a=0.647 ⇒ **没人吃到默认值，当前无影响**（读出来的） |
| `_MainTex` | 有（flags `NoScaleOffset`） | Texture "white" | 有 | "white" | `shader:140` `OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);` | ⚠算法不同（原版带 NoScaleOffset ⇒ 不声明/不用 `_MainTex_ST`；我们用） | 316 材质 `m_Scale/m_Offset` **全部**=(1,1,0,0) ⇒ 当前无影响 |
| `_SOFTPARTICLES` | 有 | Float def=0，关键字名 **`_SOFTPARTICLES`** | 有 | 0 | `shader:84` `#pragma shader_feature_local_fragment _SOFTPARTICLES_ON` · `shader:192-199` 深度淡出 | ⚠算法不同（①关键字名不同 `_SOFTPARTICLES` vs `_SOFTPARTICLES_ON`；②我们按**场景深度**淡出，原版**做不到**——原版字节码里没有 `_CameraDepthTexture`） | 15 材质 float=1，其中只有 2 个关键字为真 ⇒ 这 2 个我们**变暗**（不是偏亮） |
| `_CastShadows` | 有 | Float 0 (HideInInspector) | 有 | 0 | 只进 CBUFFER（`shader:109`），**从不读** | ✅一致 | — |
| `_Surface` | 有 | Float 1 | 有 | 1 | 只进 CBUFFER（`shader:96`），从不读 | ✅一致 | — |
| `_Blend` | 有 | Float **2** | 有 | **0** | 只进 CBUFFER（`shader:97`），从不读 | ⚠默认值不同（原 2=Additive / 我 0=Alpha） | 只驱动 Inspector 预设 ⇒ 无影响 |
| `_AlphaClip` | 有 | Float 0 | 有 | 0 | `shader:85` pragma `_ALPHATEST_ON`（同名✅）· `shader:201` `clip(col.a - _Cutoff)` | ✅一致 | 316 材质 `_AlphaClip` 全 0，只有 1 个带 `_ALPHATEST_ON` |
| `_Cutoff` | 🔴**没有** | — | 有 | 0.5 | `shader:202` `clip(col.a - _Cutoff);` | 🔴我们多 | 只在 `_ALPHATEST_ON` 下生效 ⇒ 影响面 ≈1 个材质 |
| `_SrcBlend` | 有 | Float **1** | 有 | **5** | `shader:71` `Blend [_SrcBlend] [_DstBlend]` | ⚠默认值不同（原 1=One / 我 5=SrcAlpha） | 材质有值就照抄；只有"源 shader 不声明混合"的材质会落到 `InferFromShader` 兜底 ⇒ §2·② |
| `_DstBlend` | 有 | Float **0** | 有 | **10** | 同上 | ⚠默认值不同（原 0=Zero / 我 10=OneMinusSrcAlpha） | 同上 |
| `_SrcBlendAlpha` | 有 | Float 1 | 有 | 1 | **只进 CBUFFER（`shader:100`），状态里没用** | 🔴我们少（原版 Pass0 是 `[_SrcBlend]→[_DstBlend] \| α:[_SrcBlendAlpha]→[_DstBlendAlpha]`，我们只有两参数） | 原版材质上 `_SrcBlendAlpha=1/_DstBlendAlpha=1` ⇒ 原版 alpha **累加**、我们按颜色系数混 ⇒ **我们写出的 RT alpha 更低**（纯 RGB 度量则无影响） |
| `_DstBlendAlpha` | 有 | Float 0 | 有 | 1 | 同上一行（`shader:101`） | 🔴我们少 | 同上 |
| `_ZWrite` | 有 | Float 0 | 有 | 0 | `shader:72` `ZWrite [_ZWrite]` | ✅一致 | 21 个材质是 1（材质值照抄） |
| `_ZWriteControl` | 有 | Float 0 | 有 | 0 | 只进 CBUFFER（`shader:103`），不读 | ⚠算法不同（原版由它派生 `_ZWrite`） | 推的：无影响 |
| `_ZTest` | 有 | Float 4 | 有 | 4 | `shader:73` `ZTest [_ZTest]` | ✅一致 | — |
| `_Cull` | 有 | Float **0** | 有 | **2** | `shader:74` `Cull [_Cull]`；ShadowCaster 里另有硬编码 `Cull Off`（`shader:218`） | ⚠默认值不同（原 0=Off/我 2=Back） | 主 pass 材质值照抄 ✅；影子 pass 可能多画背面（不进特效亮度） |
| `_AlphaToMask` | 有 | Float 0 | 有 | 0 | `shader:75` `AlphaToMask [_AlphaToMask]` | ✅一致 | — |
| `_QueueOffset` | 有 | Float 0 | 有 | 0 | **从不读**（Tags 硬写 `"Queue"="Transparent"`，`shader:65`） | 🔴我们少（原版用它+`_QueueControl` 定队列） | 影响绘制顺序，不改单像素亮度 |
| `_QueueControl` | 有 | Float **-1** | 有 | **0** | 从不读 | 同上 | 同上 |
| `unity_Lightmaps`·`unity_LightmapsInd`·`unity_ShadowMasks` | 有（3 项） | Texture "" | 🔴**完全没有** | — | — | 🔴我们少（3 项） | 我们是不受光 shader、不采 GI ⇒ 无影响 |
| `_EmissionColor` | 🔴**没有** | — | 有 | (1,1,1,1) | `shader:33` 属性 · `shader:92`/`shader:228` CBUFFER · **加法已删（`shader:169-184` 留了原文）** | 🔴我们多（原版没有） | 316 材质里 **309 个**带 `_EmissionColor=(1,1,1,1)`（死值白）。现在没人读；⚠ 但 `WarpforgeEffectBinder.cs:237` 用 `HasProperty("_EmissionColor")` 当 `_EMISSION` 的开关键（见 §2·③） |
| `_SoftParticlesFadeDistance` | 🔴**没有** | — | 有 | 1.0 | `shader:95` · `shader:197` `saturate((sceneEye-partEye)/max(1e-4,_SoftParticlesFadeDistance))` | 🔴我们多 | 只在 `_SOFTPARTICLES_ON` 的 2 个材质上生效 |
| （关键字）`_ALPHAPREMULTIPLY_ON` | 原版关键字表**里有** | — | `shader:187-189` 有代码块，但 `shader:80-85` **没有任何 pragma 声明它** | — | `#ifdef _ALPHAPREMULTIPLY_ON col.rgb *= col.a; #endif` | 🔴我们少（死代码，永不编译进来） | 30 个材质 `_SrcBlend=One`，其中 ≥9 个是 `One→OneMinusSrcAlpha` ⇒ §2·① |
| （状态）Pass 数 | 原版 5 个：Forward / DepthOnly / MotionVectors / DepthNormalsOnly / ShadowCaster | — | 我们 2 个：`Unlit` + `ShadowCaster` | — | `shader:77`/`shader:211` | 🔴我们少 3 个 pass | 不改本特效像素；改深度/法线/运动矢量 |
| （状态）`Fallback` | `Hidden/Shader Graph/FallbackError` | — | `Fallback Off`（`shader:265`） | — | — | ⚠不同 | 缺 pass 时的行为 |
| （纹理）`_TintColor` 等源属性残值 | 原版属性表没有 | — | 我们没声明 | — | — | ✅**没被读活**（见 §3·②） | 原版也是死值 |

## 二 · 「最可能的偏亮来源」排序

**① `_ALPHAPREMULTIPLY_ON` 是死代码 ⇒ 预乘材质按未预乘输出（每像素最多 1/a 倍偏亮）**
- `shader:187-189` 有 `col.rgb *= col.a;`，但 `shader:80-85` 的 pragma 里**没有** `_ALPHAPREMULTIPLY_ON` ⇒ 恒不生效；同时 `shader:71` 照抄材质的 `_SrcBlend=One`。
- 316 材质里 30 个 `_SrcBlend=One(1)`，其中 9 个是 `One→OneMinusSrcAlpha`（`Explosion_Color`·`FireRed`/`FireBlack`·`Flames Loop Red/Green`·`Waterfall_ExtraColor`·`Explosion_big_ground`·`Stealth_Icon`·`Default-Particle`，另 21 个是 `One→Zero` 见②）。原版这些材质的 shader（`…Premultiply` 系 / URP `Particles/Unlit`）在关键字开着时**乘 alpha**，我们不乘 ⇒ 我们加的是 `rgb` 而不是 `rgb*a`。
- **为什么更亮**：`Blend One OneMinusSrcAlpha` 下输出 `rgb + dst*(1-a)`；alpha 越小，超出原版的倍数越大（a=0.2 ⇒ 5×）。
- **怎么验/怎么改**：改前先在 frag 里临时 `col.rgb *= col.a;`（无条件）跑一遍 sweep，只有这 9 个材质的效果会变；正解 = 补 `#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON`，并在 `EffectExporter.SetBlend`（`EffectExporter.cs:783`）里对 `(1,10)` 组合 `mat.EnableKeyword("_ALPHAPREMULTIPLY_ON")`。

**② 混合模式靠「源 shader 名」猜 ⇒ 14+ 个材质落成不透明 `One→Zero, ZWrite On`**（猜的，需 A/B）
- `EffectExporter.cs:769-781` `InferFromShader`：源 shader 名里没有 additive/premultiply/multiply/alpha blended/transparent 就返回 **`(One, Zero, zwrite=1)`**；`:793-810` `ApplyRenderState` 在源 shader 不声明 `_SrcBlend/_DstBlend` 时走这条。
- 我们材质里这样落下来的：`Cruelty Particle Custom`·`Ecstasy`·`Fast`·`Ferocity`·`Flank`·`Generic Trait Particle Shine`·`JainasMor`·`Mob`·`Oath`·`Rock Spike Dissolve Particle`·`Sentry`·`Shuriken`·`Stimulate`·`Wispy_Trail_*`（共 21 个 `_SrcBlend=1,_DstBlend=0,_ZWrite=1`）。
- **为什么更亮**：`One→Zero` 完全**忽略 alpha**（贴图透明处的亮 rgb 也会被直接写上）。
- **怎么验**：逐个材质回原版 bundle 看 `_SrcBlend/_DstBlend` 真值 + 源 shader 名（判据不能只是名）；A/B = 把这些材质的 `_DstBlend` 临时改成 `10` 看方向。

**③ `_EmissionColor` 仍在属性表里 ⇒ 给别的 shader 的材料开了 `_EMISSION` 的开关**（风险项，不是当下偏亮源）
- `WarpforgeEffectBinder.cs:235-239`：`if (m.HasProperty("_EmissionColor")) m.EnableKeyword("_EMISSION");`
- 我们 shader 没有 `_EMISSION` 变体 ⇒ 对我们无害；但 **`BulletHoleMetalThrough.mat` 的 `_EmissionColor=(0, 10.69, 19.93, 1)`** 一旦被别的 shader（URP `Particles/Unlit`）读到就是每像素 +10/+20 的加法。当前靠 `hadEmissionKeyword` 闸门挡着 ⇒ **这条闸门值得单独核一次**。
- **建议**：把 `_EmissionColor` 从属性表/CBUFFER 彻底删掉，顺手消掉 316 个材质里的死值。

**④ 软粒子：我们**多**了一条原版做不到的深度淡出 ⇒ 只会变暗**
- 原版字节码资源表里 **`_CameraDepthTexture` 不存在** ⇒ 原版的 `_SOFTPARTICLES` 不可能是深度淡出。命中材质：`LightningTrail_environment Soft.mat` · `Glow Additive Extra Color Soft.mat`。

**⑤ 两参数 `Blend` ⇒ alpha 通道系数错**（方向取决于度量口径；若含 alpha 或上游有预乘合成就偏暗，纯 RGB 则无影响）
- 原版 = `[_SrcBlend]→[_DstBlend]` **外加** `α:[_SrcBlendAlpha]→[_DstBlendAlpha]`；我们只有两参数。
- **怎么改**：先把 `Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]` 补上（与原版逐字一致，零风险）。

## 三 · 查不到的 / 排除过的

① **原版 frag 的具体算式（是否 `×2`、是否乘顶点色、`_Color` 怎么参与）—— 未定**。HLSL 源码被剥、DXBC 指令流压缩 ⇒ 只能 A/B。
② **`_TintColor` 一类的"死值"我们没读活**（这条是排除）。`ExplosionFlames Add` 原版带 `_TintColor=(0.376,…)`、`spark_blend` 带 `_EmissionColor=(0,0,0,1)`；但这两个原版 shader（`Mobile/Particles/Additive` / `Mobile/Particles/Alpha Blended`）的 RDEF 里**只有 `_MainTex` 和 `_MainTex_ST`**（同 blob 里 `_MainTex_ST` 在 ⇒ 该 shader 的反射**没被剥**，缺席是真缺席）⇒ 不是我们的偏亮来源。
③ **`LightningTrail.png` 一族（原版就是 `Everguild/FX/Extra Color`）在 shader 层面已经对齐** ⇒ 大概率不在本 shader 的算式里，建议挑一个只用它做逐像素对比（先排除纹理导入/粒子数据）。
④ 🔴 **`BulletHoleMetalThrough.mat` 不指向本 shader（正本 §13.4 的那一行要改）**：其 `m_Shader` guid 解析出 URP 包内 `ParticlesUnlit.shader`，与原版材质的 pathID 解析（`Universal Render Pipeline/Particles/Unlit`）**两条独立证据互证**。
⑤ **原版纹理的 sRGB/导入设置没查到**（与 shader 无关的偏亮/偏暗源，**未排除**）。我们用重新编码的 PNG；原版 bundle 里对应 Texture2D **没被导成 JSON** ⇒ 无法对比。若原版是线性而我们是 sRGB，采样值差一个 gamma，**足以单独造成两位数百分比的亮度差**。
⑥ **`Smoke Sprite Sheet Extra Blend.mat` 的原版 shader 未核**（不在 `battleprefabs_vfxandmisc_assets_all/Material/`，可能在别的包）。

## 四 · 关键统计（子代理自己扫的 316 个材质）

`_SrcBlend` 5×284 / 1×30 / 2×2 · `_DstBlend` 1×174 / 10×119 / 0×23 · `_ZWrite=1`×21 ·
`_MainTex` scale/offset 全 (1,1,0,0) · `_Color.a` 全 >0 · `_EmissionColor`(1,1,1,1)×309 ·
`_SOFTPARTICLES=1`×15 但关键字只在 2 个上。
时间线核查：shader mtime 09-18 22:04 < sweep 22:33 ⇒ **§13 那 62 条确实是「删掉加法之后」的数**。
