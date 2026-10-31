# B109_R19 · `poisoned` 发射点 & `effectText` 链 4 步 —— 判据复核

> 只读代理 `B109_R19` · 2026-10-31 · 第一百零九会话
> 判据源：`decomp_full/*.c`（第一权威）· `out_cpp2il_dev_ilspy` / `out_il2csharp`（交叉）· 我方 `RuleEngine/**` 与 `cards_engine.json`（现读 v45）
> ⛔ 未改任何源码 / 数据 / 账 / 正本；⛔ 未跑 Unity。

## §0 一句话结论

- **`poisoned` 不是「0 发射点」** —— 原版有、我们也**已经建好并接线**。`Unity/资料/完全复刻/待接线与排队项.md:38` 那一行**已过期**（同一文件 `:960` 已经就地订正过）。真缺口只剩一条**结构性**的：我们的发射点写在「`poisonous` 关键词」这一支里，而原版写在**通用 `addEffect`** 上；池内今天两侧都是 0 生产者 ⇒ 零可观察差异。
- **`effectText` 4 步：2 步半已做完**（生成器 ✅ · `CardDef` ✅ · 载体 ✅ · 消费端**只做了右列那一半**）。剩下 4 条可立即派，其中 **1 条是我们自己的真缺陷（字体闸漏了）**。

---

## §1 `poisoned`（trigger 290）

### 1·1 原版：有发射点，**只有一处语义源**

| # | 事实 | 判据 |
|---|---|---|
| ① | 广播发在 `ResolveAddEffect` 里，闸 = `CardEffect.buffType == 200`（200 = `DefinedTrait.poisoned`） | `decomp_full/BattleManager__ResolveAddEffect.c:180` 判 `*(int *)(local_198 + 0x3c) == 200` ⇒ `:268` `BroadcastUnitPoisoned(manager, local_f8 /*被加 buff 的单位*/, *(local_198+0x18) /*enchantingCard*/)` |
| ② | `ResolveAction` 里那一处是**同一段代码的内联副本**，不是第二个语义点（闸同样是 `== 200`，ILSpy 里写作 `while (cardEffect.trait != DefinedTrait.poisoned)`） | `BattleManager__ResolveAction.c:1502` 闸 + `:1590` 调用；ILSpy `BattleManager.cs:26126-26146` |
| ③ | 广播体 = ① 双方**在场**卡逐张 `ReactToUnitPoisoned`（`manager+0x470`，先判 `IsInPlay`）② **当前行动方**手牌 ③ **对手**手牌 | `BattleManagerSupport__BroadcastUnitPoisoned.c`（65 行，逐句） |
| ④ | 接收端 = `RawCardScript.OnTrigger(0x122 = 290, this, actingCard=施予方, target=中毒单位, 0)` | `CardScript__ReactToUnitPoisoned.c:6-9` |
| ⑤ | 原版**唯一的 `poisoned` 生产者** = `poisonous(380)` 攻击：`HasCurrentTrait(poisonous) && 目标是 Minion && 非督军等价 && damageType==Attack` ⇒ 造一条 `{buffType=addTrait, trait=poisoned}` 走 `AddEffect` | `CardScript.cs:14287-14295`（ILSpy）；`DefinedTrait.cs:35 poisonous = 380` |

⇒ **时机** = 「一条 `buffType==200` 的效果落地**之后**」，`ResolveAddEffect` 尾部同步广播（不是回合末、不是伤害瞬间）。

### 1·2 我们：**已经建好并接线**

| 环节 | 落点 | 状态 |
|---|---|---|
| 发射调用 | `RuleCore.cs:16307`（`ResolveDamageDealtHook` 的 `poisonous` 支，`if (rec != null) BroadcastUnitPoisoned(...)`） | ✅（`rec != null` 对应原版 `local_198 != 0` 那道闸） |
| 广播体 | `RuleCore.cs:16970` `BroadcastUnitPoisoned` ①在场两方 ②③手牌 | ✅ |
| 在场筛子 | `RuleCore.cs:16999 HasUnitPoisonedBody`（原版对每张卡都调、没能力的空转；我们筛掉以免多刷表现事件 —— 已写进 doc） | ✅ 等价 |
| 手牌两跳 | `EffectResolver.cs:19154 BroadcastUnitPoisonedHandHops` | ✅ |
| trigger 映射 | `CardDef.cs:1929 {"UnitPoisoned", 290}` | ✅ |

