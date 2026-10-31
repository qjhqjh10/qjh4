# B109_E19 · R0-09「真加载」**全量**探针（L0/L1/L2/L3/U）

> 生成时间：2026-10-31 18:19:34（`--check` 忽略本行）
> 判据 = `判据/B107_补判据_新账15条.md` **N56**（`判据/B109_R9_资源侧剩余.md` §4 复述）：L1 = 非 Editor 运行时代码以资产名为实参调加载 API；L2 = 出现在**被这样的代码读取**的表里且表值就是加载实参同一串；L3 = 仅名字在表（不算）；U 单列；**L0 = 运行时探针（最强）**。
> 输入 = `资源/R0_资源覆盖账.tsv` 的 `我们有没有用 == 已用（数据表）` 段，**现读 16837 行（全量，不抽样）**。
> 判法实现 = `工具/gen_u2_loaded_probe.py`（本文件 **import** 它，不抄第二份）；本文件只换成全量 + 大串预筛。
> 复跑 = `python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --write` / `--check`。⛔ 本脚本不改主账。

## §0 统计

| 等级 | 条数 | 占本段 | 含义 |
|---|---:|---:|---|
| **L0** | 0 | 0.00% | 运行时真的以该名字加载过（最强证据） |
| **L1** | 0 | 0.00% | 非 Editor 运行时代码以该名字为实参调加载 API |
| **L2** | 73 | 0.43% | 名字是被这样代码读取的表里的值（表值 = 加载实参同一串） |
| **L3** | 15829 | 94.01% | 仅名字在表（不算真加载） |
| **U** | 935 | 5.55% | 动态实参 / 静态解不出（单列） |
| 合计 | 16837 | 100% | — |

**真加载率（上限）** = (L0+L1+L2) / 总数 = **0.43%**；若只认静态证据（不含 L0）= (L1+L2) / 总数 = **0.43%**。⇒ 这个段（「已用（数据表）」）里 **94.01% 只是「名字在某张表里」**。

**与 `B107_R0_U2真加载核查.md`（200 条分层抽样）对照**

| 等级 | B107 抽样 | 本报告（全量） |
|---|---:|---:|
| L1 | 0 | 0 |
| L2 | 8 | 73 |
| L3 | 185 | 15829 |
| U | 7 | 935 |

> 抽样与全量若差得多，说明「按类型等距抽」放大了小类（`AnimationClip`/`AnimatorController` 抽样里占 14 条，全量只有 41 条）——**以本报告的逐条结果为准**。

**按类型（行 = 类型，列 = 等级）**

| 类型 | 账内行数 | L0 | L1 | L2 | L3 | U | L3 占比 |
|---|---:|---:|---:|---:|---:|---:|---:|
| `GameObject` | 12939 | 0 | 0 | 48 | 11961 | 930 | 92.4% |
| `Texture2D` | 1407 | 0 | 0 | 0 | 1406 | 1 | 99.9% |
| `Sprite` | 1398 | 0 | 0 | 0 | 1398 | 0 | 100.0% |
| `MonoBehaviour` | 598 | 0 | 0 | 0 | 598 | 0 | 100.0% |
| `Material` | 263 | 0 | 0 | 0 | 263 | 0 | 100.0% |
| `MonoScript` | 185 | 0 | 0 | 0 | 181 | 4 | 97.8% |
| `AnimationClip` | 37 | 0 | 0 | 21 | 16 | 0 | 43.2% |
| `AnimatorController` | 4 | 0 | 0 | 4 | 0 | 0 | 0.0% |
| `AssetBundle` | 2 | 0 | 0 | 0 | 2 | 0 | 100.0% |
| `Mesh` | 2 | 0 | 0 | 0 | 2 | 0 | 100.0% |
| `SpriteAtlas` | 2 | 0 | 0 | 0 | 2 | 0 | 100.0% |

## §1 L0 运行时探针

**本轮读数：未采集（0 条）** —— 仓里还没有 L0 日志。

