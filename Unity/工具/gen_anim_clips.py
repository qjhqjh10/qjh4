# -*- coding: utf-8 -*-
"""生成 3 条 legacy `AnimationClip`（`.anim`）：`Rank Up` / `Division Up` / `Level Up Card`。

**为什么要有这个脚本**（J-01 的前置 J-06）：原版这 3 条 clip 只存在于 bundle 里，
`assets_full/…/AnimationClip/*.json` 有**完整曲线与事件**；而 bundle 里的 `AnimationClip`
**拷进工程是空壳**（`资料`/记忆里的「bundle 资产搬进工程的两条死路」）⇒ 只能**按数据重写**成 `.anim`。
⛔ 本脚本**不跑 Unity**、⛔ 不改任何 `.cs`。

判据（逐条）：
  · 源 = `d:/2/新解包资源/assets_full/{bundle}/AnimationClip/{file}.json`（`m_FloatCurves` / `m_Events` / `m_SampleRate`）；
  · 目标格式 = 工程里已有的 `Assets/WarpforgeVFX/Animations/Booster Window - Background Shake On Open.anim`
    （`m_Legacy: 1` · `m_Compressed: 0` · `m_UseHighQualityCurve: 1` · `m_SampleRate: 60` · `m_WrapMode: 0` ·
     **`m_StopTime: 1`**（原版也是 1，⛔ 别改）· `inWeight/outWeight: 0.33333334` · `m_PreInfinity/m_PostInfinity: 2`）。
  · 🔴 **`m_StopTime: 1` 不是 legacy clip 的运行时 length —— 2026-10-27 `B9_W37CLIP` 已证（⛔ 别再当缺陷去「修」）**：
    ① AssetRipper 导出的原版 74 条 clip 里，**66 条 `m_Legacy: 1` 全部是 `m_StopTime: 1.0`**（含曲线到 **7.05 s** 的
       `Godspear Warhead`），而 8 条 `m_Legacy: 0` 才是真值（0.8166 / 2.0 / 10.05 / 11.92 / 17.13 …）；
    ② `m_StopTime` 落在 **`UnityEngine.CoreModule.dll` 里 0 命中**、只在 `UnityEditor.CoreModule.dll` 里出现
       （`AnimationClipSettings` 7 处 / `stopTime` 1 处）⇒ 它是编辑器侧（Animation 窗口 / 导入器）的字段；
    ③ 原版**依赖**这些 >1 s 的事件才能跑：`RankedEventDivisionChangeWindow__AnimationFinished`（3.9167 s）·
       `CardDisplayLevelUpAnimationController__AnimationFinished`（3.4833 s）· `RemnantNecrons__AnimationResurrectionEnd`
       （1.8 s）· `SwarmVFXController__OnAnimationEndEvent`（1.2 s）—— 截断到 1 s 它们一次都不会响。
    ⇒ **legacy clip 的 length = 曲线最晚键**；`--check` 因此断的是「**事件时刻 ≤ 曲线跨度**」，⛔ 不是 `StopTime >= 事件`。
  · 输出目录（2026-10-27 `B9_W37CLIP` 改）：三条升级 clip 写进 **`Assets/CardPresentation/Resources/Animations/`**
    （运行期 `Resources.Load<AnimationClip>("Animations/<名字>")` 取得到；原来只写 `WarpforgeVFX/Animations/` = 全工程 0 引用、
    运行期取不到）；`Booster Window - …` 留在原处（它的用途是判据，由 `BoosterPackOpenWindow` 采样驱动）。
  · `.meta`：目标文件没有 `.meta` 时**顺带写一份**（guid = `md5("WarpforgeAnimClip/" + 名字)`，**确定性**）⇒ 重跑幂等；
    已有 `.meta` **不覆盖**（保住 guid）。
  · ⚠️ **`m_ScaleCurves` 有意不生成**：`Level Up Card` 的那条 path 是 `''`（= 挂在 clip 自己的根上）、值是 **250 → 288.98 → 250**
    （⚠️ 2026-10-27 现核：那个根是 **`CardUI`**（`MainMenu Warpforge.unity` tran `&1516`），**不是 `Upgrade Panel`** —— 后者只是控制器 `CardDisplayLevelUpAnimationController` 的宿主；`250` 是「**1 卡单位 = 250 画布 px**」的静止量纲、**不是放大倍数**）—— 照搬会把**我们的窗根**放大 250 倍。`Rank Up` / `Division Up` 的两条绑
    `Division Image` / `Rank Icon`（升级窗 `CardDetailPopup` 那棵树里没有这两个节点；段位窗 `RankedDivisionChangeWindow` 有，但那两条走 `division_clips.json`、不经本生成器）⇒ 天生绑不上。留待「原版节点 ↔ 我们节点」映射表，⛔ 别无脑加。
  · 🔴 **`floatProperty1/2` 那几条的 `script` 引用必须改成本工程那个组件**：
    原版是 `{m_FileID: 1, m_PathID: <原版 MonoScript>}` —— 那份脚本在 `Assembly-CSharp` 里，本工程解不到；
    照抄（或像 `Booster Window - …` 那样写 `guid: 0000…`）⇒ **曲线绑到空气、静默不生效**。
    这里统一写成 `{fileID: 11500000, guid: <AnimateUIMaterialProperty.cs.meta 的 guid>, type: 3}`。
  · 其余曲线（`m_Color.a` / `m_fontColor.a` / `m_Alpha` / `m_IsActive` / `ShapeModule.*` / `inherit*`）**原样照抄**
    （`classID` 用源里的值）：其中 `m_IsActive`(1) / `m_Alpha`(225 = CanvasGroup) 是 Unity 内建类、**有机会绑上**；
    走 uGUI `Image`/`Text` 的 114 曲线在本工程 **绑不上**（我们的节点是 `ImageQuad` / `Label`）—— 如实打印，⛔ 不假装。

用法：
    python -I -X utf8 d:/4/Unity/工具/gen_anim_clips.py            # dry-run（只打印，不写盘）
    python -I -X utf8 d:/4/Unity/工具/gen_anim_clips.py --write    # 真写
    python -I -X utf8 d:/4/Unity/工具/gen_anim_clips.py --check    # 只复核已写出的文件（重读 + 断言）

写盘之后 **Unity 要自己 import 一次**（生成 `.meta`）；命令见报告。
"""
import io
import hashlib
import json
import os
import sys

