# 战场线 · prefab YAML 级抽取试点（`battlearena3` / `Scenario`）

> 日期 2026-10-23 · 执行代理产出
> 判据（唯一来源）= `资料/普查产出_1023b/战场线_15prefab清单.md` §二 第 3 行 + §五 (c)
> 源 = `d:/2/tools/assetripper_out/ExportedProject`（**只读**，一个字节都没改）
> 工具 = `Unity/工具/extract_arena_prefab_yaml.py`（**新增**）
> [OK] 本试点**没有跑 Unity**（按任务书），产物要 import 一次才算验收。

---

## 一、结论（先看这六条）

1. **(c) YAML 级抽取这条路走通了**：`Scenario` 子树 **123 节点 / 482 个 YAML 文档**全量抠出，
   丢掉 27 个 MonoBehaviour 后 **455 个文档**写成 `battlearena3__Scenario.prefab`（**2,707,878 B**），
   **9 条静态自检全过**（§三）。
2. **子树自足性现核为真**：抠之前闭包里「不带 guid 的 fileID 引用越出子树」= **0 处**；
   抠完之后产物里「悬空引用」= **0 处** ⇒ 没有把任何引用吊死。
3. **丢掉 27 个 MonoBehaviour**（`MonoBehaviour` 共 27 个，脚本 9 种，最多的是 `TapHitMaterialDefinition` 13 个）。
   **静态树一点没动**：节点数仍 123、父子关系全对、每一个 Transform 都在（§四）。
4. **6 个 shader 没拷**（ExportedProject 的全是 Dummy）⇒ 22 个材质 import 后 shader 槽会 Missing/品红。
   每个 shader 在我们工程里的对应关系见 §五 —— **映射已经查清，但没有动手改，等主对话裁**。
5. **闭包资产拷了 88 个 / 13.37 MB**：44 个网格 + 4 个 Sprite + 22 个材质 + 18 张贴图，`.meta` 原样带过来（guid 不变）。
6. **副产物发现（要并入清单）**：**Black Legion 不再需要「两棵根合并」** ——
   `战场线_15prefab清单.md` §三·3 说的那 4 条跨根 bare fileID 引用，**长在 `ScenarioParticleSystemToggler`
   （MonoBehaviour）身上**，而本试点的口径是丢 MonoBehaviour ⇒ 那 4 条引用跟着消失，`Scenario` 单独就能抠。
   实测 `--arena battlearenablacklegion --root Scenario --dry-run` => **9 条自检全过**（§七·1）。

---

## 二、产物清单与大小

### 2·1 顶层

| 项 | 现读值 |
|---|---|
| prefab | `d:/4/Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/battlearena3/battlearena3__Scenario.prefab` |
| 大小 / 行数 | **2,707,878 B** / 113,689 行（LF，无 BOM）· md5 `6a8977016f5494c24432da9e3020a2a8` |
| 文档数 | **455** = 123 GameObject · 123 Transform · 48 MeshFilter · 46 MeshRenderer · 46 ParticleSystem · 46 ParticleSystemRenderer · 10 MeshCollider · 4 SpriteMask · 4 LookAtConstraint · 2 BoxCollider · 1 Camera · 1 Light · 1 Animation |
| 整个目录 | **182 个文件**（含 `.meta`）· **17 MB** 磁盘占用 = prefab 2.58 MB + 拷入资产 **13.37 MB** + `.meta` 若干 |
| git | 产物目录在 `.gitignore` 的 `/Unity/MyGame/Assets/WarpforgeVFX/*` 规则下 ⇒ **不进 git**（`git status` 里看不到它）。本次新增的仓库内文件只有两个：工具 `Unity/工具/extract_arena_prefab_yaml.py` 与这份报告 |

### 2·2 拷进来的闭包资产（88 个）

| 类别 | 个数 | 大小 | 说明 |
|---|---|---|---|
| Mesh（`.asset`，`!u!43`） | **44** | 0.75 MB | 48 个 MeshFilter 里 4 个复用了内置/共享网格 |
| Sprite（`.asset`，`!u!213`） | **4** | （在上一行里） | `Up Rays` · `Debris` · `White Square` · `Large_Monolith_Mask` |
| 材质（`.mat`） | **22** | 0.05 MB | |
| 贴图（`.png`） | **18** | **12.57 MB** | 最大头 |
| 合计 | **88** | **13.37 MB** | 不含 prefab 本体（2.58 MB） |

