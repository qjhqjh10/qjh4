# 战斗逻辑驱动 · Stage 2 前置判据（第 2 轮 · `Q1`）

> 覆盖两件（都要求「照着做，不用再读原版」）：
> **② `meleeAttackValueOptions` 在哪一步折成 `meleeAttackChange`** —— 给 `E1`（Stage 2c）等的那条。
> **③ K2/K3 `addTrait(368)` 的实现判据** —— 给下一个执行代理。
>
> 前置真相（`_stackable` 逐 trait 真值）→ **`判据/CardTrait_stackable_真值表_第2轮.md`**（本轮已实读定案：74×`add` / 1×`count` / 2 无 SO）。
> 判据优先级：🥇 `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/**.cs`（IL）→ 🥈 `D:/2/tools/decomp_full/**.c`（Ghidra，**参数个数不可信**，见 §三）→ 🥉 解包 SO。冲突时按本节 §三 的裁定。

---

## 〇、结论速览（三句话）

1. **② 是「快照」，不是「活读」**：`*ValueOptions` → `*Change` 的折叠发生在 **`AbilityLogic.PlayAbility` → `AdjustCardEffect(ref CardEffect, …)`**，作用对象是**从卡的静态技能模板 `GetNewInstanceOfThisEffect()` 克隆出来的那一份新实例**，折完再交给 `AddEffect`。
   ⇒ **存进 `activeEffects` 的记录里已经是具体数字**；之后读它的人（`CurrentMeleeAttack` 等）**只读 `*Change`，从不重算 `*ValueOptions`**。
2. `240 WhileInPlayVariable` 存在的**唯一理由**就是「快照会过期」：它靠**反复重挂**（每次广播 → 重新 `PlayAbility` → 重新克隆 + 重新折叠）刷新；重挂时**同一条被替换而不是叠加**（键 = `enchantingCard` + `IsSameEffect`）。重挂点已逐条列出（§1·4）。
3. **K2/K3 可以直接开工**：`_stackable` 真值已定；`addTrait` 的完整语义（合并三档 / `AddTraitSilently` 去重 / 11 条 trait 特例 / 收尾三跳）在 §二 逐条给出，且**与 `removeTrait(4)` 的对称关系**也写清了。

---

## 一、② `meleeAttackValueOptions` 的折叠点（**定案：快照**）

### 1·1 调用链（一条，无分叉）

```text
AbilityLogic.PlayAbility(manager, abilityData, trigger)          // AbilityLogic.cs（.c: AbilityLogic__PlayAbility.c:2）
  └─ local_170 = CardEffect.GetNewInstanceOfThisEffect()          // 克隆卡的静态技能模板（CardEffect.cs:91-140，逐字段整份复制，含 traitValue / *Change / *ValueOptions）
  └─ AbilityLogic.AdjustCardEffect(ref local_170, abilityData, multiplier, manager)
        // AbilityLogic.cs:2450-2474；.c: AbilityLogic__AdjustCardEffect.c:1-…（**唯一调用者就是 PlayAbility**，全仓 4 个调用点，`.c:839 / 918 / 1188 / 1359`）
        //   对 buffType ∈ {1 addStats, 3 addTrait, 90 addTraitRemnant, 2 changeCost, 10 changeAbilityCost, 70 setMeleeAttack, 85 setRangeAttack, 95 setCost} 才做：
        //   meleeAttackChange = NewBuffStat(manager, effect.meleeAttackValueOptions, effect.meleeAttackChange, abilityData, multiplier, altvalue)
        //   rangeAttackChange = NewBuffStat(… rangeAttackValueOptions, rangeAttackChange …)
        //   healthChange      = NewBuffStat(… healthValueOptions,     healthChange …)
        //   armourChange      = NewBuffStat(… armourValueOptions,     armourChange …)
        //   costChange        = NewBuffStat(… costValueOptions,       costChange …)
        //   ⇒ **写回同一份 CardEffect 的 `*Change` 字段**（`.c:18-19` `*(undefined4 *)(lVar1 + 0x48) = uVar2;`）
  └─ CardScript.AddEffect(effect, …)  或  BattleManager.AddEffect(…)  或  BattleManager.AddSummonUnit(…, effect, …)
        // `.c:1161` / `:1218` / `:1369` / `:2838`
```

**⇒ 存进 `activeEffects` 的那条记录 = 克隆 + 折叠后的快照。** 之后 `EntityScript.CurrentMeleeAttack`（`EntityScript.cs:580-637`）只做 `base + Σ effect.meleeAttackChange`，**完全不看 `ValueOptions`**；`setMeleeAttack` 更是**命中即 `return cardEffect.meleeAttackChange`**（同一段 `:612-616`）。

### 1·2 三条独立证据（为什么不能是「活读」）

| # | 证据 | 位置 |
|---|---|---|
| A | `AdjustCardEffect` **只被 `PlayAbility` 调**（全仓 4 处、同一个方法） | `decomp_full/AbilityLogic__PlayAbility.c:839,918,1188,1359`；`grep -rn AdjustCardEffect` 全仓仅 `AbilityLogic.cs:2450` 一处声明 |
| B | 折叠前的对象是 **`new` 出来的克隆**，不是卡上的模板本身 | `AbilityLogic__PlayAbility.c:836` `local_170 = CardEffect__GetNewInstanceOfThisEffect(...)` |
| C | **读点只读 `*Change`**，同一段代码里没有任何地方回读 `*ValueOptions` | `EntityScript.cs:580-637`（近战）/`:639-700`（远程，另加 `blind ⇒ 0`） |

