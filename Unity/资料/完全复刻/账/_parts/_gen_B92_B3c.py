# -*- coding: utf-8 -*-
"""生成 `账/_parts/B92_B3c.tsv` —— B92 波 4 · B3c。

读主表 → 只改要改的列 → 写出**整行 11 列**。键 = 主表原 `方法` 串（⛔ 一个新键都不许有）。
"""
import io

MAIN = r'd:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv'
OUT = r'd:/4/Unity/资料/完全复刻/账/_parts/B92_B3c.tsv'
ROUND = 'B92_B3c'
C_DOMAIN, C_CLASS, C_METHOD, C_SRC, C_POINT, C_STATE, C_WHERE, C_DIFF, C_LEVEL, C_ROUND, C_NOTE = range(11)

# key = 原版类 + '|' + 方法串
APPEND = {}
SET = {}


def ap(cls, method, state=None, point=None, where=None, diff=None, note=None):
    k = cls + '|' + method
    d = {}
    if state is not None:
        d[C_STATE] = state
    if point is not None:
        d[C_POINT] = point
    if where is not None:
        d[C_WHERE] = where
    if diff is not None:
        d[C_DIFF] = diff
    if note is not None:
        d[C_NOTE] = note
    APPEND[k] = d


# ---------------------------------------------------------------- ① 事件式
ap('MinionManager', 'AreMinionsWithAmbush()', state='A',
   where='｜✅【B92_B3c 2026-10-28 **已落事件式**】`BattleDriver` 类尾：`_manaHiddenByAmbush[2]`（每侧那个 bool）+ `NoteAmbushObserved`（唯一调用点 = 视图同步的 `v.SetAmbush(...)` 旁边，'
         '自己按 `InstanceId` + `FaceDown` 判「变没变」，变了才算一次事件）+ `NoteAmbushEntered` / `NoteAmbushExited` + `AreMinionsWithAmbush()`（扫**两侧**）+ `EnsureAmbushStateForBattle`（换局懒清零）；'
         '读口 = `AmbushHidesEnergy(side)` → `EnergyTextOf(side)`。',
   diff='｜【B92_B3c】**改前**：按**当前棋盘**每帧算（「还有面朝下的就继续藏」）；**改后 = 原版的事件式**：① **进伏击 ⇒ 无条件藏**（`CardScript__SetAmbush.c:56` 的 `ToggleCurrentMana(pm, false)`）'
        '② **出伏击 ⇒ 只有 `AreMinionsWithAmbush()` 为假才还原**（`:96-104`）⇒ 多张伏击时翻开第一张**不还原**、翻最后一张才还原。'
        '③ 另照抄原版那个**跨侧**形状：`AreMinionsWithAmbush()` 扫**两侧**，而 `ToggleCurrentMana` 只作用于那一侧 ⇒ «我方翻开、对面还有伏击» 时我方法力**继续藏**（18·0：原版就这样，⛔ 不「修好」）。'
        '④ 原版另外 7 个「有伏击 ⇒ 跳过本轮法力刷新」的闸（`PlayerManager__TurnStart:35` / `__RefillMana:28` / `__UseNormalEnergy:115` / `__AddNewMana` / `__RefillCustomMana` / `__RefillOneMana` / `__OnTurnEnd`）'
        '⇒ 我们「每帧从那个 bool 重算文字」**净等价**（伏击期间文字恒为 `?/{total}`），不再单建那 7 个闸。',
   note='｜【B92_B3c】判 A 依据：原版体 52 行逐句对过 + 落点真实存在（类尾 5 个成员 + 2 个读口）。旧账「查询本身没有消费者」已被 B5 §R15 推翻（**8 个活调用点**，全服务法力显示）。')

