#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_arena_groups.py —— **A191** 的旁挂：原版那些【分组节点】+ 它们的子树归属。

为什么要它（判据 → `资料/待办判据_1007.md` §A191 · `资料/普查产出_1007/波9离线_A136_A135.md` §3）：
  我们的战场是 `ArenaBuilder` **按清单平铺**建的（`meshes[].go` / `particles[].go` 一个个挂在
  `Warpforge_<场>` 根下），而原版是**有父链的**。原版那 4 个「宿主对象」在我们的平铺结构里
  **根本不存在** ⇒ `ScenarioAnimationBlend.myAnimation` / `TauCannonAnimationStopper.animationComponent`
  / `…animfxController` / `LookAtConstrainWIP.target` 四处**恒解析不到**（旁挂 `_missingTargets` 逐条记着）。

🔴 **为什么非要真当父节点、而不是建个空壳**（本生成器存在的真正理由）：
  原版 `Dark Angels Void Combat animations`（assetGUID `aac3fe87…`）那条 clip 的曲线
  `m_Path` 是 **相对 `Animation` 组件那个 GameObject** 的：
      `Turret 1 barrel` · `Turret 2 barrel` · `Turret missile joint` · `Turret 1 barrel/Lance Fire (5)` · …
  它们**正是 `Battle Arena Dark Angels baked` 的直接子件**（实测，见下面的 `children`）。
  ⇒ 屏幕节点若只是个空壳，这条 clip 一帧都动不了任何东西。

🔴 **本件的范围（有意收窄，如实记着）**：只补 **A191 点名的那几个宿主** + 为安放它们必须建的**祖先链**
  + 这些宿主的**子树**。原版那棵完整的树（`Scenario` 下**全部**对象的分层、`Scenario/Particles/…`
  的嵌套）**这一件没有复原** —— 见报告「没查清 / 新开一条账」。

判据来源（**直读原版 bundle**，不用解包 JSON —— 解包出来的 GameObject 按名命名、pid 丢了）：
  · `<AA>/scenes_scenes_<场>.bundle`（`AA` = `d:/2/.../StandaloneWindows64`）
  · 要补哪些路径**来自旁挂自己**（不手抄）：
      ① `数据/游戏数据/env_blendables.json` 的 `_missingTargets[].wantedPath`
      ② 同一份里 `scene[<场>]` 每条 `kind == "lookat"` 目标的 `fields` 里 `k == "target"` 的 `s`
         （= `LookAtConstrainWIP.target` 指的那个对象；它**不在** `_missingTargets` 里 —— 那张表只查
          blendable 自己的 target、不查「target 组件自己的字段」。判据 → `资料/普查产出_1011/W9_*.md` §五·4）
  · 要补 `Animation` 组件的对象 = 旁挂里**每一个** `kind == "animation"` 的目标
    （原版那两个 `Animation` 组件就挂在它们身上：`Directional Light` · `Battle Arena Dark Angels baked` ·
     `Railgun Turret 1/2`）。

产物：`MyGame/Assets/WarpforgeArena1/arenas/<场>/<场>_groups.json`
  { "scene", "_schema", "_sources", "_stats",
    "nodes": [ {path, name, pos[3], rot[4], scale[3], animation} ],       # 要【新建】的节点（浅→深）
    "targets": [ {name, pos[3], parent, animation} ] }                   # 已建对象：改挂到 `parent`（空 = 不改挂）
  · `pos/rot/scale` 一律 **世界量**（`ArenaBuilder` 建的时候直接 `position/rotation/localScale` 写下去
    —— 清单里的 `pos` 也是世界量，两边同一套坐标，没有第二次换算）。
  · `parent` = **节点自己的原版层级路径**（`nodes[].path` 或一个已建对象的原版路径）。

用法：PYTHONIOENCODING=utf-8 python 工具/gen_arena_groups.py            # 写盘（只写有内容的场）
      PYTHONIOENCODING=utf-8 python 工具/gen_arena_groups.py --check    # 只扫不写
