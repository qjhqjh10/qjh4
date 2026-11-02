# B117_F1 · `B117-21/22/23/24/25`：引擎判 trait 只读 `keywords` 的**续修**

> 执行写手：`B117_F1`（2026-11-02）· 上一手 = `B117_W1`（`B117-01`，见 `报告/B117_W1_trait判据.md`）
> 一句话：把「原版 `MeetsTraitCriteria` / `HasCurrentTrait` 读**当前 trait 表** `+0x128`」这条判据，
> 接到**其余**还在走 `Has()`（只读 `keywords` 列）的消费端上；并就地订正三处过期注释。

---

## 〇、先说一条**必须知道的坑**（影响写法，后面多处引用）

`KeywordTable.Prefixes`（`Core/CardDef.cs:8343`）里**只有 `"oath"` 这一条、没有 `"oathdouble"`**，
而 `CardDef.TraitKeyOf` 走的是**前缀匹配**（`Core/CardDef.cs:6484-6498`）：

```
TraitKeyOf("oathDouble") → Normalize → "oath"      // 被前缀吃掉
TraitKeyOf("oath 2")     → Normalize → "oath"
```

⇒ 全池 **28 张** `cardTraits=["oath 1..8"]` 与 `UM100` 的 `["oathDouble"]` 归一化后**同名**。
**后果**：`HasCurrentTrait("oathdouble")` / `HasCurrentTrait("oath")` 对 `UM100` **都为真**，
所以「把 `oath` 那几处 `Has` 换成 `HasCurrentTrait`」会**反向**把 `UM100` 误判成「带誓约能力」。
⇒ 本批**只对 `oathDouble` 单独做精确判定**（`RuleCore.HasOathDouble`，见 §一·7），
`oath` 的其余读点**保持 `Has`**（那里现状 = 原版口径）。

数据现扫（`cards_engine.json` 1129 张）：除 `oath` 外，**没有任何**其它 trait 名存在同族前缀碰撞
（脚本对 24 个 trait 名逐一比 `raw.startswith(name) && raw != name`，只有 `oath→oathdouble`、
以及 `stealth.` / `vanguard.` / `remnant.` 三个**带句点**的脏写法 —— 那几个前缀匹配给的是正确名字）。

---

## 一、改了什么（文件:行号 + 为什么）

### `RuleEngine/Core/ConditionEval.cs`（**行数守恒 675**，全部是同物理行替换）

| 行 | 条件 | 改前 → 改后 | 判据（`d:/2/tools/decomp_full/`） |
|---|---|---|---|
| 215 | 20 `mightAttack` ① | `u.Has(BloodThirst)` → `HasCurrentTrait` | `CardScript__get_mightAct.c:7` `EntityScript__HasCurrentTrait(param_1,0xdc,0)` |
| 220 | 20 ③ | `u.SummonSickness && !u.Has("fast") && !u.Has("flank")` → **`RuleCore.DisplaySummonSickness(u)`** | `EntityScript__get_displaySummonSickness.c`（整个方法体两个 `HasCurrentTrait`）⇒ 判据收口到 `RuleCore` 那一处 |
| 221 | 20 ④ stun | `u.IsStunned` → `u.HasCurrentTrait(KeywordTable.Stun)` | `get_mightAct.c:23` `HasCurrentTrait(param_1,100)`。⚠️ `IsStunned` 是 `UnitState` 的属性（`UnitState.cs:317` = `Has(Stun)`），**不在本代理白名单** ⇒ 改在调用点 |
| 222 | 20 ④ cantAttack | → `HasCurrentTrait` | `get_mightAct.c` `HasCurrentTrait(param_1,0x96=150)` |
| 223 | 20 ④ nonCombatant | → `HasCurrentTrait` | `get_mightAct.c` `HasCurrentTrait(param_1,0x370=880)` |
| 250 | 100 `protectedFromDamage` | 3 个 `u.Has` → `HasCurrentTrait` | `MeetsTraitCriteriaFromList`（`FilterMethods__MeetsTraitCriteriaFromList.c` 逐条调 `MeetsTraitCriteria`） |
| 276 | 110 `currentCombatAttack` | `thisUnit.Has(DropPod)` → `HasCurrentTrait` | `EntityScript__get_currentCombatAttack.c:19/38` `HasCurrentTrait(param_1,0xe6)` |
| 289–290 | 120 `protectedFromDamageOrSurvivor` | 4 个 `u.Has` → `HasCurrentTrait`（**`u.Survivor > 0` 保留**，见 §四·B） | `CardScript__IsProtectedFromDamageOrSurvivor.c:20-97` |
| 340 | 230 `stealthAndNotDealtKillDmg` | `u.Has(Stealth)` → `HasCurrentTrait` | `MeetsConditionCriteria.c:268-277` `MeetsTraitCriteria(70)` |
| 501 | 320 `hasQuestCompleted` 的 jam 闸 | `u.Has(Jam)` → `HasCurrentTrait` | 原版 `:227` `!HasCurrentTrait(entity,130)` —— **原来注释写 `HasCurrentTrait`、代码写 `Has`（自相矛盾），本批把代码对齐注释** |
| 525–526 | 330 `stealthOrWardOrDuplicitous` | 5 个 `u.Has` → `HasCurrentTrait` | `MeetsConditionCriteria.c:124-145` 全 `MeetsTraitCriteria` |
| 533 | 331 `stealthOrCamouflage` | 2 个 `u.Has` → `HasCurrentTrait` | `MeetsConditionCriteria.c:146-156` |

