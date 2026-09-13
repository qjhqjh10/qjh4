# -*- coding: utf-8 -*-
"""把「手感补间」用到的每个参数**回到原始文件里读一遍**，和 `CardFeel.cs` 里的常量对照。

**为什么要有它**：2026-09-13 用户追问「这些确定都是原版解包资料里说明的参数吧」——
逐条回溯后答案是「**大部分是，但有十处不是**」（推导的 7 条 + 我们挑的 3 条）。
代码侧的做法是把出处登记成表（`CardFeel.Catalog` + 自检用反射核对），
这个脚本是**另一条独立的路**：直接去四个原始来源里把值读出来，人眼比对。

四个来源（`资料/规则引擎_进度与交接.md` 第二十五轮有说明）：
  ① 原版 AnimationClip —— `d:/2/新解包资源/assets_full/*/AnimationClip/*.json`
  ② UnitTweenSO —— `d:/4/Unity/数据/游戏数据/tween/*.json`（⚠️ 74 个里 17 个是**相机**抖动预设）
  ③ 卡预制体序列化字段 —— `d:/2/解包整理/08_预制体特效/战斗预制体/MonoBehaviour/MonoBehaviour_1744609728290659264.json`
  ④ 反编译常量 —— 用 `工具/read_literal.py` 从 `GameAssembly.dll` 读（本脚本顺带演示）

用法：
    PY=D:/2/Warpforge_tools/py312/python.exe
    $PY d:/4/Unity/工具/verify_feel_params.py
"""
import glob
import io
import json
import os
import re
import struct

TWEEN_DIR = r'd:/4/Unity/数据/游戏数据/tween'
CLIP_GLOB = r'd:/2/新解包资源/assets_full/*/AnimationClip/*.json'
PREFAB = (r'd:/2/解包整理/08_预制体特效/战斗预制体/MonoBehaviour/'
          r'MonoBehaviour_1744609728290659264.json')
DLL = r'd:/2/unity_run_ref/GameAssembly.dll'

# DOTween 的 Ease 枚举（原版字段里存的就是它的序号）
EASE = {0: 'Unset', 1: 'Linear', 2: 'InSine', 3: 'OutSine', 4: 'InOutSine', 5: 'InQuad',
        6: 'OutQuad', 7: 'InOutQuad', 8: 'InCubic', 9: 'OutCubic', 10: 'InOutCubic',
        23: 'InElastic', 30: 'OutBounce'}


def rd(p):
    return json.load(io.open(p, encoding='utf-8'))


def dump_tween(name):
    """① UnitTweenSO：打印每条子补间的关键字段（这就是 `CardFeel` 里那些常量的出处）"""
    d = rd(os.path.join(TWEEN_DIR, name + '.json'))
    refs = {r['rid']: r for r in d.get('references', {}).get('RefIds', [])}
    print('\n### %s' % name)
    for t in d.get('tweens', {}).get('list', []):
        o = refs[t['rid']]['data']
        cls = refs[t['rid']].get('type', {}).get('class')
        parts = []
        for k in ('duration', 'ease', 'punch', 'vibratto', 'vibrato', 'elasticity', 'strength',
                  'scale', 'relative', 'appendType', 'targetUnit', 'delay', 'loops'):
            if k not in o:
                continue
            v = o[k]
            if k == 'ease':
                v = '%s(%s)' % (EASE.get(v, '?'), v)
            if isinstance(v, dict) and set(v) >= {'x', 'y', 'z'}:
                v = '(%s)' % ', '.join('%g' % v[a] for a in 'xyz')
            parts.append('%s=%s' % (k, v))
        print('   %-14s %s' % (cls, '  '.join(parts)))


def clip(name):
    for f in glob.glob(CLIP_GLOB):
        d = rd(f)
        if d.get('m_Name') == name:
            return d
    return None


