# -*- coding: utf-8 -*-
"""把原版「练习窗」那一列 `DemoDeckInfoSO`（一套两副：classic + skirmish）解成旁挂数据。

跑法（只读 `d:/2`，只写本脚本自己的产物）：
    D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/gen_demo_deck_pairs.py
    D:/2/Warpforge_tools/py312/python.exe -I -X utf8 d:/4/Unity/工具/gen_demo_deck_pairs.py --check

产出（覆盖写，可重跑，幂等）：
    d:/4/Unity/数据/游戏数据/demo_deck_pairs.json

============================================================ 判据（全部现场读原版包）
· `DemoDeckInfoSO` 的 MonoScript 类名对表：`Waprforge_monoscripts.bundle`
  （包内唯一 CAB = `CAB-17b1b5f865c595045b5fe42b7f8c8967`，722 个 MonoScript）。
  本 build 里 `DemoDeckInfoSO` 的 bundle 内 pathID = **-8258122028049581671**、
  `PracticeModePopup` = **580221229322215624**、`PrebuiltDeck` 与 `PrebuiltSortedDeck` 各自可查。
· 84 个 `DemoDeckInfoSO` 实例分在两个包：
    `menus_assets_all.bundle` 82 个 + `duplicateassetisolationso_assets_all.bundle` 2 个。
  ⚠️ 它们**也在** `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/` 的
  82 份 JSON 里（2026-10-19 界面线 P05 已用过那批）—— 本脚本**以真包为准**，
  只用 assets_full 做一次「名字集合是否一致」的旁证（读不到就跳过，不报错）。
· `classicDeck` / `skirmishDeck` 两个 PPtr 的 `m_FileID` 指向各自 SerializedFile 的
  externals；**必须解成 `CAB-dbb6f14c119af496b058533ae98dc790`**（= `prebuiltdecks_assets_all.bundle`
  的 CAB，236 个 `PrebuiltDeck`/`PrebuiltSortedDeck`）。脚本对这一条**硬断言**，
  然后**按 pathID 在目标包内取**（不是全库扫 pathID）。
· `armyIcon` 的 PPtr 必须解成 `CAB-159fa22599e5911bc9d76e52c875bd73`
  （= `armyicons_assets_all.bundle`，17 张 `40k_DeckSelection_icon_*` Sprite）。同样硬断言。
· 练习窗到底列哪 78 份：`menus_assets_all.bundle` 里 **`PracticeModePopup`** 组件
  （pathID = -5721221448436461764）的序列化 `decks` 数组**恰好 78 项**，
  `m_FileID=0`（同文件）且 78/78 全部命中一个 `DemoDeckInfoSO`。脚本把它采成 `uiOrder`
  并断言「这 78 份 == 两副齐全的那 78 份」。⇒ 「78」这个数是**数出来的**，不是抄来的。

============================================================ 与我们 103 副池的关系（本脚本的核心结论）
原版 78 对 = **156 个 deck 引用**（78 classic + 78 skirmish），且 **156 个 deckId 互不重复**。
其中**只有 78 个**落在我们的 `prebuilt_decks.json`（103 副 = 原版 `isPracticeDeck == 1` 的子集）里；
另外 **78 个**是 `isPracticeDeck == 0` 的**另一批同名副**（多为 `*_2` 后缀），我们的池里**没有**。
  · 按 `deckId` 精确命中：78/156。
  · **按名字兜底一律不成立**：那 78 条里能按名字在我们池里找到候选的有 30 条，**逐条比过牌表，
    30/30 内容不同**（都是「另一模式的那一副」，比如 skirmish 的 `UM_SK_3_2` 12 张
    对上池里 classic 的 `UMDeck5` 30 张）⇒ **不拿名字凑数**，全部进 `unresolved`。
  · 证据分两列，**两列都不自动判等**：
      - `nameCandidates`（**同名**候选；同模式 + 牌表多重集相同才 `contentEqual = true`）——
        实测 30 条同名候选 **contentEqual 全是 false**（名字撞的是另一模式那副）；
      - `contentTwins`（**不同名但牌表一模一样**）—— 实测只有 1 条：`SautekhDeck3`
        「Implacable Conqueror」↔ 池里 `SautekhDeck8`「Lord of the Storm」。是不是原版自己的重复，
        要**单独裁**（本脚本不改判）。
⇒ 运行时要把练习窗做成原版那样，**光靠现有 103 副池不够**：还差这 78 副的卡表
  （它们全在 `数据/游戏数据/decklists.json` 的 236 条里，`isPractice == 0`，产物里标 `inDecklists: true`）。
· 另外 6 份**教程** `DemoDeckInfoSO`（只有 classic）**不进预组池**，按原版走教程那条路：
  它们的 deckId 落在 `MyGame/Assets/RuleEngine/Resources/tutorial_decks.json`
  （6 关 × player/ai，见 `工具/gen_prebuilt_decks.py`）⇒ 产物里 `ourSource = "tutorial_decks"`，
  并给出 `tutorialSlots`（第几关、哪一方）。**不计入 `unresolved`。**
"""
import argparse
import hashlib
import io
import json
import os
import sys
from collections import Counter, OrderedDict

