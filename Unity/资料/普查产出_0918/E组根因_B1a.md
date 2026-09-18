# E 组逐效果根因 · B1a_纯精灵图_最差一半（66 条）

> 2026-09-18 · 只读调查（**未跑 Unity**）。数据：`资料/比对基线/sweep_{orig,exp}.tsv`(09-15 20:48/20:51) ·
> `per_effect_tech.json` · `资料/特效还原台账.tsv`(09-15 20:51) · `Assets/WarpforgeVFX/Prefabs/*.prefab` 的
> `WarpforgeEffectBinder` 块（**直接解 YAML**：`rendererSlots`/`trailSlots` 是 **hex 小端 int32 blob**，不是 YAML 列表）·
> `Materials/*.mat`(741 个) · `导出报告.tsv` · `普查产出_0918/shader原件可用性_表.md` · `WarpforgeShaderMap.cs` · `WarpforgeEffectBinder.cs`。

🔴 **两条前提，读表前必看**
1. **尺子和资产不是同一次构建**：亮度数字来自 **09-15** 那趟 sweep，而 `Prefabs/` + `Materials/` 是 **09-17 23:43** 重导的 ⇒
   「症状」列描述的是 09-15 的构建，「槽/def」列描述的是**今天**的构建 —— 两者只能互为线索，不能互相证明。
2. **本块 66 条里 54 亮 / 12 暗**（台账口径）。比值 `r` = 导出 sum ÷ 原版 sum，只在该时刻「两边 lit 都 ≥60」时才有值。

## 一、一句话结论 + 锅类计数

**本块的偏亮几乎全部指向同一条公式**：`WFParticlesExtraColor.shader:157-158` 的 `col.rgb += tex.rgb*_EmissionColor.rgb*IN.color.rgb` ——
凡是映射到自建 Extra Color 的材质槽，只要 `_EmissionColor` 取到 **shader 声明的默认 `(1,1,1,1)`**（属性未记录）或原值 ≥1，
就会在该槽上**多加一整份贴图（≈2×）**。本块 **58/66** 条效果至少有 1 个这样的槽，而 **54 条「亮」里 52 条**都命中了它。

`锅H′: 52 条`（本块新增，判据见 §一之补）· `锅A′: 9 条`（近似 shader 把 `_MainTex` 丢了，**09-18 已改走原件但没人收账**）·
`锅T: 10 条`（时序包络；其中 8 条与锅H′并存）· `锅D（软粒子嫌疑）: 12 条` · `判不出: 1 条`

**已确立的锅 A / F / B·E 在「今天的资产」里已经指不到本块了**（都是量出来的，但**不能**因此说「它们不是原因」，见 §四）：
- 锅 **A**（`trailMaterial` 绑到贴图全丢的 def）：今天 **0/66** —— `trailSlots` 指向的 def **没有一个缺 `_MainTex`**，24 条连拖尾槽都是全 -1。
  ⚠️ **锅A 的贴图修复（`带贴图 0/207 → 290/316`）发生在 09-17，正好在 sweep(09-15) 与今天之间** ⇒ 09-15 那批偏亮里可能有一大块就是它。
- 锅 **F**（`m_RenderMode=4` Mesh 且 `m_Mesh: {fileID: 0}`）：本块 **0 个**渲染器是 Mesh 模式；`n199 == n198` 在 **66/66** 条成立（没丢发射器）。
- 锅 **B/E**（`_SrcBlend=1/_DstBlend=0` 残留混合签名）：本块只剩 `Mobile/Particles/Additive`×15 + `Mobile/Particles/Alpha Blended`×4 个 def，
  而这两个名字**正好被 `WarpforgeShaderMap.InferBlend`（`:161-177`）命中并改写** ⇒ 那批是**修过且落在生效路径上**的。

### 一之补 · 本块新用的三个锅名（判据写全；其余沿用旧词）
- **锅H′**（判据）：该 def 的 shader ∈ `WarpforgeShaderMap.Replacements` 里指向 `WarpforgeVFX/Particles/Extra Color` 的 25 个名字（`WarpforgeShaderMap.cs:72-147`）
  **且** 该 def 的 `_EmissionColor` **未记录**（⇒ 取 shader 默认 1）**或** 记录值 ≥0.99 ⇒ 该槽按 `:158` 多加一份 `tex`。
  ⚠️ 与 B46 块的「锅 H」（HDR `_Color` 被当乘数）是**同一条片段公式的两半**：那边是 `col = tex*_Color*IN.color`，这边是 `+=` 那一行。
- **锅T**（判据）：`r_peak ∈ [0.7,1.4]`（**原版自己的峰值时刻两边对得上**）而序列 `spread = max ln r − min ln r ≥ 0.9`
  ⇒ 差异不在「有多亮」而在「什么时候亮」⇒ 该看的层是 prefab 粒子模块，不是材质。
- **锅A′**（判据）：该槽用的 def **`texNames` 里既没有 `_MainTex` 也没有 `_BaseMap`**，而它映射到的自建 shader 采样 `_MainTex`
  （`WFParticlesExtraColor.shader:23,101`）⇒ 贴图落回 shader 默认 `"white"`。**这是旧锅 A 的同一机制，但落在粒子 def 上、不在拖尾槽上**（本块 9 处，名单见 §三②）。

### 一之补二 · 尺子可疑的 12 条（比值只由 1–2 个采样点得出，**先别按它定量**）
`CreateCard Sabotage`(r 序列 62.9→65.4→3.44→1.00 双峰) · `EnvironmentalCondition Saim Hann Webway Rift`(nboth=1) ·
`Psychic_Lightning_down_Waaagh_Green` · `MarkOfTzeentch_enchantment` · `Buff_SW_Runes 1` · `BulletImpact_Kroot_KrootRifle` ·
`CreateCard BlackLegion` · `CreateCard` · `Psychic_Lightning_warlord_chaos` · `Tank_Shot_Small` · `Explosion_Ground`(1 个发射器)
—— 这 12 条里 `nboth ≤ 2` 或序列双峰，中位比值**测的是「哪两个时刻被选中」**，不是「有多亮」。

## 二、逐效果表

列说明：**症状**列 `r 序列` = 各时刻 导出 sum÷原版 sum；`lit o·e` = 8 个时刻的**原版·导出亮像素数**，时间点一律 `.15 .30 .50 .75 1.00 1.50 2.00 3.00`。
**槽/def** = `rendererSlots` 里非 -1 的槽数 / prefab 内 def 总数 + 这些槽实际用的 shader。

| 效果名 | 亮/暗 | \|ln\| | 症状形状（8 时刻证据） | 槽/def · shader 混 | 锅类 | 证据（文件:行号） | 置信 | 下一步看哪 |
|---|---|---|---|---|---|---|---|---|

**「下一步」列是代码**，四条各自的含义：
- **H1** = 拆该 prefab 的 binder def，找 `shader: WarpforgeVFX/Particles/Extra Color`（或任一映射到自建 Extra Color 的原版名）的那几份，读它们的 `_EmissionColor`。
  **判据：同一 prefab 里同名的那份「原版 shader」def 是 `0` 或**根本没记这个属性**（样本：`Prefabs/Buff_Mana.prefab` def0 `LightningTrail`/`Everguild/FX/Extra Color` = `0|0|0|1` vs def2 `LightningTrail`/`WarpforgeVFX/Particles/Extra Color` = `1|1|1|1`）。
  —— 同款证据在本块 **50 份 def** 上成立：**50/50 全是 `_EmissionColor=(1,1,1,1)`，而原版名那份散在 `0 / <1 / >1` 三档** ⇒ 那个 1 是 **shader 默认值**，不是量出来的原版值。
- **T1** = `ParticleModuleProbe.Run`（`Assets/WarpforgeArena1/Editor/ParticleModuleProbe.cs`）逐模块比这 8 个时刻，重点 `ColorOverLifetime` / `Emission.m_RateOverTime` / `startLifetime`。**先别动材质。**
  ⚠️ 它已经比过 9 个样本并报「模块值全同」（`资料/特效还原_进度与交接.md:114`）—— 那 9 个**不含**本块的效果，所以本块还得自己跑一次；**判据只认「哪些字段不同」这个集合，不认条数**（同文档 §三之补末）。
- **A1** = 那 9 处用的原版 shader（`Particle Dissolve Mask` / `Shine CVS` / `Unlit UV scroll`）**已经在 09-18 进了 `UseOriginal` 白名单**（`WarpforgeShaderMap.cs:44-51`，`:200-206` 里它优先级最高）
  ⇒ 这些槽现在跑的是**bundle 里的原件**，不是我们的近似 ⇒ **只要重跑 sweep 就能知道好没好，别再按旧数字改**。
