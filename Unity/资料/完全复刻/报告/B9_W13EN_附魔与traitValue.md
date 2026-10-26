# B9 · `W13EN` —— P-03 附魔层 7 条 ＋ P-02 的 `addTrait/traitValue` 半边

> 执行代理 **`W13EN`**（有写权限）· 2026-10-26 · 第八十四会话 · 完全复刻批次 9
> 白名单（独占）= `RuleEngine/Core/Enchantment.cs` · `RuleEngine/Core/UnitState.cs`
> ⛔ 没跑 Unity · 没动 git · 没改两张正本 · 没改主账表（只写 `账/_parts/B9_W13EN.tsv`）· 没碰白名单外文件。
> 判据 = **全量反编译方法体（第一权威）** + `判据/B9_剩余C_派活包.md` §2「P-03」+ 施工单逐条；账行号**一律按「原版类 + 方法名」在 `账/战斗逻辑驱动.tsv` 现定位**（快照行号已漂）。

## ① 结论

| # | 账 | 件 | 结论 | 读数 / 可观察量 |
|---|---|---|---|---|
| A① | **1047** | `EntityScript.HasSecretOrder` 的**那道闸** | ✅ **修完（C → A）** | `FindSecretOrder` 开头加 `CountSecretOrders(carrier) <= 0 ⇒ null`（`Enchantment.cs:573`）；改前「ActiveEffects 里有同卡附魔型效果、ActiveEnchantments 为空」会返回该 effect ⇒ 改后 **null**。池内零可达（密令揭示链本仓未建），**0 行为变化** |
| A② | **1044** | `EntityScript.HasEnchantmentWhileInPlay` | 🟡 **方法按判据逐句实现；数据列未接 ⇒ C → B（等 P-07）** | 新增 `HasEnchantmentWhileInPlay`（`Enchantment.cs:753`）判的是**附魔卡自己的** `whileInPlayAnim`（不是 `HasAny`）；三态数据位 `ActiveEnchantment.WhileInPlayAnim`（`:150`）＋ `CountUnknownWhileInPlayAnim`（`:762`）。数据**在本地**：`cards_raw.json` 的 `full_raw.whileInPlayAnim`，1,351 张里 **true = 41**，其中**附魔卡 3 张** |
| A③ | **481 / 532** | 摘附魔的**入队链** | 🟡 **481 入队口已建（调用点未切 ⇒ 仍 C）· 532 ①④ 等价链已挂进出队体；②③ 不在本层** | 新增 `Enchantments.EnqueueRemove(ctx, carrier, effect, lane = Instant)`（`Enchantment.cs:908`）→ `RuleCore.EnqueueAbility`，出队体 = `Remove`（`:287`）。532 ②（撤四条队列道 anim cue）③（buffType 15/16 帧特效）**都在 `CardPresentation/Battle/`** ⇒ 改动请求见 §四 |
| A④ | **687** | `CardScript.AddEnchantment` 第五步 | ✅ **修完（C → A）** | `Attach` 末尾调 `ActivateTraitsAfterEnchant`（`:938`）→ `RuleCore.ActivateTraitsOnSummonOrEnchantment`；原版的守卫 `cardState ∈ {2,3,15,17}` 用「载体在棋盘上」等价。`play check` 实测 **0 差异** |
| A⑤ | **763** | `CardScript.GetQuestTimes` | ✅ **修完（C → A）** | 新增 `Enchantments.GetQuestTimes`（`:783`）＝取第一条 `QuestTimes > 0`，全无 ⇒ 0。🆕 **原版自己 0 调用点**（ILSpy 全方法体 + IL2CSharp 全树只有定义）⇒ 死 API ⇒ 我们不接线才忠实 |
| A⑥ | **770** | `CardScript.HasCancelAttackEffect` | 🟡 **完整形态已实现；唯一调用点仍用旧重载 ⇒ 仍 C** | 新增重载 `FindCancelAttack(ctx, carrier, actingCard, targetCard, ignoreRandom)`（`:821`），五道闸 = trigger 号 / `IsRightContext` / `ActingLevelCriteriaAllows` / `AbilityLevelCriteriaAllows`；旧单参重载（`:603`）保留。切换点是 `RuleCore.cs:7994`（非本写手） |
| B | **64** | `AbilityLogic.AdjustCardEffect` 的 `addTrait/traitValue` 半边 | 🟡 **`UnitState` 侧记录形状已备好；`EffectResolver` 侧未切 ⇒ 仍 C** | `UnitState.ApplyTraitEffect`（`UnitState.cs:2983`）+ `TraitEffectRecord`（`:2901`）+ `TraitEffectValue`（`:2945`）+ `FindTraitEffect`（`:3044`）+ `RemoveTraitEffect`（`:3070`）：按 `_stackable` 合并（add 累加 / replace 取 Max / count 每次 +1 且新建强置 1）、`extrinsic` 不合并。🔴 **就地订正**：`AdjustCardEffect` **不乘 `traitValue`**（详见 §二·7） |

