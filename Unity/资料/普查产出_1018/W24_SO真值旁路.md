# W24 · SO 真值旁路（T12 · 引擎线 P0）报告

日期 2026-10-18 · 只读生成器 · ⛔ 未改任何 `.cs` / `cards_engine.json` / `card_stats.json` / 正本 / git / 未跑 Unity

## 1. 产物

| 路径 | 字节 | 排版 |
|---|---|---|
| `Unity/工具/gen_cards_truth.py`（新） | — | 生成器，`python -I -X utf8` 可跑，带 `--check` |
| `Unity/数据/游戏数据/cards_truth.json` | 2,194,975 | 人读版 `indent=1` |
| `Unity/MyGame/Assets/RuleEngine/Resources/cards_truth.json` | 1,416,080 | 运行期紧凑（无空格） |

- 条数：**1,351** 张（= `cards_raw.json` 全量）。形状 `{"cards":[…]}` 数组（`JsonUtility` 不吃字典）。
- 两份**同一次运行产出**，`json.loads` 后完全相等（只有缩进不同）；`--check` 复跑两份均「一致 ✅」。
- 头字段：`source`（路径 + SHA-256 + mtime + bundle）· `count` · `provenance` · `invariant` · `defaults` · `stats` · `idDiff` · `missing`。
- ⚠️ 未写 `.meta`（在白名单外）⇒ 下次 Unity 导入时自动生成。

## 2. 字段覆盖率（非空 / 非默认）

| 字段 | 覆盖 |
|---|---|
| `id` | 1351/1351，0 重复 |
| `abilities`（`cardAbilities`） | **1205 张** / 1496 条；`logic`（`abilityLogic`）**1873** 条 |
| `activeAbility` | **143 张**（其中 `hasActiveAbility==1` 只有 **88** 张） |
| `hasAlternativeAbility=1` | **1 张**（`SW21`，已落 `alternativeAbility`） |
| `cardTraits` | **750 张** / 1138 条（`ephemeral`125 · `rally`108 · `talent`105 …） |
| `maxTargets > 0` | **317 张**（分布 291/10/13/1/2 —— 与 `R_卡牌线…评估.md` §2 的 317 一致） |
| `attackScoringCriteria` | 1351/1351（⚠️ 全卡是**同一份模板**，见 §4） |
| `cardTargetScoringCriteria` | **367 张** / 373 条 |
| `nonTargetScoringCriteria` | **387 张** / 390 条 |
| `useOverDraw` | 1351/1351（`1`=**677**、`0`=674） |
| `rarity` | 1351/1351（0:4 · 1:358 · 2:353 · 3:261 · 4:234 · 5:141） |
| `keyword`（兵种枚举） | 非 0 **613**（5:416 · 10:137 · 15:21 · 25:8 · 30:7 · 35:17 · 40:7） |
| `cardType` | 1351/1351（0:605 · 10:66 · 20:680） |
| logic 里的 `damage` | 510 / 1873 条 |
| logic 里的 `heal` | 98 |
| logic 里的 `buff` | 1077 |
| logic 里的 `summon` | 375（引用 430 条：**428 解成卡 id**、2 条 `m_PathID=0` 空槽、**0 条解不出**） |
| logic 里的 `handBuff` | 64 |
| logic 里的 `manaBuff` | 128 |

`missing`：**空**（要求的字段全部读到；`useOverDraw`/`targeted`/三个评分判据只在 `full_raw` 里，外层 31 键没有它们 —— 已写进 `provenance`）。

## 3. id 集合差集

- **`card_ids.json.mapping`（1351）↔ truth（1351）：两边差集均 0** ✅（脚本每次运行自检并打印）。
- **`cards_engine.json` 池（1127）↔ truth：`poolOnly = 0`**；`truthOnly = 224` = 池外原始卡（SO 里的非可收集/异形态）。
- `idDiff.counts` = `{truth:1351, cardIdsMapping:1351, poolCards:1127}`。

## 4. 没查清 / 需要下游注意

1. **`attackScoringCriteria` 1,351 张逐字节相同**（同一份全 0 模板）⇒ 真正的逐卡攻击评分不在它里面；`specialAttackScoringCriteria` 全 0。
   T06 该吃哪一档（`cardTarget*`/`nonTarget*` 列表？）**未定**，本轮只落真值不推导。
2. **`hasActiveAbility` 与 `activeAbilityLogic` 不一致：55 张**（逻辑非空、flag=0；其中 **50 张 `cardType=10` = 督军**，5 张 `cardType=0`）。
   例：`ASH1 Anvirr Keltoc`、`SW4 Blood Claw`（flag=0 也有 1 条逻辑）。生成器**两个都落**（`hasActiveAbility` 原值 + `activeAbility` 实体），不替下游裁决。
3. `summon.criteria.unitsToSummon` **保序不去重**（实测 `Raging Storm` 是 `["GOF1","GOF1"]` = 召两只 ⇒ 去重会静默改语义）。
4. `CardTraits._trait` / `chosenAbility` / `trigger` 等枚举**未映射名字**（语义层的事，⛔ 不在本任务）。
5. 卡级 `maxTargets` = `max(abilityLogic[].targetCriteria.maxTargets, cardAbilities[].targetCriteria.maxTargets)`；
   前者 475 条非 0、后者只有 2 条非 0 —— 口径已写进产物 `provenance`。

## 5. 复现 / 自检命令

```bash
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_cards_truth.py
"D:/2/Warpforge_tools/py312/python.exe" -I -X utf8 d:/4/Unity/工具/gen_cards_truth.py --check
```

自检（本次实跑，全过）：① id 唯一 + 与 `card_ids.mapping` 相等；② 两份产物内容等价；③ 200 张随机卡逐字段回比源 SO（name/army/四围/rarity/keyword/useOverDraw/trigger/chosenAbility/damage/summon）**0 处不一致**；④ 产物内 `null` 值 **0 个**。
