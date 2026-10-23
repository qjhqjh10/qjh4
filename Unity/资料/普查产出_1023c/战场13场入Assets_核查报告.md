# 战场线 · 13 场场景素材入 Assets 后的静态核查（代理 AR）

> 日期 2026-10-23 · **只读核查**（未跑 Unity / player、未改任何文件）
> 快照时间 **19:48**：13 颗 prefab 的 md5 此刻仍与 manifest 逐条相等；目录内最后一次写入是 **19:12**（Unity 导入收尾），此后无变动。
> 对象：`Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/`（13 个 arena 子目录 · 2 328 文件 · 1 138 个资产 + 1 190 个 `.meta`）
> 脚本与逐条中间数据：`_tmp_view/ar_check/*.py` / `*.json`（**`dangling.json` 里有逐条 `old guid → 现 guid` 映射，修的时候直接用**）

---

## ① 一句话结论

**结构完整、引用半坏 —— 现在还不能直接用。**
13 颗 prefab **全部完好**（每颗单根 `Scenario`、节点数与 manifest 逐场相等、md5 13/13 未变、**0 处丢脚本**），
但引用层有 **74 个悬空 guid / 234 处引用**：**9/13 场的 prefab 有 77 个材质槽悬空、54 个材质有 69 个贴图槽悬空、38 个材质的 shader 为 null**。

🔴 **根因不是「没拷全」，而是「同一份原版资产被拷进了多个 arena 目录」⇒ guid 撞车 ⇒ Unity 重编了 127 个 guid，而 prefab / 材质里的引用没有跟着改。**
其中 **40 个被改号的 guid 仍有引用**（资产就在同目录里、换了号），另外 **34 个是真正没落盘的依赖**（18 个 shader + 9 个动画 clip + 5 个 controller + 1 个 timeline + 1 个灯模板 prefab）。

---

## ② 13 场完整性表

判据：`_tmp_view/arena_prefabs_out/_manifest.tsv`（`copied_files` 列）对 `Assets/.../<arena>/` 现状；md5 对 manifest 最后一列。

| arena | 资产数(不含 prefab) | manifest copied | 差 | 节点数 manifest/实测 | prefab md5 | 悬空 guid | 悬空引用(去重 file×guid) |
|---|---|---|---|---|---|---|---|
| battlearena1 | 66 | 66 | 0 | 86 / 86 | ✅ 1c15b63f… | 14 | 15 |
| battlearena2 | 88 | 88 | 0 | 133 / 133 | ✅ 1188e681… | 29 | 34 |
| battlearena3 | **88** | **0** | **+88（见注）** | 123 / 123 | ✅ 6a897701… | 4 | 4 |
| battlearenaaeldari | 88 | 88 | 0 | 76 / 76 | ✅ e002fe7d… | 16 | 17 |
| battlearenaastramilitarum | 103 | 103 | 0 | 124 / 124 | ✅ 21893d7c… | 7 | 7 |
| battlearenablacklegion | 72 | 72 | 0 | 108 / 108 | ✅ e58b9a32… | 8 | 10 |
| battlearenadarkangels | 86 | 86 | 0 | 97 / 97 | ✅ 7618f2c3… | 5 | 5 |
| battlearenaemperorschildren | 99 | 99 | 0 | 100 / 100 | ✅ ac5c74ad… | 3 | 4 |
| battlearenagenestealers | 72 | 72 | 0 | 89 / 89 | ✅ 6a73165a… | 8 | 8 |
| battlearenaleviathan | 93 | 93 | 0 | 154 / 154 | ✅ f15fc08f… | 25 | **38** |
| battlearenasororitas | 107 | 107 | 0 | 139 / 139 | ✅ 6ac1921d… | 7 | 8 |
| battlearenaspacewolves | 69 | 69 | 0 | 91 / 91 | ✅ b6432c32… | 5 | 5 |
| battlearenatauviorla | 94 | 94 | 0 | 239 / 239 | ✅ f7852925… | 5 | 6 |
| **合计** | **1 125** | **1 037** | +88 | **1 559 / 1 559** | **13/13 ✅** | **74**(全局去重) | **161** |

