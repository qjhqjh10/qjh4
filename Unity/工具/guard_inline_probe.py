#!/usr/bin/env python3
# -*- coding: utf-8 -*-
r"""
guard_inline_probe.py -- 「每方法静态初始化 guard」内联侦探（B92 波 4 · B10 方法的可复用版）

原理
====
IL2CPP 生成的 C 里，静态字段的初始化被包在一个「写一次」guard 块里：

    if (DAT_18453c402 == '\0') {
        FUN_1803f44f0(&DAT_1842618d8);
        DAT_18453c402 = '\x01';
    }

当某个方法被 C++ 内联进另一个方法时，这段 guard 块会**跟着复制**进那个方法的 .c。
于是：**某 guard 令牌出现在「别人的」.c 里 ⇒ 那个 .c 是候选内联点**。

⚠️ 局限（务必读，别把弱证据当铁证）
------------------------------------------------
guard 令牌追踪的是**静态字段**，不是方法本身。三档强度：

| 令牌的文件数 | 判定 | 强度 |
|---|---|---|
| == 1 | 该静态是本方法私有 ⇒ **本方法绝没有被内联** | **强**（唯一可下否定结论的一档） |
| 2 .. 阈值(默认20) | 内联 **或** 两个方法共用同一静态 ⇒ **候选** | 弱，需人核（读对方 .c 里令牌前后代码） |
| > 阈值 | 共享类/全局静态 init guard ⇒ **不作内联证据** | 排除（B10 口径） |

因此：`verdict=UNIQUE` 可以放心用来判「没内联」；`verdict=INLINE` 只是候选，
要人工确认对方文件里那段确实是本方法体的展开（而不是共用静态）。

用法
====
    # 1) 建索引（首次 ~25k 文件；之后增量，只重扫变动的文件）
    python -I -X utf8 d:/4/Unity/工具/guard_inline_probe.py build

    # 2) 探单个方法（三源命名都认）
    python -I -X utf8 .../guard_inline_probe.py probe BattleManager.GetRawcardScript
    python -I -X utf8 .../guard_inline_probe.py probe BattleManager__GetRawcardScript
    python -I -X utf8 .../guard_inline_probe.py probe GetRawcardScript --show-lines

    # 3) 批量（读 TSV/清单，出结论表）
    python -I -X utf8 .../guard_inline_probe.py batch \
        --tsv 判据/B88_死代码普查.tsv --class-col 类 --method-col 方法 \
        --filter 判定=内联可疑 --out /tmp/g.tsv

    # 4) 索引统计
    python -I -X utf8 .../guard_inline_probe.py stats

    # 强制全量重建
    python -I -X utf8 .../guard_inline_probe.py build --force

默认**只读**：除 build 写自己的缓存（`--cache`，默认在 `d:/4/_tmp_view/`）外，
probe/stats/batch 默认不写盘；要落盘必须显式 `--out`。

缓存：`--cache` 默认 `d:/4/_tmp_view/guard_index.json.gz`（可按 root 复用；root 变了自动全量重建）。
"""

from __future__ import annotations

import argparse
import gzip
import json
import os
import re
import sys
import time

# ----------------------------------------------------------------------------
# 常量与正则
# ----------------------------------------------------------------------------
BS = chr(92)          # 一个反斜杠
LIT = BS + BS         # 正则里表示「字面反斜杠」要写两个

# 解码后文本里：DAT_xxxx = '\x01';   （guard 块的收尾赋值）
RE_ASSIGN = re.compile("DAT_([0-9a-fA-F]{6,}) = '" + LIT + "x01';")
# 解码后文本里：if (DAT_xxxx == '\0') {  （guard 块的入口判断）
RE_CHECK = re.compile(r"if \(DAT_([0-9a-fA-F]{6,}) == '" + LIT + r"0'\)")

DEFAULT_ROOT = r"d:/2/tools/decomp_full"
DEFAULT_CACHE = r"d:/4/_tmp_view/guard_index.json.gz"
DEFAULT_THRESHOLD = 20
CACHE_VERSION = 1


