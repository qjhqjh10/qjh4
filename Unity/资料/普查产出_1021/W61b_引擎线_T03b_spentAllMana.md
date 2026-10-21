# W61b · 引擎线 `T03·b` —— 回合流程残余三块机制（执行代理 F）

> 2026-10-21 · 执行代理 F · 判据第一权威 = 全量反编译 `d:/2/tools/decomp_full/`（关键处另做**机器码反汇编**核对）。
> ⛔ 未跑 Unity、未跑 `*.Run`、未动 git、未改正本（`CLAUDE.md` / `项目任务.md` / `任务线/**`）。
> ✅ 跑了 `typecheck.sh`（0/0）· `ruleprobe.sh check`（rc=0）· `ruleprobe.sh play check`（**rc=1，判为陈旧基线，见 §7**）。

---

## 1. 结论（四句）

1. **`SpentAllMana` 整条建好了。** 原版语义 = **任何一次「正常法力」支出之后，`unusedMana` 恰变为 0 且本次支出 > 0 ⇒ 双方督军各触发一次**（`actingCard` = 花钱那一方的督军）。我们落成**唯一扣能量口** `RuleCore.SpendNormalMana`（出牌 / 战术卡 / 付费前缀·誓约 / 维护费四处共用），触发走既有队列（`FireTriggerAt` → Instant 道），今天**0 张卡**有这条 ability ⇒ 休眠。
2. **`initialHandModifier` / `initialManaModifier`（＋`initialSpiritStoneModifier`）建好了。** 数据列、`CardDto`、`CardDef`、生成器、两处消费点（起手张数 / 能量上限 / 开局灵魂石）全接；1,351 张真值**全 0** ⇒ 生成器**只在非 0 时落列**，卡表**字节不变**（`--check` 现读「一致 ✅」）。
3. **`skipCardDraw` 的生产者找到了并补上。** 原版**写点** = `abilityLogic[].handBuff.cardEffect.buffType == BuffType.skipCardDraw(7)`（`AbilityLogic__PlayAbility.c:1377-1414` → `BattleManager.AddHandEffect` → `PlayerHand__AddHandEffect.c:88-139` 进 `PlayerHand.activeEffects(+0x48)`）。我们补了：生成器列 `skipDraws` ＋ `CardDef.SkipDrawSpec` ＋ `RuleCore.GrantSkipCardDraw` ＋ 在 `FireTriggerDeferred`（触发唯一出口）里挂载。**消费端本来是好的**（`ConsumeSkipCardDrawHandEffect`），今天**0 张卡**有 `buffType == 7` ⇒ 休眠。
4. **`EnforceHandLimit` 现核：`T03·d` 那句是对的，`T03·b` 那句「白名单外 8 处」是过期账。**
   `grep -rn "EnforceHandLimit(" d:/4/Unity/MyGame/Assets --include=*.cs` = **1 处**，就是 `RuleCore.cs:2775` 的**定义本身**；其余 16 处全是注释（含 `RuleEngineTest.cs` 的「灭自证」文案）。⇒ **全仓 0 调用点**，没有未收口的先加后丢，**未改**。

---

## 2. 「原版语义 → 我们实现 → 今天有没有卡走到」

### 2.1 `SpentAllMana`（`AbilityTrigger.SpentAllMana = 641`）

