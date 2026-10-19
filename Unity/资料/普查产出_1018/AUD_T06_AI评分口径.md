# AUD_T06_AI评分口径（引擎线 T06 / A1797）

日期 2026-10-19 · **只读查证**（未改 `.cs` / 数据 / 正本 · 未跑 Unity / git 写 / typecheck）

**判据源**

- 当前 build SO：`d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张）
- 旧快照：`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（1,195 条）
- 反编译：`d:/2/tools/decomp_full/`（Ghidra）＋ `d:/2/tools/il2cpp_out/dump.cs`（字段/偏移/RVA）＋ `d:/2/unity_run_ref/GameAssembly.dll`（对 Ghidra 截断处直接反汇编，见 §六）
- 我们的实现：`d:/4/Unity/MyGame/Assets/RuleEngine/Data/SimpleAI.cs`

---

## §一 结论（口径建议，先看这 10 行）

1. **`attackScoringCriteria` 不要吃**。1,351 张是**同一份序列化默认模板**（不是严格全 0：`multiplyByFriend=1.0 / multiplyByEnemy=-1.0 / damage min=max=1 / heal min=max=1`），且 `specialAttackScoringCriteria` **1,351/1,351 = 0**（2022 旧快照 1,192/1,192 也全是 0）⇒ 原版 `AI.ScoreFromAttack` 里那两处读取**永远进不去**。即使把 flag 打开，模板是 `scoringOption=0 (FixedValue)` + `baseValue=0` ⇒ 加的分仍是 **0**（双重失效）。
2. **T06 的真正数据 = 两列**：`nonTargetScoringCriteria`（**所有卡都跑**，387 张 / 390 条）＋ `cardTargetScoringCriteria`（**只有 `targeted=1` 且动作带目标时**才跑，367 张 / 373 条；逐条过 `FilterMethods.CheckIfMeetsCriteria`）。
3. **没有这两列的卡，原版没有「兜底评分」**：单位/督军靠 `CardScript.GetValueInPlay()`（`2×费+1 + 附魔价值`，伤势 `×(0.5×血/上限+0.5)`，眩晕/压制 `×0.8`）；战术/Whisper（`cardType=20/40`）**就是 0** ⇒ 0 分出牌动作不会替换 `endTurn` 基准（§三·一）⇒ 原版 AI **不会打**这些卡。⛔ 不要拿我们推导的 `ScoreOps` 冒充「原版的兜底」——原版没有这一层。
4. 池内（1,127）**620 张没有任何评分列表**：其中 **547 张是单位**（靠 `GetValueInPlay`，不是「没分」）、**56 张督军**（不从手牌打出）、**17 张战术**（真正会落到 0 的那批，名单见 §六 附）。全量 1,351 口径的 724 张比池内多 **104 张**，全部在池外（重复/异形态/教程副本）。
5. `extraScoreFrom*` 七列、`overrideDefaultValueInPlay`、`manualValueInPlay` **全卡都是 0/默认** ⇒ 读取代码存在（§三表），但对当前数据**零影响**。T06 只需保留字段，不必为它们做逐卡表。
6. **主动技：`hasActiveAbility=0` 的 45 张（池内；全量 55 = 50 督军 + 5 单位）⛔ 不应被 AI 当成有主动技**。动作生成门是**运行时 `CardScript.hasActiveAbility`**，它初始值直接抄 SO 的 `hasActiveAbility`（`SetBaseCardTraits:37`）；`activeAbility` 逻辑非空**不参与**这个门。唯一例外：`BuffType.replaceAbility(40)` 的附魔在运行时把它置 1（§四）。
7. **切换策略**：`cards_truth.json` 已经把 `targeted / hasActiveAbility / 两列评分 / attackScoringCriteria` 落好（§五·一）⇒ 先把三列接进 `CardDef`，再实现 `GetValueInPlay` + `ScoreFromCriteria`，最后用 SO 替换 `SimpleAI.ScoreOps`。`ScoreOps` 只留作 debug 对拍，不留在产品路径。
8. **原版直证**：`GetValueInPlay` 公式、两列评分的分支/公式、`attackScoringCriteria` 的门、`hasActiveAbility` 的门、伤害/治疗子打分、`extraScore*` 读取点 —— 全部来自反编译/反汇编。**推断**：`ValueOfEnchantments` 每个 `CardEffect` 字段的语义、`ScoreFromCriteria` 少数 option 的精确系数、50 张督军 `activeAbility` 数据到底是不是 Talent（§六）。
9. **顺手查出的现存偏差**（与 T06 同一条打分链，建议一起修）：`ScoreDamaging` 对 `ambush` 返回 0，原版是 **10.0**；对 `longrange` 返回 0，原版是 **3.0**；`ScoreHealing` 用「督军 1.5×」，原版是「当前血 >10 时 ×1，≤10 时 ×2」（§三·四）。
10. `SimpleAI.ValueInPlay` 里的 `KeywordValue` **不是原版的 `ValueOfEnchantments`**（原版那层数的是卡上的 `activeEffects/CardEffect`，不是关键词表）⇒ T06 要把它标成「已知近似」，别写成「原版同款」。

---

## §二 `attackScoringCriteria` 全 0 的含义

### 2.1 数据事实（现核）

