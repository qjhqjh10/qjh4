# B113_G1 · 色彩空间（项目级）与 sRGB 旗标（资产级）判据

> 查判据代理 `B113_G1` · 2026-11-01 · 第一百一十一会话第 2 波。
> ⛔ 没跑 Unity · ⛔ 没动 git · ⛔ `d:/2` 一个字节没改 · ⛔ 没碰 `.cs` / `.shader` / `.mat` / `ProjectSettings/**` / 账 / 活状态表。
> 写盘三处：本文件（新建）· `报告/B113_G1.md`（新建）· **`CardPresentation/Resources/Art/ui_menu/Noise_Combined.png.meta` 的 `sRGBTexture: 1 → 0`**（白名单内，备份在 `_tmp_view/b113_g1_backup/`，`diff` 只 1 行）。
> 复跑脚本（临时件，非仓库正式件）：`d:/4/_tmp_view/b113_g1/{dump_meta,guids,consumers,mat_shaders,all_copies,tex_stats,sim_compress2,sim8192}.py`。

---

## 〇、一句话

1. **B-110（项目级）**：原版 **Gamma**、我们 **Linear**。**切到 Gamma 是对的方向**（它让「原版编译字节码」和「按原版数值灌的 Volume/材质」直接成立），
   但它是**一次全局观感重置**：动 **13 个自建 shader / 32 个 `LinearToSrgb` 调用点**、**14 份 Volume**、全部卡面与 UI 配色、以及**所有已做的目视验收基线**。
   ⇒ **建议：切，但作为独立批次（阶段二）**，本批只做阶段一（资产级，见 §三/§五）。
2. **B-115（38 条 sRGB）**：**B111_T1B 的「38 条全是真缺陷」不成立**。按「shader 怎么用这个值」重新判：
   **25 条真要改（其中 12 条是 0 引用的 VAT）· 10 条是项目级差异导致（Linear 下 sRGB=1 才是对的）· 3 条等价（SDF 图集只读 alpha，旗标不生效）。**
3. **`WarpforgeVFX/Textures` 的「RGBA32 vs BC7」前提错了**：我们那 20 张是 **`textureCompression: 1` = DXT（BC1/BC3）**，不是 BC7。实测模拟：**BC7 比 DXT5 高 4–7 dB**，其中真噪声/数据图（`NoiseNRM`）是唯一压了会明显变形的。

---

## 一、判据（先给规则，再给结论）

### 1·1 为什么原版的旗标不能照抄

Unity 的两个事实（本项目已两处记档：`判据/B108_AUD09_纹理格式.md` §1·4、`报告/B111_T1B.md` §〇）：

| 事实 | 出处 |
|---|---|
| **原版工程是 Gamma**：`m_ActiveColorSpace: 0` | `d:/2/tools/assetripper_out/ExportedProject/ProjectSettings/ProjectSettings.asset:56`（Unity **6000.2.6f2**） |
| **我们是 Linear**：`m_ActiveColorSpace: 1` | `Unity/MyGame/ProjectSettings/ProjectSettings.asset:50`（Unity 6000.3.23f1） |

在 **Gamma 工程里 `sRGBTexture` 旗标对采样不生效**（贴图一律按原始字节采）。
⇒ 原版那个字段只能读成「作者 **打算** 把它当什么用」（0 = 数据、1 = 颜色），**不能当成「我们该写什么」**。

### 1·2 三条判定规则（本文件所有结论都按这三条推）

记 `raw` = PNG 里的原始字节值（0..1），`dec(x)` = sRGB→linear 解码。

| 规则 | 条件 | 我们（Linear）该写什么 | 为什么 |
|---|---|---|---|
| **R1 · 值参与运算** | shader 对这个值做**算术**：`x*2-1` / `pow(x,k)` / 阈值比较 / `lerp(min,max,x)` / overlay/blend 公式 / 法线 `UnpackNormal` | **`sRGBTexture: 0`** | 原版在 Gamma 下拿到的是 `raw`，运算也是按 `raw` 算的。Linear 下 `sRGBTexture: 0` 让采样同样返回 `raw` ⇒ **逐位复现**。写 1 会拿到 `dec(raw)`，整条算式被非线性扭掉。 |
| **R2 · 值就是最终颜色** | 这个值**只**被乘进输出颜色（`col = tex * vcol * tint`），不做任何其它算术 | **`sRGBTexture: 1`（保持）** | 原版屏上结果 = `raw`。Linear 下 `sRGBTexture: 1` 走 `dec` → 线性混合 → 输出再 `enc` ≈ 恒等（tint=1 时逐位相同）。改成 0 反而变成 `enc(raw) ≈ raw^0.4545` ⇒ **整体偏亮**。 |
| **R3 · 值只走 alpha** | shader 只读 `.a`（SDF 距离场、alpha 遮罩） | **无所谓（等价）** | Unity 的 sRGB→linear 转换**只作用于 RGB，alpha 永远线性**。旗标对 `.a` 无影响。 |

**三条的公共前提**：`textureType: 0`（Default）。若改成 `NormalMap`，Unity 会强制线性 + DXT5nm 打包，那是另一条路（本批不动）。

### 1·3 判据的数据来源（都现读过）

