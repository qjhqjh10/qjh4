import io

main = '战斗逻辑驱动.tsv'
raw = io.open(main, 'rb').read()
lines = raw.split(b'\n')
hdr = lines[0].split(b'\t')
assert len(hdr) == 11, len(hdr)
byrow = {}
for i, l in enumerate(lines, 1):
    if not l.strip():
        continue
    c = l.split(b'\t')
    if len(c) == 11:
        byrow[i] = c

NEW = {}

NEW[172] = dict(status='C',
    locus='RuleEngine/Core/EffectResolver.cs:18480（`RunActiveRowOpsOrGrantedTrigger` 的 `ca == 85` 分派，同一行内追加）· RuleEngine/Core/EffectResolver.cs:19179（新增 `RunTransformUnitRow`）· RuleEngine/Core/TransformUnit_B94B12.cs:178（`RuleCore.TransformUnit` = 下游出口，已逐跳落齐）· RuleEngine/Core/CardDef.cs:9185（`AbilitySummonAt` 取 `keepEffects`）',
    diff='🆕 2026-10-29（B96_CA · 账 172 / 548 合并做）：**生产端分派已落** —— `chosenAbility 85` 现在走 `RunTransformUnitRow` → 直调 `RuleCore.TransformUnit`（等价做法，判据 `AbilityLogic__PlayAbility.c:904-941`；我们没有 27 号动作道，不另造）。四处如实记的缺口：① **计划表闸仍挡着** —— `RuleCore.UseActiveAbilityCore` 的 `else if (ActiveRowHasOps(u, j) || ca == 360 || ca == 370 || ca == 400)`（`RuleCore.cs:15424`）还没放 85/350 ⇒ 正文没配到 op 的 85 行会被当成 `unimplemented` 剔掉，**整支仍不可达**（`RuleCore.cs` 不在本代理白名单，补丁已交回）；② 「变成哪张卡」只能取卡面 `become` op 的 `Payload`（`summonCriteria.unitsToSummon` 的**主动能力版**没进卡表 —— 生成器 `ability_summon_of` 只落 `cardAbilities[].abilityLogic[]`）；③ `keepEffects` 走 `AbilitySummonAt(op.AbilityIndex, op.LogicIndex)`，active 行今天**必然**取不到 ⇒ 走「出声 + 不结算」支（数据缺口，不是逻辑缺口）；④ 原版随变身挂的 `buffEffect(+0x70)` 没接（`TransformUnit` 的签名没有这一格，那是 `TransformUnit_B94B12.cs` 的口）。池内 `chosenAbility 85` = **0/1,351** ⇒ 休眠件。',
    level='B', round_=' · B96_CA',
    note='🆕 2026-10-29（B96_CA）：分派入口 + 执行体已落（见落点）；**剩下的一跳在 `RuleCore.cs:15424`**（加 `|| ca == 85`），交回调度台派给 `RuleCore*.cs` 的写手。同批 `548` 靠这一跳闭环（`TransformUnit` 本体早已完整）。')

NEW[534] = dict(status='C',
    locus='RuleEngine/Core/BattleContext.cs:2558（新增 **`AddRemoveFromCemetery(CardInstance, priority, actingCard, isPlayer, blockPlayerActions)`** —— 入队 `actionType 60`，动作体 = `RuleCore.ExileFromCemetery`）· RuleEngine/Core/EffectResolver.cs:18480（`ca == 350` 分派）· RuleEngine/Core/EffectResolver.cs:19282（新增 `RunRemoveFromCemeteryRow`）· RuleEngine/Core/RuleCore.cs:16911（`ExileFromCemetery` = 结算体，已有）',
    diff='🆕 2026-10-29（B96_CA）：**`AddRemoveFromCemetery` 从零建好了**（旧账说「`R-B1` 已另发 F4」在本工作树里**从未落地** —— `grep -rn AddRemoveFromCemetery` 只命中注释，本轮现核）。判据 `BattleManager__AddRemoveFromCemetery.c:31-35`（`BattleAction___ctor(action, 0x3c(60), isPlayer, actingCard, block, 0)` + `card = param_2` + `AddAutoActionToQueue(priority)`；卡为空**不入队**）+ `ResolveRemoveFromCemetery.c` → `CardScript__RemoveCardFromCemetery.c`（全文一句 `*(card+0x24b)=0`）⇒ 动作体 = `ExileFromCemetery`。**三处如实记的缺口**：① **计划表闸**（`RuleCore.cs:15424`）没放 350 ⇒ 整支仍不可达（补丁已交回）；② **`ta = 60`（墓地目标）我们没建** —— `ResolveTargets` 只到 `UnitState`，墓地表是 `CardInstance` ⇒ 目标解不出，走「出声 + 不结算」支（⛔ 不许退化成「扫整份墓地全摘」= 多摘）；③ 原版逐目标各入队一条，我们照做（`while` 语义），但目标恒空 ⇒ 今天 0 条。池内 `chosenAbility 350` = **0/1,351** ⇒ 休眠件。',
    level='B', round_=' · B96_CA',
    note='🆕 2026-10-29（B96_CA）：`AddRemoveFromCemetery` = `BattleContext.cs` 类尾新 partial 块（零漂移）。**剩下的一跳在 `RuleCore.cs:15424`**（加 `|| ca == 350`）。②的修法：给 `ResolveTargets` 补 `ta = 60` 一档（读 `RuleCore.GetCardsInCemetery`，`incl*` 六开关按 `targetCriteria` 落）—— 属 `EffectResolver` 但**会改全部墓地目标卡的选法**，需单独一批 + 指纹核对。')