- **D1** = `EffectIso.Run` 单渲这一个效果，**逐个发射器关掉**看亮度怎么变。
- **D2** = 先查那个槽的 `_SoftParticlesEnabled`/`_SoftParticleFadeParams` 在**运行时**是不是被 Unity 原生 `ParticleSystemRenderer` 重算（`WarpforgeEffectBinder.ApplyDerivedParticleDefaults` 的注释 `:206-213` 说它**只补没记到的**，而这里记到的是死值 `0|0|0|0`）—— 查不出来就转 D1 实拍。

| CreateCard Sabotage | 亮 | 3.502 | 分段变化(前段更亮)；**原版峰值那一帧对得上**(r=1.00)<br>`r`: 0.3→62.90 0.5→65.42 0.75→3.44 1.0→1.00<br>`lit o·e`: 0.15|21·476 0.30|147·529 0.50|152·536 0.75|564·560 1.00|546·543 1.50|55·154 2.00|1·10 3.00|0·0 | 9槽/9def URP×6 ExtraColor×2 DissolveMask×1 | **锅A′**+**锅H′**+锅T | `Prefabs/CreateCard Sabotage.prefab:15816`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` · `资料/比对基线/sweep_{orig,exp}.tsv`(r 序列见左) · `资料/特效还原_进度与交接.md:114,128-130` · def/trail 槽 **def[0] `Generic Particle Dissolve For Sprites Create Sabotage`(Everguild/FX/Particle Dissolve Mask)** 的 `texNames` = `['_Disolve']`（**没有 `_MainTex`/`_BaseMap`**）· `WFParticlesExtraColor.shader:23,101` | 中 | **H1** **T1** **A1** |
| EnvironmentalCondition Saim Hann Webway Rift | 亮 | 2.606 | **单点**（只有 3.0 一个时刻两边都亮）<br>`r`: 3.0→13.54<br>`lit o·e`: 0.15|2·6 0.30|3·10 0.50|3·26 0.75|3·53 1.00|3·102 1.50|13·236 2.00|49·456 3.00|65·937 | 24槽/24def URP×11 ExtraColor×7 AlphaMask2×2 ExtraColor(我名)×1 | **锅H′** | `Prefabs/EnvironmentalCondition Saim Hann Webway Rift.prefab:22537`(rendererSlots) · boost 槽 **8 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 低（单点/双点） | **H1** |
| Buff_Mana | 亮 | 2.351 | 分段变化(前段更亮)<br>`r`: 0.15→10.50 0.3→10.57 0.5→10.71 0.75→2.90 1.0→3.30<br>`lit o·e`: 0.15|169·941 0.30|335·1893 0.50|549·3154 0.75|3267·5748 1.00|3437·6413 1.50|0·0 2.00|0·0 3.00|0·0 | 5槽/6def URP×4 ExtraColor(我名)×1 | **锅H′** | `Prefabs/Buff_Mana.prefab:648`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| Buff_Mana_2 | 亮 | 2.295 | 分段变化(前段更亮)<br>`r`: 0.15→9.93 0.3→10.35 0.5→10.31 0.75→3.14 1.0→3.55<br>`lit o·e`: 0.15|76·383 0.30|140·770 0.50|232·1276 0.75|1324·2326 1.00|1407·2598 1.50|5·14 2.00|0·0 3.00|0·0 | 3槽/4def URP×2 ExtraColor(我名)×1 | **锅H′** | `Prefabs/Buff_Mana_2.prefab:19934`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| Buff_Mana_3 Holy | 亮 | 2.171 | 分段变化(前段更亮)<br>`r`: 0.15→8.77 0.3→8.99 0.5→9.02 0.75→2.44 1.0→2.06<br>`lit o·e`: 0.15|94·369 0.30|183·741 0.50|307·1233 0.75|1690·2348 1.00|2546·2690 1.50|5·14 2.00|0·0 3.00|0·0 | 4槽/5def URP×2 ExtraColor(我名)×1 ExtraColor×1 | **锅H′** | `Prefabs/Buff_Mana_3 Holy.prefab:539`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| Smoke_Explosion | 暗 | 2.136 | 分段变化(前段更亮)<br>`r`: 0.15→0.18 0.3→0.15 0.5→0.09 0.75→0.03<br>`lit o·e`: 0.15|380·243 0.30|439·247 0.50|522·178 0.75|551·61 1.00|389·0 1.50|0·0 2.00|0·0 3.00|0·0 | 5槽/5def URP×5 | **锅D**（软粒子嫌疑） | `Prefabs/Smoke_Explosion.prefab:20219` · `_SoftParticlesEnabled=1` 的槽 2 个、`_SoftParticleFadeParams` 记成 `0|0|0|0` | 低 | **D2**→**D1** |
| Psychic_Lightning_down_Waaagh_Green | 亮 | 1.906 | 分段变化(后段更亮)<br>`r`: 0.15→3.72 0.3→9.73<br>`lit o·e`: 0.15|80·185 0.30|62·282 0.50|51·455 0.75|0·445 1.00|0·435 1.50|0·0 2.00|0·0 3.00|0·0 | 10槽/12def ExtraColor×5 URP×3 ExtraColor(我名)×2 | **锅H′** | `Prefabs/Psychic_Lightning_down_Waaagh_Green.prefab:45548`(rendererSlots) · boost 槽 **6 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 低（单点/双点） | **H1** |
| Lightning burst webway | 亮 | 1.837 | 分段变化(后段更亮)<br>`r`: 0.15→1.45 0.3→1.98 0.5→3.85 0.75→6.28 1.0→8.09 1.5→8.17 2.0→8.67<br>`lit o·e`: 0.15|284·373 0.30|555·823 0.50|354·934 0.75|192·496 1.00|176·586 1.50|210·677 2.00|158·536 3.00|36·127 | 5槽/6def ExtraColor×2 URP×2 ExtraColor(我名)×1 | **锅H′** | `Prefabs/Lightning burst webway.prefab:10443`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| MarkOfTzeentch_enchantment | 亮 | 1.806 | 分段变化(前段更亮)<br>`r`: 2.0→8.34 3.0→3.84<br>`lit o·e`: 0.15|0·0 0.30|0·0 0.50|0·0 0.75|0·0 1.00|0·0 1.50|46·175 2.00|254·816 3.00|573·1101 | 3槽/4def ExtraColor×1 URP×1 ExtraColor(我名)×1 | **锅H′** | `Prefabs/MarkOfTzeentch_enchantment.prefab:386`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 低（单点/双点） | **H1** |
| Buff_SW_Runes 1 | 亮 | 1.718 | **全程恒定**<br>`r`: 0.15→5.76 0.3→5.39<br>`lit o·e`: 0.15|70·75 0.30|109·111 0.50|40·57 0.75|22·22 1.00|19·20 1.50|12·13 2.00|0·0 3.00|0·0 | 7槽/8def URP×3 ExtraColor×2 ExtraColor(我名)×1 DissolveMask×1 | **锅H′** | `Prefabs/Buff_SW_Runes 1.prefab:20542`(rendererSlots) · boost 槽 **4 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 低（单点/双点） | **H1** |
| Psychic_Lightning_down_Intense_white | 亮 | 1.581 | 近似恒定(后段更亮)<br>`r`: 0.15→3.02 0.3→4.86 0.5→5.05 0.75→5.06 1.0→4.71<br>`lit o·e`: 0.15|123·189 0.30|145·282 0.50|236·453 0.75|352·668 1.00|453·888 1.50|0·0 2.00|0·0 3.00|0·0 | 9槽/11def ExtraColor×4 URP×3 ExtraColor(我名)×2 | **锅H′** | `Prefabs/Psychic_Lightning_down_Intense_white.prefab:55348`(rendererSlots) · boost 槽 **5 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| Buff_Mana_Blood | 亮 | 1.545 | 分段变化(前段更亮)<br>`r`: 0.3→6.95 0.5→7.24 0.75→2.24 1.0→2.42<br>`lit o·e`: 0.15|34·136 0.30|67·276 0.50|107·459 0.75|558·865 1.00|624·954 1.50|8·6 2.00|0·0 3.00|0·0 | 5槽/7def URP×3 ExtraColor(我名)×2 | **锅H′** | `Prefabs/Buff_Mana_Blood.prefab:15345`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| StunEffect_idle | 亮 | 1.527 | 分段变化(后段更亮)<br>`r`: 0.15→2.15 0.3→3.46 0.5→4.04 0.75→5.34 1.0→4.51 1.5→4.70 2.0→4.96 3.0→5.14<br>`lit o·e`: 0.15|515·711 0.30|739·1375 0.50|877·1836 0.75|698·1896 1.00|1128·2391 1.50|1207·2448 2.00|1115·2284 3.00|1030·2222 | 2槽/3def URP×1 ExtraColor(我名)×1 | **锅H′** | `Prefabs/StunEffect_idle.prefab:19931`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| WhileInPlay_Tzeentch | 亮 | 1.478 | 分段变化(后段更亮)<br>`r`: 0.15→1.10 1.5→4.38 2.0→5.71<br>`lit o·e`: 0.15|85·109 0.30|38·40 0.50|38·40 0.75|37·38 1.00|44·54 1.50|97·228 2.00|118·398 3.00|47·113 | 4槽/5def URP×2 ExtraColor×1 ExtraColor(我名)×1 | **锅H′** | `Prefabs/WhileInPlay_Tzeentch.prefab:20024`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| Psychic_Lightning_down | 亮 | 1.470 | 分段变化(后段更亮)<br>`r`: 0.15→2.09 0.3→5.80 0.5→2.90 0.75→6.78<br>`lit o·e`: 0.15|508·609 0.30|272·360 0.50|890·1119 0.75|69·111 1.00|0·0 1.50|0·0 2.00|0·0 3.00|0·0 | 5槽/7def ExtraColor(我名)×2 ExtraColor×2 URP×1 | **锅H′** | `Prefabs/Psychic_Lightning_down.prefab:30254`(rendererSlots) · boost 槽 **4 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| DeckBuff_Orks | 亮 | 1.441 | 近似恒定(前段更亮)<br>`r`: 0.15→6.35 0.3→3.73 0.5→4.04 0.75→4.70 1.0→4.22<br>`lit o·e`: 0.15|713·2822 0.30|994·2844 0.50|998·2840 0.75|623·2535 1.00|521·2001 1.50|16·139 2.00|0·0 3.00|0·0 | 4槽/4def URP×2 ExtraColor×2 | **锅H′** | `Prefabs/DeckBuff_Orks.prefab:5398`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| Lightning_Green | 亮 | 1.388 | 分段变化(持平)；**原版峰值那一帧对得上**(r=1.07)<br>`r`: 0.15→3.93 0.3→4.21 0.5→4.02 0.75→4.12 1.0→4.18 1.5→3.99 2.0→1.07 3.0→3.98<br>`lit o·e`: 0.15|347·462 0.30|417·631 0.50|489·701 0.75|487·706 1.00|477·693 1.50|471·628 2.00|4428·4586 3.00|443·684 | 7槽/8def URP×3 Sprites/Mask×1 Sprites/Default×1 ExtraColor(我名)×1 | **锅H′**+锅T | `Prefabs/Lightning_Green.prefab:5649`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` · `资料/比对基线/sweep_{orig,exp}.tsv`(r 序列见左) · `资料/特效还原_进度与交接.md:114,128-130` | 中高 | **H1** **T1** |
| Possession_self | 亮 | 1.382 | 分段变化(前段更亮)；**原版峰值那一帧对得上**(r=1.00)<br>`r`: 0.5→8.45 1.0→5.04 1.5→2.93 2.0→1.00<br>`lit o·e`: 0.15|13·33 0.30|37·105 0.50|63·191 0.75|43·143 1.00|76·166 1.50|72·116 2.00|2285·2281 3.00|4·5 | 7槽/8def URP×6 ExtraColor(我名)×1 | **锅H′**+锅T | `Prefabs/Possession_self.prefab:39905`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` · `资料/比对基线/sweep_{orig,exp}.tsv`(r 序列见左) · `资料/特效还原_进度与交接.md:114,128-130` | 中 | **H1** **T1** |
| Buff_Eldar_Warp Spider | 亮 | 1.336 | 分段变化(前段更亮)<br>`r`: 0.15→4.22 0.3→3.80 0.5→4.35 0.75→3.39 1.0→1.28<br>`lit o·e`: 0.15|699·757 0.30|957·1058 0.50|842·1026 0.75|446·628 1.00|167·213 1.50|41·51 2.00|6·6 3.00|0·0 | 9槽/10def URP×5 ExtraColor×2 ExtraColor(我名)×1 Mob/Add×1 | **锅H′** | `Prefabs/Buff_Eldar_Warp Spider.prefab:25556`(rendererSlots) · boost 槽 **3 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| Delightful_Agonies_Projectile | 亮 | 1.321 | 分段变化(持平)<br>`r`: 0.15→2.16 0.3→3.29 0.5→4.30 0.75→4.21 1.0→8.77 1.5→1.89<br>`lit o·e`: 0.15|154·211 0.30|260·421 0.50|365·684 0.75|401·718 1.00|344·978 1.50|1073·1398 2.00|0·0 3.00|0·0 | 7槽/8def URP×6 ExtraColor(我名)×1 | **锅H′** | `Prefabs/Delightful_Agonies_Projectile.prefab:39908`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| BulletImpact_Webber | 亮 | 1.288 | **全程恒定**<br>`r`: 0.3→3.58 0.5→3.67<br>`lit o·e`: 0.15|36·67 0.30|65·117 0.50|74·112 0.75|0·0 1.00|0·0 1.50|0·0 2.00|0·0 3.00|0·0 | 11槽/12def Mob/Add×5 URP×4 ExtraColor(我名)×1 ExtraColor×1 | **锅H′** | `Prefabs/BulletImpact_Webber.prefab:5907`(rendererSlots) · boost 槽 **5 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 低（单点/双点） | **H1** |
| Solar Beam Target | 亮 | 1.256 | **全程恒定**<br>`r`: 0.5→3.83 0.75→3.03 1.0→3.19 1.5→3.84<br>`lit o·e`: 0.15|32·98 0.30|57·124 0.50|68·158 0.75|134·265 1.00|128·274 1.50|84·278 2.00|0·0 3.00|0·0 | 7槽/8def ExtraColor×3 URP×3 ExtraColor(我名)×1 | **锅H′** | `Prefabs/Solar Beam Target.prefab:35433`(rendererSlots) · boost 槽 **3 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| FrenziedEffect | 亮 | 1.247 | 近似恒定(前段更亮)<br>`r`: 0.15→4.42 0.3→4.00 0.5→2.96 0.75→2.59<br>`lit o·e`: 0.15|534·1156 0.30|528·1024 0.50|1135·1298 0.75|1747·1759 1.00|0·0 1.50|0·0 2.00|0·0 3.00|0·0 | 5槽/5def Sprites/Mask×1 Sprites/Default×1 ShineCVS×1 ExtraColor×1 | **锅A′**+**锅H′** | `Prefabs/FrenziedEffect.prefab:460`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` · def/trail 槽 **def[2] `Generic Trait Particle Shine`(Everguild/FX/Particle Shine Custom Vertex Streams)** 的 `texNames` = `['Texture2D_0bfdc50de139497fa85c0cd08848879a']`（**没有 `_MainTex`/`_BaseMap`**）· `WFParticlesExtraColor.shader:23,101` | 中 | **H1** **A1** |
| ScytheAssault | 暗 | 1.184 | **全程恒定**<br>`r`: 0.15→0.30 0.3→0.30 0.5→0.31 0.75→0.31 1.0→0.31<br>`lit o·e`: 0.15|177·160 0.30|268·251 0.50|296·275 0.75|254·241 1.00|269·252 1.50|0·0 2.00|0·0 3.00|0·0 | 8槽/9def URP×5 ExtraColor(我名)×1 ExtraColor×1 Mob/AlphaBlend×1 | **锅D**（软粒子嫌疑） | `Prefabs/ScytheAssault.prefab:88798` · `_SoftParticlesEnabled=1` 的槽 1 个、`_SoftParticleFadeParams` 记成 `0|0|0|0` | 低 | **D2**→**D1** |
| Relentless Fusillade | 亮 | 1.153 | **全程恒定**<br>`r`: 0.15→3.32 0.3→3.03 0.5→3.42 0.75→3.17 1.0→2.90<br>`lit o·e`: 0.15|140·315 0.30|106·212 0.50|173·409 0.75|111·227 1.00|103·210 1.50|0·0 2.00|0·0 3.00|0·0 | 9槽/10def URP×5 ExtraColor×2 ExtraColor(我名)×1 Mob/Add×1 | **锅H′** | `Prefabs/Relentless Fusillade.prefab:35432`(rendererSlots) · boost 槽 **4 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| Pulse Onslaught | 亮 | 1.148 | **全程恒定**<br>`r`: 0.3→3.34 0.5→3.12 0.75→3.15<br>`lit o·e`: 0.15|32·74 0.30|98·228 0.50|91·203 0.75|83·176 1.00|42·111 1.50|0·0 2.00|0·0 3.00|0·0 | 9槽/10def URP×5 ExtraColor×2 ExtraColor(我名)×1 Mob/Add×1 | **锅H′** | `Prefabs/Pulse Onslaught.prefab:30495`(rendererSlots) · boost 槽 **4 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| Explosion Hand UI | 亮 | 1.146 | 分段变化(后段更亮)；**原版峰值那一帧对得上**(r=0.91)<br>`r`: 0.15→0.91 0.3→0.80 0.5→3.42 0.75→3.22 1.0→3.15<br>`lit o·e`: 0.15|478·532 0.30|565·630 0.50|272·517 0.75|388·670 1.00|399·737 1.50|0·0 2.00|0·0 3.00|0·0 | 4槽/4def URP×3 ExtraColor×1 | **锅H′**+锅T | `Prefabs/Explosion Hand UI.prefab:34728`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` · `资料/比对基线/sweep_{orig,exp}.tsv`(r 序列见左) · `资料/特效还原_进度与交接.md:114,128-130` | 中 | **H1** **T1** |
| Explosion Hand UI 0,5s delay | 亮 | 1.137 | 分段变化(后段更亮)<br>`r`: 0.75→1.37 1.0→3.27 1.5→3.12<br>`lit o·e`: 0.15|0·0 0.30|0·0 0.50|0·0 0.75|230·322 1.00|162·306 1.50|235·435 2.00|5·10 3.00|1·2 | 5槽/5def URP×3 Mob/AlphaBlend×1 ExtraColor×1 | **锅H′**+锅T(边界) | `Prefabs/Explosion Hand UI 0,5s delay.prefab:20131`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` · `资料/比对基线/sweep_{orig,exp}.tsv`(r 序列见左) · `资料/特效还原_进度与交接.md:114,128-130` | 中 | **H1** **T1** |
| SAU_CardDraw | 亮 | 1.107 | **全程恒定**<br>`r`: 0.5→3.05 0.75→3.02 1.0→2.98<br>`lit o·e`: 0.15|0·1 0.30|48·95 0.50|68·140 0.75|128·272 1.00|152·333 1.50|1·2 2.00|0·0 3.00|0·0 | 6槽/6def URP×2 Sprites/Mask×1 Sprites/Default×1 ExtraColor×1 | **锅H′** | `Prefabs/SAU_CardDraw.prefab:5365`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| BacklashEffect | 亮 | 1.095 | 分段变化(前段更亮)<br>`r`: 0.3→8.78 0.5→3.16 0.75→2.80 1.0→1.88 1.5→2.81 2.0→4.84<br>`lit o·e`: 0.15|0·756 0.30|550·1815 0.50|1322·2782 0.75|1426·3438 1.00|1253·2550 1.50|1099·3230 2.00|1285·3303 3.00|1·4 | 4槽/4def DissolveMask×1 URP×1 Legacy/Add×1 PremulGrey×1 | **锅A′**+**锅H′** | `Prefabs/BacklashEffect.prefab:10157`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` · def/trail 槽 **def[0] `Generic Particle Dissolve For Sprites Multiply Edge`(Everguild/FX/Particle Dissolve Mask)** 的 `texNames` = `['_Disolve']`（**没有 `_MainTex`/`_BaseMap`**）· `WFParticlesExtraColor.shader:23,101` | 中 | **H1** **A1** |
| Buff_Green_Agenda | 亮 | 1.018 | 近似恒定(后段更亮)<br>`r`: 0.15→2.06 0.3→2.34 0.5→3.19 0.75→4.12<br>`lit o·e`: 0.15|274·306 0.30|329·398 0.50|198·309 0.75|99·280 1.00|35·90 1.50|0·0 2.00|0·0 3.00|0·0 | 7槽/9def URP×4 ExtraColor(我名)×2 ExtraColor×1 | **锅H′** | `Prefabs/Buff_Green_Agenda.prefab:896`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| Buff_Sororitas_quick_fire_intense | 亮 | 1.008 | 分段变化(后段更亮)<br>`r`: 0.15→2.74 0.3→2.71 0.5→2.45 0.75→2.26 1.0→4.72 1.5→5.28 2.0→4.17<br>`lit o·e`: 0.15|290·637 0.30|445·734 0.50|669·945 0.75|821·1050 1.00|351·767 1.50|239·813 2.00|98·175 3.00|0·0 | 6槽/7def URP×3 ExtraColor(我名)×1 AlphaMask2×1 ExtraColor×1 | **锅H′** | `Prefabs/Buff_Sororitas_quick_fire_intense.prefab:64003`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| Buff_Sororitas_quick_fire | 亮 | 1.007 | 分段变化(后段更亮)<br>`r`: 0.15→2.74 0.3→2.74 0.5→2.56 0.75→2.42 1.0→6.12<br>`lit o·e`: 0.15|284·629 0.30|428·709 0.50|629·890 0.75|764·1002 1.00|249·676 1.50|59·674 2.00|34·54 3.00|0·0 | 6槽/7def URP×3 ExtraColor(我名)×1 AlphaMask2×1 ExtraColor×1 | **锅H′** | `Prefabs/Buff_Sororitas_quick_fire.prefab:766`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| Buff EC On Warlord When Creating Card | 亮 | 0.987 | 分段变化(前段更亮)；**原版峰值那一帧对得上**(r=1.00)<br>`r`: 0.15→2.97 0.3→3.24 0.5→3.29 0.75→3.36 1.0→2.39 1.5→1.00 2.0→1.00 3.0→1.01<br>`lit o·e`: 0.15|412·439 0.30|555·616 0.50|616·678 0.75|405·447 1.00|139·148 1.50|557·558 2.00|385·384 3.00|283·287 | 11槽/12def URP×7 Sprites/Mask×1 Sprites/Default×1 Mob/Add×1 | **锅H′**+锅T | `Prefabs/Buff EC On Warlord When Creating Card.prefab:5965`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` · `资料/比对基线/sweep_{orig,exp}.tsv`(r 序列见左) · `资料/特效还原_进度与交接.md:114,128-130` | 中高 | **H1** **T1** |
| UM_CardDraw | 亮 | 0.950 | **全程恒定**<br>`r`: 0.3→2.65 0.5→2.60 0.75→2.58 1.0→2.52<br>`lit o·e`: 0.15|40·73 0.30|68·140 0.50|108·212 0.75|156·315 1.00|125·270 1.50|0·0 2.00|0·0 3.00|0·0 | 6槽/6def URP×3 Sprites/Mask×1 Sprites/Default×1 ExtraColor×1 | **锅H′** | `Prefabs/UM_CardDraw.prefab:25197`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| Buff EC Stimulate | 亮 | 0.942 | 分段变化(前段更亮)<br>`r`: 0.15→1.89 0.3→2.42 0.5→2.76 0.75→2.59 1.0→2.56 1.5→3.01 2.0→1.06<br>`lit o·e`: 0.15|88·90 0.30|197·224 0.50|272·314 0.75|400·429 1.00|407·451 1.50|390·444 2.00|173·182 3.00|2·2 | 12槽/13def URP×7 Sprites/Mask×1 Sprites/Default×1 Mob/Add×1 | **锅H′** | `Prefabs/Buff EC Stimulate.prefab:25607`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| Psychic_Lightning_warlord_OLD | 亮 | 0.935 | 分段变化(后段更亮)<br>`r`: 0.15→2.23 0.3→2.55 0.5→7.07<br>`lit o·e`: 0.15|404·541 0.30|1024·1335 0.50|432·1130 0.75|0·0 1.00|0·0 1.50|0·0 2.00|0·0 3.00|0·0 | 5槽/7def URP×3 ExtraColor(我名)×2 | **锅H′** | `Prefabs/Psychic_Lightning_warlord_OLD.prefab:569`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| StunEffect | 亮 | 0.934 | 分段变化(后段更亮)<br>`r`: 0.15→0.68 0.3→0.65 0.5→0.95 0.75→1.20 1.0→3.89 1.5→4.23 2.0→4.04 3.0→4.49<br>`lit o·e`: 0.15|371·291 0.30|1018·684 0.50|1198·1142 0.75|560·224 1.00|152·242 1.50|141·240 2.00|156·242 3.00|129·206 | 4槽/5def URP×2 Mob/Add×1 ExtraColor(我名)×1 | **锅H′** | `Prefabs/StunEffect.prefab:442`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| CamouflageEffect | 亮 | 0.927 | 近似恒定(持平)<br>`r`: 0.15→2.69 0.3→2.54 0.5→2.51 0.75→2.64 1.0→2.61 1.5→2.13 2.0→1.97 3.0→1.89<br>`lit o·e`: 0.15|1038·1498 0.30|1222·1636 0.50|1254·1601 0.75|1011·1445 1.00|848·1313 1.50|1254·1680 2.00|1510·1850 3.00|2140·2548 | 4槽/4def Sprites/Mask×1 Sprites/Default×1 UnlitUVscroll×1 URP×1 | **锅A′** | def/trail 槽 **def[2] `Camouflage_Icon`(Everguild/FX/Unlit UV scroll)** 的 `texNames` = `['_SecondaryTex']`（**没有 `_MainTex`/`_BaseMap`**）· `WFParticlesExtraColor.shader:23,101` | 低 | **A1** |
| Buff_DiabolicStrength | 暗 | 0.918 | **全程恒定**<br>`r`: 0.15→0.39 0.3→0.39 0.5→0.41 0.75→0.43<br>`lit o·e`: 0.15|941·779 0.30|871·731 0.50|720·595 0.75|370·296 1.00|0·0 1.50|0·0 2.00|0·0 3.00|0·0 | 6槽/6def URP×6 | **锅D**（软粒子嫌疑） | `Prefabs/Buff_DiabolicStrength.prefab:20285` · `_SoftParticlesEnabled=1` 的槽 1 个、`_SoftParticleFadeParams` 记成 `0|0|0|0` | 低 | **D2**→**D1** |
| BulletImpact_Kroot_KrootRifle | 亮 | 0.916 | **单点**（只有 0.5 一个时刻两边都亮）<br>`r`: 0.5→2.50<br>`lit o·e`: 0.15|35·51 0.30|43·66 0.50|64·112 0.75|54·123 1.00|0·0 1.50|0·0 2.00|0·0 3.00|0·0 | 10槽/11def URP×3 Mob/Add×3 ExtraColor×3 ExtraColor(我名)×1 | **锅H′** | `Prefabs/BulletImpact_Kroot_KrootRifle.prefab:64768`(rendererSlots) · boost 槽 **7 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 低（单点/双点） | **H1** |
| Basic_army_DA | 暗 | 0.913 | 分段变化(后段更亮)；**原版峰值那一帧对得上**(r=0.81)<br>`r`: 0.15→0.43 0.3→0.34 0.5→0.35 0.75→0.38 1.0→0.81 1.5→0.95<br>`lit o·e`: 0.15|261·202 0.30|461·332 0.50|624·433 0.75|711·428 1.00|1024·981 1.50|432·393 2.00|0·0 3.00|0·0 | 8槽/8def URP×6 ExtraColor×2 | **锅D**（软粒子嫌疑）+锅T | `资料/比对基线/sweep_{orig,exp}.tsv`(r 序列见左) · `资料/特效还原_进度与交接.md:114,128-130` · `Prefabs/Basic_army_DA.prefab:15710` · `_SoftParticlesEnabled=1` 的槽 2 个、`_SoftParticleFadeParams` 记成 `0|0|0|0` | 中 | **T1** **D2**→**D1** |
| Basic_army_DA_Intense | 暗 | 0.909 | 分段变化(后段更亮)；**原版峰值那一帧对得上**(r=0.86)<br>`r`: 0.15→0.43 0.3→0.34 0.5→0.35 0.75→0.38 1.0→0.86 1.5→1.22<br>`lit o·e`: 0.15|261·202 0.30|461·332 0.50|624·433 0.75|711·428 1.00|1067·1028 1.50|525·604 2.00|1·1 3.00|0·0 | 9槽/9def URP×6 ExtraColor×3 | **锅D**（软粒子嫌疑）+锅T | `资料/比对基线/sweep_{orig,exp}.tsv`(r 序列见左) · `资料/特效还原_进度与交接.md:114,128-130` · `Prefabs/Basic_army_DA_Intense.prefab:74582` · `_SoftParticlesEnabled=1` 的槽 2 个、`_SoftParticleFadeParams` 记成 `0|0|0|0` | 中 | **T1** **D2**→**D1** |
| CreateCard BlackLegion | 暗 | 0.881 | **单点**（只有 1.5 一个时刻两边都亮）<br>`r`: 1.5→0.41<br>`lit o·e`: 0.15|36·64 0.30|40·67 0.50|42·72 0.75|42·82 1.00|42·92 1.50|80·107 2.00|56·56 3.00|0·0 | 10槽/10def URP×6 ExtraColor×2 DissolveMask×1 Mob/Add×1 | **锅A′**+**锅D**（软粒子嫌疑） | `Prefabs/CreateCard BlackLegion.prefab:50033` · `_SoftParticlesEnabled=1` 的槽 2 个、`_SoftParticleFadeParams` 记成 `0|0|0|0` · def/trail 槽 **def[0] `Generic Particle Dissolve For Sprites`(Everguild/FX/Particle Dissolve Mask)** 的 `texNames` = `['_Disolve']`（**没有 `_MainTex`/`_BaseMap`**）· `WFParticlesExtraColor.shader:23,101` | 低（单点/双点） | **D2**→**D1** **A1** |
| CreateCard EC Elixir | 亮 | 0.849 | 分段变化(持平)；**原版峰值那一帧对得上**(r=1.14)<br>`r`: 0.15→3.23 0.3→2.26 0.5→2.57 0.75→1.38 1.0→1.14 1.5→2.41<br>`lit o·e`: 0.15|283·360 0.30|448·542 0.50|370·515 0.75|215·385 1.00|335·340 1.50|162·207 2.00|20·49 3.00|0·0 | 11槽/12def URP×6 ExtraColor×3 DissolveMask×1 ExtraColor(我名)×1 | **锅A′**+**锅H′**+锅T | `Prefabs/CreateCard EC Elixir.prefab:6331`(rendererSlots) · boost 槽 **3 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` · `资料/比对基线/sweep_{orig,exp}.tsv`(r 序列见左) · `资料/特效还原_进度与交接.md:114,128-130` · def/trail 槽 **def[0] `Generic Particle Dissolve For Sprites EC Elixir`(Everguild/FX/Particle Dissolve Mask)** 的 `texNames` = `['_Disolve']`（**没有 `_MainTex`/`_BaseMap`**）· `WFParticlesExtraColor.shader:23,101` | 中高 | **H1** **T1** **A1** |
| BulletImpact_Kroot_KrootScattergun | 亮 | 0.844 | **全程恒定**<br>`r`: 0.5→2.33 0.75→2.29 1.0→2.57<br>`lit o·e`: 0.15|34·53 0.30|51·71 0.50|62·101 0.75|76·122 1.00|72·134 1.50|0·26 2.00|0·0 3.00|0·0 | 10槽/11def URP×3 Mob/Add×3 ExtraColor×3 ExtraColor(我名)×1 | **锅H′** | `Prefabs/BulletImpact_Kroot_KrootScattergun.prefab:35285`(rendererSlots) · boost 槽 **7 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| DeckBuff_Green | 暗 | 0.841 | 分段变化(后段更亮)<br>`r`: 0.15→0.40 0.3→0.42 0.5→0.43 0.75→0.44 1.0→2.59<br>`lit o·e`: 0.15|4622·3871 0.30|5046·4251 0.50|5332·4472 0.75|5366·4478 1.00|388·942 1.50|0·0 2.00|0·0 3.00|0·0 | 6槽/7def URP×4 ExtraColor×1 ExtraColor(我名)×1 | **锅D**（软粒子嫌疑） | `Prefabs/DeckBuff_Green.prefab:5616` · `_SoftParticlesEnabled=1` 的槽 1 个、`_SoftParticleFadeParams` 记成 `0|0|0|0` | 低 | **D2**→**D1** |
| Recon Scan Purple | 亮 | 0.833 | 近似恒定(持平)<br>`r`: 0.15→2.30 0.3→2.42 0.5→2.54 0.75→1.91 1.0→1.56<br>`lit o·e`: 0.15|115·163 0.30|109·143 0.50|111·150 0.75|138·186 1.00|114·132 1.50|0·0 2.00|0·0 3.00|0·0 | 7槽/8def URP×3 Sprites/Mask×1 Sprites/Default×1 UnlitUVscroll×1 | **锅H′** | `Prefabs/Recon Scan Purple.prefab:5593`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| Psychic_Lightning_warlord_chaos | 亮 | 0.828 | 近似恒定(前段更亮)<br>`r`: 0.15→2.82 0.3→1.76<br>`lit o·e`: 0.15|611·1143 0.30|1075·1599 0.50|18·68 0.75|0·0 1.00|0·0 1.50|0·0 2.00|0·0 3.00|0·0 | 5槽/7def URP×3 ExtraColor(我名)×2 | **锅H′** | `Prefabs/Psychic_Lightning_warlord_chaos.prefab:15381`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 低（单点/双点） | **H1** |
| CreateCard Orks | 亮 | 0.821 | 分段变化(后段更亮)<br>`r`: 0.15→1.97 0.3→1.56 0.5→2.27 0.75→3.89 1.0→1.55 1.5→3.33 2.0→3.05<br>`lit o·e`: 0.15|199·223 0.30|444·459 0.50|414·533 0.75|380·684 1.00|639·896 1.50|329·620 2.00|451·808 3.00|0·0 | 10槽/10def URP×7 ExtraColor×2 DissolveMask×1 | **锅A′**+**锅H′** | `Prefabs/CreateCard Orks.prefab:25720`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` · def/trail 槽 **def[0] `Generic Particle Dissolve For Sprites Orks Cardback`(Everguild/FX/Particle Dissolve Mask)** 的 `texNames` = `['_Disolve']`（**没有 `_MainTex`/`_BaseMap`**）· `WFParticlesExtraColor.shader:23,101` | 中高 | **H1** **A1** |
| CreateCard Orks 1 | 亮 | 0.821 | 分段变化(后段更亮)<br>`r`: 0.15→1.97 0.3→1.56 0.5→2.27 0.75→3.89 1.0→1.55 1.5→3.33 2.0→3.05<br>`lit o·e`: 0.15|199·223 0.30|444·459 0.50|414·533 0.75|380·684 1.00|639·896 1.50|329·620 2.00|451·808 3.00|0·0 | 10槽/10def URP×7 ExtraColor×2 DissolveMask×1 | **锅A′**+**锅H′** | `Prefabs/CreateCard Orks 1.prefab:20835`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` · def/trail 槽 **def[0] `Generic Particle Dissolve For Sprites Orks Cardback`(Everguild/FX/Particle Dissolve Mask)** 的 `texNames` = `['_Disolve']`（**没有 `_MainTex`/`_BaseMap`**）· `WFParticlesExtraColor.shader:23,101` | 中高 | **H1** **A1** |
| Explosion_Ground | 亮 | 0.815 | 近似恒定(前段更亮)<br>`r`: 0.15→3.98 0.3→2.91 0.5→2.30 0.75→2.22 1.0→2.01 1.5→2.13<br>`lit o·e`: 0.15|113·690 0.30|286·1123 0.50|567·1651 0.75|691·1797 1.00|713·1708 1.50|466·769 2.00|0·0 3.00|0·0 | 1槽/1def Premul×1 | **锅H′** | `Prefabs/Explosion_Ground.prefab:5031`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| CreateCard | 暗 | 0.810 | **单点**（只有 1.5 一个时刻两边都亮）<br>`r`: 1.5→0.45<br>`lit o·e`: 0.15|36·64 0.30|41·67 0.50|43·74 0.75|44·84 1.00|44·93 1.50|90·109 2.00|56·56 3.00|0·0 | 9槽/9def URP×5 ExtraColor×2 DissolveMask×1 Mob/Add×1 | **锅A′**+**锅D**（软粒子嫌疑） | `Prefabs/CreateCard.prefab:64660` · `_SoftParticlesEnabled=1` 的槽 1 个、`_SoftParticleFadeParams` 记成 `0|0|0|0` · def/trail 槽 **def[0] `Generic Particle Dissolve For Sprites`(Everguild/FX/Particle Dissolve Mask)** 的 `texNames` = `['_Disolve']`（**没有 `_MainTex`/`_BaseMap`**）· `WFParticlesExtraColor.shader:23,101` | 低（单点/双点） | **D2**→**D1** **A1** |
| BulletImpact_autogun_Shotgun | 亮 | 0.809 | 近似恒定(前段更亮)<br>`r`: 0.15→2.71 0.3→2.72 0.5→1.78 0.75→1.75<br>`lit o·e`: 0.15|73·97 0.30|60·154 0.50|82·108 0.75|98·127 1.00|0·0 1.50|0·0 2.00|0·0 3.00|0·0 | 11槽/12def ExtraColor×4 URP×3 Mob/Add×3 ExtraColor(我名)×1 | **锅H′** | `Prefabs/BulletImpact_autogun_Shotgun.prefab:25555`(rendererSlots) · boost 槽 **7 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| Toxic Vapors Damage | 亮 | 0.807 | 分段变化(前段更亮)<br>`r`: 0.75→2.81 1.0→2.76 1.5→1.28 2.0→1.73<br>`lit o·e`: 0.15|0·17 0.30|15·41 0.50|37·74 0.75|68·133 1.00|91·161 1.50|266·303 2.00|60·83 3.00|0·0 | 5槽/5def URP×4 ExtraColor×1 | **锅H′**+锅T(边界) | `Prefabs/Toxic Vapors Damage.prefab:10343`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` · `资料/比对基线/sweep_{orig,exp}.tsv`(r 序列见左) · `资料/特效还原_进度与交接.md:114,128-130` | 中 | **H1** **T1** |
| AcidSpraySweepAttack | 亮 | 0.800 | **全程恒定**<br>`r`: 0.3→1.95 0.5→2.29 0.75→2.23 1.0→2.22 1.5→2.33 2.0→2.12 3.0→2.25<br>`lit o·e`: 0.15|31·38 0.30|66·75 0.50|97·124 0.75|93·116 1.00|90·110 1.50|83·110 2.00|81·94 3.00|93·121 | 11槽/12def URP×6 ExtraColor×2 Sprites/Mask×1 Sprites/Default×1 | **锅H′** | `Prefabs/AcidSpraySweepAttack.prefab:20894`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| Pray_Exit | 亮 | 0.785 | 分段变化(持平)<br>`r`: 0.15→1.59 0.3→1.87 0.5→2.19 0.75→2.49 1.0→2.79 1.5→2.85 2.0→1.37<br>`lit o·e`: 0.15|237·263 0.30|262·294 0.50|289·337 0.75|336·399 1.00|364·471 1.50|344·635 2.00|173·250 3.00|20·27 | 7槽/8def URP×3 ExtraColor×2 DissolveMask×1 ExtraColor(我名)×1 | **锅H′** | `Prefabs/Pray_Exit.prefab:35184`(rendererSlots) · boost 槽 **3 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| Psychic_Lightning_blast | 亮 | 0.782 | 分段变化(后段更亮)<br>`r`: 0.15→1.50 0.3→2.19 0.5→7.49<br>`lit o·e`: 0.15|1913·2083 0.30|2992·3381 0.50|711·1946 0.75|0·0 1.00|0·0 1.50|0·0 2.00|0·0 3.00|0·0 | 5槽/7def URP×3 ExtraColor(我名)×2 | **锅H′** | `Prefabs/Psychic_Lightning_blast.prefab:20259`(rendererSlots) · boost 槽 **2 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| Buff_BL_Intense | 暗 | 0.779 | 分段变化(后段更亮)<br>`r`: 0.15→0.44 0.3→0.44 0.5→0.46 0.75→0.59 1.0→1.00<br>`lit o·e`: 0.15|438·379 0.30|418·356 0.50|335·276 0.75|234·198 1.00|82·82 1.50|24·24 2.00|0·0 3.00|0·0 | 7槽/7def URP×6 ExtraColor×1 | **锅D**（软粒子嫌疑） | `Prefabs/Buff_BL_Intense.prefab:40138` · `_SoftParticlesEnabled=1` 的槽 2 个、`_SoftParticleFadeParams` 记成 `0|0|0|0` | 低 | **D2**→**D1** |
| Recon Scan | 亮 | 0.760 | **全程恒定**<br>`r`: 0.15→2.65 0.3→2.27 0.5→2.24 0.75→1.97 1.0→1.92 1.5→2.03<br>`lit o·e`: 0.15|84·155 0.30|89·143 0.50|71·121 0.75|103·137 1.00|137·164 1.50|108·152 2.00|14·14 3.00|0·0 | 9槽/9def URP×4 Sprites/Mask×1 Sprites/Default×1 AlphaMask1×1 | **锅H′** | `Prefabs/Recon Scan.prefab:30535`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| Tank_Shot_Small | 暗 | 0.740 | 近似恒定(后段更亮)<br>`r`: 0.15→0.38 0.3→0.58<br>`lit o·e`: 0.15|600·372 0.30|934·699 0.50|142·9 0.75|5·12 1.00|2·9 1.50|4·11 2.00|6·10 3.00|0·0 | 4槽/4def URP×4 | **锅D**（软粒子嫌疑） | `Prefabs/Tank_Shot_Small.prefab:15282` · `_SoftParticlesEnabled=1` 的槽 2 个、`_SoftParticleFadeParams` 记成 `0|0|0|0` | 低（单点/双点） | **D2**→**D1** |
| Buff_Red | 暗 | 0.726 | **全程恒定**<br>`r`: 0.15→0.48 0.3→0.48 0.5→0.49 0.75→0.53<br>`lit o·e`: 0.15|876·775 0.30|791·700 0.50|637·551 0.75|349·296 1.00|0·0 1.50|0·0 2.00|0·0 3.00|0·0 | 4槽/4def URP×4 | **锅D**（软粒子嫌疑） | `Prefabs/Buff_Red.prefab:20077` · `_SoftParticlesEnabled=1` 的槽 1 个、`_SoftParticleFadeParams` 记成 `0|0|0|0` | 低 | **D2**→**D1** |
| Buff_Green_DA | 亮 | 0.702 | 近似恒定(持平)<br>`r`: 0.15→1.75 0.3→2.02 0.5→2.44 0.75→2.08 1.0→1.27<br>`lit o·e`: 0.15|327·366 0.30|334·404 0.50|324·393 0.75|229·383 1.00|69·111 1.50|46·46 2.00|15·15 3.00|0·0 | 8槽/11def URP×3 ExtraColor(我名)×3 ExtraColor×2 | **锅H′** | `Prefabs/Buff_Green_DA.prefab:5911`(rendererSlots) · boost 槽 **4 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中高 | **H1** |
| BulletImpact_AcidSpit | 亮 | 0.700 | 近似恒定(后段更亮)<br>`r`: 0.15→1.21 0.3→1.78 0.5→2.13 0.75→2.05 1.0→2.02<br>`lit o·e`: 0.15|82·86 0.30|108·145 0.50|187·291 0.75|203·286 1.00|160·214 1.50|4·4 2.00|0·0 3.00|0·0 | 6槽/6def URP×5 ExtraColor×1 | 判不出 | `资料/比对基线/sweep_{orig,exp}.tsv` 该行 · `Prefabs/BulletImpact_AcidSpit.prefab:10586`(def 与槽两边对不出差异) | 低 | **D1** |
| Recon Scan 2 | 亮 | 0.697 | **全程恒定**<br>`r`: 0.15→2.19 0.3→2.08 0.5→2.00 0.75→1.84 1.0→1.92 1.5→2.02<br>`lit o·e`: 0.15|88·131 0.30|82·117 0.50|63·93 0.75|96·109 1.00|139·164 1.50|112·154 2.00|12·12 3.00|0·0 | 6槽/6def URP×2 Sprites/Mask×1 Sprites/Default×1 UnlitUVscroll×1 | **锅H′** | `Prefabs/Recon Scan 2.prefab:15459`(rendererSlots) · boost 槽 **1 个** · `WFParticlesExtraColor.shader:22,157-158` · `WarpforgeEffectBinder.cs:114-117` | 中 | **H1** |
| Buff_Tau_Kroot | 暗 | 0.695 | **全程恒定**<br>`r`: 0.15→0.49 0.3→0.50 0.5→0.50 0.75→0.51<br>`lit o·e`: 0.15|1157·972 0.30|1039·881 0.50|826·676 0.75|462·358 1.00|18·18 1.50|1·1 2.00|0·0 3.00|0·0 | 4槽/4def URP×3 ExtraColor×1 | **锅D**（软粒子嫌疑） | `Prefabs/Buff_Tau_Kroot.prefab:546` · `_SoftParticlesEnabled=1` 的槽 1 个、`_SoftParticleFadeParams` 记成 `0|0|0|0` | 低 | **D2**→**D1** |

