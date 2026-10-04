# A152 · `scenes_*` 双 CAB 的 pid 陷阱 —— 普查「还有哪些工具会踩」

> 2026-10-06 · 执行子代理 · **只读普查**（⛔ 未改任何 `.py` / `.cs` / 正本 / git）
> **零 Unity 自检**（本件属 A 表「① 零自检」那一组：纯 Python 工具）
> 判据 = ① `资料/普查产出_1006/A127_工具两件.md`（实测数字 + 正确形态）
> ② `资料/已知的坑.md`「`scenes_*` 那 15 个包 = 双 CAB + 同一套 pid 空间」
> ③ 本件**新跑的只读实测**（命令与数字全部在下面，可复现）
> 扫过：`d:/4/Unity/工具/**/*.py` = **177 个**（含 `scripts快照/` 13 个、`arena_mat_audit/` 16 个）
> ⚠️ `d:/4/工具/` **不存在**（只有 `d:/4/CLAUDE.md` `README.md` `Unity/` `_blender_work/` `_tmp_view/` `项目任务.md`）

---

## 结论（一句话）

**会踩 7 处 · 构造有陷阱但今天实测不发作 12 处 · 安全 20 处 · 判不出 0 处**（共 39 处「按 pid 建索引 / 拿 pid 反查名字」的地方）。

三条**新实测**把这件事的边界钉死了（都不在 A127 里，是本件量的）：

| 量什么 | 结果 |
|---|---|
| **双 CAB 只有 15 个，全在 `scenes_*`** | 84 包里 69 个单 CAB · 15 个多 CAB。工具点名的那些包（`menus_assets_all` · `battleprefabs_vfxandmisc` · `soundcollection` · `cosmeticsso` · `battlesharedresources` · `Waprforge_monoscripts` · `shaders_assets_all` · `boosterpacks` · `cosmeticscardbacksimages` · **4 个 `atlasindividual_*`**）**全部单 CAB** |
| **「随手挑第一份 CAB」会指错多少条引用** | `scenes_*` 15 个包合计 **53,484 条外部引用**，其中 **21,624 条（40.4%）会指错包**（arena 每场 39.7–43.3% · 主菜单 30.9% · `simpletransition` 0%） |
| **`env.objects` 聚合出来是谁赢** | 迭代序 = `bf.files` 序 = **`.sharedAssets` 在前、主 CAB 在后** ⇒ `{o.path_id: o for o in env.objects}` **主 CAB 全胜**（arena1 5304/5304 · 主菜单 2624/2624，`.sharedAssets` **0 条**幸存） |

🔴 **另外一条把「同类型撞号」的分布量清了**（这决定了「按类型分字典」到底会不会错）：

| 包 | 交集 pid | 同类型 | 异类型 | 同类型且名字不同 |
|---|---|---|---|---|
| 13 个 `scenes_scenes_battlearena*.bundle` | 18–80 | **0** | 全部 | **0** |
| `scenes_scenes_mainmenuwarpforge.bundle` | **593** | **128** | 465 | **128（全是 GameObject）** |
| `scenes_scenes_simpletransition.bundle` | 2 | 0 | 2 | 0 |

⇒ **arena 那 13 场「按类型分字典」今天是对的（同类型撞号 = 0）—— 但那是数据性质，不是结构保证**；
换个包（主菜单）同一条写法就错。`GameObject` 的 pid→对象 字典在主菜单包里**确实被污染 128 条**。

🔴 **还有一条更值钱的**：这 7 处「会踩」里，**有 5 处的病根不是双 CAB，是「跨包同 pid 不同名」**——
pid 是**分包局部**的，两个无关的包 pid 空间独立。实测（全 84 包）：

| 类型 | pid 数 | 同 pid 不同名 |
|---|---|---|
| GameObject | 35,009 | **1,291** |
| Mesh | 465 | **82** |
| Material | 915 | **18** |
| Texture2D | 2,975 | **11** |
| Shader | 148 | **6** |
| AudioClip | 2,542 | **5** |
| **Sprite** | 3,316 | **0** |

⇒ **跨包全局 `pid → 名字` 表在这些类型上本来就是坏的**，而且**撞号的来源就是 `scenes_*`**：
`GameObject` 的 1291 条每条都涉及全部 15 个场景包；`Material` 18 条、`Shader` 6 条**无一例外全在
`scenes_scenes_battlearena*`**（因为每个场景包的主 CAB 都从 pid=1 开始编号）。
（`GameObject 1291` 与 A127 报的「全类型表 49213 条 + **撞号 1291 条**」**逐位吻合** ⇒ 两条独立测得同一个数。）

---

## 逐处清单

> 格式：**`文件:符号名`** | pid 从哪来 | 判据 | 结论 | 最小修法
> 「正确形态」的参照基准 = `_probe_deckinfo.py:fileid_note()`（**先钉引用者所在 CAB**，再取 `externals[m_FileID-1]`）
> 与 `gen_unity_arena_manifest.py:966`（`obj.assets_file.externals`，**用对象自己那一份**）。

