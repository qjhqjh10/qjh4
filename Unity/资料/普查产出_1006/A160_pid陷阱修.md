# A160 · 修那 7 个「按 pid 建索引 / 反查」的工具

> 2026-10-06 · 执行子代理 · **纯 Python 工具改动**（⛔ 未跑 Unity、未动 git、未改任何正本、未碰 `d:/2`、未碰白名单外的文件）
> 判据 = `资料/普查产出_1006/A152_pid陷阱普查.md` 的 🅰️ 档（逐处最小修法）+ `资料/已知的坑.md`（pid 是分包局部的）
> 正确形态参照 = `_probe_deckinfo.object_cab()/fileid_note()` · `gen_arena_vertexcolors.py`（键 = `(fn, pid)`）· `gen_vfx_texture_mips.py`（撞名大声报）
> 本件**新跑的只读实测**全部写在下面（命令可复现）—— 一律 `UnityPy.load` 只读打开，**没有重跑任何生成器、没有写任何产物**

---

## 结论（一句话）

**7 处全部改成 A152 给的最小修法形态**（共 **9 个 `.py`**：A6 连带 `build_shader_props.py` / `dump_shader_full.py`）；
**活/死判断：7 处全判「活」**（A6/A7 属「归档但可重跑」，不是死工具 ⇒ 按 🅰️ 档动手改，没走「只报不改」那条）；
**零 Unity 自检**（本件属「① 零自检」那一组），验收全部走**读码 + 只读复算**。

🔴 **一条重要更正**：A152 的 A3/A4 里那句「**对双 CAB 包 `prefer_bundle` 完全失效**」**经实测不成立** ——
15 个 `scenes_*` 包里两份 CAB 的 `(类型, pid)` **交集 = 0**（两份**按类型分工**：`.sharedAssets` 装资产、
主 CAB 装场景对象；唯一例外 `mainmenuwarpforge` 的 128 个 GameObject）。已在两处注释里就地更正，
**并请调度台把它并进正本**（详见「顺手发现 ①」）。

🔴 **A1 是唯一有跨仓含义的一处**（`工具/README.md` 的「改这个生成器时两边都要改」口径）——
**我一个字都没写 `d:/2`**，同步与否见下面「跨仓」那一节。

---

## 步骤 0：活 / 死 判断（逐处给依据）

