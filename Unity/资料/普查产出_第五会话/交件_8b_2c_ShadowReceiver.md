# 交件 · `§8b` 甲表第 **2c** 行：`Shadow Receiver`（第五会话 · 动手写手）

> 任务：把原版的 `Shadow Receiver` 物件**建出来**（13 个战场每个一份），**并保持与原版逐位一致 —— 即 `SetActive(false)`**。
> 交付物 = **生成器 + ArenaBuilder 两处能把这个物件建出来**。
> 判据来源：`资料/普查产出_第五会话/查证_8b战斗视图四条.md` 甲表 2c / 丙表 3 / 丁表 + `资料/战场还原度_arena1物料与相机灯光.md:81,93`。
>
> ⚠️ 本文件只记「结论 / 对账 / 出处 / 交件」。**没改 `项目任务.md` / `CLAUDE.md`**（铁律 13·3）——
> 待办与顺手发现请由调度台合并进正本。

---

## 一、结论

### 1. 那个「没查清」的 mesh —— **查清了：`Library/unity default resources` 里的内置 `Quad`**

`MeshFilter_1524.m_Mesh = {m_FileID: 5, m_PathID: 10210}` 的 `m_FileID` 是
「**相对引用者所在那份 CAB 的 externals 表的序号**」。把 arena1 主 CAB
（`CAB-8adfc300739b4da5111d4bd3eae80365`，5304 个对象）的 externals 摊开：

```
fid=5 -> '<FileIdentifier(Library/unity default resources)>'
```

⇒ `m_PathID 10210` 是 **Unity 内置资源**，不是「本地没有的跨包资产」。
（⚠️ `m_FileID` **逐场不同**（逐场实读）：**=5** 的 6 场（arena1/2/3 · blacklegion · emperorschildren · tauviorla）·
**=6** 的 6 场（aeldari · astramilitarum · darkangels · genestealers · leviathan · spacewolves）·
**=7** 的 1 场（sororitas）⇒ **不能写死 5**，必须按引用者那份 CAB 解。）

**10210 到底是哪一颗 —— 读原版自己那份内置资源（不是编辑器那份、更不是猜）**：

| 证据 | 读数 |
|---|---|
| `d:/2/Warhammer 40k Warpforge/Warpforge_Data/Resources/unity default resources` | `pid 10210` = **`Mesh "Quad"`** · **4 顶点** · `m_LocalAABB` center (0,0,0) / extent **(0.5, 0.5, 3.06e-17)** |
| 同一份的邻居（**用来验判据没读错行**） | 10202 `Cube`(24v) · 10206 `Cylinder`(88v) · 10207 `Sphere`(515v) · 10208 `Capsule`(550v) · 10209 **`Plane`(121v, extent ±5)** · **10210 `Quad`(4v)** · 10211 `Icosphere` |
| `d:/2/unity_run_ref/Warpforge_Data/Resources/unity default resources`（第二份原件） | 逐 pid 逐值 **同上** |
| 编辑器那份（`D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/unity default resources`） | 同表同值（**旁证，不当判据**） |

⇒ **`PathID 10210` = 内置 `Quad`（1×1 面片）**。
⛔ **不是 `Plane`**（10209 才是 Plane、extent ±5 = 10×10）——
`ArenaBuilder.cs` 里 `WF_SHADOWPROBE` 探针那句 `PrimitiveType.Plane` 是**探针自己的做法**，
与原件不是同一颗（查证报告 己·1 已标注，本次实读钉死）。

**物件形状**（13/13 逐场实读，见 §三）：local scale **(81.161, 35.127, 41.908)** × 绕 X **90°**
⇒ 一块 **81.161 × 35.127 的水平面片**（quad 法线 (0,0,−1) 绕 X +90° 后 = **(0,+1,0)**，**面朝上**），
世界位置 = 战场根原点 **(100, 0, 0)**（即场景 x=100 的战场原点、y=0 地面）
——**一块贴地的「接影板」**，与它的名字与材质（`Transparent Shadow Receiver`）自洽。

> 📌 材质 **`Transparent Shadow Receiver`** 的 shader 名（`Shader_34` 外层 `m_Name` 是**空串**，
> 真名在 `m_ParsedForm.m_Name`）= **`Everguild/Misc/URP Transparent Shadow Receiver`**；
> 唯一属性 `_ShadowColor` 默认 **(0.35, 0.40, 0.45, 1)**；`m_CustomRenderQueue 3000`；
> pass `rtBlend0 = src 2 / dst 0`（`Blend DstColor Zero`）+ `ZWrite Off` + `ZTest 4` + `cull 2`。
> ✅ **这个 shader 我们本地已经有**（`StreamingAssets/WarpforgeVFX/wf_arena_2.bundle` 与
> `wf_arena_tauviorla.bundle`，逐包实读）⇒ 运行时 `ArenaOriginalMaterial` 能取到它、不用另抽包。

