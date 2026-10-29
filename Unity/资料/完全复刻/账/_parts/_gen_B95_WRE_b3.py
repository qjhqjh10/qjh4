# -*- coding: utf-8 -*-
"""批 3：在 `账/_parts/B95_WRE.tsv` 上追加本批（Rally 次序重排 ＋ 残骸 DeployedTurn）。用完可删。"""
import io

LEDGER = 'd:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv'
PATCH = 'd:/4/Unity/资料/完全复刻/账/_parts/B95_WRE.tsv'

main = io.open(LEDGER, 'rb').read().decode('utf-8').split('\n')
hdr = main[0]
assert len(hdr.split('\t')) == 11

patch = io.open(PATCH, 'rb').read().decode('utf-8').split('\n')
assert patch[0] == hdr
rows, order = {}, []
for l in patch[1:]:
    if not l.strip():
        continue
    c = l.split('\t')
    assert len(c) == 11, l[:60]
    rows[(c[1], c[2])] = c
    order.append((c[1], c[2]))
assert len(order) == 14, len(order)


def main_row(cls, meth):
    hits = [l.split('\t') for l in main[1:]
            if len(l.split('\t')) >= 11 and l.split('\t')[1] == cls and l.split('\t')[2] == meth]
    assert len(hits) == 1, (cls, meth, len(hits))
    return list(hits[0])


TAG = '[B95_WRE 批3 2026-10-29]'
ORDER_SRC = '判据 `判据/B95_Rally闸_核查.md` §八·1（`_ResolvePlayCardFromHand_d__447__MoveNext.c` 逐拍）／§八·5 方案 (a)'

# ---- 869：590 Combo 的次序 ----
k = ('CardScript', 'ResolveCardPlayed(CardScript cardPlayed, CardScript targetCard)')
c = rows.get(k) or main_row(*k)
c[7] += ('｜🔴' + TAG + ' **次序已按原版改**（' + ORDER_SRC + '）：原版拍序 = `Rally(0)`(:1081 → `OnCardPlayedWithTarget.c:43`) → **600**（同函数 `:66`）'
         '→ **590 Combo**（`ResolveCardPlayed.c:25-31`）→ `195`(`ResolveUnitSummoned.c:47`) → `605`(`:51`) → 广播轮 `190/550/680`(`:1426`)。'
         '我们原来 **五处全反**（Rally/600 排在最后、190/550/680 排在最前）。本批把 `RuleCore.PlayCardCore` 的三句搬到 `:4104` 那一行前半（Rally → 600 → 590），'
         '把 190/550/680 三句下移到 `ResolveFaithAbility`(605) 那一行后半 ⇒ **590 现在紧跟 600、且在 195 之前**，与原版一致。'
         '⚠️ 三句的**函数本体一个字没动**（`FireTriggerOnBoard` / `ResolveSpiritAbility` / `NotePlayed` / `ApplyPlayedCardTailEffects`）—— 本批**只搬次序**。')
c[10] += '｜' + TAG + ' 次序已改（590 紧跟 600、在 195 之前）；零行数变化。'
rows[k] = c
if k not in order:
    order.append(k)

# ---- 939：残骸 DeployedTurn ----
k = ('CardScript', 'TransformIntoRemnant()')
c = rows.get(k) or main_row(*k)
c[7] += ('｜🔴' + TAG + ' **就地订正「（等价）」那一句**：原版残骸是**同一个 `CardScript` 对象**（`TransformIntoRemnant` 只改状态位），'
         '`turnPlayed(+0x30c)` **原样保留**，`ResetToValuesInHand.c` 也不碰它；而我们**新造** `UnitState` ⇒ 初始化器原来**没搬 `DeployedTurn`** '
         '⇒ 残骸的 `DeployedTurn` 回落 `-1`。**后果（真偏离）**：「本回合上场 → 变残骸 → 复活回单位」这一族，原版 `IsTheSameTurnPlayed` 为真、我们为假 '
         '⇒ 誓约能力被 `RuleCore.cs:11752` 的 `u.DeployedTurn != ctx.Turn` 挡掉。'
         '**已修**：`RuleCore.cs:9871` 的初始化器补 `DeployedTurn = u.DeployedTurn,`（**只搬不清**；`UnitState.DeployedTurn` 的默认 `-1` 没动）。'
         '⚠️ 独立于「次序重排」，**单独归因**。'
         '🔴 全仓 `grep "IsRemnant = true"` 的另一处（`RuleCore.cs:9794`）是**进墓地的那条记录**（`DeadUnit`，不是 `UnitState`）⇒ **不需要**这一项。')
c[10] += '｜' + TAG + ' 已修 `DeployedTurn` 未搬（零行数变化，行内追加）；全仓只有 1 处 `new UnitState{IsRemnant=true}`。'
rows[k] = c
if k not in order:
    order.append(k)

# ---- 1264：605 / 195 / 600 的相对次序 ----
k = ('RawCardScript', 'OnCardPlayedWithTarget(BattleManager manager, CardScript thisCard, CardScript cardPlayed, CardScript targetCard)')
c = rows.get(k) or main_row(*k)
c[7] += ('｜🔴' + TAG + ' 「七拍各有落点」**成立，但相对次序原来五处全反**（' + ORDER_SRC + '）：600 原来排在 680 之后、Rally 之前（原版**紧跟 trigger 0**）；'
         '195 与 605 之间夹了 190/550/680/600（原版**紧邻**）；190/550/680 原来排在 Rally 之前（原版在**最后的广播轮**）。'
         '本批已按 `:1081 → :1085 → :1090 → :1426` 的拍序重排 `RuleCore.PlayCardCore`（见账 869）。'
         '⚠️ **残余（如实标）**：195 与 605 之间现在仍夹着 `SpawnTideCopies`(潮涌) 与 `PlayCompanions`(伴生) 两句 —— 它们**不在原版那张 8 拍表里**，'
         '与 605 的先后**无据可查**（这一次没动）。')
c[10] += '｜' + TAG + ' 拍序已改；残余 = Tide/Companion 两句夹在 195 与 605 之间（原版表里没有它们）。'
rows[k] = c
if k not in order:
    order.append(k)

out = [hdr]
for k in order:
    c = rows[k]
    for x in c:
        assert '\t' not in x and '\n' not in x
    assert len(c) == 11, k
    out.append('\t'.join(c))
io.open(PATCH, 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))
print('rows:', len(out) - 1)
for k in order[14:]:
    print('  added:', k[0], '|', k[1][:50], '=>', rows[k][5])
