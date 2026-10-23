# 战场线 · `T00·13 场引用修复` 最后一跳 `P0-2`（shader 运行时重绑）执行报告

> 2026-10-23 · 执行子代理（**未跑 Unity / 未跑 player / 未动 git / 未改 `Battle/**`·`RuleEngine/**`·`Shell/**`**）
> 前置（已完成，本轮不重做）：`资料/普查产出_1023d/战场_T00引用修复_执行报告.md`（57 逻辑资产去重 · 40 个 guid 复原 · 87 副本删除 · 跨场引用 0 · 381 个 `assetBundleName` 清空）
> 判据：`..._执行报告.md` §2（18 shader / 37 材质）· `资料/任务线/战场线/准则与做法.md` §六 坑 2 · `Assets/WarpforgeVFX/ArenaOriginalPrefabs/_shared/arena_shader_guid_map.tsv`（正本表）
> 备份：`d:/4/_tmp_view/p02_rebind/backup/<场>__Scenario.prefab`（**改动前逐字节副本**，13 份）

---

## 〇、一句话结论

**37 个材质（18 行 guid / 17 个原版 shader 名）的运行时重绑做完了**：新增 `ArenaOriginalShaderRebind`（挂在 13 颗 prefab 根上，`Awake` 触发）+ `ArenaOriginalShaderIndex`（生成 C# 静态表，运行期零 I/O）；**13 颗 prefab 的 YAML 只加了「根上一个组件 + 一条 `m_Component`」**（其余逐字节未动，脚本已验）；验收探针 `ArenaOriginalShaderRebindProbe.Run` **留给主对话跑**（本代理按纪律不跑 Unity），期望 `37/37 解析到`（其中 32 个能从 13 颗 prefab 树里实例化到、5 个只在 VFX Graph 资产里）+ 13 颗 `unresolved=0 / unknown=0` + 负控数出 69 个槽。

---

## 一、做法（三条路线怎么选的）

### 1·1 修法：**加载时把 shader 绑回去**，不是「抽包」也不是「抽 `.shader` 资产」

- shader 本体**已经在随包 `wf_*.bundle` 里**（18/18，前一代理逐包实读）⇒ 不需要重打任何包；
- ⛔ **没有**把 shader 抽成工程内 `.shader` 资产 —— 与本线 §六 坑 2（bundle shader 落不了工程资产）直接冲突；
- 走既有解析链 `WarpforgeShaderMap.TryResolve(原版名)`：它最后一步本来就去 `WarpforgeShaderLoader` 的已加载包里按名取件
  ⇒ 18 个名字**逐条查过**：4 个在 `UseOriginal` 白名单（`Burning` / `Necrons Rays` / `Vortex` / `Spine/Special/HiddenPass`）、
  其余 13 个走「`Shader.Find` 拿不到 ⇒ 落 bundle」那一支；**没有一条落 `Replacements`、也没有一条会落到 `Shader.Find` 的错版本**。

### 1·2 载体：**克隆材质**（`new Material(source) { }` + `shader = 原件`），不是改 `sharedMaterial`

理由（写在 `ArenaOriginalShaderRebind.cs` 文件头第二节）：
① 直接改会把**工程内 `.mat` 资产**改脏 —— 编辑器里一存盘就写进一个非法 shader 引用（正是坑 2 的死法）；
② 克隆只影响这一份 prefab 实例；
③ 与 `ArenaOriginalMaterial.Rebuild()`（`new Material(sh)`）同一思路。
材质属性由 `new Material(source)` **原生整块**拷贝（原版材质的贴图/颜色/关键字都在 `m_SavedProperties` 里，shader 缺失不影响它们被序列化下来），
另加 `CopyTextureSlots` 按名字补一遍贴图 + Tiling/Offset 作第二道保险。
🔴 **故意不给「克隆失败」配退路**：`new Material(shader)` + 手抄属性那条路在「源 shader 坏掉」时会**静默少抄**
（`GetColor/GetTexture` 按当前 shader 的属性表走）⇒ 得到「有 shader、没属性」的材质 = **不品红但全错**（比红更难查）。
所以克隆失败就 `LogError` 出声、保持原样（探针会红）。
克隆出来的材质记在组件的 `_created` 里，`OnDestroy` 一并销毁（编辑模式下用 `DestroyImmediate`，避免警告）。