**⚠️ 附带事实（E1 可能会撞到）**：`AdjustCardEffect` **不会清掉 `*ValueOptions`** —— 快照里两个字段**同时存在**（既有解出来的 `Change`，也有原来的 `Options`）。⛔ 我们的实现如果两个都存，**读的时候必须只读 `Change`**；否则「同一件事两种表示」迟早不一致（`CLAUDE.md` §三 那条）。

**`multiplier` 是什么**：`PlayAbility` 内算出的一个 `int`（`AdjustCardEffect` 第 4 实参），`NewBuffStat` 在部分分支里把它当乘数用（如 `minValue * multiplier`，`AbilityLogic.cs:2382`）。**它不是触发档**（触发档是 `PlayAbility` 的第 4 个形参 `trigger`，两者不是同一个变量）。

### 1·3 `NewBuffStat` 读的是什么「状态」

`NewBuffStat(manager, valueOption, minValue, abilityData, multiplier, altvalue)`（`AbilityLogic.cs:2483-…`）按 `ChooseValueOptions` 分支，**在调用这一刻**读**当时的**棋盘状态。已核到的读点举例（全是**结算时**的活值）：

| valueOption | 读的东西 | 位置 |
|---|---|---|
| `130` | `entityScript.CurrentCost` | `AbilityLogic.cs:2402` |
| `140` | `manager.GetHero(playerHeroY:false).CurrentMeleeAttack` | `:2416-2420` |
| `150` | `entityScript.CurrentBastion` | `:2434` |
| `TargetHealth` | 目标当前生命 | `:2546` 一带 |
| `ThisCardHealth` / `TargetBastion` / `SelfBastion` / `TargetCardHealthMinusAttack` / `ThisCardMarksOfChaos` / `MinPlusMaxTimesMultiplier` / `RandomizedOnResolution` … | 各自现读/现掷 | `:2533-2720` |

⇒ **「折叠」= 在 PlayAbility 那一刻把活值抄成数字**。它既不是「建卡时算」，也不是「消费时算」。

### 1·4 `240 WhileInPlayVariable` 的重挂点（这就是「刷新」机制，**已逐条定位**）

**触发链**：`CardScript.UpdateWhileInPlay(bool unitJustSummoned)`（`CardScript.cs:16018-16039`）
→ `RawCardScript.UpdateWhileInPlay`（`RawCardScript.cs:1239-1253`）**连着发两档**：
`TriggerAbilities(WhileInPlay=20)`、`TriggerAbilities(WhileInPlayVariable=240)`
→ 每条 ability 的 `AbilityLogic.PlayAbility`（`RawCardScript.cs:1798-1814`）。
两档的 `AbilityData` 由 `TriggerAbility` 打标（`RawCardScript.cs:1769-1778`：`whileInPlay=true` / `whileInPlayVariable=true`），最终落到效果上：
**`extrinsic = (trigger ∈ {20,240})` · `extrinsicVariable = (trigger == 240)`**（`AbilityLogic__PlayAbility.c:1193-1199`、`:1352-1356`；已由 `判据/战斗逻辑驱动_前置门与现算口径.md` §7·1 定案）。

**重挂点（`UpdateWhileInPlay` / `BroadcastWhileInPlay` 的调用者，全部现读）**：

| # | 调用点 | 场景 | 出处 |
|---|---|---|---|
| 1 | **`BattleManager.FinishResolvingAction` 尾部 `BroadcastWhileInPlay(this, false, null)`** | **每个动作结算完之后**（**最主要的那个**） | `BattleManager.cs:35318` |
| 2 | `BroadcastWhileInPlay` 本体 | 遍历 `unitsInPlay` + **双方手牌**，只对 `HasWhileInPlayAbility()` 的单位，`unitExcluded`/`unitExcluded2` 跳过 | `BattleManagerSupport.cs:870-939` |
| 3 | `ResolveTransformUnit` | 单位变形（自己 `UpdateWhileInPlay(true)` + 广播） | `BattleManager.cs:29859` / `:29946` |
| 4 | `ResolveTransformFromRemnant` | 残骸复活（同上，`<>c__DisplayClass555_0._003CResolveTransformFromRemnant_003Eb__0`，`BattleManager.cs:412-423`） | `:30398` / `:30405` |
| 5 | `ResolveSummonUnit`(协程) | 召唤单位落地 | `:11675` |
| 6 | `ResolveCardAutoSummoned`(协程) | 自动召唤 | `:5147` |
| 7 | `ResolveExchangeMinion`(协程) | 交换随从（2 处 `UpdateWhileInPlay` + 1 处广播） | `:6990` / `:6998` / `:7009` |
| 8 | `ResolveStealMinion`(协程) | 抢人 | `:11513` / `:11516` |
| 另外 | `CardScript` 内部（非广播）：`ResetToValuesInHand` 尾巴 `UpdateWhileInPlay(false)` | 卡回手/重置 | `CardScript.cs:20117` |

**⇒ 结论**：**「重挂」不是定时器，是「每个动作结束 / 召唤 / 变形 / 抢人 / 换人」这一串事件驱动的。**
⛔ 别实现成「每帧重算」或「TurnStart 重算一次」——都不等价。

### 1·5 重挂时「替换还是叠加」—— 必须照抄的两条闸

`AddEffect` 进门的三段（`CardScript.cs:17466-17560`；`.c:87-130`）：

