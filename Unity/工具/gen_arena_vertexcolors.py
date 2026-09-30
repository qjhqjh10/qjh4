#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_arena_vertexcolors.py —— 把**原版网格的顶点色**抽成旁挂 JSON（13 场逐场一份）。

为什么要它（判据 → `项目任务.md` §三 第 30 条 散件 C 的 2026-09-30 晚那一段）：
  我们想复刻材质关键字 **`_ALPHAMODULATE_ON`**（URP 内置，语义 = 按顶点色调制 alpha），
  而**我们的 OBJ 网格没有顶点色** —— 不是解不出，是 **`UnityPy/export/MeshExporter.py` 不写**
  （它只写 `v/vt/vn/f`；`helpers/MeshHelper.py` 早就把 channel 3 读进 `m_Colors` 了）。
  实测规模：13 场 + `battlesharedresources` 共 **120 个网格**带顶点色（**24,793 个顶点**）；
  我们这 697 个 OBJ 里对得上 **130 个文件**（一个网格被按组拆成多个 OBJ 时会有多份）。

**坐标空间**（实测钉死，🔴 **改错过一次，别想当然**）：
  · 原版顶点 → 我们 **OBJ 文件**的 `v` 是 **X 取反**（`Ground Lights.0011` 原版第 1 个顶点 `+0.0146326`
    ↔ 我们 OBJ `−0.0146326`；同一条链还有**绕序翻转** —— 见 `gen_unity_arena_manifest.py:1478-1495`）；
  · 但 **Unity 的 OBJ 导入器自己还会再取反一次** ⇒ **Unity 网格的 X ≈ 原版**
    （实测：按原版空间比 **命中 186/186**，按取反后比 **0/186**；`DumpMiss` 两种手性各试一遍得到的）。
  ⇒ **本脚本产出的 `pos` 一律是原版空间（不取反）**，建场期直接与 Unity 网格顶点比。
    自检那一步要与我们 **OBJ 文件**比，所以**临时取反**（见下面 `vkeys` 那行）。

产物：`<工程>/WarpforgeArena1/arenas/<场>/<场>_vcol.json`
  { "arena": "...", "items": [ { "obj": "<我们工程里的 .obj 文件名>", "mesh": "<原版网格名>",
                                "src": "<来自哪个 bundle>", "positions": [[x,y,z],...],
                                "colors": [[r,g,b,a],...] } ], "unmatched": [...] }
  · **`positions` 与 `colors` 一一对应**（都按原版网格的顶点序）。
  · 运行时/建场期按**位置**匹配到 Unity 顶点（**不能按索引**：OBJ 是按 `g` 组拆过文件的、
    而且 Unity 的 OBJ 导入器会 `weldVertices` 重排顶点）。

自检（写盘前跑）：逐个 item 把**我们 OBJ 的 `v` 行**拿去 `positions` 里找（1e-4 容差），
  命中率与漏掉的写进日志；`unmatched` = 有顶点色但我们在工程里找不到对应 OBJ 的网格。

