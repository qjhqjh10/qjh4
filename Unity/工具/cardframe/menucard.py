import UnityPy, sys, os
sys.stdout.reconfigure(encoding='utf-8')
AA = r'D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
path = next(os.path.join(AA,f) for f in os.listdir(AA) if 'menus_assets_all' in f)
env=UnityPy.load(path); objs=list(env.objects)
print('objs', len(objs))
gos=[o.read() for o in objs if o.type.name=='GameObject']
hits=[g for g in gos if g.m_Name=='Cost Container']
print('Cost Container count', len(hits))
def tr_of(go):
    for c in go.m_Component:
        o=c.component.read()
        if type(o).__name__ in ('Transform','RectTransform'): return o
def climb(go, n=10):
    out=[go.m_Name]
    cur=go
    for i in range(n):
        tr=tr_of(cur)
        try: par=tr.m_Father.read()
        except Exception: break
        if par is None: break
        try: pg=par.m_GameObject.read()
        except Exception: break
        out.append(pg.m_Name); cur=pg
    return ' < '.join(out)
for h in hits[:3]:
    print(climb(h))