# ---------------------------------------------------------------- ② 三条现核无差距 ⇒ A
ap('BattleManager', 'DisplayBigChat(string chatMsgText, CardScript talkingUnit)', state='A',
   diff='｜✅【B92_B3c 2026-10-28 现核 → **判 A**】代码侧早就是等价实现（本笔未改这个文件）：`Battle/BattleBigChat.cs:115` 两道 guard **逐字**照 `decomp_full/BattleManager__DisplayBigChat.c`'
        '（空文案 ⇒ `LogWarning` 且不显示 / 立绘 null ⇒ `LogError` 且不显示），`:95` 的重载 A 取 `fullHeroSprite` 后**转发**给唯一实现 —— 与原版「重载 A 只做转发」同形。',
   note='｜【B92_B3c】升 A 依据：guard 逐句 + 转发形态 + 落点真实存在。**剩余不属缺口**：原版「显示调用本体」在 `decomp_full` 与 ILSpy **两棵树里都只剩 guard**（铁律 11 例外①）'
        '⇒ 显示目标是我们 `🔶 自己设计`（走 `UnitChatPanel.Speak(Radio)` 那颗框 + `fullHeroSprite` 桥），只能真机验收；教程数据里 `*ChatBig` **0 条**，今天走不到。')

ap('BattleManager', 'DisplayBigChat(string chatMsgText, Sprite talkingUnitSprite)', state='A',
   diff='｜✅【B92_B3c 2026-10-28 现核 → **判 A**】同 `DisplayBigChat(CardScript)` 那一行：两道 guard 逐字 + 唯一实现在 `Battle/BattleBigChat.cs:115`（本笔未改）；'
        '`VoiceLineData` 5 形参（chatMessage / sprite / audioClip / duration / cardType）旁证显示形态 = 「气泡 + 传入 sprite + 无音频 + 时长 + 卡类型」。',
   note='｜【B92_B3c】升 A 依据同上一行；剩余 = 原版显示体被剥（例外①）+ 真机验收。')

ap('BattleManager', 'ResolveAbilityIconAnim(BattleAction battleAction)', state='A',
   diff='｜✅【B92_B3c 2026-10-28 现核 → **判 A**】**时长口径已逐值对齐**（本笔未改 `AbilityIconAnim.cs`）：`DurationOf(trigger)` = Landing `LandingPreDelay 0.5 + HoldSeconds 0.2 = 0.7` / 其余 `0.2`，'
        '与 `BattleManager._ResolveAbilityIconAnim_d__475__MoveNext.c` 那两个 `yield`（`+0x4b0 = 0x1834b2bb4 = 0.5`、`+0x4c8 = 0x1834b2bb0 = 0.2`）逐值相同；`LandingTrigger = 440 (0x1b8)` 也对上；'
        '`Tick` 是一步式状态机（开场 / 播动画 / Finish）⇒ 三态与真体同构（协程 → 自有时钟，批处理无帧循环）。',
   note='｜【B92_B3c】升 A 依据：两跳时长逐值 + 三态同构 + 落点真实存在。**剩余不属缺口**：`BattleAction.waitForAnim`（`BattleAction.cs:49`）在**整棵 ILSpy 树零读者**'
        '（`:4378/:4538/:4600` 那三处是 `_ResolveAnimAnimationClip` / `_ResolveAnimInfoRangeAttack` 自己的同名字段）⇒ 只做读口是正解，⛔ 别再派人反复查。')

ap('BattleManager', 'MulliganFallbackCountdown()', state='A',
   diff='｜✅【B92_B3c 2026-10-28 现核 → **判 A**】实现不在本笔文件里（`CardPresentation/Net/NetBattle.cs`），只做判据侧逐条复核：'
        '`MulliganFallbackInterval = 4f`（`:1869`）与原版字面量 `0x1834b2df0 = 4.0`（`read_literal.py` 现读）**逐值吻合**；`MulliganFallbackAttempts = 15`（`:1871`）对应 `.c` 的 `0xf < counter`；'
        '`attempts > 15` 且 `!_peerMulliganSeen` ⇒ `AbortWithVictory(...)`（= 原版 `CancelMatchWithVictory("MulliganFallbackCountdown " + opponentId)` ⇒ **判本机胜**）；掉线期间由 `TickReconnectCountdown` 抢先 = 原版 `waitingWhileDisconnected` 不计数。',
   note='｜【B92_B3c】升 A 依据：三处常量/闸与 `_MulliganFallbackCountdown_d__348__MoveNext.c:1-163` 逐条对上。两处本地等价已如实标（不发 confirm 帧 / 重发自己的换牌）。')

