# -*- coding: utf-8 -*-
"""import_altstyle_logos.py -- 导出两张「画风 logo」（账 A1808）并装进工程。

来源 = 原版【远端】Addressables 包 `alternateartstyles_assets_all`（本机唯一一份在旧 profile 缓存里）。
只读 d:/2，写 `MyGame/Assets/CardPresentation/Resources/Art/ui_menu/`。

用法:
  python -I -X utf8 d:/4/Unity/工具/import_altstyle_logos.py            # 只列包内对象 + 导出到 _tmp_view
  python -I -X utf8 d:/4/Unity/工具/import_altstyle_logos.py --install  # 导进工程（ui_menu/，原版名·空格换下划线）

判据（2026-10-14 现读，三路互证）：
  · 包路径 = `Unity缓存_按哈希/7416b641…/9db3b696…/__data`（69,579 B；旧/新两版 catalog 里内容哈希**相同**）
  · 包内只有 5 个对象：AssetBundle + 2×(Sprite+Texture2D)，名字 = `Cell Shaded Alternative Art Style` /
    `WarpForgeLogo Style`（各 512×128）
  · `AlternateArtStyleIconsSO` 本体在 **`sharedassets0.assets`**（内置文件、无 type tree，按原始字节解）：
    `m_Name`(24) → `count=2` → `[style:int32][AssetGUID]`×2 ⇒ **style 0(Default) → 0195addac6…** ·
    **style 5(HammerAndBolter) → cd516d1bf3…**
  · 远端 catalog（`_guid_resolve_probe.py`）逐条解到 **束 = alternateartstyles**；束内 `m_Container`
    把 internalId `'0'` → **WarpForgeLogo Style**、`'c'` → **Cell Shaded Alternative Art Style**
  ⇒ 权威映射：**`Default` = WarpForge logo · `HammerAndBolter` = CEL SHADED logo**。
"""
import sys, os, io, argparse, shutil

BUNDLE = ("d:/2/远端缓存_旧profile_1013/Unity缓存_按哈希/"
          "7416b641463ac062ba3506935b12db35/9db3b69645d5771ac2a8991107b9d9ef/__data")
OUT_TMP = "d:/4/_tmp_view/a1808"
# 落进工程的名字：**原版名、空格换下划线**（`CardArt.MenuUi` 的命名约定，见它自己的 doc）
INSTALL = {
    "Cell Shaded Alternative Art Style": "Cell_Shaded_Alternative_Art_Style",
    "WarpForgeLogo Style":               "WarpForgeLogo_Style",
}
UI_MENU = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--bundle", default=BUNDLE)
    ap.add_argument("--out", default=OUT_TMP)
    ap.add_argument("--install", action="store_true")
    a = ap.parse_args()

    import UnityPy
    if not os.path.isfile(a.bundle):
        print("!! 包不存在: " + a.bundle); return 2
    env = UnityPy.load(a.bundle)
    print("== 包: %s (%.0f B)" % (a.bundle, os.path.getsize(a.bundle)))

    # ⚠️ 本包【没有 type tree】⇒ `str(o.type)` 给的是裸类号（28 = Texture2D · 213 = Sprite），
    #    不是名字 —— 两种形态都收（2026-10-14 实测踩过：只按名字筛 ⇒ 导出 0 张）。
    SPRITE_IDS = (28, "28", "Texture2D", 213, "213", "Sprite")
    os.makedirs(a.out, exist_ok=True)
    if a.install:
        os.makedirs(UI_MENU, exist_ok=True)

    ok = 0
    for o in env.objects:
        if str(o.type) not in SPRITE_IDS:
            continue
        d = o.read()
        nm = getattr(d, "m_Name", None) or ("pid_%s" % o.path_id)
        # Sprite 的矩形/pivot 记下来（判「原版怎么摆」要用）：`Art Style Logo` 那一格是 512×128
        if str(o.type) in ("213", "Sprite"):
            try:
                r = d.m_Rect
                print("   [Sprite] %s  rect=%.0fx%.0f ppu=%s"
                      % (nm, r.width, r.height, getattr(d, "m_PixelsToUnits", "?")))
            except Exception as e:
                print("   [Sprite] %s  <rect 读不到: %s>" % (nm, e))
        img = d.image
        if img is None:
            print("   !! %s 无 image" % nm); continue
        if a.install:
            if nm not in INSTALL:
                print("   ⚠️ 未登记的名字、跳过（⛔ 不静默）：%s" % nm); continue
            p = os.path.join(UI_MENU, INSTALL[nm] + ".png")
        else:
            safe = "".join(c if (c.isalnum() or c in "-_ ") else "_" for c in nm).strip()
            p = os.path.join(a.out, "%s_%s.png" % (safe, o.path_id))
        img.save(p)
        print("   ✅ %s → %s (%dx%d)" % (nm, p, img.width, img.height))
        ok += 1
    print("== 写出 %d 张" % ok)
    if a.install:
        print("   判据：`CardArt.MenuUi(\"%s\")` / `MenuUi(\"%s\")`"
              % (INSTALL["Cell Shaded Alternative Art Style"], INSTALL["WarpForgeLogo Style"]))
        print("   ⚠️ 新图要 Unity 导入一次才会进包（本工具只落盘，不碰 Unity）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
