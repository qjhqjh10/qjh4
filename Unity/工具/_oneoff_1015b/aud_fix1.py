# -*- coding: utf-8 -*-
"""收尾一致性审查（AUD）的 ①-1..4 / ②-4 / ②-7 —— 全在 `项目任务.md`。
①-1..4：`A2026`/`A2034`/`A2040`/`A2041` 四条**已落地**却还躺在 §29·c「等拍板」表里 ⇒ 搬进收口件。
②-4：`:50` 那句「现行交接 = `交接_第三十一会话.md`」已过期两轮。
②-7：`:672`（§29·d 的 `A1033` 行）是 2 列表里的 3 格行（撑坏表格）。"""
import io

P = r"d:/4/项目任务.md"
C = r"d:/4/Unity/资料/历史/A表已收口_第三十二会话.md"

s = io.open(P, encoding="utf-8", newline="").read()
lines = s.split("\n")
moved = []

for num in ("A2026", "A2034", "A2040", "A2041"):
    hit = [i for i, L in enumerate(lines) if L.startswith("| **`%s`** |" % num)]
    assert len(hit) == 1, (num, hit)
    moved.append((num, lines[hit[0]].strip()))
    del lines[hit[0]]

# ②-4：§〇 里那句过期两轮的交接指针（在 §〇 区块，不是 :87 那句）
hit50 = [i for i, L in enumerate(lines) if "🏁 **这是现行交接" in L or ("交接_第三十一会话.md" in L and "现行交接" in L)]
print(":50 类命中", [h + 1 for h in hit50])
for h in hit50:
    print("   ", repr(lines[h][:160]))

s = "\n".join(lines)

# ②-7：`A1033` 行 3 格 → 2 格（表头是 2 列）
lines = s.split("\n")
hit = [i for i, L in enumerate(lines) if L.startswith("| **`A1033`** |") or L.startswith("| `A1033` |")]
if hit:
    i = hit[0]
    L = lines[i]
    print("A1033 行 %d 竖线 %d" % (i + 1, L.count("|")))
    print("   ", repr(L[:200]))
    # 只打印，先不盲改（要看清楚那三格是什么）
else:
    print("A1033 行没找到（可能在别的形状）")

io.open(P, "wb").write("\n".join(lines).encode("utf-8"))

db = io.open(C, encoding="utf-8", newline="").read()
db += "\n### 二、用户 2026-10-15 拍板 + 当场执行完 ⇒ 搬出 §29·c\n\n"
for num, row in moved:
    db += "> 原行（从 `项目任务.md` §29·c 移出）：\n>\n> " + row + "\n>\n"
io.open(C, "wb").write(db.encode("utf-8"))

b = io.open(P, "rb").read()
print("项目任务 CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
print("搬出 %d 行" % len(moved))
