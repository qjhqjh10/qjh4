# B141 · W39 · `B141-EXTRA-TRIGGER-NAME` —— `ExtraTriggerName` 补两条映射（含反证）

> 执行写手 `B141-W39` · 2026-11-04 · 批次 `B141` · 会话第一百二十八。
> 白名单：`RuleEngine/Core/ActionPump.cs` · 新建 `RuleEngine/Editor/B141_W39_Assert.cs` · 本文件。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改 `d:/2` · ⛔ 未改账 / 活状态表 / 所有权表。
> 前置：`资料/完全复刻/报告/B141_W128_ENG11.md`（W36）§七 补丁①。

---

## 〇 一句话结论

两条映射**已补**（`othercardplayed` → `OtherCardPlayed` / `othercardspiritstone` → `OtherCardSpiritStone`），**零行数变化、零指纹变化**。
但 —— 🔴 **施工单的前提只对了一半**：补上之后 `TriggerNameOfKeyword("othercardplayed"/"othercardspiritstone")` 确实非 `null` 了，
**`ASH81` / `GSC14` 的 `ca 320` 仍然不会因此多跑一拍**。它们的 320 走的是**监听者路**
（`EffectResolver.cs:10657-10666`，`FireBoardListenerDeferred`），那里的比较式是
`sched == TriggerNameOfWhenKind(kind)`，**根本不经 `ExtraTriggerName` / `TriggerNameOfKeyword`**；而且
`TriggerNameOfWhenKind` 的 `Play → OtherCardPlayed`（`:7639`）与 `SpiritAbility → OtherCardSpiritStone`（`:7662`）**本来就在**。
真缺口是那条路**只发 `EmitFollowUpResolution`、没有 `FireTriggerByNumber(410)`**，修点在 `EffectResolver.cs`（本批白名单外）。

---

## 一 现读判据（全部本轮现读，⛔ 不是照抄简报）

| 项 | 现读值 | 出处 |
| --- | --- | --- |
| 引擎关键词 | `"othercardplayed"` | 施工单；`KeywordTable` **没有**对应常量（现读常量区无此词）⇒ 用字符串字面量 |
| 目标 trigger 名 | `"OtherCardPlayed"` | `CardDef.cs:1917` `{"OtherCardPlayed", 10}` |
| 引擎关键词 | `"othercardspiritstone"` | 同上（无常量） |
| 目标 trigger 名 | `"OtherCardSpiritStone"` | `CardDef.cs:1933` `{"OtherCardSpiritStone", 455}` |
| 映射方法 | `ActionPump.ExtraTriggerName` | 原 `:1894-1912`，`return null;` 在 `:1912` |

**为什么不加 `case` 到 `:1084-1122` 那张 switch**：那张表被账按行号引用（该段代码注释自述「现读 12 处」，本次未逐条重数），插行会让下游整体 +1
（类尾 §八 的注释已写死这条）。本次照既有风格走 `ExtraTriggerName` 的 `default` 兜底分支。

**为什么目标名不是别的写法**：`CardDef.AbilityTriggerNumber("OtherCardPlayed") = 10`、
`("OtherCardSpiritStone") = 455`（断言 A3/A4 钉着），与 W36 报告给的名字一致，无需改判。

---

## 二 改动清单

1. **`RuleEngine/Core/ActionPump.cs:1912`**（1 行，**行数 2657 → 2657 不变**）
   - 原（缩进 12 空格省略）：`return null;`
   - 现（同一物理行，缩进 12 空格省略）：`if (keyword == "othercardplayed") return "OtherCardPlayed"; if (keyword == "othercardspiritstone") return "OtherCardSpiritStone"; return null;   // ✅ 已复刻（2026-11-04 · B141 · …）`
   - 🔴 **为什么合进同一物理行**：本文件账落点最深到 `:2233`（现读 `资料/完全复刻/账/战斗逻辑驱动.tsv` 等，共 105 处引用），
     在 `:1912` 插行 ⇒ `:2015 / :2084 / :2091 / :2137 / :2170 / :2233` 等**全部漂移**。合并成一行 ⇒ **零漂移**
     （先例：`B95_WPUMP` 加 `requiem` 映射也是「加在既有合并行上 ⇒ 零行号漂移」）。
   - 行尾标记按铁律 20：`// ✅ 已复刻（2026-11-04 · B141 · <判据>）`，且写明「本映射对 ASH81/GSC14 零行为变化」。
