# -*- coding: utf-8 -*-
"""落账前置：按【行尾文案】验证 R2 草案给的每一个锚点是否唯一命中。只读。"""
import io, sys

TASK = r"d:/4/Unity/资料/任务_卡组卡牌战斗.md"
PROJ = r"d:/4/项目任务.md"

# (文件, 锚点行尾片段, 用途)
ANCHORS = [
    (TASK, "§对账结果。 |", "任务 §一 表末（B1-3/4/5/11/12/14/27 插在这之后）"),
    (TASK, "` §二·丁 |", "任务 §二 表末（B1-1）"),
    (TASK, "1800`。 |", "任务 §三 表末（B1-8/9/13/15/17/18/19/20/21/22/30/32/33/34/38/45）"),
    (TASK, "录.md`。 |", "任务 §四 表末（B1-29/31）"),
    (PROJ, "要重算**。 |", "项目 §29·b 主表表末（B1-28/39）"),
    (PROJ, "候选见左）。 |", "项目 §29·c 表末（B1-16/26/35）"),
    (PROJ, "对账结论）。 |", "项目 §29·d 表末（B1-10）"),
]


def load(p):
    b = io.open(p, "rb").read()
    return b, b.split(b"\r\n")


def main():
    ok = True
    for path, tail, use in ANCHORS:
        b, lines = load(path)
        t = tail.encode("utf-8")
        hits = [i for i, L in enumerate(lines) if L.endswith(t)]
        flag = "OK " if len(hits) == 1 else "!! "
        if len(hits) != 1:
            ok = False
        print("%s%-46s hits=%d  %s" % (flag, use, len(hits), hits[:6]))
        if len(hits) == 1:
            L = lines[hits[0]].decode("utf-8")
            print("      行 %d · 竖线 %d · 末 60 字: …%s" % (hits[0] + 1, L.count("|"), L[-60:]))
    # 行内追加的锚点：按编号定位
    print()
    for name, path in [("任务", TASK), ("项目", PROJ)]:
        b, lines = load(path)
        for num in ["A1777", "A1770", "A1945", "A1763", "A1982", "A1762", "A1088", "A1431",
                    "A1694", "A1335", "A1616", "A1748", "A985", "A1155", "A1217", "A1590",
                    "A1595", "A1772", "A1759", "A1752", "A1755", "A1757", "A1758", "A1070",
                    "A1033", "A1735", "A577"]:
            hits = [i for i, L in enumerate(lines) if ("**`" + num + "`**") in L.decode("utf-8", "replace")]
            if hits:
                print("%s %-8s 行 %s" % (name, num, [h + 1 for h in hits]))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
