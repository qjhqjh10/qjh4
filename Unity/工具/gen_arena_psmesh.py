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

🔴 **A160（2026-10-06）**：兜底那一趟**本身就是「拿 PathID 去全库扫」**，所以它必须**把撞号说出来** ——
实测跨包同 pid 不同名的 **Mesh 82 条**。现在它扫**全部**包、把候选**全列出来**（原来「凑齐就走」+
按包名排序 = 先到先得且不报）。取值仍是包名序第一条（= 原口径），但**有一条就点名一条**。

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

    # 真·跨包：扫全库
    # 🔴 2026-10-06 修（A160 / 普查 `资料/普查产出_1006/A152_pid陷阱普查.md` A5）：
    #   原来的写法是「**凑齐就走**（`left = need_global - done` + `break`）+ 按**包名排序**」
    #   ⇒ **先到先得**，而且**不记 CAB、不报撞号**：一个 pid 在多个包里都存在时，
    #   字母序靠前的那个包**静默**赢。实测跨包同 pid 不同名的 **Mesh 82 条**
    #   （`astramilitarum` 涉 80、`emperorschildren` 77）⇒ 会挑到**错误场次**的网格名。
    #   修法（照 `gen_vfx_texture_mips.py:84-86` 的正例：撞号就大声报、**不静默挑一个**）：
    #   **扫完所有包**、把候选全列出来；取值仍按包名序取第一条（= 原口径，不改变已有产物），
    #   但凡有「同 pid 不同名」就**点名报出来**，让人知道这一条不可信。
    ghits = {}
    if need_global:
        print('--- 跨包兜底：%d 个 PathID（扫全库，撞号要点名）---' % len(need_global))
        for fn in sorted(os.listdir(AA)):
            if not fn.endswith('.bundle'):
                continue
            try:
                env = UnityPy.load(os.path.join(AA, fn))
            except Exception:
                continue
            for o in env.objects:
                if o.path_id in need_global and getattr(o.type, 'name', '') == 'Mesh':
                    try:
                        nm = o.read().m_Name
                    except Exception:
                        continue
                    cab = getattr(getattr(o, 'assets_file', None), 'name', None)
                    ghits.setdefault(o.path_id, []).append((fn, cab, nm))
            del env
        for pid, cands in sorted(ghits.items()):
            names = sorted({c[2] for c in cands})
            if len(names) > 1:
                print('    🔴 pathID %s 在 %d 个包里都有、且**名字不同**（不静默挑一个；'
                      '下面取的是包名序第一条）：' % (pid, len(cands)))
                for fn, cab, nm in cands[:8]:
                    print('        %-46s %-34s %s' % (fn, cab or '-', nm))
        ghits = {p: (v[0][2], v[0][0]) for p, v in ghits.items()}

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
