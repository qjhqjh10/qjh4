# B89 · 波 1 · 执行代理 `E3` —— R9「批次 4：EffectResolver 效果族」（Chain E 第 1 棒）

- **日期/会话**：2026-10-28 · 第八十九会话 波 1
- **文件所有权**：只改 `RuleEngine/Core/EffectResolver.cs`（未新建文件、未碰任何别处的 `.cs`）
- **typecheck**：`TMPDIR=/tmp/wf_B89E3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
- **行尾**：仍全 CRLF（`b.count(b'\r\n') == b.count(b'\n') == 18079`，bare CR = 0）
- **行数**：17,866 → **18,079**（+213，**全部追加在类尾**）。中段三处改动**净 0 行** ——
  锚点现读未动：`ctx.NotePlayed(p, inst, true);` = :2666 · `EmitTacticAbilityUseTriggers(...)` = :2792 ·
  `CheckCodex(... shouldCheckCost: true)` = :2825 · `static void QuestPointThreshold` = :13180 ·
  `int step = QuestPointStepFor(ctx);` = :13183。
- **账行**：`L64 L212 L429 L686 L809 L853 L869 L870 L934 L935 L1278`（`账/战斗逻辑驱动.tsv` 按行号现读）

---

## §1 逐条结论

> 状态列是**建议值**（账主表的定稿由调度台核销）。所有「未跑」= 本写手按红线不跑 `ruleprobe` / Unity。

### L64 — `AbilityLogic.AdjustCardEffect(ref CardEffect, AbilityData, int multiplier, BattleManager)` — 建议 **B**（本笔零代码改动）

**判据现读**（🥇 `decomp_full/AbilityLogic__AdjustCardEffect.c:1-59` 全文）：
闸 = `buffType(+0x38) ∈ {1 addStats, 2 changeCost, 3 addTrait, 10 changeAbilityCost, 0x46=70 setMeleeAttack, 0x55=85 setRangeAttack, 0x5a=90 addTraitRemnant, 0x5f=95 setCost}`；
过闸后**五对字段**各调一次 `NewBuffStat` 写回 base 位 —— `+0x48/+0x4c`（近战）· `+0x50/+0x54`（远程）·
`+0x60/+0x64`（血）· `+0x58/+0x5c`（护甲）· `+0x68/+0x6c`（费）；**⛔ 不碰 `traitValue(+0x40)`**。
⇒ 与账里「判据要点」逐字一致，**本笔未发现新差异**。

**剩余两条缺口（账已记）的落点都【不在本文件】**：
1. `extrinsic` 恒 `false`。根因现读实据：
   - 手牌那一族走 `RuleCore.ApplyHandBuffs`（`RuleCore/Core/RuleCore.cs:10546-10572`）——
     它把 N 条 `CardInstance.HandEffect` 的 `Op` **合并成一个列表**再 `ResolveOps` ⇒ `e.Extrinsic` 在这一步被抹掉；
   - 光环那一族走 `Auras.ApplyAura`（`RuleEngine/Core/Aura.cs:892-899`）—— 授 trait 用 `t.AddAuraKeyword(...)`、
     授数值用 `t.RecordGrant(...)`（:883），**两条都不建 `TraitEffectRecord`**。
   - 原语其实齐：`UnitState.ApplyTraitEffect(trait, value, buffType, stackType, source, extrinsic, markOfChaosType, daemonHostType, timingBits)`
     （`UnitState.cs:2982-2994` 现读）与 `TraitEffectRecord` 的三格（:2966-2975，含 `Extrinsic/DaemonHostType/MarkOfChaosType`+`TimingBits`）。
2. `daemonHostType` 恒 0：`PayloadOp` 没有这一列 ⇒ 生产者只能在**解析层 / 生成器**（归 E6/CardDef 侧）。

**补法（给下一个写手，勿在本文件硬凑）**：
- ① 需要把「当前这条效果是不是 extrinsic」从载体带到 `DoGive` → `ApplyOneGain` ——
  最小改动 = `BattleContext` 加一个作用域量（`ResolvingExtrinsic`，save/restore 语义同 `CardPlaySource`），
  或给 `EffectOp` 加一格；`ApplyHandBuffs` 改成**逐条** `ResolveOps`（或用那个作用域量包每一份 `e.Extrinsic`）。
- ② Aura 那一路把 `t.AddAuraKeyword(...)` 换成 `t.ApplyTraitEffect(..., extrinsic: true, source: GrantTag)`
  ⚠️ **先核** `AddAuraKeyword` 是否有光环专属簿记（`Auras.Recompose` 的整份收回靠的是它自己那本账）—— 若有，别直接换，要两本一起写。

**验证读数口径**（调度台跑）：
`bash 工具/ruleprobe.sh play <卡id>` 现池**无消费者**读 `TraitEffectValue` ⇒ 今天应当**逐字节不变**；
可用离屏探针读 `u.TraitEffectValue("<trait>")` 与 `u.TraitEffects`（只看不写），例如 `SAU53` 的残骸 + `shield` 那一格。

---

### L212 — `BattleManager.BroadcastQuestResolved(CardScript questCard)` — 建议 **B**（本笔已改，差额收窄）

**判据现读**：
- 生产端 = `decomp_full/CardScript__RemoveEffect.c:229-247`（case `0xc/0xf/0x10` 命中「`enchantmentData(+0x94)==0x28` ∧ `spellType(+0x90)==0x78` ∧ `!fromJam`」⇒ `BroadcastQuestResolved(this)` + `questCounter(+0x270)=0`）；
- 广播端 = `BattleManager__BroadcastQuestResolved.c:1-60`：**棋盘一跳**（`IsInPlay`）+ **一次**手牌跳（排除 questCard，六个 `true`）；
- 接收端 = `RawCardScript__OnQuestResolved` → `OnTrigger(490)`，`actingCard`/`targetCard` 都是 `default`。
- 🆕 **2026-10-28 现扫池**：`abilityTriggers` 含 **490** 的卡只有 **4 张，全是 DA 督军**（`DA1 DA2 DA3 DA75`）；
  `chosenAbility` 含 **270**（`createCardInDeck`）的 7 张（`DA44 DA1 DA2 DA3 DA72 DA75 GSC55`）。

**改动**：`EffectResolver.cs:13227-13229`（`QuestPointThreshold` 内），在「减阈值 / `QuestMilestone++`」之后、
原「阵营 == DarkAngels」代理之前，加**卡能力先行**那一趟：
```csharp
var _qW = ps.Warlord != null ? ps.Warlord.Card : null;
var _qOps = _qW != null ? _qW.TriggerOps("questtriggered") : null;
if (_qOps != null && _qOps.Count > 0) { RunAbilityOfAnotherCard(ctx, _qW, "QuestTriggered", owner, ps.Warlord, null, null, by); return; }
```
（原 `E9ER` 的 `AbilityTriggerPresence(490)` / 阵营代理 / `CreatePool.Resolve("a secret")` 那一路**逐字保留**。）

**仍未闭**：DA 那 4 张的 490 能力是 **`chosenAbility 270` 驱动（非文本）** ⇒ 今天仍走阵营代理；
「别的阵营以后加一个 chosenAbility 驱动的 490」我们仍然表示不出来（现池 0 张，池内只有 `GSC55` 带 270 且它没有 490 trigger）。

**验证读数**：今天新分支**零可达**（池内无文本型 490）⇒ `play all dump` 应逐字节不变。
若调度台要造夹具：合成一张 `trigger=490` + 文本正文（如 `Gain 1 Quest Point`）的督军卡，把任务点加到阈值，
应看到「正文真的跑了」+ **不再**造那张阵营隐秘。

---

### L429 — `BattleManager.IsThereValidSpellTarget(CardScript targetedSpellCard)` — 建议 **A**

**判据现读**（🥇 `decomp_full/BattleManager__IsThereValidSpellTarget.c:1-33` 全文）：
`unitsInPlay(+0x470)` 从下标 0 递增扫，逐张 `IsValidSpellTarget(mgr, 这张战术卡, 候选, 0)`，**任一真即 return 1**；
全假 return 0。

**改动**：新增 `EffectResolver.cs:17895（IsThereValidSpellTarget）` ——
两侧棋盘（`ctx.Players[q].Board[0..BoardSpec.Size)`）逐格问 `IsLegalPlayTarget`（= 原版 `IsValidSpellTarget` 的既有等价物，
`EffectResolver.cs:2196`），任一真即真。**不引入新语义。**

**为什么建议 A**：这条账的缺口原文是「只有逐目标、没有 exists 包装」——
同一语义在本仓**原本已由 `PlayTargetPool(...).Count > 0` 承担**（`PlayTargetPool` 的 doc 与 `CardInteraction` 的高亮都这么判），
本笔补的是**原版同名的可复用包装**，行为零变化。
⚠️ **没有新增调用点**：原版那两个消费者（`_ResolvePlayCardFromHand`／`NeedToChooseMinionTarget`）在我们这边的对应口在
`CardPresentation/Battle/BattleDriver.cs`（E5 的文件）⇒ 见 §5。

**验证读数**：纯新增 public 方法，`play all dump` 必逐字节不变。

---

### L686 — `CardScript.AddEffect(CardEffect newEffect, int enchantmentCardId)` — 建议 **B**（本笔零代码改动，做了逐块核对）

**判据现读**：`decomp_full/CardScript__AddEffect.c:1-975`（本次把 **switch 全表** 逐块对过：
`:330` switch 的 case 集合 = `1/2/10 · 3 · 4 · 5 · 6 · 7/0xd/0xe/0x12-0x18/0x1a-0x1d · 8 · 9 · 0xb · 0xc/0xf · 0x10 · 0x11 · 0x19 · 0x1e · default(0x23)`）。

**逐块对照（K2 / K5 / K9 及其余）**：

| 原版 case | 语义 | 我们的落点 | 状态 |
|---|---|---|---|
| **1 / 2 / 10**（`case 1:case 2:case 10:` 共用体，`:331-459`） | 合并循环（键 = 同 `enchantingCard` + 同 `buffType` + 同 `markOfChaosType(+0x78)` + 既有 `daemonHostType(+0x7c)==0` + `count(+0x80)==0` + `!permanentBuff(+0x75)` + `SameTimingAsEffect`），累加 `+0x48/+0x50/+0x60/+0x68`，再夹血 / `AddArmour(+0x58)` | `ApplyOneGain` 的 `attack/ranged/health/armour` 支（`EffectResolver.cs:11938+`）+ **K2 = `ctx.CostMods`**（`lowercost` 写 `Delta`、`CostSetTo` 写 `SetTo`，判据见 `BattleContext.CostMod` 的 doc，`:220-242`） | ✅ 已覆盖 |
| **3 / 0x5A**（`:460-679`，共用合并循环） | `addTrait` / `addTraitRemnant` 记录合并（add 累加 / replace 取 Max / count +1；新建强置 1） | `UnitState.ApplyTraitEffect`（`UnitState.cs:2982`）+ `ApplyOneGain` 的 `val>0` 支（`:11995-12003`） | ✅ 已覆盖 |
| **4**（`:680-694`） | `removeTrait`：`RemoveActiveEffects(4,T)` → `List.Add` → `RemoveBuffedTrait` → `RemoveTrait`（stealth 先广播 `LoseStealth`） | `ApplyOneGain` 的 `val<0` 支（`u.RemoveAll` + `MarkTraitRemoved` + `LosesStealth` 广播） | ✅ 已覆盖 |
| **5**（`:695-749`，**K5**） | `swapStats`：非英雄 ⇒ `ChangeBaseAttack(卡面近战)` + `ChangeMaxHealth(卡面血)` + 夹血；英雄 ⇒ LogError | 原语 = `ChangeBaseAttackFidelity` / `ChangeMaxHealthFidelity`（`EffectResolverPartial_B88E2.cs:73 / :123`）；**但没有任何动词/生产者**（`buffType 5` 池内 **0 条**，卡面也没有对应句式） | ⏳ 原语齐、无生产者（休眠；⛔ 不凭空造英文句式 —— 与「四个休眠动词」同一口径） |
| **6 / 0x1E**（`:750 / :951`） | `RemoveActiveEffects(6/30)` + `RemoveAllExtrinsicEffects` + 追加记录 | `UnitState.RemoveExtrinsicEffectsFrom` / `RemoveAll` 那一族（`CardDef.cs:7770` 记录的组合口 = `ApplyJam` 族）；**「按 buffType 整批清」没有独立入口** | ⚠️ 形状不同（我们按来源/关键词整批摘，不是按 buffType 号） |
| **8**（`:776-793`） | markOfChaos：`GetMarkOfChaos(+0x78)` → `TriggerEnchantment` → `bm.lastMarkOfChaos = 它` → `BroadcastMarkResolved` | `ApplyMarkBody`（`EffectResolver.cs:12499`）/ `ApplyLastMarkOfChaos`（`:12548`） | ✅ 已覆盖 |
| **9**（`:794-822`，**K9**） | `RemoveActiveEffects(9)` → ecstasy 判 → 追加记录 → `currentHealth = currentMaxHealthProcessed` | `DoSetHealth`（`EffectResolver.cs:14795`）+ `sethealth` 动词（`:1098` 派发表） | ✅ 已覆盖（池内 0 条，休眠） |
| **0xB**（`:823-825`） | `RemoveExtrinsicEffectsFrom(enchantingCard)` | `UnitState.RemoveExtrinsicEffectsFrom(...)` + `Auras`/`RemoveHandEffectsCastBy` 那一族 | ✅ 等价 |
| **0xC / 0xF**（`:826-829`） | `AddEnchantment(effect)` | `Enchantment.Attach`（`Enchantment.cs:228`，`buffType ∈ {12,15,16}`） | ✅ 已覆盖 |
| **0x10**（`:830-932`） | `enchantmentData(+0x88)` 模板入 `activeEnchantments(+0x110)` → `UsedEnchantment` → 逐 `spellType` 分支（`0x3c` / `0x78` 摘同族旧附魔并清 `+0x270` / `0x82` `SpendEnchantment` + `OnTrigger(0x1cc=460)` / `0x8c` 或 `0xbe` → `lastMarkOfChaos` + `BroadcastMarkResolved`） | 部分：`Enchantment.Attach` 有 `buffType` 与 `UsedEnchantment`；**模板 `+0x88` 那条链**在 `Enchantment.cs`（E4 的文件） | ⏳ 归 E4（模板 `e8cf1eb5…` 今天**不在池里**，见账内备注） |
| **0x11**（`:933-942`） | `TriggerEnchantment(bm.lastMarkOfChaos)` | `ApplyLastMarkOfChaos` 那一族 | ✅ 等价 |
| **0x19**（`:943-950`） | `AddTraitSilently(5 ephemeral)` + `ShowEphemeral` | `CreatePoolResult.MarkEphemeral` / `CardInstance.EphemeralMarked`（`EffectResolver.cs:4126-4130`）+ `SweepEphemeral` | ✅ 已覆盖（净等价，见 L869） |

**结论**：账内「K2/K5/K9 没核」这一格 **本笔核完** —— K2/K9 已覆盖、K5 原语齐但无生产者（休眠）。
**唯一真缺口** = `case 0x10` 的附魔模板链（在 `Enchantment.cs`，E4）+ 模板入池（数据侧、判据件 §三 A1a，需调度台在无账写手窗口做）。

---

### L809 — `CardScript.NonTargetSpellPlayed()` — 建议 **A**（本笔补 Combo，休眠）

**判据现读**（`decomp_full/CardScript__NonTargetSpellPlayed.c:1-31` 全文）：
`cardState(+0x228)=6` → `vtable+0x308`（`OnCardPlayedWithTarget`，实参 `thisCard=this, cardPlayed=this, target=0`）→
`IsTheSameTurnPlayed(this)` 为真 ⇒ `OnTrigger(0x24e=590)` → `BroadcastTacticPlayed` → `GoToCemetery`。
（`R21` 已把「原版传自己」那半订正掉；本笔复核 `.c:9-13` 实参 = `(rawCard, mgr, this, this, 0, cb)`，与账一致。）

**改动**：
- 新成员 `EffectResolver.cs:17980（TryFireCombo）` —— `IsTheSameTurnPlayed` 的等价物 = 同一 `CardInstance` 在
  `ctx.PlayedCards` 里**本回合出现 ≥ 2 次**（第 1 次 = 刚记的这一笔；`NotePlayed` 写点见 `BattleContext.cs:1334`）；
  命中则跑 `card.TriggerOps("combo")`；**池内 0 张** ⇒ 休眠。
- 调用点 = `PlayTacticBodyDeferred` 的 `EffectResolver.cs:2776`（正文之后、巧技之前 —— 与 `.c` 语句次序一致）。
- 池内现读：`abilityTriggers` 含 **590 = 0 张**；`DefinedTrait.combo = 1130` 也 **0 张**。

**验证读数**：`play all dump` 应逐字节不变（休眠）；要造夹具：合成一张 `Combo:` 正文的战术卡，同回合打两次，
第二次应看到 Combo 正文结算。

---

### L853 — `CardScript.RemoveEffect(CardEffect removedEffect, bool fromJam)` — 建议 **B**（本笔补入口与两条特判的落点判定）

**判据现读**（`decomp_full/CardScript__RemoveEffect.c:1-279` 全文）：① `permanentBuff(+0x74)!=0 ⇒ return`；
② `activeEffects.Remove(effect)`；③ `switch(buffType)`；④ 各档收尾。
- `1`：夹血（`min(当前, processed 上限)`）+ `armourChange != 0 ⇒ AddArmour(-x)`；
- `3 / 0x5A`：`HasDefaultTrait` 则留；`HasCurrentTraitActiveEffect` 则留；否则 `RemoveTraitSilently`；
  **两条特判** = `trait==0xe6(230) dropPod ∧ !HasCurrentTrait ⇒ RemoveDropPod`、`trait==0x262(610) pack ∧ !HasCurrentTrait ⇒ (+0x358)=0`；
- `5`：`ChangeBaseAttack(卡面)` + `ChangeMaxHealth(卡面)` + 夹血；
- `0xC/0xF/0x10`：摘 `activeEnchantments` 那条，`cardType==0x28 ∧ spellType==0x78 ∧ !fromJam ⇒ BroadcastQuestResolved + questCounter=0`；
- `0x2D(45)`：`currentDropPodHealth -= healthChange`、`currentDropPodAttack -= meleeAttackChange`；
- `default`：只认 `0x28(40)`（`hasActiveAbility = 卡面` / `activeAbilityRef = null`）。

**改动**：新成员 `EffectResolver.cs:18028（RemoveEffectRecordFrom）` ——
`permanentBuff` 的**出声**早退 → 调 `UnitState.RemoveEffectRecord`（数值/护甲/词条/回基础那几档的既有原件）
→ 补 `case 3/0x5A` 两条特判的**落点判定**：
- `dropPod`：原版 `RemoveDropPod` 前两句（`RemoveBuffedTrait` + `RemoveTrait`）的**净效果 = 词条消失**，
  已由 `RemoveEffectRecord` 的 `RemoveAll(Trait, silent:true)` 完成；后两句是**表现层**（镜头抖动 / 状态动画）⇒ 本版无对应物（**出声**说明）。
- `pack`：原版清 `+0x358`（`UpdateWhileInPlay` 的 Pack 计数缓存）；**我们没有那个缓存**（`Aura.Recompose` 每趟整份重算）⇒ 无动作（**出声**）。
  池内 `pack` **10 张**（`SW6 SW11 SW13 SW15 SW17 SW21 SW24 SW28 SW34 SW37`）⇒ 一旦将来有调用点，这条 log 会被看到。

**仍未闭（如实）**：
1. **调用点**（账 ①）：今天没有任何一处「按一条记录摘」—— `RevertBuffs` / `RevertGrantsFrom` / `Enchantment.Remove`
   都是「按来源 / 关键词整批摘」。切换 = **改动请求**（会改 `play all dump` 指纹）⇒ 等调度台排。
2. **`case 12` 的 quest 半**（账 ③）：`Enchantment.Remove`（`Enchantment.cs:326`）**没有** `BroadcastQuestResolved`
   与 `questCounter=0` 那一跳（现读 `grep Quest Enchantment.cs` 只在 `:481-520` 的 `ReachedQuestCounter` 族里有）⇒ **归 E4**。
   可达性：池内 `spellType == 120` **0 张** ⇒ 今天不可观测。
3. `case 45` 的**攻击那半**（`currentDropPodAttack`）在 `UnitState.RemoveEffectRecord` 的 doc 里已如实记「未建」（只有血量那半有落点）。

**验证读数**：新增 public 方法、零调用点 ⇒ `play all dump` 必逐字节不变；
离屏探针可直接调 `RemoveEffectRecordFrom`（`E10TB` 的 8 条断言夹具可复用）。

---

### L869 — `CardScript.ResolveCardPlayed(CardScript cardPlayed, CardScript targetCard)` — 建议 **B**（本笔零代码改动）

**判据现读**（`decomp_full/CardScript__ResolveCardPlayed.c:1-95` 全文）：
① `vtable+0x308`（`OnCardPlayedWithTarget`：**传真 `param_2/param_3`**，与 NonTarget 的「传自己/null」不同）；
② `IsTheSameTurnPlayed(this)` ⇒ `OnTrigger(590)`（Combo）；
③ `ResolveUnitSummoned(this, cardPlayed, targetCard, 0)`；
④ **仅当 `cardPlayed == this`**：`StopReflected` → `RemoveTraitSilently(5 ephemeral)` → `+0x30c = mgr.turnCounter`（记「本回合打出」）
   → `CleanStatusAnims` / `UpdateTraitIcons` → `HasCurrentTrait(0x28a swarm)` 且**右邻同卡名** ⇒ `AddExecuteSwarm(…, 3, 1)`。

**为什么本笔不改**：这三件事的**落点全在 `RuleCore.PlayCard` 那条单位出牌链**（`RuleCore/Presentation` 之外）：
- Combo：单位卡那一笔 `NotePlayed` 写点 = `RuleCore.cs:3986` 附近（`unit.DeployedTurn = ctx.Turn` 同一段）；
  `TryFireCombo` 已是现成的 private 帮手，**只要在单位出牌链的正文之后调一次**即可（见 §5 补丁）。
- 「打出时摘 ephemeral」：完整复刻 = `RemoveTraitSilently(5)`；我们的 `ephemeral` 只在**手牌**被消费
  （`BattleContext.cs:1544/1582` + `SweepEphemeral` at `RuleCore.cs:2889`），**场上单位不读它** ⇒ 净结果大概率等价，
  但账已判「按铁律 11 仍要做」⇒ 归 E2。
- `turnPlayed`：我们只有 `DeployedTurn`（`RuleCore.cs:3986` 写、`UnitState.cs:771` 存）—— 与 `IsTheSameTurnPlayed` 恒等。

**验证读数**：池内 `ephemeral` 词条 104 张（多为 hash 型 id）；`combo` 0 张 ⇒ 两者今天零可观测。

---

### L870 — `CardScript.ResolveDamageDealt(CardScript damagedCard, DamageInfo damageInfo)` — 建议 **B**（本笔零代码改动）

**判据现读**（`decomp_full/CardScript__ResolveDamageDealt.c:1-231` 全文）：
早退 = `damageInfo == null` / `absorbed(+0x24) != 0` / `amount(+0x18) == 0`；
- **syphon(0x6e=110)**：仅当「伤害来源 == 自己」且 `damageType(+0x1c) == 4` ⇒ `HealOneCharacter(自己, 督军, 1, …)`；
- **poisonous(0x17c=380)**：来源 == 自己、被打者是**单位且非督军等价**、`damageType == 4` ⇒ 给被打者 `AddEffect{buffType 3, trait 200}`；
- **rage 族 / ecstasy(0x2cb) / cruelty** ⇒ `OnTrigger(0x3c=60 UnitDamaged)` / 伤害类型 `0x3/0x4` 分 `0xb9(185)` / `0xbb(187)`；
- **concussive(0x3a2)**：只在 `damageType == 4` **且伤害来源 == 自己**时 `AddStun`（**主目标那一档**）。

**为什么本笔不改**：全部落在 `RuleCore.cs`（账落点 `:9004-9047` / `:8713-8832` / `:8492-8600`）—— E2 的文件。
现读我们的覆盖：`HealUnit(..., syphon)` 有形参但**没有生产调用方**（`RuleCore.cs:9270-9273` 自述）；
`grep poisonous/concussive` 在 `RuleCore.cs` / `EffectResolver.cs` = **0 命中**（concussive 只在
`UnitState`/`CardDef` 的词条表与 `DeclareAttack` 的眩晕那一格）。池内现扫：`syphon 0 · poisonous 0 · concussive 6`。

→ 见 §5 的补丁要点（归 E2）。

---

### L934 — `CardScript.TacticPlayed(CardScript actingCard, CardScript targetCard)` — 建议 **B**（②已改；①归 E2）

**判据现读**：
- 接收端 `out_cpp2il_dev_ilspy/CardScript.cs:12103-12110`：`actingCard.isPlayer == this.isPlayer && HasCurrentTrait(artifice)` ⇒ `rawCardData.OnTacticPlayed(...)`；
- 广播端次序 `decomp_full/CardScript__TargetedSpellPlayed.c:22-79`：`OnCardPlayedWithTarget`（正文）→ Combo(590) →
  **`BroadcastTacticPlayed`** → 突触 → `GoToCemetery`。

**改动（② 时机）**：`FireTriggerOnSide(ctx, p, KeywordTable.Artifice)` 从「效果结算 + 进弃牌**之后**」
上移到 `PlayTacticBodyDeferred` 的 `EffectResolver.cs:2777`（正文之后、突触与 `ps.Discard.Add` **之前**）；
原位置改成注释（`EffectResolver.cs:2800-2809`），并**就地订正**旧的「这是我们挑的」说法。
⚠️ 结构差如实标着：本版是**同步结算**（原版正文先入队、由泵点执行）⇒ 只能对齐**语句次序**，
「弃牌发生在队列动作执行之前」那一层表达不出来（写进注释了）。

**① `HasFx` vs `HasCurrentTrait`（归 E2，落点 `RuleCore.cs:13394-13410` 的 `FireTriggerOnSide`）**：
- 我们：`if (!t.HasFx(keyword)) continue;`（`UnitState.HasFx` = `Card.TriggerOps(kw) != null || Card.Effect(kw) != null || _grantedOps.ContainsKey(kw)`，`UnitState.cs:1053-1058`）；
- 原版：`HasCurrentTrait(artifice)`。
- **本笔把池内分叉算清了**：`artifice` 词条 **19 张** == `abilityTriggers` 含 **560** 的 **19 张**（逐张同一集合）；
  「有词条无正文」的 10 张（`BL25 BL32 GSC31 SAU25 SAU6 SAU69 SAU70 SW6 TL14 TL26`）的正文走
  `CardDef` 的**裸写正文**那一档（`keywords` 里带 `Artifice`、`descZh` 现读带 `[[artifice]]：` 前缀 ⇒
  `CardDef.cs:3885-3907` 的「整条 Desc 归哪个带正文关键词」），⛔ **不是**「没有正文」。
  池内**没有任何** `addTrait artifice` / `removeTrait artifice` ⇒ **今天 `HasFx` 与 `HasCurrentTrait` 零可观测差异**。
  ⚠️ 上面「裸写正文」这一条是**按代码路径推的，没跑探针**（见 §4）。

**验证读数**：本笔**会改指纹**（任何场上有 `artifice` 单位的局，巧技触发点提前）⇒ 调度台需 `play all dump` 逐字节 diff +
`play check` 指纹前后对比；观测量 = 巧技触发序（`EvtKind.Trigger` 的 keyword=artifice 的位置）。

---

### L935 — `CardScript.TargetedSpellPlayed(CardScript actingCard, CardScript targetCard)` — 建议 **A**（本笔补 Combo）

**判据现读**（`decomp_full/CardScript__TargetedSpellPlayed.c:1-84` 全文）：
`cardState=6` → `OnCardPlayedWithTarget(mgr, this, **param_2**, **param_3**)`（**真目标**，不是 null）→
Combo(`0x24e=590`) → `BroadcastTacticPlayed` → **synapse 五步**（同方 / 目标带 `0x47e` / 取相邻 / 再过 `targetCriteria` / `BroadcastUnitSynapse`）→ `GoToCemetery`。

**🔴 判据订正（铁律 5）**：批次施工单写「Combo(1130)」—— 现读 `.c:16` 是 **`OnTrigger(0x24e = 590)`**；
**`1130` 是 `DefinedTrait.combo`（词条号）**，不是触发号（`dump.cs:20306 AbilityTrigger.Combo = 590` / `:45832 DefinedTrait.combo = 1130`）。
⇒ 与本批 **L809 是同一个触发（590）、同一份实现**，不是两套。

**改动**：与 L809 共用 `TryFireCombo`（`EffectResolver.cs:17980`），调用点同一处（`:2776`）。
**`OnCardPlayedWithTarget` 的 target 传参**：现读 `.c` 这一路传的是**真 `param_3`**，与我们「传选中目标」**一致**
（账内「原版此路传 null」的说法只对 NonTarget 那一路成立，`R21` 已订正过，本笔复核确认 **⛔ 别把 NonTarget 那条套到本行**）。

**验证读数**：池内 590 / 1130 各 0 ⇒ `play all dump` 应逐字节不变；突触次数观测量 = 相邻重复次数（`TestStimulation` 族可复用）。

---

### L1278 — `RawCardScript.OnTriggerEffectInCard(AbilityTrigger, BattleManager, CardScript thisCard, CardScript actingCard, CardScript targetCard)` — 建议 **B**（本笔补入口，接线未做）

**判据现读**（🥇 `decomp_full/RawCardScript__OnTriggerEffectInCard.c:1-18` 全文）：纯转发 ——
`TriggerAbilities(trigger, mgr, thisCard, cardPlayed, targetCard, 新建 damageInfo, …, includeEnchantments=1)`。
真调用链 = `CardScript.TriggerOtherCardAbility(abilityCard)` → `abilityCard.OnTriggerEffectInCard(ThisCardPlayed, mgr, this, null, null)`；
原版 5 个调用点 = `ApplyEnvEffect.c:15` · `_StartBattlePhase_d__334__MoveNext.c:88/201` · `_ResolveSummonUnit_d__510__MoveNext.c:252` · `AbilityLogic__PlayAbility.c:2869`。

**改动**：`EffectResolver.cs:17940（RunAbilityOfAnotherCard）` —— 通用窄口：
按 `CardDef.TriggerOps(trigger 小写)` 取正文，命中则 `ResolveOps`（`source` = 在场单位 / `seed` = targetCard ?? actingCard），
**解析不出来就出声**（`chosenAbility` 驱动的那一档本版没有分派口，⛔ 不假装跑过）。

**仍未闭（接线）**：5 个调用点在我们的对应口都在**别的文件**（`RuleCore` / `BattleDriver`）⇒ 见 §5。
⚠️ **账内那句「环境卡按 `BattleContext.EnvName` 映射」现读不成立**：全仓 `grep EnvName` = **0 命中**；
真实字段是 `BattleContext.OffensiveSlotIdx / OffensiveEnvSO`（`BattleContext.cs:1029-1035`），
而进攻卡/防御卡的能力在我们这边走的是**专用表** `Core/OffensiveCardData.cs` + `Core/PlayedThirdCardInTurn.cs`
（键 = `ctx.OffensiveEnvSO`）—— 那就是账里说的「走别的路」。**铁律 5：此处就地订正**（本报告即证据；账主表由调度台合并时改）。

**验证读数**：新增 public 方法、零调用点 ⇒ `play all dump` 必逐字节不变。
接线后的验收口 = 环境卡生效时某单位的 `ThisCardPlayed` 正文真的跑了（今天无此现象）。

---

## §2 验证读数（本写手跑过的 / 没跑的）

**跑过**：
- `TMPDIR=/tmp/wf_B89E3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- 行尾/行数：`CRLF 18079 / LF 18079 / bare CR 0`；中段锚点 :2666 :2792 :2825 :13180 :13183 全部未动（净 0 行）。
- 池内数据现扫（`cards_engine.json`，**count 现读 = 1129**）：
  - `abilityTriggers`：`490 = 4`（DA1/DA2/DA3/DA75）· `560 = 19` · `590 = 0` · `1130 = 0` · `0(ThisCardPlayed) = 587`
  - `chosenAbility` 含 270 = 7（`DA44 DA1 DA2 DA3 DA72 DA75 GSC55`）
  - 词条计数：`artifice 19` · `pack 10` · `concussive 6` · `syphon 0` · `poisonous 0` · `combo 0` · `droppod 0`
  - `spellType == 120`（quest 附魔）= **0**

