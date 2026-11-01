#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""check_a_vs_diff.py — 「完全复刻」账本：**A 档 × 差异/备注里的缺口词** 矛盾扫描。

🔴 本工具**只报候选，判据仍要人读**（同 `工具/arena_diff.py` 的口径）——
   它给的是「状态写着 A、差异或备注里却出现缺口措辞」的**可疑行**，不是「这行一定错了」。

🔴🔴 **v2（2026-11-01 · B115_W6）：本工具在加了 `--baseline` 之后【会写盘】，所以按
   「写盘类脚本默认只打印用法」的纪律，【不带显式动作参数 ⇒ 只打印用法并退出，什么也不做】**。
   ⛔ 别再把「无参调用」当扫描 —— 扫描要显式写 `--scan`。
   （由来：`merge_ledger_parts.py --help` 不解析 `--help`、不带参就真合并，冲掉 232 行手工修订。）

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

⚠️ **2026-11-01 现读读数（B115_W6 钉的基线，⛔ 别抄进别处当现状，引用一律现跑）**：
   **强 89 · 弱 1,492 · 合计 1,581 · 已标注 `[非缺口]` 跳过 396 · A 行 5,841**。
   ⚠️ **这个口径本身有个已知偏差**：它按**字面**匹配，所以「原备注『…未复刻』**已过期**」这种
   **引用旧措辞的订正句**仍会命中（实测强 89 里 ~47 条是这一类）⇒ **命中数 ≠ 缺口数**。
   🔧 **可选口径 `--skip-stale`**（默认**关**）：整行 `差异`/`备注` 里含 `已过期 / 已作废 / 原备注 / 原账`
   的行**整行跳过**。⚠️ **它会改数字 ⇒ 它产出的数字不能和默认基线比**；两组读数都在
   `报告/B115_W6_工具闸与类名定档.md` §转岗补齐 里钉过。

由来
----
2026-10-25 调度台**两次手工**抓到同一类错误（`CatalogItemContainer.TryPurchase`（P5）·
`UIGradientUtils.RotationDir`（P2)）：**状态 A，差异列却写着缺口** ⇒ 本批（P6）把那次手扫做成工具。
看板口径见 `资料/完全复刻/方案` 与调度台日志；**本工具在 `--scan` / `--only-new` 下不改任何账**
（要改请走各域写手）。**唯一的写盘动作 = `--baseline … --save`**。

用法（🔴 无参 = 只打印本段并退出，⛔ 不扫描、不写盘）
--------------------------------------------------
    python -I -X utf8 d:/4/Unity/工具/check_a_vs_diff.py                       # 只看用法（安全）
    python -I -X utf8 d:/4/Unity/工具/check_a_vs_diff.py --scan                # 只读扫全账
    python -I -X utf8 d:/4/Unity/工具/check_a_vs_diff.py --scan 特效动画        # 只扫一份（不带 .tsv 也行）
    python -I -X utf8 d:/4/Unity/工具/check_a_vs_diff.py --scan --strong       # 只看强信号

    # 写一份基线快照（**显式写盘动作**；文件写你要的路径，仓库里建议放 `_tmp_view/`）
    python -I -X utf8 d:/4/Unity/工具/check_a_vs_diff.py --baseline <路径.tsv> --save
    # 只报「相对基线新增」的命中（只读；同键但文本变了的只给计数）
    python -I -X utf8 d:/4/Unity/工具/check_a_vs_diff.py --baseline <路径.tsv> --only-new

    # 可选口径：再跳过「整行在讲旧措辞」的行（默认**关**，基线口径 = 不跳）
    python -I -X utf8 d:/4/Unity/工具/check_a_vs_diff.py --scan --skip-stale


    `--baseline` 必须**二选一**配 `--save` 或 `--only-new`；两个都不给 = 用法错误（防止「以为在存、
    其实在覆盖」/「以为在比、其实在存」）。`--strong` 可叠加在上面任一动作上。

