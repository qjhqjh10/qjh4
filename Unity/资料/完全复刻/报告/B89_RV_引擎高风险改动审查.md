# B89 第 1 波 · 引擎高风险改动独立审查（`RV`）

- 审查代理：`RV`（只读、只交报告）· 2026-10-28
- 范围：本轮 4 处「会真实改变结算行为」的引擎改动
- 方法：先读原版（`decomp_full/*.c` 为主），再核我们的实现与调用点；未读作者推理，结论自己找源
- ⛔ 未跑 Unity / ruleprobe / typecheck（按红线）；⛔ 未改任何 `.cs` / 账 / 正本

> **本机判据铁律**：序列化数据里不含字段名，按名字 grep = 0 命中不能当否定证据；本 build 代码在 `il2cpp` 节。以下每条都给「原版 `文件:行号`」。

---

## 总评：4 处：对 3 / 错 0 / 存疑 1

| # | 改动 | 结论 | 一句话 |
|---|---|---|---|
| 1 | 删攻击者身上 Mob(300)/Regiment(301) 两发 | **对**（有 4 条不改变今天行为的残余） | `.c:34` 的 `op_Equality(acting,this)` 分支在 `:105/:109/:112` **return**，攻击者确实不跑自己的 Mob/Regiment；同方其余那一路真的接上了 |
| 2 | 虫群合并 `MaxHealth` → `Health` | **对** | 原版取 `+0x68 = currentHealth`（不是 `+0xa4 = currentMaxHealth`）；无漏项 |
| 3 | `L1262` 补 `ctx.IsPendingDeath(t)` | **对** | 原版闸 = `CardScript.IsInPlay()`（`{2,15,3,17}`），`5` 被拒；没多加 `IsAlive`/`IsRemnant`；闸在目标校验层，与原版同层 |
| 4 | extrinsic 切段 | **存疑（机制对、覆盖面不全）** | 作用域量成对、嵌套安全、今天逐字节不变；但原版 `extrinsic` 在**4 个分支**被读，我们的切段只喂到**其中 1 个** |

---

## 1. `RuleCore.cs` 删掉攻击者 Mob/Regiment —— **对**

### ① 原版那支 return 的条件与范围
`CardScript__ResolveUnitAttacked.c` 形参 = `(this=接收者 param_1, acting=攻方 param_2, target=被打者 param_3, attackType param_4)`（`:22` 预调被打者、`:29` 预调攻击者、`:53` 棋盘循环印证）。

- `:34 cVar1 = op_Equality(param_2, param_1)` = **攻方 == 本接收者**。
- `:35 if (cVar1 != 0)` 这一大段（`:36-125`）走完：`:96-101` 若攻方 `!EnoughPendingDamageToDie` ⇒ `OnTrigger(0x244=580 Strike)`；`:103-112` 判 `HasCurrentTrait(0x4b0=Ambush)` 及其状态 ⇒ `SetAmbush(false)`。
- **每一条正常出口都在 `:105` / `:109` / `:112` `return`**，另有 `goto LAB_1805fa71e`（`:115`，非返回桩）。⇒ **攻方在这支里绝不落到 `:127` 的 Mob/Regiment 段**。**无例外分支**（`:127` 起是 `else` 语义，`:34` 为真就走不到）。
- 「同方其余」在 `:127-146`：`:128 isPlayer(acting) != isPlayer(this) ⇒ return`（同方闸）；`:131 param_4==1` ⇒ `OnTrigger(300 Mob)` + `HasCurrentTrait(0x398)||HasEnchantment(300)` 才 `BroadcastUnitMob`；`:147-168` `param_4==2` ⇒ `OnTrigger(0x12d=301)` + `HasDefaultTrait(0x4c9)` 才 `BroadcastUnitRegiment`。
- 广播器 `BattleManagerSupport__BroadcastUnitAttacked.c`：`:22` 预调被打者 → `:29` 预调攻方 → `:38/:44` 棋盘循环（排除攻方与被打者）→ `:58-81` 双方手牌两趟。

