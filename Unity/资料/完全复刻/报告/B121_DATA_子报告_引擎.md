# B121-DATA 子报告 · 引擎侧（`RuleEngine/Core` + `RuleEngine/Data`）

> 普查代理 **B121-A（引擎侧）** · 2026-11-03 · **只读**（未改任何源码/数据/场景，未跑 Unity，未动 git）
> 问题：「战斗相关功能里，**已经有代码了、但还没有设计对应数据**（卡表/配置表/SO/JSON/本地化/资源）的有哪些」。
>
> **核实口径**：所有「池内 0 张」一律**现读** `Assets/RuleEngine/Resources/cards_engine.json`
> （v47 · `count = 1129` · 2026-11-02 01:02），扫描面 = 卡顶层 `keywords` / `cardTraits` / `spellType` /
> `chosenAbility` / `abilityTriggers` / `abilityTargets[].ta` + `abilityBuffs[]`／`enchantment[]`／`permanentBuffs[]` 的
> `buffType`（三处全扫）。⛔ 未用 `cards_raw.json`（`full_raw` 双份会翻倍）。
> 脚本：`python -I -X utf8` + `io.open(..., encoding='utf-8')`，结论见下表「实测」列。

## 一、总表

| 编号 | 功能 | 类别 | 代码落点(文件:行) | 缺什么数据 | 当前行为 | 原版判据(有/无 + 出处) | 补数据来源 | 优先级 |
|---|---|---|---|---|---|---|---|---|
| ENG-01 | 回合准备触发 `TurnSetup(155)` 三跳广播 | B | `Core/RuleCorePartial_B94B21.cs:71`（接线 `Core/RuleCore.cs:2019`） | `abilityTriggers` 里 155 = **0/1129**（实测 confirm） | `BroadcastTurnSetup` 全跑，但没有任何卡有 155 能力 ⇒ 只空转 | **有**：`decomp_full/BattleManagerSupport__BroadcastTurnSetup.c` 全文 + `CardScript__OnTurnSetup.c:1-172`；原版 SO 也 0/1351（`B91_L4`） | 无（原版数据本身没有）；出现带 155 的卡自动可达 | P0 |
| ENG-02 | `Support(550)` 召唤跳 | B | `Core/RuleCorePartial_B94B21.cs:150` | 550 trigger **0/1129**（实测） | 建好但无生产者 | **有**：`CardScript__ResolveUnitSummoned.c:46-60`；原版 0/1351 | 同上 | P0 |
| ENG-03 | `Combo(590)`（同回合二次打出） | B | `Core/EffectResolver.cs:18332` / `TryFireCombo` | 590 trigger **0/1129**（实测）；`DefinedTrait.combo=1130` 也 0 | 方法在、恒不触发 | **有**：`CardScript__NonTargetSpellPlayed.c:12-18` / `__TargetedSpellPlayed.c:14-20` | 无 | P1 |
| ENG-04 | `buffType 9 setHealth` | B | `Core/EffectResolver.cs:15108`（`DoSetHealth`） | `abilityBuffs.buffType` 实测值域 `{1,2,3,4,8,12,15,16,17,25,70,90,95,100}` ⇒ **无 9** | 已按真语义重写，恒不触发 | **有**：`CardScript__AddEffect.c:794-820`；`buffType 100 setCurrentHealth` 才是真生产者（`EC39`） | 无 | P1 |
| ENG-05 | `buffType 7 skipCardDraw`（`skipDraws` 列） | B | `Data/CardDatabase.cs`「`skipDraws`」段 + 消费端 | 卡表里该键**永不出现**（原版 `handBuff.buffType` 无 7） | 休眠 | **有**：`BuffType.skipCardDraw(7)`；原版 1,351 张无一张 | 无 | P2 |
| ENG-06 | `spellType 70 Trap` | B | `Core/EffectResolver.cs:17028` / `Core/CardInstance.cs:463` | `spellType` 值域 `{0,90,150,180,190,210,220,230,240,260,265,270}` ⇒ **无 70** | 只发事件 + 出声 | **有**：`CardDef.cs:6623` doc（`cardType 20 ∧ spellType 70`）；原版 0/1351 | 无 | P1 |
| ENG-07 | `spellType 120 Quest` + `490 QuestTriggered` 全场广播 | B | `Core/RuleCorePartial_B94B8.cs:10` / `Core/Enchantment.cs:196,497` | `spellType 120` = **0**（实测） | `BroadcastQuestResolved` 恒不触发；`HasQuests` 恒 0 | **有**：`BattleManager__BroadcastQuestResolved.c`（全文 60 行）；原版 0/1351 | 无 | P1 |
| ENG-08 | `spellType 15`（`abilityLogic 340 ∧ spellType 15` 那一支） | B | `Core/ActionPump.cs:1856-1865` | `spellType 15` = **0**（实测） | 判 D 的等效：本 build 不适用 | **有**：`ActionPump.cs:1856` 判据段；`cards_engine` 0 张 | 无 | P2 |
| ENG-09 | `spellType 200 OffensiveCard` 挂附魔链 | B | `Core/Enchantment.cs:1780`（`RunGrantedEnvAbility`）/ `:1834` | 可玩池 **0 张**；但旁表 `offensive_env_cards.json` **39/39 全 `spellType 200`** | 闸生效（保证不动可玩卡）；旁表 39 张已能走 `EnsureOffensiveEnvGrantsMerged` | **有**：`CardScript__AddEffect.c:826-828`；数据在旁表（不是缺） | 已齐 | P1 |
| ENG-10 | `chosenAbility 71`（`referenceCard`） | B | `Core/EffectResolverPartial_B88E2.cs:303,326` | `chosenAbility` 实测 0 张（值域无 71） | 唯一生产者 0 行 ⇒ 恒返回 `minDamage` | **有**：`RawCardScript.referenceCard(+0x110)` 全 1,351 空 | 无 | P2 |
| ENG-11 | `chosenAbility 85`（`transformUnitWithBuff`） | B | `Core/EffectResolver.cs:19435` / `Core/TransformUnit_B94B12.cs:430` | `chosenAbility` 无 85（实测 0） | 休眠件 | **有**：`TransformUnit_B94B12.cs` 判据段 | 无 | P2 |
| ENG-12 | `chosenAbility 350`（`removeFromCemetery`） | B | `Core/EffectResolver.cs:19559` / `Core/RuleCore.cs:17069` | 实测 0 | 休眠件，0 调用点 | **有**：`EffectResolver.cs:19559` doc | 无 | P2 |
| ENG-13 | `chosenAbility 370 Relentless` / `400 Courtesy` | B | `Core/EffectResolver.cs:18785` | 实测 **370 = 0 · 400 = 0** | 两词已进 `Prefixes`+`Implemented`，无卡 | **有**：`EffectResolver.cs:18756` | 无 | P2 |
| ENG-14 | 20+ 个「已实现/已登记」trait 全池 0 张 | B | `Core/CardDef.cs:8686`（`Prefixes` 登记）· `Core/CardDef.cs:8058`（`Implemented`）· `Data/SimpleAI.cs:637,654,689` · `Core/UnitState.cs:1159` | 实测 **0/1129**：`jam` `transient` `psyker` `reactor` `massive` `poisonous` `sacrifice` `markerlight` `survivor` `resistant` `poisoned` `droppod` `ward` `wardwb` `tainted` `summonrift` `coward` `precognition` `requiem` `giantkiller` `noncombatant` `pindown` `mutation`（`unstoppable`/`oathdouble` 各 1 张，可达） | 闸/结算分支全在，入口恒 0 ⇒ 零可观测 | **有**：逐词判据在 `CardDef`/`RuleCore` doc；原版 `DefinedTrait` 枚举有，但 SO 数据同样 0 | 无（原版也休眠）；出现即自动可达 | P0（机制已就位，只差卡） |
| ENG-15 | AI「special attack scoring」`AttackScoringCriteriaScore` | B | `Data/SimpleAI.cs`（献祭支 `SacrificeBonus` 那一行）+ `Data/AiScoring.cs:863` | `specialAttackScoring` **1129/1129 = 0**；`ai_scoring.json` 只有元数据、无打分列 | 恒 0，AI 打分少一档 | ⚠️ **有字段无真值**：`CardDef.SpecialAttackScoring` 的 doc 自陈「真值 1,351 张全 0」 | 无（真值就是全 0） | P2 |
| ENG-16 | 先手判据 `FirstSeatRules.ByInitiative` | B/❓ | `Core/FirstSeatRules.cs` | `initiative` **1129/1129 = 20**；源表 `数据/游戏数据/card_initiative.json` 的 `counts.byValue = {20: 1127}` | 全池同一档 ⇒ 这一位**永不产生分叉**（退回后续判据） | ❓ 原版 `RawCardScript.initiative` 枚举有 0/10/20/30/40，但**当前 build 快照全 20** —— 抽取字段是否正确**未查清**（需 VA/反汇编核对 `+0x??`） | 需复核抽取字段，不是「缺表」 | P1（先手影响对局） |
| ENG-17 | **附魔挂载数据（`CardEffect.enchantment` PPtr）** | **A** | `Data/CardDatabase.cs`（`EnchantmentTemplateAbilityDto` / `enchantment` 列）· `Core/Enchantment.cs:1273,1539` | 真值 **114 张 / 130 行**（`cards_truth.json` 实测）；卡表 `enchantment` 列只有 **54 张 / 58 行** ⇒ **池内 9 张缺**：`ASH43` `ASH46` `BL49` `GOF104` `GOF31` `GOF41` `GOF90` `SAU52` `UM82`（另 45 张不在池） | 这 9 张「打出即挂附魔」**不生效**；其余靠 3 张硬编码白名单 | **有**：`CardScript__AddEffect.c:826-828`（`buffType ∈ {12,15,16} ⇒ AddEnchantment`）+ `buffEffect.enchantment` PPtr | `gen_cards_engine.py` 扩 `enchantment` 列的匹配（这 9 张的 `buffType` 为 `None/3`，现生成器只认 12/15） | **P0** |
| ENG-18 | `🔶 3 张「打出即挂附魔」白名单` | C | `Core/Enchantment.cs:1539-1580`（`PlayerGrantedEnchantments`） | 只有 `SAU53` / `TL47` / `SW64` 三张硬编码 | 其余 111 张挂载者未挂 | 原版走数据列（见 ENG-17） | 与 ENG-17 同一补法：列落地即删本表 | **P0**（与 ENG-17 合并做） |
| ENG-19 | `🔶 DA42 的附魔触发闸（卡 id 白名单）` | C | `Core/Enchantment.cs:1273`（`IsEnchantTriggerClause`） | 无数据列 ⇒ 硬编码 `DA42` | 只对这一张卡生效 | 原版 `CardEffect.enchantment` PPtr（同 ENG-17） | 同上 · 调度台 2026-10-27 裁定 | P1 |
| ENG-20 | `TargetCriteria.spellType` / `conditionsFilter` 两格 | **A** | `Core/CardCriteria.cs:101,116`（字段 `CardCriteria.SpellType` / `ConditionsFilter`） | `cards_engine.json` **没有 `targetCriteria` 列**（只摊了 `unitConditions`）⇒ 这两格**结构性就位、零行为** | 恒 `-1`（不筛） | **有**：`FilterMethods__CheckIfMeetsCriteria.c:286-308`（`spellType` 独立一格）/ `MeetsConditionCriteria` | `gen_cards_engine.py` 落 `targetCriteria` 列（`abilityTargetCriteria` 里另两格已在） | P1 |
| ENG-21 | `ta = 0 (any)` 多目标表 | **A（注释判据错）** | `Core/RuleCore.cs:17919` 注释称「`0 any` 池内 0 张 ⇒ 不接线」；`AbilityTargetTable` 对 0 返回 `null` | **实测 224 行 / 209 张**有 `abilityTargets[].ta == [0]`，其中 244 条有配对的 `abilityTargetCriteria` 行 | 全部回落到改前单数路（未崩，但**注释的理由不成立**） | ❓ 原版 `AbilityLogic__GetTargets.c` 的 `targetsAffected==0` 支**未逐句确认**（`.c` 是 1,427 行大 switch，本次只查到 `:76-90` 的分派头） | 先回源确认原版 `any` 语义，再决定接不接 | **P0**（影响面 209 张，需复核不接线是否正确） |
| ENG-22 | 「`Ability:`」文本档 / `PeekNextCards`（只看不抽） | B | `Core/RuleCore.cs:11357` / `:15804` | 卡面 `desc` 里以 `Ability:` 起句的 **0 张**；`Peek` **0 张**（实测） | 落点存在，走不到 | **有**：`AbilityLogic__AddDrawCard.c`（`Peek` 那一半） | 无 | P2 |
| ENG-23 | 手牌侧 `handBuff` 除 `changeCost(2)` 之外的落点 | B | `Core/RuleCorePartial_B115W20.cs:333` | 现读**池内 0 行**会落到该 `default` 分支 | 出声「手牌侧无落点」+ 不改状态（不静默） | **有**：`PlayerHand__AddHandEffect.c:126-135` | 无 | P2 |
| ENG-24 | `deathType == combatAttacker(175)` 死亡触发 / `640 EarnSkull` 广播 | B | `Core/EffectResolver.cs:8211` / `:16838` | 监听 175 的卡 **0 张**；`abilityTriggers` 里 **640 = 0**（实测；`630/645/647/650/655` 有） | 发射点已补，可玩池无监听者 | **有**：`RawCardScript__OnMinionDeath.c:25-26` / `CardScript__CheckForSkullEarned.c:1-69`；⚠️ 640 的真值 **17 行 / 12 张**（`cards_truth` 实测）全在**环境卡**（`Earthquake`/`Star Orbit`…），不在可玩池 | 无（环境卡那条链见 ENG-27） | P2 |
| ENG-27 | 附魔通用派发 `TriggerEnchantment`（原版两个调用点） | B/❓ | `Core/Enchantment.cs:1324`（**0 调用点**，doc `:1315-1319` 自陈） | ① `TutorialStage.playerTurnEndEnchantment`（`tutorial_stages.json` 里**有** 6 处，但引擎没接）② `activeAbility.loadingAbilityLogic`（`cards_engine.json` **无此列**，`needsLoading` 133 行**全 0**；`cards_truth` 也没抽出该键 ⇒ 是否有非空真值 **未查清**） | 结构照抄，两跳都空转并出声 | **有**：`RawCardScript__TriggerEnchantment.c:1-22` + `CardScript.cs:14810/14827` | ①接教程关卡那一列（教程线）②`loadingAbilityLogic` 需回 SO 真值确认后再落列 | P1 |
| ENG-25 | 死列 `eventSpriteName` / `refNameId` | B | `Data/CardDatabase.cs:278,266`（`[Obsolete]`） | 当前快照这两列**全空**（0/1129；`refNameId` 的 70 条全在未入池的 GUID 卡上） | 无消费者、也无值 | **有字段**：`RawCardScript` 同名 | 需远端 / GUID 卡入池才有值；⛔ 别拿旧快照「44 张」当现读 | P3 |
| ENG-26 | `🔶 自设计数据（非原版）` | C | `Data/StarterCards.cs:26`（26 张自设卡）· `Core/GameplayVariables.cs:194,250,444`（Skirmish 加时回合等服务器值）· `Data/SimpleAIPartial_B120W8.cs:91,108,124`（AI 难度区间）· `Core/RuleCore.cs:14997`（`MatchData` 两数）· `Core/Enchantment.cs:1539` | 判据在**远端 / 服务器**，本机 0 命中 | 已是「有代码、数据是我们自己定的」 | 原版值在远端 Addressables / PlayFab | 关服 ⇒ 只能自定（铁律 18·0 允许的门类） | P3 |

