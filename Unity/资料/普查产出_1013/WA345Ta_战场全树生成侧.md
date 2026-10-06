# WA345-T-a · 战场全树生成侧

> 写手：执行代理（A345 的 **T-a** 半边）· 2026-10-13 · **纯 python，一次 Unity 都没跑**
> · 没动 git · 没改两张正本 · 没碰任何 `.cs`（`ArenaBuilder` 一个字节没动 —— 那是 T-b）
> 文件：`d:/4/Unity/工具/gen_arena_groups.py`（改）· 13 份 `arenas/<场>/<场>_groups.json`（重生成）
> 判据：`资料/普查产出_1013/A表现核_块5.md` §A345 · `资料/普查产出_1011/W11_子8b.md` §五·1

---

## 一、结论（覆盖了哪几层、哪些场）

**`wanted_paths()` 已改成【全覆盖】，13 场全跑、13 份产物全出。**
覆盖定义（一句话，可复算）：

> **原版场景里，凡是「我们清单里会真建出来的对象」+「旁挂点名的那几个宿主」，
> 它自己、它的整条祖先链、它的整棵子树，全部进那棵树。**
> · 已经建出来的 ⇒ `targets[]`（按原版父路径**改挂**，`SetParent(…, true)` 世界位姿不变）
> · 原版有、我们没建的 ⇒ `nodes[]`（**新建空节点**）

四条来源（都不手抄）：

| # | 来源 | 贡献 |
|---|---|---|
| ① 🆕 | `<场>_manifest.json` 里**每一条会建出来的**网格 / 粒子 / 灯 / 相机 | 主体（13 场 × 平均 100 条） |
| ② 🆕 | ① 的**祖先链**（`Scenario` · `Scenario/Particles` · `<场> Baked` 这些容器层） | 那些「原来平铺」的层就是这么补回来的 |
| ③ 🆕 | ① 的**子树**（原版有、我们没建的子孙 ⇒ 空节点） | |
| ④ | 旁挂两样（**A191 原样保留**）：`_missingTargets[].wantedPath` · `kind=="lookat"` 的 `target` 字段 | tau 那两件 `Railgun Turret N Target ` 仍靠它 |

**覆盖到了哪几层**：`Scenario` / `Scenario/Particles` / `Scenario/Particle Effects` / `Scenario/<场> Baked` /
`Scenario/Battle Arena 2 Particles`（…逐场不同）**全部成为真节点**，不再是平铺；
另有 3 场（`astramilitarum` / `blacklegion` / `emperorschildren`）的战场根**不在 `Scenario` 下**，
而是**场景根级**的 `<场> Baked` —— 它们也按原样建（`Battle Arena Astra Militarum Baked` 等）。
⚠️ 顺带：13 场都会长出 `BattlePrefab/…` 两级（原因见 §二末「两条边界」）。

**规模**：**13 场 · 1427 条路径 = 新建节点 263 + 改挂对象 1164**（清单 1295 件网格/粒子 → 见 §三 对账）。

---

## 二、`wanted_paths()` 改动前后对照（旧规则 → 新规则）

### 2.1 函数本身

| | 旧 | 新 |
|---|---|---|
| 签名 | `wanted_paths(blend) → (dict[场→set(路径)], anims)` | `wanted_paths(blend, arena, sc, manifest, qoff, tally, stats) → (want, built_paths)` |
| 输入 | **只有旁挂**（`_missingTargets` + lookat） | **清单 + 旁挂**（清单是主路） |
| 场的名单 | 由旁挂决定（`for arena in sorted(want)`） | 由**清单**决定（`有 <场>_manifest.json 的场` = 13 场） |
| 找对象 | 按 `by_name` + 拼父链逐条比 | 新的 `find_by_name_pos()`：**归一化名字 + 无缩放世界链最近位置**（与 `ArenaBuilder.FindBuilt` 同判据） |
| 拿别的路径 | —— | 新增 `anim_targets(blend)` / `scene_items(blend, arena)`（从旧函数里拆出来的，行为不变） |

### 2.2 🔴 一处**判据被换掉**（这是全覆盖能做对的关键一跳，别改回去）

`build()` 里决定「改挂还是新建」的那一行：

```python
# 旧：if norm(leaf) in built and ps not in want:
# 新：if ps in built_paths:
```