- ✅ **13/13 的 prefab 字节数也与 manifest 相等**（例：b1 = 1 984 067 B）⇒ Unity 导入**没有改写 prefab**，`md5 相同` 是「内容真没变」而不是「规范化后碰巧相同」。
- 注（battlearena3）：manifest 记 `copied_files=0 / already_ours=70`，但目录里实有 **88 个资产**（另 1 个 prefab）——
  这 88 个是 **2026-10-23 试点（`资料/普查产出_1023b/战场线_prefabYAML试点_battlearena3.md:22`）**先拷进 Assets 的，
  不是 t68 staging 的产物（staging 的 b3 目录只有 prefab + meta 两个文件）。所以 b3 **不在那 127 个改号里**（它先落地、占住了原 guid）。
- 其余 12 场：**资产数与 manifest `copied_files` 逐一相等**，拷贝环节没漏。
- 内容层面：staging 的 1 910 个文件与 Assets 现状 **逐字节相同**；226 个不同（95 个 `.mat` + 131 个 `.meta`）已逐个判过 ——
  **全是 Unity 规范化**（`m_Ints: {}`→`[]`、补一条 URP `AssetVersion` MonoBehaviour、meta 补 `internalIDToNameTable`/去 `timeCreated`），**没有语义改动**。

---

## ③ 引用可解析性（968 个 `.prefab`/`.mat`/`.asset` 逐文件抽 `guid:`）

**总量 1 687 个（文件 × guid）引用对**：同场解析 **1 071** · 解析到**别的 arena 目录 112** · 解析到工程别处 **111** · 包内（PackageCache）**210** · Unity 内建 **22** · **悬空 161**（占 9.5%）。

### 3·1 按「引用方 → 目标类型」分类（**按出现次数**，合计 234）

| 引用方 | 目标类型 | 次数 | 涉及文件数 | 性质 |
|---|---|---|---|---|
| `.prefab` | 材质 | **77** | 9 | 资产在、guid 被改号 |
| `.mat` | 贴图 | **69** | 54 | 资产在、guid 被改号 |
| `.asset` | 贴图 | 2 | — | 资产在、guid 被改号 |
| `.asset` | VFX Graph 资产 | 8 | 4 | 资产在、guid 被改号 |
| `.prefab` | VFX Graph 资产 | 2 | — | 资产在、guid 被改号 |
| `.prefab` | 贴图 | 2 | — | 资产在、guid 被改号 |
| `.mat` | **shader** | **38** | 38 | **真的没有**（见 ④） |
| `.prefab` | 动画 clip | 18 | 7 | **真的没有** |
| `.prefab` | 灯模板 prefab | 10 | 2 | **真的没有**（`ParticlesLight.prefab`） |
| `.prefab` | AnimatorController | 6 | 3 | **真的没有** |
| `.prefab` | Timeline playable | 1 | 1 | **真的没有** |
| `.prefab` | 其它（timeline 绑定） | 1 | 1 | **真的没有** |
| **合计** | | **234** | | 改号 160 + 真缺 74 |

### 3·2 按「能不能在工程里找到」

| 类 | guid 数 | 说明 |
|---|---|---|
| 能在工程里找到 | 1 526 引用对 | 其中 **112 对指向别的 arena 目录**（跨场依赖，内容逐字节相同，功能无碍但结构脏） |
| **找不到 —— 资产在、Unity 改了号** | **40** | 文件就在同目录（或别的 arena），只是 `.meta` 的 guid 变了；**旧 guid 在全工程已不存在** |
| **找不到 —— 真缺** | **34** | 18 shader + 9 动画 clip + 5 controller + 1 timeline + 1 灯模板 |
| Unity 内建（**已单独归类，不算缺口**） | 22 引用对 | `…f000…`（默认资源）+ **`…e000…`（builtin extra，68 处 `m_Mesh` 指向内建 Cube/Sphere/Plane/Quad）** |

> ⚠️ `0000000000000000e000000000000000` 一度被我误判成缺口：全工程 **100 个 prefab** 都在用它（内建网格），**属 Unity 内建，不是缺口**。

### 3·3 前 10 个悬空例子（按引用次数）

