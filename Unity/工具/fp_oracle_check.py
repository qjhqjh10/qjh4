#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""fp_oracle_check.py -- machine-check the FP oracle spec (method (2) "oracle").

Spec owner: 资料/完全复刻/判据/B106_功能点三法缺口.md  §2.3 / §2.4
Spec table: 资料/完全复刻/判据/功能点_oracle规格.tsv   (12 columns)

WHAT IT IS
----------
One row of the spec = one oracle check for one feature point (FP).
Batch 1: `asset_field` (read an original serialized asset) and `code_const`
(read our own source and pin an original value) -- plus `manual_only` and
`impossible`.
Batch 2: `effect_render` -- op `sweep_ratio`, reads
`资料/比对基线/sweep_{orig,exp}.tsv`.
Batch 3: `card_truth` -- op `pool_verdict`, reads
`判据/B9_全池对拍_差异.tsv`.
Batch 4: `orig_run_dump` -- op `dump_node`, reads `runtime_ui_dump_*.tsv`.
`anim_trace` is still declared in the closed set but NOT implemented (a row
using it is reported as a loud FAIL, never silently skipped).

READ-ONLY POLICY
----------------
The tool only READS the original archives (d:/2) and our sources.  It only
WRITES under --out (default d:/4/_tmp_view/oracle/).

CLI
---
    python -I -X utf8 工具/fp_oracle_check.py --list
    python -I -X utf8 工具/fp_oracle_check.py --only FP-CARD-001
    python -I -X utf8 工具/fp_oracle_check.py --only FP-LOC-02,FP-AUDIO-04 --json _tmp_view/oracle/r.json
    python -I -X utf8 工具/fp_oracle_check.py --types asset_field,code_const
    python -I -X utf8 工具/fp_oracle_check.py --spec <path> --sbs <path> --out <dir>

SUMMARY LINE (fixed, machine readable)
--------------------------------------
    === FP oracle：机器通过 X / 机器失败 Y / 人工待签 Z / N/A W ===
(the four numbers keep their meaning.  A row whose data source does not exist
is status `missing`, printed with tag `缺`, and counted into Y -- "没测到不是
绿".  When such rows exist, ONE extra breakdown line is printed right after the
summary: `（其中「缺数据」M 行已计入机器失败）` -- the summary line itself is
byte-identical to batch 1.)

EXIT CODE
---------
    Y > 0 or Z > 0  -> 1
    else X == 0     -> 2   ("nothing measured" is NOT green)
    else            -> 0

判定方式 (the machine op) -- closed vocabulary
----------------------------------------------
  code_has      code_const.  `读数来源` = spans "path:line[-line2]" (split by ';' or '·';
                a span with no path inherits the previous path).  Every span, after
                C# comments are stripped, must contain `期望` literally.
  code_num      same spans; every number in `期望` (comma/space separated) must appear as
                a numeric literal inside the comment-stripped span (rel tol 1e-5).
  json_eq       asset_field. `输入/命令` = "src=<path>;key=<dotted>" (+ optional
                "ours=<path>;okey=<dotted>").  Value at key == `期望`.
  json_len      like json_eq but compares len(value) to `期望`.
  json_distinct asset_field. "glob=<glob>;key=<dotted>" -> number of distinct values == `期望`.
  json_files    asset_field. "glob=<glob>;key=<dotted>" -> number of files whose key is
                missing/empty == `期望` (usually 0).  Also reports the file total.
  glob_count    asset_field. "glob=<glob>" -> number of matching files == `期望`.
  set_subset    asset_field. "src=<path>;key=<path.expr>" vs `读数来源` "ours=<path>;key=<...>"
                (path.expr supports '*' = iterate dict values / list items).  Number of
                source items missing from ours == `期望` (usually 0).
  human_sign    manual_only.  PASS only if 功能点_并排比记账.tsv has a row for this FP with
                判定 in {一致,有差异} and a non-empty 签字.  Otherwise status = manual (Z).
  n_a           impossible.  Always status = N/A (W).  Not counted as pass.

  --- batch 2 / 3 / 4 (2026-10-31, B108_A9) ---

  sweep_ratio   effect_render.  `输入/命令` = "effect=<名1>|<名2>..." (+ optional
                "orig=<path>;exp=<path>").  `期望` = the acceptance threshold on
                |ln(亮度比)| (documented 0.30; 出处 资料/特效还原_进度与交接.md:1640
                「现在全表没有任何一条 > 0.30」；每行 `期望` 也写死这个数).
                Per effect the ratio is computed EXACTLY like
                `工具/analyze_sweep.py` does: a sample "has content" when its
                `lit` >= LIT_MIN(60); the median of `sum_exp / sum_orig` over the
                samples where both sides have content is the 亮度比中位; the
                acceptance number is |ln| of that median.
                  * both sides have content -> compare |ln| with 期望
                  * only one side has content and the other side's peak is
                    < LIT_MIN*0.6  -> group C/D = the effect is missing on one
                    side => FAIL (the docs call that "原版有我们没有")
                  * only one side has content but the other peak >= 36 -> "weak
                    sample" branch, same ratio rule as analyze_sweep
                  * both sides empty -> not comparable (skipped)
                  * both have content but never at the same time -> group T =>
                    FAIL (时序错位)
                If NO named effect is comparable at all -> status = missing (?).

  pool_verdict  card_truth.  `输入/命令` = "src=<B9 tsv>;family=<族名1>|<族名2>..."
                (+ "scope=render" for a FP that has no B9 effect-family dimension).
                `期望` = the allowed number of `错` rows (normally 0).
                  错 > 期望        -> FAIL (red)
                  未验到 > 0       -> status = manual (amber)
                  all 对            -> PASS (green)
                No matching family / scope=render -> status = missing (?).

  dump_node     orig_run_dump.  `输入/命令` = "dump=<runtime_ui_dump tsv>;path=<默认
                全路径>;name=<默认节点名>".  `期望` = a JSON object or a JSON list of
                objects: {"path":..., "name":..., "idx":N, "activeSelf":true,
                "activeInHierarchy":true, "anchoredPosition":[x,y],
                "sizeDelta":[w,h], "anchorMin":[x,y], "anchorMax":[x,y],
                "pivot":[x,y], "text":"...", "fontSize":30.0, "image":"..."}.
                A node is matched by (path, name); if several nodes match and no
                `idx` is given the row FAILS loudly (never silently picks one).
                NOTE the dump's HEADER LIES about its columns -- the real order is
                path,name,activeSelf,activeInHierarchy,RT|TF,anchoredPosition,
                sizeDelta,anchorMin,anchorMax,pivot,TXT,text,fontSize,IMG,sprite
                (judged from d:/2/Warpforge_tools/scenejumpshot/SceneJumpShot.cs:38-104,
                `DumpGo`).  Numeric tolerance follows the dump's own decimals
                (0.051 for the F1 rect fields, 0.0015 for the F3 anchor fields).
                Dump file missing -> status = missing (?).

