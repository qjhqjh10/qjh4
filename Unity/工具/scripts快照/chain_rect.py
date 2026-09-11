#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
chain_rect.py — 从原始 Unity JSON 沿 m_Father 链式换算任意 GO 的绝对屏幕坐标
用法: py312 chain_rect.py <场景目录> <GO PathID 或 GO 名>
输出: GO名 [父链] -> Godot 屏幕坐标 x[y1,y2]

v2 (2026-08-21): 补 m_LocalScale 支持 — 缩放绕自身 pivot 应用, 沿链累计
(mode_select 法力曲线抽屉 m_LocalScale=1.2 为唯一缩放节点的教训, 坑 37)
"""
import json, os, sys, glob

sys.stdout.reconfigure(encoding='utf-8')
SRC = sys.argv[1]
TARGET = sys.argv[2]
W, H = 1920.0, 1080.0

go_index = {}   # GO PathID -> GO data
rt_index = {}   # RT PathID -> RT data
tr_index = {}   # Transform PathID -> data (m_LocalScale)
tr_by_go = {}   # GO PathID -> Transform data
go_name = {}    # GO PathID -> name
name_idx = {}   # name -> [GO PathID]
rt_scale = {}   # RT PathID -> {x,y} (extract_rt_scale.py 从游戏本体 typetree 提取; JSON 导出丢失 m_LocalScale)

# 加载 scale 映射 (坑 37: 曲线抽屉 m_LocalScale=1.2 等 2782 节点, 2026-08-21)
SCALE_MAP_PATH = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                              'data', 'ui_layout', 'rt_scale_map.json')
if os.path.exists(SCALE_MAP_PATH):
    try:
        rt_scale = json.load(open(SCALE_MAP_PATH, encoding='utf-8'))
    except Exception:
        rt_scale = {}

# 坑 0826 (战场全元素检查): rt_scale_map 键=裸 PathID, UI 菜单与战斗场景 RT id 撞号 → 战斗场景
#   RT '2862'/'2806' 误取 0.01 → 全部矩形塌缩 (959.9,540)。修: 07_场景 (战斗/3D) 的 m_LocalScale
#   在 Transform JSON 里完好 → 走 go_scale 兜底, 不用 map (map 仅为 UI 菜单场景补导出丢的 LocalScale)。
USE_SCALE_MAP = '07_场景' not in SRC

for t in ['GameObject', 'RectTransform', 'Transform']:
    td = os.path.join(SRC, t)
    if not os.path.isdir(td):
        continue
    files = glob.glob(os.path.join(td, '*.json'))
    # 双份 pid 文件 (Type_<pid>_<pid>.json 拷贝) 排序靠后, 单份 (Type_<pid>.json) 优先 — 坑: 07_场景 双份 pid 文件串链 (2026-08-25)
    files.sort(key=lambda f: 0 if (len(os.path.basename(f).rsplit('.json', 1)[0].rsplit('_', 1)) == 2
                                   and os.path.basename(f).rsplit('.json', 1)[0].rsplit('_', 1)[1].lstrip('-').isdigit()) else 1)
    for f in files:
        base = os.path.basename(f).rsplit('.json', 1)[0]
        m = base.rsplit('_', 1)
        pid = None
        if len(m) == 2 and m[1].lstrip('-').isdigit():
            pid = int(m[1])
        try:
            d = json.load(open(f, encoding='utf-8'))
        except Exception:
            continue
        if pid is None:
            continue
        if t == 'GameObject':
            if pid not in go_index:
                go_index[pid] = d
                go_name[pid] = d.get('m_Name', '?')
                nm = d.get('m_Name')
                if isinstance(nm, str) and nm:
                    name_idx.setdefault(nm, []).append(pid)
        elif t == 'RectTransform':
            if pid not in rt_index:
                rt_index[pid] = d
        else:  # Transform (m_LocalScale 来源)
            if pid not in tr_index:
                tr_index[pid] = d
                gop = d.get('m_GameObject', {}).get('m_PathID')
                if gop is not None:
                    tr_by_go.setdefault(gop, []).append(d)


def find_goid(target):
    if target.lstrip('-').isdigit():
        return int(target)
    cands = name_idx.get(target, [])
    if not cands:
        for nm, pids in name_idx.items():
            if target.lower() in nm.lower():
                cands += pids
    return cands[0] if cands else None


def go_rt(goid):
    """GO -> 它的 RT PathID"""
    d = go_index.get(goid)
    if not d:
        return None
    for c in d.get('m_Component', []):
        pid = c.get('component', {}).get('m_PathID')
        if pid in rt_index:
            return pid
    return None


def go_scale(goid):
    """GO 的 Transform m_LocalScale (x 分量; 默认 1.0)"""
    for tr in tr_by_go.get(goid, []):
        sc = tr.get('m_LocalScale') or {}
        sx = sc.get('x', 1)
        sy = sc.get('y', sx)
        if sx != 1 or sy != 1:
            return (sx, sy)
    return (1.0, 1.0)


def rt_rect(rtid):
    """RT -> dict{s:(x1,y1,x2,y2) 屏幕坐标(含全部祖先+自身 scale),
                  l:(x1,y1,x2,y2) 布局坐标(未缩放), scale: 累计 scale, pivot:(x,y)}
       断链返回 None"""
    rt = rt_index.get(rtid)
    if rt is None:
        return None
    fid = rt.get('m_Father', {}).get('m_PathID')
    if fid is not None and fid in rt_index:
        par = rt_rect(fid)
        if par is None:
            return None
        ps, pl, psc = par['s'], par['l'], par['scale']
        ppvx, ppvy = par['pivot']
    else:
        # 根元素 (父不是 RT): 父参照 = 全屏 1920x1080 (Canvas)
        amn0 = rt.get('m_AnchorMin') or {}
        amx0 = rt.get('m_AnchorMax') or {}
        ap0 = rt.get('m_AnchoredPosition') or {}
        sd0 = rt.get('m_SizeDelta') or {}
        if (amn0.get('x', 0) == 0 and amn0.get('y', 0) == 0
                and amx0.get('x', 0) == 0 and amx0.get('y', 0) == 0
                and ap0.get('x', 0) == 0 and ap0.get('y', 0) == 0
                and sd0.get('x', 0) == 0 and sd0.get('y', 0) == 0):
            return {'s': (0.0, 0.0, W, H), 'l': (0.0, 0.0, W, H),
                    'scale': 1.0, 'pivot': (0.5, 0.5)}
        ps, pl, psc, ppvx, ppvy = (0.0, 0.0, W, H), (0.0, 0.0, W, H), 1.0, 0.5, 0.5
    pw, ph = pl[2] - pl[0], pl[3] - pl[1]
    amn = rt.get('m_AnchorMin') or {}
    amx = rt.get('m_AnchorMax') or {}
    ap = rt.get('m_AnchoredPosition') or {}
    sd = rt.get('m_SizeDelta') or {}
    pv = rt.get('m_Pivot') or {}
    ax0, ay0 = amn.get('x', 0), amn.get('y', 0)
    ax1, ay1 = amx.get('x', ax0), amx.get('y', ay0)
    apx, apy = ap.get('x', 0), ap.get('y', 0)
    sdx, sdy = sd.get('x', 0), sd.get('y', 0)
    pvx, pvy = pv.get('x', 0.5), pv.get('y', 0.5)
    # 布局矩形 (父布局空间): 锚点 min/max 点 (Godot y 向下)
    amin_x = pl[0] + ax0 * pw
    amax_x = pl[0] + ax1 * pw
    amin_gy = pl[1] + (1.0 - ay0) * ph
    amax_gy = pl[1] + (1.0 - ay1) * ph
    lx1 = amin_x + (apx - pvx * sdx)
    lx2 = amax_x + (apx + (1.0 - pvx) * sdx)
    ly1 = amax_gy - (apy + (1.0 - pvy) * sdy)
    ly2 = amin_gy - (apy - pvy * sdy)
    # 映射到屏幕: 父缩放绕父布局 pivot (pivot 点不变)
    ppiv_sx = ps[0] + (ps[2] - ps[0]) * ppvx
    ppiv_sy = ps[1] + (ps[3] - ps[1]) * ppvy
    ppiv_lx = pl[0] + (pl[2] - pl[0]) * ppvx
    ppiv_ly = pl[1] + (pl[3] - pl[1]) * ppvy
    sx1 = ppiv_sx + (lx1 - ppiv_lx) * psc
    sy1 = ppiv_sy + (ly1 - ppiv_ly) * psc
    sx2 = ppiv_sx + (lx2 - ppiv_lx) * psc
    sy2 = ppiv_sy + (ly2 - ppiv_ly) * psc
    # 自身 scale 绕自身 pivot (优先 scale 映射; 兜底 Transform JSON 关联)
    scx, scy = 1.0, 1.0
    smap = rt_scale.get(str(rtid)) if USE_SCALE_MAP else None
    if smap:
        scx = float(smap.get('x', 1))
        scy = float(smap.get('y', scx))
    else:
        scx, scy = go_scale(rt.get('m_GameObject', {}).get('m_PathID'))
    if scx != 1.0 or scy != 1.0:
        spx = sx1 + (sx2 - sx1) * pvx
        spy = sy1 + (sy2 - sy1) * pvy
        sx1 = spx + (sx1 - spx) * scx
        sx2 = spx + (sx2 - spx) * scx
        sy1 = spy + (sy1 - spy) * scy
        sy2 = spy + (sy2 - spy) * scy
    return {'s': (sx1, sy1, sx2, sy2), 'l': (lx1, ly1, lx2, ly2),
            'scale': psc * scx, 'pivot': (pvx, pvy)}


def main():
    goid = find_goid(TARGET)
    if goid is None:
        print('未找到 GO:', TARGET)
        return
    name = go_name.get(goid, '?')
    rtid = go_rt(goid)
    if rtid is None:
        print('%s (GO %s) 无 RectTransform' % (name, goid))
        return
    rect = rt_rect(rtid)
    chain, n = [], rtid
    for _ in range(40):
        rt = rt_index.get(n)
        if rt is None:
            break
        f = rt.get('m_Father', {}).get('m_PathID')
        if f is None or f not in rt_index:
            break
        fgo = rt_index[f].get('m_GameObject', {}).get('m_PathID')
        chain.append('%s(%s)' % (go_name.get(fgo, '?'), f))
        n = f
    if rect:
        sx1, sy1, sx2, sy2 = rect['s']
        print('%s (GO %s)\n  父链: %s\n  Godot: x[%.1f, %.1f] y[%.1f, %.1f] (宽 %.1f 高 %.1f)%s' % (
            name, goid, ' <- '.join(chain[::-1]) if chain else '(根)',
            sx1, sx2, sy1, sy2, sx2 - sx1, sy2 - sy1,
            ('  累计 scale=%.2f' % rect['scale']) if rect['scale'] != 1.0 else ''))
    else:
        print('%s (GO %s) 换算失败' % (name, goid))


if __name__ == '__main__':
    main()
