# -*- coding: utf-8 -*-
"""全树反查：17 条环境 cue 的 PathID 被谁引用（只读）。
扫 assets_full 下所有 MonoBehaviour / AudioSource 的 .json。
"""
import io, json, os, re, sys

ROOT = 'D:/2/新解包资源/assets_full'
CUES = {
 'Asteroid Zone': 2076751965472574616,
 'Daemonic Feast': 7869957202292150956,
 'Dawn Attack': 8147575272355607868,
 'Everstorm': -594538407514888180,
 'Factory Overdrive': 7758829246773958931,
 'Firestorm Loop': -5993767068827573205,
 'First Light': 1210299923766823312,
 'Full Moon': -2043529631028593154,
 'Immortal Beams': -42160604713367801,
 'Orbital Initial': -3888569816479931127,
 'Orks Dust Storm Start': 874716759609316447,
 'Star Orbit': -3819335262797368526,
 'Sump Overspill': 3287416670577984858,
 'Tau Electro-Static Interference': -895455500301147291,
 'Toxic Fumes Start': 9033423146207745146,
 'Warp Storm': 404106094856904824,
 'Webway Rift': 165753871913135722,
}
PIDS = {str(v): k for k, v in CUES.items()}
PAT = re.compile(r'(?<![0-9-])(' + '|'.join(re.escape(p) for p in PIDS) + r')(?![0-9])')

dirs = ('MonoBehaviour', 'AudioSource')
scanned = 0
hits = []          # (bundle, kind, file, cue, 该文件里该 pid 出现的次数)
for bundle in sorted(os.listdir(ROOT)):
    bpath = os.path.join(ROOT, bundle)
    if not os.path.isdir(bpath):
        continue
    for kind in dirs:
        d = os.path.join(bpath, kind)
        if not os.path.isdir(d):
            continue
        for f in os.listdir(d):
            if not f.endswith('.json'):
                continue
            scanned += 1
            p = os.path.join(d, f)
            try:
                t = io.open(p, encoding='utf-8', errors='ignore').read()
            except Exception:
                continue
            m = PAT.findall(t)
            if not m:
                continue
            for pid in set(m):
                hits.append((bundle, kind, f, PIDS[pid], m.count(pid)))

print('扫描文件 =', scanned)
print('命中条数 =', len(hits))
by_cue = {}
for b, k, f, cue, n in hits:
    by_cue.setdefault(cue, []).append((b, k, f, n))
for cue in CUES:
    hs = by_cue.get(cue, [])
    print('\n== %s  命中 %d' % (cue, len(hs)))
    for b, k, f, n in hs[:6]:
        print('    %-52s %-12s %-40s x%d' % (b, k, f, n))
