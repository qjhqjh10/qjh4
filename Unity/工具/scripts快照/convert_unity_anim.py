# -*- coding: utf-8 -*-
"""Unity AnimationClip JSON → Godot .tres Animation 转化器
用法: python convert_unity_anim.py <AnimationClip目录> [输出目录]
输出: data/ui_anim/<动画名>.tres (Godot 可直接 load 的 Animation 资源)
支持: m_FloatCurves (m_Color.a 等标量属性) / m_PositionCurves (Vector3) /
      m_ScaleCurves / m_EulerCurves (四元数→欧拉近似)
"""
import json, glob, os, sys

sys.stdout.reconfigure(encoding='utf-8')

SRC = sys.argv[1] if len(sys.argv) > 1 else 'D:/2/解包整理/03_界面UI/菜单/AnimationClip'
OUT = sys.argv[2] if len(sys.argv) > 2 else 'D:/2/Warpforge_tools/data/ui_anim/'

# Unity attribute → Godot 属性路径后缀 (value track 的 property)
ATTR_MAP = {
    'm_Color.a': 'color:a', 'm_Color.r': 'color:r', 'm_Color.g': 'color:g', 'm_Color.b': 'color:b',
    'm_FontSize': 'font_size', 'm_Text': 'text',
}


def parse_curve(curve_dict):
    """Unity 曲线 → (times, values) 列表"""
    keys = []
    for k in curve_dict.get('m_Curve', []):
        keys.append((k.get('time', 0.0), k.get('value')))
    keys.sort()
    return keys


def value_to_godot(v):
    """Unity 值 → Godot 字面量"""
    if isinstance(v, dict):
        # Vector3 / Color / Quaternion
        if 'x' in v and 'y' in v and 'z' in v:
            if 'w' in v:
                return f'Quaternion({v["x"]}, {v["y"]}, {v["z"]}, {v["w"]})'
            return f'Vector3({v["x"]}, {v["y"]}, {v["z"]})'
        if 'r' in v:
            return f'Color({v["r"]}, {v["g"]}, {v["b"]}, {v.get("a", 1)})'
    return str(v)


def conv_track(anim_name, path, attribute, curve_dict, track_idx):
    """一条 Unity 曲线 → Godot value track tres 片段"""
    keys = parse_curve(curve_dict)
    if not keys:
        return []
    times = ', '.join(f'{t:g}' for t, _ in keys)
    trans = ', '.join(['1'] * len(keys))
    if 'm_Color.a' in attribute or 'm_Color' in attribute:
        # 颜色属性 → Color 值 (rgba, 其他分量沿用)
        vals = ', '.join(value_to_godot(v) for _, v in keys)
    else:
        vals = ', '.join(value_to_godot(v) for _, v in keys)
    prop = ATTR_MAP.get(attribute, attribute.replace('m_', ''))
    return [
        f'tracks/{track_idx}/type = "value"',
        f'tracks/{track_idx}/imported = false',
        f'tracks/{track_idx}/enabled = true',
        f'tracks/{track_idx}/path = NodePath("{path}:{prop}")',
        f'tracks/{track_idx}/interp = 1',
        f'tracks/{track_idx}/loop_wrap = true',
        f'tracks/{track_idx}/keys = {{',
        f'"times": PackedFloat32Array({times}),',
        f'"transitions": PackedFloat32Array({trans}),',
        f'"values": [{vals}]',
        '}',
    ]


def convert_file(f, out_dir):
    d = json.load(open(f, encoding='utf-8'))
    name = d.get('m_Name', os.path.basename(f).split('_')[0])
    length = 0.0
    tracks = []
    idx = 0
    # Float curves
    for fc in d.get('m_FloatCurves', []):
        curve = fc.get('curve', {})
        keys = parse_curve(curve)
        if keys:
            length = max(length, keys[-1][0])
        lines = conv_track(name, fc.get('path', ''), fc.get('attribute', ''), curve, idx)
        if lines:
            tracks.extend(lines)
            idx += 1
    # Position curves (Vector3)
    for pc in d.get('m_PositionCurves', []):
        curve = pc.get('curve', {})
        keys = parse_curve(curve)
        if keys:
            length = max(length, keys[-1][0])
        lines = conv_track(name, pc.get('path', ''), 'm_Position', curve, idx)
        if lines:
            tracks.extend(lines)
            idx += 1
    # Scale curves
    for sc in d.get('m_ScaleCurves', []):
        curve = sc.get('curve', {})
        keys = parse_curve(curve)
        if keys:
            length = max(length, keys[-1][0])
        lines = conv_track(name, sc.get('path', ''), 'm_Scale', curve, idx)
        if lines:
            tracks.extend(lines)
            idx += 1
    if idx == 0:
        return None
    header = f'[gd_resource type="Animation" format=3]\n\n[resource]\nlength = {length:g}\nloop_mode = {0 if d.get("m_WrapMode") == 0 else 1}\n'
    body = '\n'.join(tracks)
    os.makedirs(out_dir, exist_ok=True)
    safe = name.replace(' ', '_').replace('/', '_')
    out = os.path.join(out_dir, safe + '.tres')
    with open(out, 'w', encoding='utf-8') as fp:
        fp.write(header + body + '\n')
    return out


def main():
    n_ok = 0
    for f in glob.glob(os.path.join(SRC, '*.json')):
        if f.rsplit('_', 1)[1].replace('.json', '').lstrip('-').isdigit() is False:
            continue
        if f.count('_') >= 2 and not f.rsplit('_', 2)[1].lstrip('-').isdigit():
            continue  # 跳过 <name>_<pid>_<pid> 双后缀
        if f.rsplit('_', 1)[1].replace('.json', '').lstrip('-').isdigit() and f.rsplit('_', 1)[1].replace('.json', '').isdigit() and f.rsplit('_', 2)[-1].replace('.json', '').isdigit():
            # 双后缀跳过
            if f.rsplit('_', 2)[1].replace('.json', '').lstrip('-').isdigit():
                continue
        try:
            out = convert_file(f, OUT)
            if out:
                n_ok += 1
        except Exception as e:
            print(f'[跳过] {os.path.basename(f)}: {e}')
    print(f'完成: 转化 {n_ok} 个动画 → {OUT}')


if __name__ == '__main__':
    main()
