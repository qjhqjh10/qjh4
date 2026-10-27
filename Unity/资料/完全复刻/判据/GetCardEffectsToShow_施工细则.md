# `GetCardEffectsToShow` 项源统一 · 施工细则（执行代理照做版）

> 判据型子代理 `R3CEFF`（只读）· 2026-10-27。
> 目的：`CardDisplayWindow` 的「受到以下影响：」这块**项源**从「`UnitState.TempBuff`」换成「原版 `+0x108` 的只读投影」，
> 覆盖**全部族**（永久 / 光环 / mark / 附魔 / 手牌 / 临时 buff），并把**序 / 过滤 / 空槽 / 文本**逐句钉到原版。
> **本文只出施工细则，不改任何 `.cs`。** ⛔ 在此之前谁都别动 `CardDisplayWindow.SetEffectRows`（见 §三 S4 的前置）。

---

## 〇 结论（一屏）

1. **原版这条链有【两道】过滤，不是一道** —— `EntityScript.GetCardEffectsToShow` 之后，**`CardDisplayWindow.DisplayCardEffects` 还会再删掉 4 类**（`buffType ∈ {6, 30, 35}`、以及「卡上没有任何任务附魔时的 `16`」）。旧判据（`CardEffectsToShow_判据.md`）**只记了第一道**。
2. **`+0x108` = `EntityScript.activeEffects`（`List<CardEffect>`）**（`dump.cs:22025`）—— 一条**运行记录**列表，不是「解析出来的效果」。
3. 原版**按记录逐条出**；只有 **mark-of-chaos 按「四神」各聚一条**（`traitValue` 累加）。序 = **`List.Add` 序**。
4. 我方**没有**这条统一列表：`_statMods`（数值，插入序）/ `_traitEffects`（addTrait 记录，插入序，**今天 EffectResolver 还没切过去 ⇒ 恒空**）/ `_keywords`（合并后的总数，**无记录**）/ `TempBuffs` / `CardInstance.ActiveEffects`（附魔）/ `CardInstance.HandEffects`（手牌）/ `_removedTraits`。⇒ 投影要**跨 7 个源**合成。
5. **三个真实可见差异（今天就在错）**：
   - ① 关键词行文本缺「获得 … / … 一回合」这层壳（原版 `Battle/Effect/GetTrait[OneTurn]`，我们印裸关键词名）；
   - ② 属性行**一律用 `…OneTurn` 键** —— 永久增益被印成「…一回合」；
   - ③ mark 族**根本没出**（原版按四神各一行 + `(xN)`）。
6. **序的完全还原需要「加序戳」**（每条约记录一个 `Seq`）：`Auras.Recompose` 是「整份摘掉→重新加回」，不加戳的话光环那几行每次重算都会跳到队尾。这是 §三 S2 那一片。
7. **文本分支的关键常量都已取到**（键名 + 占位符 + 拼接格式），见 §1.6；**未取到的只有 `ChangeMeleeAndHealth` / `ChangeRangedAndHealth` 这两个组合键的触发条件**（§六）。

---

## 一 原版逐句

### 1.1 调用链（四个方法，各管一件事）

| # | 方法 | 职责 | 判据 |
|---|---|---|---|
| ① | `CardDisplayWindow.DisplayCardEffects(card)` | 取项源 + **第二道过滤** + 逐项塞槽 + 整组显隐 | `decomp_full/CardDisplayWindow__DisplayCardEffects.c:32-60`（全 110 行） |
| ② | `EntityScript.GetCardEffectsToShow()` | **第一道过滤** + mark 按神聚合 → 返回**全量**（不截断） | `decomp_full/EntityScript__GetCardEffectsToShow.c:1-196` · ILSpy `EntityScript.cs:2149-2497`（**可读**，推荐用它对照） |
| ③ | `CardEffectsService.EnableEffects(list, isPlayer)` | 5 槽逐项 `Enable → LoadEffect → IsTextEmpty`；**空文本也占一槽** | `decomp_full/CardEffectsService__EnableEffects.c:1-72` · ILSpy `CardEffectsService.cs:18-60`（⚠️ ILSpy **把 `isPlayer` 丢成 `default`**，以 `.c` 为准） |
| ④ | `CardEffectItem.LoadEffect(effect, isPlayer)` | 左列（谁给的）+ 右列（给了什么）的文字 + secret/quest 打码 | `decomp_full/CardEffectItem__LoadEffect.c:1-195` · ILSpy `CardEffectItem.cs:13-157`（**可读**） |

### 1.2 `+0x108` 到底是什么

- `EntityScript.<activeEffects>k__BackingField; // 0x108`，类型 **`List<CardEffect>`**（`d:/2/tools/il2cpp_out/dump.cs:22025`；属性 `activeEffects` 在同文件 `:22094`）。
- 一条 `CardEffect` 的字段（`dump.cs:119060-119100` 亲读，逐字段）：
  `+0x10 triggerEnchantAnim` · `+0x18 enchantingCard(CardScript)` · `+0x20 effectName(string)` · `+0x28 effectText(string)` ·
  `+0x30 extrinsic` · `+0x31 extrinsicVariable` · `+0x32..+0x36` 五个 `until*` · **`+0x38 buffType`** · **`+0x3c trait(DefinedTrait)`** ·
  **`+0x40 traitValue`** · `+0x44 newRace` · `+0x48/0x50/0x58/0x60` 四项增量（melee/ranged/armour/health）· `+0x68 costChange` ·
  `+0x74 permanentBuff` · `+0x75 doNotStack` · **`+0x78 markOfChaosType`** · `+0x7c daemonHostType` · `+0x80 mutationType` ·
  `+0x88 enchantment(RawCardScript)` · `+0x90 secretBuff` · `+0x91 questBuff` · `+0x92 usedEnchantment` · `+0xA0 originalIsPlayer` · `+0xA4 enchantmentCardId` · `+0xA8 isHandBuff`。
- **序 = 这张 `List` 的索引序**（`GetItem(list, i)`，i = 0..Count−1）。原版**没有任何排序**。
- **`+0x108` 里放的只是「挂上来的效果」**；**卡面印刷的 trait 不在里面**（那是 `CurrentTraits // +0x128`）。⇒ **投影不能把 `_keywords` 整本搬出来**（见 §二 D1）。

### 1.3 `GetCardEffectsToShow` 逐句（**两道判定 + 一类聚合**）

> 伪代码 = `.c:32-191` + ILSpy `EntityScript.cs:2159-2497`（两份一致；ILSpy 那份变量名可读，逐句核过）。

