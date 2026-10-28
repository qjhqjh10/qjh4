# B89-E14 · `RuleCore` 收尾（L64 / L869 / L1186 / L870 ＋ 追加三件）—— 执行报告

- 代理：`B89` 第 1 波执行代理 `E14`（接 `B89-E2c` 空出的槽位）· 2026-10-28
- 授权文件：`RuleEngine/Core/RuleCore.cs` · `RuleEngine/Core/BattleContext.cs` · **`RuleEngine/Core/EffectResolver.cs`（调度台 2026-10-28 中途放开，见 §1 末尾「动了 EffectResolver 哪些符号」）**
- 账补丁：`账/_parts/B89_E14.tsv`（11 列表头 / LF / 无裸 CR）
- typecheck：`TMPDIR=/tmp/wf_B89E14 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**

---

## §1 逐条结论

| # | 账行 / 事项 | 原版判据 | 改动（文件:符号名 + 现读行号） | 状态建议 | 会改指纹？ |
|---|---|---|---|---|---|
| 1 | **L64** `AbilityLogic.AdjustCardEffect` 的 extrinsic 支 | `CardScript__AddEffect.c:469-568`（合并键含 `extrinsic`）· `CardEffect.extrinsic // +0x30` | **① 管道**：`BattleContext.cs:2104` 新增 `public bool ResolvingExtrinsic;`（作用域量，save/restore）；`RuleCore.ResolveHandBuffOps`（`RuleCore.cs:15939`）= `ApplyHandBuffs` 的唯一出口，把合并后的 op 列表按「extrinsic 相同」切**连续段**、逐段设该量再 `ResolveOps`；`ApplyHandBuffs` 在 `:10550` 调用它（`:10524` 那句收 `ext` 与 `ops` 按下标对应）。**② 消费端**：`EffectResolver.ApplyOneGain` 的 `extrinsic: false` → `extrinsic: ctx.ResolvingExtrinsic`（`EffectResolver.cs:12010`）。 | **B**（**手牌这一族已闭环**；剩 **光环那一族** —— `Auras.ApplyAura` 授 trait 走 `t.AddAuraKeyword(...)`/`t.RecordGrant(...)`、不建 `TraitEffectRecord`，那在 `Aura.cs`，不在本批白名单） | ⛔ 不会（`HandEffect.Extrinsic` **没有生产者** —— `AttachHandEffect` 全部调用点传 `false` ⇒ 恒 false ⇒ 只有一段 ⇒ 与改前逐字节相同） |
| 2 | **L869** `CardScript.ResolveCardPlayed` ②④ | `CardScript__ResolveCardPlayed.c:1-112`（② Combo(590) → ③ ResolveUnitSummoned → ④ `RemoveTraitSilently(5)`） | `RuleCore.ApplyPlayedCardTailEffects`（`RuleCore.cs:15979`）：`TryFireCombo(ctx,p,card,inst,chosen)` ＋ `unit.RemoveAll("ephemeral", silent:true)` ＋ 清 `CardInstance.EphemeralMarked`（🔶 自己设计，理由见代码注释）。调用点 = `RuleCore.cs:4091`（**追加在 `ctx.NotePlayed(...)` 那一行**，零行数变化），位置夹在「Play 广播」与「Deploy 广播」之间 = 原版 ② 在 `OnCardPlayedWithTarget` 之后、`ResolveUnitSummoned` 之前。 | **B**（②④ 已落；原版 ④ 里还有 `StopReflected` 一支**未核**，见 §4） | ⛔ **池内零可达**（`Combo(590)` 触发 0 张；`ephemeral` 104 张但场上单位不读它）⇒ 预期不改 |
| 3 | **L1186** `PlayerHand.RemoveFromGameCard` | `PlayerHand__RemoveFromGameCard.c`（壳）+ `_RemoveFromGameCard_d__NN__MoveNext` 逐跳 · `PlayerHand__RemoveCardFromHand.c:20` · `CardScript__GoToCemetery.c:14-21`（trait 5 ⇒ 跳过 `AddToCemetery`） | `RuleCore.RemoveCardFromHandToRemoved(ctx,p,c,reason,header)`（`RuleCore.cs:16017`，public）；**接线** = `SweepEphemeral`（`RuleCore.cs:2889`，调用行 `:2934`）改成走它。日志次序/文案/`ctx.Removed` 字段**逐字保持**。 | **A** | ⛔ 不会（逐字节等价） |
| 4 | **L870** `CardScript.ResolveDamageDealt` 的 syphon / poisonous | 🥇 `DamageType.cs`（枚举桩）= `Attack 4` · `BattleManager__ResolveAttackDamage.c:28` 的**字面量 4** · `CardScript__ResolveDamageDealt.c:33-62` | `RuleCore.ResolveDamageDealtHook`（`RuleCore.cs:16078`），由 `HurtCore` 在 `ApplyDamage` 之后调（`RuleCore.cs:9042`，追加在已有行上）。闸 = `actingCard != null ∧ dealt > 0 ∧ kind ∈ {Melee,Ranged}`（= 原版 type 4）。**concussive 未搬**：已有内联实现（见 §4） | **B**（2/3 支落地且**池内 0 张**；concussive 的剩余差额见 §4） | ⛔ 不会（`syphon 0 张` / `poisonous 0 张` ⇒ 恒不进） |
| 5 | **追加①** `RuleCore.cs:6904/:6926` 多出来的一跳（协调台转 `E3b`） | `CardScript__ResolveUnitAttacked.c:34` 的 `op_Equality(param_2,param_1)`（**攻击者==本接收者**）走「50→580→伏击」段、`:112` `return` ⇒ **攻击者本人不跑自己的 Mob/Regiment**；`:127-146` 才是「**同方其余**卡各跑自己的」 | **删掉** `RuleCore.DeclareAttack` 里那两发 `FireTriggerAt(attacker, Mob/Regiment)`（原 `:6889-6913` ⇒ 现 `RuleCore.cs:6889` 起的一段注释 + 空位）。正确落点 = `EffectResolver.FireMobOrRegimentOnSameSide`（`E3b` 建，已由 `BroadcastWhen(Attack)` 自动调） | **代码已改**；**账 L556 建议 A→B**（理由见 §3①） | 🔴 **会**（`mob` 16 张 / `regiment` 13 张可达） |
| 6 | **追加②** L1105 类「同方**手牌**单位响应」 | `BattleManagerSupport__BroadcastUnitAttacked.c` 的**第 4 跳**（双方手牌各一趟 `ResolveUnitAttacked`） | **未做**（判据不足，见 §4③） | — | — |
| 7 | **追加③** 虫群合并血量（`R1b` §2-1） | `CardScript.__c__DisplayClass401_0___ExecuteSwarm_b__0.c:13-40`（`+0x68 currentHealth`） | `RuleCore.cs:4606` `just.MaxHealth` → **`just.Health`** | **A** | 🔴 **会**（只有「带伤虫群合并」这一格变） |
| 8 | **追加④** L1262 `CanUseAbilityTarget` ② 闸 | `CardScript.IsInPlay()` = `cardState ∈ {2,15,3,17}` | `RuleCore.cs:11354` `t == null` → **`t == null \|\| ctx.IsPendingDeath(t)`**（⛔ 没加 `IsAlive` / `IsRemnant`） | **A** | 🟡 只在 `DeferDeaths` 窗口内可观测 |
| 9 | **追加⑤** L866 回手/洗回牌库重置 | `_ResolveRecallToDeck…:108/:177/:276` · `_ResolveRecallToHand…:188/:255` 全传 `clearBacklash=1` | **未做** —— 第 1、2 跳在 **`UnitState.cs`**（`AlreadyTriggeredBacklash` 字段 + `ResetToValuesInHand` 里清位），而那**不在本批白名单**（见 §4④） | — | — |

