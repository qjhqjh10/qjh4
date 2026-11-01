# B114_W21 —— 55 份 `normalize: 1` 改 `0`（B-238 收尾）+ `verify()` 盲区补黄

> 写手：`W-21` · 日期：**2026-11-01** · 接 `B114_W10` 的交回项（§一·6 + 顺手发现 A/B）
> 碰过的文件：`Unity/工具/_sfx_meta.py`（改 `verify()` 一处 + 新增 2 个 helper）· 新建 `Unity/工具/_fix_audio_normalize.py` · 本报告
> 改过**数据**：`Assets/CardPresentation/Resources/Art/audio/**/*.meta` **55 份**（**只改 `normalize` 一个字节**）
> 没碰：任何 `.ogg` / `.wav` 音频本体 · `Assets/RuleEngine/**` · 其它 `CardPresentation/**` · `WarpforgeArena1/**` · 两份正本 · `账/*.tsv` · `Scenes/*.unity` · `.gitignore` · `ProjectSettings/*`
> 没跑 Unity、没动 git。

---

## §一 活 1：55 份 `normalize: 1` → `0`

### 1·1 改前现读（不是转抄，本轮重扫）

| 项 | 读数 |
|---|---|
| `Resources/Art/audio/**` 下 `AudioImporter` 型 `.meta` | **2,549 份** |
| `normalize: 0` | **2,494** |
| `normalize: 1` | **55** |
| 行尾 | **LF × 2,549**（0 份 CRLF、0 份裸 CR） |
| 每份形状 | **恰好 1 条 `^  normalize:`**（55/55 核过；不是 0 条也不是多条） |
| 55 份的分布 | **`sfx/.wav` 46 · `sfx/.ogg` 2 · `vo/.ogg` 7** |
| 每份尺寸 | **485 字节 / 23 行**（55/55 一致） |

清单与 `报告/B114_W10_normalize与音频普查.md` §三·A **逐条相同**（48 sfx + 7 vo）。

### 1·2 改法（一次性脚本，**不能用 `--backfill`**）

新建 `Unity/工具/_fix_audio_normalize.py`（**⛔ 不带 `--scan` / `--apply` 只打 usage、什么都不做**）。

为什么不能用 `--backfill`：`ensure_meta()` 是**幂等**的
（`_sfx_meta.py:437` `if os.path.exists(meta): return "exists"`）⇒ 已存在的 meta **一个字节都不碰**。

脚本在**写盘之前**逐份做四道硬校验，**任何一份不过就整批不写**：

| # | 校验 | 判据 |
|---|---|---|
| ① | 字节数不变 | `1` → `0` 同宽 ⇒ `len(new) == len(old)` |
| ② | 行数不变 | `new.count(b"\n") == old.count(b"\n")` |
| ③ | **差异恰好 1 处、且必是 `1`→`0`** | `[i for i in range(len) if old[i] != new[i]]` 长度必须 = 1 且那一位是 `b"1"`/`b"0"` |
| ④ | 改后重读 | `normalize` 必须读回恰好 1 条、值为 `0` |

参数侧还有一道闸：`--expect 55`，**实际命中数 ≠ 55 就整批不写**（防误批量改；实测 `--expect 55` 现在会红着拒绝，因为已经 0 份可改）。

### 1·3 执行与读数

```
$ python -I -X utf8 工具/_fix_audio_normalize.py --apply --expect 55
已改 55 份（normalize 1 → 0，其余字节逐个比对未变）
rc = 0
```

改后重扫：

```
$ python -I -X utf8 工具/_fix_audio_normalize.py --scan
`.meta` 共 2549 份 · normalize 分布：0 × 2549
行尾：LF × 2549
normalize != 0 的 0 份：
```

### 1·4 逐份复核（四项要求逐条兑现）

