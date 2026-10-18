# W · `Dark Pact of *` 无费用（4 张附魔模板）

- 日期：2026-10-19
- 用户裁定：`Dark Pact of Fate / Blood / Excess / Resilience` 是**战术卡触发生成的契约 / 附魔模板**，**没有费用**。
- 结论：数据 `cost = -1`（原版 `manaCost = -1` 哨兵）· 卡面不画费用格 · 引擎显式拒绝「直接打出」。
- 归属：卡牌线（④ 列）。⛔ 未跑 Unity / `*.Run` / player；只跑秒级 typecheck（0/0）。

---

## §一 身份证据（它们是什么、谁生成的）

### 1.1 SO 行：就是我们池里这 4 张（`uniqueId` 直配）

来源 = `资料/普查产出_1018/allcards_1350/cards_raw.json`（当前 build · 1,351 张卡 SO）：

| 我们池里的卡 | `id` | SO `cardName` | `m_Name` | `manaCost` | `cardType` | `spellType` |
|---|---|---|---|---|---|---|
| Dark Pact of Fate | `4a48457c24877f345be2e96f867c1e94` | Mark of Tzeentch (enchantment) | `Z-NC BlackLegion-Mark of Tzeentch_enchantment` | **-1** | 20 (Tactic) | 190 |
| Dark Pact of Blood | `9a2cadc64c80a6b42912441105bb1cee` | Mark of Khorne (enchantment) | `Z-NC BlackLegion-Mark of Khorne_enchantment` | **-1** | 20 | 190 |
| Dark Pact of Excess | `b8e81b2fe85cda747a68c4d6326b669c` | Mark of Slaanesh (enchantment) | `Z-NC BlackLegion-Mark of Slaanesh_enchantment` | **-1** | 20 | 190 |
| Dark Pact of Resilience | `1dfc9bc738bfc4f4d93d48f165aee510` | Mark of Nurgle (enchantment) | `Z-NC BlackLegion-Mark of Nurgle_enchantment` | **-1** | 20 | 190 |

- 全快照 `manaCost == -1` 共 **105** 行；**按 `uniqueId` 落进我们池的只有这 4 行**（其余 101 行不在池）。
  ⚠️ 按**卡名**查会误伤：`Hyper-adaptation` / `Infinite Biomorphologies` 各有一批同名 `-1` 副本行，
  但我们池里那两张的 id 对应的是 `manaCost = 2` 的真行 ⇒ **必须按 id 判，不能按名字判**。
- `cardType = 20` = `CardTypeOptions.Tactic`；`spellType = 190` = 附魔标记（与 `Mark of *` 附魔行一致）。

### 1.2 生成链（反编译 + 数据 SO）

1. **`Enchantments` SO**（`d:/2/新解包资源/assets_full/bundle_duplicateassetisolationso_assets_all/MonoBehaviour/Enchantments.json`）
   的 `markOfChaos` 映射 —— 这是「哪张附魔卡对哪个 mark 类型」的**唯一数据正本**：

   | `cardEffect` | 映射到的 `targetId` | 我们池里的卡 |
   |---|---|---|
   | 10（Khorne） | `9a2cadc64c80a6b42912441105bb1cee` | Dark Pact of Blood |
   | 20（Nurgle） | `1dfc9bc738bfc4f4d93d48f165aee510` | Dark Pact of Resilience |
   | 30（Slaanesh） | `b8e81b2fe85cda747a68c4d6326b669c` | Dark Pact of Excess |
   | 40（Tzeentch） | `4a48457c24877f345be2e96f867c1e94` | Dark Pact of Fate |
   | 50（Chaos Undivided / 随机四选一） | 上面 4 个全在列表里 | 4 张都可能 |

2. **`CardScript.AddEffect` 的 case 8** —— `d:/2/tools/decomp_full/CardScript__AddEffect.c:776-792`：
   `:780-782` 取 `BattleManager` 的 `Enchantments`，用 `*(int)(effect + 0x78)`（= `buffEffect.markOfChaosType`）
   调 `Enchantments.GetMarkOfChaos`；`:784-786` `RawCardScript__TriggerEnchantment(...)` 把它作为附魔挂上去；
   `:788-792` 记到 `battleManager + 0x2f0` 并 `BroadcastMarkResolved`。
   ⇒ **它只被「挂成附魔」，从来没有走「出牌」那条路** —— 这就是「不可直接打出」的语义来源。
