# -*- coding: utf-8 -*-
"""check_tmp_view_refs.py -- `_tmp_view/` 当「第二份真相」的守卫（T05 / A1948 / A1977）。

**只报告，⛔ 不删文件、不改正文。**

判据（三条，都现读核对过）：
  ① `资料/任务线/工具文档线/准则与做法.md:47`：
     「⛔ 别拿 `_tmp_view/` 下的副本当真相 —— `A1977` 实测：对账仪器写死了过期输入路径 ⇒
      拿旧读数当现值已经实际发生（同一件对账上 1782 / 1878 / 1821 三套数字并存）」。
  ② `A1948`：`_tmp_view/` 里 7 份同源仪器副本仍写着 `('ACT', 46, …)`（= A1937 已经修掉的
     伪造触发标签），其中 `a1915_post.py` **再跑一次仍会产出旧标签**。
     判据正本 → `资料/历史/按线台账_任务_卡组卡牌战斗_原文_20261015.md:438`。
  ③ `A1977`：仪器写死过期输入路径 —— 本工具就检查「被引用的 `_tmp_view` 文件还在不在」。

本工具查三样：
  ① 仓内 `.py` / `.sh` / `.md` 里把 `_tmp_view/...` **当输入**引用的地方
     （按行内关键词分 `READ` / `WRITE` / `UNKNOWN`；⛔ 这是启发式，报告里逐条可核）；
  ② 被引用的 `_tmp_view` 文件**现在还在不在**（没了 = A1977 那类「写死过期路径」）；
  ③ `A1948` 的**过期仪器**：任何 `_tmp_view/*.py` 里还写着 `('ACT', 46,`。

用法：
    python -I -X utf8 d:/4/Unity/工具/check_tmp_view_refs.py
    python -I -X utf8 ... --no-exclude-history     # 连 资料/历史/ 一起扫
    python -I -X utf8 ... --samples 5 --top 30
    python -I -X utf8 ... --fail-missing           # 有「被引用但文件已不在」时退出 1
"""
import argparse
import ast
import io
import os
import re
import sys
import tokenize

UNITY = 'd:/4/Unity'
REPO = 'd:/4'
TV_ROOTS = (os.path.join(REPO, '_tmp_view'), os.path.join(UNITY, '_tmp_view'))

DEFAULT_INCLUDES = ('工具/**/*.py', '工具/**/*.sh', '资料/**/*.md')
DEFAULT_EXCLUDE_DIRS = ('资料/历史',)

# `_tmp_view` 路径（前后允许普通路径字符；尾部遇到空白/引号/括号/中文标点就停）
TV = re.compile(
    r'(?P<path>(?:[A-Za-z]:)?[A-Za-z0-9_./\\\-\u4e00-\u9fff]*[/\\]_tmp_view'
    r'(?:[/\\][^\s\'"`)\]}>，。；;、|（）]+)?)')

# 行内分类（启发式；⛔ 它不是判据，只是**减少人工核对量**的排序）
WRITE_HINT = re.compile(
    r"open\([^)]*['\"][wa]b?['\"]|to_csv|to_json|savefig|json\.dump|\.write\(|--out\b|"
    r"\b(OUT|OUTPUT|DST|DEST|SAVE|LOG)\w*\s*=|>\s*['\"]?\S*_tmp_view|tee\b", re.I)
READ_HINT = re.compile(
    r"open\(|read_|read\(|json\.load|loadtxt|imread|--in\b|"
    r"\b(IN|INPUT|SRC|SOURCE|SNAPSHOT|TRUTH)\w*\s*=|<\s*['\"]?\S*_tmp_view", re.I)

STALE_ACT = re.compile(r"\(\s*['\"]ACT['\"]\s*,\s*46\s*,")
FIXED_ACT = re.compile(r"\(\s*['\"]ACT['\"]\s*,\s*ACT_TRIG\s*,")


def _norm(p):
    return p.replace('\\', '/').lstrip('./')


def _walk_glob(rel_pattern, root=UNITY):
    rel = _norm(rel_pattern)
    if '*' not in rel:
        p = os.path.join(root, rel)
        if os.path.isfile(p):
            yield p
        return
    if '/**/' in rel:
        base, tail = rel.split('/**/', 1)
        ext = tail[1:] if tail.startswith('*.') else None
        for dp, dns, fns in os.walk(os.path.join(root, base)):
            dns[:] = [d for d in dns if d not in ('.git', 'Library', 'Temp', 'obj', 'Logs')]
            for fn in fns:
                if ext is None or fn.endswith(ext):
                    yield os.path.join(dp, fn)
    elif '*' in rel:
        base, tail = rel.rsplit('/', 1)
        ext = tail[1:] if tail.startswith('*') else None
        start = os.path.join(root, base)
        if os.path.isdir(start):
            for fn in os.listdir(start):
                p = os.path.join(start, fn)
                if os.path.isfile(p) and (ext is None or fn.endswith(ext)):
                    yield p


