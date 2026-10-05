# D5 · `BattleScene.Run` 7 条红诊断（只读 · 2026-10-11）

> 派单：`d:/4/_tmp_view/unity_battlescene_run.log` 跑出 **1387 通过 / 7 失败**（`^BT    ✗` 计数 = **7**，逐条与本件表格一致；退出码 139 是已知收尾段错误，不作判据）。
> 判据口径：`Editor/BattleScene.cs`（断言本体）· `ArenaBuilder.cs`（建场）· 两场旁挂 `arenas/<场>/<场>_groups.json` · `数据/游戏数据/{env_blendables,environment_conditions,animator_controllers}.json` · **`Resources/ArenaPrefabs/*.prefab` 逐字节实测** · **`wf_prefabs_extra.bundle` 用 UnityPy 1.25.3 直读**。
> 未跑 Unity、未改任何文件（本报告是唯一产物）。

---

## 一、结论（一句话）

**7 条全是 (α)（断言自己写错），0 条 (β)。** 根因只有 **2 个**，都在**测试文件自己身上**，实现/资产侧**一行没错**：

| # | 根因 | 位置 | 后果 |
|---|---|---|---|
| **R1** | `findPath` 局部助手**结构上只能找到「以 prefab 根名开头」的路径**（分支写反：首段不等于根名时 `cur` 被置 null，而 for 的继续条件要求 `cur != null` ⇒ **循环一次都不进、直接 return null**） | `Editor/BattleScene.cs:1373-1390` | 旁挂里**每一条**路径都是相对 prefab 根的 `Scenario/…`，而根名实测 = **`battlearenadarkangels` / `battlearenatauviorla`**（13 件 prefab 逐件实测）⇒ **该助手在本段里 100% 返回 null**。#1 #2 #3 #4 #5 #6 #7 全部由此发端 |
| **R2** | 改挂那一段拿「**对象自己的路径**」去比「旁挂要求的**父**路径」 | `Editor/BattleScene.cs:1430-1431`（darkangels）· `:1488-1489`（tau） | **父子恒差一段** ⇒ 凡是找得到的对象**一律判错**。实测：83 条不匹配里 **63 条**形状是 `got == want + "/" + name`（darkangels 51 条、tau 12 条）⇒ 这 51/12 件**一件不差地都在它该在的父节点下** |

另有 **3 个次生缺口**（同样在断言侧）：

| # | 次生 | 位置 |
|---|---|---|
| R3 | `adds[]` 用 `findPath(root, "Directional Light")` 查一个**单段名字** —— 单段路径要求它**等于根名** ⇒ 恒 null。正确查法是「名字 + 最近位置」（生产侧 `FindBuilt` / `SceneResolver.GoOf` 那一套） | `Editor/BattleScene.cs:1437` |
| R4 | `dkMove == dkSc.targets.Length`（61）与 `dkBad == 0` **要求把已被上游四道闸门挡掉的 58 个对象也数进去** ⇒ **修好 R1/R2 之后仍不会绿**。D4 已判：那是**生成器**的表错（`工具/gen_arena_groups.py:246-254` 的 `built` 集合没套那四道闸门），不是建场/查找的错 | `Editor/BattleScene.cs:1457-1462`·`:1506-1510` |
| R5 | #6 #7 是**纯下游**：`bl == null` ⇒ 那段 `foreach` **一次都没执行**（`0/2：` 后面**是空的**就是这个指纹）⇒ 读起来像「链断了 / 组件的 `filterCode` 是空的」，**两件事都没被测到**。#7 的消息还把 `bl?.filterCode` 插成**空串**，属于**误导性文案** | `Editor/BattleScene.cs:1546-1557`·`:1564-1568` |

**「真缺口」只有一条，而且它不是这 7 条要管的事**：`Lance Fire (5)`（darkangels 2 件）与 tau 的 `Muzzle Flash glow` 等 48 条**确实不在我们建出来的树里** —— 已由 D4（`资料/普查产出_1011/D4_A191没对上诊断.md` §二/§六）逐条定性为「四道闸门挡掉」，**不在本次 7 条红里新增任何缺陷**。

---

## 二、逐条

### #1 · `:3556` ★ A191：分组节点**真的当父节点**了 → **(α) + 一个真缺口子条件**

