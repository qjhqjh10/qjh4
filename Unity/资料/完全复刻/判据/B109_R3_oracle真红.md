# B109_R3 · 功能点 oracle「4 条真红」逐条查判据

> 只读查判据代理 `B109_R3` · 2026-10-31（第一百零九会话第 1 波）· 工作根 `d:/4`。⛔ 本代理**没有**改任何源码 / 账 / 正本 / 规格表 / 工具；只写了本文件。
> 数据快照：`sweep_orig.tsv` mtime `10-31 01:12`（962 效果）· `sweep_exp.tsv` mtime `10-31 03:39`（957 效果）· `MyGame/Assets/WarpforgeVFX/Prefabs/*.prefab` 现读 **978** 个。
> ⛔ 上游读数：`报告/B108_A9.md` §三（机器通过 64 / 失败 33 / 人工待签 30 / N/A 29，其中 33 = 29 缺数据 + **4 真红**）。

---

## §0 这 4 条是哪 4 个功能点、涉及 5 个效果

| FP | 域 / 功能点 | oracle op | 本行点名的效果 | 原版峰值 `lit`/`sum` | `sweep_exp.tsv` 现状 |
|---|---|---|---|---|---|
| **FP-VFX-01** | 特效动画 · 镜头震动与震屏预设 | `sweep_ratio` | `Earthquake Shake only` · `Basic_army_GSC_shake` | **0 / 0** · 正常 | **无行** · `\|ln\|=0.008` ✅ |
| **FP-VFX-02** | 特效动画 · 粒子生成/池化/碰撞通知 | `sweep_ratio` | `Environmental Condition Particles Orbital` · `Orks Environmental Condition Night` · `BulletImpact_Bolter_Large Area` | **0 / 0** · **58 / 58** · 正常 | **无行** · **无行** · `\|ln\|=0.000` ✅ |
| **FP-VFX-07** | 特效动画 · LUT 调色与后处理 | `sweep_ratio` | `Environmental Condition Dark Angels Asteroid Zone` · `… Void Combat` · `EnvironmentalCondition Sororitas Raging Storm` | 正常 · 正常 · 正常 | 0.123 ✅ · 0.051 ✅ · **`\|ln\|=0.348` ❌** |
| **FP-VFX-09** | 特效动画 · 战斗内卡牌/单位表现 | `sweep_ratio` | `Swarm Battle Card Icon` · `Teleport Trigger Board Effect Only` · `Psychic_Lightning_down_Waaagh_Green` · `BansheeHowl` | **12587 / 14192** · **0 / 0** · 正常 · 正常 | **无行** · **无行** · 0.000 ✅ · 0.000 ✅ |

**一句话结论（5 个效果分成三类，⛔ 别一律当「原版有我们没有」）**

1. **3 个是假红（原版自己也 0 像素）**：`Earthquake Shake only` / `Environmental Condition Particles Orbital` /
   `Teleport Trigger Board Effect Only` —— 它们三个在原版侧 8 个时刻**全是 `lit=0 sum=0`**；它们本来就是
   「只有 AnimFX 模块 / 只有 spawner 脚本的空壳 prefab」（见 §1 §2 §4 的组件表）。
2. **1 个是次阈值假红**：`Orks Environmental Condition Night` 原版恒 58 亮点，**低于尺子硬闸 `LIT_MIN=60`**
   （`工具/analyze_sweep.py:62`）⇒ 按 `analyze_sweep.py` 自己的分支它是 **W/跳过**，不是红。
3. **1 个真需要重测**：`Swarm Battle Card Icon` 原版**真有内容**（12587 亮点、恒定），而我们的 prefab
   是 **09:24 才被 `EffectExporter.RunListed` 恢复**的，`sweep_exp.tsv` 03:39 就跑了 ⇒ **基线过期**（§4）。
4. **唯一方向稳定的真差异 = FP-VFX-07 的 `Sororitas Raging Storm`**（5 份历史 exp 基线逐份算
   `|ln|` = **0.255 / 0.348 / 0.445 / 0.519 / 0.527**，**每次都偏暗**，不是随机抖动）。

