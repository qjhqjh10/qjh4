# -*- coding: utf-8 -*-
# 生成账补丁 _parts/B9_B2AU.tsv（只读主表；只改 差异/轮次/备注 三列）
import csv, io, os

LEDGER = r'C:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv'.replace('C:/4', 'D:/4')
OUT = r'D:/4/Unity/资料/完全复刻/账/_parts/B9_B2AU.tsv'

with io.open(LEDGER, encoding='utf-8', newline='') as f:
    rd = csv.reader(f, delimiter='\t')
    hdr = next(rd)
    rows = [r for r in rd]

src = None
for r in rows:
    if len(r) >= 3 and r[hdr.index('方法')].startswith('LogActivatedTrigger('):
        src = r
        break
assert src is not None, 'row not found'
assert len(hdr) == 11, hdr

diff = (
    '表现层/挑战日志那一侧**本轮补上**（B2AU 2026-10-27）：`CardPresentation/Shell/BattleLogData.cs` 新增 '
    '`LogTriggerAbility(bool isPlayer, int trigger, string triggerName=null)` —— '
    '① 照原版 `ChallengeLogMgr.LogTriggerAbility`（ILSpy `ChallengeLogMgr.cs:578-585`）'
    '**过滤 WhileInPlay(20) / WhileInPlayVariable(240) / TriggeredSynapse(630)**；'
    '② 发 `TriggerAbility` 事件（= 原版 `Signal.Raise(new TriggerAbilitySignal(trigger, isPlayer))`，'
    '订阅者逐个 try、抛异常只出声不带崩结算）；'
    '③ 🔶 自己设计（非原版）：按 `(方, trigger)` 按局累计 → `Match.Triggers`'
    '（原版**不落盘、不存任何东西**，消费者是挑战/任务进度）。'
    '引擎侧 `BattleContext.Emit → AchievementTally.AddTrigger`（X2）仍在，但**它不过滤那三条**'
    '（现读 `AchievementTally.cs:169-184` 只判边界）⇒ 与挑战口径不一致（会把 20/240/630 也计进去），'
    '改动在 `RuleEngine/Core/**`（E1RNG 占用）⇒ 只报不改。'
    '**仍缺**：表现层接线一行（`BattleDriver.PlaySignal` 的 `EvtKind.Trigger` 支，现读 `:22561` 一带）'
    '+ 挑战页 UI（P2）。'
)

note = (
    '[B2AU 2026-10-27] 施工单 440 行原来写「加 LogTriggerAbility(isPlayer, trigger, cardId) / 在 RuleCore 消费点记账 / '
    '与 LogMatchEnd 同一份存档」—— **那是猜的**：实读原版 ILSpy `BattleManager.cs:48762-48765` + '
    '`ChallengeLogMgr.cs:578-585` = **没有 cardId、不落盘**，只发一条进程内信号且**过滤 20/240/630**。'
    '本轮落 Shell 侧两半（信号 = 复刻 / 按局累计 = 🔶自设计），typecheck 0/0；接线一行在 BattleDriver（E3CMP）。'
    '引擎侧 AchievementTally 未过滤那三条 ⇒ 已报（不在白名单）。 | '
    '前述 R1T3A / E11RC「引擎侧已由 Emit 单口覆盖」的结论仍然成立，本行只是补上表现层那一半。'
)

row = list(src)
row[hdr.index('差异')] = diff
row[hdr.index('轮次')] = 'B9_B2AU'
row[hdr.index('备注')] = note
assert row[hdr.index('状态')] == src[hdr.index('状态')], '状态 must not change'

os.makedirs(os.path.dirname(OUT), exist_ok=True)
with io.open(OUT, 'w', encoding='utf-8', newline='') as f:
    w = csv.writer(f, delimiter='\t', lineterminator='\n', quoting=csv.QUOTE_MINIMAL)
    w.writerow(hdr)
    w.writerow(row)

with io.open(OUT, encoding='utf-8', newline='') as f:
    chk = list(csv.reader(f, delimiter='\t'))
print('rows', len(chk), 'cols', [len(r) for r in chk])
print('状态=', chk[1][hdr.index('状态')], '| 等级=', repr(chk[1][hdr.index('等级')]), '| 轮次=', chk[1][hdr.index('轮次')])
print('diff_len', len(chk[1][hdr.index('差异')]), 'note_len', len(chk[1][hdr.index('备注')]))
