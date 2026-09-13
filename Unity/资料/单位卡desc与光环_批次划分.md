# 单位卡 desc 缺口 + 光环族：批次划分（2026-09-14）

> 子代理只读普查的产物，**给下一个会话照着排批次**。
> 权威清单仍是 **`_tmp_view/unit_desc_unparsed.txt`**（自检每次重写）—— **数字别抄这里**。
> ⚠️ 这份是**分类与优先级**，不是「已做完」。

---

## 一、结论先行（三件改变批次的事）

### ① 光环 **29 张**，而设计稿对它**零覆盖**
`光环 10 张 + 同族「己方全体 have Y」19 张 = 29 张`，**是同一件事**
（筛选维度从「相邻」换成兵种 / 关键词 / 敌方），**该一起做**。

🔴 但 `资料/常驻效果_数据与设计.md` 全文检索「光环 / 相邻 / adjacent」= **0 命中** ——
它覆盖的是**另三支**：回合起止（§一/§二）、部署时给（§六/§七）、持续改费。
**部署时给 ≠ 光环**：前者是**事件**驱动（一次性 `give`），后者是**读时**驱动。
⇒ **A7 不是「照已有设计做」，得先给它新开一节。**

### ② 交接文档点名的两份反编译，**第二份不是光环**
- ✅ `CardScript__HasWhileInPlayAdjacentEffect.c` —— 是个**谓词**
  （`HasCurrentTrait(0x2f8)` 或「有一条 WhileInPlay 能力、其 affected 列表第一项 == 100 = `Self`」）。
  ⚠️ **在 1800 个 `.c` 里没有任何调用者** —— 「原版拿它干什么」**查不到**。
- ❌ `BattleManager__ShouldTriggerAdjacentDamage.c` —— 整个方法体是
  `return 0 < EntityScript.get_CurrentBlast(target)`，**那是爆裂（Blast）的判据**，
  我们**已经实现**（`RuleCore.cs:1213`）。**别把它当光环证据。**
- 旁证：`CardScript__HasWhileInPlayAbility.c` 认 ability type `0x14`/`0xf0` + trait `0x82`，
  `BattleManagerSupport` 的 48 个广播里有独立的 `WhileInPlay(...)` ⇒
  **原版确有一条「在场时」通道，和部署那条是两条。**

### ③ 最大的两条**是假缺口**（一行代码都不用写）
`Talent: <名>` **35 次** + `Companion N: <名>` **8 次** = **43 次**，
自检缺的是**白名单**，不是 handler —— 两条路**都已实现**：
`CardDef.TalentName` + `RuleCore.SpawnTalents`（规则书 `:218`）·
`CardDef.CompanionName` + `RuleCore.PlayCard`。
⇒ **把这两族放进自检白名单**，别再去实现一遍。

---

## 二、单位卡 desc 缺口：按**句型族**归类（族 ≈ 20 ⇒ 4 个可做批次 + 3 条挂起）

