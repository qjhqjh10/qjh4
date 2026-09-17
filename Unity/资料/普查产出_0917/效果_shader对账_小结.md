# 效果 → 用了哪些原版 shader · 对账表（958 行）· 2026-09-17

> 纯 Python 解析（`工具/_effect_shader_audit.py`）：**未启动 Unity、未写 `Assets/` 下任何文件**。
> 全表见同目录 `效果_shader对账.tsv`（958 行 + 表头，UTF-8 BOM，LF）。

## 〇、一句话 + 口径

✅ 第 3 格 **958/958 行齐全** ⇒ 下面的材质数 / shader 列表 / 是否含未映射**全部出自主源**（新鲜的）。

- **未映射** = 该原版名**不是**两张映射表的**键**（`EffectExporter.ShaderMap` ∪ `WarpforgeShaderMap.Replacements`）。
  两张表都没有 ⇒ 运行时只剩 `Shader.Find` → 原版 bundle 兜底 → 再落空就**保留占位材质**（静默）。
- **自建 `WarpforgeVFX/*` 不算原版名**，已从计数里剔除：本源里出现 346 次 / 3 个名字。
  它出现在报告里是因为**拖尾槽记账写的是替换后的名字**（`EffectExporter.cs` trail 分支 `usedShaders.Add(to)`）。

## 一、数据源实测结构（不是照文档抄）

| 事实 | 值 |
|---|---|
| 主源 | `d:/4/Unity/MyGame/Assets/WarpforgeVFX/导出报告.tsv` |
| 行数 | **959**（1 表头 + 958 效果）· 行尾 CRLF |
| 列数 | 2 格的行 **0** · 3 格的行 **958** |
| 状态列 | 全 `OK`，无 FAIL |
| 第 3 格内部（`；` 切） | `①材质定义N个` · `②原 shader: A, B` · `③近似替代 N 处`（③ 可缺） |
| 3 格齐全但抽不出数的行 | **0** |
| shader 清单用的源 | **主源（导出报告.tsv）** |

## 二、① 全部原版 shader 名 + 各自影响多少效果

⚠️ 出自 **主源（导出报告.tsv）**；`名字×N` 的 N = 958 行里有多少行的第 3 格出现该名（同一效果重复只算 1，**单位是效果行数**，
不是材质槽）。**`*` = 没进导出期表 `ShaderMap`**（§三 详列）。下面几行是**全部**原版名，共 67 个：

