# -*- coding: utf-8 -*-
import UnityPy, sys
BUNDLE = r"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64\shaders_assets_all.bundle"
env = UnityPy.load(BUNDLE)
want = sys.argv[1].lower()
for obj in env.objects:
    if obj.type.name != "Shader": continue
    d = obj.read()
    try:
        pf = d.m_ParsedForm; name = pf.m_Name or ""
    except Exception: continue
    if want not in name.lower(): continue
    props = pf.m_PropInfo.m_Props or []
    print("### %s  (属性 %d)" % (name, len(props)))
    for p in props: print("     %-32s %s" % (p.m_Name, p.m_Description))
    for ss in (pf.m_SubShaders or []):
        for pi, ps in enumerate(ss.m_Passes or []):
            st = ps.m_State
            print("     Pass[%d] src=%s dst=%s zW=%s zT=%s cull=%s tags=%s" % (
                pi, st.rtBlend0.srcBlend.val, st.rtBlend0.destBlend.val,
                st.zWrite.val, st.zTest.val, st.culling.val, [t for t in (st.m_Tags.tags or [])]))
