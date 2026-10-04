# 波 9 批二 · A137 —— 24 条常驻 `ParticleSystemAreaSpawner`（+3 controller）落地

> 执行代理 **A137** · 2026-10-07 · 一件活。
> 判据第一权威 = **原始 bundle 直读**（`battleprefabs_vfxandmisc_assets_all.bundle`，逐实例解 typetree）；
> 派活单指定的判据文件 = `资料/普查产出_1006/战A_第3_4条.md` 的「顺手发现 B」（**全篇读过**）。
> 反编译件只用来核「怎么用」：`ScenarioParticleSpawnerBlender__*.c` · `ParticleSystemAreaSpawner__*.c` ·
> `ParticleSystemAreaSpawnerController__*.c`（带 RVA 的两处已在 `Battle/ScenarioBlendables.cs` 的注释里）。
> 探针脚本在 `%TEMP%/wf_w9b/`（**临时目录、未入库**）：`count_all_spawners.py`（全库计数）·
> `probe_templates.py`（模板落点）· `verify_paths.py`（路径逐段复核）· `mk_table.py`（本报告的清单表）。

---

## 1. 结论

**做完了。** 旁挂加了 `standalone` 一节（**27 条 = 24 个 `ParticleSystemAreaSpawner` + 3 个 `…Controller`**），
运行时在环境 prefab 实例化之后按**同一套 `MakeSpawner` / `MakeController`** 建出来，推进权照旧收在执行器一处；
类型检查两遍全绿（0/0）；生成器两侧自检 `_unresolved` 仍是空。

### 1.1 我数出来的条数 vs 原记录

原记录（正本 A137 行 + 战A 报告「顺手发现 B」）写的是「**全库 ~28 个，我们只接了被 blender 引用的 4 个，
其余 24 条（外加 3 个 `…Controller`），它们 `useAutomaticSpawn = 1` 自启**」。

我把 aa 下**全部 84 个 bundle** 逐包扫了一遍（`count_all_spawners.py`）：

| 项 | 我数出来的 | 原记录 | 差 |
|---|---|---|---|
| `ParticleSystemAreaSpawner` 实例总数 | **28** | ~28 | 0 |
| 其中被 `ScenarioParticleSpawnerBlender` 引用（已接） | **4** | 4 | 0 |
| 其中**不被任何 blendable 引用**（本件要收） | **24** | 24 | 0 |
| `ParticleSystemAreaSpawnerController` 实例总数 | **3** | 3 | 0 |
| 它们在哪些包 | **全部 28+3 条都在 `battleprefabs_vfxandmisc_assets_all.bundle`**；场景侧 **0** | 未写 | 新增事实 |
| 24 条里 `useAutomaticSpawn = 1`（真自启） | **16** | 「24 条都自启」 | 🔴 **-8** |
| 24 条里 `useAutomaticSpawn = 0` | **8** | —— | 🔴 **+8** |

🔴 **那一处归类错了（数字对、说法不对）**：那 **8 条 `useAutomaticSpawn = 0` 的并不是「自启」**，
它们是**被那 3 个 controller 驱动**的（controller 直接调 `SpawnParticle()`，**根本不看**这个字段）——
逐条对得上：8 条 `auto=0` **全部**出现在某个 controller 的 `particleSystemAreaSpawners[]` 里，
16 条 `auto=1` 一条都不在里面。**净效果不变**（这 24 条在原版里都会出粒子），
但「24 条自启」这句话会让下一个人以为**只需建 24 个 spawner 就够了** —— 那样那 8 条永远不出粒子（静默）。
⇒ 建议把正本 A137 行的「24 条【自启】」改成「24 条（**16 自启 + 8 由 controller 驱动**）」。
（**我不能改正本**，请调度台落一下。）

### 1.2 逐条清单（原版直读）

**24 个 standalone `ParticleSystemAreaSpawner`**（`auto` = `useAutomaticSpawn`；「模板在哪」= `particleSystemPrefab` 那条引用落在哪）

