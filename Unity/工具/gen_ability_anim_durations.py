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
        })

    cards.sort(key=lambda c: c["id"])
    withtruth = [c for c in cards if c["active"] is not None]
    vals = sorted(c["active"] for c in withtruth)
    med = vals[len(vals) // 2] if len(vals) % 2 else round((vals[len(vals) // 2 - 1] + vals[len(vals) // 2]) / 2.0, 4)

    # 覆盖/未覆盖（按我们池子里**会发 Ability 事件**的那两族点名）
    def kw_has(c, kw):
        return any(kw in (k or "").lower() for k in (c.get("keywords") or []))

    covered_ids = set(c["id"] for c in withtruth)
    # 「这张卡在当前 build 的卡 SO 里有没有 / 它的 activeAbility 有几条 CardAnim」——
    # 🔴 判据**直接读 SO**，不再拿 vfx 表反推（vfx 是下游，用下游反推上游 = 自证）。
    so = {c["uniqueId"]: c for c in load(CARD_SO_TRUTH)["cards"]}

    def so_active_anims(cid):
        s = so.get(cid)
        if s is None:
            return None
        return sum(len((l or {}).get("cardAnimations") or [])
                   for l in ((s.get("activeAbility") or {}).get("activeAbilityLogic") or []))

    want = [c for c in ce if any(kw_has(c, k) for k in ("duty", "ferocity", "oath", "pray", "agenda"))]
    uncovered = []
    for c in want:
        if c["id"] in covered_ids:
            continue
        n_so = so_active_anims(c["id"])
        if n_so is None:
            why = "**当前 build 的卡 SO 里根本没有这张卡** ⇒ 判据为空（铁律 11 例外①）"
        elif n_so == 0:
            why = ("卡 SO 里有这张卡，但它的 `activeAbility` **一条 CardAnim 都没有**"
                   "（原版这条路不放东西；`hasActiveAbility = %d`）—— 判据不足以定「该等 0 还是等一拍」"
                   " ⇒ 退回兜底值并**出声**" % int((so[c["id"]] or {}).get("hasActiveAbility") or 0))
        else:
            why = ("卡 SO 的 `activeAbility` 有 **%d 条 CardAnim**，但 `card_vfx_by_card.json` 没覆盖到"
                   "（映射缺口，**该修**）⇒ 暂退回兜底值并**出声**" % n_so)
        uncovered.append({
            "id": c["id"], "name": c.get("name"), "faction": c.get("faction"),
            "inTruthSnapshot": c["id"] in so,
            "soActiveAnims": n_so,
            "why": why,
        })

    stats = {
        "cardsWithTruth": len(withtruth),
        "activeAnims": sum(len(c["activeAnims"]) for c in withtruth),
        "cardsMatchedButNoActiveAnim": len([c for c in cards if c["active"] is None]),
        "otherAbilityAnims": sum(len(c["otherAbilityAnims"]) for c in cards),
        "min": vals[0] if vals else None,
        "median": med,
        "max": vals[-1] if vals else None,
        "unresolved": len(unresolved),
        "wantedCards": len(want),
        "uncoveredCards": len(uncovered),
    }

    headline = (
        "🔴 `A1767`：`EventTiming.DurationOf(EvtKind.Ability)` 的那条常数 **1.0** 换成**逐卡真值**。"
        "判据 = 该卡 `activeAbility` 各 `cardAnimations` 的 `AnimInfo.GetAnimDuration()` 之和"
        "（`vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`；原版 `AnimInfo__GetAnimDuration.c`）。"
        f"覆盖 **{stats['cardsWithTruth']} 张卡 / {stats['activeAnims']} 条 CardAnim**，"
        f"min {stats['min']} / 中位 **{stats['median']}** / max {stats['max']}。"
        "⚠️ **三处如实标注**：① 仍有 " + str(len(uncovered)) + " 张「会发 Ability 事件」的卡没有真值 ——"
        " **不是快照年代**（2026-10-18 起真值 = 当前 build 的卡 SO），而是它们的卡 SO "
        "`activeAbility` 本身一条 CardAnim 都没有** ⇒ 判据为空（铁律 11 例外①）；"
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
        "uncovered": uncovered,
        "unresolved": unresolved,
        "unknownCardIds": unknown_card,
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
    print("会发 Ability 事件的关键词族 %d 张里，没真值的 %d 张（它们的卡 SO `activeAbility` 一条 CardAnim 都没有）"
          % (s["wantedCards"], s["uncoveredCards"]))
    print("另有 %d 张卡在真值里有键、但 `activeAbility` 一条动画都没有（原版就不放东西）"
          % s["cardsMatchedButNoActiveAnim"])
    return 0


if __name__ == "__main__":
    sys.exit(main())