ASSETS = 'd:/4/Unity/MyGame/Assets'
OUT_VFX = os.path.join(ASSETS, 'WarpforgeVFX', 'Animations')
OUT_RES = os.path.join(ASSETS, 'CardPresentation', 'Resources', 'Animations')
SRC = 'd:/2/新解包资源/assets_full'
SCRIPT_META = os.path.join(ASSETS, 'WarpforgeVFX', 'Runtime', 'AnimateUIMaterialProperty.cs.meta')

CLIPS = [
    # 三条升级 clip 写进 `Resources/`：运行期取得到（`CardDetailPopup.PlayUpgradeAnimation` → `EnsureUpgradeAnimation`）。
    ('Rank Up',        SRC + '/bundle_menus_assets_all/AnimationClip/AnimationClip_4418588262184220504.json', OUT_RES),
    ('Division Up',    SRC + '/bundle_menus_assets_all/AnimationClip/AnimationClip_6413171015603015678.json', OUT_RES),
    ('Level Up Card',  SRC + '/bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json', OUT_RES),
    # ⚠️ 这一条**工程里本来就有**（`BoosterPackAnim`/`BoosterPackOpenWindow` 引它但**没播**，见那两处注释）。
    #    它的 `script` 引用写的是 `guid: 0000…` ⇒ 真接线那天两条曲线会**绑到空气**（静默）。
    #    用同一份原版数据重写即可修掉；⛔ 它的 `.meta` guid 不动（重写的是内容）。
    ('Booster Window - Background Shake On Open',
     SRC + '/bundle_menus_assets_all/AnimationClip/AnimationClip_-177604310092043705.json', OUT_VFX),
]

BOUND = ('floatProperty1.value', 'floatProperty2.value',
         'floatProperty2.vector4Value.x', 'floatProperty2.vector4Value.y',
         'floatProperty2.vector4Value.z', 'floatProperty2.vector4Value.w')


def f(x):
    """浮点输出（Unity 风格：够用就好、不要科学计数法）。"""
    s = '%.7g' % float(x)
    if s in ('-0', '-0.0'):
        s = '0'
    return s


def script_guid():
    with io.open(SCRIPT_META, encoding='utf-8') as fh:
        for line in fh:
            if line.startswith('guid:'):
                return line.split(':', 1)[1].strip()
    raise SystemExit('读不到 %s 的 guid' % SCRIPT_META)