```
result = new List<CardEffect>()
agg = { Khorne:null, Nurgle:null, Slaanesh:null, Tzeentch:null }
if (activeEffects == null) return result
for i in 0 .. activeEffects.Count-1:
    e = activeEffects[i]
    if e == null: break                                  // 原版就是 break（不是 continue）

    // ── 第一道外闸：只放行「普通效果」与「mark」 ────────────────  (.c:40 / :175-186)
    if !( e.markOfChaosType == none(0)
          || e.markOfChaosType == Random(50)
          || e.trait == markOfChaos(330) ):
        continue                                          // 「markOfChaosType 是具体某一神、但 trait 不是 330」⇒ 整条不要

    if e.trait == markOfChaos(330):                        // (.c:47-130)
        switch e.markOfChaosType:
          case Khorne(10)   -> slot = agg.Khorne   ; break
          case Nurgle(20)   -> slot = agg.Nurgle   ; break
          case Slaanesh(30) -> slot = agg.Slaanesh ; break
          case Tzeentch(40) -> slot = agg.Tzeentch ; break
          default           -> continue                   // none(0) / Random(50) ⇒ **什么都不做、也不加入**（原版就这样）
        if slot == null:
            slot = CreateMarkCardEffect(e.markOfChaosType) // 见下
            slot.enchantment(+0x88) = e.enchantment(+0x88)
            result.Add(slot)                               // ⇒ 占位 = **该神第一次出现的位置**
        slot.traitValue += e.traitValue                    // **同神多条 → 一条、计数累加**
        continue

    // ── 普通效果的排除档（只对非 mark 生效）───────────────────  (.c:131-173)
    if e.buffType == changeCost(2) && e.costChange == 0: continue          // (.c:135-141)
    if e.buffType == addTrait(3) && e.trait ∈ {oathInAllTurns(1276/0x4fc),
                                               oathDouble(1277/0x4fd),
                                               oathTripleActivation(1278/0x4fe)}:
        continue                                                          // (.c:146-164)
    result.Add(e)                                                         // (.c:166-171)
return result
```

`CreateMarkCardEffect(god)`（`decomp_full/EntityScript__CreateMarkCardEffect.c:1-21` · ILSpy `EntityScript.cs:2498-2510`）：
**`new CardEffect{ buffType = markOfChaos(8), markOfChaosType = god }`**，其余全默认（`traitValue = 0`，`trait = none`，`effectName/effectText` 空）。
⚠️ 所以聚合出来的那条**不是**「原始 mark 记录」，它的 `trait` 是 **0**、`buffType` 是 **8** —— 下游 `LoadEffect` 正是按 `buffType == 8 || 0x11` 走 mark 分支的。

排除档的枚举值（`dump.cs` `DefinedTrait` 段现读）：`oathInAllTurns = 1276` · `oathDouble = 1277` · `oathTripleActivation = 1278`。

### 1.4 `DisplayCardEffects` 的第二道过滤（**`GetCardEffectsToShow` 里没有**）

`decomp_full/CardDisplayWindow__DisplayCardEffects.c:38-47`（倒序 `RemoveAt`，一遍）：

```
list = GetCardEffectsToShow(card)                // 拷一份（AddRange 到新 List）
hasQuests = HasQuests(card)                      // = 卡上 spellType==120 的附魔【件数】(:193-198 的 Enchantment.cs 注释)
for i = list.Count-1 downto 0:
    bt = list[i].buffType
    if bt == changeRace(6)
       || bt == addRace(30)
       || bt == daemonHost(35)
       || (bt == choiceEnchantment(16) && hasQuests == 0):
        list.RemoveAt(i)
```

⇒ **`changeRace / addRace / daemonHost` 永不出现在界面上**；`choiceEnchantment(16)` **只有卡上挂了任务附魔才出现**。

同一方法尾段还有两条（`.c:88-105`）：

- **整组显隐**：`n = 非空条数`；`n == 0 ⇒ 整组隐藏`；`n > 0` 时**再看卡本身**：
  `uVar12 = !RawCardScript.IsEnviromentaleffect(rawCardData)` ⇒ **环境效果卡（`spellType ∈ {210, 200}`）即使有条也把整组 `SetActive(false)`**，返回 0。
  判据：`decomp_full/RawCardScript__IsEnviromentaleffect.c:1-12`（`spellType(+0x90) == 0xd2(210) || == 200`）。
  📌 **可达**：`cards_engine.json` 池内 `spellType == 210` **39 张**（全是 `type = defence`：`Aspect Shrine` / `Webway Entrance` / `Defence Batteries` …），`200` **0 张**。⇒ 这 39 张只要在场上一被点开，「受到以下影响」这块**必须整块不出现**。
- 返回值 = 这个 bool，调用方用它决定后续（我们这边对应 `SetEffectRows` 的返回值 / `EffectRowCount`）。

### 1.5 `EnableEffects`：5 槽 / **空项占槽** / 槽满截断

`decomp_full/CardEffectsService__EnableEffects.c:17-52` · ILSpy `CardEffectsService.cs:18-60`（一致）：

```
slot = 0; nonEmpty = 0
while slot < effects.Count && slot < slots.Length:      // slots.Length = 5（13 场序列化实测恒 5）
    slots[slot].Enable()
    slots[slot].LoadEffect(effects[slot], isPlayer)
    if (!slots[slot].IsTextEmpty()) nonEmpty++
    else slots[slot].Disable()                          // ← **空文本的那一格关掉**
    slot++                                              // ← **空文本也照样 +1** ⇒ 留空洞
while slot < slots.Length: slots[slot].Disable(); slot++  // DisableLeftoverSlots
return nonEmpty > 0
```

⇒ **项源给全量、逐项一槽；空文本项 = 一个「洞」**（后面的行**不补位**）。
⚠️ `isPlayer` 那个实参：`.c` 里是 `LoadEffect(item, effect, param_3, 0)`，`param_3` 一路来自 `DisplayCardEffects` 的
`*(byte*)(card + 0x40)`（= **被展示那张卡的 `isPlayer`**，不是「看的人是谁」）。**ILSpy 版把它丢成 `default`** ⇒ 以 `.c` 为准。

### 1.6 `LoadEffect`：左列 / 右列逐分支（**文本表**）

`decomp_full/CardEffectItem__LoadEffect.c:56-195` · ILSpy `CardEffectItem.cs:13-157`（**可读**，逐句核过）：

**先判 secret / quest 打码**（`.c:56-70`）：

| 条件 | 左列（`enchanterText +0x20`） | 右列（`effectText +0x28`） |
|---|---|---|
| `secretBuff && isPlayer` | `Card_Trait/secretOrder` | `Card_Trait/classified` |
| `questBuff && isPlayer` | `Card_Trait/quest` | `GetEffectDesc(effect)`（正常正文） |
| `questBuff && !isPlayer` | `Card_Trait/quest` | `Card_Trait/questClassified` |
| 其余 | 见下表 | 见下表 |

⚠️ 这 4 条键 **不在官方 13 语表里**（`official_loc_13lang.json` 现查 4/4 缺）⇒ 见 §六。

**正常档（右列 = `GameStaticData.GetEffectDesc(effect)`）** —— `decomp_full/GameStaticData__GetEffectDesc.c`（492 行）+ ILSpy `GameStaticData.cs:1639-1830`：