注释同步订正（**行数不变**）：`:205/:207/:208/:209`（条件 20 判据逐条改成 `HasCurrentTrait` + 解释为什么不能写 `Has`）·
`:247` · `:274` · `:337` · `:515-516` · `:530`。

### `RuleEngine/Core/RuleCore.cs`

| 行 | 改了什么 |
|---|---|
| `:3553-3554` | `HasDeployExemption`：`SummonSicknessExempt(u) \|\| u.HasCurrentTrait(Ferocity) \|\| u.Has(Oath)`。**`oath` 那格故意仍用 `Has`**（理由 = §〇 的前缀碰撞；原文就地写在行尾注释里）。判据 = `CardScript__ActivateTraitsOnSummonOrEnchantment.c:86-99` 全 `HasCurrentTrait`。 |
| `:3562-3568` | `SummonSicknessExempt`：`u.Has("fast") \|\| u.Has("flank")` → 两个 `HasCurrentTrait`（`B117-21` 的另一半；`HasDeployExemption` / `DisplaySummonSickness` / `HasAttackDeployExemption` 三处共用它）。doc 就地订正（原来那句「各写了一遍 `u.Has(...)`」已过期）。 |
| `:3985-3986` | **`B117-22`**：`CardPlayedTagMask` 的 `card.Has("ephemeral")` → `card.Has("ephemeral") \|\| card.HasTrait("ephemeral")`（`card` 现读 = **`CardDef`** ⇒ 用 `HasTrait`）。注释里「104 张现读」订正成「`keywords` 103 ∪ `cardTraits` 104 = **104 张**」（两个集合是**包含**关系：`cardTraits` 那 104 是 `keywords` 103 的超集）。 |
| `:11847-11859` | **`B117-23`**：`OathExtraReplays` 的 `u.Card.OathDouble` → `HasOathDouble(u)`（精确判定，见下）。 |
| `:13753-13754` | **`B117-25`**：注释「`CardDef` 没有 `CardTraits` 列（数据是纯落值、不喂引擎）」→ 就地订正为「**已喂引擎**（`CardDatabase.AttachCardTraits`）；这里不用 `u.Has("talent")` 只是因为它只读 `keywords` 列」。 |
| `:18379-18412`（**类尾追加 34 行**） | **`B117-23` 的精确判定方法** `static bool HasOathDouble(UnitState u)` —— 见 §二。⚠️ 追加在**类尾**（本文件有 490+ 处账按 `RuleCore.cs:行号` 引用，现读最大落点 `:18247`）⇒ 既有行**零位移**。 |

### `RuleEngine/Core/EffectResolver.cs`
`:19117`（**行数守恒 21705**）：`HandTalentGateOpen` 的 doc 里「`CardDef` 的 `cardTraits` 列**没喂引擎** ⇒ 这里走不了 `HasDefaultTrait`」→ 就地订正为「**已喂**；这里仍不走它，只因本闸要与 `abilityTriggers` 保持**同集合**（三口径现读都 = 96 张）」。

