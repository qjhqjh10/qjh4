# -*- coding: utf-8 -*-
"""原版 `PrebuiltDeckCollection` 的**三张表**（`decks` / `botDecks` / `practiceDecks`）→ `prebuilt_deck_lists.json`。

跑法：
    python d:/4/Unity/工具/gen_prebuilt_bot_decks.py            # 写两份（数据面 + 运行期）
    python d:/4/Unity/工具/gen_prebuilt_bot_decks.py --check    # 只重算、比对 md5，**零写盘**
    python d:/4/Unity/工具/gen_prebuilt_bot_decks.py --dry      # 只算、打印读数，**零写盘**

产物（两份**逐字节相同**）：
    d:/4/Unity/数据/游戏数据/prebuilt_deck_lists.json
    d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_deck_lists.json

================================================================================
 为什么单开这一份产物（⛔ 不动 `prebuilt_decks.json` / `prebuilt_decks_extra.json`）
================================================================================
现有两份产物都是**预组页/练习窗要列出来给人看的那两批**：
  · `prebuilt_decks.json`       = `decklists.json` 里 `isPractice == 1` 的 103 副（预组页那一池，**要求逐字节不变**）；
  · `prebuilt_decks_extra.json` = 练习窗 78 对里落不进那 103 副的另外 78 副（`isPractice == 0`）。
原版 `PrebuiltDeckCollection` 的三张表**都不是按 isPractice 划的** —— 它们是在 `AddDeck` 里
按 **资产名含 "Tutorial" / `PrebuiltDeck.Type` / `isPracticeDeck`** 三条**同时**分出来的：

  `decomp_full/PrebuiltDeckCollection__AddDeck.c:21-40` + `cpp2il/out_cpp2il_dev_ilspy/PrebuiltDeckCollection.cs:234-253`
    ① `deck.name.Contains("Tutorial")` ⇒ **直接 return**（连 `decks` 都不进）；
    ② 否则 `decks.Add(deck)`；
    ③ `deck.Type == PrebuiltDeck.DeckType.Bot`（枚举值 **0**）⇒ `botDecks.Add(deck)`；
    ④ `deck.isPracticeDeck` ⇒ `practiceDecks.Add(deck)`。
  ⇒ `decks` 是**所有非教程副**（不是 103 副池！），`botDecks` 是其中 `Type == Bot` 的那一批。
     本脚本产的就是这三张表背后的**同一份 224 条底表**（`decks` + 每条的分类位 `meta`），
     运行时由 `PrebuiltDecks.Decks` / `BotDecks` 直接取，⛔ 不再按 isPractice 猜。

数据来源（**只读** `d:/2`，铁律 2）：
    d:/2/新解包资源/assets_full/bundle_prebuiltdecks_assets_all/MonoBehaviour/*.json   236 个 `PrebuiltDeck` SO
      —— `m_Name`（= 原版 `deck.name`，`AddDeck` 拿它判 "Tutorial"）· `Type` · `isPracticeDeck`
         · `gameMode` · `difficulty` · `cardLibraryIds`
    d:/4/Unity/数据/游戏数据/decklists.json     236 副记录（按 `deckId` 一一对上；只取其中的阵营/名字/卡表原序）
    d:/4/Unity/数据/游戏数据/card_ids.json      `mapping`（原版 id → 卡名）+ `poolIdsAreTrue`

🔴 **解析规则全部照抄 `gen_prebuilt_decks.py`，本文件【不写第二份】**（CLAUDE.md §三：
   「两处写同一条规则 = 迟早不一致」）—— 直接 import 它，复用 `make_entry()`（→ `resolve_cards` /
   `hero_match` / `resolve_cardback` / `pick_defensive` 全套）、`build_matcher()`、`ARMY_ORDER`、`DEMO_LORE_KEY`。

🔴 **`kind` 传 `"practice"` 是有意的**（不是笔误）：非教程副的张数口径与预组池同一条
   （经典 30 / 遭遇 12 —— 现读 224/224 都满足）、`complete` 同一条、**我们补的防御卡**同一条
   （用户 2026-09-26 拍板「人机对战应该有防御卡」）。⛔ **不新开第三个 `kind`** ——
   那会让 `make_entry` 长出一条与池/教程都不共享的分支，正是「两处写同一条规则」的那个坑。

幂等：同一份输入连跑两次 → 两份产物 md5 相同；`--check` 不写盘、只报差异。
"""
import hashlib
import json
import os
import sys
from collections import Counter

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/gen_prebuilt_bot_decks.py [参数...] [--check|--dry]\n'
          '  说明：原版 PrebuiltDeckCollection 的三张表（decks / botDecks / practiceDecks）→ prebuilt_deck_lists.json。\n'
          '  选项：--check（只重算比对 md5，零写盘）· --dry（只算不写盘；一个都不带 = 写两份产物）')
    _wf_help_sys.exit(0)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_prebuilt_decks as G  # noqa: E402  （共用解析规则的那一份）

