# -*- coding: utf-8 -*-
"""审计 19：battlearena1 的 4 张场景贴图像素统计（判断背景是否真的需要 alpha 混合）。"""
import os, sys
sys.stdout.reconfigure(encoding="utf-8")
from PIL import Image

D = r"d:/2/解包整理/07_场景/battlearena1/Texture2D"
for f in sorted(os.listdir(D)):
    p = os.path.join(D, f)
    im = Image.open(p).convert("RGBA")
    w, h = im.size
    px = list(im.getdata())
    n = len(px)
    opaque = sum(1 for r, g, b, a in px if a > 250)
    clear = sum(1 for r, g, b, a in px if a < 5)
    semi = n - opaque - clear
    # 不透明区的平均色
    if opaque:
        sr = sum(r for r, g, b, a in px if a > 250) / opaque
        sg = sum(g for r, g, b, a in px if a > 250) / opaque
        sb = sum(b for r, g, b, a in px if a > 250) / opaque
    else:
        sr = sg = sb = 0
    # 透明区是否黑
    tz = [(r, g, b) for r, g, b, a in px if a < 5]
    black = sum(1 for r, g, b in tz if r < 8 and g < 8 and b < 8)
    print(f"{f}")
    print(f"   {w}x{h}  不透明 {100*opaque/n:.1f}%  全透明 {100*clear/n:.1f}%  半透明 {100*semi/n:.1f}%")
    print(f"   不透明区平均 RGB = ({sr:.0f},{sg:.0f},{sb:.0f})  透明区像素 {len(tz)} 其中纯黑 {black}")