# ----------------------------------------------------------------------------
# 扫描 / 建索引
# ----------------------------------------------------------------------------
def iter_c_files(root):
    """返回 root 下的 *.c 文件名（不递归；decomp_full 是平铺的）。"""
    try:
        names = os.listdir(root)
    except OSError as e:
        sys.stderr.write("ERROR: 打不开 root %r: %s\n" % (root, e))
        return []
    return sorted(n for n in names if n.endswith(".c"))


def scan_tokens(path):
    """读一个 .c，返回它含有的 guard 令牌集合（assign ∪ check）。"""
    try:
        with open(path, "rb") as f:
            data = f.read()
    except OSError:
        return set()
    txt = data.decode("utf-8", "replace")
    toks = set(RE_ASSIGN.findall(txt)) | set(RE_CHECK.findall(txt))
    return {t.lower() for t in toks}


def stat_sig(path):
    try:
        st = os.stat(path)
        return int(st.st_size), int(st.st_mtime_ns)
    except OSError:
        return -1, -1


def load_cache(cache_path):
    if not os.path.exists(cache_path):
        return None
    try:
        with gzip.open(cache_path, "rt", encoding="utf-8") as f:
            obj = json.load(f)
        if obj.get("version") != CACHE_VERSION:
            return None
        return obj
    except Exception:
        return None


def save_cache(cache_path, obj):
    d = os.path.dirname(os.path.abspath(cache_path))
    if d and not os.path.isdir(d):
        os.makedirs(d, exist_ok=True)
    tmp = cache_path + ".tmp"
    with gzip.open(tmp, "wt", encoding="utf-8") as f:
        json.dump(obj, f, ensure_ascii=False, separators=(",", ":"))
    os.replace(tmp, cache_path)


def cmd_build(args):
    root = args.root
    cache = args.cache
    files = iter_c_files(root)
    if not files:
        sys.stderr.write("ERROR: root 下没有 .c: %r\n" % root)
        return 2

    prev = None if args.force else load_cache(cache)
    prev_files = {}
    if prev and os.path.normpath(prev.get("root", "")) == os.path.normpath(root):
        prev_files = prev.get("files", {})
    elif prev and not args.force:
        sys.stderr.write("NOTE: 缓存 root 不匹配，改为全量重建\n")
        prev = None

    new_files = {}
    scanned = reused = 0
    t0 = time.time()
    for n in files:
        full = os.path.join(root, n)
        size, mtime = stat_sig(full)
        old = prev_files.get(n)
        if old is not None and old.get("size") == size and old.get("mtime") == mtime:
            new_files[n] = old
            reused += 1
            continue
        toks = sorted(scan_tokens(full))
        new_files[n] = {"size": size, "mtime": mtime, "tokens": toks}
        scanned += 1

    removed = 0
    if prev_files:
        cur = set(files)
        removed = sum(1 for k in prev_files if k not in cur)

    obj = {
        "version": CACHE_VERSION,
        "root": root,
        "built_at": int(time.time()),
        "threshold": args.threshold,
        "files": new_files,
    }
    save_cache(cache, obj)

    ntokens = sum(len(v.get("tokens") or []) for v in new_files.values())
    ndistinct = len({t for v in new_files.values() for t in (v.get("tokens") or [])})
    sys.stderr.write(
        "build: files=%d scanned=%d reused=%d removed=%d tokens(occ)=%d distinct=%d %.1fs -> %s\n"
        % (len(files), scanned, reused, removed, ntokens, ndistinct, time.time() - t0, cache)
    )
    return 0


# ----------------------------------------------------------------------------
# 载入索引 → 派生两张表
# ----------------------------------------------------------------------------
class Index:
    def __init__(self, obj):
        self.root = obj["root"]
        self.threshold = obj.get("threshold", DEFAULT_THRESHOLD)
        self.files = obj["files"]                 # file -> {size,mtime,tokens}
        self.file2tokens = {}
        self.token2files = {}
        for fn, meta in self.files.items():
            toks = meta.get("tokens") or []
            self.file2tokens[fn] = toks
            for t in toks:
                self.token2files.setdefault(t, []).append(fn)
        for t in self.token2files:
            self.token2files[t].sort()

    def all_names(self):
        return list(self.files.keys())


