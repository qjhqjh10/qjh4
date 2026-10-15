# -*- coding: utf-8 -*-
"""修 `gen_icon_plan.py` 里被真换行打断的那条注释（用行号手术，别再碰转义）。"""
import io

P = r"d:/4/Unity/工具/gen_icon_plan.py"
s = io.open(P, encoding="utf-8", newline="").read()
lines = s.split("\n")
bad = [i for i, L in enumerate(lines) if L.strip().startswith("天赋：`/`")]
assert len(bad) == 1, bad
i = bad[0]
print("坏行 %d-%d:" % (i, i + 1))
print(repr(lines[i - 1]))
print(repr(lines[i]))
print(repr(lines[i + 1]))
assert lines[i - 1].rstrip().endswith("`")
assert "集结" in lines[i + 1]
merged = "#   同池对照：真换行那几处（`GOF76` / `SW24`）抽得出，只有 `<br>` 那几处抽不出）。"
lines[i - 1:i + 2] = [merged]
io.open(P, "wb").write("\n".join(lines).encode("utf-8"))
b = io.open(P, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
