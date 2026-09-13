# 特效 E 组 176 个 · 逐效果根因表（2026-09-13 第三十三轮，派 3 个子代理产出）

> **这份表解决什么**：`特效还原_进度与交接.md` 与 `特效E组_根因初判.md` 一直只有
> 「**结构性结论**」（哪一类锅、多少条），而**逐效果的那一层**（这个效果是哪个发射器 /
> 哪个材质槽出的问题）只存在于当时的对话里 —— **换个会话就没了**，只能重新挖一遍。
> 这里把它落盘。
>
> **怎么用**：按「锅」分组。要修某一条时，先在下面找到它属于哪类锅、嫌疑是哪个槽，
> 再回 `特效还原台账.tsv` 看它的数值。
>
> ⚠️ **台账仍然过期**：`特效还原台账.tsv` 落盘时间 **2026-09-11 20:21**，
> 比这一串修复（09-12 的 P1-a0、09-13 的混合/抓屏/shader 映射）**都老**。
> 下面所有「亮 / 暗」方向**只作结构性线索**，**不要按它定改动量** ——
> 先重跑 sweep 刷新台账。
>
> 判据来源：`资料/比对基线/per_effect_tech.json`（技术构成）·
> `资料/特效还原台账.tsv`（判定 / 亮度比 / `|ln|`）· `效果库报告.tsv`（发射器 / 渲染器 / 循环发射器）·
> 导出后的 `Assets/WarpforgeVFX/{Prefabs,Materials}/`。

---

## 一、精灵图系（89 个）

> ⚠️ 先纠一个**口径**：`per_effect_tech.json` 的 `sr`/`mr` 在**全部 958 行都是 true**（常量），
> `工具/analyze_sweep.py:148-149` 直接照抄当标签。实测 `Prefabs/Buff_Mana.prefab` 里
> **一个 `!u!212 SpriteRenderer` 都没有** ⇒ 这 89 个的真定义是
> 「**无 Mesh 粒子 / 无扭曲 / 无 Matcap 的纯 billboard 粒子**」。
> 两条负面结论（都实测过）：① 89/89 的 `!u!199` 数与 tech 的 `psr` 一致 → **本块没丢发射器**；
> ② `Editor/MatKeyCheck.cs` 的「材质缓存键碰撞」在这 89 里 **0 碰撞，可排除**。

### 锅 A —— `trailMaterial` 槽被绑到「贴图全丢的重导残留 def」（**35 条**，30 亮 / 5 暗）

**机制**：42 个槽位的 `texNames` **42/42 为空**，而同一 prefab 有同名兄弟 def 带 `_MainTex` + `_Color`
（`Prefabs/Buff_Mana.prefab` 的 `materials[2]` vs `[1]`）⇒ `_MainTex` 落回 `"white"`。
**重灾材质**：`LightningTrail` · `Lightning_001_add` · `LightningTrail_environment` ·
`smoke_light` · `smokesoft_dark` · `FadingTrail_add`。

涉及效果：
`Buff_Mana` · `Buff_Mana_2` · `Buff_Mana_3 Holy` · `Buff_Mana_Blood` · `Buff_Eldar_Warp Spider` ·
`Psychic_Lightning_blast` · `Psychic_Lightning_warlord_chaos` · `Psychic_Lightning_warlord_OLD` ·
`Psychic_Lightning_down` · `Psychic_Lightning_down_Intense_white` · `StunEffect` · `StunEffect_idle` ·
`Lightning_Green` · `Lightning burst webway` · `MarkOfTzeentch_enchantment` ·
`Delightful_Agonies_Projectile` · `Possession_self` · `Pray_Exit` · `Teleport Trait Summon` ·
`Teleport Trigger Effect Quick` · `Buff EC Pink Twirl` · `Buff EC Pink Twirl_Intense` ·
`Buff EC On Warlord When Creating Card` · `CreateCard EC Elixir` · `Buff_Sororitas_quick` ·
`Buff_Sororitas_fire` · `Buff_Sororitas_fire_intense` · `Buff_MarkOfSlaanesh` ·
`EnvironmentalCondition Ultramarines Thunderstorm` · `Sau_Dimensional_Breach_Spawn` ·
`DeckBuff_Green` · `DeckBuff_MoveToTop` · `Buff_Red_Intense` · `Buff_Green` · `Buff_Green_Intense`

