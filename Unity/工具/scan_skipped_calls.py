#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""scan_skipped_calls.py — 只读：从 Ghidra 反编译出的 `.c` 伪码里抽取「否定分支 / 早退 / goto / break」
附近的调用点，用来给「分支跳过点账」（S1）做初筛。

⛔ 只读：默认只往 stdout 打印；`--write PATH` 才落盘（且只写 PATH 这一个文件）。
⛔ 不碰 Unity / 不改账 / 不改 .cs。

用法:
  python -I -X utf8 d:/4/Unity/工具/scan_skipped_calls.py <a.c> [b.c ...] [--top N] [--write OUT.tsv]

输出列（TSV）:
  file | line | cond | then_calls | terminator | target | skipped_calls | else_calls

判定口径（与 `判据/B146_S1_分支跳过点账_攻击链.md` §一 同源）:
  * `terminator` ∈ {goto, break, return, continue, none}
  * `terminator == goto` 时，`target` = 目标标签行号，`skipped_calls` = 从 if 块结尾到该标签之间
    **被跳过**的调用（这就是「少走的一步」）。
  * `terminator == break/return/continue` 时，`skipped_calls` = 该 if 之后、到本 case/函数结尾之间
    的调用（粗筛，需要人工判「这一段本来就不该走」还是「真的被跳了」）。

⚠️ 这个工具只做**初筛**：`.c` 伪码会吞返回值 / 丢实参常量 / 有 ICF 折叠 ⇒ 每一条都要人工回读，
   并按 `out_il2csharp` 交叉核（见报告 §一 的「人工复核规则」）。
