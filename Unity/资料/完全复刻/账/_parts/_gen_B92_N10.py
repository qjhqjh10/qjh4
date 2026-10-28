# -*- coding: utf-8 -*-
# B92_N10 账补丁生成器（只改本代理动过状态/落点/差异/备注的行；未动的行不抄）
# 用法： python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B92_N10.py
import io, os

BASE = r'd:/4/Unity/资料/完全复刻/账'
HDR = '域\t原版类\t方法\t判据出处\t判据要点\t状态\t我们的落点\t差异\t等级\t轮次\t备注'


def load(fn):
    b = io.open(os.path.join(BASE, fn), 'rb').read()
    t = b.decode('utf-8-sig')
    sep = '\r\n' if b.count(b'\r\n') > 10 else '\n'
    return t.split(sep), sep


def find(rows, cls, meth_prefix):
    hits = []
    for L in rows:
        p = L.split('\t')
        if len(p) >= 11 and p[1] == cls and p[2].startswith(meth_prefix):
            hits.append(p)
    assert len(hits) == 1, 'expect exactly 1 row for %s.%s, got %d' % (cls, meth_prefix, len(hits))
    return hits[0]


out = []

# ── 1) 菜单_外壳 · AttackTypesButtonsController.ToggleAttackTypesSelector ──────────
rows, _ = load('菜单_外壳.tsv')
p = find(rows, 'AttackTypesButtonsController', 'ToggleAttackTypesSelector(')
p[5] = 'A'
p[6] = ('CardPresentation/Battle/BattleDriver.cs:15161（OpenCommand）· 角标值 = 同一文件的 `AbilityBadgeOf`'
        '（类尾，散文不给行号）；开/收与图标 = `Battle/AttackSelector.cs` 的 `Show` / `Hide` / `ApplyOptionIcons`（不带行号）')
p[7] = p[7] + (
    ' ｜ 🆕 2026-10-28（B92_N10 · 已闭合）：角标值改由 `BattleDriver.AbilityBadgeOf` 按原版三跳算 —— '
    '`decomp_full/UnitOnBoardAttackTypeSelector__Toggle.c:74-95`（`bVar11` = 卡 `+0x220` `hasActiveAbility`；'
    '**有 oath 但 `CanUseAbilityNow` 为假 ⇒ 整段跳过、值留 0**）→ '
    '`CardTraitCollection__GetTraitForActiveAbilityRawCard.c:5-24`（纯映射，唯一正本 = `CardDef.ActiveAbilityTrait`）→ '
    '`RawCardScript__GetDefaultTraitValue.c:21-34`（扫卡 trait 表命中的 `_traitValue`，唯一正本 = `CardDef.GetDefaultTraitValue`）→ '
    '`AttackTypesButtonsController__ToggleAttackTypesSelector.c:109-135`（`0 < value` 才写 `ValueText` 并 `SetActive(true)`）。'
    '旧写法读 `u.Ability.Amount`（原版卡恒 null）与 `Card.OathCost`（数值常撞上、但来源不是它）。'
    '现读观测量（`cards_engine.json` 1,129 张）：`activeAbilityLogic` 非空 132 张里 trait 值 > 0 的 **18 张**'
    '（全 Ultramarines `oath` 系、值 1–4；比 B6 记的 17 多一张 `UM101 1st Company Terminator`），'
    '其余 114 张仍留空（`pray`/`duty`/`agenda`/`ferocity` 在卡表是裸词 ⇒ `Value = 0`，与原版 `_traitValue = 0` 同）。'
    '⚠️ **如实订正 B6 的一条**：那 18 张**今天已经在印同一个数字**（`OathCost` 与 oath trait 值同为 `Oath N` 的 N）'
    '⇒ 这一改是**换来源（与原版同源）**，常规局可见差异为 0；B6 记的「我们一张都没有」在本代理的静态核查里复现不出来'
    '（`HasOath` = `OathOps.Count > 0`，`CollectOathOps` 收 `Oath N:` ⇒ `oath` 为真）。'
    '⚠️ **未建模一格**：`duty`(1220) 那条 `bVar11 = false` 抑制支（卡 `+0x4c` 的语义本机没解出）—— 今天不可观测'
    '（池内 duty 系 trait 值恒 0）。')
