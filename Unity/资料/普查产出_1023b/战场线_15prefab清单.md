# 战场线 · 15 prefab 清单（AssetRipper 工程口径）· 只读普查

> 普查日期 2026-10-23 · 只读代理产出 · 数据源 = `d:/2/tools/assetripper_out/ExportedProject`（Unity 6000.2.6f2 · 5.9 GB）
> 判据一律是本机现读（解析 `.unity` YAML 的文档图，按 `Transform.m_Father` 建父链）。

---

## 一、结论（先看这五条）

1. **13 个战场 `.unity` 全部在**：`Assets/Data/Scenes/Battle Scenarios/`（5.7–13.8 MB/份）。
   **每一份都有 `Scenario` 根**（`m_Father: {fileID: 0}`），包括 leviathan。
   ⇒ 与 `J-A_战场素材与材质诊断.md:104`「**leviathan 没有 `Scenario` 根**」**冲突**，见 §六 待并入。
2. **按 AssetRipper 导出的真实形状，世界内容根 = 16 颗**（**13 × `Scenario` + 3 颗独立 `Baked`**），
   不是 15。差别只在「怎么切」：census 的 15 = 12×Scenario + astra/blacklegion 两个 Baked + leviathan 的 `Particle Effects`；
   导出里 leviathan 的 `Particle Effects` **是 `Scenario` 的子件**（`Scenario` 子树 = 154 节点），而 EC 的 `Baked` **是独立根**。
   ⇒ **两种口径描述的是同一批内容，只影响「切成几颗 prefab」，不影响完整性。**
3. 🔴 **Black Legion 的 `Scenario` 不能单独导出**：它的 `ScenarioParticleSystemToggler` 有 **4 条引用指向兄弟根
   `Battle Arena Black Legion Baked 2/Firepits/*`**（bare `fileID`，不带 guid）⇒ 拆开导就断。
   其余 15 颗**子树自足**（内引全在子树内 + 外引全带 guid）。
4. **引用解析率 100%**：16 颗根里所有 `{fileID, guid, type}` 外引**全部能在 ExportedProject 里解到文件**；
   少数「解不到」的是 Unity 内建（guid `0000000000000000e000…/f000…`），不是缺失。
   网格是真网格（`Assets/Floor.asset` 3577 顶点 · 24.7 万字节），贴图是真 PNG，材质是真 `.mat`。
5. 🔴 **不建议真去「打开 ExportedProject」**：`Library/` 不存在（**从未导入过**）、工程 5.9 GB、
   版本 6000.2.6f2 而本机只有 **6000.3.23f1**（`D:/Unity/Hub/Editor/` 实读）⇒ 首开会触发整库升级 + 4.2 GB 重导入；
   而且 **149/149 个 shader 全是 Dummy**（`//DummyShaderTextExporter`）⇒ 开进 Unity **也拿不到任何画质收益**。
   **推荐走 §五 的 (c) YAML 级抽取**（产物形状与 AssetRipper 自己写的 prefab **完全同构**，已用 5 份现成 prefab 逐字段对过）。

---

## 二、世界内容根清单（16 颗 · 建议导出集）

- **路径**：全部是场景根，**路径 = `/<根名>`**。
- **`BattlePrefab`（1137 节点 · 全 UI/HUD）与 `PlayFabHttp`（1 节点）是另外的根，不在本表**。
  ⇒ `Scenario` 的 `m_LocalPosition` **不是 0** 的 8 场要注意（见「根位」列）。