### 2. 建出来长什么样

`Warpforge_<场>` 根下多一个 `Shadow Receiver` 空物体（与其余网格同形状）：

| 层 | 值 |
|---|---|
| `Shadow Receiver`（holder） | **`SetActive(false)`**（= 原版 `m_IsActive=false`）；`local (pos/rot/scale)` 照清单 |
| └ `mesh`（新子件） | `MeshFilter.sharedMesh` = `Resources.GetBuiltinResource<Mesh>("Quad.fbx")`（**就是原件那一颗**） |
| 同上 | `MeshRenderer.sharedMaterial` = 构建期 `.mat`（`URP/Unlit` 兜底 —— 与全工程其余网格同一条路） |
| 同上 | `shadowCastingMode = On` / `receiveShadows = true`（原版 `m_CastShadows=1` / `m_ReceiveShadows=1`） |
| 同上 | `WarpforgeVFX.ArenaOriginalMaterial`（**运行时**用原版 shader 重建）—— `shaderName` / `props` / `queue=3000` 全带上 |
| 同上 | ⚠️ holder 关着 ⇒ `Awake()` 不会被调 ⇒ 那个组件**运行时不会真的重建材质**（原版那份也是关的 ⇒ 行为一致） |

🔴 **「与原版逐位一致」= 把对象建出来 + 关掉**，**不是不建** —— 铁律 11 第①种管的是
「原版本身没有」；这里原版**有**物件、有 13/13 场景实例、有材质有组件（查证报告丁表已定案）。

---

## 二、改动清单（`文件:行号 | 改前 | 改后`）

> ⚠️ 行号会漂 ⇒ **按符号认**。两处改动只在**白名单内的两个文件**里。

### 2.1 `d:/4/Unity/工具/scripts快照/gen_unity_arena_manifest.py`

> ⚠️ **真路径**：任务简报写的是 `工具/gen_unity_arena_manifest.py`，**实际在 `工具/scripts快照/` 下**
> （全盘 `find` 只有这一份，93 KB / 2036 行）。

| 位置（符号） | 改前 | 改后 |
|---|---|---|
| 新函数（`psr_of` 之后） | —— | **`mr_of(a, gopid)`**：取 GO 上的 MeshRenderer |
| 新函数 | —— | **`default_resource_mesh_name(pid)`**：读原版自带 `unity default resources` → `pid → 网格名`（进程内缓存） |
| 新函数 | —— | **`builtin_mesh_map(a, warn)`**：翻本场包的 `MeshFilter`，按 `assets_file.externals` 解 `m_FileID`，收「内置资源 + **原版真的会画它**」的 `{GO pid: 网格名}` |
| `build_manifest` 的 `warn = {...}` | 5 个键 | **+ `'builtin_mesh': []` + `'inactive_mesh': []`** |
| `build_manifest` 网格段 | `meshes = []` | **+ `builtin_map = builtin_mesh_map(a, warn)`** |
| 网格循环开头 | `mesh_name, _ = a.mesh_of(gopid)` / `if not mesh_name: continue` | **`mesh_of` 空时先查 `builtin_map`**，命中就用它的名字（否则照旧 `continue`） |
| 网格循环 | `hit = obj_index.get(...)`；`if not obj_file: warn['obj_missing']` | 内置网格**不查 OBJ、不报 obj_missing** |
| 网格循环 | `meshes.append({…})` | 改成 `entry = {…}`；**`entry['builtinMesh'] = builtin`** |
| 网格循环尾 | —— | 内置那一支**再加 4 个键**：`active`（= `active_in_hierarchy`）、`hasShadow`、`shadowCast`、`shadowReceive`；非内置且 `active_in_hierarchy` 为假 ⇒ 记进 `warn['inactive_mesh']` |
| `run_one` 控制台报告 | —— | **+ 两段**：`[内置网格 N 件]`（逐件·含「原版不画已跳过」汇总）与 `⚠️ [原版关着、我们仍建出来是开的网格 N 件]` |

### 2.2 `d:/4/Unity/MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs`

