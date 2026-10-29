# -*- coding: utf-8 -*-
"""批 2：在 `账/_parts/B95_WRE.tsv` 上**追加/升级**本批（B95_WRE 批 2）改过的 4 行。
⛔ 不覆盖批 1 那 13 行（批 1 的文本原样保留，本脚本只在它后面追加）。用完可删。
"""
import io

LEDGER = 'd:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv'
PATCH = 'd:/4/Unity/资料/完全复刻/账/_parts/B95_WRE.tsv'

main = io.open(LEDGER, 'rb').read().decode('utf-8').split('\n')
hdr = main[0]
assert len(hdr.split('\t')) == 11

patch_lines = io.open(PATCH, 'rb').read().decode('utf-8').split('\n')
assert patch_lines[0] == hdr, '批 1 的表头与主表不一致'
rows = {}          # (类, 方法) -> list(11)
order = []
for l in patch_lines[1:]:
    if not l.strip():
        continue
    c = l.split('\t')
    assert len(c) == 11, l[:60]
    rows[(c[1], c[2])] = c
    order.append((c[1], c[2]))
assert len(order) == 13, len(order)


def main_row(cls, meth):
    hits = []
    for l in main[1:]:
        c = l.split('\t')
        if len(c) >= 11 and c[1] == cls and c[2] == meth:
            hits.append(c)
    assert len(hits) == 1, (cls, meth, len(hits))
    return list(hits[0])


TAG = '[B95_WRE 批2 2026-10-29]'

# ---- 647：外加那一支已实现 ----
k647 = ('BattleManagerSupport', 'BroadcastUnitDamaged(BattleManager manager, CardScript damagedUnit, DamageInfo damageInfo)')
c = rows[k647]
c[5] = 'B'
c[7] += ('｜🔴' + TAG + ' **「外加那一支」已实现**（`RuleCore` 那半 ＋ `EffectResolver` 这半）：'
         '① `RuleCore.EmitHit` 新增 `UnitState attacker = null`（= 原版 `DamageInfo.actingCard`），'
         '链路 `HurtCore:9063 → ApplyDamage(internal):17047 → ApplyDamageCore:8464 → EmitHit:9354 → '
         'BroadcastWhen(actingCard:)`，**全部行内追加、零行数变化**；缺省 `null` ⇒ 老调用点逐字不变。'
         '② `EffectResolver.BroadcastWhenCore` 的 `Damaged` 那半多一跳（行内追加在 `:10025`），'
         '新函数 `BroadcastUnitDamagedExtraAttacker`（**追加在类尾**）逐句照 `BattleManagerSupport__BroadcastUnitDamaged.c:31-45`：'
         '`damageType == 4 (Attack)` ∧ `actingCard != null` ∧ `actingCard != 被打者` ∧ '
         '（`OwnerOf < 0` ∨ 待死）⇒ 对**施害卡**再跑一次 `Damaged` 监听（`allowPendingDeath: true`）。'
         '③ 仍开着的只有「棋盘侧按**数值 abilityTriggers** 发 60/130/185/187」那一整族（我们棋盘跳走的是**文本监听**）'
         '—— 池内 185/187 各 0 张 ⇒ 零可观测。')
c[10] += ('｜' + TAG + ' 外加那一支已接（`RuleCore` ＋ 本文件）；断言 = `Editor/RuleEngineTest_EngineB95WRE.cs` 的 '
          '`CheckB95WREAttackerPlumbing`（**接线归调度台**：`RuleEngineTest.cs` 的 `Run()` 加一行 `Step(CheckB95WREAttackerPlumbing);`）。'
          '⚠️ 顺带修了一处**编译级缺陷**：`RuleCore` 的 `internal ApplyDamage` 只有 7 个形参，而 `HurtCore:9063` 按 8 个调 ⇒ **CS1501、整个 RuleCore 编不过**（B95_WRC 上一批留下的），本批补齐 `int attackerSeat = -1`。')
rows[k647] = c

# ---- 521：③ 驳回（覆盖等价不是我的发明，是既有口径） ----
k521 = ('BattleManager', 'ResolveJamUnit(BattleAction battleAction)')
c = rows[k521]
c[7] += ('｜🔴' + TAG + ' **③ 不按「改真调用」落地（给反证）** —— 现读判据推翻了那个前提：'
         '`CardScript__UpdateWhileInPlay.c:19-30` = `RawCardScript.UpdateWhileInPlay(raw, mgr, this, param_2 ? this : null, 0)`；'
         '`RawCardScript__UpdateWhileInPlay.c:15,18` = **只** `TriggerAbilities(0x14 = 20 WhileInPlay)` ＋ `TriggerAbilities(0xf0 = 240 WhileInPlayVariable)` 两趟。'
         '而「`UpdateWhileInPlay ×2 + BroadcastWhileInPlay` ⇒ `Auras.RefreshInPlayNumericAndKeywords`」**是本仓既有的、已复核的唯一口径**：'
         '`ActionPump.cs:143`（账 669 · `B9_E7P06` 隔离读数）与 `EffectResolver.cs:17027` 都这么写。'
         '⇒ 改成 `FireTriggerAt(WhileInPlay)` 会**重跑 while-in-play 的能力正文**（没有去重 ⇒ 会叠 buff），'
         '而且 `FireTriggerAt` **没有**「这张卡算不在场」那个形参（原版靠 `param_4 = null` 表达）。'
         '**故本批不动这一格**；若审查仍要逐字对齐，前置是给 `FireTriggerAt` 加「按 `UpdateWhileInPlay(bool)` 语义跑、且带去重」的窄口 —— 那是新机制，得先立判据。')
