# -*- coding: utf-8 -*-
"""AN1 试点 · Sautekh（79 张）逐卡动画/特效「接线 · 真伪 · 参数」扫描。

只读。输出：
  · `资料/普查产出_1024c/data/an1_sautekh_rows.tsv` —— 逐 (卡, 事件桶, 条目) 明细
  · `资料/普查产出_1024c/data/an1_sautekh_cards.tsv` —— 逐卡汇总
  · stdout —— 汇总计数（供报告引用；⛔ 报告里别写死张数）
用法：`python -I -X utf8 an1_pilot_sautekh.py`
"""
import sys, os, json, glob, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import an1_lib as L

FACTION = "Sautekh"
OUT_DIR = L.ROOT + "/资料/普查产出_1024c/data"


# ---------------------------------------------------------------- 原版 animInfo（逐 CardAnim）
def animinfo_of(cardanim, addr):
    """CardAnim 名 → 原版 MB 的 animInfo（含全部字段）。找不到回 None。"""
    v = addr["cardanim_to_asset"].get(cardanim)
    if not v:
        g = addr["cardanim_guid_to_name"].get(cardanim)
        if g:
            v = addr["cardanim_to_asset"].get(g)
    if not v:
        return None
    src = v.get("sourceMono") or ""
    if not src:
        return None
    p = os.path.join(L.ASSETS_FULL, src.replace("/", os.sep))
    if not os.path.exists(p):
        return None
    try:
        return L.jload(p).get("animInfo") or {}
    except Exception:
        return None


def curve_sig(ai, which):
    c = ((ai.get(which) or {}).get("m_Curve")) or []
    return tuple((round(p["time"], 4), round(p["value"], 4)) for p in c)


def extra_param_flags(ai):
    """返回这拍在原版 animInfo 里**那些我们的运行期参数表不覆盖**的非默认字段。"""
    if not ai:
        return ["<无 animInfo>"]
    f = []
    ss = ai.get("screenShake") or {}
    if (ss.get("presetSO") or {}).get("m_PathID", 0):
        f.append("screenShake(preset)")
    elif any(float(ss.get(k) or 0) != 0 for k in ("delay", "sustainTime", "amplitude", "frequency",
                                                  "attackTime", "decayTime")):
        f.append("screenShake(values)")
    mm = ai.get("minionMovementInfo") or {}
    if mm.get("shouldActivateMinionMovement"):
        f.append("minionMovement(activate)")
    elif any(abs(float(mm.get(k) or 0)) > 1e-9 for k in ("yOffset", "xOffset", "zOffset")) \
            or any((mm.get(k) or {}).get("enable") for k in ("rotateXAxis", "rotateYAxis", "rotateZAxis")):
        f.append("minionMovement(offsets/rotate)")
    mc = ai.get("minionAnimationClipInfo") or {}
    if mc.get("enable"):
        f.append("minionAnimationClip(enable)")
    if (ai.get("tweenAnim") or {}).get("m_PathID", 0):
        f.append("tweenAnim")
    if ai.get("startDelay"):
        f.append("startDelay=%.3f" % ai["startDelay"])
    if ai.get("waitAnimToApplyEffect"):
        f.append("waitAnimToApplyEffect")
    sig = curve_sig(ai, "easeCurve")
    if sig not in (((0.0, 0.0), (1.0, 1.0)),):
        f.append("easeCurve=%s" % (sig,))
    if ai.get("orientToVelocity"):
        f.append("orientToVelocity")
    if ai.get("applyMovementOffsets"):
        f.append("applyMovementOffsets")
    om = ai.get("offsetMovementAxis") or {}
    for ax in ("xAxis", "yAxis", "zAxis"):
        if any(abs(p["value"]) > 1e-9 for p in ((om.get(ax) or {}).get("m_Curve") or [])):
            f.append("offsetAxis.%s" % ax)
    return f


# ---------------------------------------------------------------- 声 / 语音
def build_sound_index():
    d = L.jload(L.P_RT_ANIMFX_SOUNDS)
    cues = {}
    for c in d.get("cues") or []:
        cues[c.get("cue") or c.get("name")] = c.get("clips") or []
    return cues


