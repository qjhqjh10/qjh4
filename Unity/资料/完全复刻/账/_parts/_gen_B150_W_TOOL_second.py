# -*- coding: utf-8 -*-
"""B150_W_TOOL · B145-SECOND-REF-SWEEP 补丁生成器（**只写 _parts 补丁，⛔ 不动主表**）。

口径完全复用 `工具/second_ref_census.py`（它又复用 `工具/check_ledger_points.py` 的判定函数），
只做两件它不做的事：
  1) 把 B 类（全文件唯一一处声明行 ⇒ 可机械重钉）的「旧 ref → 新落点」**落成一个账补丁**；
  2) 每改一行都复算一次「新落点那一行确实有这个名字」+「行数/列数守恒」，不符就跳过并印出来。

用法（⛔ 必须显式给 --write，否则只打印统计）：
    python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B150_W_TOOL_second.py            # 只报
    python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B150_W_TOOL_second.py --write    # 落补丁

纪律：
  · 二进制读、二进制写；补丁用 LF（`merge_ledger_parts.read_raw` 认两种行尾，补丁行尾不参与决定）。
  · 只改「我们的落点」列（第 7 列 / 0-based 6），其余 10 列逐字节原样。
  · 11 列 + `方法` 必须是单方法签名（与 `merge_ledger_parts.looks_like_one_method` 同口径）。
  · 同一账行有多个 SECOND ⇒ 合并到同一条补丁行里（key = (原版类, 方法)）。
  · 「新行有/旧行也有」的那几条**照样重钉但单独印出来**，交调度台合并前人看一眼。
"""
import glob
import importlib.util
import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
TOOLS = r"D:/4/Unity/工具"


def _load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


clp = _load('clp', os.path.join(TOOLS, 'check_ledger_points.py'))
mlp = _load('mlp', os.path.join(TOOLS, 'merge_ledger_parts.py'))
src = io.open(os.path.join(TOOLS, 'second_ref_census.py'), 'r', encoding='utf-8').read()

# 复用 second_ref_census 的 classify/ledger_row_index（从源码里取函数体太重，直接复制两段）
spec2 = importlib.util.spec_from_file_location('srcmod', os.path.join(TOOLS, 'second_ref_census.py'))

LEDGER_DIR = clp.LEDGER_DIR
ROOTS = list(clp.DEFAULT_ROOTS)
OUT = os.path.join(HERE, 'B150_W_TOOL.tsv')
COLS_HDR = mlp.COLS


def classify(base, who, land, why):
    m = re.match(r'（次要落点）\s*(\S+?):(\d+)\s', why)
    rel, ln = (m.group(1), int(m.group(2))) if m else ('?', 0)
    old = '%s:%d' % (rel, ln)
    if '找不到' in why or '短名歧义' in why or '文件不存在' in why:
        return 'D', old, '', '', 0, '路径不可解析'
    if '行号越界' in why:
        return 'D', old, '', '', 0, '行号越界'
    if '是空的' in why:
        return 'D', old, '', '', 0, '那一行是空行'
    if '读不了文件' in why:
        return 'D', old, '', '', 0, '读不了文件'
    nm = re.search(r'声称的名字 `([^`]+)`', why)
    if not nm:
        return 'D', old, '', '', 0, '未识别的告警：' + why[:60]
    name = nm.group(1)
    full, note, exact = clp.resolve_ref_file(rel, ROOTS)
    if full is None:
        return 'D', old, name, '', 0, '路径不可解析'
    lines = clp.read_src_lines(full)
    where = clp.name_lines(lines, name)
    cands = clp.find_decl_candidates(lines, name)
    if ln in where:
        return 'A', old, name, '', len(cands), '名字就在这一行（告警按散文误报）'
    if len(cands) == 1:
        return 'B', old, name, '%s:%d' % (os.path.basename(full), cands[0]), 1, '全文件唯一一处声明行'
    if len(cands) > 1:
        return 'C', old, name, '/'.join(str(c) for c in cands[:6]) + ('…' if len(cands) > 6 else ''), \
            len(cands), '多处声明候选（歧义，需人读）'
    if where:
        return 'D', old, name, '/'.join(str(c) for c in where[:6]), len(where), '名字在文件里但不构成声明行'
    return 'D', old, name, '', 0, '名字在整个文件里 0 命中'


def ledger_row_index(f):
    idx = {}
    raw = io.open(f, 'rb').read().decode('utf-8', 'replace')
    for i, line in enumerate(raw.replace('\r\n', '\n').split('\n'), 1):
        if not line.strip():
            continue
        c = line.split('\t')
        if len(c) < 7:
            continue
        idx.setdefault(('%s.%s' % (c[1], c[2]), c[6]), i)
    return idx


