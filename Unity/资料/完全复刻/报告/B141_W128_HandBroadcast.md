# B141_W87 · 手牌广播覆盖率对账（`abilityContext == 10 Hand` 的 13 张卡）

只读调查 · 批次 B141 · 会话 W128 · 2026-11-04 · 上游 = `报告/B141_W128_CaRowDispatch.md` §六·4（W67）。
白名单：只写本文件。⛔ 未改源码/资产/账/正本。

## 〇、结论（先看这里）

1. **真缺口 = 0 条**；**「判据本身错」= 13 / 13 张**。
   W67 的前提「这 13 张的手牌能力只能走 `FireHandAbilityTrigger`」**不成立**。原版每条广播都有自己的手牌接收方法
   （`CardScript.OnTurnEnd` / `ResolveCardPlayed` / `ResolveDeadCard` / `ResolveUnitSummoned` /
   `UpdateWhileInPlay` / `TriggeredSynapse` / `TriggerOtherCardCodex`）；我们引擎把这 13 张的**实际行为**落在
   **文本解析路**（`CostWhens` / `HandTrapWhens` / `ResolveAtTurn` 手牌陷阱 / `CostPerKeywords` /
   `CostIfControls` / `CostPerEnemyHandCard`），逐张有落点（§三），且 `B117_逐卡核验_*` 已把这些卡判 ✅。
2. **原版确实把这些 trigger 发到手牌** —— 下面 8 个 trigger 对应的广播器**全部有 `GetCardsInHandRef` 两跳**
   ⇒ 「不在我们那 12 个名字里」**推不出**「原版不发」。
3. 8 个 trigger 里有 **4 张卡的 `ThisCardPlayed(0)` 行是空转行**：`chosenAbility == 3` 在
   `AbilityLogic__PlayAbility.c:1456-1459` 是「log 一行就 `return`」⇒ **无行为可复刻**。
4. 事实订正：我们生产调用点实际发的 trigger 名是 **14 个**（不是 W67 写的 12）——
   W67 漏算 `UnitAttack(50)` 与 `TurnSetup(155)`，见 §二。

## 一、13 张卡清单（`cards_engine.json` 现读：16 条 context=10 行 → 13 张卡）

| # | id | 卡名 | trigger（context=10） | 卡面行为 | 我们现读落点 | 判定 |
|---|---|---|---|---|---|---|
| 1 | ASH85 | Fate Inescapable | 240 | This costs 1 less if you control a unit with Stealth | `CardDef.cs:5542` CostIfControls → `RuleCore.cs:1023` | ✅ 已覆盖 |
| 2 | AM55 | Mechanised Infantry | 190 | Lower cost by 1 when you deploy a Vehicle | CostWhens（`CardDef.cs:5692`）→ `EffectResolver.cs:10870/10888` | ✅ 已覆盖 |
| 3 | AM56 | Vengeance for Cadia | 90 | Lower cost by 1 when a friendly troop dies | 同上 | ✅ 已覆盖 |
| 4 | BL52 | Unholy Smite | 240 | costs 1 less for each Dark Pact on friendly units | `CardDefCostPerKeyword.cs` → `RuleCore.cs:1036` | ✅ 已覆盖 |
| 5 | GSC37 | Patriarch | 240 | Costs 1 less for each card in enemy hand | `RuleCore.cs:1034` CostPerEnemyHandCard | ✅ 已覆盖 |
| 6 | 7c666a04c1b9e8b4e9a96247e9fe115a | Cult Propaganda | 40（0 = 空转） | At the end of your turn, create … in opponent's hand | `EffectText.cs:5793` SplitAtTurn → `EffectResolver.cs:8906` ResolveAtTurn ① | ✅ 已覆盖 |
| 7 | 334a32d196d46ed4387bf3740cce069b | Improvised Barricade | 0 | Does nothing | `chosenAbility 3` = 空转（`AbilityLogic__PlayAbility.c:1456`） | ✅ 无行为 |
| 8 | 68909cf02f472734ea391665e0d28e15 | Jammed Communications | 10（0 = 空转） | When you play a Stratagem, your Warlord takes 1 damage | `EffectText.cs:5836` SplitHandTrapWhen → `EffectResolver.cs:10946` | ✅ 已覆盖 |
| 9 | a7bb23355a30d344e9d2decf976d91e1 | Poisoned Supplies | 40（0 = 空转） | At the end of your turn, your troops take 1 damage | SplitAtTurn → ResolveAtTurn ① | ✅ 已覆盖 |
| 10 | SW58 | Fenrisian Wolfpack | 240 | costs 1 less for each Hunt Mark on enemy troops | `RuleCore.cs:1036` CostPerKeywords（账 `B-ENG-24` 已核销） | ✅ 已覆盖 |
| 11 | TAU57 | Burgeoning Empire | 90 | Lower cost by 1 when an enemy dies | CostWhens（`EffectText.cs:8733` TryCostWhen） | ✅ 已覆盖 |
| 12 | TL70 | Norn Emissary | 630 | Lower cost by 2 every time a friendly unit triggers Synapse | CostWhens；事件 = `EffectResolver.cs:2986` → `RuleCore.cs:12975` | ✅ 已覆盖 |
| 13 | UM40 | Avenging Zeal | 650（0 = 普通出牌） | Lower cost by 1 when a friendly Troop or Stratagem trigger Codex | CostWhens；事件 = `RuleCore.cs:13432/13464` → `:12975` | ✅ 已覆盖 |