| 项 | 读数 | 出处 |
|---|---|---|
| `attackScoringCriteria` 签名种数 | **1 / 1351**（`json.dumps(sort_keys=True)` 全部相等） | `cards_raw.json` 全表 |
| 旧 2022 快照 | **1 / 1192 非空**（另 3 条 null），且与当前模板**完全相同** | `cards_1195_2022时代.json` |
| 模板里的非零值 | `multiplyByFriend=1.0` · `multiplyByEnemy=-1.0` · `damageCriteria.min/max=1` · `healCriteria.min/max=1` · `buffEffect.triggerEnchantAnim=1` | 同上 |
| 真正决定读不读的 flag | `specialAttackScoringCriteria` = **0 / 1351**（旧快照 0/1192） | 同上 |

> ⚠️ **就地更正（铁律 5）**：任务书/任务总表里写的「**全 0 模板**」不准确 —— 它不是全 0，而是 `ScoringCriteria` 的**序列化默认实例**（构造默认 `multiplyByFriend=1.0 / multiplyByEnemy=-1.0`，嵌套 `DamageCriteria/HealCriteria` 默认 1）。
> 结论不变：**它不是逐卡数据**，两代 build 都没人给它填过东西。

### 2.2 原版读不读？读，但被 flag 挡住

`RawCardScript` 字段（`dump.cs:23349-23360`）：

```
extraScoreFromDamaging          // 0x2C8
extraScoreFromKilling           // 0x2CC
extraScoreFromHealing           // 0x2D0
extraScoreFromAttacking         // 0x2D4
extraScoreFromTargetingOnAttack // 0x2D8
extraScoreFromJamming          // 0x2DC
extraScoreFromKeepingCard       // 0x2E0
cardTargetScoringCriteria       // 0x2E8
nonTargetScoringCriteria        // 0x2F0
specialAttackScoringCriteria    // 0x2F8  (bool)
attackScoringCriteria           // 0x300  (ScoringCriteria)
```

读取点 = **`AI.ScoreFromAttack`**（`decomp_full/AI__ScoreFromAttack.c`）：

- `:31-37`：`if (CardScript.HasSpecialAttackScoring(card) && !HasCurrentTrait(card, 0x1d6 /*sacrifice*/))`
  → `AI.ScoreFromCriteria(..., rawCard.attackScoringCriteria, 0)`
- `:39-69`：`if (HasCurrentTrait(card, 0x1d6))` 分支里再判一次攻击类型/攻击力，并且 `!HasCurrentTrait(0xf0 /*dodge*/) && !HasCurrentTrait(0x50 /*shield*/) && HasSpecialAttackScoring(card)`
  → 再读一次 `attackScoringCriteria`

`HasSpecialAttackScoring` 的完整判据（`CardScript__HasSpecialAttackScoring.c:11-15`）：

```c
if (rawCard.specialAttackScoringCriteria == false) return false;
return !HasCurrentTrait(card, 0x82 /*jam, DefinedTrait.jam=130*/);
```

⇒ **条件 = `specialAttackScoringCriteria` 为真 且 卡当前没有 `jam`**。当前数据第一项就全 0 ⇒ **运行时一次都不会读**。

另有第二类使用点（也不是打分，是平手裁决）：`AI.IsBetterAction` / `AI.SelectBestAction` 在「当前最佳是 endTurn、候选是攻击」时看 `HasSpecialAttackScoring`（`AI__SelectBestAction.c:58-72`、`AI__IsBetterAction.c:35-43`）⇒ 同样全 false，永远走另一支。

### 2.3 就算 flag 被打开，也仍是 0

模板 `scoringOption = 0`（`ScoringOption.FixedValue`），`baseValueFriend = baseValueEnemy = 0`。
`ScoreFromCriteria` 的 option 0 直接返回选中的 base 值（反汇编 `0x18093ABCF: movaps xmm0,xmm7`）⇒ **加 0**。
所以这一列是**双重失效**：没数据 + 即使有数据也是空模板。

---

## §三 评分来源全表

### 3.1 出牌主链（逐句，直证）

`AI.ScoreFromPlayingCard`（`AI__ScoreFromPlayingCard.c:21-30`）：

```
score = IsSpellType(cardType) ? 0 : CardScript.GetValueInPlay()
      + AI.ScoreFromPlayingNow()
```

- `IsSpellType` = `cardType == 20 (Tactic) || cardType == 40 (Whispers)`（`SupportMethods__IsSpellType.c`）
- `ScoreFromPlayingNow`（`AI__ScoreFromPlayingNow.c:22-63`，**以反汇编 `RVA 0x93C300` 为准**；Ghidra 在 `:49` 把「target 列表跑完后跳去 nonTarget」误画成了 `goto LAB_18093c4d4`，那是错误出口）：

```
if (card.targeted && targetCard != null)
    for each c in rawCard.cardTargetScoringCriteria:
        if FilterMethods.CheckIfMeetsCriteria(card, targetCard, targetCard, c.targetCriteria)
            score += ScoreFromCriteria(card, targetCard, c)

for each c in rawCard.nonTargetScoringCriteria:      // ← 所有卡都跑，targeted 卡也跑
    score += ScoreFromCriteria(card, targetCard, c)
```

**关键口径**（容易写错的三点）：

1. `nonTargetScoringCriteria` **不是**「非指向卡的替代列表」——它是**通用列表**，指向卡也会跑；指向卡只是在它之上**再加**匹配目标的 `cardTargetScoringCriteria`。
2. `cardTargetScoringCriteria` 只在 `rawCard.targeted == 1` **且** 这条动作带目标时跑；`targeted` 读的是 `RawCardScript.targeted`（`EntityScript__get_targeted.c:5-7` → `rawCard + 0x268`）。
3. 两列都为空时**没有 fallback**：`ScoreFromPlayingNow` 返回 0；单位/督军仍有 `GetValueInPlay`，战术卡就是 0。

