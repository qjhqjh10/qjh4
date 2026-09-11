# -*- coding: utf-8 -*-
"""只读审计：全量清点 解包整理 目录。"""
import os, sys, json, collections

ROOT = r"d:/2/解包整理"

def main():
    stats = collections.defaultdict(lambda: {"files":0,"bytes":0,"exts":collections.Counter()})
    empty_dirs = []
    zero_files = []
    total_files = 0
    total_bytes = 0
    for dirpath, dirnames, filenames in os.walk(ROOT):
        rel = os.path.relpath(dirpath, ROOT).replace("\\","/")
        parts = rel.split("/")
        top = parts[0]
        if not filenames and not dirnames:
            empty_dirs.append(rel)
        for f in filenames:
            p = os.path.join(dirpath,f)
            try:
                sz = os.path.getsize(p)
            except OSError:
                continue
            total_files += 1
            total_bytes += sz
            ext = os.path.splitext(f)[1].lower()
            s = stats[top]
            s["files"] += 1
            s["bytes"] += sz
            s["exts"][ext] += 1
            if sz == 0:
                zero_files.append(p.replace("\\","/"))
    print("=== TOTAL ===")
    print("files:", total_files, "bytes:", total_bytes, "GB:", round(total_bytes/1024**3,3))
    print()
    print("=== TOP LEVEL ===")
    for k in sorted(stats):
        s = stats[k]
        print(f"{k}\tfiles={s['files']}\tMB={round(s['bytes']/1024**2,1)}")
    print()
    print("=== EXT by top ===")
    for k in sorted(stats):
        exts = stats[k]["exts"].most_common(15)
        print(k, "|", " ".join(f"{e}:{c}" for e,c in exts))
    print()
    print("=== EMPTY DIRS ===", len(empty_dirs))
    for d in empty_dirs[:200]:
        print("  ", d)
    print()
    print("=== ZERO BYTE FILES ===", len(zero_files))
    for z in zero_files[:300]:
        print("  ", z)

    out = {
        "total_files": total_files,
        "total_bytes": total_bytes,
        "top": {k:{"files":v["files"],"bytes":v["bytes"],"exts":dict(v["exts"])} for k,v in stats.items()},
        "empty_dirs": empty_dirs,
        "zero_files": zero_files,
    }
    outp = r"d:/2/Warpforge_tools/tmp/audit_0910/inventory.json"
    os.makedirs(os.path.dirname(outp), exist_ok=True)
    with open(outp,"w",encoding="utf-8") as fh:
        json.dump(out, fh, ensure_ascii=False)
    print("\nwrote", outp)

main()