NEW[482] = dict(status='B',
    locus='RuleEngine/Core/UnitState.cs:3607（新增 **`ContainsExtrinsicEffectsFrom(sourceCardName)`** = 原版 `CardScript.ContainsExtrinsicEffectFrom` 的谓词）· RuleEngine/Core/EffectResolver.cs:19353（新增 **`RemoveExtrinsicEffectsFrom(ctx, actingCard, priority)`** = 先查后排队、命中才入队**一条** `actionType 47`）· RuleEngine/Core/EffectResolver.cs:19442（新增 **`WhileInPlayVariablePass`** = 调用点）· RuleEngine/Core/ActionPump.cs:143（接线：同一行**前置** `RuleCore.WhileInPlayVariablePass(_ctx)`，排在「重加」`Auras.RefreshInPlayNumericAndKeywords` 之前）· RuleEngine/Core/Aura.cs:1046（`RemoveExtrinsicBySource`，摘除唯一入口）',
    diff='🆕 2026-10-29（B96_CA）：原版两半都落了 —— ① **谓词**（`CardScript__ContainsExtrinsicEffectFrom.c` 全文四条合取：`extrinsic(+0x30) != 0` ∧ `enchantingCard(+0x18) == 参` ∧ (`buffType(+0x38) == 参 || 参 == 0`) ∧ `baseEnchantmentCard(+0x98) == 参`；唯一调用点 `RemoveExtrinsicEffectsFrom.c:48/73/91` 三次都传 `(卡, actingCard, 0, 0)`）；② **「240 不再可触发 ⇒ 整份摘」那一下**（`RawCardScript__TriggerAbilities.c:207-221`，`priority` 恒 1）+ 排在「先摘再重加」的**摘**那一半（我们「重加」= `RefreshInPlayNumericAndKeywords`）。**如实标的残余**：ⓐ 我们三本账（`_statMods.Source` / `_buff.SourceCard` / `_grant.Source`）**没有 `extrinsic` 这一列** ⇒ 谓词退化成「来源匹配」，与原版「只摘 extrinsic」相比会**多撤**（`extrinsic != 0` 闸做不到，缺「施放者」列，与 636 同一口径）；ⓑ **手牌两跳没做**（手牌是 `CardInstance`，常驻修饰在 `ctx.CostMods`，不是同一份模型）；ⓒ 「240 触发不了」的等价判据 = 「没有 240 正文 op **且** 不由光环层承载」（`Card.AuraSpecs` 空 ∧ `!Card.MeleeEqualsHealth`）—— 原版是按 `CanTriggerAbility` 逐 ability 判，我们没有那条链。📊 池内 `abilityTriggers` 含 240 = **33 张**（现扫 `cards_engine.json`）；接上后 `ruleprobe play check` 指纹**一字未变**（开/关这一趟各跑一次，均 `431d7b0d…`）⇒ 今天零行为变化。',
    level='P1', round_=' · B96_CA',
    note='🆕 2026-10-29（B96_CA）：谓词 + 生产者 + 调用点齐全，残差 = extrinsic 闸 + 手牌两跳（同 636）。⛔ 别把 `Auras.Recompose` 挂到这一趟（`E1` 已证会重放夹血/费用登记，见 `Aura.cs:1037-1041`）。')

