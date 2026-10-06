# W231① · `_verify_prefab_bundle.py` 多 SerializedFile

> 2026-10-13 · 执行写手（「清空 A 表」第四轮 · 我的账 = **A231①**）· **纯 python：没跑 Unity**、
> **没动 git**、**没写 `d:/2/`**、**只改了白名单里那一个文件**。
> 下面命令里的 `PY = "D:/2/Warpforge_tools/py312/python.exe"`；临时探针都在 `%TEMP%`（不入库）。

## 一、做了什么（结论）

**A231① 已修**：`工具/_verify_prefab_bundle.py` 里「取第一份 `SerializedFile`」这个形态**没有了**。
口径**照的是同工程里已经修好的那一份** —— `工具/extract_missing_shaders.py` 的 `repack_tree()`（A173 修的），
⛔ 没有自己发明一套。

| 用途 | 改前 | 改后 |
|---|---|---|
| 容器 / 预加载查哪一份 CAB | `bf.files` 里**第一份** `SerializedFile` | **装着 `AssetBundle` 对象的那一份**（`ab_owner_sf()`；0 个 / ≥2 个 AB 对象**出声中止**） |
| 走流 Texture2D、`AnimationClip` 普查 | **只看第一份** | **扫全部份**（`load()` 把全部份带出来） |
| 同一个 pid / 同名流对象在多份里出现 | 裸 pid（名字）当字典键 ⇒ **后写盖前写，静默少查** | 带上 CAB 名收集 + **出声**（`hits` / `dups`） |
| [3] 逐张比字节用哪条流缓冲 | 写死 `<NEW_CAB>.resS` 一条 | **按该对象自己的 `m_StreamData.path` 末段取**（新增 `buf_of()`） |
| [4] 瘦身式子的 Σsize | 全部走流对象 | **只算写那一组（`<NEW_CAB>.resS`）的**（别的 CAB 整份保留、**不瘦身**，不在这个式子里） |

**验收三条的落点：**

1. **口径与已修写法一致** —— `repack_tree()` ⓪ 段那条「**挑装着 AB 对象的那一份**」+「**挑不出就出声中止**」
   逐条照搬；pid 命名空间那一条落在 `clips_in_bundle` 的键上（`{PathID: [(CAB 名, m_Name), …]}`）。
2. **真 bundle 实跑：改前 / 改后 stdout 逐字节相同**（`diff` 无输出；两次都 `exit 0` + `✅ 四项全过`）
   —— 真产物与它的源包**都是单 CAB**，改后走的是同一条路（口径：**不动没坏的输出**）。
3. **「更全」有数**：拿真多 CAB 包（`scenes_scenes_*`，**15 个全扫**）比 —— 改前只看得见 2～593 个对象，
   改后看得见 3217～5988 个 ⇒ 合计**新多看到 73,226 个对象**；而本脚本要查的那两类
   （走流 Texture2D / `AnimationClip`）在 15 个包里**恰好都落在第一份** ⇒ **结论一致**
   （这正是「今天触发不到」的实证，与 `波B1_工具三件.md` 的普查对得上）。

> ⚠️ **别把本件当成 A231 全清**：**A231②**（`extract_missing_shaders.py` 的 `repack()` 旧口）
> 与 **A231③**（多 CAB 产物**没在 Unity 里加载过**）都**还开着**，见 §五。

## 二、改动清单（`文件:行` —— 行号 = **改后**）

`d:/4/Unity/工具/_verify_prefab_bundle.py`（`git diff --numstat` = **128 增 / 31 删**，不是整篇重写）：