| 族（句型模板） | 卡数 | 例卡 | 同一个机制？ | 现成的层 | 建议 |
|---|---|---|---|---|---|
| `Talent: <名>` / `[Talent]: <名>` | 31 种 / 35 次 | `Reanimate` · `Serene Unifier` | ❌ **已实现** | ✅ `CardDef.TalentName` + `RuleCore.SpawnTalents` | **不做** → 进自检白名单 |
| `Companion N: <卡名>` | 7 种 / 8 次 | `Marker Drone` · `Gun Drone` | ❌ **已实现** | ✅ `CardDef.CompanionName` + `RuleCore.PlayCard` | **不做** → 同上 |
| `When <事件>, deal N damage to a random enemy` | 13 种 / 13 次 | `When a friendly unit Prays…` | ✅ 事件→伤害，**事件词表已全** | ✅ `WhenEventKind` + `BroadcastWhen` + `ReDeal` | **批 1** |
| `When <事件>, give/heal <目标>` | 8 种 / 8 次 | `When a friendly unit prays, give it Armour 1` | ✅ 同上一族，载荷换成 give/heal | ✅ `ReGive` / `ReHeal` | **批 1**（同一笔） |
| `Heals N` / `Heal N`（**无目标 = 自己**） | 7 种 / 7 次 | `Strike: Heals 4` · `Codex: Heal 1` | ✅ 隐式主语自愈 | ⚠️ 半有：缺「无目标 ⇒ 自己」那一支 | **批 1**（最便宜） |
| 部署时给 `When you deploy/play a <X>, give it <Y>` | 9 种 / 10 次 | `Antaro Chronus` · `Eliminator Sergeant` | ✅ 部署事件 + 卡面筛选 | ✅ **已建好**：`EffectTargetSpec.Deployed` + `ResolveDeploy` + `CardCriteria` | **批 1**（设计稿说「解封直接用」） |
| 纯关键词声明带后缀/括号 | 3 种 / 4 次 | `Stealth (1)` · `Has Flying during your turn` | ✅ | ⚠️ `IsKeywordOnly` 在，`(1)` / `during your turn` 剥不掉 | **批 1（顺手）** |
| `At the start/end of **each** turn, …` | 3 种 / 3 次 | `At the end of each turn, deal 1 damage…` | ✅ **`each turn` ≠ `your turn`** | ⚠️ `TryAtTurn` / `RePersistAtTurn` 只认 `your turn` | **批 2** |
| `When …, deploy/create <卡>` | 6 种 / 6 次 | `When an enemy attacks, deploy a Spore Mine` | ✅ 事件 + 造牌 | ✅ 大半有：`ReDeploy`/`ReCreate` + `CreatesSecret`/`CreatesSabotage` | **批 2** |
| `After receiving a Dark Pact, …`（**无 When 前缀**） | 2 种 / 2 次 | `After receiving a Dark Pact, deal 3 damage…` | ✅ 上一族变体 | ⚠️ `GetsDarkPact` 已有，缺「无 When」这一支 | **批 2** |
| `Takes N damage at the start of your turn`（反语序） | 1 种 / 1 次 | 同左 | 同 `each turn` 族 | ⚠️ `ReTakeDamage` 上轮已加，差与 `AtTurn` 复合 | **批 2** |
| 🔴 **光环 `Adjacent units/troops have X`** | 8 种 / 9 次 | `Baneblade Tank` · `Makari the Grot` | ✅ | ❌ **无** | **A7 本批** |
| 🔴 **己方全体光环 `Friendly/Your other/Other friendly X have Y` + `Enemies have Y`** | 19 种 / 19 次 | `Cadre Fireblade` · `Triarch Stalker` | ✅ **和上一行同一件事** | ❌ 无 | **A7 本批（同一笔）** |
| 前缀触发体（`Rally:`/`Backlash:`/`Agenda:`/`Codex:`）载荷怪 | 6 种 / 6 次 | `Backlash: Returns to your hand and costs 2 more` | ❌ 不是一族，载荷动词各异 | ⚠️ `ReReturn`/`ReDraw` 都在；差 `Returns` / `the next Stratagem in your deck` 这类变体 | **批 3（逐条）** |
| 「静态改战斗规则」 | 5 种 / 5 次 | `Any attack against your Warlord targets this troop instead` | ✅ 读时改战斗判据 | ⚠️ 半有：`FieldAttack` + `IsValidTarget` | **批 4** |
| 督军专有：裸专名 / `Start the game with X in hand` | 5 种 / 5 次 | `Ethereal Supreme` · `Chosen of the Four` | ❌ 是**开局长效** | ❌ 无 | **批 4** |
| 「改别的机制的规则」 | 5 种 / 5 次 | `When a friendly unit triggers Mob, it triggers an additional time` | ✅ 改**别的机制**的次数 | ❌ 无 | ⛔ **挂起** |
| `Ecstasy N: <正文>` | 7 种 / 8 次 | `Ecstasy 5: Double this troop's [Melee] and [Ranged]` | ✅ 血量阈值触发 | ❌ **`ecstasy` 是故意没机制的三个关键词之一** | ⛔ **挂起**（先补 `ecstasy`） |
| 事件触发式降费 | 3 种 / 3 次 | `Lower cost by 2 every time a friendly unit triggers Synapse` | ✅ **一整层** | ❌ 无 | ⛔ **挂起**（设计稿明说「别顺手，单独一轮」） |
| 坏数据 | 1 种 / 1 次 | `3 Gain` | —— 不是句子，是切分残渣 | —— | ⛔ **退回数据侧** |

**批 1 覆盖了 `[unit]` ① 栏 146 次里的约 90 次** —— 先做它。

⚠️ **[hero] 是完全解析 0/56，那是结构性缺口** —— 督军卡 desc 整条没走通，
**不是「几句认不出」**。表里 hero 的 `Talent:` / 裸专名那几行会被一直误当句子问题
⇒ **建议和 [unit] 的逐句缺口分开算账。**

---

## 三、光环族逐卡（29 张）

### 3a 「相邻」型（10 张，筛选维度 = 相邻格）

| 卡（阵营） | 卡面原文 | 给谁加什么 |
|---|---|---|
| `Baneblade Tank`（AM） | `Armour 2. Adjacent units have Armour 1. Duty: Deal 8 damage` | 相邻格 +1 护甲 |
| `Genestealer Familiar`（GS） | `Adjacent units have +1. Rally: Give Flank to a friendly Infantry.` | ⚠️ **`+1 什么不明`**（原文残缺、无属性词）**语义待查** |
| `Damaged Plasmacyte`（Sautekh） | `Adjacent units have Regeneration 2` | 关键词型 |
| `Nemesor Zahndrekh`（Sautekh·hero） | `Adjacent Remnants do not disappear at the end of your turn. Talent: Relentless March` | 🔴 **不加属性 —— 改残骸寿命**（和其余 9 张**不是同一个 handler**） |
| `Banner Nob`（Goff） | `Adjacent units have +1 [attack] and +1 [weapon]` | 相邻 +1 近战 +1 武器 |
| `Makari the Grot`（Goff） | `Adjacent units have +1 Attack. Backlash: Returns to your hand…` | 相邻 +1 近战 |
| `Wolf Guard Battle Leader`（SW） | `Adjacent units have Pack.` | 关键词型 |
| `Enforcer Battlesuit`（Tau） | `Vanguard. Adjacent units have +1 Ranged Attack.` | 相邻 +1 远程 |
| `Honoured Ethereal`（Tau） | `Shield. Adjacent **troops** have Vanguard.` | 关键词型 + **兵种筛** |
| `Honour Guard`（UM） | `Adjacent units have Armour 1` | 相邻 +1 护甲 |