18 张贴图：`Battle Arena Necrons Ground With Reflection Mask` · `Battle arena 1 Clouds` ·
`BattleArena3Atlas-mat-_BaseMap-atlas-0` · `Circle_Hoop` · `Debris` · `FireBallEmission` · `Glow` ·
`Large_Monolith_Mask` · `LightningTrail` · `Lightning_Random` · `Noise Combined` · `Scarab_Necron` ·
`Smoke Sprite Sheet` · `Square Smooth` · `Up Rays` · `White Square` · `Wispy Clouds` · `ring4`

### 2·3 目录布局 / `.meta` 策略

```
ArenaOriginalPrefabs/
  ArenaOriginalPrefabs.meta            <- 确定性 guid（由路径哈希）
  battlearena3.meta
  battlearena3/
    battlearena3__Scenario.prefab(.meta)
    Material/*.mat(.meta)               <- 14 个文件
    Mesh/*.asset(.meta)                 <- 4 个文件
    Texture2D/*.png(.meta)              <- 6 个文件
    <其余 .asset/.png 直接在 battlearena3/ 下>   <- 160 个文件
```

- **子路径照抄 ExportedProject 的相对路径**（`Assets/Material/x.mat` → `<out>/Material/x.mat`）——
  避免同名资产（如 `Ground.mat` 与 `Mesh/Ground.asset`）压平后撞车。
- **拷过来的资产 `.meta` 原样复制**（guid 必须保持，否则 prefab 的外引全断）。
- **新建的**（prefab 本体 + 各级目录）用**确定性 guid**：`md5("wf-arenaoriginalprefabs:" + 相对路径)`。
  重跑字节不变（实测第二次 md5 与第一次一致、prefab 的 `.meta` guid 也不变）。

---

## 三、静态自检结果（9 条全过）

| # | 检查 | 结果 | 现读值 |
|---|---|---|---|
| 1 | 产物节点数 = 原版 `Scenario` 子树节点数 | **PASS** | 123 GameObject / 123 Transform（原版子树 123） |
| 2 | `m_PrefabInstance` 非零 = 0 | **PASS** | 0 处 |
| 3 | `m_CorrespondingSourceObject` 非零 = 0 | **PASS** | 0 处 |
| 4 | 根 `m_Father: {fileID: 0}` | **PASS** | `m_Father=0` 的 Transform **恰好 1 个**（就是根 `&1000000000000002`） |
| 5 | 无 guid 的 `{fileID}` 引用全部落在产物内 | **PASS** | 悬空 **0** 处（源闭包里越出子树也是 **0** 处） |
| 6 | fileID 唯一 | **PASS** | 455 个 |
| 7 | fileID 全是大整数（≥1e15） | **PASS** | 小号 0 个（统一 `+1e15` 偏移，保持唯一且确定性） |
| 8 | PyYAML 逐文档解析 | **PASS** | 455 个文档全过 |
| 9 | 每个外引 `{fileID, guid, type}` 都能解析 | **PASS** | 74 个 guid：拷贝 70 · 仅 ExportedProject 2 · Unity 内建 2 · **解不到 0** |

**额外做的三条结构核对**（工具外的一次性核，也全过）：

- 每个 `m_Children` 项 ↔ 对方的 `m_Father` **一一对应**，错配 0 处；
- 每个 GameObject 的 `- component:` 引用都在产物里，悬空 0 处；
- 每个组件的 `m_GameObject` 都指回产物里的 GameObject，悬空 0 处；
- `m_Component: []` 出现 0 次（根 `Scenario` 丢完 MonoBehaviour 后剩 1 个 Transform，不是空）。

### 关于第 8 条的一个坑（值得留给 13 场）

Unity 只在**文件开头**写一次 `%TAG !u! tag:unity3d.com,2011:`，而 YAML 规范里
**`%TAG` 只对紧跟的那一个文档有效** ⇒ 把整个 `.prefab` 直接喂 `yaml.safe_load_all` 会报
`found undefined tag handle '!u!'`。正确做法（工具里已实现）= **按 `--- !u!` 切块、每块补回文件头再解析**。
只加 `add_multi_constructor` **不够**。

---

## 四、丢掉的 MonoBehaviour（27 个 / 9 种脚本）