## 三、最值得先动的 3 条（+ 1 条「先看」）

### ① 锅H′ 的根：`_EmissionColor` 的 shader 默认值 / 导出器回读 —— 本块 52 条；全项目 741 条效果共用同一个 shader
**为什么先修它**：`Everguild/FX/Extra Color` 一家覆盖 **741 条效果**（`普查产出_0918/shader原件可用性_表.md:14`），
再加上 `Mobile/Particles/*`、`Legacy Shaders/Particles/*`、`Everguild/FX/Unlit UV scroll` 等 **25 个原版名**都指向同一个自建 shader（`WarpforgeShaderMap.cs:72-147`）。
本块 **58/66** 条至少有一个槽走这条路，**54 条「亮」里 52 条**命中。改一处（`WFParticlesExtraColor.shader:22` 的默认值，或导出器不再回读已解析的材质）
就能同时动这 52 条 + 全项目其余同类。
**判据（可复跑、不启 Unity）**：`Prefabs/*.prefab` 的 binder 块里，**只要同名的两份 def 里「原版 shader 名那份」`_EmissionColor` 是 0/无此属性，而「`WarpforgeVFX/Particles/Extra Color` 那份」是 `1|1|1|1`**，就说明那个 1 是默认值。
本块 50 份这样的 def **50/50 全部**是 `1|1|1|1`；对照的 122 份原版名 def 则散在 `0`(53) / `<1`(1+) / `>1`(44) / `≈1`(23) 四档。