### 🅰️ A 档 · 会踩（今天就在数据上成立，会静默取错）

**A1. `Unity/工具/scripts快照/unity_scene_to_godot.py` : `BundleResolver.__init__`（:328）** ← **最毒的一处**
- **pid 从哪来**：包内（`_index_env` 索引 `env.objects` = 两份 CAB 聚合）+ externals（跨包）
- **判据**：`sf = self.env.objects[0].assets_file` —— 拿**第一个对象**的 SerializedFile 当「本包的 CAB」。
  实测 `env.objects` 恒以 `.sharedAssets` 起头 ⇒ **`objects[0]` 恒是 `.sharedAssets`**（4/4 场实测，
  例 `scenes_scenes_battlearena1` → `CAB-8adfc300739b4da5111d4bd3eae80365.sharedAssets`）。
  而它的 externals 与主 CAB **逐位不同 17/21（81%）**（主菜单 16/17 = 94%）。
  `self.ext[fid]` 就是拿 `sf.externals` 建的（:329-332）⇒ **按 fid 反查 CAB 名，81% 的位置给出错误的 CAB**。
  ⚠️ **这个类处理的正是 `scenes_scenes_%s.bundle`**（:319）⇒ 直接踩在陷阱正中。
- **结论**：**会踩**。全 `scenes_*` 合计 **21,624/53,484 = 40.4%** 的外部引用会指错包。
  典型：`arena1` 里 **696 条**本该指 `CAB-daee69d99912a3dbccd986…` 的，指成了 `CAB-0dc9fe43572a91eb…`。
  ⚠️ 还有一层**掩盖**：`read_obj` 先查 `self.local`（pid 命中就直接返回）⇒ 只有「本地查不到、
  真要走跨包」的那批才暴露；本地命中那批**掩盖**了错误，所以症状是「一部分贴图/网格是别的场的」。
- **最小修法**：`sf` 换成**引用者自己那一份**。`read_obj(ref, want)` 里已经有 ref 的宿主对象，
  取 `o.assets_file.externals` 即可 —— **同目录的兄弟文件 `scripts快照/gen_unity_arena_manifest.py:966`
  就是这么写的（`exts = obj.assets_file.externals`），照它抄**。
  退一步（拿不到对象时）：`sf = max(bf.files.values(), key=lambda s: len(s.objects))` 并**打印选了哪份**。

**A2. `Unity/工具/_dump_shaders_batch.py` : `main()` 的 `seen_af` 块（:286-292）**
- **pid 从哪来**：全库（`os.listdir(AA)` 全部 `.bundle`）+ externals
- **判据**：`seen_af` 保证「每个包只看**第一个对象**的 `assets_file`」，与 A1 是**同一形态**
  （`sf = o.assets_file` where `o` = `env.objects` 的第一个 ⇒ 恒 `.sharedAssets`）。
  `cab2bundle[sf.name] = f` 这半边**是对的**（CAB 名唯一，两份都收得到），
  但 `externals[f] = sf.externals`（键 = **包名**）只存了**一份** CAB 的表。
  文件头 :469 自述这条路是「`SerializedFile.externals[n-1]` 的 `archive:/CAB-xxx/…` → CAB 名 → bundle → pathID」。
- **结论**：**会踩**（形态已成立）。实测「材质 → shader」只有 **1/770** 条受影响
  （因为绝大多数跨文件材质→shader 引用落在**单 CAB** 包里、两份 CAB 无从分叉），
  但**那是数据凑巧** —— 模板一改就变 A1 那个量级。
- **最小修法**：`externals` 键从 `bundle` 改成 `(bundle, CAB)`；或按对象 `o.assets_file.externals` 现取。

