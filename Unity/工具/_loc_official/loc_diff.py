# -*- coding: utf-8 -*-
"""对比 `Loc.cs` 换官方值【前后】的中文列（只读）—— 用来找「断言里写死了旧译文」的地方。"""
**落盘位置**：`d:/4/Unity/工具/_loc_official/`（2026-10-14 第二十七会话从 `_tmp_a1070/` 搬进来 —— `_tmp_*` 有被清理的风险，而这几个是**判据脚本**）。
import io
import re

OLD = r"d:/4/Unity/工具/_loc_official/Loc_换官方前.cs.bak"
NEW = r"d:/4/Unity/MyGame/Assets/CardPresentation/Core/Loc.cs"
PAT = re.compile(r'\{\s*"([^"]+)"\s*,\s*new Entry\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*\)\s*\}')


def load(p):
    s = io.open(p, encoding="utf-8").read()
    return {m.group(1): (m.group(2), m.group(3)) for m in PAT.finditer(s)}


a, b = load(OLD), load(NEW)
n = 0
for k in sorted(a):
    if k not in b:
        continue
    if a[k][0] != b[k][0]:
        n += 1
        print("%-52s %-28s -> %s" % (k, a[k][0], b[k][0]))
print("共 %d 处中文变了" % n)
