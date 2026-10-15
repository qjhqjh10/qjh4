# R6 — `STAT_FIXES` 孤立键守卫 + 删两条死键 + `ZH_NAME_ALIAS` 重复键去重

日期 2026-10-15 · 笔 `R6` · 范围 = **只做「删死键 + 加守卫」**（口径冲突 → 见 §四，⛔ 本笔不裁）
文件：`D:\4\Unity\工具\gen_cards_engine.py` · 产物：`D:\4\Unity\MyGame\Assets\RuleEngine\Resources\cards_engine.json`

---

## 〇 汇总读数

| 项 | 值 |
|---|---|
| 行尾 | 改前 crlf=**2503** lf=2503（纯 CRLF）→ 改后 crlf=**2554** lf=2554（**纯 CRLF 保持**） |
| 字节 | 189,837 → **194,985** |
| 行数 | 2,504 → 2,555 |
| `git diff --numstat` | `126  35  Unity/工具/gen_cards_engine.py`（**不是整篇重写**，行尾没翻） |
| 生成器重跑 | **rc=0**，写出 728 KB |
| 卡池 | **逐字节零变化**：md5 `3f3c0e4f0435af69e3192983fb0bc94f` · 745,730 B · `count 1127` · `version 9` |

**卡池 diff = 0 张**（1127/1127 逐卡逐字段比过，`field diffs: 0`；顶层键集合也相同）。
原因见下：两条死键**本来就是死的**（删掉不影响任何输出）、守卫只在非法状态才出声。

---

## 一 改了什么（逐处）

锚点全部按**行数组**（`b.split(b'\r\n')` → 改 → `b'\r\n'.join()`）改写，每处替换前 `assert` 原行逐字相符；脚本 `D:\4\_tmp_view\r6_edit.py`。

**① `D:\4\Unity\工具\gen_cards_engine.py:204`**（原 1 行 → 现 11 行，行 204-214）
`    "Simulacrum Imperialis": "epic",   # Sorotitas/3部队/…（紫；卡面印 `Simulacrum Celestian`）`
→ **删**，原位留一条「🔴 2026-10-15 删除」说明（理由与同表 `:183-186` 那条 `1st Company Terminator` **完全同形**：① 不在源表名里、永远不会被查中；② 这张卡**宝石表里有**）。
现读计数：`Simulacrum Imperialis` 在全文件出现 **4 次，全在注释里**，`RARITY_UNLISTED` 的**活键为 0 条**（源表名口径实测 `[]`）。

**② `:409`**（原 1 行 → 现 11 行，行 409-419）
`    "Simulacrum Imperialis": {"ranged": 4},`
→ **删**，原位留「🔴 2026-10-15 删除」说明 + 指向 §四 那两条路。
现读 `STAT_FIXES` 键数 **30 → 29**；**死键 0 条**。

**③ `:71-73`**（原 3 行 → 现 3 行注释，净 0 行）
`ZH_NAME_ALIAS` 里 `"Imotekh the Stormlord"` / `"Orikan the Diviner"` 那 3 行**逐字重复了两遍**（`:68-70` 与 `:71-73`）
→ **删掉第二份**，在保留的那一份下面补 3 行说明（🔴「同键同值、后写的赢、前一份静默失效」）。
现读 `ZH_NAME_ALIAS` **4 个键、无重复**（AST 扫全文件 `Dict` 字面量的重复键 = 0 处）。
⚠️ 三条别名**都还活着**（现核：目标键 `Fire Warrior Sniper` / `stormlord` / `Diviner` **都在** `zh_cards.json` 的 `cards` 里）⇒ **行为零变化**（两条的值本来就相同）。

**④ `:1506`（新增，`build()` 头部）**
`used_stat_fix = set()` —— 与 `used_face_fix`（`:1505`）并排，同一形状。

**⑤ `:1625`（新增，`STAT_FIXES` 消费点内）**
在 `if fix:` 块里加 `used_stat_fix.add(name_before_rename)` —— 记账口。
⚠️ 记的是 **`name_before_rename`**（**改名前的卡名**，`STAT_FIXES` 的键就是它），与 `:1601` 的 `STAT_FIXES.get(name_before_rename)` **同源**。

**⑥ `:1954-1962`（新增 9 行，`build()` 末尾、紧接 `orphan_face_fix` 那两个循环之后）**
```python
orphan_stat_fix = [k for k in STAT_FIXES if k not in used_stat_fix]
```
—— 与 `:1930-1936` 的 `orphan_face_fix` 同一形状。

