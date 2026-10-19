#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""verify_archive.py — 离线自检：`历史_Godot快照` 归档完整性（2026-10-19 · 账 A2112）

只读、不写盘；不依赖 Unity / 不跑任何 `*.Run`。

检查五件事：
  1) 旧路径不存在（`Unity/数据/界面布局/ui_layout` · `Unity/数据/粒子/{particles,particles3d,arena_particles}`），
     且两个旧父目录下没有任何文件残留；
  2) 新路径存在且文件数 = 期望值（3 / 201 / 863 / 54）；
  3) 代码侧（`Unity/MyGame/Assets/**/*.cs|*.unity`）对旧路径的引用 = 0；
  4) 文档/工具侧（`Unity/资料/**/*.md` · `Unity/工具/**/*.py`）对旧路径的引用：
     只允许「同一行同时出现 `已归档` / `历史_Godot快照` / `作废`」的行；
     豁免目录 = `资料/任务线/`（任务书，本轮写手无所有权）与 `资料/历史/`（历史归档）；
  5) 工具侧落点：`工具/sync_from_d2.py` 不再把粒子/ui_layout 播种回旧路径；
     `工具/scripts快照/convert_unity_particles.py` 必须要求 `--out`、且默认不再写 `D:/warpforge`。

退出码：0 = 全过；1 = 有失败项。
用法：
    python -I -X utf8 d:/4/Unity/数据/历史_Godot快照/verify_archive.py [--verbose]
