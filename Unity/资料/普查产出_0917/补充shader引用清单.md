# 补充 shader 引用清单（33 个）

> 2026-09-17 落盘 · **纯 Python 只读体检**：未启动 Unity、未写 `Assets/` 与 `StreamingAssets/` 下任何文件、
> 未跑 `extract_missing_shaders.py` 的非 `--check` 分支（那条路会重打 `wf_shaders_extra.bundle`）。

## 一句话

**33 个**（09-13 口径）真数不变；其中 **5 个**在这一周里已被自建替代接管 ⇒ 今天**真正还在掉兜底的只剩 28 个**。

## 读法说明（跑了哪条命令）

| 跑的命令 | 得到什么 | 注意 |
|---|---|---|
| `PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/_missing_shaders_audit.py"` | 本表**每一列**（`工具/_missing_shaders_audit.py`，只读） | 本次产出就是它 |
| `…/python.exe "d:/4/Unity/工具/extract_missing_shaders.py" --check` | 只打 [1]–[5] 段**汇总**、不落盘 | ⚠️ **今天它复现不出「33」**（打出候选 1 个），原因见末尾「三件与记录不符的事」第 2 条 |

列的含义：

- **影响效果数** —— 958 条效果里有**几行的第 3 格**出现过该名（同一效果重复出现只算 1）。
  数据源 = `资料/普查产出_0913/效果_shader_对账.tsv`（09-13 从当时**三格格式**的 `导出报告.tsv` 解析出来的存档）。
  ⚠️ **单位是「效果行数」**，不是材质槽；`资料/特效还原_进度与交接.md:737` 的「370 处引用」是**另一个口径**，两个数别混用。
- **在 wf_shaders_extra.bundle / 主包有没有** —— **实读 bundle**（UnityPy 读 `Shader.m_ParsedForm.m_Name` + `AssetBundle.m_Container`），
  不是抄记录。主包 = `wf_shaders.bundle`（45 个，`shaders_assets_all.bundle` 的副本）；补充包 = `wf_shaders_extra.bundle`（42 个，与主包重叠 1）。
- **我们现在的处置** —— 从**当前**代码正则抓的两张映射表：`WarpforgeShaderMap.Replacements`（运行时，33 条）
  与 `EffectExporter.ShaderMap`（导出期，32 条）。三者之外的解析链 = `Shader.Find` → 两个 bundle → 落空则**保留占位材质**。

## 清单（按影响效果数降序）

