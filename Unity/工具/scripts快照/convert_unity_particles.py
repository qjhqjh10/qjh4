# -*- coding: utf-8 -*-
"""
convert_unity_particles.py — Unity ParticleSystem JSON → Godot 粒子中间格式
输入: 特效名 (GO 名, 如 AttackHitSmall) 或 ParticleSystem JSON 路径 (可多个)
输出: D:/warpforge/data/particles/<名>.json (2D 简化参数, 拍平由 unity_particles.gd 做)
      --3d 模式: D:/warpforge/data/particles3d/<名>.json (3D 世界空间, 保留形状/速度/挂点偏移, unity_particles3d.gd 读取)
      + 纹理 (从 bundle 或解包目录)
用法: py312/python.exe scripts/convert_unity_particles.py AttackHitSmall Actual_Explosion ...
      py312/python.exe scripts/convert_unity_particles.py --3d AttackHitSmall ...
"""
import json
import glob
import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

PREFAB = r'D:\2\解包整理\08_预制体特效\战斗预制体'
SHARED = r'D:\2\解包整理\08_预制体特效\共享资源'
SCENE_ROOT = r'D:\2\解包整理\07_场景'   # --src 模式: PREFAB 指向 <SCENE_ROOT>/<场景>
OUT_JSON = r'D:\warpforge\data\particles'
OUT_JSON3D = r'D:\warpforge\data\particles3d'
OUT_TEX = r'D:\warpforge\assets\particles'
BUNDLE_DIR = 'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/'

_go_cache = {}   # GameObject JSON 解析缓存 (PathID 同名多文件, 按文件名缓存)


def find_files(d, fname, pid):
    """按文件名或 PathID 后缀找文件"""
    out = glob.glob(os.path.join(d, fname, f'*_{pid}.json')) + \
          glob.glob(os.path.join(d, fname, f'{pid}.json'))
    return out


def cv(v, default=None):
    """MinMaxCurve → 常数 (scalar / minScalar 优先)"""
    if isinstance(v, dict):
        st = v.get('minMaxState', 0)
        if st == 0:
            return v.get('scalar') if v.get('scalar') is not None else default
        return v.get('minScalar') if v.get('minScalar') is not None else default
    if v is None:
        return default
    return v


def cv2(v):
    """MinMaxCurve → (min, max) — 2026-08-23 修复: maxCurve/maxScalar 实测取到 (旧实现恒 None)"""
    if isinstance(v, dict):
        st = v.get('minMaxState', 0)
        if st == 0:
            s = v.get('scalar')
            return s, s
        lo = v.get('minScalar')
        hi = v.get('maxScalar')
        if hi is None:
            mc = v.get('maxCurve')
            if isinstance(mc, dict):
                if mc.get('scalar') is not None:
                    hi = mc['scalar']
                else:
                    pts = mc.get('m_Curve') or []
                    if pts:
                        hi = pts[0].get('value')
        return lo, hi
    return v, v


def load_tex_png(tex_pid, name):
    """纹理: 先解包目录 (战斗预制体/共享资源 Texture2D), 再 bundle 提取
    2026-08-23: bundle 存盘名=贴图对象 m_Name (与既有 655 贴图命名体系一致); 已存在跳过写 (幂等)"""
    for d in (PREFAB, SHARED):
        for f in glob.glob(os.path.join(d, 'Texture2D', f'*_{tex_pid}.json')):
            try:
                j = json.load(open(f, encoding='utf-8'))
                png = os.path.join(os.path.dirname(f), j.get('m_Name', '') + '.png')
                if os.path.exists(png):
                    return png
            except Exception:
                pass
    # bundle 提取
    try:
        sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
        import UnityPy
        env = UnityPy.Environment()
        bnames = ['battleprefabs_vfxandmisc_assets_all', 'battlesharedresources_assets_all']
        if PREFAB.startswith(SCENE_ROOT):   # --src 场景模式: 贴图可能在场景 bundle
            bnames.insert(0, 'scenes_scenes_' + os.path.basename(PREFAB))
        for b in bnames:
            p = os.path.join(BUNDLE_DIR, b + '.bundle')
            if os.path.exists(p):
                try:
                    env.load_file(p)
                except Exception:
                    pass
        for obj in env.objects:
            if obj.type.name == 'Texture2D' and str(obj.path_id) == str(tex_pid):
                try:
                    img_obj = obj.read()
                    img = img_obj.image
                    if img is None:
                        return None
                    os.makedirs(OUT_TEX, exist_ok=True)
                    tname = str(getattr(img_obj, 'm_Name', '') or '') or name or str(tex_pid)
                    fp = os.path.join(OUT_TEX, tname + '.png')
                    if not os.path.exists(fp):
                        img.save(fp)
                    return fp
                except Exception:
                    return None
    except Exception as e:
        print(f'  [bundle失败] {e}')
    return None


