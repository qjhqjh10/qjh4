# B141_W128_RuleCore残留（W22 执行写手）

> 执行写手 `B141-W22` · 2026-11-04 · 批次 `B141` · 会话第一百二十八。
> 白名单：`RuleEngine/Core/RuleCore.cs` · `RuleEngine/Core/TargetCriteriaEval.cs` · 新建 `RuleEngine/Editor/B141_W22_Assert.cs` · 本报告。
> ⛔ 未改 `BattleDriver.cs` / `EffectResolver.cs` / 账主表 / 活状态表 / 所有权表；⛔ 未跑 Unity；⛔ 未动 git / `d:/2`。
> 行号一律本文写作时**现读**。

---

## 〇 · 一句话结论

| 任务 | 判定 | 改在哪 |
|---|---|---|
| **1. SAN-27 残余③**（事件/表现层逐行目标） | ✅ 已做（事件层） | `RuleCore.cs:15632` 行首加 `AppendOneRowAbilityTargetsToEvent(...)`；新成员追加类尾 `AppendOneRowAbilityTargets*`。**每行**按自己的 `activeAbilityTargetCriteria[lg]` 算目标并进 `AbilityTargetList`，按 `InstanceId`/`Player+Slot` 去重、零随机消费 ⇒ `BattleDriver.ExpandAbilityBeats` 逐目标排拍（未改动）。 |
| **2. SAU55 `ta=235`** | ✅ 已做 | `TargetCriteriaEval.cs` 实现 `case 235`（`Matches` + `MatchesRawCard`）= **己方（施放者）战将的天赋卡**（按卡名比）；`RuleCore.cs` 新增 `ApplyWarlordTalentHandBuffOnPlay`，在 `PlayCardCore` 战术分支结算成功后落 `handBuff ∧ ta=235` 行到本方手牌。 |
| **3. 过期注释** | ✅ 已做 | `RuleCore.cs:91`（遭遇起手 4→3）· `:17923-17927`（手牌半已产 / 2-3 已实现）。 |

**验证**：`typecheck` 运行时 **0** / 编辑器 **0** · `ruleprobe play check` **rc=0**、结算指纹 **a0e9c63d7bfa33735f5dad8230a26edd**（**与基线一致、未变**）· `ruleprobe check`（静态）**rc=0**、全池 1134 张、解析差异 **0 行**、哨兵合计 **失败 0 条**。
**未执行断言**（无 Unity）：断言文件见 §四，**尚未接进 `RuleEngineTest.cs`**（白名单外，调度台代接）。

---

## 一 · SAN-27 残余③：多行 ability 逐行目标（事件层）

### 改法（逐行算、并表、去重）

- 生产点：`RuleCore.cs:15632` 行循环里，在 `PublishRowAbilityTargetTable(...)` 之后**同一物理行**追加
  `AppendOneRowAbilityTargetsToEvent(ctx, p, u, chosen, step);`（零净行数）。
- 新成员（类尾）：
  - `AppendOneRowAbilityTargets(ctx, owner, source, chosen, step, into)` —— 取 `source.Card.ActiveAbilityTargetCriteriaAt(step[3], step[2])`，
    **只收零随机消费档**（`ta ∈ {0,10,110,140,150}` ∧ `MaxTargets < 1`，与 `PublishRowAbilityTargetTable` 白名单同）；
    `AbilityTargetListFromCriteria` 产出后按 `InstanceId`（无实例号时按 `Player+Slot`）去重并入。
  - `AppendOneRowAbilityTargetsToEvent(...)` —— 取 `ctx.Signals` 最后一条 `EvtKind.Ability`，把该行目标并进它的 `AbilityTargetList`（`null` 时新建）。
  - `AppendPlanRowAbilityTargets` / `AppendPlanRowAbilityTargetsForTest` —— 整张计划版（自检用）。
- 为什么**行首**调用：原版每行各一次 `GetTargets + PlayAnim`（`AbilityLogic__PlayAbility.c:262/273`，第二处 `:1524-1531`）；
  行首那一刻「前几行已结算、本行未结算」⇒ `ta=140/150`（上一条 ability 的目标表）读到的是**正确的那一时刻**。

### 原版值 vs 我们值

| 项 | 原版 | 改前 | 改后 |
|---|---|---|---|
| 多行 ability 的目标集 | 每行各算各的、各播各的 | 事件表只承载**挑中的那一行**（`ActiveAbilityTargetCriteriaForPlan`） | 事件表 = **各行并集**（去重）；表现层逐目标排拍 |
| 随机序列 | 建池不掷骰（这几档） | 不变 | **不变**（只收零随机档；`play check` 指纹未变证实） |

⚠️ **结构边界（未改、如实标）**：`BattleEvent` 仍只有**一张** `AbilityTargetList`（消费端只按它逐目标排拍，不区分「这个目标是哪一行的」）。
⇒ 当**两行都带 `abilityTarget` 拍**时，每拍的每个目标会播到并集里的**全部**目标（over-play）。原版是每行播自己那批。
这是「不改 `BattleDriver`」这条约束下的最大改进；要彻底逐行需要事件按行承载（结构改动，另立）。