### 1·3 映射表放哪：**生成 C# 静态表**（`ArenaOriginalShaderIndex.cs`）

三选一的理由（也写在那个文件头）：
① 与 `OriginalShaderBlendTable`（同为生成件的 C# 表）**同一种做法**，不新增第二种读表通道；
② 运行期**零 I/O、零平台差异** —— `Awake` 里同步可用；`StreamingAssets` 在部分平台要 `UnityWebRequest`（异步）⇒ `Awake` 读不到；
   `Resources.Load<TextAsset>` 要**再拷一份**进 `Resources/`（多一份拷贝 = 多一处会漂移）；
③ 编译期可 `grep`；
④ **漂移由探针逐条对 tsv 断言**（A 组），不靠人记。
⚠️ **正本仍然是 tsv**（`_shared/arena_shader_guid_map.tsv`，⛔ 没删没改）；C# 表是它的副本（17 个名字 + 37 对「材质名 → shader 名」）。
本轮已用脚本**逐条核过**：C# 表与 tsv **37/37 完全相等**、17 个名字集合相等。

### 1·4 「按谁认领材质」的顺序（**顺序别颠倒**）

1. **prefab 上挂的 `targets` + `shaderNames`（对象引用匹配）** —— 精确，不受同名影响；
2. **兜底**：`ArenaOriginalShaderIndex`（材质**名**）—— ⚠️ **只在材质当前 shader 是坏的时候才查**。
   为什么必须要这个闸门：**37 个材质里有 6 个在工程别处有同名 `.mat`**（见 §六·3）⇒ 无脑按名字查会误伤别的墙。
3. 两条都认不出、而 shader 又是坏的 ⇒ **`LogError` 出声**（⛔ 不静默）。

### 1·5 谁触发：**13 颗 prefab 的根各挂一个组件**（`Awake`）

- 这些 prefab 目前**没有消费方**；挂组件 = 将来谁实例化都会自动跑，不依赖某个加载器记得调；
- 同时公开静态入口 `ArenaOriginalShaderRebind.RebindTree(go)`（幂等）给将来的统一加载器用；
- ⚠️ **编辑器（非 Play）下 Unity 不调 `Awake`** ⇒ 探针里是**显式调 `Rebind()`**（与 `Awake` **同一个方法**）；
  「实例化就自动重绑」这一跳只能在 Play / player 里看日志 `[Arena/Rebind]`（本轮没跑）。

---

## 二、改动清单（文件 : 符号；不写死行号）

### 2·1 新增（**Runtime，会进 git**）

| 文件 | 内容 |
|---|---|
| `Assets/WarpforgeVFX/Runtime/ArenaOriginalShaderIndex.cs`（+ `.meta`，guid `8a980f03…`） | 生成表：`ShaderNames`（17）· `MaterialNames`（37）· `MaterialShaderNames`（37）· `TryGetOriginalShaderName()` · `TsvAssetPath` |
| `Assets/WarpforgeVFX/Runtime/ArenaOriginalShaderRebind.cs`（+ `.meta`，guid **`28ce47b0cb7b42b69fffe5932b8c9575`** ← 13 颗 prefab 的 `m_Script` 就指这个） | `ArenaOriginalShaderRebind`：字段 `targets` / `shaderNames` / `logSummary`；`Awake()` → `Rebind()`；静态 `RebindTree(root[, targets, shaderNames, log, owner, created])` · `Survey()` · `TryExpectedName()` · `IsBroken()` · `Report` / `SurveyResult`；`OnDestroy` 清理克隆材质 |

### 2·2 新增（Editor 探针 —— ⚠️ **在 `.gitignore` 里，见 §六·1**）

