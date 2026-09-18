# -*- coding: utf-8 -*-
"""screen_asset_impact.py — 特效台账 × prefab 资产：筛「哪个共享资产和 E 组方向强相关」

为什么要它：E 组（偏亮/偏暗）此前三个候选主因全部落空，教训是**没控制住「效果家族」**。
按 shader 筛、按 shader 种数筛都是**家族的部分代理**；按「prefab 里引用的具体资产（材质/贴图/网格
的 guid）」筛是**更细的家族切片**——一个材质被十几个 prefab 共用，用户多半同源。

口径（三条，缺一条就会误判）：
  ① E 率 = 「用了这个资产的效果里落 E 组的比例」，基线 = E组/全池（当前 151/957 = 15.8%）
  ② 方向 = 落 E 的那些里偏亮/偏暗各多少，基线偏亮率 84/151 = 55.6%
  ③ **必须做第一方对照**：同一个族（名字前缀 / fam / 发射器数）里，用它的 vs 不用的。
     只看 ① 会把「家族标记」当成锅 —— `Sprites/Mask.mat` 就是这种（E 率高但方向不偏）。

用法：
  PYTHONIOENCODING=utf-8 python 工具/screen_asset_impact.py            # 全量筛
  PYTHONIOENCODING=utf-8 python 工具/screen_asset_impact.py --min 8    # 只列 E 内 ≥8 条的资产
  PYTHONIOENCODING=utf-8 python 工具/screen_asset_impact.py --family Buff_   # 对某族做含/不含对照

数据源（都是只读）：
  资料/特效还原台账.tsv                 判定/亮度比中位/|ln|/技术构成（分组 E = 要定位的那批）
  数据/游戏数据/effect_index.json       效果名 → prefab 路径
  Assets/WarpforgeVFX/**.meta          guid → 资产名（材质/贴图/网格）
  _tmp_view/orig_material_index.json   原版材质索引（由本脚本 --build-index 生成）
"""
import argparse
import collections
import csv
import glob
import io
import json
import os
import re

ROOT = "D:/4/Unity"
LEDGER = ROOT + "/资料/特效还原台账.tsv"
INDEX = ROOT + "/数据/游戏数据/effect_index.json"
VFX = ROOT + "/MyGame/Assets/WarpforgeVFX"
ORIG_INDEX = "D:/4/_tmp_view/orig_material_index.json"
ORIG_ROOT = "D:/2/新解包资源/assets_full"

BR = "偏亮（导出更亮/更密）"
DA = "偏暗（导出更暗/更稀）"


def build_orig_index():
    """扫原版 bundle 的 Material/*.json，建「材质名 → 文件」索引（读前 300 字节拿 m_Name）。"""
    idx = {}
    n = 0
    for d in glob.glob(ORIG_ROOT + "/*/Material"):
        for f in glob.glob(d + "/*.json"):
            n += 1
            head = io.open(f, encoding="utf-8", errors="replace").read(300)
            m = re.search(r'"m_Name":\s*"([^"]*)"', head)
            if m:
                idx.setdefault(m.group(1), []).append(f)
    os.makedirs(os.path.dirname(ORIG_INDEX), exist_ok=True)
    json.dump(idx, io.open(ORIG_INDEX, "w", encoding="utf-8"), ensure_ascii=False)
    print(f"原版材质 json {n} 个 / 唯一名字 {len(idx)} → {ORIG_INDEX}")


def load_assets():
    """guid(8 位前缀) → 我们这边的资产名（材质/贴图/网格）。"""
    g2n = {}
    for m in glob.glob(VFX + "/**/*.meta", recursive=True):
        mm = re.search(r"guid: ([0-9a-f]{32})",
                       io.open(m, encoding="utf-8", errors="replace").read(400))
        if mm:
            g2n[mm.group(1)[:8]] = os.path.basename(m)[:-5]
    return g2n


def load_effects():
    idx = json.load(io.open(INDEX, encoding="utf-8"))
    return {e["name"]: ROOT + "/MyGame/" + e["prefab"] for e in idx["effects"]}


def load_ledger():
    return list(csv.DictReader(io.open(LEDGER, encoding="utf-8-sig"), delimiter="\t"))


