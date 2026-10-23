#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""`A1932` —— 修 `card_stats.json` 的 `ocrSrc` 列（PnP 目录树被重构过，旧子目录名全失效）。

用法（本仓一律 `python -I -X utf8`）：
    python -I -X utf8 工具/_fix_ocrsrc_1014.py            # 干跑：只打印，不落盘
    python -I -X utf8 工具/_fix_ocrsrc_1014.py --apply    # 真改（只改 ocrSrc 一列）
    python -I -X utf8 工具/_fix_ocrsrc_1014.py --check    # 只做体检：每条非空 ocrSrc 是否真能打开；坏一条就退出码 1

判据（⛔ 不是猜的）：
  1. PnP 真身目录 = `d:/2/Warpforge部队卡片/<阵营>/<1督军|2天赋|3部队|4计策|5防御卡|6秘密|6药剂|6破坏卡>/<文件>`
     —— 2026-10-14 现读磁盘，真身 = os.walk 出来的**真实路径**（不是拼出来的）。
  2. 旧值失效的原因是**只有子目录那一段变了**（阵营段与文件名段实测都没变 —— 见 --report 输出）：
       · 全表 1121 条非空，旧文件名的**基名在现目录树里都能唯一命中**（1121/1121，0 歧义 0 未知）；
       · 阵营段（`Warpforge部队卡片` 紧接着的那一级）与卡的 `faction` 字段**1119/1119 全部对得上**（0 不符）；
       · 基名与卡名/`ocrName` 归一化后一致（1009 直接一致，另 24 条只差撇号/叹号/连字符这类
         文件名不许出现的字符，人工看过**逐条对得上**）。
  ⇒ 所以修法 = **只换路径里的目录段**，值取自 os.walk 的真身；⛔ 不做任何模糊匹配。

安全约束（都是被这个仓踩出来的）：
  · 只动 `ocrSrc` 这一个字段 —— 落盘前**逐条比对其余字段**，有一处不同就 abort。
  · **不整篇重新序列化** —— 本文件有 **23 行** `"desc"` 的缩进是手工多了一个空格
    （其余全是 3 空格，这 23 行是 4 空格：`:8040` `:8466` `:8497` …），`json.dumps` 会把它们抹平
    （实测往返短 23 字节）⇒ 那样 diff 就不止 ocrSrc 了。改用**原文字面量替换**。
  · **行尾保持**：原文件是纯 CRLF（36003/36003），写回时按原样（二进制写，不 translate）。
  · 写之前先把内容算进变量（`open(p,'wb').write(<表达式>)` 会先截断文件）。
