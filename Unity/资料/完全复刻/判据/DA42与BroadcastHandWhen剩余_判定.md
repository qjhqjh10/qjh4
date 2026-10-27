# `DA42` 附魔机制 与 `BroadcastHandWhen` 剩余族 —— 判定（`B9_R2DA42` · 2026-10-27 · 只读）

> 作者 = 查判据代理 `R2DA42`。**本笔没有改任何 `.cs` / 账 / 资产**，只写这一份判据 + `报告/B9_R2DA42.md`。
> 上游判据件：`判据/BroadcastHandWhen早退洞_普查.md`（下称「普查件」，六族 F1–F6 的原始结论）。
> 🔴 **本件对普查件有两处订正**（铁律 5）：`§一·2` 的「三个前提」措辞（② 混了两件不同的事）、
> 以及普查件说「DA42 今天看不到效果」—— **实际今天会立刻 +3 任务点**（见 `§一·3`）。
>
> **口径声明**：`cards_truth.json`（SO 1,351 张，**每卡一份、无 `full_raw`**）用于效果树统计；
> `cards_engine.json`（池 **1,128** 张）用于「在不在池 / `abilityTriggers` / `desc`」。
> 凡「全池 N 张」= 池 1,128；凡「全快照 N 行」= SO 1,351。两套分母不许混。
> ⚠️ **行号漂移提醒**：本笔读代码时 `Enchantment.cs` / `EffectResolver.cs` **正被别的写手并发修改**（`git status` 里是 `M`）。
> 本件所有 `RuleEngine/**` 的行号都是**当时现读**；再过一轮写手后请**按符号名定位**（`BroadcastHandWhen` / `DoChooseCard` /
> `DoFactionResource` / `Enchantments.Matching` …），⛔ 别拿行号当锚点改代码。`decomp_full/` 与数据侧的引用不受影响。

---

## §一 · `DA42 Reconnaissance Mission` —— 附魔机制

### §一·1 原版逐句语义（🥇 数据 + `.c`，全链 7 跳，每跳都有出处）

