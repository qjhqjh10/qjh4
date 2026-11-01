#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""check_aggregate_lines.py — 「聚合行」静态自检（**只读**）：防「代码被行尾 `//` 静默吞掉」。

## 为什么要有这个工具
本工程的 `Editor/BattleScene.cs` 与 `Editor/ShellScene.cs` 里各有一条「方法尾聚合行」：
上百个 `Xxx_Assert.Run(); pass += …; fail += …;` 全挤在**同一物理行**上（2026-11-01 现读 5,778 / 8,648 字符）。
往这种行上追加代码时，如果追加点落在该行**已有的 `//` 之后**，代码会落进注释里：
  · 编译照样过（`typecheck` 0/0）· 账闸照样绿 · 退出码 0 —— **但那段断言一次都不会执行**。
本项目 2026-10-30 一天内踩过 6 次（连「合计」行都可能被吞）。
⇒ 本工具对每个聚合行断言：**每一个 `Xxx_Assert.Run()` 的起始字符位 < 该行第一个「真正的」`//` 的字符位**。

## 判据 / 口径
1. **聚合行** = 一行里出现 `>= --min-runs`（默认 20）个匹配 `\w+_Assert\.Run\(\)` 的位置。
   默认模式按简报的「更通用形式」收窄到 `.Run()`；要连 `RunEngine()` / `CheckXxx()` 一起查，
   加 `--any-assert-call`（**会更容易撞上行内注释里的散文提及，见的「已知局限」**）。
2. **「真正的 `//`」** 用逐字符扫描求：跳过
   - 普通字符串 `"…"`（含 `\\` 转义）· 逐字字符串 `@"…"`（`""` 转义）· 插值字符串 `$"…"`（按普通字符串处理）
   - 字符字面量 `'…'`
   - 块注释 `/* … */`（**可以跨行**，块注释里的 `//` 不算）
   ⇒ 行内 `/* … // … */` 与字符串里的 `"http://…"` 都不会被误判成行注释。
3. 违规 = `Run()` 起始位 **>=** 该行第一个 `//` 的字符位（字符位按 Python 解码后的 Unicode 码点计，
   同一行内相对行首；若该行没有 `//`，则全部通过——没有任何东西被吞）。

## 用法
    python -I -X utf8 d:/4/Unity/工具/check_aggregate_lines.py <文件> [<文件> ...] [选项]

选项：
    --min-runs N        聚合行阈值（默认 20）
    --any-assert-call   匹配 `\w+_Assert\.\w+\(`（含 RunEngine/CheckXxx；默认只匹配 `.Run()`）
    --json              输出机器可读 JSON（stdout 只有 JSON）
    --self-test         自测：临时造正/负例（含块注释、字符串内 `//`），用子进程验证负例 rc=1
    --quiet             只在有违规时打印明细（结尾仍打合计）
    -h/--help           本帮助

退出码：
    0 = 全部聚合行的 Run() 都在 `//` 之前（或根本没有聚合行）
    1 = 发现违规（逐条打印：文件:行 + 第几个 Run + 两个字符位）
    2 = 用法/读文件错误（**不带参只打用法并 rc=2**，绝不默认去扫整棵工程）

## 推荐调用（同步点跑一次）
    python -I -X utf8 d:/4/Unity/工具/check_aggregate_lines.py \
        d:/4/Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs \
        d:/4/Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs
建议再接一条更宽的（连 `RunEngine` / `CheckXxx` 都看）：
    … --any-assert-call   # 若报出的命中全是注释里的散文提及，人工过一眼即可

## 已知局限（刻意收窄，宁可漏报也不误报）
- **只扫 `_Assert` 类调用**：`Debug.Log(...)` / `pass += ...` 落在 `//` 之后**不会**被本工具报出来。
  真正要判「这一行有没有被吞」仍以「跑一次宿主、看它印没印出来」为唯一硬证据。
- **插值字符串里的 `{ … }`** 按普通字符串处理：若插值表达式内部再嵌字符串且含 `//`，可能算错。
  （本工程聚合行里没有这种写法；出现时请把那段改成独立行。）