| # | 场（工程 key） | `.unity` 路径（相对 `ExportedProject/`） | 根对象名 | 节点数 | 组件统计（只列非 Transform） | Anim | 根位 (x,y,z) | 建议 prefab 名 |
|---|---|---|---|---|---|---|---|---|
| 1 | battlearena1 | `Assets/Data/Scenes/Battle Scenarios/Battle Arena 1.unity` | `Scenario` | 86 | MeshFilter 36 · ParticleSystem 34 · PSRenderer 34 · MeshRenderer 28 · MonoBehaviour 25 · BoxCollider 13 · LineRenderer 6 · MeshCollider 6 · **Animator 2** · Light 1 | **Animator×2**（Cannon 1/Cannon 2 → `Cannon 1.controller`） | 113.238, −8.695, 19.853 | `battlearena1__Scenario.prefab` |
| 2 | battlearena2 | `…/Battle Arena 2.unity` | `Scenario` | 133 | MeshFilter 74 · MeshRenderer 54 · ParticleSystem 46 · PSRenderer 46 · MonoBehaviour 37 · BoxCollider 22 · **Animator 3** · LineRenderer 3 · SphereCollider 2 · MeshCollider 2 · Light 1 | **Animator×3**（Battle Arena 2 / Battle Arena 2 Skulls / Toxic Pump，controller 全解到） | 0, −0.001, 0 | `battlearena2__Scenario.prefab` |
| 3 | battlearena3 | `…/Battle Arena 3.unity` | `Scenario` | 123 | MeshFilter 48 · MeshRenderer 46 · ParticleSystem 46 · PSRenderer 46 · MonoBehaviour 27 · MeshCollider 10 · LookAtConstraint 4 · **SpriteMask 4** · BoxCollider 2 · **Animation 1** · Camera 1 · Light 1 | **Animation×1**（Battle Arena 3 Baked） | 0, −0.001, 0 | `battlearena3__Scenario.prefab` |
| 4 | battlearenaaeldari | `…/Battle Arena Aeldari.unity` | `Scenario` | 76 | MeshFilter 43 · MeshRenderer 40 · MonoBehaviour 36 · ParticleSystem 23 · PSRenderer 23 · MeshCollider 9 · **VisualEffect 2 · VFXRenderer 2** · Capsule/Box/SphereCollider 各 1 · Camera 1 · Light 1 | 无 | 0, −0.001, 0 | `battlearenaaeldari__Scenario.prefab` |
| 5 | battlearenaastramilitarum | `…/Battle Arena Astra Militarum.unity` | `Scenario` | 40 | ParticleSystem 18 · PSRenderer 18 · MonoBehaviour 12 · LineRenderer 9 · SpriteRenderer 9 · Light 1 | 无 | 113.238, −8.695, 19.853 | `battlearenaastramilitarum__Scenario.prefab` |
| 6 | battlearenaastramilitarum | （同上） | `Battle Arena Astra Militarum Baked` | 84 | MeshFilter 81 · MeshRenderer 81 · MeshCollider 26 · MonoBehaviour 26 · ParticleSystem 2 · PSRenderer 2 · **Animation 1** | **Animation×1** | 0, 0, 0 | `battlearenaastramilitarum__Baked.prefab` |
| 7 | battlearenablacklegion | `…/Battle Arena Black Legion.unity` | `Scenario` | 38 | ParticleSystem 35 · PSRenderer 35 · MonoBehaviour 6 · Light 1 | 无 | 0, 0, 0 | 🔴 **必须与 #8 合并成一棵**（见 §三·3） |
| 8 | battlearenablacklegion | （同上） | `Battle Arena Black Legion Baked 2` | 70 | MeshFilter 51 · MeshRenderer 47 · MonoBehaviour 16 · MeshCollider 9 · BoxCollider 5 · ParticleSystem 4 · PSRenderer 4 · **SkinnedMeshRenderer 2** · **Animation 1** | **Animation×1** | 0, 0, 0 | 同上（合并后命名 `battlearenablacklegion__Scenario.prefab`） |
| 9 | battlearenadarkangels | `…/Battle Arena Dark Angels.unity` | `Scenario` | 97 | MeshFilter 57 · MeshRenderer 57 · ParticleSystem 34 · PSRenderer 34 · MonoBehaviour 13 · MeshCollider 5 · **Animation 2** · Camera 1 · Light 1 | **Animation×2**（Baked 子树 + Directional Light） | 113.238, −8.695, 19.853 | `battlearenadarkangels__Scenario.prefab` |
| 10 | battlearenaemperorschildren | `…/Battle Arena Emperors Children.unity` | `Scenario` | 21 | ParticleSystem 13 · PSRenderer 13 · MonoBehaviour 5 · MeshFilter 2 · MeshRenderer 2 · Camera 1 · Light 1 | 无 | 113.238, −8.695, 19.853 | `battlearenaemperorschildren__Scenario.prefab` |
| 11 | battlearenaemperorschildren | （同上） | `Battle Arena Emperor's Children Baked` | 79 | MeshFilter 78 · MeshRenderer 78 · **Animation 1** | **Animation×1** | 0, 0, 0 | `battlearenaemperorschildren__Baked.prefab` |
| 12 | battlearenagenestealers | `…/Battle Arena Genestealers.unity` | `Scenario` | 89 | MeshFilter 46 · MeshRenderer 46 · ParticleSystem 35 · PSRenderer 35 · MonoBehaviour 14 · MeshCollider 8 · **Animation 1** · Camera 1 · Light 1 | **Animation×1** | 113.238, −8.695, 19.853 | `battlearenagenestealers__Scenario.prefab` |
| 13 | battlearenaleviathan | `…/Battle Arena Leviathan.unity` | `Scenario` | 154 | ParticleSystem 45 · PSRenderer 45 · MeshFilter 43 · MeshRenderer 42 · MonoBehaviour 25 · MeshCollider 9 · **Animator 2** · RectTransform 1 · **SkinnedMeshRenderer 1** · **PlayableDirector 1** · VisualEffect 1 · VFXRenderer 1 · BoxCollider 1 · Light 1 | **Animator×2**（`Battle Arena Leviathan Baked`→controller；`Bunker_door` **无 controller**） | 0, 0, 0 | `battlearenaleviathan__Scenario.prefab` |
| 14 | battlearenasororitas | `…/Battle Arena Sororitas.unity` | `Scenario` | 139 | MeshFilter 81 · MeshRenderer 81 · PSRenderer 49 · ParticleSystem 49 · MonoBehaviour 15 · MeshCollider 10 · SpriteRenderer 3 · Camera 1 · Light 1 | 无 | 113.238, −8.695, 19.853 | `battlearenasororitas__Scenario.prefab` |
| 15 | battlearenaspacewolves | `…/Battle Arena Space Wolves.unity` | `Scenario` | 91 | MeshFilter 54 · MeshRenderer 54 · ParticleSystem 31 · PSRenderer 31 · MonoBehaviour 12 · Camera 1 · Light 1 | 无 | 113.238, −8.695, 19.853 | `battlearenaspacewolves__Scenario.prefab` |
| 16 | battlearenatauviorla | `…/Battle Arena Tau Viorla.unity` | `Scenario` | 239 | ParticleSystem 163 · PSRenderer 163 · MeshFilter 50 · MeshRenderer 49 · MonoBehaviour 32 · SpriteRenderer 11 · MeshCollider 9 · **Animation 3** · Camera 1 · SphereCollider 1 · Light 1 | **Animation×3**（Railgun Turret 1/2/BIG） | 113.238, −8.695, 19.853 | `battlearenatauviorla__Scenario.prefab` |

