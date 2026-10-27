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

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_sprites.py                # 全 13 场\n    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_sprites.py battlearenaastramilitarum\n    ... --check     只报不写\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

## 🔴 2026-10-27（B9_W59ARENA）`--check` 的**红行以 `🔴` 开头**，而本机控制台是 **GBK**
#  ⇒ `print('  🔴 缺产物：…')` 抛 `UnicodeEncodeError`，**整个 `--check` 红路径打成 traceback**
#  （实测：`wf_report_artifacts` 第 78 行崩，rc 变成 1 但原因看着像脚本坏了、且**点名一行没印出来**）。
#  照 `gen_arena_groups.py:96` 的写法**无条件**把 stdout 设成 utf-8（`errors='replace'` 兜底，不因编码再崩）。
try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

ROOT = "d:/4/Unity"
AA_DIRS = [
    'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64',
    'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64',
]
ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']


## 🆕 2026-10-27（B9_W45ARENA）`--check` 的**产物校验**（只读，不写盘）。
#  原来这一族的 `--check` 是「只扫不写」（`if check or not out: continue`）—— 盘上缺产物也恒 rc0，
#  属「看着有 check、其实啥都没查」。口径照 `工具/import_original_art.py:1714-1721`：
#  把**重算出来的内容**与盘上那份**逐字节比**；缺 = 缺产物 · 不一致 = 过期，末尾点名 + rc=1。
def wf_check_artifact(dst, text, lack, stale):
    if not os.path.isfile(dst):
        lack.append(dst)
        return
    try:
        old = io.open(dst, encoding='utf-8').read()
    except Exception as e:
        stale.append((dst, '读不出：%s' % e))
        return
    if old == text:
        return
    try:                     # 只差缩进/键序 ⇒ 语义一致，**不算过期**（`JsonUtility` 本来就不看键序）
        if json.loads(old) == json.loads(text):
            return
    except Exception:
        pass
    stale.append((dst, '内容与重算结果不同'))


def wf_report_artifacts(check, lack, stale, script):
    """`--check` 收尾：有缺/过期就点名并让 main 返回 1。→ 该返回的 rc（无问题 = 0）。"""
    if not check or not (lack or stale):
        return 0
    print('')
    for p in lack:
        print('  🔴 缺产物：%s' % p)
    for p, why in stale:
        print('  🔴 产物过期（%s）：%s' % (why, p))
    print('  ⇒ 重跑一次 `python 工具/%s` 生成（`--check` 只读、不写盘）。' % script)
    return 1


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
# ⚠️ 2026-10-24 现读**不在盘上** —— 但**这不是死链**：它是本脚本自己的**缓存**
#   （`build_index` 跑完写到这儿、下次直接读；见下面的 `os.path.isfile(IDX_CACHE)` 分支）
#   ⇒ 缺了会**自动重建**（慢一点而已）。⛔ 别把它当「输入丢了」。现读 `d:/4/_tmp_view/` 下也没有同名替代。