| 项 | 内容 |
|---|---|
| 它红在哪 | `clipPathA` = `findPath(dkInst.transform, "Scenario/Battle Arena Dark Angels baked/Turret 1 barrel")` → **null**（R1）；`clipPathB` = `…/Turret 1 barrel/Lance Fire (5)` → **null**（R1 **和**真缺口，两重原因） |
| 证据①（R1） | 根名实测：`Resources/ArenaPrefabs/*.prefab` **13/13** 的根 GameObject 名 = **文件名**（`battlearenadarkangels` …）。判据 = 逐件解 prefab YAML，取 `Transform` 里 `m_Father: {fileID: 0}` 那件再回查 `m_Name`。⚠️ 建场时那件对象叫 `Warpforge_<场>`（`ArenaBuilder.cs:1785`），**存成 prefab 之后实测是 `<场>`** |
| 证据②（**它想验的事已经成立**） | 直接解 prefab 的父链：`battlearenadarkangels/Scenario/Battle Arena Dark Angels baked/Turret 1 barrel` ✓ · `…/Turret missile joint` ✓ · `…/Turret missile joint/Turret missile pods` ✓ ⇒ **分组节点真的是父节点**，#1 的**主判据本来就成立** |
| 证据③（真缺口） | `grep 'm_Name: Lance Fire'` 在 `battlearenadarkangels.prefab` = **0**（`Lance Fire` 一个字都没有）。D4 §二 族 1：原版自己 `m_IsActive=False` + `renderMode=5` + 材质 `Spine/Special/HiddenPass`/无贴图 ⇒ **我们刻意不建**（`ArenaBuilder.cs:1997-2016` 第一道闸门） |
| 顺带纠一处**注释里的判据** | 断言注释（`:1446-1447`）说那条 clip 的路径是 4 条。实读 `wf_prefabs_extra.bundle` 里的 `Dark Angels Void Combat animations`，**真值 13 条曲线 / 4 条唯一路径**：`Turret 1 barrel`（Position＋Rotation＋Scale）· `Turret 2 barrel`（Position＋Rotation＋Scale）· `Turret missile joint`（Rotation＋Scale）· **`Turret 1 barrel/Lance Fire (5)` 与 `Turret 2 barrel/Lance Fire (5)`（Float，两条）** ⇒ 注释**只抄了 1 号炮塔那一条**，漏了 2 号炮塔那条 |
| 结论 | **(α)**：助手坏了 + 把「已知闸门挡掉的 `Lance Fire (5)`」当成本断言的应满足条件（它属于 D4 那张账） |

### #2 · `:3568` ★ A191：darkangels 的旁挂逐条对上（0/2 · 0/61 · 0 个）→ **(α)**

`dkBad = 64` 的**逐项拆解**（我把日志那行拆成四类数出来的，与断言三个计数器逐个对上）：

| 类 | 条数 | 真相 |
|---|---|---|
| 缺节点（`Scenario` · `Scenario/Battle Arena Dark Angels baked`） | **2** | **两件都在**（根下 `Scenario` ✓，其下分组节点 ✓）⇒ 纯 R1 |
| 改挂「父不对」 | **51** | 全部形状 = `want + "/" + name` ⇒ **位置一条不差**（实测 51/51 符合该形状）⇒ 纯 R2 |
| 改挂「找不到」 | **10** | `Lance Fire (5)`×2 · `Charge`/`Glow`/`Glow (1)`/`Lightning`×2 ⇒ **真的不在树里**（D4 §二 族 1/2，闸门①②） |
| `adds` 里的 `Directional Light` 找不到 | **1** | 它**在**（prefab 里 `Animation` 就挂在它身上）⇒ 纯 R3 |

`2 + 51 + 10 + 1 = 64` ✓。`dkAnim` 同理恒 0（节点找不到 ⇒ 那一支不进；adds 找不到 ⇒ 那一支不进），而 prefab 实测**恰有 2 件带 `Animation`**：`Battle Arena Dark Angels baked` + `Directional Light`（判据：解 prefab 的 `!u!111` 块回查 `m_GameObject`）。

⇒ **(α)**（R1＋R2＋R3）。**另外**：即使三处全修好，`dkBad == 0` 与 `dkMove == 61` 仍**不可能**成立（那 10 条是 D4 的账）⇒ 属 R4。

### #3 · `:3580` ★ A191：tauviorla 那两个「清单外的」也建出来了 → **(α)**

| 断言找的路径 | prefab 实测 |
|---|---|
| `battlearenatauviorla/Scenario/Battle Arena Tau Viorla Baked/Railgun Turret 1/Railgun Turret 1 Target `（**尾随空格照抄**） | ✓ 命中 1 件，父链逐字相同 |
| `…/Railgun Turret 1/Railgun Turret Base.001/Cylinder.001/Railgun turret` | ✓ 命中（`Railgun turret` 共 2 件，另一件在 `Base.002/Cylinder.003` 下，与旁挂 `nodes[]` 第 7/8 条一致） |