| 情形 | 行为 | 出处 |
|---|---|---|
| **非 extrinsic**（= 普通效果 / 非 20·240 触发） | 不走 extrinsic 去重，直接进主体 | `CardScript.cs:17466-17470` |
| **extrinsic 且 `extrinsicVariable == false`**（= trigger 20） | 若 `ContainsExtrinsicEffectFrom(enchantingCard, buffType, baseEnchantmentCard)` 已为真且不是 `removeExtrinsic` ⇒ **`return false`（整条丢弃）** —— 即 **trigger 20 的重挂是「幂等空操作」** | `:17472-17484` |
| **extrinsic 且 `extrinsicVariable == true`**（= **trigger 240**） | 从 `activeEffects` **正序**扫：遇到 `existing.extrinsicVariable != 0 ∧ existing.enchantingCard == new.enchantingCard ∧ IsSameEffect(existing,new)` ⇒ **`return false`**（一模一样，不用换）；否则走到 `RemoveExtrinsicEffectsFrom(newEffect.enchantingCard)`（**把这张施放卡的旧快照全撤掉**）再继续 | `:17485-17560`；`.c:100-130` |

`CardEffect.IsSameEffect`（`CardEffect.cs` `IsSameEffect`，IL 现读）比的是：`buffType · enchantingCard · enchantmentCardId · extrinsic · extrinsicVariable · 五个 until* 时机 · meleeAttackChange · meleeAttackValueOptions · rangeAttackChange · rangeAttackValueOptions · healthChange · healthValueOptions · costChange · costValueOptions · randomValueChange`。
🔴 **它不比 `trait` / `traitValue`** —— 「同一张施放卡挂两个不同 addTrait」在 240 档会被判成「同一条」而丢掉第二条。**池内今天 0 例**（见 §1·6），如实记下、别"顺手修"（原版如此）。

**⇒ 对 `E1` Stage 2c 的硬要求（三条）**：
1. **投影（`CurrentMeleeAttack` 等）只能把 `*Change` 累加进去**，⛔ **不允许在投影里再算 `*ValueOptions`**（那等于把快照改成活读，`240` 会变成另一个语义）。
2. 我们的「重挂」必须挂在**动作结算完之后**（对应 `FinishResolvingAction`），并**按施放卡整份替换**，不是「再叠一条」。
3. `IsSameEffect` 的**字段表**要照抄（少比一个字段 ⇒ 该刷新的不刷新 / 不该刷新的刷新）。

### 1·6 池内读数（`cards_truth.json`，1,351 张 SO，整卡扫）

| 触发档 | `addTrait` | `addTraitRemnant` | 卡 |
|---|---|---|---|
| `WhileInPlay(20)` | **5** | 0 | Vitus Gryf`flank` · Devilfish`flank ×2`（同卡两条 ⇒ 第二条被 §1·5 那条幂等闸丢掉，**同一 trait、无损失**） · Bjorn the Fell-Handed`dontReturnFerocity` · Vico Therbeus`oathInAllTurns` |
| `WhileInPlayVariable(240)` | **11** | **1** | Acid Rain`vulnerable 1` · Bringer of Decay`vulnerable 1` · Ravenwing Talonmaster`flank` · Alluress`flank` · Damaged Plasmacyte`regeneration 2` · Fyrri Askar`invulnerable` · Iron Priest`regeneration 1` · Wolf Guard Battle Leader`pack` · Honoured Ethereal`vanguard` · Ferren Areios`oathTripleActivation` · Whirlwind`sentry 2` · **Nemesor Zahndrekh`notDestroyRemnant`（90）** |
| 合计 | 16 条 / 16 张卡 | 1 | （与 `前置门与现算口径.md` §7·1 的「20 → addTrait 5 / 240 → addTrait 11 + addTraitRemnant 1」**逐数吻合**） |

> **240 档里 10 条 `traitValue == 0`**、只有 4 条带数字（`vulnerable 1`×2 / `regeneration 1·2` / `sentry 2`）。⇒ **票面值刷新**真正可观察的只有这 4 条；其余 12 条只是「挂/摘」这件事会被反复确认一次（幂等）。

### 1·7 🔴 直接回答 `E1` 卡住的那一问：`SAU27 Scarab Swarm` **不用自己发明刷新点**

`E1` 的报告（`报告/E1_属性模型Stage2_第2轮.md:197-198`）把这张卡**搁置**了，理由是「按快照实现会让它彻底失效（`Health` 变了不会刷）」。
**本轮把它的数据与机制核完了 —— 原版自己就有刷新点，不用新造：**

