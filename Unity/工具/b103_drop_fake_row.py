#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103：删掉 `菜单_外壳.tsv` 里那条「非原版方法」的账行（`GameWindow.OnLanguageChanged()`）。
判据（2026-10-30 现读）：
  · `d:/2/Warpforge_code/Scripts/Assembly-CSharp/` 全库只有 `LanguageSelector.cs` 有 `OnLanguageChanged`，
    且那个体是空的（签名桩）—— **`GameWindow` 上根本没有这个方法**。
  · 该行是 `B103_R1` 按 AUD03 #94（换语言后窗内文案不刷新）立的「等价物追踪行」，不是原版方法。
  ⇒ 账的坐标系是「原版方法」⇒ 这行不该在账里；AUD03 #94 的进度记在 `判断/AUD03_reuse_paths.tsv` 与调度台日志。
删前把整行原文打出来留档，删后校验行数 −1。
用法：python -I -X utf8 d:/4/Unity/工具/b103_drop_fake_row.py [--apply]
"""
import io, sys

PATH = "d:/4/Unity/资料/完全复刻/账/菜单_外壳.tsv"
CLS = "GameWindow"
METHOD = "OnLanguageChanged()"

def main():
    apply = "--apply" in sys.argv
    raw = io.open(PATH, "rb").read()
    nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
    lines = raw.decode("utf-8").split(nl)
    hits = [i for i, l in enumerate(lines)
            if l.strip() and len(l.split("\t")) > 10
            and l.split("\t")[1].strip() == CLS and l.split("\t")[2].strip() == METHOD]
    print("命中 %d 条" % len(hits))
    if len(hits) != 1:
        return
    i = hits[0]
    print("将删除的状态 = %s" % lines[i].split("\t")[5])
    print("原文（前 300 字）：%s" % lines[i][:300])
    if apply:
        del lines[i]
        io.open(PATH, "wb").write(nl.join(lines).encode("utf-8"))
        back = io.open(PATH, "rb").read().decode("utf-8").split(nl)
        assert len(back) == len(lines), "行数不符"
        print("✅ 已删 · 现 %d 行" % (len(back) - 1))
    else:
        print("（预演）")

if __name__ == "__main__":
    main()
