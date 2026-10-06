import io, sys
p = sys.argv[1] if len(sys.argv) > 1 else r'd:/4/项目任务.md'
keys = sys.argv[2:] if len(sys.argv) > 2 else ['| **A163**', '| **A179**', '| **A186', '| **A231', '| **A434**', '| **A556']
L = io.open(p, encoding='utf-8').read().split('\n')
for i, l in enumerate(L):
    if not l.startswith('|'):
        continue
    if any(l.startswith(k) for k in keys):
        esc = l.count('\\|')
        raw = l.count('|')
        print(i + 1, 'raw=%d esc=%d cols=%d' % (raw, esc, raw - esc - 1), l[:34].replace('\n', ''))
