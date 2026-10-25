# AnimationClip 24 条对账（R0「没用上」里唯一被标为真候选的那一批）

> **归属**：战场线（本文件**只对账，不实现**）。
> 判据：① 原版侧 = `assets_full` 的 clip typetree（时长 = 曲线最大 time）+ R0 引用图（谁挂在它上面）；
> ② 我们侧 = `MyGame/Assets/WarpforgeVFX/Animations/<同名>.anim` 是否存在 + 该 `.anim.meta` 的 **GUID 被哪些预制体引用**。
> ⛔ 没有一处拿「名字像我认识」当判据。

## §0 结论（一句话）

**24 / 24 全部「已覆盖」** —— 每一条都同时满足：
1. **数据在**：我们工程里有**逐字同名**的 `.anim`（`WarpforgeVFX/Animations/`），
2. **曲线在**：原版侧曲线数 2–6,019、时长 0.483–100.2 s（**全是 legacy clip，不是 muscle 空壳**），
3. **挂得上**：该 `.anim` 的 GUID **被我们自己的效果预制体引用**（每条的预制体名见下表）。

⇒ **这条「真候选」结案：0 条真缺。**

⚠️ **边界**：本表只证明「动画数据在、且挂在对应预制体上」。**预制体有没有被接进玩法**（哪个效果、哪张卡触发）
是特效线的事，不在本表范围。

## §1 逐条（24 条）

| # | clip 名 | 时长 | 曲线数 | 原版挂在哪个节点（Animation 组件所属 GameObject） | 我们这边：引用它的预制体 | 判定 |
|---|---|---|---|---|---|---|
| 1 | `Card Hand To Board` | 0.917 s | 40 | `CardPrefab`、`CardPrefab` | `CardPrefab.prefab` | **已覆盖** |
| 2 | `Choppa Animation` | 1.0 s | 4 | `Choppa`、`Choppa` | `NoMukkinAboutEffect.prefab` | **已覆盖** |
| 3 | `Emperor's Children Environmental Condition 1` | 4.0 s | 6 | `Rapturous Ruination Board`、`Rapturous Ruination Board`、`Environmental Condition Emperor's Children 1 Green`、`Environmental Condition Emperor's Children 1 Green`、`Environmental Condition Emperor's Children 1 Pink REJECTED`、`Environmental Condition Emperor's Children 1 Pink REJECTED`、`Environmental Condition Emperor's Children 2 Fumes`、`Environmental Condition Emperor's Children 2 Fumes` | `Environmental Condition Emperor's Children 1 Green.prefab`、`Environmental Condition Emperor's Children 1 Pink REJECTED.prefab`、`Environmental Condition Emperor's Children 2 Fumes.prefab`、`Rapturous Ruination Board.prefab` | **已覆盖** |
| 4 | `Firestorm Tornado` | 12.5 s | 4 | `Fire Tornado`、`Fire Tornado` | `EnvironmentalCondition Tau Radiation Storm.prefab` | **已覆盖** |
| 5 | `Fleet Support Barges` | 100.2 s | 6019 | `Barge Background`、`Barge Background` | `EnvironmentalCondition Ultramarines Aerial Clash.prefab` | **已覆盖** |
| 6 | `From Remnant Necrons` | 1.983 s | 79 | `RemnantBody3D Necrons` | `RemnantBody3D Necrons.prefab` | **已覆盖** |
| 7 | `Hive Fleet Arrival PP` | 6.0 s | 38 | `Explosion Hive Fleet Arrival Explosions only`、`Explosion Hive Fleet Arrival Explosions only`、`Explosion Hive Fleet Arrival Tendrils`、`Explosion Hive Fleet Arrival Tendrils`、`Explosion Hive Fleet Arrival Tendrils OLD`、`Explosion Hive Fleet Arrival Tendrils OLD` | `Explosion Hive Fleet Arrival Explosions only.prefab`、`Explosion Hive Fleet Arrival Tendrils OLD.prefab`、`Explosion Hive Fleet Arrival Tendrils.prefab` | **已覆盖** |
| 8 | `InBattleDamageCounter Variation 1` | 1.833 s | 23 | `DamageCounter`、`DamageCounter` | `CardPrefab.prefab` | **已覆盖** |
| 9 | `Planetary Invasion Animation` | 4.0 s | 4 | `Environmental Condition Astra Militarum Planetary Invasion`、`Environmental Condition Astra Militarum Planetary Invasion` | `Environmental Condition Astra Militarum Planetary Invasion.prefab` | **已覆盖** |
| 10 | `Pray Enter` | 0.667 s | 2 | `Pray VFX mesh`、`Pray VFX mesh` | `Pray_Idle.prefab` | **已覆盖** |
| 11 | `Spirit Stone Idle` | 8.0 s | 487 | `Idle`、`Idle` | `RemnantBody3D Aeldari.prefab` | **已覆盖** |
| 12 | `Stormsurge Cannon` | 1.717 s | 8 | `BulletImpact_Tau_PulseARCcannon`、`BulletImpact_Tau_PulseARCcannon`、`BulletImpact_RiftCannon`、`BulletImpact_RiftCannon` | `BulletImpact_RiftCannon.prefab`、`BulletImpact_Tau_PulseARCcannon.prefab` | **已覆盖** |
| 13 | `Swarm Triat Merge` | 1.2 s | 25 | `Swarm Card Merge VFX`、`Swarm Card Merge VFX` | `Swarm Card Merge VFX.prefab` | **已覆盖** |
| 14 | `SwarmIconOn` | 0.483 s | 7 | `Swarm Battle Card Icon`、`Swarm Battle Card Icon` | `Swarm Battle Card Icon.prefab` | **已覆盖** |
| 15 | `SweepBackAndForth` | 2.0 s | 4 | `RotationHolder`、`RotationHolder`、`RotationHolder`、`RotationHolder` | `FlamethrowerSweepAttack_1.prefab`、`FlamethrowerSweepAttack_sororitas.prefab` | **已覆盖** |
| 16 | `SweepBackAndForth_Adjacents` | 2.0 s | 3 | `GameObject`、`GameObject` | `EC Heldrake Attack.prefab` | **已覆盖** |
| 17 | `SweepBackAndForth_loop` | 4.0 s | 3 | `RotationHolder`、`RotationHolder` | `AcidSpraySweepAttack.prefab` | **已覆盖** |
| 18 | `SweepLeftToRight` | 2.0 s | 2 | `RotationHolder`、`RotationHolder`、`RotationHolder`、`RotationHolder`、`RotationHolder`、`RotationHolder` | `BolterSweep.prefab`、`BolterSweep_2.prefab`、`GatlingSweep.prefab` | **已覆盖** |
| 19 | `Thunderstorm Flash` | 0.517 s | 12 | `Lightning Main Start`、`Lightning Main Start`、`Lightning Main Start`、`Lightning Main Start` | `EnvironmentalCondition Sororitas Raging Storm.prefab`、`EnvironmentalCondition Ultramarines Thunderstorm.prefab` | **已覆盖** |
| 20 | `To Remnant Aeldari` | 4.517 s | 22 | `RemnantBody3D Aeldari`、`RemnantBody3D Aeldari` | `RemnantBody3D Aeldari.prefab` | **已覆盖** |
| 21 | `To Remnant Necrons` | 3.983 s | 59 | `RemnantBody3D Necrons`、`RemnantBody3D Necrons` | `RemnantBody3D Necrons.prefab` | **已覆盖** |
| 22 | `Valkyire Drop Movement` | 5.0 s | 308 | `MovementParent`、`MovementParent` | `Valkyrie_MultiLaser_Hover.prefab` | **已覆盖** |
| 23 | `Vanguard Frame Animation` | 1.792 s | 21 | `VanguardIdleEffect`、`VanguardIdleEffect` | `VanguardIdleEffect.prefab` | **已覆盖** |
| 24 | `Will of Grok` | 3.7 s | 17 | `Will Of Gork`、`Will Of Gork` | `Will Of Gork.prefab` | **已覆盖** |

