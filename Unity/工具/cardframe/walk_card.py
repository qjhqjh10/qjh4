import UnityPy, sys, os
## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/cardframe/walk_card.py <参数1> <参数2> <参数3>\n  选项：无自定义选项（不带参数 = 默认模式）\n  位置参数：脚本内读 sys.argv[1..3]（缺省/必填以脚本自身判断为准）')
    _wf_help_sys.exit(0)

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