### 3.2 评分来源总表

| # | 来源 | 覆盖（全 1,351 / 池 1,127） | 原版出处（文件:行 / RVA） | 缺列时兜底 |
|---|---|---|---|---|
| 1 | **`GetValueInPlay`**（单位/督军基础价值） | 所有 `cardType 0/10` 的**出牌基础分**；同时是**伤害/治疗/弹回**子打分的目标价值尺（`ScoreFromDamagingBuffedUnit` / `ScoreFromHealingUnit` / `ScoreFromReturnToHand`） | `AI__GetCardValueInPlay.c` 全文 + **RVA 0x939030 反汇编**（公式见 §三·三） | **它就是兜底**（原版没有第二层） |
| 2 | **`nonTargetScoringCriteria`** | 387 张 / 390 条（池 307 / 310） | `AI__ScoreFromPlayingNow.c:52-63`；反汇编 RVA 0x93C300 | 空 → 0 |
| 3 | **`cardTargetScoringCriteria`** | 367 张 / 373 条（池 261 / 267） | `AI__ScoreFromPlayingNow.c:31-48`；反汇编 RVA 0x93C300 | `targeted=0` 或空 → 不跑（不回落第 2 列，第 2 列本来就会跑） |
| 4 | `attackScoringCriteria` | 1,351 / 1,351（同一模板） | `AI__ScoreFromAttack.c:31-37,64-68` | **不读**（`specialAttackScoringCriteria=0` 全卡） |
| 5 | **`ActiveAbility.cardTargetScoringCriteria` / `nonTargetScoringCriteria`**（主动技自己的两列） | 主动技动作（池内 flag=1 的 87 张中 28/36 张有列） | `AI__GetActionScore.c:68-108`；`ActiveAbility` 字段 `dump.cs:23766-23767` @0x50/@0x58 | 空 → 0（技能动作不替换 endTurn） |
| 6 | `extraScoreFromDamaging` / `extraScoreFromKilling` | 0 张非零 | 反汇编 **RVA 0x93B1E0**：`0x18093B4B2 addss [raw+0x2C8]`、`0x18093B4BE addss [raw+0x2CC]`（Ghidra 的 `.c` 在这两行前被截断） | 加 0 |
| 7 | `extraScoreFromHealing` | 0 张非零 | 反汇编 **RVA 0x93BC30**：`0x18093BCF8 addss [raw+0x2D0]`（只走非英雄分支） | 加 0 |
| 8 | `extraScoreFromAttacking` / `extraScoreFromTargetingOnAttack` | 0 张非零 | `AI__ScoreFromBuffedAttack.c:94-95` | 加 0 |
| 9 | `extraScoreFromJamming` | 0 张非零 | `AI__ScoreFromJam.c:16` | 加 0 |
| 10 | `extraScoreFromKeepingCard` | 0 张非零 | `AI__ScoreFromKeepingTheCard.c:61`（**该方法在本 build 无调用点**） | 加 0 |
| 11 | `overrideDefaultValueInPlay` / `manualValueInPlay` | 0 张非零 | `AI__GetCardReferenceValueInPlay.c:8-11`；`GetValueInPlay` 反汇编 `0x180939072` | 用 `manaCost` |
| 12 | **`ValueOfEnchantments`** | 所有单位/督军（并入 `GetValueInPlay`）；也进 `ScoreFromJam` | `AI__ValueOfEnchantments.c` + RVA 0x93D3B0（§三·三） | 0（没有附魔） |
| 13 | `ValueOfTraits` = `Σ CardTrait._aIScore` | **只被 `ScoreFromJam` 用**（不是通用卡价值！） | `AI__ValueOfTraits.c:31-49`；`AI__ScoreFromJam.c:9-11` | 0 |
| 14 | `GetCardReferenceValueInPlay`（`2×费+1`，或 `2×manual+1`） | **本 build 无直接调用点**；公式**内联**在 `ScoreFromReturnToHand` | `AI__GetCardReferenceValueInPlay.c:8-11`；`AI__ScoreFromReturnToHand.c:11-19` | — |
| 15 | **攻击/治疗/晕/毒/回手/加费/抽牌等硬编码子打分** | 攻击、技能等动作 | 各 `AI__ScoreFrom*.c`（逐条见 §三·四） | 原版自带常数 |
| 16 | `clickWaystone` 固定分 | 灵族残骸动作 | `AI__GetActionScore.c` case 6：`DAT_1834b2ecc = 50.0` | 常数 |
| 17 | 平手裁决偏好（不是分数） | 攻击 vs endTurn | `AI__IsBetterAction.c:9-47` + `AI__SelectBestAction.c:25-37` | — |

### 3.3 `GetValueInPlay` / `ValueOfEnchantments` / `ScoringCriteria` 精确式

**`AI.GetCardValueInPlay`**（`RVA 0x939030`，常量从 `GameAssembly.dll` 读出）：