3. **`Enchantments.GetMarkOfChaos`** —— `d:/2/tools/decomp_full/Enchantments__GetMarkOfChaos.c:18-35`：
   遍历 `Enchantments` 的列表、按 `cardEffect` 枚举 `TryGetCard`；`TryGetCard` 里用
   `BattleManager__GetRandomInt(0, list.Count)` 随机取一张（来源
   `Enchantments.CardEffectToCard_Int32Enum___TryGetCard.c:43-63`）。
   ⇒ `50`（四张都在列表里）时就是**四选一随机**。
4. `GiveMarkOfChaos__get_Army.c:5` 返回 `0x32` = **50**（默认 mark 类型 = Chaos Undivided / 随机四选一）。

### 1.3 「哪张战术卡」——不是单张，是一族

`cards_raw.json` 里带 `markOfChaosType != 0` 的卡共 36 张。典型生成者（按类型分）：

- **战术卡**：`BL46 Gifts of Chaos`（50，随机四选一）· `EC36 Chosen of Slaanesh`（30）· `EC41 Internal Rivalry`（30）·
  `d0549600… Pledge to the Dark Prince`（30）· `BL37 Abaddon's Chosen` / `BL38 Dark Oratory` /
  `141c5ed6… Unhallowed Gifts` 等（这些是把 mark 挂给友军的战术）。
- **单位卡**：`BL6 Rubric Marine`（40 → Fate）· `BL7 Khorne Berzerker`（10 → Blood）·
  `BL8 Noise Marine`（30 → Excess）· `BL10 Plague Marine`（20 → Resilience）· `BL16 Possessed Champion`（50）等。

⇒ **没有「唯一一张战术卡」**：这 4 张是 `Enchantments` SO 里被整族 `markOfChaos` 效果共享的**模板**。
用户裁定（「没有费用」）不受这一点影响 —— 模板本来就不进手牌、不参与出牌。
**这一格如实写「不是单张战术卡」，不是「没查到」。**

---

## §二 数据表示与改动

### 2.1 为什么选「生成器层」而不是 `cardface_fixes`

- `cost = -1` 不是「某张卡 OCR 抄错」的特例，而是**原版 SO 的哨兵语义**（`manaCost = -1` = 不可直接打出）。
  规则必须跟着**真值**走：以后再有卡落进池，只要 SO 是 `-1` 就应该同样落 `-1`。
- `cardface_fixes.json` 是**逐卡人工覆盖**（数据正本，且归卡牌线但不在本次刀口）——用它会把一条通用规则
  写成一次性的卡名特判，且下次重跑生成器时容易被别的写手覆盖掉。
- 现读池内 `manaCost == -1` 的卡就是这 4 张（按 id 判定，见 §1.1）⇒ 规则**不会一刀切误伤**。

### 2.2 生成器改动（`Unity/工具/gen_cards_engine.py`）

| 位置 | 改动 |
|---|---|
| `:586` `STATS_EXCEPTIONS` 上方注释 | 2026-10-19 就地订正：原来「③ 另外三张 cost 1 ⇒ Fate 也取 1」**已作废**；该条只保留「把 Fate 收进卡池」的作用，`cost` 由 SO 层落 `-1` |
| `:1883` 记账口 | 新增 `unplayable_cost_set = []`（`(cid, 卡名, 类型, 改前值, SO 行名)`） |
| `:2161-2176` SO 真值层 | `_so_c_raw == -1`（**字段真的存在且就是 -1**）⇒ `cost = -1` 并记 `unplayable_cost_set`；`< 0` 但 `!= -1` 仍走 `so_stat_skipped`；`>= 0` 照旧 |
| `:2815` / `:2887` | 把新记账口接进 `build()` 返回元组与 `main()` 解包 |
| `:2742-2747` `source` 元数据 | `cost←manaCost` 的说明补上「`-1` = 不可直接打出，原样落 `cost=-1`」 |
| `:3332-3337` 摘要 | 新增「不可直接打出 `manaCost=-1` 的卡 N 张 ⇒ cost=-1」并逐张打印；`so_stat_skipped` 的文案去掉「`-1` 哨兵」 |

### 2.3 生成结果（`Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`）

逐卡 diff（生成前备份 `_tmp_view/darkpact/cards_engine.before.json`）：

```text
changed cards: 4
('1dfc9bc738bfc4f4d93d48f165aee510', 'Dark Pact of Resilience', ['cost'], {'cost': (1, -1)})
('4a48457c24877f345be2e96f867c1e94', 'Dark Pact of Fate',       ['cost'], {'cost': (1, -1)})
('9a2cadc64c80a6b42912441105bb1cee', 'Dark Pact of Blood',      ['cost'], {'cost': (1, -1)})
('b8e81b2fe85cda747a68c4d6326b669c', 'Dark Pact of Excess',     ['cost'], {'cost': (1, -1)})
```

