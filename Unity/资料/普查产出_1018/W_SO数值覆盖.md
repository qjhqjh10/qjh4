# W · SO 数值覆盖（卡表 `health` / `cost` / `attack`）

> 判据：`资料/普查产出_1018/R_督军10血成因.md` §1 **结论 A —— 当前 build 的 SO 就是真值**
> （2026 build 把督军生命统一下调，`allcards_1350/报告.md:101`：「这不是我们抄错，是版本变化」）。
> 数据源：`资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 行卡 SO，键 = `uniqueId`）。

---

## 1 覆盖条数 + 卡数校验

| 字段 | SO 源字段 | 覆盖条数 |
|---|---|---|
| `health` | `maxHealth` | **49**（47 张督军 + `BL71` + `SW13`） |
| `cost` | `manaCost`（仅 `>= 0`） | **6** |
| `attack` | `meleeAttack` | **1**（`TAU12 Darkstrider` 3 → 2） |
| **合计** | | **56 处** |

- 与 `R_督军10血成因.md` §3 的 **60 项**对账：**56 项已覆盖 + 4 项按判据保留**（见 §4，均为 `manaCost = -1` 哨兵）＝ 60。
- 9 张「已与 SO 相等」的督军（`ASH76 / DA75 / EC55 / EC56 / EC57 / SW1 / SW2 / SW3 / UM88`）**一处都没动**（不在 diff 里）。
- 卡数：**1127 → 1127** ✅
- `id`：**顺序逐字相同**（`ida == idb`）✅，键集合相同，**无增删**。
- schema：`version` 仍为 **9**（没有加列，只是换值）✅
- before/after **全表逐字段 diff**：只有 `health` 49 处 / `cost` 6 处 / `attack` 1 处；
  **`ranged` / `desc` / `keywords` / `art` / 其它任何列 = 0 处变化** ✅
- `数据/游戏数据/card_stats.json`（PnP 印刷记录）**一个字节都没写**。

---

## 2 字段口径（2026-10-18 现核，不是猜）

以 1127 张「池 id 能在 SO 里按 `uniqueId` 找到」的卡逐张比对：

| 我们的列 | SO 字段 | 相等数 | 结论 |
|---|---|---|---|
| `attack` | `meleeAttack` | **1126 / 1127** | ✅ `attack` = **近战攻击**；唯一例外就是本轮要改的 `TAU12`（3 vs 2） |
| `attack` | `rangeAttack` | 654 / 1127 | ❌ 不是它 |
| `ranged` | `rangeAttack` | —— | 本层**不动**（任务口径只管三列） |
| `health` | `maxHealth` | 1078 / 1127 | ✅ 差 49 项 |
| `cost` | `manaCost` | 1117 / 1127 | ✅ 差 10 项（其中 4 项是 `-1` 哨兵，见 §4） |

⇒ 生成器注释里写死：`health ← maxHealth` · `attack ← meleeAttack` · `cost ← manaCost`。

---

## 3 逐卡改前 → 改后（56 项）

| # | id | 卡名 | 类型 | 字段 | 改前 | 改后 |
|---|---|---|---|---|---|---|
| 1 | `AM1` | Lord Solar Leontus | hero | `health` | 40 | 30 |
| 2 | `AM2` | Ursula Creed | hero | `health` | 35 | 25 |
| 3 | `AM3` | Commissar Denkler | hero | `health` | 30 | 25 |
| 4 | `AM66` | Hektor Thenmann | hero | `health` | 35 | 25 |
| 5 | `ASH1` | Anvirr Keltoc | hero | `health` | 35 | 25 |
| 6 | `ASH2` | Eliac Zephyrblade | hero | `health` | 40 | 30 |
| 7 | `ASH3` | Medreyal Ghaelyn | hero | `health` | 35 | 25 |
| 8 | `ASH74` | Jain Zar | hero | `health` | 35 | 25 |
| 9 | `BL34` | Abaddon the Despoiler | hero | `health` | 40 | 30 |
| 10 | `BL35` | Haarken Worldclaimer | hero | `health` | 35 | 25 |
| 11 | `BL36` | Sylar Hexscorn | hero | `health` | 35 | 25 |
| 12 | `BL65` | Ghallaron the Pious | hero | `health` | 35 | 25 |
| 13 | `BL71` | Havoc Champion | unit | `health` | 7 | 8 |
| 14 | `DA1` | Azrael | hero | `health` | 40 | 30 |
| 15 | `DA2` | Belial | hero | `health` | 35 | 25 |
| 16 | `DA3` | Asmodai | hero | `health` | 35 | 25 |
| 17 | `GOF27` | Boss Zagstruk | hero | `health` | 35 | 25 |
| 18 | `GOF28` | Ghazghkull Thraka | hero | `health` | 40 | 30 |
| 19 | `GOF29` | Grukk Face-Rippa | hero | `health` | 35 | 25 |
| 20 | `GOF75` | Warboss Gordrang | hero | `health` | 35 | 25 |
| 21 | `GOF76` | Mozrog Skragbad | hero | `health` | 40 | 30 |
| 22 | `GOF77` | Beastboss Morgrim | hero | `health` | 35 | 25 |
| 23 | `GSC1` | Primus Saffa Rhiannor | hero | `health` | 35 | 25 |
| 24 | `GSC2` | Magus Uthrel Naas | hero | `health` | 35 | 25 |
| 25 | `GSC3` | Acolyte Iconward | hero | `health` | 35 | 25 |
| 26 | `GSC66` | Lhaska Szenari | hero | `health` | 35 | 25 |
| 27 | `SAU35` | Imotekh the Stormlord | hero | `health` | 40 | 30 |
| 28 | `SAU36` | Nemesor Zahndrekh | hero | `health` | 35 | 25 |
| 29 | `SAU37` | Orikan the Diviner | hero | `health` | 35 | 25 |
| 30 | `SAU66` | Ramatekh The Cruel | hero | `health` | 35 | 25 |
| 31 | `SOR1` | Erika Luminas | hero | `health` | 35 | 25 |
| 32 | `SOR2` | Junith Eruita | hero | `health` | 35 | 25 |
| 33 | `SOR3` | Morvenn Vahl | hero | `health` | 40 | 30 |
| 34 | `SOR65` | Celestian Sacresant Aveline | hero | `health` | 35 | 25 |
| 35 | `SW13` | Wolf Guard Headtaker | unit | `health` | 4 | 3 |
| 36 | `TAU1` | Shadowsun | hero | `health` | 40 | 30 |
| 37 | `TAU2` | Aun'Va | hero | `health` | 35 | 25 |
| 38 | `TAU3` | Commander O'Maisos | hero | `health` | 35 | 25 |
| 39 | `TAU65` | War Shaper | hero | `health` | 30 | 20 |
| 40 | `TL34` | Tervigon | hero | `health` | 40 | 30 |
| 41 | `TL35` | SwarmLord | hero | `health` | 40 | 25 |
| 42 | `TL36` | Neurothrope | hero | `health` | 35 | 25 |
| 43 | `TL74` | Terror of Vardenghast | hero | `health` | 35 | 25 |
| 44 | `UM46` | Marneus Calgar | hero | `health` | 40 | 30 |
| 45 | `UM47` | Uriel Ventris | hero | `health` | 35 | 25 |
| 46 | `UM48` | Varro Tigurius | hero | `health` | 35 | 25 |
| 47 | `UM65` | Chaplain Letharius | hero | `health` | 35 | 25 |
| 48 | `UM76` | Lieutenant Titus | hero | `health` | 35 | 25 |
| 49 | `UM87` | Roboute Guilliman | hero | `health` | 40 | 30 |
| 50 | `453b0e8f88e094a17866a6979f389ef5` | Infernal Gaze | tactic | `cost` | 2 | 1 |
| 51 | `BL74` | Spreading Corruption | tactic | `cost` | 4 | 3 |
| 52 | `TAU63` | Tidewall Gunrig | defence | `cost` | 0 | 1 |
| 53 | `TL66` | Neurogaunt | unit | `cost` | 2 | 1 |
| 54 | `UM32` | Tactical Insight | tactic | `cost` | 0 | 1 |
| 55 | `a58e05916eb5e994daa0349cbe237001` | Synaptic Imperative | tactic | `cost` | 2 | 1 |
| 56 | `TAU12` | Darkstrider | unit | `attack` | 3 | 2 |

> 生成器跑完的摘要原话：
> `SO 覆盖：health **49** 张 / cost **6** 张 / attack **1** 张（合计改 **56** 处）`
> `源 = cards_raw.json 的 maxHealth / manaCost（仅 >=0）/ meleeAttack；判据 → 资料/普查产出_1018/R_督军10血成因.md §1 结论 A`

---

## 4 没敢覆盖的 4 项（`manaCost = -1` 哨兵）与理由

| id | 我们的卡 | 类型 | 我们 cost | SO `manaCost` | SO 行名 |
|---|---|---|---|---|---|
| `4a48457c24877f345be2e96f867c1e94` | Dark Pact of Fate | tactic | 1 | **-1** | `Mark of Tzeentch (enchantment)` |
| `9a2cadc64c80a6b42912441105bb1cee` | Dark Pact of Blood | tactic | 1 | **-1** | `Mark of Khorne (enchantment)` |
| `b8e81b2fe85cda747a68c4d6326b669c` | Dark Pact of Excess | tactic | 1 | **-1** | `Mark of Slaanesh (enchantment)` |
| `1dfc9bc738bfc4f4d93d48f165aee510` | Dark Pact of Resilience | tactic | 1 | **-1** | `Mark of Nurgle (enchantment)` |

**不覆盖的三条理由（都现核过）：**

1. `manaCost = -1` 是原版「**不可直接打出**」的**哨兵值**，不是费用。全快照 1,351 行里 105 行是 `-1`；
   落进我们 1127 张池子的只有这 4 张。现表**没有任何一张卡**用负 `cost` 表达「不可打出」
   （实测 `cost` 分布：0…14，负数 0 张）⇒ 直接写 `-1` 等于给引擎引入一个它不认识的档位。
2. 这 4 个 id 在 SO 里对应的**不是我们的实体卡**：`cardName` 是 `Mark of Xxx (enchantment)`、
   `cardType = 20`、`spellType = 190`（附魔模板，`cardAbilities.trigger = 500`）；
   我们的卡是 PnP 印刷版 `Dark Pact of Fate/Blood/Excess/Resilience`（`card_stats.json` 里 type = tactic，
   印刷费用 1；`Dark Pact of Fate` 的 OCR 费用是 null，我们按同族补成 1）。
   2022 时代快照（`cards_1195_2022时代.json`）里同一 id 也已经是 `Mark of Tzeentch (enchantment)`、`manaCost = -1`
   ⇒ 这不是快照换代造成的，**是这条 id 一直指的就是附魔模板**。
3. 按「**判不准就保持原值**」的任务口径 ⇒ 保持 `cost = 1`，并在生成器里**逐条出声**
   （`so_stat_skipped` → 摘要里打印 id / 卡名 / 我们值 / SO 值 / SO 行名）。
   ⚠️ 若后续裁定「这 4 张要按数字版的附魔模板处理（不可直接打出）」，那是**另开一档 cost 语义**，
   本笔不动。

---

## 5 生成器改动（`工具/gen_cards_engine.py`）

1. **新增覆盖层**（`build()` 里，位置 = 督军 `cost` 那一段之后、`keywords` 之前）：
   - 只在 `trow is not None`（按 `uniqueId` 命中 SO）时生效；
   - `health ← maxHealth`、`attack ← meleeAttack`、非 hero 的 `cost ← manaCost`（**仅 `>= 0`**）；
   - **督军 `cost` 跳过**：上面 `A1773` 那一段已经从同一个 `manaCost` 取过值，避免两个记账口重复；
   - 负值 / 缺值 → 记 `so_stat_skipped`，**保持原值并出声**。
2. **台账**：`so_stat_changed`（每处 `id / 卡名 / 类型 / 字段 / 改前 / 改后`）+ `so_stat_skipped`
   （多带 SO 行名，用来一眼看出「不是同一张卡」）；经 `build()` 的 truth 元组返回、`main()` 打印
   （格式见 §3 末尾）。
3. **就地订正三处过期注释（铁律 5）**：
   - `TRUTH_CARDS_SRC` 那段「三围/费用一律保留我们的值」→ 补注「只适用于 2022 快照；当前 build 的 SO
     已被裁定为真值，三列改按 SO 覆盖」；
   - 输出 JSON 的 `source` 字段（原来还写着旧快照路径 `cards_1195_2022时代.json`）→ 改成当前 build
     `allcards_1350/cards_raw.json`，并写明三列真值口径；
   - `main()` 里「三围与护甲一律没动、13/13 我们 == PnP」那一行 → 换成新的「SO 数值覆盖」读数段。

---

## 6 `RuleEngine/Core/GameplayVariables.cs`（**只改注释，不改逻辑**）

在 `warlordLifeChange` 的 doc 里补了一段：

> **2026-10-18：SO 是当前数字 build 的基准；经典 0 / 冲突 −10 是运行时全局增量**
> （判据 → `R_督军10血成因.md` §2）。卡表里那 49 项 `health` 差异是 2026 build 把 SO 统一下调
> （35→25 ×39 · 40→30 ×11 · 30→20 · 35→30），**不是**本字段造成的：原版
> `BattleManager__SetupWarlordsLifeAndEnchantment.c:29,53,72` 只在 `warlordLifeChange != 0` 时走
> `ForceChangeMaxHealth(督军, 当前最大生命 + warlordLifeChange)` —— 每局敌我**同加同减**，
> 产生不出逐卡 −5（`AM3`）/ −15（`TL35`）/ 0（9 张）这种差异。
> ⇒ ⛔ 别拿本字段去解释单张卡的 `health`。

---

## 7 校验

### 7·1 卡表结构

```
count before/after 1127 1127
ids identical sequence: True set equal: True
changed field counts: {'health': 49, 'cost': 6, 'attack': 1}
keys added/removed: set() set()
version 9 count 1127 cards 1127
```

### 7·2 秒级类型检查（`TMPDIR=/tmp/wf_f7 bash d:/4/Unity/工具/typecheck.sh`）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（rc=0）

### 7·3 生成器写法

```
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_cards_engine.py
```

- `--check` 先跑过一遍：确认只有这 56 处会变，再正式落盘。
- 落盘后又原样重跑一次 ⇒ 输出**逐字节相同**（`cmp` = JSON_IDENTICAL），生成器幂等。
- 落盘前备份：`_tmp_view/w_so_1018/cards_engine.before.json`。
- 输出文件：`Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（730 KB）。
  ⚠️ `Unity/数据/游戏数据/cards_engine.json` **不存在**（全仓 `find` 只有 `Resources/` 这一份 + 三份旧备份），
  所以任务里说的「同源那一份」无需同步。