```c
base = rawCard.overrideDefaultValueInPlay ? rawCard.manualValueInPlay : (float)rawCard.manaCost;
v = AI.ValueOfEnchantments(card) + (2f * base + 1.0f);          // 0x1834B2BB8 = 1.0
if (currentHealth < currentMaxHealthProcessed)
    v *= 0.5f * currentHealth / currentMaxHealth + 0.5f;        // 0x1834B2BB4 = 0.5
if (HasCurrentTrait(card, stun /*100*/) || HasCurrentTrait(card, pindown /*970*/))
    v *= 0.8f;                                                   // 0x1834B2FA0 = 0.8
return v;
```

**`AI.ValueOfEnchantments`**（`RVA 0x93D3B0`，Ghidra 截断，反汇编恢复的骨架）：

- 遍历 `EntityScript.activeEffects`（`+0x108`），跳过 `effect+0x30 != 0`（`extrinsic`）。
- `effect+0x38`（`CardEffect` 类型）：
  - `== 1`（属性 buff）：加 `(effect+0x48 + effect+0x60 + effect+0x50) * 0.5`
    （常量 `0x1834B2BB4=0.5`）
  - `12 / 15 / 16`（enchantment / singleUse / choice）：加 **+1.0**；若目标附魔卡的 `spellType(+0x90) == 0x3c(60 = SpellType.SecretOrder)` 则加 **−2.0**
    （常量 `0x1834B2BB8=1.0`、`0x1834B2F78=-2.0`；`SpellType` 见 `dump.cs:45948-45980`）
  - 其它 `>3` 的类型：加 **+1.0**
- ⇒ 它数的是「卡上挂了几层效果/附魔」，**不是关键词表**。这一层的确切字段语义（0x48/0x50/0x60 各是什么）还没逐字段核（§六）。

**`ScoringCriteria` 字段**（`dump.cs:23723-23740`）：

| 偏移 | 字段 | 用途 |
|---|---|---|
| 0x10 | `baseValueFriend` | 目标/施法者**同侧**（或没有目标）时的基值 |
| 0x14 | `baseValueEnemy` | 目标在**对侧**时的基值；option 9/10 里当**阈值**用 |
| 0x18 | `multiplyByFriend` | 同侧目标的系数 |
| 0x1C | `multiplyByEnemy` | 对侧目标的系数 |
| 0x20 | `scoringOption` | 20 个枚举（见下表） |
| 0x28 | `targetCriteria` | 这条判据筛哪些目标 |
| 0x30 | `multiplierCriteria` | 乘数目标集（`targetsAffected==0` ⇒ 乘数=1） |
| 0x38 | `damageCriteria` | option 3 的伤害 |
| 0x40 | `healCriteria` | option 4 的治疗 |
| 0x48 | `buffEffect` | option 5/11 的 buff |
| 0x50 | `handEffect` | option 12 的手牌效果 |
| 0x58 | `codexEffect` | 为真时要求 `CanPlayWithCodexNow` |
| 0x60 / 0x68 | `comboCard` / `comboCardFirst` | 连击卡条件 |

**通用算式**（`AI.ScoreFromCriteria` RVA 0x93A290 反汇编；option 1/2/9/10 全部逐指令核过）：

```
base  = (targetCard != null && targetCard.side != thisCard.side) ? baseValueEnemy : baseValueFriend
score = base
if (targeted branch)
  foreach t in AbilityLogic.GetTargets(manager, criteria.targetCriteria, ...):
      sideMul = (t.side == thisCard.side) ? multiplyByFriend : multiplyByEnemy
      score += sideMul * SubScore(t)        // option 2/3/4/… 各自
      // option 1 (Count) / option 2 (SumValueInPlay) 还要再乘 multiplierCount
```

**`ScoringOption` 全表**（`dump.cs:23695-23717`）与池内使用量：

| 值 | 名字 | 池内条数 | 语义 / 消费点 |
|---|---|---|---|
| 0 | `FixedValue` | 59 | 只返回 base（含 option 15-19/21-24/26-34 都落这一支） |
| 1 | `Count` | 153 | `base + (友方数×multiplyByFriend + 敌方数×multiplyByEnemy) × multiplierCount`（反汇编 `0x93A58D-0x93A651`） |
| 2 | `SumValueInPlay` | 158 | 每个目标 `GetValueInPlay(t) × sideMul × multiplierCount` 累加（`0x93A656-0x93A6E8`） |
| 3 | `SumValueOfDamage` | 107 | `DamageCriteria.GetAverageDamage` → `ScoreFromDamagingUnit`，逐目标 × sideMul（`0x93A6ED`） |
| 4 | `SumValueOfHealing` | 12 | `HealCriteria.GetAverageValue` → `ScoreFromHealingUnit`（`0x93A81D`） |
| 5 | `ValueOfBuffingAttack` | 8 | `ScoreFromBestAttack`（`0x93A8F2`） |
| 6 | `CurrentAttack` | 1 | 当前近战攻击值（`0x93AA73`） |
| 7 | `SumValueOfJam` | 0 | `ScoreFromJam`（`0x93AB10`） |
| 8 | `RandomValue` | 0 | `RandomInt(0,100)`（`0x93ABA7`） |
| 9 | `FixedValueIfCountGreaterThanBaseEnemy` | 0 | `count > baseValueEnemy ? baseValueFriend : 0`（`0x93ABD7`） |
| 10 | `FixedValueIfCountLowerThanBaseEnemy` | 0 | `baseValueEnemy > count ? baseValueFriend : 0`（`0x93AC09`） |
| 11 | `ValueOfBuffing` | 1（仅主动技） | `ScoreFromBuff`（`0x93AC34`） |
| 12 | `ValueOfBuffingCost` | 2 | `ScoreFromHandBuff`（`0x93ACC7`） |
| 13 | `ValueOfBuffingMana` | 17 | `ScoreFromManaBuff`（`0x93AD01`） |
| 14 | `SumValueOfStun` | 3 | `ScoreFromStun`（`0x93AD1A`） |
| 20 | `SumValueOfPoison` | 0 | `ScoreFromPoisoning`（`0x93ADAC`） |
| 25 | `SumValueOfReturnToHand` | 6 | `ScoreFromReturnToHand`（`0x93AE47`） |
| 30 | `SumValueOfGivingCharge` | 0 | `0x93AED9` |
| 35 | `ValueOfDrawingCard` | 51 | `ScoreFromDrawingCard`（`0x93AF8F`） |

