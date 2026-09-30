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


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena', default=None)
    ap.add_argument('--out', default='d:/4/_tmp_view/env52')
    ap.add_argument('--dry', action='store_true')
    a = ap.parse_args()
    arenas = [a.arena] if a.arena else list(ARENA_ARMY.keys())
    os.makedirs(a.out, exist_ok=True)
    n = 0
    for arena in arenas:
        states = load_states(arena)
        print('=== %s（%s）：%d 态' % (arena, ARENA_ARMY[arena], len(states)))
        for lab, so in states:
            tag = '%s__%s' % (arena, lab.replace(' ', '_').replace('/', '_'))
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
    return 0


if __name__ == '__main__':
    sys.exit(main())