**结论**：作者「攻击者本人不跑自己的 Mob/Regiment、同方其余各跑自己的」= 原版。✔

### ② 「同方其它单位」那一路真的还在（追了调用点）
- 实现：`EffectResolver.cs:18174 FireMobOrRegimentOnSameSide(ctx, actor, attackType, includePendingDeath)`，逐格扫同方 `Board`、`ReferenceEquals(u,actor)` 跳过攻方、按 `PlayOrder` 排、`FireTriggerAt(u, kw)`。
- 调用点：`EffectResolver.cs:9967`（在 `BroadcastWhenCore` 内、在 `listeners.Count>0` 块**之外** ⇒ 没有棋盘监听者也照跑）← `BroadcastWhen`（`:9842`）← `RuleCore.cs:6982 BroadcastWhen(ctx, WhenEventKind.Attack, …, actor: attacker, …)`。
- `WhenEventKind.Attack` 的 `BroadcastWhen` 全仓**只有 `RuleCore.cs:6982` 一处**（`:6975/:6978` 是 `PreCallBoardListener`，不走这条）⇒ 不会多触发。
- `atkType`（`EffectResolver.cs:9896`）：调用方没给就 `actor.CurrentAttackType==Ranged?2:1`；`Attack` 且 `actor≠null` ⇒ 恒为 1/2，`FireMobOrRegimentOnSameSide` 的 `kw!=null` 必成立。
- 删除完整：`RuleCore.cs:6870-6960` 范围内只剩 `:6874 FireTriggerAt(attacker, Strike)`，**无残留 Mob/Regiment 攻击者发射**。

### ③ 账 `L556` 是不是同一件事
`战斗逻辑驱动.tsv:556`（`ResolveTriggerRegiment`，原 **A**，落点 `FireTriggerAt` + 调用点 `RuleCore.cs:6926`）——
- 原版 `BattleManager.ResolveTriggerRegiment`（`BattleManager__ResolveTriggerRegiment.c:226 → CardScript__TriggerRegiment()`）是**队列动作解析器**（action `0x58`），唯一生产者 = `AbilityLogic__PlayAbility.c:1713 AddTriggerRegiment`（buffType `0x19b=411`，即「某条能力去触发 X 的 Regiment」）。与被删的**攻击驱动**那一跳（`CardScript.ResolveUnitAttacked`）**不是同一条链**。✔ 作者「账里归错了类」成立。
- **现存问题（需跟进）**：该行落点仍写着「调用点 `RuleCore.cs:6926`」，而 `:6926` 现在是 `ctx.Emit(EvtKind.Attack,…)`，**该调用点已不存在**（落点漂移/失效）。E14 的补丁 `_parts/B89_E14.tsv` 已把该行改 **B** 并写明理由 —— 合并主表后需确认落点不再指 `6926`。
- **同族待核**：`战斗逻辑驱动.tsv:179`（`AddTriggerMob`，**A**）与 `:553`（`ResolveTriggerMob`，B）的落点证据同样引用了 `DeclareAttack` 那处 **已删除** 的攻方发射（`:6719`/`:10242`，现读已漂到哨戒/死亡登记）。⇒ 与 L556 同源，建议一并复核 A/B（E14 自己也标了「同一族同理复核」）。

### 残余（都不影响今天的观感，但如实列出）
1. **三槽不同**：原版 `OnTrigger(300, this, acting=攻方, target=被打者)`；我们 `FireTriggerAt(u, kw)` 默认 `subject=u`（acting=自己、target=null）。**今天不可观测**：现读 16 张 `mob` + 13 张 `regiment` 的正文全是自含句（「Gain +1 Attack」「Deal damage to a random enemy」「Create … in your hand」「Give +1 Health to adjacent units」），**没有一条引用 `it` / `the target of the attack`**。将来若有卡引用，会指错。
2. **次序**：原版逐接收者「UnitAttack(50) → Mob/Regiment」交错；我们是「整趟棋盘监听器跑完」再「整趟 Mob」。`FireMobOrRegimentOnSameSide` 的 doc 已如实标着。
3. **同侧被打者 + 待死**：原版 `:22` 预调被打者**不带 `IsInPlay`**；我们对被打者也套 `includePendingDeath=false`。只在「同侧自打 + 被打者已在 state 5」这一格可差（极窄）。
4. **`atkType` 是广播时现推**（`EffectResolver.cs:9896`）而非 `RuleCore.cs:6982` 传 `attackType: ranged?2:1`（`ranged` 在其作用域内可用）。原版 `param_4` 是 `_ResolveAttack` **锁定**后一路透传；我们在哨戒/星镖两段衍生伤害**之后**才推 ⇒ 若这两段触发了改 `CurrentAttackType` 的效应，会与锁定值分叉。**建议**：`RuleCore.cs:6982` 显式传 `attackType: ranged ? 2 : 1`，去掉现推这个可漂的源（窄，但不是不可能）。