**没跑（按红线，归调度台）**：
- `ruleprobe check` / `play check` / `play all dump` —— **本笔有两处会改指纹**（L934② 巧技时机、L212 的 490 分支虽零可达但属新代码路径）
  ⇒ 请在**没有 `.cs` 写手**的窗口跑 `play all dump` 逐字节 diff + `play check` 前后指纹。
- Unity 宿主自检（本轮不跑；恢复条件见 CLAUDE.md 铁律 12 的 2026-10-25 口径）。

---

## §3 顺手发现（⛔ 未擅自改，交调度台分流）

1. **`Combo(1130)` 是触发号/词条号混用**（L935 施工单）—— 真值：`AbilityTrigger.Combo = 590`、`DefinedTrait.combo = 1130`。
   本笔按 590 实现，并在 §1/L935 留了更正痕迹。
2. **账 L1278 的「`BattleContext.EnvName`」不存在**（全仓 0 命中）—— 真字段 = `OffensiveSlotIdx/OffensiveEnvSO`
   （`BattleContext.cs:1029-1035`），专用链 = `Core/OffensiveCardData.cs` + `Core/PlayedThirdCardInTurn.cs`。
3. **`RuleCore.ApplyHandBuffs` 合并 op 列表时丢掉 `e.Extrinsic`**（`RuleCore.cs:10562-10572`）——
   这就是 L64「extrinsic 判不到」的**具体根因**，不是「判据缺失」。