2. **新建 `RuleEngine/Editor/B141_W39_Assert.cs`**（未接线；`RuleEngineTest.cs` 不在本批白名单）。
3. 新建本报告。

---

## 三 🔴 核心反证：本映射**不修** `ASH81` / `GSC14` 的 `ca 320`

### A 两条路是分开的

| 路 | 入口 | 判名依据 | 是否经过 `ExtraTriggerName` |
| --- | --- | --- | --- |
| **关键词路** | `RuleCore.FireTriggerDeferred`（`RuleCore.cs:13031-13036`） | `FollowUpSchedulerOf(card) == AbilityTriggerNameOfKeyword(keyword)` → `TriggerNameOfKeyword` | ✅ 是 |
| **监听者路** | `EffectResolver.FireBoardListenerDeferred`（`EffectResolver.cs:10657-10666`） | `FollowUpSchedulerOf(card) == TriggerNameOfWhenKind(kind, selfOnly)` | ⛔ **不是** |

`FireBoardWhenListener`（`:10397`）里监听者正文是**直接 `ResolveOps`**（`:10496` 入队 → `FireBoardListenerDeferred`），
**从不调 `FireTriggerAt`** ⇒ `FireTriggerDeferred` 这一趟对监听者**不存在**（W36 报告 §2·A 自己也是这么写的，只是 §2·B 的修法建议漏了这一层）。

### B `TriggerNameOfWhenKind` 这两支**本来就在**

- `case WhenEventKind.Play: return TN_OtherCardPlayed;` —— `EffectResolver.cs:7639`
- `case WhenEventKind.SpiritAbility: return "OtherCardSpiritStone";` —— `EffectResolver.cs:7662`
- 而 `FollowUpSchedulerOf`：`ASH81 → "OtherCardSpiritStone"`（`:7964`）· `GSC14 → TN_OtherCardPlayed`（`:7966`）
⇒ `:10662` 的比较**改前就成立**。

### C 「FollowUpResolution 0 → 1」这条施工要求**今天不成立**

`EmitFollowUpResolution` 的唯一闸 = `HasTriggerSource(card, "FollowUpResolution", …)`（`EffectResolver.cs:8078`），
而 `HasTriggerSource` 的语义闸是 `CardDef.AbilityTriggerPresence`（`:8367-8377`）。
现读 `cards_engine.json`：`GSC14.abilityTriggers = [10, 410]`、`ASH81.abilityTriggers = [410, 455]`
⇒ 两张卡的 410 presence **= 1** ⇒ 监听者路 `:10663` 那一跳**改前就在发**（0→1 应为 **1→1**）。
真正没跑的是 **410 那条触发器**（`FireTriggerByNumber(ctx, u, 410, …)`）—— 它只在关键词路 `RuleCore.cs:13035`
和 `RunFollowAfterResolutionRow`（`EffectResolver.cs:20679`）两处被调，监听者路没有。

### D 这两个关键词在**生产代码**里 0 个消费者

- 全 `Assets/**/*.cs` grep `othercardplayed|othercardspiritstone`：`ActionPump.cs` 1 处（本次新增）、
  `CardPresentation/Core/VfxMap.cs` 2 处（那是**反过来**用：把 trigger 名 `ToLowerInvariant()` 之后匹配，见 `:2336-2343`）、
  其余全在断言/报告里。
- 生成器 `工具/gen_cards_engine.py:1289-1311` 的 `ABILITY_TRIGGER_KEYWORD` 只有 **21 条**，**不含 10 / 455**
  ⇒ 卡数据永远不会把这两个词作为引擎关键词喂进来。
- 所有 `FireTriggerAt(...)` 的关键词实参要么是 `KeywordTable.*` 常量、要么由调用方透传同类关键词；
  全仓 `grep` 这两个字面量在 `Core/` 下**一处都没有**（仅本次 `ActionPump.cs` 新增的判定句）⇒ 没有生产者会把它们送进 `TriggerNameOfKeyword`。

⇒ 映射补上之后：**零行为变化**（`ruleprobe check` / `play check` 指纹逐字不变，见 §四）。它补的是
「映射层缺两格」（与 `spentallmana` / `courtesy` / `relentless` 那几格同类），**不是** ENG-11b 的修复。

---

## 四 验证读数（本机实测 2026-11-04 19:33–19:35）