| # | 环境 prefab（根） | 宿主对象 | boxSize | maxPool | auto | spawnRate | chances | 模板 `particleSystemPrefab` | 模板在哪 |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `Environmental Condition Astra Militarum Planetary Invasion` | `Explosion controller` | (500, 0, 50) | 5 | **1** | 0.333 | 0.333 | `…/Far explosion` | prefab 子树里 |
| 2 | `Environmental Condition Astra Militarum Planetary Invasion` | `Explosion controller sky` | (800, 200, 0) | 5 | **1** | 0.5 | 0.333 | `…/Explosion controller sky/Far explosion (1)` | prefab 子树里 |
| 3 | `Environmental Condition Black Legion Helfire Outburst` | `Helfire Controller` | (0, 0, 0) | 5 | **1** | 0.05 | 1 | `…/Helfire Controller/Helfire Parent` | prefab 子树里 |
| 4 | `Environmental Condition Dark Angels Asteroid Zone` | `Asteroid Spawner far` | (1, 1, 1) | 5 | **1** | 0.08 | 0.75 | `…/Asteroids crash tower` | prefab 子树里 |
| 5 | `Environmental Condition Dark Angels Asteroid Zone` | `Asteroid Spawner further` | (1, 1, 1) | 5 | **1** | 0.133 | 0.75 | `…/Asteroids crash tower further` | prefab 子树里 |
| 6 | `Environmental Condition Emperor's Children Empyric Rift OLD` | `Psychic_Lightning_down` | (200, 1, 1) | 5 | **1** | 0.13 | 0.7 | `…/Lightning Main` | prefab 子树里 |
| 7 | `Environmental Condition Emperor's Children Empyric Rift` | `Psychic_Lightning_down` | (200, 1, 1) | 5 | **1** | 0.13 | 0.7 | `…/Lightning Main` | prefab 子树里 |
| 8 | `Environmental Condition GSC Mining Tremors` | `Rockfall controller` | (0, 0, 0) | 5 | **1** | 0.2 | 0.2 | `GSC Rockfall` | 🔴 **另一件独立 prefab** |
| 9 | `Environmental Condition Necrons Earthquake` | `Rockfall controller` | (0, 0, 0) | 5 | **1** | 0.2 | 0.2 | `Rockfall` | 🔴 **另一件独立 prefab** |
| 10 | `Environmental Condition Particles Orbital` | `Orbital 3 spawner` | (250, 1, 50) | 5 | **0** | 0.73 | 0.5 | `Orbital 3 repeat` | 🔴 **另一件独立 prefab** |
| 11 | `Environmental Condition Particles Orbital` | `Orbital 4 spawner` | (200, 1, 50) | 5 | **0** | 0.52 | 0.4 | `Orbital 4 repeat` | 🔴 **另一件独立 prefab** |
| 12 | `Environmental Condition Particles Orbital` | `Orbital 5 spawner ` | (150, 1, 10) | 5 | **0** | 0.5 | 0.3 | `Orbital 5 repeat ` | 🔴 **另一件独立 prefab** |
| 13 | `EnvironmentalCondition Leviathan Blazing Biomatter` | `Meteor Pooling controller/Meteor spawner` | (20, 1, 1) | 5 | **0** | 0.33 | 1 | `Meteor Angled` | 🔴 **另一件独立 prefab** |
| 14 | `EnvironmentalCondition Leviathan Blazing Biomatter` | `Meteor Pooling controller/Meteor spawner (1)` | (20, 1, 1) | 5 | **0** | 0.66 | 1 | `Meteor Angled` | 🔴 **另一件独立 prefab** |
| 15 | `EnvironmentalCondition Saim Hann Infinity Circuit Overload` | `Pooling controller/Area Spawner` | (0.1, 0.1, 0.1) | 5 | **1** | 0.18 | 0.6 | `…/Area Spawner/Lightning burst` | prefab 子树里 |
| 16 | `EnvironmentalCondition Sororitas Shrine Bombardment` | `Psychic_Lightning_down` | (0.01, 0.01, 0.01) | 5 | **1** | 0.1 | 0.6 | `…/Psychic_Lightning_down/Explosion Left` | prefab 子树里 |
| 17 | `EnvironmentalCondition Ultramarines Aerial Clash` | `Bullet far controller` | (40, 1, 1) | 5 | **1** | 0.3 | 0.3 | `…/Bullet far controller/Strafing Runs Bullets 1 (1)` | prefab 子树里 |
| 18 | `EnvironmentalCondition Ultramarines Aerial Clash` | `Bullet far controller (1)` | (5, 5, 5) | 5 | **1** | 0.22 | 0.25 | `…/Bullet far controller (1)/Strafing Runs Bullets 1 (1)` | prefab 子树里 |
| 19 | `EnvironmentalCondition Ultramarines Aerial Clash` | `Missile Controller` | (1, 1, 1) | 5 | **1** | 0.15 | 0.5 | `…/Missile Controller/Missile Particle` | prefab 子树里 |
| 20 | `EnvironmentalCondition Ultramarines Aerial Clash` | `Shadow controller (1)` | (2, 0, 2) | 5 | **1** | 0.1 | 0.3 | `…/Shadow controller (1)/Thunderhawk_shadow` | prefab 子树里 |
| 21 | `EnvironmentalCondition Ultramarines Aerial Clash` | `Shadow controller (2)` | (2, 0, 2) | 5 | **1** | 0.1 | 0.21 | `…/Shadow controller (2)/Thunderhawk_shadow` | prefab 子树里 |
| 22 | `EnvironmentalCondition Ultramarines Bombardment` | `Enviromental Condition Particles Orbital/Orbital 3 spawner` | (250, 1, 50) | 5 | **0** | 0.73 | 0.5 | `Orbital 3 repeat` | 🔴 **另一件独立 prefab** |
| 23 | `EnvironmentalCondition Ultramarines Bombardment` | `Enviromental Condition Particles Orbital/Orbital 4 spawner` | (200, 1, 50) | 5 | **0** | 0.52 | 0.4 | `Orbital 4 repeat` | 🔴 **另一件独立 prefab** |
| 24 | `EnvironmentalCondition Ultramarines Bombardment` | `Enviromental Condition Particles Orbital/Orbital 5 spawner ` | (150, 1, 10) | 5 | **0** | 0.5 | 0.3 | `Orbital 5 repeat ` | 🔴 **另一件独立 prefab** |

