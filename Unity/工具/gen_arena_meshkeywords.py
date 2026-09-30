#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_arena_meshkeywords.py —— 逐网格的**原版材质关键字**旁挂（`<场>_meshkeywords.json`）
================================================================================
为什么要有它（判据 → `资料/战场13场_逐场对账_0920.md` §一 ①-m / `项目任务.md` §三 第 30 条 散件 C）：
  清单的 `meshes[]` **不带 `matKeywords`**（只有 `particles[]` 带）⇒ **网格那一路一个原版关键字都没设过**，
  我们能设的只有 `ArenaOriginalMaterial.ApplyRenderState` **自己算的四个**
  （`_SURFACE_TYPE_TRANSPARENT` / `_ALPHABLEND_ON` / `_ALPHATEST_ON` / `_APPLYAMBIENTCOLOR`）。
  **实测受害**：`battlearenaleviathan` 的 `Toxic Pool Glow`（材质 `Toxic Pool Up light`）原版
  `m_ValidKeywords = ['_SOFT','_SURFACE_TYPE_TRANSPARENT']` ⇒ 我们少了 **`_SOFT`**（软粒子/深度淡出）
  ⇒ 那块大的辉光面**把整屏罩成一片亮黄绿**，**leviathan 亮度比的三分之二出在它身上**。
  （诊断开关 `WF_MESHKEYWORDS=_SOFT` 实测能修好 —— 但它自己写着「**这是诊断开关，不是修法**」。）

做法 = **旁挂**（不动 `工具/scripts快照/gen_unity_arena_manifest.py` 那个共享生成器，也不用重跑 13 份清单）：
  从**原版包**里按 `MeshRenderer` / `SkinnedMeshRenderer` 的 `m_Materials` 取每份材质的
  `m_ValidKeywords`，按 **GameObject 名**写进 `<场>_meshkeywords.json`；
  C# 侧（`ArenaBuilder`）读它、把关键字挂到**运行时**的材质实例上
  （⚠️ 关键字**写不进 `.mat`**：`EnableKeyword` / `shaderKeywords=` 存盘就丢 —— 见
  `ArenaParticleKeywords` 的头部注释与 `资料/已知的坑.md`）。

判据纪律（与 `gen_arena_texslots.py` 同一套）：
  · **材质可能在别的包里**（粒子材质一大半是外链的）⇒ 用 `gen_arena_texslots.build_index()`
    那份**全局材质索引**（`mat[pathID] = [[bundle, 名字, 关键字], …]`），**优先选本场包内的候选**；
  · **同名对象**：清单按 `go` 取；若原版里**同名多份且关键字集合不一致** ⇒ **出声**（不猜，宁可不写）。

用法：
    PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_meshkeywords.py [--arena <场>] [--check]
    （缺省 = 全 13 场；`--check` 只扫不写）
