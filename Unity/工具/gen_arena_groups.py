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

🔴 **A345（2026-10-13）：范围已从「A191 点名的几个宿主」扩到【全覆盖】** —— 每一场、**每一条会真被建出来的**
  清单条目（网格 / 粒子 / 灯 / 相机）都按**原版那棵树里的整条路径**归位：
    · `nodes[]` = 原版树上有、我们**没建**的对象 ⇒ 新建**空节点**（含 `Scenario` / `Particles` 这类容器层，
      以及被 `ArenaBuilder` 四道闸门挡掉的那批）；
    · `targets[]` = 我们**已经建出来**的对象 ⇒ 改挂到它的原版父节点下（`SetParent(…, true)`，画面零变化）；
    · ⇒ 容器层**不再是平铺的**（原来 `Scenario/Particles` 那一层、以及其余 11 场都没有）。
  ⚠️ **只有一条有意排除**（⛔ 别当「漏了」）：
    · **`worldBaked` 的网格**（13 场共 33 条，全在 `battlearena2`）—— 顶点已烘在世界坐标、holder 必须
      identity，`ArenaBuilder.ApplyGroupNodes` 有一道 `frozen` 明令**不许改挂**；放进来只会让「没对上」
      计数 +33（**假红**）⇒ 这一侧先挡掉 + **出声计数**（`coverage.skipWorldBaked`）。
    · ⚠️ **相机与 `Cache Stealth`（每场各 1 件）不排除**：它们原版住在 `BattlePrefab/…` 下（战斗 UI
      预制体那半边）。本来想只排掉相机，但 `Cache Stealth` 是**清单里的网格**、我们真建了它 ⇒
      **那棵树 13 场都会长出来**，只排相机反而变成「`BattlePrefab` 在、相机却平铺着」⇒ 口径统一成
      一句：**清单里每一条会建出来的对象都按原版路径归位**。理由与判据写在 `MATCH_SUSPECT` 上面那段注释里。
  判据 → `资料/普查产出_1013/A表现核_块5.md` §A345 · 原始出处 `资料/普查产出_1011/W11_子8b.md` §五·1。

判据来源（**直读原版 bundle**，不用解包 JSON —— 解包出来的 GameObject 按名命名、pid 丢了）：
  · `<AA>/scenes_scenes_<场>.bundle`（`AA` = `d:/2/.../StandaloneWindows64`）
  · 要补哪些路径，**三条来源**（都不手抄）：
      ① 🆕 **清单全覆盖**：`arenas/<场>/<场>_manifest.json` 里**每一条会建出来的**网格 / 粒子 / 灯，
         按「**归一化名字 + 无缩放世界链最近位置**」对到原版场景对象上 —— 与 `ArenaBuilder.FindBuilt`
         （`ArenaBuilder.cs:2668`）**同一套判据**（同名对象不止一个，取最近那个、没有容差）。
         ⛔ 对不上的**逐条出声**（铁律 5·c / 不许静默失败），绝不静默丢。
      ② `数据/游戏数据/env_blendables.json` 的 `_missingTargets[].wantedPath`
         （⚠️ 那张表**会自己变空** —— A191 把 4 个宿主建出来之后，2026-10-13 实测已是 **0 条**；
           见 `main()` 里那段注释：这正是「场的名单不能再由旁挂决定」的原因）
      ③ 同一份里 `scene[<场>]` 每条 `kind == "lookat"` 目标的 `fields` 里 `k == "target"` 的 `s`
         （= `LookAtConstrainWIP.target` 指的那个对象；它**不在** `_missingTargets` 里 —— 那张表只查
          blendable 自己的 target、不查「target 组件自己的字段」。判据 → `资料/普查产出_1011/W9_*.md` §五·4）
  · 要补 `Animation` 组件的对象 = 旁挂里**每一个** `kind == "animation"` 的目标
    （原版那两个 `Animation` 组件就挂在它们身上：`Directional Light` · `Battle Arena Dark Angels baked` ·
     `Railgun Turret 1/2`）。

产物：`MyGame/Assets/WarpforgeArena1/arenas/<场>/<场>_groups.json`（13 场各一份，A345 起）
  { "scene", "_schema", "_sources", "_stats",
    "nodes":   [ {path, name, localPos[3], localRot[4], localScale[3], parent, parentPos, animation} ],
    "targets": [ {name, pos[3], parent, parentPos, animation} ],
    "adds":    [ {name, pos[3], animation} ] }
  · `nodes[]` = 要【新建】的节点（**浅→深**排好序 ⇒ 父一定先于子出现），
    **写的是原版 local TRS**（`ArenaBuilder` 直接 `localPosition/localRotation/localScale` 写下去 ——
    不引入第二套坐标口径）。
  · `targets[]` = 我们**已经建出来**的对象 ⇒ 改挂到 `parent` 那条路径下。
    `pos` / `parentPos` = **与清单同一套**的无缩放世界链（`world_chain` 口径），
    用来在 `ArenaBuilder` 侧按「名字 + 最近位置」把它对上（同名对象不止一个）。
  · `adds[]` = 已建对象**不改挂**、只补 `Animation` 组件（A345 起大多已并入 `nodes[]`/`targets[]`，
    剩 0 条）。
  · `parent` = 原版那条层级路径（`nodes[].path`，或一个已建对象的原版路径；空 = 场根）。
  · `_stats.coverage` = **覆盖面的自证数**（want / 清单行 / 会建 / 旁挂贡献 / 各档出声数 + 逐条点名），
    A345 新加；`_stats.gatesExcluded` 是 A343 那四道闸门的计数（与建场日志同口径）。