| 行 | 改了什么 | 为什么 |
|---|---|---|
| `50-58` | 文件头加一段「2026-10-13 修（A231①）」变更记录 | 铁律 5·b：口径与判据要落盘；顺手写明「今天触发不到 + 判据指向哪」 |
| `119-136` | `load()`：返回值从 `(bf, sf)` 改成 `(bf, [**全部** SerializedFile])`；多 CAB 时打一行 ⓘ | 「第一份」不是判据（实测是撞巧） |
| `139-153` | **新增** `ab_owner_sf(sfs, tag)`：挑「装着 `AssetBundle` 对象的那一份」；0 个 / ≥2 个**出声**并返回 `None` | 照 `repack_tree()` 的口径 |
| `156-178` | `streams(sfs)`：参数从**一份**改成**一份列表**；记录从 3 元组 → `(CAB 名, 流路径, offset, size)`；返回值多一个 `dups`（同名撞车） | 多 CAB 时别的份原来**静默不查** |
| `181-189` | **新增** `buf_of(bf, stream_path)`：`archive:/<目录>/<内层名>` → 那份内层文件的字节 | [3] 原来写死一条 `.resS`，多 CAB 产物里别的组有自己的流 |
| `198` | `bf, sfs = load(DST)`（原 `bf, sf = …`） | 上面接口变了 |
| `200-203` | `[1]` 的红字补一句「产物真变成多 CAB 的话，这条期望值要一起改（见文件头）」 | 期望值契约的指路，**不静默** |
| `205-215` | `sf = ab_owner_sf(sfs, "产物")`；挑不出 ⇒ **出声中止 `return 1`** | 顺带挡住旧代码 `ab is None` 的崩（见 §六·1） |
| `239-247` | `clips_in_bundle` 改成**扫全部份**、键改 `{PathID: [(CAB 名, m_Name), …]}` | 裸 pid 跨 CAB 会撞号（arena1 实测 63 个）⇒ 按裸 pid 建字典会**静默丢对象** |
| `266-279` | clip 落点判据：`own = [在 AB 那一份里的]`；失败时把「为什么」打出来（在别的 CAB 里 / 这份里没有） | 容器项写的是 `PPtr(m_FileID=0, …)` ⇒ **只认 AB 那一份** |
| `281-292` | `streams(sfs)` / `streams(src_sfs)` + **同名撞车出声**；`src_raw` / `dst_raw` 只留给 [3] 表头与 [4] 显示 | 同名撞车 = 按名字配对的分不清，**出声** |
| `293-309` | [3] 逐张：缓冲改 `buf_of(…, o0[1])` / `buf_of(bf, p)`；索引从 3 元组 → 4 元组（`o0[3]` 比 size、`o0[2]` 是 offset） | 多 CAB 时用主组缓冲去比**必错** |
| `311-325` | [4] `thin` = 只有 `path` 末段 = `<NEW_CAB>.resS` 的那些；多 CAB 时打一句「另有 N 张不计入」 | 「别的 CAB 整份保留、**不瘦身**」⇒ 它们不该进这个不等式 |

**没碰**：`工具/extract_missing_shaders.py`（**A231②/A230 的地盘**，白名单外）· 任何 `.cs` · 两张正本 · `d:/2/` 的写。

## 三、判据出处（照的是哪份已修写法 / 原版哪条）

| # | 判据 | 出处（**现读**） |
|---|---|---|
| 1 | **挑「装着 AB 对象的那一份」**（不是第一份）+ 0 / ≥2 个一律出声 + 多 CAB 打一行 | `工具/extract_missing_shaders.py:718-745`（`repack_tree()` ⓪ 段：`cab_sfs` 全收 · `ab_owner` 显式挑 · `len>1` 中止 · `len(cab_sfs)>1` 打 ⓘ）。⚠️ **行号 = 2026-10-13 我开工时现读** —— 那个文件归 A231②/A230 的写手，可能被改动 |
| 2 | 三条口径的**文字判据**（写/查哪一份 · 另一份怎么留 · 挑不出就中止） | `资料/普查产出_1008/波B1_工具三件.md:31-35`（口径表）· 同件 `§四·2`（记的**正是本脚本这一处**） |
| 3 | 本件的现核（行号、现状、切块建议） | `资料/普查产出_1013/A表现核_块5.md:317-338`（§A231） |
| 4 | `m_FileID == 0` = **引用方自己所在的那一份** ⇒ 容器项只指得进 AB 那一份 | `波B1_工具三件.md:33`（判据是代码级：`extract_missing_shaders.py:427-434` 把容器/预加载写成 `PPtr(m_FileID=0, PathID)`） |
| 5 | A 表那一格的原文 | `资料/待办判据_1008.md:35`（`A231` 行） |

**独立复算（本件实跑，不是抄波B1）**：15 个多 CAB 包的 **AB 对象 15/15 都在「第一份」** ·
arena1 两份 CAB **63 个 pid 撞号** —— 两个数与 `波B1_工具三件.md:41-45` **对得上**。

