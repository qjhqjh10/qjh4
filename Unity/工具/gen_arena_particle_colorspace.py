#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_arena_particle_colorspace.py — 把原版每颗粒子的
**`ParticleSystemRenderer.m_ApplyActiveColorSpace`** 补进我们 13 场的
`<场>_manifest.json` 的 `particles[i].applyActiveColorSpace`。

**为什么**（2026-10-11 · 账号 A1557 · 上一轮 `A1552` 的 F2 报告 §五·2 点名要做）：
  上一轮把 `m_UseCustomVertexStreams` / `m_VertexStreams` 接上之后，
  F2 明确写下：「`applyActiveColorSpace` 才是会改【静止帧】亮/色的那一项」
  —— 顶点流治的是**帧间交叉淡入**（动画平滑度），静止帧读数几乎不动。
  `ParticleSystemRenderer` 上就剩这一项还没接。

**原版值（13 场 578 颗实测，本脚本逐颗重读）**：
  `m_ApplyActiveColorSpace` = **True 401 / False 177**（`battlearena1` 6 True / 28 False）。
  我们**一律** `True`（Unity 新建 `ParticleSystemRenderer` 的默认值）
  ⇒ 这一项从来没抽过、也没消费过。

🔴 **语义（判据 = 本机装的那一版 Unity 自己的字串，不是我推的）**：
  · Unity 6000.3.23f1 随编辑器发的 `UnityEngine.ParticleSystemModule.xml`
    （`P:UnityEngine.ParticleSystemRenderer.applyActiveColorSpace`）：
    「Set whether colors will be converted appropriately before being passed to the GPU
      when using Linear Rendering.」
  · 同一版编辑器的**复选框 tooltip**（`UnityEditor.CoreModule.dll` 里的 UTF-16 串，
    与 `TrailRenderer` / `LineRenderer` 共用同一句）：
    「When using Linear Rendering, colors will be converted appropriately before being
      passed to the GPU.」
  · 三个组件（ParticleSystemRenderer / TrailRenderer / LineRenderer）**都是「CPU 侧生成顶点色」的那一类**
    ⇒ 这个开关管的是**顶点色在送 GPU 之前要不要做色彩空间换算**。
  · ⚠️ **方向（sRGB→Linear 还是反过来）本地读不到** —— 实现在 native，Unity 只写「appropriately」。
    按 Unity 色彩空间惯例是「把作者在检视器里挑的颜色换算到当前色彩空间」，**本报告标为未直接查实**。
  · ✅ **本工程和原版都是 Linear**（我们 `ProjectSettings.asset:50 m_ActiveColorSpace: 1`；
    原版由 `资料/普查产出_0920/场景光照与后处理_原版规格.md:130-131` 的实况 probe 反证）
    ⇒ 这个开关**在原版和在我们这里都是活的**，不是死值。

⚠️ **上游生成器（`d:/2/Warpforge_tools/scripts/gen_unity_arena_manifest.py`）还没教这个字段** ——
  它不在本仓白名单内。**重跑生成器会把这个键冲掉** ⇒ 冲掉了就再跑一次本脚本。
  （与 `工具/gen_arena_particlepivot.py` / `gen_arena_particle_streams.py` 同路：都是往清单上**旁挂**。）

🔴 **运行顺序有依赖**：`gen_arena_particlepivot.py` → `gen_arena_particle_streams.py` → **本脚本**
  （本脚本拿 `"vertexStreams"` 行当锚点；顺序反了会**报错停下**，不会写坏）。

**幂等**：已经有这个键的粒子**原地替换**，没有的插在 `"vertexStreams"` 行之后。
**保格式**：逐字节只动要动的那些行，**行尾照旧 CRLF**。

用法：
  D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_arena_particle_colorspace.py
  D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_arena_particle_colorspace.py --arena battlearena1
  D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_arena_particle_colorspace.py --dry