def curve_yaml(c):
    keys = [k for k in (c.get('m_Curve') or []) if 'time' in k]
    assert keys, '空曲线'
    out = ['    curve:', '      serializedVersion: 2', '      m_Curve:']
    for k in keys:
        out += [
            '      - serializedVersion: 3',
            '        time: ' + f(k['time']),
            '        value: ' + f(k.get('value', 0)),
            '        inSlope: ' + f(k.get('inSlope', 0)),
            '        outSlope: ' + f(k.get('outSlope', 0)),
            '        tangentMode: %d' % int(k.get('tangentMode', 0)),
            '        weightedMode: %d' % int(k.get('weightedMode', 0)),
            '        inWeight: ' + f(k.get('inWeight', 0.33333334)),
            '        outWeight: ' + f(k.get('outWeight', 0.33333334)),
        ]
    out += ['      m_PreInfinity: 2', '      m_PostInfinity: 2', '      m_RotationOrder: 4']
    return out


def build(clip_name, src_path, guid):
    d = json.load(io.open(src_path, encoding='utf-8'))
    src_name = d.get('m_Name')
    assert src_name == clip_name, ('clip 名不符', src_name, clip_name)
    curves, report = [], []
    for b in d.get('m_FloatCurves', []):
        attr, path = b.get('attribute'), (b.get('path') or '')
        sc = b.get('script') or {}
        if attr in BOUND and int(b.get('classID', 114)) == 114:
            script = '{fileID: 11500000, guid: %s, type: 3}' % guid
            lands = 'floatProperty（本工程组件）⇒ 绑得上'
        elif int(b.get('classID', 114)) != 114:
            # ⚠️ 只有 `classID 114`(MonoBehaviour) 才带 `script` PPtr；其余是 Unity 原生类
            #   （1 = GameObject · 225 = CanvasGroup · 198 = ParticleSystem…），节点名对上就能绑。
            script = '{fileID: 0, guid: 00000000000000000000000000000000, type: 0}'
            lands = '原生类(classID %d) ⇒ 节点名对上就绑得上' % int(b.get('classID', 114))
        else:
            script = '{fileID: 0, guid: 00000000000000000000000000000000, type: 0}'
            lands = 'MonoBehaviour 类：源脚本引用是原版 MonoScript ⇒ 本工程绑不上（如实记）'
        report.append((path, attr, int(b.get('classID', 114)), lands))
        curves += ['  - serializedVersion: 2'] + curve_yaml(b.get('curve') or {}) + [
            '    attribute: ' + attr,
            '    path: ' + path,
            '    classID: %d' % int(b.get('classID', 114)),
            '    script: ' + script,
            '    flags: %d' % int(b.get('flags', 0)),
        ]

    events = d.get('m_Events', [])
    ev = []
    for e in events:
        ev += [
            '  - time: ' + f(e.get('time', 0)),
            '    functionName: ' + (e.get('functionName') or ''),
            '    data: ' + json.dumps(e.get('data') or '', ensure_ascii=False),
            '    objectReferenceParameter: {fileID: 0}',
            '    floatParameter: ' + f(e.get('floatParameter', 0)),
            '    intParameter: %d' % int(e.get('intParameter', 0)),
            '    messageOptions: %d' % int(e.get('messageOptions', 0)),
        ]

    lines = [
        '%YAML 1.1',
        '%TAG !u! tag:unity3d.com,2011:',
        '--- !u!74 &7400000',
        'AnimationClip:',
        '  m_ObjectHideFlags: 0',
        '  m_CorrespondingSourceObject: {fileID: 0}',
        '  m_PrefabInstance: {fileID: 0}',
        '  m_PrefabAsset: {fileID: 0}',
        '  m_Name: ' + clip_name,
        '  serializedVersion: 7',
        '  m_Legacy: 1',
        '  m_Compressed: 0',
        '  m_UseHighQualityCurve: 1',
        '  m_RotationCurves: []',
        '  m_CompressedRotationCurves: []',
        '  m_EulerCurves: []',
        '  m_PositionCurves: []',
        '  m_ScaleCurves: []',
        '  m_FloatCurves:',
    ] + curves + [
        '  m_PPtrCurves: []',
        '  m_SampleRate: ' + f(d.get('m_SampleRate', 60)),
        '  m_WrapMode: %d' % int(d.get('m_WrapMode', 0)),
        '  m_Bounds:',
        '    m_Center: {x: 0, y: 0, z: 0}',
        '    m_Extent: {x: 0, y: 0, z: 0}',
        '  m_ClipBindingConstant:',
        '    genericBindings: []',
        '    pptrCurveMapping: []',
        '  m_AnimationClipSettings:',
        '    serializedVersion: 2',
        '    m_AdditiveReferencePoseClip: {fileID: 0}',
        '    m_AdditiveReferencePoseTime: 0',
        '    m_StartTime: 0',
        '    m_StopTime: 1',            # 原版 66/66 条 legacy clip 都是 1；**不是 length**（length = 曲线最晚键），⛔ 别改
        '    m_OrientationOffsetY: 0',
        '    m_Level: 0',
        '    m_CycleOffset: 0',
        '    m_HasAdditiveReferencePose: 0',
        '    m_LoopTime: 0',
        '    m_LoopBlend: 0',
        '    m_LoopBlendOrientation: 0',
        '    m_LoopBlendPositionY: 0',
        '    m_LoopBlendPositionXZ: 0',
        '    m_KeepOriginalOrientation: 0',
        '    m_KeepOriginalPositionY: 1',
        '    m_KeepOriginalPositionXZ: 0',
        '    m_HeightFromFeet: 0',
        '    m_Mirror: 0',
        '  m_EditorCurves: []',
        '  m_EulerEditorCurves: []',
        '  m_HasGenericRootTransform: 0',
        '  m_HasMotionFloatCurves: 0',
        '  m_Events:',
    ] + (ev if ev else ['  []'])
    return '\n'.join(lines) + '\n', report


