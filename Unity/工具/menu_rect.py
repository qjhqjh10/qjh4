#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""menu_rect.py — 把解包里某个 prefab 子树的**绝对像素矩形**机械算出来（1920×1080 · 左上原点 · y 向下）。

为什么要有它（2026-09-23，阶段二「日常」）：
    手推 UGUI 的锚点公式**我连着推错两次**（把 `anchoredPosition.y` 的符号搞反 ⇒ 整层偏 70.94），
    而那份生成出来的 `菜单全树.md` 又**不能直接当坐标源**。⇒ 判据做成脚本，一次跑出全表。

公式（UGUI `RectTransform` 的语义，逐条照抄）：
    refNorm   = lerp(anchorMin, anchorMax, pivot)          ← **参考点是 pivot 在锚框里的插值处**
    refPx     = parentRect.topLeft + (refNorm.x * pw, (1 - refNorm.y) * ph)
    pivotPx   = refPx + (pos.x, -pos.y)                    ← pos.y 向上为正，本坐标系 y 向下 ⇒ 取负
    size      = (|anchorMax.x-anchorMin.x| * pw + sizeDelta.x,
                 |anchorMax.y-anchorMin.y| * ph + sizeDelta.y)
    rect      = [pivotPx.x - pivot.x*size.w, pivotPx.x + (1-pivot.x)*size.w]
              × [pivotPx.y - (1-pivot.y)*size.h, pivotPx.y + pivot.y*size.h]

⚠️ **不含 LayoutGroup**：被布局组排的节点在这里给的是「布局跑之前的模板位」
   （例：Rewards 左栏四键 `pos` 全是 0 ⇒ 四个完全重合）。**那一格要自己按布局组参数算**，
   脚本只能在末尾替你列出「这一层有布局组」提醒一句。

用法：
    python menu_rect.py <bundle目录> <根 GameObject 的 pid 或名字> [--depth N] [--active-only]
例：
    python menu_rect.py bundles/bundle_menus_assets_all "Missions Tab" --depth 4
