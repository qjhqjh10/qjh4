# `cardType` / `CardType` 判据点普查（只读 · 2026-10-27 · 执行代理 `E7ENG` 第四批）

> **触发**：批次 9 · 第三批刚把真值 `RawCardScript.cardType`（`int +0x94`）落成卡表新列 `cardType`（卡表 v34），
> 同步点要知道：池内新分布 **0:586 · 10:56 · 20:486 · 40:0** 会不会让**既有按 cardType 分支的地方**开始误判。

## 0. 方法与范围（搜了什么、没搜什么）

- **搜过**：`d:/4/Unity/MyGame/Assets/**/*.cs`（全仓 grep `cardType|CardType` = **409 处命中**，
  其中**代码行 96 处**、其余是注释/XML doc）＋ `d:/4/Unity/工具/gen_*.py`（**21 处**）。
- **逐行看过**：96 行 C# 代码 + 21 行 py；再把「**会决定行为**」的点单独列在 §2 / §3（注释与纯数据搬运不列）。
- **没搜**：`d:/2` 第三方的反编译与我们无关的工程；`.gd`（Godot 旧复刻）—— 都不是本仓行为。
- 行号是 **2026-10-27 现读**（`账` 那边漂移由 `check_ledger_points --fix` 收；本文件不参与闸）。

## 1. 🔴 决定性事实：池内 `type` 与 `cardType` 是**完全双射**

`CardTypeOptions`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardTypeOptions.cs` 现读）：
**`Minion = 0` · `Hero = 10` · `Tactic = 20` · `Whispers = 40`**。

`cards_engine.json`（v34，1,128 张）现读交叉表：

| 我们的 `type` | `cardType` | 张数 |
|---|---|---|
| `unit` | **0** (Minion) | 586 |
| `hero` | **10** (Hero) | 56 |
| `tactic` | **20** (Tactic) | 446 |
| `defence` | **20** (Tactic) | 39 |
| `enchantment` | **20** (Tactic) | 1 |
| `whispers` | **40** (Whispers) | **0** |

⇒ 两条结论，**后面每条判断都靠它们**：
1. **任何拿 `Type` 字符串当代理的写法，今天与 cardType 逐值等价**（`unit↔0`、`hero↔10`、`{tactic,defence,enchantment}↔20`）⇒ **不会因为新列而突然判错**。
2. 🔴 **反过来才有风险**：**`cardType == 20` 会同时命中 `tactic` / `defence` / `enchantment` 三种 `type`（486 张）**
   ⇒ 「用 20 代表某一种 type」的写法会**过筛 40 张**（39 defence + 1 enchantment）。**误筛方向永远是「多筛」，不是「漏筛」**。

## 2. ✅ 判得对、**不需要改**的点（逐条给判据）

| # | 落点（现读） | 它读什么 | 判定 |
|---|---|---|---|
| 1 | `RuleEngine/Core/TargetCriteriaEval.cs:1007-1016` `TypeGatePasses` | `type` 字符串 → `IncludeHero/Minions/Spells` | ✅ **唯一把三档写全的一处**：`tactic \|\| defence \|\| enchantment` 都归 `IncludeSpells`（= cardType 20）。doc 里也写明了双射。 |
| 2 | `RuleEngine/Core/WhenEvent.cs:1822-1836` `ActingCardTypeMatches` | `type` → `ActingHero/Minion/Tactic/Whispers` 掩码 | ✅ 主体正确（`tactic \|\| defence` ⇒ `ActingTactic`）；⚠️ **只漏 `enchantment`**，见 §3·2。 |
| 3 | `RuleEngine/Data/AiTargets.cs:774-782` `CardTypeOf` | `ai_scoring.json` 的 `cardType`（真值）优先；缺了按 `type` 映射（`unit→0 / hero→10 / else→20`） | ✅ 今天等价（`else→20` 对 `defence`/`enchantment` 也对，真值就是 20）。⚠️ 若将来出现 `whispers(40)` 会落到 `else→20`（错），见 §3·3。 |
| 4 | `RuleEngine/Data/AiTargets.cs:470` | `ct == 20 \|\| ct == 40 ⇒ 不入场`（AI 棋盘池） | ✅ 正确（棋盘上只有 0/10）。 |
| 5 | `RuleEngine/Data/AiTargets.cs:620-624` | `includeSpells/Hero/Minions/Whispers` 四格 | ✅ 正确（与 #3 同一 `CardTypeOf`）。 |
| 6 | `CardPresentation/Core/CardView.cs:12019` | `return d.isUnit;  // = cardType ∈ {0,10}` | ✅ 等价（双射）。 |
| 7 | `RuleEngine/Core/CardCriteria.cs:209` + `RuleEngine/Core/DeckRules.cs:164/174` | `IsWarlordCardType(type) = type == "hero"` = cardType 10 | ✅ 等价。 |
| 8 | `CardPresentation/Battle/BattleDriver.cs:7883 / 33985` | `!def.IsUnit` = cardType 20 那侧 | ✅ 等价（池内 `!IsUnit` ⇔ 20）。 |
| 9 | `CardPresentation/Battle/BattleDriver.cs:34811-34828` `CanUseVOs(string cardType)` | 我们的 **`type` 字符串**（`unit`→Troop VOs、`hero`→Warlord VOs） | ✅ 两档等价（原版那一格只分 0/10）。 |
| 10 | `CardPresentation/Core/CardFaceRules.cs:64` `CardTypeWhispers = 40` | 常量 | ✅ 值对；池内 0 张（该类 doc 已如实标）。 |
| 11 | `CardPresentation/Editor/CardFaceAssertE24.cs:210` | 断言「防御卡也是 `cardType 20` ⇒ 同一颗 `Tactic RaceText`」 | ✅ 与真值一致（defence 39 张全是 20）。 |
| 12 | `CardPresentation/Editor/{CollectionScene,DeckScene}` 的 `Type == "hero"` 断言（`CollectionScene.cs:5471/6854` · `DeckScene.cs:1656/1710`） | 排序键「战将」= cardType 10 | ✅ 等价。 |
| 13 | `RuleEngine/Core/CardDef.cs:6596-6652`（本批新增） | `CardType` / `IsTrapCard` = `cardType == 20 ∧ spellType == 70` | ✅ 新列的唯一「按数值」消费者。 |
| 14 | `RuleEngine/Data/CardDatabase.cs:619` + `:1404-1410`（本批新增） | DTO 列 + `< 34` 守卫 | ✅ 数据面。 |

