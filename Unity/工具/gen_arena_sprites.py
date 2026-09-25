#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把战场上**挂在 `SpriteRenderer` 上的对象**抽出来 → `arenas/<场>/<场>_sprites.json` + 贴图导到 `arenas/<场>/Sprites/`。

**为什么需要它**：战场重建器（`ArenaBuilder`）**只支持 MeshRenderer 和 ParticleSystem**，
`SpriteRenderer` **全文 0 处**支持 ⇒ 这类对象**一个都没搬进来**。自检每次点名报 `0/9`
（`BattleScene.cs` 的 missing 清单）。实测最有名的一族：`battlearenaastramilitarum` 与
`battlearenatauviorla` 各 **9 个 `Fake Light Glow`**（远景假光晕，原版挂 `MaterialFlickerEffect` 闪烁）。

数据判据（2026-09-25 查证，`assets_full` 的 `SpriteRenderer/` + `Transform/` + `Sprite/` + `Material/`）：
  · 9 个全部：`m_DrawMode = 0`(Simple) · `m_SortingLayer = 0`(Default) · `m_SortingOrder = 0` · `m_MaskInteraction = 0`
  · sprite = **`Glow Scattered`**：`m_Rect` 0,0,128,128 · `m_PixelsToUnits` **25** · `m_Pivot` (0.5,0.5) ·
    **`textureRect` = x1.03 y37.04 w125.95 h53.94**（= alpha 的紧致框，128×128 里居中）
  · 材质两组：`Fake Point Light Additive`（`Universal Render Pipeline/Particles/Unlit`）·
    `Glow Scattered Extra Color`（`Everguild/FX/Extra Color`）
  · 父链：`Fake Light Glow*` → `Particles` → `Scenario`（所以世界坐标 = local + Particles 的偏移）

用法：
    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_sprites.py                # 全 13 场
    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_sprites.py battlearenaastramilitarum
    ... --check     只报不写
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
ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']


def q4(v):
    """⚠️ 2026-09-25 踩过：**颜色是 `r/g/b/a`、旋转是 `x/y/z/w`** —— 用 x/y/z/w 读颜色会得到全 0
    （实测 9 个精灵的 `m_Color` 全变成 `(0,0,0,1)`，而原版是彩色的）。"""
    if not isinstance(v, dict):
        return [0.0, 0.0, 0.0, 1.0]
    if 'r' in v or 'g' in v or 'b' in v:
        return [v.get('r', 0.0), v.get('g', 0.0), v.get('b', 0.0), v.get('a', 1.0)]
    return [v.get('x', 0.0), v.get('y', 0.0), v.get('z', 0.0), v.get('w', 1.0)]


def v3(v):
    return [v.get('x', 0.0), v.get('y', 0.0), v.get('z', 0.0)]


IDX_CACHE = 'd:/4/_tmp_view/sprite_index.json'


def build_index(UnityPy, aa):
    """`pathID → [bundle 文件名, 类型]`（Sprite / Texture2D 都要跨包找 ——
    astra/tauviorla 的 9 个精灵里有一半引的是**别的包**里的同一张 `Glow Scattered`）。"""
    if os.path.isfile(IDX_CACHE):
        try:
            d = json.load(io.open(IDX_CACHE, encoding='utf-8'))
            if d.get('_aa') == aa:
                return d
        except Exception:
            pass
    idx = {'_aa': aa}
    for f in sorted(os.listdir(aa)):
        if not f.endswith('.bundle'):
            continue
        try:
            env = UnityPy.load(os.path.join(aa, f))
        except Exception:
            continue
        for o in env.objects:
            if o.type.name in ('Sprite', 'Texture2D', 'Material'):
                # ⚠️ 存**候选列表**：同一个 pathID 在不同包里可能撞上不同类型（实测 tauviorla 的
                # 精灵材质就撞了），只存第一条会「按类型查不到 ⇒ 判成解不出来」
                idx.setdefault(str(o.path_id), []).append([f, o.type.name])
        print('   索引中… %s' % f, end='\r')
    print('   精灵/贴图索引完成：%d 项' % (len(idx) - 1))
    io.open(IDX_CACHE, 'w', encoding='utf-8', newline='\n').write(json.dumps(idx, ensure_ascii=False))
    return idx


SHADER_DIRS = ['d:/2/新解包资源/assets_full/bundle_shaders_assets_all/Shader',
               'd:/2/新解包资源/assets_full/bundle_Warpforge_unitybuiltinassets/Shader']
_shader_cache = {}


