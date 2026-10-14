#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
_scan_stale_cardids.py —— 扫「还按旧卡 id 写、现在指错了」的字面量（只读普查）

背景：2026-10-13 第二十六会话把全池卡 id 换成原版真值（台账 A1758）。
      旧表 `_tmp_view/cardid_bak_1013/card_ids.json`（996 条）
      新表 `Unity/数据/游戏数据/card_ids.json`（1058 条）
      旧池 `_tmp_view/cardid_bak_1013/cards_engine.json`（1126 张）
      新池 `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（1126 张）

判据（三类，按强度）：
  R  renamed  —— 字面量 L 在【旧池】里存在、在【新池】里【不存在】，而 L 那张卡名在新池里
                 挂在**另一个 id** 上 ⇒ 实打实的「id 被改名了」，代码引用的卡已不在该 id 上。
  P  repointed—— L 在旧池/新池里都在，但**同一 id 指向的卡名变了** ⇒ 要按上下文判「本意是哪张」。
  T  tableonly—— 只在 card_ids 表里出现（不在池里）。
  S  same     —— L 前后指向同一张卡 ⇒ 正常。

上下文判定（仅对 R/P）：取命中行 ±200 字符，归一化（去非字母数字、转小写）后找
  旧名 / 新名；再退一步找「卡名前 2 个词」/「卡名第一个词」。
    · 找到【旧名】⇒ 真残留      · 找到【新名】⇒ 已换过 = 正常
    · 都没找到 ⇒ 判不了（若是 哨兵/夹具/断言 上下文 ⇒ 标「需人工看」）

🔑 代码 / 注释判别：逐字符状态机（.cs 的 `//` `/* */` `"…"` `@"…"` `'…'`；
  .py 的三引号 / 单双引号 / `#`）标出每个命中字符是【活代码】还是【注释/文档】。
  只在注释里的旧 id **不会影响运行**，但会误导下一个会话 —— 报告里单列。

用法：
    python -I -X utf8 d:/4/Unity/工具/_scan_stale_cardids.py                 # 报告到 stdout
    python -I -X utf8 d:/4/Unity/工具/_scan_stale_cardids.py --dump x.json  # 全量命中
    python -I -X utf8 d:/4/Unity/工具/_scan_stale_cardids.py --tsv x.tsv

