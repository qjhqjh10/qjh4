# B9 · J3SH · shader 近似映射对账（287 个走 `Replacements` 的原版材质 → 32 条映射）

> 执行代理 **J3SH** · 2026-10-26（第八十四会话）· **只读**：未跑 Unity、未改 `.cs`/材质/prefab/主账表、未动 git、未碰 `d:/2`（只读）。
> 本轮产出**只有两个文件**：本文 + `判据/B9_shader近似映射_对账.tsv`。
> 口径：⛔ 不做截图并排比 / 不做像素结论（用户 2026-10-26 口径：特效画面由用户自己看，我们只做代码层）。
> 判据只用：`WarpforgeShaderMap.cs`（映射与白名单）· `OriginalShaderBlendTable.cs`（**原版 pass 状态实读表** + **原版声明属性表**）· 我们 24 个 `.shader` 的 Properties/pass · 我们的 prefab 里 `WFMatDef` 块 · `assets_full` 的 Material/Shader 序列化 JSON。

---

## 一、结论（先看这段）

### 1.1 最重要的一条：**「287 个材质走近似」在正常态是【休眠】的**

`WarpforgeShaderMap` 里 **32 条 `Replacements`** 中，**31 条的同一个名字也在 `UseOriginal` 里**（唯一例外 `Hidden/LUTBlender`，而它**没有任何材质**、是运行时 `new Material` 出来的全屏 LUT）。
`TryResolve` 的顺序是**白名单优先**（`WarpforgeShaderMap.cs:548-594`：`wantOriginal` 先查 bundle，取不到才回落 `Replacements`）⇒ **只要 `StreamingAssets/WarpforgeVFX/` 那几个 shader 包在场（玩家构建里必在），那 287 个材质用的是原版编译 shader，不是我们的替身。**

⇒ **V1 §八·3 那句「287 个材质是视觉暴露面」需要就地收窄**：这 287 个**全部**能在正常态解析到原件（`UseOriginal` 64 条、`ShaderResolveProbe` 断言逐条盯着，注释记 64/64 走原件）。它们的近似替身**只在兜底路**（新克隆没有 `StreamingAssets`、或某个包加载失败）与诊断开关 `WFBIND_FORCE_BUILTIN` 下才生效。

### 1.2 **真正的活暴露面 = 另一个 35 个材质（P0）**

同一批 prefab 里还有 **35 个材质，`WFMatDef.shader` 字段里写的已经是【我们替身的名字】**
（`WarpforgeVFX/FX/MultiRay` 12 · `WarpforgeVFX/Particles/Extra Color EC` 9 · `WarpforgeVFX/Particles/Extra Color` 6 · `WarpforgeVFX/FX/UnlitUVScroll` 3 · `WarpforgeVFX/FX/TrailShader1` 2 · `WarpforgeVFX/FX/Distortion` 1 · `WarpforgeVFX/FX/TrailShaderFading` 1 · `WarpforgeVFX/Matcap/Matcap` 1 = **35**，涉及 **54 张卡**）
>
> ⚠️ **2026-10-26 E8SH 就地更正（逐 prefab 现读，铁律 5）**：真实规模 = **43 个基材质 · 76 个不同材质名（34 个带 `__<hash>`）· 417 个 `WFMatDef` 块 · 343 个 prefab**（EC EC 是 **14** 个基材质不是 9、`Extra Color` 是 **8** 不是 6）。下面表里的「材质数/prefab数」两列也随之偏低。逐族现读数与反查覆盖证明 → `报告/B9_E8SH_shader硬锁修复.md` §二·1。

它们**永远走不到原件**，并且：

1. `InferBlend` / `InferCull` / `InferZTest` 都是**按原版名查表**（`OriginalShaderBlendTable` 的键是原版名）⇒ 替身名查不到 ⇒ **三格都不覆盖**，落到替身默认值或材质残留值；
2. 孤儿值闸 `OriginalShaderBlendTable.OriginalProperties(shaderName)` 对替身名返回 **null**（表里没有）⇒ **不过滤**（`WarpforgeEffectBinder.cs:407` 那段「读不到 ⇒ 不过滤」）⇒ 材质上那批**内置 Standard 的残留值**（`_SrcBlend/_DstBlend/_ZWrite/_Surface/_Metallic/_Glossiness/_Mode` …）会灌进替身**声明了的**同名属性里 —— 正是 2026-09-12 那次「加法发光变不透明」的同形状风险。

