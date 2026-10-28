# -*- coding: utf-8 -*-
"""生成 `账/_parts/B92_B3b.tsv` —— B92 波 4 · B3b（转岗写手）的账补丁。

做法同 `_gen_B92_B3.py`：读主表 → 只改要改的列 → 写出**整行 11 列**（表头逐字照抄）。
键 = 主表原 `方法` 串（⛔ 一个新键都不许有 —— 交回要求 `merge_ledger_parts.py --dry` 的「新增」为 0）。
"""
import io

MAIN = r'd:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv'
OUT = r'd:/4/Unity/资料/完全复刻/账/_parts/B92_B3b.tsv'
ROUND = 'B92_B3b'

APPEND = {}
SET = {}
C_DOMAIN, C_CLASS, C_METHOD, C_SRC, C_POINT, C_STATE, C_WHERE, C_DIFF, C_LEVEL, C_ROUND, C_NOTE = range(11)


def ap(method, where=None, diff=None, note=None):
    d = {}
    if where is not None:
        d[C_WHERE] = where
    if diff is not None:
        d[C_DIFF] = diff
    if note is not None:
        d[C_NOTE] = note
    APPEND[method] = d


ap('ApplyEnvEffect(RawCardScript card, bool isPlayer)',
   where='｜✅【B92_B3b 2026-10-28 **已接线**】`BattleDriver.ApplyOffensiveEnvOnce` 里在**换环境之前**调 `ApplyEnvWarlordAbilityOnce()`（类尾新增），'
         '后者调 `RuleCore.RunAbilityOfAnotherCard(ctx, envCard, "ThisCardPlayed", 先手方座位, actingCard: 督军, targetCard: null)` '
         '⇒ 原版 `.c:15` 那一跳上齐了（⚠️ 类名是 `RuleCore` —— `EffectResolver.cs` 只是**文件名**，本仓没有叫 `EffectResolver` 的类，见该文件 `:8348`）。',
   diff='｜【B92_B3b】**仍 C，而且原因换了**：不是「没接线」，是**卡池里没有环境卡** —— `Resources/cards_engine.json`（1,129 张 = 组牌池）'
        '现读 `Dawn Attack` / `Planetary Invasion` / `Manufactorum Overdrive` / `Warp Storm` / `Helfire Outburst` / `Daemonic Feast` / `Normal Conditions` **7/7 未命中** '
        '⇒ 这一跳**接上了但跑不动**（运行时出声 `"[Battle] 进攻卡「…」不在卡池…"`，⛔ 我们**没有**自己编一棵 ability 树顶上）。'
        '要它能跑 = 数据侧把环境卡并进 `cards_engine.json`（`工具/gen_cards_engine.py`），**代码这一侧零改动**；真值在 `数据/游戏数据/cards_truth.json`'
        '（`AM63 Dawn Attack` = `cardType 20` / `spellType 200`，`trigger 0` 那条 = `chosenAbility 30` + `buffType 12` + 一个 enchantment PPtr）。',
   note='｜【B92_B3b】已知偏离（出声不静默）：`.c` 的 `if (playerMgr != null && heroCard != null)` 把「督军触发 + 换环境」**一起**包着 ⇒ 督军取不到时原版**连环境都不换**；我们仍照旧换环境。')

ap('Get2DPosOfCard(CardScript card, bool useCenter)',
   diff='｜【B92_B3b 2026-10-28】**第 2 个调用点已接**：狂暴拦截的教程指针 `from`（`ShowPendingBerzerkPointer`）原来用裸 `v.transform.position`，'
        '现走 `Get2DPosOfCard(v, useCenter: true)` —— 判据 `BattleManager__EndTurnClick.c:95/:101` 两个实参**都是 1**。'
        '⇒ 剩 **5 个**调用点未接：`AiScripted.ShowPlayerActionAnim` 的 `from` · `AiScripted.GetTipPosition` 的提示位 · `BattleManager.EnemyTargetingAnim` · '
        '`BattleManager.StartTargetedSpellAnim` · `BattleManager.GetHeroPos`（后者是一条独立小方法，我们**还没建**）。'
        '⚠️ 已核：原版 **7 个调用点全部**传 `useCenter = 1`（`.c` 的第 4 实参恒 `1`）。',
   note='｜【B92_B3b】状态仍 A（A 尺子 ①）—— 上游那 5 个调用点登记在 B/C。')