* 旧规则的隐含前提是「**`want` 里的对象一定缺**」—— 那在 A191 那个收窄范围下成立（`want` 只装 4 个缺的宿主）。
* 全覆盖之后 `want` 装的就是「清单里每一条会建出来的对象」⇒ `ps not in want` **恒 False**
  ⇒ 若不改，**每一个已建对象都会被判成「要新建」**：prefab 里会长出一整套**重名空节点**，
  而真正的对象仍平铺着。⚠️ 这个错**不会让断言变红**（节点确实建出来了），是静默的 —— 所以写在这儿。
* 新规则还顺带修掉「按名字判」的老毛病：`built` 过去是**名字集合**，同名对象不止一个
  （实测 13 场里最多的一组同名 15 个）⇒ 现在按**路径**判，不会张冠李戴。

### 2.3 其余改动（行为保持）

| 位置 | 改了什么 |
|---|---|
| `Scene.__init__` | 新增 `kids`（子件索引，替掉 `children_of()` 的**整表遍历**）· `_wcache`（`world_of` 记忆化）· `_paths`（懒建路径索引）**全是纯函数/纯索引，判据一字未改** |
| `Scene.children_of` | 改走 `kids`；**顺序与旧实现逐字相同**（都按 `sc.tr` 的读入序），所以 `subtree()` 的结果集合与顺序都不变 |
| `Scene.path_index()`（新） | 「整条路径 → gopid」。旧代码对每条 need 路径都去 `by_name` 捞同名再拼父链（O(need × 场景)）；13 场铺开是千万级 |
| `go_at_path` | 先查 `path_index()`，查不到再退回原来的线性扫（**语义不变**，`gen_env_blendables.py` 也用它） |
| `build()` 的 `adds[]` | 多一道过滤：**已经在 `nodes[]`/`targets[]` 里的不再列**（组件由 ①/② 那一支补）。若不过滤，darkangels 的 `Directional Light`（现在是 target）会**同时**出现在 `targets` 与 `adds` 里 —— C# 侧幂等不出错，但数就虚高 |
| `main()` | 遍历 13 场；默认只打**汇总 + 警告**（逐条要看加 `--verbose`）；新增 `--only <场>`；`_stats.coverage` 把**每一格覆盖数**写进产物 |

### 2.4 两条边界（都**出声**，不是静默）

1. **`worldBaked` 的网格不纳入**（13 场共 **33 条**，**全在 `battlearena2`**）：
   `ArenaBuilder.ApplyGroupNodes` 有一道 `frozen` **明令不许改挂**（顶点已烘在世界坐标、holder 必须 identity）
   ⇒ 放进 `targets[]` 只会让「没对上」计数 +33（**假红**）。**代价**：arena2 那 33 件留在场根下平铺，
   **但它们的父节点 `Scenario/Scenario Battle Arena 2 Baked/Battle Arena 2` 已经建出来了**
   （因为它下面还有 8 件非 worldBaked 的 target）。⇒ **T-b 有一条便宜的收口路**（见 §八·3）。
2. **相机与 `Cache Stealth` 纳入**（每场各 1 件，13 场共 26 件）：它们原版住在
   **`BattlePrefab/…`** 下（战斗 UI 预制体那半边）。本来想只排掉相机，但 **`Cache Stealth`
   （`BattlePrefab/Cache [No delete]`，3D 卡用的网格）是清单里的网格、我们真建了它** ⇒
   **那棵树 13 场都会长出来** ⇒ 只排相机反而变成「`BattlePrefab` 在、相机却平铺着」，比两头都糟
   ⇒ 口径统一成一句 **「清单里每一条会建出来的对象都按原版路径归位」**。
   判据两条：① 那一串父的 local **全是 identity / 纯平移**（`BattlePrefab` identity ·
      `BattlePrefab/BattleBoardElements` = `(100,0,0)` · `Cache [No delete]` identity），
      改挂走 `SetParent(…, true)` ⇒ **世界位姿一字不变**；
   ② 运行期**没有**按名字找它们：`ArenaRuntimeLoader.Load(arenaKey, cam)` 是**传引用**，
      自检 `FindBoardCamera()` 按 `depth < 0` 找（`BattleScene.cs:304`），全仓 `BoardCamera` 的
      引用**全是注释**、无 `Find("BoardCamera")`。

