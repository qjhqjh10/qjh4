# W230 · 跨 CAB 容器引用 + repack 口径

日期：2026-10-13 · 执行写手（全权限，**没跑 Unity**）· 工程 `d:/4/Unity`
账：**A230**（跨 CAB 容器引用，`m_FileID ≠ 0`）+ **A231②**（`repack()` 与 `repack_tree()` 两套口径）
白名单内只动了 **一处代码**：`工具/extract_missing_shaders.py`（+ 本报告 + `d:/2/tools/w230_crosscab/` 下的**新**目录）。
判据出处：`资料/普查产出_1013/A表现核_块5.md` §A230 / §A231 · `资料/待办判据_1008.md:34-35`（**两条的原文都在这一行里**，
A230 判据全文在 `资料/普查产出_1008/波B1_工具三件.md` §四·1 / §1.5）· 已修的 `repack_tree()` · UnityPy `PPtr.deref`。

---

## 一、结论（做成了什么 / 没做成什么）

| 件 | 状态 | 一句话 |
|---|---|---|
| **A230 跨 CAB 容器引用** | ✅ **做成了**（机制已在真数据上跑通 + 独立回读验收） | 新增 `_ext_fid()` / `_archive_path()`：目标对象在**别的内层 CAB** 时，容器项/预加载项写 `PPtr(m_FileID = sf.externals 下标 + 1, PathID)`；`sf.externals` 里**没有**指得到那一份的条目时**补一条**（照原版写法）并出声。`repack_tree()` 的 ①·d 从「**中止点名**」改成「**跨 CAB 支持**」。真例 = arena1 的 `Embers (2)`（ss 包主 CAB 的根）⇒ 产物容器项 `m_FileID=22 / m_PathID=1`，回读解出=**GameObject `Embers (2)`**（改前版本在同一输入上**中止**） |
| **A231② `repack()` 对齐** | ✅ **做成了**（口径对齐 + 改前/改后实跑对照） | `repack()` 现在：⓪ 写 **AB 对象所在的那一份**（不再「取第一份」）·① **shader 从所有 CAB 收**（原来只收第一份 ⇒ 别的 CAB 的 shader **静默漏掉**）·② 别份的 shader 走 A230 的跨 CAB 引用 + 那份内层文件留下 ·③ 只裁「会留下的那几份」。**合成真例**（真包 arena2 + 搬 1 个 shader 到兄弟 CAB）：改前 **3 个**、改后 **4 个**，第 4 个靠 `m_FileID=22` 取到 |
| ⚠️ **A230② / A231③（「多 CAB 产物在 Unity 里真加载过」）** | ❌ **没做**（本件跑不了 Unity，⛔ 不许当「已验收」） | 见 §六·1。**这两条账不能销**（与 `块5.md` 的「不能销」一致） |

**「不许静默失败」这条的落点**（本件的验收标准 2）：解不出 / 补不上时 `_ext_fid` 返回 `(None, 原因)`，
调用方**点名中止**（`repack_tree`）或**逐条列出缺席的 shader**（`repack`）—— 两种情形都有实跑输出（§二·3）。

---

## 二、真例（包名 → 资产名 → 解出来是什么；命令 + 输出）

复算脚本全在 **`d:/2/tools/w230_crosscab/`**（新目录，⛔ 没覆盖任何既有文件；`out/` 里是产物）。
命令模板（`PY = "D:/2/Warpforge_tools/py312/python.exe"`）：

```bash
$PY d:/2/tools/w230_crosscab/run_crosscab.py <工具路径> <产物路径> "Embers (2)"
$PY d:/2/tools/w230_crosscab/verify_crosscab.py <产物路径>
```

### 1. **A230 真例**：`scenes_scenes_battlearena1.bundle` 的根 `Embers (2)`

**包名/资产名/解出来是什么**（一条链读到底）：