| 要求 | 怎么验的 | 结果 |
|---|---|---|
| ① **只改 `normalize` 一项** | 改前把 55 份整份拷到 `_tmp_view/w21/before_copies/`，改后 `cmp -l` 与每份对拍 | **55/55 差异恰好 1 字节**；`diff` 逐份看就是第 17 行 `normalize: 1` → `normalize: 0` |
| ② **行数守恒** | 每份改前 23 行 / 485 字节；脚本内 ②③ 双保险 | **55/55 行数、字节数都不变** |
| ③ `--check-fallback --deep` 全 ✅ | 见下 §1·5 | **rc = 0，全 ✅** |
| ④ **4 项真值没被动过** | ① 逐份 grep 改前副本 vs 现在的 `loadType` / `compressionFormat` / `preloadAudioData` / `loadInBackground`；② `--verify` 的 ③ 段按**原版真值**逐条核 | **55/55 一致（0 mismatch）**；`--verify` 报 **4 项不一致 0 条** |

> ⚠️ **`git diff --numstat` 对这批文件恒为空** —— `Resources/Art/` 整棵树在 `.gitignore:99`（`git check-ignore -v` 已确认），这些 `.meta` **不受 git 跟踪**。所以「N 份各 1 行改动」这个判据在本活**用不上**，改用改前副本 + `cmp -l` 的字节级对拍（更强：能证明只动了一个字节）。

### 1·5 `--check-fallback --deep`（改后）

```
【① 兜底模板 非真值行 ↔ 现读众数】
  bgm          .ogg    25 份 · 众数 25   · ✅ 非真值行一致
  music        .ogg     2 份 · 众数 2    · ✅ 非真值行一致
  sfx          .ogg    10 份 · 众数 10   · ✅ 非真值行一致      （改前 8）
  sfx          .wav   612 份 · 众数 612  · ✅ 非真值行一致      （改前 566）
  vo           .ogg  1785 份 · 众数 1785 · ✅ 非真值行一致      （改前 1778）
  vo           .wav   115 份 · 众数 115  · ✅ 非真值行一致
【② `normalize` 现读值】0 × 2549 ；兜底模板：.ogg 0 · .wav 0
【③ 冷重建（纯内存推算）vs 现状：2549 份】
  冷重建后 normalize 分布：0 × 2549
  与现状逐字节一致 2549 份 · 不一致 0 份        （改前：一致 2494 · 不一致 55，且差在 normalize）
【④ 端到端（--deep）】.ogg 第 1/2/3 份 ✅ 0 · .wav 第 1/2/3 份 ✅ 0
===== 兜底档 = normalize: 0，冷重建不漂 =====   rc = 0
```

⇒ **现在「整棵树冷重建」与现盘逐字节一致 2,549/2,549**（改前有 55 份会漂）。

### 1·6 回归（两个基线都还在）

| 命令 | 改前 | 改后 |
|---|---|---|
| `--verify`（`--dir` 指 `audio/` 根） | **0 红 / 0 黄** | **0 红 / 0 黄** |
| `--self-test` | 全过 | **全过**（真值 2,549 名字 · 管线 2,549/2,549 ×2 模板 · 覆盖 2,549/2,549 有真值） |
| `--backfill --dry-run` | 补 0 条（已存在 2,549） | **补 0 条（已存在 2,549）** |

`--verify` 里两个「众数」数字按预期变大（模板组吸收了那 55 份）：`.ogg 1788 → 1795` · `.wav 659 → 705`。
（`.ogg` 只 +7 而改动了 9 份：另 2 份的 4 项组合与 `.ogg` 众数不同，本来就靠「4 项对上真值」那一档放行。）

---

## §二 活 2：补 `verify()` 的盲区（黄，不是红）

### 2·1 盲区在哪（现读代码，不是转抄）

改前 `verify()` 的 ④ 段（`_sfx_meta.py:607-614`）对**有原版真值**的文件是：

```python
if prov is not None:
    # 有原版真值 ⇒ 只要 4 项对得上就不算异常（差异是**故意的**，不是漂移）
    ...
    if all(got.get(f) == prov[f] for f in PROV_FIELDS):
        continue          # ← 整条吞掉，连黄都不报
```

⇒ 那 4 项一对上，**`normalize` / `quality` / `sampleRateOverride` / 结构行**的差异**一律不比**。
这正是那 55 份 `normalize: 1` 挂了一整轮没人发现的根因。

### 2·2 改法（`verify()` 内**零行数变化**）

把 11 行换成 11 行：把「无条件 `continue`」拆成「**4 项对 ≠ 非真值字段也对**」——
`_prov_matches()` 判 4 项；通过后再走 `_nonprov_drift_notes()` 比**非真值行**：

