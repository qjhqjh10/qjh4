#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""read_tmp_level0.py — 从原版 `level0` 的**原始字节**里读出 TMP（`TextMeshProUGUI`）组件的字段。

为什么要这个工具（2026-10-17 · `A1213④`）
----------------------------------------
`level0` 是**没有 type tree 的内置序列化文件**，解包器把 `TextMeshProUGUI` 那几条**整条跳过**
（`assets_full/level0/GameObject/{Loading text,Progress text}.json` 里组件 pid 还在 ——
`296/298` 与 `295/297` —— 但 `assets_full/level0/MonoBehaviour/` 目录里 **295–298 全缺**，
类型树缺失 ⇒ 导出器跳过）。
而**判据（`m_fontSize*` / 折行 / 对齐 / 颜色）恰恰只在那几条组件里** —— 不能靠猜。

字段顺序从哪来
--------------
**不手切**：本机有**带 type tree 的同类型资产**（同一个 `TextMeshProUGUI` 类），
直接把它的**序列化类型树**借过来喂给 `ObjectReader.read_typetree(nodes)`：
  · 样板 = `bundle_battleprefabs_vfxandmisc_assets_all` 里的
    `MonoBehaviour_5890981294211439552`（即原版 `DamageText` 那颗 TMP）；
  · `read_typetree` 默认 `check_read=True` —— 读不满 / 读超都会抛 ⇒ **解码结果可自证**；
  · 再拿 `m_text` 交叉核对（`Loading text → "Loading..."`、`Progress text → "0%"`）——
    两串对上了才说明字段没串位。

⚠️ 先例 = `工具/read_gradient2_level0.py`（同一套「原始 level0 + 借用样板」的路子，只是那份是手切的）；
⛔ 别把这份写成「按固定偏移硬切」—— TMP 的字段多且嵌套，硬切一个字节错位就静默读出垃圾。

用法
----
    python 工具/read_tmp_level0.py                  # 读 `Loading text` / `Progress text` 两条
    python 工具/read_tmp_level0.py --pid 298        # 读指定组件 pid（可给多个）
    python 工具/read_tmp_level0.py --legacy-slice   # 保留口（说明用；见 main 里那句）