> 🔴 **共同机制（这条比 4 条本身更重要）**：`工具/fp_oracle_check.py:645-650` 判「只有一侧有」时
> **只看行在不在、不看有没有内容** —— 而它自己在 `:77-91` 的文档声称「EXACTLY like `analyze_sweep.py`」。
> `analyze_sweep.py:157-201` 的做法是 `orig.get(name, {})`（**缺行 = 全程空**），
> 先判 `not orig_on and not exp_on ⇒ W`，**只有另一侧峰值 `< LIT_MIN*0.6` 才允许判 C/D**（那条闸是
> 2026-10-31 `B105_R1` 加进去的，见 `analyze_sweep.py:170-177` 的注释）。**`fp_oracle_check.py` 没继承这条闸。**

---

## §1 FP-VFX-01 · `Earthquake Shake only`

### 原版判据
- 资产：`d:/2/tools/assetripper_out/ExportedProject/Assets/Earthquake Shake only.prefab`
  —— 根 GO 挂 **3 个 MonoBehaviour，一个渲染器都没有**（`ParticleSystem`=0 / `ParticleSystemRenderer`=0 /
  `SpriteRenderer`=0 / `MeshRenderer`=0）：
  ① `AnimFXController`（`destroyTime: 6` / `exitDestroyTime: 3` / `sounds[0]=Hive Fleet Rumble`）·
  ② `AnimFXModuleScreenShake`（preset `Shake Earthquake` · `sustainTime 1.5` · `amplitude 0.2` · `frequency 0.1` · `decayTime 2`）·
  ③ `AnimFXModulePostProcess`（`doLUTAnim 1` · `customLUT1 = LUT_Dark` · `timeToOn 1` · `timeOn 1`）。
- 同一份参数在我们数据里也有一份：`数据/游戏数据/animfx_components.json` → `effects["Earthquake Shake only"]`（3 条）。
- **它渲染不出任何像素**（sweep 原版侧 8 个时刻全 0 就是证据）—— 名字里的 `Shake only` 是字面意思。

### 我们现状（已实现，不缺任何东西）
- prefab：`MyGame/Assets/WarpforgeVFX/Prefabs/Earthquake Shake only.prefab`（1 GO + `WarpforgeEffectBinder`，
  **节点树与原版一致**）；guid `e9af97dc…` 与效果库条目**逐字相同**。
