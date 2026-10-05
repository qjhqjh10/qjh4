#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_env_blendables.py —— 把「环境混合组件」（blendable）抽成旁挂 JSON。

为什么要它（判据 → `项目任务.md` §三 第 30 条「4 环境」· `资料/加时与冲突模式_原版规格.md`）：
  原版换环境那条链是 `ApplyEnvironment` → `EnableEnvironment`：
    ① 雾/环境光补间
    ② `scenarioObjects` prefab 实例化 → 对它子树里每个 `IScenarioEnvironmentBlendeable` 调 `DoScenarioBlend(opts)`
    ③ 再对【场景里已登记的那些】调一遍（方向 = `SO.defaultScenarioObjectsState`）
  这族组件原版有 **9 个实现类**（我们的 `CLASSES` 现在**全收了**）：
  · 2026-10-06 战-A 收的 5 个（`ScenarioParticleSystemBlender` 62 · `…Toggler` 36 · `…MaterialFader` 23 ·
    `…ParticleSpawnerBlender` 4 · `…GenericObjectToggler` 3）
  · 🆕 2026-10-07 波9离线（A136）收的另 4 个（`FlareScenarioToggler` 6 · `ScenarioAnimationBlend` 2 ·
    `ScenarioGenericMaterialBlend` 1 · `TauCannonAnimationStopper` 2，**11 个实例、全在场景侧**）
  ⇒ 合计 **139**（= 128 + 11）。

🔴 **按类名分辨，不按字段猜**：按「有 `particleSystems` 就算 Toggler」会命中 `AnimFXModule*` ——
   实测 `battleprefabs_vfxandmisc` 里 **314 个假阳性**。类名来自 `Waprforge_monoscripts` 的 `m_ClassName`。

产物：`数据/游戏数据/env_blendables.json`
  { "_schema", "_sources", "prefabs": { "<prefab 根名>": [...] }, "scene": { "<场>": [...] }, "stats", "_unresolved" }
  每条 = { "cls", "owner", "ownerLeaf", ["ownerPos"], "fields", "targets": [{path/name, leaf, kind, [pos]}] }
  · **prefab 侧**：`path` = 相对 prefab 根的层级路径（我们导入的 prefab 保层级 ⇒ 运行时按路径找）
  · **场景侧**：我们的战场是 `ArenaBuilder` 按清单【平铺】建的（没有父链）⇒ 给 **名字 + 世界位置**，
    运行时按「同名 + 最近」匹配（与 `ArenaBuilder.ApplyShadowSidecar` 同一套做法）

判据来源（原始 bundle，不用解包 JSON —— 解包出来的 GameObject 是按名命名的，**pid 丢了、解不了 PPtr**）：
  · prefab 侧 `<AA>/battleprefabs_vfxandmisc_assets_all.bundle`
  · 场景侧 `<AA>/scenes_scenes_<场>.bundle`（13 个）
  · 类名表 `<AA>/Waprforge_monoscripts.bundle`
  · `AA` = `d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64`

自检（写盘前跑，结果进 `_unresolved`）：
  · prefab 侧：每个 prefab 名在我们 `MyGame/Assets/WarpforgeVFX/Prefabs/<名>.prefab` 里在不在；
    每个目标 `leaf` 在该 `.prefab` 文本里有没有 `m_Name: <leaf>`
  · 场景侧：owner/target 的名字在该场 `battlearena<场>_manifest.json` 的 `go` 名字表里在不在
  · 🆕 2026-10-07（A135④）**材质补间那一层**（只有 `ScenarioGenericMaterialBlend` 有）两条：
    ① 每个 `blendProps` 名字都必须在**那件原版 Material** 的序列化属性表里（判据 = bundle 里的 Material 资产）；
    ② 每个 `blendProps` 名字也必须在**我们这场 arena 清单**里那个网格的 `subMats[].props[].k` 里
       —— 即「我们重建的那份材质真的带这个属性」（`_Blend` 就是这么核的）。

用法：PYTHONIOENCODING=utf-8 python 工具/gen_env_blendables.py            # 写盘
      PYTHONIOENCODING=utf-8 python 工具/gen_env_blendables.py --check    # 只扫不写
