# -*- coding: utf-8 -*-
"""AUD ②-1/2/3/6/8（交接内的数字打架）+ ③-2（`已知的坑.md` 的计数过期）。"""
import io

H = r"d:/4/Unity/资料/交接_第三十三会话.md"
K = r"d:/4/Unity/资料/已知的坑.md"

s = io.open(H, encoding="utf-8", newline="").read()

FIX = [
    # ②-2：账号范围
    ("**新开账** | **`A2007`\u2013`A2046`**\uff0840 \u4e2a\u53f7\uff0c\u8fde\u7eed\uff09",
     "**\u65b0\u5f00\u8d26** | **`A2007`\u2013`A2064`**\uff08**58 \u4e2a\u53f7**\uff0c\u8fde\u7eed\u3001\u65e0\u8df3\u53f7\uff1b`A2019` \u5df2\u79fb\u8fdb\u6536\u53e3\u4ef6\uff09"),
    ("`A2007`\u2013`A2046`", "`A2007`\u2013`A2064`"),
    # ②-3 / ②-8
    ("\u7528\u6237 2026-10-15 \u5df2\u5168\u90e8\u62cd\u677f\u5e76\u6267\u884c\u5b8c\u6bd5**\uff08\u23d4 \u522b\u518d\u5f53\"\u7b49\u62cd\u677f\"\u8bfb\uff09",
     "\u7528\u6237 2026-10-15 \u5df2\u5168\u90e8\u62cd\u677f\u5e76\u6267\u884c\u5b8c\u6bd5**\uff088 \u6761\uff1a`A2019`/`A2020`/`A2022`/`A2026`/`A2034`/`A2040`/`A2041`/`A2043`\uff09\uff08\u23d4 \u522b\u518d\u5f53\"\u7b49\u62cd\u677f\"\u8bfb\uff09"),
    ("### \u7532\u3001\u2705 **7 \u6761\u62cd\u677f\u9879", "### \u7532\u3001\u2705 **8 \u6761\u62cd\u677f\u9879"),
    ("\u5176\u4f59 7 \u6761\uff08`Shell` / `Rewards` / `Shop` / `Collection` / `Net\u00d72` / `Settings` / `CardBaseDemo`\uff09",
     "\u5176\u4f59 **8** \u6761\uff08`Shell` / `Rewards` / `Shop` / `Collection` / `NetSelf` / `NetBattle` / `Settings` / `CardBaseDemo`\uff09"),
    # ②-1：两轮 → 三轮
    ("**\u81ea\u68c0\uff0812 \u6761\u5168\u5957 \u00b7 \u672c\u4f1a\u8bdd\u8dd1\u4e86\u3010\u4e24\u8f6e\u3011\uff09**",
     "**\u81ea\u68c0\uff0812 \u6761\u5168\u5957 \u00b7 \u672c\u4f1a\u8bdd\u8dd1\u4e86\u3010\u4e09\u8f6e\u3011\uff09**"),
    # ②-6：「净 +0」两把尺子
    ("`RuleEngineTest` \u51c0 **+0**\uff08`BT-2` \u90a3\u6761\u964d\u4e3a\u8bfb\u6570\u3001\u53e6\u8865 2 \u6761\u524d\u7f6e\uff09",
     "`RuleEngineTest` **\u7b2c\u4e00\u8f6e\u5408\u8ba1 5387 \u2192 \u6536\u53e3\u8f6e 5469\uff08+82\uff09**\uff08\u26a0\ufe0f \u65e7\u6587\u5199\u300c\u51c0 +0\u300d\u662f\u62ff\u300c\u7b2c\u4e00\u8f6e\u7684**\u901a\u8fc7\u6570**\u300d\u6bd4\u300c\u6536\u53e3\u8f6e\u7684**\u5408\u8ba1\u6570**\u300d\u2014\u2014 **\u4e24\u628a\u5c3a\u5b50**\uff0c\u5df2\u8ba2\u6b63\uff09"),
]
n = 0
for old, new in FIX:
    c = s.count(old)
    if c == 0:
        print("  !! 没命中:", old[:50])
        continue
    s = s.replace(old, new)
    n += c
    print("  改 %d 处: %s" % (c, old[:40]))
io.open(H, "wb").write(s.encode("utf-8"))
b = io.open(H, "rb").read()
print("交接 CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"), "· 共改", n)

# ③-2：已知的坑.md 的计数
k = io.open(K, encoding="utf-8", newline="").read()
import re
m = re.search(r"\u5171\s*\*{0,2}(\d+)\*{0,2}\s*\u4e2a\s*`##`\s*\u5c0f\u8282", k)
print("已知的坑 计数句:", m.group(0) if m else "没找到")
if m:
    k2 = k.replace(m.group(0), m.group(0) + "\uff08\u26a0\ufe0f **2026-10-15 \u5c31\u5730\u8ba2\u6b63\uff08\u94c1\u5f8b 5\uff09**\uff1a\u8fd9\u4e2a\u6570**\u5df2\u8fc7\u65f6** \u2014\u2014 \u73b0\u8bfb `grep -c \"^## \"` = **297**\uff1b"
                                "\u5b83\u4e0e\u4e0b\u9762\u90a3\u53e5\u300c\u6309\u6279\u6b21\u8ffd\u52a0\u3001\u26d4 \u522b\u6570\u5b83\u300d**\u81ea\u76f8\u77db\u76fe** \u21d2 **\u4ee5\u73b0\u8bfb\u4e3a\u51c6\u3001\u522b\u5f15\u8fd9\u4e2a\u6570**\uff09", 1)
    io.open(K, "wb").write(k2.encode("utf-8"))
    print("  已订正")
