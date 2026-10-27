# `SOR66 Armorium Cherub` 主动能力（`activeAbility`）判定 —— 技能钮到底做什么

> **执行代理** `R1` · 2026-10-27 第八十七会话 · **只查不改**。
> 上游 = `资料/完全复刻/判据/督军activeAbility入口_判定.md`（**先读它**；本文件补它的 **J5 / J6 / J6'**，并订正其中 2 条）。
> 代码侧现状 = `MyGame/Assets/RuleEngine/Core/CardDefActiveAbility.cs` 文件头（四标量已补，数据待 v35）。
> 一次性脚本留在 `_tmp_view/r1_20261027/`（`extract*.py` / `survey*.py` / `table.py` / `sororitas.py`），复跑即可重生本文件的每张表。

---

## 〇 · 结论（一句话）

**SOR66 的主动能力在原版数据里是完整的两行，不是「缺数据」**：

```text
activeAbilityLogic[0] = chosenAbility 30  (buffOthers)   →  给「玩家点中的那个友方单位」+2 远程，直到回合结束
activeAbilityLogic[1] = chosenAbility 462 (triggerPray)  →  让「自己」进入祈祷（isPraying=1 + 广播 UnitPray）
```

而它之所以在我们引擎里没有入口，**唯一原因**是：**全池 87 张 `hasActiveAbility == 1` 的卡里，只有它身上的 `CardTraits` 不含 `duty / pray / agenda / ferocity / oath` 这五个里的任何一个**（其余 86 张至少有一个）。
我们那三条等价入口（`Ability:` 自定文法 / 替代行动 `alt(duty|pray|ferocity|agenda)` / 誓约 `oath`）**全部以这五个 trait 之一为载体** ⇒ 五缺 ⇒ `skill` 恒假 ⇒ 钮不出现、`UseAbility` 恒 `ErrNoAbility(14)`。

> ⚠️ 这只是**我们入口形状**与**原版入口形状**的差：原版的技能钮是**通用一格**（`CanUseActiveAbility` 里没有任何卡型/关键词要求），不是五条关键词。见 §二·6。

---

## 一 · 三处数据源逐字对过（三处一致，无矛盾）

| 字段 | `cards_engine.json`(v34，现读) | `cards_truth.json`（缩编） | **原始 SO dump** `普查产出_1018/allcards_1350/cards_raw.json`（判据首选） |
|---|---|---|---|
| `hasActiveAbility` | `1` | `1` | `1` |
| `hasAlternativeAbility` | （无此格） | `0` | `0` |
| `manaCost` | 未落列（v35 才出） | `0` | **`0`** |
| `psychicCost` / `plasmaCost` | 未落列 | `0` / `0` | **`0` / `0`** |
| `targeted` | 未落列 | `1` | **`1`** |
| `minHeroLevel` | 未落列 | （未收） | **`0`** |
| `abilitySprite` | 未落列 | （未收） | **`null`**（`m_FileID 0 / m_PathID 0`）—— 图标是**按阵营**取的（`ArmyUtilities.GetActiveAbilityIconForArmy`），不是按卡 |
| `needsLoading` / `playAnim` / `resolutionChoosingCount` | 未落列 | （未收） | `0 / 0 / 0` |
| 拾取条件 `targetCriteria` | `activeAbilityPickCriteria[0].criteria` = `targetsAffected 10, friendlyUnits 1, includeHero 1, includeMinions 1, includeSelf 1`（其余 0） | 同（`targetsAffected 10, friendlyUnits 1, includeHero 1, includeMinions 1, includeSelf 1`） | 同 |
| 逻辑行 | `activeAbilityLogic` 1 个对象 / `gate:1` / `ca:[30,462]` / `ta:[30,20]` / `fm:[1,1]` / `dmg:[{1,1},{1,1}]` | `activeAbility.logic` **2 行**（`chosenAbility 30` / `462`） | `activeAbility.activeAbilityLogic` **2 行**（全字段见 §二·2、§二·3） |
| AI 打分 | 未落列 | （未收） | `cardTargetScoringCriteria []` · **`nonTargetScoringCriteria` 1 条**（`scoringOption 2`, `multiplyByFriend 1.0`, 目标 = 友方单位） |

