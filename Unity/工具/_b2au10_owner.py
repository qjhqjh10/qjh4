# -*- coding: utf-8 -*-
"""把 17 条命中里的 ShakeAndSoundObject 组件 → 归属 GameObject（扫 m_Component 反查）。只读。"""
import io, json, os

B = 'D:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all'
MB = os.path.join(B, 'MonoBehaviour')
GO = os.path.join(B, 'GameObject')

MB_PIDS = {
 -147466137670212988: 'Asteroid Zone', 1120738079306386448: 'Daemonic Feast',
 7964408226216882434: 'Dawn Attack', 6927682770757828708: 'Everstorm',
 -3456741993900530059: 'Factory Overdrive', -5489657316863698205: 'Factory Overdrive(2)',
 6115132746584211042: 'Firestorm Loop', -6203148498290487414: 'First Light',
 -3053380746057160738: 'Full Moon', 7709385406679583499: 'Immortal Beams',
 -8940324439209517681: 'Orbital Initial', 3545334214658408379: 'Orks Dust Storm Start',
 -3361632299650125935: 'Star Orbit', 2909568355852927222: 'Sump Overspill',
 -4431462062045255759: 'Tau Electro-Static Interference', -5417358138418159934: 'Toxic Fumes Start',
 -8527852838950854796: 'Toxic Fumes Start(2)', -8389605308484142882: 'Warp Storm',
 -123650712261992429: 'Webway Rift',
}
want = set(str(p) for p in MB_PIDS)
found = {}
for f in os.listdir(GO):
    if not f.endswith('.json'):
        continue
    p = os.path.join(GO, f)
    try:
        t = io.open(p, encoding='utf-8', errors='ignore').read()
    except Exception:
        continue
    if 'm_Component' not in t:
        continue
    for pid in want:
        if '"m_PathID": ' + pid in t or '"m_PathID":' + pid in t:
            found.setdefault(int(pid), f[:-5])

for pid, who in MB_PIDS.items():
    print('%-34s -> %s' % (who, found.get(pid, '（未找到 GameObject）')))
print()
print('解析出归属 GameObject =', len(found), '/', len(MB_PIDS))