| 跳 | 原版做什么 | 判据 |
|---|---|---|
| ① **打出 DA42** | `trigger 0 (ThisCardPlayed)` 的 ability，`chosenAbility 30 = buffOthers`，`targetCriteria{targetsAffected **50 inHandFiltered**, enemyUnits 1, includeMinions 1, includeSpells 1, maxTargets 1, **selectFromChooseResolution 1**}` | `cards_truth.json` `DA42` · `AbilityEffect.cs`（`buffOthers = 30`）· `TargetsAffected.cs`（`inHandFiltered = 50`） |
| ② **目标怎么定** | `GetTargets` 的 `ta == 0x32(50)` 支 = `GetCardsInHandList(bm, friendly, enemy, minions, spells, …, 0)`（按 criteria 筛手牌）；随后 `criteria +0x80`（= `selectFromChooseResolution`）非零 ⇒ **再按名字收敛到「选牌结果那一张」**（`bm + 0x2E8` 的那张 `rawCardData.name(+0x38)`） | `decomp_full/AbilityLogic__GetTargets.c:893-905`（`ta==0x32` 支）· `:1101-1128`（`selectFromChooseResolution` 收敛）· `dump.cs:23872`（该字段 `// 0x80`） |
| ③ **挂附魔** | 对该手牌 `CardScript` 调 `AddEffect{ buffType **15 singleUseEnchantment**, enchantment → PPtr **-1426406806713108198** }`；`buffType 0xc(12)/0xf(15)` 直接 `CardScript__AddEnchantment`（**无区域闸**，手牌实例照挂）⇒ 写 `+0x110`／`+0x108`、`enchatmentsUsed` 取号并 +1、`ActivateTraitsOnSummonOrEnchantment(onSummon:0)` | `decomp_full/CardScript__AddEffect.c:826-828`（`case 0xc/0xf → AddEnchantment`）· `CardScript__AddEnchantment.c:25,37,44,48,51,63,91` |
| ④ **承载卡被打出时派发** | `BroadcastCardPlayed` 逐张手牌调 `ResolveCardPlayed`；**`thisCard == cardPlayed` 那一支**才调 `TriggerAbilities(trigger **0**, …, isEnchantment 0)` | `decomp_full/CardScript__ResolveCardPlayed.c`（首句 vtable `+0x308` = `OnCardPlayedWithTarget`）· `RawCardScript__OnCardPlayedWithTarget.c:43`（`thisCard==cardPlayed` 块内的 `TriggerAbilities(…,0,…)`）；**别的**手牌拿的是 trigger **10**（同文件 `LAB_18094a2e6` 段） |
| ⑤ **附魔自己结算** | `TriggerAbilities` 先跑自己的 ability、再**落进共享尾块** `LAB_18094b822` 扫 `thisCard.activeEnchantments(+0x110)`：`IsSpentEnchantment` 不是全花 ⇒ 逐条 `CanTriggerAbility` + `ReachedQuestCounter` ⇒ `TriggerAbility`（`enchantmentCard` 作为第 8 实参） ⇒ `AbilityLogic.PlayAbility` | `decomp_full/RawCardScript__TriggerAbilities.c:42-53`（jam 闸 + 尾块入口）· `:202`（自己那趟跑完 `goto LAB_18094b822` —— **两趟都跑**）· `RawCardScript__TriggerAbility`（`AbilityData.baseEnchantmentCard = 附魔卡`，`thisCard = 载体`） |
| ⑥ **+3 任务点给谁** | 附魔卡 `e8cf1eb5…` 的 `abilities[0]` 两行**都挂 `trigger 0`**：① `chosenAbility 60 buffMana{manaAmount 3, **enemyPlayer 1**, **questPoints 1**}` ② `chosenAbility 340 removeThisEnchantment`。`buffMana` 支：`AddManaBuff(bm, **abilityData.thisCard.isPlayer(+0x40)**, thisCard, manaBuff, priority, block)`（入队）→ `ResolveManaBuff`：`enemyPlayer` 非零 ⇒ **把那一格取反** → `ProcessManaBuff`：`param_2==0 ⇒ bm+0xC8(enemyManager) / else bm+0xC0(playerManager)` → `AddQuestPoints(该方, manaAmount)` | `decomp_full/AbilityLogic__PlayAbility.c:685-718`（`chosenAbility 0x3c` 支；`:708` 乘目标数 `local_178`，`:190` 里 `multiplier.ta==any ⇒ local_178=1`）· `BattleManager__AddManaBuff.c`（尾部 `AddAutoActionToQueue`）· `BattleManager__ResolveManaBuff.c`（`enemyPlayer ⇒ cVar2=='\\0'` 取反）· `BattleManager__ProcessManaBuff.c:24-31`（选 PlayerManager）· `:76-78`（`+0x1d questPoints ⇒ AddQuestPoints`）· `dump.cs`（`BattleManager.playerManager // 0xC0` / `enemyManager // 0xC8`；`EntityScript.isPlayer // 0x40`） |
| ⑦ **摘掉** | `340` = `AddRemoveEnchantment` → `AddAutoActionToQueue`（**入队，不是当场摘**）⇒ 与 `AddManaBuff` 同一条道 FIFO ⇒ **先 +3、后排摘** | `Enchantment.cs:1039-1071` 的 `EnqueueRemovePray` doc 已把这条链读全（同一族）；`AbilityLogic__PlayAbility.c:2250-2282` 的 340 支 |

**一句话**：打出 DA42 → 从**对手手牌**挑一张（候选受 criteria 约束，`maxTargets 1`）→ 给**那张手牌实例**挂一条 15 类附魔 →
**那张牌被打出时**，附魔的 `trigger 0` 结算：**+3 任务点给 DA42 的施放方**（= 载体属主的「敌方」）→ 附魔自己摘掉。

**订正（普查件 §6·F3 表头）**：`playersAffected 20 (enemyPlayer)` 这个读法是**错的起因**——
`buffMana` 支**根本不读 `playersAffected`**（`AbilityLogic.cs` 的 Tooltip 写的是「DrawCard, DiscardWholeHand」，
`.c:685-718` 也只读 `manaBuff(+0x68)` 与 `thisCard.isPlayer(+0x40)`）。
**极性只来自 `manaBuff.enemyPlayer`**（DA42 模板那一格 = 1）。`playersAffected` 是无关字段。

### §一·2 「三个前提」——按可施工粒度重拆成 5 条（P0–P4），逐条给「我们现在有没有」

> 普查件的三条（挂手牌 / `trigger 0` 结算口 / 极性）**方向都对**，但 ② 把两件不同的事并成一句；
> 且**漏了第 0 条**（数据侧：模板卡根本不在池里、`enchantment` PPtr 没有列）。下面按 0→3 排。

