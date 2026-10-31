#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""aud11_rescan.py -- AUD11 3A 家族（块级 guard 吞断言）复扫器

用途：`B105_AUD11b.md` 那三张表是 **F1–F6 修复前** 的快照。修完一轮之后要重扫。
本脚本 = 那套判定规则的**可复跑版**（§1·2 的 1–6 条）。

用法：
    python -I -X utf8 d:/4/Unity/工具/aud11_rescan.py <文件...>          # 人读表
    python -I -X utf8 d:/4/Unity/工具/aud11_rescan.py --tsv <文件...>    # TSV

判定：
  - 只认 Allman / 同行花括号的**跨行块**（单行闭合块不计）
  - 块内 Check 族调用数 >= 2 => 3A-b；== 1 => 3A-brace
  - 三类「已覆盖」命中即排除：
      pretag   : 前 1-8 行有字面量 `（前提`
      semantic : 前 1-8 行有**同一变量**的 `X != null`
      elseguard: 块尾后 16 行内有 else，且其中 Check(false / CheckTrue(false / 文案含（前提

⚠️ 已知盲区（与 B105_AUD11b §1·4 一致，别当「不存在」）：
  - `if` 条件跨行 => 跳过（不计入，也未排除）
  - 纯 `else { ... }` 块不扫
  - 方法组 / 反射调用不识别
"""
import io
import os
import re
import sys


CODE, LINE, BLOCK, STR, VSTR, CHR = 0, 1, 2, 3, 4, 5

CHECK_RE = re.compile(
    r"\b(Check[A-Za-z0-9_]*|MenuCheck[A-Za-z0-9_]*|NodeCheck[A-Za-z0-9_]*|"
    r"Assert[A-Za-z][A-Za-z0-9_]*|Expect[A-Za-z][A-Za-z0-9_]*)\s*\(")

KEYWORDS = {
    "if", "else", "null", "true", "false", "var", "new", "return", "this", "base",
    "out", "ref", "in", "is", "as", "typeof", "nameof", "default", "string", "int",
    "float", "bool", "double", "long", "object", "Count", "Length", "Any", "Equals",
}


def strip_code(raw: bytes):
    """把注释/字符串抹成空格（列号不变），返回每行抹后的文本。"""
    text = raw.decode("utf-8", "replace")
    lines = text.split("\n")
    out = []
    state = CODE
    for ln in lines:
        ln = ln.rstrip("\r")
        buf = []
        i = 0
        n = len(ln)
        while i < n:
            c = ln[i]
            if state == CODE:
                if c == "/" and i + 1 < n and ln[i + 1] == "/":
                    buf.append(" " * (n - i))
                    i = n
                    continue
                if c == "/" and i + 1 < n and ln[i + 1] == "*":
                    state = BLOCK
                    buf.append("  ")
                    i += 2
                    continue
                if c == '"':
                    # 逐字字符串 @"..."/$@"..." 简化处理：按普通字符串（本仓少见跨行）
                    state = STR
                    buf.append(" ")
                    i += 1
                    continue
                if c == "'":
                    state = CHR
                    buf.append(" ")
                    i += 1
                    continue
                buf.append(c)
                i += 1
            elif state == BLOCK:
                if c == "*" and i + 1 < n and ln[i + 1] == "/":
                    state = CODE
                    buf.append("  ")
                    i += 2
                    continue
                buf.append(" ")
                i += 1
            elif state == STR:
                if c == "\\" and i + 1 < n:
                    buf.append("  ")
                    i += 2
                    continue
                if c == '"':
                    state = CODE
                    buf.append(" ")
                    i += 1
                    continue
                buf.append(" ")
                i += 1
            elif state == CHR:
                if c == "\\" and i + 1 < n:
                    buf.append("  ")
                    i += 2
                    continue
                if c == "'":
                    state = CODE
                    buf.append(" ")
                    i += 1
                    continue
                buf.append(" ")
                i += 1
        out.append("".join(buf))
        if state == STR or state == CHR:
            # 单行字符串未闭合 => 保守地退回 CODE（本仓正常代码不会这样）
            state = CODE
    return out, lines


def match_paren(s, start):
    """s[start] == '('，返回配平的 ')' 下标；失败返回 -1。"""
    depth = 0
    for i in range(start, len(s)):
        c = s[i]
        if c == "(":
            depth += 1
        elif c == ")":
            depth -= 1
            if depth == 0:
                return i
    return -1


def find_if_blocks(code_lines):
    blocks = []
    for idx, line in enumerate(code_lines):
        m = re.search(r"(?<![\w.])if\s*\(", line)
        if not m:
            continue
        pstart = line.index("(", m.start())
        pend = match_paren(line, pstart)
        if pend < 0:
            continue  # 条件跨行 => 跳过（盲区）
        cond = line[pstart + 1:pend]
        rest = line[pend + 1:]
        # 块起点
        if rest.lstrip().startswith("{"):
            bstart = idx
            after = rest.lstrip()[1:]
        else:
            # 下一非空行是否以 { 开头
            j = idx + 1
            while j < len(code_lines) and not code_lines[j].strip():
                j += 1
            if j >= len(code_lines) or not code_lines[j].lstrip().startswith("{"):
                continue  # 单语句 if 或无块
            bstart = j
            after = ""
        if after.strip():
            continue  # 同一行闭合 => 单行块，不计
        # 块尾：从 bstart 起配平花括号
        depth = 0
        bend = -1
        for j in range(bstart, len(code_lines)):
            seg = code_lines[j]
            for c in seg:
                if c == "{":
                    depth += 1
                elif c == "}":
                    depth -= 1
                    if depth == 0:
                        bend = j
                        break
            if bend >= 0:
                break
        if bend < 0:
            continue
        if bend == bstart:
            continue
        blocks.append((idx, bstart, bend, cond))
    return blocks


def varnames(cond):
    out = []
    for w in re.findall(r"\b[A-Za-z_][A-Za-z0-9_]*\b", cond):
        if w in KEYWORDS:
            continue
        if w not in out:
            out.append(w)
    return out


def enclosing_start(code_lines, ifline):
    """从 if 行往上找包住它的那个 `{` 行（方法体 / 外层块）。"""
    depth = 0
    for j in range(ifline, -1, -1):
        for c in reversed(code_lines[j]):
            if c == "}":
                depth += 1
            elif c == "{":
                depth -= 1
                if depth < 0:
                    return j
    return 0


def covered(code_lines, raw_lines, ifline, bend, cond):
    lo = max(0, ifline - 8)
    window = raw_lines[lo:ifline]
    joined = "\n".join(window)
    if "（前提" in joined:
        return "pretag"
    vs = varnames(cond)
    lo_block = enclosing_start(code_lines, ifline)
    for v in vs:
        # 2026-10-31 修订：窗口从「前 1–8 行」放宽到「同作用域 + 变量无再赋值」
        # （B105_AUD11b §五 与 B106_W5/W6 实测：本仓前提常写在 9–120 行前 ⇒ 原窗口系统性假阳性）
        for j in range(ifline - 1, lo_block - 1, -1):
            cl = code_lines[j]
            if re.search(r"\b%s\s*=(?!=)" % re.escape(v), cl):
                break                                  # 变量被重新赋值 ⇒ 之前的前提管不到
            if re.search(r"\b%s\s*!=\s*null\b" % re.escape(v), cl):
                return "semantic"
            # 「变量出现在断言实参里」这条只认**近窗**（前 8 行）—— 全作用域放宽它会把
            # 「顺手拿这个变量做的其它断言」误当成判空闸（假阴性）。判空 `!= null` 才走全作用域。
            if j >= ifline - 8 and CHECK_RE.search(cl) and re.search(r"\b%s\b" % re.escape(v), cl):
                return "semantic-arg"
    # else guard
    hi = min(len(code_lines), bend + 17)
    tail = code_lines[bend + 1:hi]
    for k, t in enumerate(tail):
        if re.match(r"\s*else\b", t):
            blk = "\n".join(tail[k:k + 10])
            rawblk = "\n".join(raw_lines[bend + 1 + k:bend + 1 + k + 10])
            if "（前提" in rawblk:
                return "elseguard"
            # 2026-10-31（B106_W6 实测）：三参 `Check<T>(true, false, …)` / 任意实参含 false 也算报红
            if re.search(r"Check\w*\s*\([^)\n]*\bfalse\b", blk):
                return "elseguard"
            break
    return None


def classify(cond):
    """3A 家族只收「判空 guard」；null-eq 是写对了的对照组；其余是 3B（合法业务分支居多）。"""
    if re.search(r"(?<![=!<>])==\s*null\b", cond) or re.search(r"\bnull\s*==", cond):
        return "null-eq"
    if re.search(r"!=\s*null\b", cond):
        return "3A"
    return "3B"


def scan(path, want):
    raw = io.open(path, "rb").read()
    code_lines, raw_lines = strip_code(raw)
    rows = []
    for ifline, bstart, bend, cond in find_if_blocks(code_lines):
        n = 0
        for j in range(ifline, min(bend + 1, len(code_lines))):
            n += len(CHECK_RE.findall(code_lines[j]))
        if n == 0:
            continue
        fam = classify(cond)
        if fam == "null-eq":
            continue                      # 对照组：条件为真时报红，写对了
        if want == "3A" and fam != "3A":
            continue
        if want == "3B" and fam != "3B":
            continue
        why = covered(code_lines, raw_lines, ifline, bend, cond)
        if why:
            continue
        kind = ("3A-b" if fam == "3A" else "3B-b") if n >= 2 else \
               ("3A-brace" if fam == "3A" else "3B-brace")
        rows.append((ifline + 1, kind, n, bend + 1, cond.strip()[:60]))
    return rows


def main(argv):
    tsv = "--tsv" in argv
    want = "3B" if "--3b" in argv else "3A"
    files = [a for a in argv if not a.startswith("--")]
    if not files:
        print(__doc__)
        return 1
    total = 0
    for path in files:
        if not os.path.isfile(path):
            print("MISSING\t%s" % path)
            continue
        rows = scan(path, want)
        total += len(rows)
        if tsv:
            for r in rows:
                print("%s\t%d\t%s\t%d\t%d\t%s" % (
                    os.path.basename(path), r[0], r[1], r[2], r[3], r[4]))
        else:
            print("=== %s : %d ===" % (path, len(rows)))
            for r in rows:
                print("  L%-7d %-9s Checks=%-3d blockEnd=L%-7d cond=%s" % r)
    if tsv:
        pass
    else:
        print("TOTAL %d" % total)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
