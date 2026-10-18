#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""import_deck_warlord_art.py — 把原版**卡组格的督军方形立绘**整族导进 Unity 工程。

为什么有它（2026-10-18）：
· 卡组格（`MenuDraw.DeckCell`）原版主图 = `deckHero.cardImageReference`，但**不是卡面插图**：
  原版另有一族 **`UI_Deck_Warlord_<督军名>`**（`bundle_uiwarlords_assets_all`，**2048×2048 方形**，
  白底抠图、构图居中）—— 我们此前用卡面插图（668×1024）拉到 347.7×346.4 的方形框里再遮罩，
  于是「战将立绘残缺/变形」。判据：`bundle_uiwarlords_assets_all/Texture2D/` 现读 63 张；
  同族先例 `Shell/FTUESelectDeckContainer.cs` 的 `HeroArt` 已经按这个名字取。
· 本脚本只**拷贝**，不删任何东西；已存在的跳过（不覆盖）。
· 落盘名 = `UI_Deck_Warlord_<卡名，空格换下划线>.png`（与 `CardArt.MenuUi` 的取法一致，
  也已与 `Resources/Art/ui_menu/` 里现有的 4 张同名）。

用法：
    python -I -X utf8 d:/4/Unity/工具/import_deck_warlord_art.py [--check]
"""
import argparse
import os
import shutil
import sys

sys.stdout.reconfigure(encoding="utf-8")

SRC = "d:/2/新解包资源/assets_full/bundle_uiwarlords_assets_all/Texture2D"
OUT = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu"
PREFIX = "UI_Deck_Warlord_"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只列出会拷哪些，不写")
    a = ap.parse_args()

    if not os.path.isdir(SRC):
        print(f"FAIL 源目录不存在：{SRC}")
        return 2
    os.makedirs(OUT, exist_ok=True)

    src = sorted(f for f in os.listdir(SRC) if f.startswith(PREFIX) and f.endswith(".png"))
    if not src:
        print(f"FAIL 源目录里没有 {PREFIX}*.png")
        return 2

    copied = skipped = 0
    for f in src:
        name = f[len(PREFIX):-4]                    # <督军名>
        dst_name = PREFIX + name.replace(" ", "_") + ".png"
        dst = os.path.join(OUT, dst_name)
        if os.path.exists(dst):
            skipped += 1
            continue
        if a.check:
            print(f"WOULD  {f}  ->  {dst_name}")
        else:
            shutil.copyfile(os.path.join(SRC, f), dst)
        copied += 1

    verb = "会拷" if a.check else "已拷"
    print(f"OK {verb} {copied} 张 · 已存在跳过 {skipped} 张 · 源共 {len(src)} 张 -> {OUT}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