| 文件 | 内容 |
|---|---|
| `Assets/WarpforgeVFX/Editor/ArenaOriginalShaderRebindProbe.cs`（+ `.meta`） | `ArenaOriginalShaderRebindProbe.Run()`（`-executeMethod` 入口）· A/B/C/D 四组断言 |
| `Assets/WarpforgeVFX/Editor.meta` | 新目录的 meta（guid `80849274…`） |

### 2·3 改（13 颗 prefab，**只加组件**；都在 `ArenaOriginalPrefabs/<场>/<场>__Scenario.prefab`）

每颗只在两处动了：
1. 根 GameObject（都叫 `Scenario`，每颗**恰好 1 个根**、原本只有 1 个组件）的 `m_Component` 列表末尾加一行
   `- component: {fileID: 1000000000009901}`；
2. 文件末尾追加一个 `--- !u!114 &1000000000009901` 的 `MonoBehaviour` 块（`m_GameObject` = 该颗的根；
   `m_Script` = 上面的组件 guid；`targets` / `shaderNames` 按该场实际用到的材质逐条列出；`logSummary: 1`）。

| 场 | 目标材质条目 | 目标材质槽 |
|---|---|---|
| battlearena1 | 1 | 1 |
| battlearena2 | 3 | 5 |
| battlearena3 | 2 | 3 |
| battlearenaaeldari | 2 | 3 |
| battlearenaastramilitarum | 1 | 1 |
| battlearenablacklegion | 1 | 4 |
| battlearenadarkangels | 3 | 4 |
| battlearenaemperorschildren | 1 | 1 |
| battlearenagenestealers | 1 | 1 |
| battlearenaleviathan | 11 | 33 |
| battlearenasororitas | 2 | 3 |
| battlearenaspacewolves | 1 | 1 |
| battlearenatauviorla | 3 | 9 |
| **合计** | **32**（37 里剩下的 5 个只被 VFX Graph 资产引用） | **69** |

**「只加组件」是脚本验过的**（`_tmp_view/p02_rebind/` 那一轮脚本）：
把「新加的那一行 + 末尾那个块」删掉之后，**13 份文件逐字节等于备份**；行尾仍是 **LF**（0 处 CRLF）；
对象块数恰好多 1（`--- !u!114`）；`&1000000000009901` 在每份里出现 **1 次**、`{fileID: 1000000000009901}` 1 次；
`targets` 的 guid 与 `shaderNames` 的名字与计划表（从 prefab 的 `m_Materials` 实读出来的）**逐条相等**；
`m_GameObject` = 该颗的根 GO。

### 2·4 ⛔ 没动

`_shared/arena_shader_guid_map.tsv`（**原样**）· 任何 `.mat` · `ArenaOriginalMaterial` / `ArenaBuilder` / 战场生成器 ·
`Assets/CardPresentation/Battle|RuleEngine|Shell/**` · 两张正本 · 任何 `任务总表.md` · git · 白名单外文件。

---

## 三、探针（**主对话跑**）与断言

