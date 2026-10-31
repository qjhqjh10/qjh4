# -*- coding: utf-8 -*-
"""gen_u2_loaded_probe.py — R0-09「真加载」探针（账 N56）

判据（`资料/完全复刻/判据/B107_补判据_新账15条.md` N56）：

  · **L1**：某个**非 Editor** 运行时代码路径以该资产名为实参调用加载 API
    （`Resources.Load` / `AssetBundle.LoadAsset` / `Addressables.LoadAsset*` /
    `VideoPlayer.url` / 卡表→`Load(name)` 的**同一串**）。
  · **L2**：它出现在一张**被这样的代码读取**的表里、且表值就是加载实参同一串。
  · **L3**：只命中表串而无加载调用 = 「仅名字在表」，**不算真加载**。
  · **Unresolved**：动态实参解不出取值集合 ⇒ 单列（既不充真加载、也不判没加载）。
  排除：`Assets/**/Editor/**`、`#if UNITY_EDITOR`、自检夹具。

本脚本**只读**（除 `--write` 外不写任何文件），产出：

  `资料/完全复刻/报告/B107_R0_U2真加载核查.md`

用法：

    python -I -X utf8 d:/4/Unity/工具/gen_u2_loaded_probe.py          # 只打印用法，退出 0
    python -I -X utf8 d:/4/Unity/工具/gen_u2_loaded_probe.py --write  # 生成/覆盖报告
    python -I -X utf8 d:/4/Unity/工具/gen_u2_loaded_probe.py --check  # 重算与盘上报告比（忽略生成时间行），一致 0 / 不一致 1
    python -I -X utf8 d:/4/Unity/工具/gen_u2_loaded_probe.py --write --n 200
    python -I -X utf8 d:/4/Unity/工具/gen_u2_loaded_probe.py --write --tsv   # 顺带把逐条结果打到 stdout

⛔ 本脚本**不改主账**（`R0_资源覆盖账.tsv` 一个字节都不写）。
"""

import io
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))  # d:/4/Unity
LEDGER = os.path.join(ROOT, '资料', '完全复刻', '资源', 'R0_资源覆盖账.tsv')
REPORT = os.path.join(ROOT, '资料', '完全复刻', '报告', 'B107_R0_U2真加载核查.md')
ASSETS = os.path.join(ROOT, 'MyGame', 'Assets')
DATADIR = os.path.join(ROOT, '数据')
UNUSED_MD = os.path.join(ROOT, '资料', '完全复刻', '资源', 'R0_未使用清单.md')

BAND_U2 = '已用（数据表）'
DEFAULT_N = 200
MIN_PER_TYPE = 10          # 分层：每类至少抽这么多（余量再按占比分）
MAX_TABLE_BYTES = 40 * 1024 * 1024   # 单表 > 40 MB 不读入（本仓现读最大 ~7 MB；超过就跳过并记数）

USAGE = u"""gen_u2_loaded_probe.py — R0-09「真加载」探针（账 N56，只读）

用法：
    python -I -X utf8 d:/4/Unity/工具/gen_u2_loaded_probe.py            只打印本用法并退出 0
    python -I -X utf8 d:/4/Unity/工具/gen_u2_loaded_probe.py --write    生成/覆盖报告
    python -I -X utf8 d:/4/Unity/工具/gen_u2_loaded_probe.py --check    重算并与盘上报告比较（幂等）

选项：
    --n N        抽样条数（默认 200）
    --tsv        把逐条结果同时打到 stdout（TSV，制表符分隔）
    -h, --help   同「不带参」：只打印用法并退出 0

等级定义（判据 = 判据/B107_补判据_新账15条.md N56）：
    L1  非 Editor 运行时代码以该名字为实参调用加载 API（含 CardArt.Get 一类包装口）
    L2  名字是被这样代码读取的表里的值，且该表的值就是加载实参同一串
    L3  仅名字在表（含：表没有运行时读取者 / 表里是键或审计 dump）
    U   动态实参，静态解不出（单列，不充真加载也不判没加载）
"""


# ----------------------------------------------------------------------------
# 参数
# ----------------------------------------------------------------------------
def parse_args(argv):
    opt = {'write': False, 'check': False, 'tsv': False, 'n': DEFAULT_N, 'help': False}
    i = 1
    while i < len(argv):
        a = argv[i]
        if a in ('-h', '--help'):
            opt['help'] = True
        elif a == '--write':
            opt['write'] = True
        elif a == '--check':
            opt['check'] = True
        elif a == '--tsv':
            opt['tsv'] = True
        elif a == '--n':
            i += 1
            if i >= len(argv):
                return None
            try:
                opt['n'] = int(argv[i])
            except ValueError:
                return None
            if opt['n'] <= 0:
                return None
        else:
            return None
        i += 1
    if (opt['write'] and opt['check']) or (not opt['write'] and not opt['check'] and not opt['help']):
        # 不带参 = 只打印用法；--write / --check 二选一
        if not opt['help']:
            return None
    return opt


