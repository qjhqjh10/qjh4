# `B143-ADDEFFECT-MERGE` —— `CardScript.AddEffect` 的 buffType 1/2/10 是否对同参数累加合并

> 只读取证 · 2026-11-05 · 执行代理 `B143-W7`（反编译取证）· **未改任何代码、未跑 Unity**。
> 起因 = `B143-R3` 初读「累加合并 ⇒ 原版多次 `BroadcastWhileInPlay` 后会越打越便宜」。本文件逐条核实。

## 〇、结论（三档）

**✅ 累加确认（原版行为，⛔ 别当 bug 修）** —— 但只到 `AddEffect` **这一层**；
「每次 `BroadcastWhileInPlay` 都会再 `AddEffect` 一遍且不先摘」这条**上层前提没能取证到底**（见 §四）。

---

## 一、判据源与交叉

| 源 | 位置 | 可用性 |
|---|---|---|
| **① `out_il2csharp`（优先）** | `D:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/CardScript.cs:10663`（`public bool AddEffect(CardEffect, int enchantmentCardId = -1)`） | ✅ **逐句可读**（switch 已展开） |
| **② `decomp_full`（C 伪码）** | `D:/2/tools/decomp_full/CardScript__AddEffect.c:340-420` | ✅ 与 ① **同一形状**（四个 `+=` + 同一组闸） |
| ③ `out_cpp2il_dev_ilspy` | `CardScript.cs:17882` 起 | ⛔ **不可用作本条的判据**：switch 被降级成 `Indirect jump: v1044 @ rcx_v28 (should have been resolved before IL gen)`，case→体 的分派丢了；四个累加也被渲染成指针写（全文件 `grep "costChange +="` = **0 命中**） |

⇒ **两份独立源逐条吻合**（① 的 switch 标签 + ② 的 `+=` 与闸），定论以 ① 为主、② 交叉。

---

## 二、逐 case

`out_il2csharp:10698` 先做 `object obj18 = newEffect.buffType - 0x1;`，再 `switch (obj18)`：

| switch 标签 | `buffType` 实际值 | 枚举名 | 走同一段体？ |
|---|---|---|---|
| `case 0:` | **1** | `addStats` | ✅ |
| `case 1:` | **2** | `changeCost` | ✅ |
| `case 9:` | **10** | `changeAbilityCost` | ✅ |

（枚举值 = `out_il2csharp/Assembly-CSharp/BuffType.cs:3-33`：`addStats = 1` · `changeCost = 2` · `addTrait = 3` · … · `changeAbilityCost = 10`。）
**三个 case 是 fall-through（共用同一个块）** —— 所以三者的比较键与动作**完全一样**。

### 2·1 外层闸（先于 switch）

```csharp
if (!newEffect.extrinsic)      // il2csharp:10703
{ … switch (buffType-1) { case 0: case 1: case 9: … } }
```
🔴 **`extrinsic == true` 的效果整段跳过** ⇒ 既不合并不累加（走别的支）。

### 2·2 比较键（全部成立才合并）—— **⛔ 不含任何数值参数**

`il2csharp:10737-10790`（`for` 遍历 `this.activeEffects`，对每个 `list11[i]` 逐条比）：

| # | 判据 | 实读 |
|---|---|---|
| 1 | `list11[i].enchantingCard != null` | `cardScript2 != null` |
| 2 | `list11[i].enchantingCard == newEffect.enchantingCard` | **同一张来源卡**才算同一条 |
| 3 | `list11[i].enchantmentCardId == newEffect.enchantmentCardId` | `+0xA4` |
| 4 | `list11[i].buffType == newEffect.buffType` | `+0x38` |
| 5 | `list11[i].markOfChaosType == newEffect.markOfChaosType` | `+0x78` |
| 6 | `list11[i].daemonHostType == DaemonhostType.none` | `+0x7C == 0` |
| 7 | `list11[i].mutationType == MutationType.none` | `+0x80 == 0` |
| 8 | `!newEffect.doNotStack` | `+0x75 == 0` |
| 9 | `newEffect.SameTimingAsEffect(list11[i])` | 见下 |

