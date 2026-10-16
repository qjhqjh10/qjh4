import json, io, re
official = json.load(io.open('d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json', 'r', encoding='utf-8'))['terms']
entries = {}
pat = re.compile(r'\{ "([^"]+)",\s*new Entry\("(.*?)",\s*"(.*?)"\)')
for line in io.open('d:/4/Unity/MyGame/Assets/CardPresentation/Core/Loc.cs', 'r', encoding='utf-8'):
    m = pat.search(line)
    if m:
        entries[m.group(1)] = (m.group(2), m.group(3))
out = io.open('d:/4/Unity/资料/普查产出_1017/T17_scratch/en_diff.txt', 'w', encoding='utf-8')
n = 0
for k, (zh, en) in entries.items():
    if not k.startswith('Battle/'):
        continue
    if k not in official:
        continue
    off_en = official[k][0]
    if en.strip() != off_en.strip():
        out.write('%s\n  ours EN: %s\n  off  EN: %s\n' % (k, en, off_en))
        n += 1
tot = sum(1 for k in entries if k.startswith('Battle/'))
out.write('total Battle/ EN mismatches vs official: %d (of %d Battle entries parsed)\n' % (n, tot))
out.close()
print('done', n, tot)
