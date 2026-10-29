# B95 · `Rally` 的 `if (fromHand)` 闸 —— 判据核查（只读）

> 执行代理 `B95_A3`（批 2）· **2026-10-29** · ⛔ 本批未改任何源码 / 账 / 正本；本文件是唯一产出。
> 起因：批 1 交回时我提出「`RuleCore.cs:4946` 的 `if (fromHand)` 可能漏了效果召唤那一路」，调度台要求先闭合证据链。
> **结论：证据链闭合后，那条「可能漏了」不成立 —— 闸的行为是对的，错的是它旁边三条注释的【归因】。**

---

## 一、三问结论（一句话版）

| # | 问 | 结论 | 关键判据 |
|---|---|---|---|
| **1** | 效果召唤调 `BroadcastUnitSummoned` 那一刻 `card+0x228`（cardState）等于几？ | **= 2（InPlay）**，不是 5 ⇒ `ResolveUnitSummoned` 的 `!= 5` 闸**放行** ⇒ 195 / 605 **会发** | `_ResolveSummonUnit_d__510…MoveNext.c:101`（`SetAsUnitInPlay` → cardState=2）· `:185`（广播）· `CardScript__ResolveUnitSummoned.c:19`（闸）· `:36-49`（195+605） |
| **2** | `Rally` 在原版里挂在哪个函数上？ | **= `AbilityTrigger.ThisCardPlayed (0)`**，执行点 `RawCardScript__OnCardPlayedWithTarget.c:43` → `TriggerAbilities(…, 0, …)`；入口**只有**「牌被打出（从手牌）」那条链，**不是** `BroadcastUnitSummoned` 的下游 | 卡池数据：85 张 `Rally:` 卡的 `abilityTriggers` **全含 0**、全池 `195` 只有 1 张（`SW10`，卡面是 `When deployed…`）· 我们自己的表 `CardDef.cs:1917 {"ThisCardPlayed", 0}` / `ActionPump.cs:1099` · `CardScript__ResolveCardPlayed.c:22-24` · `BroadcastCardPlayed` 全库唯一调用点 `_ResolvePlayCardFromHand_d__447…:1426` |
| **3** | 若真漏了，池内多少张受影响？ | **不是「漏」，所以这个数是反向的**：真正风险是「**加上去会多打**」 | 见 §四 |

**⇒ 处置建议：`if (fromHand)` 一个字都别改**（⛔ 别按「补召唤广播」的思路给它加 195/190/605/680）。要改的只有 3~4 处**注释归因**，补丁见 §六。

---

## 二、Q1 逐行取证：效果召唤那一刻 `cardState == 2`

判据文件 = `d:/2/tools/decomp_full/BattleManager._ResolveSummonUnit_d__510__MoveNext.c`（307 行，本笔现读）

| 行 | 语句 | 作用 |
|---|---|---|
| `:87` | `lVar24 = BattleCardManager__CreateCard(lVar24, …)` | 造出被召唤的那张卡 |
| `:88-90` | `*plVar1 = lVar24;`（`plVar1 = param_1 + 0x130`） | 存进协程字段 ⇒ **被召唤的卡 = `coroutine+0x130`** |
| `:101` | `CardScript__SetAsUnitInPlay(*plVar1, 1, 0)` | **`CardScript__SetAsUnitInPlay.c:5` 第一句就是 `*(undefined4 *)(param_1 + 0x228) = 2;`** |
| `:183-185` | `UpdateWhileInPlay(coroutine+0x130, 1)` → `BattleManagerSupport__BroadcastUnitSummoned(lVar5, coroutine+0x130, 0, 0)` | 广播 |

**`:101` 到 `:185` 之间有没有别的地方改 `+0x228`？** 逐个查这条链上被调到的函数：
`MinionManager__SummonMinion.c`（0 处）· `MinionManager__MoveMinionToConversionPoint.c`（0 处）· `CardScript__SetCardPosAndScaleToBoard.c`（0 处）· `CardScript__SetAmbush.c`（0 处）· `CardScript__UpdateWhileInPlay.c` 有 1 处但**只读不写**（`:43` `if (*(int *)(param_1 + 0x228) == 1) goto …`）。
⇒ **`:185` 那一刻 `cardState == 2`**（本笔另跑了全库 `+0x228` 写点清单核对：写成 5 的只有 `SetAsUnitDestroyed.c:30` / `CheckIfDead.c:124`，这条链上一个都没调到）。