```
1) effectText(+0x28) 非空 → 先按 "Card_Abilities/<effectText>" 查表；查不到就**原样印 effectText**
2) enchantment(+0x88) 非空 且 spellType == Quest(120) → 印**那张附魔卡的卡面描述**
3) 否则按 buffType：
```

| buffType | 右列文本 | 左列（谁给的） |
|---|---|---|
| **1 addStats** | 按**哪几项非零**选键（下表 A） | `effectName` 非空→`GetEffectName`；否则 `enchantingCard` 的卡名；都空→`""` |
| **2 changeCost** | `Battle/Effect/ChangeCost` = `"{0} Cost"`，`{0}` = 带符号费用 | 同上 |
| **3 addTrait / 90 addTraitRemnant** | `trait == notDestroyRemnant(1190)` ⇒ **空串**；否则 `Battle/Effect/GetTrait[OneTurn]` 的 `{0}` 换成 `TraitNameToString(trait)`，**`traitValue ≥ 1` 时再拼 `" " + traitValue`** | 同上 |
| **4 removeTrait** | `Battle/Effect/RemoveTrait`（= `"Remove"`）+ `": "` + traitName（`LAB_18087a68b` 那条 Concat） | 同上 |
| **5 swapStats** | `Battle/Effect/SwapStats` = `"Swap attack & health"` | 同上 |
| **6 changeRace / 30 addRace / 35 daemonHost** | —— **在 §1.4 那道过滤里被删掉，走不到这里** | —— |
| **7 / 11 / 12 / 13 / 14 / 15 / 16** | **空串**（`switchD_…caseD_7: return ""`；`.c:388-396`） | 仍会被写（`GetEffectDesc` 返回空 ⇒ 该格 Disable） |
| **8 markOfChaos / 17 lastMarkOfChaos** | `Battle/Effect/{Khorne\|Nurgle\|Slaanesh\|Tzeentch}`（前缀常量在 `.c:397-412`，键名在官方表里 4/4 命中） | **`[[markOfX]]`**（= `Card_Trait/markOfX`）+ **`traitValue > 1` 时再拼 `" (x" + traitValue + ")"`**（ILSpy `CardEffectItem.cs:146-154` 逐字） |
| **9 setHealth** | `Battle/Effect/SetHealth` = `"Set Health to {[0]}"` | 同上 |
| **10 changeAbilityCost** | `Battle/Effect/ChangeAbilityCost` = `"{[0]} cost of ability"` | 同上 |
| **20 doubleAttack / 25 ephemeralCopy / 26 tideCopy / 40 replaceAbility** | 空串（`default:` 只特判 45 / 70 / 85 / 95） | —— |
| **45 addStatsDropPod** | `"<sprite name=icon_dropPod> +{melee} / +{health}"`（ILSpy `GameStaticData.cs:1728-1752` 逐字，含 sprite 名） | 同上 |
| **70 setMeleeAttack** | `Battle/Effect/SetMeleeAttack` = `"Set Melee Attack to {[0]}"` | 同上 |
| **85 setRangeAttack** | `Battle/Effect/SetRangeAttack` | 同上 |
| **95 setCost** | `Battle/Effect/SetCost` = `"Set Cost to {[0]}"` | 同上 |
| **100 setCurrentHealth** | 空串（不在 switch 里，`default` 只认 45/70/85/95） | —— |

**表 A（`buffType == 1 addStats` 的键选择）** —— `.c:131-306` 逐分支现读；**「一回合」与否 = 五个 `until*` 里任意一个为真**：

| 非零项 | 永久 | 一回合 |
|---|---|---|
| 只有 melee | `Battle/Effect/ChangeMeleeAttack` | `…ChangeMeleeAttackOneTurn` |
| 只有 ranged | `ChangeRangedAttack` | `ChangeRangedAttackOneTurn` |
| 只有 health | `ChangeHealth` | `ChangeHealthOneTurn` |
| 只有 armour | `ChangeArmour` | `ChangeArmourOneTurn` |
| melee + ranged | `ChangeBothAttacks`（`{0}`/`{1}`） | `ChangeBothAttacksOneTurn` |
| melee + health | `ChangeMeleeAndHealth` | ⚠️ **官方表里没有 OneTurn 变体**（见 §六） |
| ranged + health | `ChangeRangedAndHealth` | ⚠️ 同上 |
| 三项都有 | `ChangeAllStats`（`{0}`/`{1}`/`{2}`） | `ChangeAllStatsOneTurn` |
| 全 0 | **空串**（`.c:145-148` `return DAT_1842b80e0`） | 同左 |

占位符有**两套**：`{0}`（属性/关键词那几族）与 `{[0]}`（`Set*` / `ChangeAbilityCost` 那几族）—— 别互相顶替（`Loc.cs:1610-1616` 已记过同一条）。
所有键的**取值都已在本地**：`Assets/CardPresentation/Resources/official_loc_13lang.json`（10,862 键，本节用到的 22 条逐条 `in` 命中）。
`Loc.T` 的三档兜底（手写 `Table` → `official_names` → **官方全表**）已能取到 `Battle/Effect/*` 那批 —— 只有 4 条 secret/quest 键缺（§六）。

### 1.7 「空文本」的分布（= 会留洞的档）

| 档 | 为什么空 |
|---|---|
| `buffType ∈ {7, 11, 12, 13, 14, 15, 16}` | `GetEffectDesc` 直接 `return ""` |
| `buffType ∈ {20, 25, 26, 40, 100}` | `default:` 只特判 45/70/85/95 ⇒ `return ""` |
| `buffType == 3/90` 且 `trait == notDestroyRemnant(1190)` | ILSpy `GameStaticData.cs:1795-1800` 显式返空 |
| `buffType == 1` 且四项全 0 | `.c:145-148` |
| 任何**有 `effectText(+0x28)`** 的记录 | **不空**（优先用 `effectText`/`Card_Abilities/…`） |
| 附魔、但 `enchantment.spellType == Quest(120)` | **不空**（印附魔卡描述） |

📌 **我方今天唯一可达的附魔记录** = `Pray Enchantment`（`buffType 15`，`spellType 0`，无 `effectText`）⇒ **空文本** ⇒ **占一槽的洞**（见 §四）。

---

## 二 我们现状 vs 原版

### 现状（现读）

- 项源 = **`UnitState.TempBuff`**（`CardPresentation/Battle/CardDisplayWindow.cs:851 RowsOf`），唯一调用点 = `BattleDriver.cs:13080`（棋盘轻点）。
- ⚠️ **简报里写的「`Badges.For + TraitFrames`」不成立**：`Badges.cs` 那 7 枚是**场上徽标**（另一个功能），`TraitFrames` 是**特效框**（`Core/TraitFrames.cs`）。效果清单这条链**只有 `RowsOf`**（全仓 `RowsOf(` 只有 `BattleDriver.cs:13080` + 断言 `Editor/BattleScene.cs:9719`）。