可复算：脚本自己现读四份判据文件，不依赖任何缓存。
"""
import argparse
import io
import json
import os
import re
import sys

ROOT = "d:/4"
FILE_SET_REF = "601005b^"   # 换 id 那趟的父提交
OLD_IDS = ROOT + "/_tmp_view/cardid_bak_1013/card_ids.json"
NEW_IDS = ROOT + "/Unity/数据/游戏数据/card_ids.json"
OLD_POOL = ROOT + "/_tmp_view/cardid_bak_1013/cards_engine.json"
NEW_POOL = ROOT + "/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"

SCOPES = [
    ("assets-cs", ROOT + "/Unity/MyGame/Assets", {".cs"}, "生产代码（C#）"),
    ("tool-cs",   ROOT + "/Unity/工具",            {".cs"}, "工具（C#）"),
    ("tool-py",   ROOT + "/Unity/工具",            {".py"}, "生成器 / 脚本（Python）"),
    ("doc-md",    ROOT + "/Unity/资料",            {".md"}, "文档"),
]
EXTRA_FILES = [ROOT + "/项目任务.md", ROOT + "/CLAUDE.md"]

SKIP_DIR_PARTS = ("/bin/", "/obj/", "/__pycache__/", "/.git/", "/_tmp_")

# SOR72 / UM_Angels_of_Death / AM1 / #HB_BL34
TOKEN_RE = re.compile(r"(?<![A-Za-z0-9_#])(#?[A-Za-z][A-Za-z0-9]*(?:_[A-Za-z0-9]+)*)")
SENTINEL_WORDS = ("哨兵", "夹具", "断言", "sentinel", "fixture", "assert", "checktrue", "哨卡", "白盒")


def norm(s):
    return re.sub(r"[^a-z0-9]", "", (s or "").lower())


def load_json(p):
    with io.open(p, "r", encoding="utf-8") as f:
        return json.load(f)


# ---------------------------------------------------------------- 代码/注释 状态机
def code_mask(txt, ext):
    """返回 bytearray：2 = 字符串字面量内，1 = 裸代码（标识符），0 = 注释。
    ⚠️ 字符串字面量本身算【代码】（卡 id 通常写在字符串里）。"""
    n = len(txt)
    mask = bytearray(b"\x01") * n
    i = 0
    if ext == ".py":
        while i < n:
            c = txt[i]
            if c == "\n":
                i += 1
                continue
            if c == "#":
                j = txt.find("\n", i)
                j = n if j < 0 else j
                for k in range(i, j):
                    mask[k] = 0
                i = j
                continue
            if c == "\\" and i + 1 < n and txt[i + 1] == "\n":
                i += 2
                continue
            if c in "'\"":
                trip = txt[i:i + 3]
                if trip in ("'''", '"""'):
                    j = txt.find(trip, i + 3)
                    j = n if j < 0 else j + 3
                    for k in range(i, min(j, n)):
                        mask[k] = 2
                    i = j
                    continue
                j = i + 1
                while j < n and txt[j] != c and txt[j] != "\n":
                    if txt[j] == "\\":
                        j += 1
                    j += 1
                for k in range(i, min(j + 1, n)):
                    mask[k] = 2
                i = j + 1
                continue
            i += 1
        return mask
    # ---- C 系
    in_block = False
    while i < n:
        if in_block:
            j = txt.find("*/", i)
            if j < 0:
                for k in range(i, n):
                    mask[k] = 0
                break
            for k in range(i, j + 2):
                mask[k] = 0
            i = j + 2
            in_block = False
            continue
        c = txt[i]
        if c == "/" and i + 1 < n and txt[i + 1] == "/":
            j = txt.find("\n", i)
            j = n if j < 0 else j
            for k in range(i, j):
                mask[k] = 0
            i = j
            continue
        if c == "/" and i + 1 < n and txt[i + 1] == "*":
            in_block = True
            i += 2
            continue
        if c == "@" and i + 1 < n and txt[i + 1] == '"':
            j = i + 2
            while j < n:
                if txt[j] == '"':
                    if j + 1 < n and txt[j + 1] == '"':
                        j += 2
                        continue
                    break
                j += 1
            for k in range(i, min(j + 1, n)):
                mask[k] = 2
            i = j + 1
            continue
        if c == '"' or c == "'":
            j = i + 1
            while j < n and txt[j] != c and txt[j] != "\n":
                if txt[j] == "\\":
                    j += 1
                j += 1
            for k in range(i, min(j + 1, n)):
                mask[k] = 2
            i = j + 1
            continue
        i += 1
    return mask


def read_text(p):
    with io.open(p, "rb") as f:
        b = f.read()
    if b.startswith(b"\xef\xbb\xbf"):
        b = b[3:]
    return b.decode("utf-8", "replace")


class Ctx(object):
    def __init__(self):
        self.old_ids = load_json(OLD_IDS)["mapping"]
        self.new_ids = load_json(NEW_IDS)["mapping"]
        self.old_pool = {c["id"]: c["name"] for c in load_json(OLD_POOL)["cards"]}
        self.new_pool = {c["id"]: c["name"] for c in load_json(NEW_POOL)["cards"]}
        self.new_name2id = {}
        for i, n in self.new_pool.items():
            self.new_name2id.setdefault(norm(n), i)
        self.candidates = set(self.old_ids) | set(self.new_ids) | set(self.old_pool) | set(self.new_pool)
        self.renamed, self.gone = {}, {}
        for i, n in self.old_pool.items():
            if i in self.new_pool:
                continue
            t = self.new_name2id.get(norm(n))
            if t is None:
                self.gone[i] = n
            else:
                self.renamed[i] = t
        self.repointed = set(i for i in self.old_pool
                             if i in self.new_pool and self.old_pool[i] != self.new_pool[i])
        self.table_only = set(i for i in (set(self.old_ids) | set(self.new_ids))
                              if i not in self.old_pool and i not in self.new_pool)