"""
import argparse
import io
import os
import json
import re
import sys

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
import UnityPy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_arena_negscale as N            # 复用同一套矩阵算式（判据只留一处）

ROOT = 'd:/4/Unity'
AA = 'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
PREFABS = os.path.join(ROOT, 'MyGame/Assets/WarpforgeVFX/Prefabs')
ARENAS = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas')
ARENA_PREFABS = os.path.join(ROOT, 'MyGame/Assets/Resources/ArenaPrefabs')
OUT = os.path.join(ROOT, '数据/游戏数据/env_blendables.json')
FLAT = os.path.join(ROOT, 'MyGame/Assets/Resources/EnvBlendables.json')

PREFAB_BUNDLE = 'battleprefabs_vfxandmisc_assets_all.bundle'
MONO_BUNDLE = 'Waprforge_monoscripts.bundle'
ALL_ARENAS = N.ALL_ARENAS

# 类 → (目标字段列表, 该字段的目标类型标签, 要一并记下的字段)
CLASSES = {
    'ScenarioParticleSystemBlender': ([('particleSystems', 'ps')], ['forceEnableEmittersOnEnable', 'notifyFinishWhenNoParticles']),
    'ScenarioParticleSystemToggler': ([('particleSystems', 'ps')], ['priority']),
    'ScenarioMaterialFader':         ([('renderers', 'renderer')],
                                      ['fadeOnEnable', 'isInDefaultScenario', 'restoreOriginalMaterialsOnFadeOut',
                                       'restoreOriginalMaterialsOnFadeIN', 'disableOnFadeOut']),
    'ScenarioGenericObjectToggler':  ([('gameObjects', 'go')], ['isInDefaultScenario']),
    'ScenarioParticleSpawnerBlender': ([('areaSpawners', 'spawner'), ('controllers', 'controller')], []),
    # 🆕 2026-10-07 波9离线（A136）：原版 `IScenarioEnvironmentBlendeable` 的**另 4 个实现类**
    #   —— 共 **11 个实例**，**全部在场景侧**（逐 bundle 数过：`资料/普查产出_1006/战A_第3_4条.md`「顺手发现 A」）。
    #   类名 / 字段名 / 偏移的判据 = `d:/2/tools/il2cpp_out/dump.cs`（各 TypeDefIndex 写在文件末尾那段注释里）；
    #   方法体逐句读过（件名写在 `Battle/ScenarioBlendables.cs` 各方法的注释里）。
    'FlareScenarioToggler':          ([('lensFlare', 'flare', 'single')], []),
    'ScenarioAnimationBlend':        ([('myAnimation', 'animation', 'single')], ['autoPlayWhenChange']),
    'TauCannonAnimationStopper':     ([('lookAtConstrains', 'lookat'),
                                       ('animationComponent', 'animation', 'single'),
                                       ('cannon', 'transform', 'single'),
                                       ('particleSystems', 'ps'),
                                       ('animFXController', 'animfx', 'single')], []),
    # ⚠️ `ScenarioGenericMaterialBlend` 的目标藏在**嵌套 `[Serializable]` 结构体数组**里
    #   （`RendererMaterialBlender[] renderersBlend`）⇒ 上面那条通用路解不了，走 `NESTED_TARGETS`。
    'ScenarioGenericMaterialBlend':  ([], ['fadeOnEnable', 'isInDefaultScenario']),
}

# 🆕 2026-10-07 波9离线（A136）：**该组件自己**的小数字段 → 走 `Item.floats`。
#   为什么不并进上面那个 `extra`（那个写 `Item.fields`）：那套的 `v` 是 **int**
#   —— `blendTime = 0.3` 会被截成 `0`（**静默错**，本仓红线），`finalRotation` 是 Vector3 更装不下。
#   Vector3 会拆成 `finalRotation.x/y/z` 三条（与 `TARGET_FIELDS` 的 `boxSize` 同一套写法）。
FLOAT_FIELDS = {
    'ScenarioAnimationBlend':       ['blendTime', 'filterCode'],
    'TauCannonAnimationStopper':    ['finalRotation'],   # 原版 +0x40（Vector3）
    'ScenarioGenericMaterialBlend': ['filterCode'],
}

# 🆕 2026-10-07 波9离线（A135④）：目标藏在**嵌套结构体数组**里的类。
#   `ScenarioGenericMaterialBlend.renderersBlend[]` 每条 =
#     `{renderer(PPtr), customMaterial(PPtr), propertiesToBlend(string[])}`
#   —— 数组元素**不是 PPtr**（没有 `m_PathID`）、外面还包着一层对象引用，
#   所以 `collect()` 那条「`for t in d[fname]: t['m_PathID']`」的通用路解不了它。
#   值 = (数组字段名, 元素里指目标的那个键, 元素里要带走的字符串数组键, 元素里的引用键)
NESTED_TARGETS = {
    'ScenarioGenericMaterialBlend': ('renderersBlend', 'renderer', 'propertiesToBlend', 'customMaterial'),
}

# 🆕 2026-10-06 战-A：**目标组件自己**的序列化字段 —— 只对这张表里的类收，随目标一起进旁挂
#   （落点 = `Target.fields`；字段名**照原版**，别改写）。
#   判据 = `d:/2/tools/il2cpp_out/dump.cs` 里那个类的字段表（名字 + 偏移 + 类型）：
#     `ParticleSystemAreaSpawner` TypeDefIndex 1104 —— boxSize(0x20 Vector3) · particleSystemPrefab(0x30) ·
#       maxPoolSize(0x38) · useAutomaticSpawn(0x3C) · spawnRate(0x40) · chances(0x44)（**恰好这 6 个序列化字段**，
#       `totalPoolItems`/`m_Pool`/`collectionChecks` 是运行时的，不在里面）
#     `ParticleSystemAreaSpawnerController` 1107 —— spawnRate(0x28) · startOnEnable(0x2C)
#       ⚠️ 它真正的主体是 `particleSystemAreaSpawners[]`（每条 = 引用 + weight + chances 的嵌套结构），
#       **这一层没收**（要收得再给 `Target` 加一层嵌套类型）—— 实测全库 0 个实例用到 controller（4 条 blender
#       的 `controllers` 全是空数组）⇒ 记在这里当已知缺口，别当「收全了」。
#   Vector3 会拆成 `boxSize.x/y/z` 三条；**对象引用**（`particleSystemPrefab`）写成 `s` = 落点的层级路径。
TARGET_FIELDS = {
    'ParticleSystemAreaSpawner': ['boxSize', 'particleSystemPrefab', 'maxPoolSize',
                                  'useAutomaticSpawn', 'spawnRate', 'chances'],
    'ParticleSystemAreaSpawnerController': ['spawnRate', 'startOnEnable'],
    # 🆕 2026-10-11（A196）：`TauCannonAnimationStopper` 那两种目标组件**自己**的序列化字段。
    #   为什么必须收（否则运行时不建）：`LookAtConstrainWIP.Update()` 在 `target == null` 时
    #   **每帧告警**、而且组件唯一的作用就是照着 `target` 转向 ⇒ 没有 `target` 就不该建；
    #   `AnimFXController` 更硬：默认 `preventDestroy = false` + `destroyTime = 4f` 会让 `OnEnable`
    #   **当场排定 4 秒后的自毁**，而原版 4 个实例**全是 `true`** ⇒ 拿不到 `preventDestroy` 就不该建。
    #   判据 = 签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{LookAtConstrainWIP,AnimFXController}.cs`
    #          ＋ `d:/2/tools/decomp_full/LookAtConstrainWIP__Update.c` / `AnimFXController__{OnEnable,ctor}.c`
    #          ＋ 场景 4+4 个实例（`bundle_scenes_scenes_battlearenatauviorla/MonoBehaviour/`）。
    #   `target` 是 PPtr ⇒ 走那句「对象引用 → `s` = 落点的层级路径」（`pack_fields` 里本来就有）。
    #   ⚠️ **`AnimFXController` 的 `sounds` / `exitSounds` / `modules` 三层【仍然没收】**
    #      —— 它们是数组、元素还带外部资产引用（`AudioCue` 在别的 bundle 里），`pack_fields` 解不了；
    #      要收得照 `pack_controller_defs` 再开一个 packer（走 `m_FileID` 外部引用解析 cue 名）。
    #      ⇒ 运行时建出来的 `AnimFXController` 这三项都是空的，工厂会**出声**点名这件事。
    'LookAtConstrainWIP': ['target', 'lockXAxis', 'lockYAxis', 'lockZAxis', 'upVector', 'rotationOffset'],
    'AnimFXController': ['preventDestroy', 'destroyTime', 'exitDestroyTime'],
}

# 🆕 2026-10-07 波9批二（A137）：**不被任何 blendable 引用**的 `ParticleSystemAreaSpawner` / `…Controller`。
#   全库实测 **28 + 3** 个实例（逐 bundle 数过，**全在** `battleprefabs_vfxandmisc`，场景侧 0 个），
#   其中被 `ScenarioParticleSpawnerBlender` 引用的只有 **4 个** —— 那 4 条走 `CLASSES` 那条路
#   （2026-10-06 战-A 做的），**这里必须排除**：同一个 pid 收两遍 ⇒ 运行时同一个对象上建两次组件。
#   ⇒ 本节收的是**剩下 24 条 spawner + 3 条 controller**，进旁挂的 `standalone` 一节，
#     运行时由 `EnvironmentApplier` 在环境 prefab 实例化之后**照建**（同一个 `MakeSpawner`）。
#   ⚠️ 那 24 条里只有 **16** 条 `useAutomaticSpawn = 1`（自启）；另 **8** 条 `= 0`，是**被那 3 个
#     controller 驱动**的（controller 直接调它们的 `SpawnParticle()`，不看 `useAutomaticSpawn`）
#     —— 「24 条自启」这个说法**不准确**，真值已在报告里逐条列了。
STANDALONE_CLASSES = ('ParticleSystemAreaSpawner', 'ParticleSystemAreaSpawnerController')

#   `…Controller` 的主体是 `particleSystemAreaSpawners[]`（每条 = 引用 + weight + chances 的嵌套结构）。
#   它不是 PPtr 数组（外面包着一层普通 `[Serializable]` 类）⇒ `pack_fields` 那条通用路解不了，
#   在 `pack_controller_defs` 里摊平：引用 → `spawner.<i>`（`s` = 落点的层级路径）·
#   `weight.<i>` / `chances.<i>`（`f`）。**用现成的 `TargetField` 形制装**：不新开一层嵌套类型
#   （`Core/EnvBlendables.cs` 那个类型不在本件白名单里；键名带下标、运行时按 0..N-1 顺序读、读不到就停）。
CONTROLLER_ARRAY = 'particleSystemAreaSpawners'


def load_script_names():
    """`m_Script` 的 pathID → 类名（全 84 个包共用同一张表）。"""
    out = {}
    for o in UnityPy.load(os.path.join(AA, MONO_BUNDLE)).objects:
        if o.type.name != 'MonoScript':
            continue
        try:
            d = o.read_typetree()
        except Exception:
            continue
        out[o.path_id] = d.get('m_ClassName') or d.get('m_Name')
    return out


class Bundle(object):
    """一个 bundle 的对象图：读一遍 typetree，建 名字/父子 索引。"""

    def __init__(self, path):
        self.env = UnityPy.load(path)
        self.tt = {}
        for o in self.env.objects:
            try:
                self.tt[o.path_id] = o.read_typetree()
            except Exception:
                self.tt[o.path_id] = None
        # 🆕 2026-10-07（A135④）：解 `customMaterial` 那条 `{m_FileID, m_PathID}` 要用的两张表。
        # 🔴 **`m_FileID` 是【引用者所在那份 CAB】的 externals 下标**（判据 → A152 / `_probe_deckinfo` 的 A127②）：
        #    `scenes_*` 的 15 个包是**双 CAB**（主 CAB + `.sharedAssets`），**同名 pid 在两份里可能是不同的东西**
        #    ⇒ 「按 pid 全库反查」会**静默拿错**。所以先把「谁在哪份 CAB」和「每份 CAB 的 externals」都记下来。
        self.cab_ext = {}      # CAB 名 -> [external path]
        self.by_cab = {}       # CAB 名 -> {pid: ObjectReader}
        self.pid_cabs = {}     # pid -> set(CAB 名)
        for _outer, _bf in self.env.files.items():
            for _cab, _sf in getattr(_bf, 'files', {}).items():
                objs = getattr(_sf, 'objects', None)
                if not isinstance(objs, dict):
                    continue
                self.cab_ext[_cab] = [getattr(x, 'path', '') or ''
                                      for x in (getattr(_sf, 'externals', None) or [])]
                self.by_cab[_cab] = objs
                for _pid in objs:
                    self.pid_cabs.setdefault(_pid, set()).add(_cab)
        self.warnings = []     # collect() 期间攒下的「出声」（`main()` 合进 `_unresolved`）
        self.go_name, self.tr, self.tf_of_go = {}, {}, {}
        for pid, d in self.tt.items():
            if not isinstance(d, dict):
                continue
            if 'm_Component' in d:
                self.go_name[pid] = d.get('m_Name')
            if 'm_Father' in d and 'm_Children' in d:
                self.tr[pid] = d
                g = (d.get('m_GameObject') or {}).get('m_PathID')
                if g:
                    self.tf_of_go[g] = pid

    def cab_of(self, pid):
        """pid 在哪份 CAB。**多份都有时取对象数最多的那份并出声**（不许静默挑一个）。"""
        cabs = self.pid_cabs.get(pid)
        if not cabs:
            return None
        if len(cabs) == 1:
            return next(iter(cabs))
        best = max(cabs, key=lambda c: len(self.by_cab.get(c) or {}))
        self.warnings.append('pid %s 在 %d 份 CAB 里都有（%s）—— 取对象数最多的 `%s`；'
                             '这条引用**没钉死**，要复核' % (pid, len(cabs), '、'.join(sorted(cabs)), best))
        return best

    def resolve_ref(self, ref, owner_pid):
        """`{m_FileID, m_PathID}` → `(类型名, 名字)`。
        `m_FileID == 0` ⇒ 同文件；`> 0` ⇒ **引用者自己那份 CAB** 的 `externals[m_FileID-1]` 指向的 CAB。
        解不出返回 `(None, None)`（**不猜**）。"""
        fid = (ref or {}).get('m_FileID') or 0
        pid = (ref or {}).get('m_PathID')
        if not pid:
            return None, None
        if fid == 0:
            objs = self.by_cab.get(self.cab_of(owner_pid))
        else:
            ext = self.cab_ext.get(self.cab_of(owner_pid)) or []
            if fid > len(ext):
                return None, None
            tgt = (ext[fid - 1] or '').replace('\\', '/').rstrip('/').split('/')[-1]
            objs = self.by_cab.get(tgt)
            if objs is None:
                return None, None                  # 跨包引用 —— 本件两处都不是，真遇到就如实返回 None
        o = (objs or {}).get(pid)
        if o is None:
            return None, None
        try:
            d = o.read_typetree()
        except Exception:
            return o.type.name, None
        if isinstance(d, dict) and d.get('m_Name'):
            return o.type.name, d['m_Name']
        # `Material` 这类没有 `m_Name` 的走 `m_SavedProperties`；**名字不在树里** ⇒ 只回类型名
        return o.type.name, None

    def material_of(self, ref, owner_pid):
        """`customMaterial` 那条引用 → `(资产名, 它自己的属性名集合)`；解不出 `(None, None)`。"""
        tname, mname = self.resolve_ref(ref, owner_pid)
        if tname != 'Material':
            return None, None
        pid = (ref or {}).get('m_PathID')
        fid = (ref or {}).get('m_FileID') or 0
        if fid == 0:
            objs = self.by_cab.get(self.cab_of(owner_pid))
        else:
            ext = self.cab_ext.get(self.cab_of(owner_pid)) or []
            objs = self.by_cab.get((ext[fid - 1] or '').replace('\\', '/').rstrip('/').split('/')[-1]) if fid <= len(ext) else None
        o = (objs or {}).get(pid)
        if o is None:
            return mname, None
        try:
            d = o.read_typetree()
        except Exception:
            return mname, None
        sp = (d or {}).get('m_SavedProperties') or {}
        names = set()
        for sec in ('m_TexEnvs', 'm_Floats', 'm_Colors', 'm_Ints'):
            for e in (sp.get(sec) or []):
                if isinstance(e, (list, tuple)) and e and isinstance(e[0], str):
                    names.add(e[0])
        return mname, names

    def chain(self, go_pid):
        """GameObject pid → [(名字, 世界位置)]，根在前。"""
        tpid = self.tf_of_go.get(go_pid)
        out, guard = [], 0
        while tpid and guard < 300:
            t = self.tr.get(tpid)
            if not t:
                break
            g = (t.get('m_GameObject') or {}).get('m_PathID')
            pos = self.world_pos(tpid)
            out.append((self.go_name.get(g, '?'), pos))
            nxt = (t.get('m_Father') or {}).get('m_PathID')
            tpid = nxt if nxt else None
            guard += 1
        out.reverse()
        return out

    def world_pos(self, tpid):
        M4 = [[1.0 if i == j else 0.0 for j in range(4)] for i in range(4)]
        chain, cur, guard = [], tpid, 0            # ⚠️ 键是 **int** pid（UnityPy 给的）——
        while cur in self.tr and guard < 300:      #    别照抄 `gen_arena_negscale` 那句 `str()`，
            t = self.tr[cur]                       #    那边是按文件名建的**字符串**索引，转字符串会让链整条不走
            lp = t.get('m_LocalPosition') or {}
            lq = t.get('m_LocalRotation') or {}
            ls = t.get('m_LocalScale') or {}
            chain.append(((lp.get('x', 0.0), lp.get('y', 0.0), lp.get('z', 0.0)),
                          (lq.get('x', 0.0), lq.get('y', 0.0), lq.get('z', 0.0), lq.get('w', 1.0)),
                          (ls.get('x', 1.0), ls.get('y', 1.0), ls.get('z', 1.0))))
            nxt = (t.get('m_Father') or {}).get('m_PathID')
            cur = nxt if nxt else None
            guard += 1
        for (p, q, s) in reversed(chain):
            M4 = N._mul(M4, N._local_matrix(p[0], p[1], p[2], q, s))
        return [round(M4[0][3], 4), round(M4[1][3], 4), round(M4[2][3], 4)]

    def target_owner(self, pid):
        """目标组件 pid → (它的 GameObject pid, 类型名)；拿不到返回 (None, None)。"""
        d = self.tt.get(pid)
        if not isinstance(d, dict):
            return None, None
        g = (d.get('m_GameObject') or {}).get('m_PathID')
        return g, None

    def target_fields(self, pid):
        """目标**组件自己**的序列化字段（只收 TARGET_FIELDS 里列的那几个类）。

        🆕 **2026-10-11（A192③）**：这里**统一**把 `ParticleSystemAreaSpawnerController` 的
        **嵌套那一层**（`particleSystemAreaSpawners[]`）也摊平带上（`spawner.N` / `weight.N` / `chances.N`）。
        · 原来只有 `collect_standalone` 那条路调了 `pack_controller_defs`，
          **blendable 引用的那条（`collect`）没调** ⇒ 万一某条 `ScenarioParticleSpawnerBlender.controllers`
          非空，建出来的 controller 会**一条 spawner 都没配上**（运行时只 `LogWarning`、不生成粒子）。
        · 收在这里 = **两条路共用一份**（本仓铁律 6）；`collect_standalone` 也改成转调本函数，
          产物**逐字节不变**（同一份 `pack_fields` + 同一个顺序）。
        · ⚠️ 实测今天 **4 条 blender 的 `controllers` 全是空数组** ⇒ 这一改**目前不改变任何数据**
          （`--check` 前后逐字节相同）；补它是**把两条路的判据补齐**，不是修一条已发生的缺陷。"""
        d = self.tt.get(pid)
        if not isinstance(d, dict):
            return []
        cn = self.scripts.get((d.get('m_Script') or {}).get('m_PathID'))
        out = self.pack_fields(d, TARGET_FIELDS.get(cn))
        if cn == 'ParticleSystemAreaSpawnerController':
            out += self.pack_controller_defs(d.get(CONTROLLER_ARRAY) or [])
        return out

    def self_fields(self, d, cn):
        """🆕 2026-10-07（A136）：**这个组件自己**的小数字段（走 `Item.floats`，见 `FLOAT_FIELDS`）。"""
        return self.pack_fields(d, FLOAT_FIELDS.get(cn))

    def pack_fields(self, d, names):
        """把 `names` 里那几个字段打包成 `TargetField` 那套形状（`k` / `f` / `s`）。
        值一律照原值、不做任何换算；`Vector3` 拆三轴；对象引用写成 `s` = 落点的层级路径；
        **普通字符串字段**（`filterCode` 那种）也写 `s` = 原值本身。
        ⚠️ `Target.fields` 与 `Item.floats` **共用这一份** —— 别写第二遍（本仓铁律 6）。"""
        if not names:
            return []
        out = []
        for f in names:
            v = d.get(f)
            if isinstance(v, dict) and 'm_PathID' in v:          # 对象引用（当前只有 particleSystemPrefab）
                rp = (v or {}).get('m_PathID')
                if not rp:
                    continue                                     # 空引用（原版就有）—— 跳过，别当缺口
                g, _ = self.target_owner(rp)
                ch = self.chain(g) if g else []
                if ch:
                    out.append({'k': f, 's': '/'.join(n for n, _ in ch)})
            elif isinstance(v, dict) and 'x' in v:               # Vector3 → 三轴（原版一个字段，这里拆三条）
                for ax in ('x', 'y', 'z'):
                    out.append({'k': '%s.%s' % (f, ax), 'f': float(v.get(ax, 0.0) or 0.0)})
            elif isinstance(v, bool):
                out.append({'k': f, 'f': 1.0 if v else 0.0})
            elif isinstance(v, (int, float)):
                out.append({'k': f, 'f': float(v)})
            elif isinstance(v, str) and v:                       # 普通字符串字段（`filterCode`）
                out.append({'k': f, 's': v})
        return out

    def pack_controller_defs(self, arr):
        """🆕 2026-10-07（A137）：`ParticleSystemAreaSpawnerController.particleSystemAreaSpawners[]`
        摊平成 `TargetField` 三条一组（见 `CONTROLLER_ARRAY` 那段注释）。"""
        out = []
        for i, e in enumerate(arr or []):
            if not isinstance(e, dict):
                continue
            ref = e.get('particleSystemAreaSpawner') or {}
            rp = ref.get('m_PathID')
            if rp:
                g, _ = self.target_owner(rp)
                ch = self.chain(g) if g else []
                if ch:
                    out.append({'k': 'spawner.%d' % i, 's': '/'.join(n for n, _ in ch)})
            for k in ('weight', 'chances'):
                v = e.get(k)
                out.append({'k': '%s.%d' % (k, i), 'f': float(v) if isinstance(v, (int, float)) else 0.0})
        return out

    def collect_standalone(self, scripts):
        """🆕 2026-10-07（A137）：**所有** `STANDALONE_CLASSES` 实例（不限于「是某个 blendable 的目标」），
        按根分组 → `{根名: [条目]}`。与 `collect` 的差别只有两条：
          ① 不需要有个 blendable 宿主（这一批**没有任何组件引用它们**，它们自己 `OnEnable` 起循环）；
          ② 目标 = **组件自己那个 GameObject**（`kind` 沿用 `spawner` / `controller` 两个字面量，
             与 blender 那条路的 `Target.kind` 同一套 —— 运行时建的就是同一个 `MakeSpawner`）。
        🔴 被 `ScenarioParticleSpawnerBlender` 引用的那些**排除**（那 4 条由 `collect` 那条路建，
           收两遍 = 同一个对象上建两次组件）。"""
        self.scripts = scripts
        refd = set()                                     # blender 的 `areaSpawners` / `controllers`
        for pid, d in self.tt.items():
            if not isinstance(d, dict) or 'm_Script' not in d:
                continue
            if scripts.get((d.get('m_Script') or {}).get('m_PathID')) != 'ScenarioParticleSpawnerBlender':
                continue
            for f in ('areaSpawners', 'controllers'):
                for r in (d.get(f) or []):
                    p = (r or {}).get('m_PathID')
                    if p:
                        refd.add(p)
        by_root = {}
        for pid, d in self.tt.items():
            if not isinstance(d, dict) or 'm_Script' not in d or 'm_GameObject' not in d:
                continue
            cn = scripts.get((d.get('m_Script') or {}).get('m_PathID'))
            if cn not in STANDALONE_CLASSES or pid in refd:
                continue
            ch = self.chain((d['m_GameObject'] or {}).get('m_PathID'))
            if not ch:
                continue
            path = '/'.join(n for n, _ in ch)
            # 🆕 2026-10-11（A192③）：转调 `target_fields` —— 嵌套那一层（`particleSystemAreaSpawners[]`）
            #   现在**收在那一处**（两条路共用一份），这里不再自己拼一遍（本仓铁律 6）。
            #   产物逐字节不变：同一个 `pack_fields` + 同一个「先字段、后嵌套」的顺序。
            fields = self.target_fields(pid)
            by_root.setdefault(ch[0][0], []).append({
                'cls': cn,
                'owner': path, 'ownerLeaf': ch[-1][0], 'ownerPos': ch[-1][1],
                'fields': {},                            # 这个组件自己没有 `Item.fields` 那套要记的
                'targets': [{'path': path, 'leaf': ch[-1][0], 'pos': ch[-1][1],
                             'kind': 'spawner' if cn == 'ParticleSystemAreaSpawner' else 'controller',
                             'fields': fields}],
            })
        # 排序只为「同一个 bundle 反复跑出来的文件逐字节一样」（diff 稳）；运行时不依赖这个顺序
        #   —— 它自己先建 spawner 再建 controller（controller 要引用前者的组件）。
        for _r in by_root:
            by_root[_r].sort(key=lambda i: (i['cls'], i['owner']))
        return by_root

    def collect(self, scripts, want_roots=None):
        """→ {根名: [条目]}。want_roots = None 表示全收。"""
        self.scripts = scripts
        by_root = {}
        for pid, d in self.tt.items():
            if not isinstance(d, dict) or 'm_Script' not in d or 'm_GameObject' not in d:
                continue
            cn = scripts.get((d.get('m_Script') or {}).get('m_PathID'))
            if cn not in CLASSES:
                continue
            ch = self.chain((d['m_GameObject'] or {}).get('m_PathID'))
            if not ch:
                continue
            root = ch[0][0]
            if want_roots is not None and root not in want_roots:
                continue
            tgt_fields, extra = CLASSES[cn]
            targets = []
            for spec in tgt_fields:
                # 三元组 = 这个字段是**单个引用**（不是数组）；见 `CLASSES` 里 `'single'` 那几处
                fname, kind = spec[0], spec[1]
                vals = [d.get(fname)] if (len(spec) > 2 and spec[2] == 'single') else (d.get(fname) or [])
                for t in vals:
                    if not isinstance(t, dict):
                        continue
                    tp = (t or {}).get('m_PathID')
                    if not tp:
                        continue                       # 空引用（原版就有）—— 跳过，别当缺口
                    g, _ = self.target_owner(tp)
                    tch = self.chain(g) if g else []
                    if not tch:
                        continue
                    te = {'path': '/'.join(n for n, _ in tch),
                          'leaf': tch[-1][0], 'pos': tch[-1][1], 'kind': kind}
                    tf = self.target_fields(tp)
                    # ⚠️ 空就不写这个键：128 条里只有那 4 条 spawner 目标有字段，
                    #    否则每一条 target 都多一行 `"fields": []`，把 diff 全淹掉。
                    if tf:
                        te['fields'] = tf
                    targets.append(te)
            # 🆕 2026-10-07（A135④）：**嵌套结构体数组**里的目标（`RendererMaterialBlender[]`）
            nt = NESTED_TARGETS.get(cn)
            if nt:
                targets.extend(self.collect_nested(d, pid, nt))
            entry = {
                'cls': cn,
                'owner': '/'.join(n for n, _ in ch),
                'ownerLeaf': ch[-1][0],
                'ownerPos': ch[-1][1],
                'fields': {k: d.get(k) for k in extra if k in d},
                'targets': targets,
            }
            fl = self.self_fields(d, cn)
            if fl:
                entry['floats'] = fl
            by_root.setdefault(root, []).append(entry)
        return by_root

    def collect_nested(self, d, owner_pid, spec):
        """`NESTED_TARGETS` 那一条：数组字段 `arr` 的每个元素里，`refkey` 指目标、
        `propskey` = 要带走的字符串数组、`matkey` = 那条引用的 Material（记录它的**资产名**，
        并**就地核**「`propskey` 里的每个名字都真的在那个 Material 的属性表里」）。"""
        arrkey, refkey, propskey, matkey = spec
        out = []
        rows = d.get(arrkey) or []
        for el in rows:
            if not isinstance(el, dict):
                continue
            ref = el.get(refkey)
            tp = (ref or {}).get('m_PathID')
            if not tp:
                continue
            g, _ = self.target_owner(tp)
            tch = self.chain(g) if g else []
            if not tch:
                continue
            te = {'path': '/'.join(n for n, _ in tch),
                  'leaf': tch[-1][0], 'pos': tch[-1][1], 'kind': 'renderer'}
            props = [p for p in (el.get(propskey) or []) if isinstance(p, str) and p]
            if props:
                te['blendProps'] = props
            mname, mprops = self.material_of(el.get(matkey), owner_pid)
            if mname:
                te['customMaterial'] = mname
            if not mname:
                self.warnings.append('%s 的 `%s` 解不出资产名（引用 = %s）—— 如实记「解不出」，别猜'
                                     % (arrkey, matkey, json.dumps(el.get(matkey), ensure_ascii=False)))
            elif mprops is not None:
                miss = [p for p in props if p not in mprops]
                if miss:
                    self.warnings.append('原版 Material `%s` 的属性表里**没有**这些要补间的名字：%s'
                                         % (mname, '、'.join(miss)))
            out.append(te)
        return out


def prefab_names_on_disk():
    out = {}
    for fn in os.listdir(PREFABS):
        if fn.endswith('.prefab'):
            out[fn[:-len('.prefab')]] = os.path.join(PREFABS, fn)
    return out


def norm(s):
    """名字归一化：Unity 的 YAML 会把**带尾随空格/特殊字符**的名字写成 `m_Name: 'Flames '`
    （实测 `Daemonic Feast` 那个 prefab 里就是这么写的）⇒ 比对前先去掉引号与首尾空白。
    ⚠️ 原版的名字**真的有尾随空格**，所以只在**比对**时归一，别去改原值。"""
    return (s or '').strip().strip("'").strip('"').strip()


def check_prefab_side(by_root, unresolved):
    """① prefab 在不在工程里 ② 每个目标的 leaf 名在不在那份 .prefab 文本里（按归一化名比）。"""
    disk = prefab_names_on_disk()
    missing_files, missing_names = [], []
    n_ok = 0
    for root, items in sorted(by_root.items()):
        p = disk.get(root)
        if not p:
            missing_files.append(root)
            continue
        try:
            txt = io.open(p, encoding='utf-8', errors='replace').read()
        except Exception as e:
            missing_files.append('%s（读不了：%s）' % (root, e))
            continue
        names = set(norm(m) for m in re.findall(r'^\s*m_Name:\s*(.+?)\s*$', txt, re.M))
        miss = [t['leaf'] for it in items for t in it['targets'] if norm(t['leaf']) not in names]
        # 🆕 目标组件自己引用的那条对象（现在只有 spawner 的 `particleSystemPrefab`）也得在这件 prefab 里
        #    —— 我们是**运行时**拿它当模板的（原版那条引用指的就是实例里的那个对象），不在就连模板都找不到。
        for it in items:
            for t in it['targets']:
                for f in (t.get('fields') or []):
                    s = f.get('s') or ''
                    if s and norm(s.split('/')[-1]) not in names:
                        miss.append(s.split('/')[-1])
        if miss:
            missing_names.append('%s: %s' % (root, '、'.join(sorted(set(miss))[:6])))
        else:
            n_ok += 1
    if missing_files:
        unresolved.append('perfab 不在工程里（%d）：%s' % (len(missing_files), '、'.join(missing_files)))
    if missing_names:
        unresolved.append('prefab 里找不到这些目标名（%d 件）：%s'
                          % (len(missing_names), ' ； '.join(missing_names[:8])))
    return n_ok


def arena_prefab_names(arena):
    """该场 arena prefab 里所有 GameObject 名（归一化）。
    🔴 **这才是运行时真正会去找的那棵树**（`ArenaRuntimeLoader` 实例化的就是它）——
    清单只是「建 prefab 的输入」，而 `Sun flare` / `Directional Light` 这类**不是网格也不是粒子**的
    对象**只在 prefab 里、不在清单里**（`ArenaBuilder` 是分开建它们的）。
    所以「目标在不在」的判据 = prefab ∪ 清单；prefab 没建出来时只退回清单（并出声）。"""
    p = os.path.join(ARENA_PREFABS, arena + '.prefab')
    if not os.path.isfile(p):
        return None
    txt = io.open(p, encoding='utf-8', errors='replace').read()
    return set(norm(m) for m in re.findall(r'^\s*m_Name:\s*(.+?)\s*$', txt, re.M))


def check_scene_side(by_arena, unresolved, missing_targets):
    """场景侧**只查目标**：我们的战场是平铺建的，`Scenario/Particles` 这种**分组节点不在清单里**
    （实测 13/13 场都报 owner 缺失，那是预期，不是缺口）。targets 全在清单里才算过。
    🆕 2026-10-07（A135④）：带 `blendProps` 的目标再核一条 —— **那件网格在我们 arena 清单里
    重建出来的材质，真的带这些属性名**（`_Blend` 就是这么核的）。
    🆕 2026-10-07：存在性判据从「只在清单里」改成 **prefab ∪ 清单**（见 `arena_prefab_names`）。"""
    n_ok = 0
    owners_absent = []
    prop_bad = []
    for arena, items in sorted(by_arena.items()):
        mp = os.path.join(ARENAS, arena, arena + '_manifest.json')
        if not os.path.isfile(mp):
            unresolved.append('场景侧：没有 %s' % mp)
            continue
        d = json.load(io.open(mp, encoding='utf-8'))
        have = set()
        props_of = {}                 # 归一化 go 名 -> 该对象所有子材质里出现过的属性名
        for k in ('meshes', 'particles'):
            for e in (d.get(k) or []):
                if not e.get('go'):
                    continue
                g = norm(e['go'])
                have.add(g)
                s = props_of.setdefault(g, set())
                for sm in (e.get('subMats') or []):
                    for p in (sm.get('props') or []):
                        if p.get('k'):
                            s.add(p['k'])
                for p in (e.get('props') or []):
                    if p.get('k'):
                        s.add(p['k'])
        pn = arena_prefab_names(arena)
        if pn is None:
            print('  （信息）%s 的 arena prefab 没建出来 ⇒ 存在性只按清单判（偏严）' % arena)
        else:
            have |= pn
        miss, ow = [], []
        for it in items:
            if norm(it['ownerLeaf']) not in have:
                ow.append(it['ownerLeaf'])
            for t in it['targets']:
                if norm(t['leaf']) not in have:
                    miss.append(t['leaf'])
                    # 🔴 **已声明的缺口**：目标对象我们工程里**真的没有**（不是没找到）。
                    #    与 `_unresolved` 分开记 —— 后者是「本该对得上却对不上」。
                    #    运行时那一条会**出声**（`EnvironmentApplier` 的解析器返回 null 时会报）。
                    missing_targets.append({
                        'arena': arena, 'cls': it['cls'], 'kind': t['kind'],
                        'leaf': t['leaf'], 'wantedPath': t['path'],
                        'why': '我们工程里没有这个 GameObject（原版的【分组节点】/带 Animation 的父节点；'
                               '我们的战场是 `ArenaBuilder` 按清单**平铺**建的，只建网格/粒子/灯/相机/太阳耀斑）',
                    })
                for p in (t.get('blendProps') or []):
                    if p not in props_of.get(norm(t['leaf']), set()):
                        prop_bad.append('%s：`%s` 上要补间的 `%s` 不在我们清单重建出的材质属性里'
                                        % (arena, t['leaf'], p))
        if ow:
            owners_absent.append('%s(%d)' % (arena, len(ow)))
        if miss and not prop_bad:
            pass                                     # 已进 `missing_targets`，下面统一出声
        elif prop_bad:
            pass                                     # 已进 `prop_bad`
        else:
            n_ok += 1
    if prop_bad:
        unresolved.append('🔴 材质补间的属性名对不上（%d）：%s' % (len(prop_bad), ' ； '.join(prop_bad)))
    if owners_absent:
        print('  （信息）owner 分组节点不在平铺清单里的场：%s —— 预期如此，运行时改挂到目标上'
              % '、'.join(owners_absent))
    return n_ok


def check_standalone(stan_root, unresolved, gaps):
    """🆕 2026-10-07（A137）`standalone` 一节的自检（四条）：
      ① 根在不在我们工程里 —— 不在的进 **`_standaloneMissing`（已声明的缺口）**，不是 `_unresolved`；
      ② 宿主对象（`ownerLeaf`）在那件 `.prefab` 文本里在不在；
      ③ `particleSystemPrefab` 那条引用**两条路至少通一条**：prefab 子树里（同 blender 那 4 条）
         或**工程里另一件独立 prefab**（原版这条引用指的就是 bundle 里另一个 prefab 根 —— 实测 10/24 是这种）；
      ④ controller 的 `spawner.<i>` 必须在**同一个根**的 standalone 条目里（否则运行时要的那颗找不到）。
    """
    disk = prefab_names_on_disk()
    n_ok, no_host, no_tpl, no_ref = 0, [], [], []
    for root, items in sorted(stan_root.items()):
        if root not in disk:
            gaps.append({
                'root': root,
                'n': len(items),
                'clss': sorted(set(i['cls'] for i in items)),
                'leaves': sorted(i['ownerLeaf'] for i in items),
                'why': '这件 prefab 我们工程里没有 —— 🔴 **2026-10-11 记录订正（A211，铁律 5）**：'
                       '原来这里写的是「**有意没导**」，**那是误记**。真因 = `EffectExporter.Run()` 的'
                       '「效果根」过滤器（`!childOf && GetComponentsInChildren<ParticleSystemRenderer>().Length > 0`）'
                       '把它**静默挡掉** —— 实测这件子树里 `ParticleSystemRenderer` = **0 个**'
                       '（工具 `工具/a210_a211_gap.py` 每次重跑都会重算这条）。'
                       '⚖️ 调度台已裁「**要导**」⇒ 已加进 `EffectExporter.ListedPrefabs`、走 `RunListed()` 那条正规通道。'
                       '⚠️ 那两处旧判据（`资料/战场场景线_交接.md:289` 与 `资料/普查产出_0929/进攻卡_数据表.md:30`）'
                       '也已在同一天就地订正。',
                'fix': 'EffectExporter.RunListed（已把根名加进 ListedPrefabs）',
            })
            continue
        txt = io.open(disk[root], encoding='utf-8', errors='replace').read()
        names = set(norm(m) for m in re.findall(r'^\s*m_Name:\s*(.+?)\s*$', txt, re.M))
        own = set(i['owner'] for i in items)
        bad = 0
        for it in items:
            if norm(it['ownerLeaf']) not in names:
                no_host.append('%s`%s`' % (root, it['ownerLeaf']))
                bad += 1
                continue
            for t in it['targets']:
                for f in (t.get('fields') or []):
                    s = f.get('s') or ''
                    if not s or f.get('k') != 'particleSystemPrefab':
                        continue
                    leaf = norm(s.split('/')[-1])
                    if leaf not in names and leaf not in disk:
                        no_tpl.append('%s`%s`→`%s`' % (root, it['ownerLeaf'], leaf))
                        bad += 1
            if it['cls'] == 'ParticleSystemAreaSpawnerController':
                for t in it['targets']:
                    for f in (t.get('fields') or []):
                        k = f.get('k') or ''
                        if k.startswith('spawner.') and f.get('s') not in own:
                            no_ref.append('%s`%s` 的 `%s`=%s 不在本根的 standalone 里'
                                          % (root, it['ownerLeaf'], k, f.get('s')))
                            bad += 1
        if bad == 0:
            n_ok += 1
    if no_host:
        unresolved.append('standalone：这些宿主对象在 prefab 里找不到（%d）：%s'
                          % (len(no_host), '、'.join(no_host[:8])))
    if no_tpl:
        unresolved.append('standalone：这些 `particleSystemPrefab` **两条路都不通**（%d）：%s'
                          % (len(no_tpl), '、'.join(no_tpl[:8])))
    if no_ref:
        unresolved.append('standalone：controller 引用的 spawner 不在同一根里（%d）：%s'
                          % (len(no_ref), '、'.join(no_ref[:8])))
    return n_ok


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true', help='只扫不写')
    a = ap.parse_args()

    scripts = load_script_names()
    print('类名表 %d 条' % len(scripts))

    # ---- prefab 侧 ----
    print('读 %s …' % PREFAB_BUNDLE)
    pb = Bundle(os.path.join(AA, PREFAB_BUNDLE))
    by_root = pb.collect(scripts)
    print('  prefab 侧：%d 个根带 blendable' % len(by_root))
    # 🆕 2026-10-07（A137）：同一份 Bundle 上再走一遍 —— **不被 blendable 引用**的
    #   `ParticleSystemAreaSpawner` / `…Controller`（全库实测 24 + 3，全在这个包里）。
    stan_root = pb.collect_standalone(scripts)
    n_stan_sp = sum(1 for v in stan_root.values() for i in v if i['cls'] == 'ParticleSystemAreaSpawner')
    n_stan_ct = sum(1 for v in stan_root.values() for i in v if i['cls'] == 'ParticleSystemAreaSpawnerController')
    print('  standalone：%d 个根 / %d 条（spawner %d + controller %d）'
          % (len(stan_root), n_stan_sp + n_stan_ct, n_stan_sp, n_stan_ct))

    # ---- 场景侧 ----
    by_arena = {}
    arena_warn = {}                        # 场 -> 那个 Bundle 攒下的「出声」（解引用解不出等）
    stan_scene = []                        # 🆕 A137：场景侧**不该有**这一族（实测 0 个）—— 有就出声
    for arena in ALL_ARENAS:
        bp = os.path.join(AA, 'scenes_scenes_%s.bundle' % arena)
        if not os.path.isfile(bp):
            print('  跳过 %s（没有 bundle）' % arena)
            continue
        sb = Bundle(bp)
        got = sb.collect(scripts)
        by_arena[arena] = got.get('Scenario', [])       # 战场内容都在根节点 `Scenario` 下
        arena_warn[arena] = sb.warnings
        print('  %-32s %d 个组件' % (arena, len(by_arena[arena])))
        # 🆕 A137：场景侧**不该有**这一族 —— 有就点名（运行时只接了 prefab 侧那条路）
        for r, v in sb.collect_standalone(scripts).items():
            if v:
                stan_scene.append('%s/%s（%d 条）' % (arena, r, len(v)))

    unresolved = list(pb.warnings)
    missing_targets = []
    stan_gaps = []
    ok_p = check_prefab_side(by_root, unresolved)
    ok_s = check_scene_side(by_arena, unresolved, missing_targets)
    ok_stan = check_standalone(stan_root, unresolved, stan_gaps)
    if stan_scene:
        unresolved.append('🔴 场景侧出现了 standalone spawner（%d 处）：%s —— 运行时只接了 prefab 侧那条路，'
                          '要收得先给 `EnvironmentApplier` 加场景侧的解析'
                          % (len(stan_scene), '、'.join(stan_scene)))
    for arena in sorted(arena_warn):
        for w in arena_warn[arena]:
            unresolved.append('场景侧 %s：%s' % (arena, w))
    n_items = sum(len(v) for v in by_root.values()) + sum(len(v) for v in by_arena.values())
    stats = {
        'prefab_roots': len(by_root),
        'prefab_items': sum(len(v) for v in by_root.values()),
        'prefab_roots_ok': ok_p,
        'scene_arenas': len(by_arena),
        'scene_items': sum(len(v) for v in by_arena.values()),
        'scene_arenas_ok': ok_s,
        'by_class': {},
        # 🆕 2026-10-07（A137）：`standalone` 一节（**不被 blendable 引用的** spawner / controller）
        'standalone_roots': len(stan_root),
        'standalone_roots_ok': ok_stan,
        'standalone_items': n_stan_sp + n_stan_ct,
        'standalone_spawners': n_stan_sp,
        'standalone_controllers': n_stan_ct,
        'standalone_missing_roots': len(stan_gaps),
    }
    for v in list(by_root.values()) + list(by_arena.values()):
        for it in v:
            stats['by_class'][it['cls']] = stats['by_class'].get(it['cls'], 0) + 1
    print('条目 %d · 按类 %s' % (n_items, stats['by_class']))
    print('standalone：%d 个根（%d 个对上）/ %d 条 = spawner %d + controller %d；缺根 %d'
          % (len(stan_root), ok_stan, n_stan_sp + n_stan_ct, n_stan_sp, n_stan_ct, len(stan_gaps)))
    for g in stan_gaps:
        print('  （信息）**已声明缺口** 根 `%s`（%d 条：%s）—— %s'
              % (g['root'], g['n'], '、'.join(g['leaves']), g['why']))
    if missing_targets:
        seen = []
        for m in missing_targets:
            s = '%s（%s）' % (m['leaf'], m['cls'])
            if s not in seen:
                seen.append(s)
        print('（信息）**已知缺口** %d 条：这些目标对象我们工程里没有 ⇒ 运行时那条会出声、不生效。'
              '要收得先补 `ArenaBuilder` 的建场（本批不在名单内）：%s'
              % (len(missing_targets), '、'.join(seen)))
    if unresolved:
        print('⚠️ 未对上：')
        for u in unresolved:
            print('   -', u)
    else:
        print('✅ 两侧自检全过（`_unresolved` 空；已知缺口见上面那条「（信息）」与 `_missingTargets`）')

    if a.check:
        return 0

    out = {
        '_schema': 'env_blendables/1 —— 环境混合组件（IScenarioEnvironmentBlendeable）的旁挂表；'
                   'prefabs=43 件环境 prefab（路径相对 prefab 根）· scene=13 场战场（名字+世界位置，'
                   '因为我们的战场是平铺建的）· 每个 target 的 `fields` = **那个目标组件自己的**序列化字段'
                   '（`k` 原版字段名 · `f` 数值 · `s` 引用型字段落点的层级路径；Vector3 拆 x/y/z 三条。'
                   '当前有 `ParticleSystemAreaSpawner`（6 个）· `ParticleSystemAreaSpawnerController`（2 个）·'
                   '🆕 2026-10-11（A196）`LookAtConstrainWIP`（6 个：target/lockXAxis/lockYAxis/lockZAxis/'
                   'upVector/rotationOffset）· `AnimFXController`（3 个：preventDestroy/destroyTime/exitDestroyTime）'
                   ' —— 见 gen 脚本的 TARGET_FIELDS）'
                   '· 🆕 2026-10-07：`Item.floats` = **组件自己**的小数字段（同上形制；`Field.v` 是 int、'
                   '装不下 `blendTime 0.3` 与 `finalRotation` 那种 Vector3）· `Target.blendProps` / '
                   '`Target.customMaterial` = 原版 `RendererMaterialBlender.propertiesToBlend` / '
                   '`customMaterial`（只有 `ScenarioGenericMaterialBlend` 用）'
                   '· 🆕 2026-10-07（A137）`standalone` = **不被任何 blendable 引用**的 '
                   '`ParticleSystemAreaSpawner` / `…Controller`（全库 24 + 3；形制与 `prefabs` **完全一样**，'
                   '每个条目**只有一个 target = 组件自己那个 GameObject**，`kind` 仍是 `spawner` / `controller`；'
                   '6 个（controller 是 2 个 + 摊平的 `spawner.<i>`/`weight.<i>`/`chances.<i>`）字段在 '
                   '`targets[0].fields`）—— 运行时按**同一套 `MakeSpawner`** 建，**不是** blendable'
                   '· 被 blendable 引用的那 4 条**不收进** `standalone`（收了会在同一个对象上建两次）',
        '_sources': {
            'prefab_bundle': PREFAB_BUNDLE, 'scene_bundles': 'scenes_scenes_<场>.bundle',
            'class_names': MONO_BUNDLE + ' 的 MonoScript.m_ClassName',
            'aa': AA,
            'standalone_scope': 'A137：这一族全库 28 + 3 个实例**全在** `%s` 里（2026-10-07 用独立探针'
                                '扫过 aa 下全部 84 个 bundle，场景侧 0 个）—— 本脚本只扫它已经在读的'
                                '「prefab 包 + 13 个场景包」，够用；场景侧另有一道「出现了就出声」的闸' % PREFAB_BUNDLE,
        },
        'prefabs': by_root,
        'scene': by_arena,
        # 🆕 2026-10-07（A137）：**不被 blendable 引用**的 spawner / controller（形制同 `prefabs`）
        'standalone': stan_root,
        'stats': stats,
        '_unresolved': unresolved,
        # 🆕 2026-10-07（A136）：**已声明**的目标缺口 —— 旁挂里指着的对象我们工程里真的没有
        #   （原版的【分组节点】：`Sun flare` 那种有、`Railgun Turret 1` / `Battle Arena Dark Angels baked`
        #    那种没有）。与 `_unresolved`（「本该对得上却对不上」）**分开记**，运行时那一处会出声。
        #   要补 = 改 `ArenaBuilder` 的建场（会给既有断言带出连锁）⇒ 单开一件，见波9离线报告。
        '_missingTargets': missing_targets,
        # 🆕 2026-10-07（A137）：`standalone` 里那些**整件 prefab 我们没导**的根 —— 同样是**已声明**的缺口
        #   （`Particles Orbital` 是原版的公共件，原版把它内联进别的 prefab，我们有意不单独导）
        #   ⇒ 与 `_unresolved`（「本该对得上却对不上」）分开记，运行时根本不会实例化那件 prefab。
        '_standaloneMissing': stan_gaps,
    }
    io.open(OUT, 'w', encoding='utf-8', newline='\n').write(
        json.dumps(out, ensure_ascii=False, indent=1))
    print('写出 %s' % OUT)

    # ---- 摊平版：`JsonUtility` 读不了字典，只认固定字段 + 数组（与 EnvironmentConditions.json 同一套做法）----
    def flat_tfield(f):
        """目标组件的一条字段：`k` 恒有；数值走 `f`、引用型走 `s`（见 `TARGET_FIELDS` 那段注释）。"""
        o = {'k': f['k']}
        if 'f' in f:
            o['f'] = f['f']
        if 's' in f:
            o['s'] = f['s']
        return o

    def flat_item(it):
        o = {
            'cls': it['cls'], 'owner': it['owner'], 'ownerLeaf': it['ownerLeaf'],
            'ownerPos': it['ownerPos'],
            'fields': [{'k': k, 'v': 1 if v is True else (0 if v is False else int(v))}
                       for k, v in (it.get('fields') or {}).items()],
            'targets': [flat_target(t) for t in it['targets']],
        }
        # 🆕 2026-10-07（A136）：本组件自己的**小数字段**（`Field.v` 是 int，装不下 0.3 / Vector3）
        if it.get('floats'):
            o['floats'] = [flat_tfield(f) for f in it['floats']]
        return o

    def flat_target(t):
        o = {'path': t['path'], 'leaf': t['leaf'], 'kind': t['kind'], 'pos': t['pos']}
        if t.get('fields'):
            o['fields'] = [flat_tfield(f) for f in t['fields']]
        # 🆕 2026-10-07（A135④）材质补间那一层（只有 `ScenarioGenericMaterialBlend` 有）
        if t.get('blendProps'):
            o['blendProps'] = t['blendProps']
        if t.get('customMaterial'):
            o['customMaterial'] = t['customMaterial']
        return o

    flat = {
        'prefabs': [{'root': r, 'items': [flat_item(i) for i in items]}
                    for r, items in sorted(by_root.items())],
        'scene': [{'root': a, 'items': [flat_item(i) for i in items]}
                  for a, items in sorted(by_arena.items())],
        # 🆕 2026-10-07（A137）：`standalone` 一节 —— **形制与上面两组完全一样**（`EnvBlendables.Group`），
        #   所以运行时读它不用再定义一份 DTO（`EnvironmentApplier` 直接用 `EnvBlendables.Group[]` 接）。
        #   ⚠️ 它**不进** `EnvBlendables.ForPrefab` —— 那些条目不是 blendable，混进去会让工厂对它们出声。
        'standalone': [{'root': r, 'items': [flat_item(i) for i in items]}
                       for r, items in sorted(stan_root.items())],
    }
    io.open(FLAT, 'w', encoding='utf-8', newline='\n').write(
        json.dumps(flat, ensure_ascii=False, indent=1))
    print('写出 %s（摊平版，%d prefab + %d 场 + standalone %d 根 / %d 条）'
          % (FLAT, len(flat['prefabs']), len(flat['scene']),
             len(flat['standalone']), n_stan_sp + n_stan_ct))
    return 0


if __name__ == '__main__':
    sys.exit(main())
