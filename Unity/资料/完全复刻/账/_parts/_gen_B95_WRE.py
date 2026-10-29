# -*- coding: utf-8 -*-
"""生成 账/_parts/B95_WRE.tsv（B95_WRE 报回的 13 行）。用完可删。"""
import io

main = 'd:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv'
b = io.open(main, 'rb').read()
assert b.count(b'\r\n') == 0, 'main tsv is CRLF?'
lines = b.decode('utf-8').split('\n')
hdr = lines[0].split('\t')
assert len(hdr) == 11, len(hdr)


def find(cls, meth):
    hits = []
    for i, l in enumerate(lines[1:], 2):
        c = l.split('\t')
        if len(c) < 11:
            continue
        if c[1] == cls and c[2] == meth:
            hits.append((i, c))
    assert len(hits) == 1, (cls, meth, len(hits))
    return hits[0]


TAG = '[B95_WRE 2026-10-29]'
UPD = {}

UPD[('BattleManager', 'ChoiceOfCardEnemy(CardScript actingCard, CardScript targetCard, int selectedCard)')] = dict(
    status='B',
    diff_append='｜' + TAG + ' 现读复核 **未改**：施工单 = `报告/B94_Z3.md` §3·2 —— 把 `GetChoiceOptions` 等价物收进引擎（`N = min(pool, choicesDefinition.maxTargets(+0x40))` → 循环抽 → `RemoveAll(同 Id)` → 副本补齐 → bot 取 `options[0]`）＋ 新增一个 `BattleContext` 槽把同一份 options 给面板 ＋ 整批重钉 `play check`；改点 = 本文件 ＋ `BattleContext.cs` ＋ `BattleDriver.cs` ⇒ **跨文件**。据 `MatchData__GetRandomInt.c`（`index = counter % len; counter++`）「抽几下」是有意义的 ⇒ 我们少消耗 `N-1` 格是**真差异**（选中的那张相同、之后的随机整体错位）。',
    note_append='｜' + TAG + ' 未改（跨文件，需无写手窗口 + 重钉基线）。')

UPD[('BattleManager', 'ChoiceOfCardPlayer(CardScript actingCard, CardScript targetCard, int actionValue)')] = dict(
    status='B',
    diff_append='｜' + TAG + ' 现读复核 **未改**：残余两条 —— ① 面板上限走手抄表 `BattleDriver.ChooseShowByCardId`（21 张）而非卡数据 `ChoicesMaxTargets`（`CardDef.cs:585`，逐 id 一致）；② 引擎流一次不抽（选项集由 `ctx.ShowRng` 生成）。①在 `BattleDriver.cs`、②与 254 同一张施工单 ⇒ **跨文件**。',
    note_append='｜' + TAG + ' 未改（同 254 的施工单 + `BattleDriver.cs`）。')

UPD[('BattleManager', 'IsValidSpellTarget(CardScript spell, CardScript target)')] = dict(
    status='A',
    diff_append='｜' + TAG + ' **判 A**（现读逐档核）：10 档全部在结算口覆盖 —— ①非空 / ②`spell` 非空 ③行动方 ⑥`targeted` 由 `IsLegalPlayTarget`（本文件 `:2213-2220`，判据 `.c:45-46`）管；②`IsInPlay` 由 `PlayTargetPool` 的取数源管；⑦潜行(70) / ⑧`warded` / ⑨`camouflaged` 在 `AddSide` 的 `:3008` 一带；⑩`targetCriteria` 由 `EffectText.AssignCardTargetFallback` → `op.TargetCondition` → `ApplyStructuredConditionFilter` 管；③教学局闸在 `DoPlay`。表现层拖拽高亮走 `IsLegalPick` → **同一个 `IsLegalPlayTarget`**（判据只此一处）。',
    note_append='｜' + TAG + ' **A**：无新增缺口；「点亮阶段就问」那半由 `IsLegalPick` 同一口覆盖。')