**已核的好消息**：这 35 个材质的 `_SrcBlend/_DstBlend` 在 prefab 里带的是**真值**（5/10 或 5/1，不是 1/0 残留）⇒ 状态这一格**当前没坏**（逐条读数见 §二·1）。但这条「没坏」是**材质数据碰巧对**，不是解析链保证的。

### 1.3 32 条映射的分类（判据见 §二、逐条见 TSV）

| 分类 | 条数 | 说明 |
|---|---:|---|
| **等价**（属性集对齐 + 状态对齐 + 替身按字节码重写或为超集） | **21** | 走原件零差异；兜底路按现有判据也等价 |
| **有实质差异**（属性缺 / 族语义不符 / 语义不同源） | **6** | `Particles/Standard Unlit` · `Legacy Shaders/Particles/Alpha Blended` · `Legacy Shaders/Particles/Anim Alpha Blended` · `UI/Additive` · `Everguild/FX/Particle Distortion Affect Transparents` · `Hidden/LUTBlender` |
| **判据不足（近似但状态能对上，未 A/B）** | **5** | `Mobile/Particles/Additive` · `Mobile/Particles/Alpha Blended` · `Legacy Shaders/Particles/Alpha Blended Premultiply` · `Everguild/FX/Alpha Masks Two Layer` · `Everguild/UnlitAmbient` |
| **判不了** | **0 条整条** | 但上面 5 条 + 3 条局部（keyword-vs-uniform、原件-vs-重建、批处理能否渲）**局部判不了**，见 §三 |

风险档计数（在 32 条映射行上）：**P0 = 0** · **P1 = 9** · **P2 = 23**；另有一类**不属于映射行**的 **P0 = 35 个材质**（§1.2）。

### 1.4 最可能「看起来不对」的前 6 名（按影响面 + 差异硬不硬）

| # | 条目 | 为什么 | 影响面 |
|---|---|---|---|
| 1 | **35 个硬锁材质**（`d.shader` = 替身名） | 永远不用原件；三格状态修正与孤儿值闸**双双失效** | 35 材质 / 54 卡 |
| 2 | `Particles/Standard Unlit` → `WarpforgeVFX/Particles/Extra Color` | **缺 22 条真实属性**：软粒子淡出（`_SoftParticlesEnabled` + 3 个 fade 距离）· UV 翻页（`_FlipbookMode`/`_Mode`）· 颜色模式（`_ColorMode`/`_ColorAddSubDiff`）· `_BlendOp` · 扰动（`_DistortionEnabled/_DistortionStrength/_DistortionBlend`）· `_EmissionEnabled/_EmissionMap` | 6 材质 / 17 prefab / 46 卡 |
| 3 | `Legacy Shaders/Particles/Anim Alpha Blended` → `Extra Color` | 原版是**序列帧 UV 动画**族，替身**完全不滚 UV**；另缺 `_InvFade`/`_TintColor` | 1 材质 / 8 prefab / 2 卡 |
| 4 | `Legacy Shaders/Particles/Alpha Blended` | 缺 `_TintColor`（材质真带值）⇒ 颜色 tint 丢 | 1 材质 / 4 prefab / 12 卡 |
| 5 | `UI/Additive` → `Extra Color` | 原版声明**整组 Stencil**（`_Stencil/_StencilComp/_StencilOp/_StencilReadMask/_StencilWriteMask/_ColorMask/_UseUIAlphaClip`），替身一个都没有 ⇒ 兜底路 UI 遮罩失效 | 本包 0 材质；UI 侧 7 处引用 |
| 6 | `Particle Distortion Affect Transparents` → `WFDistortion` | 原件是**真·抓屏扭曲**（采样 `_DistortTex` 重采样场景色），替身是**按属性表重建的另一套公式**（原件 ShaderGraph HLSL 与字节码都被剥） | 7 材质 / 40 prefab / 48 卡（另有 233 prefab 声明面） |

