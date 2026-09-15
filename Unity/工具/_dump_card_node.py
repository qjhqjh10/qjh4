#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""按 GameObject 名字，把它的 RectTransform / Image 字段读出来（只读，用于抄原版数值）。

⚠️ 两个坑（都踩过）：
   ① 解包目录里 **RectTransform 的 JSON 内部没有 PathID** —— PathID 在**文件名**里
      （`RectTransform_3372.json`）。只读 JSON 建映射，表是空的、一条都查不到。
   ② RectTransform JSON **不带 `m_Name`**（名字在 GameObject 里）⇒「按名字找 RT」必须走
      GO → `m_Component[].component.m_PathID` → RT 这一跳。

用法：PYTHONIOENCODING=utf-8 python Unity/工具/_dump_card_node.py [名字...]
"""
import io, json, os, sys, glob

ROOT = "d:/2/解包整理/07_场景/battlearena1"
DEFAULT_NAMES = ["2DCard", "Front", "Textbackgrounds", "TextBackground Big UI",
                 "TextBackground Small UI", "CardFrame", "CardImage"]


def load_dir(sub):
    out = {}
    for f in glob.glob(os.path.join(ROOT, sub, "*.json")):
        tail = os.path.basename(f).split(".")[0].split("_")[-1]
        if not tail.lstrip("-").isdigit():
            continue
        try:
            out[int(tail)] = json.load(io.open(f, encoding="utf-8"))
        except Exception:
            pass
    return out


def main():
    names = sys.argv[1:] or DEFAULT_NAMES
    gos, rts, mbs = load_dir("GameObject"), load_dir("RectTransform"), load_dir("MonoBehaviour")
    sys.stdout.reconfigure(encoding="utf-8")

    by_name = {}
    for pid, o in gos.items():
        n = o.get("m_Name")
        if n:
            by_name.setdefault(n, []).append((pid, o))

    for name in names:
        hits = by_name.get(name, [])
        if not hits:
            print(f"\n=== {name}   ⚠️ 没找到")
            continue
        for pid, go in hits:
            print(f"\n=== {name}   GO {pid}  activeSelf={go.get('m_IsActive')}")
            for c in go.get("m_Component") or []:
                cp = (c.get("component") or {}).get("m_PathID")
                if cp in rts:
                    r = rts[cp]
                    print("   RT  sizeDelta=%s  anchoredPos=%s  anchorMin=%s anchorMax=%s  pivot=%s  scale=%s"
                          % (r.get("m_SizeDelta"), r.get("m_AnchoredPosition"),
                             r.get("m_AnchorMin"), r.get("m_AnchorMax"),
                             r.get("m_Pivot"), r.get("m_LocalScale")))
                elif cp in mbs:
                    keep = {k: v for k, v in mbs[cp].items()
                            if k in ("m_Sprite", "m_Color", "m_Type", "m_PixelsPerUnitMultiplier", "m_Enabled")}
                    if keep:
                        print("   MB  %s" % json.dumps(keep, ensure_ascii=False))

    print("\n=== 名字里含 TextBackground 的全部 GO ===")
    for n in sorted(by_name):
        if "TextBackground" in n or "Textbackground" in n:
            print("   %-28s %d 个" % (n, len(by_name[n])))


if __name__ == "__main__":
    main()
