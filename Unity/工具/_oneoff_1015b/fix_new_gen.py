# -*- coding: utf-8 -*-
"""一次性：把 `gen_cards_engine.load_official_desc()` 改成【`_new` 新代赢】。
并且就地订正那段「181 条被截断、值是单个字符」的**读错**的注释（真因见 `_loc_official/w4_lib.py:30`）。"""
import io

P = r"d:/4/Unity/工具/gen_cards_engine.py"
b = io.open(P, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))

d = b.find(b"def load_official_desc():")
assert d > 0, "找不到 load_official_desc 定义"
start = b.find(b"    terms = json.load(open(OFFICIAL_LOC_SRC", d)
end = b.find(b"    return by_id, bad, stats\r\n", start)
assert start > d and end > start, (d, start, end)
assert end - start < 3000, ("片段过大，锚点很可能跨了函数", end - start)
end += len(b"    return by_id, bad, stats\r\n")
old = b[start:end]
print("旧片段 %d 字节" % len(old))

NEW = "\r\n".join([
"    terms = json.load(open(OFFICIAL_LOC_SRC, encoding=\"utf-8\")).get(\"terms\") or {}",
"    # \U0001f534 **2026-10-15\uff08\u7b2c\u4e09\u5341\u4e8c\u4f1a\u8bdd\uff09\u5c31\u5730\u4fee\uff1a\u540c\u4e00\u5f20\u5361\u6709\u300c\u4e24\u4ee3\u300d\u952e\uff0c\u300c`_new` \u90a3\u4ee3\u5fc5\u987b\u8d62\u300d\u3002**",
"    #   \u5b98\u65b9\u8868\u91cc\u5bf9\u540c\u4e00\u5f20\u5361\u53ef\u4ee5\u6709 `Card_Description/<id>` \u4e0e `Card_Description/<id>_new` **\u4e24\u6761**",
"    #   \uff08\u73b0\u8bfb 16 \u5bf9\uff09\u3002\u5b9e\u6d4b **`_new` \u90a3\u4ee3\u624d\u4e0e PnP \u6210\u54c1\u5361\u56fe\u5370\u7684\u4e00\u81f4** \u2014\u2014 \u9010\u5f20\u5f00\u56fe\u6838\u8fc7\uff1a",
"    #     \u00b7 `TL66 Neurogaunt`  \u5361\u9762\u5370 `Tide 2`\uff08\u65b0\u4ee3\uff09\uff0c\u800c\u65e7\u4ee3\u662f `Tide 4`",
"    #       \uff08`d:/2/Warpforge\u90e8\u961f\u5361\u7247/Tyranid/3\u90e8\u961f/Warpforge_4_Neurogaunt.png`\uff09",
"    #     \u00b7 `TL44 Hardened Biology` \u5361\u9762\u5370 `+1 Health`\uff08\u65b0\u4ee3\uff09\uff0c\u800c\u65e7\u4ee3\u662f `+2`",
"    #       \uff08`Tyranid/4\u8ba1\u7b56/Warpforge_55_Hardened-Biology.png`\uff09",
"    #   PnP = \u5185\u5bb9\u7684\u5c3a\u5b50\uff08\u94c1\u5f8b 7 / \u94c1\u5f8b 10 \u7b2c 3 \u6761\uff09\u21d2 **`_new` \u8d62**\u3002",
"    #   \u26a0\ufe0f \u6539\u524d\u90a3 16 \u6761 `_new` \u88ab\u5f53\u6210**\u72ec\u7acb\u7684\u952e**\uff08`SOR30_new`\uff09\u5b58\u8fdb `by_id`\uff0c",
"    #   \u800c\u5361 id \u662f `SOR30` \u21d2 **\u4e00\u6761\u90fd\u67e5\u4e0d\u5230** \u21d2 \u90a3 16 \u5f20\u5168\u90e8\u9000\u56de\u65e7\u4ee3\u3002",
"    #   \u5b9e\u6d4b\u4ee3\u4ef7\uff1a`TL66` / `TL44` / `UM68` / `UM72` \u56db\u5f20\u7684**\u6570\u503c\u662f\u9519\u7684**\uff08\u89c1\u4e0a\uff09\u3002",
"    #   \u2139\ufe0f \u4e0d\u5728\u4eba\u5de5\u5c42\uff08`cardface_fixes.descZh`\uff09\u91cc\u7684\u90a3\u51e0\u5f20\u624d\u4f1a\u88ab\u672c\u4fee\u6539\u5230 \u2014\u2014",
"    #   \u4eba\u5de5\u5c42\u4ecd\u7136**\u6700\u540e\u751f\u6548**\uff08\u5c42\u5e8f\u4e0d\u53d8\uff09\uff0c\u4f8b `Celestian Sacresant` \u4eba\u5de5\u503c\u672c\u6765\u5c31\u662f `_new` \u8bed\u4e49\u3002",
"    #",
"    # \u26a0\ufe0f **\u540c\u65e5\u8ba2\u6b63\uff08\u94c1\u5f8b 5\uff09**\uff1a\u4e0b\u9762\u539f\u6765\u5199\u7740\u300c\u5b98\u65b9\u8868\u91cc\u6709\u4e00\u6279**\u88ab\u622a\u65ad\u7684**\u6761\u76ee \u2014\u2014 \u73b0\u8bfb\u5168\u8868 1098 \u6761\u91cc",
"    #   **181** \u6761\u7684\u503c**\u5c31\u662f\u5355\u4e2a\u5b57\u7b26**\uff1a`(` 91 \u6761 \u00b7 `[` 89 \u6761 \u00b7 `N`/`o` 1 \u6761\u300d**\u2014\u2014 \u90a3\u662f\u8bfb\u9519\u4e86**\uff1a",
"    #   \u771f\u56e0 = \u539f\u8868\u91cc **en == zh** \u7684\u6761\u76ee\u538b\u6210\u4e86**\u5355\u4e2a\u5b57\u7b26\u4e32**\uff08\u51fa\u5904 = `\u5de5\u5177/_loc_official/w4_lib.py:30`\uff09\uff0c",
"    #   \u800c\u65e7\u4ee3\u7801\u5bf9\u5b57\u7b26\u4e32\u53d6 `v[1]` = **\u7b2c\u4e8c\u4e2a\u5b57\u7b26** \u21d2 `'[[flank]]'` \u53d6\u51fa `'['`\u3001`'((\u2026))'` \u53d6\u51fa `'('`",
"    #   \u21d2 \u90a3 181 \u6761**\u597d\u503c**\u88ab\u8bef\u5224\u6210\u300c\u88ab\u622a\u65ad\u300d\u3002**\u884c\u4e3a\u4e0d\u53d8**\uff08\u5355\u503c = \u6ca1\u6709\u4e2d\u6587\u53ef\u6362\uff0c",
"    #   \u4ecd\u4fdd\u6301\u6211\u4eec\u81ea\u8bd1\uff09\uff0c\u4f46**\u62a5\u544a\u6587\u6848\u4e0e\u63a8\u7406\u5f97\u6539\u5bf9**\u3002\uff08\u5f53\u65e5\u53e6\u6709\u8be5\u65e5\u5fd7\u843d\u76d8\u3002\uff09",
"    by_id, bad, superseded = {}, [], []",
"    for k, v in terms.items():",
"        if not k.startswith(\"Card_Description/\"):",
"            continue",
"        _id = k[len(\"Card_Description/\"):]",
"        _isnew = _id.endswith(\"_new\")",
"        if _isnew:",
"            _id = _id[:-4]",
"        if isinstance(v, str):",
"            # en == zh \u7684\u5355\u503c\uff08\u73b0\u8bfb 181 \u6761\uff09\u21d2 **\u6ca1\u6709\u4e2d\u6587\u53ef\u6362**\uff0c\u4fdd\u6301\u6211\u4eec\u81ea\u8bd1",
"            bad.append((_id, v, \"\u5355\u503c(en==zh)\"))",
"            continue",
"        en = (v[0] if len(v) > 0 else \"\") or \"\"",
"        zh = ((v[1] if len(v) > 1 else \"\") or \"\").strip()",
"        if len(zh) < 3:",
"            bad.append((_id, en, zh))",
"            continue",
"        if _isnew:",
"            superseded.append((_id, by_id.get(_id), zh))",
"            by_id[_id] = zh            # \U0001f534 `_new` **\u8986\u76d6**\u65e7\u4ee3",
"        elif _id not in by_id:          # \u4e0d\u8ba9\u65e7\u4ee3\u76d6\u6389\u5df2\u5199\u5165\u7684\u65b0\u4ee3\uff08`terms` \u7684\u5e8f\u4e0d\u4fdd\u8bc1\uff09",
"            by_id[_id] = zh",
"    stats = {\"terms\": sum(1 for k in terms if k.startswith(\"Card_Description/\")),",
"             \"ok\": len(by_id), \"bad\": len(bad), \"superseded\": len(superseded),",
"             \"guid\": sum(1 for k in by_id if re.fullmatch(r\"[0-9a-f]{32}\", k))}",
"    print(f\"\u5b98\u65b9\u4e2d\u6587\u6548\u679c `Card_Description/*` {stats['terms']} \u6761\uff08\u53ef\u7528 {stats['ok']} \u00b7 GUID \u952e {stats['guid']} \u00b7\"",
"          f\" \u503c\u4e0d\u6210\u7acb\u4e22\u5f03 {stats['bad']} \u00b7 `_new` \u65b0\u4ee3\u751f\u6548 {stats['superseded']}\uff09\")",
"    if superseded:",
"        print(f\"            \u2705 `_new` \u65b0\u4ee3**\u8986\u76d6\u65e7\u4ee3** {len(superseded)} \u5f20\uff1a\"",
"              + \" \u00b7 \".join(_i for _i, _o, _n in superseded))",
"    if bad:",
"        _bc = {}",
"        for _t in bad:",
"            _zh = _t[2]",
"            _k = _zh if _zh in (\"(\", \"[\", \"\u5355\u503c(en==zh)\") else \"\u5176\u5b83\"",
"            _bc[_k] = _bc.get(_k, 0) + 1",
"        print(f\"            \u26a0\ufe0f \u6574\u6761\u4e22\u5f03 {len(bad)} \u6761\uff08\"",
"              + \" \u00b7 \".join(f\"{k!r}\u00d7{n}\" for k, n in sorted(_bc.items())) + \"\uff09\"",
"              \" \u2014\u2014 \u5355\u503c\u90a3\u4e00\u6863\u662f**\u539f\u8868\u91cc en == zh**\uff08\u6ca1\u6709\u4e2d\u6587\u53ef\u6362\uff09\uff0c\u8fd9\u4e9b\u5361\u4fdd\u6301\u6211\u4eec\u81ea\u8bd1\")",
"        for _t in bad[:6]:",
"            print(f\"            \u00b7 `Card_Description/{_t[0]}` = ({_t[1]!r}, {_t[2]!r})\")",
"        if len(bad) > 6:",
"            print(f\"            \u2026 \u5176\u4f59 {len(bad) - 6} \u6761\u540c\u7406\")",
"    return by_id, bad, stats",
]).encode("utf-8")

out = b[:start] + NEW + b[end:]
io.open(P, "wb").write(out)
nb = io.open(P, "rb").read()
print("CRLF", nb.count(b"\r\n"), "LF", nb.count(b"\n"), "bytes", len(nb))
