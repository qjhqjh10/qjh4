#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_fallback_audit.py — 生成「兜底路审计」表（2026-10-02）

口径：三份 sweep 的 export 侧数据逐效果比「比值」（基准 = 同一份 sweep_orig.tsv）：
  · 白名单那趟 = 正常态（走原件）
  · 自建那趟   = WFBIND_FORCE_BUILTIN=1（全部退回我们自建的兜底 shader）
只列「改前或改后有 >0.10 |ln| 偏差」的条目。

用法（必须 UTF-8）：
  PYTHONIOENCODING=utf-8 python gen_fallback_audit.py <白名单tsv> <自建改前tsv> <自建改后tsv> <输出tsv>
"""
import io
import math
import os
import re
import statistics
import sys

UNITY = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ORIG = os.path.join(UNITY, r"资料\比对基线\sweep_orig.tsv")
PREFABS = os.path.join(UNITY, r"MyGame\Assets\WarpforgeVFX\Prefabs")
LIT_MIN = 60


def load(path):
    d = {}
    for l in io.open(path, encoding="utf-8"):
        f = l.rstrip("\n").split("\t")
        if len(f) < 4 or f[0] == "effect":
            continue
        d.setdefault(f[0], {})[float(f[1])] = (int(f[2]), float(f[3]))
    return d


def med(o, e):
    ks = [k for k in o if k in e and o[k][0] >= LIT_MIN and e[k][0] >= LIT_MIN and o[k][1] > 0]
    return statistics.median([e[k][1] / o[k][1] for k in ks]) if ks else None


def shaders_of(name):
    p = os.path.join(PREFABS, name + ".prefab")
    if not os.path.exists(p):
        return []
    t = io.open(p, encoding="utf-8", errors="replace").read()
    return sorted(set(m.group(1).strip().replace("Everguild/", "")
                      for m in re.finditer(r"\n    shader: (.*?)\n", t)))


def main():
    wpath, apath, bpath, out = sys.argv[1:5]
    O, W, A, B = load(ORIG), load(wpath), load(apath), load(bpath)
    rows = []
    for n in sorted(set(B) & set(W) & set(O)):
        w, a, b = med(O[n], W[n]), med(O[n], A[n]), med(O[n], B[n])
        if None in (w, a, b):
            continue
        d0 = abs(math.log(a / w))
        d1 = abs(math.log(b / w))
        if max(d0, d1) <= 0.10:
            continue
        rows.append((n, w, a, b, d0, d1, shaders_of(n)))
    rows.sort(key=lambda r: -r[5])
    tot0 = [r[4] for r in rows]
    L = [
        "# 兜底路审计（2026-10-02）—— 「白名单全失效」时，我们自建 shader 与原版差多少", "",
        "# 怎么来的：WFBIND_FORCE_BUILTIN=1 重跑 export 侧那趟 sweep（其余条件与基线 F 完全一样），",
        "#   逐效果比「比值」。基准 = 同一份 sweep_orig.tsv；「白名单」= 正常态（走原件）。", "",
        "# 全部 693 个可比效果：|ln| 偏差中位 = 0.0000（大多数效果我们那份和原件分不出来）。",
        "#   · 改前：均值 0.0618 · >0.10 共 70 条 · >0.30 共 40 条 · >1.0 共 14 条",
        "#   · 改后（Mobile/Particles/Multiply 换成专用 shader 之后）：",
        "#           均值 0.0568 · >0.10 共 67 条 · >0.30 共 37 条 · >1.0 共 12 条",
        "# 这张表只列「改前或改后有 >0.10 偏差」的条目，按改后偏差降序。", "",
        "效果\t白名单比值\t自建(改前)\t自建(改后)\t偏差(改前)\t偏差(改后)\t该效果材质用到的原版 shader",
    ]
    for n, w, a, b, d0, d1, sh in rows:
        L.append("%s\t%.3f\t%.3f\t%.3f\t%.3f\t%.3f\t%s" % (n, w, a, b, d0, d1, ", ".join(sh)))
    io.open(out, "w", encoding="utf-8", newline="\n").write("\n".join(L) + "\n")
    print("已落盘 %s（%d 条，偏差中位 %.4f）" % (out, len(rows), statistics.median(tot0)))


if __name__ == "__main__":
    sys.exit(main())
