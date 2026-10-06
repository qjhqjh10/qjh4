# Block9 · 工具族七条（A523 · A561 · A620 · A621 · A625 · A626 · A801）

> 写手代理报告 · **2026-10-14** · 独占文件 = `工具/{extract_missing_shaders,menu_rect,menu_dump,menu_dump_w1diff,_probe_deckinfo}.py`
> 判据 = `资料/普查产出_1013/批次计划_1013.md` **§六·B** 那七行 + `W230_跨CAB与repack.md` + `W499_工具口径.md`
> ⛔ **一行 `.cs` 没动 · 一次 Unity 没跑 · `d:/2/` 只读 · 仓库里的 bundle 产物一个字节没写**（本文所有
> `repack()` 实跑都写在 `%TEMP%`）。
> 行号 = **改后现读**（改完 `git diff --numstat` 已逐文件核过，**没有一个文件被翻行尾**：5 个文件 CRLF 全 0）。

---

## 一、逐条

| A 编号 | 改了什么（文件:行） | 判据（§六·B 怎么说） | 做完没有 |
|---|---|---|---|
| **A523** | `extract_missing_shaders.py:109-127` 文件头新增**「重打包口归属表」**（模式 → 唯一口 → 产物 → 内层改名 → 别份怎么裁）＋三条钉死的规矩（**唯一写盘口 = `_write_bundle`，全库恰好 2 处调用** · **口径冲突时以 `repack_tree()` 为准** · 别份裁剪/内层改名**两处不同都不是欠账**）；`repack()` `:393` · `_write_bundle()` `:555` · `repack_tree()` `:921` 三处各加一行**指回那张表**（⛔ 不再抄第二份） | §六·B：「两个重打包口并存本身是债 ⇒ 收口时要定「**谁是唯一口**」（把另一个**收编/标死**）」 | ✅ **做完**（= 标死；**没有**把两个函数合成一个 —— 见「三、没做完的」第 3 条） |
| **A561** | ⛔ **一个字节没改**（`工具/extract_mirror_shaders.py` **不在白名单**）—— 只把证据核清楚，见 §二·5 | §六·B：「同族第 3 处「取第一份 `SerializedFile`」…**今天安全**（壳单 CAB）、**不在当时白名单，没动**」 | ⛔ **只核不改**（证据齐 ⇒ 见第 3 节「待办」） |
| **A620** | `menu_rect.py`：新增 `MAXDEPTH_TOPS:541` · `new_stats():544` · `count_subtree():560` · `stats_warning():582`；`walk` 加 `stats` 形参（**加在末尾**）`:618`；两处静默 return 改成**记账+表尾出声**（缺件 `:645`／深度截断 `~:655`，且**两处判定次序与 `menu_dump.walk` 对齐**）；`main()` 建账 `:830` · 表模式表尾 `:965` · `--cs` 模式走 **stderr** `:874`；`menu_dump._count_subtree:2031-2042` **改成转发 `MR.count_subtree`**（一条算法只留一份） | §六·B：「`walk` **两处静默没改**：`depth` 截断与 `rt is None`（缺件不报）—— `menu_dump` 那两处有 `stats` + 表尾出声，**`menu_rect` 一个字不说**」（硬纪律：**不许弱化** ⇒ 一律改成**出声**，⛔ 没有改「悄悄用默认值」） | ✅ **做完**（自检 5/5，见 §二·2；数据行**逐字节未变**，见 §二·1） |
| **A621** | `menu_dump.py`：① 表尾 `cut_tops` 取名字 `:2060-2065` 改走 **`go_name_of_rt`**（原来 `b.go_name(gopid)`）② `:2080-2083` 删掉一个**死局部 `gopid`**（A499 之后没有读者）③ `verify_layout` ⑲ 三处：`_kid19:3067` · `_go19:3077` · `_rectkids19:3089` 全改 **按这颗 RT 认 GO** | §六·B：「**其它按 pid 认 GO 的地方没全改**：只改了 `walk` 与 `main()` 的根解析；**`_count_subtree`（约 `:2031`）等次级取名字处仍走 `b.go_name(gopid)`** ⇒ 撞车包下【统计数字】仍可能取错件」 | ✅ **做完**（差面**量化过**：撞车包里 **49/49** 颗 RT 的名字在两条路下**不一样**，见 §二·4；`--verify-layout` **逐字节全绿**，见 §二·3） |
| **A625** | `menu_rect.py:712-729`：把那一支 `elif` **拆成两支** —— `m_ShowMaskGraphic` 那支照旧印 `Mask`；**脆 clause 那支单独印 `Mask(⚠️A625脆clause)`**（= 它真单独命中时**表里看得出来**），并把**全库实测**与「不许合并 / 不许删」的理由写在原地；图例行 `:888` 补这个记号 | §六·B：「`menu_rect` 的 **Mask 指纹比 `menu_dump.fingerprint` 多一条 clause**（拿 dict 的 repr 前 200 字符做子串搜索，**脆**）；实测本包**额外命中 0**、今天无害」（= 记一笔） | ✅ **做完**（**行为没改**：哪个节点算 Mask 一个没变；把「今天无害」的实测从**1 个包**扩到**全库 84 个包 / 66459 个 MonoBehaviour**：第一 clause 220 · 脆 clause 220（**全被覆盖**）· **它单独决定过的 = 0 次**） |
| **A626** | ① `menu_rect.py:918`（布局组清单走 `out`）与 `:949`（视觉框清单走 `shown`）**两处都写清「口径不同是有意的」**＋为什么 ② `menu_rect.py:737-742`（元组 append 处，**列出当前 10 项的次序**）与 `menu_dump.py:2445-2448`（⑤e 按下标读的现场）**两处都钉住「只许在末尾追加」** ③ `menu_dump_w1diff.py:47-52` 与 `_probe_deckinfo.py:111-127` 各写一条「**本脚本也是按 pid 认 GO**」的告警（含**撞车包实测数字**与安全替代 `find_rt` / `go_obj_of_rt`） | §六·B：①「同文件两种口径…**不算错**（警告多列更安全），但**不一致本身**值得记一笔」②「2 处 `MR.walk` 按下标取元组 ⇒ **以后谁再往那个元组塞东西必须加在末尾**」③「这两个脚本自己也「按 pid 认 GO」，撞车包下读数要留意」 | ✅ **做完**（= 三条全**记一笔**，**零行为改动**；没有引入 namedtuple/断言 —— 那会改共用 API，不是 §六·B 要的，见 §三·4） |
| **A801** | `extract_missing_shaders.py`：`:413-419` 函数头把这条**有条件**写全；⛔ **行为一个字没改**，改的是**让它看得见** —— `:536-550` 在 `_write_bundle()` 之后**真印一行** `[6] ⚠️ **内层文件沿用源包名**（⛔ 本模式【不】改名）：[…CAB 名…]`，把「为什么今天撞不上」「**一旦源包与产物同时加载** ⇒ Unity 判「同一个包」⇒ **整包被拒收**」以及症状（`LoadFromFile` 返回 null + `another AssetBundle with the same files is already loaded`）与**该改什么名**（照 `repack_tree` ③·b）全写进输出 | §六·B A801 行：「`repack()` 产物**不改内层名**（**沿用既有行为**、理由已写在代码里）—— 按判法处置（**很可能是「补一段注释/输出把风险写死」而不是改行为**）」 | ✅ **做完**（= 补输出，行为零改动；**实测坐实风险成立**：真跑两个源包，产物的内层名**与源包逐字相同**，见 §二·6） |