def dump_clip(name, pos_paths=(), scale_paths=(), float_attrs=()):
    """② AnimationClip：只打印关心的那几条曲线（免得刷屏）"""
    d = clip(name)
    if d is None:
        print('\n### %s  —— 没找到' % name)
        return
    print('\n### %s   (m_SampleRate=%s)' % (name, d['m_SampleRate']))
    for ev in d.get('m_Events', []):
        print('   EVENT t=%.4f  %s' % (ev['time'], ev['functionName']))
    for c in d.get('m_PositionCurves', []):
        if c.get('path') in pos_paths:
            print('   pos %-38r %s' % (c['path'],
                  [(round(k['time'], 4), round(k['value']['y'], 4)) for k in c['curve']['m_Curve']]))
    for c in d.get('m_ScaleCurves', []):
        if c.get('path') in scale_paths:
            print('   scl %-38r %s' % (c['path'],
                  [(round(k['time'], 4), round(k['value']['x'], 4)) for k in c['curve']['m_Curve']]))
    for c in d.get('m_FloatCurves', []):
        if c.get('attribute') in float_attrs:
            print('   flt %-38r %s' % (c['attribute'],
                  [(round(k['time'], 4), round(k['value'], 4)) for k in c['curve']['m_Curve']]))


def dump_prefab():
    """③ 卡预制体上的时序字段"""
    print('\n### 卡预制体 %s' % os.path.basename(PREFAB))
    p = rd(PREFAB)
    for k in ('timeToLand', 'timeBeforeLand', 'timeToChargeAttack', 'chargeAttackAngle',
              'chargeBackModifier', 'chargeUpModifier', 'attackStepTime',
              'attackPositionYOffset', 'attackRotationAngle', 'playerAttackMargin',
              'enemyAttackMargin', 'cardMovementSpeed'):
        if k in p:
            print('   %-26s = %s' % (k, p[k]))
    shake = p.get('meleeHitCameraShakePreset')
    if shake:
        print('   %-26s = amplitude %s' % ('meleeHitCameraShakePreset', shake.get('amplitude')))


def dump_dat(addrs):
    """④ 反编译常量（`_DAT_`）—— 读法见 `工具/read_literal.py` 的头注释"""
    print('\n### GameAssembly.dll 里的常量（⚠️ 读出来要**整齐**才算对）')
    h = open(DLL, 'rb')
    head = h.read(0x400)
    pe = struct.unpack_from('<I', head, 0x3C)[0]
    optsz = struct.unpack_from('<H', head, pe + 20)[0]
    base = struct.unpack_from('<Q', head, pe + 24 + 24)[0]
    secs, off = [], pe + 24 + optsz
    while off + 40 <= len(head):
        vsz, va, rsz, ptr = struct.unpack_from('<IIII', head, off + 8)   # VS 在前、VA 在后
        if ptr == 0 and va == 0:
            break
        secs.append((va, vsz, rsz, ptr))
        off += 40
    for a, note in addrs:
        rva = a - base
        for va, vsz, rsz, ptr in secs:
            if va <= rva < va + max(vsz, rsz):
                h.seek(ptr + (rva - va))
                print('   %-14s = %-12g  %s' % (hex(a), struct.unpack('<f', h.read(4))[0], note))
                break


if __name__ == '__main__':
    print('=' * 88)
    print('① UnitTweenSO（攻击 / 命中 / 召唤 / 消散）')
    print('=' * 88)
    for n in ('Recoil Normal Tween', 'Impact Light Tween', 'Impact Heavy Tween',
              'Summon Troop Tween', 'EC Heldrake Dissapear UP'):
        dump_tween(n)

    print()
    print('=' * 88)
    print('② AnimationClip（手牌→战场 / 伤害飘字）')
    print('=' * 88)
    dump_clip('Card Hand To Board',
              pos_paths=('Board Elements',),
              scale_paths=('2DCard/Front/CardImage',),
              float_attrs=('m_Alpha', 'material._DissolveAmount'))
    dump_clip('InBattleDamageCounter Variation 1',
              scale_paths=('', 'DamageIcon'),
              float_attrs=('m_fontColor.a', 'm_Color.a'))

    print()
    print('=' * 88)
    print('③ 卡预制体')
    print('=' * 88)
    dump_prefab()

    print()
    print('=' * 88)
    print('④ 反编译常量')
    print('=' * 88)
    dump_dat([(0x1834b3158, 'DoPushBack punch 时长'),
              (0x1834b2dc8, 'DoPushBack elasticity（vibrato 8 在调用里硬编码）'),
              (0x1834b2bbc, 'attackStepTime 的 ×2.0'),
              (0x1834b31c4, 'clockTimeLimit（EventAI 240；自检用，必须是 240）'),
              (0x1834b2df0, '换牌掉线的重试等待 4s')])
    print('\n⚠️ 对照 `CardPresentation/Core/CardFeel.cs` 的常量表；'
          '对不上的话以**这里读出来的**为准。')
