# -*- coding: utf-8 -*-
"""把「缺 UI 20 + 缺战场 17」的 37 条补进映射表（默认 dry-run；要写盘必须给 --apply）。

分类口径（本轮现查，证据见 报告/B9_B2AU6_cue映射补缺.md）：
  · A = 我们这边**已经有承载同一 clip 的 cue** ⇒ 状态 `对上·走clip代替`（表里已有这个状态值，5 条前例）
  · B = 原版**音频文件在本地**（`bundle_soundcollection_assets_all/AudioClip/`）但**没导进我们 Resources** ⇒
        状态 `缺·原版资产在本地未导入`（新值；比原来的「缺·UI / 缺·战场环境」更准 —— 缺的不是「原版没有」而是「没导入」）
  · C = 原版自己也没有 ⇒ 本轮 **0 条**（37/37 全在原版 bundle 里）
"""
import io, os, csv, re, sys

MAP = 'D:/4/Unity/资料/完全复刻/判据/音效cue到WindowSounds_映射.tsv'
RES = 'D:/4/Unity/MyGame/Assets/CardPresentation/Resources'
ORIG_DIR = 'D:/2/新解包资源/assets_full/bundle_soundcollection_assets_all/AudioClip'

APPLY = '--apply' in sys.argv

# ---- 我方已有的 cue（cue 名 -> (表文件, clip 列表)）----
import json
anim = json.load(io.open(os.path.join(RES, 'animfx_sounds.json'), encoding='utf-8'))['cues']
batt = json.load(io.open(os.path.join(RES, 'battle_sounds.json'), encoding='utf-8'))['cues']
cue_of_clip = {}
for c in anim:
    for cl in c.get('clips', []):
        cue_of_clip.setdefault(cl.strip(), ('animfx_sounds.json', c['name']))
for c in batt:
    for cl in c.get('clips', []):
        cue_of_clip.setdefault(cl.strip(), ('battle_sounds.json', c['name']))

# ---- 我方音频文件 ----
ours = {}
for dirpath, _, files in os.walk(os.path.join(RES, 'Art', 'audio')):
    for f in files:
        if f.endswith('.meta'):
            continue
        stem, ext = os.path.splitext(f)
        ours.setdefault(stem.strip(), os.path.join(dirpath, f).replace('\\', '/').split('Resources/')[-1])

# ---- 原版音频文件 ----
orig = set()
for f in os.listdir(ORIG_DIR):
    if not f.endswith('.meta'):
        orig.add(f)

rows = list(csv.reader(io.open(MAP, encoding='utf-8', newline=''), delimiter='\t'))
H = rows[0]
I = {k: i for i, k in enumerate(H)}
out = [H]
stat = {}
for r in rows[1:]:
    st = r[I['状态']]
    if st not in ('缺·UI', '缺·战场环境'):
        out.append(r)
        continue
    r = list(r)
    clip = r[I['clip（clipList 解出的真名）']].strip()
    cue = r[I['原版cue名']]
    have_file = ours.get(clip)
    have_cue = cue_of_clip.get(clip)

    if have_file and have_cue:
        r[I['状态']] = '对上·走clip代替'
        r[I['我们的key']] = have_cue[1]
        r[I['我们侧出处']] = '%s（cue `%s`）+ `%s`' % (have_cue[0], have_cue[1], have_file)
        r[I['备注']] = ((r[I['备注']] + ' ｜') if r[I['备注']] else '') + (
            '[B2AU 2026-10-27] 我方**没有同名 cue**，但**同一 clip `%s` 已导入且已有 cue `%s`** '
            '⇒ 本条只缺「登记」：按同一 clip 复用那条 cue（表内 `对上·走clip代替` 已有 5 条前例）。'
            '⚠️ 音高/音量若与原版那三列不同，接的时候要按本行前三列覆盖。' % (clip, have_cue[1]))
    elif have_file:
        r[I['状态']] = '缺·仅缺 cue 条目'
        r[I['我们的key']] = '—（需新增 cue 条目，复用 clip `%s`）' % clip
        r[I['我们侧出处']] = have_file
        r[I['备注']] = ((r[I['备注']] + ' ｜') if r[I['备注']] else '') + \
            '[B2AU 2026-10-27] clip 已导入、只是没有 cue 条目。'
    else:
        # 原版有没有这个文件？
        cand = [f for f in orig if os.path.splitext(f)[0].strip() == clip]
        if cand:
            r[I['状态']] = '缺·原版资产在本地未导入'
            r[I['备注']] = ((r[I['备注']] + ' ｜') if r[I['备注']] else '') + (
                '[B2AU 2026-10-27] **不是「原版没有」**：原版音频就在本地 '
                '`d:/2/新解包资源/assets_full/bundle_soundcollection_assets_all/AudioClip/%s`，'
                '我们 `Resources/Art/audio/sfx/` 里没有 ⇒ 缺两步：① 跑导入器把它导进 `Resources/Art/audio/sfx/`；'
                '② 加一条 cue 条目（名字 = `%s`，clip = `%s`，音高/音量/ttp = 本行前三列）。'
                '⛔ 别手改 `animfx_sounds.json`（那份文件由 `工具/import_original_sfx.py` 重生）。' % (cand[0], cue, clip))
        else:
            r[I['状态']] = '缺·原版也没有'
            r[I['备注']] = ((r[I['备注']] + ' ｜') if r[I['备注']] else '') + \
                '[B2AU 2026-10-27] 原版 `bundle_soundcollection_assets_all/AudioClip/` 里**也没有**这个 clip。'
    stat[r[I['状态']]] = stat.get(r[I['状态']], 0) + 1
    out.append(r)

print('分类结果：', stat)
print('写盘 =', APPLY)
if APPLY:
    with io.open(MAP, 'w', encoding='utf-8', newline='') as f:
        w = csv.writer(f, delimiter='\t', lineterminator='\n', quoting=csv.QUOTE_MINIMAL)
        for r in out:
            w.writerow(r)
    print('已写', MAP, '总行', len(out))
else:
    print('（dry-run：加 --apply 才写盘）')