> **未列入本表（由 `B121-DATA` 自查，我不碰）**：`CheckForHeroDeckTransform`（E03）、`Carnifex` / `TL20` 的选项卡
> （`choice_option_cards.json` 57 张 · 池内 0/57）。相关落点仅作指针：`Core/ChoiceOptionCards_B116W32.cs:29`、
> `Data/CardDatabasePartial_B116W32.cs:68`。

## 二、重点条目详情

### ENG-17 / ENG-18 / ENG-19 —— 附魔挂载列缺一半（唯一**真缺数据**的 P0）
- 原版：`CardScript__AddEffect.c:826-828`，`buffType ∈ {12,15,16}` 时把 `buffEffect.enchantment` 那张模板挂到目标 `+0x110`。
- 真值（`cards_truth.json` 现扫）：**114 张卡 / 130 行**带 `enchantment` PPtr；其中 **63 张在我们池内**。
- 我们的卡表（`cards_engine.json` 现扫）：`enchantment` 列只有 **54 张 / 58 行**（`buffType` 仅 12=21、15=37）。
- ⇒ **池内 9 张卡的挂载数据压根没落列**：`ASH43 Sudden Assault` · `ASH46 Aerial Dominion` · `BL49 Delightful Agonies` ·
  `GOF104 Drag it Down` · `GOF31 Cloud of Smoke` · `GOF41 Unbridled Carnage` · `GOF90 Wurrboy` · `SAU52 Self-Destruction` ·
  `UM82 Sergeant Gadriel`（真值里它们的 `buffType` 是 `None`（8 张）/`3`（1 张）⇒ 生成器只认 12/15，这一族被漏掉）。
