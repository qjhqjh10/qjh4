# 原版 Warpforge shader 清单 · 汇总（2026-09-17）

## 一、结论
从 84 个原版 bundle **枚举**出 **135 个唯一 shader 名**（Shader 对象 163 个，散在 21 个 bundle）；属性合计 2174 条、Pass 合计 399 个、关键字合计 2600 条、材质 1076 个（解析到 shader 的 1073 个）。

## 二、口径与判据（可复查）
- **名单不是二手清单**：直接遍历 `aa\StandaloneWindows64\*.bundle` 里 type=Shader 的对象。（任务书让读 `_tmp_view/导出报告_快照_0917.tsv` 第 3 段 —— 那份只解出 **8** 个名字，见「五」。）
- 工具：`d:/4/Unity/工具/_dump_shaders_batch.py`（包了 `dump_shader.py` + `dump_shader_blob.py` 的主逻辑；原版两个工具都一次只吃一个 shader 名，不循环）。
- **材质→shader**：`Material.m_Shader` 是 PPtr；`m_FileID=0` 是同 bundle，`m_FileID=n` 取 `SerializedFile.externals[n-1]` 的 `archive:/CAB-xxx/CAB-xxx` → CAB 名 → bundle → pathID。
- **采样纹理**分两栏：① shader **声明的 Texture 属性**（精确）；② **字节码里额外扫到**的（判据：名字有 `X_ST`/`X_TexelSize` 兄弟，或在引擎全局白名单里）。
- 本表**只覆盖原版 bundle**。我们自建的替代 shader 不在这里，在 `Assets/WarpforgeVFX/Shaders/`（跑本次普查时那个目录正被全量重导改，故没读它，免得读到半成品）。

## 三、每个 shader 一行摘要
见各块文件顶部的「一览」表（4 块 × 9 列 = 序号 + 任务书那 8 列）。本汇总受 ≤120 行上限约束，不重复抄。
- 块1：Custom/EditorIcon … Everguild/FX/Glow Shader（30 个）
- 块2：Everguild/FX/Halo UV scroll … Everguild/Matcap/Matcap With Texture（30 个）
- 块3：Everguild/Misc/URP Transparent Shadow Receiver … Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad（36 个）
- 块4：Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad … Unlit/ScrollingTextures（39 个）

