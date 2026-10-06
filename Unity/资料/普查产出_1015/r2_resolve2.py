# -*- coding: utf-8 -*-
"""R2 · 第三步（v2）：逐调用点解父链 —— 处理三元 / new GameObject / 字段 / 包装器形参。
输出：r2_resolve2.json
"""
import io, os, re, json, sys
sys.path.insert(0, r"d:/4/Unity/资料/普查产出_1015")
from r2_extract import read, strip_comments_keep_index, split_args, line_of, line_text

ROOT = r"d:/4/Unity/MyGame/Assets/CardPresentation"
OUT = r"d:/4/Unity/资料/普查产出_1015"

NOT_FACTORY = {'Text', 'TextBox', 'ClipText', 'SizeOf', 'SpanOf', 'Local', 'Px', 'Abs', 'Visible',
               'Find', 'GetComponent', 'Resolve', 'Hang', 'PaddedClip', 'IsNoClip', 'Sub', 'Off',
               'Add', 'Mul', 'Clamp', 'Min', 'Max', 'Round', 'Ceil', 'Floor'}

def find_blocks(masked):
    stack, pairs = [], []
    for i, c in enumerate(masked):
        if c == '{':
            stack.append(i)
        elif c == '}' and stack:
            pairs.append((stack.pop(), i))
    return pairs

def top_ternary(s):
    """在【顶层】找 `?` 与配对的 `:`（跳过括号内、跳过 `?.` / `??`）。返回 (真支, 假支) 或 None。"""
    d = 0
    q = -1
    i = 0
    n = len(s)
    while i < n:
        c = s[i]
        if c in '([{':
            d += 1
        elif c in ')]}':
            d -= 1
        elif d == 0 and c == '?':
            if i + 1 < n and s[i+1] in '.?':
                i += 1
                continue
            q = i
            break
        i += 1
    if q < 0:
        return None
    d = 0
    j = q + 1
    while j < n:
        c = s[j]
        if c in '([{':
            d += 1
        elif c in ')]}':
            d -= 1
        elif d == 0 and c == ':':
            return (s[q+1:j].strip(), s[j+1:].strip())
        j += 1
    return None

def collect_assignments(masked):
    res = []
    for m in re.finditer(r'(?:^|[;{}\n])\s*(?:var\s+|(?:[A-Za-z_][\w\.<>\[\],\? ]*?)\s+)?([A-Za-z_]\w*)\s*=\s*(?!=)', masked):
        name = m.group(1)
        if name in ('if', 'while', 'for', 'return', 'foreach', 'case', 'using', 'new', 'else', 'do', 'switch'):
            continue
        rhs_start = m.end()
        d, j, n = 0, rhs_start, len(masked)
        while j < n:
            c = masked[j]
            if c in '([{':
                d += 1
            elif c in ')]}':
                if d == 0:
                    break
                d -= 1
            elif c == ';' and d == 0:
                break
            j += 1
        rhs = masked[rhs_start:j].strip()
        if rhs:
            res.append((name, rhs, m.start(), j))
    return res

def method_params(masked):
    params = {}
    for m in re.finditer(r'\b([A-Za-z_]\w*)\s*\(([^;{}]*?)\)\s*(?:\{|=>|$)', masked, re.M):
        mname = m.group(1)
        if mname in ('if', 'while', 'for', 'foreach', 'switch', 'catch', 'lock', 'using', 'return', 'new'):
            continue
        inside = m.group(2)
        for a in split_args(inside):
            a = a.strip()
            if not a:
                continue
            mm = re.match(r'^(?:ref\s+|out\s+|in\s+|params\s+)?(?:[A-Za-z_][\w\.<>\[\],\? ]*?\s+)?([A-Za-z_]\w*)\s*(?:=(?!=)|$)', a)
            if mm:
                params.setdefault(mm.group(1), []).append((mname, m.start()))
    return params

