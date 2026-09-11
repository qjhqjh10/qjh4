#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""analyze_sweep.py — 把 EffectSweepBatch 的多时刻采样结果做成逐效果判定 + 台账

为什么不用单帧
--------------
原来只用 EffectCompare 的 Simulate(1.2s) 单帧判定，结果 280 个效果（29%）
「两边都是空的」—— 全是采样点不对（瞬时特效早播完了），不是还原失败。
**拿着一把不准的尺子逐个量，量出来的清单是错的。**

本脚本读 8 个时刻的采样数字（原版/导出两趟分别跑，在这里按效果名合并），逐效果给出：
- 两边在哪些时刻有内容（时序对齐情况）
- 在有内容的时刻，导出的亮度是原版的几倍（密度是否对得上）
- 一个可执行的判定 + 根因分组

⚠️ 用的是原始亮度和（不是 sRGB 转换后的那列）。实测过：`tex.GetPixels()` 拿到的值
和 EncodeToPNG 写出来的一致，再套一次 sRGB 变换会把比值全压到 0.9 附近，掩盖真实差异。

输入：`sweep_orig.tsv` + `sweep_exp.tsv`（EffectSweepBatch 的两趟产物）
输出：`d:/4/Unity/资料/特效还原台账.tsv`

用法：
  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/analyze_sweep.py"