| 项 | 现读 |
|---|---|
| 卡 | `SAU27 Scarab Swarm`（`cards_truth.json`）：`melee 8 · health 8 · regeneration 3` |
| 技能 | `abilities[0].trigger = **240**（WhileInPlayVariable）` · `buff.buffType = **70**（setMeleeAttack）` · `buff.meleeAttackValueOptions = **50**` = `ThisCardHealth`（`dump.cs` 的 `ChooseValueOptions` 显式值现读：30 TargetHealth · **50 ThisCardHealth** · 70 ThisCardDamage） · `extrinsic 1 · extrinsicVariable 1` |
| ⇒ 原版怎么「跟血走」 | **每次广播重挂一次**（§1·4 第 1 条：`FinishResolvingAction` 尾部 `BroadcastWhileInPlay`）⇒ 重新 `PlayAbility` ⇒ `AdjustCardEffect` **重新读当时的 `CurrentHealth`** 折成 `meleeAttackChange` ⇒ `IsSameEffect` 因为 `meleeAttackChange` 变了而**返回假** ⇒ `RemoveExtrinsicEffectsFrom(这张卡)` 把旧快照撤掉 ⇒ `AddEffect` 写入新快照 |
| ⇒ 语义边界（照抄） | **不是「读时活读」，是「每个动作边界刷一次」** —— 一次动作**内部**（比如同一次结算里先扣血再攻击）看到的仍是**本动作开始前**的旧值，直到该动作结算完才更新。⛔ 别为了它把 `CurrentMeleeAttack` 改成读 Health（那会与 `SAU27` 之外的 1,127 张卡全不一样） |
| ⇒ 对我们的落法 | 我们的「重挂」实现好之后，这张卡**自动**就对；在重挂没做之前，**保持现状（只在 `FieldAttack` 读 Health）比按「一次性快照」实现更接近原版**（E1 的判断是对的，别改） |

> ⚠️ 附带：`setMeleeAttack` 在 `CurrentMeleeAttack` 的 getter 里是**命中即 `return`**（`EntityScript.cs:612-616`）⇒ 一旦有这条记录，**`addStats` 的近战加成全部不再计入**。这解释了为什么它是 240 而不是常驻 buff —— 值必须靠重挂才能「重新对齐」，且它天然吞掉其它近战修正（原版如此、照抄）。

---

## 二、③ K2/K3 `addTrait(368)` 实现判据（照此实现即可）

### 2·0 适用范围与前置

- 本文覆盖 **`buffType == 3 addTrait`** 与 **`buffType == 90 addTraitRemnant`** —— 两者在 `AddEffect` 里**走同一整块**（`.c:219` `if (iVar16 == 0x5a) goto switchD_..._caseD_3;`），只有两处差异：**残骸门**与 **`RemoveActiveEffects` 的键值**。
- 池内 **370 条**（`addTrait` 368 + `addTraitRemnant` 2）。逐 trait 分布与 `traitValue` 分布 → `CardTrait_stackable_真值表_第2轮.md` §2。
- 前置闸（卡状态 / jam / extrinsic 去重 / 残骸门 / 施放者不在场）**已实现并验过**（`报告/A_*` 与交接 §一·4）——本文**不重述**，只在流程里标出「过闸之后从哪开始」。

### 2·1 完整流程（逐步 · 带原文行号）

> 记号：`E` = 待加的那条 `CardEffect`（已是**快照**，见 §1）；`T = E.trait`；`SO = CardTraitCollection.GetTrait(T)`。

| 步 | 做什么 | 判据 |
|---|---|---|
| **S1** | 过前置闸（卡状态 ∈ {waitingToDie, inCemetery, removedFromGame} ⇒ `return 0`；施放卡带 `jam` 且 `E.extrinsic` ⇒ `return 0`；extrinsic 去重；残骸门） | `.c:66-72`（状态闸，**无符号比较**：`state-5U < 2` ∨ `state == 0xb`）· `:84-90`（jam）· `:91-…`（extrinsic） |
| **S2** | `Log("Add Trait <T>")`；`SO = GetTrait(T)`。🔴 **`SO == null` ⇒ 整条中止（`return false`，什么都不做）** | `.c:461-468`；`GetTrait` = `CardTraitCollection.cs:334`（`ContainsKey` 假 ⇒ `null`） |
| **S3** | **毒 × 抗性闸**：`T == poisoned(200)` ∧ 身上 `HasCurrentTrait(resistant=600)` ⇒ **这次中毒作废** + `HighlightTraitIcon(resistant)` + `DisplayStatusAnim(resistant)` + **break（不挂）** | `.c:469-…`；`CardScript.cs:17631-17643` |
| **S4** | **survivor 零血闸**：`T == survivor(430)` ∧ `E.healthChange == 0` ⇒ `LogWarning("Ignoring survivor trait granted with 0 health change")` + **不挂** | `.c:470-471`；`CardScript.cs:17646-17650` |
| **S5** | **清同名 removeTrait 记录**：`RemoveActiveEffects(BuffType.removeTrait, T)` —— 🔴 **只清 `T` 这一条**（不是全清，见 §三裁定） | `CardScript.cs:17651`；`.c:474`（Ghidra 少印第 3 个实参，**以 IL 为准**） |
| **S6** | **`blind(975)` 特例**：① `E.untilEnemyTurnStart / untilEndOfTurn / untilNextAttack` **全置 false**；② 若当前攻击型 ≠ Melee ⇒ 置 Melee + `CardUI.AttackTypeChanged` + 播 `changeToMeleeAttackCUE`；③ `blindedAtStartOfTurn(+0x56) = false` | `.c:476-493`；`CardScript.cs:17652-17667` |
| **S7** | **挂载去重循环**：从 `activeEffects` **末尾向 0** 扫；命中条件（**六条全真**）= `buffType ∈ {3,90}` ∧ `trait == T` ∧ `markOfChaosType` 相同 ∧ `daemonHostType` 相同 ∧ `SameTimingAsEffect(E)` ∧ **两侧 `extrinsic` 都为假**。命中 ⇒ 按 `SO._stackable` 三选一（§2·3），**并置「已合并」标记**，**不再 `Add`、不再 `AddTraitSilently`** | `CardScript.cs:17937-18092`；`.c:495-560` |
| **S8** | **未命中** ⇒ ① `if (SO._stackable == count) E.traitValue = 1;`（**强制 1**）② `activeEffects.Add(E)` ③ **`AddTraitSilently(T, hide:false, defaultIntValue:0)`** | `CardScript.cs:18107-18122`；`.c:561-580` |
| **S9** | **两条路都要走**的逐 trait 特例（§2·2 表） | `CardScript.cs:18124-18214`；`.c:570-640` |
| **S10** | **在场才播**：若卡状态 ∈ {`inPlay(2)`, `inPlayAttacking(3)`, `inPlayTransforming(15)`, `inPlayAminingAttack(17)`} ∧ `SO._hasInPlayAnim` ⇒ `CardUI.DisplayStatusAnim(T)` | `CardScript.cs:17901-17931`（`CardStateOptions` 枚举现读） |
| **S11** | 收尾三跳：`CardUI.CleanStatusAnims()` → `CardUI.UpdateTraitIcons()` → **`UpdateFigures()`** → **`CheckPossibleCombinations()`** → `return true` | `CardScript.cs:18216-18229 / 18232-18244`；`.c` 尾 `LAB_1805e6a2c / a36`（**`UpdateFigures` 在 `pindown` 那条支里会被跳过** —— `.c:665` `if (!bVar2) goto LAB_1805e6a36`，照抄） |

