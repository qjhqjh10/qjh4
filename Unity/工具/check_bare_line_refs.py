# -*- coding: utf-8 -*-
"""check_bare_line_refs.py -- 裸 `文件:行号` 引用守卫（账 A2060）。

**只报告，⛔ 不改正文。** 判据 / 背景：
  `资料/历史/按线台账_任务_卡组卡牌战斗_原文_20261015.md:433`（A2060）
  —— 两个写手各自往文件中间插行，就把 `DeckScene.cs:2939` 推成 `:2962`、
  把别人的引用点错位。⇒ 建议改成引**函数名 / 断言原文**；在改完之前，
  至少要能**数得出来、扫得到**。

它查三样：
  ① **路径限定**的裸引用：`<文件>.<ext>:<行号>`（含 `:12-16` 范围）；
  ② **无文件名**的裸 `:NNN`（同一文件内自引；按上下文推断，⛔ 不归因到具体文件）；
  ③ **可判死的坏引用**：目标文件不存在 / 行号超过目标文件总行数
     （这两种**不用重跑就能断定已经过期**，是这一族里唯一能自动判死的）。

用法（默认扫 `Unity/资料/**/*.md` + `Unity/MyGame/Assets/**/*.cs`，排除 `资料/历史/`）：
    python -I -X utf8 d:/4/Unity/工具/check_bare_line_refs.py
    python -I -X utf8 ... --include "资料/**/*.md" --include "MyGame/Assets/**/*.cs"
    python -I -X utf8 ... --no-exclude-history          # 连历史台账一起数
    python -I -X utf8 ... --whitelist 工具/check_bare_line_refs_whitelist.txt
    python -I -X utf8 ... --top 30 --samples 3
    python -I -X utf8 ... --fail-over 5000              # 当闸门用（默认永远退出 0）

白名单文件格式（`--whitelist`，可省）：一行一条**正则**（`#` 开头是注释），
匹配的是**相对 `d:/4/Unity` 的路径**（正斜杠）；命中的文件整份跳过。
"""
import argparse
import io
import os
import re
import sys

UNITY = 'd:/4/Unity'
REPO = 'd:/4'

# 已知扩展名才认（⛔ 防把 `12:30` / `http://x:8080` 当引用）。
# 路径里允许中文、空格以外的常见字符；`\s*:\s*` 容忍 `Foo.cs : 12`。
REF = re.compile(
    r'(?P<path>(?:[A-Za-z]:)?[A-Za-z0-9_./\\\-\u4e00-\u9fff]+\.'
    r'(?:cs|md|py|json|tsv|txt|unity|sh|yaml|yml|cpp|h))'
    r'\s*:\s*(?P<line>\d{1,6})(?:\s*[-–~]\s*(?P<line2>\d{1,6}))?')
# 无文件名的裸 `:NNN`：前面**不能**是路径/单词字符（防 `12:30`、`http://x:8080`）。
LONE = re.compile(r'(?<![\w./\\\-]):(?P<line>\d{1,6})\b')

DEFAULT_INCLUDES = ('资料/**/*.md', 'MyGame/Assets/**/*.cs')
DEFAULT_EXCLUDE_DIRS = ('资料/历史',)


def _norm(p):
    return p.replace('\\', '/').lstrip('./')


def _walk_glob(rel_pattern, root=UNITY):
    """极简 glob（只支持 `dir/**/*.ext` / `dir/*.ext` / **单个文件路径**）。"""
    rel = _norm(rel_pattern)
    if '*' not in rel:
        p = os.path.join(root, rel)
        if os.path.isfile(p):
            yield p
        return
    if '/**/' in rel:
        base, tail = rel.split('/**/', 1)
        ext = None
        if tail.startswith('*.'):
            ext = tail[1:]
        start = os.path.join(root, base)
        for dp, dns, fns in os.walk(start):
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


def collect_files(includes, exclude_dirs, whitelist):
    out, seen = [], set()
    for pat in includes:
        for p in _walk_glob(pat):
            rp = _norm(os.path.relpath(p, UNITY))
            if rp in seen:
                continue
            if any(rp == d or rp.startswith(d.rstrip('/') + '/') for d in exclude_dirs):
                continue
            if any(pat_re.search(rp) for pat_re in whitelist):
                continue
            seen.add(rp)
            out.append(os.path.join(UNITY, rp))
    return sorted(out)


