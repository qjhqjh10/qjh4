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
   ⚠️ **反过来说**：`menu_dump.py`（另一种模式）**会**按 uGUI 算法把布局跑一遍，并把结果
   **原地写回**那张 RT 表 ⇒ 在它那张表里，「锚点 / `anchoredPosition` / `sizeDelta`」三列
   对**被布局组管的节点**是「**布局后的模拟值**」，**不是 prefab 字段**
   （写回点：`menu_dump.py:1858/1873/1874/1876/1877/1901/1906/1909`）——
   拿那三列的数去 prefab JSON 里**纯数值搜索是搜不到的**（A145②，同 A128「再算一步」那一类）。
   ⇒ 要**prefab 原值**用本工具，或 `menu_dump.py --no-layout`。

⚠️ **「宽/高」与四个坐标都是【布局框】**（= 设计值），**画出来要乘这一件自己的 `m_LocalScale`**
   —— 每行末尾并排列出 `视觉框=`（判据只此一份：`visual_cell()`；`menu_dump.py` 两种模式调的是同一个）。

⚠️ **行内标记（A499 起，与 `menu_dump.py` 同口径）**：
   `INACT` = **这一件自己的 `m_IsActive=0`**；`ANC✗` = 它自己 active、**祖先 inactive**
   （uGUI 的 `activeInHierarchy` 看**整条父链**）⇒ **原版一个像素都不画**。
   ⛔ 两者**不是一回事**，可以各自单独出现。`--active-only` 过的是**自己那一格**（= `activeSelf`）。

⚠️ **表尾的布局组清单分三档**（A499 起）：会跑的 · **`m_Enabled=0`（原版永远不跑 ⇒ 本表的模板位
   就是终值）** · **组自己不在 `activeInHierarchy` 里（此刻不跑）**。
   ⛔ **本工具【不跑】布局组** ⇒ 这三档都不给「布局之后」的值；要那个值用 `menu_dump.py`。

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
# 🔴 **A620（2026-10-14）顺手补**：**stderr 也要** —— 本文件的警告分两条流走：
#    `go_coll_warning`（撞车）与 `stats_warning`（本表不全，`--cs` 那一支）都写 **stderr**，
#    而 Windows 上 stderr 重定向到文件时按 **GBK** 开 ⇒ 中文变乱码、emoji 退化成 `\uXXXX` 转义
#    （实测：`menu_rect.py … --cs 2>err.txt` 里整段警告读不出来 ⇒ **等于没出声**）。
#    ⛔ 与 stdout 同一口径（上面那一句的理由逐字相同），别只改一边。
try:
    sys.stderr.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

BUNDLES = 'd:/2/新解包资源/assets_full'


# ================================================================ 「布局框 ≠ 视觉框」—— **只此一份**
# 🔴 **A145（2026-10-07）**：这条规则原来**只有 `menu_dump.py --md` 表里那一列**，
#    而**纯文本模式**与**本工具**要么只印个系数、要么一个字都不提 ⇒ 读表的人把
#    **布局框（设计值）当成画出来的宽**抄进 `.cs`（真发生过：`icon` 布局 56 ⇒ 实际 67.2；
#    `Special Missions` 布局 660.43 ⇒ 实际 759.5；`Daily Missions` 行宽 609.50 ⇒ 700.93）。
#    ⇒ 判据收进这里一份，`menu_dump.py`（两种模式）与本工具**都调它**，⛔ 不许再写第二份。
#
# 语义（先把范围钉死）：
#   · 「布局框」= 本表印的 `w/h` 与四个绝对坐标（= `rect_of` 的返回值，**不含本件自己的缩放**）；
#   · 「视觉框」= **布局框 × 本件自己的 `m_LocalScale`**（= 真正画在屏幕上的框）；
#   · **父链上的缩放不在这里** —— 它已经被 `rect_of` 乘进「布局框」里了（A60⑤⑨ 那条修复，
#     见 `rect_of` 的 ①–⑥ 与 `parent_rect_of`）；本函数只管**这一件自己那一级**。
SCL_EPS = 1e-6        # 逐行「这一件自带缩放」的门槛（`menu_dump` 两种模式 + 本文件同门槛）
SCL_WARN_EPS = 1e-3   # 汇成末尾清单的门槛：视觉框与布局框差 **0.1%** 以上才算「看得出来」
SCL_WARN_MAX = 12     # 末尾清单最多列几行（其余只报数）


def local_scale(rt):
    """这一件自己的 `m_LocalScale`（导出 JSON 里大多数 RT 不带它 ⇒ 读不到就是 1，不是错误）。"""
    s = rt.get('m_LocalScale') or {}
    return s.get('x', 1.0), s.get('y', 1.0)


def scale_is_one(sx, sy, eps=SCL_EPS):
    return abs(sx - 1) < eps and abs(sy - 1) < eps


def visual_size(sx, sy, w, h):
    """**布局框 → 视觉框**的算式本体（= 布局框 × 本件自己的 `m_LocalScale`）。

    🔴 **A145：这一条算式全域只此一处** —— `visual_cell()`、`menu_dump.rot_corners()`、
    `menu_dump` 的末尾清单、`menu_rect --cs` 的注释全调它（⛔ 别再写 `w * sx`）。
    """
    return w * sx, h * sy


