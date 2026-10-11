import UnityPy, sys, os
sys.stdout.reconfigure(encoding='utf-8')
AA = r'D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
BUNDLE, ROOTNAME = sys.argv[1], sys.argv[2]
path = next(os.path.join(AA, f) for f in os.listdir(AA) if BUNDLE in f)
env = UnityPy.load(path); objs=list(env.objects)
print('# bundle =', os.path.basename(path))
SPR={}
for o in objs:
    if o.type.name=='Sprite':
        try: SPR[o.path_id]=o.read().m_Name
        except Exception: pass
def go_of(x):
    try: return x.m_GameObject.read()
    except Exception:
        try: return x.read().m_GameObject.read()
        except Exception: return None
VMAP={0:'Simple',1:'Sliced',2:'Tiled',3:'Filled'}
def dump(go, depth, seen):
    tr=None; info=[]
    for c in go.m_Component:
        try: cc=c.component.read()
        except Exception: continue
        cn=type(cc).__name__
        if cn in ('Transform','RectTransform'): tr=cc
        elif cn=='MonoBehaviour':
            try: tt=c.component.read_typetree()
            except Exception: tt=None
            if tt and 'm_Sprite' in tt:
                pid=tt['m_Sprite'].get('m_PathID')
                info.append('IMG sprite=%r type=%s PA=%s ppuM=%s color=%s fillCenter=%s' % (
                    SPR.get(pid, pid), VMAP.get(tt.get('m_Type')), tt.get('m_PreserveAspect'),
                    tt.get('m_PixelsPerUnitMultiplier'),
                    tuple(round(x,3) for x in (tt.get('m_Color') or {}).get('rgba', {}).values()) if isinstance(tt.get('m_Color'), dict) else tt.get('m_Color'),
                    tt.get('m_FillCenter')))
            elif tt and 'm_fontSize' in tt:
                info.append('TMP fs=%s align=%s/%s wrap=%s color=%s' % (
                    tt.get('m_fontSize'), tt.get('m_HorizontalAlignment'), tt.get('m_VerticalAlignment'),
                    tt.get('m_enableWordWrapping'), tt.get('m_fontColor')))
        else:
            info.append(cn)
    if tr is None:
        print('%s%-28s <no transform>' % ('  '*depth, go.m_Name)); return
    isrt = type(tr).__name__=='RectTransform'
    print('%s%-28s %s' % ('  '*depth, go.m_Name,
        ('sd=(%s,%s) apos=(%s,%s) anc=(%s,%s,%s,%s) piv=(%s,%s) ls=(%s,%s)' % (
            round(tr.m_SizeDelta.x,4),round(tr.m_SizeDelta.y,4), round(tr.m_AnchoredPosition.x,4),round(tr.m_AnchoredPosition.y,4),
            round(tr.m_AnchorMin.x,3),round(tr.m_AnchorMin.y,3),round(tr.m_AnchorMax.x,3),round(tr.m_AnchorMax.y,3),
            round(tr.m_Pivot.x,3),round(tr.m_Pivot.y,3), round(tr.m_LocalScale.x,4),round(tr.m_LocalScale.y,4),
            ) if isrt else 'Transform lp=(%s,%s) ls=(%s,%s)' % (round(tr.m_LocalPosition.x,4),round(tr.m_LocalPosition.y,4),round(tr.m_LocalScale.x,4),round(tr.m_LocalScale.y,4)))))
    for s in info: print('%s   · %s' % ('  '*depth, s))
    if depth>=9: return
    for c in tr.m_Children:
        g=go_of(c)
        if g is not None: dump(g, depth+1, seen)
root=None
for o in objs:
    if o.type.name=='GameObject' and o.read().m_Name==ROOTNAME:
        root=o.read(); break
print('root:', ROOTNAME, root is not None)
if root: dump(root,0,set())