| 位置（符号） | 改前 | 改后 |
|---|---|---|
| `class MeshEntry` 尾 | `public bool worldBaked;` 后结束 | **+ `public string builtinMesh;` + `public bool active = true;`**（各带长注释与判据） |
| `BuildContent` 计数器 | `int nMeshKw = 0;` | **+ `int nMeshBuiltin = 0;`** |
| `BuildContent` 建 holder 之后 | —— | **+ `if (!e.active) holder.SetActive(false);`** |
| `BuildContent` 网格分支 | `if (!string.IsNullOrEmpty(e.objFile))` | **`if (!string.IsNullOrEmpty(e.builtinMesh)) {…} else if (…objFile) {…}`**（内置那一支：`Resources.GetBuiltinResource<Mesh>(名字 + ".fbx")` + MeshFilter + MeshRenderer + 阴影标志 + 材质 + 原版关键字 + `AttachOriginalMaterial`；**取不到就出声跳过、不拿别的网格顶**） |
| `BuildContent` 收尾 | —— | **+ 一行 `Debug.Log`**：`Unity 内置网格…建出 N 个；其中原版自己就是关着的 M 个：…` |

---

## 三、原版对账（逐字段 —— 13/13 逐场实读）

**`Transform` / `GameObject` / `MeshFilter` / `MeshRenderer`**（出处：`assets_full/bundle_scenes_scenes_<场>/…`
的 `GameObject/Shadow Receiver.json` + `Transform_*/MeshFilter_*/MeshRenderer_*`，
逐场用 `m_Component` 里的 pid 取）：

| 字段 | 原版值（出处） | 我们的值 |
|---|---|---|
| `GameObject.m_Name` | `Shadow Receiver`（13/13 同名） | `Shadow Receiver` |
| `GameObject.m_IsActive` | **`false`**（13/13） | `SetActive(false)` ✅ |
| `GameObject.m_Layer` / `m_Tag` | `0` / `0` | `0` / `0`（`new GameObject` 默认） |
| `Transform.m_LocalRotation` | `(0.7071057558059692, −0.0, −0.0, 0.7071079015731812)` —— **13/13 逐位相同** | 清单 `rot=[0.707106, 0.0, 0.0, 0.707108]`（`r6` 六位）✅ |
| `Transform.m_LocalPosition` | `(0,0,0)`（祖先链 `Colliders/BattleBoardElements/BattlePrefab` 全是 identity） | **世界** `pos=[100.0, 0.0, 0.0]` ✅（清单一律记世界变换 —— 与其余 28 件同一个口径） |
| `Transform.m_LocalScale` | `(81.16107940673828, 35.126861572265625, 41.90794372558594)` —— 13/13 逐位相同 | `scale=[81.161079, 35.126866, 41.907949]`（`r6`）✅ |
| `MeshFilter.m_Mesh` | `{m_FileID: 逐场 5/6/7, m_PathID: 10210}` → **内置 `Quad`** | `builtinMesh='Quad'` → `Resources.GetBuiltinResource<Mesh>("Quad.fbx")` ✅ |
| `MeshRenderer.m_Enabled` | `true` | `true`（新建组件默认） |
| `MeshRenderer.m_CastShadows` | **`1`**（13/13） | `shadowCastingMode = On` ✅ |
| `MeshRenderer.m_ReceiveShadows` | **`1`**（13/13） | `receiveShadows = true` ✅ |
| `MeshRenderer.m_Materials` | 1 个 → 材质 **`Transparent Shadow Receiver`**（13/13 同名） | 清单 `subMats[0]`（1 条）✅ |

**材质**（出处：`07_场景/<场>/Material/Material_<pid>.json`，13 场逐场读）：

| 字段 | 原版值（**13/13 全同**） | 我们的值（清单） |
|---|---|---|
| `m_Name` | `Transparent Shadow Receiver` | `subMats[0]` 的材质名 → `.mat` 资产名 `Shadow Receiver`（因无贴图，取 GO 名，与其余网格同规则） |
| `m_Shader` → `m_ParsedForm.m_Name` | `Everguild/Misc/URP Transparent Shadow Receiver` | `shader='Everguild/Misc/URP Transparent Shadow Receiver'` ✅ |
| `m_CustomRenderQueue` | `3000` | `subMats[0].queue = 3000` ✅ |
| `m_SavedProperties.m_Colors` | 只有 `_ShadowColor = (0.35, 0.40, 0.45, 1.0)` | `props=[{k:'_ShadowColor', t:'c', c:[0.35,0.40,0.45,1.0]}]` ✅ |
| `m_Floats` / `m_TexEnvs` | **各 0 条**（无贴图、无标量） | `tex=null` / `texFile=null` ✅ |
| shader pass 状态（`Shader_34`） | `rtBlend0 = src 2 / dst 0`（`Blend DstColor Zero`）· `zWrite 0` · `zTest 4` · `culling 2` | `subMats[0]`: `cull=2` ✅ · `srcBlend=2 / dstBlend=0` ✅ · `blendAuthoritative=true` ✅ · ⚠️ **`transparent=false`（见 §五·3，判据盲区）** |

