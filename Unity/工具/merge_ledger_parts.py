#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""把代理的「账补丁」合并进主 TSV。

## 为什么有这个脚本
`账/<域>.tsv` 是**一个文件一个写手**（方案 §6.2）。但批次 3 有 3 个代理同时写
`战斗逻辑驱动.tsv`（E1/E2/E3），还有 E5/E6 同时写 `战斗界面_HUD.tsv`、Q1 写两份。
⇒ 约定：代理**只写** `账/_parts/B3_<代号>.tsv`（与主 TSV **完全相同的 11 列**，只放它改过的行），
由调度台（人）在同步点跑本脚本合并。

## 定位规则（这一版的重点）
补丁行的**真身靠 `(原版类, 方法)` 在【全部】主表里找**，⛔ 不靠补丁自己写的 `域`：
- **恰好 1 处命中** ⇒ 就地更新那一行，并把它的 `域` 归一成**那份主表自己的域值**。
- **命中 >1 处** ⇒ **报错跳过**（⛔ 不猜：同一个方法在多份账里出现，得人看）。
- **0 处命中** ⇒ 是**新行**，这时才用补丁的 `域` 决定追加到哪份主表（域值会做 `/`↔`_` 归一）。
  理由：账里有一份**跨域文件** `泛型与启动链_补读.tsv`（里面装着 10 个域的泛型补读行），
  按「域 → 文件名」硬映射会把它错并进 `日常_奖励.tsv` 一类。

## 用法
    python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py [--dry] [--parts DIR] [--ledger DIR]

    --dry    只报会改什么，不写盘（**先跑这个**）

## 三条硬规矩
1. **二进制读、二进制写**：本仓踩过「python 文本模式把 CRLF 翻成 LF」的坑。
   每份主表**照抄它自己的行尾**；补丁的行尾不参与决定。
2. **先算进变量，确认无误再写盘**（`io.open(p,'wb').write(<表达式>)` 会**先截断再求值**，
   表达式一抛异常文件就永久空了）。
3. **列数必须 11**、**`方法` 必须是单方法签名**：不合规就报「文件:行号」并**拒绝写盘**。