### ② 锅A′ 那 9 处的**账**：「近似 shader 丢 `_MainTex`」在 09-18 已经被改走原件，但 sweep 没重跑
9 处、**9 条**效果：**`CreateCard Sabotage`(def[0] `Generic Particle Dissolve For Sprites Create Sabotage`) · `CreateCard` · `CreateCard Orks` ×2 · `CreateCard EC Elixir` ·
`CreateCard BlackLegion` · `BacklashEffect`(def[0]) · `FrenziedEffect`(def[2] `Generic Trait Particle Shine`) · `CamouflageEffect`(def[2] `Camouflage_Icon`)**。
这 9 个 def 的 `texNames` 只有 `_Disolve` / `_SecondaryTex` / `Texture2D_0bfdc50d…`（**没有 `_MainTex`**），而它们映射到的自建 Extra Color 采样 `_MainTex` ⇒ 一块**白面片**。
这是 `WarpforgeShaderMap.cs:88-96` 自己标着「**近似**：真实的溶解 / UV 滚动 / 顶点流没做」的那 10 个 shader ——
**而 09-18 的 `UseOriginal` 白名单（`WarpforgeShaderMap.cs:44-51`）已经把 `Particle Dissolve Mask` / `Particle Shine CVS` / `Unlit UV scroll` 三个改成走 bundle 原件了**（`:200-206`，优先级最高）。
⇒ 本块的**第 1 名 `CreateCard Sabotage`（33.17×）很可能已经变了**。「一修带一片」在这里恰好成立：不重跑就等于在修一个已经改过的地方。