# ----------------------------------------------------------------------------
# 源码语料
# ----------------------------------------------------------------------------
def strip_editor_blocks(lines):
    """去掉 `#if UNITY_EDITOR` 段（保留行号：被剔的行换成空串）。"""
    out = []
    stack = []            # 每层：#if 条件是否 editor-only
    for ln in lines:
        s = ln.lstrip()
        if s.startswith('#'):
            kw = s[1:].split(None, 1)
            key = kw[0] if kw else ''
            cond = kw[1] if len(kw) > 1 else ''
            if key == 'if':
                is_ed = ('UNITY_EDITOR' in cond) and ('!UNITY_EDITOR' not in cond.replace(' ', ''))
                stack.append(is_ed)
            elif key in ('else', 'elif'):
                if stack:
                    if key == 'else':
                        stack[-1] = (not stack[-1]) if ('UNITY_EDITOR' in cond or not cond) else stack[-1]
                    else:
                        stack[-1] = ('UNITY_EDITOR' in cond) and ('!UNITY_EDITOR' not in cond.replace(' ', ''))
            elif key == 'endif':
                if stack:
                    stack.pop()
        if any(stack):
            out.append('')     # 行号保留
        else:
            out.append(ln)
    return out


def strip_comments(text):
    """去 `//` 与 `/* */` 注释（保留字符串里的内容），按字符扫描。"""
    out = []
    i = 0
    n = len(text)
    instr = False
    while i < n:
        c = text[i]
        if instr:
            out.append(c)
            if c == '\\' and i + 1 < n:
                out.append(text[i + 1])
                i += 2
                continue
            if c == '"':
                instr = False
            i += 1
            continue
        if c == '"':
            instr = True
            out.append(c)
            i += 1
            continue
        if c == '/' and i + 1 < n and text[i + 1] == '/':
            j = text.find('\n', i)
            if j < 0:
                break
            out.append('\n')
            i = j + 1
            continue
        if c == '/' and i + 1 < n and text[i + 1] == '*':
            j = text.find('*/', i + 2)
            if j < 0:
                break
            out.append('\n' * text.count('\n', i, j))
            i = j + 2
            continue
        out.append(c)
        i += 1
    return ''.join(out)


LIT_RE = re.compile(r'"((?:[^"\\]|\\.)*)"')
# 加载实参里出现过的「成员名」（`Load(x.clip)` ⇒ clip）—— L2 的字段级证据
LOAD_ARG_MEMBERS = set()
LOAD_PATTERNS = [
    ('Resources.Load', re.compile(r'\bResources\.Load(?:All)?\s*(?:<[^<>()]*>)?\s*\(')),
    ('LoadAsset', re.compile(r'\.Load(?:All)?Asset(?:s)?\s*(?:<[^<>()]*>)?\s*\(')),
    ('Addressables', re.compile(r'\bAddressables\.LoadAsset\w*\s*(?:<[^<>()]*>)?\s*\(')),
    ('VideoPlayer.url', re.compile(r'\.url\s*=')),
]


def unescape_lit(s):
    return s.replace('\\"', '"').replace('\\\\', '\\').replace('\\n', '\n').replace('\\t', '\t')


def split_concat(expr):
    """按顶层 '+' 切表达式（不切字符串里的 +）。"""
    parts = []
    cur = []
    instr = False
    i = 0
    while i < len(expr):
        c = expr[i]
        if instr:
            cur.append(c)
            if c == '\\' and i + 1 < len(expr):
                cur.append(expr[i + 1])
                i += 2
                continue
            if c == '"':
                instr = False
        else:
            if c == '"':
                instr = True
                cur.append(c)
            elif c == '+':
                parts.append(''.join(cur).strip())
                cur = []
            else:
                cur.append(c)
        i += 1
    parts.append(''.join(cur).strip())
    return [p for p in parts if p]


def arg_of(line, start):
    """从 '(' 之后取实参（单行内括号配平；跨行返回 None）。"""
    depth = 1
    i = start
    out = []
    instr = False
    while i < len(line):
        c = line[i]
        if instr:
            out.append(c)
            if c == '\\' and i + 1 < len(line):
                out.append(line[i + 1])
                i += 2
                continue
            if c == '"':
                instr = False
        else:
            if c == '"':
                instr = True
                out.append(c)
            elif c == '(':
                depth += 1
                out.append(c)
            elif c == ')':
                depth -= 1
                if depth == 0:
                    return ''.join(out).strip()
                out.append(c)
            elif c == ';' and depth == 0:
                return ''.join(out).strip()
            else:
                out.append(c)
        i += 1
    return None


def classify_arg(expr, consts):
    """返回 (literal, prefix, dynamic)。literal = 完全字面量；prefix = 前导字面量。"""
    if expr is None:
        return (None, None, True)
    parts = split_concat(expr)
    if not parts:
        return (None, None, True)
    vals = []
    dyn_at = None
    for k, p in enumerate(parts):
        m = LIT_RE.fullmatch(p)
        if m:
            vals.append(unescape_lit(m.group(1)))
        elif p in consts:
            vals.append(consts[p])
        else:
            dyn_at = k
            vals.append(None)
    if dyn_at is None:
        return (''.join(v for v in vals if v is not None), None, False)
    # 末尾一边是动态（常见：prefix + name）
    lead = vals[:dyn_at]
    if all(v is not None for v in lead) and all(v is None for v in vals[dyn_at:]):
        return (None, ''.join(lead), True)
    return (None, None, True)


def basename_of(s):
    if s is None:
        return None
    s = s.replace('\\', '/')
    if '/' in s:
        s = s.rsplit('/', 1)[1]
    return s


CONST_RE = re.compile(r'\b(?:const|static\s+readonly|static)\s+string\s+([A-Za-z_]\w*)\s*=\s*"((?:[^"\\]|\\.)*)"')


def walk_cs():
    for dp, dn, fn in os.walk(ASSETS):
        dn[:] = sorted(d for d in dn if d.lower() != 'editor')
        for f in sorted(fn):
            if f.endswith('.cs'):
                yield os.path.join(dp, f)