## 四、完整名单（字母序，★=有材质在用）
  ★Custom/EditorIcon · ★Custom/SuperSampled Texture · ★Everguild/Card ImageUI · ★Everguild/Cards/3D Card · ★Everguild/Cards/3D Card Blend Image
  ★Everguild/Cards/3D Card Dissolve · ★Everguild/Cards/3D Card Explosion · ★Everguild/Cards/3D Card Stealth · ★Everguild/Cards/BlobShadow · ★Everguild/Cards/Card Swarm Effect
  ★Everguild/Cards/Gem Crystal Glitter · ★Everguild/Cards/Gem Crystal Glitter Explosion · ★Everguild/Cards/Necrons Base Death · ★Everguild/Cards/Shader Graphs/Booster Pack · ★Everguild/Cards/Shatter Inner Pieces
  ★Everguild/FX/Alpha Mask One Layer · ★Everguild/FX/Alpha Mask One Layer  Color Ramp · ★Everguild/FX/Alpha Masks Two Layer · ★Everguild/FX/Burning · ★Everguild/FX/Burning Dissolve
  ★Everguild/FX/Card Highlight And Shadow · ★Everguild/FX/Card Remnant Death Icon · ★Everguild/FX/Color Gradient · ★Everguild/FX/Extra Color · ★Everguild/FX/FX Shine For Animation
  ★Everguild/FX/FX Shine For Animation Normal · ★Everguild/FX/Floor Planar Reflections Grainny · ★Everguild/FX/Floor Planar Reflections Grainny Vertex color shadow mask · ★Everguild/FX/Genestealers Flood · ★Everguild/FX/Glow Shader
  ★Everguild/FX/Halo UV scroll · ★Everguild/FX/MarkerLight · ★Everguild/FX/Multi Ray · ★Everguild/FX/Particle Dissolve Mask · ★Everguild/FX/Particle Dissolve Premultiply
  ★Everguild/FX/Particle Distortion Affect Transparents · ★Everguild/FX/Particle Premultiply · ★Everguild/FX/Particle Premultiply Greyscale Coloring · ★Everguild/FX/Particle Shine Custom Vertex Streams · ★Everguild/FX/Rays For Trail
  ★Everguild/FX/ShieldVfx · ★Everguild/FX/Simple Fake Water · ★Everguild/FX/Specific/Necrons Rays · ★Everguild/FX/Specific/Pray Glow · ★Everguild/FX/Specific/Tau Generator Energy
  ★Everguild/FX/Spiral Trail FX · ★Everguild/FX/Sprite Dissolve Mask · ★Everguild/FX/Sprite Greyscale · ★Everguild/FX/Sprite Scan Lines · ★Everguild/FX/TrailShader_1
  ★Everguild/FX/TrailShader_Fading · ★Everguild/FX/Tutorial Highlight · ★Everguild/FX/Two passes blur · ★Everguild/FX/Tyranids/Pulsating Mesh · ★Everguild/FX/Tyranids/Tyranid Tentacle
  ★Everguild/FX/Unlit UV scroll · ★Everguild/FX/Vortex · ★Everguild/Matcap/Matcap Full Options · ★Everguild/Matcap/Matcap Full Options VAT · ★Everguild/Matcap/Matcap With Texture
  ★Everguild/Misc/URP Transparent Shadow Receiver · ★Everguild/Misc/Unlit shadows receiver · Everguild/Misc/Unlit shadows receiver Vertex Color Shadow Mask · ★Everguild/Sprites/Sprite Additive · ★Everguild/UI/Additive
  ★Everguild/UI/Campaign Points · ★Everguild/UI/Card ImageUI Simple · ★Everguild/UI/Card ImageUI Simple GreyScale · ★Everguild/UI/Card Ready for level up · ★Everguild/UI/Color Change
  ★Everguild/UI/Division Change · ★Everguild/UI/Greyscale · ★Everguild/UI/Greyscale Add · ★Everguild/UI/Pulsating · ★Everguild/UI/Scan Lines Alpha Mask
  ★Everguild/UI/UI Border Highlight Appear · ★Everguild/UI/UI Border Highlight Appear Pulsating Scale · ★Everguild/UI/UI Border Highlight Pulse Alpha · ★Everguild/UI/UI Border Mask · ★Everguild/UI/UI Clock
  ★Everguild/UI/UI Distort · ★Everguild/UI/UI Distort Color Change · ★Everguild/UI/UI Glow SDF Noise · ★Everguild/Unlit Wind · ★Everguild/UnlitAmbient
  ★Everguild/UnlitAmbient Emissive Flickker · ★Everguild/Wind Matcap · ★GlassRefraction · Hidden/Core/FallbackError · ★Hidden/LUTBlender
  Hidden/Shader Graph/FallbackError · Hidden/Universal Render Pipeline/FallbackError · ★Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad · ★Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad · ★Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad
  ★Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad · ★Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad · ★Hidden/VFX/Gargoyle Swarm/System/Output Particle Unlit Quad · Hidden/VideoComposite · Hidden/VideoDecode
  ★Legacy Shaders/Particles/Additive · ★Legacy Shaders/Particles/Alpha Blended · ★Legacy Shaders/Particles/Alpha Blended Premultiply · ★Legacy Shaders/Particles/Anim Alpha Blended · Legacy Shaders/VertexLit
  ★Mobile/Particles/Additive · ★Mobile/Particles/Alpha Blended · ★Mobile/Particles/Multiply · ★Particles/Standard Unlit · ★Shader Graphs/Doomweaver effect
  ★Shader Graphs/Eclipse Tau · ★Shader Graphs/Equalizer · ★Shader Graphs/Fx_ParticleDissolve_apb · ★Shader Graphs/Fx_RockDissolve · ★Shader Graphs/Nebula
  ★Shader Graphs/Sprite HUE Color change · ★Shader Graphs/UI Container Main Menu · ★Shader Graphs/UI Vignete Shader · ★Skybox/Cubemap · ★Spine/Special/HiddenPass
  ★Sprites/Default · ★Sprites/Mask · ★TextMeshPro/Distance Field · ★TextMeshPro/Distance Field Offset · TextMeshPro/Mobile/Distance Field
  ★TextMeshPro/Sprite · ★UI/Additive · ★UI/Default · ★Universal Render Pipeline/2D/Sprite-Unlit-Default · ★Universal Render Pipeline/Lit
  ★Universal Render Pipeline/Particles/Lit · ★Universal Render Pipeline/Particles/Simple Lit · ★Universal Render Pipeline/Particles/Unlit · ★Universal Render Pipeline/Unlit · ★Unlit/ScrollingTextures