NEW[636] = dict(status='B',
    locus='RuleEngine/Core/EffectResolver.cs:19400（新增 **`BroadcastRemoveEffectsFrom(ctx, sourceCardName)`** = 动作 47 的结算体，动作体内调 `Auras.RemoveExtrinsicBySource`）· RuleEngine/Core/EffectResolver.cs:19353（入队侧 `RemoveExtrinsicEffectsFrom`，`actionType: 47`）· RuleEngine/Core/Aura.cs:1046（`RemoveExtrinsicBySource`，含 `IsInPlay` 闸）',
    diff='🆕 2026-10-29（B96_CA）：**「动作类型 47 那一拍接线」已落** —— 全仓第一次出现 `actionType: 47` 的生产者（`RuleEngine/Core/EffectResolver.cs:19353`）+ 它的结算体（`:19400`）。判据复核：`BattleManager__ResolveAction.c:4168` 的 `case 0x2f (47)` → `ResolveRemoveEffectsFrom.c:5` → `BattleManagerSupport__BroadcastRemoveEffectsFrom.c` 三跳（棋盘 `IsInPlay` 逐张 / 当前方手牌 / 另一方手牌）。**仍实的残余**（不变）：① 我们只覆盖**棋盘那一跳**（`Aura.RemoveExtrinsicBySource` 内含 `IsInPlay` 闸），**手牌两跳缺**；② **`extrinsic != 0` 那道闸做不到**（缺「施放者」列 ⇒ 「来源匹配即撤」比原版**多撤**）。',
    level='P2', round_=' · B96_CA',
    note='🆕 2026-10-29（B96_CA）：账差异列原来那条「① 统一入口补 `IsInPlay` 闸」**已过期**（`B95_WRS` 已补，`Aura.cs:1059-1074`），本轮把它从「还差」里划掉。② 那条仍是已知残差（与 482 同源）。')

NEW[853] = dict(status='C',
    locus='RuleEngine/Core/EffectResolver.cs:18087（`RemoveEffectRecordFrom`，声明行现读 18087 —— 账里旧写 18089 是**行号漂移**）· RuleEngine/Core/UnitState.cs:2756（`RemoveEffectRecord`）· 🆕 RuleEngine/Core/EffectResolver.cs:19502（**新增 `ResolveRemoveEffect`** = `removeEffect(16)` 动作体的唯一结算口，内含 `RemoveEffectResolveCountForTest` 计数）· 🆕 RuleEngine/Core/ActionPump.cs:2233（**新增 `EnqueueRemoveEffect`** = 按一条记录摘的入队口，`actionType: 16`）',
    diff='🆕 2026-10-29（B96_CA）：「**先接分派入口**」这一步做完了 —— 有了 `EnqueueRemoveEffect`（入队）+ `ResolveRemoveEffect`（动作体，走 `RemoveEffectRecordFrom`）+ **计数器**（让「按记录摘」这条路从此**可观测**，这正是账 853 之所以改不动的症结）。**但仍是 C**：① 两个入口**今天 0 调用点** —— 现网三条 `removeEffect(16)` 的动作体全是就地整批摘（`RuleCore.cs:16510` 的 `() => unit.RemoveTempBuff(buff)`、`Enchantment.cs:983` 附魔那条）；接线要改 `RuleCore.cs` / `Enchantment.cs`，**都不在本代理白名单**⇒补丁已交回；② 逐 case 的四个真差异**没动**（case 3/90 无条件 `RemoveAll` 多摘 · case 5 不清残留攻/血 · case c/f/10 整档漏 · case 2d 攻击半未落）；③ `TempBuff` **没有 `buffType` 列** ⇒ 不能凭空造 `TempBuff → EffectRecord` 的桥（造一个 `BuffType` 就是猜，会静默走错 case）⇒ 接线时要么给 `TempBuff` 补这一列，要么让调用点持有 `EffectRecord`。',
    level='P0', round_=' · B96_CA',
    note='🆕 2026-10-29（B96_CA）：落点行号订正 18089 → **18087**（`check_ledger_points` 的漂移项，本轮实测声明行确为 18087）。接线补丁（两处，均一行换一行）：① `RuleCore.cs:16510` 的 `() => unit.RemoveTempBuff(buff)` ⇒ 先给 `TempBuff` 补 `BuffType` 列再走 `RuleCore.EnqueueRemoveEffect(ctx, unit, rec)`；② 之后再把 `RemoveTempBuff` 那条整批路按 case 拆开（账 853 的逐 case 那件活）。')

