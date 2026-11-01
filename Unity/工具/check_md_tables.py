import io, sys

def cells(line):
    # split on unescaped pipes
    out, cur, i = [], [], 0
    while i < len(line):
        ch = line[i]
        if ch == '\\' and i + 1 < len(line) and line[i + 1] == '|':
            cur.append('|')
            i += 2
            continue
        if ch == '|':
            out.append(''.join(cur))
            cur = []
        else:
            cur.append(ch)
        i += 1
    out.append(''.join(cur))
    return out

path = sys.argv[1]
start = int(sys.argv[2]); end = int(sys.argv[3])
lines = io.open(path, 'rb').read().decode('utf-8').split('\n')
bad = 0
expect = None
for n in range(start, end + 1):
    l = lines[n - 1]
    if not l.startswith('|'):
        continue
    c = cells(l)
    if expect is None:
        expect = len(c)
        print('line', n, 'header cells', expect)
        continue
    if len(c) != expect:
        bad += 1
        print('BAD line', n, 'cells', len(c), 'expected', expect)
        print('   ', l[:200])
print('bad rows:', bad)
