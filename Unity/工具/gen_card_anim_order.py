#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_card_anim_order.py — 逐卡逐技能演出表的生成器（**全量件** + **运行期瘦身投影** 两件事）。

历史：全量件 `Unity/数据/游戏数据/card_anim_order.json` 最初由 `A6`（2026-10-16）的**一次性脚本**
（仓外 `d:/tmp/a6/build.py`，没入库）生成。本文件把它**收进仓库并参数化**，同时保留原有的
「全量件 → 运行期瘦身版」投影。`B99_A2`（2026-10-30）补入库 + 按下述口径重新生成。

两个产物
--------
1) **全量件**（默认目标）：`Unity/数据/游戏数据/card_anim_order.json`
   - 基底 = 2022 快照 `cards_1195_2022时代.json`（与 A6 逐字节可复现：897 卡 / 1,468 引用）。
   - **增量** = 当前 build 的 `cards_raw.json`（`资料/普查产出_1018/allcards_1350/`）**顶层** `activeAbility`
     里、基底没有的那些卡（现读 31 卡 / 59 拍）—— 只补 `activeAbility` 这一路（`活跃能力` 那一拍），
     `cardAbilities`（别的时机）不补（与基底里这 31 卡「2022 时还没有」的历史一致；且运行期不消费）。
     🔴 判据 = `B99_R3a_施工单` AN1·R3：取 `cards_raw.json` **顶层** `activeAbility`
     `activeAbilityLogic[*].cardAnimations[*].m_AssetGUID`，⛔ **别读 `full_raw`**（`cards_raw` 里每张卡
     同时有两份同构技能树，`full_raw` 那份会让一切统计翻倍）。
   - 动画时长/位置参数 = `assets_full` 里 CardAnim MonoBehaviour 的 `animInfo`（判据正本）。

2) **运行期瘦身版**：`Unity/MyGame/Assets/CardPresentation/Resources/card_anim_order.json`
   - 由**全量件**裁出来（只取 `activeAbility`、按卡池过滤、字段改短名、join 掉 `cardAnims`）。
   - 见下面「瘦身方案」那段（2026-10-17/10-19 的既有口径，未变）。

## 全量件取数口径（逐条 · 与 A6 的 build.py 同源）
- 每张卡的「顺序」= 原版 `AbilityLogic.cardAnimations` 的**数组序**（原版 `foreach` 全播、不挑下标）。
- 每拍的「时长」= `AnimInfo.GetAnimDuration()` = `TimeMoving + TimeAtStartPos + TimeAtEndPos + Delay`
  （⛔ `StartDelay` 不在内）。四个分量逐字来自 CardAnim 资产的 `animInfo`。
- `vfx` = `animInfo.animAdressable.m_AssetGUID` → `guid_to_asset` 的资产名（= 特效 prefab 名）。
- 解析不出 / 原版就没绑 ⇒ **如实留空 + 写 `why`，⛔ 不填替代值**（`noGuess` 口径）。

## 幂等 / 字节稳定
- 两次 `--write` 逐字节相同（无时间戳、无随机；所有集合迭代前 `sorted`；字典插入序来自固定文件）。
- `--check` 只比对不写盘；`--probe-base` 只验证「基底是否仍能复现盘上那 897 卡」。

## 用法（🔴 不带参 = 只打印本用法并退出，**绝不写盘**）
    python -I -X utf8 工具/gen_card_anim_order.py --check         # 全量件：只对账（不写盘）
    python -I -X utf8 工具/gen_card_anim_order.py --write         # 全量件：重新生成（写盘）
    python -I -X utf8 工具/gen_card_anim_order.py --probe-base    # 只验证基底可复现（不写盘）
    python -I -X utf8 工具/gen_card_anim_order.py --check-slim    # 运行期瘦身版：只对账
    python -I -X utf8 工具/gen_card_anim_order.py --write-slim    # 运行期瘦身版：重新生成
    -h, --help  同「不带参」：只打印本用法并退出（退出码 0）。
