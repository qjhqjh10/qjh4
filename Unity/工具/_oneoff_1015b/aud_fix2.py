# -*- coding: utf-8 -*-
"""②-4：修 `项目任务.md` §〇 里那句过期两轮的交接指针。②-7：§29·d 的 `A1033` 行撑坏表格。"""
import io

P = r"d:/4/项目任务.md"
s = io.open(P, encoding="utf-8", newline="").read()
lines = s.split("\n")

# ②-4
i = [k for k, L in enumerate(lines) if "🏁 **`资料/交接_第三十一会话.md`**" in L]
assert len(i) == 1, i
k = i[0]
L = lines[k]
print("旧:", repr(L[:220]))
add = (" ⚠️ **2026-10-15（第三十二会话）就地订正（铁律 5）**：这一行**过期两轮** —— "
       "现行交接 = **`资料/交接_第三十三会话.md`**（判据 = `ls -t Unity/资料/交接_*.md | head -1`，现跑）。"
       "`交接_第三十一会话.md` / `交接_第三十二会话.md` 都已在 `资料/历史/`。")
if "过期两轮" not in L:
    lines[k] = L + add
    print("已补订正")

# ②-7：§29·d 里那条 3 格行
cand = [k for k, L in enumerate(lines) if "A1033" in L]
print("A1033 命中行:", [(k + 1, lines[k].count("|")) for k in cand])
for k in cand:
    if lines[k].count("|") not in (3,):
        print("   %d 竖线=%d: %s" % (k + 1, lines[k].count("|"), repr(lines[k][:260])))

io.open(P, "wb").write("\n".join(lines).encode("utf-8"))
b = io.open(P, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