| # | 前提 | 原版判据 | **我们现在有没有** | 差什么（可执行） |
|---|---|---|---|---|
| **P0** | **模板卡在池里 + 卡片能表达「我要挂哪张附魔」** | 模板 `e8cf1eb55ca41534abe38efc90b302e9` 是一条 `cardType 20` 的隐藏 SO；原版 `CardEffect.enchantment(+0x88)` 直接指它 | 🔴 **两条都缺**：① `cards_engine.json` 里**没有**这张模板（`'e8cf1eb5' in 文件` = **False**；池内 `type=enchantment` 只有 `Pray Enchantment` **1 张**）② `cards_engine.json` 的 56 列里**没有** `enchantment` 列（只有 v33 的 `enchantTrait`），`CardDef` 也没有对应字段 | ① `工具/gen_cards_engine.py` 的 `ENCHANTMENT_EXECUTORS`（`:3989`）加一条 DA42 执行器（`ids=("e8cf1eb5…",)` · `pairs=((0,60),(0,340))`）⇒ 模板入池（门 = `:3981` 的 ①引用 ∧ ②形状命中执行点，**这正是它今天不入池的原因**：`gen_cards_engine.py:3964-3972` 白纸黑字「31 张有池内挂载者但形状不支持」）② 挂载侧的「哪张附魔」按 Pray 同款做法用**常量 id + 池内按 id 查**（`Enchantments.PrayEnchantmentCardId` 的形状），**不必新开列** |
| **P1** | **附魔能挂到手牌实例** | `AddEffect case 0xf → AddEnchantment`，**无区域闸**（本笔亲核：`CardScript__AddEnchantment.c` **全文 100 行、`cardState`/`0x228` 命中 0 次**） | ✅ **容器层已经有**：`CardInstance.ActiveEffects` / `ActiveEnchantments`（`CardInstance.cs:139/142`，两块 list 随实例走手牌→场上→弃牌堆）；`Enchantments.Attach(ctx, **CardInstance** carrier, …)`（`Enchantment.cs:228-315`）签名的载体就是 `CardInstance`；`ActivateTraitsAfterEnchant`（`:1008-1017`）**只在载体在棋盘上时才调** trait 重算 ⇒ 挂手牌**天然安全**。⚠️ `excludeEnchantments` 判据也已经按实例读（`TargetCriteriaEval.cs:355-367`） | 🔴 **缺的是「调用点」**：`Enchantments.Attach` 的**生产调用者只有 `AttachPray` 一个**（`grep Enchantments.Attach` 全仓：`RuleCore.cs:12027` + `Enchantment.cs` 自身）⇒ DA42 那条 `buff{buffType 15}` **没有任何一层会去挂**。补法 = 在 DA42 的 trigger-0 本体里对「选中那一份手牌实例」调 `Enchantments.Attach(ctx, inst, tmplCard, BuffTypeSingleUse, source:…)` |
| **P2** | **承载卡被打出时，派发它身上附魔的 `trigger 0`** | `OnCardPlayedWithTarget.c:43`（`thisCard==cardPlayed`）→ `TriggerAbilities(0)` → 共享尾块扫 `+0x110`（`TriggerAbilities.c:49/202`） | 🔴 **缺**。现有等价物是 `RuleCore.FireTriggerAt` + `Enchantments.Matching(**UnitState**, kw)`（`RuleCore.cs:12475` · `Enchantment.cs:467`）—— **只认棋盘上的 `UnitState`**；而 DA42 的载体是**手牌/刚打出的 `CardInstance`**。另外：`PlayTacticCore` 走的是**文本 op**（`EffectResolver.cs:2473` `EffectText.Parse(card.Desc)`），**根本没有 `TriggerAbilities(0)` 这一跳**；`RuleCore.PlayCardCore`（`:3920`，单位支）同理 | 加一个**按 `CardInstance`** 的附魔派发口（`MatchingForInstance(inst, triggerName)`）+ 在两个出牌入口各挂一次：`EffectResolver.cs:2575`（战术 `BroadcastWhen(Play)` 之后）· `RuleCore.cs:4087`（单位同名广播之后）。⚠️ **不要**把它塞进 `BroadcastWhen` —— 那个函数只拿到 `CardDef` + `UnitState`，拿不到被打出的那份实例 |
| **P3** | **附魔能力的「执行体」** | 模板的两行是 `chosenAbility 60 buffMana` / `340 removeThisEnchantment`（数据驱动） | 🔴 **缺，而且是本链最大的一条**：我们**没有被动 `cardAbilities[].abilityLogic[]` 的解释器**（`EffectResolver` 只解释 `activeAbility` / `alternativeAbility` 的 `chosenAbility`，v23/v24）。`Pray Enchantment` 是**按 id 硬编码**的（`RuleCore.GrantPrayEnchantmentFaith` + `Enchantments.RemovePray`），**不是**数据驱动 | 两条路（见 `§三` A1a / A1b）：**A1a** 照 Pray 硬编码 DA42 模板（1 张卡，最省）；**A1b** 建通用被动解释器（解锁剩下 30 张模板，见 `gen_cards_engine.py:3964-3972` 的缺口清单） |
| **P4** | **极性的那一格**（普查件的③） | 逐句见 `§一·1⑥`（`.c` 三处）：`enemyPlayer` ⇒ **相对「载体属主」取反** | 🟡 **半有**：任务点加值本体在 `EffectResolver.DoFactionResource`（`:12675-12828`，含阈值 `QuestPointThreshold` `:13042` 与「每 1 点广播一次」`:12803-12805`）；但「换边」只有**一种输入**：`op.Target.Side == "enemy"`（`:12686`，相对**施放方**） ⇒ **没有「相对载体属主取反」这一格** | 给 `DoFactionResource` 抽一个显式形参版（`gainOwner` + 来源卡的**属主边**），DA42 传 `gainOwner = 载体属主 ^ 1`；⚠️ 抽函数必须**零行为变化**（现有调用点指纹不变） |