### 三类差异

**A. 原版会出、我们不出（整族缺）**

| # | 族 | 原版证据 | 我方现状 | 数据源（要接的） |
|---|---|---|---|---|
| A1 | **mark-of-chaos（四神各一行）** | `.c:47-130` + `CreateMarkCardEffect` | **完全不出** | `_keywords["markofchaos"]` 只有**总数**、`EffectResolver.PactOf(u)` 只记**最后一种**（`EffectResolver.cs:12436/12559`）⇒ **按神分不出来** |
| A2 | **永久属性增益**（`addStats` 无时长） | 表 A 的永久列 | 不出（`_statMods` 里**有**，但没人读） | `UnitState.StatMods`（`UnitState.cs:1892`，**插入序**，带 `Attr/Value/IsSet/Source/Trait/BuffType`） |
| A3 | **`setMeleeAttack(70)/setRangeAttack(85)/setHealth(9)/setCurrentHealth(100)`** | `.c:413-446` + `default` | 不出 | 前两个 = `_statMods` 里 `IsSet == true` 那两条；后两个**未建记录**（`DoSetHealth` 直接写 `Health`，`EffectResolver.cs:14616-14690`） |
| A4 | **`removeTrait(4)`** | `.c:355-366` | 不出 | `UnitState.RemovedTraits`（`UnitState.cs:875`，**HashSet ⇒ 无序**） |
| A5 | **附魔（`12/15/16`）** —— 空文本、占槽 | `.c:388-396` | 不出（也就没有那个洞） | `CardInstance.ActiveEffects`（`Enchantment.cs:139`）+ `ActiveEnchantments` |
| A6 | **光环给的关键词/属性** | 光环 = `WhileInPlay(20)` 那一族 ⇒ 也是 `+0x108` 记录（`CardInstance.cs:110-123` 已记这条判据） | 只在 `_keywords` 总数里，**没有行** | `_statMods`（`Source == Auras.GrantTag`）+ `_auraKw`（`UnitState.cs:1519`） |
| A7 | **手牌族（`isHandBuff`）** | `CardInstance.HandEffects` 的注释已证它属 `+0x108` | 没有行 | `CardInstance.HandEffects`（`CardInstance.cs:124`） |
| A8 | **`changeCost(2)` / `setCost(95)` / `swapStats(5)`** | 有文本 | 不出 | 见 §六（**结构上到不了展示窗**：cost 记录只挂在手牌上） |

**B. 我们出、原版不出**

| # | 现象 | 原因 |
|---|---|---|
| B1 | 屏上可能多出**印刷关键词**当「影响」行 | 现在没接（`RowsOf` 只吃 `TempBuff`）；**一旦直接搬 `_keywords` 就会多**。`+0x108` 只装挂上来的，印刷的在 `+0x128` ⇒ 必须**减掉卡面基线**（`CardDef.Keywords`，`CardDef.cs:6026`） |
| B2 | `buffType ∈ {6, 30, 35}` | 已在 §1.4 被删 ⇒ 我们**不能**给它们出行（我们也没建） |

**C. 序不同 / 文本不同（今天就在错的）**

| # | 差异 | 原版 | 我们 | 判据 |
|---|---|---|---|---|
| C1 | **关键词行缺壳** | `Battle/Effect/GetTrait` = `"Get {0}"`（一回合 = `GetTraitOneTurn` = `"Get {0} one turn"`），`{0}` = 关键词名（+`" N"`） | `CardText.KeywordDisplay(name)` ⇒ **裸关键词名**（「先锋」） | ILSpy `GameStaticData.cs:1806-1829` |
| C2 | **属性行一律 OneTurn** | 按 `until*` 选永久/一回合 | 恒 `…OneTurn`（`CardDisplayWindow.cs:890-893`）⇒ **永久增益被印成「…一回合」** | §1.6 表 A |
| C3 | **序** | `+0x108` 的 `List.Add` 序 | `TempBuff` 自己的插入序（只有一族） | §1.3 |
| C4 | **空项不留洞** | 空项占槽 → 留空洞 | 「空的根本不铸」（`CardDisplayWindow.cs:913` 自己写着这条差异） | §1.5 |
| C5 | **环境效果卡不整组隐藏** | `spellType ∈ {210,200}` ⇒ 整组 `SetActive(false)` | 没有这道闸 | §1.4 |
| C6 | 「谁给的」兜底 | `effectName` → `enchantingCard` 卡名 → `""` | `SourceCard` → `Src`（**`Src` 恒 `"战术卡"`**，`CardDisplayWindow.cs:872` 的注释自己写着） | §1.6 左列 |
| C7 | 同一次 `give` 的多个属性 = **一条**记录 | `.c` 的合并键里**没有属性维度**（`AddEffect.c:331-402`：按 `enchantmentCardId+buffType+markOfChaosType+daemonHostType+mutationType+!doNotStack+SameTiming` 合并）⇒ `+2 melee and +2 ranged` = **一条** `ChangeBothAttacks` | `_statMods` **一属性一条** ⇒ 会印成 2~3 行 | §1.6 表 A 的组合行 |

---

## 三 施工清单（执行代理照做）

> **切片原则**：一个切片一个写手、文件不相交。**S1/S2/S3 是引擎侧（`RuleEngine/`），S4/S5/S6 是表现侧，S7 是工具。**
> 🔴 **通用纪律**（本项目血案）：
> - 新成员**一律追加到类尾**（`账/*.tsv` 按 `文件:行号` 引用，插一行就漂 —— `UnitState.cs`/`CardInstance.cs`/`CardDisplayWindow.cs` 都在账里）。
> - 改完 `.cs` **立刻** `bash d:/4/Unity/工具/typecheck.sh`。
> - 自己定的东西（排序规则、缺失数据的兜底）**必须打 `// 🔶 自己设计（非原版）：… · … · …`**（铁律 18）。

### S0 前置（无它则后面全白做）

1. **`EffectResolver` 侧的 `addTrait` 还没切到记录账** —— `UnitState.ApplyTraitEffect`（`UnitState.cs:2986`）**已建好但零调用者**，`ApplyOneGain` 走的仍是裸 `u.AddKeyword`（`EffectResolver.cs:11877`）。⇒ **不切，`TraitEffects` 恒空**，A1/A4 全部做不了。
2. **`markofchaos` 没有「按神」的计数** —— 现状只有 `AddKeyword(MarkOfChaos, 1)` + 一个私有旁表记最后一种（`EffectResolver.cs:12226/12374`）⇒ A1 需要 **`ApplyTraitEffect("markofchaos", 1, markOfChaosType: god)`**（`FindTraitEffect` 的合并键里**已经有** `MarkOfChaosType`，`UnitState.cs:3047-3060` ⇒ 天然按神分行）。