**合计 16 颗根 / 1 559 节点。** 每颗根的直接子件名见 §七·E（`Scenario` 下必定是 `Directional Light` / `Particles*` / `Sun flare` / `… Baked` 这几类）。

> ⚠️ 表里 8 个 `Scenario` 的 `m_LocalPosition = (113.238, −8.695, 19.853)`，另 3 个是 `(0, −0.001, 0)`、2 个 `(0,0,0)`
> —— 这是**逐场不同**的一档值（不是同一个常量）。存成 prefab 时若要「贴地可复用」，**建实例时把根位清零**。

### 与「15 根」的映射（差额说明）

| census 的 15 根（`J-A…:106`） | AssetRipper 导出里实际是什么 | 差 |
|---|---|---|
| 12 × `Scenario`（不含 leviathan） | **13 × `Scenario`**（含 leviathan） | leviathan **有** `Scenario` 根 |
| `Battle Arena Astra Militarum Baked` | 独立根 ✅ | 一致 |
| `Battle Arena Black Legion Baked 2` | 独立根 ✅ | 一致 |
| leviathan `Particle Effects` | **`Scenario` 的子件**（22 个子件，`Scenario` 子树 154） | 不是根 |
| ——（census 说 EC 的 Baked 是 `Scenario` 子件 · `:103`） | **EC 的 `Battle Arena Emperor's Children Baked` 是独立根**（78 子件） | 多 1 颗 |

⇒ **按导出实际 = 16 颗；若要凑「一颗 prefab = 一场完整世界」，合并规则是 13 场各 1 颗**（astra / blacklegion / EC 的 Baked 并回 `Scenario` 下，三者的根变换都是单位阵，合并无损）。

---

## 三、三处必须知道的结构事实

### 3·1 每场的根集合（含被排除的两个）

| 根 | 出现场次 | 节点数 | 处置 |
|---|---|---|---|
| `BattlePrefab` | **13/13** | **1 137（每场一模一样）** | ⛔ 不导 —— 那是战斗 HUD / UI 树（RectTransform 988 · MonoBehaviour 1685） |
| `PlayFabHttp` | 4（arena1 / astra / sororitas / tauviorla） | 1 | ⛔ 不导 |
| `Scenario` | 13/13 | 21 – 239 | ✅ 导 |
| `Battle Arena … Baked`（独立根） | 3（astra / blacklegion / EC） | 70 / 84 / 79 | ✅ 导（blacklegion 要合并） |

### 3·2 子树自足性（可不可以「整棵抠出来」）

15/16 **自足**。唯一例外：