- 模块走数据装配：`Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 该条目
  `modules = [AnimFXModuleScreenShake, AnimFXController, AnimFXModulePostProcess]`；运行时由
  `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:440-466`（`BuildModules` → `WFModuleFactory.Create`）装出来；
  两个 kind 都已登记：`WFModuleScreenShake.cs:60` / `WFModulePostProcess.cs:174` 的 `[WFModuleKind(...)]`。
- **没到的地方 = 尺子**：`sweep_exp.tsv` 里没有这一行 —— 因为 03:39 跑 exp 那趟时这个 prefab 还不存在
  （09:24 `RunListed` 才恢复，见 §4 的同一段证据）。`fp_oracle_check.py` 就凭「缺行」判红。

### 施工单（⛔ 不改引擎/表现代码，只改尺子与判据）
1. **`工具/fp_oracle_check.py:641-651`** —— 删掉「缺行即红」两支，改成内容判定（与 `analyze_sweep.py` 同口径）：
   ```python
   for n in names:
       if n not in orig and n not in exp:
           reds.append('%s：两侧都没有这个效果' % n); continue
       # ⬇ 缺行 = 该侧全程空（同 analyze_sweep.py:158 的 orig.get(name, {})）；
       #    ⛔ 别用 orig[n]（defaultdict 会就地建空条目）
       o, e = orig.get(n, {}), exp.get(n, {})
   ```
   并把原来的 `o, e = orig[n], exp[n]`（现 :651）一并删掉。
   **行为变化**：两侧全 0 ⇒ 走 `not oon and not eon ⇒ skips`（不再红）；**原版真有内容而我们缺行**（如现在的
   `Swarm Battle Card Icon`）⇒ 落到 `elif not eon: reds.append('只有原版有内容（导出整个丢了，导出峰值 0）')`，**照旧红**。
2. **重测基线**（与 §4 同一趟，见那边命令）：把这 5 个效果补进 `sweep_exp.tsv`。
3. **`资料/比对基线/README.md:13-14`** 就地订正：现在写的「5 条 **原版有、我们 `WarpforgeVFX/Prefabs` 里没有**」
   **已不成立**（现读 978 个 prefab，5 个都在）。

### 断言要点
- 机器判据（修完工具后）：`python -I -X utf8 d:/4/Unity/工具/fp_oracle_check.py --only FP-VFX-01` ⇒ **PASS**。
- **灭自证**（必须补，否则「把闸放松」和「真修好」分不开）：在 `_tmp_view/oracle/make_selftest_specs.py` 加一个反例 ——
  造一行 `effect=<只存在于 orig、且 orig 全程 0 的效果>`，**去掉 exp 的对应行** ⇒ 必须判 `W`/跳过，⛔ 不许判红；
  再造一行 `effect=<orig 有内容、exp 缺行>` ⇒ 必须判红。
- 运行时那一半已由 `AnimFXCheck` 覆盖（`MyGame/Assets/WarpforgeArena1/Editor/AnimFXCheck.cs:91-92`
  「带模块的效果数 / 模块总数 = 数据∩库」，`:104`「每种 kind 都有实现」）。

### 还差什么
无（判据闭合）。**这条不需要任何代码改动。**

---

## §2 FP-VFX-02 · `Environmental Condition Particles Orbital` / `Orks Environmental Condition Night`

### 原版判据
- `Environmental Condition Particles Orbital`（`…/Assets/Environmental Condition Particles Orbital.prefab`）：
  4 个 GameObject（根 + `Orbital 3 spawner` + `Orbital 4 spawner` + `'Orbital 5 spawner '` 带尾随空格），
  **0 个 `ParticleSystem` / 0 个 `ParticleSystemRenderer`**；根挂 `ParticleSystemAreaSpawnerController`、
  3 个子件各挂一个 `ParticleSystemAreaSpawner` —— 是**运行时按区域撒粒子**的空壳，静止时 0 像素（sweep 原版 8 时刻全 0）。
- `Orks Environmental Condition Night`（同目录 `.prefab`）：**25 个 `SpriteRenderer`**，静态图，sweep 原版恒
  `lit=58 / sum=58`（**低于 `LIT_MIN=60`**）。

### 我们现状（都已在，且结构对齐）
- `MyGame/Assets/WarpforgeVFX/Prefabs/Environmental Condition Particles Orbital.prefab`：**同样 4 个 GO、节点名逐字一致
  （含 `'Orbital 5 spawner '` 的尾随空格）**，只是 4 个 MonoBehaviour 换成 1 个 binder。
  spawner 由**旁挂数据**在运行时就地建：`数据/游戏数据/env_blendables.json` 的
  `standalone["Environmental Condition Particles Orbital"]`（3 条 spawner + 1 条 controller，`boxSize` / `spawnRate` /
  `chances` / `particleSystemPrefab=Orbital 3/4/5 repeat` 全在），落到
  `CardPresentation/Battle/EnvironmentApplier.cs:585`（`MakeSpawner`，**按 `FindByPath` 找节点**，见 `:511`）、
  `:393`（`BuildStandaloneSpawners`）；建不出来会记 `StandaloneMissedCount`（`:117`）**出声不静默**。
- `Orks Environmental Condition Night.prefab`：**同样 25 个 `SpriteRenderer`、0 个空 sprite**、
  两侧各只有 **1 张** sprite（guid 不同 —— 我们是重导入的副本，**没有**逐字节核过是不是同一张图）。
- **没到的地方 = 尺子**（同上：两条都缺 exp 行，原因见 §4）。

### 施工单
1. 同 §1 第 1 条（工具那条闸修一次，这两条一起解决）。
2. 同 §4 的重测命令（这 2 个效果一起补行）。
3. `资料/比对基线/README.md:14` 订正（同上）。

### 断言要点
- `fp_oracle_check.py --only FP-VFX-02` ⇒ **PASS**。
- **`Orks` 是次阈值，别给它加「必须 ≥X 亮点」的硬断言** —— 原版自己是 58，尺子阈值是 60；
  要让这条有判别力，只能等它进 `analyze_sweep.py` 的 weak 支（比值中位），**不能定「必须 >0」**（我们 25 张图只要
  有一张没渲出来就会红，而那不是原版语义）。
- spawner 那条：`EnvironmentApplier` 侧已有读数口（`StandaloneMissedCount` / `StandaloneMissed`），
  建议在宿主断言里钉死「该环境应用后 `StandaloneMissedCount == 0`」（**这是真正在测「我们建不建得出来」的那条**，
  比 sweep 的亮度比有判别力）。

### 还差什么
- ⚠️ **`Orks ... Night` 的 58 亮点到底是不是「原版真的一张静态图」没逐张核**（我们只核了 SR 数量与 sprite 非空）。
  如需闭环，要等重测后看两侧数值是否同量级；**这不阻塞本条**。

---

## §3 FP-VFX-07 · `EnvironmentalCondition Sororitas Raging Storm`（**唯一真差异**）

### 原版判据 + 现状（逐项已排除的）
- 原版 prefab（`…/Assets/EnvironmentalCondition Sororitas Raging Storm.prefab`）：16 个 `ParticleSystem` / 16 个
  `ParticleSystemRenderer`，5 个材质槽（`Sand Storm` / `Embers 1` / `Sororitas_Window_flash` / `…_floor` / `…_tint`）。
- 我们 prefab：**16 / 16 逐名一致**；**顶层标量全同**（`lengthInSec` / `simulationSpeed` / `looping` / `prewarm` /
  `playOnAwake` 16/16 无差）；`MinMaxCurve` 的 `(minMaxState, scalar, minScalar)` 序列与全部 gradient 关键帧
  **16/16 无差**；模块 `enabled` 位 **11/11 无差**；**材质指派逐渲染器与原件一致**
  （Smoke×4→`Sand Storm` · WindowFlash×4→`…_flash` · floor×4→`…_floor` · Tint×2→`…_tint` · Lightning×2→`Embers 1`）。
  ⇒ 与 `判据/B9_特效参数对账.tsv` 该行「**逐字段 0 差异**」**互相印证**。
- **尺子侧已排除的**：`资料/比对基线/尺子不稳定名单.tsv:2` **第一行就是它**（`最大_dln = 0.412`，
  两次判定一次「偏暗」一次「对得上」）⇒ **单次读数不可当结论**。

### 实测形态（这才是要修的东西）
| t | 0.15 | 0.30 | 0.50 | 0.75 | 1.00 | 1.50 | 2.00 | 3.00 |
|---|---|---|---|---|---|---|---|---|
| 原版 `sum` | 1356 | 1413 | 786 | 81 | 84 | 90 | 95 | 103 |
| 我们 `sum` | 1321 | 1378 | 750 | 47 | 51 | 59 | 65 | 75 |
| 比值 | 0.974 | 0.975 | 0.954 | **0.580** | 0.607 | 0.656 | 0.684 | 0.728 |

⇒ **前 0.5 s 只差 2.5%，0.75 s 之后稳定只剩 58–73%**。原版 0.5→0.75 从 786 掉到 81（闪电那一波熄灭），
**尾部只剩少数几个发射器在画**。5 份历史 exp 基线（`sweep_exp.tsv` / `_baseline_1031` / `_preforig_1031` /
`_normal_1021` / `_builtin_1021`）算出的 `|ln|` = **0.348 / 0.519 / 0.445 / 0.527 / 0.255** —— **方向永远是「我们更暗」**，
幅度在 0.255–0.527 之间漂 ⇒ **是真差异，不是噪声；但幅度必须先量准**。

### 施工单（三步，缺一步就会「用不准的尺子改资产」）
1. **先把它单独量出来**（去掉批量累积压暗与跨进程漂移这两层污染）：
   `_tmp_view/sweep_targets.txt` 只写一行 `EnvironmentalCondition Sororitas Raging Storm`，
   用 §4 的 `WFSWEEP_OUT` 覆盖跑 `Side=orig` / `Side=exp` 各一趟（同一天、同机、串行）。
   判据：若两趟 `|ln|` 都 `<=0.30` ⇒ **本 FP 结案为「尺子假阳性」**，把结论写回规格表 `备注`；
   若仍 `>0.30` ⇒ 进第 2 步。
   ⚠️ `EffectSweepBatch.cs:312-327` 写着「**排在后面的效果可能被累积压暗**（`StrikeEffect` 单独 437 / 排第 6 只有 344）」
   —— 所以「单独跑」是必须的，不是可选优化。
2. **逐渲染器隔离**：`MyGame/Assets/WarpforgeArena1/Editor/EffectIso.cs:115` 的 `const float SimTime = 1.2f;`
   **写死且没有 env 覆盖**（`EffectCompare.cs:26-27` 有 `WFCMP_SIMT`，这边没有）。改成：
   ```csharp
   static readonly float SimTime =
       float.TryParse(System.Environment.GetEnvironmentVariable("WFISO_SIMT"), out var _st) ? _st : 1.2f;
   ```
   然后 `WFISO_SIMT=0.75 WFISO_FX="Raging Storm" bash d:/4/Unity/工具/run_iso.sh both` +
   `python -I -X utf8 d:/4/Unity/工具/analyze_iso.py "EnvironmentalCondition Sororitas Raging Storm"`
   ⇒ 逐槽 `contrib`。**要看的槽按嫌疑排**：`Smoke Traily Background` ×4（材质 `Sand Storm`，
   原版 shader `Everguild/FX/Alpha Mask One Layer`）> `WindowTint TEMP` ×2（`URP/Particles/Unlit`，`hadEmissionKeyword=1`）
   > `WindowFlash` ×4（`Everguild/FX/Extra Color`）。**别先改资产再量**。
3. **按第 2 步结果落修**（现阶段**不下结论**）：若缺口落在 `Sand Storm` 那 4 个槽，比较对象是
   `WarpforgeVFX/Shaders/WFAlphaMaskOneLayer.shader`（**注意它头部写着「在白名单里 ⇒ 运行时走原件，本文件只在兜底路生效」**，
   所以要么是兜底路被走到了、要么是材质属性没灌进去 —— 先分这两种，再改 shader）·
   若落在 `Tint` 槽，比较 `_EMISSION` 开没开（该效果 `emissionOn=1`、tint 材质 `hadEmissionKeyword=1`）。

### 断言要点
- 验收 = `fp_oracle_check.py --only FP-VFX-07` 的 `|ln|`，**但必须先固定测量方式**（第 1 步那趟命令），
  否则同一个实现会被测成 0.255～0.527 五种结果。
- **灭自证**：第 2 步改完隔离后，要有一条断言能区分「只有 tail 的槽被修好」与「整体被我调亮」——
  即同时钉 `t=0.30`（原已 0.975，不许变亮超过 2%）与 `t=3.00`（必须 ≥0.85）。
- ⚠️ 这条**不产生 `play check` 指纹变化**（`ruleprobe` 只跑引擎，不碰渲染）。

### 还差什么
- **根因未定**（`⛔ 不是没查，是查到了「PS 数据全同、材质指派全同」，剩下的在执行侧，本代理不能跑 Unity）**。**
- `资料/任务线/特效线/任务总表.md:17` 里「`Raging Storm` / 不稳定名单那几条需 Unity 单效果两跑」**至今仍开**，
  且 2026-10-31 `B108_A9` 已经把 `特效还原_进度与交接.md:1640`「全表无 >0.30」订正为「不成立」。