def rel(p):
    return os.path.relpath(p, ASSETS).replace('\\', '/')


def build_code_index():
    """返回：
       literals: {literal: [(rel, line, is_load_site, label)]}
       load_sites: [(rel, line, api, literal|None, prefix|None, dynamic)]
       files: {rel: [lines]}
       consts_by_file: {rel: {name: value}}
       text_by_file: {rel: 全文（去注释）}
    """
    literals = {}
    load_sites = []
    files = {}
    consts_by_file = {}
    text_by_file = {}
    for p in walk_cs():
        try:
            raw = io.open(p, 'r', encoding='utf-8', errors='replace').read()
        except OSError:
            continue
        r = rel(p)
        no_comments = strip_comments(raw)
        lines = strip_editor_blocks(no_comments.splitlines())
        files[r] = lines
        text_by_file[r] = '\n'.join(lines)
        consts = {}
        for ln in lines:
            m = CONST_RE.search(ln)
            if m:
                consts[m.group(1)] = unescape_lit(m.group(2))
        consts_by_file[r] = consts
        for idx, ln in enumerate(lines, 1):
            for lm in LIT_RE.finditer(ln):
                lit = unescape_lit(lm.group(1))
                if not lit:
                    continue
                literals.setdefault(lit, []).append((r, idx, False, 'literal'))
            for api, pat in LOAD_PATTERNS:
                m = pat.search(ln)
                if not m:
                    continue
                if api == 'VideoPlayer.url':
                    arg = ln[m.end():].strip().rstrip(';')
                else:
                    arg = arg_of(ln, m.end())
                lit, prefix, dyn = classify_arg(arg, consts)
                load_sites.append((r, idx, api, lit, prefix, dyn))
                if arg:
                    for tok in re.findall(r'\.\s*([A-Za-z_]\w*)', arg):
                        LOAD_ARG_MEMBERS.add(tok.lower())
                if lit:
                    literals.setdefault(lit, []).append((r, idx, True, api))
    return literals, load_sites, files, consts_by_file, text_by_file


METHOD_DECL_RE = re.compile(r'\b([A-Za-z_]\w*)\s*\(([^()]*)\)\s*(?:\{|$)')


def _wrapper_regex(wrappers):
    if not wrappers:
        return None
    return re.compile(r'\b(?:' + '|'.join(re.escape(w) for w in sorted(wrappers, key=len, reverse=True)) + r')\s*\(')


KEYWORDS = ('if', 'for', 'foreach', 'while', 'switch', 'return', 'new', 'catch', 'using', 'lock')


def method_decls(lines):
    """返回 [(行下标0, 方法名, 形参串)] —— 只认**像声明**的行：
    · depth ≥ 1（命名空间 ≥0、类体 ≥1、方法体 ≥2）
    · 行尾不是 `;`（排除调用语句）· `=` 不在 '(' 之前（排除赋值右侧）
    · 名字不是关键字
    ⚠️ 花括号按行数（插值串里的 `{}` 成对出现 ⇒ 净 0；已知局限）。"""
    decls = []
    depth = 0
    for i, ln in enumerate(lines):
        s = ln.strip()
        if depth >= 1 and s and not s.startswith('.'):
            dm = METHOD_DECL_RE.search(ln)
            if dm and dm.group(1) not in KEYWORDS:
                head = ln[:dm.start(1)]
                if ('=' not in head) and (not s.endswith(';')):
                    decls.append((i, dm.group(1), dm.group(2)))
        depth += ln.count('{') - ln.count('}')
    return decls


def enclosing_decl(decls, i):
    lo, hi = 0, len(decls)
    while lo < hi:
        mid = (lo + hi) // 2
        if decls[mid][0] <= i:
            lo = mid + 1
        else:
            hi = mid
    return decls[lo - 1] if lo > 0 else None


def build_wrappers(files):
    """找出「以形参为实参调 Resources.Load/LoadAsset」的包装方法（1 层 + 传递到第 2 层）。"""
    wrappers = {}   # name -> (rel, line)
    decls_by_file = {}
    for r, lines in files.items():
        decls = method_decls(lines)
        decls_by_file[r] = decls
        for i, ln in enumerate(lines):
            for api, pat in LOAD_PATTERNS:
                m = pat.search(ln)
                if not m:
                    continue
                arg = arg_of(ln, m.end()) if api != 'VideoPlayer.url' else None
                if arg is None:
                    continue
                parts = split_concat(arg)
                if len(parts) != 1 or not re.fullmatch(r'[A-Za-z_]\w*', parts[0]):
                    continue
                param = parts[0]
                dm = enclosing_decl(decls, i)
                if dm and re.search(r'\bstring\s+' + re.escape(param) + r'\b', dm[2]):
                    wrappers.setdefault(dm[1], (r, dm[0] + 1))
    # 第二层：包装 A 把**自己的 string 形参**喂给包装 B（只认这种，⛔ 否则「任何调 Get 的方法」全被登记 = 雪崩）
    for _ in range(2):
        wr = _wrapper_regex(wrappers)
        if wr is None:
            break
        add = {}
        for r, lines in files.items():
            decls = decls_by_file[r]
            for i, ln in enumerate(lines):
                if '(' not in ln or wr.search(ln) is None:
                    continue
                dm = enclosing_decl(decls, i)
                if not dm:
                    continue
                params = re.findall(r'\bstring\s+([A-Za-z_]\w*)', dm[2])
                if not params:
                    continue
                m = wr.search(ln)
                arg = arg_of(ln, m.end()) or ''
                if any(re.search(r'\b' + re.escape(p) + r'\b', arg) for p in params):
                    add.setdefault(dm[1], (r, dm[0] + 1))
        before = len(wrappers)
        for k, v in add.items():
            wrappers.setdefault(k, v)
        if len(wrappers) == before:
            break
    return wrappers


