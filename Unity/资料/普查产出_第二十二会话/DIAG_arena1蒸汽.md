# DIAG battlearena1 蒸汽粒子差异

> 只读诊断（第二十二会话）· 未跑 Unity · 未改任何生产文件。判据 = `d:/2/新解包资源/assets_full/`（新解包）+ 工程内清单/材质 + 实拍与 preview 逐像素。

## 一、结论（差异主因排序）

1. **那团东西不是 `Steam` 三颗，是 `Dust Floor (3)`**（我们清单 `idx 30`）—— 靠**投影定量反证**：按相机 `(100, 2.222, −13.572)` + `lensShiftY=−0.205` 算，它的发射体落在屏面 `(−0.03, 0.81)`，**与候选中心 `(0.03, 0.77)` 同一个地方**；全 34 颗里只有它在那儿（`Steam` 三颗在 `(0.82, 0.31)` 一带）。
2. **逐字段对照的结果是「全部一致」** —— 我核了 ~40 个 `ParticleSystem` 字段、全部材质属性、UV 翻页、顶点流、以及 `smokeysteam.png` **逐字节**。⇒ **差异不在粒子参数**，在**渲染路径**（§四）。
3. **「硬直的水平切边」= 地板把这张 billboard 的下半张切掉**（`ZTest LEqual`）。这不是我们特有的错，**原版的几何一模一样**；我用同一套投影**算出那条线在屏面 y≈0.807、实测 0.802–0.805**（§五）—— 这一条既定性又定量地把对象钉死。
4. **我们之所以「看着是缺陷」而原版不是**：原版靠 **软粒子**（`_SOFTPARTICLES_ON` + `_SoftParticleFadeParams=(0,1,0,0)`）在贴近地面处把 alpha 淡出（`fade = saturate(sceneZ − thisZ)`，**1 世界单位的衰减**）⇒ 切边被淡掉、整团也随之变暗；**我们那条淡出没见效**（根因未钉死，§八）。
5. **旁证（同一把尺子）**：同材质的 `Steam` 三颗（离地 1.9–2.9）在 2026-09-24 修 `_EMISSION` 后已与原版**差 2%**（135.5 vs 138.4）；同材质、同 emission 的 `Dust Floor` 家族却**偏亮 ≈2.4×**。两者唯一的系统差别就是**离地高度** —— 与「软粒子按场景深度淡出」完全自洽。

## 二、原版侧

- 对象路径 = **`Scenario/Particles/Dust Floor (3)`**（`GameObject/Dust Floor (3).json` · `Transform_1427` · **`ParticleSystem_1676`** · **`ParticleSystemRenderer_1732`**，包 = `bundle_scenes_scenes_battlearena1`）。
- 变换：`m_LocalPosition (−10.26, 4.91, −11.51)` · `m_LocalScale 0.41663` ⇒ 世界 ≈ **(95.89, −0.005, −8.365)**（与清单 `#30` 逐位吻合）。
- 材质 `SmokeySteam01`：`bundle_battlesharedresources_assets_all/Material/Material_5962499072093226969.json`
  · shader = **`Universal Render Pipeline/Particles/Unlit`**（`Shader_8918850207803687531`）· `m_CustomRenderQueue 3000`
  · `m_ValidKeywords = [_EMISSION, _FADING_ON, _FLIPBOOKBLENDING_ON, _SOFTPARTICLES_ON, _SURFACE_TYPE_TRANSPARENT]`
  · `_BaseColor 0.603774` · `_EmissionColor 0.481132` · `_EmissionMap = None` · `_SrcBlend 5 / _DstBlend 10 / _ZWrite 0 / _Cull 2`
  · `_SoftParticlesEnabled 1` · **`_SoftParticleFadeParams (0, 1, 0, 0)`** · `_CameraFadeParams (0, ∞, 0, 0)`。
- 贴图 `smokeysteam`：`bundle_battlesharedresources_assets_all/Texture2D/smokeysteam.png`（2048×2048 RGBA）。

## 三、我们侧

- 对象 = 清单 **`battlearena1_manifest.json` → `particles[30]`**（`go = "Dust Floor (3)"`），
  由 `WarpforgeArena1/Editor/ArenaBuilder.cs` 的生成路建出（唯一数据源 = 同一份清单；旁挂 `gen_arena_*` 只灌 `pivot/vertexStreams/applyActiveColorSpace` 三项，值与原版一致）。
