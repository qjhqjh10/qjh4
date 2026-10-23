#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""check_arena_dangling_refs.py — 检查 `ArenaOriginalPrefabs` 里「指向工程里不存在的 guid」的引用。

## 与 `check_prefab_refs.py` 的分工（别混）

| 工具 | 查什么 |
|---|---|
| `工具/check_prefab_refs.py` | **guid 全 0 且 fileID≠0**（导出器没解析出来写成的伪引用） |
| **本工具** | **guid 非 0、但工程里没有任何 `.meta` 拥有它**（悬空） |

2026-10-23 的实例：13 场原版 prefab 入 Assets 后，**74 个 guid / 234 处**属于第二类，
而第一类当时是 0 —— 只跑 `check_prefab_refs.py` 会**假绿**。两个都要跑。

## 判据

* guid 索引 = `MyGame/Assets/**` + `MyGame/Library/PackageCache/**` 下所有 `.meta` 的 `guid:`；
* `guid: 0000…0000`（全 0）**单独归类**（那是「空引用」，`check_prefab_refs.py` 管）；
* `guid: 0000000000000000xxxx…`（Unity 内建，前缀 16 个 0）**单独归类**，不算缺口；
* 其余解析不到 = **悬空**。

## 用法

    python -I -X utf8 d:/4/Unity/工具/check_arena_dangling_refs.py
    python -I -X utf8 d:/4/Unity/工具/check_arena_dangling_refs.py --root <目录>
    # 允许「已登记为运行时取件」的悬空（按 closure map 的 status 列）：
    python -I -X utf8 d:/4/Unity/工具/check_arena_dangling_refs.py \\
        --expect MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/_shared/arena_closure_guid_map.tsv \\
        --allow-status runtime-bundle
    python -I -X utf8 .../check_arena_dangling_refs.py --json out.json

退出码：0 = 没有「未被允许」的悬空 · 1 = 有 · 2 = 路径/参数错。
⛔ 只读：不改工程、不跑 Unity、不联网。
"""
import argparse
import collections
import glob
import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))     # d:/4/Unity
DEFAULT_ROOT = os.path.join(ROOT, 'MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs')
ASSETS = os.path.join(ROOT, 'MyGame/Assets')
PKG = os.path.join(ROOT, 'MyGame/Library/PackageCache')
GUID_RE = re.compile(r'guid:\s*([0-9a-fA-F]{32})')
META_GUID_RE = re.compile(r'^guid:\s*([0-9a-fA-F]{32})', re.M)
REF_EXT = ('.prefab', '.mat', '.asset')
SEP = chr(92)


def norm(p):
    return p.replace(SEP, '/')


def walk(root):
    for dp, dns, fns in os.walk(root):
        for fn in fns:
            yield norm(os.path.join(dp, fn))


def meta_guid(path):
    try:
        with io.open(path, 'rb') as f:
            head = f.read(4096)
        m = META_GUID_RE.search(head.decode('utf-8', 'replace'))
        return m.group(1).lower() if m else None
    except OSError:
        return None


def load_expected(path, allow_status):
    """closure map：kind / old_guid / name / refs / arenas / status / resolution（tab 分隔）"""
    exp = {}
    if not path:
        return exp
    lines = io.open(path, encoding='utf-8').read().splitlines()
    if not lines:
        return exp
    hdr = lines[0].split('\t')
    ig = hdr.index('old_guid') if 'old_guid' in hdr else 1
    ist = hdr.index('status') if 'status' in hdr else 5
    for ln in lines[1:]:
        p = ln.split('\t')
        if len(p) <= max(ig, ist):
            continue
        if p[ist].strip() in allow_status:
            exp[p[ig].strip().lower()] = p[ist].strip()
    return exp


def main(argv=None):
    ap = argparse.ArgumentParser(description='只读检查 ArenaOriginalPrefabs 的悬空 guid 引用')
    ap.add_argument('--root', default=DEFAULT_ROOT)
    ap.add_argument('--expect', default='', help='closure map tsv（status 列用来放行已知缺口）')
    ap.add_argument('--allow-status', default='runtime-bundle',
                    help='closure map 里哪些 status 算「已登记」默认 runtime-bundle')
    ap.add_argument('--json', default='', help='把逐条结果写进这个 json')
    args = ap.parse_args(argv)

    root = norm(args.root).rstrip('/')
    if not os.path.isdir(root):
        sys.stdout.write('🔴 目录不存在：%s\n' % root)
        return 2

    proj = set()
    for r in (ASSETS, PKG):
        if not os.path.isdir(r):
            continue
        for p in walk(r):
            if p.endswith('.meta'):
                g = meta_guid(p)
                if g:
                    proj.add(g)

    assets = [p for p in walk(root) if not p.endswith('.meta')]
    ref_files = [p for p in assets if p.endswith(REF_EXT)]
    state = collections.Counter()
    per = collections.defaultdict(lambda: {'pairs': 0, 'occ': 0, 'files': []})
    for p in ref_files:
        text = io.open(p, encoding='utf-8', errors='replace').read()
        seen = set()
        for m in GUID_RE.finditer(text):
            g = m.group(1).lower()
            per[g]['occ'] += 1
            per[g]['files'].append(p[len(root) + 1:])
            seen.add(g)
        for g in seen:
            per[g]['pairs'] += 1

    for g, d in per.items():
        if g == '0' * 32:
            state['zero'] += d['pairs']
        elif g.startswith('0000000000000000'):
            state['builtin'] += d['pairs']
        elif g in proj:
            state['ok'] += d['pairs']
        else:
            state['dangling'] += d['pairs']

    allow = load_expected(norm(args.expect) if args.expect else '', set(
        x.strip() for x in args.allow_status.split(',') if x.strip()))
    dang = {g: d for g, d in per.items()
            if g != '0' * 32 and not g.startswith('0000000000000000') and g not in proj}
    unexpected = {g: d for g, d in dang.items() if g not in allow}

    sys.stdout.write('root = %s\n' % root)
    sys.stdout.write('文件：%d 个资产（其中 %d 个引用文件）· 工程 guid 索引 %d 条\n'
                     % (len(assets), len(ref_files), len(proj)))
    sys.stdout.write('引用对：ok %d · 全0 %d · 内建 %d · **悬空 %d**（%d 个 guid）\n'
                     % (state['ok'], state['zero'], state['builtin'], state['dangling'], len(dang)))
    if allow:
        sys.stdout.write('放行（%s）：%d 个 guid\n' % (args.allow_status, len(dang) - len(unexpected)))
    for g, d in sorted(unexpected.items(), key=lambda x: -x[1]['occ']):
        sys.stdout.write('  🔴 %s  occ=%-2d pairs=%-2d  %s\n'
                         % (g, d['occ'], d['pairs'], d['files'][0]))
    if not unexpected:
        sys.stdout.write('CHECK: ✅ 没有未登记的悬空 guid\n')
    if args.json:
        io.open(args.json, 'w', encoding='utf-8', newline='\n').write(json.dumps(
            {'root': root, 'assets': len(assets), 'ref_files': len(ref_files),
             'state': dict(state), 'allowed': sorted(set(dang) & set(allow)),
             'unexpected': {g: {'occ': d['occ'], 'pairs': d['pairs'], 'files': sorted(set(d['files']))}
                            for g, d in unexpected.items()}},
            ensure_ascii=False, indent=1))
    return 0 if not unexpected else 1


if __name__ == '__main__':
    sys.exit(main())