def load_index(args, need_build=True):
    obj = load_cache(args.cache)
    if obj is None:
        if not need_build:
            return None
        sys.stderr.write("NOTE: 没有索引缓存，先 build 一次…\n")
        rc = cmd_build(args)
        if rc != 0:
            return None
        obj = load_cache(args.cache)
        if obj is None:
            return None
    if os.path.normpath(obj.get("root", "")) != os.path.normpath(args.root):
        sys.stderr.write("NOTE: 缓存 root(%s) != 当前 root(%s)，重建…\n"
                         % (obj.get("root"), args.root))
        rc = cmd_build(args)
        if rc != 0:
            return None
        obj = load_cache(args.cache)
    return Index(obj)


# ----------------------------------------------------------------------------
# 名字解析
# ----------------------------------------------------------------------------
def _strip_sig(name):
    name = name.strip()
    # 去掉参数部分（含泛型里可能出现的括号？先按最外层 '('）
    i = name.find("(")
    if i >= 0:
        name = name[:i]
    return name.strip()


def resolve_targets(name, names):
    """把一个查询名解析为若干 .c 文件名（精确优先，其次模糊）。"""
    nm = name.strip()
    if not nm:
        return [], "empty"
    base = os.path.basename(nm.replace("\\", "/"))
    # 1) 直接就是 .c 文件名 / 已带 .c
    cands = []
    if base in names:
        return [base], "exact"
    b = base if base.endswith(".c") else base + ".c"
    if b in names:
        return [b], "exact"
    # 2) 去签名，试 A__B / A.B / A.B.C ... 各种点替换
    sig = _strip_sig(base)
    if sig.endswith(".c"):
        sig = sig[:-2]
    tries = []
    tries.append(sig)
    # 把「最后 / 第一个 / 每一个」点换成 __
    if "." in sig:
        tries.append(sig.replace(".", "__", 1))
        tries.append(sig.replace(".", "__"))
        head, _, tail = sig.rpartition(".")
        tries.append(head.replace(".", "__") + "__" + tail)
    seen = set()
    for t in tries:
        if not t or t in seen:
            continue
        seen.add(t)
        fn = t + ".c"
        if fn in names:
            return [fn], "normalized"
        # 命名空间前缀形态：Everguild.LiveOps.MessageDisplay__OnPointerClick.c
        suf = "." + t + ".c"
        sh = sorted(n for n in names if n.endswith(suf))
        if len(sh) == 1:
            return sh, "ns-suffix"
        if sh:
            # 优先取「最短」的那个（少命名空间层）
            sh.sort(key=len)
            return [sh[0]], "ns-suffix-multi"
    # 3) 模糊：名字作为子串
    key = sig.split(".")[-1] if "." in sig else sig
    low = key.lower()
    hits = sorted(n for n in names if low and low in n.lower())
    if len(hits) == 1:
        return hits, "fuzzy"
    if hits:
        return hits, "fuzzy-multi"
    return [], "notfound"


# ----------------------------------------------------------------------------
# 核心：对单个目标文件做判定
# ----------------------------------------------------------------------------
def classify(idx, target_file, threshold=None, verify=False):
    thr = threshold if threshold is not None else idx.threshold
    toks = idx.file2tokens.get(target_file, [])
    detail = []
    for t in toks:
        others = [f for f in idx.token2files.get(t, []) if f != target_file]
        cnt = 1 + len(others)
        if cnt == 1:
            kind = "unique"
        elif cnt <= thr:
            kind = "inline"
        else:
            kind = "shared"
        rec = {"token": t, "count": cnt, "kind": kind, "others": others}
        if verify and kind == "inline":
            ov = {}
            for o in others:
                r = verify_inline(idx, target_file, o)
                ov[o] = round(r[0], 3) if r else None
            rec["overlap"] = ov
            rec["best_overlap"] = max([v for v in ov.values() if v is not None], default=None)
        detail.append(rec)
    # 排序：unique 优先；其余按 count 升序
    rank = {"unique": 0, "inline": 1, "shared": 2}
    detail.sort(key=lambda d: (rank[d["kind"]], d["count"], d["token"]))

    if not detail:
        return {"target": target_file, "verdict": "NO_GUARD", "primary": None, "detail": []}

    primary = detail[0]
    if primary["kind"] == "unique":
        verdict = "UNIQUE"          # 强否定：本方法没被内联
    elif primary["kind"] == "inline":
        verdict = "INLINE"          # 候选：primary.others 是候选内联点（verify 看 overlap）
    else:
        verdict = "SHARED_ONLY"     # 全是被排除的共享 guard ⇒ 本方法无私有 guard，判不了
    return {"target": target_file, "verdict": verdict, "primary": primary, "detail": detail}


