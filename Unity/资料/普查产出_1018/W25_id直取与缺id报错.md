# W25 · 引擎线 P2：id 层改「按 uniqueId 直取」+ 缺 id 报错跳过 + 生成期断言

> 执行代理产出（2026-10-18）。判据 = `R_引擎线_完整ID与SO后的调整评估.md` §4 第 4、5 行；
> `R_卡牌线_完整ID与SO后的调整评估.md` §4 前两行。
> 改动文件：`Unity/工具/gen_cards_engine.py` · `Unity/MyGame/Assets/RuleEngine/Data/CardDatabase.cs`
> · 重生成的 `Assets/RuleEngine/Resources/cards_engine.json`（只差两条元数据字符串，见 §1.4）。

## 1. A —— `gen_cards_engine.py` 的 id 层改成按 `uniqueId` 直取

### 1.1 新增的四个东西

| 位置 | 作用 |
|---|---|
| `gen_cards_engine.py:874` `load_id_mapping()` | 读 `card_ids.json.mapping`（`{uniqueId: 卡名}`）**原样**返回 |
| `:970` `_is_base_row()` | 基准卡判定：`m_Name` 归一化后以 `cardName` 归一化结尾（与 `工具/gen_card_id_truth_index.py` 的 `is_base` 同一条规则） |
| `:977` `load_direct_ids()` | 建 `(阵营码, 归一化名) → uniqueId` 的**唯一落位表** |
| `:1016` `pick_direct_id()` | 两跳查表：①我方卡名 ②源表 `ocrName`；落不了位返回 `None`（**不猜**） |

**落位表的三个名字来源**（都是真值侧写法）：
① SO `cards_raw.json` 的 `cardName`；② SO `m_Name` 尾段（`re.split('[-_]')[-1]`）；
③ `card_ids.json.mapping` 的值（**54 条**与 ① 写法不同 —— `DA1` = `Azrael` vs SO `Azrael, Grand Master`，
正好补上「SO 名 ≠ 我们的名」那批）。

**消歧**：同一 `(阵营, 名)` 落位到多个 `uniqueId` 时用**基准卡**消歧；仍不唯一 ⇒ 该键**不收**，
记进 `ambiguous` 由摘要报出（现读 28 个键，全是 `enchantment*` / `oath` 这种副本名，池内卡不用它们）。

### 1.2 主循环（`:1901` 起）改成三段

```text
① cid = pick_direct_id(...)                  ← 唯一身份判据
② cid_cascade = pick_truth_id(...) or pick_id(...)   ← 名级联，只作交叉校验
③ 直取命中：用直取；若级联给出**不同的 id** ⇒ id_cross_mismatch（摘要逐条打出来）
   直取没落位：退回级联（id_cascade，**并出声**）；级联也挑不出才 make_id（id_made）
```

### 1.3 现读读数（2026-10-18 全量重跑）

```text
直取 id 表    (阵营, 卡名) → uniqueId 唯一落位 1342 键（真值 1351 行 · 落位不唯一、不收 28 键）
卡 id        直取（uniqueId 唯一落位表）1127 张 · 名级联兜底 0 张 · 自造 0 张（共 1127）
             交叉校验：1127 张直取里与名级联**不一致 0 处** ✅
```

- 与判据文件的「1125/1127 可唯一」**多出来的 2 张** = `SAU35 Imotekh the Stormlord` / `SAU37 Orikan the Diviner`：
  源表里它们的名字是短名（`stormlord` / `Diviner`，由 `STAT_FIXES` 改名），但这两行的 **`ocrName` 是全名**
  ⇒ `pick_direct_id` 第二跳查 `ocrName` 就落得上了 ⇒ **1127/1127**。
- 直取结果与旧路（真值索引级联）**逐张一致**（`id_cross_mismatch = 0`）。

### 1.4 重生成输出 vs 改前 —— **逐字节比对结果**

