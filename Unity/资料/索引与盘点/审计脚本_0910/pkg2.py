# -*- coding: utf-8 -*-
""".unitypackage = tar.gz；统计 preview.png 大小/md5 与空白比例。"""
import os, sys, tarfile, io, hashlib, collections

sys.stdout.reconfigure(encoding="utf-8")
from PIL import Image

PKGS = []
for base in (r"d:/4/Unity/素材", r"d:/2"):
    if os.path.isdir(base):
        for dp, dn, fn in os.walk(base):
            for f in fn:
                if f.lower().endswith(".unitypackage"):
                    PKGS.append(os.path.join(dp, f))
print("找到", len(PKGS))

for pkg in PKGS:
    print("=" * 70)
    print(os.path.basename(pkg))
    try:
        tf = tarfile.open(pkg, "r:gz")
    except Exception as e:
        print("  打开失败", e)
        continue
    n = 0
    sizes = collections.Counter()
    md5s = collections.Counter()
    nprev = 0
    nblank = 0
    prefab_prev_sizes = collections.Counter()
    paths = []
    for m in tf:
        n += 1
        bn = os.path.basename(m.name)
        if bn == "pathname":
            try:
                paths.append(tf.extractfile(m).read().decode("utf-8", "replace").strip())
            except Exception:
                pass
        if bn == "preview.png":
            nprev += 1
            data = tf.extractfile(m).read()
            sizes[len(data)] += 1
            md5s[hashlib.md5(data).hexdigest()] += 1
            if len(data) < 1000:
                prefab_prev_sizes[len(data)] += 1
    print(f"  条目 {n}  pathname {len(paths)}  preview.png {nprev}")
    print(f"  preview 大小 top5: {sizes.most_common(5)}")
    print(f"  preview 唯一 md5: {len(md5s)}  最常见 md5 次数: {md5s.most_common(2)}")
    nblank = 0
    for m in tf:
        if os.path.basename(m.name) != "preview.png":
            continue
        try:
            data = tf.extractfile(m).read()
            im = Image.open(io.BytesIO(data)).convert("RGBA")
            if len(set(im.getdata())) <= 1:
                nblank += 1
        except Exception:
            nblank += 1
    print(f"  纯色/空白 preview: {nblank}/{nprev}")
    # prefab 采样
    print("  prefab 路径样例:", [p for p in paths if p.lower().endswith(".prefab")][:5])