**说明**：`CardPresentation/Shell/ProfileData.cs:112` 的 `anyCardType` 与 `:745` 的 `f.anyCardType == 0`
**不是** `CardTypeOptions` —— 那是成就 `Ach_89` 的「是否按卡型筛」开关（bool 语义）。⛔ 别混。

## 3. ⚠️ 有风险 / 可改进的点（**本批只查不改**，附准确修法）

### 3·1 `CampaignScriptRunner.CardTypeOf` —— **桩：一律返回 0**（原版这一格没接线）
- 位置：`CardPresentation/Battle/CampaignScriptRunner.cs:377-380`
  ```csharp
  public int CardTypeOf(CardDef card) { LogWarning($"`RawCardScript.cardType` 没有接线（`{card?.Id}`）⇒ 传 0"); return 0; }
  ```
- **意图**：原版 `voiceLines.ShowRadioMessage(tip, cardSprite, clip, (CardTypeOptions)rawCard.cardType)`（`:550-555`），
  那边按 cardType 分档（`CanUseVOs` 一族）。
- **今天的影响 = 0 行为**：我们把 `ShowRadioMessage` 的第四参**只打进日志**（`:330-333`），没有分支 ⇒ 传 0 只是**日志里印错**。
  但**「值」和「分支」两件都没接**，接线时两件要一起补。
- **准确修法**：`if (card != null && card.CardType >= 0) return card.CardType;`，剩下 `-1`（未知）才保留 warning。

### 3·2 `WhenEvent.ActingCardTypeMatches` **漏 `enchantment`**
- 位置：`RuleEngine/Core/WhenEvent.cs:1830-1835` —— `ActingTactic` 掩码只认 `tactic`/`defence`。
- 判据：原版 `FilterMethods__CheckIfMeetsCriteria.c:279-293` 的 `cardType == 0x14(20) ⇒ includeSpells`，
  而**附魔模板真值 `cardType = 20`** ⇒ 按原版它**该**落进 Tactic 那一格。
- **可达性 = 0**：附魔卡（`Pray Enchantment`，inventory `10 NotInInventory`）**从不作为 acting card** 出现；
  但这是**口径差**，将来接「附魔卡自己触发的 `When`」时会现形。
- **准确修法**：把条件加成 `|| string.Equals(t, "enchantment", …)`，或整段改成
  `c.CardType >= 0 ? c.CardType == 20 : (t == "tactic" || t == "defence")`（**未知仍走字符串**，⛔ 别把 `-1` 当 0）。

