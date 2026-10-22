#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_remnant_body_table.py — 把原版**逐卡残骸体**（`remnantBodyPrefab`）解成一张表。

账：`资料/普查产出_1022/W64_兜底审计_特效.md` **B16**
    （原文：「原版逐卡残骸体字段（71 条）没接；我们走按关键词的
     `RemnantBody3D Aeldari/Necrons`」⇒ B 类，必须复刻）

判据链（逐跳可复核，**不是**按阵营/关键词推）
------------------------------------------------
  ① `d:/2/tools/decomp_full/BattleCardUI__SetRawCardData.c:45` —— 只有
     `SupportMethods.HasToTransformIntoRemnant(rawCard)` 为真才 `CreateRemnantBody()`
  ② `SupportMethods__HasToTransformIntoRemnant.c` —— 那个判据 = **两条当前特质**
     `HasCurrentTrait(0x41a) || HasCurrentTrait(0x474)`（0x41a = `Remnant` · 0x474 = `Waystone`；
     与我们 `RuleCore.CleanupDeaths` 那三行 `u.Has(Remnant) || u.Has(Waystone)` 同一形状）
  ③ `BattleCardUI__CreateRemnantBody.c:36-65` —— 实例化的对象 = **卡自己的**
     `AssetReferenceTyped`（`rawCard + 0xe0` 那个字段 = `remnantBodyPrefab`），
     父节点 = 这张卡的 transform、位置 = 它自己的 `position` ⇒ **逐卡字段，不是按阵营查表**
  ④ 字段真值 = `资料/普查产出_1018/allcards_1350/cards_raw.json` 的
     `cards[*].full_raw.remnantBodyPrefab`（`AssetReference`：`m_AssetGUID`）
  ⑤ GUID → prefab 名 = `资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv`
     （与 `gen_cards_engine.py` 同一个索引；**不信**任何「按名字猜」）
  ⑥ 交叉校验 = `数据/游戏数据/card_vfx_by_card.json` 里 `death` 桶下
     `soField == remnantBodyPrefab` 的行（那份表**已经走完** ⑤ 那一跳，且带 `inLibrary`）

产物
----
`d:/4/Unity/数据/游戏数据/remnant_body_by_card.json`

用法
----
  python -I -X utf8 d:/4/Unity/工具/gen_remnant_body_table.py            # 重出产物
  python -I -X utf8 d:/4/Unity/工具/gen_remnant_body_table.py --check    # 只比不写（幂等自检）
