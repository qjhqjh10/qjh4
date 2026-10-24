# -*- coding: utf-8 -*-
"""AN1 · 引擎「触发关键词」发射口清单 + 与卡数据/白名单的交叉表。

只读。产出：
  · `data/an1_engine_emits.txt`   —— 引擎源码里**真的**带着这些关键词发 `EvtKind.Trigger` 的集合
  · `data/an1_trigger_matrix.tsv` —— 逐 (关键词) 一行：引擎发不发 · 白名单放不放行 · 卡池里多少卡/行
判据：
  · 发射点 = `RuleEngine/Core/*.cs` 里 `FireTriggerAt/FireTriggerAlways/FireTriggerOnSide/
    FireTriggerOnBoard/FireTriggerDeferred/EmitCardTrigger/BroadcastKeywordEvent(…, WhenEventKind.Triggers(X))`
  · 白名单 = `CardPresentation/Core/VfxMap.cs` 的 `IsRoutedTriggerName` 的 `case "…"` 标签（现读源码）
用法：`python -I -X utf8 an1_emit_inventory.py`
"""
import os, re, io, json, collections, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import an1_lib as L

OUT_DIR = L.ROOT + "/资料/普查产出_1024c/data"
RE_ENGINE = L.ASSETS + "/RuleEngine/Core"
RE_VFX = L.ASSETS + "/CardPresentation/Core/VfxMap.cs"


def keyword_consts():
    src = io.open(RE_ENGINE + "/CardDef.cs", encoding="utf-8").read()
    out = {}
    for m in re.finditer(r'public const string (\w+)\s*=\s*"([^"]*)"', src):
        out[m.group(1)] = m.group(2)
    return out


def engine_emits(kw):
    files = [f for f in os.listdir(RE_ENGINE) if f.endswith(".cs")]
    emits = collections.Counter()
    unresolved = collections.Counter()
    for f in files:
        src = io.open(os.path.join(RE_ENGINE, f), encoding="utf-8").read()
        for line in src.splitlines():
            if line.lstrip().startswith("//") or line.lstrip().startswith("///"):
                continue
            for fn in ("FireTriggerAt", "FireTriggerAlways", "FireTriggerOnSide",
                       "FireTriggerOnBoard", "FireTriggerDeferred", "EmitCardTrigger"):
                if fn + "(" not in line:
                    continue
                seg = line[line.index(fn + "("):]
                m = re.search(r"KeywordTable\.(\w+)", seg)
                if m and m.group(1) in kw:
                    emits[kw[m.group(1)].lower()] += 1
                    continue
                m = re.search(r'"([A-Za-z][A-Za-z0-9_ ]*)"', seg)
                if m and m.group(1) != "keyword":
                    emits[m.group(1).lower()] += 1
                else:
                    unresolved[f + ": " + line.strip()[:90]] += 1
            if "BroadcastKeywordEvent(" in line and "Triggers(" in line:
                m = re.search(r"Triggers\(\s*KeywordTable\.(\w+)", line)
                if m and m.group(1) in kw:
                    emits[kw[m.group(1)].lower()] += 1
    return emits, unresolved


def routed_names():
    src = io.open(RE_VFX, encoding="utf-8").read()
    i = src.index("static bool IsRoutedTriggerName")
    j = src.index("static string CanonicalTriggerName") if "static string CanonicalTriggerName" in src[i:] else len(src)
    body = src[i:j]
    names = set(m.group(1) for m in re.finditer(r'case\s+"([^"]+)"\s*:', body))
    # 两条 OrdinalIgnoreCase 的常量（ambush / triggerenchantment）
    for m in re.finditer(r"string\.Equals\(triggerName,\s*KeywordTable\.(\w+)", body):
        names.add("@kw:" + m.group(1))
    return names


def main():
    kw = keyword_consts()
    emits, unresolved = engine_emits(kw)
    routed = routed_names()
    pool = L.load_cards()

    # 卡池里出现过的 trigger 名（cards_engine.animTimes[].tn）→ 卡数/行数
    tn_cards = collections.defaultdict(set)
    tn_rows = collections.Counter()
    for c in pool["cards"]:
        for t in c.get("animTimes") or []:
            tn = (t.get("tn") or "").strip()
            if tn:
                tn_cards[tn].add(c["id"])
                tn_rows[tn] += 1

    # trait 绑定里 triggerType 3/6 的 trait 名（触发框那一族）
    bind = L.jload(L.ASSETS + "/CardPresentation/Resources/trait_effect_bindings.json")
    trait_trig = set(r["trait"] for r in bind["rows"] if r.get("triggerType") in (3, 6))

    allnames = sorted(set(x.lower() for x in tn_cards) | trait_trig | set(emits))
    os.makedirs(OUT_DIR, exist_ok=True)
    with open(OUT_DIR + "/an1_engine_emits.txt", "w", encoding="utf-8", newline="\n") as f:
        for k in sorted(emits):
            f.write("%s\t%d\n" % (k, emits[k]))
    with open(OUT_DIR + "/an1_trigger_matrix.tsv", "w", encoding="utf-8", newline="\n") as f:
        f.write("keyword\tengine_emit_sites\trouted_whitelist\tcards\tanim_rows\ttrait_3_6\tverdict\n")
        for n in allnames:
            em = emits.get(n, 0)
            ro = "Y" if (n in routed or ("@kw:" + n) in routed) else "N"
            cards = len(tn_cards.get(n, ()))
            rows = tn_rows.get(n, 0)
            tr = "Y" if n in trait_trig else ""
            if em and ro == "N":
                v = "发了但白名单不放行 → 整桶播/错件风险"
            elif em and ro == "Y":
                v = "OK（发+放行）"
            elif (not em) and tr == "Y":
                v = "trait 触发框：引擎**没有**发射口 → 永远不播"
            elif not em and rows:
                v = "卡数据有行但引擎**没有**发射口 → 永远不播"
            else:
                v = ""
            f.write("%s\t%d\t%s\t%d\t%d\t%s\t%s\n" % (n, em, ro, cards, rows, tr, v))

    print("engine emits:", len(emits))
    print("routed cases:", len([x for x in routed if not x.startswith("@kw:")]))
    print("unresolved trigger call lines:", len(unresolved))
    for k, v in unresolved.most_common(12):
        print("   ?", k)
    mat = [l.rstrip("\n").split("\t") for l in io.open(OUT_DIR + "/an1_trigger_matrix.tsv", encoding="utf-8")]
    h = mat[0]
    for want in ("发了但白名单不放行 → 整桶播/错件风险", "trait 触发框：引擎**没有**发射口 → 永远不播",
                 "卡数据有行但引擎**没有**发射口 → 永远不播"):
        sub = [r for r in mat[1:] if r[h.index("verdict")] == want]
        print(want, "→", len(sub))
        for r in sub[:20]:
            print("    ", r[0], "emit=%s routed=%s cards=%s rows=%s trait36=%s" % (r[1], r[2], r[3], r[4], r[5]))


if __name__ == "__main__":
    main()