**3 个 standalone `ParticleSystemAreaSpawnerController`**

| # | 环境 prefab（根） | 宿主对象 | spawnRate | startOnEnable | `particleSystemAreaSpawners[]` |
|---|---|---|---|---|---|
| 1 | `Environmental Condition Particles Orbital`（**整件没导，见 §3**） | `Environmental Condition Particles Orbital` | 1 | 1 | 3 条：w=0.33/c=0.3 ×3 |
| 2 | `EnvironmentalCondition Leviathan Blazing Biomatter` | `Meteor Pooling controller` | 0.3 | 1 | 2 条：w=0.25/c=0.5 · w=0.4/c=0.3 |
| 3 | `EnvironmentalCondition Ultramarines Bombardment` | `Enviromental Condition Particles Orbital` | 1 | 1 | 3 条：w=0.33/c=0.3 ×3 |

**按根汇总（13 个根）**：AstraMilitarum Planetary Invasion 2 · Black Legion Helfire Outburst 1 ·
Dark Angels Asteroid Zone 2 · Emperor's Children Empyric Rift 1 · Empyric Rift OLD 1 · GSC Mining Tremors 1 ·
Necrons Earthquake 1 · **Particles Orbital 4**（未导）· **Leviathan Blazing Biomatter 3**（2+1）· Saim Hann Infinity
Circuit Overload 1 · Sororitas Shrine Bombardment 1 · Ultramarines Aerial Clash 5 · **Ultramarines Bombardment 4**（3+1）。

**两个关键分布**（都直接决定实现）：
* `useAutomaticSpawn`：**16 / 8**（见 §1.1）。
* 模板落点：**14 条在环境 prefab 子树里** · **10 条在「同一 bundle 里另一个 prefab 根」上**
  （`Orbital 3/4/5 repeat` · `Meteor Angled` · `Rockfall` · `GSC Rockfall`）。
  原版那条引用是**跨 prefab 的对象引用** ⇒ 我们那两条旧的解析路（层级路径 / 子树按叶子名）**必然落空**。

---

## 2. 证据

### 2.1 旁挂新增的那一节长什么样

`数据/游戏数据/env_blendables.json` 多一个**顶层键 `standalone`**（= `{根名: [条目]}`），
`MyGame/Assets/Resources/EnvBlendables.json` 多一个 **`standalone` 数组**（= `[{root, items}]`）。
**形制与 `prefabs` 完全一样**（`EnvBlendables.Group/Item/Target/TargetField` 原样复用），
每个条目**只有一个 target = 组件自己那个 GameObject**，`kind` 仍是 `spawner` / `controller`，
6 个（controller 是 2 个 + 摊平的 3 组）字段在 `targets[0].fields`：

```json
{ "cls": "ParticleSystemAreaSpawner",
  "owner": "EnvironmentalCondition Ultramarines Bombardment/Enviromental Condition Particles Orbital/Orbital 3 spawner",
  "ownerLeaf": "Orbital 3 spawner", "ownerPos": [100.0, 86.8, 156.5892], "fields": {},
  "targets": [ { "path": "…/Orbital 3 spawner", "leaf": "Orbital 3 spawner", "pos": [...],
                 "kind": "spawner",
                 "fields": [ {"k":"boxSize.x","f":250.0}, {"k":"boxSize.y","f":1.0}, {"k":"boxSize.z","f":50.0},
                             {"k":"particleSystemPrefab","s":"Orbital 3 repeat"},
                             {"k":"maxPoolSize","f":5.0}, {"k":"useAutomaticSpawn","f":0.0},
                             {"k":"spawnRate","f":0.7300000190734863}, {"k":"chances","f":0.5} ] } ] }
```

`…Controller` 的**嵌套那一层也收了**（A136 报告里记的「没收」这条缺口，本件补上）——
摊平成带下标的键，用**现成的 `TargetField`** 装（不新开类型，因为 `Core/EnvBlendables.cs` 不在本件白名单里）：

```json
{ "cls": "ParticleSystemAreaSpawnerController",
  "owner": "EnvironmentalCondition Ultramarines Bombardment/Enviromental Condition Particles Orbital",
  "targets": [ { "kind": "controller", "fields": [
      {"k":"spawnRate","f":1.0}, {"k":"startOnEnable","f":1.0},
      {"k":"spawner.0","s":"…/Orbital 3 spawner"}, {"k":"weight.0","f":0.33}, {"k":"chances.0","f":0.3},
      {"k":"spawner.1","s":"…/Orbital 4 spawner"}, {"k":"weight.1","f":0.33}, {"k":"chances.1","f":0.3},
      {"k":"spawner.2","s":"…/Orbital 5 spawner "}, {"k":"weight.2","f":0.33}, {"k":"chances.2","f":0.3} ] } ] }
```