**读数脚本**：`python -I -X utf8 d:/4/_tmp_view/r1_20261027/extract_raw3.py`（SO 全字段）/ `extract2.py`（卡表）/ `survey.py`（全池）。

### 1·1 三个「形状陷阱」（⛔ 别读错）

1. **`cards_engine.activeAbilityLogic[0].trigger = 400` 不是这张卡的某个字段** —— 它是**运行时实参**：原版逐行调
   `AbilityLogic__PlayAbility(logic, bm, abilityData, 400, 0)`（`d:/2/tools/decomp_full/RawCardScript__ResolveActiveAbility.c:65`），
   而 `AbilityTrigger 400 = ThisUnitAbilityUsed`（`d:/2/tools/il2cpp_out/dump.cs:20285`）。
   ⛔ **别按 `AbilityEffect` 的表去读成 `triggerCourtesy = 400`**（两张 enum 撞了同一个数）。
2. **`dmg: [{min:1,max:1},{min:1,max:1}]` 不是「造成 1 点伤害」** —— 那是 SO 里 `damageCriteria` 的初值，两行用的 op
   （`buffOthers` / `triggerPray`）**根本不读它**。同族：`pa:[0,0]` = `playersAffected 0 (Both)` 也未被读。
3. **`cards_engine` 的 `playerTurn: -1` 是生成器补的** —— 原始 SO 的 `ActiveAbility.targetCriteria` **连这个键都没有**
   （实测 keys 里没有 `playerTurn`；只有 `BoardCriteria.countCriteria` 才有）⇒ ⛔ 别拿 `-1` 当判据。

---

## 二 · 逐条证据：这两行在原版分别做什么

### 2·1 这两行是被谁、以什么顺序执行的

| # | 事实 | 判据 |
|---|---|---|
| ① | 入口是**通用一格**：`BattleManager.TryUsingActiveAbility` 取「当前选中的卡」(manager+0x410) → `CanUseActiveAbility(card, playerIdx, 1, 1)` | `BattleManager__TryUsingActiveAbility.c:24` |
| ② | 目标型（`targeted=1`）走 `StartChoosingAbilityTargetByClick`，全场合法目标点亮 | `BattleManager__TryUsingActiveAbility.c:36-44` · `BattleManager__StartTargetedActiveAbility.c:59` |
| ③ | `targeted` 的读点 = `activeAbility+0x20` | `CardScript__IsActiveAbilityTargeted.c:26` · 字段偏移 `dump.cs:23756` |
| ④ | 发动后走 `CardScript.ResolveActiveAbilityPlayed` → `RawCardScript.ResolveActiveAbility` → **逐行** `PlayAbility(logic, bm, abilityData, 400, 0)` | `RawCardScript__ResolveActiveAbility.c:58-67` |
| ⑤ | 逐行时 `this.targetCriteria` 决定这一行打谁：`GetTargets(manager, logic.targetCriteria(+0x28), abilityData)` | `AbilityLogic__PlayAbility.c:262` |

### 2·2 第 0 行 = `buffOthers`（`AbilityEffect 30`，`dump.cs:20656`）

| 事实 | 判据 |
|---|---|
| 命中 `chosenAbility == 30` 或 `31` 那一支 | `AbilityLogic__PlayAbility.c:1179`（`if (1 < (uint)(chosenAbility - 0x1e)) return;`） |
| 对**每个 target** 各复制一份 `logic.buffEffect(+0x50)`，然后 `BattleManager.AddEffect(manager, target, effect, …)` | `AbilityLogic__PlayAbility.c:1182-1234`（`AddEffect` 在 `:1218`） |
| target 从 `logic.targetCriteria` 来，SOR66 这一行 = `targetsAffected 30` = **`target`**（`TargetsAffected.target = 30`，`dump.cs:20784`）⇒ 就是**玩家点中的那一个** | 同上 + `dump.cs:20784` |
| buff 本体：`buffType 1` = `addStats`（`dump.cs:45435`）· `rangeAttackChange 2` · `untilEndOfTurn 1` ⇒ **+2 远程，到回合结束** | SO 字段 `activeAbilityLogic[0].buffEffect` |

