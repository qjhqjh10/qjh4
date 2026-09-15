# -*- coding: utf-8 -*-
"""PnP 成品卡图 ↔ 我们的卡表 / 立绘 —— 逐张对账（2026-09-15）

**用户的问题**：`D:/2/Warpforge部队卡片/` 里有多少张「完整卡牌」？它们和我们的卡
一一对得上吗（插图 / 效果文本 / 稀有度 / 卡框 / 数值）？

**三条独立判据**（各自能单独发现一类错）：
  ① 卡名 ↔ 文件名   —— PnP 目录里的文件，我们的卡表里有没有这张卡（反之亦然）
  ② 字段逐项比对     —— 费用/近战/远程/护甲/生命/兵种(subtype)/类型/稀有度/效果文本
  ③ 立绘一一对应     —— `Resources/Art/cards/art_<卡名>.png` 是不是**这一张卡**的插图
                        （⚠️ 文件名按**卡名**生成 ⇒ 跨阵营同名卡会互相覆盖，这是已知风险）

⚠️ 两边都**不是**天然权威：PnP 是子代理逐张看图抄的（`资料/卡表核对_卡图提取/`），
我们的卡表是 OCR + 逐张核过的。**本脚本只报差异，不下结论** —— 判谁对由人看。

用法：
    PYTHONIOENCODING=utf-8 python Unity/工具/check_pnp_cards.py
输出：
    d:/4/_tmp_view/pnpcheck/report.md     人看的对账报告
    d:/4/_tmp_view/pnpcheck/mismatch.tsv  逐条差异（机器可读）
"""
import io, os, re, sys, json, difflib, collections

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

ROOT = "d:/4"
PNP_ROOT = "d:/2/Warpforge部队卡片"
MERGED = os.path.join(ROOT, "Unity/资料/卡表核对_卡图提取/_合并总表.md")
ENGINE = os.path.join(ROOT, "Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json")
ART = os.path.join(ROOT, "Unity/MyGame/Assets/CardPresentation/Resources/Art/cards")
OUT = os.path.join(ROOT, "_tmp_view/pnpcheck")

# PnP 目录名 → 引擎 faction（两套命名，只有这张表能对齐；加阵营时**只改这里**）
FAC_OF_DIR = {
    "Aeldari": "SaimHann", "Astra Militarum": "AstraMilitarum", "Chaos": "BlackLegion",
    "Dark Angels": "DarkAngels", "Emperor_s Children": "EmperorsChildren",
    "Genestealer Cult": "Genestealers", "Necron": "Sautekh", "Orks": "Goff",
    "Sorotitas": "Sororitas", "Space Wolves": "SpaceWolves", "Tau": "TauEmpire",
    "Tyranid": "Leviathan", "Ultramarines": "Ultramarines",
}
# PnP 的「类型」列 → 引擎 type
TYPE_OF = {"部队": "unit", "督军": "hero", "防御": "defence", "计策": "tactic",
           "天赋": "tactic", "药剂": "tactic"}


def slug(s):
    """和 `工具/import_original_art.py` 的 slug() **必须一致** —— 立绘文件名是它生成的。"""
    s = s.lower()
    s = re.sub(r"[^a-z0-9]+", "_", s)
    return s.strip("_")


def norm_name(s):
    return re.sub(r"[^a-z0-9]", "", (s or "").lower())


def load_merged():
    rows, seen = [], set()
    for line in io.open(MERGED, encoding="utf-8"):
        if not line.startswith("|"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) != 12 or cells[0] in ("文件名", "---"):
            continue
        if cells[0] in seen:            # 同一文件出现两行 = 表格本身有问题
            print("⚠️ 合并总表里同一文件名出现两次:", cells[0])
        seen.add(cells[0])
        rows.append(dict(zip(
            ["file", "name", "cost", "melee", "ranged", "armour", "health",
             "subtype", "type", "rarity", "desc", "batch"], cells)))
    return rows


def load_pnp_files():
    """PnP 目录里的真实文件 → (相对路径, 阵营目录, 分类目录)"""
    out = {}
    for d in sorted(os.listdir(PNP_ROOT)):
        p = os.path.join(PNP_ROOT, d)
        if not os.path.isdir(p) or d not in FAC_OF_DIR:
            continue
        for cat in sorted(os.listdir(p)):
            cp = os.path.join(p, cat)
            if not os.path.isdir(cp) or "PDF" in cat:
                continue
            for f in sorted(os.listdir(cp)):
                if f.lower().endswith(".png"):
                    out[f] = (f"{d}/{cat}/{f}", d, cat)
    return out


def num(x):
    x = (x or "").strip()
    if x in ("", "—", "-", "---"):
        return None
    m = re.match(r"^-?\d+$", x)
    return int(m.group()) if m else x


