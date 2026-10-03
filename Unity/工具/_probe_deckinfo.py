#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""_probe_deckinfo.py — 把一个 prefab 节点的 **MonoBehaviour 序列化字段** 逐条解出来。

为什么（2026-09-24）：`DeckGeneralInfoDemo` 的 `Toggle()` 是**两个抽屉互斥**，
但「哪个 GameObject 是 `generalInfoContainer`、哪个组件是 `cardsInDeckPanel`」
只有**读它自己那个 MonoBehaviour 的字段值**才能钉死 —— 反编译只给偏移（0x70/0x78）。
⚠️ 不要靠名字猜（`Show Deck Content Button` 这种名字会把人带偏）。

用法：
    python _probe_deckinfo.py bundle_menus_assets_all <GO名字或pid> [--class DeckGeneralInfoDemo]
    python _probe_deckinfo.py bundle_menus_assets_all "Cards" --class Image --pid 6258122472555867940

🔴 **2026-10-04（A51 F9）：同名多实例【不再静默取第一个】。**
   以前这条工具命中多个同名 GO 时只把根链印出来、然后 **dump `hits[0]`** —— 实跑
   `… "Cards" --class Image` 会印三条根链却 dump 了 `Booster Pack Open Window` 那棵（`Ban Icon` /
   `New Card Badge` / `Card Ready for level up`），**看起来像「Deck Editing Menu 的 Cards 长这样」**。
   这正是铁律 4「同名多实例先按父链判用途」要防的坑，也与本工具 `--class` 那条「表空就硬失败」同源。
   ⇒ 现在：多命中**硬失败**（把根链和 pid 打出来让你选），要指定实例请加 `--pid <GO pid>`。
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
    ap.add_argument('--pid', dest='pid', default=None,
                    help='同名多实例时指定要看哪一个（GO 的 PathID；先不加它跑一次，'
                         '命中的根链与 pid 会打出来）')
    args = ap.parse_args()

    path = args.bundle if os.path.isdir(args.bundle) else os.path.join(BUNDLES, args.bundle)
    b = Bundle(path)

    # 🔴 **同名多实例**：先按父链判用途（`CLAUDE.md` 铁律 4 那条）—— 每个实例印出它的根祖先
    hits = b.find_go_by_name(args.root) if not str(args.root).lstrip('-').isdigit() else [str(args.root)]
    if not hits:
        sys.exit('找不到 GameObject「%s」' % args.root)
    if args.pid:
        # 显式指定实例：跳过「按名字猜」这一步（仍核一遍，写错 pid 要出声）
        gopid = str(args.pid)
        if gopid not in hits:
            sys.exit('❌ `--pid %s` 不在「%s」的命中表里（命中：%s）'
                     % (args.pid, args.root, ', '.join(hits)))
    elif len(hits) > 1:
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
        # 🔴 **2026-10-04（A51 F9）**：多命中**不许静默取第一个** —— 原来这里直接 `hits[0]`，
        #    实跑 `"Cards" --class Image` 会 dump 到 `Booster Pack Open Window` 那棵，**看着像真结论**。
        #    与 `--class` 表空时硬失败同一条原则（铁律 4：同名多实例先按父链判用途）。
        sys.exit('❌ 「%s」在本包里有 %d 个同名实例 —— **多义不许静默取第一个**。'
                 '上面三条根链挑一条，用 `--pid <GO pid>` 指定。' % (args.root, len(hits)))
    else:
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
    # 🔴 **2026-10-04（W4）加**：`--class` 过滤**依赖类名表**；表一空，下面那个 `continue` 会把
    #    每一条都当「不匹配」跳过 ⇒ 输出为空、**看着像「这个包里没有这个类」**（我们就是这么被骗了几轮）。
    #    ⇒ 表空时**直接失败**，不给出空结果。
    if args.cls and not script_names_cached():
        sys.exit('❌ `--class %s` 需要 MonoScript 类名表，但那张表是空的（%s）——'
                 '不给出空结果（空 = 会被读成「这个包里没有这个类」）。' % (args.cls, _SCRIPTS_ERR or '原因未知'))
    n_shown = 0
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
            # 类名：从 m_Script 的 MonoBehaviour 里读 `m_ClassName`；取不到就印 pid
            clsname = 'MB[%s]' % (load_script_name(path, script) or ('pid ' + str(script)))
            if args.cls and args.cls not in (load_script_name(path, script) or ''):
                continue
            n_shown += 1
            print('=' * 70)
            print('%s「%s」 (depth %d)  组件 %s' % ('  ' * d, name, d, clsname))
            for k, v in mb.items():
                if k in SKIP:
                    continue
                print('   %-34s = %s' % (k, fmt(b, v, 0, owners)))
    if args.cls and n_shown == 0:
        # 表在、但一条没匹配 ⇒ 这是**真结论**（不是静默失败）：说出来
        sys.stderr.write('（`--class %s`：这棵树里一个匹配的组件都没有 —— 类名表在，这是真结论）\n' % args.cls)
    return 0


def load_script_name(bundle_dir, script_pid):
    """类名走 `menu_dump.mono_index()` —— MonoScript 在**另一个包**（`*monoscript*`）里，
    **不在当前 bundle 目录下**（照搬自 `menu_dump.py` 的注释，别再自己猜）。

    🔴 **2026-10-04（W4）修**：这里原来写的是 `from menu_dump import script_names` ——
       `menu_dump` **根本没有这个名字**（真名 = `mono_index()`，见 `menu_dump.py:143`），
       抛出 `ImportError` 时又被 `except Exception` **吞掉** ⇒ 类名表恒空 ⇒
       `--class` 过滤**恒不匹配、静默打印 0 条**（不加 `--class` 时才看不出问题）。
       现在：① 用真名 `mono_index`；② 兜底再试一次 `menu_dump` 里**实际存在的**候选名；
       ③ 真取不到时**出声**（`--class` 那条路另外硬失败，见 `main`）。"""
    return script_names_cached().get(str(script_pid))


_SCRIPTS = None
_SCRIPTS_ERR = None


def script_names_cached():
    """PathID → 类名。取不到时**返回空表并记下原因**（由调用方决定是出声还是硬失败）。"""
    global _SCRIPTS, _SCRIPTS_ERR
    if _SCRIPTS is None:
        try:
            import menu_dump
            fn = getattr(menu_dump, 'mono_index', None)
            if fn is None:                     # 上游改名兜底：把这些名字都试一遍
                for cand in ('mono_index', 'script_names', 'mono_script_index'):
                    fn = getattr(menu_dump, cand, None)
                    if fn is not None:
                        break
            if fn is None:
                raise AttributeError('menu_dump 里没有 mono_index / script_names')
            try:
                _SCRIPTS = fn(verbose=False)   # ⚠️ 别让它往 stdout 打索引行数（会混进报告）
            except TypeError:
                _SCRIPTS = fn()
        except Exception as e:                                  # noqa: BLE001
            _SCRIPTS_ERR = repr(e)
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
