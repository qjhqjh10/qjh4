# -*- coding: utf-8 -*-
"""B88_E6 第二批：把第一批补丁里的 `特效动画:198` 摘出来（避免与 B88_E6b 撞键），
并生成 `账/_parts/B88_E6b.tsv`（198 新判 + 排位三窗关窗入口 8 行）。
只写 _parts/，⛔ 不碰主表。"""
import io, os

LEDGER = 'd:/4/Unity/资料/完全复刻/账'
P1 = LEDGER + '/_parts/B88_E6.tsv'
P2 = LEDGER + '/_parts/B88_E6b.tsv'
HEADER = '域\t原版类\t方法\t判据出处\t判据要点\t状态\t我们的落点\t差异\t等级\t轮次\t备注'

# ---------- 1) 把 198 从第一批补丁里摘掉 ----------
b1 = io.open(P1, 'rb').read().decode('utf-8')
ls1 = b1.split('\n')
assert ls1[0] == HEADER
keep = [ls1[0]]
for l in ls1[1:]:
    if not l.strip():
        continue
    c = l.split('\t')
    assert len(c) == 11, len(c)
    if c[2].startswith('EnableEffects('):
        print('drop from B88_E6:', c[2])
        continue
    keep.append(l)
io.open(P1, 'wb').write(('\n'.join(keep) + '\n').encode('utf-8'))
print('B88_E6 rows now', len(keep) - 1)

# ---------- 2) 198（第一批的反证 → 第二批已落地 ①，② 判 D，③ 数据侧转 E4） ----------
src = io.open(os.path.join(LEDGER, '特效动画.tsv'), 'rb').read().decode('utf-8').split('\n')
row198 = src[197].split('\t')
assert len(row198) == 11 and row198[2].startswith('EnableEffects('), row198[2]

row198[5] = 'B'
row198[6] = ('CardPresentation/Battle/CardDisplayWindow.cs:915（SetEffectRows）'
             '（等价实现 = ① `SetEffectRows` 逐格 `on = i < n && !IsEmptyRow(rows[i])` + `NonEmptyRowsOf` 算非空条数；'
             '② 空文本的来源 = `AttrText` 数值 0 回空串（`:903`）；③ `RowsOf` 只在 `what == null` 时才回退成 `Name`（`:870`））')
row198[7] = ('**① 已复刻（B88_E6 2026-10-27）**：判据 `EnableCardEffectSlots.c:46-60`（`IsTextEmpty ⇒ Disable()`，'
             '而 **`cursor += 1` 两支都有** ⇒ 空行吃掉一个槽号、自己不画、后面的行**不往前补** = 空洞）。'
             '落地三处：`AttrText` 数值 0 改回**空串**（原版 `GameStaticData.GetEffectDesc` 同类字段全 0 直接 `return DAT_1842b80e0`）；'
             '`RowsOf` 的兜底从 `string.IsNullOrEmpty` 收紧成 `== null`（**空串不回退**，⛔ 别让它变回属性名）；'
             '`SetEffectRows` 把「吃的槽数」与「**非空**条数」分开（`EffectRowCount` 现在 = 非空条数 = 原版 `EnableEffects` 的返回值，'
             '整块根的显隐也用它 = `DisplayCardEffects.c:127-131` 那一跳）。'
             '**② 判 D**：原版收尾 `root.SetActive(!card.IsEnviromentaleffect())`（`CardDisplayEffects.c:118-135`）在本工程**不可达** —— '
             '唯一会把「环境效果卡」送进本窗的入口是 `BattleDriver.OpenOffensiveCardWindow`（`:6167` 走 `Show(d)` 那条重载 ⇒ `effects = null`），'
             '而 `effects == null` 时非空条数恒 0 ⇒ 那块本来就关着；⇒ 「非空 > 0」与「这张是环境效果卡」在本载体上**互斥**，'
             '建出来是**恒不可达的死代码**。⏳ 若将来给进攻卡补 effect 行，本判要重跑。'
             '**③ 仍缺（数据侧，已转 E4）**：进槽前倒序过滤 `buffType ∈ {6,0x23,0x1e}` 或 (`0x10` 且 `!HasQuests`)'
             '（`DisplayCardEffects.c:44-53`）—— 要 `TempBuff.buffType` + `HasQuests`，两者都没有。')
row198[9] = 'B88-E6b'
row198[10] = ('[B87-R24 的 ① 已闭 · B88_E6b 2026-10-27] ① 落地并**同刻改了断言**：`Editor/BattleScene.cs` 那条从'
              '「★ 数值 0 ⇒ 只印属性名」改成「★ 数值 0 ⇒ 「给了什么」= **空串**」（判据 `GetEffectDesc` 回空串 + '
              '`EnableCardEffectSlots.c:46-60` 占槽）；🧨 改回印属性名 / 把 `{0}` 原样印出 ⇒ 都红。'
              '② 判 D（理由+出处见差异列）。③ 数据侧已转 E4（`UnitState.TempBuff` 扩 `buffType`）。'
              '⚠️ **连带**：`特效动画:191/192/196/197`（A 行）正文里写的 `on = i < n` 现在多了 `&& !IsEmptyRow(...)` 一半 —— 语义未变、措辞待同步。')

rows = ['\t'.join(row198)]