| 用来判什么 | 读的是什么 |
|---|---|
| 每个资产的**真实消费者** | 扫 `Assets/**/*.{mat,prefab,unity,asset}` 里出现该图 guid 的条目，取 `.mat` 的 `m_Shader` 与 `m_TexEnvs` 槽名（脚本 `consumers.py` / `mat_shaders.py`） |
| shader 拿到值之后**干什么** | 逐个读 `WarpforgeVFX/Shaders/*.shader` 与 `CardPresentation/Resources/Shaders/*.shader` 的 `frag`（片段行号写在 §三表里） |
| 原版/我们各写的是什么 | `Assets/**/*.png.meta` 的 `sRGBTexture` / `textureCompression` / `textureType` / `platformSettings`（`all_copies.py`） |
| 压缩误差有多大 | 用源 PNG（= 原版 RGBA32 解码后）模拟 BC7-mode6 / DXT5 的量化模型，算 PSNR（`sim_compress2.py`、`sim8192.py`；**不是真编码器**，见 §四·2 的边界） |

---

## 二、B-110 · 项目级色彩空间（Linear → Gamma）

### 2·1 事实（现读）

| 项 | 原版 | 我们 |
|---|---|---|
| 色彩空间 | **Gamma**（`ProjectSettings.asset:56 = 0`） | **Linear**（`:50 = 1`） |
| Unity 版本 | 6000.2.6f2 | 6000.3.23f1 |
| URP 资产 | `UniversalRenderPipelineAsset Ultra.asset`：`m_SupportsHDR 1` · `m_MSAA 1` · `m_RenderScale 1` · `m_ColorGradingMode 0`(LDR) · `m_ColorGradingLutSize 16` · 主光阴影 4096 | `Settings/PC_RPAsset.asset`：`m_SupportsHDR: 1` · `m_MSAA: 1` · `m_RenderScale: 1` · `m_ColorGradingMode: 0` ⇒ **URP 那一层本来就对齐了，两个工程唯一的差就是色彩空间** |
| 每场后处理（原版 `Battle Arena 1 PostProcessing.asset`） | **Bloom**（`threshold 1.15` · `intensity 5` · `scatter 1` · `skipIterations 6`）+ **Vignette**（`0.297`）+ **ColorLookup**（`contribution 1` + 一张 LUT） | 我们也有逐场 `WarpforgeArena1/arenas/*/Profiles/*_PostFx.asset`（13 套，内含 `Bloom` / `ColorLookup`） |
| 烘焙光照 | **没有** `LightingSettings` / `Lightmap*` 资产（两边都 0） | 同（`find` 无 `LightingData.asset`） |
| 贴图 | 3,121 张 `Texture2D` | **4,628 份 `.png.meta`**：`sRGBTexture` = **1 有 4,474 / 0 有 151**（3 份解析不出字段）；`textureCompression` = 2 有 3,533 / 1 有 953 / 0 有 139 |
| 材质 / shader | —— | **1,221 份 `.mat`** · **64 份 `.shader`** |
| 自建 shader 里的「桥」 | —— | **13 份 shader / 32 个非注释 `LinearToSrgb()` 调用点** |
| 走「原版编译字节码」的 shader | —— | `WarpforgeShaderMap.UseOriginal` **66 条**（`WarpforgeVFX/Runtime/WarpforgeShaderMap.cs`） |

### 2·2 影响面清单 + 风险分级

**A. 贴图采样（4,628 份）** — 🟢 **低风险 / 大部分是「变对」**
Gamma 下旗标变 no-op ⇒ 全部按 `raw` 采，**这正是原版的行为**。`sRGB=0` 的那 151 张（数据图）本来就该 raw，不受影响。

**B. 自建 shader 的 `LinearToSrgb()` 桥（13 份 / 32 处）** — 🔴 **高风险 / ~~必改~~** → 🔴🔴 **2026-11-01 就地订正：⛔ 不要改，它们必须保留**
~~这些调用是为了在 Linear 管线里模仿原版在 gamma 空间做的运算。切到 Gamma 后它们变成二次编码 ⇒ 这些效果会整体变暗/变色。~~
**实际不成立**：这些调用是**原版字节码的逐字复刻**，不是 Linear 补偿 —— `_tmp_view/dxbc/` 的 27 份反汇编里 **9 份**命中 sRGB 编码常量 `0.003131 / 12.9232 / 1.055`（`matcap_with_tex` · `multi_ray` · `dissolve_mask` · `alpha_masks_two` · `premul_grey` · `trailshader1` · `unlit_uvscroll` · `extra_color_vs`）。
原版在 **Gamma** 下：属性原值 `C` → shader `encode(C)` → 无管线编码 → 最终 `encode(C)`。我们切 Gamma 后**逐位相同**；切前（Linear）反而是 `SetVector` 灌原值 + shader 编码 + 管线再编码 = **双重编码**。
⇒ **切 Gamma 就是它们的「归位」**；删掉会把 13 个特效改成原版不做的样子。同样保留 `WFUIGlowSdfNoise.shader:147` / `WFUIDivisionChange.shader:131` 的手写 `LinToGamma*`。
📌 全过程与判据 → `报告/B167_色彩空间切Gamma.md` §二。
必改清单（非注释调用点数）：`WFMultiRay` 5 · `WFEclipseTau` 3 · `WFMatcap` 3 · `WFParticleDissolveMask` 3 · `WFParticlePremultiplyGreyscale` 3 · `WFUnlitAmbient` 3 · `WFAlphaMasksTwoLayer` 2 · `WFDoomweaverEffect` 2 · `WFParticlePremultiply` 2 · `WFParticlesExtraColorEC` 2 · `WFUnlitUVScroll` 2 · `WFTrailShader1` 1 · `WFTrailShaderFading` 1。
⚠️ 另有 3 份 shader 是**手写的 gamma 编码常量**而不是调 `LinearToSrgb`：`CardPresentation/Resources/Shaders/WFUIGlowSdfNoise.shader:147` 的 `LinToGamma()`、`WFUIDistort` / `WFUIDivisionChange` —— 一并要查。