| 脚本 | 个数 | 挂在哪些 GameObject 上 | 原版作用 | 丢掉的影响 |
|---|---|---|---|---|
| `TapHitMaterialDefinition` | **13** | Big Monolith · Broken Monolith 1/2 · Cube · Cube (1) · Monolith Front Left/Right · Big Monolith.004/.005 · Battle Arena 3 Baked · Props 1.001/.002 · Dynamic Monolith 1/4 | 被点击时换材质/高亮 | **静态树 0 影响**；只少「点它会有反馈」 |
| `MaterialFlickerEffect` | 4 | Ground.001/.002 · Props 1.001 · Dynamic Lights 1 | 材质闪烁 | 材质的动态闪烁不播（初始状态仍在） |
| `AnimFXController` | 2 | Lightning_Green · Lightning_Green (1) | 特效状态机 | `m_Enabled: 0`，原本就没开 |
| `DestroyByTime` | 2 | Lightning_Green · Lightning_Green (1) | 定时自毁 | `m_Enabled: 0`，原本就没开 |
| `Unity.RenderPipelines.Universal.Runtime.dll` 里的脚本 | 2 | **Directional Light** · **reflection camera** | URP 附加相机数据 | 相机的 URP 附加数据丢（相机本身在） |
| `ScenarioMaterialFader` | 1 | **Scenario（根）** | 场景整体淡入淡出 | 根上就没别的组件了；淡入不播 |
| `ScenarioParticleSystemToggler` | 1 | **Particle Effects** | 按混合选项开关一批粒子 | 见 §七·1 —— 它还顺带解掉了 Black Legion 的跨根引用 |
| `Mirror` | 1 | reflection camera | 平面镜相机 | 镜面反射不更新 |
| `MaterialChangerByQuality` | 1 | Ground | 按画质档换材质 | 只有一档材质（拷进来的那份） |

**对静态树的影响 = 零**：节点数、父子关系、层名、Transform 的 TRS **全部保持原样**；
少掉的只是「这些组件活着时才会发生的行为」。⚠️ 这些组件的**字段值没有留存**——
如果以后要做「场景淡入 / 点按变色 / 混合选项开关粒子」，**必须回 `d:/2` 的 `.unity` 重新读**
（工具重跑一次即可，源不可变）。

---

## 五、shader 映射待办（22 个材质 / 6 个 shader + 3 个内建）

**现状**：材质的 `m_Shader` guid 指向 ExportedProject 里的 **Dummy shader**（149/149 都是 `//DummyShaderTextExporter`）。
本试点**按任务书不拷 shader、也不改材质** ⇒ import 后这 22 个材质的 shader 槽会 **Missing / 品红**。
下表是「原版 shader 名 → 我们工程里的对应」，**只查证、没动**：

| 原版 shader 名 | 用它的材质 | 我们工程里的对应 | 判据 |
|---|---|---|---|
| `Universal Render Pipeline/Particles/Unlit` | **10** | **URP 包自带**（`Packages/manifest.json:8` = `com.unity.render-pipelines.universal 17.3.0`） | 只需把材质 guid 换指工程内的 URP shader；或运行时走 `WarpforgeShaderMap` |
| `Everguild/FX/Extra Color` | 3 | `UseOriginal` 白名单（走 bundle 原件）+ `Replacements` → `WarpforgeVFX/Particles/Extra Color EC` | `WarpforgeShaderMap.cs:216` / `:272` |
| `Everguild/UnlitAmbient` | 2 | `UseOriginal` + `Replacements` → `WarpforgeVFX/UnlitAmbient` | `WarpforgeShaderMap.cs:86` / `:366` |
| `Everguild/FX/Unlit UV scroll` | 2 | `UseOriginal` + `Replacements` → `WarpforgeVFX/FX/UnlitUVScroll` | `WarpforgeShaderMap.cs:76` / `:341` |
| `Everguild/FX/Specific/Necrons Rays` | 1 | `UseOriginal`（**原件在主包**里） | `WarpforgeShaderMap.cs:182` |
| `Everguild/FX/Floor Planar Reflections Grainny` | 1 | **`WarpforgeShaderMap` 里没有它的条目**；`OriginalShaderBlendTable.cs:43,144,243,351` 有它的混合表；`资料/战场13场_逐场对账_0920.md:1237` 记「已接原版」 | ⚠️ 具体解析入口**没查**（见 §六·4） |
| Unity 内建 `unity_builtin_extra` fileID **10720** | 3（`citclr_light` · `treasure_ray` · `Material/default_001_add`） | Unity 内建，不需要映射 | ⚠️ **这个 fileID 对应哪个 shader 名，没查到**（§六·5） |