⇒ 两件**都在**，唯一的红因是 R1。**(α)**，无缺口。

### #4 · `:3592` ★ A191：tauviorla 的旁挂逐条对上（0/8 · 0/60 · 0 个）→ **(α)**

`tvBad = 68` 拆解：**8**（缺节点，其实 8 件全在：`Scenario` · `…Baked` · `Railgun Turret 1` · `Railgun Turret 2` · 两件 `Target` · 两件 `Railgun turret`，8 条 path 我逐条解出来都对上）+ **32**（改挂「父不对」，其中 **12** 条形状 = `want + "/" + name` ⇒ 位置正确；另 **20** 条对象**平铺在根下**、其旁挂要求的父是 `Muzzle Flash glow` —— 那件**在 prefab 里 0 件**）+ **28**（改挂「找不到」）。`8 + 32 + 28 = 68` ✓。

- 12 条正确 vs 20+28 条不能成：与建场那行日志的 `改挂 12 · 没对上 48` **逐数吻合**（48 = 28 + 20）。
- 48 条的真因 = D4 §二 族 3/4/5/6（闸门②④①＋级联），**不是查找逻辑**；D4 §六·1 已明确预警「这两条现在必红、别当新缺陷追」。

⇒ **(α)**（R1＋R2），期望值另属 R4。

### #5 · `:3616` ★ A201：`ScenarioAnimationBlend` 建出来了、`myAnimation` 非空 → **(α)（不是 (β)）**

- `Editor/BattleScene.cs:1531-1536`：`dkGroup = findPath(…, "Scenario/Battle Arena Dark Angels baked")` → **null**（R1）⇒ `bl = (it2 != null && dkGroup != null) ? Create(...) : null` → **null** ⇒ **`ScenarioBlendableFactory.Create` 一次都没被调用**。日志里的 `组件 ✗` 就是这个意思（不是「建了但属性空」）。
- **「补在了别处」这个假设被证伪**：建场那一步确实把 `Animation` 补在了**正确的那一颗**上 ——
  · `ArenaBuilder.cs:2568-2574`：`created[n.path] = go.transform` 与 `go.AddComponent<Animation>()` 是**同一个 GameObject**；
  · prefab 实测：`!u!111`（`Animation`）共 2 块，分别挂在 **`Battle Arena Dark Angels baked`** 与 **`Directional Light`** 上（tau 那件：`Railgun Turret 1` / `Railgun Turret 2`）。
- **两个 key 是同一个对象**（这是本条的关键）：断言要的宿主 = 分组节点 GameObject；生产侧取 `myAnimation` 的两种路都落回它 ——
  · `ScenarioBlendables.cs:1454-1455`：`ResolveFirst(it,"animation", res.AnimationOf)`，取不到再 `host.GetComponent<Animation>()` 兜底；
  · `EnvironmentApplier.cs:780-781`：`FindAnimationInScene` = `FindNearest(root, t.leaf, t.pos)` + `GetComponent<Animation>()`，旁挂那条的 `leaf = "Battle Arena Dark Angels baked"`、`pos = (-0,-0,0)`，而该对象 `localPos (-113.238, 8.695, -19.853)` 配上父 `Scenario` 的 `(113.238, -8.695, 19.853)` **正好 = 0**，且**全树同名只有 1 件** ⇒ 必命中。
- 附带（建议，不是判据错）：断言**自己**用 `findPath` 找宿主，而**生产**用 `PickSceneHost`（名字 + 最近位置，`EnvironmentApplier.cs:838-848`）⇒ 建议直接用生产那份解析器（`SceneResolver.GoOf`），否则就是「同一件事两处实现」（铁律 6）。
- 运行时侧同一条链**不吃这个断言的影响**：`ArenaRuntimeLoader.cs:100` 运行时 `Resources.Load<GameObject>("ArenaPrefabs/" + arenaKey)` 实例化的**就是这同一件 prefab**（§27）⇒ 修好断言 = 真验到运行时会走的那棵树，**没有「断言绿了但游戏还是旧树」的陷阱**。

⇒ **(α)**；`myAnimation` 一旦宿主不为 null **必然非空**。

### #6 · `:3628` ★ A201：GUID → `LoadAsset<AnimationClip>` → `AddClip` 这条链真的通（0/2）→ **(α)（红因是「没跑到」，不是「链断」）**