> 池内两列合计 **577 条判据**（`cardTarget` 267 + `nonTarget` 310）；`cardTarget` 里 **option 2 占 150/267**、`nonTarget` 里 **option 1 占 152/310** —— T06 第一版先把 **0/1/2/3/35** 做对，就覆盖 **528/577（91.5%）**。

### 3.4 攻击/治疗子打分（本轮反汇编补出的「直证」，顺带发现 `SimpleAI` 三处偏差）

**`AI.ScoreFromDamagingBuffedUnit`**（`RVA 0x93B1E0`；Ghidra `.c` 在 118 行处被截断，以下按反汇编）：

| 条件 | 返回值 | 池内证据 |
|---|---|---|
| `HasCurrentTrait(0x4b0 = ambush/1200)` | **10.0** | 常量 `0x1834B2DA8=10.0` |
| `HasCurrentTrait(0x136 = invulnerable/310)` | **−10.0** | `0x1834B302C=-10.0` |
| `HasCurrentTrait(0xf0=dodge/240)` 或 `0x50=shield/80` | `CurrentMeleeAttack / 3 + 1` | `0x1834B2E8C=3.0`、`0x1834B2BB8=1.0` |
| `HasCurrentTrait(0x488 = longrange/1160)` | **3.0** | `0x1834B2E8C=3.0` |
| `HasCurrentTrait(0xe6=dropPod/230)` 且 `!ignoreDroppod` | **0.5** | `0x1834B2BB4=0.5` |
| 小兵、`damage < health` | `GetValueInPlay(t) × 0.6 × damage / maxHealth` | `0.6` @ `0x1834B3180` |
| 小兵、致死 | `GetValueInPlay(t) × (survivor ? 0.8 : 1.0)` | `0.8` @ `0x1834B2FA0` |
| 督军、`damage < health` | `damage × (剩余血 > 15 ? 1.2 : 1.8)` | `1.2` @ `0x1834B2F58`、`1.8` @ `0x1834B33F4` |
| 督军、致死 | **1,000,000.0** | `0x1834B3354` |
| 最后 | `+= extraScoreFromDamaging`；致死再 `+= extraScoreFromKilling` | `0x18093B4B2/B4BE` |

> ⚠️ **与 `SimpleAI.cs:586-589` 不一致**：我们对 `ambush`/`longrange` 返回 **0**，原版是 **10.0 / 3.0**。另 `invulnerable` 原版给 **−10.0**（我们没这一支）。这三条是**直证**，建议随 T06 一起修。

**`AI.ScoreFromHealingUnit`**（`RVA 0x93BC30`）：

- `healed = min(healValue, 当前已掉的血)`；`healed <= 0` → 0。
- 非英雄：`ValueInPlay(t) × healed / maxHealth + extraScoreFromHealing`。
- 英雄：`当前血 > 10 ? healed : 2 × healed`（`0x93BD33-0x93BD45`）。
- ⇒ 我们 `SimpleAI.ScoreHealing` 的「督军恒 1.5×」是**推断值**，原版是 **1× / 2× 两档**（阈值 10）。

**`AI.ScoreFromDrawingCard`**（`RVA 0x93B610`，池内 option 35 有 51 条，必须核）：

```c
score += BattleManager.MaxCardsInHand() - hand.Count - 1;
if (deck.Count == 0) score = -2 - fatigue;         // 覆盖，不是加（🔴 2026-10-19 订正：原写 hand.Count）
return score;
```

对应反汇编：`0x93B703 call MaxCardsInHand` → `0x93B708 not ebx(+~) / 0x93B70A add eax,ebx` = `max - hand - 1`；
`0x93B71B mov eax,-2 / sub eax,ebp` —— `ebp` 是 `0x18093B6DB` 的 `op_Implicit(fatigue)` 结果
（`ObscuredInt` 在 `BattleManager +0x378/+0x38C` = `playerFatigue` / `enemyFatigue`，`dump.cs:30862-30864`）。
> ⚠️ **与 `SimpleAI.ScoreDraw` 不一致**：我们对空牌库返回 `-2 - RuleCore.HandMax`（**牌库空、手牌不一定满**）。
>
> 🔴 **2026-10-19 就地更正（铁律 5 · E6 现核 RVA 0x93B71B / 0x18093B08B）**：本行原来写「原版是 `-2 - hand.Count`」——
> **不成立**。`mov eax,-2; sub eax,ebp` 里的 `ebp` 是
> `ObscuredInt.op_Implicit(BattleManager.playerFatigue / enemyFatigue)` 的结果
> （`0x18093B6DB` / `0x18093B03B` 两处），**不是手牌数** ⇒ 原版 = **`−2 − 疲劳`**。
> 错因：把 `ebp` 当成 `GetCardsInHandRef(...).Count` 的寄存器。已按疲劳实现。

