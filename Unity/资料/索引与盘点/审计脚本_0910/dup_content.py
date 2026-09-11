# -*- coding: utf-8 -*-
"""审计 2：全量 md5 去重 + 验证 _X / _X_X 是否逐字节相同。"""
import os, re, sys, json, hashlib, collections, time

ROOT = r"d:/2/解包整理"
OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"

def md5_of(path, bufsize=1 << 20):
    h = hashlib.md5()
    with open(path, "rb") as f:
        while True:
            b = f.read(bufsize)
            if not b:
                break
            h.update(b)
    return h.hexdigest()

def main():
    t0 = time.time()
    by_size = collections.defaultdict(list)
    allfiles = []
    for dirpath, dirnames, filenames in os.walk(ROOT):
        for f in filenames:
            p = os.path.join(dirpath, f)
            try:
                sz = os.path.getsize(p)
            except OSError:
                continue
            allfiles.append((p, sz))
            by_size[sz].append(p)
    print("total files", len(allfiles), "unique sizes", len(by_size), f"{time.time()-t0:.1f}s")

    # 只对同 size 的做 md5
    digest = {}
    n_hash = 0
    for sz, paths in by_size.items():
        if sz == 0 or len(paths) < 2:
            continue
        for p in paths:
            try:
                digest[p] = md5_of(p)
            except OSError:
                pass
            n_hash += 1
    print("hashed", n_hash, f"{time.time()-t0:.1f}s")

    # 分组
    groups = collections.defaultdict(list)
    for p, d in digest.items():
        groups[d].append(p)

    dup_groups = {d: ps for d, ps in groups.items() if len(ps) > 1}
    dup_files = sum(len(ps) - 1 for ps in dup_groups.values())
    dup_bytes = sum(os.path.getsize(ps[0]) * (len(ps) - 1) for ps in dup_groups.values())
    print(f"内容重复组: {len(dup_groups)}  冗余文件: {dup_files}  冗余字节: {dup_bytes/1024**2:.1f} MB")

    # 分类：_X_X 模式 vs 跨目录真重复
    pat = re.compile(r"^(?P<c>[A-Za-z_][A-Za-z0-9_]*)_(?P<i>\d+)_(?P=i)$")
    kind = collections.Counter()
    examples = collections.defaultdict(list)
    cross_examples = []
    intra_dir = []
    for d, ps in dup_groups.items():
        stems = [os.path.splitext(os.path.basename(p))[0] for p in ps]
        dirs = set(os.path.dirname(p) for p in ps)
        m = pat.match(stems[0])
        all_same_dir = len(dirs) == 1
        if m and all(pat.match(s) and pat.match(s).group('i') == m.group('i') for s in stems):
            kind['_X_X模式'] += len(ps) - 1
            if len(examples['_X_X模式']) < 3:
                examples['_X_X模式'].append(ps[:2])
        elif all_same_dir:
            kind['同目录同内容'] += len(ps) - 1
            if len(intra_dir) < 25:
                intra_dir.append(ps)
        else:
            kind['跨目录同内容'] += len(ps) - 1
            if len(cross_examples) < 60:
                cross_examples.append(ps)
    print()
    print("=== 重复类型 ===")
    for k, v in kind.most_common():
        print(f"  {k}: {v}")
    print()
    print("=== 同目录同内容（非 _X_X 命名）示例 ===")
    for ps in intra_dir:
        print("  ", [os.path.relpath(p, ROOT) for p in ps][:6])
    print()
    print("=== 跨目录同内容 示例 ===")
    for ps in cross_examples:
        print("  ", [os.path.relpath(p, ROOT) for p in ps][:5])

    json.dump({
        "n_files": len(allfiles),
        "dup_groups": len(dup_groups),
        "dup_files": dup_files,
        "dup_bytes": dup_bytes,
        "kinds": dict(kind),
    }, open(os.path.join(OUT, "dup_content.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    # 全量 dump 组（可能很大）
    with open(os.path.join(OUT, "dup_groups.txt"), "w", encoding="utf-8") as fh:
        for d, ps in sorted(dup_groups.items(), key=lambda kv: -len(kv[1])):
            fh.write(f"# {d} n={len(ps)}\n")
            for p in ps:
                fh.write("  " + os.path.relpath(p, ROOT).replace("\\", "/") + "\n")
    print("\nwrote dup_content.json / dup_groups.txt", f"{time.time()-t0:.1f}s")

main()
