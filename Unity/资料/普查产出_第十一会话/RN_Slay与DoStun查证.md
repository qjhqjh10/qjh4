# R-N · `A1223`（Slay/Kills）与 `A1224`（DoStun）查证

> 只读代理 R-N · 2026-10-10（本轮日期见会话）· **一个 `.cs` 都没改** · 没跑 Unity · 没动 git · `d:/2/**` 只读。
> 权威 = `d:/2/tools/decomp_full/`（`Assembly-CSharp`，**它覆盖不到的层不算「原版没做」**）。
> 代码行号一律**现读**（`RuleCore.cs` 在本笔前后有挪动，`P-D` 报的 `4465` 现在是 `4551`）。

---

## 结论（两条各一句话）

- **① `A1223` 不成立**（「翻面成残骸会被当成被摧毁 ⇒ 我们多触发 Slay」这一条**不成立**：原版对**刚被打到 ≤0、即将翻面**的目标**照样触发** Slay —— 它读的那面旗 `0x65` 此刻还是 `0`）。**但同一条判据带出两条【真】差异**（见 §①·③）。
- **② `A1224` 成立**，方向 = **我们少晕**（只差「目标会被翻面成残骸」那一档；真死那一档两边一致 = 都不晕）；✅ **并且它不止是少挂一个状态** —— `GetsStun` 广播一起漏（`Yrlla the Huntress` / `Jain Zar` 会对残骸**少打 3 / 1 点**）。

---

## ① `A1223` —— `Slay` / `Kills`

### ① 我方现状（现读）

`RuleCore/Core/RuleCore.cs`（`DeclareAttack` 尾段，**攻击之后那一块**）：

```
3754  bool killed = !target.IsWarlord && !target.IsAlive;
3755  if (killed)
3757      FireTriggerAt(ctx, attacker, KeywordTable.Slay, p, atkSlot);
3761      BroadcastWhen(ctx, WhenEventKind.Kills, tgtP, target.Card, target, actor: attacker);
```

- **全仓只有这一个发射口**（`grep -n "KeywordTable.Slay\|WhenEventKind.Kills" Core/*.cs` 现核：只有 `RuleCore.cs:3757` / `:3761`；另有 `CardDef.cs:173/279` 是关键词表，不是发射口）⇒ 「击杀」只在 **`DeclareAttack`** 里判。
- 判据里**没有**任何「残骸」概念 ⇒ 只要目标是非督军且 `Health ≤ 0` 就算击杀（`IsAlive` = `Health > 0`）。
- 残骸的产生口（**唯一写点**）：`RuleCore.cs:4551` ——
  `new UnitState(u.Instance, false) { IsRemnant = true, Attack = 0, RangedAttack = 0, Health = 1, MaxHealth = 1, Exhausted = true }`
  （`grep -rn "IsRemnant = true" Core/*.cs` 现核：只有这一处；**沿用同一个 `CardInstance`**）。

### ② 原版判据（逐条现读）