**13 场逐场（生成器新路径实跑，`builtin_mesh_map` 的产出）**：

| 场 | 收（内置网格件数） | Shadow Receiver 的 GO pid | 跳过（原版不画） | 网格名 |
|---|---|---|---|---|
| `battlearena1` | 1 | 334 | 9（`Cube`×6 · `Collision`×2 · `CardLowBoardLimit`） | Quad |
| `battlearena2` | 1 | 331 | 21（`Cube`×11 · `Collision`×8 · `Sphere`×2 · `CardLowBoardLimit`） | Quad |
| `battlearena3` | 1 | 342 | 3 | Quad |
| `battlearenaaeldari` | 1 | 332 | 4 | Quad |
| `battlearenaastramilitarum` | 1 | 356 | 1 | Quad |
| `battlearenablacklegion` | 1 | 329 | 5（`Grill collision`×4 · `CardLowBoardLimit`） | Quad |
| `battlearenadarkangels` | 1 | 334 | 1 | Quad |
| `battlearenaemperorschildren` | **3** ⚠️ | 329 | 1 | Quad（另 2 件见 §五·1） |
| `battlearenagenestealers` | 1 | 335 | 1 | Quad |
| `battlearenaleviathan` | 1 | 356 | 2 | Quad |
| `battlearenasororitas` | 1 | 365 | 1 | Quad |
| `battlearenaspacewolves` | 1 | 333 | 1 | Quad |
| `battlearenatauviorla` | 1 | 389 | 2 | Quad |
| **合计** | **15** | —— | **52** | **13/13 都是 `Quad`** |

13/13 的 `pos/rot/scale`、`active=false`、材质名、`queue=3000`、`_ShadowColor` **逐场逐值相同**
（已逐场打印核对）。

---

## 四、证据

- **`git diff --numstat`**（本件只动白名单里的两个文件）：
  ```
  99      1       Unity/MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs
  237     6       Unity/工具/scripts快照/gen_unity_arena_manifest.py
  ```
  = **336 增 / 7 删**；其中**绝大部分是判据注释**（本工程的既有风格）。
- **秒级类型检查**（`bash d:/4/Unity/工具/typecheck.sh`，收尾那次）：
  ```
  --- 运行时程序集 ---   运行时错误数: 0
  --- 编辑器程序集 ---   编辑器错误数: 0
  ```
- **`.py` 语法**：`python -I -c "import ast; ast.parse(...)"` → OK。
- **行尾没被翻**（二进制读判，改前/改后各一次）：
  `ArenaBuilder.cs` **CRLF 4665 / LF 4665**（纯 CRLF，原样）；
  `gen_unity_arena_manifest.py` **CRLF 0 / LF 2036**（纯 LF，原样）。⛔ 全程没用 `sed -i`。
- **生成器真跑一遍（代表性一场，不写文件）**：`battlearena1` 的 `build_manifest` 产出
  `builtinMesh='Quad'` / `objFile=null` / `active=false` / `hasShadow=true` / `shadowCast=1` /
  `shadowReceive=1` / `shader='Everguild/Misc/URP Transparent Shadow Receiver'` / `queue=3000` /
  `props=[_ShadowColor]`（逐字段见 §三）。
- **13 场新路径逐场实跑**：见 §三第三张表（13/13 各收 1 件 `Shadow Receiver`）。
- **复现用的只读探针**（都在 `d:/tmp/wf8b/`，**不写任何仓库文件**；⚠️ `d:/tmp` 可能被清，
  脚本本身就是几行 `Assembler` + `gen_unity_arena_manifest` 的调用，重建很快）：
  `probe_ext.py`（摊开 CAB 的 externals）· `probe_builtin2.py`（读**原版自带**的 `unity default resources`，
  pid→名）· `probe_13.py`（13 场 `Shadow Receiver` 的 GO/组件/变换/材质引用）·
  `probe_mat13.py`（13 场材质的名/queue/颜色表）· `probe_newpath13.py`（**13 场逐场跑新代码路径**，§三第三张表的来源）·
  `probe_ec.py`（EC 那 2 件 `Tank distortion`）· `probe_inactive_meshes.py` / `probe_chains.py`（§五·2 的 20 件 + 祖先链）。

