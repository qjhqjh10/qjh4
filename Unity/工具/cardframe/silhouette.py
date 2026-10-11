import numpy as np, os, sys
from PIL import Image
sys.stdout.reconfigure(encoding='utf-8')
ROOT=r'D:/2/新解包资源/assets_full'
S='bundle_spacemarinesultramarinescardassets_assets_all/Texture2D'
S2='bundle_orksgoffcardassets_assets_all/Texture2D'
S3='bundle_aeldarisaimhanncardassets_assets_all/Texture2D'
S4='bundle_chaosspacemarinesemperorschildrencardassets_assets_all/Texture2D'
files=[]
for d in (S,S2,S3,S4):
    for kind in ('troop','stratagem'):
        for t in (1,2,3,4):
            for pat in ('40k_Cardframe_%s_%s_tier%d.png'):
                pass
for d,prefixes in ((S,['40k_Cardframe_troop_Ultramarines_tier%d.png','40k_Cardframe_stratagem_Ultramarines_tier%d.png']),
                   (S2,['40k_Cardframe_troop_Orks_tier%d.png','40k_Cardframe_stratagem_Orks_tier%d.png']),
                   (S3,['40k_Cardframe_troop_SaimHann_tier%d.png','40k_Cardframe_stratagem_SaimHann_tier%d.png']),
                   (S4,['40k_Cardframes_Troop_Emperor Children_tier%d.png','40k_Cardframes_stratagem_Emperor Children_tier%d.png'])):
    for p in prefixes:
        for t in (1,2,3,4):
            f=os.path.join(ROOT,d,p%t)
            if os.path.exists(f): files.append(f)
res={}
for f in files:
    im=Image.open(f).convert('RGBA')
    a=np.array(im)
    alpha=a[:,:,3]
    # sprite rect is (x=171,y=11,w=682,h=998) from bottom-left; PNG is top-left origin
    H,W=alpha.shape
    top=H-(11+998); left=171
    sub=alpha[top:top+998, left:left+682]
    mask=sub>8
    ys,xs=np.nonzero(mask)
    res[os.path.basename(f)]=(int(xs.min()),int(ys.min()),int(xs.max()),int(ys.max()),int(mask.sum()), sgn:=1)
for k,v in sorted(res.items()):
    print('%-52s bbox_in_682x998 x[%4d..%4d] y[%4d..%4d]  area=%d' % (k, v[0], v[2], v[1], v[3], v[4]))