```
Battle Arena Black Legion.unity  /  Scenario  /  Particle Effects  (MonoBehaviour 4090 = ScenarioParticleSystemToggler)
  particleSystems:
  - {fileID: 1702}   → Battle Arena Black Legion Baked 2 / Firepits / FirePit
  - {fileID: 1706}   → Battle Arena Black Legion Baked 2 / Firepits / FirePit / Smoke
  - {fileID: 1709}   → Battle Arena Black Legion Baked 2 / Firepits.001 / FirePit (1)
  - {fileID: 1743}   → Battle Arena Black Legion Baked 2 / Firepits.001 / FirePit (1) / Smoke
```
这 4 条是**不带 guid 的 bare `fileID`**（同场景内引用）。⇒ **Black Legion 必须把两个根合成一棵导出**，否则 prefab 里 4 条引用变空。

⚠️ **2026-10-23 试点补充（battlearena3 YAML 抽取代理实测）**：在「**丢弃 MonoBehaviour**」口径下，这 4 条引用长在被丢掉的 `ScenarioParticleSystemToggler` 上 ⇒ **不再需要合并**；代价是 **toggler 功能一并丢失**。若要保住 toggler（黑军团火坑的开关行为），**仍需合并两棵**。两种口径都已坐实，导出时按「是否保 toggler」二选一。

反向检查（子树外是否有人引用子树内）**全部为 0** —— 抠出去不会把别的对象吊死。

### 3·3 `Scenario Fillers` / `reflection camera` / `LookAtConstraint` / `SpriteMask`

- `Scenario Fillers`：只 3 场有（EC / sororitas / space wolves），都在 `Scenario` 子树内，**1–5 节点**。
- `reflection camera`：4 场（darkangels / genestealers / sororitas / space wolves），`Camera` 组件，在子树内。
- arena3 有 **4 个 `LookAtConstraint`**（type id `1183024399`）+ **4 个 `SpriteMask`**。
- aeldari 有 **2 个 `VisualEffect` + 2 个 `VFXRenderer`（VFX Graph，`m_Enabled: 0`）**，资产是 `Assets/Aeldari Dust.asset`（`VisualEffectAsset`）。leviathan 各 1 个。
  ⚠️ 这是 **VFX Graph 包**的东西；工程没装该包（只给了 `Unity.VisualEffectGraph.Runtime.dll` 插件）。

---

## 四、AssetRipper 工程现状（能不能在本机用 Unity 打开）

| 项 | 现读值 | 判 |
|---|---|---|
| `ProjectSettings/ProjectVersion.txt` | `m_EditorVersion: 6000.2.6f2` | ⚠️ 本机只有 `2022.3.62t15` / `6000.3.23f1` ⇒ **首开会走版本升级** |
| `Packages/manifest.json` | **只有 31 个 `com.unity.modules.*`，一个功能包都没有**（无 ugui / TMP / URP / Addressables / VFX） | 见下「替代方案」 |
| ugui / TMP / URP / VFX / Addressables | **全部以 DLL 插件形式在 `Assets/Plugins/`**：`UnityEngine.UI.dll` · `Unity.TextMeshPro.dll` · `Unity.RenderPipelines.Universal.Runtime.dll`（+Core/2D/GPUDriven/ShaderLibrary）· `Unity.VisualEffectGraph.Runtime.dll` · `Unity.Addressables.dll` · `Unity.ResourceManager.dll` | 类型能编过；但**包内脚本的 guid 解不到**（UGUI 组件会变 Missing）⇒ 只影响 `BattlePrefab`，不影响这 16 颗根 |
| 第三方库 | `NaughtyAttributes` · `Newtonsoft.Json` · `DOTween` · `Cinemachine` · `Photon3Unity3D` · `PlayFab` · `Scaffold.Core.Events` · `TypeReferences` · `Coffee.*` · `spine-*` · `Steamworks` 都有 DLL | ✅ |
| `I2.Loc` | 无 DLL，但**反编译成源码在 `Assets/Scripts/Assembly-CSharp/I2/Loc/`（79 个文件）** | ✅ |
| 脚本 | **2 644 个 `.cs`**（`Assembly-CSharp` 2 292 + `Assembly-CSharp-firstpass` 351）；**全部是「字段保留、方法体清空」的桩**（例：`ScenarioParticleSpawnerBlender.cs` 里 `DoScenarioBlend(...) { }`） | ⚠️ 序列化能用，**逻辑不跑** |
| asmdef | **0 个** ⇒ 全部进默认 `Assembly-CSharp` | ⚠️ **一个文件编不过 = 整个 Assembly-CSharp 不产出 = 22 个脚本全部变 Missing** |
| shader | **149 / 149 全是 Dummy**（`//DummyShaderTextExporter`），只剩 Properties + 一个空 pass | 🔴 材质颜色/贴图在，**渲染结果不还原** |
| 网格 | 真网格（`Assets/Floor.asset`：`m_VertexCount 3577` · 247 332 B · `NativeFormatImporter mainObjectFileID: 4300000`） | ✅ |
| 贴图 | 真 PNG（`89 50 4E 47 0D 0A 1A 0A`），2 520 个 / 376 MB | ✅ |
| 材质 | 真 `.mat`（`m_CustomRenderQueue: 3000` · valid keywords `_SURFACE_TYPE_TRANSPARENT` 等都在） | ✅（shader 槽是 Dummy） |
| `Library/` | **不存在** ⇒ 从未导入过；首开 = 26 684 个文件 / 约 4.2 GB 冷导入 | 🔴 慢 |
| 与**我们工程**的 guid 交集 | **0 / 19 727**（扫 `MyGame/Assets` 10 533 个 meta 求交） | ✅ 拷进来不会撞掉我们已有资产 |
| 磁盘 | D: 剩 **582.5 GB** | ✅ 够 |