---

## §4 FP-VFX-09 · `Swarm Battle Card Icon` / `Teleport Trigger Board Effect Only`

### 原版判据
- `Swarm Battle Card Icon.prefab`：根 GO（layer 10）挂 `SwarmIconController`（`spriteRenderer` / `offAnimationTime 0.3` /
  `myAnimation` / `toggleOnSound`）+ 传统 `Animation`（clip `SwarmIconOn`）；子件 `Icon` 挂 `SpriteRenderer`
  （`m_Sprite` 非空、`m_Size 3.06×4.45`、`localScale 1.016651`）⇒ **sweep 原版恒 12587 亮点**（静态图）。
- `Teleport Trigger Board Effect Only.prefab`：**0 渲染器**，只有 `AnimFXController` + `AnimFXModuleScreenShake`
  （`sustainTime 0.75` / `amplitude 0.35` / `frequency 2`）+ `AnimFXModulePostProcess`（`customLUT1 = LUT Blue Tint`）。
  sweep 原版 8 时刻全 0。

### 我们现状（两条都已实现）
- **Swarm**：`MyGame/Assets/WarpforgeVFX/Prefabs/Swarm Battle Card Icon.prefab` —— 根（layer 10）+ `Animation`
  （`m_Animations[0]` = `SwarmIconOn.anim`）+ binder + 子件 `Icon` 带 `SpriteRenderer`
  （sprite = `WarpforgeVFX/Textures/Atlas_trait_icon_swarm_sprite.png`、material = 内置 `Sprites-Default`、
  `m_Size 3.06×4.45`、`localScale 1.016651`、`m_WasSpriteAssigned 1`）。guid `2260f79b…` 与库条目一致。
  表现侧：`CardPresentation/Core/CardView.cs:8769-8825`（判据段）+ `:8826-8890`（`SetSwarm` 逐句对
  `SwarmIconController__Toggle.c`；`SwarmOffFadeTime 0.3` / `SwarmClipName` / `SwarmOnCue` 全是原版值）+
  `CardPresentation/Battle/BattleDriver.cs:39352-39370`（拖动预览那拍，判据 `BattleManager__Update.c:608`）。