| # | shader | 影响效果数（效果名样例 ≤3） | 在 wf_shaders_extra.bundle | 主包有没有 | 我们现在的处置 |
|---|---|---|---|---|---|
| 1 | `Everguild/FX/Rays For Trail` | 42（Atk_ChemVial_2 / Atk_ExileGlaiveThrow Movement / Atk_GrotGrenade） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 2 | `Spine/Special/HiddenPass` | 23（BulletImpact_Tau_FusionBlaster / …FusionBlaster Dual / …IonAccelerator） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 3 | `Everguild/FX/Particle Premultiply` | 12（Atk_Rokkit-Harpoon big / Atk_Rokkit-Harpoon small / BulletImpact_Ork Heavy Lobba） | **有** | 无 | ✅ 有替代 shader：`WarpforgeVFX/Particles/Extra Color`（近似，2026-09-15） |
| 4 | `Shader Graphs/Fx_ParticleDissolve_apb` | 11（Air to Ground Big / Artillery Ground / BlastEffect） | 有 | 无 | ✅ 有替代（同上，2026-09-15） |
| 5 | `Everguild/FX/ShieldVfx` | 10（ArmourEffect_Bladeguard_Shield / Axe_Slash_User_SW / Hammer_Slam_SW） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 6 | `Everguild/FX/Spiral Trail FX` | 9（Buff_DA_Forest_Self / BulletImpact_Sororitas_MissileVolley / …_board_down） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 7 | `Everguild/FX/Glow Shader` | 5（Goff_ProperKilly / Sword_Slash_DA / Sword_Slash_DA_Heavy） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 8 | `Everguild/Cards/Gem Crystal Glitter` | 4（Cosmic Serpent / Cosmic Serpent_Secondary / RemnantBody3D Aeldari） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 9 | `Everguild/FX/Burning` | 4（Explosion Hive Fleet Arrival Explosions only / …Tendrils / …Tendrils OLD） | 无 | **有** | 未映射 → 掉原版 bundle 兜底 |
| 10 | `Everguild/FX/FX Shine For Animation` | 4（Duty Reset / DutyEffect / RegimentEffect） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 11 | `Shader Graphs/Fx_RockDissolve` | 4（BlastEffect / Invoke Minion Hits Ground / …Legendary） | 有 | 无 | ✅ 有替代（2026-09-15） |
| 12 | `Everguild/FX/Burning Dissolve` | 3（Explosion Hive Fleet Arrival Explosions only / …Tendrils OLD / Will Of Gork） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 13 | `Everguild/FX/Specific/Necrons Rays` | 3（Environmental Condition Necrons Earthquake / …Immortal Beams / …Solar Storm） | 无 | **有** | 未映射 → 掉原版 bundle 兜底 |
| 14 | `Everguild/Matcap/Matcap Full Options VAT` | 3（Atk_StikkaThrow Movement / BulletImpact_RandomShitGo / VanguardIdleEffect） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 15 | `Shader Graphs/Eclipse Tau` | 3（EnvironmentalCondition Tau Electro-Static Interference / …Radiation Storm / …Solar Eclipse） | 无 | **有** | ✅ 有替代（2026-09-15） |
| 16 | `Everguild/Cards/3D Card Explosion` | 2（Necrons death explosion / RemnantBody3D Aeldari） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 17 | `Everguild/Cards/BlobShadow` | 2（CardPrefab / Swarm Card Merge VFX） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 18 | `Everguild/FX/MarkerLight` | 2（MarkerlightExpire / MarkerlightIdle） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 19 | `Everguild/Wind Matcap` | 2（BulletImpact_Spore Launch / BulletImpact_Spore Launch 1） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 20 | `GlassRefraction` | 2（Atk_ChemVial_2 / BulletImpact_ChemVial） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 21 | `Custom/EditorIcon` | 1（Toxic_Entanglement） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 22 | `Everguild/Cards/3D Card` | 1（CardPrefab） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 23 | `Everguild/Cards/Card Swarm Effect` | 1（Swarm Card Merge VFX） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 24 | `Everguild/Cards/Gem Crystal Glitter Explosion` | 1（RemnantBody3D Aeldari） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 25 | `Everguild/Cards/Necrons Base Death` | 1（RemnantBody3D Necrons） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 26 | `Everguild/Cards/Shatter Inner Pieces` | 1（RemnantBody3D Necrons） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 27 | `Everguild/FX/Card Highlight And Shadow` | 1（CardPrefab） | 无 | **有** | 未映射 → 掉原版 bundle 兜底 |
| 28 | `Everguild/FX/Card Remnant Death Icon` | 1（RemnantBody3D Necrons） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 29 | `Everguild/FX/Halo UV scroll` | 1（CardPrefab） | 无 | **有** | 未映射 → 掉原版 bundle 兜底 |
| 30 | `Everguild/FX/Particle Dissolve Premultiply` | 1（AttackHitSmall） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 31 | `Everguild/FX/Specific/Pray Glow` | 1（Pray_Idle） | 有 | 无 | 未映射 → 掉原版 bundle 兜底 |
| 32 | `Everguild/FX/Vortex` | 1（EnvironmentalCondition Saim Hann Infinity Circuit Overload） | 无 | **有** | 未映射 → 掉原版 bundle 兜底 |
| 33 | `TextMeshPro/Distance Field Offset` | 1（CardPrefab） | 无 | 无 | ✅ 有替代：`TextMeshPro/Distance Field`（2026-09-16） |

## 总数统计

| 指标 | 值 |
|---|---|
| 该档 shader 数 | **33**（今天仍未被映射 **28** · 已被自建替代接管 **5**） |
| 影响效果行合计 | **163** 处（其中仍未被映射的 **132** 处） |
| 落在 `wf_shaders_extra.bundle` | 26 |
| 落在主包 `wf_shaders.bundle` | 6 |
| **两个包都没有** | **1**（`TextMeshPro/Distance Field Offset`，唯一的**真洞**：连兜底都没有，只剩占位材质） |
| 与 09-13 记录的分类标签**逐条对账** | 33/33 全对（26 补充包 / 6 主包 / 1 未解析，与实读包一致） |