**A3. `Unity/工具/gen_arena_texslots.py` : `build_index()`（:66-95）+ `get_material()`（:156-173）+ `get_tex()`（:175-183）**
- **pid 从哪来**：**全库**（`os.listdir(aa)` 全部 `.bundle`）
- **判据**：`idx['mat'].setdefault(str(o.path_id), []).append([f, nm, kws])`（:90）—— 条目里记了
  **包名 `f` 但没有 CAB**。消费端两个都**取第一条、不报歧义**：
  `get_material` :164 `host = cands[0][0]`；`get_tex` :180-183 「优先 `b == prefer_bundle`，否则 `cands[0]`」。
  ⚠️ **2026-10-07 更正（铁律 5；A161 ⑦ 就地改）**：这一句原来写的是
  > 🔴 **对双 CAB 包，`prefer_bundle` 这个偏好完全失效** —— 两份 CAB 的 `f` **字符串相同**，
  > 所以恒**取第一条**（= 扫描时先遇到的那份）。

  ——**实测不成立**（A160 复算过 15 个包，A161 又用 UnityPy 独立复算一遍，两次同结论）：
  15 个 `scenes_*` 包里两份 CAB 的 **`(类型, pid)` 交集 = 0**（13 个 arena + `simpletransition` 都是 0；
  唯一例外是**主菜单包 `mainmenuwarpforge` 的 128 个 GameObject**）⇒ 同一个 pid 在同**一个包内**
  **不会**有两条**同类型**候选 ⇒「同包优先」这一档**今天仍然有分辨力**，「恒取第一条」**不会**取到
  另一份 CAB 的同名件。**错因**：把「两份 CAB 的 `f` 字符串相同」（真的）直接推成「偏好完全失效」，
  **没有去量两份 CAB 的 pid 空间**。
  ⇒ 本条的定位要改成：它修的是**判据完整性**（`m_FileID` 是**分包局部**的，而「包名」**不足以定位
  一份文件** —— 两份 CAB 的 `externals` 差 **17/21** 条），**不是**「今天就在算错」。
  🔑 **真正的撞号来源是【跨包】**（pid 是分包局部的：跨包同 pid 不同名的 Material 18 · Texture2D 11 条，
  全在 `scenes_*`）—— 而「比包名」这一档只在 `prefer_bundle` **恰好就是正确那个包**时有效，
  落到 `cands[0]` 时仍是「包名序第一条」⇒ 这才是 A160 要加 `prefer_cab` + 出声的原因
  （下面那条「会踩」的结论说的是**这一路**，不是双 CAB 那一路）。
  （更正痕迹：`gen_arena_texslots.py` / `gen_arena_sprites.py` 的 docstring 里 A160 已就地写过一遍，
  这里补上本报告这一份；**原话留着，别删**。）
  `get_tex` 的 docstring 自己写着「小号 pathID 会撞号」，但它只做了「同包优先」，**没有做「同 CAB 优先」**。
- **结论**：**会踩**。实测撞号：**Material 18 条 / Texture2D 11 条，来源无一例外是
  `scenes_scenes_battlearena*`**（正是它要索引的包）。
- **最小修法**：条目改成 `[f, cab, nm, …]`（`build_index` 里已经在 `for cab` 上，取得到）；
  `get_*` 的优先序改成 **同 CAB → 同包 → `cands[0]` 且出声**。

**A4. `Unity/工具/gen_arena_sprites.py` : `ext_obj()`（:172-195）+ 局部 `idx`（:67-78）**
- **pid 从哪来**：**全库**（`idx[pid] = [[f, 类型]]`，:78）
- **判据**：`:185 b = cands[0][0]` —— 只按**类型**过滤（`cands = [h for h in hits if h[1] == kind]`），
  **不按 CAB**。:76-77 的注释**已经知道撞号**（「实测 tauviorla 的精灵材质就撞了」）但只做到「存候选列表 + 按类型筛」。
- **结论**：**会踩**（类型不同的那两支）。实测：`Sprite` 撞名 **0 条**（⇒ `ext_obj(spid,'Sprite')` 目前安全），
  但 `Texture2D` **11 条**、`Material` **18 条** ⇒ 走 `EXT` 那两支时会静默取错。
- **最小修法**：`idx` 条目加 CAB；`ext_obj` 命中多个同类候选时**列出并出声**（别默认第一条）。

**A5. `Unity/工具/gen_arena_psmesh.py` : `main()` 的 `ghits` 块（:88-105）**
- **pid 从哪来**：**全库**（`for fn in sorted(os.listdir(AA))`）
- **判据**：`ghits[o.path_id] = (name, fn)`，外层 `left = need_global - done` + `done.add(...)`
  ⇒ **先到先得**（按**包名排序**，字母序靠前的赢）。记了 `fn` 但**不记 CAB**、**不报撞号**。
- **结论**：**会踩**。实测 `Mesh` 同 pid 不同名 **82 条**（其中 `astramilitarum` 涉 80、`emperorschildren` 77）
  ⇒ 会挑到错误场次的网格名。
- **最小修法**：命中多条时把候选**全列出来并出声**；或改成「按引用者所在包优先」。

**A6. `Unity/工具/arena_mat_audit/套1_pathID索引/build_shader_index.py` : `main()`（:20-47）**
（连带 **`build_shader_props.py:18-43`** 同一张表、**`dump_shader_full.py:19`** 把它**反过来用**）
- **pid 从哪来**：**全库**（:21 拼 `AA` 全部 `.bundle` + `AF` 解包目录）
- **判据**：`idx[str(o.path_id)] = nm`（:40）—— 全局 `pid → shader 名`，`missing.discard` 先到先得，**零歧义检测**。
  `dump_shader_full.py:19` 更狠：`names = {si[k]: k for k in want if k in si}` —— **把撞号的表反查**。