| # | 文件 | 判断 | 依据（可复查） |
|---|---|---|---|
| **A1** | `工具/scripts快照/unity_scene_to_godot.py` | ✅ **活（最活的一处）** | ① `工具/README.md` 的 `scripts快照/` 那一行（2026-09-30 更正）：**战场清单生成器就住在 `工具/scripts快照/` 这一份，用它跑**，并且「**改这个生成器时两边都要改**」；② `scripts快照/gen_unity_arena_manifest.py` 的 import 行 `from unity_scene_to_godot import Assembler, q_mul, …`，而 `Assembler.__init__` 里 `self.b = BundleResolver(arena)` ⇒ **本类是活生成器的依赖库**（不是「只给阅读的快照」）；③ 产物 `MyGame/Assets/WarpforgeArena1/arenas/<场>/<场>_manifest.json` 在工程里，被 `WarpforgeArena1/Editor/ArenaBuilder.cs` 读 |
| **A2** | `工具/_dump_shaders_batch.py` | ✅ **活（普查工具，可重跑）** | `工具/README.md` 的「2026-09-17 新增的四个（**跑在 d:/4**）」表里列着它，产物 `资料/普查产出_0917/shader属性表_块1~4.md` + `_汇总.md` 是特效线的引用源；文件头自述「★实测记下的三条（改动本脚本或写文档前先看）」⇒ 是被反复回读的活文档的生成器 |
| **A3** | `工具/gen_arena_texslots.py` | ✅ **活** | 产物 `arenas/<场>/<场>_texslots.json`（13 场）在工程里；消费端 `ArenaBuilder.cs`（`ReadTexSlots` 一族）+ `WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs` 的文件头都点名「由 `工具/gen_arena_texslots.py` …」 |
| **A4** | `工具/gen_arena_sprites.py` | ✅ **活** | 产物 `arenas/<场>/<场>_sprites.json` 在工程里（astra / tauviorla 各一份）；消费端 `ArenaBuilder.cs` 的 `<summary>`「`arenas/<场>/<场>_sprites.json` 的一条（`工具/gen_arena_sprites.py` 从原版包里抽）」 |
| **A5** | `工具/gen_arena_psmesh.py` | ✅ **活** | 产物 `arenas/{arena3,blacklegion,darkangels,tauviorla}/<场>_psmesh.json` 在工程里；`ArenaBuilder.cs` 的降级分支就写着「跑 `工具/gen_arena_psmesh.py` 补上」 |
| **A6** | `工具/arena_mat_audit/套1_pathID索引/build_shader_index.py`（+ `build_shader_props.py` + `dump_shader_full.py`） | ⚠️ **归档但可重跑 ⇒ 按「活」改** | `工具/arena_mat_audit/README.md`：① 那是 2026-09-26 那次**已收口**审计（「13 场已全部盘完」）收进来的脚本；② **但**它自己写着「⚠️ **这些脚本只读、不碰工程**（不跑 Unity），**可以反复跑**」；③ 中间文件 `_tmp_view/mataudit/{orig_materials,mat_index,shader_index,shader_props}.json` **今天还在**（2026-09-26）⇒ 现在就能重跑。产物**只喂同目录那 5 个只读对账脚本**（`resolve.py` / `compare.py` / `diff.py` / `show_orig.py` / `dump_shader_full.py`），不进 `Assets/` |
| **A7** | `工具/arena_mat_audit/套1_pathID索引/build_mat_index.py` | ⚠️ 同上 | 同 A6（同一个 README、同一套 `_tmp_view` 中间文件） |

**判死的工具：0 个。** 所以本件没有走「死工具只做注释级处理 / 只报不改」那条路。

---

## 逐处：原写法 → 新写法 + 怎么验的

### A1 · `工具/scripts快照/unity_scene_to_godot.py`

**符号名**：`BundleResolver.__init__` · 新增 `BundleResolver._ext_table()` / `_host_sf()` / `_ext_of()` / `_pick_anchor_sf()` ·
`BundleResolver.read_obj()` · `BundleResolver.mat_data()` · `BundleResolver.shader_info()` · `Assembler.mat_of()`

**原写法**（`__init__`）：
```python
sf = self.env.objects[0].assets_file          # ← 恒取到 .sharedAssets
self.ext = {}
for i, e in enumerate(sf.externals):
    m = re.search(r'CAB-[0-9a-f]+', str(e))
    if m: self.ext[i + 1] = m.group(0)
```

**新写法**：
1. 锚点 = **对象最多的那份 CAB**（`_pick_anchor_sf()`），构造时**打印选了哪份**（A152 的「退一步」要求）：
   `[BundleResolver] battlearena1：externals 锚点 = CAB-8adfc3…（5304 个对象 / 共 2 份 CAB）`
2. 表按 **SerializedFile 分别建、分别取**（`_ext_of` + `ext_by_sf`，键 = `id(sf)`；env 是类级缓存 ⇒ `id` 稳定）；
3. `read_obj(ref, want=None, **host=None**)`：给了 `host` 就用**引用者自己那份 CAB**（A152 的「最小修法」正解，
   形态照抄 `scripts快照/gen_unity_arena_manifest.py` 的 `exts = obj.assets_file.externals`）；
4. **把 host 真接上**：`mat_data()` 从包里读材质时把它的 CAB 存成内部键 `d['_host']`，
   `Assembler.mat_of()` 再把它传给 `shader_info(..., host=)` 与两个贴图槽的 `read_obj(..., host=)`。
   （A152 原文写「`read_obj` 里已经有 ref 的宿主对象」—— **实测没有**：ref 全来自 `07_场景` 的 drip JSON，
   是纯 dict，没有宿主对象。所以这条是**新加的管道**，不是「照抄一行」。）