---

## 二、逐条证据

### 2.1 硬锁 35 的取证（P0）

**怎么判「原件应该是哪个」**：原版材质的 `m_Shader` 是 PPtr。同一 PPtr 上挂着多个材质，其中一部分材质的 `d.shader` 仍是**原版名**、另一部分是**替身名** ⇒ 用前者给后者定性。8 组可直接落地的硬证（覆盖 35 个全部）：

| PPtr（`m_FileID`, `m_PathID`） | 本包内可直接解出的原件名 | 被写成替身名的材质数 |
|---|---|---:|
| (0, −8640852129309073691) | `Everguild/FX/Multi Ray`（本包 `Shader/` 实读） | 12 |
| (0, 8442397235057580651) | `Everguild/FX/TrailShader_1` | 2 |
| (0, 684306895731615801) | `Everguild/FX/TrailShader_Fading` | 1 |
| (0, 4341849964770849447) | `Everguild/Matcap/Matcap Full Options` | 1 |
| (7, −418309281451410444) | `Everguild/FX/Extra Color`（同 PPtr 的 `Waterfall_ExtraColor` 仍是原名） | 9 |
| (7, 5091587426579848444) | `Everguild/FX/Unlit UV scroll` | 3 |
| (7, −3785611105477734646) | `Everguild/FX/Particle Distortion Affect Transparents` | 1 |
| (2, 2855819893914472268) | `Mobile/Particles/Additive`（同 PPtr 的 24 个材质仍是原名） | 6 |

**状态读数（逐条抽）**：`DCannon_Trail_MultiRay 1` → `_SrcBlend=5 _DstBlend=10 _ZWrite=0 _ZTest=4 _Cull=0`；`Flash1` → `5/1/0/4/2`；`FadingTrail_add` → `5/1/0/4/0`；`RippleSubtle Distort` → **一个都没有**（我们的 `WFDistortion` 写死 `SrcAlpha OneMinusSrcAlpha` + `ZWrite Off` + `Cull Back`，与原版烘死值 5/10/2 一致 ⇒ 这一条恰好安全）。
**孤儿值泄漏面也逐条核过**：替身 `WarpforgeVFX/Particles/Extra Color` 声明了 `_Color`/`_EmissionColor`/`_Cutoff`，但落到它名下的那 6 个材质（`Flash1`/`line_light`/`smokesoft_dark`/`bigsmoke_light`/`Lightning_001_add`/`Trail_Plasma_Stylized_2_Add`）**一个都没带 `_Color` / `_EmissionColor`**，关键字只有 `_SURFACE_TYPE_TRANSPARENT` ⇒ 这一族**没有残留值可漏**；`WFMultiRay` 名下的 12 个材质带的是本族自己的真属性（`_Color1.._Color4`/`_Scale_*`），不是 Standard 残留。
⇒ **当前状态没错**，错的是**解析路径**（原件取不到 + 表查不到 + 闸不过滤）。但「没坏」靠的是**材质数据碰巧对**，不是解析链的保证 —— 换一个材质的导出批次就会翻。

⚠ **根因未查清**：`EffectExporter.DefIndex`（`EffectExporter.cs:3008-3017`）取的是 `om.shader.name`（源材质），理论上应是原版名。写成替身名的最可能解释是**「同一实例上先 `ImportMaterial` 换掉了 `mats[i]`、随后再跑一遍导出」的往返污染**，但**没有找到直接证据**（见 §三·3）。

### 2.2 属性覆盖：32 条里只有 3 条真缺

逐族比「原版 `m_PropInfo.m_Props` 实读」vs「替身 `Properties` 块」，并**只用材质上真带值的那部分**加权重算：