try:
    import UnityPy
except Exception as exc:  # pragma: no cover - 环境问题要说清楚
    sys.stderr.write("需要 UnityPy（用 D:/2/Warpforge_tools/py312/python.exe 跑）：%r\n" % (exc,))
    raise

# ------------------------------------------------------------------ 常量（只读 d:/2）
BUNDLE_DIR = "d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
B_MONOSCRIPTS = "Waprforge_monoscripts.bundle"          # ⚠️ 原包名就是这个拼写（少了 r）
B_MENUS = "menus_assets_all.bundle"
B_DUPISO = "duplicateassetisolationso_assets_all.bundle"
B_DECKS = "prebuiltdecks_assets_all.bundle"
B_ICONS = "armyicons_assets_all.bundle"

CAB_MONOSCRIPTS = "CAB-17b1b5f865c595045b5fe42b7f8c8967"
CAB_DECKS = "CAB-dbb6f14c119af496b058533ae98dc790"
CAB_ICONS = "CAB-159fa22599e5911bc9d76e52c875bd73"

CLS_DEMO = "DemoDeckInfoSO"
CLS_PRACTICE_POPUP = "PracticeModePopup"
CLS_DECKS = ("PrebuiltDeck", "PrebuiltSortedDeck")

POOL_FILE = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json"
TUTORIAL_FILE = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/tutorial_decks.json"
DECKLISTS_FILE = "d:/4/Unity/数据/游戏数据/decklists.json"
ASSETS_FULL_DIRS = (
    "d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour",
    "d:/2/新解包资源/assets_full/bundle_duplicateassetisolationso_assets_all/MonoBehaviour",
)
OUT = "d:/4/Unity/数据/游戏数据/demo_deck_pairs.json"

FORMAT = 1
GENERATED = "2026-10-22"   # 写死：幂等（两次跑 md5 相同），别改成 datetime.now()


# ------------------------------------------------------------------ 读包小工具
def serialized_files(bundle_filename):
    """打开一个 bundle，返回其中所有 SerializedFile（跳过 .resS 那种纯数据流）。"""
    path = os.path.join(BUNDLE_DIR, bundle_filename)
    if not os.path.isfile(path):
        raise SystemExit("找不到 bundle：%s" % path)
    env = UnityPy.load(path)
    out = []
    for _name, bundle in env.files.items():
        if not hasattr(bundle, "files"):
            continue
        for _inner, sf in bundle.files.items():
            if hasattr(sf, "objects"):
                out.append(sf)
    if not out:
        raise SystemExit("bundle 里没有 SerializedFile：%s" % path)
    return out


def external_cab(sf, file_id):
    """`m_FileID` 是 **1-based** 下标；0 = 同文件（没有外部）。"""
    if file_id == 0:
        return None
    idx = file_id - 1
    if not (0 <= idx < len(sf.externals)):
        return "<out-of-range:%d>" % file_id
    return sf.externals[idx].name