- `Editor/BattleScene.cs:1546-1563`：`clipOk` 的 `foreach` 被 `bl != null && bl.clipLoader != null && bl.myAnimation != null` 挡着；`bl == null` ⇒ 循环 0 次 ⇒ `clipOk = 0`、`clipWhat = ""`（日志 `0/2：` **后面是空的**，正是「一次都没跑」的指纹，不是「跑了 2 次都失败」）。
- 我另做的**直读证据**（UnityPy 1.25.3 打开 `Assets/StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle`）：
  · **容器 16 条键，两条 GUID 都在**：`aac3fe87a4618f5478ceaad364650105` ✓ · `58db0a1f684b5ee4ba197e8d344012d2` ✓（正是 `environment_conditions.json` 里两条 `animationsToChange[].clip`）；
  · 包里 **4 条 `AnimationClip`**，名字对得上（`Dark Angels Void Combat animations` sampleRate 25 · `LightAnimationOrbit` 60 · 另两条是卡包/卡背那批）；
  · 本进程**同一只包在同一轮里已经成功加载并被取过资产**：全日志有 5 条 `[WarpforgeEffectBinder] … 挂上原版控制器 'Card 3D WH40K Explosion'（1 个 clip）`，而**没有任何** `WarpforgeAnimatorBridge` 的「找不到包 / 加载不出 / 取不到控制器」报警。
- 断言文案里提到 `wf_prefabs_extra.bundle` 是**写死的说明**，**不是**运行时报错 —— 我逐条核过，本程**没有**任何「取不到 clip」的 `[EnvBlend]` 报错（因为那段代码没跑）。
- 唯一**没验到**的一环 = 编辑器**非播放态**下 `Animation.AddClip` 是否被接受（`ScenarioBlendables.cs:958-963`）。见 §五。

⇒ **(α)**：0/2 反映的是「前置被断言自己的坏助手挡掉」，**不反映链的状态**。

### #7 · `:3640` ★ A201：`filterCode` 配对 → **(α)**

- `Editor/BattleScene.cs:1564-1568`：`bl != null && bl.filterCode == "VoidCombatAnimations"`；`bl == null` ⇒ 恒红。消息里 `{bl?.filterCode}` 被插成**空串**，写出来成了「那颗的 `filterCode` =  ``」——**读起来像「组件 filterCode 是空的」，实际是「组件根本没建出来」**（`Directional Light` 那句「是 `LightAnimationOrbital`」同理，也是文案里写死的、不是读出来的）。
- 真值（旁挂 `数据/游戏数据/env_blendables.json` → `scene.battlearenadarkangels[]`）：
  · `ownerLeaf = "Battle Arena Dark Angels baked"` → `floats: [{k:"filterCode", s:"VoidCombatAnimations"}]`；
  · `Item.GetS(k)` 走的就是 `floats`（`EnvBlendables.cs:136` → `GetS(TargetField[], k)` 返回 `s`）⇒ 宿主一好，`bl.filterCode == "VoidCombatAnimations"` **必然成立**。
- 断言里那半句事实陈述我逐字核过、**是对的**：另一条 `ownerLeaf = "Directional Light"` 的 `floats` = **`LightAnimationOrbital`**，而 `environment_conditions.json` 里 SO `Environmental Condition Dark Angels Orbiting` 的 `animationsToChange[0].filterCode` = **`LightAnimationOrbit`**（确实差一个 `al`）⇒ **照抄原版是对的，别去「修」**。

⇒ **(α)**（纯下游）。

---

## 三、重点三件事的单独答复

### ①「节点 0/2 为什么是 0」—— 是 (α)，但**不是**「前缀多写了一段」，是**助手的「根名可选」写反了**

不是「断言按带 `Scenario/` 前缀的完整路径找、我们建的是平铺的」——**我们建的已经不是平铺**：`Scenario` → `Battle Arena Dark Angels baked` → 目标对象这条链**在 prefab 里逐字存在**（§二 #1/#3 的父链实测）。

真正的机制只有一句：`findPath`（`BattleScene.cs:1373-1390`）把「首段 == 根名」当成「从第 2 段开始下沉」；**一旦不等，它把 `cur` 置 null，而 for 的继续条件是 `cur != null` ⇒ 循环一次都不进、直接 `return null`**。于是该助手**只能找到以根名开头的路径**，而旁挂每条路径都以 `Scenario` 开头、根名实测是 `<场>` ⇒ **本段每一次 findPath 都是 null**（节点、clip 路径、tau 两件、`adds` 那件，全中）。

⇒ **两条口径根本不是一回事**（判据侧 `Scenario/…`＝相对 prefab 根；助手侧要求「从根实例自己出发」），**是 (α)**。

### ② `myAnimation` 为 null —— **(α)，「补在别处」这个假设已被证伪**

