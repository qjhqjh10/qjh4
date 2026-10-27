# -*- coding: utf-8 -*-
# 只读：把「缺 UI 20 + 缺战场 17」逐条分类（不写文件）
import io, json, os, csv

RES = 'D:/4/Unity/MyGame/Assets/CardPresentation/Resources'
MAP = 'D:/4/Unity/资料/完全复刻/判据/音效cue到WindowSounds_映射.tsv'

anim = json.load(io.open(os.path.join(RES, 'animfx_sounds.json'), encoding='utf-8'))['cues']
batt = json.load(io.open(os.path.join(RES, 'battle_sounds.json'), encoding='utf-8'))['cues']

cue_by_name = {}
for c in anim:
    cue_by_name[c['name']] = ('animfx_sounds.json', c)
for c in batt:
    cue_by_name.setdefault(c['name'], ('battle_sounds.json', c))

clip_index = {}   # clip 名 -> 相对路径
sfx_root = os.path.join(RES, 'Art', 'audio')
for dirpath, _, files in os.walk(sfx_root):
    for f in files:
        if f.endswith('.meta'):
            continue
        stem, ext = os.path.splitext(f)
        clip_index[stem] = os.path.join(dirpath, f).replace('\\', '/').split('CardPresentation/Resources/')[-1]

rows = list(csv.reader(io.open(MAP, encoding='utf-8', newline=''), delimiter='\t'))
H = rows[0]
I = {k: i for i, k in enumerate(H)}

gap = [r for r in rows[1:] if r[I['状态']] in ('缺·UI', '缺·战场环境')]
print('gap', len(gap))
cats = {}
for r in gap:
    cue = r[I['原版cue名']]
    clip = r[I['clip（clipList 解出的真名）']]
    have_cue = cue_by_name.get(cue)
    clip_path = clip_index.get(clip.strip())
    # 有同名 clip 的 cue（换名登记）
    same_clip_cues = [c['name'] for c in anim if clip.strip() in [x.strip() for x in c.get('clips', [])]]
    same_clip_cues += [c['name'] for c in batt if clip.strip() in [x.strip() for x in c.get('clips', [])]]
    if have_cue:
        cat = 'A·cue 已在我们的表里'
    elif clip_path:
        cat = 'B·clip 文件在、缺 cue 条目'
    elif same_clip_cues:
        cat = 'C·同 clip 已有别的 cue'
    else:
        cat = 'D·clip 文件也没有'
    cats[cat] = cats.get(cat, 0) + 1
    print('%-2s %-28s | cue=%-34s | clip=%-40s | %s%s%s' % (
        r[I['序']], cat, cue[:34], clip[:40],
        ('[表=%s]' % have_cue[0]) if have_cue else '',
        ('[file=%s]' % clip_path) if clip_path else '',
        ('[同名clip的cue=%s]' % same_clip_cues[:3]) if same_clip_cues else ''))
print()
for k, v in sorted(cats.items()):
    print(k, v)
