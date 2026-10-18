# R · 引擎线：拿到完整卡牌 ID 与 SO 之后的调整评估（只读 · 2026-10-18）

> 只读代理产出（无写文件工具 ⇒ 由主对话原样落盘）。

## 0. 一句话结论

**ID 那一半已经不用再调整**（引擎卡池 1127/1127 全是真 id，K06 已完成）；**SO 那一半有大量真值可用，
但引擎的结算核心【不该】改成直接吃 SO** —— SO 是结构化枚举/判据树（非文本），换运行期输入等于重写解析器；
正确动作是**把 SO 变成"对拍真值"接进 T07/T08/T06/T05/T01-T04**，并修掉三处**仍读旧快照的工具**与一处**存档/回放旧 id 迁移漏洞**。

## 1. SO 是否含结构化效果数据：**有，且是完整规则层**

证据（`资料/普查产出_1018/allcards_1350/cards_raw.json`，1,351 张）：

- `cardAbilities` 非空 **1,205/1,351**（146 张白板）；能力条目合计 **1,496**；`abilityLogic` 条目 **1,873**。
- 每个 logic 都有 `chosenAbility`（**55 个不同取值**）、`playersAffected`、`targetCriteria`、`multiplier`、
  `damageCriteria`、`healCriteria`、`buffEffect`、`summonCriteria`、`handBuff`、`manaBuff`、
  `chanceOfHappening`、`checkIfCasterInPlay`。
- 数值是真值不是文本：例 `EC29 Heldrake` → `chosenAbility=10(doDamage)`、`damageCriteria{minDamage:3,maxDamage:3,damageType:0}`、
  `targetCriteria{targetsAffected:10,enemyUnits:1,includeHero:1,…}`。
- 触发是枚举：**60 个不同 `trigger` 值**（`0 ThisCardPlayed` 772 次、`46 Talent` 105、`145 Codex` 37…）。
- `activeAbility` 内联结构（`manaCost/plasmaCost/psychicCost/targetCriteria/activeAbilityLogic`）**1,351 张都有对象**；
  `hasActiveAbility=1` **88 张**。
- 注意：SO **没有内联卡面文本**；文本走本地化键（`cardDescriptionTextReferenceId` 只有 44 张、
  `activeAbilityTextReferenceId` 3 张），当前 build 的 I2 表 `Card_Description/*` 1,260 条
  （`localization_2026_decoded.json`），且带 `[[…]]`/`((…))` 标记 ⇒ 引擎要"直接吃 SO"必须解 55 个效果枚举 +
  60 个触发 + 判据树语义，**不是换一个数据文件就能完成**。

## 2. 引擎现状（与卡 id / 卡数据来源有关）

- 卡进引擎：`数据/游戏数据/card_stats.json`（OCR/PnP 源）→ `工具/gen_cards_engine.py`（`SRC:30`；
  `TRUTH_CARDS_SRC:821` 已换 2026 导出）→ `Resources/cards_engine.json`（v9/1127）→
  `RuleEngine/Data/CardDatabase.cs:223` 反序列化 → `CardDef`。
- 效果解析：**按英文卡面文本**，不按 id/名字查表。`CardDef` 构造函数 `Core/CardDef.cs:237` →
  `CollectTriggerOps:761` → `EffectText.Parse:854`。`EffectText.cs:5213` 自陈：
  「原版**不解析英文卡面**（效果是 `cardAbilities[].abilityLogic[]`）」—— 这是我们自己选择的一条架构。
- 兜底/猜测痕迹：
  - `CardDatabase.cs:280` 缺 id 时**退回卡名当身份**（同名 3 组会串）。
  - `gen_cards_engine.py:1790-1798` 仍保留"真值挑不出 ⇒ 自造 id"分支（今天不可达）；
    `:2301` note 仍写"对不上的自造"（已过期）。
  - `CardDatabase.cs:123-186` 的 v9 真值列（`cardTraits/targeted/attackDamageType/cantBeRandomlyCreated` 等）
    **只落值、无消费者**（文件内自陈）。
  - 文本解析器有 `Partial/Unknown` 与卡面 `*` 的如实降级（`EffectText.cs:1144/1781`，
    `BattleDriver.cs:14284-14366`）—— 这是"不许静默失败"，不是猜测，保留。
- 存档/回放里的 id：`DeckStore` 存 `CardIds` = `CardDef.Id`；回放 `MsgStart.hostDeckJson/clientDeckJson`
  （同一副牌 JSON）、`MsgAction.pickIds`/`defId`（`NetProtocol.cs:107-128`）；`envSO` 是**环境资产名**不是卡 id。
  回放版本 `ReplayStore.cs:79` = 2。