4. **`cards_engine.json` 的 `count` 现读 = 1129**（有的文档还写 1127/1128）—— 引用张数一律现读。
5. **`What` 无关但会误导**：`HasFx(keyword)` 的语义是「有正文」而不是「有词条」，
   名字容易让人以为等价于 `HasCurrentTrait`；池内今天恰好在 artifice 上重合（§1/L934），
   将来一旦有 `addTrait/removeTrait artifice` 就分叉。

---

## §4 没查清（如实，别当结论）

1. **`TriggerOps("artifice")` 对那 10 张「裸写正文」卡是否真的非空** —— 我按 `CardDef` 的
   `AddTriggerOp` / 裸写正文路径（`CardDef.cs:3885-3907`、`:5140`）推断为**是**，但**没跑探针**证实。
   请调度台用 `ruleprobe` 对 `BL25` / `SAU25` 各跑一条（看巧技触发时有没有正文）。
2. **本版同步结算 vs 原版入队**在 L934 上的**可观测后果**（巧技与正文的先后）—— 我只能对齐语句次序，
   真差异要拿原版实况或更细的队列模型判。已写进代码注释，**不假装等价**。
3. **L870 的 syphon/poisonous「伤害类型 4」到底是哪种**（`DamageType`/`AttackTypes` 的哪个值 = `+0x1c == 4`）——
   本笔没展开（该行归 E2，且池内两类词条各 0 张）。E2 动手时**必须先把这个枚举坐实**，否则会把「近战伤害」当条件。