- 为什么必须有它：**L1 在这一段结构性恒 0**（`工具/r0_report.py:550-554` 的 U1 会先把「名字在 `Assets/**/*.cs` 正文出现过」的行吃掉 ⇒ 能当加载实参的字面量早已是 U1、落不进本段）；而 L2 是**字段名级**间接证据（`Load(x.clip)`）。真要说「真的加载了」，只有运行时探针说得清。
- 怎么采（**需要跑一次宿主，只有调度台能做**）：
  1. 新建运行时静态类 `Assets/CardPresentation/Core/LoadProbe.cs`（代码见 §5，粘贴即可）：
     `Begin(path)` 打开记录、`Record(name)` 记一条、`End()` 写 TSV。
  2. 在**包装口**里插 `LoadProbe.Record(<形参>)` 一行 —— 清单 = `--wrappers` 现读 **84** 个
     （定义处 `文件:行` + 形参名，全表用 `--wrappers` 打；它们就是「以形参直调加载 API」的方法，动态名字**必然**经过其中之一）。
  3. 在自检宿主的 `Run()` 首尾各一行：`LoadProbe.Begin("d:/4/_tmp_view/b109_e19/l0_loads_<宿主>.tsv")` / `LoadProbe.End()`。
  4. 跑 `BattleScene.Run` / `ShellScene.Run` / `CollectionScene.Run`（P0 主场），然后：
     `python -I -X utf8 d:/4/Unity/工具/probe_true_load.py --write --l0 <日志>` ⇒ L0 计数直接进 §0。
  ⚠️ **多宿主要合并**：不同宿主各写一个文件，用 `cat a.tsv b.tsv > l0_loads.tsv` 合并（TSV 三列、无表头）。

## §2 L1（静态最强）：0 条

**0 条 —— 这是结构性的，不是探针坏了**（见 §1 第一条）。

**阳性对照**（手挑、不是本段行）：`arena1_bg` · `40k_battle_energy_full` · `card_vfx_by_card` · `EnvBlendables` —— 探针在 `B107_R0_U2真加载核查.md` §0 里对它们判出 L1，说明「认得出加载实参」这条能力是有的。

## §3 L2（表驱动真加载）：73 条

