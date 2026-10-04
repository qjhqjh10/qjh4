#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把 4 个战场的【原版粒子材质】逐颗摊开（原文照抄，不做判断）。
解析一律按 pathID —— ⚠️ 本文件是**在【单个包内】**建表（`env.objects` 只来自那一个
`scenes_scenes_<场>.bundle`）⇒ 表里的 pid **只是包内局部编号**，别当成全局唯一键
（「`m_FileID` 不可信」**不等于**「pid 可以当唯一键」，见下面那条更正）。

⚠️ **2026-10-07 更正（铁律 5；A152 顺手发现 ① / A161 ①）**：上面那句原来写的是
   「解析一律按 pathID（这批包的 `m_FileID` **不可信** —— `gen_arena_texslots.py` 已实证）」，
   即把「`m_FileID` 不可信 **⇒ 所以按 pathID 解**」当成一条推理 —— **它不成立**：
   「fileID 不可信」与「pathID 可以当唯一键」是**两个独立问题**，前者**不蕴含**后者。
   实测 **pathID 也是分包局部的**：跨包同 pid 不同名的 GameObject **1,291** · Mesh **82** ·
   Material **18** · Texture2D **11** · Shader **6** 条（来源无一例外是 `scenes_scenes_*`，
   每个场景包的主 CAB 都从 pid=1 重新编号）。
   ⇒ 本文件的 `gos/mats/shaders/trs[o.path_id]` 是**单包内**建的 ⇒ **今天不发作**：实测那 13 场
   arena 包里，两份 CAB 之间 `(类型, pid)` 交集 = 0（唯一反例是主菜单包 `mainmenuwarpforge`
   的 128 个 GameObject）。⛔ 但这是**数据性质、不是结构保证** —— 别把「按 pathID 解」这条推理
   抄到别的工具去。
   全表与判据：`资料/已知的坑.md`「一条写在三个文件里的错推理」·
              `资料/普查产出_1006/A152_pid陷阱普查.md`（同一个理由还写在 `build_mat_index.py` /
              `gen_arena_texslots.py`，两处 2026-10-06 已由 A160 就地更正）。
"""
import io, json, os, sys
import UnityPy

AA = 'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
ARENAS = ['battlearenaleviathan', 'battlearenasororitas', 'battlearenaspacewolves',
          'battlearenatauviorla']
OUT = 'd:/4/Unity/_tmp_view/mataudit/orig_materials.json'
SCRATCH = 'd:/4/Unity/_tmp_view/mataudit'


def main():
    os.makedirs(SCRATCH, exist_ok=True)
    allout = {}
    for arena in ARENAS:
        bundle = os.path.join(AA, 'scenes_scenes_%s.bundle' % arena)
        env = UnityPy.load(bundle)
        gos, mats, shaders, trs = {}, {}, {}, {}
        for o in env.objects:
            t = o.type.name
            try:
                if t == 'GameObject':
                    gos[o.path_id] = o.read_typetree().get('m_Name', '')
                elif t == 'Material':
                    mats[o.path_id] = o.read_typetree()
                elif t == 'Shader':
                    shaders[o.path_id] = o.read_typetree().get('m_Name', '')
                elif t in ('Transform', 'RectTransform'):
                    trs[o.path_id] = o.read_typetree()
            except Exception:
                continue

        def path_of(tpid):
            names, cur, g = [], tpid, 0
            while cur and cur in trs and g < 100:
                g += 1
                names.append(gos.get((trs[cur].get('m_GameObject') or {}).get('m_PathID'), '?'))
                cur = (trs[cur].get('m_Father') or {}).get('m_PathID')
            return '/'.join(reversed(names))

        rows = []
        for o in env.objects:
            if o.type.name != 'ParticleSystemRenderer':
                continue
            try:
                pr = o.read_typetree()
            except Exception:
                continue
            goid = (pr.get('m_GameObject') or {}).get('m_PathID')
            go = gos.get(goid, '?')
            for mref in (pr.get('m_Materials') or []):
                pid = (mref or {}).get('m_PathID')
                m = mats.get(pid)
                if m is None:
                    rows.append({'go': go, 'path': path_of(None), 'matPid': pid,
                                 'mat': None, 'note': '材质不在本包'})
                    continue
                sp = m.get('m_SavedProperties') or {}
                floats = {k: v for k, v in (sp.get('m_Floats') or [])}
                colors = {k: v for k, v in (sp.get('m_Colors') or [])}
                texs = {}
                for k, v in (sp.get('m_TexEnvs') or []):
                    t2 = (v or {}).get('m_Texture') or {}
                    texs[k] = t2.get('m_PathID', 0)
                rows.append({
                    'go': go, 'matPid': pid,
                    'mat': m.get('m_Name', ''),
                    'shader': shaders.get((m.get('m_Shader') or {}).get('m_PathID'), '?'),
                    'queue': m.get('m_CustomRenderQueue'),
                    'validKw': m.get('m_ValidKeywords'),
                    'invalidKw': m.get('m_InvalidKeywords'),
                    'legacyKw': m.get('m_ShaderKeywords'),
                    'floats': floats, 'colors': colors, 'texs': texs,
                    'texKeys': list(texs.keys()),
                })
        # GO 路径（按 GameObject 的 Transform 找）
        go2tr = {}
        for tpid, tr in trs.items():
            g = (tr.get('m_GameObject') or {}).get('m_PathID')
            if g in gos:
                go2tr[g] = tpid
        for r in rows:
            for gid, tr in go2tr.items():
                if gos.get(gid) == r['go']:
                    r['path'] = path_of(tr)
                    break
        allout[arena] = rows
        print('[%s] ParticleSystemRenderer 材质引用 %d 条' % (arena, len(rows)))
    io.open(OUT, 'w', encoding='utf-8', newline='\n').write(
        json.dumps(allout, ensure_ascii=False, indent=1))
    print('→ ' + OUT)


main()
