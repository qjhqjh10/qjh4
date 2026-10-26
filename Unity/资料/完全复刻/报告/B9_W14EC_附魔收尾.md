# B9 · `W14EC` —— 附魔收尾：三个调用点切过去 ＋ `addTrait` 记录原语接线

> 执行代理 **`W14EC`**（引擎写手，有写权限）· 2026-10-26 · 第八十四会话 · 完全复刻批次 9
> 白名单（独占）= `RuleEngine/Core/Enchantment.cs` · `RuleEngine/Core/EffectResolver.cs` · `RuleEngine/Core/RuleCore.cs`
> ⛔ 没跑 Unity · 没动 git · 没改两张正本 · 没改主账表（只写 `账/_parts/B9_W14EC.tsv`）· 没碰白名单外文件。
> 判据一律 **第一权威 = 全量反编译方法体**（`d:/2/tools/decomp_full/`）＋ `报告/B9_W13EN_附魔与traitValue.md` 的逐条实读。

## ① 结论

| # | 账 | 件 | 结论 | 读数 / 可观察量 |
|---|---|---|---|---|
| 1 | **64** | `AbilityLogic.AdjustCardEffect` 的 `addTrait/traitValue` 半边 | 🟡 **C → B**（记录实例已补；限时档 / extrinsic / 两个合并键仍是缺口） | 唯一投递点 `EffectResolver.ApplyOneGain` 的 `IsKeyword` 正支：`val > 0 && duration 为空` ⇒ `UnitState.ApplyTraitEffect(...)`；`val == 0` 与限时档仍 `AddKeyword`。净词条值**逐字不变**（`AddKeyword` 的 count 档本来就每次 +1）⇒ `play check` 指纹不变 |
| 2 | **770** | `CardScript.HasCancelAttackEffect` | 🟡 **C → B**（调用点已切到五道闸那一版；剩「逐 ability 定位 / `ignoreRandom` / 附魔卡不在卡表」三条） | `RuleCore.cs:8021` 换 `FindCancelAttack(ctx, attacker, attacker, target)`（实参照原版 `(载体, 攻方, 原目标, ignoreRandom:0)`）· 池内 0 张卡挂「带 280 能力」的附魔 ⇒ 今天零可观察 |
| 3 | **481** | `BattleManager.RemoveEnchantment` | ✅ **C → A**（四个生产摘除口全部改成**排队摘**） | ① 密令揭示 = `Interrupt` 道（原版 `RemoveEnchantment(…, 2)`）；②③④ 祈祷附魔三处 = `Instant` 道（`priority` 数据真值 1）。**`:1629` 的时序已定案**：原版 `buffMana` 与 `340 removeThisEnchantment` **都入队、同道 FIFO ⇒ 先给信仰、后排摘** ⇒ 改成「先 `Faith += 1` 再排摘」（旧注释「先摘再给」已按铁律 5 就地订正） |
| 4 | **532** | `BattleManager.ResolveRemoveEffect` | 🟠 **仍 C**（①④ 的调用点本批已全接上；**②③ 在表现层**） | ②`CancelAnimFromUnitFromQueue` 四条道 / ③`RemoveDestroyableAnims`（buffType 15/16 帧特效）都在 `CardPresentation/Battle/` ⇒ 改动请求见 §四 |
| 5 | —— | `Enchantment.Remove` 的「空过」不再假成功 | ✅ 顺手修（同批需要） | 排队摘之后「记录已被残骸整表清掉」这一档会真的出现 ⇒ `List.Remove` 的返回值当判据，空过时**如实出声**并返回 `false`（改前一律打「摘掉」并返回真） |

**必报三项**