### 3·1 怎么跑

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod ArenaOriginalShaderRebindProbe.Run -logFile -
```

判绿红：日志里 `ASR === X 过 / Y 红 ===`；逐条按行首 `ASR ✓` / `ASR ✗` 数（✗ 用 `Debug.LogError` 打）。
退出码：全绿 `0`、有红 `1`（`Application.isBatchMode` 时 `EditorApplication.Exit`）。

### 3·2 四组断言（期望值）

| 组 | 断言 | 期望 |
|---|---|---|
| **A0–A5** | 判据表在；tsv 行数；17 个名字集合相等；材质条目 37/37；**「材质名→shader 名」逐条与 tsv 一致**；**每个 `.mat` 的 `m_Shader` guid 逐条 == tsv 的 `old_guid`** | 18 行 / 17 名 / 37 材质 / 37 条 guid 全中 |
| **B** | 37 个材质资产**逐个** `WarpforgeShaderMap.TryResolve(原版名)` ⇒ 拿到 shader（并打 `[来源]` 与 `isSupported`） | **37/37**（这就是任务书要的「解析到 / 仍是 null」逐条报告） |
| **C1** | 13 颗 prefab 加载得到、根上有 `ArenaOriginalShaderRebind`、`targets` 与 `shaderNames` 等长且逐条与索引表一致 | 13/13 |
| **C2** | 实例化后调 `Rebind()`（= `Awake` 同一入口）⇒ 每颗 `unresolved=0 / unknown=0`；`Survey` 复查 `broken=0`；逐材质打一行「解析到 `<shader>`（n 槽）」 | 13/13 + 32 行逐材质 |
| **C3** | 9 块地面（`Everguild/Misc/Unlit shadows receiver`）重绑后 **`_BaseMap` 非空**（盯「换 shader 把属性丢了」） | 9/9 |
| **C4** | 13 颗合计目标材质槽 = **69**；不同目标材质 = **32** | = 期望（**如果实测不等，第一个要看的是探针的分组口径，不是产品**） |
| **D（灭自证）** | 把重绑结果**故意打回品红**（`Hidden/InternalErrorShader`）⇒ `Survey` 必须数出 **N = 该 prefab 的目标槽数**（>0）；再 `RebindTree` 一次 ⇒ 回到 0 | 13/13 |

**D 组就是任务书要的「删掉重绑 ⇒ 37 个材质里有 N 个 null ⇒ 红」**：把 `Awake`/`Rebind` 那一跳删掉，C2 会以
**这 N 个 null** 变红（13 颗合计 **69 个槽**；按不同材质数是 **32 个**；再算上 VFX Graph 那 5 个 = **37 个**）。
D 组还证明「不是别的东西碰巧把它们修好的」—— 打回品红之后只有重绑能救回来。

### 3·3 探针**不落盘**

重绑走的是**克隆材质**，工程内 `.mat` 不会被改成脏；探针结尾会打一行
`ASR i 工程内 .mat 被改成脏的个数 = N`（只报不断言，避免上一轮遗留的脏标误伤）。

---

## 四、本轮做过的静态自检（不跑 Unity）

| # | 检查 | 结果 |
|---|---|---|
| 1 | `工具/typecheck.sh`（运行时 + 编辑器两个程序集；`gen_csc_rsp.py` 自动收了 3 个新 `.cs`，已在 rsp 里确认） | **0 错误 / 0 错误**（中途有一次 20 条假错的说明见 §六·5） |
| 2 | C# 静态表 vs tsv（脚本文本比对） | 17 名 / 37 对**完全相等** |
| 3 | 13 颗 prefab 的改动是「只加不改」 | **删掉新加的 2 处后逐字节等于备份**；LF 未翻 |
| 4 | 13 颗 prefab 的 `targets` 与从 prefab 实读的 `m_Materials` 比对 | 32 条 **逐条相等**（guid + 名字） |
| 5 | 全树 `.mat` 的 `m_Shader` guid 归属 | 29 个不同 guid：**18 个 = T00 那批（工程里没有，靠重绑）**；其余 11 个**都能解析**（见 §六·2） |
| 6 | 37 个材质名在工程内的唯一性 | 31 个唯一、**6 个有同名件**（⇒ 兜底查询必须加「只在 shader 坏时」的闸门，已加） |

---

## 五、没查清 / 判不准（⛔ 不猜）

1. 🔴 **探针没跑**（本代理不许跑 Unity 批处理）⇒ 上面 §三 的期望值**全部未实测**。
   ⚠️ 最可能出问题的两条：① 手写进 prefab 的 `--- !u!114` 块能否被 Unity 正常 import（结构照 `_shared/ParticlesLight.prefab`
   里那个 Unity 自己写的 MonoBehaviour 抄的）；② `new Material(source)` 在「源材质 shader 为 null」时**会不会原样拷属性** ——
   探针 C3（9 块地面的 `_BaseMap`）就是盯这条；**如果它是空的，说明属性没拷过来**，那时要么改成「按 tsv 重新灌属性」，要么换克隆路线，**别当成通过**。
2. **`Awake` 的自动触发没验**（编辑器非 Play 不调 `Awake`）。验法：进 Play / player，日志里应出现 13 行 `[Arena/Rebind] \`<场>\`：…`。
3. **5 个材质（`Hidden_VFX_Aeldari Dust…` + 4 个 `Hidden_VFX_Aeldari Portal…`）任何一颗 Scenario prefab 都触不到**
   （它们被 VFX Graph 资产 `_shared/Aeldari Dust.asset` / `battlearenaaeldari/Aeldari Portal.asset` 引用）⇒ 只在探针 B 组按「材质资产级」验过；
   **将来谁消费这两个 VFX Graph 资产，那条路要自己走一次 `RebindTree`**（把 `ArenaOriginalShaderRebind` 挂在承载对象上，或对实例调静态入口）。