## 3. 任务调整表

| 任务 | 现状 | 建议 | 优先级 | 影响面 |
|---|---|---|---|---|
| **T07 逐卡对账** | ⏳待写；判据还指 2022/第二十八会话的老对账 | **改判据源为 `cards_raw.json`**：池内 1127/1127 都被 SO 覆盖、1008 张有 `cardAbilities`；逐卡按 `abilityLogic` 建"打谁/打多少/什么触发"真值表 | **P0** | 整条卡效果复刻的输入 |
| **T08 探针/量具** | 只与自家基线比，无外部真值列 | 新增"SO 真值指纹"列（按 id join `chosenAbility+damage/heal+targetCriteria`）；`A1991` 随 id 统一**自然消解** | P1 | 读数可信度 |
| **T06 AI 与评分表** | ⏳待写；原写"照原版逐卡评分表"，当时拿不到 | **真值已在本地**：`attackScoringCriteria` 1127 张、`cardTargetScoringCriteria` 261（池内）、`nonTargetScoringCriteria` 307、`extraScoreFrom*` 列 | **P1（可开写）** | AI 选牌/选目标 |
| **T05 选择面板** | ✗ ？；靠我们推断摆几张 | SO 有 `choicesDefinition`/`targetsAvailableCounter`/`cardTargetCriteria`（含 `maxTargets`/`targetSelectMethod`）逐卡真值 | P2 | 多目标卡面板 |
| **T03 事件/时机** | ✗ ◐ ？ | 用 60 个 `trigger` 枚举 + `triggerValue` 做对拍清单 | P1 | 触发先后 |
| **T02 条件/替代** | ◐；条件型 instead 已修 | SO `boardCriteria.triggerIf`（0/1/2/3/8 五档）+ `countCriteria` 可解成条件真值；作对拍 | P1 | 条件结算 |
| **T01 伤害/死亡** | ◐；A2017 已裁"不给引擎加伤害类型" | SO `damageType` 池内真值分布 **0:1480 / 5:133 / 3:3** —— 若将来要落地伤害类型，判据已齐 | P2 | 伤害数值 |
| **T04 关键词/附带层** | ✗ | SO `CardTraits` 696/1127、`hasActiveAbility` 87、`hasAlternativeAbility` 1 | P2 | 关键词机制 |
| **T09 判据链** | ✗ ◐；多处文档仍引 2022/2025 快照与旧 id | 更新"真值源"指针 → **2026 current build `cards_raw.json`** | P2 | 文档一致性 |
| **新增 T12 · SO 真值旁路** | 无 | 只读生成器：`cards_raw.json` → `cards_truth.json`（id/trigger/chosenAbility/数值/criteria/评分），**不改运行期解析器** | **P0** | T01-T08 共同判据 |
| **新增 T13 · 存档/回放 id 闸** | 无 | 见 §5；一次性迁移 + 版本闸 | **P0** | 兼容性 |
| **新增 T14 · v9 死列接线** | 字段已落值无消费者 | `targeted`（246 张池内）、`cantBeRandomlyCreated`、`attackDamageType` 逐项接线或显式登记"不接" | P2 | SimpleAI/CreatePool/伤害 |

## 4. 代码调整表（逐处）