**广播体内**（`BattleManagerSupport__BroadcastUnitSummoned.c`）：
- `:19` `CardScript__ResolveUnitSummoned(param_2, param_2, param_3, 0)` —— **接收者 == 被召唤者**；
- 之后才 `:23-39` 扫棋盘每张牌、`:43-52` / `:57-64` 扫两手手牌（那几跳才是 190）。

**接收端**（`CardScript__ResolveUnitSummoned.c`）：
- `:19` `if (*(int *)(param_1 + 0x228) != 5)` ⇒ **cardState==2 时放行**；
- `:36-49` `op_Equality(param_2, param_1)` 支（自己）∧ `cardType == 0` ⇒ `RawCardScript__OnTrigger(…, 0xc3 /*195*/, …)` **紧接** `OnTrigger(…, 0x25d /*605*/, …)`。

⇒ **195（ThisUnitSummoned）与 605（UseFaith）在「效果召唤」这条路上确实会发。**
我们的现状与这个结论一致：605 走 `DeployFreeCore` 里**无条件**的 `if (!reanimated) ResolveFaithAbility(...)`（`RuleCore.cs:4950`）；195 走 `Deploy` 事件通道（`RuleCore.cs:4910-4912`）。**两处都不用改。**

---

## 三、Q2 逐行取证：`Rally` = `ThisCardPlayed(0)`，入口只有在「打出」链上

### 3·1 数据侧（本笔现跑 `cards_engine.json`，`count` = 1129）
- `desc` 含 `Rally` 的卡 **85 张**，`cardType` 全是 0（单位）；它们的 `abilityTriggers` **85/85 都含 0**（其余是 46 / 240 / 81 / 190 / 605 等**附加**能力）。
- 全池 `abilityTriggers` 含 **195** 的卡 = **1 张**：`SW10 Grey Hunter`，卡面 `When deployed, give Hunt Mark to a random enemy troop` —— **不是 Rally**。
- `195` 与 `Rally` 的交集 = **0 张**。

⇒ **Rally ≠ ThisUnitSummoned(195)**。`195` 是卡面写 `When deployed` 的那一档；`Rally:` 是 `ThisCardPlayed(0)` 的**印刷标签**（单位的「打出」即「从手牌部署」）。

### 3·2 我们自己的代码里本来就写着这件事
- `CardDef.cs:1917` `{"ThisCardPlayed", 0}`（`AbilityTrigger` 名→值表，表头注明「认不出 ⇒ -1，⛔ 不猜 0」）。
- `ActionPump.cs:1099` `case KeywordTable.Rally: return "ThisCardPlayed";    // 0` —— **关键词→trigger 的映射表里，Rally 明确指向 0**。
  ⇒ 「Rally = 195」只出现在**注释**里（`RuleCore.cs:4197` / `:4739` 等），与表和数据都冲突。

### 3·3 原版执行链（trigger 0 只从「打出」进）
| 环节 | 判据 |
|---|---|
| trigger 0 的唯一执行点 | `RawCardScript__OnCardPlayedWithTarget.c:43` `TriggerAbilities(raw, 0, …)`（同族另两处：`OnCardPlayedWithTargetJustThisCardPlayed.c:39`、`TriggerEnchantment.c:15`） |
| 谁调 `OnCardPlayedWithTarget` | `CardScript__ResolveCardPlayed.c:22-24`（`(**(code **)(*plVar1 + 0x308))(…)` 那一句） |
| 谁调 `ResolveCardPlayed` | **只有两处**：`BattleManagerSupport__BroadcastCardPlayed.c:35 / :46 / :58`（扫棋盘/两手牌）· `_ResolvePlayCardFromHand_d__447__MoveNext.c:1081`（自己那一张） |
| 谁调 `BroadcastCardPlayed` | **全库唯一一处**：`_ResolvePlayCardFromHand_d__447__MoveNext.c:1426` |

**反证（三条都在同一批 grep 里得到）**：
1. `_ResolveSummonUnit_d__510__MoveNext.c` 与 `_ResolveCardAutoSummoned_d__511__MoveNext.c` 的**完整调用清单**里**都没有** `ResolveCardPlayed` / `TriggerAbilities` / `BroadcastCardPlayed`（只有 `BroadcastUnitSummoned`、`MinionPlayedIntoField`、`UpdateWhileInPlay`、`SetAmbush`、`PlayMinionFromHand` …）。
2. `MinionManager__PlayMinionFromHand.c`（全文）只有摆放：`SetCardPosAndScaleToBoard` / `set_parent` / `InsertMinion` / `MoveMinionToConversionPoint` / `ReassembleMinions` —— **零触发**。
3. `PlayerHand__CardPlayedWithEffects.c`（52 行）是手牌效果记账，**零触发调用**。