def build_wrapper_sites(files, consts_by_file, wrappers):
    """包装方法被以字面量/前缀调用 → 视为加载实参。"""
    sites = []
    wr = _wrapper_regex(wrappers)
    if wr is None:
        return sites
    name_re = re.compile(r'\w+')
    for r, lines in files.items():
        consts = consts_by_file.get(r, {})
        for idx, ln in enumerate(lines, 1):
            if '(' not in ln:
                continue
            m = wr.search(ln)
            if m is None:
                continue
            w = name_re.match(m.group(0)).group(0)
            arg = arg_of(ln, m.end())
            lit, prefix, dyn = classify_arg(arg, consts)
            if arg:
                for tok in re.findall(r'\.\s*([A-Za-z_]\w*)', arg):
                    LOAD_ARG_MEMBERS.add(tok.lower())
            if lit is None and prefix is None:
                continue
            sites.append((r, idx, 'wrap:' + w, lit, prefix, dyn))
    return sites


# ----------------------------------------------------------------------------
# 表语料
# ----------------------------------------------------------------------------
def json_files():
    for p in walk_dir(ASSETS, '.json'):
        yield p
    dataname = os.path.basename(DATADIR)
    for p in walk_dir(DATADIR, '.json', skip_dirs=('索引',)):
        yield p


def walk_dir(base, ext, skip_dirs=()):
    skip = set(d.lower() for d in skip_dirs)
    for dp, dn, fn in os.walk(base):
        dn[:] = sorted(d for d in dn if d.lower() not in skip)
        for f in sorted(fn):
            if f.endswith(ext):
                yield os.path.join(dp, f)


def walk_json_strings(obj, keypath, out, depth=0):
    if depth > 8:
        return
    if isinstance(obj, str):
        out.append((keypath, obj, 'value'))
    elif isinstance(obj, dict):
        for k, v in obj.items():
            out.append((keypath + (k,), k, 'key'))
            walk_json_strings(v, keypath + (k,), out, depth + 1)
    elif isinstance(obj, list):
        for i, v in enumerate(obj):
            walk_json_strings(v, keypath + ('[%d]' % i,), out, depth + 1)


def build_table_index():
    """{value: [(rel_or_abs, line, keypath, kind, runtime_read)]}
       另返回 stats dict。"""
    idx = {}
    stats = {'files': 0, 'skipped_big': [], 'bad': []}
    for p in json_files():
        try:
            sz = os.path.getsize(p)
        except OSError:
            continue
        if sz > MAX_TABLE_BYTES:
            stats['skipped_big'].append((os.path.relpath(p, ROOT), sz))
            continue
        try:
            raw = io.open(p, 'r', encoding='utf-8', errors='replace').read()
        except OSError:
            continue
        try:
            obj = json.loads(raw)
        except Exception:
            stats['bad'].append(os.path.relpath(p, ROOT).replace('\\', '/'))
            continue
        stats['files'] += 1
        relp = os.path.relpath(p, ROOT).replace('\\', '/')
        # 字面量 → 首个行号（增量数换行：⛔ 别对每个 match 都从 0 数一遍 = O(n²)）
        lit_line = {}
        pos = 0
        ln = 1
        for m in LIT_RE.finditer(raw):
            ln += raw.count('\n', pos, m.start())
            pos = m.start()
            lit = unescape_lit(m.group(1))
            if lit not in lit_line:
                lit_line[lit] = ln
        found = []
        walk_json_strings(obj, (), found)
        for kp, val, kind in found:
            if not val or len(val) < 3:
                continue
            line = lit_line.get(val, 1)
            idx.setdefault(val, []).append((relp, line, '/'.join(str(x) for x in kp), kind))
    return idx, stats


TEXT_EXTS = ('.tsv', '.csv', '.md', '.txt')


def build_text_index():
    """`数据/**/*.{tsv,csv,md,txt}` 的非空行（账的 U2 语料含这几类；本探针按**行子串**命中）。"""
    out = []
    for dp, dn, fn in os.walk(DATADIR):
        dn[:] = sorted(d for d in dn if d != '索引')
        for f in sorted(fn):
            if not f.lower().endswith(TEXT_EXTS):
                continue
            p = os.path.join(dp, f)
            try:
                raw = io.open(p, 'r', encoding='utf-8', errors='replace').read()
            except OSError:
                continue
            relp = os.path.relpath(p, ROOT).replace('\\', '/')
            for i, line in enumerate(raw.splitlines(), 1):
                if line.strip():
                    out.append((relp, i, line.lower()))
    return out


def build_table_loader_index(sites):
    """表基名 → 加载它的代码点 [(rel, line)]。
    判据 = 该基名作为**加载实参**的 basename 出现（`Resources.Load<TextAsset>("X")` 一类）。
    ⚠️ 不能拿「代码里提到过这个 token」当读取者 —— 资产名与表名撞车会假阳（实证：`BulletImpact_2shot_trail.json`
    只是个 Godot 历史快照，而同名资产在 `VfxMap.cs` 里被加载）。"""
    idx = {}
    for (r, ln, api, lit, prefix, dyn) in sites:
        if not lit:
            continue
        b = basename_of(lit)
        if b:
            idx.setdefault(b, []).append((r, ln))
    return idx


