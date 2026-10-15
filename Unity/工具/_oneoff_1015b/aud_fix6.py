# -*- coding: utf-8 -*-
"""AUD ③-1/3/4/5：把漏落盘的 4 条发现补进正本。"""
import io

P = r"d:/4/Unity/资料/任务_卡组卡牌战斗.md"
s = io.open(P, encoding="utf-8", newline="").read()
lines = s.split("\n")

ROWS = [
"| **`A2065`** | ⚠️ **上一会话 `ee784b5` 批量改的 22 行行首标签【不准】**：它把 22 行的行首统一写成「✅ 2026-10-15 **第三十一会话**已落地」，而其中 **`A1088` 是 2026-10-13 第二十四会话**收口的、**`A1752`/`A1755`/`A1757` 是 2026-10-13 第二十六会话**落地的（判据 = 各自行内自陈）。⇒ 本会话已订正那 4 行；**其余 18 行没逐行核**（要不要全改、还是把标签里的会话名去掉，**待定**）。出处 → `资料/普查产出_第三十二会话/R1_B2正本锚点.md:229` · `AUD_收尾一致性.md` ①-6 |",
"| **`A2066`** | ⚠️ **`工具/_loc_official/w4_verify.py` 已过期**（2026-10-14 的一次性脚本、**硬编码期望值**）：顺手只读跑一次 = **通过 35 · 失败 1**，那条红是「① `Loc.cs` 表」—— `new Entry(` 现读 **732** 而脚本期望 **720**；同脚本 ⑥ 还印着 `Avatars 234/469` 而产物今天 = **419**。⇒ 要么标成「期望值已过期」、要么改成现算。⛔ 本会话没动（不在白名单）。出处 → `资料/普查产出_第三十二会话/R7_official_names文案.md:133` |",
"| **`A2067`** | ⚠️ **生成器那行「数值修正 N 张」把两条【改名】记成了「数值修正」**：`Imotekh the Stormlord` / `Orikan the Diviner` 只带 `\"name\"`、`cost/attack/health/ranged` **一个都没改**，却进了「29 张」的读数。⇒ 纯**文案/读数**问题（要不要拆成「改名 N / 数值 N」待定）。出处 → `资料/普查产出_第三十二会话/R6_STAT_FIXES守卫.md:154` · `AUD_收尾一致性.md` ③-3 |",
]

hit = [i for i, L in enumerate(lines) if L.startswith("| ✅ **`A2061`**") or L.startswith("| **`A2061`**")]
assert len(hit) == 1, hit
for r in ROWS:
    assert r.count("|") == 3, r.count("|")
lines[hit[0] + 1:hit[0] + 1] = ROWS

# ③-5：并进 `A1938`
hit2 = [i for i, L in enumerate(lines) if L.startswith("| ") and "`A1938`" in L[:40]]
print("A1938 命中:", [h + 1 for h in hit2])
if len(hit2) == 1:
    i = hit2[0]
    L = lines[i].rstrip()
    assert L.endswith("|"), L[-30:]
    lines[i] = L[:-1].rstrip() + (
        " \u2795 **2026-10-15 \u5c31\u5730\u8865\u8bb0\uff08\u94c1\u5f8b 5\uff09**\uff1a\u540c\u65cf**\u8fd8\u6709\u4e00\u5f20 `Sporecaster Biostructure`**"
        "\uff08`R8` \u73b0\u6838\uff09\uff1b\u53e6 **`card_stats.json` \u73b0\u8bfb 1213 \u6761 \u21d4 \u6211\u4eec\u5361\u6c60 1127 \u5f20**"
        "\uff08\u5dee\u51fa\u6765\u7684\u591a\u662f `environment` / \u5360\u4f4d\u884c\uff09\u3002\u51fa\u5904 \u2192 `R8_card_stats\u5ba1\u8ba1.md:112/115` |")
    print("  已并进 A1938")

io.open(P, "wb").write("\n".join(lines).encode("utf-8"))
b = io.open(P, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