> 📌 **这一行与它自己卡面上的 `Rally:` 是同一个效果** —— 事实上 `activeAbilityLogic[0]` 与
> `cardAbilities[0].abilityLogic[0]`（`trigger 0 = ThisCardPlayed`）**逐字段完全相同**（含两条 `cardAnimations` GUID，
> 甚至含 `summonCriteria.unitsToSummon = [自己 path_id]`）。见 §五·2「判不清的那一点」。

### 2·3 第 1 行 = `triggerPray`（`AbilityEffect 462`，`dump.cs:20724`）

| 事实 | 判据 |
|---|---|
| 命中 `case 0x1ce` → 对每个 target 调 `BattleManager.AddTriggerPray(manager, target, thisCard, …, 1, 0)` | `AbilityLogic__PlayAbility.c:1838-1853` |
| 这一行的 target = `targetsAffected 20` = **`self`**（`dump.cs:20783`）⇒ 祈祷的是**它自己**，不是玩家点的那个人 | SO 字段 |
| `ResolveTriggerPray` 实际做三件事（顺序固定）：<br>① `RawCardScript.OnTrigger(raw, 0x28f = 655 = AbilityTrigger.Pray, …)` → 触发它自己的 `Pray`(655) 被动<br>② `set_isPraying(card, 1)` → **进入祈祷状态**<br>③ `BroadcastUnitPray(manager, card)` → 广播「有单位祈祷」 | `BattleManager__ResolveTriggerPray.c:217` / `:266` / `:314`（`dump.cs:20318` 655=Pray） |
| ⚠️ SOR66 自身 **没有 655 行**（`cardAbilities` 的 trigger 只有 `0` 与 `30`）⇒ ①这一步在它身上**空转** | `cards_raw.json` 的 `SOR66.cardAbilities` |

> 🔴 **推论 A（要写进施工单）**：`triggerPray` 里**不含**「挂 `Pray Enchantment`」。
> 那张附魔是**卡自己的 `cardAbilities[trigger=655].abilityLogic[0].buffEffect{buffType:15}`** 挂的
> （我方注释已记：`RuleCore.cs` 的 `Enchantments.AttachPray` 那一段），SOR66 没有 655 行 ⇒ **原版不会给它挂附魔**。

### 2·4 费用闸（补齐 J7 里「两个方法体还没读」那一格）

| 事实 | 判据 |
|---|---|
| 费用只在 `cardState == 2 (inPlay)` 时查：`PlayerManager.HasEnoughMana(玩家, 卡, AbilityUsesPsychicEnergy, AbilityUsesPlasmaEnergy)` | `BattleManager__CanUseActiveAbility.c:60-70`（`HasEnoughMana` 在 `:68`） |
| `AbilityUsesPsychicEnergy` = 读 `activeAbility+0x28`（`psychicCost`）；`AbilityUsesPlasmaEnergy` = `+0x29`（`plasmaCost`） | `EntityScript__AbilityUsesPsychicEnergy.c:24` · `...PlasmaEnergy.c:24` · 偏移 `dump.cs:23760-23761` |
| SOR66 四项费用全 0 ⇒ **免费**，不会卡在费用上 | §一 表 |
| 其余闸：必须是自己的回合 · `manager+0x244/0x246` 为真 · `card+0x230` (`canAct`) 为真 · **`cardState == 2 (inPlay)`**（`+0x228`，别的状态直接 `Log` + 返回 0）· 教程白名单 · `manager+0x418`（**「主动能力流程进行中」标志**，`TryUsingActiveAbility` 一开始就置 1）为假 · 有 `duty`(1220) trait 且本回合已用过 ⇒ 拒 · 有 `oath`(1275) trait ⇒ 还要 `CanUseOathAbility` | `BattleManager__CanUseActiveAbility.c:36-105` · `BattleManager__TryUsingActiveAbility.c:29/41` · `dump.cs:45843/45854` |

### 2·5 一次发动会消耗这个单位的行动

