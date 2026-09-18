# shader 原件可用性表（④「改走原件」的输入）

> 2026-09-18 · 由 `工具/survey_shader_originals.py` 生成（**纯 Python 只读**，未启动 Unity、未写 `Assets/` 与 `StreamingAssets/`）
> 数据源：实读两个 bundle（UnityPy 读 `Shader.m_ParsedForm.m_Name` + `AssetBundle.m_Container`）、两张映射表（正则抓 `.cs`）、效果行数来自 `资料/普查产出_0913/效果_shader_对账.tsv`。

## 一句话

当前**被映射到自建 shader** 的原版名共 **36** 个；其中原件**就在随包 bundle 里**的有 **23** 个（可改走原件），**原件不在包里**的有 **13** 个（只能继续用自建）。

## 一、当前已映射到自建 shader 的（这张表要判的就是这些）

| 原版 shader | 影响效果数 | 运行时映射目标 | 导出期映射目标 | 原件在主包 | 原件在补充包 | **能否改走原件** |
|---|---|---|---|---|---|---|
| `Everguild/FX/Extra Color` | 741 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color` | 有 | 无 | **✅ 能** |
| `Mobile/Particles/Additive` | 423 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 无 | ❌ 不能（原件不在包里） |
| `Everguild/FX/Particle Distortion Affect Transparents` | 233 | `WarpforgeVFX/FX/Distortion` | `WarpforgeVFX/FX/Distortion` | 有 | 无 | **✅ 能** |
| `Mobile/Particles/Alpha Blended` | 159 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 无 | ❌ 不能（原件不在包里） |
| `Sprites/Mask` | 122 | — | `Sprites/Mask` | 无 | 无 | ❌ 不能（原件不在包里） |
| `Everguild/Matcap/Matcap Full Options` | 120 | `WarpforgeVFX/Matcap/Matcap` | `WarpforgeVFX/Matcap/Matcap` | 无 | 有 | **✅ 能** |
| `Sprites/Default` | 117 | — | `Sprites/Default` | 无 | 无 | ❌ 不能（原件不在包里） |
| `Everguild/Matcap/Matcap With Texture` | 70 | `WarpforgeVFX/Matcap/Matcap` | `WarpforgeVFX/Matcap/Matcap` | 有 | 无 | **✅ 能** |
| `Everguild/FX/Unlit UV scroll` | 39 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 有 | 无 | **✅ 能** |
| `Everguild/FX/Multi Ray` | 33 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 有 | **✅ 能** |
| `Everguild/FX/Alpha Mask One Layer` | 29 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 有 | 无 | **✅ 能** |
| `Everguild/FX/Particle Dissolve Mask` | 29 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 有 | **✅ 能** |
| `Everguild/FX/Particle Shine Custom Vertex Streams` | 26 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 有 | **✅ 能** |
| `Everguild/FX/Particle Premultiply Greyscale Coloring` | 25 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 有 | 无 | **✅ 能** |
| `Legacy Shaders/Particles/Additive` | 19 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 无 | ❌ 不能（原件不在包里） |
| `Particles/Standard Unlit` | 17 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 无 | ❌ 不能（原件不在包里） |
| `Everguild/FX/Particle Premultiply` | 12 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 有 | **✅ 能** |
| `Shader Graphs/Fx_ParticleDissolve_apb` | 11 | `WarpforgeVFX/Particles/Extra Color` | — | 无 | 有 | **✅ 能** |
| `Everguild/FX/Alpha Masks Two Layer` | 9 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 有 | 无 | **✅ 能** |
| `Legacy Shaders/Particles/Alpha Blended Premultiply` | 9 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 无 | ❌ 不能（原件不在包里） |
| `Everguild/FX/TrailShader_1` | 8 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 有 | **✅ 能** |
| `Everguild/UnlitAmbient` | 8 | `WarpforgeVFX/UnlitAmbient` | `WarpforgeVFX/UnlitAmbient` | 有 | 无 | **✅ 能** |
| `Legacy Shaders/Particles/Anim Alpha Blended` | 8 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 无 | ❌ 不能（原件不在包里） |
| `UI/Additive` | 7 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 有 | 无 | **✅ 能** |
| `Legacy Shaders/Particles/Alpha Blended` | 4 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 无 | ❌ 不能（原件不在包里） |
| `Shader Graphs/Fx_RockDissolve` | 4 | `WarpforgeVFX/Particles/Extra Color` | — | 无 | 有 | **✅ 能** |
| `Mobile/Particles/Multiply` | 3 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 无 | ❌ 不能（原件不在包里） |
| `Shader Graphs/Doomweaver effect` | 3 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 有 | **✅ 能** |
| `Shader Graphs/Eclipse Tau` | 3 | `WarpforgeVFX/Particles/Extra Color` | — | 有 | 无 | **✅ 能** |
| `Everguild/FX/TrailShader_Fading` | 2 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 有 | **✅ 能** |
| `Everguild/Sprites/Sprite Additive` | 1 | `WarpforgeVFX/Sprites/Additive` | `WarpforgeVFX/Sprites/Additive` | 有 | 无 | **✅ 能** |
| `Everguild/UnlitAmbient Emissive Flickker` | 1 | `WarpforgeVFX/UnlitAmbient` | `WarpforgeVFX/UnlitAmbient` | 有 | 无 | **✅ 能** |
| `TextMeshPro/Distance Field Offset` | 1 | `TextMeshPro/Distance Field` | — | 无 | 无 | ❌ 不能（原件不在包里） |
| `Everguild/Unlit Wind` | 0 | `WarpforgeVFX/UnlitAmbient` | `WarpforgeVFX/UnlitAmbient` | 有 | 无 | **✅ 能** |
| `Particles/Additive` | 0 | `WarpforgeVFX/Particles/Extra Color` | `WarpforgeVFX/Particles/Extra Color*` | 无 | 无 | ❌ 不能（原件不在包里） |
| `UI/Default` | 0 | — | `UI/Default` | 无 | 无 | ❌ 不能（原件不在包里） |

## 二、未映射 → 掉原版 bundle 兜底 的（**已经在用原件**，列在这里只为对照）

| 原版 shader | 影响效果数 | 原件在主包 | 原件在补充包 |
|---|---|---|---|
| `Universal Render Pipeline/Particles/Unlit` | 947 | 有 | 无 |
| `Everguild/FX/Rays For Trail` | 42 | 无 | 有 |
| `Spine/Special/HiddenPass` | 23 | 无 | 有 |
| `Universal Render Pipeline/Lit` | 16 | 有 | 无 |
| `Everguild/FX/ShieldVfx` | 10 | 无 | 有 |
| `Everguild/FX/Spiral Trail FX` | 9 | 无 | 有 |
| `Universal Render Pipeline/Particles/Simple Lit` | 6 | 无 | 有 |
| `Everguild/FX/Glow Shader` | 5 | 无 | 有 |
| `Everguild/Cards/Gem Crystal Glitter` | 4 | 无 | 有 |
| `Everguild/FX/Burning` | 4 | 有 | 无 |
| `Everguild/FX/FX Shine For Animation` | 4 | 无 | 有 |
| `Everguild/FX/Burning Dissolve` | 3 | 无 | 有 |
| `Everguild/FX/Specific/Necrons Rays` | 3 | 有 | 无 |
| `Everguild/Matcap/Matcap Full Options VAT` | 3 | 无 | 有 |
| `Everguild/Cards/3D Card Explosion` | 2 | 无 | 有 |
| `Everguild/Cards/BlobShadow` | 2 | 无 | 有 |
| `Everguild/FX/MarkerLight` | 2 | 无 | 有 |
| `Everguild/Wind Matcap` | 2 | 无 | 有 |
| `GlassRefraction` | 2 | 无 | 有 |
| `Universal Render Pipeline/Particles/Lit` | 2 | 无 | 有 |
| `Custom/EditorIcon` | 1 | 无 | 有 |
| `Everguild/Cards/3D Card` | 1 | 无 | 有 |
| `Everguild/Cards/Card Swarm Effect` | 1 | 无 | 有 |
| `Everguild/Cards/Gem Crystal Glitter Explosion` | 1 | 无 | 有 |
| `Everguild/Cards/Necrons Base Death` | 1 | 无 | 有 |
| `Everguild/Cards/Shatter Inner Pieces` | 1 | 无 | 有 |
| `Everguild/FX/Card Highlight And Shadow` | 1 | 有 | 无 |
| `Everguild/FX/Card Remnant Death Icon` | 1 | 无 | 有 |
| `Everguild/FX/Halo UV scroll` | 1 | 有 | 无 |
| `Everguild/FX/Particle Dissolve Premultiply` | 1 | 无 | 有 |
| `Everguild/FX/Specific/Pray Glow` | 1 | 无 | 有 |
| `Everguild/FX/Vortex` | 1 | 有 | 无 |
| `Universal Render Pipeline/Unlit` | 1 | 有 | 无 |
| `Custom/SuperSampled Texture` | 0 | 无 | 有 |
| `Everguild/Card ImageUI` | 0 | 有 | 无 |
| `Everguild/Cards/3D Card Blend Image` | 0 | 有 | 无 |
| `Everguild/Cards/3D Card Dissolve` | 0 | 无 | 有 |
| `Everguild/Cards/3D Card Stealth` | 0 | 有 | 无 |
| `Everguild/FX/Color Gradient` | 0 | 有 | 无 |
| `Everguild/FX/Floor Planar Reflections Grainny` | 0 | 有 | 无 |
| `Everguild/FX/Sprite Dissolve Mask` | 0 | 无 | 有 |
| `Everguild/FX/Sprite Greyscale` | 0 | 无 | 有 |
| `Everguild/FX/Sprite Scan Lines` | 0 | 有 | 无 |
| `Everguild/FX/Tutorial Highlight` | 0 | 有 | 无 |
| `Everguild/Misc/Unlit shadows receiver` | 0 | 有 | 无 |
| `Everguild/UI/Campaign Points` | 0 | 有 | 无 |
| `Everguild/UI/Card ImageUI Simple` | 0 | 有 | 无 |
| `Everguild/UI/Card ImageUI Simple GreyScale` | 0 | 有 | 无 |
| `Everguild/UI/Card Ready for level up` | 0 | 有 | 无 |
| `Everguild/UI/Color Change` | 0 | 有 | 无 |
| `Everguild/UI/Greyscale` | 0 | 有 | 无 |
| `Everguild/UI/UI Border Highlight Appear` | 0 | 有 | 无 |
| `Everguild/UI/UI Border Highlight Appear Pulsating Scale` | 0 | 有 | 无 |
| `Everguild/UI/UI Border Mask` | 0 | 有 | 无 |
| `Everguild/UI/UI Distort Color Change` | 0 | 有 | 无 |
| `Hidden/Core/FallbackError` | 0 | 有 | 无 |
| `Hidden/Shader Graph/FallbackError` | 0 | 有 | 有 |
| `Hidden/Universal Render Pipeline/FallbackError` | 0 | 有 | 无 |
| `Hidden/VFX/Gargoyle Swarm/System/Output Particle Unlit Quad` | 0 | 无 | 有 |
| `Shader Graphs/Equalizer` | 0 | 有 | 无 |
| `Shader Graphs/Sprite HUE Color change` | 0 | 有 | 无 |
| `Shader Graphs/UI Vignete Shader` | 0 | 有 | 无 |
| `Unlit/ScrollingTextures` | 0 | 无 | 有 |

## 三、判读前必须知道的

- **`m_Container` 与 Shader 名都要读**：这个包的资产名是 32 位 GUID、容器路径另有一套；且数据块是 LZ4 压缩的，**裸字节 grep 会骗人**（0917 已经栽过一次）。
- **「能改走原件」的约束已经没有了**：原写「原版编译字节码不能进发布版本（与 `Resources/Art/` 同一条红线）」
  —— **2026-09-18 用户取消版权红线**（个人学习用途），**不再要求发布前处理**。
- 效果数口径 = **效果行数**（同一效果重复出现只算 1），与 0917 台账一致；`特效还原_进度与交接.md` 的「370 处引用」是**另一个口径**，别混用。
