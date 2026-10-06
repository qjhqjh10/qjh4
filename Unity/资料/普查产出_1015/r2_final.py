# -*- coding: utf-8 -*-
"""R2 · 第五步（v2）：一条递归里做完【VC 检查 + 赋值解链 + 工厂解链 + 三元 + 包装器跳调用方】。
输出 r2_final.json
"""
import io, os, re, json, sys
sys.path.insert(0, r"d:/4/Unity/资料/普查产出_1015")
from r2_extract import read, strip_comments_keep_index, split_args, line_of, line_text
from r2_resolve2 import collect_assignments, top_ternary

ROOT = r"d:/4/Unity/MyGame/Assets/CardPresentation"
OUT = r"d:/4/Unity/资料/普查产出_1015"
FILES = {}
DEFS = {}
CALLPAT = {}

NON_NODE = {'ClipText', 'SizeOf', 'SpanOf', 'Local', 'Px', 'Abs', 'Visible', 'Find', 'GetComponent',
            'Resolve', 'Hang', 'PaddedClip', 'IsNoClip', 'Sub', 'Off', 'Add', 'Mul', 'Clamp', 'Min',
            'Max', 'Child', 'RectAbove', 'ClipRectAbove', 'Aligned', 'Art', 'Tex', 'Row', 'ColOf'}

def load_all():
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in ('.git', 'obj', 'bin', 'Library', 'Temp')]
        for fn in filenames:
            if not fn.endswith('.cs'):
                continue
            full = os.path.join(dirpath, fn)
            rel = './' + os.path.relpath(full, ROOT).replace('\\', '/')
            src = read(full)
            masked = strip_comments_keep_index(src)
            FILES[rel] = {'src': src, 'masked': masked, 'assigns': collect_assignments(masked)}

MODS = r'(?:public|private|protected|internal|static|virtual|override|abstract|sealed|async|unsafe|extern|new|partial)'

def build_defs():
    for rel, f in FILES.items():
        masked = f['masked']
        src = f['src']
        # 类名（供限定调用名用）：按行找 class X
        for m in re.finditer(r'^\s*(?:(?:public|private|protected|internal|static|abstract|sealed|partial)\s+)*class\s+([A-Za-z_]\w*)',
                             masked, re.M):
            pass
        for m in re.finditer(r'([A-Za-z_]\w*)\s*\(([^;{}]*?)\)\s*(\{|=>|where\b|$)', masked, re.M):
            nm = m.group(1)
            if nm in ('if', 'while', 'for', 'foreach', 'switch', 'catch', 'lock', 'using', 'return',
                      'new', 'get', 'set', 'nameof', 'typeof', 'case'):
                continue
            # 取本行行首到 m.start() 的片段
            ls = masked.rfind('\n', 0, m.start()) + 1
            pre = masked[ls:m.start()]
            # 定义形状：`[修饰符]* [类型] Name` 或 `[修饰符]* Name`（构造器）
            if not re.match(r'^\s*(?:' + MODS + r'\s+)*(?:[A-Za-z_][\w\.<>\[\]\?]*\s+)?$', pre):
                continue
            if '=' in pre or '.' in pre:
                continue
            ps, req = [], 0
            for a in split_args(m.group(2)):
                a = a.strip()
                if not a:
                    continue
                mm = re.match(r'^(?:ref\s+|out\s+|in\s+|params\s+)?(?:[A-Za-z_][\w\.<>\[\],\? ]*?\s+)?([A-Za-z_]\w*)\s*(?:=(?!=)|$)', a)
                ps.append(mm.group(1) if mm else '')
                if '=' not in a:
                    req += 1
            if ps:
                DEFS.setdefault(nm, []).append({'file': rel, 'line': line_of(src, m.start()),
                                                'params': ps, 'req': req, 'n': len(ps)})
        # 类名表
        for m in re.finditer(r'\bclass\s+([A-Za-z_]\w*)', masked):
            CLS.setdefault(rel, set()).add(m.group(1))

