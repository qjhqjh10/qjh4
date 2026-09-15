#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把**原版卡面数字用的字体**（`Pragati-Regular SDF`）抠成一张小图，供卡面 CPU 烤图直接用。

为什么要这一步（`资料/PnP卡图_逐张对账_0915.md` A3）：
    我们的费用/四数值一直是**自写的 5×7 点阵**（`Core/TextCanvas.cs` 的 `Glyphs`），
    而原版实测**没有任何点阵字形** —— 那几个数字是 `TextMeshProUGUI` + `Pragati-Regular SDF`。
    （`PerfectDOSVGA437.ttf` 是 **Unity 内置调试字体**，原版从没用过 —— 别再被它误导。）

做法：**直接从原版那张 SDF 图集里抠**（不找替代字体，也不要求本地有 Pragati 的 ttf）：
    · 字形矩形/度量来自 `bundle_fonts_assets_all/MonoBehaviour/Pragati-Regular SDF.json`
      的 `m_GlyphTable` / `m_CharacterTable` / `m_FaceInfo`
    · 像素来自同包的 `Texture2D/Pragati-Regular SDF Atlas.png`（**alpha 通道是距离场**）
    · 按 TMP 的算法把距离场阈值化成覆盖率，再叠一圈黑描边（原版材质 `_OutlineWidth`）

产出（**运行时只读第 3 份**）：
  1. `d:/4/_tmp_view/pragati_digits/pragati_digits.png` + `.json` + `.txt` —— **给人看的中间产物**
  2. `Assets/CardPresentation/Core/PragatiDigits.Data.cs` —— **运行时要的**（base64 内嵌，见 `Core/PragatiDigits.cs`）

⚠️ **中间产物不要放进 `Resources/`** —— 那是**全量进包**的，而代码根本不读这几份，
   等于白背三份死资产（踩过一次，已挪到 `_tmp_view/`）。

