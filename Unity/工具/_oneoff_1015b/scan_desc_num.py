# -*- coding: utf-8 -*-
"""全池对账：`desc`（英文，来自 PnP 印刷版）vs `descZh`（中文）的**数字**是否一致。
归一化：GUID 卡引用 `$$...$$` → [CARD]；圈号 ①…⑩ → 1…10。
用途：抓「同一条效果中英数值打架」这一类（铁律 10 第 6 条：PnP 是内容的尺子）。
只读，不落盘。"""
import json, io, re, sys

POOL = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
CIRCLES = "①②③④⑤⑥⑦⑧⑨⑩"


def norm(s):
    s = re.sub(r"\$\$[0-9a-fA-F]{32}\$\$", "[CARD]", s)
    s = re.sub(r"\$\$[A-Za-z0-9_]+\$\$", "[CARD]", s)
    for i, ch in enumerate(CIRCLES):
        s = s.replace(ch, str(i + 1))
    return s


def main():
    pool = json.load(io.open(POOL, encoding="utf-8"))
    cs = pool["cards"] if isinstance(pool, dict) and "cards" in pool else pool
    num = re.compile(r"\d+")
    bad = []
    for c in cs:
        d = c.get("desc") or ""
        z = c.get("descZh") or ""
        if not d or not z:
            continue
        nd = sorted(int(x) for x in num.findall(norm(d)))
        nz = sorted(int(x) for x in num.findall(norm(z)))
        if nd != nz:
            bad.append((str(c.get("id")), c.get("name"), nd, nz, d, z))
    print("卡池 %d 张 · 数字不一致 %d 张" % (len(cs), len(bad)))
    for i, n, nd, nz, d, z in bad:
        print(" - %-34s %s" % (i, n))
        print("     EN %s  %r" % (nd, d[:95]))
        print("     ZH %s  %r" % (nz, z[:95]))


if __name__ == "__main__":
    main()