4. **13 颗 prefab 里的 VFX 材质用的是 URP 包 shader**（`Universal Render Pipeline/Particles/Unlit` × 71 个材质 ·
   `Universal Render Pipeline/Unlit` × 5 个）—— 它们**能解析**（`Library/PackageCache`，见 §六·2），所以不在 37 里；
   但「原版到底是不是就该用 URP 这两个」**本轮没查**（超出 P0-2 范围：P0-2 只负责「不品红」）。若 T01 逐场对账要抠，单开一条。
5. **克隆材质的内存**：`OnDestroy` 里销毁，但**只覆盖组件自己创建的**；探针的负控额外创建的克隆没记（进程退出即回收）。

---

## 六、顺手发现（⛔ 都没改白名单外的文件）

1. 🔴 **`Assets/WarpforgeVFX/` 整个目录在 `.gitignore` 里**（`/Unity/MyGame/Assets/WarpforgeVFX/*`，只有
   `!/…/Runtime/`、`!/…/Shaders/` 两个例外 —— `.gitignore:24-30`）。后果：
   - ✅ 两个 Runtime `.cs` 会进 git；⛔ **`Editor/ArenaOriginalShaderRebindProbe.cs` 不进**（探针只在本机）；
   - ⛔ **13 颗 prefab 的改动也不进**（`ArenaOriginalPrefabs/` 与 `_shared/` 全在忽略范围）⇒ 这批资产本来就是「本机导出物」，
     换机重导时要**重新跑一次本轮那套加组件的脚本**（脚本在 `_tmp_view/p02_rebind/`，见 §七）。
   - 建议（**不在我白名单，没做**）：给 `.gitignore` 加一条 `!/Unity/MyGame/Assets/WarpforgeVFX/Editor/`，或把探针挪到
     `Assets/WarpforgeArena1/Editor/`（其它战场探针都在那儿、是跟仓库走的）。
2. ✅ **T00 的 18 个 shader guid 就是「工程里解析不到」的全集** —— 我把 `ArenaOriginalPrefabs` 全树的 `.mat` 扫了一遍（29 个不同 guid）：
   18 个 = T00 那批；其余 11 个**都能解析**：
   `7c3e1fcc…`=WFUnlitAmbient(68) · `730df9a3…`=WFParticlesExtraColorEC(19) · `23b6c6bb…`=WFUnlitUVScroll(7) ·
   `0000…f000…`=Unity 内置(7) · `650dd952…`=**URP `Unlit`**(5) · `b235a9d7…`=WFAlphaMaskOneLayer(3) · `7692e407…`=WFDistortion(2) ·
   `2e880a12…`=WFAlphaMasksTwoLayer(2) · `e405f416…`=WFEclipseTau(1) · `da6aa54c…`=WFMatcap(1) · `0406db5a…`=**URP `Particles/Unlit`**(71)。
   ⚠️ 我第一次只扫了 `Assets/**/*.shader.meta` ⇒ 把 0406/650d 误判成「工程里没有」；**它们是 URP 包 shader，在
   `Library/PackageCache/com.unity.render-pipelines.universal@…/Shaders/`**。同族教训：**「工程内没有」要连 PackageCache 一起搜**。
