#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把「配不上 PnP 的卡」列出来，并给每张**猜一个候选 PnP 文件**（只报，不自动认）。

为什么要这一步：这些卡在「逐张并排验收」里**没被看过**（名字对不上就配不上对）。
按 `资料/PnP卡图_逐张对账_0915.md` §五 那条教训 —— **PnP 文件用的是原版的旧写法，
名字对不上时必须开图看**，不能当成「原版没有这张卡」。

🔴 **配不上 ≠ 原版没有这张卡**：判据是「这个阵营还剩几张没被认领的图」——
剩余图数 == 配不上卡数时是**双射**，逐张开图就能钉死；
剩余图数 < 配不上卡数时，多出来的那些才可能是「原版没印」。
所以产物分两段：① 逐卡表（含**剩余池**里的模糊候选）② 每阵营的剩余未认领图全表。

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

    # PnP 侧：卡面印的名 + 相对路径。键与拼版脚本**同一口径**：`norm_name(卡面印的名)`
    by_fac = collections.defaultdict(list)
    for r in merged:
        d = pnp_files.get(r["file"])
        if not d:
            continue
        fac = m.FAC_OF_DIR.get(d[1])
        if fac:
            by_fac[fac].append((r["name"], d[0]))

    pnp_by_key = {(fac, m.norm_name(n)): p for fac, lst in by_fac.items() for n, p in lst}
    printed = {(fac, m.norm_name(n)): n for fac, lst in by_fac.items() for n, p in lst}

    # 🔴 两轮匹配**只在 `check_pnp_cards.match_two_rounds` 一处**（拼版脚本也调它）。
    #    这里以前自己又写了一遍 —— 两份迟早不一致，2026-09-16 收掉。
    _matched, rest, leftover = m.match_two_rounds(cs, pnp_by_key)

    L = ["# 配不上 PnP 的卡（逐张并排验收里**没看过**的那些）\n",
         f"\n共 **{len(rest)} 张**。判据与生成脚本：`Unity/工具/list_unmatched_pnp.py`。\n",
         "\n> ⚠️ **「配不上」≠「PnP 里没有这张卡」**。PnP 文件名用的是**原版当年的写法**，"
         "而我们的卡名可能改过（§五 那条教训）。**每一条都要开图看**。\n"]

    # ── ① 每阵营的「剩余图」——**判「原版到底有没有」的尺子** ──────────────
    # 剩余图 == 配不上卡数 ⇒ 双射，逐张开图就能钉死；剩余图 = 0 ⇒ 本阵营一张都没有了。
    un_by_fac = collections.Counter(c["faction"] for c in rest)
    L += ["\n## ① 每阵营剩余多少张没被认领的图\n",
          "\n| 阵营 | 引擎卡 | 配不上 | 剩余未认领图 | 判读 |",
          "\n|---|---|---|---|---|"]
    for fac in sorted({c["faction"] for c in cs}):
        n_eng = sum(1 for c in cs if c["faction"] == fac)
        n_un, n_left = un_by_fac.get(fac, 0), len(leftover.get(fac, {}))
        if n_un == 0:
            verdict = "✅ 全配上"
        elif n_left == n_un:
            verdict = "**双射** —— 逐张开图就能钉死，原版**有**这些卡"
        elif n_left < n_un:
            verdict = f"图不够 ⇒ 至多 {n_left} 张有图，其余 {n_un - n_left} 张可能原版没印"
        else:
            verdict = f"⚠️ 图比卡多 {n_left - n_un} 张（有图没卡）—— 要单独看"
        L.append(f"| {fac} | {n_eng} | {n_un} | {n_left} | {verdict} |")

    L += ["\n\n## ② 逐卡表（候选只从**剩余池**里挑）\n",
          "\n| # | 卡 id | 阵营 | 卡名 | 类型 | 费 | 兵种 | 效果（截断） | 疑似对应的 PnP | 相似度 |",
          "\n|---|---|---|---|---|---|---|---|---|---|"]
    for i, c in enumerate(sorted(rest, key=lambda x: (x["faction"], x["id"])), 1):
        # ⚠️ 候选**只从还没被认领的图里挑**。以前从该阵营全量池挑 ⇒ 32 格里有 17 格
        #    指向**别人家的图**（`SAU5 Diviner` 被指到 `SAU66` 的 `Divination-Menhir`），
        #    照着它开图会白开一遍。（2026-09-16 修）
        fac = c["faction"]
        pool = leftover.get(fac, {})
        close = difflib.get_close_matches(m.engine_key(c["name"]), list(pool), 1, 0.45)
        cand, ratio = "", "—"
        if close:
            pname = printed.get((fac, close[0]), close[0])
            ratio = f"{difflib.SequenceMatcher(None, c['name'].lower(), pname.lower()).ratio():.2f}"
            cand = f"`{pool[close[0]]}`"
        desc = (c.get("desc") or "").replace("\n", " ")[:44]
        L.append(f"| {i} | `{c['id']}` | {c['faction']} | {c['name']} | {c.get('type','')} | "
                 f"{c.get('cost','')} | {c.get('subtype') or '—'} | {desc} | {cand} | {ratio} |")

    # ── ③ 剩余未认领图全表 —— 剩余图少的时候，**逐张对一遍就能排除** ──────────
    L.append("\n\n## ③ 每阵营「还没被任何卡认领」的图（这就是全部候选）\n")
    for fac in sorted(leftover):
        pool = leftover[fac]
        if not pool:
            continue
        L.append(f"\n**{fac}**（{len(pool)} 张）\n\n")
        for k2 in sorted(pool):
            L.append(f"- `{pool[k2]}` —— 卡面名 `{printed.get((fac, k2), k2)}`\n")

    L.append("\n\n## 怎么处理\n")
    L.append("\n1. 先看 ① 那张表：**剩余图 == 配不上卡数**的阵营是双射，逐张开图就能钉死；"
             "**剩余图 = 0** 的阵营，配不上的那些才是「原版没印」。\n")
    L.append("2. 对每一张，**打开候选那张图**（`D:/2/Warpforge部队卡片/<路径>`），"
             "核对：卡名、费用、三围、兵种行、效果文字是不是同一张卡。"
             "⚠️ 候选只是模糊提示，**会指错**（`Stormsurge Battlesuit` 曾被指到 `Stealth Battlesuit`）。\n")
    L.append("3. 确认是同一张 ⇒ 把 **`引擎卡名` → `PnP 卡面印的名`** 加进 `工具/check_pnp_cards.py` 的 "
             "`PNP_NAME_ALIAS`（**唯一的对齐表**，别在别处再写一份），"
             "然后重跑这个脚本 + `工具/make_cardface_montage.py`，这批就进验收范围了。\n")
    L.append("4. 确认**原版真没有这张卡** ⇒ 单独记一笔（「原版无此卡」是**结论**，不是缺口）。\n")
    L.append("\n> 已经查到的两条（2026-09-15，见 `资料/PnP卡图_逐张对账_0915.md` §六末 A6/A7）：\n"
             "> · `EC37` → `Emperor_s Children/3部队/Warpforge_37_Varius-Lord-Kakophonist.png`"
             "（卡面名 `Varius, Lord Kakophonist`）\n"
             "> · `GOF_Special_Dose_Zodgrod_Wortsnagga_Talent` → `Orks/2天赋/Warpforge_12B_Speshul-Dose.png`"
             "（卡面名 `Speshul Dose`）\n")

    os.makedirs(OUT, exist_ok=True)
    io.open(os.path.join(OUT, "配不上清单.md"), "w", encoding="utf-8", newline="\n").write("\n".join(L))
    print(f"共 {len(rest)} 张 → {OUT}/配不上清单.md")
    for fac in sorted({c["faction"] for c in cs}):
        n_un = sum(1 for c in rest if c["faction"] == fac)
        n_left = len(leftover.get(fac, {}))
        if n_un or n_left:
            print("    %-18s 配不上 %-3d 剩余图 %-3d %s" %
                  (fac, n_un, n_left, "← 双射，可钉死" if n_un == n_left and n_un else ""))
    for c in sorted(rest, key=lambda x: (x["faction"], x["id"])):
        pool = leftover.get(c["faction"], {})
        close = difflib.get_close_matches(m.engine_key(c["name"]), list(pool), 1, 0.45)
        print("  %-40s %-16s %-34s %s" % (c["id"], c["faction"], c["name"],
                                          ("→ " + printed.get((c["faction"], close[0]), close[0]))
                                          if close else ""))


if __name__ == "__main__":
    main()
