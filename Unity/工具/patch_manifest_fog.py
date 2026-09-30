#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
patch_manifest_fog.py —— 一次性迁移：把「雾的形状参数」补进 13 份 manifest
================================================================================
为什么：
  `工具/scripts快照/gen_unity_arena_manifest.py` 原来**只读 `m_FogColor` / `m_FogDensity`**，
  没读 `m_FogMode` / `m_LinearFogStart` / `m_LinearFogEnd`。而原版这三项**逐场不同**、且
  `ScenarioEnvironmentConditionSO` 里**没有**它们（那个 SO 只有 fogColor/fogDensity，
  `…__ToggleFog.c` 全文只是 `RenderSettings.set_fog(fogDensity > 0)`）⇒ **形状只能来自场景**。
  判据全文 → `资料/普查产出_0930/§28逐场核_第一轮.md` §三。
  ⇒ 生成器源码**已补**（2026-09-30）；本脚本把**已有的 13 份 manifest** 补上同样三个字段，
     免得为这点改动重跑整个生成器（重跑会连带重写 Models/Textures）。

做法（**只插三行，不整篇重写** —— manifest 是 **CRLF**，且是生成器产物，不许被 python 文本模式翻行尾）：
  · 定位 `"ambient": {` 块（**注意 `defaultEnv` 里也有一个 `fogDensity`**，不能全局替换）
  · 在该块的 `"fogDensity": …` 行后插入 `fogMode` / `linearFogStart` / `linearFogEnd`
  · 值一律从原版 `07_场景/<场>/RenderSettings/RenderSettings_<pid>.json` 直读（去重：stem 形如 `RenderSettings_<数字>`）
幂等：已经有 `"fogMode"` 的场跳过。

用法：PYTHONIOENCODING=utf-8 python 工具/patch_manifest_fog.py [--check]
"""
import io, os, re, glob, json, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ORIG = 'd:/2/解包整理/07_场景'
ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']


def orig_fog(arena):
    cands = [f for f in glob.glob(os.path.join(ORIG, arena, 'RenderSettings', 'RenderSettings_*.json'))
             if re.match(r'^RenderSettings_\d+$', os.path.basename(f)[:-5])]
    if not cands:
        return None
    j = json.load(io.open(cands[0], encoding='utf-8'))
    return (int(j.get('m_FogMode', 3)),
            round(float(j.get('m_LinearFogStart', 0.0)), 6),
            round(float(j.get('m_LinearFogEnd', 300.0)), 6))


def main():
    check = '--check' in sys.argv
    n_done = n_skip = 0
    for a in ARENAS:
        p = os.path.join(ROOT, 'MyGame', 'Assets', 'WarpforgeArena1', 'arenas', a, '%s_manifest.json' % a)
        if not os.path.exists(p):
            print('  ⚠️ %s: manifest 不在' % a)
            continue
        raw = io.open(p, 'rb').read().decode('utf-8')
        crlf = raw.count('\r\n')
        if '"fogMode"' in raw:
            print('  ✅ %-28s 已有 fogMode，跳过' % a)
            n_skip += 1
            continue
        fog = orig_fog(a)
        if fog is None:
            print('  ⚠️ %s: 读不到原版 RenderSettings' % a)
            continue
        m = re.search(r'\n  "ambient": \{\r?\n(.*?)\r?\n  \},', raw, re.S)
        if not m:
            print('  🔴 %s: 找不到 ambient 块' % a)
            continue
        blk = m.group(1)
        # ⚠️ `(.*?)` 会把 fogDensity 行尾的 `\r\n` 吃掉（块尾就是这里）⇒ 行匹配**不能要求结尾换行**
        mm = re.search(r'(\n(\s+)"fogDensity": [^\r\n]*)', blk)
        if not mm:
            print('  🔴 %s: ambient 块里没有 fogDensity 行' % a)
            continue
        indent = mm.group(2)
        # ⚠️ 两端都要照顾：`fogDensity` 原文**没有尾逗号**（它是块里最后一项）⇒ ① 给它补一个逗号；
        #    ② 我插进去的**最后一项**（`linearFogEnd`）**也不能带逗号**，否则就是尾逗号。
        #    （第一版两头都错：先缺逗号、补上后又多了尾逗号。）
        ins = (',\r\n%s"fogMode": %d,\r\n%s"linearFogStart": %s,\r\n%s"linearFogEnd": %s'
               % (indent, fog[0], indent, fog[1], indent, fog[2]))
        start = m.start(1) + mm.end(1)          # 插在 fogDensity 行之后
        new = raw[:start] + ins + raw[start:]
        if check:
            print('  [check] %-28s 会插 3 行（fogMode=%d / %s–%s）' % (a, fog[0], fog[1], fog[2]))
            continue
        io.open(p, 'wb').write(new.encode('utf-8'))
        after = io.open(p, 'rb').read()
        ok_crlf = after.count(b'\r\n') == crlf + 3 and after.count(b'\n') == crlf + 3
        print('  ✍️  %-28s 插 3 行（fogMode=%d / %s–%s）· CRLF 校验 %s'
              % (a, fog[0], fog[1], fog[2], 'OK' if ok_crlf else '🔴 行尾被翻了！'))
        n_done += 1
    print('完成 %d · 跳过 %d' % (n_done, n_skip))


if __name__ == '__main__':
    main()
