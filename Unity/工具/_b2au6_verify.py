# -*- coding: utf-8 -*-
# 只读校验：37 条 cue 是否进了 animfx_sounds.json；34 个文件是否落盘；两张表的条数
import io, json, os

RES = 'D:/4/Unity/MyGame/Assets/CardPresentation/Resources'
CUES = ["Asteroid Zone", "Booster Open AstraMilitarum", "Booster Open BlackLegion",
        "Booster Open Emperors Children", "Booster Open Orks", "Booster Open SaimHann",
        "Booster Open Sororitas", "Booster Open Tau", "ButtonClick 1", "Daemonic Feast",
        "Dawn Attack", "Deck Editing Valid Deck", "Draft Pack Appear", "Draft Pack Finish Animation",
        "Draft Pack Selected Moving", "Draft Prebuilt Pack preview", "Energy Accumulated",
        "Energy Accumulated Used", "Everstorm", "Factory Overdrive", "Firestorm Loop", "First Light",
        "Forge Card", "Full Moon", "Gacha vault open", "Immortal Beams", "Orbital Initial",
        "Orks Dust Storm Start", "Ranked Seal SFX", "Star Orbit", "Sump Overspill",
        "Tau Electro-Static Interference", "Toxic Fumes Start", "UI Raid Claim Reward Ching",
        "UI Raid Claim Reward Wait", "Warp Storm", "Webway Rift"]

cues = json.load(io.open(os.path.join(RES, 'animfx_sounds.json'), encoding='utf-8'))['cues']
by = {c['name']: c for c in cues}
print('animfx_sounds cues =', len(cues))
miss = [n for n in CUES if n not in by]
print('37 条里缺 =', len(miss), miss)

sfx = os.path.join(RES, 'Art', 'audio', 'sfx')
files = set(os.path.splitext(f)[0] for f in os.listdir(sfx) if not f.endswith('.meta'))
nofile = []
for n in CUES:
    c = by.get(n)
    if not c:
        continue
    for cl in c['clips']:
        if cl.strip() not in files:
            nofile.append((n, cl))
print('cue 指向的 clip 缺席 =', len(nofile), nofile)

print('--- 抽样 5 条（名字 / clips / pitch / vol / ttp）')
for n in CUES[:5]:
    c = by.get(n)
    print('   ', n, '->', c['clips'] if c else 'MISSING',
          (c['minPitch'], c['maxPitch'], c['minVolume'], c['maxVolume'], c['timeToPlayAgain']) if c else '')

# 两处文件计数
print('sfx 目录：', len(files), '个 clip 文件（不含 meta）')
print('battle_sounds cues =', len(json.load(io.open(os.path.join(RES, 'battle_sounds.json'), encoding='utf-8'))['cues']))