def _strip_tail(p):
    """去掉路径尾部粘连的标点 / 行号后缀。

    · 引用句里常见 `…_tmp_view/x.json，`、`（见 …_tmp_view/x）`；
    · `…_tmp_view/w15_scan.txt:411` 里的 `:411` 指的是**那个文件里的行**，不是文件名的一部分；
    · 🔴 2026-10-24（T05 落地时现读发现）：源码字符串里的**字面量转义尾巴**会被正则一起吃掉
      （实测 `'…/matdump.log\\n'` → token 变成 `…/matdump.log\\n`，于是被误报成「目标已不在」）。
      那不是路径的一部分 ⇒ 摘掉尾部的 `\\n` / `\\r` / `\\t`。
    """
    p = p.rstrip('.,:;，。；、）)]}>')
    p = re.sub(r':\d+(?:[-–~]\d+)?$', '', p)
    p = re.sub(r'\\[nrt]+$', '', p)
    return p


def doc_line_set(path):
    """`.py` 里**注释 / 独立字符串（docstring）**所在的行号；`.md` 返回 None（整份都是文档）。

    为什么要有它：仪器脚本的**文件头长注释 / docstring** 里经常点名 `_tmp_view` 路径（讲历史），
    那不是「把 `_tmp_view` 当输入」⇒ 先把注释与 docstring 摘出去，报告才咬得住。
    ⚠️ **不能用 tokenize 的 STRING 全判文档**：路径常量本身就是字符串（`SNAP = r"d:/4/_tmp_view/…"`）
    —— 那会把**真正的代码行**全误判掉（第一版踩过）。这里只认 **`ast.Expr(Constant(str))`**
    （= docstring / 独立字符串语句）与 **COMMENT**。
    """
    if path.lower().endswith('.md'):
        return None
    if not path.lower().endswith('.py'):
        return set()
    lines = set()
    try:
        src = tokenize.open(path).read()
    except Exception:
        return lines
    try:
        import warnings
        with warnings.catch_warnings():
            warnings.simplefilter('ignore', SyntaxWarning)
            for node in ast.walk(ast.parse(src)):
                if isinstance(node, ast.Expr) and isinstance(node.value, ast.Constant) \
                        and isinstance(node.value.value, str):
                    for ln in range(node.lineno, (node.end_lineno or node.lineno) + 1):
                        lines.add(ln)
    except Exception:
        pass
    try:
        src_lines = src.split('\n')
        with tokenize.open(path) as f:
            for tok in tokenize.generate_tokens(f.readline):
                if tok.type == tokenize.COMMENT:
                    # ⚠️ 只有**行首注释**才算文档；行尾注释（`X = ...  # 说明`）那一行仍是代码。
                    ln = tok.start[0]
                    before = src_lines[ln - 1][:tok.start[1]] if ln - 1 < len(src_lines) else ''
                    if not before.strip():
                        lines.add(ln)
    except Exception:
        pass
    return lines


def _locate(rel_after_tv):
    """`_tmp_view/<sub>` 的 `<sub>` → 现在还在的绝对路径（两个根都试）。"""
    sub = rel_after_tv.replace('\\', '/')
    if not sub:
        return [r for r in TV_ROOTS if os.path.isdir(r)]
    for root in TV_ROOTS:
        p = os.path.join(root, sub)
        if os.path.exists(p):
            return [p]
    return []