- 材质 = `MyGame/Assets/WarpforgeArena1/arenas/battlearena1/Materials/PS_SmokeySteam01.mat`（**按原版材质重建的一材一份**）
  · shader 同一件 URP `Particles/Unlit`（guid `0406db5a14f94604a8c57ccfbc9f3b46`）
  · `m_ValidKeywords = [_FADING_ON, _FLIPBOOKBLENDING_ON, _SOFTPARTICLES_ON, _SURFACE_TYPE_TRANSPARENT]`
  · `_BaseColor 0.6037736` / `_EmissionColor 0.4811321` / `_SoftParticleFadeParams (0,1,0,0)` / `_CameraFadeParams (0,∞,0,0)` / `_SrcBlend 5 / _DstBlend 10 / _ZWrite 0` —— **与原件逐字相同**。
  · ⚠️ 唯一「我们自己的机制」= **`_EMISSION` 写不进 `.mat`**，改由运行时组件 `WarpforgeVFX/ArenaParticleKeywords`（`ArenaBuilder.cs:774` 建场 / `:1045` 渲图两条路都刷）补上。
- 贴图 `arenas/battlearena1/Textures/smokeysteam.png` —— **与原版逐字节相同**（2048×2048，`(a==b).all() == True`）。

## 四、逐字段对照表（原版 = `ParticleSystem_1676` / 材质 pid 5962499072093226969；我们 = 清单 `particles[30]` / `PS_SmokeySteam01.mat`）

| 字段 | 原版 | 我们 | 差异 | 出处 |
|---|---|---|---|---|
| `lengthInSec` | 5.0 | 5.0 | — | PS_1676 / 清单 `duration` |
| `simulationSpeed` | **0.5** | 0.5 | — | 同上 |
| `looping` / `prewarm` / `playOnAwake` | T / T / T | T / T / T | — | 同上 |
| `moveWithTransform`(=simulationSpace) | 1 (Local) | 1 | — | 同上 |
| `scalingMode` | 1 (Local) | 1 | — | 同上 |
| `startDelay` | 0 | 0 | — | 同上 |
| `InitialModule.startLifetime` | TwoConstants **1–3** | `[1,3]` | — | 同上 |
| `InitialModule.startSpeed` | Constant **1.0** | `[1,1]` | — | 同上 |
| `InitialModule.startSize` | TwoConstants **1–2** | `[1,2]` | — | 同上 |
| **`InitialModule.startColor`** | `mode=Color`, **maxColor `(0.3774, 0.3774, 0.3774, a 0.7725)`** | `[0.377358×3, 0.772549]` | — | 同上（`minColor` 是出厂残留，见 `战场13场…:207`） |
| `InitialModule.startRotation` | TwoConstants 0–6.28319, `randomizeDirection 0.5` | 同 | — | 同上 |
| `InitialModule.gravityModifier` | **−0.02** | −0.02 | — | 同上 |
| `InitialModule.maxNumParticles` | 1000 | 1000 | — | 同上 |
| `EmissionModule.rateOverTime` | Constant 10 | 10 | — | 同上 |
| `EmissionModule.rateOverDistance` | 0 | 0 · `bursts []` | — | 同上 |
| `ShapeModule` | type 4(Cone) · radius **0.02** · angle **8.54** · arc 360 · thick 1.0 | 同 | — | 同上 |
| `ColorModule`（colorOverLifetime） | alpha 4 键：`0→0, 4257/65535(=0.065)→1, 22822/65535(=0.3482)→1, 1→0`；RGB 全白 | **逐键相同** | — | 同上 |
| `SizeModule`（sizeOverLifetime） | `curve`：t0 **0.336283** → t1 **1.0**，`separateAxes=false` | 同 | — | 同上 |
| `RotationModule`（rotationOverLifetime） | **z** TwoConstants **±0.174533**, x/y 0 | 同 | — | 同上 |
| `UVModule` | on · **tilesX 8 · tilesY 8** · animType 0 · timeMode 0 · **fps 30** · cycles 1 · row 0 | 同 | — | 同上 |
| `ParticleSystemRenderer.m_RenderMode` | **0 = Billboard** | 0 | — | PSR_1732 / 清单 `renderMode` |
| `m_SortMode` | 1 (ByDistance) | — | 未接（4 个 `Dust Floor` 同值） | PSR_1732 |
| `m_SortingFudge` | **0.0** | 0.0 | — | 同上 |
| `m_RenderAlignment` | **0 = View** | 0 | — | 同上 |
| `m_Pivot` | **(0, 0, 0)** | `[0,0,0]` | — | 同上 |
| `m_MinParticleSize / m_MaxParticleSize` | 0.0 / **5.0** | 0.5 | ⚪ 故意不设（`ArenaBuilder` 已写理由） | 同上 |
| `m_UseCustomVertexStreams` / `m_VertexStreams` | **True / `[0,1,3,4,5,8]`** | True / 同 | — | 同上（A1557 已接 `SetActiveVertexStreams`） |
| `m_ApplyActiveColorSpace` | False | False | — | 同上 |
| 材质 `m_ValidKeywords` | 含 `_EMISSION` + `_SOFTPARTICLES_ON` + `_FLIPBOOKBLENDING_ON` + `_FADING_ON` | 同样四个（`_EMISSION` 运行时补） | — | 材质 pid 5962499072093226969 / `PS_SmokeySteam01.mat` |
| `_BaseColor` / `_EmissionColor` | 0.603774 / 0.481132 | 0.6037736 / 0.4811321 | — | 同上 |
| `_SoftParticleFadeParams` | (0, 1, 0, 0) | (0, 1, 0, 0) | — | 同上 |
| 贴图 | `smokeysteam` 2048² | **逐字节相同** | — | `bundle_battlesharedresources_assets_all/Texture2D/` vs `arenas/battlearena1/Textures/` |