**怎么验的（只读复算，`UnityPy` 打开同一个包，没有写盘）**：

| 量什么 | 数字 |
|---|---|
| `env.objects[0].assets_file` 是谁 | `CAB-8adfc300739b4da5111d4bd3eae80365**.sharedAssets**`（63 个对象）—— 与 A152 报的逐字相同 |
| 两份 CAB 各有多少对象 | `.sharedAssets` **63** / 主 CAB `CAB-8adfc3…` **5304** |
| 两份 `externals` 有几条不同 | **17 / 21**（与 A152 报的「17/21」吻合）；差在 fid = 2,3,4,5,6,7,8,9,10,11,12,13,15,16,17,20,21 |
| 主 CAB 对象发出的 `fid>0` 引用，在旧表下指错包 | **1543 / 3272 = 47.2%**（口径：按 `externals[fid-1]` 逐位比） |
| **落到产物层面的量**：`07_场景/battlearena1` 的 JSON 里**真正走到 `self.ext`** 的引用 | **174 条**（另有 152 条被 `self.local` 先命中、不走 ext） |
| 这 174 条里，**只有新表能查到那个 pid** 的 | **144 条** |
| 只有旧表能查到的 | **0 条**（`old-only = 0`） |
| 两边都查不到的 | 30 条（全是 `m_Mesh {fid:5, pid:10202/10210}` 这种 **Unity 内置**：`externals` 那一条是 `unity default resources`，`Warpforge_unitybuiltinassets.bundle` 里实测**没有**这两个 pid ⇒ 两份表都表达不了，不是本修法造成的） |
| 新代码实跑（构造 `BundleResolver('battlearena1')`）：新旧候选包列表不同的条数 | **174 / 174**（相同 = 0） |
| 构造过程有没有写盘 | `工具/data/cab_bundle_map.json` 的 mtime **没变**（`_cab_map()` 命中已有缓存） |

**另一条结构实测（决定了「为什么本地优先掩盖了错误」）**：
`scenes_scenes_battlearena1.bundle` 两份 CAB **按类型分工** ——
`.sharedAssets` = `Material 16 / Texture2D 5 / Mesh 28 / Shader 2 / AnimationClip 2 / AudioClip 3 / MonoBehaviour 4 / PreloadData / AssetBundle / AnimatorController`；
主 CAB = `GameObject 1224 / Transform 236 / *Renderer / ParticleSystem 60 / MonoBehaviour 1711 / RectTransform 988 …`，**四类资产一个都没有**。
⇒ `self.local`（`_index_env` 只收 Mesh/Texture2D/Material/Shader）**整份都来自 `.sharedAssets`**，
而**跨包引用是主 CAB 那些场景对象发出的** ⇒ 两条路互补，锚点必须跟「发出引用的那一份」走。

🔴 **顺手修（同文件、同一个病）**：`shader_info()` 的缓存键原来是**裸 pid + 类级缓存**（`_shader_cache_cls`）——
`gen_unity_arena_manifest.py --all` **一个进程跑 13 场**⇒ 后一场会拿到前一场同号 shader 的属性表。
实测跨场 Shader 撞号 **6 条（pid 28–33）**，例 pid=33 = arena1 的 `Hidden/LUTBlender` vs tauviorla 的
`Everguild/Misc/URP Transparent Shadow Receiver`（**正是 `disasm_dxbc.py` 那条注释踩过的同一个坑**）。
键改成 `(self.path, pid)`（宁可少命中重查一次，不可跨包命中），并就地更正了类头那句
「shader 属性表不随 arena 变」的错记。

---

### A2 · `工具/_dump_shaders_batch.py`

**符号名**：`main()` 的 `seen_af` 块（已删）· `main()` 的 `mats` 列表 · 材质→shader 那一段解引用

**原写法**：每个包**只看第一个对象**的 `assets_file`：
```python
seen_af = False
for o in env.objects:
    if not seen_af:
        sf = o.assets_file                     # ← 恒 .sharedAssets
        cab2bundle[sf.name] = f
        externals[f] = [...]                   # ← 键 = 包名，只存了一份 CAB 的表
        seen_af = True
```
🔴 这里还有 A152 没写出来的一半：因为 `seen_af` 只放行一次，**主 CAB 从来没进 `cab2bundle`**
⇒ 任何指向主 CAB 的外部引用都会落进「外部 CAB 不在本次扫的 bundle 里」。

