# -*- coding: utf-8 -*-
"""场景全树解析: 解包场景目录 → 完整布局清单 (层级/坐标/纹理/文字/特效)
用法: python dump_scene_tree.py <场景目录> [输出md]
输出: 树形清单, 每行: 缩进-GO名 [Godot坐标 宽x高] active 组件摘要
"""
import json, glob, os, sys

sys.stdout.reconfigure(encoding='utf-8')

SCENE = sys.argv[1] if len(sys.argv) > 1 else 'D:/2/解包整理/07_场景/mainmenuwarpforge'
OUT = sys.argv[2] if len(sys.argv) > 2 else 'D:/2/Warpforge_tools/data/ui_layout/主菜单全树.md'
W, H = 1920, 1080  # 原版设计分辨率

_cache = {}
_file_idx = None

def _build_file_idx():
    """一次扫描全部类型目录, pathid → 文件路径"""
    global _file_idx
    _file_idx = {}
    for t in ['GameObject', 'RectTransform', 'Transform', 'MonoBehaviour']:
        td = os.path.join(SCENE, t)
        if not os.path.isdir(td):
            continue
        for f in os.listdir(td):
            if not f.endswith('.json'):
                continue
            m = f.rsplit('_', 1)
            if len(m) != 2 or not m[1].replace('.json', '').lstrip('-').isdigit():
                continue
            _file_idx.setdefault(t, {})[int(m[1].replace('.json', ''))] = os.path.join(td, f)

def load(t, pid):
    key = (t, pid)
    if key in _cache:
        return _cache[key]
    if _file_idx is None:
        _build_file_idx()
    f = _file_idx.get(t, {}).get(pid)
    if f:
        try:
            d = json.load(open(f, encoding='utf-8'))
            _cache[key] = d
            return d
        except Exception:
            pass
    _cache[key] = None
    return None

def go_file(pid):
    for f in glob.glob(os.path.join(SCENE, 'GameObject', f'*_{pid}.json')):
        return f
    return None

# 1) GO 索引 (兼容无 PathID 后缀的文件: 场景为"无后缀主文件 + _<pid> 副本"双文件
#    → 无后缀文件按名字匹配后缀副本复用其 pid; 真无 pid 时合成负 pid)
#    2026-08-18 修复: 副本(_pid 文件)优先, 主文件仅作无副本时的兜底——
#    03_界面UI/菜单 里主文件与副本 m_Component 不同 (主文件缺 RT 组件), 按文件名字典序
#    主文件会先处理覆盖副本 → 组件丢失 → 子树断链 (如 Player Profile Window 内容全丢)
go_names, go_comps = {}, {}
_anon_id = 0
_pid_by_name = {}
_pid_files = {}
for f in glob.glob(os.path.join(SCENE, 'GameObject', '*.json')):
    base = os.path.basename(f)
    m = base.rsplit('_', 1)
    if len(m) == 2 and m[1].replace('.json', '').lstrip('-').isdigit():
        pid = int(m[1].replace('.json', ''))
        _pid_by_name.setdefault(base.rsplit('_', 1)[0] + '.json', pid)
        _pid_files.setdefault(pid, f)  # 副本文件 (组件完整)
# 第一遍: 只处理副本文件 (组件完整, 权威)
for pid, f in _pid_files.items():
    try:
        d = json.load(open(f, encoding='utf-8'))
    except Exception:
        continue
    name = d.get('m_Name', '')
    if not name:
        name = os.path.basename(f)[:-5]
    if pid in go_names:
        continue
    go_names[pid] = name
    go_comps[pid] = [c['component']['m_PathID'] for c in d.get('m_Component', [])]