- **Teleport**：prefab 节点树与原版一致（1 GO，只有 binder），模块在库条目里
  （`AnimFXModuleScreenShake` + `AnimFXController` + `AnimFXModulePostProcess`），装配路同 §1。
- **没到的地方 = 基线过期**（这是本条的**唯一**原因，证据链）：
  - `EffectExporter.Run` **10-31 03:26** 重写整个 `Prefabs/`，只导「子树有 `ParticleSystemRenderer` 的根」；
    这两个是 `ListedPrefabs` 的非粒子根 ⇒ **当时从工程里消失**（`调度台日志.md`「🔴 2026-10-31 新坑」那节）。
  - `sweep_exp.tsv` **03:39** 跑 ⇒ 目标清单里没有它们 ⇒ 0 行。
  - `EffectExporter.RunListed` **09:24** 才恢复（`Prefabs/` 里 58 个 `*.prefab` 的 mtime = 09:24）。
  - **现读**：`Prefabs/` 共 **978** 个，`sweep_exp.tsv` 只有 **957** 个 ⇒ **21 个 prefab 从未被 exp 扫过**；
    其中 5 个在原版侧有行（正是这 4 条红的 5 个效果），另 16 个两侧都没有。
  - `sweep_frames.tsv` **这 5 个的取景全在** ⇒ 只需补跑 exp，**不必重跑 orig**。