用法：PYTHONIOENCODING=utf-8 python 工具/gen_arena_groups.py            # 写盘（13 场全跑）
      PYTHONIOENCODING=utf-8 python 工具/gen_arena_groups.py --check    # 只扫不写
      PYTHONIOENCODING=utf-8 python 工具/gen_arena_groups.py --only battlearena3   # 只跑一场（调试）
      …  --verbose                                                       # 逐条打印 nodes/targets/adds
⚠️ **A345 之后 13 场都会写盘**（13 场共 1600 多条 nodes/targets，逐条打印会刷屏）⇒ 默认只打
   **汇总 + 警告**，`--verbose` 才是逐条（判据/核对时用）。
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
        # 🔴 A419（2026-10-12）：**`RectTransform` 也必须收进来** —— 原版场景里两种都有
        #   （实测 `battlearenatauviorla`：`Transform` 389 个 · **`RectTransform` 988 个**），
        #   而 `chain()` 是顺着 `m_Father` 往上走的、**父可能是 `RectTransform`** ⇒ 原来只索引
        #   `Transform`，一撞就 `KeyError`（实测：对该场 1377 个 GameObject 逐个调一次 `chain()`，
        #   **90 个抛 `KeyError`**；首个 = GO pid 52 的父 pid 3385）。
        #   两种类型的序列化字段**同名同义**（`m_GameObject` / `m_Father` / `m_Children` /
        #   `m_LocalPosition` / `m_LocalRotation` / `m_LocalScale`）⇒ 合并成一张表之后，
        #   下面所有读法（`chain` / `world_of` / `local_of` / `children_of`）**一处都不用分家**。
        #   ⚠️ 一个 GameObject 上二者**只会有一个**（Unity 里 `RectTransform` 就是 `Transform`
        #   那一档的特化）⇒ `go2tr` 一个 GO 仍只对应一条，不会有第二条把 `tr_of` 的选择搞乱。
        self.tr = {p: d for p, (k, d) in t.items()
                   if k in ('Transform', 'RectTransform') and isinstance(d, dict)}
        self.go2tr = {}
        for p, d in self.tr.items():
            self.go2tr.setdefault(d['m_GameObject']['m_PathID'], []).append(p)
        self.by_name = {}
        for p, d in self.go.items():
            self.by_name.setdefault(d.get('m_Name'), []).append(p)
        # 🆕 A345：**子件索引** —— 原来 `children_of()` 每次遍历整张 `tr` 表，而全覆盖之后
        #   `subtree()` 会对每个 wanted 各调一次 ⇒ O(n²)（实测每场 1300+ 个 GO）。
        #   判据与旧实现逐字相同：谁把 `m_Father` 指到我（的 Transform）谁就是我的子件。
        self.kids = {}
        for p, d in self.tr.items():
            f = (d.get('m_Father') or {}).get('m_PathID')
            if f:
                self.kids.setdefault(f, []).append(d['m_GameObject']['m_PathID'])
        self._wcache = {}          # gopid → `world_of()` 的结果。**纯函数 ⇒ 缓存不改语义**
                                   #   （不然 `find_by_name_pos` 对同名 8 个候选会各走一次父链）
        self._paths = None         # 懒建：**整条层级路径 → gopid**（见 `path_index()`）

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
        """带缓存的门面（`_world_of` 那份实现与判据逐字不变）。"""
        hit = self._wcache.get(gopid)
        if hit is None:
            hit = self._world_of(gopid)
            self._wcache[gopid] = hit
        return hit

    def _world_of(self, gopid):
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
        return list(self.kids.get(tp, ()))

    def path_index(self):
        """**整条层级路径 → gopid**（懒建一次）。
        🔴 A345 之前没有它：那时候 `build()` 对每条要补的路径都去 `by_name` 里捞同名对象、
          再逐个拼父链比对（O(要补的条数 × 场景)）；铺到 13 场之后那是千万级 ⇒ 改成先建一张表。
        ⚠️ 同一条路径理论上只对应一个对象；真撞上就**先见者胜**（与旧实现 `ps not in by_path` 同一套）。"""
        if self._paths is None:
            d = {}
            for p in self.go:
                ps = '/'.join(n for n, _, _, _ in self.chain(p))
                d.setdefault(ps, p)
            self._paths = d
        return self._paths

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
    g = sc.path_index().get(ps)
    if g is not None:
        return g
    parts = ps.split('/')
    for g in sc.by_name.get(parts[-1], []):
        if '/'.join(n for n, _, _, _ in sc.chain(g)) == ps:
            return g
    return None


