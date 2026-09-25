#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""抽出战场里那台**平面反射相机**（`kTools.Mirrors.Mirror`）的镜面几何 → `arenas/<场>/<场>_mirror.json`。

**为什么需要它**：地板材质里有一张 **`_ReflectionMap`**，原版是**运行时**由这台相机渲出来的
（`kTools` 那套用 `CommandBuffer.SetGlobalTexture` 灌**全局贴图**，所以材质属性表里看不到它）。
**我们工程里 `_ReflectionMap` / `reflection camera` 全文 0 命中** ⇒ shader 采到默认贴图
⇒ `battlearena3` 的地板反射偏强（`WF_NOREFL=1` 一比就露：整图比 1.103→1.027）。

实测（2026-09-25）：**13 场里 8 场有这台相机**（arena3 · aeldari · darkangels · emperorschildren ·
genestealers · sororitas · spacewolves · tauviorla），**参数完全相同**：
档 2 = `enabled 1` / `grainyReflections 1` / `textureScale 0.3` / `blurIterations 1`
（注册表 `HKCU\\Software\\Everguild\\Warpforge` 的 `UnityGraphicsQuality_h1669003810` = **2**）。
镜面位置各场不同（都贴地，`y ≈ −0.02`）—— 所以这里逐场把**世界坐标 + 法线**算出来存下。

判据全部来自原始 bundle：`MonoBehaviour`（有 `qualityPresets` 字段的那个就是 Mirror）+ `Transform` 父链。

用法：
    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_mirror.py           # 全 13 场
    ... --check     只报不写
"""
import io
import json
import math
import os
import sys

ROOT = "d:/4/Unity"
AA_DIRS = [
    'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64',
    'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64',
]
ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']
QUALITY = 2                    # 原版实跑档（注册表实读）


def qmul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by,
            aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw,
            aw * bw - ax * bx - ay * by - az * bz)


def qrot(q, v):
    x, y, z, w = q
    vx, vy, vz = v
    # v' = v + 2 * cross(q.xyz, cross(q.xyz, v) + w*v)
    cx = y * vz - z * vy
    cy = z * vx - x * vz
    cz = x * vy - y * vx
    cx += w * vx; cy += w * vy; cz += w * vz
    rx = y * cz - z * cy
    ry = z * cx - x * cz
    rz = x * cy - y * cx
    return (vx + 2 * rx, vy + 2 * ry, vz + 2 * rz)


def qinv(q):
    x, y, z, w = q
    return (-x, -y, -z, w)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    check = '--check' in sys.argv
    arenas = args or ARENAS
    try:
        import UnityPy
    except Exception as e:
        print('UnityPy 不可用:', e)
        return 1
    aa = next((d for d in AA_DIRS if os.path.isdir(d)), None)

    n_hit = 0
    for arena in arenas:
        path = os.path.join(aa, 'scenes_scenes_%s.bundle' % arena)
        if not os.path.isfile(path):
            continue
        env = UnityPy.load(path)
        mb, tr, go = {}, {}, {}
        for o in env.objects:
            try:
                if o.type.name == 'MonoBehaviour':
                    d = o.read_typetree()
                    if 'qualityPresets' in d:
                        mb[o.path_id] = d
                elif o.type.name == 'Transform':
                    tr[o.path_id] = o.read_typetree()
                elif o.type.name == 'GameObject':
                    go[o.path_id] = o.read_typetree()
            except Exception:
                continue
        if not mb:
            continue
        for pid, d in mb.items():
            gid = (d.get('m_GameObject') or {}).get('m_PathID')
            # 找到该 GO 的 Transform，再沿父链累加（位移 + 旋转）
            tid = None
            for k, t in tr.items():
                if (t.get('m_GameObject') or {}).get('m_PathID') == gid:
                    tid = k
                    break
            if tid is None:
                continue
            # 收集父链（自己 → 根），然后**从根往下**累加（世界 = 父世界 ∘ 本地）
            # 🔴 2026-09-25 踩过：反着累加（子→父方向）会把父级旋转漏掉 ⇒ 位置算出 y=28.5 这种离谱值。
            chain, cur, seen = [], tid, 0
            while cur and cur in tr and seen < 20:
                t = tr[cur]
                lp = t.get('m_LocalPosition') or {}
                lr = t.get('m_LocalRotation') or {}
                chain.append(((lp.get('x', 0.0), lp.get('y', 0.0), lp.get('z', 0.0)),
                              (lr.get('x', 0.0), lr.get('y', 0.0), lr.get('z', 0.0), lr.get('w', 1.0))))
                cur = (t.get('m_Father') or {}).get('m_PathID', 0)
                seen += 1
            pos = (0.0, 0.0, 0.0)
            rot = (0.0, 0.0, 0.0, 1.0)
            for local, lq in reversed(chain):
                dp = qrot(rot, local)          # ⚠️ 别用 `d` —— 那会盖掉上面那份 MonoBehaviour
                pos = (pos[0] + dp[0], pos[1] + dp[1], pos[2] + dp[2])
                rot = qmul(rot, lq)
            # 镜面法线 = 这台对象的 **forward**（判据 = 原版 `kTools.Mirrors.Mirror.GetMirrorPlane` 读的就是
            # `Transform.get_forward`，再沿它偏移 `m_Offset`=0.01 —— 见 `decomp_full/kTools.Mirrors.Mirror__GetMirrorPlane.c`）
            fwd = qrot(rot, (0.0, 0.0, 1.0))
            off = 0.01
            pos = (pos[0] + fwd[0] * off, pos[1] + fwd[1] * off, pos[2] + fwd[2] * off)
            nrm = fwd
            ps = d.get('qualityPresets') or []
            preset = next((p for p in ps if p.get('quality') == QUALITY), (ps[2] if len(ps) > 2 else {}))
            name = None
            for k, g in go.items():
                if gid == k:
                    name = g.get('m_Name')
            out = {
                'scene': arena,
                'go': name,
                'quality': QUALITY,
                'enabled': bool(preset.get('enabled', 0)),
                'grainy': bool(preset.get('grainyReflections', 0)),
                'textureScale': round(float(preset.get('textureScale', 0.0)), 4),
                'blurIterations': int(preset.get('blurIterations', 0)),
                'pos': [round(v, 5) for v in pos],
                'normal': [round(v, 5) for v in nrm],
            }
            print('[%s] %s · 镜面 pos=%s normal=%s · textureScale=%s blur=%s grainy=%s' % (
                arena, name, out['pos'], out['normal'], out['textureScale'], out['blurIterations'], out['grainy']))
            n_hit += 1
            if check:
                continue
            dst = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, arena + '_mirror.json')
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            io.open(dst, 'w', encoding='utf-8', newline='\n').write(json.dumps(out, ensure_ascii=False, indent=2))
    print('合计：%d 场有平面反射相机' % n_hit)
    return 0


if __name__ == '__main__':
    sys.exit(main())