**可达性（现读，单份口径）**：`cards_truth.json` 全卡递归扫 `buff.enchantment` ⇒ **130 行 / 114 张父卡**；
其中 `buffType == 15` **44 行**，**在池 37 行** = **24 行 Pray**（模板 `path 1528516720421242597`）+ **13 行非 Pray / 13 张卡**；
`targetCriteria.targetsAffected == **50 (inHandFiltered)**` 的**全快照只有 1 行 = `DA42`**（任何 buffType 都算上）。
⇒ **「附魔挂到手牌」这一族今天可达 = 1 张（DA42）**，判据铁。

### §一·3 我们今天的实际行为（订正普查件 §6·F3 的「看不到效果」）

DA42 今天**不是静默的**，而是**错时机 + 少一半语义**：

| 今天会发生 | 出处 |
|---|---|
| `Choose a card in your opponent's hand` 解析成 `choosecard`（`ChooseSrc="enemyhand"`）⇒ **只挑出来、什么都不做**（日志明写「本版只挑出来了」） | `EffectText.cs:6476`（`head.Contains("opponent's hand") ⇒ "enemyhand"`）· `EffectResolver.cs:4742-4756`（`DoChooseCard` 的 `case ""`，日志原文点名这张卡） |
| `When played, gain 3 Quest Points` **照常被主解析器解成 `gainquest 3`** ⇒ **打出那一刻立刻 +3**（给施放方），与「选中的那张牌何时被打出」**毫无关系** | `EffectText.cs:8745-8753`（`whenclause` 那道闸故意放它过去：`played` 省主语 ⇒ 认不出事件 ⇒ **必须继续由主解析器解成 `gainquest 3`**）· `CardDef.cs:6004-6006`（同一句的另一种记法） |
| 附魔：**从不挂、从不摘、承载卡被打出时什么都不发生** | `§一·2` P0–P3 |

⇒ **验收夹具的「红」要盯两条**：① 打出 DA42 的**当刻**不该加任务点；② 承载卡被打出**之后**才 +3（且附魔消失）。
现在这两条**都会红**（今天加值提前、且永不二次结算）。

---

## §二 · `BroadcastHandWhen` 六族 —— 逐族回代码现核

> 纪律：普查件的结论**两个方向都会过期**（第八十五会话实测 6 条里 5 条「写着缺其实已补」）。
> 下面每一族的「现状」都是**本笔现读的代码行 + 现算的数据**，不引普查件的判断当结论。

### F1 · 武装陷阱被抽到 ⇒ `250 ThisCardDrawnAsTrap`
- **现状：✅ 已做**。`BroadcastHandWhen` 的预扫已含 `|| (drawMode && h[i].ArmedTrap != 0)`（`EffectResolver.cs:10661`）；
  `hop 0` 且 `inst == drawnCard` 时调 `EmitTrapDrawnInsteadOfCardDrawn` 并 `continue`（**取代 260**）：`:10685` → 实现 `:16520-16545`。
- **还差什么**：**无**。⚠️ 但它只发**事件**、不执行效果 —— `trigger 250` 的效果层判据本来就不存在（`:16510-16513`：池 / SO 两处 `trigger==250` 均 **0 行**，`spellType 70` **0 张**，本轮复算 == 0）。
- **已被后续批次做掉**：是，第三批（代码注释自记「账 250（第三批）」）。

### F2 · `ArmedTrap == -1`（判不了）的出声路径
- **现状：✅ 已做**（普查件 §6·F2 自己写的「第五批」）：预扫从 `== 1` 放宽成 **`!= 0`**（`:10661`）；
  三条告警改走 `NoteTrapDrawnOnce`（`:16561-16566`，`HashSet _trapDrawnNoted` 追加在类尾 `:16558`），**同一张卡同一类只喊一次**。
- **还差什么**：无。⚠️ **零可观察**（池内 `ArmedTrap != 0` = 0 张）—— 所以它今天只影响日志，**不要拿它当「行为修好了」的证据**。

