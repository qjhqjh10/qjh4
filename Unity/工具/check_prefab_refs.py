#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""check_prefab_refs.py —— 只读检查 prefab / scene 里「指不到资产」的对象引用（`A1325`/`A1298`/`A1299` 的自检口）。

## 这是什么 / 为什么需要

`EffectExporter` 把 bundle 里的原版 prefab 导成工程 prefab 时，**bundle 里的图 / 字体 / 脚本
不落在工程里** ⇒ 序列化出来的引用会写成 `guid: 00000000000000000000000000000000`
（= 「这个引用指着某个资产，但那个资产在工程里不存在」）。
`A1298`/`A1299` 修的是 `m_Script`，`A1325` 修的是 uGUI `Image.m_Sprite`
与 TMP `m_fontAsset` / `m_sharedMaterial`（对照实现 → `MyGame/Assets/WarpforgeArena1/Editor/BoosterPackExporter.cs:512` / `:563`）。

🔴 **2026-10-21 起默认集补上 `m_Mesh` / `m_Texture`**（`A1325` 的 `ShapeModule` 那一跳，
实现 → `EffectExporter.cs:1912-2055`）。补之前**不传 `--fields` 扫特效 prefab 是假绿的**：
`m_Mesh` 坏引用全仓现读 **47 条 / 8 件**（2026-10-21 23:2x 复扫）——
**40 条 / 7 件在 `ParticleSystem`（`ShapeModule.m_Mesh`）**，即 `W61b_特效_ShapeModule导出.md` §2.1 那一批
（那份报告写 45 条 / 12 件是**补前**口径，其中 5 条 Shield 变体已在 23:18 的重导里归零）；
**另 7 条在 `SkinnedMeshRenderer.m_Mesh`**（`Explosion Hive Fleet Arrival Explosions only.prefab`，
那份报告没数 —— 它只扫 ShapeModule）。⚠️ `m_Texture` 补时现读 **2 条 / 2 件**（`Buff_Sororitas_quick_fire*`，
全是 ShapeModule），**23:18 的重导后已归零**（2026-10-21 23:30 复扫 = 0）。
⚠️ **`m_Material` 仍然不在默认集里**（`A1325` 的 `Image.m_Material` 那一跳要显式
`--fields …,m_Material`，交接文档里一直这么用）。

🔴 **判据不能数「`fileID: 0`」** —— 那是**合法的空引用**（原版本来就空的 `Image` / 没挂脚本的节点），
一数就会得到几十条假警报（本工具 2026-10-21 第一次写时就差点按那个口径报「`m_Sprite` 坏 32 条」）。
**坏引用 = `fileID != 0` 且 `guid` 全 0**：文件那头说「我要这张图」，工程里却没有这张图。

## 用法

    python -I -X utf8 d:/4/Unity/工具/check_prefab_refs.py                 # 默认查 CardPrefab.prefab
    python -I -X utf8 d:/4/Unity/工具/check_prefab_refs.py <a.prefab> [<b.unity> ...]
    python -I -X utf8 d:/4/Unity/工具/check_prefab_refs.py --fields m_Script,m_Sprite,m_fontAsset ...
    python -I -X utf8 d:/4/Unity/工具/check_prefab_refs.py --fields m_Material,m_Mesh     # 显式覆盖仍有效（只查列出来的）

输出：每个文件 × 每个字段一行 `total / 合法空(fileID=0) / 本地引用(无 guid) / 坏引用(guid 全 0)`，
坏引用再逐条列出 `行号 + fileID`（最多 `--max-detail` 条，默认 10）。

⚠️ **本工具只认「工程内 guid」** —— Unity **内置默认资源**（`Cube`/`Quad` 之类）在序列化里
也是 `guid: 0000…0` + 一个小 fileID，形状与坏引用**完全一样**。2026-10-21 全仓实扫
（984 个 `.prefab` + 全部 `.unity`）**没有这种内建形状的 `m_Mesh`/`m_Texture` 引用**
⇒ 这一版**不做大小 fileID 的形状排除**（按 fileID 大小切是猜，会把真坏引用也放过）。
将来若真扫出内建引用，做法是**在导出器里把它解析成工程资产**（`EffectExporter` 的 `IsUnityBuiltinMesh` 那条路），
⛔ 不是在本工具里静默跳过。