### 3·3 `AiTargets.CardTypeOf` 的 `else → 20` 会把 **Whispers(40)** 判成 Tactic
- 位置：`RuleEngine/Data/AiTargets.cs:780-781`（`return 20; // tactic / defence`）。
- 今天**不会错**（池内 `whispers` = 0 张；真值 1,351 张也没有 40），且 ai_scoring 真值优先。
- **准确修法**（低优先）：在 `AiScoringData.Get` 返回 null 的分支前面插一条
  `if (card.CardType >= 0) return card.CardType;`，`Type` 映射只作最后兜底。

### 3·4 `EffectResolver.cs:2017` `tacticCaster = !casterCard.IsUnit`
- 注释写「= cardType 20」。今天**等价**（双射），但它表达的是「非单位」而不是「Tactic」——
  将来出现 Whispers(40) 卡就不等价。
- **准确修法**（可选）：`casterCard.CardType >= 0 ? casterCard.CardType == 20 : !casterCard.IsUnit`。

### 3·5 `DeckRuntime` 的卡组计数只有 `unit` / `tactic` 两格
- 位置：`CardPresentation/Deck/DeckRuntime.cs:2457-2458`。`defence`(39) / `enchantment`(1) **落不进任一格**。
- **可达性 = 0**：数据的 `inventoryOptions` 现读 —— `defence` 39 张全是 **5 (`CantAddToDeck`)**、
  `enchantment` 1 张是 **10 (`NotInInventory`)** ⇒ 都进不了 `State.Deck.CardIds`。
- **修法（等真允许 defence 入组时再做）**：`else if (d.Type == "tactic" || d.Type == "defence") stratagems++;`
  （原版 cardType 20 计 stratagems）。

### 3·6 生成器：`cardType == 20` 被当「附魔模板」的**宽松筛**（本身没错，注释会误导）
- `gen_cards_engine.py:4166`（附魔模板入池闸）：`if r.get("cardType") != 20 or "enchant" not in m_Name: continue`
  —— 意图是「**隐藏附魔模板**」，`cardType == 20` 只是**必要不充分**条件，靠 `m_Name` 含 `enchant` 收窄。
- 本批实测**0 漏 / 0 误收**：名字含 `enchant` 的 **120 张**里 `cardType == 20` 的 118 张全入池判据、另 2 张
  （`Wurrboy` / `Sergeant Gadriel`，`cardType 0`）被名字以外的分支归「兄弟卡」；而 `cardType == 20` 的 562 张
  非 enchant 名卡被名字挡掉。
- **建议（不改逻辑）**：把那几处注释／读数文案从「`cardType 20` 模板」改成「**`Tactic(20)` ∧ 名字含 `enchant`**」
  —— 现在这句会让下一个人以为 `20 = 附魔型`（本批我自己就先误读了一次，见报告 §③ 的订正）。
  涉及：`gen_cards_engine.py:4142 / 4145 / 5194-5195`（**仅普查读数**）与 `:3954` 现有注释。

### 3·7 `gen_cards_engine.py:3954` 的注释数字（「全表 680 张都是 20」）
- 该数字经 `ai_scoring.json`（1,351 行真值快照：`20:680 · 0:605 · 10:66`）**复核对得上** ✅。
  但它写的是「**全表**」—— 池内只有 **486** 张 20。建议补一句「池内 486 / 真值全表 680」，免得拿它当池内数。

## 4. 查不到 / 边界（如实）

1. **`ai_scoring.json` 的 `cardType` 来源**：它自己带 1,351 行（= 真值快照，不是池），与本批新列**同源不同载体**；
   本批只核了分布一致（`20:680` 与生成器注释一致），**没**逐卡 join 比 1,351 行 ↔ 池 1,128 行（不属本条）。
2. **反方向（原版用 cardType 而我们用了别的）没普查** —— 本批只查「我们的代码里按 cardType 分支的地方」，
   没有逐条回原版判据复核「每处该不该用 cardType」（那是逐方法的账，`账` 里已有行）。
3. **`defence` 是否真不能进卡组**只核了数据位（`inventoryOptions = 5 CantAddToDeck`），
   没核 `DeckRules.CannotAddToDeck` 的运行时路径（有单独账）。
4. `d:/2` 侧除 `CardTypeOptions.cs` / `EntityScript__get_isTrapCard.c` 外**没再展开**（本批范围是我们自己的仓）。
