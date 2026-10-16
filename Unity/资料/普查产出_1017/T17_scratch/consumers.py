import json, io, os, re
ROOT = 'd:/4/Unity/MyGame/Assets'
official = json.load(io.open('d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json', 'r', encoding='utf-8'))['terms']
# Loc keys
pat = re.compile(r'\{ "([^"]+)",\s*new Entry\(')
lockeys = []
for line in io.open(ROOT + '/CardPresentation/Core/Loc.cs', 'r', encoding='utf-8'):
    m = pat.search(line)
    if m:
        lockeys.append(m.group(1))
# gather all cs (excluding Loc.cs)
texts = []
for dirpath, _, files in os.walk(ROOT):
    for f in files:
        if f.endswith('.cs') and f != 'Loc.cs':
            try:
                texts.append(io.open(os.path.join(dirpath, f), 'r', encoding='utf-8', errors='replace').read())
            except Exception:
                pass
blob = '\n'.join(texts)
out = io.open('d:/4/Unity/资料/普查产出_1017/T17_scratch/consumers.txt', 'w', encoding='utf-8')
fams = ['Battle/Tips/', 'Battle/Effect/', 'Battle/Cemetery/', 'Battle/HUD/', 'Battle/Chat/', 'Battle/Mulligan/', 'Battle/BattleEnd/', 'Battle/AlliancePanel/', 'Battle/ChooseCard/', 'Battle/Prebattle/', 'Battle/Settings/', 'Battle/MatchType/']
for fam in fams:
    ks = sorted(k for k in lockeys if k.startswith(fam))
    wired = [k for k in ks if blob.count('"' + k + '"') > 0]
    unwired = [k for k in ks if blob.count('"' + k + '"') == 0]
    out.write('### %s : total=%d wired=%d unwired=%d\n' % (fam, len(ks), len(wired), len(unwired)))
    out.write('  WIRED: ' + ' '.join(k[len(fam):] for k in wired) + '\n')
    out.write('  UNWIRED: ' + ' '.join(k[len(fam):] for k in unwired) + '\n\n')
out.close()
print('done')