- **`cards` 数组：完全相同**（1127 条、**每一条的每个字段**都相等；`version` / `count` 也相等）⇒ **id 分配零变化**。
- 文件整体**不是逐字节相同**，差异只有两条**元数据字符串**（都是我按评估表/铁律 5 有意更新的）：
  1. `source`（1222 → 1405 字符）：`"id 另取 数据/游戏数据/card_ids.json，对不上的自造；"`
     → 新的「按原版 `uniqueId` 直取 … 名级联只作交叉校验」；
  2. `note`（2281 → 2367 字符）：`"自造 id 形如 AM_Some_Card …"` → 新的「`W25` 起按 `uniqueId` 直取、
     现读 1127/1127 真 id、自造 0；`AM_Some_Card` 只在判据缺失时才会出现，且生成器末尾有硬断言」。
     （这两处正是评估表点名的「`:2301` note 已过期」那条。）
- 连续两次全量重跑**字节相同**（确定性）；`--check` 返回 **0**（`一致 ✅`）。

## 2. B —— `RuleEngine/Data/CardDatabase.cs`

| 行 | 改动 |
|---|---|
| `:32-35` | 过期注释订正（铁律 5）：删掉「id 可能是 `AM_Some_Card` 自造」→ 写明**池内 1127/1127 真 id、自造 0**，身份 = 原版 `uniqueId` |
| `:284-297` | **缺 id ⇒ `Debug.LogError`（写明「卡 X（阵营）缺 id —— 已跳过该卡」）+ `noId++` + `continue`**；⛔ 不再 `cid = c.name`，**卡名不再是身份** |
| `:334-336` | 收尾汇总从 `LogWarning` 改 `LogError`：`有 N 张卡没有 id，已跳过（卡名不再当身份）` |
| `:230-233` | v<6 旧表的提示同步改成「**整份卡表都会被跳过**」（旧文字还写着「Id 退回卡名」，已不成立） |

副作用（如实记）：**卡表 version < 6 时得到的是空池**（每张卡各报一条错）——这是有意的「不许静默失败」，
但确实是行为变化，若将来要读旧表需先跑生成器。

## 3. C —— 生成期自检（`gen_cards_engine.py:2458-2484`，`build()` 末尾、`return` 之前）

两条硬断言，任一不满足 ⇒ 打印逐条明细 + `sys.exit(❌…)`（**非零 = 一个字节都不写**，与 `A1626` 同口径）：

1. 池内 `id_made == 0`；
2. 池内每个 `id ∈ set(truth_cards)`（真值 SO `cards_raw.json` 的 `uniqueId` 集合）。
   另：SO 读不到（集合为空）也算失败 —— **判据缺失不等于通过**。

**实跑验证两条都炸得出来**（都是只读的 `--check`，没写任何文件）：
- 把 `TRUTH_CARDS_SRC` 指向不存在的路径 ⇒ `SystemExit`、**进程退出码 = 1**；
- 把直取/级联都打成返回 `None` ⇒ 报「池内有 1127 张自造 id：ASH_Autarch=… 其余 1117 张」并退出。

## 4. 验证与「没查清」

- `TMPDIR=/tmp/wf_f29 bash d:/4/Unity/工具/typecheck.sh` → **运行时错误数 0 · 编辑器错误数 0**。
- `.py` 全部用 `python -I -X utf8`（`D:/2/Warpforge_tools/py312/python.exe`）跑；未跑 Unity / `*.Run`。
- **没查清**：
  1. 判据文件说的「1125/1127 可唯一」用的具体 join 口径未写死；本实现做到 **1127/1127**
     （多了 `ocrName` 第二跳），两者不矛盾，但**我无法复现出「恰好 1125」那条算式**。
  2. 28 个「落位不唯一、不收」的键是否真的全部是副本（`enchantment*` / `oath`）：只看了名字形态，未逐条解 SO。
  3. `cards_engine.json` 里 `source` / `note` 两串变了（见 §1.4）。若要求**字节级完全不变**，
     把生成器里那两处字符串改回去重跑即可；`cards` 数据本身与该文件改前完全一致。