```python
                if prov is not None and _prov_matches(meta, prov):
                    # 4 项对得上 ⇒ 整篇与众数不同是**故意的**，不算异常。
                    # 🔴 但**非真值字段**（normalize / quality / sampleRateOverride / 结构行）若也漂了，
                    # 必须**报黄** —— 这就是 `B-238` 那 55 份 `normalize: 1` 挂了一轮没人发现的盲区
                    # （`B114_W21` 补）：原来这里是**无条件** `continue`，把整条差异吃掉了，连黄都不报。
                    for w in _nonprov_drift_notes(meta, canonical):
                        warnings.append(w)
                    continue
                warnings.append(f"模板与同目录众数不同（{c} 条这一款）：{os.path.basename(meta)}")
```

两个新 helper 追加在**文件尾部**（`self_test` 之后、`main()` 之前）：`_prov_matches()` · `_nonprov_drift_notes()`。
它们复用已有的 `_nonprov_lines()` / `_line_diff()` / `render_meta()`，不新增口径。

**为什么放在尾部**：`verify()` 里插行会让 `self_test` 的起始行漂走。

### 2·3 行号守恒（5/5 核过）

```
312:def apply_original_prov(template, name):
393:def pick_template(dirpath, ext):
431:def ensure_meta(path, guids=None, dry_run=False, quiet=False):
485:def verify(dirpath=SFX_DIR, root=PROJECT_ROOT):
622:def self_test(dirpath=None):
```

**与改前逐行相同**（`判据/B114_R4_B93重建性.md:25,31` 与 `活状态表.md:338` 引的就是这几个）。

### 2·4 黄计数（从多少变多少）

| 树 | 改前（旧 `verify`） | 改后（新 `verify`） |
|---|---|---|
| **真实 `audio/`（活 1 之后）** | 0 红 / 0 黄 | **0 红 / 0 黄**（基线未破） |
| **同一棵树、但把 55 份换回改前** | 0 红 / **0 黄**（被吞） | 0 红 / **55 黄** |

⇒ 黄**是新增的计数维度，不是把红变黄**：红只来自 4 项与原版真值不一致（那一段一个字没改）。

### 2·5 灭自证（三条，全部实跑）

**① 新检查真会响、且不乱报**（`_tmp_view/w21/test_verify_blindspot.py`，临时目录）

```
【对照组：两份都 normalize: 0】4 项组合 = (1, 2, 0, 1)  → 0 红 / 0 黄
【实验组：一份 normalize: 1】                         → 0 红 / 1 黄
    ⚠️ 非真值字段与同扩展名众数不同（那 4 项已对上真值）：Aeldari Blackout.wav.meta → normalize: b'normalize: 1' → b'normalize: 0'
===== 灭自证通过：对照组不乱报、实验组确实报 1 黄 =====
```

**② 旧写法确实吞掉（反证）**（`test_old_silent.py`：把那段就地回滚成旧写法、同一套实验）

```
【实验组：一份 normalize: 1】 → 0 红 / 0 黄     ← 旧版连黄都不报
===== 反证成立：回滚版 0 红 / 0 黄 —— 旧版确实把这条吞掉了 =====
```

**③ 新版能抓到「改前那 55 份」**（`test_before_state.py`：模拟树 = 全部 2,549 份 meta + 同名空音频，其中 55 份换回改前）

```
模拟树：2549 份 meta · 其中 55 份换回改前（normalize: 1）
  → 0 红 / 55 黄
===== 反证成立：新版会把这 55 份报成黄 =====
```

⚠️ ②③ 的「通过」判据都只看**结果行**（`  → 0 红 / 1 黄`），⛔ 不拿总判语里的同名子串当命中
—— 第一版就因为这个子串误判过一次（`✗ 实验组应 0 红 / 1 黄` 里也有那串），已就地修正。

---

## 没查清的部分

1. 🟠 **`normalize` 到底该是 `0` 还是 `1`，本机没有原版判据**。`AudioImporter` 是编辑器侧设置、构建时被消费、**不进包体**（`B111_T2` §B-25 查过 `assets_full` / AssetRipper 导出 / bundle typetree 三处）。
   ⇒ 本次改动的口径是「**与其余 2,494 份的现读众数对齐**」，**⛔ 不是「原版就是 0」**。
   唯一能作为间接旁证的是「原版走 FMOD、没有 Unity 导入期响度归一化这一层」，但那是**推理**，不是判据。