### ③ 锅T（时序包络）—— 10 条；其中 8 条与锅H′ 并存，**光修材质修不干净**
`Buff_Mana`（前段 10×、峰值 3.3×）· `CreateCard Sabotage`（前段 62–65×、1.00s 恰好 1.00）· `Possession_self`（2.0s 恰好 1.00，其余 2.9–8.5×）·
`Buff_Mana_2/_3 Holy/_Blood` · `WhileInPlay_Tzeentch` · `Lightning burst webway`（r 单调涨到 8.67）· `Lightning_Green` · `Explosion Hand UI` · `Basic_army_DA`(+`_Intense`)。
**为什么值得**：这些效果**在某个时刻本来是对的**（`r_peak` 落在 0.7–1.4），说明公式/材质在那一点上是匹配的 —— 差的只是**包络**。
对这类只调亮度倍数会「把对的那一段改坏」。而且工具现成（`ParticleModuleProbe`），零资产风险。

### ④（不算「修」，是「先看」）锅D 那一簇「全程恒定 ~0.5×」的暗（12 条）= 旧台账的锅 D，**至今一个都没动过**
`Buff_Red`(0.48) · `Buff_Tau_Kroot`(0.50) · `Tank_Shot_Small`(0.48) · `Buff_DiabolicStrength`(0.40) · `Smoke_Explosion`(0.12) · `Buff_BL_Intense`(0.46)
—— **症状和 09-13 那张表里记的一模一样**：全程恒定、`lit` 只降 ~15%、**逐像素降 ~50%**（`dpp` −0.4…−1.3）。
**本轮新增的唯一线索**：这 12 条的**每一个**都有槽记着 `_SoftParticlesEnabled=1` 而 `_SoftParticleFadeParams={0,0,0,0}`（死值）。
⚠️ **但它是必要不充分**：32/66 条有这种槽，其中 20 条是「亮」的 ⇒ 单靠它解释不了方向。**所以这一簇先做 D2→D1，别按数字改。**

