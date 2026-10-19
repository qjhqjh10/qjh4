# -*- coding: utf-8 -*-
"""把【与卡面正文冲突】的那一族中文**暂缓换**回我们的旧值（只改这些键的 ZH 那一格）。

**为什么**：`Card_Trait/*`（关键词显示名）与 `Armies/*`（阵营名）换上官方词之后，**卡面 `descZh` 仍是我们自译的**
⇒ 同一屏上会出现「关键词章 = 官方词、卡面正文 = 我们的旧词」（实测 33 个键的旧值在 `descZh`/`nameZh` 里出现：
`Card_Trait/armour` 护甲 ×105 · `talent` 天赋 ×100 · `stealth` 潜行 ×50 · `ephemeral` 临时 ×43 · `shield` 护盾 ×38 …）。
⇒ **这两族与卡面文字一起换**（那是 `A1762`/`A1763` 那一批的活），今天就先保持内部一致。
⛔ 官方值**没丢**（`数据/游戏数据/官方本地化_en+zh-CN.json` 里都在），随时可以再换。

行尾：`Loc.cs` 是 **LF**（脚本会断言）。

**落盘位置**：`d:/4/Unity/工具/_loc_official/`（2026-10-14 第二十七会话从 `_tmp_a1070/` 搬进来 —— `_tmp_*` 有被清理的风险，而这几个是**判据脚本**）。
"""
import io
import re
import sys

OLD = r"d:/4/Unity/工具/_loc_official/Loc_换官方前.cs.bak"
NEW = r"d:/4/Unity/MyGame/Assets/CardPresentation/Core/Loc.cs"
POOL = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
PAT = re.compile(r'\{\s*"([^"]+)"\s*,\s*new Entry\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*\)\s*\}')
ROW = re.compile(
    r'^(?P<pre>[ \t]*\{[ \t]*"(?P<key>(?:[^"\\]|\\.)*)"[ \t]*,[ \t]*new Entry\([ \t]*)"'
    r'(?P<zh>(?:[^"\\]|\\.)*)"(?P<mid>[ \t]*,[ \t]*")'
    r'(?P<en>(?:[^"\\]|\\.)*)"(?P<tail>[ \t]*\)[ \t]*\}[ \t]*,?[ \t]*(?P<cmt>//.*)?)$')


def main():
    apply_ = "--apply" in sys.argv
    old_s = io.open(OLD, encoding="utf-8").read()
    old = {m.group(1): m.group(2) for m in PAT.finditer(old_s)}
    raw = io.open(NEW, "rb").read()
    assert raw.count(b"\r\n") == 0, "Loc.cs 不是纯 LF —— 停下"
    text = raw.decode("utf-8")

    import json
    cards = json.load(io.open(POOL, encoding="utf-8"))["cards"]
    blob = "\n".join((c.get("descZh") or "") + "\n" + (c.get("nameZh") or "") for c in cards)

    # 要暂缓的键 = 「现在的中文 ≠ 旧值」且「旧值在卡面文字里出现过」
    cur = {m.group(1): m.group(2) for m in PAT.finditer(text)}
    todo = []
    for k, oldv in old.items():
        curv = cur.get(k)
        if curv is None or curv == oldv:
            continue
        if len(oldv) < 2 or "{" in oldv:
            continue
        if blob.count(oldv):
            todo.append((k, oldv, curv))
    print("要暂缓换回的键：%d 个" % len(todo))
    for k, o, n in sorted(todo):
        print("  %-46s %-16s <- %s" % (k, o, n))
    if not apply_:
        print("（--check，没写文件）")
        return 0

    lines = text.split("\n")
    done = set()
    for i, line in enumerate(lines):
        m = ROW.match(line)
        if not m:
            continue
        k = m.group("key")
        for kk, o, n in todo:
            if kk != k:
                continue
            # ⚠️ `mid` 里**已经含 en 的开引号**（`[ \t]*,[ \t]*"`）⇒ 这里**不能再补一个 `"`**。
            lines[i] = (m.group("pre") + '"' + o + '"' + m.group("mid") + m.group("en")
                        + '"' + m.group("tail") + (m.group("cmt") or ""))
            done.add(kk)
    miss = [k for k, _, _ in todo if k not in done]
    if miss:
        sys.exit("❌ 这些键没在文件里定位到（行形态不符）：%s" % miss)
    io.open(NEW, "wb").write("\n".join(lines).encode("utf-8"))
    print("写回 %s（改回 %d 个键）" % (NEW, len(done)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
