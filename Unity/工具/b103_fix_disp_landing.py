#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103：修 `DisplayEnemyCardInCenter` 落点首段（原是指向语句的散文，闸判「`)` 后接语句」）。
新首段 = `CardPresentation/Battle/BattleDriver.cs:39012（MaybeDisplayCreatedSabotageCardInCenter）`。
用法：python -I -X utf8 d:/4/Unity/工具/b103_fix_disp_landing.py [--apply]
"""
import io, sys

PATH = "d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
METHOD = "DisplayEnemyCardInCenter(CardScript card, float timeToDraw, float stepTime, bool changeVisibility)"
OLD = 'RuleEngine/Core/EffectResolver.cs:4209（发 `EvtKind.SabotageCreated`）'
NEW = 'CardPresentation/Battle/BattleDriver.cs:39012（MaybeDisplayCreatedSabotageCardInCenter）'


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
        print("⛔ 首段不符：%s" % c[6][:120]); return
    c[6] = c[6].replace(OLD, NEW, 1)
    lines[i] = "\t".join(c)
    print("新首段：%s" % c[6][:140])
    if apply:
        io.open(PATH, "wb").write(nl.join(lines).encode("utf-8"))
        print("✅ 已写")


if __name__ == "__main__":
    main()
