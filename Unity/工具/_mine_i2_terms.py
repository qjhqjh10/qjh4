#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
_mine_i2_terms.py —— 把 I2 官方本地化全表「还有什么可挖的」盘出来（**只读**，不写任何生产文件）

判据来源（全部本机可复查）：
  · 官方表全量 = `python -I -X utf8 d:/4/Unity/工具/_i2_decode_probe.py --out <path>`
    （9,447 条 × 15 列；`mLanguages[8]` = Simplified Chinese）
  · 工程已落的一份（en + zh-CN）：`d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json`
  · 我们的消费面：`Loc.cs` 表 · `cards_engine.json`（卡池）· `zh_cards.json`（自译卡名/效果）
    · `profile_cosmetics.json`（头像/称号/成就）· `Cardbacks.json` · `prebuilt_decks_full.json`
    · `Resources/trait_tips.json`（照规则书写的 61 条关键词 tooltip）· `.cs` 里出现的 I2 键字面量

用法（只读）：
  P='python -I -X utf8 d:/4/Unity/工具/_mine_i2_terms.py'
  $P --i2 <解码出来的 i2_full.json> --langs          # 15 列的填充率
  $P --i2 <...> --families                          # 键族普查（L1 + L2）
  $P --i2 <...> --keys <前缀> [--limit N]           # 看某族的值
  $P --i2 <...> --crossref                         # 每族的「我们用了多少」表
  $P --i2 <...> --joins                            # 数据面 join（卡池/装饰品/卡背/成就/trait）
  $P --i2 <...> --code                             # 代码里引用但 Loc 表里没有的键