def build_index(UnityPy, aa):
    """`pathID → [[bundle 文件名, CAB 名, 类型], …]`（Sprite / Texture2D 都要跨包找 ——
    astra/tauviorla 的 9 个精灵里有一半引的是**别的包**里的同一张 `Glow Scattered`）。

    🔴 **A160（A152 A4）：条目里补上 CAB**（原来只有 `[包名, 类型]`），`ext_obj` 的候选挑法
    改成 **同 CAB → 同包 → 第一条 + 出声**。
    ⚠️ **A152 说「只记包名 ⇒ 同源判据退化」这句，经实测要分清两种情况**（这里更正）：
      · **同包内**：15 个 `scenes_*` 双 CAB 包的 `(类型, pid)` **交集 = 0**（`.sharedAssets` 全是
        Material/Texture2D/Mesh/Shader 这类资产，主 CAB 全是场景对象；唯一例外
        `mainmenuwarpforge` 的 128 个 GameObject）⇒ **今天没有**「同一份包名下两份 CAB 都命中」的情况。
      · **跨包**：**真会踩** —— 实测**跨包同 pid 不同名：Material 18 · Texture2D 11 条，来源全是
        `scenes_scenes_battlearena*`**（每个场景包的主 CAB 都从 pid=1 重新编号）。
        而 `ext_obj` 原来**只有「按类型筛」这一档、`cands[0]` 就是包名序第一条** ⇒ 引用者在自己包里时
        取到的是**字母序靠前那个场**的对象（`battlearena1` < `battlearena10`… ）——
        这才是本条要修的缺陷。
    """
    if os.path.isfile(IDX_CACHE):
        try:
            d = json.load(io.open(IDX_CACHE, encoding='utf-8'))
            if d.get('_aa') == aa and d.get('_v') == 2:
                return d
        except Exception:
            pass
    idx = {'_aa': aa, '_v': 2}
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
                # A160：把 CAB 一起存（`m_FileID` / pid 都是**分包局部**的；「包名」不足以定位一份文件
                # —— `.sharedAssets` 与主 CAB 的 `externals` 逐位不同。`_v` 1 → 2 是条目加了 CAB）
                idx.setdefault(str(o.path_id), []).append(
                    [f, getattr(getattr(o, 'assets_file', None), 'name', None), o.type.name])
        print('   索引中… %s' % f, end='\r')
    print('   精灵/贴图索引完成：%d 项' % len([k for k in idx if not k.startswith('_')]))
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
    lack, stale = [], []
    for arena in arenas:
        bundle = 'scenes_scenes_%s.bundle' % arena
        path = os.path.join(aa, bundle)
        if not os.path.isfile(path):
            continue
        env = UnityPy.load(path)
        objs, cabs = {}, {}
        for o in env.objects:
            try:
                objs[(o.type.name, o.path_id)] = o.read_typetree()
                # A160：连**它自己那份 CAB** 一起记（`m_FileID` 是相对它那份的；也让贴图导出能钉到同一份）
                cabs[(o.type.name, o.path_id)] = getattr(getattr(o, 'assets_file', None), 'name', None)
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
        # A160：`texPathId → (宿主包, 宿主 CAB)` —— 只给下面的贴图导出用，**不写进 JSON**
        #（写出去的 `<场>_sprites.json` 键集不变，`ArenaBuilder` 那边一个字都不用动）。
        tex_pref = {}
        # 🆕 2026-10-27（B9_W59ARENA）**atlas 精灵**：`tex → {pid, rect, png_w/h, host, cab}`（**不写进 JSON**）。
        #   只给下面的贴图导出用：这类贴图要从 SpriteAtlas 的大图里**裁**出来，不是整张。
        crop_of = {}
        cache = {}

        def atlas_crop(S, s_host, s_cab):
            """**atlas 精灵**（`m_RD.texture` 是 `{m_FileID:0, m_PathID:0}`，像素在图集里）→ 裁切信息。

            🔴 判据（2026-10-27 实测，`duplicateassetisolation` 的 `Card Text smooth background`
               与 `atlasindividual_assets_battleatlasui` 的 `Atack_Icon BW` 两份都核过）：
              · `m_RD.texture` **PathID = 0** ⇒ 它没有自己的 Texture2D；
              · `m_SpriteAtlas` → `SpriteAtlas`，它的 `m_RenderDataMap` = `[(m_RenderDataKey, atlasData)]`，
                `atlasData` 里才有 `texture`（图集大图）+ `textureRect`（该 sprite 在图集里的矩形）；
              · `settingsRaw` 位域：packed=bit0 · packingMode=bit1-2 · packingRotation=bit3-4
                （两份实测都 = 1 ⇒ packed / Tight / **无旋转**）；`textureRectOffset` 为 0 且
                `textureRect` == Sprite 的 `m_Rect` ⇒ **直接裁那一块就是整张 sprite**。
            ⚠️ 不满足上面任何一条（有旋转 / downscale≠1 / 找不到图集）⇒ **如实出声 + 返回 None**，不猜。
            """
            apid = ((S.get('m_SpriteAtlas') or {}).get('m_PathID', 0))
            if not apid:
                return None
            A = objs.get(('SpriteAtlas', apid))
            if A is None and s_host:                       # 图集在 sprite 所在那个包里（两份实测都是）
                e2 = cache.get(s_host)
                if e2 is None:
                    try:
                        cache[s_host] = e2 = UnityPy.load(os.path.join(aa, s_host))
                    except Exception:
                        e2 = None
                if e2 is not None:
                    for o2 in e2.objects:
                        if o2.type.name == 'SpriteAtlas' and o2.path_id == apid:
                            A = o2.read_typetree()
                            break
            if A is None:
                print('    ⚠️ [%s] %s：`m_RD.texture` 是空的、SpriteAtlas pathID %s 又不在它自己包里(%s)'
                      ' ⇒ 这张贴图**判不出**（留空，不猜）' % (arena, S.get('m_Name'), apid, s_host))
                return None
            key = S.get('m_RenderDataKey')
            # ⚠️ 结构实测（2026-10-27）：`m_RenderDataKey` = `({'data[0]'..}, 21300000)`（**二元组**），
            #   而 `m_RenderDataMap` 每条是 `[(同形状的二元组), atlasData]` ⇒ 比的是**二元组的第一项**（那个 4 整数哈希）。
            key0 = key[0] if isinstance(key, (list, tuple)) and key else None
            hit = None
            for k, v in (A.get('m_RenderDataMap') or []):
                if key0 is not None and isinstance(k, (list, tuple)) and k and k[0] == key0:
                    hit = v
                    break
            if hit is None:
                print('    ⚠️ [%s] %s：图集 `%s` 的 m_RenderDataMap 里没有它的 RenderDataKey ⇒ 这张贴图**判不出**'
                      % (arena, S.get('m_Name'), A.get('m_Name')))
                return None
            rot = (hit.get('settingsRaw', 0) >> 3) & 3
            ds = hit.get('downscaleMultiplier', 1.0)
            if rot:
                print('    ⚠️ [%s] %s：图集 packingRotation=%d ⇒ 裁切要转，本脚本没实现（留空，不猜）'
                      % (arena, S.get('m_Name'), rot))
                return None
            if abs((ds or 1.0) - 1.0) > 1e-6:
                print('    ⚠️ [%s] %s：图集 downscaleMultiplier=%s（不是 1）⇒ 裁出来会比 m_Rect 小，先如实出声'
                      % (arena, S.get('m_Name'), ds))
            tr = hit.get('textureRect') or {}
            rc = S.get('m_Rect') or {}
            return {'tex': S.get('m_Name', ''), 'pid': (hit.get('texture') or {}).get('m_PathID', 0),
                    'rect': (tr.get('x', 0), tr.get('y', 0), tr.get('width', 0), tr.get('height', 0)),
                    'png_w': int(round(rc.get('width', 0))), 'png_h': int(round(rc.get('height', 0))),
                    'host': s_host, 'cab': s_cab}

        def ext_obj(path_id, kind, prefer_cab=None, prefer_bundle=None):
            """从**别的包**里取一个对象（Sprite/Texture2D/Material）。返回 (typetree, 包名, UnityPy 对象) 或 None。

            🔴 A160（A152 A4）：候选挑法 = **同 CAB → 同包 → 第一条**，走到「第一条」且候选不止一条时
            **出声**。CAB 名全局唯一，而双 CAB 包的**包名字符串相同** ⇒ 只按包名判会退化成「先到先得」。
            （实测跨包同 pid 不同名：Mesh 82 · Material 18 · Texture2D 11 条；来源全是 `scenes_*`。）"""
            hits = idx.get(str(path_id)) or []
            cands = [h for h in hits if h[2] == kind]
            if not cands:
                return None
            pick = None
            for h in cands:
                if prefer_cab is not None and h[1] == prefer_cab:
                    pick = h
                    break
            if pick is None:
                for h in cands:
                    if prefer_bundle is not None and h[0] == prefer_bundle:
                        pick = h
                        break
            if pick is None:
                pick = cands[0]
                if len(cands) > 1:
                    print('    🔴 [%s] %s pathID %s 有 %d 个**不同源**的候选（引用者 CAB=%s / 包=%s）'
                          '⇒ 取第一条：%s'
                          % (arena, kind, path_id, len(cands), prefer_cab, prefer_bundle,
                             ' ｜ '.join('%s/%s' % (h[0], h[1]) for h in cands[:4])))
            b = pick[0]
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
                    # 指定了 CAB 时**必须**落在那一份里（双 CAB 包两份都可能有这个 pid）
                    if pick[1] is not None and getattr(getattr(o2, 'assets_file', None), 'name', None) != pick[1]:
                        continue
                    return (o2.read_typetree(), b, o2)
            return None

        for pid, sr in sorted(((k[1], v) for k, v in objs.items() if k[0] == 'SpriteRenderer')):
            gid = (sr.get('m_GameObject') or {}).get('m_PathID', 0)
            gname = go.get(gid, {}).get('m_Name', '?')
            spid = (sr.get('m_Sprite') or {}).get('m_PathID', 0)
            if spid == 0:
                continue                                  # 空 sprite ⇒ 原版也画不出东西（实测 `Shadow`/`CardBackShadow` 一族）
            rcab = cabs.get(('SpriteRenderer', pid))       # A160：引用者自己那份 CAB（判「同源」的锚点）
            if spid in sp:
                S = sp[spid]                              # 本包自带的那份
                s_host, s_cab = bundle, cabs.get(('Sprite', spid))
            else:
                hit = ext_obj(spid, 'Sprite', rcab, bundle)   # 到别的包里取
                S = hit[0] if hit else None
                s_host, s_cab = (hit[1], getattr(getattr(hit[2], 'assets_file', None), 'name', None)) \
                    if hit else (None, None)
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
                hit = ext_obj(texid, 'Texture2D', s_cab, s_host)   # A160：贴图按 **sprite 所在**那份找
                if hit:
                    texname = hit[0].get('m_Name', '')
            if not texname and not texid:
                # 🆕 2026-10-27（B9_W59ARENA）：**图集精灵**（`m_RD.texture` 是 0，像素在 SpriteAtlas 里）。
                #   原来这里什么都不做 ⇒ `tex` 留空 ⇒ `ArenaBuilder.LoadSpriteAsset` 直接返 null
                #   ⇒ 建场时这部分精灵**一个都建不出来**（实证：sororitas 的 3 个 active `Bottom Filler *`）。
                #   贴图名用 **sprite 自己的名字**（不是图集大图名 —— 大图会被多个 sprite 共用，
                #   按大图名存会把不同 sprite 的像素写到同一个文件里）。
                ac = atlas_crop(S, s_host, s_cab)
                if ac and ac['pid']:
                    if ac['tex'] in crop_of and crop_of[ac['tex']]['rect'] != ac['rect']:
                        # 同名不同块 ⇒ 加后缀，绝不覆盖（同名同块 = 同一张图，共用一个 PNG 是对的）
                        ac['tex'] = '%s__atlas%s_%s' % (ac['tex'], int(ac['rect'][0]), int(ac['rect'][1]))
                        print('    ⚠️ [%s] 图集里有两个同名不同块的 sprite ⇒ 落到 %s'
                              % (arena, ac['tex']))
                    texname = ac['tex']
                    texid = ac['pid']
                    crop_of[texname] = ac
                    tex_pref.setdefault(texid, (ac['host'], ac['cab']))
            if texid:
                tex_pref.setdefault(texid, (s_host, s_cab))    # A160：导出那一趟要按同一份找
            mats = []
            for mref in (sr.get('m_Materials') or []):
                mpid = (mref or {}).get('m_PathID', 0)
                m = mt.get(mpid)
                if m is None:                                  # 外链材质（tauviorla 那族就是）⇒ 到别的包取
                    hit = ext_obj(mpid, 'Material', rcab, bundle)   # A160：按渲染器所在那份找
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
        dst = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, arena + '_sprites.json')
        if check:
            # `--check`：产物 = ① `<场>_sprites.json` ② `Sprites/<tex>.png`（`ArenaBuilder.BuildSprites` 按名取）
            if not out:
                # 原版这一场没有 sprite 对象 ⇒ 不该有产物（有就是过期/多余）
                if os.path.isfile(dst):
                    stale.append((dst, '原版这一场没有 SpriteRenderer ⇒ 产物多余'))
            else:
                wf_check_artifact(
                    dst, json.dumps({'scene': arena, 'sprites': out}, ensure_ascii=False, indent=2),
                    lack, stale)
                spdir = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, 'Sprites')
                seen = set()
                for e in out:
                    nm = e.get('tex')
                    if not nm or nm in seen:
                        continue
                    seen.add(nm)
                    f = os.path.join(spdir, nm + '.png')
                    if not os.path.isfile(f):
                        lack.append(f)
            continue
        if not out:
            continue

        # 贴图导出（放 Sprites/，与 Textures/ 分开 —— 那边 ApplyTextureImportSettings 会强设成 Default）
        spdir = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, 'Sprites')
        # ⚠️ 2026-10-27（B9_W59ARENA）：**目录改成「真要写文件时才建」** —— 原来无条件 `makedirs`，
        #   而 11 场的 `out` 里全是**没有独立贴图**的图集精灵（下面会跳过导出）⇒ 盘上留下
        #   一堆**空的 `Sprites/` 目录**（Unity 刷新时会给它们生成 `.meta`，是纯噪声）。
        done = {}
        for e in out:
            nm = e['tex']
            if not nm or nm in done:
                continue
            # 🔴 A160：**按 pid + CAB 取**（原来只按 pid 取，而 `env.objects` 是两份 CAB 聚合的
            #   ⇒ 双 CAB 包里会先撞上 `.sharedAssets` 那一份的同号贴图）。取不到同 CAB 的就退回按 pid。
            tpid = e['texPathId']
            tcab = cabs.get(('Texture2D', tpid))
            o, loose = None, False
            for o2 in env.objects:
                if o2.type.name != 'Texture2D' or o2.path_id != tpid:
                    continue
                if tcab is not None and getattr(getattr(o2, 'assets_file', None), 'name', None) != tcab:
                    continue
                o = o2
                break
            if o is None and tcab is not None:
                o = next((o2 for o2 in env.objects
                          if o2.type.name == 'Texture2D' and o2.path_id == tpid), None)
                if o is not None:
                    loose = True
            if o is None:                                   # 外链贴图 ⇒ 到别的包里取（按 sprite 所在那份找）
                hit = ext_obj(tpid, 'Texture2D', *tex_pref.get(tpid, (None, None)))
                o = hit[2] if hit else None
            if o is None:
                print('    ⚠️ 找不到 Texture2D pathID %s（%s）' % (tpid, nm))
                continue
            if loose:
                print('    ⚠️ %s：贴图 pathID %s 不在预期的那份 CAB 里 —— 退回按 pid 取' % (nm, tpid))
            f = os.path.join(spdir, nm + '.png')
            if not os.path.exists(f):
                os.makedirs(spdir, exist_ok=True)
                ci = crop_of.get(nm)
                if ci is not None:
                    # 🆕 图集精灵：从图集大图里**裁**出自己那一块（`textureRect` 是图集内的像素矩形）。
                    # 🔴 裁完**必须自检尺寸 == Sprite 的 `m_Rect`** —— 原版世界尺寸 = m_Rect/ptu，
                    #    裁错一块的话尺寸对不上，宁可**不写**并出声（不猜）。
                    img = o.read().image
                    W, H = img.size
                    x, y, w, h = ci['rect']
                    box = (int(round(x)), int(round(H - (y + h))), int(round(x + w)), int(round(H - y)))
                    sub = img.crop(box)
                    want = (ci['png_w'], ci['png_h'])
                    if sub.size != want:
                        print('    🔴 %s：图集裁出来 %r 与 Sprite 的 m_Rect %r 不符 ⇒ **不写**（不猜）'
                              % (nm, sub.size, want))
                        continue
                    sub.save(f)
                    print('    → 导出（图集裁切 %r · 图集 %dx%d）%s' % (box, W, H, f))
                else:
                    o.read().image.save(f)
                    print('    → 导出 %s' % f)
            done[nm] = True
        dst = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, arena + '_sprites.json')
        io.open(dst, 'w', encoding='utf-8', newline='\n').write(
            json.dumps({'scene': arena, 'sprites': out}, ensure_ascii=False, indent=2))
        print('    → 写 %s（%d 个）' % (dst, len(out)))
    print('合计：%d 个 sprite 对象' % total)
    return wf_report_artifacts(check, lack, stale, 'gen_arena_sprites.py')


if __name__ == '__main__':
    sys.exit(main())
