import io
p = r'd:/4/Unity/资料/普查产出_1015/R6_A544商品图标判据.md'
BS = chr(92)
for i, l in enumerate(io.open(p, encoding='utf-8'), 1):
    l = l.rstrip('\n')
    if not l.startswith('|'):
        continue
    n = 0
    j = 0
    while j < len(l):
        if l[j] == BS:
            j += 2
            continue
        if l[j] == '|':
            n += 1
        j += 1
    print('%4d  pipes=%d  %s' % (i, n, l[:50]))