| 结论 | 条数 | 明细 |
|---|---:|---|
| **缺 0 条真实属性** | 29 | 差额只出现在 `unity_Lightmaps/_Ind/_ShadowMasks`（原版声明、但是死值） |
| **真缺** | 3 | `Particles/Standard Unlit`（22 条，材质真带值 20）· `Legacy Shaders/Particles/Alpha Blended`（`_TintColor`）· `Legacy Shaders/Particles/Anim Alpha Blended`（`_InvFade`+`_TintColor`） |

几个反直觉但已核实的事实（⛔ 别按旧印象改）：

* `Everguild/FX/Extra Color` **只声明 18 条真属性**（`_Color` + `_MainTex` + 状态那批 + `_SOFTPARTICLES`/`_CastShadows`），**没有** `_Color1.._Color4`、没有 `_SecondaryTex`。`shaders_assets_all.bundle` 的 `m_PropInfo` 实读 = 21 条（含 3 个 unity_*）。
  ⇒ **`替代shader丢失的源属性_0917.md` 里那 202 条「Extra Color 丢的属性」不是本族的**，是「源 shader 不是 Extra Color、但导出期被指向了 Extra Color」的材质造成的（那份文档的「目标 shader」列，不是「源 shader」列）。
* `Mobile/Particles/Additive` / `Mobile/Particles/Alpha Blended` **只声明 `_MainTex`**（`Warpforge_unitybuiltinassets` 实读 1 条）。⇒ 材质上那 8 个 `_TintColor`、`_InvFade` 对这两族是**死值**。
  ⇒ `WarpforgeShaderLoader.cs:31-34` 那句「材质上的 `_TintColor`/`_InvFade` 这类属性只有原件才认」**只对 `Legacy Shaders/Particles/*` 那几族成立**，对 `Mobile/Particles/*` 不成立（顺手发现 §五·4）。
* `Everguild/Matcap/Matcap Full Options` 声明 32 条（含 `_EmissionColor`/`_EmissionTex`/`_BorderColor1,2`），而 `WarpforgeVFX/Matcap/Matcap` 声明 36 条 —— **替身是超集**，缺 0。
  ⇒ 旧账「`_EmissionColor` 在 matcap 族被丢 24 次」**在运行期不成立**：走原件时原件声明它；兜底路替身也声明它。那条只关于中间产物 `.mat`。
* 全仓 24 个自建 shader 里 `_ExtraSine`/`_FireNoise`/`_CardImage`/`_BumpMap` 确实没声明，但**32 条映射里一个都不需要**：
  `_ExtraSine` 只出现在 `Everguild/FX/ShieldVfx`、`_FireNoise` 只在 `Everguild/FX/Burning(Dissolve)`、`_BumpMap` 只在 URP 三族、`_CardImage` **全 0** ⇒ **这三族都不在 32 条里**（顺手发现 §五·3：V1 §八·3 那句措辞要更正）。

### 2.3 状态（混合 / 深度 / 剔除 / 队列）：32 条**全部对得上**

原版那一格来自 `OriginalShaderBlendTable`（**pass 状态实读**，不是猜）：
`Map`（srcBlend,dstBlend,zWrite）**78 条** · `CullMap` **78 条** · `ZTestMap` **84 条** · `DeclaredProps` **108 条**。
逐族比完的结果只有三类，全部已知且已建模：