**`UpdateFigures()` 到底重算什么**（`CardScript.cs:17202-17216`）：`UpdateHealthText` · `UpdateAttackText` · `UpdateCostText` · `CleanStatusAnims` · `UpdateTraitIcons` · **`CheckIfDead()`（单位才做）**。
⇒ **原版**在「加 trait」之后**没有**任何「重算攻击/光环/修饰表」的缓存动作 —— 因为 `CurrentMeleeAttack` / `CurrentRangeAttack` / `HasCurrentTrait` / `CurrentXxx` **全是读时求值**（§1·2 证据 C）。
⇒ **我们这边不一样**（有 `_keywords` / `Armor` / `Exhausted` / 修饰表这些**落地表示**）⇒ 见 §2·5 的「改动清单」，**唯一必须做的显式重算 = `fast`/`flank` 的 `RecomputeActAndAttack`**（已做）。

### 2·2 逐 trait 特例表（S9，**加 trait 时**）

| trait | 动作 | 出处 |
|---|---|---|
| `fast(40)` / `flank(460)` | **`UpdateSummonSicknessAnims()`** ⇒ 重算 `canAct` / `canAttack`（`displaySummonSickness = +0x58 && !fast && !flank`） | `.c:570-572`；`CardScript.cs:18125-18128` |
| `dropPod(230)` | `currentDropPodHealth = GameStaticData.dropPodMaxHealth` · `currentDropPodAttack = GameStaticData.dropPodAttack`（**重置成默认值**） | `CardScript.cs:18129-18137` |
| `survivor(430)` | `RemoveTraitSilently(survivorSpent=440)` | `CardScript.cs:18138-18141` |
| 身上有 `poisoned(200)` 且**这次加的是 `resistant(600)`** | `DisplayStatusAnim(resistant)` + **`RemoveTrait(poisoned)`**（**整条摘掉**） | `.c:…`；`CardScript.cs:18142-18151` |
| `tide(910)` 且 `E.traitValue != 0` | `tideCounter = CurrentTide`（= `CardTraitData._traitValue + Σ addTrait.traitValue`） | `CardScript.cs:18152-18156` |
| `companion(1180)` 且 `E.traitValue != 0` | `companionCounter = CurrentCompanion` | `:18157-18161` |
| `pindown(970)` | **强制攻击型 = Ranged**：若当前不是 Ranged ⇒ 置 Ranged + `CardUI.AttackTypeChanged` + `changeToRangeAttackCUE`；**这条会跳过 `UpdateFigures`**（见 S11） | `.c:625-640`；`CardScript.cs:18162-18185` |
| `markerlight(1170)` | `BattleManager.BroadcastMarkerlightResolved(this)` | `CardScript.cs:18189-18197` |
| `huntMark(1260)` | `CardUI.DisplayTriggerAnim(huntMark)` + `BattleManager.BroadcastHuntMarkResolved(this)` | `:18198-18211` |

> ⚠️ **这些特例对「合并成功」的那条路也照走**（S7 命中 ⇒ 跳到 `IL_1ea9`，即特例段的入口）。⇒ **重复授予同一条 trait 仍会重播广播/状态动画**，别顺手加「已存在就早退」。

### 2·3 `_stackable` 三档怎么落（S7）

| `_stackable` | 合并动作 | 本 build 命中数 |
|---|---|---|
| `replace(0)` | `旧.traitValue = Math.Max(旧, E.traitValue)` | **0 条 trait**（写不写随实现方，**零可观察量**） |
| `add(10)` | `旧.traitValue += E.traitValue` | **74 条**（= 本 build 的全部常规情形） |
| `count(20)` | `旧.traitValue += 1`（**不看 `E.traitValue`**） | **1 条：`markOfChaos(330)`** |
| 其它值 | 只保留旧值（不动、但算「已合并」） | 0 |

- `count` 的**新挂**那一路还有一句 **`E.traitValue = 1`**（S8①）⇒ 「契约份数」从 1 起算。
- **合并只在「六个条件全真」时发生**；条件不真（典型：`untilEndOfTurn` 不同，或一侧是 `extrinsic`）⇒ **另起一条记录**，读取时**两条相加**（`GetCurrentTraitValueWithModifiers`，`EntityScript.cs:3406-3487`）。⇒ `replace` 的「取大」在跨 timing 时**退化成本质相加** —— 这是原版行为。

