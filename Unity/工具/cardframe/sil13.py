import os, sys, glob, numpy as np
from PIL import Image
sys.stdout.reconfigure(encoding='utf-8')
ROOT=r'D:/2/新解包资源/assets_full'
RECT=(171,11,682,998)
rows=[]
for d in sorted(glob.glob(ROOT+'/bundle_*cardassets_assets_all')):
    army=os.path.basename(d).replace('bundle_','').replace('cardassets_assets_all','')
    for kind in ('troop','stratagem','Troop'):
        for t in (1,2,3,4):
            _td=os.path.join(d,'Texture2D')
            cands=[os.path.join(_td,x) for x in os.listdir(_td) if 'ardframe' in x and 'SDF' not in x and x.lower().endswith('_tier%d.png'%t) and ('_%s_'%kind) in x]
            for f in cands:
                b=os.path.basename(f)
                if kind=='troop' and 'troop' not in b.lower(): continue
                if kind=='stratagem' and 'stratagem' not in b.lower(): continue
                try: im=Image.open(f).convert('RGBA')
                except Exception: continue
                a=np.array(im); H,W=a.shape[:2]
                if (W,H)!=(1024,1024): continue
                top=H-(RECT[1]+RECT[3])
                sub=a[top:top+RECT[3], RECT[0]:RECT[0]+RECT[2]]
                m=sub[:,:,3]>8
                ys,xs=np.nonzero(m)
                rows.append((army,kind.lower()[:5],t,int(xs.min()),int(xs.max()),int(ys.min()),int(ys.max())))
seen=set(); uniq=[]
for r in rows:
    k=(r[0],r[1],r[2])
    if k in seen: continue
    seen.add(k); uniq.append(r)
for r in sorted(uniq):
    print('%-32s %-6s t%d  x[%3d..%3d] w=%3d   y[%3d..%3d] h=%3d' % (r[0],r[1],r[2],r[3],r[4],r[4]-r[3]+1,r[5],r[6],r[6]-r[5]+1))
