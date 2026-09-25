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
IDX_CACHE = 'd:/4/_tmp_view/tex_index.json'
ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']
PRIMARY = ('_BaseMap', '_MainTex')
SKIP_SLOT = ('unity_',)


def build_index(UnityPy, aa, verbose=True):
    """pathID → [bundle 文件名, 贴图名]（**全局**，因为外链贴图可能在任何包里）。"""
    if os.path.isfile(IDX_CACHE):
        try:
            d = json.load(io.open(IDX_CACHE, encoding='utf-8'))
            if d.get('_aa') == aa and d.get('_files'):
                return d
        except Exception:
            pass
    idx = {'_aa': aa, '_files': len(os.listdir(aa))}
    for f in sorted(os.listdir(aa)):
        if not f.endswith('.bundle'):
            continue
        try:
            env = UnityPy.load(os.path.join(aa, f))
        except Exception:
            continue
        for o in env.objects:
            if o.type.name != 'Texture2D':
                continue
            try:
                nm = o.read_typetree().get('m_Name', '')
            except Exception:
                continue
            if nm:
                idx[str(o.path_id)] = [f, nm]
        if verbose:
            print('   索引中… %s' % f, end='\r')
    if verbose:
        print('   索引完成：%d 张贴图' % (len(idx) - 2))
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
                m = mats.get((mref or {}).get('m_PathID'))       # 一律按 pathID 解析（fileID 不可信）
                if m is None:
                    continue
                for name, te in ((m.get('m_SavedProperties') or {}).get('m_TexEnvs') or []):
                    if name in PRIMARY or name.startswith(SKIP_SLOT):
                        continue
                    pid = ((te or {}).get('m_Texture') or {}).get('m_PathID', 0)
                    if pid == 0:
                        continue
                    hit = idx.get(str(pid))
                    slots.append({'slot': name, 'mat': m.get('m_Name', ''),
                                  'host': hit[0] if hit else None, 'tex': hit[1] if hit else None})
            if slots:
                out[go] = slots

        print('[%s] 有非主贴图槽的网格 %d 个' % (arena, len(out)))
        for go, slots in sorted(out.items()):
            for s in slots:
                print('    %-26s %-14s ← %s %s（%s）' % (
                    go, s['slot'], s['tex'] or '❌解不出', '(本包)' if s['host'] == bundle else '(' + str(s['host']) + ')', s['mat']))
        total += len(out)
        if check or not out:
            continue

        texdir = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, 'Textures')
        os.makedirs(texdir, exist_ok=True)
        keep_meshes = []
        for go, slots in sorted(out.items()):
            keep = []
            for s in slots:
                if not s['tex'] or not s['host']:
                    continue
                if not export_tex(UnityPy, aa, s['host'], s['tex'], texdir):
                    continue
                keep.append({'slot': s['slot'], 'tex': s['tex'], 'texFile': s['tex'] + '.png'})
            if keep:
                keep_meshes.append({'go': go, 'slots': keep})
        dst = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, arena + '_texslots.json')
        io.open(dst, 'w', encoding='utf-8', newline='\n').write(
            json.dumps({'scene': arena, 'meshes': keep_meshes}, ensure_ascii=False, indent=2))
        print('    → 写 %s（%d 个网格）' % (dst, len(keep_meshes)))
    print('合计：%d 个网格有非主贴图槽' % total)
    return 0


if __name__ == '__main__':
    sys.exit(main())