### 2·4 `removeTrait(4)` 的对称性（**必须一起做，否则两张表会打架**）

`case 4`（`.c:680-698`；`CardScript.cs` 对应段）**顺序固定**：

1. `T == stealth(70)` ∧ `HasCurrentTrait(70)` ⇒ **`BroadcastUnitLoseStealth(this)`**
2. **`RemoveActiveEffects(removeTrait, T)`** —— 先**去重**（同名 removeTrait 只留一条）
3. `activeEffects.Add(E)` —— **这条 removeTrait 记录本身要留下**（`HasRemovedTrait` 靠它，见 §2·6）
4. **`RemoveBuffedTrait(T)`** —— 从 `activeEffects` **倒序**摘掉**所有** `buffType ∈ {3,90}` ∧ **`!extrinsic`** ∧ `trait == T` 的记录，每条都走 `RemoveEffect`（`CardScript.cs:19215-19272`）
5. **`RemoveTrait(T)`** —— 从 `CurrentTraits` / `CurrentShowingTraits` 里摘掉 `CardTraitData`，`RemoveStatusAnim`；`blind` ⇒ `UpdateFigures()`；`fast`/`flank` ⇒ `UpdateSummonSicknessAnims()`；末尾 `CleanStatusAnims` + `UpdateTraitIcons`（`CardScript.cs:19361-19409`）
6. `T == blind(975)` ⇒ `blindedAtStartOfTurn(+0x56) = false`
7. 收尾（公共尾）`UpdateFigures()` → `CheckPossibleCombinations()` → `return true`（`.c` 尾 `LAB_1805e6a2c`）

**与「加」的三处不对称（照抄，别"修正"）**：
- **摘**会先 `RemoveActiveEffects(4, T)` 去重；**加**是 `RemoveActiveEffects(4, T)`（**同样是按 T 过滤**）。
- `RemoveEffect`（摘单条效果）里还有一条「**这个 trait 既不是卡默认带的、身上也没有别的 addTrait 记录 ⇒ `RemoveTraitSilently(T)`**」—— 这叫「**最后一个来源消失才摘词条**」（`CardScript.cs:19082-…`，该方法被 Ghidra 的 `Jump target not found` 打了 4 处，**语义靠 IL 版读**）。
- **`RemoveTrait` 自己不调 `CheckPossibleCombinations`**，靠外层公共尾调。

### 2·5 我们这边的落点 / 缺口（**改动清单 —— 这里是「要改什么」，不是已改**）

| # | 现状 | 要改成 | 依据 |
|---|---|---|---|
| **D1** | `UnitState.AddKeyword` 里 **`_removedTraits.Clear()` 全清**（`UnitState.cs:1093-1101`） | 🔴 **只清 `_removedTraits.Remove(keyword)`**（键 = 被加的那条 trait）。原版的 `RemoveActiveEffects` **带 trait 过滤**（IL 两份 + `CardScript.cs:17651` 都是 `(4, newEffect.trait)`）；现在这个「全清」是照 Ghidra 少印第 3 个实参读出来的**假口径** | 见 §三裁定 |
| **D2** | `give <trait> N` 一律 `_keywords[k] += N`（`EffectResolver.cs:11482`） | ① `markOfChaos` 走 `count` ⇒ **`+=1`**（`EffectResolver.cs:11933` 的黑暗契约那条已经是对的，但**通用的 give 路**要给 `_stackable == count` 留口子）② 其余 `add` ⇒ `+= N`（现状即可） | §2·3 |
| **D3** | 「加 trait」的逐条特例**只做了两条**（`armour` 连带 `Armor`、`blind`/`pindown` 攻击型；`fast`/`flank` 重算） | 补 §2·2 的 **6 条**：`poisoned×resistant` 双向、`survivor→摘 survivorSpent`、`tide/companion` 计数器快照、`markerlight`/`huntMark` 广播（后者已有 `EmitStatusApplied` + `GetsHuntMark`，**要核是不是同一跳、会不会双响**） | §2·2 |
| **D4** | `poisoned` × `resistant`：原版**两个方向都拦**（加毒时若有抗性 ⇒ 不加毒；加抗性时若有毒 ⇒ 摘毒）。我们只有「关键词在哪」 | 两处都补（`UnitState.AddKeyword` 或 `EffectResolver` 二选一，**判据只留一处**） | §2·1 S3 / §2·2 |
| **D5** | `removeTrait`：`RemoveKeyword` + `MarkTraitRemoved`（`EffectResolver.cs:11505-11520`） | 补 ① `stealth` ⇒ `BroadcastUnitLoseStealth` ② **`RemoveBuffedTrait`** 语义（摘掉该 trait 的**所有非 extrinsic 授予**）③ `blind` ⇒ `BlindedAtStartOfTurn = false`（**已有**，`UnitState.cs:1188` 段） | §2·4 |
| **D6** | 没有 `CheckPossibleCombinations`（`TraitCombination` 全仓 0 命中） | **不实现**，但要在账上写清「原版在**每次 AddEffect 成功**后都调它，读 `PlayerDataManager._traitCombinationsList`（`TraitCombinationsList` SO）；**本地解包里没有该 SO 实例**（`find d:/2/新解包资源 -iname '*TraitCombination*'` = 0，只有 `MonoScript_3059.json` 这个**脚本定义**）⇒ 判据空 / 无资产」 | 铁律 11 例外① |
| **D7** | `AddKeyword` 已是 `+=`（= `add`），但**没有「同 timing / 同来源」这一维** | 我们的 `_keywords` 是**单标量**，原版是「每条 CardEffect 各带时机」；**值相加的净效果相同**，但**逐条到期**要靠 `TempBuff`。⇒ 只需要确认：**限时授予到期时按各条自己的值减**（`TempBuff` 已这么做），**不要**到期时 `RemoveAll` | §2·3 末 |
| **D8** | `CardTraitData._traitValue`（卡**默认**带的数值，如 `Blast 3`） | 我们的默认值来自 `CardDef.Keywords`（`CardDef.cs:6021`）← `KeywordTable.Parse`（`:8032` `kws[name] = FirstNumber(HeadOf(item))`，**没印数字兜底 1**）。✅ **本轮逐张对过账**：`cards_engine.json` 的 **1,128 张** 里「关键词带数字」的条目 vs `cards_truth.json` 的 `cardTraits._traitValue` ⇒ **0 处不同**；唯一的「裸数字 trait」= `TAU13 Pathfinder` 的裸 `Companion`（SO 真值 **2**），**已由 `CollectCompanionKeyword` 从 `desc` 的 `Companion 2:` 补回 2**（`CardDef.cs:4156-…`，含成品卡图判据）⇒ **现状正确，不用改**。⚠️ `cardTraits` 列**不喂引擎**（该列自己的 doc 写明）⇒ **不要**改去读 `cardTraits` | `Data/CardDatabase.cs:154` / `cards_engine.json` + `cards_truth.json` 实测 |

