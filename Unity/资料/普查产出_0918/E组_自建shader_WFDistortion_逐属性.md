# E组 · 自建 `WFDistortion` 逐属性对照（2026-09-19，只读子代理产出）

> 出处：待办第 19 行的第三轮。子代理**只读**跑出来的（除 grep 外只实跑了一次
> `工具/dump_shader_blob.py "Particle Distortion"`，没碰 Unity、没跑批处理）。
> 目标 = 找出自建 shader 比原版**多做了什么**（E 组剩 62 条**全部偏亮**）。

对象：`Unity/MyGame/Assets/WarpforgeVFX/Shaders/WFDistortion.shader` ↔ 原版 `Everguild/FX/Particle Distortion Affect Transparents`

尺子①`资料/普查产出_0917/shader属性表_块2.md:100-108`（属性/pass 状态/关键字/材质）
尺子②**本轮实跑 `工具/dump_shader_blob.py "Particle Distortion"`**（blob 没被剥，200400 字节）——
原版**常量缓冲布局里的 material 常量只有 9 个**：`_DistortionStrength` `_DistortTex` `_Depth_And_Fallof` `_SOFTPARTICLES` `_USEMASK` `_Mask` `_ANIMUVS` `_UVSpeed` `_UVScale`；
管线全局：`_GrabPassTransparent` `_CameraDepthTexture`(+`_TexelSize`) `_ScaledScreenParams` `_RTHandleScale` `_GlobalMipBias` `_ProjectionParams` `_ZBufferParams` `_TimeParameters` `_NonJittered/PrevViewProjMatrix`。
**`_DistortTex_ST`/`_Mask_ST` 不在其中；`_CameraOpaqueTexture` 也没有。**（全是「读出来的」）
⚠️ 该 blob 只有 `ISGN/OSGN/SHDR`、**无 RDEF 反射表**（`工具/_dump_shaders_batch.py` §六.1）⇒ 名字是明文、**公式读不到**：本文凡涉「原版怎么算」一律标「推」。

## 一 · 逐属性对照表

