#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""ledger_mark.py — 真机验收【即时核销】用的小工具（主对话专用）

为什么要它：
  · 任务总表里的行**极长**（单行 1000+ 字符），用 Edit 做锚点容易改坏（CLAUDE.md 记过「行首当锚点」那次）；
  · 真机是「完成一处、核销一处」，需要几十次单行改写 —— 逐行手改既慢又容易碰坏表格。
  ⇒ 按 **ID** 找行、只改【状态】那一格（+ 往最后一格追加一句证据），其余字节原样保留。

用法：
  python -I ledger_mark.py mark <任务总表.md> <ID> <新状态> [备注]
  python -I ledger_mark.py check <文件>            只列出命中行（不改）
  python -I ledger_mark.py prepend <真Play待验清单.md> <A8> <前缀文本>   # 目录表没有状态列

规矩（照 CLAUDE.md）：
  · 二进制读、按原行尾写回（CRLF/LF 混行也不翻），改完打印 `git diff --numstat` 供核对；
  · **找不到 ID / 找到多行 ⇒ 不改、非 0 退出**（⛔ 不猜）。
"""
import io
import os
import re
import subprocess
import sys

try:  # Windows 控制台默认 GBK，中文/✓ 会炸
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")
except Exception:
    pass


def die(msg):
    sys.stderr.write("FAIL " + msg + "\n")
    sys.exit(2)


def read(path):
    raw = io.open(path, "rb").read()
    crlf = raw.count(b"\r\n")
    lf = raw.count(b"\n")
    eol = "\r\n" if crlf > 0 and crlf == lf else "\n"
    if crlf > 0 and crlf != lf:
        sys.stderr.write("WARN 混行尾（CRLF %d / LF %d）—— 按 %r 写回\n" % (crlf, lf, eol))
    return raw.decode("utf-8-sig"), eol


def write(path, text, eol):
    data = text.replace("\r\n", "\n").replace("\n", eol).encode("utf-8")
    io.open(path, "wb").write(data)
    try:
        out = subprocess.run(["git", "diff", "--numstat", "--", path],
                             capture_output=True, text=True, cwd=os.path.dirname(path) or ".")
        print("git diff --numstat:", out.stdout.strip() or "(无变化)")
    except Exception as e:  # 不是 git 仓库也照常落盘
        print("(git diff 跳过：%s)" % e)


def find_rows(lines, ident):
    """在表格里找第一格 = ident 的行（ident 的大小写/星号不敏感）。"""
    hits = []
    want = ident.replace("*", "").strip().lower()
    for i, ln in enumerate(lines):
        s = ln.strip()
        if not s.startswith("|"):
            continue
        cells = s.strip("|").split("|")
        if not cells:
            continue
        first = cells[0].replace("*", "").strip().lower()
        if first == want:
            hits.append((i, cells))
    return hits


def status_index(lines, row_i):
    """往上找最近的**表头**（必须以 `| # |` 或 `| 编号 |` 开头），返回「状态」列下标；找不到退回 3。
    ⚠️ 2026-10-25 踩过：原来只判「这一行以 | 开头且含『状态』」⇒ 匹配到正文里带「状态」二字的**数据行**，
       于是 T19/T23 的状态被塞进了第 4 格（表格错列）。判据必须是**表头本身**。"""
    for j in range(row_i, max(-1, row_i - 400), -1):
        s = lines[j].strip()
        if not s.startswith("|"):
            continue
        cells = [c.strip() for c in s.strip("|").split("|")]
        if len(cells) >= 3 and cells[0] in ("#", "编号"):
            for k, c in enumerate(cells):
                if c == "状态":
                    return k + 1  # 前面 split 掉的那个空串
    return 3


def rebuild(line, idx, value):
    eol = ""
    body = line.rstrip("\r\n")
    eol = line[len(body):]
    lead = "|" if body.strip().startswith("|") else ""
    seg = body.rstrip()
    tail_ws = body[len(seg):]
    parts = seg.strip("|").split("|")
    if idx - 1 >= len(parts):
        die("列下标越界（idx=%d, 列数=%d）" % (idx, len(parts)))
    parts[idx - 1] = " " + value.strip() + " "
    return "|" + "|".join(parts) + "|" + tail_ws + eol


def main():
    if len(sys.argv) < 3:
        die("用法见文件头")
    mode = sys.argv[1]
    path = sys.argv[2]
    text, eol = read(path)
    lines = text.splitlines(True)

    if mode == "check":
        ident = sys.argv[3]
        for i, cells in find_rows(lines, ident):
            print("%d: %s" % (i + 1, lines[i].rstrip()[:400]))
        return

    if mode == "mark":
        ident, status = sys.argv[3], sys.argv[4]
        note = sys.argv[5] if len(sys.argv) > 5 else ""
        hits = find_rows(lines, ident)
        if len(hits) != 1:
            die("`%s` 命中 %d 行（要恰好 1 行）" % (ident, len(hits)))
        i = hits[0][0]
        idx = status_index(lines, i)
        lines[i] = rebuild(lines[i], idx, status)
        if note:
            body = lines[i].rstrip("\r\n")
            eolx = lines[i][len(body):]
            if body.endswith("|"):
                body = body[:-1] + " ｜" + note + "|"
            lines[i] = body + eolx
        write(path, "".join(lines), eol)
        print("OK mark %s → %s" % (ident, status))
        return

    if mode == "prepend":
        ident, prefix = sys.argv[3], sys.argv[4]
        hits = find_rows(lines, ident)
        if len(hits) != 1:
            die("`%s` 命中 %d 行（要恰好 1 行）" % (ident, len(hits)))
        i = hits[0][0]
        lines[i] = rebuild(lines[i], 2, prefix + " " + hits[0][1][1].strip())
        write(path, "".join(lines), eol)
        print("OK prepend %s" % ident)
        return

    die("未知模式 %s" % mode)


if __name__ == "__main__":
    main()