### 锅 B —— 内置 Standard 残留混合值被自建 shader 当真值用（**32 条**，28 亮 / 4 暗）

**机制**：原版老 shader 把混合**写死在 pass 里**，材质上仍序列化着
`_SrcBlend=1`/`_DstBlend=0`/`_ZWrite=1`；自建 shader 用 `Blend [_SrcBlend][_DstBlend]` 间接寻址 ⇒
**One/Zero 不透明 + 写深度**（加法发光全废）。
🔴 **根因在 `WarpforgeEffectBinder.cs` 的混合判据**（见本节末「这一轮修了什么」）。
**重灾材质**（按命中行数）：`line_light`(10) · `ray_light`(11) · `Fire1`(8) · `smokesoft_blend`(5)。
原 shader 分布：`Mobile/Particles/Additive` 25 条 + `Mobile/Particles/Alpha Blended` 15 条。

涉及效果：
`Stealth_Proc_Eldar` · `Healing_Circles` · `Antimatter Explosion` · `BloodySplash` ·
`Sau_MasterChronomancer` · `Sau_DamageProtocol_Quick` · `SlayEffect` · `CodexEffect` ·
`CodexIdleEffect` · `Buff_Red_ork` · `BloodThirstEffect_Lose` · `BloodThirstEffect_Lose_Idle` ·
`Hive Fleet Ground Growth` · `Recall_Eldar_2` · `Recall_Eldar_2_reverse` · `Recall_Eldar_2_quick` ·
`Recall_Eldar_2_quick_UI` · `WhileInPlay_Nurgle` · `MarkOfNurgle_enchantment` · `ToxicMiasma_Board` ·
`ToxicMiasma_Venomthrope`

### 锅 C1 —— 原版 shader 没进映射表 ⇒ 保留占位材质（**16 条**，14 亮 / 2 暗）

**机制**：这 10 个 shader 名**不在** `WarpforgeShaderMap.Replacements`，`Shader.Find` 也找不到 ⇒
binder 返回 null ⇒ 该槽**保留占位材质**（导出时的 `URP/Particles/Unlit`）。
未映射清单（**已在本轮补进两张表**）：
`Everguild/FX/Particle Dissolve Mask` · `Multi Ray` · `TrailShader_1` · `TrailShader_Fading` ·
`Unlit UV scroll` · `Particle Shine Custom Vertex Streams` · `Particle Premultiply Greyscale Coloring` ·
`Alpha Mask One Layer` · `Alpha Masks Two Layer` · `Shader Graphs/Doomweaver effect`。

涉及效果（括号里是命中的材质槽）：
`Teleport Trait Summon`(`Teleport Trail`) · `BacklashEffect`(`Generic Particle Dissolve For Sprites Multiply Edge` / `Flames Loop Red`) ·
`Buff_Sororitas_quick_fire` / `_intense`(`Sororitas_Flames_TwoLayer`) ·
`TalentEffect` / `FrenziedEffect` / `SlayEffect`(`Flank Particle Custom`) · `CreateCard EC Elixir` ·
`Pray_Exit` · `LongRange_LoseTrait` ·
`BulletImpact_deathspinner_arc_alt` / `_DeathSpinner Test`(`Wispy_Trail_Deathspinner` / `Doomweaver effect`) ·
`Sau_Dimensional_Breach_Spawn`(`Wispy_Trail_Dimensional_Breach`) · `CamouflageEffect`(`Camouflage_Icon`) ·
`Buff_Blue_Tau`(`Hexagon_cylinder`) · `Environmental Condition Emperor's Children 2 Fumes`(`Sand Storm`)