| 属性名 | 原版有没有 | 原版类型·默认值 | 我们有没有 | 我们默认值 | 我们在哪用它 | 判定 | 影响面 |
|---|---|---|---|---|---|---|---|
| `_DistortionStrength` | ✅ | Float def=1（块2:101） | ✅ | 1.0（:62） | :121 声明；:185 `offset=(d.rg*2-1)*_DistortionStrength*IN.color.a` | ✅一致（原版材质 0.1=我方 0.1，json:150 / .mat:39） | 全 45 抓屏；**唯一能把「偏移」与「alpha」分开的旋钮** |
| `_DistortTex` | ✅ | Texture def="bump"，flags=0xc（**NoScaleOffset+Normal**） | ✅ | "bump"（:63，**未加 `[NoScaleOffset]`/`[Normal]`**） | :184 `d=SAMPLE_TEXTURE2D(_DistortTex,sampler_DistortTex,uv)`；:185 | ⚠算法不同（推：原版按 Normal 型采样 ⇒ DX11/DXT5nm 取 `.a`/`.g`；我们取 `.r`/`.g`） | 18 条（RippleSubtle）+全 45 |
| `_DistortTex_ST` | 🔴无（属性 NoScaleOffset，字节码名表也没有） | — | 🔴有（:116 声明） | — | :169 `OUT.uv=TRANSFORM_TEX(IN.uv,_DistortTex)` | 🔴我们多（读死值） | 11 份 .mat **全为 (1,1)/(0,0)** ⇒ 当前不生效；Inspector 一动 Tiling 就位移，而原版永不 |
| `_Mask` | ✅ | Texture def="white" flags=0x4 | ✅ | "white"（:67） | :200 `alpha*=SAMPLE_TEXTURE2D(_Mask,sampler_Mask,uv).r` | ✅一致（两边都只在 USEMASK 下采） | RippleSubtle 的 `_Mask`=null 且 `_USEMASK=0` ⇒ 本 18 条不生效 |
| `_Mask_ST` | 🔴无 | — | 🔴有（:117） | — | 全文件未引用 | 🔴我们多（声明未用，会被编译剥掉） | 无 |
| `_Depth_And_Fallof` | ✅ | Vector def=(1,0.1,0,0) | ✅ | (1,0.1,0,0)（:68） | :208 `fade=saturate((sceneEye-partEye)/max(1e-4,_Depth_And_Fallof.y))` | ⚠算法不同（推：只用 `.y` 当**世界单位淡出距离**） | 材质值 (16.31,0.39) ⇒ 候选 2 |
| `_SOFTPARTICLES` | ✅ | Float def=0，**在原版 CB 里是活值** | ⚠只当关键字（:65 `[Toggle(_SOFTPARTICLES)]`、:107 pragma）——**CBUFFER(:115-122) 里没这个 float** | 0 | :204 `#ifdef _SOFTPARTICLES` | ⚠我们少（少一层「float 当值」） | RippleSubtle 关键字 ✓(.mat:14-15)；**6/11 份材质 float=1 而关键字空** ⇒ 候选 5 |
| `_USEMASK` | ✅ | Float def=0，CB 里是活值 | ⚠同左（:66、:108） | 0 | :199 `#ifdef _USEMASK` | ⚠我们少 | 同上 ⇒ 候选 5 |
| `_ANIMUVS` / `_UVSpeed` / `_UVScale` | ✅ | Float 0 / Vector(1,1,0,0)×2 | ✅（:70-72、:118-119） | 同 | :179-181 `uv=uv*_UVScale.xy+_UVSpeed.xy*_Time.y` | ✅一致（11 份里 `_ANIMUVS` 生效的只有 Trail Distort，关键字同步 ✓） | `_Time.y`≈`_TimeParameters.y`，等价 |
| `_QueueOffset` / `_QueueControl` | ✅ | Float 0 / -1（HideInInspector） | ✅（:74-75） | 0 / -1 | 未进 CBUFFER ⇒ 不读 | ✅一致（**原版 CB 里也没有** ⇒ 两边都是死值；材质写的 `_QueueControl:0` 谁都不读） | 无 |
| `unity_Lightmaps`×3 | ✅ | Texture | ❌未声明 | — | — | 无害（Unlit） | 无 |
| **内置 Standard 残留整批**：`_SrcBlend _DstBlend _ZWrite _Surface _ZTest _Cull _Blend _AlphaClip _EmissionColor _Color _BaseColor _Cutoff _Glossiness _Metallic _BumpScale _Parallax _SpecColor _ClearCoat* _Smoothness _ReceiveShadows _EnvironmentReflections _SpecularHighlights _WorkflowMode _OcclusionStrength …` | ❌属性表 0 个 | — | ❌Properties(:60-76) 与 CBUFFER(:115-122) 里 0 个 | — | — | ✅一致（**都没有**） | **本轮关键否证**：这堆在原版材质 json:108-233 里确实存在（`_SrcBlend:1`@:214、`_DstBlend:0`@:154、`_ZWrite:1`@:230、`_Surface:0`@:218、`_EmissionColor:(0,0,0,1)`@:262），**但原版 CB 布局里一个都没有** ⇒ 原版是死值；我们属性表也没有这些名字 ⇒ **一个都没被读活**。我们唯一多读的只有 `_DistortTex_ST`/`_Mask_ST`（上两行） |
| `_GrabPassTransparent` | ✅（明文） | 全局 Texture | ✅（:138-139） | — | :191-193 | ✅一致 | 主路径 |
| `_CameraDepthTexture` | ✅（明文） | 全局+`_TexelSize` | ✅（:113、:205） | — | :205-206 | ✅一致 | 仅软粒子 |
| `_CameraOpaqueTexture`（`SampleSceneColor`） | 🔴无（工具输出 `—`） | — | 🔴有（:112、:195） | — | :195 `scene=SampleSceneColor(...)` | 🔴我们多（原版没有的路径） | 仅 `_GrabPassAvailable<=0.5` 时；PC/Mobile renderer 都注册了 Feature（`Settings/PC_Renderer.asset:13`）⇒ 主路径不生效 |
| `_GrabPassAvailable` | 🔴无 | — | 🔴有（:140、:191） | 全局 float | :191 | 🔴我们多（自制开关，必要） | 无 |
| `_ScreenParams` ↔ `_ScaledScreenParams`+`_RTHandleScale` | 原版**只有** `_ScaledScreenParams`/`_RTHandleScale`，**没有 `_ScreenParams`** | — | 只有 `_ScreenParams`（:188） | — | :188 `float2 screenUV=IN.positionCS.xy/_ScreenParams.xy;` | ⚠算法不同 | `PC_RPAsset.asset:29 m_RenderScale:1` ⇒ **当前等价**；RenderScale≠1 或 RT 被缩放时整体错位 |
| 顶点色 `IN.color`（COLOR） | ❓无法对照（CB 看不到；原版 alpha 的唯一来源是贴图/顶点） | — | ✅（:148、:170、:185、:197） | — | :197 `half alpha = d.a*IN.color.a;` | ⚠无法对照 | 候选 4 |
| 混合状态 | ✅ P0 `SrcAlpha→OneMinusSrcAlpha`，**α 分离 `One→OneMinusSrcAlpha`**，mask=15（块2:103） | — | `Blend SrcAlpha OneMinusSrcAlpha`（:91，两通道同规则） | — | :91 | ⚠算法不同（**只差 alpha 通道**） | **RGB 逐位相同 ⇒ 不可能造成偏亮** |
| ZWrite/ZTest/Cull | P0 `Off/LEqual/Back`（块2:103） | — | :92-94 同 | — | — | ✅一致 | 无 |
| pass 数 | 3 个（P0 Forward / P1 MotionVectors / P2 DepthNormalsOnly，块2:103-105） | — | 1 个 | — | — | 🔴我们少 | 与亮度无关；原版材质自己 `disabledShaderPasses:["MOTIONVECTORS"]`（json:16-18） |
| 关键字名 | 30 个，含**裸名** `_SOFTPARTICLES/_USEMASK/_ANIMUVS`（块2:106） | — | ✅同名裸名（:107-109） | — | — | ✅一致（**无 `_ON` 后缀**；与 `WFMatcap`/`WFParticlesExtraColor` 的 `_ON` 拼法相反——那两个仍在犯） | 无 |

