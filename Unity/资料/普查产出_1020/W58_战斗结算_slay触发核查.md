# W58 战斗结算 · Slay 触发核查（Haarken Worldclaimer / Helspear Assault）

> 只读调查（W58-INV-SLAY）。未改任何代码/资产，未跑 Unity / `*.Run`。
> 用了纯 dotnet 的 `工具/ruleprobe`（`card` / `play` / `seg`）做静态之外的对拍。
> ⚠️ **行号会漂**（本会话里 `CardPresentation/Battle/BattleDriver.cs` 有别的写手在动，实测同一符号半小时内位移 61 行）——
> 引用以**符号**为准，行号只作辅助。

---

## 〇、结论（先直接回答）

**1）我们的实现：只对了一半。**
`Slay` 的判定口与发射口存在（`RuleCore.TryCreditKill`，判据逐条照 `CardScript__ResolveDeadCard.c:242-263` 落的），
但它**只在「攻击主目标」与「效果伤害的目标」上被调用**：
- **攻击直接击杀主目标 → 触发**（正确）；
- **同一次攻击里 Blast 溅射打死的相邻单位 → 一个都不记击杀 ⇒ Slay 不触发**（用户描述的「爆炸又消灭两个部队」这一半，我们**没实现**）；
- **易伤导致主目标死亡 → 触发**（正确，因为易伤被折进同一次 `Hurt`）；
- **易伤导致被 Blast 溅射的相邻单位死亡 → 不触发**（同上，被 Blast 那一格吞掉）；
- **同一结算死 N 个 → 我们只记 1 次**（主目标那一次）；**原版每死一个记一次**。

**2）还有一个更靠前的缺陷：Helspear Assault 在我们这儿根本没给战将 Blast。**
`GivePayload.ParseIntoCore` 的「嵌入效果头后缀匹配」分支（`Core/GivePayload.cs:507-537`）
遇到 `"Blast 2 and Slay: Deploy a Black Legionary"` 这种「普通关键词 + and + 嵌入关键词」的载荷时，
会把 `and` 前面那半**静默丢掉**，只挂 `Slay`。
实测（ruleprobe `play 61046837c330b48978b4af5201230b6e`）：打完 Helspear 后战将关键词 = `{slay1}`，
**没有 `blast2`**，日志也只有「挂上『slay：deploy a black legionary』」——**用户场景里的「爆炸」在我们这儿不会发生**。
同一根因还打坏了 `Ferocious Rage`（`+1 fist` 丢）与 `'Uge Choppa`（`+2 Attack` 丢）——实测同样只剩 `{slay1}`。

**3）解包资料：这套机制完整存在（数据 + 代码都齐）。**
- 数据：Helspear 的 `buffEffect` 明确写着 `buffType=3(addTrait) · trait=350(blast) · traitValue=2 · untilEndOfTurn=1`，
  以及 `buffType=15(singleUseEnchantment) · enchantment.m_PathID=196526527010153435 · untilEndOfTurn=1`；
  该 enchantment 的 `cardAbilities[0].trigger=120(Slay)` · `abilityLogic[0].chosenAbility=70(summonUnit)`（部署一个友方部队）。
- 代码：`AbilityTrigger.Slay=120`、`BattleActionType.triggerSlay=89`、`AbilityEffect.triggerSlay=467`、
  `CardScript.ResolveDeadCard` 的 Slay 闸、`CardScript.TriggerSlay`、以及「死亡动作的 actingCard = 伤害来源」这条链路。

---

## 一、原版语义与证据（判据 = `d:/2/tools/decomp_full/`）

### A1 攻击结算顺序与死亡链路

`BattleManager._ResolveAttack_d__438__MoveNext.c` 逐跳（顺序与我们的 `DeclareAttack` 注释一致）：

| 顺序 | 段 | 位置 | 打谁 / 死亡类型 |
|---|---|---|---|
| ① | 星镖 Shuriken | `:686` | 目标；`ReceiveDamage(target, 0x14 = ability 20)` |
| ② | 哨戒 Sentry | `:715` 段（真正落血在 `:1421`）| 攻击者；`ReceiveDamage(attacker, 0x14 = ability 20)` |
| ③ | 攻击触发族（`ResolveUnitAttacked`）| `:988` | —— |
| ④ | **主伤害** | `:999` | 防御方；`ReceiveDamage(target, 0xf = combatDefender 15)` |
| ⑤ | **爆裂 Blast** | `:1030` → `BattleManager__ResolveTraitAdjacentDamage.c:18-24` | 目标**相邻**单位；`DamageInfo(source=攻击者, type=6 Blast)` → `ReceiveDamage(victim, 0xf)` |
| ⑥ | 践踏 Stomp | `:1089` → `BattleManager__ResolveTraitStompDamage.c` | 相邻随机一个；`type=0xb Stomp` → `ReceiveDamage(victim, 0xf)` |
| ⑦ | 反击 | `:1142` | **攻击者**；`ReceiveDamage(attacker, 10 = combatAttacker)`（source = 防御方） |
| ⑧ | 标记光 Markerlight | `:1290` | 目标；`type=0 Normal` → `ReceiveDamage(target, 0x14 = ability 20)` |