| # | 名字 | 类型 | 表 → 读者 | 说明 |
|---:|---|---|---|---|
| 1 | `BigExplosion` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29568 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[0]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 2 | `BigExplosion` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29568 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[0]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 3 | `Fire Small` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29624 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[4]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 4 | `Fire Small` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29624 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[4]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 5 | `Fire Small` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29624 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[4]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 6 | `Launch Smoke` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29638 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[5]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 7 | `Fire Small` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29624 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[4]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 8 | `Fire Small` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29624 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[4]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 9 | `BigExplosion` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29568 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[0]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 10 | `Fire Small` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29624 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[4]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 11 | `BattleHudShow` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/battlehud_clips.json:843 + CardPresentation/Battle/BattleHudAnim.cs:507` | 表值（`clips/[2]/name`）→ 读者 `BattleHudAnim.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 12 | `BattleHudHidden` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/battlehud_clips.json:9 + CardPresentation/Battle/BattleHudAnim.cs:507` | 表值（`clips/[0]/name`）→ 读者 `BattleHudAnim.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 13 | `BattleHudShowInstantNoHand` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/battlehud_clips.json:2801 + CardPresentation/Battle/BattleHudAnim.cs:507` | 表值（`clips/[7]/name`）→ 读者 `BattleHudAnim.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 14 | `BattleHudHideNoHand` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/battlehud_clips.json:1933 + CardPresentation/Battle/BattleHudAnim.cs:507` | 表值（`clips/[5]/name`）→ 读者 `BattleHudAnim.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 15 | `BattleHudHide` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/battlehud_clips.json:345 + CardPresentation/Battle/BattleHudAnim.cs:507` | 表值（`clips/[1]/name`）→ 读者 `BattleHudAnim.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 16 | `BattleHudShowNoHand` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/battlehud_clips.json:2367 + CardPresentation/Battle/BattleHudAnim.cs:507` | 表值（`clips/[6]/name`）→ 读者 `BattleHudAnim.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 17 | `BattleHudShowInstant` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/battlehud_clips.json:1353 + CardPresentation/Battle/BattleHudAnim.cs:507` | 表值（`clips/[3]/name`）→ 读者 `BattleHudAnim.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 18 | `BattleHudHiddenNoHand` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/battlehud_clips.json:1643 + CardPresentation/Battle/BattleHudAnim.cs:507` | 表值（`clips/[4]/name`）→ 读者 `BattleHudAnim.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 19 | `Wave Shine` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:3497 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[12]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 20 | `Wave Shine R` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:3850 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[13]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 21 | `Sparks background` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:354 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[2]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 22 | `Trails R` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:4203 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[14]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 23 | `Promote Letter Effect` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:1397 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[5]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 24 | `Glow Big` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:1060 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[4]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 25 | `Promote Letter Effect` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:1397 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[5]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 26 | `Sparks Burn` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:707 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[3]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 27 | `Glow below` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:21 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[1]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 28 | `Wave Shine` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:3497 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[12]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 29 | `Wave Shine` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:3497 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[12]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 30 | `Glow below` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:21 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[1]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 31 | `Wave Shine` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:3497 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[12]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 32 | `Wave Shine` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:3497 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[12]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 33 | `Glow Big` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:1060 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[4]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 34 | `Trails L` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:4540 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[15]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 35 | `Sparks Burn` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:707 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[3]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 36 | `Battle Arena 1 Camera Intro` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json:8 + CardPresentation/Battle/CameraIntro.cs:238` | 表值（`clips/[0]/name`）→ 读者 `CameraIntro.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 37 | `Cannon 1` | `AnimatorController` | `MyGame/Assets/CardPresentation/Resources/arena_scene_anims.json:11 + CardPresentation/Battle/ArenaSceneAnims.cs:154` | 表值（`arenas/[0]/animators/[0]/controller`）→ 读者 `ArenaSceneAnims.cs` 动态加载口，字段 `controller` 出现在加载实参里 |
| 38 | `Cannon 1` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/arena_scene_anims.json:11 + CardPresentation/Battle/ArenaSceneAnims.cs:154` | 表值（`arenas/[0]/animators/[0]/controller`）→ 读者 `ArenaSceneAnims.cs` 动态加载口，字段 `controller` 出现在加载实参里 |
| 39 | `Battle Arena 2 Camera Intro` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json:136 + CardPresentation/Battle/CameraIntro.cs:238` | 表值（`clips/[1]/name`）→ 读者 `CameraIntro.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 40 | `Dynamic Skulls` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/arena_scene_anims.json:41 + CardPresentation/Battle/ArenaSceneAnims.cs:154` | 表值（`arenas/[1]/animators/[2]/controller`）→ 读者 `ArenaSceneAnims.cs` 动态加载口，字段 `controller` 出现在加载实参里 |
| 41 | `Pump 1` | `AnimatorController` | `MyGame/Assets/CardPresentation/Resources/arena_scene_anims.json:29 + CardPresentation/Battle/ArenaSceneAnims.cs:154` | 表值（`arenas/[1]/animators/[0]/controller`）→ 读者 `ArenaSceneAnims.cs` 动态加载口，字段 `controller` 出现在加载实参里 |
| 42 | `Dynamic Skulls` | `AnimatorController` | `MyGame/Assets/CardPresentation/Resources/arena_scene_anims.json:41 + CardPresentation/Battle/ArenaSceneAnims.cs:154` | 表值（`arenas/[1]/animators/[2]/controller`）→ 读者 `ArenaSceneAnims.cs` 动态加载口，字段 `controller` 出现在加载实参里 |
| 43 | `Battle Arena 2` | `AnimatorController` | `MyGame/Assets/CardPresentation/Resources/arena_scene_anims.json:35 + CardPresentation/Battle/ArenaSceneAnims.cs:154` | 表值（`arenas/[1]/animators/[1]/controller`）→ 读者 `ArenaSceneAnims.cs` 动态加载口，字段 `controller` 出现在加载实参里 |
| 44 | `BigExplosion` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29568 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[0]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 45 | `Battle Arena 2` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/arena_scene_anims.json:35 + CardPresentation/Battle/ArenaSceneAnims.cs:154` | 表值（`arenas/[1]/animators/[1]/controller`）→ 读者 `ArenaSceneAnims.cs` 动态加载口，字段 `controller` 出现在加载实参里 |
| 46 | `Launch Smoke` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29638 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[5]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 47 | `Fire Small` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29624 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[4]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 48 | `Battle Arena 3 Camera Intro` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json:264 + CardPresentation/Battle/CameraIntro.cs:238` | 表值（`clips/[2]/name`）→ 读者 `CameraIntro.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 49 | `Battle Arena Aeldari Camera Intro` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json:354 + CardPresentation/Battle/CameraIntro.cs:238` | 表值（`clips/[3]/name`）→ 读者 `CameraIntro.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 50 | `Battle Arena Astra Militarum Camera Intro` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json:500 + CardPresentation/Battle/CameraIntro.cs:238` | 表值（`clips/[4]/name`）→ 读者 `CameraIntro.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 51 | `Chimera` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/ability_anim_durations.json:64 + CardPresentation/Core/AbilityAnimDurations.cs:184` | 表值（`items/[9]/name`）→ 读者 `AbilityAnimDurations.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 52 | `Battle Arena Black Legion Camera Intro` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json:628 + CardPresentation/Battle/CameraIntro.cs:238` | 表值（`clips/[5]/name`）→ 读者 `CameraIntro.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 53 | `Battle Arena Dark Angels Camera Intro` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json:718 + CardPresentation/Battle/CameraIntro.cs:238` | 表值（`clips/[6]/name`）→ 读者 `CameraIntro.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 54 | `Battle Arena Emperors Children Camera Intro` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json:846 + CardPresentation/Battle/CameraIntro.cs:238` | 表值（`clips/[7]/name`）→ 读者 `CameraIntro.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 55 | `Battle Arena Genestaler Cults Camera Intro` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json:4071 + CardPresentation/Battle/CameraIntro.cs:238` | 表值（`clips/[8]/name`）→ 读者 `CameraIntro.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 56 | `Fire Small` | `GameObject` | `MyGame/Assets/Resources/EnvBlendables.json:29624 + CardPresentation/Battle/EnvironmentApplier.cs:159` | 表值（`sceneStandaloneBuild/[0]/reparent/[4]/name`）→ 读者 `EnvironmentApplier.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 57 | `Battle Arena Sororitas Camera Intro` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json:7460 + CardPresentation/Battle/CameraIntro.cs:238` | 表值（`clips/[10]/name`）→ 读者 `CameraIntro.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 58 | `Battle Arena Emperors Children Camera Intro` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json:846 + CardPresentation/Battle/CameraIntro.cs:238` | 表值（`clips/[7]/name`）→ 读者 `CameraIntro.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 59 | `Battle Arena Tau Virola Camera Intro` | `AnimationClip` | `MyGame/Assets/CardPresentation/Resources/camera_intro_clips.json:13910 + CardPresentation/Battle/CameraIntro.cs:238` | 表值（`clips/[12]/name`）→ 读者 `CameraIntro.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 60 | `Side Border Left` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:2076 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`images/[0]/name`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 61 | `Borders` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:2077 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`images/[0]/parent`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `parent` 出现在加载实参里 |
| 62 | `Side Border Right` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:2107 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`images/[1]/name`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 63 | `Glow Explosion Up` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:865 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`particles/[4]/name`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 64 | `Glow Big` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/ranked_division_particles.json:1060 + CardPresentation/Shell/RankedDivisionParticles.cs:131` | 表值（`nodes/[4]/name`）→ 读者 `RankedDivisionParticles.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 65 | `Glow Explosion R` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:1063 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`particles/[5]/name`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 66 | `Ray Right` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:1459 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`particles/[7]/name`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 67 | `Level Up Effect` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:2138 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`images/[2]/name`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 68 | `Lighting Rays` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:1460 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`particles/[7]/parent`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `parent` 出现在加载实参里 |
| 69 | `Sparks R` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:1678 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`particles/[8]/name`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 70 | `Level Up Effect Down` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:2169 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`images/[3]/name`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 71 | `Sparks RL` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:1876 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`particles/[9]/name`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `name` 出现在加载实参里 |
| 72 | `Glow Explosion` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:866 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`particles/[4]/parent`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `parent` 出现在加载实参里 |
| 73 | `Glow Explosion Down` | `GameObject` | `MyGame/Assets/CardPresentation/Resources/Art/card_display_effects.json:1261 + CardPresentation/Shell/CardDisplayEffects.cs:86` | 表值（`particles/[6]/name`）→ 读者 `CardDisplayEffects.cs` 动态加载口，字段 `name` 出现在加载实参里 |

## §4 U（静态解不出，单列）：935 条

| # | 名字 | 类型 | 证据 | 说明 |
|---:|---|---|---|---|
| 1 | `Documentation` | `MonoScript` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里没有同名字面量）⇒ 无法静态判定 |
| 2 | `Documentation` | `MonoScript` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里没有同名字面量）⇒ 无法静态判定 |
| 3 | `UniversalAdditionalLightData` | `MonoScript` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里没有同名字面量）⇒ 无法静态判定 |
| 4 | `software` | `Texture2D` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里没有同名字面量）⇒ 无法静态判定 |
| 5 | `UniversalAdditionalLightData` | `MonoScript` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里没有同名字面量）⇒ 无法静态判定 |
| 6 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 7 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 8 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 9 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 10 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 11 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 12 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 13 | `InboxBtn` | `GameObject` | `CardPresentation/Shell/TopBar.cs:562` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:LoadAvatar`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/TopBar.cs:249）⇒ 无法静态判定 |
| 14 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 15 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 16 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 17 | `OneText` | `GameObject` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BadgeNotifications.cs:523）⇒ 无法静态判定 |
| 18 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 19 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 20 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 21 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 22 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 23 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 24 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 25 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 26 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 27 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 28 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 29 | `Badge Highlight` | `GameObject` | `CardPresentation/Shell/CampaignTab.cs:703` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Draw`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/CampaignTab.cs:1717）⇒ 无法静态判定 |
| 30 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 31 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 32 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 33 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 34 | `OneText` | `GameObject` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BadgeNotifications.cs:523）⇒ 无法静态判定 |
| 35 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 36 | `Badge Highlight` | `GameObject` | `CardPresentation/Shell/CampaignTab.cs:703` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Draw`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/CampaignTab.cs:1717）⇒ 无法静态判定 |
| 37 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 38 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 39 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 40 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 41 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 42 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 43 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 44 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 45 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 46 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 47 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 48 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 49 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 50 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 51 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 52 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 53 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 54 | `OneText` | `GameObject` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BadgeNotifications.cs:523）⇒ 无法静态判定 |
| 55 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 56 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 57 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 58 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 59 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 60 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 61 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 62 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 63 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 64 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 65 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 66 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 67 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 68 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 69 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 70 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 71 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 72 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 73 | `WIldcard Counter` | `GameObject` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里没有同名字面量）⇒ 无法静态判定 |
| 74 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 75 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 76 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 77 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 78 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 79 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 80 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 81 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 82 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 83 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 84 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 85 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 86 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 87 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 88 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 89 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 90 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 91 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 92 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 93 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 94 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 95 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 96 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 97 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 98 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 99 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 100 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 101 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 102 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 103 | `OneText` | `GameObject` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BadgeNotifications.cs:523）⇒ 无法静态判定 |
| 104 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 105 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 106 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 107 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 108 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 109 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 110 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 111 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 112 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 113 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 114 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 115 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 116 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 117 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 118 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 119 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 120 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 121 | `OneText` | `GameObject` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BadgeNotifications.cs:523）⇒ 无法静态判定 |
| 122 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 123 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 124 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 125 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 126 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 127 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 128 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 129 | `Badge Highlight` | `GameObject` | `CardPresentation/Shell/CampaignTab.cs:703` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Draw`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/CampaignTab.cs:1717）⇒ 无法静态判定 |
| 130 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 131 | `OneText` | `GameObject` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BadgeNotifications.cs:523）⇒ 无法静态判定 |
| 132 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 133 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 134 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 135 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 136 | `Badge Highlight` | `GameObject` | `CardPresentation/Shell/CampaignTab.cs:703` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Draw`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/CampaignTab.cs:1717）⇒ 无法静态判定 |
| 137 | `OneText` | `GameObject` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BadgeNotifications.cs:523）⇒ 无法静态判定 |
| 138 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 139 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 140 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 141 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 142 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 143 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 144 | `Avatar Item Small` | `GameObject` | `CardPresentation/Battle/BattleDriver.cs:17488` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里有同名字面量但不作加载实参：CardPresentation/Battle/BattleDriver.cs:13453）⇒ 无法静态判定 |
| 145 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 146 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 147 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 148 | `OneText` | `GameObject` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BadgeNotifications.cs:523）⇒ 无法静态判定 |
| 149 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 150 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 151 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 152 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 153 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 154 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 155 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 156 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 157 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 158 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 159 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 160 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 161 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 162 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 163 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 164 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 165 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 166 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 167 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 168 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 169 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 170 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 171 | `Avatar Item Small` | `GameObject` | `CardPresentation/Battle/BattleDriver.cs:17488` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里有同名字面量但不作加载实参：CardPresentation/Battle/BattleDriver.cs:13453）⇒ 无法静态判定 |
| 172 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 173 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 174 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 175 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 176 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 177 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 178 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 179 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 180 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 181 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 182 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 183 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 184 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 185 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 186 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 187 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 188 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 189 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 190 | `Badge Highlight` | `GameObject` | `CardPresentation/Shell/CampaignTab.cs:703` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Draw`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/CampaignTab.cs:1717）⇒ 无法静态判定 |
| 191 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 192 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 193 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 194 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 195 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 196 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 197 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 198 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 199 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 200 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 201 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 202 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 203 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 204 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 205 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 206 | `Badge Highlight` | `GameObject` | `CardPresentation/Shell/CampaignTab.cs:703` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Draw`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/CampaignTab.cs:1717）⇒ 无法静态判定 |
| 207 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 208 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 209 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 210 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 211 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 212 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 213 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 214 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 215 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 216 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 217 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 218 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 219 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 220 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 221 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 222 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 223 | `OneText` | `GameObject` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BadgeNotifications.cs:523）⇒ 无法静态判定 |
| 224 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 225 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 226 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 227 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 228 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 229 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 230 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 231 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 232 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 233 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 234 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 235 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 236 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 237 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 238 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 239 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 240 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 241 | `Badge Highlight` | `GameObject` | `CardPresentation/Shell/CampaignTab.cs:703` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Draw`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/CampaignTab.cs:1717）⇒ 无法静态判定 |
| 242 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 243 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 244 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 245 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 246 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 247 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 248 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 249 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 250 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 251 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 252 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 253 | `Badge Highlight` | `GameObject` | `CardPresentation/Shell/CampaignTab.cs:703` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Draw`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/CampaignTab.cs:1717）⇒ 无法静态判定 |
| 254 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 255 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 256 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 257 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 258 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 259 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 260 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 261 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 262 | `OneText` | `GameObject` | `CardPresentation/Battle/ArenaRuntimeLoader.cs:100` | 静态找不到字面量或整值表命中；仓里有动态加载口 `Resources.Load`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BadgeNotifications.cs:523）⇒ 无法静态判定 |
| 263 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 264 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 265 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 266 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 267 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 268 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 269 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 270 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 271 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 272 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 273 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 274 | `Message Preview (1)` | `GameObject` | `CardPresentation/Shell/AllianceMemberTab.cs:934` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:ArtOf`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AllianceMemberTab.cs:992）⇒ 无法静态判定 |
| 275 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 276 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 277 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 278 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 279 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 280 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 281 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 282 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 283 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 284 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 285 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 286 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 287 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 288 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 289 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 290 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 291 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 292 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 293 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 294 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 295 | `Fill Area` | `GameObject` | `CardPresentation/Shell/AchievementsMenu.cs:304` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Art`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/AchievementsMenu.cs:417）⇒ 无法静态判定 |
| 296 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 297 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 298 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 299 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| 300 | `Price Display` | `GameObject` | `CardPresentation/Shell/BaseOfferPopup.cs:636` | 静态找不到字面量或整值表命中；仓里有动态加载口 `wrap:Create`（代码里有同名字面量但不作加载实参：CardPresentation/Shell/BaseOfferPopup.cs:986）⇒ 无法静态判定 |
| … | 余 635 条见 `--tsv-out` | | | |