`CardScript.ResolveActiveAbilityPlayed` 第 1 句就 `usedActiveAbilityTimesPerTurn += 1`、`usedActiveAbility = true`
（`CardScript.cs:14764-14768`）。⇒ 与我们 `UseAlternativeCore` 里 `u.Exhausted = true; u.AttacksThisTurn++;` 同一语义。
⚠️ 但 `pray` 那一支**不置** `+0x4C`（我方 `RuleEngine/Core/RuleCore.cs:12005` 那行 `u.ActiveAbilityUsed |= keyword != KeywordTable.Pray;` 的判据仍然成立）。

### 2·6 原版的入口**不是**五条关键词 —— 这是我们自己的等价形状

`CanUseActiveAbility` 里**没有任何卡型/关键词要求**（只有 §2·4 那几条闸；`duty`/`oath` 只**额外收紧**）。
⇒ 87 张 `hasActiveAbility=1` 的卡在原版**都**有这一格。我们今天的 `skill = HasAbility || alt || oath`
（`BattleDriver.cs:15085-15087`）是**三条等价入口**拼出来的一格 —— 这一点本身是既有设计（`CardDefActiveAbility.cs` 文件头已记录），本文件不改它，只指出 **SOR66 是这拼法漏掉的那一张**。

---

## 三 · 与另外 86 张的对照表（数据侧）

### 3·1 全池口径（现读，`table.py` 可重生）

| 口径 | 值 |
|---|---|
| 全池卡数（`cards_engine.json.count`） | **1128** |
| `hasActiveAbility == 1`（池内） | **87**（`cardType 10 = 督军 11` · `cardType 0 = 部队 76`） |
| 原始 SO 全表 `hasActiveAbility == 1` | **88**（多 1 张 `66486b3f61682ab42ba2ce0fa921002f` Tempestus Scion，**不在池内**） |
| `manaCost` 分布 | **87 张全是 `0`** |
| `psychicCost` / `plasmaCost` | **87 张全是 `0`** |
| `targeted` 分布 | **`1` × 75 · `0` × 12** |
| `activeAbilityLogic` 行数 | 全部 ≥1（`nLogic==0` = **0 张**） |
| 末行 `chosenAbility` 分布 | `triggerPray 24` · `doDamage 18` · `buffOthers 12` · `buffMana 8` · `buffSelf 8` · `summonUnit 5` · `stun 3` · 其余各 1–2 |

### 3·2 「我们能入口」的判据 = 身上有没有那五个 trait

`RuleCore.CanUseAlternative` 的承载物是 `HasFx(keyword)`（卡面正文解析出来的 op 表），而**卡面正文之所以有 op，是因为卡上有那个 trait**
（`duty 1220` / `pray 1185` / `agenda 1235` / `ferocity 1265` / `oath 1275`，`dump.cs:45839-45854`）。
逐张扫 `CardTraits`：

| 卡 | 五个 trait 命中的 | 结果 |
|---|---|---|
| **SOR66 Armorium Cherub** | **`[]`**（traits = `flying`, `rally`） | ❌ **唯一一张，五缺** |
| 其余 **86** 张 | 至少一个（部分卡卡面没有 `Pray:` 那种**前缀**，但 trait 在：`SOR13` = `pray`、`SW4/SW11/SW21` = `ferocity`、`DA18/DA29/DA1/DA71` = `agenda`、`AM5/AM11/AM16/AM23/AM66` = `duty`） | ✅ 有入口 |

> 🔑 **这就是「到底哪一处不同」的准确答案**：不是 `activeAbilityLogic` 缺行（它有 2 行，与同族一样多），
> 不是四个标量为 0（**全池 87 张都是 0**），也不是 `targeted`（75/87 都是 1）。**差别只在 trait 清单**。

### 3·3 同阵营（Sororitas，`cardArmy = 80`）横向对照 —— 印证 §二 的解码

Sororitas 的主动能力形状 = **[卡面 `Pray:` 那一句的效果行] + [triggerPray]**，逐张与卡面文字一一对上（**解码正确的强证据**）：

