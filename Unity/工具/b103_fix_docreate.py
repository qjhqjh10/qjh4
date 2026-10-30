#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103：修 `ResolveCreateHandCard` 的落点首段（原是指向 `case "hand"` 的散文，闸判「注释行」）。
新首段 = `RuleEngine/Core/EffectResolver.cs:4075（DoCreate）`（现读：该 case 块挂在 `DoCreate` 里）。
用法：python -I -X utf8 d:/4/Unity/工具/b103_fix_docreate.py [--apply]
"""
import io, sys

PATH = "d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
METHOD = "ResolveCreateHandCard(BattleAction battleAction)"
OLD = 'RuleEngine/Core/EffectResolver.cs:4176（`case "hand"/"enemyhand"` 的 sabotage 支：广播 → 中央展示事件 → 再进手，R5 顺序已改）'
NEW = 'RuleEngine/Core/EffectResolver.cs:4075（DoCreate）'


def main():
    apply = "--apply" in sys.argv
    raw = io.open(PATH, "rb").read()
    nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
    lines = raw.decode("utf-8").split(nl)
    hits = [i for i, l in enumerate(lines) if l.strip() and len(l.split("\t")) > 6
            and l.split("\t")[2].strip() == METHOD]
    print("命中 %d" % len(hits))
    if len(hits) != 1:
        return
    i = hits[0]
    c = lines[i].split("\t")
    if OLD not in c[6]:
        print("⛔ 首段与预期不符：%s" % c[6][:150])
        return
    c[6] = c[6].replace(OLD, NEW, 1)
    lines[i] = "\t".join(c)
    print("新首段：%s" % c[6][:120])
    if apply:
        io.open(PATH, "wb").write(nl.join(lines).encode("utf-8"))
        print("✅ 已写")


if __name__ == "__main__":
    main()
