#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把「配不上 PnP 的卡」列出来，并给每张**猜一个候选 PnP 文件**（只报，不自动认）。

为什么要这一步：这 32 张在「逐张并排验收」里**没被看过**（名字对不上就配不上对）。
按 `资料/PnP卡图_逐张对账_0915.md` §五 那条教训 —— **PnP 文件用的是原版的旧写法，
名字对不上时必须开图看**，不能当成「原版没有这张卡」。

用法：PYTHONIOENCODING=utf-8 python Unity/工具/list_unmatched_pnp.py
产出：`资料/待人工对_PnP/配不上清单.md`
"""
import io, json, os, sys, difflib, collections

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_cardface_montage as M       # 复用 PnP 匹配判据，**不写第二份**

ROOT = "d:/4"
OUT = os.path.join(ROOT, "Unity/资料/待人工对_PnP")


def main():
    m = M.pnp_mod
    pnp_files = m.load_pnp_files()
    merged = m.load_merged()
    cs = json.load(io.open(os.path.join(ROOT, "Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"),
                           encoding="utf-8"))["cards"]

    # PnP 侧：本阵营全部切片名（相对路径），供模糊挑候选
    by_fac = collections.defaultdict(list)
    for r in merged:
        d = pnp_files.get(r["file"])
        if not d:
            continue
        fac = m.FAC_OF_DIR.get(d[1])
        if fac:
            by_fac[fac].append((r["name"], d[0]))

    # 引擎侧：哪些卡配得上（用与拼版脚本完全相同的两轮规则）
    matched = {}
    avail = {fac: {m.norm_name(n): p for n, p in lst} for fac, lst in by_fac.items()}
    for c in cs:
        k = m.norm_name(c["name"])
        if k in avail.get(c["faction"], {}):
            matched[c["id"]] = avail[c["faction"]].pop(k)
    unmatched = [c for c in cs if c["id"] not in matched]
    for c in unmatched:                       # 第二轮：模糊兜底（与拼版脚本同规则）
        pool = avail.get(c["faction"], {})
        close = difflib.get_close_matches(m.norm_name(c["name"]), list(pool), 1, 0.80)
        if close:
            matched[c["id"]] = pool.get(close[0])

    rest = [c for c in unmatched if c["id"] not in matched]

    L = ["# 配不上 PnP 的卡（逐张并排验收里**没看过**的那些）\n",
         f"\n共 **{len(rest)} 张**。判据与生成脚本：`Unity/工具/list_unmatched_pnp.py`。\n",
         "\n> ⚠️ **「配不上」≠「PnP 里没有这张卡」**。PnP 文件名用的是**原版当年的写法**，"
         "而我们的卡名可能改过（§五 那条教训）。**每一条都要开图看**。\n",
         "\n| # | 卡 id | 阵营 | 卡名 | 类型 | 费 | 兵种 | 效果（截断） | 疑似对应的 PnP | 相似度 |",
         "\n|---|---|---|---|---|---|---|---|---|---|"]
    for i, c in enumerate(sorted(rest, key=lambda x: (x["faction"], x["id"])), 1):
        pool = by_fac.get(c["faction"], [])
        close = difflib.get_close_matches(c["name"], [n for n, _ in pool], 3, 0.45)
        cand = ""
        if close:
            p = next((pp for n, pp in pool if n == close[0]), "")
            ratio = difflib.SequenceMatcher(None, c["name"].lower(), close[0].lower()).ratio()
            cand = f"`{p}`"; ratio = f"{ratio:.2f}"
        else:
            ratio = "—"
        desc = (c.get("desc") or "").replace("\n", " ")[:44]
        L.append(f"| {i} | `{c['id']}` | {c['faction']} | {c['name']} | {c.get('type','')} | "
                 f"{c.get('cost','')} | {c.get('subtype') or '—'} | {desc} | {cand} | {ratio} |")

    L.append("\n\n## 怎么处理\n")
    L.append("\n1. 对每一张，**打开「疑似对应的 PnP」那张图**（`D:/2/Warpforge部队卡片/<路径>`），"
             "核对：卡名、费用、三围、兵种行、效果文字是不是同一张卡。\n")
    L.append("2. 确认是同一张 ⇒ 把 **`<PnP 名> → <引擎卡名>`** 加进 `工具/check_pnp_cards.py` 的别名表"
             "（照 `FAC_OF_DIR` 那张「唯一的对齐表」的思路，**别在别处再写一份**），"
             "然后重跑拼版脚本，这 32 张就进验收范围了。\n")
    L.append("3. 确认**原版真没有这张卡** ⇒ 单独记一笔（「原版无此卡」是结论，不是缺口）。\n")
    L.append("\n> 已经查到的两条（2026-09-15，见 `资料/PnP卡图_逐张对账_0915.md` §六末 A6/A7）：\n"
             "> · `EC37` → `Emperor_s Children/3部队/Warpforge_37_Varius-Lord-Kakophonist.png`"
             "（卡面名 `Varius, Lord Kakophonist`）\n"
             "> · `GOF_Special_Dose_Zodgrod_Wortsnagga_Talent` → `Orks/2天赋/Warpforge_12B_Speshul-Dose.png`"
             "（卡面名 `Speshul Dose`）\n")

    os.makedirs(OUT, exist_ok=True)
    io.open(os.path.join(OUT, "配不上清单.md"), "w", encoding="utf-8", newline="\n").write("\n".join(L))
    print(f"共 {len(rest)} 张 → {OUT}/配不上清单.md")
    for c in sorted(rest, key=lambda x: (x["faction"], x["id"])):
        pool = by_fac.get(c["faction"], [])
        close = difflib.get_close_matches(c["name"], [n for n, _ in pool], 1, 0.45)
        print("  %-40s %-16s %-34s %s" % (c["id"], c["faction"], c["name"],
                                          ("→ " + close[0]) if close else ""))


if __name__ == "__main__":
    main()