**结论（只判不打开）**：
- **「能不能打开」= 大概率能，但代价与不确定性都不低**：版本升级 + 4.2 GB 冷导入 + `Assembly-CSharp` 是否编得过**未验**
  （脚本里出现 `CodeStage` / `PolyAndCode` / `FlyingWormConsole3` / `Beebyte` / `Helpshift` / `XNode` / `RESTClient` / `FS4`… 等十几个小命名空间，
  每个只有 1–2 个 `using`，**任何一个没有提供方就是一个编译错误**）。
- **打开也拿不到画质**：shader 100% Dummy。
- **只有一件事必须靠 Unity 才能拿到**：`SaveAsPrefabAsset` 的「Unity 认证」。而这个认证可以换到**我们自己的工程**里做（见 §五 推荐）。

---

## 五、导出方案对比与推荐

### (a) 用 Unity 打开 ExportedProject → Editor 脚本 `SaveAsPrefabAsset` → 拷进我们工程

- **依赖**：能编出的 Unity 6000.3.23f1（升级 6000.2.6f2）+ **`Assembly-CSharp` 必须编译通过** + 一个 `Assets/Editor/*.cs` 导出脚本。
- **耗时**：首开冷导入 4.2 GB（粗估 20–40 min，未实测）＋ 每场一次存 prefab（秒级）。
- **风险**：
  1. 🔴 **编译风险是致命的**（不是慢，是**静默丢数据**）：只要 `Assembly-CSharp` 编不过，
     场景能开但 22 个脚本类全部不可用 ⇒ `SaveAsPrefabAsset` **把 274 个 MonoBehaviour 当 Missing 丢掉**，
     而它们的字段（混合选项 / 淡入淡出 / 粒子开关）**一去不回**。
  2. 版本升级会重序列化整个工程（不可逆地改动 `d:/2` 那份 —— 虽然有备份价值但违反「不动 d:/2」）。
  3. Shader 仍全是 Dummy，**打开不换来任何画质**。
- **优点**：Unity 亲自写 prefab ⇒ 格式 100% 合法；`m_LocalPosition` 等值由 Unity 规整。

### (a′) 「瘦转移工程」：新建空工程（用我们的 6000.3.23f1 + 我们已知可用的包集合）→ 只拷这 16 颗根的子树闭包 → 导出

- **依赖**：只拷 13 个 `.unity`（94 MB）+ 闭包 1 091 个文件（**296.8 MB**，其中 PNG 276 MB）⇒ **约 400 MB 导入**，不是 4.2 GB。
- **风险**：包和 `Assets/Plugins` 里的 DLL 会**类型重复**，要么删 DLL（脚本编不过）要么不装包（UGUI 编不过）
  ⇒ **同样受 §五(a) 的编译链牵制**。
- **适合**：只有当 (c) 在 Unity 里被证明有问题时才上。

### (c) YAML 级手工抽（不经过 Unity）★ 推荐

- **做法**：对每颗根，把「归属该子树的全部 YAML 文档」原样搬到新 `.prefab` 文件里，
  把根 `Transform.m_Father` 改成 `{fileID: 0}`，**fileID 统一做一次偏移/重编号**（避免小整数 id），其余字段原样；
  同时按 §七·D 的闭包把依赖文件**连同 `.meta` 一起**拷进目标工程（guid 保持不变 ⇒ 外引自动成立）。
- **可行性证据（本次已逐条验）**：
  1. 16 颗根里 **15 颗子树自足**（内引全在子树内、外引全带 guid）——只有 Black Legion 要合并；
  2. AssetRipper 自己写出来的 prefab **就是这个形状**：`Battle Arena 3 Baked.prefab` 由 `!u!1/4/23/33/64/65/111/114/198/199/20` 构成、
     **没有 `PrefabInstance` 文档**、根 `Transform.m_Father: {fileID: 0}`、全部 `m_CorrespondingSourceObject: {fileID: 0}`；
  3. 13 个场景里 **`PrefabInstance` 文档数 = 0**（逐场 grep）⇒ 没有嵌套 prefab 实例要展开；
  4. 外引 **100% 可解**，与我们工程 guid 交集 **0**。