**为什么另开一节、不折进 `prefabs`**（这一条是我做的设计决定，写清楚）：
① 这些条目**不是 blendable**，混进 `EnvBlendables.ForPrefab` 会让 `ScenarioBlendableFactory.Create`
打「不在工厂的表里」的**假警报**；② 更重要的：`BattleScene.cs:1318` 那条既有断言
`Check(nbPf == 42 && nbSc == 13)`（`EnvBlendables.Counts`）数的是**组数** —— 折进去会让根数变成 43、**当场变红**。
③ `Core/EnvBlendables.cs`（`File` 那个类型）在白名单外 ⇒ 运行时**读不了**折进去之外的新键；
所以 `EnvironmentApplier` 里用**同一套 `EnvBlendables.Group`** 接一个新顶层字段
（`[Serializable] class StandaloneFile { public EnvBlendables.Group[] standalone; }`，**不另定义 DTO**）。
代价是 `Resources/EnvBlendables.json`（663 KB）被**解析两遍**（各一次、都有缓存）—— 如实记。

**生成器的自检**（`--check` 全程跑过，`_unresolved` 仍为空）新加三条
（`check_standalone`）：宿主对象在 prefab 文本里在不在 · `particleSystemPrefab` **两条路至少通一条**
（prefab 子树里 **或** 工程里另一件独立 prefab）· controller 的 `spawner.<i>` 必须落在**同一个根**的 standalone 里。
`stats` 多 6 个键：`standalone_roots 13` / `standalone_roots_ok 12` / `standalone_items 27` /
`standalone_spawners 24` / `standalone_controllers 3` / `standalone_missing_roots 1`；
整件没导的根进**新的 `_standaloneMissing`**（与 `_unresolved`、`_missingTargets` 三分开）。

### 2.2 运行时建了几颗 / 怎么保证「不少建」

`EnvironmentApplier.BuildStandaloneSpawners(实例, prefabName)` 在 `Apply()` 里紧跟在
`AttachBlendables(...)` 之后（**原版那一刻 prefab 刚实例化、这批组件正 `OnEnable`**）：

1. 从旁挂 `standalone` 里取 **`root == prefabName`** 那一组（取不到 = 这件本来没有，**不出声**，多数件如此）；
2. **先建 spawner**（`MakeSpawner(tr, 实例, t)` —— 与 blender 那条路**同一个函数**），**再建 controller**
   （`MakeController` 按 `spawner.<i>` 的路径回头取**刚建出来那个组件**填 `particleSystemAreaSpawners[]`）；
3. 建不出来的**逐条点名**进 `StandaloneMissed`（public，自检直接读）；
   计数进 `InstanceSpawnerCount` / `InstanceControllerCount`；
4. 推进：`AdvanceBlendables(dt)` 里多一句 `AdvanceSpawners(_curSpawners, _curControllers, dt)`，
   正在淡出的旧实例走 `FadeOut.spawners/controllers`（**不推的话撤环境那一程它们会停摆 —— 静默**）。

**本件这一程（`BattleScene.Run`）实际会建多少**：那一程当前的环境是
`OffensiveCards.Choices("Ultramarines")[1]` = 卡槽 0 = **`EnvironmentalCondition Ultramarines Orbital Bombardment`**
⇒ prefab `EnvironmentalCondition Ultramarines Bombardment` ⇒ 旁挂那 4 条 ⇒ **3 spawner + 1 controller**
（三条 spawner 全是 `useAutomaticSpawn=0`、模板全是**外部 prefab**）。
**静态可核的三条前提我都逐条核过**：

| 前提 | 怎么核的 | 结果 |
|---|---|---|
| 宿主对象在那个 prefab 的**真实父链**上走不走得通 | `verify_paths.py`：解我们那份 `.prefab` 的 YAML Transform 父链，逐段比 | **23/23 命中**（唯一没核的 4 条是整件没导那个根）；`Orbital 5 spawner ` 的**尾随空格**靠 `Norm()` 归一命中 |
| 外层那 4 条旁挂能不能按路径取到 | 同上 | ✓ |
| 10 条**外部模板**取不取得到 | `Orbital 3/4/5 repeat` · `Meteor Angled` · `Rockfall` · `GSC Rockfall` 这 6 个名字：`WarpforgeEffectLibrary.asset` 的 `entries[].name` **6/6 有**（该资产 966 条）；`WarpforgeVFX/Prefabs/<名>.prefab` **6/6 在盘上**，且**每件的 GameObject 名与 prefab 名逐字相同**（`Orbital 5 repeat.prefab` 的**文件名**已无尾随空格，库里那条名字也是无空格版） | ✓ |

### 2.3 新断言（`Editor/BattleScene.cs`，**另开一节**，`@@ -1326,0 +1327,154 @@`，**纯插入 0 删除**）

位置：紧跟在既有那三条 blendable 断言之后、`if (envIt != null && ap0 != null)` **之前**
（必须在这儿：再往后 `c1` 就把环境换掉了，那一段的当前实例就不再是 Bombardment）。