> ⚠️ **U 不是「没加载」**，是「静态判不了」。它们里的绝大多数会落进 §1 的 L0 里 —— 这也是 L0 必须做的原因。
> ⚠️ U 行「证据」列列的是**仓里第一个动态加载口**（不是这些名字真的从那里加载）——判法只用它回答「仓里存在动态加载口 ⇒ 静态判不了」。现读里绝大多数指向同一个文件，是**产物形状**，不是发现。

## §5 L0 探针代码（粘贴即用）

```csharp
// Assets/CardPresentation/Core/LoadProbe.cs —— 运行时程序集（⛔ 不是 Editor 目录）
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class LoadProbe
{
    public static bool Enabled;
    static readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);
    static readonly List<string> _rows = new List<string>();
    static string _out;

    public static void Begin(string outPath)
    {
        _out = outPath; _seen.Clear(); _rows.Clear(); Enabled = true;
        Debug.Log("[LoadProbe] begin -> " + outPath);
    }

    /// <summary>加载 API 的每个动态实参都从这里过一遍。名字为空 / 已记过 ⇒ 直接返回（不拖慢正常路径）。</summary>
    public static void Record(string name)
    {
        if (!Enabled || string.IsNullOrEmpty(name)) return;
        if (_seen.Add(name)) _rows.Add(name + "\tload");
    }

    public static void End()
    {
        Enabled = false;
        if (string.IsNullOrEmpty(_out)) return;
        try
        {
            string d = Path.GetDirectoryName(_out);
            if (!string.IsNullOrEmpty(d) && !Directory.Exists(d)) Directory.CreateDirectory(d);
            File.WriteAllLines(_out, _rows, new UTF8Encoding(false));
            Debug.Log("[LoadProbe] wrote " + _rows.Count + " rows -> " + _out);
        }
        catch (Exception e) { Debug.LogWarning("[LoadProbe] write failed: " + e.Message); }
    }
}
```

