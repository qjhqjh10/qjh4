# -*- coding: utf-8 -*-
"""②-7：把 `§29·d` 那条 `A1033` 行搬出（它自陈已闭、且是 2 列表里的 3 格行）。"""
import io

P = r"d:/4/项目任务.md"
C = r"d:/4/Unity/资料/历史/A表已收口_第三十二会话.md"

s = io.open(P, encoding="utf-8", newline="").read()
lines = s.split("\n")
hit = [i for i, L in enumerate(lines) if L.startswith("| ✅ **`A1033`** |")]
assert len(hit) == 1, hit
i = hit[0]
L = lines[i]
print("行 %d · 竖线 %d" % (i + 1, L.count("|")))
print(repr(L[:200]))
assert L.count("|") == 4, L.count("|")
# 该行是「一行多格」的撑坏行；把它整行搬走
row = L.strip()
del lines[i]
# 顺带看看它下面是不是还有续行（同一条账的正文）
nxt = lines[i] if i < len(lines) else ""
print("下一行起头:", repr(nxt[:120]))

io.open(P, "wb").write("\n".join(lines).encode("utf-8"))

db = io.open(C, encoding="utf-8", newline="").read()
db += ("\n### 三、`§29·d` 里「已闭该搬出 + 表格被撑坏」的行\n\n"
       "> 与 `A1070③` 同族（那是 `R1` 的 B2-18 修掉的）。这一条 `A1033` 自陈「🔴 已闭 ⇒ 该从 §29·d 搬出」，\n"
       "> 而它在 2 列表里塞了 **3 格**（`R1` / `AUD` 两次都抓到）。整行原样留在这里。\n\n"
       "> " + row + "\n")
io.open(C, "wb").write(db.encode("utf-8"))

b = io.open(P, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