# 🔴 A345：**有意排除**的一条（⛔ 别当「漏了」—— 判据 → 文件头 + `资料/普查产出_1013/A表现核_块5.md` §A345）
#   · `worldBaked` 的网格（13 场共 33 条，全在 `battlearena2`）：顶点已烘在世界坐标、holder 必须 identity，
#     `ArenaBuilder.ApplyGroupNodes` 有一道 `frozen` 明令**不许改挂**（照抄 `ArenaBuilder.cs` 那一段）。
#     生成侧把它们放进 `targets[]` 只会让「没对上」计数 +33（C# 侧必然 refuse）⇒ 这一侧先挡掉 + 出声。
#   · ⚠️ **相机与 `Cache Stealth` 不排除**（2026-10-13 定）：它们（每场各 1 件，13 场共 26 件）原版住在
#     `BattlePrefab/…` 下 —— 那是**战斗 UI 预制体那半边**。本来想只排掉相机，但**排不掉那棵树**：
#     `Cache Stealth`（`BattlePrefab/Cache [No delete]`，3D 卡用的网格）是**清单里的网格**、我们真建了它，
#     全 13 场都会因此凭空长出 `BattlePrefab` 节点 ⇒ 只排相机就变成「`BattlePrefab` 在、它底下的相机却平铺着」
#     —— 比两头都糟。⇒ 口径统一成一句：**清单里每一条会建出来的对象都按原版路径归位**（含这两件）。
#     ⚠️ 判据两条：① 那两级父的 local 全是 identity/纯平移（`BattlePrefab` 与 `Cache [No delete]` 实测 identity），
#     改挂走 `SetParent(…, true)` ⇒ **世界位姿一字不变**；② 运行期**没有**按名字找它们的代码
#     （`ArenaRuntimeLoader.Load(key, cam)` 是**传引用**；自检 `FindBoardCamera()` 按 `depth < 0` 找）。
#     ⛔ 若调度台后来判定该排掉 ⇒ 改 `wanted_paths()` 里那一处过滤（一行），重跑 3.5 秒。
#
# 「同名对象里取最近的那一个」**没有容差**（与 `ArenaBuilder.FindBuilt` 逐条对齐：它也只取最近）。
# 这个数只用来**出声**：最近的那个也差这么远 ⇒ 报「可疑匹配」，⛔ 不静默接受、也不静默丢弃。
MATCH_SUSPECT = 1.0


def scene_items(blend, arena):
    """旁挂里那一场的条目表（`scene[<场>]` 有两种写法：list 或 {items: […] }）。"""
    v = (blend.get('scene') or {}).get(arena)
    if v is None:
        return []
    return v if isinstance(v, list) else (v.get('items') or [])


def anim_targets(blend):
    """要补 `Animation` 组件的对象 = 旁挂里**每一个** `kind == "animation"` 的目标（→ `{场: {路径: 1}}`）。
    原版那两个 `Animation` 组件就挂在它们身上（`Directional Light` · `Battle Arena Dark Angels baked` ·
    `Railgun Turret 1/2`）。⚠️ 这一条**与全覆盖无关**，A345 前后行为不变（只从 `wanted_paths()` 里拆出来）。"""
    anims = {}
    for arena, v in (blend.get('scene') or {}).items():
        items = v if isinstance(v, list) else (v.get('items') or [])
        for it in items:
            for t in (it.get('targets') or []):
                if t.get('kind') == 'animation' and t.get('path'):
                    anims.setdefault(arena, {})[t['path']] = 1
    return anims


def find_by_name_pos(sc, name, pos):
    """按「**归一化名字 + 无缩放世界链最近位置**」在原版场景里找一个对象 —— 判据与
    `ArenaBuilder.FindBuilt`（`ArenaBuilder.cs:2668`，**生产那一份**）**逐条对齐**：
    `Norm(name)` 相等 + 同名里取距离最小的那一个（**没有容差**，不因「差一点点」而拒绝候选）。
    返回 `(gopid, 距离)`；找不到回 `(None, None)`。

    ⚠️ 位置口径 = 清单那一套（`工具/scripts快照/gen_unity_arena_manifest.py:662` 的 `world_chain`：
      沿父链只累乘旋转与平移、**不乘父级缩放**）—— `Scene.world_of()` 是同一套（见它的 docstring）。
    ⚠️ 名字先按**原样**查（快），查不到再按 `norm()` 扫一遍（原版真有带**尾随空格**的名字，
      如 `'Railgun Turret 1 Target '`；清单里存的也是原样）。"""
    cands = sc.by_name.get(name)
    if not cands:
        wn = norm(name)
        cands = [p for n, lst in sc.by_name.items() if norm(n) == wn for p in lst]
    if not cands:
        return None, None
    if not pos or len(pos) < 3:
        return cands[0], 0.0
    best, bd = None, None
    for g in cands:
        wp, _, _ = sc.world_of(g)
        d = math.sqrt(sum((wp[i] - pos[i]) ** 2 for i in range(3)))
        if bd is None or d < bd:
            best, bd = g, d
    return best, bd


