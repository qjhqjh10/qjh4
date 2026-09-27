#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_arena_camera.py — 把**原版战场相机的光学参数**从场景包里抽出来，写成旁挂 JSON。

为什么要有它（2026-09-27 查明，判据见下）：
    `ArenaBuilder.ConfigureBoardCamera` 原来把 `sensorSize` **写死 (41.5, 24)**，
    而原版 **13 台 `BoardCamera` 里 `m_SensorSize.x` 并不一致** ——
    `emperorschildren` / `genestealers` / `spacewolves` 三台是 **37.2**，其余 10 台是 41.5。
    `m_GateFitMode = 2 (Horizontal)` 下 `sensorSize.x` 直接决定 hFOV（vFOV 按画幅比例联动）
    ⇒ 那三场被**等比放大 10.36%**（`0.37366 / 0.41685 = 0.8964`）——
    这正是文档里记了很久的「**这三场的原版实拍图不能用、与原版授权取景差 ~10%**」的真因。
    🔴 **那条结论已作废**：原版实拍图是好的，**是我们的相机写错了**（详见任务文件 §三 第 3 条 ⑥）。

为什么不改 `gen_unity_arena_manifest.py`：
    那是 `d:/2/Warpforge_tools/scripts/` 下的生成器（**档案库**，动它要单独确认）；
    而本工程早有一套既成做法 = **旁挂数据 + `工具/gen_arena_*.py`**
    （非主贴图槽 / 精灵 / 平面反射 / 画质档开关 / 粒子网格，全走这条）⇒ 照办。

用法：
    python d:/4/Unity/工具/gen_arena_camera.py            # 13 场全写
    python d:/4/Unity/工具/gen_arena_camera.py --arena battlearenaspacewolves

产物：`<工程>/WarpforgeArena1/arenas/<场>/<场>_camera.json`

自检（跑完怎么知道没跑偏）：
    · **13 场都要有产物**，且 `gateFit` 全是 **2**、`focalLength` 全是 **28**、`sensorSizeY` 全是 **24**；
    · `sensorSizeX` 应当**只有两个取值**：**37.2（三场）与 41.5（十场）** ——
      数目不对就是抽取口径错了（比如挑错了相机：这一场里不止一台 `Camera`，
      **判据 = `m_Depth = −1` 那一台**）。
"""
import argparse
import glob
import io
import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8', errors='replace')

ASSETS = 'd:/2/新解包资源/assets_full'
OUT_ROOT = 'd:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas'

# 判据：原版 `BoardCamera` 是**唯一 `m_Depth = −1`** 的那台（其余是 UI/Overlay/HUD 相机）
WANT_DEPTH = -1


def find_camera_json(arena):
    """在该场的场景包里找 `m_Depth = −1` 那台 `Camera`，返回 (路径, 反序列化后的 dict)。"""
    pat = os.path.join(ASSETS, 'bundle_scenes_scenes_%s' % arena, 'Camera', '*.json')
    hits = []
    for f in sorted(glob.glob(pat)):
        try:
            t = json.load(io.open(f, encoding='utf-8'))
        except Exception:
            continue
        if t.get('m_Depth') == WANT_DEPTH:
            hits.append((f, t))
    if not hits:
        return None, None
    if len(hits) > 1:
        # 真出现两台就**出声**（不许静默挑一台）
        print('  ⚠️ %s：`m_Depth=-1` 的 Camera 有 %d 台 ⇒ 取文件名字典序第一台，人工核一下'
              % (arena, len(hits)))
    return hits[0]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena', default=None, help='只做这一场（缺省 = 13 场全做）')
    ap.add_argument('--out-root', default=OUT_ROOT)
    a = ap.parse_args()

    arenas = ([a.arena] if a.arena else
              sorted(os.path.basename(p).replace('bundle_scenes_scenes_', '', 1)
                     for p in glob.glob(os.path.join(ASSETS, 'bundle_scenes_scenes_battlearena*'))))
    n_ok = 0
    xs = {}
    for arena in arenas:
        f, t = find_camera_json(arena)
        if t is None:
            print('  ✗ %-32s 找不到 `m_Depth=-1` 的 Camera（搜过 %s）' % (arena, 'bundle_scenes_scenes_%s/Camera' % arena))
            continue
        ss = t.get('m_SensorSize') or {}
        d = {
            'arena':        arena,
            'sensorSizeX':  round(float(ss.get('x', 0.0)), 6),
            'sensorSizeY':  round(float(ss.get('y', 0.0)), 6),
            'focalLength':  round(float(t.get('m_FocalLength', 0.0)), 6),
            'gateFit':      int(t.get('m_GateFitMode', -1)),
            'fov':          round(float(t.get('m_FieldOfView', 0.0)), 6),
            'lensShiftX':   round(float((t.get('m_LensShift') or {}).get('x', 0.0)), 6),
            'lensShiftY':   round(float((t.get('m_LensShift') or {}).get('y', 0.0)), 6),
            '_source':      os.path.relpath(f, ASSETS).replace('\\', '/'),
        }
        out = os.path.join(a.out_root, arena, '%s_camera.json' % arena)
        os.makedirs(os.path.dirname(out), exist_ok=True)
        with io.open(out, 'w', encoding='utf-8', newline='\n') as fh:
            fh.write(json.dumps(d, ensure_ascii=False, indent=2) + '\n')
        xs.setdefault(d['sensorSizeX'], []).append(arena)
        n_ok += 1
        print('  ✓ %-32s sensorSize.x=%-7s gateFit=%s focal=%s' % (arena, d['sensorSizeX'], d['gateFit'], d['focalLength']))

    print('\n写出 %d 份' % n_ok)
    print('sensorSizeX 分布：')
    for v in sorted(xs):
        print('   %-8s × %d 场：%s' % (v, len(xs[v]), ', '.join(xs[v])))
    if len(xs) > 1:
        print('⚠️ 两个取值 ⇒ 这正是「三场与原版差 ~10%」的根因（见文件头）。**别把它当成抽错了。**')


if __name__ == '__main__':
    main()
