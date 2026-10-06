# -*- coding: utf-8 -*-
"""W10 离线自证：把 C# 那条读链**逐句照抄**跑一遍（⛔ 不跑 Unity —— 本轮规矩）。

照抄的是三处（判据 = 本仓源码，改了那边这里也要跟着改）：
  · `CardPresentation/Battle/ScenarioBlendables.cs` 的 `BuildAnimFxModules`
    （`modules.count` / `GetF` / `GetS` / 前缀 `modules.<i>.` 剥离 / `*Unresolved` 跳过）
  · `WarpforgeVFX/Runtime/WFEffectModule.cs` 的 `WFModuleDef`（`CountList` / `GetString` / `GetFloat` / `GetInt`）
  · `WarpforgeVFX/Runtime/WFModuleScreenShake.cs` 的 `ReadList` + `Resolve` + `SplitRef`

输出：两条轨道各有几条、`preset` 解成什么、以及按 preset 表算出来的 6 个数值。
"""
import io
import json
import os
import sys

sys.stdout.reconfigure(encoding='utf-8', errors='replace')

FLAT = r'd:/4/Unity/MyGame/Assets/Resources/EnvBlendables.json'
PRESETS = r'd:/4/Unity/MyGame/Assets/Resources/WarpforgeVFX/shake_presets.json'


class Target(object):
    """`EnvBlendables.Target`（只搬我们要的两个口：`GetF` / `GetS`；键不在 ⇒ 默认值）。"""

    def __init__(self, d):
        self.leaf = d['leaf']
        self.fields = d.get('fields') or []

    def _find(self, k):
        for f in self.fields:
            if f.get('k') == k:
                return f
        return None

    def GetF(self, k, dflt=0.0):
        f = self._find(k)
        return float(f.get('f', 0.0)) if f is not None and 'f' in f else dflt

    def GetS(self, k, dflt=''):
        f = self._find(k)
        return (f.get('s') or '') if f is not None and 's' in f else dflt

    def Has(self, k):
        return self._find(k) is not None


class Def(object):
    """`WarpforgeVFX.WFModuleDef`（键值两条平行数组）。"""

    def __init__(self):
        self.keys, self.values = [], []

    def IndexOf(self, k):
        return self.keys.index(k) if k in self.keys else -1

    def GetString(self, k, dflt=''):
        i = self.IndexOf(k)
        return self.values[i] if i >= 0 else dflt

    def GetFloat(self, k, dflt=0.0):
        i = self.IndexOf(k)
        try:
            return float(self.values[i]) if i >= 0 else dflt
        except ValueError:
            return dflt

    def GetInt(self, k, dflt=0):
        i = self.IndexOf(k)
        # ⚠️ 照抄 C#：`int.TryParse` **不认** `"0.0"` ⇒ 回默认值（这正是「生成器必须让
        #    整数在回读时还是整数字面量」的那条判据；`TargetField.f` 是 float，
        #    `BuildAnimFxModules` 用 `ToString(InvariantCulture)` 写回字符串 —— 这里照抄那个口径）。
        if i < 0:
            return dflt
        v = self.values[i]
        try:
            iv = int(v)
            if str(iv) == v or (v.lstrip('-').isdigit()):
                return iv
        except ValueError:
            pass
        return dflt

    def HasPrefix(self, p):
        return any(k.startswith(p) for k in self.keys)

    def CountList(self, k):
        n = 0
        while self.HasPrefix('%s[%d]' % (k, n)):
            n += 1
        return n


def csharp_float_str(x):
    """`((float)f).ToString(InvariantCulture)` 的近似（0.0 → '0'，0.5 → '0.5'）。"""
    f = float(x)
    if f == int(f):
        return str(int(f))
    return repr(f)


def BuildAnimFxModules(t):
    """照抄 `ScenarioBlendables.BuildAnimFxModules` 的**数据那一半**（不建组件）。"""
    nf = t.GetF('modules.count', -1.0)
    if nf < 0:
        return [('<没有 modules.count>', None, ['（这一层没接上 —— 那里会出声）'])]
    out = []
    for i in range(int(nf)):
        kind = t.GetS('modules.%d' % i)
        if not kind:
            out.append(('<解不出类名>', None, []))
            continue
        pre = 'modules.%d.' % i
        d = Def()
        unresolved = []
        for f in t.fields:
            k = f.get('k') or ''
            if not k.startswith(pre):
                continue
            name = k[len(pre):]
            if not name:
                continue
            if name.endswith('Unresolved'):          # 🔴 标记键：不进 def，只出声
                unresolved.append(name)
                continue
            d.keys.append(name)
            # ⚠️ 与 C# 那句同口径：`s` 优先、否则 `f` 走不变文化的 float 字面量
            d.values.append(f['s'] if f.get('s') else csharp_float_str(f.get('f', 0.0)))
        out.append((kind, d, unresolved))
    return out