---

## 六、没查清的部分（⛔ 别当成已查）

1. **Unity import 没验**（本试点不许跑 Unity）——产物是否真能被 6000.3.23f1 吃下、画面对不对，**都要等主对话跑一次**。
2. **序列化版本代差**：源 YAML 是 Unity 6000.2.6f2 时代写的（例 `ParticleSystemRenderer.serializedVersion: 6`），
   而我们工程里的 prefab 现读是 **`serializedVersion: 7`**。Unity 对「比我老的版本」有升级路径，
   **大概率没事、但没有实测**；若 import 后粒子参数有缺失，先查这一条。
3. **`LookAtConstraint`（type id `1183024399`）**：本机 `UnityEngine.AnimationModule.dll` 里**有**这个类名
   （字符串命中），所以保留了 4 个实例；但**type id 是否与我们这版 Unity 一致没验**。
   若 import 报 unknown class，第二版可用 `--drop-class LookAtConstraint` 重出。
4. **`Everguild/FX/Floor Planar Reflections Grainny` 的解析入口没查**
   —— `WarpforgeShaderMap` 的两张表里都没有它，只有 `OriginalShaderBlendTable` 有数据、`ArenaMirror.cs` 提到它。
   「已接原版」具体走哪条链（是不是 `WarpforgeShaderLoader` 按名字直接取 bundle）**没查清**。
5. **`unity_builtin_extra` fileID `10720` 的 shader 名没查到**（3 个材质用它）。它在任何 Unity 工程里都能解析，
   但**是不是原版那个内置管线 shader、在 URP 下渲染对不对**，没查。
6. **两个「仅 ExportedProject」的外引没拷**（按任务书的「只拷网格/材质/贴图」口径）：
   - `Assets/ParticlesLight.prefab` —— 4 个 ParticleSystem 的 **`light:` 字段**（Lights 模块的灯光模板）指向它；
   - `Assets/AnimationClip/Battle Arena 3 Animations.anim` —— `Animation` 组件的 `m_Animation`。
   两处在 import 后会 missing。要补：`--copy-exts anim,prefab`（工具已支持，**这次没开**）。
7. **拷过来的 `.meta` 带 `assetBundleName`**（93 个 meta 里 **76 个**非空，两个哈希值）——
   这是 AssetRipper 从 bundle 抄来的。编辑器里使用没影响，但**打 player 时会不会把资产塞进同名 bundle 没验**。
   要清的话是一行正则的事，**这次没动**（改了 .meta 就等于改产物，得先由主对话裁）。
8. **与我们工程已有资产重名 77 个**（guid 不同 ⇒ 工程里会同时存在两份）：
   主因是我们早先那条搬运链（`Assets/WarpforgeArena1/arenas/battlearena3/Models/*.obj`、`WarpforgeVFX/Materials/`）
   已经把同名网格/材质导过一遍。**这次没有去重、也没有改指向** —— 是否改走已有那份，要主对话定。
9. **跨场复用**：后续场次如果共享同一批材质（实测 `battlearena2` 有 2 个、`leviathan` 有 5 个 guid
   已经落在 `ArenaOriginalPrefabs/battlearena3/` 里），工具会**跳过重复拷贝**，那些场的 prefab 会指向
   battlearena3 目录里的副本。这是有意的（省磁盘），但**目录不能单独删**。

---

## 七、给主对话的两条额外发现

### 7·1 Black Legion 不需要「两棵根合并」了（会改清单 §三·3 的结论）

清单 §三·3 说：`Scenario` 里有个 `ScenarioParticleSystemToggler`（MonoBehaviour）带 4 条
**不带 guid 的 bare fileID**，指向兄弟根 `Battle Arena Black Legion Baked 2/Firepits/*`，所以「必须合并两棵根」。

现读：**这 4 条引用就在那个 MonoBehaviour 的 `particleSystems:` 字段里**。本试点口径丢 MonoBehaviour
⇒ 引用随之消失 ⇒ `Scenario` 单独抠**自检 9 条全过**（实测 `--arena battlearenablacklegion --root Scenario --dry-run`：
38 节点 · 6 个 MonoBehaviour 丢掉后 kept=147 · `ALL CHECKS: PASS`）。