p[10] = p[10] + (
    ' ｜ 【B92_N10 2026-10-28】B→A：角标值改由 `AbilityBadgeOf` 按 `.c` 三跳算（新成员追加在 `BattleDriver.cs` 类尾，零行号漂移）；'
    '落点收成「一个方法 + 一个文件:行号」。')
out.append(p)

# ── 2) 特效动画 · CardEffectItem.LoadEffect ─────────────────────────────────────
rows, _ = load('特效动画.tsv')
p = find(rows, 'CardEffectItem', 'LoadEffect(')
p[5] = 'B'
p[6] = ('CardPresentation/Battle/CardDisplayWindow.cs:871（RowsOf）· 槽位摆法 = 同一文件的 `SetEffectRows`（散文不给行号）；'
        '数据面 = `RuleEngine/Core/UnitStateEffectDisplay.cs` 的 `TempBuff.DisplaySourceName`/`BuffType`/`IsEffectListEntry`（不带行号）')
p[7] = p[7] + (
    ' ｜ 🆕 2026-10-28（B92_N10 · ③ 已闭合、① 订正、② 仍缺）：'
    '③「谁给的」已改用 `TempBuff.DisplaySourceName`（`EffectName` 优先、否则施加卡名；判据 `CardEffectItem__LoadEffect.c:110-140`）。'
    '① **就地订正**：`LoadEffect.c:42-52` 那三道闸是**真文案支的入口**'
    '（`(secret & isPlayer) == 0 && (quest == 0 || isPlayer != 0) && (isPlayer & quest) == 0`）'
    '—— 两掩码恒 false 时**本来就落真文案支**，所以这一格今天**无需改动**（原差异把方向写反成「⇒ 走隐藏支」）；'
    '真正缺的是**生产者**（`EffectResolver` 施加那一刻带下两掩码，不在本代理白名单）。'
    '② `buffType` 分派（`8`/`0x11` 双行合成、`+0x78` 四个专名、`+0x40 > 1` 追加数值，`LoadEffect.c:53-140`）**仍未做** ——'
    '数据面 `TempBuff.BuffType`/`TraitValue` 已备，缺的是 `RowsOf` 的 `what` 那一半按 buffType 分派。⇒ 保持 B。')
p[10] = p[10] + ' ｜ 【B92_N10 2026-10-28】③ 已接（`DisplaySourceName`）；① 方向订正 + 缺生产者；② buffType 分派仍未做。'
out.append(p)

# ── 3) 特效动画 · CardEffectsService.EnableEffects ───────────────────────────────
rows, _ = load('特效动画.tsv')
p = find(rows, 'CardEffectsService', 'EnableEffects(')
p[5] = 'A'
p[6] = ('CardPresentation/Battle/CardDisplayWindow.cs:851（RowsOf）· 槽位/计数 = 同一文件的 `SetEffectRows`（散文不给行号）；'
        '过滤规则唯一正本 = `UnitState.IsEffectListEntry`（不带行号）')
p[7] = p[7] + (
    ' ｜ 🆕 2026-10-28（B92_N10 · ③ 已闭合 ⇒ A）：进槽前过滤已接 —— `RowsOf` 逐条问 '
    '`UnitState.IsEffectListEntry(b.BuffType, hasQuests)`，`hasQuests` 由唯一调用点 `BattleDriver.ToggleUnitCard` '
    '传 `Enchantments.CountQuests(u.Instance) > 0`（= 原版 `EntityScript.HasQuests(载体)`；判据 '
    '`CardDisplayWindow__DisplayCardEffects.c:44-53`：`buffType ∈ {6 changeRace, 30 addRace, 35 daemonHost}` 或 '
    '`(16 choiceEnchantment 且 !HasQuests)` ⇒ `RemoveAt`）。① 早在此前闭合（`SetEffectRows` 把「吃的槽数」与「非空条数」分开、'
    '`AttrText` 数值 0 回空串）· ② 环境卡互斥支维持判 D（载体上「非空 > 0」与「环境效果卡」互斥）。⇒ 判 A。')
p[10] = p[10] + ' ｜ 【B92_N10 2026-10-28】③ 已接（`RowsOf` 调 `IsEffectListEntry` + 传 hasQuests）⇒ B→A。'
out.append(p)