## 四、实跑验证（命令 + 输出摘要 + 改前改后对比）

### 4·1 编译 + 真跑 + 改前/改后 `diff`（核心验收）

```bash
PY -m py_compile "d:/4/Unity/工具/_verify_prefab_bundle.py"                  # → compile OK
git -C d:/4 show HEAD:Unity/工具/_verify_prefab_bundle.py > $TEMP/w231_before.py
PY "$TEMP/w231_before.py"                        > "$TEMP/w231_before.txt" 2>&1   # exit=0
PY "d:/4/Unity/工具/_verify_prefab_bundle.py"    > "$TEMP/w231_after.txt"  2>&1   # exit=0
diff "$TEMP/w231_before.txt" "$TEMP/w231_after.txt"                          # → 无输出
```

```
（无差异 —— stdout 与改前逐字节相同）
```

改后那一次的输出（节选，`✅ 四项全过`）：

```
[1] 产物内层文件 2 个：CAB-wfprefabsextra, CAB-wfprefabsextra.resS
[2] 容器 16 条（期望 16 = 7 件根 × 裸名/小写路径 + 2 条 GUID 别名）：[…16 条…]
   ✅ 有   Card 3D Death Explosion              裸名 ✅ · 小写路径 'assets/card 3d death explosion.prefab' ✅
   …（7 件根逐条 ✅）
    预加载表 7 条（应为 7 = 根个数）
[2·b] … ✅ 包里 PathID 1230949865609814630 的对象 m_Name = 'LightAnimationOrbit'（应为 'LightAnimationOrbit'）
        ✅ 包里 PathID 2397232529203406555 的对象 m_Name = 'Dark Angels Void Combat animations'（…）
[3] 走资源流的纹理：源包 344 张 · 产物 6 张；流大小 源包 201.6 MB → 产物 219 KB
   ✅ Embers / GlowPalet / Vanguard Frame Animation VAT_PositionTex / …_RotationTex / Vanguard_emission / Vanguard_tex2
[4] 流瘦身：产物 219 KB（用到的区间合计 219 KB，上限 219 KB）· 源包 201.6 MB

结论：✅ 四项全过
```

⇒ **真产物上「改前 = 改后」**（单 CAB，两条路走的是同一份 ⇒ 输出逐字节相同）。

### 4·2 「更全」：真多 CAB 包上的改前/改后各看到什么（15 个全扫）

探针 `$TEMP/w231_cmp.py` —— 用**两份真实实现**（`OLD` = `git show HEAD:` 那一份、
`NEW` = 改后的工程件），各自调用自己模块里的**真函数**（不是重写一遍）：

```python
NEW = imp("vp_new", r"d:/4/Unity/工具/_verify_prefab_bundle.py")
OLD = imp("vp_old", r"C:/Users/qjh36/AppData/Local/Temp/w231_before.py")
for 每个 scenes_scenes_*.bundle:
    env = UnityPy.load(p); bf = list(env.files.values())[0]
    sfs = [v for v in bf.files.values() if type(v).__name__ == "SerializedFile"]
    old_sf = next(v for v in bf.files.values() if type(v).__name__ == "SerializedFile")  # 旧口径
    new_sf = NEW.ab_owner_sf(sfs, n)          # 新口径（唯一时它不说话）
    比 OLD.streams(old_sf) vs NEW.streams(sfs)  /  AnimationClip 计数
```