| 卡 | 卡面 | `activeAbilityLogic` 的 `chosenAbility` | 对得上吗 |
|---|---|---|---|
| `SOR6` Battle Sister Gunner | `Pray: Deal 2 damage to an enemy` | `doDamage` → `triggerPray` | ✅ |
| `SOR30` Celestian Sacresant | `Pray: Heal 3 to a friendly unit` | `doHeal` → `triggerPray` | ✅ |
| `SOR69` Zephyrim Superior | `Pray: Draw a troop` | `drawCard` → `triggerPray` | ✅ |
| `SOR5` Battle Sister | `Pray: Gain +1 [Attack] and +1 [Ranged] and heal 1` | `buffSelf` → `doHeal` → `triggerPray` | ✅ |
| `SOR25` Imagifier | `Pray: Give +1 [Attack], +1 [Ranged] and Flank to a friendly troop` | `buffOthers` → `buffOthers` → `triggerPray` | ✅ |
| `SOR35` Immolator | `Pray: Deal 3 damage to an enemy and its adjacent units` | `doDamage` → `doDamage` → `triggerPray` | ✅ |
| `SOR13` Simulacrum Bearer | （无 `Pray:` 前缀）`Give +1 [Attack] to your troops, and to your Warlord this turn` | `buffOthers` → `buffOthers` → `triggerPray` | ✅（**先例：卡面不写 `Pray:` 也照样有主动能力**） |
| `SOR65` Celestian Sacresant Aveline | `Pray: Give +1 [attack] and +1 [ranged] to a friendly troop. Talent: …` | `buffOthers` → `triggerPray` | ✅ |
| **`SOR66` Armorium Cherub** | **`Flying. Rally: Give +2 [Ranged] to a friendly unit this turn. At the start of your turn, …`** | **`buffOthers`（= 与 `Rally` 同一份）→ `triggerPray`** | ⚠️ **卡面只写了 `Rally`，没给这颗钮任何文字** |

**全池含 `triggerPray` 的卡 = 31 张**（`cardArmy 80` = 28 张 + `cardArmy 90` = 3 张）；**池内 `hasActiveAbility=1` 的 24 张**里，23 张卡面写的是 `Pray:` 那一支，**只有 SOR66 不是**。

---

## 四 · 施工建议（分两段，第一段零风险，第二段是真修）

### 4·1 数据段（v35 四列）—— 与 J7 / R1 同一条，**不是**本文件的决定

**改哪个文件**：`工具/gen_cards_engine.py`
**改成什么**：对每张 `hasActiveAbility == 1` 的卡，从 `cards_raw.json` 的 `activeAbility` 对象取四格落列
`activeAbilityManaCost` / `activeAbilityPsychicCost` / `activeAbilityPlasmaCost` / `activeAbilityTargeted`。

**SOR66 的四格真值（现读）**：**`0 / 0 / 0 / 1`**。
（全池 87 张：`manaCost` 全 `0`、`psychic`/`plasma` 全 `0`、`targeted` = 75 张 `1` / 12 张 `0` ⇒ 这四列**今天不会**把 87 张区分开。）

**怎么验**：①`bash 工具/typecheck.sh`；②重生成后 `python -I -X utf8 -c` 读回四格对 `cards_truth.json` 逐张比（应 87/87 一致）；
③`bash 工具/ruleprobe.sh check`（四列今天**无消费者** ⇒ 指纹必须**不变**；变了就是接错线）。

### 4·2 入口段（真修）—— ⛔ 别走「把它当 pray 卡」那条捷径

**为什么不走捷径**：最省事的想法是给 SOR66 造一条 `Pray:` 正文，让它走 `RuleCore.UseAlternative(…, "pray")`。**这条路会做错一处**：
`UseAlternativeCore` 在 `keyword == pray` 那一支**无条件**调 `Enchantments.AttachPray(ctx, u)`
（`RuleEngine/Core/RuleCore.cs:12076`），而原版那张 `Pray Enchantment` 是**卡自己的 `cardAbilities[trigger=655]`**
挂的（该段注释自己写着「23 张 pray 卡的 `cardAbilities[trigger=655].abilityLogic[0].buffEffect{buffType:15}`」）。
**SOR66 没有 655 行** ⇒ 走 pray 会**多挂一张原版不会挂的附魔** = 静默做多。
（另外一个次要理由：造 `Pray:` 正文会改**卡面显示**的文字，那是玩家看得见的东西。）