插桩写法（`--wrappers` 列出的每个包装口，函数体**第一行**加一句；⛔ 不改返回值、不改原逻辑）：

```csharp
public static Texture2D Get(string path)
{
    LoadProbe.Record(path);        // ← 只加这一行
    ...                            //   原逻辑一字不动
}
```

## §6 覆盖边界与已知局限

- 代码：**610** 个非 Editor `.cs`；加载调用点 **1962**（动态实参 **1474**）；识别出包装口 **84** 个。
- 表：读入 **1455** 个 JSON（`Assets/**/*.json` + `数据/**/*.json`，排除 `数据/索引`）；解析失败 **0**；超大跳过 **0**；表值条目 **331125**；文本行 **12739**。
- 本报告是**全量**（16837 行），不是抽样；但「等级」仍然是**静态判法**的结论，L3 里必然混着「其实运行时加载了、只是走的动态路径」⇒ 只有 §1 的 L0 能把它们分出来。
- 判法本身的六条已知局限（单行实参解析 / 同文件常量折叠 / L2 字段名级 / `#if` 简化栈 / SO 注册表按名字取资产不算加载 API / 子串命中大小写不敏感）见 `B107_R0_U2真加载核查.md` §2，本条不重复。
- 本报告在本文件里对「子串 / 文本行」两处用了**大串预筛**（`str.find`）——只影响 L3 那一行的**证据行号**，不影响等级；`--selfcheck` 会把全量等级与 `gen_u2_loaded_probe.classify` 逐条对齐（0 不一致才算过）。
- ⛔ 本报告**不判**「资产该不该留」（那是 R7/R8/R9 的活）。