---

## 三、实跑差异（改前 / 改后）

### 3.1 场数与产物

| | 改前（今天跑旧的） | 改后 |
|---|---|---|
| 跑的场 | **1 场**（只有 `battlearenatauviorla`） | **13 场** |
| 产出的场 | 0 份（`--check`）/ 1 份（写盘） | **13 份** |
| darkangels | **连跑都不跑**（旁挂里没有它了） | nodes 17 · targets 84 |
| tau | nodes **6** · **targets 0** · adds 0 | nodes 96 · targets 135 |

> 🔴 **改前的「今天」是真会退化的**：`env_blendables.json` 的 `_missingTargets` **已经自己变空**
> （A191 把那 4 个宿主建出来之后，`gen_env_blendables.py` 的检查 `prefab ∪ 清单` 就都找得到了
> —— 2026-10-13 实测 **0 条**）。旧生成器的输入只剩 tau 那 2 条 lookat ⇒
> **若今天照旧跑一次写盘，会把 tau 那份 `targets 32` 覆盖成 `targets 0`**（A191 断言当场红），
> 而 darkangels 那份旧文件留着不动 ⇒ 旁挂与 prefab 两边各说各话。
> 实据：`PYTHONIOENCODING=utf-8 python 工具/gen_arena_groups.py --check`（旧版）= `nodes 6 · targets 0`。

### 3.2 逐场数字（= 产物 `_stats`，T-b 可直接拿去对建场日志）

| 场 | want | **nodes** | **targets** | adds | 带 `Animation` | 清单行 | 会建 |
|---|---|---|---|---|---|---|---|
| battlearena1 | 62 | 21 | 62 | 0 | 0 | 65 | 65 |
| battlearena2 | 62 | 13 | 62 | 0 | 0 | 103 | 98 |
| battlearena3 | 91 | 29 | 91 | 0 | 1 | 95 | 91 |
| battlearenaaeldari | 63 | 13 | 63 | 0 | 0 | 66 | 64 |
| battlearenaastramilitarum | 102 | 7 | 102 | 0 | 1 | 102 | 102 |
| battlearenablacklegion | 87 | 16 | 87 | 0 | 1 | 91 | 87 |
| battlearenadarkangels | 84 | 17 | 84 | 0 | 2 | 94 | 84 |
| battlearenaemperorschildren | 94 | 6 | 94 | 0 | 1 | 94 | 94 |
| battlearenagenestealers | 83 | 9 | 83 | 0 | 1 | 84 | 84 |
| battlearenaleviathan | 89 | 14 | 89 | 0 | 0 | 91 | 89 |
| battlearenasororitas | 130 | 10 | 130 | 0 | 0 | 133 | 133 |
| battlearenaspacewolves | 82 | 12 | 82 | 0 | 0 | 88 | 82 |
| battlearenatauviorla | 137 | 96 | 135 | 0 | 3 | 215 | 135 |
| **合计** | — | **263** | **1164** | **0** | **10** | **1321** | **1208** |

（`want` = 清单那 ①②③ 算出来的路径数；`want` 与 `targets` 有差的两处：
 tau 137 = 135 已建 + 2 条「旁挂点名但我们没建」（`Railgun Turret N Target `）⇒ 那 2 条进 `nodes`。）

### 3.3 与「现读的 578 粒子 / 717 网格」对账 —— **逐位吻合**

| 项 | 数 | 出处 |
|---|---|---|
| 清单里带 `go` 的**粒子** | **578** | 13 份 `_manifest.json` 的 `particles[]`，逐场 34/46/46/23/20/39/34/13/35/45/49/31/163 |
| 清单里带 `go` 的**网格** | **717** | 同上 `meshes[]`，逐场 29/55/47/41/80/50/58/79/47/44/82/55/50 |
| = 网格 + 粒子 | **1295** | **与 §A345 那句「带 `go` 的条目 = 粒子 578 · 网格 717 = 1295 条」逐数相同** ✅ |
| ⊕ 灯 / 相机（每场各 1） | +26 = **1321 行** | 这就是表里「清单行」那一列 |
| 被四道闸门挡掉 | **−113** | 逐场 0/5/4/2/0/4/10/0/0/2/0/6/80（与建场日志同口径，键在 `_stats.gatesExcluded`） |
| = 会真建出来 | **1208** | 表里「会建」那一列 |
| − `worldBaked`（有意排除） | −33 | 全在 `battlearena2` |
| − 同一路径被两条清单条目命中 | −11 | 见 §七·4（清单本身有这份重复，不是我引入的） |
| = **unique 路径 → `targets[]`** | **1164** | ✅ 与表里 targets 合计一致 |
| ⊕ 祖先链 + 子树（原版有、我们没建） | 263 | → `nodes[]` |
| **need 总路径** | **1427** | |