**新写法**：**遍历每个对象、按 SerializedFile 去重登记**（`id(sf) in seen_sf` 去重，不会对每个对象重算 externals）；
`externals` 键改 **`(bundle, CAB)`**；`mats` 条目带上**材质自己那份 CAB**；解引用改 `externals.get((bundle, cab))`；
新增**多 CAB 包点名**（实测：84 包里 15 个多 CAB，**全部**是 `scenes_scenes_*`）。生成的那段 markdown 口径说明
（`## 二、口径与判据` 里那条「材质→shader」）也一并改对了。

**怎么验的**：读码 + 用 A1 那套实测（同一个包、同 15 个多 CAB 包、2 份 CAB 的 externals 差 17/21 条）说明「原来为什么错」。
⚠️ **本工具自身的实际纠正条数没量**（要跑全 84 包 + 对 770 条材质→shader 各解两次，且它写 `.md`）—— 见「没查清 ②」。

---

### A3 · `工具/gen_arena_texslots.py`

**符号名**：`build_index()` · 新增 `pick_cand()` · `export_tex()` · `main()` 内的 `get_material()` / `get_tex()` / `slots_of()`

**原写法**：索引条目只记**包名** —— `idx['tex'][pid].append([f, nm])` / `idx['mat'][pid].append([f, nm, kws])`；
`get_material` 取 `cands[0][0]`；`get_tex` 只做「同包优先，否则 `cands[0]`」；`export_tex` 只按**名字**在包内取第一张。

**新写法**：
1. 条目改 **`[f, cab, nm]` / `[f, cab, nm, kws]`**，缓存版本 `_v` **3 → 4**（旧缓存会被判过期重建，不会按旧形状误读）；
2. 新增 **`pick_cand(cands, pid, prefer_cab, prefer_bundle, what)`**，判据 = **同 CAB → 同包 → 第一条且点名**；
3. `get_material(pid, prefer_cab)` / `get_tex(pid, prefer_bundle, prefer_cab)` / `slots_of(m, host, host_cab)`；
   `prefer_cab` 取**引用者（`MeshRenderer` / `ParticleSystemRenderer`）自己那份 CAB**（`o.assets_file.name`），
   外链材质再把它自己那份 CAB 往下传（贴图按**材质所在那份**找）；
4. `export_tex(..., cab=None)` 钉到同一份 CAB，退回按包名找时**出声**；
5. 索引建完**报一次撞号**（不同包名字不同的 pid 数与样例）。

**怎么验的**：
- 读旧缓存 `_tmp_view/tex_mat_index.json`（`_v:3`，2026-09-30 建，84 包）：**同 pid 不同名的 tex 11 条 / mat 18 条**，
  **每一条的来源都是「不同的包」**（`同一个包名出现两次` 的 pid = **0 条**）。
- 独立复算 15 个 `scenes_*` 包内、两份 CAB 的 `(类型, pid)` 交集：**13 个 arena + simpletransition = 0**，
  `mainmenuwarpforge` = 128（全是 `GameObject`）。
- 🔴 ⇒ **更正 A152**：那句「两份 CAB 的 `f` 字符串相同 ⇒ `prefer_bundle` **完全失效**、恒取第一条」**不成立** ——
  对 tex/mat 两张表，**今天没有任何 pid 会同时出现在同一个包的两份 CAB 里** ⇒「同包优先」这一档**仍然有分辨力**。
  本条修的是**判据完整性**（`m_FileID` 是分包局部的，而「包名」**不足以定位一份文件**：两份 CAB 的 externals 差 17/21），
  不是为了修一个今天就在出错的取值。**已把这个更正写进 `build_index` 的 docstring**（照铁律 5：就地改、留更正痕迹）。

---