### F3 · 非单位卡的**多段**正文里的 `When …`（= **DA42 那一族**）
- **现状：🔴 仍缺**（与普查件一致，已现核）：注册端 `EffectText.SplitHandTrapWhen` 仍要求
  `c.IsUnit == false`（`EffectText.cs:5731`）+ **`segs.Count != 1 ⇒ return null`**（`:5733`）+ 事件短语 `ParseAll` 认得（`:5741`）。
  DA42 的 desc 是**两句** ⇒ 永远进不了 `HandTrapWhens`；而它那句 `When played` 又**故意**不注册监听器（`:8748-8753` 的立意）。
- **复算可达性（现读，单份）**：非单位卡 desc 含 `when` = **10 张**：`For the rest of this battle` **4** · `Lower cost by 1 when` **4** ·
  单段 `When you play a Stratagem…` **1**（`68909cf0…` Jammed Communications，已进 `HandTrapWhens`，**正对照**）· **`DA42` 1**。
  `defence`/`enchantment` 里无 `when` 开头的（`§F5`）。
- **还差什么**：**就是 `§一` 那一条链**（P0–P4），**不是**这条广播的洞。
- **已被后续批次做掉**：**否**（账 1287 仍 `C`；`账/战斗逻辑驱动.tsv` 里 `DA42` **自身 0 行**，只有 1287 的备注提到它）。
- ⛔ **仍然不要**放宽 `SplitHandTrapWhen` 的 `segs.Count == 1` —— 理由见普查件 §3·F3（会与 ①/②/`ResolveAtTurn` 重复触发）。

### F4 · 非单位卡的结构化 listeners（除 250）
- **现状：✅ 结论仍成立（没有「没被接住的手牌态监听者」），但口径要写全**。本笔复算（`cards_engine.json` 486 张非单位）：
  `abilityTriggers` 逐 trigger 命中**行数** = `0:481 · 10:1 · 31:1 · 40:2 · 60:1 · 90:13 · 145:6 · 190:1 · 240:3 · 410:18 · 500:4 · 600:13 · 605:14 · 650:1`
  ⇒ **带任一非 0 trigger 的卡 = 76 张**（一张卡可命中多个 trigger，所以行数 ≠ 卡数；普查件写的 75 是旧口径）。
  其中**只有两条**落在「原版从手牌派发」的那个 trigger 集合（`{0,10,190,250,260}`，判据 = `ResolveCardPlayed` / `CardDrawn` 两条 .c）：
  - `68909cf02f472734ea391665e0d28e15` **Jammed Communications**：`trigger 10`（`chosenAbility 10 doDamage 1` 打己方督军）—— **已由 ③ `HandTrapWhens` 接住**（同一句 `When you play a Stratagem, your Warlord takes 1 damage`，单段）。另有一条 `trigger 0` 的 `chosenAbility 3 (doOnlyVfx)` = **纯动画**。
  - `AM55` **Mechanised Infantry**：`trigger 190`（`chosenAbility 40 buffSelf` + `buff{buffType 2, costChange:-1}` + `actingCardCriteria{keywordFilter 10}`）—— **已由 ① 事件型降费（`CostWhens` / `BroadcastCostWhen`）接住**（`Lower cost by 1 when you deploy a Vehicle`）。⚠️ 它的另几行 `trigger 0 → summon AM5 ×3` 走卡面正文（`Deploy three Shock Trooper`）。
  - `250/260` 在非单位卡上 = **0 行**。
- **还差什么**：**结论零缺口**，但要补一条**口径注**（普查件已自标）：这是「trigger 值 + desc 形态」分类 + 抽样读，**不是**逐卡读 `targetCriteria.targetsAffected`；
  而且下面这条**新轴**不在它的覆盖面里（见 `§二·补`）。
- **已被后续批次做掉**：F4 本身在第五批查清（结论「0 族」）；**本轮复算仍为 0 族**。

### F5 · `defence`(39) / `enchantment`(1)
- **现状**：现读池 `type` 分布 = `unit 586 / tactic 446 / hero 56 / defence 39 / enchantment 1`（Σ **1,128**）。
  `defence` + `enchantment` 里 desc 以 `When(ever)` 开头的 = **0 张**（本笔现算）。
- **还差什么**：无。别再改。

### F6 · 武器卡 / 低语卡
- **现状**：`cards_engine.json` 的 `cardType` 分布 = **只有 `0 Minion 586 / 10 Hero 56 / 20 Tactic 486`** —— **没有 30 / 40**；
  `type` 也没有 `weapon` / `whisper`。⇒ **数据里不存在**（不是「没找到」）。
- **还差什么**：不适用。将来出现时它们自动落「非单位」侧 ⇒ 继承 ③（单段 `When`）+ F3 的限制。

