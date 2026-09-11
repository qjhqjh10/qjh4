#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""battlearena1 2DCard 树遍历 v2: RT链 (correct Unity hierarchy)"""
import json, io, sys, os, glob

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
SCENE = r"D:/2/解包整理/07_场景/battlearena1"

cache = {}
def load(subdir, pid):
    key = (subdir, pid)
    if key in cache:
        return cache[key]
    p = os.path.join(SCENE, subdir, f"{subdir}_{pid}.json")
    if not os.path.exists(p):
        p2 = os.path.join(SCENE, subdir, f"{subdir}_{pid}_{pid}.json")
        if os.path.exists(p2):
            p = p2
        else:
            hits = glob.glob(os.path.join(SCENE, subdir, f"*_{pid}.json"))
            if not hits:
                cache[key] = None
                return None
            p = hits[0]
    try:
        d = json.load(open(p, encoding='utf-8'))
    except Exception:
        d = None
    cache[key] = d
    return d

def go_name(gopid):
    g = load("GameObject", gopid)
    return g.get('m_Name', '?') if g else '?'

def walk(rtpid, depth=0):
    rt = load("RectTransform", rtpid)
    if not rt:
        print("  " * depth + f"(RT {rtpid} 缺失)")
        return
    gopid = rt.get('m_GameObject', {}).get('m_PathID')
    name = go_name(gopid) if gopid else '?'
    a = rt.get('m_AnchorMin', {})
    ap = rt.get('m_AnchoredPosition', {})
    sd = rt.get('m_SizeDelta', {})
    pv = rt.get('m_Pivot', {})
    info = f"anchorMin=({a.get('x')},{a.get('y')}) pos=({ap.get('x')},{ap.get('y')}) size=({sd.get('x')}x{sd.get('y')}) pivot=({pv.get('x')},{pv.get('y')})"
    # 组件
    detail = []
    g = load("GameObject", gopid) if gopid else None
    if g:
        for comp in g.get('m_Component', []):
            cpid = comp['component']['m_PathID']
            mb = load("MonoBehaviour", cpid)
            if not mb:
                continue
            sc = mb.get('m_Script', {}).get('m_PathID')
            if sc == 350208831926335389:  # UI Image
                sp = mb.get('m_Sprite', {})
                spid = sp.get('m_PathID')
                detail.append(f"[Image spritePID={spid} type={mb.get('m_Type')} color=({mb.get('m_Color',{}).get('r')},{mb.get('m_Color',{}).get('g')},{mb.get('m_Color',{}).get('b')},{mb.get('m_Color',{}).get('a')})]")
            elif mb.get('m_text') is not None:
                fnt = mb.get('m_font', {})
                detail.append(f"[TextMeshPro? script={sc} text={repr(mb.get('m_text'))[:50]}]")
            else:
                # TMP? m_fontAsset 字段
                if mb.get('m_fontAsset') is not None:
                    detail.append(f"[TMP script={sc} text={repr(mb.get('m_text'))[:40]} fontPID={mb.get('m_fontAsset',{}).get('m_PathID')}]")
    print("  " * depth + f"RT{rtpid} GO{gopid} {name} | {info}")
    for t in detail:
        print("  " * depth + "    " + t)
    for ch in rt.get('m_Children', []):
        walk(ch['m_PathID'], depth + 1)

if __name__ == "__main__":
    for r in sys.argv[1:]:
        walk(int(r))