- 现状：`Core/Enchantment.cs:1539` 用**3 张硬编码白名单**顶（`SAU53` / `TL47` / `SW64`）—— 注释自己写着
  「数据列一落地就删本表」。⇒ **补法 = 先把列补全，再删白名单**（顺序不能反，否则 3 张会回退）。

### ENG-21 —— `ta=0 (any)`：注释的判据是错的（不是「没数据」，是「数据有 224 行」）
- `Core/RuleCore.cs:17919` 原文：「其余 `ta`（含 `0 any` —— **池内 0 张**、原版休眠，裁定 ④ 不接线）」。
- 实测：`abilityTargets[].ta == [0]` 共 **224 行 / 209 张卡**；其中 **244 条**有配对的 `abilityTargetCriteria`。
  样例：`ASH79 Orian Laratharjos`（trigger 46）· `ASH34 Avatar of Khaine`（46）· `ASH55 Aspect Shrine`（0）。
- ⇒ 结论不是「无事可做」，而是**这条不接线的理由要重判**：要么原版 `any` 真的不由 `GetTargets` 处理（那就该把
  注释改成真理由），要么这 209 张卡一直走的是旧单数路 = 潜在缺口。**回源口径**：`AbilityLogic__GetTargets.c` 全文
  1,427 行的大 switch，本次只读到分派头（`:76-90`），`targetsAffected==0` 那一支**未逐句确认** ⇒ 判 ❓ 不判 A/B。