| guid | 次数 | 修得回去吗 | 目标 |
|---|---|---|---|
| `d44ceea2…` | 15 | ✅ 同目录已有（新号 `b370f889…`） | `spacewolves/LargeFlame03.mat` |
| `1ddb9e6d…` | 13 | ✅ `c8f86bcd…` | `spacewolves/Glow Sphere 01.mat` |
| `1c566b3a…` | 10 | ❌ 真缺 | `ParticlesLight.prefab`（LightsModule） |
| `2ef9073a…` | 9 | ❌ 真缺 | shader `Everguild/Misc/Unlit shadows receiver` |
| `8bc71054…` | 8 | ✅ `5b53cf37…` | `leviathan/wispySmoke.mat` |
| `56bdd9cf…` | 8 | ✅ `9fed8e38…` | `genestealers/Smoke Sprite Sheet Soft.mat` |
| `20f67c2c…` | 7 | ✅ `19a74974…` | `leviathan/wispySmoke.png` |
| `1a4eec26…` | 6 | ✅ `6522234d…` | `leviathan/wispySmoke Additive Soft.mat` |
| `521934c8…` | 6 | ✅ `0d266693…` | `sororitas/Sand Storm.mat` |
| `c58c12a6…` | 6 | ✅ `01eb9558…` | `spacewolves/LargeFlame01.png` |

---

## ④ shader 缺口：**18 个 guid / 17 个 shader 名 / 38 个材质**

（`_unresolved.md` 记的是 staging 口径 **17 guid / 36 材质**；**b3 的试点目录不在 staging 里，多出 2 个**，所以现读数 = 18/38。）

| # | 原版 shader 名 | 材质数 | 在 `UseOriginal` 白名单? | 用在哪个场 |
|---|---|---|---|---|
| 1 | `Everguild/Misc/Unlit shadows receiver` | **9** | ❌ | b1/b2/astra/blacklegion/darkangels/EC/genestealers/leviathan/spacewolves 的 **Ground/Floor** |
| 2 | `Everguild/FX/Tyranids/Pulsating Mesh` | 6 | ❌ | leviathan |
| 3 | `Everguild/FX/Tyranids/Tyranid Tentacle` | 4 | ❌ | leviathan |
| 4 | `Everguild/FX/Floor Planar Reflections Grainny` | 3 | ❌ | **b3** / aeldari / sororitas 的地面 |
| 5 | `Everguild/FX/Alpha Mask One Layer  Color Ramp` | 2 | ❌（白名单里只有 `… One Layer`，**后缀不同不算命中**） | b2 / darkangels |
| 6 | `Hidden/VFX/Aeldari Dust/System/Output Particle Unlit Quad` | 2 | ❌ | aeldari / leviathan |
| 7 | `Everguild/FX/Specific/Tau Generator Energy` | 2 | ❌ | tauviorla |
| 8 | `Everguild/FX/Simple Fake Water` | 1 | ❌ | b2 |
| 9 | `Everguild/FX/Specific/Necrons Rays` | 1 | ✅ | **b3** |
| 10 | `Hidden/VFX/Aeldari Portal/System/Output Particle Unlit Quad` | 1 | ❌ | aeldari |
| 11 | `Hidden/VFX/Aeldari Portal/System (3)/Output ParticleStrip Unlit Quad` | 1 | ❌ | aeldari |
| 12 | `Hidden/VFX/Aeldari Portal/System (4)/Output Particle Unlit Quad` | 1 | ❌ | aeldari |
| 13 | `Hidden/VFX/Aeldari Portal/System (5)/Output Particle Unlit Quad` | 1 | ❌ | aeldari |
| 14 | `Everguild/FX/Vortex` | 1 | ✅ | aeldari |
| 15 | `Spine/Special/HiddenPass` | 1 | ✅ | darkangels |
| 16 | `Everguild/FX/Burning` | 1 | ✅ | sororitas |
| 17 | `Everguild/FX/Floor Planar Reflections Grainny Vertex color shadow mask` | 1 | ❌ | tauviorla 地面 |
| 18 | （同 #5 另一个原 guid：`fc85a458…`，同名不同 guid） | 1 | ❌ | b2 |

**白名单只覆盖 4 / 18**（`HiddenPass` · `Vortex` · `Burning` · `Necrons Rays`）。
🔴 **而且白名单在这件事上【不构成修复】**：`WarpforgeShaderMap.TryResolve` 是**按名字**去 bundle 取件，
而这里是**工程内 `.mat` 资产的 `m_Shader` guid 解析不到**（导入即 null ⇒ 品红/Missing）。
现核：**全仓没有任何 `.cs` 引用 `ArenaOriginalPrefabs`**，也**没有 `AssetPostprocessor` 去重绑材质** ⇒ **没有任何东西会在加载时按名字补 shader**。
（4 个白名单条目的价值只在：将来把 shader 抽成工程资产 / 运行时加载时「名字对得上」。）

