#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
gen_ability_anim_durations.py — 账 `A1767`：把「发动效果的时长」从一条常数换成**逐卡真值**。

产物（同一次运行写两份）：
  ① `d:/4/Unity/数据/游戏数据/ability_anim_durations.json`          —— 全量真值（给工具/人看，含逐条 CardAnim）
  ② `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/ability_anim_durations.json`
                                                                    —— **运行时件**（`EventTiming` 读的就是它）

判据链（逐跳可复核，**不是**按卡名/关键词推）：
  原版卡 SO `activeAbility.activeAbilityLogic[*].cardAnimations[*]`（`AssetReference`，存 GUID）
    → `数据/游戏数据/card_vfx_by_card.json` 的 `byCard[<我方 id>].ability[]`（`conf=authoritative`，
      `abilitySlot` 以 `activeAbility` 开头）—— 那份表已经走完了
      「GUID → 束 → 资产原名」那一跳（判据 → `资料/普查产出_第二十七会话/甲2_GUID到资源_调查.md`）
    → `d:/2/新解包资源/assets_full/bundle_<束>/MonoBehaviour/<CardAnim 名>.json` 的 `animInfo`
    → `AnimInfo.GetAnimDuration()` = `vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`

**为什么是 `activeAbility` 那一支**（原版方法体，不是猜的）：
  · `AbilityLogic__PlayAnim.c` 遍历**该 abilityLogic 自己的** `cardAnimations` 列表，逐条
    `BattleManager.AddCardAnim(...)` 排队（`d:/2/tools/decomp_full/`）；`PlayAnim` 的调用点是
    `AbilityLogic__PlayAbility.c:273`，而 `PlayAbility` 只被 `TriggerAbility` / `ResolveActiveAbility` /
    `ResolveAbilities` 调。
  · 排在末尾那一条若 `animInfo.waitAnimToApplyEffect != 0` 才 `bVar13 = true`（`PlayAnim.c` 尾段），
    然后由 `_ResolveAnim` 播：`BattleManager._ResolveAnim_d__466__MoveNext.c` 里
    `WaitForCompletion(BuildSequence(tweenAnim))`（**仅当** `tweenAnim != null && waitAnimation != 0`）
    之后**一定**再 `WaitForSeconds(AnimInfo.GetAnimDuration())`。
  · `CardAbility.trigger` 那一族（`cardAbilities[*].abilityLogic[*]`，`ThisCardPlayed` / `Slay` / …）
    是**别的时机**播的（我们的 `EvtKind.Trigger` / `Play`），**不进这张表** —— 全量件里留在
    `cards[*].otherAbilityAnims` 里备查。

⚠️ **本表覆盖到当前 build 的卡 SO**：真值源 = `资料/普查产出_1018/allcards_1350/cards_raw.json`
   （**2026-10-18 换**；旧的是 2025-07-30 的 `cards_1195_2022时代.json` 快照，⛔ 别改回去）。
   仍有少量「会发 Ability 事件」的卡没有真值 —— 原因**不是快照年代**，而是它们的卡 SO
   `activeAbility` 本身**一条 CardAnim 都没有**（铁律 11 例外①）。逐张名单见 `uncovered`。

用法：
  python -I -X utf8 d:/4/Unity/工具/gen_ability_anim_durations.py            # 重出两份产物
  python -I -X utf8 d:/4/Unity/工具/gen_ability_anim_durations.py --check    # 只比不写（幂等自检）