"""

import io
import json
import os
import sys
import collections
import hashlib
import re

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法（本仓一律 `python -I -X utf8`）：\n    python -I -X utf8 工具/_fix_ocrsrc_1014.py            # 干跑：只打印，不落盘\n    python -I -X utf8 工具/_fix_ocrsrc_1014.py --apply    # 真改（只改 ocrSrc 一列）\n    python -I -X utf8 工具/_fix_ocrsrc_1014.py --check    # 只做体检：每条非空 ocrSrc 是否真能打开；坏一条就退出码 1\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT4 = os.path.dirname(HERE)                                   # d:/4/Unity
STATS = os.path.join(ROOT4, '数据', '游戏数据', 'card_stats.json')
PNP = r'd:/2/Warpforge部队卡片'
# `Warpforge部队卡片` 这一段的结尾下标 —— 全表旧值里它只出现一次。
ANCHOR = 'Warpforge部队卡片'


# ---------------------------------------------------------------- PnP 目录索引

def build_index():
    """返回 (exact_basename_upper_or_not, ) —— 两个索引：原名精确、NFC+大小写/下划线/空格归一。"""
    import unicodedata

    exact = collections.defaultdict(list)
    for dp, _dn, fn in os.walk(PNP):
        for f in fn:
            exact[f].append(os.path.join(dp, f))
    norm = collections.defaultdict(list)
    for k, v in exact.items():
        key = unicodedata.normalize('NFC', k).lower().replace('_', '-').replace(' ', '-')
        norm[key].extend(v)

    def lookup(basename):
        import unicodedata as u
        c = exact.get(basename)
        if c:
            return c
        key = u.normalize('NFC', basename).lower().replace('_', '-').replace(' ', '-')
        return norm.get(key, [])

    return lookup


# ---------------------------------------------------------------- 核心：算新值

def plan(cards, lookup):
    """返回 (changes, unresolved, anomalies)。

    changes   : [(idx, old, new)]
    unresolved: [(idx, name, old, 为什么)]
    anomalies : [(idx, name, old, new, 备注)]  —— 解析得出来，但落点不在「五位标准子目录」里，只报不改口径
    """
    changes, unresolved, anomalies = [], [], []
    for i, c in enumerate(cards):
        old = c.get('ocrSrc')
        if not old:
            continue
        if os.path.isfile(old.replace('\\', '/')):
            continue                                    # 本来就打得开 ⇒ 一个字节都不动
        base = os.path.basename(old.replace('\\', '/'))
        cand = lookup(base)
        if len(cand) != 1:
            unresolved.append((i, c.get('name'), old, '基名命中 %d 个（要求恰好 1 个）' % len(cand)))
            continue
        real = cand[0].replace('\\', '/')
        rel = os.path.relpath(real, PNP).replace('\\', '/')
        sep = '\\' if '\\' in old else '/'
        head = old[: old.index(ANCHOR) + len(ANCHOR)]
        new = head + sep + sep.join(rel.split('/'))
        if new == old:
            unresolved.append((i, c.get('name'), old, '解析出来还是原值（应当不存在）'))
            continue
        changes.append((i, old, new))
        top = rel.split('/')
        if len(top) != 3 or top[1] not in ('1督军', '2天赋', '3部队', '4计策', '5防御卡',
                                          '6秘密', '6药剂', '6破坏卡'):
            anomalies.append((i, c.get('name'), old, new, '落点 = %s' % rel))
    return changes, unresolved, anomalies


# ---------------------------------------------------------------- 原地替换

def apply_text(raw, changes):
    """原文里**字面量**替换：`"旧值"` -> `"新值"`（JSON 转义形态），并核对出现次数。"""
    txt = raw.decode('utf-8')
    pairs = collections.OrderedDict()                    # old -> new（同值必然同新）
    expect = collections.Counter()
    for _i, old, new in changes:
        if old in pairs and pairs[old] != new:
            raise SystemExit('⛔ 同一个旧值要有两个新值：%r' % old)
        pairs[old] = new
        expect[old] += 1
    for old in pairs:
        lit = json.dumps(old, ensure_ascii=False)
        n = txt.count(lit)
        if n != expect[old]:
            raise SystemExit('⛔ 旧值字面量出现 %d 次、表里 %d 条：%r' % (n, expect[old], old))
    for old, new in pairs.items():
        txt = txt.replace(json.dumps(old, ensure_ascii=False),
                          json.dumps(new, ensure_ascii=False))
    return txt.encode('utf-8')


# ---------------------------------------------------------------- 体检

def do_check(pairs):
    bad = []
    for row in pairs:
        if not os.path.isfile(row['v'].replace('\\', '/')):
            bad.append(row)
    print('检查 %d 条非空 ocrSrc：可打开 %d · 打不开 %d' % (len(pairs), len(pairs) - len(bad), len(bad)))
    for r in bad[:30]:
        print('  ✗ %s | %s' % (r['n'], r['v']))
    return 1 if bad else 0


def rows_of(cards):
    return [{'n': (c.get('name') or '?'), 'v': c.get('ocrSrc')} for c in cards if c.get('ocrSrc')]


# ---------------------------------------------------------------- main

def main():
    argv = sys.argv[1:]
    apply_ = '--apply' in argv
    check_ = '--check' in argv

    raw = open(STATS, 'rb').read()
    md5_before = hashlib.md5(raw).hexdigest()
    crlf_b = raw.count(b'\r\n')
    lf_b = raw.count(b'\n')
    cards = json.loads(raw.decode('utf-8'))['cards']
    print('文件 %s' % STATS)
    print('  改前：md5=%s  size=%d  行尾 CRLF=%d / LF=%d' % (md5_before, len(raw), crlf_b, lf_b))
    print('  条目 %d（非空 ocrSrc %d）' % (len(cards), len(rows_of(cards))))

    if check_:
        sys.exit(do_check(rows_of(cards)))

    lookup = build_index()
    changes, unresolved, anomalies = plan(cards, lookup)
    print()
    print('=== 计划 ===')
    print('  要改 %d 条 · 解析不了 %d 条 · 落点非标准子目录（已改、另报）%d 条' %
          (len(changes), len(unresolved), len(anomalies)))
    oldsub = collections.Counter(os.path.dirname(c[1].replace('\\', '/')).split('/')[-1] for c in changes)
    newsub = collections.Counter(os.path.dirname(c[2].replace('\\', '/')).split('/')[-1] for c in changes)
    print('  旧子目录：%s' % dict(oldsub.most_common()))
    print('  新子目录：%s' % dict(newsub.most_common()))
    for r in unresolved:
        print('  ⛔ 修不了 idx=%d name=%s old=%s（%s）' % r)
    for a in anomalies:
        print('  ⚠️ 非标准落点 idx=%d name=%s -> %s（%s）' % (a[0], a[1], a[3], a[4]))

    if not changes:
        print('没有要改的。')
        return

    print()
    print('=== 抽样 10 条 ===')
    for _i, old, new in changes[:10]:
        print('  %s\n    -> %s' % (old, new))

    if not apply_:
        print()
        print('（干跑，没落盘。加 --apply 才写。）')
        return

    out = apply_text(raw, changes)
    # 落盘前的死检查：① JSON 还能解 ② 除 ocrSrc 外一个字段都没变 ③ 行尾没翻
    after = json.loads(out.decode('utf-8'))['cards']
    if len(after) != len(cards):
        raise SystemExit('⛔ 条数变了')
    ndirty = 0
    for a, b in zip(cards, after):
        for k in set(a) | set(b):
            if k == 'ocrSrc':
                continue
            if a.get(k) != b.get(k):
                ndirty += 1
                if ndirty <= 5:
                    print('⛔ 字段 %r 被改了：%r -> %r' % (k, a.get(k), b.get(k)))
    if ndirty:
        raise SystemExit('⛔ 有 %d 处非 ocrSrc 字段被动过，已中止，文件没写。' % ndirty)
    if out.count(b'\r\n') != out.count(b'\n'):
        raise SystemExit('⛔ 行尾翻了（CRLF=%d LF=%d），已中止，文件没写。' %
                         (out.count(b'\r\n'), out.count(b'\n')))
    if out.count(b'\r\n') != crlf_b:
        raise SystemExit('⛔ 行数变了，已中止。')
    open(STATS, 'wb').write(out)                     # ← 内容早已算进 out，这一句才截断
    md5_after = hashlib.md5(open(STATS, 'rb').read()).hexdigest()
    print()
    print('✅ 已落盘：md5 %s -> %s · size %d -> %d' % (md5_before, md5_after, len(raw), len(out)))
    rc = do_check(rows_of(json.loads(open(STATS, 'rb').read().decode('utf-8'))['cards']))
    print('复检退出码 = %d（0 = 全部能打开）' % rc)


if __name__ == '__main__':
    main()
