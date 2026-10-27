# -*- coding: utf-8 -*-
"""check_draft_choices.py —— 账 208 / J-11 的**可跑自检**：13 个阵营 × 胜场 0..9 的「可用环境卡数」对表。

**它验什么**：`CardPresentation/Resources/OffensiveCards.json` 的**数据** + **阈值口径**（严格大于）是否满足
施工单 208 的验收口：
    victories = 0/2/3/6/9 ⇒ 防御 **1/1/1/2/3** · 进攻 **0/0/1/2/3**
⚠️ 它**不**验 C# 代码本身（那是 `OffensiveCards.ChoicesForDraft` / `DumpDraftAvailabilityForTest`，
在 Unity 自检宿主里跑）。规则是**照抄**那两处的：
    ② `offensive && victories < 3` ⇒ 空
    ④ 防御：恒 `[0]` · `>5` 加 `[1]` · `>8` 加 `[2]`
    ⑤ 进攻：`>2` 加 `[0]` · `>5` 加 `[1]` · `>8` 加 `[2]`
⇒ 两边任何一处改了阈值，这里会**和 C# 对不上**（本脚本不写盘，改哪边由人定）。

用法：
    python -I -X utf8 d:/4/Unity/工具/check_draft_choices.py          # 打印表 + 断言（不符 ⇒ 退出码 1）
    python -I -X utf8 d:/4/Unity/工具/check_draft_choices.py -v       # 额外逐格打印
"""
import io
import json
import sys

SRC = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/OffensiveCards.json'
VS = (0, 2, 3, 6, 9)


def counts(army, offensive, victories):
    """与 C# `ChoicesForDraft` 同一份规则。"""
    if offensive and victories < 3:
        return 0
    arr = army.get('cards' if offensive else 'defensiveCards') or []
    if not offensive:
        n = 1 if len(arr) >= 1 else 0
        if victories > 5 and len(arr) >= 2:
            n += 1
        if victories > 8 and len(arr) >= 3:
            n += 1
        return n
    n = 0
    if victories > 2 and len(arr) >= 1:
        n += 1
    if victories > 5 and len(arr) >= 2:
        n += 1
    if victories > 8 and len(arr) >= 3:
        n += 1
    return n


def main():
    verbose = '-v' in sys.argv[1:]
    d = json.load(io.open(SRC, encoding='utf-8'))
    armies = d.get('armies') or []
    bad = []
    print('%-16s %s' % ('阵营', '  '.join('v%d(防/攻)' % v for v in VS)))
    for a in armies:
        name = a.get('army') or '?'
        cells = []
        for v in VS:
            dd = counts(a, False, v)
            oo = counts(a, True, v)
            cells.append('%d/%d' % (dd, oo))
            want_d = 1 + (1 if v > 5 else 0) + (1 if v > 8 else 0)
            want_o = (1 if v > 2 else 0) + (1 if v > 5 else 0) + (1 if v > 8 else 0)
            if len(a.get('defensiveCards') or []) < 3:
                pass            # 数据本身不足 ⇒ 不记逻辑错（C# 的 DefensiveSlotsShort 同口径）
            elif dd != want_d:
                bad.append((name, 'def', v, dd, want_d))
            if oo != want_o:
                bad.append((name, 'off', v, oo, want_o))
        print('%-16s %s' % (name, '  '.join('%-7s' % c for c in cells)))

    def uniq(off):
        return sorted({counts(a, off, v) for a in armies for v in VS})

    print()
    print('每阵营槽数：cards=%s defensiveCards=%s' %
          (sorted({len(a.get('cards') or []) for a in armies}),
           sorted({len(a.get('defensiveCards') or []) for a in armies})))
    print('防御：victories %s ⇒ %s（期望 1/1/1/2/3）' %
          (list(VS), [sorted({counts(a, False, v) for a in armies}) for v in VS]))
    print('进攻：victories %s ⇒ %s（期望 0/0/1/2/3）' %
          (list(VS), [sorted({counts(a, True, v) for a in armies}) for v in VS]))

    # 🔎 全扫：0..9 每一档都按公式核一遍（不只是验收那 5 档）。
    sweep_bad = []
    for a in armies:
        name = a.get('army') or '?'
        short = len(a.get('defensiveCards') or []) < 3
        for v in range(0, 10):
            want_d = 1 + (1 if v > 5 else 0) + (1 if v > 8 else 0)
            want_o = (1 if v > 2 else 0) + (1 if v > 5 else 0) + (1 if v > 8 else 0)
            if not short and counts(a, False, v) != want_d:
                sweep_bad.append((name, 'def', v))
            if counts(a, True, v) != want_o:
                sweep_bad.append((name, 'off', v))
    cells = len(armies) * 10 * 2
    print('0..9 全扫：%d 格 %s' % (cells, '✅ 全对' if not sweep_bad else '🔴 %d 格不符 %s' % (len(sweep_bad), sweep_bad[:6])))
    if sweep_bad:
        bad.extend([(n, k, v, -1, -1) for (n, k, v) in sweep_bad])

    if verbose:
        for b in bad:
            print('  ✗', b)
    ok = not bad
    print('结论：', '✅ 全部对上' if ok else '🔴 %d 格不符' % len(bad))
    sys.exit(0 if ok else 1)


if __name__ == '__main__':
    main()