**先定位「Slay 到底在哪发射」** —— `AbilityTrigger.Slay = 120`
（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs:22`）。把**全库 111 个 `RawCardScript__OnTrigger` 调用点**（多行感知扫描，不只 grep 单行）扫一遍，**只有两处**传 `0x78`：

| # | 发射点 | 它是谁 | 闸 |
|---|---|---|---|
| **A** | **`CardScript__ResolveDeadCard.c:259`**（死亡结算；**`P-D` 那份漏了这一处**） | `OnTrigger(param_1.rawcard, 0x78, bm, param_1, uStack_210/*actingCard*/, local_308/*targetCard*/)` | `:242-256`，见下 |
| **B** | `CardScript__TriggerSlay.c:16` | 被 `BattleManager__ResolveTriggerSlay.c:226` + `BattleManager__ResolveAction.c:8777` 调；那条 action（`BattleActionType.triggerSlay = 89`，`BattleActionType.cs:92`）**只由 `BattleManager__AddTriggerSlay` 建**，而 `AddTriggerSlay` 的唯一调用点 = `AbilityLogic__PlayAbility.c:1903` = `case AbilityEffect.triggerSlay (467)`（`AbilityEffect.cs:81`） | `IsInPlayOrDying`（`ResolveTriggerSlay.c:128`） |

⇒ **`P-D` 说的「`AddTriggerSlay` 全库只有一个调用点」是对的，但那条路不是「普通击杀」的路**：`AbilityEffect.triggerSlay = 467` 的语义是**「强行触发某个单位的 Slay」**（卡面长句 `Trigger the Teleport and Slay effects of a friendly unit`，`Master Lazarus`；我们的解析层 `/Core/EffectText.cs:5241` 已收这一族）⇒ B 与「这一下是不是击杀」无关。**普通击杀走 A。**

**A 的闸逐条**（`CardScript__ResolveDeadCard.c`）：

```
176  cVar13 = op_Equality(local_308, param_1, 0);
177  if ((cVar13 == '\0') || (get_cardType(param_1) != 10)) {   ← 主支
     ...                                                     ← else（:314）⇒ DeadHero(param_1.side)
231  if ((isAlive-ish(local_308)) && (local_308 + 0x222 == 0))
236      CardScript__TriggerOnMinionDeath(param_1, uStack_210, local_308, local_28, local_223, 0);
242  if (op_Equality(local_308, param_1, 0) == 0) {            ← 解析出的那张 ≠ 死者
244      CardScript__RemoveExtrinsicEffectsFrom(param_1, local_308);
248      if ((op_Equality(uStack_210, param_1, 0) != 0) && (*(int*)(param_1 + 0x228) != 5)) {
252          if ((get_cardType(local_308) != 10) && (local_28 == 0xf || local_28 == 0x14)) {
256              if ((param_1.isPlayer(0x40) == IsPlayerTurn(bm)) && (*(char*)(local_308 + 0x65) == 0)) {
259                  RawCardScript__OnTrigger(param_1.rawcard, 0x78, bm, param_1, uStack_210, local_308, 0);
267  ...同样形状的 Requiem 那一块（0x1ae=430；:267 `GetAdjacentUnits(param_1).Contains(local_308)`）
```

**字段身份（这几条是本笔的关键，逐条有出处）：**

1. **`local_308` = action 的 `targetCard`（死者）；`uStack_210` = action 的 `actingCard`（凶手）** ——
   由三段独立推导：① `BattleAction` 的字段序（`BattleAction.cs:8-14`：`actionType(0)` / `isPlayer(4)` / `actingCard(8)` / `targetCard(0x10)`）；
   ② 两个 call site 把 action 存在状态机 `+0x20` 上（`BattleManager._ResolveBacklash_d__451__MoveNext.c:57` `puVar16 = (undefined8 *)(param_1 + 0x20);`），而 `_ResolveBacklash` 取 `*(param_1 + 0x30)` 当「死者」用（`:29-35`：`cardState==5 || 0x65!=0 || HasEnoughPendingDamageAndHasToTransformIntoRemnant`）⇒ `+0x30 = action+0x10 = targetCard` ✓；
   ③ **直接读到 action 的构造**：`CheckIfDead.c:110-147` 两支都调 `CardScript__TriggerUnitBacklashActions(死者, dmgType, 凶手)`，而该函数 `:44-51` 明写 `local_308[0] = 0xd`（actionType 13 = backlash）、`local_2f8[0] = param_1`（base+0x10 = **targetCard = 死者**）、`uStack_300 = param_3`（base+0x8 = **actingCard = 凶手**）✓✓。
2. **`param_1` 是「死亡广播列表里的那一个」** —— `ResolveDeadCard` 的调用链是
   `BroadcastDeadUnit`（`BattleManagerSupport__BroadcastDeadUnit.c:87` 起遍历 `bm + 0x470` 逐个 `ResolveDeadCard(元素)`，`:147/206/262`）
   ← 只被 `_ResolveBacklash:144` 与 `_ResolveMultiDamageAndDestroySelf:314` 调。
   而 `:248` 的闸是 `actingCard == param_1`、`:267` 的闸是 `param_1` 与 `local_308` **相邻** ⇒ **`param_1` 只能是「列表里的某一张」**，`:248` 把「凶手」从那列表里挑出来，`:267` 把「死者的邻居」挑出来（Requiem 语义 ✓）。四块（DeadHero `:176` / 死亡触发 `:231` / Slay `:242` / Requiem `:267`）在「`param_1` = 列表元素、`local_308` = 死者」这一读法下**全部自洽**。
3. **`+0x65` = `isRemnant`** —— `EntityScript__set_isRemnant.c:5`（`*(param_1 + 0x65) = param_2`）、`CardScript__TransformIntoRemnant.c:29`（`= 1`）、`CardScript__TransformFromRemnant.c:10`（`= 0`）、`CardScript__CardSetup.c:94`（`= 0`）。
   它在 `CheckIfDead.c:123` 的用法：`HasToTransformIntoRemnant(card) == 0 || card.isRemnant != 0` ⇒ **真死**；否则 ⇒ 走 `AddTransformIntoRemnant`（**不置 `cardState = 5`**，这就是 `A1176` 那条）。
4. **`local_28` = `UnitDeathType`** —— `UnitDeathType.cs`：`combatAttacker = 10` · **`combatDefender = 15`** · **`ability = 20`** · `tactic = 30` · `poison = 40`；
   实证：`CardScript__TriggerOnMinionDeath.c:97` 把它交给 `RawCardScript__OnMinionDeath.c:19-27`，那里把 **10 → 0xaf(175 `UnitDeathAttacking`)**、**0x14 → 0xb4(180 `UnitDeathByAbility`)**、**0x28 → 0xaa(170 `UnitDeathByPoison`)** 一一对上 ✓。
   ⇒ `local_28 ∈ {0xf, 0x14}` = **「死者是被攻击的那一方 / 死于能力」**。

**⇒ 原版这一跳的形状 =**「**死亡广播**时，把**凶手**挑出来，让他触发 `Slay(120)`」，闸 = 凶手活着（`cardState != 5`）∧ 死者**不是督军**（`cardType != 10`）∧ 死者**死于 `combatDefender` 或 `ability`** ∧ 凶手属于当前回合那一方 ∧ **死者 `isRemnant == 0`**。

### ③ 成不成立

| 命题 | 判定 | 差在哪 |
|---|---|---|
| `A1223` 原文：「**翻面成残骸**的目标会被当成被摧毁 ⇒ 触发 `Slay`/`Kills`」（⇒ 我们多） | ❌ **不成立** | 原版在死亡结算时读的是**死者那一刻的 `isRemnant`**；而 `CheckIfDead` 的翻面支路是**先入队 `transformIntoRemnant`、`isRemnant` 要到那条 action 结算时才置 1**（`TriggerUnitBacklashActions` 在 `AddTransformIntoRemnant` **之前**调用，`CheckIfDead.c:110-147`）⇒ **死亡处理时死者 `isRemnant` 还是 0** ⇒ **闸真 ⇒ 原版照样触发 Slay** ⇒ **这一档我们和原版一致** |
| 🆕 **我们多**：打掉一个**本身已经是残骸**的格子 | ✅ **成立**（新差异） | 原版 `:256` 的 `isRemnant == 0` 把它挡掉；我们的 `!target.IsAlive` 放它过去（残骸 `Health = 1`、`IsRemnant = true`）。方向 = **我们多触发** |
| 🆕 **我们少**：**用能力/效果**摧毁（不是攻击） | ✅ **成立**（新差异） | 原版允许 `deathType = ability(20)` ⇒「本单位用能力摧毁了单位」也算击杀；我们的 `killed` 只在 `DeclareAttack` 里算（全仓唯一发射口），效果/天赋击杀**一次都不触发** `Slay`/`Kills`。方向 = **我们少触发** |
| 另一条口径：`combatAttacker(10)`（死者是**攻击方**，如被反噬/哨戒打死）不计 | ✅ 与我们一致 | 我们的 Slay 只对 `target`（被打的那一方）算，结构上就不会走这一档 |

### ④ 可达性（卡池现数：`Assets/RuleEngine/Resources/cards_engine.json`，`count = 1126`）

- **Slay 一方**：`keyword` 带 `slay` **9 张**；`desc` 里写 `Slay:` **19 张**；**并集 21 张**
  （含 `Sammael` · `Belial` · `Company Champion` · `Black Knight` · `Fiend` · `Flawless Blade` … ；另有光环授予：`Beastboss on Squigosaur` 给友方野兽挂 `Slay:`，`Core/Aura.cs:752`）。
- **残骸一方**：`keyword` 带 `remnant` **25 张** + 带 `waystone` **24 张** = **49 张**（Necron 一族 + Aeldari 一族；`desc` 里出现这两个词的 29 张是同一批）。
- **「我们多」那一档的交叉**：Slay 单位攻击一个**已经是残骸**的格子 —— 残骸 `Health = 1`，挨任意一下就没 ⇒ **真可达**（不需要 `Slay` 一方特别强）。
- **「我们少」那一档**：21 张 Slay 卡里**哪些自带能摧毁单位的能力/天赋** —— **没数**（没查到；要逐张读 `desc`/天赋正文）。

### ⑤ 该不该改 + 形状

- 「我们多」那一档：**该改**，形状极小 —— 在 `RuleCore.cs:3754` 补一条**同一面旗**：
  `bool killed = !target.IsWarlord && !target.IsAlive && !target.IsRemnant;`
  判据 = `CardScript__ResolveDeadCard.c:256`（`isRemnant == 0`）。⛔ **不新造判据**：`IsRemnant` 全仓唯一写点 = `RuleCore.cs:4551`，读它不算「第二份规则」。⚠️ 语义正好对上：**刚翻面的那一下**我们手里那张 `target` 是**旧对象**（`IsRemnant == false`）⇒ 放行 ✓（= 原版此刻 `0x65 == 0`）；**本来就是残骸**的 `target` 是残骸对象（`IsRemnant == true`）⇒ 挡掉 ✓。
- 「我们少」那一档（效果击杀）：**该改**（铁律 11：与原版不符的全部要做），但**形状要调度台定** —— 它等于把「击杀成立」这件事从「`DeclareAttack` 的局部变量」提升成一个**共用判定口**（`Slay` 与 `Kills` 现在共用同一条判据，⛔ 别写成两份）。判据 = `local_28 ∈ {combatDefender(15), ability(20)}` + `actingCard == 击杀者`。
- ⛔ **不要动 `targetDied` 的定义** —— `P-D` §2 那五处已经逐条核过（这五处的原版判据都不是「会不会翻面」），本笔**不推翻**它。

### ⑥ 没查清 / 还差什么

1. **字段偏移没有独立核证**：`local_308`/`uStack_210` ↔ `targetCard`/`actingCard` 我是用「字段声明序 + 状态机拷贝基址（`+0x20`）+ `TriggerUnitBacklashActions` 的构造」三段推的，**没有**拿 `dump.cs` / IL 的偏移表核过。
   **还差**：`CardScript`/`BattleAction` 的 `FieldOffsets`（或 `BattleManager.cs` 那份签名桩里没有偏移，得去 `dump.cs`/IL 取）。
2. **`bm + 0x470` 是什么列表**（`BroadcastDeadUnit` 遍历它）—— **没查**。它决定那一跳到底遍历谁（若是「全场单位」则 `:248` 的挑凶手成立；若是别的集合，`③` 的结论要复核）。
3. 「我们少」那一档的**可达性没数**（见 §①·④）。
4. **没做实况验证**（红线禁跑 Unity）—— 全部是静态判据。

---

## ② `A1224` —— `EffectResolver.DoStun`

### ① 我方现状（现读）

`RuleEngine/Core/EffectResolver.cs`：

```
2102  static bool DoStun(...)
2112      var targets = ResolveTargets(ctx, owner, spec, null, chosen);
2115      foreach (var t in targets) { if (t == null || !t.IsAlive) continue; ... }   ← 逐目标闸门
```

- 目标来源**两条路**，**两条都按「现刻的 `IsAlive`」过滤**：
  ① 明写目标 ⇒ `AddSide`（`:1294-1304`）`:1299  if (u == null || !u.IsAlive) continue;`
  ② **代词 `it`/`them`** ⇒ `ResolveTargets` 的 `prev` 支（`:628` `LastTargets` / `:631-633` `LastTarget`）**也要求 `IsAlive`**
     （解析层把 `it` 定成 `Side="prev", Kind="prev"`：`Core/EffectText.cs:6934`）。
- ⇒ 同一段效果里「**先打死、再晕它**」时：`LastTarget` 仍指着**挨打前那个旧对象**（`Health ≤ 0`）⇒ 目标列表**空** ⇒ `DoStun` 只打一条 `眩晕了 0 个单位` 的日志就走（**不静默，但语义错**）。

### ② 原版判据（逐条现读）

1. **眩晕那一步的闸 = `IsInPlay()`** —— `CardScript__Stun.c:44-48`（`A1176` 已录）：
   `RawCardScript__IsUnit(raw)` ∧ `cardState ∈ {2, 0xf, 3, 0x11}` ∧ `!HasCurrentTrait(0xfa /*unstunnable*/)` ⇒ `AddTraitSilently(param_1, 100, …)`。
   ⇒ **只看「在不在场上」，不看「血是不是 ≤0」**。
2. **能力的目标只解析一次** —— `RawCardScript__TriggerAbilities.c`（约 `:288`）`AbilityLogic__GetTargets(...)` 取一次目标表交进 `AbilityData`；
   `AbilityLogic__PlayAbility` 是**按那份目标表**逐个动词入队（`BattleActionType.stun` 那条 action 在主循环 `BattleManager__ResolveAction` 里结算）。
   ⇒ 「造成伤害」与「眩晕」**打的是同一个已被选中的目标**，眩晕的**可施与否**在它自己结算时用 `IsInPlay()` 判。
3. **顺序（`A1176` 的结论，本轮未重读队列）**：翻面支路里 `TriggerUnitBacklashActions` 排在 `AddTransformIntoRemnant` **之前**（`CheckIfDead.c:110-147`）⇒ stun action 结算时目标**还在场上**（`cardState == 2`）⇒ **闸真**。

### ③ 成不成立

**成立，方向 = 我们少晕**，且**只差一档**：

| 同一段效果里目标被打成 | 原版 | 我们 | 判 |
|---|---|---|---|
| **真死**（`cardState = 5`） | 不晕（`IsInPlay()` 假） | 不晕 | ✅ 一致 |
| **翻面成残骸**（`cardState` 仍是 2） | **晕** | **不晕** | ❌ **差这一档**（= `A1176` 同族，那条只修了 `Concussion` 那一格） |

🔴 **放大效应**：我们漏的不只是状态位 —— `DoStun` 里的 `BroadcastKeywordEvent(ctx, WhenEventKind.GetsStun, t)`（`:2154`）会一起漏，
而 **`Yrlla the Huntress`「When an enemy receives Stun, deal 3 damage to it」**、**`Jain Zar`「… deal 1 damage to it」** 正等着这条事件 ⇒ 一次漏晕可以再少 1~3 点伤害。

### ④ 可达性（现数卡池）

**「造成伤害并眩晕同一个目标」全池 8 张**（`cards_engine.json` 的 `desc` 逐条读）：

| 卡 | 正文 |
|---|---|
| `Banshee Mask` | Deal 3 damage to an enemy and Stun it |
| `Pinning Fire` | Deal 2 damage to an enemy and Stun it |
| `Hektor Thenmann` | Deal 2 damage to an enemy and Stun it |
| `Toxic Bonfire` | Deal 2 damage to an enemy troop and Stun it |
| `Attack Squig` | Deal 1 damage to an enemy and Stun it |
| `Roaming Outriders` | Deal 1 damage to a random enemy and Stun it |
| `Tempestus Scion`（Duty） | Deal 2 damage to an enemy. If it is a troop, Stun it |
| `Malicious Volleys` | Deal 2 damage to an enemy. If it is a troop, Stun it |

配 **49 张**带 `Remnant`/`Waystone` 的目标（§①·④）⇒ 「这 1~3 点正好把带翻面关键词的部队打到 ≤0」是**实打实的一手**（例：2 点打死一个 2 血 Necron ⇒ 原版晕住回复的残骸、我们不晕）。
⚠️ `RuleEngineTest.cs` 里**这 8 张一张都没被引用**（`grep` 现核）⇒ 现有断言**照不到**这一格。

### ⑤ 该不该改 + 形状

- **该改**（铁律 11）。形状 = **复用既有判定口的形状，别新造**：
  1. `RuleCore.StunStillOnBoard(ctx, tgtP, tgtSlot, target, targetDied)`（`RuleCore.cs:2892`）已经是**`A1176` 收口的那个口**（判据 = 读棋盘事实 `Board[slot].IsRemnant` + **`ReferenceEquals(occ.Instance, target.Instance)`**，⛔ 不预测「会不会翻面」）；
     `DoStun` 需要的是同一条判据换一个入口（它的目标可能已不在原格）⇒ **建议把「按实例找棋盘占位者」这一步也收进 `StunStillOnBoard` 一族**（或给它加一个只认 `Instance` 的重载），⛔ **别在 `EffectResolver` 里再写一份 `IsRemnant` 判断**。
  2. **代词那条路要一起修**：`ResolveTargets` 的 `prev` 支（`:628`/`:631-633`）用 `IsAlive` 过滤，会把「刚翻面的占位者」挡在外面 —— 这是「8 张卡里 6 张」走的形状（`… and Stun it`），⛔ 只修明写目标那一半 = **静默偏一半**（`A1176` 那笔已经踩过一次同族的坑）。
  3. ⚠️ 判据（原版）只说 `Stun` 这一格；**别的动词各有自己的闸**，⛔ 别一把梭（见「顺手发现」）。

### ⑥ 没查清 / 还差什么

1. **队列先后未重读**：原版「stun action 排在 `transformIntoRemnant` action 之前 ⇒ 结算时 `cardState` 还是 2」这条我**引用的是 `A1176` 的结论**，本轮**没有**重新逐行读队列。
2. **「`Stun` 没写目标」那条兜底**（我们按「槽号最小」`EffectResolver.cs:2107-2111`）与原版的挑选顺序**没核**。
3. **同族的其它动词没逐条核**：`grep -c "!t.IsAlive" EffectResolver.cs` = 13（全文 `!IsAlive` 类过滤 **25 处**，含 `DoBlind` `:2182` 等）。**没查**它们各自的动词在原版有没有「翻面那一档」。
4. **没做实况验证**（红线禁跑 Unity）。

---

## 顺手发现（⛔ 只报不改）

1. `P-D` 报的「`IsRemnant = true` 唯一写点 = `RuleCore.cs:4465`」—— **现在是 `RuleCore.cs:4551`**（文件已挪 ~86 行），唯一性**成立**（现核 `grep`）。
2. **我们「先翻面、后反噬」与原版顺序相反**：`RuleCore.cs:4535` 的注释自己写着「两支都在从棋盘移除之前、进墓地之前，更在**反噬结算之前**」，而原版是 `TriggerUnitBacklashActions(...)`（入队 backlash action）**先于** `AddTransformIntoRemnant(...)`（`CheckIfDead.c:110-147` 两支的调用序）。⇒ 这正是 §①·③「我们多」那一档能被观察到的原因（原版死亡结算时 `isRemnant` 还是 0，我们这边残骸对象**已经建好**了）。⚠️ 这是**另一笔账**（`TestDiedThisTurnDuringBacklash` 一族），本笔不动。
3. ✅ 独立复现 `P-D` §8·3：`BattleManager__ResolveStun.c` **全库没有直接调用者**（只命中它自己 + `ResolveAction` 里内联的 `case 0x11` 那一跳）。
4. 🔴 **`ResolveDeadCard` 那两块（Slay / Requiem）的闸带出一个结构事实**：`P-D` 猜「原版对『目标真的死了吗』另有一道判据（大概在 `targetCriteria`）」—— **方向对，位置不对**：它不在 `targetCriteria`，而在**死亡结算的广播那一跳**（`param_1 == actingCard` 挑凶手、`param_1` 与死者相邻挑 Requiem 听众）。
5. 🔴 **`AbilityEffect.triggerSlay/triggerAmbush/triggerUprising/triggerTeleport (465-468)`** 是「**强行触发**某个单位的那个关键词」这一族 —— `AbilityLogic__PlayAbility.c:1804-1910` 的整个 switch 是按 **`AbilityEffect`** 枚举分派的（逐个对上：460 `triggerMob` / 462 `triggerPray` / 467 `triggerSlay` / 470 `showCardsWindow` / 475 `resetDuty` / 480 `highlightBloodThirst`）。以后查「某个触发怎么被强行引发」直接看这一族，省一整轮。
6. ⚠️ **`CardScript__ResolveDeadCard` 只有 6 个调用点**（多行感知扫描）：`BroadcastDeadUnit.c:147/206/262` + `_ResolveBacklash:88` + `_ResolveMultiDamageAndDestroySelf:266`（+ 其内部的 `lStack_120` 一处）。⇒ 任何「死亡结算」的结论都应在这 6 处里闭环。

---

## 没查清 / 停手的部分（汇总）

- ①里：字段偏移的独立核证 · `bm + 0x470` 是什么列表 · 「效果击杀」那一档的可达性。
- ②里：原版队列先后的重读 · 「未写目标」兜底顺序 · 另外 24 处 `!IsAlive` 的同族性。
- **两条都没有实况验证**（红线：只读、不跑 Unity）。
- ⛔ 本笔**没有**改任何 `.cs`、没动 git、没有碰 `项目任务.md` / `CLAUDE.md`。
