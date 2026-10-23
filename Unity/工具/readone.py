# -*- coding: utf-8 -*-
import UnityPy, sys
## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python 工具/readone.py <参数1>\n  选项：无自定义选项（不带参数 = 默认模式）\n  位置参数：脚本内读 sys.argv[1]（缺省/必填以脚本自身判断为准）')
    _wf_help_sys.exit(0)

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