---

## §四 55 张「有主动技逻辑但 flag=0」的处置

### 4.1 数据

| 口径 | 总数 | `cardType=10` 督军 | `cardType=0` 单位 |
|---|---|---|---|
| 全 1,351 | **55** | **50** | **5**（SOR70 / AM4 / SOR23 / SOR71 / SOR72） |
| 池 1,127 | **45** | **40** | **5** |

判据 = `activeAbility.activeAbilityLogic` 非空且 `hasActiveAbility == 0`。

### 4.2 原版 AI 侧的正确判据：**不当作有主动技**

证据链（全部直证）：

1. **动作生成门**：`BattleManager.GetAvailableActions`（`BattleManager__GetAvailableActions.c:155`）：
   ```c
   if (*(char *)(card + 0x220) != '\0') {          // CardScript.hasActiveAbility
       if (BattleManager.CanUseActiveAbility(...)) { ... AvailableAction(actionType=5) ... }
   }
   ```
   `0x220` = `CardScript.hasActiveAbility`（`dump.cs:35443-35444` 的 backing field）。
2. **初始值来源**：`CardScript.SetBaseCardTraits`（`CardScript__SetBaseCardTraits.c:37`）：
   ```c
   *(byte *)(card + 0x220) = *(byte *)(rawCard + 0x2A9);   // RawCardScript.hasActiveAbility
   ```
   每个实例创建/重置都会走（`CardScript__CardSetup.c`、`CardScript__ResetToValuesInHand.c` 调它）。
3. **`GetActiveAbility()` 本身不看这个 flag**（`RawCardScript__GetActiveAbility.c:10-17` 只看 `hasAlternativeAbility 0x2AA`）—— 但它在 AI 里只在 `GetActionScore` **case 5** 被调用；**case 5 的动作根本不会生成** ⇒ AI 永远看不到这些卡。
4. **运行时例外（必须保留）**：
   - `CardEffect` 的 `BuffType.replaceAbility(40 /*0x28*/)`：`CardScript__AddEffect.c:242-251` 把 `card.hasActiveAbility(0x220)=1`、`activeAbilityRef(0x118)=effect.enchantment`；`CheckReplacedAbility.c:23-35` 同款。
   - `RemoveEffect.c:271` 在效果移除时把 flag **恢复成 rawCard 的 0x2A9**。
   - `ReactToUnitJammed.c:153` 置 0；`HideWarlordActiveAbilityForTutorial.c:5` 置 0。
   ⇒ 正确口径 = **`CardScript.hasActiveAbility` 这个运行时值**（= SO flag + 运行时附魔），不是 `activeAbility != null`。

### 4.3 这 50 张督军的 `activeAbility` 数据是什么？（观察 + 未证）

- 50 张里 **48 张同时有 `cardAbilities.trigger == 46 (Talent)`**；`activeAbility.manaCost` 44 张 = 2、6 张 = 0。
- 卡面文本（我们池内）**没有任何一张写 `Ability:`**（45 张逐张核过 `desc`，命中 0）——它们写的是 `Talent:` / `Pray:` / `Agenda:` / `Duty:`。
- 最合理的解释：这段 `activeAbility` 是 **Talent 卡/天赋施放逻辑**（或旧版遗留），不是「英雄可点的主动技」。**但这只是推断，未从代码确证**（§六）。
- **判据不依赖它**：动作生成只看 flag，所以结论不变。

### 4.4 我们引擎侧

- 现状：`CardDef.HasAbility => Ability != null`（`CardDef.cs:2948`），而 `Ability` 只从卡面 `Ability:` 文本解析（`CardDef.cs:442`）。
- 45 张池内 flag=0 卡**都没有 `Ability:` 文本** ⇒ 目前**行为恰好与原版一致**（不会产出技能动作）。
- 但 T06/T04 接线时**必须显式加 `HasActiveAbility` 闸**，⛔ 不要因为 `cards_truth.json` 里有 `activeAbility` 实体就把它接成 `CardDef.Ability` —— 那会把 45 张卡变成原版没有的技能。

---

## §五 与现有 `SimpleAI` 的切换策略

### 5.1 数据已经就绪

`Unity/MyGame/Assets/RuleEngine/Resources/cards_truth.json`（T12 产物，1,351 张）**已经包含**：

- `targeted`（0/1）
- `hasActiveAbility`（0/1）
- `attackScoringCriteria`（模板）
- `cardTargetScoringCriteria` / `nonTargetScoringCriteria`（**默认值省略**：缺键 = 0/空；`multiplyBy*` 缺键 = 0.0）

⚠️ `specialAttackScoringCriteria` / `extraScoreFrom*` / `overrideDefaultValueInPlay` / `manualValueInPlay` **没有落进 truth**（生成器按默认值省略）。当前数据它们全是 0 ⇒ 不影响；若 T06 要消费 `specialAttackScoringCriteria`，**先把它补进 truth 生成器**（否则「缺键 = false」也对，但无法区分「没落」与「真 false」）。

### 5.2 切换顺序（建议）