### §二·补 · 普查件 F4 **覆盖不到的另一条轴**（如实标：不是本洞的族，但同属「附魔/挂载」）
> 这条**不属于** `BroadcastHandWhen` 早退洞（F1–F6 都是「谁在听事件」），但它是 `DA42` 的同族前提，
> 而且普查件 F4 的口径（只看 `abilityTriggers` + desc 形态）**看不见**它。列在这里免得下一个人以为「F4 = 全清」。

- **`buff.enchantment` 全快照 = 130 行 / 114 张父卡**，按 buffType 分：
  `12` = **71 行**（其中 `spellType 200`（进攻/环境卡）**50 行、全不在池**；`spellType 0` **20 行 / 17 张在池**；`spellType 240` **1 行 / 1 张在池**
  ⇒ 在池的 `buffType 12` 父卡共 **18 张 / 21 行**）
  · `15` = **44 行**（37 在池 = 24 Pray + 13 非 Pray）· `None` 12 行 · `3` 2 行 · `1` 1 行。
- **今天真正「挂上去」的只有 Pray 那一族**（`Enchantments.AttachPray`）。其余：
  - **`buffType 12` 的 18 张在池父卡**（`Cadia Stands` / `Wages of Retribution` / `Pledge to the Dark Prince` / `Undying Legions` /
    `Hallowed Martyrs` / `Hive Fleet Leviathan` / `Oath of Moment` / `Coterie of the Conceited` …）——
    原版走「把效果挂到一张活着的载体牌身上」，**我们走的是 `PersistentEffects` 等自设计替代**（`EffectResolver.cs:6774-6825` 那段块注释已如实标「原版没有 `PersistentEffect` 这个类型，这是我们自设计」）。⇒ **不是新账**，是**已登记的自设计偏离**（`A1950`/`A1955`）。
  - **`buffType 15` 的 13 行非 Pray / 13 张在池**（含 DA42）—— 这段**没有**替代物：`Enchantments.Attach` 的唯一调用者是 `AttachPray`。
    ⚠️ 这些卡的**卡面正文**另有解析路径（`Give X to …` 等），是否等价**本笔没有逐张验**（不在白名单范围）⇒ **记为待查**，见 `§四`。
- **一处独立的缺口（顺手发现，已在别处有账的影子）**：`Enchantments.Spend` / `AllSpent`（`Enchantment.cs:418/440`）**全仓 0 生产调用点**；
  原版 `SpendEnchantment` 的两处调用点是 `CardScript__AddEffect.c:896/917`（`spellType 130` 那一支）与 `AbilityLogic__PlayAbility.c:2504`（`chosenAbility 0x118 = 280 revealOrder`）。
  现读：池内 `spellType 130` = **0 张**、`abilities[].chosenAbility 280` = **0 行** ⇒ **今天零可观察**；本文不立新账，只留给 532/1044 那条线核。

---

## §三 · 可施工清单（执行代理可直接照做；每条标阻塞）

> ⚠️ **前置阻塞（共同）**：`Enchantment.cs` / `EffectResolver.cs` / `CardDef.cs` / `RuleCore.cs` 目前列在
> `待接线与排队项.md` 的「**等引擎文件解封**」那一节 ⇒ 施工前先确认调度台已经把这四个文件的所有权交出来。
> ⚠️ **`gen_cards_engine.py` 一旦改名/加列，池指纹会变** ⇒ 只能在「没有账写手」的同步点重钉（`c2ee753d…` 那一颗）。