def typetree(reader):
    try:
        t = reader.read_typetree()
    except Exception:
        return None
    return t if isinstance(t, dict) else None


# ------------------------------------------------------------------ 各段数据
def load_monoscript_classes():
    """pathID -> m_ClassName（只认 CAB-17b1b5… 那个包；它是所有脚本的 MonoScript 家）。"""
    table = {}
    for sf in serialized_files(B_MONOSCRIPTS):
        for reader in sf.objects.values():
            if reader.type.name != "MonoScript":
                continue
            t = typetree(reader)
            if t and t.get("m_ClassName"):
                table[reader.path_id] = t["m_ClassName"]
    assert len(table) > 600, "MonoScript 表太小（%d），包不对？" % len(table)
    return table


def class_of(reader, sf, mono):
    """MonoBehaviour 的脚本类名；解不出返回 None。"""
    t = typetree(reader)
    if t is None:
        return None, None
    script = t.get("m_Script") or {}
    cab = external_cab(sf, script.get("m_FileID") or 0)
    if cab != CAB_MONOSCRIPTS:
        return None, t
    return mono.get(script.get("m_PathID")), t


def load_prebuilt_decks(mono):
    """pathID -> PrebuiltDeck 记录（236 个：224 PrebuiltDeck + 12 PrebuiltSortedDeck）。"""
    decks = {}
    for sf in serialized_files(B_DECKS):
        for reader in sf.objects.values():
            if reader.type.name != "MonoBehaviour":
                continue
            cls, t = class_of(reader, sf, mono)
            if cls not in CLS_DECKS or t is None:
                continue
            if "deckId" not in t:
                continue
            decks[reader.path_id] = {
                "deckId": t.get("deckId") or "",
                "deckName": (t.get("deckName") or "").strip(),
                "gameMode": t.get("gameMode"),
                "isPractice": t.get("isPracticeDeck"),
                "cardCount": len(t.get("cardLibraryIds") or []),
                "difficulty": t.get("difficulty"),
                "heroId": ((t.get("deckHero") or {}).get("targetId") or ""),
                "cardIds": list(t.get("cardLibraryIds") or []),
            }
    assert len(decks) == 236, "PrebuiltDeck 个数不是 236（实读 %d）" % len(decks)
    return decks


def load_demo_infos(mono, decks):
    """84 个 DemoDeckInfoSO（menus 82 + duplicateassetisolationso 2）。"""
    rows = []
    for bundle in (B_MENUS, B_DUPISO):
        for sf in serialized_files(bundle):
            for reader in sf.objects.values():
                if reader.type.name != "MonoBehaviour":
                    continue
                cls, t = class_of(reader, sf, mono)
                if cls != CLS_DEMO or t is None:
                    continue
                row = {"bundle": bundle, "pathID": reader.path_id, "raw": t}

                # ---- 两个 deck PPtr：硬断言落在 prebuiltdecks 那个 CAB 上，再按 pathID 取
                for side, key in (("classic", "classicDeck"), ("skirmish", "skirmishDeck")):
                    ptr = t.get(key) or {}
                    cab = external_cab(sf, ptr.get("m_FileID") or 0)
                    pid = ptr.get("m_PathID") or 0
                    if pid == 0:
                        row[side] = None          # 原版就是空（教程那 6 份的 skirmish）
                        continue
                    assert cab == CAB_DECKS, (
                        "%s 的 %s 指向了 %s（不是 %s）" % (t.get("m_Name"), key, cab, CAB_DECKS))
                    assert pid in decks, "%s 的 %s pathID=%d 在 decks 包里找不到" % (
                        t.get("m_Name"), key, pid)
                    row[side] = decks[pid]

                # ---- armyIcon：同样硬断言 + 按 pathID 取
                icon = t.get("armyIcon") or {}
                icon_pid = icon.get("m_PathID") or 0
                icon_cab = external_cab(sf, icon.get("m_FileID") or 0)
                if icon_pid:
                    assert icon_cab == CAB_ICONS, (
                        "%s 的 armyIcon 指向了 %s（不是 %s）" % (t.get("m_Name"), icon_cab, CAB_ICONS))
                row["armyIconPathID"] = icon_pid
                rows.append(row)
    assert len(rows) == 84, "DemoDeckInfoSO 个数不是 84（实读 %d）" % len(rows)
    return rows