**改哪个文件**：新增 `RuleEngine/Core/RuleCore.cs` 的一条 `UseActiveAbility(ctx, p, slot, targetSlot)`
（数据源用**已有**的 `CardDef.ActiveAbilityLogicGroup(0)` / `CardDef.HasActiveAbility`，v34 的 `gate=1` 行已经在了，**不用改卡表形状**）；
表现层那三处（`BattleDriver.OpenCommand` 的 `skill` / `ActiveActionText` / 按钮目标侧判定）**只需把这一条并进 `skill` 那一格**。

**改成什么**（SOR66 只需要两个 op，都不新发明）：

| 行 | 执行 |
|---|---|
| `chosenAbility 30 buffOthers` + `buffEffect{buffType 1 addStats, rangeAttackChange 2, untilEndOfTurn 1}` + `targetCriteria{targetsAffected 30 target}` | 对**玩家点中的那一个友方单位**上一条 `+2 远程 / 到回合结束` 的 buff（我们现有正文层对 `Rally:` 已经有这条 op，直接复用） |
| `chosenAbility 462 triggerPray` + `targetCriteria{targetsAffected 20 self}` | 等价于 `u.Prayed = true` + `FireTriggerAt(ctx, u, "pray", …)` + `BroadcastWhen(Prays)`；**不**调 `Enchantments.AttachPray`（§2·3 推论 A） |

**闸**（照 `BattleManager__CanUseActiveAbility.c`）：`CardDef.HasActiveAbility == 1` · 自己回合 · `!u.Exhausted` · `!u.IsStunned` ·
`duty` 用过一次就不给（`u.DutyUsed`）· 有 `oath` 时还要 `CanUseOathAbility`。**⛔ 不要加卡型/关键词要求**（§2·6）。
动作经济：`u.Exhausted = true; u.AttacksThisTurn++;`（§2·5），`u.ActiveAbilityUsed` **这一支要置真**（它是主动能力本体，不是 `pray` 关键词那一支）。

**怎么验**（验收口 = 命令 + 断言）：

1. `bash d:/4/Unity/工具/ruleprobe.sh play SOR66`
   —— `steps` 里应新增一格（建议命名 `aa=<code>@tgt<slot>`，与既有 `abil=` / `alt(...)` / `oath=` 并列）。
   **三条硬断言**（缺一条就等于没做对）：① 目标单位 `RangeAttack` **+2**；② 发动者 `u.Prayed == true`；③ 发动者 `u.Exhausted == true`。
2. `bash d:/4/Unity/工具/ruleprobe.sh play all dump <out.tsv>` + `pf` —— 全池逐卡真打。
   ⚠️ **这一支会让结算指纹变**（多了一条能发的主动能力）⇒ **必须排在「没有 `.cs` 写手 + 补丁已合并完」的重钉窗口里**做。
3. `bash d:/4/Unity/工具/typecheck.sh`（改 `.cs` 后立刻）。
4. 若要一并修其他 ``chosenAbility``（J8 那一整台执行器），按 §3·1 的频率表从高到低收（`triggerPray 24` → `doDamage 18` → `buffOthers 12` → …），一张卡一条断言。

---

## 五 · 还查不清 / 需要裁定

1. **原版实况里这颗钮到底出不出现 —— 没跑到实况。**
   数据侧四条正面信号都指向「有」：`hasActiveAbility=1` · `activeAbilityLogic` 两行齐 · `targeted=1` · `nonTargetScoringCriteria` **有内容**
   （`scoringOption 2`、`multiplyByFriend 1.0`、目标 = 友方单位 —— 是**按「给友方上 buff」专门写的**，不是空模板）。
   反面信号只有一条：**卡面正文一个字都没提这颗钮**（`Rally:` / 回合开始那两句都与它无关）。
   ⇒ 建议**真机点一次 SOR66** 拍板（受真 Play 窗口限制，本轮做不到）。