> ⚠️ **A 表（W11 §五·1）那句「965 件粒子」我同样复现不出**（与 §A345 的结论一致）——
> 我拿到的是 **578**（口径 = 「清单 `particles[]` 里带 `go` 的条目」；`go` 空的 0 条）。
> 一个**未确证的猜测**（⛔ 别当结论）：`965` 更像 **`WarpforgeVFX/Prefabs/` 下的 `.prefab` 个数**
> —— 今天实测 **962**（`d:/4/Unity/MyGame/Assets/WarpforgeVFX/Prefabs/*.prefab`，`WarpforgeVFX` 全树共 962 个），
> 与 965 只差 3；那段记录写在 2026-10-11，之后 A210/A211 动过导出。**这条要按铁律 5 去订正那张 A 表。**

### 3.4 逐场产物 vs 旧文件（只有 2 场有旧文件可比）

| 场 | 旧（盘上，2026-10-11/12 生成） | 新 | 有没有丢东西 |
|---|---|---|---|
| battlearenadarkangels | nodes 12 · targets 51 · adds **1** | nodes **17** · targets **84** · adds **0** | **旧的那 12/51 条一条不少**，全是新增 |
| battlearenatauviorla | nodes 36 · targets 32 · adds 0 | nodes **96** · targets **135** · adds 0 | 同上，36/32 一条不少 |

* **旧有新的没有 = 0 条 / 0 条**（我逐集合比过 `nodes[].path` 与 `targets[].(name,parent)`）。
* darkangels 的 `adds` 从 1 → 0：**`Directional Light` 从 `adds[]` 升格成 `targets[]`**
  （父 = `Scenario`，`animation = 1`）—— 全过程（建节点 → 改挂 → 补 `Animation`）现在都在 ② 那一支里，
  **组件照样补得上**。⚠️ **但这会让 `BattleScene` 的一条断言变红**，见 §八·2。
* **A191 那几条硬判据（clip 的 `m_Path`）逐条复核过、全在**：
  `…/Dark Angels baked`（anim=1）· `Turret 1 barrel` · `Turret 2 barrel` · `Turret missile joint` ·
  `Turret 1 barrel/Lance Fire (5)` · `Turret 2 barrel/Lance Fire (5)` —— 6/6 在树里；
  tau 的 `Railgun Turret N Target `（**带尾随空格，原样保留**）与 `…/Cylinder.001/Railgun turret` 也都在。

### 3.5 三条自证检查（全过）

| 检查 | 结果 |
|---|---|
| 每一条 `nodes[].parent` / `targets[].parent` 都能在树里解析到（节点或已建对象） | **0 条解析不了**（263 + 1164 条逐条查） |
| `parentPos` 为空的条数（A345 那条「顺手加一条出声」要的就是这个） | **0 条** |
| 🔴 同名同位置不同父（`FindBuilt`/`FindNearest` 只能取最近 ⇒ 会**静默摆错**） | **0 组**（同名可区分的 15 组最多一场，都靠位置分得开） |

另：**重跑逐字节稳定**（13 份产物的 SHA-256 前后一致）—— 生成器是纯函数（`sorted(os.listdir)` + 直读原版包，
无时间戳 / 无随机），所以「重跑一次就能复现这 13 份」。

---

## 四、`ppos()` 那条静默 `[]` 改成怎么了

* 旧：取不到就 `return []`，**一个字都不打**。后果不是「少个字段」——`ArenaBuilder.ResolveGroupParent`
  收到空数组时也不出声，只按「名字 + 没有 pos」去找（= 同名对象里随便挑一个）⇒ **摆错了也无声**。
