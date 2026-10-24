# -*- coding: utf-8 -*-
"""W2 · **同一套判定逻辑**在 R4 改完之后重跑（只读；输出写到 `data/w2_bucket_matrix_after.tsv`）。

原版 = `an1_bucket_matrix.py`（AN1 2026-10-24）。**逐字复制、只改输出路径** —— 这样
「改前 456 行 / 274 卡」与「改后」是**同一条判据**算出来的，可比。

只读。判据（代码现读，出处见每条的注释）：
  · 桶 → 出路：`BattleDriver.PlaySignal` / `ExpandAbilityBeats` / `VfxMap.TryResolveTriggerPicks`
    / `CardFeel.PickDeathFx` / `TraitFrames`
  · `EvtKind.Play` 且 `Slot < 0`（未指定目标的战术/防御）⇒ `PlaySignal` 在 `slot < 0` 处 return
  · `ability` 桶里 `CardAnimTime.Trigger != -1` 的行 = 生成器「未归口」的触发动画
    （`工具/gen_card_vfx_by_card.py` 的 `trigger_event()`：只有 7 个名字归口，其余一律 `EV_ABILITY`）
  · `ability` 桶要逐拍锚点，必须命中 `CardAnimOrder`（62 张卡）；否则整桶单点播
产出：`data/an1_bucket_matrix.tsv` + stdout 汇总。
用法：`python -I -X utf8 an1_bucket_matrix.py [Faction]`
"""
import sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import an1_lib as L

OUT = L.ROOT + "/资料/普查产出_1024c/data/w2_bucket_matrix_after.tsv"   # 全池；带阵营参数时写成 an1_bucket_matrix_<Faction>.tsv
# 与 gen_card_vfx_by_card.py 的 trigger_event() 同一张表（7 个名字归口）
MAPPED = {"ThisCardPlayed": "play", "Strike": "trigger", "Slay": "trigger",
          "ThisUnitDeath": "death", "UnitDeathByAbility": "death",
          "UnitDamaged": "hit", "UnitAttack": "attack_melee"}


def main():
    only = sys.argv[1] if len(sys.argv) > 1 else None
    pool = L.load_cards()
    rt = L.load_rt_vfx()
    rt_ev = L.rt_events_by_card(rt)
    by = {c["id"]: c for c in pool["cards"]}
    order = L.jload(L.ASSETS + "/CardPresentation/Resources/card_anim_order.json")
    beat_ids = set()
    for c in order.get("cards") or []:
        if isinstance(c, dict):
            if c.get("id"):
                beat_ids.add(c["id"])
            if c.get("name"):
                beat_ids.add(c["name"])
            if c.get("nm"):
                beat_ids.add(c["nm"])
    # 白名单（VfxMap.IsRoutedTriggerName 的 case 标签）
    src = open(L.ASSETS + "/CardPresentation/Core/VfxMap.cs", encoding="utf-8").read()
    i = src.index("static bool IsRoutedTriggerName")
    body = src[i:src.index("static string CanonicalTriggerName", i)]
    routed = set(m.group(1).lower() for m in __import__("re").finditer(r'case\s+"([^"]+)"\s*:', body))
    routed |= {"ambush", "triggerenchantment"}

    stat = collections.defaultdict(collections.Counter)
    cards_of = collections.defaultdict(set)
    detail = []
    for cid, evs in rt_ev.items():
        c = by.get(cid)
        if c is None:
            continue
        if only and c["faction"] != only:
            continue
        times = c.get("animTimes") or []
        idx = collections.defaultdict(list)
        for t in times:
            if t.get("a"):
                idx[t["a"]].append(t)
        has_active_ability = bool(c.get("activeAbilityLogic"))
        has_beat = (cid in beat_ids) or (c["name"] in beat_ids)
        for b, es in evs.items():
            for e in es:
                a = e.get("a") or ""
                rows = idx.get(a) or []
                t = rows[0] if rows else None
                tg = t.get("tg") if t else None
                tn = (t.get("tn") or "") if t else ""
                verdict = ""
                if b == "play":
                    if int(c.get("targeted") or 0) == 0 and c["type"] in ("tactic", "defence"):
                        verdict = "永不播（未指定目标 ⇒ Play 事件 Slot=-1）"
                    else:
                        verdict = "播放（单点）· 锚点/位移参数不消费"
                elif b == "ability":
                    if tg == -1:
                        if has_beat:
                            verdict = "逐拍播（锚点/位移已消费）"
                        else:
                            verdict = "单点播 · 锚点/位移参数不消费"
                    else:
                        if MAPPED.get(tn) == "trigger" or tn:
                            if has_active_ability:
                                verdict = "风险：在能力拍上播（原版时机 = %s）" % (tn or "?")
                            else:
                                verdict = "不要在这拍播：原版时机 = %s（走 trigger 归口；未归口则永不播）" % (tn or "?")
                elif b == "trigger":
                    if tn and tn.lower() not in routed:
                        verdict = "整桶播（名字未放行 ⇒ 可能错件/多播；trait 行另有 TraitFrames 一份）"
                    else:
                        verdict = "按 trigger 名归口"
                elif b == "death":
                    so = ""
                    verdict = "阵亡粒子（PickDeathFx）"
                elif b in ("attack_melee", "hit"):
                    verdict = "搬进 unit_attack_anim / unit_damaged_anim 桶"
                elif b == "attack_ranged":
                    verdict = "逐卡远程（锚点/位移已消费）"
                elif b == "trait_onplay":
                    verdict = "TraitFrames._playBook（登场那一拍）"
                elif b == "deploy":
                    verdict = "登场根实例化（竞技场空间）"
                stat[(b, verdict)]["rows"] += 1
                stat[(b, verdict)]["cards"] += 0
                cards_of[(b, verdict)].add(cid)
                detail.append((c["faction"], cid, c["name"], b, a, e.get("n") or "", tn, str(tg), verdict))

    out = OUT if not only else OUT.replace(".tsv", "_%s.tsv" % only)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        f.write("faction\tcard\tname\tbucket\tcardanim\tprefab\torigTrigger\torigTriggerVal\tverdict\n")
        for d in sorted(detail):
            f.write("\t".join(d) + "\n")
    print("== 桶 × 出路 ==")
    for (b, v), s in sorted(stat.items()):
        n = len(cards_of[(b, v)])
        print("  %-14s rows=%-4d cards=%-4d  %s" % (b, s["rows"], n, v))
    print("→", out)
    if only:
        print("== %s 的明细（按出路）==" % only)
        for (b, v), s in sorted(stat.items()):
            print("  %-14s rows=%-3d %s" % (b, s["rows"], v))


if __name__ == "__main__":
    main()
