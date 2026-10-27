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
import json
import os
import sys

ASSETS = 'd:/4/Unity/MyGame/Assets'
OUT_DIR = os.path.join(ASSETS, 'WarpforgeVFX', 'Animations')
SRC = 'd:/2/新解包资源/assets_full'
SCRIPT_META = os.path.join(ASSETS, 'WarpforgeVFX', 'Runtime', 'AnimateUIMaterialProperty.cs.meta')

CLIPS = [
    ('Rank Up',        SRC + '/bundle_menus_assets_all/AnimationClip/AnimationClip_4418588262184220504.json'),
    ('Division Up',    SRC + '/bundle_menus_assets_all/AnimationClip/AnimationClip_6413171015603015678.json'),
    ('Level Up Card',  SRC + '/bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json'),
    # ⚠️ 这一条**工程里本来就有**（`BoosterPackAnim`/`BoosterPackOpenWindow` 引它但**没播**，见那两处注释）。
    #    它的 `script` 引用写的是 `guid: 0000…` ⇒ 真接线那天两条曲线会**绑到空气**（静默）。
    #    用同一份原版数据重写即可修掉；⛔ 它的 `.meta` guid 不动（重写的是内容）。
    ('Booster Window - Background Shake On Open',
     SRC + '/bundle_menus_assets_all/AnimationClip/AnimationClip_-177604310092043705.json'),
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
        '    m_StopTime: 1',            # 判据：原版也是 1，⛔ 别改
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


def main():
    args = sys.argv[1:]
    check = '--check' in args
    write = '--write' in args
    guid = script_guid()
    print('AnimateUIMaterialProperty.cs guid =', guid)
    ok = True
    for name, src in CLIPS:
        text, report = build(name, src, guid)
        dst = os.path.join(OUT_DIR, name + '.anim')
        print('=' * 8, name, '→', dst, '|', len(report), 'curves')
        for path, attr, cid, lands in report:
            print('   %-46s %-32s classID=%-4d %s' % (repr(path), attr, cid, lands))
        if check:
            have = io.open(dst, 'rb').read().decode('utf-8') if os.path.exists(dst) else None
            if have != text:
                ok = False
                print('   🔴 CHECK 失败：磁盘内容与生成内容不一致（缺失或过期）')
            else:
                print('   ✅ CHECK：逐字节一致')
            continue
        if write:
            io.open(dst, 'wb').write(text.encode('utf-8'))
            print('   WROTE')
    if check:
        sys.exit(0 if ok else 1)
    if not write:
        print('（dry-run，未写盘；要真写加 --write；复核加 --check）')


if __name__ == '__main__':
    main()