| # | 做哪个文件 | 加哪一句 / 改什么 | 怎么验 | 阻塞 |
|---|---|---|---|---|
| **A1a** | `工具/gen_cards_engine.py` | `ENCHANTMENT_EXECUTORS`（`:3989`）加一条：`{"name":"Reconnaissance Mission","ids":("e8cf1eb55ca41534abe38efc90b302e9",),"pairs":((0,60),(0,340)),"why":…}` | 重跑生成器 ⇒ 池 **1,128 → 1,129**，报告里那张「未入池原因」的计数 **31 → 30**；`ruleprobe check` 基线 +1 卡（**需重钉基线**） | 与 A1b 二选一；**卡在生成器所有权 + 重钉时机** |
| **A1b** | 新建 `RuleEngine/Core/PassiveAbility.cs`（+ `EffectResolver` 一处分派） | 被动 `cardAbilities[].abilityLogic[]` 解释器（至少支持 `chosenAbility 60 buffMana` / `340 removeThisEnchantment` / `40 buffSelf`），数据源 = 新列 | 全池对拍 `B9_全池对拍_差异.tsv` 里**首列是附魔模板 uniqueId 的 91 行**（现读；80 个模板中的一部分）从「未验到」变「已验到」 | 大件（解锁 30 张模板）；**先 A1a 后 A1b** |
| **A2** | `RuleEngine/Core/Enchantment.cs`（**类尾追加**） | `public const string DA42EnchantmentCardId = "e8cf1eb5…";` + `public static CardDef FindDA42Enchantment(BattleContext ctx)`（照 `FindPrayCard` `:716-728` 的形状）+ `public static void OnCarrierPlayed(BattleContext ctx, CardInstance carrier, int owner)` | 见 A6 的夹具 | ⛔ 依赖 A1a（模板不在池里就查不到） |
| **A3** | `RuleEngine/Core/Enchantment.cs`（**类尾追加**） | `public static List<ActiveEnchantment> MatchingForInstance(CardInstance carrier, string triggerName)` —— 与 `Matching(UnitState,…)`（`:467-477`）**同一条口径，但吃 `CardInstance`**；DA42 模板**没有卡面正文**（不在 `cards_engine` 的 OCR 层）⇒ 认它走 **`EnchantmentCardId` 白名单 + 模板 `abilities[].trigger` 的等价物**（A1b 之后读数据列；A1a 阶段直接按 id 认「它有 trigger 0」） | 自检里对一个人造 `CardInstance` 断言 `MatchingForInstance(inst,"ThisCardPlayed").Count == 1` | ⛔ 依赖 A1a/A1b |
| **A4** | `RuleEngine/Core/EffectResolver.cs` | 在 `PlayTacticCore` 的 `BroadcastWhen(Play)`（`:2575`）**之后、`EnqueueAbility(tactic-body)`（`:2638`）之前**调 A2 的 `OnCarrierPlayed(ctx, inst, p)`；同一句**同样加在** `RuleCore.PlayCardCore` 的 `BroadcastWhen(Play)`（`RuleCore.cs:4087`）之后 | 现有 14 宿主跑一遍（战术/单位两条出牌路都覆盖）；`play all dump` 里 DA42 那一行的事件序列变化要**只有** DA42 | 阻塞 = 引擎文件所有权 |
| **A5** | `RuleEngine/Core/Enchantment.cs` + `EffectResolver.cs` | `OnCarrierPlayed` 体内：① 对每条命中附魔 `EnqueueAbility(…, () => { /* +3 任务点 */ })` ② 再 `EnqueueRemove(ctx, carrier, effect, ActionPriority.Instant)`（**顺序照原版 FIFO：先加、后排摘**，见 `EnqueueRemovePray` 的 doc `:1049-1052`）。**+3 的落点** = 把 `DoFactionResource` 的任务点那一支抽成显式形参版，DA42 传 `gainOwner = 1 - OwnerOfCarrier`；广播 subject 走「来源卡属于另一方 ⇒ **收点数那方督军**」那一支（`:12783-12788`） | 夹具（见 A6） | 阻塞同上；⚠️ 抽函数必须**零行为变化**（`play all dump` 逐字节不变） |
| **A6** | `RuleEngine/Editor/RuleEngineTest_*`（新一条） | 夹具：手牌放 DA42、对手手牌 3 张 ⇒ 打出 DA42、选一张 ⇒ 断言「那张实例 `ActiveEnchantments.Count == 1`」；**对手打出那张** ⇒ 断言「我方 `QuestPoints` **+3**、附魔 `Count == 0`」；**灭自证**：把 A4 的钩子改回「打出即给」⇒ 必须红（现在就是那个行为） | `ruleprobe play` rc=0 + 宿主自检 | 需要 Unity 跑（**调度台执行**，子代理不跑） |
| **A7** | `RuleEngine/Core/EffectText.cs`（**只许在去掉「立即 +3」那一步做**） | ⛔ **在 A4/A5 落地前不许动**。落地后：`When played, gain 3 Quest Points` 必须**不再**由主解析器产 `gainquest`，而是作为「附魔挂载卡的触发条件」——**仍然不要**注册监听器（别回到 `whenclause` 死路）；做法 = 在 `CardDef` 侧给「带 `enchantment` 引用的卡」把这一句**收进附魔族**并让主解析器跳过 | 同上两条断言（打出当刻 = 0 任务点） | ⛔ 严格串行在 A5 之后；顺序反了 = 「两个来源都给 3 点」 |
| **B1** | 账 / 判据件 | F1 / F2 **登记核销**（「已做」写进 `账` 与普查件的对应行），⛔ 不再排工 | — | 无 |
| **B2** | `工具/`（可选加固） | 把「非单位卡里 `abilityTriggers` ∩ `{0,10,190,250,260}`」的扫描固化成一支只读小工具/断言，防止 F4 的结论随数据漂移过期 | 输出 = 今天那 2 张（`Jammed Communications` / `AM55`） | 无（纯工具） |
| **B3** | `EffectText.cs` 注释（**文档级**） | `:8751` / `:8765` 把 `DA47 Reconnaissance Mission` 改成 **`DA42 Reconnaissance Mission`**（`DA47` 实为 `Blacksword Missiles`，见 `cards_engine.json`）——**铁律 5：记错了就地改**；行尾/行数不变（就地改写同行） | `grep -n "DA47 Reconnaissance"` = 0 | 卡在 `.cs` 写手（一行注释） |