def load_army_icons():
    """pathID -> Sprite m_Name（armyicons 包里 17 张）。"""
    icons = {}
    for sf in serialized_files(B_ICONS):
        for reader in sf.objects.values():
            if reader.type.name != "Sprite":
                continue
            t = typetree(reader)
            if t:
                icons[reader.path_id] = t.get("m_Name") or ""
    assert icons, "armyicons 包里一张 Sprite 都没读到"
    return icons


def load_practice_order(mono, demo_by_pid):
    """PracticeModePopup 的序列化 decks 数组 = 练习窗左列的真实顺序（78 项）。"""
    for sf in serialized_files(B_MENUS):
        for reader in sf.objects.values():
            if reader.type.name != "MonoBehaviour":
                continue
            cls, t = class_of(reader, sf, mono)
            if cls != CLS_PRACTICE_POPUP or t is None:
                continue
            arr = t.get("decks")
            if not isinstance(arr, list) or len(arr) < 10:
                continue
            pids = []
            for ptr in arr:
                assert (ptr.get("m_FileID") or 0) == 0, "PracticeModePopup.decks 有跨文件引用"
                pids.append(ptr.get("m_PathID") or 0)
            assert len(pids) == 78, "PracticeModePopup.decks 不是 78 项（实读 %d）" % len(pids)
            assert len(set(pids)) == 78, "PracticeModePopup.decks 有重复项"
            missing = [p for p in pids if p not in demo_by_pid]
            assert not missing, "PracticeModePopup.decks 有 %d 项解不到 DemoDeckInfoSO：%s" % (
                len(missing), missing[:3])
            return {"pathID": reader.path_id, "order": pids}
    raise SystemExit("menus 包里没找到 PracticeModePopup（m_Script 类名对不上？）")


def load_pool():
    """我们自己的 103 副预组池（原版 isPracticeDeck == 1 的子集）。"""
    with io.open(POOL_FILE, encoding="utf-8") as fh:
        data = json.load(fh)
    decks = data["decks"]
    return {d["deckId"]: d for d in decks}, len(decks)


def load_tutorial_decks():
    """教程那 12 副（6 关 × player/ai）—— `deckId -> [(stage, role)]`。
    教程 SO 的 `classicDeck` 全部落在这里（⛔ 不落预组池：原版教程牌从不进池，
    见 `工具/gen_prebuilt_decks.py` 头注释引的 `PrebuiltDeckCollection.AddDeck`）。"""
    if not os.path.isfile(TUTORIAL_FILE):
        return {}
    with io.open(TUTORIAL_FILE, encoding="utf-8") as fh:
        data = json.load(fh)
    out = {}
    for stage in data.get("stages") or []:
        for role in ("player", "ai"):
            d = stage.get(role) or {}
            did = d.get("deckId")
            if did:
                out.setdefault(did, []).append((stage.get("stage"), role))
    return out


def load_decklists_ids():
    """原版 236 副全表（含 `isPractice == 0` 的那批）—— 只用来标注「池外那 78 副的数据在不在」。"""
    if not os.path.isfile(DECKLISTS_FILE):
        return set()
    with io.open(DECKLISTS_FILE, encoding="utf-8") as fh:
        data = json.load(fh)
    return {d.get("deckId") for d in (data.get("decklists") or [])}


# ------------------------------------------------------------------ 组数据
def color_hex(c):
    def ch(v):
        n = int(round(float(v) * 255.0))
        return 0 if n < 0 else (255 if n > 255 else n)
    return "#%02X%02X%02X" % (ch(c.get("r", 0)), ch(c.get("g", 0)), ch(c.get("b", 0)))


