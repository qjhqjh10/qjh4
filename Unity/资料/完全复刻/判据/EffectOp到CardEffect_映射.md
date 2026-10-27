# `EffectOp` → 原版 `CardEffect` 字段映射判据

> 判据型子代理（只读）。目的：把「我方 `EffectOp`（解析后的能力文本模型）」逐字段映到「原版 `CardEffect`（运行时效果记录）」的 8 个字段，判「能不能映射 / 怎么映射 / 差什么」，为 `GetEffectsToShow`（C2T7m §七挂起项）铺路。
> 我方源 = `RuleEngine/Core/EffectText.cs` 的 `EffectOp`；原版源 = `dump.cs` `CardEffect // TypeDefIndex 3199`（字段）+ `EntityScript__GetCardEffectsToShow.c`（用法）。
> 🔑 已有判据：`EffectResolver.IsSameHandEffect`（`:5662-5710`）本身就是一张「原版 `CardEffect` 字段 ↔ 我方那一格」的对照表，本文件把它扩成 8 字段逐条判。

---

## 〇、🔴 先讲一个结构性前提（决定映射不是 1:1）
- 我方 `EffectOp` = **一段能力文本解析出来的「动作」**（`Verb/Amount/Payload/…`）；原版 `CardEffect` = **一条运行时效果记录**。
- ⇒ **一个 `EffectOp` 不一定产生一条 `CardEffect`**：`deal/draw/create/deploy/destroy/reanimate/…` 这些是**动作**、根本不是 `+0x108` 里的记录；真正落成 `CardEffect` 的是「给 buff」那类 verb（`give/gain/lose/lowercost/setcurrenthealth/skipcarddraw/…`）。
- ⇒ 且**一条 `EffectOp` 的 `Payload` 可解析出多条** `GivePayload`（`give A and B`）⇒ **映射粒度 = 「每条 `GivePayload.Parse(Op.Payload)` 项」**，不是「每个 EffectOp」。
- ⇒ **结论：映射的口子是 `GivePayload`（载荷文法，`EffectText.cs` / `GivePayload.Parse`），不是 `EffectOp` 顶层。** 这跟原版 `CardEffect.IsSameEffect` 用 `buffType + 数值` 那套是**两张不同层的东西**（原版比较的是**运行记录**，我方比较的是**解析载荷**）。

---

## 一、逐字段映射表（8 字段）

| 原版字段（offset） | 我方映射源 | 能不能映射 | 怎么映射 / 差什么 |
|---|---|---|---|
| **`buffType` (+0x38)** | **`Op.Verb`**（我们这一层 verb 就是 buff 种类 —— `EffectResolver.cs:5653` 原话） | **可映射，但需一张「verb → BuffType 枚举值」表**（见 §二） | verb 是**字符串**、`BuffType` 是**int 枚举**（`none=0/addStats=1/changeCost=2/addTrait=3/removeTrait=4/swapStats=5/changeRace=6/skipCardDraw=7/markOfChaos=8/setHealth=9/…enchantment=12/…replaceAbility=40/…setCurrentHealth=100`）。**只有「产生持久记录」的 verb 进 `+0x108`**；动作类 verb（deal/draw/…）**不进**。⚠️ 表要新做，且**按 verb 里出现的频率/语义逐条定**，判据 = 原版 `CardScript.AddEffect` 的 `buffType` 分派（`decomp_full/CardScript__AddEffect.c` 的 case/switch）。 |
| **`trait` (+0x3c)** | **`GivePayload.Parse(Op.Payload)` 里那条的 trait 名**（仅 `give/gain/lose … [trait]` 有） | **部分可映射**（仅 trait-giving verb） | payload 里写 `"vanguard"` / `"mark of chaos"` / `"stun"` 等 → 映射到 `DefinedTrait` 枚举名。⚠️ 只有「这条 verb 授/摘 trait」时才有 trait；`addStats`/`changeCost` 那类 trait = `none(0)`。 |
| **`traitValue` (+0x40)** | **`Op.Amount`**（`GivePayload` 的数值） | **部分可映射** | trait 的层数 / mark 计数 = payload 项的数量（`give 2 vanguard` → traitValue=2；mark-of-chaos 聚合计数也是这格累加）。⚠️ `addStats` 的「值」不是 traitValue（那是 statChange，见下）。 |
| **`costChange` (+0x68)** | **`Op.CostSetTo` / `Op.Amount`**（`lowercost`/`paidmod`/`setcost` verb） | **部分可映射** | `buffType==changeCost(2)` 时 costChange=费用增量；`setCost(95)` 时用 `CostSetTo`。判据 = 原版 `changeCost`/`setCost` 那两个 buffType（见 §二）。 |
| **`markOfChaosType` (+0x78)** | **`GivePayload.Parse(Op.Payload)` 里 DarkPact 条的 `Variant`**（`EffectText.cs:2514-2529`：`DarkPact` 关键词 + `Variant`；没写 `of X` 时 = `"random"`） | **可映射（经 payload 文法）** | `Variant` 字符串（`khorne`/`tzeentch`/`slaneesh`/`nurgle`/`random`）→ `MarkOfChaosType` 枚举（原版 `+0x78` 值 10/0x14/0x1e/0x28 = 四神）。⚠️ `"random"` 是「未指定」——原版那格此时是哪个值需在 `GetCardEffectsToShow` 的聚合点确认（可能是 0 或抽定后的神）。 |
| **`secretBuff` (+0x90)** | **无** | ❌ **没建** | `EffectOp`/`HandEffect`/`ActiveCardEffect` **都没有这一格**。差什么：要么解析期按「secret 类效果」打标（判据 = 原版哪里置 `secretBuff=1`，`CardScript.AddEffect` 的某支），要么从卡的 `spellType`/`secretBuff` 来源推。**当前无法映射。** |
| **`questBuff` (+0x91)** | **无** | ❌ **没建** | 同上（quest 类）。**当前无法映射。** |
| **`originalIsPlayer` (+0xA0)** | **无（per-effect）** | ❌ **没建（per-effect）** | 我方 `HandEffect`/`ActiveCardEffect` **不存**「这条效果原始属于哪一方」；运行时能由 `ActingUnit` 的阵营推，但**没落成字段**（`CardInstance.cs:330` / `RuleCore.cs:10592` 只在注释里引原版 `originalIsPlayer` 的用法）。**当前无法逐条取。** |