def old_name_of(ctx, lit):
    return ctx.old_pool.get(lit) or ctx.old_ids.get(lit)


def new_name_of(ctx, lit):
    return ctx.new_pool.get(lit) or ctx.new_ids.get(lit)


def classify(ctx, lit):
    on, nn = old_name_of(ctx, lit), new_name_of(ctx, lit)
    if lit in ctx.renamed:
        return "R", on, ctx.new_pool.get(ctx.renamed[lit])
    if lit in ctx.gone:
        return "G", on, None
    if lit in ctx.repointed:
        return "P", on, nn
    if on is not None and nn is not None and norm(on) == norm(nn):
        return "S", on, nn
    if on is not None and nn is None:
        return "G2", on, None
    if on is None and nn is not None:
        return "N", None, nn
    return "T", on, nn


def name_score(t, name):
    """返回 (strong, weak)：strong = 整名 / 名前 2 词命中；weak = 名词长 >=7 的单字命中。"""
    if not name:
        return False, False
    n = norm(t)
    full = norm(name)
    strong = bool(full) and full in n
    w = [x for x in re.split(r"[^A-Za-z0-9]+", name) if x]
    if not strong and len(w) >= 2:
        p2 = norm("".join(w[:2]))
        if len(p2) >= 6 and p2 in n:
            strong = True
    weak = False
    for x in w:
        if len(x) >= 7 and norm(x) in n:
            weak = True
            break
    return strong, weak


def verdict_from_names(t, old_name, new_name):
    """返回 'STALE' / 'OK' / '?'（判不了）。先看强匹配，再看弱匹配。"""
    os_, ow = name_score(t, old_name)
    ns_, nw = name_score(t, new_name)
    if ns_ and not os_:
        return "OK"
    if os_ and not ns_:
        return "STALE"
    if nw and not ow:
        return "OK"
    if ow and not nw:
        return "STALE"
    return "?"


def build_pre_sweep(pre_ref, wanted):
    """对每个文件跑 `git diff -U0 <pre_ref> -- <file>`（pre_ref → **工作区**），
    把「工作区里相对 pre_ref 被改过的行号」收成集合。
      · 文件在 pre_ref 里不存在   ⇒ ('new', None)      整份都算新写
      · 文件在 pre_ref 存在、无改动 ⇒ ('same', frozenset())
      · 否则                      ⇒ ('diff', {改过的行号})
    ⚠️ 用行号、不用行文本 —— 表里 id 置换后行文本会撞车（实测 `gen_icon_plan.py` 的
    `("EC8","[Attack]")` 与换 id 前的 `("EC10","[Attack]")` 行足够像，行文本匹配会误判「没动过」）。
    """
    import subprocess
    out = {}
    for path in sorted(wanted):
        rel = os.path.relpath(path, ROOT).replace("\\", "/")
        try:
            r = subprocess.run(["git", "-C", ROOT, "diff", "-U0", pre_ref, "--", rel],
                               capture_output=True, text=True, encoding="utf-8",
                               errors="replace")
        except Exception:
            out[path] = ("err", None)
            continue
        if r.returncode != 0:
            out[path] = ("err", None)
            continue
        txt = r.stdout
        if not txt.strip():
            # 没 diff：要么两版一样，要么 pre_ref 里没有它
            chk = subprocess.run(["git", "-C", ROOT, "cat-file", "-e",
                                  "%s:%s" % (pre_ref, rel)],
                                 capture_output=True, text=True)
            out[path] = ("same", frozenset()) if chk.returncode == 0 else ("new", None)
            continue
        chg = set()
        for mm in re.finditer(r"^@@ -[0-9,]* \+([0-9]+)(?:,([0-9]+))? @@", txt, re.M):
            c = int(mm.group(1))
            d = 1 if mm.group(2) is None else int(mm.group(2))
            for k in range(c, c + d):
                chg.add(k)
        out[path] = ("diff", frozenset(chg))
    return out