Notes
-----
* TSV files are read with a bare `split('\t')` on purpose: the repo's ledger
  consumers do the same, and Python's csv module would mis-parse backslashes.
* Source files are read in BINARY and split on CRLF/CR/LF ourselves, so line
  numbers match what the ledger/editor reports (see CLAUDE.md line-ending notes).
"""
import argparse
import collections
import glob as globmod
import io
import json
import math
import os
import re
import statistics
import sys
import time

sys.stdout.reconfigure(encoding='utf-8')

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)                      # d:/4/Unity
SPEC_DEFAULT = os.path.join(REPO, '资料', '完全复刻', '判据', '功能点_oracle规格.tsv')
SBS_DEFAULT = os.path.join(REPO, '资料', '完全复刻', '判据', '功能点_并排比记账.tsv')
OUT_DEFAULT = 'd:/4/_tmp_view/oracle'

HEADER = ['FP_ID', '功能点', '域', 'oracle类型', '输入/命令', '期望', '读数来源',
          '判定方式', '覆盖档', '证据(文件:行/资产路径)', '日期', '备注']

ORACLE_TYPES = ['asset_field', 'code_const', 'orig_run_dump', 'effect_render',
                'card_truth', 'anim_trace', 'manual_only', 'impossible']

OPS = ['code_has', 'code_num', 'json_eq', 'json_len', 'json_distinct', 'json_files',
       'glob_count', 'set_subset', 'human_sign', 'n_a',
       'sweep_ratio', 'pool_verdict', 'dump_node']

# ops reachable from each oracle type (a mismatch is a spec bug -> FAIL)
TYPE_OPS = {
    'asset_field': {'json_eq', 'json_len', 'json_distinct', 'json_files', 'glob_count', 'set_subset'},
    'code_const': {'code_has', 'code_num'},
    'effect_render': {'sweep_ratio'},
    'card_truth': {'pool_verdict'},
    'orig_run_dump': {'dump_node'},
    'manual_only': {'human_sign'},
    'impossible': {'n_a'},
}

# ---- batch 2/3/4 defaults (all read-only; paths resolved like everything else)
SWEEP_ORIG_DEFAULT = os.path.join(REPO, '资料', '比对基线', 'sweep_orig.tsv')
SWEEP_EXP_DEFAULT = os.path.join(REPO, '资料', '比对基线', 'sweep_exp.tsv')
B9_DEFAULT = os.path.join(REPO, '资料', '完全复刻', '判据', 'B9_全池对拍_差异.tsv')

# 口径出处：工具/analyze_sweep.py:60 "LIT_MIN = 60"（低于它的亮点数当噪声）、
# :62-63 "另一侧峰值 < LIT_MIN*0.6（真缺席时峰值是 0）"（weak_min / 假 C/D 的判据）。
LIT_MIN = 60.0
WEAK_MIN_FACTOR = 0.6
# 验收阈值 = |ln(亮度比中位)| <= 0.30。出处：资料/特效还原_进度与交接.md:1640
# 「现在全表没有任何一条 > 0.30」+ 同文件 1463/1562/1632 三张表的 `>0.30` 列。
# ⛔ 别改这个默认值；每行自己也在 `期望` 里写死 0.30。
SWEEP_LN_THRESHOLD = 0.30

# runtime_ui_dump_*.tsv 的真实列序（表头是错的；判据 = SceneJumpShot.cs 的 DumpGo，
# 它依次写 path,name,activeSelf,activeInHierarchy, RT|TF,ap,sd,amin,amax,pv,
# TXT,text,fontSize, IMG,sprite）。
DUMP_IDX = {
    'path': 0, 'name': 1, 'activeSelf': 2, 'activeInHierarchy': 3, 'type': 4,
    'anchoredPosition': 5, 'sizeDelta': 6, 'anchorMin': 7, 'anchorMax': 8, 'pivot': 9,
    'hasTMP': 10, 'text': 11, 'fontSize': 12, 'hasIMG': 13, 'image': 14,
}
DUMP_VEC_FIELDS = ('anchoredPosition', 'sizeDelta', 'anchorMin', 'anchorMax', 'pivot')

ROOTS = [
    'd:/4/Unity/',
    'd:/4/Unity/MyGame/Assets/',
    'd:/4/Unity/资料/完全复刻/',
    'd:/4/',
]

NUM_RE = re.compile(r'(?<![A-Za-z0-9_.])-?\d+(?:\.\d+)?(?:[eE][-+]?\d+)?f?')


# ---------------------------------------------------------------- io helpers

def resolve_path(p):
    """Resolve a repo-relative path against the known roots; None if missing."""
    if not p:
        return None
    p = p.strip().replace('\\', '/')
    if os.path.isabs(p) and os.path.exists(p):
        return p
    for r in ROOTS:
        cand = r + p
        if os.path.exists(cand):
            return cand
    return None


def read_text(p):
    with io.open(p, 'rb') as f:
        return f.read().decode('utf-8', 'replace')


def split_lines(t):
    return re.split(r'\r\n|\r|\n', t)


def read_tsv(p):
    """Return (header, rows).  Bare split('\t'); '#'-only line == comment."""
    hdr = None
    rows = []
    with io.open(p, encoding='utf-8', newline='') as f:
        for raw in f:
            line = raw.rstrip('\r\n')
            if not line:
                continue
            if line.startswith('#'):
                continue
            cells = line.split('\t')
            if hdr is None:
                hdr = cells
                continue
            rows.append(cells)
    return hdr, rows


def strip_cs_comments(txt):
    """Remove // and /* */ comments, keeping string/char literals intact."""
    bs = chr(92)
    out = []
    i = 0
    n = len(txt)
    st = 0                      # 0 code, 1 line-comment, 2 block-comment, 3 string, 4 char
    while i < n:
        c = txt[i]
        if st == 0:
            if c == '/' and i + 1 < n and txt[i + 1] == '/':
                st = 1
                i += 2
                continue
            if c == '/' and i + 1 < n and txt[i + 1] == '*':
                st = 2
                i += 2
                continue
            if c == '"':
                st = 3
            elif c == "'":
                st = 4
            out.append(c)
            i += 1
        elif st == 1:
            if c == '\n':
                st = 0
                out.append(c)
            i += 1
        elif st == 2:
            if c == '*' and i + 1 < n and txt[i + 1] == '/':
                st = 0
                i += 2
            else:
                i += 1
        elif st == 3:
            out.append(c)
            if c == bs and i + 1 < n:
                out.append(txt[i + 1])
                i += 2
                continue
            if c == '"':
                st = 0
            i += 1
        else:
            out.append(c)
            if c == bs and i + 1 < n:
                out.append(txt[i + 1])
                i += 2
                continue
            if c == "'":
                st = 0
            i += 1
    return ''.join(out)