⇒ **表中「差异」列全空**。没有一条参数能解释画面差；改任何一条都会**把对的改坏**。

## 五、「硬直切边」的候选与逐条判定

| # | 候选 | 判定 | 判据 |
|---|---|---|---|
| ① | `renderMode` = HorizontalBillboard（会裁一半） | **排除** | 原/我们都是 **0 = Billboard**（`PSR_1732.m_RenderMode` / 清单）；Horizontal 是 3 |
| ② | 贴图底下是硬的 | **排除** | `smokeysteam.png` **与原版逐字节相同**，且原版用同一张 |
| ③ | quad/网格比贴图区大 | **排除** | 粒子走 Unity 内置 billboard quad，没有自建 mesh（`m_Mesh` 为 0） |
| ④ | 材质用了不透明混合 | **排除** | `_SrcBlend 5 / _DstBlend 10 / _ZWrite 0`，与原版逐字相同，且 `.mat` 写着 `RenderType: Transparent` |
| ⑤ | **地板把 billboard 下半张 ZTest 掉** | ✅ **判定成立（定量）** | 发射体世界 `(95.89, 0, −8.365)`；相机 `(100, 2.222, −13.572)`、vFOV 46.397、`lensShiftY −0.205` ⇒ 该处 `y=0` 平线投影到 **屏面 y ≈ 0.807**；实测硬边 **0.802–0.805**（`_tmp_view` 之外临时裁剪，逐行扫描）。**同一条几何原版也有**，不是我们独有的错 |
| ⑥ | **软粒子淡出在我们这边没生效**（⇒ 切边不淡、整体偏亮） | 🔴 **最可能、但未钉到根因** | 见 §八 |

⚠️ **⑥ 的证据链**：原版 `fade = saturate(1 × (sceneZ − thisZ))`（`ShaderLibrary/Particles.hlsl:104-116`，`far=1` 来自 `_SoftParticleFadeParams.y`）；
(a) 原版那条边是**渐隐**、我们的是**平台→断崖**（同列扫描：我们 `108,163,157,151,146,132,90,54,12`；原版 `58,68,48,28`）；
(b) 同材质同 emission 的 `Steam` 三颗（离地 1.9–2.9）**已对上原版 2%**，而贴地的 `Dust Floor` 家族**亮 ≈2.4×** —— 差别只在离地高度。

## 六、同族粒子清单（同材质/同贴图）

