# P4 —— (a) `_missing_shaders_audit.py` 修过期判定 · (b) M5 尾随空格消费点审计

> 执行代理 P4 · 2026-10-22 · **没跑 Unity** · 没动 git · 没改正本 · 没碰 `EffectExporter.cs` / `WarpforgeShaderMap.cs`

## 一、结论

- **(a) 工具已修好**：`TextMeshPro/Distance Field Offset` 不再报「未解析 / 有替代 shader（近似）」，
  改为正确的「✅ 已进 `UseOriginal` 白名单（走原件）」，且 `在别的包 = wf_tmp_shaders.bundle`。
  **没有引入新的假绿** —— 其余 32 行处置逐条不变（前后日志做了 diff），`未映射：占位材质保留` 仍是 **0 条**。
- **(b) M5 消费点：`EnvironmentApplier.cs` 里 **0 个真漏点**（6 个 `Norm` 点覆盖全部路径，逐条核过）。
  但全仓另有 **1 个真漏点**，在 `EffectLibraryBuilder.cs`（不在我的白名单里，**只报不修**）：
  `animfx_modules.json` 的 `"Orbital 5 repeat "`（带尾随空格）与 `effect_index.json` 的
  `"Orbital 5 repeat"`（无空格）**逐字 join 失败** ⇒ 库条目 `Orbital 5 repeat` 的 `modules: []`。

## 二、改动清单

| 文件 | 行 | 改了什么 |
|---|---|---|
| `Unity/工具/_missing_shaders_audit.py` | `1-27`（文件头） | 把「读的 4 个源」里「只读主包+补充包两只」**就地订正**成「目录下每只包都实读」，留 2026-10-22 更正痕迹 + 错因 |
| 同上 | `20` `26-31` | `import glob`；新增 `TMP_BUNDLE` 常量 + 为什么必须有它的注释 |
| 同上 | `78-91` | 新增 `read_all_shader_bundles(vfx_dir)`：`glob` 扫 `*.bundle` 实读全部 |
| 同上 | `146-178` | `main()` 改成读全目录；新增 `[C2]`（逐包实读行数/重叠）、`[C3]`（**缺 `wf_tmp_shaders` 时出声**，⚠️ 它是 gitignore 产物）；`[D]` 加「全目录 shader 名并集」 |
| 同上 | `196-200` | `[E]`「两边都没有」→「**只在别的包** X 个 · **所有包都没有** Y 个」 |
| 同上 | `202` `241-252` | 表格加一列 `在别的包`；`elif s in extra or s in main` → `elif s in alln`（否则落在 tmp/prefabs 包的件会掉进假缺口） |
| 同上 | `262-268` | `[G]` 原来 `'原件在主包' if s in main else '原件在补充包'` 是**非此即彼的假二分**（TMP 明明在 tmp 包里却被印成「补充包」）⇒ 改成按实读命中的包名打印 |

`git diff --numstat` = **75 / 15**；行尾实测 `CRLF 0 / LF 276`（改前 216 行 LF，**没翻行尾**）。

## 三、证据

### 3·1 audit 前后对比（关键行）

跑前（`/tmp/wf_p4/before.log`）：
```
[C] wf_shaders_extra.bundle 实读 42 个 shader（与主包重叠 1）
[E] 实读：在主包 6 个 · 在补充包 26 个 · 两边都没有 1 个
TextMeshPro/Distance Field Offset  1  CardPrefab  N  N  有替代 shader：TextMeshPro/Distance Field（近似）  未解析
[G] ... TextMeshPro/Distance Field Offset -> 原件在补充包        ← 假的（它不在补充包里）
```
跑后（`d:/4/_tmp_view/p4_audit.log`）：
```
[C2] 另外实读 9 只包：wf_tmp_shaders.bundle 4 个 / wf_prefabs_extra.bundle 5 个 / wf_builtin.bundle 15 个 / ...
[E] 实读：在主包 6 个 · 在补充包 26 个 · **只在别的包** 1 个 · **所有包都没有** 0 个
TextMeshPro/Distance Field Offset  1  CardPrefab  N  N  wf_tmp_shaders.bundle  ✅ **已进 `UseOriginal` 白名单**（走原件）  未解析
[G] ... TextMeshPro/Distance Field Offset -> 原件在 wf_tmp_shaders.bundle
```
**全目录实读结果（11 只包，123 个不同 shader 名）**：`wf_shaders` 45 · `wf_shaders_extra` 42 · `wf_builtin` 15 ·
`wf_arena_shaders` 7 · `wf_arena_tauviorla` 5 · **`wf_prefabs_extra` 5** · `wf_arena_2` 4 · **`wf_tmp_shaders` 4** ·
`wf_arena_mirror` 2 · `wf_menus_extra` 2 · `wf_boosters_extra` 1。
`group`（33 件）覆盖：主包 6 · 补充包 26 · 只在别的包 1 · **所有包都没有 0**。

