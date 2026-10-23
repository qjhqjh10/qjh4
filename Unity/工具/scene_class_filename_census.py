# -*- coding: utf-8 -*-
"""普查：哪些 MonoBehaviour 类【没有同名 .cs 文件】—— 那类类 Unity 建不出 MonoScript，
存场景时会被烘成内联 `!u!115` 存根（A1506 / A1481 的根因）。"""
import io, os, re, sys, collections

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/scene_class_filename_census.py <参数1>\n  说明：普查：哪些 MonoBehaviour 类【没有同名 .cs 文件】—— 那类类 Unity 建不出 MonoScript，\n  选项：无自定义选项（不带参数 = 默认模式）\n  位置参数：脚本内读 sys.argv[1]（缺省/必填以脚本自身判断为准）')
    _wf_help_sys.exit(0)

ROOT = sys.argv[1]
files = []
for dp, dn, fn in os.walk(ROOT):
    norm = dp.replace(os.sep, '/')
    if any(x in norm for x in ('/Library/', '/Temp/', '/obj/', '/.git/')) or norm.endswith('/Library'):
        continue
    for f in fn:
        if f.endswith('.cs'):
            files.append(os.path.join(dp, f))

decl = {}       # class -> (relpath, first base)
for p in files:
    txt = io.open(p, encoding='utf-8', errors='replace').read()
    rel = os.path.relpath(p, ROOT).replace(os.sep, '/')
    pat = (r'^[ \t]*(?:\[[^\]]*\][ \t]*\r?\n[ \t]*)*'
           r'(?:public|internal|sealed|abstract|static|partial|new|unsafe|[ \t])*'
           r'\bclass\s+(\w+)\s*(?::\s*([^\{\r\n]+))?')
    for m in re.finditer(pat, txt, re.M):
        name = m.group(1)
        base = (m.group(2) or '').strip()
        if name not in decl:
            decl[name] = (rel, base)

# 传递闭包到 MonoBehaviour
mb = set()
changed = True
while changed:
    changed = False
    for n, (rel, b) in decl.items():
        if n in mb:
            continue
        for one in b.split(','):
            base = one.split('<')[0].strip()
            if base in ('MonoBehaviour', 'MonoBehaviourPun', 'MonoBehaviourPunCallbacks') or base in mb:
                mb.add(n)
                changed = True
                break

namefiles = set(os.path.basename(p)[:-3] for p in files)
bad = [(n, decl[n][0], decl[n][1].split(',')[0].strip()) for n in sorted(mb) if n not in namefiles]

print('扫了 %d 个 .cs；认出的 MonoBehaviour 类 %d 个；其中【无同名文件】%d 个\n' % (len(files), len(mb), len(bad)))
byfile = collections.defaultdict(list)
for n, rel, b in bad:
    byfile[rel].append('%s(:%s)' % (n, b))
for rel in sorted(byfile):
    print('%-52s %s' % (rel, ' · '.join(sorted(byfile[rel]))))