- Universal Render Pipeline/Particles/Unlit×947 · Everguild/FX/Extra Color×741 · Mobile/Particles/Additive×423 · Everguild/FX/Particle Distortion Affect Transparents×233 · Mobile/Particles/Alpha Blended×159
- Sprites/Mask×122 · Everguild/Matcap/Matcap Full Options×120 · Sprites/Default×117 · Everguild/Matcap/Matcap With Texture×70 · Everguild/FX/Rays For Trail×42* · Everguild/FX/Unlit UV scroll×39 · Everguild/FX/Multi Ray×33
- Everguild/FX/Alpha Mask One Layer×29 · Everguild/FX/Particle Dissolve Mask×29 · Everguild/FX/Particle Shine Custom Vertex Streams×26 · Everguild/FX/Particle Premultiply Greyscale Coloring×25
- Spine/Special/HiddenPass×23* · Legacy Shaders/Particles/Additive×19 · Particles/Standard Unlit×17 · Universal Render Pipeline/Lit×16 · Everguild/FX/Particle Premultiply×12 · Shader Graphs/Fx_ParticleDissolve_apb×11*
- Everguild/FX/ShieldVfx×10* · Everguild/FX/Alpha Masks Two Layer×9 · Everguild/FX/Spiral Trail FX×9* · Legacy Shaders/Particles/Alpha Blended Premultiply×9 · Everguild/FX/TrailShader_1×8 · Everguild/UnlitAmbient×8
- Legacy Shaders/Particles/Anim Alpha Blended×8 · UI/Additive×7 · Universal Render Pipeline/Particles/Simple Lit×6 · Everguild/FX/Glow Shader×5* · Everguild/Cards/Gem Crystal Glitter×4* · Everguild/FX/Burning×4*
- Everguild/FX/FX Shine For Animation×4* · Legacy Shaders/Particles/Alpha Blended×4 · Shader Graphs/Fx_RockDissolve×4* · Everguild/FX/Burning Dissolve×3* · Everguild/FX/Specific/Necrons Rays×3*
- Everguild/Matcap/Matcap Full Options VAT×3* · Mobile/Particles/Multiply×3 · Shader Graphs/Doomweaver effect×3 · Shader Graphs/Eclipse Tau×3* · TextMeshPro/Distance Field×3* · Everguild/Cards/3D Card Explosion×2*
- Everguild/Cards/BlobShadow×2* · Everguild/FX/MarkerLight×2* · Everguild/FX/TrailShader_Fading×2 · Everguild/Wind Matcap×2* · GlassRefraction×2* · Universal Render Pipeline/Particles/Lit×2* · Custom/EditorIcon×1*
- Everguild/Cards/3D Card×1* · Everguild/Cards/Card Swarm Effect×1* · Everguild/Cards/Gem Crystal Glitter Explosion×1* · Everguild/Cards/Necrons Base Death×1* · Everguild/Cards/Shatter Inner Pieces×1*
- Everguild/FX/Card Highlight And Shadow×1* · Everguild/FX/Card Remnant Death Icon×1* · Everguild/FX/Halo UV scroll×1* · Everguild/FX/Particle Dissolve Premultiply×1* · Everguild/FX/Specific/Pray Glow×1*
- Everguild/FX/Vortex×1* · Everguild/Sprites/Sprite Additive×1 · Everguild/UnlitAmbient Emissive Flickker×1 · TextMeshPro/Distance Field Offset×1* · Universal Render Pipeline/Unlit×1

其中 **34 个没进导出期表 `ShaderMap`** · **30 个两张表都没有**（= §三 A 组）。

## 三、② 没有进 `ShaderMap` 的 shader（**最该看的一批**）

`ShaderMap` = `EffectExporter.cs` 的导出期表（36 键）· `Replacements` = 运行时表（33 键）。

**A · 两张表都没有（真掉兜底）—— 30 个 / 命中 137 处**（一处 = 一个效果×一个 shader；**去重后是 121 条效果**，见 §四），`shader` × 效果数 —— 样例效果：