## 四、三条「旧锅」在本块的实测 —— 以及「为什么修了 E 组反而变大」

| 旧锅 | 说它覆盖 | 本块实测（**按 09-17 的 prefab**） | 怎么量的 |
|---|---|---|---|
| **A** `trailMaterial` 绑到贴图全丢的 def | 35 条 | 今天 **0/66**：`trailSlots` 指向的 def **没有一个缺 `_MainTex`/`_BaseMap`**；24 条 `trailSlots` 全 -1 | 解 66 个 prefab 的 `trailSlots`（**hex blob**）+ def 的 `texNames/texVals`；def 级缺贴图 **0/511** |
| **F** prefab 发射器不画 | 16 条 | **0 个** Mesh 模式渲染器（`m_RenderMode=4` 本块不存在）；`n199==n198` 在 **66/66** 成立 | 逐 prefab 数 `!u!199`/`!u!198` + 读 `m_RenderMode`/`m_Mesh` |
| **B/E** 残留混合值 | 61 条 | 签名 `(_SrcBlend=1,_DstBlend=0)` 只剩 `Mobile/Particles/Additive`×15 + `Mobile/Particles/Alpha Blended`×4 个 def —— 这两个名字**正好被 `InferBlend` 命中**（`WarpforgeShaderMap.cs:161-177`） | 按 shader 名分组统计 def 的 `(_SrcBlend,_DstBlend,_ZWrite)` 三元组 |