* 新：`ps` 非空且取不到 ⇒ **`stats['parentPosMissed'] += 1`**，把那条路径记进
  `_stats.coverage.parentPosMissedWhat`，`main()` 汇总后打一行
  `⚠️ <场>：`parentPos` 取不到（A345 之前是**静默**回 []）—— <逐条路径>`，并计入全场合计。
* `ps` 为空（**父就是场根**）**照旧静默回 `[]`** —— 那是正常态，不是失败。
* **实测：全 13 场 = 0 条**（全覆盖之后祖先链一定在 `by_path` 里）⇒ 这条现在是一条**真·哨兵**：
  哪天它不为 0，就是树与产物不同步。

---

## 五、产物清单（生成/更新了哪些文件 · 备份在哪）

**代码**：`d:/4/Unity/工具/gen_arena_groups.py`（477 行 → **784 行**；`git diff --numstat` = **+405 / −98**，**LF-only 复核过**）

**产物**（13 份；`LF-only` 与既有那份一致，全部 `io.open(p,'wb')` 写）：

| 场 | 文件 | 行 | 字节 | git |
|---|---|---|---|---|
| battlearena1 | `arenas/battlearena1/battlearena1_groups.json` | 1531 | 24310 | 新建 |
| battlearena2 | … | 1349 | 21890 | 新建 |
| battlearena3 | … | 2182 | 34382 | 新建 |
| battlearenaaeldari | … | 1330 | 20507 | 新建 |
| battlearenaastramilitarum | … | 1749 | 26251 | 新建 |
| battlearenablacklegion | … | 1767 | 27397 | 新建 |
| battlearenadarkangels | … | 1756 | 27704 | **改**（+652/−14） |
| battlearenaemperorschildren | … | 1602 | 23912 | 新建 |
| battlearenagenestealers | … | 1522 | 23441 | 新建 |
| battlearenaleviathan | … | 1747 | 26727 | 新建 |
| battlearenasororitas | … | 2254 | 34880 | 新建 |
| battlearenaspacewolves | … | 1588 | 24442 | 新建 |
| battlearenatauviorla | … | 4654 | 86300 | **改**（+3615/−432） |

合计 **402 KB**。⚠️ 11 份新文件**还没有 `.meta`**（Unity 下次刷新会生成；与 W11 当年那两份同）。

**备份（改前的那两份产物 + 改前的生成器）**：
`d:/4/_tmp_view/wa345a_backup/`
* `battlearenadarkangels_groups.json`（旧 991 行）· `battlearenatauviorla_groups.json`（旧 1128 行）
* `gen_arena_groups.py.bak`（改前那份，**`--check` 可复现「旧规则今天只跑 1 场、targets 0」**）

**跑过的命令（都不需要 Unity）**：
```
D:/2/Warpforge_tools/py312/python.exe -m py_compile 工具/gen_arena_groups.py
PYTHONIOENCODING=utf-8 … 工具/gen_arena_groups.py --check          # 13 场 3.5 秒
PYTHONIOENCODING=utf-8 … 工具/gen_arena_groups.py                  # 写 13 份
PYTHONIOENCODING=utf-8 … 工具/gen_env_blendables.py --check        # 共用件回归，见 §七·2
```

---

## 六、没查清 / 没做的（如实写）

1. 🔴 **「完整的树」还有 182 个对象没进节点**（**口径上是有意的，但必须说清**）：
   我的定义是「**最小生成树**」= 已建对象 ∪ 祖先 ∪ 已建对象的后代。原版那几棵战场树里，
   **既不建、也不是祖先/后代**的对象共 **182 个**（逐场：arena1 7 · arena2 51 · arena3 8 · aeldari 5 ·
   astra 20 · blacklegion 10 · darkangels 1 · emperorschildren 5 · genestealers 1 · leviathan 56 ·
   sororitas 4 · spacewolves 2 · tau 12），**它们现在没有节点**（= 它们本来就不在我们建的东西里）。
   典型：`Scenario/Sun flare`（`LensFlare`）· `Scenario/…/Smoke Column Left 1-3`（**LineRenderer**，我们没有这条产线）
   · arena2 的 `Tap collisions/Cube*`、`Battle Arena 2/*` 那 33 件 **worldBaked**。
   * **没做**的理由：它们要么是**我们根本不建的组件**（建个同名空壳 = W11 说的「空壳」，
     与「复刻」是两回事），要么是被 §2.4 那条硬闸门挡的。
   * ⛔ **但「全部展开」这条路我试算过、有坑**：若改成「covered 根下的全部后代」，
     `BattlePrefab` 也是 covered 根（`Cache Stealth` 在它下面）⇒ **会再拉进 1129 个战斗 UI 对象**
     （全场 1137 个），那显然不是要的东西。⇒ 真要展开，得先划一条边界（例如「不含 `BattlePrefab`」），
     **这条边界我不替调度台定**。**要展开的话是 `wanted_paths()` 里加 8 行 + 重跑 3.5 秒。**
