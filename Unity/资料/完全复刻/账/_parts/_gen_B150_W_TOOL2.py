# -*- coding: utf-8 -*-
"""B150_W_TOOL2 生成器（2026-11-06）。

把 `判据/AUD02_裁定_卡组编辑.tsv` 里 53 条「我们落点列没有 `文件:行`」的 ❌ 行，
逐条补上**真实落点**（Enclosing method 声明行 + 现读语句行），产 `账/_parts/B150_W_TOOL2.tsv`。

⛔ 只读 AUD02 + DeckRuntime.cs；只写 B150_W_TOOL2.tsv（⛔ 不动账主表）。
⛔ 裸 split("\t")，不用 csv 模块（消费方 merge_ledger_parts.py 就是裸 split）。
"""
import io
import re
import sys

AUD = r"d:/4/Unity/资料/完全复刻/判据/AUD02_裁定_卡组编辑.tsv"
SRC = r"d:/4/Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B150_W_TOOL2.tsv"

HDR = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
       "我们的落点", "差异", "等级", "轮次", "备注"]

# ---- 落点表：(匹配谓词, 语句行, 声明行, 方法名, 状态, 差异/现状一句话) ----
# 说明：语句行 = 现读那一句；声明行 = 其所在方法的 `void Xxx(...)` 声明行（账闸只认声明行）。
LANDINGS = [
    (lambda p, f: p.endswith("Content Area/Header/Filters") and f == "m_Type",
     1591, 1539, "BuildHeader", "A",
     "一致：`_hdrFltBtn = NineSlice(…, border 10,10,10,10)`（2026-10-31 `B108_A3`）"),
    (lambda p, f: p.endswith("Header/Filters/Generic Simplified UI Button_updated/Button Text") and f == "m_VerticalAlignment",
     1674, 1539, "BuildHeader", "A",
     "一致：`MenuDraw.SetVAlign(hdrClearTx, Label.VAlign.Capline, …)`（2026-10-31 `B108_A3`）"),
    (lambda p, f: p.endswith("Header/Close/Button Text") and f == "m_VerticalAlignment",
     1586, 1539, "BuildHeader", "A",
     "一致：`MenuDraw.SetVAlign(hdrBackTx, Label.VAlign.Capline, …)`（2026-10-31 `B108_A3`）"),
    (lambda p, f: p.endswith("/Background Slot Free") and f == "m_Type",
     9874, 9862, "BuildRowExtras", "A",
     "一致：`ImageQuad.CreateTiled(…, SlotFreeTileW, SlotFreeTileH, …)`（原版 Tiled 九宫(15,14,15,12)；2026-10-31 `B108_A3` 补建）"),
    (lambda p, f: p.endswith("/Slot Free Text") and f == "m_VerticalAlignment",
     9942, 9929, "ApplyRowExtras", "A",
     "一致：`tx.SetVAlign(warlord ? Capline(8192) : Middle(512))` —— 督军族 8192 / 防御卡族 512（2026-10-31 `B108_A3`）"),
    (lambda p, f: p.endswith("/Cost Image/Cost") and f == "m_VerticalAlignment",
     10034, 10017, "ApplyRowStyles", "A",
     "一致：`cost.SetVAlign(Label.VAlign.Capline)`（2026-11-04 `B141_W26`）"),
    (lambda p, f: p.endswith("/Text fill/Card Name") and f == "m_VerticalAlignment",
     10030, 10017, "ApplyRowStyles", "A",
     "一致：`nm.SetVAlign(heroRow ? Middle : Capline)` —— 卡行/防御卡行 = Capline（2026-11-04 `B141_W26`）"),
    (lambda p, f: p.endswith("/Text fill/Count") and f == "m_VerticalAlignment",
     10039, 10017, "ApplyRowStyles", "A",
     "一致：`cnt.SetVAlign(heroRow ? Middle : Midline)` —— 卡行/防御卡行 = Midline（2026-11-04 `B141_W26`）"),
    (lambda p, f: p.endswith("/Card Cost") and f == "m_VerticalAlignment",
     2395, 2377, "BuildCurve", "A",
     "一致：`lb.SetVAlign(Label.VAlign.Midline, U(CurveRowH))`（2026-10-31 `B108_A3`）"),
    (lambda p, f: p.endswith("/Cards in deck") and f == "m_VerticalAlignment",
     2404, 2377, "BuildCurve", "A",
     "一致：`num.SetVAlign(Label.VAlign.Midline, U(CurveRowH))`（2026-10-31 `B108_A3`）"),
    (lambda p, f: p.endswith("/Slider/Background") and f == "m_Type",
     2396, 2377, "BuildCurve", "A",
     "一致：`BuildCurveBg` → `MenuDraw.Nine(…, border 3,0,15,0, …)`（2026-10-31 `B108_A3`）"),
    (lambda p, f: p.endswith("/Slider/Fill") and f == "m_Type",
     2397, 2377, "BuildCurve", "A",
     "一致：`BuildCurveFill` → `MenuDraw.Nine(…, border 3,0,14,0, …)`（2026-10-31 `B108_A3`）"),
    (lambda p, f: p.endswith("Footer/Done/Button Text") and f == "m_VerticalAlignment",
     2500, 2473, "BuildFooter", "A",
     "一致：`MenuDraw.SetVAlign(footDoneTx, Label.VAlign.Capline, …)`（2026-10-31 `B108_A3`）"),
    (lambda p, f: p.endswith("Card Filters/Scroll View") and f == "m_Type",
     2532, 2515, "BuildFilters", "C",
     "仍缺：原版 Scroll View 自带的那层 `Background`(32×32 九宫10, Sliced) 我们**没有建**；"
     "现只画父层 `_fltPanelBg`(40k_main_tab_background, Simple)"),
    (lambda p, f: p.endswith("Rarity FIlter/Content/Toggle/Label") and f == "m_VerticalAlignment",
     7647, 7528, "RefreshFilterCells", "A",
     "一致：`ApplyFilterLabelVAlign(lb, c, lr)` ⇒ 稀有度族 V=Bottom(1024)（2026-10-31 `B108_A3`）"),
    (lambda p, f: p.endswith("Type Filter/Content/Toggle/Label") and f == "m_VerticalAlignment",
     7647, 7528, "RefreshFilterCells", "A",
     "一致：`ApplyFilterLabelVAlign(lb, c, lr)` ⇒ 类型族 V=Bottom(1024)（2026-10-31 `B108_A3`）"),
]