| 环节 | 值（实测） |
|---|---|
| 源包 | `scenes_scenes_battlearena1.bundle`（内层 4 个文件：2 份 CAB + 2 条流） |
| **要写的那一份**（装着 `AssetBundle` 对象） | `CAB-8adfc300739b4da5111d4bd3eae80365.sharedAssets`（63 个对象，AB 对象 pid=2） |
| **根落在哪一份** | `CAB-8adfc300739b4da5111d4bd3eae80365`（主 CAB，5304 个对象）→ GameObject `Embers (2)`、**PathID 1** |
| 补的 externals 条目 | `externals[21]` = `archive:/CAB-8adfc300739b4da5111d4bd3eae80365/CAB-8adfc300739b4da5111d4bd3eae80365`（`m_FileID=22`） |
| 产物容器项 | `'Embers (2)'` → `m_FileID=22 m_PathID=1`、`'assets/embers (2).prefab'` → 同一条 |
| 产物预加载项 | `[0] m_FileID=22 m_PathID=1` |
| **回读解出**（UnityPy `deref()`） | **GameObject `Embers (2)` @`CAB-w230crosscab_1`**（= 留下的那一份、已整组改名） |
| 产物 | **10.9 MB**（`CAB-w230crosscab` + `CAB-w230crosscab_1` + `CAB-w230crosscab_1.resource`） |

改后（本件）关键输出：

```
[P1] 🔴 **1 个根在【别的 CAB】里** ⇒ 容器/预加载项走**跨 CAB 引用**（`m_FileID ≠ 0`，A230 那条路 —— ⛔ 不是「按 pid 在所有 CAB 里找」）：
       要写的那一份（AssetBundle 对象所在）= `CAB-8adfc300739b4da5111d4bd3eae80365.sharedAssets`
       根 `Embers (2)`(PathID 1) 在 = `CAB-8adfc300739b4da5111d4bd3eae80365` ⇒ 容器项 `m_FileID=22`（🔴 **新补** `externals[21]` = `archive:/…（原版没有这条；写法照 `_archive_path`，字段样式照本文件既有 `archive:/` 条目）`）
       ⇒ 那一份 CAB **整组保留进产物**：[…] （两份 CAB 有 **63 个 pid 撞号** ⇒ 所以非带 `m_FileID` 不可）
[7] m_Container 重写为 2 条，m_PreloadTable 补齐 1 条
[9] 已写出 …/final_arena1.bundle  (11163 KB)
```

**改前（HEAD 版）同一条命令**（负对照 ⇒ 证明这条账原来真的做不到）：

```
=== 工具 = /tmp/…/before_extract.py     ← `git show HEAD:Unity/工具/extract_missing_shaders.py`
[P1] 🔴 **根不在要写的那一份 CAB 里** ⇒ 中止（⛔ 不退而求其次）：
       根却落在 = `CAB-8adfc300739b4da5111d4bd3eae80365`：Embers (2)(PathID 1)
       要收这一份里的根 = 得先定「跨 CAB 容器引用」怎么登记（`m_FileID` ≠ 0 那条路**未验过**，⛔ 别猜）
=== 返回值: None
```

**为什么「按 pid 在所有 CAB 里找」必错**（源包里直接量，§A230 的判据①）：

```
源包里同一 pid=1 在两份 CAB 里各自是什么：
  `.sharedAssets`（AB 那一份）: PreloadData ``
  主 CAB（根所在）: GameObject `Embers (2)`
`.sharedAssets` 的 externals 里有没有指向【本包主 CAB】的条目： 没有 ⇒ 想指进去只能【补】一条
主 CAB 的 externals 里指向【本包 .sharedAssets】的条目： ['archive:/CAB-8adfc3…/CAB-8adfc3….sharedAssets']
```
⇒ ① 裸 pid 会静默指到 `PreloadData`；② 反向那条条目**原版就有**（同包兄弟 CAB 的 `archive:/` 写法**有先例**，
但方向是 主 CAB → `.sharedAssets`，我们要的方向本地**没有现成条目** ⇒ 只能补）。

### 2. **A231② 真例**（合成输入）：shader 住在**别的 CAB** 里

⚠️ **如实说明**：全库 84 个包里**没有一个**「shader 住在 AB 那一份之外」的真例（13 个 `scenes_*` 战场包的 shader
**全在 `.sharedAssets`**，即 AB 那一份 —— 所以**改前这个缺陷今天触发不到**，与 `块5.md` 的「今天触发不到」一致）。
为了让新代码路径**真的跑一次**，我用真包做了**最小合成**：把 arena2 的 `Everguild/FX/Simple Fake Water`
从 `.sharedAssets` **原样搬**到主 CAB（对象字节不动，只改 pid / `type_id` / `serialized_type`）。
脚本 = `d:/2/tools/w230_crosscab/make_synth.py`（⛔ 不是新格式，只是换了个内层文件）。