3. ⚠️ **6 个材质名在工程别处有同名 `.mat`**：`Battle Arena 1 Floor` / `Battle Arena 2 Ground` / `Astra Militarum Ground Atlas`
   （`WarpforgeArena1/arenas/*/Materials/`）、`Necrons Flying Monolyth Rays` / `Vortex portal` / `HiddenPass`（`WarpforgeVFX/Materials/`）。
   ⇒ 已用「只在 shader 坏时查名字表」的闸门挡住；**将来若真要做「按名字修 shader」，先看这条**。
4. ℹ️ 13 颗 prefab **每颗恰好 1 个根 GO（都叫 `Scenario`）**、原本**一个 MonoBehaviour 都没有**（`--- !u!114` 计数 0/13），
   也没有嵌套 prefab 实例（`!u!1001` 0/13）⇒ 加组件这条路没有「跟别的组件抢 fileID」的风险。
5. ⚠️ **中途 `typecheck.sh` 报过 20 条错，全在 `Assets/RuleEngine/Core/Enchantment.cs`（不是本轮的活）** ——
   那是**别的代理正在写的半成品**（文件 mtime 与报错同一分钟内跳动）；按铁律 13·3③ 不去动别人的文件，**等它写完重跑**
   ⇒ 46 秒后同一命令 **0 / 0**。⇒ 报告里的 `typecheck 0/0` 是最后一次的读数。

---

## 七、回滚 / 重放

**脚本与数据都留在 `d:/4/_tmp_view/p02_rebind/`**（`_tmp_view/` 在 `.gitignore` 里）：
`plan.json`（每颗 prefab 的根 GO id + 要挂的材质）· `gen_plan.py`（从 prefab 的 `m_Materials` 重算 plan）·
`apply_add_component.py`（加组件；**自带闸门：文件里已有 `&1000000000009901` 就报错停手** —— 只能对改动前的 prefab 跑）·
`verify_edits.py`（「删掉新加的两处后逐字节等于备份」+ 结构/内容校验）· `static_check_index_vs_tsv.py`（C# 表 vs tsv）。

- **prefab 回滚**：`_tmp_view/p02_rebind/backup/<场>__Scenario.prefab` → 拷回 `Assets/WarpforgeVFX/ArenaOriginalPrefabs/<场>/`；
  想重放就先回滚、再 `python -I -X utf8 d:/4/_tmp_view/p02_rebind/apply_add_component.py apply`。
- **代码回滚**：删掉 `Runtime/ArenaOriginalShaderIndex.cs`(+meta) 与 `Runtime/ArenaOriginalShaderRebind.cs`(+meta) 即可 ——
  但**要先回滚 prefab**，否则 13 颗 prefab 上的组件会变成 Missing Script。
- **探针回滚**：删 `Editor/` 目录（或单删那两个文件 + 目录 meta）。

---

**改了什么**：新增 2 个 Runtime 文件（`ArenaOriginalShaderIndex` 生成表 + `ArenaOriginalShaderRebind` 重绑组件/静态 API）、
1 个 Editor 探针（+2 个 meta、1 个目录 meta）；13 颗 prefab **只**在根上加了一个组件（`targets`/`shaderNames` 按该场实际材质逐条列）。
**验了什么**：`typecheck.sh` 两个程序集 0 错；C# 表 vs tsv 37/37 相等；13 份 prefab「删掉新加的两处后逐字节等于备份」；
13 份的 `targets` 与 prefab 实读 `m_Materials` 32/32 逐条相等；全树 29 个 shader guid 里**只有 T00 那 18 个解析不到**。
**主对话要跑什么**：`-executeMethod ArenaOriginalShaderRebindProbe.Run`（§三·1）—— 期望 `ASR === X 过 / 0 红 ===`、
B 组 37/37、C 组 69 槽 / 32 材质 / 9 地面、D 组 13/13；跑完把日志里 `ASR ✗` 与 `[Arena/Rebind]` 两行贴回来即可定位。