# 第二遍: 无后缀主文件 — 有同名副本时跳过 (副本已处理); 真无 pid 时合成负 pid
for f in glob.glob(os.path.join(SCENE, 'GameObject', '*.json')):
    base = os.path.basename(f)
    m = base.rsplit('_', 1)
    if len(m) == 2 and m[1].replace('.json', '').lstrip('-').isdigit():
        continue  # 副本已处理
    pid = _pid_by_name.get(base, None)
    if pid is None:
        _anon_id -= 1
        pid = _anon_id
    if pid in go_names:
        continue  # 主文件与副本同名同 pid, 副本优先已处理
    try:
        d = json.load(open(f, encoding='utf-8'))
    except Exception:
        continue
    name = d.get('m_Name', '')
    if not name:
        name = base[:-5]
    go_names[pid] = name
    go_comps[pid] = [c['component']['m_PathID'] for c in d.get('m_Component', [])]

# 2) RectTransform + Transform
rects = {}
for f in glob.glob(os.path.join(SCENE, 'RectTransform', '*.json')):
    base = os.path.basename(f)
    m = base.rsplit('_', 1)
    if len(m) != 2 or not m[1].replace('.json', '').lstrip('-').isdigit():
        continue
    try:
        d = json.load(open(f, encoding='utf-8'))
    except Exception:
        continue
    rects[int(m[1].replace('.json', ''))] = d

transforms = {}
for f in glob.glob(os.path.join(SCENE, 'Transform', '*.json')):
    base = os.path.basename(f)
    m = base.rsplit('_', 1)
    if len(m) != 2 or not m[1].replace('.json', '').lstrip('-').isdigit():
        continue
    try:
        d = json.load(open(f, encoding='utf-8'))
    except Exception:
        continue
    transforms[int(m[1].replace('.json', ''))] = d

# 3) 组件索引 (MonoBehaviour 摘要)
def mb_summary(d):
    ks = d.keys()
    out = []
    if 'm_Sprite' in ks:
        sp = d.get('m_Sprite', {})
        if sp and sp.get('m_PathID'):
            out.append(f'sprite:{sp["m_PathID"]}')
    if 'm_text' in ks:
        t = d.get('m_text', '')
        if t:
            out.append(f'text:{t[:40]!r}')
    if 'm_OnClick' in ks:
        out.append('Button')
    if 'm_SliderValue' in ks:
        out.append('Slider')
    if 'm_horizontalScrollbarSpacing' in ks or 'm_VerticalScrollPosition' in ks:
        out.append('ScrollRect')
    if 'm_Script' in ks:
        out.append('script')
    return out

# 4) 树构建: 找所有根 (father=0), 递归
def abs_rect(pid, pabs=(0, 0, W, H)):
    r = rects.get(pid)
    if not r:
        return None
    amin, amax = r.get('m_AnchorMin', {}), r.get('m_AnchorMax', {})
    pp = r.get('m_AnchoredPosition', {})
    sd = r.get('m_SizeDelta', {})
    pv = r.get('m_Pivot', {})
    p0x, p0y, p1x, p1y = pabs
    pw, ph = p1x - p0x, p1y - p0y
    ax0 = p0x + amin.get('x', 0) * pw; ay0 = p0y + amin.get('y', 0) * ph
    ax1 = p0x + amax.get('x', 0) * pw; ay1 = p0y + amax.get('y', 0) * ph
    w = (ax1 - ax0) + sd.get('x', 0); h = (ay1 - ay0) + sd.get('y', 0)
    cx = ax0 + (ax1 - ax0) * pv.get('x', 0.5) + pp.get('x', 0)
    cy = ay0 + (ay1 - ay0) * pv.get('y', 0.5) + pp.get('y', 0)
    return (cx - w * pv.get('x', 0.5), cy - h * pv.get('y', 0.5),
            cx + w * (1 - pv.get('x', 0.5)), cy + h * (1 - pv.get('y', 0.5)))

def godot(a):
    return (a[0], H - a[3], a[2] - a[0], a[3] - a[1])

children_map = {}
for pid, d in list(rects.items()) + list(transforms.items()):
    for c in d.get('m_Children', []):
        children_map.setdefault(pid, []).append(c['m_PathID'])

