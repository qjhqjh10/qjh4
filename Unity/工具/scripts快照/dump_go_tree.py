#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
dump_go_tree.py — 从原始 Unity JSON 提取指定根 GameObject 的完整子树布局
来源: 解包目录 (03_界面UI/菜单/ 或 07_场景/<scene>/)
用法: py312 dump_go_tree.py <解包目录> <根GO名|根GO PathID> [最大深度] [输出文件]
输出: 缩进树 — GO名 [锚点min/max pos size pivot] 组件摘要 (Image sprite名/Text文字/按钮)
      每行末尾标注原始 JSON 文件路径, 坐标数值直接来自 m_AnchoredPosition/m_SizeDelta (1920x1080 可直接用)
"""
import json
import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

SRC = sys.argv[1] if len(sys.argv) > 1 else 'D:/2/解包整理/03_界面UI/菜单'
ROOT_NAME = sys.argv[2] if len(sys.argv) > 2 else 'Deck Editing Menu'
MAX_DEPTH = int(sys.argv[3]) if len(sys.argv) > 3 else 99
OUT = sys.argv[4] if len(sys.argv) > 4 else None

_cache = {}
_index = None  # pathid -> {type, path, data}
_name_idx = None  # name -> [pathid]

TYPES = ['GameObject', 'RectTransform', 'Transform', 'MonoBehaviour', 'Sprite', 'Texture2D']


def _build_index():
    global _index, _name_idx
    _index, _name_idx = {}, {}
    for t in TYPES:
        td = os.path.join(SRC, t)
        if not os.path.isdir(td):
            continue
        for f in os.listdir(td):
            if not f.endswith('.json'):
                continue
            m = f.rsplit('_', 1)
            pid = None
            if len(m) == 2 and m[1].replace('.json', '').lstrip('-').isdigit():
                pid = int(m[1].replace('.json', ''))
            try:
                d = json.load(open(os.path.join(td, f), encoding='utf-8'))
            except Exception:
                continue
            if pid is not None and pid not in _index:
                _index[pid] = {'type': t, 'path': os.path.join(td, f), 'data': d}
            nm = d.get('m_Name')
            if isinstance(nm, str) and nm:
                _name_idx.setdefault(nm, []).append(pid)


def load(t, pid):
    key = (t, pid)
    if key in _cache:
        return _cache[key]
    # pathid 全局唯一, 按 pid 查索引 (不限定类型目录)
    e = _index.get(pid)
    if e is not None:
        _cache[key] = e['data']
        return e['data']
    _cache[key] = None
    return None


# ---- 跨 bundle sprite 补充索引 (ui_extract 提取目录) ----
_extra_spr = None


def _load_extra_sprites():
    global _extra_spr
    if _extra_spr is not None:
        return _extra_spr
    _extra_spr = {}
    base = 'D:/2/Warpforge_tools/data/ui_extract/'
    if not os.path.isdir(base):
        return _extra_spr
    for bundle in os.listdir(base):
        sdir = os.path.join(base, bundle, 'Sprite')
        if not os.path.isdir(sdir):
            continue
        for f in os.listdir(sdir):
            if not f.endswith('.json'):
                continue
            try:
                d = json.load(open(os.path.join(sdir, f), encoding='utf-8'))
            except Exception:
                continue
            pid = d.get('pathid')
            if pid is not None:
                _extra_spr[pid] = d.get('m_Name') or '?'
    return _extra_spr


def go_name(pid):
    d = load('GameObject', pid)
    return d['m_Name'] if d else '?%d' % pid


def go_components(pid):
    d = load('GameObject', pid)
    if not d:
        return []
    return [c['component']['m_PathID'] for c in d.get('m_Component', [])]


def rect_str(rt):
    if not rt:
        return ''
    a = rt.get('m_AnchoredPosition') or {}
    s = rt.get('m_SizeDelta') or {}
    return '[pos(%s,%s) size(%s,%s) anchor(%s,%s,%s,%s) pivot(%s,%s)]' % (
        _f(a.get('x')), _f(a.get('y')), _f(s.get('x')), _f(s.get('y')),
        _f((rt.get('m_AnchorMin') or {}).get('x')), _f((rt.get('m_AnchorMin') or {}).get('y')),
        _f((rt.get('m_AnchorMax') or {}).get('x')), _f((rt.get('m_AnchorMax') or {}).get('y')),
        _f((rt.get('m_Pivot') or {}).get('x')), _f((rt.get('m_Pivot') or {}).get('y')))


def godot_rect_str(rt):
    """Unity RectTransform → Godot Control 属性 (1920x1080 参考分辨率下直接等价)
    Unity: anchorMin/Max + anchoredPosition(相对锚点) + sizeDelta + pivot
    Godot: anchor_left/right/top/bottom + offset_left/right/top/bottom
    公式: offset_left  = ap.x - sd.x*pivot.x   (点锚点: 锚点同一位置, 相对锚点偏移)
          offset_right = ap.x + sd.x*(1-pivot.x)
          offset_top   = ap.y - sd.y*pivot.y
          offset_bottom= ap.y + sd.y*(1-pivot.y)
    拉伸锚点同理 (offset 是相对锚点区边缘的偏移)"""
    if not rt:
        return ''
    amin = rt.get('m_AnchorMin') or {}
    amax = rt.get('m_AnchorMax') or {}
    a = rt.get('m_AnchoredPosition') or {}
    s = rt.get('m_SizeDelta') or {}
    p = rt.get('m_Pivot') or {}
    ax, ay = amin.get('x', 0), amin.get('y', 0)
    bx, by = amax.get('x', 1), amax.get('y', 1)
    apx, apy = a.get('x', 0), a.get('y', 0)
    sdx, sdy = s.get('x', 0), s.get('y', 0)
    px, py = p.get('x', 0.5), p.get('y', 0.5)
    ol = apx - sdx * px
    orr = apx + sdx * (1 - px)
    ot = apy - sdy * py
    ob = apy + sdy * (1 - py)
    return 'anchors(%s,%s,%s,%s) offsets(%s,%s,%s,%s)' % (
        round(ax, 3), round(ay, 3), round(bx, 3), round(by, 3),
        round(ol, 1), round(orr, 1), round(ot, 1), round(ob, 1))


def _f(v):
    if v is None:
        return '?'
    return round(v, 1)


def comp_summary(pid, depth):
    """组件摘要: 找 RT 与 Image/Text/Button 特征"""
    d = load('MonoBehaviour', pid)
    if d is None:
        return ''
    keys = set(d.keys())
    parts = []
    if 'm_AnchoredPosition' in keys or 'm_Father' in keys:
        parts.append('RT' + rect_str(d))
    if 'm_Sprite' in keys:
        sp = d.get('m_Sprite') or {}
        spid = sp.get('m_PathID')
        spname = ''
        if spid is not None:
            sd = load('Sprite', spid)
            if sd:
                spname = sd.get('m_Name', '')
                # Sprite rect
                r = sd.get('m_Rect') or {}
                if r:
                    spname += ' rect(%s,%s,%s,%s)' % (round(r.get('x', 0), 1), round(r.get('y', 0), 1),
                                                      round(r.get('width', 0), 1), round(r.get('height', 0), 1))
            else:
                spname = _load_extra_sprites().get(spid, '?')
        parts.append('Image->' + spname)
    if 'm_text' in keys:
        txt = str(d.get('m_text', ''))[:60]
        parts.append('Text:%r' % txt)
    if 'm_Interactable' in keys:
        parts.append('Button')
    if 'm_fontSize' in keys:
        parts.append('fontsize=%s' % d.get('m_fontSize'))
    if 'm_Color' in keys:
        c = d.get('m_Color') or {}
        parts.append('color(%s,%s,%s,%s)' % (round(c.get('r', 0), 2), round(c.get('g', 0), 2),
                                             round(c.get('b', 0), 2), round(c.get('a', 0), 2)))
    return ' | '.join(parts)


def walk(pid, depth, lines, prefix=''):
    if depth > MAX_DEPTH or pid is None:
        return
    d = load('GameObject', pid)
    if d is None:
        return
    name = d.get('m_Name', '?')
    comps = go_components(pid)
    rt_pid = None
    for c in comps:
        cd = load('MonoBehaviour', c)
        if cd and 'm_AnchoredPosition' in cd:
            rt_pid = c
            break
    rt_info = ''
    rtf = ''
    g_rt = ''
    if rt_pid is not None:
        cd = load('MonoBehaviour', rt_pid)
        rt_info = rect_str(cd)
        g_rt = godot_rect_str(cd)
        e = _index.get(rt_pid)
        if e:
            rtf = os.path.relpath(e['path'], SRC)
    comps_str = []
    for c in comps:
        cs = comp_summary(c, depth)
        if cs:
            comps_str.append(cs)
    gof = ''
    e = _index.get(pid)
    if e:
        gof = os.path.relpath(e['path'], SRC)
    lines.append('%s- %s %s %s%s%s <%s> <%s>' % (
        prefix, name, rt_info, g_rt,
        ' active' if not d.get('m_IsActive', True) else '',
        (' :: ' + '; '.join(comps_str)) if comps_str else '',
        gof, rtf))
    # 子节点: m_Children 是子 RectTransform 的 pid → 子 RT.m_GameObject 才是子 GO pid
    if rt_pid is not None:
        rt = load('MonoBehaviour', rt_pid)
        for ch in rt.get('m_Children', []):
            ch_rt = load('RectTransform', ch.get('m_PathID'))
            child_go = None
            if ch_rt:
                child_go = (ch_rt.get('m_GameObject') or {}).get('m_PathID')
            if child_go is not None:
                walk(child_go, depth + 1, lines, prefix + '  ')


def find_root_pid():
    """根 GO: 按名字 → 优先非副本 (优先取文件名无 _<pid> 后缀的主文件对应的 pid)"""
    pids = _name_idx.get(ROOT_NAME, [])
    if not pids:
        # 尝试数字
        try:
            pid = int(ROOT_NAME)
            return pid
        except ValueError:
            return None
    # 取第一个(主文件优先: 索引顺序按目录扫描, 副本在最后? 不可靠 → 取有父的排除: 根 GO 无父)
    for pid in pids:
        comps = go_components(pid)
        for c in comps:
            cd = load('MonoBehaviour', c)
            if cd and 'm_AnchoredPosition' in cd:
                f = (cd.get('m_Father') or {}).get('m_PathID')
                if f in (None, 0):
                    return pid
    return pids[0]


def main():
    _build_index()
    pid = find_root_pid()
    if pid is None:
        print('未找到根 GO: %s' % ROOT_NAME)
        print('同名 GO: ', list(_name_idx.keys())[:50])
        sys.exit(1)
    lines = []
    walk(pid, 0, lines)
    text = '\n'.join(lines)
    if OUT:
        open(OUT, 'w', encoding='utf-8').write(text)
        print('已输出 %d 行 → %s' % (len(lines), OUT))
    else:
        print(text)


if __name__ == '__main__':
    main()
