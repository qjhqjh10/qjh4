# -*- coding: utf-8 -*-
"""把 `Loc.cs` 的中文列换成**原版官方 zh-CN**（默认 --check 预演，`--apply` 才写）。

判据 = `数据/游戏数据/官方本地化_en+zh-CN.json`（原版 I2 词条表，见 `工具/gen_official_loc.py`）。
⚠️ **只改 ZH 那一格**，EN 列一个字不动（EN 是原版 prefab 的 TMP `m_text` 原文，另一条线）。
⚠️ `Loc.cs` 是 **LF**（现读 0 个 CRLF）—— 写回必须保持 LF。
⚠️ 键在官方表里**没有值**的、或**逐字相同**的 ⇒ **不动**。
⚠️ **复杂形态**（`new Entry(` 后面是字符串拼接 `"…" + "…"`）**整条跳过**并在报告里列出来（⛔ 不猜）。

用法：
  python -I -X utf8 d:/4/_tmp_a1070/loc_use_official.py --check   # 只报不改（默认）
  python -I -X utf8 d:/4/_tmp_a1070/loc_use_official.py --apply

**落盘位置**：`d:/4/Unity/工具/_loc_official/`（2026-10-14 第二十七会话从 `_tmp_a1070/` 搬进来 —— `_tmp_*` 有被清理的风险，而这几个是**判据脚本**）。
"""
import io
import json
import re
import sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n  python -I -X utf8 d:/4/_tmp_a1070/loc_use_official.py --check   # 只报不改（默认）\n  python -I -X utf8 d:/4/_tmp_a1070/loc_use_official.py --apply\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

SRC = r"d:/4/Unity/MyGame/Assets/CardPresentation/Core/Loc.cs"
LOC = r"d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json"

_STR = r'(?:[^"\\]|\\.)*'
_TAIL = r'(?P<tail>[ \t]*\)[ \t]*\}[ \t]*,?[ \t]*(?P<cmt>//.*)?)$'
# 单行： { "键", new Entry("中文", "English") },   // 注释
PAT_ONE = re.compile(
    r'^(?P<pre>[ \t]*\{[ \t]*"(?P<key>' + _STR + r')"[ \t]*,[ \t]*new Entry\([ \t]*)"'
    r'(?P<zh>' + _STR + r')"(?P<mid>[ \t]*,[ \t]*")'
    r'(?P<en>' + _STR + r')"' + _TAIL)
# 两行： { "键", new Entry("中文",\n      "English") },
PAT_TWO = re.compile(
    r'^(?P<pre>[ \t]*\{[ \t]*"(?P<key>' + _STR + r')"[ \t]*,[ \t]*new Entry\([ \t]*)"'
    r'(?P<zh>' + _STR + r')"(?P<mid>[ \t]*,[ \t]*\n[ \t]*")'
    r'(?P<en>' + _STR + r')"' + _TAIL)


def csharp_escape(s):
    """明文 → C# 字面量内部形态（只处理 Loc.cs 里真出现过的几样）。"""
    return (s.replace("\\", "\\\\").replace('"', '\\"')
             .replace("\r\n", "\\n").replace("\n", "\\n")
             .replace("\t", "\\t"))


def unescape(s):
    out, i = [], 0
    while i < len(s):
        c = s[i]
        if c == "\\" and i + 1 < len(s):
            n = s[i + 1]
            out.append({"n": "\n", "t": "\t", '"': '"', "\\": "\\"}.get(n, n))
            i += 2
        else:
            out.append(c)
            i += 1
    return "".join(out)


def main():
    apply_ = "--apply" in sys.argv
    raw = io.open(SRC, "rb").read()
    assert raw.count(b"\r\n") == 0, "Loc.cs 不是纯 LF —— 停下（本仓踩过三次行尾坑）"
    text = raw.decode("utf-8")
    terms = json.load(io.open(LOC, encoding="utf-8"))["terms"]

    hits, skipped, noloc, same, needarg = [], [], [], 0, []
    for i, line in enumerate(text.split("\n")):
        m = PAT_ONE.match(line) or PAT_TWO.match(line)
        if not m:
            continue
        key = m.group("key")
        off = terms.get(key)
        if off is None:
            noloc.append(key)
            continue
        zh_off = off if isinstance(off, str) else off[1]
        if not zh_off:
            noloc.append(key)
            continue
        old = unescape(m.group("zh"))
        if zh_off == old:
            same += 1
            continue
        # 🔴 **官方文案里带了占位符、而我们那条没有** ⇒ 调用点**没传那个参数** ⇒ 照抄会把 `{0}` 字面留在界面上。
        #    这一档**不换**，单列出来（要接的话得同时改调用点，那是另一笔活）。
        if set(re.findall(r"\{\d+\}", zh_off)) - set(re.findall(r"\{\d+\}", old)):
            needarg.append((i + 1, key, old, zh_off))
            continue
        hits.append((i + 1, key, old, zh_off, m))

    # 复杂形态（跨行 + 字符串拼接）—— 直接标出来
    for i, line in enumerate(text.split("\n")):
        if "new Entry(" in line and not PAT_ONE.match(line):
            nxt = text.split("\n")[i + 1] if i + 1 < len(text.split("\n")) else ""
            if PAT_TWO.match(line + "\n" + nxt) is None:
                skipped.append((i + 1, line.strip()[:90]))

    print(f"要改 {len(hits)} 条 · 已相同 {same} · 官方表没值 {len(noloc)}"
          f" · 复杂形态跳过 {len(skipped)} · 要改调用点的 {len(needarg)}")
    for ln, key, old, new, _m in hits:
        print(f"  :{ln:<5} {key[:54]:54s} {old[:22]!r} → {new[:22]!r}")
    if needarg:
        print("\n⚠️ 官方文案带占位符、我们那条没有 ⇒ **不换**（要改调用点才行）：")
        for ln, key, old, new in needarg:
            print(f"  :{ln}  {key}  {old!r} → {new!r}")
    if skipped:
        print("\n⛔ 复杂形态（字符串拼接 / 别的写法）—— 本次**没动**：")
        for ln, s in skipped:
            print(f"  :{ln}  {s}")

    if not apply_:
        print("\n（--check 模式，没写文件）")
        return 0

    lines = text.split("\n")
    for ln, key, old, new, m in hits:
        # ⚠️ `mid` 里**已经含 en 的开引号**（`[ \t]*,[ \t]*"`）⇒ 这里**不能再补一个 `"`**。
        #    （第一版就是多写了一个 `"` ⇒ 变成 `""Graphics"` ⇒ `CS1003` 934 条 —— 秒级类型检查当场抓住。）
        lines[ln - 1] = (m.group("pre") + '"' + csharp_escape(new) + '"'
                         + m.group("mid") + m.group("en") + '"' + m.group("tail"))
    out = "\n".join(lines)
    io.open(SRC, "wb").write(out.encode("utf-8"))
    print(f"\n写回 {SRC}（改了 {len(hits)} 行）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