"""
import argparse
import io
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, 'd:/2/Warpforge_tools/scripts')

import arena_particle_audit as A   # noqa: E402

MANIFEST_DIR = 'd:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/{arena}/{arena}_manifest.json'

# 锚点 = `vertexStreams` 那一行（上一轮 `A1557-F2` 已保证它逐颗都在）
ANCHOR = r'(?m)^      "vertexStreams": \[[^\]]*\],\r\n'
KEY_RE = r'(?m)^      "applyActiveColorSpace": (?:true|false),\r\n'


def build_table(arena):
    """→ [(idx, go, bool)] —— 逐下标对齐（名字必须全等，否则报错退出）。"""
    recs, _a = A.collect(arena)
    man = A.load_manifest(arena)
    mp = man['particles']
    if len(recs) != len(mp):
        raise SystemExit(f'❌ {arena}：原版 {len(recs)} 颗 vs 清单 {len(mp)} 颗 —— 对不上，先查生成器')
    out = []
    for i, (r, m) in enumerate(zip(recs, mp)):
        if r['name'] != m['go']:
            raise SystemExit(f'❌ {arena} 下标 {i}：原版 `{r["name"]}` vs 清单 `{m["go"]}` —— '
                             '顺序不一致，拒绝按下标注入（会静默错配）')
        v = r['psr'].get('m_ApplyActiveColorSpace')
        if v is None:
            raise SystemExit(f'❌ {arena} `{r["name"]}`：原版 `ParticleSystemRenderer` 里**没有** '
                             '`m_ApplyActiveColorSpace` 字段 —— 别猜一个默认值填进去，先查清楚')
        out.append((i, m['go'], bool(v)))
    return out


def inject(arena, dry=False):
    path = MANIFEST_DIR.format(arena=arena)
    raw = io.open(path, 'rb').read()
    n_crlf = raw.count(b'\r\n')
    n_lf = raw.count(b'\n')
    if n_crlf != n_lf:
        raise SystemExit(f'❌ {arena}：清单行尾不是纯 CRLF（CRLF {n_crlf} / LF {n_lf}）—— 先停下')
    s = raw.decode('utf-8')

    table = build_table(arena)

    n_anchor = len(re.findall(ANCHOR, s))
    if n_anchor != len(table):
        raise SystemExit(f'❌ {arena}：`"vertexStreams"` 行 {n_anchor} 条 vs 粒子 {len(table)} 颗 —— '
                         '锚点不唯一/不齐。先跑 `工具/gen_arena_particle_streams.py`。')

    # 幂等：先把已有的键整行删掉
    n_replaced = len(re.findall(KEY_RE, s))
    s = re.sub(KEY_RE, '', s)

    anchor_end = [m.end() for m in re.finditer(ANCHOR, s)]
    if len(anchor_end) != len(table):
        raise SystemExit(f'❌ {arena}：删掉旧键之后锚点变成 {len(anchor_end)} 条 —— 清单结构不对，停下')

    out = []
    last = 0
    n_false = 0
    for pos, (i, go, v) in zip(anchor_end, table):
        line = f'      "applyActiveColorSpace": {"true" if v else "false"},\r\n'
        out.append(s[last:pos])
        out.append(line)
        last = pos
        if not v:
            n_false += 1
    out.append(s[last:])
    s2 = ''.join(out)

    # 校验：JSON 还能解出来，且每颗都有这个键、值与原版逐颗相等
    chk = json.loads(s2)
    assert len(chk['particles']) == len(table), '粒子数变了'
    for (i, go, v), p in zip(table, chk['particles']):
        assert p.get('applyActiveColorSpace') is v, f'idx {i} applyActiveColorSpace 没落对'
        assert p['go'] == go, f'idx {i} 名字错位'

    print(f'=== {arena}：{len(table)} 颗 · 原版 `m_ApplyActiveColorSpace=False` **{n_false}** 颗'
          f'（True {len(table) - n_false}）'
          + (f' · 覆盖已有键 {n_replaced} 处' if n_replaced else ''))
    if dry:
        return n_false, len(table)
    tmp = path + '.tmp'
    io.open(tmp, 'wb').write(s2.encode('utf-8'))
    os.replace(tmp, path)
    back = io.open(path, 'rb').read()
    assert back.count(b'\r\n') == back.count(b'\n'), '写回后行尾被翻了'
    print(f'    ✅ 写回 {path}（{len(back)} 字节，CRLF 保持）')
    return n_false, len(table)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena', default=None)
    ap.add_argument('--dry', action='store_true')
    args = ap.parse_args()
    arenas = [args.arena] if args.arena else A.ARENAS
    tot_f = tot_n = 0
    for a in arenas:
        f, n = inject(a, dry=args.dry)
        tot_f += f
        tot_n += n
    print(f'=== 13 场合计：{tot_n} 颗 · `m_ApplyActiveColorSpace=False` {tot_f} 颗 / True {tot_n - tot_f} 颗')
    print('    ⚠️ 上面是【清单里】的数。**产物里该看到的是实际建出的 PSR 数**'
          '（清单里有一部分没建：无贴图 / 原版关着 / renderMode=None）——'
          ' 预期看 `ArenaBuilder` 的「色彩空间自检」那一行（⛔ 别在这里写死颗数）。')


if __name__ == '__main__':
    main()