**C. 走「原版编译字节码」的 66 条 shader** — 🟢 **低风险 / 变对**
原版那份字节码是**在 Gamma 工程里编译**的（`Everguild/FX/*` 等）。今天它跑在 Linear 管线里，等于**原版算式 + 非原版空间**；切到 Gamma 后它才是「原样执行」。这是一条**支持切**的硬理由。

**D. 后处理（`DefaultVolumeProfile` 29 个 overrides + 13 套逐场 `*_PostFx`）** — 🔴 **高风险 / 必须重校**
`Bloom.threshold`(1.15) 与 `ColorLookup` 的 LUT 都吃色彩空间。好消息：这 13 套是从原版导入的（值本来就是 **Gamma 下的原版值**）⇒ 切过去**方向是变对**；坏消息：没人量过切完差多少，等于**13×3 个参数要重校一遍**。

**E. 光照 / 环境光** — 🟠 **中风险**
战场有方向光 + `unity_AmbientSky`（`ArenaEnvGlobal`/`EnvironmentApplier` 灌）。gamma 空间的光照不守恒 ⇒ **整体亮度/对比会变**。无烘焙光照 ⇒ 不涉及 lightmap 重烘（省掉最大的一块）。

**F. UI 的 alpha 混合（整个外壳 / 卡面 / 商店 / 卡组编辑）** — 🔴 **高风险 / 观感重置**
纯不透明 UI 在两种空间下**逐位相同**；差别全在**半透明叠加**。`CardView` 有 20+ 层、`MenuDraw`/`ImageQuad` 全是透明队列 ⇒ 叠出来的中间色会变。这一项**没法局部验**，只能整体重看。

**G. C# 里的颜色常量** — 🟠 **中风险**
`new Color(...)` 的值是在 Linear 工程里**挑出来**的。Gamma 下它们被原样用（不再 `gamma→linear`）⇒ 所有手挑的色号观感会偏。仓内只找到 1 处显式空间转换（`Editor/BattleScene.cs:3978` `Mathf.GammaToLinearSpace`），但**手挑的色号没法机器找**。

**H. 验收基线** — 🔴 **高风险（流程性）**
`资料/真Play待验清单.md` 与各线「并排比原版成品」的结论**全部作废**，要重做一轮。

### 2·3 切 / 不切 / 分阶段 —— 建议

**建议 = 切（Gamma），但分两个阶段；本批只做阶段一。**

**理由（为什么最终该切）**
1. **原版就是 Gamma**，而「彻底复刻原版」是本项目第一目标（`CLAUDE.md` §四）。**我们需要拍板的地方默认复刻原版**（铁律 18·0）。
2. 切换能**一次性消掉两整类欠账**：B-110（项目级）+ **B-115（38 条 sRGB 在 Gamma 下全部变 no-op、自动归零）**。
3. 「走原版编译字节码」的 66 条 shader 只有在 Gamma 下才是**原样执行** —— 这是当前 Linear 里**怎么补都补不出来**的一块。
4. 留在 Linear 的话，原版那些 **gamma 空间的混合公式**（overlay/threshold/`2x-1`）只能靠逐个 shader 手工桥接；**每加一个原版 shader 就多一处要桥**，是长期负债。

**风险（为什么不能在本批切）**
- 需要同步改 13 份 shader / 32 处 + 3 份手写 gamma 函数；这些是**改完必须看画面**的东西，而本代理跑不了 Unity（红线）。
- 13 套逐场后处理 + `DefaultVolumeProfile` 要重校，卡面/UI 观感要整体重看 —— **一次观感全量重置**。
- 切完之后，之前所有「并排比原版」的验收结论失效，需要**重新建立基线**；如果一次性做完又没空验，会出现「改了但没人知道对不对」的黑洞。

**分阶段方案**

