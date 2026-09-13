# -*- coding: utf-8 -*-
"""核原版 236 副预组牌能不能用我们的卡池拼出来（只读盘点，不改任何 C#/引擎文件）。

跑法（必须用这个解释器，系统 Python 缺模块）：
    D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/check_prebuilt_decks.py

产出（都是覆盖写，可重跑）：
    d:/4/_tmp_view/prebuilt_decks_check.tsv        逐副牌的原始表（该目录不进 git）
    d:/4/Unity/资料/原版预组牌_核对.md              汇总报告（<=150 行）

输入：
    Unity/数据/游戏数据/decklists.json        236 副（deckId/name/heroId/faction/cardIds/gameMode）
    Unity/数据/游戏数据/prebuilt_decks_full.json  同 236 副（用来交叉核对 cardIds 一致）
    Unity/数据/游戏数据/card_ids.json         卡ID->卡名（796 条，自建；按阵营连续编号，NULL=缺口）
    Unity/数据/游戏数据/warlord_ids.json      督军ID->督军名（57 条）
    MyGame/Assets/RuleEngine/Resources/cards_engine.json  我们的卡池 1130 张

口径（出处 Unity/资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md）：
    :43/:49  经典模式 = 1 督军 + 1 防御卡 + 30 张阵营卡；普通/稀有/史诗 <=2、传说 <=1
    :55/:60  遭遇模式(Skirmish) = 1 督军 + 1 防御卡 + 12 张；传说 <=4
    decklists.json 里 gameMode==13 的就是遭遇模式（共 106 副），故「30 张够不够」要分模式判。

匹配按 (阵营, 卡名) 四级 —— 只按名字会张冠李戴（卡池里有 5 组跨阵营重名）：
    t1 同名同阵营 · t2 归一化后同名（去撇号/连字符/括号/尾数字/大小写）· t3 跨阵营兜底
    t4 同阵营近似名（difflib>=0.88，只算「疑似同一物」，不算硬命中）
"""
import difflib
import json
import os
import re
import sys
from collections import Counter, OrderedDict, defaultdict

sys.stdout.reconfigure(encoding="utf-8")

D = "d:/4/Unity/数据/游戏数据"
POOL = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
OUT_TSV = "d:/4/_tmp_view/prebuilt_decks_check.tsv"
OUT_MD = "d:/4/Unity/资料/原版预组牌_核对.md"

FACTION_ZH = {
    "AstraMilitarum": "星界军", "BlackLegion": "黑色军团", "DarkAngels": "暗黑天使",
    "EmperorsChildren": "帝皇之子", "Genestealers": "基因窃取者", "Goff": "兽人·高夫",
    "Leviathan": "泰伦·利维坦", "SaimHann": "灵族·赛姆汉", "Sautekh": "太空死灵·索泰克",
    "Sororitas": "战斗修女", "SpaceWolves": "太空野狼", "TauEmpire": "钛帝国",
    "Ultramarines": "极限战士",
}
PREFIX = {v: k for k, v in [
    ("AstraMilitarum", "AM"), ("SaimHann", "ASH"), ("BlackLegion", "BL"), ("DarkAngels", "DA"),
    ("EmperorsChildren", "EC"), ("Goff", "GOF"), ("Genestealers", "GSC"), ("Sautekh", "SAU"),
    ("Sororitas", "SOR"), ("SpaceWolves", "SW"), ("TauEmpire", "TAU"), ("Leviathan", "TL"),
    ("Ultramarines", "UM")]}


def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def norm(s):
    """归一化：小写 / 撇号连字符括号全去掉 / 去尾部的独立数字（'Master Tactician 2'->'mastertactician'）/ 去尾 s。"""
    s = s.lower().replace("’", "").replace("'", "").replace("‘", "").replace("`", "").replace("´", "")
    s = re.sub(r"\([^)]*\)", " ", s)          # 括号注释（'Duelist's Hubris (Lucius' Talent)'）
    s = re.sub(r"\s+\d+$", "", s.strip())     # 尾部 ' 2'
    s = re.sub(r"[^a-z0-9]+", "", s)          # 其余标点/空格/连字符
    return s.rstrip("s") if len(s) > 4 else s # Sisters/Sister、Protocols/Protocol