def main():
    ap = argparse.ArgumentParser(description='`_tmp_view/` 第二份真相守卫（T05/A1948/A1977）—— 只报告')
    ap.add_argument('--include', action='append', default=None,
                    help=f'相对 d:/4/Unity 的 glob（可多次；默认 {DEFAULT_INCLUDES}）')
    ap.add_argument('--exclude-dir', action='append', default=None)
    ap.add_argument('--no-exclude-history', action='store_true')
    ap.add_argument('--top', type=int, default=20)
    ap.add_argument('--samples', type=int, default=3, help='每文件最多列几条（0=不列）')
    ap.add_argument('--fail-missing', action='store_true',
                    help='存在「被引用但文件已不在」时退出 1（默认只报告）')
    args = ap.parse_args()

    includes = args.include or list(DEFAULT_INCLUDES)
    exclude_dirs = tuple(args.exclude_dir) if args.exclude_dir else \
        (() if args.no_exclude_history else DEFAULT_EXCLUDE_DIRS)

    files, seen = [], set()
    for pat in includes:
        for p in _walk_glob(pat):
            rp = _norm(os.path.relpath(p, UNITY))
            if rp in seen:
                continue
            if any(rp == d or rp.startswith(d.rstrip('/') + '/') for d in exclude_dirs):
                continue
            seen.add(rp)
            files.append(p)

    rows = []          # (相对路径, 行号, 分类, 匹配token, 是否存在)
    n_read = n_write = n_unknown = n_doc = n_missing = 0
    missing = []

    for p in sorted(files):
        rp = _norm(os.path.relpath(p, UNITY))
        try:
            text = io.open(p, encoding='utf-8', errors='replace').read().split('\n')
        except Exception as e:
            print(f'⚠️ 读不了 {rp}：{e}', file=sys.stderr)
            continue
        doc_lines = doc_line_set(p)
        for ln_no, line in enumerate(text, 1):
            for m in TV.finditer(line):
                tok = _strip_tail(m.group('path'))
                is_doc = (doc_lines is None) or (ln_no in doc_lines) or \
                         (p.lower().endswith('.sh') and line.lstrip().startswith('#'))
                if is_doc:
                    cls = 'DOC'
                elif re.search(r"open\([^)]*['\"][wa]b?['\"]|to_csv|to_json|savefig|json\.dump|"
                               r"\.write\(|--out\b|>\s*['\"]?\S*_tmp_view|tee\b", line, re.I) \
                        or WRITE_HINT.search(line):
                    cls = 'WRITE'
                elif READ_HINT.search(line):
                    cls = 'READ'
                else:
                    cls = 'UNKNOWN'
                sub = tok.split('_tmp_view', 1)[1] if '_tmp_view' in tok else ''
                sub = sub.lstrip('/\\')
                exists = True
                if sub and not any(ch in sub for ch in '*?{%<>$'):
                    exists = bool(_locate(sub))
                if cls == 'READ':
                    n_read += 1
                elif cls == 'WRITE':
                    n_write += 1
                elif cls == 'DOC':
                    n_doc += 1
                else:
                    n_unknown += 1
                if cls != 'WRITE' and not exists:
                    n_missing += 1
                    missing.append((rp, ln_no, cls, tok))
                rows.append((rp, ln_no, cls, tok, exists))

    # ---- ③ A1948：过期仪器副本 ----
    stale = []
    for root in TV_ROOTS:
        if not os.path.isdir(root):
            continue
        for fn in sorted(os.listdir(root)):
            if not fn.endswith('.py'):
                continue
            fp = os.path.join(root, fn)
            try:
                t = io.open(fp, encoding='utf-8', errors='replace').read()
            except Exception:
                continue
            if STALE_ACT.search(t):
                stale.append((_norm(os.path.relpath(fp, REPO)), 'STALE `(\'ACT\', 46, …)`'))
    canon = os.path.join(UNITY, '工具', '_a1759_reconcile.py')
    canon_ok = os.path.isfile(canon) and bool(
        FIXED_ACT.search(io.open(canon, encoding='utf-8', errors='replace').read()))

    # ---- 报告 ----
    print('=== `_tmp_view/` 第二份真相守卫（T05 · A1948 · A1977）—— 只报告，不删文件 ===')
    print(f'扫描：{" · ".join(includes)}  ⇒ {len(files)} 个文件')
    print('排除：' + ('（无）' if not exclude_dirs else ' · '.join(exclude_dirs)))
    print(f'\n【引用分类】READ **{n_read}** 处 · UNKNOWN **{n_unknown}** 处 · '
          f'WRITE {n_write} 处 · DOC（注释/字符串/文档里的提及）{n_doc} 处')
    print('  ⚠️ READ/UNKNOWN = **可能在把 `_tmp_view` 当输入**；逐条见下面样本（启发式，须人工核）。')
    print(f'  🔴 其中「被引用但**目标文件已不在**」= **{n_missing}** 处'
          f'（= A1977 那类「写死过期路径」的现成候选；含文档里的死链）')
    for rp, ln_no, cls, tok in missing[:20]:
        print(f'      [{cls:<7}] {rp}:{ln_no}  {tok}')
    if len(missing) > 20:
        print(f'      …… 另有 {len(missing) - 20} 处')

    by_file = {}
    for rp, ln_no, cls, tok, ex in rows:
        by_file.setdefault(rp, []).append((ln_no, cls, tok, ex))
    if args.top:
        print(f'\n按文件（前 {args.top} 名）：')
        for rp, hits in sorted(by_file.items(), key=lambda kv: -len(kv[1]))[:args.top]:
            bad = sum(1 for _, c, _, e in hits if c != 'WRITE' and not e)
            print(f'  {len(hits):>6}  {rp}' + (f'  🔴 死链 {bad}' if bad else ''))

    if args.samples:
        print(f'\n样本（每个文件最多 {args.samples} 条）：')
        for rp, hits in sorted(by_file.items(), key=lambda kv: kv[0]):
            for ln_no, cls, tok, ex in hits[:args.samples]:
                flag = '' if ex else '   🔴 目标已不在'
                print(f'  [{cls:<7}] {rp}:{ln_no}  {tok}{flag}')

    print(f'\n【A1948 · 过期仪器副本】还在写 `(\'ACT\', 46, …)` 的 `_tmp_view/*.py`：**{len(stale)}** 份')
    for path, why in stale:
        print(f'  🔴 {path}  ——  {why}')
    print(f'  入仓正本 `Unity/工具/_a1759_reconcile.py` 已用 `ACT_TRIG`（A1937 修）：'
          f'{"✅ 是" if canon_ok else "❌ 不是（再去核）"}')
    print('  处置建议：这几份**不是**仪器正本，⛔ 别再跑；要复算一律跑入仓的 '
          '`工具/_a1759_reconcile.py`。本工具**不删任何文件**。')

    print('\n✅ 只报告：不改正文、不删文件。默认退出码 0。')
    if args.fail_missing and n_missing:
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