### 锅 C2 —— `_ColorMode=1` 的材质把亮度写在 `_EmissionColor`，被静默丢掉（**11 条**，6 亮 / 5 暗，其中 4 条可量化偏暗）

**机制**：`WFParticlesExtraColor` 原来只读 `_Color`×`_MainTex`；这些 def 的亮度写在 `_EmissionColor`
（`DA_Winged_Sword_Glow_extra` = **4.62468** 而 `_Color`=1；`Square_Glow_border` = **4.62468** vs 2），
binder 的逐属性拷贝 `if (m.HasProperty(name))` 一看没这个属性就**静默跳过**。

涉及效果（括号里是命中的材质槽）：
`Basic_army_DA` / `_Intense`(`DA_Winged_Sword_Glow_extra`) · `DeckBuff_Green` / `DeckBuff_MoveToTop`(`Square_Glow_border`) ·
`CodexEffect` / `CodexIdleEffect`(`Iron_Halo 1_add`) · `Sau_MasterChronomancer`(`Cognition_Glyph` / `Damage Glyph` / `Heal_Glyph`) ·
`ArtificeEffect`(`Gears_Sheet_Extra`) · `CreateCard EC Elixir`(`Waterfall_ExtraColor`) ·
`Hive Fleet Ground Growth`(`Growth_2`) · `SlayEffect`(`Crosshair_4`)

### 锅 D —— **没查出根因的**（四条线索都干净）

`Smoke_Explosion` · `Buff_DiabolicStrength` · `Tank_Shot_Small` · `StunEffect_proc` ·
`Buff_Red` / `_Intense` · `Buff_Tau_Kroot` · `Buff_Orange` · `Buff_DarkOratory` ·
`Buff_Blue` / `_Blue_Tau` / `_Blue_Tau_quick` · `Buff_Drach'nyen` · `Buff_LegacyOfVengeance` ·
`Helfire Burst Target` · `Basic_army_UM` / `_Chant` / `_Chainsword` · `Tap Firepit 2` ·
`Buff_Tau_Air` / `_Water` / `_Fire` · `CantAttackEffect` · `Psychic_Lightning_down`

> 这几条在 **binder 记录、材质残留、mesh 槽、渲染模式四处都干净**，
> **只能实拍隔离渲染**（`EffectIso.Run`）才能定 —— **别按台账数字改**。
> 唯一可用信号是 `_Color`/`_BaseColor` 数值本身。

---

## 二、Mesh 粒子 / Matcap 系（42 个）

技术构成：MP = Mesh 粒子+精灵(31) · MM = Matcap+Mesh(9) · MS = Matcap+精灵(2)。
**本子集 42 行都不含抓屏扭曲**，所以 09-12 那次 `WFDistortion` 修复动不到它们。

### 锅 E —— 残留混合值（同锅 B，**29 条**）

`DaIrongobEffect`(`Particles` rm=5 · `line_light`) · `Goff_WaaaghRed_skulls`(`ringBig`/`ring` · `ray_light`) ·
`HeroOfTheEmpire OLD`(`MidRing`/`MidRing_Grow` · `ray_light`；`Trait Icon` rm=5) ·
`OrkBuff_KrumpDaGitz`(`MidRing_Grow` · `ray_light`；`OrkSkullParticle`) ·
`Sau_HealProtocol` / `Sau_HealProtocol_OLD` / `Sau_DamageProtocol` / `Sau_DamageProtocol_OLD` / `Protocol Base`
(`Flash select` · `line_light`；`Ring`/`Ring2` Cylinder) · `Buff_DaemonicPact`(`Smoke Trail` · `smokesoft_dark`) ·
`UM_TacticalInsight` / `UM_Buff`(`ring` · `ray_light`；`Lines` · `Flash1`) ·
`Duty Reset` / `DutyEffect` / `RegimentEffect`(`MidRing` · `ray_light`；`FX Shine For Animation`) ·
`BloodThirstEffect` / `BloodThirstEffect_Gain`(`MidRing` · `ray_light`；`Fire1`) ·
`Cosmic Serpent` / `Cosmic Serpent_Secondary`(`Line Disappear` · `line_light`；`Trail` rm=5) ·
`UM_AvengingSon`(`ring` · `ray_light`；`GoldField` · `treasure_ray`；`Fire1`) ·
`Recall_Eldar` / `BlueSummonCircle`(`Upward glow` Cylinder · `Circle_Hoop`；`Lines` · `line_light`；`Fire1`) ·
`Environmental Condition Necrons Earthquake`(57 发射器全屏叠加，**建议单列人工看**)