| 阶段 | 做什么 | 前置 | 验收 |
|---|---|---|---|
| **一（本批）** | 资产级：按 §三 把「数据类」的 `sRGBTexture` 改 0；压缩档按 §四 提到 BC7 / 未压缩。**不动项目开关** | 无 | 跑一次受影响宿主；难点是 `.meta` 在 gitignore 里（见 §五） |
| **二（独立批次）** | ① `m_ActiveColorSpace: 1 → 0`（**2026-11-01 已做**）；② ~~清掉 32 处 `LinearToSrgb`~~ → 🔴 **订正：不要删，它们是原版字节码的复刻（见 §2·2 B）**；③ 重跑 `EffectSweepBatch` 两趟 + `analyze_sweep`；④ 重校 13 套 `*_PostFx` + `DefaultVolumeProfile`；⑤ 卡面/UI 整体重看 | 用一次完整会话 + 能反复跑 Unity 的时间窗 | ① sweep `>0.30` 归零；② 四宿主截图与之前比、差异能逐条解释；③ 真 Play 一轮 |
| **回滚** | 只改一行（`m_ActiveColorSpace`）就能回去，但**shader 那 32 处必须一起回滚**，否则会回到「桥 + 桥」的双重编码 | —— | git 分支 |

⚠️ **一句话记住**：**Phase 1 是「止损」，Phase 2 是「归位」**。Phase 1 无论如何都该做；Phase 2 要不要做，请用户点头后按上表走。

---

## 三、B-115 · 那 38 条 sRGB（逐条裁定）

> 口径：B111_T1B 的 38 条 = 「原版 `sRGB=0` / 我们 `sRGB=1`」那一档。它把 38 条全判「真缺陷」——**本表按 §1·2 的三条规则重判**。
> ⚠️ **同名多份**：同一个名字在 `WarpforgeVFX/Textures/`（特效线，P0 相关）、`WarpforgeArena1/arenas/**`（战场线，**2026-10-29 起暂停**）、`WarpforgeVFX/ArenaOriginalPrefabs/**`（原件副本）各有一份，**取值不同**。
> 现读：**`WarpforgeVFX/ArenaOriginalPrefabs/**` 那几份已经是正确的**（`sRGB=0` + `comp=2` + 法线 `textureType=1`）—— 这是**仓内现成的判据样板**。

### 3·1 分组小计

| 组 | 条数 | 判定 | 说明 |
|---|---:|---|---|
| **A · 值参与运算（R1）** | **25** | **真缺陷，应改 `sRGBTexture: 0`** | 13 条非 VAT + 12 条 VAT。其中 12 条 VAT **当前 0 引用**（隐患） |
| **B · 值就是最终颜色（R2）** | **10** | **项目级差异导致（不是缺陷，保持 1）** | 切到 Gamma 后自然归位 |
| **C · 只读 alpha（R3）** | **3** | **等价（旗标完全不生效）** | 3 个 TMP SDF 图集 |

### 3·2 A 组 · 真缺陷（应改 0）· 13 条非 VAT

| # | 资产（实际消费者在哪个副本） | 槽位 / shader | shader 拿这个值干什么（行号） | 原版→我们 | 判 |
|---|---|---|---|---|---|
| A1 | `SplatNormal`（`WarpforgeVFX/Textures/`） | `_BumpMap` × `Particles/Lit`（`Water_Ripples_Puddle_reflective.mat`）· `_BumpMap` × `Particles/Unlit`（`WaterDropSmall.mat`） | `UnpackNormalScale()` ⇒ `rgb*2-1` | 0→1 | **改 0** |
| A2 | `NoiseNRM`（`WarpforgeVFX/Textures/`） | `_DistortTex` × `WFDistortion`（`Heat Distortion` / `Trail Distort*` / `EnvironmentalCondition*`） | `WFDistortion.shader` frag：`offset = (d.rg*2-1) * _DistortionStrength` | 0→1 | **改 0** |
| A3 | `EarthCrackNormal`（`WarpforgeVFX/Textures/`） | `_BumpMap` × `Particles/Unlit`（`EarthCrack Add` / `EarthCrack__db155fcb`） | `UnpackNormalScale()` ⇒ `rgb*2-1` | 0→1 | **改 0** |
| A4 | `GoopSprayNormal` | `_BumpMap` × `Particles/Simple Lit`（`GoopSplat_blood.mat`） | `UnpackNormalScale()` | 0→1 | **改 0** |
| A5 | `GoopStreakNormal` | `_BumpMap` × `Particles/Simple Lit`（`GoopStreak_blood.mat`） | `UnpackNormalScale()` | 0→1 | **改 0** |
| A6 | `RippleNormal` | `_DistortTex` × `WFDistortion`（`RippleNormal Distort` / `Explosion Distort`） | `d.rg*2-1` | 0→1 | **改 0** |
| A7 | `SandDecalNormal` | `_BumpMap` × `Particles/Unlit`（`BulletDecalSand.mat`） | `UnpackNormalScale()` | 0→1 | **改 0** |
| A8 | `SmokePuffNormal` | `_DistortTex` × `WFDistortion`（`DistortPuff.mat`） | `d.rg*2-1` | 0→1 | **改 0** |
| A9 | `RippleSubtle`（`WarpforgeVFX/Textures/`） | `_DistortTex` × `WFDistortion` × 2 | `d.rg*2-1` | 0→1 | **改 0** |
| A10 | `Distortion Star` | `_DistortTex` × `WFDistortion` × 2 | `d.rg*2-1` | 0→1 | **改 0** |
| A11 | `Explosion Distort`（`WarpforgeVFX/Textures/`） | **三个消费者全是数据用途**：`_BumpMap` × 2（`Particles/Unlit`）+ `_DistortTex` × 1（`WFDistortion`） | `UnpackNormalScale` / `d.rg*2-1` | 0→1 | **改 0** |
| A12 | `Noise Combined`（`WarpforgeVFX/Textures/` + 13 个战场副本） | `WFParticleDissolveMask._Disolve`（`cut = (A>=d)`）· `WFTrailShader1/…Fading._Noise_Combined`（`A = (1-uv.x)+n.z-_TrailLenght`）· `WFUnlitAmbient`（`lerp(min,max,n)`）· `WFAlphaMasksTwoLayer._NoiseTex1/2` · `WFUnlitUVScroll`（overlay）· `WFMatcap._Noise` · `Particles/Unlit(URP).`噪声槽 | **阈值 / lerp / overlay / `2x-1`** —— 全是算术 | 0→1 | **改 0** |
| A13 | `SmokeLoopAlpha` | `_SecondaryTex` × `WFUnlitUVScroll`（`EC_Whip_Lucius__*`） | overlay：`lo=2*S0*S1` / `hi=1-2*(1-S0)*(1-S1)`（`WFUnlitUVScroll.shader:222-223`） | 0→1 | **改 0** |

