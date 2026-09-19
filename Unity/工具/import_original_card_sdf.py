#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""import_original_card_sdf.py — 把原版卡面那层 **SDF 软光/影** 的贴图导进工程

**为什么要它**：原版卡面最底层有一层 4.4281² 的软光/影（`Card Highlight And Shadow`）——
卡牌落地感的来源。2026-09-19 查实（`资料/普查产出_0919/卡面SDF软光影_查证.md`）：
它不是运行时生成的，而是 **Addressables 里预生成的 SDF 资产**：
`40k_Cardframe{s}_{troop|stratagem}_<阵营>_SDF_tier{1..4}`，**13 阵营 × 4 tier × 2 类型 = 104 张**，
本机解包资源里就有。另有一张**通用**的 `Card board frame SDF.png`。

**导出到哪**：`Assets/CardPresentation/Resources/Art/card_sdf/`（该目录 **gitignore**，是本地件，
和 `cards/frame_*.png` 同一个规矩：随时可以用本脚本重建）。
命名与 `cards/frame_*` **一一对应**：`frame_goff_tier2.png` ↔ `card_sdf/goff_tier2.png`、
`frame_goff_strat_tier2.png` ↔ `card_sdf/goff_strat_tier2.png`（`CardArt` 两边用同一套拼法）。

用法
----
  PYTHONIOENCODING=utf-8 "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/import_original_card_sdf.py
  # 加 --check 只体检、不写文件

⚠️ 导出来的仍是**原版美术**。本项目是个人学习用途（版权红线 2026-09-18 已由用户取消），照用。
"""
import argparse
import glob
import io
import os
import re
import shutil
import sys

SRC_GLOB = "d:/2/新解包资源/assets_full/bundle_*cardassets_assets_all/Texture2D/*SDF_tier*.png"
GENERIC = ("d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Texture2D/"
           "Card board frame SDF.png")
DEST_DIR = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/card_sdf"

# 原版 SDF 里的阵营名 → **我们工程的阵营 key**（= `cards/frame_<key>.png` 里那一段，全小写）
# ⚠️ 四个不是「小写去空格」就能对上的，**必须显式写出来**（写错不会报错，只会静静少一层软光）：
#      Emperor Children → emperorschildren（我们多一个 s）
#      GSC              → genestealers（原版这里是缩写）
#      Orks             → goff（我们的兽人阵营叫 Goff）
#      Tau              → tauempire
#   另外两个「去空格」就对的：Astra Militarum → astramilitarum · Space Wolves → spacewolves。
FACTION = {
    "Astra Militarum": "astramilitarum",
    "BlackLegion": "blacklegion",
    "DarkAngels": "darkangels",
    "Emperor Children": "emperorschildren",
    "GSC": "genestealers",
    "Leviathan": "leviathan",
    "Orks": "goff",
    "SaimHann": "saimhann",
    "Sautekh": "sautekh",
    "Sororitas": "sororitas",
    "Space Wolves": "spacewolves",
    "Tau": "tauempire",
    "Ultramarines": "ultramarines",
}

RE = re.compile(r"40k_Cardframes?_(troop|Troop|stratagem|Stratagem)_(.+)_SDF_tier(\d)\.png$")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只体检，不写文件")
    args = ap.parse_args()

    files = sorted(glob.glob(SRC_GLOB))
    print(f"[1] 源目录里找到 {len(files)} 张 SDF 贴图（期望 13 阵营 × 4 tier × 2 类型 = 104）")

    jobs, unknown, seen = [], [], {}
    for p in files:
        m = RE.search(os.path.basename(p))
        if not m:
            unknown.append(os.path.basename(p))
            continue
        kind, fac, tier = m.group(1), m.group(2), m.group(3)
        key = FACTION.get(fac)
        if key is None:
            unknown.append(f"{os.path.basename(p)}（阵营「{fac}」没在 FACTION 表里）")
            continue
        name = f"{key}{'_strat' if kind.lower() == 'stratagem' else ''}_tier{tier}.png"
        if name in seen:
            print(f"      !! 重名：{name} ← {os.path.basename(p)} 与 {seen[name]}")
        seen[name] = os.path.basename(p)
        jobs.append((p, name))

    print(f"[2] 解析出 {len(jobs)} 张、目标名 {len(seen)} 个唯一")
    if unknown:
        print(f"[2b] 解析不出来的 {len(unknown)} 个：")
        for u in unknown:
            print("      !!", u)

    # 对账：每个阵营都该有 4 tier × 2 类型
    miss = []
    for key in sorted(set(FACTION.values())):
        for t in "1234":
            for s in ("", "_strat"):
                if f"{key}{s}_tier{t}.png" not in seen:
                    miss.append(f"{key}{s}_tier{t}.png")
    print(f"[3] 按「13 阵营 × 4 tier × 2 类型」对账：缺 {len(miss)} 个")
    for m in miss:
        print("      !! 缺", m)

    if args.check:
        print("\n--check：只体检，未写文件")
        return 0 if not (unknown or miss) else 1

    os.makedirs(DEST_DIR, exist_ok=True)
    for src, name in jobs:
        shutil.copyfile(src, os.path.join(DEST_DIR, name))
    print(f"[4] 已写入 {len(jobs)} 张 → {DEST_DIR}")

    if os.path.exists(GENERIC):
        shutil.copyfile(GENERIC, os.path.join(DEST_DIR, "generic.png"))
        print("[5] 通用那张 `Card board frame SDF.png` → generic.png")
    else:
        print("[5] !! 通用那张没找到（不影响，只是少一个兜底）")

    total = sum(os.path.getsize(os.path.join(DEST_DIR, f)) for f in os.listdir(DEST_DIR))
    print(f"[6] 目录现在 {len(os.listdir(DEST_DIR))} 个文件、{total/1024:.0f} KB")
    print("\n⚠️ 导完记得在 Unity 里跑一次 `ArtBaker.ApplyImportSettings`（mipmap/alpha 那套统一设置）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