`SameTimingAsEffect`（`out_il2csharp/Assembly-CSharp/CardEffect.cs:94-156`，`RVA 0x877660`）逐个比**时机位**：
`untilEndOfTurn` · `untilPlayerTurnStart` · `untilEnemyTurnStart` · `untilFollowingPlayerTurnStart` · `untilNextAttack`
（后三者还叠一个 `SameIsPlayerEffect` = 两边的 `enchantingCard.isPlayer` 相同 —— `CardEffect.cs:158-172`）。

🔴 **关键**：**`meleeAttackChange` / `rangeAttackChange` / `healthChange` / `costChange` 四个数值一个都不参与比较**
⇒ 「同 key 但**参数不同**」的新效果**也会**并进旧那条。

### 2·3 命中时：**累加**，不是覆盖

```csharp
list11[num2].meleeAttackChange += newEffect.meleeAttackChange;   // +0x48
list11[num2].rangeAttackChange += newEffect.rangeAttackChange;   // +0x50
list11[num2].healthChange      += newEffect.healthChange;        // +0x60
list11[num2].costChange        += newEffect.costChange;          // +0x68
obj26 = 1;                                                       // 「命中了」标记
```
字段偏移出处 = `D:/2/tools/il2cpp_out/dump.cs:119088-119097`（`CardEffect` 的 `// 0x48/0x50/0x60/0x68`）。
`decomp_full/CardScript__AddEffect.c:387/392/402/…` 四句 `*(int *)(lVar8+0x48) = *(int *)(lVar8+0x48) + *(int *)(param_2+0x48)` 逐字段对上。

**未命中 ⇒ `activeEffects.Add(newEffect)`**（新增一条，`il2csharp:10790` 的 `if (obj21 == 0) list11.Add(newEffect);`）。

⚠️ 另有一处**不是合并带来的**副作用：合并之后那段仍会照常执行
`if (newEffect.healthChange != 0) currentHealth ±= …` 与 `AddArmour(newEffect.armourChange)`
⇒ **即使合并了，`healthChange` / `armourChange` 每次调用仍各结算一次**（`il2csharp:10791-10810`）。

---

## 三、可达性（池内逐卡统计）