- `Everguild/FX/Rays For Trail` × 42 —— Atk_ChemVial_2 / Atk_ExileGlaiveThrow Movement / Atk_GrotGrenade …另 39 条
- `Spine/Special/HiddenPass` × 23 —— BulletImpact_Tau_FusionBlaster / BulletImpact_Tau_FusionBlaster Dual / BulletImpact_Tau_IonAccelerator …另 20 条
- `Everguild/FX/ShieldVfx` × 10 —— ArmourEffect_Bladeguard_Shield / Axe_Slash_User_SW / Hammer_Slam_SW …另 7 条
- `Everguild/FX/Spiral Trail FX` × 9 —— Buff_DA_Forest_Self / BulletImpact_Sororitas_MissileVolley / BulletImpact_Sororitas_MissileVolley_board_down …另 6 条
- `Everguild/FX/Glow Shader` × 5 —— Goff_ProperKilly / Sword_Slash_DA / Sword_Slash_DA_Heavy …另 2 条
- **影响 ≤4 条的其余 25 个**：`Everguild/Cards/Gem Crystal Glitter`×4（Cosmic Serpent） · `Everguild/FX/Burning`×4（Explosion Hive Fleet Arrival Explosions only） · `Everguild/FX/FX Shine For Animation`×4（Duty Reset） · `Everguild/FX/Burning Dissolve`×3（Explosion Hive Fleet Arrival Explosions only） · `Everguild/FX/Specific/Necrons Rays`×3（Environmental Condition Necrons Earthquake） · `Everguild/Matcap/Matcap Full Options VAT`×3（Atk_StikkaThrow Movement） · `TextMeshPro/Distance Field`×3（CardPrefab） · `Everguild/Cards/3D Card Explosion`×2（Necrons death explosion） · `Everguild/Cards/BlobShadow`×2（CardPrefab） · `Everguild/FX/MarkerLight`×2（MarkerlightExpire） · `Everguild/Wind Matcap`×2（BulletImpact_Spore Launch） · `GlassRefraction`×2（Atk_ChemVial_2） · `Universal Render Pipeline/Particles/Lit`×2（Environmental Condition GSC Sump Overspill） · `Custom/EditorIcon`×1（Toxic_Entanglement） · `Everguild/Cards/3D Card`×1（CardPrefab） · `Everguild/Cards/Card Swarm Effect`×1（Swarm Card Merge VFX） · `Everguild/Cards/Gem Crystal Glitter Explosion`×1（RemnantBody3D Aeldari） · `Everguild/Cards/Necrons Base Death`×1（RemnantBody3D Necrons） · `Everguild/Cards/Shatter Inner Pieces`×1（RemnantBody3D Necrons） · `Everguild/FX/Card Highlight And Shadow`×1（CardPrefab） · `Everguild/FX/Card Remnant Death Icon`×1（RemnantBody3D Necrons） · `Everguild/FX/Halo UV scroll`×1（CardPrefab） · `Everguild/FX/Particle Dissolve Premultiply`×1（AttackHitSmall） · `Everguild/FX/Specific/Pray Glow`×1（Pray_Idle） · `Everguild/FX/Vortex`×1（EnvironmentalCondition Saim Hann Infinity Circuit Overload）

**B · 只在运行时表、导出期表没有（两张表的同步缺口）—— 4 个**：`Shader Graphs/Fx_ParticleDissolve_apb`×11 · `Shader Graphs/Fx_RockDissolve`×4 · `Shader Graphs/Eclipse Tau`×3 · `TextMeshPro/Distance Field Offset`×1
**C · 只在导出期表、运行时表没有 —— 6 个**（都是标准 shader，运行时 `Shader.Find(原名)` 直接命中，不需要替代）：`Universal Render Pipeline/Particles/Unlit`×947 · `Sprites/Mask`×122 · `Sprites/Default`×117 · `Universal Render Pipeline/Lit`×16 · `Universal Render Pipeline/Particles/Simple Lit`×6 · `Universal Render Pipeline/Unlit`×1
**D · 在表里但标了「近似替代」（`*`，拿最接近的自建 shader 顶上，功能没做全）—— 20 个**：`Mobile/Particles/Additive`×423 · `Mobile/Particles/Alpha Blended`×159 · `Everguild/FX/Unlit UV scroll`×39 · `Everguild/FX/Multi Ray`×33 · `Everguild/FX/Alpha Mask One Layer`×29 · `Everguild/FX/Particle Dissolve Mask`×29 · `Everguild/FX/Particle Shine Custom Vertex Streams`×26 · `Everguild/FX/Particle Premultiply Greyscale Coloring`×25 · `Legacy Shaders/Particles/Additive`×19 · `Particles/Standard Unlit`×17 · `Everguild/FX/Particle Premultiply`×12 · `Everguild/FX/Alpha Masks Two Layer`×9 · `Legacy Shaders/Particles/Alpha Blended Premultiply`×9 · `Everguild/FX/TrailShader_1`×8 · `Legacy Shaders/Particles/Anim Alpha Blended`×8 · `UI/Additive`×7 · `Legacy Shaders/Particles/Alpha Blended`×4 · `Mobile/Particles/Multiply`×3 · `Shader Graphs/Doomweaver effect`×3 · `Everguild/FX/TrailShader_Fading`×2