### ENG-16 —— `initiative` 全池 20：看起来「有数据」，其实这一位是死的
- `cards_engine.json`：`initiative` **1129 张全部 = 20**；源表 `数据/游戏数据/card_initiative.json` 的 `counts`
  自己写着 `{'pool': 1127, 'mapped': 1127, 'unknown': 0, 'byValue': {'20': 1127}}`。
- `Core/FirstSeatRules.cs` 的 `ByInitiative` 因此**永不产生分叉**（退回原版 ③④⑤ 掷硬币）。
- ⚠️ 不能直接判 B：原版 `Initiative` 枚举确实有 0/10/20/30/40 五档，而**当前 build 快照里全是 20** ——
  要么原版这一版真的全 20，要么我们抽错了字段（未反汇编核对偏移）⇒ 记 **❓ 需复核抽取**，别当「数据已齐」。

### ENG-14 —— 20+ 个「已实现但零张」的 trait（机制全在，只差卡）
实测 0/1129（`keywords` + `cardTraits` + `desc` 三层同扫 0 命中）：
`jam` · `transient` · `psyker` · `reactor` · `massive` · `poisonous` · `sacrifice` · `markerlight` · `survivor` ·
`resistant` · `poisoned` · `droppod` · `ward` · `wardwb` · `tainted` · `summonrift` · `coward` · `precognition` ·
`requiem` · `giantkiller` · `noncombatant` · `pindown` · `mutation`。
这些**不是待办**（铁律 11：机制先建、数据一变自动可达），列出来是为了让「复刻百分比」别把它们算成「没做」。