---

## ⑤ GUID 冲突现状

| 项 | 数 | 说明 |
|---|---|---|
| **现在仍冲突的 guid（同一 guid ≥2 个 `.meta`）** | **0 组** | 扫 `Assets` 11 733 + `Library/PackageCache` 12 394 个 meta；含跨 Assets×PackageCache 的 0 |
| **被 Unity 改过号的 `.meta`** | **127** | 与导入日志 `Assigning a new guid` **127 条**逐条相等；与 staging 对比也是 127 |
| 旧 guid **彻底消失**（全工程没有资产再用它） | 90 / 127 | 其中 **40 个仍被自己目录里的 prefab/材质引用** ⇒ 悬空 |
| 旧 guid **被兄弟副本占住**（引用仍能解析，指向别的 arena） | 37 / 127 | 内容逐字节相同，无功能性错误 |
| 冲突是否只发生在 13 场之间 | **是** | 日志 127 条冲突的对手方路径**全部**在 `ArenaOriginalPrefabs/` 下（254/254 行命中），没有一条撞到工程原有资产 |
| 撞车根源 | 57 个 guid 被 staging **同时拷进 2～6 个 arena 目录** | 这 57 组**逐组 md5 全相同**（同一份原件），直方图：2 份 ×72、3 份 ×27、4 份 ×6、5 份 ×17、6 份 ×5 |

---

## ⑥ 目录卫生

| 检查 | 结果 |
|---|---|
| 缺 `.meta` 的资产 | **0** |
| 孤儿 `.meta` | **0**（52 个「看起来孤儿」的是 **目录 meta**：13 个 arena + 39 个 `Material/`·`Mesh/`·`Texture2D/`，正常） |
| 空目录 | **0** |
| 混进 `_json` / `_logs` / `.tsv` / `.md` | **0**（staging 根目录那几个文件**没有**被搬进 Assets） |
| 扩展名构成 | `.asset` 694（网格等）· `.mat` 261 · `.png` 170 · `.prefab` 13 |
| ⚠️ 卫生瑕疵 | **466 个 `.meta` 带原版 `assetBundleName`**（两个哈希：`8a81dd2e…` 373 个、`a57b3f2f…` 93 个）—— 正常 Play/打包不受影响，**一旦走 AssetBundle 会被塞进原版的包名** |

---

## ⑦ 必须修的清单（按严重度）

### P0-1 🔴 127 个 guid 被 Unity 改号，**40 个仍被引用** ⇒ 161 处悬空
- **病灶**：同一份资产被拷进了 2～6 个 arena 目录 ⇒ Unity 保留一个号、其余改号 ⇒ **prefab/材质里的 `guid:` 没跟着改**（prefab 字节未变，可证）。
- **受害面**：**9 / 13 颗 prefab**（b2·blacklegion·leviathan·sororitas·spacewolves 各 10～14 处，genestealers 9、b1 6、aeldari 4、astra 4；
  **b3 / darkangels / EC / tauviorla 干净**）+ **54 个材质**（69 处贴图）+ 4 个 VFX `.asset`（8 处）。
- **修法（建议，二选一，都别再靠「重跑一次导入」）**：
  1. **首选 = 去重**：每个 guid 只保留**一份**副本（建议挪到 `ArenaOriginalPrefabs/_shared/`），把 `.meta` 的 guid **改回原 guid**，再把各场引用统一指过去 —— 一次性消掉 112 处跨场依赖；
  2. **次选 = 改引用**：按 `_tmp_view/ar_check/dangling.json` 的 `old → 现 guid` 逐条改写 prefab / `.mat` / `.asset` 里的 `guid:`（40 条映射已备好，**同场文件优先**）。
- ⚠️ 两条都不能用「整目录再拷一遍」——那会再撞一次号。

### P0-2 🔴 38 个材质 shader 为 null（18 guid / 17 个名字）
- 其中 **9 个是各场的地面材质**（`Unlit shadows receiver`）⇒ 直接实例化时**整个地面会是品红/Missing**。
- **修法**：按既定路（`工具/extract_missing_shaders.py` 一路）把这 18 个 shader 从随包 bundle 抽成**工程内 `.shader` 资产**，再把 38 个材质的 `m_Shader` 绑上去。
  白名单只覆盖 4 个、且**当前没有代码会按名字兜底**（见 ④）。