### 锅 F —— prefab 侧发射器**根本不画**（**16 条**）

`m_RenderMode: 4`(Mesh) 且 **`m_Mesh: {fileID: 0}`**：`Artillery Ground` · `Goff_Rok_Invasion`。
`m_RenderMode: 5`(None) 共 **18 个发射器、涉及 14 行**，其中 `KhaineBuff` 的 `Fire Twirl`
是该效果**唯一的 Matcap 发射器**。

### 锅 G —— 原版 shader 未进地图 ⇒ 落 URP 近似替代（**13 条**，Matcap 系 11 行全中）

`WFMatcap.shader` 只认 25 个属性，原 `Matcap Full Options` 的
`_BorderColor1/2` · `_Noise` · `_NOISECHANNEL_R` · `_USEEMISSION` · `_EmissionTex` 全无对应；
且 `WFMatcap.shader:144-146` **无条件** `_MainTex × _MatCap × _Intensity` ⇒
若原版只采样 `_MainTex` 就是**系统性变暗**（实测 `Rpp` 一致落在 0.42–0.65）。

涉及：`Tyranid_Burrow` · `Buff_Tyranid Armor`（关键字拼写不一致：材质 `_APPLYAMBIENTCOLOR`
vs shader `_APPLYAMBIENTCOLOR_ON`，见 `WFMatcap.shader:73,148`）· `Goff_ProphetOfDaWaaagh` ·
`Goff_GreatestWarboss` · `BeastSnagga_Summon`（都是 `RockDebris.mat`）·
`Explosion Fenrisian Monstrosities Extra Damage`(`IceSpike.mat`，唯一透明 matcap) ·
`VanguardIdleEffect`(`Vanguard_Frame VAT.mat` —— 原 shader `Matcap Full Options VAT` 两张地图都没登记)

### 🔎 一条**排除**证据（值得记）

`RockDebris.mat` 在 **4 行偏暗、2 行偏亮**（`Recall_Eldar_2_quick_ground` / `_warlord`）
⇒ **那 2 行是 prefab 锅、材质可排除**；同一材质出现两种方向 = 不是材质的问题。

## 三、抓屏扭曲系（45 个）

**结构性结论**：45 个里 **41 个的扭曲槽共用同一份材质 `Materials/RippleSubtle Distort.mat`**
（另 2 个 `Heat Distortion.mat`、1 个 `Explosion Distort.mat`、1 个 `Distortion Star Low Strength.mat`）。
四份都映射到 `WarpforgeVFX/FX/Distortion`（`WarpforgeShaderMap.cs`），
**读的是 `_CameraOpaqueTexture`，而原版（字节码实测）读 `_GrabPassTransparent`**
⇒ **一个 Renderer Feature 覆盖全部 41 条的扭曲分量**（**本轮已做**）。
台账 `技术构成` 的「抓屏扭曲」来源 = `per_effect_tech.json` 的 `fam` 字段，
45/45 与 `导出报告.tsv` 的「原 shader 含 Particle Distortion Affect Transparents」一致 —— **tag 无假阳**。

