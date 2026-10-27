#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_env_shake_sounds.py — 生成 `数据/游戏数据/env_shake_sounds.json`
（旁挂表：prefab 上的 **`ShakeAndSoundObject`** 组件 = 进场震屏 + 定时环境音）

判据（只读 `assets_full`，逐字段现读）：
  · 组件识别 = `m_Script.m_PathID == 8184164905518033017`（`ShakeAndSoundObject`），字段 = `onEnableShakes[]` + `sounds[]`
  · `sounds[i] = { time, sound(PPtr→AudioCue), is2d, repeat, loops, timeInterval }`
  · `onEnableShakes[i] = { presetSO(PPtr), delay, sustainTime, overwriteSustainTime,
                            amplitude, overwriteAmplitude, frequency, overwriteFrequency,
                            direction{x,y,z}, overwriteDirection, attackTime, overwriteAttackTime,
                            decayTime, overwriteDecayTime }`   ← **整份覆盖参数都在序列化里**，不用去解 SO
  · 归属 prefab 名 = 顺 `m_Component` 反查 GameObject（⛔ 别按 cue 名猜 prefab 名，至少 5 处对不上）
  · cue 名 = 映射表 `判据/音效cue到WindowSounds_映射.tsv` 的 `PathID → 原版cue名`（那张表就是从同一个 bundle 出来的）；
    表里没有的退回「文件名 `<cue 名>_<pid>.json`」；再没有 ⇒ `PID:<n>`（**不静默**：计入 `_stats.unresolvedCue`）

为什么另开一张表、⛔ 不塞 `env_blendables.json`：
  那张表的 schema 是 `IScenarioEnvironmentBlendeable` 的旁挂（现读 `ShakeAndSound` 出现 0 次），塞进去没人读 = 死数据。
  判据全文 → `判据/环境音17条_载体定位.md`。

用法：
    python 工具/gen_env_shake_sounds.py --check    # 只报告（默认就是 dry-run）
    python 工具/gen_env_shake_sounds.py --apply    # 真写盘
