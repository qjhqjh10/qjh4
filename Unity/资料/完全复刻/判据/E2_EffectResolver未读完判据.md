# E2 · EffectResolver/目标解析族「判据未读完」16 条（2026-10-28 · 只读代理，调度台落盘）

> 判据第一权威 `decomp_full/*.c`，第二 `out_cpp2il_dev_ilspy`。行号均为本轮现读。

## §判决汇总

| 账行 | 原版方法 | 结论 |
|---|---|---|
| L64 | `AbilityLogic.AdjustCardEffect` | **②可做**（缺口在 `RuleCore.ApplyHandBuffs:10562-10572` 丢 `Extrinsic` + `Aura.cs:883/897` 不建 `TraitEffectRecord`） |
| L74 | `AbilityLogic.GetRefCard` | **②可做**（加 `RefCardOf(ctx)` 取 `[0]` + 补 valueOptions 100/110/120） |
| L76 | `AbilityLogic.GetTargets(6)` | **已一致（核销）**；41 档已逐档判过，`ta=190/230` 在现池 0 行 |
| L79 | `AbilityLogic.NewBuffStat` | **已一致（核销）**；可达面只有 `Normal`，其余 17 档休眠 |
| L80 | `AbilityLogic.PlayAbility` | **已一致（核销本行）**；分支缺口落 86 行索引（A20/B47/C19），各支另有账 |
| L381 | `BattleManager.GetTargetInPos` | **②可做**（`TargetReticle.ResolveAim` 重写：boardCamera + `TouchPosition` + `200f` + `BoardElements` 掩码 + 三档返回） |
| L436 | `BattleManager.IsValidSpellTarget` | **②可做**（`EffectResolver.cs:2196 IsLegalPlayTarget` 补 `Targeted != 1 ⇒ false` + 当前回合方闸） |
| L490 | `BattleManager.ResolveAction` | **②可做**（91 型映射表 + 回填 `QueuedAction.ActionType`，现恒 0） |
| L503 | `ResolveClearEndOfTurnEffectsFrom` | **已一致（核销）**；入队者已证 = `CardScript.OnTurnEnd.c:150-202` 内联（不是 `SendClearEndOfTurnEffects`） |
| L521 | `BattleManager.ResolveJamUnit` | **②可做**（在 `EffectResolver.cs:16106` 插一行 `ActionPump.CancelEffectsFromDisappearingUnit(...)` —— 那个口已建但 0 调用点） |
| L546 | `ResolveTransformFromRemnant` | **②可做**（补 `CheckStun` 等价位；trait 250 名待定） |
| L853 | `CardScript.RemoveEffect` | **②可做**（附魔 quest 支 `cardType 0x28 ∧ spellType 0x78 ∧ !fromJam`；45 档攻击半无字段） |
| L879 | `SendClearEndOfTurnEffects` | **原版自身不可达**（三源 grep 0 调用点）⇒ 核销，不是缺口 |
| L880 | `SendRemoveEffect` | **②可做**（5 个调用点 priority 对齐：1/1/2/3/`+0xb8`） |
| L1122 | `MinionManager.GetNextSlotToCard` | **②可做**（`createAtPos` 三处接线；算法已逐句等价） |
| L306 | `BattleManager.ExecuteNextAction` | **②可做（小）**（回放游标越界补出声） |

**计数**：②可做 **11** · 已一致核销 **4** · 原版不可达 **1** · 判据仍空 0。

## §反对清单（账要订正）

1. **L76 账列③「`your Warlord and adjacent units` 邻居漏放」不成立** —— `EffectResolver.cs:1766-1815` 相邻支确实对每个邻居 `list.Add(u)`，返回 `list`（`:1823-1825`）。若真有该形状缺陷，应在 `AnchorInSet`/解析层。
2. **L381 账列「还差 VA 反汇编读求交系数」过期** —— `B89-R1b` 已解出 `-1f` / `200f` / `1e-5`，本轮 `.c` 现读同样拿到；真缺的是**表现层相机/方向接线**。
3. **L546 落点又漂了** —— `DoReanimate` 现读在 `EffectResolver.cs:15489`（账写 15379）。
4. **L521 落点指到 doc 注释** —— `ResolveJamUnit` 声明行现读 `EffectResolver.cs:16043`（账写 15933 = 方法区头注释）。
5. **L879 仍被当生产点** —— `SendClearEndOfTurnEffects` 三源 0 调用点；真正生产者是 `CardScript.OnTurnEnd`（`decomp_full/CardScript__OnTurnEnd.c:150` 写 `0x3d`、`:205` `AddAutoActionToQueue(…,3)`）内联 ⇒ 本行应改指 `OnTurnEnd`。
   ⚠️ **2026-10-28（B91 铁律 5 就地订正）**：原句写的 `CardTurn.OnTurnEnd` —— **全库没有 `CardTurn` 这个类**（B91_L4b 现核），真名是 `CardScript`。主表落点已按 `RuleEngine/Core/RuleCore.cs:2300（EndTurnCore）· :2342-2357` 改正。
6. **L853 附魔 quest 支不该排高优** —— `spellType==120` 池内 **0 张**，是休眠档。
7. **L1122 需补口径** —— `createAtPos` 在 `cards_truth.json` 里**有 71 处**，只是 DTO 未带出 ⇒ 属「数据可达、接线缺失」，不是「数据本身没有」。
8. **L490 行数口径** —— ILSpy 21939-22450 是**失真的 500 行**；Ghidra `.c` 里同一张 switch 是 `:688-:9249`（约 8,500 行）⇒ 逐型对账以 `.c` 为准。

## §未做（如实记）
- L880 另 3 个调用点（`UsedActiveAbility` / `ResolveUnitAttacked` / `RemoveOwnExtrinsicEffects`）对应的我方位置**未定位**。
- L546 的 trait `250` 名称未在 `dump.cs` 展开。