def go_file(go_name):
    """GameObject JSON 定位: exact <名>.json 优先, 其次前缀 glob 稳定排序 (2026-08-23 防变体乱序)"""
    exact = os.path.join(PREFAB, 'GameObject', go_name + '.json')
    if os.path.exists(exact):
        return exact
    cands = [f for f in glob.glob(os.path.join(PREFAB, 'GameObject', go_name + '*.json'))
             if not re.search(r'_\d+_\d+[-+]?\d*\.json$', f)]
    return sorted(cands)[0] if cands else None


def load_go(go_name):
    """GO 名 → (GameObject dict, 组件 PathID 列表); 带文件缓存"""
    gp = go_file(go_name)
    if not gp:
        return None, None
    if gp not in _go_cache:
        _go_cache[gp] = json.load(open(gp, encoding='utf-8'))
    g = _go_cache[gp]
    comps = [c['component']['m_PathID'] for c in g.get('m_Component', [])]
    return g, comps


def ps_file_of(comps):
    """组件列表 → 第一个 ParticleSystem JSON 路径"""
    for c in comps:
        for f in glob.glob(os.path.join(PREFAB, 'ParticleSystem', f'*_{c}.json')):
            if f.endswith(f'_{c}.json') and not f.endswith(f'_{c}_{c}.json'):
                return f
    return None


def go_has_tex(comps):
    """GO 组件: 存在 ParticleSystemRenderer → 材质 m_TexEnvs 有非零纹理? (贴图优先选择依据)"""
    rid = None
    for c in comps:
        for f in glob.glob(os.path.join(PREFAB, 'ParticleSystemRenderer', f'*_{c}.json')):
            if f.endswith(f'_{c}.json') and not f.endswith(f'_{c}_{c}.json'):
                rid = c
                break
        if rid:
            break
    if rid is None:
        return False
    r = json.load(open(glob.glob(os.path.join(PREFAB, 'ParticleSystemRenderer', f'*_{rid}.json'))[0],
                       encoding='utf-8'))
    for m in r.get('m_Materials', []):
        pid = m.get('m_PathID')
        for mf in glob.glob(os.path.join(PREFAB, 'Material', f'*_{pid}.json')):
            mat = json.load(open(mf, encoding='utf-8'))
            sp = mat.get('m_SavedProperties', {})
            for key, val in sp.get('m_TexEnvs', []):
                if isinstance(val, dict) and val.get('m_Texture', {}).get('m_PathID'):
                    return True
    return False


def transform_of(comps):
    """组件列表 → Transform dict (m_Children 用)"""
    for c in comps:
        for f in glob.glob(os.path.join(PREFAB, 'Transform', f'*_{c}.json')):
            if f.endswith(f'_{c}.json') and not f.endswith(f'_{c}_{c}.json'):
                return json.load(open(f, encoding='utf-8'))
    return None


def child_go_of(cpid):
    """子 Transform 组件 PathID → 子 GO 名 (扫描 GameObject 目录匹配)"""
    for fn in os.listdir(os.path.join(PREFAB, 'GameObject')):
        if not fn.endswith('.json'):
            continue
        gp = os.path.join(PREFAB, 'GameObject', fn)
        if gp not in _go_cache:
            try:
                _go_cache[gp] = json.load(open(gp, encoding='utf-8'))
            except Exception:
                _go_cache[gp] = None
        gg = _go_cache[gp]
        if gg is None:
            continue
        gcomps = [c['component']['m_PathID'] for c in gg.get('m_Component', [])]
        if cpid in gcomps:
            return gg.get('m_Name')
    return None


def collect_ps(go_name, depth, cands):
    """递归收集子树全部含 ParticleSystem 的候选 [(go名, ps_path, comps), ...]"""
    if depth > 6:
        return
    _, comps = load_go(go_name)
    if comps is None:
        return
    ps_f = ps_file_of(comps)
    if ps_f:
        cands.append((go_name, ps_f, comps))
    tr = transform_of(comps)
    if not tr:
        return
    for ch in tr.get('m_Children', []):
        cpid = ch.get('m_PathID') if isinstance(ch, dict) else ch
        child_name = child_go_of(cpid)
        if child_name:
            collect_ps(child_name, depth + 1, cands)