---

## 二、`buffType` 的「verb → BuffType」表要新做（§一的 ①）

- 我方 `Verb` 词汇（现读，`grep Verb = "..."` 全文 43 个，下面只列**与 `CardEffect` 记录相关**的 + 明确**不是记录**的）：
  - **产生记录（进 `+0x108`）候选**：`give` / `gain` / `lose`（→ `addStats(1)` / `addTrait(3)` / `removeTrait(4)` / `markOfChaos(8)` / `changeRace(6)` 等，**按 payload 里的关键词定**——⚠️ 同一个 verb 按 payload 落不同 buffType，不是一个 verb 一个 buffType！）；`lowercost` / `paidmod`（→ `changeCost(2)`）；`setcurrenthealth`（→ `setCurrentHealth(100)`）；`skipcarddraw`（→ `skipCardDraw(7)`）；`stun` / `blind` / `jam`（→ `addTrait(3)`，trait = stun/blind/jam）；`reloadduty` / `ferocitystay` 等看语义。
  - **不是记录（不进 `+0x108`，属动作/流程）**：`deal` / `draw` / `drawref` / `drawtype` / `create` / `deploy` / `destroy` / `heal` / `gainenergy` / `refill` / `return` / `takecontrol` / `forceattack` / `repeat` / `whenclause` / `atturn` / `chooseone` / `choosecard` / `chooseeffect` / `triggerability` / `extratrigger` / `reanimate` / `persist` / `double` / `noeffect` / `become` / `costmore` / `costifcontrol` / `costwhen` / `eachunitdeal` / `spendspirit` / `reloadduty`…
- ⇒ **这张表不能简单「verb→一个 buffType」** —— 因为 `give/gain/lose` **按 payload 里的关键词/属性落不同 buffType**。正确判据 = **遍历 `GivePayload.Parse(payload)` 的每一项**：关键词类（darkpact/stun/blind/jam/vanguard…）→ `addTrait(3)`+trait；属性类（+N melee/ranged/health/armour）→ `addStats(1)`+statChange；`markOfChaos` → `markOfChaos(8)`+markOfChaosType。
- **要做的活**：把「`GivePayload` 项 → `buffType`(+trait/traitValue/statChange 对应格)」抽成一张表，判据 = 原版 `CardScript__AddEffect.c` 的 `buffType` 分派 + `CardEffect` 各 statChange 字段。

---

## 三、差什么（落地 `GetEffectsToShow` 前必须补齐）

1. **`buffType` 表**（§二）：`GivePayload` 项 → `BuffType` 枚举值（含 statChange 落到 `+0x48/+0x50/+0x58/+0x60` 哪几格）。**这是 §七挂起项第②件事的核心。**
2. **`secretBuff`/`questBuff`**：两格我们没建 —— 要么解析期打标、要么从卡来源推。**判据 = 原版置这两格的地方**（`CardScript.AddEffect` 或 spellType/secret 链），**本项目此前没查**。
3. **`originalIsPlayer`（per-effect）**：我们没落字段；要逐条取需在挂载时记「这一条来自哪一方」。
4. **`markOfChaosType` 的 `random` 兜底**：`Variant=="random"` 时原版那一刻的 `+0x78` 是什么值，需在 `GetCardEffectsToShow` 聚合点确认（四神之一？0？）。
5. **`EffectOp` 的多 payload / 非记录 verb 过滤**（§〇）：`GetEffectsToShow` 的项源不能直接遍历 `EffectOp`，要遍历「已生效的 `GivePayload` 项 / 运行记录」——这又回到**项源模型统一**（C2T7m §七的阻塞）。

---

## 四、结论
- **8 字段里**：`buffType`（需建 verb/payload→枚举表）、`trait`、`traitValue`、`costChange`、`markOfChaosType` **可映射（多经 `GivePayload.Parse`，粒度是 payload 项不是 EffectOp）**；`secretBuff`、`questBuff`、`originalIsPlayer` **三格我方没建、当前无法映射**。
- **这张表 ≠ 直接可用**：它只是「字段对应」，真正的阻塞仍是「**没有按原版 `+0x108` 序的统一运行记录列表**」（C2T7m §七）。映射要接在「统一项源」之上才有意义。
- **下一步**：① 建 `GivePayload`项 → `BuffType/trait/statChange` 表（判据 `CardScript__AddEffect.c`）；② 查 `secretBuff/questBuff/originalIsPlayer` 原版置值点；③ 项源统一（挂起项）。