def sweep_state(preinfo, path, lineno):
    """'touched' = 这一行在换 id 那一趟（或之后）被改过；'kept' = 从换 id 之前就是现在这样。"""
    e = preinfo.get(path)
    if e is None:
        return "unknown"
    kind, chg = e
    if kind == "new":
        return "touched"
    if kind == "err":
        return "unknown"
    if kind == "same":
        return "kept"
    return "kept" if lineno not in chg else "touched"


def iter_files(root, exts):
    if os.path.isfile(root):
        yield root
        return
    for dirpath, dirnames, filenames in os.walk(root):
        low = dirpath.replace("\\", "/") + "/"
        if any(p in low for p in SKIP_DIR_PARTS):
            dirnames[:] = []
            continue
        for fn in filenames:
            if os.path.splitext(fn)[1].lower() in exts:
                yield os.path.join(dirpath, fn)


def scan_file(ctx, path, scope, collect_S=False, pre=None):
    try:
        txt = read_text(path)
    except Exception as e:
        return [], str(e)
    ext = os.path.splitext(path)[1].lower()
    mask = code_mask(txt, ext)
    lines = txt.split("\n")
    # char offset of each line start
    offs = []
    o = 0
    for L in lines:
        offs.append(o)
        o += len(L) + 1
    hits = []
    for ln, line in enumerate(lines, 1):
        if len(line) > 6000:
            line = line[:6000]
        for m in TOKEN_RE.finditer(line):
            lit = m.group(1)
            if lit not in ctx.candidates:
                continue
            cls, on, nn = classify(ctx, lit)
            if cls == "S" and not collect_S:
                continue
            gpos = offs[ln - 1] + m.start()
            mv = mask[gpos] if gpos < len(mask) else 1
            if ext in (".md",):
                where = "doc"
            else:
                where = "str" if mv == 2 else ("bare" if mv == 1 else "comment")
            in_code = mv != 0
            # 字符串字面量是不是「就是这个 id」（`"ASH83"`）—— 那才是被当【卡 id 键】用
            end = gpos + len(lit)
            barelit = (gpos > 0 and end < len(txt)
                       and txt[gpos - 1] in "\"'"
                       and txt[end] == txt[gpos - 1])
            if cls == "S":
                hits.append(dict(scope=scope, path=path, lineno=ln, lit=lit, cls="S",
                                 old=on, new=nn, in_code=in_code, where=where,
                                 barelit=barelit, verdict="OK", flag="",
                                 swept="", line=line.strip()))
                continue
            ctext = line[max(0, m.start() - 200):m.end() + 200]
            verdict, flag = "?", ""
            if cls in ("R", "G"):
                verdict = "STALE"
            elif cls in ("P", "G2"):
                verdict = verdict_from_names(ctext, on, nn)
            else:                       # N (新表有、旧表/旧池没有)
                verdict = "NEW"
            if verdict == "?":
                low = ctext.lower()
                if any(w in low for w in SENTINEL_WORDS):
                    flag = "需人工看(哨兵/夹具/断言)"
            swept = ""
            if pre is not None:
                st = sweep_state(pre[0], pre[1], path, line, [lit]).get(lit)
                swept = "unknown" if st is None else ("touched" if st else "kept")
            hits.append(dict(scope=scope, path=path, lineno=ln, lit=lit, cls=cls,
                             old=on, new=nn, in_code=in_code, where=where,
                             barelit=barelit, verdict=verdict, flag=flag,
                             swept=swept, line=line.strip(), ctx=ctext.strip()))
    return hits, None


def migration_map(ctx):
    """旧 id -> 该卡在新池里的 id（按**卡名**配对）。固定点 = 没动过。"""
    m = {}
    for i in set(ctx.old_pool) | set(ctx.old_ids):
        name = ctx.old_pool.get(i) or ctx.old_ids.get(i)
        m[i] = ctx.new_name2id.get(norm(name), i)
    return m


