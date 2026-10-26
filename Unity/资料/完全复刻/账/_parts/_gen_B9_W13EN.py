# -*- coding: utf-8 -*-
# 生成 B9_W13EN.tsv —— 只写本批（第八十四会话 · W13EN）改过的 8 行；键 = (原版类, 方法) 精确串。
# 做法 = 读主表原文 → 只覆盖本批涉及的列 → 写出（避免手抄长行出错）。
# ⚠️ 落点格式：每个引用一律 `<路径>:<行>（<裸声明名>）`（判法见 工具/check_ledger_points.py 文件头）。
import io, csv, os

HERE = os.path.dirname(os.path.abspath(__file__))
MAIN = os.path.join(HERE, '..', '战斗逻辑驱动.tsv')
OUT = os.path.join(HERE, 'B9_W13EN.tsv')

rows = list(csv.reader(io.open(MAIN, encoding='utf-8', newline=''), delimiter='\t'))
COLS = rows[0]
assert len(COLS) == 11, COLS
by_key = {}
for r in rows[1:]:
    if len(r) < 11:
        continue
    by_key[(r[1], r[2])] = list(r)

PATCH = {}

def take(cls, method):
    k = (cls, method)
    assert k in by_key, ('未找到行', k)
    r = list(by_key[k])
    assert len(r) == 11, (k, len(r))
    return r

# ---------------------------------------------------------------- 64 AdjustCardEffect
k = ('AbilityLogic', 'AdjustCardEffect(ref CardEffect basicEffect, AbilityData abilityData, int multiplier, BattleManager manager)')
r = take(*k)
r[4] = (r[4] +
        "｜🆕 **2026-10-26 批次 9 `W13EN`**：亲读 `AbilityLogic__AdjustCardEffect.c:1-59` **全文** + "
        "`CardScript__AddEffect.c:462-570` ⇒ 订正一处机制表述：**`AdjustCardEffect` 只乘五对数值字段**"
        "（`+0x48 melee / +0x50 range / +0x60 health / +0x58 armour / +0x68 cost`），**⛔ 不碰 `traitValue(+0x40)`**；"
        "`traitValue = change × N` 是 `AddEffect` 的 `case 3` **记录合并**自然得到的（新建时 count 强置 1 `:496` · "
        "replace 取 Max `:531-535` · add 累加 `:544` · count +1 `:554`）⇒ 该补的是「**一条记录**」，不是「乘」。")
r[6] = ("RuleEngine/Core/UnitState.cs:2983（ApplyTraitEffect）（等价实现 = 原版 `CardScript__AddEffect.c` `case 3` 的"
        "**记录合并**：把「N 次施加」落成**一条记录**、`traitValue = change × N`）"
        "+ 同族口：RuleEngine/Core/UnitState.cs:2901（TraitEffectRecord）· "
        "RuleEngine/Core/UnitState.cs:2945（TraitEffectValue）· "
        "RuleEngine/Core/UnitState.cs:3044（FindTraitEffect）· "
        "RuleEngine/Core/UnitState.cs:3070（RemoveTraitEffect）"
        "+ 原落点仍在、**未切到上面那口**：" + r[6])
r[7] = (r[7] +
        "｜🆕 **2026-10-26 `W13EN`**：`UnitState` 侧的记录形状**已备好** —— `ApplyTraitEffect` 按 `CardTrait._stackable` "
        "合并（`add` 累加 / `replace` 取 Max / `count` 每次 +1 且新建强置 1）、`extrinsic` 不参与合并、"
        "合并键 = `trait + markOfChaosType(+0x78) + daemonHostType(+0x7c) + TimingBits(≡ SameTimingAsEffect 五个 until* 位)`；"
        "**剩的是 `EffectResolver` 侧切调用**（`ApplyOneGain` 的 `IsKeyword` 支在 `buffType ∈ {3,90}` 时改走 "
        "`ApplyTraitEffect(val)`）→ 改动请求见报告 §五。可达性：池内 `_stackable == replace` **0 条** ⇒ 差异②今天不可观察；"
        "① `count`（`markOfChaos 330`）原版一条实例 `traitValue = N`、我们 `_keywords = N` ⇒ **读数等价、只差实例**。")
r[10] = (r[10] +
         "｜🆕 2026-10-26（第八十四会话 · `W13EN`）：UnitState 侧的 `addTrait` 记录账已落（见落点）；"
         "`EffectResolver` 那一半仍未做 ⇒ 保持 C。⚠️ 就地订正「`AdjustCardEffect` 把 `traitValue` 乘 N」这句的字面说法："
         "它只乘五对数值字段（判据 = 该 `.c` 全文），×N 来自记录合并 ⇒ **施工结论不变**（要的是记录，不是乘）。")