## 不做什么
- ⛔ 不动「行号重定位」（那是 `check_ledger_points.py --fix` 的事，按**符号名**重定位）。本脚本只管内容合并。
- ⛔ 不删主表里补丁没提到的行。
- ⛔ 不改 `账/_parts/` 下的任何文件。
"""
import io
import os
import re
import sys

COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]
NCOL = len(COLS)

DEFAULT_PARTS = r"d:/4/Unity/资料/完全复刻/账/_parts"
DEFAULT_LEDGER = r"d:/4/Unity/资料/完全复刻/账"

# 🔴 2026-10-26 修：原来写死 `startswith("B3_")` ⇒ 批次 4 的 `B4_*.tsv` 会被**整批静默忽略**（F6 报的）。
# 现在认 `B<数字>_<代号>.tsv` 任意批次号。
PATCH_RE = re.compile(r"^B\d+_.*\.tsv$")


def read_raw(path):
    """二进制读 + 自己切行。返回 (行列表, 行尾(str), 裸CR数)。"""
    b = io.open(path, "rb").read()
    bare_cr = b.count(b"\r") - b.count(b"\r\n")
    text = b.decode("utf-8-sig")
    if b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n"):
        return text.split("\r\n"), "\r\n", bare_cr
    return text.split("\n"), "\n", bare_cr


def looks_like_one_method(name):
    """一行必须是**一个原版方法**。⛔ 拒绝三类（本项目真踩过：代理会把总结行/数据行写进账）：
    ① 全角括号 —— 那是注记不是签名（例：`（数据类）`）
    ② 没有 `(...)` —— 例：`C1-W3 接 5 扇活动窗`
    ③ 一格塞多个方法 —— 例：`Routine()/TryTriggerAction()/CanTrigger()`
    ⚠️ **先把 `[...]` 特性段剥掉再数括号** —— 主表里真有
       `QueueAction(System.Action action, [Optional][DefaultParameterValue(0)] …)` 这种签名，
       特性自带 `(0)`；不剥就会把**合法行**误判成「一格多方法」（Q3 2026-10-26 实测撞到）。
    """
    if "（" in name or "）" in name:
        return False, "含全角括号（注记不是方法签名）"
    stripped = re.sub(r"\[[^\]]*\]", "", name)
    if stripped.count("(") == 0 or stripped.count(")") == 0:
        return False, "没有 `(...)`（不是方法签名）"
    if stripped.count("(") > 1:
        return False, "一格塞了多个方法（应为一行一方法）"
    return True, ""


def norm(s):
    return s.replace("/", "_").replace("_", "").strip()


def load_ledgers(ledger_dir):
    """返回 {文件名: {...}}，每份带 lines / eol / rows(list of fields) / domains(该文件用到的域值)。"""
    out = {}
    for fn in sorted(os.listdir(ledger_dir)):
        if not fn.endswith(".tsv") or fn.startswith("_"):
            continue
        lines, eol, bare_cr = read_raw(os.path.join(ledger_dir, fn))
        rows, bad = [], []
        for i, l in enumerate(lines):
            if not l.strip():
                continue
            f = l.split("\t")
            if len(f) != NCOL:
                bad.append((i + 1, len(f)))
            rows.append((i, f))
        domains = []
        for _i, f in rows[1:]:
            d = f[0].strip()
            if d and d not in domains:
                domains.append(d)
        out[fn] = {"lines": lines, "eol": eol, "bare_cr": bare_cr,
                   "rows": rows, "bad": bad, "domains": domains, "dirty": False}
    return out


def pick_target_file(dom_raw, ledgers):
    """新行的落点：按域值找主表。① 文件名 stem 与域最匹配的优先 ② 否则唯一含该域的文件。"""
    d = dom_raw.strip()
    cand = [fn for fn, L in ledgers.items() if d in L["domains"]]
    if not cand:
        cand = [fn for fn, L in ledgers.items()
                if any(norm(x) == norm(d) for x in L["domains"])]
    if not cand:
        return None, "没有任何主表用这个域"
    if len(cand) == 1:
        return cand[0], ""
    exact = [fn for fn in cand if norm(fn[:-4]) == norm(d)]
    if len(exact) == 1:
        return exact[0], ""
    return None, "多份主表都有这个域，且文件名对不上：%s" % ", ".join(cand)


def main():
    dry = "--dry" in sys.argv
    parts_dir, ledger_dir = DEFAULT_PARTS, DEFAULT_LEDGER
    only = None
    a = sys.argv[1:]

    # 🔴🔴 2026-10-26 事故加固：**不带 `--only` 且不 `--dry` 时拒绝执行**。
    # 事故经过：有代理跑 `merge_ledger_parts.py --help` 想「看用法」，而本脚本**不解析 `--help`**、
    # 不带参就是**真合并** ⇒ 把批次 3 的旧补丁整批盖回主表，**冲掉了 232 行后续手工修订**（E12/E13/G2 的活）。
    # 现在起：真合并**必须**显式给 `--only <前缀>`（或 `--all` 明确表示并全部）。
    wants_all = "--all" in sys.argv
    # ⚠️ 用 sys.argv 直接判，而不是  ——  在下面才被解析，此处恒 None
    #   （2026-10-26 G6 抓到：这样写会让  的真合并被永远拒绝）。
    if not dry and ("--only" not in sys.argv) and not wants_all:
        print("!! 拒绝执行：真合并必须显式指定范围。")
        print("   用法：--dry [--only B4_]            只看")
        print("         --only B4_                     真合并批次 4 的补丁")
        print("         --all                          真合并 `_parts/` 下**全部**未归档补丁（⚠️ 会盖掉后续手工修订）")
        print("   （本次什么都没做。）")
        return 2

    # 🆕 `--only B4_` ⇒ 只合并指定前缀的补丁。
    # 为什么需要：批次 3 的 `B3_*.tsv` 合并完之后**留在目录里**，而之后 E12/E13 又手工改过主表
    # （备注/等级/落点）。下一次再整目录合并会把那些手工修订**覆盖回补丁里的旧内容**，
    # 而且**没有任何报错**（补丁总是「后写的赢」）。
    if "--only" in a:
        i = a.index("--only")
        if i + 1 >= len(a):
            print("!! --only 后面要跟前缀（例：--only B4_）")
            return 2
        only = a[i + 1]
        a = a[:i] + a[i + 2:]
    for flag in ("--parts", "--ledger"):
        if flag in a:
            i = a.index(flag)
            if i + 1 >= len(a):
                print("!! %s 后面要跟目录" % flag)
                return 2
            if flag == "--parts":
                parts_dir = a[i + 1]
            else:
                ledger_dir = a[i + 1]

    if not os.path.isdir(parts_dir):
        print("!! 找不到补丁目录 %s" % parts_dir)
        return 2
    parts = sorted(f for f in os.listdir(parts_dir)
                   if PATCH_RE.match(f) and (only is None or f.startswith(only)))
    if not parts:
        print("（没有%spatch 补丁，无事可做）" % (("%s*" % only) + " " if only else " "))
        return 0

    ledgers = load_ledgers(ledger_dir)
    if not ledgers:
        print("!! 建不出主表索引")
        return 2

    # 全局键索引： (原版类, 方法) -> [(文件名, 行下标)]
    keyidx = {}
    for fn, L in ledgers.items():
        if L["bad"]:
            print("!! %s 有列数不为 11 的行（%s）⇒ 先修再合并" % (fn, L["bad"][:3]))
            return 2
        if L["bare_cr"]:
            print("!! %s 有 %d 个裸 CR ⇒ 先修再合并" % (fn, L["bare_cr"]))
            return 2
        for i, f in L["rows"][1:]:
            k = (f[1].strip(), f[2].strip())
            keyidx.setdefault(k, []).append((fn, i))

    errors, notes = [], []
    n_upd = n_new = n_same = 0
    for pfn in parts:
        lines, _eol, bare_cr = read_raw(os.path.join(parts_dir, pfn))
        if bare_cr:
            errors.append("%s：有 %d 个裸 CR" % (pfn, bare_cr))
            continue
        lines = [l for l in lines if l.strip()]
        if not lines:
            continue
        hdr = [h.strip() for h in lines[0].split("\t")]
        if hdr != COLS:
            errors.append("%s：表头不是那 11 列（读到 %d 列）" % (pfn, len(hdr)))
            continue
        for i, l in enumerate(lines[1:], start=2):
            f = l.split("\t")
            if len(f) != NCOL:
                errors.append("%s:%d：列数 %d ≠ %d" % (pfn, i, len(f), NCOL))
                continue
            ok, why = looks_like_one_method(f[2].strip())
            if not ok:
                errors.append("%s:%d：`方法` 不合规（%s）：%r" % (pfn, i, why, f[2][:70]))
                continue
            k = (f[1].strip(), f[2].strip())
            hits = keyidx.get(k, [])
            if len(hits) > 1:
                errors.append("%s:%d：%r 在 %d 份主表里都有（%s）⇒ 请人指定"
                              % (pfn, i, k, len(hits), ", ".join(h[0] for h in hits)))
                continue
            if len(hits) == 1:
                fn, ri = hits[0]
                L = ledgers[fn]
                dom = L["rows"][ri][1][0].strip()
                if f[0].strip() != dom:
                    notes.append("%s:%d 域 `%s` → `%s`（跟随 %s）" % (pfn, i, f[0].strip(), dom, fn))
                    f[0] = dom
                old = L["rows"][ri][1]
                if old != f:
                    L["lines"][ri] = "\t".join(f)
                    L["rows"][ri] = (ri, f)
                    L["dirty"] = True
                    n_upd += 1
                else:
                    n_same += 1
                continue
            # 没命中 ⇒ 新行
            target, why = pick_target_file(f[0], ledgers)
            if target is None:
                errors.append("%s:%d：域 `%s` 定位不到主表 —— %s" % (pfn, i, f[0].strip(), why))
                continue
            L = ledgers[target]
            dom = L["domains"][0] if L["domains"] else f[0].strip()
            if f[0].strip() != dom:
                notes.append("%s:%d 域 `%s` → `%s`（新行追加到 %s）"
                             % (pfn, i, f[0].strip(), dom, target))
                f[0] = dom
            while L["lines"] and not L["lines"][-1].strip():
                L["lines"].pop()
            L["lines"].append("\t".join(f))
            L["rows"].append((len(L["lines"]) - 1, f))
            L["dirty"] = True
            keyidx.setdefault(k, []).append((target, len(L["lines"]) - 1))
            n_new += 1

    if notes:
        print("↻ 归一（域跟随主表）：")
        for n in notes:
            print("   " + n)

    if errors:
        print("\n!! 补丁有问题，**未写盘**（%d 条）：" % len(errors))
        for e in errors:
            print("   " + e)
        return 2

    if not dry:
        for fn, L in ledgers.items():
            if not L["dirty"]:
                continue
            while L["lines"] and not L["lines"][-1].strip():
                L["lines"].pop()
            out = L["eol"].join(L["lines"]) + L["eol"]
            io.open(os.path.join(ledger_dir, fn), "wb").write(out.encode("utf-8"))
            print("  ✅ 写盘 %s（行尾 %s）" % (fn, "CRLF" if L["eol"] == "\r\n" else "LF"))
    else:
        for fn, L in ledgers.items():
            if L["dirty"]:
                print("  [dry] 会写 %s" % fn)

    print("\n合计：更新 %d · 新增 %d · 无变化 %d%s"
          % (n_upd, n_new, n_same, "（dry-run，未写盘）" if dry else ""))
    if not dry and (n_upd or n_new):
        print("⏭ 下一步（必做）：")
        print("   python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A")
        print("   python -I -X utf8 d:/4/Unity/工具/ledger_progress.py")
    return 0


if __name__ == "__main__":
    sys.exit(main())
