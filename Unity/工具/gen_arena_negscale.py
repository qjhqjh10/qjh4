#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_arena_negscale.py — 把**原版场景里「镜像（负缩放）」的对象**抽出来，写成旁挂 JSON。

为什么要有它（2026-09-28 查明，判据见下）：
    `gen_unity_arena_manifest.py` 的 `world_scale()` 返回的是**世界矩阵的列长**
    （`sqrt(Σ M[i][j]²)`）—— **按构造就永远是正数**，于是**负缩放的符号被静默吃掉**。
    而那个文件自己的文件头写着「pos/rot/scale = world chain 结算，
    **不做任何手性/镜像转换**」⇒ **说法与实现对不上，是它这一处丢了信息**。

    后果**不是「位置偏一点」**：负行列式 = 该对象**镜像**，Unity 下会连带把**三角形绕序翻过来**
    ⇒ 在 `Cull Back` 的材质上**该藏的那一面被画出来、该画的那一面被剔掉**。
    实测（`battlearenaleviathan`，`Toxic Pool Glow` ×2，原版 `m_LocalScale = (1, −1, 1)`）：
      · 清单里写成 `(1, 1, 1)` ⇒ 那颗网格把**整屏罩成一片亮黄绿**，
        亮度比 **1.065**（>±5%），逐块差在 y=160 一条横带上 **+71~+107**；
      · 把 Y 改回 −1（重跑该场）：**1.065 → 1.023**，那条横带整条消失。
    ⇒ 这是**还原度**的账，而且是「原版有、我们画错」那一类。

为什么不改 `gen_unity_arena_manifest.py`：
    那是 `d:/2/Warpforge_tools/scripts/` 下的生成器（**档案库**，动它要单独确认）；
    而本工程早有一套既成做法 = **旁挂数据 + `工具/gen_arena_*.py`**
    （非主贴图槽 / 精灵 / 平面反射 / 画质档开关 / 粒子网格 / 相机 `sensorSize`，全走这条）⇒ 照办。

判据（这一份工具自己的口径，别在别处再写一套）：
    · **世界矩阵** 按 `m_Father` 链逐级左乘本地矩阵（与生成器 `_local_matrix` 同一套算式）；
    · **符号** = 链上**逐分量相乘**的本地缩放（`sx·sx·…`, `sy·sy·…`, `sz·sz·…`）
      —— 链上只要有**非轴对齐的旋转**，这个乘积就与原版的 `lossyScale` 不再一一对应；
    · **自检**：签名的**幅值**必须与清单里的 `scale`（= 生成器给的列长）**逐轴吻合到 1% 以内**，
      否则**跳过该对象并在日志里出声**（宁可漏一个，也不写一个猜出来的符号）。

用法：
    python d:/4/Unity/工具/gen_arena_negscale.py                  # 13 场全写
    python d:/4/Unity/工具/gen_arena_negscale.py --arena battlearenaleviathan
    python d:/4/Unity/工具/gen_arena_negscale.py --check           # 只扫不写

产物：`<工程>/WarpforgeArena1/arenas/<场>/<场>_negscale.json`
      `{ "arena": "<场>", "items": [ { "go": "<对象名>", "pos": [x,y,z], "scale": [sx,sy,sz] } ] }`
      只列**至少有一个分量为负**的对象；没有就是空 items（正常场次）。