## 二 · 「最可能的偏亮来源」排序（针对 18 条 RippleSubtle）

1. **`_DistortTex` 的 X 分量取错通道 ⇒ 偏移退化成「常量 ≈0.1 屏宽位移」**（推，但有两条间接证据）
   `.shader:185`（`(d.rg*2-1)*…`）+ `.shader:63`（属性 flags=0xc 含 `Normal`，我们没声明 `[Normal]`）。
   为什么更亮：原版按 Normal 型采样（DX11/DXT5nm 下 X 在 `.a`、Y 在 `.g`）⇒ 偏移是**零均值噪声场**、只做小幅摇晃；我们取 `.r`，而该贴图在平台侧 `.r` 恒为 1 ⇒ 偏移变成**固定 +0.1 屏宽**（`_DistortionStrength=0.1`），再叠 `GrabPassTransparentFeature.cs:25-30` 自认的「上一帧全合成」反馈链 ⇒ 亮部被**复制/拖尾**（亮斑面积变大），正是「纯亮度、时段相同」的形态。
   怎么验：① `RippleSubtle Distort.mat:39` 的 `_DistortionStrength` 置 0（`:197` 的 alpha 不用它 ⇒ 干净地只关偏移），若 18 条回到 1.0 即实锤；② `:185` 的 `d.rg` 改 `d.ag` 复扫。
   旁证（读出来的）：解出的 `RippleSubtle.png`（128×128 RGBA，我方与原版**同一 IHDR**）整体呈粉白 = R≈B=1、G<1，**正是 DXT5nm 的特征**；但「平台侧格式」没有元数据可直接证（见三）。
2. **软粒子淡出把 `.y`(0.39) 当世界单位淡出距离 ⇒ 我们的淡出几乎不生效，alpha 系统性偏高**（推，量级吻合）
   `.shader:208`：`saturate((sceneEye-partEye)/max(1e-4,_Depth_And_Fallof.y))`；材质值 `.mat:45 (16.31,0.39)`。
   为什么更亮：若原版是 `pow(saturate(diff/_Depth_And_Fallof.x),_Depth_And_Fallof.y)`（名字「Depth And Fallof」= 距离 + 衰减指数，`.x=16.31` 当距离），d=1/2/5 单位时原版 alpha≈0.34/0.44/0.63，我们恒为 1 ⇒ **1.6–3.0×**，与观测 ×1.5–2.9 同量级。对照：`Heat Distortion.mat:44 (1.18,1.31)` 两种读法只差 <1.2×（作者明显在调同一个东西）。
   怎么验：把 `:208` 换成 pow 版（或单变量地把 `.y` 临时改 `.x`）复扫那 18 条。
3. **抓屏内容/时刻差：我们采「上一帧全合成（含亮部）」，原版 GrabPass 采「画到它那一刻」**（推）
   `.shader:191-195`（整份直接采，无钳制）+ `GrabPassTransparentFeature.cs:21-30` 自认「这是近似，不是等价」。
   为什么更亮：拷贝里含上一帧的全部高光/自身亮粒子，被扭曲物**原样贴回**；原版当时那份还没画到后面的亮部 ⇒ 我们位移的是更亮的内容。
   怎么验：把 `passEvent` 挪到 `AfterRenderingOpaques`（或对同帧内容拷贝）看方向/量级是否变。
