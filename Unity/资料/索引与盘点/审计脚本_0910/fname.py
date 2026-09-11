# -*- coding: utf-8 -*-
"""审计 7：文件名问题扫描（非法字符 / 超长 / 00 前缀 / 尾点空格 / 非 BMP）。"""
import os, re, json, collections

ROOT = r"d:/2/解包整理"
OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"
illegal = re.compile(r'[<>:"|?*\x00-\x1f]')

bad_chars = collections.Counter()
long_names = []
zero_prefix = []
trailing = []
nonbmp = []
hash_names = []

for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        p = os.path.join(dp, f)
        rel = os.path.relpath(p, ROOT).replace("\\", "/")
        if illegal.search(f):
            bad_chars[f] += 1
        if len(f) > 150:
            long_names.append((len(f), rel))
        if re.match(r"^00[ _\-.]", f) or re.search(r"\s00\.", f) or re.search(r"\s00$", os.path.splitext(f)[0]):
            zero_prefix.append(rel)
        if f != f.strip() or f.rstrip(". ") != f:
            trailing.append(rel)
        for ch in f:
            if ord(ch) > 0xFFFF:
                nonbmp.append((rel, ch)); break
        if re.search(r"_(-?\d{6,})(_|\.|$)", f):
            hash_names.append(rel)

print("含 Windows 非法字符 (< > : \" | ? * 控制符) 的文件名:", len(bad_chars), "种")
for k, v in bad_chars.most_common(20):
    print("   ", repr(k), v)
print()
print("超长文件名 (>150 字符):", len(long_names))
for l, r in sorted(long_names, reverse=True)[:25]:
    print(f"    {l}  {r}")
print()
print("00 前缀 / 含 ' 00' 的名字:", len(zero_prefix))
for r in zero_prefix[:40]:
    print("   ", r)
print()
print("首尾空白 / 结尾点:", len(trailing))
for r in trailing[:20]:
    print("   ", r)
print()
print("含非 BMP 字符 (emoji 等):", len(nonbmp))
for r, c in nonbmp[:20]:
    print("   ", r, hex(ord(c)))
print()
print("文件名带 6 位以上数字后缀(pathID 泄漏):", len(hash_names))
for r in hash_names[:15]:
    print("   ", r)

json.dump({"bad_chars": dict(bad_chars), "long_names": long_names,
           "zero_prefix": zero_prefix, "trailing": trailing,
           "nonbmp": nonbmp, "hash_names": len(hash_names)},
          open(os.path.join(OUT, "filename_issues.json"), "w", encoding="utf-8"),
          ensure_ascii=False, indent=1)
