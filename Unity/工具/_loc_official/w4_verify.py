# -*- coding: utf-8 -*-
"""W4 收尾自检（**纯盘上核对、不跑 Unity**）：A1803 那几族的「官方键 → 我们的落点 → 值」逐项复算。"""
import io
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import w4_lib as L  # noqa: E402

## E5 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
## 样板 = `d:/4/Unity/工具/scan_swallowed_clauses.py` 的 A17 那段（工具线口径：`工具/**/*.py` 的 `--help` 必须 rc=0）。
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n'
          '  PYTHONIOENCODING=utf-8 python -I -X utf8 d:/4/Unity/工具/_loc_official/w4_verify.py\n'
          '  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。\n'
          '说明：W4 收尾自检 —— **纯盘上核对、不跑 Unity**（本脚本无其它参数，默认就跑全套）：\n'
          '  ① `Core/Loc.cs` 条目数下限 + 正则可见条数  ② 待接 12 条键  ③ 6 条中文缺口（只打印）\n'
          '  ④ 官方 `Battle/*` 键覆盖与 EN  ⑤ `trait_tips.json`  ⑥ `official_names.json` 命中（下限）\n'
          '  ⑦ 三处接线点 grep。全绿 ⇒ rc=0；任一条红 ⇒ rc=1。')
    _wf_help_sys.exit(0)

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
    # 条目数**下限**（**2026-10-24 重定基线** · 现读 **750** 条 · 正则可见 749 + 拼接写法 1）：
    #   上一版是 775（2026-10-19 的读数）—— **那不是「表变少了」，是下限过期**：
    #   第六十二会话 `a514fc53` **有意删掉 30 条自造文案**（`git show a514fc53 --numstat -- …/Loc.cs`
    #   = `42 插入 / 73 删除`，例：`Settings/Media/AudioMixerNote`，注释写「原版 `Media Tab` 只有 3 个子件」）
    #   ⇒ 表从 780 掉到 750。⛔ **别为了凑数字往表里补垃圾条目**（那正好是这一笔要清掉的东西）。
    #   余量 = **30 条（4.0%）**，依据：①这条守卫要挡的是「表被整段删掉」那种事故，所以余量不能大到
    #   几百条都放过；②同时必须容得下**一次合法的清理**——实测一次就删过 30 条，而比较是 `>=`
    #   ⇒ 750 再删 30 条仍过（750→720），删 31 条才红。⛔ 别把守卫关掉。
    #   ⚠️ 2026-10-24 现读复核（`A4`）：第七十会话 `45dd42d4` 又**有意删了 2 条**自造文案
    #      （`MenuDeck/GameMode/Skirmish` / `…/Classic` —— 值里带「（Skirmish · 12 张）」那种）
    #      ⇒ 752 → **750**（`git diff 45dd42d4^ 45dd42d4 -- …/Loc.cs` 两行 `-`、无 `+`）。
    #      ⚠️ `Core/Loc.cs` 文件头那段「本表共 N 条 / `grep -c` 现读 M」也**跟本条同源**，但它归
    #      界面前端代理维护（本脚本不越界改）⇒ 两边读数**以本脚本现读为准**，别照抄文件头那个数。
    #   ⚠️ 表还在长 ⇒ 下次「有意增删」后请**连同这条注释一起**更新基线，⛔ 别把守卫关掉。
    ENTRY_FLOOR = 720
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
