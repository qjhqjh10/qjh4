#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
把 29 份子代理产出（Unity/资料/卡表核对_卡图提取/*.md）合并成一张总表。

用法： python Unity/工具/merge_cards_new.py
产出： Unity/资料/卡表核对_卡图提取/_合并总表.md   + 控制台自检报告
"""
import io, os, re, sys, collections

# ⚠️ 2026-09-13 从 `d:/4/工具/` 挪到 `Unity/工具/` 了，产物也搬出了 `_tmp_view/`（那目录**不进 git**）。
ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
BASE = os.path.join(ROOT, 'Unity', '资料', '卡表核对_卡图提取')

COLS = ["文件名","卡名","费用","近战","远程","护甲","生命","字段","类型","稀有度","效果原文"]

def parse_file(path):
    rows = []
    bad = []
    for ln, line in enumerate(io.open(path, encoding='utf-8'), 1):
        line = line.rstrip('\n').rstrip('\r')
        if not line.startswith('|'):
            continue
        # 表头 / 分隔行
        if line.startswith('|---') or '文件名' in line and '卡名' in line and '效果原文' in line:
            continue
        parts = line.split('|')
        # 首尾各一个空串
        if parts and parts[0].strip() == '': parts = parts[1:]
        if parts and parts[-1].strip() == '': parts = parts[:-1]
        if len(parts) < len(COLS) - 1:
            continue
        if len(parts) > len(COLS):
            # 多出来的竖线都在「效果原文」里 —— 合并回去
            head = parts[:len(COLS)-1]
            tail = '|'.join(parts[len(COLS)-1:])
            parts = head + [tail]
        cells = [c.strip() for c in parts]
        if len(cells) != len(COLS):
            bad.append((ln, line[:90]))
            continue
        rows.append(cells)
    return rows, bad

def main():
    files = sorted(f for f in os.listdir(BASE) if f.endswith('.md') and not f.startswith('_'))
    allrows = []
    report = []
    for f in files:
        rows, bad = parse_file(os.path.join(BASE, f))
        report.append((f, len(rows), len(bad)))
        for r in rows:
            r.append(f)          # 记来源文件，方便回溯
            allrows.append(r)

    print("=== 各文件解析结果 ===")
    for f, n, b in report:
        flag = "" if b == 0 else "   ⚠️ %d 行没解析出来" % b
        print("  %-30s %3d 行%s" % (f, n, flag))
    print("  合计 %d 行（%d 个文件）" % (len(allrows), len(files)))

    # 文件名去重检查
    byfile = collections.defaultdict(list)
    for r in allrows:
        byfile[os.path.basename(r[0])].append(r)
    dup = {k: v for k, v in byfile.items() if len(v) > 1}
    print("\n=== 重复文件名：%d 个 ===" % len(dup))
    for k, v in list(dup.items())[:10]:
        print("  %s  出现在: %s" % (k, ", ".join(r[-1] for r in v)))

    out = os.path.join(BASE, '_合并总表.md')
    with io.open(out, 'w', encoding='utf-8') as fh:
        fh.write("# 卡面逐张提取 · 合并总表（%d 张 / %d 份批次文件）\n\n" % (len(allrows), len(files)))
        fh.write("> 来源：`D:/2/Warpforge部队卡片/` 的卡图，由子代理**逐张看图**独立抄录（未参考任何已有数据）。\n")
        fh.write("> 生成脚本：`Unity/工具/merge_cards_new.py`。**别手改本文件**。\n\n")
        fh.write("| " + " | ".join(COLS) + " | 来源批次 |\n")
        fh.write("|" + "---|" * (len(COLS)+1) + "\n")
        for r in sorted(allrows, key=lambda x: (x[-1], os.path.basename(x[0]))):
            fh.write("| " + " | ".join(r) + " |\n")
    print("\n写出：%s" % out)
    return 0

if __name__ == '__main__':
    sys.exit(main())
