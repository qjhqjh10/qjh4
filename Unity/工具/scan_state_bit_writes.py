#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Scan Ghidra `.c` pseudo-code (and dump.cs field tables) for **state-bit writes**.

Why this exists
---------------
The original game keeps many coroutine flags in state-machine fields
(`<defenderReturnsDamage>5__4 // 0x138`, ...).  Ghidra often merges two
adjacent `bool` fields into one 16-bit store, e.g.

    *(undefined2 *)(param_1 + 0x138) = 0x100;   // clears 0x138, sets 0x139

If we map such a flag to a single C# `bool`, one of the two semantics is lost
silently.  This scanner lists every candidate write so a human can build a
"state bit ledger" (one row per offset: who writes, who reads, our field).

It does NOT decide anything.  It only finds candidate lines.
It is read-only unless `--write` is passed.

Usage
-----
    python -I -X utf8 d:/4/Unity/工具/scan_state_bit_writes.py <dir-or-file> [...]
    python -I -X utf8 .../scan_state_bit_writes.py <dir> --only \\.c$
    python -I -X utf8 .../scan_state_bit_writes.py <dir> --context 2
    python -I -X utf8 .../scan_state_bit_writes.py <dir> --write out.txt
    python -I -X utf8 .../scan_state_bit_writes.py --fields <dump.cs> --class "BattleManager.<ResolveAttack>d__438"

Modes
-----
  (default)  scan sources, print candidate write lines + context to stdout
  --fields   print field declarations carrying `// 0xNN` offsets from a dump.cs
  --reads    also print reads of the same offsets seen in the scanned files
             (default: on; `--no-reads` disables)

Default is stdout only.  `--write <path>` is required to write a file.