"""
import argparse
import io
import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8', errors='replace')

ASSETS = 'd:/2/新解包资源/assets_full'
ARENAS_ROOT = 'd:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas'
ALL_ARENAS = [
    'battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
    'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
    'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
    'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla',
]


def bundle_dir(arena):
    return f'{ASSETS}/bundle_scenes_scenes_{arena}'


def load(path):
    try:
        return json.load(io.open(path, encoding='utf-8'))
    except Exception:
        return None


def _local_matrix(px, py, pz, q, s):
    """与 `gen_unity_arena_manifest.py:_local_matrix` 同一套算式（列主序、缩放乘在列上）。"""
    qx, qy, qz, qw = q
    sx, sy, sz = s
    x2, y2, z2 = qx + qx, qy + qy, qz + qz
    xx, xy, xz = qx * x2, qx * y2, qx * z2
    yy, yz, zz = qy * y2, qy * z2, qz * z2
    wx, wy, wz = qw * x2, qw * y2, qw * z2
    r = [[1 - (yy + zz), xy - wz, xz + wy, 0.0],
         [xy + wz, 1 - (xx + zz), yz - wx, 0.0],
         [xz - wy, yz + wx, 1 - (xx + yy), 0.0],
         [0.0, 0.0, 0.0, 1.0]]
    for i in range(3):
        r[i][0] *= sx
        r[i][1] *= sy
        r[i][2] *= sz
    r[0][3], r[1][3], r[2][3] = px, py, pz
    return r


def _mul(A, B):
    return [[sum(A[i][k] * B[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


class Scene(object):
    def __init__(self, bdir):
        self.bdir = bdir
        self.tf = {}          # transform pid(str) -> Transform json
        self.tr_of_go = {}    # GameObject pid(str) -> transform pid(str)
        self.name_of_go = {}  # GameObject pid(str) -> 名字
        for fn in os.listdir(os.path.join(bdir, 'Transform')):
            if not fn.endswith('.json'):
                continue
            d = load(os.path.join(bdir, 'Transform', fn))
            if d is None:
                continue
            self.tf[fn[len('Transform_'):-len('.json')]] = d
        go_dir = os.path.join(bdir, 'GameObject')
        for fn in os.listdir(go_dir):
            if not fn.endswith('.json'):
                continue
            g = load(os.path.join(go_dir, fn))
            if g is None:
                continue
            gpid = fn[len('GameObject_'):-len('.json')]
            self.name_of_go[gpid] = g.get('m_Name')
            for c in g.get('m_Component', []):
                tp = str(c['component']['m_PathID'])
                if tp in self.tf:                 # 🔴 Transform 的 pid 与 GameObject 的 pid 不同：
                    self.tr_of_go[gpid] = tp      #    这里按「组件 pid 恰好是该 GO 的 Transform」认领
                    break

    def locals_of(self, tpid):
        """沿 `m_Father` 上溯，返回 [根…自身] 的 (pos, rot, scale)。"""
        chain = []
        cur = str(tpid)
        guard = 0
        while cur in self.tf and guard < 200:
            t = self.tf[cur]
            lp = t.get('m_LocalPosition') or {}
            lq = t.get('m_LocalRotation') or {}
            ls = t.get('m_LocalScale') or {}
            chain.append((
                (lp.get('x', 0.0), lp.get('y', 0.0), lp.get('z', 0.0)),
                (lq.get('x', 0.0), lq.get('y', 0.0), lq.get('z', 0.0), lq.get('w', 1.0)),
                (ls.get('x', 1.0), ls.get('y', 1.0), ls.get('z', 1.0)),
            ))
            nxt = (t.get('m_Father') or {}).get('m_PathID')
            if nxt in (None, 0):
                break
            cur = str(nxt)
            guard += 1
        chain.reverse()
        return chain

    def world_of(self, tpid):
        """→ `(世界位置, 世界矩阵 M 的 3×3, det)`（M = 沿 `m_Father` 链逐级左乘本地矩阵）。"""
        M4 = [[1.0 if i == j else 0.0 for j in range(4)] for i in range(4)]
        for (p, q, s) in self.locals_of(tpid):
            M4 = _mul(M4, _local_matrix(p[0], p[1], p[2], q, s))
        M = [[M4[i][j] for j in range(3)] for i in range(3)]
        det = (M[0][0] * (M[1][1] * M[2][2] - M[1][2] * M[2][1])
               - M[0][1] * (M[1][0] * M[2][2] - M[1][2] * M[2][0])
               + M[0][2] * (M[1][0] * M[2][1] - M[1][1] * M[2][0]))
        return (M4[0][3], M4[1][3], M4[2][3]), M, det

    def by_name(self, want):
        """所有 GameObject 名 == want 的 Transform pid。"""
        return [self.tr_of_go[g] for g, n in self.name_of_go.items()
                if n == want and g in self.tr_of_go]


def signed_scale_from_entry(world_M, rot):
    """**从原版世界矩阵反解**出「清单里的 rot 不变时应设的 localScale」。

    `ArenaBuilder` 把清单的 `pos/rot/scale` **当世界值直接写进一个新建的根节点**
    （父链恒等）⇒ 我们要解的正是：给定 `R_q`（清单的 rot 转成的旋转）与 `M`（原版世界矩阵），
    求 `D` 使 `R_q · D = M` ⇒ **`D = R_qᵀ · M`**。只有当 `D` 是对角阵时，它才是「纯缩放」。

    🔴 **别拿「世界矩阵归一化后的列」去判符号**（本工具第一版就是这么写的，在 `battlearena3` 上翻车）：
    那个归一化矩阵 `R_norm = R_q · D / |D|` 在**镜像**时满足 `R_norm = -R_q`（det = −1），
    与 `R_q` **差一个整体负号** —— 于是「按它的列去映射轴」会把符号安到**错的轴**上，
    等价于给原版多转 180°。实测：`battlearena3` 那 4 个镜像对象照第一版写 ⇒ 该场亮度比 **1.010 → 1.214**（变差）。

    返回 `(scale, err)`；`err` 非空表示 `D` 不是对角阵（链上有非轴对齐的东西）⇒ 调用方跳过。
    """
    Rq = _local_matrix(0.0, 0.0, 0.0, rot, (1.0, 1.0, 1.0))
    # D = Rqᵀ · M（Rq 是正交阵 ⇒ 转置即逆）；`_local_matrix` 给的是列主序 4×4
    D = [[sum(Rq[k][i] * world_M[k][j] for k in range(3)) for j in range(3)] for i in range(3)]
    diag = [D[i][i] for i in range(3)]
    mx = max(abs(v) for v in diag) or 1.0
    for i in range(3):
        for j in range(3):
            if i != j and abs(D[i][j]) > 0.02 * mx:
                return None, f'解出来的缩放不是对角阵（off-diag[{i}][{j}]={D[i][j]:.4f}）'
    return diag, None


def entries_of(manifest):
    """清单里要处理的**网格**对象。

    🔴 **只做 `meshes`，不做 `particles`**（2026-09-28 实测定下来的，别顺手放开）：
    这一条修的是「**负行列式把三角形绕序翻过来 ⇒ `Cull Back` 材质上画错面**」——
    那是**网格渲染**的现象，而且 `leviathan` 的判据对象正是一颗网格。
    把**粒子**也一起改时实测**变差**：`battlearena3` 那 4 条（2 网格 + 2 粒子
    `Monolith Glow`/`(1)`）全开 ⇒ 该场亮度比 **1.010 → 1.214**；
    ⇒ 粒子的负缩放**另有机制（发射形状 / billboard 朝向），本工具判据不足，先不做**。
    """
    out = []
    for e in manifest.get('meshes') or []:
        if e.get('go') and e.get('pos') and e.get('scale'):
            out.append(e)
    return out


def scan_arena(arena):
    """→ (items, warnings)；items 只含**有负分量**的对象。"""
    bdir = bundle_dir(arena)
    mpath = f'{ARENAS_ROOT}/{arena}/{arena}_manifest.json'
    if not os.path.isdir(bdir):
        return None, [f'没有包目录 {bdir}']
    if not os.path.exists(mpath):
        return None, [f'没有清单 {mpath}']
    sc = Scene(bdir)
    mf = load(mpath)
    items, warns, seen = [], [], set()
    n_mirror, n_gap = 0, 0
    for e in entries_of(mf):
        name = e['go']
        want = tuple(round(float(v), 3) for v in e['pos'])
        cands = sc.by_name(name)
        if not cands:
            continue
        pick = None
        for tpid in cands:
            pos, M, det = sc.world_of(tpid)
            if max(abs(pos[i] - want[i]) for i in range(3)) < 0.02:
                pick = (M, det)
                break
        if pick is None:
            # 位置对不上（粒子那批的清单坐标与场景链算出来的不一致）——
            # **退路**：同名候选里**每一个镜像的**都给出同一个签名的世界缩放时才采用。
            outs = []
            for tpid in cands:
                _pos, M, det = sc.world_of(tpid)
                if det >= 0:
                    continue
                s, err = signed_scale_from_entry(M, e['rot'])
                outs.append(None if s is None else tuple(round(v, 4) for v in s))
            if not outs:
                continue
            uniq = set(o for o in outs if o is not None)
            if len(uniq) == 1 and None not in outs:
                signed = list(next(iter(uniq)))
                n_mirror += 1
                key = (name, want)
                if key not in seen:
                    seen.add(key)
                    items.append({'go': name, 'pos': [round(want[0], 3), round(want[1], 3), round(want[2], 3)],
                                  'scale': [round(v, 6) for v in signed]})
                continue
            n_gap += 1
            warns.append(f'{name} @{want}: **镜像**（det<0）且位置对不上清单 ⇒ 判不出符号 ⇒ 跳过（缺口）')
            continue
        M, det = pick
        if det >= 0:
            continue                      # 没镜像 —— 正常，静音
        n_mirror += 1
        signed, err = signed_scale_from_entry(M, e['rot'])
        if signed is None:
            n_gap += 1
            warns.append(f'{name} @{want}: **镜像**（det<0）但 {err} ⇒ 跳过（缺口）')
            continue
        # 自检：幅值必须与清单里的列长逐轴吻合（否则解错了）
        bad = None
        for i in range(3):
            m = float(e['scale'][i])
            if m > 1e-6 and abs(abs(signed[i]) - m) / m > 0.01:
                bad = f'|算出来|={abs(signed[i]):.4f} 与清单 scale[{i}]={m:.4f} 差 >1%'
                break
        if bad:
            n_gap += 1
            warns.append(f'{name} @{want}: {bad} ⇒ 跳过（缺口）')
            continue
        if not any(v < 0 for v in signed):
            n_gap += 1
            warns.append(f'{name} @{want}: det<0 却算不出负分量（口径冲突）⇒ 跳过（缺口）')
            continue
        key = (name, want)
        if key in seen:
            continue
        seen.add(key)
        items.append({'go': name, 'pos': [round(want[0], 3), round(want[1], 3), round(want[2], 3)],
                      'scale': [round(v, 6) for v in signed]})
    return items, warns, n_mirror, n_gap


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena')
    ap.add_argument('--check', action='store_true', help='只扫不写')
    a = ap.parse_args()

    arenas = [a.arena] if a.arena else ALL_ARENAS
    total, gtotal, mtotal = 0, 0, 0
    for arena in arenas:
        r = scan_arena(arena)
        if r[0] is None:
            print(f'✗ {arena}: ' + '; '.join(r[1]))
            continue
        items, warns, n_repl, n_gap = r
        total += len(items)
        mtotal += n_repl
        gtotal += n_gap
        print(f'{"=" if items else "-"} {arena}: 写 {len(items)} 条'
              + (f'（本场镜像对象共 {n_repl} 个，其中 {n_gap} 个判不出 ⇒ 缺口）' if n_repl else ''))
        for it in items:
            print(f'      {it["go"]:<40} scale={it["scale"]}')
        for w in warns:
            print(f'      ⚠️ {w}')
        if not a.check:
            p = f'{ARENAS_ROOT}/{arena}/{arena}_negscale.json'
            # 🔴 行尾：这一份是**新产物**，统一 LF（工程里既有文件是 LF / CRLF 混的）。
            io.open(p, 'w', encoding='utf-8', newline='\n').write(
                json.dumps({'arena': arena, 'items': items}, ensure_ascii=False, indent=1))
            print(f'      → {p}')
    print(f'=== 13 场：写出 {total} 条 · 场上镜像对象共 {mtotal} 个 · 判不出 {gtotal} 个 ===')


if __name__ == '__main__':
    main()