数据源 = `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（`count = 1142`；`abilityTriggers` = 触发器 id 数组、`abilityBuffs` = 效果树）。
触发器 id：`WhileInPlay = 20 (0x14)` · `WhileInPlayVariable = 240 (0xF0)`（`RuleEngine/Core/AchievementTally.cs:265-270`；原版接收端 `decomp_full/RawCardScript__UpdateWhileInPlay.c` = `TriggerAbilities(0x14)` + `TriggerAbilities(0xf0)` **全文两句、无摘除**）。

| 口径 | 张数 |
|---|---|
| 全池 `abilityBuffs` 里 `buffType` 出现次数 | `addStats(1)` **399** · `changeCost(2)` **117** · `changeAbilityCost(10)` **0** |
| `abilityTriggers` 含 **20** 且带 1/2/10 的卡 | **9** |
| `abilityTriggers` 含 **240** 且带 1/2/10 的卡 | **22** |
| 同时含 20 与 240 | **0** |

### 3·1 再筛掉 `extrinsic`（§2·1：extrinsic 整段不进合并）

**trigger 20（9 → 4 张会合并）**：

| 卡 | 效果 | 是否 extrinsic |
|---|---|---|
| Venomcrawler | `changeCost -1` | extrinsic ⟹ **不合并** |
| Ramatekh the Cruel | `addStats (+1/0/0)` | extrinsic ⟹ 不合并 |
| Beastboss on Squigosaur | `changeCost -1` | extrinsic ⟹ 不合并 |
| **Grukk Face-Rippa** | `addStats (+1/0/0)` permanent + `changeCost -1` EOT | ✅ **会合并** |
| Hrolf the Ironhowl | `changeCost -1` | extrinsic ⟹ 不合并 |
| **Land Raider** | `addStats (0/0/0)` permanent | ✅ 会合并（数值全 0 ⇒ 无可观测） |
| **Aun'Shi** | `changeCost -1` permanent | ✅ **会合并** |
| Cadre Fireblade | `addStats (0/+2/0)` ×2 | extrinsic ⟹ 不合并 |
| **Piranha** | `changeCost -1` permanent | ✅ **会合并** |

**trigger 240（22 → 21 非 extrinsic）**：`Winged Autarch` / `Fate Inescapable` / `Minka Lesk` / `Baneblade Tank` / `Chimera` / `Unholy Smite` / `Alluress` / `Winged Daemon Prince` / `Rugged Disharmonist` / `Genestealer Familiar` / `Metamorph Leader` / `Patriarch` / `Atalan Leader` / `Nob on Smasha Squig` / `Banner Nob` / `Makari the Grot` / `Fenrisian Wolfpack` / `Enforcer Battlesuit` / `Bladeguard Ancient` / `Company Ancient` / `Honour Guard`（`Triarch Stalker` 是 extrinsic）。
⚠️ **但 240 那一族有「先摘」**（见 §四），所以它们**不会**跨次累加。

### 3·2 「同参数多次」最直白的候选

- **`Cadre Fireblade`**：`abilityBuffs` 里**两条一模一样的** `addStats (0, +2, 0)`、同 timing ⇒ 一轮里都跑到就会并成 +4（**但它俩都 extrinsic ⇒ 实际不合并**，见 §2·1）。
- **`Atalan Leader` / `Makari the Grot`**：各带两条同值 `changeCost` / 多条 `addStats`（均属 240 族 ⇒ 有先摘）。

---

## 四、上层前提：每次 `BroadcastWhileInPlay` 会不会**先摘掉**再重加？

这是「越打越便宜」能不能成立的关键，**分两族**：

| 族 | 是否先摘 | 证据 |
|---|---|---|
| **240 `WhileInPlayVariable`** | ✅ **有**「先摘」动作 | `RuleCore.WhileInPlayVariablePass` → 命中时入队 `removeEffectsFrom(47)`；原版对应 = `BattleManager__ResolveRemoveEffectsFrom.c`（全文两句：`BattleManagerSupport__BroadcastRemoveEffectsFrom(this, actingCard)` + `BattleManager__FinishResolvingAction(this, 0x2f, false)`），`0x2f = 47` |
| **20 `WhileInPlay`** | ❓ **没找到任何摘除点** | ① `RawCardScript__UpdateWhileInPlay.c` 全文 = `TriggerAbilities(0x14)` + `TriggerAbilities(0xF0)`，**无摘**；② `CardScript.RemoveActiveEffects` 在整个 `decomp_full` 里**只被 3 个文件引用**（它自己、`CardScript__Stun.c`、`CardScript__AddEffect.c` 的 **addTrait** 支）⇒ **addStats/changeCost 这一支不调它**；③ `BroadcastRemoveEffectsFrom` 的引用只有 `ResolveRemoveEffectsFrom` 自己 + `BattleManager__ResolveAction.c`（那是 case 分派，不是生产者） |

**❓ 缺什么（下一步取证）**：
1. **`0x2f`（removeEffectsFrom）这个排队动作的【生产者】** —— `decomp_full` 里只找到 resolver、没有 enqueue 点
   （应是某条 ability logic 在「240 / 光环重挂」路径上发的）。`grep -rl RemoveEffectsFrom decomp_full` 只有 7 个文件，
   `AbilityLogic__*` 一族里 **0 命中** ⇒ 要么它在别的类里、要么这条 0x2f 由**战斗动作结构**而非 ability 发。
2. **20 那一族的 ability logic 体**：`addStats` / `changeCost` 的执行体（`AbilityLogic.AdjustCardEffect` 一带）
   里**有没有**「先把这一来源加的同 buffType 清掉」这一步。今天 `CardScript.RemoveActiveEffects` 的三处引用里没有它，
   但那不等于没有别的清法（例如 `UnitState.RemoveExtrinsicEffectsFrom` 那条 extrinsic 专路）。

⚠️ **我们自己的等价物**：`RuleEngine/Core/Aura.cs:573` 写明光环重挂 = 「整份摘掉再重挂（= 原版 `RemoveExtrinsicEffectsFrom` + `AddEffect`）」，
`Aura.Recompose` 是「先全清、再加回」⇒ **我们这一侧不累加**。
⇒ 若 §四 第 2 条最终证明「原版 20 那一族**不摘**」，那么我们与原版**分叉**（原版越打越便宜、我们恒定）—— 那才是要修的差异；
在此之前，**⛔ 不要**按 R3 的初读去改任何东西。

---

## 五、给下游的三句话

1. **`AddEffect` 的 case 1/2/10 确实是累加合并**，比较键**不含数值参数**，且 `extrinsic` 整段不进 ⇒ **这一层照抄，别「修」**。
2. **🔴 2026-11-05 就地订正（第二轮取证，见 §六）**：原文写「20 的摘除点**没找到** ⇒ 尚未定论」—— **已查到**：`TriggerAbilities` 里**唯一**的摘除只在 `trigger == 240` 那一支，**20 完全没有摘除分支** ⇒ **20 族会累加，定论见 §六**。
3. 池内真正落在「20 ∧ 非 extrinsic ∧ 有数值」的只有 **4 张**（Grukk Face-Rippa / Land Raider / Aun'Shi / Piranha），
   其中**降费**的 2 张（Aun'Shi / Piranha）；`changeAbilityCost(10)` 全池 **0 张**（case 9 今天不可达）。

---

## 六、第二轮取证（2026-11-05 续）：**20 族确认无摘除**（🔴 本条结论**已被 §八 推翻**，保留取证痕迹）

### 6·1 结论（三档）

**🔴 2026-11-05 就地订正（`B143-R2` 复核 + 调度台裁定 `✅ R2 对`）**：本节原结论写「✅ 确认无摘除 ⇒ 原版会累加」——**错**。
「20 族没有摘除点」这半句**成立且已复核无误**，但它**推不出「会累加」**：漏了一步 —— trigger 20 的效果**运行时 `extrinsic = true`**，
而 `AddEffect` 的外层闸是 `if (!newEffect.extrinsic)` ⇒ **整段不进合并** ⇒ **不累加**。决定性证据与撤回清单见 **§八**。
下面 §6·2～§6·6 的**取证事实**（无摘除 / 无一次性闸 / 0x2f 不是生产路）**仍然成立**，只是**不能**用来推「累加」。

### 6·2 证据链（四跳，每跳两份源交叉）

| 跳 | 结论 | 判据 |
|---|---|---|
| ① `BroadcastWhileInPlay` 只负责遍历 + 调 `UpdateWhileInPlay` | **整条链无任何摘除** | `decomp_full/BattleManagerSupport__BroadcastWhileInPlay.c` 全文 125 行：先遍历战场（`IsInPlay` → `HasWhileInPlayAbility` → `UpdateWhileInPlay`），再遍历双方手牌同两跳；**全文 0 处 `Remove*`** |
| ② `UpdateWhileInPlay` 只连发两档触发 | **无摘除** | `decomp_full/RawCardScript__UpdateWhileInPlay.c` 全文 = `TriggerAbilities(0x14)` + `TriggerAbilities(0xf0)` 两句 |
| ③ **`TriggerAbilities` 里唯一的摘除被 `trigger == 0xF0` 闸住** | **20 (`0x14`) 走不到摘除分支** | 见 6·3（两份源逐字一致） |
| ④ 触发后没有「一次性」闸 ⇒ 每趟都会重新执行 | **会重复 `AddEffect`** | `decomp_full/CardAbility__CanTriggerAbility.c`（143 行）全文 = `ability.trigger == 传入 trigger` + `IsRightContext` + 三条 criteria；**没有任何 `alreadyTriggered` / 计数闸** |

### 6·3 关键证据：`TriggerAbilities` 的摘除只对 240

`decomp_full/RawCardScript__TriggerAbilities.c:207-233`（**C 版**）：
```c
cVar1 = CardAbility__CanTriggerAbility(lVar2, param_2, …);
if (cVar1 == '\0') {                    // 这一趟【不能】触发
    if (param_2 == 0xf0) {              // ← 只有 240
        if (ability.id == 0xf0) {
            if (param_3 == 0) break;
            BattleManager__RemoveExtrinsicEffectsFrom(param_3, param_4, 1);   // ← 摘在这一支
        }
    }
} else { … RawCardScript__TriggerAbility(...) … }   // 能触发 ⇒ 正常执行，**不摘**
```
`out_il2csharp/Assembly-CSharp/RawCardScript.cs:1036-1049`（**il2csharp 版**，逐字同形）：
```csharp
else if (num2 != 240)          { obj31 = obj10; num5 = num11; }
else if (this.cardAbilities[num11].trigger != num2) { obj31 = obj10; num5 = num11; }
else                           { manager.RemoveExtrinsicEffectsFrom(thisCard, 1); }
```
🔴 **`0x14`（20）连一个「else」分支都没有掉进摘除** —— 全文 `grep "Remove"` 只有上面这一处。

### 6·4 任务里问的另外两条

- **「任何遍历 `activeEffects` 并 `Remove` 的方法」**：`CardScript.RemoveActiveEffects` 在整个 `decomp_full` 里**只被 3 个文件引用**
  （它自己 / `CardScript__Stun.c` / `CardScript__AddEffect.c` 的 **addTrait** 支）—— **addStats / changeCost 支一次都不调**；
  另一个入口 `RemoveExtrinsicEffectsFrom` 的引用里有 `RawCardScript__TriggerAbilities.c`（= §6·3 那一处，240 专用）。
  ⇒ **20 族的 addStats/changeCost 没有任何清场入口。**
- **`0x2f`（47）在卡表里的出现**：`cards_engine.json` 全池 `abilityBuffs.buffType` 的取值分布 =
  `1(399) · 2(117) · 3(320) · 4(13) · 8(24) · 12(23) · 15(37) · 16(13) · 17(1) · 70(1) · 95(3) · 100(1)` —— **没有 47**。
  ⇒ `0x2f` **不是**卡表里的 buffType，它是 `BattleManager` 的**动作 kind**（`FinishResolvingAction(0x2f)`），
  由 `ResolveRemoveEffectsFrom` 那条 resolver 消费 ⇒ **「池内 0 行」符合预期，它本来就不该出现在卡表**。
  它**也不是** 20 族的生产路（`ResolveRemoveEffectsFrom` 的引用只有 resolver 自己 + `BattleManager__ResolveAction.c` 的 case 分派）。

### 6·5 施工要点（给那 4 张 · ⛔ 本代理不改代码）

> 前提：**这是原版自身的缺陷**（每个动作边界重放一次 while-in-play ⇒ 数值线性增长）。按 `CLAUDE.md` 铁律「原版自己也 bug ⇒ 照抄，⛔ 别修好」，我们**要改成累加**。

**先确认范围（不要一刀切）**：

| 卡 | while-in-play 效果 | 会累加吗 | 观测性 |
|---|---|---|---|
| **Aun'Shi** | `changeCost -1`，**无 `until*`**（常驻） | ✅ 每动作 −1 累积 | 🔴 **会越打越便宜**（第 N 个动作后所有手牌 −N 费） |
| **Piranha** | `changeCost -1`，常驻 | ✅ 同上 | 🔴 同上 |
| **Grukk Face-Rippa** | `addStats (+1/0/0)` 常驻 **+** `changeCost -1` EOT | ✅ 前半累积；后半 EOT 每回合被清 | 🟠 越打攻击越高（+1/动作） |
| **Land Raider** | 光环 `Your Warlord has Armour 2`（🔴 **有值**；早期按 `abilityBuffs` 记的 `(0/0/0)` 只覆盖**结构化那一行**，见 §6·5 末尾的订正） | ✅ 每趟 +2 护甲累积 | 🟠 督军护甲线性涨（实测见 §七） |

**落点建议（⛔ 别动通用光环路径）**：
1. `RuleEngine/Core/Aura.cs` 的 `AuraSpec` 是「来源在场就有效、离场就收回」的**重挂模型**（`Aura.cs:180-186` 自己写明这一族就是 `WhileInPlay = 20`）；
   直接改 `Recompose` 会波及**全部光环卡** ⇒ ⛔ **不许**。
2. 正确做法 = 给 `AuraSpec` 加一个「**while-in-play 累加**」标记（暂名 `AccumulateOnWhileInPlay`），
   只由这 4 张卡的 aura 规格置真；`Aura.Recompose` 遇到它时**跳过「先清」那一半**、只做「再加」，
   并让 `AddEffect` 的合并语义接管累加（= 原版 `list11[n].costChange += new.costChange`）。
3. `play check` 指纹**必然变**（属预期变化）⇒ 按流程交调度台裁定是否带入基线，⛔ 子代理不重钉基线。

**验收（含灭自证）**：
- `ruleprobe play` 跑一张带 `Aun'Shi` 的局，连续 3 个动作后：手牌费用应比初始**低 3**（改前恒低 1）。
- 「改坏法」：把 `AccumulateOnWhileInPlay` 去掉 ⇒ 必须回到 −1（红）；把 `+=` 写成 `=` ⇒ 也必须红。
- `Land Raider` 做**结构断言**（aura 标记置真）而不是数值断言（三值全 0，数值断言恒真 = 自证）。