Each candidate line carries a `[write] / [clear] / [setbits] / [clearbits] /
[xorbit]` label.  NOTE: the label is a *mechanical* reading of the literal.
A wider store (e.g. `*(undefined2 *)(p + 0x138) = 0x100`) writes TWO adjacent
byte fields at once -- it clears `+0x138` and sets `+0x139`.  A human must
decompose those; the scanner only reports the store.
"""

import io
import os
import re
import sys

# --------------------------------------------------------------------------
# Writes:  *(undefined2 *)(EXPR + 0xNN) = VALUE;
#          *(undefined1 *)((longlong)EXPR + 0xNN) = VALUE;
#          *(char *)(EXPR + 0xNN) |= 0x..;   &=  ;  ^=  ;
# Type tokens seen in Ghidra output.  Keep the list explicit so we do not
# match a function call that merely looks like a cast.
# --------------------------------------------------------------------------
TYPE_TOKENS = (
    "undefined1", "undefined2", "undefined4", "undefined8",
    "char", "uchar", "byte", "sbyte", "short", "ushort",
    "int", "uint", "long", "ulong", "longlong", "ulonglong",
    "float", "double", "bool", "code", "pointer",
)
TYPE_RE = "|".join(TYPE_TOKENS)

# Lazy base expression: allows `param_1`, `(longlong)puVar23`, `*puVar19`, etc.
# `=(?!=)` keeps `==` comparisons out (Ghidra writes `if (*(int*)(p+0x8) == 0)`).
WRITE_RE = re.compile(
    r"\*\s*\(\s*(?P<type>" + TYPE_RE + r")\s*\*\s*\)"
    r"\s*\(\s*(?P<base>.+?)\s*\+\s*(?P<off>0x[0-9a-fA-F]+|[0-9]+)\s*\)"
    r"\s*(?P<op>\|=|&=|\^=|=(?!=))\s*(?P<val>[^;]+);"
)

PTR_SIZE = {
    "undefined1": 1, "undefined2": 2, "undefined4": 4, "undefined8": 8,
    "char": 1, "byte": 1, "sbyte": 1, "uchar": 1,
    "short": 2, "ushort": 2,
    "int": 4, "uint": 4, "float": 4,
    "long": 8, "ulong": 8, "longlong": 8, "ulonglong": 8, "double": 8,
}
DECL_RE = re.compile(
    r"^\s*(?P<ty>[A-Za-z_][\w ]*?)\s*(?P<star>\*+)?\s*(?P<name>[A-Za-z_]\w*)\s*(\[[^\]]*\])?\s*;"
)

# Read shapes:  *(int *)(EXPR + 0xNN)   /  *(... + 0xNN) == ...
READ_RE = re.compile(
    r"\*\s*\(\s*(?P<type>" + TYPE_RE + r")\s*\*\s*\)"
    r"\s*\(\s*(?P<base>.+?)\s*\+\s*(?P<off>0x[0-9a-fA-F]+|[0-9]+)\s*\)"
)

FIELD_RE = re.compile(
    r"^\s*(?P<decl>.+?);\s*//\s*(?P<off>0x[0-9a-fA-F]+)\s*$"
)


def iter_files(paths, only):
    pat = re.compile(only) if only else None
    for p in paths:
        p = p.replace("\\", "/")
        if os.path.isdir(p):
            for root, dirs, files in os.walk(p):
                dirs.sort()
                for f in sorted(files):
                    if pat and not pat.search(f):
                        continue
                    yield os.path.join(root, f)
        elif os.path.isfile(p):
            if pat and not pat.search(os.path.basename(p)):
                continue
            yield p


def read_text(path):
    b = io.open(path, "rb").read()
    # Ghidra output is UTF-8/ASCII; dump.cs may carry a BOM.
    return b.decode("utf-8-sig", errors="replace")


def decl_map(lines):
    """name -> (declared type, is_pointer). Used to flag pointer-scaled offsets."""
    m = {}
    for line in lines[:400]:
        d = DECL_RE.match(line)
        if not d:
            continue
        ty = d.group("ty").strip().split()[-1]
        m[d.group("name")] = (ty, bool(d.group("star")))
    return m


def classify(op, val):
    """Human label: write / clear / set-bits / clear-bits / xor-bits."""
    if op == "|=":
        return "setbits"
    if op == "&=":
        return "clearbits"
    if op == "^=":
        return "xorbit"
    v = val.strip()
    if v in ("0", "0x0", "0x00", "'\\0'"):
        return "clear"
    return "write"


def scan_writes(path, context):
    text = read_text(path)
    lines = text.split("\n")
    decls = decl_map(lines)
    out = []
    for i, line in enumerate(lines, 1):
        for m in WRITE_RE.finditer(line):
            lo = max(1, i - context)
            hi = min(len(lines), i + context)
            ctx = [(j, lines[j - 1]) for j in range(lo, hi + 1)]
            base = m.group("base").strip()
            note = ""
            scaled = False
            if re.match(r"^[A-Za-z_]\w*$", base):
                ty, is_ptr = decls.get(base, ("", False))
                sz = PTR_SIZE.get(ty, 1) if is_ptr else 1
                if sz > 1:
                    scaled = True
                    note = "  [pointer-scaled x%d: real byte offset = 0x%x]" % (
                        sz, int(m.group("off"), 0) * sz)
            out.append({
                "file": path,
                "line": i,
                "type": m.group("type"),
                "base": base,
                "off": m.group("off"),
                "op": m.group("op"),
                "val": m.group("val").strip(),
                "kind": classify(m.group("op"), m.group("val")),
                "scaled": scaled,
                "note": note,
                "ctx": ctx,
            })
    return out


def scan_reads(path):
    """Return {offset_normalized: [line numbers]} for the file (skips pointer-scaled)."""
    text = read_text(path)
    lines = text.split("\n")
    decls = decl_map(lines)
    hits = {}
    for i, line in enumerate(lines, 1):
        for m in READ_RE.finditer(line):
            base = m.group("base").strip()
            if re.match(r"^[A-Za-z_]\w*$", base):
                ty, is_ptr = decls.get(base, ("", False))
                if is_ptr and PTR_SIZE.get(ty, 1) > 1:
                    continue        # index into a struct array, not a byte offset
            off = m.group("off").lower()
            hits.setdefault(off, []).append(i)
    return hits


def norm_off(s):
    try:
        v = int(s, 0)
    except ValueError:
        return s
    return "0x%x" % v


def mode_fields(path, cls):
    text = read_text(path)
    lines = text.split("\n")
    cur = None
    print("== fields with 0x offsets: %s ==" % path)
    for i, line in enumerate(lines, 1):
        s = line.strip()
        if re.search(r"\bclass\s+\S", s) and not s.startswith("//"):
            cur = s
        m = FIELD_RE.match(line)
        if not m:
            continue
        if cls and (cur is None or cls not in cur):
            continue
        print("%-8s | %s | line %d | %s" % (
            m.group("off"), cur, i, m.group("decl").strip()))


def main():
    argv = sys.argv[1:]
    if not argv or argv[0] in ("-h", "--help"):
        print(__doc__)
        return 2

    if argv[0] == "--fields":
        rest = argv[1:]
        cls = None
        if "--class" in rest:
            k = rest.index("--class")
            cls = rest[k + 1]
            rest = rest[:k] + rest[k + 2:]
        if not rest:
            print("--fields needs a dump.cs path")
            return 2
        mode_fields(rest[0], cls)
        return 0

    context = 2
    only = None
    write_to = None
    show_reads = True
    paths = []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--context":
            context = int(argv[i + 1]); i += 2
        elif a == "--only":
            only = argv[i + 1]; i += 2
        elif a == "--write":
            write_to = argv[i + 1]; i += 2
        elif a == "--no-reads":
            show_reads = False; i += 1
        elif a.startswith("--"):
            print("unknown option: %s" % a)
            return 2
        else:
            paths.append(a); i += 1

    if not paths:
        print(__doc__)
        return 2

    files = list(iter_files(paths, only))
    if not files:
        print("no files matched", file=sys.stderr)
        return 1

    buf = []
    buf.append("# state-bit write candidates")
    buf.append("# files: %d" % len(files))
    total = 0
    off_index = {}

    for f in files:
        hits = scan_writes(f, context)
        if not hits:
            continue
        for h in hits:
            total += 1
            off = norm_off(h["off"])
            if not h["scaled"]:
                off_index.setdefault(off, []).append((f, h["line"], h["op"], h["scaled"]))
            buf.append("")
            buf.append("-- %s:%d  off=%s  %s %s  [%s]  base=%s  type=%s%s" % (
                h["file"], h["line"], off, h["op"], h["val"], h["kind"],
                h["base"], h["type"], h["note"]))
            for j, cl in h["ctx"]:
                mark = ">>" if j == h["line"] else "  "
                buf.append("%s %6d | %s" % (mark, j, cl))

    if show_reads:
        read_index = {}
        for f in files:
            r = scan_reads(f)
            for off, lns in r.items():
                read_index.setdefault(norm_off(off), []).extend((f, ln) for ln in lns)
        buf.append("")
        buf.append("# offsets: writes / reads  (a '*' after the line number = pointer-scaled store,")
        buf.append("#   i.e. `ptr + 0xNN` in units of the pointee -- NOT a byte offset)")
        for off in sorted(off_index, key=lambda x: int(x, 16) if x.startswith("0x") else 0):
            w = off_index[off]
            ws = ", ".join("%s:%d%s%s" % (os.path.basename(p), ln, op, "*" if sc else "")
                           for p, ln, op, sc in w)
            r = read_index.get(off, [])
            rs = ", ".join("%s:%d" % (os.path.basename(p), ln) for p, ln in r[:12])
            if len(r) > 12:
                rs += ", ...(+%d)" % (len(r) - 12)
            buf.append("%-8s writes: %s" % (off, ws))
            if r:
                buf.append("%-8s reads : %s" % ("", rs))

    text = "\n".join(buf) + "\n"
    if write_to:
        with io.open(write_to, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(text)
        print("wrote %s (%d candidates)" % (write_to, total))
    else:
        sys.stdout.write(text)
        print("\n# total candidates: %d (stdout only; use --write to save)" % total)
    return 0


if __name__ == "__main__":
    sys.exit(main())