**必报三项**

* **typecheck**：`TMPDIR=/tmp/wf_W13EN bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（本批最后一次 `.cs` 改动之后跑的）。
* **`ruleprobe.sh check`**：**rc=1，1 条失败**，且**不是本批** ——
  * 失败条 = `PinnedParseMd5` 哨兵：当次现算 **`c9a647982fad19bb65896011c281c93e`** ≠ 钉住值；
  * 「解析差异 **1 行**」= `BL47|Disgustingly Resilient`；
  * 归因 = **`E10TB` 的 `EffectText.cs`**（`+20/−3`：新增 `ReAdditionalForEachReversed`，让 `and an additional Armour 1` 也走加法型）—— 本批两个文件**不在解析路径上**（`Enchantment` / `UnitState` 都不被 `EffectText` 引用，`grep` 只有注释里的 8 处提及）。
  * ⛔ 本代理**没有**动 `PinnedParseMd5`（按红线：那是调度台确认时手改的一行）。
* **`ruleprobe.sh play check`**：**rc=1 · 当次现算 `1b40f42ab010e623e908fcd4a236205f`** · 基线文件 `af8e8178…`（未动）。逐条差异与归因：

  | 档 | 卡 | 归因 | 证据 |
  |---|---|---|---|
  | ①-a 主档 | **BL47** | **`E10TB`**（`EffectText` 加法型） | 多出 `✎ 改动 … {armour1}` ⇒ 护甲 +1，正是那张卡的修复 |
  | ①-b 电池 | **BL18 / UM18** | **`E7P06`**（账 669：`Auras.RefreshInPlayNumericAndKeywords` 已打开） | `UM18 net 32 → 36`（+4），与该报告里「各 +4」逐字吻合 |
  | ①-b 电池 | **SW25** | **`E10TB`**（P-05① 残骸那本账） | 该报告的 §三·2 已用隔离实验定位为「本批 B① 的副作用」 |
  | —— | **本批改动** | **0 差异** | 两条理由：① `FindSecretOrder` / `GetQuestTimes` / `HasEnchantmentWhileInPlay` 在**全仓 0 调用点**（后两个在原版也是 0 调用点）；② `Attach` 唯一的生产入口是 `AttachPray`，而 23 张 pray 卡里**只有 `SOR69 Zephyrim Superior` 带 `flank`**（`cards_truth` 现读）⇒ `ActivateTraitsAfterEnchant` 的四个 trait 门槛只对它可能生效，而它不在上面三张变化卡里。工作树里 `play check` 的「跑 N 遍」是逐张独立夹具 ⇒ 不存在跨卡的连带 |

## ② 逐条证据

### ②·1 `1047 HasSecretOrder` —— 闸看 `activeEnchantments`、体查 `activeEffects`

* 判据（`out_cpp2il_dev_ilspy/EntityScript.cs:1977-2035` 逐句）：`int num = HasSecretOrders(); if (num > 0) { … 遍历 activeEffects … }` ⇒ 两个**不同的 list**。
* 我们的病：`FindSecretOrder` 只扫 `ActiveEffects`，没那道闸 ⇒ 「身上没有 SecretOrder 附魔、但 ActiveEffects 里有一条同卡附魔型效果（`buffType ∈ {12,15,16}`）」时**我们会返回它、原版返回 null**。
* 修法（只一行，形状照原版）：
  ```csharp
  if (CountSecretOrders(carrier) <= 0) return null;   // 闸看 activeEnchantments（⛔ 别改成数 activeEffects）
  ```
  闸用的 `CountSecretOrders`（`Enchantment.cs:518`）数的正是 `ActiveEnchantments` ＋ `SpellType == 0x3C` ⇒ 与原版 `HasSecretOrders()` 同口径。
* 可达性：原版唯一调用点 = `BattleManager._ResolveRevealOrders`（`out_cpp2il_dev_ilspy/BattleManager.cs:10583`）＝**密令揭示链，本仓未建** ⇒ 今天零可观察；但补它是「缺闸」这条判 C 的唯一原因。

### ②·2 `1044 HasEnchantmentWhileInPlay` —— 判的是**附魔卡自己的表现开关**

* 判据（`out_cpp2il_dev_ilspy/EntityScript.cs:1843-1885` 逐句）：`foreach (ActiveEnchantment e in activeEnchantments(+0x110)) { var c = e.enchantmentCard; if (c != null && c.whileInPlayAnim) return true; } return false;`；`e == null` / `enchantmentCard == null` 那两格原版是 **NRE 桩**（`return (byte)(int)new NullReferenceException() != 0;`）⇒ 按既有口径**不复制崩溃**，跳过并把件数交给 `CountUnknownWhileInPlayAnim`。
* `whileInPlayAnim` 的定义处：`dump.cs:23338  public bool whileInPlayAnim; // 0x288`（`RawCardScript`）。
* **数据普查（本批现做，`cards_raw.json` 1,351 张全扫）**：字段在 **`full_raw.whileInPlayAnim`**（解析视图那一层没有）⇒ `true` = **41 张**；其中**附魔卡 3 张**：
  `eab54e4c131034e4dacf8ce72807ed07` = `T16 Sautekh-Undying Legions_enchantment` ·
  `a5960475756bec144a1d6d8a9d141fc9` = `T18 Leviathan-Hive Fleet Leviathan_enchantment` ·
  `7c06b8117fe86e74dbc454139da8653b` = `Z-SW-Everstorm_enchantment`。
  ⇒ **P-07 的落列判据就这几行**（`cards_truth` 目前没有这一列；`grep -rn whileInPlayAnim Assets` = 0）。
