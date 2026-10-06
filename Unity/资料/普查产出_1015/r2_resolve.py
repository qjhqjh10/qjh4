# -*- coding: utf-8 -*-
"""R2 · 第三步：逐调用点解父链。
规则（同 ViewportClip.FindAbove 的语义）：一个表达式表示的运行期节点，其祖先链 = 建它的那个工厂的【第 1 实参】往上。
  · 工厂 = 返回【新节点】的静态方法（MenuDraw.Node/Rect/Nine/Tiled/Hit/DeckCell/Absorb/ShadeHit、
    MenuWindowBase.New/Node/NewPlainTransform、各文件的 Node(...) 包装 …）⇒ 新节点挂在第 1 实参之下。
  · `X.transform` / `X.gameObject.transform` ⇒ 同 X 这个节点。
  · `transform` ⇒ 本组件所在的那个 GameObject。
  · 方法形参 ⇒ 乙类：要找调用方（本脚本先标记为 WRAPPER）。
  · 字段（`_xxx`）⇒ 找字段赋值点（同文件）。
"""
import io, os, re, json, sys
sys.path.insert(0, r"d:/4/Unity/资料/普查产出_1015")
from r2_extract import read, strip_comments_keep_index, split_args, line_of, line_text

ROOT = r"d:/4/Unity/MyGame/Assets/CardPresentation"
OUT = r"d:/4/Unity/资料/普查产出_1015"

# 返回【新节点】的工厂名（末段方法名）。第一个实参 = 新节点的父。
NODE_FACTORIES = {
    'Node', 'Rect', 'Nine', 'Tiled', 'Hit', 'DeckCell', 'Absorb', 'ShadeHit',
    'New', 'NewPlainTransform', 'Panel', 'Group', 'Box', 'Wrap', 'Row', 'Col',
    'Button', 'Label', 'Quad', 'Image', 'Band', 'Cell',
}
# 明确【不建新节点】的（读值/查询），不要往下追
NOT_FACTORY = {'Text', 'TextBox', 'ClipText', 'SizeOf', 'SpanOf', 'Local', 'Px', 'Abs', 'Visible',
               'Find', 'GetComponent', 'Resolve', 'Hang', 'PaddedClip', 'IsNoClip'}

def find_blocks(masked):
    """返回 [(open_idx, close_idx)] 全部花括号对。"""
    stack = []
    pairs = []
    for i, c in enumerate(masked):
        if c == '{':
            stack.append(i)
        elif c == '}':
            if stack:
                o = stack.pop()
                pairs.append((o, i))
    return pairs

def innermost_chain(blocks, idx):
    """返回包含 idx 的所有块，按【由内到外】排序。"""
    out = [b for b in blocks if b[0] < idx < b[1]]
    out.sort(key=lambda b: b[1] - b[0])
    return out

def collect_assignments(masked, blocks):
    """收集所有 `NAME = EXPR ;`（含 var / 类型前缀）。返回 [(name, rhs, start, end, depth_block_idx)]"""
    res = []
    for m in re.finditer(r'(?:^|[;{}\n])\s*(?:var\s+|(?:[A-Za-z_][\w\.<>\[\],\? ]*?)\s+)?([A-Za-z_]\w*)\s*=\s*', masked):
        name = m.group(1)
        if name in ('if', 'while', 'for', 'return', 'foreach', 'case', 'using', 'new', 'else', 'do', 'switch'):
            continue
        rhs_start = m.end()
        # 找顶层分号
        d = 0
        j = rhs_start
        n = len(masked)
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
    """粗略抓方法签名里的形参名（供 WRAPPER 判定）。"""
    params = set()
    for m in re.finditer(r'\)\s*$', masked, re.M):
        pass
    for m in re.finditer(r'\b(?:public|private|protected|internal|static|void|Transform|GameObject|Label|string|float|int|bool|PxRect|Vector2|Vector4|Action|Color)\b[^;{}()]*?\(([^;{}]*)\)\s*(?:\{|=>|$)', masked):
        inside = m.group(1)
        for a in split_args(inside):
            a = a.strip()
            if not a:
                continue
            mm = re.match(r'^(?:ref\s+|out\s+|in\s+|params\s+)?(?:[A-Za-z_][\w\.<>\[\],\? ]*?\s+)?([A-Za-z_]\w*)\s*(?:=|$)', a)
            if mm:
                params.add(mm.group(1))
    return params

