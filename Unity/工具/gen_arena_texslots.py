#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""抽出**材质上除主贴图之外的贴图槽** → `arenas/<场>/<场>_texslots.json`，并把贴图导出到 `arenas/<场>/Textures/`。

**为什么需要它**（2026-09-25 查 `battlearenadarkangels` 时发现）：
清单生成器只导**主贴图**（`_BaseMap` / `_MainTex`）。而原版有一批材质**主贴图本来就是空的**，
内容全在**别的槽**里。实测铁证 —— `battlearenadarkangels` 的 `Background space noise`：
它用的材质 `Glow Space Dark Angels`（包里 path_id 9）`_MainTex` = `PathID 0`（空），
真数据在 **`_NoiseTex1` / `_NoiseTex2`**（两个都指向 pathID `-8214795764591836323` = **`Noise Combined`**，128×128，
宿主包 **`duplicateassetisolation_assets_all.bundle`**，**不在本场包里**）。
丢了它 ⇒ 我们渲出来是**品红**的天 `(180,118,190)`，原版是蓝的 `(62,112,187)`；
`WF_HIDE==Background space noise` 之后我们变成 `(63,110,186)` —— **与原版逐值吻合** ⇒ 根因钉死。

⚠️ **两条查证纪律**（都踩过）：
  ① **别按槽名猜贴图** —— 同包里就有一张名字更像的 `NoiseContrast.png`，**但不是它**；
     判据只能是 pathID 到包里去查（本工具就是干这个）。
  ② **`m_FileID` 在这批包里不可信**（同一份里，MeshRenderer → 材质是 `fileID 2`、材质 → 外链贴图是 `fileID 10`，
     而两者都能在本地/总表里按 `pathID` 查到）⇒ **一律按 `pathID` 解析**。

**与生成器的关系**：**本工具不改清单、只产旁挂文件**（清单是生成物，手改会被下次重跑抹掉）；
`ArenaBuilder` 读这个旁挂文件（有就套、没有就当没有）。

用法：
    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_texslots.py                 # 全 13 场
    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_texslots.py battlearenadarkangels
    ... --check    只报不写（不导出贴图、不写 json）