2. **`activeAbilityLogic[0]` 与 `cardAbilities[0].abilityLogic[0]` 逐字段完全相同 —— 分不清「有意复用」还是「复制粘贴」。**
   两者都带着 `summonCriteria.unitsToSummon = [SOR66 自己的 path_id]` 与同两条 `cardAnimations` GUID（Unity 复制对象图会留这种自引用痕迹）。
   全池 **143** 张有 `activeAbilityLogic` 行的卡里，**只有 2 张**出现「第 0 行与自身某条 cardAbility 行完全同」：`SOR66` 与 `SOR34`
   （`SOR34` 那份对应的是它自己的 `6☀: Deploy a Celestian`，与它 `Pray: Deploy a Celestian` 同效 ⇒ 天然同份，不算异常）。
   ⇒ **倾向「有意」**（AI 打分那一格是对着「给友方上 buff」写的），但**数据上判不死**。
3. **原版对「非督军的技能钮」有没有 UI 层的额外限制**：未查。`CanUseActiveAbility` 里没有卡型判断；
   `ShowWarlordActiveAbilityForTutorial` 这个方法名带 `Warlord`，但它的体只是 `hasActiveAbility = raw.hasActiveAbility;`，
   看不出卡型闸。⇒ 若真机点不出钮，这一格就是下一个要查的地方。
4. **`HasEnoughMana` 里 `manaCost` 到底从哪读**（`ActiveAbility.manaCost` 还是卡上的 `currentAbilityCost`）：`PlayerManager__HasEnoughMana` 的方法体**本轮没读**。
   SOR66 费用全 0 不受影响，但将来接费用闸必须先读它。

---

## 六 · 就地订正（`督军activeAbility入口_判定.md` 的两条 —— 请调度台并回正本）

> 本代理白名单只许写本文件，**没有**改上游那份 ⇒ 两条订正记在这里，**等调度台合并**。

1. **J6' 的「`cards_truth.json` 现读 143 张有对象但 **0 张带行**」—— 不成立。**
   现读：`cards_truth.json` 里 `activeAbility` 非空 **143** 张，**143 张全部带 `logic` 行**（`>1` 行的也不少）。
   `SOR66` 那一格就有 2 行（`chosenAbility 30` / `462`）。143 = 池内 87 + 池外/`hasActiveAbility=0` 的 55 + 1 张池外 hasAA=1。
   ⇒ 「拿 `cards_truth` 当『这张卡没有主动能力行』」这个提醒**仍然有效**（它的行确实比 SO **缩编**：掉了 `playersAffected` / `cardAnimations` / `mustHaveCasterInPlay` 等），
   但**理由不是「0 张带行」**，而是「行在、字段不全」⇒ 要全字段仍然只能读 `cards_raw.json`。
2. **「`+0x220` 由卡数据灌，卡型 ≠ Hero 时压成 false（督军除外）」这句被引错了两处 —— 那是 `Jam` 效果的体，不是初始化。**
   - 真正的初始化：`CardScript.SetBaseCardTraits()`（`out_cpp2il_dev_ilspy/CardScript.cs:11425`）里
     **`:11436 hasActiveAbility__BackingField = rawCardScript.hasActiveAbility;`** —— **无条件，不看 cardType**。
   - 被误引的那三行（`CardScript.cs:20090-20093`）在 **`ReactToUnitJammed(CardScript disenchantedCard)`**（方法从 `:19980` 起）体内，
     上一句是 `CardTraitData(DefinedTrait.jam)` 加进 `CurrentTraits` ⇒ 语义是「**被 Jam 的非督军卡丢掉主动能力**」，**不是**「非督军天生没有」。
   - ⛔ 请一并核 `UnitState.cs:2180` / `:2200` / `:2202` 三处注释（它们抄的正是这句）。
   - 影响：把「77 张部队卡的 `hasActiveAbility=1`」判成假的会让它们全都无法发动 —— 与原版 `SetBaseCardTraits` 的读法相反。

---

## 附 · 三条「别推翻」

1. ⛔ **别拿 `dmg: {min 1, max 1}` 给 SOR66 加伤害** —— 那是 `damageCriteria` 初值，两行 op 都不读它（§1·1-②）。
2. ⛔ **别给它造一条 `Pray:` 正文让捷径成立** —— 会多挂一张 `Pray Enchantment`（§4·2 理由）。
3. ⛔ **别因为「卡面没写这颗钮」就判「不做」** —— 数据侧四条正面信号 + 铁律 11；判死只能靠真机实测（§五·1）。
