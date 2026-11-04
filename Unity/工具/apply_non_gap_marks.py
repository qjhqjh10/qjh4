#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""apply_non_gap_marks.py — 给账本 A 行的 `备注`（第 11 列）末尾追加 `[非缺口]` 标记。

🔴 为什么要有它（2026-11-05 · `B143-W9`）：
   `工具/check_a_vs_diff.py` 的「已定性」口径 = **整行 `差异`/`备注` 里出现 `[非缺口]` 就整行跳过**。
   逐条判完不等于销号 —— 判定必须**回写进账**，否则下一次扫描还是同一批。本脚本只做那一件事。

🔴 纪律（照本仓「写盘类脚本默认什么都不做」那条）：
   · **不带显式动作参数 ⇒ 只打印用法并退出，rc=2，一个字节都不写**；
   · `--dry` 只打印「会改哪几行、改成什么」；只有 `--write` 才真写；
   · **只动第 11 列（备注）**：第 1–10 列逐字节原样保留；
   · **行数与列数不变**（只在行尾追加，不插行/不删行）；
   · 写回前自检（每行 11 列 · LF · 裸 CR = 0），任一项不符 ⇒ **拒写整份文件**；
   · **幂等**：整行已含 `[非缺口]` ⇒ 跳过（计入「跳过重复」）。

用法
----
    python -I -X utf8 d:/4/Unity/工具/apply_non_gap_marks.py                      # 只打印本段并退出（rc=2）
    python -I -X utf8 d:/4/Unity/工具/apply_non_gap_marks.py --list <清单.tsv> --dry    # 只报，不写
    python -I -X utf8 d:/4/Unity/工具/apply_non_gap_marks.py --list <清单.tsv> --write  # 真写

    # 🆕 2026-11-05：`--list` 与 `--exclude` 都可**重复给**（多清单合并，再减掉撤出清单）
    python -I -X utf8 d:/4/Unity/工具/apply_non_gap_marks.py \
        --list 批1.tsv --list 批2.tsv --exclude 撤出.tsv --dry
    · `--list` 多份 ⇒ 并集（同键去重，依据取最先出现的那份）；
    · `--exclude` 多份 ⇒ 从并集里**扣除**这些键；清单格式同 `--list`（只读前两列）。

清单格式（TSV；**只读前两列**，第三列起是给人看的一句话依据）
--------------------------------------------------------------
    <账文件名(可带 .tsv)>\t<行号(1-based，含表头)>\t<依据>

统计口径（输出里逐项印）
------------------------
    清单行数 / 会改 N 行 / 跳过重复 M 行 / 跳过（状态≠A）K 行 / 拒写原因