4. **`case 45 addStatsDropPod` 的攻击那半**（`currentDropPodAttack +0xe0`）在我们模型里**没有对应物**——
   `UnitState.RemoveEffectRecord` 的 doc 已记「只有血量那半」，攻击那半怎么落（是否要新字段）**没查清**。

---

## §5 交接给下一个代理（Chain E 第 2 棒 = 批次 5）

### 5.1 我在 `EffectResolver.cs` 动过哪些符号（下一个写手别重复建）

**追加在类尾（新成员，:17860 之后）**：
| 符号 | 行 | 可见性 | 用途 / 账行 |
|---|---|---|---|
| `IsThereValidSpellTarget(ctx, owner, card, asIfOnBoard=null)` | **17895** | public | L429 |
| `RunAbilityOfAnotherCard(ctx, abilityCard, triggerName, owner, abilityOwnerUnit=null, actingCard=null, targetCard=null, by=null)` | **17940** | public | L1278 + L212 |
| `TryFireCombo(ctx, owner, card, inst, chosen)` | **17980** | **private**（批次 5 若要在 `BroadcastWhen` 里也用，改成 `internal`/`public` 或自行新增调用点） | L809 / L935 |
| `RemoveEffectRecordFrom(ctx, u, rec, fromJam=false, by=null)` | **18028** | public | L853 |

