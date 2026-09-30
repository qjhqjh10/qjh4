#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""sweep_env_states.py —— **逐态渲染 13 场 × 4 环境 = 52 个状态**（【4 环境】的验收尺 / 闸门②）

为什么要它（判据 → `项目任务.md` §三 第 30 条 · `资料/战场场景线_交接.md` §二 第 5 件）：
  本线的交付物是「**13 个场景各自做对 × 每场 4 种环境**（1 默认 + 3 张进攻卡）= **52 个状态**」。
  单看一场说不了话 —— 得**逐态**渲出来、并且**逐态**有一行数，才能判「这一态动没动、动得对不对」。

它做什么：对每一场，先渲**默认态**，再对**该阵营的每一条效果环境**渲一态（走 `WF_ENV="<SO 名>"`，
即 `ArenaBuilder.ApplyEnvProbeIfAny` → **真机同一个执行器** `CardPresentation.Battle.EnvironmentApplier`）。
⚠️ **必须带 `WF_PSFIXSEED=1`**（不开时同一份构建两次能差 6%，表就失去意义）。
⚠️ 同工程**同时只能跑一个 Unity 实例** ⇒ 本脚本**串行**，别并行跑。
⚠️ **态数是 55 不是 52**：交付物按「13 × 4（1 默认 + 3 张进攻卡）」= 52 算，而**有 3 个阵营有 4 条效果 SO**
（`BlackLegion` 的 `Void battle`/`Warp Storm` 同 GUID 共用一个 prefab · `Ultramarines` 的 `Fleet Support`
≡ `Aerial Clash`（同图异名）· `EmperorsChildren` 有个废件）⇒ 13 默认 + 42 效果 = **55 态**。
这 3 条**原版自己就这样**（判据 → `项目任务.md` §三 第 30 条 散件 A），**照实渲、别去重**。

用法：
    PYTHONIOENCODING=utf-8 D:/2/Warpforge_tools/py312/python.exe 工具/sweep_env_states.py
    ... --arena battlearenadarkangels        # 只跑一场
    ... --dry                                # 只列表不渲染