def wav_exists(clip):
    p = os.path.join(L.ASSETS, "CardPresentation/Resources/Art/audio/sfx", clip + ".wav")
    return os.path.exists(p)


def voice_exists(name):
    return os.path.exists(os.path.join(L.ASSETS, "CardPresentation/Resources/Art/audio/vo", name + ".ogg"))


# ---------------------------------------------------------------- 主线
def main():
    pool = L.load_cards()
    orig = L.load_orig_vfx()
    rt = L.load_rt_vfx()
    addr = L.load_anim_addr()
    prefabs = L.load_prefab_names()
    report = L.load_effect_report()
    ledger = L.load_ledger()
    animfx = L.load_animfx()
    clips = L.load_cardanim_clips()
    clips_rows = {r["a"]: r for r in clips["rows"]}
    rt_ev = L.rt_events_by_card(rt)
    cards = L.cards_of_faction(pool, FACTION)
    by_id = {c["id"]: c for c in cards}

    # 原版 byCard 是 {ev: [entry...]}（不是 {events:[...]}）
    def orig_ev(cid):
        v = orig["byCard"].get(cid) or {}
        return {k: v[k] for k in v if not k.startswith("_")}

    # 音效：animfx_modules → 每个 effect 的 cue
    cue_by_effect = {}
    for e in animfx["effects"]:
        cs = []
        for m in e.get("modules") or []:
            ks = m.get("keys") or []
            vs = m.get("values") or []
            for k, v in zip(ks, vs):
                if k.endswith(".sound") and isinstance(v, str) and v.startswith("@asset:"):
                    cs.append(v.split(":", 2)[-1])
        cue_by_effect[e["name"]] = cs
    sound_idx = build_sound_index()

    # trait 绑定（TraitFrames 那本）
    bind = L.jload(L.ASSETS + "/CardPresentation/Resources/trait_effect_bindings.json")
    bind_by_trait = collections.defaultdict(list)
    for r in bind["rows"]:
        bind_by_trait[r["trait"]].append(r)
    trait_meta = L.jload(L.ROOT + "/数据/游戏数据/trait_collection.json")
    # 每张卡的 keywords → trait 名（由关键词串取首词小写；只用于**对账**，不是播放判据）
    def keyword_traits(c):
        out = []
        for k in c.get("keywords") or []:
            s = k if isinstance(k, str) else (k.get("keyword") or k.get("name") or "")
            kw = s.split(":")[0].strip().lower()
            if kw:
                out.append(kw)
        return out

    rows_out = []
    card_sum = []
    for c in sorted(cards, key=lambda x: x["id"]):
        cid, nm, fa = c["id"], c["name"], c["faction"]
        oev = orig_ev(cid)
        rev = rt_ev.get(cid, {})
        anim_times = c.get("animTimes") or []
        tn_by_anim = collections.defaultdict(list)
        for t in anim_times:
            tn_by_anim[t.get("a") or ""].append(t)

        buckets = sorted(set(oev) | set(rev))
        n_rows = 0
        flags_card = []
        for b in buckets:
            o = oev.get(b) or []
            r = rev.get(b) or []
            # 每个条目一行；同桶多条目按序
            n = max(len(o), len(r))
            for i in range(n):
                oe = o[i] if i < len(o) else None
                re_ = r[i] if i < len(r) else None
                prefab = (re_ or {}).get("n") or (oe or {}).get("prefab")
                can = (re_ or {}).get("a") or (oe or {}).get("cardanim")
                conf = (re_ or {}).get("c") or (oe or {}).get("conf")
                tns = sorted(set(t.get("tn") or "" for t in tn_by_anim.get(can or "", [])))
                # 原版 CardAnim 的落点（判「播的是不是原版那件」）
                ca = addr["cardanim_to_asset"].get(can or "") or {}
                ca_target = ca.get("targetName")
                ai = animinfo_of(can, addr) if can else None
                ex = extra_param_flags(ai) if ai else []
                led = ledger.get(prefab or "") or {}
                rep = report.get(prefab or "") or {}
                in_lib = (prefab or "") in prefabs
                # trait 覆盖（运行期不按阵营取 —— 见 bind['conventions']['overrides']）
                over = []
                for tr in keyword_traits(c):
                    for br in bind_by_trait.get(tr, []):
                        if br.get("overrides"):
                            over.append((tr, br.get("triggerType"), br.get("prefab"),
                                         [x.get("prefab") for x in br["overrides"]]))
                rows_out.append(dict(
                    card=cid, name=nm, faction=fa, bucket=b, idx=i,
                    orig_has=("Y" if oe else "N"), rt_has=("Y" if re_ else "N"),
                    prefab=prefab or "", cardanim=can or "", conf=conf or "",
                    ca_target=ca_target or "", ca_match=("Y" if (ca_target and ca_target == prefab) else ("-" if not ca_target else "N")),
                    in_lib=("Y" if in_lib else "N"),
                    ledger_verdict=led.get("判定", ""), report_verdict=rep.get("判定", ""),
                    trig_names="|".join(tns),
                    extra_params="|".join(ex),
                    overrides="|".join("%s:%s→%s" % (t, tt, pp) for t, tt, pp, _ in over),
                ))
                n_rows += 1
                if not in_lib:
                    flags_card.append("库里没有:" + (prefab or "?"))
                if ca_target and ca_target != prefab:
                    flags_card.append("CardAnim落点不符:%s→%s" % (can, ca_target))
        # 语音
        refs = (orig["byCard"].get(cid) or {}).get("_assetRefs") or []
        vo = {}
        for r in refs:
            f = r.get("soField") or ""
            if f in ("summonAudio", "attackAudio", "deathAudio"):
                v = r.get("assetName") or ""
                if v:
                    vo[f] = (v, voice_exists(v))
        card_sum.append(dict(
            card=cid, name=nm, rows=n_rows,
            buckets="|".join("%s:%d/%d" % (b, len(oev.get(b) or []), len(rev.get(b) or []))
                             for b in buckets),
            has_animtimes=("Y" if anim_times else "N"), animtimes=len(anim_times),
            ability_anim_times=sum(1 for t in anim_times if t.get("tg") == -1),
            voice=";".join("%s=%s(%s)" % (k, v[0], "ok" if v[1] else "缺") for k, v in vo.items()),
            flags=";".join(sorted(set(flags_card))),
        ))

    os.makedirs(OUT_DIR, exist_ok=True)
    def write_tsv(name, rows):
        cols = list(rows[0].keys()) if rows else []
        with open(os.path.join(OUT_DIR, name), "w", encoding="utf-8", newline="\n") as f:
            f.write("\t".join(cols) + "\n")
            for r in rows:
                f.write("\t".join(str(r.get(k, "")) for k in cols) + "\n")
    write_tsv("an1_sautekh_rows.tsv", rows_out)
    write_tsv("an1_sautekh_cards.tsv", card_sum)

    # ---- 汇总 ----
    print("== Sautekh ==", len(cards), "张")
    bc = collections.Counter(r["bucket"] for r in rows_out)
    print("rows by bucket:", bc.most_common())
    print("in library missing:", sum(1 for r in rows_out if r["in_lib"] != "Y"))
    print("cardanim target mismatch:", sum(1 for r in rows_out if r["ca_match"] == "N"))
    print("ledger verdicts:", collections.Counter(r["ledger_verdict"] for r in rows_out).most_common())
    print("report verdicts:", collections.Counter(r["report_verdict"] for r in rows_out).most_common())
    ext = collections.Counter()
    for r in rows_out:
        for x in (r["extra_params"] or "").split("|"):
            if x:
                ext[x.split("=")[0]] += 1
    print("extra params:", ext.most_common())
    print("curves-animinfo-missing:", sum(1 for r in rows_out if r["extra_params"] == "<无 animInfo>"))
    print("override rows:", sum(1 for r in rows_out if r["overrides"]))
    print("cards without animTimes:", [r["card"] for r in card_sum if r["has_animtimes"] != "Y"])
    print("voice files:", collections.Counter(
        v.split("=")[-1] for r in card_sum for v in r["voice"].split(";") if v))


if __name__ == "__main__":
    main()
