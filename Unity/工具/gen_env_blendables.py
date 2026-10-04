#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_env_blendables.py —— 把「环境混合组件」（blendable）抽成旁挂 JSON。

为什么要它（判据 → `项目任务.md` §三 第 30 条「4 环境」· `资料/加时与冲突模式_原版规格.md`）：
  原版换环境那条链是 `ApplyEnvironment` → `EnableEnvironment`：
    ① 雾/环境光补间
    ② `scenarioObjects` prefab 实例化 → 对它子树里每个 `IScenarioEnvironmentBlendeable` 调 `DoScenarioBlend(opts)`
    ③ 再对【场景里已登记的那些】调一遍（方向 = `SO.defaultScenarioObjectsState`）
  这族组件有 **5 个类**，我们要**旁挂 + 运行时挂组件**复刻 ⇒ 先把
  「哪个 prefab / 哪一场的哪个对象挂着它、它管着哪些粒子/渲染器/物体」抽出来。

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
}


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
        值一律照原值，不做任何换算；`Vector3` 拆三轴；对象引用写成 `s` = 落点的层级路径。"""
        d = self.tt.get(pid)
        if not isinstance(d, dict):
            return []
        cn = self.scripts.get((d.get('m_Script') or {}).get('m_PathID'))
        names = TARGET_FIELDS.get(cn)
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
        return out

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
            for fname, kind in tgt_fields:
                for t in (d.get(fname) or []):
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
            entry = {
                'cls': cn,
                'owner': '/'.join(n for n, _ in ch),
                'ownerLeaf': ch[-1][0],
                'ownerPos': ch[-1][1],
                'fields': {k: d.get(k) for k in extra if k in d},
                'targets': targets,
            }
            by_root.setdefault(root, []).append(entry)
        return by_root


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


def check_scene_side(by_arena, unresolved):
    """场景侧**只查目标**：我们的战场是平铺建的，`Scenario/Particles` 这种**分组节点不在清单里**
    （实测 12/13 场都报 owner 缺失，那是预期，不是缺口）。targets 全在清单里才算过。"""
    n_ok = 0
    owners_absent = []
    for arena, items in sorted(by_arena.items()):
        mp = os.path.join(ARENAS, arena, arena + '_manifest.json')
        if not os.path.isfile(mp):
            unresolved.append('场景侧：没有 %s' % mp)
            continue
        d = json.load(io.open(mp, encoding='utf-8'))
        have = set()
        for k in ('meshes', 'particles'):
            for e in (d.get(k) or []):
                if e.get('go'):
                    have.add(norm(e['go']))
        miss, ow = [], []
        for it in items:
            if norm(it['ownerLeaf']) not in have:
                ow.append(it['ownerLeaf'])
            for t in it['targets']:
                if norm(t['leaf']) not in have:
                    miss.append(t['leaf'])
        if ow:
            owners_absent.append('%s(%d)' % (arena, len(ow)))
        if miss:
            unresolved.append('场景侧 %s：清单里没有这些**目标**（%d）—— %s'
                              % (arena, len(set(miss)), '、'.join(sorted(set(miss))[:8])))
        else:
            n_ok += 1
    if owners_absent:
        print('  （信息）owner 分组节点不在平铺清单里的场：%s —— 预期如此，运行时改挂到目标上'
              % '、'.join(owners_absent))
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

    # ---- 场景侧 ----
    by_arena = {}
    for arena in ALL_ARENAS:
        bp = os.path.join(AA, 'scenes_scenes_%s.bundle' % arena)
        if not os.path.isfile(bp):
            print('  跳过 %s（没有 bundle）' % arena)
            continue
        sb = Bundle(bp)
        got = sb.collect(scripts)
        by_arena[arena] = got.get('Scenario', [])       # 战场内容都在根节点 `Scenario` 下
        print('  %-32s %d 个组件' % (arena, len(by_arena[arena])))

    unresolved = []
    ok_p = check_prefab_side(by_root, unresolved)
    ok_s = check_scene_side(by_arena, unresolved)
    n_items = sum(len(v) for v in by_root.values()) + sum(len(v) for v in by_arena.values())
    stats = {
        'prefab_roots': len(by_root),
        'prefab_items': sum(len(v) for v in by_root.values()),
        'prefab_roots_ok': ok_p,
        'scene_arenas': len(by_arena),
        'scene_items': sum(len(v) for v in by_arena.values()),
        'scene_arenas_ok': ok_s,
        'by_class': {},
    }
    for v in list(by_root.values()) + list(by_arena.values()):
        for it in v:
            stats['by_class'][it['cls']] = stats['by_class'].get(it['cls'], 0) + 1
    print('条目 %d · 按类 %s' % (n_items, stats['by_class']))
    if unresolved:
        print('⚠️ 未对上：')
        for u in unresolved:
            print('   -', u)
    else:
        print('✅ 两侧自检全过')

    if a.check:
        return 0

    out = {
        '_schema': 'env_blendables/1 —— 环境混合组件（IScenarioEnvironmentBlendeable）的旁挂表；'
                   'prefabs=43 件环境 prefab（路径相对 prefab 根）· scene=13 场战场（名字+世界位置，'
                   '因为我们的战场是平铺建的）· 每个 target 的 `fields` = **那个目标组件自己的**序列化字段'
                   '（`k` 原版字段名 · `f` 数值 · `s` 引用型字段落点的层级路径；Vector3 拆 x/y/z 三条。'
                   '当前只有 `ParticleSystemAreaSpawner` 那 6 个字段用得上 · 见 gen 脚本的 TARGET_FIELDS）',
        '_sources': {
            'prefab_bundle': PREFAB_BUNDLE, 'scene_bundles': 'scenes_scenes_<场>.bundle',
            'class_names': MONO_BUNDLE + ' 的 MonoScript.m_ClassName',
            'aa': AA,
        },
        'prefabs': by_root,
        'scene': by_arena,
        'stats': stats,
        '_unresolved': unresolved,
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
        return {
            'cls': it['cls'], 'owner': it['owner'], 'ownerLeaf': it['ownerLeaf'],
            'ownerPos': it['ownerPos'],
            'fields': [{'k': k, 'v': 1 if v is True else (0 if v is False else int(v))}
                       for k, v in (it['fields'] or {}).items()],
            'targets': [flat_target(t) for t in it['targets']],
        }

    def flat_target(t):
        o = {'path': t['path'], 'leaf': t['leaf'], 'kind': t['kind'], 'pos': t['pos']}
        if t.get('fields'):
            o['fields'] = [flat_tfield(f) for f in t['fields']]
        return o

    flat = {
        'prefabs': [{'root': r, 'items': [flat_item(i) for i in items]}
                    for r, items in sorted(by_root.items())],
        'scene': [{'root': a, 'items': [flat_item(i) for i in items]}
                  for a, items in sorted(by_arena.items())],
    }
    io.open(FLAT, 'w', encoding='utf-8', newline='\n').write(
        json.dumps(flat, ensure_ascii=False, indent=1))
    print('写出 %s（摊平版，%d prefab + %d 场）'
          % (FLAT, len(flat['prefabs']), len(flat['scene'])))
    return 0


if __name__ == '__main__':
    sys.exit(main())
