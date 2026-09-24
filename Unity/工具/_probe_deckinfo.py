#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""_probe_deckinfo.py — 把一个 prefab 节点的 **MonoBehaviour 序列化字段** 逐条解出来。

为什么（2026-09-24）：`DeckGeneralInfoDemo` 的 `Toggle()` 是**两个抽屉互斥**，
但「哪个 GameObject 是 `generalInfoContainer`、哪个组件是 `cardsInDeckPanel`」
只有**读它自己那个 MonoBehaviour 的字段值**才能钉死 —— 反编译只给偏移（0x70/0x78）。
⚠️ 不要靠名字猜（`Show Deck Content Button` 这种名字会把人带偏）。

用法：
    python _probe_deckinfo.py bundle_menus_assets_all <GO名字或pid> [--class DeckGeneralInfoDemo]
"""
import argparse
import io
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    sys.stderr.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

from menu_rect import Bundle, BUNDLES, chain_up   # noqa: E402

SKIP = ('m_ObjectHideFlags', 'm_CorrespondingSourceObject', 'm_PrefabInstance',
        'm_PrefabAsset', 'm_GameObject', 'm_Script', 'm_EditorHideFlags',
        'm_EditorClassIdentifier', 'm_Name')


def load_mb(bundle_dir, pid):
    p = os.path.join(bundle_dir, 'MonoBehaviour', 'MonoBehaviour_%s.json' % pid)
    if not os.path.exists(p):
        return None
    try:
        return json.load(io.open(p, encoding='utf-8'))
    except Exception:
        return None


def name_of_pid(b, pid):
    """pid → 「GameObject 名 / 组件类名」这种可读串。"""
    g = b.go.get(str(pid))
    if g:
        return 'GO「%s」' % (g.get('m_Name') or g.get('_name'))
    r = b.rt.get(str(pid))
    if r:
        gopid = r.get('m_GameObject', {}).get('m_PathID')
        g2 = b.go.get(str(gopid))
        if g2:
            return 'RT of GO「%s」' % (g2.get('m_Name') or g2.get('_name'))
    return None


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('bundle')
    ap.add_argument('root')
    ap.add_argument('--class', dest='cls', default=None,
                    help='只解这个类名的 MonoBehaviour（默认全解）')
    args = ap.parse_args()

    path = args.bundle if os.path.isdir(args.bundle) else os.path.join(BUNDLES, args.bundle)
    b = Bundle(path)

    # 🔴 **同名多实例**：先按父链判用途（`CLAUDE.md` 铁律 4 那条）—— 每个实例印出它的根祖先
    hits = b.find_go_by_name(args.root) if not str(args.root).lstrip('-').isdigit() else [str(args.root)]
    if not hits:
        sys.exit('找不到 GameObject「%s」' % args.root)
    if len(hits) > 1:
        print('# 「%s」在本包里有 %d 个实例 —— 按根祖先区分（**先判用途再取值**）' % (args.root, len(hits)))
        for h in hits:
            rp = b.rt_of_go(h)
            chain = chain_up(b, rp) if rp else []
            tops = []
            for c in chain:
                gp = b.go_of_rt(c)
                tops.append(b.go_name(gp) or '?')
            print('#   GO %s  根链：%s' % (h, ' > '.join(tops)))
        print()
    gopid = hits[0]
    rtp = b.rt_of_go(gopid)
    if rtp is None:
        sys.exit('「%s」没有 RectTransform' % args.root)

    # 走一遍整棵子树，收集 (rtpid, gapid, 名字)
    out = []
    stack = [(rtp, 0)]
    seen = set()
    while stack:
        cur, d = stack.pop()
        if cur in seen:
            continue
        seen.add(cur)
        rt = b.rt.get(str(cur))
        if rt is None:
            continue
        gp = rt.get('m_GameObject', {}).get('m_PathID')
        g = b.go.get(str(gp)) or {}
        out.append((cur, gp, g.get('m_Name') or g.get('_name'), d))
        for c in b.children(cur):
            stack.append((c, d + 1))

    owners = comp_owners(b)
    for (rtpid, gp, name, d) in out:
        g = b.go.get(str(gp)) or {}
        for c in g.get('m_Component', []):
            cp = str(c['component']['m_PathID'])
            if not cp.lstrip('-').isdigit():
                continue
            mb = load_mb(path, cp)
            if mb is None:
                continue
            script = mb.get('m_Script', {}).get('m_PathID')
            cls = b.go_name(0) if False else None
            # 类名：从 m_Script 的 MonoBehaviour 里读 m_ClassName；取不到就印 pid
            clsname = 'MB[%s]' % (load_script_name(path, script) or ('pid ' + str(script)))
            if args.cls and args.cls not in (load_script_name(path, script) or ''):
                continue
            print('=' * 70)
            print('%s「%s」 (depth %d)  组件 %s' % ('  ' * d, name, d, clsname))
            for k, v in mb.items():
                if k in SKIP:
                    continue
                print('   %-34s = %s' % (k, fmt(b, v, 0, owners)))
    return 0


def load_script_name(bundle_dir, script_pid):
    """类名走 `menu_dump.script_names()` —— MonoScript 在**另一个包**（`*monoscript*`）里，
    **不在当前 bundle 目录下**（照搬自 `menu_dump.py:45` 的注释，别再自己猜）。"""
    return script_names_cached().get(str(script_pid))


_SCRIPTS = None


def script_names_cached():
    global _SCRIPTS
    if _SCRIPTS is None:
        try:
            from menu_dump import script_names
            _SCRIPTS = script_names()
        except Exception as e:                                  # noqa: BLE001
            sys.stderr.write('⚠️ 取不到 MonoScript 类名表：%r\n' % (e,))
            _SCRIPTS = {}
    return _SCRIPTS


def comp_owners(b):
    """组件 pid → 它的 GameObject 名。🔴 字段值多半是**组件**（Image/TMP/Button…）的 pid，
    不是 GameObject/RT 的 —— 只查 rt/go 两张表会全部落空（我第一版就栽在这）。"""
    m = {}
    for gopid, g in b.go.items():
        nm = g.get('m_Name') or g.get('_name')
        for c in g.get('m_Component', []):
            cp = str(c['component']['m_PathID'])
            m.setdefault(cp, (nm, gopid))
    return m


def comp_go_name(m, pid):
    hit = m.get(str(pid))
    return hit[0] if hit else None


def fmt(b, v, depth=0, owners=None):
    if isinstance(v, dict):
        if 'm_PathID' in v and set(v.keys()) <= {'m_PathID', 'm_FileID'}:   # 🔴 字段值是 {m_FileID, m_PathID} **两个键**，不是只剩 m_PathID（第一版就是按 len==1 判的，全落空）
            pid = v['m_PathID']
            if pid == 0:
                return 'null'
            r = b.rt.get(str(pid))
            if r is not None:
                gopid = r.get('m_GameObject', {}).get('m_PathID')
                g = b.go.get(str(gopid)) or {}
                return '→RT of「%s」(GO %s / RT %s)' % (g.get('m_Name') or g.get('_name'), gopid, pid)
            g = b.go.get(str(pid))
            if g is not None:
                return '→GO「%s」(pid %s)' % (g.get('m_Name') or g.get('_name'), pid)
            if owners:
                nm = comp_go_name(owners, pid)
                if nm:
                    return '→组件 on GO「%s」(组件 pid %s)' % (nm, pid)
            return '→pid %s（这个包里没有）' % pid
        return '{' + ', '.join('%s:%s' % (k, fmt(b, x, depth + 1, owners)) for k, x in v.items()) + '}'
    if isinstance(v, list):
        if depth > 1:
            return '[%d 项]' % len(v)
        return '[' + ', '.join(fmt(b, x, depth + 1, owners) for x in v) + ']'
    return repr(v)


if __name__ == '__main__':
    sys.exit(main())
