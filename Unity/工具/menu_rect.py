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
            # 🔴 **把自己那个 pid 挂回 dict**（`menu_dump` 要拿它去查 `m_Children`，
            #    而 `walk` / `_child_sizes` 那几条路**手上只有 dict、没有 pid**）。
            #    ⚠️ 键名叫 `_pid`（带下划线）= 导出 JSON 里**不可能出现**的字段名 ⇒ 不会撞车；
            #       下游读字段一律按名字取（`m_AnchorMin` 这些），多一个键没有副作用。
            j['_pid'] = pid

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
    """算一个 RT 在父矩形下的绝对矩形。parent_rect = (x1, y1, x2, y2)，y 向下。

    🔴 **2026-10-05 修一处真缺陷：`scale` 原来是个【死参】—— 收了、函数体一次没用**（A60⑤⑨）。
       后果：**父链上有 `m_LocalScale ≠ 1` 的节点时，它下面整棵子树的「绝对矩形」全都不乘缩放**
       （本包实测 1488 个 RT 自带非 1 缩放 ⇒ 不是一个理论问题）。四处独立复核都指向它。

    ## `scale` 是什么（先把语义钉死，再谈乘在哪一级）
    `walk()` 传进来的是 **`lossyScale(父)`** = 从根到父（**含父自己那一级**）的 `m_LocalScale`
    连乘 —— 判据就是 `walk` 的递归：根收 `(1,1)`，每下一级 `scale *= 本级的 m_LocalScale`。

    ## 为什么该乘、乘在哪（逐条从 uGUI 语义推）
    ① uGUI 的 `RectTransform.rect` 是 **本件的局部尺寸**：
       `rect.size = sizeDelta + (anchorMax − anchorMin) ⊙ parentRect.size`
       （`parentRect` 是**父的 `rect.size`**，即父的**局部**尺寸，**不带任何缩放**）。
    ② 屏幕像素 = 局部值 × 沿链的 `localScale` 连乘（Unity 的
       `rect.size × lossyScale == 渲染尺寸`，这是标准关系）。
    ③ 本函数的 `parent_rect` 是**父在屏幕上的框** ⇒ 父的**局部**尺寸 = `(pw/scale.x, ph/scale.y)`。
    ④ 把 ① 代入 ③，再把结果换算回屏幕像素（× `scale`，**不含自己这一级**）：
         `布局框 = anchorDiff ⊙ parent_rect尺寸 + sizeDelta ⊙ scale`
       —— 注意 `anchorDiff` 那一项**不再乘** scale（它本来就是按父框的**屏幕**尺寸占比例），
       而 `sizeDelta`（局部单位）**必须**乘。
    ⑤ 枢轴点（屏幕）= 锚参考点（父屏幕框里按比例插值）+ `anchoredPosition ⊙ scale`
       （`anchoredPosition` 也是**父的局部单位**）。
    ⑥ 本函数返回的 `rect`/`(w,h)` 是**布局框**（**不含本件自己的 `m_LocalScale`**）——
       画出来的是 `布局框 × 本件 localScale`，那就是 `menu_dump._scl_cell` 那一列。
       **修好之后那一列才是恒对的**（修之前当 `lossyScale(父) ≠ 1` 时它也是错的）。

    ⚠️ **不含 LayoutGroup**：被布局组排的节点在这里给的是「布局跑之前的模板位」（见文件头）。
    ⚠️ **不含 `m_LocalRotation`**：旋转不改 `rect.size`（它是局部框），但会转**画出来的框**；
       本工具按未旋转帧给值，`menu_dump.py` 逐行标 `rot=` 并单列清单（别静默）。

    手算 fixture（`menu_dump.verify_layout` 里也钉了同一条）：
        父屏幕框 (100,200)→(500,800)（400×600）、`scale=(2,2)`、本件
        `aMin=aMax=(0.5,0.5)`、`pivot=(0.5,0.5)`、`pos=(10,0)`、`sizeDelta=(100,50)`
        ⇒ 父局部 200×300；参考点 (300,500)；枢轴 (300+10×2, 500) = **(320,500)**；
          布局框 `0×400+100×2 = 200` × `0×600+50×2 = 100`
        ⇒ rect **(220,450)→(420,550)**。
    """
    px1, py1, px2, py2 = parent_rect
    pw, ph = px2 - px1, py2 - py1
    sx, sy = scale

    a_min, a_max = rt['m_AnchorMin'], rt['m_AnchorMax']
    piv, pos, sz = rt['m_Pivot'], rt['m_AnchoredPosition'], rt['m_SizeDelta']

    ref_nx = a_min['x'] + (a_max['x'] - a_min['x']) * piv['x']
    ref_ny = a_min['y'] + (a_max['y'] - a_min['y']) * piv['y']
    ref_x = px1 + ref_nx * pw
    ref_y = py1 + (1.0 - ref_ny) * ph

    piv_x = ref_x + pos['x'] * sx          # `anchoredPosition` 是【父的局部单位】
    piv_y = ref_y - pos['y'] * sy          # pos.y 向上为正；本坐标系 y 向下

    w = abs(a_max['x'] - a_min['x']) * pw + sz['x'] * sx
    h = abs(a_max['y'] - a_min['y']) * ph + sz['y'] * sy

    x1 = piv_x - piv['x'] * w
    x2 = piv_x + (1.0 - piv['x']) * w
    y1 = piv_y - (1.0 - piv['y']) * h
    y2 = piv_y + piv['y'] * h
    return (x1, y1, x2, y2), (w, h)