- **结论**：**会踩**。实测 `Shader` 同 pid 不同名 **6 条，全部来自 `scenes_scenes_battlearena*`**：
  ```
  pid=33 → arena1「Hidden/LUTBlender」 · tauviorla「Everguild/Misc/URP Transparent Shadow Receiver」 · emperorschildren 又一条
  pid=28/29/30/31/32 同形（tauviorla / darkangels / astramilitarum / emperorschildren 互撞）
  ```
  🔴 **这个坑已经咬过一次**：`disasm_dxbc.py:197` 的注释写着
  「`Hidden/LUTBlender` 又漏 —— 它在 `scenes_scenes_battlearena*` 里」。
- **最小修法**：全局表**要么键带包名、要么撞号全列出来**（照 `_probe_deckinfo.pid_name_map()` 的 `amb` 写法）；
  `dump_shader_full` 别用反查 —— 它本来就有 `pf.get('m_Name')`，直接按名字匹配。

**A7. `Unity/工具/arena_mat_audit/套1_pathID索引/build_mat_index.py` : `main()`（:29-47）**
- **pid 从哪来**：**全库**（:31 `sorted(f for f in os.listdir(AA) if f.endswith('.bundle'))`）
- **判据**：文件头 :3-4 自述「**全局索引：pathID → Material 的瘦身 typetree（84 个 aa bundle）**。
  **为什么按 pathID：这批包 `m_FileID` 不可信**」—— `idx.setdefault(str(o.path_id), slim(d))`（:38）先到先得，
  另存 `idx[pid]['_b'] = f`（:39，**只记包名、不记 CAB**）。
- **结论**：**会踩**。实测 `Material` 同 pid 不同名 **18 条，涉及 14 个包，全部是 `scenes_scenes_battlearena*`**。
  ⚠️ 而且「`m_FileID` 不可信」这个理由**推不出「pid 可以当唯一键」** —— 两个问题独立，
  实测 pid 也**不是**唯一键。同一个理由写在 `dump_orig.py:4` 与 `gen_arena_texslots.py:212`。
- **最小修法**：键改 `(bundle, pid)`（`_b` 已经在算了，把它提到键里）；或至少撞号全列出来。

### 🅱️ B 档 · 构造上有陷阱，但今天实测不发作（**数据性质保证，不是结构保证**）

> 共同形态：**读 `scenes_scenes_*.bundle` + `for o in env.objects`（两份 CAB 聚合）+ 按 pid 建「按类型分」的字典**。
> 实测 arena 场包**同类型撞号 = 0** ⇒ 今天每个类型字典的键只来自一份 CAB ⇒ **结果与只读主 CAB 等价**。
> ⛔ 但这**不是**因为代码做对了 —— 是因为 arena 的两个 CAB 恰好类型不重叠。
> **主菜单包（128 条同类型 GameObject 撞号）同一条写法就会错**（那 128 条由主 CAB 胜，`.sharedAssets` 的被静默影子掉）。