---

## 二、自检输出（全部原样贴，都是**实跑**）

### 1 `menu_rect.py` 六案 旧(HEAD) vs 新 —— **数据行逐字节未变**

```
run(){ 旧 = /tmp/menu_rect_old.py（git show HEAD:… 的副本）; 新 = 现读; diff }
[1] bundle_menus_assets_all "Player Profile Window" --depth 4             删除行=1  新增 11 行
[2] （同上）--cs                                                          删除行=0  新增 0 行（stdout 逐字节一致）
[3] （同上）--active-only                                                 删除行=1  新增 11 行
[4] （同上）--relative                                                    删除行=1  新增 11 行
[5] bundle_scenes_scenes_mainmenuwarpforge "Resource Counter Item" --depth 3   删除行=1  新增 0 行
[6] bundle_menus_assets_all "Alliance Trophy Info Popup" --depth 3        删除行=1  新增 4 行
```

**六案的「删除行」逐条查过 = 只有那一行图例**（`# 行内标记：…`，我往后接了新记号）：

```
$ diff o1.txt n1.txt | grep '^<'
< # 行内标记：`INACT` = **这一件自己的 `m_IsActiv…      ← 六案都是它（case 2 是 --cs，本来就不印图例）
```

⇒ **一个坐标、一个名字、一个标记都没动**；新增的全是**表尾出声**。例（真输出）：

```
⚠️ **本表不全**：深度上限 `--depth 4` 截掉了 **582** 个节点（那儿最深到第 **14** 层）
⇒ 要看全用 `--depth 14`（**没印 ≠ 不存在**）：
              Tab Toggle Title   （第 5 层起被截）
              …（共列 8 处）
    …… 被截掉的第一层共 25 处