- **耗时**：脚本几十秒跑完＋拷 297 MB。**不碰 Unity、不碰 `d:/2`**。
- **风险**：
  1. ⚠️ **产物没被 Unity 认证过** —— 第一次 import 才是验收（这正是「我们工程里跑一次 batch」的活）。
  2. ⚠️ **MonoBehaviour 到了我们工程会变 `Missing (Mono Script)`**（我们没这 22 个桩脚本）。
     数据仍在 YAML 里，但**一存盘就丢**。⇒ 要嘛「只当几何/粒子用、不存盘」，要嘛把 20 个桩脚本一起搬（会牵扯它们的依赖类）。
  3. ⚠️ 材质 shader 槽指向 Dummy shader ⇒ 要过一遍我们已有的 `WarpforgeShaderMap`（我们本来就在做这件事）。
  4. ⚠️ 小 fileID 重编号若做错会串引用 —— 必须**先做一次「引用闭包自检」**（本次脚本已能跑）。

### 推荐

> **主推 (c)**，理由三条：① 省掉 4.2 GB 冷导入 + 版本升级；② 不需要 `Assembly-CSharp` 编得过（**这是 (a) 唯一的致命点**）；
> ③ 产物形状与 AssetRipper 自己写的 prefab 同构（已在 5 份现成 prefab 上逐字段核过）。
>
> **同时用上现成的 4 份 Baked prefab**：`Assets/Battle Arena 3 Baked.prefab` / `…Aeldari Baked` / `…Leviathan Baked` /
> `…Adepta Sororitas Baked` **已经是 prefab 了**，可以**零抽取**直接用（见 §六·2 的同树核对结果）。
>
> **(a) 留作兜底**，触发条件 = (c) 的产物在我们工程里 import 后节点数/引用对不上。
> ⛔ **不要直接打开 5.9 GB 的 ExportedProject 来「顺便看看」** —— 4.2 GB 冷导入且 shader 全 Dummy，没有收益。

---

## 六、没查清 / 需要主对话裁定的

1. ⚠️ **census 与导出结果冲突 3 处**（我按导出实测为准，但**没有改 census** —— 红线只许写本文件）：
   - `J-A_战场素材与材质诊断.md:104`「leviathan **没有** `Scenario` 根」→ 导出里 **有**（`m_Father: {fileID: 0}`，子树 154 节点，直接子件 = `Directional Light` / `Sun flare` / `Particle Effects` / `Battle Arena Leviathan Baked`）。
   - 同文件 `:103`「EC 的 `Battle Arena Emperor's Children Baked` 是 `Scenario` 的子件」→ 导出里**是独立根**（78 子件；`Scenario` 只有 21 节点）。
   - 同文件 `:101` 把 arena1/2/3 等 9 场写成「`Scenario` 的子件是 `… Baked`」→ 导出里 **arena1 的 Baked 叫 `Scenario Battle Arena 1 Baked`**、arena2 叫 `Scenario Battle Arena 2 Baked`、**arena3 叫 `Battle Arena 3 Baked`** —— 名字前缀不统一，按字符串匹配会漏。
2. ✅ **census §四④ 的开放问题「4 份 Baked 的 battleshared 版与场包内嵌版是不是同一棵树」—— 本次已查清**：

   | Baked | 场景内子树 | 现有 `.prefab` | 结论 |
   |---|---|---|---|
   | `Battle Arena Aeldari Baked` | 44 节点 | 44 | **逐节点同名同路径，完全一致** |
   | `Battle Arena Leviathan Baked` | 99 | 99 | **完全一致** |
   | `Battle Arena 3 Baked` | 50 | 50 | **完全一致** |
   | `Battle Arena Adepta Sororitas Baked` | **80** | **82** | **不同** —— prefab 多 `Candles 32` / `Candles 33` 两个节点 |

   ⇒ 前 3 份可直接拿 prefab；sororitas 那份**要用场景内版本**（或者接受多两支蜡烛）。
3. ❓ **`Battle Arena Leviathan.prefab`（96 节点 · 无 Animator）是什么版本** —— 与场景内 `Battle Arena Leviathan Baked`（99 节点 · 带 2 Animator）**不是同一棵**（差 3 节点 + Animator）。用途未查。
4. ❓ **`Assembly-CSharp` 到底编不编得过**（方案 a 的命门）—— 需要真开一次 Unity 才知道。
   已知**没有** `UnityEngine.InputSystem` / `UnityEngine.VFX` 的 `using`（0 个文件），所以只差那几个 1–2 处的小命名空间。