def file_set_check(ctx):
    """【集合级】判据：拿 pre 版每个文件的「候选 id 多重集」过一遍迁移映射，
    和现版比 —— 相等 ⇒ 该文件的 id 全部换到位（这个文件**干净**，不必逐行看）。
    不等 ⇒ 这一对 (少/多) 就是该文件里真正错的那几个 id。
    ⚠️ 为什么必须用集合级：id 是**置换**，逐行比会撞车 —— 实测
    `gen_icon_plan.py` 换 id 前 `("EC24","[Attack]")` 与换后同行文本一模一样，
    行级判据会说「没动过」，其实换 id 时那一行代表的是**另一张卡**。"""
    import subprocess
    mig = migration_map(ctx)
    out = []
    roots = [("assets-cs", ROOT + "/Unity/MyGame/Assets", {".cs"}),
             ("tool", ROOT + "/Unity/工具", {".cs", ".py"})]
    for scope, root, exts in roots:
        for p in iter_files(root, exts):
            rel = os.path.relpath(p, ROOT).replace("\\", "/")
            r = subprocess.run(["git", "-C", ROOT, "show", "%s:%s" % (FILE_SET_REF, rel)],
                               capture_output=True, text=True, encoding="utf-8", errors="replace")
            cur_txt = read_text(p)
            cur = _ids_of(cur_txt, ctx.candidates)
            if r.returncode != 0:
                out.append((scope, p, "NEWFILE", None, None))
                continue
            pre = _ids_of(r.stdout, ctx.candidates)
            exp = __import__("collections").Counter()
            for k, v in pre.items():
                exp[mig.get(k, k)] += v
            if exp == cur:
                continue
            out.append((scope, p, "DELTA", dict(exp - cur), dict(cur - exp)))
    return out