def parse_spans(s):
    """'path:12-14;:20 · other.cs:3' -> [(path, a, b), ...]; bare ':20' inherits path."""
    spans = []
    cur = None
    for part in re.split(r'[;·]', s or ''):
        part = part.strip()
        if not part:
            continue
        m = re.match(r'^(.*?):(\d+)(?:\s*-\s*(\d+))?$', part)
        if not m:
            continue
        head = m.group(1).strip()
        if head:
            cur = head
        if cur is None:
            continue
        a = int(m.group(2))
        b = int(m.group(3) or m.group(2))
        spans.append((cur, a, b))
    return spans


def parse_kv(cmd):
    d = {}
    for part in (cmd or '').split(';'):
        part = part.strip()
        if not part or '=' not in part:
            continue
        k, v = part.split('=', 1)
        d[k.strip()] = v.strip()
    return d


def get_dotted(obj, key):
    if key in ('', '.', None):
        return obj
    cur = obj
    for p in key.split('.'):
        if isinstance(cur, dict):
            if p not in cur:
                return _MISSING
            cur = cur[p]
        elif isinstance(cur, list) and p.isdigit():
            i = int(p)
            if i >= len(cur):
                return _MISSING
            cur = cur[i]
        else:
            return _MISSING
    return cur


_MISSING = object()


def walk_expr(obj, expr):
    """Collect values matched by a dotted expression; '*' iterates dict/list."""
    if not expr or expr == '.':
        yield obj
        return
    parts = expr.split('.')
    stack = [(obj, 0)]
    while stack:
        cur, idx = stack.pop()
        if idx >= len(parts):
            yield cur
            continue
        p = parts[idx]
        if p == '*':
            if isinstance(cur, dict):
                for v in cur.values():
                    stack.append((v, idx + 1))
            elif isinstance(cur, list):
                for v in cur:
                    stack.append((v, idx + 1))
        elif isinstance(cur, dict):
            if p in cur:
                stack.append((cur[p], idx + 1))
        elif isinstance(cur, list) and p.isdigit():
            i = int(p)
            if i < len(cur):
                stack.append((cur[i], idx + 1))


def load_json(p):
    with io.open(p, encoding='utf-8') as f:
        return json.load(f)


def num_ok(x, y):
    return abs(x - y) <= max(1e-6, abs(y) * 1e-5)


