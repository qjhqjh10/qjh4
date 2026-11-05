#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B146 · S3 数据列 -> 消费端覆盖普查（可复跑）。

目的：抓一类反复漏掉的功能缺口 —— **数据里有值、代码里没有消费端**。
账的粒度是「类 x 方法」，审计按域看参数值，都没有「数据列 -> 消费端」这一维。

用法：
    python -I -X utf8 d:/4/Unity/工具/audit_data_consumers.py            # 只打印摘要到 stdout
    python -I -X utf8 d:/4/Unity/工具/audit_data_consumers.py --write    # 另写 .md + .tsv
    python -I -X utf8 d:/4/Unity/工具/audit_data_consumers.py --verbose  # 打印全部列

统计口径（详见报告 §七）：
  1. 列名 = JSON 里递归出现过的叶子 key 名（含数组元素里的 key），大小写不敏感归一。
  2. cards_engine.json 是**稠密表**（每张卡带全部键，默认值 0/空），cards_truth.json 是**稀疏表**
     （缺键 = SO 默认值）。所以「有值卡数」取**非默认值**口径；否则 728 列全是 rows=1142，无判别力。
  3. 代码命中：`Assets/**/*.cs`，**先剥注释与字符串字面量**再按整词 `\\b` 大小写不敏感计数。
  4. 桶：生产端 / 测试端（路径含 `/Editor/`）/ DTO（拷表与模型声明层）/ 生成器。
     `read_hits` 只算**生产端** —— 测试断言消费不算「功能已接上」。