⇒ **结论：Rally 走的是「打出 ⇒ ResolveCardPlayed ⇒ OnCardPlayedWithTarget(trigger 0)」这条链，与 `BroadcastUnitSummoned` 完全无关。** 效果召唤不发它，**我们的 `if (fromHand)` 与原版一致**。

### 3·4 附带核到的一件好事：`Rally` 排在 `605` 之前是对的
原版 `CardScript__ResolveCardPlayed.c` 内部次序：`:22-24`（trigger 0 = **Rally**）→ `:25-31`（590 Combo）→ `:32`（`ResolveUnitSummoned` ⇒ 195 → 605）。
我们的 `PlayCardCore`：`:4211` Rally → `:4213` `ResolveFaithAbility(605)` —— **次序与原版一致**（虽然注释给的理由不是这个）。

---

## 四、Q3 数字（**反向**：加上去会多打）

| 假设的改动 | 受影响张数 | 说明 |
|---|---|---|
| 去掉 `if (fromHand)`（`DeployFree` 无条件发 Rally） | **85 张**（`desc` 含 `Rally`，全 `cardType 0`）· 按 `keywords` 含 `Rally` 计 = **105 张** | 每一次「效果召唤」都会多跑一遍这些卡的 Rally 正文 |
| 在 `TransformUnit` 补 195/190/605/680（批 1 简报的字面要求） | **会多打：0 张** | 全池 Rally 卡**没有一张**带 195；195 只有 `SW10`，而它走 `Deploy` 通道早已发 ⇒ 这条改动**没有正确性收益，只有一个"原版没有的触发"风险** |
| **反方向**：`fromHand: true`（`A1571` 的 `autoPlayFromHand`）那一档**我们可能发多了** | 今天只有 **2 张**战术用它：`BL55 Black Crusade`（`Deploy 4 random troops from your deck`）· `SW57 Birth of a Saga`（`Each player deploys 3 troops from their deck`）。它们拉出来的部队若带 `Rally` ⇒ 我们会发、原版（d510/d511 都不发 trigger 0）不发 | 需要写手核「这两张战术在原版走的是哪个 action」；若确认是 `autoPlayFromHand(250)`（`BattleAbilityEffect=250` / `BattleActionType.autoPlayFromHand=46`，`dump.cs:20696` / `:45356`），则 `if (fromHand) FireTriggerOnBoard(Rally)` 应**去掉**（但 `ApplyHandBuffs` 那半**保留** —— 它在 d511 里真有 `PlayerHand.CardPlayedWithEffects` 对应物） |

---

## 五、任务 2：`0x4B0` = **1200** = **Ambush 的【默认/卡面】trait**

判据（全部现读）：

| 出处 | 内容 | 说明 |
|---|---|---|
| `CardScript__CardSetup.c:244` | `RawCardScript__HasDefaultTrait(raw, 0x4b0)` | **它是"默认 trait"**（卡 SO 上印的那一条），不是运行时挂的效果 |
| `CardScript__CardSetup.c:282` | `HasCurrentTrait(0x4b0)` | 当前是否仍有（伏击翻开后会被摘掉） |
| `CardScript__SetAmbush.c:27-50`（true 支） | `CardTraitCollection__GetTrait(0x4b0)` → `CardTrait.get_CustomObject`（一个 Sprite）→ `BasicCardUI__SetImageSprite` + `SetEmptyRarityStars` + 隐藏法力/属性 | = **伏击那张"卡背"图**：0x4b0 这条 trait 自带自定义贴图 |
| `CardScript__SetAmbush.c:74`（false 支） | `CardScript__RemoveTraitAndEffects(card, 0x4b0, 0)` | **翻开 = 摘掉 1200** |
| `CardScript__SetAmbush.c:126` | `OnTrigger(0x29e = 670 = Ambush)` | 翻开发 `Ambush(670)`（与 `CardDef.cs` 的 `{"Ambush", 670}` 对得上，**那是 AbilityTrigger，与 trait id 是两套编号**） |
| `MinionManager__AreMinionsWithAmbush.c:26/:44` | 扫双方棋盘 `HasCurrentTrait(0x4b0)` | 判定「场上还有伏击单位」 |
| 其余读点 | `CardScript__LandOnGround.c` · `CardScript__UpdateTraitIcons` · `CardScript__CardToMinionAnim` · `CardScript__HasToUseLegendarySummonEffect` · `CemeteryManager__AddPlayCardAction` · `CardScript__ResolveUnitAttacked` · `CardScript__ResolveDamageDealt` · `BattleCardUI__SetCardImageTo3DBase` | 全部与伏击相关（落地/图标/攻击/受伤/墓地记录） |