> ⚠️ 我**没按文件名猜**：上面每一只都是实读出来的；`wf_prefabs_extra` / `wf_arena_*` / `wf_builtin` 名字里都不带
> 「shader」，但里面确实有。

### 3·2 M5 消费点表

| # | 消费点（文件:行号） | 比较方式 | 风险 | 判据 |
|---|---|---|---|---|
| 1 | `EnvironmentApplier.cs:922` `FindPrefab(name)` → `WarpforgeEffectLibrary.TryGet` | **Ordinal 精确**（`WarpforgeEffectLibrary.cs:99,122`） | 本身不 Trim；看两个调用点 | — |
| 2 | `EnvironmentApplier.cs:159` `FindPrefab(it.prefabName)` | 直送、**未 Norm** | ✅ 当前安全 | 实测 `Resources/EnvironmentConditions.json` 55 条 `prefabName`，**0 条**带首尾空格 |
| 3 | `EnvironmentApplier.cs:638-639` `FindPrefab(Norm(leaf))` | Norm（Trim）后查 | ✅ 安全 | `EnvBlendables.json` 带尾随空格的 30 处（`path`/`leaf`/`fields.s`）全走这条 |
| 4 | `:528 FindChild` · `:673 FindDescendant` · `:746/751/766 FindNearest` | **两侧都 Norm** | ✅ 安全 | 6 个 Norm 点；`EnvBlendables` 的 `Flames `/`Floor Heat Grill 1 Helfire `/`Orbital 5 repeat `/`Orbital 5 spawner `/`Railgun Turret 1 Target ` 全覆盖 |
| 5 | `:280 AttachBlendables→EnvBlendables.ForPrefab` · `:401 groups[i].root == prefabName` | Ordinal 精确 | ✅ 当前安全 | 两侧数据源实测 **0 条**带空格（`prefabs[].root` 42 条 / `standalone[].root` / `EnvironmentConditions.prefabName`） |
| 6 | `ScenarioBlendables.cs:1257/1306/1310` | `Normalize()` = `EnvironmentApplier.Norm`（`:1329`） | ✅ 安全 | 转发同一份判据 |
| 7 | `WarpforgeEffectPlayer.cs:129-149` `Play(string)` | 精确 `TryGet`；miss **出声**（`:146`） | ✅ 安全 | 实参全是 .cs 常量或 #3 的 Norm 结果 |
| 8 | `BattleDriver.cs:10112` · `CardFeel.cs:1800` · `CardView.cs:1823/4990` · `TraitFrames.cs:382` · `TraitParticles.cs:79` · `WarpforgeEffectHook.cs:33` | 精确 `TryGet`/`Play` | ✅ 安全 | 实参 = `SummonVfx*`/`LandDust*`/`DeathBodyFx`/`FromCode` 表，全仓 `.cs` 扫过：**0 条**尾随空格的特效名字面量 |
| 9 | `VfxMap`（`ByEvent`/`ByFaction`/`ByCard`/`SharedRangedVfxDuration`/`AnimDurationOfPrefab:1269`） | 精确字典 | ✅ 安全 | 数据源 `Resources/card_vfx_by_card.json` 实测 **0 条**带空格；其余是 .cs 字面量 |
| 10 | **`EffectLibraryBuilder.cs:98` ↔ `:148`** | `modsByEffect[me.name]` 存 raw → `TryGetValue(e.name)` 查 raw | 🔴 **真漏 1 条** | `animfx_modules.json` `effects[643].name = "Orbital 5 repeat "`（带空格）；`effect_index.json` = `"Orbital 5 repeat"`（无）⇒ join miss。实证：`WarpforgeEffectLibrary.asset:63258-63269` 该条目 `modules: []`；`animfx_modules.json` 那边是 2 个模块（`AnimFXModuleScreenShake` + `AnimFXController` destroyTime 8 / exitDestroyTime 3 / 音 `Orbital Close`） |
| 11 | `WFEffectModule.ResolvePath`（`WarpforgeEffectPlayer.cs:270-275`） | 段名**逐字**比（`:189 next.name != s.name`） | ✅ 安全（已实核） | 我们导出的 prefab **保住了**子件空格：`Prefabs/Whip Attack EC Daemon.prefab:40330 m_Name: 'Whip '`、`BulletImpact_3shot_trail_EC.prefab:1152 m_Name: 'BulletImpact '` |