- **同材质 `SmokeySteam01` + 同贴图 `smokeysteam` + 同关键词集合**：`battlearena1` 里 **7 颗** = `Dust Floor` `(1)` `(2)` `(3)`（**idx 33 / 31 / 29 / 30**）+ `Steam` `Steam (1)` `Steam (2)`（idx 18/17/16）。**改法要一次覆盖这 7 颗**，别只修 idx 30。
- **更外一圈（同带 `_SOFTPARTICLES_ON` 的）**：`battlearena1` **16/34** 颗 —— `SmokeEffect ×2`(`SmokeLight Fade`) · `SmokeEffect`(`SmokeySteam No Color`) · `RisingSteam`(`wispySmoke Additive Soft`) · `TinyFlames` · `TinyExplosion_Far/_Close` · `Shockwave ×2`。
- **13 场总量**：带 `_SOFTPARTICLES_ON` 的 = **118 颗**（arena1 16 · tauviorla 29 · arena2 17 · blacklegion 16 · leviathan 15 · astramilitarum 9 · sororitas 6 · arena3/aeldari/darkangels/genestealers 各 2 · emperorschildren/spacewolves 0）。
- §5①「是不是替身/兜底」：**不是**。材质/贴图/shader 都是**从原版原件重建**的（贴图逐字节同、属性逐条同），清单里没有 fallback 标记；唯一自建机制是 `_EMISSION` 的运行时补写。

## 七、改法建议（逐条：改哪里 / 改成什么 / 风险）

1. **不要动任何粒子字段**（§四 全一致）—— 改 `startColor`/`startSize`/`rotation`/`UV` 任何一条都是「把对的改坏」，且会破坏 13 场共用的同一套抽取口径。
2. **先做一次 2 分钟的 A/B（判 ⑥ 真假的唯一廉价判据）**：在 `PS_SmokeySteam01.mat` 或运行时把 **`_SOFTPARTICLES_ON` 关掉**再渲一次 `RenderPreviewFromCLI` —— 关掉后**画面一点不变** ⇒ **⑥ 成立**（淡出那条路是死的），顺着 `ArenaParticleKeywords` → 材质实例 → `_CameraDepthTexture` 往下查；关掉后**明显变化** ⇒ ⑥ 不成立，改查「原版那颗粒子当时是不是本来就没到地板以下」（= 相位/帧差，属 `A1615` 家族）。
3. **补一条不出声的自检**：现在没有任何断言盯「软粒子到底有没有生效」—— 建议加一条**结构上不可能自证**的断言（7 颗 `SmokeySteam01` 的 `_SOFTPARTICLES_ON` 关/开各渲一次，像素差必须非零）。
4. **风险**：`_CameraDepthTexture` 在 `RenderPreview`（渲到 RenderTexture）这条路上有没有被真正生成，**我没有实测**（§八）；若 A/B 证明是它，修的是**渲染设置**，不是 `.mat` —— ⛔ 别去改 `_SoftParticleFadeParams`（那是原版值，改了就与原版不符）。

## 八、没查清的部分（还差什么）

1. 🔴 **⑥ 的根因没钉死**：我没有跑任何 Unity（调度台占着）。`PC_RPAsset.m_RequireDepthTexture = 1`、`Assets/Settings` 里没有任何地方关深度图（`PC_Renderer.m_DepthPrimingMode: 0` 是另一件事），所以「深度图没有」只是**假设**，⛔ 别当结论写进正本。要不要真判，需要一个探针（或 Play 里进一次）读 `cameraData.requiresDepthTexture` 与 `Shader.GetGlobalTexture("_CameraDepthTexture")`。
2. **相位/翻页帧的贡献没分出来**：`smokeysteam` 是 **8×8 翻页图**（`uvFps 30`、`cycles 1`），不同模拟相位显示**不同的帧**、密度天然不同。4 相位取中位能压掉大部分，但压不掉「两套模拟的 prewarm 相位整体不同」（`WF_PSPREWARM` 是我们自己模拟的）。⇒ 要用 §七·2 的 A/B 先把它排除。
3. **`m_SortMode`（ByDistance）我们没接**（4 颗 `Dust Floor` 都是 1）—— 只影响粒子间自排序，**没证据**它跟这条有关，如实记着。
4. **搜过的包/词**（供复核）：包 = `bundle_scenes_scenes_battlearena1` · `bundle_battlesharedresources_assets_all` · `bundle_shaders_assets_all` · `bundle_battleprefabs_vfxandmisc_assets_all`；词 = `Dust Floor` `Steam` `SmokeySteam` `smokeysteam` `SOFTPARTICLES` `_SOFTPARTICLES_ON`。
   ⚠️ 本报告**没有**用旧解包 `d:/2/解包整理/`（其 650 张同名 PNG 与新解包不同）。