def parse_expected_scalar(s):
    s = (s or '').strip()
    if s.startswith('[') or s.startswith('{'):
        try:
            return json.loads(s)
        except Exception:
            pass
    try:
        return float(s)
    except Exception:
        return s


def deep_eq(a, b):
    if isinstance(a, float) and isinstance(b, float):
        return num_ok(a, b)
    if isinstance(a, (int, float)) and isinstance(b, (int, float)):
        return num_ok(float(a), float(b))
    if isinstance(a, list) and isinstance(b, list):
        return len(a) == len(b) and all(deep_eq(x, y) for x, y in zip(a, b))
    if isinstance(a, dict) and isinstance(b, dict):
        return set(a) == set(b) and all(deep_eq(a[k], b[k]) for k in a)
    return a == b


# ---------------------------------------------------------------- ops

def op_code_has(row):
    exp = row['期望']
    spans = parse_spans(row['读数来源'])
    if not spans:
        return False, '读数来源 里没有可解析的 path:line'
    missing = []
    for path, a, b in spans:
        fp = resolve_path(path)
        if not fp:
            return False, '源文件不存在: %s' % path
        lines = split_lines(strip_cs_comments(read_text(fp)))
        txt = '\n'.join(lines[max(0, a - 1):b])
        if exp not in txt:
            missing.append('%s:%d-%d' % (os.path.basename(path), a, b))
    if missing:
        return False, '期望串不在这些代码行里: %s' % ', '.join(missing)
    return True, '%d 处代码 span 都含 %r' % (len(spans), exp)


def op_code_num(row):
    exp = row['期望']
    exp_nums = [float(x.rstrip('f')) for x in NUM_RE.findall(exp)]
    if not exp_nums:
        return False, '期望里没有数字: %r' % exp
    spans = parse_spans(row['读数来源'])
    if not spans:
        return False, '读数来源 里没有可解析的 path:line'
    for path, a, b in spans:
        fp = resolve_path(path)
        if not fp:
            return False, '源文件不存在: %s' % path
        lines = split_lines(strip_cs_comments(read_text(fp)))
        txt = '\n'.join(lines[max(0, a - 1):b])
        have = [float(x.rstrip('f')) for x in NUM_RE.findall(txt)]
        miss = [v for v in exp_nums if not any(num_ok(h, v) for h in have)]
        if miss:
            return False, '代码 span %s:%d-%d 缺数字 %s' % (
                os.path.basename(path), a, b, ','.join('%g' % v for v in miss))
    return True, '%d 处代码 span 都含 %s' % (len(spans), exp)


def _src_pair(row):
    cmd = parse_kv(row['输入/命令'])
    src = resolve_path(cmd.get('src', ''))
    if not src:
        return None, None, cmd, 'src 不存在: %s' % cmd.get('src')
    return src, load_json(src), cmd, None


def op_json_eq(row):
    src, doc, cmd, err = _src_pair(row)
    if err:
        return False, err
    val = get_dotted(doc, cmd.get('key', '.'))
    if val is _MISSING:
        return False, 'key 不存在: %s' % cmd.get('key')
    exp = parse_expected_scalar(row['期望'])
    ok = deep_eq(val, exp)
    detail = 'src%s=%r' % (('.' + cmd['key']) if cmd.get('key') else '', val)
    if cmd.get('ours'):
        ours = resolve_path(cmd['ours'])
        if not ours:
            return False, 'ours 不存在: %s' % cmd['ours']
        oval = get_dotted(load_json(ours), cmd.get('okey', '.'))
        detail += ' / ours=%r' % (oval,)
        ok = ok and deep_eq(oval, exp)
    return ok, detail


def op_json_len(row):
    src, doc, cmd, err = _src_pair(row)
    if err:
        return False, err
    val = get_dotted(doc, cmd.get('key', '.'))
    if val is _MISSING or not hasattr(val, '__len__'):
        return False, 'key 不是可数对象: %s' % cmd.get('key')
    exp = int(float(row['期望']))
    ok = len(val) == exp
    detail = 'src.len=%d exp=%d' % (len(val), exp)
    if cmd.get('ours'):
        ours = resolve_path(cmd['ours'])
        if not ours:
            return False, 'ours 不存在: %s' % cmd['ours']
        oval = get_dotted(load_json(ours), cmd.get('okey', '.'))
        n = len(oval) if hasattr(oval, '__len__') else -1
        detail += ' / ours.len=%d' % n
        ok = ok and n == exp
    return ok, detail


def _glob_docs(cmd):
    pat = cmd.get('glob', '')
    pat = pat.replace('\\', '/')
    files = sorted(globmod.glob(pat))
    return files


def op_json_distinct(row):
    cmd = parse_kv(row['输入/命令'])
    files = _glob_docs(cmd)
    if not files:
        return False, 'glob 无文件: %s' % cmd.get('glob')
    vals = set()
    bad = 0
    for f in files:
        try:
            v = get_dotted(load_json(f), cmd.get('key', '.'))
        except Exception:
            bad += 1
            continue
        if v is _MISSING or v in (None, '', [], {}):
            continue
        vals.add(repr(v))
    exp = int(float(row['期望']))
    ok = len(vals) == exp and bad == 0
    return ok, 'distinct=%d exp=%d files=%d unreadable=%d' % (len(vals), exp, len(files), bad)