### 2·6 验收断言建议（给执行代理；**期望值必须标判据行号**）

1. **`add` 累加**：同一 trait 连加两次（同 timing、非 extrinsic）⇒ 值 = 两次之和、`activeEffects` **只有一条**记录（`_stackable = add`）。
2. **`count` 记次**：`markOfChaos` 加两次 ⇒ 值 = 2（与 `traitValue` 无关；即使传 `traitValue=5` 也是 2）。
3. **`AddTraitSilently` 去重**：同一 trait 加两次、**timing 不同** ⇒ 词条表里**一条**（`CurrentTraits` 只有一份），但**值相加**。
4. **灭自证**（`CLAUDE.md` §三）：给一条**已存在的 trait** 再加一次，断言「记录条数没增加」**同时**断言「值变了」——
   只断前者会被「整条丢弃」的实现混过去，只断后者会被「重复 Add」混过去。
5. **`removeTrait` 对称**：加 `flank` → 摘 `flank` ⇒ `CanAct/CanAttack` 回到「不带 flank」的那一档（`RecomputeActAndAttack` 被调过）；
   且 `RemoveBuffedTrait` 摘干净后 `HasRemovedTrait` 变真、再 `addTrait` **同一条** ⇒ 变假（D1 修好之后才会对）。
6. **`SO == null` 中止**：对本 build 不可达（§三·4 已核 75/77 有 SO、池内用的全有 SO）⇒ **不写断言**，只留注释。
7. **`240` 快照**：同一条 240 效果在两份不同棋盘状态下结算，记录里的 `*Change` 应当**不同**（快照）；
   而 `CurrentMeleeAttack` 的读数**不随后续棋盘变化而变**（除非重挂）。

---

## 三、判据冲突裁定（都在本轮现核过）

### 3·1 🔴 `RemoveActiveEffects(4)` 到底带不带 trait —— **带**（Ghidra 少印实参）

| 源 | 写法 |
|---|---|
| `out_cpp2il_dev_ilspy/CardScript.cs:17651` | `RemoveActiveEffects(BuffType.removeTrait, newEffect.trait);` |
| `out_il2csharp/Assembly-CSharp/CardScript.cs:10855`（**独立第二份 IL 反编译**） | `this.RemoveActiveEffects(BuffType.removeTrait, newEffect.trait);` |
| `decomp_full/CardScript__AddEffect.c:474` | `CardScript__RemoveActiveEffects(param_1,4);` ← **只有 2 个实参** |

**裁定：以 IL 为准（带 trait）。**理由三条：
① **IL 的实参个数不会被反编译器"发明"**（`call` 指令的压栈个数是确定的）；两份独立 IL 反编译**一致**。
② **Ghidra 在这份文件里的实参个数本身就不自洽**：同一个函数 `RemoveActiveEffects`（签名 `(this, int buffType, int trait)`，`CardScript__RemoveActiveEffects.c:1`）在 `AddEffect.c` 里被印成 **2 / 3 / 4** 个实参三种形态（`:213` 4 个 · `:235` 4 个 · `:474` 2 个 · `:686` 3 个 · `:795` 3 个）⇒ **它的实参列表不能当证据**。
③ 函数体自证：`param_3 != 0` 时才按 `+0x3c`（trait）过滤（`CardScript__RemoveActiveEffects.c:34-40`）—— 说明「带 trait」这条路是**设计内**的。

⇒ **D1 就是按这条改。**

### 3·2 `_stackable` 不是占位值

见 `CardTrait_stackable_真值表_第2轮.md` §0（73/73 `bytesRead == byte_size`）。`判据/战斗逻辑驱动_AddEffect分派器.md:171/174` 已就地订正。

### 3·3 Ghidra 的 `case 3` 里 `RemoveActiveEffects` 之后紧跟的 `bVar2 = false;` 与「跳过 `UpdateFigures`」

