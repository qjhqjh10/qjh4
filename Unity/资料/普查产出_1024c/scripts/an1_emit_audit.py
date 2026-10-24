# -*- coding: utf-8 -*-
"""AN1 试点 · 「这一拍到底播得出来吗」—— 逐 (卡,桶,条目) 归口审计。

只读。对每个阵营（默认 Sautekh）逐条判定：
  · 这条候选走哪条**生产播放口**（或：根本没有口）
  · trait 行 ↔ `trait_effect_bindings.json` 的交叉覆盖（同一件会被谁播、会不会两遍/一遍都没有）
  · 阵亡桶：`customDeathParticles` vs 死亡触发的**能力动画**（后者没有播放拍）
  · `Wait_*` 纯计时拍（原版合法、无 prefab）
输出：`data/an1_<faction>_emit.tsv` + stdout 汇总。
用法：`python -I -X utf8 an1_emit_audit.py [Faction]`
"""
import sys, os, json, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import an1_lib as L

OUT_DIR = L.ROOT + "/资料/普查产出_1024c/data"

# 原版 `AbilityTrigger` 枚举里、pipeline 会归到 EvtKind.Trigger 的那批（白名单，判据 = VfxMap.IsRoutedTriggerName）
# 这里**不复写一份**：从 `cards_engine.json` 的 tn 现读，白名单由报告里的人工核对列出。
ABILITY_TRIGGER_EVENT = {
    "play": "EV_PLAY",
    "attack_ranged": "EV_ATTACK_RANGED",
    "attack_melee": "EV_UNIT_ATTACK(50) → unit_attack_anim（装载时搬桶）",
    "hit": "EV_UNIT_DAMAGED(60) → unit_damaged_anim（装载时搬桶）",
    "ability": "EV_ABILITY",
    "trigger": "EV_TRIGGER",
    "trait_onplay": "EV_TRAIT_ONPLAY（TraitFrames._playBook，不在 VfxMap）",
    "death": "EV_DEATH",
    "deploy": "EV_DEPLOY",
}


def main():
    faction = sys.argv[1] if len(sys.argv) > 1 else "Sautekh"
    pool = L.load_cards()
    orig = L.load_orig_vfx()
    rt = L.load_rt_vfx()
    addr = L.load_anim_addr()
    prefabs = L.load_prefab_names()
    rt_ev = L.rt_events_by_card(rt)
    cards = {c["id"]: c for c in pool["cards"] if c["faction"] == faction}

    bind = L.jload(L.ASSETS + "/CardPresentation/Resources/trait_effect_bindings.json")
    bind_by_trait = collections.defaultdict(list)
    for r in bind["rows"]:
        bind_by_trait[r["trait"]].append(r)

    rows_out = []
    for cid, c in sorted(cards.items()):
        o = orig["byCard"].get(cid) or {}
        r = rt_ev.get(cid, {})
        for b in sorted(k for k in set(o) | set(r) if not k.startswith("_")):
            oe = o.get(b) or []
            re_ = r.get(b) or []
            for i in range(max(len(oe), len(re_))):
                e = oe[i] if i < len(oe) else None
                q = re_[i] if i < len(re_) else None
                pf = (q or {}).get("n") or (e or {}).get("prefab") or ""
                ca = (q or {}).get("a") or (e or {}).get("cardanim") or ""
                src = (e or {}).get("src") or ""
                tt = (e or {}).get("traitTriggerType") or ""
                trait = (e or {}).get("traitName") or ""
                so = (e or {}).get("soField") or ""
                emit, note = classify(b, src, so, tt, trait, ca, pf, c, bind_by_trait, prefabs)
                rows_out.append(dict(card=cid, name=c["name"], bucket=b, idx=i,
                                     prefab=pf, cardanim=ca, src=src, soField=so,
                                     trait=trait, traitType=tt, emit=emit, note=note))

    os.makedirs(OUT_DIR, exist_ok=True)
    p = os.path.join(OUT_DIR, "an1_%s_emit.tsv" % faction)
    cols = ["card", "name", "bucket", "idx", "prefab", "cardanim", "src", "soField",
            "trait", "traitType", "emit", "note"]
    with open(p, "w", encoding="utf-8", newline="\n") as f:
        f.write("\t".join(cols) + "\n")
        for x in rows_out:
            f.write("\t".join(str(x.get(k, "")) for k in cols) + "\n")

    print("rows:", len(rows_out))
    print("emit:", collections.Counter(x["emit"] for x in rows_out).most_common())
    print("bucket×emit:")
    for (b, em), n in sorted(collections.Counter((x["bucket"], x["emit"]) for x in rows_out).items()):
        print("  %-14s %-28s %d" % (b, em, n))
    print("notes:", collections.Counter(x["note"].split("；")[0] for x in rows_out if x["note"]).most_common(15))
    print("→", p)


