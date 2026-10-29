# -*- coding: utf-8 -*-
# 生成 B98_M2 的账补丁：从主表 `轮抽.tsv` 的 `轮抽:6`（DraftEventBase.Expire）那一行**原样起手**，
# 只在 `差异` / `轮次` / `备注` 三列追加本轮结论（状态保持 A）。
# 纪律：裸 `split("\t")` 读写（消费方 `工具/merge_ledger_parts.py` 就是裸 split），LF，写前先自检列数。
import io, os

HERE = os.path.dirname(os.path.abspath(__file__))
MAIN = os.path.join(HERE, '..', '轮抽.tsv')
OUT = os.path.join(HERE, 'B98_M2.tsv')

def load_rows(path):
    b = io.open(path, 'rb').read().decode('utf-8')
    return [l for l in b.split('\n')]

rows = load_rows(os.path.normpath(MAIN))
header = rows[0]
assert len(header.split('\t')) == 11, '主表表头不是 11 列：%d' % len(header.split('\t'))

# 定位 `轮抽:6` = 域=轮抽 & 方法 以 `Expire(Action onComplete)` 起头
target = None
for r in rows[1:]:
    if not r.strip():
        continue
    c = r.split('\t')
    if len(c) == 11 and c[0] == '轮抽' and c[2].startswith('Expire(Action onComplete)'):
        target = c
        break
assert target is not None, '主表里没找到 `轮抽` / `Expire(Action onComplete)` 那一行'

DIFF_ADD = (
    ' ｜[B98_M2 2026-10-29] **`== OngoingRun(15)` 那一支现在可达**（`报告/B98_K5.md` §七·1 的残差已清）：'
    '`DraftEventBase.CurrentDraftState` 的反推按 `DraftModeWindow.UpdateState` ①②③ 补齐 `SelectPack(10)` / `OngoingRun(15)` 两档；'
    '真源 = `CardPresentation/Shell/DraftEventBase.cs` **类尾**那三个 `DraftState*` 小函数（`DraftStateFinished` / `DraftStateNotStarted` / '
    '`DraftStatePacksPicked`，逐句对应 `DraftModeWindow.cs:2259-2305`）。既有行**零位移**：本文件 9 条账落点 '
    '`:114/:168/:207/:224/:242/:277/:352/:362/:390` 逐条现核未动（新成员追加类尾）。'
    '⚠️ **仍未解决（超出本代理简报，未改）**：原版 `…DraftEventBase_object__object_.Expire__180f230.c:68-85` 的收尾支**不直接调** `onComplete`'
    '（它把完成动作交给云脚本 `ExpireDraftEvent`(0x36d) 的成功回调 `…_Expire_b__1`，那个回调里做 `SaveGame.joinState = Expired(3)`）；'
    '我们那一支调的是**窗的** `FinishRun()`（`set_DraftState(20)` 语义）且同样不调 `onComplete` ⇒ 两支用什么口径收尾尚未核实到可施工粒度，'
    '见 `报告/B98_M2.md` §还差什么 第 1 条。'
)

ROUND_ADD = '｜B98_M2'

NOTE_ADD = (
    ' ｜[B98_M2 2026-10-29] **状态保持 A**：本轮清的只是「状态反推缺 10/15 两档」造成的**可达性**缺口（`报告/B98_K5.md` §七·1），'
    '方法体与触发源都是既有件。断言 = `CardPresentation/Editor/B98M2_Assert.cs`：A–E 段（反推四档 0/5/10/15 + 收尾三条 + `<` 边界）、'
    'F 段（`OngoingRun(15)` 下 `Expire` 走**收尾支** —— 判支拿原版 `.c:79` 的 else 日志字面量 `Trying to finish an unstarted run` 作对照，'
    '不靠「看一眼像对的」）、G 段（`SelectPack(10)` 下走 else 支且 `onComplete` 被调一次 = 灭自证的**对照支**）。'
    '🧨 灭自证：把 `CurrentDraftState` 的反推改回 K5 那一版（只给 0/5/20）⇒ F1 红（状态算成 5 ⇒ 打进 else 支）；'
    '把 ③ 的 `<` 写成 `<=` ⇒ D4 红。**宿主未接线**（建议 `Editor/DraftWindowAssertD2.cs` 的 `Run()` 加 `B98M2_Assert.Run();` + 两行 Pass/Fail 累加）。'
    'typecheck（`TMPDIR=/tmp/wf_m2`）：运行时程序集 **0**；编辑器在「别人的 `Editor/BattleScene.cs` 6 条 `CS1061 ShowMyDeck`」出现之前的上一趟是 **0**，'
    '之后的每一趟仍只有那 6 条（`B98_N1` 删了 `BattleDriver.ShowMyDeck`、`BattleScene.cs` 还在调 ⇒ 与本轮文件无关，见 `报告/B98_M2.md` §七·3）。'
)

target[7] = target[7] + DIFF_ADD
target[9] = target[9] + ROUND_ADD
target[10] = target[10] + NOTE_ADD

assert len(target) == 11, '补丁行列数 %d != 11' % len(target)
for i, v in enumerate(target):
    assert '\n' not in v and '\r' not in v, '第 %d 列里有换行' % i

out = header + '\n' + '\t'.join(target) + '\n'
data = out.encode('utf-8')
io.open(OUT, 'wb').write(data)

# 往返自检：读回来逐列比对（裸 split）
raw = io.open(OUT, 'rb').read()
print('wrote', OUT, len(raw), 'bytes · LF', raw.count(b'\n'), '· CRLF', raw.count(b'\r\n'))
back = [l for l in raw.decode('utf-8').split('\n') if l.strip()]
print('rows(含表头) =', len(back), '· 列数 =', [len(l.split('\t')) for l in back])
assert back[0].split('\t') == header.split('\t'), '表头往返不一致'
assert back[1].split('\t') == target, '行往返不一致'
print('往返自检通过')