| | 改前（HEAD 版） | 改后（本件） |
|---|---|---|
| `repack()` 收几个 shader | **3 个**（`Simple Fake Water` **静默不见**，一个字不报） | **4 个** |
| 产物 | **54 KB**（只有 `.sharedAssets` 一份） | **4.9 MB**（多留下主 CAB 那份，只留它的 shader） |
| 容器项 | 3 条（全 `m_FileID=0`） | 4 条，其中 `'Everguild/FX/Simple Fake Water'` → **`m_FileID=22 pid=5521`** |
| 回读 `deref()` | —— | **Shader `Everguild/FX/Simple Fake Water` @`CAB-a5741cd1d9cfa42275c94e631a043bdb`** ✅ |

改后输出（节选）：
```
[6] 按 CAB 收 shader：`CAB-a5741cd1d9cfa42275c94e631a043bdb` 1 个 · `CAB-a5741cd1d9cfa42275c94e631a043bdb.sharedAssets` 3 个
[6] ⓘ **跨 CAB 引用**：`Everguild/FX/Simple Fake Water`(PathID 5521) @`CAB-a5741cd1d9cfa42275c94e631a043bdb` ⇒ 容器/预加载项 `m_FileID=22`（🔴 **新补** `externals[21]` = `archive:/CAB-a5741cd1d9cfa42275c94e631a043bdb/CAB-a5741cd1d9cfa42275c94e631a043bdb`）
[6] ⓘ 体积：留下的别组内层文件 […] 未压缩合计 4.7 MB（⛔ 今天**没有真例**走到这一支 …⚖️ 口径**还没定**，见 §六）
--- 产物容器 4 条 / deref 失败 0 / 4
```

### 3. **「解不出来必须出声」** 的三条路径（前两条有代码级证据、第三条今天不触发）

* `repack_tree`：补不出 externals 时（`_ext_fid` 返回 `None`）⇒ `[P1] 🔴 **有根在别的 CAB 里、而跨 CAB 容器引用登记不出来** ⇒ 中止` +
  逐根点名 + 写明「为什么不许将就」。（代码路径 `工具/extract_missing_shaders.py:1043-1051`；本次**没在真数据上触发**，
  因为补条目这条路成功了 —— 如实标注：**这一支只有代码级证据**。）
* `repack`：登记不进去时 ⇒ `[6] 🔴 **N 个 shader 登记不进容器**（这份包里的它们会缺席）—— ⛔ 不打猜的引用：` + 逐条列出。
  （同样**未在真数据上触发**。）
* `repack`：**同名 shader 落在两份 CAB** 时 ⇒ `[6] 🔴 同名 shader `N` 登记了 2 条容器项 …`LoadAsset('N')` 取到哪一条**由 Unity 定**（本脚本不挑）`
  —— 今天不触发（真数据里没有跨 CAB 的同名 shader），加了是为了**不静默**。

---

## 三、改动清单（`文件:行` = **改后**行号 + 一句话）

全部在 `工具/extract_missing_shaders.py`（行尾：**原本就是 LF**，改完仍是 LF —— `CRLF 0 / LF 1524`（94679 字节）；`git diff --numstat` = **295 加 / 62 删**）。