PATCH[k] = r

# ---------------------------------------------------------------- 481 RemoveEnchantment
k = ('BattleManager', 'RemoveEnchantment(CardScript card, CardEffect enchantment, int priority)')
r = take(*k)
r[6] = ("RuleEngine/Core/Enchantment.cs:908（EnqueueRemove）（等价实现 = 原版那一步「组装 "
        "`BattleAction{actionType=0x10 removeEffect}` 带 carrier+effect 两个对象引用 → `AddAutoActionToQueue(action, priority)`」："
        "走 `RuleCore.EnqueueAbility(ctx, lane, …)`、出队时调 RuleEngine/Core/Enchantment.cs:287（Remove）；"
        "**入队口已备、调用点未切**）")
r[7] = ("原版 = **只入队**（`BattleManager__RemoveEnchantment.c:26-79`，83 行；本条已按 `B9_E4` 的订正口径）；"
        "我们在 2026-10-26 前是**调用点同步摘**（`Remove` / `RemoveByCard` / `RemovePray`）。"
        "🆕 **入队口已实现**（`Enchantment.EnqueueRemove`，`lane` 缺省 `Instant` = 原版默认 `priority 1`，"
        "判据 = `CardAbility__ctor.c:5` 写 `+0x18 = 1`），**但调用点未切**：`RemovePray` 的三个落点"
        "（`RuleCore.cs:1629`「先摘再给」/ `:3079` / `:8813`）仍是同步 —— 切它要先定那条时序"
        "（改成入队会把「立刻发一次」变成「晚一拍发」）⇒ **切换请求见报告 §五**（本写手不动 `RuleCore`）。")
r[10] = (r[10] +
         "｜🆕 2026-10-26（`W13EN`）：入队口已建（`Enchantment.EnqueueRemove`）；调用点未切 ⇒ 保持 C。"
         "532 的 ②③（撤四条队列道 / `RemoveDestroyableAnims`）在表现层，另见 532 行。")
PATCH[k] = r

# ---------------------------------------------------------------- 532 ResolveRemoveEffect
k = ('BattleManager', 'ResolveRemoveEffect(CardScript targetCard, CardEffect cardEffect)')
r = take(*k)
r[6] = ("RuleEngine/Core/Enchantment.cs:908（EnqueueRemove）（等价实现 = 那条 `removeEffect(16)` 动作的出队体：先调 "
        "RuleEngine/Core/Enchantment.cs:287（Remove），对应原版 ① `targetCard.RemoveEffect(effect)` —— "
        "我们由 RuleEngine/Core/UnitState.cs:1405（RevertGrantsFrom）＋ `Enchantments.Remove` 分担；④ "
        "`FinishResolvingAction` 已有；② 四条队列道撤 anim cue ／ ③ `RemoveDestroyableAnims`：**无落点**）")
r[7] = (r[7] +
        "｜🆕 **2026-10-26 `W13EN`**：①和④的等价链现在挂在**同一条入队体的 lambda** 里（`EnqueueRemove` 出队时先 `Remove`）；"
        "②③ **不在本层** —— anim cue（`CancelAnimFromUnitFromQueue` 连走 `actionQueue / instantQueue / interruptQueue / delayedInstantQueue` 四条道）"
        "与 `RemoveDestroyableAnims`（`buffType ∈ {15,16}` 的 `TraitFrameEffectsPlay`）都在 `CardPresentation/Battle/`（P-11 的面）⇒ "
        "逐段判据与改动请求见报告 §五。")
r[10] = (r[10] +
         "｜🆕 2026-10-26（`W13EN`）：①/④ 的等价链已备（在入队体里）；②③ 属表现层（P-11）⇒ 保持 C。")
PATCH[k] = r

# ---------------------------------------------------------------- 687 AddEnchantment
k = ('CardScript', 'AddEnchantment(CardEffect newEffect)')
r = take(*k)
r[5] = 'A'
r[6] = ("RuleEngine/Core/Enchantment.cs:208（Attach）（等价实现 = 五步全对上：① 空卡闸 ② 惰性建记录表 ③ "
        "`ID = ctx.EnchantmentsUsed` 后自增（RuleEngine/Core/Enchantment.cs:236（Attach）那一句）④ 同时追加 "
        "`ActiveEnchantments` + `ActiveEffects` ⑤ **本批补上** `CardScript__AddEnchantment.c:91` 的 "
        "`ActivateTraitsOnSummonOrEnchantment(onSummon:false)` → RuleEngine/Core/Enchantment.cs:938（ActivateTraitsAfterEnchant）"
        "→ `RuleCore.ActivateTraitsOnSummonOrEnchantment`）")