```csharp
// ============================================================
// 🆕 2026-10-07 波9批二（A137）**独立一节**：环境 prefab 里那批**常驻生成器**
//   （`ParticleSystemAreaSpawner` / `…Controller`；**不是** blendable，旁挂单开 `standalone` 一节）。
// 判据 = **原版 bundle 直读**（`工具/gen_env_blendables.py` 的 `collect_standalone`，逐条清单
//   在 `资料/普查产出_1007/波9批二_A137_自启spawner.md`）：略
// ============================================================
{
    int nStan = CardPresentation.EnvironmentApplier.StandaloneDataCount();
    Check(nStan == 27, $"★ 旁挂 `standalone` 一节在位：{nStan} 条（原版直读 = 24 spawner + 3 controller；-1 = 那一节没读到 / 解析失败）…");
    if (ap0 != null)
    {
        Check(ap0.InstanceSpawnerCount == 3 && ap0.InstanceControllerCount == 1, "★ …实例上建出了 3 spawner + 1 controller…一条都不许少建");
        Check(ap0.StandaloneMissedCount == 0, "★ 一条都没漏（建不出时**点名**，不静默跳过）");
        // …按名字挑出 Orbital 3/4/5 spawner 三个组件 + controller…
        Check(bSp.Length == 3 && sp3 != null && sp4 != null && sp5 != null, "★ 三条 `Orbital N spawner` 都建在正确的宿主上…");
        Check(sp3.maxPoolSize == 5 && !sp3.useAutomaticSpawn && boxSize≈(250,1,50) && spawnRate≈0.73 && chances≈0.5,
              "★ `Orbital 3 spawner` 的 6 个字段 = **原版实读值**…");
        Check(!sp4.useAutomaticSpawn && !sp5.useAutomaticSpawn, "★ 另两条也是 `useAutomaticSpawn=false` —— 原版这 8 条**由 controller 驱动**，**不能**自己起循环…");
        Check(sp3.particleSystemPrefab != null, "★ **外部模板解出来了**：`particleSystemPrefab` 指的是**同一 bundle 里另一个 prefab 根**…");
        Check(!sp3.particleSystemPrefab.gameObject.activeSelf, "★ 模板按原版那句 `SetActive(false)` 关着（判据 = 反汇编 `RVA 0x675E10`）…");
        Check(ctl.startOnEnable, "★ controller 建出来了、`startOnEnable` 是**真**（原版 3/3 都是 1 ⇒ 自启）");
        Check(defsOk /* 3 条定义 · 每条都指到实例里那颗组件 · weight 0.33 / chances 0.3 */,
              "★ controller 的 `particleSystemAreaSpawners[]` **这一层也收了**…不收这层的话那 8 条 `auto=0` 的 spawner 永远不出粒子（**静默**）");
        int was = sp3.transform.childCount; sp3.SpawnParticle();
        Check(sp3.transform.childCount == was + 1, "★ `SpawnParticle()` 真的把模板 `Instantiate` 出来（子件 was → +1）…");

        // ② 同一个 GameObject 上两条 spawner 各是各的组件（见 §2.4 的「一律新建」）
        //    —— 临时 Apply(Sororitas Shrine Bombardment) 验一次，再 Apply 回 c0 + 把淡出推完
        Check(dup.Length == 2 && dup[0].transform == dup[1].transform && was013 && was010
           && bl.areaSpawners[0].spawnRate ≈ 0.13, "★ 同一个 GameObject 上那两条 **各是各的组件、字段没互相覆盖**…");
        Check(ap0.FadingCount == 0 && ap0.CurrentSO == c0.envSO && ap0.InstanceSpawnerCount == 3, "★ 验完还原…");
    }
}
```

（上面是压缩摘录，**逐字全文**在 `Editor/BattleScene.cs:1327-1480`；行数 154。）
其中 `StandaloneDataCount()` 是 `EnvironmentApplier` 新开的 public static
（「旁挂那一节读到了没有 + 一共几条」，与 `EnvBlendables.Counts` 同一族用法）。

### 2.4 撞到的既有断言：**逐条判「是断言过时，还是我改错了」**

