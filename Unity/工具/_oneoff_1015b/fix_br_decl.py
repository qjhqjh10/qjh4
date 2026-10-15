# -*- coding: utf-8 -*-
"""一次性：给 `CardText.IsDeclarationAt` 的「段首」那一支补上 `<br>` 收尾。
用下标手术、不写字面量大串（避免空白数错）。"""
import io

P = r"d:/4/Unity/MyGame/Assets/CardPresentation/Core/CardText.cs"
b = io.open(P, "rb").read()

start_mark = b"            return hay[k] == '.'"
i = b.find(start_mark)
assert i > 0, "找不到起点"
j = b.find(b";", b.find(b"hay[k] == '\\r'", i))
assert j > 0 and j - i < 400, (i, j)
end = j + 3  # 含 ';\r\n'
old = b[i:end]
print("旧片段 %d 字节:" % len(old))
print(repr(old))

NEW = "\r\n".join([
    "            if (hay[k] == '.' || hay[k] == '\u3002' || hay[k] == '\uff0e' || hay[k] == ';' ||",
    "                hay[k] == '\uff1b' || hay[k] == ',' || hay[k] == '\uff0c' || hay[k] == '\\n' || hay[k] == '\\r')",
    "                return true;",
    "            // \U0001f534 **2026-10-15\uff08\u7b2c\u4e09\u5341\u4e8c\u4f1a\u8bdd\uff09\u8865\uff1a`<br>` \u6536\u5c3e\u4e5f\u7b97\u6bb5\u9996\u3002**",
    "            //   \u4e3a\u4ec0\u4e48\u8981\u6709\u5b83\uff1a\u539f\u7248\u90a3\u4ee3\u6587\u672c\uff08\u5b98\u65b9 `Card_Description/*` \u6a21\u677f\uff09\u7528 `<br>` \u65ad\u884c\uff0c",
    "            //   \u4e8e\u662f\u300c`\u5929\u8d4b\uff1a\u51ef\u6069\u4e4b\u6012\u3002`\u300d\u8fd9\u79cd**\u58f0\u660e**\u524d\u9762\u90a3\u4e2a\u5b57\u7b26\u662f `>`\u3001\u4e0d\u662f\u53e5\u8bfb\u7b26",
    "            //   \u21d2 \u4e0a\u9762\u90a3\u4e00\u884c\u5224\u4e0d\u51fa\u6765 \u21d2 \u90a3\u4e2a\u8bcd\u88ab\u5f53\u6210\u300c\u6b63\u6587\u91cc\u6ca1\u58f0\u660e\u8fc7\u300d\u3001",
    "            //   \u7531 `KeywordSegment` **\u518d\u8865\u4e00\u904d** \u21d2 \u5361\u9762**\u5370\u4e24\u904d**\u3002",
    "            //   \u5b9e\u6d4b `ASH34 Avatar of Khaine`\uff08`keywords:[\"Armour 2\",\"Talent\"]`\u3001\u6b63\u6587",
    "            //   `\u62a4\u7532 2\u3002<br>\u2461\uff1a\u83b7\u5f97\u7206\u88c2 6 \u548c\u4f2a\u88c5\u3002<br>\u5929\u8d4b\uff1a\u51ef\u6069\u4e4b\u6012\u3002`\uff09\uff1a\u6211\u4eec\u5370",
    "            //   `\u3014\u5929\u8d4b\u3015\u5929\u8d4b\u3002 \u3014\u62a4\u7532\u3015\u62a4\u7532 2\u3002\u2026\u3014\u5929\u8d4b\u3015\u5929\u8d4b\uff1a\u51ef\u6069\u4e4b\u6012\u3002`\uff08`\u5929\u8d4b` \u4e24\u6b21\uff09\uff0c",
    "            //   \u800c PnP \u6210\u54c1\u5361 `Aeldari/3\u90e8\u961f/Warpforge_42_Avatar-of-Khaine.png` \u53ea\u5370\u4e00\u904d\u3002",
    "            //   PnP \u662f**\u5185\u5bb9**\u7684\u5c3a\u5b50\uff08\u94c1\u5f8b 7 / \u94c1\u5f8b 10 \u7b2c 3 \u6761\uff09\u21d2 \u4ee5\u5361\u9762\u4e3a\u51c6\u3002",
    "            //   \u26a0\ufe0f \u65b9\u5411\u4e0a\u5b83\u53ea\u4f1a\u8ba9 `DeclaredInBody` **\u66f4\u5bb9\u6613**\u4e3a\u771f \u21d2 \u53ea\u4f1a**\u51cf**\u5173\u952e\u8bcd\u6bb5\u91cc\u90a3\u4e9b\u9879\uff0c\u4e0d\u4f1a\u65b0\u589e\u3002",
    "            return EndsWithLineBreak(hay, k);",
    "        }",
    "",
    "        /// <summary>`hay[..k]`\uff08\u542b\u4e0b\u6807 `k`\uff09\u7684\u7ed3\u5c3e\u662f\u4e0d\u662f\u4e00\u4e2a `<br>` / `<br/>` / `<br />` \u6807\u7b7e\u3002",
    "        /// <para>\u5224\u636e\uff1a\u5f80\u5de6\u627e\u5230\u6700\u8fd1\u7684 `<`\uff0c\u4e2d\u95f4\u90a3\u622a\u53ea\u5141\u8bb8\u662f `br` \u53ca\u5176\u7a7a\u767d\u4e0e\u659c\u6760\u3002</para></summary>",
    "        static bool EndsWithLineBreak(string hay, int k)",
    "        {",
    "            int lt = hay.LastIndexOf('<', k);",
    "            if (lt < 0 || k - lt > 5) return false;      // `<br>`=3 \u00b7 `<br/>`=4 \u00b7 `<br />`=5",
    "            string inner = hay.Substring(lt + 1, k - lt - 1).Replace(\" \", \"\").Replace(\"/\", \"\");",
    "            return string.Equals(inner, \"br\", System.StringComparison.OrdinalIgnoreCase);",
    "        }",
    "",
]).encode("utf-8")

out = b[:i] + NEW + b[end:]
io.open(P, "wb").write(out)
nb = io.open(P, "rb").read()
print("CRLF", nb.count(b"\r\n"), "LF", nb.count(b"\n"), "bytes", len(nb))