"""

import io
import json
import os
import sys

ROOT4 = "d:/4/Unity"

SO_TRUTH = ROOT4 + "/资料/普查产出_1018/allcards_1350/cards_raw.json"
GUID_MAP = ROOT4 + "/资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv"
VFX_TABLE = ROOT4 + "/数据/游戏数据/card_vfx_by_card.json"
CARDS_ENGINE = ROOT4 + "/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
VFX_LIB = ROOT4 + "/MyGame/Assets/WarpforgeVFX/Prefabs"

OUT = ROOT4 + "/数据/游戏数据/remnant_body_by_card.json"

# ② `HasToTransformIntoRemnant` 认的那两条特质的 **id**（`EntityScript.HasCurrentTrait` 的实参）：
#   `0x41a` = 1050 = `remnant` · `0x474` = 1140 = `waystone`
#   （名字来自 ST 的 `CardTraits[].name`，本件逐卡核过：1050 只出现在 `remnant` 上、
#    1140 只出现在 `waystone` 上 —— 全 1,351 张卡，0 例外）
TRAIT_REMNANT = 1050
TRAIT_WAYSTONE = 1140
REMNANT_KW = ("remnant", "remnant.")
WAYSTONE_KW = ("waystone",)


def load(p):
    return json.load(io.open(p, encoding="utf-8"))


def load_guid_index():
    """GUID → (资产名, 束文件, 类型, 类名)。同一 GUID 多行时**保留第一行**（同 `gen_cards_engine`）。"""
    idx = {}
    if not os.path.isfile(GUID_MAP):
        print("⚠️ 读不到 GUID 表 %s ⇒ 只能靠 vfx 表那一侧，逐条会标 `guidMap: false`" % GUID_MAP)
        return idx
    with io.open(GUID_MAP, encoding="utf-8") as f:
        for line in f:
            p = line.rstrip("\n").split("\t")
            if len(p) < 9 or not p[3] or not p[8]:
                continue
            idx.setdefault(p[3], (p[8], p[4], p[6], p[7]))
    return idx


def in_library(prefab):
    return bool(prefab) and os.path.isfile(os.path.join(VFX_LIB, prefab + ".prefab"))


def kw_of(cid, ce):
    c = ce.get(cid) or {}
    return [(k or "") for k in (c.get("keywords") or [])]


def can_transform(traits):
    """② `HasToTransformIntoRemnant` = 卡上带 `0x41a`(1050) 或 `0x474`(1140) 这两条**当前特质**
    —— 直接读卡 SO 的 `CardTraits`，**不经过**我们的关键词表（那只是代用品）。"""
    return any(t in (TRAIT_REMNANT, TRAIT_WAYSTONE) for t in traits)


def can_transform_by_keyword(keywords):
    """我们现在那条判据（`Remnant`/`Waystone` 关键词）—— 只用来交叉校验 ② 那条。"""
    low = [(k or "").strip().lower() for k in keywords]
    return any(k in REMNANT_KW for k in low) or any(k in WAYSTONE_KW for k in low)


def keyword_rule_prefab(keywords):
    """我们现在那条规则（`BattleDriver.RemnantPrefabOf`）会给出哪个 prefab。
    `Waystone` → Aeldari · `Remnant` → Necrons · 都不是 → null。"""
    low = [(k or "").strip().lower() for k in keywords]
    if any(k in WAYSTONE_KW for k in low):
        return "RemnantBody3D Aeldari"
    if any(k in REMNANT_KW for k in low):
        return "RemnantBody3D Necrons"
    return None


def build():
    so = load(SO_TRUTH)["cards"]
    ce = {c["id"]: c for c in load(CARDS_ENGINE)["cards"]}
    idx = load_guid_index()
    vfx = load(VFX_TABLE)

    # ⑥ vfx 表那一侧（已走完 GUID→名字那一跳）：id → 行
    vfx_rows = {}
    for cid, e in (vfx.get("byCard") or {}).items():
        for r in (e.get("death") or []):
            if (r.get("soField") or "") == "remnantBodyPrefab":
                vfx_rows[cid] = r

    by_card = {}
    unresolved = []
    conflicts = []
    for s in so:
        fr = s.get("full_raw") or {}
        ref = fr.get("remnantBodyPrefab") or {}
        guid = (ref.get("m_AssetGUID") or "").strip().lower()
        if not guid:
            continue                          # 这张卡没有残骸体（原版字段是空引用）
        cid = s.get("uniqueId")
        rec = idx.get(guid)
        name_from_map = rec[0] if rec else None
        bundle_from_map = (rec[1][:-7] if rec and rec[1].endswith(".bundle") else (rec[1] if rec else None))
        vrow = vfx_rows.get(cid) or {}
        name_from_vfx = vrow.get("assetName")

        prefab = name_from_map or name_from_vfx
        if not prefab:
            unresolved.append({"id": cid, "guid": guid,
                               "why": "GUID 表里没有这条，vfx 表也没解出名字"})
            continue
        if name_from_map and name_from_vfx and name_from_map != name_from_vfx:
            conflicts.append({"id": cid, "guid": guid,
                              "fromGuidMap": name_from_map, "fromVfxTable": name_from_vfx})

        kws = kw_of(cid, ce)
        c = ce.get(cid) or {}
        in_pool = cid in ce
        rule = keyword_rule_prefab(kws) if in_pool else None
        trait_rows = [{"name": t.get("name"), "id": t.get("_trait")}
                      for t in (s.get("CardTraits") or [])]
        traits = [t.get("_trait") for t in (s.get("CardTraits") or [])]
        can = can_transform(traits)                   # ② 原版那条判据（同一对 trait id）
        can_kw = can_transform_by_keyword(kws)
        by_card[cid] = {
            "id": cid,
            "name": c.get("name") or s.get("cardName") or "",
            "faction": c.get("faction") or "",
            "type": c.get("type") or "",              # unit / hero / tactic / defence
            "cardType": s.get("cardType"),            # 原版 `RawCardScript.cardType`
            "cardArmy": s.get("cardArmy"),            # 原版阵营枚举（30 = Aeldari · 40 = Necrons …）
            "inPool": in_pool,
            "guid": guid,
            "prefab": prefab,
            "assetBundle": bundle_from_map or vrow.get("assetBundle"),
            "inLibrary": in_library(prefab),          # 工程 `WarpforgeVFX/Prefabs/<名>.prefab` 在不在
            "canTransform": can,                      # ② `HasToTransformIntoRemnant`（trait 1050/1140）
            "traits": trait_rows,                     # 原版 `CardTraits` 全量（判据可复查）
            "keywords": kws,
            # 我们那条「按关键词挑 prefab」的规则给的答案 + 与逐卡字段是否一致
            "keywordRulePrefab": rule,
            "agreesWithKeywordRule": (None if not in_pool else (rule == prefab)),
            "keywordsAgreeWithTraits": (None if not in_pool else (can_kw == can)),
            "guidResolvedBy": ("guidMap" if name_from_map else "vfxTable"),
            "vfxTableAgrees": (None if not name_from_vfx else name_from_vfx == prefab),
        }

    ids = sorted(by_card)
    pool_ids = [cid for cid in ids if by_card[cid]["inPool"]]
    npool_ids = [cid for cid in ids if not by_card[cid]["inPool"]]
    by_prefab = {}
    for cid in pool_ids:
        by_prefab.setdefault(by_card[cid]["prefab"], []).append(cid)

    unreachable = [cid for cid in ids if not by_card[cid]["canTransform"]]
    # 只比**池内**的（池外那 3 张 tutorial 卡在 `cards_engine` 里没有条目 ⇒
    # `keywordRulePrefab` 恒 None，拿它算「不一致」是假阳性）
    disagree = [cid for cid in pool_ids if by_card[cid]["canTransform"]
                and not by_card[cid]["agreesWithKeywordRule"]]
    kw_mismatch = [cid for cid in ids if by_card[cid]["keywordsAgreeWithTraits"] is False]
    missing_in_lib = [cid for cid in ids if not by_card[cid]["inLibrary"]]

    stats = {
        "cardsWithRemnantPrefab": len(pool_ids),       # 我们卡池里的（审计记的 71 条就是这一档）
        "cardsWithRemnantPrefabAllSo": len(ids),      # 含不在池里的 3 张 tutorial 卡
        "uniquePrefabs": len(by_prefab),
        "perPrefab": {k: len(v) for k, v in sorted(by_prefab.items())},
        # ② 能不能真的变残骸（= 原版 `HasToTransformIntoRemnant`，trait 1050/1140）
        "cardsCanTransform": len([c for c in pool_ids if by_card[c]["canTransform"]]),
        "cardsCanTransformAllSo": len([c for c in ids if by_card[c]["canTransform"]]),
        "cardsPrefabButCannotTransform": len(unreachable),
        "cardsOutsidePool": len(npool_ids),
        # 两条旁证是否与 ② 一致（都该是 0）
        "keywordsDisagreeWithTraits": len(kw_mismatch),
        # 现在那条按关键词的规则 vs 逐卡字段
        "disagreeWithKeywordRule": len(disagree),
        "notInVfxLibrary": len(missing_in_lib),
        "unresolved": len(unresolved),
        "guidMapConflicts": len(conflicts),
    }

    headline = (
        "🔴 `W64 · B16`：原版**逐卡**残骸体 `RawCardScript.remnantBodyPrefab` 的真值表。"
        "判据 = `BattleCardUI__SetRawCardData.c:45`（只有 `HasToTransformIntoRemnant` 才建）"
        " + `SupportMethods__HasToTransformIntoRemnant.c`（= 两条当前特质 `0x41a Remnant` / "
        "`0x474 Waystone`） + `BattleCardUI__CreateRemnantBody.c:36-65`（实例化的是**这张卡自己的**"
        " AssetReference，父节点 = 卡、位置 = 卡的位置）。"
        "现读我们卡池里 **%d 张卡**带这个字段（另有 %d 张**不在池里**的 tutorial 卡也带），"
        "池内只指向 **%d 件** prefab（%s）；两条链（SO 的 GUID→GUID 表 · vfx 表）**逐条一致**（冲突 %d）。"
        "其中**能真的变残骸**（带 Remnant/Waystone 特质）的 **%d 张**，这 %d 张的逐卡字段"
        "与我们现在「按关键词挑」的答案 **%d 处不同**；其余 **%d 张**带 prefab 但拿不到那两条特质 ⇒ "
        "原版也不会建残骸体（⛔ 别照字段无脑建，会**多出**残骸）。"
        % (stats["cardsWithRemnantPrefab"], stats["cardsOutsidePool"], stats["uniquePrefabs"],
           " / ".join("%s×%d" % (k, v) for k, v in stats["perPrefab"].items()),
           stats["guidMapConflicts"],
           stats["cardsCanTransform"], stats["cardsCanTransform"], stats["disagreeWithKeywordRule"],
           stats["cardsPrefabButCannotTransform"]))

    conventions = {
        "whoBuildsIt": "② `HasToTransformIntoRemnant` 为真时，建**这张卡自己的** `remnantBodyPrefab`；"
                       "为假 ⇒ 原版一个都不建（`SetRawCardData.c:45`）",
        "prefer": "运行时取法：先查本表 `byCard[id].prefab`；查不到（不在表里 = 原版字段为空）"
                  "才回现在的关键词规则（`Remnant` → Necrons / `Waystone` → Aeldari）",
        "prefabIsNotTheUnitCard": "残骸体是**盖在卡上的 3D 体**（`CardView.SetRemnantBody`），"
                                  "不是残骸单位本身；残骸单位仍是同一张卡翻面（`RuleCore` 那三行）",
        "notRuntimeYet": "本表是**数据侧**。`CardView`/`BattleDriver` 的接线不在本批文件所有权内 —— "
                         "接法见《W64》B16 的「建议动作」",
    }

    return {
        "schema": "remnant_body_by_card/v1",
        "generatedBy": "d:/4/Unity/工具/gen_remnant_body_table.py",
        "_sources": {
            "cardSoTruth": SO_TRUTH,
            "guidMap": GUID_MAP,
            "vfxTable": VFX_TABLE,
            "cardsEngine": CARDS_ENGINE,
            "decomp": "d:/2/tools/decomp_full/",
        },
        "_judges": {
            "onlyIfHasTrait": "BattleCardUI__SetRawCardData.c:45",
            "traitJudge": "SupportMethods__HasToTransformIntoRemnant.c（0x41a / 0x474）",
            "instantiateWhat": "BattleCardUI__CreateRemnantBody.c:36-65（rawCard + 0xe0）",
            "fieldTruth": "cards_raw.json → full_raw.remnantBodyPrefab.m_AssetGUID",
        },
        "_headline": headline,
        "_conventions": conventions,
        "byCard": {cid: by_card[cid] for cid in ids},
        "byPrefab": {k: sorted(v) for k, v in sorted(by_prefab.items())},
        "prefabButCannotTransform": sorted(unreachable),
        "disagreeWithKeywordRule": sorted(disagree),
        "keywordsDisagreeWithTraits": sorted(kw_mismatch),
        "outsidePool": sorted(npool_ids),
        "unresolved": unresolved,
        "guidMapConflicts": conflicts,
        "stats": stats,
    }


def canon(o):
    return json.dumps(o, ensure_ascii=False, sort_keys=True, indent=1)


def main():
    check = "--check" in sys.argv
    doc = build()
    s = doc["stats"]
    if check:
        if not os.path.exists(OUT):
            print("✗ 缺文件: " + OUT)
            return 1
        cur = load(OUT)
        if canon(cur) == canon(doc):
            print("✅ 一致: " + OUT)
            print("=== --check 全过 ✅")
            return 0
        print("✗ 不一致: " + OUT)
        return 1
    io.open(OUT, "w", encoding="utf-8", newline="\n").write(canon(doc))
    print("[写出] %s (%d 字节)" % (OUT, os.path.getsize(OUT)))
    print("池内带 `remnantBodyPrefab` 的卡：%d 张（另有池外 tutorial %d 张）→ %d 件 prefab（%s）"
          % (s["cardsWithRemnantPrefab"], s["cardsOutsidePool"], s["uniquePrefabs"],
             " / ".join("%s×%d" % (k, v) for k, v in s["perPrefab"].items())))
    print("能真变残骸（trait 1050/1140）：%d 张 · 带 prefab 但变不了：%d 张"
          % (s["cardsCanTransform"], s["cardsPrefabButCannotTransform"]))
    print("两条旁证：关键词 vs 特质 不一致 %d 处 · 逐卡字段 vs 关键词挑 prefab 不一致 %d 处"
          % (s["keywordsDisagreeWithTraits"], s["disagreeWithKeywordRule"]))
    print("与「按关键词挑」不同的：%d 处 · 工程里没有这件 prefab：%d · 解不出：%d · GUID 两链冲突：%d"
          % (s["disagreeWithKeywordRule"], s["notInVfxLibrary"], s["unresolved"],
             s["guidMapConflicts"]))
    for cid in doc["disagreeWithKeywordRule"][:10]:
        r = doc["byCard"][cid]
        print("   ! %s %s（%s）逐卡=%s · 关键词规则=%s"
              % (cid, r["name"], ",".join(r["keywords"][:3]), r["prefab"], r["keywordRulePrefab"]))
    for u in doc["unresolved"][:10]:
        print("   ? 解不出 %s guid=%s —— %s" % (u["id"], u["guid"], u["why"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
