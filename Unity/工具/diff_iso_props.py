#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""diff_iso_props.py —— 逐槽位对比 `EffectIso` 两趟日志里的**材质全属性**。

用法（在 d:/4/Unity 下）：
    PYTHONIOENCODING=utf-8 python 工具/diff_iso_props.py                     # 全部效果
    PYTHONIOENCODING=utf-8 python 工具/diff_iso_props.py Buff_DA_Forest_Self # 只一个效果
    PYTHONIOENCODING=utf-8 python 工具/diff_iso_props.py --only-diff         # 只列有差异的槽

数据来源 = 两趟日志里 C# 侧 `EffectIso.DumpAllSlots()` 写的那一行：
    [props <side>] <效果名> :: <渲染器名><槽标签> | shader=<名> | k=v k=v …

🔴 **为什么要在日志外侧比，而不是在 C# 里进程内比**（`CompareMaterials` 那条路仍在，但它会骗人）：
   orig 趟里 **84 个源包都在场** ⇒ 我们的 `wf_shaders_extra.bundle` 会被**同内容顶掉**
   （`AssetBundle.LoadFromFile` 对已在进程里的内容返回 null），而从 `battleprefabs` 抽出来的那些
   shader 在 `HarvestFromLoadedBundles()` 里**也捡不回来**（名字不在那个包的容器里）
   ⇒ 导出侧那一槽会**保留占位材质**，`CompareMaterials` 于是报「原版 Everguild shader ≠ 导
   URP/Particles/Unlit」——**看着像缺陷，其实是那一趟的环境产物**。
   实测 2026-10-02：`Buff_DA_Forest_Self` 的 `Smoke Trails 槽#1` 就是这么被误报的
   （exp 趟同一个槽 = `Everguild/FX/Spiral Trail FX`，是对的）。
   本脚本两侧**各读自己那一趟的真值**，不吃这个坑。

⚠️ 行格式由 `EffectIso.DumpAllSlots` 的注释钉住 —— **改那一边要同步改这里**。
"""
import io
import os
import re
import sys
from collections import OrderedDict

BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
TMP = r"d:/4/_tmp_view"
DEFAULT = {
    "orig": os.path.join(TMP, "iso_orig.log"),
    "exp": os.path.join(TMP, "iso_exp.log"),
}

LINE = re.compile(r"^\s*\[props (?P<side>orig|exp)\] (?P<eff>.+?) :: (?P<key>.+?) \| "
                  r"shader=(?P<sh>.+?) \| (?P<props>.*)$")
# 拆 `k=v k=v`：值里可能有逗号空格（`RGBA(0.7, 0.7, 0.7, 1.0)`）⇒ 只在「空格 + 合法键名 + =」处切
SPLIT = re.compile(r"\s+(?=[A-Za-z_][A-Za-z0-9_]*=)")

# 这些属性是 Unity 自己按贴图/环境推出来的，两边必然一样，列出来只添噪声
NOISE = re.compile(r"^(unity_|_).*(_TexelSize|_HDR|_ST)$")


def parse(path, side):
    """→ OrderedDict {(效果, 槽): {"shader":…, "props": {名: 值}}}"""
    out = OrderedDict()
    if not os.path.exists(path):
        print(f"!! 缺日志：{path}")
        return out
    bad = 0
    with io.open(path, encoding="utf-8", errors="replace") as f:
        for line in f:
            m = LINE.match(line)
            if not m or m.group("side") != side:
                continue
            props = OrderedDict()
            for kv in SPLIT.split(m.group("props").strip()):
                if "=" not in kv:
                    bad += 1
                    continue
                k, v = kv.split("=", 1)
                props[k] = v
            out[(m.group("eff"), m.group("key"))] = {"shader": m.group("sh"), "props": props}
    if bad:
        print(f"⚠️  {os.path.basename(path)}：{bad} 个片段没解析出 `k=v`（值里带空格？）—— 格式可能变了")
    return out


def num(v):
    """RGBA(a, b, c, d) / (a, b, c, d) / 纯数字 → 一串 float；不是数字返回 None。"""
    if v is None:
        return None
    body = v.strip()
    for pre in ("RGBA(", "Vector4(", "("):
        if body.startswith(pre):
            body = body[len(pre):]
            break
    if body.endswith(")"):
        body = body[:-1]
    try:
        return [float(x) for x in body.split(",")]
    except ValueError:
        return None


def diff_props(a, b):
    """→ (差异行列表, 只在一侧出现的键数)"""
    lines, only = [], 0
    for k in a:
        if k not in b:
            only += 1
            lines.append(f"      属性 {k}: 原={a[k]} 导=**无此属性**")
            continue
        if a[k] == b[k]:
            continue
        va, vb = num(a[k]), num(b[k])
        extra = ""
        if va and vb and len(va) == len(vb):
            # 数值差异给个比值，省得人肉算（分母为 0 时不给）
            if len(va) == 4 and all(abs(x) > 1e-6 for x in va[:3]) and not k.endswith("_ST"):
                rs = [vb[i] / va[i] for i in range(3)]
                if max(rs) - min(rs) < 0.01:
                    extra = f"   ← 比例 {rs[0]:.3f}×"
        lines.append(f"      属性 {k}: 原={a[k]} 导={b[k]}{extra}")
    for k in b:
        if k not in a:
            only += 1
            lines.append(f"      属性 {k}: 原=**无此属性** 导={b[k]}")
    return lines, only


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    flags = {a for a in sys.argv[1:] if a.startswith("--")}
    want = args[0] if args else None

    o = parse(DEFAULT["orig"], "orig")
    e = parse(DEFAULT["exp"], "exp")
    if not o or not e:
        print("两趟日志都要在 —— 先跑 bash 工具/run_iso.sh")
        return 1

    keys = [k for k in o if k in e and (want is None or want in k[0])]
    missing = [k for k in o if k not in e and (want is None or want in k[0])]

    n_clean = n_diff = 0
    for eff, key in keys:
        a, b = o[(eff, key)], e[(eff, key)]
        lines = []
        if a["shader"] != b["shader"]:
            lines.append(f"      **shader**: 原「{a['shader']}」≠ 导「{b['shader']}」")
        pl, only = diff_props(a["props"], b["props"])
        lines += [l for l in pl if not NOISE.match(l.split()[1].split(":")[0])]
        if not lines:
            n_clean += 1
            if "--only-diff" not in flags:
                print(f"  ✓ {eff} :: {key}")
            continue
        n_diff += 1
        print(f"  ✗ {eff} :: {key}")
        print("\n".join(lines))

    print(f"\n=== 槽位合计 {len(keys)} 个：有差异 {n_diff} · 完全一致 {n_clean} ===")
    if missing:
        print(f"⚠️ 只在 orig 趟出现、exp 趟没有的槽 {len(missing)} 个（导出侧缺槽/少渲染器）：")
        for eff, key in missing:
            print(f"      {eff} :: {key}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