| 形态 | 族 | 判定 |
|---|---|---|
| 原版**材质驱动**（pass 里写 `Blend [_SrcBlend][_DstBlend]`，表里故意不收） | Extra Color · Matcap ×2 · Unlit UV scroll · Multi Ray · Dissolve Mask · Alpha Mask ×2 · Premultiply Greyscale · Doomweaver · UnlitAmbient ×3 · `Particles/Standard Unlit`（pass2） | 替身也写成 `[_SrcBlend][_DstBlend]` + `[_ZWrite]`/`[_ZTest]`/`[_Cull]` ⇒ 材质值直接生效。**等价** |
| 原版**烘死**、替身也**写死同值** | Shine CVS(5/10,Off,4) · Distortion(5/10,Back,4) · Premultiply(5/10,Back,4) · TrailShader_1(5/10,Off,4) · TrailShader_Fading(5/10,Off,4) · DissolveAPB(5/10,Off,4) · RockDissolve(1/0,Back,4) · LegacyAdditive(5/1,Off,4,RGB) · Multiply(0/3,Off,4) · SpriteAdditive(1/1,Off,4) · EclipseTau(5/10,Off,4) | **等价** |
| 原版**烘死**、替身默认值与之不同、**靠 `InferBlend/InferCull/InferZTest` 补** | `Mobile/Particles/Additive`(5/1,0,4) · `Mobile/Particles/Alpha Blended`(5/10,0,4) · `Legacy ...Premultiply`(1/10) · `UI/Additive`(1/1, 0, **zTest 0**) | **兜底路等价**（三张表均命中）；⚠ **硬锁那 35 个材质走不到这里** |

队列：`renderQueue` 运行期一律由 `WFMatDef.renderQueue`（= `EffectExporter.TruthQueue` = 原版材质队列或原版 shader 默认队列）覆盖（`WarpforgeEffectBinder.cs:573`）⇒ 「原版 −1 / 我们 3000」那种差别**等价**（原版 shader 默认就是 3000；V1 §4.7 已定量：全量 101 条队列差里 88 条属于这一类）。
32 条映射行里，**Queue Tag 与原版默认队列逐条比完只发现 1 处不同**：`Everguild/Unlit Wind`（原版 2450 vs 替身 `Geometry` 2000；本包 **0 材质**，且正常态走原件 + `TruthQueue` 覆盖 ⇒ 无影响）。`Shader Graphs/Fx_RockDissolve` 的 2450 ↔ 替身 `AlphaTest`(2450) 一致。

### 2.4 关键字：一条真差异 + 一组「形态不同」

* 真差异：`Particles/Standard Unlit` 的 `_ALPHABLEND_ON` 替身没有（混合靠材质值，判据上无害，但原件那条分支的其余语义也一起没了 —— 与 §2.2 那 22 条同源）。
* 形态不同（**判据上等价，未 A/B**）：
  * `Everguild/FX/Alpha Masks Two Layer`：材质带 `_NOISE1CHANNEL_R/G/B/A`、`_NOISE2CHANNEL_*`、`_CHANNELSELECTOR_R` **10 条关键字**，替身文件里一条都没有 —— 替身改用 `_NOISE1CHANNEL`/`_NOISE2CHANNEL`/`_CHANNELSELECTOR` **三个 uniform 分支**（`WFAlphaMasksTwoLayer.shader` 里无 `Toggle`/`KeywordEnum` 行）。
  * `Everguild/Matcap/Matcap Full Options`：`_NOISECHANNEL_R` 同理（uniform `_NOISECHANNEL`）。
  * `_SURFACE_TYPE_TRANSPARENT`：替身多数不声明（混合已由材质值给）⇒ 死关键字。
* 注意 binder 已经有一道**同族撞车**的闸（`WarpforgeEffectBinder.cs:503-552`，按原版 `m_ValidKeywords` 关掉「默认档」那条）—— 这是 2026-10-25 X15 的修法，与本轮结论不冲突。

### 2.5 「287 走原件」的代码依据（一句话版）

`WarpforgeShaderMap.TryResolve`：
① `wantOriginal = UseOriginal.Contains(name) && !ForceBuiltInFor(name)` → ② 先 `WarpforgeShaderLoader.TryGetShader(name)`（三个包 `wf_shaders` / `wf_shaders_extra` / `wf_builtin` + `wf_tmp_shaders` + `wf_arena_*` 通配）→ ③ 取不到才 `Replacements` → ④ 再 `Shader.Find(name)` → ⑤ 最后再去 bundle 兜一次。
`UseOriginal` 现读 **64 条**，覆盖 32 条 `Replacements` 里的 **31 条**（唯一例外 `Hidden/LUTBlender`）。

---

## 三、没查清的部分（写清还差什么）

