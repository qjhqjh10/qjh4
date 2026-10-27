# -*- coding: utf-8 -*-
# 只读：37 条逐条确认「原版解包里的 AudioClip 在不在」+ 我们的目标路径
import io, os, csv, re

MAP = 'D:/4/Unity/资料/完全复刻/判据/音效cue到WindowSounds_映射.tsv'
ORIG = 'D:/2/新解包资源/assets_full/bundle_soundcollection_assets_all/AudioClip'
OURS = 'D:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/audio/sfx'

orig = {}
for f in os.listdir(ORIG):
    if f.endswith('.meta'):
        continue
    stem, ext = os.path.splitext(f)
    orig.setdefault(stem.lower(), []).append(f)

def norm(s):
    return re.sub(r'[\s_\-]+', '', (s or '').lower())

orig_norm = {}
for k, v in orig.items():
    orig_norm.setdefault(norm(k), []).extend(v)

ours = set()
for f in os.listdir(OURS):
    if not f.endswith('.meta'):
        ours.add(os.path.splitext(f)[0].lower())

rows = list(csv.reader(io.open(MAP, encoding='utf-8', newline=''), delimiter='\t'))
I = {k: i for i, k in enumerate(rows[0])}
gap = [r for r in rows[1:] if r[I['状态']] in ('缺·UI', '缺·战场环境')]

hit = miss = 0
for r in gap:
    clip = r[I['clip（clipList 解出的真名）']].strip()
    ex = orig.get(clip.lower()) or orig_norm.get(norm(clip))
    have = clip.lower() in ours
    if ex:
        hit += 1
        print('OK   %-3s %-34s clip=%-40s 原版文件=%s%s' % (
            r[I['序']], r[I['原版cue名']][:34], clip[:40], ex[:2], '（我们已导入）' if have else ''))
    else:
        miss += 1
        print('MISS %-3s %-34s clip=%-40s  ★原版解包里也没找到' % (r[I['序']], r[I['原版cue名']][:34], clip[:40]))
print()
print('原版有 wav =', hit, ' · 原版也没有 =', miss)
