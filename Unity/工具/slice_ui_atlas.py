# -*- coding: utf-8 -*-
"""从原版 UI 图集里切出独立 PNG。

为什么需要它：**Unity 的 Sprite 数据里没有图集坐标** —— `Sprite.m_Rect` / `m_RD.textureRect`
全是 (0,0,w,h)（那是 sprite 自己的尺寸），`uvTransform` 是枢轴偏移，都不是它在图集里的位置。
真正的位置只在 `SpriteAtlas.m_RenderDataMap` 的 `textureRect` 里，按 `m_PackedSprites` 的顺序一一对应。
（这条踩过，详见 `资料/原版复刻_场景与美术.md` 第四节）

用法：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \
    d:/4/Unity/工具/slice_ui_atlas.py <bundle 关键字> [输出子目录名]

例：
  slice_ui_atlas.py battleatlasui battleatlasui
  slice_ui_atlas.py 0_mainmenu 0_mainmenu

产物：`Assets/CardPresentation/Art/原版/<名字>/`（**备查库**，不是运行时用的；实际要用哪几张
再拷进 `Resources/Art/`）+ 同目录 `_atlas_rects.json`（sprite 名 → 图集里的矩形）。

⚠️ 有些图集里同一张图 pack 了两份（名字带 `_<数字>` 后缀）。这里按**像素内容去重**，
保存时跳过字节完全相同的副本，并在末尾报告去重前后的数量。
"""
import os
import sys
import json
import hashlib
import UnityPy

sys.stdout.reconfigure(encoding="utf-8")

AA = "d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
OUTROOT = "d:/4/Unity/MyGame/Assets/CardPresentation/Art/原版"

key = sys.argv[1] if len(sys.argv) > 1 else "battleatlasui"
sub = sys.argv[2] if len(sys.argv) > 2 else key

# 在 aa 目录里找唯一匹配的图集 bundle
hits = [f for f in os.listdir(AA) if f.endswith(".bundle") and key.lower() in f.lower()]
if not hits:
    print("找不到 bundle：", key)
    sys.exit(1)
if len(hits) > 1:
    print("匹配到多个 bundle，请写更精确的关键字：", hits)
    sys.exit(1)
BUNDLE = os.path.join(AA, hits[0])
OUT = os.path.join(OUTROOT, sub)
os.makedirs(OUT, exist_ok=True)

print("图集 bundle :", os.path.basename(BUNDLE))
print("输出        :", OUT)

env = UnityPy.load(BUNDLE)
atlas_img = None
atlas = None
sprite_names_by_id = {}
for o in env.objects:
    if o.type.name == "Texture2D":
        img = o.read().image.convert("RGBA")
        # 图集本体是最大的那张，其余是零散贴图
        if atlas_img is None or img.size[0] * img.size[1] > atlas_img.size[0] * atlas_img.size[1]:
            atlas_img = img
    elif o.type.name == "Sprite":
        sprite_names_by_id[o.path_id] = o.read().m_Name
    elif o.type.name == "SpriteAtlas":
        atlas = o.read()

if atlas is None or atlas_img is None:
    print("这个 bundle 里没有 SpriteAtlas（它可能是普通 UI 资源包，不是图集包）")
    sys.exit(2)

TW, TH = atlas_img.size
names = list(atlas.m_PackedSpriteNamesToIndex)
made, seen_hash, rects, dupes = 0, {}, {}, []

for i, (key_, rd) in enumerate(atlas.m_RenderDataMap):
    name = names[i] if i < len(names) else str(key_)
    r = rd.textureRect
    x, y, w, h = int(r.x), int(r.y), int(r.width), int(r.height)
    if w <= 0 or h <= 0 or x + w > TW or y + h > TH:
        print("  跳过（矩形越界）", name, (x, y, w, h))
        continue
    y0 = TH - y - h                       # Unity 的 rect y 从下往上
    im = atlas_img.crop((x, y0, x + w, y0 + h))
    sig = hashlib.md5(im.tobytes()).hexdigest()
    if sig in seen_hash:
        dupes.append((name, seen_hash[sig]))
        continue
    seen_hash[sig] = name
    safe = name.replace("/", "_").replace(" ", "_")
    im.save(os.path.join(OUT, safe + ".png"))
    rects[name] = [x, y, w, h]
    made += 1

json.dump(rects, open(os.path.join(OUT, "_atlas_rects.json"), "w", encoding="utf-8"),
          ensure_ascii=False, indent=1)
print(f"图集 {TW}x{TH}：packed {len(names)} 个 → 切出 {made} 张，跳过像素相同的副本 {len(dupes)} 个")
print("样例 sprite 名：", list(rects)[:8])