退出码：0 = 动作正常跑完（即使 0 行可改）；2 = 用法错 / 自检失败拒写。
"""
import io
import os
import sys

LEDGER_DIR = r"D:/4/Unity/资料/完全复刻/账"
NCOL = 11
NOTE_COL = 10          # 0-based：第 11 列
STATUS_COL = 5         # 0-based：第 6 列
MARK = "[非缺口]（B143 2026-11-05：见 判据/B143_CHK05弱信号定性.md）"

USAGE = __doc__


def die_usage(msg=None):
    if msg:
        print("⛔ " + msg)
    print(USAGE)
    sys.exit(2)


def parse_args(argv):
    if not argv:
        die_usage()
    action = None
    lists, excludes = [], []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a in ('--dry', '--write'):
            if action is not None and action != a:
                die_usage("--dry 与 --write 只能给一个")
            action = a
        elif a in ('--list', '--exclude'):
            i += 1
            if i >= len(argv):
                die_usage(a + " 后面要跟清单路径")
            (lists if a == '--list' else excludes).append(argv[i])
        else:
            die_usage("不认识的参数：" + a)
        i += 1
    if action is None:
        die_usage("必须显式给 --dry 或 --write")
    if not lists:
        die_usage("必须给至少一个 --list <清单.tsv>")
    for p in lists + excludes:
        if not os.path.isfile(p):
            die_usage("清单不存在：" + p)
    return action, lists, excludes


def load_list(path):
    """⇒ [(文件, 行号, 依据)]。歧义/坏行直接报出来（不静默丢）。"""
    raw = io.open(path, 'rb').read().decode('utf-8').split('\n')
    out, bad = [], []
    for n, l in enumerate(raw, 1):
        if not l.strip():
            continue
        if l.lstrip().startswith('#'):      # 允许 `#` 注释行（清单开头写口径用）
            continue
        c = l.split('\t')
        if len(c) < 2:
            bad.append((n, '列数 < 2')); continue
        f = c[0].strip()
        if not f.endswith('.tsv'):
            f += '.tsv'
        try:
            row = int(c[1].strip())
        except ValueError:
            bad.append((n, '行号不是整数：' + c[1])); continue
        why = c[2].strip() if len(c) > 2 else ''
        out.append((f, row, why))
    return out, bad


def read_lines(path):
    return io.open(path, 'rb').read().decode('utf-8').split('\n')


def audit(lines, name):
    """写前自检：返回问题清单（空 = 通过）。"""
    problems = []
    bad_cols = [i + 1 for i, l in enumerate(lines) if l.strip() and len(l.split('\t')) != NCOL]
    if bad_cols:
        problems.append('%s：%d 行列数 ≠ %d（首个：第 %d 行）' % (name, len(bad_cols), NCOL, bad_cols[0]))
    raw = io.open(os.path.join(LEDGER_DIR, name), 'rb').read()
    if raw.count(b'\r') != raw.count(b'\r\n'):
        problems.append('%s：存在**裸 CR**（%d 个）' % (name, raw.count(b'\r') - raw.count(b'\r\n')))
    if b'\r\n' in raw:
        problems.append('%s：含 CRLF（本仓账本一律 LF）' % name)
    return problems


def main():
    action, lists, excludes = parse_args(sys.argv[1:])
    items, bad, seen = [], [], {}
    for lp in lists:
        part, pbad = load_list(lp)
        print('清单：%s  行数 %d（坏行 %d）' % (lp, len(part), len(pbad)))
        for n, why in pbad:
            print('  ⛔ 坏行 %d：%s' % (n, why))
        for f, row, why in part:
            k = (f, row)
            if k in seen:
                continue                      # 同键去重：依据取最先出现的那份
            seen[k] = True
            items.append((f, row, why))
        bad += pbad
    if len(lists) > 1:
        print('  ⇒ 多清单并集（去重后）%d 行' % len(items))
    if excludes:
        keep = set()
        for ep in excludes:
            part, pbad = load_list(ep)
            for f, row, why in part:
                keep.add((f, row))
            print('扣除清单：%s  行数 %d（坏行 %d）' % (ep, len(part), len(pbad)))
        before = len(items)
        items = [it for it in items if (it[0], it[1]) not in keep]
        print('  ⇒ 扣除 %d 行（并集 %d → %d）' % (before - len(items), before, len(items)))

    by_file = {}
    for f, row, why in items:
        by_file.setdefault(f, []).append((row, why))

    total_changed = total_dup = total_status = 0
    refuse = []
    plans = {}          # 文件名 -> (新行列表, 变更明细)
    for f in sorted(by_file):
        p = os.path.join(LEDGER_DIR, f)
        if not os.path.isfile(p):
            refuse.append('找不到账文件：' + f)
            continue
        lines = read_lines(p)
        problems = audit(lines, f)
        if problems:
            refuse += problems
            continue
        changed = dup = skipped_status = 0
        detail = []
        for row, why in by_file[f]:
            if row < 1 or row > len(lines) or not lines[row - 1].strip():
                refuse.append('%s:%d 行号越界或空行' % (f, row))
                continue
            c = lines[row - 1].split('\t')
            if len(c) != NCOL:
                refuse.append('%s:%d 列数 ≠ %d（拒改）' % (f, row, NCOL))
                continue
            if '[非缺口]' in c[7] or '[非缺口]' in c[NOTE_COL]:
                dup += 1
                continue
            if c[STATUS_COL].strip() != 'A':
                skipped_status += 1
                continue
            new = lines[row - 1] + ' ' + MARK
            if len(new.split('\t')) != NCOL:
                refuse.append('%s:%d 追加后列数变了（拒改）' % (f, row))
                continue
            lines[row - 1] = new
            changed += 1
            detail.append((row, why))
        plans[f] = (lines, detail, changed, dup, skipped_status)
        total_changed += changed
        total_dup += dup
        total_status += skipped_status

    print('')
    print('%-24s %8s %8s %10s' % ('账文件', '会改', '跳过重复', '状态≠A'))
    for f in sorted(plans):
        _, _, ch, dp, st = plans[f]
        print('%-24s %8d %8d %10d' % (f, ch, dp, st))
    print('%-24s %8d %8d %10d' % ('合计', total_changed, total_dup, total_status))

    if refuse:
        print('')
        print('⛔ **拒写 %d 条**：' % len(refuse))
        for r in refuse:
            print('   ' + r)
        print('')
        print('⇒ 有拒写项，**一个字节都没写**。修好清单/账之后再跑。')
        sys.exit(2)

    if action == '--dry':
        print('')
        print('--- DRY RUN（未写盘）---')
        for f in sorted(plans):
            _, detail, _, _, _ = plans[f]
            if not detail:
                continue
            print('## ' + f)
            for row, why in detail:
                print('   L%-5d %s' % (row, ('原因：' + why) if why else ''))
        print('')
        print('⇒ DRY：会改 %d 行 · 跳过重复 %d 行 · 跳过（状态≠A）%d 行 · 拒写 0 条。⛔ 未写任何文件。'
              % (total_changed, total_dup, total_status))
        return

    # --write
    for f in sorted(plans):
        lines, _, ch, _, _ = plans[f]
        if not ch:
            continue
        p = os.path.join(LEDGER_DIR, f)
        out = '\n'.join(lines)
        io.open(p, 'wb').write(out.encode('utf-8'))
        print('✅ 写入 %s：%d 行' % (f, ch))
    print('')
    print('⇒ WRITE 完成：%d 行 · 跳过重复 %d · 跳过（状态≠A）%d。'
          % (total_changed, total_dup, total_status))


main()