"""
import argparse
import io
import json
import math
import os
import sys

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
import UnityPy

ROOT = 'd:/4/Unity'
AA = 'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
ARENAS = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas')
BLEND = os.path.join(ROOT, '数据/游戏数据/env_blendables.json')


def norm(s):
    """按名比之前先归一化：原版真有带**尾随空格**的名字（`'Railgun Turret 1 Target '`）。
    ⚠️ 这是**比法**上的归一化，不是「把原版名字改好」—— 产物里那份仍是原样留档的判据。"""
    if s is None:
        return ''
    s = s.strip()
    if len(s) >= 2 and s[0] == s[-1] and s[0] in ("'", '"'):
        s = s[1:-1]
    return s.strip()


class Scene(object):
    def __init__(self, arena):
        self.arena = arena
        self.path = os.path.join(AA, 'scenes_scenes_%s.bundle' % arena)
        env = UnityPy.load(self.path)
        self.tt = {}
        for o in env.objects:
            try:
                self.tt[o.path_id] = (o.type.name, o.read_typetree())
            except Exception:
                self.tt[o.path_id] = (o.type.name, None)
        t = self.tt
        self.go = {p: d for p, (k, d) in t.items() if k == 'GameObject' and isinstance(d, dict)}
        self.tr = {p: d for p, (k, d) in t.items() if k == 'Transform' and isinstance(d, dict)}
        self.go2tr = {}
        for p, d in self.tr.items():
            self.go2tr.setdefault(d['m_GameObject']['m_PathID'], []).append(p)
        self.by_name = {}
        for p, d in self.go.items():
            self.by_name.setdefault(d.get('m_Name'), []).append(p)

    def tr_of(self, gopid):
        t = self.go2tr.get(gopid)
        return t[0] if t else None

    def chain(self, gopid):
        """→ [(名字, local_pos[3], local_rot[4](x,y,z,w), local_scale[3]), …] 从**场景根**到它自己。"""
        cur = self.tr_of(gopid)
        out, guard = [], 0
        while cur and guard < 128:
            d = self.tr[cur]
            g = self.go.get(d['m_GameObject']['m_PathID'])
            lp = d.get('m_LocalPosition') or {}
            lq = d.get('m_LocalRotation') or {}
            ls = d.get('m_LocalScale') or {}
            out.append((g.get('m_Name') if g else '?',
                        [float(lp.get('x', 0.0)), float(lp.get('y', 0.0)), float(lp.get('z', 0.0))],
                        [float(lq.get('x', 0.0)), float(lq.get('y', 0.0)), float(lq.get('z', 0.0)), float(lq.get('w', 1.0))],
                        [float(ls.get('x', 1.0)), float(ls.get('y', 1.0)), float(ls.get('z', 1.0))]))
            cur = (d.get('m_Father') or {}).get('m_PathID')
            guard += 1
        out.reverse()
        return out

    def world_of(self, gopid):
        """**与清单同一套**（判据 = `工具/scripts快照/gen_unity_arena_manifest.py:662` 的 `world_chain`）：
        `pos` 沿父链**只累乘旋转与平移、不乘父级缩放**（那个函数就是这么写的 —— 它自己注释
        「坐标: 一律 Unity 原始世界变换」，但 `world_chain` 里没有 scale 这一项）。
        🔴 **必须照抄这个口径**：我们要匹配的是**我们建出来的对象**，而它们的 `localPosition`
          就是清单那个值（父级 = 场根 = identity）⇒ 拿「真·世界坐标」去比会**对不上**
          （实测 `Cylinder.001`：真世界 (84.6697, 4.9387, 56.2087) vs 清单 (83.9002, 4.9602, 55.8322)，
           差 0.86 —— 那是父级 183.7 倍缩放乘在 0.0047 的小偏移上）。
        `scale` 用另一套（真正的 lossyScale，判据 = 同文件 `world_scale`）。
        ⚠️ 建节点时**不用**这个值 —— 用**原版 local**（见 `nodes[]` 的字段），那才是不会有第二套口径的做法。"""
        p = [0.0, 0.0, 0.0]
        q = [1.0, 0.0, 0.0, 0.0]          # w,x,y,z
        m = [[1.0 if i == j else 0.0 for j in range(4)] for i in range(4)]
        for (_n, lp, lq, ls) in self.chain(gopid):
            rp = quat_rot(q, lp)                       # ← 不乘 scale（照 `world_chain`）
            p = [p[0] + rp[0], p[1] + rp[1], p[2] + rp[2]]
            q = quat_mul(q, [lq[3], lq[0], lq[1], lq[2]])
            m = mat_mul(m, local_matrix(lp, lq, ls))
        s = [math.sqrt(sum(m[i][j] ** 2 for i in range(3))) for j in range(3)]
        return p, [q[1], q[2], q[3], q[0]], s

    def local_of(self, gopid):
        """它**自己**那一级的 local TRS（原版值，原样）。"""
        ch = self.chain(gopid)
        return ch[-1][1], ch[-1][2], ch[-1][3]

    def children_of(self, gopid):
        tp = self.tr_of(gopid)
        if tp is None:
            return []
        return [self.tr[p]['m_GameObject']['m_PathID'] for p in self.tr
                if (self.tr[p].get('m_Father') or {}).get('m_PathID') == tp]

    def components(self, gopid):
        out = []
        for c in (self.go.get(gopid, {}).get('m_Component') or []):
            cp = (c.get('component') or {}).get('m_PathID')
            ty, cd = self.tt.get(cp, ('?', None))
            name = ty
            if ty == 'MonoBehaviour' and isinstance(cd, dict):
                name = 'MonoBehaviour(pid=%s)' % ((cd.get('m_Script') or {}).get('m_PathID'))
            out.append(name)
        return out

    def has_animation(self, gopid):
        return 'Animation' in self.components(gopid)

    def subtree(self, gopid):
        """它自己 + 全部后代（先序）。"""
        out, stack = [], [gopid]
        while stack:
            g = stack.pop()
            out.append(g)
            stack.extend(reversed(self.children_of(g)))
        return out


def quat_mul(a, b):          # a,b = (w,x,y,z)
    return (a[0] * b[0] - a[1] * b[1] - a[2] * b[2] - a[3] * b[3],
            a[0] * b[1] + a[1] * b[0] + a[2] * b[3] - a[3] * b[2],
            a[0] * b[2] - a[1] * b[3] + a[2] * b[0] + a[3] * b[1],
            a[0] * b[3] + a[1] * b[2] - a[2] * b[1] + a[3] * b[0])


def quat_rot(q, v):          # q=(w,x,y,z)
    w, x, y, z = q
    t = [2 * (y * v[2] - z * v[1]), 2 * (z * v[0] - x * v[2]), 2 * (x * v[1] - y * v[0])]
    return [v[0] + w * t[0] + (y * t[2] - z * t[1]),
            v[1] + w * t[1] + (z * t[0] - x * t[2]),
            v[2] + w * t[2] + (x * t[1] - y * t[0])]


def mat_mul(A, B):
    return [[sum(A[i][k] * B[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def local_matrix(lp, lq, ls):
    """判据 = `工具/scripts快照/gen_unity_arena_manifest.py:682` 的 `_local_matrix`（逐行照抄）。"""
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


def go_at_path(sc, ps):
    """按**整条层级路径**找一个 GameObject（`A/B/叶子`；同名对象可能有多个，所以必须拿全路径对）。"""
    parts = ps.split('/')
    for g in sc.by_name.get(parts[-1], []):
        if '/'.join(n for n, _, _, _ in sc.chain(g)) == ps:
            return g
    return None


def wanted_paths(blend):
    """要补的路径 —— **全部来自旁挂自己**（不手抄）。"""
    out = {}
    for m in (blend.get('_missingTargets') or []):
        out.setdefault(m['arena'], set()).add(m['wantedPath'])
    for arena, v in (blend.get('scene') or {}).items():
        items = v if isinstance(v, list) else v.get('items', [])
        for it in items:
            for t in (it.get('targets') or []):
                if t.get('kind') != 'lookat':
                    continue
                for f in (t.get('fields') or []):
                    if f.get('k') == 'target' and f.get('s'):
                        out.setdefault(arena, set()).add(f['s'])
    # `Animation` 组件要补到**每一个** `kind == "animation"` 的目标上（那些对象已经建好了 —— 例如 `Directional Light`）
    anims = {}
    for arena, v in (blend.get('scene') or {}).items():
        items = v if isinstance(v, list) else v.get('items', [])
        for it in items:
            for t in (it.get('targets') or []):
                if t.get('kind') == 'animation' and t.get('path'):
                    anims.setdefault(arena, {})[t['path']] = 1
    return out, anims


def quality_off(arena):
    """`ArenaBuilder.LoadInactiveByQuality`（`:3957-3967`）的 python 侧对读 —— **同一份旁挂、同一条判据**：
    原版 `ObjectTogglerByQuality` 在**我们这一档**关着的对象（清单里 `active=True` 也照关）。
    ⚠️ 名字**不做 `norm()`**（C# 侧是 `inactiveByQuality.Contains(goName)`，原样比）。
    文件不存在 ⇒ 空集（那几场没有按档开关的对象）。"""
    p = os.path.join(ARENAS, arena, '%s_qualitytoggle.json' % arena)
    if not os.path.exists(p):
        return set()
    f = json.load(io.open(p, encoding='utf-8'))
    return set(o['go'] for o in (f.get('objects') or [])
               if isinstance(o, dict) and o.get('go') and not o.get('active', True))


def is_built(arena, e, kind, qoff, tally):
    """一条清单条目**会不会真被建出来** —— 逐条照抄 `ArenaBuilder.BuildContent` 的那几道闸门（A343）。

    🔴 **为什么非共用不可**（铁律 6「两处写同一条规则 = 迟早不一致」）：本函数的结果决定
      这个对象进 `targets[]`（「已建 ⇒ 只改挂」）还是进 `nodes[]`（「没建 ⇒ 新建节点」）。
      不照闸门 ⇒ 被挡掉的对象仍进 `targets[]` ⇒ `ApplyGroupNodes` 里**必然**找不到 ——
      A191 那 58 条「没对上」（darkangels 10 · tau 48）**全部**是这么来的，
      **不是查找逻辑的 bug**。判据 → `资料/普查产出_1011/D4_A191没对上诊断.md` §三 / §六。

    ⚠️ **闸门有先后**（`ArenaBuilder.cs:2011-2039` 每道都是 `continue`）：①在最前 ——
      「既无贴图又关着」的要记在①那一档，顺序错了建场日志里那几个数就换（`:4362` / `:7468`）。

    出处逐条（唯一判据 = `Editor/ArenaBuilder.cs`，⛔ 别在这里另立一套）：
      · 粒子 ① 无贴图 `:2011-2016`（豁免口 `NoTexMeshModeOk` `:3466` = `renderMode==4` **且**网格取得到）
      · 粒子 ② 原版关着 `:2018-2025`（清单的 `active` 已沿父链逐层与过）· ③ 画质档 `:2026-2031`
      · 粒子 ④ `renderMode == 5 (None)` `:2032-2039`
      · 网格 **只有 ③** `:1843-1844` —— 网格**没有**「无贴图 / `!active` / renderMode」这三道，
        ⛔ 别顺手加上（holder 在 `:1845` **无条件**建出来；OBJ 读不出只是少一个渲染器，`:1874`）
      · 灯 / 相机 **一道都没有**（`:2770-2780` / `:3118-3126`，无条件建）

    ⚠️ ① 的豁免口在 python 侧是**近似**：这里只判「`renderMode == 4` 且 `mesh` 非空」，
      C# 还要 `AssetDatabase` **真把网格取到**（`ParticleMesh` `:79`）⇒ 「字段有、资产坏」那一格
      这条判不了。实测全 13 场落在这一格上的清单条目**共 2 条**（arena3 的 `Necrons Close Monolith Rays`
      ×2，`Sphere.obj` 都在盘上），而**本生成器只跑 darkangels / tau 两场** ⇒ 今天零影响。"""
    go = e.get('go')
    if kind == 'meshes':
        if go and go in qoff:                       # ③
            tally['meshQuality'] += 1
            return False
        return True
    if kind == 'particles':
        if not e.get('texFile') and not (e.get('renderMode') == 4 and e.get('mesh')):
            tally['psTexNoMesh'] += 1               # ①
            return False
        if not e.get('active'):                     # ②
            tally['psInactive'] += 1
            return False
        if go and go in qoff:                       # ③（⚠️ 这一档**不在**建场日志那句汇总里，见 `:2438-2442`）
            tally['psQuality'] += 1
            return False
        if e.get('renderMode') == 5:                # ④
            tally['psRenderNone'] += 1
            return False
    return True


def build(arena, want, anims, manifest):
    sc = Scene(arena)
    # 🔴 2026-10-11（A343）：`built` = **清单里那些真会被建出来的** —— 与被闸门挡掉的分家。
    #   `tally` 的键**与建场日志那句汇总一一对应**（`ArenaBuilder.cs:2438-2442`），方便两处对数。
    qoff = quality_off(arena)
    tally = {'psTexNoMesh': 0, 'psInactive': 0, 'psQuality': 0, 'psRenderNone': 0, 'meshQuality': 0}
    built = set()
    for kind in ('meshes', 'particles'):
        for e in (manifest.get(kind) or []):
            if not e.get('go'):
                continue
            if not is_built(arena, e, kind, qoff, tally):
                continue                            # 被闸门挡掉 ⇒ 不算「已建」（进 `nodes[]`）
            built.add(norm(e['go']))
    # ⚠️ **灯 / 相机不在 `meshes`/`particles` 里** —— `ArenaBuilder` 单独建它们
    #   （`ArenaBuilder.cs` 的 `new GameObject(mf.light.name)` 与 `mf.camera.name`）。
    #   漏了它们，`Scenario/Directional Light` 那条「只补 Animation」就永远进不了旁挂
    #   ⇒ `ScenarioAnimationBlend` 那颗的 `myAnimation` 仍然恒 null。
    #   🆕 A343：这两件**没有任何闸门**（`:2770-2780` / `:3118-3126` 无条件建）⇒ 照旧直接进 `built`。
    for key in ('light', 'camera'):
        v = manifest.get(key) or {}
        if isinstance(v, dict) and v.get('name'):
            built.add(norm(v['name']))

    # 要补的路径 → GameObject
    gopids = []
    for p in sorted(want):
        leaf = p.split('/')[-1]
        cands = [g for g in sc.by_name.get(leaf, []) if '/'.join(n for n, _, _, _ in sc.chain(g)) == p]
        if not cands:
            cands = [g for g in sc.by_name.get(leaf, [])
                     if norm('/'.join(n for n, _, _, _ in sc.chain(g))) == norm(p)]
        if not cands:
            print('  ⚠️ 原版场景里找不到这条路径：%s' % p)
            continue
        gopids.append(cands[0])

    # ① 祖先链（含 `Scenario` 与中间那两级）+ ② 每个 wanted 的子树
    need = set()
    for g in gopids:
        ch = sc.chain(g)
        for i in range(1, len(ch) + 1):                      # 祖先链（`ch` 已在下面按路径建索引）
            need.add(tuple(n for n, _, _, _ in ch[:i]))
        for d in sc.subtree(g):
            need.add(tuple(n for n, _, _, _ in sc.chain(d)))

    # 名字/路径 → gopid（同一个路径只留一个）
    by_path = {}
    for g in need:
        ps = '/'.join(g)
        leaf = g[-1]
        for c in sc.by_name.get(leaf, []):
            if '/'.join(n for n, _, _, _ in sc.chain(c)) == ps and ps not in by_path:
                by_path[ps] = c

    def ppos(ps):
        """某条路径上的对象的**无缩放世界位置**（= 清单同一套）——
        用来在 `ArenaBuilder` 侧按「名字 + 最近位置」把它对上（同名对象不止一个）。"""
        if not ps:
            return []
        g = by_path.get(ps) or go_at_path(sc, ps)
        if g is None:
            return []
        p, _, _ = sc.world_of(g)
        return [round(x, 6) for x in p]

    nodes, targets = [], []
    for ps in sorted(by_path, key=lambda s: (s.count('/'), s)):
        g = by_path[ps]
        leaf = ps.split('/')[-1]
        ppath = '/'.join(ps.split('/')[:-1])
        p, _r, _s = sc.world_of(g)                      # 匹配用（清单口径）
        lp, lq, ls = sc.local_of(g)                     # 建节点用（原版 local，不引入第二套口径）
        anim = 1 if sc.has_animation(g) else 0
        if norm(leaf) in built and ps not in want:
            # 已经建好了 ⇒ 只可能「改挂」（`wanted` 那几条一定缺，不走这支）
            targets.append({'name': leaf, 'pos': [round(x, 6) for x in p],
                            'parent': ppath, 'parentPos': ppos(ppath), 'animation': anim})
        else:
            nodes.append({'path': ps, 'name': leaf,
                          'localPos': [round(x, 6) for x in lp],
                          'localRot': [round(x, 7) for x in lq],
                          'localScale': [round(x, 6) for x in ls],
                          'parent': ppath, 'parentPos': ppos(ppath), 'animation': anim})

    # 🔴 **不改挂、只补 `Animation`** 的那几条（`Directional Light` 就是）：它们是
    #   `kind == "animation"` 的目标、**已经建好**、而且**不在**上面那棵子树里
    #   （`Scenario/Directional Light` 是 `Scenario` 的另一个子件）⇒ 单独一张表。
    adds = []
    for ps in sorted(anims.get(arena) or {}):
        leaf = ps.split('/')[-1]
        if norm(leaf) not in built:
            continue                                     # 它由 `nodes` 那条路新建、组件在那儿挂
        cands = [g for g in sc.by_name.get(leaf, [])
                 if '/'.join(n for n, _, _, _ in sc.chain(g)) == ps]
        if not cands:
            continue
        p, _, _ = sc.world_of(cands[0])
        if not sc.has_animation(cands[0]):
            continue
        adds.append({'name': leaf, 'pos': [round(x, 6) for x in p], 'animation': 1})

    return {'scene': arena,
            '_schema': 'nodes[] = 要新建的节点（浅→深；localPos/localRot/localScale = **原版 local**，'
                       '`ArenaBuilder` 直接写下去 —— 不引入第二套坐标口径）· '
                       'targets[] = 已建对象（改挂到 `parent` 那条路径下；`pos`/`parentPos` = '
                       '**与清单同一套**的无缩放世界链，用于按「名字 + 最近位置」对上对象）· '
                       'adds[] = 已建对象**不改挂**、只补 `Animation` 组件 · animation=1 = 原版这个 GO 上有它',
            '_sources': 'scenes_scenes_%s.bundle（直读）＋ 数据/游戏数据/env_blendables.json 的 '
                        '_missingTargets[].wantedPath 与 lookat 目标的 target 字段' % arena,
            '_stats': {'nodes': len(nodes), 'targets': len(targets), 'adds': len(adds),
                       'animation': sum(1 for n in nodes if n['animation'])
                                    + sum(1 for t in targets if t['animation'])
                                    + sum(1 for t in adds if t['animation']),
                       # 🆕 A343：**被 `ArenaBuilder` 的闸门挡掉、因而没进 `built` 的清单条目数**。
                       #   键名与建场日志那句「内容：网格 … 按画质档不建 N…、粒子 … 另跳过无贴图 N ·
                       #   原版关着 M · renderMode=None K 个」一一对应（`ArenaBuilder.cs:2438-2442`），
                       #   **顺序也是①→②→③→④** —— 对数时挨个比。`psQuality` 那一档日志里没印
                       #   （原版场景里粒子按画质档关的少），列在这儿是为了**这是 0 还是没查**一眼可辨。
                       'gatesExcluded': dict(tally)},
            'nodes': nodes, 'targets': targets, 'adds': adds}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true')
    args = ap.parse_args()

    blend = json.load(io.open(BLEND, encoding='utf-8'))
    want, anims = wanted_paths(blend)
    print('要补的场：%s' % ', '.join(sorted(want)))
    out_dir_n = 0
    for arena in sorted(want):
        mf = json.load(io.open(os.path.join(ARENAS, arena, arena + '_manifest.json'), encoding='utf-8'))
        g = build(arena, want[arena], anims, mf)
        print('=== %s：nodes %d · targets %d · animation %d ==='
              % (arena, g['_stats']['nodes'], g['_stats']['targets'], g['_stats']['animation']))
        ex = g['_stats']['gatesExcluded']
        print('   闸门（A343 · 与建场日志「[Arena] 内容：…」那行**同口径**）：粒子 —— 另跳过无贴图 %d · '
              '原版关着 %d · 画质档 %d · renderMode=None %d 个；网格 —— 按画质档不建 %d 个'
              '（合计没进 `built` 的清单条目 %d 条）'
              % (ex['psTexNoMesh'], ex['psInactive'], ex['psQuality'], ex['psRenderNone'],
                 ex['meshQuality'], sum(ex.values())))
        for n in g['nodes']:
            print('   [node] %-96s anim=%d local=%s' % (n['path'], n['animation'], n['localPos']))
        for t in g['targets']:
            print('   [target] %-34s parent=%-64s anim=%d pos=%s' % (t['name'], t['parent'], t['animation'], t['pos']))
        for t in g['adds']:
            print('   [add] %-34s anim=%d pos=%s' % (t['name'], t['animation'], t['pos']))
        if not args.check:
            p = os.path.join(ARENAS, arena, arena + '_groups.json')
            txt = json.dumps(g, ensure_ascii=False, indent=1) + '\n'
            with io.open(p, 'wb') as f:
                f.write(txt.encode('utf-8'))
            print('   → %s' % p)
            out_dir_n += 1
    print('写出 %d 份' % out_dir_n)
    return 0


if __name__ == '__main__':
    sys.exit(main())
