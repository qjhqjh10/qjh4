# -*- coding: utf-8 -*-
"""W4：把 `Core/Loc.cs` 里**还有旧 EN 的**那些条目换成官方 EN（`A1794` 那一批的另一半）。

口径：
  · 只动 **EN 列**（ZH 列本轮已在别处处理过；`Card_Trait`/`Armies`/`Card_Race`/`MenuDeck…Minions`
    那几族的 ZH **故意留着**，理由见 `Loc.cs` 文件头那 33 条「暂缓换回」）。
  · `MenuDeck/CantImportDeck` **跳过**（它连 ZH 也要换、且要同时改调用点 ⇒ 不在本笔白名单）。
  · 逐条先 assert「那条键的 EN 现在逐字等于旧值」，再换（⛔ 不写行号锚点）。
"""
import io
import os
import re
import sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/_loc_official/w4_fix_en.py [--write]\n  说明：W4：把 `Core/Loc.cs` 里**还有旧 EN 的**那些条目换成官方 EN（`A1794` 那一批的另一半）。\n  选项：--write（脚本内按字符串匹配；一个都不带 = 默认模式）')
    _wf_help_sys.exit(0)

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import w4_lib as L  # noqa: E402

DO = "--write" in sys.argv
SKIP = {"MenuDeck/CantImportDeck"}


def find_entry_end(s, i):
    """从 `"key"` 的位置起，返回 (第二个字符串字面量的起止, 其内容)。"""
    j = s.find("new Entry(", i)
    if j < 0:
        return None
    k = s.find("(", j)
    depth, p = 0, k
    lits = []
    while p < len(s):
        c = s[p]
        if c == '"':
            q = p + 1
            while q < len(s):
                if s[q] == "\\":
                    q += 2
                    continue
                if s[q] == '"':
                    break
                q += 1
            lits.append((p, q + 1, s[p + 1:q]))
            if len(lits) == 2:
                return lits[1]
            p = q + 1
            continue
        if c == ")" and depth == 0:
            return None
        if c in "(":
            depth += 1
        elif c == ")":
            depth -= 1
        p += 1
    return None


def esc(s):
    return s.replace("\\", "\\\\").replace('"', '\\"')


def main():
    rows = [l.rstrip("\n").split("\t") for l in
            io.open(r"d:/tmp/w4_en_diffs.tsv", encoding="utf-8") if l.count("\t") == 2]
    p = L.LOC
    s = io.open(p, encoding="utf-8").read()
    n = 0
    for k, old, new in sorted(rows):
        if k in SKIP:
            print("  ⏭ 跳过 %s（要同时改调用点）" % k)
            continue
        i = s.find('"%s"' % k)
        if i < 0:
            raise SystemExit("❌ 找不到键 %s" % k)
        got = find_entry_end(s, i)
        if got is None:
            raise SystemExit("❌ 解析不出 %s 的 Entry" % k)
        a, b, lit = got
        if L.unesc(lit) != old:
            raise SystemExit("❌ %s 的 EN 与预期不同：%r vs %r" % (k, lit, old))
        s = s[:a] + '"' + esc(new) + '"' + s[b:]
        n += 1
    print("改了 %d 条 EN" % n)
    if DO:
        io.open(p, "wb").write(s.encode("utf-8"))
        print("落盘 %s" % p)
    else:
        print("（干跑）")


if __name__ == "__main__":
    main()