**没有跑 Unity**（红线）：`-executeMethod` 一个都没跑，**尤其没跑 `BattleScene.BuildAndSaveScene`**。
⇒ 交付物「**ArenaBuilder 能按它建出 mesh + renderer + 材质**」这半句是**读代码 + 类型检查**得出的，
**未经 Unity 编译/建场验证**（由调度台在收件后跑一次 `BattleScene.BuildAndSaveScene` 收口）。

---

## 五、顺手发现的（⛔ **都没顺手改**，请调度台分流）

### 1. 🔴 `battlearenaemperorschildren` 还有 **2 件原版真的会画、我们从来没建** 的内置 Quad

新闸口如实收下了它们（**它们也是内置 `Quad`**，所以原来同样被静默丢掉）：

| GO | 祖先链 | 材质 | 状态 |
|---|---|---|---|
| `Tank distortion 1`（goPid 5） | `Scenario/Particles/Distort`（**全开**） | `Emperos Children Fake Distort 1`（有贴图）· shader `Everguild/UnlitAmbient` · 关键字 `_APPLYAMBIENTCOLOR`/`_SURFACE_TYPE_TRANSPARENT`/`_USEDISTORT` | 原版 `m_IsActive=true` · 清单**从来没有过**它 |
| `Tank distortion 1 (1)`（goPid 7） | 同上 | `Emperos Children Fake Distort 2` | 同上 |

- 按铁律 11 ⇒ **该建**（原版有、我们缺）。本件**照建了**（属于新路径「闸口」的自然结果，
  不是特意挑的）。
- ⚠️ **但它是本件唯一会「进画面」的副作用**（`Shadow Receiver` 本身是关的 ⇒ 零画面影响）。
  它们带贴图 + 透明混合，运行时会走与其余网格**完全相同**的重建路（`CopyCommon` 会把贴图搬过去），
  所以大概率是贴图 quad 而不是白板；**但这一趟跑不了 Unity、没法并排看**。
  ⇒ **要不要保留这 2 件，请调度台裁**（若要收窄，闸口条件是「只收原版关着的」那种写法**不建议** ——
  那会让这条路只为 `Shadow Receiver` 存在）。
- ⚠️ 顺带：这 2 件与 `Shadow Receiver` 的**原版关键字**（`_USEDISTORT` 那三个）走的是**旁挂**
  `_meshkeywords.json`，而旁挂是**从已建出的清单反向生成**的（`gen_arena_meshkeywords.py` 只报清单里有的对象）
  ⇒ **第一轮必然没有它们**；清单重生成后**再跑一次 `gen_arena_meshkeywords.py`** 就齐了（值与清单同源，不冲突）。

### 2. 🔴 13 场里还有 **20 个**「原版关着、我们建出来是开的」网格对象（**本件没改**）

| 场 | 对象 | 件数 |
|---|---|---|
| `battlearenaaeldari` | `Dynamic Lights 5/8/9/10/11` | 5 |
| `battlearenaspacewolves` | `Full Moon 1..13` · `Full Moon` · `Background Sky First Light` | 15 |

另有 **13 场每场一件 `Cache Stealth`**（它祖先 `Cache [No delete]` **也**关着；且它本来就因为
`Card 3D WH40k` 这个 OBJ 找不到而没进清单 —— 与本批不是同一件事）。

- 逐场实读的**祖先链**证明这些**都是它自己关的**（父级全开），与原版等价 ⇒
  把它们一并关掉是**更贴原版**的（铁律 11）。
- ⛔ **本件没做**：一并铺开**会改这两个战场的画面**（`Full Moon` 一族是 16 张天幕片），
  而这一趟跑不了 Unity 去验收 ⇒ 属**另一笔账**，须单独裁。
- 生成器**已出声**（`⚠️ [原版关着、我们仍建出来是开的网格 N 件]`，逐件列名），**不静默**。

### 3. 🔴 材质判据的盲区：`Blend DstColor Zero`（src 2 / dst 0）被判成「不透明」