# ----------------------------------------------------------------------------
# 抽样
# ----------------------------------------------------------------------------
def read_u2_rows():
    rows = []
    with io.open(LEDGER, 'r', encoding='utf-8') as f:
        hdr = f.readline().rstrip('\n').split('\t')
        for line in f:
            c = line.rstrip('\n').split('\t')
            if len(c) != len(hdr):
                continue
            if c[4] == BAND_U2:
                rows.append(c)
    return hdr, rows


def allocate(counts, n, minimum):
    types = sorted(counts, key=lambda t: (-counts[t], t))
    total = sum(counts[t] for t in types)
    if total <= 0:
        return {}
    # 每类至少 min(minimum, 该类总数)；缺口按容量加权再分（小类凑不满时自动让给大类）
    alloc = {t: min(minimum, counts[t]) for t in types}
    left = n - sum(alloc.values())
    guard = 0
    while left > 0 and guard < 1000:
        guard += 1
        cap = {t: counts[t] - alloc[t] for t in types if counts[t] > alloc[t]}
        if not cap:
            break
        tot = float(sum(cap.values()))
        exact = {t: left * cap[t] / tot for t in cap}
        gave = 0
        for t in cap:
            add = min(int(exact[t]), cap[t])
            if add:
                alloc[t] += add
                gave += add
        left -= gave
        if gave == 0:
            for t in sorted(cap, key=lambda x: (-(exact[x] - int(exact[x])), x)):
                if left <= 0:
                    break
                alloc[t] += 1
                left -= 1
    return alloc


def pid_key(s):
    try:
        return (0, int(s))
    except ValueError:
        return (1, s)


def sample(rows, n, minimum):
    by_type = {}
    for r in rows:
        by_type.setdefault(r[2], []).append(r)
    counts = {t: len(v) for t, v in by_type.items()}
    alloc = allocate(counts, n, minimum)
    out = []
    for t in sorted(alloc, key=lambda x: (-counts[x], x)):
        k = alloc[t]
        lst = sorted(by_type[t], key=lambda r: (r[0], pid_key(r[1])))
        m = len(lst)
        if k <= 0 or m == 0:
            continue
        if k >= m:
            picks = lst
        else:
            picks = [lst[int((i + 0.5) * m / k)] for i in range(k)]
        out.extend(picks)
    return out, alloc, counts


# ----------------------------------------------------------------------------
# 判定
# ----------------------------------------------------------------------------
def build_literal_name_index(literals):
    """literal 全串 + 路径 basename 两条键 → 出现点（同名去重，加载点优先）。"""
    idx = {}
    for lit, occs in literals.items():
        keys = [lit]
        b = basename_of(lit)
        if b and b != lit and '/' in lit:
            keys.append(b)
        for k in keys:
            idx.setdefault(k, []).extend(occs)
    # 去重：(rel,line) 只留一条，is_load 优先
    for k in idx:
        best = {}
        for (r, ln, is_load, label) in idx[k]:
            key = (r, ln)
            cur = best.get(key)
            if cur is None or (is_load and not cur[0]):
                best[key] = (is_load, label)
        idx[k] = [(r, ln, v[0], v[1]) for (r, ln), v in best.items()]
    return idx


def find_evidence_literal(name, literals, load_sites):
    """名字（或其路径 basename）作为字面量出现在代码里 → 返回 (等级, 证据, 说明, 位置文件)。"""
    occs = literals.get(name)
    if not occs:
        return None
    cands = []
    for (r, ln, is_load, label) in occs:
        cands.append((0 if is_load else 1, r, ln, is_load, label, name))
    cands.sort(key=lambda x: (x[0], x[1], x[2]))
    best = cands[0]
    if best[3]:
        return ('L1', '%s:%d' % (best[1], best[2]),
                u'字面量作加载实参（%s）' % (best[4],), best[1])
    return (None, None, u'代码里有同名字面量但不作加载实参：%s:%d' % (best[1], best[2]), best[1])


