# -*- coding: utf-8 -*-
"""把原版「预组卡组」解析成我们卡池的 id，产出一份**旁挂数据**给运行时读。

跑法：
    python d:/4/Unity/工具/gen_prebuilt_decks.py

产出（覆盖写，可重跑）：
    d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json

🔴 判据只有一份：匹配规则**原样**搬自 `工具/check_prebuilt_decks.py` 的 `match()` / `hero_check()`
   —— 那边是「核对报告」，这边是「生产产物」。**改规则必须两边一起改**（或者以后让本脚本 import 它）。
🔴 **本脚本不重排、不补牌、不改任何原版数据** —— 只做「原版 id → 我们的 id」的解析。
   尤其**绝不碰教程那 12 副**：按 `isPractice == 1` 过滤（教程副全部 `isPractice == 0`），并有断言兜底。

输入：
    Unity/数据/游戏数据/decklists.json        236 副（取 isPractice==1 的 103 副）
    Unity/数据/游戏数据/card_ids.json         原版卡 id -> 英文卡名
    Unity/数据/游戏数据/warlord_ids.json      督军 id -> 督军名
    MyGame/Assets/RuleEngine/Resources/cards_engine.json   我们的卡池 1126 张
"""
import difflib
import json
import os
import re
import sys
from collections import Counter, defaultdict

D = "d:/4/Unity/数据/游戏数据"
POOL = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
ZH = "d:/4/Unity/数据/卡牌翻译/zh_decks.json"
# 卡背：`cardback` 那一列有 31 副是空的，但 `cardbackId`（GUID）一直都在。
# GUID → 卡背名 的表**就在解包资源里**：`bundle_cosmeticsso_assets_all/MonoBehaviour/Cardback_*.json`
# 的 `uniqueId`（= 那串 GUID）与 `m_Name`（= 卡背名）。⚠️ 本脚本因此**只读**依赖 `d:/2`（铁律 2：只读不改）。
CARDBACK_SO = "d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/Cardback_*.json"
# 我们工程里实际有的卡背图（判「这张图到底有没有」用）
CARDBACK_DIR = "d:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/cardbacks"

# 🔴 **卡背名别名**（原版 cosmetic SO 名 ↔ 图名，两套前缀写法）。
# 证据（**整套**对应，不是猜单张）：帝皇之子 `cardArmy == 120` 的 cosmetic 共 **9 个**、我们手上的图 **9 张**，
# **后缀逐张一一对应**（A Canvas Eclectic / Chorus of Excess / Dark Tempations / Feeding the Addiction /
# Genesis of Perfection / Lament of the Flayed / Lord of Excess / Thrill Seekers / Emperors Children）——
# 只有前缀两种写法：名字本身就是「Emperors Children」那张用 `EC_`，其余 8 张用 `CSM_EmperorsChildren_`。
CARDBACK_ALIAS = {
    "Cardback_EC_A Canvas Eclectic": "Cardback_CSM_EmperorsChildren_A Canvas Eclectic",
    "Cardback_EC_Chorus of Excess": "Cardback_CSM_EmperorsChildren_Chorus of Excess",
    "Cardback_EC_Dark Tempations": "Cardback_CSM_EmperorsChildren_Dark Tempations",
    "Cardback_EC_Feeding the Addiction": "Cardback_CSM_EmperorsChildren_Feeding the Addiction",
    "Cardback_EC_Genesis of Perfection": "Cardback_CSM_EmperorsChildren_Genesis of Perfection",
    "Cardback_EC_Lament of the Flayed": "Cardback_CSM_EmperorsChildren_Lament of the Flayed",
    "Cardback_EC_Lord of Excess": "Cardback_CSM_EmperorsChildren_Lord of Excess",
    "Cardback_EC_Thrill Seekers": "Cardback_CSM_EmperorsChildren_Thrill Seekers",
}

