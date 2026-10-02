#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""menu_dump.py — 一把尺子：把解包里**一整个界面子树**摊成「层 × 参数」表（`项目任务.md` §三 第 10 条 10·3 的「第 0 层」）。

为什么要有它（2026-09-27，多人界面那一批）：
    `menu_rect.py` 只给**矩形**，而 10·3 第 0 层要的格子是
    **类名** / **sprite 名** / rect / 锚点 / pivot / **字号** / **颜色** / **activeSelf**。
    少一样就得回头补，补的时候又各写各的（两处写同一条规则 = 迟早不一致）。
    ⇒ **矩形算法**只此一处（`import menu_rect`，不抄第二份），其余在这里补齐。

🔴 本脚本解决三件「不查就一定会错」的事：
    ① **被 `LayoutGroup` 排的子节点** —— 导出 JSON 里是「布局跑之前的模板位」
       （`Rewards` 左栏四键四个完全重合就是它）。本脚本**按 uGUI 算法算成布局后的位**（`--no-layout` 关）。
    ② **MonoBehaviour 的类名** —— 导出 JSON 里 `m_Script` 是**跨文件 PPtr**，而
       `VerticalLayoutGroup` 与 `HorizontalLayoutGroup` 的序列化字段**完全相同** ⇒ 光看字段**判不出主轴**。
       出路 = `assets_full/bundle_Waprforge_monoscripts/MonoScript/*.json`：**`m_ClassName` 是明文**，
       文件名 = PathID ⇒ 由 `m_Script.m_PathID` 直接拿类名。
    ③ **sprite 名与九宫格** —— Image 里只有 PathID，名字要回图集/切片索引对齐查。

用法：
    python menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 8
    python menu_dump.py bundle_menus_assets_all --rt -8094654694055052750 --depth 6 --relative --md
    python menu_dump.py --verify-layout        # 自检：拿正本 §2·1 已手算的 Tab Buttons 核布局算法

    --rt <pid>     直接从某个 RectTransform 往下走（重名、或手上只有 RT pid 时用）
    --relative     坐标相对**根节点左上角**（量窗口内部版面时用这个）
    --md           吐 Markdown 表（贴进正本的格式）
    --no-layout    不算布局组（只看模板位）
    --no-sprite    不查 sprite 名 / 九宫格（快；查名要扫 4700+ 个 json，首次十几秒）
    --active-only  只列 active（**缺省也列 inactive 并打 F** —— activeSelf 的出现条件是 10·3 要的一格）

出处：
    · 矩形算法 / 父链爬升 = `工具/menu_rect.py`（import；**别在本文件里再写一份**）
    · 类名 = `assets_full/bundle_Waprforge_monoscripts/MonoScript/*.json` 的 `m_ClassName`
    · 布局算法 = uGUI `HorizontalOrVerticalLayoutGroup` 的 `CalcAlongAxis` /
      `SetChildrenAlongAxis` / `SetChildAlongAxisWithScale`（逐条照抄；自检见 `--verify-layout`）
    · sprite 名 = ① **真包**（`sprite_pid_map()`，UnityPy 扫 `bundle_*.bundle` 的 `o.path_id`）
      —— **唯一能解跨文件引用的那条**（`m_Sprite.m_FileID ≠ 0` 时 sprite 在别的包里）；
      ② 解包目录的切片缓存（`ui_extract/*/Sprite/*.json` 的 `pathid`）补剩下的。
    · sprite 九宫格 / 原尺寸 = `d:/4/Unity/素材/Warpforge原版/UI图集/图集/*/Sprite/*.json`
      的 `m_Border` / `m_Rect`（缺的退到 `assets_full/bundle_*/Sprite/<名>.json`）

⚠️ **尺寸那一列是按【名字】查的，不是按 pid** —— 反查链是 `pid → 名字 →(按名字)→ 尺寸/九宫格`。
   所以**同名的另一张图**会被当成它（例：UGUI 内置的 `Background` / `UIMask`）。
   ⇒ **名字可信，尺寸/九宫格遇到通用名（`Background`/`Image`/`Mask` 这类）要留个心眼**；
   真要钉死得按 pid 去真包里读那张 Sprite 本体。