**池内张数**：`cards_engine.json` 的 **`cardTraits` 列整列为空**（1129 张一张不差）⇒ **我们的数据里没有 trait id**，无法精确数 1200。按关键词近似：`keywords` 带 `Ambush` 的 **12 张**（`desc` 含 `Ambush` 11 张，两者并集 15 张）。
样例：`GSC20 Aberrant`（`['Armour 1','Ambush']`）· `GSC16 Acolyte Heavy`（`['Ambush','Strike']`）· `GSC9 Acolyte Hybrid`（`['Ambush: Gain +2 [Attack] and +2 [Ranged]']`）。

⇒ **对 `ResolveTransformUnit.c:1775-1825` 的落地含义**：那一句是「**变身之后如果这张卡仍是伏击形态，重设伏击外观/状态**」。我们的等价动作是「按卡面 `ambush` 关键词判 `FaceDown`（`RuleCore.PlaysFaceDown`）」，**不是**去找一个名为「1200」的数字常量，也**不需要**为此新造一个 trait-id 映射表 —— 判据是**卡面上的伏击标记**，trait 1200 只是它在 SO 里的编号。

---

## 六、可粘贴补丁（**只改注释，不改行为** —— 由调度台决定派给 `RuleCore.cs` 的写手）

### P1 `Core/RuleCore.cs:4734-4741`（`DeployFree` 的 doc，归因错得最离谱的一处）

**原文**
```
        /// ⚠️ **默认不触发 Rally**。这是**故意的**，不是漏了：效果召唤 / 部署在原版走的是
        ///   `_ResolveSummonUnit_d__510`（`MinionPlayedIntoField(..., 0)` = `shouldTriggerSummon` **假**），
        ///   `RuleCore.PlayCard` 那条「从手牌打出」的路才触发 —— 两条路不一样是对的。
        /// 🔴 **例外 = `fromHand: true`（`A1571`）**：原版 `autoPlayFromHand(250)` 走的是
        ///   `_ResolveCardAutoSummoned_d__511`，那里 `MinionPlayedIntoField(..., 1)` ⇒
        ///   `ThisUnitSummoned(195)` = **Rally 会触发**，而且 `PlayerHand.CardPlayedWithEffects`
        ///   会兑现手牌加成。⇒ 只有「从牌库拿出来免费打出」那一档传 `true`（今天 = `BL55` / `SW57`）；
        ///   ⛔ 别给普通效果召唤传 —— 那会把 Rally 多触发一次（静默多打）。
```
**新文**
```
        /// ⚠️ **默认不触发 Rally**。这是**故意的**，不是漏了 —— 但理由**不是** `shouldTriggerSummon`：
        ///   ① `Rally` = **`AbilityTrigger.ThisCardPlayed(0)`**（卡池 85 张 `Rally:` 卡的 `abilityTriggers`
        ///      全含 0；`CardDef.cs:1917 {"ThisCardPlayed",0}`，`ActionPump.cs:1099` 的 keyword→trigger 表也这么写），
        ///      执行点 = `RawCardScript__OnCardPlayedWithTarget.c:43`；
        ///   ② trigger 0 的入口**只有「打出」那一条**：`CardScript__ResolveCardPlayed.c:22-24` ←
        ///      `BattleManagerSupport__BroadcastCardPlayed.c:35/46/58` + `_ResolvePlayCardFromHand…:1081`，
        ///      而 `BroadcastCardPlayed` **全库唯一调用点** = `_ResolvePlayCardFromHand_d__447…:1426`；
        ///   ③ 两条召唤协程（`_d__510` / `_d__511`）的**完整调用清单里没有** `ResolveCardPlayed` / `TriggerAbilities`，
        ///      `MinionManager__PlayMinionFromHand` 只有摆放 ⇒ **召唤不跑 trigger 0**；
        ///   ④ 🔴 `shouldTriggerSummon`（`_MinionPlayedIntoField…MoveNext.c:188/:194`）**只管落地特效**，
        ///      两支都进 state 4 ⇒ 与任何触发无关。
        /// 🔴 **`fromHand: true`（`A1571`）那一档仍需重核（未闭合）**：d511 里**没有** trigger-0 的调用点，
        ///   所以「自动从手牌打出会触发 Rally」**目前没有判据支持**；`PlayerHand.CardPlayedWithEffects`
        ///   那一半（兑现手牌加成）**有**对应物。⚠️ 改动前先核「这两张战术在原版走哪个 action」。
```
（净 **+4 行**；该文件被账按行号引用 **365+** 处 ⇒ 建议由 `RuleCore.cs` 的写手在**行数稳定后**跑一次 `check_ledger_points --all-A --fix`。）