### 3·3 A 组 · 12 条 VAT（应改 0 · **当前 0 引用**）

`WarpforgeVFX/Runtime/Vat/{Card Remnant_Remnant Anim {1,3,5,8,9}_{Position,Rotation}Tex, Vanguard Frame Animation VAT_{Position,Rotation}Tex}.png`
—— 全员 `sRGB=1 · enableMipMap=1 · textureCompression=1 · nPOTScale=1`，而
`CardPresentation/Battle/WarpforgeVatDriver.cs:40-43` 白纸黑字要求 **`sRGBTexture: 0` · `textureCompression: 0` · `enableMipMap: 0` · `nPOTScale: 0`**（VAT 顶点数据、28×41 是 NPOT）。
**判定 = 真缺陷（隐患）**：这 12 个 guid 在 `Assets/**` 的 prefab/material/asset/unity 里 **0 次引用**，prefab 用的是 `WarpforgeVFX/Textures/` 那两份副本。谁把它们接回去，就会 NPOT 重采样 + 数据被压 + gamma 解码，VAT 直接错乱。

> 🔴 **2026-11-01 就地订正（`B111W` 现读）**：上面这段有三处不准确 ——
> ① **真正被 prefab 引用的那 12 份副本早就对了**：`WarpforgeVFX/Textures/` 下同名副本现读 `sRGB=0 · nPOT=0 · mip=0 · comp(Default)=0`，是 `EffectExporter.RestoreVatTextures()`（`EffectExporter.cs:782-820`，`:802` 只 `File.Copy` 读 PNG、**不读那份 meta**）自己写的，重跑导出器也不会冲掉。引用证据：`RemnantBody3D Necrons.prefab` 引 10 份、`Vanguard Frame Animated VAT.prefab` / `VanguardIdleEffect.prefab` 各引 2 份。
> ② **`nPOTScale` 现读 12/12 都是 `0`**（不是写的 `1`）；四项里只差 `mip` 与 `comp`。
> ③ **「28×41 是 NPOT」只覆盖 Vanguard 两张**；Remnant 那 10 张实测是 **150×15 / 75×15**。
> ⇒ 「手改 12 份 `Runtime/Vat/*.meta`」改的是**没人用的那份**；剩的真欠账只有 `mip`/`comp`/`format`（0 引用、不影响画面）。判据全文 → `判据/B111W_B115_B110_现状复核.md`。

### 3·4 B 组 · 纯输出色（R2）· 保持 `sRGBTexture: 1` · 10 条

| 资产 | 消费者 / 槽位 | 为什么保持 1 |
|---|---|---|
| `DoubleFlames` · `GlowPalet` · `MuzzleFlash_Front` · `MuzzleFlash_Side` · `MuzzleFlash_Sparks` · `Sparks Metal` | `WFParticlesExtraColor` 的 `_MainTex`；frag 只有 `half4 col = tex * IN.color;`（`WFParticlesExtraColor.shader:247`） | 没有任何算术 ⇒ `enc(dec(raw)*v) ≈ raw*v`（tint=1 逐位相等）。改成 0 会变成 `enc(raw) ≈ raw^0.4545` ⇒ **整体偏亮** |
| `drops_cloud` · `drops_dripping` · `Cloud_puff` · `burst_puff` | `Particles/Unlit(URP)` 的 `_BaseMap`；`col = _BaseMap * _BaseColor * vcol` | 同上 |

> ⚠️ **这一组不是「我们写错了」**，是「**项目级差异导致**」：在 Linear 工程里 `sRGB=1` 才是复现原版无光照观感的正确写法。切到 Gamma 后这条会自动消失（旗标 no-op、按 raw 采）。

### 3·5 C 组 · 等价（R3）· 3 条

| 资产 | 消费者 | 判据 |
|---|---|---|
| `RobotoCondensed-Regular SDF Atlas` | `TextMesh Pro/Shaders/TMP_SDF.shader:250/294/299` → `tex2D(_MainTex, uv).a` | **只读 `.a`**；实测三张 SDF 图集的 **RGB 全是 0（纯黑）、距离全在 alpha 通道**（脚本实测：`RGB mean=0.000 std=0.000`）。Unity 的 sRGB 解码**只作用 RGB，alpha 永远线性** ⇒ **旗标对距离值零影响** |
| `Asar-Regular SDF Atlas` | 同上 | 同上 |
| `Pragati-Regular SDF Atlas` | 同上 | 同上 |