🔴 **但上面三行说的是「今天的 prefab」，而 E 组那 241 个是 09-15 量出来的** —— 锅A 的贴图修复恰好发生在 **两者之间**：
`资料/特效还原_进度与交接.md:204` 记「自建 shader 带贴图 **0/207 → 290/316**」，那是 **09-17** 那趟重导。
⇒ **09-15 量到的偏亮里，很可能有一大块就是锅A**；今天它已经不在资产里了。**本块无法区分这两者**，因为 09-15 的 prefab 没有留档（`Assets/WarpforgeVFX/` 是 gitignore 的，见 `.gitignore:26`）。
同理，**09-17 的重导与 09-18 的 `UseOriginal` 白名单都在 sweep 之后**（`shader原件可用性_表.md:40-63`）——
⇒ **「E 组现在是多少」这件事，全项目目前没有数字**。

**这解释了「已修的锅没让 E 组变小」**（三条，(b) 是主因）：
(a) 本块（纯精灵图）不是 A/F 的地盘 —— A 是拖尾槽的锅、F 是 Mesh/None 渲染模式的锅，两者落在**别的技术构成块**；
(b) **E 组那 241 个数字本身已经过期**：它是 09-15 的，而 09-17 的重导修掉了「带贴图 0/207」这种量级的缺口 ——
     **没人重跑过 sweep**，所以「176 → 241」这个变化里，**修之前和修之后混在一张表里**；