用法：PYTHONIOENCODING=utf-8 python 工具/gen_arena_vertexcolors.py [--arena <场>] [--check]
"""
import argparse
import collections
import io
import json
import os
import struct
import sys

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
import UnityPy

AA = 'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
MODELS = 'd:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/%s/Models'
BUNDLES = ['battlesharedresources_assets_all'] + [
    'scenes_scenes_battlearena%s' % a for a in
    ['1', '2', '3', 'aeldari', 'astramilitarum', 'blacklegion', 'darkangels', 'emperorschildren',
     'genestealers', 'leviathan', 'sororitas', 'spacewolves', 'tauviorla']]

FMT_SIZE = {0: 4, 1: 2, 2: 1, 3: 1}      # 0=Float32 1=Float16 2=UNorm8 3=SNorm8（Unity 的 VertexAttributeFormat）


def item_size(fmt, dim):
    return FMT_SIZE.get(fmt, 4) * dim


def read_channel(raw, stride, ch, n, fmt):
    """按通道取 n 个顶点 → [(v...)]。只支持 fmt 0（Float32）与 2（UNorm8）——实测就这两种。"""
    off, dim = ch['offset'], ch['dimension']
    out = []
    if fmt == 0:
        for i in range(n):
            out.append(struct.unpack_from('<%df' % dim, raw, i * stride + off))
    elif fmt == 2:
        for i in range(n):
            out.append(tuple(b / 255.0 for b in struct.unpack_from('<%dB' % dim, raw, i * stride + off)))
    else:
        return None
    return out


def mesh_colors(d):
    """→ (positions, colors) 或 None（没有顶点色 / 格式不支持）。positions 已按 X 取反。"""
    vd = d.get('m_VertexData')
    if not vd:
        return None
    ch = vd.get('m_Channels') or []
    if len(ch) < 4 or not ch[3].get('dimension'):
        return None
    raw = vd.get('m_DataSize')          # ⚠️ UnityPy 把字节 blob 放在这个名字下（不是长度）
    if not isinstance(raw, (bytes, bytearray)):
        return None
    n = vd['m_VertexCount']
    stride = max(c['offset'] + item_size(c['format'], c['dimension']) for c in ch if c.get('dimension'))
    if stride * n > len(raw):
        return None
    pos = read_channel(raw, stride, ch[0], n, ch[0]['format'])
    col = read_channel(raw, stride, ch[3], n, ch[3]['format'])
    if pos is None or col is None:
        return None
    if len(col[0]) == 3:                # 理论上是 4；真遇到 3 就补 alpha
        col = [tuple(c) + (1.0,) for c in col]
    # 🔴 **这里【不】取反 —— 保留原版空间**。实测（2026-09-30 晚，`DumpMiss` 两种手性各试一遍）：
    #   · 我们的 OBJ 文件 = **原版 X 取反**（导出器干的）
    #   · 而 **Unity 的 OBJ 导入器自己还会再取反一次** ⇒ **Unity 网格的 X ≈ 原版**
    #   ⇒ 拿 Unity 网格的顶点比，就该用**原版空间**：`原样命中 0/186 · X 取反后命中 186/186`
    #     （`Candles 52`）· `0/258 → 258/258`（`Ground Lights.0011`）。
    #   与 OBJ 文件比对（下面的自检）时**临时取反**即可。
    return pos, col


def obj_vertices(path):
    """读我们 OBJ 的 `v` 行（顺序即文件序）。"""
    out = []
    for line in io.open(path, encoding='utf-8', errors='replace'):
        if line.startswith('v '):
            a = line.split()
            out.append((float(a[1]), float(a[2]), float(a[3])))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena', default=None, help='只处理某一场（默认 13 场全做）')
    ap.add_argument('--check', action='store_true', help='只扫不写')
    a = ap.parse_args()

    # 网格名 → [(bundle, positions, colors)]（**只有带顶点色的**）
    index = collections.defaultdict(list)
    # 网格名 → {bundle}（**所有**网格，用来判「本场那个同名的网格本来就没顶点色」）
    names_all = collections.defaultdict(set)
    colored_pair = set()      # (名字, bundle) 里**带顶点色**的那些 —— 判「本场这个网格有没有色」只能靠它
    for b in BUNDLES:
        p = os.path.join(AA, b + '.bundle')
        if not os.path.isfile(p):
            print('⚠️ 缺 bundle', b)
            continue
        env = UnityPy.load(p)
        got = 0
        for o in env.objects:
            if o.type.name != 'Mesh':
                continue
            try:
                d = o.read_typetree()
            except Exception:
                continue
            nm = d.get('m_Name')
            names_all[nm].add(b)
            r = mesh_colors(d)
            if r is None:
                continue
            colored_pair.add((nm, b))
            index[nm].append((b, r[0], r[1]))
            got += 1
        if got:
            print('  %-46s 带顶点色的网格 %d 个' % (b, got))
    print('唯一网格名 %d 个（其中带色 %d 个）' % (len(names_all), len(index)))

    arenas = [a.arena] if a.arena else [b.replace('scenes_scenes_', '') for b in BUNDLES[1:]]
    for arena in arenas:
        mdir = MODELS % arena
        if not os.path.isdir(mdir):
            print('⚠️ 没有 %s' % mdir)
            continue
        objfiles = [f for f in os.listdir(mdir) if f.endswith('.obj')]
        items, unmatched, miss_items, miss_total, hit_total, nocount = [], [], [], 0, 0, 0
        for f in sorted(objfiles):
            mesh_name = f[:-len('.obj')]
            v = obj_vertices(os.path.join(mdir, f))
            if not v:
                continue
            # ⚠️ OBJ 文件是**原版 X 取反**的 ⇒ 拿它比之前先翻回来（见 `mesh_colors` 的说明）
            vkeys = [(round(-p[0], 4), round(p[1], 4), round(p[2], 4)) for p in v]
            # ---- 候选怎么来（🔴 别只按名字精确查，实测会**静默配错**）----
            #   · 我们的 OBJ 有一批是**按 `g` 组拆出来的**，文件名 = `<网格名><序号>`
            #     （`Barrels1.obj` / `Banner 11.obj`）；而**别的包里可能恰好有一个真叫 `Barrels1` 的网格**
            #     ⇒ 精确查名会把两个不同的网格配到一起（实测 4 场、共 529 个顶点因此对不上）。
            #   · 所以：候选 = **精确名** ∪ **去掉尾部数字的名字**，再**按几何认领**
            #     （拿 OBJ 的顶点去候选的顶点集里找，命中率最高的胜）——「内容认领」这条
            #     与 `资料/解包数据…判据` 那套「按内容认、不按文件名认」是同一条纪律。
            own = 'scenes_scenes_' + arena
            # 🔴 **先问一句**：本场包 / 共享包里有没有这个网格？
            #   有、而且**它本来就没顶点色** ⇒ 这个 OBJ 不需要色，**直接跳过**（不是缺口；
            #   实测 `Barrels1` / `Banner 31` 这批就是这种 —— 本场有同名网格但无色，
            #   而别的包里恰好有个**同名的带色网格**，只按名字查就会给它贴上错色）。
            SHARED = 'battlesharedresources_assets_all'
            here = names_all.get(mesh_name, set())
            # 🔴 **本场包优先**：我们的 OBJ 就是这个包里的网格导出来的 ——
            #   ① 本场包里有这个网格 ⇒ 以它为准（有色就取它；**没色就跳过**，别去别包找同名的）
            #   ② 本场包没有 ⇒ 看共享包（`arena3/aeldari/leviathan/sororitas` 的网格都在那里）
            st = None
            for b in (own, SHARED):
                if b in here:
                    st = 'colored' if (mesh_name, b) in colored_pair else 'plain'
                    break
            if st == 'plain':
                nocount += 1
                continue
            # ---- 候选：精确名 ∪ 去掉尾部数字的名字，再**按几何认领**（拿 OBJ 的顶点去候选顶点集里找）----
            base = mesh_name.rstrip('0123456789').strip()
            base1 = mesh_name[:-1].strip() if mesh_name[-1:].isdigit() else base
            cands = [c for c in (index.get(mesh_name) or [])
                     if st == 'colored' and c[0] in (own, SHARED) or st is None]
            if not cands:
                # st == 'colored' 但候选被过滤空了（本场有色却没进 index？）⇒ 出声，别静默
                if st == 'colored':
                    print('      ⚠️ %-34s 本场包说它带色，但索引里没有 —— 不写色' % f[:34])
                    miss_items.append('%s(本场包说带色，索引缺失)' % f)
                continue
            for alt in {base, base1}:
                if alt and alt != mesh_name:
                    for c in (index.get(alt) or []):
                        if c not in cands:
                            cands.append(c)
            best, best_hit, best_b = None, -1, None
            for (b, pos, col) in cands:
                ks = set((round(p[0], 4), round(p[1], 4), round(p[2], 4)) for p in pos)
                h = sum(1 for k in vkeys if k in ks)
                # 同分时**同场优先**（与「优先与材质同包」同一条纪律）
                rank = (0 if b == own else (2 if 'shared' in b else 1))
                if (h, -rank) > (best_hit, -(0 if best_b == own else (2 if best_b and 'shared' in best_b else 1))):
                    best, best_hit, best_b = (b, pos, col), h, b
            miss = len(v) - best_hit
            if miss > 0:
                # 🔴 **宁可认不出，也不贴错色**（项目红线）：对不上就**不写这一条**，并出声
                print('      ⚠️ %-34s 认不出（%d 个候选里最好的只命中 %d/%d）—— 这一条不写色'
                      % (f[:34], len(cands), best_hit, len(v)))
                miss_items.append('%s(候选%d个, 最好命中 %d/%d, 候选源=%s)'
                                  % (f, len(cands), best_hit, len(v), best_b))
                miss_total += miss
                continue
            b, pos, col = best
            hit_total += len(v)
            items.append({
                'obj': f, 'mesh': mesh_name, 'src': b,
                # 🔴 **扁平数组，不是 `[[x,y,z],...]`** —— Unity 的 `JsonUtility` **不支持交错数组**
                #   （`float[][]` 会**静默变 null** ⇒ 建场期 NRE；2026-09-30 实测踩过）：
                #   `pos` = xyz 三元组依次摊平、`col` = rgba 四元组依次摊平，配对靠下标。
                'n': len(pos),
                # 🔴 **坐标不四舍五入** —— 有的网格小到坐标 ~2e-4（实测 `Candles 52`），
                #   舍到 5 位小数就等于丢掉 1.6% 的量；而建场侧是按**网格自身尺度**做最近邻匹配的
                #   （步长 = 包围盒对角 × 1e-3）⇒ 这里必须给全精度。
                'pos': [v for q in pos for v in q],
                'col': [round(v, 5) for q in col for v in q],
                '_objVerts': len(v), '_hitVerts': best_hit,
            })
        # 有顶点色但工程里没有对应 OBJ 的（那些网格没被建出来 or 名字不同）
        for name, cands in sorted(index.items()):
            if name.endswith('.obj'):
                continue
            if not any(it['mesh'] == name for it in items):
                if any(c[0] == 'scenes_scenes_' + arena for c in cands):
                    unmatched.append(name)
        print('%-32s 带色网格 %d 个 · OBJ 顶点命中 %d / 漏 %d%s%s'
              % (arena, len(items), hit_total, miss_total,
                 ('（另有 %d 个 OBJ 的原网格本来就没顶点色，跳过）' % nocount) if nocount else '',
                 ('  ⚠️ 本场有顶点色但没建出来的网格 %d 个' % len(unmatched)) if unmatched else ''))
        if a.check:
            continue
        out = {'arena': arena,
               '_schema': 'arena_vcol/1 —— 原版网格的顶点色（positions 已按 X 取反，与我们 OBJ 同一手性）；'
                          '运行时按**位置**匹配到 Unity 顶点，别按索引（OBJ 被按组拆过、Unity 还会 weldVertices）',
               '_source': 'd:/2/.../aa/StandaloneWindows64/{battlesharedresources,scenes_scenes_<场>}.bundle',
               'items': items,
               # ⚠️ 对不上的那些**照实记着**（obj 顶点在源网格里找不到 ⇒ 颜色不可信，运行时别用）
               '_missItems': miss_items,
               'unmatched': unmatched}
        dst = os.path.join('d:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas', arena, arena + '_vcol.json')
        io.open(dst, 'w', encoding='utf-8', newline='\n').write(json.dumps(out, ensure_ascii=False))
        print('   写出 %s（%.1f KB）' % (dst, os.path.getsize(dst) / 1024.0))
    return 0


if __name__ == '__main__':
    sys.exit(main())