---

## 二 · SAU55 `ta=235 warlordsTalent`

### 原版判据（现读 `.c`）

`d:/2/tools/decomp_full/FilterMethods__CheckIfMeetsCriteria.c:126-157`：
`if (iVar17 == 0xeb /*235*/)` → `BattleManager__GetHero(施放者侧)` → `CardScript__GetTalentCard(hero, 0)` → 与候选 `System_String__Equals(…, 0)`（**按卡名** ordinal）。
枚举 `d:/2/tools/il2cpp_out/dump.cs:20816` `warlordsTalent = 235`；数据 `SAU55.abilityBuffs[lg2] = handBuff{ buffType 2, costChange -1, pa 10, criteria.targetsAffected 235 }`。
⇒ 主语 = **己方（施放者）战将的天赋卡**（手上那张 `SpawnTalents` 造的临时战术卡）。

### 改法

1. `TargetCriteriaEval.cs`：
   - `Matches` 的 `case 235`（原 `gap=未建模; return null`）→ 有上下文时按卡名比、无上下文 ⇒ `null`（判不了，保留候选）。
   - `MatchesRawCard` 的 `if (ta == 0) return true;` 那一行**行内追加** `if (ta == 235) {...}`（手牌卡走的就是这条；零净行数）。
   - 类尾新增静态上下文 `_warlordTalentCardForEval` + `Set/ClearWarlordTalentContext` + `HasWarlordTalentContext`
     （本类没有 ctx/owner 形参 ⇒ 由调用方在匹配前设、用后清；唯一生产写点见下）。
2. `RuleCore.cs` 类尾：
   - `ResolveWarlordTalentCard(ctx, owner)` —— `TalentCardData.Resolve(pool, 督军卡 id)` 的**第一个**（`CardScript__GetTalentCard` = `unitsToSummon[0]`），退 `TalentName` 真值表/按名查。
   - `ApplyWarlordTalentHandBuffRows(...)` —— 设上下文 → 逐 `handBuff ∧ ta=235` 行调 `AttachHandAbilityBuff`（**复用既有手牌效果机制**：登记记录 + 当前手牌 + 后进手牌）→ finally 清上下文。
   - `ApplyWarlordTalentHandBuffOnPlay(ctx, owner, played)` —— 生产入口。
   - 接线：`RuleCore.cs:4045`（`PlayCardCore` 战术分支）把 `return PlayTactic(...)` 改成**同物理行**的块：先存 `CardDef`、`PlayTactic`、成功则 `ApplyWarlordTalentHandBuffOnPlay`。**零净行数**。
3. 范围：现读 `cards_engine.json`（1142 张）里 `handBuff ∧ criteria.targetsAffected == 235` **只有 `SAU55` 1 行**
   ⇒ 只影响 SAU55，零回归；`AttachHandAbilityBuff` 自带 `doNotStack` 闸 ⇒ 与任何既有派发点叠跑幂等。

### 原版值 vs 我们值

| 项 | 原版 | 改前 | 改后 |
|---|---|---|---|
| 「你的战将的才能花费 -1」 | `ta=235` 命中天赋卡 → `AddHandEffect`（`buffType 2 changeCost -1`） | `ta=235` 未建模 ⇒ 一格都命中不了（ca 50 行也被正文闸挡） | 命中天赋卡、-1 费；非天赋卡不动；幂等 |

⚠️ **没有改的两条**（不在本白名单 / 属另一笔）：
- `EffectResolver.cs:21078` 的 `ca 50` 正文闸（`ops 非空 ⇒ FireTriggerRowsByChosenAbility 不进`）**未动** —— 本改法**绕开**它：
  直接在战术结算后按数据行（`abilityBuffs`）落手牌效果，不走 `ca 50` 派发。好处是**不双发**（走数据行一次）；代价是 `ca 50` 那一族仍保持原状。
- 天赋卡身份的「哪一份」：按**卡名**比（照原版 `System_String__Equals`），不是实例；同名的另一份也会被 -1（原版亦然）。

---

## 三 · 过期注释（铁律 5）

- `RuleCore.cs:91`：`遭遇 起手 4` → `遭遇 起手 3`（真值 `GameplayVariables.cs:113 SkirmishStartingHand = 3`，已带 `✅ B141` 标记）。
- `RuleCore.cs:17923`：`ta=150 落到手牌的那一半不产` → `【已产出】`（`handRefs → Slot=-1 + InstanceId` 事件表；效果层只吃棋盘项）。
- `RuleCore.cs:17924`：改成历史注释（`PreviousAbilityTargets` 现为 `List<CardRef>`、`AbilityTargetView` 有 `Slot=-1` 手牌回落）。
- `RuleCore.cs:17926-17927`：`2/3 valueInPlay` → `【已实现】`（`KeepByValueInPlay` 按 `SimpleAI.ValueInPlay` 排序；未知档仍 `null`）。

---

## 四 · 断言文件（`RuleEngine/Editor/B141_W22_Assert.cs`，247 行）