### 施工单（一次修好 4 条里的 3 条）
1. **加输出路径覆盖**（否则补跑会整份覆盖 962 效果的基线）：`MyGame/Assets/WarpforgeArena1/Editor/EffectSweepBatch.cs:61`
   ```csharp
   static string ResultPathFor(string side)
   {
       var o = System.Environment.GetEnvironmentVariable("WFSWEEP_OUT");
       return string.IsNullOrEmpty(o) ? $@"d:\4\Unity\资料\比对基线\sweep_{side}.tsv" : o;
   }
   ```
2. **换掉目标清单**（`EffectSweepBatch.cs:96` 的 `TargetFile` 是**固定路径** `d:\4\_tmp_view\sweep_targets.txt`；
   现在那份是 **233 行**，不换掉就会「只扫 233 个 + 整份覆盖」）：
   ```bash
   cp d:/4/_tmp_view/sweep_targets.txt d:/4/_tmp_view/sweep_targets.bak_233.txt
   printf '%s\n' "Earthquake Shake only" "Environmental Condition Particles Orbital" \
     "Orks Environmental Condition Night" "Swarm Battle Card Icon" \
     "Teleport Trigger Board Effect Only" > d:/4/_tmp_view/sweep_targets.txt
   ```
3. **只跑 exp 那侧**（`EffectSweepBatch.cs:47` 不设变量 = `exp`；取景缓存已有）：
   ```bash
   cd d:/4/Unity && mkdir -p _tmp_view/sweep_fix && unset ELECTRON_RUN_AS_NODE
   WFSWEEP_OUT=d:/4/_tmp_view/sweep_fix/exp5.tsv \
     "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
     -projectPath "D:\4\Unity\MyGame" -executeMethod EffectSweepBatch.Run -logFile -
   ```
   ⚠️ **看日志末行**（`EffectSweepBatch.cs:414`）：`成功 5，跳过 0` 才是对的；`跳过 >0` 要逐条查（它不出声就不会有这行）。