1. **「原版 shader 在我们这个 URP 工程里到底渲不渲得出来」未被证实** —— 这条一旦为假，§1.1 的「287 休眠」就整个翻掉（那 287 会退回替身）。
   现有证据是**一手实读**：`UseOriginal` 里 64 条（含 `Mobile/Particles/*`、`Hidden/Everguild/*`）全在走 bundle 原件，且 `ShaderResolveProbe` 只断言「解析到原件 + 不是故障色」。
   ⚠ 2026-10-23 已就地订正过「原版在批处理下渲品红」这条**理由本身没核过**（`资料/普查产出_1023/E4_R29分因与R1判据.md`）。
   **还差**：一次 `WFBIND_FORCE_BUILTIN` 的全量 sweep A/B（要起 Unity，本代理不许跑）。
2. **3 处「近似但等价」缺 A/B**：Mobile 族（`Mobile/Particles/{Additive,Alpha Blended}` ⇒ 通用 `Extra Color`）· `Alpha Masks Two Layer` 的 keyword-vs-uniform · `UnlitAmbient` 的 `_VertexColorLerpOn` 前提（网格有没有 COLOR 流，9 个材质未逐个核）。
3. **35 个硬锁的根因**：只证明了**状态**（`WFMatDef.shader` = 替身名）与**判据**（同 PPtr 的其它材质仍是原名），**没证明是谁写进去的**。
   候选：① 导出器往返污染（`EffectExporter.DefIndex` 取 `om.shader.name`，而 `ImportMaterial` 会先把 `mats[i]` 换成我们的 `.mat`）；② 某轮手工修复脚本；③ 跨包同名材质。**还差**：`EffectExporter` 的两次导出顺序取证 / `git log -S` 追那条 `shader: WarpforgeVFX/FX/MultiRay`。
4. **32 条以外的族不在本轮**（`Everguild/FX/ShieldVfx` 6 材质 / `Rays For Trail` 9 / `Burning(Dissolve)` 4 / `Spiral Trail FX` 1 / `Flow Shader` 2 / `Card Image` 族 / URP 六族 231+ … ）。它们不在 `Replacements` 上，但**兜底路 + 4 个未声明属性**（`_ExtraSine`/`_FireNoise`）就在它们身上 —— 建议下一轮单开一格。
5. **关键字只做了「字符串在不在替身文件里」这一层检查**，没做「原版这条关键字在原件里是不是真分支」的逐条判定（例：`_EMISSION` 在 `Unlit UV scroll` 上是死关键字，靠 `DeclaredProps` 没有 `_Emission*` 反推的，不是字节码证）。
6. **队列**只比了「我们替身的 Queue Tag / 原版 shader 默认队列 / 原版材质 `m_CustomRenderQueue`」三处，`_QueueOffset`/`_QueueControl` 的运行时语义没核（那份逻辑在 `ApplyDerivedParticleDefaults`，本轮没读）。

---

## 四、改动清单（**只有这两个文件**）

| 文件 | 性质 |
|---|---|
| `d:/4/Unity/资料/完全复刻/判据/B9_shader近似映射_对账.tsv` | 新建（**32 条映射行 + 8 条硬锁行**，一律 11 列；另加 2 行分节标题） |
| `d:/4/Unity/资料/完全复刻/报告/B9_J3SH_shader近似对账.md` | 新建（本文） |

**没有改**任何 `.cs` / `.shader` / `.mat` / `.prefab` / 主账表；**没有跑** Unity；**没有动** git。
复跑脚本（只读、临时目录）：`python -I -X utf8 d:/4/_tmp_view/j3sh/scan.py && ... an5.py && an2.py && gen_tsv.py`（`_tmp_view` 是 gitignore 的，会被清空 —— 结论以本报告 + TSV 为准）。

---

## 五、🔴 顺手发现（只报不改）