退出码：`0` = 所有被查字段的坏引用 = 0 · `1` = 有坏引用 · `2` = 文件不存在 / 读不了。
⛔ 只读：不改工程、不跑 Unity、不联网。
"""
import argparse
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))      # d:/4/Unity
DEFAULT_TARGETS = [os.path.join(ROOT, 'MyGame/Assets/WarpforgeVFX/Prefabs/CardPrefab.prefab')]
DEFAULT_FIELDS = ['m_Script', 'm_Sprite', 'm_fontAsset', 'm_sharedMaterial',
                  'm_Mesh', 'm_Texture']   # 2026-10-21 补后两个（ShapeModule 资产引用，见文件头）
ZERO_GUID = '0' * 32

# `m_Sprite: {fileID: -123, guid: <32hex>, type: 3}` —— 也容忍字段顺序变化与缺 guid。
_REF = re.compile(r'^(?P<indent>\s*)(?P<field>[A-Za-z_][A-Za-z0-9_]*):\s*\{fileID:\s*(?P<fid>-?\d+)'
                  r'(?P<rest>[^}]*)\}', re.M)
_GUID = re.compile(r'guid:\s*([0-9a-fA-F]{32})')


def scan(path, fields):
    """返回 {field: {'total','empty','local','broken','lines':[(lineno, fid)]}}。只读。"""
    text = io.open(path, encoding='utf-8', errors='replace').read()
    want = set(fields)
    out = {f: {'total': 0, 'empty': 0, 'local': 0, 'broken': 0, 'lines': []} for f in fields}
    for m in _REF.finditer(text):
        f = m.group('field')
        if f not in want:
            continue
        d = out[f]
        d['total'] += 1
        fid = int(m.group('fid'))
        if fid == 0:
            d['empty'] += 1
            continue
        g = _GUID.search(m.group('rest') or '')
        if g is None:
            # 没有 guid 字段 = 指向**本文件内**另一个对象（Transform/GO 之类），不是外部资产引用。
            d['local'] += 1
            continue
        if g.group(1).lower() == ZERO_GUID:
            d['broken'] += 1
            lineno = text.count('\n', 0, m.start()) + 1
            d['lines'].append((lineno, fid))
        # 否则：guid 非 0 ⇒ 指到工程里某个资产（**是否真存在**不在本工具范围 —— 那要扫 .meta）
    return out


def main(argv=None):
    ap = argparse.ArgumentParser(description='只读检查 prefab/scene 里 guid 全 0 的对象引用')
    ap.add_argument('targets', nargs='*', default=None,
                    help='要查的 .prefab / .unity（默认 = CardPrefab.prefab）')
    ap.add_argument('--fields', default=','.join(DEFAULT_FIELDS),
                    help='要查的序列化字段名，逗号分隔（默认 %(default)s）')
    ap.add_argument('--max-detail', type=int, default=10, help='每个字段最多列几条坏引用（默认 10）')
    args = ap.parse_args(argv)

    targets = args.targets or DEFAULT_TARGETS
    fields = [x.strip() for x in args.fields.split(',') if x.strip()]
    rc = 0
    for path in targets:
        if not os.path.isfile(path):
            sys.stdout.write('🔴 文件不存在：%s\n' % path)
            rc = 2
            continue
        sys.stdout.write('== %s（%d B）\n' % (path, os.path.getsize(path)))
        try:
            got = scan(path, fields)
        except OSError as e:
            sys.stdout.write('  🔴 读不了：%s: %s\n' % (type(e).__name__, e))
            rc = 2
            continue
        for f in fields:
            d = got[f]
            if d['total'] == 0:
                sys.stdout.write('  %-18s 本文件里没有这个字段\n' % f)
                continue
            mark = '✅' if d['broken'] == 0 else '🔴'
            sys.stdout.write('  %s %-16s 总数 %-4d 合法空(fileID=0) %-4d 本地引用 %-4d **坏引用 %d**\n'
                             % (mark, f, d['total'], d['empty'], d['local'], d['broken']))
            if d['broken']:
                rc = 1
                for lineno, fid in d['lines'][:max(0, args.max_detail)]:
                    sys.stdout.write('       行 %-6d fileID %s\n' % (lineno, fid))
                if len(d['lines']) > args.max_detail:
                    sys.stdout.write('       … 另有 %d 条（--max-detail 调大）\n'
                                     % (len(d['lines']) - args.max_detail))
    if rc == 0:
        sys.stdout.write('CHECK: ✅ 被查字段里没有「guid 全 0 且 fileID≠0」的坏引用\n')
    elif rc == 1:
        sys.stdout.write('CHECK: 🔴 有坏引用 —— 导出器补齐那一跳后**重建该 prefab**才会归零'
                         '（`A1325` 的修法在 `EffectExporter.cs`，验证要 Unity 跑一次导出）\n')
    return rc


if __name__ == '__main__':
    sys.exit(main())
