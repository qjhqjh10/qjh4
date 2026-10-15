import re, io, glob, os, subprocess

MEM = 'C:/Users/qjh36/.claude/projects/d--4/memory/'
pat = re.compile('\`((?:Assets|Resources|Scenes|Shell|Core|Battle|Deck|Editor|RuleEngine|CardPresentation|WarpforgeVFX|StreamingAssets|Board|Net)/[^\`\n]*?[.](?:cs|unity|json|sh))\`')
seen = {}
for f in sorted(glob.glob(MEM + '*.md')):
    t = io.open(f, encoding='utf-8').read()
    for m in pat.finditer(t):
        seen.setdefault(m.group(1), set()).add(os.path.basename(f))

# build index of basenames under d:/4/Unity/MyGame/Assets
idx = {}
for root, dirs, files in os.walk('d:/4/Unity/MyGame/Assets'):
    for fn in files:
        idx.setdefault(fn, []).append(os.path.join(root, fn).replace('\\', '/'))

for p, fs in sorted(seen.items()):
    base = os.path.basename(p)
    hits = idx.get(base, [])
    tail = p.split('/', 1)[1] if '/' in p else p
    # try to match by path suffix
    exact = [h for h in hits if h.endswith('/' + p) or h.endswith('/' + tail)]
    if exact:
        continue
    if hits:
        print('DIFF-PATH  %-46s <- %s' % (p, ','.join(sorted(fs))[:90]))
        for h in hits[:4]:
            print('             实读: ' + h.replace('d:/4/Unity/MyGame/Assets/', ''))
    else:
        print('NOT-FOUND  %-46s <- %s' % (p, ','.join(sorted(fs))[:90]))