"""

import argparse
import io
import json
import os
import re
import sys
import collections

ROOT = r"d:/4"
A = os.path.join(ROOT, "Unity/MyGame/Assets")
LOC_CS = os.path.join(A, "CardPresentation/Core/Loc.cs")
CARDS_ENGINE = os.path.join(A, "RuleEngine/Resources/cards_engine.json")
ZH_CARDS = os.path.join(ROOT, "Unity/数据/卡牌翻译/zh_cards.json")
PROFILE = os.path.join(A, "CardPresentation/Resources/profile_cosmetics.json")
CARDBACKS = os.path.join(A, "CardPresentation/Resources/Cardbacks.json")
TRAITTIPS = os.path.join(A, "CardPresentation/Resources/trait_tips.json")
PREBUILT = os.path.join(ROOT, "Unity/数据/游戏数据/prebuilt_decks_full.json")
FIXES = os.path.join(ROOT, "Unity/数据/游戏数据/cardface_fixes.json")

EN, ZH = 0, 8


def load_i2(path):
    with io.open(path, encoding="utf-8") as f:
        d = json.load(f)
    return d["mTerms"], d["mLanguages"]


def fam(terms, f, drop=1):
    """f 下面的后缀 → (en, zh)。⚠️ drop = 要剥掉的段数 +1：`Card_Name/X` 用默认 1；
    `Achievements/Title/X` 那种两级前缀要传 2（或直接用整段前缀切片）。"""
    off = len(f) + 1
    return {t["Term"][off:]: (t["Languages"][EN] or "", t["Languages"][ZH] or "")
            for t in terms if t["Term"].startswith(f + "/")}


# ── 我们工程侧：Loc.cs 那张表 ─────────────────────────────────────────────
ENTRY_RE = re.compile(
    r'\{\s*"(?P<key>[^"]+)"\s*,\s*new\s+Entry\(\s*"(?P<zh>(?:[^"\\]|\\.)*)"\s*,\s*"(?P<en>(?:[^"\\]|\\.)*)"\s*\)')


def load_loc_table():
    s = io.open(LOC_CS, encoding="utf-8").read()
    out = {}
    for m in ENTRY_RE.finditer(s):
        out[m.group("key")] = {"zh": m.group("zh"), "en": m.group("en"),
                               "line": s.count("\n", 0, m.start()) + 1}
    return out


# ── 我们工程侧：.cs 里出现的 I2 键字面量 ──────────────────────────────────
STR_RE = re.compile(r'"([A-Za-z][A-Za-z0-9_ /.\-]{2,60})"')
KEYLIKE = re.compile(r'^[A-Z][A-Za-z0-9_]*(?: [A-Za-z0-9_]+)?/[A-Za-z0-9_/.\- ]+$')


def scan_cs_keys():
    hits = collections.defaultdict(list)
    for dirpath, _d, files in os.walk(A):
        for fn in files:
            if not fn.endswith(".cs"):
                continue
            p = os.path.join(dirpath, fn)
            try:
                s = io.open(p, encoding="utf-8", errors="replace").read()
            except OSError:
                continue
            for i, line in enumerate(s.splitlines(), 1):
                for lit in STR_RE.findall(line):
                    if KEYLIKE.match(lit):
                        hits[lit].append("%s:%d" % (p.replace("\\", "/"), i))
    return hits


def jload(p):
    return json.load(io.open(p, encoding="utf-8"))


def joins(terms):
    """数据面 join —— 每条都给出「官方键怎么找 / 命中多少 / 我们现在是什么」。"""
    cn, cd = fam(terms, "Card_Name"), fam(terms, "Card_Description")
    out = []

    # ① 卡池
    cards = jload(CARDS_ENGINE)["cards"]
    en2cn = {}
    for k, v in cn.items():
        en2cn.setdefault(v[0].strip().lower(), k)
    n_n = n_n_same = n_n_diff = 0
    n_d = n_d_same = n_d_diff = 0
    miss = []
    for c in cards:
        k = c["id"] if c["id"] in cn else en2cn.get(c["name"].strip().lower())
        kd = c["id"] if c["id"] in cd else (k if k in cd else None)
        if k:
            n_n += 1
            if (c.get("nameZh") or "").strip() == cn[k][1].strip():
                n_n_same += 1
            else:
                n_n_diff += 1
        else:
            miss.append(c)
        if kd:
            n_d += 1
            if (c.get("descZh") or "").strip() == cd[kd][1].strip():
                n_d_same += 1
            else:
                n_d_diff += 1
    out.append(("Card_Name", len(cards), n_n, "按 id 或英文名", "%d 相同 / %d 不同" % (n_n_same, n_n_diff),
                "cards_engine.json 的 nameZh ← zh_cards.json（自译）"))
    out.append(("Card_Description", len(cards), n_d, "按 id 或经 Card_Name 英文名转", "%d 相同 / %d 不同" % (n_d_same, n_d_diff),
                "cards_engine.json 的 descZh ← zh_cards.json（自译）"))
    out.append(("(两族都拿不到官方键的卡)", len(cards), len(miss), "—", str(len(miss)),
                "前缀 = " + str(collections.Counter(re.match(r"^[A-Z]+", c["id"]).group(0) for c in miss))))

    # ② 装饰品
    pc = jload(PROFILE)
    for fname, arr, ofam, keyf in (("Avatars", pc["avatars"], "Avatars", "n"),
                                   ("Titles", pc["titles"], "Titles", "id")):
        off = fam(terms, ofam)
        hit = sum(1 for x in arr if x[keyf] in off)
        out.append((ofam, len(arr), hit, "%s/%s" % (ofam, keyf), "%d/%d" % (hit, len(arr)),
                    "profile_cosmetics.json（%s 显示名 = 我们拼的）" % ofam))
    ach = fam(terms, "Achievements/Title")
    acd = fam(terms, "Achievements")
    h1 = sum(1 for x in pc["achievements"] if x["id"] in ach)
    h2 = sum(1 for x in pc["achievements"] if x["id"] in acd)
    out.append(("Achievements/Title (+描述)", len(pc["achievements"]), h1, "Achievements/Title/<eventId>",
                "名称 %d/%d · 描述 %d/%d" % (h1, len(pc["achievements"]), h2, len(pc["achievements"])),
                "profile_cosmetics.json 的 n（资产英文串）"))

    # ③ 卡背
    cb = fam(terms, "Cardbacks")
    items = jload(CARDBACKS)["items"]
    hit = sum(1 for x in items if x.get("name") in cb)
    out.append(("Cardbacks (+Item_Description)", len(items), hit, "Cardbacks/<sprite 名>",
                "%d/%d" % (hit, len(items)), "Cardbacks.json 的 name（= sprite 名，英文）"))

    # ④ 预组卡组
    pd = fam(terms, "PrebuiltDecks")
    decks = jload(PREBUILT)
    h = sum(1 for x in decks if x.get("deckId") in pd)
    en2k = {v[0]: k for k, v in pd.items()}
    h2 = sum(1 for x in decks if x.get("deckName") in en2k)
    out.append(("PrebuiltDecks", len(decks), max(h, h2), "PrebuiltDecks/<deckId>", "deckId %d · 英文名 %d" % (h, h2),
                "prebuilt_decks_full.json 的 deckName（英文）"))

    # ⑤ 关键词 tooltip（我们照规则书写的）
    loc = load_loc_table()
    ctd = fam(terms, "CardTraitDescription")
    our_traits = [k.split("/", 1)[1] for k in loc if k.startswith("Card_Trait/")]
    hit = sum(1 for k in our_traits if k in ctd)
    tt = jload(TRAITTIPS)["entries"]
    out.append(("CardTraitDescription", len(tt), hit, "CardTraitDescription/<规范键>", "%d/%d" % (hit, len(tt)),
                "Resources/trait_tips.json ← 规则书（**非原版**）"))

    # ⑥ 聊天钮
    cm = [t for t in terms if t["Term"].startswith("Chat_Messages/")]
    per = [t for t in cm if re.match(r"^Chat_Messages/[^-]+-Chat\d+$", t["Term"])]
    wl = sorted({t["Term"].split("/")[1].split("-")[0] for t in per})
    out.append(("Chat_Messages", len(cm), 0, "Chat_Messages/<战将id>-Chat5..10（+ `Battle/Chat/*` 6 条分类名）",
                "战将 %d 个 · 通用行 %d" % (len(wl), len(per)),
                "ChatPopupPanel.cs —— 现用枚举名当标签（我们挑的）"))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--i2", required=True)
    for f in ("families", "crossref", "joins", "code"):
        ap.add_argument("--" + f, action="store_true")
    ap.add_argument("--keys", default=None)
    ap.add_argument("--langs", action="store_true")
    ap.add_argument("--out", default=None)
    ap.add_argument("--tsv", default=None)
    ap.add_argument("--limit", type=int, default=40)
    args = ap.parse_args()

    terms, langs = load_i2(args.i2)
    by = {t["Term"]: t for t in terms}

    if args.langs:
        for i, l in enumerate(langs):
            print("%2d  %-24s code=%-8s 非空=%d" % (i, l.get("Name"), l.get("Code"),
                                                  sum(1 for t in terms if t["Languages"][i])))
        return 0

    if args.keys:
        sel = [t for t in terms if t["Term"].startswith(args.keys)]
        print("前缀 %r 命中 %d 条（打印前 %d）" % (args.keys, len(sel), args.limit))
        for t in sel[:args.limit]:
            print("%-52s | EN: %s | ZH: %s" % (t["Term"],
                  (t["Languages"][EN] or "").replace("\n", "\\n")[:60],
                  (t["Languages"][ZH] or "").replace("\n", "\\n")[:50]))
        return 0

    if args.families:
        f1, f2, z = collections.Counter(), collections.Counter(), collections.Counter()
        for t in terms:
            p = t["Term"].split("/")
            f1[p[0]] += 1
            f2["/".join(p[:2])] += 1
            if not t["Languages"][ZH]:
                z[p[0]] += 1
        print("=== L1（%d 族 · %d 条）===" % (len(f1), len(terms)))
        for k, v in f1.most_common():
            print("%6d  %-26s zh空=%d" % (v, k, z[k]))
        print("\n=== L2（>=15）===")
        for k, v in f2.most_common():
            if v >= 15:
                print("%6d  %s" % (v, k))
        return 0

    if args.joins:
        print("%-34s %-8s %-8s %s" % ("族", "我们条数", "官方命中", "明细 / 我们现在的值从哪来"))
        for r in joins(terms):
            print("%-34s %-8s %-8s %s | %s | %s" % (r[0], r[1], r[2], r[3], r[4], r[5]))
        return 0

    if args.code:
        loc = load_loc_table()
        cs = scan_cs_keys()
        io_ = [k for k in cs if k in by]
        missing = [k for k in io_ if k not in loc]
        print("cs 引用且官方有该键: %d；其中【不在 Loc.cs 表里】: %d  %s"
              % (len(io_), len(missing), dict(collections.Counter(k.split('/')[0] for k in missing))))
        for k in sorted(missing):
            print("  %-46s | %-40s | %s" % (k, by[k]["Languages"][EN][:38], by[k]["Languages"][ZH][:24]))
        return 0

    if args.crossref:
        loc = load_loc_table()
        cs = scan_cs_keys()
        cards = jload(CARDS_ENGINE)["cards"]
        ids = {c["id"] for c in cards}
        f1 = collections.Counter(t["Term"].split("/")[0] for t in terms)
        hdr = ["键族", "官方条数", "cs字面量命中", "Loc.cs表命中", "卡池命中", "zh与官方逐字相同", "zh与官方不同"]
        rows = []
        for f, n in f1.most_common():
            csh = sum(1 for k in cs if k.split("/")[0] == f and k in by)
            loch = sum(1 for k in loc if k.split("/")[0] == f)
            pool = same = diff = 0
            if f in ("Card_Name", "Card_Description"):
                for t in terms:
                    if not t["Term"].startswith(f + "/"):
                        continue
                    suf = t["Term"].split("/", 1)[1]
                    if suf in ids:
                        pool += 1
                        nm = next((c["name"] for c in cards if c["id"] == suf), None)
                        ours = None
                        if f == "Card_Name":
                            ours = next((c.get("nameZh") for c in cards if c["id"] == suf), None)
                        else:
                            ours = next((c.get("descZh") for c in cards if c["id"] == suf), None)
                        if ours is not None:
                            if ours.strip() == t["Languages"][ZH].strip():
                                same += 1
                            else:
                                diff += 1
            rows.append((f, n, csh, loch, pool, same, diff))
        print("\t".join(hdr))
        for r in rows:
            print("\t".join(str(x) for x in r))
        if args.out:
            with io.open(args.out, "w", encoding="utf-8", newline="") as fo:
                fo.write("\t".join(hdr) + "\n")
                for r in rows:
                    fo.write("\t".join(str(x) for x in r) + "\n")
            print("\n已写出: %s" % args.out)
        return 0

    ap.print_help()
    return 0


if __name__ == "__main__":
    sys.exit(main())