def _ids_of(t, cands):
    import collections
    return collections.Counter(m.group(1) for m in TOKEN_RE.finditer(t) if m.group(1) in cands)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--tsv", default=None)
    ap.add_argument("--dump", default=None)
    ap.add_argument("--scope", default=None)
    ap.add_argument("--all", action="store_true", help="连 S（正常）也收")
    ap.add_argument("--pre", default="601005b^",
                    help="换 id 之前那一版（默认 601005b^ = 第二十六会话收口提交的父提交）")
    ap.add_argument("--no-pre", action="store_true")
    ap.add_argument("--filecheck", action="store_true", help="只跑集合级文件体检")
    a = ap.parse_args()

    ctx = Ctx()
    if a.filecheck:
        print("=== 集合级文件体检（ref=%s）===" % FILE_SET_REF)
        rows = file_set_check(ctx)
        nf = sum(1 for r in rows if r[2] == "NEWFILE")
        print("有 delta 的文件 = %d（其中新文件 %d）" % (len(rows), nf))
        for scope, p, kind, miss, extra in rows:
            print("[%s] %s" % (scope, os.path.relpath(p, ROOT)))
            if kind == "NEWFILE":
                print("    （%s 里没有 → 整个文件都是换 id 之后新写的）" % FILE_SET_REF)
            else:
                print("    少(应为): %s" % miss)
                print("    多(现版): %s" % extra)
        return
    print("=== 判据统计 ===")
    print("old card_ids = %d / new = %d" % (len(ctx.old_ids), len(ctx.new_ids)))
    print("old pool = %d / new pool = %d" % (len(ctx.old_pool), len(ctx.new_pool)))
    print("候选 id 全集 = %d" % len(ctx.candidates))
    print("  R renamed(旧池有/新池无, 卡名搬走) = %d  → 例 %s" %
          (len(ctx.renamed), list(ctx.renamed.items())[:3]))
    print("  G gone   (旧池有/新池无, 卡名也找不到) = %d" % len(ctx.gone))
    print("  P repointed(同 id 换卡) = %d" % len(ctx.repointed))
    print("  T table-only = %d" % len(ctx.table_only))
    print()

    scopes = [s for s in SCOPES if (a.scope is None or s[0] == a.scope)]
    all_hits, errs, stats = [], [], {}
    for name, root, exts, desc in scopes:
        nf, hits = 0, []
        for p in iter_files(root, exts):
            nf += 1
            h, err = scan_file(ctx, p, name, a.all)
            if err:
                errs.append("%s: %s" % (p, err))
            hits.extend(h)
        stats[name] = (nf, hits)
        all_hits.extend(hits)

    # 第二趟：只对「有非 S 命中」的文件补 swept 判据
    if not a.no_pre:
        wanted = set(h["path"] for h in all_hits
                     if h["scope"] in ("assets-cs", "tool-cs", "tool-py"))
        pre = build_pre_sweep(a.pre, wanted)
        print("pre-sweep 索引：%d 个文件（%s）" % (len(pre), a.pre))
        for h in all_hits:
            if h["cls"] != "S":
                h["swept"] = sweep_state(pre, h["path"], h["lineno"])
        for name, (nf, hits) in stats.items():
            stats[name] = (nf, hits)
    if a.scope is None or a.scope == "extra":
        ex = []
        for p in EXTRA_FILES:
            if os.path.isfile(p):
                h, err = scan_file(ctx, p, "extra", a.all, None)
                if err:
                    errs.append("%s: %s" % (p, err))
                ex.extend(h)
        stats["extra"] = (len(EXTRA_FILES), ex)
        all_hits.extend(ex)

    def key(h):
        if h["scope"] == "doc-md" or h["scope"] == "extra":
            return "Z-doc"
        return ("A-code" if h["in_code"] else "B-comment")
    def verdict_of(h):
        if h["cls"] == "S":
            return "S"
        if h["cls"] == "N":
            return "N"
        return h["verdict"]

    print("=== 逐范围 × 命中性质 ===")
    print("%-10s %6s %8s %6s %8s %8s %8s %8s %8s" %
          ("scope", "files", "hits(非S)", "S", "STALE-str", "STALE-bare", "STALE-cmt", "?", "NEW"))
    for name, (nf, hits) in stats.items():
        ns = sum(1 for h in hits if h["cls"] == "S")
        def cnt(v, w):
            return sum(1 for h in hits if h["cls"] != "S" and h.get("verdict") == v and h.get("where") == w)
        nq = sum(1 for h in hits if h["cls"] != "S" and h.get("verdict") == "?")
        nn = sum(1 for h in hits if h["cls"] == "N")
        print("%-10s %6d %8d %6d %8d %8d %8d %8d %8d" %
              (name, nf, len(hits) - ns, ns,
               cnt("STALE", "str"), cnt("STALE", "bare"), cnt("STALE", "comment"), nq, nn))

    if a.dump:
        with io.open(a.dump, "w", encoding="utf-8", newline="\n") as f:
            json.dump(all_hits, f, ensure_ascii=False, indent=1)
        print("\n[dump] %s" % a.dump)
    if a.tsv:
        with io.open(a.tsv, "w", encoding="utf-8", newline="\n") as f:
            f.write("scope\tfile\tline\tliteral\tcls\told_name\tnew_name\tin_code\tverdict\t"
                    "flag\tline_text\tcontext\n")
            for h in all_hits:
                f.write("\t".join([
                    h["scope"], h["path"], str(h["lineno"]), h["lit"], h["cls"],
                    h["old"] or "", h["new"] or "", "1" if h["in_code"] else "0",
                    h.get("where",""), "1" if h.get("barelit") else "0",
                    verdict_of(h), h["flag"], h.get("line", "").replace("\t", " "),
                    h["ctx"].replace("\t", " ").replace("\r", " ")]) + "\n")
        print("[tsv] %s" % a.tsv)
    for e in errs[:10]:
        print("ERR", e)


if __name__ == "__main__":
    main()
