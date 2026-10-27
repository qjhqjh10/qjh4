# -*- coding: utf-8 -*-
"""import_offer_backgrounds.py —— 把「Offer 底图」这一族图从原版图集切进工程。

为什么需要它（R3 · 界面线）：
  高级商店 / 活动报价的**卡片底图**是 `WF_Special offer_Bck_tier1/2/3`（各 316×739），
  它们**只躺在图集里**（`Offers Atlas`），`Sprite/*.json` 拿不到图集坐标 ——
  位置只在 `SpriteAtlas.m_RenderDataMap[].textureRect` 里，按 `m_PackedSpriteNamesToIndex` 的顺序对应。
  ⇒ 本脚本按那份 RenderDataMap 逐张切，**不手抄任何一个矩形**。

判据（全部现读，路径可复查）：
  · 图集 json  d:/2/新解包资源/assets_full/bundle_liveopsmenuimages_assets_all/SpriteAtlas/SpriteAtlas_6374560847574922512.json
      `m_PackedSpriteNamesToIndex` = [tier1, tier3, 40k_shop_popup_info_bg, tier2]
      `m_RenderDataMap[i][1].textureRect` = 每张在图集里的矩形（**Unity 的口径：y 从下往上**）
  · 图集贴图   同 bundle 的 `Texture2D/sactx-0-2048x1024-DXT5_BC3-Offers Atlas-*.png`（2048×1024 RGBA，已解码）
  · 谁引用它们（为什么值得导）：
      - `WF_Special offer_Bck_tier*` = `EventAssetReferenceType.BackgroundImage`(=1)，
        4 份 `Premium Item Offer` SO 的 `drawData.EventAssets[key=1]` 都指向 **tier1**
        （`bundle_cosmeticsso_assets_all/MonoBehaviour/Expansion UM1 Premium Item Offer.json` 等；
         GUID `117e7654c91ec124db66c365be480065` → `bundle_liveopsmenuimages_assets_all/AssetBundle/AssetBundle_1.json` 的容器键）。
        消费点 = **`PurchaseableOfferContainer.OnInitialize`** 的 `background` Image
        （`PurchaseableOfferContainer__OnInitialize.c`：`LoadAsset(offer,2)`→`foreground`(+0x58) ·
         `LoadAsset(offer,1)`→`background`(+0x60)，两者都再 `set_enabled(op_Implicit(sprite))`）。
        tier2 / tier3 在**离线数据里 0 个引用者**（只在图集与容器表里出现）—— 服务端报价那边用，照样导。
      - `40k_shop_popup_info_bg` = `Base Offer Popup` 的 `Artwork/background`（565×588，同在图集里）。
      - `40k_OfferBadge` 已切好在备查库（`Assets/CardPresentation/Art/原版/0_mainmenu/`），本脚本只**拷**。
  ⚠️ 导入后的 `.meta` 由 Unity 生成（与 `import_original_art.py` 同一条规矩）：
     **第一次导入前 `.meta` 不存在是正常的**；开过一次 Unity 之后要复核 `alphaIsTransparency`
     （那批菜单图的既有值是 **0**，见 `Resources/Art/ui_menu/WF_Special_offer_Value.png.meta`）。

用法（**默认只打印用法，不写盘**）：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \
      d:/4/Unity/工具/import_offer_backgrounds.py --apply
  -h / --help   只打印用法并退出（退出码 0）

产物：`d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu/`
  · `WF_Special_offer_Bck_tier1.png` · `..._tier2.png` · `..._tier3.png`  （316×739）
  · `40k_shop_popup_info_bg.png`                                                  （565×588）
  · `40k_OfferBadge.png`                                                          （256×256，从备查库拷）
重跑幂等（同名文件按字节比对，一样就跳过）。
"""
import io
import json
import os
import shutil
import sys

ASSETS = "d:/2/新解包资源/assets_full"
BUNDLE = "bundle_liveopsmenuimages_assets_all"
ATLAS_JSON = os.path.join(ASSETS, BUNDLE, "SpriteAtlas", "SpriteAtlas_6374560847574922512.json")
TEXDIR = os.path.join(ASSETS, BUNDLE, "Texture2D")
OUT = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu"

# 备查库里已经切好、直接拷的那张（不在本图集里）
COPY_ONLY = {
    "40k_OfferBadge.png": "d:/4/Unity/MyGame/Assets/CardPresentation/Art/原版/0_mainmenu/40k_OfferBadge.png",
}

# 要从图集切的（原版 sprite 名 → 落盘文件名；空格换下划线 = `CardArt.MenuUi` 的命名约定）
WANT = {
    "WF_Special offer_Bck_tier1": "WF_Special_offer_Bck_tier1.png",
    "WF_Special offer_Bck_tier2": "WF_Special_offer_Bck_tier2.png",
    "WF_Special offer_Bck_tier3": "WF_Special_offer_Bck_tier3.png",
    "40k_shop_popup_info_bg": "40k_shop_popup_info_bg.png",
}