### P1 真缺的依赖（34 guid / 74 处，**都不是拷贝失误，是闭包口径没含它们**）
| 缺什么 | guid 数 | 处数 | 影响 |
|---|---|---|---|
| 动画 clip（`Genestealers Scene Animations` / `Black Legion Scene Animation` / `Battle Arena 3 Animations` / `Astra Militarum Scene Animations` / `Dark Angels Scene animations` / `Emperor's Children Scene Animations` / `Railgun Turret 1·2` / `Railgun BIG`） | 9 | 18 | **7 颗 prefab** 的 `Animation` 组件不播（炮塔/场景待机） |
| AnimatorController（`Cannon 1` / `Battle Arena 2` / `Dynamic Skulls` / `Pump 1` / `Battle Arena Leviathan`） | 5 | 6 | **3 颗**（b1/b2/leviathan）的 7 个 `Animator` 无 controller |
| Timeline `Leviathan Bunker Door Animation.playable` | 1 | 2 | leviathan 的 `PlayableDirector` 空 |
| `ParticlesLight.prefab`（LightsModule 的灯光模板） | 1 | 10 | b2 / b3；**8 处模块 `enabled: 0`（无影响）· 2 处 `enabled: 1`（这两颗粒子不会发光）** |

### P2 结构与卫生
- **112 处跨 arena 引用**（例：b1 的 prefab 指向 `battlearena3/Material/Embers 1.mat`）—— 内容相同、功能无碍，但**每个场都不是自包含的**；随 P0-1 的去重一起做掉最省事。
- **466 个 `.meta` 带原版 `assetBundleName`** —— 若哪天走 AssetBundle 会按原版包名分组，建议清空。
- **0 处丢脚本**（105 个 `m_Script` 全部指向 URP 的 `AssetVersion.cs`；13 颗 prefab **MonoBehaviour = 0**、与 manifest `dropped_mono=301` 口径一致）⇒ **不存在「加载时组件丢失」的风险**，但同时说明**这些 prefab 只有纯视觉树，没有任何战场逻辑组件**。

### P3 文档订正（铁律 5 · 只报不改，正本归主对话）
- `资料/普查产出_1023b/战场线_prefabYAML试点_battlearena3.md:20` 与 `:133` 写「6 个 shader 没拷 ⇒ **22 个材质** import 后 Missing」——
  **实测 b3 只有 2 个材质 shader 悬空**（`Material/Ground.mat` = `Floor Planar Reflections Grainny`、`Necrons Flying Monolyth Rays.mat` = `Necrons Rays`），
  其余 20 个材质在试点里已被 `map_arena_prefab_shaders.py` 绑到自建/内置 shader 上。**那句话是「映射前」的预判，不是现状**。

---

## ⑧ 没查清的

1. **t68 把 staging 搬进 Assets 的脚本没找到**（`工具/` 下只有 `extract_arena_prefab_yaml.py`，`_tmp_view/_sync_arenas.sh` 是战场重建链、与本次无关）⇒
   「b3 的 88 个资产为何 mtime 是 18:55 而不是 12:30」**查不到**。搜过：`Unity/工具/`（按 `ArenaOriginalPrefabs` 全量 grep）、`_tmp_view/` 下 `*sync*` / `*copy*`。
2. **「null shader ⇒ 品红」是 Unity 行为的推断，没跑过 Unity 验收**（任务书禁止启动第二个实例）。要坐实得在同步点渲一张。
3. **这 13 颗 prefab 目前没有任何消费者**（全仓 0 处 `.cs` 引用）⇒ 「用起来对不对」除静态引用外无判据；是否会替换现有 `Resources/ArenaPrefabs/*.prefab` 那套（ArenaBuilder 重建的那套）**不在本任务范围**，未查。
4. **b2 那 2 个 `enabled: 1` 的 LightsModule 是否真会亮**（原版靠 `ParticlesLight.prefab` 出灯）—— 缺模板，静态判不了画面对不对。
5. **127 个改号里 90 个旧 guid 彻底消失** —— 余下 50 个「消失了但没人引用」的资产**是否还有别的用途**（例如别处按 guid 保存的引用）**没全局查**（只查了这 13 个目录内的 968 个文件）。