def find_lines(path, token):
    """在已解码文本里找 token 出现处（含 guard 两种写法）的行号。"""
    out = []
    try:
        with open(path, "rb") as f:
            data = f.read()
    except OSError:
        return out
    txt = data.decode("utf-8", "replace")
    for i, line in enumerate(txt.split("\n"), 1):
        if token in line and (("x01" in line) or ("'\0'" in line) or ("== '" + BS + "0" in line)):
            out.append(i)
    return out


# ----------------------------------------------------------------------------
# 交叉验证：被内联时，对方文件应包含本方法的大部分「调用符号」
# （B10 对 Resolve 族用的「调用符号 100% 包含」判据；用来排掉「共用静态」假阳性）
# ----------------------------------------------------------------------------
SYM_RE = re.compile(r"[A-Za-z_][A-Za-z0-9_.]*__[A-Za-z_][A-Za-z0-9_]*")


def _read_text(path):
    try:
        with open(path, "rb") as f:
            return f.read().decode("utf-8", "replace")
    except OSError:
        return ""


def callee_symbols(path, drop_self=True):
    txt = _read_text(path)
    syms = set(SYM_RE.findall(txt))
    if drop_self:
        own = os.path.basename(path)[:-2]  # 去掉 .c
        syms.discard(own)
    return syms


def verify_inline(idx, target_file, other_file):
    """返回 (overlap, na, nb, nshared)：target 的调用符号有多少比例也出现在 other 里。"""
    a = callee_symbols(os.path.join(idx.root, target_file))
    b = callee_symbols(os.path.join(idx.root, other_file))
    if not a:
        return None
    shared = len(a & b)
    return (shared / len(a), len(a), len(b), shared)


# ----------------------------------------------------------------------------
# 输出格式化
# ----------------------------------------------------------------------------
def render_text(result, idx, show_lines=False):
    lines = []
    tr = result["target"]
    v = result["verdict"]
    lines.append("target : %s" % tr)
    lines.append("verdict: %s" % v)
    p = result.get("primary")
    if p:
        lines.append("primary: DAT_%s  count=%d  kind=%s" % (p["token"], p["count"], p["kind"]))
        if p.get("best_overlap") is not None:
            lines.append("  best callee-overlap: %.3f" % p["best_overlap"])
        if p["others"]:
            lines.append("  also in:")
            for o in p["others"]:
                ov = (p.get("overlap") or {}).get(o)
                tag = ("  overlap=%.3f" % ov) if ov is not None else ""
                if show_lines:
                    ln = find_lines(os.path.join(idx.root, o), p["token"])
                    lines.append("    %s%s  (lines %s)" % (o, tag, ln or "?"))
                else:
                    lines.append("    %s%s" % (o, tag))
    others_detail = [d for d in result["detail"] if d is not p]
    if others_detail:
        lines.append("other guard tokens in this file:")
        for d in others_detail:
            lines.append("  DAT_%s count=%d %s" % (d["token"], d["count"], d["kind"]))
    return "\n".join(lines)


# ----------------------------------------------------------------------------
# 命令
# ----------------------------------------------------------------------------
def cmd_probe(args):
    idx = load_index(args)
    if idx is None:
        return 2
    results = []
    for q in args.names:
        hits, how = resolve_targets(q, idx.all_names())
        if not hits:
            results.append({"query": q, "target": None, "verdict": "NOT_FOUND",
                            "primary": None, "detail": [], "how": how})
            continue
        if len(hits) > 1:
            results.append({"query": q, "target": None, "verdict": "AMBIGUOUS",
                            "candidates": hits, "primary": None, "detail": [], "how": how})
            continue
        r = classify(idx, hits[0], args.threshold, args.verify)
        r["query"] = q
        r["how"] = how
        results.append(r)

    if args.json:
        sys.stdout.write(json.dumps(results if len(results) > 1 else results[0],
                                    ensure_ascii=False, indent=2) + "\n")
    else:
        out = []
        for r in results:
            if r["verdict"] in ("NOT_FOUND", "AMBIGUOUS"):
                out.append("query  : %s\nverdict: %s%s" % (
                    r["query"], r["verdict"],
                    ("\n  candidates: " + ", ".join(r.get("candidates") or [])) if r.get("candidates") else ""))
            else:
                out.append(render_text(r, idx, args.show_lines))
        sys.stdout.write("\n\n".join(out) + "\n")
    return 0