def classify(name, ctx):
    # 1) L1：字面量直接作加载实参
    ev = find_evidence_literal(name, ctx['literals'], ctx['load_sites'])
    if ev and ev[0] == 'L1':
        return ('L1', ev[1], ev[2])
    code_note = ev[2] if ev else u'代码里没有同名字面量'

    # 2) 表命中
    hits = ctx['tables'].get(name, [])
    if hits:
        # 优先：运行时读得到的表 + 读者有动态加载口
        runtime = []
        audit = []
        for (relp, line, kp, kind) in hits:
            base = os.path.splitext(os.path.basename(relp))[0]
            loaders = ctx['table_loaders'].get(base, [])
            if loaders:
                runtime.append((relp, line, kp, kind, loaders))
            else:
                audit.append((relp, line, kp, kind))
        if runtime:
            relp, line, kp, kind, loaders = runtime[0]
            # 读者文件里有没有动态加载口
            dyn = None
            for (br, bl) in loaders:
                for (r, ln, api, lit, prefix, dynamic) in ctx['load_sites']:
                    if r == br and dynamic:
                        dyn = (r, ln, api)
                        break
                if dyn:
                    break
            if dyn:
                keys = [s for s in kp.split('/') if not re.fullmatch(r'\[\d+\]', s)]
                field = keys[-1] if keys else ''
                field_l = field.lower()
                reader_text = '\n'.join(ctx['files'].get(dyn[0], []))
                field_used = (field_l in ctx['load_arg_members']
                              and re.search(r'\.\s*' + re.escape(field) + r'\b', reader_text, re.I) is not None)
                if not field_used:
                    if field_l in ctx['load_arg_members']:
                        why = (u'字段 `%s` 在**别的文件**里被当过加载实参，但本表的读者 `%s` 里没有动态加载口 '
                               u'⇒ 未证（可能由别的消费者加载；本探针不追跨文件数据流）' % (field, os.path.basename(dyn[0])))
                    else:
                        why = u'字段 `%s` 从没出现在任何加载实参（`Load(x.%s)` 一类）' % (field, field)
                    return ('L3', '%s:%d' % (relp, line),
                            u'表值（`%s`）在运行时读取的表里，但%s ⇒ 不判真加载' % (kp, why))
                return ('L2', '%s:%d + %s:%d' % (relp, line, dyn[0], dyn[1]),
                        u'表%s（`%s`）→ 读者 `%s` 动态加载口，字段 `%s` 出现在加载实参里' % (
                            u'值' if kind == 'value' else u'键', kp,
                            os.path.basename(dyn[0]), field))
            # 表有运行时读取者、但读者没有动态加载口
            return ('L3', '%s:%d' % (relp, line),
                    u'表%s（`%s`）的读取者 `%s` 没有动态加载口（表值不是加载实参）' % (
                        u'值' if kind == 'value' else u'键', kp,
                        ', '.join(sorted(set(b for b, _ in loaders))[:2])))
        relp, line, kp, kind = audit[0]
        return ('L3', '%s:%d' % (relp, line),
                u'仅名字在表（%s，`%s`）—— 该表在非 Editor 代码里 **没有读取者**（审计 dump / 工具表）' % (
                    u'值' if kind == 'value' else u'键', kp))

    # 3) 整值没命中：文本表（`数据/**/*.{tsv,csv,md,txt}`）里的行子串命中
    txt = ctx['find_text'](name)
    if txt:
        relp, line_no = txt
        return ('L3', '%s:%d' % (relp, line_no),
                u'仅名字在**文本表/文档的一行**里（子串命中）—— 不是加载实参')

    # 4) 整值没命中：看是不是**子串**命中（账的 U2 就是整串/词组匹配，可能命中更长的串）
    sub = ctx['find_substring'](name)
    if sub:
        relp, line, kp, kind, val = sub
        return ('L3', '%s:%d' % (relp, line),
                u'仅名字**作为子串**在表里（`%s` ⊂ `%s`，%s）—— 不是整值，表值不可能是加载实参同一串' % (
                    name, val[:60], kp))

    # 5) 没有字面量、没有表命中 → Unresolved（运行期拼出来的 / 账是子串命中）
    if ctx['dynamic_sites']:
        ev_file = ev[3] if ev and len(ev) > 3 else None
        pick = None
        if ev_file:
            for (r, ln, api) in ctx['dynamic_sites']:
                if r == ev_file:
                    pick = (r, ln, api)
                    break
        if pick is None:
            pick = ctx['dynamic_sites'][0]
        r, ln, api = pick
        return ('U', '%s:%d' % (r, ln),
                u'静态找不到字面量或整值表命中；仓里有动态加载口 `%s`（%s）⇒ 无法静态判定' % (api, code_note))
    return ('U', u'—', u'静态找不到字面量或整值表命中，也没有动态加载口')


# ----------------------------------------------------------------------------
# 报告
# ----------------------------------------------------------------------------
GRADE_DESC = {
    'L1': u'非 Editor 运行时代码以该名字为实参调用加载 API',
    'L2': u'名字是被这样代码读取的表里的值（表值 = 加载实参同一串）',
    'L3': u'仅名字在表（不算真加载）',
    'U': u'动态实参 / 静态解不出（单列）',
}


