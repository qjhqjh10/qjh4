#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""_apply_card_fix.py —— 按**编译器给的行列**给「该是卡模板的地方」补 `.Card`

背景：`PlayerState.Deck/Hand/Discard` 从 `List<CardDef>` 换成了 `List<CardInstance>`，
一次暴露出 120 个去重错误点，形状高度一致：
  · CS1503 「cannot convert from CardInstance to CardDef」= 把实例当模板传给了某个函数
  · CS1061 「CardInstance does not contain a definition for X」= 在实例上读模板的成员
  · CS0029 「cannot implicitly convert」= 赋值
三者的修法**都是**在**报错位置那个表达式**后面补 `.Card`。

用法：
  python _apply_card_fix.py <编译器日志> <工程根> --dry      # 只看前 N 条对不对称
  python _apply_card_fix.py <编译器日志> <工程根> --apply    # 真改（按文件从后往前改，位置不串）
"""
import collections
import io
import re
import sys

PAT = re.compile(r"^(Assets[^(]*)\((\d+),(\d+)\): error (CS\d+)")
WANT = ("CS1503", "CS1061", "CS0029")
OPEN = "([{"
CLOSE = ")]}"
STOP = ",;"


def expr_end(s, i):
    """从 i 开始找一个「表达式」的结尾（括号配平；深度 0 遇到 , ; ) ] 就停）。"""
    depth = 0
    n = len(s)
    while i < n:
        c = s[i]
        if c in OPEN:
            depth += 1
        elif c in CLOSE:
            if depth == 0:
                break
            depth -= 1
        elif depth == 0 and c in STOP:
            break
    # 空白：只有当后面不是 `.` 或 `(` 时才算结束
        elif depth == 0 and c == " ":
            j = i
            while j < n and s[j] == " ":
                j += 1
            if j >= n or s[j] not in ".(":
                break
        i += 1
    return i


def main():
    log, proj, mode = sys.argv[1], sys.argv[2], (sys.argv[3] if len(sys.argv) > 3 else "--dry")
    sites = collections.defaultdict(set)          # file -> {(line, col)}
    for raw in io.open(log, encoding="utf-8", errors="ignore"):
        line = raw.replace("\\", "/")
        m = PAT.match(line)
        if not m:
            continue
        if m.group(4) not in WANT:
            continue
        sites[m.group(1)].add((int(m.group(2)), int(m.group(3))))

    total = sum(len(v) for v in sites.values())
    print("去重错误点 %d 个，分布在 %d 个文件" % (total, len(sites)))

    shown = 0
    for f in sorted(sites):
        for ln, col in sorted(sites[f], reverse=True):
            txt = io.open(proj + "/" + f, encoding="utf-8", errors="ignore").read().split("\n")
            if ln - 1 >= len(txt):
                continue
            s = txt[ln - 1]
            i = col - 1
            j = expr_end(s, i)
            before, expr, after = s[:i], s[i:j], s[j:]
            if mode == "--dry":
                if shown < 14:
                    print("  %-46s L%d C%d" % (f.split("/")[-1], ln, col))
                    print("     原：%s" % s.strip()[:110])
                    print("     改：%s" % (before + expr + ".Card" + after).strip()[:110])
                    shown += 1
            else:
                txt[ln - 1] = before + expr + ".Card" + after
                io.open(proj + "/" + f, "w", encoding="utf-8", newline="").write("\n".join(txt))
    if mode != "--dry":
        print("已写入 %d 个文件" % len(sites))


if __name__ == "__main__":
    main()
