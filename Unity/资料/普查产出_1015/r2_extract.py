# -*- coding: utf-8 -*-
"""R2 · A799 全量表 · 第一步：机械抽取所有 MenuDraw.Text/TextBox 调用点 + 第 1 实参。
只读脚本；不写工程源码树。"""
import io, os, re, sys, json

ROOT = r"d:/4/Unity/MyGame/Assets/CardPresentation"

def read(p):
    return io.open(p, 'r', encoding='utf-8-sig', newline='').read()

def strip_comments_keep_index(src):
    """返回一份与 src 等长的字符串：注释/字符串字面量/char 字面量里的字符全部换成空格。
    => 用同一个下标既能定位原文、又不会被注释里的调用骗到。"""
    out = list(src)
    i = 0
    n = len(src)
    while i < n:
        c = src[i]
        # 行注释
        if c == '/' and i + 1 < n and src[i+1] == '/':
            j = i
            while j < n and src[j] != '\n':
                out[j] = ' '
                j += 1
            i = j
            continue
        # 块注释
        if c == '/' and i + 1 < n and src[i+1] == '*':
            j = i
            while j + 1 < n and not (src[j] == '*' and src[j+1] == '/'):
                if src[j] != '\n':
                    out[j] = ' '
                j += 1
            for k in range(i, min(j+2, n)):
                if src[k] != '\n':
                    out[k] = ' '
            i = j + 2
            continue
        # 逐字字符串 @"..."  (可能含 "")
        if c == '@' and i + 1 < n and src[i+1] == '"':
            j = i + 2
            while j < n:
                if src[j] == '"':
                    if j + 1 < n and src[j+1] == '"':
                        j += 2
                        continue
                    j += 1
                    break
                j += 1
            for k in range(i, min(j, n)):
                if src[k] != '\n':
                    out[k] = ' '
            i = j
            continue
        # 普通字符串
        if c == '"':
            j = i + 1
            while j < n:
                if src[j] == '\\':
                    j += 2
                    continue
                if src[j] == '"':
                    j += 1
                    break
                if src[j] == '\n':
                    break
                j += 1
            for k in range(i, min(j, n)):
                if src[k] != '\n':
                    out[k] = ' '
            i = j
            continue
        # char 字面量
        if c == "'":
            j = i + 1
            while j < n:
                if src[j] == '\\':
                    j += 2
                    continue
                if src[j] == "'":
                    j += 1
                    break
                if src[j] == '\n':
                    break
                j += 1
            for k in range(i, min(j, n)):
                if src[k] != '\n':
                    out[k] = ' '
            i = j
            continue
        i += 1
    return ''.join(out)

def split_args(s):
    """按顶层逗号切分实参（考虑 () [] {} <> 三种括号 + 泛型尖括号的粗处理）。"""
    args = []
    depth = 0
    cur = []
    angle = 0
    i = 0
    n = len(s)
    while i < n:
        c = s[i]
        if c in '([{':
            depth += 1
        elif c in ')]}':
            depth -= 1
        elif c == ',' and depth == 0 and angle == 0:
            args.append(''.join(cur).strip())
            cur = []
            i += 1
            continue
        elif c == '<':
            # 只在像泛型时算尖括号（前面是标识符字符、后面是标识符字符）
            if cur and (cur[-1].isalnum() or cur[-1] in '_>.') and i+1 < n and (s[i+1].isalnum() or s[i+1] in '_ '):
                angle += 1
        elif c == '>':
            if angle > 0:
                angle -= 1
        cur.append(c)
        i += 1
    if cur:
        args.append(''.join(cur).strip())
    return args

def line_of(src, idx):
    return src.count('\n', 0, idx) + 1

def line_text(src, idx):
    a = src.rfind('\n', 0, idx) + 1
    b = src.find('\n', idx)
    if b < 0:
        b = len(src)
    return src[a:b].strip()

def main():
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
            for m in re.finditer(r'MenuDraw\.(Text|TextBox)\s*\(', masked):
                api = m.group(1)
                open_idx = m.end() - 1
                # 括号配平（在 masked 上做，位置与 src 一致）
                depth = 0
                j = open_idx
                n = len(masked)
                while j < n:
                    if masked[j] == '(':
                        depth += 1
                    elif masked[j] == ')':
                        depth -= 1
                        if depth == 0:
                            break
                    j += 1
                inner = masked[open_idx+1:j]
                args = split_args(inner)
                rows.append({
                    'file': rel,
                    'line': line_of(src, m.start()),
                    'api': api,
                    'nargs': len(args),
                    'arg0': args[0] if args else '',
                    'arg1': args[1] if len(args) > 1 else '',
                    'argN_last': args[-1] if args else '',
                    'snippet': line_text(src, m.start()),
                })
    rows.sort(key=lambda r: (r['file'], r['line']))
    out = r"d:/4/Unity/资料/普查产出_1015/r2_callsites.json"
    io.open(out, 'w', encoding='utf-8', newline='\n').write(json.dumps(rows, ensure_ascii=False, indent=1))
    print("total code call sites:", len(rows))
    from collections import Counter
    c = Counter(r['api'] for r in rows)
    print("by api:", dict(c))
    print("by file:", len(set(r['file'] for r in rows)))

main()
