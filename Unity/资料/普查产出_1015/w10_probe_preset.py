# -*- coding: utf-8 -*-
"""W10 只读探针：场景侧那颗 `AnimFXModuleScreenShake`（tauviorla pid 5320）的
`manualTriggerCameraShakes[0].presetSO` 这条引用落在哪个包、叫什么名字。

⛔ 只读，不写任何工程文件。
"""
import io
import os
import sys
import json

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
import UnityPy

AA = 'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
BUNDLE = 'scenes_scenes_battlearenatauviorla.bundle'

env = UnityPy.load(os.path.join(AA, BUNDLE))
print('顶层 files:', list(env.files.keys()))
cab_ext = {}
by_cab = {}
for _outer, bf in env.files.items():
    for cab, sf in getattr(bf, 'files', {}).items():
        objs = getattr(sf, 'objects', None)
        if not isinstance(objs, dict):
            continue
        cab_ext[cab] = [getattr(x, 'path', '') or '' for x in (getattr(sf, 'externals', None) or [])]
        by_cab[cab] = objs
        print('CAB %s：%d 个对象，%d 条 externals' % (cab, len(objs), len(cab_ext[cab])))

# 找 pid 5320 在哪份 CAB
for cab, objs in by_cab.items():
    if 5320 in objs:
        print('pid 5320 在 CAB', cab)
        ext = cab_ext[cab]
        for i, e in enumerate(ext):
            print('  externals[%d] (=> m_FileID %d) = %s' % (i, i + 1, e))
        o = objs[5320]
        d = o.read_typetree()
        print('pid 5320 typetree keys:', list(d.keys()))
        print('  actionStart =', d.get('actionStart'))
        print('  cameraShakes =', json.dumps(d.get('cameraShakes'), ensure_ascii=False))
        m0 = (d.get('manualTriggerCameraShakes') or [{}])[0]
        print('  manualTriggerCameraShakes[0] =', json.dumps(m0, ensure_ascii=False))
        ref = m0.get('presetSO') or {}
        fid = ref.get('m_FileID') or 0
        print('  presetSO m_FileID=%s m_PathID=%s' % (fid, ref.get('m_PathID')))
        if fid:
            print('  => 目标 CAB =', ext[fid - 1])
        # 也把 m_Script 的 class 名打出来确认
        sc = (d.get('m_Script') or {})
        print('  m_Script =', sc)
        break

    # 顺便：这条引用能不能在本包内解出来？
# 直接试 UnityPy 的 PPtr.read()
for cab, objs in by_cab.items():
    o = objs.get(5320)
    if o is None:
        continue
    d = o.read()
    arr = getattr(d, 'manualTriggerCameraShakes', None)
    if arr:
        e0 = arr[0]
        pp = getattr(e0, 'presetSO', None)
        print('UnityPy PPtr.read() 试解：', pp)
        try:
            tgt = pp.read()
            print('  ->', type(tgt).__name__, getattr(tgt, 'm_Name', None))
        except Exception as ex:  # noqa: BLE001
            print('  -> 解不出：%r' % (ex,))
    break
