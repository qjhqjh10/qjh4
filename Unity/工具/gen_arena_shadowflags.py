#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_arena_shadowflags.py —— 逐对象的**阴影标志**旁挂（`<场>_shadowflags.json`）
================================================================================
为什么：
  `ArenaBuilder.ApplyShadowFlags` 原来**两处都硬写死 `Off / false`**，而原版
  `battlearenablacklegion` 的 67 个 `MeshRenderer` 里 **52 个 `m_CastShadows=1` 且 `m_ReceiveShadows=1`**
  （其余 15 个是 0/0）⇒ 我们关掉投/收之后阴影贴图是空的、地板全亮
  （实测该场前景比原版亮 **+29 ~ +37**，是它亮度比 1.074 的大头）。
  ⇒ **原版不是全场统一**（52 开 / 15 关），一刀切开也会错 ⇒ 必须**逐对象**搬。
  判据与实测 → `MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs:3002-3024` ·
  `资料/战场13场_逐场对账_0920.md` §一 ①-l。

产物：`MyGame/Assets/WarpforgeArena1/arenas/<场>/<场>_shadowflags.json`
  `{ "arena": "<场>", "items": [ {"go": 名, "pos": [世界坐标], "cast": 0/1, "receive": 0/1}, … ] }`
  —— C# 侧按 **名字 + 世界位置** 匹配（与 `_negscale.json` 同一套 `SameObject`）。

⚠️ **只报清单里有的对象**（我们建了什么就记什么）；位置与名字都对不上的**出声记缺口**，不猜。

用法：
  PYTHONIOENCODING=utf-8 python 工具/gen_arena_shadowflags.py [--arena <场>] [--check]
  （缺省 = 全 13 场；`--check` 只扫不写）

判据是**原版包**（`assets_full/bundle_scenes_scenes_<场>/`）：
· `MeshRenderer/*.json` 的 `m_CastShadows` / `m_ReceiveShadows`
· GameObject 名 → 组件 pid → MeshRenderer（**dump 的 GameObject 是按资产名命名的**，
  所以不能用 `fn[len('GameObject_'):]` 去切 pid —— 那是 `gen_arena_negscale.py` 2026-09-30 修过的坑）
"""
import io, os, re, json, glob, sys
from collections import defaultdict

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_arena_negscale as G   # 复用它的 Scene（世界矩阵 / 名字索引，判据只留一处）

ARENAS_ROOT = G.ARENAS_ROOT
ALL_ARENAS = G.ALL_ARENAS


def load_dir(bdir, kind):
    out = {}
    for f in glob.glob(os.path.join(bdir, kind, '%s_*.json' % kind)):
        m = re.match(r'^%s_(\d+)$' % kind, os.path.basename(f)[:-5])
        if m:
            v = G.load(f)
            if v is not None:
                out[int(m.group(1))] = v
    return out


def scan_arena(arena):
    bdir = G.bundle_dir(arena)
    mpath = f'{ARENAS_ROOT}/{arena}/{arena}_manifest.json'
    if not os.path.isdir(bdir):
        return None, [f'没有包目录 {bdir}']
    if not os.path.exists(mpath):
        return None, [f'没有清单 {mpath}']
    sc = G.Scene(bdir)                      # 名字 → Transform pid（判据共用）
    # 🔴 **`MeshRenderer` 与 `SkinnedMeshRenderer` 都要收**（2026-09-30 实测：blacklegion 的
    #    `Chain1` / `Chain2` 是**蒙皮网格**（`SkinnedMeshRenderer_1682`，cast=1/receive=1），
    #    只扫 `MeshRenderer/` 会把它们记成「原版里找不到同名 MeshRenderer」的假缺口）。
    mrs = {}
    mrs.update(load_dir(bdir, 'MeshRenderer'))
    mrs.update(load_dir(bdir, 'SkinnedMeshRenderer'))
    gos = {}
    for f in glob.glob(os.path.join(bdir, 'GameObject', '*.json')):
        g = G.load(f)
        if g is None:
            continue
        gos[f] = g

    # GO 名 → [(transform pid, cast, receive)]（同名可能多份 ⇒ 按位置挑，见下）
    name2flags = defaultdict(list)
    for fn, g in gos.items():
        comps = []
        for c in (g.get('m_Component') or []):
            cc = c.get('component', c) if isinstance(c, dict) else c
            if isinstance(cc, dict):
                comps.append(cc.get('m_PathID'))
        mrp = next((p for p in comps if p in mrs), None)
        if mrp is None:
            continue
        tpid = None
        for c in comps:
            if str(c) in sc.tf:
                tpid = c
                break
        if tpid is None:
            continue
        mr = mrs[mrp]
        name2flags[g.get('m_Name')].append((tpid,
                                           int(mr.get('m_CastShadows', 0) or 0),
                                           1 if mr.get('m_ReceiveShadows') else 0))

    mf = G.load(mpath)
    items, warns = [], []
    for e in mf.get('meshes') or []:
        if not (e.get('go') and e.get('pos')):
            continue
        want = tuple(round(float(v), 3) for v in e['pos'])
        cands = name2flags.get(e['go']) or []
        if not cands:
            warns.append(f"{e['go']}：原版里找不到同名 MeshRenderer ⇒ 不写（缺口）")
            continue
        pick, best = None, None
        for tpid, cast, recv in cands:
            pos, _M, _det = sc.world_of(tpid)
            d = max(abs(pos[i] - want[i]) for i in range(3))
            if best is None or d < best:
                best, pick = d, (cast, recv)
        if best is not None and best > 0.05:
            warns.append(f"{e['go']} @{want}：同名对象的世界位置对不上清单（最近差 {best:.3f}）⇒ 取最近的，可疑")
        items.append({'go': e['go'], 'pos': [round(want[0], 3), round(want[1], 3), round(want[2], 3)],
                      'cast': pick[0], 'receive': pick[1]})
    return items, warns


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    check = '--check' in sys.argv
    arenas = args or ALL_ARENAS
    tot = cast_on = recv_on = gtot = 0
    for a in arenas:
        items, warns = scan_arena(a)
        if items is None:
            print('  🔴 %s: %s' % (a, warns[0]))
            continue
        tot += len(items)
        cast_on += sum(1 for i in items if i['cast'])
        recv_on += sum(1 for i in items if i['receive'])
        gtot += len(warns)
        line = ('= %s: 写 %d 条（cast=1 %d 个 · receive=1 %d 个）'
                % (a, len(items), sum(1 for i in items if i['cast']), sum(1 for i in items if i['receive'])))
        if warns:
            line += ' · ⚠️ %d 条缺口' % len(warns)
        print(line)
        for w in warns[:4]:
            print('      ⚠️ ' + w)
        if not check:
            p = f'{ARENAS_ROOT}/{a}/{a}_shadowflags.json'
            io.open(p, 'w', encoding='utf-8', newline='\n').write(
                json.dumps({'arena': a, 'items': items}, ensure_ascii=False, indent=1))
    print('=== 合计：写 %d 条（cast=1 %d · receive=1 %d）· 缺口 %d ==='
          % (tot, cast_on, recv_on, gtot))


if __name__ == '__main__':
    main()