- `count` 1127 → 1127；id 增删 = 0；**除这 4 张的 `cost` 外 0 处变化**。
- 元数据 `source` 文本按 §2.2 更新（不是卡数据）。
- 现读池内 `cost < 0` 恰好 4 张、`min(cost) = -1`。

---

## §三 引擎守卫（`cost < 0` 不能被当成「免费」）

**风险点**：`RuleCore.CostOf` 两个返回点都是 `System.Math.Max(0, v)`
（`RuleEngine/Core/RuleCore.cs:861` / `:870`）⇒ `cost = -1` 会被 **clamp 成 0**，
`CanPlayCard` / `CanPlayTactic` 里那句 `CostOf(...) > ps.Energy` **永远为假** ⇒ 误判成「免费可打」，
而且 `PlayTactic` 还会 `ps.Energy -= 0`。

**守卫（三条，最小改动）**：

1. `RuleEngine/Core/RuleCodes.cs:80` 新增 `ErrNotPlayable = 20`；`:130` 中文名「这张卡不能直接打出」。
   ⚠️ 不复用 `ErrCost`（会误导成「攒够能量就能打」）、也不复用 `ErrUnimplemented`（那是「本版没实现效果」）。
   `TermKey(20)` 返回 `null` ⇒ 诊断面走 `Describe` 的中文兜底（原版没有这个状态、也没有 I2 词条，如实）。
2. `RuleEngine/Core/RuleCore.cs:2069-2075`（`CanPlayCard`，取到 `card` 之后、**战术卡分流之前**）：
   `if (card.Cost < 0) return RuleCodes.ErrNotPlayable;`
   —— 这是表现层唯一查询口（拖拽高亮 / AI 都问它）。
3. `RuleEngine/Core/EffectResolver.cs:1388-1398`（`RuleCore.CanPlayTactic`，同一个 partial class）：
   同样的守卫，挡在 `CostOf` 之前。
   —— **必须有这一条**：`RuleCore.PlayCard` 对非单位卡**开头就分流到 `PlayTactic`**
   （`RuleCore.cs:2391-2394`），不再经过 `CanPlayCard`；`PlayTactic` 内部只调 `CanPlayTactic`。

**覆盖面核实**：

- AI：`SimpleAI.Enumerate` 的每条候选都问 `RuleCore.CanPlayCard`（`RuleEngine/Data/SimpleAI.cs:302,312`）⇒ 被挡。
  它前面那句 `if (cost > mine.Energy) continue` 用的是 clamp 后的 0，但后面这条会兜住。
- 牌组：`DeckBuilder.StarterDeck` 用 `DeckRules.CannotAddToDeck` 排除（`Data/DeckBuilder.cs:58`）；
  这 4 张 `showInCollectionOptions = 0` ⇒ 真值口径下本来就不能进卡组/收藏。
- ⚠️ `BattleDriver.cs` **未改**：它的出牌合法性最终都回到 `RuleCore`（`BattleDriver.cs` 的 `CanPlayCard` 调用点），
  没必要在战斗线文件里再写一份判据（两处写同一条规则 = 迟早不一致）。

---

## §四 卡面显示

**现读结论：显示侧原本就是对的，本次只补「判据出处」、不动行为。**

- 判据只一处：`CardView.ShowsCost(d) { return d.cost >= 0 && d.type != "hero"; }`
  （`CardPresentation/Core/CardView.cs:1486`）。`cost = -1` ⇒ `false`。
- 本次在该方法的 XML doc（`:1479-1485`）补上 2026-10-19 裁定：**`cost < 0` = 「无费用」的数据表达**
  （不是缺值、不能拿 0 顶），并指向引擎守卫 `ErrNotPlayable`。
- 三个入口都过它（逐个现读）：
  - **出生入口** `Build`：`if (!board && costTex != null && ShowsCost(d))` 才建 `_costBg`（`:2732-2744`）；
  - **换形态入口** `SetFace`：`Show(_costBg, !board && ShowsCost(Data))`（`:1393`）；
  - **数字**：`InfoTexture` 里 `if (ShowsCost(d))` 才烘费用数字（`:7013`）；
  - **命中区**：`StatAt` 对 `StatCost` 也走 `ShowsCost`（`:5507`）。
- `CostAt01` / `FaceCostAt01` 是**纯几何函数**（返回落点），不该在里面判显隐；
  闸门在它们的**调用点**（上面三处）——本笔不动 W27 的贴右缘规则，只是整个费用件不显示。