* 我们的落法：`ActiveEnchantment.WhileInPlayAnim` 三态（`1/0/-1`，`:150`，被 `Attach(..., whileInPlayAnim:)` 填）＋ `HasEnchantmentWhileInPlay`（`:753`）。P-07 落列后把 `Attach` 的缺省改成读 `CardDef.WhileInPlayAnim`，**消费端一行不动**。
* 🆕 **可达性新证**：`out_cpp2il_dev_ilspy` / `out_il2csharp` 全树**只有定义、0 调用点** ⇒ 原版自己也不调它（死 API）⇒ 我们**不接线才忠实**（这条也回答了「要不要找调用点」）。

### ②·3 `481 RemoveEnchantment` / `532 ResolveRemoveEffect` —— 入队链

* **481 原版全文 83 行亲读**（`BattleManager__RemoveEnchantment.c:26-79`）：它**不摘任何东西** —— 组装 `BattleAction{actionType = 0x10 (removeEffect)}`（写进 carrier + effect 两个对象引用，各带一次写屏障），再 `AddAutoActionToQueue(action, priority)`。⇒ 缺的只是**入队这一层**。
* 我方的等价口：`RuleCore.EnqueueAbility(ctx, lane, label, body)`（`AddAutoActionToQueue` 的对照物；`ActionPriority` 数值 0/1/2/3 与 `CardAbility.priority` 同一套字面量，`CardAbility__ctor.c:5` 写 `+0x18 = 1` = `Instant`）。
* 新增：`Enchantments.EnqueueRemove(ctx, carrier, effect, lane = ActionPriority.Instant)`（`Enchantment.cs:908`），出队体 = `Remove(ctx, carrier, effect)`（`:287`）；入队失败**出声**（⛔ 不静默）。
* **为什么调用点没切**（这是判据，不是省事）：今天生产上唯一的摘除口是 `RemovePray`（三个落点 `RuleCore.cs:1629` / `:3079` / `:8813`），而 `:1629` 那条的语义写死了「**先摘再给（绝不重复发）**」—— 改成入队会把「立刻发一次」变成「晚一拍发」，**先要定那条时序**（原版那条链的时序判据在 `chosenAbility 340 removeThisEnchantment` → `AddRemoveEnchantment` → 队列，本轮没读到能钉死时序的那一段）⇒ 写进 §四的改动请求，由 `RuleCore` 的写手一起做。
* **532 四段**（`out_cpp2il_dev_ilspy/BattleManager.cs:26290-26347`）：① `RemoveEffect`（有：`RevertGrantsFrom` + `Enchantments.Remove`）· ② `CancelAnimFromUnitFromQueue` **连走四条道**（`actionQueue / instantQueue / interruptQueue / delayedInstantQueue`）· ③ `RemoveDestroyableAnims`（`buffType ∈ {15,16}` 的 `TraitFrameEffectsPlay`）· ④ `FinishResolvingAction`（有）。②③ **都在表现层**（anim cue 与 trait 帧特效都在 `CardPresentation/Battle/`）⇒ 不在 `RuleEngine` 能碰的范围，改动请求见 §四·3。