2. **`worldBaked` 那 33 件没进 `targets[]`**（原因见 §2.4）。arena2 因此是唯一「树里有洞」的场：
   `Battle Arena 2` 节点有了、它下面 8 件挂进去了，**33 件留在场根下**。
   **要不要让 `ArenaBuilder` 也改挂它们 = C# 侧决定（T-b）**，我给不了判据（见 §八·3）。
3. **`965 件粒子` 复现不出**（与 §A345 同结论）—— 我给的是 578，见 §三·3 那张表和那段未确证猜测。
4. **没验「建场侧真跑一遍」**：一条 Unity 都没跑（本件明令）。所以「`ApplyGroupNodes` 拿到新旁挂后
   真能 0 漏挂」这件事**今天仍是推断**（依据 = 生成侧三条自证 + 与 `FindBuilt`/`ResolveGroupParent`
   判据逐条对齐），**必须由 T-b 的 `ArenaBuilder.BuildArenaPrefabs` 实证**。
5. **性能只有估算**：263 个新节点 / 1164 条改挂，`ApplyGroupNodes` 里 `FindBuilt` 是
   「对每个 target 扫一遍整棵树」（`GetComponentsInChildren<Transform>(true)`）⇒ 量级 ≈
   1164 × 树节点数（~1400）× 常数。**我没跑过，不知道会不会慢**（A345 也点了这一条）。
   ⚠️ 若建场日志显示明显变慢，**别改判据**、先看能不能加一张 name→Transform 的预索引（那是 T-b 的活）。
6. **`BattlePrefab` 那两级节点**是本次唯一「有争议但已定」的口径（§2.4）——
   我按「清单里会建出来的都归位」定，**如果调度台认为该排掉，改 1 行**（`wanted_paths()` 的 `add()`
   里加一条按顶层根过滤）+ 重跑。

---

## 七、顺手发现（⛔ 一个都没自己改）

1. 🔴 **旧生成器今天已经在退化**（见 §三·1）：`_missingTargets` 变空 ⇒ 旧规则只跑 1 场、`targets 0`。
   这正是本件事前没被记下来的一格 —— **A191 做完 ⇒ `_missingTargets` 自清 ⇒ 生成器失去输入**。
   本件顺手修掉了（场的名单改由清单来），**但 A 表/交接文档里那句「要补哪几条 = 旁挂自己给的」
   现在半过期**（旁挂仍然是一个来源，但**不再是主路**）—— 请调度台在合并时一并订正。
2. 🟡 **`工具/gen_env_blendables.py` 里两处注释已过期**（我只报不改，它在我的白名单外）：
   · `:873` 与 `:1438` 都写着「`nodes[]` 只补祖先链 + 宿主自己，不展开子树（**与 A191 那条
     `wanted_paths` 的范围**…）」—— A345 之后 `wanted_paths` 的范围已经**不是**那个了（现在是全覆盖）。
   实测**行为零影响**（那两个文件各写各的 `nodes[]`），只是措辞会误导。
   · 另：`--check` 实测仍打两条 `🔴 场景侧 standalone：battlearena3（AnimFXController）的宿主…
   也没进 nodes[]` —— **那是它自己那份 `nodes[]`**，不是我的。