见 §二 #5：`Animation` 补在 `created[n.path]` 那颗 GameObject 上，prefab 实测就是 `Battle Arena Dark Angels baked`（+ `Directional Light`）；生产侧取 `myAnimation` 的两条路（`res.AnimationOf` / `host.GetComponent<Animation>()`）**都落回同一颗对象**。断言**没有查错节点** —— 它的宿主变量本身是 null，**根本没查**。

### ③「GUID → `LoadAsset` → `AddClip`」那条链 0/2 —— **既不是「前置没建好所以必然 0」，也不是「链本身断了」**

准确说法：**前置不是「没建好」，而是「断言自己的宿主查找返回了 null」⇒ 这段代码一次都没执行**。链的每一环我都拿到了独立证据（两条 GUID 都在包的容器键里 · 包里确有这两条 clip · 同一只包同一轮里加载成功过），**只有「编辑器非播放态的 `Animation.AddClip`」这一跳无法在只读条件下验证**（§五）。

⚠️ 日志里那句关于 `wf_prefabs_extra.bundle` 的话是**断言文案里写死的说明**，不是运行时报错；本程**没有**任何「取不到 clip」的报错。

---

## 四、若是 (α)：7 条各给出【可执行的正确判据文字】

### 0）先修两处公共件（一次性）

**（0-a）`findPath` 改对**（`BattleScene.cs:1373-1390` 整段替换；语义：「路径相对根」或「首段就是根名」两种写法都支持）：

```csharp
System.Func<Transform, string, Transform> findPath = (rt, path) =>
{
    if (rt == null || string.IsNullOrEmpty(path)) return null;
    var segs = path.Split('/');
    Transform cur = rt;
    // 🔴 2026-10-11 订正：原来写成 `cur = 根名相等 ? rt : null` ⇒ 首段不是根名时
    //    `cur` 为 null、for 的条件 `cur != null` 不成立 ⇒ **一次都不进循环就返回 null**。
    //    旁挂的路径是**相对 prefab 根**的（`Scenario/…`），根名实测是 `<场>` ⇒ 原来恒 null。
    int i = CardPresentation.EnvironmentApplier.Norm(rt.name) == CardPresentation.EnvironmentApplier.Norm(segs[0]) ? 1 : 0;
    for (; cur != null && i < segs.Length; i++)
    {
        Transform nx = null;
        for (int c = 0; c < cur.childCount; c++)
            if (CardPresentation.EnvironmentApplier.Norm(cur.GetChild(c).name)
                == CardPresentation.EnvironmentApplier.Norm(segs[i])) { nx = cur.GetChild(c); break; }
        cur = nx;
    }
    return cur;
};
```

**（0-b）比「父路径」而不是「自己的路径」**（新增一个助手，`pathOf` 保持不动）：

```csharp
System.Func<Transform, Transform, string> parentPathOf =
    (t, rt) => (t == null || t == rt) ? "" : pathOf(t, rt);
// 用法（改挂两处）：var got = parentPathOf(tr.transform.parent, dkInst.transform);
//                    if (got != t.parent) { … }
```
（旁挂 `parent` 的含义 = **父的路径**，判据 = 两场 `_groups.json` 的 `nodes[].parent`：节点 `Scenario/Battle Arena Dark Angels baked` 的 `parent` 写的是 `Scenario`。实测两场**没有任何** `parent` 段带尾随空格 ⇒ 今天不需要按段 Trim，但真要稳，比较前两侧都过一遍 `Norm`。）

**（0-c）`adds[]` 改用「名字 + 最近位置」**（旁挂 `adds[]` 只有 `name`/`pos`，**没有 path**）：

```csharp
var tr = findTarget(resDk, t.name, t.pos);   // 生产解析器 SceneResolver.GoOf —— 与 ArenaBuilder.FindBuilt 同一套判据
```

### 1）`★ A191：分组节点真的当父节点`（`:1451-1456`）

```csharp
// 判据 = 那条 clip 的**唯一路径清单**里，**当前应当在树上的那几条**逐条解析得到，
//   且分组节点是它们的祖先链上的一环（A191 的全部意义：只建空壳在这里当场红）。
string[] clipPaths =
{
    "Scenario/Battle Arena Dark Angels baked/Turret 1 barrel",
    "Scenario/Battle Arena Dark Angels baked/Turret 2 barrel",
    "Scenario/Battle Arena Dark Angels baked/Turret missile joint",
    "Scenario/Battle Arena Dark Angels baked/Turret 1 barrel/Lance Fire (5)",   // ⚠️ 见下
    "Scenario/Battle Arena Dark Angels baked/Turret 2 barrel/Lance Fire (5)",   // ⚠️ 见下
};
int clipOk = 0;
foreach (var p in clipPaths) if (findPath(dkInst.transform, p) != null) clipOk++;
Check(clipOk >= 3,     // 前三条 = 当前已建出来的；后两条 = 上游闸门挡掉的 `Lance Fire (5)`（D4 §二 族 1）
      $"★ A191：分组节点**真的当父节点**了 —— `Dark Angels Void Combat animations` 的路径 {clipOk}/{clipPaths.Length} 条解析得到"
    + "（当前应为 3：`Turret 1 barrel`/`Turret 2 barrel`/`Turret missile joint`；"
    + "`…/Lance Fire (5)` 两条 = 原版自己 `m_IsActive=False`+`renderMode=5` 被闸门①挡掉，见 D4 §二 族 1）");
```
（若想更严：把它拆成两条断言 —— 「前三条必须全中」＋「`Lance Fire (5)` 两条**当前必须不中**（= 闸门生效），等生成器补上闸门后再改成必须中」。）

