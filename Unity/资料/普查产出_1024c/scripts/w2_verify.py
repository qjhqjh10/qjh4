# -*- coding: utf-8 -*-
"""W2 · R4 / R7 / R13 / R14 的**校验器**（只读；⛔ 不改任何产物）。

用法：
    python -I -X utf8 w2_verify.py

验四件事（每条都写清「改坏哪里会红」= 灭自证）：
  · **R4-A** `ability` 桶里**只许**有主动技能那一拍的条目（`abilityEventClass == "ability"`）。
       🧨 把 `trigger_event()` 的兜底改回 `return EV_ABILITY, "未归口…"` ⇒ 红。
  · **R4-B** 原版 `cardAbilities[]` / `activeAbility` 的**每一条 `cardAnimations` GUID 都还在**
       （行数守恒；⛔ 不许「归口时静默丢行」）。判据 = 当前 build 的卡 SO 真值快照。
       🧨 把某条分支写成 `continue` 丢掉 ⇒ 红。
  · **R4-C** 「在能力拍上错播」= **0 行**（复算 AN1 `an1_bucket_matrix.py` 的判据：
       `ability` 桶里的条目钉到 `CardAnimTime` 后 `tg != -1` 的条数）。
       🧨 把 R4 改回去 ⇒ 从 0 变成 456（AN1 2026-10-24 现读）。
  · **R4-D** `trigger_unmapped` 里的名字**都不是** `VfxMap.IsRoutedTriggerName` 放行的
       （放行了就该走 `trigger`）；且它们都在 `NoEmitTriggerNames` 或引擎里没有发射口（逐个列出）。
  · **R7/R13/R14** `cardanim_clips.json` 里三族字段的行数与逐值抽样（与 `animInfo` 原值比）。
       🧨 把 `_shake_block` / `_minion_move_block` / `sd` 删掉 ⇒ 红。
"""
import io
import json
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, _HERE)
import w2_classify as W  # noqa: E402  （白名单解析 / 分类口径**只此一份**）

ROOT = "d:/4/Unity"
CARDS = ROOT + "/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
FULL = ROOT + "/数据/游戏数据/card_vfx_by_card.json"
RT = ROOT + "/MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json"
TRUTH = ROOT + "/资料/普查产出_1018/allcards_1350/cards_raw.json"
CLIPS_FULL = ROOT + "/数据/游戏数据/cardanim_clips.json"
CLIPS_RT = ROOT + "/MyGame/Assets/CardPresentation/Resources/cardanim_clips.json"
ASSETS_FULL = "d:/2/新解包资源/assets_full"

FAILS = []


def check(cond, msg):
    if cond:
        print("  ✅ " + msg)
    else:
        print("  ❌ " + msg)
        FAILS.append(msg)