def find_ps_recursive(go_name, depth=0):
    """沿子 GO 递归找含 ParticleSystem 的 GO (组合特效: 根 GO 只是空容器)
    2026-08-23: 收集全部候选 → 优先返回带纹理者 (视觉主体), 否则 BFS 首个"""
    cands = []
    collect_ps(go_name, 0, cands)
    if not cands:
        return None
    for gname, ps_f, comps in cands:
        if go_has_tex(comps):
            return (gname, ps_f, comps)
    return cands[0]


def load_go_transform(comps):
    """GO 组件列表 → 根 Transform localPosition/localScale (特效挂点偏移/缩放, 3D 模式用)"""
    for c in comps:
        for f in glob.glob(os.path.join(PREFAB, 'Transform', f'*_{c}.json')):
            if f.endswith(f'_{c}.json') and not f.endswith(f'_{c}_{c}.json'):
                tr = json.load(open(f, encoding='utf-8'))
                pos = tr.get('m_LocalPosition', {})
                sc = tr.get('m_LocalScale', {})
                return [pos.get('x', 0), pos.get('y', 0), pos.get('z', 0)], \
                       [sc.get('x', 1), sc.get('y', 1), sc.get('z', 1)]
    return [0, 0, 0], [1, 1, 1]


def convert_effect(go_name, as3d=False, as_name=None):
    """按 GO 名转换特效: GO → ParticleSystem + Renderer → Material → 纹理 (组合特效递归子 GO)
    2026-08-23: as_name 覆盖输出名 (battle.gd 引用名≠源 GO 名时用); 精确 GO 优先 + 贴图优先子 GO"""
    out_name = as_name or go_name
    gp = go_file(go_name)
    if not gp:
        print(f'✗ 未找到 GO: {go_name}')
        return False
    g, comps = load_go(go_name)
    # 找 ParticleSystem (直接组件; 无则递归子 GO)
    ps_path = ps_file_of(comps)
    if not ps_path:
        r = find_ps_recursive(go_name)
        if r:
            resolved_name, ps_path, rcomps = r
            g, comps = load_go(resolved_name)
            print(f'  [子GO] {go_name} → {resolved_name}')
    if not ps_path:
        print(f'✗ {go_name}: 无 ParticleSystem 组件')
        return False
    ps = json.load(open(ps_path, encoding='utf-8'))
    im = ps.get('InitialModule', {})
    # Renderer + 纹理
    tex_path = None
    tex_name = None
    render_mode = 0
    for c in comps:
        rf = glob.glob(os.path.join(PREFAB, 'ParticleSystemRenderer', f'*_{c}.json'))
        if not rf:
            continue
        r = json.load(open(rf[0], encoding='utf-8'))
        render_mode = r.get('m_RenderMode', 0)
        for m in r.get('m_Materials', []):
            pid = m.get('m_PathID')
            for mf in glob.glob(os.path.join(PREFAB, 'Material', f'*_{pid}.json')):
                mat = json.load(open(mf, encoding='utf-8'))
                sp = mat.get('m_SavedProperties', {})
                for key, val in sp.get('m_TexEnvs', []):
                    if isinstance(val, dict) and val.get('m_Texture', {}).get('m_PathID'):
                        tex_pid = val['m_Texture']['m_PathID']
                        tex_name = mat.get('m_Name', '') or go_name
                        tex_path = load_tex_png(tex_pid, out_name)
                        if tex_path:
                            break
                if tex_path:
                    break
            if tex_path:
                break
    # 爆发 (EmissionModule bursts)
    bursts = []
    em = ps.get('EmissionModule', {})
    blist = em.get('m_Bursts') or em.get('bursts') or []
    if isinstance(blist, list):
        for b in blist:
            if isinstance(b, dict):
                t = b.get('time', 0)
                cnt = cv(b.get('countCurve'))
                bursts.append([t, cnt or 1])
    # Size/Color over lifetime (简化: 取曲线关键帧)
    size_curve = []
    sm = ps.get('SizeModule', {})
    if sm.get('enabled'):
        c = sm.get('curve', {}).get('maxCurve', {}).get('m_Curve', [])
        size_curve = [[k.get('time', 0), k.get('value', 1)] for k in c]
    color_curve = []
    cm = ps.get('ColorModule', {})
    if cm.get('enabled'):
        grad = cm.get('gradient', {}).get('maxGradient', {})
        keys = []
        for i in range(8):
            k = grad.get(f'key{i}')
            if k:
                keys.append([i / 7.0, [k.get('r', 1), k.get('g', 1), k.get('b', 1), k.get('a', 1)]])
        color_curve = keys
    # 图集帧 (UVModule: tilesX/tilesY + startFrame) → 切片纹理
    frame_grid = None
    start_frame = 0
    uv = ps.get('UVModule', {})
    if uv.get('enabled') and (uv.get('tilesX') or 1) * (uv.get('tilesY') or 1) > 1:
        tx = int(uv.get('tilesX') or 1)
        ty = int(uv.get('tilesY') or 1)
        frame_grid = [tx, ty]
        total = tx * ty
        sf = uv.get('startFrame', {}).get('scalar', 0.0)
        start_frame = int(sf * total) % total if sf else 0
    tex_final = tex_path
    if tex_path and os.path.exists(tex_path):
        # 预乘 alpha 修复 (解包纹理 ~54% alpha 损坏: RGB 完整但 alpha≈0 → 粒子全透明)
        # ⚠️ 只对"RGB 完整"型损坏修复 (RGB 平均 >100); 合法半透明纹理 (暗底亮芯火花图) 不动
        from PIL import Image
        img_tex = Image.open(tex_path).convert('RGBA')
        w0, h0 = img_tex.size
        px0 = img_tex.load()
        n0 = 0; tr0 = 0; rgb_sum = 0
        for yy in range(0, h0, 2):
            for xx in range(0, w0, 2):
                n0 += 1
                p = px0[xx, yy]
                if p[3] < 10:
                    tr0 += 1
                rgb_sum += (p[0] + p[1] + p[2]) / 3
        if n0 > 0 and tr0 / n0 > 0.5 and rgb_sum / n0 > 100:
            r, g, b, a = img_tex.split()
            img_tex = Image.merge('RGBA', (r, g, b, a.point(lambda v: 255)))
            img_tex.save(tex_path)
            print(f'  [alpha修复] {os.path.basename(tex_path)}')
    if tex_path and frame_grid:
        # 切片: 取 start_frame 对应帧
        from PIL import Image
        img_tex = Image.open(tex_path).convert('RGBA')
        w, h = img_tex.size
        fw, fh = w // frame_grid[0], h // frame_grid[1]
        row, col = divmod(start_frame, frame_grid[0])
        crop = img_tex.crop((col * fw, row * fh, (col + 1) * fw, (row + 1) * fh))
        # 2026-08-23: 黑帧回退 (图集动画首帧常为空白/虚影 — Slash Repeating 2x 案例:
        # startFrame→第 1 格近黑, 单帧近似=无形) → 自动改切同图集最亮格
        frames = []
        for fy in range(frame_grid[1]):
            for fx in range(frame_grid[0]):
                cell = img_tex.crop((fx * fw, fy * fh, (fx + 1) * fw, (fy + 1) * fh))
                p2 = cell.convert('L').resize((16, 16))
                frames.append((sum(p2.getdata()), fx, fy))
        top = max(frames)
        cur = sum(crop.convert('L').resize((16, 16)).getdata())
        if top[0] > 40 * 256 and top[0] > cur:
            _, fx2, fy2 = top
            row, col = fy2, fx2
            crop = img_tex.crop((col * fw, row * fh, (col + 1) * fw, (row + 1) * fh))
            print(f'  [黑帧回退] {os.path.basename(tex_path)} frame{start_frame}→({col},{row}) 亮度 {top[0]}')
        crop = crop.resize((128, 128), Image.LANCZOS)
        tex_final = os.path.join(OUT_TEX, out_name + '_frame.png')
        crop.save(tex_final)
    sc = im.get('startColor', {})
    spd_lo, spd_hi = cv2(im.get('startSpeed'))   # 2026-08-23: startSpeed 随机区间 (cv2 修复后)
    if as3d:
        # ── 3D 世界空间输出 (unity_particles3d.gd 读取) ──
        offset, escale = load_go_transform(comps)
        shp = ps.get('ShapeModule', {})
        shape = {
            'type': shp.get('type', shp.get('shapeType', 0)),
            'radius': cv(shp.get('radius'), 1),
            'radiusThickness': cv(shp.get('radiusThickness'), 1),
            'angle': cv(shp.get('angle'), 0),
            'scale': [cv(shp.get('m_Scale', shp.get('scale', {})).get('x'), 1), cv(shp.get('m_Scale', shp.get('scale', {})).get('y'), 1),
                      cv(shp.get('m_Scale', shp.get('scale', {})).get('z'), 1)] if isinstance(shp.get('m_Scale', shp.get('scale')), dict) else [1, 1, 1],
        }
        life = cv(im.get('startLifetime')) or 1.0
        sz = cv(im.get('startSize')) or 1.0
        out = {
            "name": out_name,
            "effect_offset": [round(v, 3) for v in offset],
            "effect_scale": [round(v, 3) for v in escale],
            "system_lifetime": ps.get('lengthInSec', 1.0),
            "one_shot": not bool(ps.get('looping', False)),
            "play_on_awake": bool(ps.get('playOnAwake', True)),
            "amount": im.get('maxNumParticles', 100),
            "lifetime_min": life,
            "lifetime_max": life,
            "speed_min": spd_lo or 0.0,
            "speed_max": (spd_hi if spd_hi is not None else spd_lo) or 0.0,
            "size_min": sz,
            "size_max": sz,
            "gravity": cv(im.get('gravityModifier')) or 0.0,
            "color_min": [sc.get('minColor', {}).get(k, 1.0) for k in 'rgba'] if sc.get('minColor') else [1, 1, 1, 1],
            "color_max": [sc.get('maxColor', {}).get(k, 1.0) for k in 'rgba'] if sc.get('maxColor') else [1, 1, 1, 1],
            "emission_rate": cv(em.get('rateOverTime')) or 0.0,
            "bursts": bursts,
            "shape": shape,
            "velocity": {},
            "rot_z_speed": 0.0,
            "size_over_lifetime": size_curve,
            "color_over_lifetime": color_curve,
            "render_mode": render_mode,
            "texture": ("res://assets/particles/" + os.path.basename(tex_final)) if tex_final else "",
        }
        os.makedirs(OUT_JSON3D, exist_ok=True)
        fp = os.path.join(OUT_JSON3D, out_name + '.json')
    else:
        out = {
            "name": out_name,
            "system_lifetime": ps.get('lengthInSec', 1.0),
            "one_shot": not bool(ps.get('looping', False)),
            "play_on_awake": bool(ps.get('playOnAwake', True)),
            "amount": im.get('maxNumParticles', 100),
            "lifetime": cv(im.get('startLifetime')) or 1.0,
            "speed_min": spd_lo or 0.0,
            "speed_max": (spd_hi if spd_hi is not None else spd_lo) or 0.0,
            "size_min": cv(im.get('startSize')) or 1.0,
            "size_max": cv(im.get('startSize')) or 1.0,
            "gravity": cv(im.get('gravityModifier')) or 0.0,
            "color_min": [sc.get('minColor', {}).get(k, 1.0) for k in 'rgba'] if sc.get('minColor') else [1, 1, 1, 1],
            "color_max": [sc.get('maxColor', {}).get(k, 1.0) for k in 'rgba'] if sc.get('maxColor') else [1, 1, 1, 1],
            "emission_rate": cv(em.get('rateOverTime')) or 0.0,
            "bursts": bursts,
            "shape_type": ps.get('ShapeModule', {}).get('type', ps.get('ShapeModule', {}).get('shapeType')),
            "render_mode": render_mode,
            "size_over_lifetime": size_curve,
            "color_over_lifetime": color_curve,
            "texture": ("res://assets/particles/" + os.path.basename(tex_final)) if tex_final else "",
        }
        os.makedirs(OUT_JSON, exist_ok=True)
        fp = os.path.join(OUT_JSON, out_name + '.json')
    with open(fp, 'w', encoding='utf-8') as f:
        json.dump(out, f, ensure_ascii=False, indent=1)
    print(f'✓ {out_name}: lifetime={out["lifetime_max" if as3d else "lifetime"]} size={out["size_min"]} '
          f'speed={out.get("speed_min")}~{out.get("speed_max")} bursts={bursts} '
          f'tex={os.path.basename(tex_path) if tex_path else "无"} -> {fp}')
    return True


def main():
    args = sys.argv[1:] or ['AttackHitSmall', 'Actual_Explosion']
    as3d = False
    as_name = None
    src = None
    rest = []
    i = 0
    while i < len(args):
        a = args[i]
        if a == '--3d':
            as3d = True
            i += 1
        elif a == '--as':
            if i + 1 < len(args):
                as_name = args[i + 1]
            i += 2
        elif a == '--src':
            if i + 1 < len(args):
                src = args[i + 1]
            i += 2
        else:
            rest.append(a)
            i += 1
    if src:
        global PREFAB
        PREFAB = os.path.join(SCENE_ROOT, src)
        print(f'  [--src] PREFAB={PREFAB}')
    ok = 0
    for a in rest:
        if a.endswith('.json'):
            go_name = os.path.basename(a).split('_')[0]
        else:
            go_name = a
        if convert_effect(go_name, as3d, as_name):
            ok += 1
    print(f'完成 {ok}/{len(rest)}' + (' (3D)' if as3d else ''))
    return 0


if __name__ == '__main__':
    sys.exit(main())