"""
import argparse
import io
import json
import os
import shutil
import subprocess
import sys

ROOT = 'd:/4/Unity'
UNITY = 'D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe'
PROJ = 'D:\\4\\Unity\\MyGame'
LOG = 'd:/4/_tmp_view'
ENV_JSON = os.path.join(ROOT, 'MyGame/Assets/Resources/EnvironmentConditions.json')

# 场名 → 阵营（判据：13 场各自的原版阵营；`battlearena<阵营>` 的命名）
ARENA_ARMY = {
    'battlearena1': 'Ultramarines', 'battlearena2': 'Goff', 'battlearena3': 'Sautekh',
    'battlearenaaeldari': 'SaimHann', 'battlearenaastramilitarum': 'AstraMilitarum',
    'battlearenablacklegion': 'BlackLegion', 'battlearenadarkangels': 'DarkAngels',
    'battlearenaemperorschildren': 'EmperorsChildren', 'battlearenagenestealers': 'Genestealers',
    'battlearenaleviathan': 'Leviathan', 'battlearenasororitas': 'Sororitas',
    'battlearenaspacewolves': 'SpaceWolves', 'battlearenatauviorla': 'TauEmpire',
}


def load_states(arena):
    """→ [(标签, SO 名或 None)]：第一个是默认态（不改环境）。"""
    d = json.load(io.open(ENV_JSON, encoding='utf-8'))
    army = ARENA_ARMY[arena]
    eff = [it for it in d['items'] if it.get('army') == army and it.get('kind') == 'effect']
    out = [('默认', None)]
    for it in eff:
        # 标签 = SO 名去掉 `Environmental Condition(s)?` 前缀那一段（够读就行）
        lab = it['so']
        for pre in ('EnvironmentalCondition ', 'Environmental Condition '):
            if lab.startswith(pre):
                lab = lab[len(pre):]
                break
        out.append((lab, it['so']))
    return out


def render(arena, so):
    env = dict(os.environ)
    env.pop('ELECTRON_RUN_AS_NODE', None)
    env['WF_ARENA'] = arena
    env['WF_PSFIXSEED'] = '1'
    if so:
        env['WF_ENV'] = so
    else:
        env.pop('WF_ENV', None)
    log = os.path.join(LOG, 'envsweep.log')
    r = subprocess.run([UNITY, '-batchmode', '-quit', '-projectPath', PROJ,
                        '-executeMethod', 'ArenaBuilder.RenderPreviewFromCLI', '-logFile', log],
                       env=env, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return r.returncode


def tag_of(arena, lab):
    """态 → 文件名（**只此一处**，出图与出表共用）。"""
    return '%s__%s' % (arena, lab.replace(' ', '_').replace('/', '_'))


def table(outdir):
    """🆕 **逐态对比表**（闸门② 真正要看的那张）：每个效果态 vs **本场默认态**。
    判「这一态动没动」——**能验的只有「参数到位 + 有变化 + 不炸 + 互不相同」，
    ⚠️ 不能验「像不像原版」**（这 55 态没有原版参考图）。
    指标算法复用 `arena_imgstats.stats`（**判据只留一处**）。
    🔴 **2026-09-30 晚更正：判「动没动」不能只看整图均值** —— 实测 `EC 1 Green` 的
      **64.97% 像素都变了**（最大差 196），而整图 mean 只挪了 **+0.04**（明暗互相抵消）。
      ⇒ 判据改成 **逐像素差占比（>2 的像素 / 全图）**，另把 Δmean 作为参考列一起打出来。"""
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from PIL import Image, ImageChops
    import numpy as np
    import arena_imgstats as S
    print('\n=== 逐态表（每态 vs 本场默认态；判据 = `arena_imgstats.stats` + 逐像素差）===')
    print('%-32s %-30s %8s %8s %9s %7s %7s  %s'
          % ('arena', 'state', 'mean', 'Δmean', '变化像素%', 'dark%', 'hi%', '动没动'))
    moved = 0
    total = 0
    for arena in ARENA_ARMY:
        base = None
        base_img = None
        for lab, so in load_states(arena):
            p = os.path.join(outdir, tag_of(arena, lab) + '.png')
            if not os.path.exists(p):
                continue
            img = Image.open(p).convert('RGB')
            st = S.stats(img)
            if lab == '默认':
                base = st
                base_img = img
                print('%-32s %-30s %8.2f %8s %9s %7.2f %7.2f  %s'
                      % (arena, lab, st['mean'], '—', '—', st['dark'], st['hi'], '(基准)'))
                continue
            if base is None:
                continue
            d = st['mean'] - base['mean']
            arr = np.asarray(ImageChops.difference(base_img, img)).max(axis=2)
            chg = 100.0 * float((arr > 2).mean())
            # 「动没动」：**逐像素差占比 ≥ 0.5%** 才算动 —— 均值会被明暗抵消（见 docstring）；
            #   噪声底：同一构建内开 `WF_PSFIXSEED=1` 可复现，这个阈值远高于它。
            flag = '动' if chg >= 0.5 else '**没动**'
            moved += (1 if flag == '动' else 0)
            total += 1
            print('%-32s %-30s %8.2f %+8.2f %8.2f%% %7.2f %7.2f  %s'
                  % (arena, lab, st['mean'], d, chg, st['dark'], st['hi'], flag))
    print('—— %d/%d 态相对本场默认态**有变化**（没动的那些要逐个看：SO 没有 prefab 的、'
          '或本来就只补间雾/环境光的那几条，**是预期**）' % (moved, total))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena', default=None)
    ap.add_argument('--out', default='d:/4/_tmp_view/env52')
    ap.add_argument('--dry', action='store_true')
    ap.add_argument('--table', action='store_true', help='只按已出的图算表（不渲染）')
    a = ap.parse_args()
    if a.table:
        table(a.out)
        return 0
    arenas = [a.arena] if a.arena else list(ARENA_ARMY.keys())
    os.makedirs(a.out, exist_ok=True)
    n = 0
    for arena in arenas:
        states = load_states(arena)
        print('=== %s（%s）：%d 态' % (arena, ARENA_ARMY[arena], len(states)))
        for lab, so in states:
            tag = tag_of(arena, lab)
            print('   %-44s ← %s' % (lab, so or '(默认，不设 WF_ENV)'))
            if a.dry:
                continue
            rc = render(arena, so)
            src = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena,
                               'preview_%s.png' % arena)
            dst = os.path.join(a.out, tag + '.png')
            if os.path.exists(src):
                shutil.copy(src, dst)
            print('      rc=%s → %s' % (rc, os.path.basename(dst)))
            n += 1
    print('共 %d 态（图在 %s）' % (n, a.out))
    if n > 0:
        table(a.out)
    return 0


if __name__ == '__main__':
    sys.exit(main())