```
包                            bf.files 序                AB 在         旧=AB?   对象旧/新      走流Tex 旧/新  clip 旧/新  同名撞车
battlearena1                  8adfc300739b→8adfc300739b  8adfc300739b…  是        63/5367        5/5           2/2         0
battlearena2                  a5741cd1d9cf→a5741cd1d9cf  a5741cd1d9cf…  是        69/5589        6/6           4/4         0
battlearena3                  5a65e4d1d096→5a65e4d1d096  5a65e4d1d096…  是        20/5447        0/0           1/1         0
battlearenaaeldari            3e77f00d313c→3e77f00d313c  3e77f00d313c…  是        20/5299        2/2           1/1         0
battlearenaastramilitarum     6ed17e44621e→6ed17e44621e  6ed17e44621e…  是       111/5592        4/4           2/2         0
battlearenablacklegion        aac6b8f6d2ec→aac6b8f6d2ec  aac6b8f6d2ec…  是        77/5453        3/3           2/2         0
battlearenadarkangels         8bfe6c0676e8→8bfe6c0676e8  8bfe6c0676e8…  是        89/5431        5/5           2/2         0
battlearenaemperorschildren   2f09397c0a41→2f09397c0a41  2f09397c0a41…  是       112/5450        6/6           2/2         0
battlearenagenestealers       dfc5d1957f6f→dfc5d1957f6f  dfc5d1957f6f…  是        83/5392        7/7           2/2         0
battlearenaleviathan          0c7eecc48def→0c7eecc48def  0c7eecc48def…  是        18/5487        1/1           1/1         0
battlearenasororitas          8c234bade431→8c234bade431  8c234bade431…  是        20/5535        1/1           1/1         0
battlearenaspacewolves        b8d10d00737d→b8d10d00737d  b8d10d00737d…  是        77/5387        3/3           1/1         0
battlearenatauviorla          b969b7b62606→b969b7b62606  b969b7b62606…  是        80/5988        4/4           4/4         0
mainmenuwarpforge             32e46c0abb8a→32e46c0abb8a  32e46c0abb8a…  是       593/3217        1/1           1/1         0
simpletransition              379095088d9d→379095088d9d  379095088d9d…  是         2/26          0/0           0/0         0

共 15 个多 CAB 包；走流 Tex2D + AnimationClip 两类「旧看得到的 ⊆ 新看得到的」：15/15 个完全相同
（差异 = 新多看到 73226 个对象）
AB 对象是否总落在「第一份」：True
```

⇒ ① **覆盖确实更全**（多 73,226 个对象）；② 今天**结论仍一致**（两类对象恰好都在第一份里）
—— 与 `波B1_工具三件.md` 的普查（「非写那份里 0 个走流对象」）**互相印证**。

### 4·3 「AB 不在第一份」会怎样（真包 + 条件模拟，把将来那一支提前跑一遍）

真包 = `scenes_scenes_battlearena1.bundle`；把「第一份」**模拟**成主 CAB（`AB` 对象不在它里面）：

```python
OLD.DST = P; OLD.SRC = P; OLD.load = lambda p: (bf, main_cab)   # 让改前那套走到这个条件
OLD.main()      # ← 真跑改前代码
```

```
   🔴 期望 2 个（主 CAB + .resS）
[2] 容器 0 条（期望 16 …）：[]
   🔴 容器条数 0 ≠ 期望 16
   🔴 没有 裸名 `Card 3D Death Explosion` —— `LoadAsset(name)` 会取不到
   …（7 件根 × 2 条，共 16 条**误导性**红字：真因是「AB 对象不在这一份里」，字面却像「容器没登记」）
改前：**抛异常** AttributeError: 'NoneType' object has no attribute 'm_PreloadTable'
--- 改后同一条件 ---
改后挑到的仍是 `.sharedAssets`（= 装着 AB 的那一份，**与 bf.files 的序无关**）; 对象 63 个；流 5 张
```

⇒ 改前在那一支是**「一串误导性红字 + traceback」**；改后是**「挑对了那一份，照常出结论」**
（挑不出来时才是 `🔴 挑不出该查的那一份 ⇒ 中止`，见 4·4）。
⚠️ 这是**条件模拟**（真包 + monkeypatch），不是真多 CAB 产物 —— 见 §五。

### 4·4 新加的四条出声路径（今天真数据触发不到，人工构造输入验过）

```
[a] 0 个 AB 对象（真包里的主 CAB `CAB-8adfc300739b4da5111d4bd3eae80365`）：
   🔴 产物里 AssetBundle 对象 0 个（要求恰好 1 个）⇒ 挑不出「该查的那一份」
    → ab_owner_sf 返回 None
[b] 2 个 AB 对象（同一份传两次）：
   🔴 产物里 AssetBundle 对象 2 个（要求恰好 1 个）⇒ …：pid 2 @`CAB-….sharedAssets`
    → ab_owner_sf 返回 None
[c] buf_of 拿不存在的流文件：None
[d] 多 CAB 时 load() 的出声（真包）：
   ⓘ 多 CAB 包：内层 2 份 CAB（['CAB-….sharedAssets', 'CAB-…']）—— 对象/流按**全部份**查；
     容器与预加载只查「装着 AssetBundle 对象的那一份」
    → 带回 2 份 SerializedFile（改前只带 1 份）
```