"""
import io
import os
import re
import sys

CALL_RE = re.compile(r"(?<![A-Za-z0-9_])([A-Za-z_][A-Za-z0-9_]*(?:__[A-Za-z0-9_]+|___ctor))\s*\(")
LABEL_RE = re.compile(r"^\s*(LAB_[0-9a-fA-F]+|joined_[0-9a-fA-F]+)\s*:")
GOTO_RE = re.compile(r"\bgoto\s+(LAB_[0-9a-fA-F]+|joined_[0-9a-fA-F]+)")
IF_RE = re.compile(r"^\s*\}?\s*if\s*\(")
SKIP_CALL = re.compile(r"^(FUN_|thunk_FUN_|il2cpp_)")


def read_lines(path):
    # 二进制读 + 自己按行切（项目纪律：源码按行号引用时一律二进制读）
    b = io.open(path, "rb").read()
    b = b.replace(b"\r\r\n", b"\r\n")
    return b.decode("utf-8", "replace").split("\n")


def strip_comment(line):
    i = line.find("/*")
    if i >= 0:
        j = line.find("*/", i + 2)
        if j >= 0:
            return line[:i] + line[j + 2:]
    return line


def collect_calls(text):
    out = []
    for m in CALL_RE.finditer(text):
        name = m.group(1)
        if name.startswith("if"):
            continue
        if SKIP_CALL.match(name):
            continue
        if name not in out:
            out.append(name)
    return out


def find_if_body_end(lines, start):
    """从 `if (...)` 所在行出发，返回 (body_begin, body_end, has_braces)。
    body_end 是块内最后一个**有效**行号（闭括号那行）。"""
    i = start
    # 找条件括号的闭合
    text = strip_comment(lines[i])
    depth = 0
    seen = False
    while i < len(lines):
        text = strip_comment(lines[i])
        for ch in text:
            if ch == "(":
                depth += 1
                seen = True
            elif ch == ")":
                depth -= 1
        if seen and depth == 0:
            break
        i += 1
        if i >= len(lines):
            return start, start, False
    # 条件闭合在本行（可能跨行）
    close_line = i
    tail = strip_comment(lines[close_line])
    # 去掉条件部分，看剩下的是 `{` 还是单句
    # 简化：从最后一个 ')' 之后取
    k = tail.rfind(")")
    rest = tail[k + 1:] if k >= 0 else tail
    if "{" in rest:
        # 块开头可能在后续行
        depth = rest.count("{") - rest.count("}")
        j = close_line
        while j < len(lines) and depth > 0:
            j += 1
            if j >= len(lines):
                break
            t2 = strip_comment(lines[j])
            depth += t2.count("{") - t2.count("}")
        return close_line + 1, min(j, len(lines) - 1), True
    # 无花括号：body 只有一行
    return close_line + 1, close_line + 1, False


def scan_file(path):
    lines = read_lines(path)
    fn = lines[1].strip() if len(lines) > 1 else ""
    labels = {}
    for idx, ln in enumerate(lines):
        m = LABEL_RE.match(ln)
        if m:
            labels[m.group(1)] = idx + 1  # 1-based

    rows = []
    for i, raw in enumerate(lines):
        if not IF_RE.match(raw):
            continue
        body_begin, body_end, _braced = find_if_body_end(lines, i)
        body_txt = "\n".join(strip_comment(x) for x in lines[i:body_end + 1])
        then_calls = collect_calls("\n".join(strip_comment(x) for x in lines[body_begin:body_end + 1]))
        term = "none"
        target = ""
        if re.search(r"\bgoto\s+", body_txt):
            m = GOTO_RE.search(body_txt)
            if m:
                term, target = "goto", m.group(1)
        elif re.search(r"\breturn\b", body_txt):
            term = "return"
        elif re.search(r"\bbreak\b", body_txt):
            term = "break"
        elif re.search(r"\bcontinue\b", body_txt):
            term = "continue"

        skipped = []
        if term == "goto" and target in labels:
            tline = labels[target]
            lo, hi = sorted((body_end, tline - 1))
            skipped = collect_calls("\n".join(strip_comment(x) for x in lines[lo:hi]))
        elif term in ("break", "return", "continue"):
            # 粗筛：往下 40 行以内（同 case 内）的调用
            lo, hi = body_end, min(len(lines) - 1, body_end + 40)
            skipped = collect_calls("\n".join(strip_comment(x) for x in lines[lo:hi]))

        # else 分支
        else_calls = []
        # 找 `}` 之后紧邻的 else
        j = body_end
        if j + 1 < len(lines):
            nxt = strip_comment(lines[j + 1]).strip()
            k = j + 1
            while k < len(lines) and (nxt == "" or nxt.startswith("/*")):
                k += 1
                nxt = strip_comment(lines[k]).strip() if k < len(lines) else ""
            if nxt.startswith("else"):
                eb, ee, _ = find_if_body_end(lines, k) if "if" in nxt else (k + 1, k + 1, True)
                if "if" not in nxt:
                    # `else {`
                    depth = strip_comment(lines[k]).count("{") - strip_comment(lines[k]).count("}")
                    m = k
                    while m < len(lines) and depth > 0:
                        m += 1
                        if m >= len(lines):
                            break
                        depth += strip_comment(lines[m]).count("{") - strip_comment(lines[m]).count("}")
                    eb, ee = k + 1, min(m, len(lines) - 1)
                else_calls = collect_calls("\n".join(strip_comment(x) for x in lines[eb:ee + 1]))

        cond = strip_comment(lines[i]).strip()
        rows.append({
            "file": os.path.basename(path),
            "line": i + 1,
            "cond": cond[:160],
            "then_calls": ",".join(then_calls)[:300],
            "terminator": term,
            "target": target,
            "skipped_calls": ",".join(skipped)[:300],
            "else_calls": ",".join(else_calls)[:300],
        })
    return fn, rows


def main(argv):
    files = []
    top = 0
    out = None
    write_flag = False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--write":
            i += 1
            if i >= len(argv):
                sys.stderr.write("--write 需要路径\n")
                return 2
            out = argv[i]
            write_flag = True
        elif a == "--top":
            i += 1
            top = int(argv[i])
        elif a in ("-h", "--help"):
            sys.stderr.write(__doc__)
            return 0
        elif a.startswith("-"):
            sys.stderr.write("未知参数: %s\n" % a)
            return 2
        else:
            files.append(a)
        i += 1

    if not files:
        sys.stderr.write("用法: python -I -X utf8 scan_skipped_calls.py <a.c> [...] [--top N] [--write OUT.tsv]\n")
        return 2

    header = ["file", "line", "cond", "then_calls", "terminator", "target", "skipped_calls", "else_calls"]
    all_rows = []
    for f in files:
        if not os.path.isfile(f):
            sys.stderr.write("跳过（不是文件）: %s\n" % f)
            continue
        fn, rows = scan_file(f)
        sys.stderr.write("== %s :: %s -> %d 条 if\n" % (os.path.basename(f), fn, len(rows)))
        all_rows.extend(rows)

    if top > 0:
        all_rows = [r for r in all_rows if r["terminator"] != "none" or r["skipped_calls"]][:top]

    lines = ["\t".join(header)]
    for r in all_rows:
        lines.append("\t".join(str(r[h]).replace("\t", " ") for h in header))
    text = "\n".join(lines) + "\n"

    if write_flag:
        io.open(out, "wb").write(text.encode("utf-8"))
        sys.stderr.write("写入 %s（%d 行）\n" % (out, len(lines)))
    else:
        sys.stdout.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