用法：PYTHONIOENCODING=utf-8 python Unity/工具/gen_pragati_digits.py
"""
import io, json, os, sys

import numpy as np
from PIL import Image

sys.stdout.reconfigure(encoding="utf-8")

ASSETS = "d:/2/新解包资源/assets_full/bundle_fonts_assets_all"
FONT_JSON = os.path.join(ASSETS, "MonoBehaviour/Pragati-Regular SDF.json")   # ⚠️ 名字里有空格，路径照抄
ATLAS_PNG = os.path.join(ASSETS, "Texture2D/Pragati-Regular SDF Atlas.png")
# ⚠️ 这三份（png/json/txt）是**给人看的中间产物**，**别放 `Resources/`** ——
#    那是**全量进包**的，而运行时只读 `OUT_CS` 那份（base64 内嵌，见下）。
#    放进 Resources 等于白背三份死资产（踩过）。
OUT_PNG = "d:/4/_tmp_view/pragati_digits/pragati_digits.png"
OUT_JSON = OUT_PNG.replace(".png", ".json")
OUT_META = OUT_PNG + ".meta"
OUT_TXT = OUT_PNG.replace(".png", ".txt")
OUT_CS = "d:/4/Unity/MyGame/Assets/CardPresentation/Core/PragatiDigits.Data.cs"

CS_TEMPLATE = '''// ============================================================================
//  PragatiDigits.Data.cs —— 🔴 **生成物，别手改**
//
//  由 `Unity/工具/gen_pragati_digits.py` 从**原版** `Pragati-Regular SDF` 字体资产生成：
//    · 字形矩形/度量：`bundle_fonts_assets_all/MonoBehaviour/Pragati-Regular SDF.json`
//    · 像素：同包 `Texture2D/Pragati-Regular SDF Atlas.png`（alpha = 距离场）
//  表本身由 `Core/PragatiDigits.cs` 消费。**为什么不用 Resources 里的 PNG**：
//  读像素要 `m_IsReadable=1`，而那个位在 `.meta` 里（批处理下由 Unity 生成，手改容易打架）；
//  工程里 `TextCanvas.Glyphs` 本来就是代码内嵌的表，这里沿用同一个路子。
//
//  每个像素 2 字节：`[0]` = **字面覆盖**、`[1]` = **描边覆盖**，行主序。
//  颜色是固定的（原版就是白字 + 黑描边），所以表里不存 RGB。
// ============================================================================
namespace CardPresentation
{
    public static partial class PragatiDigits
    {
        public const int SheetW = __W__;
        public const int SheetH = __H__;
        /// <summary>字形高（表内像素）。原版数值字号 0.34、字形 62.03/pointSize 95
        /// ⇒ 0.222 卡单位 ⇒ 我们 256×407 的卡面贴图上正好 28 px。</summary>
        public const int DigitH = __H_DIGIT__;

        /// <summary>一行一个字：variant（`thin`/`thick`）、字符、x、y、w、h、步进、左轴承（表内像素）</summary>
        public static readonly Glyph[] Rows =
        {
__ROWS__
        };

        /// <summary>整张表的像素（base64，每像素 2 字节，行主序）</summary>
        public const string SheetB64 =
            "__B64__";
    }
}
'''

# ── 目标尺寸（**卡面贴图里的像素**，不是屏幕像素）──────────────────────────
# `CardView.FaceW/FaceH = 256×407`，卡高 3.3313 → 122.2 px/卡单位。
#   原版数值字号 0.34、字形高 62.03 / pointSize 95 ⇒ 字高 0.34×62.03/95 = **0.222 卡单位**
#   ⇒ 目标像素高 = 0.222 × 122.2 = **27.1 px**。取 28（和原来那套 5×7 点阵的 7×4=28 一样大，
#   所以**换字体不改尺寸**，位置/对齐都不用动）。
DIGIT_H = 28
SS = 4                      # 超采样倍数：先在 4× 上算覆盖率再降采样，边缘才不锯齿
FONT_POINT = 95.0
GRADIENT_SCALE = 21.0       # = m_AtlasPadding(20) + 1，TMP 的 SDF 梯度标尺
PAD = 3                     # 每格四周留的空白（描边+抗锯齿要地方）

# 两档描边（原版材质里的 `_OutlineWidth`，见 `TmpFont.ApplyOutline` 的注释）：
#   生命/近战/远程 = Thin 0.05；护甲/费用 = Thick 0.097
# ⚠️ 这张表同时出**两套**字形，C# 侧按位子选哪一套。
OUTLINES = {"thin": 0.05, "thick": 0.097}

DIGITS = "0123456789"


def load_glyphs():
    d = json.load(io.open(FONT_JSON, encoding="utf-8"))
    by_unicode = {c["m_Unicode"]: c["m_GlyphIndex"] for c in d["m_CharacterTable"]}
    glyphs = {g["m_Index"]: g for g in d["m_GlyphTable"]}
    out = {}
    for ch in DIGITS:
        gi = by_unicode.get(ord(ch))
        if gi is None:
            raise SystemExit(f"字体资产里没有字形 {ch!r}")
        out[ch] = glyphs[gi]
    return out, d["m_FaceInfo"]


def coverage_from_sdf(a_alpha, target_scale, extra_px, softness=1.0):
    """把 SDF 的 alpha 变成覆盖率。

    SDF 的约定：`0.5` = 字边；图集里 alpha 覆盖 ±`m_AtlasPadding` 像素的距离
    ⇒ 距离（图集像素）= `(a − 0.5) × 2 × padding`；再乘 `target_scale`（降采样倍率）变到目标像素。
    `extra_px` = 往外扩多少**目标像素**（>0 就是描边）。
    """
    d_atlas = (a_alpha.astype(np.float32) - 0.5) * 2.0 * 20.0
    d_target = d_atlas * target_scale + extra_px
    return np.clip(0.5 + d_target / softness, 0.0, 1.0)


def main():
    glyphs, face = load_glyphs()
    atlas = Image.open(ATLAS_PNG).convert("RGBA")
    A = np.array(atlas)[..., 3].astype(np.float32) / 255.0     # alpha = 距离场
    H = atlas.height

    cell_w, cell_h = 0, 0
    cells = {}
    for ch, g in glyphs.items():
        r = g["m_GlyphRect"]
        m = g["m_Metrics"]
        # ⚠️ TMP 的 `m_Y` 是**从图集底部**量的，PNG 行是从顶部数的 ⇒ 翻过来
        y0 = H - r["m_Y"] - r["m_Height"]
        sub = A[y0:y0 + r["m_Height"], r["m_X"]:r["m_X"] + r["m_Width"]]
        scale = DIGIT_H / m["m_Height"]                        # 图集像素 → 目标像素
        w_t = max(1, int(round(r["m_Width"] * scale)))
        # 先在 4× 上算，再降采样 ⇒ 边缘平滑（比调 SDF 软度稳）
        big_w, big_h = w_t * SS, DIGIT_H * SS
        src = np.array(Image.fromarray((sub * 255).astype(np.uint8))
                       .resize((big_w, big_h), Image.BILINEAR)).astype(np.float32) / 255.0
        sc = (DIGIT_H / m["m_Height"]) / SS                     # 图集像素 → 4× 目标像素
        faces = {}
        for tag, ow in OUTLINES.items():
            extra = ow * GRADIENT_SCALE * (DIGIT_H / m["m_Height"]) * SS
            faces[tag] = (coverage_from_sdf(src, sc, 0.0),
                          coverage_from_sdf(src, sc, extra))
        cells[ch] = dict(w=w_t, h=DIGIT_H, adv=m["m_HorizontalAdvance"] * scale,
                         bearing_x=m["m_HorizontalBearingX"] * scale,
                         big_w=big_w, big_h=big_h, faces=faces)
        cell_w = max(cell_w, w_t + PAD * 2)
        cell_h = max(cell_h, DIGIT_H + PAD * 2)

    # ── 排版：每档一行（thin / thick），每行 10 个数字 ──────────────────────
    tags = list(OUTLINES.keys())
    W = len(DIGITS) * cell_w
    Himg = len(tags) * cell_h
    img = Image.new("RGBA", (W, Himg), (0, 0, 0, 0))
    meta = {t: {} for t in tags}
    for ti, tag in enumerate(tags):
        for di, ch in enumerate(DIGITS):
            c = cells[ch]
            face_cov, out_cov = c["faces"][tag]
            # 4× → 目标：降采样（两路一起降，保证 face 永远在 out 之内）
            stack = np.stack([face_cov, out_cov], axis=-1)
            small = np.array(Image.fromarray((stack * 255).astype(np.uint8))
                             .resize((c["w"], c["h"]), Image.LANCZOS)).astype(np.float32) / 255.0
            f, o = small[..., 0], small[..., 1]
            o = np.maximum(o, f)                                # 描边至少把字面盖住
            rgb = np.zeros((c["h"], c["w"], 3), np.float32)     # 字面白、描边黑
            rgb[..., :] = np.repeat(f[..., None], 3, axis=2)
            rgba = np.concatenate([rgb, o[..., None]], axis=2)
            tile = Image.fromarray((rgba * 255).astype(np.uint8), "RGBA")
            x = di * cell_w + PAD
            y = ti * cell_h + PAD
            img.paste(tile, (x, y), tile)
            meta[tag][ch] = dict(x=x, y=y, w=c["w"], h=c["h"],
                                 adv=c["adv"], bearing_x=c["bearing_x"])

    os.makedirs(os.path.dirname(OUT_PNG), exist_ok=True)
    img.save(OUT_PNG)
    json.dump(dict(rows=meta, cell=[cell_w, cell_h], digit_h=DIGIT_H,
                   note="由 工具/gen_pragati_digits.py 从原版 Pragati-Regular SDF 图集生成，别手改"),
              io.open(OUT_JSON, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    # 再出一份**纯文本表**给 C# —— `JsonUtility` 解不了「字典键是数字」的结构，
    # 而这份表就是 10×2 行数字，用 `Split(',')` 读最省事（也不用引 JSON 库）。
    with io.open(OUT_TXT, "w", encoding="utf-8", newline="\n") as fh:
        fh.write("# variant,digit,x,y,w,h,adv,bearing_x   由 gen_pragati_digits.py 生成，别手改\n")
        for tag in tags:
            for ch in DIGITS:
                m = meta[tag][ch]
                fh.write("%s,%s,%d,%d,%d,%d,%.4f,%.4f\n"
                         % (tag, ch, m["x"], m["y"], m["w"], m["h"], m["adv"], m["bearing_x"]))

    # ── 给 C# 的**数据源文件** ────────────────────────────────────────────────
    # 为什么不走 `Resources` 的 PNG：读像素要 `m_IsReadable=1`，而那个位在 `.meta` 里
    # （批处理下 meta 是 Unity 自己生成的，手改容易和导入器打架）。
    # 工程里 `TextCanvas.Glyphs` 本来就是**代码内嵌的表**，这里沿用同一个路子：
    # 每个像素 2 字节（字面覆盖 / 描边覆盖），整张表 base64 成一行字符串。
    arr = np.array(img.convert("RGBA"))
    face_a = arr[..., 0]        # R = 字面覆盖
    out_a = arr[..., 3]         # A = 描边覆盖
    packed = np.stack([face_a, out_a], axis=-1).astype(np.uint8).tobytes()
    import base64
    b64 = base64.b64encode(packed).decode("ascii")
    lines = []
    for tag in tags:
        for ch in DIGITS:
            m = meta[tag][ch]
            # ⚠️ 必须写 `new Glyph { … }` —— C# 的数组初始化**不允许**裸 `{ … }` 元素
            #    （`error CS0623`，生成物第一次就是栽在这）
            lines.append(f'            new Glyph {{ variant = "{tag}", ch = \'{ch}\', '
                         f'x = {m["x"]}, y = {m["y"]}, w = {m["w"]}, h = {m["h"]}, '
                         f'adv = {m["adv"]:.4f}f, bearingX = {m["bearing_x"]:.4f}f }},')
    src = (CS_TEMPLATE
           .replace("__W__", str(img.width)).replace("__H__", str(img.height))
           .replace("__H_DIGIT__", str(DIGIT_H))
           .replace("__ROWS__", "\n".join(lines))
           .replace("__B64__", b64))
    io.open(OUT_CS, "w", encoding="utf-8", newline="\n").write(src)

    print(f"写出 {OUT_PNG}  {img.size}  · 每格 {cell_w}×{cell_h} · 字高 {DIGIT_H}px")
    print(f"      {OUT_JSON}")
    print(f"      {OUT_TXT}")
    print(f"      {OUT_CS}  （base64 {len(b64)} 字符 / {len(packed)} 字节，**生成物，别手改**）")


if __name__ == "__main__":
    main()