CLS = {}

def find_calls(name):
    if name in CALLPAT:
        return CALLPAT[name]
    out = []
    pat = re.compile(r'(?<![\w])' + re.escape(name) + r'\s*\(')
    for rel, f in FILES.items():
        src, masked = f['src'], f['masked']
        for m in pat.finditer(masked):
            o = m.end() - 1
            d, j, n = 0, o, len(masked)
            while j < n:
                if masked[j] == '(':
                    d += 1
                elif masked[j] == ')':
                    d -= 1
                    if d == 0:
                        break
                j += 1
            tail = masked[j+1:j+14].strip()
            # 跳过定义：① 行首到方法名之间只有 修饰符 + 类型（或构造器名）；② 右括号后紧跟方法体
            ls = masked.rfind('\n', 0, m.start()) + 1
            pre = masked[ls:m.start()]
            is_def = (tail.startswith('{') or tail.startswith('=>') or tail.startswith('where')) and \
                     bool(re.match(r'^\s*(?:' + MODS + r'\s+)*(?:[A-Za-z_][\w\.<>\[\]\?]*\s+)?$', pre)) and \
                     not ('=' in pre or '.' in pre)
            if is_def:
                continue
            out.append({'file': rel, 'line': line_of(src, m.start()), 'args': split_args(masked[o+1:j])})
    CALLPAT[name] = out
    return out

VC = {}

def build_vc():
    vcnodes = json.load(io.open(os.path.join(OUT, 'r2_vc_nodes.json'), encoding='utf-8'))
    for v in vcnodes:
        if v['file'].endswith('ViewportClip.cs'):
            continue
        if v['kind'] == 'Hang' and v['lhs']:
            VC.setdefault(v['file'], {})[v['lhs']] = 'Hang@%d' % v['line']
    for rel, f in FILES.items():
        for m in re.finditer(r'([A-Za-z_][\w\.]*(?:\.transform)?)\s*\.\s*gameObject\s*\.\s*AddComponent\s*<\s*ViewportClip\s*>', f['masked']):
            nm = m.group(1)
            if nm.endswith('.transform'):
                nm = nm[:-10]
            VC.setdefault(rel, {})[nm.split('.')[-1]] = 'AddComponent@%d' % line_of(f['src'], m.start())

BUDGET = [200000]

_ENC = {}

def enclosing_def(rel, tidx):
    """调用点所在的【最内层方法】= (方法名, [形参名], 定义行)。找不到返回 None。"""
    key = (rel, )
    f = FILES[rel]
    masked = f['masked']
    stack = []
    for i, c in enumerate(masked):
        if i >= tidx:
            break
        if c == '{':
            stack.append(i)
        elif c == '}':
            if stack:
                stack.pop()
    while stack:
        openb = stack.pop()
        # openb 之前应该是 `)`（方法体）或 `=>` 之后…
        head = masked[max(0, openb-3000):openb]
        m = None
        # 取最后一个 `Name(params)` 形态，且其后只有空白
        for mm in re.finditer(r'([A-Za-z_]\w*)\s*\(([^;{}]*?)\)\s*$', head, re.S):
            m = mm
        if m and m.group(1) not in ('if', 'while', 'for', 'foreach', 'switch', 'catch', 'lock', 'using',
                                    'checked', 'unchecked', 'fixed', 'unsafe'):
            nm = m.group(1)
            ps = []
            for a in split_args(m.group(2)):
                a = a.strip()
                if not a:
                    continue
                mm2 = re.match(r'^(?:ref\s+|out\s+|in\s+|params\s+)?(?:[A-Za-z_][\w\.<>\[\],\? ]*?\s+)?([A-Za-z_]\w*)\s*(?:=(?!=)|$)', a)
                ps.append(mm2.group(1) if mm2 else '')
            return (nm, ps)
    return None

