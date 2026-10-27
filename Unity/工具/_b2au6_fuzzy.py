# -*- coding: utf-8 -*-
# 只读：对 35 条「clip 文件也没有」做 ①模糊匹配（去空格/下划线/大小写）②原版解包资源里找 AudioClip
import io, json, os, csv, re

RES = 'D:/4/Unity/MyGame/Assets/CardPresentation/Resources'
MAP = 'D:/4/Unity/资料/完全复刻/判据/音效cue到WindowSounds_映射.tsv'
ASSETS = 'D:/2/新解包资源/assets_full'

def norm(s):
    return re.sub(r'[\s_\-]+', '', (s or '').lower())

# 我们的音频文件（全部，带归一化名）
ours = {}
for dirpath, _, files in os.walk(os.path.join(RES, 'Art', 'audio')):
    for f in files:
        if f.endswith('.meta'):
            continue
        stem, ext = os.path.splitext(f)
        ours.setdefault(norm(stem), []).append(os.path.join(dirpath, f).replace('\\', '/').split('Resources/')[-1])

rows = list(csv.reader(io.open(MAP, encoding='utf-8', newline=''), delimiter='\t'))
I = {k: i for i, k in enumerate(rows[0])}
gap = [r for r in rows[1:] if r[I['状态']] in ('缺·UI', '缺·战场环境')]

# 原版解包里的 AudioClip 名（只扫 assets_full 里 name 字段像音频的 MonoBehaviour/AudioClip）
# 便宜做法：只看 bundle_soundcollection 的目录清单 + 全局搜文件名
cands = []
for r in gap:
    clip = r[I['clip（clipList 解出的真名）']].strip()
    fuzzy = ours.get(norm(clip))
    cands.append((r[I['序']], r[I['原版cue名']], clip, fuzzy))

print('=== 模糊匹配（我们的 Resources/Art/audio 全树）===')
for seq, cue, clip, fuzzy in cands:
    if fuzzy:
        print('%-3s %-34s clip=%-40s -> %s' % (seq, cue[:34], clip[:40], fuzzy[:3]))
print('（以上为命中；没列出的 = 模糊也找不到）')