死亡链路（关键：**击杀归属由「死亡动作的 actingCard」承载**）：

1. `CardScript__CheckIfDead.c:124` 生命归零 → `cardState = 5(waitingToDie)`；
   `:135/:141` 调 `TriggerUnitBacklashActions(this, deathType, damageInfo.actingCard)`。
2. `CardScript__TriggerUnitBacklashActions.c:44-50` 造一个 `BattleAction(actionType=0xd backlash)`：
   `targetCard = 死者`、**`actingCard = DamageInfo.actingCard（= 伤害来源）`**、`deathType` 原样带过去（`:49`；
   `ResolveDeadCard` 里读的就是这个字段，见下）。`:181` `AddAutoActionToQueue`。
3. 动作队列结算 → `BattleManager._ResolveBacklash_d__451__MoveNext.c`：
   `:88 CardScript__ResolveDeadCard(死者, action)` → `:94 TriggerUnitDeathActions` → `:144 BroadcastDeadUnit(action)`。
4. `BattleManagerSupport__BroadcastDeadUnit.c`：对**场上每张牌 + 双方手牌**各调一次
   `CardScript__ResolveDeadCard(观察者, 同一个 action)`（`:147/:206/:262/:369`）。

> ⚠️ `ResolveDeadCard` 的 `param_1` 是**观察者**，不是死者；死者是 action 的 `targetCard`（`local_308`），
> 伤害来源是 action 的 `actingCard`（`uStack_210`）。这一点决定了下面对 `:242-263` 的读法。

### A2 Slay 的判定条件（`CardScript__ResolveDeadCard.c:242-263`）

```
:242 if (targetCard /*死者*/ != this /*观察者*/) {
:244     RemoveExtrinsicEffectsFrom(死者, 伤害来源)
:248     if (actingCard /*= 伤害来源*/ == this && this.cardState != 5 /*waitingToDie*/) {
:251       if (死者.cardType != 10 /*督军*/ && (deathType == 0xf /*combatDefender*/ || deathType == 0x14 /*ability*/)) {
:256         if (this.isPlayer == IsPlayerTurn(bm) && 死者.isRemnant(+0x65) == 0) {
:258-260       RawCardScript__OnTrigger(this.rawCard, 0x78 /*AbilityTrigger.Slay*/, ..., 死者, 伤害来源, 死者)
```

⇒ **「击杀归属」= 那条致死伤害的 `DamageInfo.actingCard`**（不是「本回合谁打死过人」的累计）：
- **Blast 溅射**：`ResolveTraitAdjacentDamage` 用 `DamageInfo(actingCard = 攻击者)` 打相邻单位 ⇒ **算**（deathType=15）。
- **Stomp 溅射**：同上，`actingCard = 攻击者`、deathType=15 ⇒ **算**。
- **易伤**：`CardScript._ReceiveDamage_d__381__MoveNext.c:188-257` —— 易伤是**同一时刻的第二发独立伤害**
  （`:226` 造 `DamageInfo(actingCard = 原伤害的 actingCard /*攻击者*/, 值 = CurrentVulnerable, 类型 = 7 或 8)`，
  `:252-254 DealMultiDamage`）；`GetDeathType` 对 type 7 返回 20、type 8 走 `:258-372` 返回 15/10 ⇒ 典型档 = 15 ⇒ **算**。
  ⚠️ **原版的「易伤」不是延后伤害**；真正延后的毒（poison）走 `DestroyUnit(..., deathType = 40)` ⇒ **不算**。
- **反击打死攻击者**：deathType = 10（`_ResolveAttack…:1142`）⇒ **不算**；且 `this.isPlayer == IsPlayerTurn` 那道闸
  （防御方在当前回合方眼里通常也不成立）会再挡一次。
- **战术卡伤害**：`GetDeathType` 对 spell 类型 actingCard 返回 30（`:140-147/:249-256`）⇒ **不算**；且战术卡不在「场上那批牌」里，无人可当触发主体。
- **督军死亡**：`cardType == 10` 排除；**残骸**：`+0x65 isRemnant != 0` 排除。
- **「强行触发某某的 Slay」** 是另一条路：`AbilityEffect.triggerSlay = 467` → `AbilityLogic__PlayAbility.c:1903`
  `BattleManager__AddTriggerSlay`（action 89）→ `ResolveTriggerSlay` / `ResolveAction case 0x59`：
  `IsInPlayOrDying` 时调 `CardScript.TriggerSlay()`。**它不是「这一下算不算击杀」的判定口**，别混。

