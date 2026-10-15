import re, io, glob, os

MEM = 'C:/Users/qjh36/.claude/projects/d--4/memory/'
BASES = ['d:/4/Unity/', 'd:/4/', 'd:/2/', 'd:/4/Unity/MyGame/Assets/']
pat = re.compile('\`((?:\u8d44\u6599|\u5de5\u5177|\u6570\u636e|Assets|Resources|Scenes|Shell|Core|Battle|Deck|Editor|RuleEngine|CardPresentation|WarpforgeVFX|StreamingAssets)/[^\`\n]*?[.](?:md|py|json|csv|cs|sh|unity|gd|txt))\`')
seen = {}
for f in sorted(glob.glob(MEM + '*.md')):
    t = io.open(f, encoding='utf-8').read()
    for m in pat.finditer(t):
        seen.setdefault(m.group(1), set()).add(os.path.basename(f))

miss = []
ok = []
for p, fs in sorted(seen.items()):
    if any(os.path.exists(b + p) for b in BASES):
        ok.append((p, sorted(fs)))
    else:
        miss.append((p, sorted(fs)))

print('== 相对路径 - 在 4 个基目录下都不存在 (%d) ==' % len(miss))
for p, fs in miss:
    print(p, '  <-', ','.join(fs)[:130])
print()
print('== 存在 %d ==' % len(ok))
for p, fs in ok:
    print(p, '  <-', ','.join(fs)[:110])