# ---------- 3) 排位三窗的关窗入口（8 行） ----------
P_OLD = ('（等价实现 = `MenuDraw.ShadeHit(_dark, DarkR, QShade, QTitle, () => Close(), "BackgroundCloseButton")`：'
         '原版 `OnEnable` 把 `CloseButton` 挂到 `backgroundCloseButton.onClick` 上，而 `CloseButton()` 首句就是 `Close()`'
         ' ⇒ 点暗底 = 关窗）')
P_NEW = ('（等价实现 = `MenuDraw.ShadeHit(_dark, DarkR, QShade, QDivision, () => Close(), "BackgroundCloseButton")`：'
         '同上，原版两套系统（旧/`NewSystem`）**共用这一扇窗**、OnEnable 体逐字同形）')
P_SE = ('（等价实现 = `MenuDraw.ShadeHit(_dark, DarkR, QShade, QDivision, () => Close(), "BackgroundCloseButton")`：'
        '同族；`RankedEventSeasonEndedWindow` 与 `…Old` 两套 OnEnable 体逐字同形，共用这一扇窗）')

D_CB = ('**关窗入口已落地（B88_E6b 2026-10-27）**：原来全文件 `MenuDraw.Hit` / `ShadeHit` 命中 **0** ⇒ 开出来关不掉。'
        '判据 `{CLS}__OnEnable.c:15-22`（`backgroundCloseButton.onClick.AddListener(CloseButton)`）+ '
        '`{CLS}__CloseButton.c:9`（首句 = 虚表 `+0x1b8` = `Close()`）。'
        '🔴 **同刻订正了文件头那句「`BackgroundCloseButton.window = 0` ⇒ 点暗底不关窗」** —— 那条只说明**基类**的自动关窗被关掉，'
        '派生窗自己补了监听 ⇒ 实际行为是**点暗底会关窗**（铁律 5）。'
        '**残差**：`CloseButton()` 第二句 `ChatPreviewMessage.Enable(true)`（`…__CloseButton.c:10-14`）**未接** —— '
        '那是一条横切链（弹窗期间封禁聊天预览），本窗不是唯一宿主，同 `个人资料_成就_排行:219 SafetyInputEnabler`。⇒ 判 **B**。')

D_ON = ('**已落地（B88_E6b 2026-10-27）**：原版 `OnEnable` 体 = `backgroundCloseButton.onClick.AddListener(CloseButton)`'
        '（`{CLS}__OnEnable.c:15-22`；旧窗末尾还多一句 `AudioCue.Play2D(+0xb0, 3)` = `soundOnOpen`，'
        '我们已在 `PlayPromote` 贴近 promote clip 起播处播过，见 `Open()` 的注释）⇒ 我们的等价物 = '
        '`Build()` 里那句 `MenuDraw.ShadeHit(... () => Close())`（监听已挂上）。判 **A**。'
        '⚠️ 与 `CloseButton` 那行的差别只在**被调方的第二句**（`ChatPreviewMessage.Enable`）—— 那一半算在 `CloseButton` 账上，本行不重复计。')

N_CB = ('[B88_E6b 2026-10-27] 判 B 依据见差异列（关窗入口已通；`ChatPreviewMessage.Enable` 那半是横切链、另记）。'
        '**验收口**：开窗后点暗底 ⇒ 窗关（`RankedDivisionChangeWindow` 由 `Editor/BattleScene.cs` / '
        '`WindowsManager.OpenRankedDivisionChange` 自检开窗）。⚠️ `qContentMin` 传的是本窗**内容最低档**'
        '（`QTitle` 3411 / `QDivision` 3431），`MenuDraw.ShadeHit` 会现场核 `QShade < qContentMin`。')
N_ON = ('[B88_E6b 2026-10-27] 判 A：监听已挂（`MenuDraw.ShadeHit` 的 `() => Close()`）。'
        '⚠️ 旧窗那句 `soundOnOpen`（`+0xb0`, mixer 3 = Jingles）**已接**（`PlayPromote`，2026-10-22 补的），本条不新增。')

JOB = [
    (215, 'B', P_OLD, 'D_CB', N_CB),
    (217, 'A', P_OLD, 'D_ON', N_ON),
    (222, 'B', P_NEW, 'D_CB', N_CB),
    (227, 'A', P_NEW, 'D_ON', N_ON),
    (230, 'B', P_SE, 'D_CB', N_CB),
    (233, 'A', P_SE, 'D_ON', N_ON),
    (235, 'B', P_SE, 'D_CB', N_CB),
    (239, 'A', P_SE, 'D_ON', N_ON),
]

prof = io.open(os.path.join(LEDGER, '个人资料_成就_排行.tsv'), 'rb').read().decode('utf-8').split('\n')
for lineno, st, land, dtype, note in JOB:
    c = prof[lineno - 1].split('\t')
    assert len(c) == 11, (lineno, len(c))
    cls = c[1]
    c[5] = st
    c[6] = land
    c[7] = (D_CB if dtype == 'D_CB' else D_ON).replace('{CLS}', cls)
    c[9] = 'B88-E6b'
    c[10] = note.replace('{CLS}', cls)
    for x in c:
        assert '\n' not in x and '\r' not in x
    rows.append('\t'.join(c))

io.open(P2, 'wb').write((HEADER + '\n' + '\n'.join(rows) + '\n').encode('utf-8'))
print('wrote', P2, len(rows), 'rows')
for r in rows:
    c = r.split('\t')
    assert len(c) == 11
    assert '(' in c[2]
print('all rows 11 cols, 方法 带括号 OK')