### A3 时机与次数

- 死亡触发是**每个死者一个 backlash action**（`TriggerUnitBacklashActions` 里入队一条），
  `BroadcastDeadUnit` 也是**每次死亡各广播一轮**（`_ResolveBacklash_d__451__MoveNext.c:144`）。
- 每个死者 → 观察者（= 凶手的牌）各命中一次 `OnTrigger(Slay)`。
- 数据里**没有「一回合一次 / 每次攻击一次」的旗标**：Helspear 的 enchantment `cardAbilities[0] = {trigger:120, abilityContext:0, priority:1}`
  （`资料/普查产出_1018/allcards_1350/cards_raw.json`，`m_Name = Z-NC BlackLegion-Helspear Assault_enchantment`）。
- `singleUseEnchantment(15)` 这个 buffType **不会**在 Slay 触发时自动「花掉」：
  `EntityScript__SpendEnchantment` 的调用点只有 `AbilityLogic__PlayAbility.c:2504`（0x118 AboutToAttack / 揭示指令那条）
  与 `CardScript__AddEffect.c:896/:917`（Rage / MarkOfChaos 那几支），**Slay 这条路上一次都没有**。
- ⇒ **同一次结算死 3 个 = Slay 触发 3 次**（每次死一个就入一条 backlash action、广播一轮）。

> 另有一条与本题无关但容易看错的：Helspear 这种 `trigger=120` 的附魔在**加入时**会把宿主身上**已有的**其它 Slay 附魔移除
> （`CardScript__AddEffect.c:856-886`，`buffType 0xc/0xf` 走 `AddEnchantment`）——即「Slay 附魔不叠加」，但**不限制触发次数**。

### A4 方法体可用性

本次用到的关键方法体**全都在**（非 `.c` 缺失）：`ResolveDeadCard` · `TriggerSlay` · `TriggerUnitBacklashActions` ·
`BroadcastDeadUnit` · `ResolveTraitAdjacentDamage` · `ResolveTraitStompDamage` · `GetDeathType` · `ReceiveDamage(MoveNext)`。
未走 VA 反汇编；没有「查不到」的判据。
⚠️ 唯一没钉死的是**哨戒那一下的 `DamageInfo.actingCard` 到底是谁**（伪代码里寄存器复用严重，见 §六）。

### A5 解包资源里有没有这套机制：**完整存在**

| 编码物 | 位置（`资料/普查产出_1018/allcards_1350/cards_raw.json`） | 值 |
|---|---|---|
| Blast 2 | `Z-NC BlackLegion-Helspear Assault`（`uniqueId = 61046837c330b48978b4af5201230b6e`）的 `cardAbilities[0].abilityLogic[0].buffEffect` | `buffType=3 addTrait` · `trait=350`（`DefinedTrait.blast`）· `traitValue=2` · `untilEndOfTurn=1` |
| Slay 附魔 | 同卡 `abilityLogic[1].buffEffect` | `buffType=15 singleUseEnchantment` · `enchantment.m_PathID=196526527010153435` · `untilEndOfTurn=1` |
| Slay 触发 + 载荷 | `Z-NC BlackLegion-Helspear Assault_enchantment`（`path_id=196526527010153435`） | `cardAbilities[0].trigger=120`（`AbilityTrigger.Slay`）· `abilityLogic[0].chosenAbility=70`（`AbilityEffect.summonUnit`）· `targetCriteria: friendly minions, maxTargets=1` |
| 战将侧 | `BlackLegion_war_Haarken Worldclaimer`（`uniqueId=BL35`） | `abilityCount=2`：`trigger=46 Talent → logic 100 createCardInHand`；`trigger=31 TurnStartPlayer → logic 40 buffSelf`；`CardTraits=[talent]` |

⇒ **不是「只有数据没有代码」，也不是「只有代码没有数据」**：数据把 Blast 2 与 Slay 附魔分别编码，
代码在死亡广播里按「actingCard + deathType」挑凶手触发 Slay。

> ⚠️ 一处**别被带偏**的数据：该 enchantment 记录的 `CardTraits` 写的是 `{"name":"strike","_trait":1110}`，
> 与 `cardAbilities[0].trigger=120(Slay)` **不一致**。以 **ability trigger 为准**（与卡面文字 `Slay:`、
> 与 `AbilityTrigger.Slay=120` 都对得上）；`CardTraits` 那一栏在这条记录上像是模板/模板注册用的，
> 本次**没有追它的用途**（不影响本结论）。