### 1·3 真缺口（一条，结构性）

我们的广播写在 **`poisonous` 关键词**那一支（`RuleCore.cs:16307` 直接调 `UnitState.ApplyTraitEffect`），
而原版的闸挂在**通用 `addEffect`**（`ResolveAddEffect`）上 ⇒ 任何**效果文本**给出的 `give … poisoned`（走 `EffectResolver.ApplyOneGain` 的 `addTrait` 正支）**在原版会广播、在我们这里不会**。

**今天零可观察**（现读三扫）：池内 `poisonous` 关键词卡 **0 张** · `buffType 3 + trait 200` 行 **0 条** · `trigger 290` 卡 **0 张**。
但按铁律 11（有卡就白打）⇒ 要补。

### 1·4 施工单（可粘贴）

落点：`RuleEngine/Core/EffectResolver.cs`，`ApplyOneGain` 的 `addTrait` 支，紧接在
`if (added && val > 0) EmitStatusApplied(ctx, u, p.Keyword, hadKw);`（约 `:12238`）**之后**：

```csharp
// 判据：原版 `BattleManager__ResolveAddEffect.c:180/268` —— 闸在【通用 addEffect】上（buffType==200），
//       不是只在 `poisonous` 关键词那一支。`appliedTrait != null` = 原版 `local_198 != 0`；
//       `rec` 落账之后才广播。⚠️ `RuleCore.ResolveDamageDealtHook` 是直接调 `ApplyTraitEffect`
//       （不走本函数）⇒ 这里补上**不会**与那条双播。
if (appliedTrait != null && CardDef.Normalize(p.Keyword) == KeywordTable.Poisoned)
    RuleCore.BroadcastUnitPoisonedPublic(ctx, u, ctx.ActingUnit);
```

配套两点：
1. `RuleCore.cs:17017` 的 `BroadcastUnitPoisonedForTest` 现在只有**自检门**；产品路径直接调 `…ForTest` 是坏味道 ⇒ 加一个 `public static void BroadcastUnitPoisonedPublic(...)`（同体转调），自检门保留。
2. `CardDef.Normalize`（`CardDef.cs:8696`）在现池 `"poisoned"` 上前缀不撞车（`pindown`/`pack`/`penitence`/`poisoned` 第 2 字符分岔，见 `CardDef.cs:8686` 的现核注释）—— 仍建议先 `CardDef.DefinedTraitNumber(p.Keyword) == 200` 落一次判据，比字符串比更稳。

验收（零行为变化）：`bash d:/4/Unity/工具/ruleprobe.sh play check` 指纹应与改前**逐字节相同**（0 生产者）。

### 1·5 还差什么

- 无判据缺口。**只剩这条施工单**。
- 📌 **顺手订正**：`Unity/资料/完全复刻/待接线与排队项.md:38`（「关键词 290（poisoned）全仓 0 发射点」）与 `:835` 那一行都应标「已过期」—— 同文件 `:960` 早已订正，**同一件事现在有三份说法**。（本次只读，未改。）

---

## §2 `effectText` 链（38 行 / 31 值）

### 2·0 现读数据

`cards_engine.json`（现读 **version 45** / 1,129 张）：`abilityEffectTexts` **38 行 · distinct `src` 31 · 31 张卡**；
行形状 `{ab, lg, at, src}` —— 🔴 **没有 `name`（`effectName`）字段**（见 2·2 第 ④ 条）。

### 2·1 四步逐条