| # | `文件:符号名` | pid 从哪来 | 判据 | 结论 | 最小修法 |
|---|---|---|---|---|---|
| B1 | `arena_mat_audit/套1_pathID索引/dump_orig.py:main()`（:19-33） | 单包（`scenes_scenes_%s.bundle`，4 场） | `gos/mats/shaders/trs[o.path_id]`，`env.objects` 聚合 | 不发作（该场同类型撞号 0） | 字典建在**主 CAB** 上：`max(cabs.values(), key=lambda s: len(s.objects))` |
| B2 | `arena_mat_audit/套2_逐场对账/orig2.py:main()`（:72-77） | 单包（4 场） | `inbundle[o.path_id] = Material typetree` | 同上 | 同上 |
| B3 | `gen_arena_mirror.py:main()`（:81-95） | 单包（13 场） | `mb/tr/go[o.path_id]` | 同上 | 同上 |
| B4 | `gen_arena_meshkeywords.py:main()`（:69-80） | 单包 | `go_names/mats[o.path_id]` | 同上 | 同上 |
| B5 | `gen_arena_quality_toggle.py:main()`（:66-76） | 单包 | `names[o.path_id]` | 同上 | 同上 |
| B6 | `gen_anim_address_map.py:resolve_bundle()`（:226-234） | 单包 | `byid = {o.path_id: o for o in objs}` —— **但返回值键是 `(bundle_dir, pid)`**（:565 `resolved[(d,pid)]`）⇒ 跨包那半边**是对的** | 不发作（残留：包内双 CAB 时取「后写的」= 主 CAB） | `byid` 按 CAB 分两份；命中两份**出声** |
| B7 | `gen_card_vfx_by_card.py:unitypy_names()`（:211-212） | 单包（`battleprefabs_vfxandmisc_assets_all` = 单 CAB） | `objs = {o.path_id: o for o in env.objects}`；:243 注释自述「跨文件引用用**全库 path_id 索引**解 —— **实测 path_id 唯一**」 | **不踩**（包单 CAB）；🔴 **但「path_id 唯一」这句实测不成立**（GameObject 1291 / Mesh 82 / Material 18 / Texture2D 11 / Shader 6） | **改注释**（这句会误导下一个会话）；`objs` 键带包名 |
| B8 | `menu_dump.py:sprite_pid_map()`（:105-150） | **全库**（84 包） | `out[str(o.path_id)] = nm`，**last-write-wins、零歧义检测**；docstring 自认「同名不同包撞号时**会静默取错**；命中冲突由调用方报」（调用方 `sprite_index()` :208 只比对了「切片缓存 vs 真包」那一路） | **实测不踩**：全 84 包 **3316 个 Sprite pid，同 pid 不同名 = 0 条** | 建议补 `amb` 表（照 `_probe_deckinfo.pid_name_map()`），**别等它坏了才修** |
| B9 | `dump_animfx.py:main()`（:259-265） | **全库**（`UnityPy.load(*files)` 一个 env） | `go_by_pid[o.path_id] = o`（GameObject），而全库 GameObject 撞号 **1291** | **不踩 —— 因为 `go_by_pid` 是死代码**：全文只有 :262/:265 两行，**没有任何地方读它**（`root_name_of` 走的是 PPtr `.read()` 自己解析） | 删掉，或改 `(bundle, pid)` 键；**并在注释里写明「别按 pid 全局查」**（否则下一个人会把它用起来） |
| B10 | `_probe_prefab_deps.py:get_sf()`（:52-53）+ `ExtResolver.ext_raw_path()`（:170） | 单包 | `sf = next(v for v in bf.files.values() if type(v).__name__ == "SerializedFile")` = **`bf.files` 的第一个** = `.sharedAssets`；`file_id → self.sf.externals[file_id-1]` | **默认用法不踩**（`BUNDLE = battleprefabs_vfxandmisc_assets_all.bundle`，单 CAB）；⚠️ **`--bundle` 是可传参的 ⇒ 指向任一 `scenes_*` 就静默错 40%** | 按对象取 `assets_file.externals`；或至少把「选了哪份 CAB」**打出来** |
| B11 | `extract_missing_shaders.py:` `get_sf()`（:316-317 / :595-596）+ `_ext_name()`（:565-575） | 单包 | 同一「取第一个 SerializedFile」形态。**但引用解析那一半是对的** —— `_collect_refs` 收的是 `read()` 出来的 PPtr，UnityPy 自带 `assetsfile`。只有 `_ext_name(sf, fid)` 用 `sf.externals` | **不踩产物**：`_ext_name` 只用在**报错文案**里。⚠️ 但它会把「指向哪个包」**说错** | `_ext_name(o.assets_file, fid)` |
| B12 | `bundle_dump_objects.py:main()`（:42-57） | 单包（argv） | 只打印 `pid / 类型 / 名字`，不建索引 | **不踩**（无 pid 反查）。⚠️ 给 `scenes_*` 时会**两份 CAB 混着印、没有 CAB 列** ⇒ 读的人会以为是一个 pid 空间 | 加一列 CAB（`o.assets_file.name`） |

### 🟢 安全（判据写清为什么）