r[7] = ''
r[10] = (r[10] +
         "｜🆕 **2026-10-26（`W13EN`）第五步补上 ⇒ 升 A**。判据 = `CardScript__AddEnchantment.c:91` → "
         "`CardScript__ActivateTraitsOnSummonOrEnchantment.c` 全文：它遍历的是**载体自己的** `CurrentTraits(+0x128)`，"
         "只重算 canAct/canAttack（`fast`/`flank` ⇒ canAct=true；`ferocity`/`oath` ⇒ canAct=true 且 canAttack=false；"
         "`dropPod 0xe6` 那支只在 `onSummon==true` 才动）—— **不把附魔卡的 trait 并进载体**。"
         "🔴 **就地订正上一版施工步骤里「把附魔卡的 addTrait 并进载体」的口径**（那是读错了那一跳；真并进去反而与原版不符）。"
         "残余：同一跳原版还对每条 trait 调 `BattleCardUI.DisplayStatusAnim`（表现层，P-11）。"
         "可达性：`AttachPray` 在生产路径上（23 张 pray 卡），其中只有 `SOR69 Zephyrim Superior` 带 `flank` ⇒ "
         "`ruleprobe play check` 实测 **0 差异**。")
PATCH[k] = r

# ---------------------------------------------------------------- 763 GetQuestTimes
k = ('CardScript', 'GetQuestTimes()')
r = take(*k)
r[5] = 'A'
r[6] = ("RuleEngine/Core/Enchantment.cs:783（GetQuestTimes）（等价实现 = 遍历 `ActiveEnchantments`，返回**第一条** "
        "`Card.QuestTimes > 0` 的那个值；全无 ⇒ `0`）")
r[7] = ''
r[10] = (r[10] +
         "｜🆕 **2026-10-26（`W13EN`）取值口已实现 ⇒ 升 A**。🆕 可达性新证：`cpp2il/out_cpp2il_dev_ilspy`（全方法体）与 "
         "`out_il2csharp` 全树**只有定义、0 调用点** ⇒ 原版自己也不调它（死 API）⇒ 我们**不接线才忠实**；"
         "`spellType == 120` 池内 0 张，接上也不改任何对局。")
PATCH[k] = r

# ---------------------------------------------------------------- 770 HasCancelAttackEffect
k = ('CardScript', 'HasCancelAttackEffect(CardScript actingCard, CardScript targetCard)')
r = take(*k)
r[6] = ("RuleEngine/Core/Enchantment.cs:821（FindCancelAttack）（等价实现 = **完整形态**：① 逐件 ② "
        "`HasAboutToAttackAbility`（= 原版 `RawCardScript.HasAbilityType(0x118)`，读 v17 真值列 "
        "`CardDef.AbilityTriggerPresence`）③ 四道闸走 `RuleCore.IsRightContext` / `ActingLevelCriteriaAllows` / "
        "`AbilityLevelCriteriaAllows`）· 旧单参重载仍在 RuleEngine/Core/Enchantment.cs:603（FindCancelAttack）"
        "（只判「附魔卡有没有 AboutToAttack 正文」）")
r[7] = ("原版逐件过 `CardAbility.CanTriggerAbility(0x118, bm, 载体, actingCard, targetCard, ignoreRandom)` 的"
        "**五道闸**（`CardAbility__CanTriggerAbility.c:1-143`：trigger 号 / `IsRightContext` / "
        "`actingCardCriteria(+0x28)` / `targetCriteria(+0x30)` / `boardCriteria(+0x38)`）—— "
        "🆕 2026-10-26 `W13EN` 已把五道闸的等价链**实现**进 `FindCancelAttack(ctx, carrier, actingCard, targetCard, ignoreRandom)`。"
        "**剩余（⇒ 仍判 C）**：① 唯一调用点 `RuleCore.cs:7994` **还在用旧单参重载**（切过去要先把它手上的 ctx 与 "
        "acting/target 两个实参带下来）；② 三处残余：同卡同 trigger 多条 ability 时走既有「定位不唯一 ⇒ 不触发 + 出声」、"
        "`ignoreRandom` 无等价位（出声后按 false）、附魔卡多半不在 `cards_engine.json` ⇒ 三道闸回「判不了 ⇒ 放行」。")
