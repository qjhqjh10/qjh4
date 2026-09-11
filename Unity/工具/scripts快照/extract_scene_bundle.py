#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
extract_scene_bundle.py — 从场景 AssetBundle 导出完整 UI 场景 (与 03_界面UI 同构)
来源: aa/StandaloneWindows64/scenes_scenes_*.bundle
输出: d:/2/Warpforge_tools/data/ui_scene/<bundle名>/
      GameObject / RectTransform / MonoBehaviour / CanvasRenderer / Sprite(*.png+json) / Texture2D
用法: d:/2/Warpforge_tools/py312/python.exe extract_scene_bundle.py <bundle名>
"""
import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

BUNDLE_DIR = 'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/'
OUT_DIR = 'd:/2/Warpforge_tools/data/ui_scene/'


def main() -> int:
    if len(sys.argv) < 2:
        print('用法: extract_scene_bundle.py <bundle名>')
        return 1
    name = sys.argv[1]
    path = os.path.join(BUNDLE_DIR, name + '.bundle')
    if not os.path.exists(path):
        print(f'[错误] {path} 不存在')
        return 1
    import UnityPy
    env = UnityPy.load(path)
    out = os.path.join(OUT_DIR, name)
    counts = {}
    for obj in env.objects:
        try:
            t = obj.type.name
            if t in ('GameObject', 'RectTransform', 'Transform',
                     'MonoBehaviour', 'CanvasRenderer', 'CanvasGroup',
                     'Canvas', 'Button', 'Image', 'Text', 'ScrollRect',
                     'TextMeshProUGUI', 'HorizontalLayoutGroup',
                     'VerticalLayoutGroup', 'LayoutElement', 'Mask'):
                data = obj.read_typetree()
                mname = str(data.get('m_Name') or obj.path_id)
                fname = mname.replace('/', '_')[:60]
                sub = os.path.join(out, t)
                os.makedirs(sub, exist_ok=True)
                fp = os.path.join(sub, f'{fname}_{obj.path_id}.json')
                if not os.path.exists(fp):
                    with open(fp, 'w', encoding='utf-8') as f:
                        json.dump(data, f, ensure_ascii=False, indent=1)
                    counts[t] = counts.get(t, 0) + 1
            elif t == 'Sprite':
                data = obj.read()
                sname = data.m_Name
                if not sname:
                    continue
                sub = os.path.join(out, 'Sprite')
                os.makedirs(sub, exist_ok=True)
                img = data.image
                if img is not None:
                    fp = os.path.join(sub, sname + '.png')
                    if not os.path.exists(fp):
                        img.save(fp)
                        counts['Sprite'] = counts.get('Sprite', 0) + 1
                rect = data.m_Rect
                info = {'m_Name': sname, 'pathid': getattr(obj, 'path_id', None),
                        'm_Rect': {'x': rect.x, 'y': rect.y,
                                   'width': rect.width, 'height': rect.height}}
                fp = os.path.join(sub, sname + '.json')
                if not os.path.exists(fp):
                    with open(fp, 'w', encoding='utf-8') as f:
                        json.dump(info, f, ensure_ascii=False, indent=1)
            elif t == 'Texture2D':
                data = obj.read()
                tname = data.m_Name
                if not tname:
                    continue
                img = data.image
                if img is not None:
                    sub = os.path.join(out, 'Texture2D')
                    os.makedirs(sub, exist_ok=True)
                    fp = os.path.join(sub, tname + '.png')
                    if not os.path.exists(fp):
                        img.save(fp)
                        counts['Texture2D'] = counts.get('Texture2D', 0) + 1
        except Exception:
            continue
    print(f'✓ {name}: ' + ', '.join(f'{k} {v}' for k, v in sorted(counts.items())))
    print(f'  -> {out}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