def classify(b, src, so, tt, trait, ca, pf, card, bind_by_trait, prefabs):
    """返回 (emit, note)。emit 取值见文件头；`note` 里写判据/缺口。"""
    n = []
    if ca.startswith("Wait"):
        return "TIMING_ONLY", "纯计时拍（原版 animAdressable 空）"

    if b == "play":
        return "PLAY_BUCKET(逐卡)", "EvtKind.Play → FireEventAll"
    if b == "attack_ranged":
        if not pf:
            return "TIMING_ONLY", "远程逐卡拍里这件没有 prefab"
        return "ATTACK_RANGED(逐卡)", "ScheduleAttackPresentation → ResolveRangedAttackPieces"
    if b == "attack_melee":
        return "UNIT_ATTACK_BUCKET", "装载时搬进 unit_attack_anim（VfxMap.LoadTableFromJson）"
    if b == "hit":
        return "UNIT_DAMAGED_BUCKET", "装载时搬进 unit_damaged_anim"
    if b == "ability":
        return "ABILITY_BEAT", "ExpandAbilityBeats → PlayAbilityBeat（逐拍锚点/位移）"
    if b == "deploy":
        return "DEPLOY", "FireDeployVfxInArenaSpace（根实例化 · 竞技场空间）"
    if b == "death":
        if so == "customDeathParticles":
            return "DEATH_PARTICLE", "CardFeel.PickDeathFx → ResolveDeathParticle（只认 customDeathParticles）"
        if so == "remnantBodyPrefab":
            return "REMNANT_TABLE", "走 RemnantBodyTable（不进死亡桶，A2083 既定）"
        # 死亡触发的能力动画（ThisUnitDeath(80) / UnitDeathByAbility(180) …）
        return "DEATH_ABILITY_NO_EMIT", ("死亡触发的能力动画：`PickDeathFx` 只取 customDeathParticles，"
                                         "这条今天没有播放拍（`%s` / `%s`）" % (tt or "?", ca or "?"))
    if b == "trigger" or b == "trait_onplay":
        if src == "cardtrait-so" or trait:
            # trait 行：查 TraitFrames 那本
            tt_num = parse_tt(tt)
            hits = [x for x in bind_by_trait.get(trait, []) if x.get("triggerType") == tt_num]
            if tt_num in (0, 2):
                book = "_startBook" if tt_num == 0 else "_playBook"
                if not hits:
                    return "TRAIT_NO_BINDING", "trait `%s` 的 triggerType=%s 在绑定表里**没有行** ⇒ %s 不播" % (trait, tt_num, book)
                return "TRAIT_FRAMES(OnPlay/Start)", "TraitFrames.%s（绑定 %s → %s）" % (book, trait, hits[0].get("prefab"))
            if tt_num in (3, 6):
                if not hits:
                    return "TRAIT_NO_BINDING", "trait `%s` 的 triggerType=%s 在绑定表里**没有行** ⇒ 触发框不播" % (trait, tt_num)
                ov = hits[0].get("overrides") or []
                if ov:
                    return "TRAIT_FRAMES(Trigger,override未应用)", "绑定有行，但原版 `overrideByArmies` %d 条、运行时未按阵营取" % len(ov)
                return "TRAIT_FRAMES(Trigger)", "TraitFrames._triggerBook（绑定 %s → %s）" % (trait, hits[0].get("prefab"))
            if tt_num == 5:
                return "TRAIT_FROMCODE", "TraitParticles（FromCode）"
        # 非 trait 的触发行 / 卡级行
        return "TRIGGER_BUCKET(归口待定)", "非 trait 行：走 TryResolveTriggerPicks 的归口/整桶滤波"
    return "UNKNOWN", "未分类"


def parse_tt(s):
    if not s:
        return -1
    if "(" in s:
        try:
            return int(s.split("(")[1].split(")")[0])
        except Exception:
            return -1
    try:
        return int(s)
    except Exception:
        return -1


if __name__ == "__main__":
    main()