# 🔴 **卡背替身**（原版那张图**本地四个来源都没有**，用同阵营里最贴近的一张顶上）—— **这是我们的选择**。
# 用户 2026-09-26 拍板：「没有的两个卡背……你从它们阵营里选一个卡背替代。」
# 选法（逐条可复述）：
#   · `Cardback_GOF_Warlord_Zagstruk`（兽人/高夫，**督军主题**）⇒ `Cardback_GOF_Ghazgkhull_AA_HB`
#     —— 该阵营里**唯一另一张督军主题**的（Ghazghkull 是兽人至高头目；原版那张的 `GOF_Warlord_Ghazghkull` **也没有图**）。
#   · `Cardback_SW_Wolfs Lair`（太空野狼）⇒ `Cardback_SW_Feral Barbarity`
#     —— 用它的那副牌叫「**野狼·凶暴**+猎杀标记控制」，卡背名 **Feral = 凶暴**，且图是纯正的狼头徽记。
CARDBACK_SUBSTITUTE = {
    "Cardback_GOF_Warlord_Zagstruk": "Cardback_GOF_Ghazgkhull_AA_HB",
    "Cardback_SW_Wolfs Lair": "Cardback_SW_Feral Barbarity",
}

# 🔴 **已知「名字对得上、图本地没有」的卡背**（多出来任何一条 ⇒ 生成器断言会炸，回来看看）。
#    这些是 **GUID 查出来的 cosmetic 名与图名拼法不同、且没有整套证据**，所以**只记着、不接别名也不找替身**
#    （**没核实过，按红线不猜**）。`ASH_Warlord_Anvirr`（本地连相近的都没有）·
#    `ASH_Warlord_Eliac`（我们的是 `ASH_Presale_Eliac`）· `ASH_Warp Spiders`（我们的是单数 `Warp Spider`）·
#    `SW_Campaign_Free` / `SW_Campaign_Premium`（我们的是**空格**不是下划线）·
#    `TAU_Sphere of Expansion`（我们的是 `TAU_Campaign_Sphere of Expansion`）。
#    ⚠️ 这几条**只影响当前不显示的那些副**（练习池里的经典 29 副，卡背已全部能取到图）。
KNOWN_NO_ART = {
    "Cardback_ASH_Warlord_Anvirr", "Cardback_ASH_Warlord_Eliac", "Cardback_ASH_Warp Spiders",
    "Cardback_SW_Campaign_Free", "Cardback_SW_Campaign_Premium", "Cardback_TAU_Sphere of Expansion",
}
OUT = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json"

# 产物格式版本：字段有增减就 +1（C# 侧会读它，见 PrebuiltDecks.cs）
FORMAT = 1

# `CardArmy` 枚举值（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardArmy.cs`，10 一档）。
# 🔴 原版预组页的**第二排序键就是它**（`OrderBy(difficulty)` → `ThenBy(deckArmy)`，
#    `DeckSelectionTabController__Awake.c` / `__c___Awake_b__6_4.c`）——**不是字符串排序**，
#    所以这里落成数字，别让 C# 那边按阵营名字母序排（那会得到另一个顺序）。
ARMY_ORDER = {
    "Neutral": 0, "Ultramarines": 10, "Goff": 20, "SaimHann": 30, "Sautekh": 40,
    "BlackLegion": 50, "Leviathan": 60, "TauEmpire": 70, "Sororitas": 80,
    "Genestealers": 90, "AstraMilitarum": 100, "DarkAngels": 110,
    "EmperorsChildren": 120, "SpaceWolves": 130,
}


def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def norm(s):
    """归一化：小写 / 撇号连字符括号全去掉 / 去尾部独立数字 / 去尾 s（>4 字才去）。
    ⚠️ 与 `check_prebuilt_decks.py` 的 `norm()` 必须**逐字一致**。"""
    s = s.lower().replace("’", "").replace("'", "").replace("‘", "").replace("`", "").replace("´", "")
    s = re.sub(r"\([^)]*\)", " ", s)
    s = re.sub(r"\s+\d+$", "", s.strip())
    s = re.sub(r"[^a-z0-9]+", "", s)
    return s.rstrip("s") if len(s) > 4 else s