### 6·6 仍然没查的一点（如实记）

`RemoveExtrinsicEffectsFrom(thisCard, 1)` 的**第二形参 `1`** 与「摘的是**这张卡身上的** extrinsic 还是**它施加给别人的**」没有展开
（`BattleManager__RemoveExtrinsicEffectsFrom.c` 未逐句读）。这条**只影响 240 族**，不影响本次 20 族结论；
若将来要核 240 那半，以 `BattleManager__RemoveExtrinsicEffectsFrom.c` 为准。

---

## 七、落地（2026-11-05 · `B143-W7`）—— 🔴 **本节全部已撤回，见 §八**

### 7·1 改了哪几处

| 文件 | 改动 | 行数 |
|---|---|---|
| `RuleEngine/Core/Aura.cs` | ① `AuraSpec.AccumulateOnWhileInPlay`（**默认 false**）· ② 静态计数表 `_wipAccum` + `AccKey` / `BumpWhileInPlayAccum` / `WhileInPlayAccumCount` · ③ `RecomposeCore` 入口调 `BumpWhileInPlayAccum` · ④ 费用那半 `Delta = -CostLess * (置真 ? n : 1)` · ⑤ `ApplyAura` 的 `accumN`，属性授予与关键词都 `* accumN` | +约 60 |
| `RuleEngine/Core/CardDef.cs` | `AddAura` 里按 **卡 id** 置真 + `AccumulateWhileInPlayIds`（`TAU32` / `TAU20` / `GOF29` / `SW38`） | +约 20 |
| `CardPresentation/Editor/B143_W7_Assert.cs` | **新建**：A 段（结构 9 条）· B 段（费用累加 5 条）· C 段（属性累加 5 条），含 3 条灭自证 | 新建 |