def max_curve_time(d):
    """legacy clip 的运行时 length 判据 = 所有曲线键里最晚的那个 `time`（⛔ 不是 `m_StopTime`，见文件头）。"""
    mt = 0.0
    for key in ('m_FloatCurves', 'm_ScaleCurves', 'm_PositionCurves',
                'm_RotationCurves', 'm_EulerCurves'):
        for c in (d.get(key) or []):
            for k in ((c.get('curve') or {}).get('m_Curve') or []):
                t = k.get('time')
                if t is not None and float(t) > mt:
                    mt = float(t)
    return mt


def max_event_time(d):
    return max([float(e.get('time', 0)) for e in (d.get('m_Events') or [])] or [0.0])


def meta_text(name):
    """`.meta` 的确定性 guid（重跑幂等；已有 `.meta` 不覆盖，保住 guid）。"""
    g = hashlib.md5(('WarpforgeAnimClip/' + name).encode('utf-8')).hexdigest()
    return ''.join([
        'fileFormatVersion: 2\n',
        'guid: %s\n' % g,
        'NativeFormatImporter:\n',
        '  externalObjects: {}\n',
        '  mainObjectFileID: 0\n',
        '  userData: \n',
        '  assetBundleName: \n',
        '  assetBundleVariant: \n',
    ])


def main():
    args = sys.argv[1:]
    check = '--check' in args
    write = '--write' in args
    guid = script_guid()
    print('AnimateUIMaterialProperty.cs guid =', guid)
    ok = True
    for name, src, out_dir in CLIPS:
        text, report = build(name, src, guid)
        dst = os.path.join(out_dir, name + '.anim')
        print('=' * 8, name, '→', dst, '|', len(report), 'curves')
        for path, attr, cid, lands in report:
            print('   %-46s %-32s classID=%-4d %s' % (repr(path), attr, cid, lands))
        d = json.load(io.open(src, encoding='utf-8'))
        mt, me = max_curve_time(d), max_event_time(d)
        print('   length(曲线最晚键) = %.7f s · 最晚事件 = %.7f s · m_StopTime = 1（原版同值，不是 length）'
              % (mt, me))
        if me > mt + 1e-5:
            ok = False
            print('   🔴 事件时刻 %.7f > 曲线跨度 %.7f ⇒ 播到片尾也**够不到**这条事件' % (me, mt))
        elif me > 0:
            print('   ✅ 事件都在曲线跨度之内（事件点 = 片尾）')
        if check:
            have = io.open(dst, 'rb').read().decode('utf-8') if os.path.exists(dst) else None
            if have != text:
                ok = False
                print('   🔴 CHECK 失败：磁盘内容与生成内容不一致（缺失或过期）')
            else:
                print('   ✅ CHECK：逐字节一致')
            continue
        if write:
            if not os.path.isdir(out_dir):
                os.makedirs(out_dir)
            data = text.encode('utf-8')          # ⚠️ 先算好再 write（`io.open(...,'wb')` 会先截断）
            io.open(dst, 'wb').write(data)
            print('   WROTE')
            meta = dst + '.meta'
            if not os.path.exists(meta):
                mdata = meta_text(name).encode('utf-8')
                io.open(meta, 'wb').write(mdata)
                print('   WROTE (meta, 新 guid)')
            else:
                print('   META 已存在，未覆盖（保住 guid）')
    if check:
        sys.exit(0 if ok else 1)
    if not write:
        print('（dry-run，未写盘；要真写加 --write；复核加 --check）')


if __name__ == '__main__':
    main()
