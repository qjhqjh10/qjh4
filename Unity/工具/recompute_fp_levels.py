#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""重算「功能点级」那把尺子：按【现账】逐 FP 重算三列（`构成条数` / `状态分布` / `等级`）。

## 口径（沿用 E9 的读法，⛔ 别自创）
- **FP 等级 = 该 FP 构成行里【最差的那一条】**，但 **D 不参与竞争** ——
  D = 原版不可达 / 不复刻，它不阻塞玩家用上这个功能点。
  顺序：任一 C ⇒ C · 否则任一 B ⇒ B · 否则 A。**所有构成行都是 D ⇒ D**。
- ⚠️ 这与「行级真复刻率 A/(A+B+C)」是两把尺子，⛔ 别混（行级测「读懂了多少」）。

## 🔧 2026-11-01 `B111_F1`：**三列的唯一权威实现 = `工具/feature_index_check.py`**
本脚本 `import` 它的 `expand()` / `grade_of()` / `dist_of()` / `load_ledger()`，⛔ 不再自带第二份。
（收口前本脚本自带一份 `expand_no_paren` + 泛型族展开，与机检口径差 **415 行次**：
 去重合计 9,618 vs 机检「已认领」9,203 —— 两边把同一份输入读成两个数。）

## 用法（默认只打印，⛔ 不写盘；**每个写盘动作都要显式给参数**）
    python -I -X utf8 d:/4/Unity/工具/recompute_fp_levels.py            # 只看统计与变化
    python -I -X utf8 d:/4/Unity/工具/recompute_fp_levels.py --verbose  # 逐 FP 打旧→新
    python -I -X utf8 d:/4/Unity/工具/recompute_fp_levels.py --apply    # 写回 part.tsv 的 3 列（显式动作）
    python -I -X utf8 d:/4/Unity/工具/recompute_fp_levels.py --fix-coverage  # 重写备注「覆盖 N/M 行」的 N 与 M
    python -I -X utf8 d:/4/Unity/工具/recompute_fp_levels.py --fix-doubt     # 把存疑行的 `（类.方法）` 补全

## `备注` 里的「覆盖 N/M 行」口径（2026-11-01 `B112_D` 定，⛔ 别再猜）
- **N = 本 FP 的 `构成条数`**（= 按 `账文件:行号` 去重的账行数，与机检 C2 同口径）。
- **M = 该域账行总数**（`账/*.tsv` 按 `域` 列计，**含 `泛型与启动链_补读.tsv` 里属于该域的行**）。
  判据：① 同一份 part 内所有 FP **共用一个 M**（子集不会这样）；② 战役 126 / 教程 155 / 本地化 10 /
  联机 179 / 锻造 118 / 音频 154 这几个域的 M **与现读域账行数逐字相等**；③ 其余域的 M 小/大几个
  = 写表时的旧快照（账后来增删过）⇒ **`--fix-coverage` 一律按现读域总数写**。
- ⚠️ 这是**备注里的散文**，不是机检列；`--fix-coverage` 只换数字，句子其余部分逐字保留。
- ⚠️ **写法有三种**（2026-11-01 扫全 20 份实测）：`覆盖 30/126 行` · `覆盖 该域 4/10 行` ·
  **不含「覆盖」直接写** `53/350 行` / `约 100/350 行`。只认「覆盖」会**静默漏掉 21 行**
  （个人资料 6 · 其他/基础设施 9 · 社交/聊天 6）—— 这正是 `B111_F1` 报告里那 10 个「N 对不上」的来源。

## `存疑行` 口径（2026-11-01 `B112_D` 落进工具）
- 每条 `文件.tsv:行号` 必须**同时带该账行的 `类.方法`**（⛔ 只写行号 = 下一个人得回账里翻）。
- `--fix-doubt` **就地补**：缺名字的，把 `（类.方法）` 插在行号后面；**解不出来 ⇒ 打 ✗ 且不写**
  （⛔ 不编、不猜、不静默跳过）。

