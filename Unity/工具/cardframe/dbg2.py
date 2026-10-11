import UnityPy, sys, os
sys.stdout.reconfigure(encoding='utf-8')
AA = r'D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
path = next(os.path.join(AA, f) for f in os.listdir(AA) if 'battlearena1' in f)
env = UnityPy.load(path); objs=list(env.objects)
done=set()
for o in objs:
    if o.type.name != 'GameObject': continue
    d=o.read()
    if d.m_Name not in ('Card Info','Cost Container','Cost Background','Front','2DCard','Melee Attack Container','Health Container','Armour Container','Rarity'): continue
    if d.m_Name in done: continue
    done.add(d.m_Name)
    print('==', d.m_Name, 'active=', d.m_IsActive)
    for c in d.m_Component:
        try:
            cc=c.component.read(); cn=type(cc).__name__
        except Exception as e:
            print('   <err>',e); continue
        if cn in ('Transform','RectTransform'):
            print('   %-14s lp=%s ls=%s sd=%s ap=%s anc=%s' % (cn, (cc.m_LocalPosition.x,cc.m_LocalPosition.y), (cc.m_LocalScale.x,cc.m_LocalScale.y), (cc.m_SizeDelta.x,cc.m_SizeDelta.y), (cc.m_AnchoredPosition.x,cc.m_AnchoredPosition.y), (cc.m_AnchorMin.x,cc.m_AnchorMax.x)))
        elif cn=='Image':
            s=cc.m_Sprite.read().m_Name if cc.m_Sprite else None
            print('   Image sprite=%r type=%s PA=%s ppuM=%s col=%s' % (s, cc.m_Type, cc.m_PreserveAspect, cc.m_PixelsPerUnitMultiplier, (round(cc.m_Color.r,3),round(cc.m_Color.g,3),round(cc.m_Color.b,3),round(cc.m_Color.a,3))))
        else:
            print('   %s' % cn)
