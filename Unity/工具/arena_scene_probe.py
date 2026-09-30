#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
arena_scene_probe.py —— 查「我们**建好的** `Battle_<场>.unity` 里某个对象」的 pos/rot/scale/active
================================================================================
为什么需要：`ArenaBuilder` 是**把清单写进场景 YAML** —— 想验收「清单改了、场景里真生效了吗」，
只能去读**场景文件**（读清单只能证明清单改了）。§28 那一批修复（雾 / 镜像负缩放 / 精灵）都要靠它验收。

用法：
  PYTHONIOENCODING=utf-8 python 工具/arena_scene_probe.py <场名> <对象名> [对象名...]
  PYTHONIOENCODING=utf-8 python 工具/arena_scene_probe.py --rs <场名>     # 只打这一场的 RenderSettings

判据出处：场景 YAML 的 `--- !u!1 &<goId>`（GameObject）/ `--- !u!4 &<trId>`（Transform）/ `--- !u!104`（RenderSettings）。
只读脚本。
"""
import io, os, re, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
SCENES = os.path.join(ROOT, 'MyGame', 'Assets', 'CardPresentation', 'Scenes')


def load_scene(arena):
    p = os.path.join(SCENES, 'Battle_%s.unity' % arena)
    if not os.path.exists(p):
        p = os.path.join(SCENES, '%s.unity' % arena)
    return io.open(p, 'r', encoding='utf-8', errors='replace').read(), p


def blocks(text):
    """{anchorId: (className, body)}"""
    out = {}
    for m in re.finditer(r'^--- !u!(\d+) &(\d+)(?: stripped)?\s*$\n(.*?)(?=^--- |\Z)', text, re.M | re.S):
        out[m.group(2)] = (m.group(1), m.group(3))
    return out


def vec(body, key):
    m = re.search(r'^\s*%s:\s*\{x:\s*([-\d.eE]+),\s*y:\s*([-\d.eE]+),\s*z:\s*([-\d.eE]+)' % re.escape(key),
                  body, re.M)
    if not m:
        return None
    return tuple(round(float(m.group(i)), 5) for i in (1, 2, 3))


def main():
    a = sys.argv[1:]
    if not a:
        print(__doc__)
        return
    if a[0] == '--rs':
        text, p = load_scene(a[1])
        m = re.search(r'^--- !u!104 &\d+\s*$(.*?)(?=^--- |\Z)', text, re.M | re.S)
        body = m.group(1)
        print('== %s ==' % os.path.basename(p))
        for k in ('m_AmbientMode', 'm_AmbientSkyColor', 'm_AmbientIntensity',
                  'm_Fog', 'm_FogMode', 'm_FogDensity', 'm_FogColor', 'm_LinearFogStart', 'm_LinearFogEnd',
                  'm_ReflectionIntensity', 'm_FlareStrength'):
            mm = re.search(r'^\s*%s:\s*(.+)$' % k, body, re.M)
            print('  %-24s %s' % (k, mm.group(1).strip() if mm else '(缺)'))
        return

    arena = a[0]
    text, p = load_scene(arena)
    bs = blocks(text)
    print('== %s ==' % os.path.basename(p))
    for want in a[1:]:
        hit = 0
        for aid, (cls, body) in bs.items():
            if cls != '1':
                continue
            m = re.search(r'^  m_Name: (.+)$', body, re.M)
            if not m or m.group(1).strip() != want:
                continue
            hit += 1
            act = re.search(r'^  m_IsActive: (\d+)', body, re.M)
            comps = re.findall(r'component: \{fileID: (\d+)\}', body)
            tr = None
            for c in comps:
                if c in bs and bs[c][0] == '4':
                    tr = bs[c][1]
                    break
            print('  %-26s active=%s' % (want, act.group(1) if act else '?'))
            if tr:
                print('      pos=%s rot=%s scale=%s' % (vec(tr, 'm_LocalPosition'), vec(tr, 'm_LocalRotation'), vec(tr, 'm_LocalScale')))
            else:
                print('      (找不到 Transform)')
        if not hit:
            print('  %-26s 🔴 场景里没有这个对象' % want)
        elif hit > 1:
            print('      ⚠️ 有 %d 个同名对象（上面全部列出）' % hit)


if __name__ == '__main__':
    main()