DESC_ICON = re.compile(r"【[^】]*】")


def norm_desc(s):
    s = DESC_ICON.sub(" ", s or "")
    s = re.sub(r"\[[^\]]*\]", " ", s)
    s = s.lower()
    s = re.sub(r"[\s\.,;:!?·。，；：！？、'\"“”‘’()（）\-–—/]+", "", s)
    return s


def main():
    os.makedirs(OUT, exist_ok=True)
    merged = load_merged()
    pnp = load_pnp_files()
    eng = json.load(io.open(ENGINE, encoding="utf-8"))["cards"]

    # ── 引擎侧索引 ───────────────────────────────────────────────
    by_fac_name = {}
    for c in eng:
        by_fac_name.setdefault((c["faction"], norm_name(c["name"])), []).append(c)
    name_count = collections.Counter(norm_name(c["name"]) for c in eng)
    dup_names = {n for n, k in name_count.items() if k > 1}

    # ── ① 文件 ↔ 表 行 ──────────────────────────────────────────
    m_files = set(r["file"] for r in merged)
    only_file = sorted(set(pnp) - m_files)      # 有图、表里没抄
    only_row = sorted(m_files - set(pnp))       # 表里有行、图上没有

    # ── ② 逐张比对 ──────────────────────────────────────────────
    mismatch, unmatched, ok, note_rows = [], [], 0, []
    for r in merged:
        d = pnp.get(r["file"])
        fac = FAC_OF_DIR.get(d[1]) if d else None
        if not fac:
            unmatched.append((r["file"], r["name"], "PnP 目录里没有这个文件"))
            continue
        key = (fac, norm_name(r["name"]))
        cand = by_fac_name.get(key, [])
        if not cand:                            # 名字对不上 → 模糊兜底（只报，不自动认）
            allnames = [c for c in eng if c["faction"] == fac]
            close = difflib.get_close_matches(r["name"], [c["name"] for c in allnames], 1, 0.86)
            unmatched.append((r["file"], r["name"],
                              f"本阵营没有同名卡（最接近：{close[0] if close else '—'}）"))
            continue
        c = cand[0]
        diffs, notes = [], []
        # ⚠️ 护甲在**关键词**里（`"Armour 2"`），不在独立字段上 —— 引擎表里就没有 `armour` 键。
        #    判据与 `KeywordTable.Armour` 同源，这里只取「Armour N」那一条。
        eng_armour = None
        for k in c.get("keywords", []):
            mm = re.match(r"^Armour\s*(\d+)$", k)
            if mm:
                eng_armour = int(mm.group(1))
        # 卡图上「没有这一栏」（战术/防御卡不印三围与兵种）时，和引擎的 0 / 缺省**不是差异**。
        # 判据：这一栏**卡图上印了没有**（印了才比），不是「引擎里是什么」。
        ptype = TYPE_OF.get(r["type"])
        statless = ptype in ("tactic", "defence")
        pairs = [
            ("费用", num(r["cost"]), c["cost"]),
            ("近战", num(r["melee"]), c["attack"]),
            ("远程", num(r["ranged"]), c["ranged"]),
            ("护甲", num(r["armour"]), eng_armour),
            ("生命", num(r["health"]), c["health"]),
            ("类型", ptype, c["type"]),
            ("稀有度", None if r["rarity"] in ("—", "---", "") else r["rarity"], c.get("rarity")),
        ]
        for label, pv, ev in pairs:
            if pv is None and statless:               # 卡图不印这一栏 ⇒ 不比
                continue
            if pv != ev:
                diffs.append(f"{label}: 卡图={pv} / 引擎={ev}")
        psub = None if r["subtype"] in ("—", "---", "") else r["subtype"]
        if psub is not None and psub != c.get("subtype"):
            diffs.append(f"兵种: 卡图={psub} / 引擎={c.get('subtype')}")
        if ptype == "hero" and c["cost"] == 0 and num(r["cost"]):
            # 督军的「费用」卡图印的是**编队费用**，引擎里督军不上场所以是 0 —— 口径差，不是错
            notes.append(f"督军费用口径（卡图 {num(r['cost'])} / 引擎 0）")
            diffs = [d for d in diffs if not d.startswith("费用:")]
        # 效果文本：剥离图标记号后比相似度（阈值只用来**筛出要人看**的，不是判据）
        a, b = norm_desc(r["desc"]), norm_desc(c.get("desc"))
        ratio = difflib.SequenceMatcher(None, a, b).ratio()
        if ratio < 0.90:
            diffs.append(f"效果文本相似度 {ratio:.2f}")
        if diffs:
            mismatch.append((r["file"], r["name"], c["id"], "; ".join(diffs)))
        else:
            ok += 1
        if notes:
            note_rows.append((r["file"], r["name"], c["id"], "; ".join(notes)))

    # ── ③ 立绘一一对应 ──────────────────────────────────────────
    art_files = set(f for f in os.listdir(ART) if f.endswith(".png"))
    no_art, dup_art = [], []
    art_of = {}
    for c in eng:
        want = f"art_{slug(c['name'])}.png"
        art_of.setdefault(want, []).append(c)
        if want not in art_files:
            no_art.append((c["id"], c["name"], c["faction"], want))
    for want, cs in art_of.items():
        if len(cs) > 1:                          # 同一个立绘文件被多张卡共用 ⇒ 至少一张错
            dup_art.append((want, [(c["id"], c["faction"]) for c in cs]))

    # ── 报告 ────────────────────────────────────────────────────
    L = []
    L.append("# PnP 成品卡图 ↔ 我们的卡：逐张对账\n")
    L.append(f"- PnP 卡图文件 **{len(pnp)}** 张 · 合并总表 **{len(merged)}** 行 · 引擎卡表 **{len(eng)}** 张")
    L.append(f"- 逐项全对 **{ok}** 张 · 有差异 **{len(mismatch)}** 张 · 表里有行但配不上 **{len(unmatched)}** 张")
    L.append(f"- 只有图、表里没抄：**{len(only_file)}** · 只有行、图上没有：**{len(only_row)}**")
    L.append(f"- 口径差（**不是错**，单独列）：督军费用 **{len(note_rows)}** 张")
    L.append("")
    if note_rows:
        L.append("## 口径差（卡图印的 ≠ 引擎存的，但两边说的不是一件事）\n")
        L.append("| 文件 | 卡名 | 卡 id | 说明 |")
        L.append("|---|---|---|---|")
        for f, n, cid, d in note_rows:
            L.append(f"| `{f}` | {n} | `{cid}` | {d} |")
        L.append("")
    if only_file:
        L.append("## 只有图、合并总表里没有对应行\n")
        for f in only_file:
            L.append(f"- `{f}`  →  {pnp[f][0]}")
        L.append("")
    if only_row:
        L.append("## 合并总表里有行、PnP 目录里找不到图\n")
        for f in only_row:
            L.append(f"- `{f}`")
        L.append("")
    if unmatched:
        L.append("## 表里有行、但本阵营找不到同名卡（**要人看**）\n")
        for f, n, why in unmatched:
            L.append(f"- `{f}`  {n} —— {why}")
        L.append("")
    if mismatch:
        L.append(f"## 逐项有差异（{len(mismatch)} 张）\n")
        L.append("| 文件 | 卡名 | 卡 id | 差异 |")
        L.append("|---|---|---|---|")
        for f, n, cid, d in mismatch:
            L.append(f"| `{f}` | {n} | `{cid}` | {d} |")
        L.append("")
    L.append("## 立绘一一对应\n")
    L.append(f"- 引擎里**找不到立绘文件**的卡：**{len(no_art)}** 张")
    for cid, n, fac, want in no_art:
        L.append(f"  - `{cid}` {n}（{fac}）→ 缺 `{want}`")
    L.append(f"- 🔴 **多个卡共用同一个立绘文件**（按卡名生成 ⇒ 同名跨阵营会撞）：**{len(dup_art)}** 组")
    for want, cs in dup_art:
        L.append(f"  - `{want}` ← " + " · ".join(f"`{i}`({f})" for i, f in cs))
    L.append("")
    L.append(f"- 引擎里**重名**的卡（跨阵营）：**{len(dup_names)}** 个名字")
    for n in sorted(dup_names):
        who = [f"`{c['id']}`({c['faction']})" for c in eng if norm_name(c["name"]) == n]
        L.append(f"  - {n}: " + " · ".join(who))
    io.open(os.path.join(OUT, "report.md"), "w", encoding="utf-8").write("\n".join(L))

    with io.open(os.path.join(OUT, "mismatch.tsv"), "w", encoding="utf-8") as fh:
        fh.write("文件\t卡名\t卡id\t差异\n")
        for row in mismatch:
            fh.write("\t".join(row) + "\n")

    print(f"PnP 图 {len(pnp)} · 表行 {len(merged)} · 引擎卡 {len(eng)}")
    print(f"逐项全对 {ok} · 有差异 {len(mismatch)} · 配不上 {len(unmatched)}")
    print(f"只有图没行 {len(only_file)} · 只有行没图 {len(only_row)}")
    print(f"缺立绘 {len(no_art)} 张 · 立绘被多卡共用 {len(dup_art)} 组 · 引擎重名 {len(dup_names)} 个")
    print("报告 →", os.path.join(OUT, "report.md"))


if __name__ == "__main__":
    main()