ap('LeaveBattle()',
   note='｜🔴【B92_B3b 2026-10-28 **停手，没做**】③「教程前两局重开教程局」差三样，且**三样都不在本轮白名单文件里**：'
        '① **战斗教程**前两关的完成位 —— 我们只有**菜单 FTUE** 的 Done 位（`Shell/MenuTutorialFlow.cs` 的 `IsTutorialIndexCompleted`），'
        '原版读 `PlayerDataManager.tutorialStages(+0xF8)`（那是**战斗教程**下标，`MenuTutorialFlow.cs:1097-1098` B91_R2 已就地订正）；本仓**没有**战斗教程进度的存档；'
        '② `TutorialManager.SetupTutorialMatch()` 的等价物 —— 全仓 0 命中，要它得先有「教程用哪副牌 + PlayerBattleData」的产出；'
        '③ 原版走 `MatchMakerManager.StartMatch(PlayModes.Tutorial, …)` = **换场景**重开，我们只有 `BattleDriver.BeginTutorial(stageIndex)` = **原地重开局、不换场景** ⇒ 用它与原版那条链**不等价**。'
        '⛔ 三样没到位之前不硬做（铁律 18·0：需要拍板的地方默认照原版，不许自己设计）。')

ap('DisplayBigChat(string chatMsgText, CardScript talkingUnit)',
   note='｜✅【B92_B3b 2026-10-28 现核】**无代码差距**（本笔未改这个文件）：两道 guard 逐字照 `.c`（`Battle/BattleBigChat.cs:115`）+ 重载 A 取 `fullHeroSprite` 转发（`:95`）都在 ⇒ '
        '建议核销 A；剩的只有真机验收（原版显示调用体被剥 = 铁律 11 例外①）。')

ap('DisplayBigChat(string chatMsgText, Sprite talkingUnitSprite)',
   note='｜✅【B92_B3b 2026-10-28 现核】同 `DisplayBigChat(CardScript)` 那一行：无代码差距（本笔未改），建议核销 A，剩真机验收。')

ap('ResolveAbilityIconAnim(BattleAction battleAction)',
   note='｜✅【B92_B3b 2026-10-28 现核】**时长口径已对齐、本笔未改**：`AbilityIconAnim.DurationOf` = Landing `LandingPreDelay 0.5 + HoldSeconds 0.2 = 0.7` / '
        '其余 `0.2`，与 `_d__475__MoveNext` 那两个 `yield` 逐值相同；`LandingTrigger = 440 (0x1b8)` 也对上 ⇒ 本条除「形状差 + `waitForAnim` 只做读口」外**无剩余**。')

ap('MulliganFallbackCountdown()',
   note='｜【B92_B3b 2026-10-28】「收尾」这一项**不在本轮白名单**（实现在 `CardPresentation/Net/NetBattle.cs`）—— 本笔只做判据侧复核：'
        '`TickMulliganFallback` 与 `.c` 逐条对齐（`4f` / `attempts > 15` / `CancelMatchWithVictory` 等价物 `AbortWithVictory`），**无残留** ⇒ 建议核销 A。')

# ---------------------------------------------------------------- 生成
raw = io.open(MAIN, 'rb').read().decode('utf-8-sig')
lines = raw.replace('\r\n', '\n').split('\n')
header = lines[0].split('\t')
assert len(header) == 11, header

targets = {}
for i, ln in enumerate(lines[1:], 2):
    c = ln.split('\t')
    if len(c) != 11 or c[C_CLASS] != 'BattleManager':
        continue
    if c[C_METHOD] in APPEND:
        assert c[C_METHOD] not in targets, '重复键 %s' % c[C_METHOD]
        targets[c[C_METHOD]] = (i, c)

missing = [k for k in APPEND if k not in targets]
assert not missing, '主表里找不到（⛔ 不许新增键）: %r' % missing

out = []
for sig, (lineno, c) in targets.items():
    cells = list(c)
    for col, txt in APPEND[sig].items():
        assert '\t' not in txt and '\n' not in txt, sig
        cells[col] = cells[col] + txt
    if ROUND not in cells[C_ROUND]:
        cells[C_ROUND] = (cells[C_ROUND] + ' · ' + ROUND) if cells[C_ROUND].strip() else ROUND
    assert len(cells) == 11 and '(' in cells[C_METHOD]
    assert cells[C_STATE] in ('A', 'B', 'C', 'D'), (sig, cells[C_STATE])
    out.append((lineno, cells))
out.sort(key=lambda x: x[0])

body = '\t'.join(header) + '\r\n' + ''.join('\t'.join(c) + '\r\n' for _, c in out)
io.open(OUT, 'wb').write(b'\xef\xbb\xbf' + body.encode('utf-8'))
print('OK -> %s  rows=%d  bytes=%d' % (OUT, len(out), len(body) + 3))
for lineno, c in out:
    print('  main:%d  %-2s  %s' % (lineno, c[C_STATE], c[C_METHOD][:64]))