def shader_name(pid):
    """shader pathID → 名字（从解包出来的 `Shader_<pid>.json` 的 `m_ParsedForm.m_Name` 读）。
    ⚠️ 不猜：查不到就返回 None（下游会拿它当「材质没解出来」处理）。"""
    if pid in _shader_cache:
        return _shader_cache[pid]
    nm = None
    for d in SHADER_DIRS:
        f = os.path.join(d, 'Shader_%s.json' % pid)
        if os.path.isfile(f):
            try:
                j = json.load(io.open(f, encoding='utf-8'))
                nm = ((j.get('m_ParsedForm') or {}).get('m_Name')
                      or j.get('m_Name') or None)
            except Exception:
                nm = None
            break
    _shader_cache[pid] = nm
    return nm


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
        path = os.path.join(aa, bundle)
        if not os.path.isfile(path):
            continue
        env = UnityPy.load(path)
        objs = {}
        for o in env.objects:
            try:
                objs[(o.type.name, o.path_id)] = o.read_typetree()
            except Exception:
                continue
        go = {k[1]: v for k, v in objs.items() if k[0] == 'GameObject'}
        tr = {k[1]: v for k, v in objs.items() if k[0] == 'Transform'}
        sp = {k[1]: v for k, v in objs.items() if k[0] == 'Sprite'}
        mt = {k[1]: v for k, v in objs.items() if k[0] == 'Material'}
        tx = {k[1]: v for k, v in objs.items() if k[0] == 'Texture2D'}

        def world(tid):
            """沿 m_Father 累加 local（场景里这些父链都是纯平移，缩放=1）。"""
            p = [0.0, 0.0, 0.0]
            cur = tid
            seen = 0
            while cur and cur in tr and seen < 20:
                p = [p[i] + tr[cur].get('m_LocalPosition', {}).get('xyz'[i], 0.0) for i in range(3)]
                cur = (tr[cur].get('m_Father') or {}).get('m_PathID', 0)
                seen += 1
            return p

        def active(tid):
            """沿父链把 `m_IsActive` 逐层与起来 —— 原版 `activeInHierarchy`。
            🔴 **必须判**：tauviorla 那 9 个 `Fake Light Glow` 在原版里 **activeInHierarchy=False**
            （astra 的 9 个是 True）⇒ 照清单建就会**多画 9 个发光体**（实测比从 1.017 涨到 1.029）。
            与粒子那条同一条规矩：**原版关着的，不要建**（`资料/已知的坑.md` / `ArenaBuilder` 粒子段）。"""
            cur, seen = tid, 0
            while cur and cur in tr and seen < 20:
                g = go.get((tr[cur].get('m_GameObject') or {}).get('m_PathID', 0), {})
                if g.get('m_IsActive', 1) != 1:
                    return False
                cur = (tr[cur].get('m_Father') or {}).get('m_PathID', 0)
                seen += 1
            return True

        def chain(tid):
            names, cur, seen = [], tid, 0
            while cur and cur in tr and seen < 20:
                g = (tr[cur].get('m_GameObject') or {}).get('m_PathID', 0)
                names.append(go.get(g, {}).get('m_Name', '?'))
                cur = (tr[cur].get('m_Father') or {}).get('m_PathID', 0)
                seen += 1
            return ' ← '.join(names)

        out = []
        cache = {}
        def ext_obj(path_id, kind):
            """从**别的包**里取一个对象（Sprite/Texture2D）。返回 (typetree, 包名, UnityPy 对象) 或 None。"""
            hits = idx.get(str(path_id)) or []
            cands = [h for h in hits if h[1] == kind]
            if not cands:
                return None
            b = cands[0][0]
            if b not in cache:
                try:
                    cache[b] = UnityPy.load(os.path.join(aa, b))
                except Exception:
                    cache[b] = None
            env2 = cache[b]
            if env2 is None:
                return None
            for o2 in env2.objects:
                if o2.type.name == kind and o2.path_id == path_id:
                    return (o2.read_typetree(), b, o2)
            return None

        for pid, sr in sorted(((k[1], v) for k, v in objs.items() if k[0] == 'SpriteRenderer')):
            gid = (sr.get('m_GameObject') or {}).get('m_PathID', 0)
            gname = go.get(gid, {}).get('m_Name', '?')
            spid = (sr.get('m_Sprite') or {}).get('m_PathID', 0)
            if spid == 0:
                continue                                  # 空 sprite ⇒ 原版也画不出东西（实测 `Shadow`/`CardBackShadow` 一族）
            if spid in sp:
                S = sp[spid]                              # 本包自带的那份
            else:
                S = (ext_obj(spid, 'Sprite') or (None,))[0]   # 到别的包里取
            if S is None:
                print('    ⚠️ [%s] %s: sprite pathID %s 全库都找不到 —— 跳过' % (arena, gname, spid))
                continue
            if sr.get('m_Enabled', 1) != 1:
                print('    · [%s] %s: m_Enabled=False（原版不画）—— 不建' % (arena, gname))
                continue
            tid = (go.get(gid, {}).get('m_Component') or [{}])[0].get('component', {}).get('m_PathID', 0)
            T = tr.get(tid, {})
            rect = S.get('m_Rect', {})
            texid = ((((S.get('m_RD') or {}).get('texture')) or {}).get('m_PathID', 0))
            texname = tx.get(texid, {}).get('m_Name', '')
            if not texname and texid:                          # 外链贴图 ⇒ 到别的包取名字
                hit = ext_obj(texid, 'Texture2D')
                if hit:
                    texname = hit[0].get('m_Name', '')
            mats = []
            for mref in (sr.get('m_Materials') or []):
                mpid = (mref or {}).get('m_PathID', 0)
                m = mt.get(mpid)
                if m is None:                                  # 外链材质（tauviorla 那族就是）⇒ 到别的包取
                    hit = ext_obj(mpid, 'Material')
                    m = hit[0] if hit else None
                if m is None:
                    continue
                props = []
                saved = m.get('m_SavedProperties') or {}
                for k, v in (saved.get('m_Floats') or []):
                    props.append({'k': k, 't': 'f', 'f': v})
                for k, v in (saved.get('m_Colors') or []):
                    props.append({'k': k, 't': 'c', 'c': q4(v)})
                mpid2 = (m.get('m_Shader') or {}).get('m_PathID')
                mats.append({'name': m.get('m_Name', ''), 'shader': shader_name(mpid2),
                             'shaderPathId': mpid2,
                             'queue': m.get('m_CustomRenderQueue', -1), 'props': props})
            out.append({
                'go': gname, 'chain': chain(tid),
                'pos': v3(T.get('m_LocalPosition', {})), 'rot': q4(T.get('m_LocalRotation', {})),
                'scale': v3(T.get('m_LocalScale', {})), 'world': world(tid),
                'sprite': S.get('m_Name', ''), 'ptu': S.get('m_PixelsToUnits', 100.0),
                'rect': [rect.get('x', 0), rect.get('y', 0), rect.get('width', 0), rect.get('height', 0)],
                'pivot': v3(S.get('m_Pivot', {})),
                'tex': texname, 'texPathId': texid,
                'color': q4(sr.get('m_Color', {})), 'drawMode': sr.get('m_DrawMode', 0),
                'size': v3(sr.get('m_Size', {})), 'flipX': sr.get('m_FlipX', False), 'flipY': sr.get('m_FlipY', False),
                'sortOrder': sr.get('m_SortingOrder', 0), 'sortLayer': sr.get('m_SortingLayer', 0),
                'active': active((go.get(gid, {}).get('m_Component') or [{}])[0].get('component', {}).get('m_PathID', 0)),
                'mats': mats,
            })
        print('[%s] 有 sprite 的 SpriteRenderer %d 个' % (arena, len(out)))
        for e in out:
            print('    %-20s sprite=%-14s ptu=%-4s world=%s  color=%s  mat=%s' % (
                e['go'], e['sprite'], e['ptu'], [round(x, 2) for x in e['world']],
                [round(x, 3) for x in e['color']], e['mats'][0]['name'] if e['mats'] else '?'))
        total += len(out)
        if check or not out:
            continue

        # 贴图导出（放 Sprites/，与 Textures/ 分开 —— 那边 ApplyTextureImportSettings 会强设成 Default）
        spdir = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, 'Sprites')
        os.makedirs(spdir, exist_ok=True)
        done = {}
        for e in out:
            nm = e['tex']
            if not nm or nm in done:
                continue
            o = next((o for o in env.objects if o.type.name == 'Texture2D' and o.path_id == e['texPathId']), None)
            if o is None:                                   # 外链贴图 ⇒ 到别的包里取
                hit = ext_obj(e['texPathId'], 'Texture2D')
                o = hit[2] if hit else None
            if o is None:
                print('    ⚠️ 找不到 Texture2D pathID %s（%s）' % (e['texPathId'], nm))
                continue
            f = os.path.join(spdir, nm + '.png')
            if not os.path.exists(f):
                o.read().image.save(f)
                print('    → 导出 %s' % f)
            done[nm] = True
        dst = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, arena + '_sprites.json')
        io.open(dst, 'w', encoding='utf-8', newline='\n').write(
            json.dumps({'scene': arena, 'sprites': out}, ensure_ascii=False, indent=2))
        print('    → 写 %s（%d 个）' % (dst, len(out)))
    print('合计：%d 个 sprite 对象' % total)
    return 0


if __name__ == '__main__':
    sys.exit(main())