"""
import argparse
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

D4 = 'd:/4/Unity'
ARCHIVE = f'{D4}/数据/历史_Godot快照'

# 归档前的旧相对路径（相对 Unity/）
OLD_DIRS = [
    '数据/界面布局/ui_layout',
    '数据/粒子/particles',
    '数据/粒子/particles3d',
    '数据/粒子/arena_particles',
]
OLD_PARENTS = ['数据/界面布局', '数据/粒子']
NEW_DIRS = {
    '数据/历史_Godot快照/界面布局/ui_layout': 3,
    '数据/历史_Godot快照/粒子/particles': 201,
    '数据/历史_Godot快照/粒子/particles3d': 863,
    '数据/历史_Godot快照/粒子/arena_particles': 54,
}
# 旧路径片段（只匹配 Unity/数据/ 下这两族；d:/2/Warpforge_tools/data/ui_layout 是另一条路径、不在此列）
OLD_FRAGS = [
    'Unity/数据/粒子', 'Unity\\数据\\粒子',
    'Unity/数据/界面布局', 'Unity\\数据\\界面布局',
    '数据/粒子/', '数据\\粒子\\',
    '数据/界面布局/ui_layout', '数据\\界面布局\\ui_layout',
]
SELF_MARKERS = ['已归档', '历史_Godot快照', '作废']

fails = []
checks = 0


def ok(cond, msg):
    global checks
    checks += 1
    if not cond:
        fails.append(msg)
        print('  [FAIL] ' + msg)
    elif VERBOSE:
        print('  [ ok ] ' + msg)


def count_files(root):
    n = 0
    for _dp, _dn, fs in os.walk(root):
        n += len(fs)
    return n


def iter_files(root, exts):
    for dp, dn, fs in os.walk(root):
        dn[:] = [d for d in dn if d not in ('.git', '_tmp_view')]
        for f in fs:
            if any(f.endswith(e) for e in exts):
                yield os.path.join(dp, f)


def scan_refs(files, skip_dirs=()):
    """返回 [(path, lineno, line)] —— 命中旧路径片段且没有豁免标记的行。"""
    hits = []
    for p in files:
        np = p.replace('\\', '/')
        if any(d in np for d in skip_dirs):
            continue
        try:
            with open(p, 'r', encoding='utf-8', errors='replace') as fh:
                for i, line in enumerate(fh, 1):
                    if not any(frag in line for frag in OLD_FRAGS):
                        continue
                    if any(m in line for m in SELF_MARKERS):
                        continue
                    hits.append((p, i, line.strip()))
        except OSError as e:
            fails.append('读不了 %s: %s' % (p, e))
    return hits


def main():
    global VERBOSE
    ap = argparse.ArgumentParser()
    ap.add_argument('--verbose', action='store_true')
    VERBOSE = ap.parse_args().verbose

    print('== 1) 旧路径 ==')
    for rel in OLD_DIRS:
        p = os.path.join(D4, rel)
        ok(not os.path.exists(p), '旧路径仍存在: %s' % p)
    for rel in OLD_PARENTS:
        p = os.path.join(D4, rel)
        ok(not os.path.isdir(p) or count_files(p) == 0,
           '旧父目录下还有文件: %s（%d 个）' % (p, count_files(p)))

    print('== 2) 新路径 + 文件数 ==')
    for rel, want in NEW_DIRS.items():
        p = os.path.join(D4, rel)
        got = count_files(p) if os.path.isdir(p) else -1
        ok(got == want, '%s：文件数 %d（期望 %d）' % (rel, got, want))

    print('== 3) 代码侧引用 ==')
    code_root = f'{D4}/MyGame/Assets'
    code_files = list(iter_files(code_root, ('.cs', '.unity')))
    code_hits = scan_refs(code_files)
    ok(not code_hits, '代码侧仍有 %d 处旧路径引用（前 5 条见下）' % len(code_hits))
    for p, i, line in code_hits[:5]:
        print('        %s:%d: %s' % (p, i, line[:200]))

    print('== 4) 文档/工具侧引用（豁免：任务线 / 历史） ==')
    doc_files = list(iter_files(f'{D4}/资料', ('.md',))) + \
                list(iter_files(f'{D4}/工具', ('.py',)))
    doc_hits = scan_refs(doc_files, skip_dirs=('资料/任务线/', '资料/历史/'))
    ok(not doc_hits, '文档/工具侧仍有 %d 处未标归档的旧路径引用（前 10 条见下）' % len(doc_hits))
    for p, i, line in doc_hits[:10]:
        print('        %s:%d: %s' % (p.replace('\\', '/'), i, line[:220]))

    print('== 5) 工具落点 ==')
    sync_p = f'{D4}/工具/sync_from_d2.py'
    conv_p = f'{D4}/工具/scripts快照/convert_unity_particles.py'
    sync_txt = open(sync_p, encoding='utf-8').read()
    conv_txt = open(conv_p, encoding='utf-8').read()
    ok("f'{D4}/数据/粒子/" not in sync_txt,
       'sync_from_d2.py 仍把粒子播种回旧路径（f-string 落点）')
    ok("f'{D4}/数据/界面布局/ui_layout'" not in sync_txt,
       'sync_from_d2.py 仍把 ui_layout 播种回旧路径（f-string 落点）')
    ok("ARCHIVE = f'{D4}/数据/历史_Godot快照'" in sync_txt,
       'sync_from_d2.py 缺 ARCHIVE 归档落点常量')
    ok("'--force'" in sync_txt and 'force=args.force' in sync_txt,
       'sync_from_d2.py 缺 --force')
    ok('_assert_not_writing_into_source' in sync_txt,
       'sync_from_d2.py 缺「源侧只读」兜底断言')
    ok("'--out'" in conv_txt, 'convert_unity_particles.py 缺 --out')
    ok("r'D:\\warpforge\\data\\particles'" not in conv_txt,
       'convert_unity_particles.py 默认落点仍写 d:/warpforge')
    ok('拒绝执行' in conv_txt, 'convert_unity_particles.py 缺「默认拒绝写 d:/warpforge」的出声')

    print()
    if fails:
        print('=== 结束：%d 项检查 / %d 项失败 ===' % (checks, len(fails)))
        return 1
    print('=== 结束：%d/%d 全过 ===' % (checks, checks))
    return 0


if __name__ == '__main__':
    VERBOSE = False
    sys.exit(main())