### S1 · 新文件 `RuleEngine/Core/EffectsToShow.cs`（**只读投影**；无写点、无状态）

- **为什么新建而不是加在 `CardInstance`**：投影要读 `UnitState` 的 `_statMods/_keywords/_buffs/_traitEffects/_auraKw`（`CardInstance` 看不见这些）；而 `CardInstance` 那一半（`ActiveEffects/HandEffects`）可从 `u.Instance` 拿到（`UnitState.cs:32 public readonly CardInstance Instance`）。
- **内容**：

```csharp
public enum ShownFamily { Stat, Trait, RemoveTrait, Mark, Enchantment, HandBuff }

public struct ShownEffect {
    public ShownFamily Family;
    public int    BuffType;        // 原 +0x38
    public string Trait;           // 原 +0x3c（我们的关键词名；null/"" = 无）
    public int    TraitValue;      // 原 +0x40
    public string Attr;            // addStats / set* 的哪一项：attack|ranged|health|armour
    public int    Value;           // addStats 的增量 / set* 的绝对值
    public int    MarkOfChaosType; // 原 +0x78（none=0 / Khorne=10 / Nurgle=20 / Slaanesh=30 / Tzeentch=40）
    public bool   Timed;           // 原 five until* 任一为真
    public bool   SecretBuff, QuestBuff;   // 原 +0x90 / +0x91
    public string Source;          // 左列（谁给的）
    public string Body;            // 原 +0x28 effectText（非空 ⇒ 右列优先用它）
    public int    Seq;             // 加序（见 S2）
    public int    RowGroup;        // 同一次 give 的合并组（见 S3-c）
}

public static class EffectsToShow {
    /// 原版 `+0x108` 的等价投影：**全量、不截断、不排 5 槽**。
    public static List<ShownEffect> Of(UnitState u, bool isPlayer, BattleContext ctx = null);
}
```

- **投影规则（逐族，全都要有）**：

| 族 | 从哪读 | 字段怎么填 | 过滤 |
|---|---|---|---|
| **Stat（永久）** | `u.StatMods` | `Family=Stat; BuffType=m.BuffType; Attr=m.Attr; Value=m.Value; Timed=false; Source=m.Source` | 跳过 `Source == Auras.GrantTag` **? 不** —— 光环那份**要出**（A6），原版它在 `+0x108` 里 |
| **Stat（限时）** | `u.TempBuffs`（**优先于 `_statMods`**：同一条在两边都有 —— `AddTempBuff` 会建同一条修饰，`UnitState.cs:1670-1675`） | `Timed=true; Source=b.SourceCard ?? b.Src; Seq=b.Mod?.Seq` | 关键词型 `b.IsKeyword` 归 **Trait（限时）**，不归 Stat |
| **Trait（addTrait/90）** | `u.TraitEffects`（S3-a 接上后才有内容） | `Family=Trait; Trait=r.Trait; TraitValue=r.TraitValue; BuffType=r.BuffType; Timed=(r.TimingBits & 0x1F)!=0; MarkOfChaosType=r.MarkOfChaosType; Source=r.Source` | `Trait ∈ {oathInAllTurns(1276), oathDouble(1277), oathTripleActivation(1278)}` ⇒ **跳过**（§1.3） |
| **Trait（光环给的关键词）** | `u.AuraKeywords`（`UnitState.cs:1529`）+ `u.GrantedText` | 同 Trait，`Source = "光环"`（`Auras.GrantTag`） | 同上 |
| **Mark** | `u.TraitEffects` 里 `Trait == "markofchaos"` **按 `MarkOfChaosType` 分组求和** | `Family=Mark; MarkOfChaosType=神; TraitValue=Σ; Source = 该神对应的契约卡名` | 神 ∉ {10,20,30,40} ⇒ **跳过**（原版 `default → continue`） |
| **RemoveTrait** | `u.RemovedTraits` | `Family=RemoveTrait; Trait=t` | 无序集 ⇒ `Seq` 缺（§六） |
| **Enchantment** | `u.Instance.ActiveEffects` | `Family=Enchantment; BuffType=e.BuffType; Trait=e.Trait; Source=e.Source; SecretBuff/QuestBuff ← 对应 ActiveEnchantment.SpellType ∈ {60,120}` | `BuffType==16 && ctx 上该卡 ActiveEnchantments 里 spellType==120 的件数==0` ⇒ **跳过**（§1.4） |
| **HandBuff** | `u.Instance.HandEffects` | 每条 `he.Op` → `GivePayload.Parse(he.Op.Payload)` 逐项出 `Stat`/`Trait` 行，`Source = he.Source`，`Timed = he.Expire != Never` | 见 §六（映射表粒度） |

- **序**：`result.Sort((a,b) => a.Seq.CompareTo(b.Seq))`，`Seq` 相同再按 `Family` 稳定序（`List.Sort` **不稳定** ⇒ 必须给全序：`Seq` → `Family` → `Trait/Attr` 名，⛔ 别只比 `Seq`）。
- 🔶 **自己设计（非原版）**：`Seq` 相同的族内次序、以及「光环行被重算后归位到哪」= 我们定的（判据：原版只有一条 `List`，我们没有；见 S2）。**必须打标记**。

### S2 · `RuleEngine/Core/UnitState.cs`（**只加字段 + 类尾追加方法**，⛔ 不插进已有成员之间）

- 加 **`int _effectSeq;`** + **`public int NextEffectSeq() { return ++_effectSeq; }`**（**追加在类尾**）。
- 给三条记录类型各加一格 `Seq`：`StatMod.Seq` · `TraitEffectRecord.Seq` · `TempBuff.Seq`（**同样追加在各自类尾**）；`AddStatMod` / `ApplyTraitEffect` / `AddTempBuff` 各在**建记录那一刻**写 `Seq = NextEffectSeq()`。
- **光环重算不许换戳**（否则每次 `Auras.Recompose` 光环那几行都跳队尾）：在 `ClearAuraGrants()`（`UnitState.cs:1556`）里把 `_statMods`/`_auraKw` 那份的 `Seq` 快照进一个私有 `Dictionary<string,int> _auraSeq`（键 = `Attr` 或 `keyword`），`RecordGrant(..., source: Auras.GrantTag)` / `AddAuraKeyword` 重挂时**先查快照、命中就复用**。
  🔶 **自己设计**：原版没有「重算」这件事（光环记录只在来源进场时 `List.Add` 一次）⇒ 这是**我们模型的等价物**，要打标记。
- **断言**（在 S6 里写）：`Recompose` 前后同一张卡 `EffectsToShow.Of()` 的 `Seq` 序列**逐项相等**（灭自证：把快照删掉 ⇒ 这条红）。

### S3 · 引擎写入点接线（三个文件，**互不相交**）