**量化（#10）**：`animfx_modules.json` 912 个效果名 vs 库 964 个条目名 —— 逐字比 **16 条 miss**，
其中**只有 1 条 trim 后就能对上**（`Orbital 5 repeat `）；另外 15 条是真孤儿（`Content`、`Scenario`、
`Earthquake Shake only`、`Remnant Aeldari Collect particles` … = W64 已记的 M1~M4 那一族）。
⇒ **尾随空格造成的 join 漏点，全仓就这 1 条。**

## 四、没查清 / 顺手发现

1. 🔴 **#10 只报没修**（`EffectLibraryBuilder.cs` 不在我的白名单；它也不属于 P1/P2 正在改的两个文件）。
   修法很小：`:98` 与 `:148` 两侧都 `Trim()`（或建字典时用 `me.name.Trim()`）。
   ⚠️ **修了之后会碰到第二层**：`WarpforgeEffectLibrary.asset` 的条目名是 `Orbital 5 repeat`（无空格），
   而 `animfx` 的 `__node = "Orbital 5 repeat "`（带空格）⇒ `ResolvePath` 第一段 `s0.name == root.name`
   **比不上**、会落进「当下一条子件」那条错路（打警告、但 `host` 退回根，多数模块仍能建）。
   **建议一并把 `ResolvePath` 的第一段比较也走 Trim。**
2. **M5 当前实际影响 ≈ 0**：`Orbital 5 repeat` 只经 `EnvironmentApplier` 的
   `ParticleSystemAreaSpawner` **当模板**用（`:638` 那条路拷一份私有副本），
   环境 prefab 是 `Instantiate(prefab,…)` 直接落地、**从不走 `WarpforgeEffectPlayer.Play(entry)`**
   ⇒ 模块本来就不参与。所以 #10 是**结构缺陷**、不是当前可见的画面差异；一旦有人
   `Play("Orbital 5 repeat")` 就会现形（无震屏/无音/寿命取 `natural=8` 而不是原版 `destroyTime=8`）。
3. **`Orbital 5 repeat.prefab` 的根 GameObject 名丢了尾随空格**（`:34338 m_Name: Orbital 5 repeat`），
   而 `Whip ` / `BulletImpact ` 那些**子件**保住了。⇒ 「根名」与「子件名」在这件事上行为不同，
   以后按原版名逐字比**根节点**时要注意（这次的 M5 消费点没有踩到，因为都走 Norm）。
4. **工具 `[D]` 行两次跑数值不同**（`UseOriginal 61 → 66`）是 **P2 正在改 `WarpforgeShaderMap.cs`**所致
   （`git status` 里那份是 modified），**不是我的改动**。同理 `wf_prefabs_extra.bundle` 在本次会话中途
   被重建过（3 → 5 个 shader，mtime 今天 18:10，应该是 P1 的 `EffectExporter` 跑出来的）——
   这正好印证「按文件名写死包清单会过期」，所以我把工具改成了**扫全目录**。
5. `资料/普查产出_1022/W64_素材导出缺口.md:17,79` 的「工具不扫 `wf_tmp_shaders`」那句话**现在过期了**；
   我没改那份（避免与别的代理撞车），**请主对话合并进正本时一起订正**。