def op_json_files(row):
    cmd = parse_kv(row['输入/命令'])
    files = _glob_docs(cmd)
    if not files:
        return False, 'glob 无文件: %s' % cmd.get('glob')
    key = cmd.get('key', '.')
    missing = 0
    bad = 0
    for f in files:
        try:
            v = get_dotted(load_json(f), key)
        except Exception:
            bad += 1
            continue
        if v is _MISSING or v in (None, '', [], {}):
            missing += 1
    exp = int(float(row['期望']))
    ok = missing == exp and bad == 0
    return ok, 'missing=%d exp=%d files=%d unreadable=%d' % (missing, exp, len(files), bad)


def op_glob_count(row):
    cmd = parse_kv(row['输入/命令'])
    files = _glob_docs(cmd)
    exp = int(float(row['期望']))
    return len(files) == exp, 'files=%d exp=%d' % (len(files), exp)


def op_set_subset(row):
    cmd = parse_kv(row['输入/命令'])
    src = resolve_path(cmd.get('src', ''))
    if not src:
        return False, 'src 不存在: %s' % cmd.get('src')
    ours_cmd = parse_kv(row['读数来源'])
    ours = resolve_path(ours_cmd.get('ours', ''))
    if not ours:
        return False, 'ours 不存在: %s' % ours_cmd.get('ours')
    src_set = set()
    for v in walk_expr(load_json(src), cmd.get('key', '.')):
        if v is not None:
            src_set.add(str(v))
    ours_set = set()
    for v in walk_expr(load_json(ours), ours_cmd.get('key', '.')):
        if v is not None:
            ours_set.add(str(v))
    missing = sorted(src_set - ours_set)
    exp = int(float(row['期望']))
    ok = len(missing) == exp
    detail = 'src=%d ours=%d missing=%d exp=%d' % (len(src_set), len(ours_set), len(missing), exp)
    if missing:
        detail += ' e.g. ' + ', '.join(missing[:5])
    return ok, detail


# ------------------------------------------------- batch 2 (effect_render) op

def _load_sweep(path):
    """{effect: {time(string): (lit:int, sum:float)}} -- same loader as analyze_sweep."""
    d = collections.defaultdict(dict)
    if not path or not os.path.exists(path):
        return d
    for line in io.open(path, encoding='utf-8-sig'):
        f = line.rstrip('\r\n').split('\t')
        if len(f) < 4 or f[0] == 'effect':
            continue
        try:
            d[f[0]][f[1]] = (int(float(f[2])), float(f[3]))
        except ValueError:
            continue
    return d


def _sweep_median_ratio(samples, lit_gate):
    """Median of sum_exp/sum_orig.  lit_gate=True: only samples where BOTH sides
    have lit >= LIT_MIN; False: every sample where both sums > 0 (weak branch)."""
    rr = []
    for _t, o, e in samples:
        if lit_gate and (o[0] < LIT_MIN or e[0] < LIT_MIN):
            continue
        if o[1] > 0 and e[1] > 0:
            rr.append(e[1] / o[1])
    return statistics.median(rr) if rr else None


def op_sweep_ratio(row):
    cmd = parse_kv(row['输入/命令'])
    names = [x.strip() for x in (cmd.get('effect') or '').split('|') if x.strip()]
    try:
        thr = float((row['期望'] or '').strip())
    except ValueError:
        thr = SWEEP_LN_THRESHOLD
    if not names:
        return 'missing', '缺数据：本 FP 没有 sweep 效果基线（行里未指定 effect=）'
    orig_p = resolve_path(cmd.get('orig', '')) or (
        SWEEP_ORIG_DEFAULT if os.path.exists(SWEEP_ORIG_DEFAULT) else None)
    exp_p = resolve_path(cmd.get('exp', '')) or (
        SWEEP_EXP_DEFAULT if os.path.exists(SWEEP_EXP_DEFAULT) else None)
    if not orig_p or not exp_p:
        return 'missing', '缺数据：sweep 基线不存在（orig=%s / exp=%s）' % (
            cmd.get('orig') or SWEEP_ORIG_DEFAULT, cmd.get('exp') or SWEEP_EXP_DEFAULT)
    orig = _load_sweep(orig_p)
    exp = _load_sweep(exp_p)

    reds, oks, skips = [], [], []
    for n in names:
        if n not in orig and n not in exp:
            reds.append('%s：两侧都没有这个效果' % n)
            continue
        # 🔴 2026-10-31（B109_E13）：**缺行 = 该侧全程空**，与 `工具/analyze_sweep.py:158` 的
        #    `orig.get(name, {})` 同口径。原来这里「一侧缺行就判红」，把「原版自己也 0 像素」的空壳效果
        #    （只有 AnimFX 模块 / 只有 spawner 脚本的 prefab）判成了「原版有我们没有」——实测 3 条：
        #    `Earthquake Shake only` / `Environmental Condition Particles Orbital` /
        #    `Teleport Trigger Board Effect Only`（判据 → `判据/B109_R3_oracle真红.md` §1 §2 §4）。
        #    下面那段分支本来就带着 `analyze_sweep.py:170-177` 的闸（**另一侧峰值必须 < LIT_MIN*0.6
        #    才允许判 C/D**），所以这里只负责把「缺行」读成「全程空」；原版真有内容而我们缺行 ⇒ 仍然红。
        #    ⛔ 别用 orig[n] / exp[n]（defaultdict 会就地建空条目）。
        o, e = orig.get(n, {}), exp.get(n, {})
        samples = [(t, o.get(t, (0, 0.0)), e.get(t, (0, 0.0)))
                   for t in sorted(set(o) | set(e))]
        oon = [s for s in samples if s[1][0] >= LIT_MIN]
        eon = [s for s in samples if s[2][0] >= LIT_MIN]
        both = [s for s in samples if s[1][0] >= LIT_MIN and s[2][0] >= LIT_MIN]
        opeak = max([s[1][0] for s in samples] or [0])
        epeak = max([s[2][0] for s in samples] or [0])
        weak_min = LIT_MIN * WEAK_MIN_FACTOR
        # ---- exactly analyze_sweep.py's branch order (2026-10-31 B105_R1 口径)
        if not oon and not eon:
            skips.append('%s：两侧全程空' % n)
            continue
        if not oon and epeak > 0 and opeak >= weak_min:
            med = _sweep_median_ratio(samples, False)
            tag = '（次阈值小样本）'
        elif not eon and opeak > 0 and epeak >= weak_min:
            med = _sweep_median_ratio(samples, False)
            tag = '（次阈值小样本）'
        elif not oon:
            reds.append('%s：只有导出有内容（原版峰值 %d）' % (n, opeak))
            continue
        elif not eon:
            reds.append('%s：只有原版有内容（导出整个丢了，导出峰值 %d）' % (n, epeak))
            continue
        elif not both:
            reds.append('%s：两侧有内容但从不同时出现（时序错位）' % n)
            continue
        else:
            med = _sweep_median_ratio(samples, True)
            tag = ''
        if med is None or med <= 0:
            skips.append('%s：有内容但亮度比读不出' % n)
            continue
        ln = abs(math.log(med))
        if ln > thr:
            reds.append('%s：|ln|=%.3f > %.2f（比值 %.3f）%s' % (n, ln, thr, med, tag))
        else:
            oks.append('%s：|ln|=%.3f%s' % (n, ln, tag))
    if reds:
        return False, '%d/%d 红：%s' % (len(reds), len(names), '；'.join(reds[:4]))
    if not oks:
        return 'missing', '缺数据：没有可比样本（%s）' % '；'.join(skips[:3])
    return True, '全部 |ln| <= %.2f：%s' % (thr, '；'.join(oks[:4]))


