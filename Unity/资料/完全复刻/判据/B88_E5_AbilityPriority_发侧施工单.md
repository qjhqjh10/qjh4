# 施工单 · `EvtKind.Ability` 带上 `CardAbility.priority`（发侧，给 E1 / E2）

> 由来：账 `特效动画:486`（`BattleManager.AddCardAnim` 的四道 AutoAction 队列）。
> 表现侧**已经做完了**（第四批，执行代理 `E5`）：`BattleEvent.Priority` 列 + `BattleDriver` 的三处排期已带
> `lane = e.Priority`（`BattleDriver.cs:19558` 主信号 · `:19724` 展开的每一拍 · `:19745` 空拍）。
> **缺的只有「发」这一半** —— 那两处在 `RuleCore.cs`(E1) / `BattleContext.cs`(E1) / 可能还有 `EffectResolver.cs`(E2)。

## 一、为什么需要它（判据，逐条）

1. 原版 `BattleManager.AddCardAnim(…, int priority, …)` 的两处 `AbilityLogic.PlayAnim` 调用点传的是
   **`AbilityData.priority`**（`decomp_full/AbilityLogic__PlayAnim.c:223` 的 `*(undefined4 *)(param_3 + 0x44)`，
   `param_3 = AbilityData`）。
2. `AbilityData.priority` 由 `RawCardScript.TriggerAbility` 从 **`CardAbility.priority(+0x18)`** 灌入
   （`decomp_full/RawCardScript__TriggerAbility.c:32`：`uVar11 = *(uint *)(param_2 + 0x18)`）。
3. 主动技那一支是**固定 1**（`decomp_full/RawCardScript__ResolveActiveAbility.c:47` 的 `SetNewData(…, CONCAT44(uVar7,1), …)`）。
4. 缺省 = **1**（`CardAbility__.ctor.c:5` 写 `+0x18 = 1`）—— 所以我们 `CardDef.DefaultTriggerPriority = 1` 是对的口径。

## 二、要做的一共三处（按顺序）

### 步骤 1（E1）`BattleContext.Emit(...)` 加一个形参
`RuleEngine/Core/BattleContext.cs:1938`（`public void Emit(EvtKind kind, int player, int slot, string cardId, …)`）：

```csharp
                                     int abilityIndex = -1, int priority = 0)
```
并在它构造的 `new BattleEvent { … }` 里补一行 `Priority = priority,`。

⚠️ **默认 0 = 改前行为**（所有老调用点不动 ⇒ 零回归）。
⚠️ 另一个重载 `Emit(BattleEvent e)`（`:1866`）不用动 —— 它收的是整对象。

### 步骤 2（E1）四处 `ctx.Emit(EvtKind.Ability, …)` 各加一个 `priority:` 实参

| # | 位置 | 属于哪一支 | 传什么 |
|---|---|---|---|
| ① | `RuleCore.cs:11621`（`UseAbilityCore`） | 主动技 | **`priority: 1`**（判据 = 上面第 3 条：主动技那一支固定 1） |
| ② | `RuleCore.cs:11789` | 誓约（Oath） | **`priority: 1`**（同走 `ResolveActiveAbility` 那一支） |
| ③ | `RuleCore.cs:12047`（`UseAlternativeCore`） | 替代行动 | **`u.Card.AbilityTriggerPriority(<这条替代能力的原版 trigger 名>)`**；认不出 trigger 名 ⇒ **传 1** 并在 `ctx.Log` 里出声一次（`CardDef.AbilityTriggerPriority` 的缺省契约本来就是 1，见 `CardDef.cs:1896`） |
| ④ | `RuleCore.cs:15425` | ⚠️ **请 E1 先确认它属于哪一支**（文件里这一处与 ① 的形状不同：`effect: "activeAbility"`）。判法同上：主动技固定 1；能对上 `abilityTriggerPriority` 的按键就按数据。 |

⚠️ **`u.Ability` 是 `EffectSpec`，不是 `CardAbility`** —— 本仓的 priority 有两本字典（`TriggerPriority(keyword)` = 引擎关键词
/ `AbilityTriggerPriority(name)` = 原版 `AbilityTrigger` 枚举名），**别混用**（`CardDef.cs:1841/1896` 那两段注释写清了分工）。
⚠️ 认不出时**必须出声**（`ctx.Log` 会改 `Events.Count` ⇒ **动 `NetProtocol.Fingerprint`** —— 若这条路上不允许动指纹，
改成 `Debug.Log` 并在报告里写明）。

### 步骤 3（E2，只有需要才做）
`EffectResolver.cs` 里**没有**直接发 `EvtKind.Ability`（现读 0 处）⇒ **本步大概率不用做**。
唯一要确认的是：能力正文结算过程中若**间接**经过 `RuleCore.UseAbility*`，那三处已在 E1 的清单里。

## 三、验收（零回归是硬要求）

1. **不填 = 0**：所有没传 `priority:` 的 `Emit` 仍 0 ⇒ 表现侧 `AnimLaneRank(0) = 3`、并列仍「先来先播」⇒ 与批 2 之后逐字等价。
2. `ruleprobe play check` 的**指纹应当不变**：`NetProtocol.Fingerprint` 读的是 `ctx.Events.Count`（本仓多处注释写死了这条），
   **`BattleEvent` 新增字段不改条数** ⇒ 指纹稳定。（若 E1 的实现在指纹里真读了字段，请回执说明。）
3. 接上之后的可观测点：同一条 `at` 上「打断道(2) 的能力动画」应先于「瞬发(1)」，再先于「延迟(3)」，最后是「玩家(0)」。
   自检口现成：`BattleDriver.AnimLaneRanksForTest()`（`BattleDriver.cs:36343` 一带）应与 `AnimLaneRank` 的取值一致。