## 三、顺手发现（只报不改）

1. `Core/EffectResolver.cs:16838` 那句「17 张带 `trigger 640` 能力的卡（`Earthquake` / `Star Orbit` …，池内 0 张可达）」
   —— **经复核是对的**：`cards_truth.json` 的 `abilities[].trigger == 640` 实测 **17 行 / 12 张**，全在环境卡
   （`Earthquake` `Star Orbit` `Toxic Fumes` `Solar Eclipse` `Radiation Storm` `Thunderstorm` …），
   **一张都不在可玩池**；而这 17 行**已经在 `enchantment_templates.json` 里**（模板 ability 的 `trigger` 分布含 `640`×17）。
   ⇒ 数据是齐的；缺的是「谁来跑模板上的 640」= ENG-27 那族（`TriggerEnchantment` 0 调用点）。
2. `Data/CardDatabase.cs:3977` 写「本表（进攻卡）今天只有数据、还没有消费者」—— 现读
   `BattleDriver.offensiveCardEnabled = true`（`CardPresentation/Battle/BattleDriver.cs:9244`）、
   `ChosenOffensiveCardDef()` 已在生产调用（`:37479`）⇒ 该句**已过期**（属表现层，交调度台核）。
3. `cantBeRandomlyCreated`：池内 **167 张 = 1**，但原版唯一读点（`AbilityLogic.GetTargets` 的
   「`filterSpecificCards` 为空 ⇒ 回落全卡池」支）我们**还没建** ⇒ 这一列有值、无消费方（A 类候选，
   与 ENG-21 同属「`ta`/回落池」那一族）。