### 2）`★ A191：darkangels 的旁挂逐条对上`（`:1457-1462`）

```csharp
// 计数器拆成三档，别再拿 targets 总数当分母（分母是「旁挂写的条数」，不是「应当发生的条数」）
//   · moved      = 找到 + 父路径逐条对上
//   · parentBad  = 找到 + 父路径不对（**这一档必须 0** —— 它才是「我们建错了」）
//   · notBuilt   = 旁挂里有、我们树上没有（= 上游四道闸门挡掉的那批，改成逐条点名）
Check(dkSc != null && dkNode == dkSc.nodes.Length && dkAnim >= 2 && parentBad == 0,
      $"★ A191：darkangels 的旁挂**结构逐条对上**（节点 {dkNode}/{dkSc.nodes.Length} · 带 `Animation` 的 {dkAnim} 个"
    + $" · 改挂 {moved} 条、父路径全对 · 闸门挡掉未建 {notBuilt} 条：{notBuiltWhat}）");
```
数字（**从旁挂现算，别写死**）：今天 `dkNode = 2`、`dkAnim = 2`、`moved = 51`、`parentBad = 0`、`notBuilt = 10`。等 `工具/gen_arena_groups.py:246-254` 补上四道闸门（D4 §三），那 10 条会转成 `nodes[]`（建成**空节点**，带原版 local TRS）⇒ 那时 `notBuilt` 自然降到 0、`dkNode` 升到 12。（这两笔是**同一笔账**，别分开修两次。）

### 3）`★ A191：tauviorla 那两个「清单外的」也建出来了`（`:1501-1505`）

```csharp
// 判据不变（这两件本来就该在）：唯一改的是 (0-a) 那个助手
Check(tvTgt1 != null && tvTurret != null,
      "★ A191：tauviorla 那两件「清单外的」建出来了 —— `Railgun Turret 1 Target `（尾随空格照抄）与 `…/Cylinder.001/Railgun turret`"
    + $"（{(tvTgt1 != null ? "✓" : "✗")} / {(tvTurret != null ? "✓" : "✗")}）");
```

### 4）`★ A191：tauviorla 的旁挂逐条对上`（`:1506-1510`）

同 #2 的三档写法。今天：`tvNode = 8`（8 条 path 全在）· `tvAnim = 2`（两个炮塔节点）· `moved = 12` · `parentBad = 0` · `notBuilt = 48`（D4 §二）。**⛔ 别写 `改挂 60/60`**。

### 5）`★ A201：ScenarioAnimationBlend 建出来了、myAnimation 非空`（`:1531-1544`）

```csharp
// 宿主改用**生产那份**解析器（旁挂那条目标自带 leaf+pos；与运行时 PickSceneHost 同一条路）
var resA = new CardPresentation.EnvironmentApplier.SceneResolver(dkInst.transform);
var hostA = (it2 != null && it2.targets != null && it2.targets.Length > 0)
          ? resA.GoOf(it2.targets[0]) : null;         // 不会写第二份「按名字+最近位置找对象」
var bl = hostA != null ? CardPresentation.ScenarioBlendableFactory.Create(it2, hostA, resA, true)
                         as CardPresentation.ScenarioAnimationBlend : null;
Check(bl != null && bl.myAnimation != null,
      $"★ A201：`ScenarioAnimationBlend` 建出来了、`myAnimation` 非空（组件 {(bl != null ? "✓" : "✗")} · "
    + $"`myAnimation` {(bl != null && bl.myAnimation != null ? bl.myAnimation.name : "null")}）");
```

### 6）`★ A201：GUID → LoadAsset → AddClip 这条链`（`:1546-1563`）