5. ❓ **`Bunker_door` 的 Animator（leviathan）没有 controller** —— 原版就是空的，还是 AssetRipper 没导出？未查。
6. ❓ **子件名里的 `'` 与空格**（`'Battle Arena Aeldari '`、`'Generator '`、`Battle Arena Emperor's Children Baked`）——
   写文件路径/资源名时要处理，本次未验证 Unity 是否接受含 `'` 的 GameObject 名（应该是接受的，但没实测）。

---

## 七、证据（文件:行号 / 路径）

**A. 场景与根**
- 13 个场景：`d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Battle Scenarios/*.unity`（5 671 111 – 13 799 579 B）。
- 根判定：`Transform` / `RectTransform` 文档的 `m_Father: {fileID: 0}`（例：`Battle Arena Aeldari.unity` 的 `Scenario` 变换）。
- `SceneRoots` 文档 **0 个**（AssetRipper 不写 `!u!1660057539`）⇒ 只能按 `m_Father` 建父链，**不能靠 SceneRoots**。
- `PrefabInstance`（`!u!1001`）文档数：13 场**全为 0**。
- `BattlePrefab` 每场都是 1 137 节点（`Battle Arena 1.unity` / `…Tau Viorla.unity` 逐一相同）。

**B. 组件类型 id 对照**（本次现读映射）
`1` GameObject · `4` Transform · `224` RectTransform · `114` MonoBehaviour · `198` ParticleSystem · `199` ParticleSystemRenderer ·
`23` MeshRenderer · `33` MeshFilter · `137` SkinnedMeshRenderer · `108` Light · `20` Camera · `212` SpriteRenderer ·
`64` MeshCollider · `65` BoxCollider · `135` SphereCollider · `136` CapsuleCollider · `61` BoxCollider2D · `111` Animation ·
`95` Animator · `120` LineRenderer · `210` SortingGroup · `225` CanvasGroup · `223` Canvas · `222` CanvasRenderer ·
`331` SpriteMask · `320` PlayableDirector · **`1183024399` LookAtConstraint** · **`2083052967` VisualEffect** · **`73398921` VFXRenderer**。
（`1183024399` 见 `Battle Arena 3.unity:227989`；`2083052967` 见 `Battle Arena Aeldari.unity:172787`；`73398921` 见同文件 `:172697`。）

**C. 引用解析**
- 16 颗根的全部 `{fileID, guid, type}` 外引 **100% 命中** `Assets/**/*.meta` 的 guid 表（19 727 条）。
- 唯一「未命中」的 9 处 guid 全是 Unity 内建 `0000000000000000f000000000000000`：
  `Battle Arena 2.unity`（2 × PSRenderer `m_Mesh`）、`Battle Arena 3.unity`（4 × SpriteMask + 1）、`Battle Arena Sororitas.unity`（3 × SpriteRenderer）—— **不是缺失**。
- 解析到的目标类型分布：`.mat` 1 335 · `.asset` 902 · `.cs` 274 · `.dll` 27 · `.anim` 18 · `.prefab` 10 · `.shader` 9 · `.controller` 6 · `.png` 2 · `.playable` 2。

**D. 依赖闭包（16 颗根合计）**
1 091 个文件 / **296.83 MB**：
`.asset` 681（18.0 MB）· `.mat` 219（0.58 MB）· `.png` 121（**276.3 MB**）· `.shader` 27（0.06 MB）· `.cs` 20 · `.anim` 14（1.07 MB）· `.controller` 5 · `.dll` 2 · `.playable` 1 · `.prefab` 1。
- 与我们工程**同名**资产：PNG **131/139 = 94%** 已有、`.mat` 126/336 = 38%、`.asset`（网格）76/902 = **8%** ⇒ **主要缺的是网格**。
- 材质示例：`Assets/Material/Aeldari_Ships.mat:12` `m_Shader: {fileID: 4800000, guid: 625e9c59bd367b544b5fe09c3a5bc96e, type: 3}`；`:16-17` `m_ValidKeywords: - _SURFACE_TYPE_TRANSPARENT`。
- 网格示例：`Assets/Floor.asset:5` `Mesh:` · `:10` `indexCount: 18051` · `:25` `m_VertexCount: 3577`（文件 247 332 B）。
- 网格 meta：`Assets/Floor.asset.meta` `NativeFormatImporter: mainObjectFileID: 4300000`。