⚠️ **只读**（铁律 2：`d:/2` 是只读档案库）。结果 → `资料/普查产出_1017/UI/W_A1213批.md`。
"""
import io
import json
import os
import struct
import sys

import UnityPy

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法\n----\n    python 工具/read_tmp_level0.py                  # 读 `Loading text` / `Progress text` 两条\n    python 工具/read_tmp_level0.py --pid 298        # 读指定组件 pid（可给多个）\n    python 工具/read_tmp_level0.py --legacy-slice   # 保留口（说明用；见 main 里那句）\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

LEVEL0 = 'D:/2/unity_run_ref/Warpforge_Data/level0'          # 原始序列化文件（只读）
GGM = 'D:/2/unity_run_ref/Warpforge_Data/globalgamemanagers.assets'   # level0 的 externals[0]（MonoScript 在这）
BASE = 'D:/2/新解包资源/assets_full/level0'                   # 同一次解包的产出（GO JSON：名字 → 组件 pid）
TEMPLATE_BUNDLE = ('D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/'
                   'battleprefabs_vfxandmisc_assets_all.bundle')
TEMPLATE_PID = 5890981294211439552                            # 原版 `DamageText` 的 TMP（同类型样板）

# 关心的字段（顺序只影响打印）
FIELDS = ['m_text', 'm_fontSize', 'm_fontSizeMin', 'm_fontSizeMax', 'm_fontSizeBase',
          'm_enableAutoSizing', 'm_TextWrappingMode', 'm_HorizontalAlignment',
          'm_VerticalAlignment', 'm_fontColor']

# 默认目标（`A1213④` 那两条）
DEFAULT_GO = ['Loading text', 'Progress text']


def script_of(raw):
    """MB 头：m_GameObject{int,int64} + m_Enabled(int, padded) + m_Script{int,int64} + m_Name(string)"""
    return struct.unpack_from('<i', raw, 16)[0], struct.unpack_from('<q', raw, 20)[0]


def goto_components():
    """GO 名 → 组件 pid 列表（来自已经导出成功的 GameObject JSON）。"""
    out = {}
    gdir = os.path.join(BASE, 'GameObject')
    if not os.path.isdir(gdir):
        return out
    for fn in os.listdir(gdir):
        if not fn.endswith('.json'):
            continue
        p = os.path.join(gdir, fn)
        try:
            g = json.load(io.open(p, encoding='utf-8'))
        except Exception:
            continue
        if g.get('m_Name') in DEFAULT_GO:
            out[g['m_Name']] = [c['component']['m_PathID'] for c in g.get('m_Component', [])]
    return out


def mono_class_map():
    """globalgamemanagers.assets 里 MonoScript 的 pathID → 类名（只建 TextMeshPro* 与小体积 MB 那几条）。"""
    m = {}
    try:
        env = UnityPy.load(GGM)
        f = list(env.files.values())[0]
    except Exception as e:
        print('⚠️ 读不到 `%s`（%r）⇒ 组件类名不判，按 TMP 直接解' % (GGM, e))
        return m
    for pid, ob in f.objects.items():
        if ob.type.name != 'MonoScript':
            continue
        try:
            raw = ob.get_raw_data()
        except Exception:
            continue
        parts = [p.decode('latin1') for p in raw.split(b'\x00')
                 if len(p) > 2 and all(32 <= c < 127 for c in p)]
        # MonoScript：m_Name(资产名) / m_ClassName / m_Namespace / m_AssemblyName
        if len(parts) >= 2:
            m[pid] = parts[0]
    return m


def borrow_typetree():
    """从样板 bundle 里借 `TextMeshProUGUI` 的序列化类型树。"""
    env = UnityPy.load(TEMPLATE_BUNDLE)
    b = list(env.files.values())[0]
    f = list(b.files.values())[0]
    ob = f.objects[TEMPLATE_PID]
    return ob.serialized_type.node


def dump_obj(ob, tree, name):
    raw = ob.get_raw_data()
    sf, sp = script_of(raw)
    print('\n=== %s（pid %d · %d 字节 · m_Script fileID=%d pathID=%d）'
          % (name, ob.path_id, len(raw), sf, sp))
    try:
        d = ob.read_typetree(tree)          # check_read 默认 True ⇒ 读不满/读超都会抛
    except Exception as e:
        print('  ❌ 读不出：%s: %s' % (type(e).__name__, e))
        print('     ⇒ 卡在「借来的类型树与这条组件对不上」（类不同 / 版本不同）—— ⛔ 别猜值')
        return
    for k in FIELDS:
        if k not in d:
            print('  ⚠️ 类型树里没有 `%s`（这条组件可能不是 TMP）' % k)
            continue
        v = d[k]
        print('  %-22s = %s' % (k, json.dumps(v, ensure_ascii=False)))
    extra = [k for k in d if k not in FIELDS]
    print('  （另 %d 个字段已成功解码 —— 类型树吃满了整条字节流）' % len(extra))


def main():
    args = sys.argv[1:]
    if '--legacy-slice' in args:
        print('说明：本工具**不再**按固定偏移手切 —— 见文件头「字段顺序从哪来」。'
              '借类型树 + `check_read` 是同一件事的自证版。')
        args = [a for a in args if a != '--legacy-slice']

    tree = borrow_typetree()
    print('样板类型树：%s（pid %d）' % (os.path.basename(TEMPLATE_BUNDLE), TEMPLATE_PID))

    env = UnityPy.load(LEVEL0)
    f = list(env.files.values())[0]
    print('level0：%d 个对象' % len(f.objects))

    pids = []
    if '--pid' in args:
        i = args.index('--pid')
        pids = [int(x) for x in args[i + 1:] if not x.startswith('-')]
    else:
        comps = goto_components()
        for nm in DEFAULT_GO:
            for pid in comps.get(nm, []):
                pids.append((nm, pid))

    classes = mono_class_map()
    for item in pids:
        if isinstance(item, tuple):
            nm, pid = item
        else:
            nm, pid = 'pid %d' % item, item
        ob = f.objects.get(pid)
        if ob is None:
            print('\n=== %s（pid %d）—— level0 里没有这个对象' % (nm, pid))
            continue
        if ob.type.name != 'MonoBehaviour':
            print('\n=== %s（pid %d）—— 不是 MonoBehaviour（%s），跳过' % (nm, pid, ob.type.name))
            continue
        sf, sp = script_of(ob.get_raw_data())
        cls = classes.get(sp, '?')
        # 先把组件类名印出来：`load_typetree` 只在 TextMeshPro* 上有意义
        if not str(cls).startswith('TextMeshPro'):
            # 小体积 MB（如 `EverguildTextController`，40 字节）不是 TMP —— 如实报，别硬解
            print('\n=== %s（pid %d · %d 字节）—— 组件类 `%s`，不是 TMP，略过'
                  % (nm, pid, ob.byte_size, cls))
            continue
        dump_obj(ob, tree, '%s · %s' % (nm, cls))


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    main()