def deck_ref(side_deck, pool, tutorials):
    """把一个原版 deck 记录落成产物里的一项。
    落点**只用 deckId 精确命中**：预组池（103 副）优先，教程那 12 副次之（教程 SO 走这条）。
    ⛔ 不再按名字兜底 —— 见文件头「按名字兜底一律不成立」那一节。"""
    d = side_deck
    ours = pool.get(d["deckId"])
    tut = tutorials.get(d["deckId"]) or []
    if ours:
        source = "prebuilt_decks"
    elif tut:
        source = "tutorial_decks"
    else:
        source = None
    return OrderedDict([
        ("deckId", d["deckId"]),
        ("deckName", d["deckName"]),
        ("gameMode", d["gameMode"]),
        ("isPractice", d["isPractice"]),
        ("cardCount", d["cardCount"]),
        ("difficulty", d["difficulty"]),
        ("heroId", d["heroId"]),
        ("ourDeckId", d["deckId"] if source else None),
        ("ourSource", source),
        ("ourNameZh", ours.get("nameZh") if ours else None),
        ("tutorialSlots", [OrderedDict([("stage", s), ("role", r)]) for s, r in tut] if tut else None),
    ])


def name_candidates(side_deck, pool_by_name):
    """同名候选（**只是证据**，不据此判等）：同模式 + 牌表多重集完全相同才算 contentEqual。"""
    out = []
    for cand in pool_by_name.get(side_deck["deckName"].strip(), []):
        same_mode = cand.get("gameMode") == side_deck["gameMode"]
        same_cards = Counter(cand.get("cardIds") or []) == Counter(side_deck["cardIds"])
        out.append(OrderedDict([
            ("ourDeckId", cand["deckId"]),
            ("sameGameMode", same_mode),
            ("contentEqual", bool(same_mode and same_cards)),
        ]))
    out.sort(key=lambda x: x["ourDeckId"])
    return out


def content_twins(side_deck, pool_by_sig):
    """**不同名但牌表一模一样**的池内副（同模式）。同样只是证据 —— 名字不同 ⇒ 不自动判等。"""
    sig = (side_deck["gameMode"], tuple(sorted(Counter(side_deck["cardIds"]).items())))
    out = [OrderedDict([
        ("ourDeckId", c["deckId"]),
        ("ourName", c.get("name") or ""),
        ("ourNameZh", c.get("nameZh") or ""),
    ]) for c in pool_by_sig.get(sig, []) if c["deckId"] != side_deck["deckId"]]
    out.sort(key=lambda x: x["ourDeckId"])
    return out