---

## 2. 虫群合并 `MaxHealth` → `Health` —— **对**

- 原版 `CardScript.__c__DisplayClass401_0___ExecuteSwarm_b__0.c`：`:24/_0x48 = CurrentMeleeAttack(just)` · `:27/_0x50 = CurrentRangeAttack(just)` · `:31-41` 读 `*(just+0x68..0x78)` 经 `ObscuredInt.op_Implicit` 解密后写 `*(drawer+0x60)`；`:45 CardScript__AddEffect(host, drawer, -1)`。
- 字段真值：`EntityScript__get_currentHealth.c:8-12` = `+0x68`；`EntityScript__get_currentMaxHealth.c:8-12` = `+0xa4`。⇒ **`+0x68` 就是 currentHealth**。✔ 作者取 `just.Health` 正确（`UnitState.cs:59 public int Health` 是存档字段 = 当前生命；`MaxHealth` 是投影）。
- **无漏项**：原版只写 attack / range / health 三格（`+0x48/+0x50/+0x60`），**不含护甲、不含上限夹取**；`+0x38=1` = buffType `addStats(1)`。我们 `host.Health += just.Health`（当前）+ `AddStatMod("attack"/"ranged"/"health", …, SwarmSrc)`，而 `AddStatMod` 默认 `BuffType=1`（`UnitState.cs:1923`）⇒ 与 `addStats` 同档、且 `"health"` 修饰进 `MaxHealth` 投影 ⇒ 当前/上限同时 +，与原版 `AddEffect` 一节等价。✔

---

## 3. `L1262` 补 `ctx.IsPendingDeath(t)` —— **对**

- 现读：`RuleCore.cs:11354 var t = ctx.Players[targetOwner].Board[targetSlot]; if (t == null || ctx.IsPendingDeath(t)) return ErrTarget;`
- **原版闸**：`BattleManager__IsValidActiveAbilityTarget.c:21-28` —— 候选 `op_Equality(param_3,0)` 走 null 支；否则 `:28 CardScript__IsInPlay(param_3)` 为假 ⇒ `return 0`。`CardScript__IsInPlay.c` = `cardState ∈ {2, 0xf, 3, 0x11}`。`RawCardScript__IsValidActiveAbilityTarget.c:102` 再判一次同一 `IsInPlay`。
- `IsInPlay` **不含 `5`**（对比 `CardScript__IsInPlayOrDying.c:10-11` 才把 `5` 也算上）⇒ 待死态应被拒。我们的 `IsPendingDeath` = `_pendingDeaths` 里那张（`BattleContext.cs:1741`）= `DeferDeaths` 窗口内仍占 `Board[slot]` 的死者 ≈ state 5。✔
- **没多加**：`:11354` 只有 `t == null || IsPendingDeath(t)`，**没有** `!t.IsAlive` / `t.IsRemnant`。✔
- **层次**：`CanUseAbilityTarget` 的调用点只有 `CanUseActiveAbility`（`RuleCore.cs:15331`，目标校验）与 `IsThereValidAbilityTarget`（`:15642`，exists 包装）—— 都是**目标校验层**，与原版 `IsValidActiveAbilityTarget` 同龄，**不是**结算前/广播前的另加闸。顺序也一致：null → IsInPlay → 潜行 → ward → summonRift → tainted（原版 `:21/:28/:47/:51/:55/:57`）。✔

