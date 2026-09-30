#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
arena_share_scan.py —— 【战场场景线】§28 第①步：跨场共用资产普查
================================================================================
做什么：
  ① 建 guid → 资产路径表（读 MyGame/Assets 下全部 *.meta）
  ② 逐份 `Battle_<场>.unity` 抽出它引用的**全部资产 guid**
  ③ 报出**被 ≥2 场共用的资产**（= 「每场该各一份的东西被共用」的可疑点）
  ④ 顺带把每场的 `RenderSettings` 块（环境光 / 雾 / 天空盒 / 耀斑强度）摊成表

判据（为什么这么查）：
  `项目任务.md` §三 第 30 条 · 第 1 件 §28 三步做法的第①步 ——
  「每场该各一份的东西（RenderSettings / 后处理 profile / 大气与雾 / 天空盒 / 材质实例 / 环境光）
    若一个资产被 13 场共用，那就是可疑点」。
  ⚠️ 共用**本身不一定错** —— 判定要拿该场的**原版 bundle** 比（原版也共用 ⇒ 我们共用没问题）。

用法：
  PYTHONIOENCODING=utf-8 python 工具/arena_share_scan.py
  PYTHONIOENCODING=utf-8 python 工具/arena_share_scan.py --md > out.md   # 直接吐可贴的 Markdown