UPD[('BattleManager', 'ResolveCreateHandCard(BattleAction battleAction)')] = dict(
    status='B',
    diff_append='｜' + TAG + ' 现读补数 **未改**：① `BroadcastCardCreated(210)` 的结构件仍缺且**跨文件** —— 原版 `BattleManager__BroadcastCardCreated.c:14-56` = Ⅰ 场上每张 `IsInPlay` 的牌 Ⅱ `GetCardsInHandList(1,1,1,1,1,1,0)` **一份表覆盖双方手牌**，逐个 `ReactToCardCreated`（`CardScript__ReactToCardCreated.c:4` = `OnTrigger(0xd2 = 210, thisCard, actingCard = 被造那张, target = null)`）；我们缺 `WhenEventKind` 常量（`WhenEvent.cs`，白名单外）＋ `TriggerNameOfWhenKind` 映射 ＋ `DoCreate` hand 支发射。② 满手 heads-up 是表现层。④ carried effect 重放（主路径该槽恒 0）。🔴 可达性：**池内 `abilityTriggers` 含 210 的卡 = 0 张**（`cards_engine.json` 1,129 张现扫）⇒ 接上**零可观测**。',
    note_append='｜' + TAG + ' 未改（`WhenEvent.cs` 白名单外）；池内 210 = 0 张。')

UPD[('BattleManager', 'ResolveFollowAfterResolution(BattleAction battleAction)')] = dict(
    status='B',
    diff_append='｜' + TAG + ' 现读复核 **未改**：②「第二实参 `targetCard`」仍是 `EmitFollowUpResolution` 的签名缺口，但**单独加形参 = 死代码** —— 要先生成「按 trigger 410 跑正文」的执行口（`RuleCore.FireTriggerAt` / 队列体，跨文件）；`EffectResolver.cs:7389` 明写那一跳只发表现层事件、不结算。③ 由泵收尾属我们接受的设计差异。',
    note_append='｜' + TAG + ' 未改（跨 `RuleCore`）。')

UPD[('BattleManager', 'ResolveJamUnit(BattleAction battleAction)')] = dict(
    status='B',
    diff_append='｜🔴' + TAG + ' **① 已改（本代理）**：`ResolveJamUnit` 的 ③「剔别人」与 ④「致死检查」**整块对调** —— 现在顺序 = `ApplyJam()` → `CheckIfDead` → 剔别人 → 广播 → 撤排队 → 光环重算，与原版 `CardScript__ReactToUnitJammed.c:178`（`CheckIfDead` 在函数体内）＋ `BattleManager__ResolveJamUnit.c`（`BroadcastUnitJammed` 在其后）一致；**零行数变化**（两块 17 / 21 行互换，文件仍 19,065 行、下游 A 行落点零漂移，机检 A 998 通过 / 漂移 0）。② 由 B94_B14 补（`CardInstance` 两本账）。③ 只剩「`UpdateWhileInPlay(false)` 用覆盖等价顶」这一条 **judgment call**：`Auras.RefreshInPlayNumericAndKeywords` 与它语义等价但**不是同一条调用**（该窄口在 `BL18`/`UM18` 上仍有 +4 event battery）⇒ ⛔ 本代理**不据此升 A**，留给审查裁定；另 `CancelEffectsFromDisappearingUnit` 的 `ActionType` 豁免那半记在账 L490。',
    note_append='｜' + TAG + ' **① 顺序已按原版改回**（零行数变化，`:16093-16136` 区内无 A 行落点）；②/③ 见差异列。**保持 B**（③ 是等价论证、非同一条调用，不自行升档）。池内 jam 生产者 0 张 ⇒ 零可观测。')