**⑦ `:2033`** `       orphan_face_fix, \` → `       orphan_face_fix, orphan_stat_fix, \`（返回元组，+1 元）

**⑧ `:2096`** `main()` 解包：`… orphan_face_fix, key_conflicts, goes_second, …`
→ `… orphan_face_fix, orphan_stat_fix, key_conflicts, goes_second, …`（**位置必须跟返回元组一致**）

**⑨ `:2162-2180`（新增 19 行，`main()` 里紧接 `orphan_face_fix` 那个打印块之后）**
镜像 `orphan_face_fix` 那一支的打印（`if … else …` 两态），输出见下。
⚠️ **口径与 `orphan_face_fix` 完全一致：只出声、不进 `_bad`**（`:2470` 那句 `_bad = …` 没动）⇒ **它不会让生成器拒写**。要「红到不落盘」得把 `orphan_stat_fix` 加进 `_bad`，那是**改行为、本笔不裁**（已写进代码注释）。

---

## 二 守卫有效性的演示

**注入** = `D:\4\Unity\工具\gen_cards_engine.py` 里把 `"Baneblade Tank":` 改成 `"Baneblade Tank_R6PROBE":`（1 处，`b.count` 断言；行尾 CRLF 计数前后相同）。
注入后跑生成器 **rc=0**，`D:\4\_tmp_view\r6_probe.log:76-78` 输出**逐字照抄**：

```
🔴 数值修正表里有 1 个键**没匹配到任何卡**：
            · Baneblade Tank_R6PROBE
            ⇒ 这几条修正**这次一条都没生效**（卡还在的话，数值退回源表的 OCR 值）。多半是卡改名了 —— `STAT_FIXES` 的键必须写成**改名前的卡名**（`build()` 里 `STAT_FIXES.get(name_before_rename)`）。