"""
import io
import json
import os
import sys

ROOT = "d:/4/Unity"
AA_DIRS = [
    'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64',
    'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64',
]
IDX_CACHE = 'd:/4/_tmp_view/tex_mat_index.json'
ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']
PRIMARY = ('_BaseMap', '_MainTex')
SKIP_SLOT = ('unity_',)


def build_index(UnityPy, aa, verbose=True):
    """**一次遍历建两份索引**：
        · `tex[pathID] = [[bundle, 贴图名], …]` —— 🔴 **保留全部候选**，不是「最后一个赢家」
        · `mat[pathID] = [[bundle, 材质名, [原版关键字]], …]`（关键字供 `gen_arena_meshkeywords.py` 用）
    🔴 **为什么要留候选**（2026-09-30 实测的真缺陷）：原来只留最后一个赢家，于是
      `battlearenadarkangels` 的 `Glow Charge Lines._SecondaryTex`（**本场包内** pathID **24** = `NoiseContrast`）
      被解成了别的包里同号的 `LUT Battle Arena Tau Viorla` ⇒ **静默拿错贴图**。
      小号 pathID 在包里**很常见**（每个 bundle 各自编号）⇒ 撞号必然发生。
      纪律 = **优先选与「材质所在包」同一个包**的那个候选（同族坑 → `资料/已知的坑.md` 的
      「解析引用一律先走 `PPtr.read()`；拿 PathID 去全库扫小号会静默拿错」）。
    （原来的缓存文件 `tex_index.json` 只有「最后一个赢家」，**格式已变** ⇒ 换文件名，旧缓存自然作废。）
    """
    if os.path.isfile(IDX_CACHE):
        try:
            d = json.load(io.open(IDX_CACHE, encoding='utf-8'))
            if d.get('_aa') == aa and d.get('_files') and d.get('_v') == 3:
                return d
        except Exception:
            pass
    idx = {'_aa': aa, '_files': len(os.listdir(aa)), '_v': 3, 'tex': {}, 'mat': {}}
    for f in sorted(os.listdir(aa)):
        if not f.endswith('.bundle'):
            continue
        try:
            env = UnityPy.load(os.path.join(aa, f))
        except Exception:
            continue
        for o in env.objects:
            if o.type.name not in ('Texture2D', 'Material'):
                continue
            try:
                tt = o.read_typetree()
                nm = tt.get('m_Name', '')
            except Exception:
                continue
            if not nm:
                continue
            if o.type.name == 'Texture2D':
                idx['tex'].setdefault(str(o.path_id), []).append([f, nm])
            else:
                # 关键字一并收（`m_ValidKeywords` = 原版**开着**的那些；应用时是**整表替换**
                # ⇒ 没列的自然是关，正是「照原版」）
                kws = sorted(set(tt.get('m_ValidKeywords') or []))
                idx['mat'].setdefault(str(o.path_id), []).append([f, nm, kws])
        if verbose:
            print('   索引中… %s' % f, end='\r')
    if verbose:
        print('   索引完成：贴图 %d 个 pathID（其中 %d 个多候选）· 材质 %d 个'
              % (len(idx['tex']), sum(1 for v in idx['tex'].values() if len(v) > 1), len(idx['mat'])))
    os.makedirs(os.path.dirname(IDX_CACHE), exist_ok=True)
    io.open(IDX_CACHE, 'w', encoding='utf-8', newline='\n').write(json.dumps(idx, ensure_ascii=False))
    return idx


def export_tex(UnityPy, aa, bundle, name, dst_dir):
    """把某包里的某张贴图导成 PNG（已存在就跳过）。返回是否成功。"""
    path = os.path.join(dst_dir, name + '.png')
    if os.path.exists(path):
        return True
    try:
        env = UnityPy.load(os.path.join(aa, bundle))
        for o in env.objects:
            if o.type.name != 'Texture2D':
                continue
            d = o.read()
            if getattr(d, 'm_Name', '') == name:
                d.image.save(path)
                return True
    except Exception as e:
        print('    ⚠️ 导出 `%s` 失败：%s' % (name, e))
    return False


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
    if aa is None:
        print('找不到 aa 目录（原始 bundle）')
        return 1
    idx = build_index(UnityPy, aa)

    total = 0
    for arena in arenas:
        bundle = 'scenes_scenes_%s.bundle' % arena
        if not os.path.isfile(os.path.join(aa, bundle)):
            print('[%s] 没有 bundle' % arena)
            continue
        env = UnityPy.load(os.path.join(aa, bundle))
        go_names, mats = {}, {}
        for o in env.objects:
            try:
                if o.type.name == 'GameObject':
                    go_names[o.path_id] = o.read_typetree().get('m_Name', '')
                elif o.type.name == 'Material':
                    mats[o.path_id] = o.read_typetree()
            except Exception:
                continue

        # 🆕 2026-09-30：**外链材质**（`Glow Additive` / `Lightning Burst Random add` 这类粒子材质
        #   一大半不在本场包里）⇒ 按全局材质索引去**宿主包**取；贴图再**优先宿主包内**解析。
        _ext = {}

        def get_material(pid):
            """→ (typetree, 宿主包名)；取不到 (None, None)。"""
            m = mats.get(pid)
            if m is not None:
                return m, bundle
            cands = (idx.get('mat') or {}).get(str(pid))
            if not cands:
                return None, None
            host = cands[0][0]
            if host not in _ext:
                try:
                    e2 = UnityPy.load(os.path.join(aa, host))
                    _ext[host] = {o.path_id: o.read_typetree()
                                  for o in e2.objects if o.type.name == 'Material'}
                except Exception as e:
                    print('    ⚠️ 打开外链包 `%s` 失败：%s' % (host, e))
                    _ext[host] = {}
            return _ext[host].get(pid), host

        def get_tex(pid, prefer_bundle):
            """贴图 → (宿主包, 名字)。**优先与材质同包**的候选（小号 pathID 会撞号）。"""
            cands = (idx.get('tex') or {}).get(str(pid))
            if not cands:
                return None, None
            for b, n in cands:
                if b == prefer_bundle:
                    return b, n
            return cands[0][0], cands[0][1]

        def slots_of(m, host):
            """材质 → [{slot, mat, host, tex}]（非主贴图槽；解不出贴图的 tex/host 为 None）。"""
            got = []
            for name, te in ((m.get('m_SavedProperties') or {}).get('m_TexEnvs') or []):
                if name in PRIMARY or name.startswith(SKIP_SLOT):
                    continue
                pid = ((te or {}).get('m_Texture') or {}).get('m_PathID', 0)
                if pid == 0:
                    continue
                b, n = get_tex(pid, host)
                got.append({'slot': name, 'mat': m.get('m_Name', ''), 'host': b, 'tex': n})
            return got

        out = {}
        for o in env.objects:
            if o.type.name != 'MeshRenderer':
                continue
            try:
                mr = o.read_typetree()
            except Exception:
                continue
            go = go_names.get((mr.get('m_GameObject') or {}).get('m_PathID'), '')
            if not go:
                continue
            slots = []
            for mref in (mr.get('m_Materials') or []):
                m, host = get_material((mref or {}).get('m_PathID'))   # 一律按 pathID 解析（fileID 不可信）
                if m is None:
                    continue
                slots += slots_of(m, host)
            if slots:
                out[go] = slots

        # 🆕 2026-09-30：**粒子那一支** —— 同一件事，渲染器换成 `ParticleSystemRenderer`。
        #   为什么必须补（判据 → `资料/战场场景线_交接.md` §二 ⑨）：
        #   以前粒子的材质在建场期只能用兜底 `URP/Particles/Unlit`，原版的那些槽
        #   （`_EmissionMap` / `_SecondaryTex` / `_DistortTex` / `_NoiseTex1,2` / `_BumpMap` / `_MinTex` / `_Mask`）
        #   在兜底 shader 上**根本不存在** ⇒ 没接也看不出来；⑨ 之后粒子改用**原版 shader** 重建
        #   ⇒ 这些槽第一次真正被采样，而**没接 = 采样 shader 自带的默认图（多为白）**。
        #   13 场普查实测：**35 份「场×材质」带非主槽（去重 21 份材质）、共 40 个槽**，
        #   槽名跨场高度重复（`_EmissionMap` 11 场 / 25 份）⇒ 一次性全接。
        #   ⚠️ 与网格那一支**分开存**（`particles` 键，按 `go` 索引）—— 两边可能撞名。
        ps_out, ps_missing = {}, {}
        for o in env.objects:
            if o.type.name != 'ParticleSystemRenderer':
                continue
            try:
                rr = o.read_typetree()
            except Exception:
                continue
            go = go_names.get((rr.get('m_GameObject') or {}).get('m_PathID'), '')
            if not go:
                continue
            slots = []
            for mref in (rr.get('m_Materials') or []):
                mpid = (mref or {}).get('m_PathID', 0)
                m, host = get_material(mpid)
                if m is None:
                    # 🔴 **不许静默**：材质哪里都找不到 ⇒ 这个对象的槽我们看不见，要报出来
                    if mpid:
                        ps_missing.setdefault(go, []).append(mpid)
                    continue
                slots += slots_of(m, host)
            if slots:
                ps_out[go] = slots

        for go, pids in sorted(ps_missing.items()):
            print('    ⚠️ 粒子 `%s`：材质**哪个包里都找不到**（pathID %s）—— 这一颗的槽看不到'
                  % (go, ','.join(str(p) for p in pids)))

        print('[%s] 有非主贴图槽的：网格 %d 个 · 粒子 %d 个' % (arena, len(out), len(ps_out)))
        for kind, tab in (('网格', out), ('粒子', ps_out)):
            for go, slots in sorted(tab.items()):
                for s in slots:
                    print('    [%s] %-26s %-16s ← %s %s（%s）' % (
                        kind, go, s['slot'], s['tex'] or '❌解不出', '(本包)' if s['host'] == bundle else '(' + str(s['host']) + ')', s['mat']))
        total += len(out)
        if check or not out:
            continue

        texdir = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, 'Textures')
        os.makedirs(texdir, exist_ok=True)

        def keep_and_export(tab):
            """把能导出贴图的槽留下来 → `[{go, slots:[{slot,tex,texFile}]}]`。
            ⚠️ 解不出贴图的槽**要报**（不许静默丢）。"""
            keep_list = []
            for go, slots in sorted(tab.items()):
                keep = []
                for s in slots:
                    if not s['tex'] or not s['host']:
                        print('    ⚠️ %s：槽 `%s` 的贴图按 pathID 解不出（材质 `%s`）—— 跳过'
                              % (go, s['slot'], s['mat']))
                        continue
                    if not export_tex(UnityPy, aa, s['host'], s['tex'], texdir):
                        continue
                    keep.append({'slot': s['slot'], 'tex': s['tex'], 'texFile': s['tex'] + '.png'})
                if keep:
                    keep_list.append({'go': go, 'slots': keep})
            return keep_list

        keep_meshes = keep_and_export(out)
        keep_ps = keep_and_export(ps_out)          # 🆕 粒子那一支（2026-09-30）
        dst = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, arena + '_texslots.json')
        io.open(dst, 'w', encoding='utf-8', newline='\n').write(
            json.dumps({'scene': arena, 'meshes': keep_meshes, 'particles': keep_ps},
                       ensure_ascii=False, indent=2))
        print('    → 写 %s（网格 %d 个 · 粒子 %d 个）' % (dst, len(keep_meshes), len(keep_ps)))
    print('合计：%d 个网格有非主贴图槽' % total)
    return 0


if __name__ == '__main__':
    sys.exit(main())
