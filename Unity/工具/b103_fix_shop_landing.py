#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103：修 `商店.tsv` 那条 A 行的「落点」—— 闸要求第一个落点是【声明行】，
原来第一个是调用点（`BoosterPackOpenWindow.cs:1549` 是 `EnsureArmyTexts(...)` 的实参位置）⇒ 判失败。
用法：python -I -X utf8 d:/4/Unity/工具/b103_fix_shop_landing.py [--apply]
"""
import io, sys

PATH = "d:/4/Unity/资料/完全复刻/账/商店.tsv"
METHOD = "BoosterPack(ArmyText)"
OLD = "Shell/BoosterPackOpenWindow.cs:1549（`EnsureArmyTexts` 调用点，原 `FindArmyTexts`，同行替换）· :1698（`EnsureArmyTexts`）· :1722（`BuildArmyText`）"
NEW = "Shell/BoosterPackOpenWindow.cs:1698（EnsureArmyTexts）· :1722（BuildArmyText）· :1549（调用点 = `EnsureArmyTexts` 的实参位置，原 `FindArmyTexts` 同行替换）"


def main():
    apply = "--apply" in sys.argv
    raw = io.open(PATH, "rb").read()
    nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
    lines = raw.decode("utf-8").split(nl)
    hits = [i for i, l in enumerate(lines) if l.strip() and len(l.split("\t")) > 6
            and l.split("\t")[2].strip() == METHOD]
    print("命中 %d 条" % len(hits))
    if len(hits) != 1:
        print("⛔ 命中数不是 1，跳过")
        return
    i = hits[0]
    c = lines[i].split("\t")
    if OLD not in c[6]:
        print("⛔ 落点格与预期不符，跳过。现读：%s" % c[6][:200])
        return
    print("状态 = %s" % c[5])
    if apply:
        c[6] = c[6].replace(OLD, NEW)
        lines[i] = "\t".join(c)
        io.open(PATH, "wb").write(nl.join(lines).encode("utf-8"))
        back = io.open(PATH, "rb").read().decode("utf-8").split(nl)[i].split("\t")
        assert NEW in back[6] and len(back) == len(c), "往返校验失败"
        print("✅ 已写 · 落点数 %d" % len(back))
    else:
        print("（预演）新落点：%s" % NEW)


if __name__ == "__main__":
    main()
