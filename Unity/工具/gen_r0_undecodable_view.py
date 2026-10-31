#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""R0-05: 「解不出」的独立视图（**不改主表**）。

背景（判据 → `判据/R0_资源覆盖账_现状与判据.md` §4.3 / §7-2）：
  905 个「解不出」对象（`_stats.json` 的 fail 求和）在账里**没有任何可过滤的标志** ——
  它们散在正常使用档里（无独立名字 718 · 已用工程内 90 · 读文档 50 · 用数据表 18 ·
  已搬运 15 · 从没看过 13 · 读索引 1）⇒ 其中 **64 条（50+13+1）被静默计进「没用上 17,371」**。

做法：
  🔴 **主表 `R0_资源覆盖账.tsv` 一个字都不改**（R1/R7 都按列名读，加列会静默改变消费方行为）。
  改成**流式 join** 出一份 905 行的独立视图：`(文件, PathID)` ↔ 主表行（91 MB，⛔ 不整表读进内存）。
  列 = `源文件 / PathID / 名字 / R0档位 / R1分档`；同一 `(文件,PathID)` 在主表里命中多行时，
  `R0档位` 用 ` | ` 串起来（`.sharedAssets` 的 CAB 分叉会产生这种情形）。

🔴 默认（不带参数）**只打印用法并退出，什么都不做**（本项目铁律：会写盘的脚本必须显式动作参数）。

Run:  python -I -X utf8 d:/4/Unity/工具/gen_r0_undecodable_view.py --check
      python -I -X utf8 d:/4/Unity/工具/gen_r0_undecodable_view.py --apply
      [--r1 PATH] [--ledger PATH] [--out PATH]