- **S3-a `EffectResolver.cs`**：`ApplyOneGain` 的**关键词正支**（`:11877` 那句 `u.AddKeyword(p.Keyword, val)`）改成
  `u.ApplyTraitEffect(p.Keyword, val, buffType: u.IsRemnant && !u.IsLeavingRemnant ? 90 : 3, source: by, markOfChaosType: 0, timingBits: <限时位>)`
  —— 返回值 `null` 时保留原来那条「Resisted / 没挂上」日志。`lose <trait>` 那一支（`:11912+`）**不动**（原版是 `removeTrait` 记录，我们走 `RemovedTraits`）。
  ⚠️ `val <= 0` 时 `ApplyTraitEffect` 返 `null`（`UnitState.cs:2991`）⇒ 那条路径要**保持现在的行为**（不建记录）。
  ⚠️ 限时（`duration` 非空）那条：`timingBits` 要用 `TempBuff` 的到期位（`UntilMyNextTurn ? bit0/1`），**否则合并键会把它与永久记录合并成一条**。
- **S3-b `EffectResolver.ApplyMarkBody`**（`:12369-12376`）：把
  `u.AddKeyword(KeywordTable.MarkOfChaos, 1)` **换成** `u.ApplyTraitEffect("markofchaos", 1, source: by, markOfChaosType: GodOf(k))`，
  `AddKeyword(KeywordTable.DarkPact, 1)` **保留**（那是我们自己的层数代理，原版没有）。
  `GodOf(kind)` 映射（**三重已证**：`DarkPactFx` 的载荷文本 × 四张附魔卡 500 能力的属性项 × 官方 `Card_Trait/markOf*` 的中文名）：
  `blood → 10 Khorne` · `resilience → 20 Nurgle` · `excess → 30 Slaanesh` · `fate → 40 Tzeentch`。
  ⚠️ `random` 已在 `GrantDarkPactCore` 上游落成具体 `k`（`:12195-12211`）⇒ 这里拿到的一定是四种之一。
- **S3-c `EffectResolver.DoGive`（那条逐 `ops` 的循环）**：给同一次 `give` 解析出的**多条** `PayloadOp` 打**同一个 `RowGroup` 号**（`ctx.NextRowGroup()` 或就用 `NextHandEffectRecord()` 同款计数器），写进 `StatMod.RowGroup` / `TraitEffectRecord.RowGroup`。用途 = **C7**（多条属性合成一行）。
  🔶 **自己设计**：原版没有这个字段（原版一条 `CardEffect` 天生带齐四项增量）⇒ 要打标记。
- **S3-d `Enchantment.cs`**：`Enchantments.Attach` 给 `ActiveCardEffect` 补 `Seq`（挂载那一刻取值），并**把 `ActiveEnchantment.SpellType` 从 `CardDef.SpellType` 填上**（原文写「卡表没有 spellType 列」**已过期** —— `cards_engine.json` 有 `spellType`，`CardDef.SpellType` 在 `CardDef.cs:136/1016`）。⛔ 不改摘/花逻辑。
- **S3-e `Aura.cs`**：`RecordGrant(..., GrantTag)` / `AddAuraKeyword` 那两处（`Aura.cs:896/900`）加 `source` 传递即可 —— `Seq` 复用逻辑在 `UnitState` 里（S2），**Aura 侧不要自己写序**。
- **S3-f `RuleCore.cs`**：`AttachHandEffect` 给 `HandEffect` 补 `Seq`（挂载那一刻）。另：`RuleCore.cs:7251`（`stun`）`8600/8659`（`droppod`）三处裸 `AddKeyword` **要不要进记录账**——见 §六（droppod 是我们自己的表示，不建；stun 应走 `ApplyTraitEffect`）。

### S4 · `CardPresentation/Battle/CardDisplayWindow.cs`（**表现侧**）

> ⛔ **前置**：S1~S3 落地、`EffectsToShow.Of` 能出全族之后才动 `SetEffectRows`（否则等于用半成品项源替换一个好项源）。

1. **新增** `public static List<EffectRow> RowsOfEffects(IReadOnlyList<ShownEffect> items, bool isPlayer)` —— **左列/右列**按 §1.6 现算：

   | 族 | 左列（Who） | 右列（What） |
   |---|---|---|
   | Stat | `Source ?? ""` | 表 A 的键（`Timed` 决定永久/一回合） |
   | Trait | `Source ?? ""` | `Body` 非空 ⇒ 用 `Body`；否则 `Loc.T(Timed ? "Battle/Effect/GetTraitOneTurn" : "Battle/Effect/GetTrait").Replace("{0}", 关键词显示名 + (TraitValue>=1 ? " "+TraitValue : ""))` （见 §六 关于 `≥1` 的边界） |
   | RemoveTrait | `Source ?? ""` | `Loc.T("Battle/Effect/RemoveTrait") + ": " + 关键词显示名`（`RemoveTrait` 官方值 = 「移除」/`"Remove"`） |
   | Mark | `Loc.T("Card_Trait/" + GodKey) + (TraitValue>1 ? $" (x{TraitValue})" : "")` | `Loc.T("Battle/Effect/" + GodKey)` |
   | Enchantment / 空档 | 按 §1.6 的 secret/quest 打码，否则 `Source ?? ""` | **`""`**（= 洞） |

   `GodKey` = `Khorne|Nurgle|Slaanesh|Tzeentch`。
2. **保留** 旧 `RowsOf(IReadOnlyList<UnitState.TempBuff>)`（`Editor/BattleScene.cs:9719` 那条断言在用，别改签名）—— 但把它**标成 `[Obsolete]` 级别的注释**：「只为旧断言与手牌兜底，正式项源 = RowsOfEffects」。
3. **`SetEffectRows` 改成「槽位模型」**（对应 §1.5）：
   - 入参改成 `IReadOnlyList<EffectRow>`，其中**空文本项用 `EffectRow(null, null)` 表示**；
   - 循环 `i < min(rows.Count, 5)`：**每个下标固定占一槽**；`row == null` ⇒ `_fxBg/_fxWho/_fxWhat` 三件 `SetActive(false)`，**槽号照走**；
   - 整组 `_fxRoot/_fxTitle` 的显隐 = **非空条数 > 0**（不是 `rows.Count > 0`）；
   - **新增第 3 个实参** `bool hideForEnvironmental`（= `def.SpellType == 200 || def.SpellType == 210`）⇒ 为真时整组**强制隐藏**（§1.4）；`SetEffectRows` 改成返回 `bool`（= 是否真的显示了）并写进 `EffectRowCount`。
4. **关键次序别弄反**（本项目踩过）：先 `gameObject.SetActive(true)` 再 `SetText`（TMP 未激活量不出尺寸，`CardDisplayWindow.cs:913-923` 的注释）。

### S5 · `CardPresentation/Battle/BattleDriver.cs`（1 行）