| 原版语义（判据） | 我们实现 | 今天有没有卡走到 |
|---|---|---|
| **判据点**：每次 `PlayerManager.UseNormalEnergy(amount)` 之后，**扣完的余额 == 0 且 `0 < amount`** ⇒ 触发（`PlayerManager__UseNormalEnergy.c:100-124`；反汇编 `0x18063F3CE test eax,eax / jne` ＋ `0x18063F3D6 cmp [rbp+0x28],esi / jle`） | `RuleCore.SpendNormalMana(ctx, p, amount, source)` —— 扣完判 `ps.Energy == 0` | **否**。全池唯一带 641 的卡 = 战役附魔 `Factories Overdrive`（**不在 1127 张池内**） |
| **唯一的法力出口**：`UseMana(…, ManaType.Normal, …)` → `UseNormalEnergy`（`PlayerManager__UseMana.c:97`）。**灵魂石**（`ManaType.SpiritStone=5`）走另一支，**不触发** | 四个扣能量点全部改调 `SpendNormalMana`：`PlayCardCore` / `EffectResolver.PlayTacticCore` / `EffectResolver.ResolveOneCore`（付费前缀·誓约）/ `BeginTurn`（维护费）。灵魂石扣点在 `ResolveOneCore` 的 `kind == "spirit"` 支，**未动** | **否**（同上） |
| **触发主体**：`TriggerSpentAllMana` 被调**两次** —— 己方督军 + **对方督军**，`actingCard` 都是花钱那一方（`PlayerManager__UseNormalEnergy.c:100-124`） | `FireSpentAllMana` 对 `ctx.Players[*].Warlord` 逐个 `FireTriggerAt(..., SpentAllMana, …, subject: 花钱方督军)` | **否** |
| **投递**：`RawCardScript.OnTrigger(0x281 = 641)`（`CardScript__TriggerSpentAllMana.c:24-28`） | `FireTriggerAt` → `ActionQueue`（道 = `CardAbility.priority`，缺省 1 = Instant） | **否** |
| **余额不足 ⇒ 余额写成 0**（不是「整笔不扣」；反汇编 `0x18063F31C xor edx,edx` + `0x18063F381` 写回 `[rbx+0x4c]`） | `SpendNormalMana` 不足支：`paid = 原余额; ps.Energy = 0` | **否**（维护费全池 0 张） |
| **卡面写法** = 原版枚举名 `SpentAllMana:` | `KeywordTable.SpentAllMana = "spentallmana"`，进 `Prefixes` / `RoutableTriggers` / `BodyKeywords`（三处都写了「全池 0 命中」的实测） | 0 张 |

### 2.2 开局加成三列

| 原版语义（判据） | 我们实现 | 今天有没有卡走到 |
|---|---|---|
| **起手张数** = `ScenarioVariables.startingHand(+0x1C) + 督军 initialHandModifier(+0x118) + （不是玩家那一手 ? secondExtraCards(+0x20) : 0）`，第三实参非 0 时整个覆盖（教程）—— `PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:35-76`。⚠️ 那段把负修正并进 `base`、正修正并进 `handMod`，**两种情况下总数都是 `startingHand + mod`** ⇒ 落成一个整数项 | `NewBattle`：**逐座位** `seatHand[seat] = max(0, startingHand + 督军 mod)`，交替发牌按 `max(seatHand)` 轮转。教程那一支（`tutorial != null`）**未动** | **否**。1,351 张 `initialHandModifier` **全 0** ⇒ 两座位都 = `startingHand`，逐字节等价改前 |
| **初始法力** = `ResetMana(模式基数(startingMana/Second) + 督军 initialManaModifier(+0x11C) + 教程关卡增量, …)`（`BattleManager__SetupInitialMana.c:20-46`） | `BeginTurn` 的 `MaxEnergy` 式子加**常量项** `督军.InitialManaModifier`（我们是每回合重算，常量项 ⇔ 原版「一次性抬高、之后逐回合累加」） | **否**（全 0） |
| **初始灵魂石** = 同一个 `ResetMana` 的第 4 个实参（`+0x128`） | `NewBattle` 开局一次性 `SpiritStones += 督军.InitialSpiritStoneModifier` | **否**（全 0） |
| `initialPsychicModifier(+0x120)` / `initialPlasmaModifier(+0x124)` | **不建模** —— 原版这两个货币在当前 build 是死功能（`UsesPsychicEnergy()` 恒 `xor al,al;ret`、`ProcessManaBuff` 不读那两个 flag），1351 张也全 0（判据 → `W58_回合流程_三处未定判据.md` §二） | 0 张（原版本身就是死的） |

### 2.3 `skipCardDraw` 的生产者