def _pick_col(header, spec):
    """spec 可以是 1 基列号字符串，也可以是表头名。返回 0 基索引或 None。"""
    spec = str(spec)
    if spec.isdigit():
        i = int(spec) - 1
        return i if 0 <= i < len(header) else None
    if spec in header:
        return header.index(spec)
    return None


def cmd_batch(args):
    idx = load_index(args)
    if idx is None:
        return 2
    with open(args.tsv, "rb") as f:
        raw = f.read().decode("utf-8-sig", "replace")
    rows = raw.replace("\r\n", "\n").replace("\r", "\n").split("\n")
    if not rows or not rows[0].strip():
        sys.stderr.write("ERROR: 空 TSV\n")
        return 2
    header = rows[0].split("\t")
    ci = _pick_col(header, args.class_col)
    mi = _pick_col(header, args.method_col)
    if ci is None or mi is None:
        sys.stderr.write("ERROR: 找不到 类/方法 列。表头=%r\n" % header)
        return 2
    fi = vi = None
    if args.filter:
        for spec in args.filter:
            if "=" not in spec:
                continue
            col, val = spec.split("=", 1)
            j = _pick_col(header, col)
            if j is not None:
                fi, vi = j, val
                break

    out = ["".join(["列\t类\t方法\t目标文件\tverdict\tguard令牌\t文件数\t出现在\tother_guard"])
           ] if False else []
    header_out = ["行号", "类.方法", "目标文件", "verdict", "guard令牌", "文件数", "出现在", "其它guard(文件数)"]
    out.append("\t".join(header_out))
    stats = {}
    for i, line in enumerate(rows[1:], 2):
        if not line.strip():
            continue
        f = line.split("\t")
        if max(ci, mi) >= len(f):
            continue
        if fi is not None:
            if fi >= len(f) or f[fi].strip() != vi:
                continue
        cls = f[ci].strip()
        meth = f[mi].strip()
        q = "%s.%s" % (cls, meth) if cls else meth
        hits, how = resolve_targets(q, idx.all_names())
        if not hits:
            hits2, _ = resolve_targets(meth, idx.all_names())
            hits = hits2
        if len(hits) != 1:
            verdict = "NOT_FOUND" if not hits else "AMBIGUOUS"
            out.append("\t".join([str(i), q, "", verdict, "", "", "", ""]))
            stats[verdict] = stats.get(verdict, 0) + 1
            continue
        r = classify(idx, hits[0], args.threshold, args.verify)
        p = r.get("primary") or {}
        others_detail = [d for d in r["detail"] if d is not p]
        out.append("\t".join([
            str(i), q, hits[0], r["verdict"],
            ("DAT_" + p["token"]) if p else "",
            str(p.get("count", "")) if p else "",
            ", ".join(p.get("others") or []) if p else "",
            "; ".join("%s(%d)" % (d["token"], d["count"]) for d in others_detail),
        ]))
        stats[r["verdict"]] = stats.get(r["verdict"], 0) + 1

    eol = "\r\n" if getattr(args, "crlf", False) else "\n"
    text = eol.join(out) + eol
    if args.out:
        payload = text.encode("utf-8")
        if getattr(args, "bom", False):
            payload = b"\xef\xbb\xbf" + payload
        with open(args.out, "wb") as f:
            f.write(payload)
        sys.stderr.write("wrote %d rows -> %s\n" % (len(out) - 1, args.out))
    else:
        sys.stdout.write(text)
    sys.stderr.write("stats: %r\n" % stats)
    return 0