lines = []
def walk(go_pid, depth, pabs, is_root=False):
    name = go_names.get(go_pid, f'<GO{go_pid}>')
    go = load('GameObject', go_pid)
    active = go.get('m_IsActive', True) if go else True
    # 找该 GO 的 RT / Transform (battlearena1 混合: UI 用 RT, 3D 对象用 Transform)
    rt = None
    tr = None
    for cpid in go_comps.get(go_pid, []):
        if cpid in rects and rt is None:
            rt = cpid
        elif cpid in transforms and tr is None:
            tr = cpid
    npabs = pabs
    extra = []
    parts = []
    if rt:
        a = abs_rect(rt, pabs)
        if a:
            g = godot(a)
            parts.append(f'[{g[0]:.0f},{g[1]:.0f} {g[2]:.0f}x{g[3]:.0f}]')
            npabs = a
    # 组件摘要
    for cpid in go_comps.get(go_pid, []):
        mb = load('MonoBehaviour', cpid)
        if mb:
            s = mb_summary(mb)
            if s:
                parts.append(','.join(s))
    mark = ' (inactive)' if not active else ''
    line = '  ' * depth + f'- {name}{mark} ' + ' '.join(parts)
    lines.append(line)
    # 子项: Transform 链 + RT 链 合并去重 (Unity 父子引用均为 Transform PathID)
    kids = []
    if tr:
        kids += children_map.get(tr, [])
    if rt:
        for c in children_map.get(rt, []):
            if c not in kids:
                kids.append(c)
    for c in kids:
        child_go = None
        for gpid, comps in go_comps.items():
            if c in comps:
                child_go = gpid
                break
        if child_go:
            walk(child_go, depth + 1, npabs)

# 根: 优先找 Transform.m_Father 为空 的 GO (兼容 battlearena1: 场景根在 Transform 层)
# 孤儿判定: 父引用为空, 或父 PathID 在本场景任何类型文件中都不存在 (跨场景/跨 bundle 引用
#   → 该子树实际是独立界面, 必须作为根输出, 否则整棵树被吞)
def _is_orphan(fpid):
    if not fpid:
        return True
    return not any(fpid in d for d in _file_idx.values())

roots = 0
root_seen = set()
_build_file_idx()  # 孤儿判定需要全场景 PathID 索引
for pid, d in transforms.items():
    f = d.get('m_Father', {})
    fpid = f.get('m_PathID') if isinstance(f, dict) else f
    if _is_orphan(fpid):
        for gpid, comps in go_comps.items():
            if pid in comps and gpid not in root_seen:
                walk(gpid, 0, (0, 0, W, H))
                roots += 1
                root_seen.add(gpid)
                break
# 退回: RT 层孤儿节点 (纯 UI 场景如 mainmenuwarpforge; Transform 分支找到根也继续,
#  UI 树根在 RT 层, 两分支都要执行, root_seen 去重)
for pid in rects:
    f = rects[pid].get('m_Father', {})
    fpid = f.get('m_PathID') if isinstance(f, dict) else f
    if _is_orphan(fpid):
        for gpid, comps in go_comps.items():
            if pid in comps:
                walk(gpid, 0, (0, 0, W, H))
                roots += 1
                break

scene_name = os.path.basename(SCENE.rstrip('/\\'))
header = f"""# {scene_name} 场景全树 (解包 07_场景/{scene_name})
> 生成: dump_scene_tree.py | GO {len(go_names)} / RT {len(rects)} / 根 {roots}
> 坐标=Godot(1920x1080, y向下); sprite=PathID(查 ui_extract 索引); 标注 (inactive) 的场景中禁用

"""
with open(OUT, 'w', encoding='utf-8') as fp:
    fp.write(header + '\n'.join(lines))
print(f'输出 {OUT}: {len(lines)} 行, {roots} 根')