2. **`normalize: 1` 对实际音量的影响幅度没测**（要真解码 / 听感比对，超出本活；且本机没有「那 55 条改前 vs 改后」的音频真值可比 —— 音频本体一个字节都没动，变的只是导入设置）。
3. **`quality` / `sampleRateOverride` 仍然没有原版真值**（同 1）。本轮只处理了 `normalize`。

---

## 顺手发现

**A. `Resources/Art/` 整棵树被 gitignore**（`.gitignore:99`：`/Unity/MyGame/Assets/CardPresentation/Resources/Art/`）
⇒ 这批 `.meta` 的改动**不会出现在 `git status` / `git diff`** 里，也不会进提交。
⚠️ 简报里那条「改完 `git diff --numstat` 应该是 N 份文件各 1 行改动」在这个目录上**判不了**（恒空）。
本轮改用 `cmp -l` 字节级对拍（更强）。**后续任何改 `Resources/Art/` 的活都要知道这一点** —— 别拿「git 干净」当「没改动」。

**B. `verify()` ④ 的告警文案与实际分组口径不符**
文案写「模板与**同目录**众数不同」，但 `by_ext` 是按**扩展名**跨目录分组的（`_sfx_meta.py:565-567`）。
本轮的 `.ogg` 众数就是 **bgm / music / sfx / vo 四个目录一起投出来的**（取得是 `Warpforge Genestaler Cults Combat Medium.ogg`，在 `bgm/`）。
**没改**（超出本活范围、零行为影响）：只在这里记一笔，下个会话若动这段可以顺手把「同目录」改成「同扩展名」——我那两条新告警已经写的是「同**扩展名**众数」。

**C. 全工程只有这 2,549 份 `AudioImporter` meta，全部在这棵树里**
`Assets/**` 下 `AudioImporter:` 命中 **2,549**、`normalize: 1` 命中 **0**（改后）—— **没有别处的漏网**。
唯一另外的 12 处 `normalize: 1` 在 `MyGame/Library/PackageCache/com.unity.timeline@…/Samples~/…/*.mp3.meta`，那是 **Unity 包缓存里的官方示例**，⛔ 不属于我们的资产、不该动。

**D. `--verify` 报的「全工程 meta 份数」在两轮之间从 26,757 变成 26,758**
那是**别的写手在本波新建 `.cs.meta`** 造成的（本轮只有我一个进程在动音频），不是本活的影响。
⚠️ 这个数字**受并发写手影响**，别拿它当基线判据。

**E. `.ogg` 与 `.wav` 的 4 项组合分布（`--self-test` 现读，可当日后的回头尺子）**
`(1,1,0,1) ×1795` · `(1,2,0,1) ×705` · `(1,2,0,0) ×21` · `(1,1,1,1) ×15` · `(2,1,1,1) ×8` · `(2,1,0,1) ×5`，合计 2,549。
—— 这就是为什么 `.ogg` 的众数只有 1,795/1,822、`.wav` 705/727：**不是漂移，是 6 种原版真值组合并存**。

---

## 产物 / 复跑方式

```bash
# 只读普查（分布 + 逐份清单 + 行尾）
python -I -X utf8 工具/_fix_audio_normalize.py --scan
# 真写盘（幂等；--expect 是防误批量的闸）
python -I -X utf8 工具/_fix_audio_normalize.py --apply --expect 55
# 改后验证（三条都要绿）
python -I -X utf8 工具/_sfx_meta.py --verify        --dir d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio
python -I -X utf8 工具/_sfx_meta.py --check-fallback --deep --dir d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio
python -I -X utf8 工具/_sfx_meta.py --self-test     --dir d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio
# 灭自证（三条）
python -I -X utf8 _tmp_view/w21/test_verify_blindspot.py
python -I -X utf8 _tmp_view/w21/test_old_silent.py
python -I -X utf8 _tmp_view/w21/test_before_state.py
```

`_tmp_view/w21/` 是 scratch（改前副本 `before_copies/` + 清单 + 哈希 + 三个测试脚本），可随时删。