| 既有断言（**现读**行号，本件改完之后） | 会不会红 | 判 |
|---|---|---|
| `BattleScene.cs:1318` `Check(nbPf == 42 && nbSc == 13)` | **不会** | 我**故意**没折进 `prefabs`（见 §2.1 的理由 ②）⇒ `EnvBlendables.Counts()` 读到的还是 42/13。**不是断言过时，是我选了不碰它** —— 跑了生成器后 `stats.prefab_roots=42`、`scene_arenas=13` 逐字未变 |
| `BattleScene.cs:1321` / `:1324` `InstanceBlendableCount > 0` / `SceneBlendableCount > 0` | **不会** | 那三个计数只数 `ScenarioBlendable`，我的组件不是 blendable，**不参与** |
| `BattleScene.cs:8320`「战场里没有材质没贴图的粒子」 | **不会** | 它读的是 **arena prefab 资产**（`arenaContent`），不是环境实例；我的新对象全在环境实例里 |
| `BattleScene.cs:1485/1487/1489` `state=0 ⇒ off>0` / `state=1 ⇒ off==0`（`CountEmittersOff`） | **不会**（静态推断） | 本程那一条是 `defaultScenarioObjectsState = 0` ⇒ 判据是 `off > 0`（单调）。而且我新加的东西**都不改变任何 `emission.enabled`**：① 3 份外部模板副本**不活跃**（`SetActive(false)`）但 `emission.enabled` 仍是 1（逐条核过 `Orbital 3/4/5 repeat.prefab` 的 `EmissionModule.enabled` **全是 1**）⇒ `FindObjectsOfType(...,true)` 找得到它们、但**不计入 off**；② `SpawnParticle()` 那一颗的 emission 也是 1；③ 模板被 `SetActive(false)` **不改** `emission.enabled`。⚠️ **这一条我没实跑，是静态推断**（见 §3.1） |
| `BattleScene.cs:1494` `Check(ap0.FadingCount == 1)` / `:1497` `== 0` | **不会** | `pending` 只数 `NotifiesComplete` 的 **blendable**；我的 spawner/controller **不参与计数**，只是**跟着一起被推**（`AdvanceSpawners`）。⚠️ 我为 §2.3 ② 那条断言**临时换过一次环境**，所以**必须**在换回来后 `Apply(envIt,...)` + `AdvanceBlendables(1000f)` 把淡出推干净 —— 代码里已经这么写了（并配了一条「验完还原」的断言），否则这里会从 1 变成 2 |
| `BattleScene.cs:1496` 那一带 `AdvanceBlendables(600f)` 之后那几条 | **不会**（静态推断） | 那一下会推正在淡出的 Bombardment 实例上的 3 spawner（`auto=0` ⇒ `Advance` 直接返回）与 1 个 controller（最多出 1 颗，`Instantiate` 出来的副本随实例当场销毁）|

**结论：一条「编码了旧行为的过时断言」都没找到** —— 唯一真会撞的是 `:1318`，而我用「另开顶层键」避开了它。
🔴 但我**没跑 Unity**，上表后两行的「不会」是**静态推断**，请以同步点那次 `BattleScene.Run` 为准。

### 2.5 两处必须改的既有行为（改了才不静默）

1. **`MakeSpawner` / `MakeController` 从「复用同 object 上已有的组件」改成「一律新建」**。
   原因：原版**真的会在同一个 GameObject 上挂两个** `ParticleSystemAreaSpawner` ——
   实测 `EnvironmentalCondition Sororitas Shrine Bombardment/Psychic_Lightning_down` 就是
   （GO pid `4562059142910105967`，`m_Component` 里两个 spawner pid：`5279182412443867503` 被 blender 引用 / `-7732775592107583121` standalone；
   两条的 `boxSize`/`spawnRate`/`chances`/模板都不同）。
   写成 `GetComponent() ?? AddComponent()` 的话，后建那条会 `Configure` 到**先建那个组件**上
   ⇒ 先建那条的 6 个字段被**静默覆盖**（只在同 object 双组件时现形）。§2.3 的第 ② 条断言就是盯它。
2. **`FindTemplatePS` 加第三条兜底：外部模板**（10/24 条）。前两条路（层级路径 / 子树按叶子名）
   对那 10 条**必然落空**（那条引用落在**另一个 prefab 根**上）。走 `MakeTemplateCopy`：
   `Instantiate` 一份**挂在 spawner 下的私有副本**当模板。
   🔴 为什么不能直接把 `Resources` 取到的那件 prefab 资产当模板：`ParticleSystemAreaSpawner.OnEnable`
   会**无条件** `particleSystemPrefab.gameObject.SetActive(false)`（反汇编 `RVA 0x675E10`），
   而 `Resources.Load` 拿到的是**全进程共享的那份资产** ⇒ 会把**别的系统也在用的 prefab 弄成关闭态**（静默、跨系统）。
   原版没这个问题（它那条引用是 bundle 里的独立资产、只归这个 spawner 用）。
   ⚠️ **有意的偏离**：这比原版**多一个场景对象**（原版那份资产不在场景里，我们多一份不活跃副本）—— 已写进代码注释。

---

## 3. 没查清的部分

1. 🔴 **没跑 Unity**（红线）⇒ 「运行时到底建出几条、会不会把既有断言带红」**只有静态推断**。
   静态可核的三条前提都核过了（§2.2 那张表：路径 23/23 · 库名 6/6 · 盘上 6/6），
   但**没有实跑证据**。请调度台在同步点跑 `BattleScene.Run`。
2. ⚠️ **观感改不了、批处理里量不了**：这 24 条是**常驻粒子**（`spawnRate` 0.05~0.73、`chances` 0.2~1），
   接上后那 13 件有这一族的环境 prefab 会**多出随机粒子**。**判据不在批处理里** ⇒
   请把「常驻粒子多出来之后像不像原版」挂到 `资料/真Play待验清单.md`（**⛔ 我没自己去改那个文件**）。
3. `Orbital 5 spawner `（尾随空格）这类名字靠 `Norm()` 归一命中 —— 静态核过，**实跑未验**。
4. **`ParticleSystemAreaSpawner` 用 `UnityEngine.Random`**（原版就是它）⇒ 粒子出现的位置/时序
   **不可复现**（A136 已记）。要 A/B 图得连它一起固定。
