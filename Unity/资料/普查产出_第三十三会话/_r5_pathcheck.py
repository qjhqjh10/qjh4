import re, io, glob, os

MEM = 'C:/Users/qjh36/.claude/projects/d--4/memory/'
BS = chr(92)
pat = re.compile('\`([A-Za-z]:[' + BS + BS + '/][^\`' + BS + 'n]*?[.](?:md|py|json|csv|cs|sh|unity|gd|exe|txt|png))\`')
seen = {}
for f in sorted(glob.glob(MEM + '*.md')):
    t = io.open(f, encoding='utf-8').read()
    for m in pat.finditer(t):
        p = m.group(1).replace(BS, '/')
        seen.setdefault(p, set()).add(os.path.basename(f))

miss = []
ok = []
for p, fs in sorted(seen.items()):
    q = p if not (p[:2] in ('d:', 'D:')) else p[2:]
    cand = [q, 'd:' + q, 'D:' + q]
    if any(os.path.exists(c) for c in cand):
        ok.append((p, sorted(fs)))
    else:
        miss.append((p, sorted(fs)))

print('== 不存在 (%d) ==' % len(miss))
for p, fs in miss:
    print(p, '  <-', ','.join(fs)[:130])
print()
print('== 存在 %d ==' % len(ok))