## 五、未解析 / 查不到
- 读取失败：**0**（每个 Shader 对象都带 m_ParsedForm 与 m_Name）。
- 字节码解压失败：**0**
- 属性表为空：**10**（都是 FallbackError 与 VFX 的 `Hidden/VFX/*/Output Particle*` 生成 shader）
- 没有任何材质引用的 shader：**8** 个
- 材质指向的 shader 没解出来（3/1076）：指向的 pathID 不在该 bundle 的 Shader 表里 × 2；外部 CAB 不在本次扫的 bundle 里（unity default resources） × 1
- 导出报告快照 `_tmp_view/导出报告_快照_0917.tsv`（960 行）里**只有 2 行**带 `材质定义N个；原 shader: …` 字段（`BulletImpact_artillery_big`、`Plasma_basic_blue`），去重后 **8 个**名字：`Everguild/FX/Extra Color`、`Everguild/FX/Particle Distortion Affect Transparents`、`Everguild/Matcap/Matcap Full Options`、`Legacy Shaders/Particles/Additive`、`Mobile/Particles/Alpha Blended`、`Universal Render Pipeline/Lit`、`Universal Render Pipeline/Particles/Unlit`、`WarpforgeVFX/Particles/Extra Color`。这 8 个是 135 个的**子集** ⇒ 只当名单会漏 127 个。

## 六、关键发现
1. **DXBC 里没有 RDEF 段**：`工具/dump_shader_blob.py` 头注释写「DXBC 的 RDEF 段里资源名是明文」，实测每个 DXBC 只有 `ISGN`/`OSGN`/`SHDR` 三段（出包时反射表被剥了）；该工具扫到的名字来自 Unity 自己那段**常量缓冲布局头**，不是 DXBC 反射表。
2. **`pass.m_Name`/`pass.m_Tags` 是空的**，真值在 `pass.m_State.m_Name`/`m_State.m_Tags` —— `dump_shader.py` 印 `p.m_Name`，会印成空串。
3. **`rtBlend1..7` 全库都是默认值**（One/Zero/Add/RGBA）⇒ 只看 `rtBlend0`。⚠ **`rtSeparateBlend` 全库 False，但它不等于「有没有分开的 alpha 混合」**：`Universal Render Pipeline/2D/Sprite-Unlit-Default` 的源码第 20 行明写 `Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`（分开的 alpha），而 `rtSeparateBlend` 仍是 False —— 真正的 alpha src/dst 记在 `rtBlend0.srcBlendAlpha/destBlendAlpha`，**别看那个布尔**。
4. **大量渲染状态是「材料属性驱动」**（表里写作 `[名字]`）：URP 那批用 `_SrcBlend/_DstBlend/_ZWrite/_Cull/_ZTest`，UI 那批用 `unity_GUIZTestMode` 与 `_ColorMask`。**照这些状态硬编码 = 错**，得同时看材质的属性值。
5. 本版本 `SerializedShader` **没有** `m_ShaderRequirements` / `m_CommonParameters` 字段（老工具 getattr 兜底印 None，看着像「查不到」，其实是这个版本不序列化）。

## 七、复跑与自检
- 复跑一条命令即可（见二）；脚本只读 bundle、只写 `资料/普查产出_0917/`，不碰 `Assets/`、不启 Unity。
- **枚举数值→名字已对过权威源**：`PackageCache/com.unity.render-pipelines.universal@7865b6b91f8a/Shaders/2D/Sprite-Unlit-Default.shader:20` 写 `Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`，本表该 shader 记的是 `SrcAlpha→OneMinusSrcAlpha | α:One→OneMinusSrcAlpha` —— **逐字吻合**（5=SrcAlpha、10=OneMinusSrcAlpha、1=One），`Cull Off` / `ZWrite [_ZWrite]` 也对上。其余枚举值（Zero/DstColor/SrcColor/OneMinusDstColor/…）**只按 Unity 公开枚举的次序推出，没在本机找到源码逐条对**。
- 抽查计数：`shaders_assets_all.bundle` 45 个 Shader 对象、`battleprefabs_vfxandmisc_assets_all.bundle` 42 个。