def ReadList(d, key):
    """照抄 `WFModuleScreenShake.ReadList`（含 `presetSO` 那条 `SplitRef`）。"""
    n = d.CountList(key)
    arr = []
    for i in range(n):
        p = '%s[%d]' % (key, i)
        e = dict(
            preset=d.GetString(p + '.presetSO'),
            delay=d.GetFloat(p + '.delay'),
            overwriteSustainTime=d.GetInt(p + '.overwriteSustainTime'),
            sustainTime=d.GetFloat(p + '.sustainTime'),
            overwriteAttackTime=d.GetInt(p + '.overwriteAttackTime'),
            attackTime=d.GetFloat(p + '.attackTime'),
            overwriteDecayTime=d.GetInt(p + '.overwriteDecayTime'),
            decayTime=d.GetFloat(p + '.decayTime'),
            overwriteAmplitude=d.GetInt(p + '.overwriteAmplitude'),
            amplitude=d.GetFloat(p + '.amplitude'),
            overwriteFrequency=d.GetInt(p + '.overwriteFrequency'),
            frequency=d.GetFloat(p + '.frequency'),
            overwriteDirection=d.GetInt(p + '.overwriteDirection'),
            direction=(d.GetFloat(p + '.direction.x'), d.GetFloat(p + '.direction.y'),
                       d.GetFloat(p + '.direction.z')),
        )
        # `WFModuleDef.SplitRef`：`@kind:type:rest`；`kind == "asset"` ⇒ 取 rest
        v = e['preset']
        if v.startswith('@'):
            a = v.split(':', 2)
            if len(a) == 3 and a[0][1:] == 'asset':
                e['preset'] = a[2]
        arr.append(e)
    return arr


def Resolve(e, presets):
    """照抄 `WFModuleScreenShake.Resolve`（preset 表 + 6 个覆盖开关，**非 0 才覆盖**）。"""
    p = presets.get(e['preset'])
    r = dict(
        preset=e['preset'],
        sustainTime=p['sustainTime'] if p else 0.1,
        attackTime=p['attackTime'] if p else 0.0,
        decayTime=p['decayTime'] if p else 0.3,
        amplitude=p['amplitude'] if p else 2.0,
        frequency=p['frequency'] if p else 0.05,
        direction=(p['dirX'], p['dirY'], p['dirZ']) if p else (0.0, 0.0, 0.0),
        delay=p['delay'] if p else 0.0,
    )
    if e['overwriteSustainTime'] != 0: r['sustainTime'] = e['sustainTime']
    if e['overwriteAttackTime'] != 0:  r['attackTime'] = e['attackTime']
    if e['overwriteDecayTime'] != 0:   r['decayTime'] = e['decayTime']
    if e['overwriteAmplitude'] != 0:   r['amplitude'] = e['amplitude']
    if e['overwriteFrequency'] != 0:   r['frequency'] = e['frequency']
    if e['overwriteDirection'] != 0:   r['direction'] = e['direction']
    if e['delay'] > 0:                 r['delay'] = e['delay']
    return r


def main():
    doc = json.load(io.open(FLAT, encoding='utf-8'))
    pdoc = json.load(io.open(PRESETS, encoding='utf-8'))
    presets = dict((p['name'], p) for p in pdoc['presets'])

    ok = True
    for grp in doc['sceneStandalone']:
        for it in grp['items']:
            t = Target(it['targets'][0])
            if t.GetF('modules.count', -1.0) <= 0:
                continue
            print('== %s / %s（本场 %s）' % (grp['root'], it['owner'], it['cls']))
            for kind, d, unresolved in BuildAnimFxModules(t):
                print('  模块 kind=%s · def 键 %d 条 · Unresolved %s' % (kind, len(d.keys) if d else -1, unresolved))
                if d is None:
                    continue
                for track in ('cameraShakes', 'manualTriggerCameraShakes'):
                    arr = ReadList(d, track)
                    print('    %-26s %d 条' % (track, len(arr)))
                    for e in arr:
                        r = Resolve(e, presets)
                        print('        preset=%r（表里%s） delay=%.3f sustain=%.3f attack=%.3f decay=%.3f '
                              'amp=%.3f freq=%.3f dir=(%.2f,%.2f,%.2f)'
                              % (e['preset'], '有' if e['preset'] in presets else '**没有**',
                                 r['delay'], r['sustainTime'], r['attackTime'], r['decayTime'],
                                 r['amplitude'], r['frequency'], r['direction'][0],
                                 r['direction'][1], r['direction'][2]))
                        if kind == 'AnimFXModuleScreenShake' and track == 'manualTriggerCameraShakes':
                            good = (e['preset'] == 'Shake Earthquake'
                                    and abs(r['sustainTime'] - 5.0) < 1e-3 and abs(r['attackTime'] - 0.5) < 1e-3
                                    and abs(r['decayTime'] - 1.0) < 1e-3 and abs(r['amplitude'] - 1.0) < 1e-3
                                    and abs(r['frequency'] - 0.1) < 1e-3
                                    and abs(r['direction'][1] - 1.0) < 1e-3
                                    and abs(r['direction'][2] - 1.0) < 1e-3)
                            print('       ⇒ 验收判据（`Shake Earthquake` = 5/0.5/1/1/0.1 · dir(0,1,1)）：%s'
                                  % ('✅ 过' if good else '❌ 不过'))
                            ok = ok and good
                print('    另一条轨道 cameraShakes 应为 0 条（原版 `cameraShakes = []`）')
    print('总判：%s' % ('✅ 全部对上' if ok else '❌ 有不过的'))
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