⚠️ 「掉原版 bundle 兜底」的 28 个 = **能渲染，但不安全**：按 `WarpforgeShaderMap.cs:150-158` 的实测，
bundle 里的 shader **在编辑器下渲染会出故障**；且它们是**原版编译字节码，不能进发布版本**。

## ⚠️ 三件与记录不符的事

1. 🔴 **`WarpforgeShaderMap.cs:50-56` 那段注释是错的 —— `Everguild/FX/Particle Premultiply` 就在补充包里。**
   实读：本机那份 `wf_shaders_extra.bundle` 的 `AssetBundle.m_Container` **42 条里同时有**
   `Everguild/FX/Particle Premultiply` 与 `Everguild/FX/Particle Dissolve Premultiply`，
   且是两个不同的 Shader 对象（pathid `1092032425491312587` / `-7164788934849017719`）。
   **错因（已复现）**：那段注释说是「**grep 过**两个包」得出的 —— 而**裸字节 grep 对这个包不可靠**：
   实测该包 42 条容器名里有 **36 条**在裸字节里搜不到（bundle 数据块是 LZ4 压缩的）。
   ⇒ 这条与铁律 2 的「按猜的字段名搜出 0 命中 ≠ 资源不存在」是**同一类错**。
   **连带影响**：注释里据此推出的「`Shader.Find` 返回 null ⇒ 保留占位材质 ⇒ `Explosion_Ground` 整块渲不出来」
   **根因需要重查** —— 补充包能提供这个 shader，更像踩的是**另一条已记录的坑**：
   「导出那趟一个源包都不能加载，否则 `wf_shaders_extra.bundle` 被顶掉」（`资料/特效还原_进度与交接.md:941`）。
   **本次没有改它**（边界：不动 `Assets/`），请下一个会话改掉那 7 行注释。
2. **`extract_missing_shaders.py --check` 今天复现不出「33」**（打出候选 **1** 个）。
   原因：它读的 `Assets/WarpforgeVFX/导出报告.tsv` **今天 21:44 被正在跑的全量重导改成了两格格式**
   （958 行是 `效果名 ⇥ OK`，只有 2 行带第 3 格 `原 shader:`）—— 而它按 `p[2]` 取 shader，于是几乎全空。
   ⇒ 本表因此改用**存档** `资料/普查产出_0913/效果_shader_对账.tsv`（同样是 958 行、三格格式的原始解析）。
   **下一个会话注意**：全量重导跑完之后，`--check` 这条路要么改解析口径、要么明确它已不适用于新格式。
3. **33 里 5 个已被接管**（今天才成立的事实，09-13 的两份文档都还没有）：
   `Everguild/FX/Particle Premultiply`(12) · `Shader Graphs/Fx_ParticleDissolve_apb`(11) ·
   `Shader Graphs/Fx_RockDissolve`(4) · `Shader Graphs/Eclipse Tau`(3) · `TextMeshPro/Distance Field Offset`(1)。
   ⚠️ 前 4 个只在**运行时**表里（`WarpforgeShaderMap.Replacements`），**导出期表 `EffectExporter.ShaderMap` 里没有**
   ⇒ 导出报告不会把它们标成「近似替代」，而两张表按注释**要求同步**。这是**该同步的一处缺口**。

## 出处

- 脚本：`工具/_missing_shaders_audit.py`（只读）· 原体检脚本：`工具/extract_missing_shaders.py:116-131`
- 效果×shader 存档：`资料/普查产出_0913/效果_shader_对账.tsv` · 分类与 33 的原始出处：同目录 `效果_shader_对账.md:192-226`
- 映射表：`MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs` · `MyGame/Assets/WarpforgeArena1/Editor/EffectExporter.cs:50-109`
- bundle 实读：`MyGame/Assets/StreamingAssets/WarpforgeVFX/{wf_shaders,wf_shaders_extra}.bundle`