def build():
    mono = load_monoscript_classes()
    decks = load_prebuilt_decks(mono)
    infos = load_demo_infos(mono, decks)
    icons = load_army_icons()
    demo_by_pid = {r["pathID"]: r for r in infos}
    practice = load_practice_order(mono, demo_by_pid)
    pool, pool_count = load_pool()
    tutorials = load_tutorial_decks()
    decklist_ids = load_decklists_ids()
    pool_by_name = {}
    pool_by_sig = {}
    for d in pool.values():
        pool_by_name.setdefault(d["name"].strip(), []).append(d)
        pool_by_sig.setdefault(
            (d.get("gameMode"), tuple(sorted(Counter(d.get("cardIds") or []).items()))), []).append(d)

    # 「两副齐全」的 78 份 == PracticeModePopup 的 78 项（判据互证；不是抄数）
    both = {r["pathID"] for r in infos if r["classic"] and r["skirmish"]}
    assert set(practice["order"]) == both, (
        "PracticeModePopup 的 78 项与「两副齐全的 DemoDeckInfoSO」不是同一批：差 %s" % (
            list(set(practice["order"]) ^ both)[:3]))

    order_index = {pid: i for i, pid in enumerate(practice["order"])}

    pairs = []
    unresolved = []
    for row in infos:
        t = row["raw"]
        pid = row["pathID"]
        is_practice = pid in order_index
        entry = OrderedDict()
        entry["so"] = t.get("m_Name") or ""
        entry["pathID"] = pid
        entry["bundle"] = row["bundle"]
        entry["kind"] = "practice" if is_practice else "tutorial"
        entry["uiOrder"] = order_index.get(pid)
        entry["loreKey"] = t.get("loreLocalizationKey") or ""
        entry["armyColor"] = color_hex(t.get("armyColor") or {})
        raw = t.get("armyColor") or {}
        entry["armyColorRaw"] = OrderedDict([
            ("r", round(float(raw.get("r", 0)), 9)),
            ("g", round(float(raw.get("g", 0)), 9)),
            ("b", round(float(raw.get("b", 0)), 9)),
            ("a", round(float(raw.get("a", 0)), 9)),
        ])
        icon_pid = row["armyIconPathID"]
        entry["armyIcon"] = icons.get(icon_pid) if icon_pid else None
        entry["armyIconPathID"] = icon_pid
        entry["tutorialIndex"] = t.get("tutorialIndex")
        entry["tutorialIndexToBeUnlocked"] = t.get("tutorialIndexToBeUnlocked")
        for side in ("classic", "skirmish"):
            d = row[side]
            if d is None:
                entry[side] = None
                continue
            entry[side] = deck_ref(d, pool, tutorials)
            if not entry[side]["ourDeckId"]:
                unresolved.append(OrderedDict([
                    ("so", entry["so"]),
                    ("pathID", pid),
                    ("kind", entry["kind"]),
                    ("side", side),
                    ("deckId", d["deckId"]),
                    ("deckName", d["deckName"]),
                    ("gameMode", d["gameMode"]),
                    ("isPractice", d["isPractice"]),
                    ("cardCount", d["cardCount"]),
                    ("reason", "not_in_prebuilt_decks"),
                    ("inDecklists", d["deckId"] in decklist_ids),
                    ("nameCandidates", name_candidates(d, pool_by_name)),
                    ("contentTwins", content_twins(d, pool_by_sig)),
                ]))
        pairs.append(entry)

    # 稳定顺序：练习 78 份按原版窗内顺序；教程 6 份按名字
    pairs.sort(key=lambda e: (0, e["uiOrder"]) if e["kind"] == "practice" else (1, e["so"]))
    unresolved.sort(key=lambda e: (e["so"], e["side"]))

    refs = [e[side] for e in pairs for side in ("classic", "skirmish") if e[side]]
    practice_refs = [e[side] for e in pairs if e["kind"] == "practice"
                     for side in ("classic", "skirmish") if e[side]]
    in_pool = sum(1 for r in refs if r["ourSource"] == "prebuilt_decks")
    in_tut = sum(1 for r in refs if r["ourSource"] == "tutorial_decks")
    full = sum(1 for e in pairs if e["kind"] == "practice"
               and e["classic"]["ourDeckId"] and e["skirmish"]["ourDeckId"])
    partial = sum(1 for e in pairs if e["kind"] == "practice"
                  and bool(e["classic"]["ourDeckId"]) != bool(e["skirmish"]["ourDeckId"]))
    none_ = sum(1 for e in pairs if e["kind"] == "practice"
                and not e["classic"]["ourDeckId"] and not e["skirmish"]["ourDeckId"])

    # assets_full 旁证（读不到就跳过；只比名字集合）
    assets_full_check = None
    try:
        names = set()
        for d in ASSETS_FULL_DIRS:
            if not os.path.isdir(d):
                raise IOError(d)
            for fn in os.listdir(d):
                if not fn.endswith(".json"):
                    continue
                with io.open(os.path.join(d, fn), encoding="utf-8") as fh:
                    j = json.load(fh)
                if isinstance(j, dict) and "classicDeck" in j and j.get("m_Name"):
                    names.add(j["m_Name"])
        assets_full_check = OrderedDict([
            ("soNames", len(names)),
            ("sameNameSet", names == {r["raw"].get("m_Name") for r in infos}),
        ])
    except Exception:
        assets_full_check = None

    data = OrderedDict()
    data["format"] = FORMAT
    data["generated"] = GENERATED
    data["source"] = OrderedDict([
        ("bundleDir", BUNDLE_DIR),
        ("bundles", OrderedDict([
            ("demo", [B_MENUS, B_DUPISO]),
            ("decks", B_DECKS),
            ("icons", B_ICONS),
            ("monoscripts", B_MONOSCRIPTS),
        ])),
        ("cabs", OrderedDict([
            ("decks", CAB_DECKS),
            ("icons", CAB_ICONS),
            ("monoscripts", CAB_MONOSCRIPTS),
        ])),
        ("demoScript", OrderedDict([
            ("className", CLS_DEMO),
            ("monoScriptPathID", -8258122028049581671),
        ])),
        ("practicePopup", OrderedDict([
            ("bundle", B_MENUS),
            ("className", CLS_PRACTICE_POPUP),
            ("pathID", practice["pathID"]),
            ("deckCount", len(practice["order"])),
        ])),
        ("poolFile", POOL_FILE),
        ("poolDeckCount", pool_count),
        ("assetsFullCrossCheck", assets_full_check),
    ])
    data["summary"] = OrderedDict([
        ("soTotal", len(infos)),
        ("practicePairs", sum(1 for e in pairs if e["kind"] == "practice")),
        ("tutorialSo", sum(1 for e in pairs if e["kind"] == "tutorial")),
        ("deckRefs", len(refs)),
        ("practiceDeckRefs", len(practice_refs)),
        ("tutorialDeckRefs", len(refs) - len(practice_refs)),
        ("refsInPool", in_pool),
        ("refsInTutorial", in_tut),
        ("refsUnresolved", len(refs) - in_pool - in_tut),
        ("pairsFullyInPool", full),
        ("pairsPartiallyInPool", partial),
        ("pairsNoneInPool", none_),
    ])
    data["pairs"] = pairs
    data["unresolved"] = unresolved
    return data