| 原版语义（判据） | 我们实现 | 今天有没有卡走到 |
|---|---|---|
| **谁写** = `abilityLogic` 的逻辑类型 `0x32`(50) 时，把 `AbilityLogic.handBuff(+0x60)` 复制一份、盖 `isPlayer`/`+0xA8=1`，调 `BattleManager.AddHandEffect`（动作 `0x19`）—— `AbilityLogic__PlayAbility.c:1377-1414` | **数据列** `skipDraws: [{keyword, uses, limited, source}]`（生成器从 `handBuff.cardEffect.buffType == 7` 产出）＋ **引擎** `RuleCore.GrantSkipCardDraw`（登记一条 `CardInstance.HandEffect`，`Op.Verb = "skipcarddraw"`、`Uses = {Limited, Left, Max}`） | **否**。1,351 张 `handBuff.buffType` 只有 **0 / 2 / 3**，**没有 7** |
| **落到哪** = `PlayerHand.activeEffects(+0x48)`（记录表）＋ 同时按 `targetCriteria` 逐张贴到手牌上的牌（`PlayerHand__AddHandEffect.c:88-139`） | 只登记进 **`PlayerState.HandEffectRecords`**（= 原版 `activeEffects` 的对应物）。**不逐张贴牌**：`buffType 7` 管的是「这一方这个回合抽不抽」，对**某一张手牌**没有语义 —— 原版那半只为 UI 显示，**如实不建模** | — |
| **谁读** = 扫 `+0x48` 找 `buffType == 7` 的**最后一条** ⇒ 不抽；`limitedUses(+0x28)` 真则 `numberOfUses(+0x2C)--`，`<1` 整条摘（`PlayerHand__CanDrawTurnStartCard.c:56-99`） | **原本就有**（`RuleCore.ConsumeSkipCardDrawHandEffect`，2026-10-20 已建），**未动** | **否** |
| **挂载时机** = 那条 ability 自己的动作 | `FireTriggerDeferred`（所有触发唯一的出口）**正文之前**调 `GrantSkipCardDraw` | **否** |

---

## 3. 证据（原版判据逐条）

