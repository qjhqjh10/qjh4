#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103 v2：修 A 行落点行号漂移（按 `原版类` + `方法` 定位，替换落点格里第一个 `<tail>:OLD` → `<tail>:NEW`）。
用法：python -I -X utf8 d:/4/Unity/工具/b103_fix_landings.py [--apply]
"""
import io, sys

LEDGER = "d:/4/Unity/资料/完全复刻/账/"
# (账文件, 原版类（精确）, 方法子串（空=不限）, 落点文件尾段, 旧行, 新行)
JOBS = [
    ("菜单_外壳.tsv", "CardDrawer", "ToggleBanned", "DeckRuntime.cs", 9537, 9538),
    ("日常_奖励.tsv", "RewardWindow", "FixLayoutSize()", "RewardWindow.cs", 1047, 1051),
    ("日常_奖励.tsv", "RewardWindow", "OnDisable()", "RewardWindow.cs", 1047, 1051),
    ("日常_奖励.tsv", "RewardWindow", "OnEnable()", "RewardWindow.cs", 1047, 1051),
]


def main():
    apply = "--apply" in sys.argv
    for fn, cls, msub, tail, old, new in JOBS:
        path = LEDGER + fn
        raw = io.open(path, "rb").read()
        nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
        lines = raw.decode("utf-8").split(nl)
        hit = 0
        for i, l in enumerate(lines):
            if not l.strip():
                continue
            c = l.split("\t")
            if len(c) < 7 or c[1].strip() != cls:
                continue
            if msub and msub not in c[2]:
                continue
            needle = tail + ":%d" % old
            if needle not in c[6]:
                print("  · 跳过（落点里没有 %s）：%s" % (needle, c[2][:40]))
                continue
            c[6] = c[6].replace(needle, tail + ":%d" % new, 1)
            lines[i] = "\t".join(c)
            hit += 1
            print("  %s | %s.%s | %d→%d" % (fn, cls, c[2][:40], old, new))
        if apply and hit:
            io.open(path, "wb").write(nl.join(lines).encode("utf-8"))
            back = io.open(path, "rb").read().decode("utf-8").split(nl)
            assert len(back) == len(lines), "行数变了"
            print("  ✅ 已写 %s（%d 条）" % (fn, hit))
        elif not hit:
            print("  ⛔ %s：0 命中" % fn)
    print("完成" if apply else "（预演）")


if __name__ == "__main__":
    main()