# --------------------------------------------------- batch 3 (card_truth) op

def op_pool_verdict(row):
    cmd = parse_kv(row['输入/命令'])
    if (cmd.get('scope') or '').strip() == 'render':
        return 'missing', ('缺数据：B9 全池对拍只有「效果结算」维度（族名/期望/实得/判定），'
                           '没有卡面渲染维度 ⇒ 卡面 4 个 FP 仍只能人签；缺的基线 = 卡面像素级'
                           '机器比对（B107_W5 §六 1「同比例」问题未解）')
    fams = set(x.strip() for x in (cmd.get('family') or '').split('|') if x.strip())
    if not fams:
        return 'missing', ('缺数据：本 FP 在 B9 全池对拍里没有对应效果族'
                           '（B9 只覆盖 57 个 chosenAbility 族）')
    src = resolve_path(cmd.get('src', '')) or (
        B9_DEFAULT if os.path.exists(B9_DEFAULT) else None)
    if not src or not os.path.exists(src):
        return 'missing', '缺数据：B9 全池对拍差异表不存在: %s' % (
            cmd.get('src') or B9_DEFAULT)
    hdr, rows = read_tsv(src)
    try:
        fi = hdr.index('族名')
        vi = hdr.index('判定')
    except ValueError:
        return 'missing', '缺数据：B9 表头不含「族名/判定」列（%r）' % (hdr,)
    counts = collections.Counter()
    total = 0
    for r in rows:
        if len(r) <= max(fi, vi):
            continue
        if r[fi].strip() in fams:
            total += 1
            counts[r[vi].strip()] += 1
    detail = '族=%s 行=%d 对=%d 错=%d 未验到=%d' % (
        ','.join(sorted(fams)), total, counts.get('对', 0),
        counts.get('错', 0), counts.get('未验到', 0))
    if total == 0:
        return 'missing', detail + '（该族名在 B9 里 0 行）'
    try:
        allowed = int(float((row['期望'] or '0').strip()))
    except ValueError:
        allowed = 0
    if counts.get('错', 0) > allowed:
        return False, detail + '（错 > 允许 %d）' % allowed
    if counts.get('未验到', 0) > 0:
        return 'manual', detail + '（琥珀：未验到，人工待签）'
    unknown = total - counts.get('对', 0) - counts.get('错', 0) - counts.get('未验到', 0)
    if unknown > 0:
        return 'missing', detail + '（判定列有 %d 行无法归类）' % unknown
    return True, detail


# ------------------------------------------------ batch 4 (orig_run_dump) op

def _load_dump(path):
    """Read runtime_ui_dump_*.tsv with the REAL column order (header is wrong)."""
    recs = []
    for line in io.open(path, encoding='utf-8-sig'):
        cells = line.rstrip('\r\n').split('\t')
        if not cells or not cells[0].strip():
            continue
        if cells[0].strip() == 'path':
            continue
        rec = {}
        for k, i in DUMP_IDX.items():
            rec[k] = cells[i].strip() if i < len(cells) else ''
        recs.append(rec)
    return recs


def _dump_tol(tok):
    """Tolerance from the dump's own printed decimals (F1 -> .1, F3 -> .001)."""
    if '.' in tok:
        dec = len(tok.split('.', 1)[1])
        if dec <= 1:
            return 0.051
        if dec == 3:
            return 0.0015
    return 1e-9