`unity_mat_fields` 只认三档混合（`5/10` · `5/1` · `1/0`），**`2/0`（`Blend DstColor Zero`）落到
`transparent=false`**。而 `Shadow Receiver` 的 pass 正是 `src 2 / dst 0` + **`ZWrite Off`**
（= 真的在混合，而且是「乘暗」那一族）。
- 影响面：本件**不可见**（对象关着）；但**凡是 `src=2` 的材质**都会走「不透明」那一档
  （`blendAuthoritative=true` 时 `MakeTransparent` 不会被调、`ZWrite` 保持写）。
- 落点 = `unity_scene_to_godot.py` 的 `unity_mat_fields`（**不在本件白名单里**）⇒ **只登记，没动**。

### 4. 一件小事（供核对）：`warn['builtin_mesh']` 在 arena1 是 **10 条**

10 = **1 收 + 9 跳**（8 个「GO 上没有 MeshRenderer」+ 1 个 `CardLowBoardLimit` 的 `m_Enabled=0`）。
这 9 个正是 `资料/战场还原度_arena1物料与相机灯光.md` §二·4 记的
「**不是物料的定位件（别当物料摆）：`Cube`×6 · `Collision`×7**」——
本件的闸口与那条既有结论**方向一致**（它们确实不该被当物料建出来）。

---

## 六、没查清 / 停手的部分

1. **`Resources.GetBuiltinResource<Mesh>("Quad.fbx")` 没能在 Unity 里实跑**（红线不许跑 Unity）。
   依据是 Unity 的既有 API 形态 + 「内置资源随 player 一起发」这一常识，
   **本件未实测**。⇒ 代码里写死了**取不到就 `Debug.LogWarning` + 跳过**（不静默）；
   调度台跑一次 `BattleScene.BuildAndSaveScene`，日志里有没有
   「取不到 Unity 内置网格 `Quad.fbx`」一眼就知道。
2. **原版那份内置资源里 `Quad` 的顶点/UV/法线**只读到 `VertexCount=4` + bounds
   （没逐顶点比 —— 因为我们的路径是**直接引用同一份内置资产**，不是复制几何，
   所以「逐顶点一致」是**结构性成立**的，不必再比）。
3. **`m_FileID` 只对 arena1 的主 CAB 把 externals 名称打印出来了**（`Library/unity default resources`）；
   其余 12 场是**用同一段代码逐场跑出来的结果**（`builtin_mesh_map` 逐场都命中、13/13 收 1 件）
   —— 即：**结论是逐场实跑得出的，但字面量只对 arena1 取证**。
4. **`Shadow Receiver` 到底「该接收谁的影子」没查**（原版给它的 shader 名就叫
   `URP Transparent Shadow Receiver`、`_ShadowColor` 是冷灰 `(0.35,0.40,0.45)`）——
   但**这属于「让它可见」那一笔**（要先把我们断掉的 URP 实时阴影链修好，见查证报告甲表 2c 的备注），
   **不在本件范围**。本件只负责「建出来 + 照原版关着」。
5. **13 场「全量 `build_manifest`」没跑完**（一场要十几分钟、13 场 >1 小时）——
   改为**只跑新代码路径**（`builtin_mesh_map` + 世界变换 + 材质解析）逐场验证，13/13 全过。
   ⚠️ 因此**没有**逐场复核「新条目会不会挤掉/影响别的条目」（读代码看：新条目只 append、
   闸口只从 `builtin_map` 取，其余条目走的是原路，**逻辑上零影响**；但没跑过全量对比）。

---

## 七、给调度台的收口清单

1. **跑一次 `python 工具/scripts快照/gen_unity_arena_manifest.py --arena <场>`（或 `--all`）** 重生成 13 份清单
   —— ⚠️ **会改 `arenas/<场>/<场>_manifest.json`（本件白名单外，故本件没跑）**；
   控制台应出现 `[内置网格 1 件] Shadow Receiver -> pid 10210 = 内置网格 'Quad'`。
2. **跟一次 `BattleScene.BuildAndSaveScene`**（铁律：改完 `ArenaBuilder` 必须跟一次）——
   日志里应有 `Unity 内置网格…建出 1 个；其中**原版自己就是关着的**…1 个：Shadow Receiver`。
3. **可选**：`python 工具/gen_arena_shadowflags.py --arena <场>` / `gen_arena_meshkeywords.py`
   重生成旁挂（清单里多了一件对象，旁挂跟着补上 —— 值同源，不冲突）。
4. **裁两件**：§五·1（EC 那 2 件要不要留）· §五·2（那 20 件的 `active` 要不要一并铺开）。