### 3b 「己方全体」型（19 张，筛选维度 = 兵种 / 关键词 / 排除自己）

| 类型 | 卡 | 卡面 |
|---|---|---|
| **排除自己** | `Cadre Fireblade`（Tau）· `Triarch Stalker`（Sautekh） | `Your other Infantry and Battlesuit troops have +2` · `Your other units have +2 Ranged Attack` |
| **带筛选** | `Company Ancient` · `Bladeguard Ancient`（UM）· `Winged Autarch`（ASH） | `Your other units have +1 Melee and +1 Ranged Attack` · `…+2 [strength]` · `Your other Flying units have +2 Melee and +2 Ranged Attack` |
| **关键词型（5 张同一句法）** | `Iron Priest`（SW）· `Ravenwing Talonmaster`（DA）· `Devilfish`（Tau）· `Vitus Gryf`（AM）· `Chimera`（AM） | `Friendly Vehicles have Regeneration 1.` · `…Flank.` · `…Flank.` · `Friendly troops with Flying have Flank.` · `Friendly Infantry troops have Armour 1.` |
| **带条件/带值** | `Fyrri Askar`（SW）· `Whirlwind`（UM） | `Friendly units with Pack have Invulnerable during your turn` · `Other friendly units have Sentry 2` |
| **敌方全体 / 小类** | `Bringer of decay`（BL）· `Rugged Disharmonist`（EC）· `Nob on Smasha Squig`（Goff）· `Grukk Face-Rippa`（Goff）· `Ramatekh The Cruel`（Sautekh）· `Alluress`（EC）· `Winged Daemon Prince`（EC） | `Enemies have Vulnerable 1` · `Enemy troops have -2 Attack` · `Other friendly Beasts have +1 [attack] and +1 [health].` · `Your troops with Tide have +1 Attack.` · `Your other Destroyers have +1 [Attack].` · `Friendly Daemonette have Flank.` · `Other friendly Daemons cost 2 less and have +2 [attack]`（**费用+属性合体，唯一一张**） |

---

## 四、A7 动手前必须定的事（都在只读范围内查到的）

1. **引擎里没有「常驻光环层」**。`ctx.PersistentEffects` 是**事件驱动**的
   （`Trigger="turn"|"deploy"`，登记后**没有移除路径**，也不问「来源还在不在场上」）
   ⇒ 直接拿它做光环 = **违反纪律①（光环是持续效果，人走效果没）**。
2. **该照「读时现算」的形状做**，仓里有三个先例：
   `RuleCore.FieldAttack`（**所有攻击力修正的唯一出口** —— `+N Attack` 光环的天然落点）·
   `RuleCore.DamageAfterReduction`（护甲唯一公式 —— `Armour 1` 光环的落点）·
   `RuleCore.CostOf` + `CostModApplies`。
3. **「来源离场就收回」最现成的机器是 `UnitState.RecordGrant` / `RevertGrantsFrom`** ——
   现在给「黑暗契约被替换」用，语义正好是「整份收回」。
4. 🔴 **最大的设计选择：关键词型光环没有单一读点** —— 全仓 ~20 处散读 `u.Has(kw)`。
   要么借 `AddKeyword`/`RemoveAll` 做「进入/离开相邻」的**增量维护**，
   要么**新开一个 `AuraKeywords` 读点**。
5. ⚠️ **「谁算相邻」只读 `BoardSpec.AdjacentSlots`**（全仓唯一）。原版
   `BattleManager.GetAdjacentUnits` 按**所属方**取那一方行内左右格 ⇒ **不跨排**，
   光环**只作用于己方相邻格**。
6. ⚠️ **两条待定语义**：`AdjacentSlots` **不含自己**（`Makari` 自己吃不吃 +1 要定）·
   `Armour` 是单值 `u.Armor`，`Armour 2` 的 `Baneblade` 旁边再给 `Armour 1` **叠不叠**要定。

---

## 五、一处需要更新的文档

`_tmp_view/adjacent_report.md` 第 3 行还写着「光环族（**本轮不做**）」——
现在**用户已定「要做」**，那份是自检重写的，**不用手改**；但引用它的文档要跟着更新。