🔴 **「不绑就零影响」的硬保证**：`AccumulateOnWhileInPlay` 默认 `false` ⇒ `BumpWhileInPlayAccum` 连表都不建；
`WhileInPlayAccumCount` 对未置真的一律返回 `1`；两处施加点都是 `* 1` ⇒ **既有光环卡逐位不变**。

### 7·2 `play check` 结果（⛔ 未重钉基线）

```
结算指纹：当次现算 = a1b413fe6693b2a11297523238dfb466 · 基线 = 2f640db50876e5630a6a5dd4fcbe82ad（不同）· rc=1
①-a 逐卡差异（主档）：🔴 1 张卡变了 · 新增 0 · 消失 0
①-b 逐卡差异（事件电池）：🔴 2 张卡变了 · 新增 0 · 消失 0
```

| 卡 | 档 | 变什么 |
|---|---|---|
| **`SW38 Land Raider`** | ①-a 主档 | `✎ 改动 P0[4] WARLORD **2/0/0/38 → 2/0/2/38** {armour2}` ⇒ 改成 **`2/0/4/38` {armour4}**（投出去那一拍本来就 recompose 两次 ⇒ +2 ×2） |
| **`SW38 Land Raider`** | ①-b 电池 | `raw / ctrl 37 → 51`（电池里再多 recompose 几趟 ⇒ 护甲继续涨） |
| **`TAU20 Piranha`** | ①-b 电池 | `netFull` **只是顺序变了**（`-1` / `-3` 两组数值两侧一致）⇒ 降价条目在账上的排列位次变了，数值本身未偏离基线口径 |