| 行 | 改了什么 | 为什么 |
|---|---|---|
| `66-73`（文件头） | 「多 CAB 三条口径」那句从「根不在那一份就中止点名」改成「**A230 起走跨 CAB 容器引用**」+ 补一句 `repack()` 已对齐 | 铁律 5：就地改掉已过期的记录 |
| `367-491` **`repack()`** | 整段重写：⓪ 挑 AB 那一份 ·① 全 CAB 收 shader（先只读）·② `_ext_fid` 登记 + 别份留组 ·③ 只裁会留下的那几份 | A231②（见 §四） |
| `494-527` **`_write_bundle()`** | `entries` 键从裸 `path_id` 改成 **`(m_FileID, path_id)`**；两张表都按 `m_FileID` 写；把 `preloadIndex` 那段实测注释搬进来 | A230：裸 pid 在两份 CAB 之间撞号（arena1 63 个） |
| `751-768` 新 **`_archive_path()`** | 内层名 → `archive:/<基名>/<内层名>`（三种原版写法逐字对上） | 补 externals 时**不猜**写法 |
| `770-823` 新 **`_ext_fid()`** | 目标 CAB → `m_FileID`；找不到就**补一条**（照本文件既有 `archive:/` 条目的 guid/type/temp_empty，追加在末尾、不动既有下标）；补不了返回 `(None, 原因)` | A230 本体 |
| `922-955`（`repack_tree` ①） | 单遍扫根时顺手记「每个根**在哪一份** CAB」+ 同名根跨 CAB 时**出声**（原来只打印名字） | ①·d 要用 CAB 名；同名根原来**静默取第一个** |
| `1000-1051`（①·d） | 「根不在要写的那一份 ⇒ **中止**」→「**跨 CAB 支持**」：`_ext_fid` 定 `m_FileID`、那份 CAB 整组留下；**补不出来才中止**（点名 + 说清为什么不能将就） | A230 |
| `1067`（② 队列起点） | `queue = [(sf, pid) …]` → **`(根自己那份, pid)`** | 🔴 **顺手抓到的一个真 bug**：根在别份时，起点用 `sf` 会 `sf.objects.get(pid)` 取到**同 pid 的另一个对象**（arena1 pid=1 = `PreloadData`）且**一个字不报** —— 正是 A173 的静默形态；改前根只可能在 `sf` 里，所以看不出来 |
| `1111`（③） | `keep_files` 初值从 `set()` 改成 **`set(cross_files)`** | 跨 CAB 根所在的那几组内层文件**必须留**（跟「依赖树引用到的别份」走同一条路：③·a 整组补齐 + 整组改名） |
| `1359-1384`（⑤） | 容器登记键改 `(fid, path_id)`（`fid` 来自 `cross_roots`，同份 = 0） | A230 |
| `876-887`（`repack_tree` 函数头） | 口径 3 改写 + 写明证据级别与「Container 侧本地无先例」 | 铁律 5 |

---

## 四、`repack()` vs `repack_tree()` 口径对照

| 口径 | `repack_tree()`（A173 起） | `repack()`（本件 A231② 起） | 依据 |
|---|---|---|---|
| **写哪一份 CAB** | 装着 `AssetBundle` 对象的那一份 | **同左**（原来「取第一份」，全库 15/15 碰巧对，但是运气不是规则） | 容器/预加载是 `PPtr(m_FileID=0)` = 引用方自己那一份（UnityPy `PPtr.deref`） |
| **AB 对象 0 个 / ≥2 个** | 出声中止 | **同左**（0 个那句文案一字未改 ⇒ `run_arenas` 的既有输出不变） | 只重写一个容器、另一个会留旧容器（静默坏包） |
| **跨 CAB（根/对象在别份）** | **走 `m_FileID ≠ 0`**（①·d）+ 那份整组留 | **同左**（② 段） | A230（本件） |
| **别份里「留下的对象」怎么裁** | **整份不裁剪**（依赖树可能引用它里面任何对象，裁了就断引用） | **只留 shader**（本函数的容器项**只指 Shader**，而 shader 的字节码 `compressedBlob` 内联、不依赖别的对象） | ⚠️ **这是唯一有意不同的一处**；依据 = 本文件头 ⚠️2 的既有判据（`repack` 因此才敢丢流） |
| **内层文件改名** | **整组改名**（否则与仍加载着的源包撞名 ⇒ 整包拒收） | **不改名**（沿用源包 CAB 名） | 三个 `repack` 模式的产物是**运行时**加载的、进程里没有源包 ⇒ 撞不上（原文在两个函数里都有） |
| **资源流** | 依赖树用到的流保留 + 瘦身 | 一律丢（shader 内联） | 既有判据 |
| **`m_StreamData` / `externals[].path` 重写** | 改名后必须重写 | 不改名 ⇒ 不需要（跨 CAB shader 那一支**新补的 externals 条目**会跟着③·b·1 一起改名，已实测） | `_rename_stream_path` |
| **返回值** | `{(CAB 名, PathID): 对象}` | `{(CAB 名, PathID): shader 名}`（原来 `{pid: 名}`） | 裸 pid 会撞号；调用方只用 `len()` / `.values()` ⇒ 两处都已核对 |

