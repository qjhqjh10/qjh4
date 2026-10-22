#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_effect_index.py — 生成效果索引 `数据/游戏数据/effect_index.json`

为什么要有索引
--------------
958 个导出的 prefab 平铺在 `Prefabs/` 里，没有索引就没法「按名字播一个特效」。
索引要回答的是：**这个名字对应哪个 prefab、它该活多久、原版是不是不让它自己销毁**。

数据来源（三份，都是现成的，不重新解析 bundle）
----------------------------------------------
1. `Prefabs/` 目录          —— 有哪些效果（名字即原版 prefab 名）
2. `animfx_components.json` —— 原版 AnimFXController 的 destroyTime / exitDestroyTime / preventDestroy
3. `特效还原台账.tsv`        —— 每个效果还原得怎么样（判定 / 置信度 / 亮度比）

为什么要单独跑这一趟，而不是让 C# 直接读那两个文件
--------------------------------------------------
`animfx_components.json` 是「效果名 → 组件列表」的**字典**，Unity 的 JsonUtility 解析不了字典
（它只认固定字段和数组）。所以在这里把三份数据 join 成**扁平数组**，Unity 那边才读得进。
分工是：Python 负责 join 数据，C#（EffectLibraryBuilder）负责连 prefab 资产、算粒子自然时长、
生成 ScriptableObject。

⚠️ 一个效果可能挂**多个** AnimFXController（实测 43 个效果是这样）。取值口径取「最保守」的：
   destroyTime / exitDestroyTime 取**最大**（要等所有子控制器都播完），
   preventDestroy 取「**全部**都设了才为真」（只要有一个自己管销毁，就得我们自己收）。

用法：
  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/gen_effect_index.py"
  # --check 只对账不写文件