---

## 二、我们的实现与证据

### B1 Slay 的判定口 / 发射口

`RuleCore.cs:4587-4615` `TryCreditKill`（注释逐条对上了 A2 的六道闸）：
杀手非空、非自己、存活、在场（`FindSlot`）、**属于当前回合方**；死者确实死、非督军、非残骸；
然后 `:4609 FireTriggerAt(ctx, killer, KeywordTable.Slay, kOwner, kSlot)` + `BroadcastWhen(Kills)`。
**全仓 `KeywordTable.Slay` 的发射点只有这一处**（grep 全 `RuleEngine/` 仅命中它 + 测试引用）。

### B2 调用点（= 哪些伤害会记账）

| 调用点 | 覆盖的伤害 |
|---|---|
| `RuleCore.cs:5251`（`DeclareAttack` 尾段）| **主攻击目标**（无论它是被星镖 / 主伤害 / 标记光 / 易伤打死——最后统一在这里判一次）|
| `EffectResolver.cs:2005`（`DoDeal`）| 效果 `deal` 的**每个目标** |
| `EffectResolver.cs:2414`（每个友方单位各开一火的 `DoDamageEach`）| 每个被选中目标 |
| `EffectResolver.cs:2961`（`DoDestroy`）| 效果「摧毁」 |
| `RuleCore.cs:9415`（`ResolveEffect` 的 `damage`）| 封闭文法的效果伤害 |

### B3 **没有**调用点的伤害（= 缺口）

- `RuleCore.cs:5065-5075` **Blast 溅射**：只有 `Hurt(ctx, au, blast, …)`，**没有 `TryCreditKill`**。
- `RuleCore.cs:5095-5106` **Stomp 溅射**：同上。
- `RuleCore.cs:5119-5135` **反击**：只 `Hurt(attacker)` —— 这里**应当**不记账（原版 deathType=10），正确。
- `Hurt`（`RuleCore.cs:6732`）→ `RemoveIfDead`（`:6858`）→ `CleanupDeaths`（`:7032`）/ `FlushDeaths`（`:7497`）
  里**没有任何击杀记账**：`killer` 形参只用于猎杀标记（`:7197-7219`）。
  ⇒ 所以「某条伤害没在调用点写 `TryCreditKill`」= 那条路**一定**不触发 Slay，没有兜底。
- 攻击段的死亡在「同时伤害」批里延后（`using (SimultaneousDamage(ctx))` 开在 `RuleCore.cs:4862`、
  闭在 `:5167`；Blast/Stomp/反击/标记光都在批内），批末 `FlushDeaths` 按 (方, 格号) 处理 ——
  但主目标那一次记账是**批外**的 `:5251` 单发。

### B4 易伤

`RuleCore.DamageAfterReduction`（`:5370-5451`）把易伤折进**同一个** `Hurt` 的返回值
（`VulnerableDamagePart` `:5314`，`max(1, 易伤 − 护甲)`，且带原版 `:210-211` 的「主伤害没打死才落」闸）。
所以：
- 主目标被易伤那一份打死 ⇒ `target.IsAlive == false` ⇒ `:5251` 记账 ⇒ **触发**（与原版一致）。
- **Blast 受害者**被「Blast + 易伤」打死 ⇒ 仍在 B3 那个没记账的循环里 ⇒ **不触发**。

### B5 Helspear 的授予（**这里有一个前提级缺陷**）

1. 文本解析：`EffectText` 产出**一条** `gain`，载荷 = `"blast 2 and slay: deploy a black legionary"`
   （ruleprobe `card "Helspear Assault"` 实测）。
2. `DoGive`（`EffectResolver.cs:7637`）→ `GivePayload.Parse`。
3. `GivePayload.ParseIntoCore`：
   - `:507` 先进「**嵌入效果**」分支（因为载荷里有 `:`）；
   - `:520-528` 从**后往前**试「头能不能被 `KeywordTable` 整段吃掉」：`"Blast 2 and Slay"` → `"2 and Slay"` → `"and Slay"` → **`"Slay"` 命中**；
   - `:529-537` 直接 `ops.Add(new PayloadOp{ Embedded = "slay: deploy a black legionary" })` 然后 **`return true`**
     ——**`"Blast 2 and"` 这一段被丢掉**，而 `:540` 的「按 `and` 拆分」分支根本轮不到。