def parent_local_size(parent_rect, scale):
    """**父的局部尺寸**（uGUI `GetParentSize()` = `parent.rect.size`），两个调用方都要它：

    · `ContentSizeFitter` 的 `SetSizeWithCurrentAnchors` ⇒ `sizeDelta = 目标 − parentSize ⊙ anchorDiff`；
    · `AspectRatioFitter.FitInParent/EnvelopeParent` 的 `parentSize`。

    🔴 **`scale` 有 0 时返回 `None`（不是抛异常）**：父的局部尺寸 = 屏幕框 ÷ `lossyScale(父)`，
       而 `lossyScale(父) == 0` 时那个除法是 `0/0` —— **从这两个入参里根本恢复不出父的局部尺寸**
       （父的框已经被压成 0 了）。实测 `bundle_menus_assets_all` 有 **390 个** RT 的
       `m_LocalScale` 是 `(0,0)`（还有 1476 个是 `(0.01,0.01)`）⇒ 这是常态不是一个边角。
       调用方**必须**处理 `None`（= 这件算不出来，别瞎填、也别让它抛出去）。
    """
    if abs(scale[0]) < 1e-12 or abs(scale[1]) < 1e-12:
        return None
    return ((parent_rect[2] - parent_rect[0]) / scale[0],
            (parent_rect[3] - parent_rect[1]) / scale[1])


def local_size(rt, parent_rect, scale):
    """这个 RT 自己的 **`RectTransform.rect.size`**（uGUI 意义上的**局部**尺寸，任何缩放都不含）。

    用途：`AspectRatioFitter` 的模式 1/2 读的是 `rectTransform.rect.height / .width`
    （`AspectRatioFitter.cs:135/141`），那是**局部值**，不是屏幕值。

    `parent_rect` / `scale` 与 `rect_of` 同义（父的屏幕框 / `lossyScale(父)`）。
    父链上缩放为 0 时返回 `None`（理由与 `parent_local_size` 同）。
    """
    pl = parent_local_size(parent_rect, scale)
    if pl is None:
        return None
    _, (w, h) = rect_of(rt, (0.0, 0.0, pl[0], pl[1]), (1.0, 1.0))
    return w, h


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


