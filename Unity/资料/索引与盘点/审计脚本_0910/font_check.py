# -*- coding: utf-8 -*-
"""审计 16：10_字体 —— Font JSON 内嵌 m_FontData 是否完整，且能否还原成 ttf。"""
import os, sys, json, glob

sys.stdout.reconfigure(encoding="utf-8")
ROOT = r"d:/2/解包整理/10_字体"
for dp, dn, fn in os.walk(ROOT):
    for f in sorted(fn):
        if not f.endswith(".json"):
            continue
        p = os.path.join(dp, f)
        try:
            d = json.load(open(p, encoding="utf-8"))
        except Exception as e:
            print("  解析失败", p, e); continue
        if "m_FontData" not in d:
            continue
        fd = d["m_FontData"]
        raw = bytes(fd) if isinstance(fd, list) else (fd if isinstance(fd, (bytes, bytearray)) else b"")
        magic = raw[:4]
        print(f"{os.path.relpath(p, ROOT):52s} name={d.get('m_Name')!r:28s} "
              f"fontdata={len(raw):>10d} magic={magic!r}")

print()
print("=== ttf 落地文件 ===")
for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        if f.lower().endswith((".ttf", ".otf", ".ttc")):
            p = os.path.join(dp, f)
            print(f"  {os.path.relpath(p, ROOT)}: {os.path.getsize(p)} 字节")

print()
print("=== 字体资源/Texture2D PNG 尺寸 ===")
try:
    from PIL import Image
    td = os.path.join(ROOT, "字体资源/Texture2D")
    for f in sorted(os.listdir(td)):
        p = os.path.join(td, f)
        if f.lower().endswith(".png"):
            try:
                im = Image.open(p)
                print(f"  {f}: {im.size} {im.mode}")
            except Exception as e:
                print(f"  {f}: 打开失败 {e}")
except ImportError:
    pass

# 全局: 找其它 0 字节 / 极小 PNG
print()
print("=== 全树极小 PNG (<200 字节) ===")
ROOT2 = r"d:/2/解包整理"
n = 0
for dp, dn, fn in os.walk(ROOT2):
    for f in fn:
        if f.lower().endswith(".png"):
            p = os.path.join(dp, f)
            sz = os.path.getsize(p)
            if sz < 200:
                n += 1
                print(f"  {sz:6d}  {os.path.relpath(p, ROOT2)}")
print("  共", n)