### P2 `Core/RuleCore.cs:4195-4197`
**原文** `///    `Rally`（= `ThisUnitSummoned(195)`）**之后**。`
**新文** `///    `Rally` = `ThisCardPlayed(0)`（**不是 195**；195 只有 `SW10 Grey Hunter` 那张 `When deployed` 卡）**之后**。`
（那一整段的**结论仍成立**：原版 `ResolveCardPlayed.c` 里 trigger 0 在 `:22-24`、605 在 `:32` 之后 ⇒ Rally 先。只是依据要换。）

### P3 `Core/RuleCore.cs:4915-4917` 与 `:4929-4932`
两处把 Rally 的入口差异归因成 `shouldTriggerSummon` —— 同样换成「trigger 0 只在打出链上」这一条（措辞可照 P1 的 ①②③）。

### P4 `Core/UnitState.cs:767-770`（与批 1 那条同源，`DeployedTurn` 第 4 个写点）
见批 1 报告 §6·B，不重复。

---

## 七、没查清的部分（⛔ 没猜）

1. **`autoPlayFromHand`（action `0x2e = 46`）到底绑定 `_d__511` 还是 `_d__510`** ——
   `ResolveAction.c:4063 case 0x2e` 造匿名状态机后没有可读的方法名；两个 wrapper（`ResolveSummonUnit` / `ResolveCardAutoSummoned`）在 ILSpy 里**没有符号调用点**（走的是委托/方法表）。
   ⚠️ **但不影响本报告的结论**：两个候选协程**都不发 trigger 0**（§三·3 反证 1）⇒ 无论绑哪个，`autoPlayFromHand` 都不触发 Rally。
2. **`fromHand: true` 那 2 张战术的真实行为**（§四第三行）—— 需要写手拿「牌库部署」那条效果的原版解析（`AbilityEffect` 值 / 入队 action）逐条核一遍；本笔只做到「两条候选协程都不发 Rally」。
3. **`WhenEventKind.Deploy` 与 195/190 的对应关系在实际数据上的覆盖面** —— 本笔只核了 `CardDef.cs` 的 trigger 名表与 `DeployFreeCore` 的调用点，**没有跑自检**验证 `SW10` 在「效果召唤」时真会响（建议写手补一条断言：`DeployFree(SW10)` ⇒ 195 那支跑到）。
4. **`cards_engine.json` 的 `cardTraits` 整列为空** —— 生成器没带 trait ⇒ 1200 的池内张数只能按关键词近似（12 张）。要精确，得回去读 1,351 张卡 SO 的 `cardTraits`（`D:/2/关服前存档/…/ea8bca…/__data`）。
5. **顺手发现（需另开一路核，未改）**：`PlayCardCore` 里 `190/195`（`Deploy` 通道，`:4127`）**早于** Rally（`:4211`），而原版 `_ResolvePlayCardFromHand…` 是**自己那张**先 `ResolveCardPlayed`（`:1081`，含 trigger 0 = Rally）**再** `BroadcastCardPlayed`（`:1426`，通知别人 = 190）⇒ 我们这两步的**先后与原版相反**。对「190 监听者给新单位加成」这类卡，Rally 是否看得见那份加成会不同。**本笔只记不判**。

---

# 附：`190/195` 与 `Rally` 的**先后次序**（批 3 · 只读核查 · 2026-10-29）

> 起因：批 2 §七·5 的「顺手发现」。**批 3 结论：那是一条真偏离**（次序确实相反），且已把原版的完整次序逐条钉死。
> ⛔ 本批仍未改任何源码；`RuleCore.cs` 归 `B95_WRE`，本文件只给方案。

## 八·1 原版「从手牌打出一张单位」的**完整触发次序**（逐条 `.c` 行号）

判据文件：`_ResolvePlayCardFromHand_d__447__MoveNext.c`（1548 行）+ 它调到的三个函数。

