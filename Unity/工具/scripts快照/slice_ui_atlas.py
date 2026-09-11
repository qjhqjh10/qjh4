#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
slice_ui_atlas.py — 按 Sprite JSON 的 rect 从图集纹理裁出独立小图
来源: data/ui_extract/<bundle>/ (Sprite/*.json + Texture2D/*.png)
输出: data/ui_extract/<bundle>/sliced/<sprite名>.png
注意: Unity rect 的 y 从下往上, 裁切时需翻转 (pil y = 图高 - rect.y - rect.h)
用法: d:/2/Warpforge_tools/py312/python.exe slice_ui_atlas.py <bundle名>
"""
import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PIL import Image

BASE = 'd:/2/Warpforge_tools/data/ui_extract/'


def main() -> int:
    if len(sys.argv) < 2:
        print('用法: slice_ui_atlas.py <bundle名>')
        return 1
    name = sys.argv[1]
    root = os.path.join(BASE, name)
    tex_dir = os.path.join(root, 'Texture2D')
    spr_dir = os.path.join(root, 'Sprite')
    out_dir = os.path.join(root, 'sliced')
    if not os.path.isdir(tex_dir) or not os.path.isdir(spr_dir):
        print(f'[错误] {root} 缺 Texture2D 或 Sprite 目录')
        return 1
    os.makedirs(out_dir, exist_ok=True)
    # 纹理: 名字 -> Image
    textures = {}
    for f in os.listdir(tex_dir):
        if f.endswith('.png'):
            textures[f[:-4]] = Image.open(os.path.join(tex_dir, f)).convert('RGBA')
    n = 0
    for f in sorted(os.listdir(spr_dir)):
        if not f.endswith('.json'):
            continue
        with open(os.path.join(spr_dir, f), encoding='utf-8') as fh:
            d = json.load(fh)
        rect = d.get('m_Rect') or d.get('textureRect') or {}
        w, h = rect.get('width', 0), rect.get('height', 0)
        if not w or not h:
            continue
        # 从包含该 sprite 的纹理裁 (图集 bundle 一般单纹理; 多纹理时按尺寸匹配)
        tex = None
        for tname, img in textures.items():
            if img.width >= rect['x'] + w and img.height >= rect['y'] + h:
                tex = img
                break
        if tex is None:
            continue
        x0 = int(rect['x'])
        y0 = int(tex.height - rect['y'] - h)  # y 翻转
        crop = tex.crop((x0, y0, int(x0 + w), int(y0 + h)))
        dest = os.path.join(out_dir, d.get('m_Name', f[:-5]) + '.png')
        if not os.path.exists(dest):
            crop.save(dest)
            n += 1
    print(f'✓ {name}: 切片 {n} 张 -> {out_dir}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
