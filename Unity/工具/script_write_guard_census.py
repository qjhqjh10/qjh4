# -*- coding: utf-8 -*-
"""B150_W_TOOL · `B143-SCRIPT-WRITE-GUARD` 静态普查（**纯只读**）。

口径（账 `B143-SCRIPT-WRITE-GUARD` 那一行）：**写盘类脚本不带显式动作参数时必须什么都不做**，
最好 `rc=2`。本脚本按静态证据把 `工具/gen_*.py` 分三档：

  · `GUARDED`  —— 找得到「裸跑 ⇒ 用法 + 非零退出」的结构（argparse 的显式动作 / `if not args: … return 2` /
                  `if not _ARGS` / `if len(argv) <= 1` / `not (check or write or dry)`）
  · `WEAK`     —— 有 `--write` 门（`write = '--write' in args` 之类 ⇒ 裸跑零写盘），但**裸跑 rc=0**
  · `UNGUARDED`—— 有写盘动作、且上面两种都找不到

⚠️ 静态判据，**不执行任何脚本**（那正是被守护对象）。误判率约 4/17（账里自己写的）。
产出 → 账 `B143-SCRIPT-WRITE-GUARD` 的下一批名单。

用法：
    python -I -X utf8 d:/4/Unity/工具/script_write_guard_census.py
"""
import io
import os
import re
import sys

TOOLS = r"D:/4/Unity/工具"

GUARD_PATS = [
    r'if\s+not\s+_ARGS',
    r'if\s+not\s+args\s*:',
    r'if\s+not\s+argv\s*:',
    r'if\s+len\(\s*argv\s*\)\s*<=\s*1',
    r'not\s*\(\s*check\s+or\s+write\s+or\s+dry\s*\)',
    r'argparse\.ArgumentParser',
    r'if\s+not\s*\(\s*DRY\s+or\s+CHECK\s+or\s+WRITE\s*\)',
    r'die_usage',
    r'raise SystemExit\(2\)',
]
RC_PAT = re.compile(r'(return\s+2|sys\.exit\(2\))')
WRITE_PATS = [
    r"open\([^)]*['\"]wb['\"]",
    r"_write_text\(",
    r"json\.dump\(",
    r"io\.open\([^)]*,\s*['\"]wb['\"]",
    r"write_text\(",
]


def scan(p):
    t = io.open(p, 'rb').read().decode('utf-8', 'replace')
    has_write = any(re.search(w, t) for w in WRITE_PATS)
    guard = [g for g in GUARD_PATS if re.search(g, t)]
    weak = re.search(r"write\s*=\s*['\"]--write['\"]\s+in\s+args", t) or \
        re.search(r"WRITE\s*=\s*['\"]--write['\"]\s+in", t) or re.search(r"DRY\s*=\s*not\s+WRITE", t)
    rc2 = bool(RC_PAT.search(t))
    if guard and rc2:
        return 'GUARDED', has_write, ','.join(guard)[:60]
    if weak:
        return 'WEAK', has_write, ','.join(guard)[:60]
    return 'UNGUARDED', has_write, ','.join(guard)[:60]


def main():
    files = sorted(f for f in os.listdir(TOOLS) if f.startswith('gen_') and f.endswith('.py'))
    rows = []
    for f in files:
        cls, hw, g = scan(os.path.join(TOOLS, f))
        rows.append((cls, f, hw, g))
    from collections import Counter
    c = Counter(r[0] for r in rows)
    print('扫了 %d 个 `gen_*.py`' % len(files))
    print('  GUARDED   %d' % c.get('GUARDED', 0))
    print('  WEAK      %d' % c.get('WEAK', 0))
    print('  UNGUARDED %d' % c.get('UNGUARDED', 0))
    for tag in ('UNGUARDED', 'WEAK'):
        sub = [r for r in rows if r[0] == tag and r[2]]
        print()
        print('## %s（且有写盘动作）—— %d 个' % (tag, len(sub)))
        for _c, f, _hw, g in sub:
            print('  %-42s %s' % (f, g))
    return 0


if __name__ == '__main__':
    sys.exit(main())