def build_repo_index():
    """把「会被引用的那些文件」全收进来（`.cs/.md/.py/.sh/.json/.tsv/.txt/.unity`）。

    为什么要它（⛔ 否则「目标不存在」会有一大半是假警报）：仓里的引用大量是**简写**——
    `Deck/DeckRuntime.cs`（真身 `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs`）、
    `项目任务.md`（真身在 `Unity/` 根）……只按固定 base 拼是解析不出来的。
    """
    idx = {}   # 相对 d:/4/Unity 的路径（正斜杠）-> 绝对路径
    for rel_root, exts in (('MyGame/Assets', ('.cs', '.unity')),
                           ('资料', ('.md', '.json', '.tsv', '.txt')),
                           ('工具', ('.py', '.sh', '.md'))):
        root = os.path.join(UNITY, rel_root)
        if not os.path.isdir(root):
            continue
        for dp, dns, fns in os.walk(root):
            dns[:] = [d for d in dns if d not in ('.git', 'Library', 'Temp', 'obj', 'Logs', '__pycache__')]
            for fn in fns:
                if os.path.splitext(fn)[1].lower() not in exts:
                    continue
                p = os.path.join(dp, fn)
                idx[_norm(os.path.relpath(p, UNITY))] = p
    # `项目任务.md` 等正本在 `Unity/` 根
    for fn in os.listdir(UNITY):
        p = os.path.join(UNITY, fn)
        if os.path.isfile(p) and os.path.splitext(fn)[1].lower() in ('.md', '.json', '.txt'):
            idx[_norm(fn)] = p
    return idx


def resolve_target(path_token, repo_idx):
    """把引用里的路径解析成实际文件。返回 (绝对路径 | None, 解析方式)。

    解析顺序：① 精确相对路径 ② 路径**后缀**唯一匹配（仓里的简写）③ 常见 base 拼接
    ④ basename 唯一匹配。`ambiguous` 与 `missing` **分开报**（前者不一定是坏引用）。
    """
    t = _norm(path_token)
    if t.startswith('d:/') or t.startswith('D:/'):
        return (t if os.path.isfile(t) else None), ('abs' if os.path.isfile(t) else 'missing')
    if t in repo_idx:
        return repo_idx[t], 'exact'
    # ② 后缀匹配：允许 `Deck/DeckRuntime.cs` 命中 `MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs`
    suffix = '/' + t
    hits = [p for rel, p in repo_idx.items() if rel.endswith(suffix)]
    if len(hits) == 1:
        return hits[0], 'suffix'
    if len(hits) > 1:
        return None, f'ambiguous({len(hits)})'
    # ③ 常见 base 拼接（防 index 没覆盖到的目录）
    for base in (UNITY, REPO, os.path.join(UNITY, 'MyGame', 'Assets'),
                 os.path.join(UNITY, 'MyGame', 'Assets', 'CardPresentation')):
        p = os.path.normpath(os.path.join(base, t))
        if os.path.isfile(p):
            return p, 'rel'
    # ④ basename 唯一匹配
    bn = os.path.basename(t)
    hits = [p for rel, p in repo_idx.items() if os.path.basename(rel) == bn]
    if len(hits) == 1:
        return hits[0], 'basename'
    if len(hits) > 1:
        return None, f'ambiguous({len(hits)})'
    return None, 'missing'


def count_lines(path):
    try:
        with io.open(path, 'rb') as f:
            return f.read().count(b'\n') + 1
    except Exception:
        return 0