def cmd_stats(args):
    idx = load_index(args)
    if idx is None:
        return 2
    from collections import Counter
    cnt = Counter()
    for t, fs in idx.token2files.items():
        cnt[len(fs)] += 1
    big = sorted(((len(fs), t) for t, fs in idx.token2files.items()), reverse=True)
    print("root        : %s" % idx.root)
    print("files       : %d" % len(idx.files))
    print("with guard  : %d" % sum(1 for v in idx.file2tokens.values() if v))
    print("no guard    : %d" % sum(1 for v in idx.file2tokens.values() if not v))
    print("distinct tok: %d" % len(idx.token2files))
    print("occ         : %d" % sum(len(v) for v in idx.file2tokens.values()))
    print("token file-count histogram: %r" % dict(sorted(cnt.items())[:15]))
    print("top shared tokens (count, token):")
    for c, t in big[:10]:
        print("  %5d  DAT_%s" % (c, t))
    return 0


# ----------------------------------------------------------------------------
# main
# ----------------------------------------------------------------------------
def build_parser():
    ap = argparse.ArgumentParser(
        prog="guard_inline_probe.py",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        description="「每方法静态初始化 guard」内联侦探（decomp_full 全树索引 → 内联点候选）。",
        epilog=__doc__.split("用法\n====")[-1] if "用法" in __doc__ else None,
    )
    ap.add_argument("--root", default=DEFAULT_ROOT, help="decomp_full 目录（默认 %s）" % DEFAULT_ROOT)
    ap.add_argument("--cache", default=DEFAULT_CACHE, help="索引缓存路径（默认 %s）" % DEFAULT_CACHE)
    ap.add_argument("--threshold", type=int, default=DEFAULT_THRESHOLD,
                    help="共享 guard 排除阈值：文件数 > 此值判共享（默认 %d）" % DEFAULT_THRESHOLD)
    sub = ap.add_subparsers(dest="cmd")

    b = sub.add_parser("build", help="扫描 decomp_full 建/更新索引（增量）")
    b.add_argument("--force", action="store_true", help="强制全量重建")
    b.set_defaults(func=cmd_build)

    p = sub.add_parser("probe", help="探一个或多个方法，给 guard 判定")
    p.add_argument("names", nargs="+", help="方法名：Class.Method / Class__Method / 裸名 / *.c")
    p.add_argument("--json", action="store_true", help="JSON 输出")
    p.add_argument("--show-lines", action="store_true", help="在候选内联点里报令牌行号")
    p.add_argument("--verify", action="store_true", help="算「调用符号重叠率」交叉验证内联")
    p.add_argument("--out", default=None, help="把输出写到文件（默认只打印）")
    p.set_defaults(func=cmd_probe)

    bt = sub.add_parser("batch", help="批量探（读 TSV 的 类/方法 列）")
    bt.add_argument("--tsv", required=True, help="输入 TSV（带表头）")
    bt.add_argument("--class-col", default="类", help="类列（1 基列号或表头名）")
    bt.add_argument("--method-col", default="方法", help="方法列（1 基列号或表头名）")
    bt.add_argument("--filter", action="append", default=[], help="过滤 col=val，可多次")
    bt.add_argument("--verify", action="store_true", help="算「调用符号重叠率」交叉验证内联")
    bt.add_argument("--out", default=None, help="输出 TSV 路径（默认打印）")
    bt.add_argument("--bom", action="store_true", help="输出加 UTF-8 BOM")
    bt.add_argument("--crlf", action="store_true", help="输出用 CRLF 行尾")
    bt.set_defaults(func=cmd_batch)

    st = sub.add_parser("stats", help="打印索引统计")
    st.set_defaults(func=cmd_stats)
    return ap


def main(argv):
    ap = build_parser()
    args = ap.parse_args(argv)
    if not getattr(args, "cmd", None):
        ap.print_help()
        return 0
    if getattr(args, "cmd", None) == "probe" and getattr(args, "out", None):
        # probe --out：捕获 stdout 再落盘
        import io as _io
        buf = _io.StringIO()
        old = sys.stdout
        sys.stdout = buf
        try:
            rc = args.func(args)
        finally:
            sys.stdout = old
        with open(args.out, "wb") as f:
            f.write(buf.getvalue().encode("utf-8"))
        sys.stderr.write("wrote -> %s\n" % args.out)
        return rc
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