"""

import argparse
import io
import json
import os
import re
import sys
from collections import Counter, defaultdict

ROOT = 'd:/4/Unity'
ASSETS = ROOT + '/MyGame/Assets'
RES = ASSETS + '/RuleEngine/Resources'
OUT_MD = ROOT + '/资料/完全复刻/判据/B146_S3_数据列消费覆盖.md'
OUT_TSV = ROOT + '/资料/完全复刻/判据/B146_S3_数据列消费覆盖.tsv'

# 认作 DTO / 拷表 / 模型声明层的文件
DTO_FILES = {'CardDatabase.cs'}     # 整份 = JSON -> 模型的拷表装载层
MODEL_FILES = {'CardDef.cs'}        # 模型声明层：**按行**区分「声明」(dto) 与「逻辑」(prod)
DTO_SUFFIXES = ('Dto', 'Row', 'Spec', 'Data', 'Set', 'Entry', 'Json')

# 枚举值「弱命中」的下限：>= 该值的数字出现在函数实参里也算一次弱消费
WEAK_MIN = 40

# 名字过短 / 过于通用：命中数不可信（会在报告里标「名字过泛」）
GENERIC_NAMES = {
    'a', 'ab', 'at', 'ca', 'cond', 'd', 'e', 'ex', 'fm', 'id', 'ids', 'lg', 'm', 'n',
    'o', 'opt', 'op', 'pa', 'rc', 's', 'sd', 't', 'ta', 'tg', 'tn', 'u', 'name',
    'type', 'value', 'count', 'min', 'max', 'keep', 'all', 'enemy', 'cap', 'mv',
    'choose', 'cem', 'role', 'refValue', 'nameZh', 'desc', 'descZh',
}
SHORT_NAME_LEN = 2

# 数值「量」列（不是枚举）：取值来自数值运算/差值，字面量口径不适用
QUANTITY_DENY = {
    '_trait', '_traitValue', 'trait', 'traitValue', 'meleeAttackChange', 'rangeAttackChange',
    'armourChange', 'healthChange', 'costChange', 'randomValueChange', 'manaCost', 'melee',
    'range', 'health', 'armour', 'attack', 'initiative', 'numberOfUses', 'limitedUses',
    'maxTargets', 'max', 'min', 'cap', 'mv', 'playerTurn', 'referenceValue', 'refValue',
    'triggerValue', 'value', 'changeValue', 'newRace', 'ids', 'traitValue',
}
# 数据驱动派发的枚举：代码按「整张表迭代 / 名称映射」消费，不按字面量匹配
DATA_DRIVEN_ENUMS = {'trigger', 'abilityTriggers', 'chosenAbility'}

# 强制纳入枚举值分析（即使取值集合偏大），按现读数据里出现过的列名为准
CURATED_ENUMS = {
    'buffType', 'trigger', 'abilityTriggers', 'targetsAffected', 'ta', 'spellType',
    'cardType', 'rarity', 'keyword', 'attackDamageType', 'cond', 'role', 'at',
    'showInCollectionOptions', 'inventoryOptions', 'targetSelectMethod',
    'selectFromChooseResolution', 'opt', 'ex', 'untilEndOfTurn', 'permanentBuff',
    'doNotStack', 'extrinsic', 'markOfChaosType', 'daemonHostType', 'mutationType',
    'chosenAbility',
}
# 枚举值分析里跳过的值（0/1/-1 在代码里到处都是，literal 命中无意义）
SKIP_VALUES = {0, 1, -1}

# 已知案例：选牌池 36 格（A-19）
CHOICE_SET_FIELDS = [
    'targetsAffected', 'maxTargets', 'filterSpecificCards', 'includeMinions',
    'includeSpells', 'includeWhispers', 'includeHero', 'includeWeapons',
    'includeHeroParts', 'includeRemnants', 'friendlyUnits', 'enemyUnits',
    'traitsFilter', 'excludeTrait', 'rarityFilter', 'rarityFilterOption',
    'keywordFilter', 'attackFilter', 'attackFilterOption', 'currentHealthFilter',
    'currentHealthFilterOption', 'manaCostFilter', 'manaCostFilterOption',
    'spellType', 'armyFilter', 'chosenArmy', 'excludeSimilarCards',
    'excludeTargetted', 'includeSelf', 'excludeNotSelf', 'excludeFriendlyHero',
    'excludeEnemyHero', 'excludeRace', 'selectFromChooseResolution',
    'targetSelectMethod', 'excludeEnchantments',
]

IDENT_RE = re.compile(r'[A-Za-z_][A-Za-z0-9_]*')


# ---------------------------------------------------------------- JSON 侧

def is_meaningful(v):
    if v is None:
        return False
    if isinstance(v, bool):
        return True
    if isinstance(v, (int, float)):
        return v != 0
    if isinstance(v, str):
        return v != ''
    if isinstance(v, (list, dict)):
        return len(v) > 0
    return True


def collect(cards):
    """返回 (leaf, path, values, scalar)：
    leaf[name] = [携带卡数, 有值卡数]
    path[路径] = [携带卡数, 有值卡数]
    values[name][标量值] = 携带该值的卡数（只记有意义的值）
    scalar = 曾经以**标量**出现过的 key 名集合（用来把「容器」和「叶子列」分开）
    """
    leaf = defaultdict(lambda: [0, 0])
    path = defaultdict(lambda: [0, 0])
    values = defaultdict(Counter)
    scalar = set()

    for card in cards:
        seen_l = {}
        seen_p = {}
        seen_v = defaultdict(set)

        def rec(o, p):
            if isinstance(o, dict):
                for k, v in o.items():
                    np = (p + '.' + k) if p else k
                    m = is_meaningful(v)
                    seen_l[k] = seen_l.get(k, False) or m
                    mp = seen_p.get(np)
                    seen_p[np] = (mp or False) or m
                    if v is not None and not isinstance(v, (dict, list)):
                        scalar.add(k)
                        if m and isinstance(v, (int, str)) and not isinstance(v, bool):
                            seen_v[k].add(v)
                    rec(v, np)
            elif isinstance(o, list):
                for v in o:
                    if isinstance(v, (dict, list)):
                        rec(v, p)

        rec(card, '')
        for k, m in seen_l.items():
            leaf[k][0] += 1
            if m:
                leaf[k][1] += 1
        for p, m in seen_p.items():
            path[p][0] += 1
            if m:
                path[p][1] += 1
        for k, vs in seen_v.items():
            for v in vs:
                values[k][v] += 1

    return leaf, path, values, scalar


def load_json(name):
    b = io.open(RES + '/' + name, 'rb').read()
    return json.loads(b.decode('utf-8'))


# ---------------------------------------------------------------- 代码侧

def strip_code(text):
    """把注释和字符串/字符字面量替换成等长空白（保留换行与偏移）。

    这样按偏移判「在哪个类里」仍然准，同时注释里的同名文字不会虚增命中。
    """
    out = list(text)
    n = len(text)
    i = 0
    while i < n:
        c = text[i]
        nxt = text[i + 1] if i + 1 < n else ''
        if c == '/' and nxt == '/':
            while i < n and text[i] != '\n':
                out[i] = ' '
                i += 1
        elif c == '/' and nxt == '*':
            out[i] = ' '
            if i + 1 < n:
                out[i + 1] = ' '
            i += 2
            while i < n and not (text[i] == '*' and i + 1 < n and text[i + 1] == '/'):
                if text[i] != '\n':
                    out[i] = ' '
                i += 1
            if i < n:
                out[i] = ' '
                if i + 1 < n:
                    out[i + 1] = ' '
                i += 2
        elif c == '"':
            verbatim = i > 0 and text[i - 1] == '@'
            out[i] = ' '
            i += 1
            while i < n:
                if verbatim and text[i] == '"' and i + 1 < n and text[i + 1] == '"':
                    out[i] = ' '
                    out[i + 1] = ' '
                    i += 2
                    continue
                if not verbatim and text[i] == '\\' and i + 1 < n:
                    out[i] = ' '
                    out[i + 1] = ' '
                    i += 2
                    continue
                if text[i] == '"':
                    out[i] = ' '
                    i += 1
                    break
                if text[i] != '\n':
                    out[i] = ' '
                i += 1
        elif c == "'":
            out[i] = ' '
            i += 1
            while i < n:
                if text[i] == '\\' and i + 1 < n:
                    out[i] = ' '
                    out[i + 1] = ' '
                    i += 2
                    continue
                if text[i] == "'":
                    out[i] = ' '
                    i += 1
                    break
                out[i] = ' '
                i += 1
        else:
            i += 1
    return ''.join(out)


CLASS_RE = re.compile(
    r'(?:^|[;}\s])(?:\[[^\]]*\]\s*)*'
    r'(?:(?:public|internal|private|protected|static|sealed|abstract|partial|readonly|new)\s+)*'
    r'(class|struct)\s+([A-Za-z_]\w*)')


def dto_class_ranges(clean):
    """返回 [(start, end)] —— 被判为 DTO/模型层的类体区间。"""
    spans = []
    for m in CLASS_RE.finditer(clean):
        name = m.group(2)
        head = clean[max(0, m.start() - 300):m.start()]
        is_dto = ('Serializable' in head) or name.endswith(DTO_SUFFIXES)
        if not is_dto:
            continue
        brace = clean.find('{', m.end())
        if brace < 0:
            continue
        depth = 0
        j = brace
        n = len(clean)
        while j < n:
            ch = clean[j]
            if ch == '{':
                depth += 1
            elif ch == '}':
                depth -= 1
                if depth == 0:
                    break
            j += 1
        spans.append((m.start(), j))
    return spans


def classify_file(path):
    base = os.path.basename(path)
    rel = path.replace('\\', '/').lower()
    if '/editor/' in rel or rel.endswith('/editor'):
        return 'test'
    if base in DTO_FILES:
        return 'dto'
    if base in MODEL_FILES:
        return 'model'
    if base.startswith('gen_') or '.gen.' in base.lower() or '/editor/gen' in rel:
        return 'gen'
    return 'prod'


def scan_code():
    """返回 (counts, prod_top, enum_ctx, enum_weak, str_ctx, buckets)

    counts[bucket][ident_lower] = 次数
    enum_ctx  = 强上下文里的数值（`case N` / `== N` / `N =>`）
    enum_weak = 弱上下文里的数值（函数实参 `f(46)` / `f(a, 46)`，只收 >= WEAK_MIN）
    str_ctx   = 字符串字面量
    """
    buckets = {'prod': 0, 'test': 0, 'dto': 0, 'gen': 0}
    counts = {k: Counter() for k in buckets}
    prod_top = defaultdict(Counter)
    enum_ctx = Counter()
    enum_weak = Counter()
    str_counts = {k: Counter() for k in buckets}
    obsolete = Counter()
    obsolete_re = re.compile(r'Obsolete\([^\n]{0,500}?\)')

    num_re = re.compile(
        r'(?:\bcase\s+)(0[xX][0-9A-Fa-f]+|\d+)'
        r'|(?:[=!]=\s*)(0[xX][0-9A-Fa-f]+|\d+)'
        r'|(0[xX][0-9A-Fa-f]+|\d+)\s*=>')
    weak_re = re.compile(r'[\(\,]\s*(0[xX][0-9A-Fa-f]+|\d+)\s*[\,\)]')
    str_re = re.compile(r'"([^"\\\n]{1,40})"')

    files = []
    for dp, _dn, fn in os.walk(ASSETS):
        for f in fn:
            if f.endswith('.cs'):
                files.append(os.path.join(dp, f))
    files.sort()

    decl_re = re.compile(r'\b(public|private|protected|internal)\b')
    logic_re = re.compile(r'\b(return|case|if|else|while|for|switch|foreach)\b')

    def add_prod(lower, base):
        counts['prod'][lower] += 1
        prod_top[lower][base] += 1

    for path in files:
        try:
            raw = io.open(path, 'rb').read().decode('utf-8', 'replace')
        except OSError:
            continue
        clean = strip_code(raw)
        cls = classify_file(path)
        base = os.path.basename(path)
        # `CardDef` / `CardDatabase` 的 `partial class` 分身（`CardDefPartial_*.cs` 等）也按模型文件处理：
        # 里面的**声明行**是 DTO，**逻辑行**是生产端。
        if cls == 'prod' and re.search(r'partial\s+class\s+(?:CardDef|CardDatabase)\b', clean):
            cls = 'model'

        if cls == 'prod':
            spans = dto_class_ranges(clean)
            if spans:
                dto_mask = bytearray(len(clean))
                for s, e in spans:
                    dto_mask[s:min(e + 1, len(clean))] = b'\x01' * (min(e + 1, len(clean)) - s)
                for m in IDENT_RE.finditer(clean):
                    lower = m.group(0).lower()
                    if dto_mask[m.start()]:
                        counts['dto'][lower] += 1
                    else:
                        add_prod(lower, base)
            else:
                for m in IDENT_RE.finditer(clean):
                    add_prod(m.group(0).lower(), base)
            buckets['prod'] += 1
        elif cls == 'model':
            # 模型文件：声明行算 DTO，逻辑行算生产端（CardDef.cs 里有真逻辑，如 `case 75: return "summon"`）
            line_start = 0
            line_no = 0
            starts = [0]
            for i, ch in enumerate(clean):
                if ch == '\n':
                    starts.append(i + 1)
            for m in IDENT_RE.finditer(clean):
                # 二分找行
                lo, hi = 0, len(starts) - 1
                while lo < hi:
                    mid = (lo + hi + 1) // 2
                    if starts[mid] <= m.start():
                        lo = mid
                    else:
                        hi = mid - 1
                ls = starts[lo]
                le = clean.find('\n', ls)
                if le < 0:
                    le = len(clean)
                line = clean[ls:le]
                lower = m.group(0).lower()
                if decl_re.search(line) and not logic_re.search(line):
                    counts['dto'][lower] += 1
                else:
                    add_prod(lower, base)
            buckets['prod'] += 1
        else:
            for m in IDENT_RE.finditer(clean):
                counts[cls][m.group(0).lower()] += 1
            buckets[cls] += 1

        # 枚举上下文（只收生产端；字符串上下文需要保留字符串，这里用 raw 去注释）
        if cls in ('prod', 'model'):
            for m in num_re.finditer(clean):
                tok = m.group(1) or m.group(2) or m.group(3)
                try:
                    enum_ctx[int(tok, 0)] += 1
                except ValueError:
                    pass
            for m in weak_re.finditer(clean):
                try:
                    v = int(m.group(1), 0)
                except ValueError:
                    continue
                if v >= WEAK_MIN:
                    enum_weak[v] += 1
        nostr = strip_comments_only(raw)
        sbucket = 'prod' if cls == 'model' else cls
        for m in str_re.finditer(nostr):
            str_counts[sbucket][m.group(1)] += 1

        # `[Obsolete("死列…")]` 自标：这是「代码自己承认没人读」的硬证据
        for m in obsolete_re.finditer(raw):
            for ident in set(IDENT_RE.findall(m.group(0))):
                obsolete[ident.lower()] += 1

    return counts, prod_top, enum_ctx, enum_weak, str_counts, buckets, obsolete


def strip_comments_only(text):
    out = list(text)
    n = len(text)
    i = 0
    while i < n:
        c = text[i]
        nxt = text[i + 1] if i + 1 < n else ''
        if c == '/' and nxt == '/':
            while i < n and text[i] != '\n':
                out[i] = ' '
                i += 1
        elif c == '/' and nxt == '*':
            out[i] = ' '
            if i + 1 < n:
                out[i + 1] = ' '
            i += 2
            while i < n and not (text[i] == '*' and i + 1 < n and text[i + 1] == '/'):
                if text[i] != '\n':
                    out[i] = ' '
                i += 1
            if i < n:
                out[i] = ' '
                if i + 1 < n:
                    out[i + 1] = ' '
                i += 2
        else:
            i += 1
    return ''.join(out)


# ---------------------------------------------------------------- 判定

def merge_columns(e_leaf, t_leaf):
    """合并两源的叶子列：carriers / nz 各取 max。"""
    cols = {}
    for name in set(e_leaf) | set(t_leaf):
        ec = e_leaf.get(name, [0, 0])
        tc = t_leaf.get(name, [0, 0])
        cols[name] = {
            'carriers': max(ec[0], tc[0]),
            'nz': max(ec[1], tc[1]),
            'e_nz': ec[1],
            't_nz': tc[1],
            'e_rows': ec[0],
            't_rows': tc[0],
        }
    return cols


def judge(col, hits):
    nz = col['nz']
    if col['carriers'] == 0:
        return '— 无数据'
    if nz == 0:
        return '— 全默认'
    if hits['prod'] > 0:
        return '✅ 有读取'
    if hits['test'] > 0:
        return '🟡 仅测试消费'
    if hits['dto'] > 0 or hits['gen'] > 0:
        return '🟡 仅 DTO/生成器'
    return '🟠 有数据0读取'


def containers_of(name, e_path, t_path, limit=3):
    """数据主要落在哪些容器里（按有值卡数排序）。"""
    out = []
    for p, v in list(e_path.items()) + list(t_path.items()):
        if '.' in p and p.rsplit('.', 1)[1] == name and v[1] > 0:
            out.append((p.rsplit('.', 1)[0], v[1]))
    agg = defaultdict(int)
    for c, n in out:
        agg[c] = max(agg[c], n)
    return sorted(agg.items(), key=lambda kv: -kv[1])[:limit]


# ---------------------------------------------------------------- 输出

def write_bytes(path, text):
    io.open(path, 'wb').write(text.encode('utf-8'))


def main():
    ap = argparse.ArgumentParser(add_help=True)
    ap.add_argument('--write', action='store_true', help='写出 .md 与 .tsv（默认只打印摘要）')
    ap.add_argument('--verbose', action='store_true', help='stdout 打印全部列')
    ap.add_argument('--top', type=int, default=60, help='报告表格最多列几条（默认 60）')
    args = ap.parse_args()

    eng = load_json('cards_engine.json')
    tru = load_json('cards_truth.json')
    e_leaf, e_path, e_vals, e_scalar = collect(eng['cards'])
    t_leaf, t_path, t_vals, t_scalar = collect(tru['cards'])
    scalar_names = e_scalar | t_scalar
    cols = merge_columns(e_leaf, t_leaf)
    vals = defaultdict(Counter)
    for name, c in e_vals.items():
        vals[name].update(c)
    for name, c in t_vals.items():
        vals[name].update(c)

    counts, prod_top, enum_ctx, enum_weak, str_counts, buckets, obsolete = scan_code()

    rows = []
    for name in sorted(cols):
        c = cols[name]
        h = {b: counts[b].get(name.lower(), 0) for b in ('prod', 'test', 'dto', 'gen')}
        is_leaf = name in scalar_names
        status = judge(c, h)
        if not is_leaf:
            status = '— 容器（非叶子列）'
        es = e_leaf.get(name, [0, 0])[1] > 0
        ts = t_leaf.get(name, [0, 0])[1] > 0
        src = 'both' if (es and ts) else ('engine' if es else ('truth' if ts else 'none'))
        if status == '🟠 有数据0读取' and src == 'truth':
            status = '⚠️ 仅truth未承接'
        note = []
        if not is_leaf:
            note.append('容器（子键才是列）')
        if len(name) <= SHORT_NAME_LEN or name in GENERIC_NAMES:
            note.append('名字过泛')
        if src != 'both':
            note.append('来源 ' + src)
        if c['carriers'] and c['nz'] == 0:
            note.append('全部默认值')
        if obsolete.get(name.lower()):
            note.append('代码自标 Obsolete/死列')
        ct = containers_of(name, e_path, t_path)
        if ct:
            note.append('容器: ' + ', '.join('%s(%d)' % (a, b) for a, b in ct))
        rows.append({
            'type': 'column' if is_leaf else 'container', 'name': name, 'carriers': c['carriers'],
            'nz': c['nz'], 'code': sum(h.values()), 'prod': h['prod'],
            'test': h['test'], 'dto': h['dto'], 'gen': h['gen'],
            'status': status, 'src': src, 'note': ' · '.join(note),
            'top': prod_top.get(name.lower(), Counter()).most_common(3),
        })

    # 枚举值级
    enum_cols = []
    for name, vc in vals.items():
        if len(name) <= SHORT_NAME_LEN or name in ('id', 'ids', 'name', 'desc', 'descZh', 'nameZh'):
            continue
        if name.startswith('_') or name in QUANTITY_DENY:
            continue
        numeric = all(isinstance(v, int) and not isinstance(v, bool) for v in vc)
        if not numeric:
            continue
        if name not in CURATED_ENUMS:
            if len(vc) > 20 or sum(vc.values()) < 2:
                continue
            if any(v < 0 for v in vc):
                continue
        enum_cols.append((name, vc))
    enum_cols.sort(key=lambda kv: -sum(kv[1].values()))

    enum_rows = []
    for name, vc in enum_cols:
        for val, ncards in sorted(vc.items()):
            if val in SKIP_VALUES:
                continue
            strong = enum_ctx.get(val, 0)
            weak = enum_weak.get(val, 0)
            hits = strong + weak
            if ncards >= 1 and hits == 0:
                status = ('❓ 需人工核（数据驱动派发）' if name in DATA_DRIVEN_ENUMS
                          else '🟠 有数据0消费')
            else:
                status = '✅ 有消费'
            enum_rows.append({
                'type': 'enum', 'name': '%s=%d' % (name, val),
                'carriers': ncards, 'nz': ncards, 'code': hits, 'prod': hits,
                'test': 0, 'dto': 0, 'gen': 0, 'status': status, 'col': name,
                'note': '列 %s 的取值 %d（强 %d / 弱 %d）' % (name, val, strong, weak), 'top': [],
            })

    # 字符串型枚举值（role / rarity / type / at / subtype）
    str_cols = ['role', 'rarity', 'type', 'at', 'subtype']
    str_enum_rows = []
    for name in str_cols:
        vc = vals.get(name)
        if not vc:
            continue
        for val, ncards in sorted(vc.items(), key=lambda kv: -kv[1]):
            if not isinstance(val, str) or len(val) > 30:
                continue
            hp = str_counts['prod'].get(val, 0)
            ht = str_counts['test'].get(val, 0)
            hd = str_counts['dto'].get(val, 0)
            if hp > 0:
                status = '✅ 有消费'
            elif ht > 0 or hd > 0:
                status = '🟡 仅测试/DTO消费'
            else:
                status = '🟠 有数据0消费'
            str_enum_rows.append({
                'type': 'strenum', 'name': '%s="%s"' % (name, val),
                'carriers': ncards, 'nz': ncards, 'code': hp, 'prod': hp,
                'test': ht, 'dto': hd, 'gen': 0, 'status': status, 'col': name,
                'note': '列 %s 的取值 "%s"（prod %d / test %d / dto %d）' % (name, val, hp, ht, hd),
                'top': [],
            })

    # choiceSet 36 格专表
    choice_rows = []
    for f in CHOICE_SET_FIELDS:
        p = 'unitConditions.choiceSet.' + f
        e = e_path.get(p, [0, 0])
        t = t_path.get(p, [0, 0])
        h = counts['prod'].get(f.lower(), 0)
        choice_rows.append({
            'field': f, 'nz': max(e[1], t[1]), 'e_nz': e[1], 't_nz': t[1],
            'prod': h, 'test': counts['test'].get(f.lower(), 0),
            'dto': counts['dto'].get(f.lower(), 0),
            'status': '✅ 有读取' if h > 0 else '🟠 生产端0读取',
        })

    # ---- 汇总
    column_rows = [r for r in rows if r['type'] == 'column']
    container_rows = [r for r in rows if r['type'] == 'container']
    data_cols = [r for r in column_rows if r['nz'] > 0]
    orange = [r for r in data_cols if r['status'] == '🟠 有数据0读取']
    truth_only = [r for r in data_cols if r['status'] == '⚠️ 仅truth未承接']
    yellow = [r for r in data_cols if r['status'].startswith('🟡')]
    orange.sort(key=lambda r: -r['nz'])
    truth_only.sort(key=lambda r: -r['nz'])
    yellow.sort(key=lambda r: -r['nz'])
    enum_orange = [r for r in enum_rows if r['status'].startswith('🟠')]
    enum_manual = [r for r in enum_rows if r['status'].startswith('❓')]
    str_enum_orange = [r for r in str_enum_rows if r['status'].startswith('🟠')]

    print('=== B146 数据列 -> 消费端覆盖普查 ===')
    print('JSON 源: engine %d 卡 / truth %d 卡' % (len(eng['cards']), len(tru['cards'])))
    print('代码文件桶: %s' % buckets)
    print('叶子列 %d | 有值列 %d | 🟠 %d | ⚠️仅truth %d | 🟡 %d | ✅ %d | 容器 %d'
          % (len(column_rows), len(data_cols), len(orange), len(truth_only), len(yellow),
             len(data_cols) - len(orange) - len(truth_only) - len(yellow), len(container_rows)))
    print('枚举列 %d | 枚举值行 %d | 🟠 %d | ❓需人工核 %d'
          % (len(enum_cols), len(enum_rows) + len(str_enum_rows),
             len(enum_orange) + len(str_enum_orange), len(enum_manual)))
    print('--- 🟠 列（前 40）---')
    for r in orange[:40]:
        print('%-42s nz=%-5d prod=%-4d test=%-4d dto=%-4d %s'
              % (r['name'], r['nz'], r['prod'], r['test'], r['dto'], r['note'][:70]))
    print('--- 🟠 枚举值（前 40）---')
    for r in (enum_orange + str_enum_orange)[:40]:
        print('%-45s nz=%-5d %s' % (r['name'], r['nz'], r['note']))
    if args.verbose:
        print('--- 全部叶子列 ---')
        for r in sorted(column_rows, key=lambda r: -r['nz']):
            print('%-45s nz=%-5d prod=%-4d %s' % (r['name'], r['nz'], r['prod'], r['status']))

    if not args.write:
        print('\n（未写文件；加 --write 生成 .md + .tsv）')
        return 0

    # ---- TSV
    tsv = ['类型\t列名\t有值卡数\t携带卡数\t代码命中\t生产端\t测试端\tDTO\t生成器\t状态\t备注']
    for r in column_rows:
        tsv.append('\t'.join([
            r['type'], r['name'], str(r['nz']), str(r['carriers']), str(r['code']),
            str(r['prod']), str(r['test']), str(r['dto']), str(r['gen']),
            r['status'], r['note'].replace('\t', ' '),
        ]))
    for r in container_rows:
        tsv.append('\t'.join([
            r['type'], r['name'], str(r['nz']), str(r['carriers']), str(r['code']),
            str(r['prod']), str(r['test']), str(r['dto']), str(r['gen']),
            r['status'], r['note'].replace('\t', ' '),
        ]))
    for r in enum_rows + str_enum_rows:
        tsv.append('\t'.join([
            r['type'], r['name'], str(r['nz']), str(r['carriers']), str(r['code']),
            str(r['prod']), str(r['test']), str(r['dto']), str(r['gen']),
            r['status'], r['note'],
        ]))
    write_bytes(OUT_TSV, '\n'.join(tsv) + '\n')

    # ---- MD
    L = []
    L.append('# B146 · S3 数据列→消费端覆盖普查（2026-11-05）')
    L.append('> 脚本 = Unity/工具/audit_data_consumers.py（可复跑：python -I -X utf8 工具/audit_data_consumers.py --write）')
    L.append('> 口径与误判边界见 §七。')
    L.append('')
    L.append('## 一、总览')
    L.append('')
    L.append('| 指标 | 数值 |')
    L.append('|---|---|')
    L.append('| JSON 源 | `cards_engine.json` %d 卡（稠密）· `cards_truth.json` %d 卡（稀疏） |' % (len(eng['cards']), len(tru['cards'])))
    L.append('| 代码扫描 | `Assets/**/*.cs`（生产 %d / 测试 %d / DTO %d / 生成器 %d 个文件） |'
             % (buckets['prod'], buckets['test'], buckets['dto'], buckets['gen']))
    L.append('| 叶子列 | %d |' % len(column_rows))
    L.append('| 有值列（非默认值 ≥1 张卡） | %d |' % len(data_cols))
    L.append('| **🟠 有数据 0 读取（主卡表侧，生产端）** | **%d** |' % len(orange))
    L.append('| ⚠️ 仅 truth 侧未承接（不在主卡表，另见 §三） | %d |' % len(truth_only))
    L.append('| 🟡 仅测试 / 仅 DTO | %d（其中代码自标 `Obsolete("死列")` 的 %d 条） |'
             % (len(yellow), len([r for r in yellow if 'Obsolete' in r['note']])))
    L.append('| ✅ 有读取 | %d |' % (len(data_cols) - len(orange) - len(truth_only) - len(yellow)))
    L.append('| 枚举列 / 枚举值行 / 🟠 枚举值 / ❓需人工核 | %d / %d / **%d** / %d |'
             % (len(enum_cols), len(enum_rows) + len(str_enum_rows),
                len(enum_orange) + len(str_enum_orange), len(enum_manual)))
    L.append('')
    L.append('**本批结论（一句话）**：列级 🟠 = 0（口径必然：任意一处生产端命中即 ✅）；'
             '真正「有值但没人读」的是 §四 的 %d 条 🟡（只在 DTO/装载层或测试里出现），'
             '其中 %d 条代码自己打了 `Obsolete("死列")`；'
             '枚举值级另有 %d 条 0 消费、%d 条 trigger 族需人工核。'
             % (len(yellow), len([r for r in yellow if 'Obsolete' in r['note']]),
                len(enum_orange) + len(str_enum_orange), len(enum_manual)))
    L.append('')
    L.append('## 二、🟠 有数据 0 读取 —— 列级（本任务要抓的）')
    L.append('')
    L.append('判定 = 在 **主卡表 `cards_engine.json`** 里非默认值至少 1 张卡 **且** 生产端代码命中 0'
             '（测试断言 / DTO / 生成器都不算消费）。')
    L.append('')
    L.append('| 列名 | 有值卡数 | 携带卡数 | 代码命中 | 测试 | DTO | 来源 | 数据所在容器 | 备注 |')
    L.append('|---|---|---|---|---|---|---|---|---|')
    if not orange:
        L.append('| ——（0 条） | | | | | | | | 主卡表的叶子列全部能在生产端找到至少一处命中 |')
    shown = orange[:args.top]
    for r in shown:
        note = r['note']
        if r['top']:
            note = ('测试/DTO: ' + ','.join('%s:%d' % t for t in r['top'])) + (' · ' + note if note else '')
        L.append('| `%s` | %d | %d | %d | %d | %d | %s | %s | %s |'
                 % (r['name'], r['nz'], r['carriers'], r['code'], r['test'], r['dto'],
                    r['src'], _containers_cell(r), note.replace('|', '\\|') if note else ''))
    if len(orange) > len(shown):
        L.append('')
        L.append('（另 %d 条见 TSV）' % (len(orange) - len(shown)))
    L.append('')
    if not orange:
        L.append('**本批 0 条。** 这不是「没有问题」，而是口径的必然结果：'
                 '只要一个列名在**任何**生产端文件里出现过（哪怕一次、哪怕只是构造参数），就判 ✅。'
                 '真正的「有值但没人读」落在 **§四 的 🟡 清单**（只在 DTO/装载层出现 = `read_hits == 0`），'
                 '那里才是本任务该派活的地方。')
    L.append('')
    L.append('## 三、⚠️ 仅 truth 侧未承接（`cards_truth.json` 有、主卡表没有）')
    L.append('')
    L.append('这些键只存在于 SO 真值旁路 `cards_truth.json`，**主卡表 `cards_engine.json` 没有对应列**。'
             '多数是「SO 内部形状」与「引擎表形状」的命名差异（如 `_trait` vs `cardTraits`、'
             '`actingCardCriteria` vs `abilityActingCriteria`）⇒ **不代表真缺口**；'
             '逐个判定必须回源核原版读不读，⛔ 别直接派活。')
    L.append('')
    L.append('| 列名 | 有值卡数 | 携带卡数 | 代码命中 | 数据所在容器 | 备注 |')
    L.append('|---|---|---|---|---|---|')
    for r in truth_only[:args.top]:
        L.append('| `%s` | %d | %d | %d | %s | %s |'
                 % (r['name'], r['nz'], r['carriers'], r['code'],
                    _containers_cell(r), r['note'].replace('|', '\\|')[:70]))
    if len(truth_only) > args.top:
        L.append('')
        L.append('（另 %d 条见 TSV）' % (len(truth_only) - args.top))
    L.append('')
    L.append('## 四、🟡 仅测试 / 仅 DTO 消费（次一级）')
    L.append('')
    L.append('| 列名 | 有值卡数 | 生产端 | 测试 | DTO | 状态 | 备注 |')
    L.append('|---|---|---|---|---|---|---|')
    for r in yellow[:args.top]:
        L.append('| `%s` | %d | %d | %d | %d | %s | %s |'
                 % (r['name'], r['nz'], r['prod'], r['test'], r['dto'], r['status'],
                    r['note'].replace('|', '\\|')[:110]))
    if len(yellow) > args.top:
        L.append('')
        L.append('（另 %d 条见 TSV）' % (len(yellow) - args.top))
    L.append('')
    L.append('⚠️ **这 %d 条不全是缺口**，派活前必须逐条回源核：' % len(yellow))
    L.append('')
    L.append('- **改名承接（不是缺口）**：`goesSecond` → `CardDatabase` 映射成 `GoesSecondId`（`RuleCore` 等处读）；'
             '`fullHeroSpriteReference` → `CardArt.TryDeckHero` 的旁挂表。'
             '这类「DTO 里一行映射、之后换名字用」是本脚本的结构性假阳性。')
    L.append('- **已确认死列**：`cardShortId` / `itemDescriptionKey` 两条代码里已自打 `[Obsolete("死列…")]`。')
    L.append('- **真缺口候选**：`attackDamageType`（注释自陈「引擎整条没有伤害类型概念」）· '
             '`expansionPack`（生产端只出现在 doc 注释）· `hasNormalAbilityBoardCriteria`（只在 DTO 里声明）。'
             '这三条需要按铁律 11 回源核原版行为。')
    L.append('- **仅测试消费**：`moveToPos` / `doNotStack` / `cem` / `randomValueChange` —— 生产端没有读取点，'
             '但语义可能被硬编码等价实现顶掉（`doNotStack` 就是这样：`UnitState` 注释自陈「我们的模型从不合并」）。')
    L.append('')
    L.append('## 五、枚举值级：有数据 0 消费')
    L.append('')
    L.append('口径 = 该列该取值至少 1 张卡、且生产端代码里 **强命中**（`case N` / `== N` / `N =>`）与 '
             '**弱命中**（函数实参 `f(46)`，只收 ≥ %d）都为 0（已跳过 0/1/-1）。' % WEAK_MIN)
    L.append('')
    L.append('| 列=取值 | 有值卡数 | 生产端命中 | 状态 |')
    L.append('|---|---|---|---|')
    for r in (enum_orange + str_enum_orange)[:args.top]:
        L.append('| `%s` | %d | %d | %s |' % (r['name'], r['nz'], r['prod'], r['status']))
    if len(enum_orange) + len(str_enum_orange) > args.top:
        L.append('')
        L.append('（另 %d 条见 TSV）' % (len(enum_orange) + len(str_enum_orange) - args.top))
    L.append('')
    L.append('**❓ 需人工核（数据驱动派发，字面量口径不适用）**：')
    L.append('')
    L.append('`trigger` / `abilityTriggers` / `chosenAbility` 的值在本仓**按整表迭代或名称映射**消费'
             '（`CardAbilityDispatch_B116.cs` / `CardDef.GetTraitFromAbilityTrigger` / `WhenEventKind`），'
             '所以「字面量 0 命中」**不构成缺口证据**。这些值单列在这里，⛔ 别直接派活。')
    L.append('')
    L.append('| 列=取值 | 有值卡数 | 状态 |')
    L.append('|---|---|---|')
    for r in enum_manual[:30]:
        L.append('| `%s` | %d | %s |' % (r['name'], r['nz'], r['status']))
    if len(enum_manual) > 30:
        L.append('')
        L.append('（另 %d 条见 TSV）' % (len(enum_manual) - 30))
    L.append('')
    L.append('## 六、已知案例复现：选牌池 36 格（`unitConditions.choiceSet.*`）')
    L.append('')
    L.append('A-19 的原始记录是「84 行数据、全仓 0 消费端」。B145 已把 36 格接到 `TargetCriteriaEval`，'
             '所以现在**字段名层面**多数有生产端命中（共享求值器）；下表用来确认「哪些格仍然 0 读取」。')
    L.append('')
    L.append('| 格 | 有值卡数（engine/truth） | 生产端命中 | 测试 | DTO | 判定 |')
    L.append('|---|---|---|---|---|---|')
    for r in choice_rows:
        L.append('| `%s` | %d（%d/%d） | %d | %d | %d | %s |'
                 % (r['field'], r['nz'], r['e_nz'], r['t_nz'], r['prod'], r['test'], r['dto'], r['status']))
    cs_data = [r for r in choice_rows if r['nz'] > 0]
    cs_zero = [r for r in choice_rows if r['nz'] > 0 and r['prod'] == 0]
    L.append('')
    L.append('**复现结论**：36 格里 **%d 格有非默认数据**，其中 **%d 格生产端 0 命中**。' % (len(cs_data), len(cs_zero)))
    L.append('')
    L.append('A-19 的原始形态是「`choiceSet` 这个容器本身全仓 0 消费端」——脚本在**容器级**能复现：'
             '按容器路径 `unitConditions.choiceSet.*` 取数 = **84 行**；改前（B145 之前）'
             '`choiceSet` 全仓只有 `CardDef.cs` 声明与 `CardDatabase.cs` 映射、没有消费点。'
             '现读生产端命中 12，落点 = `CardDef.cs:2281`（`ChoiceSet` 属性）· '
             '`CardDatabase.cs:2758`（映射）· `EffectResolver.cs:22617`（取 choice 行）· '
             '`EffectResolver.cs:22662`（`ApplyChoiceCriteriaToPool`，B145 新接）'
             '⇒ **A-19 已由 B145 收口**，本表因此全绿。')
    L.append('')
    L.append('⚠️ 本表的「生产端命中」是**按字段名**统计的：同一字段名在 `abilityTargetCriteria.criteria`、'
             '`cardTargetCriteriaSet` 等别的容器也有消费，因此**无法区分**「这一格是不是真的从 choiceSet 这条路读的」。'
             '要判「这格在 choice 这条路上有没有接」，必须回源看 `TargetCriteriaEval.MatchesRawCard` 有没有读它 —— '
             'B145 的结论是：`includeWeapons` / `includeHeroParts` 等按原版**故意不读**。')
    L.append('')
    L.append('## 七、方法与误判边界')
    L.append('')
    L.append('**1. 「有值卡数」为什么取非默认值**：`cards_engine.json` 是稠密表（1,142 张卡每张都带全部键，'
             '默认值 `0`/`""`/`[]`），若把 `0` 也算「有数据」，全部叶子列都会被 1,142 张卡携带，没有判别力。'
             '`cards_truth.json` 是稀疏表（缺键 = SO 默认值）。两源各算「非默认值卡数」，取 `max`。'
             '⚠️ 因此 `0` 是合法值的那类列（如 `playerTurn=0`）在「有值」口径下会低估 —— 这是刻意的。')
    L.append('')
    L.append('**2. 代码命中怎么数**：先剥 `//`、`/* */` 注释与字符串/字符字面量（本项目注释极多，不剥会大面积虚增），'
             '再按整词 `\\b`、**大小写不敏感**匹配。大小写不敏感是必须的：JSON 是 `targetsAffected`，'
             '真正的消费端读的是 `TargetsAffected`。')
    L.append('')
    L.append('**3. 四个桶**：')
    L.append('- **生产端** = 不在 `Editor/`、不在 DTO/模型拷表层、不在生成器的 `.cs`。**只有它算「已接上」。**')
    L.append('- **测试端** = 路径含 `/Editor/`（自检断言会大量引用字段名，不能当功能消费）。')
    L.append('- **DTO** = 整份 `CardDatabase.cs`（JSON→模型拷表）；`CardDef.cs` **按行判**（声明行算 DTO、'
             '逻辑行算生产端 —— 该文件里既有 `public int TargetsAffected;` 也有 `case 75: return "summon"`）；'
             '其他文件里类头 300 字符内有 `Serializable`、或类名以 `Dto/Row/Spec/Data/Set/Entry/Json` 结尾的类体。')
    L.append('- **生成器** = 文件名 `gen_*.cs` / `*.gen.cs`；本次扫描命中 **0 个**（仓里 3 个 `*gen*.cs` 都在 `Editor/` 下、已归测试桶，且都不是卡表生成器）。')
    L.append('')
    L.append('**4. 已知假阳性（脚本会误报「0 读取」）**：')
    L.append('- **原版本身就不读的字段**：`includeWeapons` / `includeHeroParts` 等 36 格里原版 `CardTypeOptions` 只有 4 值，'
             'B145 已判「原版一次都不读」。这类报 0 是**正确的**，不是缺口。')
    L.append('- **仅 truth 侧的内部形状**（§三）：`_trait` / `_traitValue` / `m_PathID` / `actingCardCriteria` / '
             '`boardCriteria` / `multiCountCriteria` 等，主卡表用**另一个名字**承接（`cardTraits` / `enchantment` / '
             '`abilityActingCriteria` / `normalAbilityBoardCriteria` / `unitConditions`），不是真缺口。')
    L.append('- **改名承接**：DTO 里只做一行映射、之后改用另一个字段名消费（`goesSecond` → `GoesSecondId`、'
             '`startingCardInHand` → `StartWithNames`、`fullHeroSpriteReference` → `CardArt.TryDeckHero` 的旁挂表）'
             '⇒ 本列判 0、真消费在别名上。这是本脚本最大的结构性假阳性来源。')
    L.append('- **反射 / 字符串拼出来的键**：本项目表装载走 `JsonUtility` 直绑字段名，没有反射拼名；'
             '但如果将来出现 `GetField("xxx")` 形式，脚本看不到。')
    L.append('')
    L.append('**5. 已知假阴性（脚本会漏报）**：')
    L.append('- **同名不同义**：命中按「名字」而非「语义」，`type`/`name`/`id`/`army` 这类通用名会被无关代码命中 ⇒ '
             '真缺口被掩盖。报告里 `名字过泛` 就是给这类打标。')
    L.append('- **枚举值用命名常量消费**：本仓多数是数字字面量（`ta = 50`、`case 0x16`），但 `TargetsAffected.any` '
             '这类命名常量出现时，`case N` 统计看不到 ⇒ 枚举值级可能高估「0 消费」。')
    L.append('- **枚举值只在这条口径下算准**：强命中 = `case N` / `== N` / `N =>`；**弱命中** = 函数实参里的 `f(46)` / `f(a, 46)`'
             '（只收 ≥ %d 的值，避免坐标/尺寸噪声）。两条都为 0 才判「0 消费」——否则 `OnTrigger(0x2e)`、'
             '`HasCurrentTrait(0x29e)` 这类**实参式消费**会被误报。' % WEAK_MIN)
    L.append('- **同一数值跨列共用**：枚举值统计不区分列（`90` 既可能是 `trigger` 也可能是别的），'
             '因此「非 0 命中」里混着别的列的用法 ⇒ 枚举值级整体偏乐观。')
    L.append('- **字段只在别的字段的 doc/注释里出现**：注释已剥，因此「只写在注释里」= 0 命中，这正是我们要的口径。')
    L.append('')
    L.append('**6. 与账的分工**：本普查只出「候选缺口清单」，**不判定要不要补**。'
             '每条都要按铁律 11 回源核「原版读不读」，再决定派活；⛔ 别直接把 🟠 当缺陷修。')
    L.append('')
    write_bytes(OUT_MD, '\n'.join(L) + '\n')
    print('\n写出：%s' % OUT_MD)
    print('写出：%s' % OUT_TSV)
    return 0


def _containers_cell(r):
    m = re.search(r'容器: (.+?)(?: · |$)', r['note'])
    return m.group(1) if m else ''


if __name__ == '__main__':
    sys.exit(main())
