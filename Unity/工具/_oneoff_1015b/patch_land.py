# -*- coding: utf-8 -*-
"""重写 land_1032.py 里 B1-24 / B1-43 / B1-7+44 三处（改成「行尾追加」，且锚点用真实卡池里的行首标记）。"""
import io

P = r"d:/4/Unity/工具/_oneoff_1015b/land_1032.py"
s = io.open(P, encoding="utf-8").read()

start = s.index("    # B1-24\uff1a\u5e76\u5165 `A1770` \u884c\u5c3e")
end = s.index("    # B1-2 \u5df2\u5e76\u5165\u4e0a\u9762\u90a3\u6bb5")
print("替换区间 %d 字节" % (end - start))

NEW = "\r\n".join([])  # 占位（下面用 \n，因为脚本本身是 LF）
NEW = "\n".join([
"    # B1-24\uff1a\u5e76\u5165 `A1770` \u884c\u5c3e",
"    T = append_to_row(T, \"**`A1770`**\",",
"             \" \\u2795 **2026-10-15 \u6536\u53e3\uff08\u672c\u6761\u53ef\u95ed\uff09**\uff1a\u771f\u503c 2 \u5f20 `cardImageReference` \u6307\u5230\u5361\u6846\u7684\"",
"             \"\uff08`EC62 Aural Hijack` / `4eec3b13\u2026Skorflense`\uff09**\u4e24\u5f20\u90fd\u4e0d\u7528\u6539** \u2014\u2014 \u524d\u8005**\u4e0d\u5728\u6211\u4eec\u5361\u6c60**\uff08\u65e0\u5f71\u54cd\uff09\u00b7 \"",
"             \"\u540e\u8005 PnP \u5df2\u6838\u300c\u4fdd\u6301\u6211\u4eec\u7684\u300d\uff1b\u26a0\ufe0f `EC62` \u90a3\u4e00\u884c\u771f\u503c**\u81ea\u5df1\u81ea\u76f8\u77db\u76fe**\"",
"             \"\uff08`cardName='Aural Hijack'` \u2194 `m_Name='Z-EmperorsChildren-Cacophonic Surge'`\uff09\u3002\u5224\u636e \u2192 `AA_A1770.md` \u00a74/\u00a7\u4e03\u00b72\",",
"             \"B1-24\")",
"",
"    # B1-43\uff1a\u5e76\u5165 `A1945` \u884c\u5c3e",
"    T = append_to_row(T, \"**`A1945`**\",",
"             \" \\U0001f534 **2026-10-15 `Z` \u8865\u7684\u300c\u53ef\u5220\u524d\u7f6e\u6761\u4ef6\u300d\uff08\u4e24\u6761\uff0c\u4eca\u5929\u4e00\u6761\u90fd\u4e0d\u6ee1\u8db3\uff09**\uff1a\"",
"             \"\u2460 **`desc` \u4e5f\u6362\u6210\u5b98\u65b9 `Card_Description` \u7684 EN \u5217**\uff08\u90a3\u662f `A1995`/`A1763` \u7684\u53e6\u4e00\u534a\uff0c**\u53e3\u5f84\u660e\u786e\u89c4\u5b9a\u4e0d\u52a8**\uff09\"",
"             \"\u2461 **`gen_icon_plan.py` \u6574\u65cf\u6539\u9020\u5b8c**\uff08\u5b83\u73b0\u5728\u4e00\u8dd1\u5c31 `rc=1`\u3001\u4e0d\u843d\u76d8\uff09\u3002\u26a0\ufe0f \u73b0\u8bfb `card_icon_plan` \u7684 **`desc` \u534a\u8fb9\u4ecd\u5728\u7528**\u3001\"",
"             \"`descZh` \u534a\u8fb9\u4e5f\u5728\u7528 \u21d2 **\u5220\u6389\u65b9\u62ec\u53f7\u90a3\u4e00\u652f = \u82f1\u6587\u5361\u9762\u6574\u7247\u6389\u5149\u56fe\u6807**\u3002\"",
"             \"\u5224\u636e \u2192 `Z_\u5361\u724cP3.md` \u00a7\u56db \u00b7 \u00a7\u516d\u00b73\",",
"             \"B1-43\")",
"",
"    # B1-7 + B1-44\uff1a\u5e76\u5165 `A1777` \u884c\uff08\u9879\u76ee\uff09",
"    P = append_to_row(P, \"**`A1777`**\",",
"             \" \\u2795 **2026-10-15 \u4e3b\u5bf9\u8bdd\u5df2\u88c1**\uff1a25 \u5904\u300c\u6ce8\u91ca/\u65ad\u8a00\u6587\u6848\u91cc\u8fd8\u5199\u7740\u65e7 id\u300d**\u8981\u6539**\"",
"             \"\uff08\u4ee3\u7801\u5df2\u662f GUID\uff09\uff1b\u53e6 **2026-10-15\uff08`M` \u62a5 / `V` \u505a\uff09**\uff1a\u8dd1\u5b8c P1 \u540e **`.cs` \u91cc\u7684\u65e7 id \u5b57\u9762\u91cf 30 \u5904\uff088 \u6587\u4ef6\uff09\u5df2\u5168\u6362**\uff1b\"",
"             \"**\u4e24\u5904\u4f8b\u5916 \u26d4 \u4e0d\u6362** \u2014\u2014 `card_ids.json` \u7684 7 \u884c\u6b7b id\uff08`EC72`\u2013`EC77`/`TL81`\uff0c**\u96f6\u5f15\u7528\u3001\u5df2\u6e05**\uff09\"",
"             \"\u4e0e `warlord_ids.json` \u7684 4 \u4e2a\u503c\uff08`DA_Azrael`/`DA_Belial`/`DA_Asmodai`/`TL_Neurogaunt` \u2014\u2014 \u90a3\u662f**\u81ea\u5efa\u540d\u8868**\u3001**\u4e0d\u662f\u65e7 id**\uff09\",",
"             \"B1-7+44\")",
"",
])

s = s[:start] + NEW + s[end:]
io.open(P, "w", encoding="utf-8", newline="").write(s)
print("OK")
