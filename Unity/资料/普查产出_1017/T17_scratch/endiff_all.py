import json, io, re
official = json.load(io.open('d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json', 'r', encoding='utf-8'))['terms']
entries = {}
pat = re.compile(r'\{ "([^"]+)",\s*new Entry\("(.*?)",\s*"(.*?)"\)')
for line in io.open('d:/4/Unity/MyGame/Assets/CardPresentation/Core/Loc.cs', 'r', encoding='utf-8'):
    m = pat.search(line)
    if m:
        entries[m.group(1)] = (m.group(2), m.group(3))
out = io.open('d:/4/Unity/资料/普查产出_1017/T17_scratch/en_diff_all.txt', 'w', encoding='utf-8')
en_mis = 0
zh_mis = 0
for k, (zh, en) in sorted(entries.items()):
    if k not in official:
        continue
    off_en, off_zh = official[k][0], (official[k][1] if len(official[k]) > 1 else '')
    if en.strip() != off_en.strip():
        out.write('[EN] %s\n  ours: %s\n  off : %s\n' % (k, en, off_en)); en_mis += 1
    if zh.strip() != off_zh.strip():
        out.write('[ZH] %s\n  ours: %s\n  off : %s\n' % (k, zh, off_zh)); zh_mis += 1
out.write('Loc entries with an official key: %d ; EN mismatches=%d ; ZH mismatches=%d\n' % (
    sum(1 for k in entries if k in official), en_mis, zh_mis))
out.close()
print('done', en_mis, zh_mis)