4. **`IN.color.a` 这一层乘数**（猜的，需 A/B 干预实测）
   `.shader:197`。原版 CB 里**没有任何颜色/色调属性**，唯一 alpha 来源是贴图与顶点色 ⇒ 原版是否也乘顶点色 alpha 无从对照。若导出侧粒子网格的顶点色 alpha 恒为 1（未核，见三），我们的 alpha 就只由贴图 `.a` 决定，比原版多/少一层。
   怎么验：`:197` 改成 `d.a`（去掉顶点色）单变量复扫。
5. **`_USEMASK`/`_SOFTPARTICLES` float=1 但关键字缺失 ⇒ 我们整段跳过「乘 mask」「乘 fade」**（这一条是**直接量到的**，不是猜）
   我们对这两个功能的开关**只认关键字**（`.shader:199`、`:204`；CBUFFER 里也没有这两个 float），而原版 CB 里它们是**活值**。
   6/11 份 Distort 材质踩中：`Heat Distortion.mat:41,42`、`Distortion Star.mat:42`、`Distortion Star Low Strength.mat:41,42`、`Explosion Distort Spherical.mat:41,42`、`Trail Distort 1.mat:36,41`（`_Mask` 都挂了真贴图）——`m_ValidKeywords` 均为空。
   为什么更亮：mask 与软粒子淡出**都是把 alpha 乘小**的项，整段跳过 ⇒ 我们偏亮。**对那 18 条（`RippleSubtle Distort.mat`：`_USEMASK=0`、`_Mask`=null、关键字 `_SOFTPARTICLES` 有）不生效**，但它是 45 条抓屏里另外 3 条（`特效E组_逐效果根因表.md:157`：Heat Distortion×2、Distortion Star Low Strength×1）的候选，且随时会踩到。
   怎么改：把 `#ifdef` 换成判 float（并把这三个 float 加进 CBUFFER——**原版 CB 里就有**），或给这些材质补关键字。**这条同时是一条通用的导出侧隐患：关键字与 float 不同步。**

## 三 · 查不到的 / 排除过的（别用「大概是」）

- **原版每个属性的确切公式读不到**：DXBC 段只有 `ISGN/OSGN/SHDR`，无 RDEF（`工具/_dump_shaders_batch.py` §六.1），常量缓冲布局头只给**名字**，没有类型/偏移/取值；没跑反汇编（`disasm_va.py`/`resolve_va.py` 是 C# 侧按 VA 查方法名，与 shader 无关）。⇒ 候选 1/2/3 的「原版怎么算」全是推的。
- **原版 `_GrabPassTransparent` 的填充者/时刻**：字节码里只有名字。`资料` 全目录搜过 `_GrabPassTransparent`/`DistortProbe`，只有 `GrabPassTransparentFeature.cs` 与三份根因表，**没有原版侧证据**。
- **`RippleSubtle.png` 在平台侧的真实格式（DXT5nm？BC5？）没有直接证据**：`素材/Warpforge原版/**/Texture2D/` 下**只有解出的 PNG、没有任何 json 元数据**（`ls | grep -c json` = 0）。按 `m_TextureFormat`、`textureFormat`、`RippleSubtle` 三种名字在 `资料/**.md`、`素材/**` 里搜，0 命中。DXT5nm 结论只有「属性 flags 含 `Normal`」+「解出图像偏粉（R≈B=1）」。⇒ **推的**。
- **没跑**：`_dump_shaders_batch.py`（批处理，明令禁）、Unity、任何写操作。
- **没查**：我方导出侧粒子网格是否写顶点色 alpha（`IN.color.a` 的实际取值）——它是候选 1/2/4 的放大器；`资料` 里没找到这项的记录。
- **已排除（带理由）**：`_DistortTex_ST`/`_Mask_ST`（11/11 材质 identity）、`_ScreenParams` vs `_ScaledScreenParams`（`m_RenderScale:1` 等价）、混合的 alpha 通道差（RGB 逐位相同）、缺 MotionVectors/DepthNormalsOnly（与亮度无关且被材质禁用）、`_CameraOpaqueTexture` 回退支（Feature 已在 PC/Mobile renderer 注册，非主路径）、Standard 残留死值（**我方属性表里 0 个 ⇒ 一个都没被读活**——这是与 `WFParticlesExtraColor` 那次不同的结论）。

## 四 · 两条给主对话的提示（子代理原文）

① 18 条里凡带 ⭐（覆盖翻倍/循环发射器/寿命差异，见 `资料/特效E组_逐效果根因表.md:157` 的 45 条清单）的，是**独立混淆**，A/B 时要单独拆。
② 若要一次性判「是不是偏移场」，最便宜的实验是 `RippleSubtle Distort.mat` 的 `_DistortionStrength: 0.1 → 0`（只影响 :185 的偏移，不影响 :197 的 alpha）。