- **C# 11 raw string（三个双引号包起来的那种）** 未特殊处理（按普通字符串首尾配对），含 `//` 时会误判——本工程未使用。
- 只报「字符位」。**行号会被别的写手改动**：本工具每次现读文件，不缓存行号。
"""

import io
import json
import os
import re
import sys
import tempfile

DEFAULT_MIN_RUNS = 20
RUN_PATTERN = re.compile(r"\w+_Assert\.Run\(\)")
ANY_CALL_PATTERN = re.compile(r"\w+_Assert\.\w+\(")
PROG = "check_aggregate_lines.py"

USAGE = __doc__.split("## 用法", 1)[1].split("## 推荐调用", 1)[0].rstrip()
USAGE = "## 用法" + USAGE


def _scan_first_line_comment(text):
    """逐字符扫全文，返回 (first_comment_per_line, line_starts)。

    first_comment_per_line[i] = 第 i 行「真正的 `//`」的**行内字符位**，没有则 None。
    行按 '\\n' 切（行尾的 '\\r' 留在内容里，但不影响字符位计算）。
    """
    n_lines = text.count("\n") + 1
    first = [None] * n_lines
    line_starts = [0] * n_lines
    ln = 0
    i = 0
    n = len(text)

    NORMAL, STRING, CHARS, VERBATIM, BLOCK = 0, 1, 2, 3, 4
    state = NORMAL

    # 先算行首偏移（一次线性扫，和主扫描合并也行，但这样更清楚）
    for k in range(1, n_lines):
        line_starts[k] = text.index("\n", line_starts[k - 1]) + 1

    while i < n:
        c = text[i]
        if c == "\n":
            if state == BLOCK:
                pass  # 块注释可以跨行
            elif state in (STRING, CHARS, VERBATIM):
                # 普通字符串/字符字面量跨行在 C# 里非法；这里容忍并复位，避免一行写错拖垮全文
                state = NORMAL
            ln += 1
            i += 1
            continue

        if state == NORMAL:
            two = text[i:i + 2]
            if two == "//":
                if first[ln] is None:
                    first[ln] = i - line_starts[ln]
                # 跳到行尾（行注释到换行为止）
                j = text.find("\n", i)
                if j < 0:
                    break
                i = j
                continue
            if two == "/*":
                state = BLOCK
                i += 2
                continue
            # 逐字 / 插值逐字字符串：@"…" · $@"…" · @$"…"
            if two == '@"' or text[i:i + 3] in ('$@"', '@$"'):
                state = VERBATIM
                i = text.index('"', i) + 1
                continue
            if c == '$' and text[i + 1:i + 2] == '"':
                state = STRING
                i += 2
                continue
            if c == '"':
                state = STRING
                i += 1
                continue
            if c == "'":
                state = CHARS
                i += 1
                continue
            i += 1
            continue

        if state == BLOCK:
            if text[i:i + 2] == "*/":
                state = NORMAL
                i += 2
            else:
                i += 1
            continue

        if state == STRING:
            if c == "\\":
                i += 2
            elif c == '"':
                state = NORMAL
                i += 1
            else:
                i += 1
            continue

        if state == CHARS:
            if c == "\\":
                i += 2
            elif c == "'":
                state = NORMAL
                i += 1
            else:
                i += 1
            continue

        # VERBATIM
        if c == '"':
            if text[i + 1:i + 2] == '"':
                i += 2
            else:
                state = NORMAL
                i += 1
        else:
            i += 1

    return first, line_starts


def check_file(path, min_runs=DEFAULT_MIN_RUNS, pattern=RUN_PATTERN):
    """返回该文件的检查结果 dict（不抛；读失败走异常由调用方处理）。"""
    with io.open(path, "rb") as f:
        raw = f.read()
    text = raw.decode("utf-8-sig")
    first_comment, line_starts = _scan_first_line_comment(text)
    lines = text.split("\n")

    agg_lines = []
    for idx, line in enumerate(lines):
        body = line[:-1] if line.endswith("\r") else line
        hits = [(m.start(), m.group(0)) for m in pattern.finditer(body)]
        if len(hits) < min_runs:
            continue
        comment = first_comment[idx]
        violations = []
        for k, (pos, name) in enumerate(hits, 1):
            if comment is not None and pos >= comment:
                violations.append({"index": k, "call": name, "pos": pos, "comment_pos": comment})
        agg_lines.append({
            "line": idx + 1,
            "line_length": len(body),
            "runs": len(hits),
            "first_comment": comment,
            "ok": not violations,
            "violations": violations,
        })

    return {
        "file": path,
        "aggregate_lines": agg_lines,
        "ok": all(a["ok"] for a in agg_lines),
    }


def _fmt_text(results, quiet, min_runs=DEFAULT_MIN_RUNS):
    out = []
    any_violation = False
    total_agg = 0
    total_runs = 0
    total_viol = 0
    for r in results:
        for a in r["aggregate_lines"]:
            total_agg += 1
            total_runs += a["runs"]
            if a["ok"]:
                if not quiet:
                    out.append(
                        "PASS  {f}:{l}  {n} 个调用全在 `//` 之前"
                        "（首个 // 在字符 {c} · 行宽 {w}）".format(
                            f=r["file"], l=a["line"], n=a["runs"],
                            c=a["first_comment"], w=a["line_length"]))
            else:
                any_violation = True
                total_viol += len(a["violations"])
                out.append(
                    "✗ FAIL {f}:{l}  {n} 个调用里有 {v} 个落在 `//` 之后"
                    "（首个 // 在字符 {c} · 行宽 {w}）".format(
                        f=r["file"], l=a["line"], n=a["runs"],
                        v=len(a["violations"]), c=a["first_comment"], w=a["line_length"]))
                for x in a["violations"]:
                    out.append(
                        "        第 {k} 个 `{call}`：起始字符 {p} ≥ `//` 所在字符 {c}".format(
                            k=x["index"], call=x["call"], p=x["pos"], c=x["comment_pos"]))
        if not r["aggregate_lines"] and not quiet:
            out.append("----  {f}：未发现聚合行（< {m} 个匹配）".format(
                f=r["file"], m=min_runs))
    out.append("合计：文件 {nf} · 聚合行 {na} · 调用 {nr} · 违规 {nv}  ⇒ {verdict}".format(
        nf=len(results), na=total_agg, nr=total_runs, nv=total_viol,
        verdict="FAIL" if any_violation else "PASS"))
    return "\n".join(out), (1 if any_violation else 0)


def _run_self_test():
    """自测：造 4 个临时样本，用子进程跑自己，验证 rc 与预期一致。"""
    tmp = tempfile.mkdtemp(prefix="wf_aggchk_")
    runs = "; ".join("B{n}_Assert.Run(); pass += B{n}_Assert.Pass; fail += B{n}_Assert.Fail".format(n=i)
                     for i in range(1, 26))
    samples = {
        # 正例 1：25 个 Run() 全在 // 之前
        "pos_plain.cs": "        " + runs + "  // 说明\n",
        # 正例 2：块注释里出现 // ，字符串里也有 // —— 首个真 // 在很后面
        "pos_block_and_string.cs": "        /* a // b */ var s = \"http://x\"; " + runs + "  // 真注释\n",
        # 负例：同一物理行上，25 个 Run() 之后先出现 `//`，再「追加」2 个 Run() ⇒ 后两个被吞
        "neg.cs": "        " + runs + "  // 🔴 追加：B26_Assert.Run(); B27_Assert.Run();\n",
    }

    expect = {"pos_plain.cs": 0, "pos_block_and_string.cs": 0, "neg.cs": 1}
    ok = True
    print("== check_aggregate_lines.py --self-test ==")
    for name, want in expect.items():
        path = os.path.join(tmp, name)
        with io.open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(samples[name])
        rc = _child_rc(path)
        good = (rc == want)
        ok = ok and good
        print("{mark} {name}: rc={rc}（期望 {want}）".format(
            mark="✓" if good else "✗", name=name, rc=rc, want=want))
    print("自测结论：{v}".format(v="PASS" if ok else "FAIL"))
    return 0 if ok else 1


def _child_rc(path):
    import subprocess
    cmd = [sys.executable, "-I", "-X", "utf8", os.path.abspath(__file__), path]
    p = subprocess.run(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return p.returncode


def main(argv):
    if not argv or argv[0] in ("-h", "--help"):
        print("用法：python -I -X utf8 {prog} <文件> [<文件> ...] [--min-runs N] "
              "[--any-assert-call] [--json] [--self-test] [--quiet]".format(prog=PROG))
        print()
        print(USAGE)
        return 2 if not argv else 0

    if "--self-test" in argv:
        return _run_self_test()

    min_runs = DEFAULT_MIN_RUNS
    as_json = False
    quiet = False
    any_call = False
    files = []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--json":
            as_json = True
        elif a == "--quiet":
            quiet = True
        elif a == "--any-assert-call":
            any_call = True
        elif a == "--min-runs":
            i += 1
            if i >= len(argv):
                print("✗ --min-runs 缺少数值", file=sys.stderr)
                return 2
            try:
                min_runs = int(argv[i])
            except ValueError:
                print("✗ --min-runs 必须是整数", file=sys.stderr)
                return 2
        elif a.startswith("--min-runs="):
            try:
                min_runs = int(a.split("=", 1)[1])
            except ValueError:
                print("✗ --min-runs 必须是整数", file=sys.stderr)
                return 2
        elif a.startswith("-"):
            print("✗ 未知选项：{a}".format(a=a), file=sys.stderr)
            print(USAGE)
            return 2
        else:
            files.append(a)
        i += 1

    if not files:
        print("✗ 没有给文件（本工具不默认扫整棵工程，避免误扫生成物/临时视图）", file=sys.stderr)
        print(USAGE)
        return 2

    pattern = ANY_CALL_PATTERN if any_call else RUN_PATTERN
    results = []
    for f in files:
        try:
            results.append(check_file(f, min_runs=min_runs, pattern=pattern))
        except (IOError, OSError) as e:
            print("✗ 读不到 {f}：{e}".format(f=f, e=e), file=sys.stderr)
            return 2

    if as_json:
        payload = {
            "ok": all(r["ok"] for r in results),
            "min_runs": min_runs,
            "pattern": pattern.pattern,
            "results": results,
            "totals": {
                "files": len(results),
                "aggregate_lines": sum(len(r["aggregate_lines"]) for r in results),
                "runs": sum(a["runs"] for r in results for a in r["aggregate_lines"]),
                "violations": sum(len(a["violations"]) for r in results for a in r["aggregate_lines"]),
            },
        }
        print(json.dumps(payload, ensure_ascii=False, indent=2))
        return 0 if payload["ok"] else 1

    text, rc = _fmt_text(results, quiet, min_runs)
    print(text)
    return rc


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