> 🔴 **这是对 `B111_T1B` 表②的一处订正**：那三条被记成「SDF 图集是数据 ⇒ 我们 1 是缺陷」。**不成立** —— 距离在 alpha 里，旗标根本不生效。

### 3·6 对 `B111_T1B` 的口径订正（三处）

| 它写的 | 实际 | 错因 |
|---|---|---|
| 「38 条是真缺陷（法线 8 · VAT 12 · SDF 3 · 噪声/遮罩/光效 15）」 | **25 真缺陷 · 10 项目级差异 · 3 等价** | 用了「原版 0 ⇒ 我们该 0」这条**单一规则**，没有按「shader 怎么用这个值」分档；SDF 那 3 条还漏了「alpha 不受 sRGB 影响」 |
| 「`WarpforgeVFX/Textures` 里原版有 19 张 RGBA32 未压缩、我们压了 —— 最可能的可见质量损失」 | 数量现读 **20 张**（差 1 张未查明）；而且**我们压的是 DXT 不是 BC7** | 没读我们 `.meta` 的 `textureCompression`（见 §四·1） |
| 「SDF 图集距离值被 gamma 解码，字形边/发光会偏」 | **不会**（§3·5） | 默认「RGB 通道装距离」 |

---

## 四、`WarpforgeVFX/Textures` 的 RGBA32 未压缩（现读 20 张）

### 4·1 先纠一个前提：**我们压的是 DXT，不是 BC7**

现读这 20 份 `.meta`：**`textureCompression: 1`（Compressed）** —— 桌面落 **BC1/BC3（DXT1/DXT5）**，不是 `CompressedHQ` 的 BC7。

| 取值 | 语义 | 我们 `WarpforgeVFX/Textures` |
|---|---|---|
| `0` | Uncompressed | 0 张 |
| **`1`** | **Compressed（DXT1/DXT5）** | **20 张（全部）** |
| `2` | CompressedHQ（桌面 BC7） | 0 张 |

根因 = `WarpforgeArena1/Editor/EffectExporter.cs:3732` 的 `ImportTexture()` **只写 `sRGBTexture = true`**，**从来没写过 `textureCompression`** ⇒ 吃 Unity 默认 `Compressed`。

### 4·2 实测：压了以后画面差多少（模拟量化模型）

方法：拿源 PNG（= 原版 RGBA32 解码后）逐 4×4 块拟合端点 + 插值（BC7-mode6：8-bit 端点 / 16 级；DXT5：RGB565 端点 / 8 级），算重建 PSNR。
⚠️ **边界**：这不是真编码器（没有模式搜索、没有位打包），真 BC7 会比这里**更好一点**、真 DXT5 基本同档。所以下面的数是**误差的量级估计**，不是精确值。

| 资产 | 尺寸 | BC7(m6) PSNR | DXT5 PSNR | DXT5 最大误差 |
|---|---|---:|---:|---:|
| `NoiseNRM` | 128² | 46.6 dB | **39.2 dB** | **0.036**（进 `*2-1` 后 ×2 = **0.072**） |
| `MuzzleFlash_Sparks` | 64² | 45.6 | 38.6 | 0.063 |
| `MuzzleFlash_Front` | 64² | 46.3 | 39.8 | 0.057 |
| `MuzzleFlash_Side` | 64² | 48.1 | 41.3 | 0.062 |
| `DoubleFlames` | 64² | 49.4 | 42.2 | 0.040 |
| `necron_skull` | 128² | 52.9 | 45.8 | 0.043 |
| `trait_icon_*`（8 张） | 80² | **35.4–41.7** | **33.8–37.5** | **0.15–0.28** |
| `Battle Arena Space Wolves Props 1` | 8192² | 48.0–57.7（取 3 个有内容的 512² 窗口） | 43.8–49.7 | 0.07–0.34 |
| `Smooth background square` | 32² | 无损 | 无损 | RGB 完全均匀（std=0） |

### 4·3 判据表（「哪些是真噪声/遮罩、压了会糊」）

| 判据 | 含义 | 命中 |
|---|---|---|
| **P1 · 值参与算术** | 量化误差会被算式放大（`*2-1` 翻倍、阈值把它变成可见轮廓） | **`NoiseNRM`**（`_DistortTex` 的 `*2-1`）—— **本组唯一真正会「糊/变形」的一张** |
| **P2 · 高频结构 + 1:1 显示** | BC7/DXT 的 4×4 块会把细颗粒聚成粗颗粒 | `MuzzleFlash_Sparks`（块内高频占比 0.43，本组最高）、`trait_icon_*`（0.35–0.63，但只 80²、显示尺寸小 ⇒ 影响有限） |
| **P3 · 纯加色光斑 / 大面积低频** | 误差被低强度与叠加掩盖 | `DoubleFlames` / `MuzzleFlash_Front` / `MuzzleFlash_Side` / `GlowPalet` / `Sparks Metal` / `necron_skull` |
| **P4 · 完全均匀** | 压不压一样 | `Smooth background square` |