| # | `文件:符号名` | 为什么安全 |
|---|---|---|
| S1 | `_probe_deckinfo.py:pid_name_map()`（:375-437） | **正确形态（本件的参照基准）**：`seen[pid] = {(类型, 名字, 包)}`，撞号进 `amb` **全列出来、不替读者判**。实测它自报的 **1291 撞号**与本件独立实测的 GameObject 撞号**逐位吻合** |
| S2 | `_probe_deckinfo.py:object_cab()/cab_object()/fileid_note()`（:598-686） | **正确形态**：`object_cab` 先钉「引用者在哪份 CAB」，命中多份就**拒绝**（「钉不死」），再取 `externals[m_FileID-1]` |
| S3 | `menu_dump.py` 主语料（`BUNDLES = d:/2/新解包资源/assets_full`，:64） | 读**解包目录**（按资产类型分目录、按名字存文件）⇒ **根本不带 pid**，pid 空间不参与 |
| S4 | `menu_rect.py:Bundle`（:71 `self.rt[j.get('m_PathID', fn)]`） | 同 S3，读 `assets_full`；pid 来自**文件名**，是「本 bundle 内」的一棵树 |
| S5 | `_try_typetree.py:main()`（:31 `UnityPy.load(*files)`，:76 `for o in env.objects`） | 全包一个 env **但只遍历/打印、不按 pid 建表** ⇒ 无「pid → 名字」反查 |
| S6 | `read_itemdrawerconfig.py:read_config()`（:199-202） | **正确形态**：`sf = env.files[[k for k in env.files if k.endswith('sharedassets0.assets')][0]]` —— **显式点名**要哪份 |
| S7 | `read_default_cardbacks.py:read_defaults()`（:104-105） | 读 `sharedassets0.assets`（**单文件、不是 bundle**）⇒ 无多 CAB |
| S8 | `read_gradient2_level0.py:main()`（:83） | `list(env.files.values())[0]` 作用在 `level0`（单文件） |
| S9 | `find_asset_by_name.py:main()`（:43-46） | 走 `env.container`（**AssetBundle 容器名 / GUID**，不是 pid） |
| S10 | `gen_vfx_texture_mips.py:main()`（:46,72-74,84-86） | 键是**贴图名**、不是 pid；且**撞名时大写报警**「🔴 N 张同名贴图在不同包里 mip 层数不一致（**不静默挑一个**）」 ← **正确形态的正面例子** |
| S11 | `gen_module_material_sources.py:main()`（:78-112） | `cand.setdefault(nm, []).append((fname, is_extra, cont.get(o.path_id,'')))` —— 键是**名字**、且**记了包名** |
| S12 | `gen_arena_sprites.py` / `gen_arena_texslots.py` 的**局部** dict（`go_names/mats`） | 见 A3/A4 —— 那两个文件的**全局 `idx`** 会踩，但**局部**那份（:142 / :74）与 B 档同形（不发作） |
| S13 | `gen_arena_vertexcolors.py`（:365-399, :421-426） | **键是 `(fn, o.path_id)`**（`meshes[(fn, o.path_id)]`）—— **正确形态**（pid + 文件一起做键）；且全文只认 `m_FileID == 0`，跨文件**明说跳过**（:422） |
| S14 | `gen_env_blendables.py:main()`（:108-114） | `UnityPy.load(PREFAB_BUNDLE)` = `battleprefabs_vfxandmisc_assets_all.bundle`，**单 CAB** |
| S15 | `gen_offensive_cards.py:name_index()`（:253-278） | `src = bundle_name + "_assets_all.bundle"` ⇒ 只能寻址 `*_assets_all` 包，**全是单 CAB**；且它自述 `m_FileID` **恒 4 = 外部文件**（卡数据不在本地），不做 pid 反查依赖 |
| S16 | `import_original_sfx.py:load_bundles()`（:76-85） | 包来自 `BUNDLE_FALLBACK = ["soundcollection_assets_all", "battleprefabs_vfxandmisc_assets_all"]`，**两个都单 CAB** |
| S17 | `import_remnant_sfx.py:main()`（:62-70） | `BUNDLE = soundcollection_assets_all.bundle`，**单 CAB** |
| S18 | `gen_skybox_cubemap.py:main()`（:53-57） | `battlesharedresources_assets_all.bundle`，**单 CAB** |
| S19 | `slice_ui_atlas.py:main()`（:45,52-63） | 图集包 = `atlasindividual_assets_*` / `atlasgroup_assets_all`，**实测全单 CAB** |
| S20 | `_verify_prefab_bundle.py:load()`（:57-58）· `extract_mirror_shaders.py:load()`（:85） | 同一「取第一个 SerializedFile」形态，**但目标包单 CAB**：分别是 `battleprefabs_vfxandmisc_assets_all.bundle` 与我们自己的产物 `wf_arena_shaders.bundle` |
| S21 | `_missing_shaders_audit.py` / `survey_shader_originals.py` / `audit_effect_shaders.py` / `dump_shader.py` / `dump_shader_blob.py` / `gen_shader_blend.py` / `readone.py` | 目标全是我们自己或单 CAB 的 shader 包；且**只按类型/名字筛、不按 pid 建表** |
| S22 | `gen_animator_controllers.py` / `gen_profile_cosmetics.py` / `gen_campaign_rewards.py` / `gen_cardbacks.py` / `gen_shake_presets.py` / `gen_arena_camera.py` / `gen_arena_shadowflags.py` / `gen_arena_negscale.py` / `arena_particle_audit.py` / `arena_lc_diff.py` / `import_original_art.py` 等 | 读 `assets_full` 解包 JSON（按名字/`*_<pid>.json` 文件名），**不进真包的 pid 空间** |

---

## 我怎么找的

**搜过哪些词**（全部 `d:/4/Unity/工具/**/*.py`，177 个文件，逐行正则、跳过 `#` 注释行）：

```
第 1 轮（找「会不会碰 pid」）
  UnityPy\.load\(            ← 谁在读真包
  \benv\w*\.objects\b        ← 谁在遍历聚合对象
  list\(\w+\.files\.values\(\)\)\[0\]   ← 「随手拿第一份」
  \.files\.values\(\)|\.files\.items\(\)|\.files\b
  \[[^\]]*\bpath_id\b[^\]]*\]\s*=|\.setdefault\(str\(\w+\.path_id\)
  listdir|glob               ← 谁在扫全库
  externals | m_FileID | \bcabs?\b
  by_pid|byid|name_of|go_by_pid|pid2name|pid_name|gos\[
  def (build|main|collect|index|scan|load|read)\w*   ← 取「哪个符号名」

第 2 轮（找「没钉 CAB」的机械指纹 —— 这是本件最可复用的一招）
  \.objects\s*\[\s*0\s*\]                       ← env.objects[0]
  next\s*\(\s*v\s+for\s+v\s+in\s+\w*\.?files\.values\(\)\s*if\s+type\(v\)\.__name__
  files\.values\(\)\s*\)\s*\[\s*0\s*\]
  \.externals\b
  \.files\s*\[0\]|cabs\s*=\s*\{

第 3 轮（找「谁点名要 `scenes_*`」）
  scenes_scenes_ | mainmenuwarpforge | menus_assets_all | battleprefabs_ | assets_full
```