1. **`OriginalShaderBlendTable` 的 CullMap 实读 78 条，文件注释写「现读 79 条」**（`WarpforgeShaderMap.cs:470-474`）。差 1 条 —— 我没逐条核是哪一条被漏数（可能是注释写时的旧数），但**注释与实读对不上**这件事本身该订正。
2. **V1 报告 §四·7 引用的行号 `WarpforgeShaderMap.cs:161-230` 已过期**：白名单 `UseOriginal` 在 **71-256**、`Replacements` 在 **259-410**。V1 是同一批的产物，建议就地改（铁律 5）。
3. **V1 §八·3 那句「我们自建的 24 个 WF shader 里 `_ExtraSine`/`_FireNoise`/`_CardImage`/`_BumpMap` 完全没声明 —— 用到它们的材质如果落在这 32 条映射里」需要收窄**：实测这 4 个属性**只有 `_BumpMap` 出现在 32 条之外**（URP 三族），`_ExtraSine` 只在 `ShieldVfx`、`_FireNoise` 只在 `Burning(Dissolve)`、`_CardImage` 全 0 ⇒ **32 条里一个都不涉及**。
4. **`WarpforgeShaderLoader.cs:31-34` 的注释对 `Mobile/Particles/*` 不成立**：原文「这批全是 unlit 粒子 shader…材质上的 `_TintColor`/`_InvFade` 这类属性**只有原件才认**」—— 实读 `Mobile/Particles/{Additive,Alpha Blended}` 的 `m_PropInfo` **只声明 `_MainTex`**，而材质上带 `_TintColor` 的有 **16 / 2** 个、带 `_InvFade` 的有 **16 / 2** 个 ⇒ 这些值对这两族是**死值**（不是「只有原件认出」，是「原件也不认」）。
   `_TintColor`/`_InvFade` 真正活着的只有 `Legacy Shaders/Particles/{Additive,Alpha Blended,Anim Alpha Blended}` 三族（各声明 3 条）与 `…Premultiply`（2 条）。**结论不变**（走原件仍是对的），只是理由要改。
5. **`WarpforgeVFX/Particles/Extra Color` 一个替身顶 6 个原版族**：`Mobile/Particles/Additive` · `Mobile/Particles/Alpha Blended` · `Particles/Standard Unlit` · `Legacy Shaders/Particles/Alpha Blended Premultiply` · `Legacy Shaders/Particles/Anim Alpha Blended` · `UI/Additive`（+ 它自己那份 EC 走 `… Extra Color EC`）。
   ⇒ **任何一族的兜底修补都会碰到另外 5 族**（与旧记录「Extra Color 顶 8 个原版 shader」是同一条风险，只是现在是 **6**）。下次动它必须**按族拆**，别再改一份全体受害。
6. **「`d.shader` = 替身名」在 prefab 里的实际实例数比 35 多** —— ✅ **2026-10-26 E8SH 已查清**：现读 **417 块 / 343 个 prefab / 76 个不同名字（34 个带 `__<hash>`）/ 43 个基名**；`__<hash>` 变体的来源**与基名同源**（E8SH 按「替身名 + 材质基名」反查，**417/417 全部解析到原名，0 未覆盖**，其中 2 个基名要按名字分流：`IceTrail` → `Particles/Standard Unlit`、`smokesoft_dark` → `Mobile/Particles/Alpha Blended`）。**：按 `(材质名, shader)` 去重是 35 个原始材质，但按 prefab 内的 `WFMatDef` 块数是 **76 个**（41 个是导出器给同名不同源材质加的 `__<hash>` 变体）。**修的时候按名字修够不够，要先确认 `__<hash>` 变体的 shader 名来源**。
7. **`Particles/Standard Unlit` 的 cull 判据本来就标了「pass0 是 `gpuProgramID=-1` 的空 pass」**（`WarpforgeShaderMap.cs:499-501`）—— 本轮复核成立：`OriginalShaderBlendTable.CullMap` **故意没有它**，替身也不该硬写 cull。这条是对的，⛔ 别为了「表要全」把它加进去。

---

## 六、给下一波写手的施工顺序（**都要做，只分先后**）

判序依据 = **「当前会不会被看到」×「判据是否已定」**；⛔ 不用「影响小所以不做」这类分档。