只读脚本，不写任何工程文件。
"""
import io, os, re, sys, glob
from collections import defaultdict

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ASSETS = os.path.join(ROOT, 'MyGame', 'Assets')
SCENES = os.path.join(ASSETS, 'CardPresentation', 'Scenes')

GUID_RE = re.compile(r'guid:\s*([0-9a-f]{32})')


def build_guid_map():
    """guid -> 相对 Assets 的路径"""
    m = {}
    for dirpath, _dirnames, filenames in os.walk(ASSETS):
        for fn in filenames:
            if not fn.endswith('.meta'):
                continue
            p = os.path.join(dirpath, fn)
            try:
                head = io.open(p, 'r', encoding='utf-8', errors='replace').read(400)
            except OSError:
                continue
            g = re.search(r'^guid:\s*([0-9a-f]{32})', head, re.M)
            if g:
                rel = os.path.relpath(p[: -len('.meta')], ASSETS).replace('\\', '/')
                m[g.group(1)] = rel
    return m


def parse_rendersettings(text):
    """抽 RenderSettings 块（--- !u!104 &N --- 之后到下一个 --- 之前）"""
    out = {}
    for blk in re.finditer(r'^--- !u!104 &(\d+)\s*$(.*?)(?=^--- |\Z)', text, re.M | re.S):
        body = blk.group(2)
        for key in ('m_Fog', 'm_FogMode', 'm_FogDensity', 'm_FogColor',
                    'm_AmbientMode', 'm_AmbientSkyColor', 'm_AmbientEquatorColor',
                    'm_AmbientGroundColor', 'm_AmbientIntensity', 'm_AmbientLight',
                    'm_SkyboxMaterial', 'm_ReflectionIntensity', 'm_ReflectionBounces',
                    'm_DefaultReflectionMode', 'm_DefaultReflectionResolution',
                    'm_HaloStrength', 'm_FlareStrength', 'm_FlareFadeSpeed'):
            m = re.search(r'^\s*%s:\s*(.+)$' % re.escape(key), body, re.M)
            if m:
                out[key] = m.group(1).strip()
        # 天空盒材质 guid 单抽
        sky = out.get('m_SkyboxMaterial', '')
        g = re.search(r'guid:\s*([0-9a-f]{32})', sky)
        out['_skyboxGuid'] = g.group(1) if g else ''
        break  # 一个场景只有一份 RenderSettings
    return out


def main():
    as_md = '--md' in sys.argv
    guid2path = build_guid_map()
    sys.stderr.write('[scan] guid 表 %d 条\n' % len(guid2path))

    scenes = sorted(glob.glob(os.path.join(SCENES, 'Battle*.unity')))
    per_arena = {}       # arena -> set(guid)
    rs = {}              # arena -> RenderSettings dict
    for sc in scenes:
        name = os.path.basename(sc)[: -len('.unity')]
        arena = name[len('Battle_'):] if name.startswith('Battle_') else name
        text = io.open(sc, 'r', encoding='utf-8', errors='replace').read()
        per_arena[arena] = set(GUID_RE.findall(text))
        rs[arena] = parse_rendersettings(text)

    order = sorted(per_arena)
    print('# 跨场共用资产普查（§28 第①步）\n')
    print('场景数：%d —— %s\n' % (len(order), ' · '.join(order)))

    # ---- 1. 共用 guid ----
    byguid = defaultdict(list)
    for arena in order:
        for g in per_arena[arena]:
            byguid[g].append(arena)
    shared = {g: a for g, a in byguid.items() if len(a) >= 2}

    def bucket(path):
        if not path:
            return '（不在工程里 / 内置 / 已删）'
        for pre, tag in (
            ('WarpforgeArena1/arenas/', 'WarpforgeArena1/arenas/*（逐场目录）'),
            ('WarpforgeArena1/skybox/', 'WarpforgeArena1/skybox'),
            ('WarpforgeArena1/Textures/', 'WarpforgeArena1/Textures'),
            ('WarpforgeArena1/flares/', 'WarpforgeArena1/flares'),
            ('WarpforgeArena1/Models/', 'WarpforgeArena1/Models'),
            ('WarpforgeVFX/', 'WarpforgeVFX'),
            ('CardPresentation/', 'CardPresentation'),
            ('Resources/', 'Resources'),
        ):
            if path.startswith(pre):
                return tag
        return path.split('/')[0]

    print('## 1. 被 ≥2 场共用的资产（全量，按「共用场数」降序）\n')
    print('| 共用场数 | 资产 | 分类 | 用了它的场 |')
    print('|---|---|---|---|')
    for g, arenas in sorted(shared.items(), key=lambda kv: (-len(kv[1]), guid2path.get(kv[0], ''))):
        p = guid2path.get(g, '')
        print('| %d | `%s` | %s | %s |' % (len(arenas), p or g, bucket(p), ' '.join(arenas)))

    print('\n## 2. 逐场专有资产计数（只看 WarpforgeArena1/arenas/ 下**只有它自己用**的）\n')
    print('| 场 | 引用的 arena 目录资产 | 其中只自己用 | 其中与别场共用 |')
    print('|---|---|---|---|')
    for arena in order:
        own = [g for g in per_arena[arena]
               if guid2path.get(g, '').startswith('WarpforgeArena1/arenas/')]
        solo = [g for g in own if len(byguid[g]) == 1]
        sh = [g for g in own if len(byguid[g]) >= 2]
        print('| %s | %d | %d | %d |' % (arena, len(own), len(solo), len(sh)))

    # ---- 3. 交叉引用（A 场的场景引用了 B 场目录里的资产）----
    print('\n## 3. 交叉引用：某场场景里引用了**其它场**目录下的资产（应为 0）\n')
    cross = []
    for arena in order:
        for g in per_arena[arena]:
            p = guid2path.get(g, '')
            m = re.match(r'WarpforgeArena1/arenas/([^/]+)/', p)
            if m and m.group(1) != arena:
                cross.append((arena, m.group(1), p))
    print('命中 %d 条' % len(cross))
    for a, b, p in cross[:60]:
        print('- **%s** 引用了 **%s** 的 `%s`' % (a, b, p))

    # ---- 4. RenderSettings 表 ----
    print('\n## 4. 逐场 RenderSettings（从场景 YAML 直读）\n')
    cols = ['m_AmbientMode', 'm_AmbientSkyColor', 'm_AmbientEquatorColor', 'm_AmbientGroundColor',
            'm_AmbientIntensity', 'm_Fog', 'm_FogMode', 'm_FogDensity', 'm_FogColor',
            'm_ReflectionIntensity', 'm_HaloStrength', 'm_FlareStrength', 'm_FlareFadeSpeed',
            '_skyboxGuid']
    print('| 场 | ' + ' | '.join(cols) + ' |')
    print('|---' * (len(cols) + 1) + '|')
    for arena in order:
        r = rs[arena]
        cells = []
        for c in cols:
            v = r.get(c, '?')
            if c == '_skyboxGuid' and v:
                v = '`%s`' % guid2path.get(v, v)
            cells.append(v)
        print('| %s | %s |' % (arena, ' | '.join(cells)))

    # 天空盒共用情况
    sky = defaultdict(list)
    for arena in order:
        g = rs[arena].get('_skyboxGuid', '')
        sky[g].append(arena)
    print('\n**天空盒 guid 分组**：')
    for g, arenas in sky.items():
        print('- `%s` ← %s' % (guid2path.get(g, g or '(无)'), ' '.join(arenas)))


if __name__ == '__main__':
    main()