NEW[1288] = dict(status='B',
    locus='RuleEngine/Core/EffectResolver.cs:18447（`FireTriggerOnce`，取正文的口已改为按触发数值）· 🆕 RuleEngine/Core/CardDefPartial_B96CA.cs:102（**新增 `TriggerOpsByNumber(trigger)`** = 「触发数值 → 正文 op」的唯一取值口；`:61` `TryAbilityTriggerNameOfNumber` 反查；`:80` `AttachTriggerOpsByNumber` 给数据半用）· 🆕 RuleEngine/Core/EffectResolver.cs:19516（**新增 `TriggerOncePass`** = 调用方「试一次、成功就置 false」）· 🆕 RuleEngine/Core/UnitState.cs:3635（**新增 `TriggerOnceFired`** = 原版 `triggeredAbility` 位）· RuleEngine/Core/ActionPump.cs:143（接线：`ActionScope.Dispose` 同一行前置 `RuleCore.TriggerOncePass(_ctx)`）',
    diff='🆕 2026-10-29（B96_CA）：**代码半全落**。① 取正文的口不再写死卡面关键词，改成 `CardDef.TriggerOpsByNumber(200)`（两级：先读数据半挂进来的表，再退回「卡面关键词 == 触发词名」那一格）；② **once 标志**（`UnitState.TriggerOnceFired`，判据 ILSpy `CardScript.cs:16033-16039` 的 `if (triggeredAbility && UpdateTriggerOnce(...)) triggeredAbility = false;`）；③ **调用点**（`ActionPump.ActionScope.Dispose`，= 原版 `UpdateWhileInPlay` 一趟，⛔ 不进 `PumpActionQueues`）。**仍缺的只有数据半**：生成器要输出「`trigger` 数值 → 该 ability 的正文 op」，`CardDatabase` 解析时调 `AttachTriggerOpsByNumber`（`工具/gen_cards_engine.py` 不在本代理白名单）。⚠️ 另一处口径限制：`FireTriggerAt` 按关键词取正文、**没有 `opsOverride` 形参** ⇒ 数据半挂进来的表今天**只能当闸**，要让它真的成为正文来源得给 `FireTriggerAt` 加一个形参（`RuleCore.cs`，交回补丁）。📊 池内 `abilityTriggers` 含 200 = **0/1,129** ⇒ 零行为变化。',
    level='P0', round_=' · B96_CA',
    note='🆕 2026-10-29（B96_CA）：账原来那句「缺 ②once 标志 ③调用点」**已过期**（本轮补上）。剩下的：数据半（生成器）+ `FireTriggerAt` 的 `opsOverride` 形参（一行补丁：形参表尾加 `List<EffectOp> opsOverride = null`，然后 `var ops = opsOverride ?? (only != null ? ... : ...)`）。')

out = [b'\t'.join(hdr)]
for r in (172, 534, 482, 636, 853, 1288):
    c = list(byrow[r])
    n = NEW[r]
    c[5] = n['status'].encode('utf-8')
    c[6] = n['locus'].encode('utf-8')
    c[7] = n['diff'].encode('utf-8')
    c[8] = n['level'].encode('utf-8')
    if n['round_'] not in c[9].decode('utf-8'):
        c[9] = (c[9].decode('utf-8') + n['round_']).encode('utf-8')
    c[10] = (c[10].decode('utf-8') + ' ｜ ' + n['note']).encode('utf-8')
    out.append(b'\t'.join(c))

data = b'\n'.join(out) + b'\n'
io.open('_parts/B96_CA.tsv', 'wb').write(data)

back = [x for x in io.open('_parts/B96_CA.tsv', 'rb').read().split(b'\n') if x.strip()]
print('lines:', len(back))
for l in back:
    cc = l.split(b'\t')
    print(len(cc), cc[1].decode('utf-8'), '|', cc[2].decode('utf-8')[:50], '|', cc[5].decode('utf-8'), '|', cc[8].decode('utf-8'))
