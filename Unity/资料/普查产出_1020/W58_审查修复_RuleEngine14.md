# W58 审查修复：`RuleEngineTest.Run` 14 条

> 执行代理 **W58-REFIX** · 2026-10-20
> 输入判据：`资料/普查产出_1020/W58_诊断_RuleEngine14条.md`
> 白名单：`RuleEngine/Editor/RuleEngineTest.cs` · `CardPresentation/Core/VfxMap.cs` · `RuleEngine/Core/EffectResolver.cs`（仅注释）
> 红线遵守：未跑 Unity、未跑 `*.Run`、未动 git、未改正本。

## 结论

- 14 条里 **1 组真实现缺陷（3 条断言）** 已按诊断修掉：`VfxMap` 的「钉行」与「按能力序号筛行」先后反了。
- 其余 11 条全部是**测试侧**问题（断言过期 / 夹具前提不成立 / 断言算错），已逐条改断言或夹具，**没有为了让测试变绿而改期望值语义**。
- `TMPDIR=/tmp/wf_refix bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- 未跑 `RuleEngineTest.Run`（按派单要求）；解析类结论用 `ruleprobe seg` 复读（Spanner payload 5 段 / amount=5）。

---

## 11 真缺陷修法（11a / 11b / 11c）

**根因**（诊断 §11）：`TryResolveTriggerPicks` 先按能力序号筛（`FilterPicksByAbilityIndex` 自己从游标 0 重钉一遍），
`ResolveAllTimed` 随后又用**另一组** `pin[]` 从游标 0 再钉一次；`CardPick` 不带行号 ⇒ 第 2 条能力被钉回第 1 次出现的同名行，
拿到第 1 条能力的 `Offset`。

**做法**（`CardPresentation/Core/VfxMap.cs`）：

1. `TryResolveTriggerPicks` 的 ③ 之后**不再筛**（删掉 `FilterPicksByAbilityIndex` 调用，并删除该已无调用点的私有方法）。
2. `ResolveAllTimed` 在 `pin[]` 钉完之后新增「①·bis」：`evt == Trigger && evtAbility >= 0` 时，
   用**同一组 `pin[]`** 读 `times[pin[i]].Order` 第一段（`CardDef.TryParseAbilityIndexOfOrder`），
   只留 `== evtAbility` 的候选，并同步重建 `picks` / `pin` / `n`。
   · 钉不上 / `Order` 解不出 ⇒ 不算这一条能力（宁可留白，不多播）。
   · 筛完为空 ⇒ 与改前 `!hit` 分支同口径（`perCardOnly` ⇒ 空数组；否则保留原兜底）。
3. 文档注释同步订正（铁律 5），不再指向已删除的方法。

**按数据复核的三条期望**（`cards_engine.json` 的 `animTimes` + `card_vfx_by_card.json` 的候选顺序）：

| 卡 | 行（`Order` → `t`） | 能力 #1 | 能力 #2 |
|---|---|---|---|
| SOR56 Beacon of Faith | 1.0.0→0 · 1.0.1→1 · 2.0.0→2 · 2.0.1→3 | T = 0 / 1 | **T = 2 / 3** ✔ |
| SOR23 Canoness | 0.0.0→0 · 0.0.1→1 · 1.0.0→2.8 | T = 0 / 1（#0） | **T = 2.8**（#1）✔ |
| SOR67 Sacred Rose Sister | 1.0.0→0 · 2.0.0→1 | T = 0（#1） | **T = 1**（#2）✔ |

（三条卡 `rc = 0`，`rng` 过滤不改候选，钉行游标与 `times` 顺序一一对应。）

---

## 其余逐条（改前 → 改后 + 判据出处）

### 3 · P3-① 夹具前提不成立（`RuleEngineTest.cs:3946` 起）
- 改前：注释「两边棋盘都空」；断言 `LogCount("上面那一句已经结算了") == 1`（0 目标分支）。
- 改后：注释写明 `NewBattle` 必摆督军（`RuleCore.cs:549`）⇒ 走「目标≥1、只含能量」分支；断言改为
  `LogCount("只含能量 ⇒ 已按") == 1`（真实路径日志，`EffectResolver.cs:8064-8068`）。
- 同时按派单订正 B2 注释（`EffectResolver.cs:7939` 附近）：该 0 目标分支**真实对局基本不可达**，
  `P3-1`「两句日志会同时出现」不成立（0 目标时先 `return`，到不了 8067）。

### 7/8 · D8 夹具漏 `ranged: true`（`RuleEngineTest.cs:25662`）
- 改前：`DeclareAttack(ctx, 0, 3, 1, 5)` ⇒ 按近战结算 ⇒ 攻击者吃 4 点反击 ⇒ 第二次 `damaged` 广播。
- 改后：`DeclareAttack(ctx, 0, 3, 1, 5, ranged: true)`（形参默认 `false`，`RuleCore.cs:5195`）。
- 未动 `EmitHit` / `BroadcastWhen`（预调/排除实现本身正确）。

### 9/10 · 哨戒日志取词写错（`RuleEngineTest.cs:25825`）
- 改前：`LogIdx(ctx, "的 Sentry")` —— 那只是 `Hurt` 的 `source` 实参，只有盾/闪避/无敌挡下才进日志。
- 改后：`LogIdx(ctx, "Sentry 5：")` —— 引擎真正打的哨戒行 `Sentry {se}：{target} 反击了正在攻击它的 …`
  （`RuleCore.cs:5482-5483`）。

### 6 · `Slay: Draw a card` 灭自证手牌数（`RuleEngineTest.cs:24666`）
- 改前：期望 `hand0`（打出前取）。
- 改后：期望 `hand0 - 1` —— `PlayTactic` 会把打出的战术卡移出手牌（`EffectResolver.cs:1682`）；
  `CountSlaySignals == 0` 仍是「Slay 没响」的真判据。

### 5 · 抽 2 张部队 → 能量（`RuleEngineTest.cs:6396`）
- 改前：`Energy = energy0 - 1 + 2`（旧语义：回当前能量）。
- 改后：`Energy = energy0 - 1`（只扣本卡费）＋ `MaxEnergy = max0 + 2`
  （`W58-TURN-A`：`Gain N Energy` = 永久抬上限、当前不动；`EffectResolver.DoEnergy` / `DoGive` 能量支）。

### 2 · `EvtKind` 15 → 16（`RuleEngineTest.cs:15171`）
- 改后：期望 **16**，并补 `kinds.Contains("Heal")`（`BattleEvent.cs` 枚举实测 16 项，末项 `Heal`）。

### 4 · E10 后单位卡 at-turn 只在场结算（`RuleEngineTest.cs:23500` 起）
- 改前：DS8 压在牌库顶、靠抽进手牌触发 `turn_start`。
- 改后：DS8 直接 `Place` 到本方场上再 `BeginTurn`（棋盘那趟结算，`EffectResolver.cs:6060+`）；
  ② 反例保留「它在牌库里（不在场）⇒ 不结算」。未放宽 `IsHandTrap`。

### 1 · Spanner「random bonus」三项 → 五项（`RuleEngineTest.cs:11930`）
- 改前：要求 `melee && ranged && armour`。
- 改后：`Amount == 5 && melee && ranged && health && !armour`
  （`GOF12` SO 的 5 条 `abilityLogic`，`EffectText.cs:2818-2852`；`ruleprobe seg` 实测 payload 5 段、无 armour）。

### 12 · AI 出牌按目标打分（`RuleEngineTest.cs:27049`）
- 改前：合成 `Tactic("Zap", …)` —— 不在 `ai_scoring.json` 真值表 ⇒ `AiScoringData.Get == null` ⇒ 两个目标同分 0。
- 改后（诊断方案 2，未碰 `SimpleAI.cs`）：改用池内真实战术卡 **`AM42 Pinning Fire`**
  （`scoringOption=3` 伤害分 · `targetsAffected=30` · `targeted=1`），并给动作补 `TargetP=0` / `TargetSlot`
  （与 `SimpleAI.EnumerateActions` 的战术卡口径一致）。负例（`ScoreOps` 非目标同分）保留合成卡。
- 预期：Squishy（1 血，2 伤致死）`3.0` > Tank（9 血，2/9 部分伤）`≈0.4`。

---

## typecheck

```
TMPDIR=/tmp/wf_refix bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

行尾：`RuleEngineTest.cs` / `EffectResolver.cs` 纯 CRLF，`VfxMap.cs` 纯 LF；均未翻转（二进制计数核对）。