### 我这轮动了 `EffectResolver.cs` 的哪几个符号（协调台要求单列）

**只有一个**：`EffectResolver.ApplyOneGain`（private static）里 `u.ApplyTraitEffect(...)` 的实参 `extrinsic: false` → `extrinsic: ctx.ResolvingExtrinsic`（现读 `EffectResolver.cs:12010`）。**没有新增/删除任何成员**，文件仍是 CRLF（`CRLF 18206 / LF 18206 / bareCR 0`）。下一棒写手只需知道：`ApplyOneGain` 现在依赖 `BattleContext.ResolvingExtrinsic`（`BattleContext.cs:2104`）。

---

## §2 typecheck 读数

```
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```

（中途出现过 3 条**白名单之外**的错 —— `CardPresentation/Core/TMProWarpText.cs:148/149` 的 `Bounds.extent`、`CardPresentation/Shell/BaseEventPopup.cs:558` 的 `LiveOpsLocalCatalog.GetAssetReference` —— 都是别人在飞的活，我**没有改**；最后一次跑已全部消失。）

---

## §3 顺手发现（⛔ 未擅自扩大改动，交调度台分流）

1. 🔴 **账 `L556` 的 `A` 站不住（照 `.c` 现读）**：`L556` 是 `BattleManager.ResolveTriggerRegiment(BattleAction)` ——
   那是**队列动作解析器**，由 `AbilityLogic.PlayAbility:1713` 入队（`AddTriggerRegiment`，动作号 `0x58`），
   语义 = 「某条能力去触发 X 的 Regiment」，**与攻击驱动的那一条不是同一条链**。
   而账里写的调用点 `RuleCore.cs:6926` 是**攻击驱动**那一跳，它属于原版
   `CardScript.ResolveUnitAttacked.c:127-146`（另一个方法）⇒ **调用点归错了**。
   ⇒ 建议：`L556` **A → B**（`FireTriggerAt` 那个原语对，但「队列动作入队那一跳」我们没有生产者）；
   攻击驱动那一支已由 `E3b` 的 `FireMobOrRegimentOnSameSide`（账 `L645`）承接。
   ⚠️ 同一族 `BattleManager.ResolveTriggerMob` 若也有 `A` 行，同理复核。
