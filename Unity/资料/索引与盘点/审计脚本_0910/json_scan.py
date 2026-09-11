# -*- coding: utf-8 -*-
"""审计 14：全树 JSON 可解析性扫描（找截断/损坏的转储）。"""
import os, sys, json, collections

sys.stdout.reconfigure(encoding="utf-8")
ROOT = r"d:/2/解包整理"
OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"

bad = []
n = 0
empty_name = []
for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        if not f.lower().endswith(".json"):
            continue
        p = os.path.join(dp, f)
        n += 1
        try:
            with open(p, encoding="utf-8") as fh:
                json.load(fh)
        except Exception as e:
            bad.append((os.path.relpath(p, ROOT).replace("\\", "/"), type(e).__name__, str(e)[:120]))
print("JSON 总数:", n, " 解析失败:", len(bad))
for b in bad[:60]:
    print("  ", b)
json.dump(bad, open(os.path.join(OUT, "bad_json.json"), "w", encoding="utf-8"),
          ensure_ascii=False, indent=1)