| # | 判据 | 出处 |
|---|---|---|
| 1 | `SpentAllMana` 的投递与 `this/actingCard` | `CardScript__TriggerSpentAllMana.c:24-28`（`OnTrigger(0x281, …, this, actingCard, this, 0)`） |
| 2 | 「花光」判据 = 余额 0 ＋ `0 < amount`；**两次**调用（双方督军） | `PlayerManager__UseNormalEnergy.c:100-124` |
| 3 | 余额不足 ⇒ **写回 0** | 反汇编 `0x18063F2DC cmp/jg`、`0x18063F31C xor edx,edx`、`0x18063F381` 编码写回 `[rbx+0x4c]`（工具 `工具/disasm_va.py`，VA `0x18063F240` ← RVA `0x63F240`） |
| 4 | 所有正常法力支出都经过它 | `PlayerManager__UseMana.c:97` · `BattleManager__PayCardCost.c:27` · `PayActiveAbilityCostOath.c:27` · `PayCardPlayedCostOath.c:22` · `PlayerManager__TurnStart.c:110` |
| 5 | `AbilityTrigger.SpentAllMana = 641` | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs:77`（`dump.cs:20313`） |
| 6 | 唯一带 641 的卡 = 战役附魔 | `资料/普查产出_1018/allcards_1350/cards_raw.json` 逐条扫 `trigger == 641` ⇒ 2 条 ability，都属 `Factories Overdrive`（`m_Name = Z-AstraMilitarum-Manufactorum Overdrive_enchantment`、`manaCost -1`、`cardType 20`、`spellType 200`） |
| 7 | 起手张数公式（含 `initialHandModifier` 的正负两支） | `PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:35-76` |
| 8 | 初始法力的三实参 | `BattleManager__SetupInitialMana.c:20-46`（`ResetMana(mana+0x11C, plasma+0x124, psychic+0x120, spiritStone+0x128)`） |
| 9 | 字段偏移 | `dump.cs:23282-23286`（`initialHandModifier 0x118` · `initialManaModifier 0x11C` · `initialPsychicModifier 0x120` · `initialPlasmaModifier 0x124` · `initialSpiritStoneModifier 0x128`） |
| 10 | 三个 modifier 真值全 0 | `cards_raw.json` 的 `full_raw` 逐张扫：`initialHandModifier` / `initialManaModifier` / `initialSpiritStoneModifier` = **0 / 1351** |
| 11 | `skipCardDraw` 的生产链 | `AbilityLogic__PlayAbility.c:1377-1414` · `BattleManager__AddHandEffect.c`（`BattleAction` 类型 `0x19`） · `PlayerHand__AddHandEffect.c:88-139` |
| 12 | `BuffType.skipCardDraw = 7` | `dump.cs:45441` |
| 13 | 消费端（已存在） | `PlayerHand__CanDrawTurnStartCard.c:56-99` ↔ `RuleCore.ConsumeSkipCardDrawHandEffect` |
| 14 | 全池 0 张 `buffType == 7` | `cards_raw.json` 逐张扫 `handBuff.cardEffect.buffType` ⇒ `{0:1809, 2:63, 3:1}` |
| 15 | `EnforceHandLimit` 现状 | `grep -rn "EnforceHandLimit(" Assets --include=*.cs` ⇒ **1 处 = 定义本身** |

---

## 4. 没查清 / 仍开着的口（如实标着）

1. **`SpentAllMana` 的 `actingCard` 在我们这边只落成 `FireTriggerAt` 的 `subject`** —— 原版那个参数会参与 `actingCardCriteria` 过滤（`Factories Overdrive` 两条 ability 正是靠
   `friendlyUnits:1/includeHero/includeSelf` vs `enemyUnits:1/includeHero` 区分「打自己督军 2」和「打对面督军 2」）。
   我们的 `CardDef` **不导出 `actingCardCriteria`** ⇒ 若将来真有这种卡，**这一维筛不出来**（会两边都触发）。
   还差：把 `actingCardCriteria` 带进卡数据（跨数据管线），或至少对 `SpentAllMana` 这一族显式支持。
2. **`skipDraws` 的 `trigger → 关键词` 映射覆盖面**：生成器复用 `ABILITY_TRIGGER_KEYWORD`（21 条），
   `ThisCardPlayed(0)` / `TurnStart(30)` / `SpentAllMana(641)` **都不在**里面 ⇒ 真有这种卡时**不落列并出声**
   （`report["unmapped"]`，⛔ 不编造键名）。离线实测（造了 trigger=0 与 trigger=641 两条假 handBuff）：
   两条都进 `unmapped`，`rows = 0`。还差：给「打出时 / 回合开始 / 花光法力」这一族补关键词通道。
3. **`initial*Modifier` 的 `CardDatabase` 端到端没走过真数据**（全 0 ⇒ 卡表里没有这三个键）。
   机制由 `CardDef` 构造直传的夹具卡验（`TestW61bInitialModifiers`）；「生成器→JSON→`CardDto`」这一段今天**无法用真卡验**。
4. **`Factories Overdrive` 的附魔能力归属**：`TriggerSpentAllMana` 是发在**督军卡**上的（`CardScript.rawCard.OnTrigger`），
   而 641 的两条 ability 挂在**附魔卡**上 —— 原版靠 `RawCardScript.TriggerAbilities` 的**附魔那一趟**把附魔能力算进来
   （我们 `FireTriggerAt` 里 `Enchantments.Matching` 那一段）。**我们这条路径没接 641**（`FireSpentAllMana` 只对督军发），
   因为附魔/战役今天不在池内。没查清：战役附魔进池时要不要把 641 也发给附魔卡。

---

## 5. 改动清单（逐文件）

| 文件 | 改了什么 |
|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/CardDef.cs` | ① 新常量 `KeywordTable.SpentAllMana = "spentallmana"`（长判据 doc）② `Prefixes` / `RoutableTriggers` / `BodyKeywords` 各加一格 ③ 新属性 `InitialHandModifier` / `InitialManaModifier` / `InitialSpiritStoneModifier` ④ 新类型 `CardDef.SkipDrawSpec` + 属性 `SkipDraws` + `SkipDrawFor(keyword)` ⑤ 构造函数加 4 个可选参数并赋值 |
| `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs` | ① 新 `SpendNormalMana`（唯一扣能量口 + 花光判定 + 不足清零）与 `FireSpentAllMana`（双方督军）② 新 `GrantSkipCardDraw` ③ `PlayCardCore` / `BeginTurn`（维护费）改走 `SpendNormalMana` ④ `BeginTurn` 的 `MaxEnergy` 加 `InitialManaModifier` ⑤ `NewBattle` 起手张数改**逐座位**、加开局灵魂石 ⑥ `FireTriggerAt` 的「有没有东西可跑」闸加 `skipDraw`（并处理 `ops==null && spec==null` 的两处 NRE 风险：`what` 与 `emit.amount`）⑦ `FireTriggerDeferred` 开头挂 `GrantSkipCardDraw` ⑧ 就地订正维护费那段「不够就整笔不扣」的错注释（铁律 5） |
| `Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs` | 三个扣能量点改调 `RuleCore.SpendNormalMana`：`PlayTacticCore` / `ResolveOneCore`（`kind=="energy"`） |
| `Unity/MyGame/Assets/RuleEngine/Data/CardDatabase.cs` | `CardDto` 加 3 个 int + `SkipDrawDto[] skipDraws`；新 `SkipDrawDto` 类；`MapSkipDraws()`；`new CardDef(...)` 传新参数 |
| `Unity/工具/gen_cards_engine.py` | 新常量 `INITIAL_MODIFIER_COLS` / `SKIP_DRAW_BUFF_TYPE`；新函数 `skip_draws_of()`；entry 里落「非 0 才写」的三列与 `skipDraws`；跑完摘要加两行（**只落在有值时才写列** ⇒ 卡表字节不变） |
| `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_T03b.cs`（新） | 4 条用例：`TestW61bSpentAllMana` / `TestW61bSpendInsufficientDrainsToZero` / `TestW61bInitialModifiers` / `TestW61bSkipCardDrawProducer`（每条带 🧨） |
| `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`（只加用例表） | `Run()` 里加 1 个 `Section` + 4 个 `Step(...)`（8 空格缩进，过 `gen_diag_runner` 的 `CASE_CALL` 口径） |
| `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_Diag.cs` | **由 `工具/gen_diag_runner.py` 重生成**（282 条）；diff = 只加我那 4 条，⛔ 未手改 |