| 阶段 | 做什么 | 判据 |
|---|---|---|
| **P1 接线** | `CardDef` 加 `Targeted / HasActiveAbility / NonTargetScoring / CardTargetScoring`（或旁挂 `cards_truth` 查询表）；`targeted` 已有 v9 死列可直接接 | `CardDatabase.cs:189` 的 `targeted` 已是 SO 真值（池内 0 处不一致，本轮现核）；`EntityScript__get_targeted.c:5-7` 直接读 `rawCard+0x268` ⇒ **`CardDatabase.cs:188` 那句「两者是否等价未核」可以核销** |
| **P2 基础值** | 实现 `ValueInPlay` 精确式（§三·三）；`ValueOfEnchantments` 先按「activeEffects 计数」近似或保留 `KeywordValue` 并**标注近似** | RVA 0x939030 / 0x93D3B0 |
| **P3 评分** | 实现 `ScoreFromCriteria`（先 0/1/2/3/35）＋ `ScoreFromPlayingCard`/`ScoreAbility` 的两列遍历 | §三·一 / §三·三 |
| **P4 换表** | 产品路径改为 SO；`ScoreOps` **退役**（只留 `#if` / 自检对照）。`ScoreFromPlayingCard` 不再读 `EffectText.WillRunOps` | 原版没有「正文→分」这一层 |
| **P5 对拍** | 对 1,127 张池卡逐张跑「新分 vs 旧分」，把差异**分类**（单位基础值差异 / 有列卡的差异 / 17 张无列战术）——只用来抓实现 bug，不用来裁决谁对 | 铁律 12：验证是收口时一次性做 |

**切换时 ⛔ 不要做**：

- 不要给「没有评分列表」的卡**造**一个 fallback（`ScoreOps` / 关键词表 / 卡面文本）——原版没有；这会把 AI 变成另一个 AI。
- 不要把 `attackScoringCriteria` 当逐卡表吃（§二）。
- 不要把 `extraScoreFrom*` 当「有数据的列」——全 0。
- 不要把 `ValueOfTraits` 当「卡价值」——它只服务 `ScoreFromJam`；`GetValueInPlay` 用的是 `ValueOfEnchantments`。

### 5.3 原版直证 vs 推断（逐项）

| 项 | 性质 | 证据 |
|---|---|---|
| `attackScoringCriteria` 被 `specialAttackScoringCriteria` 门控 | **直证** | `CardScript__HasSpecialAttackScoring.c:11-15` + `AI__ScoreFromAttack.c:31-37` |
| `nonTarget` 所有卡都跑；`cardTarget` 只给 targeted + 带目标 | **直证** | `AI__ScoreFromPlayingNow` 反汇编 RVA 0x93C300（Ghidra 出口画错，已订正） |
| `targeted` = `rawCard.targeted (0x268)` | **直证** | `EntityScript__get_targeted.c:5-7` |
| `GetValueInPlay` 公式（2×费+1 / 伤势 / 眩晕压制 0.8） | **直证** | RVA 0x939030 + 常量 `0x1834B2BB8/BB4/FA0` |
| `ScoringCriteria` 字段与 option 1/2/9/10 算式 | **直证** | `dump.cs:23723-23740` + RVA 0x93A290 |
| 攻击/治疗子打分常数（ambush 10 / longrange 3 / 1e6 / 1.2 / 1.8 / 0.6 / 0.8 / heal 1×/2×） | **直证** | RVA 0x93B1E0 / 0x93BC30 |
| `extraScoreFromDamaging/Killing/Healing` 的读取点 | **直证** | 同上（Ghidra `.c` 截断，反汇编补出） |
| `hasActiveAbility` 门控动作生成 | **直证** | `BattleManager__GetAvailableActions.c:155` + `SetBaseCardTraits.c:37` |
| `ValueOfEnchantments` 逐 `CardEffect` 的精确语义 | **部分推断** | 骨架直证（RVA 0x93D3B0），`0x48/0x50/0x60` 的字段名未逐一对 `CardEffect` |
| `ScoreFromCriteria` option 4/5/12/13/14/20/25/30/35 的精确系数 | **部分推断** | 已定位到各自 `ScoreFrom*` 子程序，未逐指令核完 |
| 50 张督军 `activeAbility` = Talent | **推断** | 48/50 有 trigger 46 + 卡面无 `Ability:`；代码无直接注释 |
| 运行时实机行为 | **未验** | 原版服务器已关，本轮只读，未跑 Unity |

### 5.4 顺带查出的 `SimpleAI` 偏差（建议一起修，均为直证）

| `SimpleAI` 现写法 | 原版 | 证据 |
|---|---|---|
| `ScoreDamaging`: `ambush → 0` | **10.0** | RVA 0x93B256→0x93B568 |
| `ScoreDamaging`: `longrange → 0` | **3.0** | RVA 0x93B333→0x93B4CE |
| `ScoreDamaging`: 没有 `invulnerable` 支 | **−10.0** | RVA 0x93B2C7→0x93B55B |
| `ScoreHealing`: 督军恒 ×1.5 | `当前血 >10 ? ×1 : ×2` | RVA 0x93BD33-0x93BD45 |
| `ValueInPlay`: `+ KeywordValue`（注释称 `ValueOfTraits`） | `+ ValueOfEnchantments`（activeEffects） | RVA 0x9390A1；`AI__ScoreFromJam.c:9-11` 才是 `ValueOfTraits` |
| `ScoreDraw`: 牌库空 → `-2 - RuleCore.HandMax` | 牌库空 → **`-2 - 疲劳`**（🔴 2026-10-19 订正：本行原写「`-2 - 手牌张数`」，`ebp` 实为疲劳值；RVA 0x93B71B / 0x18093B6DB） | RVA 0x93B610：`0x93B71B mov eax,-2 / sub eax,ebp`（`ebp` = `op_Implicit(fatigue)`） |