### A4 · `工具/gen_arena_sprites.py`

**符号名**：`build_index()` · `ext_obj()` · `main()` 的 `cabs` / `tex_pref` / 贴图导出循环

**原写法**：
```python
idx.setdefault(str(o.path_id), []).append([f, o.type.name])   # 只记包名
...
cands = [h for h in hits if h[1] == kind]                    # 只按类型筛
b = cands[0][0]                                              # ← 包名序第一条
```
导出那一趟还有一条：`next((o for o in env.objects if o.type.name=='Texture2D' and o.path_id==…), None)`
（`env.objects` 是**两份 CAB 聚合**的）。

**新写法**：条目 **`[f, cab, type]`**（缓存加 `_v:2`，旧缓存自动作废）；
`ext_obj(path_id, kind, prefer_cab=None, prefer_bundle=None)` = **同 CAB → 同包 → 第一条 + 点名**，
且命中时**必须落在那份 CAB**（`o2.assets_file.name != pick[1]` 就跳过）；
`main()` 建 `cabs[(类型,pid)]` 与（**只给导出用、不写进 JSON** 的）`tex_pref[pid] → (宿主包, 宿主 CAB)`；
导出改**按 pid + CAB** 取（退化成按 pid 时**出声**）。⚠️ 写出去的 `<场>_sprites.json` **键集一个字没变**（`ArenaBuilder` 不用动）。

**怎么验的**：这条**是真缺陷**（与 A3 不同）—— 跨包同 pid 不同名实测 **Texture2D 11 / Material 18 条，来源全是 `scenes_*`**，
而 `ext_obj` 原来的 `cands[0]` 就是**包名序第一条**（字母序靠前那个场赢），
**没有任何「同源优先」这一档** ⇒ 引用者在自己包里时取到的是别的场的对象。已写进 `build_index`/`ext_obj` 的 docstring。

---

### A5 · `工具/gen_arena_psmesh.py`

**符号名**：`main()` 的 `ghits` 块 · 用 `ghits` 的那段打印

**原写法**：`left = need_global - done` + `if not left: break` ⇒ **凑齐就走**；`ghits[pid] = (name, fn)` 先到先得、
**不记 CAB、不报撞号**。

**新写法**：**扫完全部包**（去掉 `break`），候选收成 `ghits[pid] = [(包, CAB, 名字), …]`；
凡「同 pid、名字不同」的**逐条点名**（照 `gen_vfx_texture_mips.py` 的正例）；取值仍按**包名序第一条**（= 原口径，不改已有产物）。

**怎么验的**：独立复算 15 个 `scenes_*` 包内**跨包**同 pid 不同名的 **Mesh = 82 条**（与 A152 逐条吻合，样例：
`pid=88 → 'Bunker Foreground1' / 'Curtain 21' / 'Generator.001'`）。取值口径没变 ⇒ **已有产物不变**，只是不再静默。

---

### A6 · `工具/arena_mat_audit/套1_pathID索引/build_shader_index.py`（连带 `build_shader_props.py` + `dump_shader_full.py`）

**符号名**：`build_shader_index.py` 模块级主循环 · `build_shader_props.py` 模块级主循环 · `dump_shader_full.py` 的反查 `names`

**原写法**：`idx[str(o.path_id)] = nm` + `missing.discard(...)`（全局裸 pid → 名字，**零歧义检测**）；
`build_shader_props.py` 更糟（`idx[pid] = {...}` 是**覆盖** = last-write-wins，取到哪个看扫到哪张文件）；
`dump_shader_full.py` 把这张表**反过来用**：`names = {si[k]: k for k in want if k in si}`。

**新写法**（三处一套）：
1. **保住消费端格式**：顶层仍是 `pid → 值`（`resolve.py` / `compare.py` / `diff.py` 都是 `shidx.get(str(pid))`，**不能改成嵌套**）；
2. 值改**首见为准**，另存 **`_amb`** 撞号表（`pid → [[名字, 包, CAB], …]`，**全列出来**）；
3. **扫完所有文件**（去掉 `if not missing: break` / `while need and …`）——
   ⛔ 早停会让撞号表**残缺**，那等于**假报「没有撞号」**（正是 `disasm_dxbc.py` 那条注释记录过的形状）；