"""
import argparse
import io
import json
import os
import sys
import glob

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import menu_rect as MR          # noqa: E402  —— 矩形算法只此一处

try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

BUNDLES = 'd:/2/新解包资源/assets_full'
SPRITE_IDX_CACHE = 'd:/4/_tmp_view/_menu_sprite_idx.json'
MONO_REL = ['bundle_Waprforge_monoscripts/MonoScript', 'globalgamemanagers/MonoScript']
EXTRACT = 'd:/2/Warpforge_tools/data/ui_extract'
ATLAS_DIR = 'd:/4/Unity/素材/Warpforge原版/UI图集/图集'
# 🔴 **真包目录**（`UnityPy` 扫它建 `Sprite pid → 名字`）—— 这条路能解**跨文件引用**的 sprite，
#    是纯读解包目录**做不到**的那一半（见 `sprite_pid_map()` 的说明）。
#    ⚠️ 这是**本工具从 `menu_dump.py` 的上一版（2026-09-23 建）继承下来的唯一一条独有能力** ——
#    2026-09-27 我重写这个文件时**把它弄丢了**（当场发现、当场接回来）。
BUNDLE_DIR = os.environ.get(
    'WF_BUNDLE_DIR', r'D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64')
REAL_SPRITE_CACHE = 'd:/4/_tmp_view/sprite_pids_ALL.json'
SKIP_KEYS = ('m_GameObject', 'm_Enabled', 'm_Script', 'm_Name', 'm_Material', 'm_Color',
             'm_RaycastTarget', 'm_RaycastPadding', 'm_Maskable', 'm_OnCullStateChanged')

# TMP 的 `HorizontalAlignmentOptions`/`VerticalAlignmentOptions` 是**位标志**，不是 0 基枚举：
#   `Left=1 · Center=2 · Right=4 · Justified=8 · Flush=16 · Geometry=32`；
#   垂直 `Top=256(0x100) · Middle=512(0x200) · Bottom=1024(0x400) · Baseline=2048 · Midline=4096 · Capline=8192`。
# 🔴 **上一版在这里错过**：按 0 基枚举猜 ⇒ 把 **Center 印成 Right、Left 印成 Center、Right 印成 Flush**，
#    2026-09-24 波及过四扇窗的对齐结论。判据 `d:/2/tools/il2cpp_out/dump.cs:861389-861394`。
H_ALIGN = {1: 'Left', 2: 'Center', 4: 'Right', 8: 'Justified', 16: 'Flush', 32: 'Geometry'}
V_ALIGN = {256: 'Top', 512: 'Middle', 1024: 'Bottom', 2048: 'Baseline', 4096: 'Midline', 8192: 'Capline'}
IMG_TYPE = {0: 'Simple', 1: 'Sliced', 2: 'Tiled', 3: 'Filled'}


def sprite_pid_map(quiet=True):
    """**从原始 .bundle** 建 `Sprite 的 PathID → 名字`（缓存到 `_tmp_view/sprite_pids_ALL.json`）。

    🔴 **为什么非要有它**：Image 组件的 `m_Sprite` 带 `m_FileID` —— **非 0 表示 sprite 在另一个包**
    （图集包，例：`atlasindividual_assets_0_mainmenu`）。那些 pid 是**别的 bundle 的局部 pid**，
    纯读解包目录（按名字存文件、不带 pid）**根本反查不到**，只能读真包的 `o.path_id`。
    实测：`bundle_menus_assets_all` 自己只有 22 张 sprite，一个奖励窗引用的十几张分散在别的包。
    ⚠️ 缓存是**全局 pid→名字**（不区分包）⇒ 同名不同包撞号时**会静默取错**；命中冲突由调用方报。
    """
    if os.path.exists(REAL_SPRITE_CACHE):
        try:
            return json.load(io.open(REAL_SPRITE_CACHE, encoding='utf-8'))
        except Exception:
            pass
    try:
        import UnityPy
    except ImportError:
        if not quiet:
            sys.stderr.write('⚠️ 没有 UnityPy ⇒ 跨文件 sprite 名这次解不出（只走解包目录那条索引）\n')
        return {}
    if not os.path.isdir(BUNDLE_DIR):
        if not quiet:
            sys.stderr.write('⚠️ 找不到真包目录 %s ⇒ 同上\n' % BUNDLE_DIR)
        return {}
    out = {}
    files = sorted(f for f in os.listdir(BUNDLE_DIR) if f.endswith('.bundle'))
    for i, fn in enumerate(files):
        try:
            env = UnityPy.load(os.path.join(BUNDLE_DIR, fn))
            for o in env.objects:
                if o.type.name != 'Sprite':
                    continue
                try:
                    d = o.read()
                except Exception:
                    continue
                nm = getattr(d, 'm_Name', None)
                if nm:
                    out[str(o.path_id)] = nm
        except Exception as e:
            if not quiet:
                sys.stderr.write('  · %s 读不了（%s）\n' % (fn, e))
        if not quiet and (i + 1) % 20 == 0:
            sys.stderr.write('  … 扫到 %d/%d 个包\n' % (i + 1, len(files)))
    try:
        os.makedirs(os.path.dirname(REAL_SPRITE_CACHE), exist_ok=True)
        io.open(REAL_SPRITE_CACHE, 'w', encoding='utf-8').write(json.dumps(out, ensure_ascii=False))
    except Exception:
        pass
    if not quiet:
        sys.stderr.write('（扫 %d 个真包，读出 %d 个 Sprite 的 pid→名字）\n' % (len(files), len(out)))
    return out


# ================================================================ 索引
def mono_index(verbose=True):
    """MonoScript 的 PathID → 类名。**拿类名的唯一可靠路子**（字段指纹分不出 V/H LayoutGroup）。"""
    idx = {}
    for rel in MONO_REL:
        for f in glob.glob(os.path.join(BUNDLES, rel, '*.json')):
            pid = os.path.basename(f).split('_', 1)[-1][:-len('.json')]
            try:
                d = json.load(io.open(f, encoding='utf-8'))
            except Exception:
                continue
            cn = d.get('m_ClassName') or d.get('m_Name')
            if cn:
                idx[pid] = cn
    if verbose:
        print(f'# MonoScript 类名索引：{len(idx)} 条')
    return idx


def fingerprint(k):
    """类名没解出时按字段兜底（UnityEngine.UI 那几个不在这份 monoscripts 里也说不准）。"""
    if {'m_Padding'} <= k and ({'m_Spacing'} & k or {'m_ChildAlignment'} & k):
        return 'LayoutGroup(?)'
    if {'m_Content', 'm_Viewport'} <= k:
        return 'ScrollRect'
    if 'm_ShowMaskGraphic' in k:
        return 'Mask'
    if 'm_text' in k:
        return 'TextMeshProUGUI'
    if 'm_FontData' in k:
        return 'Text'
    if 'm_IsOn' in k:
        return 'Toggle(?)'
    if 'm_OnClick' in k:
        return 'Button'
    if 'm_Sprite' in k and 'm_FillCenter' in k:
        return 'Image'
    return None


def sprite_index(verbose=True):
    """Sprite 的 PathID → 名字。**两级来源，真包优先**：

    ① **真包**（`sprite_pid_map()`，UnityPy 扫 `bundle_*.bundle` 的 `o.path_id`）—— **唯一能解跨文件引用的那条**；
    ② **解包目录的切片缓存**（`ui_extract/*/Sprite/*.json` 里有 `pathid`）—— 补真包没覆盖到的。

    ⚠️ **PathID 是分包局部的** —— 同号在不同包里可能是两张图。①② 都说得出名字但**不一致**时记进
    `conflicts`（表里打 `?`），**别照抄**。
    """
    by_pid, conflicts = {}, {}
    real = sprite_pid_map()
    for pid, nm in real.items():
        by_pid[str(pid)] = nm
    if os.path.exists(SPRITE_IDX_CACHE):
        try:
            d = json.load(io.open(SPRITE_IDX_CACHE, encoding='utf-8'))
            for pid, nm in d.get('by_pid', {}).items():
                if pid not in by_pid:
                    by_pid[pid] = nm
                elif by_pid[pid] != nm:
                    conflicts[pid] = sorted({by_pid[pid], nm})
            if verbose:
                print(f'# sprite 索引：真包 {len(real)} 条 + 切片缓存补齐 ⇒ 共 {len(by_pid)} 条'
                      + (f'（**真包与缓存打架 {len(conflicts)} 条**）' if conflicts else ''))
            return {'by_pid': by_pid, 'ambiguous': conflicts}
        except Exception:
            pass
    n = 0
    for f in glob.glob(os.path.join(EXTRACT, '*', 'Sprite', '*.json')):
        try:
            d = json.load(io.open(f, encoding='utf-8'))
        except Exception:
            continue
        pid, nm = d.get('pathid'), d.get('m_Name')
        if pid is None or not nm:
            continue
        n += 1
        k = str(pid)
        if k in by_pid and by_pid[k] != nm:
            conflicts[k] = sorted({by_pid[k], nm})
        else:
            by_pid.setdefault(k, nm)
    out = {'by_pid': by_pid, 'ambiguous': conflicts}
    try:
        os.makedirs(os.path.dirname(SPRITE_IDX_CACHE), exist_ok=True)
        io.open(SPRITE_IDX_CACHE, 'w', encoding='utf-8').write(json.dumps(out, ensure_ascii=False))
    except Exception:
        pass
    if verbose:
        print(f'# sprite 索引：真包 {len(real)} 条 + 切片缓存扫 {n} 张 ⇒ 共 {len(by_pid)} 条'
              + (f'（打架 {len(conflicts)} 条）' if conflicts else ''))
    return out


def border_index(verbose=True):
    """sprite 名 → (宽, 高, border(x,y,z,w), pixelsToUnits, offset)。九宫格判据在这里。"""
    idx = {}

    def add(d):
        nm = d.get('m_Name')
        if not nm or nm in idx:
            return
        r, b, o = d.get('m_Rect', {}), d.get('m_Border', {}), d.get('m_Offset', {})
        idx[nm] = (r.get('width', 0), r.get('height', 0),
                   (b.get('x', 0), b.get('y', 0), b.get('z', 0), b.get('w', 0)),
                   d.get('m_PixelsToUnits', 100.0), (o.get('x', 0), o.get('y', 0)))

    for f in glob.glob(os.path.join(ATLAS_DIR, '*', 'Sprite', '*.json')):
        try:
            add(json.load(io.open(f, encoding='utf-8')))
        except Exception:
            continue
    for f in glob.glob(os.path.join(BUNDLES, 'bundle_*', 'Sprite', '*.json')):
        try:
            add(json.load(io.open(f, encoding='utf-8')))
        except Exception:
            continue
    if verbose:
        print(f'# sprite 尺寸/九宫格索引：{len(idx)} 条')
    return idx


# ================================================================ 组件
def components_of(b, mono, rt):
    """一个节点上全部 MonoBehaviour → [(pathid, 类名, 原始 JSON)]。"""
    gopid = rt.get('m_GameObject', {}).get('m_PathID')
    g = b.go.get(str(gopid)) or {}
    out = []
    for c in g.get('m_Component', []):
        cp = str(c['component']['m_PathID'])
        mb = MR.load(b.path, 'MonoBehaviour', cp)
        if not mb:
            continue                       # RectTransform / CanvasRenderer 等不是 MonoBehaviour
        sp = str(mb.get('m_Script', {}).get('m_PathID', ''))
        out.append((cp, mono.get(sp) or fingerprint(set(mb.keys())) or 'Unknown', mb))
    return out


def _c(c):
    return '' if not isinstance(c, dict) else \
        f'({c.get("r", 1):.3g},{c.get("g", 1):.3g},{c.get("b", 1):.3g},{c.get("a", 1):.3g})'


def _p(pptr):
    return str(pptr.get('m_PathID', 0)) if isinstance(pptr, dict) else '0'


def _pn(pptr, sidx):
    """PPtr → **切片名**（拿不到就空串）。

    为什么要它（2026-10-03，A17）：`m_SpriteState` 里那四张图**只存 pid**，
    而「哪颗按钮的高亮图是哪张」正是 A17 要接的东西 ⇒ 必须解成名字才算判据。
    ⚠️ 与 `sprite` 列同一张索引（`sidx['by_pid']`）—— **同一个警告也适用**：
       那是「全局 pid→名字」的反查，**跨包同名会静默取错**（撞了就打个 `?`）。"""
    pid = _p(pptr)
    if pid == '0' or sidx is None:
        return ''
    nm = sidx['by_pid'].get(pid)
    if nm is None:
        return f'<未解出 {pid}>'
    if pid in sidx.get('ambiguous', {}):
        nm = f'?{nm}(冲)'
    return nm


def _pad(d):
    return '?' if not isinstance(d, dict) else \
        (f'{d.get("m_Left", 0):g},{d.get("m_Right", 0):g},{d.get("m_Top", 0):g},{d.get("m_Bottom", 0):g}')


def describe(cls, mb, sidx, bidx):
    """把一个组件摊成四列：sprite / 颜色贴图模式 / 文字 / 其它参数。"""
    k = set(mb.keys())
    sprite = tint = text = extra = ''
    if 'm_Sprite' in mb:
        mt = IMG_TYPE.get(mb.get('m_Type'), mb.get('m_Type'))
        pid = _p(mb.get('m_Sprite'))
        if pid == '0':
            sprite = '<无图>'
        else:
            nm = sidx['by_pid'].get(pid, f'<未解出 {pid}>')
            if pid in sidx.get('ambiguous', {}):
                nm = f'?{nm}(冲)'
            bb = bidx.get(nm)
            if bb:
                w, h, bd, ppu, off = bb
                nine = '否' if bd == (0, 0, 0, 0) else f'九宫{bd[0]:g},{bd[1]:g},{bd[2]:g},{bd[3]:g}'
                sprite = (f'{nm} {w:g}×{h:g} {nine}'
                          + (f' ppu={ppu:g}' if ppu != 100 else '')
                          + (f' off={off[0]:g},{off[1]:g}' if off != (0, 0) else ''))
            else:
                sprite = f'{nm} <无尺寸记录>'
        tint = f'{mt} {_c(mb.get("m_Color"))}'
        if mb.get('m_PreserveAspect'):
            tint += ' preserveAspect'
        if not mb.get('m_FillCenter', 1):
            tint += ' fillCenter=0'
        if mb.get('m_PixelsPerUnitMultiplier', 1) not in (1, 1.0):
            tint += f' ppuMul={mb["m_PixelsPerUnitMultiplier"]}'
        if mb.get('m_Enabled', 1) == 0:
            tint += ' **m_Enabled=0**'
    if 'm_text' in mb:
        s = f'{mb.get("m_text")!r} 字号={mb.get("m_fontSize")}'
        if mb.get('m_enableAutoSizing'):
            s += f' auto[{mb.get("m_fontSizeMin")}~{mb.get("m_fontSizeMax")}]'
        ha, va = mb.get('m_HorizontalAlignment'), mb.get('m_VerticalAlignment')
        s += (f' 对齐={H_ALIGN.get(ha, "raw=%s" % ha)}/{V_ALIGN.get(va, "raw=%s" % va)}'
              f' 折行={mb.get("m_TextWrappingMode")} 色={_c(mb.get("m_fontColor"))}')
        if mb.get('m_characterSpacing'):
            s += f' 字距={mb["m_characterSpacing"]:g}'
        text = s
    elif 'm_FontData' in mb:
        text = f'{mb.get("m_Text")!r} 色={_c(mb.get("m_Color"))}'
    if 'm_Spacing' in k and 'm_Padding' in k:
        if 'm_CellSize' in k:
            # 🔴 `GridLayoutGroup` **也有** `m_Spacing`/`m_Padding`/`m_ChildAlignment`
            #    ⇒ 不先分流的话会被上面那条泛型分支吃掉、**`cellSize` 根本不印**
            #    （2026-09-27 踩过：标题页的 `Item Drawer` 只印出 spacing/pad，看不到 325.9×130）。
            pad = mb.get('m_Padding', {})
            cs = mb.get('m_CellSize', {})
            extra = (f'**【GridLayoutGroup】** cellSize={cs.get("x"):g}×{cs.get("y"):g} '
                     f'spacing={mb.get("m_Spacing")} pad={_pad(pad)} '
                     f'corner/axis={mb.get("m_StartCorner")}/{mb.get("m_StartAxis")} '
                     f'align={mb.get("m_ChildAlignment")} '
                     f'constraint={mb.get("m_Constraint")}({mb.get("m_ConstraintCount")})')
        else:
            extra = (f'spacing={mb.get("m_Spacing")} align={mb.get("m_ChildAlignment")} '
                     f'pad={_pad(mb.get("m_Padding"))} ctrlW={mb.get("m_ChildControlWidth")} '
                     f'ctrlH={mb.get("m_ChildControlHeight")} expandW={mb.get("m_ChildForceExpandWidth")} '
                     f'expandH={mb.get("m_ChildForceExpandHeight")} scaleW={mb.get("m_ChildScaleWidth")} '
                     f'scaleH={mb.get("m_ChildScaleHeight")}')
    elif 'm_CellSize' in k:
        extra = (f'cell={mb.get("m_CellSize")} spacing={mb.get("m_Spacing")} '
                 f'corner/axis={mb.get("m_StartCorner")}/{mb.get("m_StartAxis")} '
                 f'align={mb.get("m_ChildAlignment")} constraint={mb.get("m_Constraint")}'
                 f'({mb.get("m_ConstraintCount")})')
    elif 'm_ShowMaskGraphic' in k:
        extra = f'showGraphic={mb.get("m_ShowMaskGraphic")}'
    elif 'm_Content' in k and 'm_Viewport' in k:
        extra = (f'h={mb.get("m_Horizontal")} v={mb.get("m_Vertical")} '
                 f'mode={mb.get("m_MovementType")} inertia={mb.get("m_Inertia")} '
                 f'elasticity={mb.get("m_Elasticity")} decel={mb.get("m_DecelerationRate")}')
    elif 'm_IsOn' in k:
        extra = f'isOn={mb.get("m_IsOn")}'
        if 'onSprite' in k:
            extra += (f' onSprite={_p(mb.get("onSprite"))} offSprite={_p(mb.get("offSprite"))}'
                      f' onColor={_c(mb.get("onColor"))} offColor={_c(mb.get("offColor"))}')
    elif 'm_OnClick' in k:
        extra = (f'trans={mb.get("m_Transition")} target={_p(mb.get("m_TargetGraphic"))} '
                 f'interactable={mb.get("m_Interactable")}')
        # 🆕 2026-10-03（A17）：**把 `m_SpriteState` 印出来** —— 原来只印 `trans=`，
        #   而 `trans==2 (SpriteSwap)` 的按钮**悬停换的是图**（不是变暗），高亮图就存在这里。
        #   原版 1276 个按钮里 630 颗是 SpriteSwap；逐颗映射 → `资料/普查产出_1003/按钮悬停图_普查.md`。
        ss = mb.get('m_SpriteState')
        if isinstance(ss, dict):
            parts = []
            for key, lbl in (('m_HighlightedSprite', 'HL'), ('m_PressedSprite', 'P'),
                             ('m_SelectedSprite', 'SEL'), ('m_DisabledSprite', 'DIS')):
                nm = _pn(ss.get(key), sidx)
                if nm: parts.append(f'{lbl}={nm}')
            if parts: extra += ' | ' + ' '.join(parts)
    elif 'm_Alpha' in k:                       # CanvasGroup（原版几处整块淡入淡出用它）
        extra = (f'CanvasGroup a={mb.get("m_Alpha")} blocksRaycasts={mb.get("m_BlocksRaycasts")} '
                 f'interactable={mb.get("m_Interactable")}')
    elif cls not in ('Image', 'TextMeshProUGUI', 'Text'):
        keys = sorted(x for x in k if x not in SKIP_KEYS)
        extra = '字段: ' + ','.join(keys[:26])
    return sprite, tint, text, extra


# ================================================================ 布局组（uGUI 逐条照抄）
def align_on_axis(axis, align):
    """照 uGUI `HorizontalOrVerticalLayoutGroup.GetAlignmentOnAxis`：

    ```
    if (axis == 0) return ((int)m_ChildAlignment % 3) * 0.5f;      // 横轴：Left/Center/Right
    else           return ((int)m_ChildAlignment / 3) * 0.5f;      // 纵轴：Upper/Middle/Lower
    ```
    纵轴的 `pos` 是**从父顶往下**量的 ⇒ `Upper*(0/1/2) → 0`、`Middle*(3/4/5) → 0.5`、`Lower*(6/7/8) → 1.0`。

    🔴 **2026-09-27 修一处写反的公式**：纵轴原来写的是 `(2 - align//3) * 0.5`（**反的** —— 把
       `UpperLeft(0)` 算成 **1.0** = 贴底）。子代理普查 Trophies 页时抓到：那一页的 `buttons`
       是真 VLG `align=0 (UpperLeft)`、内容高 580 < 父高 697.64 ⇒ 余量 117.64 被**错加到顶部**，
       表里 5 个 toggle 的 y **每个都多 117.64**。
       ⚠️ **§2·1 那条自检抓不到它**（`Tab Buttons` 的 `align=5 (MiddleRight)`，两式同值 0.5）
       ⇒ **自检绿 ≠ 这条对**（同族第二次踩，前一次是「没滤 inactive 子节点」）。
       出处：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/` 里没有 UI 源码，判据是
       uGUI 官方 `HorizontalOrVerticalLayoutGroup.cs` 的该函数 + Trophies 页那条实测反例。
    """
    return (int(align) % 3) * 0.5 if axis == 0 else (int(align) // 3) * 0.5


def _child_sizes(b, mono, k, axis, ctrl, fexp):
    """`GetChildSizes` + `LayoutUtility.Get{Min,Preferred,Flexible}Size` 的可用近似。

    ⚠️ **诚实边界**：主轴 `childControl*=1` 时，mins/pref 要走 `LayoutUtility` ——
       **文字的首选尺寸要 Unity 的字体度量，这里算不出**；嵌套布局件、ScrollRect 同理。
       遇到就 `unknown=True`，表里给那几个子节点的结果打 `?`，**不瞎填**。
    """
    if not ctrl:
        v = k['m_SizeDelta']['x'] if axis == 0 else k['m_SizeDelta']['y']
        return v, v, 0.0, False
    mn = pf = 0.0
    unknown = False
    for _cp, cls, mb in components_of(b, mono, k):
        if cls in ('TextMeshProUGUI', 'Text'):
            unknown = True
        if 'm_Spacing' in mb and 'm_Padding' in mb:
            unknown = True
        if 'm_CellSize' in mb:
            unknown = True
        if {'m_Content', 'm_Viewport'} <= set(mb.keys()):
            unknown = True
        key = 'Width' if axis == 0 else 'Height'
        if 'm_Min' + key in mb:
            if mb['m_Min' + key] >= 0:
                mn = mb['m_Min' + key]
            if mb.get('m_Preferred' + key, -1) >= 0:
                pf = mb['m_Preferred' + key]
            if pf < mn:
                pf = mn
    return mn, pf, (1.0 if fexp else 0.0), unknown


def _ignores_layout(b, mono, kid_rt):
    """这个孩子是不是 `LayoutElement.m_IgnoreLayout == 1`（Unity 建 `rectChildren` 时跳过它）。
    🔴 只看 **`LayoutElement`** 这一个类型（`ILayoutIgnorer` 在本工程里只有它）；类名走同一套
    `components_of`（MonoScript 明文），**不猜字段**。读不到就当 False（= 照旧收进布局）。"""
    try:
        for _cp, cls, mb in components_of(b, mono, kid_rt):
            if cls == 'LayoutElement' and mb.get('m_IgnoreLayout'):
                return True
    except Exception:
        return False
    return False


def apply_layout_to_children(b, mono, rtpid, rect, scale, kids):
    """把布局组**直接子节点**的 anchor/pos/sizeDelta 就地改成「布局跑之后」的值。

    返回 (est, lgcls, lgmb)：est ∈ {'ok','unk','grid?'}。
    主轴 = Vertical→y / Horizontal→x（**靠 MonoScript 的类名判，不靠字段猜**）。

    🔴 **2026-09-27 修一处真缺陷**：`kids` 必须**先滤掉 `m_IsActive=0` 的那些**。
       Unity 的 `LayoutGroup.SetDirty()` 建 `rectChildren` 时是
       `if (child == null || !child.gameObject.activeInHierarchy) continue;` —— **只收 active 的子节点**。
       不滤的话：① 每个键分到的尺寸会**偏小**（分母多算了 inactive 的）；② 整组的位置也会偏。
       本条是**子代理在 Ranking 页普查时抓到并报出来的**（出处：那页有 3 类共 11 个布局组带 inactive 子节点）。
       ⚠️ **`Tab Buttons` 那条自检抓不到它**（六个键全 active）—— 所以自检绿**不等于**这条对。
    """
    kids = [k for k in kids if (b.go.get(str(k.get('m_GameObject', {}).get('m_PathID'))) or {})
            .get('m_IsActive', 1)]
    # 🔴 **2026-09-27 再修一处真缺陷（同一族的第二条）**：还要滤掉
    #   **`LayoutElement.m_IgnoreLayout == 1`** 的子节点 —— Unity 建 `rectChildren` 时同样跳过它们
    #   （`ILayoutIgnorer.ignoreLayout`；`LayoutGroup` 收孩子那一步两者都判）。
    #   ⚠️ 这条是**普查聊天窗时抓到的**（子代理实测被它坑到 3 行）：`ChatPanel/Chat/Enter Text`
    #   的 `Background` 与 `Button`（真值都是**整行全拉伸 / 40×40 锚右中**，被算成 1120×0）、
    #   以及 `ChatMessageRow/RowBackground`（真值全拉伸，被算成 0 高并把**行内 y 全带偏**）。
    #   ⚠️ **自检抓不到它**：`--verify-layout` 那两个 fixture 的子节点都没有 `LayoutElement`。
    kids = [k for k in kids if not _ignores_layout(b, mono, k)]
    lg = None
    for _cp, cls, mb in components_of(b, mono, b.rt[str(rtpid)]):
        # 🔴 `m_Enabled=0` 的布局件**原版不跑** —— 跑了会把子节点整体挪走。
        #    实例：本窗的 `Menu Area` 挂着 HorizontalLayoutGroup 但 `m_Enabled=0`，
        #    照跑会把 `Tab Buttons` 从 180.24 顶到 187.24（正本 §2·1 早标了这条，2026-09-27 踩了一次）。
        if mb.get('m_Enabled', 1) == 0:
            continue
        if cls.startswith('VerticalLayout') or cls.startswith('HorizontalLayout') \
                or 'GridLayoutGroup' in cls or cls == 'LayoutGroup(?)':
            lg = (cls, mb)
            break
    if lg is None or not kids:
        return None, None, None
    cls, mb = lg
    if 'm_CellSize' in mb or 'GridLayoutGroup' in cls:
        return 'grid?', cls, mb
    if not any(k.startswith('VerticalLayout') or k.startswith('HorizontalLayout') for k in [cls]):
        return 'axis?', cls, mb          # 字段像布局组但类名没解出 ⇒ **不猜主轴**
    axis = 0 if cls.startswith('Horizontal') else 1
    pad = mb.get('m_Padding', {}) or {}
    spacing = mb.get('m_Spacing', 0) or 0
    ctrl = bool(mb.get('m_ChildControlWidth' if axis == 0 else 'm_ChildControlHeight'))
    fexp = bool(mb.get('m_ChildForceExpandWidth' if axis == 0 else 'm_ChildForceExpandHeight'))
    csz = MR.rect_of(b.rt[str(rtpid)], rect, scale)[1]
    size_main = csz[axis]
    size_other = csz[1 - axis]

    per = [(k,) + _child_sizes(b, mono, k, axis, ctrl, fexp) for k in kids]
    pad_main = (pad.get('m_Left', 0) + pad.get('m_Right', 0)) if axis == 0 \
        else (pad.get('m_Top', 0) + pad.get('m_Bottom', 0))
    n = len(per)
    tot_min = sum(p[1] for p in per) + spacing * max(0, n - 1) + pad_main
    tot_pref = sum(p[2] for p in per) + spacing * max(0, n - 1) + pad_main
    tot_pref = max(tot_min, tot_pref)
    tot_flex = sum(p[3] for p in per)
    minmax = 0.0
    if tot_min != tot_pref:
        minmax = max(0.0, min(1.0, (size_main - tot_min) / (tot_pref - tot_min)))
    pos = pad.get('m_Top', 0) if axis == 1 else pad.get('m_Left', 0)
    fmul = 0.0
    if size_main - tot_pref > 0:
        if tot_flex == 0:
            pos = (pad.get('m_Left', 0) if axis == 0 else pad.get('m_Top', 0)) \
                + (size_main - tot_pref) * align_on_axis(axis, mb.get('m_ChildAlignment', 0))
        else:
            fmul = (size_main - tot_pref) / tot_flex

    unk = False
    for (k, mn, pf, fx, u) in per:
        unk = unk or u
        child = mn + (pf - mn) * minmax + fx * fmul
        new = child if ctrl else (k['m_SizeDelta']['x'] if axis == 0 else k['m_SizeDelta']['y'])
        pv = k['m_Pivot']
        k['m_AnchorMin'] = {'x': 0, 'y': 1}
        k['m_AnchorMax'] = {'x': 0, 'y': 1}
        if axis == 0:
            k['m_AnchoredPosition']['x'] = pos + new * pv['x']
            k['m_SizeDelta']['x'] = new
        else:
            k['m_AnchoredPosition']['y'] = -pos - new * (1 - pv['y'])
            k['m_SizeDelta']['y'] = new
        pos += new + spacing

    # ---- 交叉轴（`alongOtherAxis` 支）----
    other = 1 - axis
    o_ctrl = bool(mb.get('m_ChildControlWidth' if other == 0 else 'm_ChildControlHeight'))
    o_align = align_on_axis(other, mb.get('m_ChildAlignment', 0))
    o_pad = (pad.get('m_Left', 0) + pad.get('m_Right', 0)) if other == 0 \
        else (pad.get('m_Top', 0) + pad.get('m_Bottom', 0))
    for (k, mn_, pf_, fx_, u_) in per:
        gmn, gpf, gfx, _ = _child_sizes(b, mono, k, other, o_ctrl, bool(mb.get(
            'm_ChildForceExpandWidth' if other == 0 else 'm_ChildForceExpandHeight')))
        inner = size_other - o_pad
        req = max(gmn, min(inner, size_other if gfx > 0 else gpf))
        start = (pad.get('m_Left', 0) if other == 0 else pad.get('m_Top', 0)) \
            + (size_other - (req + o_pad)) * o_align
        if o_ctrl:
            target = start
            k['m_SizeDelta']['x' if other == 0 else 'y'] = req
        else:
            sd = k['m_SizeDelta']['x' if other == 0 else 'y']
            target = start + (req - sd) * o_align
        if other == 0:
            k['m_AnchoredPosition']['x'] = target + (
                k['m_SizeDelta']['x'] * k['m_Pivot']['x'])
        else:
            k['m_AnchoredPosition']['y'] = -target - (
                k['m_SizeDelta']['y'] * (1 - k['m_Pivot']['y']))
    return ('unk' if unk else 'ok'), cls, mb


# ================================================================ 走树
def walk(b, mono, rtpid, rect, scale, depth, maxdepth, out, indent,
         apply_layout, sidx, bidx, force_root_rect=None):
    rt = b.rt.get(str(rtpid))
    if rt is None or depth > maxdepth:
        return
    gopid = rt.get('m_GameObject', {}).get('m_PathID')
    name = b.go_name(gopid) or f'<RT {rtpid}>'
    g = b.go.get(str(gopid)) or {}
    active = g.get('m_IsActive', 1)

    r, (w, h) = MR.rect_of(rt, rect, scale)
    if depth == 0 and force_root_rect is not None:
        r = force_root_rect
        w, h = r[2] - r[0], r[3] - r[1]
    scl = rt.get('m_LocalScale', {'x': 1, 'y': 1})

    details = []
    for _cp, cls, mb in components_of(b, mono, rt):
        sp, ti, tx, ex = describe(cls, mb, sidx, bidx)
        details.append(dict(cls=cls, sprite=sp, tint=ti, text=tx, extra=ex, mb=mb))

    kids = [b.rt.get(str(c)) for c in b.children(rtpid)]
    kids = [k for k in kids if k]
    est = None
    lgcls = None
    if apply_layout and kids:
        est, lgcls, _mb = apply_layout_to_children(b, mono, rtpid, rect, scale, kids)

    out.append(dict(ind=indent, name=name, rect=r, w=w, h=h, active=active, rt=rt, scl=scl,
                    details=details, est=est, lgcls=lgcls))
    for c in b.children(rtpid):
        walk(b, mono, c, r, (scale[0] * scl.get('x', 1), scale[1] * scl.get('y', 1)),
             depth + 1, maxdepth, out, indent + 1, apply_layout, sidx, bidx)


# ================================================================ 自检
def verify_layout():
    """自检两条，都要绿：
       ① `Tab Buttons` 自己的绝对矩形 = **95.48,180.24 → 273.48,885.75**（正本 §2·1）
       ② 6 个页签键**布局后**：165 × 109.252，x 108.48→273.48，顶从 180.24 步进 119.252
    🔴 **必须走整棵树**，不能只测 `Tab Buttons` 自己 —— 只测它的话，
       `Menu Area` 那个 **`m_Enabled=0`** 的布局件把父级挪歪的错误**测不出来**
       （2026-09-27 就是这么漏掉一次的）。
    """
    b = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    mono = mono_index(verbose=False)
    out = []
    walk(b, mono, '8897360498599033394', (0.0, 0.0, 1920.0, 1080.0), (1.0, 1.0),
         0, 4, out, 0, True, {'by_pid': {}, 'ambiguous': {}}, {})

    def find(nm, ind=None):
        for e in out:
            if e['name'] == nm and (ind is None or e['ind'] == ind):
                return e
        return None

    ok = True
    tb = find('Tab Buttons')
    if tb is None:
        print('  ❌ 没找到 Tab Buttons')
        return 1
    r = tb['rect']
    good = abs(r[0] - 95.48) < 0.02 and abs(r[1] - 180.24) < 0.02 \
        and abs(r[2] - 273.48) < 0.02 and abs(r[3] - 885.75) < 0.02
    ok = ok and good
    print(f'  {"✅" if good else "❌"} Tab Buttons 绝对矩形 {r[0]:.2f},{r[1]:.2f}→{r[2]:.2f},{r[3]:.2f}'
          f'（要 95.48,180.24→273.48,885.75）')

    want_y = [180.24, 299.49, 418.74, 537.99, 657.24, 776.49]
    want_h, want_x1, want_x2 = 109.252, 108.48, 273.48
    keys = [e for e in out if e['ind'] == 3 and e['name'].endswith(('Button', 'Trophies', 'Ranked'))]
    if len(keys) != 6:
        print(f'  ❌ 页签键数 = {len(keys)}（要 6）')
        return 1
    for i, (e, wy) in enumerate(zip(keys, want_y)):
        rr = e['rect']
        g = (abs(e['h'] - want_h) < 0.01 and abs(rr[1] - wy) < 0.02
             and abs(rr[0] - want_x1) < 0.02 and abs(rr[2] - want_x2) < 0.02)
        ok = ok and g
        print(f'  {"✅" if g else "❌"} {e["name"]:<20} {e["w"]:.2f}×{e["h"]:.3f} '
              f'x {rr[0]:.2f}→{rr[2]:.2f} 顶 {rr[1]:.2f}'
              f'（要 {want_h:.3f} / {want_x1}→{want_x2} / {wy}）')

    # ---- ②**纵轴对齐**的回归用例（`align = 0 (UpperLeft)` 那种）----
    # 🔴 为什么要单独一条：`Tab Buttons` 的 `align` 是 **5 (MiddleRight)**，
    #    纵轴公式写成 `(align//3)*0.5` 还是 `(2-align//3)*0.5` **两式同值 0.5** ⇒ 上面那条**抓不到**。
    #    子代理普查 Trophies 页时抓到过：那一页的 `buttons` 是 `align=0`，余量 117.64 被**错加到顶部**。
    #    判据：VLG `align=0` + `pad=0` ⇒ 第一个子节点**贴组的顶**（不是 +余量）。
    b2 = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    out2 = []
    walk(b2, mono_index(verbose=False), '8482975979751504434', (0.0, 0.0, 1920.0, 1080.0), (1.0, 1.0),
         0, 3, out2, 0, True, {'by_pid': {}, 'ambiguous': {}}, {})
    grp = next((e for e in out2 if e['name'] == 'buttons'), None)
    togs = [e for e in out2 if e['ind'] == 2 and e['name'].startswith('Achievement Type Toggle')]
    if grp is None or len(togs) != 5:
        print(f'  ❌ Trophies 的 `buttons` 用例取不到（组 {grp is not None}，子 {len(togs)}/5）')
        return 1
    gtop = grp['rect'][1]
    for i, e in enumerate(togs):
        wy = gtop + 120.0 * i          # 子高 100 + spacing 20
        g = abs(e['rect'][1] - wy) < 0.05 and abs(e['h'] - 100.0) < 0.02
        ok = ok and g
        print(f'  {"✅" if g else "❌"} VLG align=0 回归 · 子 {i} 顶 {e["rect"][1]:.2f}'
              f'（要 {wy:.2f} = 组顶 {gtop:.2f} + 120×{i}）')

    print('✅ 布局算法与正本 §2·1 的手算值逐位一致（含纵轴对齐回归用例）' if ok
          else '❌ 与 §2·1 不一致 —— **别用这套布局结果**')
    return 0 if ok else 1


# ================================================================ 主流程
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('bundle', nargs='?')
    ap.add_argument('root', nargs='?')
    ap.add_argument('--rt', default=None)
    ap.add_argument('--depth', type=int, default=6)
    ap.add_argument('--size', default='1920x1080')
    ap.add_argument('--relative', action='store_true')
    ap.add_argument('--active-only', action='store_true')
    ap.add_argument('--md', action='store_true')
    ap.add_argument('--no-sprite', action='store_true')
    ap.add_argument('--no-layout', action='store_true')
    ap.add_argument('--root-size', default=None)
    ap.add_argument('--verify-layout', action='store_true')
    args = ap.parse_args()

    if args.verify_layout:
        return verify_layout()
    if not args.bundle or (not args.root and not args.rt):
        ap.error('要 `bundle` + `root`（或 `--rt <pid>`）')

    path = args.bundle
    if not os.path.isdir(path):
        path = os.path.join(BUNDLES, args.bundle)
    b = MR.Bundle(path)
    mono = mono_index()

    sw, sh = (float(x) for x in args.size.lower().split('x'))
    root_rect = (0.0, 0.0, sw, sh)
    if args.root_size:
        rw, rh = (float(x) for x in args.root_size.lower().split('x'))
        root_rect = (0.0, 0.0, rw, rh)

    if args.rt:
        rtpid = str(args.rt)
        if rtpid not in b.rt:
            sys.exit(f'找不到 RectTransform {rtpid}（在 {path}）')
    else:
        gopid = b.find_go(args.root)
        if gopid is None:
            sys.exit(f'找不到 GameObject「{args.root}」')
        rtpid = b.rt_of_go(gopid)
        if rtpid is None:
            sys.exit(f'「{args.root}」没有 RectTransform')

    base_rect, pname = MR.parent_rect_of(b, rtpid, root_rect)
    if pname:
        print(f'# （已沿 `m_Father` 爬父链：被查节点的父 = 「{pname}」 '
              f'{base_rect[0]:.2f},{base_rect[1]:.2f} → {base_rect[2]:.2f},{base_rect[3]:.2f}）')

    sidx = {'by_pid': {}, 'ambiguous': {}} if args.no_sprite else sprite_index()
    bidx = {} if args.no_sprite else border_index()

    out = []
    walk(b, mono, rtpid, base_rect, (1.0, 1.0), 0, args.depth, out, 0,
         not args.no_layout, sidx, bidx,
         force_root_rect=(root_rect if args.root_size else None))

    ox, oy = (out[0]['rect'][0], out[0]['rect'][1]) if (args.relative and out) else (0.0, 0.0)

    def nm(e):
        return e['name']

    if args.md:
        print('| 缩进 | 名字 | 绝对矩形 x1,y1→x2,y2 | 宽×高 | 锚点 min→max | pivot | '
              'anchoredPosition | sizeDelta | act | 组件（类名） | sprite（名 + 原尺寸 + 九宫格） | '
              '贴图模式/颜色 | 文字（字号/对齐/色） | 其它参数 |')
        print('|---|---|---|---|---|---|---|---|---|---|---|---|---|---|')
        for e in out:
            if args.active_only and not e['active']:
                continue
            rt = e['rt']
            a, aM, p = rt['m_AnchorMin'], rt['m_AnchorMax'], rt['m_Pivot']
            pos, sd = rt['m_AnchoredPosition'], rt['m_SizeDelta']
            r = e['rect']
            sp = ' / '.join(d['sprite'] for d in e['details'] if d['sprite'])
            ti = ' / '.join(d['tint'] for d in e['details'] if d['tint'])
            tx = ' / '.join(d['text'] for d in e['details'] if d['text'])
            ex = ' ; '.join(d['extra'] for d in e['details'] if d['extra'])
            mark = '' if e['est'] in (None, 'ok') else f' ⚠️{e["est"]}'
            print(f'| {"·" * e["ind"]}{e["ind"]} | {nm(e)} '
                  f'| {r[0] - ox:.2f},{r[1] - oy:.2f}→{r[2] - ox:.2f},{r[3] - oy:.2f} '
                  f'| {e["w"]:.2f}×{e["h"]:.2f} | ({a["x"]:g},{a["y"]:g})→({aM["x"]:g},{aM["y"]:g}) '
                  f'| ({p["x"]:g},{p["y"]:g}) | ({pos["x"]:g},{pos["y"]:g}) '
                  f'| ({sd["x"]:g},{sd["y"]:g}) | {"T" if e["active"] else "**F**"} '
                  f'| {",".join(d["cls"] for d in e["details"])}{mark} | {sp} | {ti} | {tx} | {ex} |')
    else:
        print(f'# {args.root or args.rt}  ' + ('相对根左上角' if args.relative else '绝对矩形')
              + '（1920×1080 · 左上原点 · y 向下）')
        print(f'# 出处 {path}')
        print(f'{"深":<3}{"名字":<38}{"x1":>8}{"y1":>8}{"x2":>8}{"y2":>8}'
              f'{"宽":>8}{"高":>8}  {"act":<5}{"组件（类名）":<30}参数')
        for e in out:
            if args.active_only and not e['active']:
                continue
            r = e['rect']
            sc = '' if (abs(e['scl'].get('x', 1) - 1) < 1e-6
                        and abs(e['scl'].get('y', 1) - 1) < 1e-6) \
                else f' scl={e["scl"].get("x", 1):.4g}'
            cls = ','.join(d['cls'] for d in e['details'])
            mark = '' if e['est'] in (None, 'ok') else f'⚠️{e["est"]} '
            body = ' | '.join(x for x in (
                ' / '.join(d['sprite'] for d in e['details'] if d['sprite']),
                ' / '.join(d['tint'] for d in e['details'] if d['tint']),
                ' / '.join(d['text'] for d in e['details'] if d['text']),
                ' ; '.join(d['extra'] for d in e['details'] if d['extra']),
            ) if x)
            print(f'{e["ind"]:<3}{"  " * e["ind"] + e["name"]:<38}'
                  f'{r[0] - ox:>8.1f}{r[1] - oy:>8.1f}{r[2] - ox:>8.1f}{r[3] - oy:>8.1f}'
                  f'{e["w"]:>8.2f}{e["h"]:>8.2f}  {"" if e["active"] else "INACT":<5}'
                  f'{cls[:29]:<30}{mark}{sc} {body[:200]}')

    lg = [e for e in out if e['est']]
    if lg:
        print('\n⚠️ 布局组（子节点位置**由布局算**，上面已是**布局跑之后**的值）：')
        for e in lg:
            print(f'    {"  " * e["ind"]}{e["name"]} → {e["lgcls"]} [{e["est"]}]')
    unk = [e for e in out if e['est'] == 'unk']
    if unk:
        print('\n🔴 **下面这些布局组的主轴尺寸算不准**（子节点里有文字/嵌套布局件/ScrollRect，'
              '首选尺寸要 Unity 的字体度量）—— 表里那几个子节点的值**别照抄**：')
        for e in unk:
            print(f'    {"  " * e["ind"]}{e["name"]}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