"""
import collections
import io
import json
import os
import statistics
import sys

SWEEP_ORIG = r"d:/4/Unity/资料/比对基线/sweep_orig.tsv"
SWEEP_EXP = r"d:/4/Unity/资料/比对基线/sweep_exp.tsv"
LEDGER = r"d:/4/Unity/资料/特效还原台账.tsv"
OLD = r"d:/4/Unity/资料/比对基线/per_effect_metrics.json"
TECH = r"d:/4/Unity/资料/比对基线/per_effect_tech.json"

# 低于这个亮点数当噪声。256×256 = 65536 像素，60 个亮点 = 0.09%。
# ⚠️ 这个阈值只能挡住最离谱的噪声（实测有个效果原版只有 22 个亮点、导出 114 个，
#    比值算出来 5.38，纯属噪声）。再往上调不会把问题分得更干净 ——
#    只会把效果从 E 组挪进 W 组。真正的解法是**给判定加置信度**（见 conf()），
#    让小样本效果自己暴露出来，而不是硬给结论。
LIT_MIN = 60

# 峰值亮点数达到多少才算「这个效果确实有实质内容」
CONF_HI = 500
CONF_MID = 200


def conf(peak):
    """判定的置信度。峰值亮点太少时，两个小数字相除得出的比值没有意义。"""
    if peak >= CONF_HI:
        return "高"
    if peak >= CONF_MID:
        return "中"
    return "低"


def load_side(path):
    """{效果名: {时刻: (lit, sum)}}"""
    d = collections.defaultdict(dict)
    if not os.path.exists(path):
        return d
    for l in io.open(path, encoding="utf-8"):
        f = l.rstrip("\n").split("\t")
        if len(f) < 4 or f[0] == "effect":
            continue
        d[f[0]][float(f[1])] = (int(f[2]), float(f[3]))
    return d


def main():
    orig = load_side(SWEEP_ORIG)
    exp = load_side(SWEEP_EXP)
    if not orig or not exp:
        print(f"缺输入：orig={len(orig)} exp={len(exp)}。两趟都要跑（先 orig 后 exp）")
        return 1

    old = {}
    if os.path.exists(OLD):
        old = {m["name"]: m for m in json.load(io.open(OLD, encoding="utf-8"))}
    tech = json.load(io.open(TECH, encoding="utf-8")) if os.path.exists(TECH) else {}

    def tech_tag(n):
        t = tech.get(n)
        if not t:
            return ""
        tags = []
        if t.get("mesh"): tags.append("Mesh粒子")
        if t.get("sr"):   tags.append("精灵图")
        if t.get("psr", 0) >= 8: tags.append(f"多发射器({t['psr']})")
        elif t.get("psr"):       tags.append(f"{t['psr']}发射器")
        if t.get("fam"): tags.append("/".join(t["fam"]))
        return " ".join(tags)

    out = []
    for name in sorted(set(orig) | set(exp)):
        o, e = orig.get(name, {}), exp.get(name, {})
        times = sorted(set(o) | set(e))
        samples = [(t, o.get(t, (0, 0.0)), e.get(t, (0, 0.0))) for t in times]

        orig_on = [s for s in samples if s[1][0] >= LIT_MIN]
        exp_on = [s for s in samples if s[2][0] >= LIT_MIN]
        both = [s for s in samples if s[1][0] >= LIT_MIN and s[2][0] >= LIT_MIN]

        def span(lst):
            return f"{lst[0][0]:.2f}–{lst[-1][0]:.2f}s" if lst else "—"

        med = None
        if not orig_on and not exp_on:
            grp, verdict = "W", "两边全程空（完全脚本驱动 / 需要触发条件）"
        elif not orig_on:
            grp, verdict = "D", "只有导出有内容"
        elif not exp_on:
            grp, verdict = "C", "只有原版有内容 —— 导出整个丢了"
        elif not both:
            grp, verdict = "T", "有内容但从不同时出现（时序错位）"
        else:
            ratios = [(s[2][1] / s[1][1]) for s in both if s[1][1] > 0]
            med = statistics.median(ratios) if ratios else None
            if med is None:
                grp, verdict = "?", "有内容但亮度读不出来"
            elif 0.7 <= med <= 1.4:
                grp, verdict = "Z", "对得上"
            elif med > 1.4:
                grp, verdict = "E", "偏亮（导出更亮/更密）"
            else:
                grp, verdict = "E", "偏暗（导出更暗/更稀）"

        life = ""
        if orig_on and exp_on:
            if orig_on[-1][0] > exp_on[-1][0]:
                life = f"导出提前 {orig_on[-1][0]:.2f}s 结束"
            elif exp_on[-1][0] > orig_on[-1][0]:
                life = f"导出多播到 {exp_on[-1][0]:.2f}s"

        o_old = old.get(name, {})
        peak = max([s[1][0] for s in samples] + [s[2][0] for s in samples] or [0])
        out.append({
            "name": name, "grp": grp, "verdict": verdict, "med": med,
            "orig_span": span(orig_on), "exp_span": span(exp_on),
            "old_kind": o_old.get("kind", ""), "old_l1": o_old.get("l1"),
            "tech": tech_tag(name), "life": life,
            "peak": peak, "conf": conf(peak),
        })

    order = {"C": 0, "D": 1, "T": 2, "E": 3, "W": 4, "?": 5, "Z": 9}
    cout = {"高": 0, "中": 1, "低": 2}
    out.sort(key=lambda r: (order.get(r["grp"], 8), cout[r["conf"]],
                            r["med"] if r["grp"] == "E" and r["med"] is not None else 9, r["name"]))

    with io.open(LEDGER, "w", encoding="utf-8-sig", newline="") as f:
        f.write("\t".join(["效果名", "分组", "判定", "亮度比中位", "置信度", "峰值亮点",
                           "原版有内容时段", "导出有内容时段", "寿命差异",
                           "单帧旧判定", "单帧L1", "技术构成", "人工备注"]) + "\n")
        for r in out:
            f.write("\t".join([
                r["name"], r["grp"], r["verdict"],
                f"{r['med']:.2f}" if r["med"] is not None else "",
                r["conf"], str(r["peak"]),
                r["orig_span"], r["exp_span"], r["life"],
                r["old_kind"], f"{r['old_l1']:.4f}" if r["old_l1"] is not None else "",
                r["tech"], "",
            ]) + "\n")

    c = collections.Counter(r["grp"] for r in out)
    label = {"C": "只有原版有（导出整个丢了）", "D": "只有导出有", "T": "时序错位",
             "E": "亮度/密度不对", "W": "两边全程空", "?": "读不出", "Z": "对得上"}
    print(f"分析 {len(out)} 个效果 → {LEDGER}\n")
    print(f"{'判定':34s} {'数量':>5s}  {'高置信':>6s} {'中':>4s} {'低':>4s}   旧单帧里被判成")
    print("-" * 96)
    for g in sorted(c, key=lambda x: order.get(x, 8)):
        gs = [r for r in out if r["grp"] == g]
        cc = collections.Counter(r["conf"] for r in gs)
        olds = collections.Counter(r["old_kind"] for r in gs)
        print(f"{g} {label[g]:32s} {c[g]:5d}  {cc['高']:6d} {cc['中']:4d} {cc['低']:4d}   "
              + ", ".join(f"{k} {v}" for k, v in olds.most_common(3)))

    print("\n被旧尺子误判的（差异最大的几类）：")
    moved = collections.Counter((r["old_kind"], r["grp"]) for r in out if r["old_kind"])
    for (ok, ng), n in moved.most_common(12):
        if n >= 10 and ok != label.get(ng):
            print(f"   旧「{ok}」→ 新「{label.get(ng, ng)}」: {n} 个")
    return 0


if __name__ == "__main__":
    sys.exit(main())