def wanted_paths(blend, arena, sc, manifest, qoff, tally, stats):
    """**该场要补的全部路径**（🆕 A345 · 全覆盖）—— 三条来源，一条都不手抄：

      **① 清单全覆盖**（新增的那条主路）：该场**每一条会真被建出来的**网格 / 粒子 / 灯 / 相机，
        在原版场景里的**整条路径**。⇒ 容器层（`Scenario` / `Particles` / `<场> Baked` …）由
        `build()` 的「祖先链」那一步自动建出来，不再平铺。
      **② / ③ 旁挂那两样**（A191 原样保留，见文件头）。

    ⛔ **一条都不许静默丢**：对不上 / 匹配可疑 / 被有意排除的，全部**出声 + 计数**进 `stats`
      （`unmatched` / `suspect` / `skipWorldBaked` …），由 `main()` 打出来。

    返回 `(want, built_paths)`：
      · `want` = 要进那棵树的**全部路径**（含祖先链与子树由 `build()` 展开）；
      · `built_paths` = 其中**我们已经建出来**的那些（= `targets[]`，其余进 `nodes[]`）。
        🔴 **必须由这里给**：`built` 原来是个**名字集合**，而全覆盖之后
        「这条路径上的对象建没建」必须**按路径**判（同名对象不止一个 ⇒ 按名字判会**张冠李戴**）。"""
    out = set()
    built_paths = set()

    def add(kind, name, pos, world_baked=False):
        """一条清单条目 → 原版路径。"""
        if world_baked:
            stats['skipWorldBaked'] += 1
            stats['skipWorldBakedWhat'].append('%s `%s`' % (kind, name))
            return
        g, d = find_by_name_pos(sc, name, pos)
        if g is None:
            stats['unmatched'] += 1
            stats['unmatchedWhat'].append('%s `%s`（清单 pos %s）' % (kind, name, pos))
            return
        if d is not None and d > MATCH_SUSPECT:
            stats['suspect'] += 1
            stats['suspectWhat'].append('%s `%s` → 最近的原版对象也差 %.3f' % (kind, name, d))
        if d is not None and d > stats['maxDist']:
            stats['maxDist'] = d
        ps = '/'.join(n for n, _, _, _ in sc.chain(g))
        out.add(ps)
        built_paths.add(ps)

    # ---- ① 清单全覆盖：网格 / 粒子（过 `is_built` 那四道闸门的才算「会建出来」）----
    for kind in ('meshes', 'particles'):
        for e in (manifest.get(kind) or []):
            go = e.get('go')
            if not go:
                continue
            stats['manifestRows'] += 1
            if not is_built(arena, e, kind, qoff, tally):
                continue
            stats['manifestBuilt'] += 1
            add(kind, go, e.get('pos'), bool(e.get('worldBaked')))
    # 灯 / 相机**不在** `meshes`/`particles` 里（`ArenaBuilder` 单独建它们，见 `build()` 里那段注释）
    lv = manifest.get('light') or {}
    if isinstance(lv, dict) and lv.get('name'):
        stats['manifestRows'] += 1
        stats['manifestBuilt'] += 1
        add('light', lv['name'], lv.get('pos'))
    cv = manifest.get('camera') or {}
    if isinstance(cv, dict) and cv.get('name'):
        stats['manifestRows'] += 1
        stats['manifestBuilt'] += 1
        add('camera', cv['name'], cv.get('pos'))
    # ---- ② 旁挂：`_missingTargets[].wantedPath` ----
    for m in (blend.get('_missingTargets') or []):
        if m.get('arena') == arena and m.get('wantedPath'):
            out.add(m['wantedPath'])
            stats['sidecarPaths'] += 1
    # ---- ③ 旁挂：`kind == "lookat"` 的 `target` 字段 ----
    for it in scene_items(blend, arena):
        for t in (it.get('targets') or []):
            if t.get('kind') != 'lookat':
                continue
            for f in (t.get('fields') or []):
                if f.get('k') == 'target' and f.get('s'):
                    out.add(f['s'])
                    stats['sidecarPaths'] += 1
    return out, built_paths


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
      ×2，`Sphere.obj` 都在盘上）。
      🔴 **2026-10-13 订正（A345）**：这句原来接着写「而本生成器只跑 darkangels / tau 两场 ⇒ 今天零影响」
      —— **A345 起 13 场全跑**，那 2 条**今天就在跑的范围里**（它们落在①的豁免口上、豁免口成立 ⇒
      结果不受影响，但「零影响」那个理由**已经过期**，如实改掉）。"""
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


def build(arena, blend, anims, manifest, sc=None):
    """一场的旁挂。🆕 A345：`want` **不再由调用方传** —— 它由 `wanted_paths()` 当场算（全覆盖），
    因为算它要用到**已经读进来的 `Scene`**（要按名字 + 位置把清单条目对到原版对象上）。"""
    sc = sc if sc is not None else Scene(arena)
    # 🔴 2026-10-11（A343）：闸门计数 `tally` 的键**与建场日志那句汇总一一对应**
    #   （`ArenaBuilder.cs:2438-2442`），方便两处对数。
    qoff = quality_off(arena)
    tally = {'psTexNoMesh': 0, 'psInactive': 0, 'psQuality': 0, 'psRenderNone': 0, 'meshQuality': 0}
    stats = {'manifestRows': 0, 'manifestBuilt': 0,
             'skipWorldBaked': 0, 'skipWorldBakedWhat': [],
             'unmatched': 0, 'unmatchedWhat': [],
             'suspect': 0, 'suspectWhat': [], 'maxDist': 0.0,
             'sidecarPaths': 0, 'want': 0,
             'missingPath': 0, 'missingPathWhat': [],
             'parentPosMissed': 0, 'parentPosMissedWhat': [],
             'addsSkipped': 0, 'addsSkippedWhat': []}

    # 🔴 A345：`want` + `built_paths` 一起算出来。`built_paths` = **我们已经建出来**的那些路径
    #   ⇒ 它们进 `targets[]`（只改挂）；其余进 `nodes[]`（新建空节点）。
    want, built_paths = wanted_paths(blend, arena, sc, manifest, qoff, tally, stats)
    stats['want'] = len(want)

    # 要补的路径 → GameObject
    gopids = []
    for p in sorted(want):
        leaf = p.split('/')[-1]
        g = go_at_path(sc, p)
        if g is None:
            g = next((c for c in sc.by_name.get(leaf, [])
                      if norm('/'.join(n for n, _, _, _ in sc.chain(c))) == norm(p)), None)
        if g is None:
            stats['missingPath'] += 1
            stats['missingPathWhat'].append(p)
            continue
        gopids.append(g)

    # ① 祖先链（含 `Scenario` / `Particles` 这些容器层）+ ② 每个 wanted 的子树
    need = set()
    for g in gopids:
        ch = sc.chain(g)
        for i in range(1, len(ch) + 1):                      # 祖先链（`ch` 已在下面按路径建索引）
            need.add(tuple(n for n, _, _, _ in ch[:i]))
        for d in sc.subtree(g):
            need.add(tuple(n for n, _, _, _ in sc.chain(d)))

    # 路径 → gopid（同一个路径只留一个）
    # 🆕 A345：原来是对每条 need 都去 `by_name` 里捞同名对象、逐条拼父链比（O(need × 场景)）——
    #   13 场铺开之后那是千万级 ⇒ 改用 `Scene.path_index()`（一次建表）。
    pidx = sc.path_index()
    by_path = {}
    for g in need:
        ps = '/'.join(g)
        c = pidx.get(ps)
        if c is None:
            leaf = g[-1]
            c = next((c for c in sc.by_name.get(leaf, [])
                      if '/'.join(n for n, _, _, _ in sc.chain(c)) == ps), None)
        if c is not None:
            by_path[ps] = c

    def ppos(ps):
        """某条路径上的对象的**无缩放世界位置**（= 清单同一套）——
        用来在 `ArenaBuilder` 侧按「名字 + 最近位置」把它对上（同名对象不止一个）。

        🔴 **2026-10-13（A345）：这一处原来取不到就静默回 `[]`。** 后果不是「少个字段」——
          `ArenaBuilder.ResolveGroupParent` 收到空数组时**一个字都不打**，只按「名字 + 没有 pos」
          去找（= 同名对象里随便挑一个）⇒ **摆错了也无声**。全覆盖之后同名对象只会更多
          （`Smoke` / `FirePit` / `Banners.00N` …），所以这一格必须出声。
        ⚠️ `ps` 为空是**正常态**（父 = 场根），照旧静默回 `[]`。"""
        if not ps:
            return []
        g = by_path.get(ps) or go_at_path(sc, ps)
        if g is None:
            stats['parentPosMissed'] += 1
            stats['parentPosMissedWhat'].append(ps)
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
        # 🔴 **2026-10-13（A345）判据换了 —— 这是全覆盖能做对的关键一跳。**
        #   旧规则 = `norm(leaf) in built and ps not in want`：隐含前提是「`want` 里的对象**一定缺**」，
        #     那是 A191 那个收窄范围下的事实（`want` 只装 4 个缺的宿主）。
        #   全覆盖之后 **`want` 装的就是「清单里每一条会建出来的对象」** ⇒ `ps not in want` 恒 False
        #     ⇒ **每一个已建对象都会被判成「要新建」** —— 那会在 prefab 里造出**一整套重名空节点**、
        #     而真正的对象仍平铺着（静默的灾难：断言还照样能全绿，因为节点确实建出来了）。
        #   新规则 = **`ps in built_paths`**（按**路径**判，不是按名字 —— 同名对象不止一个，
        #     按名字判会张冠李戴）。语义也更直白：**建过的 ⇒ 改挂；没建的 ⇒ 新建节点。**
        if ps in built_paths:
            targets.append({'name': leaf, 'pos': [round(x, 6) for x in p],
                            'parent': ppath, 'parentPos': ppos(ppath), 'animation': anim})
        else:
            nodes.append({'path': ps, 'name': leaf,
                          'localPos': [round(x, 6) for x in lp],
                          'localRot': [round(x, 7) for x in lq],
                          'localScale': [round(x, 6) for x in ls],
                          'parent': ppath, 'parentPos': ppos(ppath), 'animation': anim})

    # 🔴 **不改挂、只补 `Animation`** 的那几条：`kind == "animation"` 的目标里，
    #   **已经建好、又不在上面那棵树里**的那几个（`adds[]` 只带 `name`/`pos`，没有 path）。
    #   🆕 A345 起多一道过滤：**已经在 `nodes[]`/`targets[]` 里的不再列** —— 那些对象的组件
    #   由 `ApplyGroupNodes` ①/② 补（`nodes[].animation` / `targets[].animation`），
    #   列两遍只是让 `adds` 这个数看着虚高（C# 侧幂等、不会重复 AddComponent，但数就对不上了）。
    adds = []
    for ps in sorted(anims.get(arena) or {}):
        leaf = ps.split('/')[-1]
        if ps in by_path:
            stats['addsSkipped'] += 1
            stats['addsSkippedWhat'].append('%s（已经在 %s 里）' % (ps, 'targets' if ps in built_paths else 'nodes'))
            continue
        if norm(ps) not in set(norm(x) for x in built_paths):
            stats['addsSkipped'] += 1
            stats['addsSkippedWhat'].append('%s（不在本场那棵树里、也不是我们建的对象）' % ps)
            continue
        cands = [g for g in sc.by_name.get(leaf, [])
                 if '/'.join(n for n, _, _, _ in sc.chain(g)) == ps]
        if not cands:
            stats['addsSkipped'] += 1
            stats['addsSkippedWhat'].append('%s（原版场景里找不到这条路径）' % ps)
            continue
        p, _, _ = sc.world_of(cands[0])
        if not sc.has_animation(cands[0]):
            stats['addsSkipped'] += 1
            stats['addsSkippedWhat'].append('%s（原版这个 GO 上没有 `Animation` 组件）' % ps)
            continue
        adds.append({'name': leaf, 'pos': [round(x, 6) for x in p], 'animation': 1})

    return {'scene': arena,
            '_schema': 'nodes[] = 要新建的节点（浅→深；localPos/localRot/localScale = **原版 local**，'
                       '`ArenaBuilder` 直接写下去 —— 不引入第二套坐标口径）· '
                       'targets[] = 已建对象（改挂到 `parent` 那条路径下；`pos`/`parentPos` = '
                       '**与清单同一套**的无缩放世界链，用于按「名字 + 最近位置」对上对象）· '
                       'adds[] = 已建对象**不改挂**、只补 `Animation` 组件 · animation=1 = 原版这个 GO 上有它 · '
                       '🆕 A345：`want` = 本场清单里**每一条会建出来的**对象（网格/粒子/灯）的原版路径 '
                       '+ 旁挂那两样 ⇒ `nodes[]` 是「原版有、我们没建」的那批（含容器层）',
            '_sources': 'scenes_scenes_%s.bundle（直读）＋ arenas/%s/%s_manifest.json（清单全覆盖，A345）'
                        '＋ 数据/游戏数据/env_blendables.json 的 _missingTargets[].wantedPath 与 '
                        'lookat 目标的 target 字段' % (arena, arena, arena),
            '_stats': {'nodes': len(nodes), 'targets': len(targets), 'adds': len(adds),
                       'animation': sum(1 for n in nodes if n['animation'])
                                    + sum(1 for t in targets if t['animation'])
                                    + sum(1 for t in adds if t['animation']),
                       # 🆕 A343：**被 `ArenaBuilder` 的闸门挡掉、因而没进 `built` 的清单条目数**。
                       #   键名与建场日志那句「内容：网格 … 按画质档不建 N…、粒子 … 另跳过无贴图 N ·
                       #   原版关着 M · renderMode=None K 个」一一对应（`ArenaBuilder.cs:2438-2442`），
                       #   **顺序也是①→②→③→④** —— 对数时挨个比。`psQuality` 那一档日志里没印
                       #   （原版场景里粒子按画质档关的少），列在这儿是为了**这是 0 还是没查**一眼可辨。
                       'gatesExcluded': dict(tally),
                       # 🆕 A345：**覆盖面的自证数**（每一格都必须能解释；判据 → `A_表现核_块5.md` §A345）
                       'coverage': {
                           'want': stats['want'],                    # 要归位的路径数（含旁挂那两样）
                           'manifestRows': stats['manifestRows'],    # 清单里带 `go`/`name` 的条目（网格+粒子+灯+相机）
                           'manifestBuilt': stats['manifestBuilt'],  # 过闸门、会真建出来的
                           'sidecarPaths': stats['sidecarPaths'],    # 旁挂贡献的路径数（A191 那两样）
                           'skipWorldBaked': stats['skipWorldBaked'],
                           'unmatched': stats['unmatched'], 'unmatchedWhat': stats['unmatchedWhat'],
                           'suspect': stats['suspect'], 'suspectWhat': stats['suspectWhat'],
                           'maxMatchDist': round(stats['maxDist'], 6),
                           'missingPath': stats['missingPath'], 'missingPathWhat': stats['missingPathWhat'],
                           'parentPosMissed': stats['parentPosMissed'],
                           'parentPosMissedWhat': stats['parentPosMissedWhat'],
                           'addsSkipped': stats['addsSkipped'], 'addsSkippedWhat': stats['addsSkippedWhat'],
                           'skipWorldBakedWhat': stats['skipWorldBakedWhat'],
                       }},
            'nodes': nodes, 'targets': targets, 'adds': adds}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true', help='只扫不写')
    ap.add_argument('--verbose', action='store_true', help='逐条打印 nodes/targets/adds（默认只打汇总 + 警告）')
    ap.add_argument('--only', default='', help='只跑这几场（逗号分隔；调试用）')
    args = ap.parse_args()

    blend = json.load(io.open(BLEND, encoding='utf-8'))
    anims = anim_targets(blend)

    # 🔴 **A345（2026-10-13）：场的名单来自【清单】，不再来自旁挂。**
    #   原来这里是 `for arena in sorted(want)` —— 而 `wanted_paths()` 的输入只有旁挂那两样，
    #   其中 `_missingTargets` **会自己变空**：A191 把 4 个宿主建出来之后，`gen_env_blendables.py`
    #   的检查（`prefab ∪ 清单`）就都找得到了 ⇒ 2026-10-13 实测 `_missingTargets` = **0 条**。
    #   ⇒ 照旧写法，今天只会跑 `battlearenatauviorla` 一场、而且 `targets` 掉到 0
    #     （`--check` 实测：`nodes 6 · targets 0`；darkangels 那一场**连跑都不跑**，
    #      它那份 2026-10-11 的旧 `_groups.json` 会**留在盘上不动** ⇒ 旁挂与 prefab 两边各说各话）。
    #   ⇒ 名单改用「有 `<场>_manifest.json` 的场」（= 13 场）。
    arenas = sorted(d for d in os.listdir(ARENAS)
                    if os.path.exists(os.path.join(ARENAS, d, d + '_manifest.json')))
    if args.only:
        keep = set(x.strip() for x in args.only.split(',') if x.strip())
        arenas = [a for a in arenas if a in keep]
    print('要补的场（%d 场）：%s' % (len(arenas), ', '.join(arenas)))

    out_n = 0
    tot = {'nodes': 0, 'targets': 0, 'adds': 0, 'animation': 0,
           'manifestRows': 0, 'manifestBuilt': 0, 'sidecarPaths': 0,
           'skipWorldBaked': 0, 'unmatched': 0, 'suspect': 0, 'parentPosMissed': 0,
           'missingPath': 0, 'addsSkipped': 0, 'gates': 0}
    warn = []          # 所有**要出声**的东西（铁律：不许静默失败）
    for arena in arenas:
        mf = json.load(io.open(os.path.join(ARENAS, arena, arena + '_manifest.json'), encoding='utf-8'))
        g = build(arena, blend, anims, mf)
        st = g['_stats']; cov = st['coverage']; ex = st['gatesExcluded']
        print('=== %s：nodes %d · targets %d · adds %d · animation %d | want %d '
              '（清单行 %d · 会建 %d · 旁挂 %d）==='
              % (arena, st['nodes'], st['targets'], st['adds'], st['animation'], cov['want'],
                 cov['manifestRows'], cov['manifestBuilt'], cov['sidecarPaths']))
        print('   闸门（A343 · 与建场日志「[Arena] 内容：…」那行**同口径**）：粒子 —— 另跳过无贴图 %d · '
              '原版关着 %d · 画质档 %d · renderMode=None %d 个；网格 —— 按画质档不建 %d 个'
              '（合计没进 `built` 的清单条目 %d 条）'
              % (ex['psTexNoMesh'], ex['psInactive'], ex['psQuality'], ex['psRenderNone'],
                 ex['meshQuality'], sum(ex.values())))
        print('   覆盖：匹配最大距离 %.6f · 对不上清单条目 %d · 可疑匹配 %d · 路径找不到 %d · '
              '`parentPos` 取不到 %d · 有意排除 worldBaked %d'
              % (cov['maxMatchDist'], cov['unmatched'], cov['suspect'], cov['missingPath'],
                 cov['parentPosMissed'], cov['skipWorldBaked']))
        # 🔴 出声（**每一条都要能解释**；空 = 这一场干净）
        if cov['unmatchedWhat']:
            warn.append('%s：清单条目对不上原版场景对象 —— %s' % (arena, ' / '.join(cov['unmatchedWhat'])))
        if cov['suspectWhat']:
            warn.append('%s：匹配可疑（最近的原版对象也差 > %.1f）—— %s'
                        % (arena, MATCH_SUSPECT, ' / '.join(cov['suspectWhat'])))
        if cov['missingPathWhat']:
            warn.append('%s：要补的路径在原版场景里找不到 —— %s' % (arena, ' / '.join(cov['missingPathWhat'])))
        if cov['parentPosMissedWhat']:
            warn.append('%s：`parentPos` 取不到（A345 之前是**静默**回 []）—— %s'
                        % (arena, ' / '.join(cov['parentPosMissedWhat'])))
        if cov['skipWorldBakedWhat']:
            warn.append('%s：`worldBaked` ⇒ **有意不改挂**（`ArenaBuilder` 会 refuse）—— %s'
                        % (arena, ' / '.join(cov['skipWorldBakedWhat'])))
        if cov['addsSkippedWhat']:
            warn.append('%s：旁挂要补 `Animation` 但被跳过 —— %s' % (arena, ' / '.join(cov['addsSkippedWhat'])))
        if args.verbose:
            for n in g['nodes']:
                print('   [node] %-96s anim=%d local=%s' % (n['path'], n['animation'], n['localPos']))
            for t in g['targets']:
                print('   [target] %-34s parent=%-64s anim=%d pos=%s'
                      % (t['name'], t['parent'], t['animation'], t['pos']))
            for t in g['adds']:
                print('   [add] %-34s anim=%d pos=%s' % (t['name'], t['animation'], t['pos']))
        if not args.check:
            p = os.path.join(ARENAS, arena, arena + '_groups.json')
            txt = json.dumps(g, ensure_ascii=False, indent=1) + '\n'   # ← 先把内容算完再开文件写
            with io.open(p, 'wb') as f:
                f.write(txt.encode('utf-8'))
            print('   → %s' % p)
            out_n += 1
        tot['nodes'] += st['nodes']; tot['targets'] += st['targets']
        tot['adds'] += st['adds']; tot['animation'] += st['animation']
        tot['manifestRows'] += cov['manifestRows']; tot['manifestBuilt'] += cov['manifestBuilt']
        tot['sidecarPaths'] += cov['sidecarPaths']; tot['skipWorldBaked'] += cov['skipWorldBaked']
        tot['unmatched'] += cov['unmatched']; tot['suspect'] += cov['suspect']
        tot['parentPosMissed'] += cov['parentPosMissed']; tot['missingPath'] += cov['missingPath']
        tot['addsSkipped'] += cov['addsSkipped']
        tot['gates'] += sum(ex.values())
    print('')
    print('=== 全 %d 场合计：nodes %d · targets %d · adds %d · animation %d ==='
          % (len(arenas), tot['nodes'], tot['targets'], tot['adds'], tot['animation']))
    print('    清单行 %d · 会建 %d（被闸门挡掉 %d）· 旁挂贡献路径 %d · 有意排除 worldBaked %d · '
          '对不上 %d · 可疑 %d · 路径找不到 %d · `parentPos` 取不到 %d · adds 跳过 %d'
          % (tot['manifestRows'], tot['manifestBuilt'], tot['gates'], tot['sidecarPaths'],
             tot['skipWorldBaked'], tot['unmatched'], tot['suspect'], tot['missingPath'],
             tot['parentPosMissed'], tot['addsSkipped']))
    if warn:
        print('')
        print('⚠️ 出声 %d 条（⛔ 不是静默通过 —— 每一条都要能解释）：' % len(warn))
        for w in warn:
            print('   ⚠️ %s' % w)
    print('写出 %d 份%s' % (out_n, '（--check：没写盘）' if args.check else ''))
    return 0


if __name__ == '__main__':
    sys.exit(main())
