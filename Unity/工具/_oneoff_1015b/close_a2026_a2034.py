# -*- coding: utf-8 -*-
"""把 `A2026` / `A2034` 标结（用户 2026-10-15 拍板后执行完毕）。"""
import io

PROJ = r"d:/4/项目任务.md"
TASK = r"d:/4/Unity/资料/任务_卡组卡牌战斗.md"

# A2026 在 §29·c
s = io.open(PROJ, encoding="utf-8", newline="").read()
lines = s.split("\n")
hit = [i for i, L in enumerate(lines) if L.startswith("| **`A2026`** |")]
assert len(hit) == 1, hit
i = hit[0]
L = lines[i].rstrip()
assert L.endswith("|"), L[-30:]
lines[i] = L[:-1].rstrip() + (
    " ➕ **2026-10-15 用户拍板（路 A）：⛔ 不改那两行 + 补一句注明** —— "
    "⚠️ **现核：那句注明【已经有了】**（`card_ids.json` 的 `note` 里 `AN` 上一会话写的「**这两行【不改】** —— "
    "`GSC64` = `Rusted Vents` · `GSC49` = `Telephatic Domin…」）⇒ **本条已满足、无需再动**。"
    "判据 → `资料/普查产出_第三十二会话/R1_B2正本锚点.md` B2-26 |")
io.open(PROJ, "wb").write("\n".join(lines).encode("utf-8"))

# A2034 在 任务 §一（引擎线，因为它是生成器文案）—— 先找它在哪
s2 = io.open(TASK, encoding="utf-8", newline="").read()
lines2 = s2.split("\n")
hit2 = [i for i, L in enumerate(lines2) if L.startswith("| **`A2034`** |")]
print("A2034 在 任务 的命中数 =", len(hit2))
if len(hit2) == 1:
    j = hit2[0]
    L2 = lines2[j].rstrip()
    assert L2.endswith("|"), L2[-30:]
    lines2[j] = L2[:-1].rstrip() + (
        " ➕ **2026-10-15 用户拍板：改** —— 已落（`gen_cards_engine.py:2343`）：现读 "
        "`卡面核对修正 字段 {n_sub} 处（= 第③层盖掉②OCR 的处数） …`，并写清「关键词」那一列的口径"
        "（它 = 第③层与**①真值铺底之后的②OCR** 不一致的卡数 ⇒ **≠「转录改了几处」**）。⛔ **表达式一个字没动、只改字**。"
        "判据 → `资料/普查产出_第三十二会话/G2_文案与守卫.md` |")
    io.open(TASK, "wb").write("\n".join(lines2).encode("utf-8"))

for p in (PROJ, TASK):
    b = io.open(p, "rb").read()
    print(p.split("/")[-1], "CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