def visual_cell(sx, sy, w, h, md=False):
    """表里那一格「布局框 → 视觉框」的**唯一实现**（A60① 建 · A145 扩到文本模式与本工具）。

    形状：`—` = 缩放 1（绝大多数行）；`×1.2 → 视觉 67.20×56.00` = 要乘（`md=True` 时加粗）。
    ⚠️ 缩放到 0 的件（`bundle_menus_assets_all` 实测 390 个 `(0,0)`）加 `⚠️` ——
       别让 `0.00×0.00` 看着像正常读数。
    """
    if scale_is_one(sx, sy):
        return '—'
    lab = f'×{sx:.4g}' if abs(sx - sy) < 1e-9 else f'×{sx:.4g},×{sy:.4g}'
    vw, vh = visual_size(sx, sy, w, h)
    s = f'{lab} → 视觉 {vw:.2f}×{vh:.2f}'
    if vw == 0 or vh == 0:
        s += ' ⚠️'
    return f'**{s}**' if md else s


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
        self.go = {}      # gopid -> dict（**撞车时只留得下第一份**，见 `go_coll` / `go_by_rt`）
        # 🔴 **A499 追加的两个索引**（都是「撞车」的产物，见 `_scan_go`）：
        self.go_by_rt = {}   # rtpid -> GO dict（**这颗 RT 归哪个 GO 文件**，撞车时唯一认得准的那条路）
        self.go_coll = {}    # 真 pid -> [GO 文件名, …]（>1 份 = 撞车；`go` 里留的是**第一份**）
        self._go_pid = {}    # 真 pid -> GO dict（**只记真 pid 的登记**，撞车判定专用，见 `_scan_go`）
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
                    # 🔴 **A499 追加：顺手记下「这颗 RT 归哪个 GO 文件」** —— 这是**更细一级**的索引，
                    #    撞车时靠它认人（判据见 `go_obj_of_rt` / `go_name_of_rt`）。
                    self.go_by_rt.setdefault(cp, j)
                    if true_pid and true_pid != '0':
                        break
            if true_pid:
                # 🔴 **A499 追加：⛔ 原来这里写的是 `self.go[true_pid] = j`（后扫覆盖先扫、且不出声）。**
                #    一个导出目录里**可以有两个内层 CAB 各有一个同号 GO**（本包实测：`Resources Bar` 用
                #    `m_FileID=5` 跨文件引用计数器 prefab ⇒ `Icon_238.json` 与 `Player Level.json`
                #    的 RT 分别是 `371` / `1279`，而**两者都自称 `m_GameObject.m_PathID = 238`**）⇒
                #    覆盖以后 `self.go['238']` 是谁**取决于 `os.listdir` 的顺序**（不可复现）。
                #    ⇒ 改成**先到先得**（确定）+ **把撞车记下来出声**（`go_coll`）。
                #    ⚠️ 光「确定」还不够 —— **要认得对**得走 `go_by_rt`（见 `go_obj_of_rt`）。
                #    ⚠️ `_go_pid` 这一本账是**专为撞车判定**记的：`self.go` 里还有**退化解键**
                #        （下面那条按文件名尾巴写的），拿它当依据会把「真 pid 撞退化解键」误报成
                #        撞车（实测：本包真撞车 **49** 组，用 `self.go` 判会虚报成 **64** 组）。
                prev = self._go_pid.get(true_pid)
                if prev is None:
                    self._go_pid[true_pid] = j
                    # ⚠️ **直写**（不是 `setdefault`）—— 与「真 pid 压过退化解键」这条老行为一致；
                    #    撞车那一支走下面的 `else`、**不覆盖**。
                    self.go[true_pid] = j
                else:
                    self.go_coll.setdefault(true_pid, [prev.get('_name')]).append(j.get('_name'))
            # 文件名退化键（`名_pid` 那种至少还能按 pid 找到）
            tail = fn[:-len('.json')].rsplit('_', 1)[-1]
            if tail.lstrip('-').isdigit():
                self.go.setdefault(tail, j)

    def go_obj_of_rt(self, rtpid):
        """**这一颗 RT** 所属的 GO 原始 JSON —— 认人优先走 `go_by_rt`（撞车时唯一认得准的那条路）。

        🔴 **为什么不能只用 `self.go[go_of_rt(rtpid)]`（A499 追加）**：pid 是**分包 / 分内层 CAB
           局部**的 ⇒ 一个导出目录里可以有两份同号 GO，`self.go` 只留得下一份、
           另一份**挂错名字**（而且还带着**错的名字背后的 `m_IsActive` 与 `m_Component`**）——
           `Resource Counter Item` 那棵树被 dump 成 `Player Level` 就是这么来的。
           而**这一颗 RT 自己的 pid 是唯一的** ⇒ 拿它去问「哪个 GO 文件的 `m_Component` 里列过它」
           认得准（每个 GO 文件必然会列出自己那颗 RT）。
        ⚠️ 查不到时**退回老路**（`self.go[go_of_rt(...)]`）⇒ 没有撞车时**逐字节同旧值**。
        """
        j = self.go_by_rt.get(str(rtpid))
        if j is not None:
            return j
        return self.go.get(str(self.go_of_rt(rtpid)))

    def go_name_of_rt(self, rtpid):
        """**这一颗 RT** 所属 GO 的名字（`go_obj_of_rt` 的取名字版）。"""
        g = self.go_obj_of_rt(rtpid)
        if not g:
            return None
        return g.get('m_Name') or g.get('_name')

    def go_collisions(self):
        """**真 pid 撞车**的清单：`{pid: [GO 文件名, …]}`（>1 份才算撞车，已确定取第一份）。

        用途：`main()` 起身时**出声**（铁律「不许静默失败」）—— 撞了就意味着**这个目录里
        「按 pid 认 GO」这套口径本身不够用**，凡走到撞车 pid 的读数都要用 `go_obj_of_rt` 复核。
        """
        return self.go_coll

    def find_rt(self, key):
        """按 pid 或名字找**一棵子树的 RT pid**（两个工具的 `main()` 都走它）—— 撞车安全版（A499 追加）。

        ① **老路先走**：`find_go(key)` → `rt_of_go`（结果与以前**逐字相同**，含「命中多个」那句警告）；
        ② 老路给不出 ⇒ 在 `go_by_rt`（**全部带 RT 的 GO 文件**）里按名字再扫一遍。
           🔴 **为什么必须有 ②**：`find_go` 只在 `self.go`（**pid 去重后的索引**）里找，而 pid 撞车时
              **另一份 GO 根本不在那张索引里** ⇒ 按名字找会**整个找不到**。
              实测 `bundle_scenes_scenes_mainmenuwarpforge` 的 `Resource Counter Item`：
              它的 pid `224` 被 `Armour Text_224` 先占了 ⇒ 名字查询**整个失效**
              （这就是「撞车」除了挂错名字之外的第二重后果）。撞车清单见 `go_collisions()`。
        """
        gopid = self.find_go(key)
        if gopid is not None:
            return self.rt_of_go(gopid)
        hits = [p for p, g in self.go_by_rt.items()
                if g.get('m_Name') == key or g.get('_name') == key
                or g.get('_name', '').rsplit('_', 1)[0] == key]
        if not hits:
            return None
        hits.sort(key=lambda x: int(x) if x.lstrip('-').isdigit() else 0)
        if len(hits) > 1:
            sys.stderr.write(f'⚠️ 「{key}」在 `go_by_rt` 里命中 {len(hits)} 个（`find_go` 那条路一个都没给）：'
                             + '、'.join(f'{h}({self.go_by_rt[h]["_name"]})' for h in hits[:8]) + '\n')
        return hits[0]

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