## §2 判据细节

| 检查项 | 怎么做 | 结果 |
|---|---|---|
| 空壳风险 | `m_Legacy` / `m_MuscleClipSize` / 曲线关键帧数（`ttread` 与 UnityPy 两条路都读过） | 24/24 都是 **legacy**、关键帧 2–6,019 |
| 时长 | 曲线里最大的 `time`（**不是** `m_MuscleClip.m_StopTime` —— legacy clip 那里恒 0，照它读会全是 0） | 0.483 – 100.2 s |
| 原版挂点 | R0 图的**父对象** = `Animation`（legacy 组件）→ 反解它的 `m_GameObject.m_Name` | 见 §1 第 5 列 |
| 我们有没有 | `.anim.meta` 的 GUID 在 `Assets/**/*.{prefab,asset,unity,controller,mat}` 里搜 | **24/24 命中**，均在 `WarpforgeVFX/Prefabs/` |
| 名字对不上怎么办 | 用 **GUID 引用**判，不用名字 —— 例 `Choppa Animation` 在我们这边挂在 `NoMukkinAboutEffect.prefab`（名字不同、clip 同一条） | —— |

## §3 这批为什么会被 R0 判成「没用上」（口径已修）

`R0` 的「已用（工程内）」是**按文件名**判的，而扫描扩展名表里**漏了 `.anim`** ——
我们的 `.anim` 偏偏**逐字沿用原版 clip 名**（`Spirit Stone Idle.anim`），于是 24 条全被判「没看过」。
补上 `.anim`（以及 `.controller/.overrideController/.mask/.playable/.mixer`）后，
**这 24 条全部自动转为「已用（工程内）」**，七类候选 **279 → 255**。

> 这已经是**同一类 bug 的第三次**（前两次：`数据/索引` 当集合差、漏 `.ogg/.wav`）。
> 结论：**「按文件名判用没用」必须先把扩展名表覆盖全**，否则数字会虚高、让人去复刻已经有的东西。

