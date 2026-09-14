# -*- coding: utf-8 -*-
"""把原版那个 TMP sprite asset `Warpforge Trait TextSprites` 的**字形表**dump 成 JSON。

**为什么要它**：卡面效果文字里那些图标（`⟦拳⟧` / `⟦枪⟧` / `⟦太阳⟧` …）在原版是
**TMP 的行内 sprite**（文本里写 `<sprite name=Atlas_trait_icon_Melee>`），
字号、缩放、基线这些**参数在读解包资源时就能抄到**，不必照截图试。

出处（2026-09-15 子代理查证 + 本脚本复核）：
  `d:/2/新解包资源/assets_full/bundle_fonts_assets_all/MonoBehaviour/Warpforge Trait TextSprites.json`
  · `m_SpriteCharacterTable` 87 条（78 个唯一名：73 张 `Atlas_trait_icon_*` + 5 张 `Atlas_SpiritStone_*`）
  · `m_GlyphTable` 96 条（`m_Metrics{m_Width,m_Height,m_HorizontalBearingX,m_HorizontalBearingY,
    m_HorizontalAdvance}` / `m_GlyphRect{x,y,w,h}` / `m_Scale` / `sprite`）
  · `spriteSheet` 指向 `40k Trait icon atlas`（1024×1024，PathID 5893885796341565625）
  · `m_Material` = `TextMeshPro/Sprite`
  · ⚠️ `m_FaceInfo` **全 0**（原版就是这么存的），所以字号照卡面 `DescTextUnit` 的 `m_fontSize: 23.55`

产物：`Unity/数据/游戏数据/trait_textsprites.json`
用法：`PYTHONIOENCODING=utf-8 python 工具/dump_trait_textsprites.py`
"""
import json
import os
import sys

sys.stdout.reconfigure(encoding="utf-8")

SRC = "d:/2/新解包资源/assets_full/bundle_fonts_assets_all/MonoBehaviour/Warpforge Trait TextSprites.json"
ATLAS = "d:/2/新解包资源/assets_full/bundle_atlasindividual_assets_40ktraiticonatlas/Texture2D/40k Trait icon atlas.png"
OUT = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                   "数据/游戏数据/trait_textsprites.json")


def main():
    d = json.load(open(SRC, encoding="utf-8"))
    chars = d["m_SpriteCharacterTable"]
    glyphs = d["m_GlyphTable"]

    rows = []
    for g in glyphs:
        m, r = g["m_Metrics"], g["m_GlyphRect"]
        rows.append({
            "index": g["m_Index"],
            "w": m["m_Width"], "h": m["m_Height"],
            "bx": m["m_HorizontalBearingX"], "by": m["m_HorizontalBearingY"],
            "adv": m["m_HorizontalAdvance"],
            "rect": [r["m_X"], r["m_Y"], r["m_Width"], r["m_Height"]],
            "scale": g["m_Scale"],
            "atlas": g.get("m_AtlasIndex", 0),
        })

    # 一个 glyph 可能被多个字符引用（`pack`/`ferocity`/… 重名），两张表都留着，按名字查
    ch = [{"name": c["m_Name"], "glyph": c["m_GlyphIndex"], "scale": c["m_Scale"]} for c in chars]

    # 图集纹理尺寸（PNG 头里直接读，别写死）
    b = open(ATLAS, "rb").read(24)
    w, h = int.from_bytes(b[16:20], "big"), int.from_bytes(b[20:24], "big")

    out = {
        "note": "原版 TMP sprite asset `Warpforge Trait TextSprites` 的字形表（工具/dump_trait_textsprites.py 生成）。"
                "卡面效果文字里的图标就是它 —— 照抄这张表 = 图标的大小/基线与原版一致，不用试。",
        "source": SRC,
        "atlas": {"png": ATLAS, "w": w, "h": h,
                  "spritePixelsToUnits": 100.0, "spritePivot": [0.5, 0.5]},
        "material": "TextMeshPro/Sprite",
        "face": {"pointSize": d["m_FaceInfo"]["m_PointSize"], "scale": d["m_FaceInfo"]["m_Scale"]},
        "characters": ch,
        "glyphs": rows,
    }
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)

    scales = sorted({r["scale"] for r in rows})
    adv = sorted({round(r["adv"], 2) for r in rows})
    by = sorted({r["by"] for r in rows})
    print(f"图集 {w}×{h}；字符 {len(ch)} 条 / 字形 {len(rows)} 条")
    print(f"  m_Scale 取值 {scales}")
    print(f"  bearingY 取值 {by}；advance 取值 {adv}")
    print(f"  宽高全是 80：{all(r['w'] == 80 and r['h'] == 80 for r in rows)}")
    print(f"  FaceInfo pointSize={out['face']['pointSize']} scale={out['face']['scale']}")
    print(f"落盘 {OUT}")


if __name__ == "__main__":
    main()
