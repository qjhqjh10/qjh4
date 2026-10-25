#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""检查 `Core/Loc.cs` 的本地化词条表有没有重复键。

为什么需要它（2026-10-25 真踩过）：
  `Loc` 的词条表是**字典初始化器**，同一个键出现两次 ⇒ 运行时抛
  `ArgumentException: An item with the same key has already been added`
  ⇒ **`Loc` 静态构造失败 ⇒ 任何走本地化的路径全部崩**。
  而 **`typecheck` 完全通过**（编译合法），只有真正跑起来才现形
  （当时是 `CardFaceProbe.Run` 当场 TypeInitializationException，一张图没渲出来）。

用法：python -I -X utf8 d:/4/Unity/工具/check_loc_dupes.py
退出码：0 = 无重复；1 = 有重复（并列出）。
"""
import collections
import io
import re
import sys

LOC = r"D:/4/Unity/MyGame/Assets/CardPresentation/Core/Loc.cs"
PAT = re.compile(r'\{\s*"([^"]+)"\s*,\s*new Entry\(')


def main():
    s = io.open(LOC, "r", encoding="utf-8").read()
    keys = PAT.findall(s)
    cnt = collections.Counter(keys)
    dup = sorted(k for k, v in cnt.items() if v > 1)
    print("词条 %d 条 / 唯一键 %d 个 / 重复键 %d 个" % (len(keys), len(cnt), len(dup)))
    for k in dup:
        # 打出每个重复键所在行号，方便直接改
        lines = [i for i, ln in enumerate(s.splitlines(), 1)
                 if re.search(r'\{\s*"%s"\s*,\s*new Entry\(' % re.escape(k), ln)]
        print("  ✗ %-45s 出现在行 %s" % (k, lines))
    if dup:
        print("\n🔴 有重复键：`Loc` 静态构造会抛异常，全工程走本地化的路径都会崩。")
        print("   修法：删掉其中一条（保留带出处注释的那条），或改键名。")
        return 1
    print("✅ 无重复键。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