### ②·4 `687 AddEnchantment` 第五步 —— 不是「把附魔卡的 trait 并进载体」

* 判据（`CardScript__AddEnchantment.c:91`）＝ `CardScript__ActivateTraitsOnSummonOrEnchantment(carrier, /*onSummon*/ 0)`。
* 那一跳逐句（`CardScript__ActivateTraitsOnSummonOrEnchantment.c` 全文）：
  * 守卫 `cardState(+0x228) ∈ {2 在场, 3, 0xf(15), 0x11(17)}`（`.c:43-44`）；
  * 遍历 **载体自己的 `CurrentTraits(+0x128)`**，每条 `BattleCardUI.DisplayStatusAnim(...)`（表现层）；
  * `0xe6 dropPod` 那一支**只在 `onSummon != 0`** 时重置 drop pod 血/攻（我们这一跳 `onSummon:false` ⇒ **不进**）；
  * `0x28 fast` / `0x1cc flank` ⇒ `canAct(+0x230) = true`；
  * `0x4f1 ferocity` / `0x4fb oath` ⇒ `canAct = true` 且 `canAttack(+0x23c) = false`。
* ⇒ **它不合并任何 trait**。旧施工单写的「把附魔卡的 addTrait 并进载体」是读错了这一跳（真并进去反而**与原版不符**）。我方的等价口早就有一个唯一写点：`RuleCore.ActivateTraitsOnSummonOrEnchantment(UnitState)`（`RuleCore.cs:3576`，逐行判据在那边）⇒ 本批只在 `Attach` 末尾接上（`ActivateTraitsAfterEnchant`，`Enchantment.cs:938`），并补原版那条 cardState 守卫（按「载体在棋盘上」等价）。
* ⚠️ 原版同一跳的 `DisplayStatusAnim`（每条 trait 一次）**我们没有对应层** ⇒ 如实留缺口（P-11 / 特效线）。

### ②·5 `763 GetQuestTimes` —— 取值口

* 判据（`CardScript__GetQuestTimes.c:1-45` · `out_cpp2il_dev_ilspy/CardScript.cs:10604-10654`）：遍历 `+0x110`，返回**第一条** `enchantmentCard.questTimes(+0x7c) > 0` 的值；全无 ⇒ 0；`enchantmentCard == null` 那格是 NRE 桩 ⇒ 我们跳过。
* 与 `FilterByQuestCounter`（`ReachedQuestCounter` 的消费点：逐条累计 + 比较）**不是同一件事** —— 两个口现在并存，注释互相指名。
* 🆕 **可达性**：ILSpy / IL2CSharp 全树**只有定义、0 调用点** ⇒ 原版自己也 0 调用点 ⇒ 我们不接线是忠实的（`spellType == 120` 池内 0 张，接上也零变化）。

### ②·6 `770 HasCancelAttackEffect` —— 五道闸的等价链

* 判据（`CardScript__HasCancelAttackEffect.c:1-66` 逐句）：① 取 `+0x110`；② 逐件 `enchantmentCard(+0x10)`；③ `RawCardScript.HasAbilityType(0x118)`；④ 遍历该卡 `cardAbilities(+0x290)` 逐条 `CardAbility__CanTriggerAbility(ability, 0x118, manager, **载体**, actingCard, targetCard, 0)`；⑤ 任一真 ⇒ 返回该件；⑥ 全无 ⇒ 0。
* 五道闸（`CardAbility__CanTriggerAbility.c:1-143` 逐句）：`ability.trigger == 280` · `IsRightContext(ability, thisCard)` · `CheckIfMeetsCriteria(thisCard, actingCard, ability+0x28)` · `CheckIfMeetsCriteria(thisCard, targetCard, ability+0x30)` · `BoardAnalysis.MeetsCriteria(manager, ability+0x38, thisCard, actingCard, targetCard, ignoreRandom)`。
* 我方映射（新增重载 `FindCancelAttack(ctx, carrier, actingCard, targetCard, ignoreRandom)`，`Enchantment.cs:821`）：
  ③ → `HasAboutToAttackAbility`（`:880`）= v17 真值列 `CardDef.AbilityTriggerPresence("AboutToAttack")`（1/0/−1），旧表才退回「有没有 AboutToAttack 正文」；
  区域闸 → `RuleCore.IsRightContext`（`abilityContext` 取 `RuleCore.AbilityContextOfAbilityV14`）；
  第一道 → `RuleCore.ActingLevelCriteriaAllows`；第二/三道 → `RuleCore.AbilityLevelCriteriaAllows`。