def guid_cache(prefabs):
    cache = {}

    def guids(name):
        if name in cache:
            return cache[name]
        p = prefabs.get(name)
        s = set()
        if p and os.path.exists(p):
            s = {g[:8] for g in re.findall(
                r"guid: ([0-9a-f]{32})",
                io.open(p, encoding="utf-8", errors="replace").read())}
        cache[name] = s
        return s
    return guids


def two_way(rows, pred, label, baseline_e, baseline_br):
    hit = [r for r in rows if pred(r)]
    b = sum(1 for r in hit if r["判定"] == BR)
    d = sum(1 for r in hit if r["判定"] == DA)
    t = b + d
    if t == 0:
        return None
    return dict(label=label, n=t, bright=b, dark=d,
                e_rate=len(hit) / len(rows), bright_rate=b / t)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--min", type=int, default=8, help="E 组内至少用了几条才列（默认 8）")
    ap.add_argument("--family", default=None, help="对名字前缀为它的族做「含/不含」对照")
    ap.add_argument("--build-index", action="store_true", help="重建原版材质索引后退出")
    a = ap.parse_args()

    if a.build_index:
        build_orig_index()
        return

    g2n = load_assets()
    prefabs = load_effects()
    rows = load_ledger()
    guids = guid_cache(prefabs)
    er = [r for r in rows if r["分组"] == "E"]
    base_br = sum(1 for r in er if r["判定"] == BR) / len(er)

    if a.family:
        fam = [r for r in er if r["效果名"].startswith(a.family)]
        print(f"族 {a.family}* 共 {len(fam)} 条（E 组内）")
        cnt = collections.Counter()
        for r in fam:
            cnt.update(guids(r["效果名"]))
        for g, n in cnt.most_common(40):
            if n < 3:
                break
            name = g2n.get(g, "(不在 WarpforgeVFX)")
            with_ = two_way(fam, lambda r, gg=g: gg in guids(r["效果名"]), "含", None, None)
            with_out = two_way(fam, lambda r, gg=g: gg not in guids(r["效果名"]), "不含", None, None)
            if not with_ or not with_out:
                continue
            print(f"  {name:<38} 含 {with_['bright']:>3}亮/{with_['dark']:>3}暗 "
                  f"({with_['bright_rate']*100:>5.1f}%) | "
                  f"不含 {with_out['bright']:>3}亮/{with_out['dark']:>3}暗 "
                  f"({with_out['bright_rate']*100:>5.1f}%)")
        return

    cnt = collections.Counter()
    for r in er:
        cnt.update(guids(r["效果名"]))
    out = []
    for g, n in cnt.items():
        if n < a.min:
            continue
        users = [r for r in er if g in guids(r["效果名"])]
        allu = [r for r in rows if g in guids(r["效果名"])]
        b = sum(1 for r in users if r["判定"] == BR)
        d = sum(1 for r in users if r["判定"] == DA)
        if b + d < a.min:
            continue
        out.append(((b / (b + d)), b + d, len(allu), g2n.get(g, "(不在 WarpforgeVFX)"), b, d))
    print(f"E 组 {len(er)} 条（偏亮 {sum(1 for r in er if r['判定']==BR)} / "
          f"偏暗 {sum(1 for r in er if r['判定']==DA)}）· 基线偏亮率 {base_br*100:.1f}%")
    print(f"全池基线 E 率 = {len(er)}/{len(rows)} = {len(er)/len(rows)*100:.1f}%")
    print(f"\n{'资产':<44}{'E内':>5}{'总用':>5}{'偏亮':>5}{'偏暗':>5}{'偏亮率':>8}{'偏离':>8}")
    for rate, tot, allu, name, b, d in sorted(out, reverse=True):
        dev = (rate - base_br) * 100
        mark = " <<<< 强" if abs(dev) > 30 and tot >= 15 else ""
        print(f"{name[:43]:<44}{tot:>5}{allu:>5}{b:>5}{d:>5}{rate*100:>7.1f}%{dev:>+7.1f}%{mark}")


if __name__ == "__main__":
    main()