# ---------------------------------------------------------------- ③ Get2DPosOfCard 第 3 个调用点
ap('BattleManager', 'Get2DPosOfCard(CardScript card, bool useCenter)',
   diff='｜【B92_B3c 2026-10-28】**第 3 个调用点已接**：AI 出手前的准星演出（原版 `BattleManager__EnemyTargetingAnim.c:18`）—— 起点原来用裸 `caster.transform.position`，'
        '现走 `Get2DPosOfCard(caster, useCenter: true)`（`.c` 第 4 实参恒 `1`）；**终点仍是裸 `transform.position`（`.c:22`）**，那一半本来就对。'
        '⇒ 剩 **4 个**调用点未接，且**三个都卡在同一条前置上**：`BattleManager.GetHeroPos(isPlayer)`（= `Get2DPosOfCard(该侧 heroCard, useCenter:1)`，5 行）我们**还没建** —— '
        '它是 `AiScripted.GetTipPosition`（教程提示位）与 `BattleManager.StartTargetedSpellAnim` 的共同前置；`AiScripted.ShowPlayerActionAnim` 的 `from` 那一档我们走的是 '
        '`TutorialPointerAnchorFor` → `TutorialAnchor(...)`（锚点 + 尺寸那套，2026-10-28 `B90_B2` 刚定案、带 36 条断言）⇒ 改它需另立项。',
   note='｜【B92_B3c】状态仍 A（A 尺子 ①）—— 上游未接的调用点登记在 B/C；`GetHeroPos` 的两个消费者 = `AiScripted.GetTipPosition` / `CardScript.MoveIntoFieldFromHand`（**都不在本文件**，调度台已认可「不建零调用者死方法」）。')

# ---------------------------------------------------------------- 生成
raw = io.open(MAIN, 'rb').read().decode('utf-8-sig')
lines = raw.replace('\r\n', '\n').split('\n')
header = lines[0].split('\t')
assert len(header) == 11, header

targets = {}
for i, ln in enumerate(lines[1:], 2):
    c = ln.split('\t')
    if len(c) != 11:
        continue
    k = c[C_CLASS] + '|' + c[C_METHOD]
    if k in APPEND:
        assert k not in targets, '重复键 %s' % k
        targets[k] = (i, c)

missing = [k for k in APPEND if k not in targets]
assert not missing, '主表里找不到（⛔ 不许新增键）: %r' % missing

out = []
for k, (lineno, c) in targets.items():
    cells = list(c)
    for col, txt in APPEND[k].items():
        assert '\t' not in txt and '\n' not in txt, k
        if col == C_STATE:
            assert txt in ('A', 'B', 'C', 'D')
            cells[col] = txt
        elif col == C_DIFF and txt.startswith('｜✅'):
            cells[col] = cells[col] + txt          # 追加（保留旧证据）
        else:
            cells[col] = cells[col] + txt
    if ROUND not in cells[C_ROUND]:
        cells[C_ROUND] = (cells[C_ROUND] + ' · ' + ROUND) if cells[C_ROUND].strip() else ROUND
    assert len(cells) == 11 and '(' in cells[C_METHOD]
    assert cells[C_STATE] in ('A', 'B', 'C', 'D')
    out.append((lineno, cells))
out.sort(key=lambda x: x[0])

body = '\t'.join(header) + '\r\n' + ''.join('\t'.join(c) + '\r\n' for _, c in out)
io.open(OUT, 'wb').write(b'\xef\xbb\xbf' + body.encode('utf-8'))
print('OK -> %s  rows=%d  bytes=%d' % (OUT, len(out), len(body) + 3))
for lineno, c in out:
    print('  main:%d  %-2s  %s' % (lineno, c[C_STATE], c[C_CLASS] + '.' + c[C_METHOD][:56]))