* 残余（⛔ 不当「做全了」）：① 原版**逐条 ability**、任一真即真，我们那三个入口按 `(卡, trigger)` 定位**唯一**一条 ⇒ 同卡同 trigger 多条时走既有口径「定位不唯一 ⇒ 不触发 + 出声」；② `ignoreRandom` 无等价位（`AbilityLevelCriteriaAllows` 不收它）⇒ 传 true 时**出声**并按 false 判；③ 附魔卡一般不在 `cards_engine.json` ⇒ 三道闸多半「判不了 ⇒ 放行」（原版会按 ability 数据判）。池内 0 张卡挂「带 280 能力」的附魔 ⇒ 今天零可观察。

### ②·7 活 B —— `addTrait` 的 `traitValue` 账（**一处机制订正**）

* 🔴 **`AdjustCardEffect` 不乘 `traitValue`**（`AbilityLogic__AdjustCardEffect.c:1-59` 全文亲读）：它只对**五对数值字段**各调一次 `NewBuffStat`
  （`+0x4c/+0x48` 近战 · `+0x54/+0x50` 远程 · `+0x64/+0x60` 血 · `+0x5c/+0x58` 护甲 · `+0x6c/+0x68` 费），**`+0x40 traitValue` 一次都没出现**；
  `NewBuffStat` 把 multiplier 乘进值（`AbilityLogic__NewBuffStat.c`: `uVar10 = param_6; if (param_4 != 0) uVar10 = param_4 * param_6;`）。
  ⇒ 「`traitValue = change × N`」**不是它算的**，而是 `CardScript__AddEffect.c` `case 3` 的**记录合并**自然得到的：
  * 匹配键 = 同 `trait(+0x3c)` ∧ 同 `markOfChaosType(+0x78)` ∧ 同 `daemonHostType(+0x7c)` ∧ `CardEffect.SameTimingAsEffect` ∧ **两边 `extrinsic(+0x30)` 都为 0**，**倒序**扫（`:492-568`）；
  * `_stackable == add(10)` ⇒ `旧.traitValue += 新.traitValue`（`:544`）
  * `_stackable == replace(0)` ⇒ `旧.traitValue = Max(旧, 新)`（`:531-535`）
  * `_stackable == count(20)` ⇒ `旧.traitValue += 1`（`:554`），**新建那条先被强置 1**（`:496`）
  * `_stackable` 真值（`判据/CardTrait_stackable_真值表_第2轮.md`）= 74 add · **1 count（`markOfChaos 330`）** · **0 replace**。
  值 = `GetCurrentTraitValueWithModifiers`（`EntityScript__GetCurrentTraitValueWithModifiers.c`）= `Σ(buffType==3 ∧ trait 命中 的 traitValue)` ＋ 卡面那份 ⇒ **记录列表才是值的来源**，`CurrentTraits` 只管「在不在」（`CardScript__AddTraitSilently.c` 只建默认值 + `Linq.Any` 去重）。
* ⇒ **施工结论不变、理由换掉**：要补的是「**一条记录**」（可被 `RemoveEffect` 按记录摘、能回答「这是哪条、谁给的」），而不是「把值乘上去」。我们的「N 次 `AddKeyword`」净读数与原版**等价**（add 档两边都是累加；count 档 `IsCountStackableTrait` 已等价），差的只是**实例**。
* `UnitState` 侧新增（全部追加在类尾）：
  * `TraitEffectRecord`（`:2901`）：`Trait / BuffType(3·90) / TraitValue(+0x40) / StackType / Extrinsic(+0x30) / MarkOfChaosType(+0x78) / DaemonHostType(+0x7c) / TimingBits / Source`；
  * `ApplyTraitEffect(trait, value, …)`（`:2983`）：按上表合并 + 把**增量**落到 `AddKeyword`（副作用一条不少；毒×抗性那一档 `AddKeyword` 拒收 ⇒ **不建记录**，与原版 `.c:469-471` 整支跳过一致）；`value <= 0` 直接拒绝（`removeTrait` 不在那 8 个 buffType 里，原版不乘也不记）；
  * `TraitEffectValue`（`:2945`）· `FindTraitEffect`（`:3044`，倒序 + 三个合并键）· `RemoveTraitEffect`（`:3070`，= `RemoveEffect` 的 `case 3`）· `TraitStackTypeOf`（`:2963`，只有 `markOfChaos` 是 count，**判据单一口**：调 `IsCountStackableTrait`）；
  * `ClearAllKeywords` 里同步清这本账（`ResetToValuesInHand` 那一路原版会把 `activeEffects +0x108` 整个清掉）。
  * 🔶 标记：`TimingBits`（把五个 `until*` 位 + isPlayer 压成位掩码）与 `Source`（原版没有的列）**已打 `🔶 自己设计`**，三样齐全（是什么 / 为什么拿不到 / 谁定的+日期）。

