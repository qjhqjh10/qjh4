#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103：给指定账行的「备注」列追加一段说明（不改状态、不改别的列）。
用法：python -I -X utf8 d:/4/Unity/工具/b103_ledger_note.py [--apply]
"""
import io, sys

LEDGER = "d:/4/Unity/资料/完全复刻/账/"
TARGETS = [
    ("日常_奖励.tsv", "EventBadgeController", "Awake()",
     "【B103 2026-10-30 订正（铁律 5）】上面那两处「维持 B」是 **D 之前的中间结论、已过期** —— 以本行末句「B98_H5 2026-10-29：判 D」为准，状态列 **D 正确**。"
     "另：AUD08 复核确认我们这边的 `Shell/EventBadgeController.cs` 只被 Editor 自检引用、**运行时未接线** ⇒ 属「休眠件」，按 `⏸ 原版不可达` 口径处理（见 `判据/AUD08_we_do_more.md` §二 #3）。"),
]

def main():
    apply = "--apply" in sys.argv
    for fn, cls, method, note in TARGETS:
        path = LEDGER + fn
        raw = io.open(path, "rb").read()
        nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
        lines = raw.decode("utf-8").split(nl)
        hits = [i for i, l in enumerate(lines)
                if l.strip() and len(l.split("\t")) > 10
                and l.split("\t")[1].strip() == cls and l.split("\t")[2].strip() == method]
        print("== %s | %s.%s | 命中 %d" % (fn, cls, method, len(hits)))
        if len(hits) != 1:
            print("   ⛔ 命中数不是 1，跳过")
            continue
        i = hits[0]
        c = lines[i].split("\t")
        print("   状态=%s 列数=%d" % (c[5], len(c)))
        if apply:
            c[10] = c[10] + "｜" + note
            lines[i] = "\t".join(c)
            io.open(path, "wb").write(nl.join(lines).encode("utf-8"))
            back = io.open(path, "rb").read().decode("utf-8").split(nl)[i].split("\t")
            assert len(back) == len(c) and back[5] == c[5], "往返校验失败"
            print("   ✅ 已写 · 列数 %d" % len(back))
    print("完成" if apply else "（预演）")

if __name__ == "__main__":
    main()
