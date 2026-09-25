#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把「`renderMode = 4 (Mesh)` 的粒子」用的**网格名**解析出来 → `arenas/<场>/<场>_psmesh.json`。

**为什么需要它**：`ArenaBuilder` 里有一行**显式的降级** ——
```csharp
int rm = Mathf.Clamp(p.renderMode, 0, 4);
rend.renderMode = rm == 4 ? ParticleSystemRenderer.Billboard : (ParticleSystemRenderer)rm;
```
即 **`renderMode = 4 (Mesh)` 的粒子被当成 Billboard 画**（当时的理由写在清单里：**清单没带网格**）。
实测规模（13 场全量）：**19 颗 / 4 场** ——
`tauviorla` 10（`Sphere lightning` / `Muzzle Flash sphere`）· `blacklegion` 6（`Light Shaft`）·
`arena3` 2（`Necrons Close Monolith Rays`）· `darkangels` 1（`Generator Emit`）。
⚠️ 其中 **tauviorla（1.033）与 blacklegion（1.076）都还在 >±5% 名单里**。

**为什么不能在建场器里直接读**：`m_Mesh` 可能是**跨包引用**（`tauviorla` 那 10 颗的网格在
`battlesharedresources_assets_all.bundle` 里，场景 bundle 里没有）⇒ 那一类要扫全库。

🔴 **一条踩过的（第一版就这么错）**：**解析一律先走 UnityPy 的 PPtr（`PPtr.read()`）**，
它按 bundle 的文件表解，文件内 / 同包跨文件都对（实测 `darkangels` 的 `{fileID 2, pathID 88}` → `Generator.001` ✓）。
**别拿 PathID 去全库扫小号** —— `88` 这种号每个包都有，先扫到的会是别的包的 `Bunker Foreground1`，
**静默拿错网格**（而且它在原版里也画得出来，只是画的是错的形状）。只有 PPtr 解不出来时才做全库兜底。

写出的旁挂数据只存**网格名**；**「网格名 → 哪个 .obj」由生成器侧用它自己的 `obj_resolver` 解决**
（那份索引同时管主包与共享包，判据只留一处）。

用法：
    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_psmesh.py            # 全 13 场，写
    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_psmesh.py --check    # 只报不写
"""
import io
import json
import os
import sys

AA = 'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
ROOT = 'd:/4/Unity'
ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']


def main():
    import UnityPy
    check = '--check' in sys.argv
    result, need_global = {}, set()

    for a in ARENAS:
        p = os.path.join(AA, 'scenes_scenes_%s.bundle' % a)
        if not os.path.isfile(p):
            print('  [跳过] 没有 bundle：%s' % a)
            continue
        env = UnityPy.load(p)
        gos = {}
        for o in env.objects:
            if getattr(o.type, 'name', '') == 'GameObject':
                try:
                    gos[o.path_id] = o.read().m_Name
                except Exception:
                    pass
        rows = []
        for o in env.objects:
            if getattr(o.type, 'name', '') != 'ParticleSystemRenderer':
                continue
            try:
                d = o.read()
            except Exception:
                continue
            if getattr(d, 'm_RenderMode', None) != 4:
                continue
            m = getattr(d, 'm_Mesh', None)
            pid = m.path_id if m else 0
            name = None
            if m is not None:
                try:
                    name = m.read().m_Name          # ← PPtr 解析（文件内 / 同包跨文件都对）
                except Exception:
                    name = None                     # ← 真·跨包，待兜底
            g = d.m_GameObject.path_id if d.m_GameObject else 0
            rows.append((gos.get(g, '?'), pid, name))
            if name is None and pid:
                need_global.add(pid)
        if rows:
            result[a] = rows

    # 真·跨包：扫全库（按包名排序，凑齐就走）
    ghits, done = {}, set()
    if need_global:
        print('--- 跨包兜底：%d 个 PathID ---' % len(need_global))
        for fn in sorted(os.listdir(AA)):
            left = need_global - done
            if not left:
                break
            if not fn.endswith('.bundle'):
                continue
            try:
                env = UnityPy.load(os.path.join(AA, fn))
            except Exception:
                continue
            for o in env.objects:
                if o.path_id in left and getattr(o.type, 'name', '') == 'Mesh':
                    try:
                        ghits[o.path_id] = (o.read().m_Name, fn)
                        done.add(o.path_id)
                    except Exception:
                        pass

    print('=== renderMode=4 的粒子网格 ===')
    bad = []
    for a, rows in result.items():
        clean = {}
        for go, pid, name in rows:
            src = 'PPtr'
            if name is None and pid in ghits:
                name, src = ghits[pid][0], ghits[pid][1]
            if name:
                clean[go] = name
            else:
                bad.append('%s/%s(pid=%s)' % (a, go, pid))
            print('  %-24s %-34s %-22s -> %-20s (%s)' % (a, go, pid, name or '**解析不出**', src))
        dst = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', a, a + '_psmesh.json')
        if not check:
            io.open(dst, 'w', encoding='utf-8', newline='\n').write(
                json.dumps(clean, ensure_ascii=False, indent=1) + '\n')
            print('  → 已写 %s（%d 条）' % (dst, len(clean)))
    print(('🔴 解析不出：%s' % ', '.join(bad)) if bad else '✅ 全部解析出来了')


if __name__ == '__main__':
    main()
