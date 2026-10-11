import os, sys, glob, numpy as np
from PIL import Image
sys.stdout.reconfigure(encoding='utf-8')
ROOT=r'D:/2/新解包资源/assets_full'
RECT=(171,11,682,998)
CARD_W, CARD_H = 2.2452, 3.2572      # CardFrame sd
CARD_ORIGIN_Y = 0.05                 # CardFrame origin in 2DCard space (Front apos 0.08 + CardFrame apos -0.03)
def load(f):
    im=Image.open(f).convert('RGBA'); a=np.array(im)
    H,W=a.shape[:2]
    top=H-(RECT[1]+RECT[3]); left=RECT[0]
    return a[top:top+RECT[3], left:left+RECT[2]]
def blob(mask, label):
    ys,xs=np.nonzero(mask)
    if len(xs)<50: return None
    # centroid weighted
    return (xs.mean(), ys.mean(), len(xs), xs.min(), xs.max(), ys.min(), ys.max())
def px2card(cx, cy):
    ux = (cx - 341.0)/682.0*CARD_W
    uy = (499.0-cy)/998.0*CARD_H + CARD_ORIGIN_Y
    return ux, uy
fam = {
 'Ultramarines': ('bundle_spacemarinesultramarinescardassets_assets_all', '40k_Cardframe_%s_Ultramarines_tier%d.png', ['40k_Cardframe_troop_Ultramarines_tier%d.png','40k_Cardframe_stratagem_Ultramarines_tier%d.png']),
 'Orks':         ('bundle_orksgoffcardassets_assets_all', '40k_Cardframe_%s_Orks_tier%d.png', ['40k_Cardframe_troop_Orks_tier%d.png','40k_Cardframe_stratagem_Orks_tier%d.png']),
 'SaimHann':     ('bundle_aeldarisaimhanncardassets_assets_all', '40k_Cardframe_%s_SaimHann_tier%d.png', ['40k_Cardframe_troop_SaimHann_tier%d.png','40k_Cardframe_stratagem_SaimHann_tier%d.png']),
 'EmperorChildren': ('bundle_chaosspacemarinesemperorschildrencardassets_assets_all', None, ['40k_Cardframes_Troop_Emperor Children_tier%d.png','40k_Cardframes_stratagem_Emperor Children_tier%d.png']),
}
print('%-16s %-10s %-5s | %-22s | %-22s | %-22s | %-22s' % ('faction','kind','tier','MELEE(red) center->card','RANGE(purple)','HEALTH(green)','ARMOUR(silver/gray)'))
for name,(bundle,_,pats) in fam.items():
    for kind,pat in zip(('troop','strat'),pats):
        for t in (1,2,3,4):
            f=os.path.join(ROOT,bundle,'Texture2D',pat%t)
            if not os.path.exists(f): print('missing',f); continue
            a=load(f).astype(int); R,G,B,A=a[:,:,0],a[:,:,1],a[:,:,2],a[:,:,3]
            op = A>200
            red    = op & (R>90) & (R>G*1.6) & (R>B*1.6) & (G<90)
            purple = op & (B>70) & (R>50) & (B>G*1.4) & (R>G*1.1) & (B>R*0.85)
            green  = op & (G>70) & (G>R*1.35) & (G>B*1.25)
            # armour: silver shield = low saturation bright/grey in the bottom band
            mx=np.maximum(np.maximum(R,G),B); mn=np.minimum(np.minimum(R,G),B)
            grey = op & ((mx-mn)<26) & (mx>60)
            grey[:780,:]=False; grey[:, :500]=False      # armour region: right side, bottom band
            out=[]
            for m,lab in ((red,'red'),(purple,'purple'),(green,'green'),(grey,'grey')):
                b=blob(m,lab)
                out.append('%s n=%d' % (('%.3f,%.3f'%px2card(b[0],b[1])), b[2]) if b else 'none')
            print('%-16s %-10s %-5d | %-22s | %-22s | %-22s | %-22s' % (name,kind,t,*out))
