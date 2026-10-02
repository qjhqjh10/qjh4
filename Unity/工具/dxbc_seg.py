#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""dxbc_seg.py —— 读 `disasm_dxbc.py --out` 落下来的反汇编正文，按「段」取用。

为什么要有它：一个 shader 常常有**几百段**变体（`Matcap Full Options` 111 段、
`UnlitAmbient` 812 段），直接读会淹掉上下文；而「基础变体是哪一段」又不能靠肉眼翻。

用法（必须 UTF-8）：
  PYTHONIOENCODING=utf-8 python dxbc_seg.py <文件>                 # 列表：每段一行（指令数/采样数/纹理/输出 alpha）
  PYTHONIOENCODING=utf-8 python dxbc_seg.py <文件> --seg 12        # 打印第 12 段的完整正文
  PYTHONIOENCODING=utf-8 python dxbc_seg.py <文件> --min-inst 20 --max-inst 30
  PYTHONIOENCODING=utf-8 python dxbc_seg.py <文件> --first         # 打印**指令数最少**那段（含签名）

⚠️ 两个坑（都踩过）：
  · **指令行是顶格的**（D3DDisassemble 的输出没缩进）—— 按 `^\\s+` 匹配会一条都找不到。
  · 段起点 = 匹配 `^(ps|vs|gs|hs|ds|cs)_\\d_\\d` 的行；段与段之间还有一段 `//` 注释头。
"""
import io
import re
import sys


def segs(path):
    lines = io.open(path, encoding="utf-8", errors="replace").read().splitlines()
    idx = [i for i, l in enumerate(lines) if re.match(r"^(ps|vs|gs|hs|ds|cs)_\d_\d", l)]
    out = []
    for k, i in enumerate(idx):
        j = idx[k + 1] if k + 1 < len(idx) else len(lines)
        out.append((i + 1, j, lines[i:j]))
    return out


def info(body):
    ins = [l for l in body if re.match(r"^[a-z][a-z0-9_]*\s", l)]
    smp = [l for l in ins if l.startswith(("sample", "ld", "gather"))]
    texs = sorted(set(re.findall(r"\bt([0-9])\b(?!\w)", " ".join(smp))))
    cbs = sorted(set(re.findall(r"\bcb([0-9])\[(\d+)\]", " ".join(ins))), key=lambda t: (t[0], int(t[1])))
    lastw = [l for l in ins if re.match(r"^mov\s+o0\.w", l)]
    alpha = lastw[-1].split(",")[1].strip() if lastw else "-"
    return ins, smp, texs, cbs, alpha


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 1
    path = sys.argv[1]
    S = segs(path)
    if "--seg" in sys.argv:
        n = int(sys.argv[sys.argv.index("--seg") + 1])
        a, b, body = S[n]
        print("=== 段 %d（行 %d–%d，%d 条指令）===" % (n, a, b, len(info(body)[0])))
        for l in body:
            print(l)
        return 0
    if "--first" in sys.argv:
        k = min(range(len(S)), key=lambda i: len(info(S[i][2])[0]))
        a, b, body = S[k]
        print("=== 指令数最少的段 = 段 %d（行 %d–%d）===" % (k, a, b))
        for l in body:
            print(l)
        return 0
    lo = int(sys.argv[sys.argv.index("--min-inst") + 1]) if "--min-inst" in sys.argv else 0
    hi = int(sys.argv[sys.argv.index("--max-inst") + 1]) if "--max-inst" in sys.argv else 10 ** 9
    print("%-5s %-13s %5s %5s %-12s %-22s %s" % ("段", "行范围", "指令", "采样", "纹理", "cb 用到的槽", "o0.w"))
    for k, (a, b, body) in enumerate(S):
        ins, smp, texs, cbs, alpha = info(body)
        if not (lo <= len(ins) <= hi):
            continue
        cbspan = "cb" + ",".join("%s[%s]" % (c, i) for c, i in cbs[:6]) if cbs else "-"
        print("%-5d %-13s %5d %5d %-12s %-22s %s" % (
            k, "%d-%d" % (a, b), len(ins), len(smp), ",".join(texs) or "-", cbspan[:22], alpha))
    print("\n共 %d 段（用 --seg N 打印某一段的正文，--min-inst/--max-inst 过滤）" % len(S))
    return 0


if __name__ == "__main__":
    sys.exit(main())
