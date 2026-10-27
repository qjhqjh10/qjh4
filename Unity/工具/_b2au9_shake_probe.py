# -*- coding: utf-8 -*-
# 只读：35 个原版 ShakeAndSoundObject 组件各自引用哪些 cue（PPtr pathID -> 文件名后缀索引）
import io, json, os, glob

CUE_DIR = 'D:/2/新解包资源/assets_full/bundle_soundcollection_assets_all/MonoBehaviour'
PREFAB_DIR = 'D:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour'

# pathID -> cue 名（文件名形如 "<cue 名>_<pathID>.json"）
by_pid = {}
for p in glob.glob(os.path.join(CUE_DIR, '*.json')):
    stem = os.path.splitext(os.path.basename(p))[0]
    if '_' not in stem:
        continue
    name, pid = stem.rsplit('_', 1)
    try:
        by_pid[int(pid)] = name
    except ValueError:
        pass

hits = []
for p in glob.glob(os.path.join(PREFAB_DIR, '*.json')):
    try:
        d = json.load(io.open(p, encoding='utf-8'))
    except Exception:
        continue
    if 'onEnableShakes' not in d and 'sounds' not in d:
        continue
    sounds = d.get('sounds') or []
    cues = []
    for s in sounds:
        if not isinstance(s, dict):
            continue
        snd = s.get('sound')
        if isinstance(snd, dict):
            pid = snd.get('m_PathID')
            cues.append(by_pid.get(pid, 'PID:%s' % pid))
        elif isinstance(snd, str):
            cues.append(snd)
    shakes = d.get('onEnableShakes') or []
    hits.append((os.path.basename(p), len(shakes), cues))

print('带 ShakeAndSoundObject 的组件 =', len(hits))
allc = []
for f, ns, cues in hits:
    allc.extend([c for c in cues if c])
    print(' %-46s shakes=%d cues=%s' % (f, ns, cues[:4]))

SEVENTEEN = ['Asteroid Zone', 'Daemonic Feast', 'Dawn Attack', 'Everstorm', 'Factory Overdrive',
             'Firestorm Loop', 'First Light', 'Full Moon', 'Immortal Beams', 'Orbital Initial',
             'Orks Dust Storm Start', 'Star Orbit', 'Sump Overspill', 'Tau Electro-Static Interference',
             'Toxic Fumes Start', 'Warp Storm', 'Webway Rift']
found = [c for c in SEVENTEEN if c in set(allc)]
print()
print('17 条环境 cue 里，被这 35 个组件引用的 =', len(found), found)
