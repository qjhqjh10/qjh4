import os, sys, numpy as np
from PIL import Image
sys.stdout.reconfigure(encoding='utf-8')
ROOT=r'D:/2/新解包资源/assets_full'
RECT=(171,11,682,998); CARD_W,CARD_H=2.2452,3.2572; OY=0.05
def load(f):
    a=np.array(Image.open(f).convert('RGBA')); H,W=a.shape[:2]
    top=H-(RECT[1]+RECT[3]); return a[top:top+RECT[3], RECT[0]:RECT[0]+RECT[2]]
def px2card(cx,cy): return (cx-341.0)/682.0*CARD_W, (499.0-cy)/998.0*CARD_H+OY
# windows in the 682x998 crop (x,y from top-left)
WIN={'MELEE':(10,760,200,990),'RANGE':(180,800,330,995),'HEALTH':(430,800,620,995),'ARMOUR':(560,650,682,830)}
fam=[('Ultramarines','bundle_spacemarinesultramarinescardassets_assets_all','40k_Cardframe_troop_Ultramarines_tier%d.png'),
     ('Orks','bundle_orksgoffcardassets_assets_all','40k_Cardframe_troop_Orks_tier%d.png'),
     ('SaimHann','bundle_aeldarisaimhanncardassets_assets_all','40k_Cardframe_troop_SaimHann_tier%d.png'),
     ('EmperorChildren','bundle_chaosspacemarinesemperorschildrencardassets_assets_all','40k_Cardframes_Troop_Emperor Children_tier%d.png'),
     ('Necron_Sautekh','bundle_necronssautekhcardassets_assets_all','40k_Cardframe_troop_Sautekh_tier%d.png'),
     ('AstraMilitarum','bundle_astramilitarumcardassets_assets_all','40k_Cardframes_Troop_Astra Militarum_tier%d.png')]
print('%-16s %-5s | %-16s %-16s %-16s %-16s' % ('faction','tier','MELEE','RANGE','HEALTH','ARMOUR'))
print('%-16s %-5s | %-16s %-16s %-16s %-16s' % ('PREFAB','-','-0.836,-1.110','-0.595,-1.320','0.740,-1.280','0.899,-0.911'))
for name,bundle,pat in fam:
    for t in (1,2,3,4):
        f=os.path.join(ROOT,bundle,'Texture2D',pat%t)
        if not os.path.exists(f):
            print('%-16s %-5d | MISSING %s' % (name,t,pat%t)); continue
        a=load(f).astype(int); R,G,B,A=a[:,:,0],a[:,:,1],a[:,:,2],a[:,:,3]
        out=[]
        for lab,(x0,y0,x1,y1) in WIN.items():
            sub=slice(y0,y1),slice(x0,x1)
            r,g,b,al=R[sub],G[sub],B[sub],A[sub]
            op=al>200
            if lab=='MELEE':  m=op&(r>90)&(r>g*1.7)&(r>b*1.7)&(g<80)
            elif lab=='RANGE':m=op&(b>70)&(b>g*1.5)&(r>50)&(b>=r*0.85)
            elif lab=='HEALTH':m=op&(g>70)&(g>r*1.35)&(g>b*1.25)
            else:
                mx=np.maximum(np.maximum(r,g),b); mn=np.minimum(np.minimum(r,g),b)
                m=op&((mx-mn)<26)&(mx>60)
            ys,xs=np.nonzero(m)
            if len(xs)<40: out.append('none'); continue
            ux,uy=px2card(xs.mean()+x0, ys.mean()+y0); out.append('%.3f,%.3f' % (ux,uy))
        print('%-16s %-5d | %-16s %-16s %-16s %-16s' % (name,t,*out))