# ── 4) 特效动画 · EnviromentalEffectCardsSO.GetEnvCardListDraft ────────────────────
rows, _ = load('特效动画.tsv')
p = find(rows, 'EnviromentalEffectCardsSO', 'GetEnvCardListDraft(')
p[5] = 'B'
p[6] = ('CardPresentation/Core/OffensiveCards.cs:182（ChoicesForDraft）· 进攻半边接线 = `BattleDriver.OffensiveChoicesOwnedCore`'
        '（散文不给行号）；防御半边见差异列')
p[7] = p[7] + (
    ' ｜ 🆕 2026-10-28（B92_N10 现核）：防御半边**不是「未接」而是「接不了」** —— `OffensiveCards.Card.name` 对防御槽'
    '**恒空串**（`Resources/OffensiveCards.json` / `数据/游戏数据/defensive_cards.json` 的 `_unresolved`，文件头已写明卡名解不出），'
    '唯一身份是 `cardRefPathID`（原版 PPtr），而我们的 `CardDef` 侧**没有任何 pathID 列** ⇒ '
    '`ChoicesForDraft(faction, offensive:false, …)` 的产出**无法映射回卡池 `CardDef`**（防御面板吃的是 `CardDef`）。'
    '⇒ ② 的正确处置是先补「防御槽 → 卡池卡」的映射（数据侧），⛔ 不是在 `BattleDriver` 里硬接；'
    '`defensiveCardPanelEnabled` 默认 false（原版 `ShouldUseEnviromentalEffects` 恒 false）⇒ 今天仍不可观测。'
    '③ 双端胜场 min 维持 🔶 自设计（调度台 2026-10-27 裁定）。')
p[10] = p[10] + ' ｜ 【B92_N10 2026-10-28】② 定性订正：防御半边缺的是「防御槽 → CardDef」的映射（数据侧），不是接线；落点收成单条。'
out.append(p)

# ── 5) 特效动画 · TutorialPointer.StartPointer ───────────────────────────────────
rows, _ = load('特效动画.tsv')
p = find(rows, 'TutorialPointer', 'StartPointer(')
p[5] = 'B'
p[6] = ('CardPresentation/Battle/TutorialOverlay.cs:1075（SetPointer）· 建层闸 = `BattleDriver.EnsureTutorialOverlay`（散文不给行号）')
p[7] = p[7] + (
    ' ｜ 🆕 2026-10-28（B92_N10 现核 · 判据补全、未实现）：三条协程体已读全 —— `ClickInPosition` / `ClickAndHold` 只把指针放 `from`；'
    '`TutorialPointer._ClickAndDrag_d__25__MoveNext.c:1-95` 四态循环：① `ShowStartingPointer(from, 朝向)` + `WaitForSeconds(+0x54)` '
    '② `ShowClickedPointer(from, 朝向)` + `WaitForSeconds(+0x58 × const)` ③ `DOMove(GetAdjustedPos(endPos, 朝向), +0x68)` ④ 回 ①。'
    '⇒ **双点口的真身 = 同一个指针沿 `from → to` 移动**（⛔ 不是画两个点），三个时长是 `TutorialPointer` 组件的序列化字段 '
    '`+0x54/+0x58/+0x68`（**尚未从 prefab 读出**）。未做两件：① 把 `SetPointer(bool, Vector3)` 扩成 `(on, from, to?, orientation)` + 四态循环'
    '（先要取那三个时长）② 非教程局也建 overlay（与 `485③` 同源，需 `Ctx.CampaignStage` 写点）。')
p[10] = p[10] + ' ｜ 【B92_N10 2026-10-28】判据补全（四态循环 + 三个时长字段位置）；仍未实现（缺 prefab 里那三个时长）。'
out.append(p)

# ── 写出（BOM + CRLF，裸 tab 分隔）──────────────────────────────────────────────
buf = [HDR]
for p in out:
    assert len(p) == 11, 'col count %d' % len(p)
    for c in p:
        assert '\t' not in c and '\n' not in c and '\r' not in c, 'bad cell'
    buf.append('\t'.join(p))
data = ('\r\n'.join(buf) + '\r\n').encode('utf-8')
path = os.path.join(BASE, '_parts', 'B92_N10.tsv')
io.open(path, 'wb').write(b'\xef\xbb\xbf' + data)
print('written', path, len(data), 'bytes;', len(out), 'rows')