**扫过哪些目录**：
- `d:/4/Unity/工具/`（顶层 **148** 个 `.py`）
- `d:/4/Unity/工具/scripts快照/`（**13** 个 `.py` —— ⚠️ **A1 就藏在这里**，只扫顶层会漏掉）
- `d:/4/Unity/工具/arena_mat_audit/套1_pathID索引/`（**10** 个）+ `套2_逐场对账/`（**6** 个）—— **A6/A7 在这里**
- `d:/4/Unity/工具/arena_mat_audit/套…/` 下还有 `arena_mat_audit/`、`data/` 等非 `.py` 目录，已随 `os.walk` 覆盖
- ❌ `d:/4/工具/` —— **不存在**（`ls` 报 `No such file or directory`；`d:/4/` 只有 `CLAUDE.md` `README.md` `Unity/` `_blender_work/` `_tmp_view/` `项目任务.md`）
- ⚠️ **没扫**（不在白名单内、且不属「工具」）：`d:/2/Warpforge_tools/**`、`d:/warpforge/**`、`d:/4/Unity/MyGame/Assets/**/*.cs`

**本件新跑的只读实测**（脚本走 stdin heredoc，**不落盘、不 import 任何工具模块**，只 `UnityPy.load`）：

| 量什么 | 命令要点 |
|---|---|
| 双 CAB 包清单 + 工具点名包的单/多 CAB | 遍历 84 个 `*.bundle`，取 `bf.files` 去掉 `.resS`/`.resource`，数内层 CAB |
| 同类型 / 异类型撞号 | 取每包 CAB 里对象最多的当主 CAB、最少的当 `.sharedAssets`，逐 pid 比 `o.type.name` 与 `peek_name()` |
| 「随手第一份 CAB」指错多少条外部引用 | 对每个包：`ex_first = bf.files[第一个].externals` vs `ex_own = 该对象所在 CAB 的 externals`；递归收每个对象 typetree 里的 `{m_FileID, m_PathID}`，比 `ex_first[fid-1] != ex_own[fid-1]` |
| 跨包同 pid 不同名 | 全 84 包按 `(类型, pid)` 收 `{(名字, 包, CAB)}`，按类型统计「名字集合大小 > 1」 |
| Shader 撞号 | ⚠️ 必须 `read_typetree()` 取 `m_ParsedForm.m_Name` —— **`peek_name()` 对 Shader 恒失败**（顶层 `m_Name` 是空的），会得到「0 条撞号」的假结论 |
| `env.objects[0].assets_file` 是谁 | `bf.files` 顺序 + `id(sf)` 反查 CAB 名 |

⚠️ **扫描期间有别的代理正在改 `.py`**（铁律 13·3 那条「源码互相可见」）——
本件扫描窗口内被改过的是（按 mtime）：`menu_rect.py` 10:25 · `gen_env_blendables.py` 11:35 ·
`menu_dump.py` 12:38 · `_probe_deckinfo.py` 12:44 · `collect_unmatched_art.py` 13:16 ·
`import_original_art.py` **13:19**（本件报告写于 13:20）。
⇒ 这 5 个在这份报告里的结论**按我读到的那一版**为准；其中 `collect_unmatched_art.py` /
`import_original_art.py` / `gen_env_blendables.py` / `menu_rect.py` 本件的判定都是
「读 `assets_full` 解包目录、按名字定位 ⇒ **不进真包 pid 空间**」，**改动不会推翻这个判据**；
`menu_dump.py` 与 `_probe_deckinfo.py` 的判定落在 **B8 / S1 / S2**，**A127 那轮的核心结构（`amb` 表、
`object_cab` 先钉 CAB）没变**，所以结论不受影响。⛔ 但**若要动手修，先 `git status` 看那两处的最新版**。

---

## 没查清的部分

1. **A1（`unity_scene_to_godot.py`）到底有没有真的产出过坏资产 —— 没查清。**
   已知「40.4% 的外部引用会指错包」是**静态算的**；但 `read_obj` 会先查 `self.local`（本地命中就返回），
   真正走到 `self.ext` 的只有「本地查不到」的那批。**还差**：跑一次 `--arena <场>` 并比「解出来的
   shader/贴图名 vs 主 CAB 的 externals 算出来的名」，数出**实际进产物的**错误条数。
   ⚠️ 本件**不动手跑它**（它会写 `d:/2/战场演示` 与 `DATA_DIR`，属「会写盘」）。