def main():
    routed = W.routed_names()
    routed_lc = set(n.lower() for n in routed)
    noemit = W.no_emit_names()
    d = json.load(io.open(FULL, encoding="utf-8"))
    rt = json.load(io.open(RT, encoding="utf-8"))
    cards = json.load(io.open(CARDS, encoding="utf-8"))
    by = {c["id"]: c for c in cards["cards"]}

    print("== R4-A · `ability` 桶只许有主动技能那一拍 ==")
    bad = []
    ability_rows = 0
    for cid, m in d["byCard"].items():
        for e in (m.get("ability") or []):
            ability_rows += 1
            if e.get("abilityEventClass") != "ability":
                bad.append((cid, e.get("abilityTriggerName"), e.get("prefab")))
    check(not bad, "ability 桶 %d 行，全部 `abilityEventClass == \"ability\"`%s"
                   % (ability_rows, "" if not bad else "；混进 %d 条：%s" % (len(bad), bad[:5])))

    print()
    print("== R4-B · 行数守恒（原版 cardAbilities/activeAbility 的每个动画 GUID 都还在）==")
    truth = json.load(io.open(TRUTH, encoding="utf-8"))
    truth_cards = truth["cards"] if isinstance(truth, dict) else truth
    tby = {}
    for t in truth_cards:
        raw = t.get("full_raw") or t
        if raw.get("uniqueId"):
            tby[raw["uniqueId"]] = raw
    miss, extra, nc = [], [], 0
    for cid in sorted(set(by) & set(d["byCard"])):
        raw = tby.get(cid)
        if not raw:
            continue
        want = []
        for ab in raw.get("cardAbilities") or []:
            for lg in ab.get("abilityLogic") or []:
                for a in lg.get("cardAnimations") or []:
                    g = (a or {}).get("m_AssetGUID")
                    if g:
                        want.append(g.lower())
        for lg in (raw.get("activeAbility") or {}).get("activeAbilityLogic") or []:
            for a in lg.get("cardAnimations") or []:
                g = (a or {}).get("m_AssetGUID")
                if g:
                    want.append(g.lower())
        if not want:
            continue
        nc += 1
        got = []
        for ev, lst in d["byCard"][cid].items():
            if not isinstance(lst, list):
                continue
            for e in lst:
                g = (e.get("guid") or "").lower()
                if g:
                    got.append(g)
        for g in want:
            if g not in got:
                miss.append((cid, g))
    check(not miss, "逐卡 %d 张：原版每个 `cardAnimations` GUID 都还在（缺 %d 条%s）"
                    % (nc, len(miss), "" if not miss else "：" + str(miss[:5])))

    print()
    print("== R4-C · 「在能力拍上错播」= 0 行（AN1 判据复算，**按桶 + 按触发档**判，不按名字首行猜）==")
    # ⚠️ 匹配法要严：同一个 `CardAnim` 名可以在**另一条 CardAbility** 下各出现一次
    #   （现读 4 条：`SOR66` 的 `Buff_Sororitas_quick_target`/`Buff_Purple_target` 同时挂
    #    `ThisCardPlayed` 与 `activeAbility`；`SOR34` 的 `SororitasSummon_Basic` 同时挂 `UseFaith`；
    #    `SOR70` 同时挂 `TurnEnd`）⇒ **只按名字取第一条** 会把这 4 条判成「错拍」（假红，
    #   AN1 的 `an1_bucket_matrix.py` 就是这么算的 —— 它取 `rows[0]`）。
    #   判据改成：`ability` 桶的每一条，必须能在这张卡的 `animTimes` 里找到**同名且 trigger ∈
    #   {-1, 400, 600, 605}** 的那一行（= 主动技能那一族）。
    ABIL_TG = {-1, 400, 600, 605}
    wrong = []
    for cid, m in d["byCard"].items():
        times = (by.get(cid) or {}).get("animTimes") or []
        ok = {}
        for t in times:
            a = t.get("a")
            if a and t.get("tg") in ABIL_TG:
                ok[a] = ok.get(a, 0) + 1
        for e in (m.get("ability") or []):
            a = e.get("cardanim") or ""
            if ok.get(a, 0) > 0:
                ok[a] -= 1
            else:
                wrong.append((cid, a, e.get("prefab")))
    check(len(wrong) == 0,
          "`ability` 桶里**找不到主动技能那一拍**对应 `CardAnimTime` 的行数 = **%d**"
          "（改前 = 456「tg != -1」口径；AN1 2026-10-24 现读）%s"
          % (len(wrong), "" if not wrong else "：" + str(wrong[:5])))

    print()
    print("== R4-D · `trigger_unmapped` = 引擎没有发射口的那批 ==")
    unm = {}
    umb = 0
    for cid, m in d["byCard"].items():
        for e in (m.get("trigger_unmapped") or []):
            umb += 1
            nm = e.get("abilityTriggerName") or "（空）"
            unm.setdefault(nm, set()).add(cid)
    leak = [n for n in unm if n.lower() in routed_lc]
    check(not leak, "`trigger_unmapped` %d 行 / %d 个名字，无一在 `IsRoutedTriggerName` 白名单里%s"
                    % (umb, len(unm), "" if not leak else "；漏了：" + str(leak)))
    unknown = [n for n in unm if n not in noemit]
    check(not unknown,
          "每个名字都是「已登记的没有发射口」（`NoEmitTriggerNames`）%s"
          % ("" if not unknown else "；⚠️ 未登记但有名的：%s（下次核一遍发射口）" % unknown))
    for n in sorted(unm):
        print("     · %-24s %d 卡%s" % (n, len(unm[n]), "（`NoEmitTriggerNames`）" if n in noemit else ""))

    print()
    print("== R4-F · 挪进 `trigger` 的行**能被运行期按名字挑到**（case ③ 的可达性）==")
    #   运行期 `TryResolveTriggerPicks` ③ 靠 `CardAnimTime.TriggerName` 给候选标名 ⇒ 一条候选要能被
    #   按名挑到，**必须**在这张卡的 `animTimes` 里有一条同名 `a` 且 `tn == 它的 trigger 名` 的行。
    unlabelled, unlabelled_unm = [], []
    for cid, m in d["byCard"].items():
        times = (by.get(cid) or {}).get("animTimes") or []
        lab = {}
        for t in times:
            if t.get("a") and t.get("tn"):
                lab.setdefault(t["a"], set()).add(t["tn"].lower())
        for e in (m.get("trigger") or []):
            if e.get("abilityEventClass") != "routed":
                continue
            nm = (e.get("abilityTriggerName") or "").lower()
            if nm not in lab.get(e.get("cardanim") or "", set()):
                unlabelled.append((cid, e.get("cardanim"), e.get("abilityTriggerName")))
        for e in (m.get("trigger_unmapped") or []):
            nm = (e.get("abilityTriggerName") or "").lower()
            if nm and nm not in lab.get(e.get("cardanim") or "", set()):
                unlabelled_unm.append((cid, e.get("cardanim"), e.get("abilityTriggerName")))
    check(not unlabelled, "`trigger` 桶里 `routed` 的行都能按 `CardAnimTime.TriggerName` 标上名（标不上 %d 条%s）"
                          % (len(unlabelled), "" if not unlabelled else "：" + str(unlabelled[:5])))
    check(not unlabelled_unm, "`trigger_unmapped` 的行同样能标上名（标不上 %d 条%s）"
                              % (len(unlabelled_unm), "" if not unlabelled_unm else "：" + str(unlabelled_unm[:5])))

    print()
    print("== R4-E · 桶分布（全量件 → 运行期件：差额必须**只有**两类既定剔除）==")
    # 运行期那份**有意**剔两类（判据 = 生成器的 `RT_SKIP_SOFIELDS` 与 `rt_noprefab`）：
    #   ① `_assetRefs`（立绘/语音，不挂事件）整档不投影；
    #   ② `remnantBodyPrefab`（`soField` 是它）= 语义与 `death` 不同，**故意**不进运行期；
    #   ③ 没解析出 prefab 名的条目（`Wait_*` 纯计时拍 / 2 个解不出的 GUID）。
    RT_SKIP = {"remnantBodyPrefab"}
    exp, full_c = {}, {}
    for m in d["byCard"].values():
        for ev, lst in m.items():
            if not isinstance(lst, list) or ev == "_assetRefs":
                continue
            full_c[ev] = full_c.get(ev, 0) + len(lst)
            for e in lst:
                base = (e.get("soField") or "").split("[")[0]
                if base in RT_SKIP or not e.get("prefab"):
                    continue
                exp[ev] = exp.get(ev, 0) + 1
    cr = {}
    for c in rt["cards"]:
        for e in (c.get("events") or []):
            cr[e["ev"]] = cr.get(e["ev"], 0) + len(e.get("es") or [])
    diff = []
    for k in sorted(set(full_c) | set(cr) | set(exp)):
        mark = "✅" if exp.get(k, 0) == cr.get(k, 0) else "❌"
        if exp.get(k, 0) != cr.get(k, 0):
            diff.append(k)
        print("     %-18s 全量 %-5d 期望(运行期) %-5d 实得 %-5d %s"
              % (k, full_c.get(k, 0), exp.get(k, 0), cr.get(k, 0), mark))
    check(not diff, "运行期条数 == 全量减去两类既定剔除（不一致：%s）" % (diff or "无"))

    print()
    print("== R7 / R13 / R14 · `cardanim_clips.json` 三族字段 ==")
    cf1 = json.load(io.open(CLIPS_FULL, encoding="utf-8"))
    cr1 = json.load(io.open(CLIPS_RT, encoding="utf-8"))
    check(cf1["count"] == cr1["count"],
          "两份 `cardanim_clips.json` 行数一致（%d）" % cf1["count"])
    rows = {r["a"]: r for r in cf1["rows"]}
    check(all(io.open(CLIPS_FULL, "rb").read() == io.open(CLIPS_RT, "rb").read() for _ in [0]),
          "全量件与运行期件**逐字节相同**")

    # ---- 逐值抽核：直接从 assets_full 的 animInfo 再读一遍（**不是**回读自己写的那份）----
    am = json.load(io.open(ROOT + "/数据/索引/anim_address_map.json", encoding="utf-8"))["cardanim_to_asset"]
    sd_exp, ss_exp, mm_exp = {}, {}, {}
    for ca, info in am.items():
        src = info.get("sourceMono") or ""
        if not src:
            continue
        p = os.path.join(ASSETS_FULL, src)
        if not os.path.exists(p):
            continue
        ai = json.load(io.open(p, encoding="utf-8", errors="replace")).get("animInfo") or {}
        if float(ai.get("startDelay") or 0.0):
            sd_exp[ca] = round(float(ai["startDelay"]), 6)
        ss = ai.get("screenShake") or {}
        ps = ss.get("presetSO") or {}
        ov = [k for k in ("overwriteSustainTime", "overwriteAmplitude", "overwriteFrequency",
                          "overwriteDirection", "overwriteAttackTime", "overwriteDecayTime")
              if int(ss.get(k) or 0)]
        if (ps.get("m_PathID") or 0) or ov or float(ss.get("delay") or 0.0):
            ss_exp[ca] = {"pid": str(ps.get("m_PathID") or 0),
                          "delay": round(float(ss.get("delay") or 0.0), 6),
                          "amp": round(float(ss.get("amplitude") or 0.0), 6),
                          "sustain": round(float(ss.get("sustainTime") or 0.0), 6),
                          "ov": sorted(ov)}
        mm = ai.get("minionMovementInfo") or {}
        cur_nonempty = any(((mm.get(k) or {}).get("m_Curve") or [])
                           for k in ("minionMovementAnimCurveX", "minionMovementAnimCurveY",
                                     "minionMovementAnimCurveZ"))
        nondef = (int(mm.get("shouldActivateMinionMovement") or 0)
                  or int(mm.get("visibilityStateAfterFinish") or 0)
                  or float(mm.get("delay") or 0.0) or float(mm.get("xOffset") or 0.0)
                  or float(mm.get("yOffset") or 0.0) or float(mm.get("zOffset") or 0.0)
                  or int(mm.get("useXZCurve") or 0) or int(mm.get("useYCurve") or 0)
                  or cur_nonempty
                  or any(int((mm.get(k) or {}).get("enable") or 0)
                         or ((mm.get(k) or {}).get("curve") or {}).get("m_Curve")
                         for k in ("rotateXAxis", "rotateYAxis", "rotateZAxis")))
        if nondef:
            mm_exp[ca] = {"y": round(float(mm.get("yOffset") or 0.0), 6),
                          "act": int(mm.get("shouldActivateMinionMovement") or 0)}

    got_sd = {r["a"]: r["sd"] for r in cf1["rows"] if "sd" in r}
    check(got_sd == sd_exp, "R14 `sd`：期望 %d 行、实得 %d 行、值逐一相同 %s"
                            % (len(sd_exp), len(got_sd), "" if got_sd == sd_exp else "❌"))
    got_ss = {r["a"]: {"pid": r.get("ssPid"), "delay": r.get("ssDelay"),
                       "amp": r.get("ssAmp"), "sustain": r.get("ssSustain"),
                       "ov": sorted(k for k in ("ssOvSustain", "ssOvAmp", "ssOvFreq", "ssOvDir",
                                                "ssOvAttack", "ssOvDecay") if r.get(k))}
              for r in cf1["rows"] if "ssPid" in r}
    # 期望那边的 `ov` 是原版字段名，这里换算成短名再比
    _map = {"overwriteSustainTime": "ssOvSustain", "overwriteAmplitude": "ssOvAmp",
            "overwriteFrequency": "ssOvFreq", "overwriteDirection": "ssOvDir",
            "overwriteAttackTime": "ssOvAttack", "overwriteDecayTime": "ssOvDecay"}
    ss_exp2 = {k: {"pid": v["pid"], "delay": v["delay"], "amp": v["amp"],
                   "sustain": v["sustain"], "ov": sorted(_map[x] for x in v["ov"])}
               for k, v in ss_exp.items()}
    check(got_ss == ss_exp2, "R7 `ss*`：期望 %d 行、实得 %d 行、6 组开关＋值逐一相同 %s"
                             % (len(ss_exp2), len(got_ss), "" if got_ss == ss_exp2 else "❌ 差异：%s"
                                % [{k: (got_ss.get(k), ss_exp2.get(k))} for k in
                                   sorted(set(got_ss) | set(ss_exp2)) if got_ss.get(k) != ss_exp2.get(k)][:4]))
    ss_no_name = [r["a"] for r in cf1["rows"] if "ssPid" in r and "ssPreset" not in r]
    check(not ss_no_name, "R7 每条 `ss*` 都解出了 `ssPreset` 名（解不出：%s）" % (ss_no_name or "无"))
    got_mm = {r["a"]: {"y": r.get("mmY"), "act": r.get("mmAct")} for r in cf1["rows"] if "mmAct" in r}
    check(got_mm == mm_exp, "R13 `mm*`：期望 %d 行、实得 %d 行、抽样值（yOffset / shouldActivate）相同 %s"
                            % (len(mm_exp), len(got_mm), "" if got_mm == mm_exp else "❌"))

    print()
    if FAILS:
        print("❌ **%d 条红**：" % len(FAILS))
        for f in FAILS:
            print("   -", f)
        return 1
    print("✅ 全部通过（R4 + R7/R13/R14）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