4. **并回主基线**：把 `exp5.tsv` 的 **40 行**（5 效果 × 8 时刻）并按进 `资料/比对基线/sweep_exp.tsv`
   （**裸 `split('\t')`、utf-8-sig 表头保留、行尾按现有文件**；`analyze_sweep` / oracle 都按键取值，行序无所谓）。
5. **还原 233 行的 `sweep_targets.txt`**（`mv` 回去），否则下次全量跑又会被截断。
6. 复跑 `python -I -X utf8 d:/4/Unity/工具/fp_oracle_check.py --only FP-VFX-01,FP-VFX-02,FP-VFX-09`。

### 断言要点
- 修完工具（§1 第 1 条）+ 补完行之后：**FP-VFX-01 / 02 / 09 必须 PASS**；
  若 `Swarm` 补测后仍红，**先看贴图导入**（`Atlas_trait_icon_swarm_sprite.png.meta` 的 `alphaIsTransparency` / mip），
  **不要去改粒子参数**（它与原版 `SpriteRenderer` 逐字段同形）。
- **灭自证（尺子侧）**：补一条「**prefab 数 = sweep 行数**」的护栏断言 —— 现读 978 vs 957 就是这条护栏该抓的东西；
  建议加在 `EffectSweepBatch` 收尾日志里（`成功 + 跳过 == targets.Count` 且 `targets.Count == PrefabDir 里 *.prefab 数`），
  **⛔ 不要**写在 oracle 里（oracle 读的是文件，看不见目录）。
- **灭自证（口径侧）**：§1 那两条反例（全程 0 不许红 / 有内容缺行必须红）。

### 还差什么
- 补跑必须由**主对话跑 Unity**（子代理不许），且 `Prefabs/` 不在 git 里（`.gitignore`），**补跑前先 `cp -r` 一份快照**
  （`_tmp_view/scene_bak/` 惯例），因为 `EffectExporter.Run` 有过「擦掉 listed 非粒子 prefab」的前科。

---

## §5 汇总（按落点文件分组，方便按文件切块派写手）

