#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""抽出战场里**按画质档开关**的对象（`ObjectTogglerByQuality`）→ `arenas/<场>/<场>_qualitytoggle.json`。

**为什么需要它**：原版有一批对象是**运行时按 `QualitySettings.GetQualityLevel()` 二选一**的，
而清单里它们**全是 `active=True`** ⇒ 我们会**两个都建**。
实测受害：`battlearenaaeldari` 的 `Portal High quality` 与 `Portal Low Quality`
（**同名同位置** (108.4, 7.7, 22.8)，都是 `Circle Halo` 粒子）**都被建出来了** ⇒ 门的效果翻倍。

**判据**（本地反编译 `d:/2/tools/decomp_full/ObjectTogglerByQuality__OnEnable.c`，逐字）：
```
q = QualitySettings.GetQualityLevel()
cond = conditionIsHigherOrEqual ? (toggleBellowIncluding <= q) : (q <= toggleBellowIncluding)
gameObject.SetActive(cond ? stateIfConditionMet : !stateIfConditionMet)
```
字段偏移：`0x20` = `toggleBellowIncluding` · `0x24` = `conditionIsHigherOrEqual` · `0x25` = `stateIfConditionMet`。
原版实跑档 = **2**（注册表 `HKCU\\Software\\Everguild\\Warpforge` 的 `UnityGraphicsQuality_h1669003810`）
⇒ 这里把**每一档的结果都算出来**存进 json，建场时按我们用的档取（不自作主张）。

用法：
    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_quality_toggle.py           # 全 13 场
    ... --check     只报不写
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
    print('用法：\n    D:/2/Warpforge_tools/py312/python.exe 工具/gen_arena_quality_toggle.py           # 全 13 场\n    ... --check     只报不写\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
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
MONO_DIRS = ['bundle_Waprforge_monoscripts/MonoScript']
QUALITY = 2


## 🆕 2026-10-27（B9_W45ARENA）`--check` 的**产物校验**（只读，不写盘）。
#  原来这一族的 `--check` 是「只扫不写」（`if check: continue`）—— 盘上缺产物也恒 rc0，
#  属「看着有 check、其实啥都没查」。口径照 `工具/import_original_art.py:1714-1721`：
#  把**重算出来的内容**与盘上那份**逐字节比**；缺 = 缺产物 · 不一致 = 过期，末尾点名 + rc=1。
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

    # 全局 MonoScript pathID → 类名
    import glob
    mono = {}
    for p in glob.glob('d:/2/新解包资源/assets_full/*/MonoScript/*.json'):
        try:
            d = json.load(io.open(p, encoding='utf-8'))
        except Exception:
            continue
        if d.get('m_ClassName'):
            mono[os.path.basename(p)[len('MonoScript_'):-5]] = d['m_ClassName']

    total = 0
    lack, stale = [], []
    for arena in arenas:
        path = os.path.join(aa, 'scenes_scenes_%s.bundle' % arena)
        if not os.path.isfile(path):
            continue
        env = UnityPy.load(path)
        names = {}
        toggles = []
        for o in env.objects:
            try:
                if o.type.name == 'GameObject':
                    d = o.read_typetree()
                    names[o.path_id] = d.get('m_Name', '')
                elif o.type.name == 'MonoBehaviour':
                    d = o.read_typetree()
                    cls = mono.get(str((d.get('m_Script') or {}).get('m_PathID')), '')
                    if cls == 'ObjectTogglerByQuality':
                        toggles.append(d)
            except Exception:
                continue
        if not toggles:
            # `--check`：原版这一场**没有**按画质档开关的对象 ⇒ 不该有产物；
            # 盘上还留着就是**多余**（`ArenaBuilder` 会照旧表把对象关掉）。
            if check:
                p = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena,
                                 arena + '_qualitytoggle.json')
                if os.path.isfile(p):
                    stale.append((p, '原版这一场没有 ObjectTogglerByQuality ⇒ 产物多余'))
            continue
        out = []
        for d in toggles:
            gid = (d.get('m_GameObject') or {}).get('m_PathID')
            base = d.get('toggleBellowIncluding', 2)
            ge   = bool(d.get('conditionIsHigherOrEqual', 0))
            met  = bool(d.get('stateIfConditionMet', 1))
            active = {q: (met if ((base <= q) if ge else (q <= base)) else (not met)) for q in range(6)}
            out.append({'go': names.get(gid, '?'), 'toggleBellowIncluding': base,
                        'conditionIsHigherOrEqual': ge, 'stateIfConditionMet': met,
                        'activeByQuality': {str(k): v for k, v in active.items()}})
            print('[%s] %-24s base=%s ge=%s met=%s → 各档 %s' % (
                arena, names.get(gid, '?'), base, ge, met, {k: int(v) for k, v in active.items()}))
        total += len(out)
        dst = os.path.join(ROOT, 'MyGame/Assets/WarpforgeArena1/arenas', arena, arena + '_qualitytoggle.json')
        # ⚠️ 只落**定论**：`{go, active}`（按我们用的档算好了）—— 判定规则**只留在本工具一处**，
        #    建场侧不再抄一遍公式（「两处写同一条规则 = 迟早不一致」）。
        flat = [{'go': o['go'], 'active': o['activeByQuality'][str(QUALITY)]} for o in out]
        text = json.dumps(
            {'scene': arena, 'quality': QUALITY, 'objects': flat}, ensure_ascii=False, indent=2)
        if check:
            wf_check_artifact(dst, text, lack, stale)
            continue
        io.open(dst, 'w', encoding='utf-8', newline='\n').write(text)
    print('合计：%d 个按画质开关的对象' % total)
    return wf_report_artifacts(check, lack, stale, 'gen_arena_quality_toggle.py')


if __name__ == '__main__':
    sys.exit(main())
