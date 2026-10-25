#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""check_a_vs_diff.py — 「完全复刻」账本：**A 档 × 差异/备注里的缺口词** 矛盾扫描（只读）。

🔴 本工具**只报候选，判据仍要人读**（同 `工具/arena_diff.py` 的口径）——
   它给的是「状态写着 A、差异或备注里却出现缺口措辞」的**可疑行**，不是「这行一定错了」。

判法
----
扫 `资料/完全复刻/账/*.tsv`（顶层，不含 `_parts/`）里 **状态 == A** 的行，
在 **`差异`（第 8 列）** 与 **`备注`（第 11 列）** 里找下面这张词表（每列只报**最靠前**的那个命中词）。

词表 + 排除规则（⛔ 改词表请连同排除规则一起想清楚，否则会把否定句 / 流程标记扫进来）
------------------------------------------------------------------------------
| 强度 | 命中词 | 正则 | 上下文排除 |
|---|---|---|---|
| **强** | 未落地 | `未落地` | — |
| **强** | 未实现 | `未实现` | — |
| **强** | 未复刻 | `未复刻` | — |
| **强** | 待做   | `待做` / `待办` / `TODO`（不分大小写） | — |
| 弱 | 没有 | `没有(?!差异|不同|问题|缺口|偏离|遗漏|变化)` | —（「没有差异 / 没有不同 / 没有问题 …」是**否定句**） |
| 弱 | 缺     | `(?<![不无零没])缺`，且 `缺(?!注解)` | `缺注解`（这是**流程标记**「缺注解闸」，不是缺口）·「不缺 / 无缺 / 零缺 / 没缺」 |
| 弱 | 不同   | `(?<!没有)(?<!名)不同` | `原版名不同`（G 的**等价注解模板**：原名不同、已核）·「没有不同」 |
| 弱 | 不适用 | `不适用` | — |

**`[非缺口]` 标记（人工确认后的停机位）**
----------------------------------------
一行 A 的 `差异`/`备注` 里**出现 `[非缺口]`** ⇒ 本工具**整行跳过**（并计入「已标注」）。
口径：只有**人读过原文、确认那段是「等价解释」而不是缺口**时才加这个标记（例：
「我们没有 Canvas ⇒ 用渲染队列」）。⛔ 别拿它当消音器 —— 它是**结论**，不是**白名单**。

**强 = 几乎一定是矛盾**（`状态 A` 却写着「没做」）；**弱 = 常见于「等价替换」的正当解释**
（「我们没有 Canvas ⇒ 用渲染队列」），需要人逐条扫。输出分两段给，别把强信号淹在弱信号里。

由来
----
2026-10-25 调度台**两次手工**抓到同一类错误（`CatalogItemContainer.TryPurchase`（P5）·
`UIGradientUtils.RotationDir`（P2)）：**状态 A，差异列却写着缺口** ⇒ 本批（P6）把那次手扫做成工具。
看板口径见 `资料/完全复刻/方案` 与调度台日志；**本工具不改任何账**（要改请走各域写手）。

用法
----
    python -I -X utf8 d:/4/Unity/工具/check_a_vs_diff.py            # 全账
    python -I -X utf8 d:/4/Unity/工具/check_a_vs_diff.py 特效动画    # 只扫一份（名字可不带 .tsv）
    python -I -X utf8 d:/4/Unity/工具/check_a_vs_diff.py --strong    # 只看强信号

