#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_arena_particle_streams.py — 把原版每颗粒子的 **`ParticleSystemRenderer.m_UseCustomVertexStreams`
+ `m_VertexStreams`** 补进我们 13 场的 `<场>_manifest.json` 的
`particles[i].useCustomVertexStreams` / `particles[i].vertexStreams`。

**为什么**（2026-10-11 · 账号 A1557）：
  `Barrels` 旁那团火（`TinyFlames`）我们渲出来**又小又暗、没有火苗形状**，而原版是一簇簇分明的火舌。
  上一轮（`A1552`）已把 `m_Pivot` 接上（贴地火焰少半截那个坑，实测已生效），**火仍然不对**；
  逐字段对完，`ParticleSystemRenderer` 上**只剩三项我们没接**，这是第 ①项（嫌疑最大的一项）。

**原版值（13 场 578 颗实测）**：
  · `m_UseCustomVertexStreams` = **True 179 / False 399**（`battlearena1` 27/34 True）；
  · `m_VertexStreams` 共 **11 种**取值，`TinyFlames` = **`[0, 3, 4, 5, 8]`**、`WildFire` 同；
  · 我们**一律**是 Unity 默认（`False` + `[0,1,3,4]`）⇒ 这两项从来没抽过、也没消费过。

🔴 **序数 → 名字的对照（唯一判据 = 游戏自己的 IL2CPP dump）**：
  `d:/2/tools/il2cpp_out/dump.cs:1089677` 的 `UnityEngine.ParticleSystemVertexStream`：
  `0 Position · 1 Normal · 2 Tangent · 3 Color · 4 UV · 5 UV2 · 6 UV3 · 7 UV4 · 8 AnimBlend ·
   9 AnimFrame · 10 Center · 11 VertexID … 23 StableRandomX … 31 Custom1X · 32 Custom1XY …`
  ⚠️ **不能用 `ParticleSystemVertexStreams`（复数）那张表** —— 那一张是**位标志**
  （`Position=1 · Normal=2 · … · UV2BlendAndFrame=32 · Custom1=2048`，同 dump `:1089794` 起），
  与 `m_VertexStreams` 里的小序数**不是同一套编号**。
  ⚠️ 本工程的目标 Unity（6000.3.23f1）里这张表的**序数逐条相同**（本脚本作者用 Roslyn 读
  `UnityEngine.ParticleSystemModule.dll` 的 Constant 表逐条比过）⇒ 可以按序数直接搬。

**为什么它重要**（机制见报告 §机制）：
  `UV2(5)` 写 `TEXCOORD0.zw`、`AnimBlend(8)` 写 `TEXCOORD1.x` —— 正是 URP
  `Particles/Unlit` 在 `_FLIPBOOKBLENDING_ON` 下读的那两个量
  （`ShaderLibrary/Particles.hlsl` 的 `GetParticleTexcoords` 非实例化支 + `SampleAlbedo`）。
  原版 `TinyFlame` / `LargeFlame02` 材质**开着这个关键字**、原版流里**正好带 5+8**（53/53 命中）。

⚠️ **上游生成器（`d:/2/Warpforge_tools/scripts/gen_unity_arena_manifest.py`）还没教这两个字段** ——
  它不在本仓白名单内。**重跑生成器会把这批键冲掉** ⇒ 冲掉了就再跑一次本脚本。
  （这与 `工具/gen_arena_particlepivot.py` 同路：都是往清单上**旁挂**。）

**幂等**：已经有这两个键的粒子**原地替换**，没有的插在 `"pivot"` 之后（没有 pivot 就插在 `"duration"` 之前）。
**保格式**：逐字节只动要动的那些行，**行尾照旧 CRLF**。

用法：
  D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_arena_particle_streams.py
  D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_arena_particle_streams.py --arena battlearena1
  D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_arena_particle_streams.py --dry
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

# m_VertexStreams 里出现过的最大序数（2026-10-11 实测 = 32；Unity 6 该枚举到 52）
MAX_STREAM = 52


def build_stream_table(arena):
    """→ [(idx, go, useCustom, [streams])] —— 逐下标对齐（名字必须全等，否则报错退出）。"""
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
        psr = r['psr']
        use_custom = bool(psr.get('m_UseCustomVertexStreams'))
        raw = psr.get('m_VertexStreams') or []
        streams = [int(v) for v in raw]
        bad = [v for v in streams if v < 0 or v > MAX_STREAM]
        if bad:
            raise SystemExit(f'❌ {arena} `{r["name"]}`：m_VertexStreams 里有越界序数 {bad}'
                             f'（枚举只到 {MAX_STREAM}）—— 先查清楚再搬，别写坏 prefab')
        out.append((i, m['go'], use_custom, streams))
    return out