---

## 4. `ApplyHandBuffs` / `ResolveHandBuffOps` 的 extrinsic 切段 —— **存疑（机制对、覆盖不全）**

### ②③ 作用域量：成对 + 嵌套安全 —— 对
`RuleCore.cs:15945-15962`：每段 `bool prev = ctx.ResolvingExtrinsic; ctx.ResolvingExtrinsic = x; try { ResolveOps(...) } finally { ctx.ResolvingExtrinsic = prev; }`。
- **成对**：`finally` 兜住异常；`ops.Count==0` 提前 return **在设之前**；`ResolveOps` 内部早退也在 `finally` 之后复位 ⇒ **不存在漏复位的早退路径**。✔
- **嵌套**：若 `ResolveOps` 期间再进 `ResolveHandBuffOps`，内层 save/restore 会把外层值原样恢复 ⇒ 不会把外层的 `true` 泄漏给后续 gain。✔
- 下标对齐：`ops` 与 `ext` 只在 `e.Op != null` 时**同刻各加一条**（`:10549`）⇒ 一一对应成立。✔

### ① 覆盖面：**不等价** —— 原版 `extrinsic` 读在 4 个分支，我们只喂到 1 个
`CardScript__AddEffect.c` 里 `+0x30` 被读的点：
| 行 | 分支 | 语义 | 我们是否建模 |
|---|---|---|---|
| `:86` | `HasCurrentTrait(source,0x82) && extrinsic ⇒ return 0` | **resistant × extrinsic** 拒挂 | 仅 `AttachHandEffect` 的 `AddEffectCasterGates(extrinsic)` 近似；`ApplyOneGain` 这条没覆盖 |
| `:91-130` | `ContainsExtrinsicEffectFrom / RemoveExtrinsicEffectsFrom` | **外来源效果的去重/替换** | ❌ 未建模 |
| `:521` | `effectA.extrinsic==0 && effectB.extrinsic==0` 才合并 | **合并键**：extrinsic **永不与** intrinsic 合并 | ❌ 未建模（`ApplyTraitEffect` 的 `FindTraitEffect` 合并键没带 extrinsic） |
| `:728` | `buffType==1(addStats) && extrinsic ⇒ Σ(+0x60)` | **外来源加属性时的当前生命重算** | ❌ 未建模 |

⇒ 我们把 `ctx.ResolvingExtrinsic` 只喂给 `EffectResolver.cs:12010` 的 `u.ApplyTraitEffect(extrinsic: …)`（**记录来源旗标**那一支），**其余三支没接**。作者把 L64 判 **B**（非 A）是对的；但简报若把它当「同分支已闭环」则不成立。

### 为什么今天仍逐字节不变（可信）
`AttachHandEffect` 的全部 6 个生产调用点（`EffectResolver.cs:5749 / 5915 / 5975 / 6089 / 6293 / 10850`）**extrinsic 实参全 `false`**（⚠️ `:6293` 的 `true` 是第 7 位 `dedupSamePayload`，不是 extrinsic）；`AttachHandEffectCopy`（`:5842/:5856`）只复制 `src.Extrinsic`（源即 false）⇒ `HandEffect.Extrinsic` **今天恒 false** ⇒ 只有一段、内容/次序不变。✔ 作者「无生产者」成立。

**建议（若要真闭环 L64）**：把 `:521` 合并键、`:91-130` 的去重/替换、`:86` 的 resistant 闸也改成读 `ctx.ResolvingExtrinsic`（或按 op 逐条携带 extrinsic），否则一旦出现 `Extrinsic=true` 的生产者，这四处会静默地「按 intrinsic 处理」。

---

## 未做 / 未覆盖（如实）
- ⛔ 未跑 typecheck / ruleprobe / Unity（红线）。
- 只核了「原版方法体 vs 我们的落点」，**未**验证 `AbilityLogic.PlayAbility` 的 buffType 410/411 在我方引擎是否有生产者（影响 L553/L556 的 A/B 终值，见 §1③「同族待核」）。