def go_coll_warning(b, stream=None, max_show=12):
    """**GO pid 撞车**的出声（判据只此一份 = `Bundle.go_coll`；两个工具都调它）。

    返回撞车的 pid 数（**0 ⇒ 一个字都不打**）。
    ⚠️ 默认写 **stderr**：`menu_dump` 的 stdout 是**表**（产出的 `.md` 会被 `menu_redoc.py` 切块）
        ⇒ 警告写进 stdout 会污染表；`menu_dump` 里 `find_go` 的「命中多个」也是写 stderr 的。
    """
    coll = b.go_collisions()
    if not coll:
        return 0
    w = stream or sys.stderr
    w.write(f'⚠️ 这个目录里有 {len(coll)} 个 **GO pid 撞车**（同一个 pid 在两个内层 CAB 里'
            f'各有一份 GameObject）—— **按 pid 认 GO 是不可靠的**，`Bundle.go[pid]` 只留得下'
            f'**第一份**：\n')
    for pid, names in sorted(coll.items(), key=lambda kv: int(kv[0]) if kv[0].lstrip('-').isdigit() else 0)[:max_show]:
        w.write(f'    pid {pid}: ' + ' / '.join(names) + '\n')
    if len(coll) > max_show:
        w.write(f'    …… 还有 {len(coll) - max_show} 个\n')
    w.write('    本工具取名字 / `m_IsActive` / 组件走的是 `go_by_rt`（**按 RT pid 认人**）⇒ 认得准；'
            '⛔ 你自己写脚本时别按 pid 认 GO（`工具/_probe_deckinfo.py` 这类也要核一眼）。\n')
    return len(coll)


def active_in_hierarchy(b, rt):
    """uGUI 的 **`GameObject.activeInHierarchy`** —— **整条父链**都 active，不是只看自己那一格。

    🔴 **A499（2026-10-13）：这份实现从 `menu_dump._active_in_hierarchy` 挪到这里**
       —— 两个工具都要它，而「同一条规则只留一份」是本仓红线 ⇒ `menu_dump` 那一支改成**转发**。
       用途：`menu_dump` 用它判「这个布局组此刻跑不跑」（`grp_aih` / `⛔GRP-off`）与标 `ANC✗`；
       本工具用它标 `ANC✗` —— `m_IsActive` 只管**自己那一格**，原版画不画看**整条父链**
       （见 `walk` 里 `anc_off` 那一段）。

    ⚠️ 判据 = uGUI `LayoutGroup.CalculateLayoutInputHorizontal` 收孩子那一句
       `if (rect == null || !rect.gameObject.activeInHierarchy) continue;`
       （本地那份 `LayoutGroup.cs:60`）。
    ⚠️ 递归到 `m_Father` 断链为止（预制体根没有父 ⇒ 链端那一件就是「最上面的祖先」）。
    ⚠️ 查不到（RT/GO 缺）时**返回 True**（不冤枉它）—— 与「缺件另有一块出声」分工不同。
    ⚠️ 缓存键名 `_aih_cache` 挂在 **`Bundle` 实例**上（`menu_dump` 的 `--verify-layout` ⑮ 会
       `pop` 它来清缓存 —— 那是**被自检钉住的实现细节**，改名要连着改那边）：
       pid 是**分包局部**的，两张不同包的同号节点可以不一样。

    🔴 **2026-10-14（新账「`active_in_hierarchy` 仍按 pid 认 GO」）—— 认 GO 与缓存键两处都改了：**
       ① **认 GO 改走 `go_obj_of_rt(rtpid)`**（A499 那条撞车安全的路）。原来走
          `b.go[rt.m_GameObject.m_PathID]`（**按 GO pid 认**）：pid 是**分包 / 分内层 CAB 局部**的，
          一个导出目录里可以有两份同号 GO ⇒ 读到**另一份的 `m_IsActive`**（连带整条父链的判定）。
       ② **缓存键从 `gopid` 换成 `rtpid`** —— 两份同号 GO 原来**共用一条缓存**；
          ⚠️ 只改①不改②**不够**：旧键会把第一次的错值一直喂回来（本件实测过这一格）。
       **实测**（`bundle_scenes_scenes_mainmenuwarpforge`，**49** 组 GO pid 撞车）：
       · GO **文件 / 名字**取错 **49/49** 处（与 A621 的数一致）；
       · **`m_IsActive` 值读错 13 处** —— 例：rtpid `1126` 旧读 `True`、真值 `False`
         （**原始 JSON 直读** `GameObject/Level Up Effect.json` 独立复核过）；rtpid `341` 旧读 `False`、真值 `True`。
       ⛔ **没有撞车的包逐位不变**（`bundle_menus_assets_all` 实测 **0 / 16510** 处不同）——
       这正是「本件只把错的改对」的判据。
    ⚠️ **退回老路的唯一一格**：`rt` 是个**不带 `_pid` 的 dict**（只有 `menu_dump` 的 `--verify-layout` ⑮
       那三个打桩件是这么造的）⇒ 键用 `'g:' + gopid`、认 GO 也退回 `b.go[gopid]`。
       ⛔ **别把这一格删掉**（⑮ 会红：它拿的就是没带 `_pid` 的桩）；⚠️ 真数据里 `_pid` **一条都不缺**
       （实测 592/592 颗 RT（1184 个键）· 16510/16510）。
    """
    if isinstance(rt, str):
        rt = b.rt.get(str(rt))
    if not isinstance(rt, dict):
        return True
    rtpid = rt.get('_pid')
    gopid = str(rt.get('m_GameObject', {}).get('m_PathID'))
    cache = b.__dict__.setdefault('_aih_cache', {})
    # 🔴 键 = **这颗 RT 的 pid**（不是 GO 的）—— 同号 GO 各认各的，见上面 ②
    key = str(rtpid) if rtpid is not None else 'g:' + gopid
    if key in cache:
        return cache[key]
    g = b.go_obj_of_rt(rtpid) if rtpid is not None else b.go.get(gopid)
    ok = bool((g or {}).get('m_IsActive', 1))
    if ok:
        p = rt.get('m_Father', {}).get('m_PathID', 0)
        pr = b.rt.get(str(p)) if p else None
        ok = True if pr is None else active_in_hierarchy(b, pr)
    cache[key] = ok
    return ok


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
    # 🔴 **2026-10-14（同一族 · 顺手多改的这一处，只有这一行）**：父名字原来走
    #    `b.go_name(b.go_of_rt(chain[-2]))` —— 那两步都是**按 GO pid 认**（`go_of_rt` = 拿这颗 RT 的
    #    `m_GameObject.m_PathID` 去 `self.go` 里取，而 `self.go` 撞车时只留得下第一份）⇒ 表头那句
    #    「被查节点的父 = 「…」」在**撞车包**里会印**另一个 CAB 那一件**的名字，**且不出声**。
    #    **实测**（`bundle_scenes_scenes_mainmenuwarpforge`，49 组撞车）：全包 **49/592** 颗 RT 两条路
    #    名字不同；**父正好是这 49 颗之一的 RT = 66 颗** ⇒ 以它们为根 dump 时表头父名是错的
    #    （例：根 `Cardback`(rtpid 1221) 的父会印成 `'Background Darkener'`，真值 `'Cardback Container'`）。
    #    ⚠️ 与 `walk`/`go_obj_of_rt` 同一条判据（A499/A621 那一族：「认人一律按**【这颗 RT】**」）；
    #    ⛔ 别退回去（`go_name_of_rt` 里含 `go_obj_of_rt` 的老路兜底 ⇒ **没有撞车时逐位同旧值**）。
    return rect, (b.go_name_of_rt(chain[-2]) or f'<RT {chain[-2]}>'), sc