## `--apply` 写哪三列（2026-11-01 起）
- 第 7 列 `构成条数` = **按 `账文件:行号` 去重**的账行数（与 `feature_index_check` 的 `len(set(exp))` 一致）
- 第 8 列 `状态分布` = `A12·D3`（= `feature_index_check.dist_of`）
- 第 9 列 `等级`     = worst 档 + 沿用旧值里的 `🔶` + **`存疑行` 非空才带 `?`**
  ⇒ ⛔ 与旧版不同：旧版 `--apply` 只写 `等级`，而且**把 `?` 一律抹掉**（会静默丢掉 19 个存疑标记，
  与设计文 §1.1 第 9 列「`?` ⟺ `存疑行` 非空」冲突）。旧版也不写 7/8 两列。

## 三条硬规矩（本仓踩过）
1. **二进制读、二进制写**：照抄每份文件自己的行尾；⛔ 别用文本模式（会把 CRLF 翻成 LF）。
2. **先算进变量、确认无误再写盘**（`write(<表达式>)` 会先截断再求值，异常 = 文件空掉）。
3. `--apply` 只改第 7/8/9 三列，其余列逐字不动；`--apply` 之外**一律不写盘**。
   `--fix-coverage` / `--fix-doubt` 同理，各自只动自己那一列。
4. **无变化的文件不写**（2026-11-01 起）：逐行比过才置 `touched`，一份文件一行没变就 `SKIP`。
   ⛔ 别「跑一次就刷全部 mtime」—— 那会让人分不清「内容是新的」还是「只是被重写过」。
