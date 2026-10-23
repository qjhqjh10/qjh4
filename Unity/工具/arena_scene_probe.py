#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
arena_scene_probe.py —— 查「我们**建好的** `<场>.unity` 里某个对象」的 pos/rot/scale/active
================================================================================
为什么需要：`ArenaBuilder` 是**把清单写进场景 YAML** —— 想验收「清单改了、场景里真生效了吗」，
只能去读**场景文件**（读清单只能证明清单改了）。§28 那一批修复（雾 / 镜像负缩放 / 精灵）都要靠它验收。
战场场景的真实位置 = `Assets/WarpforgeArena1/Scenes/<场>.unity`（见下面 `SCENES` 的 2026-10-23 注释）。

用法：
  PYTHONIOENCODING=utf-8 python 工具/arena_scene_probe.py <场名> <对象名> [对象名...]
  PYTHONIOENCODING=utf-8 python 工具/arena_scene_probe.py --rs <场名>     # 只打这一场的 RenderSettings

判据出处：场景 YAML 的 `--- !u!1 &<goId>`（GameObject）/ `--- !u!4 &<trId>`（Transform）/ `--- !u!104`（RenderSettings）。
只读脚本。
"""
import glob, io, os, re, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
# 🔴 2026-10-23 修（`T01` 施工计划 §2·2 的 B1）：原来指向 `Assets/CardPresentation/Scenes`
#   —— 那里放的是 `Battle.unity` / `CardBase.unity` 这类**非战场**场景；13 个战场场景早在
#   `Assets/WarpforgeArena1/Scenes/`（现读该目录 = 13 个 `battlearena*.unity` + 2 个 `VFXWhiteboard`）。
#   原写法 ⇒ 每次调用都 `FileNotFoundError` ⇒「候选 → 缺陷」的定位关**没有工具**。
SCENES = os.path.join(ROOT, 'MyGame', 'Assets', 'WarpforgeArena1', 'Scenes')


def load_scene(arena):
    # 现名 = 裸名 `<场>.unity`（`battlearena1.unity`）；`Battle_<场>.unity` 是历史写法，留着兜底。
    for name in ('%s.unity' % arena, 'Battle_%s.unity' % arena):
        p = os.path.join(SCENES, name)
        if os.path.exists(p):
            return io.open(p, 'r', encoding='utf-8', errors='replace').read(), p
    # 兜底：`Assets/*/Scenes/` 任一目录 —— 场景 2026-10-11 从 CardPresentation 搬过一次，
    #   以后再搬就不用改路径（⛔ 别写死第二份目录，这里只按场名去 glob）。
    hits = sorted(glob.glob(os.path.join(ROOT, 'MyGame', 'Assets', '*', 'Scenes', '%s.unity' % arena)))
    if hits:
        return io.open(hits[0], 'r', encoding='utf-8', errors='replace').read(), hits[0]
    sys.exit('🔴 找不到战场场景 `%s`：查过 `%s` 与 `Assets/*/Scenes/`。\n'
             '   场名应是 ArenaBuilder 用的那个（例：battlearena1 / battlearenatauviorla）。' % (arena, SCENES))


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