r[10] = (r[10] +
         "｜🆕 **2026-10-26（`W13EN`）完整形态已实现**（`Enchantment.cs:821`）；调用点（`RuleCore.cs:7994`）未切 ⇒ 保持 C。"
         "可达性：原版两个调用点 = `BattleManager.CheckIfAbilityCancelsAttack` / `CardScript.HasCancelAttackAbility`；"
         "池内 0 张卡挂「带 280 能力」的附魔 ⇒ 今天零可观察。")
PATCH[k] = r

# ---------------------------------------------------------------- 1044 HasEnchantmentWhileInPlay
k = ('EntityScript', 'HasEnchantmentWhileInPlay()')
r = take(*k)
r[5] = 'B'
r[6] = ("RuleEngine/Core/Enchantment.cs:753（HasEnchantmentWhileInPlay）（等价实现 = 逐件读附魔的 `whileInPlayAnim`）"
        "· 数据位暂存在 RuleEngine/Core/Enchantment.cs:150（WhileInPlayAnim）（三态，`Attach(..., whileInPlayAnim:)` 填）"
        "· 诚实计数口 RuleEngine/Core/Enchantment.cs:762（CountUnknownWhileInPlayAnim）")
r[7] = ("方法形状与判据**逐条一致**（判的是**附魔卡自己**的 `whileInPlayAnim(+0x288)`，不是「身上有没有附魔」；"
        "`enchantmentCard == null` 那一格原版 NRE ⇒ 我们不复制崩溃、跳过并计数）。"
        "**未对齐的那一半 = 数据列**：`whileInPlayAnim` 还没进卡表 ⇒ `Attach` 缺省 = 未知 ⇒ 今天对所有附魔返回 false。"
        "数据**已在** `资料/普查产出_1018/allcards_1350/cards_raw.json` 的 **`full_raw.whileInPlayAnim`**"
        "（1,351 张各一条；`true` = **41** 张，其中**附魔卡 3 张**：`eab54e4c…` T16 Sautekh-Undying Legions · "
        "`a5960475…` T18 Leviathan-Hive Fleet Leviathan · `7c06b811…` Z-SW-Everstorm）⇒ 等 P-07 落 "
        "`CardDef.WhileInPlayAnim` 后把 `Attach` 的缺省改成读它，**本方法一行不用动**。")
r[10] = (r[10] +
         "｜🆕 **2026-10-26（`W13EN`）**：谓词已按 `EntityScript.cs:1843-1874` 逐句实现（含空引用那一档）；"
         "**数据列未接 ⇒ 保持 B（等 P-07）**。🆕 可达性：`out_cpp2il_dev_ilspy` / `out_il2csharp` 全树 **0 调用点** ⇒ "
         "原版自己也不调它（死 API）⇒ 我们也不接线是忠实的。")
PATCH[k] = r

# ---------------------------------------------------------------- 1047 HasSecretOrder
k = ('EntityScript', 'HasSecretOrder(RawCardScript enchantment)')
r = take(*k)
r[5] = 'A'
r[6] = ("RuleEngine/Core/Enchantment.cs:573（FindSecretOrder）（等价实现 = ① 闸 "
        "RuleEngine/Core/Enchantment.cs:518（CountSecretOrders）`<= 0 ⇒ null`（数的正是 `ActiveEnchantments` + "
        "`SpellType == 0x3C`，与原版 `HasSecretOrders()` 同口径）② 体扫 `ActiveEffects`，取 "
        "`IsEnchantmentBuffType` 且 `EnchantmentCard` 引用相等的那条）")
r[7] = ''
r[10] = (r[10] +
         "｜🆕 **2026-10-26（`W13EN`）：`EntityScript.cs:1977-1981` 的那道闸已补 ⇒ 升 A**"
         "（判 C 的唯一原因就是缺它；⛔ 别把闸改成数 `ActiveEffects` —— 那正是原版的坑）。"
         "✅ 原版唯一调用点 = `BattleManager._ResolveRevealOrders`（密令揭示链，本仓未建）⇒ 池内零可达。"
         "断言：只往 `ActiveEffects` 塞一条同卡附魔型效果、`ActiveEnchantments` 为空 ⇒ 返回 null（改前返回该 effect）。")
PATCH[k] = r

out = ['\t'.join(COLS)]
for k, r in PATCH.items():
    assert len(r) == 11, (k, len(r))
    r = [c.replace('\r', ' ').replace('\n', ' ｜ ').replace('\t', ' ') for c in r]
    out.append('\t'.join(r))
io.open(OUT, 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
print('wrote', OUT, len(PATCH), 'rows')