> 前三/第四条不在「评分列」问题本身，但都在 `A1797` 这条打分链上，且都是**原版直证** ⇒ 按铁律 11「全部都要做，只有先后」应一并进账。

---

## §六 没查到 / 证据边界

1. **Ghidra 截断**：`AI__GetCardValueInPlay.c`、`AI__ValueOfEnchantments.c`、`AI__ScoreFromDamagingBuffedUnit.c`、`AI__ScoreFromHealingUnit.c`、`AI__ScoreFromDrawingCard.c`、`AI__ScoreFromCriteria.c` 的 `.c` 产物**都在函数中途断了**（返回值和部分浮点运算被吞）。本轮改用 `capstone` 直接反汇编 `GameAssembly.dll`（RVA 见各节）恢复；`.c` 与反汇编冲突时**以反汇编为准**。
2. **`ValueOfEnchantments` 的字段语义**：`CardEffect+0x48/0x50/0x60` 我按数值猜是「攻击/生命/费用类增量」，**未逐字段对 `dump.cs` 的 `CardEffect` 核**。不影响「它不是关键词表」这个结论。
3. **`ScoreFromCriteria` 少数 option 的精确系数**：option 4/5/12/13/14/20/25/30/35 已定位到各自子程序，但没有逐指令核完（本轮重点是「来源清单 + 两列口径」）。T06 实现时按子程序逐个核。
4. **`CardTrait._aIScore` 的资产值**：`ValueOfTraits` 要的 `CardTraitCollection`/`CardTrait` SO **不在 `allcards` 那 1,351 个对象里**（`S_引擎线判据审计.md` 已记）。它只影响 `ScoreFromJam`，不影响出牌主链。
5. **50 张 flag=0 督军的 `activeAbility` 语义**：只做到「48/50 有 trigger 46 + 卡面无 `Ability:` 文本」的旁证；**没有**代码注释/字段名能直接证明它是 Talent。判据不依赖它。
6. **未做运行时验证**：原版服务器已关（`d:/2/unity_run_ref` 只能进场景，不能开局注入卡数据），本轮也没有跑 Unity 自检 —— 报告里所有「原版会/不会打这张卡」都是**代码层结论**，不是实机 observation。
7. **搜过的词/文件**（供下轮避免重复）：
   - 词：`ScoringCriteria` · `attackScoringCriteria` · `cardTargetScoringCriteria` · `nonTargetScoringCriteria` · `specialAttackScoringCriteria` · `extraScoreFrom` · `ValueOfEnchantments` · `ValueOfTraits` · `GetCardValueInPlay` · `GetCardReferenceValueInPlay` · `hasActiveAbility` · `GetActiveAbility` · `ScoreFromPlayingNow` · `ScoreFromCriteria` · `ScoreFromDamagingBuffedUnit` · `ScoreFromHealingUnit` · `CheckIfMeetsCriteria` · `GetAvailableActions`
   - 文件：`d:/2/tools/decomp_full/`（全部 `AI__*.c`、`CardScript__*`、`RawCardScript__*`、`BattleManager__*`、`EntityScript__*`、`SupportMethods__*`）· `d:/2/tools/il2cpp_out/dump.cs` · `d:/2/tools/decomp_AI/`（与 `decomp_full` 同内容）· `d:/2/Warpforge_tools/tmp/full_decomp/AI.cs`（**只有方法签名、无方法体**）· `d:/2/Warpforge_tools/data/decomp_il2cpp_0827/`（同样的截断）· `GameAssembly.dll`（RVA 反汇编）
   - 数据：`cards_raw.json`（1,351）· `cards_1195_2022时代.json`（1,195）· `cards_truth.json`（运行期）· `cards_engine.json`（池 1,127）· `prebuilt_decks_full.json`（236 副 / 876 张去重）

### 附：池内 17 张「没有任何评分列表」的战术卡（`cardType=20`）

`EC58 Vengeful Surge` · `AM50 Thermal Weaponry` · `UM114 Lead From the Front` · `TL71 Toxic Entanglement` · `ASH49 Eldritch Storm` · `SW58 Fenrisian Wolfpack` · `TL53 Swarming Masses` · `TL48 Predator Instincts` · `UM117 No Respite` · `GSC53 Enhanced Aggression` · `SW41 Pack of One` · `TL52 Leviathan's Tendrils` · `ASH43 Sudden Assault` · 以及 4 张 enchantment：`Mark of Slaanesh / Nurgle / Tzeentch / Khorne (enchantment)`

> 这 17 张里 **11 张出现在预组卡组**（`prebuilt_decks_full.json`）—— 按原版代码它们出牌分 = 0 ⇒ **原版 bot 拿到手里也不会打**（0 分不能替换 `endTurn` 基准，`AI__SelectBestAction.c:25-37` + `b__8_0` 的初始 best = endTurn）。这是原版行为，不是我们该补的兜底。

### 附：池内 620 张「没有任何评分列表」的构成

| 类型 | 数量 | 原版出牌分 |
|---|---|---|
| `cardType=0` 单位 | **547** | `GetValueInPlay`（费用×2+1 + 附魔，伤势/眩晕修正） |
| `cardType=10` 督军 | **56** | 不从手牌打出（无出牌动作） |
| `cardType=20` 战术 | **17** | **0**（名单见上） |

—— 完 ——
