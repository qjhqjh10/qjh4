#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""把补丁里「占位签名」的 `方法` 列换成主表里的精确签名。

## 为什么
代理常把 `方法` 写成 `Foo(...)` / `Foo(参数1, 参数2` 这种**缩写**，而主表存的是**完整签名**。
合并器按 `(原版类, 方法)` 精确匹配 ⇒ 这些行会被当成「新行」追加，
结果是**同一个原版方法在账里占两行**（重复键）。

## 做法（只按方法名唯一匹配，⛔ 不猜）
对补丁里的每一行：
1. 取方法名 = `方法` 列第一个 `(` 之前的部分；
2. 在**全部主表**里找 `原版类` 相同、且方法名相同的行；
3. **恰好 1 处** ⇒ 把 `方法` 换成那一行的完整签名；
   **0 处** ⇒ 可能是真·新行，**不动**，报出来；
   **>1 处** ⇒ 歧义，**不动**，报出来；
4. 改完的行若与主表某行同键，合并器就会走「就地更新」而不是「新增」。

## 用法
    python -I -X utf8 d:/4/_tmp_view/fixkey_1026.py --dry     # 只看
    python -I -X utf8 d:/4/_tmp_view/fixkey_1026.py           # 写回补丁

⛔ 只改 `账/_parts/B3_*.tsv`，⛔ 不碰主表。
"""
import io
import os
import re
import sys

LEDGER = r"d:/4/Unity/资料/完全复刻/账"
PARTS = os.path.join(LEDGER, "_parts")

# 🔴 认任意批次号（`B3_` / `B4_` …）。原来写死 `startswith("B3_")` ⇒ 批次 4 的补丁会被静默跳过。
PATCH_RE = re.compile(r"^B\d+_.*\.tsv$")


def read_raw(p):
    b = io.open(p, "rb").read()
    eol = "\r\n" if (b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n")) else "\n"
    return b.decode("utf-8-sig").split(eol), eol


def base_name(m):
    i = m.find("(")
    return (m[:i] if i >= 0 else m).strip()


def main():
    dry = "--dry" in sys.argv
    # 主表索引： (原版类, 方法名) -> [完整签名, ...]
    idx = {}
    for fn in sorted(os.listdir(LEDGER)):
        if not fn.endswith(".tsv") or fn.startswith("_"):
            continue
        lines, _e = read_raw(os.path.join(LEDGER, fn))
        for l in lines:
            f = l.split("\t")
            if len(f) < 3:
                continue
            k = (f[1].strip(), base_name(f[2].strip()))
            idx.setdefault(k, set()).add(f[2].strip())

    fixed = amb = miss = 0
    report = []
    for pfn in sorted(x for x in os.listdir(PARTS) if PATCH_RE.match(x)):
        p = os.path.join(PARTS, pfn)
        lines, eol = read_raw(p)
        changed = 0
        for i, l in enumerate(lines):
            f = l.split("\t")
            if len(f) != 11:
                continue
            meth = f[2].strip()
            if "(" not in meth:
                continue
            k = (f[1].strip(), base_name(meth))
            cands = idx.get(k, set())
            if meth in cands:            # 已经是精确签名，没事
                continue
            if len(cands) == 1:
                newm = next(iter(cands))
                report.append("  ✔ %s:%d  %s  ->  %s" % (pfn, i + 1, meth[:48], newm[:70]))
                f[2] = newm
                lines[i] = "\t".join(f)
                changed += 1
                fixed += 1
            elif len(cands) == 0:
                report.append("  ? %s:%d  %s  —— 主表无同名方法（可能是真·新行，未动）"
                              % (pfn, i + 1, meth[:60]))
                miss += 1
            else:
                report.append("  ⚠️ %s:%d  %s  —— 主表有 %d 个同名重载，歧义（未动）：%s"
                              % (pfn, i + 1, meth[:48], len(cands), " | ".join(sorted(cands))[:140]))
                amb += 1
        if changed and not dry:
            io.open(p, "wb").write(eol.join(lines).encode("utf-8"))
            print("✅ 写回 %s（改 %d 行）" % (pfn, changed))

    print("\n".join(report))
    print("\n合计：修正 %d · 主表无同名 %d · 歧义 %d%s"
          % (fixed, miss, amb, "（dry-run，未写盘）" if dry else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
