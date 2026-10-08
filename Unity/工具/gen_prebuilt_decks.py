# -*- coding: utf-8 -*-
"""把原版「预组卡组」解析成我们卡池的 id，产出一份**旁挂数据**给运行时读。

跑法：
    python d:/4/Unity/工具/gen_prebuilt_decks.py

产出（覆盖写，可重跑）：
    d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json   预组页那一池（103 副）
    d:/4/Unity/MyGame/Assets/RuleEngine/Resources/tutorial_decks.json   🆕 教程 6 关那 12 副（B6 · 2026-10-17）

🔴 **为什么是【两个文件】而不是给 103 副那张表加标记**（2026-10-17 决定，理由可复核）：
   ① **原版自己就是分开的两条路** —— `PrebuiltDeckCollection.AddDeck` 对**资产名含 `"Tutorial"` 的直接
      `return`**（`decomp_full/PrebuiltDeckCollection__AddDeck.c:21-25`），教程那 12 副**从不进任何集合**，
      而是由 **`TutorialStage.playerDeck/aiDeck` 两个 PPtr 直引**（同文件；另见
      `资料/预组卡组_原版规格.md:522-523`）。⇒ 我们照原版把「池」与「教程直引」分成两份产物，
      结构上就不可能互相污染。
   ② **硬约束**：预组页现在列出的那 66 副（103 副里 `complete` 的）**一个字符都不许动**。分开文件 ⇒
      `prebuilt_decks.json` 的 diff 可以要求**逐字节为 0**（`git diff --numstat` 一看就知道；
      本脚本为此**连 `note`/`pool` 那两句都不改**，免得头两行抖一下就说不清了）。

🔴 判据只有一份：匹配规则**原样**搬自 `工具/check_prebuilt_decks.py` 的 `match()` / `hero_check()`
   —— 那边是「核对报告」，这边是「生产产物」。**改规则必须两边一起改**（或者以后让本脚本 import 它）。
🔴 **本脚本不重排、不补牌、不改任何原版数据** —— 只做「原版 id → 我们的 id」的解析。
   两路都**逐张按 `cardIds` 的原始顺序**解析、**绝不重排** —— 教程牌的牌序是教学脚本赖以成立的东西。

输入：
    Unity/数据/游戏数据/decklists.json        236 副（预组池取 isPractice==1 的 103 副）
    Unity/数据/游戏数据/card_ids.json         原版卡 id -> 英文卡名
    Unity/数据/游戏数据/warlord_ids.json      督军 id -> 督军名
    MyGame/Assets/RuleEngine/Resources/cards_engine.json   我们的卡池 1126 张
    d:/2/新解包资源/…/bundle_prebuiltdecks_assets_all/MonoBehaviour/*_Deck0_Tutorial*.json  12 副教程牌 SO

🔴 **2026-10-17 数据订正（写手 B16 · 改的是输入，不是本脚本）**：`decklists.json` 里 `UMTutorial`
   **S1 玩家侧那一副**（`heroId = ec0d25faeb266ea4eaec41ae72b8d8ad`）原来写着
   `faction=EmperorsChildren` / `factionId=120` / `armyId=120` / `heroPortrait=…Lucius.png` —— **四个字段都不对**，
   是**导出端**的老 bug：旧 Godot 工程 `d:/warpforge/tools/export_game_data.py:73 faction_of_deck()`
   拿 `heroId` 的**头两个字母**当阵营前缀（`PREF2['EC']`），而这一条 `heroId` 是 **32 位十六进制 GUID**
   `ec0d25fa…` ⇒ `EC` 命中「EmperorsChildren」。**原版 id 是「字母+纯数字」（`EC56`）**，GUID 不该走那条路；
   deckId 兜底（`UMTutorial` → alias `ultramarine`）本来会给对 Ultramarines，只是没轮到。
   判据（三条，互相独立）：① 教程 SO 资产名 `Ultramarines_Deck0_Tutorial1_Uriel`（原版包里的 `m_Name`）；
   ② `warlord_ids.json` 说该 GUID = `UM_Warlord_Uriel Ventris`；③ 我们卡池里该督军是 `UM3 Uriel Ventris`（Ultramarines）。
   已按 `Ultramarines` / `10` 订正四个字段（`heroPortrait` 是导出器按**阵营**推的，见 `faction_defaults`；
   实测 236 副里每个阵营的 `heroPortrait` 只有 1 种取值，所以它跟着 faction 走）。
   ⚠️ **旧 Godot 工程那个脚本不在本仓、没改** —— 若以后重跑它导 `decklists.json`，这 4 个字段会被打回去。
   实测 236 副里**只有这一条**踩到（其余 GUID heroId 的阵营都对得上）。
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
    # 🆕 2026-09-26：**另外 5 条同族**（原来记在 `KNOWN_NO_ART` 里「只影响不显示的副」——
    #   现在遭遇模式那批**也列出来了**，于是它们露了出来，必须接上）。
    #   每一条都**逐条核过唯一性**（在 `Resources/Art/cardbacks/` 那 233 张里数出来的），
    #   而且命名规律就是正本 §六 第 2 项已经记过的那几条 —— **不是猜**：
    "Cardback_ASH_Warlord_Eliac":       "Cardback_ASH_Presale_Eliac",   # `Presale_` 前缀（ASH 里只有这一个 Eliac）
    "Cardback_ASH_Warp Spiders":        "Cardback_ASH_Warp Spider",     # 单复数（唯一）
    "Cardback_SW_Campaign_Free":        "Cardback_SW_Campaign Free",    # 下划线 ↔ 空格（唯一）
    "Cardback_SW_Campaign_Premium":     "Cardback_SW_Campaign Premium",  # 下划线 ↔ 空格（唯一）
    "Cardback_TAU_Sphere of Expansion": "Cardback_TAU_Campaign_Sphere of Expansion",  # 多一段 `Campaign_`（唯一）
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
    # 🆕 2026-09-26：`ASH_SK_2` 那张**本地四个来源都没有、连相近的都没有**
    #   （ASH 阵营的卡背里只有 `Cardback_ASH_Warlord_Ghaelyn` 是督军主题）⇒ 按用户 2026-09-26
    #   那条裁决「从它们阵营里选一个替代」办：**取该阵营唯一另一张督军主题的**（与 `OrksDeck2` 同一条选法）。
    "Cardback_ASH_Warlord_Anvirr": "Cardback_ASH_Warlord_Ghaelyn",
}

# 🔴 **已知「名字对得上、图本地没有」的卡背**（多出来任何一条 ⇒ 生成器断言会炸，回来看看）。
#    ✅ **2026-09-26：清空了** —— 原来那 6 条里 5 条按命名规律接上了别名、1 条找了同阵营替身
#       （见上面两张表）。当时留着的理由写的是「**只影响当前不显示的那些副**」，
#       而遭遇模式那批现在**也列出来了** ⇒ 那条理由不再成立，必须全部解决。
#    ⚠️ **这张表要一直空着**：它非空就说明有副牌的卡背画不出来 —— 而那份名单已经会显示给玩家了。
KNOWN_NO_ART = set()
OUT = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json"

# 产物格式版本：字段有增减就 +1（C# 侧会读它，见 PrebuiltDecks.cs）
FORMAT = 1

# ============================================================ 教程 6 关那 12 副（B6 · 2026-10-17）
# 教程牌 SO（**只读** `d:/2`，铁律 2）。这 12 个文件的 `m_Name` 就是 `…_Deck0_Tutorial{N}_{督军}`。
TUT_SO_DIR = "d:/2/新解包资源/assets_full/bundle_prebuiltdecks_assets_all/MonoBehaviour"
TUT_OUT = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/tutorial_decks.json"

# 🔴 **哪副是玩家、哪副是敌方** —— 判据是**关卡 SO 自己那两个 PPtr**，不是猜的。
#    `bundle_tutorialso_assets_all/MonoBehaviour/Warpforge_TutorialStage{1..6}.json` 里
#    `playerDeck` / `aiDeck` 两个引用都写着 `{m_FileID: 2, m_PathID: <pid>}`（`m_FileID: 2` = 该 SO 自己
#    所在 CAB 的 externals[1]，指向 `prebuiltdecks_assets_all.bundle`）。**2026-10-17 实测**：
#    用 UnityPy 打开 `D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/
#    prebuiltdecks_assets_all.bundle`（包内只有一份 CAB `CAB-dbb6f14c119af496b058533ae98dc790`），
#    按 pid 12 个**全部解出**，名字如下表（每关两条 pid **原样抄在下面**，可复算）：
#      Stage1 player 7422324008993234997 → Ultramarines_Deck0_Tutorial1_Uriel
#             ai     673972951073179591  → Ultramarines_Deck0_Tutorial1_Marneus
#      Stage2 player -8933306869251456372 → Orks_Deck0_Tutorial2_Ghazghkull
#             ai     7861926528340494372  → Ultramarines_Deck0_Tutorial2_Uriel
#      Stage3 player 8840064364143669265  → Sautekh_Deck0_Tutorial3_Zahndrekh
#             ai     4394258514378169129  → Sautekh_Deck0_Tutorial3_Orikan
#      Stage4 player -6553397148531013743 → BlackLegion_Deck0_Tutorial4_Sylar
#             ai     1818928420836408829  → Ultramarines_Deck0_Tutorial4_Varro
#      Stage5 player -8984653494689495507 → Aeldari_Deck0_Tutorial5_Ghaelyn
#             ai     4632543796033614790  → Sautekh_Deck0_Tutorial5_Imotekh
#      Stage6 player -7989913330245200695 → Leviathan_Deck0_Tutorial6_Tervigon
#             ai     4000797182673508289  → Ultramarines_Deck0_Tutorial6_Uriel
#    ⇒ 表以 **SO 的 `m_Name`** 为键（不拿 pid / 数组下标当键 —— 那两个都不可读，错了看不出）。
TUT_ROLE = {
    1: ("Ultramarines_Deck0_Tutorial1_Uriel",   "Ultramarines_Deck0_Tutorial1_Marneus"),
    2: ("Orks_Deck0_Tutorial2_Ghazghkull",      "Ultramarines_Deck0_Tutorial2_Uriel"),
    3: ("Sautekh_Deck0_Tutorial3_Zahndrekh",    "Sautekh_Deck0_Tutorial3_Orikan"),
    4: ("BlackLegion_Deck0_Tutorial4_Sylar",    "Ultramarines_Deck0_Tutorial4_Varro"),
    5: ("Aeldari_Deck0_Tutorial5_Ghaelyn",      "Sautekh_Deck0_Tutorial5_Imotekh"),
    6: ("Leviathan_Deck0_Tutorial6_Tervigon",   "Ultramarines_Deck0_Tutorial6_Uriel"),
}

# 🔴 **教程那一路的督军裁定**（2026-10-17 调度台拍板）—— `{牌组资产名: (我们池里的督军 id, 依据)}`。
#    **以【原版 SO 的牌组资产名尾段】为准**。理由：**我们自己的两张自建表互相矛盾** ——
#      · `数据/游戏数据/card_ids.json` 的 `mapping` 说 `ASH3 = Eliac Zephyrblade`（我们池里 ASH3 也是 Eliac）
#      · `数据/游戏数据/warlord_ids.json` 说 `ASH3 = ASH_Warlord_Medreyal Ghaelyn`
#    而 SO 资产名 `Aeldari_Deck0_Tutorial5_Ghaelyn` 的尾段是 `Ghaelyn`（池里 = `ASH5` Medreyal Ghaelyn）
#    ⇒ 以 SO 为准，取 `ASH5`。
#    ⚠️ **只改教程这一路解出来的结果**：那三张数据表**一个都没动** —— 表之间的矛盾是**另一件账**（A876）。
#    ⚠️ 也**不要**把这条搬进 `hero_match()`：池那一路（103 副）走的是同一个函数，搬进去会让
#       `prebuilt_decks.json` 跟着变（那份要求逐字节不变）。覆盖只在本文件的 `build_tutorial` 里做。
#    ⚠️ 表里每一条都会**当场核**「目标督军的名字里真的含 SO 尾段」—— 核不上就抛，⛔ 不会静默换一个"看着像"的。
TUT_HERO_BY_SO = {
    "Aeldari_Deck0_Tutorial5_Ghaelyn": ("ASH5", "SO 资产名尾段 Ghaelyn ⇒ ASH5 Medreyal Ghaelyn（2026-10-17 裁定）"),
}

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
    ⚠️ 判据全在 `资料/预组卡组_原版规格.md` §五之七。

    🔴🔴 **2026-10-19（`A1070`）补一句口径：这个 `defensiveId` **只有玩家侧会读**。**
      电脑（AI）那一方原版**从不从卡组取**这张 —— 电脑后手时原版走
      `BattleManager.AddGoesSecondCardToDeck` 的另两条（② `matchType ∈ {50,80,110,120}` ⇒ 后手方阵营
      防御池随机；③ 其余 AI 模式 ⇒ 督军自己的 `goSecondCardInHand`），引擎侧对应
      `RuleCore.GoesSecondCard`（**它会丢掉电脑侧分流出来的这张**）。
      ⇒ **别删这个字段**（玩家也可能用一副预组牌，那时它就是玩家那一侧的那张），但
      ⛔ **别把它当成「电脑也有防御卡」的依据** —— 那正是 `A1070` 修掉的那份「看起来能用」。
    """
    by = defaultdict(list)
    for c in pool:
        if c["type"] == "defence":
            by[c["faction"]].append(c)
    return {f: sorted(cs, key=lambda c: (c["cost"], c["id"]))[0] for f, cs in by.items()}