- 断言可读的公开口：`CardView.CostVisible`（`:1691`）—— 直接读 `_costBg` 节点的 `activeSelf`。

---

## §五 断言（含灭自证）

落在 `CardPresentation/Editor/CardBaseDemo.cs`（**在 `_run_8_checks.sh` 的 12 条里**）：

- `CheckDarkPactNoCost()`（`:80-116`），由 `Run()` 开头调用（`:124`）。
- 对 4 张逐张断三条：

| # | 断言 | 怎么才能红（灭自证） |
|---|---|---|
| ① | `def.Cost == -1`（数据） | 把生成器 SO 层改回「只取 `manaCost >= 0`」⇒ 退回 1 ⇒ **必红** |
| ② | `CardView.ShowsCost(data) == false` | 删掉 `ShowsCost` 里的 `cost >= 0` ⇒ **必红** |
| ③ | `CardView.CostVisible == false`（费用底板节点不激活） | 把 `Build`/`SetFace` 里的 `ShowsCost` 判据删掉 ⇒ **必红** |

⚠️ ③ 是「节点真的没建/没开」，和 ② 的纯函数判据分开断 —— 只断 ② 会漏掉「判据对、调用点没接」的那种静默失败。

---

## §六 逐卡 diff / typecheck

- **逐卡 diff**：见 §2.3 —— 只有 4 张 `cost 1 → -1`，其余 0 变化。
- **类型检查**：`TMPDIR=/tmp/wf_darkpact bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- **未跑** Unity 自检（用户 2026-10-15 口径：只保留秒级类型检查）。因此没有 `RuleEngineTest` 计数可对。
  代码侧判断：本次没有改任何**计数常量**，只改了 4 个卡数据值 + 新增 1 个返回码
  （`RuleEngineTest` 里对 `RuleCodes` 的断言是逐条指定码值、没有「枚举全集」断言，见
  `Editor/RuleEngineTest.cs` 的 `ErrNoTargetAvailable` / `ErrNotEnoughRoom` 那一段）。

---

## §七 没查到 / 风险（如实记）

1. **「哪张战术卡」没有唯一答案** —— 生成者是 `markOfChaos` 一整族（§1.3），
   其中战术卡 `BL46 Gifts of Chaos` 是「四选一随机」的典型。**不是「没查到」**，是原版本来就这么设计。
   用户裁定不受影响。
2. **`DeckRuntime` 牌组行的费用文字**：`DeckRuntime.cs:4229` 无条件 `def.Cost.ToString()`。
   若这 4 张**真的进了某个牌组**，牌组行会印出 `-1`。
   现存链路进不去：`DeckRules.CannotAddToDeck`（`showInCollectionOptions = 0`）挡着；自动凑牌也走同一条
   （`Data/DeckBuilder.cs:58`）。`Deck/**` 属界面/卡组线、本次 ⛔ 不动 ⇒ **记账**：
   若将来允许这类模板进牌组，需要给 `_deckRowCostText` 加 `cost >= 0` 判据。
3. **排序**：`DeckRuntime.cs:4092` 按 `Cost` 升序 ⇒ `-1` 会排在最前。只影响**会显示这些卡的池子**；
   收藏页 `showInCollectionOptions = 0` 已隐藏；卡组编辑池若显示它们，顺序会变（数据正确、非缺陷）。
4. **`DeckEditorState.IsEmpty`**（`Deck/DeckEditorState.cs:63-69`）把 `Cost < 0` 当「筛选为空」的其中一项，
   但它还要求 Name/Faction/Rarity/Type 全空 ⇒ 这 4 张（有名字/阵营/稀有度/类型）不受影响。
5. **`DeckScene.cs:7293`** 的 tooltip 夹具用 `d.Cost < 0` 跳过 —— 与英雄同档，不影响断言。
6. **`CostOf` 的 clamp** 保持原样（`Math.Max(0, v)`）：它在「费用修正把费用降到 0 以下」时也是对的；
   负费用的语义由**守卫**表达，不靠改 clamp。
7. **`CreatePool` 的费用上下界**（`RuleEngine/Core/CreatePool.cs:179-180,228-229`）对 `-1` 的过滤：
   `costMin > 0` 时这些卡现在会被排除（旧值 1 可能通过），`costMax > 0` 时仍会通过。
   这些卡 `cantBeRandomlyCreated = 1`，本就不该被「随机造牌」选中；`CreatePool` 目前没有单独的
   `cantBeRandomlyCreated` 闸（属引擎线的既有缺口，不在本次刀口）⇒ **如实记账**，不在此处扩大改动。
