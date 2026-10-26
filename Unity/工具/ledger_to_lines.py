# -*- coding: utf-8 -*-
"""账 → 任务线 对账口（只读）。

来源：`资料/完全复刻/报告/RU_任务核销_第2轮.md` §④·2 的要求 ——
「每批收工时把**动过的账行**映射到对应任务线（哪怕只是指针）」。
否则「回头核旧任务是否顺带解决」（批次 6）永远核不全。

用法（⛔ 只读，不写任何文件）：
    python -I -X utf8 d:/4/Unity/工具/ledger_to_lines.py --patch 账/_parts/B8_A.tsv
    python -I -X utf8 d:/4/Unity/工具/ledger_to_lines.py --domain 战斗逻辑驱动 --status B
    python -I -X utf8 d:/4/Unity/工具/ledger_to_lines.py --domain 收藏 --status B --missing
        `--missing` = 只打「在任务线目录里 0 命中」的行（= 需要补指针的）

输出：TSV（域 / 线 / 原版类 / 方法 / 状态 / 任务线命中 / 建议）
"""
import io
import os
import sys

LEDGER = r"d:/4/Unity/资料/完全复刻/账/"
LINES = r"d:/4/Unity/资料/任务线/"

# 域 → 任务线（多值 = 两边都该留指针；顺序 = 主责在前）
MAP = {
    '个人资料_成就_排行': ['界面线'],
    '其他_基础设施': ['工具文档线'],
    '卡牌面_渲染': ['卡牌线'],
    '卡组编辑': ['界面线'],
    '商店': ['界面线'],
    '战役': ['界面线'],
    '战斗界面_HUD': ['战斗线'],
    '战斗逻辑驱动': ['引擎线', '战斗线'],
    '收藏': ['界面线'],
    '教程_引导': ['新手引导线'],
    '日常_奖励': ['活动线'],
    '未分类_其他': ['工具文档线'],
    '本地化': ['工具文档线'],
    '特效动画': ['特效线'],
    '社交_聊天': ['界面线'],
    '联机': ['联机线'],
    '菜单_外壳': ['界面线'],
    '轮抽': ['界面线'],
    '锻造': ['界面线'],
    '音频_语音': ['音效线'],
    '泛型与启动链_补读': ['引擎线'],
}


def rows_from_patch(p):
    raw = io.open(p, 'rb').read().decode('utf-8-sig')
    out = []
    for i, l in enumerate(raw.split('\n')):
        c = l.split('\t')
        if len(c) == 11 and i > 0:
            out.append(c)
    return out


def rows_from_ledger(domain, statuses):
    out = []
    p = os.path.join(LEDGER, domain + '.tsv')
    raw = io.open(p, 'rb').read().decode('utf-8')
    for i, l in enumerate(raw.split('\n')):
        c = l.split('\t')
        if len(c) != 11 or i == 0:
            continue
        if statuses and c[5] not in statuses:
            continue
        out.append(c)
    return out


def index_line_dir(line):
    """返回 {文件名: 全文小写}"""
    d = os.path.join(LINES, line)
    files = {}
    if not os.path.isdir(d):
        return files
    for root, _dirs, fs in os.walk(d):
        for f in fs:
            if not f.endswith('.md'):
                continue
            p = os.path.join(root, f)
            try:
                files[os.path.relpath(p, LINES)] = io.open(p, 'rb').read().decode('utf-8', 'replace').lower()
            except Exception:
                pass
    return files


def main():
    a = sys.argv[1:]
    if not a:
        print(__doc__)
        return 0
    patch = domain = ''
    statuses = []
    only_missing = '--missing' in a
    for flag in ('--patch', '--domain', '--status'):
        if flag in a:
            i = a.index(flag)
            v = a[i + 1] if i + 1 < len(a) else ''
            if flag == '--patch':
                patch = v
            elif flag == '--domain':
                domain = v
            else:
                statuses = [s.strip() for s in v.split(',') if s.strip()]

    if patch:
        if not os.path.isabs(patch):
            patch = os.path.join(r'd:/4/Unity/资料/完全复刻', patch) if not patch.startswith('账') \
                else os.path.join(r'd:/4/Unity/资料/完全复刻', patch)
        rows = rows_from_patch(patch)
    elif domain:
        rows = rows_from_ledger(domain, statuses)
    else:
        print(__doc__)
        return 0

    cache = {}
    print('域\t线\t原版类\t方法\t状态\t任务线命中\t建议')
    miss = 0
    for c in rows:
        dom, klass, meth, st = c[0], c[1], c[2], c[5]
        lines = MAP.get(dom, ['(未映射)'])
        needle = meth.split('(')[0].strip().lower()
        if not needle:
            needle = klass.lower()
        hit = ''
        for ln in lines:
            if ln not in cache:
                cache[ln] = index_line_dir(ln)
            for rel, text in cache[ln].items():
                if needle and needle in text:
                    hit = '%s/%s' % (ln, rel)
                    break
            if hit:
                break
        if not hit:
            miss += 1
        if only_missing and hit:
            continue
        print('%s\t%s\t%s\t%s\t%s\t%s\t%s' % (
            dom, '/'.join(lines), klass, meth, st,
            hit or '-', '已有' if hit else '补一行指针'))
    sys.stderr.write('\n合计 %d 行 · 任务线 0 命中 %d 行\n' % (len(rows), miss))
    return 0


if __name__ == '__main__':
    sys.exit(main())