### 4·5 行尾 / diff 规模自证（铁律：别把行尾翻了）

```
git diff --numstat -- Unity/工具/_verify_prefab_bundle.py   →  128  31   （不是整篇 235 行）
crlf= 0 lf= 332     （改前 235 行 → 改后 332 行，**纯 LF**，没被翻成 CRLF）
```

改法用 Edit 工具逐处替换（⛔ 全程**没用 `sed -i`**）。

## 五、没查清 / 没做的

1. 🔴 **A231③ 仍开着，别当已验收**：**多 CAB 产物没有在 Unity 里真加载过** —— 本件只是
   UnityPy 规则级 + 脚本级证据（与 `块5 §A231③`、A230 的 ② 是同一条）。
2. 我**没有端到端造一个多 CAB 产物**来整跑 `main()` —— 造它要重打包，落在白名单外
   （`extract_missing_shaders.py` / `d:/2/` 写）。⇒ 多 CAB 分支只做到**分支级**验证：
   `buf_of()` 用真包的 `archive:/CAB-…/CAB-….resS` 路径验过 · [4] 的 `thin` 过滤条件用真数据等价式验过 ·
   `ab_owner_sf` 的 0 / ≥2 支用真包对象人工构造验过（4·4）· 4·3 那次把「AB 不在第一份」整条路走过一遍。
3. **「同名流对象撞车」`dups` 支没有真实用例**（15 个真包 0 例）⇒ 只做了代码审查，**没跑过真数据**。
4. `[2·b]` 里「pid 在别的 CAB 里、但不在 AB 那一份里」的**失败文案**同理没人触发过（今天两条 clip 都在 AB 那一份）。
5. 本件**没跑任何 Unity 自检** —— 纯工具（`工具/*.py`）⇒ 按铁律 12 判据③ **一条都不用跑**。

## 六、顺手发现（⛔ 没自己顺手改）

1. 🔴 **改前那套在「AB 不在第一份」时会崩**（不只是静默）：`ab=None` 时仍去读 `ab.m_PreloadTable`
   ⇒ `AttributeError`（4·3 实跑）。**本件顺带把它挡住了**（挑不出 AB 那一份 ⇒ `🔴` + `return 1`，无 traceback）
   —— 这是本次改动的**同一处**（挑那一份 + 出声），**不是**另开的改动。⛔ 白名单外一个字没动。
2. **`extract_missing_shaders.py` 的 `repack()` 旧口（A231②）没动**（白名单外，另派）；
   `块5 §A231` 那句「**两个重打包口并存**本身是一条债」**仍然成立**（建议 A231 收口时定「谁是唯一口」并写进文件头）。
3. `bf.files` 的序**不是**「`.sharedAssets` 一定在前」这么简单：arena1 完整序实测 =
   `[.sharedAssets, .resource, .sharedAssets.resS, 主 CAB]`；⚠️ **「第 0 项」与「第一份 SerializedFile」是两回事**
   —— `load()` 里 `list(env.files.values())[0]` 取的是**外层 BundleFile**，那一步本来就对，**没动**（别被 grep 吓到）。
4. **独立复算印证了波B1 的两个普查数字**（15/15 AB 在第一份 · arena1 63 个 pid 撞号）——
   两个独立来源一致，可以放心当已知量用。
5. `资料/待办判据_1008.md:35`（`A231` 行）写的行号 **现核全部准确**
   （改前 `:110` = `bf = list(env.files.values())[0]` · `:111` = `next(…)` · `:115` = `def streams(sf):`）
   ⇒ **没有可改的错，我没改那份文档**（避免与并发写手撞车）。但**改后行号已变**
   （`load` 在 `:119-136` · `ab_owner_sf` 在 `:139-153` · `streams` 在 `:156-178`），
   且 ① 已做完 ⇒ **销账时请连行号一起换**。
6. ⚠️ `[1]` 那条 `len(bf.files) != 2`：真出现多 CAB 产物时它会红 —— 那是**期望值要随包内容一起改**
   （文件头已写这条契约），**不是缺陷**；我在红字后补了一句指路。