4. 实测（ruleprobe，纯 dotnet）：
   - `play 61046837c330b48978b4af5201230b6e`（Helspear）→ 战将 `{slay1}`（**无 `blast2`**），日志只挂 Slay；
   - `play 84d4d192e0f57a34a90ac0f6172c3358`（`Ferocious Rage`）→ 战将 `{slay1}`，**攻击力 2 不变**（`+1 fist` 丢了）；
   - `play 6899b199f5e36144ebbdd7ed9d9e7afe`（`'Uge Choppa`）→ 战将 `{slay1}`，**攻击力 2 不变**（`+2 Attack` 丢了）。
   （对照：`play AM24` 的棋盘显示 `{armour1, blast2, vanguard1}` —— 证明显示层会打出 `blast2`，不是显示问题。）
5. 现有断言 `RuleEngineTest.cs:1236-1256` 只断「嵌入的 `Slay:` 认出来了」，**没有断 `+1 fist` 这一段还在**
   ⇒ 这条断言对上面三张卡的丢段**全绿**（典型「只测了后半句」）。

### B6 表现层

引擎发 `EvtKind.Trigger`（`FireTriggerAt`，`RuleCore.cs` 内 `ctx.Emit(EvtKind.Trigger, …, keyword)`），
`BattleDriver` 在 `case EvtKind.Trigger: PlayTraitPulse(e)`（**按符号定位**；本会话看到它在 16197 附近）统一消费；
`VfxMap` 把 `Strike/Slay → trigger` 粗桶。**表现层没有 Slay 专属缺口** ——
问题是引擎侧根本没为 Blast 溅射死者发这个事件。

### B7 现有断言 / 夹具覆盖

| 用例 | 覆盖 | 不覆盖 |
|---|---|---|
| `TestStrikeAndSlay`（`RuleEngineTest.cs:23589` 附近）| 直接击杀 → Slay 响；没打死 → 不响；攻击者同归于尽 → 不响 | Blast / 易伤 |
| `A1249` 夹具（`:23643`）| 打「已经是残骸」的格子不算击杀 | —— |
| `A1250` 夹具（`:23701`）| 效果/能力击杀算；战术卡伤害不算（`:23730`）| —— |
| `TestA1279SlaySelfChain`（`:23781`）| 效果击杀自链；「Slay 可被授予」的存在性（Pledge / Beastboss）| Blast |
| `TestAttackKeywords`（`:5513`，Blast 段 `:5537-5542`）| Blast 溅射**扣血** | **完全不断 Slay** |
| `RuleEngineTest.cs:1236-1256` | 嵌入 `Slay:` 被认出 | 同载荷里 `and` 前的 `+1 fist` / `Blast 2` 是否还在 |

⇒ **Blast / Stomp / 易伤 × Slay 的组合 = 无覆盖**；Helspear 整体 = **无覆盖**（全 `RuleEngine/Editor` 里没有 `Helspear`/`BL35` 的机制用例）。

---

## 三、场景 × 行为对照表

> 「原版」= A2 的 `actingCard + deathType ∈ {15,20}` 口径；「我们」= `TryCreditKill` 的实际调用点。

| # | 场景 | 原版应否触发 Slay | 我们现在 | 证据 | 缺口 |
|---|---|---|---|---|---|
| A | 攻击**主目标**直接打死 | ✅ 1 次 | ✅ 1 次 | `RuleCore.cs:5023` Hurt → `:5251` `TryCreditKill`；测试 `TestStrikeAndSlay` | 无 |
| B | 同一次攻击 **Blast 溅射**打死相邻单位（1 个）| ✅ 1 次 | ❌ **0 次** | 原版 `ResolveTraitAdjacentDamage.c:18-24`（source=攻击者、deathType=0xf）；我们 `RuleCore.cs:5065-5075` 只有 `Hurt`，无 `TryCreditKill` | **有（核心）** |
| B′ | Blast 一次溅死 2 个 | ✅ **2 次** | ❌ 0 次 | A3「每个死者一条 backlash action」 | **有（核心）** |
| C | 主目标**被易伤那一份**打死（主伤害没打死）| ✅ 1 次 | ✅ 1 次 | 原版 `_ReceiveDamage…:188-257`；我们 `DamageAfterReduction:5370-5451` 折进同一 `Hurt`，`:5251` 记账 | 无 |
| C′ | **Blast 受害者**被「Blast + 易伤」打死 | ✅ 1 次 | ❌ 0 次 | 同上，被 B 吞掉 | **有** |
| D | **反击**把攻击者打死 | ❌（deathType=10 + 阵营闸）| ❌ | 原版 `_ResolveAttack…:1142`；我们 `:5128` 只 `Hurt`、不记账 | 无（正确） |
| E | 同一次攻击死 3 个（主目标 + 2 个 Blast）| ✅ **3 次** | ⚠️ **1 次**（只有主目标）| A3 | **有** |
| F | **Stomp 溅射**打死相邻单位 | ✅ 1 次 | ❌ 0 次 | 原版 `ResolveTraitStompDamage.c`（source=攻击者、deathType=0xf）| **有（同族，用户未点名）** |
| G | 能力 / 效果伤害打死 | ✅ 1 次 | ✅ 1 次 | 原版 deathType=20；`EffectResolver.cs:2005/2414/2961` + `RuleCore.cs:9415` | 无 |
| H | **战术卡**伤害打死 | ❌ | ❌ | 原版 deathType=30 且战术卡不在场上清单；我们 `ctx.ActingUnit == null` | 无（正确）|
| I | 打死**督军** / **残骸** | ❌ | ❌ | 原版 `:251` / `:256`；我们 `victim.IsWarlord` / `victim.IsRemnant` | 无（正确）|
| J | **Helspear 给战将 Blast 2** | ✅ 数据里有 | ❌ **根本没给** | 原版 `buffEffect{trait:350,traitValue:2}`；我们 `GivePayload.cs:507-537` 丢段；ruleprobe `play` 实测 `{slay1}` | **有（前提级）** |