```csharp
// 判据不变；只是把「循环 0 次」也如实说出来（今天那个 `0/2：` 后面是空的，容易被读成「跑了 2 条都失败」）
Check(clipOk == guids.Count && guids.Count > 0,
      $"★ A201：GUID → `LoadAsset<AnimationClip>` → `Animation.AddClip` 这条链真的通（{clipOk}/{guids.Count}："
    + (bl == null || bl.clipLoader == null || bl.myAnimation == null
        ? "**没跑到**（前置未满足：组件 / `clipLoader` / `myAnimation` 有一处是 null）"
        : clipWhat)
    + "）—— 走的包 = `StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle`");
```

### 7）`★ A201：filterCode 配对`（`:1564-1568`）

```csharp
Check(bl != null && bl.filterCode == "VoidCombatAnimations",
      "★ A201（照抄原版数据，别去「修」）：那颗的 `filterCode` = "
    + (bl != null ? $"`{bl.filterCode}`" : "**组件没建出来 ⇒ 读不到**")
    + "；SO `Dark Angels Void Combat` 那条 `animationsToChange[].filterCode` 写的就是 `VoidCombatAnimations` ⇒ 配得上；"
    + "而 `Directional Light` 那颗旁挂里是 `LightAnimationOrbital`、SO `… Orbiting` 写的是 `LightAnimationOrbit`"
    + "（差一个 `al`）⇒ **原版自己那一对永远配不上**，照抄");
```
（把写死的「`Directional Light` 那颗是 X」改成**从旁挂读**，免得下次它变了没人知道。）

---

## 五、判不了的（缺什么才能判）

1. **编辑器非播放态下 `Animation.AddClip` 是否被接受**（唯一没验的一环，直接决定 #6 修好之后是绿还是红）。
   · 事实：`ScenarioBlendables.cs:958-963` 是 `myAnimation.AddClip(clip, clip.name)`；断言在**编辑模式**下调它，随后用 `GetClip(clip.name)` 回读（`BattleScene.cs:1553`）。
   · 我**没有找到**任何本仓经验记录（`资料/` 全域 grep `AddClip` 只有反编译判据与这三处断言文案），也**没有**任何证据说它一定失败。
   · 判法：跑一次 `BattleScene.Run` 即知（本件被禁止跑 Unity）。
   · 万一不成立 ⇒ 判据降级成「loader 那一跳」：断言 `clip != null && clip.name == "…"`；`AddClip`/`GetClip` 那两步挪进 `if (Application.isPlaying)` 分支，并在注释里写明「编辑器非播放态不接受 `AddClip`（实测）」。
2. **「两条 GUID 都在包里」我只验到「容器键里有」**，没验「`LoadAsset<AnimationClip>(guid)` 这条 API 真的按容器键命中」（那需要 Unity 运行时）。旁证：同一只包同一轮里 `LoadAsset<RuntimeAnimatorController>(名字)` 成功过（AnimatorBridge 那 5 行日志）。
3. **`Lance Fire (5)` 这个缺口要不要补**（是保持「不建」还是照生成器补闸门后建成**空节点**）—— 那是 §三 的决策，不是诊断能定的。

---

## 六、顺手发现（⛔ 只报不改）

1. 🔴 **`grep -a <字符串> <bundle>` 对 UnityFS 包不是有效判据（会漏）**。我自己踩了一次：`grep -ac aac3fe87…bundle` = **0**，先用 UnityPy 打开才发现容器键**在里面**（块是压缩的）。⇒ 报「包里有没有某个东西」必须走 UnityPy / 解包树，别用字符串 grep（与铁律 5 那条「翻过一个镜像目录就写『本地没有』」是同一类错）。
2. 🔴 **这条日志的分母会把人骗住**：`节点 0/2 · 改挂 0/61` 的**分母是「旁挂写的条数」**，不是「应当发生的条数」⇒ 读起来像 0% 完成，实际是 51/61 已经就位。建议和 D4 §六·2 那条一起改（把三档拆开打印）。
3. 🔴 **同一件事两处实现（铁律 6）**：建场那一步的结果**已经**落在 `ArenaBuilder.LastGroupNodeCreated/Moved/AnimAdded/Missed`（`ArenaBuilder.cs:2539-2541`，含逐条点名），而断言**把旁挂整个重算了一遍**（自己写 `findPath` / `pathOf` / 用 `SceneResolver`）。更省且更不会走味的做法：断言读那四个静态量 + 只加一层「结构正确性」（父路径对不对）。
4. ⚠️ **#6 会把两条不同 SO 的 clip 都 `AddClip` 到同一个 `Animation` 上**（一条来自 `Dark Angels Void Combat`、一条来自 `Dark Angels Orbiting`），**生产路径不会这样**（`DoScenarioBlend` 先按 `filterCode` 过滤，`ScenarioBlendables.cs:935`）⇒ 它是**loader 链探针**，不是「这两条 clip 会播」的证据；文案里值得写明，免得下个会话误读。
5. 🟡 **`ArenaPrefabs/*.prefab` 的根名是 `<场>`（13/13），而有两处代码仍按 `Warpforge_<场>` 认根**：`EnvironmentApplier.ArenaRoot()` 的兜底分支（`EnvironmentApplier.cs:503-504`：`t.name == "Warpforge_" + key`）与 `ArenaBuilder.cs:2505` 的注释。今天不影响任何断言（`ArenaRoot` 先找 `Arena3D`，自检/运行都命中），但这条兜底**实际上是死分支**；改名发生在哪一步（存盘时？后来某轮？）我**没查**。建议下一轮核一次并订正注释。
6. 🟡 `pathOf` 每段都 `Trim()`，而旁挂的 `parent` 是**原样**字符串 ⇒ 若哪天出现「父的某一段带尾随空格」，比较会**静默假红**。实测今天两场 `parent` 段**没有**带空格（0/121），属潜在坑。