**卡表 `cards_engine.json`：一个字节没动** —— 生成器 `--check` 现读「一致 ✅」，两次生成一致。

---

## 6. 顺手发现（只报、除第 1 条已就地修外都不改）

1. ✅ **【已就地修】原版 `UseNormalEnergy` 余额不足 = 清零，不是「整笔不扣」。**
   2026-10-20 那一版（`RuleCore.BeginTurn` 维护费）把反编译里的 `iVar3` 读成了「要扣的额度」，它其实是**扣完的余额**
   （正常支 `iVar3 = mana - amount`，错误支 `iVar3 = 0` 再写回 `unusedMana`）。反汇编实证见 §3 第 3 行。
   ⇒ 已改并留订正痕迹；今天维护费 0 张卡 ⇒ 无可见影响，但口径照原版。
2. **`T03·b` 台账里「白名单外 8 处 `EnforceHandLimit` 仍先加后丢」是过期账** —— 现核 **0 调用点**（`T03·d` 的「11 处全收口、全仓 0 调用点」才对）。建议主对话把 `T03·b` 那一句划掉。
3. **`PlayerHand__CanDrawTurnStartCard` 里 `sentinel` 那道闸是【内联】的**（与 `IsThereSentinel` 同一段扫描，`0x29e` = `DefinedTrait.sentinel = 670`），
   **不是**两次函数调用 —— 原版 `TurnStartCardDraw` 里 ②③ 两条其实是同一个判据的两次读。我们分成 `HasSentinelInPlay` + `ConsumeSkipCardDrawHandEffect` 两处，
   行为等价，但**别**据此以为「原版扫了两遍」。