```
```
⚠️ 本表不全：深度上限 `--depth 3` 截掉了 **14** 个节点（那儿最深到第 **9** 层）⇒ 用 `--depth 9`
              Progress / selectButton   （第 4 层起被截）
```

`--cs` 那一支：**stdout 逐字节 0 行差**（要贴进 `.cs` 的东西一个字节没变），警告落在 **stderr**：

```
$ menu_rect.py … --cs 2>err.txt   ⇒  stdout 46 行（与改前 diff 0 行）· err.txt 可读（UTF-8）
⚠️ **本表不全**：深度上限 `--depth 4` 截掉了 **582** 个节点…
```

### 2 A620 专项自检（`/tmp/a620_selftest.py`，5 条）

```
[1] 深度截断：cut_nodes=621（全树 622 行 ⇒ 没印的应为 621） · cut_deepest=14 · cut_roots=2 ⇒ ✅
    （**独立复核**：`cut_nodes` 与「maxdepth=99 跑出来的行数 − 1」相等，不是自证）
[2] 真缺件：miss=1 · t_kids=0 ⇒ ✅   出声那段: 🔴 **1** 个子 pid 在 `RectTransform/` 里**查不到**…
[3] 纯 Transform 子件：miss=1 · t_kids=1 ⇒ ✅   出声段: ⚠️ 另有 **1** 个纯 `Transform` 子件（3D…）
[4] 无话可说时：said=False · 文本长度=0 ⇒ ✅（**0 件时一个字都不打**，不刷屏）
[5] stats=None（老签名 8 位置参，`menu_dump` ⑤e 那种调法）→ ✅ 不抛、行为同旧
===== A620 自检： 全绿 ✅ =====
```

### 3 `menu_dump.py --verify-layout`（真回归自检）旧 vs 新

```
$ diff vl_old.txt vl_new.txt        ⇒ ✅ 逐字节一致（0 行差）
✅ 计数 = 50 · ❌ 计数 = 0
（含 ⑤e：它正是拿 `MR.walk` 的元组 `e[0]/e[1]/e[3]` 读数的那一段；⑲ 在我改过的那三处上）
```

### 4 A621 差面量化（`/tmp/a621_evidence.py`，真数据逐 RT 比）

```
===== bundle_scenes_scenes_mainmenuwarpforge
  GO pid 撞车组数: 49 · 有 _pid 的 RT: 1184
  ① 名字取错（go_name(pid) ≠ go_name_of_rt）: 49 处
       rtpid 1126 · 旧印 'icon' · 真名 'Level Up Effect'
       rtpid 1181 · 旧印 'Background' · 真名 'Image'
       rtpid 1220 · 旧印 'Mask' · 真名 'UI Collider'
===== bundle_menus_assets_all
  GO pid 撞车组数: 0 · 有 _pid 的 RT: 33020 · ① 0 处