"""
import io
import json
import os
import re
import csv
import sys
import collections

ROOT = "d:/4/Unity"

# ---- 全量件：输入（只读） ----
ASSETS = "d:/2/新解包资源/assets_full"
TRUTH = "d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json"   # 2022 快照（基底）
RAW_CARDS = ROOT + "/资料/普查产出_1018/allcards_1350/cards_raw.json"  # 当前 build（增量源）
AMAP = ROOT + "/数据/索引/anim_address_map.json"
CAMP = ROOT + "/数据/游戏数据/card_anim_map.json"
FACT = ROOT + "/数据/游戏数据/factions.json"
GMAP = ROOT + "/资料/普查产出_第二十七会话/guid_map.tsv"
ENUM_STUBS = "d:/2/Warpforge_code/Scripts/Assembly-CSharp"
# ---- 全量件：输出 ----
FULL_OUT = ROOT + "/数据/游戏数据/card_anim_order.json"
# ---- 运行期瘦身版：输入 = 全量件；输出 ----
SRC = FULL_OUT                                   # 兼容旧引用：瘦身版的输入
DST = ROOT + "/MyGame/Assets/CardPresentation/Resources/card_anim_order.json"
POOL = ROOT + "/MyGame/Assets/RuleEngine/Resources/cards_engine.json"

CARDANIM_SCRIPT = 8650191106899952245
PARAM_KEYS = ["startPosOption", "endPosOption", "pointTowardsOption", "vfxDelayTime", "startDelay",
              "timeAtStartPos", "timeMoving", "timeAtEndPos", "shouldMoveVFX", "waitAnimToApplyEffect"]
# 阵营 id → card_anim_map.json 的键（A6 build.py 逐字照抄；90/0 = 无键）
ID2KEY = {10: "Ultramarines", 20: "Orks", 30: "Aeldari", 40: "Necrons", 50: "BlackLegion",
          60: "Tyranids", 70: "Tau", 80: "Sororitas", 90: None, 100: "AstraMilitarum",
          110: "DarkAngels", 120: "EmperorsChildren", 130: "SpaceWolves", 0: None}


# ======================================================================
# 通用
# ======================================================================
def rj(p):
    return json.load(io.open(p, encoding="utf-8"))


def dumps(o):
    """稳定序列化（无时间戳/无随机；调用方保证字典插入序稳定）。"""
    return json.dumps(o, ensure_ascii=False, indent=1, sort_keys=False)


def load_enum_stub(name, root=ENUM_STUBS):
    """逐字复用 `工具/gen_card_vfx_by_card.py`（同一份判据，不另写一套）。"""
    p = os.path.join(root, name + ".cs")
    if not os.path.isfile(p):
        return {}
    body = re.search(r"\benum\s+%s\s*\{(.*?)\n\}" % re.escape(name),
                     io.open(p, encoding="utf-8", errors="replace").read(), re.S)
    if not body:
        return {}
    out = {}
    for m in re.finditer(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(-?\d+)\s*,", body.group(1), re.M):
        out[int(m.group(2))] = m.group(1)
    return out


# ======================================================================
# 全量件：构建
# ======================================================================
def _truth_rows(truth):
    """基底行：2022 快照的 activeAbility + cardAbilities（与 A6 顺序逐字节一致）。"""
    rows = []
    for t in truth:
        cid = t.get("uniqueId")
        army = t.get("cardArmy")
        aa = t.get("activeAbility") or {}
        for li, lg in enumerate(aa.get("activeAbilityLogic") or []):
            for i, ca in enumerate(lg.get("cardAnimations") or []):
                rows.append(dict(cardId=cid, armyId=army, path="activeAbility", ai=0, li=li,
                                 trig=None, chosen=lg.get("chosenAbility"), i=i,
                                 guid=(ca or {}).get("m_AssetGUID") or ""))
        for ai, ab in enumerate(t.get("cardAbilities") or []):
            for li, lg in enumerate(ab.get("abilityLogic") or []):
                for i, ca in enumerate(lg.get("cardAnimations") or []):
                    rows.append(dict(cardId=cid, armyId=army, path="cardAbilities", ai=ai, li=li,
                                     trig=ab.get("trigger"), chosen=lg.get("chosenAbility"), i=i,
                                     guid=(ca or {}).get("m_AssetGUID") or ""))
    return rows


def _augment_rows(raw, base_ids):
    """增量行：当前 build 里、基底没有的卡，只补 **activeAbility** 一路。

    返回 (per_card_rows, stats)。按卡 id 排序 ⇒ 与 raw 的文件序无关，稳定。
    🔴 只读顶层 `activeAbility`，⛔ 不碰 `full_raw`。
    """
    by_card = {}
    for c in raw.get("cards") or []:
        cid = c.get("uniqueId")
        if not cid or cid in base_ids:
            continue
        rows = []
        aa = c.get("activeAbility") or {}
        for li, lg in enumerate(aa.get("activeAbilityLogic") or []):
            for i, ca in enumerate(lg.get("cardAnimations") or []):
                rows.append(dict(cardId=cid, armyId=c.get("cardArmy"), path="activeAbility", ai=0, li=li,
                                 trig=None, chosen=lg.get("chosenAbility"), i=i,
                                 guid=(ca or {}).get("m_AssetGUID") or ""))
        if not [r for r in rows if r["guid"]]:
            continue  # 当前 build 这张卡也没有 activeAbility 动画 ⇒ 不补
        by_card[cid] = (rows, c.get("cardName"))
    stats = {"cards": 0, "rows": 0, "blank": 0}
    out = []
    for cid in sorted(by_card):
        rows, _ = by_card[cid]
        out.extend(rows)
        stats["cards"] += 1
        stats["rows"] += len(rows)
        stats["blank"] += len([r for r in rows if not r["guid"]])
    names = {cid: by_card[cid][1] for cid in by_card}
    return out, names, stats


def _resolve_cardanim(g, g2a, gmap):
    v = g2a.get(g)
    if v and v.get("type") == "MonoBehaviour":
        return v.get("name"), v.get("bundle"), v.get("dump"), "A:anim_address_map.guid_to_asset"
    recs = gmap.get(g)
    if recs:
        names = {r["name"] for r in recs}
        if len(names) == 1:  # 多行不同名 ⇒ 出声放弃，⛔ 不猜
            nm = names.pop()
            b = recs[0].get("bundle") or ""
            d = ("bundle_" + b[:-7]) if b.endswith(".bundle") else b
            return nm, d, d + "/MonoBehaviour/" + nm + ".json", "B:guid_map.tsv(非 GUID 容器键)"
    return None, None, None, None


def _dump_animinfo(rel):
    if not rel:
        return None, None
    p = os.path.join(ASSETS, rel)
    if not os.path.isfile(p):
        return None, None
    js = rj(p)
    if (js.get("m_Script") or {}).get("m_PathID") != CARDANIM_SCRIPT:
        return None, None
    return js.get("animInfo") or {}, js.get("m_Name")


def _load_gmap():
    gmap = collections.defaultdict(list)
    if os.path.isfile(GMAP):
        with io.open(GMAP, encoding="utf-8") as fh:
            for r in csv.DictReader(fh, delimiter="\t"):
                if r.get("assetGuid") and r.get("className") == "CardAnim" and r.get("name"):
                    gmap[r["assetGuid"]].append(r)
    return gmap


def _resolve_all(ref_guids, g2a, gmap, camp, map_keys):
    cardanims = []
    byguid = {}
    stat = collections.Counter()
    for g in ref_guids:
        nm, dirn, rel, how = _resolve_cardanim(g, g2a, gmap)
        if not nm:
            stat["unresolved"] += 1
            continue
        ai, real = _dump_animinfo(rel)
        if ai is None:
            stat["noAnimInfo"] += 1
            continue
        vg = (ai.get("animAdressable") or {}).get("m_AssetGUID") or ""
        vv = g2a.get(vg) or {}
        name = real or nm
        rec = {"guid": g, "name": name, "srcBundle": dirn, "srcMono": rel, "resolvedBy": how,
               "inCardAnimMapKeys": [k for k in map_keys if name in camp[k]] or None,
               "vfx": vv.get("name") or None, "vfxGuid": vg or None, "vfxType": vv.get("type") or None,
               "params": {k: ai.get(k) for k in PARAM_KEYS}, "animInfo": ai}
        cardanims.append(rec)
        byguid[g] = rec
        stat[how] += 1
    return cardanims, byguid, stat


def _assemble_cards(rows, names, trig_names, eff_names, byguid):
    cards = []
    idx = {}
    for r in rows:
        if r["cardId"] not in idx:
            idx[r["cardId"]] = len(cards)
            cards.append({"id": r["cardId"], "name": None, "armyId": r["armyId"],
                          "armyKey": ID2KEY.get(r["armyId"]), "abilities": []})
    for cid, nm in names.items():
        if cid in idx:
            cards[idx[cid]]["name"] = nm
    for r in rows:
        c = cards[idx[r["cardId"]]]
        dest = c["abilities"]
        key = (r["path"], r["ai"], r["li"])
        if not dest or (dest[-1]["path"], dest[-1]["abilityIndex"], dest[-1]["logicIndex"]) != key:
            dest.append({"path": r["path"], "abilityIndex": r["ai"], "logicIndex": r["li"],
                         "trigger": r["trig"],
                         "triggerName": trig_names.get(r["trig"]) if r["trig"] is not None else None,
                         "chosenAbility": r["chosen"],
                         "chosenAbilityName": eff_names.get(r["chosen"]) if r["chosen"] is not None else None,
                         "steps": []})
        s = {"i": r["i"], "ca": r["guid"] or "", "why": None}
        if not r["guid"]:
            s["why"] = "γ 原版这条的 m_AssetGUID 是空串（原版自己就没绑；PlayAnim:62-64 会跳过这一拍）"
        elif r["guid"] not in byguid:
            s["why"] = "α 两个解析源都解不出这个 CardAnim GUID"
        dest[-1]["steps"].append(s)
    return cards


def _cov(rows, byguid, camp, id2key, subset=None):
    rs = subset if subset is not None else rows
    ok = blank = unres = noparam = 0
    for r in rs:
        if not r["guid"]:
            blank += 1
            continue
        c = byguid.get(r["guid"])
        if not c:
            unres += 1
            continue
        fk = id2key.get(r["armyId"])
        if fk and c["name"] in camp.get(fk, {}):
            ok += 1
        else:
            noparam += 1
    return dict(total=len(rs), hit=ok, blankGuid=blank, unresolvedGuid=unres,
                namedButNotInFactionMap=noparam)


def _pool_coverage(pool, cards, byguid):
    """全池（cards_engine）视角：多少张卡「能取到时长」。"""
    cmap = {c["id"]: c for c in cards}
    total = len(pool)
    resolved = unresolved = no_ab = absent = 0
    for pc in pool:
        cid = pc.get("id")
        c = cmap.get(cid)
        if not c:
            absent += 1
            continue
        steps = [st for ab in c["abilities"] if ab["path"] == "activeAbility" for st in ab["steps"]]
        nz = [st for st in steps if st["ca"]]
        if not nz:
            no_ab += 1
            continue
        if any(st["ca"] in byguid for st in nz):
            resolved += 1
        else:
            unresolved += 1
    return dict(poolCards=total, withResolvedActiveAnim=resolved,
                refsButUnresolved=unresolved, noActiveAbilityAnim=no_ab,
                absentFromTable=absent)


def build_full(augment=True):
    """建全量件。返回 (obj, probe)；probe 是给 `--probe-base` 用的基底指纹。"""
    truth = rj(TRUTH)
    amap = rj(AMAP)
    camp = rj(CAMP)
    fact = rj(FACT)
    g2a = amap["guid_to_asset"]
    id2name = {f["id"]: f["name"] for f in fact["factions"]}
    trig_names = load_enum_stub("AbilityTrigger")
    eff_names = load_enum_stub("AbilityEffect")
    map_keys = [k for k in camp if k != "_guid_summary"]
    gmap = _load_gmap()

    base_rows = _truth_rows(truth)
    base_ids = {r["cardId"] for r in base_rows}
    base_names = {t.get("uniqueId"): t.get("cardName") for t in truth}

    aug_rows, aug_names, aug_stats = ([], {}, {"cards": 0, "rows": 0, "blank": 0})
    if augment:
        raw = rj(RAW_CARDS)
        aug_rows, aug_names, aug_stats = _augment_rows(raw, base_ids)

    rows = base_rows + aug_rows
    names = dict(base_names)
    names.update(aug_names)

    n_base = len(rows) - len(aug_rows)
    base_refs = sorted({r["guid"] for r in base_rows if r["guid"]})
    all_refs = sorted({r["guid"] for r in rows if r["guid"]})

    cardanims, byguid, stat = _resolve_all(all_refs, g2a, gmap, camp, map_keys)
    base_cardanims, base_byguid, base_stat = _resolve_all(base_refs, g2a, gmap, camp, map_keys)

    cards = _assemble_cards(rows, names, trig_names, eff_names, byguid)

    # ---- 覆盖率（基底口径 + 全量口径都留）----
    cb_only = [r for r in rows if r["path"] == "cardAbilities"]
    noname_key = {k: id2name[k] for k in ID2KEY}
    id2key_tyr = dict(ID2KEY)
    id2key_tyr[90] = "Tyranids"
    multi = 0
    tot_ab = 0
    hist = collections.Counter()
    for c in cards:
        for ab in c["abilities"]:
            tot_ab += 1
            n = len(ab["steps"])
            hist[n] += 1
            if n > 1:
                multi += 1
    waitbeats = sum(1 for r in rows if r["guid"] and r["guid"] in byguid
                    and not byguid[r["guid"]]["vfxGuid"])
    pool = rj(POOL)["cards"]
    coverage = {
        "cardAnimationsRefs": len(rows),
        "byPath": {"cardAbilities": len(cb_only), "activeAbility": len(rows) - len(cb_only)},
        "cardsWithRefs": len(cards),
        "abilityRows": tot_ab,
        "abilityRowsWithMoreThanOneStep": multi,
        "stepsHistogram": {str(k): v for k, v in sorted(hist.items())},
        "cardAnimAssetsReferenced": len(all_refs),
        "cardAnimAssetsResolved": len(cardanims),
        "resolveStats": dict(stat),
        "stepsWithoutVfx_pureWaitBeats": waitbeats,
        "paramsThisFile": {"hit": sum(1 for r in rows if r["guid"] and r["guid"] in byguid),
                           "blankGuid": sum(1 for r in rows if not r["guid"]),
                           "unresolvedGuid": sum(1 for r in rows if r["guid"] and r["guid"] not in byguid)},
        "paramsViaCardAnimMap_BEFORErename": _cov(rows, byguid, camp, noname_key),
        "paramsViaCardAnimMap_AFTERrename": _cov(rows, byguid, camp, ID2KEY),
        "paramsViaCardAnimMap_AFTERrename_cardAbilitiesOnly": _cov(rows, byguid, camp, ID2KEY, cb_only),
        "paramsViaCardAnimMap_BEFORErename_cardAbilitiesOnly": _cov(rows, byguid, camp, noname_key, cb_only),
        "paramsViaCardAnimMap_ifGenestealersToTyranids": _cov(rows, byguid, camp, id2key_tyr)["hit"],
        "paramsViaCardAnimMap_byBundleKey_noNameGuess": _bundle_key_cov(rows, byguid),
        "pool": {"cards": len(pool)},
        "poolActiveAbility": _pool_coverage(pool, cards, byguid),
        "augment": {
            "source": "资料/普查产出_1018/allcards_1350/cards_raw.json（当前 build · 顶层 activeAbility）",
            "base": "d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json（2022 · 897 卡）",
            "baseRefs": n_base,
            "augmentCards": aug_stats["cards"],
            "augmentRows": aug_stats["rows"],
            "augmentBlankGuids": aug_stats["blank"],
            "augmentNewCardAnims": len(cardanims) - len(base_cardanims),
            "note": "只补 activeAbility 一路；cardAbilities 不补（运行期不消费，且这些卡 2022 时尚未入库）。",
        },
    }

    out = {
        "_schema": 1,
        "_generatedBy": ("A6（T05 第 1 步）· 2026-10-16 一次性脚本建底；"
                         "B99_A2 · 2026-10-30 收进 `工具/gen_card_anim_order.py` + 按 AN1·R3 增量补 31 卡（activeAbility）。"
                         "只读输入 · 未跑 Unity · 未动 git。"),
        "_what": "「带顺序」的逐卡逐技能演出步骤表。abilities[].steps[] = 原版 AbilityLogic.cardAnimations 的数组序（原版 foreach 全播、不挑下标）。",
        "_sources": {
            "cardSOTruth": TRUTH + "（1,195 张 · 2022 快照 · 基底）",
            "cardSOAugment": RAW_CARDS + "（当前 build · 只取顶层 activeAbility · 增量）",
            "guidToAsset": "数据/索引/anim_address_map.json.guid_to_asset（只索引「以 GUID 为容器键」的条目）",
            "guidMap": GMAP + "（补 guid_to_asset 漏掉的【非 GUID 容器键】）",
            "factionParams": CAMP + "（只覆盖 12 个阵营 bundle；见 _coverage）",
            "cardAnimDump": ASSETS + "/bundle_*cardanims_assets_all/MonoBehaviour/*.json（判据正本 = AnimInfo 全字段）",
            "enumStubs": ENUM_STUBS + "/{AbilityTrigger,AbilityEffect}.cs（与 gen_card_vfx_by_card.py 同一份）",
            "consumer": None,
        },
        "_conventions": {
            "shape": "cards / cardAnims 是【数组】（本仓既有约定：JsonUtility 解不了 Dictionary）。_* 三段是给人看的元数据，驱动侧别拿 JsonUtility 解它。",
            "stepOrder": "steps[i].i 升序 = 原版 cardAnimations 的 List 下标序。",
            "stepFields": "steps[] = {i 下标, ca CardAnim 资产 GUID（空串 = 原版没绑）, why 解不出时的原因（正常为 null）} ⇒ 拿 ca 去 cardAnims[] 查参数/特效。",
            "stepCaNotGlobal": "ca = CardAnim 资产 GUID；cardAnims[].guid 是同一把键。params/animInfo 都在 cardAnims[] 上（去重、一份）。",
            "runtimeSkip": "原版 AbilityLogic__PlayAnim.c 两处逐拍跳过：① AssetReference 无效（:62-64）② start/end/pointTowards ∈ {14,15,16,17} 且空位不可用（:104-169 MinionManager.IsAvailableSlot ⇒ goto 下一拍）⇒ 表里的 N 拍是【上限】。",
            "targetMultiplex": "三个位置选项里【任一 == 2（abilityTarget）】⇒ 该拍按 abilityTargets 列表逐目标各播一份（PlayAnim:171-248）。",
            "waitForAnim": "是否等这拍播完再结算 = 该拍自己的 AnimInfo.waitAnimToApplyEffect(0x58)；多目标分支里再 && (是否最后一个目标)（PlayAnim:217-222）。⛔ 不是「只有最后一拍才 wait」。",
            "pureWaitBeats": "animAdressable 为空、vfx 为 null 的 cardAnim（Wait_*）是【纯延时拍】：没有特效，时长在 timeMoving、waitAnimToApplyEffect=1。⛔ 别把它当「解不出」丢掉，它的时长要照算（原版用它在两拍之间插停顿）。",
            "vfx": "cardAnims[].vfx = AnimInfo.animAdressable(0x10) → guid_to_asset 的名字（= 特效 prefab 名）；vfx 为 null 且 vfxGuid 非空 = 那个 prefab 的 guid 也不在本地索引里。",
            "paramsVerbatim": "cardAnims[].animInfo = 原版 CardAnim dump 的 animInfo 逐字照抄（~20 字段，未裁剪）；params 是其中 10 个核心字段的扁平副本（便于 JsonUtility 直接解）。",
            "noGuess": "γ（原版空 GUID）/ α（解析不出）两类留空 + 写 why，⛔ 不填替代值。",
            "augmentScope": "增量只补「当前 build 有、2022 基底没有」的卡的 activeAbility；不回改基底里已有的行（含 SOR13 的 2022 vs 当前 build 差异，见报告）。",
        },
        "_coverage": coverage,
        "cardAnims": cardanims,
        "cards": cards,
    }
    probe = {"cards": cards, "cardAnims": cardanims,
             "baseCardAnims": base_cardanims, "stat": dict(stat), "baseStat": dict(base_stat),
             "aug": aug_stats}
    return out, probe


def _bundle_key_cov(rows, byguid):
    hit = miss = 0
    for r in rows:
        c = byguid.get(r["guid"])
        if not c:
            continue
        if c["inCardAnimMapKeys"]:
            hit += 1
        else:
            miss += 1
    return dict(hitAnyFactionKey=hit, notInAnyKey=miss)


# ======================================================================
# 运行期瘦身版（既有口径，未改）
# ======================================================================
def _curve_flat(curve):
    """`AnimationCurve`（全量件里的序列化形状）→ 扁平 `[t0,v0,t1,v1,…]`（按 time 升序）。"""
    cur = ((curve or {}).get("m_Curve") or [])
    out = []
    for k in sorted(cur, key=lambda x: x.get("time", 0.0)):
        out.append(round(float(k.get("time") or 0.0), 4))
        out.append(round(float(k.get("value") or 0.0), 4))
    return out


def _ease_flat(ai):
    return _curve_flat(ai.get("easeCurve"))


def slim_beat(step, anims, stats):
    ca = (step.get("ca") or "").strip()
    if not ca:
        stats["blank_guid"] += 1
        return None
    a = anims.get(ca)
    if a is None:
        stats["unresolved_guid"] += 1
        return None
    p = a.get("params") or {}
    ai = a.get("animInfo") or {}
    vfx = a.get("vfx")
    if vfx is None:
        stats["pure_wait"] += 1
    mv = 1 if ai.get("shouldMoveVFX") else 0
    if mv:
        stats["movers"] += 1
    b = {
        "ca": ca,
        "vfx": vfx if vfx is not None else "",
        "sp": int(p.get("startPosOption") or 0),
        "ep": int(p.get("endPosOption") or 0),
        "pt": int(p.get("pointTowardsOption") or 0),
        "vd": float(p.get("vfxDelayTime") or 0.0),
        "ts": float(p.get("timeAtStartPos") or 0.0),
        "tm": float(p.get("timeMoving") or 0.0),
        "te": float(p.get("timeAtEndPos") or 0.0),
        "w": int(p.get("waitAnimToApplyEffect") or 0),
        "mv": mv,
        "otv": 1 if ai.get("orientToVelocity") else 0,
        "ease": _ease_flat(ai) if mv else [],
    }
    ao = 1 if ai.get("applyMovementOffsets") else 0
    if ao:
        stats["offset_beats"] += 1
        om = ai.get("offsetMovementAxis") or {}
        b["ao"] = 1
        b["ont"] = 1 if om.get("useNonModifiedAnimTime") else 0
        b["ofx"] = _curve_flat(om.get("xAxis"))
        b["ofy"] = _curve_flat(om.get("yAxis"))
        b["ofz"] = _curve_flat(om.get("zAxis"))
        for mn_key, mx_key, range_key in (("mnx", "mxx", "minMaxXOffset"),
                                          ("mny", "mxy", "minMaxYOffset"),
                                          ("mnz", "mxz", "minMaxZOffset")):
            r = om.get(range_key) or {}
            b[mn_key] = round(float(r.get("x") or 0.0), 6)
            b[mx_key] = round(float(r.get("y") or 0.0), 6)
    return b


def build():
    d = json.load(io.open(SRC, encoding="utf-8"))
    anims = {a["guid"]: a for a in d.get("cardAnims") or []}
    pool_name = {}
    try:
        pool = json.load(io.open(POOL, encoding="utf-8"))
        for pc in pool.get("cards") or []:
            if pc.get("id"):
                pool_name[pc["id"]] = pc.get("name") or ""
    except Exception as e:
        print("[gen] ⚠️ 读不到卡池 %s（%s）⇒ 卡名退回原版快照那一份" % (POOL, e))
    stats = {"blank_guid": 0, "unresolved_guid": 0, "pure_wait": 0,
             "cards": 0, "steps": 0, "cards_no_steps": 0,
             "rows_not_active": 0, "steps_not_active": 0,
             "movers": 0, "offset_beats": 0,
             "not_in_pool": 0, "renamed": 0, "poolMissing": 0 if pool_name else 1}
    cards = []
    for c in d.get("cards") or []:
        cid = c.get("id") or ""
        src_name = c.get("name") or ""
        if pool_name and cid not in pool_name:
            stats["not_in_pool"] += 1
            continue
        name = pool_name.get(cid) or src_name
        if name != src_name:
            stats["renamed"] += 1
        steps = []
        for ab in c.get("abilities") or []:
            if (ab.get("path") or "") != "activeAbility":
                stats["rows_not_active"] += 1
                stats["steps_not_active"] += len(ab.get("steps") or [])
                continue
            for st in sorted(ab.get("steps") or [], key=lambda s: s.get("i", 0)):
                b = slim_beat(st, anims, stats)
                if b is not None:
                    steps.append(b)
        if not steps:
            stats["cards_no_steps"] += 1
            continue
        cards.append({"id": cid, "name": name, "steps": steps})
        stats["cards"] += 1
        stats["steps"] += len(steps)
    out = {
        "schema": 1,
        "source": "Unity/数据/游戏数据/card_anim_order.json（A6 · 2026-10-16）；"
                  "卡名以 cards_engine.json 的 id→name 为准（W · 2026-10-19）",
        "filter": "abilities[].path == 'activeAbility'（= 该卡的主动能力；判据见生成器文件头）；"
                  "卡池里没有的卡整条丢弃（运行期永远查不到，且名字会挡住院内同名卡）",
        "note": "运行期瘦身版。字段短名 = CardAnimBeat："
                "id/name = 卡 id + **我们池子的卡名**（`TryGetBeats` 的卡名键就是它）· "
                "ca=CardAnim guid · vfx=prefab 名（空串 = 纯延时拍）· "
                "sp/ep/pt=startPosOption/endPosOption/pointTowardsOption · "
                "vd/ts/tm/te=vfxDelayTime/timeAtStartPos/timeMoving/timeAtEndPos · w=waitAnimToApplyEffect · "
                "mv=shouldMoveVFX · otv=orientToVelocity · ease=easeCurve 的扁平关键帧 [t0,v0,t1,v1,…]"
                "（2026-10-19 · 偏差 6「起→终位移 + 朝向」）· "
                "ao=applyMovementOffsets · ont=offsetMovementAxis.useNonModifiedAnimTime · "
                "ofx/ofy/ofz=三轴偏移曲线（同 ease 的扁平约定）· mnx/mxx/mny/mxy/mnz/mxz=各轴随机幅度区间"
                "（2026-10-19 · 偏差 6「逐轴偏移」；只有 ao==1 的拍写这 11 个字段，其余缺省 0）",
        "stats": stats,
        "cards": cards,
    }
    return out


# ======================================================================
# CLI（🔴 不带参 = 只打印用法并退出，绝不写盘）
# ======================================================================
USAGE = """用法（🔴 不带参 = 只打印本用法并退出，绝不写盘）：
    python -I -X utf8 工具/gen_card_anim_order.py --check         # 全量件：只对账（不写盘）
    python -I -X utf8 工具/gen_card_anim_order.py --write         # 全量件：重新生成（写盘）
    python -I -X utf8 工具/gen_card_anim_order.py --probe-base    # 只验证基底可复现（不写盘）
    python -I -X utf8 工具/gen_card_anim_order.py --check-slim    # 运行期瘦身版：只对账
    python -I -X utf8 工具/gen_card_anim_order.py --write-slim    # 运行期瘦身版：重新生成
    -h, --help  同「不带参」：只打印本用法并退出（退出码 0）。"""


def _read_text(p):
    if not os.path.exists(p):
        return None
    return io.open(p, "rb").read().decode("utf-8")


def _write_text(p, txt):
    # ⚠️ 先算好再写（本仓踩过 `open(...,'wb').write(<表达式>)` 先截断后求值的坑）
    data = txt.encode("utf-8")
    with io.open(p, "wb") as fh:
        fh.write(data)


def _summary(o):
    cov = o["_coverage"]
    pa = cov["poolActiveAbility"]
    return {
        "cards": cov["cardsWithRefs"],
        "abilityRows": cov["abilityRows"],
        "cardAnimationsRefs": cov["cardAnimationsRefs"],
        "cardAnimAssetsReferenced": cov["cardAnimAssetsReferenced"],
        "cardAnimAssetsResolved": cov["cardAnimAssetsResolved"],
        "resolveStats": cov["resolveStats"],
        "stepsBlank": cov["paramsThisFile"]["blankGuid"],
        "stepsUnresolved": cov["paramsThisFile"]["unresolvedGuid"],
        "poolCards": pa["poolCards"],
        "pool_withResolvedActiveAnim": pa["withResolvedActiveAnim"],
        "pool_refsButUnresolved": pa["refsButUnresolved"],
        "pool_noActiveAbilityAnim": pa["noActiveAbilityAnim"],
        "pool_absentFromTable": pa["absentFromTable"],
        "augment": cov["augment"],
    }


def main(argv):
    args = [a for a in argv[1:]]
    if not args or any(a in ("-h", "--help") for a in args):
        print(USAGE)
        return 0

    known = {"--check", "--write", "--probe-base", "--check-slim", "--write-slim"}
    unknown = [a for a in args if a not in known]
    if unknown:
        print("未知参数：%s\n" % " ".join(unknown))
        print(USAGE)
        return 2

    if "--probe-base" in args:
        obj, probe = build_full(augment=False)
        disk = _read_text(FULL_OUT)
        if disk is None:
            print("[probe-base] 盘上没有 %s" % FULL_OUT)
            return 1
        dj = json.loads(disk)
        cards_same = json.loads(dumps(obj["cards"])) == dj.get("cards")
        # cardAnims 的「内容」逐字段 = 原版；只有 `resolvedBy` 是**解析来源标签**，
        #   会随 `anim_address_map.json` 更长而 A↔B 漂移（内容不变）⇒ 内容比对时剔掉它，单独报漂移数。
        def _strip_why(recs):
            out = []
            for r in recs:
                c = dict(r)
                c.pop("resolvedBy", None)
                out.append(c)
            return out
        anims_same = json.loads(dumps(_strip_why(obj["cardAnims"]))) == json.loads(dumps(_strip_why(dj.get("cardAnims") or [])))
        gen_by = {r["guid"]: r.get("resolvedBy") for r in obj["cardAnims"]}
        dis_by = {r["guid"]: r.get("resolvedBy") for r in (dj.get("cardAnims") or [])}
        by_drift = sum(1 for k in gen_by if k in dis_by and gen_by[k] != dis_by[k])
        print("[probe-base] 基底 cards 复现 =", cards_same, "| cardAnims 内容复现 =", anims_same,
              "| baseCards =", len(obj["cards"]), "| baseAnims =", len(obj["cardAnims"]),
              "| resolvedBy 漂移 =", by_drift, "（amap 变长导致，内容不变）")
        return 0 if (cards_same and anims_same) else 1

    if "--check-slim" in args or "--write-slim" in args:
        o = build()
        txt = dumps(o)
        if "--check-slim" in args:
            old = _read_text(DST)
            same = (old == txt)
            print("[check-slim] 幂等 =", same, "| cards =", o["stats"]["cards"],
                  "| steps =", o["stats"]["steps"])
            return 0 if same else 1
        _write_text(DST, txt)
        print("[write-slim] ->", DST)
        print("[write-slim] bytes =", len(txt.encode("utf-8")))
        print("[write-slim] stats =", o["stats"])
        return 0

    # 全量件
    obj, _probe = build_full(augment=True)
    txt = dumps(obj)
    if "--check" in args:
        old = _read_text(FULL_OUT)
        same = (old == txt)
        print("[check] 全量件 幂等 =", same)
        print("[check] 摘要 =", json.dumps(_summary(obj), ensure_ascii=False, indent=1))
        if not same and old is not None:
            newj = json.loads(txt)
            oldj = json.loads(old)
            print("[check] 盘上 cards =", len(oldj.get("cards") or []),
                  "| 生成 cards =", len(newj.get("cards") or []))
            print("[check] 盘上 cardAnims =", len(oldj.get("cardAnims") or []),
                  "| 生成 cardAnims =", len(newj.get("cardAnims") or []))
        return 0 if same else 1

    # --write
    _write_text(FULL_OUT, txt)
    print("[write] ->", FULL_OUT)
    print("[write] bytes =", len(txt.encode("utf-8")))
    print("[write] 摘要 =", json.dumps(_summary(obj), ensure_ascii=False, indent=1))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