---

## 四、差异清单（按「修了才有意义」的先后）

| # | 场景 | 原版 | 我们 | 证据 | 缺口 / 建议落点 |
|---|---|---|---|---|---|
| D1 | Helspear 的 `Blast 2` 被静默丢掉 | 战将同时获得 `blast 2` + `slay`（附魔） | 只有 `slay` | `Core/GivePayload.cs:507-537`（后缀命中后 `return true` 丢掉前缀）；ruleprobe `play` 实测 `{slay1}`；同族 `Ferocious Rage`/`'Uge Choppa` 同样丢段 | **先修**：嵌入分支命中后缀时，把 `w` 里「`head` 命中位置之前的那截」递归 `ParseInto` 进同一个 `ops`（并按 `and`/`,` 收尾）；断言要断**两段都在**，不是只断嵌入段 |
| D2 | Blast 溅射死者不记 Slay | 每个 Blast 死者各 1 次 | 0 次 | 我们 `RuleCore.cs:5065-5075` 无 `TryCreditKill`；原版 `ResolveTraitAdjacentDamage.c` + `ResolveDeadCard.c:242-263` | 在 Blast 循环里对**每个被打死**的相邻单位记一次（阵营要在 `Hurt` **之前**取，照 `DoDeal:2003` 的先例）；⚠️ 顺序差异见 §六 |
| D3 | Stomp 溅射死者不记 Slay | 1 次 | 0 次 | 我们 `RuleCore.cs:5095-5106` 无记账；原版 `ResolveTraitStompDamage.c` | 同 D2 |
| D4 | 一次攻击多个死者只记 1 次 | N 次 | 1 次 | `RuleCore.cs:5251` 是**单发**，且只对 `target` | D2/D3 修完后自然变成 N 次；但**顺序**要与原版对（见 §六·4）|
| D5 | （附带）Blast/Stomp 击杀不记猎杀标记 | 原版按同一 `actingCard` 结算 | `Hurt` 没传 `killer` ⇒ 不回血 | `RuleCore.cs:5072/:5102` 未传 `killer`；`CleanupDeaths:7197` 只在 `killer>=0` 时回血 | 与 D2/D3 同一处顺手补（`killer: p`）|
| D6 | （表现，未验证）授予 Slay 时的状态动画 | 原版 `CardScript__AddEffect.c:889-891` 在附魔加身时 `DisplayStatusAnim(0x2ee)` + `UpdateTraitIcons` | 我们的 `DoGive` 只记日志 / 挂关键词 | `EffectResolver.cs:7511-7514` | 需先确认真机表现（本次不跑 Unity）；若用户在意「卡上闪一下」再补 |
| D7 | （测试）现有断言只断嵌入段 | —— | `Ferocious Rage` 丢 `+1 fist` 仍全绿 | `RuleEngineTest.cs:1236-1256` | 补「前缀段也在」的断言；另补 Blast/易伤 × Slay 的判别式夹具 |

---

## 五、修复要点（不是本轮动手，只留给下一位）

1. **D1 是前提**：不修它，Haarken 这条场景在引擎里连 Blast 都没有，D2/D3 的断言也摆不出来。
2. D2/D3 的记账必须在**每个死者**上各做一次，且：
   - 阵营 `vOwner` 要在 `Hurt` **之前**取（死后 `OwnerOf == -1`，见 `DoDeal` 的注释与先例）；
   - `TryCreditKill` 自己会判 `IsAlive / IsRemnant / IsWarlord / IsRealmTurn`，不需要在循环里重写；
   - ⛔ 不要改成「主目标没死就不记」——原版的 credit 与主目标生死无关。
