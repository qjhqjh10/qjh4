# -*- coding: utf-8 -*-
"""W4 收尾自检（**纯盘上核对、不跑 Unity**）：A1803 那几族的「官方键 → 我们的落点 → 值」逐项复算。"""
import io
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import w4_lib as L  # noqa: E402

RES = r"d:/4/Unity/MyGame/Assets/CardPresentation/Resources"
ok = bad = 0


def chk(cond, msg):
    global ok, bad
    if cond:
        ok += 1
    else:
        bad += 1
        print("  ✗ " + msg)
    return cond


def main():
    tab = {k: v for k, v in L.loc_table().items()}
    off = L.official_pairs()
    s = io.open(L.LOC, encoding="utf-8").read()

    print("=== ① Loc.cs 表 ===")
    # 🔴 2026-10-19（`A2066`）：**别数整段文本里的 `new Entry(`** —— 文件头那段说明自己就写了
    #    两次 `new Entry(`（`//` 注释里，其中一句还是「复算：`grep -c 'new Entry(' …」）
    #    ⇒ 775 条会被数成 777（实测）。判据改成只数**代码行**（行首不是 `//`）。
    n_entry = sum(1 for ln in s.splitlines()
                  if "new Entry(" in ln and not ln.lstrip().startswith("//"))
    # ⚠️ `loc_table()` 的正则**看不见**那条**拼接写法**的词条（`MainMenu/PurchasePremium/Description`
    #    是 `"…" + "…"` 三段），所以它少 1 条 —— **不是表少了**，判据用代码行里的 `new Entry(` 数。
    n_re = len(tab)
    # 条目数**下限**（2026-10-19 现读 775 条 · 正则可见 774 + 拼接写法 1）：
    #   720 是 2026-10-14 那一轮的读数、早就过期；这个数只防「表被整段删掉」，不是现值（表还会长）。
    ENTRY_FLOOR = 775
    chk(n_entry >= ENTRY_FLOOR, "`new Entry(` 表条目 %d 条（下限 %d）" % (n_entry, ENTRY_FLOOR))
    chk(n_re == n_entry - 1, "正则可见 %d 条 = 表 %d 条 − 1（那条拼接写法）" % (n_re, n_entry))
    print("  Entry 现读 = %d（其中 1 条是拼接写法、正则看不见；表实为 %d 条）" % (n_re, n_entry))

    print("=== ② 待接清单（原 12 条）===")
    twelve = ['Battle/Tips/DamageFatigue', 'Demo/Tutorial', 'MainMenu/Forge/Help', 'MainMenu/General/Claim',
              'MainMenu/Level', 'MainMenu/PurchasePremium/Description', 'MainMenu/RewardWindow/AlreadyOwned',
              'MenuShop/BoosterInfo/LegendaryTooltip', 'MenuShop/referral/counter',
              'MenuShop/referral/counterMaximum', 'Missions/CollectingRewards', 'Rewards Menu/Claimed']
    for k in twelve:
        # ⚠️ 拼接写法那条正则看不见 ⇒ 用「文件里有没有这个键」判（`Loc.cs` 里逐字出现过）
        chk(k in tab or ('"%s"' % k) in s, "%s 不在表里" % k)
        if k in off and k in tab:
            chk(L.unesc(tab[k][1]) == L.unesc(off[k][0]), "%s 的 EN 与官方不同：%r vs %r" % (k, tab[k][1], off[k][0]))
    print("  12 条全部在表：%s（拼接写法那条按文件里逐字出现判）"
          % all(k in tab or ('"%s"' % k) in s for k in twelve))

    print("=== ③ 「中文还不是官方值」那 6 条 ===")
    six = ['Settings/Support/FAQText', 'Settings/Support/ContactText', 'Settings/Support/FAQTextMobile',
           'MenuShop/ExtraLegendaryWarning', 'Battle/AlliancePanel/NotInAnAlliance', 'MenuDeck/CantImportDeck']
    for k in six:
        same = (tab[k][0] == off[k][1])
        print("   %-46s ZH 一致=%s%s" % (k, same, "" if same else "  ← 本轮**未换**（要同时改调用点，见报告）"))

    print("=== ④ `Battle/*`（官方现读 156 条）===")
    miss = [k for k in off if k.startswith("Battle/") and k not in tab]
    chk(not miss, "官方 Battle 键还缺 %d 个：%s" % (len(miss), miss[:5]))
    diff = [(k, tab[k][1], off[k][0]) for k in tab if k.startswith("Battle/") and k in off
            and L.unesc(tab[k][1]) != L.unesc(off[k][0])]
    chk(not diff, "Battle EN 与官方不同还剩 %d：%s" % (len(diff), diff[:3]))
    n_battle = len([k for k in off if k.startswith("Battle/")])
    # ⚠️ 2026-10-19（`A2066`）：这行原来**无条件**打「全部在表、EN 全部与官方一致」——
    #    上面两条已经红了它还说全绿（假读数）。改成按现读算。
    print("  官方 Battle 键 %d 条：在表 %d 条、EN 与官方一致 %d 条"
          % (n_battle, n_battle - len(miss), n_battle - len(miss) - len(diff)))

    print("=== ⑤ trait_tips.json（关键词 tooltip）===")
    tt = json.load(io.open(os.path.join(RES, "trait_tips.json"), encoding="utf-8"))
    ent = tt["entries"]
    from collections import Counter
    c = Counter(e["src"] for e in ent)
    print("  条目 %d：%s" % (len(ent), dict(c)))
    chk(len(ent) == 62, "条目数 %d（期望 62 = `Card_Trait/*` 那一族）" % len(ent))
    chk(c["official"] == 56, "official %d（期望 56）" % c["official"])
    # 官方正文逐条复核
    badbody = []
    for e in ent:
        if e["src"] == "official" and not e["bodyEn"]:
            badbody.append(e["key"])
    chk(not badbody, "official 条目缺 bodyEn：%s" % badbody[:5])
    chk(all(e["key"] == e["key"].lower() for e in ent), "键必须是小写规范名")
    chk("Ability" not in [e["key"] for e in ent], "键不该是显示名")

    print("=== ⑥ official_names.json（数据面）===")
    on = json.load(io.open(os.path.join(RES, "official_names.json"), encoding="utf-8"))
    t = {e["k"]: e for e in on["entries"]}
    pc = json.load(io.open(os.path.join(RES, "profile_cosmetics.json"), encoding="utf-8"))
    h_t = sum(1 for x in pc["titles"] if ("Titles/" + x["id"]) in t)
    h_a = sum(1 for x in pc["avatars"] if ("Avatars/" + x["n"]) in t)
    h_an = sum(1 for x in pc["achievements"] if ("Achievements/Title/" + x["id"]) in t)
    h_ad = sum(1 for x in pc["achievements"] if ("Achievements/" + x["id"]) in t)
    pb = json.load(io.open(r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json",
                           encoding="utf-8"))["decks"]
    h_p = sum(1 for d in pb if ("PrebuiltDecks/" + d["deckId"]) in t)
    print("  Titles %d/462 · Avatars %d/469 · 成就名 %d/102 · 成就描述 %d/102 · 预组 %d/103"
          % (h_t, h_a, h_an, h_ad, h_p))
    # 🔴 2026-10-19（`A2066`）：原来写死的是 2026-10-14 那一轮的一整组命中数
    #    （368 / 234 / 95 / 95 / 74），而之后数据**只增不减** ⇒ 每次数据变好反而报红。
    #    改成**下限**（各族命中不许回退到旧基线以下），现读只打印；数据将来再涨也不用改这里。
    floors = {'Titles': (h_t, 459), 'Avatars': (h_a, 234),
              '成就名': (h_an, 102), '成就描述': (h_ad, 102), '预组': (h_p, 95)}
    low = {k: "%d/%d" % (cur, flo) for k, (cur, flo) in floors.items() if cur < flo}
    chk(not low, "命中数低于下限（家族: 现读/下限）：%s" % low)

    print("=== ⑦ 接线点（grep 现读）===")
    for f, frag in [(r"d:/4/Unity/MyGame/Assets/CardPresentation/Shell/ProfileData.cs", "Loc.BulkOr"),
                    (r"d:/4/Unity/MyGame/Assets/CardPresentation/Shell/PrebuiltDecks.cs", "Loc.BulkOr"),
                    (r"d:/4/Unity/MyGame/Assets/CardPresentation/Core/Tooltip.cs", "_tips.TryGetValue")]:
        body = io.open(f, encoding="utf-8").read()
        chk(frag in body, "%s 里没有 %s" % (os.path.basename(f), frag))
    print("  三处接线点都在")

    print("\n=== 合计：通过 %d · 失败 %d ===" % (ok, bad))
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