def main():
    ap = argparse.ArgumentParser(description='裸 `文件:行号` 引用守卫（A2060）—— 只报告，不改正文')
    ap.add_argument('--include', action='append', default=None,
                    help=f'相对 d:/4/Unity 的 glob（可多次；默认 {DEFAULT_INCLUDES}）')
    ap.add_argument('--exclude-dir', action='append', default=None,
                    help='相对 d:/4/Unity 的目录前缀（可多次；默认排除 资料/历史）')
    ap.add_argument('--no-exclude-history', action='store_true', help='连 资料/历史/ 一起数')
    ap.add_argument('--whitelist', default=None, help='白名单文件：一行一条正则（匹配相对路径）')
    ap.add_argument('--top', type=int, default=20, help='按文件列出前 N 名（默认 20）')
    ap.add_argument('--samples', type=int, default=3, help='每个文件最多列几条样本（默认 3；0=不列）')
    ap.add_argument('--lone', action='store_true', help='同时统计「无文件名的裸 :NNN」')
    ap.add_argument('--fail-over', type=int, default=None,
                    help='路径限定引用总数超过它时退出 1（默认 0 = 只报告）')
    args = ap.parse_args()

    includes = args.include or list(DEFAULT_INCLUDES)
    if args.exclude_dir:
        exclude_dirs = tuple(args.exclude_dir)
    elif args.no_exclude_history:
        exclude_dirs = ()
    else:
        exclude_dirs = DEFAULT_EXCLUDE_DIRS

    whitelist = []
    wl = args.whitelist
    if wl is None:
        # 约定：工具目录下若有同名白名单就**自动加载**（没有就等同「没白名单」）。
        cand = os.path.join(UNITY, '工具', 'check_bare_line_refs_whitelist.txt')
        if os.path.isfile(cand):
            wl = cand
    if wl:
        if not os.path.isabs(wl):
            wl = os.path.join(UNITY, wl)
        if not os.path.isfile(wl):
            sys.exit(f'白名单文件不存在：{wl}')
        for ln in io.open(wl, encoding='utf-8'):
            ln = ln.strip()
            if ln and not ln.startswith('#'):
                whitelist.append(re.compile(ln))

    files = collect_files(includes, exclude_dirs, whitelist)
    repo_idx = build_repo_index()

    per_file = []          # (相对路径, [样本], 引用数)
    n_refs = n_ranges = n_missing = n_ambiguous = n_range_bad = 0
    n_lone = 0
    lone_samples = []

    for p in files:
        rp = _norm(os.path.relpath(p, UNITY))
        try:
            text = io.open(p, encoding='utf-8', errors='replace').read().split('\n')
        except Exception as e:
            print(f'⚠️ 读不了 {rp}：{e}', file=sys.stderr)
            continue
        hits = []
        for ln_no, line in enumerate(text, 1):
            for m in REF.finditer(line):
                tok = m.group('path')
                line_no = int(m.group('line'))
                line2 = int(m.group('line2')) if m.group('line2') else None
                # 同一条里可能既有路径引用、又有裸 :NNN ⇒ 先记路径引用，下面再数裸的。
                n_refs += 1
                if line2 is not None:
                    n_ranges += 1
                tgt, how = resolve_target(tok, repo_idx)
                bad = None
                if tgt is None:
                    if how.startswith('ambiguous'):
                        n_ambiguous += 1
                        bad = f'TARGET-{how}'
                    else:
                        n_missing += 1
                        bad = 'TARGET-missing'
                else:
                    total = count_lines(tgt)
                    hi = line2 if line2 is not None else line_no
                    if hi > total or line_no < 1:
                        bad = f'OUT-OF-RANGE({hi}>{total})'
                        n_range_bad += 1
                hits.append((ln_no, m.group(0), bad))
            if args.lone:
                rest = REF.sub(' ', line)          # 先挖掉路径引用，剩下的才数裸 `:NNN`
                for m in LONE.finditer(rest):
                    n_lone += 1
                    if len(lone_samples) < 10:
                        lone_samples.append((rp, ln_no, m.group(0), line.strip()[:100]))
        if hits:
            per_file.append((rp, hits))

    per_file.sort(key=lambda x: (-len(x[1]), x[0]))

    print('=== 裸 `文件:行号` 引用守卫（A2060）—— 只报告，不改正文 ===')
    print(f'扫描：{" · ".join(includes)}  ⇒ {len(files)} 个文件')
    print('排除：' + ('（无）' if not exclude_dirs else ' · '.join(exclude_dirs)))
    if whitelist:
        print(f'白名单：{wl}（{len(whitelist)} 条）')
    print(f'\n【路径限定引用】共 **{n_refs}** 处，涉及 **{len(per_file)}** 个文件'
          f'（其中带范围 `:a-b` 的 {n_ranges} 处）')
    print(f'  🔴 目标文件不存在：**{n_missing}** 处（TARGET-missing，可判死）')
    print(f'  🔴 行号越界（可判死）：**{n_range_bad}** 处')
    print(f'  ⚠️ 目标名命中多份、无法判定：{n_ambiguous} 处（TARGET-ambiguous，⛔ 不算坏、但要人工看）')
    print(f'  ⚠️ 其余 {n_refs - n_missing - n_ambiguous - n_range_bad} 处**目前解析得通** —— '
          f'但仍是裸数字：只要有人在目标文件中间插一行，它就会**静默指错**。')

    if args.top:
        print(f'\n按文件（前 {args.top} 名）：')
        for rp, hits in per_file[:args.top]:
            bads = sum(1 for _, _, b in hits if b and not b.startswith('TARGET-ambiguous'))
            amb = sum(1 for _, _, b in hits if b and b.startswith('TARGET-ambiguous'))
            flag = (f'  🔴 坏 {bads}' if bads else '') + (f'  ⚠️ 多义 {amb}' if amb else '')
            print(f'  {len(hits):>6}  {rp}{flag}')
        if len(per_file) > args.top:
            print(f'  …… 另有 {len(per_file) - args.top} 个文件')

    if args.samples:
        print(f'\n样本（每个文件最多 {args.samples} 条）：')
        for rp, hits in per_file:
            show = hits[:args.samples]
            for ln_no, txt, bad in show:
                tail = f'   🔴 {bad}' if bad else ''
                print(f'  {rp}:{ln_no}  {txt}{tail}')

    if args.lone:
        print(f'\n【无文件名的裸 `:NNN`（同文件自引，⛔ 无法归因）】共 **{n_lone}** 处')
        for rp, ln_no, tok, ctx in lone_samples:
            print(f'  {rp}:{ln_no}  {tok}   … {ctx}')

    print('\n✅ 本工具**只报告**：不改任何正文，默认退出码 0。'
          '要当闸门用 `--fail-over N`。')
    if args.fail_over is not None and n_refs > args.fail_over:
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main())