UPD[('BattleManager', 'ResolveTransformFromRemnant(BattleAction battleAction)')] = dict(
    status='B',
    diff_append='｜' + TAG + ' 现读复核 **未改**：① 已由 B94_B14 改（`Reanimated` 上移）。③「`cardState` 回 2 早一拍」被**既有断言**挡住 —— 挪到 `ReanimateRemnantInPlace` 之后会打红 `Editor/RuleEngineTest_W12.cs:473-483`（它把 `CardStateProbe` 放在 `Emit(Deploy)` 之后），该文件归调度台。④ 我们多的 `EvtKind.Deploy` / `Tally.AddReanimated` / `PlayOrder = TakePlayOrder()` 在 `DoReanimate`（本文件 `:15513`）里可改，但改 `PlayOrder` 会改监听者先后序 ⇒ 必须配断言，而本件白名单**不含任何 `Editor/*.cs`** ⇒ 改出来无法验证。',
    note_append='｜' + TAG + ' 未改：③ 被 `RuleEngineTest_W12.cs:473-483` 挡；④ 需断言而白名单不含 `Editor/`。')

UPD[('BattleManagerSupport', 'BroadcastUnitDamaged(BattleManager manager, CardScript damagedUnit, DamageInfo damageInfo)')] = dict(
    status='C',
    diff_append='｜' + TAG + ' 现读补数 **未改**：原版 `BroadcastUnitDamaged.c:19` 预调 → `:20-61` 棋盘循环（`IsInPlay ∧ != 被打者`，**外加** `damageType == 4(Attack) ∧ damageInfo.actingCard == 这张` 时**不看 `IsInPlay`**）→ `:62-70` 当前方手牌（不过滤）→ `:71-86` 对方手牌；每一跳都是 `ResolveDamageDealt(thisCard, 被打者, info)`，其体内按 `damageType ∈ {0,3} ⇒ 185` / `{4,6} ⇒ 187`（`:191`）。🔴 **池内 `abilityTriggers` 含 185 / 187 的卡 = 0 张**（`cards_engine.json` 1,129 张现扫）⇒「棋盘侧 185/187 未建」**零可观测**；「外加那一支」要 `RuleCore.EmitHit` 把 `damageType` ＋ 施害卡一起带进广播（`EmitHit` 属 `RuleCore`）。',
    note_append='｜' + TAG + ' 未改（跨 `RuleCore.EmitHit`）；池内 185/187 各 0 张。')

UPD[('CardScript', 'ReactToUnitJammed(CardScript disenchantedCard)')] = dict(
    status='B',
    diff_append='｜' + TAG + ' 现读复核 **未改**：① 覆盖等价（见账 521）。② `RemoveExtrinsicEffectsFrom` 的 `RecordGrant` 不记施放者 —— 落点在 `UnitState`（= B-6，B24 已建 `OwnerSeat`，那一列仍未加）。③ 自造 `UnitJammed` 已按铁律 18 标 🔶 且 `TriggerNameOfWhenKind` 对它返 `null`。池内 jam 生产者 0 张。',
    note_append='｜' + TAG + ' 未改（② 落点 `UnitState`）。')

UPD[('CardScript', 'RemoveEffect(CardEffect removedEffect, bool fromJam)')] = dict(
    status='C',
    diff_append='｜' + TAG + ' 现读复核 **未改**：`RemoveEffectRecordFrom`（本文件 `:18089`）与 `UnitState.RemoveEffectRecord` 仍 **0 调用点**；四个缺口的落点**全不在本文件** —— case 3/90「多摘」与 case 5「残留攻/血」在 `UnitState`、case c/f/10 的附魔摘除在 `Enchantment`、`BroadcastQuestResolved + questCounter=0` 在 `RuleCorePartial_B94B8` ⇒ 本件只能改壳。',
    note_append='｜' + TAG + ' 未改（落点全在 `UnitState` / `Enchantment` / `RuleCore`）。')