"""
import argparse
import io
import json
import os
import sys

# 🔴 Windows 控制台默认 GBK ⇒ 输出 `✗` 这类符号会 UnicodeEncodeError（2026-09-23 踩过）
try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

BUNDLES = 'd:/2/新解包资源/assets_full'


def load(bundle, kind, pid):
    p = os.path.join(bundle, kind, f'{kind}_{pid}.json')
    if not os.path.exists(p):
        return None
    try:
        return json.load(io.open(p, encoding='utf-8'))
    except Exception:
        return None


class Bundle(object):
    def __init__(self, path):
        self.path = path
        self.rt = {}      # rtpid -> dict
        self.go = {}      # gopid -> dict
        self._scan_rt()
        self._scan_go()

    def _scan_rt(self):
        d = os.path.join(self.path, 'RectTransform')
        if not os.path.isdir(d):
            sys.exit(f'没有 {d}')
        for fn in os.listdir(d):
            if not (fn.startswith('RectTransform_') and fn.endswith('.json')):
                continue
            try:
                j = json.load(io.open(os.path.join(d, fn), encoding='utf-8'))
            except Exception:
                continue
            self.rt[j.get('m_PathID', fn)] = j
            # 文件名里的 pid 比内容可靠（内容里没有 m_PathID 时）
            pid = fn[len('RectTransform_'):-len('.json')]
            self.rt[pid] = j

    def _scan_go(self):
        d = os.path.join(self.path, 'GameObject')
        if not os.path.isdir(d):
            return
        for fn in os.listdir(d):
            if not fn.endswith('.json'):
                continue
            try:
                j = json.load(io.open(os.path.join(d, fn), encoding='utf-8'))
            except Exception:
                continue
            j['_name'] = fn[:-len('.json')]
            # 🔴 **GO 的真 pid 只能从它的组件反推**：文件名有三种形状 —— `唯一名` / `名_pid` / `名 (N)`
            #    （`(N)` 是 AssetStudio 的计数器，**不含 pid**）。判据：拿它的 RectTransform 组件，
            #    读那个 RT 的 `m_GameObject.m_PathID` —— 那就是这个 GO 自己的 pid（双向校验）。
            true_pid = None
            for c in j.get('m_Component', []):
                cp = str(c['component']['m_PathID'])
                r = self.rt.get(cp)
                if r is not None:
                    true_pid = str(r.get('m_GameObject', {}).get('m_PathID', ''))
                    if true_pid and true_pid != '0':
                        break
            if true_pid:
                self.go[true_pid] = j
            # 文件名退化键（`名_pid` 那种至少还能按 pid 找到）
            tail = fn[:-len('.json')].rsplit('_', 1)[-1]
            if tail.lstrip('-').isdigit():
                self.go.setdefault(tail, j)

    def find_go_by_name(self, name):
        hits = [pid for pid, g in self.go.items()
                if g.get('m_Name') == name or g.get('_name') == name
                or g.get('_name', '').rsplit('_', 1)[0] == name]
        return hits

    def go_name(self, gopid):
        g = self.go.get(str(gopid))
        if not g:
            return None
        return g.get('m_Name') or g.get('_name')

    def go_of_rt(self, rtpid):
        r = self.rt.get(str(rtpid))
        if not r:
            return None
        return r.get('m_GameObject', {}).get('m_PathID')

    def children(self, rtpid):
        r = self.rt.get(str(rtpid))
        if not r:
            return []
        return [c['m_PathID'] for c in r.get('m_Children', [])]

    def parent(self, rtpid):
        r = self.rt.get(str(rtpid))
        if not r:
            return None
        p = r.get('m_Father', {}).get('m_PathID', 0)
        return p or None

    def rt_of_go(self, gopid):
        g = self.go.get(str(gopid))
        if not g:
            return None
        for c in g.get('m_Component', []):
            cp = str(c['component']['m_PathID'])
            if cp in self.rt:
                return cp
        return None

    def find_go(self, key):
        """按 pid 或名字找 GameObject。名字命中多个时报出来，让人自己挑。"""
        if str(key) in self.go:
            return str(key)
        hits = [pid for pid, g in self.go.items()
                if g.get('_name') == key or g.get('m_Name') == key
                or g.get('_name', '').rsplit('_', 1)[0] == key]
        if not hits:
            return None
        if len(hits) > 1:
            sys.stderr.write(f'⚠️ 「{key}」命中 {len(hits)} 个，取第一个：'
                             + '、'.join(f'{h}({self.go[h]["_name"]})' for h in hits[:8]) + '\n')
        return hits[0]


def rect_of(rt, parent_rect, scale):
    """算一个 RT 在父矩形下的绝对矩形。parent_rect = (x1, y1, x2, y2)，y 向下。"""
    px1, py1, px2, py2 = parent_rect
    pw, ph = px2 - px1, py2 - py1

    a_min, a_max = rt['m_AnchorMin'], rt['m_AnchorMax']
    piv, pos, sz = rt['m_Pivot'], rt['m_AnchoredPosition'], rt['m_SizeDelta']

    ref_nx = a_min['x'] + (a_max['x'] - a_min['x']) * piv['x']
    ref_ny = a_min['y'] + (a_max['y'] - a_min['y']) * piv['y']
    ref_x = px1 + ref_nx * pw
    ref_y = py1 + (1.0 - ref_ny) * ph

    piv_x = ref_x + pos['x']
    piv_y = ref_y - pos['y']            # pos.y 向上为正；本坐标系 y 向下

    w = abs(a_max['x'] - a_min['x']) * pw + sz['x']
    h = abs(a_max['y'] - a_min['y']) * ph + sz['y']

    x1 = piv_x - piv['x'] * w
    x2 = piv_x + (1.0 - piv['x']) * w
    y1 = piv_y - (1.0 - piv['y']) * h
    y2 = piv_y + piv['y'] * h
    return (x1, y1, x2, y2), (w, h)


def chain_up(b, rtpid):
    """从 rtpid 沿 `m_Father` 一路爬到根，返回 [root, …, rtpid] 的 RT pid 列表。"""
    chain = []
    cur = rtpid
    seen = set()
    while cur and cur not in seen:
        seen.add(cur)
        chain.append(cur)
        cur = b.parent(cur)
    chain.reverse()
    return chain


def parent_rect_of(b, rtpid, screen_rect):
    """🔴 **被查节点的父矩形** —— 沿 `m_Father` 爬上去，把每一级的 `rect_of` 逐层算下来。

    为什么必须有它（2026-09-24 踩）：
        原来 `main()` 把 `(0,0,1920,1080)` 直接当成**被查节点的父矩形**交给 `walk()`。
        这对「本身就是场景根」的节点是对的，但对**任何父链上有非全屏节点**的都是错的 ——
        典型就是收藏窗/奖励窗的 `Content Area`：它自己 = `167.17,70.94 → 1920.01,1080`
        （`pos=(83.59,-35.47) sd=(-167.17,-70.94)`）⇒ 它下面**所有**节点被整套平移。
        实测代价：`Card Filters` 被报成 `-166.92,85`（真值 `0.25,155.94`）；
        2026-09-24 的一次 Styles 页普查整份坐标准错了 `(167.17,70.94)`。
        **A3 那次是靠人工手算把内缩补回来的**（`1804 普查产出_0923/A3_Cards页.md` 头部那句警告），
        靠人工 = 迟早再错一次 ⇒ 把口径做进脚本。

    规则：**根节点的矩形按整屏（或 `--size`）算**，然后逐级往下套 `rect_of`，
    返回的是**被查节点的父**那一个矩形（`walk()` 会拿它去算被查节点自己）。
    """
    chain = chain_up(b, rtpid)
    if len(chain) <= 1:
        return screen_rect, None          # 被查节点自己就是根（没有父）
    rect = screen_rect
    for p in chain[:-1]:                  # 走到「被查节点的父」为止
        rt = b.rt.get(str(p))
        if rt is None:
            return screen_rect, None
        rect, _ = rect_of(rt, rect, (1.0, 1.0))
    f = b.rt.get(str(chain[-2]))
    return rect, (b.go_name(b.go_of_rt(chain[-2])) or f'<RT {chain[-2]}>')


def walk(b, rtpid, rect, scale, depth, maxdepth, out, indent=0, force_root_rect=None):
    if depth > maxdepth:
        return
    rt = b.rt.get(str(rtpid))
    if rt is None:
        return
    gopid = rt.get('m_GameObject', {}).get('m_PathID')
    name = b.go_name(gopid) or f'<RT {rtpid}>'
    g = b.go.get(str(gopid)) or {}
    active = g.get('m_IsActive', 1)

    (r, (w, h)) = rect_of(rt, rect, scale)
    if depth == 0 and force_root_rect is not None:
        # `--root-size`：**不按屏幕算根节点**，直接给它这个矩形（卡片的作者尺寸 ≠ 显示尺寸时用）
        r = force_root_rect
        w, h = force_root_rect[2] - force_root_rect[0], force_root_rect[3] - force_root_rect[1]
    scl = rt.get('m_LocalScale', {'x': 1, 'y': 1})

    # 组件：只报「有意思的」那几个（布局组 / 滚动 / 掩码）
    kinds = []
    for c in g.get('m_Component', []):
        cp = str(c['component']['m_PathID'])
        mb = load(b.path, 'MonoBehaviour', cp)
        if not mb:
            continue
        # 布局组的字段指纹：有 `m_Padding` + (`m_Spacing` 或 `m_ChildAlignment`)
        if 'm_Padding' in mb and ('m_Spacing' in mb or 'm_ChildAlignment' in mb):
            kinds.append('LayoutGroup')
        elif 'm_Content' in mb and 'm_Viewport' in mb:
            kinds.append('ScrollRect')
        elif 'm_ShowMaskGraphic' in mb or ('m_Maskable' not in mb and 'm_ShowMask' in str(mb)[:200]):
            kinds.append('Mask')
    kinds = sorted(set(kinds))

    out.append((indent, name, r, w, h, active, rt, scl, kinds))

    for c in b.children(rtpid):
        walk(b, c, r, (scale[0] * scl.get('x', 1), scale[1] * scl.get('y', 1)),
             depth + 1, maxdepth, out, indent + 1)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('bundle')
    ap.add_argument('root')
    ap.add_argument('--depth', type=int, default=3)
    ap.add_argument('--active-only', action='store_true')
    ap.add_argument('--size', default='1920x1080', help='根容器尺寸，默认 1920x1080')
    ap.add_argument('--root-size', default=None,
                    help='**强行给根节点这个尺寸**（WxH）。用途：一张卡的作者尺寸与实际显示尺寸不同时'
                         '（例：`Daily Mission Container` 作者 787.97×150，被布局撑到 539.19 宽），'
                         '按显示尺寸重算卡内版面 —— 锚点公式会自动按父宽重分布。')
    ap.add_argument('--relative', action='store_true',
                    help='输出**相对根节点左上角**的坐标（量一张卡/一个预制体内部的版面时用这个；'
                         '直接用屏幕坐标的话，根自己是按屏心锚点摆的，读起来要心算）')
    ap.add_argument('--no-parent', action='store_true',
                    help='🔴 **别用**（只为复现 2026-09-24 之前的旧行为）：不爬 `m_Father`，'
                         '直接把 `--size` 那个整屏矩形当被查节点的父。'
                         '被查节点挂在 `Content Area` 这类非全屏节点下时，坐标会整体平移。')
    ap.add_argument('--cs', action='store_true',
                    help='直接吐 **C# 能贴的参数表**（每行 = 名字 + 锚点五元组），'
                         '配 `UguiRect.Child` 用 —— 省掉手工誊抄几十个五元组（誊错一个就是一个静默的版面 bug）')
    args = ap.parse_args()

    path = args.bundle
    if not os.path.isdir(path):
        path = os.path.join(BUNDLES, args.bundle)
    b = Bundle(path)

    gopid = b.find_go(args.root)
    if gopid is None:
        sys.exit(f'找不到 GameObject「{args.root}」（pid 或名字）')
    rtpid = b.rt_of_go(gopid)
    if rtpid is None:
        # 根自己可能就是被当容器用：直接用场景尺寸
        sys.exit(f'「{args.root}」没有 RectTransform')

    sw, sh = (float(x) for x in args.size.lower().split('x'))
    root_rect = (0.0, 0.0, sw, sh)

    if args.root_size:
        rw, rh = (float(x) for x in args.root_size.lower().split('x'))
        root_rect = (0.0, 0.0, rw, rh)

    # 🔴 被查节点的**父矩形**：沿 `m_Father` 爬上去算，别把整屏直接当它的父（见 `parent_rect_of`）
    pname = None
    if args.no_parent:
        base_rect = root_rect
    else:
        base_rect, pname = parent_rect_of(b, rtpid, root_rect)
        if pname is not None:
            print(f'# （已沿 `m_Father` 爬父链：被查节点的父 = 「{pname}」'
                  f' {base_rect[0]:.2f},{base_rect[1]:.2f} → {base_rect[2]:.2f},{base_rect[3]:.2f}）')

    out = []
    walk(b, rtpid, base_rect, (1.0, 1.0), 0, args.depth, out,
         force_root_rect=(root_rect if args.root_size else None))

    ox, oy = (out[0][2][0], out[0][2][1]) if (args.relative and out) else (0.0, 0.0)

    if args.cs:
        # 吐 C# 数据表：`N(缩进, "名字", aMinX,aMinY, aMaxX,aMaxY, pivX,pivY, posX,posY, szX,szY),`
        print(f'// {args.root} —— 锚点五元组（原版 JSON 原文；缩进 = 层级）')
        print(f'// 出处 {path}')
        print(f'// 用法：`UguiRect.Child(父矩形, new Vector2(aMinX,aMinY), new Vector2(aMaxX,aMaxY),'
              f' new Vector2(pivX,pivY), new Vector2(posX,posY), new Vector2(szX,szY))`')
        for (ind, name, r, w, h, active, rt, scl, kinds) in out:
            a_min, a_max = rt['m_AnchorMin'], rt['m_AnchorMax']
            piv, pos, sz = rt['m_Pivot'], rt['m_AnchoredPosition'], rt['m_SizeDelta']
            nm = name.replace('"', "'")[:40]
            print(f'    N({ind}, "{nm}", {a_min["x"]:g},{a_min["y"]:g}, {a_max["x"]:g},{a_max["y"]:g},'
                  f' {piv["x"]:g},{piv["y"]:g}, {pos["x"]:g},{pos["y"]:g}, {sz["x"]:g},{sz["y"]:g}),'
                  + ('' if active else '   // 出厂 inactive'))
        return 0

    print(f'# {args.root}  ' + ('相对根左上角' if args.relative else '绝对矩形')
          + '（1920×1080 · 左上原点 · y 向下）')
    print(f'# 出处 {path}')
    print(f'{"深度":<4}{"名字":<44}{"x1":>9}{"y1":>9}{"x2":>9}{"y2":>9}{"宽":>9}{"高":>9}  act  组件')
    for (ind, name, r, w, h, active, rt, scl, kinds) in out:
        if args.active_only and not active:
            continue
        r = (r[0] - ox, r[1] - oy, r[2] - ox, r[3] - oy)
        nm = '  ' * ind + name
        flag = '' if active else 'INACT'
        sc = '' if (abs(scl.get('x', 1) - 1) < 1e-6 and abs(scl.get('y', 1) - 1) < 1e-6) \
            else f' scl={scl.get("x",1):.3g}'
        print(f'{ind:<4}{nm[:43]:<44}{r[0]:>9.2f}{r[1]:>9.2f}{r[2]:>9.2f}{r[3]:>9.2f}'
              f'{w:>9.2f}{h:>9.2f}  {flag:<5} {",".join(kinds)}{sc}')

    # 布局组提醒：祖先里有布局组的，子节点的位置**不是**这里给的
    lg_idx = [n for n, e in enumerate(out) if 'Layout' in ''.join(e[8])]
    if lg_idx:
        print('\n⚠️ 下面这些节点**带布局组** —— 它们的子节点位置由布局算，本表给的是「布局跑之前的模板位」：')
        for n in lg_idx:
            print('   ', '  ' * out[n][0] + out[n][1])
    return 0


if __name__ == '__main__':
    sys.exit(main())