4. `dump_shader_full.py` **不做反查**：它本来就在逐对象读 `pf.get('m_Name')` ⇒ 名字直接取对象自己的；
   索引只当「pid 在不在」的存在性检查，撞号的 pid **点名**而不是替它选一个名字（另把 `bundle` / `cab` / `pathId` 记进结果留痕）。

**怎么验的**：`_amb` 是**新增键**（pid 键全是数字串）⇒ 五个消费端用的 `.get(str(pid))` 全部不受影响（逐个读过）；
实测撞号数 **Shader 6 条（pid 28–33）**，与 A152 逐条吻合（`pid=33`：`Hidden/LUTBlender` vs
`Everguild/Misc/URP Transparent Shadow Receiver`）。⚠️ **`_amb` 本身没在真数据上生成过**（那些脚本会写 `_tmp_view/`）—— 见「没查清 ③」。

---

### A7 · `工具/arena_mat_audit/套1_pathID索引/build_mat_index.py`

**符号名**：`main()` · `slim()`

**原写法**：`idx.setdefault(str(o.path_id), slim(d))` + `idx[pid]['_b'] = f`（**只记包名、不记 CAB**）——
而且 `_b` 是**无条件覆盖**（last-write-wins），`name` 却是 setdefault（first-write-wins）
⇒ 撞号时**`name` 与 `_b` 可能来自两个不同的包**。

**新写法**：**保留 pid 键**（见下「为什么没换成 `(bundle, pid)`」），但 ① `_b` 与 `name` **同源**（都首见为准）；
② 条目加 **`_cab`**；③ 另存 **`_amb`** 撞号表 + 逐条点名。

🔴 **为什么没把键改成 `(bundle, pid)`**（A152 给的第一选项）：`mat_index.json` 的消费端是 `resolve.py`
（`idx.get(str(pid))`），**它不在本件白名单里** ⇒ 换键格式会**静默**让它一条都查不到（全部落进「未解出」）。
所以取 A152 给的**第二个选项**（「或至少撞号全列出来」），并把 CAB 一并记进条目。已把这个取舍写进文件头。

🔴 **顺手修（同文件、独立缺陷）**：原来 Shader 那一支读的是**顶层 `m_Name`** ——
实测（2026-10-06 抽验 5 个）**恒为空串**（真名在 `m_ParsedForm.m_Name`）⇒ **本文件重跑一次就会把
`shader_index.json` 写成 148 条全空**（它和 `build_shader_index.py` **写同一个文件**）。
现在是 `(m_ParsedForm.m_Name) or m_Name or ''`，判据同 `arena_mat_audit/README.md` 那条。

**怎么验的**：读 `shader_index.json` 现值 = 148 条、**空值 0 条** ⇒ 现盘上是 `build_shader_index.py` 的产物
（即 `build_mat_index.py` 没在它之后跑过）；抽验 5 个 Shader 的 `top m_Name = ''`、`m_ParsedForm.m_Name` 是真名。
撞号数实测 **Material 18 条**（与 A152 吻合）。

---

## 跨仓（A1）：`d:/2/` 那份要不要同步

**要同步**，但**我没动它一个字**（那是档案库，要用户点头）。

- 关系正本 = `工具/README.md` 的 `scripts快照/` 那一行：**「改这个生成器时两边都要改（或改完回来同步一次）」**，
  并记着 **2026-10-01 已同步过一次**（当时两边逐字节相同，md5 `b99e4cf476cc3cb2034eab600a6c6348`）。
- `sync_from_d2.py` 的 `TOOL_SNAPSHOT` 名单里 `unity_scene_to_godot.py` 的注释就是
  **「★ 上面的依赖库（Assembler / 材质解析）」** ⇒ 它就是「两边都要改」的那个依赖库。