def load_have_cardbacks():
    """我们工程 `Art/cardbacks/` 里实际有的卡背名（不含扩展名）。"""
    import glob
    return {os.path.splitext(os.path.basename(p))[0] for p in glob.glob(CARDBACK_DIR + "/*.png")}


# ============================== 两条输出路共用的解析（**产物形状只此一处**）==============================
def resolve_cards(d, ctx):
    """**按 `cardIds` 的原始顺序**逐张解析 → `(our_ids, missing)`（不重排、不补牌、不分组）。

    🔴 顺序不能动：教程牌的**牌序就是教学脚本赖以成立的东西**
       （`资料/教程线_原版规格与资源存量.md` §一 · 本文件原来的注释也写着同一句）。
    `missing` 保留原版「同 id 相邻则合并计数」的写法。
    """
    fac = d["faction"]
    our_ids, missing = [], []
    for cid in d["cardIds"]:
        tier, our = ctx["match"](cid, fac)
        if our is None:
            if not missing or missing[-1]["origId"] != cid:
                missing.append({"origId": cid, "name": ctx["mapping"].get(cid), "count": 0, "tier": tier})
            missing[-1]["count"] += 1
        else:
            our_ids.append(our)
    return our_ids, missing


def resolve_cardback(d, ctx):
    """→ `(卡背名 | None, 来源, 原版卡背名)`。
    `cardback` 列直接是文件名（去 `res://` 前缀）；**那一列空的就用 GUID 查表**
    （实测 236 副里 31 副那一列是空的，但 GUID 一直都有 ⇒ 这一查把名字补全了）。
    名字对上了、但我们手上没这张图 ⇒ ① 先试**别名**（有整套证据的）② 再试**替身**（用户授权的同阵营替换）。
    """
    raw = d.get("cardback") or ""
    if raw.endswith(".png"):
        cb, cb_from = raw.rsplit("/", 1)[-1][:-4], "column"
    else:
        cb = ctx["gmap"].get(d.get("cardbackId") or "")
        cb_from = "guid" if cb else "none"
    cb_orig = cb or ""
    if cb and cb not in ctx["have_cb"] and cb in CARDBACK_ALIAS:
        cb, cb_from = CARDBACK_ALIAS[cb], "alias"
    if cb and cb not in ctx["have_cb"] and cb in CARDBACK_SUBSTITUTE:
        cb, cb_from = CARDBACK_SUBSTITUTE[cb], "substitute"
    return cb, cb_from, cb_orig