(c) 09-15 那次「乘法改加法」（`WFParticlesExtraColor.shader:138-158` 的注释自己写了）**恰好在 E 组重测之前落地**：
     改之前 `_EmissionColor=1` 是 `tex*1*1`（中性），改之后是 `tex + tex*1`（翻倍）⇒ 本块 58/66 条的偏亮都在它上面。
     ⚠️ 这条是**推断**：原版 `Everguild/FX/Extra Color` 的 HLSL 已剥（只剩字节码），**「原版到底加不加 emission」本块没证到**。
     要证就走 CLAUDE.md 记的那条路：`工具/dump_shader_blob.py` 解 DXBC，看那个颜色常量在片段里是加还是乘。

## 四点五、**动手之前先做的一件事**

**重跑一次 sweep 出台账**（`资料/比对基线/README.md:27-44` 三行命令，25 分钟，两趟必须分两个进程）。
理由：本块 66 条、以及整个 E 组 241 条，**症状数字全部来自 09-15，而资产已经被 09-17 重导过**。
现在按这些数字排优先级，很可能把已经修好的排在前面、把新坏的漏掉（锅A 就是实例）。

## 五、判不出来的（如实列，不硬编根因）

1. **12 条「暗」的逐像素 ~0.5× 机制**：`Buff_Red` / `Buff_Tau_Kroot` / `Tank_Shot_Small` / `Buff_DiabolicStrength` / `Smoke_Explosion` / `Buff_BL_Intense`
   + `Basic_army_DA` / `_Intense` / `DeckBuff_Green` / `CreateCard` / `CreateCard BlackLegion` / `ScytheAssault`。
   它们的材质槽全部走 **原版 `URP/Particles/Unlit` + 属性逐个照抄** 这条路 ⇒ **没有一条我能从数据上指出「哪个值是错的」**。
   旧表把它们归锅 D（要实拍），本轮**没能推翻也没能推进**，只多了一条软粒子线索（必要不充分）。
2. **一条完全判不出**：`BulletImpact_AcidSpit`（6 槽 URP×5 + ExtraColor×1；`boost=0`、`soft=1`、`r` 全程 1.21→2.13、`spread` 0.56）
   —— 就是一个「恒定 ~2×」而**指不出任何一个值是错的**。同类的还有 `Explosion Hand UI 0,5s delay`（已按边界归 **锅T**：`r` = 0.75s→1.37、1.0s→3.27、1.5s→3.12，`spread` 0.867 差一点到阈值）
   与 `CamouflageEffect`（已归 **锅A′**，但那是**判断**：它 8 个时刻恒定 1.89–2.69×，与「白面片」一致，也可能来自另外那 2 个 URP 发射器 ⇒ 置信打**低**）。
3. **`EnvironmentalCondition Saim Hann Webway Rift`**：台账 13.54 是从 **1 个采样点**（3.00s，原版 lit 65 vs 导出 937）算出来的，
   而两边**任何时刻都没同时亮到能做比较**（原版全程 2–65 个亮点 = 几乎空）。⇒ **它到底是不是「偏亮」本身就不该信这个数**（55 渲染器 / 24 def，可能是原版那边根本没跑起来）。
4. **原版 `Everguild/FX/Extra Color` 的公式**（本块 122 份 def 的归属全靠它）：HLSL 已剥，**本块没证到**。

## 六、复跑办法（全部只读，不启 Unity）

```
# 全部纯 Python、只读、不启 Unity（按顺序跑）：
P="D:/2/Warpforge_tools/py312/python.exe"; E=d:/4/_tmp_view/E0918
$P $E/b1a_collect_b1a.py   # sweep(orig/exp)+tech+shader对账+台账 → b1a_raw.json
$P $E/b1a_prof_b1a.py      # 8 时刻 lit/sum、density(ln lit 比) 与逐像素(dpp) 分解 → b1a_prof.json/.txt
$P $E/b1a_an3.py           # 66 个 prefab 的 binder defs + 贴图 guid → b1a_defs2.json
$P $E/b1a_shape.py         # r 序列 / spread / r_peak → b1a_shape.json/.txt
$P $E/b1a_an5.py           # rendererSlots/trailSlots（**hex 小端 int32**）→ b1a_slots.json
$P $E/b1a_an6.py           # boost(锅H′)/soft 计数 → b1a_final.json
```
⚠️ **`rendererSlots`/`trailSlots` 不是 YAML 列表**，是 `0100000002000000…` 这种 **hex blob**（小端 int32，`ffffffff` = -1）。
第一遍用 `^\s+- (-?\d+)$` 去解会解出**空数组**，于是误得「trailSlots 全空、锅A 已死」—— **本块第一遍就是这么错的**（差点写进结论）。
中间产物落在 `d:/4/_tmp_view/E0918/`：`b1a_prof.json`（sweep+shader 合并）· `b1a_shape.json`（r 序列）·
`b1a_slots.json`（**解好的 `rendererSlots`/`trailSlots`/defs**，`rendererSlots` 是 hex 小端 int32）· `b1a_final.json`（boost/soft 计数）。