```
⇒ A621 不是「理论上可能」：**那个包里 49 处**统计数字/自检会取错件。`menu_rect.Bundle.rt`
的每条目都带 `_pid`（实测 **0 条缺**），所以新的 `go_name_of_rt(_r['_pid'])` 有据可依。

三个真 dump 的旧/新对照（`Alliance Trophy Info Popup --depth 3` · 撞车包 `Game UI --depth 3` ·
`Player Profile Window --depth 2`）：**stdout 与 stderr 全部逐字节一致**（那是预期：这些节点不在截断面上，
改的是**将来真踩到撞车 pid 时**的行为）。

### 5 A561 证据（只读；`extract_mirror_shaders.py` 我一个字没改）

| 事实 | 实测 |
|---|---|
| 壳今天长什么样 | `wf_arena_shaders.bundle` **143037 字节** · `bf.files` **只有 1 份** `CAB-68cc81d35f88d5ad277f2717d6e1ac25`（SerializedFile，`AssetBundle=1` `Shader=7`，`Shader.type_id=24`） |
| `:84 _serialized_file()` 今天 | `next(…"SerializedFile")` **取到那一份，不抛** |
| `:112 next(AB)` 今天 | **取到 `path_id=1`，不抛** |
| 什么时候会抛 | `_serialized_file`：**一份 SerializedFile 都没有**才抛（它**会跳过**非 SerializedFile 的内层项 ⇒ 多 CAB 本身不触发）。`:112`：**第一份 SerializedFile 不是 AB 宿主**就抛 |
| **多 CAB 时到底抛不抛**（本件新测） | 拿**真包** arena1（4 个内层项）实跑：`_serialized_file()` 取到 **`CAB-….sharedAssets`**（它 `AssetBundle=1`）⇒ 今天仍不抛；**但同一包里主 CAB 的 `AssetBundle=0`** —— 在那份上跑同一句 ⇒ **`StopIteration` 实测抛出**。⇒ **失败条件是真实的**，只是「全库 84 包 AB 对象 15/15 都在第一份序列化文件里」（W230 普查）今天把它挡住了 |

### 6 A801 实跑（产物写在 `%TEMP%`，**仓库里一个字节没写**）

```
[单 CAB] 源 wf_arena_shaders.bundle → [6] ⚠️ **内层文件沿用源包名**：['CAB-68cc81d35f88d5ad277f2717d6e1ac25']…
         ✓ 返回 7 个 shader · 产物 143037 字节
         回读产物内层名 = ['CAB-68cc81d35f88d5ad277f2717d6e1ac25']
         🔴 与源包**同名**的内层文件: ['CAB-68cc81d35f88d5ad277f2717d6e1ac25']

[多 CAB] 源 scenes_scenes_battlearena1.bundle（14 MB / 2 份 CAB）
         [6] ⓘ **多 CAB 包**：AssetBundle 对象在 `CAB-….sharedAssets` ⇒ **写这一份**
         [6] 保留 2 个 shader + AssetBundle(True)，丢弃 60 个对象
         [6] ⚠️ **内层文件沿用源包名**：['CAB-8adfc300739b4da5111d4bd3eae80365.sharedAssets']…
         🔴 与源包**同名**的内层文件: ['CAB-8adfc300739b4da5111d4bd3eae80365.sharedAssets']
```
⇒ 「不改名」**逐字坐实**（不是从注释抄的），新输出两档都印得出来，且 `repack()` 的既有行为
（返回 shader 数 / 跨 CAB 登记 / 裁对象）**没受影响**。

### 7 其余冒烟（纯 python，全过）

```
extract_missing_shaders.py --builtin --check   → 正常（--check：只体检，未写文件）
_probe_deckinfo.py --selftest                  → ✅ 7/7（全过 ✅）
menu_dump_w1diff.py "Alliance Trophy Info Popup" → 29 行 (W1) / 29 行 (W3)，逐节点位移正常印出
5 个 .py compile()                             → OK ×5
行尾：5 个文件 CRLF=0（纯 LF，与改前一致）；git diff --numstat = 16/1 · 52/0 · 33/18 · 6/0 · 165/7
      （都远小于总行数 ⇒ **没有一个文件被翻行尾**）
```

---

## 三、顺手发现（⛔ 一条都没改；请调度台分流）

1. 🔴 **`menu_rect.active_in_hierarchy`（`:345-373`）还是「按 pid 认 GO」，而且它的缓存也按 pid 存** ——
   `ok = bool((b.go.get(gopid) or {}).get('m_IsActive', 1))`：撞车时读的是**另一个 CAB 那一件**的 `m_IsActive`；
   更麻烦的是 `_aih_cache[gopid]` —— **两份同号 GO 会共用一条缓存**（pid 是分包局部的，docstring 自己也这么说）。
   **实测**（同 §二·4 那个包）：**13 颗 RT** 的 `m_IsActive` 在两条路下**不一样**
   （例 `Level Up Effect`：pid 路读 True、真值 False；`Mission Toast Notification`：pid 路读 False、真值 True）。
   ⇒ 后果是 **`ANC✗`（两个工具都用它）与 `menu_dump` 的 `⛔GRP-off` 判错**。
   ⚠️ 这**不在我 7 条账里**，而且**改了会动撞车包的输出**（A619 那一族刚好在核那 24 个节点）⇒ 只报不改。
   修法草案（~2 行）：`(b.go_obj_of_rt(rt.get('_pid')) or b.go.get(gopid) or {})`，**并把缓存键从 `gopid` 换成 `rtpid`**
   （否则改动仍会被旧缓存掩盖）。⚠️ 缓存键改名要**连着看 `menu_dump.verify_layout` ⑮**（它会 `pop` 它清缓存）。
2. `menu_rect.py --cs` 的 **stdout 里本来就混着 `#` 开头的话**（撞车点名那一行 + `# （已沿 m_Father 爬父链…）`
   那两行，都在 `--cs` 分支**之前**打印）—— 那不是合法 C#，整段贴进 `.cs` 本来就会编不过。
   本件**没动**（已在那段注释里记一笔）；要清理得连那三处一起，是另一条账。