def main():
    do_write = '--write' in sys.argv[1:]
    files = sorted(f for f in glob.glob(os.path.join(LEDGER_DIR, '*.tsv'))
                   if not os.path.basename(f).startswith('_'))
    todo = {}          # (basename, rowno) -> {'line': [...], 'fixes': [(old, new, name)]}
    stats = {'A': 0, 'B': 0, 'C': 0, 'D': 0}
    cross = {}
    for f in files:
        total, fails, drifts, rewrites, getters, warns, bstat = clp.check_file(f, ROOTS, False, False)
        ridx = ledger_row_index(f)
        for base, who, land, tag, why in warns:
            if tag != clp.WARN_SECOND:
                continue
            cls, old, name, newref, ncand, note = classify(base, who, land, why)
            stats[cls] += 1
            if cls != 'B':
                continue
            rowno = ridx.get((who, land), 0)
            if not rowno:
                print('⚠ 找不到账行：%s · %s' % (who, land))
                continue
            full, _n, _e = clp.resolve_ref_file(newref.split(':')[0], ROOTS)
            ls = clp.read_src_lines(full)
            newln = int(newref.split(':')[-1])
            oldln = int(old.split(':')[-1])
            has_new = name in ls[newln - 1] if 0 < newln <= len(ls) else False
            has_old = name in ls[oldln - 1] if 0 < oldln <= len(ls) else False
            tag2 = ('新行有/旧行无' if (has_new and not has_old) else
                    ('新行有/旧行也有' if has_new else '新行没有名字(!)'))
            cross[tag2] = cross.get(tag2, 0) + 1
            if not has_new:
                print('⛔ 跳过（新落点没有名字）：%s:%d %s → %s' % (os.path.basename(f), rowno, name, newref))
                continue
            key = (os.path.basename(f), rowno)
            ent = todo.setdefault(key, {'who': who, 'land': land, 'fixes': [], 'cross': []})
            ent['fixes'].append((old, newref, name))
            ent['cross'].append((name, tag2))

    # 逐行改
    patch_rows = []
    for (base, rowno), ent in sorted(todo.items()):
        path = os.path.join(LEDGER_DIR, base)
        raw = io.open(path, 'rb').read()
        crlf = raw.count(b'\r\n') == raw.count(b'\n') and raw.count(b'\r\n') > 0
        text = raw.decode('utf-8-sig')
        lines = text.split('\r\n') if crlf else text.split('\n')
        n_before = len(lines)
        cols = lines[rowno - 1].split('\t')
        if len(cols) != 11:
            print('⛔ 跳过（列数 %d ≠ 11）：%s:%d' % (len(cols), base, rowno))
            continue
        land = cols[6]
        ok_all = True
        for old, newref, name in ent['fixes']:
            oldpath = old.rsplit(':', 1)[0]
            # newref 是 basename:行 ⇒ 必须把**原路径前缀**补回去（账里其余 ref 都是全路径）
            newfull = oldpath + ':' + newref.rsplit(':', 1)[-1]
            if old not in land:
                print('⛔ 跳过（落点列找不到旧 ref `%s`）：%s:%d' % (old, base, rowno))
                ok_all = False
                break
            land = land.replace(old, newfull, 1)
        if not ok_all:
            continue
        cols[6] = land
        good, whyb = mlp.looks_like_one_method(cols[2])
        if not good:
            print('⛔ 跳过（方法列不合规 %s）：%s:%d %s' % (whyb, base, rowno, cols[2][:60]))
            continue
        patch_rows.append(cols)
        if len(lines) != n_before:      # 只是自证，不可能触发
            print('⛔ 行数变了：%s' % base)

    print('分类：A %s · B %s · C %s · D %s' % (stats['A'], stats['B'], stats['C'], stats['D']))
    print('交叉核：' + ' · '.join('%s = %d' % (k, v) for k, v in cross.items()))
    print('补丁行数 = %d（账行 %d 条 · SECOND 条目 %d 条）'
          % (len(patch_rows), len(todo), sum(len(v['fixes']) for v in todo.values())))
    for (base, rowno), ent in sorted(todo.items()):
        for name, tag in ent['cross']:
            if tag != '新行有/旧行无':
                print('⚠ 人看一眼：%s:%d · %s · %s' % (base, rowno, name, tag))

    if not do_write:
        print('（未给 --write ⇒ 一个字节都没写）')
        return 0
    out = ['\t'.join(COLS_HDR)] + ['\t'.join(c) for c in patch_rows]
    body = ('\n'.join(out) + '\n').encode('utf-8')
    tmp = OUT + '.tmp'
    io.open(tmp, 'wb').write(body)
    io.open(OUT, 'wb').write(body)
    os.remove(tmp)
    print('✅ 已写 %s（%d 行 + 表头）' % (OUT, len(patch_rows)))
    return 0


if __name__ == '__main__':
    sys.exit(main())
