# -*- coding: utf-8 -*-
"""收尾 · 核销：给 `资料/任务_卡组卡牌战斗.md` 里【本会话已落地】的账加 ✅ 标记。

判据 = `资料/普查产出_第三十一会话/AO_收尾盘点.md` 的**清单 A**（逐条带出处）。
纪律：① 只改**行首那个标记**（保持 3 列结构，⛔ 不动行内正文）；② 每处 `assert count == 1`；
      ③ 纯 LF 判据 = `CRLF == 0`（本文件现读纯 LF）；④ 用 `wb` 字节级写。
"""
import io
import sys

P = "d:/4/Unity/资料/任务_卡组卡牌战斗.md"
TAG = "  ✅ 2026-10-15 第三十一会话已落地"

# 行首锚点 → 新行首（只换标记）
FULL = ["A1088", "A1431", "A1694", "A1217", "A1595", "A1748", "A1155",
        "A1616", "A1752", "A1755", "A1757", "A1758",
        "A1763", "A1770", "A1773", "A1776", "A1778", "A1782", "A1783", "A1799", "A1800"]
PARTIAL = {"A1335": "①", "A1590": "③"}


def main():
    raw = io.open(P, "rb").read()
    assert raw.count(b"\r\n") == 0, "本文件应是纯 LF，实测 CRLF=%d ⇒ 停手" % raw.count(b"\r\n")
    s = raw.decode("utf-8")
    out = s
    n = 0

    for a in FULL:
        for old in ('| 🆕 **`%s`** |' % a, '| ✅ **`%s`** |' % a):
            if out.count(old) == 1:
                if old.startswith("| 🆕"):
                    out = out.replace(old, '| ✅ **`%s`**%s |' % (a, TAG), 1)
                    n += 1
                break
        else:
            raise AssertionError("锚点找不到或不唯一：%s" % a)

    for a, half in PARTIAL.items():
        old = '| 🆕 **`%s`** |' % a
        assert out.count(old) == 1, "锚点不唯一：%s" % a
        out = out.replace(old, '| ✅ **`%s`** | 🔴 **%s 已落地（2026-10-15 第三十一会话）** —— 其余子项仍开着，见行内； |' % (a, half), 1)
        n += 1

    # `A985⑥ 尾巴` 那行行首没有表情标记
    old = '| `A985⑥` 尾巴 |'
    assert out.count(old) == 1, "A985⑥ 尾巴 锚点不唯一"
    out = out.replace(old, '| ✅ `A985⑥` 尾巴%s |' % TAG, 1)
    n += 1

    ob = out.encode("utf-8")
    assert ob.count(b"\r\n") == 0, "写入后行尾被翻"
    assert len(ob) > len(raw), "总长没增加 ⇒ 不对劲"
    with io.open(P, "wb") as f:
        f.write(ob)

    back = io.open(P, "rb").read().decode("utf-8")
    assert back == out
    print("OK · 核销 %d 处 · 字节 %d -> %d" % (n, len(raw), len(ob)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
