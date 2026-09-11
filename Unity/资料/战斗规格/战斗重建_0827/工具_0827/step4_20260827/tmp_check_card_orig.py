# -*- coding: utf-8 -*-
"""检查: 原版卡牌 vs 项目合成 face 的透明圈/尺寸 (2026-08-27 Step4 用户三点)"""
from PIL import Image
import os

paths = {
    "orig_zoom": "D:/2/Warpforge部队卡片/Ultramarines/3部队/desolation.png",
    "proj_face": "D:/warpforge/assets/cards/faces/Ultramarines_极限战士/desolation.png",
    "proj_art": "D:/warpforge/assets/cards/art/Ultramarines_极限战士/SM_UM_Inf_Desolation Marine.png",
}
for tag, p in paths.items():
    if not os.path.exists(p):
        print(tag, "MISSING", p)
        continue
    img = Image.open(p)
    w, h = img.size
    a = img.getchannel("A")
    bbox = a.getbbox()
    px = img.load()
    print("%s size=%sx%s bbox=%s corner_alphas=%s/%s/%s/%s rgb_TL=%s rgb_TR=%s" % (
        tag, w, h, bbox, a.getpixel((0, 0)), a.getpixel((w - 1, 0)),
        a.getpixel((0, h - 1)), a.getpixel((w - 1, h - 1)),
        px[0, 0], px[w - 1, 0]))

# 原版阵营文件夹全貌
for root, dirs, files in os.walk("D:/2/Warpforge部队卡片/Ultramarines"):
    for f in files[:6]:
        print("origDir:", os.path.join(root, f))
    break