3. `menu_dump.py` `walk` 里原来还挂着 `gopid = rt.get('m_GameObject', {})…`（A499 之后的**死局部**）
   —— 已随 A621 删掉（它是「按 pid 认 GO 的残留」这一类里最容易被误读的一处）。
4. `工具/extract_mirror_shaders.py` 的两处 `next(...)`（见 §二·5）：**同族第 3 处**，今天安全、
   失败条件是真实的。⛔ **不在白名单** ⇒ 只核不改（**建议立待办**，见下）。
5. 与 A801/A802 同族的**仓库产物过期**（`wf_shaders_extra.bundle` 1338640 B vs 工具跑 699808 B）
   = **已经是 A562**，本件没重复立账；提醒：本件所有 `repack()` 实跑**只写 `%TEMP%`**，
   **没有**去「顺手更新」仓库里的那几个 bundle。

---

## 四、没做完的 / 留待办的（+ 为什么）

1. 🔴 **A561 本身没修** —— 落点是 `工具/extract_mirror_shaders.py`，**明确不在我的白名单**（简报点名）。
   **待办（可直接照做）**：`:84-85 _serialized_file()` 与 `:112 next(AB)` 两处都**加 `default` + 出声点名**
   （照 `extract_missing_shaders.py` 的 `ab_owner` 那一套：0 个 / ≥2 个一律**出声并中止**，⛔ 别静默取第一份）。
   本件已把证据核清楚（§二·5）：今天不抛、多 CAB 时**真会抛**（有真数据实证）。
2. **A625 的「脆 clause」按 §六·B 处置 = 留着 + 记一笔**（本件做的就是这个），**没有**删、**没有**合并：
   合并做不到（`menu_dump` 反向 `import menu_rect` ⇒ 成环，文件里 2026-10-13 那段已记过），
   删则有静默风险。⇒ **结论：这条账可以销**（行为零改动；「今天无害」的实测已从 1 个包扩到全库）。
3. **A523 我没有把两个口真的合成一个函数**（「收编」的另一半）：`repack()`（只留 Shader、丢流）
   与 `repack_tree()`（依赖树整份留、切流）**产物语义不同**，合并 = 大改 + 会动那份 **md5 逐字节回归**
   （`regress_repack.py` 5/5）。§六·B 要的是「**定谁是唯一口**」，本件给的是**归属表 + 冲突以谁为准 +
   唯一写盘口 + 3 处指针**。⚠️ 若调度台要的是「真合并」，那是**另一次拆函数**的活，请点名（我不自己发明口径）。
4. **A626② 的强制力只到注释级**（两处：元组 append 处 + ⑤e 读点）—— §六·B 的原话是「以后谁再往那个元组
   塞东西**必须加在末尾**」＝一句**规矩**。要**硬**约束只有两条路（加 namedtuple / 加长度断言），
   都会改共用 API 或让「合法追加」也报错 ⇒ 不在本件口径内，**留待裁**。
5. **A802（体积口径 / 要不要连别组的流一起留）** 不在我的 7 条里（`清单_A表全量.md` 标 ⚖️ 待裁）
   —— 相关的 `[6] ⓘ 体积：…` 那段输出**原样没动**。
6. ⚠️ **没验的**（如实写）：`menu_dump` 的改法只在 `bundle_menus_assets_all`（0 撞车）与
   `bundle_scenes_scenes_mainmenuwarpforge`（49 组撞车）两个包上跑过；**其余 82 包没逐包跑**
   （没撞车的包两条路恒等、撞车的包本件已量化）。`menu_rect` 的表模式只在 3 个根上对过
   （六案），没有对全量 308 个根跑逐节点比对。