2. **`RuleCore.cs:6886-6888` 的旧注释已过期**（还写着「下面的 `TakeExtraTrigger` 那一段」）——
   消费点 `2026-10-22` 就挪进 `FireTriggerDeferred` 了。我这一批**没有改它**（不在我这几条账的范围），
   留给下一棒 `.cs` 写手就地订正（铁律 5）。
3. **`EffectResolver.cs:9967` 的 `FireMobOrRegimentOnSameSide` 只覆盖【棋盘】** ——
   原版 `BroadcastUnitAttacked` 的第 4 跳（**双方手牌**）它没有做（`E3b` 自己在 doc 里也标了）。见 §4③。
4. **`KeywordTable` 里没有 `syphon` / `poisonous` 的具名常量**（现核 0 命中）⇒ 我的 `ResolveDamageDealtHook`
   用字面量并注明。要不要补常量由卡牌线定（`SimpleAI.PoisonousKeyword = "poisonous"` 是 AI 侧的私有常量）。

---

## §4 没查清 / 为什么停下（⛔ 不许当结论）

1. **L64 光环那一支（`Auras.ApplyAura`）没做**：`Aura.cs` 不在白名单。E3 的补法 ② 要求把
   `t.AddAuraKeyword(...)` 换成 `t.ApplyTraitEffect(..., extrinsic: true, source: GrantTag)`，
   **但必须先核** `AddAuraKeyword` 有没有光环专属簿记（`Auras.Recompose` 的整份收回靠的是它自己那本账）——
   若有就要**两本一起写**。这一条**没查**（属 `Aura.cs` 写手）。
2. **L870 concussive 的剩余差额**：我们的 concussive **已经在 `DeclareAttack` 里**（带
   `StunStillOnBoard` 残骸落点，`A1176`/`A1224` 修过的 bug，⛔ 别搬），覆盖「主攻击」这一条 type-4 路径。
   原版还有**反击**那条 type-4 路径（`_ResolveAttack…:1137` 也走 `ResolveAttackDamage` ⇒ 类型 4、
   `actingCard` = 反击方），我们那次 `Hurt(...)` **既没传 `actingCard` 也没传 `kind`** ⇒ 反击方的
   concussive/syphon/poisonous 不触发。补齐要**同时**决定「反击的 `Hurt` 该不该打 `kind=Melee/Ranged`」
   ——那会连带改**成就** `Ach_2/Ach_3` 的计数面（跨面）⇒ 本笔不擅自做。
3. **追加② 手牌那一跳没做**：`BroadcastUnitAttacked` 的第 4 跳要在**双方每一张手牌**上跑
   `ResolveUnitAttacked`（触发 `UnitAttacked(50)` / `Mob(300)` / `Regiment(301)`）。
   **缺什么**：手牌卡在我们这边**只有 `CardInstance`、没有 `UnitState`**，而 `RuleCore.FireTriggerAt`
   的形参就是 `UnitState`。手牌上现存的唯一通道是 `CardDef.HandTrapWhens`（`When …: …` 那种**数据链**，
   走 `BroadcastHandWhen` / `BroadcastHandTrapWhen`），**abilityTriggers 型的手牌能力没有执行口**
   —— 这与 `RuleCore.NoteHandTurnStartedNoChannel`（trigger 160 的手牌能力）是**同一个缺口**，
   全仓已有一处出声口。⇒ 要做得先建「手牌自身 abilityTriggers」那条执行口（属效果层），
   ⛔ 别在 `FireMobOrRegimentOnSameSide` 里塞假 `UnitState`。
   ⚠️ 另：协调台提到的「`L1105`」在 `账/战斗逻辑驱动.tsv` 第 1105 行是 `MinionManager.GetAdjacentUnits`，
   **不是这条** —— 编号可能指别的账本，我按**语义**记录（本报告的「追加②」）。
