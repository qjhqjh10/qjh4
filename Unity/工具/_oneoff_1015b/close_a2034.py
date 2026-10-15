# -*- coding: utf-8 -*-
"""把 `A2034` 标结（用户 2026-10-15 拍板：改）。它在 `项目任务.md` §29·c。"""
import io

P = r"d:/4/项目任务.md"
s = io.open(P, encoding="utf-8", newline="").read()
lines = s.split("\n")
hit = [i for i, L in enumerate(lines) if L.startswith("| **`A2034`** |")]
assert len(hit) == 1, hit
i = hit[0]
print("行 %d" % (i + 1))
print(repr(lines[i][-160:]))
L = lines[i].rstrip()
assert L.endswith("|"), L[-30:]
if "2026-10-15 用户拍板" in L:
    print("已经标结过，跳过")
else:
    lines[i] = L[:-1].rstrip() + (
        " \u2795 **2026-10-15 \u7528\u6237\u62cd\u677f\uff1a\u6539** \u2014\u2014 \u5df2\u843d"
        "\uff08`\u5de5\u5177/gen_cards_engine.py:2343`\uff09\uff1a\u73b0\u8bfb "
        "`\u5361\u9762\u6838\u5bf9\u4fee\u6b63 \u5b57\u6bb5 {n_sub} \u5904\uff08= \u7b2c\u2462\u5c42\u76d6\u6389\u2461OCR \u7684\u5904\u6570\uff09`\uff0c"
        "\u5e76\u5199\u6e05\u300c\u5173\u952e\u8bcd\u300d\u90a3\u4e00\u5217\u7684\u53e3\u5f84"
        "\uff08\u5b83 = \u7b2c\u2462\u5c42\u4e0e**\u2460\u771f\u503c\u94fa\u5e95\u4e4b\u540e\u7684\u2461OCR** \u4e0d\u4e00\u81f4\u7684\u5361\u6570 "
        "\u21d2 **\u2260\u300c\u8f6c\u5f55\u6539\u4e86\u51e0\u5904\u300d**\uff09\u3002\u26d4 **\u8868\u8fbe\u5f0f\u4e00\u4e2a\u5b57\u6ca1\u52a8\u3001\u53ea\u6539\u5b57**\u3002"
        "\u5224\u636e \u2192 `\u8d44\u6599/\u666e\u67e5\u4ea7\u51fa_\u7b2c\u4e09\u5341\u4e8c\u4f1a\u8bdd/G2_\u6587\u6848\u4e0e\u5b88\u536b.md` |")
    io.open(P, "wb").write("\n".join(lines).encode("utf-8"))
    print("已标结")
b = io.open(P, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
