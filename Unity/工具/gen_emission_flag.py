# -*- coding: utf-8 -*-
"""gen_emission_flag.py — 从「原版基线」与「原版·关掉 _EMISSION」两趟 sweep，算出**逐效果的
「原版到底用没用 emission」**标记表。

为什么要这么算（别再改成按材质名/关键字猜）：
  · 本轮试过两种推断判据，**都不成立**（数据见 `资料/普查产出_0918/E组_共享资产筛_与EMISSION线索.md` §十）：
      ① 「`_EMISSION` 是全局的 ⇒ 两侧等效」 —— 被 Y 条件推翻；
      ② 「按粒子系统 Emission 模块判」   —— 被 Z≡Y（13/13 完全相同）推翻；
      ③ 按材质 `m_ValidKeywords` / `_EmissionColor` 非黑猜 —— 逐效果准确率只有 71–78%，误判上百条。
  · **唯一靠得住的是直接量**：把原版那趟的 `_EMISSION` 关掉再渲一遍，
    **数变了 = 原版在用**。这就是 ground truth，不需要任何推断。

口径：逐时点比 `sum`（亮度和）的相对差，取**最大**值；阈值默认 5%
（Chestrays 族实测 38–53%，`BlindEffect` 0.1% —— 两者离得很开，阈值不敏感）。

用法：
  PYTHONIOENCODING=utf-8 python 工具/gen_emission_flag.py \
      <原版基线.tsv> <原版·关EMISSION.tsv> [--write]

不带 --write 只打统计与抽查，不落盘。落盘位置：`数据/游戏数据/emission_flag.json`
（`EffectExporter` 导出时按效果名查它，写进 prefab 上 binder 的 `emissionOn`）。
"""
import argparse
import collections
import csv
import io
import json
import os

OUT = "D:/4/Unity/数据/游戏数据/emission_flag.json"
LEDGER = "D:/4/Unity/资料/特效还原台账.tsv"


def load(path):
    d = collections.defaultdict(dict)
    for l in io.open(path, encoding="utf-8-sig"):
        f = l.rstrip("\r\n").split("\t")
        if len(f) >= 6 and f[0] != "effect":
            d[f[0]][f[1]] = (int(f[2]), int(f[3]))
    return d


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("base", help="原版基线 sweep_orig.tsv")
    ap.add_argument("noemit", help="原版·关掉 _EMISSION 的那趟")
    ap.add_argument("--th", type=float, default=0.05, help="判定阈值（默认 5%%）")
    ap.add_argument("--write", action="store_true")
    a = ap.parse_args()

    B, N = load(a.base), load(a.noemit)
    ratio = {}
    for n in N:
        if n not in B:
            continue
        ds = [abs(N[n][t][1] - B[n][t][1]) / max(B[n][t][1], 1)
              for t in N[n] if t in B[n]]
        if ds:
            ratio[n] = max(ds)

    flag = {n: (v >= a.th) for n, v in ratio.items()}
    on = sum(1 for v in flag.values() if v)
    print(f"可比效果 {len(flag)} · 原版在用 emission 的 {on} 个（{on/max(len(flag),1)*100:.1f}%）· 阈值 {a.th:.0%}")

    if os.path.exists(LEDGER):
        led = {r["效果名"]: r for r in csv.DictReader(
            io.open(LEDGER, encoding="utf-8-sig"), delimiter="\t")}
        e = [n for n in flag if led.get(n, {}).get("分组") == "E"]
        b = sum(1 for n in e if flag[n] and led[n]["判定"].startswith("偏亮"))
        d = sum(1 for n in e if flag[n] and led[n]["判定"].startswith("偏暗"))
        b0 = sum(1 for n in e if not flag[n] and led[n]["判定"].startswith("偏亮"))
        d0 = sum(1 for n in e if not flag[n] and led[n]["判定"].startswith("偏暗"))
        print(f"E 组 {len(e)} 条：在用 → 偏亮{b} 偏暗{d} ｜ 没用 → 偏亮{b0} 偏暗{d0}")
        print("  （判定有效的标志：偏暗侧绝大多数应为「在用」、偏亮侧绝大多数应为「没用」）")

    print("\n抽查：")
    for n in ["Buff_Red", "Buff_Green", "KhaineBuff", "BlindEffect",
              "BulletImpact_artillery_arc", "InvulnerableEffect", "Air to Ground Big"]:
        if n in ratio:
            print(f"  {n:<34}{ratio[n]*100:>7.1f}%  {'在用' if flag[n] else '没用'}")

    if a.write:
        json.dump(flag, io.open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=0)
        print(f"\n已写 {OUT}（{len(flag)} 条）")
    else:
        print("\n（干跑，加 --write 才落盘）")


if __name__ == "__main__":
    main()