def parent_rect_of(b, rtpid, screen_rect, keep_scales=True):
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

    🔴 **2026-10-05 起同时返回 `lossyScale(父)`** —— 两个理由，少一个都不行：
      ① `rect_of` 修好之后要 `lossyScale(父)` 才能正确换算（见它的 docstring）；
      ② 爬链这一路**必须跟着累积 `m_LocalScale`**，否则「被查节点的父在屏幕上的框」
         在父链带缩放时**本身就是错的**（与 `rect_of` 之前那个死参是同一个错，只是位置不同）。
      返回值 = `(父矩形, 父名字, lossyScale(父))`；
      被查节点自己就是根时 = `(screen_rect, None, (1,1))`（根的父 = 画布，缩放 1）。
    """
    chain = chain_up(b, rtpid)
    if len(chain) <= 1:
        return screen_rect, None, (1.0, 1.0)     # 被查节点自己就是根（没有父）
    rect = screen_rect
    sc = (1.0, 1.0)                              # 根那一级的父（画布）缩放 = 1
    for p in chain[:-1]:                         # 走到「被查节点的父」为止
        rt = b.rt.get(str(p))
        if rt is None:
            return screen_rect, None, (1.0, 1.0)
        rect, _ = rect_of(rt, rect, sc)
        s = rt.get('m_LocalScale') or {}         # 本级跑完 ⇒ 下一级的 lossyScale(父) 已更新
        sc = (sc[0] * s.get('x', 1.0), sc[1] * s.get('y', 1.0)) if keep_scales else (1.0, 1.0)
    f = b.rt.get(str(chain[-2]))
    return rect, (b.go_name(b.go_of_rt(chain[-2])) or f'<RT {chain[-2]}>'), sc


def walk(b, rtpid, rect, scale, depth, maxdepth, out, indent=0, force_root_rect=None,
         keep_scales=True):
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
        ks = (scale[0] * scl.get('x', 1), scale[1] * scl.get('y', 1)) if keep_scales \
            else (1.0, 1.0)
        walk(b, c, r, ks, depth + 1, maxdepth, out, indent + 1,
             force_root_rect=None, keep_scales=keep_scales)


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
    ap.add_argument('--no-ancestor-scale', action='store_true',
                    help='🔴 **别当默认**（只为复现 2026-10-05 之前的旧行为 / 读「未缩放帧」的设计值）：'
                         '凡父链上有 `m_LocalScale` 的件**不乘缩放** ⇒ 退回「当缩放=1」那一套读数。'
                         '⚠️ 默认（不传）才是对的：那时「绝对矩形」才真是屏幕像素（见 `rect_of`）。'
                         '⚠️ 原版 prefab 里存着动画/隐藏态（本包 390 个 RT 的 scale 是 `(0,0)`、'
                         '1476 个是 `(0.01,0.01)`）⇒ 默认口径下那些子树会被压成 0/极小。'
                         '要看它们**设计上**的版面就用这个开关。')
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
    base_scale = (1.0, 1.0)
    keep = not args.no_ancestor_scale
    if args.no_parent:
        base_rect = root_rect
    else:
        base_rect, pname, base_scale = parent_rect_of(b, rtpid, root_rect, keep_scales=keep)
        if pname is not None:
            print(f'# （已沿 `m_Father` 爬父链：被查节点的父 = 「{pname}」'
                  f' {base_rect[0]:.2f},{base_rect[1]:.2f} → {base_rect[2]:.2f},{base_rect[3]:.2f}）')
            if abs(base_scale[0] - 1) > 1e-6 or abs(base_scale[1] - 1) > 1e-6:
                print(f'# ⚠️ 父链上有 `m_LocalScale`：`lossyScale(父)` = '
                      f'{base_scale[0]:.4g},{base_scale[1]:.4g} ⇒ 下面所有矩形都按它换算')
    if not keep:
        base_scale = (1.0, 1.0)
        print('# 🔴 `--no-ancestor-scale`：父链上的 `m_LocalScale` **一律不乘**'
              '（= 2026-10-05 之前的口径，读「未缩放帧」的设计值用）')

    out = []
    walk(b, rtpid, base_rect, base_scale, 0, args.depth, out,
         force_root_rect=(root_rect if args.root_size else None), keep_scales=keep)

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