**推荐顺序**：`B3`（一行注释）→ `A1a` → `A2`/`A3` → `A4` → `A5` → `A7` → `A6`（断言最后补，但**不晚于 A7**）。
**A1b** 单独立账（它解锁的是另外 30 张模板，不该挂在 DA42 名下）。

---

## §四 · 还差什么（真查不到的，明写）

1. **`buffType 15` 那 13 行非 Pray / 13 张在池卡**（`DA42` · `61046837… Helspear Assault` · `6899b1… Uge Choppa` · `84d4d1… Ferocious Rage` ·
   `9176318e… Litany of Despair` · `ASH72 Graceful Avoidance` · `BL74 Spreading Corruption` · `GOF56 Deffkopta` · `GOF91 Beastboss on Squigosaur` ·
   `GSC49 Telephatic Domination` · `SW46 Raid Tactics` · `SW56 Let Loose` · `c433e3… Master Outrider`）
   **它们的卡面正文是否已由别的通道等价接住** —— **本笔没逐张验**（属于「附魔挂载」这条轴，不是 `BroadcastHandWhen` 的族，也不在本笔白名单）。
   ⚠️ 同族但**不在池**的 7 行（`SW62 First Light` ×2 等）今天不可达，别把它们算进来。
   还差：逐张读 `desc` ↔ 模板 `abilities[].logic[]` ↔ 现有 op 的等价性核对。**判据入口**：本件 `§二·补` 的分桶 + `判据/B9_全池对拍_差异.tsv` 里首列是附魔模板 uniqueId 的那 91 行。
2. **`selectFromChooseResolution` 的「选牌结果」在哪个动作里写入 `bm + 0x2E8`** —— 本笔只读到**消费端**（`AbilityLogic__GetTargets.c:1101-1128` 按名字收敛）。
   `bm+0x2E8` 的**写点**没扫（需要 VA 级追写点或读 `BattleManager._ResolveChooseCard*` 协程）。
   影响：不影响 DA42 的最终语义（选哪张、给谁 +3 都不依赖它），只影响「同名牌多份时收敛到哪一份」这一档。
3. **`e8cf1eb5…` 模板的 `cards_engine` 列契约**：今天模板类卡片由 `build_enchantment_entries` **单独一条窄路**造 entry（`gen_cards_engine.py:3940-3960`）。
   它**故意不落** `animTimes` / `abilityTargets` / `abilities` 各 criteria（理由：那张卡永不进手牌/棋盘）。
   ⇒ 若 A1b 要走「读数据列」，**先要决定这些列落不落**（今天 `CardDef` 读出来是「未知」）。本笔**不代裁**。
4. **今天我们 `Enchantments.Matching` 用「附魔卡有没有该关键词的正文」来认「有没有这条 ability」**（`Enchantment.cs:467-477`）。
   DA42 的模板**没有卡面正文** ⇒ 这条判据对它**恒 false**。所以 A3 必须**另认一条路**（id 白名单或数据列），
   **不能**沿用 `Matching` 的 text 判据 —— 这一点本笔**只能给出方向，不能给出现成实现**（因为两条路都牵 P0）。

---

## §五 · 顺手发现（不在本笔任务范围，但值得记）

1. **`EffectText.cs:8751` / `:8765` 把 `DA42 Reconnaissance Mission` 写成了 `DA47 Reconnaissance Mission`** —— `DA47` 实为 `Blacksword Missiles`。
   两处都是**代码注释里的错误 id**（不是逻辑错）⇒ 建议就地改（`§三·B3`）。
2. **`Enchantments.Spend` / `AllSpent` 全仓 0 生产调用点**（见 `§二·补`）—— 今天零可观察（`spellType 130` / `chosenAbility 280` 池内均 0），
   但它意味着「`singleUse` 附魔花掉之后就不再派发」这道闸**从未生效**。留给 532 / 1044 那条线核。
3. **`gen_cards_engine.py:3964-3972` 已经把「附魔模板入池的 31 张缺口」写成白纸黑字**（「共同缺的那一跳是同一个：引擎没有被动 `abilityLogic` 解释器」）。
   ⇒ 本件 `§一·2 P3` 与它**是同一件事**，两根线别各修一半。
4. **`账` 里 `DA42` 自身 0 行**（只有 1287 的备注提到它）—— 按铁律 11「全部都要做、只分先后」，DA42 这条链应该**有一条自己的账行**
   （否则验收读数无处挂）。建议 `§三` 施工时一并补账，落点 `账/战斗逻辑驱动.tsv`。
