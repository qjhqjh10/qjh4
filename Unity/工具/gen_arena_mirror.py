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

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_mirror.py           # 全 13 场\n    ... --check     只报不写\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

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


def mat_mul(A, B):
    return [[sum(A[i][k] * B[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def local_matrix(lp, lq, ls):
    """一级局部 TRS → 4×4（左上 3×3 = R·diag(s)，平移列 = localPosition）。
    四元数是本文件的 (x,y,z,w) 次序。判据 → `资料/完全复刻/判据/T09_清单坐标口径_第4轮.md` §1.2
    （与 `gen_unity_arena_manifest._local_matrix` 同一份算式，只是四元数下标顺序不同）。"""
    px, py, pz = lp
    qx, qy, qz, qw = lq
    sx, sy, sz = ls
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
            # 收集父链（自己 → 根），然后**从根往下**累乘（世界 = 父世界 ∘ 本地）
            # 🔴 2026-09-25 踩过：反着累加（子→父方向）会把父级旋转漏掉 ⇒ 位置算出 y=28.5 这种离谱值。
            # 🔴 2026-10-26（T09）：原来只累乘「旋转 + 平移」、**不乘父级缩放** —— 那是本文件**第三份**
            #    无缩放链（另两份 = `gen_unity_arena_manifest.world_chain` / `gen_arena_groups._world_of`）。
            #    镜面对象在 `…/Floor/reflection camera` 下、`Floor` 世界缩放 **100** ⇒ aeldari / tauviorla
            #    各偏 **3.553**（`ArenaMirror.planePos` 直接用这个值 ⇒ 反射平面摆错）。
            #    改法 = 父世界矩阵的平移列（判据 → `资料/完全复刻/判据/T09_清单坐标口径_第4轮.md` §9·4）。
            chain, cur, seen = [], tid, 0
            while cur and cur in tr and seen < 20:
                t = tr[cur]
                lp = t.get('m_LocalPosition') or {}
                lr = t.get('m_LocalRotation') or {}
                ls = t.get('m_LocalScale') or {}
                chain.append(((lp.get('x', 0.0), lp.get('y', 0.0), lp.get('z', 0.0)),
                              (lr.get('x', 0.0), lr.get('y', 0.0), lr.get('z', 0.0), lr.get('w', 1.0)),
                              (ls.get('x', 1.0), ls.get('y', 1.0), ls.get('z', 1.0))))
                cur = (t.get('m_Father') or {}).get('m_PathID', 0)
                seen += 1
            M = [[1.0 if i == j else 0.0 for j in range(4)] for i in range(4)]
            rot = (0.0, 0.0, 0.0, 1.0)
            for local, lq, ls in reversed(chain):
                M = mat_mul(M, local_matrix(local, lq, ls))
                rot = qmul(rot, lq)
            pos = (M[0][3], M[1][3], M[2][3])
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
            # 🆕 2026-09-26：**Blit 那两个材质要喂的四个数，就在这个组件的序列化字段里** ——
            #    判据（唯一）：`decomp_full/kTools.Mirrors.Mirror__.ctor.c:22-31` 把
            #    `_DepthFadeBottom`/`_DepthFadeTop`/`_Iterations`/`_BlurRadius` 四个 **PropertyToID**
            #    存进 +0xbc/+0xc0/+0xc8/+0xcc，而 `__ExecuteCommand.c:25,27,81,84` 正是拿这四个 id 去
            #    `Material.SetFloat(id, 值)` —— **值就是下面这四个字段**（反编译器把值那个实参丢了，
            #    害得一度只能靠猜；**序列化字段才是权威**）。
            #    ⚠️ **别把 `m_blurIterations`（=3，喂给 shader 的 `_Iterations`）与档位里的
            #    `blurIterations`（=1，喂给 C# 那一层的循环次数）搞混** —— 两个字段、两回事。
            #    🔴 顺带结掉一条悬案：`m_Scope = 0` + `m_Renderers = []` ⇒
            #    **原版不限制 renderer 范围**（那个「原版可能只渲一部分 renderer」的候选**不成立**）。
            out = {
                'scene': arena,
                'go': name,
                'quality': QUALITY,
                'enabled': bool(preset.get('enabled', 0)),
                'grainy': bool(preset.get('grainyReflections', 0)),
                'textureScale': round(float(preset.get('textureScale', 0.0)), 4),
                'blurIterations': int(preset.get('blurIterations', 0)),
                # 📌 两个材质要的四个数（见上）—— 逐场读，**别写成常量**
                'depthFadeBottom': float(d.get('m_depthFadeBottom', 0.0)),
                'depthFadeTop': float(d.get('m_depthFadeTop', 0.0)),
                'shaderIterations': int(d.get('m_blurIterations', 0)),
                'blurRadius': float(d.get('m_blurRadius', 0.0)),
                'scope': int(d.get('m_Scope', 0)),
                'rendererCount': len(d.get('m_Renderers') or []),
                'pos': [round(v, 5) for v in pos],
                'normal': [round(v, 5) for v in nrm],
            }
            print('[%s] %s · 镜面 pos=%s normal=%s · textureScale=%s blur=%s grainy=%s · '
                  'depthFade=%s..%s · shaderIter=%s radius=%s · scope=%s renderers=%s' % (
                      arena, name, out['pos'], out['normal'], out['textureScale'], out['blurIterations'],
                      out['grainy'], out['depthFadeBottom'], out['depthFadeTop'],
                      out['shaderIterations'], out['blurRadius'], out['scope'], out['rendererCount']))
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