```
同一跑里 `数值修正     28 张`（原 29）、`名` 那一行确认 `Baneblade` 还在池里但**数值掉回源表的 OCR 值**：

| | 未注入 | 注入后 |
|---|---|---|
| 池里 `AM38 Baneblade Tank.ranged` | **12**（卡面紫圆） | **2**（= OCR 把右侧护甲盾读成远程那个老坑） |
| 池 md5 | 3f3c0e4f… | `b20f6a2d7bdd6b8a920ac6bb53f08e11` |

⇒ **守卫说的「一条都没生效」是真的**：池子是**静默**变的（rc=0、别的读数照常打印）。

**撤回**：`cp D:\4\_tmp_view\gen_cards_engine.py.r6_clean 工具/gen_cards_engine.py`（`grep -c R6PROBE` = **0**）→ 重跑 **rc=0**：

- 撤回后池 md5 = **`3f3c0e4f0435af69e3192983fb0bc94f`** · 745,730 B —— **与「未注入」时逐字节相同** ✅
- `数值修正表：所有 29 个键都匹配上了 ✅`
- 临时探针**无残留**（`'R6PROBE' in 全文件` = False；AST 解析 OK）

### 「怎么才能红」的判据（四条，任一成立即出声）

1. **结构性判据（不跑生成器就能查）**：`STAT_FIXES` 的某个键**不等于任何源表卡名**（`card_stats.json` 的 `name`，空白归一后）⇒ 该键必然 `orphan`。本笔实测：改前 30 键里 **1 条**命中（`Simulacrum Imperialis`），改后 **0 条**。
2. **口径判据**：`STAT_FIXES.get(name_before_rename)` 里 `name_before_rename` = 源表原始 `name`（`:1546`），**不是** `STAT_FIXES` 自己 `"name"` 改后的名字 ⇒ **把某张卡的源表卡名改掉**（或给某条键写一个源表里不存在的名字）就红。
3. **行为判据**：红时 `len(stat_fixed)` 会比键数**少**（演示里 29→28）—— 这是「有修正没生效」的第二个读数。
4. ⛔ **它不会让 rc ≠ 0**（口径同 `orphan_face_fix`）⇒ 「红」= **跑完摘要里出现那一行**，不是退出码。判据：`grep -c "数值修正表里有" <跑完日志>` ≥ 1。

---

## 三 卡池 diff

**变了 0 张。** 逐卡逐字段比（1127 张，`id` 集合完全相同，字段级差异 **0** 处），顶层字段（含 `version` / `count` / `source` / `note`）也全同；**md5 与改前逐字节相同**。

变的是**过程读数**（不是卡池）：`数值修正 29 张`（原 29 —— 删掉的那条本来就没生效）、新增一行 `数值修正表：所有 29 个键都匹配上了 ✅`、原来那一行 `卡面修正表：所有键都匹配上了 ✅` 照旧。

---

## 四 没查清 / 没做的

### 4·1 🔴 一处**不许我裁**的口径冲突（照做、未选边）

改 `SOR24` 那次是**把修正值写进了源表**（`数据/游戏数据/card_stats.json` 的 `Simulacrum Celestian` 行 `ranged_attack` `null → 4` —— 本笔现核：现值就是 **4**），而

- `工具/gen_cardface_fixes.py:35` 把同一份文件叫「**`card_stats.json`（OCR 原始源表）**」；
- `STAT_FIXES` **其余 29 条键**全都**保持源表原值不动**（修正留在表里）。

⇒ **两条路今天的产物相同**（池里 `Simulacrum Celestian` 的 `ranged` 都是 `4`，`rarity` 都是 `epic`）。**要挑哪条得裁**：

| 路 | 做法 | 代价 |
|---|---|---|
| A 照现况（值进源表） | 源表不再是「OCR 原始值」，`gen_cardface_fixes.py:35` 那句注解要改 | 源表这一格从此与 OCR 原文不同，**不可逆** |
| B 回表里（键改名） | 把 `STAT_FIXES` 的键改成 `Simulacrum Celestian`、并把源表那一格**改回 `null`** | 动源表、动键，**跨两个文件** |

⛔ **本笔按简报只做「删死键 + 加守卫」** —— ⛔ 没动 `card_stats.json`、⛔ 没把键改成现名。

### 4·2 同族但**本笔没做**的守卫（建议另立一条账，不由我裁）

现核（只读普查，脚本见本报告说明）：

| 表 | 查表用的名字 | 有孤立键守卫吗 | 现读死键 |
|---|---|---|---|
| `cardface_fixes`（`desc`/`keywords`/`subtype`/`descZh`） | 改名**后**的 `name` | ✅ 有（`:1930-1936` + `:2152`） | 0 |
| **`STAT_FIXES`** | **改名前**的 `name_before_rename` | ✅ **本笔补上** | 0（本笔删掉 1） |
| **`RARITY_UNLISTED`** | 改名**后**的 `name` | ❌ **没有** | 0（本笔删掉 1） |
| `ZH_NAME_ALIAS` | 改名**后**的 `name` | ❌ 没有 | 0（3 条全活） |
| `ZH_NAME_OVERRIDE` | 改名**后**的 `name` | ❌ 没有 | 0（1 条活） |
| `STATS_EXCEPTIONS` | 源表名 | ❌ 没有 | 0（3 条全中源表） |
| `RARITY_BY_FACTION` | 源表 `(阵营, 名)` | ❌ 没有 | 0 |

⇒ **`RARITY_UNLISTED` 是同一个病**（本笔刚在它身上删掉一条死键、但没有守卫）：键落空时**静默**退到「宝石表查不到 ⇒ `still_missing` ⇒ 稀有度空串」，而那一行 `still_missing` 的打印只是一行 ⚠️、不带卡名。**加一条同形守卫是一行代码**（`[k for k in RARITY_UNLISTED if k not in seen_source_names]`），但**超出本笔范围**，未做。

### 4·3 其它

- 生成器有 `--check` 子档，按简报 **⛔ 没用**（它会顺带做别的事）；本笔是「直接跑 + 自己 diff」。
- 本笔**没跑任何 Unity / 没动 git / 没碰两张正本**（现核 `git diff --numstat` 对 `项目任务.md` / `资料/任务_卡组卡牌战斗.md` / `CLAUDE.md` **无输出**）。

---

## 五 顺手发现

1. 🔴 **本文件的 `build()` 返回元组是「位置对位」的** —— 我第一版把 `orphan_stat_fix` 加在解包表**末尾**，而返回元组里它在 `orphan_face_fix` **之后** ⇒ 生成器当场 `ValueError: not enough values to unpack (expected 4, got 0)`（rc=1、**一个字节都没写**，红线兜住了）。**19 个返回值全无名字**，以后往这里加东西**必须两边同位置改**；这也是「rc 非 0 = 没写」这条红线**当天就兑现了价值**的一次。
2. ℹ️ **`数值修正 N 张` 那一行把两条「改名」记成了「数值修正」**：`Imotekh the Stormlord: name=Imotekh the Stormlord` / `Orikan the Diviner: name=Orikan the Diviner` —— 这两条 `STAT_FIXES` 条目只带 `"name"`，`:1604-1607` 只应用 `cost/attack/health/ranged` ⇒ 它们**一个数值都没改**，却被 `stat_fixed.append` 收进「数值修正」的读数与逐条清单（现读 29 张里有 **2 张**是这种）。**纯读数/文案问题**（`name` 那一步在 `:1565` 已生效），无功能影响，未改。
3. ℹ️ **`Sororitas` 有两张容易混的卡**：`Simulacrum Celestian`（`SOR24`，宝石表 `:698` 紫 epic）与 `Simulacrum Bearer`（`SOR13`，宝石表 `:686` 浅蓝 common）—— 本笔删的那条死键只管前者，已写进 `:210-211` 的注释防止后人删错。
4. ℹ️ **「源表卡名」这个口径有两份实现**：本笔做死键普查时用「`card_stats.json` 的 `name`（空白归一）」；而生成器里实际判重用的是 `(norm_str(faction), name)`（`:1551`）⇒ 同一卡名**跨阵营**时两条都算活。**本笔的普查口径比生成器宽**（只要某个阵营有这名字就算活）—— 结论 0 死键不受影响，但**若将来要写自动化守卫，判据要用生成器那一套**（否则会把「只在一个阵营存在的键」误报成活）。
5. ℹ️ 备份与日志（全部落在 `D:\4\_tmp_view\`，不在工程树里）：`cards_engine.json.bak_r6`（改前池）· `gen_cards_engine.py.bak_r6`（**改前**生成器）· `gen_cards_engine.py.r6_clean`（**改后、注入前**的干净版）· `r6_run1.log`（第一次失败跑）· `r6_run2.log`（干净跑）· `r6_probe.log`（注入跑）· `r6_run3.log`（撤回后跑）· `r6_edit.py`（改写脚本）。