`BattleDriver.cs:13080`：
```csharp
_cardDisplay.Toggle(ToCardData(...), u.Card,
        CardDisplayWindow.RowsOfEffects(EffectsToShow.Of(u, side == _me), side == _me));
```
（`isPlayer` 必须传「**这张卡是不是我方的**」—— 原版 `LoadEffect` 的 `isPlayer` 实参就是卡自己的 `+0x40`，见 §1.5。
⚠️ 另两处 `_cardDisplay.Show(...)`（`:2148` / `:6152`）与 `:13173` 的 `Toggle(card.Data, DefOf(card))` **传 null**，保持不变。）

### S6 · 断言（`RuleEngine/Editor/RuleEngineTest.cs` + `CardPresentation/Editor/BattleScene.cs`）

| # | 断言（文案） | 灭自证（改坏哪一处会红） |
|---|---|---|
| 1 | 「A1：连给 `blood` + `resilience` 两份契约 ⇒ 投影出 **2 条 Mark 行**（Khorne x1 / Nurgle x1），**不是 1 条 x2**」 | 把 S3-b 的 `markOfChaosType` 恒传 0 ⇒ 合成 1 条 ⇒ 红 |
| 2 | 「A1′：同神两份 ⇒ **1 条、`TraitValue == 2`**」 | 去掉 `FindTraitEffect` 的按神匹配 ⇒ 变 2 条 ⇒ 红 |
| 3 | 「A2：永久 `+2 melee` ⇒ 右列 = `Get +2 Melee Attack`（**不含 `one turn`**）；限时那条 = `… one turn`」 | 把 `Timed` 写死 true ⇒ 永久那条红 |
| 4 | 「A4：`lose Stealth` ⇒ 出 **1 条 RemoveTrait 行**，右列含关键词名」 | 删 `RemovedTraits` 那一路 ⇒ 0 条 ⇒ 红 |
| 5 | 「A5/§四：卡上挂 `Pray Enchantment` + 一条 `+2 melee` ⇒ **槽 0 是洞、槽 1 才是 melee 行**（`EffectWho(0) == ""` 且 `EffectWhat(0) == ""`）」 | 把空项改成不占槽 ⇒ 行跑到槽 0 ⇒ 红 |
| 6 | 「C5：`spellType == 210` 的卡（如 `Aspect Shrine`）带 buff ⇒ **整组不显示**（`EffectRowCount == 0` 且根 `activeSelf == false`）」 | 删环境卡闸 ⇒ 红 |
| 7 | 「C1：关键词行 `== "Get Vanguard one turn"` / `"获得 先锋 一回合"`（两语档各断一次），且 `!= "Vanguard"`」 | 退回裸关键词名 ⇒ 两条同时红 |
| 8 | 「B1：一张**只有印刷关键词**的单位 ⇒ 投影 **0 条**」 | 把 `_keywords` 整本搬出来 ⇒ 红 |
| 9 | 「S2：`Auras.Recompose` 前后 `Seq` 序列逐项相等」 | 删 `_auraSeq` 快照 ⇒ 红 |
| 10 | 「C7：`give +2 melee and +2 ranged` ⇒ **1 行**、右列 = `Get +2 Melee Attack and +2 Ranged Attack`（`ChangeBothAttacks`）」 | 不按 `RowGroup` 合并 ⇒ 2 行 ⇒ 红 |
| 11 | 「§1.3 排除档：`addTrait` 且 trait ∈ {1276/1277/1278} ⇒ 投影里**没有**（原版 §1.3 跳过）」 | 删那个过滤 ⇒ 红 |
| 12 | 「§1.4 排除档：`changeRace(6)/addRace(30)/daemonHost(35)` 与「无任务附魔时的 `choiceEnchantment(16)`」⇒ **投影末段没有**」 | 删过滤 ⇒ 红 |

### S7 · 工具切片 `工具/ruleprobe/EffectsProbe.cs`（+ `Program.cs` 一行分发）

```
bash d:/4/Unity/工具/ruleprobe.sh effects <卡id|卡名>
```
输出：`i | Family | BuffType | Trait | TraitValue | Attr/Value | Timed | MarkGod | Source | 左列 | 右列`
（**逐行、按 `Seq` 序**，前 5 行再标 `[槽 i]`/`[洞]`）。
⚠️ `Program.cs` 是热点文件，改它前先确认没有别的写手（`git status` 不算数 —— 看文件所有权表）。

### 推荐落地顺序（每步都能单独验）

`S0 → S3-a/S3-b（切记录账，跑 `ruleprobe check` 对指纹）→ S2（Seq）→ S1（投影）+ S7（探针）→ S4/S5（表现）→ S6（断言）→ S3-c/S3-d/S3-e/S3-f（其余族）`。

---

## 四 空槽处理（「空项占槽」到底归哪一步）

**结论：它是 S4-3 的一部分，不是单独一件活；但它【必须】在「项源换成全量投影」之后才可见。**

- 原版机制（§1.5）：项源给全量 → 逐项一槽 → 文本空的 `Disable` 掉那格 → **槽号照走** ⇒ 后面的行**不补位**。
- **为什么「单独先做」是死功夫**：今天的项源是 `TempBuff`，**每一条都有文本**（`RowsOf` 里 `what` 永远非空，认不出也回退 `b.Name`）⇒ 空项**一条都不会出现**，做了也看不见（`CardEffectsToShow_判据.md:124` 已记这条）。
- **换成全量投影之后，洞就是真的了**：§1.7 那几档一片一片地变成洞。**今天唯一可达的例子** = `Pray Enchantment`（`buffType 15`，`spellType 0`，无 `effectText`）⇒ 挂在单位身上时出一个洞（池内 23 张 SOR 卡祈祷后可达，判据见 `Enchantment.cs:53-58`）。
- **实现口径（照原版三条，别自己发明）**：
  1. `rows` 长度 = **投影条数**（含空项），**不是**「非空条数」；
  2. 槽号 = **投影下标**（第 i 条进第 i 槽，i < 5）；
  3. 整组显隐 = **非空条数 > 0**；`rows.Count > 5` ⇒ 静默截断（原版就是 `break`，我们保留现有那条 warning）。
- ⚠️ **洞的位置取决于序**（`Seq`）⇒ 没有 S2 的加序戳，洞会随光环重算乱跳 ⇒ **S2 是 S4-3 的前置**。

---

## 五 验收口