"""
import io
import json
import os
import sys
from collections import defaultdict

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gen_arena_texslots as T          # 复用它的 build_index（判据只留一处）

ROOT = 'd:/4/Unity'

# 🔴 **已知「我们这一侧还不具备、先滤掉」的关键字**（每条都要写清为什么 + 复刻它的前提）：
#   · `_ALPHAMODULATE_ON` —— 语义 = **按顶点色调 alpha**。**我们的网格没有顶点色**
#     （实测：生成器只透传 `v / vt / vn` 三个段，抽出来的 OBJ **0 行**带 r g b；
#      而 Unity 内置的 OBJ 导入器**根本不支持顶点色**）⇒ 关键字开了只会拿**白**去乘
#      ⇒ 比原版**亮**。实测（加这个关键字之前/之后，同一天同口径）：
#       `battlearena3` **1.012 → 1.023**（4 个 `LightShaft`）· `battlearenatauviorla` 0.994 → 0.997
#       （4 个 `Tau Viorla Energy Bar`）。
#     🔴 **2026-09-30 晚更正（铁律 5）**：**`battlearena3` 那 4 个 `LightShaft` 的原版网格【本来就没有顶点色】**
#       —— `battlesharedresources` 的网格 `LightShaft`（49 顶点）`ch3(Color)` 是 `dimension: 0`
#       （按 `MeshFilter.m_Mesh = {fileID:3, pathID:-1361042145547270219}` 解析出来、我独立复核过）。
#       ⇒ **那两处不是「缺顶点色」的例子，别拿它们当这条的理由**（`tauviorla` 的 `Energy Bar` 同理）。
#     ⏭ **要完全复刻的前提** = 把顶点色带进工程。**真账在 §三 第 30 条 散件 C**：
#       全量扫下来原版 **834 个 Mesh 里 132 个有 ch3**，我们这 697 个 OBJ 对得上 **130 个**；
#       颜色**不是解不出、是导出器不写**（`UnityPy/export/MeshExporter.py` 只写 `v/vt/vn/f`）。
# 🔴 **2026-09-30 晚：不再跳过 `_ALPHAMODULATE_ON`（原版就是开的）** ——
#   原来跳过的理由是「我们的网格没有顶点色、开了只会拿白去乘 ⇒ 比原版亮」。现在顶点色进来了
#   （`工具/gen_arena_vertexcolors.py` → `arenas/<场>/<场>_vcol.json`，建场期由
#    `Editor/MeshVertexColors.cs` 按位置贴上去）⇒ **照原版开着**。
#   ⚠️ 两条如实记着：① 带这个关键字的网格**全场只有 8 个**
#     （`battlearena3` 4 个 `LightShaft` ＋ `battlearenatauviorla` 4 个 `Tau Viorla Energy Bar`），
#     而**这 8 个的原版网格本来就没有顶点色**（ch3 dim=0）⇒ 顶点色**改不了它们**；
#     ② 因此它实测仍是 `battlearena3` **1.012 → 1.023**（在原版里那处是**亮光柱**、
#     我们这边是**暗的**）—— 真因**还没查**（另立一条，与顶点色无关）。
SKIP_KEYWORDS = set()


def scan_arena(UnityPy, aa, idx, arena):
    """→ (entries, warns)。entries = [{'go', 'mat', 'keywords'}]"""
    bundle = 'scenes_scenes_%s.bundle' % arena
    path = os.path.join(aa, bundle)
    if not os.path.isfile(path):
        return None, ['没有 bundle %s' % bundle]
    env = UnityPy.load(path)
    go_names, mats = {}, {}
    for o in env.objects:
        try:
            if o.type.name == 'GameObject':
                go_names[o.path_id] = o.read_typetree().get('m_Name', '')
            elif o.type.name == 'Material':
                mats[o.path_id] = o.read_typetree()
        except Exception:
            continue

    def mat_keywords(pid):
        """材质的原版关键字：**先本包、再全局索引**（与 texslots 同一套纪律）。→ (keywords, 来源名)"""
        m = mats.get(pid)
        if m is not None:
            return sorted(set(m.get('m_ValidKeywords') or [])), m.get('m_Name', '')
        cands = (idx.get('mat') or {}).get(str(pid)) or []
        for b, nm, kws in cands:
            if b == bundle:
                return sorted(set(kws or [])), nm
        if cands:
            return sorted(set(cands[0][2] or [])), cands[0][1]
        return None, None

    # GO 名 → 关键字集合（同名可能多份 ⇒ 记下来，冲突就出声）
    by_name = defaultdict(list)
    for o in env.objects:
        if o.type.name not in ('MeshRenderer', 'SkinnedMeshRenderer'):
            continue
        try:
            rr = o.read_typetree()
        except Exception:
            continue
        go = go_names.get((rr.get('m_GameObject') or {}).get('m_PathID'), '')
        if not go:
            continue
        kws, names = [], []
        for mref in (rr.get('m_Materials') or []):
            k, nm = mat_keywords((mref or {}).get('m_PathID', 0))
            if k is None:
                continue
            kws += k
            names.append(nm)
        by_name[go].append((sorted(set(kws)), names[0] if names else ''))

    mf_path = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, arena + '_manifest.json')
    mf = json.load(io.open(mf_path, encoding='utf-8'))
    entries, warns = [], []
    for e in mf.get('meshes') or []:
        go = e.get('go')
        if not go:
            continue
        cands = by_name.get(go) or []
        if not cands:
            continue      # 原版里没有同名渲染器 ⇒ 这本来就不该有（别的旁挂会报这条）
        uniq = {tuple(c[0]) for c in cands}
        if len(uniq) > 1:
            warns.append('%s：原版里同名 %d 份、**关键字集合不一致** ⇒ 不写（不猜）%s'
                         % (go, len(cands), sorted(uniq)))
            continue
        kws = [k for k in cands[0][0] if k not in SKIP_KEYWORDS]
        if kws:
            entries.append({'go': go, 'mat': cands[0][1], 'keywords': kws})
    return entries, warns


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    check = '--check' in sys.argv
    arenas = args or T.ARENAS
    import UnityPy
    aa = next((d for d in T.AA_DIRS if os.path.isdir(d)), None)
    if aa is None:
        print('找不到 aa 目录（原始 bundle）')
        return 1
    idx = T.build_index(UnityPy, aa)
    total = 0
    for arena in arenas:
        entries, warns = scan_arena(UnityPy, aa, idx, arena)
        if entries is None:
            print('[%s] %s' % (arena, warns[0]))
            continue
        print('[%s] 带原版关键字的网格 %d 个（共 %d 个关键字实例）'
              % (arena, len(entries), sum(len(e['keywords']) for e in entries)))
        for w in warns:
            print('    ⚠️ %s' % w)
        for e in entries[:4]:
            print('    %-26s %-28s %s' % (e['go'], e['mat'], ','.join(e['keywords'])))
        if len(entries) > 4:
            print('    …（另 %d 条）' % (len(entries) - 4))
        total += len(entries)
        if check:
            continue
        dst = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, arena + '_meshkeywords.json')
        io.open(dst, 'w', encoding='utf-8', newline='\n').write(
            json.dumps({'scene': arena, 'meshes': entries}, ensure_ascii=False, indent=2))
        print('    → 写 %s' % dst)
    print('合计：%d 个网格带原版关键字' % total)
    return 0


if __name__ == '__main__':
    sys.exit(main())