def dumps(data):
    return json.dumps(data, ensure_ascii=False, indent=2) + "\n"


def main(argv=None):
    ap = argparse.ArgumentParser(description="解原版 DemoDeckInfoSO 的 classic/skirmish 成对表")
    ap.add_argument("--check", action="store_true", help="只重算并与盘上文件比 md5/逐字节，不写盘")
    args = ap.parse_args(argv)

    text = dumps(build())
    blob = text.encode("utf-8")
    md5 = hashlib.md5(blob).hexdigest()

    if args.check:
        if not os.path.isfile(OUT):
            print("CHECK FAIL：产物还不存在 → %s" % OUT)
            return 1
        with io.open(OUT, "rb") as fh:
            old = fh.read()
        old_md5 = hashlib.md5(old).hexdigest()
        if old == blob:
            print("CHECK OK  md5=%s  bytes=%d" % (md5, len(blob)))
            return 0
        # 只在换行差异时报出来（行尾必须是 LF）
        if old.replace(b"\r\n", b"\n") == blob:
            print("CHECK FAIL：内容相同但行尾不是 LF（盘上 md5=%s，重算=%s）" % (old_md5, md5))
        else:
            print("CHECK FAIL：盘上 md5=%s，重算 md5=%s" % (old_md5, md5))
        return 1

    # ⚠️ 先把内容算进变量再写（io.open(p,'wb').write(<表达式>) 会先截断再求值）
    with io.open(OUT, "wb") as fh:
        fh.write(blob)
    with io.open(OUT, "rb") as fh:
        back = fh.read()
    assert back == blob, "写完回读不一致"
    assert b"\r\n" not in back, "产物里出现了 CRLF"
    print("WROTE %s  md5=%s  bytes=%d" % (OUT, md5, len(blob)))
    data = json.loads(text)
    s = data["summary"]
    print("SO=%d（练习对 %d + 教程 %d）· deck 引用 %d（练习 %d / 教程 %d）· 池内 %d · 教程 %d · 未落点 %d" % (
        s["soTotal"], s["practicePairs"], s["tutorialSo"], s["deckRefs"],
        s["practiceDeckRefs"], s["tutorialDeckRefs"],
        s["refsInPool"], s["refsInTutorial"], s["refsUnresolved"]))
    print("练习对：整对在池 %d / 半对 %d / 全不在 %d" % (
        s["pairsFullyInPool"], s["pairsPartiallyInPool"], s["pairsNoneInPool"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