- **`TAU32 Aun'Shi` / `GOF29 Grukk Face-Rippa` 在这一版夹具里没上榜** —— 它们的 while-in-play 目标是**自己场上的部队**，
  而 `play check` 的夹具只摆单卡 + 对手督军 ⇒ 目标池为空 ⇒ 这条改动在**那个夹具里**观测不到（不是没生效；
  `B143_W7_Assert` 的 B/C 两段用专门夹具直接钉住了 `-1/-2/-3` 与 `+1/+2/+3`）。
- 🔴 **基线没有重钉**（`PinnedPlayMd5` 仍是 `2f640db5…`）—— 按派活要求留给调度台在同步点裁定。
  这三个差异**都是本改动的预期结果**，不是回归。

### 7·3 `typecheck`

```
运行时错误数: 0
编辑器错误数: 0
```

### 7·4 遗留

1. `B143_W7_Assert` **未接宿主**（派活说宿主由调度台接）：入口 = `B143_W7_Assert.Run()`，读 `Pass` / `Fail` / `RunCount`。
2. 计数表是 `ConditionalWeakTable<BattleContext, …>` ⇒ **一局一份**；但**同一局内来源离场再回来**计数**不会清零**
   （原版离场会把它的 extrinsic 效果摘掉 ⇒ 回来从 0 重新叠）。⚠️ 这条**只在「同一个卡位、同一光环序号」上重现时**才有差别，
   池内那 4 张都是 `permanent` 常驻，实际触发面很小；**如实记在这里**，不在本批修。