3. 🟢 **本件顺带把 4 条「宿主建不出来」修成「宿主有了」**（T-b 重建 prefab 后生效，值得复核）：
   * `gen_env_blendables.py --check` 今天仍会点名的 `battlearena3` 那两条 `Scenario/Particle Effects/Lightning_Green (1)`
     ⇒ **实测在我的 `nodes[]` 里**（`battlearena3_groups.json`）；
   * A393 的 `sceneStandalone` 那 5 条里，`Scenario/Battle Arena 2 Particles/RocketTrail`（battlearena2）、
     `Scenario/Battle Arena Tau Viorla Baked/Railgun BIG (1)` 与 `…/Big Gun Effect`（tau）
     ⇒ **都在对应场的 `nodes[]` / `targets[]` 里**（RocketTrail 是 node、它的 5 个子件是 targets）。
   ⇒ **prefab 重建之后**，那几条 `AnimFXController` / 场景侧 blendable 的宿主就不再是空的（判据 = 路径在树上）。
4. 🟡 **清单本身有 11 处「两条条目 → 同一个原版对象」**（不是我引入的，全覆盖只是让它显形）：
   例：`battlearena1 Scenario/Particles/Generator glows` 两条、`…/TinyExplosion_Far/Embers` 两条、
   `battlearenasororitas …/CandleVerts/CandleFlame_mid`（**一条 mesh + 一条 particle**）等。
   ⇒ 效果 = `targets[]` 里会有**同名同位置的重复条目**（C# 侧 `FindBuilt` 取最近 ⇒ 命中同一个、幂等）。
   ⚠️ 我**没有**去重（去重会改变条数与 `_stats`）；只想让下一位知道「1164 比 1175 少 11 是这么来的」。
5. 🟡 **`ArenaBuilder.FindBuilt` 用 `Transform.position`（真世界位置），旁挂 `pos` 是无缩放世界链** ——
   A345 点名的那条已知盲区。本件**新增的覆盖面把同名对象从 121 条撑到 1164 条**，
   ⇒ 那一格的风险面**变大了**，但仍**没有实测发现触发**（我能给的只有：匹配阶段用的
   `world_of` 与清单口径逐位同源、最大距离 0.000001）。**这条要 T-b 在建场时盯「没对上」那行**。
6. 🟡 **arena2 那 33 件 worldBaked 的父 `Battle Arena 2` 是纯翻译/identity 吗**——
   我没逐条核（只核了 `BattlePrefab` 那两级）。`worldBaked` 能不能改挂，**判据就卡在这里**（§八·3）。

---

## 八、⏭ 交给主对话的 T-b 待办（建场侧要跑什么、验什么）

> **硬同步点**：生成侧已交件，**T-b 之前先别跑任何建场** —— 现在盘上的 11 份新旁挂 + 2 份重生成，
> 而 `Resources/ArenaPrefabs/*.prefab` 还是旧的 ⇒ **中间态**。⛔ 别让别的线插进来跑 `ArenaBuilder`。

### 1. 重建 13 件 arena prefab（唯一入口）
```
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod ArenaBuilder.BuildArenaPrefabs -logFile -
```
（Unity 版本号照 `资料/命令速查.md`。）

**期望**：`[Arena] §27 战场 prefab：建好 13 件、失败 0 件`，且 **13 场各多一行**（不再只有 2 行）：
```
[Arena] A191 分组节点（<场>）：新建 <表 §3.2 nodes> 个节点 · 改挂 <表 §3.2 targets> 个对象 ·
        补 `Animation` <表 §3.2> 个 · 没对上 0 条
```
| 场 | 新建 | 改挂 | Animation |
|---|---|---|---|
| battlearena1 | 21 | 62 | 0 |
| battlearena2 | 13 | 62 | 0 |
| battlearena3 | 29 | 91 | 1 |
| battlearenaaeldari | 13 | 63 | 0 |
| battlearenaastramilitarum | 7 | 102 | 1 |
| battlearenablacklegion | 16 | 87 | 1 |
| battlearenadarkangels | 17 | 84 | 2 |
| battlearenaemperorschildren | 6 | 94 | 1 |
| battlearenagenestealers | 9 | 83 | 1 |
| battlearenaleviathan | 14 | 89 | 0 |
| battlearenasororitas | 10 | 130 | 0 |
| battlearenaspacewolves | 12 | 82 | 0 |
| battlearenatauviorla | 96 | 135 | 3 |