def inject(arena, dry=False):
    path = MANIFEST_DIR.format(arena=arena)
    raw = io.open(path, 'rb').read()
    n_crlf = raw.count(b'\r\n')
    n_lf = raw.count(b'\n')
    if n_crlf != n_lf:
        raise SystemExit(f'❌ {arena}：清单行尾不是纯 CRLF（CRLF {n_crlf} / LF {n_lf}）—— 先停下')
    s = raw.decode('utf-8')

    table = build_stream_table(arena)

    # 锚点 = 每颗粒子的 `"pivot"` 行（上一轮 `A1552` 已保证它逐颗都在）。
    pivot_idx = [m.start() for m in re.finditer(r'(?m)^      "pivot": \[[^\]]*\],\r\n', s)]
    if len(pivot_idx) != len(table):
        raise SystemExit(f'❌ {arena}：`"pivot"` 行 {len(pivot_idx)} 条 vs 粒子 {len(table)} 颗 —— '
                         '锚点不唯一/不齐。先跑 `工具/gen_arena_particlepivot.py`。')
    pivot_end = [m.end() for m in re.finditer(r'(?m)^      "pivot": \[[^\]]*\],\r\n', s)]

    # 幂等：先把已有的两个键整行删掉
    n_replaced = len(re.findall(r'(?m)^      "useCustomVertexStreams": .*\r\n', s))
    s = re.sub(r'(?m)^      "useCustomVertexStreams": .*\r\n', '', s)
    s = re.sub(r'(?m)^      "vertexStreams": \[[^\]]*\],\r\n', '', s)

    # 删完重新定位锚点
    anchor_end = [m.end() for m in re.finditer(r'(?m)^      "pivot": \[[^\]]*\],\r\n', s)]
    if len(anchor_end) != len(table):
        raise SystemExit(f'❌ {arena}：删掉旧键之后锚点变成 {len(anchor_end)} 条 —— 清单结构不对，停下')

    out = []
    last = 0
    n_custom = 0
    for pos, (i, go, use_custom, streams) in zip(anchor_end, table):
        js = '[' + ', '.join(str(v) for v in streams) + ']'
        line = (f'      "useCustomVertexStreams": {"true" if use_custom else "false"},\r\n'
                f'      "vertexStreams": {js},\r\n')
        out.append(s[last:pos])
        out.append(line)
        last = pos
        if use_custom:
            n_custom += 1
    out.append(s[last:])
    s2 = ''.join(out)

    # 校验：JSON 还能解出来，且每颗都有这两个键、值与原版逐颗相等
    chk = json.loads(s2)
    assert len(chk['particles']) == len(table), '粒子数变了'
    for (i, go, use_custom, streams), p in zip(table, chk['particles']):
        assert p.get('useCustomVertexStreams') is use_custom, f'idx {i} useCustomVertexStreams 没落对'
        assert [int(v) for v in p.get('vertexStreams', [])] == streams, f'idx {i} vertexStreams 没落对'

    print(f'=== {arena}：{len(table)} 颗 · 原版 `m_UseCustomVertexStreams=True` {n_custom} 颗'
          + (f' · 覆盖已有键 {n_replaced} 处' if n_replaced else ''))
    if dry:
        return n_custom, len(table)
    tmp = path + '.tmp'
    io.open(tmp, 'wb').write(s2.encode('utf-8'))
    os.replace(tmp, path)
    back = io.open(path, 'rb').read()
    assert back.count(b'\r\n') == back.count(b'\n'), '写回后行尾被翻了'
    print(f'    ✅ 写回 {path}（{len(back)} 字节，CRLF 保持）')
    return n_custom, len(table)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena', default=None)
    ap.add_argument('--dry', action='store_true')
    args = ap.parse_args()
    arenas = [args.arena] if args.arena else A.ARENAS
    tot_c = tot_n = 0
    for a in arenas:
        c, n = inject(a, dry=args.dry)
        tot_c += c
        tot_n += n
    print(f'=== 13 场合计：{tot_n} 颗 · `m_UseCustomVertexStreams=True` {tot_c} 颗')


if __name__ == '__main__':
    main()