| 步骤 | 应做什么 | 现读做了没有（文件:行） | 缺什么 |
|---|---|---|---|
| **① 生成器** | `gen_cards_engine.py` 新增 `abilityEffectTexts` 列（`buffEffect.effectText` + `handBuff.cardEffect.effectText`，只收非空） | ✅ **已做**：`工具/gen_cards_engine.py:7242 / 7254-7310`（v44，`B99_W3`）；数据已落 v45。**同时收 `effectName`** → ❌ 未做 | `effectName` 那一半（判据 §5·1 说 16 条非空 / 4 个值） |
| **② `CardDef`** | `AbilityEffectText` 类 + `AbilityEffectTexts` + **三态 `…Versioned`** + `EffectTextOf` | ✅ **已做**：`CardDef.cs:9388` `AbilityEffectTextSpec` · `:9402` 字段 · `:9406 HasAbilityEffectTexts`（三态）· `:9420 AbilityEffectTextAt` · `:9434 EffectTextOf` · `:9447 EffectTextTermOf`；装载 `CardDatabase.cs:1749` → `:4849 MapAbilityEffectTexts`（`B100_F9` 已修 `at` 判 `IsHand`） | 无（仅 `Name` 那一格，归 ①） |
| **③ 运行期载体** | 三个载体各带 `EffectText/EffectName` | ✅ **已做（换了形状，更省）**：不加 `EffectText` 字段，改带**归属四元组** `{Owner, Ab, Lg, IsHand}` —— `StatMod` `UnitState.cs:2887` · `TraitEffectRecord` `:1853` · `TempBuff.Origin` `:1644`；投影 `EffectsToShow.cs:640-646`（`ShownEffect.EffectTextCard/Ab/Lg/IsHand`）+ 反解 `:700 AttachEffectTexts` + `:238/:324 OwnerCard`（`B107_W28` 2026-10-31 补的来源卡） | ⚠️ **覆盖读数过期**：`EffectsToShow.cs:688` 那段 doc 写「31 张里 27 张可达」，但它是 `B107_W28`（`OwnerCard` 来自 `EffectOwner`）**之前**写的 ⇒ **别拿 27 当现状**，需重跑 |
| **④ 消费端** | ① `RowsOf` 右列 ② 抽共用件 ③ `ShowCardsWindowOf` 去硬编码 ④ 左列 `effectName` 三档 | ① ✅ `CardDisplayWindow.cs:2229`（`RowsOfEffects` 里第一跳）→ `:2671 TryAddEffectTextRow`；② ❌ `AbilityTerm/AbilityRaw/AbilityMarked` 仍是 `SkillPanel.cs:2216/2223/2241` 私有静态；③ ❌ `EffectResolver.cs:7935` 仍 `case "GSC55"`；④ ❌ `TempBuff.EffectName`（`UnitStateEffectDisplay.cs:77`）**0 个写点**（`grep "\.EffectName *="` 在 `RuleEngine/` = 0 命中），`DisplaySourceName`（`:114`）恒退 `SourceCard` | 4 项 |

### 2·2 可立即派的 4 条

**A（真缺陷，优先）字体闸漏了。**
`CardDisplayWindow.TryAddEffectTextRow`（`:2671-2688`）自己写了一遍 `Loc.T(key)` + `CardIcons.RewriteMarks(what, Loc.Current == Chinese)`，
**没有** `SkillPanel.AbilityRaw/AbilityMarked` 里那道 `CardText.Zh` 字体闸（中文档但中文字体资产缺失 ⇒ 应改取 `Loc.OfficialEnglish`）。
⇒ 后果：`Card_Abilities/*` 那 31 个值在「中文 + 无中文字体」这一档会印**方块**（`SkillPanel` 那条路不会）。
改法：把 `TryAddEffectTextRow` 里的取词/渲染两跳改成转调共用件（与下面 B 合并做）。

