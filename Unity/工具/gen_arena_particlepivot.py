#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_arena_particlepivot.py — 把原版每颗粒子的 **`ParticleSystemRenderer.m_Pivot`**
补进我们 13 场的 `<场>_manifest.json` 的 `particles[i].pivot`。

**为什么**（2026-10-11 · 账号 A1552）：
  `battlearena1` 里 `Barrels` 旁那团火（`TinyFlames`）我们渲出来**又小又暗**、
  逐像素比原版弱一个数量级（`R-B>140` 的像素：原版 539 / 我们 16）。
  逐字段对完发现**粒子系统本身的参数全对**（45 个字段 × 34 颗，0 处真差异）、
  贴图逐像素相同、材质 49 项属性全同 —— 唯独 **`m_Pivot` 我们从来没抽过、也没消费过**。

**原版值**（`battlearena1`，6/34 非零，全部是「贴地 / 贴面」的效果）：
  `TinyFlames (-0.1, 0.48, 0)` · `WildFire (0, 0.4, 0)` · `RisingSteam (0, 0.5, 0)` ·
  `SmokeEffect ×3 (0, 0, -0.28)`；其余 28 颗是 `(0,0,0)`（= Unity 默认，不吃亏）。

**为什么它重要**：`renderAlignment`（默认 View）下公告板以**粒子位置为中心**画。
  `TinyFlames` 的位置在 `y = 0.0098`（**正好在地面上**）⇒ 不套 pivot 时**下半张永远在地板以下**，
  被不透明地板挡住（`ZWrite=0` 但 `ZTest LEqual`），**只剩上半截、而且是最淡的那半截**。
  原版 `pivot.y = +0.48` 把整张抬上去 ⇒ 火苗**站**在地面上。
  ⇒ 症状（暗一半 · 又小 · 偏蓝）与「下半张被地板吃掉」逐条吻合（偏蓝 = 火苗没了、露出偏冷的地板）。

🔴 **判据来源**：原版 prefab 的 `ParticleSystemRenderer.m_Pivot`（
  `d:/2/解包整理/07_场景/<场>/`，按 PathID 解外链）。**不是猜的**，本脚本逐颗读出来。

⚠️ **上游生成器（`d:/2/Warpforge_tools/scripts/gen_unity_arena_manifest.py`）还没教这个字段** ——
  它不在本仓白名单内。**重跑生成器会把这个键冲掉** ⇒ 冲掉了就再跑一次本脚本。
  （根治 = 在生成器的 `particles[]` 里补 `pivot`，与本项目 `工具/gen_arena_*.py` 那套「旁挂数据」同路。）

**幂等**：已经有 `"pivot"` 的粒子**原地替换**，没有的插在 `"duration"` 之前。
**保格式**：逐字节只动要动的那一行，**行尾照旧 CRLF**。

用法：
  D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_arena_particlepivot.py
  D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_arena_particlepivot.py --arena battlearena1
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


def compact(v):
    """3.0 -> 3.0 · 0.0 -> 0.0（与清单里其它 float 的写法一致）。"""
    f = float(v)
    if f == 0.0:
        return '0.0'
    return repr(round(f, 6))


def build_pivot_list(arena):
    """→ [(idx, go, (x,y,z))] —— 逐下标对齐（名字必须全等，否则报错退出）。"""
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
        pv = r['psr'].get('m_Pivot') or {}
        out.append((i, m['go'], (pv.get('x', 0.0), pv.get('y', 0.0), pv.get('z', 0.0))))
    return out


def inject(arena, dry=False):
    path = MANIFEST_DIR.format(arena=arena)
    raw = io.open(path, 'rb').read()
    n_crlf = raw.count(b'\r\n')
    n_lf = raw.count(b'\n')
    if n_crlf != n_lf:
        raise SystemExit(f'❌ {arena}：清单行尾不是纯 CRLF（CRLF {n_crlf} / LF {n_lf}）—— 先停下')
    s = raw.decode('utf-8')

    table = build_pivot_list(arena)
    # 粒子块里唯一属于粒子的键：`"duration"`（meshes[] 没有）。
    dur_idx = [m.start() for m in re.finditer(r'(?m)^      "duration": ', s)]
    if len(dur_idx) != len(table):
        raise SystemExit(f'❌ {arena}：`"duration"` 出现 {len(dur_idx)} 次 vs 粒子 {len(table)} 颗 —— 锚点不唯一/不齐')

    # 已经有 pivot 的：先删掉（幂等）
    n_replaced = len(re.findall(r'(?m)^      "pivot": \[[^\]]*\],\r\n', s))
    s = re.sub(r'(?m)^      "pivot": \[[^\]]*\],\r\n', '', s)
    dur_idx = [m.start() for m in re.finditer(r'(?m)^      "duration": ', s)]

    out = []
    last = 0
    for pos, (i, go, (x, y, z)) in zip(dur_idx, table):
        line = f'      "pivot": [{compact(x)}, {compact(y)}, {compact(z)}],\r\n'
        out.append(s[last:pos])
        out.append(line)
        last = pos
    out.append(s[last:])
    s2 = ''.join(out)

    # 校验：JSON 还能解出来，且每颗都有 pivot
    chk = json.loads(s2)
    assert len(chk['particles']) == len(table), '粒子数变了'
    for i, p in enumerate(chk['particles']):
        assert 'pivot' in p and len(p['pivot']) == 3, f'idx {i} 没拿到 pivot'

    nz = [(i, g, v) for i, g, v in table if any(abs(c) > 1e-6 for c in v)]
    print(f'=== {arena}：{len(table)} 颗 · 非零 pivot {len(nz)} 颗'
          + (f' · 覆盖已有键 {n_replaced} 处' if n_replaced else ''))
    for i, g, v in nz:
        print(f'    idx {i:2d} {g:24s} pivot=({compact(v[0])}, {compact(v[1])}, {compact(v[2])})')
    if dry:
        return
    tmp = path + '.tmp'
    io.open(tmp, 'wb').write(s2.encode('utf-8'))
    os.replace(tmp, path)
    back = io.open(path, 'rb').read()
    assert back.count(b'\r\n') == back.count(b'\n'), '写回后行尾被翻了'
    print(f'    ✅ 写回 {path}（{len(back)} 字节，CRLF 保持）')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena', default=None)
    ap.add_argument('--dry', action='store_true')
    args = ap.parse_args()
    arenas = [args.arena] if args.arena else A.ARENAS
    for a in arenas:
        inject(a, dry=args.dry)


if __name__ == '__main__':
    main()