| 文件 | 改什么 | 服务哪条 | 风险 / 影响面 |
|---|---|---|---|
| `工具/fp_oracle_check.py`（`:641-651`） | 删「缺行即红」两支，改 `orig.get(n, {})` / `exp.get(n, {})` | §1 §2 §4（**3 条假红的总闸**） | 纯工具；不碰 `.cs`；`play check` 指纹不变；**必须配 2 条反例自检** |
| `MyGame/Assets/WarpforgeArena1/Editor/EffectSweepBatch.cs`（`:61`） | 加 `WFSWEEP_OUT` 覆盖 | §4 §3 | Editor 工具；**账里 0 行引用它**（已核 `资料/完全复刻/账/` grep = 0）⇒ 加行不漂账 |
| `MyGame/Assets/WarpforgeArena1/Editor/EffectIso.cs`（`:115`） | 加 `WFISO_SIMT`（照抄 `EffectCompare.cs:26-27`） | §3 第 2 步 | 同上，账 0 引用 |
| `资料/比对基线/sweep_exp.tsv` | 补 5 效果 × 8 时刻 = 40 行 | §1 §2 §4（+§3 的单独两趟另存） | **主对话跑 Unity 才能产出**；先备份、先 `WFSWEEP_OUT` |
| `资料/比对基线/README.md`（`:13-14`） | 订正「5 条我们 Prefabs 里没有」这句话 | §1 §2 §4 | 纯文档 |
| `资料/完全复刻/判据/功能点_oracle规格.tsv`（FP-VFX-01/02/09 的 `备注`） | 把「只在原版侧（5 条缺 prefab 之一）」改成「基线过期，见 B109_R3」 | §1 §2 §4 | **只改 `备注`/`证据`，⛔ 不改 `期望`**（`0.30` 是验收阈值，与 `特效还原_进度与交接.md:1640` 同源） |

**预估影响面**：4 条**全部不影响 `play check` 指纹、不动 `RuleEngine/**`、不动任何表现代码** ——
三条假红是尺子/工具的错，第四条是渲染差异（改也只碰 shader/材质/测量工具）。
**最该先做 = §4 的「补跑 exp + 还原 targets」**（一次解决 3 条，产出的基线也是 §3 的前提）；
**工具那条闸（§1-1）与它同批做**，否则下次 `EffectExporter.Run` 会再次复发。

---

## §6 顺手发现（⛔ 本代理不改，交调度台分流）

1. 🔴 **`资料/比对基线/README.md:14` 的内容已过期**：写「5 条原版有、我们 `WarpforgeVFX/Prefabs` 里没有」，
   现读 **978 个 prefab 全在**（含这 5 个），且它们的 guid 与 `WarpforgeEffectLibrary.asset` 条目**逐字对得上**。
2. 🔴 **`Prefabs/` 与 `sweep_*.tsv` 之间有 21 个效果的缺口从来没有护栏**：`EffectSweepBatch` 只在
   `targets` 里找不到时计 `skipped`，但 `targets` 本身就是从 `PrefabDir` 列出来的 ⇒ **prefab 不在了它不会跳过、只会当没这回事**，
   而 orig 那趟用的是**上一版**的清单。建议按 §4「断言要点」加一条「targets 数 == PrefabDir 文件数」的收尾断言。
3. 🔴 **`工具/fp_oracle_check.py` 的文档与实现不一致**：`:77-91` 声称 `sweep_ratio`「EXACTLY like `analyze_sweep.py`」，
   实际两处在「某一侧整个缺行」时分道扬镳（`analyze_sweep.py:158` 用 `.get(name, {})`，`fp_oracle_check.py:645-650` 直接判红）。
   **这是本批 4 条红里 3 条的产生机制**。
4. ⚠️ **`sweep_exp.tsv` 与 `sweep_exp_baseline_1031.tsv` 是两份不同的 exp 结果**（md5 不同）：`Raging Storm` 尾部 3.0 s 处 `sum` 一个 **75**、一个 **63**（差 19%）⇒ 任何「拿某一份 sweep 当绝对判据」的结论都要先问是哪一份。
5. ⚠️ **`EffectSweepBatch.ResultPathFor` 是固定路径**（`:61`）：任何「单效果两跑」不先加 `WFSWEEP_OUT`
   就会**整份覆盖** 962 行基线，而且跑完没有任何提示。现有 `_baseline_1031` 副本是唯一保险。
6. 📌 `资料/任务线/特效线/任务总表.md:17`「`Raging Storm` / 不稳定名单那几条需 Unity 单效果两跑」**仍开**，与本文件 §3 是同一件事（别另开账）。