MISSING = [("Background Slot Free", "m_Type"), ("Slot Free Text", "m_VerticalAlignment")]


def clean_method(p, f):
    """方法列：整条节点路径（去 prefab 根）+ 字段。` (N)` 换成 ` #N`，避免「一格多方法」误判。"""
    q = p
    if q.startswith("Deck Editing Menu/"):
        q = q[len("Deck Editing Menu/"):]
    q = re.sub(r"\s+\((\d+)\)", r" #\1", q)
    return q + "(" + f + ")"


def main():
    raw = io.open(AUD, "rb").read().decode("utf-8")
    lines = raw.split("\n")
    rows = [(i + 1, l.split("\t")) for i, l in enumerate(lines) if l.strip()]
    hdr = rows[0][1]
    assert hdr[3] == "字段" and hdr[5] == "我们落点(文件:行)" and hdr[9] == "裁定", hdr

    picked = []
    for ln, r in rows[1:]:
        if len(r) != 11:
            continue
        if r[9] != "❌":
            continue
        if r[3] not in ("m_VerticalAlignment", "m_Type"):
            continue
        if re.search(r":\d", r[5]):
            continue
        picked.append((ln, r))
    assert len(picked) == 53, "期望 53 条，实得 %d" % len(picked)

    out, missing = [], []
    for ln, r in picked:
        path, field, orig = r[1], r[3], r[4]
        hit = None
        for pred, stmt, decl, meth, st, note in LANDINGS:
            if pred(path, field):
                hit = (stmt, decl, meth, st, note)
                break
        if hit is None:
            missing.append((ln, path, field, orig))
            continue
        stmt, decl, meth, st, note = hit
        landing = "Deck/DeckRuntime.cs:%d（%s）· Deck/DeckRuntime.cs:%d" % (decl, meth, stmt)
        key = "原版 `%s` = **%s**" % (path.rsplit("/", 1)[-1] if "/" in path else path, orig)
        out.append([
            "卡组编辑",
            "DeckEditingWindow",
            clean_method(path, field),
            "判据/AUD02_裁定_卡组编辑.tsv:%d" % ln,
            key + "（prefab `Deck Editing Menu` 序列化字段；原裁定 ❌）",
            st,
            landing,
            note,
            "P1",
            "B150_W_TOOL2",
            "2026-11-06 现读核销；落点 = 语句 :%d / 所在方法 :%d。⛔ 本行只补落点，未改代码" % (stmt, decl),
        ])

    # 53 条**全部**找到了落点（2026-11-06 现读：原以为「节点未建」的 4 条其实 2026-10-31 `B108_A3` 已补建）
    assert len(missing) == 0, "未匹配：%r" % (missing,)
    assert len(out) == 53, len(out)

    with io.open(OUT, "wb") as f:
        body = "\t".join(HDR) + "\n" + "\n".join("\t".join(x) for x in out) + "\n"
        f.write(body.encode("utf-8"))

    # ---- 读→写→再读 逐列往返比对 ----
    back = io.open(OUT, "rb").read().decode("utf-8")
    assert "\r" not in back, "产出了 CRLF/裸 CR"
    bl = back.split("\n")
    assert bl[-1] == "", "结尾应有换行"
    bl = bl[:-1]
    assert bl[0].split("\t") == HDR
    assert len(bl) == 54, len(bl)
    for a, b in zip(out, bl[1:]):
        assert a == b.split("\t"), ("往返不一致", a, b.split("\t"))
    for b in bl[1:]:
        assert len(b.split("\t")) == 11, ("列数不是 11", len(b.split("\t")))
    print("OK rows=%d cols=11 LF-only" % (len(out)))


if __name__ == "__main__":
    sys.exit(main())