## 四、③ 含未映射 shader 的效果清单

口径 = §三 A 组（两张表都没有）；同一效果只算 1 行 ⇒ **共 121 条效果**（主源（导出报告.tsv））。全清单：

- ArmourEffect_Bladeguard_Shield · Atk_ChemVial_2 · Atk_ExileGlaiveThrow Movement · Atk_GrotGrenade · Atk_GrotGrenade_big · Atk_GrotRock · Atk_HelspearThrow Movement · Atk_KrootJavelinThrow Movement · Atk_StikkaThrow Movement · AttackHitSmall · Axe_Slash_User_SW
- Buff_DA_Forest_Self · BulletImpact_1shot_random_EC · BulletImpact_1shot_random_chaos · BulletImpact_2shot_trail_EC · BulletImpact_2shot_trail_chaos · BulletImpact_2shot_trail_nurgle · BulletImpact_3shot_trail_EC · BulletImpact_3shot_trail_chaos
- BulletImpact_4shot_trail_EC · BulletImpact_4shot_trail_chaos · BulletImpact_4shot_trail_nurgle · BulletImpact_5shot_trail_EC · BulletImpact_5shot_trail_chaos · BulletImpact_9shot_trail_EC · BulletImpact_9shot_trail_chaos · BulletImpact_ChemVial
- BulletImpact_Pyrovore · BulletImpact_Pyrovore OLD · BulletImpact_RandomShitGo · BulletImpact_Sororitas_MissileVolley · BulletImpact_Sororitas_MissileVolley_MoveAnim · BulletImpact_Sororitas_MissileVolley_board_down
- BulletImpact_Sororitas_MissileVolley_board_up · BulletImpact_Spore Launch · BulletImpact_Spore Launch 1 · BulletImpact_Tau_FusionBlaster · BulletImpact_Tau_FusionBlaster Dual · BulletImpact_Tau_IonAccelerator · BulletImpact_Tau_Missile 10x
- BulletImpact_Tau_Missile 3x · BulletImpact_Tyranid_Barblauncher · BulletImpact_Tyranid_Big · BulletImpact_Tyranid_Cannon · BulletImpact_Tyranid_Small · BulletImpact_Tyranid_Small_Dual · BulletImpact_Tyranid_Stranglethorn
- BulletImpact_Tyranid_Stranglethorn_Dual · CardPrefab · Cosmic Serpent · Cosmic Serpent_Secondary · CreateCard DA · Duty Reset · DutyEffect · EC Sword Cut Board DMC Style · Environmental Condition Astra Militarum Planetary Invasion
- Environmental Condition Dark Angels Void Combat · Environmental Condition GSC Sump Overspill · Environmental Condition GSC Sump Overspill OLD · Environmental Condition Necrons Earthquake · Environmental Condition Necrons Immortal Beams
- Environmental Condition Necrons Solar Storm · EnvironmentalCondition Saim Hann Infinity Circuit Overload · EnvironmentalCondition Ultramarines Bombardment · Explosion Hive Fleet Arrival Explosions only · Explosion Hive Fleet Arrival Tendrils
- Explosion Hive Fleet Arrival Tendrils OLD · Goff_ProperKilly · Hammer_Slam_SW · Hammer_Throw_SW · Hexmark_Artifice_Deal1 · HuntMark_IdleEffect · Laser_Red · Lasrifle_3shots · Lasrifle_auto OLD · Lasrifle_basic OLD · Lasrifle_hellpistol · Lasrifle_laspistol OLD
- MarkerlightExpire · MarkerlightIdle · MegaBlasta · Mining_Laser · NecronGauss · NecronGauss_Damaged Hexmark · NecronGauss_Deathmark · NecronGauss_Double · NecronGauss_Hexmark · NecronGauss_Large · NecronGauss_Trilinked · NecronGauss_Twinlinked
- Necrons death explosion · NoMukkinAboutEffect · Orbital 3 repeat · Orbital 4 repeat · Orbital 5 repeat · Orbital Bombardment Enemy Warlord · Orbital Bombardment Target · Pray_Idle · RegimentEffect · RemnantBody3D Aeldari · RemnantBody3D Necrons
- RiftCannon_Laser · SW_Axe_Buff · Scarab Explosion · ShieldTraitEffect_Idle · ShieldTraitEffect_Idle Emperor's Children Variant · ShieldTraitEffect_Idle Necron Variant · ShieldTraitEffect_Idle Sororitas Variant · ShieldTraitEffect_Idle TAU Variant · StompEffect
- Swarm Card Merge VFX · Sword_Slash_DA · Sword_Slash_DA_Heavy · Sword_Slash_User_DA · Sword_Slash_User_DA No Tween · Tau_Vespid_laser · Toxic_Entanglement · Vanguard Onslaught Board · VanguardIdleEffect · WaystoneTrigger · Will Of Gork

