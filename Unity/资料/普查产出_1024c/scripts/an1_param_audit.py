# -*- coding: utf-8 -*-
"""AN1 · 参数层审计：`AnimInfo` 的**位置/位移/朝向**参数在各事件桶里到底有没有被消费。

只读。判据（代码侧，2026-10-24 现读）：
  · `ability` → `ExpandAbilityBeats` → `PlayAbilityBeat`（sp/ep/pt/mv/ts/tm/ease 全消费）
  · `attack_ranged` → `ScheduleAttackPresentation` → `ResolveAttackPieceAnchors` + `ArmFireMove`（消费）
  · `play` / `trigger` / `death` / `hit` / `deploy` → `FireEventAll` **只给「一个点」**
    （`layout.DropTargetWorld(slot)`）⇒ sp/ep/pt/mv **一个都不消费**
  · `EvtKind.Play` 且 `BattleEvent.Slot < 0`（= **未指定目标的战术/防御卡**）⇒ `PlaySignal`
    在 `if (slot < 0) return;` 处直接返回 ⇒ 这一桶**一件都不播**
产出：`data/an1_param_buckets.tsv` + stdout 汇总。
用法：`python -I -X utf8 an1_param_audit.py`
"""
import sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import an1_lib as L

OUT = L.ROOT + "/资料/普查产出_1024c/data/an1_param_buckets.tsv"
CONSUMED = {"ability", "attack_ranged"}          # 逐拍锚点/位移已被消费的桶
SUMMON_SPOTS = {14, 15, 16, 17}
HUD_SPOTS = {6, 7, 8, 10, 11, 12, 13, 18, 19, 50, 55, 60, 61, 70, 71, 80, 90,
             100, 110, 120, 130, 140, 145, 150, 155}


def main():
    pool = L.load_cards()
    rt = L.load_rt_vfx()
    clips = {r["a"]: r for r in L.load_cardanim_clips()["rows"]}
    rt_ev = L.rt_events_by_card(rt)
    by_id = {c["id"]: c for c in pool["cards"]}

    # 逐 (阵营, 桶) 统计
    stat = collections.defaultdict(lambda: collections.Counter())
    rows = []
    for cid, evs in rt_ev.items():
        c = by_id.get(cid)
        if c is None:
            continue
        for b, es in evs.items():
            for e in es:
                a = e.get("a") or ""
                cl = clips.get(a)
                keys = []
                if cl:
                    if cl["mv"] and cl["tm"] > 0:
                        keys.append("move")
                    if cl["pt"]:
                        keys.append("orient")
                    if cl["sp"] != cl["ep"]:
                        keys.append("start!=end")
                    if cl["sp"] in SUMMON_SPOTS or cl["ep"] in SUMMON_SPOTS:
                        keys.append("summonSpot")
                    if cl["sp"] in HUD_SPOTS or cl["ep"] in HUD_SPOTS:
                        keys.append("hudAnchor")
                    if cl.get("ao"):
                        keys.append("moveOffsets")
                    if cl.get("otv"):
                        keys.append("orientToVelocity")
                rows.append(dict(faction=c["faction"], bucket=b, card=cid, name=c["name"],
                                 cardanim=a, prefab=e.get("n") or "",
                                 sp=(cl or {}).get("sp", ""), ep=(cl or {}).get("ep", ""),
                                 pt=(cl or {}).get("pt", ""), mv=(cl or {}).get("mv", ""),
                                 tm=(cl or {}).get("tm", ""),
                                 params="|".join(keys),
                                 consumed=("Y" if b in CONSUMED else "N")))
                s = stat[(c["faction"], b)]
                s["rows"] += 1
                s["cards"] += 0
                for k in keys:
                    s[k] += 1
                s["has_params"] += 1 if keys else 0

    cards_with_play = collections.defaultdict(set)
    untargeted = collections.Counter()
    for cid, evs in rt_ev.items():
        c = by_id.get(cid)
        if c is None or "play" not in evs:
            continue
        cards_with_play[c["faction"]].add(cid)
        if int(c.get("targeted") or 0) == 0 and c["type"] in ("tactic", "defence"):
            untargeted[c["faction"]] += 1

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8", newline="\n") as f:
        cols = ["faction", "bucket", "rows", "has_params", "move", "orient", "start!=end",
                "summonSpot", "hudAnchor", "moveOffsets", "orientToVelocity", "consumed"]
        f.write("\t".join(cols) + "\n")
        for (fa, b), s in sorted(stat.items()):
            f.write("\t".join(str(x) for x in [fa, b, s["rows"], s["has_params"], s["move"],
                                               s["orient"], s["start!=end"], s["summonSpot"],
                                               s["hudAnchor"], s["moveOffsets"], s["orientToVelocity"],
                                               "Y" if b in CONSUMED else "N"]) + "\n")

    print("== 逐桶（全池）==")
    agg = collections.defaultdict(collections.Counter)
    for (fa, b), s in stat.items():
        agg[b].update(s)
    for b, s in agg.items():
        print("  %-14s rows=%-4d withParams=%-4d move=%-4d orient=%-4d sp!=ep=%-4d summonSpot=%-3d hudAnchor=%-3d consumed=%s"
              % (b, s["rows"], s["has_params"], s["move"], s["orient"], s["start!=end"],
                 s["summonSpot"], s["hudAnchor"], "Y" if b in CONSUMED else "N"))
    print("== 未指定目标的战术/防御卡（`Slot=-1` ⇒ play 桶一件都不播）==")
    for fa, n in untargeted.most_common():
        print("  %-16s %d 张（该阵营有 play 行的卡 %d 张）" % (fa, n, len(cards_with_play[fa])))
    print("  合计 %d 张 / play 行合计见上表" % sum(untargeted.values()))
    print("→", OUT)


if __name__ == "__main__":
    main()