def _cmp_dump_vec(got, want):
    parts = [x.strip() for x in (got or '').split(',')]
    if len(parts) != 2:
        return 'dump 值不是二维: %r' % got
    if not isinstance(want, (list, tuple)) or len(want) != 2:
        return '期望不是 [x,y]: %r' % (want,)
    for i in (0, 1):
        try:
            gv = float(parts[i])
        except ValueError:
            return 'dump 值不可解析: %r' % got
        if abs(gv - float(want[i])) > _dump_tol(parts[i]) + 1e-9:
            return 'dump=%s 期望=%s' % (got, want)
    return None


def op_dump_node(row):
    cmd = parse_kv(row['输入/命令'])
    dp = resolve_path(cmd.get('dump', ''))
    if not dp:
        if not (cmd.get('dump') or '').strip():
            return 'missing', ('缺数据：本行未给 dump=（该 FP 在 13 份 runtime_ui_dump 里'
                               '没有可逐节点比的节点）')
        return 'missing', '缺数据：dump 不存在: %s' % cmd.get('dump')
    recs = _load_dump(dp)
    exp = parse_expected_scalar(row['期望'])
    if isinstance(exp, dict):
        exp = [exp]
    if not isinstance(exp, list) or not exp:
        return False, '期望 不是 JSON 对象/数组: %r' % (row['期望'] or '')[:60]
    fails, oks = [], 0
    for e in exp:
        if not isinstance(e, dict):
            fails.append('期望项不是对象: %r' % (e,))
            continue
        path = e.get('path', cmd.get('path', ''))
        name = e.get('name', cmd.get('name', ''))
        cand = [r for r in recs
                if r['path'] == path and (not name or r['name'] == name)]
        if not cand:
            fails.append('节点不在 dump 里: path=…%s name=%s' % (path[-42:], name))
            continue
        if 'idx' in e:
            try:
                rec = cand[int(e['idx'])]
            except (ValueError, IndexError):
                fails.append('idx=%r 越界（匹配 %d 个）' % (e['idx'], len(cand)))
                continue
        elif len(cand) > 1:
            fails.append('path+name 匹配 %d 个节点、未给 idx: …%s/%s'
                         % (len(cand), path[-42:], name))
            continue
        else:
            rec = cand[0]
        err = None
        for f in ('activeSelf', 'activeInHierarchy'):
            if f in e:
                if rec[f] not in ('True', 'False'):
                    err = '%s 不是布尔: %r' % (f, rec[f])
                elif (rec[f] == 'True') != bool(e[f]):
                    err = '%s: dump=%s 期望=%s' % (f, rec[f], e[f])
                if err:
                    break
        if err is None and any(f in e for f in DUMP_VEC_FIELDS) and rec['type'] != 'RT':
            err = '节点不是 RectTransform（dump type=%r），不能比矩形字段' % rec['type']
        if err is None:
            for f in DUMP_VEC_FIELDS:
                if f in e:
                    err = _cmp_dump_vec(rec[f], e[f])
                    if err:
                        err = '%s: %s' % (f, err)
                        break
        if err is None and 'fontSize' in e:
            try:
                if abs(float(rec['fontSize']) - float(e['fontSize'])) > 0.051:
                    err = 'fontSize: dump=%s 期望=%s' % (rec['fontSize'], e['fontSize'])
            except ValueError:
                err = 'fontSize 不可解析: %r' % rec['fontSize']
        if err is None and 'text' in e and rec['text'] != str(e['text']):
            err = 'text: dump=%r 期望=%r' % (rec['text'], e['text'])
        if err is None and 'image' in e and rec['image'] != str(e['image']):
            err = 'image: dump=%r 期望=%r' % (rec['image'], e['image'])
        if err:
            fails.append('%s: %s' % (name or path.split('/')[-1], err))
        else:
            oks += 1
    if fails:
        return False, '%d/%d 节点不符：%s' % (len(fails), len(exp), '；'.join(fails[:4]))
    return True, '%d 个节点逐项吻合（%s）' % (oks, os.path.basename(dp))


def _sbs_rows(path):
    if not os.path.exists(path):
        return []
    _, rows = read_tsv(path)
    return rows


def op_human_sign(row, sbs_path):
    fp = row['FP_ID']
    signed = []
    for r in _sbs_rows(sbs_path):
        if len(r) < 8:
            continue
        if r[0].strip() != fp:
            continue
        verdict = r[6].strip()
        signer = r[7].strip()
        if verdict in ('一致', '有差异') and signer:
            signed.append(verdict)
    if signed:
        return 'pass', '已签 %d 条 (%s)' % (len(signed), ','.join(signed))
    return 'manual', '并排比记账里没有该 FP 的已签行'


def op_n_a(row):
    return 'na', 'impossible: 原版 inj 失败，卡数据在远端'


# ---------------------------------------------------------------- driver

