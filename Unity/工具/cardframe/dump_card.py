import UnityPy, sys, os, math
## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/cardframe/dump_card.py <参数1> <参数2>\n  选项：无自定义选项（不带参数 = 默认模式）\n  位置参数：脚本内读 sys.argv[1..2]（缺省/必填以脚本自身判断为准）')
    _wf_help_sys.exit(0)

sys.stdout.reconfigure(encoding='utf-8')
AA = r'D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
BUNDLE, ROOTNAME = sys.argv[1], sys.argv[2]
path = next(os.path.join(AA, f) for f in os.listdir(AA) if BUNDLE in f)
env = UnityPy.load(path); objs = list(env.objects)
print('# bundle =', os.path.basename(path))

def go_of(x):
    try: return x.m_GameObject.read()
    except Exception:
        try: return x.read().m_GameObject.read()
        except Exception: return None
def tr_of(go):
    for c in go.m_Component:
        o = c.component.read()
        if o.__class__.__name__ in ('Transform','RectTransform'): return o
def comp_of(go, name):
    for c in go.m_Component:
        o = c.component.read()
        if o.__class__.__name__ == name: return o
VMAP = {0:'Simple',1:'Sliced',2:'Tiled',3:'Filled'}

def q(v, n=4):
    return round(v, n)

def walk(go, depth, base, out):
    tr = tr_of(go)
    name = go.m_Name
    if tr.__class__.__name__ == 'RectTransform':
        sd = (tr.m_SizeDelta.x, tr.m_SizeDelta.y)
        am, aM = (tr.m_AnchorMin.x, tr.m_AnchorMin.y), (tr.m_AnchorMax.x, tr.m_AnchorMax.y)
        ap, pv = (tr.m_AnchoredPosition.x, tr.m_AnchoredPosition.y), (tr.m_Pivot.x, tr.m_Pivot.y)
        ls = (tr.m_LocalScale.x, tr.m_LocalScale.y)
    else:
        sd = am = aM = ap = pv = ls = None
    img = comp_of(go, 'Image')
    spr = None; itype = None; pa = None; ppum = None; col = None
    if img is not None:
        spr = img.m_Sprite.m_Name if img.m_Sprite else None
        itype = VMAP.get(getattr(img,'m_Type',None))
        pa = getattr(img,'m_PreserveAspect',None)
        ppum = getattr(img,'m_PixelsPerUnitMultiplier',None)
        col = getattr(img,'m_Color',None)
        col = (round(col.r,3), round(col.g,3), round(col.b,3), round(col.a,3)) if col else None
    print('%s%-26s sd=%-18s anc=%-16s apos=%-18s piv=%-14s ls=%s%s' % (
        '  '*depth, name,
        str((q(sd[0]),q(sd[1]))) if sd else '-',
        str((round(am[0],3),round(am[1],3),round(aM[0],3),round(aM[1],3))) if am else '-',
        str((q(ap[0]),q(ap[1]))) if ap else '-',
        str((round(pv[0],3),round(pv[1],3))) if pv else '-',
        str((q(ls[0]),q(ls[1]))) if ls else '-',
        ('' if img is None else ' IMG[%s type=%s PA=%s ppuM=%s col=%s]' % (spr, itype, pa, ppum, col))
    ))
    if depth >= 8: return
    for c in tr.m_Children:
        g = go_of(c)
        if g is not None: walk(g, depth+1, base, out)

root = None
for o in objs:
    if o.type.name == 'GameObject' and o.read().m_Name == ROOTNAME:
        root = o.read(); break
print('root found:', root is not None)
if root: walk(root, 0, None, None)