Out:  <out> 默认 D:/4/Unity/资料/完全复刻/资源/R0_解不出视图.tsv
"""
import os, sys, io

R1 = r'D:/4/Unity/资料/完全复刻/资源/R1_905对象归属.tsv'
LEDGER = r'D:/4/Unity/资料/完全复刻/资源/R0_资源覆盖账.tsv'
OUT = r'D:/4/Unity/资料/完全复刻/资源/R0_解不出视图.tsv'

# R0 主表列序（现读，⛔ 别按记忆写 —— 见 `R0_资源覆盖账.tsv` 表头）
R0_COL_FILE, R0_COL_PID, R0_COL_USAGE = 0, 1, 4
R1_COL_FILE, R1_COL_PID, R1_COL_NAME, R1_COL_TIER = 0, 1, 3, 4
USAGE_UNUSED = ('从没看过', '读过（索引）', '读过（文档）')

USAGE = (
    '用法：python -I -X utf8 d:/4/Unity/工具/gen_r0_undecodable_view.py --check|--apply\n'
    '      [--r1 PATH] [--ledger PATH] [--out PATH]\n'
    '  从 `R1_905对象归属.tsv`（905 行）+ `R0_资源覆盖账.tsv`（91 MB，流式 join）生成「解不出」独立视图。\n'
    '  选项： --check       只比对（不写盘）；退出码 0=一致 · 1=不一致 · 2=输入缺失\n'
    '         --apply       真写（**唯一会写文件的参数**；不带它 = 只打印本用法并退出，零读零写）\n'
    '         --r1 PATH     R1 归属表，默认 D:/4/Unity/资料/完全复刻/资源/R1_905对象归属.tsv\n'
    '         --ledger PATH 主账，默认 D:/4/Unity/资料/完全复刻/资源/R0_资源覆盖账.tsv\n'
    '         --out PATH    输出，默认 D:/4/Unity/资料/完全复刻/资源/R0_解不出视图.tsv\n'
    '  -h, --help     只打印本用法并退出（退出码 0）\n'
)

import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print(USAGE)
    _wf_help_sys.exit(0)

HEADER = ['源文件', 'PathID', '名字', 'R0档位', 'R1分档']


def read_r1(path):
    """→ (rows, order)。rows 用 (文件, PathID) 做键；保留 R1 的行序（稳定输出）。"""
    rows, order = {}, []
    with io.open(path, encoding='utf-8-sig') as f:      # R1 带 BOM（现读 EF BB BF）
        for i, line in enumerate(f):
            if i == 0:
                continue
            p = line.rstrip('\n').rstrip('\r').split('\t')
            if len(p) <= R1_COL_TIER:
                continue
            key = (p[R1_COL_FILE], p[R1_COL_PID])
            if key in rows:                              # 理论上不该有重复；有就出声
                print('⚠️ R1 里出现重复键：%s / %s（后一条覆盖前一条）' % key, flush=True)
            rows[key] = p
            order.append(key)
    return rows, order


def join_stream(r1, ledger_path):
    """**流式**扫 91 MB 主表，只留下 R1 那 905 个键的档位。"""
    hits = {}          # key -> [R0档位, ...]
    seen = set()
    with io.open(ledger_path, encoding='utf-8') as f:
        for i, line in enumerate(f):
            if i == 0:
                continue
            p = line.rstrip('\n').rstrip('\r').split('\t')
            if len(p) <= R0_COL_USAGE:
                continue
            key = (p[R0_COL_FILE], p[R0_COL_PID])
            if key not in r1:
                continue
            seen.add(key)
            hits.setdefault(key, []).append(p[R0_COL_USAGE])
    return hits, seen


def build(r1_path=R1, ledger_path=LEDGER):
    r1, order = read_r1(r1_path)
    hits, seen = join_stream(r1, ledger_path)
    L = ['\t'.join(HEADER)]
    missing = 0
    for key in order:
        row = r1[key]
        us = hits.get(key)
        if not us:
            missing += 1
            us_s = '（主表里查无此键）'
        else:
            # 去重保序：同键多行时（同一 PathID 出现在不同 CAB）把档位串起来
            uniq = []
            for u in us:
                if u not in uniq:
                    uniq.append(u)
            us_s = ' | '.join(uniq)
        L.append('\t'.join([row[R1_COL_FILE], row[R1_COL_PID], row[R1_COL_NAME],
                            us_s, row[R1_COL_TIER]]))
    return L, len(order), missing, hits, order, r1


def summarize(order, hits):
    import collections
    cnt = collections.Counter()
    unused = 0
    for key in order:
        us = hits.get(key) or ['（主表里查无此键）']
        cnt[us[0]] += 1
        if us[0] in USAGE_UNUSED:
            unused += 1
    return cnt, unused


def _write(path, text):
    with io.open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)
    # 存盘类脚本：写完**重开读一遍**确认真的落了盘
    back = io.open(path, encoding='utf-8').read()
    assert back == text, 'tsv 落盘回读不一致: ' + path


def _parse_args(argv):
    action, r1, ledger, out, i = None, R1, LEDGER, OUT, 0
    while i < len(argv):
        a = argv[i]
        if a in ('--check', '--apply'):
            action = a[2:]
            i += 1
            continue
        if a in ('--r1', '--ledger', '--out'):
            if i + 1 >= len(argv):
                return None, None, None, None, '`%s` 缺参数' % a
            v = argv[i + 1]
            if a == '--r1':
                r1 = v
            elif a == '--ledger':
                ledger = v
            else:
                out = v
            i += 2
            continue
        return None, None, None, None, '未知参数：%s' % a
    if action is None:
        return None, None, None, None, '没有 `--check` / `--apply`：什么都不做（这是有意的，见文件头）'
    return action, r1, ledger, out, None


def main(argv):
    action, r1p, ledger, out, err = _parse_args(argv)
    if err:
        print(err)
        print()
        print(USAGE)
        return 0 if not argv else 2
    for p in (r1p, ledger):
        if not os.path.exists(p):
            print('🔴 输入不存在：' + p)
            return 2
    L, n_r1, missing, hits, order, r1 = build(r1p, ledger)
    text = '\n'.join(L) + '\n'
    cnt, n_unused = summarize(order, hits)
    print('rows', n_r1, 'joined', len(hits), 'missing', missing, flush=True)
    print('R0档位分布：', dict(sorted(cnt.items(), key=lambda kv: -kv[1])), flush=True)
    print('其中落在「没用上」三档（从没看过/读过索引/读过文档）的 =', n_unused, flush=True)
    if missing:
        print('⚠️ %d 行在主表里按 (文件,PathID) 找不到 —— ⛔ 别当成 0，回去核主表' % missing, flush=True)
    if action == 'check':
        old = io.open(out, encoding='utf-8').read() if os.path.exists(out) else None
        if old is None:
            print('CHECK DIFF  目标 tsv 不存在：' + out)
            return 1
        if old == text:
            print('CHECK OK  一致：' + out)
            return 0
        ol, nl = old.split('\n'), text.split('\n')
        for i in range(min(len(ol), len(nl))):
            if ol[i] != nl[i]:
                print('CHECK DIFF  第 %d 行起不一致：\n  现盘: %s\n  重算: %s'
                      % (i + 1, ol[i][:160], nl[i][:160]))
                break
        else:
            print('CHECK DIFF  行数不同：现盘 %d · 重算 %d' % (len(ol), len(nl)))
        return 1
    _write(out, text)
    print('wrote', out, len(L) - 1, 'rows')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
