# -*- coding: utf-8 -*-
"""AUD ①-5 / ①-6 —— `任务_卡组卡牌战斗.md`：
①-5：本会话那批 `A20xx` 里**行内已自陈闭、行首却没标记**的 11 行补 `✅`；
①-6：4 行（`A1752`/`A1755`/`A1757`/`A1758`）行首标签写「第三十一会话已落地」而行内自陈是**第二十六会话**。"""
import io

P = r"d:/4/Unity/资料/任务_卡组卡牌战斗.md"
s = io.open(P, encoding="utf-8", newline="").read()
lines = s.split("\n")

CLOSED = ["A2018", "A2020", "A2021", "A2022", "A2023", "A2024",
          "A2031", "A2035", "A2043", "A2051", "A2061"]
changed = 0
for num in CLOSED:
    hit = [i for i, L in enumerate(lines) if L.startswith("| **`%s`** |" % num)]
    if len(hit) != 1:
        print("  !! %s 命中 %d" % (num, len(hit)))
        continue
    i = hit[0]
    lines[i] = "| \u2705 **`%s`**  \u2705 2026-10-15 \u7b2c\u4e09\u5341\u4e8c\u4f1a\u8bdd\u5df2\u843d\u5730 |" % num + lines[i][len("| **`%s`** |" % num):]
    changed += 1
print("补 ✅ %d 行" % changed)

# ①-6：4 行的会话标签
for num in ("A1752", "A1755", "A1757", "A1758"):
    hit = [i for i, L in enumerate(lines) if L.startswith("| ✅ **`%s`**" % num)]
    assert len(hit) == 1, (num, hit)
    i = hit[0]
    old = lines[i]
    new = old.replace("2026-10-15 \u7b2c\u4e09\u5341\u4e00\u4f1a\u8bdd\u5df2\u843d\u5730",
                      "2026-10-13 \u7b2c\u4e8c\u5341\u516d\u4f1a\u8bdd\u5df2\u843d\u5730\uff08\u2757 \u6807\u7b7e\u539f\u5199\u300c\u7b2c\u4e09\u5341\u4e00\u4f1a\u8bdd\u300d\uff0c2026-10-15 \u5c31\u5730\u8ba2\u6b63\uff1a\u884c\u5185\u81ea\u9648\u662f 10-13\uff09")
    if new != old:
        lines[i] = new
        print("  改标签", num)
    else:
        print("  !! %s 标签没匹配上" % num)

io.open(P, "wb").write("\n".join(lines).encode("utf-8"))
b = io.open(P, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