c[10] += ('｜' + TAG + ' ③ **驳回改真调用**（反证见差异列：既有口径就是 `Auras.RefreshInPlayNumericAndKeywords`；改成 `FireTriggerAt` 会叠 buff 且缺「不在场」形参）。'
          '`RuleCore.FireTriggerAt` 也**没有**改成 `internal`（`FireTriggerAtForTest` 早就是 `public`，另开一个导出等于死代码）。')
rows[k521] = c

# ---- 517：施工单（不加形参） ----
k517 = ('BattleManager', 'ResolveFollowAfterResolution(BattleAction battleAction)')
c = rows[k517]
c[7] += ('｜🔴' + TAG + ' **② 按「只加形参 = 死代码」处置，未加形参**；施工单见 `报告/B95_WRE_批2.md` §3（三步：'
         '① `RuleCore` 开一个「按 **trigger 数值 410** 取这条 ability 的 op 并结算」的执行口（现库只有按**卡面关键词**取正文的 `FireTriggerAt`）；'
         '② 该口要能带**两个卡槽**（`actingCard` = 干了这件事那张、`targetCard` = 宾语那张），并把它们各自种进 `ctx.EventActingCard` / `ctx.EventTarget`；'
         '③ 再把 `EffectResolver.EmitFollowUpResolution` 的 `targetCard` 透传进去）。')
c[10] += '｜' + TAG + ' ② 未加形参（死代码）；施工单已落报告。'
rows[k517] = c

# ---- 658：已核，不一致（新行，不在批 1 的 13 行里） ----
k658 = ('BattleManagerSupport', 'BroadcastUnitPoisoned(BattleManager manager, CardScript poisonedUnit, CardScript actingUnit)')
c = main_row(*k658)
c[7] += ('｜🔴' + TAG + ' **已逐句核，结论 = 不一致（但今天零可观测、且改点不在本件）**：'
         '原版触发闸 = `BattleManager__ResolveAddEffect.c` 里 `*(int *)(cardEffect + 0x3c) == 200` 之后调 '
         '`BattleManagerSupport__BroadcastUnitPoisoned(bm, local_f8 = 被打那个, *(cardEffect + 0x18) = **效果记录自己的 actingCard**)`，'
         '**位置 = 效果落账之后**；我们 = `RuleCore.cs:16143-16144` 的 `victim.ApplyTraitEffect(KeywordTable.Poisoned, 1, stackType:-1, extrinsic:false)` '
         '**成功之后**才 `BroadcastUnitPoisoned(ctx, victim, actingCard)`（`rec != null` 兜住「毒 × 抗性」那一档，与 `UnitState.cs:1131` 的拒收一致 —— **这一条对得上**）。'
         '三条差异：① 原版闸是 **`buffType == 200`**，而我们的 buff 表**从未建模 200**（`EffectsToShow.cs:52-60` 只登记 2/3/8/17；`UnitState.BuffTypeAddTrait = 3`）'
         '⇒ 我们这条是 **poisoned trait（buffType 3）** 的等价物，不是同一条闸；'
         '② 原版是**通用 `addEffect` 生产点**（任何 buffType 200 落账都广播），我们只有「`poisonous` 关键词命中」**一条**生产点；'
         '③ 原版「施害者」取自**效果记录自己的 `actingCard`**，我们取当场打人的那张。'
         '**要逐字一致**得先把 `BuffType 200` 建模（`UnitState` 的 buff 表 + `ApplyTraitEffect` 的 buffType 语义）⇒ 属 `UnitState`/数据线，不是本件一行能改的。')
c[10] += ('｜' + TAG + ' 已核 **不一致**（buffType 200 未建模；生产点窄一条）⇒ 未改代码；池内 `abilityTriggers 290 = 0`、'
          '全池提「poison」的只有 `GSC10 Biophagus`（造的是**陷阱卡 Poisoned Supplies**，不是毒 trait）⇒ 零可观测。')
if 'B95_WRE' not in c[9]:
    c[9] += '｜B95_WRE'
rows[k658] = c
order.append(k658)

out = [hdr]
for k in order:
    c = rows[k]
    for x in c:
        assert '\t' not in x and '\n' not in x
    assert len(c) == 11, k
    out.append('\t'.join(c))
data = '\n'.join(out) + '\n'
io.open(PATCH, 'wb').write(data.encode('utf-8'))
print('rows:', len(out) - 1)
for k in order[13:]:
    print('  new/updated:', k[0], '|', k[1][:40], '=>', rows[k][5])
print('  updated-in-place:', [k[1].split('(')[0] for k in order[:13] if rows[k][5] != 'B' or True][:0] or '647/521/517 已在 13 行内更新')