4. **`ruleprobe` 的结算基线（`play`）已陈旧**：`基线/out_play_baseline.txt` mtime **20:52**，而 `Core/` 里
   `ActionQueue.cs`(22:59) / `BattleContext.cs`(23:00) / `PlayedThirdCardInPlay.cs`(23:05) / `ActionPump.cs`(23:09)
   都在它之后被**别的写手**改过（`git status` 里那几个 M / `??` 就是他们）。⇒ `play check` rc=1 是**基线没跟上**，
   不是本笔引入（本笔所有新路径在今天的数据下**全 null**，且 `check` 的「输出指纹」那条独立哨兵**仍是 c9f6c642… 一致**）。
   **重钉**是调度台的动作（`PinnedPoolMd5` / `PinnedBaselineMd5` / `PinnedParseMd5` 三个常量），我**没动**。
5. **`cards_raw.json` 里 `handBuff` 只有 `buffType` 0/2/3**（1809/63/1 条）—— 也就是说，历史上 Warpforge **可能从来没在正式卡上用过 `skipCardDraw`**（只给战役/未实装内容留了口子）。如实记着。
6. **`initial*Modifier` 三个「缺席 ⇔ 0」的等价不成立时要小心**：本笔为此**没有**提 `version`（与 v9..v13 的「缺列要出声」相反）。
   理由写在 `gen_cards_engine.py` 的 `INITIAL_MODIFIER_COLS` 上方；将来若有人把 0 改成「缺值哨兵」，这一条要一起改。

---

## 7. 自检输出摘要

| 命令 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_f6 bash d:/4/Unity/工具/typecheck.sh` | **运行时错误数 0 · 编辑器错误数 0**（首跑曾出现 1542 条编辑器错，**全部集中在别的写手当时正在写的 `RuleEngineTest_Order.cs`**（`UnitState.PlayOrder`），隔一会儿重跑即 0 —— 正是铁律 13·3 那条「独立 TMPDIR 解决不了源码互相可见」） |
| `bash d:/4/Unity/工具/ruleprobe.sh check` | **rc=0** · `=== 哨兵合计：失败 0 条 ===` · `=== 全池 1121 张 · 解析差异 0 行 · 改名 0 · 新增 0 · 消失 0 ===` · ⑦ 输出指纹 `c9f6c642…` **一致**（⇒ 我加的 3 个关键词格**没有改动任何一张卡的解析**） |
| `bash d:/4/Unity/工具/ruleprobe.sh play check` | **rc=1** —— 主档 **1 张**（`DA28`：`💰 手牌总价 P0 6→0` vs 基线 `7→1`，即**费用列**差异）· 事件电池 **28 张**（`camouflage` 关键字消失、伤害累计不同等）。判据：**基线陈旧**（见 §6.4）；本笔在「今天的数据」下所有新路径都不可达。⛔ **没有**重生成基线（那会静默抹掉别人的回归） |
| `python -I -X utf8 工具/gen_cards_engine.py --check` | **一致 ✅**（新增摘要两行：`开局加成 … 0 张` · `抽牌封锁 skipDraws 落值 0/1127 张`） |
| `tool ruleprobe.sh seg "SpentAllMana: Gain +2 Attack"` | `unparsed=` 空 ⇒ **`SpentAllMana:` 这个前缀解析得出来**（`gain` op，载荷 `[+2 attack]`） |
| `PYTHONIOENCODING=utf-8 python 工具/gen_diag_runner.py` | 282 条用例（含新增 4 条）重生成 `RuleEngineTest_Diag.cs` |

> ⚠️ **未跑** `RuleEngineTest.Run`（铁律 12：本阶段不做验收动作）⇒ 新增的 4 条用例**只编过、没跑过**，
> 同步点请跑 `RuleEngineTest.Run`（期望在现基线 6207 上 **+N 条断言**，具体条数以实跑为准）。
> ⚠️ 首跑的 1542 条编辑器错提醒：**同一时刻有别的写手在改 `Editor/`**（`RuleEngineTest_Priority.cs` 现读 `M`），
> 同步点复跑 typecheck 时若又集中报在别人的文件上，按 §13·3 判「那不是我的」。

---

*本报告只写结论与证据；源码改动都在白名单内（`Assets/RuleEngine/**` · `工具/ruleprobe/**` 未动 · `gen_cards_engine.py`），
未改 `CLAUDE.md` / `项目任务.md` / 任务线正本，未动 git，未跑 Unity。*
