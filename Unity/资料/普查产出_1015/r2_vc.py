# -*- coding: utf-8 -*-
"""R2 · 第二步：抽取真实的（非注释）ViewportClip 建点，并记下【节点变量名】与【父】。"""
import io, os, re, json

ROOT = r"d:/4/Unity/MyGame/Assets/CardPresentation"
import sys
sys.path.insert(0, r"d:/4/Unity/资料/普查产出_1015")
from r2_extract import read, strip_comments_keep_index, split_args, line_of, line_text

rows = []
for dirpath, dirnames, filenames in os.walk(ROOT):
    dirnames[:] = [d for d in dirnames if d not in ('.git', 'obj', 'bin', 'Library', 'Temp')]
    for fn in filenames:
        if not fn.endswith('.cs'):
            continue
        full = os.path.join(dirpath, fn)
        rel = './' + os.path.relpath(full, ROOT).replace('\\', '/')
        src = read(full)
        masked = strip_comments_keep_index(src)
        for m in re.finditer(r'(?:([A-Za-z_][\w\.\[\]]*)\s*=\s*)?(ViewportClip\.Hang|AddComponent\s*<\s*ViewportClip\s*>)\s*\(?', masked):
            lin = line_of(src, m.start())
            stmt = line_text(src, m.start())
            # 取赋值目标
            lhs = m.group(1)
            # 取完整语句（到分号）
            a = masked.rfind(';', 0, m.start()) + 1
            b = masked.find(';', m.start())
            if b < 0:
                b = len(masked)
            raw_stmt = re.sub(r'\s+', ' ', src[a:b]).strip()
            kind = 'Hang' if 'ViewportClip.Hang' in m.group(2) else 'AddComponent'
            args = []
            if kind == 'Hang':
                o = masked.find('(', m.start())
                d = 0
                j = o
                while j < len(masked):
                    if masked[j] == '(':
                        d += 1
                    elif masked[j] == ')':
                        d -= 1
                        if d == 0:
                            break
                    j += 1
                args = split_args(masked[o+1:j])
            rows.append({
                'file': rel, 'line': lin, 'kind': kind, 'lhs': lhs,
                'parent': args[0] if args else '', 'name': args[1] if len(args) > 1 else '',
                'rect': args[2] if len(args) > 2 else '', 'pad': args[3] if len(args) > 3 else '',
                'soft': args[4] if len(args) > 4 else '',
                'stmt': raw_stmt, 'snippet': stmt,
            })
rows.sort(key=lambda r: (r['file'], r['line']))
io.open(r"d:/4/Unity/资料/普查产出_1015/r2_vc_nodes.json", 'w', encoding='utf-8', newline='\n').write(
    json.dumps(rows, ensure_ascii=False, indent=1))
print("VC code build points:", len(rows))
from collections import Counter
print("by kind:", dict(Counter(r['kind'] for r in rows)))
print("by file:")
for f, n in Counter(r['file'] for r in rows).most_common():
    print("  ", n, f)
print()
for r in rows:
    print("%s:%d | %s | %s | parent=%s | name=%s" % (r['file'], r['line'], r['kind'], r['lhs'], r['parent'], r['name']))
