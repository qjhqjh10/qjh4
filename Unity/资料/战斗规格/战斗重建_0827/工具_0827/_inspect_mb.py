#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""临时分析工具: 解码 battlearena1 MonoBehaviour 组件 (Image/Text/Button/脚本壳) + Sprite 名/尺寸"""
import json, os, sys, glob

sys.stdout.reconfigure(encoding='utf-8')
SRC = 'd:/2/解包整理/07_场景/battlearena1'
SPRITEMAP = 'd:/2/Warpforge_tools/data/ui_extract/battlearena1_sprite_map.json'

mb_index = {}
name_of_script = {}
script_names_file = {}

def load():
    for f in glob.glob(os.path.join(SRC, 'MonoBehaviour', '*.json')):
        base = os.path.basename(f).rsplit('.json', 1)[0]
        m = base.rsplit('_', 1)
        pid = None
        if len(m) == 2 and m[1].lstrip('-').isdigit():
            pid = int(m[1])
        if pid is None: continue
        try:
            d = json.load(open(f, encoding='utf-8'))
        except Exception:
            continue
        if pid not in mb_index:
            mb_index[pid] = d
    # script 名映射: 12_主程序资源 has MonoBehaviour JSONs with m_MonoScript names? use Warpforge_code? skip; print script pid only

spritemap = {}
def load_spritemap():
    d = json.load(open(SPRITEMAP, encoding='utf-8'))
    for k, v in d.items():
        spritemap[str(int(k))] = v

# Sprite JSON 目录: 找 m_Name==name 的 Sprite json (03_界面UI 与 12_主程序资源)
sprite_rect_cache = {}
def find_sprite(name):
    if name in sprite_rect_cache:
        return sprite_rect_cache[name]
    res = []
    for root in ['d:/2/解包整理/03_界面UI', 'd:/2/解包整理/12_主程序资源', 'd:/2/解包整理/07_场景']:
        for sd in ['Sprite']:
            d2 = os.path.join(root, sd)
            if not os.path.isdir(d2): continue
            for f in os.listdir(d2):
                if not f.endswith('.json'): continue
                try:
                    d = json.load(open(os.path.join(d2, f), encoding='utf-8'))
                except Exception:
                    continue
                if d.get('m_Name') == name:
                    r = d.get('m_Rect') or {}
                    p = d.get('m_Pivot') or {}
                    ppu = d.get('m_PixelsPerUnit', 100)
                    res.append(('{}/{}'.format(root, f),
                                (r.get('x'), r.get('y'), r.get('width'), r.get('height')),
                                (p.get('x'), p.get('y')), ppu,
                                d.get('m_AtlasTags', []), d.get('m_PackingRotation', 0)))
                    break
    sprite_rect_cache[name] = res
    return res

def show(pid):
    d = mb_index.get(pid)
    if not d:
        print('MB %s 不存在' % pid); return
    keys = list(d.keys())
    print('MB %s scr=%s' % (pid, d.get('m_Script', {}).get('m_PathID')))
    # 按字段打印可读信息
    info = []
    if 'm_Sprite' in d and d.get('m_Sprite', {}).get('m_PathID'):
        sp = str(d['m_Sprite']['m_PathID'])
        nm = spritemap.get(sp, '??')
        info.append('Sprite pid=%s name=%s' % (sp, nm))
    if 'm_Color' in d:
        c = d['m_Color']
        info.append('color=(%.4f,%.4f,%.4f,%.4f)' % (c.get('r',1),c.get('g',1),c.get('b',1),c.get('a',1)))
    if 'm_text' in d:
        fs = d.get('m_fontSize', '')
        fc = d.get('m_fontColor') or d.get('m_Color')
        info.append('text=%r fontSize=%s color=%s align=%s anchor=%s fontAsset=%s' % (
            d.get('m_text'), fs,
            ('(%.4f,%.4f,%.4f,%.4f)' % (fc['r'],fc['g'],fc['b'],fc['a'])) if isinstance(fc,dict) else fc,
            d.get('m_textAlignment'), d.get('m_HorizontalAlignment'), d.get('m_fontAsset', {}).get('m_PathID')))
    if 'm_Transition' in d:
        info.append('Button transition=%s target=%s colors=%s' % (
            d.get('m_Transition'),
            d.get('m_TargetGraphic', {}).get('m_PathID'),
            json.dumps(d.get('m_Colors', {}).get('m_NormalColor', {})) [:80]))
    if 'm_Type' in d:
        info.append('Image type=%s preserveAspect=%s' % (d.get('m_Type'), d.get('m_PreserveAspect')))
    print('   ' + ' | '.join(info))
    # 特殊字段
    for k in ['m_Material', 'm_RaycastTarget', 'm_Enabled']:
        if k in d and k not in ('m_Color',):
            pass
    if 'm_Material' in d:
        pass
    # 未知脚本: 打印前几个字段名帮助识别类型
    known = set(['m_GameObject','m_Enabled','m_Script','m_Name','m_Material','m_Color','m_RaycastTarget','m_RaycastPadding','m_Maskable','m_OnCullStateChanged','m_Sprite','m_Type','m_PreserveAspect','m_FillCenter','m_FillMethod','m_FillAmount','m_FillClockwise','m_FillOrigin','m_UseSpriteMesh','m_PixelsPerUnitMultiplier'])
    if not (set(d.keys()) & {'m_text','m_Sprite','m_Transition'}):
        print('   未知字段: %s' % [k for k in d.keys() if k not in known][:12])

if __name__ == '__main__':
    load(); load_spritemap()
    for pid in sys.argv[1:]:
        show(int(pid))