| 文件:行号 | 现状 | 建议 | 优先级 | 影响面 |
|---|---|---|---|---|
| `工具/gen_talent_cards.py:40` | 仍读 **2022 快照**；输出 89 条，池内 SO `trigger==46` 有 96 张 ⇒ **缺 7 张**（DA75/SW1/SW2/SW3/UM87/UM88/UM100） | 换 `cards_raw.json`（trigger46 → `unitsToSummon`），重生成 | P1 | 天赋造卡 |
| `工具/gen_related_cards.py:76` | 仍读**旧 bundle（2025-07-30）**；表 176 键，而池内 SO `relatedCard1..4` 非空 **198 张** ⇒ **缺 22 张** | 换新 `__data` / `cards_raw.json`，重生成 | P1 | 详情窗/Companion |
| `工具/gen_cards_engine.py:1790-1798` | 自造 id 兜底仍在（今天 0 张走到）；`:2301` note 过期 | 缺 id 改**硬失败**（或注明"仅历史路径"）；更新 note | P2 | 防 id 回归 |
| `RuleEngine/Data/CardDatabase.cs:280` | 缺 id 退回卡名（跨阵营 3 组同名会串） | 改为报错并跳过该卡（卡名不作身份） | P2 | 身份稳定性 |
| `RuleEngine/Data/CardDatabase.cs:123-186` | v9 SO 真值列只落值、`CardDef` 构造未传 | 接线或登记；`targeted` 先接 | P2 | AI/目标判定 |
| `RuleEngine/Core/CardDef.cs:237/761/854` | 效果 = `desc` 文本解析 | **不替换**为 `abilityLogic`；在 T12 里做真值对拍 | P0(新增) | 结算核心（保持不动是结论） |
| `工具/gen_cards_engine.py:1928` + `:30` | `desc` 来自 `card_stats.json`（OCR/PnP） | **别直接换官方 `Card_Description`**：池内 805/1127 与之不同（I2 标记语法），换源须先改解析器 | P2 | 解析输入 |
| `RuleEngine/Data/SimpleAI.cs:509` | 主动技打分走我们封闭文法/op 表 | 用 SO 评分真值（T06）替换推导表 | P1 | AI 行为 |
| `CardPresentation/Battle/DeckBuilder.cs:308-313` | 卡组里查不到的 id **只 LogError 后丢牌** | 加"旧 id → 真 id"**静态迁移**或回放版本闸（见 §5） | **P0** | 存档/回放可复现 |
| `CardPresentation/Battle/ReplayStore.cs:79` | 版本 2；不校验卡 id 集合 | 若做 id 迁移：旧档读时按映射改；否则 bump 版本并显式拒绝旧 id 档 | P0 | 回放 |
| `CardPresentation/Net/NetProtocol.cs:107-128` | `pickIds/defId` 已是 `CardDef.Id` | 真 id 后**无需改** | — | 联机/录像 |
| `工具/ruleprobe/*` 基线 | K06 已按新 id 重生成、三指纹重钉 | 真 id 后**无需改** | — | 探针 |

**引擎"对战结算核心"（RuleCore/EffectResolver/EffectText）不需要因 id 或 SO 改动**；
要动的是**数据来源、死兜底、SO 真值旁路、存档兼容**。

## 5. 迁移风险（重点：现有结论有一处漏）

- 🔴 **存档/回放里有 4 个已不存在的自造 id**（K06 §五 只扫了"旧真 id 集合"，漏了自造形态）：
  - 2 份 `WarpforgeDecks.json` + 3 份带该卡组的回放（10-11/10-13/10-15）里都含
    `ASH_Autarch`（今 **ASH83**）、`ASH_Death_Spinner_Warp_Spider`（**ASH77**）、`ASH_Farseer`（**ASH80**）、
    `ASH_Orian_Laratharjos`（**ASH79**）。
  - 后果：每副 30 张**丢 7 张**（`DeckBuilder` 只报错不补），回放 `finalHash` 必然对不上 ⇒ 这些回放**已不可复现**。
  - 建议：一次性把上述 4 个 token 按静态表改写（只改存档/录像副本，⛔ 不在运行时猜），
    或 bump 录像版本 + 明确提示"旧档不兼容"。
- `card_ids.json.poolTrueIds` 是**历史表**，⛔ 不可反转。
- **数值漂移**：池内 **60 张**与 2026 SO 四格不同（health 49、cost 10、attack 1；例 ASH1/ASH2/ASH3/AM2 英雄 35/40→25/30）。
  引擎的伤害/死亡阈值按现池值跑；这是"PnP 优先 vs 当前 build"的口径决定，**不是引擎 bug**，但要在卡牌线一次裁定。
- 官方 `desc` 与现 OCR 文本 805/1127 不同（标记语法），**换源会让解析器大面积失配**；当前不要换。
- K06 已重生成探针基线；任何旧基线/指纹文件不得再用。

## 6. 没查清（⛔ 不猜）

1. 2026 build 的 **55 个 `chosenAbility` / 60 个 `trigger` 是否都有现成枚举名**：`abilityLogic_枚举词典.tsv`
   是旧快照口径（53/57），未逐值核对 2026 新增值。
2. `abilityLogic` 与 `EffectText` op 文的 **1:1 语义覆盖率**：旧报告只有 2022 快照抽样
   （draw 97%/heal 98%/damage 81%），未在 2026 build 重算。
3. `initialHandModifier` 等数值/PPtr 字段的真实非空数：「非空」判定对 PPtr 形恒真，未逐字段重数。
4. `envSO` 能否换成 id：环境对象不是 `RawCardScript`，`dump.cs`/`cards_raw.json` 里没找到对应 id 字段。
5. 上述回放是"迁移"还是"判废"是产品口径。