def main():
    calls = json.load(io.open(os.path.join(OUT, 'r2_callsites.json'), encoding='utf-8'))
    vcnodes = json.load(io.open(os.path.join(OUT, 'r2_vc_nodes.json'), encoding='utf-8'))

    # 每个文件的：VC 节点变量名 -> (建点行, 它的父表达式)
    byfile_vc = {}
    for v in vcnodes:
        byfile_vc.setdefault(v['file'], []).append(v)

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
            files[rel] = {
                'src': src, 'masked': masked,
                'blocks': find_blocks(masked),
                'assigns': collect_assignments(masked, None),
                'params': method_params(masked),
            }

    results = []
    for c in calls:
        f = files[c['file']]
        src, masked = f['src'], f['masked']
        # 调用点的字符偏移
        pos = 0
        ln = 1
        target_idx = None
        for i, ch in enumerate(masked):
            if ln == c['line']:
                target_idx = i + masked[i:].find('MenuDraw.' + c['api'] + '(')
                break
            if ch == '\n':
                ln += 1
        if target_idx is None or target_idx < 0:
            # 退路：从行首找
            a = sum(len(x) + 1 for x in src.split('\n')[:c['line']-1])
            target_idx = masked.find('MenuDraw.' + c['api'] + '(', a)

        chain = []
        verdict, conf, note = None, None, ''
        expr = c['arg0']
        cur_expr = expr
        seen = set()
        depth = 0
        while depth < 12:
            depth += 1
            e = cur_expr.strip()
            if e in seen:
                chain.append('↺' + e); verdict = '判不了'; conf = '低'; note = '自引用'; break
            seen.add(e)
            # 去掉 .transform / .gameObject.transform
            m = re.match(r'^(.*?)\.transform$', e)
            if m:
                chain.append(e); cur_expr = m.group(1); continue
            m = re.match(r'^(.*?)\.gameObject$', e)
            if m:
                chain.append(e); cur_expr = m.group(1); continue
            if e == 'transform' or e == 'this.transform':
                chain.append('transform(=本窗根)'); verdict = '不会'; conf = '高'; note = '链停在窗根'; break
            # 工厂调用：Foo.Bar( parentArg , ...)
            m = re.match(r'^([\w\.]+)\s*\((.*)\)$', e, re.S)
            if m:
                fname = m.group(1).split('.')[-1]
                args = split_args(m.group(2))
                if fname in NOT_FACTORY:
                    chain.append(e); verdict = '判不了'; conf = '低'; note = '非工厂调用(%s)' % fname; break
                if args and (fname in NODE_FACTORIES or fname[:1].isupper()):
                    chain.append(e + '  ⌐父=第1实参')
                    cur_expr = args[0]
                    continue
                chain.append(e); verdict = '判不了'; conf = '低'; note = '未知调用'; break
            # 简单标识符
            if re.match(r'^[A-Za-z_]\w*$', e):
                # 1) 同文件 VC 节点变量名（需在调用点之前建）
                vcs = [v for v in byfile_vc.get(c['file'], []) if v['lhs'] == e and v['line'] <= c['line']]
                if vcs:
                    v = sorted(vcs, key=lambda x: -x['line'])[0]
                    chain.append(e + ' = ★ViewportClip(第%d行建)' % v['line'])
                    verdict = '会'; conf = '高' if v['line'] <= c['line'] and not _crossscope(f, v, c) else '中'
                    note = '父链命中 VC（%s）' % v['kind']
                    break
                # 2) 最近一次赋值
                cands = [a for a in f['assigns'] if a[0] == e and a[2] < target_idx]
                if cands:
                    a = sorted(cands, key=lambda x: -x[2])[0]
                    chain.append(e + ' = ' + a[1])
                    cur_expr = a[1]
                    continue
                if e in f['params']:
                    chain.append(e + '（方法形参）')
                    verdict = 'WRAPPER'; conf = '中'; note = '链停包装器形参 ⇒ 要找调用方'
                    break
                chain.append(e + '（无赋值/非形参）')
                verdict = '判不了'; conf = '低'; note = '解不出'
                break
            chain.append(e)
            verdict = '判不了'; conf = '低'; note = '表达式形态未覆盖'
            break
        results.append({
            'file': c['file'], 'line': c['line'], 'api': c['api'],
            'arg0': c['arg0'], 'chain': chain, 'verdict': verdict, 'conf': conf, 'note': note,
            'snippet': c['snippet'],
        })

    io.open(os.path.join(OUT, 'r2_resolve.json'), 'w', encoding='utf-8', newline='\n').write(
        json.dumps(results, ensure_ascii=False, indent=1))
    from collections import Counter
    print(dict(Counter(r['verdict'] for r in results)))

def _crossscope(f, v, c):
    return False

main()