# ================================================================ 「本表不全」也要出声（A620 · 2026-10-14）
# 🔴 **A620**：`walk` 原来有**两处静默 return** —— ① `depth > maxdepth`（深度截断）
#    ② 子件在 `RectTransform/` 里查不到。`menu_dump.py` 那两处早就有 `stats` + 表尾出声
#    （见它 `new_stats` 的 A60⑤ 那段：默认 `--depth 6` 下四件一个都不印、而输出里**一个字都没提**），
#    **本工具一个字不说**。而本工具的表常被当成「全树」抄进 `.cs`（`--depth` 默认才 **3** ⇒ 截断是
#    **常态**）⇒ 按纪律「不许静默失败」补齐。⚠️ 只出声，⛔ **不改任何一个坐标**（算法一个字没动）。
MAXDEPTH_TOPS = 8      # 表尾最多列几处「被深度截断的子树」（其余只报数）


def new_stats(maxdepth=None):
    """走树时顺带记的几个数（**只为了让结尾能出声**，不影响表里任何一个数）。

    ⛔ **别把本 dict 与 `menu_dump.new_stats()` 合并**：那份还要记布局 / 旋转 / 自适应那几族
       （本工具**不跑布局**，那些量在本工具里根本不存在）⇒ 两边键集本来就不同；
       真正共用的是「数子树」那**一个算法**（`count_subtree` 住在本文件，唯一一份）。
    """
    return {'maxdepth': maxdepth,   # 这一趟用的深度上限（表尾文案要引它）
            'cut_nodes': 0,         # 因深度上限**没印**的节点数（含它们的整棵子树）
            'cut_deepest': 0,       # 这些节点里最深的是第几层（= 该用的 `--depth`）
            'cut_roots': 0,         # 被截掉的第一层有几个（>= len(cut_tops)）
            'cut_tops': [],         # 被截掉的第一层，最多列 `MAXDEPTH_TOPS` 个 (缩进, 名字, 层)
            'miss': 0,              # 子 pid 在 `RectTransform/` 里**查不到**的个数
            't_kids': 0}            # 其中是**纯 `Transform`**（3D，没有 RectTransform）的个数


def count_subtree(b, rtpid, depth, _guard=0):
    """只**数**这棵子树有多少个能进表的节点、最深到第几层（不建表、不改任何 dict）。

    ⚠️ 与 `walk` 同一个口径：`RectTransform/` 里查不到的（纯 `Transform` 3D 件）**不算** ——
       它们本来就不该进这张表，算进来会把「还有几个没印」报大。
    `_guard` 防御 `m_Children` 成环（`chain_up` 也防了同一个坑）。

    🔴 **A620：本函数是「数子树」这条算法的【唯一一份】** —— `menu_dump._count_subtree` 是它的
       **转发**（与 `active_in_hierarchy` 同一条做法：两个工具都要它，而「同一条规则只留一份」是
       本仓红线）。⛔ 别在 `menu_dump.py` 里再写一份。
    """
    rt = b.rt.get(str(rtpid))
    if rt is None or _guard > 64:
        return 0, depth
    n, deep = 1, depth
    for c in b.children(rtpid):
        k, d = count_subtree(b, c, depth + 1, _guard + 1)
        n += k
        deep = max(deep, d)
    return n, deep


def stats_warning(st, stream=None, max_show=MAXDEPTH_TOPS):
    """**「本表不全」的表尾出声**（判据只此一份；本工具两种模式都调它）。返回「有没有话说」。

    ⚠️ 写哪个流由调用方定，两条理由：
      · **表模式 ⇒ stdout**：警告要**跟着表走**（重定向到文件时不能丢，与 `menu_dump` 的表尾同）；
      · **`--cs` 模式 ⇒ stderr**：那个 stdout 是**要贴进 `.cs` 的东西** ⇒ 灌进一段散文会让人抄出错
        （与 `go_coll_warning` 写 stderr 同一条理由）。
    ⚠️ 文案与 `menu_dump` 表尾那几段**各写各的**（它的键集多好几族）—— 数字⛔别互相抄。
    """
    if not st:
        return False
    w = stream or sys.stdout
    said = False
    if st.get('cut_nodes'):
        said = True
        w.write(f'\n⚠️ **本表不全**：深度上限 `--depth {st.get("maxdepth")}` 截掉了 '
                f'**{st["cut_nodes"]}** 个节点（那儿最深到第 **{st["cut_deepest"]}** 层）'
                f'⇒ 要看全用 `--depth {st["cut_deepest"]}`（**没印 ≠ 不存在**）：\n')
        for ind, nm_, d in st['cut_tops'][:max_show]:
            w.write(f'    {"  " * ind}{nm_}   （第 {d} 层起被截）\n')
        if st['cut_roots'] > len(st['cut_tops']):
            w.write(f'    …… 被截掉的第一层共 {st["cut_roots"]} 处\n')
    if st.get('t_kids'):
        said = True
        w.write(f'\n⚠️ 另有 **{st["t_kids"]}** 个纯 `Transform` 子件（3D，例：卡片的 3D 体）'
                f'—— 它们**没有 RectTransform**，本表本来就不该有它们'
                f'（与 `menu_dump` 同口径）。\n')
    miss = st.get('miss', 0) - st.get('t_kids', 0)
    if miss > 0:
        said = True
        w.write(f'\n🔴 **{miss}** 个子 pid 在 `RectTransform/` 里**查不到**、而且不是纯 `Transform` '
                f'—— 要么导出缺了那一件、要么 `m_Children` 指向了别的文件 ⇒ **本表少了这些件**。\n')
    return said


