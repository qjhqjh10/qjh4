# -*- coding: utf-8 -*-
"""一次性：给 `CardBaseDemo` 的卡面那一节补三条断言（`<br>` 后紧跟声明 ⇒ 关键词段不再补印）。"""
import io

P = r"d:/4/Unity/MyGame/Assets/CardPresentation/Editor/CardBaseDemo.cs"
b = io.open(P, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))

anchor = (
    '              "\u2466 \u540c\u4e0a\uff0c`blast` \u7684\u522b\u540d `\u7206\u7834`'
    '\uff085 \u5f20\u5361 `AM13/AM28/AM34/AM37/AM54` \u7684\u7ffb\u8f66\u70b9\uff09");\r\n'
).encode("utf-8")
assert b.count(anchor) == 1, b.count(anchor)

lines = [
'',
'        // ---- \u2468 \U0001f195 2026-10-15\uff08\u7b2c\u4e09\u5341\u4e8c\u4f1a\u8bdd\uff09\uff1a\u58f0\u660e\u884c\u7d27\u8ddf `<br>` \u4e5f\u8981\u5224\u6210\u300c\u5df2\u58f0\u660e\u300d ----',
'        //   \u6765\u7531\uff1a\u5b98\u65b9\u90a3\u4ee3\u6587\u672c\u7528 `<br>` \u65ad\u884c\uff0c\u800c `IsDeclarationAt` \u7684\u300c\u6bb5\u9996\u300d\u90a3\u4e00\u652f',
'        //   \u53ea\u770b**\u7d27\u90bb\u5b57\u7b26**\u662f\u4e0d\u662f\u53e5\u8bfb\u7b26 \u21d2 `<br>` \u7684 `>` \u5224\u4e0d\u51fa\u6765 \u21d2 \u8bcd\u88ab\u518d\u8865\u4e00\u904d\u3002',
'        //   \u5b9e\u6d4b\uff1a`ASH34 Avatar of Khaine` \u5361\u9762\u591a\u5370\u4e00\u4e2a `\u5929\u8d4b\u3002`\uff08PnP \u6210\u54c1\u5361\u53ea\u5370\u4e00\u904d\uff09\u3002',
'        var kwTalent = new System.Collections.Generic.Dictionary<string, int> { { "talent", 0 } };',
'        Check(CardText.KeywordSegment(kwTalent, true, "\u62a4\u7532 2\u3002<br>\u5929\u8d4b\uff1a\u51ef\u6069\u4e4b\u6012\u3002") == "",',
'              "\u2468 \u6b63\u6587\u91cc `\u5929\u8d4b\uff1a\u51ef\u6069\u4e4b\u6012\u3002` \u7d27\u8ddf `<br>` \u21d2 \u5173\u952e\u8bcd\u6bb5**\u4e0d\u518d\u8865** `\u5929\u8d4b\u3002`"',
'            + " \u2014\u2014 \U0001f9e8 \u628a `IsDeclarationAt` \u7684\u300c\u6bb5\u9996\u300d\u90a3\u652f\u6539\u56de**\u53ea\u770b\u53e5\u8bfb\u7b26** \u21d2 \u672c\u6761\u7ea2"',
'            + "\uff08\u5b9e\u6d4b `ASH34 Avatar of Khaine` \u5361\u9762\u4f1a\u591a\u5370\u4e00\u4e2a `\u5929\u8d4b\u3002`\uff0c\u800c PnP \u53ea\u5370\u4e00\u904d\uff09");',
'        Check(CardText.KeywordSegment(kwTalent, true, "\u62a4\u7532 2\u3002\\n\u5929\u8d4b\uff1a\u51ef\u6069\u4e4b\u6012\u3002") == "",',
'              "\u2468 **\u5224\u522b\u5f0f**\uff1a`<br>` \u4e0e\u771f\u6362\u884c `\\\\n` \u5fc5\u987b**\u540c\u6548**"',
'            + " \u2014\u2014 \u53ea\u7ed9 `<br>` \u5f00\u5c0f\u7076\u3001\u6216\u53ea\u8ba4 `\\\\n`\uff0c\u4e24\u6761\u5fc5\u7ea2\u5176\u4e00");',
'        Check(CardText.KeywordSegment(kwTalent, true, "\u62a4\u7532 2\u3002\u51ef\u6069\u4e4b\u6012\u3002") != "",',
'              "\u2468 **\u5224\u522b\u5f0f\uff08\u53cd\u5411\uff09**\uff1a\u6b63\u6587\u91cc**\u6ca1\u6709**\u8fd9\u4e2a\u8bcd\u7684\u58f0\u660e"',
'            + "\uff08`\u51ef\u6069\u4e4b\u6012` \u662f\u6548\u679c\u7684\u5bbe\u8bed\u3001\u4e0d\u662f\u58f0\u660e\uff09\u21d2 \u8be5\u8865\u7684\u8fd8\u5f97\u8865"',
'            + " \u2014\u2014 \U0001f9e8 \u628a `DeclaredInBody` \u6539\u6210\u6052\u771f\uff08\u4f8b\u5982\u300c\u51fa\u73b0\u8fc7\u5c31\u7b97\u300d\uff09\u21d2 \u672c\u6761\u7ea2");',
]
block = ("\r\n".join(lines) + "\r\n").encode("utf-8")
out = b.replace(anchor, anchor + block, 1)
io.open(P, "wb").write(out)
nb = io.open(P, "rb").read()
print("CRLF", nb.count(b"\r\n"), "LF", nb.count(b"\n"), "bytes", len(nb))
