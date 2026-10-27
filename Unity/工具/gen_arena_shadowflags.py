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

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n  PYTHONIOENCODING=utf-8 python 工具/gen_arena_shadowflags.py [--arena <场>] [--check]\n  （缺省 = 全 13 场；`--check` 只扫不写）\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_arena_negscale as G   # 复用它的 Scene（世界矩阵 / 名字索引，判据只留一处）

ARENAS_ROOT = G.ARENAS_ROOT
ALL_ARENAS = G.ALL_ARENAS


## 🆕 2026-10-27（B9_W45ARENA）`--check` 的**产物校验**（只读，不写盘）。
#  原来这一族的 `--check` 是「只扫不写」（`if not check:` 才写）—— 盘上缺产物也恒 rc0，
#  属「看着有 check、其实啥都没查」。口径照 `工具/import_original_art.py:1714-1721`：
#  把**重算出来的内容**与盘上那份**逐字节比**；缺 = 缺产物 · 不一致 = 过期，末尾点名 + rc=1。
def wf_check_artifact(dst, text, lack, stale):
    if not os.path.isfile(dst):
        lack.append(dst)
        return
    try:
        old = io.open(dst, encoding='utf-8').read()
    except Exception as e:
        stale.append((dst, '读不出：%s' % e))
        return
    if old == text:
        return
    try:                     # 只差缩进/键序 ⇒ 语义一致，**不算过期**（`JsonUtility` 本来就不看键序）
        if json.loads(old) == json.loads(text):
            return
    except Exception:
        pass
    stale.append((dst, '内容与重算结果不同'))


def wf_report_artifacts(check, lack, stale, script):
    """`--check` 收尾：有缺/过期就点名并让 main 返回 1。→ 该返回的 rc（无问题 = 0）。"""
    if not check or not (lack or stale):
        return 0
    print('')
    for p in lack:
        print('  🔴 缺产物：%s' % p)
    for p, why in stale:
        print('  🔴 产物过期（%s）：%s' % (why, p))
    print('  ⇒ 重跑一次 `python 工具/%s` 生成（`--check` 只读、不写盘）。' % script)
    return 1


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
    lack, stale = [], []
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
        p = f'{ARENAS_ROOT}/{a}/{a}_shadowflags.json'
        text = json.dumps({'arena': a, 'items': items}, ensure_ascii=False, indent=1)
        if check:
            wf_check_artifact(p, text, lack, stale)
            continue
        io.open(p, 'w', encoding='utf-8', newline='\n').write(text)
    print('=== 合计：写 %d 条（cast=1 %d · receive=1 %d）· 缺口 %d ==='
          % (tot, cast_on, recv_on, gtot))
    return wf_report_artifacts(check, lack, stale, 'gen_arena_shadowflags.py')


if __name__ == '__main__':
    sys.exit(main())