## ③ 读数 · 回归 · 指纹逐条解释

| 读数 | 值 | 解释 |
|---|---|---|
| `typecheck` | 运行时 **0** / 编辑器 **0** | 本批两个文件的最后一次改动之后跑的 |
| `ruleprobe check` | rc=1 · 解析差异 **1 行** · 基线 `out_baseline.txt` | 唯一差异 `BL47` = **`E10TB`**（`EffectText.cs` +20/−3，加法型正则）；`PinnedParseMd5` 未动（钉住的那行由调度台确认时手改） |
| 我在解析路径上的影响 | **0** | `EffectText.cs` 里 `Enchantment`/`UnitState` 只有 8 处**注释**提及，无引用；本批没有改卡表/解析器 |
| `ruleprobe play check` | 主档 1 张（BL47）· 电池 3 张（BL18/SW25/UM18） | 全部**非本批**：BL47 = `E10TB` A；SW25 = `E10TB` B①（其报告 §三·2 隔离实验）；BL18/UM18 = `E7P06` 账 669（各 +4，与该报告逐字吻合） |
| 本批的可观察面 | **0 差异** | 三个新谓词全仓 0 调用点（其中两个**原版也是 0 调用点**）；`Attach` 唯一生产入口是 `AttachPray`，23 张 pray 卡里只有 `SOR69` 带 `flank`（`cards_truth` 现读） |
| 账机检（只对我这 8 条补丁跑） | `A=3 通过=3 漂移=0 失败=0` | `python 工具/check_ledger_points.py 资料/完全复刻/账/_parts/B9_W13EN.tsv` |
| 补丁冲突 | ✅ 0 | `python 工具/check_patch_conflicts.py` ⇒ 「没有跨补丁重复键（3 份补丁）」 |

⚠️ **行号漂移（必读）**：`Enchantment.cs` 里**追加/插入了文档与一个字段** ⇒ 该文件**所有旧落点行号整体 +18**（例：`Remove` 269 → **287**、`Attach` 190 → **208**、`CountSecretOrders` 500 → **518**、`Spend` 335 → **353**）。受影响的 A 行都在**主账表**（155 / 503 / 533 / 606 / 623 / 1043 / 1045 / 1048 / 1051 / 1052 / 1054）⇒ 请调度台在同步点跑一次
`python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A --fix`（漂移不是失败；`--fix` 按**裸声明名**重定位，本批已确认这些名字都还在）。
本批**自己的 8 行**用的是**改后现读**的行号，无需 `--fix`。

## ④ 改动清单 ＋ 改动请求

### ④·1 改动清单（两个文件，均在白名单内）