"""
import argparse
import collections
import datetime
import io
import json
import os
import sys

ROOT = r"d:/4/Unity"
ANIMFX = os.path.join(ROOT, "数据/游戏数据/animfx_components.json")
LEDGER = os.path.join(ROOT, "资料/特效还原台账.tsv")
PREFAB_DIR = os.path.join(ROOT, "MyGame/Assets/WarpforgeVFX/Prefabs")
PREFAB_REL = "Assets/WarpforgeVFX/Prefabs"          # 工程内相对路径，写进索引给 Unity 用
OUT = os.path.join(ROOT, "数据/游戏数据/effect_index.json")


def load_animfx():
    """{效果名: {'destroyTime':…, 'exitDestroyTime':…, 'preventDestroy':…, 'modules':[...]}}"""
    if not os.path.exists(ANIMFX):
        return {}
    d = json.load(io.open(ANIMFX, encoding="utf-8"))
    out = {}
    for name, comps in d.get("effects", {}).items():
        ctrls = [c for c in comps if c.get("class") == "AnimFXController"]
        modules = [c["class"] for c in comps if c.get("class") != "AnimFXController"]
        rec = {"modules": sorted(set(modules)), "controllers": len(ctrls)}
        if ctrls:
            f = [c.get("fields", {}) for c in ctrls]
            # 取最保守的一组值（多控制器时），理由见文件头
            rec["destroyTime"] = max(_f(x, "destroyTime") for x in f)
            rec["exitDestroyTime"] = max(_f(x, "exitDestroyTime") for x in f)
            rec["preventDestroy"] = all(bool(x.get("preventDestroy")) for x in f)
        out[name] = rec
    return out


def _f(fields, key, default=0.0):
    v = fields.get(key)
    return float(v) if isinstance(v, (int, float)) else default


def load_ledger():
    """{效果名: {'verdict':…, 'confidence':…, 'ratio':…}}"""
    out = {}
    if not os.path.exists(LEDGER):
        return out
    for i, line in enumerate(io.open(LEDGER, encoding="utf-8-sig")):
        if i == 0:
            continue
        c = line.rstrip("\n").split("\t")
        if len(c) < 7 or not c[0]:
            continue
        ratio = None
        try:
            ratio = float(c[3]) if c[3] else None
        except ValueError:
            pass
        out[c[0]] = {"verdict": c[2], "confidence": c[5], "ratio": ratio}
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只对账，不写文件")
    args = ap.parse_args()

    animfx = load_animfx()
    # join 键**两侧 Trim**：原版有一个效果名带**尾随空格**（`"Orbital 5 repeat "`，见
    # `EffectLibraryBuilder.JoinKey` 的注释），而这里的 `name` 来自 **Windows 文件名**
    # （文件系统落不了尾随空格）⇒ 逐字 `animfx.get(name)` 会**静默 miss**：那条效果在索引里成了
    # `hasController: false`，寿命退回 natural 兜底、`preventDestroy` 也丢了。
    # ⚠️ 只用来**查**，⛔ 别原地改 `animfx` 的键 —— 下面 orphan 那一段要报**原始**名字。
    animfx_by_key = {k.strip(): v for k, v in animfx.items()}
    ledger = load_ledger()
    prefabs = sorted(os.path.splitext(f)[0]
                     for f in os.listdir(PREFAB_DIR) if f.endswith(".prefab"))
    print(f"prefab {len(prefabs)} 个 / animfx 数据 {len(animfx)} 个 / 台账 {len(ledger)} 条")

    effects = []
    no_ctrl = no_ledger = 0
    for name in prefabs:
        a = animfx_by_key.get(name.strip())
        l = ledger.get(name, {})
        has_ctrl = a is not None and a.get("controllers", 0) > 0
        if not has_ctrl:
            no_ctrl += 1
        if not l:
            no_ledger += 1
        effects.append({
            "name": name,
            "prefab": f"{PREFAB_REL}/{name}.prefab",
            "hasController": has_ctrl,
            # 原版没有 AnimFXController 时记 -1（Unity 那边读成 <0 = 没有数据），
            # 生命周期退到「粒子自然时长 → SAFE_DESTROY_TIME」
            "destroyTime": a.get("destroyTime", -1.0) if has_ctrl else -1.0,
            "exitDestroyTime": a.get("exitDestroyTime", -1.0) if has_ctrl else -1.0,
            "preventDestroy": bool(a.get("preventDestroy", False)) if has_ctrl else False,
            "modules": ",".join(a.get("modules", [])) if a else "",
            "controllers": a.get("controllers", 0) if a else 0,
            "verdict": l.get("verdict", ""),
            "confidence": l.get("confidence", ""),
            "ratio": l.get("ratio") if l.get("ratio") is not None else -1.0,
        })

    # ---- 对账：两边对不上的都要说出来，别静默吞掉 ----
    # ⚠️ 2026-10-22：判 orphan 也要用**同一个 Trim 键** —— 否则带尾随空格的那条（`Orbital 5 repeat `）
    #    会被误报成「有 animfx 数据但没有导出 prefab」，其实它**有** prefab，只是文件名落不了空格。
    #    报出来的是**原始名字**（`!r` 能把尾随空格显出来），方便回数据里查。
    _prefab_keys = {n.strip() for n in prefabs}
    ws_names = sorted(k for k in animfx if k != k.strip())
    if ws_names:
        print(f"ℹ️ 有 {len(ws_names)} 个原版效果名带首尾空白（join 时已两侧 Trim，"
              f"文件名落不了空白 ⇒ 索引里用 prefab 文件名）：")
        for n in ws_names:
            print(f"    {n!r}")

    orphan_animfx = sorted(n for n in animfx if n.strip() not in _prefab_keys)
    if orphan_animfx:
        print(f"\n⚠️ 有 animfx 数据但没有导出 prefab 的 {len(orphan_animfx)} 个（索引里不会有它们）：")
        for n in orphan_animfx[:10]:
            print(f"    {n}")
        if len(orphan_animfx) > 10:
            print(f"    … 其余 {len(orphan_animfx) - 10} 个")

    multi = [e for e in effects if e["controllers"] > 1]
    cnt = collections.Counter(e["verdict"] for e in effects)
    print(f"\n没有 AnimFXController（原版不自己销毁）: {no_ctrl} 个")
    print(f"挂多个 AnimFXController: {len(multi)} 个（取值取最保守的一组）")
    print(f"台账里没有的: {no_ledger} 个")
    print("判定分布: " + ", ".join(f"{k or '(空)'} {v}" for k, v in cnt.most_common()))

    doc = {
        "generated": datetime.datetime.now().strftime("%Y-%m-%d %H:%M"),
        "prefabDir": PREFAB_REL,
        "count": len(effects),
        "withController": sum(1 for e in effects if e["hasController"]),
        "source": "animfx_components.json + 特效还原台账.tsv + Prefabs/ 目录",
        "effects": effects,
    }

    if args.check:
        print("\n--check：不写文件")
        return 0

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, "w", encoding="utf-8") as f:
        json.dump(doc, f, ensure_ascii=False, indent=1)
    print(f"\n写出 {OUT}（{len(effects)} 个效果）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
