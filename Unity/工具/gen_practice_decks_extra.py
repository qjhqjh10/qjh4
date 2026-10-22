# -*- coding: utf-8 -*-
"""练习窗「一对两副」里**落不进 103 副池**的那 78 副 → `prebuilt_decks_extra.json`。

跑法：
    python d:/4/Unity/工具/gen_practice_decks_extra.py            # 写两份（数据 + 运行期副本）
    python d:/4/Unity/工具/gen_practice_decks_extra.py --check    # 只重算、比对 md5，**零写盘**

产物（两份 **extra** 逐字节相同，可重跑）：
    d:/4/Unity/数据/游戏数据/prebuilt_decks_extra.json                     ← 数据面（与别的数据产物同目录）
    d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks_extra.json ← 运行期（`PrebuiltDecks.Load()` 读它）
外加一份**镜像**（不重新生成，只把 D3 的成对表原样抄进 `Resources/`）：
    d:/4/Unity/MyGame/Assets/RuleEngine/Resources/demo_deck_pairs.json     ← `PrebuiltDecks.Pairs` 读它

🔴 **为什么单开一份产物、⛔ 不动 `prebuilt_decks.json`**：
   · 现有那份是预组页那一池（`decklists.json` 里 `isPractice == 1` 的 103 副），
     `工具/gen_prebuilt_decks.py` 的文件头写明它**必须逐字节不变**（预组页列的表面板）；
   · 本脚本补的 78 副是**练习窗 `DemoDeckInfoSO` 引用的另一批**（`isPractice == 0`），
     原版自己也把「池」与「练习对」分成两条路（`DemoDeckInfoSO.classicDeck/skirmishDeck` 直引）。
   ⇒ 两份产物并列，运行时按 **`deckId`** 合并查表（`PrebuiltDecks.ById`）。

🔴 **解析规则全部照抄 `gen_prebuilt_decks.py`，本文件【不写第二份】**（CLAUDE.md §三：
   「两处写同一条规则 = 迟早不一致」）—— 直接 `import` 那个模块，复用：
   `make_entry()`（→ 卡表 `resolve_cards` / 督军 `hero_match` / 卡背 `resolve_cardback` /
   防御卡 `pick_defensive` 全套）、`build_matcher()`、`ARMY_ORDER`。
   ⚠️ 本脚本**只读** `d:/2`（卡背 GUID 表），不写。

输入：
    `数据/游戏数据/demo_deck_pairs.json`    D3 的成对表 —— 取 `unresolved[]`（78 条 = 落不进池的那些 deckId）
    `数据/游戏数据/decklists.json`          236 副原版预组（按 deckId 取回那 78 条记录）
    `MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json`  只读，用来断言「两个来源的 deckId 没有交集」

幂等：同一份输入连跑两次 → 两份产物 md5 相同；`--check` 不写盘、只报差异。
"""
import hashlib
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_prebuilt_decks as G  # noqa: E402  （共用解析规则的那一份）

D = G.D
PAIRS = os.path.join(D, "demo_deck_pairs.json")
POOL_RES = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json"
OUT_DATA = os.path.join(D, "prebuilt_decks_extra.json")
OUT_RES = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks_extra.json"
# 🔴 **练习对成对表的运行期副本**：`PrebuiltDecks` 从 `Resources/` 读（`ResourcePath` / `PairResourcePath`），
#    而 D3 的产物落在 `数据/游戏数据/`。两份**必须逐字节相同** ⇒ 本脚本顺手镜像一份，
#    免得「数据面改了、运行期还是旧的」这种两份漂移（铁律 6：数字与清单只留一处）。
#    ⚠️ 源仍然是 D3 那份（`demo_deck_pairs.json`）—— 本脚本**不重新生成它**，只镜像。
PAIRS_RES = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/demo_deck_pairs.json"

# 产物格式版本（字段有增减就 +1；C# 侧与 `prebuilt_decks.json` 共用同一个 `PrebuiltDecks.Deck` 形状）
FORMAT = 1

CHECK = "--check" in sys.argv[1:]