| 文件 | 内容 | 行（改后现读） |
|---|---|---|
| `RuleEngine/Core/Enchantment.cs` | `ActiveEnchantment.WhileInPlayAnim` 三态字段（+ doc） | `:150` |
| 同上 | `Attach` 新增形参 `whileInPlayAnim`（缺省 = 未知）＋ 末尾调 `ActivateTraitsAfterEnchant`；订正方法头第五步的旧说明 | `:208`（签名）/`:236-237`（ID）/`:275`（末尾那句） |
| 同上 | `FindSecretOrder` 加 `CountSecretOrders <= 0` 早退闸 ＋ 判据 doc | `:573` |
| 同上 | 类尾新段：`UnknownWhileInPlayAnim` · `HasEnchantmentWhileInPlay` · `CountUnknownWhileInPlayAnim` · `GetQuestTimes` · `FindCancelAttack(ctx, carrier, actingCard, targetCard, ignoreRandom)` · `TriggerNameAboutToAttack` · `HasAboutToAttackAbility` · `EnqueueRemove` · `ActivateTraitsAfterEnchant` · `BoardOwnerSlotOf` | `:728-967` |
| `RuleEngine/Core/UnitState.cs` | `ClearAllKeywords` 清记录账（+3 行注释/语句） | `:2575-2588` |
| 同上 | `RemoveEffectRecord` 的 `case 3` 注释补一句「按记录摘走 `RemoveTraitEffect`」（同零行增） | `:2811` |
| 同上 | 类尾新段：`BuffTypeAddTrait(3)` / `BuffTypeAddTraitRemnant(90)` / `TraitStack{Replace,Add,Count}` / `TraitEffectRecord` / `TraitEffects` / `TraitEffectValue` / `TraitStackTypeOf` / `ApplyTraitEffect` / `FindTraitEffect` / `RemoveTraitEffect` | `:2844-3079` |

> 行尾：两个文件改后仍是**纯 LF**（`CRLF 0 / bare CR 0`，二进制现数）—— 全程用 Edit 工具，⛔ 没用 `sed -i`。

账补丁：`资料/完全复刻/账/_parts/B9_W13EN.tsv`（8 行：64 / 481 / 532 / 687 / 763 / 770 / 1044 / 1047）· 生成脚本 `_gen_B9_W13EN.py`（读主表再覆盖，避免手抄长行）。

### ④·2 `EffectResolver` 侧改动请求（活 B 的另一半，**本写手不动**）

1. **落点**：`EffectResolver.ApplyOneGain`（现读 `EffectResolver.cs:11817`）的 `if (p.IsKeyword)` 正支（`:11838` 起）。
2. **改法**：入口加一个「这条是不是 `buffType ∈ {3, 90}` 的 addTrait」的标记（`EffectOp.AddTraitRemnant` 已经在用 `viaAddTraitRemnant` 这个先例），然后**正支**从
   ```csharp
   bool added = u.AddKeyword(p.Keyword, val);
   ```
   改成
   ```csharp
   bool added = u.ApplyTraitEffect(p.Keyword, val,
                   buffType: viaAddTraitRemnant ? UnitState.BuffTypeAddTraitRemnant : UnitState.BuffTypeAddTrait,
                   stackType: -1,                 // 按 truth table 现查（只有 markOfChaos = count）
                   source: by, extrinsic: false,  // 见 ③
                   timingBits: 0) != null;        // 见 ③
   ```
   ⚠️ **保留** `val >= 0` 的负值支（`lose <trait>` = `removeTrait(4)`）**原样**（原版不在那 8 个 buffType 里 ⇒ 不乘、也不记 addTrait 记录）。
3. **两个必须由 `EffectResolver` 决定的实参**（我这边只能给形状）：
   * `extrinsic`：原版 = `ability.trigger ∈ {20 WhileInPlay, 240 WhileInPlayVariable}`（`CardDef.IsExtrinsicAbilityTrigger` 已有判据）。**若传错**，后果 = 「本该合并的没合并 / 本该分条的合并了」⇒ 值仍对（`Σ` 不变），只影响实例条数。
   * `timingBits`：五个 `until*`（`untilEndOfTurn` / `untilPlayerTurnStart` / `untilEnemyTurnStart` / `untilFollowingPlayerTurnStart` / `untilNextAttack`）+ isPlayer。⚠️ 我们的**限时**增益现在走 `AddTempBuff` 那条独立路（到期摘修饰）——**建议**：先只把「无限期」那一档（`timingBits = 0`）切到 `ApplyTraitEffect`，限时那一档**保持现状**（否则会多出一本到期不清的记录账）⇒ 这条要 `EffectResolver` 的写手判，我这边已经把 `timingBits` 形参备好。
4. **不要动**：`PerCount` 折叠（`:576`）与 `buffMana` 折叠（`:16006`）—— 派活包/账 64 都写着「别碰已折好的两条路」。

### ④·3 其余改动请求（都不是我的文件）