def make_entry(d, ctx, kind):
    """把 `decklists.json` 的一条记录解析成产物对象。

    🔴 **预组池（103 副）与教程 12 副共用这一份** —— 产物形状只此一处（两处写同一条 = 迟早不一致）。
    `kind` 两档：
      · `"practice"` = 预组页那一池。张数按池的口径卡（经典 30 / 遭遇 12）；`complete` 另要求 `problems` 空。
      · `"tutorial"` = 教程 6 关那 12 副。🔴 **不卡张数、也不卡「同名 ≤2」** —— 教程牌是**手工编的**
        （`资料/预组卡组_原版规格.md:286`：「破格的 5 副全是教程副」），原版自己就是 **20 / 25 / 30 张**不等，
        所以 `problems` 只当情报印出来、不进 `complete`。`complete` = **卡全解析出 + 督军解析出**。
    🔴 **防御卡**：原版 `CardDeck(PrebuiltDeck,…)` 那条 ctor 的 `defensiveCard` 恒 0
       （`资料/预组卡组_原版规格.md:350`「原版预组牌都不带防御卡」）⇒
       池那一路那三格是**我们加的**（用户 2026-09-26 拍板）；**教程那一路不加**、三格留空串。
       🔴🆕 **2026-10-19（`A1070`）**：这三格**只有玩家侧会读**（电脑侧原版从不从卡组取）——
       口径只写在 `pick_defensive()` 的 docstring 里，⛔ 别在这儿再抄一份。
    """
    fac = d["faction"]
    our_ids, missing = resolve_cards(d, ctx)
    hero_id, hero_note = ctx["hero_match"](d["heroId"], fac)
    cb, cb_from, cb_orig = resolve_cardback(d, ctx)
    by_id = ctx["by_id"]

    problems = []
    if kind == "practice":
        # 🔴 **不要拿「同名 ≤ 传说上限」当硬伤** —— 原版预组牌自己就不守规则书那条
        #    （128 副经典里有若干副装了 2 张同名传说卡，见 `资料/原版预组牌_核对.md`）；
        #    真正越线的是「同一张 ≥3」，那才是原版自己都守的线（只有教学副破）。
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
    if kind == "practice":
        complete = not missing and hero_id is not None and not problems
        def_id = ctx["defpick"][fac]["id"]
        def_name = ctx["defpick"][fac]["name"]
        def_zh = ctx["defpick"][fac]["nameZh"]
    else:
        complete = not missing and hero_id is not None
        def_id = def_name = def_zh = ""          # 🔴 原版教程牌没有防御卡（见 docstring）
    # 🔴 产物形状必须让 `JsonUtility` 读得动：**只有 string / int / bool / 数组**，
    #    不能有字典（`CardIcons.cs:57` 那条注释记的就是这个坑）。
    return dict(
        deckId=d["deckId"], name=d["name"], nameZh=ctx["zh_names"].get(d["name"], ""),
        faction=fac, armyOrder=ARMY_ORDER[fac],
        gameMode=d.get("gameMode") or 0, difficulty=d.get("difficulty") or 0,
        heroId=hero_id or "", heroNote=hero_note, cardback=cb or "", cardbackFrom=cb_from,
        cardbackOrig=cb_orig,
        defensiveId=def_id, defensiveName=def_name, defensiveNameZh=def_zh,
        cardIds=our_ids, complete=complete,
        missing=["%s x%d (%s)" % (m["origId"], m["count"], m["tier"]) for m in missing],
        problems=problems, overLegendary=over_legendary,
    )