**中段改动（净 0 行，已核对锚点）**：
| 位置 | 改动 |
|---|---|
| `PlayTacticBodyDeferred` `:2776-2777` | 新增 `TryFireCombo(...)` + `FireTriggerOnSide(ctx, p, KeywordTable.Artifice)`（巧技触发点上移） |
| 同函数 `:2800-2809` | 原巧技调用位置改成注释（旧口径「排在效果与弃牌之后」**已作废**，就地订正） |
| `PlayTacticBodyDeferred` `:2765-2779` 注释块 | 换成「原版 `.c` 语句次序」五步 + 结构差说明 |
| `QuestPointThreshold` `:13227-13229` | 新增「督军卡 490 卡能力先行」三行（`_qW` / `_qOps` / `if (… RunAbilityOfAnotherCard …) return;`） |

### 5.2 待接点（都要在**别的文件**里落，本写手无权改）

1. **L934①** — `RuleCore/Core/RuleCore.cs:13394-13410` `FireTriggerOnSide` 的
   `if (!t.HasFx(keyword)) continue;` → 按 `keyword == KeywordTable.Artifice` 走 `t.Has(KeywordTable.Artifice)`（= `HasCurrentTrait`）。
   ⚠️ 这条会**改触发面**（虽然池内今天零分叉，见 §1/L934）⇒ 与 L934② 一起跑 `play all dump`。