def run_row(row, sbs_path):
    op = row['判定方式'].strip()
    typ = row['oracle类型'].strip()
    if typ not in ORACLE_TYPES:
        return 'fail', 'oracle 类型不在闭集: %s' % typ
    allowed = TYPE_OPS.get(typ)
    if allowed is None:
        return 'fail', 'oracle 类型 %s 本工具未实现（批次 2/3/4 之外；判定方式 %s）' % (op, typ)
    if op not in allowed:
        return 'fail', '判定方式 %s 与 oracle 类型 %s 不匹配（该类型可用：%s）' % (
            op, typ, ','.join(sorted(allowed)))
    if op == 'human_sign':
        return op_human_sign(row, sbs_path)
    if op == 'n_a':
        return op_n_a(row)
    fn = globals().get('op_' + op)
    if fn is None:
        return 'fail', '判定方式 未实现: %s' % op
    try:
        res = fn(row)
    except Exception as e:
        return 'fail', '%s 抛异常: %s' % (op, e)
    if not (isinstance(res, tuple) and len(res) == 2):
        return 'fail', '%s 返回值格式错: %r' % (op, res)
    ok, detail = res
    if ok is True:
        return 'pass', detail
    if ok is False:
        return 'fail', detail
    if ok in ('manual', 'missing', 'na'):
        return ok, detail
    return 'fail', '未知状态 %r' % (ok,)


def main():
    ap = argparse.ArgumentParser(
        description='Check 判据/功能点_oracle规格.tsv (batch 1-4: asset_field + code_const + effect_render + card_truth + orig_run_dump)',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__)
    ap.add_argument('--spec', default=SPEC_DEFAULT, help='oracle spec TSV (12 cols)')
    ap.add_argument('--sbs', default=SBS_DEFAULT, help='side-by-side ledger TSV (10 cols)')
    ap.add_argument('--only', default='', help='comma separated FP ids (prefix match)')
    ap.add_argument('--types', default='', help='comma separated oracle types to run')
    ap.add_argument('--list', action='store_true', help='list rows and exit')
    ap.add_argument('--json', default='', help='write a JSON report to this path')
    ap.add_argument('--out', default=OUT_DEFAULT, help='output dir for logs/report')
    args = ap.parse_args()

    if not os.path.exists(args.spec):
        print('spec not found: %s' % args.spec)
        return 2
    hdr, raw_rows = read_tsv(args.spec)
    if hdr != HEADER:
        print('WARN: spec header differs from the 12-column contract')
        print('  got: %r' % (hdr,))
        print('  exp: %r' % (HEADER,))

    only = [x.strip() for x in args.only.split(',') if x.strip()]
    types = [x.strip() for x in args.types.split(',') if x.strip()]

    rows = []
    for cells in raw_rows:
        if len(cells) < len(HEADER):
            cells = cells + [''] * (len(HEADER) - len(cells))
        row = dict(zip(HEADER, cells))
        if only and not any(row['FP_ID'].startswith(o) for o in only):
            continue
        if types and row['oracle类型'].strip() not in types:
            continue
        rows.append(row)

    if args.list:
        for r in rows:
            print('%-14s %-13s %-13s %s' % (r['FP_ID'], r['oracle类型'], r['判定方式'], r['功能点'][:40]))
        print('total %d row(s)' % len(rows))
        return 0

    outdir = args.out
    os.makedirs(outdir, exist_ok=True)

    counts = {'pass': 0, 'fail': 0, 'manual': 0, 'na': 0, 'missing': 0}
    results = []
    for r in rows:
        status, detail = run_row(r, args.sbs)
        counts[status] += 1
        tag = {'pass': 'PASS', 'fail': 'FAIL', 'manual': '人工', 'na': 'N/A ', 'missing': '缺  '}[status]
        print('[%s] %-14s %-12s %-10s %s' % (tag, r['FP_ID'], r['oracle类型'], r['判定方式'], detail[:160]))
        results.append({'fp': r['FP_ID'], 'op': r['判定方式'], 'type': r['oracle类型'],
                        'status': status, 'detail': detail, 'evidence': r['证据(文件:行/资产路径)']})

    M = counts['missing']
    X, Y, Z, W = counts['pass'], counts['fail'] + M, counts['manual'], counts['na']
    summary = '=== FP oracle：机器通过 %d / 机器失败 %d / 人工待签 %d / N/A %d ===' % (X, Y, Z, W)
    print(summary)
    if M:
        print('（其中「缺数据」%d 行已计入机器失败：数据源不存在 ≠ 通过，见各行 detail）' % M)
    if Y > 0 or Z > 0:
        rc = 1
    elif X == 0:
        rc = 2
    else:
        rc = 0
    print('rc=%d  (%s)' % (rc, 'machine red' if Y else ('manual pending' if Z else ('nothing measured' if X == 0 else 'green'))))

    report = {'summary': {'machine_pass': X, 'machine_fail': Y, 'machine_fail_missing_data': M,
                          'manual_pending': Z, 'na': W},
              'rc': rc, 'generated': time.strftime('%Y-%m-%d %H:%M:%S'), 'rows': results}
    if args.json:
        jp = args.json
        if not os.path.isabs(jp):
            if jp.replace('\\', '/').startswith('_tmp_view/'):
                jp = 'd:/4/' + jp.replace('\\', '/')
            else:
                jp = os.path.join(outdir, os.path.basename(jp))
        os.makedirs(os.path.dirname(jp), exist_ok=True)
        with io.open(jp, 'w', encoding='utf-8', newline='\n') as f:
            json.dump(report, f, ensure_ascii=False, indent=1)
        print('json -> %s' % jp)
    with io.open(os.path.join(outdir, 'last_run.txt'), 'w', encoding='utf-8', newline='\n') as f:
        f.write(summary + '\n' + 'rc=%d\n' % rc)
    return rc


if __name__ == '__main__':
    sys.exit(main())