D = G.D
SO_DIR = "d:/2/新解包资源/assets_full/bundle_prebuiltdecks_assets_all/MonoBehaviour"
DECKLISTS = os.path.join(D, "decklists.json")
POOL_RES = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json"
OUT_DATA = os.path.join(D, "prebuilt_deck_lists.json")
OUT_RES = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/prebuilt_deck_lists.json"

FORMAT = 1

CHECK = "--check" in sys.argv[1:]
DRY = "--dry" in sys.argv[1:]

# 🔴 **现读的规模常数**（2026-10-26 采自上面那批 SO）。**变了就炸**，由人来复核输入是不是换 build 了 ——
#    ⛔ 别「顺手把数字改大」：这三个数就是「原版三张表各几条」这件事本身。
EXPECT_SO_TOTAL = 236        # 236 个 PrebuiltDeck SO（= decklists.json 的 236 条记录）
EXPECT_TUTORIAL = 12         # `m_Name` 含 "Tutorial" 的 12 副（= `Type == Tutorial(2)` 的那 12 副）
EXPECT_DECKS = 224           # `decks`（非教程全部）
EXPECT_BOT = 196             # `botDecks`（`Type == Bot(0)`）
EXPECT_DEFAULT = 28          # `Type == Default(1)`
EXPECT_PRACTICE = 103        # `practiceDecks`（`isPracticeDeck == 1`，两边都非教程）—— 必须 = 预组池那 103 副


def load_sos():
    """236 个 `PrebuiltDeck` SO → `(全部 SO 列表, 非教程 SO 列表)`。

    🔴 **不能按 deckId 建全量索引**：教程那批**共用** deckId（`UMTutorial` / `NecronsTutorial` 各两份，
       见 `decklists.json` 的同名两条）⇒ 只有**非教程**那 224 副的 deckId 才唯一（本函数当场断言）。"""
    import glob
    files = sorted(glob.glob(os.path.join(SO_DIR, "*.json")))
    assert len(files) == EXPECT_SO_TOTAL, "SO 文件数应是 %d，实际 %d" % (EXPECT_SO_TOTAL, len(files))
    all_sos = [G.load(p) for p in files]
    for j, p in zip(all_sos, files):
        assert j.get("deckId"), "SO 没有 deckId：%s" % p
    live = [j for j in all_sos if "Tutorial" not in (j.get("m_Name") or "")]
    seen = {}
    for j in live:
        did = j["deckId"]
        assert did not in seen, "非教程副的 deckId 重复：%s（%s vs %s）" % (
            did, seen[did], j.get("m_Name"))
        seen[did] = j.get("m_Name")
    return all_sos, live