| 序 | `.c` 出处 | 触发 | 说明 |
|---|---|---|---|
| 0 | `:659` / `:892` | `CardScript.MinionPlayedIntoField` | 落地；**无触发**（`shouldTriggerSummon` 只管落地特效，批 1 已证） |
| 1 | `:1081` → `CardScript__ResolveCardPlayed.c:22-24` → `RawCardScript__OnCardPlayedWithTarget.c:43` | **`TriggerAbilities(…, 0, …)` = `ThisCardPlayed` = `Rally`** | **「自己那张」的 trigger 0** |
| 2 | `RawCardScript__OnCardPlayedWithTarget.c:66` | **600 `UseSpiritStone`** | 同一个 `OnCardPlayedWithTarget` 内，紧跟 trigger 0（`:43` → `:66`） |
| 3 | `CardScript__ResolveCardPlayed.c:25-31` | **590 `Combo`** | `IsTheSameTurnPlayed` 闸 |
| 4 | `CardScript__ResolveCardPlayed.c:32` → `CardScript__ResolveUnitSummoned.c:47` | **195 `ThisUnitSummoned`** | `op_Equality(self,self)` 支 |
| 5 | 同上一跳 → `:51` | **605 `UseFaith`** | 紧接 195 |
| 6 | `:1423` | `PlayerHand.CardPlayedWithEffects` | 手牌效果记账（零触发） |
| 7 | `:1426` → `BattleManagerSupport__BroadcastCardPlayed.c:21-58`（在场表 `+0x470` 于 `:25-35`；两手牌 `:42-46` / `:52-58`） | 对**在场每张牌 + 两手牌**各调 `ResolveCardPlayed(那一张, 打出的那张)` | 这一轮才是「**别人**看见有人打出」 |
| 7a | → `RawCardScript__OnCardPlayedWithTarget.c:43`（接收者 = 别人） | 「别人打出」那一族（`OtherCardPlayed=10` 等） | |
| 7b | → `CardScript__ResolveUnitSummoned.c:32` | **190 `OtherUnitSummoned`** | `op_Inequality(别人, 打出的那张)` ∧ `IsUnit(打出的那张)` |
| 7c | → 同文件 `:66` | **550 `Support`** | 同一个 `op_Inequality` 族的第二条 |
| 7d | → 同文件 `:98` | **680 `Uprising`** | 闸 = `playedIntoFieldTurnCounter(+0x2E0) == turnCounter(+0x3F8)` |

**⇒ 195 的位置很明确：它在 `:1081` 那一跳【内部】**（`ResolveCardPlayed.c:32`），**早于** `:1426` 的广播；而它在**整套**次序里排在 Rally(0) → 600 → 590 **之后**、605 之前。
**⇒ 190 在「手牌打出」这条路上【确实会发】** —— 但**不是**走 `BroadcastUnitSummoned`（那个函数全库只有 `_d__510` / `_d__511` 两个调用点），而是走 `BroadcastCardPlayed` → `ResolveCardPlayed(别人, 打出的)` → `ResolveUnitSummoned` 的 `op_Inequality` 支。

## 八·2 我们的次序（`RuleCore.PlayCardCore`，行号现读）

| 行 | 调用 | 对应原版 |
|---|---|---|
| `:4087` | `BroadcastWhen(WhenEventKind.Play, …)` | （文字通道「当你打出一张牌」） |
| `:4104` | `NotePlayed` + `ApplyPlayedCardTailEffects` | 590 `Combo` + ④ ephemeral 摘除 |
| `:4125-4126` | `PreCallBoardListener(selfOnly)` | 195（自己那一支的预调） |
| `:4127` | `BroadcastWhen(WhenEventKind.Deploy, …)` | 190 / 190 的文字通道 |
| `:4133` | `EffectResolver.ResolveDeploy` | 190 的常驻作业那一半 |
| `:4156` | `FireTriggerOnSide(… Uprising …)` | 680 |
| `:4183` | `ResolveSpiritAbility` | 600 |
| `:4211` | **`FireTriggerOnBoard(… Rally …)`** | **trigger 0** |
| `:4213` | `ResolveFaithAbility` | 605 |

## 八·3 差异清单（四条，全部有 `.c` 依据）

| # | 原版 | 我们 | 判定 |
|---|---|---|---|
| ① | Rally(0) 是**自己那一跳的第一件事**（`:1081` → `OnCardPlayedWithTarget.c:43`） | Rally 在 `:4211`，排在 195/190/550/680/600/590 **全部之后** | 🔴 **次序反了**（批 2 §七·5 那条成立） |
| ② | 190 / 550 / 680 在**广播那一轮**（`:1426`），是整套次序的**最后** | `:4127` / `:4133` / `:4156`，排在 Rally 之前 | 🔴 反了（同一条根因） |
| ③ | 600 紧跟 trigger 0（同一个 `OnCardPlayedWithTarget`，`:43`→`:66`） | `:4183`（在 680 之后、Rally 之前） | 🔴 反了 |
| ④ | 590 在 trigger 0 与 195 **之间**（`ResolveCardPlayed.c:25-31`，`:32` 之前） | `:4104`（排在全部触发之前） | 🟡 也偏前 |
| ⑤ | 195 → 605 相邻（`:47` → `:51`） | `:4125-4126`(195) 与 `:4213`(605) 之间夹了 190/550/680/600 | 🔴 被拆开（原版是**紧邻**） |

