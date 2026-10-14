# -*- coding: utf-8 -*-
"""_apply_merge_28.py -- 把 第二十七会话 的 57 条新账并进两张待办正本（第二十八会话）。

只干一件事：读 TSV，**按锚点整行插入**。安全纪律（本仓踩过的坑，逐条照做）：
  · 先算进变量、校验无误再写（`io.open(p,'wb').write(<表达式>)` 会先截断再求值，
    表达式一抛异常文件就空掉 —— 见 CLAUDE.md §二）；
  · **锚点必须整行、且全文恰好出现 1 次**（拿行首片段当锚点会把上一行丢了行首）；
  · 行尾照抄原文件（本项目实测两份正本都是 **LF**），⛔ 别一刀切；
  · 改完打印 `git diff --numstat` 供人工对一眼（数字接近文件总行数 = 行尾被翻了）。

用法:
  python -I -X utf8 d:/4/Unity/工具/_apply_merge_28.py [--dry]

判据来源：Unity/资料/普查产出_第二十八会话/并账_插入指令.tsv（+ 同目录 _说明.md）
"""
import io, os, sys, shutil, subprocess, argparse

TSV = "d:/4/Unity/资料/普查产出_第二十八会话/并账_插入指令.tsv"
ROOT_UNITY = "d:/4/Unity/"
ROOT_PROJ = "d:/4/"


def resolve(target):
    """TSV 的目标文件列 -> 绝对路径。
    ⚠️ 该列可能写成 `` `项目任务.md` §29·b `` 这种「路径 + 节名」的形态（实测踩过）
    ⇒ 先取反引号里那个以 .md 结尾的片段，取不到再退回第一个空白分段。"""
    t = target.strip()
    import re
    for cand in re.findall(r"`([^`]+)`", t):
        if cand.endswith(".md"):
            t = cand
            break
    else:
        t = t.split()[0].strip("`")
    if t.startswith("资料/") or t.startswith("资源/"):
        return ROOT_UNITY + t
    if t.startswith("Unity/"):
        return "d:/4/" + t
    return ROOT_PROJ + t


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry", action="store_true")
    ap.add_argument("--tsv", default=TSV, help="插入指令 TSV（默认 = 第二十七会话那批）")
    a = ap.parse_args()

    raw = io.open(a.tsv, "rb").read().decode("utf-8")
    lines = [l for l in raw.split("\r\n") if l != ""]
    header, rows = lines[0], lines[1:]
    print("== TSV: 表头 = %s" % header)
    print("== 数据行 %d" % len(rows))

    # 1) 解析 + 分组（按文件、保持 TSV 顺序）
    plan = {}          # path -> [(编号, anchor, insert)]
    for i, ln in enumerate(rows, 1):
        c = ln.split("\t")
        if len(c) != 4:
            print("!! 第 %d 行列数 = %d（应为 4）⇒ 停手" % (i + 1, len(c)))
            return 2
        num, tgt, anchor, ins = (x.strip() for x in c)
        p = resolve(tgt)
        plan.setdefault(p, []).append((num, anchor, ins))

    # 2) 逐文件校验 + 构造新内容（**先算进变量**）
    results = []
    for p, items in plan.items():
        if not os.path.isfile(p):
            print("!! 目标文件不存在: %s ⇒ 停手" % p)
            return 2
        b = io.open(p, "rb").read()
        crlf, lf = b.count(b"\r\n"), b.count(b"\n")
        eol = b"\r\n" if (crlf == lf and crlf > 0) else b"\n"
        kind = "CRLF" if eol == b"\r\n" else ("LF" if crlf == 0 else "MIXED")
        text = b.decode("utf-8")
        # 保留原行尾：先算行列表（不靠 splitlines，免得混行尾被吞）
        flines = text.split("\n")
        if flines and flines[-1] == "":
            flines = flines[:-1]
        for idx, l in enumerate(flines):
            if l.endswith("\r"):
                flines[idx] = l[:-1]

        # 逐条：锚点唯一性
        out = []
        pending = {}      # anchor -> [insert...]
        for num, anchor, ins in items:
            n = sum(1 for l in flines if l == anchor)
            if n != 1:
                print("!! 锚点不唯一/找不到（出现 %d 次）编号=%s 文件=%s" % (n, num, p))
                print("   锚点 = %s" % anchor[:120])
                return 2
            pending.setdefault(anchor, []).append((num, ins))
            # 插入内容自检：表格行必须首尾都是 |
            if ins.startswith("|") != ins.endswith("|"):
                print("!! 插入行首尾 | 不配对：%s" % num)
                return 2
        for l in flines:
            out.append(l)
            for num, ins in pending.get(l, []):
                out.append(ins)
        new = ("\n".join(out) + "\n").encode("utf-8")
        if eol == b"\r\n":
            new = new.replace(b"\n", b"\r\n")
        results.append((p, b, new, kind, len(flines), len(out), items))

    # 3) 写盘（先备份）
    for p, old, new, kind, n0, n1, items in results:
        print("== %s  行尾=%s  %d -> %d 行（+%d）  插入 %d 条"
              % (p, kind, n0, n1, n1 - n0, len(items)))
        if a.dry:
            continue
        bak = p + ".bak_s28"
        if not os.path.exists(bak):
            shutil.copy2(p, bak)
            print("   备份 -> %s" % bak)
        io.open(p, "wb").write(new)     # new 已算好，不会再抛

    if a.dry:
        print("== --dry：一个字节都没写 ==")
        return 0

    # 4) 回读校验：插入的行真在盘上？
    print("\n== 回读校验 ==")
    for p, old, new, kind, n0, n1, items in results:
        cur = io.open(p, "rb").read()
        crlf, lf = cur.count(b"\r\n"), cur.count(b"\n")
        ok_eol = (crlf == lf and crlf > 0) if kind == "CRLF" else (crlf == 0)
        miss = [num for num, _, ins in items if ins.encode("utf-8") not in cur]
        print("   %s: 行尾仍=%s? %s · 缺行 %d 条 %s"
              % (os.path.basename(p), kind, ok_eol, len(miss), miss if miss else ""))
    out = subprocess.run(["git", "-C", "d:/4", "diff", "--numstat"],
                         capture_output=True, text=True)
    print("\n== git diff --numstat ==\n" + (out.stdout or out.stderr))
    return 0


if __name__ == "__main__":
    sys.exit(main())