def load_tutorial_sos():
    """读 12 副教程牌 SO（`*_Deck0_Tutorial*.json`，**只读** `d:/2`）。返回 `{m_Name: SO}`。

    ⚠️ 这批 SO 的 `cardLibraryIds` 是**原版直引**、`deckHero.targetId` 是我们现成 id 表里的督军 id
       ⇒ 拿它当**联立键**去 `decklists.json` 里认人（下面 `build_tutorial` 的②）。
    """
    import glob
    out = {}
    for p in sorted(glob.glob(os.path.join(TUT_SO_DIR, "*_Deck0_Tutorial*.json"))):
        j = load(p)
        out[j["m_Name"]] = j
    return out


def build_tutorial(ctx, decks):
    """教程 6 关 × (玩家 / 敌方) = 12 副 —— 产出 `tutorial_decks.json`。

    判据链（每一步都可复算，**对不上就炸**，绝不静默给一份半成品）：
      ① 12 个 SO 的 `m_Name` 与 `TUT_ROLE` 表**逐个对上**（多一个、少一个都炸）；
      ② SO → `decklists.json` 那条记录：按 `(deckId, heroId, 牌张数)` 三键联立 **+ `cardIds` 多重集相等**
         必须**恰好命中 1 条**；
      ③ 联立出的 12 条必须**恰好等于** `decklists` 里 `deckId` 含 "tutorial" 的那 12 条（含重名的那 7 个）。
    返回 `(doc, 命中到的那些 decklists 记录)`。
    """
    sos = load_tutorial_sos()
    want = {n for pair in TUT_ROLE.values() for n in pair}
    assert len(want) == 12, "TUT_ROLE 表里应是 12 个互不相同的 SO 名，实际 %d" % len(want)
    assert set(sos) == want, ("教程牌 SO 与 TUT_ROLE 表对不上：\n  多出 %s\n  缺少 %s"
                              % (sorted(set(sos) - want), sorted(want - set(sos))))

    name2idx = {}
    for i, d in enumerate(decks):
        name2idx.setdefault((d["deckId"], d["heroId"], len(d["cardIds"])), []).append(i)

    stages, used, bad = [], [], []
    # 督军对账：SO 的资产名尾段就是**这一关该用哪个督军**（`{Army}_Deck0_Tutorial{N}_{督军}`）。
    # 拿它去核「解出来的督军卡名」——**只对账、不改**（解不出/对不上照样按 matcher 的结果落盘，
    # 但**必须出声**：那是数据缺陷，⛔ 不许静默用一个看着像的督军顶上去）。
    hero_mismatch = []
    for stage in sorted(TUT_ROLE):
        row = dict(stage=stage)
        for role, so_name in zip(("player", "ai"), TUT_ROLE[stage]):
            so = sos[so_name]
            key = (so["deckId"], so["deckHero"]["targetId"], len(so["cardLibraryIds"]))
            cands = name2idx.get(key, [])
            # 三个键还可能与同名同长的另一条撞 ⇒ 再比一次 `cardIds` 多重集（顺序可能不同，比多重集）
            cands = [i for i in cands
                     if Counter(decks[i]["cardIds"]) == Counter(so["cardLibraryIds"])]
            assert len(cands) == 1, ("教程 SO `%s` 在 decklists.json 里应恰好联立出 1 条，实际 %d 条"
                                     "（键 deckId=%s heroId=%s n=%d）"
                                     % (so_name, len(cands), key[0], key[1], key[2]))
            used.append(cands[0])
            e = make_entry(decks[cands[0]], ctx, "tutorial")
            e["so"] = so_name                      # 坐标：原版那个 SO 资产名（可复查）
            # ⚠️ `nameZh` 这 12 副**全是空串**（`zh_decks.json` 没有它们的条目）—— **这是有意的**：
            #    教程窗上不显示 `deckId`/卡组名（它显示的是关卡标题，来自 `DemoDeckInfoSO` + 远端词条表），
            #    所以这里**没有**像预组池那样加「卡组名必须有中文」的断言，也**不编**一个译名。
            row[role] = e
            # 资产名尾段 vs 解出的督军名（归一化后子串）
            want_hero = so_name.rsplit("_", 1)[-1]
            # 🔴 教程那一路的督军裁定（表在上面的 `TUT_HERO_BY_SO`，理由也写在那里）。
            #    核不上就抛 —— 覆盖只允许覆盖成"名字真的含 SO 尾段"的那一张。
            fix = TUT_HERO_BY_SO.get(so_name)
            if fix is not None:
                fix_id, why = fix
                c = ctx["by_id"].get(fix_id)
                assert c is not None and c["type"] == "hero", "TUT_HERO_BY_SO 指的 %s 不是督军" % fix_id
                assert norm(want_hero) in norm(c["name"]), (
                    "TUT_HERO_BY_SO[%s] 指的 %s（%s）对不上 SO 尾段 %r"
                    % (so_name, fix_id, c["name"], want_hero))
                e["heroId"], e["heroNote"] = fix_id, "SO尾段裁定（%s）" % why
            got = ctx["by_id"].get(e["heroId"], {}).get("name", "")
            if norm(want_hero) not in norm(got or ""):
                hero_mismatch.append(
                    "stage%d %s：SO `%s` 尾段「%s」，但解出的督军是「%s」"
                    "（decklists heroId=%s · matcher 注=%s）"
                    % (stage, role, so_name, want_hero, got or "（没解出）", e["heroId"] or "（空）",
                       e["heroNote"]))
            if not e["complete"]:
                bad.append((stage, role, so_name, len(e["cardIds"]),
                            len([s for s in e["missing"]])))
        stages.append(row)


    # ③ 12 条必须正好是 deckId 含 "tutorial" 的那 12 条
    tut_idx = {i for i, d in enumerate(decks) if "tutorial" in (d.get("deckId") or "").lower()}
    assert len(tut_idx) == 12, "decklists 里含 tutorial 的应是 12 条，实际 %d" % len(tut_idx)
    assert set(used) == tut_idx and len(set(used)) == 12, (
        "联立出的 12 条与「deckId 含 tutorial」的 12 条对不上：\n  少 %s\n  多 %s"
        % (sorted(tut_idx - set(used)), sorted(set(used) - tut_idx)))

    doc = dict(
        format=FORMAT,
        note="由 工具/gen_prebuilt_decks.py 生成，**别手改**。匹配规则与 工具/check_prebuilt_decks.py 同源。"
             "🔴 卡池或 id 表变了 ⇒ 必须重跑生成器。",
        pool="教程 6 关 × (玩家/敌方) = 12 副（原版 SO `*_Deck0_Tutorial{1..6}_*`，deckId 含 Tutorial）。"
             "🔴 **与 `prebuilt_decks.json` 的那一池（isPractice==1 的 103 副）是两份产物** —— "
             "原版自己也分两条路：`PrebuiltDeckCollection.AddDeck` 对含 `Tutorial` 的名字直接 return"
             "（`decomp_full/PrebuiltDeckCollection__AddDeck.c:21-25`），教程那 12 副由 "
             "`TutorialStage.playerDeck/aiDeck` 直引。",
        roleSource="关卡 SO 的 `playerDeck`/`aiDeck` 两个 PPtr 解出来的（判据与 pid 抄在 "
                   "gen_prebuilt_decks.py 的 TUT_ROLE 注释里）—— **不是按名字猜的**。",
        incomplete="⚠️ `complete=false` 的副**不是本脚本的锅**：那 12 副引用了一批**原版 id 表里没有的卡号**"
                   "（`UM6/UM8/UM15/UM19/UM20/UM21/UM25/UM26/UM33/UM36/UM38…` 这类「连续编号里的洞」"
                   "与 32 位 GUID 卡）。本地**没有任何 id→名的表**能补（`资料/原版预组牌_核对.md` 已查证）"
                   "⇒ 如实标 `missing`，**不补牌、不猜名**。",
        heroMismatch="⚠️ 逐关对账：SO 资产名 `{Army}_Deck0_Tutorial{N}_{督军}` 的**尾段**与本副解出的督军卡名"
                     "对不上 —— 这是**数据缺陷**（在 `card_ids.json` / `decklists.json` 那一层），"
                     "本脚本**只对账、不改**（不替它挑一个「看着像」的督军）。空数组 = 12 副全对得上。",
        knownIssues=hero_mismatch,
        stageCount=len(stages),
        stages=stages,
    )
    return doc, used, bad, hero_mismatch


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

    # 两条输出路共用的只读表（**只建一次**；`make_entry` 只用这个 ctx）
    ctx = dict(match=match, hero_match=hero_match, mapping=mapping, by_id=by_id,
               zh_names=zh_names, gmap=gmap, have_cb=have_cb, defpick=defpick)

    out_decks, stats = [], Counter()
    for d in decks:
        # 🔴 只取练习池（预组页签那一池）。教程副全是 isPractice==0 ⇒ 结构性排除 ——
        #    **这正是原版的做法**：`PrebuiltDeckCollection.AddDeck` 对名字含 `Tutorial` 的直接 return
        #    （`decomp_full/PrebuiltDeckCollection__AddDeck.c:21-25`）。
        #    教程那 12 副走**另一份产物** `tutorial_decks.json`（见 `build_tutorial`）。
        if d.get("isPractice") != 1:
            continue
        # 🔴 逐张按 `cardIds` 的原始顺序解析，**不重排、不分组**（`resolve_cards` 的 docstring）。
        x = make_entry(d, ctx, "practice")
        out_decks.append(x)
        stats["total"] += 1
        stats["complete"] += 1 if x["complete"] else 0
        stats["classic"] += 1 if d.get("gameMode") == 0 else 0
        stats["classic_complete"] += 1 if (d.get("gameMode") == 0 and x["complete"]) else 0
        stats["no_cardback"] += 1 if not x["cardbackOrig"] else 0
        stats["hero_missing"] += 1 if not x["heroId"] else 0

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

    # ---- 教程那 12 副（**另一份产物**，见 `build_tutorial`）--------------------
    tut_doc, tut_used, tut_bad, tut_hero = build_tutorial(ctx, decks)
    with open(TUT_OUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(tut_doc, f, ensure_ascii=False, indent=1)

    # 控制台用 ASCII，别打 emoji（Windows 控制台 GBK）
    print("wrote", OUT)
    print("practice=%d complete=%d classic=%d classic_complete=%d no_cardback=%d hero_missing=%d"
          % (stats["total"], stats["complete"], stats["classic"],
             stats["classic_complete"], stats["no_cardback"], stats["hero_missing"]))
    print("wrote", TUT_OUT)
    print("tutorial stages=%d decks=%d complete=%d"
          % (tut_doc["stageCount"], len(tut_used),
             sum(1 for s in tut_doc["stages"] for r in ("player", "ai") if s[r]["complete"])))
    for (stage, role, so, n, nmiss) in tut_bad:
        print("   stage%d %-6s %-42s 卡 %2d 张 / 缺 %d 段（原版 id 表里没有，见产物 note）"
              % (stage, role, so, n, nmiss))
    print("tutorial heroMismatch = %d（>0 就是数据缺陷，见产物 knownIssues）" % len(tut_hero))
    for s in tut_hero:
        print("   " + s.encode("ascii", "backslashreplace").decode("ascii"))
    bad = [x for x in out_decks if not x["complete"]]
    print("not complete = %d" % len(bad))
    for x in bad[:12]:
        why = []
        if x["missing"]:
            why.append("缺%d种" % len(x["missing"]))
        if not x["heroId"]:
            why.append("督军" + x["heroNote"])
        if x["problems"]:
            why.append(";".join(x["problems"][:2]))
        print("   %-24s gm=%-3s diff=%-3s %s" % (x["deckId"], x["gameMode"], x["difficulty"], " / ".join(why)))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
