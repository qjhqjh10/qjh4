# -*- coding: utf-8 -*-
"""R2 · 第四步：通用「找调用方」——给 (方法名, 第几实参)，列出全仓调用点与该位置的实参原文。"""
import io, os, re, sys
sys.path.insert(0, r"d:/4/Unity/资料/普查产出_1015")
from r2_extract import read, strip_comments_keep_index, split_args, line_of, line_text

ROOT = r"d:/4/Unity/MyGame/Assets/CardPresentation"
CACHE = {}

def load():
    for dirpath, dirnames, filenames in os.walk(ROOT):
        dirnames[:] = [d for d in dirnames if d not in ('.git', 'obj', 'bin', 'Library', 'Temp')]
        for fn in filenames:
            if not fn.endswith('.cs'):
                continue
            full = os.path.join(dirpath, fn)
            rel = './' + os.path.relpath(full, ROOT).replace('\\', '/')
            src = read(full)
            CACHE[rel] = (src, strip_comments_keep_index(src))

def find_calls(method, argidx=0, skip_def_files=()):
    load()
    out = []
    pat = re.compile(r'(?<![\w])' + re.escape(method) + r'\s*\(')
    for rel, (src, masked) in CACHE.items():
        if rel in skip_def_files:
            continue
        for m in pat.finditer(masked):
            # 跳过定义行（前一字符上下文像 `static ... Method(` 且后面紧跟 `{` 或 `=>`）
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
            args = split_args(masked[o+1:j])
            tail = masked[j+1:j+8]
            isdef = tail.strip().startswith('{') or tail.strip().startswith('=>') or tail.strip().startswith('where')
            # 定义的特征：前面有类型修饰符 + 方法名后紧跟 `{`/`=>`
            head = masked[max(0, m.start()-90):m.start()]
            if isdef and re.search(r'(public|private|protected|internal|static|virtual|override|abstract|sealed)\s', head):
                continue
            out.append({'file': rel, 'line': line_of(src, m.start()),
                        'arg': args[argidx] if len(args) > argidx else '',
                        'nargs': len(args), 'snippet': line_text(src, m.start()),
                        'args': [a[:60] for a in args]})
    return out

if __name__ == '__main__':
    import json
    what = sys.argv[1]
    ai = int(sys.argv[2]) if len(sys.argv) > 2 else 0
    skips = tuple(sys.argv[3:])
    for c in find_calls(what, ai, skips):
        print('%s:%d | arg%d=%s | %d args | %s' % (c['file'], c['line'], ai, c['arg'], c['nargs'], c['snippet'][:110]))