---

## 五、实跑验证（纯 python，⛔ 没跑 Unity / ⛔ 没动 git 的写操作）

| 目的 | 命令 | 结果 |
|---|---|---|
| 编译 | `$PY -m py_compile 工具/extract_missing_shaders.py` | ✅ OK（每轮改动后都跑） |
| **单 CAB 回归（stdout 逐字节）** | `--prefabs --check`（改前 vs 最终）`diff` | ✅ **逐字节相同**（110 行） |
| **产物回归（md5）** | `regress_repack.py`：5 个真源包各跑一遍 `repack()` | ✅ **5/5 与改前逐字节相同**：`wf_builtin` 91472B/`1b3ce543a2ba` · `wf_arena_shaders` 143037B/`ce664898092b` · `wf_arena_2` 63830B/`ddf58ceff4cb` · `wf_arena_tauviorla` 78068B/`049fed705a84` · `wf_shaders_extra` 699808B/`645fe8370277`（其中**前 4 个**还与仓库在用的产物**逐字节相同**） |
| 其余模式不炸 | `--builtin --check` · `--arenas --check` · 默认 `--check` | ✅ 三条都 `exit 0` |
| **A230 真例** | `run_crosscab.py <工具> <产物> "Embers (2)"` | ✅ 打出 10.9 MB 产物；`verify_crosscab.py` 回读：容器 2 条全 `m_FileID=22`、`deref()` = GameObject `Embers (2)`、**deref 失败 0** |
| A230 负对照（改前） | 同命令 + HEAD 版工具 | ✅ 如预期**中止**（`=== 返回值: None`） |
| **A231② 真例**（合成） | `make_synth.py` → `run_repack_one.py`（改前 / 改后） | ✅ 改前 3 个 shader（少 1 个、静默）· 改后 4 个 + 跨 CAB 容器项解得出 |
| 跨 CAB 引用解不解得开 | `verify_crosscab.py`（独立回读，不看写侧代码） | ✅ `pid=1 m_FileID=22 → GameObject 'Embers (2)'`；同 pid 用 `m_FileID=0` 在产物里解不出（那份已被裁到只剩 AB 对象） |

> ⚠️ **判绿红的口径**：以上全部是 **UnityPy（库级/格式级）** 证据 —— 它**和 Unity 同规则但不是 Unity 本身**（§六·1）。

---

## 六、没查清 / 没做的（如实写，⛔ 不许猜）

1. 🔴 **Unity 侧一次都没加载过**（本件跑不了 Unity）⇒ **A230② / A231③ 两条账不能销**：
   - 「`m_Container` 里写 `m_FileID ≠ 0` Unity 认不认」= **未验**。最省事的验法：把 `out/final_arena1.bundle`
     丢进一个最小 Unity 探针包，`AssetBundle.LoadFromFile` → `GetAllAssetNames()` 是否含 `Embers (2)`、
     `LoadAsset<GameObject>("Embers (2)")` 是否非 null。（这条也顺带覆盖「多 CAB 产物能否加载」= A231③。）
2. ⚠️ **`m_Container` 侧本地【没有先例】**：全库 84 个包 **13448 条容器项，`m_FileID` 全是 0**（2026-10-13 普查）。
   有先例的是**另一张表**：`m_PreloadTable` 里 `m_FileID ≠ 0` 遍布全库（84 个包中 **30+ 个**有；
   `menus_assets_all` 一家 3.5 万条、fid 最大 29；`shaders_assets_all` 4 条）—— **但那些指的都是别的包
   （跨 bundle），不是同包兄弟 CAB**。⇒ 我写的是**两条都写**（容器 + 预加载），容器那条是**新形态**。
3. ⚠️ **补出来的 externals 条目 = 本地没有逐字相同的先例**：写法（`archive:/<基名>/<内层名>`）对上了**三处**原版
   写法（兄弟 CAB 条目 / 别的包的 CAB 条目 / 资源流条目），**方向**（`.sharedAssets` → 主 CAB）本地没有现成条目
   （只有反方向）。⇒ 「补条目」这件事本身**只有格式级 + 同文件内其余条目样式**作依据。
