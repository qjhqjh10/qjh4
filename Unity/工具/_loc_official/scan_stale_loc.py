# -*- coding: utf-8 -*-
"""扫 `Assets/**/*.cs`：还有哪些地方**写死了我们那批被换掉的旧译文**（可能是断言期望值）。只读。"""
**落盘位置**：`d:/4/Unity/工具/_loc_official/`（2026-10-14 第二十七会话从 `_tmp_a1070/` 搬进来 —— `_tmp_*` 有被清理的风险，而这几个是**判据脚本**）。
import io
import os
import re

OLD = r"d:/4/Unity/工具/_loc_official/Loc_换官方前.cs.bak"
NEW = r"d:/4/Unity/MyGame/Assets/CardPresentation/Core/Loc.cs"
ROOT = r"d:/4/Unity/MyGame/Assets"
PAT = re.compile(r'\{\s*"([^"]+)"\s*,\s*new Entry\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*\)\s*\}')


def load(p):
    s = io.open(p, encoding="utf-8").read()
    return {m.group(1): (m.group(2), m.group(3)) for m in PAT.finditer(s)}


bak, cur = load(OLD), load(NEW)
stale = []
for k, (o, _e) in bak.items():
    c = cur.get(k)
    if c is None or c[0] == o:
        continue
    if len(o) < 2 or "{" in o:
        continue
    stale.append((k, o, c[0]))
print("被换掉且**没被暂缓回**的键：%d 个" % len(stale))

files = []
for dirpath, dirnames, filenames in os.walk(ROOT):
    for fn in filenames:
        if fn.endswith(".cs"):
            files.append(os.path.join(dirpath, fn))
hits = {}
for f in files:
    try:
        s = io.open(f, encoding="utf-8", errors="replace").read()
    except Exception:
        continue
    for k, o, n in stale:
        if ('"' + o + '"') in s or ("\"" + o) in s:
            hits.setdefault(f, []).append((k, o, n))
for f in sorted(hits):
    print("\n==", f.replace(ROOT, "").lstrip("/\\"))
    for k, o, n in hits[f]:
        print("   %-46s 旧「%s」 -> 新「%s」" % (k, o, n))
print("\n共 %d 个文件命中" % len(hits))