def build_matcher(pool, mapping):
    """把 `check_prebuilt_decks.py` 的 `match()` 原样搬过来，只是返回值多带一个「我们的卡 id」。"""
    exact = set()
    by_norm = defaultdict(dict)
    name_any = defaultdict(set)
    card_id = {}
    by_id = {c["id"]: c for c in pool}
    hero_ids = defaultdict(dict)          # faction -> 归一化督军名 -> 我们的 hero id
    for c in pool:
        key = (c["faction"], c["name"])
        exact.add(key)
        by_norm[c["faction"]].setdefault(norm(c["name"]), c["name"])
        name_any[norm(c["name"])].add(key)
        card_id.setdefault(key, c["id"])
        if c["type"] == "hero":
            hero_ids[c["faction"]][norm(c["name"])] = c["id"]
    claimed = set(mapping.values())
    cache = {}

    def match(cid, faction):
        """返回 (tier, 我们的卡 id or None)。与核对脚本同档位、同顺序。"""
        key = (cid, faction)
        if key in cache:
            return cache[key]
        nm = mapping.get(cid)
        res = ("miss-unknown", None)
        if nm is not None:
            res = ("miss-known", None)
            hit = None
            if (faction, nm) in exact:
                hit, res = (faction, nm), ("t1", None)
            elif norm(nm) in by_norm.get(faction, {}):
                hit, res = (faction, by_norm[faction][norm(nm)]), ("t2", None)
            else:
                n = norm(nm)
                for f2, nm2 in sorted(name_any.get(n, ())):
                    hit = (f2, nm2)
                    break
                if hit is None and n in by_norm.get(faction, {}):
                    hit = (faction, by_norm[faction][n])
                if hit is not None:
                    res = ("t3", None)
                else:
                    # t4/t5 是弱档：只有池中那个名字**没被别的 id 认领**时才算（防 BL2 误配 BL26）
                    def free(v):
                        return v not in claimed
                    toks = set(re.findall(r"[a-z0-9]+", nm.lower()))
                    for k, v in sorted(by_norm.get(faction, {}).items()):
                        if not free(v):
                            continue
                        if (len(n) >= 6 and len(k) >= 6 and (n in k or k in n)) or (
                                len(toks) >= 2 and toks == set(re.findall(r"[a-z0-9]+", v.lower()))):
                            hit = (faction, v)
                            break
                    if hit is not None:
                        res = ("t5", None)
                    else:
                        cands = [(difflib.SequenceMatcher(None, n, k).ratio(), v)
                                 for k, v in by_norm.get(faction, {}).items()
                                 if free(v) and difflib.SequenceMatcher(None, n, k).ratio() >= 0.88]
                        if cands:
                            hit, res = (faction, max(cands)[1]), ("t4", None)
            if hit is not None:
                res = (res[0], card_id.get(hit))
        cache[key] = res
        return res

    def hero_match(hid, faction):
        """返回 (我们的 hero id or None, 说明)。
        🔴 **先按 id 直查**（我们的卡池里 973/1126 张就是原版 id，督军同理），查不到再回落名字表 ——
           2026-09-25 实测：`warlord_ids.json` 那张自建名表**有错**
           （`TL74` 写成 `TL_Neurogaunt`，而卡池里 `TL74` 就是督军 `Terror of Vardenghast`），
           拿它当第一判据会把能用的牌误判成不能用。"""
        c = by_id.get(hid)
        if c is not None and c["type"] == "hero":
            return c["id"], "id命中"
        v = (warlord or {}).get(hid)
        if v is None:
            return None, "查不到"
        short = re.sub(r"^[A-Za-z]{2,3}_(Warlord_|Presale_)?", "", v)
        short = re.sub(r"^Presale_", "", short)
        s = norm(short)
        table = hero_ids.get(faction, {})
        if s in table:
            return table[s], "名字命中"
        for h, cid in sorted(table.items()):
            if s in h or h in s:
                return cid, "疑似(子串)"
        best = [(difflib.SequenceMatcher(None, s, h).ratio(), cid) for h, cid in table.items()]
        if best and max(best)[0] >= 0.8:
            return max(best)[1], "疑似(近似)"
        return None, "对不上" if c is None else ("id指向非督军(%s)" % c["type"])

    warlord = load(os.path.join(D, "warlord_ids.json"))
    return match, hero_match


def load_cardback_guid_map():
    """GUID（`cardbackId`）→ 卡背名。**只读** `d:/2`。表大小实测 243。"""
    import glob
    m = {}
    for p in glob.glob(CARDBACK_SO):
        try:
            j = load(p)
        except Exception:
            continue
        u, n = j.get("uniqueId"), j.get("m_Name")
        if u and n:
            m[u] = n
    return m


def pick_defensive(pool):
    """每个阵营挑一张防御卡 —— **这是我们的选择，不是原版的做法**。
    🔴 **原版预组牌没有防御卡**（已用反汇编证实：`CardDeck(PrebuiltDeck,…)` 调
       `DeckBasicSetup(this, name, hero, R9=0, [rsp+0x20]=0, …)` ⇒ `pDeckId` 与 `defensiveCard` 都是 0）。
       用户 2026-09-26 拍板：「人机对战应该有防御卡，这些预组卡组我们可以加入防御卡，由你选择加入」。
    **规则（可复现、可复算）：本阵营 `type == "defence"` 里 `cost` 最低 → `id` 最小。**
    ⚠️ 判据全在 `资料/预组卡组_原版规格.md` §五之七。"""
    by = defaultdict(list)
    for c in pool:
        if c["type"] == "defence":
            by[c["faction"]].append(c)
    return {f: sorted(cs, key=lambda c: (c["cost"], c["id"]))[0] for f, cs in by.items()}