> 16 条行里多出的 3 条 = Cult Propaganda / Jammed / Poisoned 各多一条 `0 ThisCardPlayed`（空转行）。

## 二、我们「手牌广播」现读实际发的 trigger 名（14 个，全部走 `FireHandAbilityTrigger`）

| trigger | 号 | 出处 |
|---|---|---|
| TurnStart / TurnStartPlayer / Relentless / Talent | 30 / 31 / 230 / 46 | `EffectResolver.cs:19204/19206/19207/19209`（`BroadcastTurnStartHandHops`） |
| TurnStarted | 160 | `EffectResolver.cs:19226` |
| UnitPoisoned | 290 | `EffectResolver.cs:19239` |
| OtherCardRegiment | 646 | `EffectResolver.cs:19252` |
| UnitDamaged / …ByAbility / …ByAttack / Cruelty | 60 / 185 / 187 / 720 | `EffectResolver.cs:19300/19303/19306/19311` |
| CardCreatedInHand | 210 | `EffectResolverPartial_B97W2.cs:129` |
| UnitAttack | 50 | `EffectResolver.cs:20083`（W67 漏算） |
| TurnSetup | 155 | `RuleCorePartial_B94B21.cs:98`（W67 漏算） |

⇒ 这 14 个名字与 13 张卡的 8 个 trigger **无交集**；但**不等于**这 13 张没人管（见 §一）。

## 三、13 张卡的 trigger → 原版手牌事件源（逐条现读 `decomp_full`）

| trigger | 号 | 原版广播器（手牌两跳行号） | 手牌接收方法 → 发出的 trigger |
|---|---|---|---|
| ThisCardPlayed | 0 | `BattleManagerSupport__BroadcastCardPlayed.c:39/:49` | `CardScript__ResolveCardPlayed.c:22` → `OnCardPlayedWithTarget`（ILSpy `RawCardScript.cs:1115`；`thisCard==cardPlayed` 支） |
| OtherCardPlayed | 10 | 同上 | 同上（`thisCard!=cardPlayed` 支） |
| TurnEnd | 40 | `BattleManagerSupport__BroadcastTurnEnd.c:45/:54` | `CardScript__OnTurnEnd.c:73`（`0x28`） |
| OtherUnitDeath | 90 | `BattleManagerSupport__BroadcastDeadUnit.c:152/:209` | `CardScript__ResolveDeadCard.c:236` → `RawCardScript__OnMinionDeath.c:19`（`0x5a`） |
| OtherUnitSummoned | 190 | `BattleManagerSupport__BroadcastUnitSummoned.c:44/:54` | `CardScript__ResolveUnitSummoned.c:19/:32`（`0xbe`；入口闸 `cardState != 5` ⇒ 手牌不被排除） |
| WhileInPlayVariable | 240 | `BattleManagerSupport__BroadcastWhileInPlay.c:55/:83` | `CardScript__UpdateWhileInPlay` → `RawCardScript__UpdateWhileInPlay.c:14`（`0x14`=20）/`:17`（`0xf0`=240） |
| TriggeredSynapse | 630 | `BattleManagerSupport__BroadcastUnitSynapse.c:41/:50` | `CardScript__TriggeredSynapse.c:7`（`0x276`） |
| TriggeredCodex | 650 | `BattleManagerSupport__BroadcastUnitCodex.c:42/:51` | `CardScript__TriggerOtherCardCodex.c:20`（`0x28a`） |