3. `BumpWhileInPlayAccum` 是**每次 `RecomposeCore` 调用** +1 —— 生产路径上「每个动作边界」跑的是
   `Auras.RefreshInPlayNumericAndKeywords`（`numericOnly = true`，**也走 `RecomposeCore`** ⇒ 也 +1），
   而棋盘变动那趟是**完整** `Recompose`（再 +1）⇒ **同一动作里若两者都跑，计数会 +2**。
   观感上等价于「原版那次动作的 while-in-play 广播跑了两遍」；`play check` 的实测差异（§7·2）已经把这个口径算进去了。

---

## 八、🔴 撤回（2026-11-05 · 裁定 `✅ R2 对`）

### 8·1 一句话：原结论**为什么错**

**只看了 SO 的 `extrinsic: 0`，漏了 `AbilityLogic.PlayAbility` 的【运行时覆盖】。**
`CardEffect.extrinsic`（`+0x30`）在运行时被重写：
```c
// decomp_full/AbilityLogic__PlayAbility.c:1195-1197
cVar4 = '\x01';
if (*(char *)(param_3 + 0x40) == '\0') { cVar4 = *(char *)(param_3 + 0x41); }   // +0x40 = whileInPlay · +0x41 = whileInPlayVariable
…
*(bool *)(local_res8 + 0x30) = cVar4 != '\0';                                     // ⇒ effect.extrinsic = whileInPlay || whileInPlayVariable
// :1208  if (*(char *)(uVar23 + 0x30) == '\0')  ⇒ **只有 extrinsic 为假**才走那条（合并 / `AddEffect`）路
```
而 `RawCardScript.cs:1270-1277` 对 **trigger 20** 就置 `AbilityData.whileInPlay = 1`
⇒ 这 4 张卡的 while-in-play 效果**运行时恒 `extrinsic = true`** ⇒ 撞上 `AddEffect` 的 `if (!newEffect.extrinsic)` 闸
⇒ **整段不进合并**，第 2/3 次被 `AlreadyContainsEffect:854` / `ContainsExtrinsicEffectFrom` + `AddEffect:11598/11669` 挡掉
⇒ **不累加**。§六 那条「无摘除 ⇒ 会累加」的推理**缺了这一跳**（与 `CLAUDE.md` §三「字段值推不出行为」同一族）。

