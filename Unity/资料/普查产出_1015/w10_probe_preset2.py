# -*- coding: utf-8 -*-
"""W10 只读探针②：确认 `CAB-da1bb534be23f0576b2df09a41d87665` 落在哪个 bundle、
pid -8043713765525655674 是什么。⛔ 只读。"""
import os
import sys

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
import UnityPy

AA = 'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
TARGET_CAB = 'CAB-da1bb534be23f0576b2df09a41d87665'
TARGET_PID = -8043713765525655674

for b in ('tweenandshakes_assets_all.bundle', 'battleprefabs_vfxandmisc_assets_all.bundle'):
    p = os.path.join(AA, b)
    if not os.path.isfile(p):
        print('缺 %s' % p)
        continue
    env = UnityPy.load(p)
    for _outer, bf in env.files.items():
        for cab, sf in getattr(bf, 'files', {}).items():
            objs = getattr(sf, 'objects', None)
            if not isinstance(objs, dict):
                continue
            print('%s → CAB %s：%d 个对象；目标 CAB? %s' % (b, cab, len(objs), cab == TARGET_CAB))
            if cab == TARGET_CAB:
                o = objs.get(TARGET_PID)
                print('   pid %s → %s' % (TARGET_PID, o))
                if o is not None:
                    d = o.read_typetree()
                    print('   type=%s  m_Name=%r' % (o.type.name, d.get('m_Name')))
                    print('   keys=%s' % list(d.keys()))
                else:
                    # 看看 pid 的符号形式
                    print('   负号试 %s → %s' % (abs(TARGET_PID), objs.get(abs(TARGET_PID))))
                    allp = list(objs.keys())
                    print('   pid 样本 %s … %s' % (allp[:5], allp[-5:]))