`bVar2` 只在 `pindown` 那条支里被置成「跳过」（IL 版对应 `obj19 = 0`，`CardScript.cs:18213`）。⇒ **只有 `pindown` 不刷 `UpdateFigures`**，其余都刷。照抄。

### 3·4 「`SO == null` ⇒ 整条中止」在本 build 里的可达性

- `CardTraitCollection.GetTrait` 对 **`definedTraits` 里没有的 key** 返回 `null`（`CardTraitCollection.cs:334`）。
- 本轮现读：`TraitsCollection` 的 **77 个 pair** 覆盖了 `DefinedTrait` 的 77 个值，其中 **75 个有 SO**、**2 个没有**（`berzerk(360)` / `dodge(240)`）。
- **池内 `addTrait` 用到的 28 个 trait 全部在「有 SO」那一档**（含 `dontReturnFerocity(1270)`→`NotFerocity`、`oathInAllTurns(1276)` / `oathDouble(1277)` / `oathTripleActivation(1278)`→共用 pathID 498）。
⇒ **这条分支今天 0 可观察量**，但**必须写着**（否则将来数据一变就静默走错）。

---

## 四、没查清 / 判据空

1. **`CheckPossibleCombinations` 的数据源**：`PlayerDataManager._traitCombinationsList` 是私有字段，**ILSpy 会把私有 setter 反编译成不带字段名的写**（`CLAUDE.md` §三 那条 2026-10-25 的坑）⇒ **「它有没有被赋值」没证到**；且**资产实例本地不存在**（只有 MonoScript 定义）。
   ⇒ 记「判据空」；**不实现、不写断言**。回来的条件 = 拿到远端 `TraitCombinationsList` 或原版实况里看到「凑齐两个 trait 触发附魔」。
2. **`multiplier` 的来源语义**：只证到「`PlayAbility` 里算出来的 `int`」；它对 `NewBuffStat` 哪些分支生效、怎么算出来的，**本轮没逐支读**（`NewBuffStat` 全程 1,000+ 行、跳转极碎）。E1 若只需「快照 vs 活读」，不受影响；要接 `ValueOptions` 全表时才需要。
3. **`RandomizedOnResolution(90)`**：`NewBuffStat` 里会**现掷**（`AbilityLogic.cs:2555` 一带）⇒ 与「快照」不矛盾（**掷出来的数写进快照**）。**随机源**是不是 `manager.GetRandomInt`（= 可复现的 `System.Random`）**没逐行核**；⚠️ 若它是 `UnityEngine.Random`，我们的可复现性红线会被打破 —— **这一条要 `E1` 顺手看一眼**。
4. **`IsSameEffect` 比字段表**：是**逐字段现读**得出的（`CardEffect.cs` 那一大串 `&&`），但**没做「少比一个字段会怎样」的推演**；`trait`/`traitValue` 不在表里已确认。
5. **`tideCounter` / `companionCounter` 的消费语义**：原版在 `CardSetup`（`CardScript.cs:6581-6590`）与 `AddEffect` 的 addTrait（`18152-18161`）两处快照。我们这边有 `CardInstance.CompanionCounter`（用途 = 伴生链剩余环数），**是否是同一个东西没核**；`tideCounter` 我们这边 **0 命中**。

---

## 五、顺手发现（不在本任务范围内，但下个代理会踩）

1. 🔴 **`UnitState.AddKeyword` 的 `_removedTraits.Clear()` 是我们自己写进去的偏差**（`UnitState.cs:1093-1101`，注释声称「照抄原版、别改成只清同名」）—— 按 §三·1 的裁定**原版是只清同名**。这条不只是注释错，是**行为错**（`HasRemovedTrait` 会多清）。
   影响面：今天 `HasRemovedTrait` 的唯一消费者 = `ConditionEval` 的 `280 siege`（reload 那条），**可观察量可能为 0** —— 但**口径必须先改对**，否则 K2 一做，两个错会互相掩护。
2. **`战斗逻辑驱动_AddEffect分派器.md:172` 的行号是旧的**：`UnitState.AddKeyword`（写 `:954`，现读 **1090**）· `Has`（`:806` → **942**）· `Keywords`（`:816` → **952**）。本轮**没有**去改那一格（避免与别的写手撞车），**记在这里**。
3. **`OathAdditionalEffect` 是三个 trait 共用的一个 SO**（pathID 498，服务 1276/1277/1278）⇒ 任何「按 SO 建索引」的代码都会**一个 SO 覆盖三个 trait**，要用 `DefinedTrait` 当 key。
4. **`CardTrait词条权威表.md` 里 `NotFerocity` 被列为「我们完全没有 / 没有 SO」**：SO **有**（pathID 496 = `dontReturnFerocity`）；「我们代码里没有」那是另一件事（老实说：`NotFerocity` 的**机制**我们做了什么，本轮没核）。
5. **`pindown` 加进来会强制改成远程攻击型**（`.c:625`），而且**跳过 `UpdateFigures`** —— 这与「`pindown` 只禁近战」的既有注释（`UnitState.SyncKeywordState` 那段）**不冲突**，但**两个行为（改攻击型 + 禁近战）是两件独立的事**，⛔ 别把 `SyncKeywordState` 里那句当成全部。
6. **`devilfish` 在同一条 ability 里挂了**两条** `flank`**（trigger 20）⇒ 第二条被 `ContainsExtrinsicEffectFrom` 幂等闸丢掉。**原版如此**，⛔ 别改成「挂两层」。