4. **L866 没做**：三跳里**第 1 跳（`UnitState.AlreadyTriggeredBacklash` 字段）与第 2 跳
   （`ResetToValuesInHand(bool clearBacklash)` 里清位）都在 `UnitState.cs`**，本批白名单没有它。
   第 3 跳（`RuleCore` 置位 / 闸）与第 4 跳（`EffectResolver.DoReturn` 调重置）依赖第 1 跳的字段 ⇒
   三跳**必须同一个写手一次做完**（否则半套）。判据与四步原文 → `报告/B89_R1b_悬案落地与账订正.md` §2-3。
   ⚠️ 调度台已在 2026-10-28 裁定「按 `.c` 清 `+0x222`」，那条口径冲突**已解除** —— 只差文件所有权。
5. **L869 的 `StopReflected`（反射术）没核**：原版 ④ 的第一句是 `StopReflected`，我**没有**在
   `CardScript__ResolveCardPlayed.c` 之外追它（反射术在我们引擎里有没有对应物未查）。

---

## §5 交接给下一个代理

### 5.1 我这轮新建 / 改过哪些符号（别重复建）

| 文件 | 符号 | 行 | 可见性 | 用途 / 账行 |
|---|---|---|---|---|
| `RuleCore.cs` | `ResolveHandBuffOps` | **15939** | private static | L64 的切段器（`ApplyHandBuffs` 唯一出口） |
| `RuleCore.cs` | `ApplyPlayedCardTailEffects` | **15979** | private static | L869 ②④ |
| `RuleCore.cs` | `RemoveCardFromHandToRemoved` | **16017** | **public** static | L1186 通用出口；调用点 `SweepEphemeral`(`:2934`) |
| `RuleCore.cs` | `ResolveDamageDealtHook` | **16078** | private static | L870 syphon / poisonous；由 `HurtCore` `:9042` 调 |
| `BattleContext.cs` | `ResolvingExtrinsic` | **2104** | public 字段 | L64 作用域量（**写点只有 `ResolveHandBuffOps`**；读点只有 `ApplyOneGain`） |
| `EffectResolver.cs` | `ApplyOneGain`（实参） | **12010** | — | `extrinsic: ctx.ResolvingExtrinsic` |

### 5.2 🔴 行号漂移（同步点必须重钉账）

`RuleCore.cs` 从 **15,952 → 16,124 行**（净 **+172**）：

- **中段净 −22**：`SweepEphemeral` 改走新口 **−13**（`2916-2948` → 18 行）· Mob/Regiment 那两发删掉 **−9**（`6889-6913` → 16 行注释）。
  ⇒ **`RuleCore.cs` 里 >2916 的账落点行号整体上移 13**、**>6889 的整体上移 22**（实证：`E2c` 那条 `RemoveHandEffectAt` **`15930 → 15908`** —— 现读已核）。
- **类尾 +194**（四个新方法 + 注释，全部在 `NoteHandTurnStartedNoChannel` 之前），它在文件最尾 ⇒ 不影响任何既有落点。
- ⇒ **同步点请跑 `check_ledger_points --all-A --fix`（在没有 `.cs` 写手的窗口）**。本笔没有动主表。

### 5.3 待接点（都在**别的文件**里落）

1. **L64 光环那一半**（`Aura.cs` · `ApplyAura`）—— 先核 `AddAuraKeyword` 的簿记，再决定「换口」还是「两本一起写」。
2. **L870 concussive 的反击路径**（`RuleCore.DeclareAttack:7169` 那次 `Hurt` 要不要传 `actingCard`+`kind`）—— 跨成就计数面，要调度台裁。
3. **L866 三跳**（`UnitState.cs` 字段 + 清位；`RuleCore` 置位/闸；`EffectResolver.DoReturn` 调重置）。
4. **手牌 abilityTriggers 执行口**（§4③）—— 建了它，`BroadcastUnitAttacked` 第 4 跳与 `TurnStarted` 手牌那一跳一起解锁。
5. **`RuleCore.cs:6886-6888` 的过期注释**（§3②）。

### 5.4 同步点要跑的读数（本笔按红线**没跑**）

- 🔴 本笔**有 3 处会改指纹**：**Mob/Regiment**（`mob` 16 张 / `regiment` 13 张可达）· **虫群带伤合并**
  （`just.Health`）· **`CanUseAbilityTarget` 的待死闸**（`DeferDeaths` 窗口内）。
  ⇒ `bash 工具/ruleprobe.sh play check` 前后指纹 + `play all dump` 逐字节 diff。
- **`RuleEngineTest.Run`**：`TestMob*` 那两条断言（Mob 不再打在攻击者身上）与虫群断言都要复跑。
- 若 `RuleEngineTest` 红：先判 **(α) 断言写的是旧行为** / **(β) 我这笔真错**，⛔ 别拿「改成让断言过」当修好。