3. **判别式（灭自证）建议**：
   - 主目标 1 血 + 相邻 2 血 + 战将带 `Blast 2`/`Slay: Damage 2 EnemyWarlord`：
     打主目标 ⇒ 主目标死、相邻 2 血**不死** ⇒ 督军 **-2**（D2 若写成「Blast 一开就打」会红）；
   - 相邻单位 2 血 ⇒ 被打死 ⇒ 督军**再 -2**（总计 -4；把 D2 拆掉 ⇒ -2）；
   - 相邻单位 3 血 ⇒ 不死 ⇒ 只有主目标那 -2（把 D2 放宽成「Blast 一开就记」⇒ -4，红）。
4. D4 的顺序：原版按**死亡入队顺序**（主伤害先、Blast 后）逐条广播；我们 `FlushDeaths` 按 (方, 格号) 排序，
   而 `PendingDeath` 只带 `Killer`（玩家号）、**不带 actingCard** ⇒ 若要「Slay 的结算顺序」也对上，
   需要给死亡批补一个「凶手牌」字段，或把 credit 挪到伤害点但按入队顺序延后执行。
   （这一条会影响「多次部署」的落位顺序，属精修，不是「有没有」的问题。）

---

## 六、没查清的部分

1. **哨戒（Sentry）那一下的 `DamageInfo.actingCard` 到底是谁**：
   `_ResolveAttack_d__438__MoveNext.c:1421` 的 `ReceiveDamage(攻击者, 0x14)` 明确是 deathType=20，
   意味着按 A2 的闸**只要 actingCard 是攻击者或哨戒单位，就可能记一次 Slay**；
   但伪代码里 `DamageInfo___ctor` 的 source 实参被寄存器复用搅乱（`lVar25` 同一函数里先后是 List / CardTrait），
   **没有钉死**。⇒ 如果 actingCard = 哨戒单位，那么「哨戒反杀攻击者」在原版**会**触发哨戒自己的 Slay；
   我们现在对哨戒只 `Hurt(attacker)` 不记账。**需要在报告外再核一次 VA 或实况**，别照着「反击不记」类推。
2. **`singleUseEnchantment(15)` 的准确语义**：本会话只查到「`EntityScript__SpendEnchantment` 按 `buffType==0xf` 置 spent 位，
   `RawCardScript__TriggerAbilities` 跳过 spent」，而 **Slay 路径没有任何 `SpendEnchantment` 调用点**
   ⇒ 我判定「不消耗、可多次触发」。但没找到「为什么这一定义成 15 而不是 12」的正面说明；
   若原版存在一条我漏掉的通用消耗点，A3 的「3 死 3 触发」就可能是「3 死 1 触发」。
   追法：`grep -rn "SpendEnchantment" decomp_full` 只有 3 个调用点（已列），再看 `buffType 0xf` 与 0xc 的其它差异。
3. **实况无法复现**：原版已关服、卡数据在远端 CCD、`hand injection` 不可用（`CLAUDE.md` §一·4 表）；
   所以「同一次攻击 3 个死者 → 3 次 `Deploy a Black Legionary`」是**静态判定**，没有真机读数。
4. **`waitingToDie` 与「存活」的边界**：原版闸是 `cardState != 5`（`ResolveDeadCard:248`），我们用 `killer.IsAlive`（`TryCreditKill:4593`）。
   两者在「血 ≤ 0 但被 `Survivor` 救回 / 仍在死亡批里」那些档**可能不等价**；本轮只逐条对了正常档。
5. **Helspear 的 Slay 是否应当每次死亡都「部署」**：数据没有 once 旗标，但也没有实况；
   如果将来能进原版对战，最小验证 = 战将带 Blast 2 + Slay 打死 2 个相邻单位，数一下部署了几个 Black Legionary。
6. **表现层 `EvtKind.Trigger` 的行号**：`BattleDriver.cs` 本会话被别的写手改动（同一符号位移 61 行），
   我按符号引用；主对话合并报告时不要写死 16197 这种数字。

---

## 七、证据索引（文件 + 符号）