2. **`scripts快照/` 是不是「活」的 —— 没查清。** 它是快照目录，但 `sync_from_d2.py:123-124`
   把 `unity_scene_to_godot.py` / `gltf_export.py` 列进了同步名单 ⇒ 说明它**有真用途**。
   `README.md:30` 也写着 `gen_unity_arena_manifest.py` **依赖** `unity_scene_to_godot.py`。
   **没查**：`d:/2/` 那边是否另有一份**同源**的 `unity_scene_to_godot.py`、那一份 :328 是不是同形
   （⛔ 本件白名单只到 `d:/4/Unity/工具/`，没越界查）。
3. **A2 的实际影响面没查全。** 只量了「材质 → shader」这一路（1/770）。
   `_dump_shaders_batch.py` 还用 `externals[f]` 解**别的** fileID（它收了 `mats` 三元组），
   **没查**它最终把哪些字段当结论印出来。
4. **`gen_anim_address_map.py` 与 `gen_card_vfx_by_card.py` 的「wanted pid」集合没取全。**
   只能证明「这两个工具会把 `scenes_*` 扫进去、且包内 `byid` 是聚合的」，
   **没有**把它俩真正要查的 pid 列表拉出来、逐条判「这批 pid 里有没有落在双 CAB 撞号集合上的」。
   还差：把 `want[d]` 那个集合 dump 出来比一次。
5. **`gen_offensive_cards.py:name_index()` 的 `bundle_name` 取值没逐个核。**
   判据是「`src = bundle_name + "_assets_all.bundle"` ⇒ 只能寻址 `*_assets_all` 包，而它们全是单 CAB」——
   这条**依赖「`*_assets_all` 全是单 CAB」**，本件只逐个数了 15 个多 CAB 包的**名字**
   （全是 `scenes_scenes_*`，没有 `*_assets_all`）⇒ 结论成立，但**没有**把两个 `name_index` 调用点
   （:361 的 `bn`、:373 的 `PB`）的实际字符串打出来。
6. **`_probe_deckinfo.pid_name_map()` 有一档「撞不出来」的盲区，本件没量。**
   它按 `(类型, 名字, 包)` 去重 —— 双 CAB 内**同类型同名字**的两件会被当成**一件**
   （实测 84 包里有 **1 条** `Sprite` 属于这种：同 pid 同名、来自两份不同 CAB）。
   对「查名字」这个用途无害，但**若将来有人拿它当「对象身份」用就会错**。**没查**它的消费端有没有这种用法。

---

## 顺手发现（⛔ 别自己改，交调度台分流）

1. 🔴 **「`m_FileID` 不可信 ⇒ 所以按 pathID 解」这个推理是错的，它写在三个文件里**
   （`build_mat_index.py:4` · `dump_orig.py:4` · `gen_arena_texslots.py:212`）。
   本件实测 **pathID 也不是全库唯一**（GameObject 1291 / Mesh 82 / Material 18 / Texture2D 11 / Shader 6 条撞名）。
   ⇒ 「fileID 不可信」与「pid 可以当键」是**两个独立问题**，前者不蕴含后者。
   这条措辞会**误导下一个会话**照抄，建议进坑表。
2. 🔴 **`gen_card_vfx_by_card.py:243` 的注释「实测 path_id 唯一」是假结论**（实测见上表）。
   该工具**今天不踩**（包单 CAB），**但那条注释会让人在别处照抄**。建议就地改掉（铁律 5）。
3. 🔴 **`dump_animfx.py` 的 `go_by_pid`（:262-265）是死代码** —— 建了**从没被读**。
   它恰好是全仓**唯一**一处「按 pid 建全库 GameObject 索引」，而 GameObject 是全库撞号最多的类型（1291）。
   ⇒ 今天**无害**（没人读），但它是**上了膛的枪**：下一个人接上去用就会静默取错。
   建议删掉或改键，并在原处留一句「别按 pid 全局查」。
4. **`_ext_name(sf, fid)`（`extract_missing_shaders.py:565`）会把宿主包说错**（用于报错文案）。
   症状形状：**报错信息指的包是错的** ⇒ 排查时会被带到别处。属「静默失败」的近亲，建议一并修。
5. **`bundle_dump_objects.py` 对 `scenes_*` 包会两份 CAB 混着印、没有 CAB 列。**
   它不是 bug（无 pid 反查），但**是个误导源**：读的人会以为 `pid=1` 只对应一件。
   建议加一列 `o.assets_file.name`。
6. **`_probe_prefab_deps.py:411` 把 `bf.files.keys()` 印出来**（`内部文件 = ...`）——
   **这是本仓已有的好习惯**（把「一个包有几份 CAB」说清楚），建议同类工具照做。
7. **`gen_vfx_texture_mips.py:84-86` 是「撞号就大声报、不静默挑一个」的正面例子**，
   与本件要修的那 7 处正好是同一个判据 ⇒ 修的时候**照它抄**（比照 `_probe_deckinfo.pid_name_map()` 更轻）。
