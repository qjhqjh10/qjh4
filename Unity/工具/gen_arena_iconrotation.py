#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""抽出战场里挂 **`IconRotation`** 的对象（「背景环自转」）→ `arenas/<场>/<场>_iconrotation.json`。

**为什么需要它**：原版 `Battle Arena Black Legion` 里有两个背景环是**每帧自转**的
（`Background_Ring_1` / `Background_Ring_2.001`），而我们的 `ArenaBuilder` 建出来的是**静态 transform**
⇒ **两环不转**（缺一条可观察行为）。

**判据**（本地反编译 `d:/2/tools/decomp_full/`，逐字）：
```
IconRotation__Start.c  : *(float*)(this+0x30) = 0;                       # 累加器清零
IconRotation__Update.c : accum(+0x30) += Time.deltaTime;
                         if (period(+0x2c) <= accum) { accum = 0; transform.Rotate(x(+0x20), y(+0x24), z(+0x28)); }
```
序列化字段名 = `xSpeed` / `ySpeed` / `zSpeed` / `updateRate`（对应 +0x20/+0x24/+0x28/+0x2c）。
🔴 两个实例的 `updateRate` 都是 **0** ⇒ 判据恒真 ⇒ **每帧都转**（帧率相关，原版就这样，⛔ 别改成按秒）。

现读结果（2026-10-29，`assets_full` 全区按脚本 pathID 反查）：

    battlearenablacklegion : Background_Ring_1  (—, -0.02, —, rate 0)
                             Background_Ring_2.001 (—, +0.01, —, rate 0)

其余 12 场 **0 个实例**（原版就没有）。

用法：
    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_iconrotation.py           # 全 13 场
    ... --check     只报不写（缺产物 / 产物过期 ⇒ rc=1）
"""
import io
import json
import os
import sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：\n    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_iconrotation.py           # 全 13 场\n    ... --check     只报不写\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

ROOT = "d:/4/Unity"
AA_DIRS = [
    'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64',
    'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64',
]
ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']
SCRIPT_CLASS = 'IconRotation'


## `--check` 的产物校验（只读，不写盘）。口径照 `工具/gen_arena_quality_toggle.py`。
def wf_check_artifact(dst, text, lack, stale):
    if not os.path.isfile(dst):
        lack.append(dst)
        return
    try:
        old = io.open(dst, encoding='utf-8').read()
    except Exception as e:
        stale.append((dst, '读不出：%s' % e))
        return
    if old == text:
        return
    try:                     # 只差缩进/键序 ⇒ 语义一致，**不算过期**（`JsonUtility` 本来就不看键序）
        if json.loads(old) == json.loads(text):
            return
    except Exception:
        pass
    stale.append((dst, '内容与重算结果不同'))


def wf_report_artifacts(check, lack, stale, script):
    """`--check` 收尾：有缺/过期就点名并让 main 返回 1。→ 该返回的 rc（无问题 = 0）。"""
    if not check or not (lack or stale):
        return 0
    print('')
    for p in lack:
        print('  🔴 缺产物：%s' % p)
    for p, why in stale:
        print('  🔴 产物过期（%s）：%s' % (why, p))
    print('  ⇒ 重跑一次 `python 工具/%s` 生成（`--check` 只读、不写盘）。' % script)
    return 1


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    check = '--check' in sys.argv
    arenas = args or ARENAS
    try:
        import UnityPy
    except Exception as e:
        print('UnityPy 不可用:', e)
        return 1
    aa = next((d for d in AA_DIRS if os.path.isdir(d)), None)
    if aa is None:
        print('找不到 aa 目录（两处都不在）')
        return 1

    # 脚本 pathID → 类名（只认 `IconRotation`；其余忽略）
    import glob
    target_pids = set()
    for p in glob.glob('d:/2/新解包资源/assets_full/*/MonoScript/*.json'):
        try:
            d = json.load(io.open(p, encoding='utf-8'))
        except Exception:
            continue
        if d.get('m_ClassName') == SCRIPT_CLASS:
            target_pids.add(os.path.basename(p)[len('MonoScript_'):-5])
    if not target_pids:
        print('⚠️ 解包目录里找不到 `%s` 的 MonoScript（脚本类名检索不到）' % SCRIPT_CLASS)
        return 1
    print('MonoScript 命中：%s' % ', '.join(sorted(target_pids)))

    total = 0
    lack, stale = [], []
    for arena in arenas:
        path = os.path.join(aa, 'scenes_scenes_%s.bundle' % arena)
        if not os.path.isfile(path):
            continue
        env = UnityPy.load(path)
        names = {}
        instances = []
        for o in env.objects:
            try:
                if o.type.name == 'GameObject':
                    d = o.read_typetree()
                    names[o.path_id] = d.get('m_Name', '')
                elif o.type.name == 'MonoBehaviour':
                    d = o.read_typetree()
                    pid = str((d.get('m_Script') or {}).get('m_PathID'))
                    if pid in target_pids:
                        instances.append(d)
            except Exception:
                continue
        if not instances:
            # `--check`：原版这一场**没有** IconRotation ⇒ 不该有产物；盘上还留着就是多余。
            if check:
                p = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena,
                                 arena + '_iconrotation.json')
                if os.path.isfile(p):
                    stale.append((p, '原版这一场没有 IconRotation ⇒ 产物多余'))
            continue

        out = []
        for d in instances:
            gid = (d.get('m_GameObject') or {}).get('m_PathID')
            name = names.get(gid, '?')
            ent = {
                'go': name,
                'x': float(d.get('xSpeed', 0.0)),
                'y': float(d.get('ySpeed', 0.0)),
                'z': float(d.get('zSpeed', 0.0)),
                'rate': float(d.get('updateRate', 0.0)),
            }
            out.append(ent)
            print('[%s] %-24s rot=(%g, %g, %g) rate=%g%s' % (
                arena, name, ent['x'], ent['y'], ent['z'], ent['rate'],
                '  ← rate 0 ⇒ 每帧都转' if ent['rate'] == 0.0 else ''))
        total += len(out)

        text = json.dumps({'scene': arena, 'objects': out}, ensure_ascii=False, indent=2)
        dst = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, arena + '_iconrotation.json')
        if check:
            wf_check_artifact(dst, text, lack, stale)
            continue
        io.open(dst, 'w', encoding='utf-8', newline='\n').write(text)

    print('合计：%d 个 IconRotation 实例' % total)
    return wf_report_artifacts(check, lack, stale, 'gen_arena_iconrotation.py')


if __name__ == '__main__':
    sys.exit(main())