**结论（可执行）**
1. **`NoiseNRM` 必须回 `Uncompressed`**（原版就是 RGBA32；块误差进 `*2-1` 后翻倍）。
2. 其余 19 张：**至少提到 `CompressedHQ`（BC7）** —— 实测比 DXT5 高 **4–7 dB**，代价只是显存约 2×（20 张里 18 张 ≤128²，唯一大的 8192² 从 64 MB→128 MB）。这一条比「全部回未压缩」更实际。
3. `trait_icon_*` 12 张是 **UI 图标**（硬边、80²），BC7 比 DXT5 高 2–3 dB、最大误差从 0.23 降到 0.15 ⇒ 同样建议提 BC7。

---

## 五、可执行改动清单（逐条 · 谁落地）

| # | 改什么 | 旧 → 新 | 落地方式 | 本批做了吗 |
|---|---|---|---|---|
| 1 | `A/CardPresentation/Resources/Art/ui_menu/Noise_Combined.png.meta:10` | `sRGBTexture: 1` → **`0`** | 直接改 `.meta`（`ArtBaker` **不写** `sRGBTexture` ⇒ 不会被烘焙覆盖） | ✅ **已改**（117 行守恒 · LF 守恒 · `diff` 只 1 行 · 备份 `_tmp_view/b113_g1_backup/`）<br>⚠️ **会改画面**：材质虽无 prefab/场景 guid 引用，但**运行期按路径 `Resources.Load`**：`MenuFxMaterials.GlowSdfAllianceTrophy` → `Shell/AllianceMemberTab.cs:718`（联盟成员页发光描边）· `MenuFxMaterials.DistortAtlasSmall` → `Shell/ExpansionPassEventWindow.cs:2818` · `Art/vfx/WFUI_DivisionChange` → `Shell/RankedDivisionChangeWindow.cs:1707`。**这三处的观感会变（噪声回到原版 raw 值）**，需在 Unity 里扫一眼 |
| 2 | `A/WarpforgeVFX/Textures/{SplatNormal, NoiseNRM, EarthCrackNormal, GoopSprayNormal, GoopStreakNormal, RippleNormal, SandDecalNormal, SmokePuffNormal, RippleSubtle, Distortion Star, Explosion Distort, Noise Combined, SmokeLoopAlpha}.png.meta` | `sRGBTexture: 1` → **`0`** | ⚠️ **手改 `.meta` 不持久**：`WarpforgeVFX/*` 在 `.gitignore:26`；且下次 `EffectExporter` 重导会把 `sRGBTexture` 写回 1。**持久做法 = 改 `EffectExporter.cs:3732`**（下一条） | ⛔ 未改（不在白名单 + 不持久） |
| 3 | `A/WarpforgeArena1/Editor/EffectExporter.cs:3732` `ti.sRGBTexture = true;` | 改成「按原版 `m_ColorSpace` 写」—— 但 Unity 的 `Texture2D` **不暴露 colorSpace**，所以要落成**按名字的白名单/表**（数据类 13 条，见 A 组）或「从 bundle dump 出的 `colorspace` 列读」 | `.cs` 改动（不在本代理白名单） | ⛔ |
| 4 | 同文件 `ImportTexture()` | **补 `ti.textureCompression`**：原版 `fmt=4`(RGBA32)→`Uncompressed`；`12`(DXT5)/`10`(DXT1)→`Compressed`；`25`(BC7)→`CompressedHQ`。数据来源已有：`_tmp_view/b108_r7/join_arena.tsv` 的 `orig_fmt` 列 | `.cs` 改动 | ⛔ |
| 5 | `A/WarpforgeArena1/arenas/**/Textures/*.png.meta`（同名战场副本） | 同 #2 | **战场线（2026-10-29 用户口径：暂停，以后再说）** ⇒ 只记账 | ⛔（暂停线） |
| 6 | `A/WarpforgeVFX/Runtime/Vat/*.png.meta`（12 张） | 按驱动规格：`sRGBTexture 0` · `textureCompression 0` · `enableMipMap 0` · `nPOTScale 0` | 这 12 份是**手配**的、无导出器会写 ⇒ 改 `.meta` 即可（但 `WarpforgeVFX/*` 是 gitignore ⇒ 换机器要重配；判据已在 `WarpforgeVatDriver.cs:38-44`） | ⛔（0 引用，隐患项） |
| 7 | `A/WarpforgeVFX/Textures/**` 压缩档 | 20 张 `textureCompression: 1 → 2`（BC7）；`NoiseNRM` → `0` | 持久做法同 #4 | ⛔ |
| 8 | `ProjectSettings/ProjectSettings.asset:50` | `m_ActiveColorSpace: 1 → 0` | **阶段二**（§2·3），⛔ 本批绝对不动 | ⛔ 只给建议 |

---

## 六、没查清 / 还差什么（如实记）