涉及效果（41 条，⭐ = 另叠「寿命差异 / 循环发射器 / 覆盖翻倍」，修扭曲时要一并处理）：
`Impact_blunt_lightning`⭐ · `SynapseEffect_NoIcon` · `BansheeHowl_blood` · `SynapseEffect` ·
`Rapturous Ruination Board` · `Slash Repeating 2x`⭐ · `BansheeHowl` · `BansheeHowl_warlord` ·
`Sword_Slash_DA`⭐ · `Environmental Condition Emperor's Children 1 Green`⭐ ·
`BulletImpact_artillery_arc` · `Artillery Manticore up 4x` · `Artillery Manticore up 2x` ·
`BulletImpact_artillery_manticore_spread`⭐ · `Environmental Condition Emperor's Children 1 Pink REJECTED`⭐ ·
`Telepathic_Control_target` · `Slash Repeating 5x` ·
`Environmental Condition Black Legion Helfire Outburst`⭐ · `Scream_purple`⭐ ·
`Cut Termie Lightning Claws`⭐ · `Explosion_Debris` · `Explosion_Debris_UI` · `Atk_JainasMor`⭐ ·
`Scream_Ragnar_Target` · `BansheeHowl_blood glow` · `Sororitas_BladeOfFaith`⭐ · `Scream_purple_nid`⭐ ·
`Sororitas_Destroy_Flames` · `Basic_army_UM_Hammer` · `Explosion Hive Fleet Arrival Tendrils OLD` ·
`Scream_gold`⭐ · `ToxicMiasma_Warlord`⭐(Rl2.23 覆盖翻倍) · `Basic_army_Guard_quick Master of Ordnance` ·
`Chaos_Destroy_Brutal`⭐ · `BulletImpact_Tau_Missile 10x`⭐ · `Artillery Manticore down 4x spread` ·
`BulletImpact_Psychophage`⭐ · `BulletImpact_Sonic Weapon 1` ·
`Psychic_Lightning_Genestealer_Benefictus`(Rl1.75/Rpp1.84 双翻倍) · `Vortex Warhead Impact` ·
`Hammer_Slam_SW`⭐

**另有各自原因 4 条**（不是 `_GrabPassTransparent` 那一个原因）：
- `Vortex Explosion Massive` —— 逐像素 0.033 = **不透明黑块**（混合残留，09-12 已修）＋循环发射器
- `Stealth_Proc_UM` —— 逐像素 0.049、lit 2242 但 sum≈0 = **同一个黑块**
- `Environmental Condition Astra Militarum Planetary Invasion` —— **9 个渲染器材质槽为 null**
  （prefab 里解析出 9 处 `fileID: 0`：`Las bullet controller`×5、`Missile controller` 等）
- `RemnantBody3D Aeldari` —— **循环发射器常驻不衰减层**（导出 8 帧恒定 660–735 亮点）

---

## 四、这一轮修了什么（把锅对应到修复）

| 锅 | 覆盖面 | 修在哪 | 验证 |
|---|---|---|---|
| **B / E**（残留混合值） | **61 条** | `WarpforgeEffectBinder` 的混合判据改成「一律按原版 shader 名推」 | `BlendProbe` 混合 **1099/1099** |
| **抓屏**（41 条） | **41 条** | `GrabPassTransparentFeature`（URP Renderer Feature 填 `_GrabPassTransparent`） | `DistortProbe` `_GrabPassAvailable = 1.0` |
| **C1**（未映射 shader） | **16 条** | 10 个 shader 补进两张表 | `BlendProbe` shader 解析 **70/70** |
| **C2**（`_EmissionColor`） | **11 条** | 自建 shader 补 `_EmissionColor`（两个 pass 的 CBUFFER 都补） | 同上 |

**还没修的**：锅 **A**（`trailMaterial` 绑到贴图全丢的 def，35 条）· 锅 **F**（prefab 发射器不画，16 条）·
锅 **G**（Matcap 属性表缺口，13 条）· 锅 **D**（4 条查不出根因，要实拍）·
**W 组 189 个「两边全程空」**（要先用 `关键词图标/_规则书关键词表.md` 附录那张触发表分类）。

> ⚠️ **改完任一条，先重跑 sweep 刷新台账再读数值**。
