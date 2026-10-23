# -*- coding: utf-8 -*-
"""严格对账：before / after 两份计划表。

  python -I cmp_plan.py <before.json> <after.json>

输出：条目增删 / 新增条目的 (卡,字段,token,sprite) / 消失或被改的条目。
"""
import json, io, sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/icon/cmp_plan.py <参数1> <参数2> [参数...]\n  说明：严格对账：before / after 两份计划表。\n  选项：无自定义选项（不带参数 = 默认模式）\n  位置参数：脚本内读 sys.argv[1..2]（缺省/必填以脚本自身判断为准）')
    _wf_help_sys.exit(0)

sys.stdout.reconfigure(encoding="utf-8")


def key_items(path):
    d = json.load(io.open(path, encoding="utf-8"))
    out = {}
    for c in d["cards"]:
        for f in c["fields"]:
            for i in f["items"]:
                out[(c["id"], f["name"], i["token"])] = (i["sprite"], i["why"])
    return out


b = key_items(sys.argv[1])
a = key_items(sys.argv[2])
print("条目数: before=%d  after=%d  (+%d)" % (len(b), len(a), len(a) - len(b)))
added = [k for k in a if k not in b]
gone = [k for k in b if k not in a]
changed = [(k, b[k], a[k]) for k in b if k in a and b[k] != a[k]]
print("新增 %d 条 · 消失 %d 条 · 被改 %d 条" % (len(added), len(gone), len(changed)))
for k in sorted(gone):
    print("  🔴 消失:", k, b[k])
for k, x, y in changed:
    print("  🔴 被改:", k, "\n      before:", x, "\n      after :", y)
print("--- 新增逐条（按 sprite 分档）---")
from collections import Counter
cnt = Counter()
toks = Counter()
for k in sorted(added):
    cnt[a[k][0]] += 1
    toks[(k[2], a[k][0])] += 1
print("  by sprite:", dict(cnt))
print("  by (token,sprite):", dict(toks))
for k in sorted(added):
    print("   ", k, "->", a[k][0])
print("--- 两份 json 的 cards 数组是否逐字节相同 ---")
import hashlib
for p in sys.argv[1:]:
    d = json.load(io.open(p, encoding="utf-8"))
    s = json.dumps(d["cards"], ensure_ascii=False, sort_keys=True)
    print("  ", p, hashlib.md5(s.encode("utf-8")).hexdigest())
