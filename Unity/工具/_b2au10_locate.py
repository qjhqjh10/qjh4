# -*- coding: utf-8 -*-
"""定位：17 条环境 cue 各自被哪个 ShakeAndSoundObject 组件引用 → 归属哪个 GameObject(prefab)、哪个字段。只读。"""
import io, json, os

ROOT = 'D:/2/新解包资源/assets_full'
BUNDLE = 'bundle_battleprefabs_vfxandmisc_assets_all'
CUES = {
 'Asteroid Zone': 2076751965472574616, 'Daemonic Feast': 7869957202292150956,
 'Dawn Attack': 8147575272355607868, 'Everstorm': -594538407514888180,
 'Factory Overdrive': 7758829246773958931, 'Firestorm Loop': -5993767068827573205,
 'First Light': 1210299923766823312, 'Full Moon': -2043529631028593154,
 'Immortal Beams': -42160604713367801, 'Orbital Initial': -3888569816479931127,
 'Orks Dust Storm Start': 874716759609316447, 'Star Orbit': -3819335262797368526,
 'Sump Overspill': 3287416670577984858, 'Tau Electro-Static Interference': -895455500301147291,
 'Toxic Fumes Start': 9033423146207745146, 'Warp Storm': 404106094856904824,
 'Webway Rift': 165753871913135722,
}
BY_PID = {v: k for k, v in CUES.items()}
MB_DIR = os.path.join(ROOT, BUNDLE, 'MonoBehaviour')
GO_DIR = os.path.join(ROOT, BUNDLE, 'GameObject')

def go_name(pid):
    p = os.path.join(GO_DIR, 'GameObject_%d.json' % pid)
    if not os.path.exists(p):
        return 'GameObject_%d.json(缺)' % pid
    try:
        return json.load(io.open(p, encoding='utf-8')).get('m_Name') or '?'
    except Exception:
        return '?'

rows = []
for f in os.listdir(MB_DIR):
    if not f.endswith('.json'):
        continue
    try:
        d = json.load(io.open(os.path.join(MB_DIR, f), encoding='utf-8'))
    except Exception:
        continue
    if 'sounds' not in d or 'onEnableShakes' not in d:
        continue           # 只要 ShakeAndSoundObject
    gopid = (d.get('m_GameObject') or {}).get('m_PathID')
    for i, s in enumerate(d.get('sounds') or []):
        snd = (s or {}).get('sound')
        pid = snd.get('m_PathID') if isinstance(snd, dict) else None
        if pid in BY_PID:
            rows.append((BY_PID[pid], f, 'sounds[%d].sound' % i, go_name(gopid), pid))

seen = {}
for cue, f, field, go, pid in rows:
    seen.setdefault(cue, []).append((f, field, go))
for cue in CUES:
    hs = seen.get(cue, [])
    if not hs:
        print('MISS %-32s —— 没在 ShakeAndSoundObject 里找到' % cue)
        continue
    for f, field, go in hs:
        print('OK   %-32s prefab=%-56s %-16s %s' % (cue, go[:56], field, f))
print()
print('命中 cue 数 =', len(seen), '/ 17')