def build_doc():
    """算出整份产物对象。**纯函数**（不写盘）—— `--check` 也走它。"""
    pairs = G.load(PAIRS)
    unresolved = pairs.get("unresolved") or []
    decks = G.load(os.path.join(D, "decklists.json"))["decklists"]
    ids_doc = G.load(os.path.join(D, "card_ids.json"))

    # ---- deckId → decklists 记录（断言：每条恰好 1 份、都是 isPractice == 0 的练习对成员）----
    by_id = {}
    for d in decks:
        by_id.setdefault(d["deckId"], []).append(d)

    want = [u["deckId"] for u in unresolved]
    assert len(want) == 78, "`demo_deck_pairs.json` 的 unresolved 应是 78 条，实际 %d" % len(want)
    assert len(set(want)) == len(want), "unresolved 里有重复 deckId：%s" % (
        sorted({k for k in want if want.count(k) > 1}),)
    dup = [k for k in want if len(by_id.get(k, [])) != 1]
    assert not dup, "这些 deckId 在 decklists.json 里不是恰好 1 条：%s" % dup
    bad_practice = [k for k in want if by_id[k][0].get("isPractice") != 0]
    assert not bad_practice, "这些 deckId 的 isPractice != 0（不该进本产物）：%s" % bad_practice
    bad_tut = [k for k in want if "tutorial" in k.lower()]
    assert not bad_tut, "教程副混进来了：%s" % bad_tut

    # ---- 「一对两副」里的两侧 ↔ deckId（拿来核 gameMode 与 loreKey）----
    side_of, lore_of = {}, {}
    for p in pairs.get("pairs") or []:
        if p.get("kind") != "practice":
            continue
        for side, mode in (("classic", 0), ("skirmish", 13)):
            ref = p.get(side)
            if not ref:
                continue
            side_of[ref["deckId"]] = (side, mode)
            lore_of[ref["deckId"]] = p.get("loreKey") or ""

    # ---- 与现有 103 副池**不许有交集**（有交集 = 该条本来就在池里，不该进本产物）----
    pool_ids = {d["deckId"] for d in (G.load(POOL_RES).get("decks") or [])}
    overlap = sorted(set(want) & pool_ids)
    assert not overlap, ("这些 deckId **已经在 prebuilt_decks.json 的池里**（本产物只补池外的）：%s" % overlap)

    # ---- 共用解析上下文（与 `gen_prebuilt_decks.main()` 逐行同构）----
    mapping = ids_doc["mapping"]
    # 🔴 与 `gen_prebuilt_decks.main()` 同一条口径：`poolIdsAreTrue` 之后 **⛔ 不能再反转 `poolTrueIds`**
    #    （它只是历史迁移表；反转会把真 id 映射回旧 id，撞上另一张卡）。
    true_to_pool = None if ids_doc.get("poolIdsAreTrue") \
        else {v: k for k, v in (ids_doc.get("poolTrueIds") or {}).items()}
    pool = G.load(G.POOL)["cards"]
    zh_names = G.load(G.ZH)["names"]
    match, hero_match = G.build_matcher(pool, mapping, true_to_pool)
    ctx = dict(match=match, hero_match=hero_match, mapping=mapping,
               by_id={c["id"]: c for c in pool}, zh_names=zh_names,
               gmap=G.load_cardback_guid_map(), have_cb=G.load_have_cardbacks(),
               defpick=G.pick_defensive(pool))

    out = []
    for deck_id in want:                       # 顺序 = `unresolved` 的顺序（= D3 产物的确定序）
        d = by_id[deck_id][0]
        side, mode = side_of[deck_id]
        assert d.get("gameMode") == mode, (
            "%s 的 gameMode = %s，而它在一对里是 %s 侧（应为 %d）" % (deck_id, d.get("gameMode"), side, mode))
        # 🔴 与池那一路**完全同一份** `make_entry`（卡表/督军/卡背/防御卡/张数口径一字不差）
        x = G.make_entry(d, ctx, "practice")
        # 🆕 练习窗的 lore 键来自 `DemoDeckInfoSO.loreLocalizationKey`（一对两副共用一条）——
        #    本产物把它落到每一副上，运行时就能和池里那 78 副走**同一条** `Deck.LoreText` 取法。
        x["loreKey"] = lore_of.get(deck_id, "")
        assert x["loreKey"], "%s 没有 loreKey（`demo_deck_pairs.json` 的 practice 对里应当每条都有）" % deck_id
        out.append(x)

    # ---- 断言（宁可炸掉，也不悄悄产出错数据）----
    assert len(out) == 78, "应产出 78 副，实际 %d" % len(out)
    assert len({x["deckId"] for x in out}) == 78, "deckId 重名（练习对的 156 个引用本来就互不重复）"
    not_complete = [(x["deckId"], x["missing"], x["problems"]) for x in out if not x["complete"]]
    assert not not_complete, "这些副拼不齐（本轮实测应为 0）：%s" % not_complete
    no_hero = [x["deckId"] for x in out if not x["heroId"]]
    assert not no_hero, "这些副的督军没解出来：%s" % no_hero
    no_cb = [x["deckId"] for x in out if not x["cardback"]]
    assert not no_cb, "这些副没有卡背名：%s" % no_cb
    no_art = [(x["deckId"], x["cardback"]) for x in out
              if x["cardback"] not in ctx["have_cb"] and x["cardback"] not in G.KNOWN_NO_ART]
    assert not no_art, "这些副的卡背图本地没有（补别名/替身表，或确认后记账）：%s" % no_art
    bad_army = sorted({x["faction"] for x in out} - set(G.ARMY_ORDER))
    assert not bad_army, "这些阵营不在 CardArmy 枚举里（排序会错）：%s" % bad_army

    doc = dict(
        format=FORMAT,
        note="由 工具/gen_practice_decks_extra.py 生成，**别手改**。"
             "解析规则与 工具/gen_prebuilt_decks.py **同一份代码**（本脚本 import 它）。"
             "🔴 卡池或 id 表变了 ⇒ 必须重跑生成器。",
        pool="练习窗 `DemoDeckInfoSO` 那 78 对里**落不进 `prebuilt_decks.json`（isPractice==1 的 103 副）**的 78 副"
             "（`isPractice == 0`）。与那份产物**并列**，运行期按 `deckId` 合并查表（`PrebuiltDecks.ById`）。",
        source="d:/4/Unity/数据/游戏数据/demo_deck_pairs.json 的 `unresolved[]`（D3 · 2026-10-22）"
               " + d:/4/Unity/数据/游戏数据/decklists.json 的同名记录。",
        deckCount=len(out),
        decks=out,
    )
    stats = dict(
        total=len(out),
        complete=sum(1 for x in out if x["complete"]),
        classic=sum(1 for x in out if x["gameMode"] == 0),
        skirmish=sum(1 for x in out if x["gameMode"] == 13),
        no_zh=sum(1 for x in out if not x["nameZh"]),
        warn=len(ctx.get("warn") or []),
    )
    return doc, stats, ctx