| 层 | 怎么验 | 期望 |
|---|---|---|
| **引擎（快内环，~1.5 s 重编）** | `bash d:/4/Unity/工具/ruleprobe.sh effects <卡id>` | 逐行打印 §三 S7 那张表；四神各一行、永久/一回合文本不同 |
| **引擎回归（不许退化）** | `bash d:/4/Unity/工具/ruleprobe.sh check`（**看两行**：解析差异 + 哨兵合计；⚠️ 只改「显示投影」**不应改结算指纹**） | 差异 0 行、哨兵 0 条。**若指纹变了 ⇒ 说明改动跑进了结算路径**（本活应该是纯读），要停下来查 |
| **引擎离线全池** | `bash d:/4/Unity/工具/ruleprobe.sh play all dump <tsv>` 前后对同一份基线 | 除 `handCostDelta` 类列外应无变化（S3-a 切 `ApplyTraitEffect` **有可能**改指纹 —— 那一天要**单独一个提交**、单独对，见 §六） |
| **Unity 断言** | `RuleEngineTest.Run`（引擎侧 1~4、9~12）· `BattleScene.Run`（表现侧 5~8 + 旧 `:9719` 那条不许红） | 全绿；**判绿看「断言合计」不看退出码** |
| **并排比（人眼）** | `CardDisplayWindow` 打开一张**带永久 buff + 一份黑暗契约**的单位，与原版截图并排 | 行的**垂直位置**（洞）、左列（谁给的）、右列（`Get …` / `Dark Pact of Blood (x2)`） |

**正例 / 反例（写断言时直接用）**

| 用途 | 卡/夹具 | 为什么 |
|---|---|---|
| Mark 正例 | 人造夹具：同一单位连 `GrantDarkPact(blood)` + `GrantDarkPact(resilience)` | 池内 `BL46 Gifts of Chaos` 会连给 2 份（`EffectResolver.cs:12222` 注释） |
| 永久 vs 一回合 | `+2 melee`（无时长）vs `+2 melee this turn` | C2 的判别式 |
| Trait 正例 | `give Vanguard` / `give Stun` | C1（`Get {0}` 外壳） |
| 空槽正例 | 挂 `Pray Enchantment` + 一条 `+2 melee` | §四唯一可达的洞 |
| 环境卡反例 | `Aspect Shrine` / `Webway Entrance`（`spellType 210`） | C5 |
| 印刷关键词反例 | 任何只有印刷关键词的单位（如 `Deathwing Terminator`） | B1（必须 0 条） |
| 排除档反例 | 人造 `addTrait` + `oathInAllTurns(1276)` | §1.3 的 oath 三档 |

---

## 六 还差什么（明写「查不到 / 还差哪份数据」）

1. **`RowGroup`（同一次 `give` 的多条合成一行）的粒度判据不全** —— 原版合并键（`CardScript__AddEffect.c:331-402`）里**没有属性维度**、且要求 `SameTimingAsEffect + 两边 extrinsic==0`；我们**不知道**「哪些 `PayloadOp` 在原版是同一个 `CardEffect`」。今天的可行口径 = 「同一次 `give` 子句（= 一次 `DoGive` 里的那条 `give`）」。
   **还差**：`EffectText` 的 `GivePayload` 项与原版 `CardEffect` 的**分句对应表** —— 目前**只能按 payload 文法切**，判据（原版是数据里一条 `buffEffect` 一条 `CardEffect`）**没做逐卡对账**。
2. **`ChangeMeleeAndHealth` / `ChangeRangedAndHealth` 的「一回合」变体在官方表里不存在**（22 条 `Battle/Effect/*` 现查：这两条只有永久版）⇒ 当「melee+health」是**限时**时，原版那一支返回什么**没核到**（`.c:175-306` 的组合分支有 8 条，我只逐句核到「按键名可辨」的粒度）。**还差**：`GetEffectDesc` case 1 那 175 行的逐分支行号表（要么读完 `.c:131-306`，要么造一条「限时 melee+health」记录跑原版）。
3. **`traitValue` 的拼接口径**：`addTrait` 行用 `" " + traitValue`（ILSpy `GameStaticData.cs:1815-1820`，条件是 **`traitValue >= 1`** ⇒ **1 也会印**），而 mark 行用 `" (x" + n + ")"`（条件 **`> 1`**）。两套格式都有判据，但「`addTrait` 行印 `Vanguard 1` 是不是玩家真看到的」**没有实拍核对过** ⇒ 建议 S6 加一条**只断格式、不断业务**的断言，并在报告里标明「未经实拍」。
4. **`RemovedTraits` 是 `HashSet`（无序、无值）** —— 原版 `removeTrait` 记录有 `+0x40 traitValue`、且在 `+0x108` 里**有序**。我们两样都没有 ⇒ A4 的行**放在队尾、值恒不印**。**还差**：把 `_removedTraits` 换成一条有序记录列表（**改动会碰 `HasRemovedTrait` 的 3 个读点**，属另一片）。
5. **`setHealth(9)` / `setCurrentHealth(100)` 没有记录**：我们的 `DoSetHealth` 直接写 `Health`（`EffectResolver.cs:14616-14690`），**不留痕** ⇒ 投影出不了这两行。**还差**：给这两条 op 建记录（`buffType 9` / `100`），或至少给 `UnitState` 加一格「本回合被 set 过」。
6. **`secretBuff` / `questBuff` 的 4 条键不在官方 13 语表**（`Card_Trait/secretOrder` `classified` `quest` `questClassified`，现查 4/4 缺）。**还差**：老 9,447 快照里有没有（未查）→ 没有就**如实兜底**（印键名是最丑的，改用 `"??"` + 出声？要按铁律 11 做，但先查快照）。
7. **`changeCost(2)` / `setCost(95)` / `swapStats(5)` 结构上到不了展示窗**：前两个只挂在手牌（`BattleContext.CostMods`），手牌卡在原版**不显示效果树**（`BattleDriver.cs:13062-13072` 已核实）；`swapStats(5)` 池内是否可达**没查**。⇒ 本活**不出这三行**，但要在账里写清「不是漏做、是结构上不出现」。
8. **手牌族（A7）的映射粒度**：`CardInstance.HandEffects` 存的是 `EffectOp`（`CardInstance.cs:172-245`），要变成 `CardEffect` 形状得走 `GivePayload.Parse`（`EffectOp到CardEffect_映射.md` 已出）—— 那张表的「`buffType` 表」**仍未落成代码**（该文件 §三 第 1 条）。本轮可以先**只出 `addStats` / `addTrait` 两类**（`GivePayload` 能直接判），其余 `Op` 出声跳过（⛔ 不静默）。
9. **`S3-a` 切 `ApplyTraitEffect` 会不会改结算**：`ApplyTraitEffect` 内部会 `AddKeyword`（`UnitState.cs:3021`），净行为**应当**等价；但 `FindTraitEffect` 的**合并**会改变 `_keywords` 之外的记录侧数值（`TraitEffectValue`），且 `EffectResolver` 目前有一条 `count` 档的既有实现（`IsCountStackableTrait`）⇒ **必须一次 `play all dump` 对指纹**才敢说「零差异」。**这条不确定 = 不许拍胸脯**。
10. **原版有没有「超过 5 条」的实况**：`.c` 里是 `break`（静默截断），我们保留 warning —— 但**没有任何一张真卡被验证过会产生 >5 条**。**还差**：全池离线算一遍投影条数分布（`ruleprobe effects` 扫全池即可），如果池内最大 < 5，则「截断」这一支**今天不可达**，断言写成「分布上限」而不是「截断行为」。