def enclosing_method(masked, idx, params):
    """找 idx 所在的方法名（用「最近的 '{' 之前的签名」近似）。"""
    # 找包含 idx 的最内层块的开括号
    stack = []
    owner = None
    for i, c in enumerate(masked):
        if i >= idx:
            break
        if c == '{':
            stack.append(i)
        elif c == '}' and stack:
            stack.pop()
    if not stack:
        return None
    openb = stack[-1]
    head = masked[max(0, openb-400):openb]
    m = None
    for mm in re.finditer(r'([A-Za-z_]\w*)\s*\(([^;{}]*?)\)\s*$', head, re.S):
        m = mm
    if m and m.group(1) not in ('if', 'while', 'for', 'foreach', 'switch', 'catch', 'lock', 'using'):
        return m.group(1)
    return None

def addcomponent_nodes(masked, src):
    """AddComponent<ViewportClip> 的目标【节点表达式】。"""
    res = []
    for m in re.finditer(r'([A-Za-z_][\w\.]*(?:\.transform)?)\s*\.\s*gameObject\s*\.\s*AddComponent\s*<\s*ViewportClip\s*>', masked):
        res.append({'expr': m.group(1), 'line': line_of(src, m.start())})
    return res

def main():
    calls = json.load(io.open(os.path.join(OUT, 'r2_callsites.json'), encoding='utf-8'))
    vcnodes = json.load(io.open(os.path.join(OUT, 'r2_vc_nodes.json'), encoding='utf-8'))

    files = {}
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in ('.git', 'obj', 'bin', 'Library', 'Temp')]
        for fn in filenames:
            if not fn.endswith('.cs'):
                continue
            full = os.path.join(dirpath, fn)
            rel = './' + os.path.relpath(full, ROOT).replace('\\', '/')
            src = read(full)
            masked = strip_comments_keep_index(src)
            files[rel] = {'src': src, 'masked': masked, 'assigns': collect_assignments(masked),
                          'params': method_params(masked)}

    # 每文件的 VC 节点【变量名】集合（Hang 的 lhs + AddComponent 的目标 + Hang 无 lhs 时的定位）
    vc_names = {}
    vc_meta = {}
    for v in vcnodes:
        if v['file'].endswith('ViewportClip.cs'):
            continue
        if v['kind'] == 'Hang' and v['lhs']:
            vc_names.setdefault(v['file'], {})[v['lhs']] = ('Hang', v['line'], v.get('parent', ''), v.get('name', ''))
    for rel, f in files.items():
        for a in addcomponent_nodes(f['masked'], f['src']):
            nm = a['expr']
            if nm.endswith('.transform'):
                nm = nm[:-len('.transform')]
            nm = nm.split('.')[-1]
            vc_names.setdefault(rel, {})[nm] = ('AddComp', a['line'], '', '')

    results = []
    for c in calls:
        f = files[c['file']]
        masked, src = f['masked'], f['src']
        a = sum(len(x) + 1 for x in src.split('\n')[:c['line']-1])
        target_idx = masked.find('MenuDraw.' + c['api'] + '(', a)
        if target_idx < 0:
            target_idx = a
        vcset = vc_names.get(c['file'], {})
        ctx = {'target_idx': target_idx}
        chain, verdict, conf, note = [], None, None, ''
        # 递归解
        stack = [(c['arg0'].strip(), 0, '')]
        seen = set()
        hits = []
        while stack:
            e, depth, via = stack.pop(0)
            if depth > 14:
                chain.append('…深度超限: ' + e); verdict = verdict or '判不了'; conf = conf or '低'; note = note or '解链过深'; continue
            key = (e, depth)
            if key in seen:
                chain.append('↺' + e); continue
            seen.add(key)
            e = e.strip()
            # 去后缀
            m = re.match(r'^(.*?)\.transform$', e)
            if m:
                chain.append(e); stack.append((m.group(1), depth+1, via)); continue
            m = re.match(r'^(.*?)\.gameObject$', e)
            if m:
                chain.append(e); stack.append((m.group(1), depth+1, via)); continue
            if e in ('transform', 'this.transform'):
                chain.append('【窗根 transform】'); verdict = '不会'; conf = conf or '高'; note = note or '链停窗根'
                continue
            # 三元（顶层 ? 与配对的 :）
            t = top_ternary(e)
            if t:
                chain.append(re.sub(r'\s+', ' ', e)[:70] + '  ⇒ 三元两支')
                stack.append((t[0], depth+1, via))
                stack.append((t[1], depth+1, via))
                continue
            # new GameObject / new Xxx
            if re.match(r'^new\s', e):
                chain.append(e + '（无父 ⇒ 独立根）')
                verdict = verdict or '不会'; conf = conf or '中'; note = note or 'new 出来的独立节点'
                continue
            # 工厂调用
            m = re.match(r'^([\w\.]+)\s*\((.*)\)$', e, re.S)
            if m:
                fname = m.group(1).split('.')[-1]
                args = split_args(m.group(2))
                if fname in NOT_FACTORY:
                    chain.append(e + ' [非节点工厂]')
                    verdict = verdict or '判不了'; conf = conf or '低'; note = note or ('读到非工厂值: ' + fname)
                    continue
                if args:
                    chain.append(fname + '(...)  ⇒ 父=第1实参')
                    stack.append((args[0], depth+1, via))
                    continue
                chain.append(e); verdict = verdict or '判不了'; conf = conf or '低'; note = note or '空实参'
                continue
            # 标识符
            if re.match(r'^[A-Za-z_]\w*$', e):
                if e in vcset:
                    kind, ln, par, nm = vcset[e]
                    hits.append((e, kind, ln))
                    chain.append('★★ ' + e + f' = ViewportClip（{kind} @第{ln}行' + (f'，name={nm}' if nm else '') + '）')
                    verdict = '会'; conf = '高'; note = note or ('父链命中 VC 节点变量 ' + e)
                    continue
                cands = [x for x in f['assigns'] if x[0] == e and x[2] < target_idx]
                if cands:
                    x = sorted(cands, key=lambda y: -y[2])[0]
                    chain.append(e + ' := ' + re.sub(r'\s+', ' ', x[1])[:80])
                    stack.append((x[1], depth+1, e))
                    continue
                cands_after = [x for x in f['assigns'] if x[0] == e]
                if cands_after:
                    x = sorted(cands_after, key=lambda y: y[2])[0]
                    chain.append(e + ' （赋值在调用点【之后】） := ' + re.sub(r'\s+', ' ', x[1])[:60])
                    stack.append((x[1], depth+1, e))
                    continue
                if e in f['params']:
                    mm = f['params'][e]
                    mnames = sorted(set(y[0] for y in mm))
                    chain.append(e + '（方法形参；所在方法 ≈ ' + ','.join(mnames[:3]) + '）')
                    verdict = 'WRAPPER'; conf = '中'; note = '链停包装器形参 ⇒ 要找调用方方法 ' + ','.join(mnames[:3])
                    continue
                chain.append(e + '（无赋值、非形参）')
                verdict = verdict or '判不了'; conf = conf or '低'; note = note or '解不出（字段/其它对象成员/跨方法）'
                continue
            # 成员访问 x.y —— 常见 f._root / this._root
            m = re.match(r'^([A-Za-z_]\w*)\.([A-Za-z_]\w*)$', e)
            if m:
                chain.append(e + '（成员访问）')
                if m.group(1) in ('this',):
                    stack.append((m.group(2), depth+1, via)); continue
                verdict = verdict or '判不了'; conf = conf or '低'; note = note or ('跨对象成员 ' + e)
                continue
            chain.append(e); verdict = verdict or '判不了'; conf = conf or '低'; note = note or '形态未覆盖'
        results.append({
            'file': c['file'], 'line': c['line'], 'api': c['api'], 'arg0': c['arg0'],
            'chain': chain, 'verdict': verdict or '判不了', 'conf': conf or '低', 'note': note,
            'vc_hits': hits, 'snippet': c['snippet'],
            'method': enclosing_method(masked, target_idx, f['params']),
        })

    io.open(os.path.join(OUT, 'r2_resolve2.json'), 'w', encoding='utf-8', newline='\n').write(
        json.dumps(results, ensure_ascii=False, indent=1))
    from collections import Counter
    print('verdict:', dict(Counter(r['verdict'] for r in results)))
    print('VC 命中:', sum(1 for r in results if r['vc_hits']))
    for r in results:
        if r['vc_hits']:
            print('  HIT', r['file'], r['line'], r['arg0'], '->', r['vc_hits'])

main()