- ⚠️ 同步时要注意**方向**：这条口径历史上翻过车（2026-09-30 实测 `d:/2` 那份比仓库**少 120 行**，
  跑 `d:/2` 那份会写出坏 manifest；10-01 才同步回 `d:/2`）。**本件改的是仓库这一份**，
  所以 `d:/2` 那边**现在是旧的**（`BundleResolver` 还是 `objects[0].assets_file` 那版）。
- 实测两边**改动前是同源的**：`d:/2/Warpforge_tools/scripts/unity_scene_to_godot.py` 与仓库那份
  **都是 164014 字节**（mtime 分别是 2026-09-22 13:04 / 14:10）；**改完之后 `d:/2` 那份没被碰**
  （mtime 仍是 2026-09-22 13:04）⇒ 现在两边**确定不同**，要同步。
- ⛔ 另外提醒：如果将来有人从 `d:/2` 反向拷回来，会把本件 9 个文件里的 A1 **整段退回去**（其余 8 个在 `工具/` 下，不在同步名单里，不受影响）。

---

## 没查清的部分

1. **A1 修完「产物到底差多少」没查清（本件禁跑生成器）。**
   已知的静态判据是「174 条走到 ext 的引用里，144 条的新候选包是对的、30 条是 Unity 内置（两份表都表达不了）」，
   但**实际少画/多画了哪几个网格或贴图**要跑一次才能数。
   **还差**：跑 `gen_unity_arena_manifest.py --arena battlearena1`（**会写 `arenas/`**），
   比 manifest 里 mesh / 贴图条数与 `ArenaBuilder` 的自检 missing 清单。⇒ 建议并进「真 Play / 攒批」那一轮。
2. **A2 的实际纠正条数没量**（A152 报 1/770，我没复算）。**还差**：跑一遍 84 包 + 770 条材质→shader 的双表对拍
   （会写 `资料/普查产出_0917/*.md`）。**判据形状已定**：对每条 `(bundle, cab, fid)`，比 `externals[(bundle,cab)][fid-1]`
   与 `externals[(bundle, 第一个 CAB)][fid-1]`。
3. **A3/A4/A5/A6/A7 修完的产物差异没量**：这五个都会写盘（`arenas/<场>/*.json`、`_tmp_view/mataudit/*.json`），
   本件禁跑 ⇒ **`_amb` 表、`_cab` 字段、`_v:4`/`_v:2` 都是「已把代码改成会生成它们」，不是「已生成」**。
   实际数字引的是 A152 的实测 + 本件**独立复算**的那几条（Material 18 / Texture2D 11 / Shader 6 / Mesh 82 逐条吻合）。
4. **A1 的 `read_obj(host=)` 只接到 3 个调用点**（`shader_info` 的 shader ref、材质的两个非主贴图槽）。
   剩下 5 个 `read_obj` 调用点（ColorLookup LUT、粒子/网格那几处的 ref）ref 都来自 drip JSON、
   **没有宿主对象**，只能走默认锚点（= 主 CAB，已实测是对的）。**没查清**：那几处若真落在 `.sharedAssets` 里
   会不会与主 CAB 口径冲突 —— 实测两份 CAB 的 `(类型, pid)` 交集 = 0 ⇒ **结构上不会**，但**没有逐条跑过**。
5. **A152 的 🅱️ 档（B1–B12）本轮一处没动**（不在本件范围）。其中仍挂着的：
   `gen_card_vfx_by_card.py` 的注释「实测 path_id 唯一」是**假结论**；`dump_animfx.py` 的 `go_by_pid` **死代码**（上了膛的枪）；
   `_probe_prefab_deps.py --bundle` 指向 `scenes_*` 时会静默错 40%；`extract_missing_shaders._ext_name()` 会把**报错文案里的包说错**；
   `bundle_dump_objects.py` 对 `scenes_*` 两份 CAB 混着印、没有 CAB 列。

---

## 顺手发现（⛔ 别自己改；交调度台分流）

