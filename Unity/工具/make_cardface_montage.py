#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""卡面并排拼版 —— 「逐张并排验收」的输入（2026-09-15）

把**我们的卡面渲染**和 **PnP 成品卡图**按同一张卡配成对、缩到同高、左右并排，
按阵营切成若干页，交给子代理逐张看。

用户 2026-09-15 点名要这件事（`资料/PnP卡图_逐张对账_0915.md` §六末「还没做」）：
> **逐张并排验收**（我们的卡面 vs PnP 成品卡）—— 全池 1127 张，不是抽查。

上游：`-executeMethod CardFaceProbe.RunAll` → `_tmp_view/cardface_all/<id>.png` + `_manifest.tsv`

用法：
    PYTHONIOENCODING=utf-8 python Unity/工具/make_cardface_montage.py [每页对数]

产出：
    d:/4/_tmp_view/cardface_cmp/<阵营>_P<n>.png   拼版页（每页 `每页对数` 对，左=我方 右=PnP）
    d:/4/_tmp_view/cardface_cmp/_index.tsv        页 → 该页上的卡 id（按从左到右顺序）
    d:/4/_tmp_view/cardface_cmp/_missing.tsv      配不上 PnP / 没渲出来的卡

🔴 **PnP ↔ 引擎卡的匹配判据一律 import `check_pnp_cards`**（`load_merged` / `load_pnp_files` /
`FAC_OF_DIR` / `norm_name`）—— **绝不在这里写第二份**。「两处写同一条规则 = 迟早不一致」。
"""
import io, os, sys, importlib.util, collections, difflib

ROOT = "d:/4"
HERE = os.path.dirname(os.path.abspath(__file__))
ALL = os.path.join(ROOT, "_tmp_view/cardface_all")
OUT = os.path.join(ROOT, "_tmp_view/cardface_cmp")

# 并排时两边都缩到这个高度（px）—— 够看清阵营行/兵种行/效果文字，又不会让一页太大
ROW_H = 660                # 每张卡的渲图高度（px）
LABEL_H = 36               # 卡 id 那一行
PAGE_COLS = 2              # 每行放几对（一对 = 我们 + PnP 两张）
PER_PAGE_PAIRS = 4         # 每页放几对 ⇒ 2 列 × 2 行
GAP = 22
PAD = 16
PAGE_BG = (24, 26, 32)
# ⚠️ 页宽压在 ~2000 px 以内：实测读图端会把大图缩到 2000 宽
#    （`_tmp_view/faces_cmp.png` 2011×490 → 显示 2000×487）。再宽就等于每张卡更小，白搭。

# ── PnP 匹配判据：只从 check_pnp_cards 拿，不重写一份 ────────────────────────
# ⚠️ 这个 import 必须**在**我们碰 `sys.stdout` 之前 —— `check_pnp_cards` 模块级会干
#    `sys.stdout = io.TextIOWrapper(sys.stdout.buffer, ...)`。如果我们自己先建了一个 wrapper，
#    它就不在 `sys.__stdout__` 的保护之下，被换掉时 GC 会把**底层 buffer 一起关掉**
#    ⇒ 之后每次 print 都 `ValueError: I/O operation on closed file`（实测踩到）。
#    所以下面一律用 `reconfigure`（原地改，不产生新对象），绝不再自己 `TextIOWrapper(...)`。
spec = importlib.util.spec_from_file_location("check_pnp_cards",
                                             os.path.join(HERE, "check_pnp_cards.py"))
pnp_mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pnp_mod)

sys.stdout.reconfigure(encoding="utf-8")


def _close_1d(m, gap):
    """沿最后一维把 ≤gap px 的空隙填上（闭运算 = 先膨胀再腐蚀）。

    卡框内部有细透明缝（`Deathwing-Terminator` 在 y=192 有一道），不先填上，
    「过中线的连续段」就会被切成两截。用累积和实现，**不引 scipy**。
    """
    import numpy as np
    L = m.shape[1]
    cs = np.cumsum(m, axis=1)
    lo = np.maximum(np.arange(L) - gap, 0)
    hi = np.minimum(np.arange(L) + gap + 1, L)

    def win(c):                       # 每条线上 [lo,hi) 窗口内 1 的个数
        return c[:, hi - 1] - np.where(lo > 0, c[:, np.maximum(lo - 1, 0)], 0)

    d = win(cs) > 0                   # 膨胀
    return win(np.cumsum(d, axis=1)) == (hi - lo)     # 腐蚀


def _run_through(closed, c):
    """每条线上「过第 c 个像素」的那一段长度（`closed` 每行是一条线）"""
    import numpy as np
    L = closed.shape[1]
    left = np.where(~closed[:, :c + 1], np.arange(c + 1), -1).max(axis=1)
    right = np.where(~closed[:, c:], np.arange(c, L), L).min(axis=1)
    return left + 1, right - 1, right - left - 1


def body_rect(im, fg, gap=12, keep=0.93):
    """卡体矩形 —— **不是** alpha bbox。

    ⚠️ 实测（2026-09-15）：数值圆 / 盾牌 / 生命框 / 稀有度宝石是**伸出卡框外**的，
    拿 bbox 当卡体宽度会多出几十 px（`Deathwing-Terminator` 卡体 583 宽、bbox 622 宽）。
    卡框本身是**实心矩形**，所以取「**过画布中线的连续段**」，按参考段长 ×`keep` 判卡体行/列。

    🔴 **参考段长不能用 `max`**（第一版就是）：少数几列的段会被立绘越框拉长
    （`Aeldari/…/Warpforge_34_Fire-Dragon.png` 实测 最大 1044 / 中位 929），
    于是阈值被抬到 971，**只有 70 列过线**、卡体被切成一条窄缝。
    改成「**先丢掉短于最大值一半的段，再取中位**」—— 抗离群，又不会塌到一小块。

    矢量化：1118 张 PnP 全跑一遍 ~2 分钟（纯 Python 双重循环要十几分钟 —— 实测踩过）。
    """
    import numpy as np
    m = fg(np.array(im))
    H, W = m.shape
    cy, cx = H // 2, W // 2

    def ref(run):
        big = run[run > 0.5 * run.max()]
        return float(np.median(big)) if len(big) else float(run.max())

    _, _, hrun = _run_through(_close_1d(m, gap), cx)        # 每一行的段长
    ys = np.where(hrun >= keep * ref(hrun))[0]
    _, _, vrun = _run_through(_close_1d(m.T, gap), cy)      # 每一列的段长
    xs = np.where(vrun >= keep * ref(vrun))[0]
    return int(xs[0]), int(xs[-1]), int(ys[0]), int(ys[-1])


# 📌 我方渲图的**固定裁框** —— 相机不动、卡在原点、`orthographicSize` 是常量 ⇒ 每张都在同一像素位置。
#    ✅ 2026-09-15 实测确认：对 126 张跑「过中线连续段」检测，得到 **35 种**不同矩形 ——
#    说明**逐张检测在我方渲图上反而不可靠**（卡的暗部与背景色接近 ⇒ 段被打断，
#    再叠加 §「数值圆伸出框外」），于是这里直接固定裁框。
#    由相机参数算得：1115 / (2×1.855) = 300.54 px/单位；卡框实绘 2.1158×3.2572 ⇒ 635.7×978.8 px，
#    中心 y = 557.5 − 0.03×300.54 = 548.5 ⇒ y∈[59.1,1037.9]、x∈[32.2,667.8]。四边各留 ~12 px 余量。
OUR_RECT = (30, 45, 670, 1050)      # left, top, right, bottom


def fg_pnp(a):
    return a[..., 3] > 250


def load_font(size):
    from PIL import ImageFont
    for p in ("C:/Windows/Fonts/msyh.ttc", "C:/Windows/Fonts/arial.ttf",
              "C:/Windows/Fonts/segoeui.ttf"):
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                pass
    return ImageFont.load_default()


def main():
    from PIL import Image, ImageDraw
    per_page = int(sys.argv[1]) if len(sys.argv) > 1 else PER_PAGE_PAIRS
    os.makedirs(OUT, exist_ok=True)

    # ── 我方清单 ─────────────────────────────────────────────────
    man = os.path.join(ALL, "_manifest.tsv")
    if not os.path.exists(man):
        print("❌ 没有", man, "—— 先跑 `-executeMethod CardFaceProbe.RunAll`")
        return
    cards = []
    for line in io.open(man, encoding="utf-8").read().splitlines()[1:]:
        cid, fac, name, fn = line.split("\t")
        cards.append(dict(id=cid, faction=fac, name=name, file=fn))

    # ── PnP 侧：文件 → 卡名 → 阵营（判据全部来自 check_pnp_cards）─────
    pnp_files = pnp_mod.load_pnp_files()          # {文件名: (相对路径, 阵营目录, 分类目录)}
    merged = pnp_mod.load_merged()                # 逐张抄录表（文件名 + 卡面印的名）
    pnp_by_key, collide = {}, collections.Counter()
    for r in merged:
        d = pnp_files.get(r["file"])
        if not d:
            continue
        fac = pnp_mod.FAC_OF_DIR.get(d[1])
        if not fac:
            continue
        key = (fac, pnp_mod.norm_name(r["name"]))
        collide[key] += 1
        pnp_by_key[key] = d[0]                    # 与 check_pnp_cards 同口径：后写的覆盖

    print(f"我方渲图 {len(cards)} 张 · PnP 映射 {len(pnp_by_key)} 键 "
          f"（其中 {sum(1 for v in collide.values() if v > 1)} 个键有多个 PnP 文件同名争抢）")

    font_s = load_font(20)
    missing, pages = [], collections.defaultdict(list)

    by_fac = collections.defaultdict(list)
    for c in cards:
        by_fac[c["faction"]].append(c)

    # 🔴 **两轮匹配**（精确 → 模糊兜底）的判据**只在 `check_pnp_cards.match_two_rounds` 一处** ——
    #    这里别再写一份：别名表 / 天赋卡剥后缀 / 兜底阈值都跟着那一份走。
    #    （`工具/list_unmatched_pnp.py` 也调它，两边口径从此必定一致。）
    resolved, _unmatched, _leftover = pnp_mod.match_two_rounds(cards, pnp_by_key)

    for fac in sorted(by_fac):
        cs = by_fac[fac]

        pairs = []
        for c in cs:
            if c["id"] not in resolved:
                missing.append((c["id"], fac, c["name"], "PnP 里既没有同名的、也模糊配不上"))
                continue
            p, fuzzy = resolved[c["id"]]
            ours_path = os.path.join(ALL, c["file"])
            pnp_path = os.path.join(pnp_mod.PNP_ROOT, p)
            if not os.path.exists(pnp_path) or not os.path.exists(ours_path):
                missing.append((c["id"], fac, c["name"],
                                f"文件缺：ours={os.path.exists(ours_path)} pnp={os.path.exists(pnp_path)}"))
                continue
            try:
                o = Image.open(ours_path).convert("RGB").crop(OUR_RECT)
                q = Image.open(pnp_path).convert("RGBA")
                px0, px1, py0, py1 = body_rect(q, fg_pnp)
                # 转 RGB：圆角外的透明区变成黑底，贴到深色页面上才不会出现「半透明方块」
                q = q.crop((px0, py0, px1 + 1, py1 + 1)).convert("RGB")
            except Exception as e:
                missing.append((c["id"], fac, c["name"], f"检测卡体失败：{type(e).__name__}: {e}"))
                continue
            pairs.append((c, o, q, fuzzy, p))

        # 按每页 per_page 对切块，排成 PAGE_COLS 列的网格
        for pi in range(0, len(pairs), per_page):
            chunk = pairs[pi:pi + per_page]
            tiles = []
            for c, o, q, fuzzy, p in chunk:
                o2 = o.resize((max(1, int(round(o.width * ROW_H / o.height))), ROW_H), Image.LANCZOS)
                q2 = q.resize((max(1, int(round(q.width * ROW_H / q.height))), ROW_H), Image.LANCZOS)
                tiles.append((c, o2, q2, fuzzy))

            cellw = max(t[1].width for t in tiles) + GAP + max(t[2].width for t in tiles)
            cellh = LABEL_H + ROW_H
            rows = (len(tiles) + PAGE_COLS - 1) // PAGE_COLS
            W = PAD * 2 + PAGE_COLS * cellw + (PAGE_COLS - 1) * GAP * 3
            head = 30
            H_ = head + PAD + rows * cellh + (rows - 1) * GAP + PAD
            page = Image.new("RGB", (W, H_), PAGE_BG)
            d = ImageDraw.Draw(page)
            # 页面抬头：把版式说清楚，免得看的人自己猜哪张是我们、哪张是原版
            d.text((PAD, 7), "每格 = 一对：左=我们的卡面 · 右=PnP 成品卡 · 同一对是同一张卡 · 张冠李戴的会标 ⚠",
                   font=font_s, fill=(200, 205, 215))
            for i, (c, o2, q2, fuzzy) in enumerate(tiles):
                r, col = divmod(i, PAGE_COLS)
                x = PAD + col * (cellw + GAP * 3)
                y = head + PAD + r * (cellh + GAP)
                lab = f"{'⚠配对存疑  ' if fuzzy else ''}{c['id']}   {c['name'][:30]}"
                d.text((x, y), lab, font=font_s, fill=(255, 140, 120) if fuzzy else (255, 214, 120))
                page.paste(o2, (x, y + LABEL_H))
                page.paste(q2, (x + o2.width + GAP, y + LABEL_H))
            fn = f"{fac}_P{pi // per_page + 1:02d}.png"
            page.save(os.path.join(OUT, fn))
            pages[fn] = [(c["id"], fuzzy) for c, _, _, fuzzy in tiles]

    # ── 索引 / 缺口 ──────────────────────────────────────────────
    with io.open(os.path.join(OUT, "_index.tsv"), "w", encoding="utf-8", newline="\n") as fh:
        fh.write("页\t该页卡 id（从左到右；带 ⚠ 的是模糊配对）\t对数\n")
        for fn in sorted(pages):
            ids = [cid + ("⚠" if fz else "") for cid, fz in pages[fn]]
            fh.write(f"{fn}\t{' '.join(ids)}\t{len(ids)}\n")
    with io.open(os.path.join(OUT, "_missing.tsv"), "w", encoding="utf-8", newline="\n") as fh:
        fh.write("卡id\t阵营\t卡名\t原因\n")
        for row in missing:
            fh.write("\t".join(row) + "\n")

    print(f"页数 {len(pages)} → {OUT}")
    print(f"配不上 / 渲不出 {len(missing)} 张 → {OUT}/_missing.tsv")
    if missing:
        for m in missing[:10]:
            print("   ", m[0], m[1], m[2][:24], "|", m[3][:60])


if __name__ == "__main__":
    main()