4. ⚖️ **体积口径（A230 判据里点名要一起定的那条）**：现在按 A173 口径 2 **整组留**（不裁剪）⇒
   真例产物 **10.9 MB**（原来这个包根本打不出来）；合成例里 `repack` 那支是 **+4.7 MB 未压缩**（`.resource` 4.7 MB）。
   「要不要把别组也裁 / 连它的流一起留」**没定** —— 需要真实需求（现在 0 个真例）或调度台裁。**已写进代码输出**（`[6] ⓘ 体积：…`）。
5. ⚠️ **`repack()` 产物不改内层名**这件事我**没有改**（沿用既有行为）—— 理由写在代码里：那三个模式的产物是运行时加载的、
   进程里没有源包。**若将来有源包与产物同时加载**，这条会变成「整包被拒收」的雷（`repack_tree` 那段实测就是这个症状）。
6. ⚠️ **`_ext_fid` 的「补条目」在 `--check` 下也会改内存里的 `externals`**（不落盘、不写文件）—— 有意为之（体检要能看到真实产物会长什么样）。
7. **没做的**：`repack_tree` 里「根在别份 + 该份**也**有流对象」的组合**没造过合成例**（arena1 实测别份 0 个走流对象）。
8. `工具/_verify_prefab_bundle.py`（A231①）**我没碰**（另一个写手）；它的期望值只覆盖**单 CAB 产物**，
   `repack_tree` 一旦打出多 CAB 产物它就只查第一份 —— **那正是 A231① 要修的**，本件没越界。

---

## 七、顺手发现（⛔ 只报不改）

1. 🔴 **仓库里的 `wf_shaders_extra.bundle` 是过期的**：仓库副本 **1338640 B / md5 `ecebd2f811a8`（2026-09-11）**，
   而**改前和改后**的工具跑 `battleprefabs_vfxandmisc_assets_all.bundle` 打出来的都是 **699808 B / md5 `645fe8370277`**
   ⇒ **与本次改动无关**（改前=改后），是**旧版本工具留下的产物**。要不要重打 / 有没有消费方依赖那 1.3 MB 版，请调度台定。
2. 🔴 **同族第 3 处「取第一份 `SerializedFile`」**：`工具/extract_mirror_shaders.py:84`（`_serialized_file()`）
   + `:112` 的 `next(o for o in sf.objects.values() if o.type.name == "AssetBundle")` —— **没有 default** ⇒
   壳包一旦变成多 CAB 且 AB 对象不在第一份，会抛 **`StopIteration`**（而不是本项目要的「出声点名」）。
   今天安全（壳 = `wf_arena_shaders.bundle`，单 CAB，且壳里的东西全丢）。**本件没动**（不在白名单）。
3. 🔴 **`repack_tree` ①段原来会静默取同名根的第一个** —— 本件顺手补了出声（§三 `922-955`）。
   顺带一条给 A435（迁移表）用的普查事实：**同名 GameObject 跨两份 CAB** 只在**同包**里有意义，
   本件遇到的所有包里 arena1 的 `Embers`/`Embers (2)` 四件**全在主 CAB**（同名不同 pid，不跨份）。
4. **给「以后从 `scenes_*` 抽东西」的一组数字**（本件复核，与 `波B1` 一致）：15 个多 CAB 包**全是 `scenes_scenes_*`**；
   **AB 对象 15/15 都在 `.sharedAssets`**；**13 个战场包的 Shader 100% 在 AB 那一份**；
   **`.sharedAssets` 的 externals 里 0 条指向本包主 CAB**（⇒ 跨 CAB 容器引用**一定**要补条目，不是「有时」）。
5. **UnityPy 搬对象要改三处**（做合成例时踩到，值得记）：`path_id`（`ObjectReader.write` 写的是 `self.path_id`，
   不是 dict 的键）· `type_id`（= **那个文件 `types` 表的下标**，直接搬会指到另一个类型上 —— 实测搬过去变成了 `AudioSource`）·
   `serialized_type`。少一处**不报错**，只是对象变了样。
6. `run_prefabs()` 的 `BOOSTER_GROUPS[0]["roots"]` **结构一字未动**（主对话随后要往里加 `RewardAppearParticle` 所在的那一行）；
   本件的改动**不需要**那份列表配合（新逻辑对单 CAB 包 = 零影响）。
