# R · 关账复核与 224 张「包内多出卡」（只读 · 2026-10-18）

> 只读代理产出（无写文件工具 ⇒ 主对话原样落盘）。
> 现读：`allcards_1350/cards_raw.json`(1351) · `数据/游戏数据/card_ids.json`(mapping 1351 / poolTrueIds 1127 / poolIdsAreTrue=true) ·
> `cards_engine.json`(1127) · `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`(旧快照 1195/1194 uid)。

## 1. 「已关账」逐条复核

| 编号 | 判定 | 现读证据 |
|---|---|---|
| `A2075` 普通卡 id 拿不到 | **① 解决** | SO 1351 行：`uniqueId` 非空 **1351/1351**、重复 **0**；池 1127 全 ∈ SO（**0 自造**） |
| `A2072` 118 张 id 自己排 | **① 解决** | `poolTrueIds` 1127 = 1013 不变 + **114 改**；114 个新 id 全在 SO，逐张 `(cost,atk,rng,hp)` 与 SO **114/114 完全相等**；「4 张确认」= `DA75`/`SW1`/`SW2`/`SW3` |
| `A1869` 171→118 接不上真值 | **① 解决** | 池 **1127/1127 ∈ 当前包**；不在旧快照的 **118** 张全部在 SO，四围 **118/118 精确相等** |
| 「20 个自造残留短 id 键」 | **① 解决** | `mapping` 1351 键 == SO 1351 id（**双向差 0**）、0 个含 `_` 的键；原 21 个串只在 `LegacyCardIds.cs` 作**旧代迁移键**（有意保留） |
| `A2032` 28 张同名多候选 | **① 解决（数变 31）** | 现读同名组 **31**；每组恰好 1 个基准形态（`inventoryOptions=5 ∧ spellType=200 ∧ 无 _enchantment`）+ 1~2 附魔副本，池内命中 0 ⇒ join 需 **uniqueId + 基准形态判别** |
| `A2050` 按英文名找键 | **② 一半** | 数据侧：池 **1126/1127** 直接命中 `Card_Name/<id>`（剩 `AM24` 官方无该键）；按 `by_en` 桥接 **0/1127**。机制侧：`gen_cards_engine.py` 的 `official_name()` **仍有 `by_en` 回退、未加闸**（将来会复发） |
| `A2037③` 117 张不在真值快照 | **① 解决** | 现读 **118**（同 A1869 那批）全部 ∈ SO，四围 118/118 相等 |
| `A2056` 预组 `complete:false` | **① 解决** | `prebuilt_decks.json` **103** 副、**103/103 complete**、`missing`/`problems` 全空、引用 **0 个池外 id** |

## 2. 224 张多出卡 = 什么

**类型统计仍成立**：计策 195 / 单位 19 / 英雄 10。

| cardType | 池 | SO | 多出 | 其中变体 | 非变体 |
|---|---:|---:|---:|---:|---:|
| unit | 586 | 605 | 19 | 19 | 0 |
| tactic | 446 | 641 | 195 | 143 | **52** |
| hero | 56 | 66 | 10 | 10 | 0 |
| defence | 39 | 39 | 0 | 0 | 0 |
| **合计** | **1127** | **1351** | **224** | **172** | **52** |

**172 张变体**：附魔 `_enchantment/_enchanatment` **120**（70 张基卡在池 + 35 张基卡是进攻卡 + 15 张武器附魔/别名副本）·
教程 `_tutorial` **22** · 战役 `_oath` **10** + `_normal` **10** · `_EASY` **6** · `_startinghand` **2** · `_withVanguard` **1** · `REMOVED_` **1**。

**52 张非变体**：**39 张进攻卡**（`spellType=200 ∧ inv=5`，正好对应 `数据/游戏数据/offensive_cards.json` 的 39 槽 = 3×13 阵营）
+ **13 张 `Normal Conditions (各阵营)`**（`inv=10`，即「不使用进攻卡」的空进攻卡）。

**SO 字段证据**：224/224 `showInCollectionOptions=0`；`inventoryOptions` = 185×`10(NotInInventory)` + 39×`5(CantAddToDeck)`。
⇒ **没有一张是可收集卡**。

**结论**：「224 张都是没 ID 时代的产物」这个**因果判断不成立**（它们是当前 build 客观存在的非可收集形态/进攻线）；
但用户的**操作性判断成立** —— **需进主池的新卡 = 0**，**这条应关账**。
（另：`A2032` 的 31 个同名组正是这批里的进攻卡 = base + 附魔副本。）

## 3. 建议关账原文

> **224 张「包内多出卡」（计策 195 / 单位 19 / 英雄 10）** —— 已复核（2026-10-18）：224 = **172 张变体**
> （附魔 120 · 教程 22 · 战役 oath/normal 20 · EASY 6 · startinghand 2 · withVanguard 1 · REMOVED 1）
> + **52 张非变体**（39 张进攻卡，已在 `offensive_cards.json` 的 39 槽 + 13 张「Normal Conditions」空进攻卡）；
> 224/224 `showInCollectionOptions=0`、`inventoryOptions∈{5,10}` ⇒ **需进主池的新卡 = 0，关账**。

## 4. 需进池名单
**无**。

## 5. 没查清
1. 39 张进攻卡有 **4 个名字不一致**：SO = `Aural Hijack` / `Stimm-Vents Leak` / `Shrine Bombardment` / `Immortal Beams`，
   `offensive_cards.json` = `1 Green` / `2 Fumes` / `Shrine Bombardement` / `Ethereal Energy`（后两个在 SO 里根本不存在）。哪边是卡面准名未核。
2. 15 张「其它附魔」（Carnifex 武器附魔等）只按 `inv=10` + `m_Name` 判非收藏；未核引擎是否会按 id 实例化它们。
3. `AM24` 的官方 `Card_Name` 缺条目（源值 `"u"` 被丢），`nameZh` 走英文原文，未核是否需补官方名。
4. 13 张 `Normal Conditions` 与 `offensive_cards.json` 的 `emptyOffensiveCard` 是 PPtr 对应，未逐阵营核 PPtr。
5. `LegacyCardIds.cs` 的 73 个「旧 id 同时是现池另一张卡的 id」仍靠双解闸门兜；未跑运行期双解用例。