def needle(expr, file, line, depth, path, seen, enc=None):
    """返回 dict(verdict, conf, note, hits)"""
    res = {'verdict': None, 'conf': None, 'note': '', 'hits': []}
    BUDGET[0] -= 1
    if BUDGET[0] < 0:
        res['verdict'] = '判不了'; res['note'] = '预算耗尽'; return res
    if depth > 22:
        res['verdict'] = '判不了'; res['note'] = '解链/跳转层数超限'; return res
    e = expr.strip()
    if not e:
        res['verdict'] = '判不了'; res['note'] = '空表达式'; return res
    key = (file, line, e)
    if key in seen:
        res['verdict'] = '判不了'; res['note'] = '环（同一表达式已解过）'; path.append('↺' + e[:50]); return res
    seen = seen | {key}
    if enc is None:
        ck = (file, line)
        if ck not in _ENC:
            f2 = FILES[file]
            a2 = sum(len(x) + 1 for x in f2['src'].split('\n')[:line-1])
            _ENC[ck] = enclosing_def(file, a2)
        enc = _ENC[ck]
    # ① VC 命中（含 .transform 后缀）
    base = e[:-10] if e.endswith('.transform') else e
    if base in VC.get(file, {}):
        res['hits'].append((file, base, VC[file][base]))
        res['verdict'] = '会'; res['conf'] = '高'
        res['note'] = '父链命中 VC 节点变量 %s（%s）' % (base, VC[file][base])
        path.append('★★ %s = ViewportClip(%s)' % (base, VC[file][base]))
        return res
    if e.endswith('.transform'):
        path.append(e)
        r = needle(base, file, line, depth+1, path, seen)
        return r
    if e.endswith('.gameObject'):
        path.append(e)
        return needle(e[:-11], file, line, depth+1, path, seen)
    if e in ('transform', 'this.transform'):
        res['verdict'] = '不会'; res['conf'] = '高'; res['note'] = '链停窗根（本组件 GameObject）'
        path.append('【窗根 transform】'); return res
    t = top_ternary(e)
    if t:
        path.append(re.sub(r'\s+', ' ', e)[:60] + ' ⇒ 三元')
        ra = needle(t[0], file, line, depth+1, path, seen)
        rb = needle(t[1], file, line, depth+1, path, seen)
        order = {'会': 0, 'WRAPPER': 1, '判不了': 2, '不会': 3}
        return ra if order[ra['verdict']] <= order[rb['verdict']] else rb
    if re.match(r'^new\s', e):
        res['verdict'] = '不会'; res['conf'] = '中'
        res['note'] = 'new 出来的节点（无父 ⇒ 独立根；⚠️ 若后续 SetParent 到某处需另判）'
        path.append(re.sub(r'\s+', ' ', e)[:60] + '（无父）'); return res
    m = re.match(r'^([\w\.]+)\s*\((.*)\)$', e, re.S)
    if m:
        fn = m.group(1).split('.')[-1]
        args = split_args(m.group(2))
        if fn in NON_NODE or fn in ('Text', 'TextBox', 'NodeValue'):
            res['verdict'] = '判不了'; res['note'] = '读到非节点工厂值: ' + fn
            path.append(e[:70]); return res
        if args:
            path.append('%s(...) ⇒ 父=第1实参' % fn)
            return needle(args[0], file, line, depth+1, path, seen)
        res['verdict'] = '判不了'; res['note'] = '空实参'; return res
    if re.match(r'^[A-Za-z_]\w*$', e):
        f = FILES[file]
        a = sum(len(x) + 1 for x in f['src'].split('\n')[:line-1])
        cands = [x for x in f['assigns'] if x[0] == e]
        before = [x for x in cands if x[2] < a]
        pick = sorted(before, key=lambda y: -y[2])[0] if before else \
               (sorted(cands, key=lambda y: y[2])[0] if cands else None)
        if pick:
            path.append('%s := %s' % (e, re.sub(r'\s+', ' ', pick[1])[:70]))
            return needle(pick[1], file, line, depth+1, path, seen)
        # 包装器形参？
        owners = [d for d in DEFS.get_owner(e, file)] if False else []
        # 找本文件里含该形参的定义
        # 包装器形参？🔴 **只用【本调用点所在那个方法】的形参**（同名跨作用域复用是已知盲区）
        cand_defs = []
        if enc and e in enc[1]:
            cand_defs.append((enc[0], enc[1].index(e), {'file': file, 'params': enc[1],
                                                        'req': 0, 'n': len(enc[1])}))
        if cand_defs:
            # 同文件可能多个同名形参；逐个跳。
            # 🔴 关键：**按实参个数过滤**（只取 n 在 [req, n] 区间、且该位置实参非空者）
            #    否则 `Text` / `Hit` 这种短名会满仓乱跳（实测过：跳到 BattleDriver.Hit）。
            any_hit = False
            budget_calls = 0
            for nm, idx, dre in cand_defs:
                cls = CLS.get(file, set())
                calls = []
                for cn in cls:
                    for c in find_calls(cn + '.' + nm):
                        calls.append(c)
                for c in find_calls(nm):
                    if c['file'] == file:
                        calls.append(c)
                if not calls:
                    for c in find_calls(nm):
                        if len(c['args']) > idx and c['args'][idx].strip():
                            calls.append(c)
                for c in calls:
                    if len(c['args']) <= idx:
                        continue
                    if not c['args'][idx].strip():
                        continue
                    budget_calls += 1
                    if budget_calls > 8:
                        break
                    any_hit = True
                    path.append('↪ 调用方 %s:%d 的 %s(arg%d=%s)' % (c['file'], c['line'], nm, idx, c['args'][idx][:44]))
                    rr = needle(c['args'][idx], c['file'], c['line'], depth+1, path, seen)
                    if rr['verdict'] == '会':
                        return rr
                    res.setdefault('_alt', []).append(rr)
                if budget_calls > 8:
                    break
            if any_hit:
                alts = res.get('_alt', [])
                order = {'会': 0, 'WRAPPER': 1, '判不了': 2, '不会': 3}
                if alts:
                    return sorted(alts, key=lambda z: order[z['verdict']])[0]
                return res
            res['verdict'] = 'WRAPPER'; res['conf'] = '中'
            res['note'] = '链停包装器形参（找不到调用点）'
            path.append(e + '（方法形参）'); return res
        res['verdict'] = '判不了'; res['conf'] = '低'
        res['note'] = '标识符无解（字段/跨对象成员/跨方法，静态判不了）'
        path.append(e + '（无解）'); return res
    res['verdict'] = '判不了'; res['note'] = '形态未覆盖'; path.append(e[:70]); return res

def main():
    load_all(); build_defs(); build_vc()
    calls = json.load(io.open(os.path.join(OUT, 'r2_callsites.json'), encoding='utf-8'))
    results = []
    for c in calls:
        path = []
        try:
            r = needle(c['arg0'], c['file'], c['line'], 0, path, set())
        except Exception as ex:
            r = {'verdict': '判不了', 'conf': '低', 'note': '脚本异常 %r' % ex, 'hits': []}
        results.append({'file': c['file'], 'line': c['line'], 'api': c['api'], 'arg0': c['arg0'],
                        'verdict': r['verdict'] or '判不了', 'conf': r['conf'] or '低',
                        'note': r['note'], 'path': path, 'hits': r['hits'], 'snippet': c['snippet']})
    io.open(os.path.join(OUT, 'r2_final.json'), 'w', encoding='utf-8', newline='\n').write(
        json.dumps(results, ensure_ascii=False, indent=1))
    from collections import Counter
    print('verdict:', dict(Counter(r['verdict'] for r in results)))
    print('VC 命中:', sum(1 for r in results if r['hits']))
    for r in results:
        if r['hits']:
            print('  HIT %s:%d | %s' % (r['file'], r['line'], r['hits']))

if __name__ == '__main__':
    main()