"""
import glob
import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)
import feature_index_check as FIC          # noqa: E402  —— 唯一实现

IDX_GLOB = os.path.join(r"d:/4/Unity/资料/完全复刻/判据", "功能点索引_*_part.tsv")
KNOWN_STATES = ("A", "B", "C", "D")
PLACEHOLDER = ("", "—")

# 「覆盖 N/M 行」：N = 本 FP 构成条数（去重账行数）· M = 该域账行总数（见文头）。
# ⚠️ 实测写法有三种（2026-11-01 `B112_D` 扫全 20 份）：
#   `覆盖 30/126 行` · `覆盖 该域 4/10 行` · **不含「覆盖」直接写** `53/350 行` / `约 100/350 行`
#   ⇒ 前缀做三选一（`覆盖…` / `约 ` / 行首），只换两个数字，其余逐字不动。
COVER_RE = re.compile(r"(覆盖\s*(?:该域\s*)?|约\s*|^)(\d+)(\s*/\s*)(\d+)(\s*行)")
# 存疑行 token 里的 `文件.tsv:行号`（账文件名不含空格/括号/分号）。
DOUBT_SRC_RE = re.compile(r"([^\s;；（）()]+\.tsv)\s*:\s*(\d+)")


def read_raw(path):
    b = io.open(path, "rb").read()
    text = b.decode("utf-8-sig")
    if b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n"):
        return text.split("\r\n"), "\r\n"
    return text.split("\n"), "\n"


def worst(states):
    """D 不参与竞争（见文头）。仅用于**报告**；写盘值一律取 `FIC.grade_of`（同一实现）。"""
    ss = [s for s in states if s]
    live = [s for s in ss if s != "D"]
    if not live:
        return "D" if ss else "?"
    if "C" in live:
        return "C"
    if "B" in live:
        return "B"
    if "A" in live:
        return "A"
    return live[0]


def domain_totals(rows):
    """域 → 账行数（**含 `泛型与启动链_补读.tsv` 里属于该域的行**；口径见文头）。"""
    cnt = {}
    for r in rows:
        cnt[r["域"]] = cnt.get(r["域"], 0) + 1
    return cnt


def resolve_doubt(tok, by_src):
    """存疑行 token → (完整 `类.方法`, 账行, None)；取不到返回 (None, None, 原因)。"""
    m = DOUBT_SRC_RE.search(tok)
    if not m:
        return None, None, "取不出 `文件.tsv:行号`"
    src = "%s:%d" % (os.path.basename(m.group(1)), int(m.group(2)))
    r = by_src.get(src)
    if not r:
        return None, None, "账里没有这一行（%s）" % src
    return "%s.%s" % (r["类"], FIC.base(r["方法"])), r, None


def main():
    verbose = "--verbose" in sys.argv
    do_apply = "--apply" in sys.argv
    do_cov = "--fix-coverage" in sys.argv
    do_doubt = "--fix-doubt" in sys.argv

    rows = FIC.load_ledger(FIC.LEDGER)
    print("账行 =", len(rows), "（feature_index_check 口径）")
    by_src = {r["src"]: r for r in rows}
    dom_tot = domain_totals(rows)

    files = sorted(glob.glob(IDX_GLOB))
    print("功能点索引 part 文件 =", len(files))
    if not files:
        return

    old_cnt, new_cnt = {}, {}
    changes, misses = [], 0
    matrix = {}
    plan = []               # (path, lines, eol, rownum -> (构成条数, 状态分布, 等级), 三列号, extravals)
    empty_expansions = []   # (文件, token, 错误)
    unknown_hits = []       # (FP id, [非 ABCD 档...])
    fp_sizes = []           # (FP id, 构成行数-按 文件:行 去重)
    cov_changes = []        # (FP id, 旧「覆盖 N/M 行」片段, 新片段)
    cov_unresolved = []     # (FP id, 域) —— 域账行数为 0，无从定 M
    doubt_changes = []      # (FP id, 旧 token, 新 token)
    doubt_bad = []          # (FP id, token, 原因) —— 账里解不出，⛔ 不写
    total_fp = 0
    sum_cnt = 0

    for p in files:
        lines, eol = read_raw(p)
        if not lines:
            continue
        hdr = lines[0].split("\t")
        need = ("等级", "原版构成", "构成条数", "状态分布", "存疑行")
        if any(k not in hdr for k in need):
            print("SKIP (缺列 %s):" % ",".join(k for k in need if k not in hdr), os.path.basename(p))
            continue
        i_lv, i_cs = hdr.index("等级"), hdr.index("原版构成")
        i_cnt, i_dist, i_sec = hdr.index("构成条数"), hdr.index("状态分布"), hdr.index("存疑行")
        newvals = {}
        extravals = {}
        for n, l in enumerate(lines):
            if n == 0 or not l.strip():
                continue
            c = l.split("\t")
            if len(c) <= max(i_lv, i_cs, i_cnt, i_dist, i_sec):
                continue
            old = c[i_lv].strip()
            old_cnt[old] = old_cnt.get(old, 0) + 1
            cons = [x.strip() for x in c[i_cs].split(";") if x.strip()]
            exp = []
            for item in cons:
                hit, err = FIC.expand(item, rows)
                if err:
                    empty_expansions.append((os.path.basename(p), item, err))
                    continue
                exp.extend(hit)
            # 按 `账文件:行号` 去重（与机检的 C2 完全同口径）
            seen, uniq = set(), []
            for r in exp:
                if r["src"] in seen:
                    continue
                seen.add(r["src"])
                uniq.append(r)
            states = [r["状态"] for r in uniq]
            bad = sorted(set(s for s in states if s not in KNOWN_STATES))
            if bad:
                unknown_hits.append((c[0].strip() if c else "?", bad))
            nv = FIC.grade_of(states)
            dist = FIC.dist_of(states)
            selfd = "🔶" in old
            has_q = c[i_sec].strip() not in PLACEHOLDER
            cell = nv + ("🔶" if selfd else "") + ("?" if has_q else "")
            newvals[n] = (str(len(uniq)), dist, cell)
            # --- 备注「覆盖 N/M 行」：N=本 FP 构成条数 · M=域账行总数（只读计算；写盘要 --fix-coverage）---
            extra = {}
            if "备注" in hdr:
                i_note = hdr.index("备注")
                if len(c) > i_note and COVER_RE.search(c[i_note]):
                    dom = c[2].strip() if len(c) > 2 else ""
                    mt = dom_tot.get(dom, 0)
                    if not mt:
                        cov_unresolved.append((c[0].strip(), dom))
                    else:
                        def _cov(mo, _n=len(uniq), _m=mt):
                            return "%s%d%s%d%s" % (mo.group(1), _n, mo.group(3), _m, mo.group(5))
                        fixed = COVER_RE.sub(_cov, c[i_note])
                        if fixed != c[i_note]:
                            cov_changes.append((c[0].strip(), c[i_note], fixed))
                            if do_cov:
                                extra[i_note] = fixed
            # --- 存疑行：每条 token 必须带 `类.方法`（只读检查；补全要 --fix-doubt）---
            doubt = c[i_sec].strip()
            if doubt and doubt not in PLACEHOLDER:
                out_toks = []
                for t in doubt.split(";"):
                    if not t.strip() or t.strip() in PLACEHOLDER:
                        out_toks.append(t)
                        continue
                    full, _row, err = resolve_doubt(t, by_src)
                    if err:
                        doubt_bad.append((c[0].strip(), t.strip(), err))
                        out_toks.append(t)
                    elif full in t:
                        out_toks.append(t)
                    else:
                        mo = DOUBT_SRC_RE.search(t)
                        newt = t[:mo.end()] + "（%s）" % full + t[mo.end():]
                        doubt_changes.append((c[0].strip(), t.strip(), newt.strip()))
                        out_toks.append(newt if do_doubt else t)
                if do_doubt:
                    joined = ";".join(out_toks)
                    if joined != c[i_sec]:
                        extra[i_sec] = joined
            if extra:
                extravals[n] = extra
            new_cnt[nv] = new_cnt.get(nv, 0) + 1
            fp_sizes.append((c[0].strip() if c else "?", len(uniq)))
            sum_cnt += len(uniq)
            if not cons:
                misses += 1
            norm_old = FIC.parse_grade(old)[0]
            matrix[(norm_old, nv)] = matrix.get((norm_old, nv), 0) + 1
            if norm_old != nv or old != cell:
                changes.append((os.path.basename(p), c[0] if c else "?", old, cell,
                                len(cons), len(uniq)))
        plan.append((p, lines, eol, newvals, (i_cnt, i_dist, i_lv), extravals))
        total_fp += len(newvals)

    print("\n--- 旧等级分布（原样）---", dict(sorted(old_cnt.items(), key=lambda x: -x[1])))
    print("--- 新等级分布（含 ? / 🔶 的**档位**统计）---", dict(sorted(new_cnt.items(), key=lambda x: -x[1])))
    print("\nFP 总数 %d · **逐 FP 去重相加 = %d** · 账行总数 = %d"
          % (total_fp, sum_cnt, len(rows)))
    print("构成条目展开失败（含 0 行展开）= %d" % len(empty_expansions))

    print("\n--- 非 ABCD 档警告（⛔ 不静默）---")
    print("出现非 ABCD 档的 FP = %d 个" % len(unknown_hits))
    for fid, ss in unknown_hits[:20]:
        print("  ⚠ %s 含 %s" % (fid, ",".join(ss)))

    print("\n--- 展开失败 / 0 行的构成条目（⛔ 不许静默）---")
    for f, it, err in empty_expansions[:20]:
        print("  ⚠ %s · %r ⇒ %s" % (f, it, err))
    if len(empty_expansions) > 20:
        print("  … 还有 %d 条" % (len(empty_expansions) - 20))

    print("\n--- 自检 ---")
    if fp_sizes:
        mx_fid, mx_n = max(fp_sizes, key=lambda x: x[1])
        print("构成条数（按 文件:行 去重）最大的 FP = %s (%d)" % (mx_fid, mx_n))
        over = [(fid, nn) for fid, nn in fp_sizes if nn > len(rows)]
        print("构成条数 > 账总行数 的 FP = %d" % len(over))
        for fid, nn in over[:20]:
            print("  ✗ %s 构成 %d > 账 %d" % (fid, nn, len(rows)))
        ok = (not empty_expansions and not over)
        print("自检结论 =", "PASS ✓（无展开失败、无 FP 超上限）" if ok else "FAIL ✗（详见上面各项）")

    print("\n--- 旧→新 矩阵（档位，去掉 ? / 🔶）---")
    for (o, n2), c in sorted(matrix.items(), key=lambda x: (-x[1], str(x[0]))):
        print("  %-6s → %-4s %4d" % (o, n2, c))

    if verbose:
        print("\n--- 逐 FP 构成条数（按 文件:行 去重，只打印不写盘）---")
        for fid, nn in fp_sizes:
            print("  %-18s %d" % (fid, nn))
        print("\n--- 等级/三列有变化的 FP（旧 → 新）---")
        for r in changes:
            print("  %-40s %-14s %-10s → %-10s  构成%d条→去重%d" % r)
    else:
        print("（加 --verbose 看逐 FP 变化）")

    print("\n--- 备注「覆盖 N/M 行」（N=构成条数 · M=域账行总数）---")
    print("N/M 与现读不一致 = %d（%s）"
          % (len(cov_changes), "将被 --fix-coverage 改写" if do_cov else "只读；要改加 --fix-coverage"))
    for fid, old, new in cov_changes[:20]:
        print("  ~ %-16s 旧：%s" % (fid, old[:130]))
        print("  %-18s 新：%s" % ("", new[:130]))
    if len(cov_changes) > 20:
        print("  … 还有 %d 条" % (len(cov_changes) - 20))
    for fid, dom in cov_unresolved[:20]:
        print("  ✗ %s 域 `%s` 账行数为 0 ⇒ M 定不了，⛔ 不改" % (fid, dom))

    print("\n--- 存疑行（每条必须带 `类.方法`，⛔ 只写行号不合格）---")
    print("缺 `类.方法` = %d（%s）"
          % (len(doubt_changes), "将被 --fix-doubt 补全" if do_doubt else "只读；要改加 --fix-doubt"))
    for fid, old, new in doubt_changes[:20]:
        print("  ~ %-16s 旧：%s" % (fid, old[:130]))
        print("  %-18s 新：%s" % ("", new[:130]))
    if len(doubt_changes) > 20:
        print("  … 还有 %d 条" % (len(doubt_changes) - 20))
    for fid, tok, err in doubt_bad[:20]:
        print("  ✗ %s %r ⇒ %s（⛔ 不写）" % (fid, tok[:90], err))

    if not (do_apply or do_cov or do_doubt):
        print("\n未写盘（3 列：--apply · 覆盖 N/M：--fix-coverage · 存疑行：--fix-doubt）。")
        return

    for p, lines, eol, newvals, idx, extravals in plan:
        i_cnt, i_dist, i_lv = idx
        out = []
        changed = False
        for n, l in enumerate(lines):
            if not l.strip():
                out.append(l)
                continue
            c = l.split("\t")
            touched = False
            if do_apply and n in newvals and len(c) > max(i_cnt, i_dist, i_lv):
                cnt, dist, cell = newvals[n]
                if (c[i_cnt], c[i_dist], c[i_lv]) != (cnt, dist, cell):
                    c[i_cnt], c[i_dist], c[i_lv] = cnt, dist, cell
                    touched = True
            for ci, val in (extravals.get(n) or {}).items():
                if ci < len(c):
                    c[ci] = val
                    touched = True
            if touched:
                changed = True
            out.append("\t".join(c) if touched else l)
        if not changed:
            print("SKIP", os.path.basename(p), "（无变化，⛔ 不写）")
            continue
        data = eol.join(out)
        if not data.endswith(eol):
            data += eol
        payload = data.encode("utf-8")
        io.open(p, "wb").write(payload)
        print("WROTE", os.path.basename(p))


if __name__ == "__main__":
    main()
