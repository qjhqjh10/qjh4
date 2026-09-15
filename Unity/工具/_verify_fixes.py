#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""复验「逐张并排验收」这一批的修正 —— 把改过的卡按同一版式再拼一次，人看。

为什么单写一个：全套拼版要跑 5 分钟、278 页；这里只拼**这一轮改过的那几张**，
一眼就能确认「改对了没有」。

用法（**先跑 `-executeMethod CardFaceProbe.RunAll` 重渲**）：
    PYTHONIOENCODING=utf-8 python Unity/工具/_verify_fixes.py
产出：`d:/4/_tmp_view/_verify_fixes.png`（左=我们 右=PnP）
"""
import io, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_cardface_montage as M          # 复用卡体裁剪 / PnP 匹配判据，**不写第二份**

from PIL import Image, ImageDraw

ALL = "d:/4/_tmp_view/cardface_all"
OUT = "d:/4/_tmp_view/_verify_fixes.png"

# 这一轮改过的卡（id → 想看的那个元素）。
# ⚠️ 也可以命令行给：`python _verify_fixes.py UM12 GSC7 ...`（给了就只拼这些，说明列自动留空）
CARDS = [
    ("BL9",  "ranged=0 现在该画 0"),
    ("GSC7", "ranged=0 现在该画 0"),
    ("SW3",  "ranged=0 现在该画 0"),
    ("AM13", "关键词段与 descZh 都叫「爆破」⇒ 只印一遍"),
    ("GOF53", "英文是裸 +2 ⇒ 中文不该再有「+2 生命值」"),
    ("GOF50", "同上"),
    ("ASH_Forewarned", "「+3 护甲」应为「+3 远程攻击」"),
    ("TAU14", "句首该补出「飞行。潜行。」"),
    ("TAU42", "句首该补出「护盾。远射。」"),
    ("UM74", "Oath 3（原来印成 1）"),
]


def main():
    argv = sys.argv[1:]
    cards_spec = [(c, "") for c in argv] if argv else CARDS
    cards = {}
    for line in io.open(os.path.join(ALL, "_manifest.tsv"), encoding="utf-8").read().splitlines()[1:]:
        cid, fac, name, fn = line.split("\t")
        cards[cid] = dict(id=cid, faction=fac, name=name, file=fn)

    pnp_files = M.pnp_mod.load_pnp_files()
    merged = M.pnp_mod.load_merged()
    by_key = {}
    for r in merged:
        d = pnp_files.get(r["file"])
        if not d:
            continue
        fac = M.pnp_mod.FAC_OF_DIR.get(d[1])
        if fac:
            by_key[(fac, M.pnp_mod.norm_name(r["name"]))] = d[0]

    font = M.load_font(20)
    tiles = []
    for cid, why in cards_spec:
        c = cards.get(cid)
        if not c:
            print("⚠️ 清单里没有", cid); continue
        p = by_key.get((c["faction"], M.pnp_mod.norm_name(c["name"])))
        if not p:
            print("⚠️ 配不上 PnP：", cid, c["name"]); continue
        o = Image.open(os.path.join(ALL, c["file"])).convert("RGB").crop(M.OUR_RECT)
        q = Image.open(os.path.join(M.pnp_mod.PNP_ROOT, p)).convert("RGBA")
        x0, x1, y0, y1 = M.body_rect(q, M.fg_pnp)
        q = q.crop((x0, y0, x1 + 1, y1 + 1)).convert("RGB")
        H = M.ROW_H
        o2 = o.resize((max(1, int(o.width * H / o.height)), H), Image.LANCZOS)
        q2 = q.resize((max(1, int(q.width * H / q.height)), H), Image.LANCZOS)
        tiles.append((cid, c["name"], why, o2, q2))

    if not tiles:
        print("没有可拼的"); return
    CW = max(t[3].width + t[4].width for t in tiles) + M.GAP
    CH = 34 + M.LABEL_H + M.ROW_H + 26
    COLS, PER = 2, 4          # ⚠️ 每张图只放 4 张卡：读图端会把大图缩到 ~2000 px，
    for pi in range(0, len(tiles), PER):   #    10 张挤一张就每张都小到看不清文字
        chunk = tiles[pi:pi + PER]
        rows = (len(chunk) + COLS - 1) // COLS
        page = Image.new("RGB", (M.PAD * 2 + COLS * CW + M.GAP * 3,
                                 rows * CH + M.PAD * 2 + 26), M.PAGE_BG)
        d = ImageDraw.Draw(page)
        d.text((M.PAD, 7), "复验这一轮改过的卡：每格左=我们的卡面 · 右=PnP 成品卡", font=font,
               fill=(200, 205, 215))
        for i, (cid, name, why, o2, q2) in enumerate(chunk):
            r, col = divmod(i, COLS)
            x = M.PAD + col * (CW + M.GAP * 3)
            y = 26 + M.PAD + r * CH
            d.text((x, y), f"{cid}  {name[:26]}", font=font, fill=(255, 214, 120))
            d.text((x, y + 20), why, font=font, fill=(150, 200, 255))
            page.paste(o2, (x, y + 22 + M.LABEL_H))
            page.paste(q2, (x + o2.width + M.GAP, y + 22 + M.LABEL_H))
        fn = OUT.replace(".png", f"_{pi // PER + 1}.png")
        page.save(fn)
        print(f"  {fn}  {page.size}")
    print(f"共 {len(tiles)} 张")


if __name__ == "__main__":
    main()