1. 🔴 **A152 的 A3/A4 那句「对双 CAB 包 `prefer_bundle` 完全失效」实测不成立 —— 建议进正本更正。**
   实测：15 个 `scenes_*` 包里两份 CAB 的 **`(类型, pid)` 交集 = 0**（唯一例外 `mainmenuwarpforge` 的 **128 个 GameObject**）；
   旧 `tex_mat_index.json` 里「同一个包名出现两次」的 pid = **0 条**。
   原因很值得记：**两份 CAB 按类型分工**（`.sharedAssets` = 资产，主 CAB = 场景对象）。
   ⇒ 不改的话，下一个会话会照 A152 以为「同包优先等于没做」，把力气花错地方。
   （本件已把更正写进 `gen_arena_texslots.py` / `gen_arena_sprites.py` 的 docstring；
   **A152 报告本身在 `资料/普查产出_1006/`，不在我白名单里，没动**。）
2. 🔴 **两个写手写同一个文件**：`build_shader_index.py` 与 `build_mat_index.py` **都写 `_tmp_view/mataudit/shader_index.json`**，
   而后者原来用的是**恒空的顶层 `m_Name`** ⇒ 谁后跑谁定内容。本件已把后者改对（读 `m_ParsedForm.m_Name`），
   但**「两个写手」这个隐患还在**，建议记进坑表（并考虑让 `build_mat_index.py` 别写 shader 那一半）。
3. 🔴 **类级缓存按裸 pid 做键**（`BundleResolver._shader_cache_cls`）—— 已在 A1 里顺手修 +
   就地更正了类头那句错记（「不随 arena 变」实测不成立）。**形状值得进坑表**：
   「凡是**类级/模块级**缓存，键里带上「它属于哪份文件」」。
4. 🔴 `dump_animfx.py` 的 `go_by_pid` 仍是一处**死代码 + 全库 GameObject 撞号 1291** 的组合（A152 B9 已判）。
5. **`.sharedAssets` 的「按类型分工」是个好用的结构判据**（资产 vs 场景对象）—— 建议写进 `资料/已知的坑.md`：
   它解释了「为什么双 CAB **今天**在 arena 那 13 场不发作」（同包内 `(类型,pid)` 不撞），
   也是「A1 为什么必须跟主 CAB 走」的根据。**主菜单包是唯一的反例**（128 个 GameObject 同类型撞号）。
6. **A152 的「40.4% 指错包」与本件的「174/174 候选包不同、144 条只有新表查得到」是两个口径**——
   前者按**所有 `fid>0` 的引用**算，后者按**真走到 `self.ext` 的那批**算。两个都对，但**后者才是产物层面的量**；
   建议引用时写清是哪一个。
7. **`unity_scene_to_godot.py` 的 dump 是「只读」还是「会写」要分清**：它**会写** `d:/2/战场演示` 与 `DATA_DIR`
   （`--out` 与 `_cab_map()`），所以本件所有复算都**只 import 它的类、没调 `main()`**。

---

## 改动清单（9 个 `.py`，全部只改代码/注释，没有产物落盘）

```
工具/scripts快照/unity_scene_to_godot.py               （A1 + shader 缓存键）
工具/_dump_shaders_batch.py                            （A2）
工具/gen_arena_texslots.py                             （A3）
工具/gen_arena_sprites.py                              （A4）
工具/gen_arena_psmesh.py                               （A5）
工具/arena_mat_audit/套1_pathID索引/build_shader_index.py  （A6）
工具/arena_mat_audit/套1_pathID索引/build_shader_props.py  （A6 连带）
工具/arena_mat_audit/套1_pathID索引/dump_shader_full.py    （A6 连带）
工具/arena_mat_audit/套1_pathID索引/build_mat_index.py     （A7）
```

**自检**：本件属「零自检」类（纯 Python 工具）⇒ **一条 Unity 自检都没跑**（按铁律 12 第 3 条：纯工具零条），
每个文件改完跑过**语法编译**（`py_compile`，9/9 通过），并逐个核对**行尾没被翻**
（`unity_scene_to_godot.py` 是 **CRLF**、其余 8 个 **LF**，改完 `CRLF == LF` 计数不变）；
`git status` 确认**没有多出任何新文件**（`__pycache__` 未跟踪）。