| 命令 | rc | 读数 |
| --- | --- | --- |
| `TMPDIR=/tmp/wf_b141_w39 bash 工具/typecheck.sh` | **0** | `运行时错误数: 0` · `编辑器错误数: 0`（新增断言文件一起编过） |
| `bash 工具/ruleprobe.sh check` | **0** | `=== 全池 1134 张 · 解析差异 0 行 · 改名 0 行 · 新增 0 · 消失 0 ===` · `哨兵合计：失败 0 条` · 输出指纹 **`77a3c2bc…`**（与钉住一致） |
| `bash 工具/ruleprobe.sh play check` | **0** | ①-a 主档逐卡全等 · ①-b 电池逐卡全等 · ② 哨兵基线 **`a0e9c63d…`**（与钉住一致） |

⚠️ **没能跑的两条**（如实标着）：

1. **运行时「410 是否真的派了」** —— 需要宿主（`RuleEngineTest` / Unity）打一局，本批 ⛔ 不跑 Unity；
   `ruleprobe` 的 `play` 电池覆盖 `unit/tactic/defence/enchantment` 的「打出去那一拍」，**不含**「对手打战术 / 用灵魂石」这两个事件电池。
2. **`HasTriggerSource` 的实跑** —— 断言 B5/B6 已写，但**未接线、未运行**（同 W36 的断言文件处境）。

---

## 五 行数变化 / 账漂移

- `RuleEngine/Core/ActionPump.cs`：**2657 → 2657 行**（`b.count(b'\n')` 前后相同；LF 0 改 → 现读 `crlf 0 / barecr 0`）⇒ **账落点零漂移**。
  `git diff --numstat` = **`1 1`**（只换了一行）。`python -I -X utf8 工具/check_ledger_points.py` 的输出里
  **`ActionPump.cs` 0 命中**（全仓现存的 349 漂移 / 51 失败都是别处的既有账，与本件无关）。
- 新增 `RuleEngine/Editor/B141_W39_Assert.cs`：新文件，无账落点。
- 新增本文件。

---

## 六 还差什么（按优先级）

1. 🔴 **ENG-11b 的真修点**：`EffectResolver.cs:10657-10666`（`FireBoardListenerDeferred`）在
   `EmitFollowUpResolution` 之后补 `FireTriggerByNumber(ctx, u, 410, …)`（形参形状照关键词路 `RuleCore.cs:13035`：
   `actingCard: chosen ?? subject, targetCard: subject ?? chosen` 需按监听者路重新裁定两槽，⛔ 不要照抄）。
   落点文件 `EffectResolver.cs` —— **本批白名单外**，未动。
   修完之后才谈「320 跑了几拍」，且要配一条宿主断言（打一张 `ASH81` / 让对手打战术，数 410 的入队/结算）。
2. ⚠️ **`Editor/B141_W36_Assert.cs` 的 C6/C7 会红**：那两条是「`othercardplayed` / `othercardspiritstone` **必须为 null**」的钉子，
   本批补上映射之后**按设计变红**（W36 原文：「届时删掉本行并确认没有重复派 320」）。
   **本次已核 = 没有重复派**（§三·D 三条），⇒ 接线时把那两条删掉或改成「必须非 null」。
3. **`SW34` 的 `480`（ENG-11c）** 不在本件范围，仍欠（W36 §六·2 的顺序问题未定）。
4. **本断言文件待接线**（调度台在 `RuleEngineTest.cs` 聚合行**同一物理行、第一个 `//` 之前**追加）：
   `Guard("B141W39_Assert", B141_W39_Assert.Run); _pass += B141_W39_Assert.Pass; _fail += B141_W39_Assert.Fail;`

---

## 七 给调度台的处置建议

1. `B141-EXTRA-TRIGGER-NAME` 的**代码部分已交**（映射层补齐），但 **ENG-11b 的「320 真跑」≠ 已解决** ——
   ⛔ 别把本件当 `ASH81`/`GSC14` 的修复核销；建议单开一条：**「监听者路补 410」**，写手归 `EffectResolver.cs`。
2. 本件的账：`ActionPump.cs:1912` 这一行**没有被账引用**，`ExtraTriggerName` 那一格只在账备注里被点名
   （`战斗逻辑驱动.tsv:178/183` 引的是 `ActionPump.cs:1894` 这个方法头）。⇒ **不需要改账**；若要记一笔，
   建议落在「`ExtraTriggerName` 已覆盖 21 条关键词 + 2 条监听器名」的备注里，⛔ 不要新增落点行。
3. 断言接线时**连同 W36 的 C6/C7 一起处理**（见 §六·2）。