| 组 | 断言 | 灭自证（删掉哪一处会红） |
|---|---|---|
| A | A1 row1（ta=140）的敌方进表 · A2 row0（ta=10）的己方部队进表 · A4 并两次去重 | A3：只跑 row0 ⇒ 敌方**不在** |
| B | B2/B3 `MatchesRawCard` 命中天赋卡、不命中别卡 · B6/B7 棋盘路同档 | B4：上下文换一张 ⇒ 原天赋卡 false |
| C | C1 命中 1 张 · C2 天赋卡 -1 · C3 非天赋卡不动 · C4/C5 幂等 | C2/C3：放开 ta 过滤 ⇒ 非天赋卡也 -1；C4：删 `doNotStack` ⇒ 第二次又 -1 |

**接线（paste-ready，调度台在 `RuleEngineTest.cs` 聚合行同一物理行、第一个 `//` 之前追加）**：
```
Guard("B141W22_Assert", B141_W22_Assert.Run); _pass += B141_W22_Assert.Pass; _fail += B141_W22_Assert.Fail;
```

---

## 五 · 验证原始读数

```
$ TMPDIR=/tmp/wf_b141w22e bash 工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0

$ bash 工具/ruleprobe.sh play check
结算指纹：当次现算 = a0e9c63d7bfa33735f5dad8230a26edd · 基线文件 = a0e9c63d7bfa33735f5dad8230a26edd（一致）
=== 18:43:44 结束（rc=0）===

$ bash 工具/ruleprobe.sh check
=== 哨兵合计：失败 0 条 ===
=== 全池 1134 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 · 基线 out_baseline.txt ===
=== 18:43:51 结束（rc=0）===
```

`check_ledger_points.py`：合计 A 5966 · 通过 5530 · 漂移 **399** · 失败 **38**。⚠️ 这些是**仓库既有 + 他人在飞**的存量（本件把 `TargetCriteriaEval.cs` 的两处漂移**消掉了**：改前 408/40 → 改后 399/38）。我的对账落点未引入新漂移。

---

## 六 · 行数变化

| 文件 | 改前 | 改后 | 说明 |
|---|---|---|---|
| `RuleCore.cs` | 18,596 | **18,748** | **+152，全部追加在类尾**；三处生产改动（`:4045` / `:15625` / `:15632`）都是**同物理行改写 = 零净行数** |
| `TargetCriteriaEval.cs` | 1,211 | **1,223** | **+12，全部追加在类尾**；`case 235` 与 `MatchesRawCard` 的改动都是**行内改写 = 零净行数**（`git diff` 之外的判据：`check_ledger_points` 的 `:301`/`:1062` 落点已回位） |
| `Editor/B141_W22_Assert.cs` | —— | 247（新） | 新文件 |

---

## 七 · 顺手发现（⛔ 只报不改）

1. 🔴 **`PublishRowAbilityTargetTable` 对真实主动能力恒早退（B140_WA 的潜伏 bug）**：
   `RuleCore.cs:18577`（现读）`if (step[2] < 0 || step[3] < 0) return;` —— 而 `step[3] = row.Ability` = `ActiveAbilityObject` = **-1**
   （`CardDef.ActiveAbilityObject = -1`；数据 `activeAbilityLogic[].ab` 现读全 `-1`/`-2`），
   `ActiveAbilityTargetCriteriaAt(-1, j)` 本来就接受 -1 ⇒ 这一句让**效果层那张 `ctx.AbilityTargetTable` 的按行重挂从未执行过**。
   `B140_WA_Assert` 的 C3/C4 传的正是 `CardDef.ActiveAbilityObject`，接线后**会红**（该断言文件尚未接线，所以没被发现）。
   **建议一行修**：`if (step[2] < 0) return;`（只判 logic 下标）。⚠️ **本件未改** —— 它改的是**效果层**（结算消费那张表），
   会动实际命中的目标、很可能改 `play check` 指纹，需单独立项 + 逐张归因，不在本件「事件/表现层」范围内。
2. ⚠️ 我的 `AppendOneRowAbilityTargets` 已避开同一坑（只判 `logic < 0`，允许 `abilityObject` 为 -1/-2），并在注释里点名。
3. ℹ️ `RuleCore.cs:18577` 那一句的注释写「`ca == 462` 那类行没有 (ability, logic) 二元组」—— 实际上 462 行的 `step[2] = -1`（logic）已足够挡住，`step[3]` 那一半纯属误伤。

---

## 八 · 还差什么 / 未闭合

- **断言未执行**（无 Unity）；`B141_W22_Assert` 未接进 `RuleEngineTest.cs`（白名单外，交调度台）。
- **多行事件层的结构边界**（两行都带 `abilityTarget` 拍 ⇒ over-play）未解（需 `BattleEvent` 按行承载，跨 `BattleDriver`，本批不做）。
- **`PublishRowAbilityTargetTable` 效果层早退**（§七·1）未修 —— 建议另立一笔；修完需重跑 `play check` 逐张归因。