**判据**：**「没对上 0 条」×13**。不为 0 ⇒ 逐条点名会告诉你差在哪一类：
①「target 找不到」⇒ 上游闸门口径不一致（A343 那一族，`工具/gen_arena_groups.py` 的 `is_built` 是**对读实现**）；
②「父路径找不到」⇒ 树没建全（本该进 `nodes[]` 的没进）；
③「`worldBaked` ⇒ 不改挂」⇒ **不该出现**（生成侧已经先挡掉了；出了就是过滤没生效）。
⚠️ 跑完**必须跟一次 `BattleScene.BuildAndSaveScene`**（本仓既有纪律：改 `ArenaBuilder` 之后场景要重打）。

### 2. ⚠️ **一条断言会「正确地红」——形状要改（这是本件唯一已知的必红）**
`CardPresentation/Editor/BattleScene.cs:1610` 的 darkangels 那条：
```csharp
Check(dkSc != null && dkBad == 0 && dkNode == dkSc.nodes.Length && dkAnim >= 2, …)
```
* `dkNode == nodes.Length`：**17/17**（重建后成立，**不需要改**）。
* `dkAnim >= 2`：**会红**。`dkAnim` 只从 `nodes[]`（`:1539-1541`）和 `adds[]`（`:1571-1575`）两处累加，
  **`targets[]` 那一支（`:1544-1562`）根本不看 `animation`**。而新旁挂里
  `Scenario/Directional Light` 已从 `adds[]` **升格成 `targets[]`**（`animation = 1`）
  ⇒ `dkAnim` 只剩 `Battle Arena Dark Angels baked` 这 1 个 ⇒ **1 < 2**
  （`adds` 现在 0 条，实测；旧的 1 条不再有）。
* **改法（3 行，照 tau 那段 `:1637-1641` 的写法搬进 targets 循环）**：
  `if (t.animation != 0) { if (tr.GetComponent<Animation>() != null) dkAnim++; else { dkBad++; … } }`
  —— 与 C# 侧 ② 的行为**逐字对应**（`ArenaBuilder.cs:2606-2607`）。
* ⚠️ **这条红不是缺陷**：组件照样补上了（②那一支补的），只是**断言的计数口径漏了一支**。
  ⛔ 别为了让它变绿去把 `Directional Light` 塞回 `adds[]`（那会破坏全覆盖）。
* tau 的同类断言（`:1681`）**不受影响**：它的 `tvAnim` 只数 `nodes[]`，两个炮塔节点照样是 2。

### 3. 三条**要调度台/写手拍板**的口径（都只差 1 行，重跑 3.5 秒）
1. **`worldBaked`（arena2 的 33 件）要不要改挂？** 生成侧今天**不纳入**（因为
   `ArenaBuilder.ApplyGroupNodes` 的 `frozen` 会 refuse 并计成「没对上」）。
   要收口有两条路：**(a)** 维持现状（arena2 那 33 件留在场根下 —— 树有洞）；
   **(b)** 让 C# 也改挂它们（`SetParent(…, true)` 会保住世界位姿；**先核 `Battle Arena 2` 那条父链是不是
   纯翻译/identity** —— 若是，改挂不会二次变换），同时把 `frozen` 那道闸门改成「只在父级缩放 ≠ 1 时拒绝」。
   ⚠️ (b) 要动 C#，**是 T-b/写手的活**，不在本件。
2. **182 个「我们根本不建」的原版对象要不要也建空节点？**（口径与坑见 §六·1）
3. **`BattlePrefab/…` 那两级（13 场都有）留不留？**（口径见 §2.4；要排掉 = 1 行过滤）

### 4. 建议顺带核的（免费）
* `gen_env_blendables.py --check`：`battlearena3` 那两条 `Lightning_Green` 现在**有宿主了**（§七·3），
  但那个脚本查的是**它自己**的 `nodes[]` ⇒ **它的报错不会自动消失**，别把它当成红。
* `Scenario/Particles/*` 那一层：随便挑一场开 `Resources/ArenaPrefabs/<场>.prefab` 看一眼 ——
  **粒子该在 `Scenario/Particles` 下，不再是一排平铺**（这是本件改动的直接观感判据）。

---

## 附：本件**没有**碰的东西（白名单外）
`ArenaBuilder.cs` / 任何 `.cs` · 别的 `工具/*.py` · `项目任务.md` · `CLAUDE.md` · git · Unity。
临时脚本与备份全在 `d:/4/_tmp_view/`（`wa345a_*.py` · `wa345a_backup/` · `wa345a_cache/`）。