def walk(b, rtpid, rect, scale, depth, maxdepth, out, indent=0, force_root_rect=None,
         keep_scales=True, act_anc=None, stats=None):
    """🔴 **本函数【不跑】布局组**（本工具的口径 = prefab 原值 / 模板位，见文件头）——
       所以它**没有** `menu_dump._rect_children` 那一份「哪些兄弟参与布局」的过滤，
       也**不该有**（那份判据只此一处，住在 `menu_dump.py`）。

       ⚠️ 它**有**的两条与 `menu_dump` 同族的判定，都已对齐（A499 · 2026-10-13）：
         ① **`m_IsActive` 只看自己那一格** —— 与 `menu_dump.walk` 同口径；但「自己 active、
            祖先 inactive」的件**原版一个像素都不画**，两个工具都**必须标出来** ⇒
            本函数多带一个 `act_anc`、`out` 里多一列 `anc_off`（`menu_dump` 的行首 `ANC✗`）。
         ② **布局组的 `m_Enabled`** —— `m_Enabled=0` 的布局件**原版不跑**（判据见
            `menu_dump.apply_layout_to_children` 里 2026-09-27 那条），`kinds` 里给它单独一个
            记号，表尾那块清单据此分档（⛔ 别把它当成「子节点位置由布局算」）。

    🔴 **A620（2026-10-14）：下面两处 return 都**出声**了**（`stats` 收 `new_stats()` 那个 dict；
       传 `None` = 不当账，那份老行为还在 —— `menu_dump.verify_layout` ⑤e 那两处就是这么调的）。
       ⛔ **别把出声改成「悄悄填个默认值」**：这两处丢的是**行进表里的件**（表会少得看不出来）。
    """
    # ⚠️ **两处判定的【次序】与 `menu_dump.walk` 对齐**（A620）：那边先判「件在不在」再判「深不深」。
    #    本函数原来是反的；换过来**不改变任何输出**（两支都 `return`、都不 append）——
    #    换来的是「深度截断」与「缺件」两个计数的**归属口径与 `menu_dump` 逐字一致**
    #    （否则一个「既超深、又缺件」的 pid 在两边会被记进不同的账）。
    rt = b.rt.get(str(rtpid))
    if rt is None:
        # 🔴 **A620**：子件在 `RectTransform/` 里查不到 = **缺件**，原来**一个字不说**就 return。
        #    分两种（与 `menu_dump.walk` 同口径）：「纯 `Transform`」（3D，本来就不该进 UI 表）
        #    与「真缺」—— 前者不冤枉它，后者是**表少列了东西**，要单独出声。
        if stats is not None:
            stats['miss'] += 1
            if os.path.exists(os.path.join(b.path, 'Transform', f'Transform_{rtpid}.json')):
                stats['t_kids'] += 1
        return
    if depth > maxdepth:
        # 🔴 **A620：深度截断不许静默** —— 记下「还有几个没印、要到第几层」，`main()` 的表尾引它。
        #    （本工具 `--depth` 默认才 3 ⇒ 这一支是**常态**，不是边角。）
        if stats is not None:
            n, deep = count_subtree(b, rtpid, depth)
            stats['cut_nodes'] += n
            stats['cut_deepest'] = max(stats['cut_deepest'], deep)
            stats['cut_roots'] += 1
            if len(stats['cut_tops']) < MAXDEPTH_TOPS:
                stats['cut_tops'].append(
                    (indent, b.go_name_of_rt(rtpid) or f'<RT {rtpid}>', depth))
        return
    # 🔴 **A499 追加：名字 / `m_IsActive` / 组件都按【这颗 RT】认 GO**（`go_obj_of_rt`）——
    #    原来走 `b.go.get(rt.m_GameObject.m_PathID)`，**撞车时整套会取到另一个 CAB 的那一份上**
    #    （名字、act、组件列全是那一份的）⇒ 本包实测：`Resource Counter Item` 那棵树
    #    被印成 **`Player Level`**（真名 `Icon`）。⚠️ 没有撞车时两条路指向**同一个对象**
    #    ⇒ 输出逐字节不变（`go_obj_of_rt` 里有退回老路）。
    g = b.go_obj_of_rt(rtpid) or {}
    gopid = rt.get('m_GameObject', {}).get('m_PathID')
    name = (g.get('m_Name') or g.get('_name')) or f'<RT {rtpid}>'
    active = g.get('m_IsActive', 1)
    # 🔴 **祖先的可见性**（A499）：`m_IsActive` 管**自己那一格**，uGUI 的一切
    #    （`activeInHierarchy`）看**整条父链** ⇒「自己 active、祖先 inactive」的件**原版一个像素都不画**。
    #    `walk` 故意会走进 inactive 子树把件列出来（要如实列出），但**必须标出来** ——
    #    否则读表的人会以为那件**是可见的**。`menu_dump.walk` 早就是这么做的（行首 `ANC✗`），
    #    本工具原来**缺这一档**（A499 与 `menu_dump` 对齐）。
    if act_anc is None:
        par = b.parent(rtpid)
        act_anc = active_in_hierarchy(b, par) if par else True
    anc_off = bool(active) and not act_anc

    (r, (w, h)) = rect_of(rt, rect, scale)
    if depth == 0 and force_root_rect is not None:
        # `--root-size`：**不按屏幕算根节点**，直接给它这个矩形（卡片的作者尺寸 ≠ 显示尺寸时用）
        r = force_root_rect
        w, h = force_root_rect[2] - force_root_rect[0], force_root_rect[3] - force_root_rect[1]
    # ⚠️ `or {}`（不是 `rt.get('m_LocalScale', {...})`）—— 键**存在但为 `null`** 时后者会给 `None`
    #    再 `.get` 就抛 `AttributeError`；`menu_dump.walk` 用的是 `or` 那一支（A499 对齐）。
    #    ⚠️ 实测本包 16510 个 RT **0 个**是 null ⇒ today 无影响，但两处写法必须一致。
    scl = rt.get('m_LocalScale') or {'x': 1, 'y': 1}

    # 组件：只报「有意思的」那几个（布局组 / 滚动 / 掩码）
    kinds = []
    for c in g.get('m_Component', []):
        cp = str(c['component']['m_PathID'])
        mb = load(b.path, 'MonoBehaviour', cp)
        if not mb:
            continue
        # 布局组的字段指纹：有 `m_Padding` + (`m_Spacing` 或 `m_ChildAlignment`)
        # ⚠️ 判据与 `menu_dump.fingerprint` 的 `<LayoutGroup>` 那一条**逐字相同**
        #    （那边是「类名优先、指纹兜底」；本文件拿不到 monoscripts 索引 —— `menu_dump`
        #     反向 `import menu_rect`，在这里调它会成环 ⇒ 只能走同一个指纹）。
        #    实测 `bundle_menus_assets_all`：`menu_dump` 认的 1470 个布局组**一个都不漏、无一处误报**。
        if 'm_Padding' in mb and ('m_Spacing' in mb or 'm_ChildAlignment' in mb):
            # 🔴 **A499：`m_Enabled=0` 的布局件【原版不跑】**(判据 = `menu_dump.apply_layout_to_children`
            #    2026-09-27 那条，实例 `Player Profile Window > Menu Area`) ⇒ 单独一个记号，
            #    表尾那块清单据此分档 —— ⛔ 别再对它说「子节点位置由布局算」（那会把读表的人引去
            #    按布局重算一遍，而真值就是本表印的模板位）。实测本包 2 处（另一处 =
            #    `Collection Menu Variant/…/Deck Scroll View/Viewport/Content` 的 GridLayoutGroup）。
            kinds.append('LayoutGroup' if mb.get('m_Enabled', 1) != 0
                         else 'LayoutGroup(m_Enabled=0)')
        elif 'm_Content' in mb and 'm_Viewport' in mb:
            kinds.append('ScrollRect')
        elif 'm_ShowMaskGraphic' in mb:
            kinds.append('Mask')
        elif 'm_Maskable' not in mb and 'm_ShowMask' in str(mb)[:200]:
            # 🔴 **A625（2026-10-14）：这一支 = 脆 clause** —— 拿 **dict 的 repr 前 200 字符**做子串
            #    搜索（依赖 `str(dict)` 的键序与格式），与上面那几条**按真键名**判的指纹不同族。
            #    ⛔ **既不许合并、也不许删**：
            #      · **不许合并**：`menu_dump.fingerprint` 那份**没有**这一条 —— 而两份合一做不到
            #        （`menu_dump` 反向 `import menu_rect` ⇒ 在这里调它成环，见上面 A499 那段）；
            #      · **不许删**：删 = **可能静默少报一个 Mask**（这一支管的是
            #        「`m_Maskable` 都没了却仍有 `m_ShowMask`」那种形状），而「它恒不命中」
            #        要**全库扫过**才敢说（⛔ 别拿一个包的实测当全库结论）。
            #    📌 **实测（2026-10-14 · 全库 84 个包 / 66459 个 MonoBehaviour）**：
            #       第一 clause 命中 **220** · 脆 clause 命中 **220**（**全部被第一 clause 覆盖**）
            #       ⇒ **「只有脆 clause 命中」= 0 次**（`bundle_menus_assets_all` 早先那次也是 0）。
            #    ⇒ 处置 = **留着 + 单独一个记号**：哪一天它真单独命中，表里**看得出来**
            #       （⛔ 别改成悄悄也印 `Mask` —— 那就又变回静默了）。
            kinds.append('Mask(⚠️A625脆clause)')
    # 🔴 **优先级照 `menu_dump`**：一个 GO 上挂了 ≥2 颗布局组时，那边是「**第一颗 `m_Enabled≠0` 的说了算**
    #    （禁用的 `continue` 跳过、认出来的就 `break`）」⇒ 只要**有一颗是启用**的，这个节点就**会跑布局**。
    #    ⇒ 记号也照这个口径收敛：有启用件时**不算**「`m_Enabled=0`」那一档。
    #    ⚠️ 实测本包（`bundle_menus_assets_all`）**0 个** GO 挂 ≥2 颗布局组 ⇒ 今天无影响，是防将来静默错档。
    if 'LayoutGroup' in kinds and 'LayoutGroup(m_Enabled=0)' in kinds:
        kinds.remove('LayoutGroup(m_Enabled=0)')
    kinds = sorted(set(kinds))

    # 🔴 **A626②：这个元组【只许在末尾追加】字段** —— `menu_dump.verify_layout` ⑤e 有 2 处
    #    按**下标**取它（`e[0]` 缩进 / `e[1]` 名字 / `e[3]` 布局宽，`menu_dump.py` 里现读）。
    #    往中间插一项 ⇒ 那两处**静默取错列**（`--verify-layout` 会红得莫名其妙）。
    #    当前次序（= A499 补第 10 项时的口径）：0 缩进 · 1 名字 · 2 矩形 · 3 宽 · 4 高 ·
    #    5 自己 active · 6 RT dict · 7 自己的 localScale · 8 组件记号 · 9 祖先 inactive。
    out.append((indent, name, r, w, h, active, rt, scl, kinds, anc_off))

    for c in b.children(rtpid):
        ks = (scale[0] * scl.get('x', 1), scale[1] * scl.get('y', 1)) if keep_scales \
            else (1.0, 1.0)
        walk(b, c, r, ks, depth + 1, maxdepth, out, indent + 1,
             force_root_rect=None, keep_scales=keep_scales,
             act_anc=(act_anc and bool(active)), stats=stats)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('bundle')
    ap.add_argument('root')
    ap.add_argument('--depth', type=int, default=3)
    ap.add_argument('--active-only', action='store_true',
                    help='只列 **`m_IsActive=1`（= `activeSelf`，只管自己那一格）** 的节点 —— '
                         '与 `menu_dump.py --active-only` **同口径**（⛔ 不是 `activeInHierarchy`：'
                         '「自己 active、祖先 inactive」的件仍会列出来，但行首打 `ANC✗` '
                         '= **原版一个像素都不画**，见下）')
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
    # 🔴 **A499 追加：GO pid 撞车必须出声**（判据只此一份 = `go_coll_warning`）——
    #    撞了 ⇒ 「按 pid 认 GO」这套口径在这个目录里不够用。写 stderr（stdout 是表）。
    if go_coll_warning(b):
        print('# ⚠️ 本目录有 **GO pid 撞车** —— 详见 stderr（本表取名字/act/组件走 `go_by_rt`，已避开）')

    # 🔴 **A499 追加：`find_rt`**（不是 `find_go` + `rt_of_go`）—— pid 撞车时**另一份 GO 不在
    #    `self.go` 那张索引里** ⇒ 按名字找会整个找不到（实例 `Resource Counter Item`）。
    rtpid = b.find_rt(args.root)
    if rtpid is None:
        # 根自己可能就是被当容器用：直接用场景尺寸
        sys.exit(f'找不到 GameObject「{args.root}」（pid 或名字）—— 或它没有 RectTransform')

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
    # 🔴 **A620**：`stats` 记账（`walk` 的两处早退都在里面记数），表尾 / stderr 出声见 `stats_warning`。
    st = new_stats(args.depth)
    walk(b, rtpid, base_rect, base_scale, 0, args.depth, out,
         force_root_rect=(root_rect if args.root_size else None), keep_scales=keep, stats=st)

    ox, oy = (out[0][2][0], out[0][2][1]) if (args.relative and out) else (0.0, 0.0)

    if args.cs:
        # 吐 C# 数据表：`N(缩进, "名字", aMinX,aMinY, aMaxX,aMaxY, pivX,pivY, posX,posY, szX,szY),`
        print(f'// {args.root} —— 锚点五元组（**prefab JSON 里的原值**；本工具【不跑】布局；缩进 = 层级）')
        print(f'// 出处 {path}')
        print(f'// ⚠️ 带 `⚠️localScale` 注释的行：**那一件的 `m_LocalScale ≠ 1`** —— 这几个五元组是'
              f'**布局框**，画出来还要 × 那个倍数（`UguiRect.Child` 算完的框不是屏幕框）。')
        print(f'// ⚠️ 被布局组管的节点：prefab 里存的 `m_AnchoredPosition`/`m_SizeDelta` **就是**这几个数'
              f'（本工具不跑布局），但**运行期会被布局组改写**（`menu_dump.py` 那张表印的是改写后的值）。')
        print(f'// ⚠️ 行末 `// 出厂 inactive` = **这一件自己的 `m_IsActive=0`**；'
              f'`// ⛔ANC-off` = 它自己 active、**祖先 inactive**（uGUI 的 `activeInHierarchy` 看整条父链）'
              f'⇒ **原版一个像素都不画**。两者可能各自出现，含义不同。')
        print(f'// 用法：`UguiRect.Child(父矩形, new Vector2(aMinX,aMinY), new Vector2(aMaxX,aMaxY),'
              f' new Vector2(pivX,pivY), new Vector2(posX,posY), new Vector2(szX,szY))`')
        for (ind, name, r, w, h, active, rt, scl, kinds, anc_off) in out:
            a_min, a_max = rt['m_AnchorMin'], rt['m_AnchorMax']
            piv, pos, sz = rt['m_Pivot'], rt['m_AnchoredPosition'], rt['m_SizeDelta']
            nm = name.replace('"', "'")[:40]
            sx, sy = local_scale(rt)
            tail = '' if active else '   // 出厂 inactive'
            if anc_off:
                # 🔴 A499：`m_IsActive=1` 但**祖先 inactive** ⇒ 原版一个像素都不画（`activeInHierarchy`）。
                #    与「出厂 inactive」**是两件事**（那一件是它自己那一格 = 0），别混。
                tail += '   // ⛔ANC-off（祖先 inactive ⇒ 原版不画它）'
            if not scale_is_one(sx, sy):
                # 🔴 A145：抄进 `.cs` 的那条路正是「设计值被当成画出来的宽」的案发现场 ⇒ 逐行标出来
                vw, vh = visual_size(sx, sy, w, h)
                tail += (f'   // ⚠️localScale ×{sx:.4g}'
                         + ('' if abs(sx - sy) < 1e-9 else f',×{sy:.4g}')
                         + f' ⇒ 视觉 {vw:.2f}×{vh:.2f}')
            print(f'    N({ind}, "{nm}", {a_min["x"]:g},{a_min["y"]:g}, {a_max["x"]:g},{a_max["y"]:g},'
                  f' {piv["x"]:g},{piv["y"]:g}, {pos["x"]:g},{pos["y"]:g}, {sz["x"]:g},{sz["y"]:g}),'
                  + tail)
        # 🔴 **A620**：`--cs` 的 stdout **要贴进 `.cs`** ⇒ 这段「本表不全」的散文只能走 **stderr**
        #    （往 stdout 灌 = 让人抄出错，与 `go_coll_warning` 写 stderr 同一条理由）。
        #    ⛔ 别因为「表模式打在 stdout」就把它也搬过去。
        #    📌 顺手发现（本件没动）：`--cs` 的 stdout 里**本来**就有 0–3 行 `#` 开头的话
        #      （撞车点名那一行 + `# （已沿 m_Father 爬父链…）` 那两行）—— 那不是合法 C#，
        #      整段贴进 `.cs` 本来就会编不过；要清理得连那三处一起（另立账）。
        stats_warning(st, stream=sys.stderr)
        return 0

    print(f'# {args.root}  ' + ('相对根左上角' if args.relative else '绝对矩形')
          + '（1920×1080 · 左上原点 · y 向下）')
    print(f'# 出处 {path}')
    print(f'# 🔴 「宽」「高」与四个坐标 = **布局框**（= 设计值）；画出来的是【视觉框 = 布局框 × '
          f'这一件自己的 `m_LocalScale`】—— 见行末 `视觉框=`（缩放 = 1 的行不印）')
    # 🔴 A499：行内标记的图例（`menu_dump.py` 纯文本模式早就有一行；本工具原来只在表里打 `INACT`
    #    而**一个字都不解释**，新加的 `ANC✗` 更没有出处可查 ⇒ 补这一行，与 `menu_dump` 同口径）。
    print(f'# 行内标记：`INACT` = **这一件自己的 `m_IsActive=0`**（出厂就是这样）· '
          f'`ANC✗` = 它自己 active 但**祖先 inactive**（uGUI 的 `activeInHierarchy` 看**整条父链**）'
          f'⇒ **原版一个像素都不画**（与 `menu_dump.py` 同一个记号）· '
          f'`⚠️LayoutGroup(m_Enabled=0)` = 布局组件**自己关着**，原版**不跑**它（见表尾）· '
          f'`Mask(⚠️A625脆clause)` = 那一件的 Mask 是**脆 clause** 判出来的（全库实测 0 次，见那段注释）')
    print(f'{"深度":<4}{"名字":<44}{"x1":>9}{"y1":>9}{"x2":>9}{"y2":>9}{"宽":>9}{"高":>9}  act  组件')
    shown = []
    for (ind, name, r, w, h, active, rt, scl, kinds, anc_off) in out:
        if args.active_only and not active:
            continue
        shown.append((ind, name, r, w, h, active, rt, scl, kinds, anc_off))
        r = (r[0] - ox, r[1] - oy, r[2] - ox, r[3] - oy)
        nm = '  ' * ind + name
        # ⚠️ `INACT` 与 `ANC✗` **不是一回事**（A499）：前者是**自己那一格**（`activeSelf`），
        #    后者是**祖先有 inactive**（`activeInHierarchy`）—— 两者都可能单独出现。
        flag = ('ANC✗' if anc_off else '') + ('INACT' if not active else '')
        vc = visual_cell(*local_scale(rt), w, h)          # 🔴 判据只此一份（见文件头 A145）
        sc = '' if vc == '—' else f'  视觉框={vc}'
        print(f'{ind:<4}{nm[:43]:<44}{r[0]:>9.2f}{r[1]:>9.2f}{r[2]:>9.2f}{r[3]:>9.2f}'
              f'{w:>9.2f}{h:>9.2f}  {flag:<6} {",".join(kinds)}{sc}')

    # 布局组提醒：祖先里有布局组的，子节点的位置**不是**这里给的。
    # 🔴 **A499（2026-10-13）：这一段原来把【所有】带布局组的节点一并说成「子节点位置由布局算」——
    #    对 `m_Enabled=0` 的那些**是错的**。uGUI 的布局件挂在 `m_Enabled=0` 的组件上时，
    #    原版**根本不跑它**（判据 = `menu_dump.apply_layout_to_children` 那条 2026-09-27 记下的
    #    `m_Enabled=0` 分支；实例 `Player Profile Window > Menu Area`：照跑会把 `Tab Buttons`
    #    的 y 从 180.24 顶到 187.24 —— 那正是 `menu_dump` 当年踩过一次的坑）。
    #    对它说「子节点位置由布局算」= 把读表的人**引去按布局重算一遍**，而真值就是本表印的模板位。
    #    ⇒ 照 `menu_dump.py` 表尾那一套拆成**互斥的三档**（那边是 `laid` / `nolaid` / `⛔GRP-off`）：
    #      ① 会跑的组 → 原句（本表给的是「布局跑之前的模板位」）；
    #      ② `m_Enabled=0` 的组 → 原版**永远不跑** ⇒ 本表那些子件**就是**终值；
    #      ③ 组自己不在 `activeInHierarchy` 里 → **此刻**不跑（激活之后会跑），单独出声。
    lg_run, lg_dis, lg_aihoff = [], [], []
    for e in out:
        # 🔴 **A626①：这一段走 `out`（不是 `shown`）—— 故意的**，⛔ 别「统一」成 `shown`：
        #    布局组**不管它的子件印没印出来**都照样排（`--active-only` 滤掉的是「没被印出来的行」，
        #    不是「那件不存在」）⇒ 少列它们 = 让读表的人**漏掉**「这些子件的模板位不是终值」这句警告。
        #    同一个文件末尾那段「视觉框」清单走 `shown`（它说的是**上面印出来的那些行**）——
        #    两者**口径不同是有意的**（W499 §六·3 记的这条「不一致」= 记一笔，不是缺陷）。
        if not any(k.startswith('LayoutGroup') for k in e[8]):
            continue
        if 'LayoutGroup(m_Enabled=0)' in e[8]:
            lg_dis.append(e)
        elif active_in_hierarchy(b, e[6]):
            lg_run.append(e)
        else:
            lg_aihoff.append(e)
    if lg_run:
        print('\n⚠️ 下面这些节点**带布局组** —— 它们的子节点位置由布局算，本表给的是「布局跑之前的模板位」：')
        for e in lg_run:
            print('   ', '  ' * e[0] + e[1])
    if lg_dis:
        print(f'\n🔴 **下面这些节点带布局组、但那个布局组件 `m_Enabled=0`**（{len(lg_dis)} 个）—— '
              f'uGUI **不跑它**（原版此刻与以后都不会排）⇒ 它们的子节点**就停在本表印的模板位上**，'
              f'⛔ 别按布局组参数重算一遍：')
        for e in lg_dis:
            print('   ', '  ' * e[0] + e[1])
    if lg_aihoff:
        print(f'\n⛔ **下面这些节点是布局组、但它自己不在 `activeInHierarchy` 里**（{len(lg_aihoff)} 个，'
              f'与 `menu_dump.py` 的 `⛔GRP-off` 同一个记号）—— 原版**此刻不跑它们的布局**；'
              f'激活之后会跑，届时子节点位置**不是**本表给的值：')
        for e in lg_aihoff:
            print('   ', '  ' * e[0] + e[1])

    # ---- 🔴 「布局框 ≠ 视觉框」的末尾清单（A145；判据 = `visual_cell`，与 `menu_dump.py` 同一份）----
    # 🔴 **A626①：这一段走 `shown`（不是 `out`）—— 故意的**：它说的是「**上面印出来的那些行**」
    #    的视觉框，`--active-only` 滤掉的行**不该**出现在这里（否则会出现「上面没有这一行」的怪话）。
    #    ⚠️ 与上面那段布局组警告的 `out` **口径不同是有意的**，两条都由这一条注释钉住（别再「统一」）。
    sc_nodes = [e for e in shown if not scale_is_one(*local_scale(e[6]), eps=SCL_WARN_EPS)]
    if sc_nodes:
        print(f'\n⚠️ **上面「宽」「高」与四个坐标是【布局框】，不是画出来的大小** —— '
              f'这 {len(sc_nodes)} 处自带 `m_LocalScale`，**视觉框 = 布局框 × localScale**'
              f'（行末那格逐行标着）：')
        for (ind, name, r, w, h, active, rt, scl, kinds, anc_off) in sc_nodes[:SCL_WARN_MAX]:
            vc = visual_cell(*local_scale(rt), w, h)
            print(f'    {"  " * ind}{name}  布局 {w:.2f}×{h:.2f}  {vc}')
        if len(sc_nodes) > SCL_WARN_MAX:
            print(f'    …… 还有 {len(sc_nodes) - SCL_WARN_MAX} 处')

    # ---- 🔴 **本表不全**的末尾出声（A620；判据 = `stats_warning`，两种模式共用那一份）----
    # 表模式走 **stdout**（警告**跟着表走**：重定向到文件时不能丢）；`--cs` 模式走 stderr（见上面）。
    stats_warning(st)
    return 0


if __name__ == '__main__':
    sys.exit(main())