5. **不在本件范围、我也没碰**：`_missingTargets`（5 条，A191）· A192（两个 clip）·
   `ScenarioAnimationBlend` / `TauCannonAnimationStopper` 的任何事 · 整件没导的
   `Environmental Condition Particles Orbital` 要不要导（见 §4·A）。

---

## 4. 顺手发现（**没自己动手改**，交调度台分流）

### A. 🔴 我们那 961 件 prefab 里**一条 `ParticleSystemPoolable` 都没有**，原版有 **30 条**

* 判据：扫 `battleprefabs_vfxandmisc_assets_all.bundle` 的 `m_Script` → 类名，
  `ParticleSystemPoolable` **30 个实例**，落点是**模板粒子自己那个 GameObject**
  （`Orbital 3/4/5 repeat` · `Meteor Angled` · `Rockfall` · `GSC Rockfall` · `Lightning Main` ·
  `Thunderhawk_shadow` · `Missile Particle` · `Explosion Left/Right` · `Strafing Runs Bullets 1 (1)` …）。
* 我们这边：`WarpforgeVFX/Prefabs/*.prefab` **grep `ParticleSystemPoolable` = 0 命中**（也没有 `m_Script: {fileID: 0}` 那种缺失脚本占位）。
  根因大概是**导入那批 prefab 时这个类还不存在**（它是 2026-10-06 才移植进来的）⇒ Unity 把那个组件丢了。
* **行为等价、但会刷假警报**：`ParticleSystemAreaSpawner.CreatePooledItem` 拿不到就
  `Debug.LogError("粒子 prefab 上没有 ParticleSystemPoolable（回收那一跳靠它）—— 照原版就地补一个")` + `AddComponent`。
  原版 `.c` 里那条分支本来就在，说明它**容忍**这种情况 ⇒ **不出错，只是每建一颗粒子刷一条 LogError**。
  ⚠️ 这条**不是本件引入的**：A136 那 4 条 spawner 今天就已经会刷（`AdvanceBlendables(600f)` 那一处）。
* **两条修法，都请调度台裁**：
  (i) **资产侧**：重新导入/补挂那 30 处 → 动的是 `WarpforgeVFX/Prefabs/**` 与导入工具（**本件白名单外**）；
  (ii) **运行侧**：`MakeSpawner` 解析到模板后 `AddComponent<ParticleSystemPoolable>()` +
  `AssignParticleSystemReference(template)`（只动 `EnvironmentApplier.cs`，**但会同时改 A136 那 4 条的行为** ⇒ 不是本件该单方面决定的事）。
  我**没有**自己动手 —— 它同时越「本件范围」和「A136 的路径」。

### B. 整件没导的 `Environmental Condition Particles Orbital`（4 条 standalone 落在里面）

`Particles Orbital` 是原版的**公共件**，我们**有意没导**（判据 → `资料/战场场景线_交接.md:289`、
`资料/普查产出_0929/进攻卡_数据表.md:30`：它被别的 prefab **内联成副本**用，
`EnvironmentalCondition Ultramarines Bombardment` 里那 3 条就是内联副本，**各自独立、能建**）。
⇒ 那 4 条（3 spawner + 1 controller）**运行时永远不会被走到**（我们不会实例化那件 prefab）。
已进旁挂新键 `_standaloneMissing`（**不是** `_unresolved`），生成器每次都点名打印。**要收就得把公共件导进来**（另开一件）。

### C. 正本 A137 行那句话的归类要改（见 §1.1）

「24 条【自启】」→「24 条（16 自启 + 8 由 controller 驱动）」。**我没资格改正本。**

---

## 5. 改动清单