---

## 七、我读过的文件与命令

**读的源码**
- `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（`:1330-1609` 逐行；含 `findPath`/`pathOf`/`findTarget`/七条断言）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/EnvironmentApplier.cs`（`:430-560` · `:743-833` —— `Norm` / `FindNearest` / `FindAnimationInScene` / `SceneResolver` / `PickSceneHost` / `ArenaRoot`）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/ScenarioBlendables.cs`（`:869-964` `ScenarioAnimationBlend` · `:1377-1542` `Create`/`ResolveFirst` · `:1699-1807` `AnimationClipByGuid`/`ClipFrom`/`EnsureClipBundle`）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Core/EnvBlendables.cs`（`GetS` / `Item.floats` / `ForArena`）
- `d:/4/Unity/MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs`（`:455-495` 存 prefab · `:1785` 建根 · `:2494-2674` `ApplyGroupNodes`/`ResolveGroupParent`/`FindBuilt`）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/ArenaRuntimeLoader.cs`（`:100` 运行时取 prefab）
- `d:/4/Unity/工具/gen_arena_groups.py`（`:246-330` `built` 集合与 `targets/nodes/adds` 分类）
- `d:/4/Unity/工具/extract_missing_shaders.py`（`:228-290` `clips_by_guid` / GUID 容器别名）

**读的数据**
- `arenas/battlearenadarkangels/battlearenadarkangels_groups.json`（nodes 2 · targets 61 · adds 1）· `…taulviorla…`（nodes 8 · targets 60 · adds 0）—— 逐条 dump
- `数据/游戏数据/env_blendables.json`（`scene.battlearenadarkangels[]` 两条 `ScenarioAnimationBlend` 的 `ownerLeaf`/`targets[].kind`/`floats.filterCode`）
- `数据/游戏数据/environment_conditions.json`（两条 `animationsToChange`）
- `数据/游戏数据/animator_controllers.json`（`clipsByGuid` 两条）

**只读实测（python，未写任何文件）**
- 解 `Resources/ArenaPrefabs/*.prefab` 的 YAML：13 件根名 · darkangels/tau 的父链 · `!u!111`（`Animation`）挂在谁身上 · `Lance Fire` 命中 0 · tau 各名字计数
- 逐条拆日志：`^BT    ✗` = **7**；`=== 合计：1387 通过 / 7 失败 ===`；把两行「旁挂逐条对上」按四类（缺节点/父不对/改挂找不到/adds找不到）**重新计数并与日志自报数逐一对上**（64 = 2+51+10+1 · 68 = 8+32+28）；用正则把 83 条「父不对」逐条比形状（63 条 = `want + "/" + name`）
- **UnityPy 1.25.3** 打开 `Assets/StreamingAssets/WarpforgeVFX/wf_prefabs_extra.bundle`：列 16 条容器键（两条 GUID 都在）· 4 条 `AnimationClip` · 逐条打印 `Dark Angels Void Combat animations` 的 13 条曲线路径

**读的既有报告**
- `资料/普查产出_1011/D4_A191没对上诊断.md`（§一/§二/§三/§六）· `资料/普查产出_1011/W11_子8b.md`（`:125-150` 行尾与 9 条断言表 · `:200-230` 期望值）
- 日志：`d:/4/_tmp_view/unity_battlescene_run.log` · `d:/4/_tmp_view/unity_a191_build.log`

**未做（按简报红线）**：未跑 Unity / 未跑自检 / 未动 git / 未改任何文件（本报告除外）。
