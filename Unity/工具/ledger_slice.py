# -*- coding: utf-8 -*-
"""只读账切片器（供子代理按文件/类挑自己要做的行）。

用法（⛔ 只读，绝不写盘）：
    python -I -X utf8 d:/4/Unity/工具/ledger_slice.py --file RuleCore            # 落点里出现 RuleCore 的行
    python -I -X utf8 d:/4/Unity/工具/ledger_slice.py --file RuleCore --status B # 再按状态筛
    python -I -X utf8 d:/4/Unity/工具/ledger_slice.py --class AbilityLogic       # 按【原版类】筛
    python -I -X utf8 d:/4/Unity/工具/ledger_slice.py --domain 战斗逻辑驱动 --status B --nofile
    python -I -X utf8 d:/4/Unity/工具/ledger_slice.py --file RuleCore --keys      # 只打「类 / 方法 / 状态」三列，便于清单

输出：默认打「行号 + 11 列」的 TSV（第一列 = **该主表文件的真实行号**，表头是第 1 行）。
行数以 --limit 控制（默认 200，0 = 不限）。
"""
import argparse
import io
import os
import sys

D = r"D:/4/Unity/资料/完全复刻/账/"
COLS = ['域', '原版类', '方法', '判据出处', '判据要点', '状态',
        '我们的落点', '差异', '等级', '轮次', '备注']


def load(domain=None):
    out = []
    files = sorted(f for f in os.listdir(D) if f.endswith('.tsv'))
    for f in files:
        if domain and f[:-4] != domain:
            continue
        raw = io.open(D + f, 'rb').read().decode('utf-8')
        for i, line in enumerate(raw.split('\n')):
            if not line or line.startswith('域\t'):
                continue
            c = line.split('\t')
            if len(c) != 11:
                continue
            out.append((f, i, c))
    return out


def main():
    ap = argparse.ArgumentParser(add_help=True)
    ap.add_argument('--file', default='', help='我们的落点里含这个子串（例：RuleCore）')
    ap.add_argument('--class', dest='klass', default='', help='原版类名（精确或子串）')
    ap.add_argument('--domain', default='', help='只在这个域文件里找（例：战斗逻辑驱动）')
    ap.add_argument('--status', default='', help='状态：A/B/C/D，逗号分隔')
    ap.add_argument('--method', default='', help='方法名子串')
    ap.add_argument('--nofile', action='store_true', help='只要落点里**没有** `xxx.cs:N` 的行')
    ap.add_argument('--hasfile', action='store_true', help='只要落点里**有** `xxx.cs:N` 的行')
    ap.add_argument('--keys', action='store_true', help='只打 类/方法/状态')
    ap.add_argument('--limit', type=int, default=200)
    a = ap.parse_args()

    sts = [s.strip() for s in a.status.split(',') if s.strip()]
    rows = []
    for f, i, c in load(a.domain or None):
        if a.file and a.file not in c[6]:
            continue
        if a.klass and a.klass not in c[1]:
            continue
        if a.method and a.method not in c[2]:
            continue
        if sts and c[5] not in sts:
            continue
        has = '.cs:' in c[6]
        if a.nofile and has:
            continue
        if a.hasfile and not has:
            continue
        rows.append((f, i, c))

    if a.limit and len(rows) > a.limit:
        rows = rows[:a.limit]

    if a.keys:
        for f, i, c in rows:
            print('%s\t%d\t%s' % (f, i + 1, '\t'.join([c[1], c[2], c[5]])))
    else:
        print('域文件\t物理行\t' + '\t'.join(COLS))
        for f, i, c in rows:
            print('%s\t%d\t%s' % (f, i + 1, '\t'.join(c)))
    sys.stderr.write('\n合计 %d 行%s\n' % (len(rows), '' if not a.limit else '（已全打）'))


if __name__ == '__main__':
    main()