⚠️ 但这不是「白赚」：**那个 toggler 的功能（按混合选项开关 Baked 根里的火坑粒子）一并没了**。
如果以后要恢复它，就**必须**走「两棵根合并」那条路。⇒ 结论应该写成
「**丢 MonoBehaviour 的前提下可以单独抠；要保住 toggler 就必须合并**」。

### 7·2 工具对别的场也是通的（干跑实测，都没写盘）

| 场 / 根 | 节点数 | 闭包文档 | 丢 MonoBehaviour | 资产 | 自检 |
|---|---|---|---|---|---|
| `battlearena2` / `Scenario` | 133 | 556 | 37 | 88 文件 / 44.30 MB | **PASS** |
| `battlearenaleviathan` / `Scenario` | 154 | 525 | 25 | 93 文件 / 25.93 MB | **PASS** |
| `battlearenaastramilitarum` / `Battle Arena Astra Militarum Baked` | 84 | 387 | 26 | 89 文件 / 31.51 MB | **PASS** |
| `battlearenablacklegion` / `Scenario` | 38 | 153 | 6 | —— | **PASS**（见 7·1） |

节点数与清单 §二逐行一致 ⇒ 父链遍历口径对得上。

---

## 八、怎么跑

```bash
# 本试点（已执行过，重跑字节不变）
python -I -X utf8 d:/4/Unity/工具/extract_arena_prefab_yaml.py --arena battlearena3 --root Scenario

# 只看统计、不写盘
python -I -X utf8 d:/4/Unity/工具/extract_arena_prefab_yaml.py --arena battlearena3 --root Scenario --dry-run

# 别的场（13 场逐个跑，⛔ 铁律 15：不许一把梭）
python -I -X utf8 d:/4/Unity/工具/extract_arena_prefab_yaml.py --arena battlearenaleviathan --root Scenario
python -I -X utf8 d:/4/Unity/工具/extract_arena_prefab_yaml.py --arena battlearenaastramilitarum --root "Battle Arena Astra Militarum Baked"

# 想把 .anim / 那个 ParticlesLight.prefab 也带上（这次没开）
python -I -X utf8 d:/4/Unity/工具/extract_arena_prefab_yaml.py --arena battlearena3 --root Scenario --copy-exts anim,prefab
```

常用开关：`--dry-run`（不写盘）· `--no-copy`（只出 prefab）· `--zero-root-position`（根位清零，见下）·
`--drop-class X`（多丢一种组件）· `--out-dir` / `--name` · `--json-out FILE`（摘要写 JSON）。

**根位**：`Scenario` 的 `m_LocalPosition` 现读 `(0, -0.001, 0)`，**工具原样保留**（清单 §二 提醒过：
8 个场的 `Scenario` 根位是 `(113.238, -8.695, 19.853)`，逐场不同）。要「贴地可复用」就加 `--zero-root-position`。

退出码：**9 条自检全过 = 0，否则 1**（可以直接串进脚本）。

---

## 九、证据（文件:行号 / 路径）

- 源场景：`d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Battle Scenarios/Battle Arena 3.unity`
  （7,458,958 B · 5,426 个文档 · **无 stripped 文档**、**`PrefabInstance` 非零 0 处**、**`m_CorrespondingSourceObject` 非零 0 处**）
- 根：`Scenario` 的 Transform `fileID 1262` · GameObject `fileID 2` · `m_LocalPosition (0, -0.001, 0)` ·
  `m_Children` = `Directional Light` / `Particle Effects`(14) / `Battle Arena 3 Baked`(42)
- 模板 prefab：`d:/2/tools/assetripper_out/ExportedProject/Assets/Battle Arena 3 Baked.prefab`
  （`m_Father: {fileID: 0}` 在 `:982`；224 个文档；全部 `m_PrefabInstance: {fileID: 0}`）
- shader 映射：`d:/4/Unity/MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs:76, :86, :182, :216, :272, :341, :366`
  · `OriginalShaderBlendTable.cs:43,144,243,351` · `资料/战场13场_逐场对账_0920.md:1237`
- URP 包：`d:/4/Unity/MyGame/Packages/manifest.json:8`
- 工具：`d:/4/Unity/工具/extract_arena_prefab_yaml.py`