def load_have_cardbacks():
    """我们工程 `Art/cardbacks/` 里实际有的卡背名（不含扩展名）。"""
    import glob
    return {os.path.splitext(os.path.basename(p))[0] for p in glob.glob(CARDBACK_DIR + "/*.png")}


def main():
    decks = load(os.path.join(D, "decklists.json"))["decklists"]
    mapping = load(os.path.join(D, "card_ids.json"))["mapping"]
    pool = load(POOL)["cards"]
    zh_names = load(ZH)["names"]
    gmap = load_cardback_guid_map()
    have_cb = load_have_cardbacks()
    defpick = pick_defensive(pool)
    match, hero_match = build_matcher(pool, mapping)

    rarity = {(c["faction"], c["name"]): c["rarity"] for c in pool}
    by_id = {c["id"]: c for c in pool}

    out_decks, stats = [], Counter()
    for d in decks:
        # 🔴 只取练习池（预组页签那一池）。教程副全是 isPractice==0 ⇒ 结构性排除。
        if d.get("isPractice") != 1:
            continue
        fac = d["faction"]
        # 🔴 **按 cardIds 的原始顺序逐张解析**，不重排、不分组 —— 万一将来要收教程那批，
        #    牌序就是教学脚本赖以成立的东西（见 资料/教程线_原版规格与资源存量.md §一）。
        our_ids, missing, tiers = [], [], Counter()
        for cid in d["cardIds"]:
            tier, our = match(cid, fac)
            tiers[tier] += 1
            if our is None:
                if not missing or missing[-1]["origId"] != cid:
                    missing.append({"origId": cid, "name": mapping.get(cid), "count": 0, "tier": tier})
                missing[-1]["count"] += 1
            else:
                our_ids.append(our)
        hero_id, hero_note = hero_match(d["heroId"], fac)
        # 卡背：`cardback` 列直接是文件名（去 `res://` 前缀）；**那一列空的就用 GUID 查表**
        #（实测 236 副里 31 副那一列是空的，但 GUID 一直都有 ⇒ 这一查把名字补全了）。
        raw = d.get("cardback") or ""
        if raw.endswith(".png"):
            cb, cb_from = raw.rsplit("/", 1)[-1][:-4], "column"
        else:
            cb = gmap.get(d.get("cardbackId") or "")
            cb_from = "guid" if cb else "none"
        # 名字对上了、但我们手上没这张图 ⇒ ① 先试**别名**（有整套证据的）② 再试**替身**（用户授权的同阵营替换）
        cb_orig = cb or ""
        if cb and cb not in have_cb and cb in CARDBACK_ALIAS:
            cb, cb_from = CARDBACK_ALIAS[cb], "alias"
        if cb and cb not in have_cb and cb in CARDBACK_SUBSTITUTE:
            cb, cb_from = CARDBACK_SUBSTITUTE[cb], "substitute"
        # 「能不能用」= 卡全解析 + 督军解析出（**与 `check_prebuilt_decks.py` 的 `full` 同口径**）。
        # 🔴 **不要拿「同名 ≤ 传说上限」当硬伤** —— 原版预组牌自己就不守规则书那条
        #    （128 副经典里有若干副装了 2 张同名传说卡，见 `资料/原版预组牌_核对.md`）；
        #    真正越线的是「同一张 ≥3」，那才是原版自己都守的线（只有教学副破）。
        problems = []
        need = 12 if d.get("gameMode") == 13 else 30
        if len(our_ids) != need:
            problems.append("size:%d/%d" % (len(our_ids), need))
        over2 = ["%s x%d" % (cid, k) for cid, k in Counter(our_ids).items() if k > 2]
        if over2:
            problems.append("over2:" + ";".join(over2))
        # 口径差（**不是缺陷**）：我们标成传说、但原版预组放了 2 张 —— 记着，别拿来拦牌
        over_legendary = ["%s x%d (%s)" % (cid, k, by_id[cid]["name"])
                          for cid, k in Counter(our_ids).items()
                          if k > 1 and by_id[cid]["rarity"] == "legendary"]
        complete = not missing and hero_id is not None and not problems
        # 🔴 产物形状必须让 `JsonUtility` 读得动：**只有 string / int / bool / 数组**，
        #    不能有字典（`CardIcons.cs:57` 那条注释记的就是这个坑）。
        out_decks.append(dict(
            deckId=d["deckId"], name=d["name"], nameZh=zh_names.get(d["name"], ""),
            faction=fac, armyOrder=ARMY_ORDER[fac],
            gameMode=d.get("gameMode") or 0, difficulty=d.get("difficulty") or 0,
            heroId=hero_id or "", heroNote=hero_note, cardback=cb or "", cardbackFrom=cb_from,
            cardbackOrig=cb_orig,
            defensiveId=defpick[fac]["id"], defensiveName=defpick[fac]["name"],
            defensiveNameZh=defpick[fac]["nameZh"],
            cardIds=our_ids, complete=complete,
            missing=["%s x%d (%s)" % (m["origId"], m["count"], m["tier"]) for m in missing],
            problems=problems, overLegendary=over_legendary,
        ))
        stats["total"] += 1
        stats["complete"] += 1 if complete else 0
        stats["classic"] += 1 if d.get("gameMode") == 0 else 0
        stats["classic_complete"] += 1 if (d.get("gameMode") == 0 and complete) else 0
        stats["no_cardback"] += 1 if cb is None else 0
        stats["hero_missing"] += 1 if hero_id is None else 0

    # ---- 断言（宁可炸掉，也不悄悄产出错数据）----------------------------------
    assert stats["total"] == 103, "练习池应是 103 副，实际 %d" % stats["total"]
    for x in out_decks:
        assert "tutorial" not in x["deckId"].lower(), "教程副混进来了：%s" % x["deckId"]
        assert "tutorial" not in x["name"].lower(), "教程副混进来了：%s" % x["name"]
    dupes = [k for k, v in Counter(x["deckId"] for x in out_decks).items() if v > 1]
    assert not dupes, "deckId 重名：%s" % dupes
    no_zh = sorted({x["name"] for x in out_decks if not x["nameZh"]})
    assert not no_zh, "这些卡组名还没有中文（补 数据/卡牌翻译/zh_decks.json）：%s" % no_zh
    unknown = sorted({d["faction"] for d in decks if d.get("isPractice") == 1} - set(ARMY_ORDER))
    assert not unknown, "这些阵营不在 CardArmy 枚举里（排序会错）：%s" % unknown
    no_def = sorted({d["faction"] for d in decks if d.get("isPractice") == 1} - set(defpick))
    assert not no_def, "这些阵营没有防御卡可挑（补卡池或改规则）：%s" % no_def
    missing_art = {x["cardback"] for x in out_decks if x["cardback"] and x["cardback"] not in have_cb}
    assert missing_art <= KNOWN_NO_ART, "多出没图的卡背：%s" % sorted(missing_art - KNOWN_NO_ART)

    doc = dict(
        format=FORMAT,
        note="由 工具/gen_prebuilt_decks.py 生成，**别手改**。匹配规则与 工具/check_prebuilt_decks.py 同源。"
             "🔴 卡池或 id 表变了 ⇒ 必须重跑生成器（与 cards_engine.json 一样是构建步骤）。",
        pool="isPractice==1 的 103 副（预组页签那一池）；教程副（isPractice==0）**不在其中、也没被改过**。",
        deckCount=len(out_decks),
        decks=out_decks,
    )
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(doc, f, ensure_ascii=False, indent=1)

    # 控制台用 ASCII，别打 emoji（Windows 控制台 GBK）
    print("wrote", OUT)
    print("practice=%d complete=%d classic=%d classic_complete=%d no_cardback=%d hero_missing=%d"
          % (stats["total"], stats["complete"], stats["classic"],
             stats["classic_complete"], stats["no_cardback"], stats["hero_missing"]))
    bad = [x for x in out_decks if not x["complete"]]
    print("not complete = %d" % len(bad))
    for x in bad[:12]:
        why = []
        if x["missing"]:
            why.append("缺%d种" % len(x["missing"]))
        if x["heroId"] is None:
            why.append("督军" + x["heroNote"])
        if x["problems"]:
            why.append(";".join(x["problems"][:2]))
        print("   %-24s gm=%-3s diff=%-3s %s" % (x["deckId"], x["gameMode"], x["difficulty"], " / ".join(why)))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