1. **`WarpforgeVFX` 与 arena 那 903 张里有 262 张文件名对不上原版** ⇒ 它们的原版格式/色彩空间**无从比对**（`join_arena.tsv` 的空行）。本文件所有「原版值」只覆盖能对上的那 643 张。
2. **19 vs 20 差 1 张没查明**：B111_T1B 记 19，现读 20。最可能是 `Smooth background square`（32²、RGB 完全均匀、压不压一样）被它按「无差异」剔掉，**但没有它的口径原文可核**。
3. **`Explosion Distort` 一名两用**：`WarpforgeVFX/Textures/Explosion Distort.png` 同时被 `_BumpMap`（法线解码）×2 与 `_DistortTex`（`*2-1`）×1 使用 —— 三个消费者**都是数据用途**，所以本轮判「改 0」没有冲突；但若将来有人把它挂到 `_BaseMap` 当颜色用，**必须拆成两份资产**。
4. **改完的观感没验**（红线：不跑 Unity）。#1 那一条**是活的**：8 份 `WFUI_*` 材质虽然没有 prefab/场景 guid 引用，但**运行期按 `Resources.Load` 路径取** —— `AllianceMemberTab`（联盟成员页描边）· `ExpansionPassEventWindow`（活动窗扭曲）· `RankedDivisionChangeWindow`（排位分部扭曲）三处会变。**必须跑一次 `ShellScene` / 进游戏扫一眼再合**。
5. **`Resources/Art/**`（可写树）里还有一批 `sRGB=1` 的疑似数据图没审**：`ui_menu/*_SDF.png`（6 张）· `ui_deck/FX_Square_UI_SDF.png` · `ui_menu/UI_Dirt_And_Noise_skratches.png`（`WFUIBorderHighlight._Noise_R`）· `cardbacks/*_sdf.png`（200+）。它们**不在本次 38 条的范围内**，需要单独一次普查（判据同 §1·2）。
6. **`arena1_lut.png` 是 `sRGB=1` 但 0 引用**（LUT 按 `ArenaBuilder.EnsureLutImportSettings` 必须 `sRGB=false`）。它是死资产 ⇒ 本轮没动；谁把它接进 LUT 混合链就会中招。
7. **BC7 是否 100% 落桌面**：`textureCompression: 2` 在 Standalone 是否落 BC7，本代理**不跑 Unity 读不到**（与 `B108_AUD09` §五·1 同一条）。

---

## 七、顺手发现

1. **`WarpforgeVFX/ArenaOriginalPrefabs/**` 的同名副本已经是正确设置**（`sRGB=0` / `comp=2` / 法线 `textureType=1`）—— 有人手工配对过，但**只配了「参照副本」那一棵树**，真正被 VFX 材质引用的 `WarpforgeVFX/Textures/**` 没配。⇒ 仓内现成的判据样板，落地时直接照它。
2. 🔴 **`ui_menu/Noise_Combined.png` 与 `card_sdf/cardback_noise.png` 逐字节完全相同**（实测 `identical: True`，128×128 RGBA），但一张 `sRGBTexture: 1`、一张 `0` ⇒ **同一张图两种导入**，内部矛盾。已按后者修正（§五 #1）。
3. 🔴 **`CardPresentation/Core/VoiceWaveMaterial.cs:30` 早就写着**「⛔ 别挂 `Art/ui_menu/Noise_Combined.png`：那张是 **sRGB** 导入，噪声值会被 gamma 解码」—— **代码层发现了、没人回头改 `.meta`**（铁律 5·b 的漏网）。同类隐患建议全仓 grep 一遍「sRGB 导入」这类注释。
4. 🔴 **B-115 的根因不在 `.meta`，在 `EffectExporter.cs:3732` 的硬编码 `ti.sRGBTexture = true`**。手改 `.meta` 只是止痛：下次全量重导就回退。**同一族**：`ArtBaker.cs` 不写 sRGB（所以 `Resources/Art/**` 手改能持久），两棵树**规则写在两个地方**。
5. 🟠 **`WarpforgeVFX/*` 与 `CardPresentation/Resources/Art/` 都在 `.gitignore`**（`:26`、`:99`）⇒ 这两棵树的 `.meta` 改动 **`git status` 看不见**（与 `B-19` 记的是同一类）。本批 #1 的改动因此**不会出现在提交里**，判据只能靠本文件 + 代码侧修正。
6. 🟠 **`WFUIDistort` / `WFUIDivisionChange` / `WFUIGlowSdfNoise` 三个 UI 特效 shader 对噪声做 `*2-1` 或阈值**。当前 `sRGB=1` ⇒ `decode(0.5)=0.214` ⇒ 中性灰变成 **−0.57** 而不是 0 ⇒ **扭曲方向是单向偏的、不是围绕 0 摆动**。这三个材质现在是 0 引用，接上去那天就会看到不对称扭曲。
7. 🟠 **`Battle Arena Space Wolves Props 1` 的 Standalone 平台块写着 `maxTextureSize: 2048`，但 `overridden: 0`** ⇒ 死值，实际按 Default 的 **8192** 走。⛔ 别拿它当「被缩了一半」的证据（与 `B108_AUD09` 顺手发现 1 同族）。
8. 🟡 **`trait_icon_*` 12 张是 `WarpforgeVFX/Textures` 下的 UI 图标**，却按「特效贴图」的 DXT 压。它们跟着特效树的策略走，但其实是 UI ⇒ 建议提 BC7（§4·3）。
