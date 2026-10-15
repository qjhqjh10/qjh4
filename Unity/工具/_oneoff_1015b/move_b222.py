# -*- coding: utf-8 -*-
"""把 B2-22 那一步挪到【T 刚读出来、还没被别的编辑动过】的位置，便于定位失败原因。"""
import io

P = r"d:/4/Unity/工具/_oneoff_1015b/land_1032.py"
s = io.open(P, encoding="utf-8").read()

CALL = ('    T = edit(T, "**\\u6536\\u53e3\\u89c1 \\u00a7\\u56db \\u90a3\\u4e00\\u884c**", '
        '"**\\u6536\\u53e3\\u89c1 \\u00a7\\u4e94 \\u90a3\\u4e00\\u884c**", "B2-22")\n')
assert s.count(CALL) == 1, ("CALL 命中 %d" % s.count(CALL))

# 删掉原位置
s = s.replace(CALL, "", 1)

# 插到 `    T = read(TASK)` 之后
ANCH = "    T = read(TASK)\n"
assert s.count(ANCH) == 1, ("ANCH 命中 %d" % s.count(ANCH))
s = s.replace(ANCH, ANCH + CALL, 1)

io.open(P, "w", encoding="utf-8", newline="").write(s)
print("OK")
