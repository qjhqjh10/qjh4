#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""read_gradient2_level0.py — 从原版 `level0` 的**原始字节**里读出 `Image` + `Gradient2` 的字段。

为什么要这个工具（2026-10-03 立）
--------------------------------
原版 `level0` 是**没有 type tree 的内置文件**，解包时那两条组件（`Image` + `Gradient2`）**整条被跳过**
（`assets_full/level0/MonoBehaviour/` 里根本没有它们 —— 例：`Smooth background fade Left` 的 4 个组件
pid = 282/273/**309/310**，而 309/310 **目录里没有对应文件**）。
🔴 但 **UnityPy 其实读得到它们的 `get_raw_data()`** —— 卡住的只是「不知道字段顺序」。

**字段顺序的钥匙**：拿一份**已导出成功**的 `Gradient2` JSON 当样板 ——
`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-7858062199064615340.json`
（它的字段名与顺序：`_gradientType` `_blendMode` `_modifyVertices` `_offset` `_zoom` `_effectGradient`）。
⇒ 两边的**字段声明顺序**必然相同（同一个类），于是原始字节可以直接按那个顺序切开。

判定「哪条 MB 是 `Gradient2` / `Image`」的办法：`m_Script` 的 PPtr pathID ——
**`1277` = `UnityEngine.UI.Extensions.Gradient2` · `4528` = `UnityEngine.UI.Image`**（本 build 实测）。

用法
----
    python 工具/read_gradient2_level0.py                      # 列全部 4 条 `Smooth background fade *`
    python 工具/read_gradient2_level0.py "Smooth background fade Left"

⚠️ 只读（铁律 2：`d:/2` 是只读档案库）。结果 → `项目任务.md` §三第29条 A18。
"""
import io
import json
import os
import struct
import sys

import UnityPy

LEVEL0 = 'D:/2/unity_run_ref/Warpforge_Data/level0'          # 原始序列化文件（只读）
BASE = 'D:/2/新解包资源/assets_full/level0'                   # 同一次解包的产出（用来查 GO 名 → 组件 pid）
SCRIPT_GRADIENT2 = 1277
SCRIPT_IMAGE = 4528

# ---- 样板（字段顺序的来源）----
SAMPLE = ('D:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/'
          'MonoBehaviour_-7858062199064615340.json')

# MB 头：m_GameObject{fileID(i32)+pathID(i64)} + m_Enabled(i32) + m_Script{fileID(i32)+pathID(i64)} + m_Name(对齐串)
MB_HEAD = 12 + 4 + 12 + 4


def script_pid(raw):
    return struct.unpack_from('<q', raw, 20)[0]


def decode_gradient2(raw):
    """按样板那份的顺序切：三个 int / 两个 float / 8 个 Color / 8×ctime / 8×atime / mode(u16) / 两个 byte。"""
    o = MB_HEAD
    gt, bm, mv = struct.unpack_from('<iii', raw, o); o += 12
    off, zoom = struct.unpack_from('<2f', raw, o); o += 8
    keys = [struct.unpack_from('<4f', raw, o + 16 * i) for i in range(8)]; o += 128
    ctime = struct.unpack_from('<8H', raw, o); o += 16
    atime = struct.unpack_from('<8H', raw, o); o += 16
    mode = struct.unpack_from('<H', raw, o)[0]; o += 2
    nck, nak = struct.unpack_from('<2B', raw, o)
    return dict(type=gt, blend=bm, modify=mv, offset=off, zoom=zoom,
                keys=keys, ctime=ctime, atime=atime, mode=mode, nck=nck, nak=nak)


def decode_image(raw):
    """`Graphic` 先来：`m_Material`(PPtr 12) + `m_Color`(Color 16) + …"""
    mat = raw[MB_HEAD:MB_HEAD + 12]
    col = struct.unpack_from('<4f', raw, MB_HEAD + 12)
    return mat, col


def main():
    want = sys.argv[1] if len(sys.argv) > 1 else None
    print('样板（字段顺序来源）:', os.path.basename(SAMPLE))
    try:
        s = json.load(io.open(SAMPLE, encoding='utf-8'))
        print('  样板的字段顺序:', [k for k in s if not k.startswith('m_')])
    except Exception as e:
        print('  ⚠️ 样板读不到（%r）—— 顺序仍按本脚本里写死的那套' % e)

    env = UnityPy.load(LEVEL0)
    f = list(env.files.values())[0]

    names = {}
    gdir = os.path.join(BASE, 'GameObject')
    for fn in os.listdir(gdir):
        if not fn.startswith('Smooth background fade'):
            continue
        g = json.load(io.open(os.path.join(gdir, fn), encoding='utf-8'))
        names[g['m_Name']] = [c['component']['m_PathID'] for c in g['m_Component']]

    for nm in sorted(names):
        if want and want not in nm:
            continue
        print('\n===', nm, '组件 pid =', names[nm])
        for pid in names[nm]:
            ob = f.objects.get(pid)
            if ob is None or ob.type.name != 'MonoBehaviour':
                continue
            raw = ob.get_raw_data()
            sp = script_pid(raw)
            if sp == SCRIPT_GRADIENT2:
                d = decode_gradient2(raw)
                print('  Gradient2: _gradientType=%d _blendMode=%d _modifyVertices=%d _offset=%g _zoom=%g'
                      % (d['type'], d['blend'], d['modify'], d['offset'], d['zoom']))
                print('    m_Mode=%d numColorKeys=%d numAlphaKeys=%d' % (d['mode'], d['nck'], d['nak']))
                for i in range(d['nck'] or 0):
                    print('    colorKey%d rgba=(%g, %g, %g, %g)  time=%d' % (i, *d['keys'][i], d['ctime'][i]))
                for i in range(d['nak'] or 0):
                    print('    alphaKey%d a=%g  time=%d (%.3f)'
                          % (i, d['keys'][i][3], d['atime'][i], d['atime'][i] / 65535.0))
            elif sp == SCRIPT_IMAGE:
                mat, col = decode_image(raw)
                print('  Image: m_Material=%s  m_Color=(%g, %g, %g, %g)'
                      % (mat.hex(), col[0], col[1], col[2], col[3]))


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    main()