| 目标 | 请求 | 判据 |
|---|---|---|
| `RuleCore.cs:7994`（`TryCancelAttackByOwnAboutToAttack` 一带） | 把 `Enchantments.FindCancelAttack(attacker)` 换成 `FindCancelAttack(ctx, attacker, actingCard, targetCard, ignoreRandom)`（实参怎么取要那边判） | `CardScript__HasCancelAttackEffect.c:1-66` + `CardAbility__CanTriggerAbility.c:1-143` |
| `RuleCore.RemovePray` 三处（`:1629` / `:3079` / `:8813`） | 若要按原版「排队摘」：改调 `Enchantments.EnqueueRemove(ctx, carrier.Instance, e.Effect, lane)`；⛔ **先定 `:1629`「先摘再给」的时序**（否则会晚一拍） | `BattleManager__RemoveEnchantment.c:26-79` |
| `CardPresentation/Battle/BattleDriver.cs`（P-11） | 532 ②：`removeEffect` 出队时对 targetCard **连撤四条队列的 anim cue**；③：`buffType ∈ {15,16}` 时撤该 trait 的帧特效（`RemoveDestroyableAnims` / `TraitFrameEffectsPlay`） | `BattleManager.cs:26290-26347`（ILSpy 全文） |
| `工具/gen_cards_truth.py`（P-07） | 加一列 `whileInPlayAnim`，值取 `cards_raw.json` 的 **`full_raw.whileInPlayAnim`**；`CardDef` 透传后把 `Attach` 的缺省改成读它 | `dump.cs:23338`；本机数据见 §二·2（true = 41，附魔卡 3 张） |
| 调度台 | ① `check_ledger_points --all-A --fix`（`Enchantment.cs` 全体 +18）② `PinnedParseMd5`（`E10TB` 的解析改动确认后） | §三 两行 |

## ⑤ 没查清 / 留给下一轮

1. **532 ②③ 的落点只能由表现层做** —— anim cue 与 trait 帧特效都在 `CardPresentation/Battle/`；`RuleEngine` 侧只有「入队体」这一层。判据已逐段写明（四条队列道名 + `buffType 15/16` 闸），留给 P-11。
2. **`ignoreRandom` 没有等价位** —— `RuleCore.BoardLevelCriteriaAllows`/`AbilityLevelCriteriaAllows` 都不收这个实参（原版只把它传给 `BoardAnalysis.MeetsCriteria` 的随机项）。本批**出声后按 false 判**，没有自己造一个随机口径。
3. **同卡同 trigger 多条 ability 的歧义** —— 原版 `HasCancelAttackEffect` 逐条 ability 判、任一真即真；我们三个闸按 `(卡, trigger)` 唯一定位 ⇒ 走既有「定位不唯一 ⇒ 不触发」。要不要为这条链单独做「逐 ability」口，留给引擎线裁。
4. **附魔卡不在 `cards_engine.json`** —— 三道 criteria 闸对附魔卡多半回「判不了 ⇒ 放行」，与原版「按 ability 数据判」不等价。要真对齐，得让附魔卡进卡表（那是卡牌线/数据面）。
5. **记录账与 `_keywords` 的两本账**（`UnitState`）—— 摘掉一条记录时，原版「同 trait 其它记录仍贡献值」这一格我们做不到（整条摘）。今天零可观察（多记录只能由 `ApplyTraitEffect` 造出来，而 `EffectResolver` 还没切）；真的切了之后要一起复核。`RemoveEffectRecord` 的 `case 3` 拿不到「哪条记录」⇒ 只摘词条（注释已互相指名）。
6. **`Attach` 那条 `ActivateTraitsAfterEnchant` 的守卫** —— 原版是 `cardState ∈ {2,3,15,17}`，我用「载体在棋盘上（`cell != null`）」等价；「待死（状态 5）」那一格我们这边的 `cell` 还在棋盘上（`CleanupDeaths` 才摘）⇒ 这一格方向**一致**，但「状态 3/17」的具体含义本轮没逐值核到 ⇒ 如实记。
7. **没跑 Unity 自检 / 真机**（按简报：写手不跑）。本批的可验口建议由主对话在同步点做：
   * `CountSecretOrders == 0` ⇒ `FindSecretOrder` 返回 null（人造夹具：只往 `ActiveEffects` 塞一条同卡 `buffType 12/15/16` 效果）；
   * `ApplyTraitEffect(kw, 2)` 连调两次 ⇒ `TraitEffects` **一条**、`TraitValue == 4`、`KwValue(kw) == 卡面 + 4`；`markOfChaos` 调两次 ⇒ `TraitValue == 2`（每次 +1）；
   * `Attach` 到带 `flank` 的在棋盘单位 ⇒ 该单位 `Exhausted == false`（= 原版 `canAct = true`）。