2. **L212 剩余** — 想让 `chosenAbility 270`（`createCardInDeck`）真的跑起来：需要在 `RuleCore` 的
   **chosenAbility 分派**里加 270 一档（`AbilityLogic__PlayAbility.c:2607`），然后 `QuestPointThreshold` 就能直接走卡能力、
   把「阵营 == DarkAngels」代理整个删掉。
3. **L869（单位侧 Combo + 摘 ephemeral）** — `RuleCore.PlayCard` 单位出牌链（`RuleCore.cs:3986` 附近，`unit.DeployedTurn = ctx.Turn` 同一段）：
   - 在**正文结算之后**调一次 `TryFireCombo`（当前 private ⇒ 改可见性或把逻辑搬过去）；
   - 在 `cardPlayed == this` 那一支加 `u.RemoveAll("ephemeral", silent: true)`（= 原版 `RemoveTraitSilently(5)`）。
4. **L870** — `RuleCore.DeclareAttack` / `RuleCore.EmitHit` 那一族补三支：
   `syphon`（来源有 `syphon` 且伤害类型 4 ⇒ `HealUnit(自己, 1, syphon:true)` 的**督军**那一半）、
   `poisonous`（来源有 `poisonous`、被打者是单位非督军等价、类型 4 ⇒ 给被打者 `ApplyTraitEffect("poisoned", 1)` 一类的 `addTrait` 记录）、
   `concussive` 从「只主目标」扩到 `ResolveDamageDealt` 覆盖的每一条伤害路径。
   ⚠️ 先坐实「伤害类型 4」的枚举值（§4·3）。