### 8·2 撤回清单（逐项 · 全部已删，⛔ 未用 `git checkout`）

| # | 文件 | 撤掉了什么 |
|---|---|---|
| 1 | `RuleEngine/Core/Aura.cs` | `AuraSpec.AccumulateOnWhileInPlay` 字段 · `_wipAccum` 计数表 · `AccKey` / `BumpWhileInPlayAccum` / `WhileInPlayAccumCount` / `WhileInPlayAccumCountForTest` / `NoteWhileInPlayBroadcast` · 费用那半 `Delta = -CostLess * n` 改回 `-CostLess` · `ApplyAura` 的 `accumN` 与两处 `* accumN`（属性 / 关键词）改回 |
| 2 | `RuleEngine/Core/CardDef.cs` | `AddAura` 里那 16 行（置真 + `AccumulateWhileInPlayIds` 名单） |
| 3 | `RuleEngine/Core/ActionPump.cs:143` · `RuleCore.cs:2121` · `EffectResolver.cs:9711 / 15858 / 17418` · `TransformUnit_B94B12.cs:274` | 6 处行内前缀 `Auras.NoteWhileInPlayBroadcast(ctx); ` 逐句删（**只删这一句，未碰同文件里的任何其它改动**） |
| 4 | `CardPresentation/Editor/B143_W7_Assert.cs` | **整份删除**（W8 已确认没被接过；`.meta` 未生成过） |

### 8·3 撤回后的验证

| 项 | 结果 |
|---|---|
| 残留扫描 `grep -rn "AccumulateOnWhileInPlay\|NoteWhileInPlayBroadcast\|_wipAccum\|accumN" Assets/**/*.cs` | **0 命中** |
| `typecheck` | **运行时 0 · 编辑器 0** |
| `play check` | **rc=0** · 指纹 `2f640db50876e5630a6a5dd4fcbe82ad` = 基线 **一致** · ①-a 主档 1142 张**逐字节一致** · ①-b 电池 1086 张**逐字节一致** |

### 8·4 将来做这一族时的**正确期望值（恒定量）**

| 卡 | 正确读数（恒定，不随动作数增长） |
|---|---|
| `Aun'Shi`（`TAU32`） | **6 / 5 / 5 / 5** |
| `Piranha`（`TAU20`） | **4 / 3 / 3 / 3** |
| `Grukk Face-Rippa`（`GOF29`） | **3 / 2 / 25** |
| `Land Raider`（`SW38`） | 督军护甲 **只 +2 一次**（`2/0/0/38 → 2/0/2/38`，**不再涨**） |

⛔ **本轮不要另建实现** —— 这四条只作判据留存。

### 8·5 附带订正（铁律 5）

- §6·5 原写 `Land Raider`「`addStats (0/0/0)` 无可观测」—— 已改：它的**光环**是 `Your Warlord has Armour 2`，
  `abilityBuffs` 的 0/0/0 只覆盖**结构化那一行**。
- `RawCardScript__UpdateWhileInPlay.c` 原写「全文只有两句 `TriggerAbilities`」—— **不完整**：
  C 版还有两处 `CardScript__RemovePackBuffs`（`:35` / `:67`）。但那两句**只删 `definedTrait == 0x262`（Pack）、
  且只删「施放者 = 自己」的那些**（`CardScript__RemovePackBuffs.c` 全文），**这 4 张卡都没有 Pack** ⇒ **结论不变**。
- §三 的 240 口径：`abilityTriggers` 含 **240 = 33 张 / 33 次**（每卡至多 1 次）；其中带 `buffType 1/2/10` 的是 **22 张**（子集）。
  `20 ∩ 240 = 0 张` ⇒ 不会被 240 的摘除误伤。