def text_of(doc):
    """序列化口径与 `gen_prebuilt_decks.py` 一致：`ensure_ascii=False` · `indent=1` · LF · 无尾随换行。"""
    return json.dumps(doc, ensure_ascii=False, indent=1)


def md5_bytes(b):
    return hashlib.md5(b).hexdigest()


def main():
    doc, stats, ctx = build_doc()
    text = text_of(doc)
    data = text.encode("utf-8")
    digest = md5_bytes(data)
    pairs_blob = open(PAIRS, "rb").read()          # 镜像**原样**（不重新序列化）
    pairs_digest = md5_bytes(pairs_blob)
    writes = ((OUT_DATA, data), (OUT_RES, data), (PAIRS_RES, pairs_blob))

    if CHECK:
        bad = 0
        for p, want in writes:
            if not os.path.exists(p):
                print("MISSING", p)
                bad += 1
                continue
            have = md5_bytes(open(p, "rb").read())
            ok = have == md5_bytes(want)
            print(("OK   " if ok else "STALE") + " " + p + "  md5=" + have)
            if not ok:
                bad += 1
        print("want md5(extra)=" + digest + "  md5(pairs)=" + pairs_digest)
        print("practice-extra=%d complete=%d classic=%d skirmish=%d noZh=%d crossFactionIdHits=%d"
              % (stats["total"], stats["complete"], stats["classic"], stats["skirmish"],
                 stats["no_zh"], stats["warn"]))
        if bad:
            print("--check 失败：%d 份产物与重算结果不一致（重跑不带 --check 即可刷新）" % bad)
            sys.exit(1)
        print("--check 通过（extra 两份 + 练习对镜像都与重算结果逐字节相同）")
        return

    for p, blob in writes:
        with open(p, "wb") as f:
            f.write(blob)
        print("wrote", p)
    print("md5(extra)=" + digest + "  md5(pairs)=" + pairs_digest)
    print("practice-extra=%d complete=%d classic=%d skirmish=%d noZh=%d crossFactionIdHits=%d"
          % (stats["total"], stats["complete"], stats["classic"], stats["skirmish"],
             stats["no_zh"], stats["warn"]))
    # 🔴 「卡号命中了、但那张卡不在本副阵营」= 卡表缺陷 ⇒ 出声（同 `gen_prebuilt_decks.py` 的收尾）
    for s in (ctx.get("warn") or []):
        print("   " + s.encode("ascii", "backslashreplace").decode("ascii"))
    # 中文名：本批 78 副里有 48 副 `zh_decks.json` 没有条目（官方名表 `PrebuiltDecks/<deckId>` 也没有 ⇒
    # `DisplayName` 按既定口径回落英文原名）。**如实印出来，⛔ 不编译名**。
    no_zh = sorted(x["deckId"] for x in doc["decks"] if not x["nameZh"])
    print("noZh = %d（这些副没有我们自译的中文名，界面回落英文原名）%s"
          % (len(no_zh), "" if not no_zh else "：" + ", ".join(no_zh[:8]) + ("…" if len(no_zh) > 8 else "")))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