"""

import io
import json
import os
import re
import sys

ROOT4 = "d:/4/Unity"
ROOT2 = "d:/2"

VFX_TABLE = ROOT4 + "/数据/游戏数据/card_vfx_by_card.json"
CARDS_ENGINE = ROOT4 + "/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
ASSETS_FULL = ROOT2 + "/新解包资源/assets_full"

# 🔴 **真值源 = 当前 build 的 allcards 卡 SO 语料**（2026-10-18 换）。
#   ⛔ 旧路径 `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json` 是 **2025-07-30 的旧快照**
#   （1,195 张，UM/SW 那几批不在里面）—— **别改回去**。
#   判据 → `资料/普查产出_1018/R_卡牌线_完整ID与SO后的调整评估.md` §4。
#   ⚠️ 它不是本表时长的来源（时长来自下面的 card_vfx_by_card → CardAnim dump 那条链）——
#      它只用来**如实判定**「这张卡在不在真值里」「SO 的 activeAbility 到底有没有 CardAnim」。
CARD_SO_TRUTH = ROOT4 + "/资料/普查产出_1018/allcards_1350/cards_raw.json"

# 🔴 2026-10-22（`W64 · B17`）：`alternativeAbility` 那一支的 GUID → CardAnim 解析索引。
#   原版 `RawCardScript.GetActiveAbility` = **二选一**：
#     `hasAlternativeAbility(+0x2aa) && BoardAnalysis.MeetsCriteria(+0x2b0)` ⇒ 返回**替代能力**(+0x2b8)，
#     否则返回普通 `activeAbility`(+0x260)（方法体 → `d:/2/tools/decomp_full/RawCardScript__GetActiveAbility.c`）。
#   `card_vfx_by_card.json` 的 `ability` 桶**只覆盖 `activeAbility`**（`abilitySlot` 以它开头），
#   所以替代那一支得单独解析一遍（现读全库只有 **1 张**卡真有这条：`SW21`）。
GUID_MAP_TSV = ROOT4 + "/资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv"

OUT_FULL = ROOT4 + "/数据/游戏数据/ability_anim_durations.json"
OUT_RT = ROOT4 + "/MyGame/Assets/CardPresentation/Resources/ability_anim_durations.json"

# 四字段之和的中位（旧常数）—— 生成器会把**本表的**中位算出来写进产物，供自检核对
FALLBACK = 1.0


def load(p):
    return json.load(io.open(p, encoding="utf-8"))


def anim_duration(ai):
    """`AnimInfo.GetAnimDuration`（`AnimInfo__GetAnimDuration.c`，偏移 0x24/0x2C/0x30/0x34 —— 逐项对应
    `vfxDelayTime` / `timeAtStartPos` / `timeMoving` / `timeAtEndPos`；**不含** `startDelay`）。"""
    return (float(ai.get("vfxDelayTime") or 0.0)
            + float(ai.get("timeAtStartPos") or 0.0)
            + float(ai.get("timeMoving") or 0.0)
            + float(ai.get("timeAtEndPos") or 0.0))


def dump_path(bundle, name):
    if not bundle or not name:
        return None
    folder = "bundle_" + bundle[:-len(".bundle")] if bundle.endswith(".bundle") else "bundle_" + bundle
    return os.path.join(ASSETS_FULL, folder, "MonoBehaviour", name + ".json")


def load_guid_index():
    """`guid_map_bundles.tsv` → `{guid: (资产名, 束文件, 类名)}`（同 `gen_cards_engine.load_anim_guid_index`：
    同一 GUID 多行时**保留第一行**）。只给 `alternativeAbility` 那一支用（见 `GUID_MAP_TSV` 的注释）。"""
    idx = {}
    if not os.path.isfile(GUID_MAP_TSV):
        print("⚠️ 读不到 GUID 表 %s ⇒ `alternativeAbility` 那一支**判不了**（会在产物里如实标）"
              % GUID_MAP_TSV)
        return idx
    with io.open(GUID_MAP_TSV, encoding="utf-8") as f:
        for line in f:
            p = line.rstrip("\n").split("\t")
            if len(p) < 9 or not p[3] or not p[8]:
                continue
            idx.setdefault(p[3], (p[8], p[4], p[7]))
    return idx


def so_ability_anims(so_row, key):
    """卡 SO 里 `activeAbility` / `alternativeAbility` 的**非空** CardAnim GUID 清单（保序）。"""
    out = []
    for lg in ((so_row or {}).get(key) or {}).get("activeAbilityLogic") or []:
        for an in (lg.get("cardAnimations") or []):
            g = ((an or {}).get("m_AssetGUID") or "").strip()
            if g:
                out.append(g.lower())
    return out


def build():
    vfx = load(VFX_TABLE)
    ce = load(CARDS_ENGINE)["cards"]
    byid = {c["id"]: c for c in ce}
    name_of = {c["id"]: c.get("name") or "" for c in ce}

    cards = []
    unresolved = []
    unknown_card = []          # byCard 里的键不在我们卡池（不该发生，发生就报）
    for cid, e in vfx["byCard"].items():
        act, other = [], []
        for it in (e.get("ability") or []):
            if it.get("conf") != "authoritative":
                continue
            slot = it.get("abilitySlot") or ""
            ca = it.get("cardanim")
            bundle = it.get("assetBundle")
            p = dump_path(bundle, ca)
            if not ca or not bundle or not os.path.exists(p):
                unresolved.append({"id": cid, "cardanim": ca, "soField": it.get("soField"),
                                   "why": "CardAnim 名或束查不到" if not ca or not bundle
                                          else "dump 文件不在盘上：" + str(p)})
                continue
            ai = (load(p).get("animInfo") or {})
            row = {
                "cardanim": ca,
                "seconds": round(anim_duration(ai), 4),
                "slot": slot,
                "trigger": it.get("abilityTriggerName"),
                "chosenAbility": it.get("chosenAbilityName"),
                "waitAnimToApplyEffect": int(ai.get("waitAnimToApplyEffect") or 0),
                "tweenAnim": None if not (ai.get("tweenAnim") or {}).get("m_PathID") else {
                    "m_PathID": (ai.get("tweenAnim") or {}).get("m_PathID"),
                    "note": "UnitTweenSO —— 它自己的 waitAnimation/length 本地**没算**（见产物 `_headline`）",
                },
            }
            (act if slot.startswith("activeAbility") else other).append(row)

        if not act and not other:
            continue
        if cid not in byid:
            unknown_card.append(cid)
        total = round(sum(r["seconds"] for r in act), 4)
        cards.append({
            "id": cid,
            "name": name_of.get(cid, ""),
            "active": total if act else None,     # None = 原版这张卡的主动能力**一条动画都没有**
            "activeAnims": act,
            "otherAbilityAnims": other,
            # 🔴 `alternativeAbility` 那一支（二选一，见 `GUID_MAP_TSV` 的注释）—— 下面统一填
            "alternative": None,
        })

    cards.sort(key=lambda c: c["id"])

    # ---- `alternativeAbility` 那一支（原版 `GetActiveAbility` 的二选一）------------------
    #
    # 🔴 2026-10-22（`W64 · B17`）：**一个值 ≠ 全部情况**（铁律 5·c）。
    #   原版 `RawCardScript__GetActiveAbility.c`：`hasAlternativeAbility(+0x2aa)` 且
    #   `BoardAnalysis.MeetsCriteria(+0x2b0 的 `normalAbilityBoardCriteria`)` ⇒ 返回**替代能力**(+0x2b8)，
    #   **否则**返回普通 `activeAbility`(+0x260)。⇒ 带替代能力的卡，时长**随棋盘状态变化**。
    #   现读全库只有 **1 张**（`SW21 Wulfen Pack Leader`：替代那支 1 条 0.2 s，普通那支 2 条 0.4 s）。
    #   ⚠️ 本表 `cards[*].active` 记的是**普通那一支**（= 现在的运行时行为）；
    #      替代那一支单独记在 `cards[*].alternative`，并进顶层 `conditional`（含 SO 的判据原件）。
    #   ⚠️ **判据原件没解码**：`normalAbilityBoardCriteria` 里的 `playerTurn: 10 / countOptions: 0 /
    #      triggerIf: 1 / referenceValue: 0` 要靠 `BoardAnalysis.MeetsCriteria` 的方法体才读得懂 ——
    #      那个方法体本件**没读到** ⇒ 不许猜，原样带出。
    so_all = {c["uniqueId"]: c for c in load(CARD_SO_TRUTH)["cards"]}
    guid_idx = load_guid_index()
    conditional = []
    for c in cards:
        srow = so_all.get(c["id"]) or {}
        alt_guids = so_ability_anims(srow, "alternativeAbility")
        if not alt_guids:
            continue
        rows, missing = [], []
        for g in alt_guids:
            rec = guid_idx.get(g)
            p = dump_path(rec[1], rec[0]) if rec else None
            if not p or not os.path.exists(p):
                missing.append({"guid": g, "why": "GUID 表里没有 / dump 不在盘上"})
                continue
            ai = load(p).get("animInfo") or {}
            rows.append({"cardanim": rec[0], "seconds": round(anim_duration(ai), 4), "guid": g})
        c["alternative"] = {
            "seconds": round(sum(r["seconds"] for r in rows), 4) if rows else None,
            "anims": rows,
            "missing": missing,
            "hasAlternativeAbility": int(srow.get("hasAlternativeAbility") or 0),
            "selectCriteria": srow.get("full_raw", {}).get("normalAbilityBoardCriteria")
            if isinstance(srow.get("full_raw"), dict) else None,
            "why": ("原版 `GetActiveAbility`：`hasAlternativeAbility && MeetsCriteria(normalAbilityBoardCriteria)` "
                    "⇒ 播**替代能力**；否则播普通 `activeAbility`。⛔ 本表 `active` 只记普通那一支"),
        }
        conditional.append({
            "id": c["id"], "name": c["name"],
            "normal": c["active"], "alternative": c["alternative"]["seconds"],
            "note": "同一张卡、不同棋盘状态 ⇒ 两个不同的时长（铁律 5·c）",
        })
    withtruth = [c for c in cards if c["active"] is not None]
    vals = sorted(c["active"] for c in withtruth)
    med = vals[len(vals) // 2] if len(vals) % 2 else round((vals[len(vals) // 2 - 1] + vals[len(vals) // 2]) / 2.0, 4)

    # 覆盖/未覆盖（**「我们引擎真能发 `EvtKind.Ability`」那一整族**点名）
    #
    # 🔴 2026-10-22（`W64 · B17`）**换过判据**：原来按 `keywords` 里有没有
    #   duty/ferocity/oath/pray/agenda 这五个词点名。那条**不是**引擎的判据，两处都会错：
    #   · **漏**：卡面印着 `Pray: …` 而 `keywords` 数组没写那个词的卡（`RuleCore.CanUseAlternative`
    #     用的是 `UnitState.HasFx`，不是 `Has`）⇒ 现读 3 张这样的卡；
    #   · **多**：`oath` 写在**战术卡**上的 10 张（`UM109` 等）—— 它们不在棋盘上，
    #     `UseOathAbility` 永远走不到（那个方法第一步就 `ctx.Players[p].Board[slot]`）。
    #
    # 现在按**引擎那三条产生 `EvtKind.Ability` 的路**点名（判据在 `RuleCore`）：
    #   ① `UseAbility`（`Ability: ` 正文）⇒ `CardDef.HasAbility` —— 原版卡恒 false（`CardDef.cs` 有实测口径）
    #   ② `UseOathAbility`  ⇒ 卡上要有 `Oath N:` 正文（`CollectOathOps`）
    #   ③ `UseAlternative`   ⇒ `Duty`/`Pray`/`Ferocity`/`Agenda` 之一（`RuleCore.IsAlternativeAction`，
    #                          闸是 `UnitState.HasFx` = 原生正文 / 挂上去的 / 封闭文法三合一）
    # 并且**必须能上棋盘**（`type` ∈ unit/hero）—— 战术卡在场上没有槽位。
    #
    # ⚠️ 如实标：② ③ 的静态代用品是「卡面正文里有没有那一段」(`desc` + `keywords` 两种写法)
    #   —— 那正是 `EffectText` 解析的输入，但我们**没有**它的输出表（那是 C# 现算的）
    #   ⇒ 这里是**代理判据**，不是引擎的运行期真相（挂上去的 `FxOps` 看不见）。
    ALT_KEYWORDS = ("duty", "pray", "ferocity", "agenda")
    BOARD_TYPES = ("unit", "hero")

    def can_emit_ability(c):
        """这张卡在**我们引擎**里有没有一条会 `Emit(EvtKind.Ability)` 的路（见上面那段）。"""
        if (c.get("type") or "") not in BOARD_TYPES:
            return False
        kws = [(k or "") for k in (c.get("keywords") or [])]
        low = [k.strip().lower() for k in kws]
        desc = c.get("desc") or ""
        # ③ 替代行动：裸关键词（正文就是整条 desc，`CollectBareKeywordBody`）
        if any(k in ALT_KEYWORDS for k in low):
            return True
        # ③ 替代行动：`Pray: …` 这种**带头**的写法（卡面正文或 `keywords` 条目里）
        if any(k.startswith(tuple(a + ":" for a in ALT_KEYWORDS)) for k in low):
            return True
        if any((("%s:" % a) in desc.lower()) for a in ALT_KEYWORDS):
            return True
        # ② 誓约：`Oath N: …`（`CollectOathOps` 认的是这条正文）
        if any(k.startswith("oath") for k in low):
            return True
        if re.search(r"(?i)(?<!\w)Oath\s*\d", desc):
            return True
        return False

    covered_ids = set(c["id"] for c in withtruth)
    # 「这张卡在当前 build 的卡 SO 里有没有 / 它的 activeAbility 有几条 CardAnim」——
    # 🔴 判据**直接读 SO**，不再拿 vfx 表反推（vfx 是下游，用下游反推上游 = 自证）。
    #   （`so_all` 在上面 `alternativeAbility` 那一段已经载过一次，这里复用，别载两遍）
    so = so_all

    def so_active_guids(cid):
        """SO 里这张卡 `activeAbility` 的 **CardAnim GUID 清单** —— 只数**非空**的。

        🔴 2026-10-22（`W64 · B17`）**修过一条假数**：原来数的是
        `len(cardAnimations)`，而现读有 **34 张卡**的 `cardAnimations` 里是**空引用**
        （`m_AssetGUID: ""`）⇒ 那些卡会被算成「有 2 条动画」，进而被错误地判成
        「映射缺口，该修」。真实口径 = 非空 GUID 的条数（= 原版真的会播几条）。"""
        s = so.get(cid)
        if s is None:
            return None
        out = []
        for lg in ((s.get("activeAbility") or {}).get("activeAbilityLogic") or []):
            for an in (lg.get("cardAnimations") or []):
                g = ((an or {}).get("m_AssetGUID") or "").strip()
                if g:
                    out.append(g)
        return out

    want = [c for c in ce if can_emit_ability(c)]
    uncovered = []
    for c in want:
        if c["id"] in covered_ids:
            continue
        guids = so_active_guids(c["id"])
        if guids is None:
            why = "**当前 build 的卡 SO 里根本没有这张卡** ⇒ 判据为空（铁律 11 例外①）"
        elif not guids:
            why = ("卡 SO 里有这张卡、`hasActiveAbility = %d`，但它的 `activeAbility` "
                   "**一条非空的 CardAnim 引用都没有**（`m_AssetGUID` 全空）⇒ 原版这条路上"
                   "**不放任何动画** ⇒ 判据不足以定「该等 0 还是等一拍」，暂退回兜底值并**出声**"
                   % int((so[c["id"]] or {}).get("hasActiveAbility") or 0))
        else:
            why = ("卡 SO 的 `activeAbility` 有 **%d 条** CardAnim（非空 GUID），但 "
                   "`card_vfx_by_card.json` 没覆盖到（映射缺口，**该修**）⇒ 暂退回兜底值并**出声**"
                   % len(guids))
        uncovered.append({
            "id": c["id"], "name": c.get("name"), "faction": c.get("faction"),
            "inTruthSnapshot": c["id"] in so,
            "soActiveAnims": None if guids is None else len(guids),
            "canEmitAbility": True,
            "why": why,
        })

    # ---- 第二条链交叉校验：`cards_engine.json` 的 `animTimes`（`gen_cards_engine.py` 那条独立链）
    #
    # 两链互不依赖：本表 = `card_vfx_by_card` → CardAnim dump；`animTimes` = 卡 SO 的 GUID →
    # `guid_map_bundles.tsv` → CardAnim dump。**都落在同一份 `AnimInfo` 上**，
    # 但 GUID 解析、束定位、聚合是各写各的 ⇒ 逐卡比一遍能抓「某一链静默丢了一条」。
    # `animTimes` 里 `tg == -1` 的就是 `activeAbility.activeAbilityLogic[*].cardAnimations[*]`，
    # 它的 `u` = `m + s + e + d`（与 `anim_duration` **逐字同一公式**）。
    an_reported = {c["id"]: c.get("animTimes") or [] for c in ce}
    cross = {"animTimesCards": 0, "agree": 0, "mismatch": [], "onlyInAnimTimes": [],
             "onlyInTable": []}
    for cid, rows in an_reported.items():
        act = [r for r in rows if int(r.get("tg", 0)) == -1]
        if not act:
            continue
        cross["animTimesCards"] += 1
        s2 = round(sum(float(r.get("u") or 0.0) for r in act), 4)
        if cid in covered_ids:
            s1 = round(next(c["active"] for c in withtruth if c["id"] == cid), 4)
            if abs(s1 - s2) <= 1e-6:
                cross["agree"] += 1
            else:
                cross["mismatch"].append({"id": cid, "table": s1, "animTimes": s2})
        else:
            cross["onlyInAnimTimes"].append({"id": cid, "animTimes": s2})
    for c in withtruth:
        if not any(int(r.get("tg", 0)) == -1 for r in an_reported.get(c["id"], [])):
            cross["onlyInTable"].append({"id": c["id"], "table": c["active"]})
    cross["allAgree"] = (not cross["mismatch"] and not cross["onlyInAnimTimes"]
                         and not cross["onlyInTable"])

    stats = {
        "cardsWithTruth": len(withtruth),
        "activeAnims": sum(len(c["activeAnims"]) for c in withtruth),
        "cardsMatchedButNoActiveAnim": len([c for c in cards if c["active"] is None]),
        "otherAbilityAnims": sum(len(c["otherAbilityAnims"]) for c in cards),
        "min": vals[0] if vals else None,
        "median": med,
        "max": vals[-1] if vals else None,
        "unresolved": len(unresolved),
        "wantedCards": len(want),            # 引擎里**真能发 `EvtKind.Ability`** 的卡（池内、能上棋盘）
        "uncoveredCards": len(uncovered),
        # 两链交叉校验（`cards_engine.animTimes` 的 `tg == -1` 行）
        "animTimesCards": cross["animTimesCards"],
        "animTimesAgree": cross["agree"],
        "animTimesMismatch": len(cross["mismatch"]),
        "animTimesOnly": len(cross["onlyInAnimTimes"]),
        "tableOnly": len(cross["onlyInTable"]),
    }

    headline = (
        "🔴 `A1767`：`EventTiming.DurationOf(EvtKind.Ability)` 的那条常数 **1.0** 换成**逐卡真值**。"
        "判据 = 该卡 `activeAbility` 各 `cardAnimations` 的 `AnimInfo.GetAnimDuration()` 之和"
        "（`vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`；原版 `AnimInfo__GetAnimDuration.c`）。"
        f"覆盖 **{stats['cardsWithTruth']} 张卡 / {stats['activeAnims']} 条 CardAnim**，"
        f"min {stats['min']} / 中位 **{stats['median']}** / max {stats['max']}。"
        # 🔴 B17（2026-10-22）加的两段：第二条链交叉校验 + 「会发 Ability 事件」那一族的准确口径
        f"**第二条链交叉校验**（`cards_engine.json` 的 `animTimes`，`tg == -1` 行；那是一条独立的 "
        f"GUID→CardAnim 链）：{stats['animTimesCards']} 张卡，与本表**逐卡一致 {stats['animTimesAgree']}**、"
        f"不一致 {stats['animTimesMismatch']} · 只在 animTimes 有 {stats['animTimesOnly']} · "
        f"只在本表有 {stats['tableOnly']}"
        f"{'（**两链全同**）' if cross['allAgree'] else '（⚠️ 有分歧，见 `_crossCheck`）'}。"
        f"**「会发 Ability 事件」那一族 = {stats['wantedCards']} 张**（`type ∈ unit/hero` 且卡面有 "
        "`Duty/Pray/Ferocity/Agenda` 正文或 `Oath N:` —— 引擎那三条产生点的代理判据），"
        f"其中 **{stats['uncoveredCards']} 张没有真值**（逐张见 `uncovered`，理由逐条写明）。"
        f"**同一张卡、不同棋盘状态 = 不同时长**的那一族：**{len(conditional)} 张**（见 `conditional`）—— "
        "原版 `RawCardScript.GetActiveAbility` 在 `activeAbility` / `alternativeAbility` 之间按 "
        "`normalAbilityBoardCriteria` **二选一**；本表 `active` 只记**普通那一支**。"
        "⚠️ **三处如实标注**：① 那 " + str(len(uncovered)) + " 张没有真值 ——"
        " **不是快照年代**（2026-10-18 起真值 = 当前 build 的卡 SO），而是它们的卡 SO "
        "`activeAbility` 里**一条非空 CardAnim 引用都没有**（`m_AssetGUID` 全空）⇒ "
        "原版这条路上不放动画、「该等 0 还是等一拍」判据不足（铁律 11 例外①）；"
        "② 原版 `_ResolveAnim` 里 tween 那一段（`animInfo.tweenAnim != null && waitAnimation != 0` 时 "
        "`WaitForCompletion(BuildSequence)`）**本表没算**（`UnitTweenSO` 的长度本地没接）；"
        "③ `cardAbilities[*]`（`ThisCardPlayed` / `Slay` / … 那一族）是**别的时机**播的，"
        "**不进这张表**（留在 `cards[*].otherAbilityAnims` 备查）。"
    )

    conventions = {
        "scope": "只取 `activeAbility.activeAbilityLogic[*].cardAnimations[*]`（原版「单位主动发动能力」那一条路）；"
                 "`cardAbilities[*]` 归 `EvtKind.Trigger`/`Play`，不在本表",
        "formula": "AnimInfo.GetAnimDuration() = vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos（不含 startDelay）",
        "aggregate": "一张卡的值 = 它 `activeAbility` 全部 CardAnim 的时长**求和** —— 依据 "
                     "`AbilityLogic__PlayAnim.c` 会把这一整串逐条 `AddCardAnim` 排队播完",
        "fallback": f"表里没有这张卡 ⇒ 退回 {FALLBACK}（= 本表中位数；**能力事件那条路会出声**，见 `AbilityAnimDurations`）",
        "notComputed": "`animInfo.tweenAnim`（UnitTweenSO）的长度；`waitAnimToApplyEffect` 只如实带出不参与求和",
        "whoEmitsAbility": "`EvtKind.Ability` 在引擎里只有三条产生点：`RuleCore.UseAbility`（`Ability:` 正文，"
                           "原版卡恒 false）· `UseOathAbility`（`Oath N:`）· `UseAlternative`"
                           "（`Duty/Pray/Ferocity/Agenda`）。前两条要 `Board[slot]` ⇒ **战术卡不可能发**",
        "secondChain": "`cards_engine.json` 的 `animTimes` 里 `tg == -1` 的行 = 同一份 "
                       "`activeAbility.activeAbilityLogic[*].cardAnimations[*]`；那条链走的是 "
                       "`guid_map_bundles.tsv`（与本表的 `card_vfx_by_card.json` 不同一条）⇒ 逐卡比一遍",
        "soAnimCount": "`uncovered[*].soActiveAnims` 数的是**非空 GUID** 的条数 —— "
                       "现读有 34 张卡的 `cardAnimations` 里是空引用（`m_AssetGUID: \"\"`），"
                       "数 `len()` 会得出假数（2026-10-22 修）",
    }

    full = {
        "schema": "ability_anim_durations/v1",
        "generatedBy": "d:/4/Unity/工具/gen_ability_anim_durations.py",
        "_sources": {
            "cardVfxByCard": VFX_TABLE,
            "cardsEngine": CARDS_ENGINE,
            "cardAnimDumps": ASSETS_FULL + "/bundle_*cardanims*/MonoBehaviour/*.json",
            # 当前 build 的卡 SO 语料（2026-10-18 换；旧的是 2025-07-30 的 cards_1195_2022时代.json）
            "cardSoTruth": CARD_SO_TRUTH,
            "splitter": ROOT2 + "/tools/decomp_full/AbilityLogic__PlayAnim.c",
            "resolver": ROOT2 + "/tools/decomp_full/BattleManager._ResolveAnim_d__466__MoveNext.c",
            "duration": ROOT2 + "/tools/decomp_full/AnimInfo__GetAnimDuration.c",
        },
        "_headline": headline,
        "_conventions": conventions,
        "cards": cards,
        # 🔴 2026-10-22（`W64 · B17`）：**同一张卡、不同棋盘状态 = 不同时长**的那一族
        #   （原版 `GetActiveAbility` 在 `activeAbility` / `alternativeAbility` 之间二选一）。
        #   现读只有 `SW21` 一张；`cards[*].alternative` 里带 SO 的判据原件。
        "conditional": conditional,
        "uncovered": uncovered,
        "unresolved": unresolved,
        "unknownCardIds": unknown_card,
        "_crossCheck": cross,
        "stats": stats,
    }

    rt_items = [{"id": c["id"], "name": c["name"], "s": c["active"], "n": len(c["activeAnims"])}
                for c in withtruth]
    rt = {
        "schema": "ability_anim_durations/v1+rt",
        "generatedBy": "d:/4/Unity/工具/gen_ability_anim_durations.py",
        "_headline": (
            f"`A1767`：逐卡「发动能力」时长 = 该卡 `activeAbility` 各 CardAnim 的 "
            f"`AnimInfo.GetAnimDuration()` 之和。{len(rt_items)} 张卡 · 中位 {med} · "
            "表里没有这张卡 ⇒ 退回 `fallbackSeconds` 并**出声**。"
            "判据长文 / 未覆盖名单 → `数据/游戏数据/ability_anim_durations.json` 的 `_headline` 与 `uncovered`。"),
        "fallbackSeconds": med,
        "count": len(rt_items),
        "items": rt_items,
    }
    return full, rt


def canon(o):
    return json.dumps(o, ensure_ascii=False, sort_keys=True, indent=1)


def main():
    check = "--check" in sys.argv
    full, rt = build()
    if check:
        ok = True
        for p, o in ((OUT_FULL, full), (OUT_RT, rt)):
            if not os.path.exists(p):
                print("✗ 缺文件: " + p)
                ok = False
                continue
            a = canon(load(p))
            b = canon(o)
            if "generatedAt" in a:
                pass
            if a == b:
                print("✅ 一致: " + p)
            else:
                # 忽略 generatedAt 的差异
                fa, fb = json.loads(a), json.loads(b)
                fa.pop("generatedAt", None)
                fb.pop("generatedAt", None)
                if fa == fb:
                    print("✅ 一致（仅 generatedAt 不同）: " + p)
                else:
                    print("✗ 不一致: " + p)
                    ok = False
        print("=== --check " + ("全过 ✅" if ok else "有差异 ✗"))
        return 0 if ok else 1
    io.open(OUT_FULL, "w", encoding="utf-8", newline="\n").write(canon(full))
    io.open(OUT_RT, "w", encoding="utf-8", newline="\n").write(canon(rt))
    s = full["stats"]
    print("[全量] %s  (%d 字节)" % (OUT_FULL, os.path.getsize(OUT_FULL)))
    print("[运行时] %s  (%d 字节)" % (OUT_RT, os.path.getsize(OUT_RT)))
    print("覆盖 %d 张卡 / %d 条 CardAnim · min %s / 中位 %s / max %s · 解不出 %d"
          % (s["cardsWithTruth"], s["activeAnims"], s["min"], s["median"], s["max"], s["unresolved"]))
    print("引擎里真能发 `EvtKind.Ability` 的卡 %d 张，其中没真值的 **%d 张**（逐张见 `uncovered`）"
          % (s["wantedCards"], s["uncoveredCards"]))
    for u in full["uncovered"]:
        print("   ! %s %s（%s）soActiveAnims=%s —— %s"
              % (u["id"], u["name"], u["faction"], u["soActiveAnims"], u["why"][:60]))
    print("另有 %d 张卡在真值里有键、但 `activeAbility` 一条动画都没有（原版就不放东西）"
          % s["cardsMatchedButNoActiveAnim"])
    print("两链交叉校验（animTimes 的 tg=-1）：%d 张 · 一致 %d · 不一致 %d · 只在 animTimes %d · 只在本表 %d%s"
          % (s["animTimesCards"], s["animTimesAgree"], s["animTimesMismatch"],
             s["animTimesOnly"], s["tableOnly"],
             "" if full["_crossCheck"]["allAgree"] else "  ⚠️ 有分歧"))
    for c in full["conditional"]:
        print("   ~ 条件分支 %s %s：普通 %s s / 替代 %s s（⛔ 不是一个固定值）"
              % (c["id"], c["name"], c["normal"], c["alternative"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
