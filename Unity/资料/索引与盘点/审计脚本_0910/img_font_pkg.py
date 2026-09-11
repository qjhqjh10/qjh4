# -*- coding: utf-8 -*-
"""审计 10：
 (a) PNG alpha 健康度（全不透明 / 透明区 RGB 是否为黑=预乘嫌疑 / 尺寸）
 (b) 10_字体 目录真实内容
 (c) .unitypackage 内 preview.png 是否空白
"""
import os, sys, json, zipfile, io, hashlib, collections

sys.stdout.reconfigure(encoding="utf-8")
from PIL import Image

ROOT = r"d:/2/解包整理"
OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"

# ---------- (a) PNG alpha ----------
print("=== (a) PNG alpha 健康度 ===")
dirs = ["07_场景", "08_预制体特效/共享资源/Texture2D", "08_预制体特效/战斗预制体/Texture2D",
        "03_界面UI/去重资源/Texture2D", "01_卡牌", "09_游戏数据/卡包/Texture2D"]
stat = collections.Counter()
per_dir = collections.defaultdict(lambda: collections.Counter())
samples = collections.defaultdict(list)
for d in dirs:
    base = os.path.join(ROOT, d)
    if not os.path.isdir(base):
        continue
    for dp, dn, fn in os.walk(base):
        for f in fn:
            if not f.lower().endswith(".png"):
                continue
            p = os.path.join(dp, f)
            rel = os.path.relpath(p, ROOT).replace("\\", "/")
            key = d
            try:
                im = Image.open(p)
                if im.mode != "RGBA":
                    im = im.convert("RGBA")
                a = im.getchannel("A")
                amin, amax = a.getextrema()
                if amin == 255:
                    k = "全不透明(alpha 恒 255)"
                elif amax == 0:
                    k = "全透明(alpha 恒 0)"
                else:
                    k = "有透明度"
                    # 检查 alpha=0 处 RGB 是否为黑 (预乘/无效区渲染)
                    px = im.getdata()
                    nzero = 0; nblack = 0
                    for r, g, b, al in list(px)[::max(1, len(px)//4000)]:
                        if al == 0:
                            nzero += 1
                            if r == 0 and g == 0 and b == 0:
                                nblack += 1
                    if nzero > 20 and nblack == nzero:
                        k = "有透明度+透明区全黑(预乘)"
                stat[k] += 1
                per_dir[key][k] += 1
                if len(samples[k]) < 5:
                    samples[k].append((rel, im.size, amin, amax))
            except Exception as e:
                stat["打开失败"] += 1
                per_dir[key]["打开失败"] += 1
print("  总:", dict(stat))
for k, v in per_dir.items():
    print("   ", k, dict(v))
for k, ss in samples.items():
    print("   样例", k, ss[:4])

# ---------- (b) 10_字体 ----------
print()
print("=== (b) 10_字体 ===")
for dp, dn, fn in os.walk(os.path.join(ROOT, "10_字体")):
    rel = os.path.relpath(dp, ROOT).replace("\\", "/")
    print("  ", rel, "->", len(fn), "文件", sorted(fn)[:6])
fonts = []
for dp, dn, fn in os.walk(os.path.join(ROOT, "10_字体")):
    for f in fn:
        if f.lower().endswith(".ttf"):
            p = os.path.join(dp, f)
            fonts.append((os.path.relpath(p, ROOT).replace("\\", "/"), os.path.getsize(p),
                          open(p, "rb").read(4)))
print("  ttf 文件:", len(fonts))
for f in fonts:
    print("   ", f)

# ---------- (c) unitypackage ----------
print()
print("=== (c) .unitypackage preview.png ===")
PKGS = []
for base in (r"d:/4/Unity/素材", r"d:/2"):
    if os.path.isdir(base):
        for dp, dn, fn in os.walk(base):
            for f in fn:
                if f.lower().endswith(".unitypackage"):
                    PKGS.append(os.path.join(dp, f))
print("  找到", len(PKGS), "个")
for pkg in PKGS[:6]:
    try:
        z = zipfile.ZipFile(pkg)
    except Exception as e:
        print("   ", pkg, "打开失败", e); continue
    names = z.namelist()
    prev = [n for n in names if n.endswith("/preview.png") and n.count("/") == 1]
    assets = [n for n in names if n.endswith("/asset") and n.count("/") == 1]
    prefab_prev = 0
    blank = 0
    sizes = collections.Counter()
    md5s = collections.Counter()
    for n in prev:
        data = z.read(n)
        sizes[len(data)] += 1
        md5s[hashlib.md5(data).hexdigest()] += 1
    print(f"   {os.path.basename(pkg)}: 条目={len(names)} 资产={len(assets)} preview={len(prev)}")
    print(f"       preview 大小分布 top5: {sizes.most_common(5)}")
    print(f"       preview 唯一 md5 数: {len(md5s)}  最常见 md5 次数: {md5s.most_common(3)}")
    # 判定空白
    nblank = 0
    for n in prev:
        try:
            im = Image.open(io.BytesIO(z.read(n))).convert("RGBA")
            if im.getchannel("A").getextrema() == (0, 0) or len(set(im.getdata())) <= 1:
                nblank += 1
        except Exception:
            pass
    print(f"       其中纯色/全透明 = {nblank}/{len(prev)}")