UPD[('MinionManager', 'GetNextSlotToCard(CardScript card)')] = dict(
    status='B',
    diff_append='｜' + TAG + ' 现读复核 ＋ **就地订正文档（铁律 5）**：`EffectResolverPartial_B88E2.cs:244-252` 的「`cards_truth.json` 现读 `0 self` 仍是 0 处」与「本函数今天仍没有调用点」两句**已作废** —— 错因是 `cards_truth.json` 为**稀疏导出**（`defaults.omitted` 明写 `0 / 0.0 / "" / false / [] / null` 一律省略）⇒ 按「有没有这个键」数 0 值必然数出 0；真值见 `报告/B94_Z3.md` §3·3（`createAtPos = 0` **73 行**，其中 **12 行属场上单位卡**）。接线确认 = `EffectResolver.cs:4001` 的 `DeployFree(..., requestSlot: SummonRequestSlotFor(...))`；取数口 = 同文件类尾 `SummonRequestSlotFor`。残余仍开：① `AddMinionOccupation(area,N)` 我们无对应物（棋盘 = 连续无洞数组 ＋ `BoardSlots.Insert` 插推）；② `op.AbilityIndex/LogicIndex` 在某些 op 上恒 `-1`。',
    note_append='｜' + TAG + ' 接线已在（`:4001`）；文档已按 §3·3 订正（**零行数变化**，A 行落点 `:61/:114/:158/:197/:320/...` 全未漂）。')

UPD[('RawCardScript', 'OnTriggerEffectInCard(AbilityTrigger trigger, BattleManager manager, CardScript thisCard, CardScript actingCard, CardScript targetCard)')] = dict(
    status='B',
    diff_append='｜' + TAG + ' 现读复核 **未改**：残余①（`actingCard` / `targetCard` 并进同一个 `seed`）**已由 B94_B22 解决** —— `ResolveOps` 调用点外套了 `EventSlotsScope.TargetCardAndActing(ctx, targetCard, abilityCard, actingCard)`，`actingCard` 走独立槽 `ctx.EventActingCard`。残余② `isEnchantment=1` 的语义**仍未定**：现库 `Enchantments.RunGrantedEnvAbility` 只在 `spellType == 200` 时生效，与「跑本卡身上附魔的能力」不是同一件事 ⇒ ⛔ 不发明口径。',
    note_append='｜' + TAG + ' 未改（② 语义未定；① 已由 B94_B22 闭）。')

UPD[('RawCardScript', 'UpdateTriggerOnce(BattleManager manager, CardScript thisCard)')] = dict(
    status='C',
    diff_append='｜' + TAG + ' 现读补数 **未改**：存在性闸已对（`AbilityTriggerPresence("TriggerOnceIfBoardCriteria") == 1` 走**数值**表）。🔴 **池内 `abilityTriggers` 含 200 的卡 = 0 张**（`cards_engine.json` 1,129 张现扫）⇒ 与「数据侧无生产者」一致。仍缺两件，**都跨文件**：① `CardDef` / 生成器补「trigger 数值 → ability 的 op」表（`_triggerOps` 只按**卡面关键词**建键，全池 0 张卡面写 `triggeronceifboardcriteria` ⇒ `TriggerOps` 恒 null）；② 调用方持「触发过后置 false」的 once 标志（`RuleCore.UpdateWhileInPlay` 的等价物）。',
    note_append='｜' + TAG + ' 未改（跨 `CardDef`/生成器 ＋ `RuleCore`）；池内 200 = 0 张。')

out_lines = [lines[0]]
report = []
for (cls, meth), upd in UPD.items():
    idx, c = find(cls, meth)
    c = list(c)
    old_status = c[5].strip()
    c[5] = upd['status']
    c[7] = c[7] + upd['diff_append']
    c[10] = c[10] + upd['note_append']
    if 'B95_WRE' not in c[9]:
        c[9] = c[9] + '｜B95_WRE'
    for x in c:
        assert '\t' not in x and '\n' not in x
    assert len(c) == 11
    out_lines.append('\t'.join(c))
    report.append((idx, cls, meth.split('(')[0], old_status, upd['status']))

dst = 'd:/4/Unity/资料/完全复刻/账/_parts/B95_WRE.tsv'
data = '\n'.join(out_lines) + '\n'
for ln in out_lines:
    assert len(ln.split('\t')) == 11, ln[:60]
io.open(dst, 'wb').write(data.encode('utf-8'))
print('rows written:', len(out_lines) - 1, '->', dst)
for r in sorted(report):
    print(r)
