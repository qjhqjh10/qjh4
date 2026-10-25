#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""把账里「我们的落点」列中的**裸文件名**补成相对 `MyGame/Assets` 的完整路径。

## 为什么
`工具/check_ledger_points.py` 的根是 `MyGame/Assets`，所以：
- `RuleEngine/Core/RuleCore.cs` ✅
- `RuleCore.cs` ❌ —— 报「文件不存在（试过 原样 / Assets / Assets/CardPresentation / Unity）」

代理写账时常只写裸文件名（有时是第二个路径段，如
`RuleEngine/Core/IAiScorer.cs（IAiScorer）… · Data/SimpleAI.cs:1356`）。

## 规则
只改**第 7 列（`我们的落点`）**；把**前面没有 `/` 的**这些文件名补全：
    RuleCore.cs · WhenEvent.cs · EffectResolver.cs · CardDef.cs · UnitState.cs
    → RuleEngine/Core/<名字>
    SimpleAI.cs · Data/SimpleAI.cs → RuleEngine/Data/SimpleAI.cs
    IAiScorer.cs → RuleEngine/Core/IAiScorer.cs
⚠️ 已经带路径的（前面有 `/`）不动。

## 用法
    python -I -X utf8 d:/4/Unity/工具/fix_ledger_bare_paths.py --dry
    python -I -X utf8 d:/4/Unity/工具/fix_ledger_bare_paths.py
"""
import io
import os
import re
import sys

LEDGER = r"d:/4/Unity/资料/完全复刻/账"

CORE = ["RuleCore.cs", "WhenEvent.cs", "EffectResolver.cs", "CardDef.cs", "UnitState.cs"]
RULES = [(n, "RuleEngine/Core/" + n) for n in CORE]
RULES += [("Data/SimpleAI.cs", "RuleEngine/Data/SimpleAI.cs"),
          ("SimpleAI.cs", "RuleEngine/Data/SimpleAI.cs"),
          ("IAiScorer.cs", "RuleEngine/Core/IAiScorer.cs")]
# 长的先替换（Data/SimpleAI.cs 先于 SimpleAI.cs）
RULES.sort(key=lambda t: -len(t[0]))

COLP = 6  # 我们的落点


def read_raw(p):
    b = io.open(p, "rb").read()
    eol = "\r\n" if (b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n")) else "\n"
    return b.decode("utf-8-sig").split(eol), eol


def fix_cell(cell):
    for bare, full in RULES:
        # 前面不是 / 也不是标识符字符 ⇒ 是裸的
        cell = re.sub(r"(?<![\w/.])" + re.escape(bare), full, cell)
    return cell


def main():
    dry = "--dry" in sys.argv
    total = 0
    for fn in sorted(os.listdir(LEDGER)):
        if not fn.endswith(".tsv") or fn.startswith("_"):
            continue
        p = os.path.join(LEDGER, fn)
        lines, eol = read_raw(p)
        n = 0
        for i, l in enumerate(lines):
            f = l.split("\t")
            if len(f) <= COLP:
                continue
            new = fix_cell(f[COLP])
            if new != f[COLP]:
                f[COLP] = new
                lines[i] = "\t".join(f)
                n += 1
        if n:
            total += n
            if dry:
                print("  [dry] %s：会改 %d 行" % (fn, n))
            else:
                io.open(p, "wb").write(eol.join(lines).encode("utf-8"))
                print("  ✅ %s：改 %d 行" % (fn, n))
    print("\n合计 %d 行%s" % (total, "（dry-run，未写盘）" if dry else ""))


if __name__ == "__main__":
    main()