退出码：**恒 0**（它是报告器，不是闸）—— 要不要拦，由调用方按输出条数决定。
"""
import glob
import io
import os
import re
import sys

LEDGER_DIR = r"D:/4/Unity/资料/完全复刻/账"
NCOL = 11

# (强度, 显示名, 正则, 上下文排除短语)
KEYWORDS = [
    ('强', '未落地', re.compile(r'未落地'), ()),
    ('强', '未实现', re.compile(r'未实现'), ()),
    ('强', '未复刻', re.compile(r'未复刻'), ()),
    ('强', '待做', re.compile(r'待做|待办|TODO', re.IGNORECASE), ()),
    ('弱', '没有', re.compile(r'没有(?!差异|不同|问题|缺口|偏离|遗漏|变化)'), ()),
    ('弱', '缺', re.compile(r'(?<![不无零没])缺(?!注解)'), ('缺注解',)),
    ('弱', '不同', re.compile(r'(?<!没有)(?<!名)不同'), ('原版名不同',)),
    ('弱', '不适用', re.compile(r'不适用'), ()),
]
COLS = [('差异', 7), ('备注', 10)]


def snippet(text, m, span=28):
    a = max(0, m.start() - span)
    b = min(len(text), m.end() + span)
    s = text[a:b].replace('\t', ' ')
    return ('…' if a > 0 else '') + s + ('…' if b < len(text) else '')


def pick(text):
    """这一列最靠前的那个命中（含强度/命中词/片段）；没有 ⇒ None。"""
    best = None
    for strength, kwname, rx, ctx in KEYWORDS:
        m = rx.search(text)
        while m is not None:
            win = text[max(0, m.start() - 6):m.end() + 12]
            if any(c in win for c in ctx):
                m = rx.search(text, m.end())        # 被排除 ⇒ 找同列下一个（别漏后面的真命中）
                continue
            if best is None or m.start() < best[0]:
                best = (m.start(), strength, kwname, m)
            break
    return None if best is None else (best[1], best[2], best[3])


def scan_file(path):
    rows = io.open(path, 'rb').read().decode('utf-8').split('\n')
    hits = []
    a_rows = 0
    bad_cols = 0
    annotated = 0
    for i, l in enumerate(rows):
        if not l.strip():
            continue
        c = l.split('\t')
        if i == 0 and c[0] == '域':
            continue                                  # 表头
        if len(c) != NCOL:
            bad_cols += 1
            continue
        if c[5].strip() != 'A':
            continue
        a_rows += 1
        if '[非缺口]' in c[7] or '[非缺口]' in c[10]:
            annotated += 1
            continue                                  # 人工确认「这是解释不是缺口」⇒ 不再报
        for cname, idx in COLS:
            text = c[idx]
            if not text:
                continue
            got = pick(text)
            if got:
                strength, kwname, m = got
                hits.append((strength, '%s.%s' % (c[1], c[2]), kwname,
                             '%s：%s' % (cname, snippet(text, m))))
    return a_rows, hits, bad_cols, annotated


def main():
    strong_only = '--strong' in sys.argv
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    if args:
        files = []
        for a in args:
            p = a if os.path.isfile(a) else os.path.join(LEDGER_DIR, a if a.endswith('.tsv') else a + '.tsv')
            if os.path.isfile(p):
                files.append(p)
            else:
                print('找不到账本：' + a)
                return 2
    else:
        files = sorted(f for f in glob.glob(os.path.join(LEDGER_DIR, '*.tsv'))
                       if not os.path.basename(f).startswith('_'))

    print('= A 档 × 差异/备注缺口词 矛盾扫描（只报候选，判据仍要人读）=')
    print('  强：未落地 / 未实现 / 未复刻 / 待做·待办·TODO')
    print('  弱：没有 / 缺 / 不同 / 不适用（排除「缺注解」「原版名不同」「没有差异」这类否定句与流程标记）')
    print()
    strong, weak, inner = [], [], 0
    annotated = 0
    for f in files:
        a_rows, hits, bad_cols, ann = scan_file(f)
        annotated += ann
        dom = os.path.basename(f)[:-4]
        if bad_cols:
            inner += bad_cols
            print('⚠️ %s：%d 行列数 ≠ %d（已跳过；先跑 `工具/ledger_progress.py` 查）'
                  % (dom, bad_cols, NCOL))
        for strength, who, kwname, seg in hits:
            (strong if strength == '强' else weak).append((dom, who, kwname, seg))
        if a_rows and not hits:
            print('%-14s | （A 共 %d 条，无命中）' % (dom, a_rows))

    def dump(title, rows):
        if not rows:
            return
        print()
        print('---- %s（%d 条）----' % (title, len(rows)))
        print('%-14s | %-52s | %-6s | %s' % ('域', '类.方法', '命中词', '片段'))
        by = {}
        for dom, who, kwname, seg in rows:
            by[dom] = by.get(dom, 0) + 1
            print('%-14s | %-52s | %-6s | %s' % (dom, who[:52], kwname, seg))
        print('  按域分布：' + ' · '.join('%s %d' % (k, v) for k, v in
                                     sorted(by.items(), key=lambda kv: (-kv[1], kv[0]))))

    if strong:
        dump('🔴 强信号 = 状态 A 却写着「没做」（%d 条，几乎一定是矛盾）' % len(strong), strong)
    if not strong_only:
        dump('🟡 弱信号 = 常见于「等价替换」的正当解释（需人逐条扫）', weak)

    print()
    print('合计 %d 条（强 %d · 弱 %d）· 已标注 `[非缺口]` 跳过 %d 条%s'
          % (len(strong) + len(weak), len(strong), len(weak), annotated,
             '（%d 行列数异常，已跳过）' % inner if inner else ''))
    allrows = strong + weak
    if allrows:
        by = {}
        for dom, who, kwname, seg in allrows:
            by[dom] = by.get(dom, 0) + 1
        print('按域分布（合计）：' + ' · '.join('%s %d' % (k, v) for k, v in
                                          sorted(by.items(), key=lambda kv: (-kv[1], kv[0]))))
    else:
        print('按域分布：（无）')
    print('⚠️ 只报候选：每一条都要回读该行的 `差异`/`备注` 与判据再判（A 行有缺口措辞 = 可疑，'
          '但不必然错，例如「原版也没有」这档）。')
    return 0


if __name__ == '__main__':
    sys.exit(main())