def render(rows, alloc, counts, results, stats, n, controls):
    from collections import Counter
    gcnt = Counter(r[4] for r in results)
    tcnt = {}
    for r in results:
        tcnt.setdefault(r[5], Counter())[r[4]] += 1
    out = []
    a = out.append
    a(u'# B107_R0_U2「真加载」核查（N56）')
    a(u'')
    a(u'> 生成时间：%s（`--check` 忽略本行）' % stats['now'])
    a(u'> 判据 = `资料/完全复刻/判据/B107_补判据_新账15条.md` **N56**：L1 = 非 Editor 运行时代码以资产名为实参调加载 API；'
      u'L2 = 出现在**被这样的代码读取**的表里且表值就是加载实参同一串；L3 = 仅名字在表（不算）；Unresolved 单列。')
    a(u'> 输入 = `资源/R0_资源覆盖账.tsv` 的 `我们有没有用 == %s` 段（现读 **%d** 行）'
      u'（`资源/R0_未使用清单.md:1003` 自述「只证明名字在表里」= 本核查要回答的问题）。' % (BAND_U2, stats['band_rows']))
    a(u'> 抽样 = **%d 条**，按 `类型` 分层：每类至少 %d 条，余量按占比（最大余数法）；类内按 `(文件, PathID)` 排序后等距抽取（确定性，无随机种子）。'
      % (n, MIN_PER_TYPE))
    a(u'> 复跑 = `python -I -X utf8 d:/4/Unity/工具/gen_u2_loaded_probe.py --write` / `--check`。'
      u'⛔ 本脚本**不改主账**。')
    a(u'')
    a(u'## §0 统计')
    a(u'')
    a(u'| 等级 | 条数 | 占比 | 含义 |')
    a(u'|---|---:|---:|---|')
    for g in ('L1', 'L2', 'L3', 'U'):
        c = gcnt.get(g, 0)
        a(u'| **%s** | %d | %.1f%% | %s |' % (g, c, (100.0 * c / max(1, len(results))), GRADE_DESC[g]))
    a(u'| 合计 | %d | 100%% | — |' % len(results))
    a(u'')
    weak = sum(1 for r in results if r[4] == 'L3' and u'别的文件' in r[3])
    a(u'> L3 里有 **%d** 条的字段名曾在**别的文件**被当加载实参（跨文件数据流本探针不追 ⇒ 未证，仍记 L3）。' % weak)
    a(u'')
    a(u'**阳性对照（证明探针能认出 L1/L2 —— 这几条不是抽样行，是手挑的已知样本）**')
    a(u'')
    a(u'| 名字 | 等级 | 证据 | 说明 |')
    a(u'|---|---|---|---|')
    for (nm, g, ev, note) in controls:
        a(u'| `%s` | **%s** | `%s` | %s |' % (nm, g, ev, note))
    a(u'')
    a(u'> ⚠️ **本抽样 L1 = 0 是结构性的、不是探针坏了**：账的 `U1`（已用（工程内））会先吃掉「名字在 `Assets/**/*.cs` '
      u'正文里出现过」的行（`r0_report.py:550-554`，2026-10-26 起 `.cs` 进白名单，正文词元/字面量都算）⇒ '
      u'一个名字只要能以字面量进加载实参，它在账里就**已是 U1、不会落在 U2 段**。所以对 U2 这段而言，'
      u'真问题是「L2 vs L3」，L1 恒 0 是口径互斥的结果。')
    a(u'')
    a(u'**按类型（行 = 类型，列 = 等级）**')
    a(u'')
    a(u'| 类型 | 账内 U2 行数 | 抽样 | L1 | L2 | L3 | U |')
    a(u'|---|---:|---:|---:|---:|---:|---:|')
    for t in sorted(tcnt, key=lambda x: (-counts.get(x, 0), x)):
        c = tcnt[t]
        a(u'| `%s` | %d | %d | %d | %d | %d | %d |' % (
            t, counts.get(t, 0), alloc.get(t, 0),
            c.get('L1', 0), c.get('L2', 0), c.get('L3', 0), c.get('U', 0)))
    a(u'')
    a(u'## §1 逐条（%d 条）' % len(results))
    a(u'')
    a(u'| # | 类型 | 名字 | 来源文件 | 等级 | 证据 | 说明 |')
    a(u'|---:|---|---|---|---|---|---|')
    for k, r in enumerate(results, 1):
        name = r[1].replace('|', '\\|')
        a(u'| %d | `%s` | %s | `%s` | **%s** | `%s` | %s |' % (k, r[5], name, r[0], r[4], r[2], r[3]))
    a(u'')
    a(u'## §2 判法（本探针的静态规则）')
    a(u'')
    a(u'- **代码语料**：`MyGame/Assets/**/*.cs`，排除目录名 `Editor`（大小写不敏感）与 `#if UNITY_EDITOR` 段（去注释后按预处理器栈剔行）。')
    a(u'- **L1**：在代码里搜「该名字（或路径 basename）作为字符串字面量」，并要求该字面量所在行是加载调用'
      u'（`Resources.Load/LoadAll` · `.LoadAsset/LoadAllAsset(s)` · `Addressables.LoadAsset*` · `.url =`），'
      u'或该字面量是**包装口**的实参（包装口 = 以形参直调上述 API 的方法，如 `CardArt.Get`；自动识别 + 一层传递）。')
    a(u'- **L2**：名字作为**整值**出现在某张 JSON 表里，且该表的基名作为**加载实参**出现在非 Editor 代码里（= 有运行时读取者），'
      u'读者文件里存在**动态实参**的加载口，且该值的字段名（忽略数组下标后的最后一段键）**出现在某个加载实参的成员访问里**'
      u'（`Load(x.clip)` ⇒ `clip` 才算）。')
    a(u'- **L3**：只命中表（含：表在非 Editor 代码里没有读取者 = 审计 dump / 工具表；或表里是**键**不是值；'
      u'读者没有动态加载口；字段名从没进过任何加载实参；只在 `数据/**/*.{tsv,csv,md,txt}` 的某一行里；或只是**子串**命中）。')
    a(u'- **U**：代码里没有同名字面量、也没有整值表命中，但仓里有动态加载口 ⇒ 静态无法判定（账的 U2 是**子串**命中，可能就是这样来的）。')
    a(u'- ⚠️ **已知局限**（也是「没查清」的部分）：① 只做**单行**实参解析（跨行调用记为无法解析 → U）；'
      u'② 常量折叠只认同文件 `const/static string`；③ L2 是**字段名级**证据（`Load(x.clip)` 里的 `clip`），'
      u'没有逐条做完整数据流；④ `#if` 处理是**简化栈**（`#elif` 只按文本含 `UNITY_EDITOR` 判）；'
      u'⑤ **经 SO 注册表按名字取资产**（`WarpforgeEffectLibrary.TryGet(name)`）**不算加载 API** ⇒ 这类名字落 L3'
      u'（资产其实被直接引用使用，只是没走 `Resources.Load`）；⑥ 子串/文本行命中按**大小写不敏感**（与账的 U2 同口径）。')
    a(u'')
    a(u'## §3 覆盖边界与统计底数')
    a(u'')
    a(u'- 代码文件：现读 **%d** 个非 Editor `.cs`；加载调用点 **%d** 个（其中动态实参 **%d**）；识别出包装口 **%d** 个。'
      % (stats['cs_files'], stats['load_sites'], stats['dynamic_sites'], stats['wrappers']))
    a(u'- 表：读入 **%d** 个 JSON（`Assets/**/*.json` + `数据/**/*.json`，排除 `数据/索引`）；解析失败 **%d** 个；超 %d MB 跳过 **%d** 个；'
      u'另有 `数据/**/*.{tsv,csv,md,txt}` 的非空行 **%d** 行（按行子串判，命中即 L3）。'
      % (stats['tables'], len(stats['bad_tables']), MAX_TABLE_BYTES // (1024 * 1024), len(stats['big_tables']), stats['text_lines']))
    a(u'- 账：`%s` 段 **%d** 行；本报告抽 **%d** 条（**没有**逐条核完 %d 条 —— 抽样结论只能外推到「同类型同形状」的行）。'
      % (BAND_U2, stats['band_rows'], n, stats['band_rows']))
    a(u'- ⛔ 本探针**不判**「资产该不该留」（那是 R7/R8/R9 的活）；只回答「这名字有没有被真加载」。')
    a(u'')
    return '\n'.join(out) + '\n'


# ----------------------------------------------------------------------------
# main
# ----------------------------------------------------------------------------
def main(argv):
    opt = parse_args(argv)
    if opt is None or opt.get('help'):
        sys.stdout.write(USAGE)
        return 0

    import time
    now = time.strftime('%Y-%m-%d %H:%M:%S')

    if not os.path.exists(LEDGER):
        sys.stderr.write(u'找不到账：%s\n' % LEDGER)
        return 2

    hdr, rows = read_u2_rows()
    picks, alloc, counts = sample(rows, opt['n'], MIN_PER_TYPE)

    literals, load_sites, files, consts_by_file, text_by_file = build_code_index()
    wrappers = build_wrappers(files)
    wrapper_sites = build_wrapper_sites(files, consts_by_file, wrappers)
    all_sites = load_sites + wrapper_sites
    dynamic_sites = [(r, ln, api) for (r, ln, api, lit, prefix, dyn) in all_sites if dyn]
    # 包装口 / 加载点的字面量也要进「实参」索引（build_code_index 只标了直调那批）
    for (r, ln, api, lit, prefix, dyn) in all_sites:
        if lit:
            literals.setdefault(lit, []).append((r, ln, True, api))
    literal_index = build_literal_name_index(literals)

    tables, tstats = build_table_index()
    table_loaders = build_table_loader_index(all_sites)
    table_values = []
    for v, occs in tables.items():
        for (relp, line, kp, kind) in occs[:2]:
            table_values.append((v, relp, line, kp, kind))

    def find_substring(name):
        if len(name) < 4:
            return None
        low = name.lower()
        for (v, relp, line, kp, kind) in table_values:
            if low in v.lower():
                return (relp, line, kp, kind, v)
        return None

    text_lines = build_text_index()

    def find_text(name):
        low = name.lower()
        for (relp, line_no, line) in text_lines:
            if low in line:
                return (relp, line_no)
        return None

    ctx = {
        'literals': literal_index,
        'load_sites': all_sites,
        'files': files,
        'tables': tables,
        'table_loaders': table_loaders,
        'load_arg_members': LOAD_ARG_MEMBERS,
        'dynamic_sites': dynamic_sites,
        'find_substring': find_substring,
        'find_text': find_text,
    }

    results = []
    for r in picks:
        name = r[3]
        if not name:
            results.append((r[0], r[3], u'—', u'名字为空', 'U', r[2], u'账里名字为空，无法判定'))
            continue
        grade, ev, note = classify(name, ctx)
        results.append((r[0], name, ev, note, grade, r[2]))

    stats = {
        'now': now,
        'band_rows': len(rows),
        'cs_files': len(files),
        'load_sites': len(all_sites),
        'dynamic_sites': len(dynamic_sites),
        'wrappers': len(wrappers),
        'tables': tstats['files'],
        'bad_tables': tstats['bad'],
        'big_tables': tstats['skipped_big'],
        'text_lines': len(text_lines),
    }
    controls = []
    for c in (u'arena1_bg', u'40k_battle_energy_full', u'card_vfx_by_card', u'EnvBlendables'):
        g_, ev_, note_ = classify(c, ctx)
        controls.append((c, g_, ev_, note_))
    report = render(rows, alloc, counts, results, stats, opt['n'], controls)

    if opt['tsv']:
        sys.stdout.write(u'类型\t名字\t来源文件\t等级\t证据\t说明\n')
        for r in results:
            sys.stdout.write(u'%s\t%s\t%s\t%s\t%s\t%s\n' % (r[5], r[1], r[0], r[4], r[2], r[3]))

    if opt['write']:
        d = os.path.dirname(REPORT)
        if not os.path.isdir(d):
            os.makedirs(d)
        io.open(REPORT, 'w', encoding='utf-8', newline='\n').write(report)
        sys.stdout.write(u'写出 %s（%d 条）\n' % (os.path.relpath(REPORT, ROOT).replace('\\', '/'), len(results)))
        return 0

    # --check
    if not os.path.exists(REPORT):
        sys.stderr.write(u'--check：报告不存在（先跑 --write）\n')
        return 1
    old = io.open(REPORT, 'r', encoding='utf-8').read()
    strip = lambda s: '\n'.join(l for l in s.splitlines() if not l.startswith(u'> 生成时间：'))
    if strip(old) != strip(report):
        sys.stderr.write(u'--check：与盘上报告不一致（重新 --write 后再比）\n')
        return 1
    sys.stdout.write(u'--check：一致（%d 条）\n' % len(results))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv))
