import UnityPy, sys, os
sys.stdout.reconfigure(encoding='utf-8')
AA = r'D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
BUNDLE, ROOTNAME, MAXD = sys.argv[1], sys.argv[2], int(sys.argv[3])
path = next(os.path.join(AA, f) for f in os.listdir(AA) if BUNDLE in f)
env = UnityPy.load(path); objs = list(env.objects)
def go_of(x):
    try: return x.m_GameObject.read()
    except Exception:
        try: return x.read().m_GameObject.read()
        except Exception: return None
def transform_of(go):
    for c in go.m_Component:
        o = c.component.read()
        if o.__class__.__name__ in ('Transform','RectTransform'): return o
    return None
def walk(go, depth=0):
    print('%s%s' % ('  '*depth, go.m_Name))
    if depth >= MAXD: return
    tr = transform_of(go)
    if tr is None: return
    for c in tr.m_Children:
        g = go_of(c)
        if g is not None: walk(g, depth+1)
root = None
for o in objs:
    if o.type.name == 'GameObject' and o.read().m_Name == ROOTNAME:
        root = o.read(); break
if root is None: print('root not found')
else: walk(root, 0)