"""
import argparse, io, json, os, sys

sys.stdout.reconfigure(encoding='utf-8')

ASSETS = 'd:/2/新解包资源/assets_full'
BUNDLE = 'bundle_battleprefabs_vfxandmisc_assets_all'
CUE_BUNDLE = 'bundle_soundcollection_assets_all'
MAP_TSV = 'd:/4/Unity/资料/完全复刻/判据/音效cue到WindowSounds_映射.tsv'
OUT = 'd:/4/Unity/数据/游戏数据/env_shake_sounds.json'
OUT_RESOURCES = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/env_shake_sounds.json'
SCRIPT_PID = 8184164905518033017

ap = argparse.ArgumentParser(add_help=True)
ap.add_argument('--apply', action='store_true', help='真写盘（默认 dry-run）')
ap.add_argument('--check', action='store_true', help='只校验现有产物与新算的一致（不写）')
args = ap.parse_args()


def cue_name_index():
    """pid -> cue 名。① 映射表（权威）② 文件名 <名>_<pid>.json 兜底。"""
    idx = {}
    import csv
    if os.path.exists(MAP_TSV):
        with io.open(MAP_TSV, encoding='utf-8', newline='') as f:
            rows = list(csv.reader(f, delimiter='\t'))
        H = rows[0]
        I = {k: i for i, k in enumerate(H)}
        for r in rows[1:]:
            try:
                idx[int(r[I['PathID']])] = r[I['原版cue名']]
            except Exception:
                pass
    d = os.path.join(ASSETS, CUE_BUNDLE, 'MonoBehaviour')
    if os.path.isdir(d):
        for fn in os.listdir(d):
            if not fn.endswith('.json') or '_' not in fn:
                continue
            stem = fn[:-5]
            name, pid = stem.rsplit('_', 1)
            try:
                idx.setdefault(int(pid), name)
            except ValueError:
                pass
    return idx


def go_names():
    """mb pid -> GameObject 名（扫 m_Component 反查）。"""
    d = os.path.join(ASSETS, BUNDLE, 'GameObject')
    out = {}
    if not os.path.isdir(d):
        return out
    for fn in os.listdir(d):
        if not fn.endswith('.json'):
            continue
        p = os.path.join(d, fn)
        try:
            t = io.open(p, encoding='utf-8', errors='ignore').read()
        except Exception:
            continue
        try:
            j = json.loads(t)
        except Exception:
            continue
        for c in j.get('m_Component') or []:
            comp = c.get('component') if isinstance(c, dict) else None
            if isinstance(comp, dict) and comp.get('m_FileID') == 0:
                out[int(comp.get('m_PathID') or 0)] = fn[:-5]
    return out


def main():
    cues = cue_name_index()
    gos = go_names()
    mb_dir = os.path.join(ASSETS, BUNDLE, 'MonoBehaviour')
    prefabs = {}
    stats = {'components': 0, 'prefabs': 0, 'sounds': 0, 'shakes': 0,
             'unresolvedCue': 0, 'unresolvedOwner': 0, 'unresolvedPreset': 0}
    for fn in sorted(os.listdir(mb_dir)):
        if not fn.endswith('.json'):
            continue
        p = os.path.join(mb_dir, fn)
        try:
            d = json.load(io.open(p, encoding='utf-8'))
        except Exception:
            continue
        if (d.get('m_Script') or {}).get('m_PathID') != SCRIPT_PID:
            continue
        if 'sounds' not in d and 'onEnableShakes' not in d:
            continue
        stats['components'] += 1
        mb_pid = int(fn[len('MonoBehaviour_'):-len('.json')])
        go = gos.get(mb_pid)
        if not go:
            stats['unresolvedOwner'] += 1
            go = 'MB:%d' % mb_pid
        entry = prefabs.setdefault(go, [])
        comp = {'cls': 'ShakeAndSoundObject', 'componentFile': fn,
                'kind': ('environment' if ('nvironmental' in go or go.startswith('Ork Environmental')) else 'other'),
                'sounds': [], 'onEnableShakes': []}
        for s in d.get('sounds') or []:
            snd = (s or {}).get('sound') or {}
            pid = snd.get('m_PathID')
            name = cues.get(int(pid)) if pid is not None else None
            if name is None:
                stats['unresolvedCue'] += 1
                name = 'PID:%s' % pid
            comp['sounds'].append({
                'time': s.get('time'), 'sound': name, 'soundPid': pid,
                'is2d': bool(s.get('is2d', 1)), 'repeat': bool(s.get('repeat', 0)),
                'loops': s.get('loops', 0), 'timeInterval': s.get('timeInterval', 0.0)})
            stats['sounds'] += 1
        for k in d.get('onEnableShakes') or []:
            k = k or {}
            so = (k.get('presetSO') or {}).get('m_PathID')
            nm = cues.get(int(so)) if so is not None else None
            if nm is None:
                stats['unresolvedPreset'] += 1
                nm = 'PID:%s' % so
            comp['onEnableShakes'].append({
                'preset': nm, 'presetPid': so, 'delay': k.get('delay', 0.0),
                'sustainTime': k.get('sustainTime'), 'overwriteSustainTime': k.get('overwriteSustainTime', 0),
                'amplitude': k.get('amplitude'), 'overwriteAmplitude': k.get('overwriteAmplitude', 0),
                'frequency': k.get('frequency'), 'overwriteFrequency': k.get('overwriteFrequency', 0),
                'direction': k.get('direction'), 'overwriteDirection': k.get('overwriteDirection', 0),
                'attackTime': k.get('attackTime'), 'overwriteAttackTime': k.get('overwriteAttackTime', 0),
                'decayTime': k.get('decayTime'), 'overwriteDecayTime': k.get('overwriteDecayTime', 0)})
            stats['shakes'] += 1
        entry.append(comp)
    stats['prefabs'] = len(prefabs)
    # 🆕 2026-10-27：**再出一份扁平 `entries[]`**（运行时用 `JsonUtility` 读 —— 它不支持字典；
    #   与 `prefabs` 是**同一份数据的两个视图**，由同一生成器产出 ⇒ 不会漂）
    entries = []
    for name in sorted(prefabs):
        for c in prefabs[name]:
            entries.append({'prefab': name, 'cls': c['cls'], 'kind': c['kind'],
                            'sounds': c['sounds'], 'onEnableShakes': c['onEnableShakes'],
                            'componentFile': c['componentFile']})
    doc = {'_schema': 'env_shake_sounds/1',
           '_source': '%s/%s（ShakeAndSoundObject，m_Script pid %d）' % (ASSETS, BUNDLE, SCRIPT_PID),
           '_note': '旁挂表：prefab 上的进场震屏 + 定时环境音。⛔ 别并进 env_blendables.json（那是 blendable 专用表）。'
                    '判据 → 资料/完全复刻/判据/环境音17条_载体定位.md',
           '_stats': stats, 'prefabs': prefabs, 'entries': entries}
    text = json.dumps(doc, ensure_ascii=False, indent=1)

    env = [k for k, v in prefabs.items() if any(c['kind'] == 'environment' for c in v)]
    print('组件 = %d · prefab = %d（其中环境 = %d）· sounds = %d · shakes = %d'
          % (stats['components'], stats['prefabs'], len(env), stats['sounds'], stats['shakes']))
    print('未解析：cue %d · owner %d · preset %d'
          % (stats['unresolvedCue'], stats['unresolvedOwner'], stats['unresolvedPreset']))

    if args.check:
        old = None
        if os.path.exists(OUT):
            old = json.load(io.open(OUT, encoding='utf-8'))
        same = old is not None and json.dumps(old.get('prefabs'), sort_keys=True) == json.dumps(prefabs, sort_keys=True)
        if os.path.exists(OUT_RESOURCES):
            try:
                same = same and io.open(OUT_RESOURCES, encoding='utf-8').read().strip() == text.strip()
            except Exception:
                same = False
        else:
            same = False
        print('--check：', '一致' if same else '不一致（需要 --apply 重生）')
        return 0 if same else 1
    if not args.apply:
        print('（dry-run：加 --apply 才写盘）')
        return 0
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with io.open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text + '\n')
    # 🆕 2026-10-27：**同一份内容**再落一份到 `Resources/`（运行时 `Resources.Load<TextAsset>` 只认那里；
    #   两个文件由**同一个生成器**产出 ⇒ 单一来源，⛔ 别手改任何一份）
    with io.open(OUT_RESOURCES, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text + '\n')
    print('已写', OUT, len(text), 'bytes')
    print('已写', OUT_RESOURCES, len(text), 'bytes')
    return 0


sys.exit(main())