def build_doc():
    """算出整份产物对象。**纯函数**（不写盘）—— `--check` / `--dry` 都走它。"""
    all_sos, live_sos = load_sos()
    sos = {j["deckId"]: j for j in live_sos}          # 非教程 224 副：deckId 唯一（load_sos 已断言）
    decks = G.load(DECKLISTS)["decklists"]
    ids_doc = G.load(os.path.join(D, "card_ids.json"))

    # ---- `AddDeck` 的第 ① 条：资产名含 "Tutorial" 的直接不进任何表 ------------------
    #    ⚠️ 这一条判的是 **SO 资产名**（`m_Name` = 原版 `deck.name`），**不是 deckId** ——
    #       教程那批的 deckId 有重名（`UMTutorial` / `NecronsTutorial`），拿 deckId 比会漏。
    tut = sorted(j["m_Name"] for j in all_sos if "Tutorial" in (j.get("m_Name") or ""))
    assert len(tut) == EXPECT_TUTORIAL, "`m_Name` 含 Tutorial 的应是 %d 副，实际 %d：%s" % (
        EXPECT_TUTORIAL, len(tut), tut)
    tut_type = sorted(j["m_Name"] for j in all_sos if j.get("Type") == 2)
    assert tut == tut_type, (
        "「资产名含 Tutorial」与「`Type == Tutorial(2)`」不是同一批 —— `AddDeck` 只认**资产名**这一条，"
        "两批不一致时要人来裁：名-only=%s · type-only=%s"
        % (sorted(set(tut) - set(tut_type)), sorted(set(tut_type) - set(tut))))

    live = sorted(sos)
    assert len(live) == EXPECT_DECKS, "`decks` 应是 %d 副，实际 %d" % (EXPECT_DECKS, len(live))
    types = Counter(sos[did].get("Type") for did in live)
    assert types[0] == EXPECT_BOT and types[1] == EXPECT_DEFAULT and len(types) == 2, (
        "`decks` 的 Type 分布与现读不符（Bot %d / Default %d，实测 %s）—— 先复核输入是不是换 build 了"
        % (EXPECT_BOT, EXPECT_DEFAULT, dict(types)))

    # ---- deckId → decklists 记录（恰好 1 条；卡表逐字相同）------------------------
    by_dl = {}
    for d in decks:
        by_dl.setdefault(d["deckId"], []).append(d)
    bad_join = [did for did in live if len(by_dl.get(did, [])) != 1]
    assert not bad_join, "这些 deckId 在 decklists.json 里不是恰好 1 条：%s" % bad_join
    mism = [(did, len(sos[did].get("cardLibraryIds") or []), len(by_dl[did][0].get("cardIds") or []))
            for did in live if list(sos[did].get("cardLibraryIds") or []) != list(by_dl[did][0].get("cardIds") or [])]
    assert not mism, "SO 的 `cardLibraryIds` 与 decklists 的 `cardIds` 不是逐字同序（先查清再决定用哪一份）：%s" % mism[:5]
    bad_meta = [(did, sos[did].get("gameMode"), by_dl[did][0].get("gameMode"),
                 sos[did].get("difficulty"), by_dl[did][0].get("difficulty"))
                for did in live
                if sos[did].get("gameMode") != by_dl[did][0].get("gameMode")
                or (sos[did].get("difficulty") or 0) != (by_dl[did][0].get("difficulty") or 0)]
    assert not bad_meta, "SO 与 decklists 的 gameMode/difficulty 不一致：%s" % bad_meta[:5]

    # ---- 共用解析上下文（与 `gen_prebuilt_decks.main()` 逐行同构）------------------
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

    out, meta = [], []
    for did in live:
        so, rec = sos[did], by_dl[did][0]
        # 🔴 与预组池/教程**完全同一份** `make_entry`（卡表/督军/卡背/防御卡/张数口径一字不差）
        x = G.make_entry(rec, ctx, "practice")
        x["loreKey"] = G.DEMO_LORE_KEY.get(did, "")
        out.append(x)
        meta.append(dict(deckId=did, srcName=so.get("m_Name"),
                         type=so.get("Type"), isPractice=so.get("isPracticeDeck")))

    # ---- 断言（宁可炸掉，也不悄悄产出错数据）--------------------------------------
    assert len({x["deckId"] for x in out}) == EXPECT_DECKS, "deckId 重名"
    not_complete = [(x["deckId"], x["missing"], x["problems"]) for x in out if not x["complete"]]
    assert not not_complete, "这些副拼不齐（现读应为 0）：%s" % not_complete
    no_hero = [x["deckId"] for x in out if not x["heroId"]]
    assert not no_hero, "这些副的督军没解出来：%s" % no_hero
    no_cb = [x["deckId"] for x in out if not x["cardback"]]
    assert not no_cb, "这些副没有卡背名：%s" % no_cb
    no_art = [(x["deckId"], x["cardback"]) for x in out
              if x["cardback"] not in ctx["have_cb"] and x["cardback"] not in G.KNOWN_NO_ART]
    assert not no_art, "这些副的卡背图本地没有（补别名/替身表，或确认后记账）：%s" % no_art
    bad_army = sorted({x["faction"] for x in out} - set(G.ARMY_ORDER))
    assert not bad_army, "这些阵营不在 CardArmy 枚举里：%s" % bad_army

    # ---- 与预组池那 103 副交叉核对：`practiceDecks` 必须**就是**那一池 ----------------
    prac_meta = sorted(m["deckId"] for m in meta if m["isPractice"] == 1)
    assert len(prac_meta) == EXPECT_PRACTICE, "`isPracticeDeck == 1` 应是 %d 副，实际 %d" % (
        EXPECT_PRACTICE, len(prac_meta))
    pool_ids = {d["deckId"] for d in (G.load(POOL_RES).get("decks") or [])}
    assert set(prac_meta) == pool_ids, (
        "`practiceDecks`（`isPracticeDeck == 1`）与 `prebuilt_decks.json` 那 103 副不是同一批 —— "
        "只差一边的：prod-only=%s · pool-only=%s"
        % (sorted(set(prac_meta) - pool_ids), sorted(pool_ids - set(prac_meta))))

    # ---- 🔴 `GetDeckByDifficulty` 的「原版 bug 可观察性」：每个 (难度, 模式) 格子都必须比 224 少 -----
    #    原版拿**未过滤**表长度取下标（`Random.Range(0, decks.Count)`）再 `ElementAtOrDefault` ⇒
    #    只要某格 `count < 224`，那格就有 `1 - count/224` 的概率返回 null。**这是原版自带缺陷，照抄不修。**
    grid = Counter((x["difficulty"], x["gameMode"]) for x in out)
    wide = {k: v for k, v in grid.items() if v >= EXPECT_DECKS}
    assert not wide, "这些 (难度, 模式) 格子把 224 副全占了 ⇒ 原版那条缺陷观察不到（复核输入）：%s" % wide

    doc = dict(
        format=FORMAT,
        note="由 工具/gen_prebuilt_bot_decks.py 生成，**别手改**。解析规则与 工具/gen_prebuilt_decks.py "
             "**同一份代码**（本脚本 import 它）。🔴 卡池或 id 表变了 ⇒ 必须重跑生成器。",
        source="d:/2/新解包资源/assets_full/bundle_prebuiltdecks_assets_all/MonoBehaviour/*.json（236 个 "
               "`PrebuiltDeck` SO：`m_Name` / `Type` / `isPracticeDeck` / `gameMode` / `difficulty` / "
               "`cardLibraryIds`）+ d:/4/Unity/数据/游戏数据/decklists.json（同 deckId 的记录）。",
        deckCount=len(out),
        botCount=types[0],
        practiceCount=len(prac_meta),
        decks=out,
        meta=meta,
    )
    stats = dict(
        total=len(out), bot=types[0], default=types[1], practice=len(prac_meta),
        complete=sum(1 for x in out if x["complete"]),
        classic=sum(1 for x in out if x["gameMode"] == 0),
        skirmish=sum(1 for x in out if x["gameMode"] == 13),
        no_zh=sum(1 for x in out if not x["nameZh"]),
        no_lore=sum(1 for x in out if not x["loreKey"]),
        grid=grid,
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
    data = text_of(doc).encode("utf-8")
    digest = md5_bytes(data)
    writes = ((OUT_DATA, data), (OUT_RES, data))

    if CHECK:
        bad = 0
        for p, want in writes:
            if not os.path.exists(p):
                print("MISSING", p)
                bad += 1
                continue
            have = md5_bytes(open(p, "rb").read())
            if have != md5_bytes(want):
                print("DIFF", p, "disk=%s want=%s" % (have, md5_bytes(want)))
                bad += 1
        print("--check %s（%d 份产物）" % ("FAIL" if bad else "OK", len(writes)))
        return 1 if bad else 0

    if DRY:
        print("干跑（未写盘）：", OUT_DATA)
        print("干跑（未写盘）：", OUT_RES)
    else:
        for p, b in writes:
            with open(p, "wb") as f:
                f.write(b)

    # 控制台用 ASCII，别打 emoji（Windows 控制台 GBK）
    print("decks=%(total)d (bot=%(bot)d / default=%(default)d) practice=%(practice)d "
          "complete=%(complete)d classic=%(classic)d skirmish=%(skirmish)d "
          "noZh=%(no_zh)d noLore=%(no_lore)d warn=%(warn)d md5=%(md5)s"
          % dict(stats, md5=digest))
    print("GetDeckByDifficulty 的 (难度, 模式) 格子（分母 = %d；count < 分母 ⇒ 那格会返回 null）：" % stats["total"])
    for (diff, mode), n in sorted(stats["grid"].items()):
        print("   diff=%-3d mode=%-3d -> %-4d  命中率 %.1f%%" % (diff, mode, n, 100.0 * n / stats["total"]))
    if ctx.get("warn"):
        print("⚠️ 解析告警 %d 条（详见 gen_prebuilt_decks.py 的 ctx['warn']）：" % len(ctx["warn"]))
        for w in ctx["warn"][:10]:
            print("   -", w)
    return 0


if __name__ == "__main__":
    sys.exit(main())