* **typecheck**：`TMPDIR=/tmp/wf_W14EC bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（本批最后一次 `.cs` 改动之后跑的；跑了两次，两次都是 0/0）。
* **`ruleprobe.sh check`**：**rc=1 · 1 条失败**，与批次开工时**完全相同**（**不是本批**）：
  * 失败条 = `PinnedParseMd5` 哨兵：现算 `c9a647982fad19bb65896011c281c93e` ≠ 钉住值；解析差异 **1 行** = `BL47|Disgustingly Resilient`；
  * 归因 = **`E10TB` 的 `EffectText.cs`**（加法型）。本批三个文件**都不在解析路径上**（`Enchantment` / `EffectResolver` / `RuleCore` 里被 `EffectText` 引用的只有注释）。
  * ⛔ 本代理**没有**动 `PinnedParseMd5` / `PinnedPlayMd5`。
* **`ruleprobe.sh play check`**：**rc=1**，当次现算 **`1b40f42ab010e623e908fcd4a236205f`** —— **与本批开工前 `W13EN` 记录的那一版逐字相同** ⇒ **本批 0 新差异**。四张差异卡（主档 `BL47`；电池 `BL18` / `SW25` / `UM18`）全部沿用开工前的归因（`E10TB` A / `E10TB` B① / `E7P06` 账 669 各 +4）⇒ 见 §三。

## ② 逐条证据

### ②·1 账 64 —— `addTrait` 正支改走【记录原语】（`EffectResolver.ApplyOneGain`）

* 判据（`W13EN` §二·7 逐句亲读，本批复核引用链）：原版 `CardScript__AddEffect.c` 的 `case 3` 在 `activeEffects(+0x108)`
  里留**一条 `CardEffect` 记录**，合并键 = 同 `trait(+0x3c)` ∧ 同 `markOfChaosType(+0x78)` ∧ 同 `daemonHostType(+0x7c)` ∧
  `CardEffect.SameTimingAsEffect` ∧ **两边 `extrinsic(+0x30)` 都为 0**（`:492-568`，倒序扫）；值 = **Σ `traitValue(+0x40)`**
  （`EntityScript__GetCurrentTraitValueWithModifiers.c`）。`AbilityLogic__AdjustCardEffect.c:1-59` **只乘五对数值字段**
  （`+0x48/+0x50/+0x60/+0x58/+0x68`），**⛔ 不碰 `traitValue`** ⇒ 「`change × N`」是**记录合并**自然得到的。
* 改法（`EffectResolver.cs:11846-11874`）：`IsKeyword` 正支里
  ```csharp
  bool added;
  if (val > 0 && string.IsNullOrEmpty(duration))
      added = u.ApplyTraitEffect(p.Keyword, val,
                 buffType: viaAddTraitRemnant ? UnitState.BuffTypeAddTraitRemnant : UnitState.BuffTypeAddTrait,
                 stackType: -1, source: by, extrinsic: false, timingBits: 0) != null;
  else
      added = u.AddKeyword(p.Keyword, val);
  ```
  * `viaAddTraitRemnant` 是**既有**形参（批次 4 `F2` 的 `EffectOp.AddTraitRemnant`，闸③ 的同一份判据）⇒ ⛔ 没有在别处再写一份闸。
  * `stackType: -1` = 让 `UnitState.TraitStackTypeOf` 按 `CardTrait._stackable` 真值现查（池内 75 词条 **74 add / 1 count（markOfChaos）/ 0 replace**）。
  * **净读数不变的论证**：`ApplyTraitEffect` 把**增量**交给同一个 `AddKeyword`，而 `AddKeyword` 自己已经把 `count` 档压成「每次 +1」
    （`UnitState.cs:1138`）⇒ add 档 = `val`（同改前）、count 档 = 1（同改前）、replace 档 0 张卡。
  * ⛔ **没动的三处**：`PerCount` 折叠（`EffectResolver.cs:578` 的 `TryFoldPerCount`）· `buffMana` 折叠（`:600` 的 `FoldManaMultiplier`）· **`removeTrait` 负值支整段**（原版不在那 8 个 buffType 里 ⇒ 不乘、也不建记录）。
* **只切「无限期」那一档**（判据 + 后果都写进代码注释）：限时档走 `AddTempBuff`，到期是 `RevertBuffs` → `RemoveKeyword`（摘词条、**不摘记录**）⇒ 切过去会攒下一本永不清理的记录账。`val == 0` 时原版连记录都不建。
* `extrinsic` 恒 `false`：本仓 `extrinsic` 只有两个载体（`CardInstance.HandEffect.Extrinsic`、光环 `Auras.GrantTag` → `UnitState.IsExtrinsicSource`），**都不经过 `ApplyOneGain`**（光环那条走 `Aura.cs:896` 的 `RecordGrant`）⇒ 这里判不到「来自 trigger 20/240」。如实记，不猜。
* 残余 (e)：`markOfChaos` 的那一跳（`EffectResolver.ApplyMarkBody` ①）仍**直接 `AddKeyword`**、不建记录 —— 那条记录的合并键含
  `markOfChaosType`（四种契约 10/20/30/40 各不同），而「契约种类名 → 数值」我们还没落列 ⇒ 填 0 会把不同契约并成一条（= 自己发明口径）⇒ **不补**，写进账。今天 `TraitEffectValue` 零消费者。

### ②·2 账 770 —— 唯一调用点切到五道闸重载

* 判据：`decomp_full/CardScript__HasCancelAttackEffect.c:1-66`（逐件附魔 → `HasAbilityType(0x118)` → 逐条 ability
  `CardAbility.CanTriggerAbility(0x118, bm, **载体**, actingCard, targetCard, ignoreRandom)`）＋ `CardAbility__CanTriggerAbility.c:1-143`（五道闸）。
* 实参（原版 `_ResolveAttack…:1310` → `:1368 CardScript__ResolveCancelAttack(攻方, 攻方, 原目标)`）：
  `carrier = attacker`、`actingCard = attacker`、`targetCard = target`、`ignoreRandom = false`（原版传 `0`）。
* 落点：`RuleCore.cs:8021`（`TryCancelAttackByOwnAboutToAttackCore` 的 ① 那一支）。旧单参重载（`Enchantment.cs:612`）保留未删。
* 为什么判 B 不判 A：① 原版**逐条 ability** 判、任一真即真，我们按 `(卡, trigger)` 定位唯一一条 ⇒ 同卡同 trigger 多条时走既有「定位不唯一 ⇒ 不触发 + 出声」；② `ignoreRandom` 没有等价位（传 true 时出声、按 false 判）；③ **附魔卡不进 `cards_engine.json`** ⇒ 三道 criteria 闸多半回「判不了 ⇒ 放行」—— 这一格是**卡数据侧**缺口（与 1044 的 `whileInPlayAnim` 落列同类）。今天零可观察。

### ②·3 账 481 —— `:1629` 的时序**定案**（这是本批最需要判据的一处）

判据（本批亲读，全量反编译）：

| 跳 | 判据 | 说明 |
|---|---|---|
| 340 → 入队 | `AbilityLogic__PlayAbility.c:2250-2282` | `chosenAbility == 0x154 (340 removeThisEnchantment)` 支 → `BattleManager__AddRemoveEnchantment(bm, 载体, 记录, priority = 载体ability+0x44)` |
| `AddRemoveEnchantment` | `BattleManager__AddRemoveEnchantment.c`（全文） | 组 `BattleAction{actionType=0x10}` → 尾部 **`BattleManager__AddAutoActionToQueue`** ⇒ **只入队** |
| `buffMana` 也是入队 | `BattleManager__AddManaBuff.c` 尾部 | 组 `BattleAction{actionType=0x22}` → **同样 `AddAutoActionToQueue`**、**同一个 `priority`** |
| 密令那一跳的优先级 | `BattleManager._ResolveRevealOrders_d__549__MoveNext.c:73` | `RemoveEnchantment(…, 2)` ⇒ `Interrupt` 道（且 `:76` 的 `AddExecuteSecretOrderAction` 在**入队之后同步**发） |

⇒ 两条结论：
1. **摘附魔在原版是「排队」，不是当场摘** ⇒ 我们四个调用点都该走 `EnqueueRemove`；
2. **`:1629` 的相对次序 = 先给信仰、后排摘**（两条 action 同道 FIFO）。旧注释「先摘再给（绝不重复发）」把「防重复」与
   「先后」混成一件事 —— 防重复靠的是 `HasPray` 闸，先后按原版是「给在前」。已按铁律 5 就地订正，并把 `Faith += 1`
   挪到排摘之前（本方法每回合开始对每个单位只调一次，摘动作排在**本回合动作结束**的泵点 ⇒ 不会重复发信仰）。

改动面（全部在 `RuleCore.cs`）：

| 落点 | 改成 | 道 |
|---|---|---|
| `:1659`（`GrantPrayEnchantmentFaith` 尾） | `Enchantments.EnqueueRemovePray(ctx, u)` | `Instant`（数据真值 1） |
| `:3099`（疲劳支 `MoveDeckTopToHand`） | 同上 | `Instant` |
| `:8863`（`ApplyDamageCore` 的 `!absorbed && dmg != 0`） | 同上 | `Instant` |
| `:8146`（`RevealSecretOrdersCore` ④） | `Enchantments.EnqueueRemove(ctx, host.Instance, eff, ActionPriority.Interrupt, …)` | `Interrupt`（原版参数 2） |

新增等价口：`Enchantment.EnqueueRemovePray`（`Enchantment.cs:1004`）= `FindPray` → `EnqueueRemove`（`Effect` 丢了那一条兜底走同步摘，⛔ 不留半条）。
`RevealSecretOrders` 本批**包了一层 `using (BeginAction(ctx))`**（它会入队 ⇒ 必须自带泵点，形状照 `GrantDarkPact` / `ApplyDamage` 的 W62 R1 口径）；该方法今天 **0 调用点**。

残余（如实记）：
(a) 原版每次入队带的是**发这条 ability 那条 action 的 `priority(+0x44)`**，我们只按「该族数据真值」取（密令 2 / 祈祷 1）—— 没有做「逐 ability 读 priority」的通用口；
(b) **疲劳那一支的泵点边界**：`MoveDeckTopToHand` 按 `Draw` 的注释跑在 `BeginAction` **之外** ⇒ 若调用方自己也没有外层作用域，这条摘会留到**下一个 `ActionScope`**（构造时先泵一次 / 出栈时泵）才执行 —— 与原版「排队、等下一个动作」同形，但比原来的同步摘晚。今天没有「疲劳之后立刻读 `HasPray`」的消费者；
(c) `Enchantment.RemoveByCard` 仍无调用点（原版也没有「按卡摘」入口）。

### ②·4 顺手修：`Enchantment.Remove` 的「空过」不再假成功

排队摘之后，「记录已经被清掉」这一档会真的出现 —— `EffectResolver.cs` 的残骸支对 `inst.ActiveEffects` / `inst.ActiveEnchantments`
两条 `Clear()`（翻残骸整表清）。改前 `Remove` 一律打「从 X 身上摘掉 Y」并返回真 ⇒ 会对外宣称一次**没发生**的摘除（静默假成功）。
现在用 `List.Remove` 的返回值当判据：真摘了打原来那句、否则**如实出声**（「本来就不在 …—— 这条 `removeEffect` 空过」）并返回 `false`。
调用面：`RemoveByCard` 的计数（只有 stale 条目会变）、`EnqueueRemove` 的出队体；`RemovePray` 生产上已无调用点。

## ③ 读数 · 回归 · 指纹逐条解释

| 读数 | 值 | 解释 |
|---|---|---|
| `typecheck` | 运行时 **0** / 编辑器 **0** | 两次（第一次三件改完、第二次 `Enchantment.Remove` 改完） |
| 行尾（二进制现数） | `Enchantment.cs` CRLF **0** / bare CR **0** · `RuleCore.cs` 同 · `EffectResolver.cs` **CRLF == LF**（16420/16420） | `EffectResolver.cs` 本来就是 CRLF，Edit 工具全程保持；⛔ 没用 `sed -i` |
| `ruleprobe.sh check` | rc=1 · 解析差异 **1 行（BL47）** | = 开工时状态，归 `E10TB`；本批三文件不在解析路径 |
| `ruleprobe.sh play check` | 现算 **`1b40f42a…`** · 基线 `af8e8178…` | **与 `W13EN` 记录的开工值逐字相同** ⇒ 本批 0 新差异 |
| ①-a 主档 | 1 张（BL47 = `E10TB` 加法型：多出 `✎ 改动 … {armour1}`） | 非本批 |
| ①-b 电池 | 3 张（BL18 / UM18 = `E7P06` 账 669 各 +4；SW25 = `E10TB` B①） | 非本批 |
| 账机检（只对我这 4 条补丁） | `A=1 通过=1 漂移=0 告警=0 失败=0` | `python 工具/check_ledger_points.py 资料/完全复刻/账/_parts/B9_W14EC.tsv` |
| 补丁冲突 | ✅ 0 | `python 工具/check_patch_conflicts.py` ⇒ 「没有跨补丁重复键（1 份补丁）」 |

⚠️ **行号漂移（必读，同步点处理）**：本批在三份文件里都插了行 ——

| 文件 | 行数 | 会漂的落点 |
|---|---|---|
| `Enchantment.cs` | 968 → **1017**（+49） | `RemovePray` 718→**727** · `HasEnchantmentWhileInPlay` 755→**764** · `GetQuestTimes` 785→**794** · `EnqueueRemove` 913→**922** · `FindCancelAttack(ctx,…)` 823→**832** · 旧单参 `FindCancelAttack` 603→**612** · `ActivateTraitsAfterEnchant` 943→**952**（`Remove` 287 / `CountSecretOrders` 518 / `FindSecretOrder` 573 / `Attach` 208 **未动**） |
| `RuleCore.cs` | 14921 → **14971**（+50） | 本批落点之后的全部（含 `:1629` → 现 `1659` 之类的旧引用） |
| `EffectResolver.cs` | 16392 → **16420**（+28） | `11846` 之后的全部（含 `ApplyMarkBody`、`buffMana` 那批旧引用） |

⇒ 同步点跑一次 `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A --fix`（**名字都还在**，只需重定位行号）。
本批自己的 4 行补丁用的是**改后现读**行号，无需 `--fix`。

## ④ 改动清单 ＋ 改动请求

### ④·1 改动清单（三份文件，均在白名单内）

| 文件 | 内容 | 行（改后现读） |
|---|---|---|
| `RuleEngine/Core/EffectResolver.cs` | `ApplyOneGain` 的 `IsKeyword` 正支：无限期 `addTrait` 走 `UnitState.ApplyTraitEffect`（+ 判据/残余注释） | `11846-11874` |
| `RuleEngine/Core/RuleCore.cs` | 账 770：换 `FindCancelAttack(ctx, attacker, attacker, target)` | `8021` |
| 同上 | 账 481：`GrantPrayEnchantmentFaith` 改成「先给信仰 → 再排摘」 | `1626-1661`（排摘在 `1659`） |
| 同上 | 账 481：疲劳支改排摘（+ 泵点边界注释） | `3099` |
| 同上 | 账 481：`ApplyDamageCore` 改排摘（`!absorbed && dmg != 0` 同一道闸） | `8863` |
| 同上 | 账 481：`RevealSecretOrders` 包 `BeginAction` ＋ ④ 改 `EnqueueRemove(Interrupt)` | `8099-8150`（排摘在 `8146`） |
| `RuleEngine/Core/Enchantment.cs` | 类尾新增 `EnqueueRemovePray`（排摘口） | `1004` |
| 同上 | `Remove`：空过如实出声、返回 `wasThere` | `297-310` |
| 同上 | `EnqueueRemove` / `RemovePray` 的 doc 就地订正（「调用点未切」→「已切」；「先摘再给」→「先给后排摘」） | `~912` / `~727` |

账补丁：`资料/完全复刻/账/_parts/B9_W14EC.tsv`（4 行：64 / 481 / 532 / 770）。**532 保持 C**，理由见 §四·2。

### ④·2 `532 ResolveRemoveEffect` 的 ②③ —— **表现层改动请求**（不是本写手的面）

| 目标 | 请求 | 判据 / 现状 |
|---|---|---|
| **③ `RemoveDestroyableAnims`**（`CardPresentation/Battle/BattleDriver.cs` ~`33736-33780`） | **表现层这一半早就就位**（`BattleDriver.RemoveDestroyableAnims(player, slot, trait)` + `Core/TraitFrames.RemoveDestroyable`，批次 83 `V3` 落的）。缺的是**引擎侧两个输入**：① `ActiveCardEffect` 没有 `Trait` 列（原版 `CardEffect.trait +0x3C`）② 摘效果**没有表现信号**（`EvtKind` 里没有「一条效果被摘掉」这一档） | 数据**在**：`cards_truth.json` 的 `abilities[].logic[].buff.trait`（`buffType ∈ {15,16}`）—— 现读 **57** 条里 **28** 条带 `trait`，只有两个值：**`480 vulnerable`**（`Spreading Corruption` 4 条）与 **`1185 pray`**（24 张祈祷卡）。⇒ 建议派给**卡牌线/数据生成器**：`CardDef` 侧落一列「该附魔的效果 trait」，然后本层在 `Enchantments.Attach(..., trait:)` 补一行写点（`AttachPray` 传 `pray`）＋ `EvtKind.EnchantmentRemoved`（`BattleEvent.cs`，不在本写手白名单）由 `Enchantments.Remove` 成功那一支 `ctx.Emit(...)` |
| **② `CancelAnimFromUnitFromQueue`**（四条动作道） | 摘效果出队时对 `targetCard` 调一次现成的「按卡撤待播 `playAnim`」口（`BattleDriver.cs` ~`34021`，账 238） | `decomp_full/BattleManager__CancelAnimFromUnitFromQueue.c:25-81`；现在只在「载体变残骸」时撤，摘效果时不撤 |
| 顺序（照抄原版） | ①`RemoveEffect` → ②四条道撤 cue → ③帧特效 → ④`FinishResolvingAction` | `out_cpp2il_dev_ilspy/BattleManager.cs:26290-26347`。①④ 的等价链本批已全接上（入队体的 lambda = `Enchantment.Remove`，出队即 ①；④ = 泵点） |

> ⚠️ 提醒表现层写手：`RuleCore.RevealSecretOrders` 本批起**会入队**（摘附魔晚一拍，与原版同形）—— 若重放/联机链要「同步看到摘掉」，请用同步口 `Enchantments.RemovePray` / `Enchantments.Remove`，⛔ 别指望 `RevealSecretOrders` 返回时附魔已经不在了（该方法今天 0 调用点）。

## ⑤ 没查清 / 留给下一轮

1. **限时 `addTrait` 不建记录**（账 64(a)）—— 原版连 `untilEndOfTurn` 那条也建记录、由 `RemoveEffect` 摘。要补齐得让**限时路**也进记录账并在 `RevertBuffs` 里按记录摘（`AddTempBuff` / `RevertBuffs` 在 `UnitState.cs`，本批白名单外）。今天零可观察（池内限时 `+trait` 那批的效果读数不走记录）。
2. **`extrinsic` / `markOfChaosType` / `daemonHostType`** 三个合并键恒常量（账 64(b)(c)）—— 需要「这句话来自哪条 ability（trigger 号）」与「契约种类 → 数值」两层数据，都在卡数据侧。
3. **`markOfChaos` 的 ① 不建记录**（账 64(e)）—— 理由见 ②·1 末段（合并键缺 `markOfChaosType` 数值）。
4. **同卡同 trigger 多条 ability**（账 770 ①）—— 要不要为「HasCancelAttackEffect 逐 ability」单独开一个口，留给引擎线裁。
5. **`ignoreRandom` 无等价位**（账 770 ②）—— 原版只把它传给 `BoardAnalysis.MeetsCriteria` 的随机项；我们没有随机项建模。
6. **`EnqueueRemove` 的 lane 没有「逐 ability 读 priority」的口**（账 481(a)）—— 今天靠「该族数据真值」硬填（密令 2 / 祈祷 1），两个都从数据/判据来，不是猜。
7. **本批没跑 Unity 自检 / 真机**（写手不跑）。建议同步点验的口：
   * `RuleEngineTest.Run` 的 `TestPrayEnchantmentX13`（**必跑**：它直接断言「自己回合开始 +1 信仰 → 附魔被摘」「真受伤 → 摘」「被盾挡下的 0 伤 → 不摘」——本批把这三处都改成排队摘，泵点 = `BeginTurn` / `ApplyDamage` 的 `ActionScope` 退出，测试在这两跳之后读 `HasPray` ⇒ 应仍绿）；
   * 人造夹具 `ApplyTraitEffect(kw, 2)` 连调两次 ⇒ `TraitEffects` **一条**、`TraitValue == 4`、`KwValue(kw) == 卡面 + 4`；`markofchaos` 连调两次 ⇒ `TraitValue == 2`；
   * 「排队的 removeEffect 在残骸整表清之后出队」⇒ 打「本来就不在 … 空过」且 `Remove` 返回 `false`（不假成功）。
