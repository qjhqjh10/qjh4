# -*- coding: utf-8 -*-
"""审计 17：mtime 对比 —— 有 pid 后缀的文件是否比无后缀的更新（验证「二次运行」假说）。"""
import os, re, sys, datetime

sys.stdout.reconfigure(encoding="utf-8")
ROOT = r"d:/2/解包整理"
PAT = re.compile(r"_(-?\d{5,})$")

def rng(lst):
    if not lst:
        return "n/a"
    return (datetime.datetime.fromtimestamp(min(x[0] for x in lst)).strftime("%m-%d %H:%M:%S"),
            datetime.datetime.fromtimestamp(max(x[0] for x in lst)).strftime("%m-%d %H:%M:%S"),
            len(lst))

CHECK = [
    ("07_场景/battlearena1", ["ParticleSystem", "Material", "CanvasRenderer", "Transform", "GameObject"]),
    ("08_预制体特效/战斗预制体", ["ParticleSystem", "Transform", "Material", "GameObject"]),
    ("03_界面UI/菜单", ["RectTransform", "MonoBehaviour", "CanvasRenderer"]),
    ("01_卡牌/Ultramarines_极限战士", ["Sprite"]),
    ("02_装饰品/卡背", ["Sprite", "Texture2D"]),
]
for base_rel, classes in CHECK:
    print("===", base_rel)
    for cls in classes:
        d = os.path.join(ROOT, base_rel, cls)
        if not os.path.isdir(d):
            continue
        a, b = [], []
        for f in os.listdir(d):
            s = os.path.splitext(f)[0]
            p = os.path.join(d, f)
            try:
                m = os.path.getmtime(p)
            except OSError:
                continue
            (b if PAT.search(s) else a).append((m, f))
        print(f"  {cls:18s} 无后缀 {rng(a)}")
        print(f"  {'':18s} 有后缀 {rng(b)}")