- 原版代码：`d:/2/tools/decomp_full/`
  - `CardScript__ResolveDeadCard.c:242-263`（Slay 六道闸）· `CardScript__TriggerSlay.c:14-17`（OnTrigger 120）
  - `CardScript__TriggerUnitBacklashActions.c:44-50,181`（死亡动作 actingCard=伤害来源）
  - `BattleManagerSupport__BroadcastDeadUnit.c:147/206/262/369`（对场上+手牌逐个广播）
  - `BattleManager._ResolveBacklash_d__451__MoveNext.c:88,144`（ResolveDeadCard → BroadcastDeadUnit）
  - `BattleManager._ResolveAttack_d__438__MoveNext.c:686,715,988,999,1030,1089,1142,1290`
  - `BattleManager__ResolveTraitAdjacentDamage.c:18-24`（Blast：source=攻击者、deathType=0xf）
  - `BattleManager__ResolveTraitStompDamage.c`（Stomp：source=攻击者、deathType=0xf）
  - `CardScript._ReceiveDamage_d__381__MoveNext.c:188-257`（易伤 = 同一时刻的第二发独立伤害）
  - `BattleManager__GetDeathType.c:140-147,258-372`（30 / 10 / 15 / 20 的分档）
  - `BattleManager__ResolveAction.c:8579-8821`（action 0x59 inline slay 闸）· `BattleManager__ResolveTriggerSlay.c`
  - `AbilityLogic__PlayAbility.c:1894-1909`（effect 467 → AddTriggerSlay）
  - `CardScript__AddEffect.c:856-891`（Slay 附魔加入时的替换 + 状态动画）
- 枚举：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/`
  - `AbilityTrigger.cs:22 Slay=120` · `AbilityEffect.cs:81 triggerSlay=467` · `BattleActionType.cs:92 triggerSlay=89`
  - `UnitDeathType.cs`（combatAttacker=10 / combatDefender=15 / ability=20 / tactic=30 / poison=40）
  - `DamageType.cs`（Attack=4 / Blast=6 / Vulnerable=7 / VulnerableCombat=8 / Stomp=11）
  - `BuffType.cs`（addTrait=3 / enchantment=12 / singleUseEnchantment=15）· `DefinedTrait.cs`（slay=260 / blast=350）
  - `CardStateOptions.cs`（waitingToDie=5）· `DamageInfo.cs`（actingCard 在 +0x10）
- 解包数据：`d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`
  - `Z-NC BlackLegion-Helspear Assault`（`61046837c330b48978b4af5201230b6e`）buffEffect 两条
  - `Z-NC BlackLegion-Helspear Assault_enchantment`（`path_id=196526527010153435`）trigger 120 → logic 70
  - `BlackLegion_war_Haarken Worldclaimer`（`BL35`）
- 我们的代码：`d:/4/Unity/MyGame/Assets/RuleEngine/`
  - `Core/RuleCore.cs`：`TryCreditKill`（4587-4615）· Blast（5065-5075）· Stomp（5095-5106）· 反击（5119-5135）·
    标记光（≈5160）· `DeclareAttack` 尾段记账（5251）· `Hurt`（6732）· `CleanupDeaths`（7032）· `FlushDeaths`（7497）
  - `Core/EffectResolver.cs`：`DoDeal` 记账（2005）· 每个 shooter（2414）· `DoDestroy`（2961）· `GrantEmbeddedCore`（7448-7515）·
    `DoGive`（7634+）· `ResolveEffect` damage（9415）
  - `Core/GivePayload.cs`：嵌入分支（507-537）· `and` 拆分（540-571）· `EmbeddedBody/Keyword`（713-726）
  - `Core/UnitState.cs`：`_grantedOps` / `GrantOps` / `FxOps`（688-727）· `TempBuff.UntilMyNextTurn`（1150-1180）
- 测试：`RuleEngine/Editor/RuleEngineTest.cs` — `TestStrikeAndSlay`（≈23589）· `A1249`（23643）· `A1250`（23701/23730）·
  `TestA1279SlaySelfChain`（23781）· `TestAttackKeywords` 的 Blast 段（5537-5542）· 嵌入头断言（1236-1256）
- 表现：`CardPresentation/Battle/BattleDriver.cs` `case EvtKind.Trigger: PlayTraitPulse(e)`（按符号）·
  `CardPresentation/Core/VfxMap.cs`（Strike/Slay → `trigger` 粗桶）
- 实测（纯 dotnet）：`工具/ruleprobe`
  - `play 61046837c330b48978b4af5201230b6e` → 战将 `{slay1}`（**无 blast2**）
  - `play 84d4d192e0f57a34a90ac0f6172c3358`（Ferocious Rage）→ `{slay1}`，攻击 2 不变
  - `play 6899b199f5e36144ebbdd7ed9d9e7afe`（Uge Choppa）→ `{slay1}`，攻击 2 不变
  - `play AM24` → 棋盘显示 `{armour1, blast2, vanguard1}`（证明显示层不是问题）
  - `card "Helspear Assault"` / `seg "Deploy a Black Legionary"`（载荷与 deploy 解析）