## 四、本次搜过什么（供复核）
- 代码面：`Assets/RuleEngine/Core/*.cs` + `Assets/RuleEngine/Data/*.cs` 全量 `grep`：
  `池内 0` / `0 张可达` / `休眠` / `等数据` / `数据未接` / `恒 0` / `没有生产者` / `未接线` / `🔶 自己设计`。
- 数据面：`cards_engine.json`（v47 · 1129）、`cards_truth.json`（1351）、`enchantment_templates.json`（86 模板）、
  `offensive_env_cards.json`（39）、`choice_option_cards.json`（57）、`ai_scoring.json`（1351 条仅元数据）。
- 原版面：`d:/2/tools/decomp_full/`（`AbilityLogic__GetTargets.c` · `CardScript__AddEffect.c` ·
  `BattleManagerSupport__BroadcastTurnSetup.c` · `CardScript__ResolveUnitSummoned.c` ·
  `BattleManager__BroadcastQuestResolved.c`）；`d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetsAffected.cs`。
- ⛔ 没做的事：没有改任何文件（除本报告）；没有跑 Unity；没有动 git / 活状态表 / 账。

---

## 五、主代理 `B121-DATA` 回源订正（2026-11-03，铁律 5）

1. **ENG-17 / ENG-18 / ENG-19 判 A（「附魔列缺 9 张」）不成立 —— 是假缺口。**
   主代理现读 `cards_raw.json` 的 `full_raw`：全表 `buffEffect.enchantment.m_PathID != 0` 共 **130 行**，其中
   `buffType ∈ {12,15,16}` 的只有 **115 行**；其余 **15 行**是 `buffType 0`×12 / `1`×1 / `3`×2。
   而那 9 张「缺列」卡恰好全部落在这 15 行里（`ASH43 ASH46 BL49 GOF104 GOF31 GOF41 GOF90 SAU52 UM82`）。
   原版 `CardScript__AddEffect.c` 只有 `case 0xc / 0xf` 进 `AddEnchantment` ⇒ **生成器 `gen_cards_engine.py:4478` 的
   `bt not in ENCHANTMENT_ATTACH_BUFFTYPES` 闸与原版同语义**，不是漏数据。
   ⇒ 池内 `enchantment` 列 54 卡 / 58 行与真值一致；ENG-18/19 的「白名单」是 `ruleprobe`/无数据层的兜底，
   不是「用白名单顶替数据列」。**下个会话别去补这 9 张。**
2. **ENG-21 的 `ta=0` 原版语义已确认**：`AbilityLogic__GetTargets.c:884-891` 的 `if (targetsAffected == 0)` 支 =
   `GetUnitsInPlayList(ref list, clearList 0, inclHero 1, inclMinions 1, inclHeroParts 1, inclWeapons 1,
   inclRemnants 1, inclPlayer 1, inclEnemy 1, includeDying 0)` = **双方全场的单位**。
   所以「池内 0 张」这句注释确定是错的；但是否 224 行全部由 `GetTargets` 消费仍需逐 case 核（部分 logic 自带目标）。
3. **ENG-27 的「`tutorial_stages.json` 里有 6 处 `playerTurnEndEnchantment`」措辞需修正**：
   现读是 **6 个 stage × 2 个字段 = 12 个键，值全部是 `matchTier: null-ref` 的空结构体** ⇒ 原版教程数据本身也是空的。
   这一条归 **B（原版也休眠）**，不是「数据在、代码没接」。
4. **ENG-14 的「markerlight / poisoned 池内 0 张」口径**：按 `keywords` + `cardTraits` 两列确为 0；
   `desc` 里出现的 15 处 Markerlight / 1 处 poisoned 是**引用词**，⛔ 不能当 trait 已落地。
5. **ENG-11 `chosenAbility 85` 等五档**：主代理复核 `chosenAbility` 全池集合，确认无 71/85/350/370/400（B 类成立）。
6. **新发现（不在原子报告里）**：`EnchantmentExecutor_B115.cs:77-109` 的 `ca` 执行器只接 30/40/50/340；
   模板 138 行 logic 里 75 行没有执行器（73 行无人跑）——这是「机制在、数据在、执行器缺一格」的**最大 A 缺口**，
   见主清单 B121-01 / B121-02。