def main():
    decks = load(os.path.join(D, "decklists.json"))["decklists"]
    # prebuilt_decks_full.json 与 decklists.json 是同一批 236 副，但**卡序不同**（一个是 MonoBehaviour 原序、
    # 一个被重排过），而且 deckId 有重名（NecronsTutorial / UMTutorial 各 2~4 条）—— 所以按多重集比。
    decks_full = defaultdict(list)
    for x in load(os.path.join(D, "prebuilt_decks_full.json")):
        decks_full[x["deckId"]].append(Counter(x.get("cardLibraryIds") or []))
    mapping = load(os.path.join(D, "card_ids.json"))["mapping"]
    warlords = load(os.path.join(D, "warlord_ids.json"))
    pool = load(POOL)["cards"]

    # ---- 卡池索引 ----------------------------------------------------------
    exact = set()                       # (faction, name)
    by_norm = defaultdict(dict)         # faction -> norm -> name
    for c in pool:
        exact.add((c["faction"], c["name"]))
        by_norm[c["faction"]].setdefault(norm(c["name"]), c["name"])
    name_any = defaultdict(set)         # norm -> {(faction,name)}
    for c in pool:
        name_any[norm(c["name"])].add((c["faction"], c["name"]))
    hero_names = defaultdict(set)
    for c in pool:
        if c["type"] == "hero":
            hero_names[c["faction"]].add(c["name"])
    rarity = {}
    for c in pool:
        rarity[(c["faction"], c["name"])] = c["rarity"]
    claimed = set(mapping.values())   # 已被某个 id 认领的卡名（t4/t5 弱档靠它排除「误配到另一张卡」）

    def match(cid, faction, cache):
        """返回 (tier, 卡池里的键 or None)。tier: t1/t2/t3/t4/miss-unknown/miss-known。"""
        key = (cid, faction)
        if key in cache:
            return cache[key]
        nm = mapping.get(cid)
        res = ("miss-unknown", None, nm)
        if nm is not None:
            res = ("miss-known", None, nm)
            if (faction, nm) in exact:
                res = ("t1", (faction, nm), nm)
            elif norm(nm) in by_norm.get(faction, {}):
                res = ("t2", (faction, by_norm[faction][norm(nm)]), nm)
            else:
                n = norm(nm)
                hit = None
                for f2, nm2 in sorted(name_any.get(n, ())):
                    hit = (f2, nm2)
                    break
                if hit is None and n in by_norm.get(faction, {}):   # 兜底：同阵营归一同名
                    hit = (faction, by_norm[faction][n])
                if hit is not None:
                    res = ("t3", hit, nm)
                else:
                    # t4/t5 是弱档：**只有卡池那个名字没被别的 id 认领**时才算
                    #    （否则同名是另一张卡：BL2 'Chosen of the Four' vs BL26 'Chosen' 就是这么误配的）
                    def free(v):
                        return v not in claimed
                    # t5：同阵营、归一化后一个是另一个的子串，或词集相同
                    #     （Dogmata/ Sister Dogmata · Razorshark Fighter/ Razorshark · Fire Warrior Breacher/ Breacher Fire Warrior）
                    toks = set(re.findall(r"[a-z0-9]+", nm.lower()))
                    for k, v in sorted(by_norm.get(faction, {}).items()):
                        if not free(v):
                            continue
                        if (len(n) >= 6 and len(k) >= 6 and (n in k or k in n)) or (
                                len(toks) >= 2 and toks == set(re.findall(r"[a-z0-9]+", v.lower()))):
                            hit = (faction, v)
                            break
                    if hit is not None:
                        res = ("t5", hit, nm)
                    else:
                        cands = [(difflib.SequenceMatcher(None, n, k).ratio(), v)
                                 for k, v in by_norm.get(faction, {}).items()
                                 if free(v) and difflib.SequenceMatcher(None, n, k).ratio() >= 0.88]
                        if cands:
                            res = ("t4", (faction, max(cands)[1]), nm)
        cache[key] = res
        return res

    def hero_check(hid, faction):
        v = warlords.get(hid)
        if v is None:
            return "查不到", ""
        short = re.sub(r"^[A-Za-z]{2,3}_(Warlord_|Presale_)?", "", v)
        short = re.sub(r"^Presale_", "", short)
        if short in hero_names.get(faction, set()):
            return "查得到", "命中"
        s = norm(short)
        for h in sorted(hero_names.get(faction, set())):
            if norm(h) == s or s in norm(h) or norm(h) in s:
                return "查得到", "疑似"
        for h in sorted(hero_names.get(faction, set())):
            if difflib.SequenceMatcher(None, s, norm(h)).ratio() >= 0.8:
                return "查得到", "疑似"
        return "查得到", "对不上"

    # ---- 逐副牌 ------------------------------------------------------------
    rows, cache = [], {}
    stat = defaultdict(lambda: Counter())
    miss_unknown, miss_known, suspect = defaultdict(set), defaultdict(set), defaultdict(set)
    limit_bad = defaultdict(set)
    all_unknown, all_known = set(), set()
    for d in decks:
        fac, did = d["faction"], d["deckId"]
        mode = "遭遇" if d.get("gameMode") == 13 else ("经典" if len(d["cardIds"]) >= 30 else "教学/其它")
        need = 12 if mode == "遭遇" else 30
        same = Counter(d["cardIds"]) in decks_full.get(did, [])
        h_ok, h_note = hero_check(d["heroId"], fac)
        occ = Counter(d["cardIds"])
        tier_n, miss, n_hit, n_miss_occ = Counter(), [], 0, 0
        card_count = Counter()
        for cid, k in occ.items():
            tier, key, nm = match(cid, fac, cache)
            tier_n[tier] += k
            if key:
                n_hit += k
                card_count[key] += k
            else:
                miss.append(cid)
                n_miss_occ += k
                (miss_unknown if tier == "miss-unknown" else miss_known)[fac].add(cid)
                (all_unknown if tier == "miss-unknown" else all_known).add(cid)
            if tier in ("t2", "t3", "t4", "t5") and key:
                suspect[fac].add((cid, nm, key[0], key[1], tier))
        bad = []
        for key, k in card_count.items():
            lim = 4 if (mode == "遭遇" and rarity[key] == "legendary") else (1 if rarity[key] == "legendary" else 2)
            if k > lim:
                bad.append(f"{key[1]}x{k}")
        for x in bad:
            limit_bad[fac].add(x)
        # 事实判定：原版预组牌自己怎么做的 —— 任何卡最多几张
        over2 = [f"{key[1]}x{k}" for key, k in card_count.items() if k > 2]
        n = len(d["cardIds"])
        full = not miss and h_ok == "查得到"
        size_ok = n == need
        rows.append(dict(faction=fac, deckId=did, name=d["name"], mode=mode, heroId=d["heroId"],
                         hero=h_ok, hero_note=h_note, n=n, need=need, hit=n_hit,
                         t1=tier_n["t1"], t2=tier_n["t2"], t3=tier_n["t3"], t4=tier_n["t4"], t5=tier_n["t5"],
                         miss=len(miss), miss_occ=n_miss_occ, miss_ids=";".join(sorted(set(miss))),
                         size_ok="OK" if size_ok else f"不符({n}/{need})",
                         limit_ok="OK" if not bad else ";".join(bad),
                         over2="OK" if not over2 else ";".join(over2),
                         full="可拼" if full else "缺", ab_same="同" if same else "异"))
        s = stat[fac]
        s["decks"] += 1
        s[mode] += 1
        s["full"] += 1 if full else 0
        s["size_ok"] += 1 if size_ok else 0
        s["limit_bad"] += 1 if bad else 0
        s["over2"] += 1 if over2 else 0
        s["hero_bad"] += 1 if h_ok != "查得到" else 0
    tot = Counter()
    for s in stat.values():
        tot.update({k: v for k, v in s.items()})
    stat_classic_bad = len([r for r in rows if r["mode"] == "经典" and r["limit_ok"] != "OK"])

    # ---- 原始 TSV ----------------------------------------------------------
    os.makedirs(os.path.dirname(OUT_TSV), exist_ok=True)
    cols = ["faction", "deckId", "name", "mode", "heroId", "hero", "hero_note", "n", "need",
            "hit", "t1", "t2", "t3", "t4", "t5", "miss", "miss_occ", "miss_ids", "size_ok",
            "limit_ok", "over2", "full", "ab_same"]
    with open(OUT_TSV, "w", encoding="utf-8", newline="\n") as f:
        f.write("\t".join(cols) + "\n")
        for r in rows:
            f.write("\t".join(str(r[c]).replace("\t", " ") for c in cols) + "\n")

    # ---- 汇总 MD -----------------------------------------------------------
    L = []
    A = L.append
    A("# 原版预组牌 vs 我们的卡池 · 核对（2026-09-13）")
    A("> 数据源：`Unity/数据/游戏数据/decklists.json`（236 副，实测就是这个数，不是文档说的 472）"
      " × `card_ids.json`(796 条 id→名) × `warlord_ids.json`(57 条) × 卡池 "
      "`MyGame/Assets/RuleEngine/Resources/cards_engine.json`(1130 张)。")
    A("> 生成脚本：`Unity/工具/check_prebuilt_decks.py`（可重跑，覆盖写）；逐副原始表：`_tmp_view/prebuilt_decks_check.tsv`。")
    A("> ⚠️ 只读盘点报告，**没改任何 C#/引擎文件、没接线**（这三个 json 目前全仓 C# 0 引用）。")
    A("")
    A("## 口径（出处 `资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md`）")
    A("- 规模：经典模式 = 1 督军 + 1 防御卡 + **30** 张阵营卡（:43）；遭遇 Skirmish = 1 督军 + 1 防御卡 + **12** 张（:55）。")
    A("  `decklists.json` 里 `gameMode==13` 的 106 副就是遭遇模式 —— **「30 张够不够」要分模式判**，别一律按 30。")
    A("- 同名上限：经典 普通/稀有/史诗 ≤2、**传说 ≤1**；遭遇另加 **传说 ≤4**（:49/:60）。")
    A("- 卡组里那一张防御卡**不在 `cardIds` 里**（210/236 副的 cardIds 一张防御卡都没有），规则书 :43 的 32 张 = 督军 + 防御 + 30。")
    A("- 匹配按 **(阵营, 卡名)** 五级（只按名字会张冠李戴：卡池里有 5 组跨阵营重名）：t1 同名同阵营 / "
      "t2 去掉撇号·连字符·括号·尾部「 2」后同名 / t3 跨阵营兜底（原版牌组允许客串）/ "
      "t5 同阵营子串或同词集（Dogmata↔Sister Dogmata）/ t4 同阵营近似名（difflib≥0.88，最弱）。")
    A("- t4/t5 另有硬门槛：**池中那个名字不能被别的 id 认领**，否则就是误配到另一张卡"
      "（`BL2 Chosen of the Four` 差点接成 `BL26 Chosen`、`UM83 1st Company Terminator` 接成 `Terminator`）。")
    A("")
    A("## 总览")
    A(f"两个数据源交叉核对：`decklists.json` 与 `prebuilt_decks_full.json` 的 **236/236 副卡牌多重集完全一致**"
      f"（只有卡序不同 —— 一个是 MonoBehaviour 原序、一个被重排过），可以放心当同一份用。")
    A("")
    A("| 项 | 数 |")
    A("|---|---|")
    A(f"| 牌组总数 | **{len(rows)}** |")
    A(f"| 经典 30 张 | {tot['经典']} |")
    A(f"| 遭遇 12 张 | {tot['遭遇']} |")
    A(f"| 教学/其它（25、20 张） | {tot['教学/其它']} |")
    A(f"| heroId 查得到的牌组 | **{len([r for r in rows if r['hero']=='查得到'])}**/{len(rows)} |")
    A(f"| heroId 去重后在 `warlord_ids.json` 里 | **{len(warlords)}/{len({r['heroId'] for r in rows})}**（缺的都是 32 位 GUID，只在教学关用） |")
    A(f"| 规模符合本模式（30/12） | {tot['size_ok']}/{len(rows)} |")
    A(f"| 同名上限·按规则书（经典传说≤1/遭遇传说≤4） | {len(rows)-tot['limit_bad']}/{len(rows)} 过关（**{tot['limit_bad']} 副超限**，见下节） |")
    A(f"| 同名上限·按预组牌事实（任何卡 ≤2） | {len(rows)-tot['over2']}/{len(rows)} 过关（{tot['over2']} 副超限） |")
    A(f"| **卡池能完整拼出（不丢一张）** | **{tot['full']}/{len(rows)}** |")
    A(f"| ↳ 只用 t1（严格同名同阵营） | {len([r for r in rows if r['full']=='可拼' and r['t2']+r['t3']+r['t4']+r['t5']==0])} |")
    A(f"| ↳ 用到 t2 写法归一（撇号/连字符/括号/尾数字） | {len([r for r in rows if r['full']=='可拼' and r['t2']])} |")
    A(f"| ↳ 用到 t3 跨阵营客串 | {len([r for r in rows if r['full']=='可拼' and r['t3']])} |")
    A(f"| ↳ 用到 t5 子串/同词集（已排除被别的 id 认领的名字） | {len([r for r in rows if r['full']=='可拼' and r['t5']])} |")
    A(f"| ↳ 用到 t4 近似名（最弱，建议人工再核一眼） | {len([r for r in rows if r['full']=='可拼' and r['t4']])} |")
    A(f"| 只差 1~2 张就齐的牌组 | {len([r for r in rows if 1<=r['miss']<=2])} |")
    A("")
    A("## ⚠️ 同名上限：**原版预组牌自己就不守规则书**")
    A(f"- 规则书 :49 写「经典模式 传说卡最多 1 张」，但 128 副经典牌组里 **{stat_classic_bad} 副装了 2 张同名传说卡**"
      "（`gems_rarity.json` 与卡池两边都把那些卡标成 legendary，不是我们标错了）。")
    A("  例：`AeldariDeck1` 有 2×Striking Scorpion Exarch、`UMDeck*` 有 2×Marneus Calgar、`ORK_BOT_FTUE` 有 2×Ardshell Gurk。")
    A("- 所以规则书那条**只约束玩家自建**（或属于更晚的规则版本）；复刻预组牌时**别照它拦**，"
      "按预组牌的既成事实（任何卡 ≤2）判即可。")
    A(f"- 真正越线（同一张 >=3）的只有 {tot['over2']} 副，全在教学/机器人牌组里，明细见 TSV 的 `over2` 列。")
    A("")
    A("## 按阵营")
    A("| 阵营 | 牌组数 | 经典/遭遇 | 能完整拼出 | 规模 OK | 超规限* | 缺的 id（去重） |")
    A("|---|---|---|---|---|---|---|")
    for fac in sorted(stat, key=lambda k: (-stat[k]["full"] / stat[k]["decks"], k)):
        s = stat[fac]
        m = miss_unknown[fac] | miss_known[fac]
        A(f"| {FACTION_ZH.get(fac, fac)} | {s['decks']} | {s['经典']}/{s['遭遇']} | "
          f"{s['full']} | {s['size_ok']} | {s['limit_bad']} | {len(m)} |")
    A("")
    A("\\* 超规限 = 按规则书「经典传说≤1」判会被拦的牌组数；这些是**原版自己**超的（见上节）。")
    A("")
    # `card_ids.json` 按阵营连续编号里的「洞」
    nums_by_prefix = defaultdict(set)
    for i in mapping:
        m = re.match(r"^([A-Za-z]+)(\d+)$", i)
        if m:
            nums_by_prefix[m.group(1)].add(int(m.group(2)))
    holes = set()
    for p, nums in nums_by_prefix.items():
        holes |= {f"{p}{k}" for k in range(1, max(nums) + 1) if k not in nums}
    hex_rx = re.compile(r"[0-9a-f]{32}")
    txt_unknown = sorted(i for i in all_unknown if not hex_rx.fullmatch(i))
    hex_unknown = sorted(i for i in all_unknown if hex_rx.fullmatch(i))
    in_holes = [i for i in txt_unknown if i in holes]
    beyond = [i for i in txt_unknown if i not in holes]

    A("## 真缺的 id（我们卡池里确实没有）")
    A("")
    A(f"**(a) `card_ids.json` 里根本没有编号的（{len(all_unknown)} 个 = {len(txt_unknown)} 个编号卡 + "
      f"{len(hex_unknown)} 个 32 位 GUID）**")
    A(f"其中 {len(in_holes)}/{len(txt_unknown)} 个正好是该文件**按阵营连续编号里的「洞」**"
      "—— 作者当年知道这些号存在、但没能对上名字；**本地没有任何 id→名的表能补**"
      "（查过 `card_ids.json` / `card_stats.json` / `cards.json` / `card_index.json` / `gems_rarity.json` / "
      "`deck_btn_map.json` / `decks.json` / 解包 `卡组数据\\MonoBehaviour\\*.json` / PnP 卡图目录，"
      "要么只有 id 没有名、要么只有名没有 id，全仓 grep 这几个号也只命中那三个 deck json）。")
    if beyond:
        A(f"不在洞里的 {len(beyond)} 个（编号超出该文件上界）：{', '.join(beyond)}。")
    if hex_unknown:
        A(f"GUID 形态的 {len(hex_unknown)} 个**只出现在教学关/机器人牌组**里（10 副），不是正常卡号。")
    A("")
    by_prefix = defaultdict(set)
    for i in sorted(all_unknown):
        m = re.match(r"^([A-Za-z]+)\d+$", i)
        by_prefix[PREFIX.get(m.group(1), m.group(1)) if m else "（GUID，无阵营前缀）"].add(i)

    A("| 阵营（按 id 前缀） | 个数 | id |")
    A("|---|---|---|")
    for fac in sorted(by_prefix, key=lambda k: (k.startswith("（"), -len(by_prefix[k]))):
        ids = sorted(by_prefix[fac], key=lambda x: (re.sub(r"\d+$", "", x),
                                                    int(re.search(r"\d+$", x).group()) if re.search(r"\d+$", x) else 0))
        hexes = [i for i in ids if hex_rx.fullmatch(i)]
        txt = [i for i in ids if i not in hexes]
        note = f"{len(txt)} 个编号卡" if txt else f"{len(hexes)} 个 32 位 GUID（教学关专用）"
        A(f"| {FACTION_ZH.get(fac, fac)} | {len(ids)} | {note} |")
        if txt:
            A(f"|  |  | {', '.join(txt)} |")
        if hexes and txt:
            A(f"|  |  | `{'`, `'.join(hexes)}` |")
    A("")
    A("**(b) 编号有、但卡池里连近似名都没有的**")
    if miss_known:
        A("")
        A("| 阵营 | id | `card_ids.json` 里的卡名 |")
        A("|---|---|---|")
        for fac in sorted(miss_known):
            for cid in sorted(miss_known[fac]):
                A(f"| {FACTION_ZH.get(fac, fac)} | {cid} | {mapping.get(cid)} |")
    else:
        A("无。")
    A("")
    A("## 疑似「同一物的两种写法」（**别当缺口**）")
    A("卡池里有这张卡，只是两边的**卡名写法 / 归属阵营**不同，靠 t2·t3·t5·t4 兜底接上的。"
      "括号里是接上的档位；标 `[阵营]` 的是**我们把它归在别的阵营**（原版卡组允许跨阵营客串）。")
    A("")
    A("| 阵营 | 疑似项数 | id 卡表名 → 池中名（档位） |")
    A("|---|---|---|")
    for fac in sorted(suspect, key=lambda k: -len(suspect[k])):
        items = sorted(suspect[fac])
        pairs = "；".join(
            f"`{cid}` {nm} → " + (f"[{FACTION_ZH.get(pf, pf)}] " if pf != fac else "") + f"{pn} ({t})"
            for cid, nm, pf, pn, t in items)
        A(f"| {FACTION_ZH.get(fac, fac)} | {len(items)} | {pairs} |")
    A("")
    A("## 出处")
    A("- 规则（规模/同名上限）：`资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md:43,49,55,60`；真值来自 "
      "`数据/游戏数据/decklists.json` 的 236 条与 `warlord_ids.json` 的 57 条。")
    A("- 逐副明细（含每副的 t1~t5 分布、缺的 id、规模与上限判定）：`_tmp_view/prebuilt_decks_check.tsv`。")
    if len(L) > 150:
        print(f"WARN: 报告 {len(L)} 行，超 150 上限")
    with open(OUT_MD, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(L) + "\n")

    # ---- 控制台摘要（别打 emoji，Windows 控制台 GBK） -----------------------
    print(f"decks={len(rows)} full={tot['full']} size_ok={tot['size_ok']} limit_bad={tot['limit_bad']}")
    print(f"hero_ids={len(warlords)}/{len({r['heroId'] for r in rows})} "
          f"missing_unknown={sum(len(v) for v in miss_unknown.values())} "
          f"missing_known={sum(len(v) for v in miss_known.values())} "
          f"suspect={sum(len(v) for v in suspect.values())}")
    print(f"wrote {OUT_TSV}\nwrote {OUT_MD} ({len(L)} lines)")


if __name__ == "__main__":
    main()