**B 抽共用件（判据 §5·4-2）。**
把 `AbilityRaw` / `AbilityMarked` / `AbilityTerm` 从 `SkillPanel` 提成 `Core/Loc.cs` 的 `Loc.AbilityText/AbilityRaw`（或等价静态类），
`SkillPanel.cs:2216-2245` 改转调；`CardDisplayWindow.TryAddEffectTextRow` 与 `EffectResolver.ShowCardsWindowOf` 侧一并转调。
⚠️ 搬的时候**连字体闸一起搬**（`CardText.Zh == false` ⇒ 取英文列）—— 这正是 A 那一格。
⚠️ 这几个方法在 `SkillPanel` 被账按行号引用（`SkillPanel.cs:2145/2191/2216` 等），**别新增行**（铁律 18·b）⇒ 原地改写成转调。

**C `ShowCardsWindowOf` 去硬编码（判据 §5·4-3）。**
`EffectResolver.cs:7935` 现读 `case "GSC55": return "EffectDisplayCardsAddToDeck";`。
数据列已上线 ⇒ 改成「扫本卡 `AbilityEffectTexts` 里 `ab/lg` 对上 `chosenAbility == 470` 那条的 `Src`」，**保留硬编码作回退**（`TestW62EntryPump`/`TestW64ShowCardsWindow` ①组还钉着它）。
注意：`cards_engine.json` **没有 `chosenAbility` 列**（`ShowCardsWindowOf` 的 doc 已写明）⇒ 这一条要**先确认**能不能只靠 `abilityEffectTexts` + `abilityTargets` 认出 `GSC55` 那条（今天全池恰好 1 张 ⇒ 否则这条只能「数据列 + 硬编码二选一」）。**这是本项唯一的不确定点。**

**D 左列 `effectName` 三档（判据 §5·4-4）。**
原版 `CardEffectItem.LoadEffect` 三档：`effectName` 非空 ⇒ `Card_Name/<effectName>`；否则 `enchantingCard` 的卡名；都空 ⇒ 空串（`CardEffectItem__LoadEffect.c:116 / :135 / :126-131`）。
我们已有骨架（`UnitStateEffectDisplay.cs:77` + `:114`），但 **`effectName` 从来没被采集**：
① `gen_cards_engine.py` 加同列字段（判据说 16 条非空 / 4 个值，**本次未复核**，需用 `cards_raw.json` 的 `full_raw` 现数，⛔ 注意每卡两份技能树会**翻倍**）；
② `CardDef.AbilityEffectTextSpec` 加 `Name`；
③ `DeriveEffectOrigin` / `AttachEffectTexts` 顺手带过来写进 `TempBuff.EffectName` / `ShownEffect`。
**池内影响小但为真**：今天所有带 `effectText` 的行左列印的都是施加卡名，原版有一部分会印 `effectName`。

---

## §3 汇总

| 类别 | 条目 |
|---|---|
| **可立即派（判据已齐）** | `poisoned` 通用 `addTrait` 钩子（§1·4）· A 字体闸（§2·2 A）· B 抽共用件（§2·2 B）· D `effectName` 三档（§2·2 D，数据侧） |
| **可派但要先定一件事** | C `ShowCardsWindowOf` 去硬编码 —— 需先确认「没有 `chosenAbility` 列时怎么唯一认出那条」（§2·2 C） |
| **需再查（读数，不是判据）** | `AttachEffectTexts` 的真实覆盖（「27/31」是 `B107_W28` 之前的数字，重跑一次再报） |
| **判 D** | 0 条 |

**回执**：`poisoned` 原版**有**发射点（`ResolveAddEffect.c:180/268`，闸 `buffType==200`），我们也**已建好并接线** ⇒ `待接线与排队项.md:38` 那条「0 发射点」**已过期**；真缺口只有「发射点位置不同（关键词支 vs 通用 addEffect）」1 条。`effectText` 4 步 = 生成器 ✅ / `CardDef` ✅ / 载体 ✅（形状换成归属四元组）/ 消费端**只做了右列**（4 项里完成 1 项）。**可立即派 4 条**（其中 1 条是我们自己的字体闸缺陷），另 1 条要先定口径。