## 五、④ 抽不出第 3 格的行有多少、为什么

**本表用的是主源：0/958 行抽不出**（958 行有）。

🔴 这个坑**要一直记着** —— `Resume=true` 的续跑会把「没重导的每一行」的第 3 格**永久抹掉**：`EffectExporter.LoadReport():229`
只取第 2 格（`Report[p[0]] = p[1]`，第 3 格直接丢），而 `SaveReport():234-240` 每 10 个效果刷一次盘、把内存那份**照原样写回**。

📌 **实测到的实例** = 本次交上来的那份快照：`_tmp_view/导出报告_快照_0917.tsv` 里 **956/958 行**抽不出第 3 格，
只有那趟真被重导的 2 个效果（`BulletImpact_artillery_big` / `Plasma_basic_blue`）留着 —— 旁证是 `资料/特效还原_进度与交接.md:224`
那句「其余 **956** 个效果」，与「956 行被截」逐位吻合。
本次主源**没有**这个症状（全量重导是 `Resume=false`：先 `File.Delete(ReportPath)`，`EffectExporter.cs:139`）。

## 六、新旧两源差异（本次主源 vs 0913 存档）

配对上的效果行 **958** · **名单+材质数+近似处数全同 920 条** · **有差 38 条**。

**shader 名单的净变化**：新增 **0** 个 · 消失 **0** 个 · 效果数变了 **0** 个。

即 **原版 shader 名单一个没加、没减、没换**（连出现次数都没变）。

⇒ 这 **38** 条有差的效果里，**35 条的 shader 列表变了，而且变的全是同一件事**：**只多出一个自建 `WarpforgeVFX/*` 条目**、**没有少任何名字**
  —— 0917「拖尾材质按渲染器逐槽记账」的直接结果（trail 分支 `usedShaders.Add(to)` 记的是**替换后的名字**）。下表前 4 条：

| 效果 | 材质数 0913→本次 | 近似 0913→本次 | shader 列表 |
|---|---|---|---|
| BulletImpact_DCannon | 14→14 | 6→6 | +1 自建名 |
| BulletImpact_DCannon_twinlink | 14→14 | 12→12 | +1 自建名 |
| BulletImpact_DeathSpinner | 11→11 | 7→7 | +1 自建名 |
| BulletImpact_DeathSpinner Test | 6→6 | 10→10 | +1 自建名 |

## 七、复跑与出处

- 复跑：`"D:/2/Warpforge_tools/py312/python.exe" -X utf8 "d:/4/Unity/工具/_effect_shader_audit.py"`
  （加 `--check` 只打汇总不写文件；带一个路径参数就换主源）
- 映射表：`MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs` · `MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs:50-109`
- 另一份体检（A 组那批 shader 落在哪个 bundle）：`资料/普查产出_0917/补充shader引用清单.md`
- 0913 完整表与解析器：`资料/普查产出_0913/效果_shader_对账.{tsv,md}` · `工具/audit_effect_shaders.py`
