# B89-E2c · R9 批次 10（手牌出口）+ E3 交回的 4 条 RuleCore 落点 —— 执行报告

- 代理：`B89` 第 1 波执行代理 `E2`（续派第 2 次）· 2026-10-28
- 授权文件：`RuleEngine/Core/RuleCore.cs`（**只这一个**；本批未动 `BoardSlots.cs` / `BattleContext.cs`）
- 账补丁：`账/_parts/B89_E2c.tsv`（8 行 = 表头 + 7 条，11 列 / LF / 无裸 CR 已校验）
- typecheck：`TMPDIR=/tmp/wf_B89E2c bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**

---

## §1 逐条结论

| 账行 | 结论 | 状态建议 |
|---|---|---|
| **L1185** `RemoveEffectsFromCard` | ✅ **已由 `B88-A2`（2026-10-28）建好并接线**：`RemoveHandEffectsCastBy(ctx, caster)`（`EffectResolver.cs:17381`，调用点 `RuleCore.cs:9859`）就是「按施放者反查 → 整条摘」。本笔复核后**无需再改** | **A** |
| **L1186** `RemoveFromGameCard` | ⛔ **未做**（本波上下文用尽）。现存只有「回合末 `ephemeral` 手牌自动消失」一条**内联**入口（`RuleCore.cs:2884-2933`），没有可复用的通用方法 | **B** |
| **L1187** `RemoveHandEffectAt(int)` | 🔧 **本笔新建**（`RuleCore.cs:15930`，类尾），转发到既有 `RemoveHandEffectFromHand` | **A**（附两条如实记：无调用方 · 今天恒空转） |
| **L934①** `TacticPlayed` | 🔧 **本笔改一行**（`RuleCore.cs:13409`）：巧技那一族 `HasFx` → `HasCurrentTrait`（`t.Has`） | **A** |
| **L64** `AdjustCardEffect` | ⛔ **未做**：E3 给的最小改法要 **`BattleContext` 加作用域量 `ResolvingExtrinsic`** ⇒ 那一格**不在我本轮的白名单**（只给 `RuleCore.cs`）。只改 `ApplyHandBuffs` 逐条 `ResolveOps` 是**半套**（`Extrinsic` 仍传不进 `DoGive → ApplyOneGain`）⇒ 按红线拒做半套 | **B** |
| **L869** `ResolveCardPlayed` | ⛔ **未做**（上下文用尽）。两笔都已定位、**无技术障碍**：① 正文结算后调 `TryFireCombo`（`EffectResolver.cs:17980`，`private` 但**同一个 partial class ⇒ `RuleCore.cs` 可调**）；② 同段 `u.RemoveAll("ephemeral", silent: true)` | **B** |
| **L870** `ResolveDamageDealt` | ⛔ **未做，且按红线停下**：三支（syphon / poisonous / concussive）**全挂在「伤害类型 4」这个未坐实的枚举值上**（E3 报告 §4·3 自己写明未坐实）⇒ 值不定就是猜。另 `concussive` 扩到「每条伤害路径」是改伤害面的大手术 | **B** |

---

## §2 每条怎么观测（照上一批格式）

| 账行 | 观测口 / 断言 / 探针 |
|---|---|
| **L1185**（无改动） | 夹具：一张手牌吃了某单位给的 buff → 那个单位翻面成残骸 ⇒ 该手牌的费用/数值修正**当场消失**（`CostOf` 回落印价）。反证：不调 `RemoveHandEffectsCastBy` ⇒ 修正留着（改前形状） |
| **L1187** | 造一条**带 `Uses`** 的记录 ⇒ `RemoveHandEffectAt(ctx, owner, 0)` 后 `ps.HandEffectRecords.Count` 减 1、`CostOf` 回落；反证：越界 index ⇒ 返回 0、表不变 |
| **L934①** | `play check` 对指纹（预期不变）+ `play all dump` 逐字节 diff。判别式：造一张「**只有运行时授予 artifice、正文里没有**」的夹具卡打巧技 ⇒ 改后**会**触发、改前不会 |
| **L1186 / L869 / L870 / L64**（未做） | 见 tsv 各自的「备注」列（每一步都写了读数口与反向对照） |

---

## §3 本会话 **`RuleCore.cs` 里会改指纹的改动清单**（调度台点名要的，含前两批）

> 口径：`Fingerprint` 读的是 `ctx.Events`（字符串日志）+ 局面哈希 ⇒ **只有写 `ctx.Log` / 改局面 / 改队列动作的才算**。
> 纯 `ctx.Emit(...)`（`EvtKind.*`）只进 `ctx.Signals` **不进** `ctx.Events` ⇒ 不改指纹。

| # | 批次 | 位置（现读行号） | 改了什么 | 会改指纹？ | 依据 / 隔离归因办法 |
|---|---|---|---|---|---|
| 1 | **E2（批 2）** | `RuleCore.cs:13274` | `TriggerCodex` 的 `FireTriggerAt` 加第 6 实参 `chosen: u` | 🔴 **会（本会话唯一高风险）** | `chosen` 同时喂「指代 it」与 `AbilityFamilyGateOpsForTrigger` 族闸 ⇒ 可能剔/留 op。隔离办法：只回滚这一行跑 `play check` 对比 |
| 2 | E2（批 2） | `RuleCore.cs:9795` / `:10255` | `DeadUnit` 加 `IsRemnant` 并灌值 | 🟡 **疑似** | `NetProtocol.Fingerprint` 会遍历 `ctx.DeadUnits`（`NetProtocol.cs:743`），但只读 `Card/Owner/DeathTurn` ⇒ **预期不读新字段**。核实办法：看 `NetProtocol.cs:743-750` 有没有逐字段展开 |
| 3 | E2（批 2） | 类尾 6 个新方法 | `PeekNextCards` / `IsThereValidAbilityTarget` / `AddResetDuty` / `ResolveResetDuty` / `GetCardsInCemetery` | ⛔ **不会** | 全部**没有调用方** |
| 4 | E2b（批 3） | `RuleCore.cs:12461` | `EmitBloodThirst` 补 ③④ 两闸 | 🟡 **可能**（改了「发不发 `EvtKind.Trigger`」） | 只发 `Signals`、不进 `ctx.Events`；但若某处把 Trigger 事件当判定信号就会变。隔离：只回滚这一行 |
| 5 | E2b（批 3） | `RuleCore.cs:2512` | 加 `EmitTurnEndCourtesy` / `EmitTurnEndResolution` | ⛔ **不会（预期）** | 两跳各只在 `AbilityTriggerPresence == 1` 时才调，**池内 0 张** |
| 6 | E2b（批 3） | `RuleCore.cs:2262` | 加 `BroadcastTurnStartedToHands` | ⛔ **不会** | 只 `Debug.LogWarning`（不进 `ctx.Events`），且池内 0 张 |
| 7 | **E2c（本批）** | `RuleCore.cs:13409` | 巧技闸 `HasFx` → `HasCurrentTrait` | ⛔ **不会（预期）** | 池内 0 分叉（带 artifice 的卡都有正文） |
| 8 | E2c（本批） | `RuleCore.cs:15930` | 新增 `RemoveHandEffectAt` | ⛔ **不会** | 无调用方 |

🔴 **给同步点的建议**：真要逐项隔离，**只需回滚 / 单开关 #1 与 #4**（#2 先读一眼 `NetProtocol.cs:743` 的字段展开即可判定）；#5–#8 都是「池内 0 可达」，不必单独跑。

---

## §4 没查清 / 为什么停下

1. **L1186 未做**是**预算**问题不是判据问题：通用「把某张手牌移出游戏」的口要新建（类尾），并把 `:2933` 那处内联改成调它 —— 后者**会动行号**（该处在 365 个账落点的上游）⇒ 要么同批重钉账、要么只建口不接线。我选择**不在没核对行号影响的情况下动它**。
2. **L869 未做**同上（预算）。技术障碍为零，`TryFireCombo` 因为是 `private` 但**同在 `partial class RuleCore`** ⇒ `RuleCore.cs` 里**直接可调**（这点值得写进下一个代理的简报，别再当它是障碍）。
3. **L870**：真正的拦路虎是 **「伤害类型 4」未坐实**（E3 报告 §4·3）。没有那个枚举值，三支**一支都不能写**。
4. **L64**：卡在「`BattleContext` 不归我」。E3 的方案需要一个新的作用域量；我这一轮的所有权表只给了 `RuleCore.cs`。
