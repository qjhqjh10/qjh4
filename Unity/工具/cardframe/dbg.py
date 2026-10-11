import UnityPy, sys, os
sys.stdout.reconfigure(encoding='utf-8')
AA = r'D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
path = next(os.path.join(AA, f) for f in os.listdir(AA) if 'battlearena1' in f)
env = UnityPy.load(path); objs=list(env.objects)
def go_of(x):
    try: return x.m_GameObject.read()
    except Exception: return None
for o in objs:
    if o.type.name != 'GameObject': continue
    d = o.read()
    if d.m_Name in ('Card Info',):
        print('GO', d.m_Name, 'pathid', o.path_id)
        for c in d.m_Component:
            try:
                cc = c.component.read()
                print('   ', type(cc).__name__, '  pathid', c.component.m_PathID)
                if type(cc).__name__ == 'RectTransform':
                    print('        ', cc.m_SizeDelta, cc.m_AnchoredPosition, cc.m_LocalScale, cc.m_Pivot)
            except Exception as e:
                print('    ERR', e)
        break