USAGE = """用法（默认不写盘）：
  PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe \\
      d:/4/Unity/工具/import_offer_backgrounds.py --apply
  -h / --help   只打印本用法并退出（退出码 0）

做的事：从 `Offers Atlas` 按 `SpriteAtlas.m_RenderDataMap` 切 4 张底图 +
从备查库拷 `40k_OfferBadge.png`，落到 `Resources/Art/ui_menu/`（重跑幂等）。
"""


def find_atlas_png():
    """图集贴图 = Texture2D 目录下名字含 `Offers Atlas` 的那张（不写死文件名里的哈希）。"""
    hits = [f for f in os.listdir(TEXDIR) if "Offers Atlas" in f and f.lower().endswith(".png")]
    if not hits:
        raise SystemExit("找不到图集贴图（%s 下名字含 `Offers Atlas` 的 png）" % TEXDIR)
    if len(hits) > 1:
        raise SystemExit("图集贴图匹配到多张，请先确认：%s" % hits)
    return os.path.join(TEXDIR, hits[0])


def write_if_changed(src_bytes, dst):
    """按字节比对再写（重跑幂等）。返回 'new' / 'same' / 'diff'。"""
    if os.path.isfile(dst):
        if io.open(dst, "rb").read() == src_bytes:
            return "same"
        io.open(dst, "wb").write(src_bytes)
        return "diff"
    io.open(dst, "wb").write(src_bytes)
    return "new"


def main(apply):
    from PIL import Image  # 只有真跑才 import（--help 那条路不依赖 PIL）

    atlas = json.load(io.open(ATLAS_JSON, encoding="utf-8"))
    names = atlas["m_PackedSpriteNamesToIndex"]
    renders = atlas["m_RenderDataMap"]
    if len(names) != len(renders):
        raise SystemExit("图集名表(%d)与 RenderDataMap(%d) 长度不等 —— 口径变了，先查清再跑"
                         % (len(names), len(renders)))
    rects = {}
    for nm, entry in zip(names, renders):
        r = entry[1]["textureRect"]          # Unity 口径：x,y 是**左下**原点
        rects[nm] = (int(r["x"]), int(r["y"]), int(r["width"]), int(r["height"]))

    png = find_atlas_png()
    im = Image.open(png).convert("RGBA")
    W, H = im.size
    print("图集贴图 :", os.path.basename(png), "%dx%d" % (W, H))
    print("输出目录 :", OUT)

    if not apply:
        print("[dry] 只报告不写盘：")
        for src, dst in WANT.items():
            if src not in rects:
                print("  ! %s 不在图集里" % src)
                continue
            x, y, w, h = rects[src]
            print("  %-28s -> %-32s rect=(%d,%d,%d,%d)" % (src, dst, x, y, w, h))
        for dst, src in COPY_ONLY.items():
            print("  %-28s -> %-32s (copy %s)" % ("(备查库)", dst, src))
        print("\n要真写盘请加 --apply")
        return 0

    os.makedirs(OUT, exist_ok=True)
    counts = {"new": 0, "same": 0, "diff": 0}
    for src, dst_name in WANT.items():
        if src not in rects:
            print("  ! 跳过（图集里没有）:", src)
            continue
        x, y, w, h = rects[src]
        top = H - y - h                        # 换成 PIL 的左上原点
        crop = im.crop((x, top, x + w, top + h))
        tmp = os.path.join(OUT, "_tmp_" + dst_name)
        crop.save(tmp, "PNG")
        st = write_if_changed(io.open(tmp, "rb").read(), os.path.join(OUT, dst_name))
        os.remove(tmp)
        counts[st] += 1
        print("  %-28s %-6s %dx%d -> %s" % (src, st, w, h, dst_name))

    for dst_name, src in COPY_ONLY.items():
        if not os.path.isfile(src):
            print("  ! 备查库里没有（跳过）:", src)
            continue
        st = write_if_changed(io.open(src, "rb").read(), os.path.join(OUT, dst_name))
        counts[st] += 1
        print("  %-28s %-6s -> %s" % ("(备查库)", st, dst_name))

    print("\n完成：new %(new)d / diff %(diff)d / same %(same)d" % counts)
    print("⚠️ `.meta` 由 Unity 生成；首次导入后复核 `alphaIsTransparency`（既有菜单图那批是 0）。")
    return 0


if __name__ == "__main__":
    argv = sys.argv[1:]
    if any(a in ("-h", "--help") for a in argv):
        try:
            sys.stdout.reconfigure(encoding="utf-8")
        except Exception:
            pass
        print(USAGE)
        sys.exit(0)
    if "--apply" not in argv:
        try:
            sys.stdout.reconfigure(encoding="utf-8")
        except Exception:
            pass
        print(USAGE)
        print("（本次未带 --apply ⇒ 什么都没做）")
        sys.exit(0)
    sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main(True))
