import os, sys, glob, numpy as np
from PIL import Image
sys.stdout.reconfigure(encoding='utf-8')
OUT=r'C:/Users/qjh36/AppData/Local/Temp/d2/img'
os.makedirs(OUT, exist_ok=True)
ROOT=r'D:/2/新解包资源/assets_full'
# 1) frame pieces
for tag,f in [('um_t1','bundle_spacemarinesultramarinescardassets_assets_all/Texture2D/40k_Cardframe_troop_Ultramarines_tier1.png'),
              ('orks_t1','bundle_orksgoffcardassets_assets_all/Texture2D/40k_Cardframe_troop_Orks_tier1.png'),
              ('sh_t1','bundle_aeldarisaimhanncardassets_assets_all/Texture2D/40k_Cardframe_troop_SaimHann_tier1.png')]:
    im=Image.open(os.path.join(ROOT,f)).convert('RGBA')
    im.crop((171,1024-(11+998),171+682,1024-11)).resize((341,499)).save(os.path.join(OUT,'frame_%s.png'%tag))
# 2) cost icon + melee + armour + health sprites: find pngs
for nm in ['Card Frame Cost Icon','pedestal_icon_armor']:
    hits=glob.glob(ROOT+'/bundle_*/Texture2D/'+nm+'.png')
    if hits:
        im=Image.open(hits[0]).convert('RGBA'); print(nm, hits[0], im.size)
        im.save(os.path.join(OUT, nm.replace(' ','_')+'.png'))
# 3) composite: frame + icons overlaid at the PREFAB positions (converted)
