# -*- coding: utf-8 -*-
"""审计 13：判定 _X / _X_<pid> 两个文件到底是「同一对象从两个包各 dump 一次」
还是「两个同名不同对象」。直接查源 bundle 里同名对象的 pathID 集合。
"""
import os, sys, json, collections

sys.stdout.reconfigure(encoding="utf-8")
import UnityPy

BUNDLE_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"

TARGETS = [
    ("cosmeticscardbacksimages_assets_all.bundle", "Sprite", "Cardback_Season_July_2024_Tier1_Main"),
    ("aeldarisaimhanncardassets_assets_all.bundle", "Sprite", "40k_Cardframe_stratagem_SaimHann_SDF_tier1"),
]
# 也查 _X_X 模式的一个对象: Material_10 (battlearena1)
TARGETS2 = [("scenes_scenes_battlearena1.bundle", "Material", None),
            ("battlesharedresources_assets_all.bundle", "Material", None)]

for bname, tn, want in TARGETS:
    p = os.path.join(BUNDLE_DIR, bname)
    if not os.path.exists(p):
        print("缺包", bname); continue
    env = UnityPy.load(p)
    found = []
    for o in env.objects:
        if o.type.name != tn:
            continue
        try:
            nm = o.read_typetree().get("m_Name")
        except Exception:
            continue
        if nm == want:
            found.append(o.path_id)
    print(f"{bname} {tn} '{want}': {len(found)} 个 -> {found}")

# 跨包统计: 同一个 pid 出现在几个包里
print()
print("=== 同一 pathID 在不同 bundle 中重复出现的情况（抽 3 个包）===")
for bname in ["scenes_scenes_battlearena1.bundle", "battlesharedresources_assets_all.bundle",
              "battleprefabs_vfxandmisc_assets_all.bundle"]:
    p = os.path.join(BUNDLE_DIR, bname)
    if not os.path.exists(p):
        continue
    env = UnityPy.load(p)
    c = collections.Counter(o.type.name for o in env.objects)
    print(f"  {bname}: {dict(c)}")