5. **L853 的 `case 12` quest 半** — `Enchantment.Remove`（`Enchantment.cs:326`）补：
   摘掉的那一刻，若 `effect.EnchantmentCard` 满足 `cardType == 0x28 ∧ spellType == 0x78 ∧ !fromJam`
   ⇒ `RuleCore` 侧发 `QuestResolved`（我们今天的等价物 = `RunAbilityOfAnotherCard(warlord, "QuestTriggered", …)` 那条链）
   + `carrier.Instance.QuestCounter = 0`。可达性今天 0（`spellType 120` 池内 0 张）。
6. **L1278 接线（5 处）** — 最直接的一处：环境卡链。原版 = `ApplyEnvEffect` 里让**督军**跑环境卡的 `ThisCardPlayed`；
   我们的对应点 = `RuleCore.ChooseOffensiveCard`（`RuleCore.cs:527-540`）之后、战斗开始前的应用口。
   ⚠️ 现池两张进攻卡的 SO **没有 desc**（`OffensiveCardData.cs` 头注），能力是 `trigger 15` 的**逻辑表**
   ⇒ 用 `RunAbilityOfAnotherCard` 只会出声「解析不出来」。**正解**是把 `Core/PlayedThirdCardInTurn.cs` 那张表**也**当作
   `OnTriggerEffectInCard` 的一个数据源接进来（或等本地化/desc 到位）。
7. **L64 extrinsic 生产者** — 见 §1/L64 的 ①② 两条补法（`BattleContext` 作用域量 + `ApplyHandBuffs` 逐条 + `Aura.ApplyAura` 改口）。

### 5.3 批次 5（触发开关族）与你共享本文件的注意事项

- ⛔ **别把新成员插到类中间**：`账/*.tsv` 按 `本文件:行号` 落点，插一行就整体漂移（我这一笔已核过锚点）。
- 批次 5 要的三个开关（`resolveOnlyEnchantments` / `dontResolveEnchantments` / jam 闸）建议**落在 `RunAbilityOfAnotherCard`
  与 `FireTriggerAt` 那条触发族旁边**（类尾追加），⛔ 别塞进 `PlayTacticBodyDeferred`（那里已经动过、且行号敏感）。
- `TryFireCombo` 现在是 `private` —— 若批次 5 要在 `BroadcastWhen` 里复用，**改可见性即可**（同文件内），别再写第二份。
- `play all dump` 的已知影响：本笔只有 **L934②** 会改指纹（巧技时机）；L212 / L809 / L929 / L1278 的代码路径今天**零可达**。