**E. 每场 `Scenario` 的直接子件（用于核对整树是否抠全）**
- arena1：`Directional Light` · `Particles`(36) · `Sun flare` · `Scenario Battle Arena 1 Baked`(47)
- arena2：`Directional Light` · `Sun flare` · `Battle Arena 2 Particles`(50) · `Scenario Battle Arena 2 Baked`(80)
- arena3：`Directional Light` · `Particle Effects`(71) · `Battle Arena 3 Baked`(50)
- aeldari：`Directional Light` · `Sun flare` · `Particle Effects`(27) · `Aeldari Ships`(2) · `Battle Arena Aeldari Baked`(44)
- astra：`Directional Light` · `Particles`(28) · `Sun flare` · `Smoke Column Right 1-5` · `Smoke Column Left 1-4`（共 12 个直接子件）
- blacklegion：`Directional Light` · `Particle Effects`(36)
- darkangels：`Directional Light` · `Particles`(26) · `reflection camera` · `Battle Arena Dark Angels baked`(62) · `… Light Shafts baked`(6)
- EC：`Directional Light` · `Particles`(17) · `Scenario Fillers`(1) · `reflection camera`
- genestealers：`Directional Light` · `Particles`(39) · `reflection camera` · `Battle Arena Genestealers Baked`(47)
- leviathan：`Directional Light` · `Sun flare` · `Particle Effects`(52) · `Battle Arena Leviathan Baked`(99)
- sororitas：`Directional Light` · `Particles`(51) · `Battle Arena Adepta Sororitas Baked`(80) · `Scenario Fillers`(5) · `reflection camera`
- spacewolves：`Particles`(33) · `Scenario Fillers`(1) · `reflection camera` · `Battle Arena Space Wolves Baked`(55)
- tauviorla：`Directional Light` · `Particles`(64) · `Sun flare` · `Battle Arena Tau Viorla Baked`(172)

**F. 工程现状**
- `ProjectSettings/ProjectVersion.txt`：`m_EditorVersion: 6000.2.6f2`
- `Packages/manifest.json`：31 个 `com.unity.modules.*`，**0 个功能包**
- `Assets/Plugins/`：`UnityEngine.UI.dll` · `Unity.TextMeshPro.dll` · `Unity.RenderPipelines.Universal.Runtime.dll` · `Unity.VisualEffectGraph.Runtime.dll` · `Unity.Addressables.dll` · `Unity.ResourceManager.dll` 等
- `Assets/Shader/*.shader`：**149 / 149** 含 `//DummyShaderTextExporter`（例 `Assets/Shader/Custom_EditorIcon.shader`）
- `Assets/Scripts/Assembly-CSharp/ScenarioParticleSpawnerBlender.cs`：`[SerializeField] private ParticleSystemAreaSpawnerController[] controllers;` + `public void DoScenarioBlend(ScenarioBlendOptions scenarioBlendOptions) { }`（方法体空 = 桩）
- 脚本规模：`find Assets -name "*.cs" | wc -l` = **2 644**；`Assembly-CSharp` 2 292；`Assembly-CSharp-firstpass` 351；`Assets/Editor/AssetRipperPatches/AudioMixerPostprocessor.cs` 1
- 无 `Library/`（`ls -d Library` → 不存在）
- 与我们工程 guid 交集 = **0**（`MyGame/Assets` 10 533 meta vs 导出 19 727 meta）

**G. 现成 prefab**
- `Assets/Battle Arena 3 Baked.prefab`：50 节点，文档类型 `!u!1 ×50 · 4 ×50 · 33 ×44 · 23 ×42 · 64 ×10 · 114 ×20 · 198 ×2 · 199 ×2 · 65 ×2 · 111 ×1 · 20 ×1`，`m_CorrespondingSourceObject` 非零 **0 处**，根 `m_Father: {fileID: 0}`（`:982`）
- `Assets/Battle Arena Aeldari Baked.prefab` 44 · `Assets/Battle Arena Leviathan Baked.prefab` 99 · `Assets/Battle Arena Adepta Sororitas Baked.prefab` 82（多 2 个 Candles）· `Assets/Battle Arena Leviathan.prefab` 96
- 13 场里 `Scenario` 的根位：113.238/−8.695/19.853 的有 8 场；`(0,−0.001,0)` 3 场（arena2/3/aeldari）；`(0,0,0)` 2 场（blacklegion/leviathan）

**H. 做法参考（不新建文件）**
- 本次所有分析都是 `python -I -X utf8 -`（stdin 脚本）跑的，**没有在 `d:/2` 或工程里落任何中间文件**。
- 可复用的解析套路：按 `^--- !u!(\d+) &(\d+)$` 切文档 → `GameObject.m_Component` 拿组件 → `Transform.m_Father/m_Children` 建树 → 根 = `m_Father == 0`。