| 序 | 做什么 | 为什么排这里 | 判据已在哪 | 需要起 Unity? |
|---:|---|---|---|---|
| **1** | **修硬锁材质的 `d.shader`**（✅ 2026-10-26 E8SH 已按「路 A」把**代码侧**做完：反查表 → 三格状态 / 孤儿值闸 / 派生默认值都拿得到原版表；**解析路径与数据侧仍待同步点**，见 `报告/B9_E8SH_shader硬锁修复.md`）（或给 `WarpforgeShaderMap` 加一张「替身名 → 原名」反查，让 `wantOriginal` 与三张表按原名查） | 唯一 **P0**、且是**解析路径**问题（不是 shader 质量问题）；修完这 35 个立刻免费拿到：原件 + 三格状态修正 + 孤儿值闸 | TSV 的「硬锁」小节（8 行，含 PPtr 对照）· `WarpforgeShaderMap.TryResolve:548-594` | 否（改数据/解析，秒级 typecheck 够） |
| **2** | **`Particles/Standard Unlit` 单开替身**：补软粒子淡出 / flipbook / 颜色模式 / `_BlendOp` 四组（22 条属性里材质真带值 20 条） | 32 条里**唯一**「属性大面积缺 + 有真卡引用」的一族 | §2.2 · `OriginalShaderBlendTable.DeclaredProps("Particles/Standard Unlit")` | 否（写 shader）；验收要 A/B |
| **3** | **`Legacy Shaders/Particles/Anim Alpha Blended` 单开替身**（序列帧 UV + `_TintColor` + `_InvFade`） | 族语义不符（不只是缺属性）：UV 完全不滚 ⇒ 序列帧效果会看成一张静止贴图 | `DeclaredProps={_MainTex,_TintColor,_InvFade}` · `Replacements:393` | 否 |
| **4** | **`Legacy Shaders/Particles/Alpha Blended` 补 `_TintColor`** | 一个属性、12 张卡；改 3 行 | `WFLegacyParticlesAlphaBlended.shader` | 否 |
| **5** | **`UI/Additive` 单开一份 UI 专用替身**（补 Stencil 组 + ZTest 0） | 本包 0 材质，但 UI 侧 7 处引用；兜底路上 UI 遮罩/裁剪会静默失效 | `DeclaredProps("UI/Additive")` · `ZTestMap=0` | 否 |
| **6** | **`Particle Distortion Affect Transparents` 跑 A/B**（`WFBIND_FORCE_BUILTIN=Distortion`） | 差异是**语义不同源**，不是缺属性 ⇒ 只能量；影响面 233 prefab（第二大） | `WarpforgeShaderMap.cs:240-244` 的待办原文 | **是**（主对话） |
| **7** | **`Hidden/LUTBlender` 跑 `LutBlendProbe.Run`（7 条断言）** | 唯一**永远走替身**的映射；「原版批处理渲品红」这条理由本身未证实 | `资料/普查产出_1023/E4_R29分因与R1判据.md` §④-1 | **是**（主对话） |
| **8** | **补三条「近似但等价」的 A/B**：Mobile 两族（最高覆盖面 666/153 卡）· `Alpha Masks Two Layer` 的 keyword-vs-uniform · `UnlitAmbient` 的 `_VertexColorLerpOn` 前提 | 判据上等价但**没量过**；这三条一旦不等价，覆盖面是最大的一档 | §三·2 | **是** |
| **9** | （并行、非阻塞）把 **32 条以外的族**补一格普查：`ShieldVfx` / `Rays For Trail` / `Burning(Dissolve)` / `Spiral Trail FX` / `Glow Shader` / 卡牌 3D 族 | 那批在 `UseOriginal` 里但也靠兜底；`_ExtraSine`/`_FireNoise` 就在那儿 | §三·4 | 否 |

> 另：本轮**没有**发现「32 条映射的混合/深度/剔除写错」这一类问题（原版 pass 状态与替身逐条比完，三类形态都判得清）。**要修的都不是「参数抄错」，而是「映射族选得糙」与「解析路径被绕过」。**
