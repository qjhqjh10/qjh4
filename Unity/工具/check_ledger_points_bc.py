#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B/C 行的落点体检（只读）—— 补 `check_ledger_points.py` 只查 A 行留下的盲区。

## 为什么需要它
`check_ledger_points.py` 只统计 `状态 == A` 的行 ⇒ **B/C 行的落点漂移永远静默**
（2026-10-28 `B91_L2` 实测：`348`/`428`/`708`/`709`/`514-517` 等 12 处全指错，机检一条都没报）。
本脚本把 B/C 行里**带 `文件:行` 的落点**逐个现读，判三档：
  · `MISSING-FILE`  落点里的文件不存在（最严重 —— 这种行「实现已落」整轮都无法复核）
  · `LINE-OOR`      行号超出文件长度
  · `NAME-ABSENT`   括号里给了方法名，但该行找不到那个名字（可能是漂移或指到注释）
⛔ 本脚本**只读**，不改任何文件；默认只打印统计 + 前 N 条，`--all` 打全。

## 用法
    python -I -X utf8 d:/4/Unity/工具/check_ledger_points_bc.py [--all] [--top N]

## 口径注意
- 落点里**没有 `文件:行`** 的 B/C 行（写「查不到」「—」「新建 …」）**不算问题**，单独计数。
- 判据里允许「类级命中」（只到文件 + 方法名、没有行号）：这类进 `NO-LINE` 桶，不判失败。
- 一个落点里可能有多段 `文件:行`（用 `·` 分隔），逐段检查。
"""
import glob
import io
import os
import re
import sys

LEDGER = r"d:/4/Unity/资料/完全复刻/账"
ASSETS = r"d:/4/Unity/MyGame/Assets"

# 一段落点：路径 + 行号（可带区间）+ 可选括号里的名字
SEG = re.compile(r"([A-Za-z0-9_./\\-]+\.cs):(\d+)(?:-(\d+))?(?:\s*[（(]([^）)]{0,40})[）)])?")


def load(path):
    b = io.open(path, "rb").read()
    text = b.decode("utf-8-sig")
    if b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n"):
        return text.split("\r\n")
    return text.split("\n")


_INDEX = None


def _build_index():
    """basename → [绝对路径]。落点里的路径写法很杂（`BattleDriver.cs` 裸名 / 相对 Assets / 带 CardPresentation 前缀）。"""
    idx = {}
    for root, _dirs, files in os.walk(ASSETS):
        for fn in files:
            if fn.endswith(".cs"):
                idx.setdefault(fn, []).append(os.path.join(root, fn).replace("\\", "/"))
    return idx


def resolve(rel):
    global _INDEX
    p = rel.replace("\\", "/")
    cands = [
        os.path.join(ASSETS, p).replace("\\", "/"),
        os.path.join(ASSETS, "CardPresentation", p).replace("\\", "/"),
    ]
    for c in cands:
        if os.path.isfile(c):
            return c
    if _INDEX is None:
        _INDEX = _build_index()
    base = p.split("/")[-1]
    hits = _INDEX.get(base, [])
    if not hits:
        return None
    # 多个同名文件时，优先「路径以落点写法结尾」的那一个
    for h in hits:
        if h.endswith("/" + p):
            return h
    return hits[0]


def main():
    show_all = "--all" in sys.argv
    top = 40
    if "--top" in sys.argv:
        i = sys.argv.index("--top")
        if i + 1 < len(sys.argv):
            top = int(sys.argv[i + 1])

    cache = {}
    counts = {"rows": 0, "no_line": 0, "segs": 0, "ok": 0,
              "MISSING-FILE": 0, "LINE-OOR": 0, "NAME-ABSENT": 0}
    bad = []

    for f in sorted(glob.glob(os.path.join(LEDGER, "*.tsv"))):
        name = os.path.basename(f)
        if name.startswith("_"):
            continue
        lines = load(f)
        if not lines:
            continue
        hdr = lines[0].split("\t")
        if "状态" not in hdr or "我们的落点" not in hdr:
            continue
        i_st, i_land = hdr.index("状态"), hdr.index("我们的落点")
        i_cls, i_meth = hdr.index("原版类"), hdr.index("方法")
        for n, l in enumerate(lines):
            if n == 0 or not l.strip():
                continue
            c = l.split("\t")
            if len(c) <= max(i_st, i_land):
                continue
            st = c[i_st].strip()
            if st not in ("B", "C"):
                continue
            counts["rows"] += 1
            land = c[i_land]
            segs = SEG.findall(land)
            if not segs:
                counts["no_line"] += 1
                continue
            for rel, ln, ln2, nm in segs:
                counts["segs"] += 1
                path = resolve(rel)
                if path is None:
                    counts["MISSING-FILE"] += 1
                    bad.append(("MISSING-FILE", name, n, c[i_cls] + "." + c[i_meth][:40], land[:90]))
                    continue
                if path not in cache:
                    cache[path] = load(path)
                body = cache[path]
                ln_i = int(ln)
                if ln_i < 1 or ln_i > len(body):
                    counts["LINE-OOR"] += 1
                    bad.append(("LINE-OOR", name, n, c[i_cls] + "." + c[i_meth][:40], land[:90]))
                    continue
                line_txt = body[ln_i - 1]
                if nm:
                    head = nm.strip().strip("`").split("·")[0].strip()
                    head = re.split(r"[：:，,、\s]", head)[0]
                    # 括号里常写 `Class.Method` 或 `Class.Method<T>` ⇒ 取最后一段标识符
                    head = head.split(".")[-1]
                    head = re.sub(r"<.*$", "", head)
                    head = head.strip("`'\"*")
                    if head and head not in line_txt:
                        counts["NAME-ABSENT"] += 1
                        bad.append(("NAME-ABSENT", name, n,
                                    c[i_cls] + "." + c[i_meth][:40],
                                    "落点 %s:%s(%s) 实为: %s" % (rel, ln, head, line_txt.strip()[:70])))
                        continue
                counts["ok"] += 1

    print("B/C 行 = %d · 落点里没有 `文件:行` 的 = %d · 带行号的段 = %d" %
          (counts["rows"], counts["no_line"], counts["segs"]))
    print("逐段：OK %d · 文件不存在 %d · 行号越界 %d · 名字不在该行 %d" %
          (counts["ok"], counts["MISSING-FILE"], counts["LINE-OOR"], counts["NAME-ABSENT"]))
    if not bad:
        print("✅ 没有发现问题")
        return
    print("\n---- 问题明细%s ----" % ("" if show_all else "（前 %d 条，加 --all 看全）" % top))
    for k, f, n, key, extra in (bad if show_all else bad[:top]):
        print("%-12s %-22s L%-5d %-46s %s" % (k, f, n, key, extra))


if __name__ == "__main__":
    main()