⇒ 结论：**原版 8/8 都由手牌广播触发**。W67 把「不在我们 14 个名字里」读成「原版不发／整条链到不了」，**前提错**。

## 四、「补法」= 不需要补；但要补的是【结构层留痕】，不是再加广播

1. 这 13 张的行为已在文本路落地，**再加 `FireHandAbilityTrigger` 的对应广播会双减费／双结算**
   （与 W67 的 B7「正文 op 已覆盖 ⇒ 本层让开」同一条约束）。
2. 若将来真要把 9 个 trigger 家族的**能力触发层**补齐（20/240 · 0/10 · 40 · 90 · 190 · 630 · 650），
   入口只在 `EffectResolver.HandHops` 与各广播器；**必须先建「文本层已覆盖 ⇒ 让开」闸**，
   且逐张给正/负例断言，⛔ 不能先发文路。
3. 现状 `FireHandAbilityTrigger` 的 14 个名字**全池零可达**（W67 §五·1 已记，本轮复核成立）——
   它是「结构完整但休眠」的基础设施，不是缺陷。

## 五、❓ / 顺手发现（请调度台分流，⛔ 本笔未改）

1. 🟠 **`ResolveAtTurn` ① 只扫当前行动方手牌**（`EffectResolver.cs:8916-8948`），而原版
   `BroadcastTurnEnd` 手牌两跳**两边都扫**（`.c:45/:54`）。今天池内这族卡面全是 `your turn` ⇒
   可观测差 0；数据行没有 holder 闸，**原版是否两边都响未证**（如实写「未证」，⛔ 不猜）。
2. 🟡 `AM55` / `AM56` 的 `abilitySummon` 里塞了 `AM11 ×3 all=1`，而卡面无召唤句、`ca` 是 40/30
   ⇒ `PlayAbility` 按 `chosenAbility` 分派 ⇒ 该字段是**不会被读的死数据**。如实记，不影响本判定。
3. 🟡 原版带手牌两跳的 `BattleManagerSupport.Broadcast*` 共 **36 条**，我们的能力触发层只有 **8 条**
   手牌跳（§二那 14 个名字来自 8 个广播器）；其余手牌语义由文本路/棋盘路承载 —— 若做「能力层全覆盖」
   对账，这是缺口面的**分母**（不是本 13 张的缺口）。
4. 🟢 `B117_逐卡核验_B` 对 UM40 记「Codex 降费支未触发」、`_E` 对 TAU57 记「电池 A2 盲区」——
   那是**探针电池没跑到**，不是代码缺分支：`FireTriggerAt(Codex)` → `FireTriggerDeferred` →
   `BroadcastWhen(Triggers(codex))` → `BroadcastCostWhen` 这条链现读完整（`RuleCore.cs:13464` / `:12975` /
   `EffectResolver.cs:10870`）。

## 六、出处清单

| 用途 | 路径 |
|---|---|
| 13 张卡的数据行 | `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（`abilityContext`）· `cards_truth.json`（`abilities[].logic`） |
| 我们 14 个手牌 trigger 名 | `EffectResolver.cs:19204-19311 / 20083` · `EffectResolverPartial_B97W2.cs:129` · `RuleCorePartial_B94B21.cs:98` |
| 文本层落点 | `CardDef.cs:5542/5692` · `EffectText.cs:5793/5836/8733` · `EffectResolver.cs:8906/10870/10946` · `RuleCore.cs:1023/1034/1036` |
| 原版手牌两跳 | §三 表内各 `.c` 行号（`d:/2/tools/decomp_full/`） |
| 逐卡先例 | `判据/B117_逐卡核验_A_Sororitas_SaimHann.md:55` · `_B_Ultramarines_EC.md:75` · `_D_Sautekh_BL_AM.md:44/49/77` · `_E_Leviathan_Tau_SW_GS.md:46/50/52/77/102/133` |