### `RuleEngine/Data/CardDatabase.cs`
`:170-181`（**行数守恒 5088**）：
- 「⚠️ **本列不喂引擎**（纯落值）」→「🔴 **本列【已喂引擎】**」+ 给出读口清单（`CardDef.HasTrait` / `UnitState.HasCurrentTrait`）与判据（`CardScript__SetBaseCardTraits.c:37-42`）。
- 「全仓唯一的读点 = `Editor/RuleEngineTest.cs`」→ 现读点清单（`CardDef` / `UnitState` / `RuleCore` / `BattleContext`）。

---

## 二、`B117-23` 判据（**查到就改**：结论 = 该生效，已改）

| 环节 | 现读判据 |
|---|---|
| 原版读什么 | `BattleManager__IsThereDoubleOathEffect.c:33` —— `EntityScript__HasCurrentTrait(lVar3, **0x4fd**, 0)`，`lVar3` 是 `param_1 + 0x470` 那张**同方场上牌表**里被 `*(char*)(lVar3+0x40) == param_2` 筛出来的每一张 ⇒ **当前 trait 表**，不是正文文本。 |
| `0x4fd` 是什么 | `1277` = `DefinedTrait.oathDouble`（我们：`Core/EffectsToShow.cs:63` `public const int OathDouble = 1277;`，同文件 `:86` 把 1277 归进 `OathInAllTurns / OathDouble / OathTripleActivation` 那一族）。 |
| 卡面 | `d:/2/Warpforge部队卡片/Ultramarines/3部队/Warpforge_20_Chaplain-Cassius.png`；数据 `desc` = `Friendly Oath abilities apply an additional time.` / `descZh` = `友方的[[oath]]能力可以额外应用一次。` —— 与「多结算一次」**逐字对应**。 |
| 卡数据 | `UM100`：`keywords=["Talent: Catechism of Death"]` / `cardTraits=["Talent","oathDouble"]` ⇒ 旧判据（`u.Card.OathDouble`）读的是**正文文本**落的 bool（`CardDef.cs:4401` 的 `case "誓约加倍"`），而全池 **0 张** `descZh` 含「誓约加倍」⇒ 旧 bool **恒 false** ⇒ 这张卡的整个效果**从来没生效**。 |
| 结论 | **该生效，已改。** |

**改法（为什么不是一句 `HasCurrentTrait("oathdouble")`）**：见 §〇 的前缀碰撞。落地 =

```csharp
static bool HasOathDouble(UnitState u)   // RuleCore.cs 类尾
{
    if (u == null || u.Card == null) return false;
    var traits = u.Card.CardTraits;                 // public IReadOnlyList<CardTraitEntry>（CardDef.cs:6397）
    if (traits == null) return false;
    if (!u.HasCurrentTrait("oath")) return false;   // 借它带出「残骸档 / 已摘除」两道闸（那两位是 UnitState 私有）
    for (...) if (traits[i].Raw 去空格小写后 StartsWith("oathdouble")) return true;
    return false;
}
```

- **精确比 `Raw`** ⇒ 28 张 `oath N` 不会被误算（`Raw` 是卡表原样写法，见 `CardTraitEntry.Raw` 的 doc）。
- **`HasCurrentTrait("oath")` 当闸** ⇒ 残骸（原版 `TransformIntoRemnant.c:54-75` 摘光当前表）与被 jam 摘过（`ReactToUnitJam. :96-113`）两种情况下自动为假。
- ⚠️ **残余（如实记，未修）**：运行时用 `addTrait` 授予的 `oathDouble` 会走 `_keywords`、同样被折成 `oath` ⇒ 这条判不到。今天池内 `addTrait` 命中 `oathdouble` 的**为 0**（`EffectsToShow.cs:554` 自陈），零影响。**要彻底修**：往 `KeywordTable.Prefixes`（`CardDef.cs:8343`）补一条 `"oathdouble"` 且在 `"oath"` **之前** —— 那个文件不在本批白名单，交调度台。

---

## 三、闸读数