退出码：`0` = 动作正常跑完（**它仍是报告器，不因命中而为非 0**）；`2` = 用法/参数错（什么都没做）。
"""
import glob
import hashlib
import io
import os
import re
import sys

LEDGER_DIR = r"D:/4/Unity/资料/完全复刻/账"
NCOL = 11
BASELINE_MAGIC = '# check_a_vs_diff baseline v1'

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
# `--skip-stale` 用的「这条备注在讲旧状态」标记（**只在显式开关下生效，默认口径不变**）。
# 判据：命中词落在这类句子里多半是「引用旧措辞」——例：`原备注「活动线暂停，未复刻」已过期`。
STALE_MARKERS = ('已过期', '已作废', '原备注', '原账')

USAGE = __doc__


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


def is_stale(text):
    return any(k in text for k in STALE_MARKERS)


def scan_file(path, skip_stale=False):
    """⇒ (A 行数, [(强度, 类.方法, 命中词, 列名, 片段, 整列原文)], 列数异常行数, 已标注跳过行数, 旧措辞跳过行数)。"""
    rows = io.open(path, 'rb').read().decode('utf-8').split('\n')
    hits = []
    a_rows = 0
    bad_cols = 0
    annotated = 0
    stale = 0
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
        if skip_stale and (is_stale(c[7]) or is_stale(c[10])):
            stale += 1
            continue                                  # 可选口径：整行在讲旧措辞 ⇒ 不计（默认关）
        for cname, idx in COLS:
            text = c[idx]
            if not text:
                continue
            got = pick(text)
            if got:
                strength, kwname, m = got
                hits.append((strength, '%s.%s' % (c[1], c[2]), kwname, cname,
                             '%s：%s' % (cname, snippet(text, m)), text))
    return a_rows, hits, bad_cols, annotated, stale


def resolve_files(args):
    """⇒ (files, error)。文件名解析（原有语义不变）。"""
    if not args:
        return sorted(f for f in glob.glob(os.path.join(LEDGER_DIR, '*.tsv'))
                      if not os.path.basename(f).startswith('_')), None
    files = []
    for a in args:
        p = a if os.path.isfile(a) else os.path.join(LEDGER_DIR, a if a.endswith('.tsv') else a + '.tsv')
        if os.path.isfile(p):
            files.append(p)
        else:
            return None, '找不到账本：' + a
    return files, None


def collect(files, skip_stale=False):
    """⇒ dict(strong, weak, inner, annotated, stale, a_rows)，hits 里每条 = (dom, who, kwname, col, seg, 原文)。"""
    strong, weak = [], []
    inner = annotated = a_rows_total = stale_total = 0
    for f in files:
        a_rows, hits, bad_cols, ann, st = scan_file(f, skip_stale=skip_stale)
        a_rows_total += a_rows
        annotated += ann
        inner += bad_cols
        stale_total += st
        dom = os.path.basename(f)[:-4]
        if bad_cols:
            print('⚠️ %s：%d 行列数 ≠ %d（已跳过；先跑 `工具/ledger_progress.py` 查）'
                  % (dom, bad_cols, NCOL))
        for strength, who, kwname, cname, seg, text in hits:
            rec = (dom, who, kwname, cname, seg, text, strength)
            (strong if strength == '强' else weak).append(rec)
    return {'strong': strong, 'weak': weak, 'inner': inner, 'stale': stale_total,
            'annotated': annotated, 'a_rows': a_rows_total, 'files': files}


def dump(title, rows):
    if not rows:
        return
    print()
    print('---- %s（%d 条）----' % (title, len(rows)))
    print('%-14s | %-52s | %-6s | %s' % ('域', '类.方法', '命中词', '片段'))
    by = {}
    for dom, who, kwname, cname, seg, text, strength in rows:
        by[dom] = by.get(dom, 0) + 1
        print('%-14s | %-52s | %-6s | %s' % (dom, who[:52], kwname, seg))
    print('  按域分布：' + ' · '.join('%s %d' % (k, v) for k, v in
                                 sorted(by.items(), key=lambda kv: (-kv[1], kv[0]))))


def print_header():
    print('= A 档 × 差异/备注缺口词 矛盾扫描（只报候选，判据仍要人读）=')
    print('  强：未落地 / 未实现 / 未复刻 / 待做·待办·TODO')
    print('  弱：没有 / 缺 / 不同 / 不适用（排除「缺注解」「原版名不同」「没有差异」这类否定句与流程标记）')
    print()


def print_summary(res, weak_shown=True):
    strong, weak = res['strong'], res['weak']
    print()
    print('合计 %d 条（强 %d · 弱 %d）· 已标注 `[非缺口]` 跳过 %d 条%s'
          % (len(strong) + len(weak), len(strong), len(weak), res['annotated'],
             '（%d 行列数异常，已跳过）' % res['inner'] if res['inner'] else ''))
    print('A 行 %d · 账文件 %d 份%s'
          % (res['a_rows'], len(res['files']),
             ' · `--skip-stale` 又整行跳过 %d 行（讲旧措辞的 A 行 %s）'
             % (res['stale'], ' / '.join(STALE_MARKERS)) if res.get('stale') else ''))
    allrows = strong + weak
    if allrows:
        by = {}
        for dom, who, kwname, cname, seg, text, strength in allrows:
            by[dom] = by.get(dom, 0) + 1
        print('按域分布（合计）：' + ' · '.join('%s %d' % (k, v) for k, v in
                                          sorted(by.items(), key=lambda kv: (-kv[1], kv[0]))))
    else:
        print('按域分布：（无）')
    print('⚠️ 只报候选：每一条都要回读该行的 `差异`/`备注` 与判据再判（A 行有缺口措辞 = 可疑，'
          '但不必然错，例如「原版也没有」这档，以及「原备注『…未复刻』已过期」这种**引用旧措辞**的订正句）。')


def hit_key(rec):
    dom, who, kwname, cname, seg, text, strength = rec
    return (dom, who, strength, kwname, cname)


def text_digest(rec):
    return hashlib.sha1(rec[5].encode('utf-8')).hexdigest()[:8]


def write_baseline(path, res):
    """写基线快照（**唯一会写盘的动作**）。先算完内容再一次性写，避免「先截断再求值」。"""
    lines = [BASELINE_MAGIC]
    lines.append('# saved %s · ledger_dir=%s · files=%d · a_rows=%d · strong=%d · weak=%d · annotated=%d'
                 % (now_stamp(), LEDGER_DIR, len(res['files']), res['a_rows'],
                    len(res['strong']), len(res['weak']), res['annotated']))
    lines.append('域\t类.方法\t强度\t命中词\t列\t文本摘要\t片段')
    allrows = res['strong'] + res['weak']
    allrows.sort(key=lambda r: (r[0], r[1], r[6], r[2], r[3]))
    for rec in allrows:
        dom, who, kwname, cname, seg, text, strength = rec
        lines.append('\t'.join([dom, who, strength, kwname, cname, text_digest(rec), seg]))
    body = '\n'.join(lines) + '\n'
    d = os.path.dirname(os.path.abspath(path))
    if d and not os.path.isdir(d):
        os.makedirs(d, exist_ok=True)
    with io.open(path, 'w', encoding='utf-8', newline='\n') as fh:
        fh.write(body)
    print('已写基线：%s（%d 条命中 · 强 %d · 弱 %d）' % (path, len(allrows), len(res['strong']), len(res['weak'])))


def now_stamp():
    import time
    return time.strftime('%Y-%m-%dT%H:%M:%S')


def read_baseline(path):
    """⇒ (keys set, digests dict, header dict)。文件格式不对 ⇒ 抛 ValueError。"""
    if not os.path.isfile(path):
        raise ValueError('基线文件不存在：' + path)
    rows = io.open(path, 'rb').read().decode('utf-8').split('\n')
    if not rows or not rows[0].startswith(BASELINE_MAGIC):
        raise ValueError('不是本工具写的基线（首行应为 `%s`）：%s' % (BASELINE_MAGIC, path))
    keys, digests, header = set(), {}, {}
    for i, l in enumerate(rows):
        if i == 0:
            continue
        if l.startswith('#'):
            if 'a_rows=' in l:
                header['line'] = l
            continue
        if not l.strip():
            continue
        c = l.split('\t')
        if l.startswith('域\t类.方法\t'):
            continue                                  # 列名行（不是数据）
        if len(c) != 7:
            raise ValueError('基线第 %d 行列数 %d ≠ 7（文件可能被手工改过）' % (i + 1, len(c)))
        k = (c[0], c[1], c[2], c[3], c[4])
        keys.add(k)
        digests[k] = c[5]
    return keys, digests, header


def main(argv):
    a = list(argv)
    if not a:
        print(USAGE)
        print('⛔ 未执行任何动作（本工具是写盘类：无参调用只打印用法）。')
        return 2
    if '-h' in a or '--help' in a:
        print(USAGE)
        return 0

    do_scan = '--scan' in a
    do_save = '--save' in a
    only_new = '--only-new' in a
    strong_only = '--strong' in a
    skip_stale = '--skip-stale' in a
    baseline = None
    domains = []
    i = 0
    while i < len(a):
        t = a[i]
        if t == '--baseline':
            if i + 1 >= len(a):
                print('⛔ `--baseline` 缺路径。\n')
                print(USAGE)
                return 2
            baseline = a[i + 1]
            i += 2
            continue
        if t in ('--scan', '--save', '--only-new', '--strong', '--skip-stale'):
            i += 1
            continue
        if t.startswith('--'):
            print('⛔ 未知参数：%s\n' % t)
            print(USAGE)
            return 2
        domains.append(t)
        i += 1

    if domains and not (do_scan or only_new or do_save):
        print('⛔ 给了账本名却没给动作（要 `--scan` / `--only-new` / `--save`）。\n')
        print(USAGE)
        return 2
    if do_save and only_new:
        print('⛔ `--save` 与 `--only-new` 不能同时给（一个是写、一个是读）。\n')
        print(USAGE)
        return 2
    if do_save and skip_stale:
        print('⛔ `--save` 不能配 `--skip-stale`：基线**只钉默认口径**，'
              '否则以后拿它比 `--only-new` 会凭空多出一堆「新增」。\n')
        print(USAGE)
        return 2
    if (do_save or only_new) and not baseline:
        print('⛔ `--save` / `--only-new` 必须配 `--baseline <路径>`。\n')
        print(USAGE)
        return 2
    if baseline and not (do_save or only_new):
        print('⛔ 给了 `--baseline` 却没说是写还是读：请加 `--save`（写）或 `--only-new`（读）。'
              '（这条是故意的 —— 写盘必须显式。）\n')
        print(USAGE)
        return 2
    if not (do_scan or do_save or only_new):
        print('⛔ 没有给动作（要 `--scan` / `--save` / `--only-new` 之一）。\n')
        print(USAGE)
        return 2

    files, err = resolve_files(domains)
    if err:
        print(err)
        return 2

    print_header()
    print('动作：%s · 账文件 %d 份%s%s'
          % ('+'.join([x for x, on in (('--scan', do_scan), ('--save', do_save), ('--only-new', only_new)) if on]),
             len(files), (' · 基线 ' + baseline) if baseline else '',
             ' · --skip-stale（默认口径不含它）' if skip_stale else ''))
    res = collect(files, skip_stale=skip_stale)

    if do_save:
        write_baseline(baseline, res)
        if not only_new and not do_scan:
            return 0

    if only_new:
        try:
            base_keys, base_digests, header = read_baseline(baseline)
        except ValueError as e:
            print('⛔ %s' % e)
            return 2
        if 'line' in header:
            print('基线头：' + header['line'].lstrip('# '))
        allrows = res['strong'] + res['weak']
        state = {}
        for rec in allrows:
            state.setdefault(hit_key(rec), []).append(rec)
        new_rows, changed = [], []
        for rec in allrows:
            k = hit_key(rec)
            if k not in base_keys:
                new_rows.append(rec)
            elif base_digests.get(k) != text_digest(rec):
                changed.append(rec)
        print()
        print('新增 %d 条（基线 %d 条 · 现读 %d 条）· 同键但文本已变 %d 条（不计入新增）'
              % (len(new_rows), len(base_keys), len(allrows), len(changed)))
        new_strong = [r for r in new_rows if r[6] == '强']
        new_weak = [r for r in new_rows if r[6] == '弱']
        if new_strong:
            dump('🔴 新增强信号', new_strong)
        if not strong_only and new_weak:
            dump('🟡 新增弱信号', new_weak)
        if not new_rows:
            print('✅ 相对基线没有新增命中。')
        return 0

    # --scan（或 --save 后仍要继续打）
    if res['strong']:
        dump('🔴 强信号 = 状态 A 却写着「没做」', res['strong'])
    if not strong_only:
        dump('🟡 弱信号 = 常见于「等价替换」的正当解释（需人逐条扫）', res['weak'])
    print_summary(res)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