⚠️ 我们 `:4131` / `:4175` / `:4221` 三处注释写着「`ResolveDeploy` / 灵魂石 排在 `Rally` 之前 —— **我们挑的**（Rally 结算时看得见刚给的关键词）」。**这三处正是本次要推翻的自设计**（CLAUDE.md 18·0：能复刻就复刻）。另 `CardDef.cs:8091` 也写了「`ResolveDeploy` 之后、`Rally` 之前」，同批要改。

## 八·4 Q2 · 可观测面（池内张数，`cards_engine.json`，`count`=1129）

- `abilityTriggers` 含 **190** 的卡 = **11 张**。逐张看卡面：
  `ASH66`(给 Vehicle Shuriken 1) · `AM7`(给 Vehicle Flank) · `UM12`(给 Vehicle Flank) · `BL30`(给 troop 随机 Dark Pact) · `TAU34`(给 troop +2 Health) · `TAU25`(给 Drone +1 Ranged) · `GOF20`(给 Stormboy Flank) · `SAU2`(给 Destroyer Regeneration 1) · `UM66`(给**敌方**刚部署的造成 2 伤害) · `GOF77`(给**你的督军** +1 Attack) · `AM55`(战术卡，减费)。
  ⇒ **「给刚打出的那张加东西」= 9 张**（前 8 张给己方刚打出的 + `UM66` 打敌方刚打出的）。
- 次序差异**看得见**的条件 = 被打出的那张**自己有 Rally 正文、且正文读的属性/关键词正好被观察者改了**。按 `desc` 里 `Rally` 之后的正文筛（token：Attack / Ranged / Health / Armour / Melee / Flank / Shuriken / Regeneration / Vanguard / Stealth / Camouflage / Hunt Mark / Damage / Stun / Blind …）：

| 观察者的条件 | Rally 卡（池内） | 其中「正文读属性/关键词」的 | 备注 |
|---|---:|---:|---|
| **任意 troop**（`BL30` / `TAU34`） | **85** | **53** | 这就是**可观测上界 = 53 张** |
| Vehicle（`AM7` / `UM12` / `ASH66`） | 18 | **13** | 含在 53 里 |
| Stormboy（`GOF20`） · Drone（`TAU25`） · Destroyer（`SAU2`） | **0** | **0** | 这三族池内**没有**带 Rally 的卡 ⇒ 零可观测 |
| 同时是「Rally 卡」又是「190 观察者」 | — | — | 只有 `BL30 Chaos Lord` / `TAU25 Firesight Marksman` 两张；190 是 `op_Inequality` ⇒ **不会自己给自己**，无自环 |

⇒ **结论：可观测面 = 最多 53 张 Rally 卡**（在 `BL30` / `TAU34` 这类「对任意 troop 生效」的观察者于场上时）；不是 0，所以这**值得修**。

## 八·5 Q3 · 一句话方案（⛔ 不改代码，交 `RuleCore.cs` 的写手）

**搬两处、换完次序 = `Rally(0)` → `600` → `590` → `195` → `605` → `190/550/680`**：
- ① 把 `:4183`（`ResolveSpiritAbility`=600）与 `:4211`（`FireTriggerOnBoard(Rally)`）**上移**到 `:4104`（590 那批）**之前**；
- ② 把 `:4125-4133`（`PreCallBoardListener` + `BroadcastWhen(Deploy)` + `ResolveDeploy`，= 190/550 那批）与 `:4156`（`Uprising`=680）**下移**到 `:4213`（605）**之后**。
- 与 `:4213`（605）**不冲突**：原版 195 与 605 紧邻，而 195 在 `PreCallBoardListener` 那一句里 ⇒ 195 与 605 会被 ② 拆到两处。**这一格是本方案的唯一代价**，两条路可选：**(a)** 把 195 的预调（`PreCallBoardListener(…selfOnlyOnly:true)`）**单独留在原位**（只把非 selfOnly 的 `BroadcastWhen(Deploy)` + `ResolveDeploy` + `Uprising` 下移）—— 这样 195 → 605 仍紧邻、与原版一致，**推荐**；**(b)** 整个 Deploy 块下移（195 会跑到 605 之后，与原版不符）。

## 八·6 断言条文（两条，供派活）