| 闸 | 命令 | 读数 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_b117f1 bash d:/4/Unity/工具/typecheck.sh` | 运行时 **0** 错 · 编辑器 **0** 错（**0/0**） |
| 解析对拍 | `bash d:/4/Unity/工具/ruleprobe.sh check` | `输出指纹 … ebdac779…` **未变** · `哨兵合计 失败 0 条` · `全池 1121 张 · 解析差异 0 行 · 改名 0 · 新增 0 · 消失 0` · **rc=0** |
| 账闸 | `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py` | 我改的四个文件 + 测试宿主**零漂移/零失败**（grep 逐文件确认）。全表读数在本次运行里是 `A 5831 · 漂移 7 · 失败 0`，那 7 条全部落在 `CardPresentation/Shell/MenuDraw.cs` / `CardPresentation/Core/ArmyIcons.cs`（**别的代理正在改**，非本批引入 —— 同一时段两次运行分别是 20 / 42 / 7 条，随它们落盘而浮动）。B 档 2 条 PROSE 是既有的。 |

- **行数守恒已逐文件核**：`ConditionEval.cs` 675（原 675）· `EffectResolver.cs` 21705（原 21705）· `CardDatabase.cs` 5088（原 5088）· `RuleCore.cs` 18379 → **18413（+34，全在类尾）**。
- `b.count(b'\r\n')` 对四个文件都是 **0**（纯 LF、无裸 CR），未见行尾翻转。
- ⛔ **没有跑 Unity 批处理**（简报要求：断言只写不跑，由调度台统一跑）。

---

## 四、B117-24 逐处判定表

> 口径：原版 `MeetsTraitCriteria` / `MeetsTraitCriteriaFromList` / `HasCurrentTrait` / `HasDefaultTrait` 读的都是**当前 trait 表** `+0x128`
> （`FilterMethods__MeetsTraitCriteria.c:30-47` 读 `param_1+0x128`；`CardScript__SetBaseCardTraits.c:37-42` 把 `cardTraits(+0x230)` 整表灌进去）
> ⇒ **trait 语义**。`RawCardScript.IsKeyword`（`+0x98`）那些是**兵种/关键词单值**，⛔ 与 trait 表不是一张表。

| # | 落点 | 原版读 trait? | 今天有受害者? | 动作 |
|---|---|---|---|---|
| 1 | `ConditionEval:215` 条件 20 ① `bloodThirst` | ✅ `HasCurrentTrait(0xdc)` | 无（`bloodthirst` 无 cardTraits-only） | **改** |
| 2 | `ConditionEval:220` 条件 20 ③ fast/flank | ✅ `HasCurrentTrait` | **有**：`GOF57` | **改**（收口到 `RuleCore.DisplaySummonSickness`） |
| 3 | `ConditionEval:221` 条件 20 ④ stun | ✅ `HasCurrentTrait(100)` | 无 | **改**（调用点；源头 `UnitState.IsStunned` 不在白名单） |
| 4 | `ConditionEval:222` ④ cantAttack (150) | ✅ | 无 | **改** |
| 5 | `ConditionEval:223` ④ nonCombatant (880) | ✅ | 无 | **改** |
| 6 | `ConditionEval:250` 条件 100 dodge/invuln/dropPod | ✅ `MeetsTraitCriteriaFromList` | 无 | **改** |
| 7 | `ConditionEval:289-290` 条件 120 同上 + shield | ✅ `MeetsTraitCriteriaFromList` | 无 | **改**（4 个词） |
| 7b | `ConditionEval:290` 的 **`u.Survivor > 0`** | ✅ 原版是 `MeetsTraitCriteria(430 survivor)` | **无**：`survivor` 在 `keywords` / `cardTraits` 两列的卡 **各 0 张**（全池现扫） | **不改**（理由见下） |
| 8 | `ConditionEval:340` 条件 230 stealth | ✅ `MeetsTraitCriteria(70)` | 无 | **改** |
| 9 | `ConditionEval:501` 条件 320 jam | ✅ `!HasCurrentTrait(130)` | 无（`jam` cardTraits-only = 0） | **改**（注释——代码自相矛盾就此消解） |
| 10 | `ConditionEval:525-526` 条件 330 stealth/ward/wardwb/tainted/vanguard | ✅ `MeetsTraitCriteriaFromList` | 无（`ward`/`wardwb`/`tainted` 全池 0 张） | **改** |
| 11 | `ConditionEval:533` 条件 331 stealth/camouflage | ✅ `MeetsTraitCriteriaFromList` | 无 | **改** |
| 12 | `ConditionEval:276` 条件 110 施法者 `dropPod` | ✅ `EntityScript__get_currentCombatAttack.c:19/38` | 无 | **改**（**简报没列，本批顺手改**；不涉及 110 那条已记账的 `null` 偏离） |
| 13 | `RuleCore.HasDeployExemption:3553` ferocity / oath | ✅ `ActivateTraitsOnSummonOrEnchantment.c:86-99` | 无 | **改 ferocity；`oath` 保留 `Has`**（§〇 前缀碰撞，改了反而把 `UM100` 误判成誓约卡） |
| 14 | `RuleCore.SummonSicknessExempt:3567` fast/flank | ✅ `get_displaySummonSickness.c` | **有**：`GOF57` | **改** |
| 15 | `RuleCore.RecomputeActAndAttack:3657` ferocity/oath | ✅ `ActivateMinion.c:43-69` | 无 | **未改，只报告**（同 `oath` 前缀碰撞；`ferocity` 那半零影响 ⇒ 一起留给下批，避免同一函数里一半新一半旧读起来像 bug） |
| 16 | `RuleCore.ActivateTraitsOnSummonOrEnchantment:3718/3721` fast/flank + ferocity/oath | ✅ 同 `:75-99` | 无 | **未改，只报告**（同上；且它只被 `TutorialScript.PlaceTroops` 调，教程里没有这几张卡） |
| 17 | `RuleCore:5759` `attacker.SummonSickness && !attacker.Has("fast")` | ✅ `displaySummonSickness` | 无 | **未改，只报告**（在另一条既有逻辑里；简报未列） |
| 18 | `RuleCore:6028 / :6126` `Has(Ferocity) / Has(Oath)`（Duty/替代行动闸） | ✅（对应 `.c` 侧也是 trait 读） | 无 | **未改，只报告** |
| 19 | `RuleCore:2862` `b[s].Has(Oath)` | ✅ | 无 | **未改，只报告** |
| 20 | `RuleCore:17129` `inst.Card.Has(Oath)`，行尾注释却写 `.c:10 HasCurrentTrait(oath)` | ✅ | 无 | **未改，只报告**（⚠️ **自相矛盾同 §一·501 那条**；但要改必须一起处理 `oath` 前缀碰撞，否则 `UM100` 会多出一个誓约激活口） |
| 21 | `EffectResolver:11867 / :17949 / :18595` `Has(Jam)` | ✅ `ReactToUnitJammed` 那一族 | 无 | **未改，只报告**（`jam` 的无白名单问题见 `B116_W19`；今天 cardTraits-only jam = 0 张） |
| 22 | `EffectResolverPartial_B88E2:205` `Has(Jam)` / `:992` `Has(Oath)` | ✅ | 无 | **不在白名单，只报告** |
| 23 | `EffectResolver.DoTakeControl` 那条 `t.Has("fast")` 预判 | ❌ **不是同一条判据** | — | **⛔ 不动**（既有判决：那是**卡面文本预判**「马上要给的豁免」，`ResolveOps` 按序跑、走到那一刻 `Has("fast")` 还是 false —— 见 `RuleCore.cs:3509-3518` 的原注） |

**第 7b 行为什么不改**：`u.Survivor` 是 `KwValue(KeywordTable.Survivor)`（`UnitState.cs:528`），即**关键词层**的数值镜像，
而原版那一格是 `MeetsTraitCriteria(430)` 的**存在性**判定。两者只有在「某张卡的 `cardTraits` 里有 survivor 却不在 `keywords` 里」时才分叉
—— 全池现扫 **0 张**（`keywords` 0 · `cardTraits` 0）。要改得动 `UnitState.Survivor` 的语义（还要覆盖「值为 0 但 trait 存在」那一档），
不在本批范围。**判据已写清，留给下批**。

---

## 五、断言清单

宿主 = **已接线**的 `Editor/RuleEngineTest_B116W19.cs`（`RuleEngineTest.cs` 的 `Guard("CheckB116W19Jam", …)`）
⇒ **零新增接线**：新节 `B117F1TraitConsumers()`（`:383`）由 `CheckB116W19Jam()` 的 `:42` 调用（紧接上一批的 `B117W1CardTraitsOnly()`）。

共 **21 条** `CheckTrue`，全部「先 `Check(前提)` 再 `Check(结论)`」，**没有** `if (前提) Check(结论)` 那种静默跳过
（`if (x == null) { CheckTrue(false, "结论无法判定…") }` 是显式失败，不是跳过）。

| # | 断的是什么 | 🧨 灭自证（改哪儿会红） |
|---|---|---|
| ① 前提 | `GOF57` 的 `keywords` **空** ⇒ `Has("flank")` 判不到 | 数据被改 ⇒ 红 |
| ①-a | `GOF57` 的 `cardTraits` 有 `Flank` ⇒ `HasTrait` 真 | 删 `HasTrait` ⇒ 红 |
| ①-b | 场上单位 `HasCurrentTrait("flank")` 真 | `HasCurrentTrait` 退回 `Has` ⇒ 红 |
| ①-c | 夹具带召唤病 + `SummonSicknessExempt` **真** | `RuleCore.SummonSicknessExempt` 改回 `Has` ⇒ 红 |
| ①-d | `DisplaySummonSickness` **假** | —— |
| ①-e | `HasDeployExemption` **真** | —— |
| **①-f** | **消费端**：`MeetsUnitCondition(ctx, u, 20, false) == true` | **条件 20 改回 `Has` ⇒ 红**（本件主诉） |
| ①'-a | **反证**：无 flank 的 `SAU7` + 召唤病 ⇒ `SummonSicknessExempt` **假** | 写成恒真 ⇒ 红 |
| ①'-b | **反证**：条件 20 对 `SAU7` **假** | —— |
| ② 前提 | `Daemonbreaker` 的 `keywords` 空 | —— |
| **②-a** | **消费端**：反射调 `RuleCore.CardPlayedTagMask` ⇒ `Ephemeral` 位**置位** | **去掉 `HasTrait("ephemeral")` ⇒ 红**（`B117-22` 主诉。`CardPlayedTagMask` 是 `internal`、本仓无 `InternalsVisibleTo` ⇒ 只能反射） |
| ②-b | **反证**：`ASH83` 的 `Ephemeral` 位**不置** | 恒置位 ⇒ 红 |
| ③ 前提 | `UM100` 的 `keywords` 无 oathdouble；`CardTraits` 已喂；`CardDef.OathDouble == false`（旧判据恒 false = 缺口本体） | —— |
| ③-a | 按 `CardTraitEntry.Raw` 精确命中 `oathDouble` | **改用 `HasTrait("oathdouble")` 就会被前缀折叠骗过** ⇒ 本行仍绿，但 ③-d 会红（见下） |
| **③-b** | **消费端**：`UM100` 在场 ⇒ `OathExtraReplays == 1` | **改回 `u.Card.OathDouble` ⇒ 红**（`B117-23` 主诉） |
| ③-c | **分侧**：对手侧 `p=1` 仍 0 | 不按 `p` 扫全棋盘 ⇒ 红 |
| **③-d** | **反证 · 前缀撞名**：`UM101`（`cardTraits=["oath 2"]`）**不算**加倍源 | **把 `OathExtraReplays` 写成裸 `HasCurrentTrait("oathdouble")` ⇒ 红**（会把全池 29 张 oath 卡都算成加倍源） |
| ④ 前提 | 三张夹具卡各自只在 `cardTraits` 列有 `Stealth` / `Dodge` / `ferocity` | —— |
| **④-a/b/c** | 条件 **330 / 230 / 331** 对「只在 `cardTraits` 有 stealth」的卡 **为真** | **那三处改回 `u.Has(...)` ⇒ 红**（`B117-24` 一族） |
| **④-d/e** | 条件 **100 / 120** 对「只在 `cardTraits` 有 dodge」的卡 **为真** | 同上 ⇒ 红 |
| **④-f** | `HasDeployExemption` 与条件 20 对「只在 `cardTraits` 有 ferocity」的卡 **都为真** | `HasDeployExemption` 的 `HasCurrentTrait` 改回 `Has` ⇒ 红 |
| ④-g | **反证**：只有 `Remnant` 的夹具卡三条全假 | 恒真 ⇒ 红 |

⚠️ 断言**只写不跑**（简报要求），真跑由调度台用 `RuleEngineTest.Run` 统一做。

---

## 六、查不到的

1. **`oathDouble` 的运行时授予路径**：`addTrait` 效果若授予 `oathDouble`，在原版会进 `+0x128`、被 `HasCurrentTrait(0x4fd)` 判到；
   在我们这儿它会被 `Prefixes` 折成 `oath`，`HasOathDouble` 的 `Raw` 精确比**覆盖不到**。
   ⇒ 今天池内 `addTrait` 命中 `oathdouble` 的 **0 条**（`EffectsToShow.cs:554` 自陈），**零影响**。
   **查过**：`cards_engine.json` 全池 1129 张的 `cardTraits`/`keywords`/`abilities` 三列、`CardDef.cs:8343` 的 `Prefixes` 表、
   `EffectsToShow.cs:540-560` 的 `OathTraitNames` 一族。**没有查**：真机跑一局塞一张 `addTrait oathdouble` 的效果卡（池内不存在这种卡）。
2. **`KeywordTable.Prefixes` 该不该补 `oathdouble`**：判据（`Prefixes` 表本身）看得清，但那个文件（`Core/CardDef.cs`）**不在本批白名单**
   ⇒ 本批**没改**，用 `HasOathDouble` 绕开。**这不是「查不到」，是「不在白名单」**，已在 §二 写明落地做法。

---

## 七、顺手发现（**未改**，交调度台分流）

1. 🔴 **`KeywordTable.Prefixes` 缺 `oathdouble`（以及 `oathtripleactivation` / `oathinallturns`）——这是本批踩到的真坑**：
   `TraitKeyOf` 的前缀匹配会让 `oathDouble` → `oath`，导致
   （a）`CardDef.HasTrait("oathdouble")` **对所有 29 张 oath 卡都为真**（假阳性）；
   （b）任何 `HasCurrentTrait(KeywordTable.Oath)` 的改动都会把 `UM100` 误判成带誓约能力。
   **建议**：在 `CardDef.cs:8343` 的 `Prefixes` 里、`new[] { "oath", "oath" }` **之前**插三条
   （`oathdouble` / `oathtripleactivation` / `oathinallturns`，右侧 = 同名），然后回头把 §四 里第 13/15/16/19/20 条统一换成 `HasCurrentTrait`。
   ⚠️ 改了它，`_keywords` 列里若有 `oathDouble` 的卡会从 `oath` 变成 `oathdouble` —— 现扫 **0 张**，安全。
2. **`CardDef.Has` 与 `HasCurrentTrait` 的分工建议写进 `CardDef.Has` 的 doc**（它现在只说「卡面事实 vs 场上状态」，
   没提「原版 trait 家族读的是后者」）。`CardDef.cs:6121-6143` 那段被账按行号引用，插行会整片漂 ⇒ 只能就地改写。
3. **`UnitState.cs:4381` 的交接注又过期了一点**：它写「`EffectResolver.cs` / `ConditionEval.cs` / `EffectResolverPartial_B88E2.cs` 共 4 处仍走 `Has`」——
   现在 `ConditionEval` 已**零处**（全换完，含 `:276`）。建议那条注改成逐处点名。
   （`UnitState.cs` 不在本批白名单 ⇒ 只报告。）
4. **`RuleCore.cs:17129` 与 `:501` 是同一类「注释说 `HasCurrentTrait`、代码写 `Has`」的自相矛盾**：`:501` 本批已修，
   `:17129` 因涉及 `oath` 前缀碰撞**没动**（见 §四 #20）。这类矛盾建议做一次全仓 grep 收口
   （口径：`grep -n "HasCurrentTrait" -B1 | grep "\.Has("`）。
5. **`RuleEngineTest_B116W19.cs` 里上一批（`B117_W1`）的四处 `if (card != null) { Check(...) }`** 属于「前提不成立就静默跳过」的写法
   （没有显式的「无法判定」失败行）。本批新增的段落全部按 `if (x == null) CheckTrue(false, "结论无法判定…")` 写；
   上一批那四处**不是本批产物、也没改**（避免同一文件两批风格混着改）⇒ 建议下批统一。