| # | 文件 | 改前 → 改后 | `git diff --numstat` |
|---|---|---|---|
| 1 | `Unity/工具/gen_env_blendables.py` | ＋ `STANDALONE_CLASSES` / `CONTROLLER_ARRAY` · `Bundle.pack_controller_defs` · `Bundle.collect_standalone` · `check_standalone`（三条自检）· `main()` 里收 standalone（prefab 侧 + 场景侧那道「出现了就出声」的闸）· `stats` 6 个新键 · dev 侧 `standalone` / `_standaloneMissing`、flat 侧 `standalone` · `flat_item` 的 `it['fields']`→`it.get('fields')` | **本件 +203 / −4**（读数是含 A136/A135 的 **494 / 23**，A136 收工时是 291 / 19） |
| 2 | `Unity/MyGame/Assets/CardPresentation/Battle/EnvironmentApplier.cs` | ＋ `StandaloneFile`/`StandaloneGroups`/`StandaloneDataCount`/`OwnTarget`/`FindStandaloneHost`/`BuildStandaloneSpawners`/`AdvanceSpawners`/`MakeTemplateCopy` · `MakeSpawner` 改「一律新建」· `FindTemplatePS` 加第三条兜底 · `MakeController` 加 `prefabRoot` 参数 + 收 `particleSystemAreaSpawners[]` · `FindControllerInPrefab/Scene` 跟着改签名 · `Apply` 里调 `BuildStandaloneSpawners` · `AdvanceBlendables` 推新组件 · `FadeOut` 加两个字段 · 4 个新 public 计数/名字 · 头注释 | **本件 +291 / −26**（读数是含 A136/A135 的 **439 / 50**，A136 收工时是 148 / 24） |
| 3 | `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs` | **只加**：`@@ -1326,0 +1327,154 @@` 一节 154 行（0 删除）。**没动**波4件② 的二分四条边段 / 手柄那件的弱断言段 / A193+A197 的 `:5598-5629` 段 | **本件 +154 / −0**（读数是 **314 / 2**，A136 收工时是 160 / 2） |
| 4 | `Unity/数据/游戏数据/env_blendables.json` | 重跑生成器（非手改）：＋顶层 `standalone`（13 根 / 27 条）· `_standaloneMissing` · `stats` 6 键 · `_schema`/`_sources` 各一段 | A136 收工时 **141 / 5** → 现在 **2119 / 19** |
| 5 | `Unity/MyGame/Assets/Resources/EnvBlendables.json` | 同上（摊平版）：＋ `standalone` 数组 | A136 收工时 **136 / 0** → 现在 **2082 / 1** |
| ＋ | `Unity/资料/普查产出_1007/波9批二_A137_自启spawner.md` | 本报告（新建） | —— |

* **行尾**：改完二进制数过 —— `EnvironmentApplier.cs` **CRLF 0 / LF 912**（纯 LF，未翻）·
  `BattleScene.cs` **CRLF 9388 / LF 9388**（纯 CRLF，未翻）· `gen_env_blendables.py` **CRLF 0 / LF 918** ·
  两份 JSON **CRLF 0**。全程用 Edit 工具 / python `wb`，**没用 `sed -i`**。
* **类型检查**：`TMPDIR=/tmp/wf_w9d bash 工具/typecheck.sh` → 运行时 **0** / 编辑器 **0**（改完每个 `.cs` 都跑过，最后一次也在收尾）。
* **没跑 Unity / 没动 git / 没改两张正本 / 没越白名单**（`Core/EnvBlendables.cs` 我一个字都没动 ——
  `git diff --numstat` 里它的 58/20 是 A135/A136 留下的，不是我的）。

### 5.1 「怎么改坏就红」

| 改坏的方式 | 哪条断言会红 |
|---|---|
| 生成器不再收 `standalone`（或运行时读不到这一节） | `nStan == 27` 那条（`-1` 或 0）；同时 `StandaloneGroups()` 会打 `LogError` |
| 运行时少建 / 静默跳过 | `InstanceSpawnerCount == 3 && InstanceControllerCount == 1` 与 `StandaloneMissedCount == 0` |
| 6 个字段抄错（`boxSize` / `spawnRate` / `chances` / `maxPoolSize` / `useAutomaticSpawn`） | `Orbital 3 spawner 的 6 个字段 = 原版实读值` 那两条（`auto` 抄成 1 单独有一条） |
| 外部模板那条兜底被删 | `外部模板解出来了`（`particleSystemPrefab != null`）→ 连带 `模板 SetActive(false)` 与 `子件 +1` 两条 |
| `MakeSpawner` 改回 `GetComponent() ?? AddComponent()` | `同一个 GameObject 上那两条各是各的组件`（临时换到 `Sororitas Shrine Bombardment` 验的那条） |
| controller 不再收 `particleSystemAreaSpawners[]` | `这一层也收了`（3 条定义 / 指到实例里那颗 / 0.33 & 0.3） |
| `AdvanceBlendables` 里不再推新组件 | 不直接红（没有「跑一段时间后粒子数」的断言）；**但 `SpawnParticle` 那条只验「池子通」**—— 这一格我如实标：**驱动器这一处目前没有直接断言**（批处理里时间不可控） |
| 旁挂折回 `prefabs`（把 standalone 当 blendable 收） | `nbPf == 42 && nbSc == 13`（**这条正是我用来防它的**） |

### 5.2 该跑哪几条自检（供同步点参考）

改动落在**对战那一面**（`EnvironmentApplier.cs` + 两份 `Resources/EnvBlendables.json`；
`Battle/ScenarioBlendables.cs` **一个字节都没动**）。按铁律 12 的三条判据：
只动一个宿主 ⇒ **跑 `BattleScene.Run` 一条**。
⚠️ 判绿红看**断言合计**（不看退出码、别用 `grep -c ✗`），本件让断言**总数增加 10 条左右**。
⚠️ 跑之前先知道两件事（免得把预期行为当缺陷）：
① 那 13 件环境 prefab 从此**有常驻随机粒子**了 ⇒ 日志里会多出
   `[Env] <prefab> 的常驻生成器：旁挂 N 条 → 建出 spawner a + controller b`（**Info，不是警告**）；
② 会多出若干条 `[EnvSpawner] … 粒子 prefab 上没有 ParticleSystemPoolable …`——**见 §4·A**，
   是**已知的**资产缺口（A136 那 4 条今天就在刷），**不是本件引入的**。
