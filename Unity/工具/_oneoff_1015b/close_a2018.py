# -*- coding: utf-8 -*-
"""把 `A2018`（descZh 换代残余候选）按 `R9` 的裁定收口。"""
import io

P = r"d:/4/Unity/资料/任务_卡组卡牌战斗.md"
s = io.open(P, encoding="utf-8", newline="").read()
lines = s.split("\n")
hit = [i for i, L in enumerate(lines) if L.startswith("| **`A2018`** |")]
assert len(hit) == 1, hit
i = hit[0]
add = (" \u2795 **2026-10-15 \u6536\u53e3\uff08`R9`\uff09**\uff1a\u90a3 48 \u6761\u5019\u9009**\u5df2\u5168\u90e8\u9010\u5f20\u5f00 PnP \u5361\u56fe\u88c1\u5b9a\u5b8c**"
       "\uff08\u5019\u9009\u5b9e\u6570 **52**\uff1a\u5176\u4e2d **24 \u5df2\u4fee**\uff08`AD` 4 + `AM` 19 + `SOR24` 1\uff09\u00b7 "
       "**24 \u6211\u4eec\u672c\u6765\u5c31\u5bf9** \u00b7 **4 \u6761\u771f\u7f3a\u9677\u672c\u4f1a\u8bdd\u5df2\u4fee**\uff1a"
       "`UM44` \u6218\u6597\u2192\u6bd4\u8d5b \u00b7 `Legendary Commandant` \u5220\u300c\u975e\u4f20\u5947\u300d \u00b7 `TL69` \u5220\u300c1\u8d39\u7684\u300d \u00b7 `UM73` \u5355\u4f4d\u2192\u90e8\u961f\uff09"
       "\u21d2 **\u672c\u884c\u53ea\u5269\u300c\u6b8b\u4f59\u5019\u9009\u300d\u8fd9\u4e2a\u58f3\uff0c\u53ef\u9500**\u3002"
       "\u26a0\ufe0f `R9` \u8fd8\u627e\u5230 `AD` \u6e05\u5355\u5c11\u5217\u7684\u90a3\u4e00\u6761 = `UM44 Armoured Support`\u3002"
       "\u5224\u636e \u2192 `\u8d44\u6599/\u666e\u67e5\u4ea7\u51fa_\u7b2c\u4e09\u5341\u4e8c\u4f1a\u8bdd/R9_descZh\u5019\u9009\u88c1\u5b9a.md` |")
L = lines[i].rstrip()
assert L.endswith("|"), L[-40:]
lines[i] = L[:-1].rstrip() + add
io.open(P, "wb").write("\n".join(lines).encode("utf-8"))
b = io.open(P, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
print("该行竖线数 =", lines[i].replace("\\|", "").count("|"))