### A · `SW10` 的 195 在「效果召唤」上会响（补批 2 §七·3 的空缺）
- **加在哪**：`Assets/RuleEngine/Editor/RuleEngineTest_W12.cs`（该文件已经在管「部署广播」那一族，见 `W12EffectSummonEmitsThisUnitSummoned`）。
- **怎么写**：
  ```csharp
  static void B95Sw10ThisUnitSummonedFiresOnSummon()
  {
      // 前提：SW10 是池内**唯一** abilityTriggers 含 195 的卡（cards_engine.json 现读；卡面 "When deployed, give Hunt Mark to a random enemy troop"）
      var sw10 = CardDatabase.Get("SW10");                     // ⚠️ 具体取卡 API 以本仓现有助手为准（`B12*` 用的是手写夹具）
      CheckTrue(sw10 != null, "前提：卡表里取得到 SW10（取不到 ⇒ 本断言不成立，⛔ 不要静默跳过）");
      if (sw10 == null) return;
      var foe = Unit("B95Foe", 1, 1, 9);
      var ctx = Battle(new CardDef[0], new[] { foe });
      ToP1Turn(ctx, 1);
      int slot;
      bool ok = RuleCore.DeployFree(ctx, 0, sw10, out slot);   // = 效果召唤（**不是** PlayCard）
      CheckTrue(ok, "前提：效果召唤 SW10 成功");
      var u = slot >= 0 ? ctx.Players[0].Board[slot] : null;
      CheckTrue(u != null, "前提：SW10 在格 " + slot);
      Check(foe.Has("huntMark") ? 1 : 0, 1, "🧨 效果召唤也发 195 ⇒ 敌方单位拿到 Hunt Mark（判据 `CardScript__ResolveUnitSummoned.c:19` 闸放行 + `:47` 195）");
  }
  ```
  （`Hunt Mark` 的落到哪个 keyword 名 / 是否用 `AddKeyword(KeywordTable.HuntMark,…)` 表示，**写手现读 `CardDef.cs` 定**，⛔ 别照抄我这行。）
- **🧨 什么能让它红**：把 `DeployFreeCore` 的 `Deploy` 电台那一句删掉 / 把它挪进 `if (!reanimated)` 之外再包一层 `fromHand` 判断。

### B · **次序结构断言**（比行为断言更稳，且能挡住回归）
- **加在哪**：同 `RuleEngineTest_W12.cs`（或 `RuleEngineTest_EngineB94B12.cs` 那种「按源码扫描」的族 —— `TestNewUnitEntryList` 就是这个风格）。
- **怎么写**：读 `Core/RuleCore.cs` 的源码文本，找 `PlayCardCore` 段内两个锚点的**行号先后**：
  ```csharp
  int iRally = IndexOfLine(code, "FireTriggerOnBoard(ctx, unit, KeywordTable.Rally, chosen)");
  int iDeploy = IndexOfLine(code, "BroadcastWhen(ctx, WhenEventKind.Deploy, p, unit.Card, unit, preCalled: unit)");
  CheckTrue(iRally > 0 && iDeploy > 0, "前提：两个锚点都还在（改名/删掉 ⇒ 本断言前提不成立，⛔ 别静默跳过）");
  CheckTrue(iRally < iDeploy,
      "🧨 原版 `Rally`（trigger 0，`_ResolvePlayCardFromHand…:1081`）**早于**别人的 190（`:1426` BroadcastCardPlayed）");
  ```
- **判据**：`_ResolvePlayCardFromHand_d__447__MoveNext.c:1081`（自己那张 → trigger 0）**在** `:1426`（`BroadcastCardPlayed`）**之前**。
- **⚠️ 现状会红** —— 这正是它存在的意义（写手搬完次序它才绿）。

## 八·7 没查清的部分

1. **`BattleAbilityEffect 250 (autoPlayFromHand)` → 哪个协程**（批 2 §七·1 的延续）：`ResolveAction.c:4063 case 0x2e` 之后无可读方法名 ⇒ 仍未绑死。不影响本节的结论（本节讲的是 `_ResolvePlayCardFromHand` 那条路）。
2. **`UM66`（「当敌人被部署时，对其造成 2 伤害」）在我们的 190 通道上**：它的目标是**敌方**刚部署的那张，属于「190 但要换靶」的一档 —— 本节只统计未判它的实现是否符合；建议写手顺手核 `EffectResolver.ResolveDeploy` 对 `OwnerIs` 的处理。
3. **53 张「正文读属性」的判法是文本 token 匹配**（`desc` 里 `Rally` 之后的段）⇒ 是**上界**；精确到「真的会因为 Flank/+2 Health 而结果不同」需要逐张读正文语义（本批没做）。
4. **`GOF77`（给**你的督军** +1 Attack）不受次序影响**（靶不是新单位）—— 归类时已排除，但未逐条核它的效果解析。
