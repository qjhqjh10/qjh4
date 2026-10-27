# -*- coding: utf-8 -*-
"""第八批：把 37 条缺口在映射表里落成「对上」（默认 dry-run；--apply 才写）。

前置（已跑）：`工具/import_original_sfx.py` 收录 37 条 cue → `animfx_sounds.json` 473→510，
`Art/audio/sfx/` 拷入 34 个新文件（148/150 共用一个 clip）。
"""
import io, json, os, csv, sys

MAP = 'D:/4/Unity/资料/完全复刻/判据/音效cue到WindowSounds_映射.tsv'
RES = 'D:/4/Unity/MyGame/Assets/CardPresentation/Resources'
APPLY = '--apply' in sys.argv

cues = {c['name']: c for c in json.load(io.open(os.path.join(RES, 'animfx_sounds.json'), encoding='utf-8'))['cues']}
files = {}
for dirpath, _, fs in os.walk(os.path.join(RES, 'Art', 'audio')):
    for f in fs:
        if f.endswith('.meta'):
            continue
        stem, ext = os.path.splitext(f)
        files.setdefault(stem.strip(), os.path.join(dirpath, f).replace('\\', '/').split('Resources/')[-1])

rows = list(csv.reader(io.open(MAP, encoding='utf-8', newline=''), delimiter='\t'))
H = rows[0]
I = {k: i for i, k in enumerate(H)}
out = [H]
changed = 0
for r in rows[1:]:
    st = r[I['状态']]
    cue = r[I['原版cue名']]
    if st not in ('缺·原版资产在本地未导入', '对上·走clip代替') or cue not in cues:
        out.append(r)
        continue
    r = list(r)
    c = cues[cue]
    clip = c['clips'][0].strip() if c['clips'] else ''
    path = files.get(clip, '')
    r[I['状态']] = '对上'
    r[I['我们的key']] = cue
    r[I['我们侧出处']] = 'animfx_sounds.json（cue `%s`）+ `%s`' % (cue, path)
    r[I['备注']] = ((r[I['备注']] + ' ｜') if r[I['备注']] else '') + (
        '[B2AU 2026-10-27 补缺实施] 已落地：`工具/import_original_sfx.py` 的 `EXTRA_CUES` 收了这条 cue ⇒ '
        '音频拷进 `Resources/Art/audio/sfx/`（含 `.meta`）+ `animfx_sounds.json` 重生（473→510）。'
        '参数取原版 cue 资产实读：pitch %s–%s · vol %s–%s · ttp %s。'
        % (c['minPitch'], c['maxPitch'], c['minVolume'], c['maxVolume'], c['timeToPlayAgain']))
    changed += 1
    out.append(r)

print('更新行数 =', changed, '· 写盘 =', APPLY)
if APPLY:
    with io.open(MAP, 'w', encoding='utf-8', newline='') as f:
        w = csv.writer(f, delimiter='\t', lineterminator='\n', quoting=csv.QUOTE_MINIMAL)
        for r in out:
            w.writerow(r)
    print('已写', MAP)
else:
    print('（dry-run：加 --apply 才写盘）')
