import json, io, os, re
ROOT = 'd:/4/Unity/MyGame/Assets'
pat = re.compile(r'\{ "([^"]+)",\s*new Entry\(')
lockeys = []
for line in io.open(ROOT + '/CardPresentation/Core/Loc.cs', 'r', encoding='utf-8'):
    m = pat.search(line)
    if m:
        lockeys.append(m.group(1))
files = []
for dirpath, _, fs in os.walk(ROOT):
    for f in fs:
        if f.endswith('.cs') and f != 'Loc.cs':
            try:
                files.append((os.path.join(dirpath, f).replace(ROOT + '/', ''), io.open(os.path.join(dirpath, f), 'r', encoding='utf-8', errors='replace').read()))
            except Exception:
                pass
out = io.open('d:/4/Unity/资料/普查产出_1017/T17_scratch/consumers2.txt', 'w', encoding='utf-8')
fams = ['Battle/Tips/', 'Battle/Effect/', 'Battle/Cemetery/', 'Battle/HUD/', 'Battle/Chat/']
for fam in fams:
    ks = sorted(k for k in lockeys if k.startswith(fam))
    for k in ks:
        hits = []
        for path, txt in files:
            c = txt.count('"' + k + '"')
            if c:
                hits.append('%s x%d' % (os.path.basename(path), c))
        # a lone hit is likely just the const declaration (dead)
        mark = 'WIRED' if len(hits) >= 2 or (hits and not any(h.endswith('x1') for h in hits)) else ('const-only' if hits else 'UNWIRED')
        out.write('%s\t%s\t%s\n' % (mark, k, '; '.join(hits) if hits else '(none)'))
    out.write('\n')
out.close()
print('done')
