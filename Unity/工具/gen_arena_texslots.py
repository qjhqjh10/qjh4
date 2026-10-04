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
     ⚠️ **2026-10-06 更正（A152 顺手发现 ① / A160）**：这条**推不出**「pathID 可以当唯一键」——
     实测**跨包同 pid 不同名**：Mesh 82 · Material 18 · Texture2D 11 · Shader 6 条（来源全是 `scenes_*`）。
     两个问题互相独立。本工具的做法 = **按 pathID 取候选，再按 CAB（CAB 名全局唯一）→ 包名 挑**。

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
        · `tex[pathID] = [[bundle, CAB, 贴图名], …]` —— 🔴 **保留全部候选**，不是「最后一个赢家」
        · `mat[pathID] = [[bundle, CAB, 材质名, [原版关键字]], …]`（关键字供 `gen_arena_meshkeywords.py` 用）
    🔴 **为什么要留候选**（2026-09-30 实测的真缺陷）：原来只留最后一个赢家，于是
      `battlearenadarkangels` 的 `Glow Charge Lines._SecondaryTex`（**本场包内** pathID **24** = `NoiseContrast`）
      被解成了别的包里同号的 `LUT Battle Arena Tau Viorla` ⇒ **静默拿错贴图**。
      小号 pathID 在包里**很常见**（每个 bundle 各自编号）⇒ 撞号必然发生。
      纪律 = **优先选与「材质所在包」同一个包**的那个候选（同族坑 → `资料/已知的坑.md` 的
      「解析引用一律先走 `PPtr.read()`；拿 PathID 去全库扫小号会静默拿错」）。
    🔴 **2026-10-06（A160 / A152 A3）：条目**补上 CAB**（原来只有包名）**。
      ⚠️ **A152 那句「对双 CAB 包 `prefer_bundle` 完全失效」经实测**不成立**，这里更正**：
      15 个 `scenes_scenes_*.bundle` 虽然都是双 CAB，但两份**按类型分工**、`(类型, pid)` **交集 = 0**
      （`.sharedAssets` 装 Material/Texture2D/Mesh/Shader 这类**资产**，主 CAB 装 GameObject/Transform/
      *Renderer/MonoBehaviour 这类**场景对象**；唯一例外是 `mainmenuwarpforge` 的 128 个 GameObject）
      ⇒ 对 `tex`/`mat` 两张表，**今天没有任何 pid 的同包候选会出现两次**（实测旧缓存：同包重号 = 0 条）
      ⇒ 「同包优先」这一档**仍然有分辨力**（它跨包区分 arena1 / arena2 …）。
      那这条修的是什么：**判据不完整** —— `m_FileID` 是**分包局部**的，而「包名」**不足以定位一份文件**
      （`.sharedAssets` 与主 CAB 的 `externals` 逐位不同 17/21）。记了 CAB 之后：① 挑选与
      **导出**（`export_tex`）都能钉到**同一份** CAB，不再依赖「扫到哪份算哪份」；
      ② 与正确形态（`_probe_deckinfo.object_cab()`：先钉引用者所在 CAB）对齐。撞号来源实测见下。
    （原来的缓存文件 `tex_index.json` 只有「最后一个赢家」，**格式已变** ⇒ 换文件名，旧缓存自然作废。
      `_v` 3 → 4 是**再变一次**：条目从 `[f, nm]` 变成 `[f, cab, nm]`。）
    """
    if os.path.isfile(IDX_CACHE):
        try:
            d = json.load(io.open(IDX_CACHE, encoding='utf-8'))
            if d.get('_aa') == aa and d.get('_files') and d.get('_v') == 4:
                return d
        except Exception:
            pass
    idx = {'_aa': aa, '_files': len(os.listdir(aa)), '_v': 4, 'tex': {}, 'mat': {}}
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
            cab = getattr(getattr(o, 'assets_file', None), 'name', None)
            if o.type.name == 'Texture2D':
                idx['tex'].setdefault(str(o.path_id), []).append([f, cab, nm])
            else:
                # 关键字一并收（`m_ValidKeywords` = 原版**开着**的那些；应用时是**整表替换**
                # ⇒ 没列的自然是关，正是「照原版」）
                kws = sorted(set(tt.get('m_ValidKeywords') or []))
                idx['mat'].setdefault(str(o.path_id), []).append([f, cab, nm, kws])
        if verbose:
            print('   索引中… %s' % f, end='\r')
    if verbose:
        print('   索引完成：贴图 %d 个 pathID（其中 %d 个多候选）· 材质 %d 个'
              % (len(idx['tex']), sum(1 for v in idx['tex'].values() if len(v) > 1), len(idx['mat'])))
        # 🔴 **撞号要出声**（不许静默留一堆候选让下面「第一条」悄悄赢）
        for kind, key, ni in (('贴图', 'tex', 2), ('材质', 'mat', 2)):
            amb = [(p, v) for p, v in idx[key].items() if len({x[ni] for x in v}) > 1]
            if amb:
                print('   🔴 %s：%d 个 pathID 在**不同包里名字不同**（下面按「同 CAB → 同包 → 第一条」取，'
                      '取不到同源时会点名）' % (kind, len(amb)))
                for p, v in amb[:6]:
                    print('        pid=%s → %s' % (p, ' ｜ '.join('%s/%s' % (x[0], x[ni]) for x in v[:4])))
    os.makedirs(os.path.dirname(IDX_CACHE), exist_ok=True)
    io.open(IDX_CACHE, 'w', encoding='utf-8', newline='\n').write(json.dumps(idx, ensure_ascii=False))
    return idx


def pick_cand(cands, pid, prefer_cab, prefer_bundle, what):
    """从候选里挑一条：**同 CAB → 同包 → 第一条**。

    A160：判据次序里**同 CAB 在最前**（CAB 名全局唯一，而两份 CAB 共享同一个包名字符串 ——
    原来只有「同包」这一档，对双 CAB 包**恒真**、等于没判）。走到最后那一档（= 同源信息都没有）
    且候选**不止一条**时**出声**（照 `gen_vfx_texture_mips.py:84-86` 的正例：撞号就报，不静默挑一个）。
    返回 `(条目 | None, 说明文字 | None)`。"""
    if not cands:
        return None, None
    for c in cands:
        if prefer_cab is not None and c[1] == prefer_cab:
            return c, None
    for c in cands:
        if prefer_bundle is not None and c[0] == prefer_bundle:
            return c, None
    why = None
    if len(cands) > 1:
        why = ('⚠️ %s pathID %s 有 %d 个候选、且**都不同源**（引用者 CAB=%s / 包=%s）'
               '⇒ 只能取第一条：%s'
               % (what, pid, len(cands), prefer_cab, prefer_bundle,
                  ' ｜ '.join('%s/%s=%s' % (x[0], x[1], x[2] if len(x) > 2 else '') for x in cands[:4])))
    return cands[0], why


def export_tex(UnityPy, aa, bundle, name, dst_dir, cab=None):
    """把某包（可指定 CAB）里的某张贴图导成 PNG（已存在就跳过）。返回是否成功。

    A160：`cab` 给了就**只认那一份 CAB 里的同名贴图** —— 双 CAB 包里两份都可能有同名贴图，
    只按名字 + 包名取会静默导错那一张（这里导出去的 PNG 是要进工程的）。"""
    path = os.path.join(dst_dir, name + '.png')
    if os.path.exists(path):
        return True
    try:
        env = UnityPy.load(os.path.join(aa, bundle))
        for o in env.objects:
            if o.type.name != 'Texture2D':
                continue
            if cab is not None and getattr(getattr(o, 'assets_file', None), 'name', None) != cab:
                continue
            d = o.read()
            if getattr(d, 'm_Name', '') == name:
                d.image.save(path)
                return True
        if cab is not None:                     # 退一步：那一份里没有就按包再找一次（**要说出来**）
            print('    ⚠️ `%s` 在 `%s` 的 CAB `%s` 里没有同名贴图 —— 退回按包名找'
                  % (name, bundle, cab))
            return export_tex(UnityPy, aa, bundle, name, dst_dir, cab=None)
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
                    # A160：连**它自己那份 CAB** 一起记（本场包是双 CAB，本地材质也可能来自任一份）
                    mats[o.path_id] = (o.read_typetree(),
                                       getattr(getattr(o, 'assets_file', None), 'name', None))
            except Exception:
                continue

        # 🆕 2026-09-30：**外链材质**（`Glow Additive` / `Lightning Burst Random add` 这类粒子材质
        #   一大半不在本场包里）⇒ 按全局材质索引去**宿主包**取；贴图再**优先宿主包内**解析。
        _ext = {}

        def get_material(pid, prefer_cab):
            """→ (typetree, 宿主包名, 宿主 CAB)；取不到 (None, None, None)。

            `prefer_cab` = **引用者（`MeshRenderer`/`ParticleSystemRenderer`）自己那份 CAB**
            （A160：本场包是双 CAB，`m_FileID` / 同源判据都要认这一份）。"""
            m = mats.get(pid)
            if m is not None:
                return m[0], bundle, m[1]
            cands = (idx.get('mat') or {}).get(str(pid))
            if not cands:
                return None, None, None
            pick, why = pick_cand(cands, pid, prefer_cab, bundle, '材质')
            if why:
                print('    ' + why)
            if pick is None:
                return None, None, None
            f2, c2 = pick[0], pick[1]
            key = (f2, c2)
            if key not in _ext:
                try:
                    e2 = UnityPy.load(os.path.join(aa, f2))
                    _ext[key] = {o.path_id: o.read_typetree()
                                 for o in e2.objects
                                 if o.type.name == 'Material'
                                 and getattr(getattr(o, 'assets_file', None), 'name', None) == c2}
                except Exception as e:
                    print('    ⚠️ 打开外链包 `%s`（CAB %s）失败：%s' % (f2, c2, e))
                    _ext[key] = {}
            return _ext[key].get(pid), f2, c2

        def get_tex(pid, prefer_bundle, prefer_cab):
            """贴图 → (宿主包, 宿主 CAB, 名字)。**优先同 CAB → 再同包**的候选（小号 pathID 会撞号）。"""
            cands = (idx.get('tex') or {}).get(str(pid))
            if not cands:
                return None, None, None
            pick, why = pick_cand(cands, pid, prefer_cab, prefer_bundle, '贴图')
            if why:
                print('    ' + why)
            if pick is None:
                return None, None, None
            return pick[0], pick[1], pick[2]

        def slots_of(m, host, host_cab):
            """材质 → [{slot, mat, host, cab, tex}]（非主贴图槽；解不出贴图的 tex/host 为 None）。"""
            got = []
            for name, te in ((m.get('m_SavedProperties') or {}).get('m_TexEnvs') or []):
                if name in PRIMARY or name.startswith(SKIP_SLOT):
                    continue
                pid = ((te or {}).get('m_Texture') or {}).get('m_PathID', 0)
                if pid == 0:
                    continue
                b, c, n = get_tex(pid, host, host_cab)
                got.append({'slot': name, 'mat': m.get('m_Name', ''), 'host': b, 'cab': c, 'tex': n})
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
            rcab = getattr(getattr(o, 'assets_file', None), 'name', None)   # A160：引用者自己那份 CAB
            for mref in (mr.get('m_Materials') or []):
                # 一律按 pathID 解析（这批包 `m_FileID` **不可信** —— 但**不等于** pathID 可以当全局唯一键：
                # 见 `资料/已知的坑.md`「一条写在三个文件里的错推理」+ A160）
                m, host, hcab = get_material((mref or {}).get('m_PathID'), rcab)
                if m is None:
                    continue
                slots += slots_of(m, host, hcab)
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
            rcab = getattr(getattr(o, 'assets_file', None), 'name', None)   # A160：引用者自己那份 CAB
            for mref in (rr.get('m_Materials') or []):
                mpid = (mref or {}).get('m_PathID', 0)
                m, host, hcab = get_material(mpid, rcab)
                if m is None:
                    # 🔴 **不许静默**：材质哪里都找不到 ⇒ 这个对象的槽我们看不见，要报出来
                    if mpid:
                        ps_missing.setdefault(go, []).append(mpid)
                    continue
                slots += slots_of(m, host, hcab)
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
                    if not export_tex(UnityPy, aa, s['host'], s['tex'], texdir, cab=s.get('cab')):
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
