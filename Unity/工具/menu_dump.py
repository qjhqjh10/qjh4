#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""menu_dump.py — 一把尺子：把解包里**一整个界面子树**摊成「层 × 参数」表（`项目任务.md` §三 第 10 条 10·3 的「第 0 层」）。

为什么要有它（2026-09-27，多人界面那一批）：
    `menu_rect.py` 只给**矩形**，而 10·3 第 0 层要的格子是
    **类名** / **sprite 名** / rect / 锚点 / pivot / **字号（`m_fontSize` + `m_fontSizeBase`，后者印成 `基准=`）** / **颜色** / **activeSelf**。
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

🔴 **读数口径两条（A145，2026-10-07）—— 抄表之前先读这两条**：
   ① **「宽×高」「绝对矩形」是【布局框】（= 设计值），不是画出来的大小** ——
      画出来的 = 布局框 × **这一件自己的** `m_LocalScale`（父链上的缩放**已经**乘进去了）。
      表里逐行有一格 `×1.2 → 视觉 67.20×56.00`（缩放 = 1 的行是 `—`），文末另有一张清单。
      ⚠️ 本工程**踩过两次**（A60①/A106/A124）：`icon` 布局 56 ⇒ 实际 67.2 ·
      `Special Missions` 660.43 ⇒ 759.5 · `Daily Missions` 行宽 609.50 ⇒ 700.93。
      **判据只在 `menu_rect.visual_cell` 一处**（`--md`/纯文本/`menu_rect.py` 三种输出共用）。
   ② **「锚点 / `anchoredPosition` / `sizeDelta`」三列，对【被布局组管的节点】是「布局后的模拟值」、
      不是 prefab 字段** —— 本工具会把 uGUI 的布局跑一遍，并把结果**原地写回**那张 RT 表
      （`apply_layout_to_children()`：`m_AnchorMin`/`m_AnchorMax`/`m_SizeDelta`/`m_AnchoredPosition`
      四个字段都有写回点）。⇒ **拿这三列的数去 prefab JSON 里纯数值搜索是搜不到的**
      （A145② / A128 的「再算一步」那一类；实例：`#N FactionScoreBig` 的 x/apos ·
      `Main Icon`/`Individual rating value` 减 44.4/60 的「修正值」· `left-side` 内所有 y）。
      ⇒ 要 **prefab 原值**就用 `--no-layout`（那一档子件停在模板位、表里会出声），或 `menu_rect.py`。
      ⚠️ `--cs` 吐的也是这三列 ⇒ 它上面那句「原版 JSON 原文」只对**没被布局组管的节点**成立。
"""
import argparse
import io
import json
import os
import sys
import glob
import math

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

# 「这一件自带 `m_LocalScale`（≠1）」的两个**不同**阈值 —— 两处判据**故意不一样**，别合并：
#   · `SCL_EPS`（1e-6）：**逐行**那一格用。与**文本模式**的 `scl=` 同一个门槛
#     ⇒ 两种模式对「哪几行带缩放」的判断逐行一致（两处写两条判据 = 迟早不一致）。
#   · `SCL_WARN_EPS`（1e-3）：**结尾警告**用。只列**看得出来**的 ——
#     `0.999989` 这种序列化噪声（差 0.001%）逐行标出来是信息，汇成一张清单就是噪声。
#     ⚠️ 判据：视觉框比布局框差 **0.1%** 以上才进末尾清单。
# 🔴 **A145（2026-10-07）：这三个数与「视觉框」的算法已收进 `menu_rect.py`（只此一份）** ——
#    本文件只是**别名**，⛔ 别把数抄回来（两处写同一条规则 = 迟早不一致）。
SCL_EPS = MR.SCL_EPS
SCL_WARN_EPS = MR.SCL_WARN_EPS
SCL_WARN_MAX = MR.SCL_WARN_MAX
# 文本模式**最后那一列**（`参数`）的字符上限。🔴 原来是裸的 `body[:200]`（**静默截断**）；
# 2026-10-05 起截断时**出声**（尾上印 `……[+N]`，N = 被砍掉的字符数）—— 与同批修掉的
# `cls[:29]`（**直接不再截断**）同一族：本项目纪律「不许静默失败」。
# ⚠️ `--md` 表**本来就不截**（它按 `|` 分列，砍了会破坏整行）⇒ 两种模式对「哪几行被砍」不同口径，
# 但**都会明说**，不再有静默丢字符。
BODY_MAX = 200


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


def _scl_of(rt):
    """这一件自己的 `m_LocalScale`（导出 JSON 里大多数 RT 不带它 ⇒ 读不到就是 1，不是错误）。

    🔴 **A145：实现只在 `menu_rect.local_scale`**（⛔ 别在本文件里再写一份）。"""
    return MR.local_scale(rt)


def _scl_is_one(sx, sy, eps=SCL_EPS):
    """🔴 **A145：实现只在 `menu_rect.scale_is_one`**（门槛常量也住在那边）。"""
    return MR.scale_is_one(sx, sy, eps)


# ================================================================ activeInHierarchy（A108）
def _active_in_hierarchy(b, rt):
    """uGUI 的 **`GameObject.activeInHierarchy`** —— **整条父链**都 active，不是只看自己那一格。

    🔴 **2026-10-13（A499）：实现已挪到 `menu_rect.active_in_hierarchy`（只此一份），本函数 = 转发。**
       理由 = 本仓红线「两处写同一条规则 = 迟早不一致」：`menu_rect.py` 也要它了
       （它那张表的行内标记 `ANC✗` 与表尾的 `⛔GRP-off` 用的就是这一条）。
       ⛔ **别在这里再写第二份**；⛔ 也别改签名（`menu_rect` 那一份是同一个函数）。

    🔴 **2026-10-06 补（A108）**：uGUI 建 `rectChildren` 那一句判的是
       `if (rect == null || !rect.gameObject.activeInHierarchy) continue;`
       （`LayoutGroup.cs:60-79` 的上一句）——**父被关掉 ⇒ 子也不参与布局**。
       本文件原来一律只读 `m_IsActive`（`apply_layout_to_children` 里那行过滤），
       **只看自己那一格** ⇒ 「自己 active、祖先 inactive」的子件被**收进布局**（uGUI 会跳过）。
       **本包（`bundle_menus_assets_all`）落在差集里的点 = 0**（所以今天无影响），
       但那是**真差** —— 换一批窗口就会露。
    ⚠️ 递归到 `m_Father` 断链为止（预制体根没有父 ⇒ 链端那一件就是「最上面的祖先」）。
    ⚠️ 查不到（RT/GO 缺）时**返回 True**（不冤枉它）—— 与「缺件另有一块出声」分工不同。
    ⚠️ 缓存挂在 **`Bundle` 实例**上（键名 `_aih_cache`，本文件的 `--verify-layout` ⑮ 会 `pop` 它）：
       pid 是**分包局部**的，两张不同包的同号节点可以不一样。
    """
    return MR.active_in_hierarchy(b, rt)


# ================================================================ Image 的首选尺寸（A109）
# 🔴 **2026-10-06 接上**（A109，块13 认下的缺口）：`Image` 有 sprite 时 `preferredW/H`
#    **算得出**，公式来自 uGUI `Image.cs:1844-1879`（本地那份 = 工程实际用的版本）：
#    ```
#    if (activeSprite == null) return 0;
#    if (type == Sliced || type == Tiled) return DataUtility.GetMinSize(activeSprite).x / pixelsPerUnit;
#    return activeSprite.rect.size.x / pixelsPerUnit;
#    ```
#    · `DataUtility.GetMinSize(sprite)` = `(border.x + border.z, border.y + border.w)`
#      —— `m_Border` 的四个分量是 (left, bottom, right, top)（原版反汇编 VA `0x18304EFF0`；
#      本工程那份 uGUI 源码里 `Sprites.DataUtility.GetMinSize` 是同一条，见 `Sprites/DataUtility.cs`）。
#    · `activeSprite` = **`m_OverrideSprite` 非空就用它**（`Image.cs:408`），否则 `m_Sprite`。
#    · `pixelsPerUnit`（`Image.cs:741-754`）= `sprite.pixelsPerUnit / canvas.referencePixelsPerUnit`。
#      `canvas` 是**运行期**字段、预制体里没有 ⇒ **参考 ppu 只能取默认 100**（判据三条：
#      ① Unity `Canvas.referencePixelsPerUnit` 默认 100；② 本作 15 个 `CanvasScaler` 的
#      `m_ReferencePixelsPerUnit` **全是 100.0**；③ `Image.m_CachedReferencePixelsPerUnit`
#      的字段初值就是 100）。⚠️ 这一条**是假设**：若哪天某个 Canvas 改成别的值，这里要跟着改。
CANVAS_REF_PPU = 100.0
IMG_SLICED_TILED = (1, 2)          # `Image.Type`: 0 Simple · 1 Sliced · 2 Tiled · 3 Filled


def active_sprite_pid(mb):
    """`Image.activeSprite` 的 pid（`m_OverrideSprite` 非空就用它）—— 0 = 没有图。"""
    for key in ('m_OverrideSprite', 'm_Sprite'):
        pid = str((mb.get(key) or {}).get('m_PathID', 0))
        if pid not in ('0', ''):
            return pid
    return '0'


def image_pref_size(mb, sidx, bidx):
    """`Image.preferredWidth/Height`（uGUI `Image.cs:1844-1879`）。

    返回 `((w, h), why)`；**算不出时返回 `(None, why)`** —— `why` 必须能说出原因
    （「解不出名字」/「无尺寸记录」/「ppu=0」/「没索引」），⛔ 不许静默退成 0。
    ⚠️ 形状是「一个元组 + 一个原因串」，**判「算不算得出」只看第一个元素是不是 `None`**
      （2026-10-06 当场踩过：写成 `return (None, None), '原因'` ⇒ 调用方拿到 `(None,None)`、
       不是 `None`、于是取 `v[axis]` 得到 `None`、一路传成 `None` 才在别处炸掉）。
    """
    pid = active_sprite_pid(mb)
    if pid == '0':
        return (0.0, 0.0), ''                     # `activeSprite == null ⇒ 0`
    if not sidx or not bidx:
        return None, '没有 sprite 索引（`--no-sprite` 或索引取不到）'
    nm = sidx['by_pid'].get(pid)
    if not nm:
        return None, f'sprite pid {pid} 解不出名字'
    bb = bidx.get(nm)
    if not bb:
        return None, f'「{nm}」无尺寸记录'
    rw, rh, bd, ptou, _off = bb
    ppu = (ptou or 0.0) / CANVAS_REF_PPU
    if ppu == 0:
        return None, f'「{nm}」pixelsPerUnit=0'
    if mb.get('m_Type', 0) in IMG_SLICED_TILED:
        return ((bd[0] + bd[2]) / ppu, (bd[1] + bd[3]) / ppu), ''
    return (rw / ppu, rh / ppu), ''


# 🔴 **`m_LocalRotation` 的 z**（2026-10-05 补，A60⑩）。本文件在此之前**完全不读它** ——
#    旋转过的件在表里跟没转过的一模一样，**一个字都不提**（同族第四次：`m_ReverseArrangement` /
#    `m_Enabled=0` / 两个自适配件 / 这一个）。
#    实测 `bundle_menus_assets_all`：**190 / 16510** 个 RT 的 `|z| > 1e-6`（全库 253）。
#    ⚠️ 这些件的**子树都是空的**（190 个里没有任何一个有子节点 ⇒ 子树并集就是这 190 个），
#      所以传播只做在「万一有子节点」的保险上。
ROT_EPS = 1e-4          # 度；`|角度|` 小于它当没转（序列化噪声 `z≈1e-8` 那种）


def _rot_z(rt):
    """本件自己的 `m_LocalRotation` 绕 z 的角度（**度**；0 = 没转）。

    判据 = 四元数 (x, y, z, w) 里纯绕 z 的 `θ = 2·atan2(z, w)`（Unity 用右手系、y 向上）。
    ⚠️ **归一化到 `(-180, 180]`**：四元数是**双覆盖**的（`q` 与 `-q` 是同一个旋转），
       实测里 `w < 0` 的件算出来是 `355.36°` 这种，归一化后读作 `-4.64°`（同一个姿态，更好读）。
    ⚠️ **本工具不重算旋转后的位置**（要一条「有向框链」，不是 `rect_of` 能给的东西）⇒
       表里的矩形仍是**未旋转帧**的值；旋转件在表里逐行标 `rot=`，末尾另有一块单列出来。
    """
    q = rt.get('m_LocalRotation') or {}
    z, w = q.get('z', 0.0), q.get('w', 1.0)
    if abs(z) < 1e-9 and abs(w - 1.0) < 1e-9:
        return 0.0
    ang = math.degrees(2.0 * math.atan2(z, w))
    while ang > 180.0:
        ang -= 360.0
    while ang <= -180.0:
        ang += 360.0
    return 0.0 if abs(ang) < ROT_EPS else ang


def _rot_xy(rt):
    """**只绕 x / y 转**（`m_LocalRotation` 的 x/y 分量 ≠ 0、z ≈ 0）的那一类 —— 返回 `'x'`/`'y'`/`'xy'`/`''`。

    🔴 **2026-10-06 补（A110 尾巴）**：本文件只建模了**绕 z** 的旋转（`_rot_z`），
       `(x=-1, w=0)` 这种**翻转 180°** 的件在表里和没转过的一模一样、**一个字都不提**。
       实测 `bundle_menus_assets_all`：**14 个 RT**（33020 个带 `m_LocalRotation` 的 RT 里）
       的 x/y 分量非零 —— 例：`(x=0, y=1, z=0, w=0)` = 绕 y 翻 180°、
       `(x=-1, w≈0)` = 绕 x 翻 180°、`(x=-0.0279, w=0.99961)` = 小角度倾斜。
       ⚠️ **只报不改**（与 z 那一档同口径）：本工具不重算旋转后的位置/尺寸，但**必须出声** ——
          「转了却报成没转」是静默失败。
    阈值 = 四元数分量的 `1e-6`（`z` 那一档用的是**角度**阈值 `ROT_EPS`，两处量纲不同、别合并）。
    """
    q = rt.get('m_LocalRotation') or {}
    ax = 'x' if abs(q.get('x', 0.0)) > 1e-6 else ''
    ay = 'y' if abs(q.get('y', 0.0)) > 1e-6 else ''
    if not (ax or ay):
        return ''
    return (ax + ay) if _rot_z(rt) == 0.0 else ''      # z 非零的已由 `_rot_z` 那一档报过


def rot_corners(e):
    """一个**带旋转**的件的**视觉框四角 + AABB**（屏幕像素；`e` = `walk` 的 out 条目）。

    为什么需要它：`rect` / `w` / `h` 是**未旋转帧**里的布局框（`rectTransform.rect` 本身就不含旋转），
    而画出来的是一块**转了 `rot` 度**的四边形。旋转件不给个交代 = 静默错。
    算式（`M = T·R·S`，绕 **pivot** 转）：
      ① 视觉框尺寸 = 布局框 × **本件自己的** `m_LocalScale`（`m_LocalScale` 在 R 前面 ⇒ 先缩放再转）；
      ② 四角相对枢轴 = `((i − pivot.x)·W, (j − (1 − pivot.y))·H)`，`i, j ∈ {0,1}`（本坐标系 y 向下）；
      ③ 转 θ（**y 向下 ⇒ 屏幕上顺时针为正**）：`(dx·cosθ + dy·sinθ, −dx·sinθ + dy·cosθ)`
         —— 与 y 向上的 `(x cosθ − y sinθ, x sinθ + y cosθ)` 差一个 y 取反。
    返回 `(四角, (ax1, ay1, ax2, ay2))`。
    """
    x1, y1, x2, y2 = e['rect']
    piv = e['rt']['m_Pivot']
    sx, sy = _scl_of(e['rt'])
    W, H = MR.visual_size(sx, sy, e['w'], e['h'])   # 🔴 A145：算式只此一处（`menu_rect.visual_size`）
    pvx, pvy = x1 + piv['x'] * W, y1 + (1.0 - piv['y']) * H
    th = math.radians(e['rot'])
    c, s = math.cos(th), math.sin(th)
    pts = []
    for i in (0, 1):
        for j in (0, 1):
            dx, dy = (i - piv['x']) * W, (j - (1.0 - piv['y'])) * H
            pts.append((pvx + dx * c + dy * s, pvy - dx * s + dy * c))
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    return pts, (min(xs), min(ys), max(xs), max(ys))


def _md(x):
    """Markdown 单元格转义：`|` → `\\|`。

    🔴 2026-10-05：原来**不转义** ⇒ 内容里带 `|` 的那一行**整行多出一列**，后面所有列
      右移一格（例：`Alliance Trophy Info Popup` 的 `Generic Close Button Orange` ——
      它的 `m_SpriteState` 那截是 `HL=… P=… | HL=… P=…`，见 `describe()`）。
      **列错了比缺列更坏**：抄的人会从**错的列**里取值，而且那一行看着「有内容、不像坏的」。
      GFM 里 `\\|` 不分列（`CLAUDE.md` 数竖线那条也认这个转义）。
    """
    return x.replace('|', '\\|')


def _scl_cell(e, sx, sy):
    """表里那一格「局部缩放→视觉框」—— **`--md` 表与本文件的【纯文本】表共用这一格**。

    🔴 **为什么单开一列（2026-10-05，A60①）**：本表「宽×高」「绝对矩形」印的都是**布局框**
      （`sizeDelta` / 锚点算式），而**画出来**的是 **布局框 × 这一件自己的 `m_LocalScale`**。
      **纯文本模式本来就有 `scl=` 这一格，`--md` 表原来没有** ⇒ 看纯文本表的人知道要乘，
      看 `--md` 表的人**会把布局框当成视觉值抄走**（本工程真发生过：`icon` 布局 56、实际 67.2；
      `Special Missions` 布局 660.43、实际 759.5）。
    🔴 **A145（2026-10-07）**：核实结果 —— 这一格**逐行都盖**（`--md` 的每一行都过它，
      不是「只盖某一类节点」）；缺的是**纯文本模式只有一个 `scl=` 系数、没有视觉框**，
      以及 `menu_rect.py` 那张表一个字都不提 ⇒ 已把**纯文本模式也改成这一格**，
      并把算法收进 `menu_rect.visual_cell`（**只此一份**，三种输出调的是同一个函数）。
      与文本模式的 `scl=` **同门槛**（`SCL_EPS`），三种输出对「哪几行带缩放」的判断逐行一致。
    形状：`—` = 缩放 1（绝大多数行）；`**×1.2 → 视觉 67.20×56.00**` = 要乘。
    """
    return MR.visual_cell(sx, sy, e['w'], e['h'], md=True)


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
            # 🔴 **A499 追加：把类名绑上去**（原来是裸的 `m_Enabled=0`）—— 见下面兜底那一段的长注释。
            tint += f' **m_Enabled=0**（`{cls}` 组件被禁）'
    if 'm_text' in mb:
        s = f'{mb.get("m_text")!r} 字号={mb.get("m_fontSize")}'
        # 🆕 **A335（2026-10-12）：把 `m_fontSizeBase` 印成一格 `基准=`**（原来**一个字都不提**）。
        #   🔴 **为什么非印不可**：`base` 是**自适应重排的起点**（`TextMeshPro.cs:2149-2150`
        #     `if (m_enableAutoSizing) m_fontSize = Clamp(m_fontSizeBase, min, max)`），
        #     而已有的两格**都盖不到它** —— ① `字号=m_fontSize` 是**收敛后**的值；
        #     ② `fontSize` 的 setter **只在 `!m_enableAutoSizing` 时才回写 base**，且 `m_fontSize == value` 直接早退
        #     （`TMP_Text.cs:467`）⇒ 作者开着 autoSizing 调字号时 **base 一次都没被改过**，
        #     于是「base = 36」多半意味**没设过**（TMP 序列化默认值，`TMP_Text.cs:473`）。
        #   ⇒ **原先想核 base 的人只能自己重写一遍扫描**（A305 普查就是这么被逼的，
        #     见 `资料/普查产出_1011/V7_A305_A304_普查.md` §六·1）。
        #   ⚠️ **与 `auto[…]` 不同档、别合并**：这一格**与 `m_enableAutoSizing` 无关** ——
        #     `auto=0` 的站也有 base（例：`Campaign Tab` 那颗 `Quantity` = 61.23，V7 §二·3 #4）。
        #   ⚠️ **印在 `字号=` 之后是故意的**：文本模式最后那格「参数」有 `BODY_MAX` 截断、**砍的是尾巴**
        #     ⇒ 越靠前的字段越保得住（同 `reverse=1` 那条体例）。
        if 'm_fontSizeBase' in mb:
            s += f' 基准={mb["m_fontSizeBase"]}'
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
            # 🆕 2026-10-05：把 **`m_ReverseArrangement`** 印出来（原来是**一个字都不提**）。
            #   本文件在 2026-10-05 之前**完全不读这个字段** ⇒ 凡 `reverse=1` 的布局组，
            #   子节点的「名字 ↔ 位置」是**镜像**的（`布局跑后`那一列全是错的，而且看着很像对的）。
            #   uGUI 语义见 `apply_layout_to_children` 里 `rev` 那一段。
            #   🔴 **必须放在最前面**：文本模式的「参数」列有 `BODY_MAX` 截断（印 `……[+N]`），
            #      实测放在末尾时 `Deck Options` 那一行**正好把它截掉**（这一格正是最该看见的那一格）。
            #      只在为真时印（与 `preserveAspect` / `**m_Enabled=0**` 同一体例）。
            extra = ('**reverse=1**（主轴按树序倒排：最后一个子件在最左/最上） '
                     if mb.get('m_ReverseArrangement') else '') \
                + (f'spacing={mb.get("m_Spacing")} align={mb.get("m_ChildAlignment")} '
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
    # 🔴 **2026-10-06 补（A98）：`m_Enabled=0` 原来【只有 `Image` 那一支】印**（印在 `tint` 里）——
    #    `TextMeshProUGUI` / `Text` / 布局组 / `Mask` / `ScrollRect` / `Toggle` / `Button` /
    #    `CanvasGroup` / 认不出类名的那一支**一个字都不提**。
    #    后果实例：原版 `Unlock` 那颗 `Text` 的 TMP 组件**是 `m_Enabled=0`**（文本 `Debug Unlock`）
    #    ⇒ 那行字**原版根本画不出来**；但 dump 表里看不出来，后来是**另写脚本直读 prefab JSON**
    #    才坐实的 —— 只读表的人会以为它是可见的，反过来「修」出一个**原版没有的字**。
    #    这里**统一兜底**（一个组件只要有 `m_Enabled=0`，一定在它自己那一行看得见）：
    #    挑该支**已经产出了内容**的那一格挂上去（`text` 优先，因为读者最先看那格）；
    #    `Image` 那支已经在 `tint` 里印过了 ⇒ **不重复印**（判据 = 四格里已经有 `m_Enabled=0`）。
    #    ⚠️ 与 `MenuDraw` 那边的口径一致：**组件级禁用 ≠ GO 级 inactive**，两个都要能看见。
    #
    # 🔴 **A499 追加（2026-10-13）：这一条原来【不带类名】，一个节点挂多个组件时会被读错。**
    #    行里那四格是**各组件的内容并排拼起来的**（`sp`/`ti`/`tx`/`ex` 四个 `join`，见 `main()`），
    #    **看不出哪一格属于哪个组件** ⇒ 光一个 `m_Enabled=0` **看不出是哪个组件被禁**。
    #    **真代价（记档）**：`资料/普查产出_1013/A表现核_块1.md` §A389 把
    #    `holder/Image` 写成「`Image` 挂着 `m_Enabled = 0`（原版根本不画它）」—— **错**。
    #    现读两份 MB 原文：那一颗 `Image` 是 **`m_Enabled: 1`** + `m_Sprite: 40k_missions_milestone_off`
    #    （`MonoBehaviour_-5355200480893724929.json`）；**`m_Enabled: 0` 的是同一节点上的 `Outline`**
    #    （`MonoBehaviour_7171112632542172927.json`）⇒「被禁」被记到了**另一个组件**头上。
    #    实测本包（`bundle_menus_assets_all`）：挂 ≥2 个组件的 GO **9764** 个，其中**有禁用组件**的
    #    **595** 个；「启用且有 sprite 的 `Image` + 另一个被禁组件」这一档 **23** 个
    #    （全是 `Image`+`Outline`，图都是 `40k_missions_milestone_off`）—— **误读面不是一个孤例**。
    #    ⇒ 两处一起改：① **标记带上类名**（这一句）；② `main()` 里 ≥2 组件且有禁用时**行内再出声**
    #       （`⛔MULTI-COMP`，判据只此一份 = `multi_comp_flag`）。
    if mb.get('m_Enabled', 1) == 0 and 'm_Enabled=0' not in (sprite + tint + text + extra):
        _mk = f' **m_Enabled=0**（`{cls}` 组件被禁 ⇒ 它不画；**组件级禁用 ≠ GO inactive**）'
        if text:
            text += _mk
        elif extra:
            extra += _mk
        elif tint:
            tint += _mk
        else:
            sprite += _mk
    return sprite, tint, text, extra


def multi_comp_flag(e):
    """行内那个 `⛔MULTI-COMP(…)` 标记（**'' = 不打**）—— **判据只此一份**，两种输出模式共用。

    🔴 **A499 追加**：一个节点挂 **≥2 个组件**时，注里那四格（sprite/tint/text/extra）是
       **各组件的内容并排拼起来的**、**没有一格说得清自己属于谁**；而 `m_Enabled=0` 那一条
       只说「**有一个**组件被禁」。⇒ 这一档**必须出声**，否则读的人会把「被禁」记到另一个组件头上。
       （真代价 + 实测数字见 `describe()` 里那一段。）
    ⚠️ `n == 1` 时**不打** —— 那时四格属于同一个组件，没有歧义（也让单组件那些行的输出**逐字节不变**）。
    """
    n = len(e['details'])
    k = sum(1 for d in e['details'] if d['mb'].get('m_Enabled', 1) == 0)
    return f'⛔MULTI-COMP({n}组件·{k}禁用)' if (n >= 2 and k) else ''


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


def _clamp(v, lo, hi):
    """`Mathf.Clamp(value, min, max)` —— **逐行照抄**，不是 `max(lo, min(v, hi))`。

    Unity 的实现是先判 `value < min` **再**判 `value > max`（`if/else if` 两段）
    ⇒ `min > max`（比如 `LayoutElement` 的 `m_MinWidth` 大于组自己的 `size`）时，
    三个分支各有归属，**与 `max(lo, min(v, hi))` 不总是同值**。
    本文件只在 `requiredSpace` 那一处用到（见 `apply_layout_to_children` 交叉轴）。
    """
    if v < lo:
        return lo
    if v > hi:
        return hi
    return v


# ================================================================ ILayoutElement 的取值
# 🔴 **2026-10-05 补 `LayoutUtility.GetLayoutProperty` 的优先级规则**（A60②，原来按 `m_Component`
#    序「后写覆盖」—— 那是**错的**）。判据 = uGUI `Layout/LayoutUtility.cs:135-175` 逐行照抄。
#
# 先把「谁算 ILayoutElement」钉死（**不猜类名**）：名单 = **原版自己的 il2cpp dump** 里
# `ILayoutElement` 的**传递闭包**（41 个类；`d:/2/tools/il2cpp_out/dump.cs`，
# 关键几个：`LayoutElement:924898` · `LayoutGroup:925049` · `ScrollRect:926190` ·
# `Image:923051` · `Text:927217` · TMP 系 854711/855093 · `UIPrimitiveBase:695963` ·
# `UIPreferedSizeFixer:114680`）。**我们自己工程里的 uGUI 版本可能不同** ⇒ 只拿它当「有哪些类」的名单，
# 语义一律读 dump 里的实现。逐类的值（都有出处）：
#
# | 类 | priority | min | pref | flex | 出处 |
# |---|---|---|---|---|---|
# | `LayoutElement` | `m_LayoutPriority`（**默认 1**） | `m_Min{W,H}` | `m_Preferred{W,H}` | `m_Flexible{W,H}` | `LayoutElement.cs:21,175` |
# | `UIPreferedSizeFixer` | **代理** → 转发给 `layoutElement` 字段那一颗 | 同左 | 同左 | 同左 | VA `0x1808724E0/510/540/5A0` 四条都是 `rcx=[this+0x30]` 再 `jmp` 它的 vtable 槽 |
# | TMP 系（`TextMeshPro{,UGUI}` · `EverguildTextMeshPro`） | 0 | `m_minWidth`（**不序列化 ⇒ 0**） | **算不出**（字体度量） | `-1` ⇒ **跳过** | `TMP/TMP_Text.cs:1460/1466/1507` |
# | `Text` · `TextPic` | 0 | 0 | **算不出**（字体度量） | -1 ⇒ 跳过 | `Text.cs:720/725/734` |
# | `Image` · `SkewedImage` · `UIParallaxImage` · `InvertedMaskImage` | 0 | 0 | 有 sprite ⇒ **算不出**（要图集尺寸）；无 sprite ⇒ 0 | -1 ⇒ 跳过 | `Image.cs:1836-1890` |
# | LayoutGroup 系（16 个子类） | 0 | 本组内容之和 ⇒ **算不出**（要递归 + 字体度量） | 同左 | 同左 | `LayoutGroup.cs:117-125` |
# | `ScrollRect` 系 | **-1** | -1 | -1 | -1 | `ScrollRect.cs:1126-1152` ⇒ **全系数为负 ⇒ 每个属性都 `continue` ⇒ 等于不存在** |
# | `UIPrimitiveBase` 系 | 0（`xor eax,eax; ret`，VA `0x1804BD440`） | **算不出** | 算不出 | **算不出** | 保守：宁可标 `?` 不瞎填 |
# | `InputField` | 1 | 5 | 算不出（字体度量） | -1 ⇒ 跳过 | `InputField.cs:3437/3442/3456/3485` |
#
# ⚠️ **`Image` 有 sprite 时的 `pref`** —— 公式已查实、**2026-10-06 已接上（A109）**：
#    `Simple/Filled` ⇒ `sprite.rect.size / pixelsPerUnit`（`Image.cs:1849`）；
#    `Sliced/Tiled` ⇒ `Sprites.DataUtility.GetMinSize(sprite) / pixelsPerUnit`，而那个函数
#    **反汇编出来了**（VA `0x18304EFF0`）= `(border.x + border.z, border.y + border.w)`；
#    接线在 `image_pref_size()`，算不出时仍标 `LG_UNK` 并**说得出原因**。
#    ⚠️ 接之前它们被当 **0** 且标 `ok`（**静默错**，本包 296 个 ctrl=1 组的 `pref` 赢家是 `Image`）。
# 🔴 **`TMP` 系的 `pref`** —— **2026-10-06（调度台裁定）也改成 `LG_UNK`**：字段根本没序列化，
#    原来 `mb.get(tmp_field, 0.0)` 恒 **0** ⇒ 报「pref = 0、**确定**」= 把「不知道」写成「知道」，
#    是**静默错**（详见 `_ilayout_cells` 的 TMP 支那一段）。
LG_SKIP = 'SKIP'                # `prop < 0` ⇒ `LayoutUtility` 直接 `continue`
LG_UNK = 'UNK'                  # 这个 ILayoutElement **会**供这个属性，但本工具算不出
# 「谁算 ILayoutElement」的名单（判据 = dump.cs 里 ILayoutElement 的传递闭包，见上表）
LGE_IDENTITY = {'LayoutElement'}
LGE_PROXY = {'UIPreferedSizeFixer'}
LGE_TMP = {'TextMeshPro', 'TextMeshProUGUI', 'EverguildTextMeshPro'}
LGE_TEXT = {'Text', 'TextPic'}
LGE_IMAGE = {'Image', 'SkewedImage', 'UIParallaxImage', 'InvertedMaskImage'}
LGE_GROUP = {'LayoutGroup', 'HorizontalLayoutGroup', 'VerticalLayoutGroup',
             'HorizontalOrVerticalLayoutGroup', 'GridLayoutGroup', 'EverguildGridLayoutGroup',
             'EverguildLayoutGroup', 'AutoGridLayout', 'CurvedLayout', 'FlexibleGridLayout',
             'FlowLayoutGroup', 'RadialLayout', 'TableLayoutGroup'}
LGE_SCROLL = {'ScrollRect', 'ScrollRectEx', 'MultiTouchScrollRect', 'RecyclableScrollRect'}
LGE_PRIM = {'UIPrimitiveBase', 'UICircle', 'UICornerCut', 'UILineRenderer', 'UILineRendererList',
            'UILineTextureRenderer', 'UIPolygon', 'UISquircle', 'UIGridRenderer', 'DiamondGraph'}
LGE_INPUT = {'InputField', 'TMP_InputField', 'EverguildInputField'}


def layout_property(cells):
    """uGUI **`LayoutUtility.GetLayoutProperty`**（`LayoutUtility.cs:135-175`）的逐行对照。

    ```csharp
    float min = defaultValue;  int maxPriority = int.MinValue;
    rect.GetComponents(typeof(ILayoutElement), components);      // ← 【m_Component 序】
    for (...) {
        var layoutComp = components[i] as ILayoutElement;
        if (layoutComp is Behaviour && !isActiveAndEnabled) continue;   // ← m_Enabled=0 跳过
        int priority = layoutComp.layoutPriority;
        if (priority < maxPriority) continue;                    // 低优先 ⇒ 忽略
        float prop = property(layoutComp);
        if (prop < 0) continue;                                  // 【不更新 maxPriority】
        if (priority > maxPriority) { min = prop; maxPriority = priority; }
        else if (prop > min) { min = prop; }                     // 同级 ⇒ 取 max
    }
    ```

    `cells` = `[(priority, 值 | LG_SKIP | LG_UNK)]`，**必须按 `m_Component` 序**。
    返回 `(值, 是否确定)`；不确定 = 有一个**会赢的**分量算不出（`LG_UNK`）。

    🔴 **三处容易写错，逐条钉死**（手算 fixture 在 `verify_layout()` ⑥）：
      ① **`priority < maxPriority` 是严格小于** ⇒ 同级进 `else if (prop > min)` 分支取 max；
      ② **`prop < 0`（= `LG_SKIP`）的 `continue` 在 `maxPriority` 更新【之前】** ⇒
         一个「声明了但值为负」的高优先级件**既不当选、也不压制**后面的低优先级件
         （`LayoutElement` 的默认值 `-1` = 「没声明」走的就是这条路）；
      ③ 高优先级**无条件覆盖**（哪怕值更小）：`priority > maxPriority ⇒ min = prop`。

    手算（`defaultValue = 0`、`maxPriority` 初值 = `int.MinValue`）：
        `[(1,50),(1,30)]`     → 50            （同级取 max）
        `[(1,50),(2,30)]`     → 30            （高优先级**更小也覆盖**）
        `[(2,50),(1,30)]`     → 50            （低优先级直接忽略）
        `[(1,SKIP),(0,20)]`   → 20            （SKIP 不抬 maxPriority）
        `[(0,20),(1,SKIP)]`   → 20            （同上，后者根本没进循环体）
        `[(1,50),(0,UNK)]`    → (50, **确定**) ← 关键：高优先级已定，0 级的 TMP 不参与
        `[(0,UNK),(1,50)]`    → (50, 确定)
        `[(1,SKIP),(0,UNK)]`  → (0, **不确定**)
        `[(1,50),(1,UNK)]`    → (50, **不确定**)
        `[]`                  → (0, 确定)     （`defaultValue`）
    """
    val = 0.0                       # `defaultValue`
    maxp = None                     # `int.MinValue`（用 `None` 表示「还没人说过话」）
    known = True
    for prio, cell in cells:
        if maxp is not None and prio < maxp:
            continue                                        # ① 低优先 ⇒ 忽略
        if cell is LG_SKIP:
            continue                                        # ② 注意：不更新 maxp
        if maxp is None or prio > maxp:                     # ③ 严格更高 ⇒ 无条件覆盖
            maxp = prio
            if cell is LG_UNK:
                known = False
            else:
                val = cell
                known = True                                # 更高优先级把「不确定」顶掉了
        else:                                               # 同级 ⇒ `else if (prop > min)`
            if cell is LG_UNK:
                known = False
            elif cell > val:
                val = cell
    return val, known


# 🔴 `EverguildLayoutGroup` 的 `CalculateSpacing` 开关。**拼写照原版序列化字段抄**
#    （`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/` 里那一颗的键名，逐字节核过）：
#    是 `evenlySpace`**`i`**`nBetween`（小写 i，`0x69`），**不是** `evenlySpaceInBetween`（大写 I，`0x49`）。
#    ⚠️ 两个拼写只差一个字符、`mb.get()` 给不出任何提示（错的那个**静默返回 None**）——
#       实测本文件 2026-10-06 之前所有**代码**里的拼写都是对的（`_lg_is_vertical` / ⑱ fixture），
#       本条只是把它**收成一个常量**，免得下一处再手打一遍。
#       （写错的表现 = 那一档永远走 `flag=0` 分支、静默按「序列化 spacing」排；
#        本包 3 个实例因为组是拉伸锚点，序列化值恰好等于运行期值 ⇒ **在这个包上根本看不出来**，
#        但 `--verify-layout` ⑱c 那条纯函数 fixture 会红。）
EG_FLAG = 'evenlySpaceinBetween'


def everguild_spacing(flag, spacing_serial, n, first_size, self_size):
    """`EverguildLayoutGroup.CalculateSpacing()` 的逐行对照（A150）⇒ `(spacing, why)`。

    **纯函数**（不碰 bundle）—— 判据 = 反编译 `d:/2/tools/decomp_full/EverguildLayoutGroup__CalculateSpacing.c`
    逐行读出来的，原 C# 形状：

    ```csharp
    protected virtual void CalculateSpacing() {
        if (!evenlySpaceinBetween) return;                      // ① flag=0 ⇒ 整条 no-op
        var list = rectChildren.Where(x => x != null).ToList(); // ② 过滤 null（**不是**重新收孩子）
        if (list.Count == 0) return;                            // ③ n=0 ⇒ return（不动 m_Spacing）
        var first = list[0].rect;                               // ④ 第一个 rectChildren 的 rect
        var self  = rectTransform.rect;                         // ⑤ 本组自己的 rect
        float a = (alignment == Horizontal) ? first.width : first.height;
        float b = (alignment == Horizontal) ? self.width  : self.height;
        spacing = (b - list.Count * a) / (list.Count - 1);      // ⑥ 写回 m_Spacing
    }
    ```
    反汇编逐句（`--` 后是 VA 里的形状）：`if (*(char*)(this+0x74) == 0) return;` ⇒ `0x74` = 本字段 ·
    `Enumerable.Where(char)` + `ToList` + `get_Item(0)` ⇒ 第 ④ 步 ·
    两个分支取 `Rect` 的 **低 4 字节（width）/ 高 4 字节（height）** ⇒ 第 ⑤ 步 ·
    末尾 `set_spacing((b − n·a) / (n − 1))`。

    参数：`flag` = `mb[EG_FLAG]` · `spacing_serial` = 序列化的 `m_Spacing` ·
    `n` = `rectChildren.Count`（= 本工具 `_rect_children` 那一份）·
    `first_size` / `self_size` = **主轴**上第一个子件 / 本组自己的局部尺寸
    （`RectTransform.rect.size[axis]`，`axis = 0 if alignment == Horizontal else 1`）。

    返回 `(spacing, why)`：`why is None` ⇒ 算得出；否则是「为什么算不出」的短标签（调用方**出声**）。

    🔴 **`n == 1` 这一档原版是坏的，本工具不跟着算**（`why = 'n==1'`）：
    `(b − 1·a) / (1 − 1)` = `x / 0` ⇒ C# 的 float 除法得 **±Infinity**（`x == 0` 时是 **NaN**）；
    往下 `CalcAlongAxis` 里 `totalMin += min + spacing` 之后再 `-= spacing`
    （`HorizontalOrVerticalLayoutGroup.cs:126-128`）⇒ `Inf − Inf = NaN`
    ⇒ `totalPreferred = Max(NaN, NaN)`、`minMaxLerp = Clamp01(NaN)`、
    `SetChildAlongAxisWithScale(child, axis, pos + NaN, …)` ⇒ **子件的 `anchoredPosition` 是 NaN**。
    ⇒ 原版这一档**本来就没有确定的版面**，本工具给 `sp?`（没排、子件停在模板位）比给一个有限值诚实。
    ⚠️ 只有 `n == 1` 会这样：`n >= 2` 时算式与 `n == 0`（原版直接 return）都正常。
    ⚠️ `n == 0` **不是**退化档 —— 原版 `if (list.Count == 0) return;` 会把 `m_Spacing`
    **原样留在序列化值上** ⇒ 那时序列化值**就是**运行期值（本函数 `n == 0` 也返回 `spacing_serial`）。
    """
    if not flag:
        return spacing_serial, None          # ① `evenlySpaceinBetween = 0` ⇒ 整条 no-op
    if n == 0:
        return spacing_serial, None          # ③ 原版 `return` ⇒ `m_Spacing` 保持序列化值
    if n == 1:
        return None, 'n==1'                  # ⑥ `x / 0` ⇒ ±Inf / NaN ⇒ 原版没有确定版面
    if first_size is None or self_size is None:
        return None, 'size?'                 # 尺寸恢复不出（父链缩放为 0）⇒ 别猜
    return (self_size - n * first_size) / (n - 1), None


def _lg_is_vertical(cls, mb):
    """这个布局组的**主轴是不是纵轴**（uGUI 靠**子类**把 `isVertical` 传进 `CalcAlongAxis`）。

    返回 `True` / `False` / **`None`（判不出 ⇒ 调用方必须标 `LG_UNK`，⛔ 不许猜）**。
    · `VerticalLayoutGroup` / `HorizontalLayoutGroup` —— 类名直接说（原版 dump 的继承链：
      `VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup : LayoutGroup`，
      `d:/2/tools/il2cpp_out/dump.cs:925008/925425`）。
    · 🔴 **`EverguildLayoutGroup`（2026-10-06 A127 接上）** —— 它是
      `HorizontalOrVerticalLayoutGroup` 的子类，主轴**不在类名里**而在序列化字段 `alignment` 上
      （`dump.cs:112913-112935`：`alignment` @0x70 · `evenlySpaceinBetween` @0x74 ·
      枚举 `Alignment { Horizontal = 0, Vertical = 1 }` @`dump.cs:112899`）。
      判据 = 反汇编（`d:/2/tools/decomp_full/`），**四条一起读才有结论**：

      ① `EverguildLayoutGroup__get_IsVertical` = `*(int *)(this + 0x70) == 1`
         ⇒ `isVertical = (alignment == 1)`（`0x70` 就是 `alignment`）。
      ② 四个覆写体**开头先调一次它自己的虚方法**（`CalculateSpacing`，本类**新增**的虚方法 ——
         全量 dump 里 `CalculateSpacing` **只出现在这个类**，uGUI 侧没有这个钩子；
         调用形状 = `klass` 上 slot 41 的 `(methodPtr, methodInfo)` 一对，见下面 ⑯ 那条注释）：

             public override void CalculateLayoutInputHorizontal() {
                 CalculateSpacing();                       // ← 头一句
                 base.CalculateLayoutInputHorizontal();
                 CalcAlongAxis(0, IsVertical);             // ← 反汇编里就是 (int)this[0xe] == 1
             }
             （`CalculateLayoutInputVertical` / `SetLayoutHorizontal` / `SetLayoutVertical`
               三处同形：先 `CalculateSpacing()`，再 `CalcAlongAxis(1|SetChildrenAlongAxis(0/1), IsVertical)`）

      ③ `EverguildLayoutGroup__CalculateSpacing` 的**第一句**是
         `if (*(char *)(this + 0x74) == 0) return;` ⇒ **`evenlySpaceinBetween == 0` 时整条是 no-op**，
         四个覆写体剩下的部分**只剩**「用 `isVertical = (alignment == 1)` 调 HOVLG 的
         `CalcAlongAxis` / `SetChildrenAlongAxis`」⇒ 与 `HorizontalOrVerticalLayoutGroup` **逐位等价**
         （本工具那两套算式就是照 HOVLG 抄的，见 `_group_axis_totals` / `apply_layout_to_children`）。
      ④ `evenlySpaceinBetween == 1` 时它会**改写 `m_Spacing`**（`set_spacing`）：
         `spacing = (本组该轴尺寸 − 子件数 × 第一个子件该轴尺寸) / (子件数 − 1)`
         —— **序列化的 `m_Spacing` 不是运行期那个值**。
         🔴 **2026-10-06 A150：算得出与算不出要分开看 —— 本函数【仍】返回 `None`，但只代表
         「**本组自己的 ILayoutElement 总量**算不出」，**不再**代表「子件排不了」**：
           · `apply_layout_to_children`（= 子件落位）**单独放行这一档**（算式见 `everguild_spacing`）：
             它所处的帧**恰好**是 uGUI 调 `CalculateSpacing` 的那一帧（推导写在它的 docstring 里）；
           · `_ilayout_cells`（= 本组的三项总量，走 `_group_axis_totals`）**仍标 `LG_UNK`**：
             `CalcAlongAxis` 那一帧与本工具的帧**不是同一个**（phase 1/3 vs phase 2/4），
             而且 `_group_axis_totals` 手上**没有「本组自己的局部尺寸」**这个入参
             （要补得让它穿过 `_child_sizes → _ilayout_cells → _group_axis_totals` 三层）。
             ⛔ **拿序列化的 `m_Spacing` 顶替** = 把「不知道」写成「知道」（本包 3 个实例恰好相等，
             只是**因为它们都是拉伸锚点的组**；换一档宽度就错）⇒ 宁可 `LG_UNK`。
         ⚠️ 实测（`bundle_menus_assets_all`）：本包 3 个 `EverguildLayoutGroup`
         （`Missions Tab/…/Mission Milestones Progress/…/steps` ×2、
         `…/Weekly Mission Container/…/steps`）**全是 `evenlySpaceinBetween = 1`** ——
         三件的 `alignment` 全 **0（Horizontal）**、各 4 个子件（70×70）、组自己**拉伸锚点**。
    · 其余子类（`FlexibleGridLayout` / `CurvedLayout` / `TableLayoutGroup` / …）**一律 None**：
      `FlexibleGridLayout` 是**另一套算法**（`LayoutGroup` 的直接子类，没有 isVertical）——
      `FlexibleGridLayout__CalculateLayoutInputHorizontal`（VA `0x180807E20`）里自己摸
      `rows`/`columns`/`itemSize`/`spacing` + 一张 `bool[,] grid` 打包表，逐个子件调
      `LayoutGroup.SetChildAlongAxis` 并回写本组 `sizeDelta`；而
      `CalculateLayoutInputVertical` / `SetLayoutHorizontal` / `SetLayoutVertical`
      **全是空壳**（三处共用同一个 RVA `0x4B33B0`，反汇编是裸 `ret`）⇒ 与 HOVLG / Grid 都不是一回事。
    """
    if cls.startswith('VerticalLayout'):
        return True
    if cls.startswith('HorizontalLayout'):
        return False
    if cls == 'EverguildLayoutGroup':
        # 见上面 ③④：flag=0 ⇒ 与 HOVLG 逐位等价（敢用）；flag=1 ⇒ spacing 运行期算 ⇒ 算不出。
        if mb.get('evenlySpaceinBetween'):
            return None
        al = mb.get('alignment')
        if al in (0, 1):
            return al == 1
    return None


def _group_axis_totals(b, mono, rt, mb, axis, sidx, bidx, is_vertical, _seen=None, _guard=0):
    """一个**布局组自己的** `ILayoutElement` 三项（A110）—— uGUI
    `HorizontalOrVerticalLayoutGroup.CalcAlongAxis(axis, isVertical)`
    （`com.unity.ugui@*/Runtime/UGUI/UI/Core/Layout/HorizontalOrVerticalLayoutGroup.cs:96-141`，逐行照抄）：

    ```csharp
    float combinedPadding = (axis == 0 ? padding.horizontal : padding.vertical);
    bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
    bool useScale    = (axis == 0 ? m_ChildScaleWidth  : m_ChildScaleHeight);
    bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);
    float totalMin = combinedPadding, totalPreferred = combinedPadding, totalFlexible = 0;
    bool alongOtherAxis = (isVertical ^ (axis == 1));            // ← 异或
    for (每个 rectChildren) {
        GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
        if (useScale) { float sf = child.localScale[axis]; min *= sf; preferred *= sf; flexible *= sf; }
        if (alongOtherAxis) { totalMin = Max(min + combinedPadding, totalMin);   // ← 交叉轴：取 max
                              totalPreferred = Max(preferred + combinedPadding, totalPreferred);
                              totalFlexible = Max(flexible, totalFlexible); }
        else                { totalMin += min + spacing; totalPreferred += preferred + spacing;
                              totalFlexible += flexible; }                      // ← 主轴：求和
    }
    if (!alongOtherAxis && rectChildren.Count > 0) { totalMin -= spacing; totalPreferred -= spacing; }
    totalPreferred = Max(totalMin, totalPreferred);
    SetLayoutInputForAxis(totalMin, totalPreferred, totalFlexible, axis);
    ```

    返回 `(min, pref, flex, known)`；`known=False` = **有分量算不出**（字体度量 / 嵌套的 Grid 系 /
    `UIPrimitiveBase`）⇒ 调用方标 `LG_UNK`（**出声不出数**，不许当 0）。

    🔴 **为什么必须有它**（`项目任务.md` A110 / A106）：原来这一支一律 `LG_UNK` ⇒ **嵌套的布局组**
    （组里套组）总量算不出 ⇒ 父组拿不到子组的尺寸。实例 = 奖励窗的 `Normal Missions`：
    工具原来给 `Special Missions` 宽 **10.92**、`Daily Missions` 左 **384.9**，
    而照 uGUI 手算真值是 **347.64** / **781.41**（判据与全过程见
    `资料/普查产出_1005/块13_工具四件.md` §件3）。
    ⚠️ `alongOtherAxis` 是**异或**：`isVertical=true` 的组在 **axis 0** 上走「取 max」那一支
    —— 两条分支别写反（写反的表现是「纵横尺寸互换着算」，看着还挺像回事）。
    ⚠️ 递归**可能套多层**（组里套组里套组）⇒ `_seen` 防成环、`_guard` 防病态深链。
    ⚠️ 全程**局部单位**（`LayoutUtility` 的尺寸本来就是局部量，不带任何缩放）——
       与 `apply_layout_to_children` 那条「别拿屏幕框当局部尺寸」是同一件事的两半。
    """
    if _guard > 16:
        return 0.0, 0.0, 0.0, False
    _seen = set() if _seen is None else _seen
    gopid = rt.get('m_GameObject', {}).get('m_PathID') if isinstance(rt, dict) else None
    if gopid is not None and gopid in _seen:
        return 0.0, 0.0, 0.0, False                       # 成环（数据坏了）⇒ 别递归下去
    _seen = _seen | ({gopid} if gopid is not None else set())
    pid = rt.get('_pid') if isinstance(rt, dict) else None
    if pid is None:
        return 0.0, 0.0, 0.0, False                       # 反查不到 pid ⇒ 算不出（不许猜）
    kids = [b.rt.get(str(c)) for c in b.children(pid)]
    kids = [k for k in kids if k]
    # 🔴 与 `apply_layout_to_children` **同一套过滤** —— **只此一处**（`_rect_children`）：
    #    uGUI 建 `rectChildren` 就是那两个条件（`!activeInHierarchy` + `ILayoutIgnorer.ignoreLayout`），
    #    两处各写一份 = 迟早不一致（同一个 GO 在「它自己的总量」与「它排孩子」两条路上算出的集合必须相同）。
    kids, _grp_aih = _rect_children(b, mono, pid, kids)
    pad = mb.get('m_Padding', {}) or {}
    spacing = mb.get('m_Spacing', 0) or 0
    combined = (pad.get('m_Left', 0) + pad.get('m_Right', 0)) if axis == 0 \
        else (pad.get('m_Top', 0) + pad.get('m_Bottom', 0))
    ctrl = bool(mb.get('m_ChildControlWidth' if axis == 0 else 'm_ChildControlHeight'))
    use_scale = bool(mb.get('m_ChildScaleWidth' if axis == 0 else 'm_ChildScaleHeight'))
    fexp = bool(mb.get('m_ChildForceExpandWidth' if axis == 0 else 'm_ChildForceExpandHeight'))
    along_other = bool(is_vertical) != (axis == 1)        # `isVertical ^ (axis == 1)`
    tot_min = tot_pref = combined
    tot_flex = 0.0
    known = True
    for k in kids:
        mn, pf, fx, u = _child_sizes(b, mono, k, axis, ctrl, fexp, sidx, bidx, _seen, _guard + 1)
        known = known and not u
        if use_scale:
            sf = _axis_scale(k, axis, use_scale)
            mn *= sf
            pf *= sf
            fx *= sf
        if along_other:
            tot_min = max(mn + combined, tot_min)
            tot_pref = max(pf + combined, tot_pref)
            tot_flex = max(fx, tot_flex)
        else:
            tot_min += mn + spacing
            tot_pref += pf + spacing
            tot_flex += fx
    if not along_other and kids:
        tot_min -= spacing
        tot_pref -= spacing
    tot_pref = max(tot_min, tot_pref)
    return tot_min, tot_pref, tot_flex, known


def _ilayout_cells(b, mono, rt, which, axis, sidx=None, bidx=None, _seen=None, _guard=0):
    """这个 RT 上全部 `ILayoutElement` 摊成 `[(优先级, 值|LG_SKIP|LG_UNK)]`（**`m_Component` 序**）。

    `which` ∈ `{'min','pref','flex'}`、`axis` ∈ `{0,1}`（0 = 宽）。逐类的取值与出处见上面那张表。
    `sidx`/`bidx` = sprite 的 `pid→名字` / `名字→尺寸·九宫格` 两张索引 —— **只有 `Image` 的
    `pref` 要用它们**（A109）；传 `None`（或 `--no-sprite` 的空表）时那一支退回 `LG_UNK`。
    `_seen`/`_guard` 只给**布局组递归**用（A110），别的地方不用传。
    """
    key = 'Width' if axis == 0 else 'Height'
    field = {'min': 'm_Min', 'pref': 'm_Preferred', 'flex': 'm_Flexible'}[which] + key
    tmp_field = {'min': 'm_min', 'pref': 'm_preferred', 'flex': 'm_flexible'}[which] + key
    out = []
    for _cp, cls, mb in components_of(b, mono, rt):
        k = set(mb.keys())
        # ---- `LayoutElement` 自己（+ `UIPreferedSizeFixer`：**代理到它指的那一颗**）----
        if cls in LGE_PROXY:
            # 判据 = 反汇编 `UIPreferedSizeFixer.get_{min,preferred,flexible}{Width,Height}` /
            #        `get_layoutPriority`（VA 0x1808724E0/0x180872510/0x180872540/0x1808725A0）——
            #        四条形状完全一样：`rcx = [this+0x30]`（= 序列化字段 `layoutElement`）
            #        再 `jmp` 它的 vtable 槽 ⇒ **它就是把另一个 LayoutElement 转发出来的空壳**。
            tgt = str((mb.get('layoutElement') or {}).get('m_PathID', 0))
            sub = MR.load(b.path, 'MonoBehaviour', tgt) if tgt not in ('0', '') else None
            if sub is None:
                out.append((1 << 20, LG_UNK))       # 指不到 ⇒ 当成最高优先级的不确定件（保守）
                continue
            cls, mb = 'LayoutElement', sub
        if cls in LGE_IDENTITY or ('m_Min' + key in k and 'm_Preferred' + key in k):
            # 🔴 `LayoutUtility.GetLayoutProperty:154`：`if (layoutComp is Behaviour &&
            #    !((Behaviour)layoutComp).isActiveAndEnabled) continue;` ⇒ **`m_Enabled=0` 的一律不看**。
            #    ⚠️ 实据：`bundle_menus_assets_all` 有 **18 个** `m_Enabled=0` 的 `LayoutElement`
            #       带着**有效值**（`m_PreferredWidth = 5000 / 5001.38`、`m_MinWidth = 48.83`），
            #       宿主是 `Individual rating value`（16 个）与 `Edit Name Button`（2 个），
            #       父组是 **`ctrlW=1` 的 HLG** ⇒ 不跳的话 `tot_pref` 被 5000 撑爆、整组的
            #       `minMaxLerp` / `surplusSpace` 全错（实测 5 窗口/19 节点、最大 163.19px）。
            #    ⚠️ **这一条【只】在这里跳** —— `LayoutGroup` 收子件时那个 `ILayoutIgnorer`
            #       循环**不看 `m_Enabled`**（`LayoutGroup.cs:60-79` 是裸读 `ignoreLayout`）
            #       ⇒ `_ignores_layout()` **保持不动**（两处规则不同，别合并）。
            if mb.get('m_Enabled', 1) == 0:
                continue
            v = mb.get(field, -1.0)
            out.append((int(mb.get('m_LayoutPriority', 1)), LG_SKIP if (v is None or v < 0) else v))
        # ---- TMP 系 / `Text`：min 是 0、flex 是 -1（跳过），pref 要字体度量 ----
        elif cls in LGE_TMP or ('m_text' in k and 'm_fontAsset' in k):
            # 🔴 **2026-10-06 修一处静默错（调度台裁定）**：TMP 系的
            #    `m_minWidth/m_preferredWidth/m_flexibleWidth`（+ Height 三个）**根本不在序列化里**
            #    （实测本包 35014 个 MB **一个都没有**）⇒ 原来那行 `v = mb.get(tmp_field, 0.0)`
            #    对 **min/pref/flex 三个属性一律**产出 `(0, 0.0)` ⇒ `LayoutUtility` 读到的是
            #    「**pref = 0、确定**」，与上一张表自己写的「pref 要字体度量 ⇒ 算不出」**互相矛盾**。
            #    后果是**静默错**（不是保守）：`ctrl=1` 的组按「文字宽 0」算总量；挂在 TMP 上的
            #    `ContentSizeFitter` 还会**回写 `sizeDelta = 0`**（实测 `⚙CSF h:PreferredSize=0`）。
            #    ⇒ 现在：`flex` 恒 `LG_SKIP`（`TMP_Text.flexibleWidth = -1`）、
            #       **字段缺席时的 `pref` 标 `LG_UNK`**（要字体度量，本工具算不出）、
            #       `min` 仍取 0（`TMP_Text.minWidth = 0`，与表一致）。
            #    ⚠️ 字段**真的序列化了**（非本包）就照用它的值 —— 判据是「键在不在」，不是「值是不是 0」。
            v = mb.get(tmp_field, 0.0)
            if which == 'flex':
                out.append((0, LG_SKIP))
            elif which == 'pref' and tmp_field not in k:
                out.append((0, LG_UNK))
            else:
                out.append((0, LG_SKIP if (v is None or v < 0) else v))
        elif cls in LGE_TEXT or 'm_FontData' in k:
            out.append((0, 0.0 if which == 'min' else (LG_SKIP if which == 'flex' else LG_UNK)))
        # ---- `Image` 系：min 0；pref 有 sprite ⇒ **按公式算**（A109）；无 sprite ⇒ 0；flex -1 ----
        elif cls in LGE_IMAGE or ('m_Sprite' in k and 'm_FillCenter' in k):
            if which == 'flex':
                out.append((0, LG_SKIP))
            elif which == 'min':
                out.append((0, 0.0))
            else:
                v, why = image_pref_size(mb, sidx, bidx)
                if v is None:
                    out.append((0, LG_UNK))         # 算不出（索引缺 / 名字对不上）⇒ 出声不出数
                else:
                    out.append((0, v[axis]))
        # ---- LayoutGroup 系：三个量 = **本组自己的内容总量**（A110，递归算）----
        elif cls in LGE_GROUP or ('m_Padding' in k and ('m_Spacing' in k or 'm_ChildAlignment' in k)):
            iv = _lg_is_vertical(cls, mb)
            if iv is None or 'm_CellSize' in k:
                # 判不出主轴（不认识的子类）/ Grid 系（网格算法与 HOVLG 不同，本工具不硬套）
                out.append((0, LG_UNK))
            else:
                t = _group_axis_totals(b, mono, rt, mb, axis, sidx, bidx, iv, _seen, _guard)
                v = {'min': t[0], 'pref': t[1], 'flex': t[2]}[which]
                out.append((0, v if t[3] else LG_UNK))
        elif 'm_CellSize' in k:
            out.append((0, LG_UNK))
        # ---- `ScrollRect` 系：priority = -1 且四个量全是 -1 ⇒ 每个属性都 `continue` ----
        elif cls in LGE_SCROLL or {'m_Content', 'm_Viewport'} <= k:
            out.append((-1, LG_SKIP))
        elif cls in LGE_PRIM:
            out.append((0, LG_UNK))
        elif cls in LGE_INPUT:
            # `InputField.cs:3437/3461` ⇒ minW=**5**、minH=**0**；`TMP_InputField.cs:4697/4729`
            # ⇒ minW=minH=**0**；两者 flex 都是 -1、priority 都是 **1**、pref 都要字体度量。
            # ⚠️ `EverguildInputField : TMP_InputField` ⇒ 走 TMP 那一档（minW 也是 0）。
            if which == 'flex':
                out.append((1, LG_SKIP))
            elif which == 'min':
                out.append((1, 5.0 if (cls == 'InputField' and axis == 0) else 0.0))
            else:
                out.append((1, LG_UNK))
    # 🔴 **兜底（2026-10-06）**：任何一格算出 `None` ⇒ 一律改标 `LG_UNK`。
    #    `None` 传出去会在 `layout_property` 里变成「值 = None」，再在 `if pf < mn` 处炸 ——
    #    而**炸的是调用方的行号**，看不出是谁产的（当天真踩过一次：`image_pref_size` 那四个
    #    unknown 分支把返回形状写错了）。这里不改数值，只保证「要么是数、要么是标记」。
    return [(p, LG_UNK if c is None else c) for (p, c) in out]


def _child_sizes(b, mono, k, axis, ctrl, fexp, sidx=None, bidx=None, _seen=None, _guard=0):
    """uGUI `HorizontalOrVerticalLayoutGroup.GetChildSizes` 的逐行对照
    （`com.unity.ugui@*/Runtime/UGUI/UI/Core/Layout/HorizontalOrVerticalLayoutGroup.cs:221-239`）：

    ```csharp
    if (!controlSize) { min = child.sizeDelta[axis]; preferred = min; flexible = 0; }
    else { min = LayoutUtility.GetMinSize(child, axis);
           preferred = LayoutUtility.GetPreferredSize(child, axis);
           flexible = LayoutUtility.GetFlexibleSize(child, axis); }
    if (childForceExpand) flexible = Mathf.Max(flexible, 1);     // ← 在 if/else【外面】
    ```

    🔴 **2026-10-05 补两条**（A60② / 十二①，原来都缺）：
      · **`:234` `LayoutUtility.GetFlexibleSize`** = `LayoutElement.m_FlexibleWidth/Height`
        （`defaultValue=0` + **负值忽略** ⇒ 序列化默认 `-1` 当 **0**）。
        原来 `ctrl=1` 分支直接返回 `1.0 if fexp else 0.0` ⇒ **`fexp=0` 时卡上声明的 flexible 被丢掉**。
      · **`flexible = Max(flexible, 1)` 与 `ctrl` 无关**（在 if/else 外面）⇒
        **`m_ChildControl*=0` 且 `m_ChildForceExpand*=1` 时 `flexible` 是 `1`**，原来给 `0`。
        ⚠️ 这一条**会改大面积输出**（实测 W2 32 窗口/351 节点、W3 29 窗口/376 节点）——
        `tot_flex` 由 0 变 >0 ⇒ `GetStartOffset` 那条**对齐偏移整条消失**、每格再按 `flexible×fmul` 撑开。
        影响面逐节点表 → `资料/普查产出_1004/menu_dump_影响面_1005.md`。
      · 🔴 **第三条（顺带查出来的，简报只点了前两条）**：`LayoutUtility.GetLayoutProperty`
        **跳过 `m_Enabled=0` 的组件** ⇒ 见下面 `if mb.get('m_Enabled', 1) == 0: continue` 的注释。

    ⚠️ **诚实边界**：主轴 `childControl*=1` 时，mins/pref 要走 `LayoutUtility` ——
       **文字的首选尺寸要 Unity 的字体度量，这里算不出**；嵌套布局件、**有图 `Image` 的首选宽高**同理。
       遇到就 `unknown=True`，表里给那几个子节点的结果打 `?`，**不瞎填**。
       ⚠️ `ctrl=0` 那支**不需要**字体度量（只用 `sizeDelta`）⇒ 照 uGUI 不打 `?`。

    🔴 **2026-10-05 第四条（本次）：`layoutPriority` 的规则** ——
       原来按 `m_Component` 序「后写覆盖」，现在走 `layout_property()`（**优先级循环**）。
       ⚠️ 值的**变化面很小**：`bundle_menus_assets_all` 实测只有 **1 个 GO 挂了 ≥2 颗
       `LayoutElement`**（`Title` 那个：两颗都是优先级 1、都只声明了高度，同级取 max ⇒ 结果同值；
       1333 颗里 1 颗 `m_LayoutPriority=10`、其余全是 1）。
       ⚠️ 但「**算不算得准**」那一格变化大：TMP 系的 `min=0`/`flex=-1` 是**确定**的，
       只有 `pref` 要字体度量 ⇒ 一个声明全了 min/pref/flex 的 `LayoutElement`（优先级 1）
       盖住同级 0 的 TMP 之后，**整件由「算不准」变成「算得准」**（原来是一刀切标 `?`）。
    """
    if not ctrl:
        v = k['m_SizeDelta']['x'] if axis == 0 else k['m_SizeDelta']['y']
        mn = pf = v
        fx = 0.0
        unknown = False
    else:
        # `LayoutUtility.GetPreferredSize` = `Mathf.Max(GetMinSize, GetLayoutProperty(pref))`
        # ⇒ **两条各自独立的优先级循环** ⇒ `pref` 的「确定」= 两条都确定。
        mn, ok_mn = layout_property(_ilayout_cells(b, mono, k, 'min', axis, sidx, bidx, _seen, _guard))
        pf, ok_pf = layout_property(_ilayout_cells(b, mono, k, 'pref', axis, sidx, bidx, _seen, _guard))
        fx, ok_fx = layout_property(_ilayout_cells(b, mono, k, 'flex', axis, sidx, bidx, _seen, _guard))
        if pf < mn:
            pf = mn
        unknown = not (ok_mn and ok_pf and ok_fx)
    if fexp:                          # `:237-238` —— 在 if/else【外面】，与 `ctrl` 无关
        fx = max(fx, 1.0)
    return mn, pf, fx, unknown


def _axis_scale(rt, axis, use_scale):
    """子件在 `axis` 上的 `m_LocalScale` —— uGUI 的 `scaleFactor = useScale ? child.localScale[axis] : 1f`
    （`HorizontalOrVerticalLayoutGroup.CalcAlongAxis` / `SetChildrenAlongAxis`）。

    `use_scale` = 布局组的 `m_ChildScaleWidth`（axis 0）/ `m_ChildScaleHeight`（axis 1）。
    ⚠️ 导出 JSON 里**大多数 RT 不带 `m_LocalScale`** ⇒ 读不到就当 1（不是错误）。
    """
    if not use_scale:
        return 1.0
    s = rt.get('m_LocalScale') or {}
    return s.get('x' if axis == 0 else 'y', 1.0)


# ================================================================ 自适配组件（ILayoutSelfController）
# 🔴 **2026-10-05 新增**（A60④）：`ContentSizeFitter`（本包 795 颗）与 `AspectRatioFitter`（2136 颗）
#    都是 **`ILayoutSelfController`** —— 它们**直接改写自己这个 RectTransform 的尺寸**，
#    本文件在此之前**一个字都不读** ⇒ 凡是挂了它们的节点，表里的尺寸是**模板值**，
#    而且**不带任何提示**（同族第三次：`m_ReverseArrangement` / `m_Enabled=0` / 这一对）。
#
# 次序（uGUI `LayoutRebuilder.PerformLayoutControl`，逐行）：
#    对每个 RectTransform 顶部向下 —— ① 先跑它自己的 `ILayoutSelfController`；
#    ② 再跑剩下的 controller（= 它自己的 `LayoutGroup`）；③ 然后递归进子节点。
#    ⇒ 「**父组先排我 → 我再自适配 → 我再排我的孩子**」。`walk` 就是前序 ⇒ 本函数放在
#    `walk` **算自己矩形之前**调一次，次序与 uGUI 完全一致。
#    ⚠️ `ContentSizeFitter` 的目标值**只依赖它自己的内容**（`LayoutUtility.Get*Size(m_Rect)`），
#       **不依赖自己的尺寸** ⇒ 与「跑几轮收敛」无关；`AspectRatioFitter` 的模式 1/2 依赖
#       `rectTransform.rect`（父组给我的尺寸）⇒ 也必须排在父组之后。两件都自洽。
#    ⚠️ 唯一的单趟/收敛差异：父组 `ctrl=0` 时父用的 `cell` 是**模板 `sizeDelta`**、不是回写后的值。
#       本包实测这一类点两者的数值相同（`--verify-layout` ⑦ 钉了 `ChatPanel` 那一处），**不修**。
FIT_MODE = {0: '不约束', 1: 'MinSize', 2: 'PreferredSize'}
ASPECT_MODE = {0: 'None', 1: '宽控高', 2: '高控宽', 3: 'FitInParent', 4: 'EnvelopeParent'}


def _set_size_axis(rt, axis, size, parent_local):
    """`RectTransform.SetSizeWithCurrentAnchors(axis, size)` —— 让 `rect.size[axis] == size`。

    判据 = `AspectRatioFitter.GetSizeDeltaToProduceSize`（`AspectRatioFitter.cs:196-199`）：
        `size - GetParentSize()[axis] * (anchorMax[axis] - anchorMin[axis])`
    —— 与 `rect.size = sizeDelta + parentSize ⊙ (anchorMax − anchorMin)` **互为逆运算**
    （两条式子都出自同一个文件，互为印证）。
    ⚠️ `parent_local` 必须是**父的局部尺寸**（`GetParentSize()` = `parent.rect.size`），
       不是父在屏幕上的像素框 —— 传错就差一整个 `lossyScale`。
    """
    p = 'x' if axis == 0 else 'y'
    rt['m_SizeDelta'][p] = size - parent_local[axis] * (rt['m_AnchorMax'][p] - rt['m_AnchorMin'][p])


def _size_delta_for(size, psize, a_min, a_max):
    """`GetSizeDeltaToProduceSize` / `SetSizeWithCurrentAnchors` 的**纯函数**版：
    要让 `rect.size[axis] == size`，`sizeDelta[axis]` 该是多少（`psize` = 父的**局部**尺寸）。"""
    return size - psize * (a_max - a_min)


def aspect_fields(mode, ar, rect_local, plocal, a_min, a_max):
    """`AspectRatioFitter.UpdateRect`（`AspectRatioFitter.cs:119-176`）的**纯函数**版。

    返回 `None`（模式 0 = 什么都不做）或一个 dict，键是**它要改的字段**：
      · 模式 1/2 ⇒ 只含 `'sd_x'` 或 `'sd_y'`（**只写一个轴**，另一个轴原样不动）；
      · 模式 3/4 ⇒ 含 `'amin','amax','pos','sd_x','sd_y'`（四个量全写）。
    `rect_local` = 本件当前的 `rectTransform.rect.size`（模式 1/2 要读它）；
    `plocal` = `GetParentSize()`（模式 3/4 要读它）；
    `a_min`/`a_max` = 本件**当前**的锚点（模式 3/4 里被先铺成 (0,0)/(1,1) 再算 ⇒ 传铺好的那对）。

    手算 fixture（见 `verify_layout()` ⑧）：
      · `(2, 4.0, (456.9613,93.5475), plocal=(5000,1000), amin=(0,1), amax=(0,1))`
        → 写【宽】：`sd_x = 93.5475×4 − 5000×(0−0) = 374.19`
      · `(1, 2.0, (400,50), plocal=(1000,600), amin=(0,0), amax=(1,1))`
        → 写【高】：`sd_y = 400/2 − 600×(1−0) = −400`
      · `(3, 1.0, …, plocal=(800,600), amin=(0,0), amax=(1,1))` → `FitInParent`：
        `(600×1 < 800) ^ True = False` ⇒ `sd = (600×1−800, 0) = (−200, 0)`
        ⇒ 局部尺寸 `(600,600)` = **内接**在 800×600 里 ✔
      · `(4, 1.0, …, plocal=(800,600), amin=(0,0), amax=(1,1))` → `EnvelopeParent`：
        `(600 < 800) ^ False = True` ⇒ `sd = (0, 800/1−600) = (0, 200)`
        ⇒ 局部尺寸 `(800,800)` = **外接**（含住父）✔
      ⚠️ 模式 1 读 `rect.height`、2 读 `rect.width` —— **别按「名字里的方向」顺手反了**
        （`UpdateRect` 里 `HeightControlsWidth ⇒ SetSizeWithCurrentAnchors(Horizontal, rect.height × ar)`）。
    """
    if mode == 0:
        return None
    if mode in (1, 2):
        if mode == 2:                       # HeightControlsWidth ⇒ 写【宽】
            return {'sd_x': _size_delta_for(rect_local[1] * ar, plocal[0], a_min[0], a_max[0])}
        return {'sd_y': _size_delta_for(rect_local[0] / ar if ar else 0.0,
                                        plocal[1], a_min[1], a_max[1])}
    # 模式 3 / 4：先把锚点铺满父，再按 ratio 定 sizeDelta（另一轴写 0）
    # ⚠️ `_size_delta_for(size, psize, a_min, a_max)` 的第 3/4 参是**锚点的 min 和 max**
    #    （铺好之后是 `0.0` 与 `1.0`，`a_max − a_min = 1`）—— **别顺手写成 `(1.0, 0.0)`**，
    #    那样差值变成 −1、整个式子反号（2026-10-05 当场踩过一次，7 个窗口 22 处输出被改坏）。
    if (plocal[1] * ar < plocal[0]) ^ (mode == 3):
        return {'amin': (0.0, 0.0), 'amax': (1.0, 1.0), 'pos': (0.0, 0.0), 'sd_x': 0.0,
                'sd_y': _size_delta_for(plocal[0] / ar if ar else 0.0, plocal[1], 0.0, 1.0)}
    return {'amin': (0.0, 0.0), 'amax': (1.0, 1.0), 'pos': (0.0, 0.0), 'sd_y': 0.0,
            'sd_x': _size_delta_for(plocal[1] * ar, plocal[0], 0.0, 1.0)}


def _csf_clamp(mb, rt):
    """`ContentSizeFitterMinMax.ClampSize()`（A107）—— **逐行对照反汇编**（VA `0x1807FF080`）。

    ```csharp
    Vector2 sd = rectTransform.sizeDelta;                    // ← 读的是【sizeDelta】，不是 rect.size
    if (clampWidth)  sd.x = Mathf.Clamp(sd.x, widthMin, widthMax);
    if (clampHeight) sd.y = Mathf.Clamp(sd.y, heightMin, heightMax);
    rectTransform.sizeDelta = sd;
    ```
    反汇编的形状（`Mathf.Clamp` 的两个分支都在）：
    `if (clampWidth && widthMin <= sd.x) { t = sd.x; if (widthMax < sd.x) t = widthMax; }`
    —— 第一个条件**不成立**时 `t` 已被赋成 `widthMin`（Ghidra 把赋值写在条件里）⇒ 就是 Clamp。
    ⚠️ 两个轴**都**钳（`SetLayoutHorizontal` 与 `SetLayoutVertical` 各自跑完 base 后都调它）。
    ⚠️ 钳的是 **`sizeDelta`**（局部），不是 `rect.size` —— 与 `RectSizeLimiter` **不是一回事**。
    返回说人话的短标签（没钳到就返回空）。"""
    out = []
    for axis, ck, mnk, mxk, an in ((0, 'clampWidth', 'widthMin', 'widthMax', 'w'),
                                   (1, 'clampHeight', 'heightMin', 'heightMax', 'h')):
        if not mb.get(ck):
            continue
        p = 'x' if axis == 0 else 'y'
        v = rt['m_SizeDelta'][p]
        lo, hi = mb.get(mnk, 0.0), mb.get(mxk, 0.0)
        new = lo if v < lo else (hi if v > hi else v)         # `Mathf.Clamp` 逐行（先 min 后 max）
        if new != v:
            rt['m_SizeDelta'][p] = new
            out.append(f'CSFMinMax {an}: clamp→{new:g}（[{lo:g},{hi:g}]，原 {v:g}）')
    return out


def usf_scale(mb, plocal, own_local):
    """`UIScaleToFit.Fit()` 里**真正起作用的那一步**（A107）—— 纯函数，返回 `(ratio, why)`。

    判据 = 反汇编 VA `0x180872B00`（RVA `0x872B00`）+ `dump.cs:114751-114796` 的字段表：
    ```csharp
    if (targetRect == null) { Debug.LogWarning(...); return; }
    Vector2 t;                                                // → 目标尺寸
    if (paddingType == Flat /*0*/) t = targetRect.rect.size + padding;
    else                          t = Vector2.Scale(targetRect.rect.size, padding);   // Multiplier=1
    Rect r = rect.rect;                                       // ← 本件自己的**局部**尺寸
    float ratio = Mathf.Min(t.x / r.width, t.y / r.height);    // `divss` + `minss`
    if (float.IsNaN(ratio) || float.IsInfinity(ratio)) return; // 两个 `btr/cmp` 有限性检查
    if (Mathf.Approximately(ratio, 0f)) return;                // ⇒ **约等于 0 就一个字都不改**
    transform.localScale = new Vector3(ratio, ratio, ratio);   // 三个分量**全写**（`movss` 三次）
    ```
    ⚠️ 「约等于 0」按 Unity 的 `Mathf.Approximately(a,b) = |b-a| < Max(1e-6·Max(|a|,|b|), Epsilon·8)`
       —— 反汇编里那两条常数就是 `1e-6`（`0x1834B2DB8`）与 `Mathf.Epsilon × 8`
       （`0x1834B2E00` = 8 乘上 `Mathf` 的静态 `Epsilon`；`0x18424F4F8` 就是 `Mathf` 的 TypeInfo 槽）。
       `|ratio| < 1e-6·|ratio|` 对任何非零值都不成立 ⇒ **实际等价于「`ratio == 0` 才跳过」**。
    ⚠️ `own_local`（本件局部尺寸）任一维为 0 ⇒ 除法给 `Inf/NaN` ⇒ 前面那两个有限性检查会 return
       （**原版也什么都不写**）—— 所以这里返回 `(None, '本件局部尺寸有 0 ⇒ 原版也算不出（Inf/NaN）')`。
    ⚠️ `targetRect`：`fitInParent=1` 时 = **父件的 RectTransform**（`Awake` 里 `transform.parent.GetComponent`）；
       `fitInParent=0` 时 = **序列化字段指的那一件**（任意节点）⇒ 本工具**只算前者**
       （后者要跨子树算别人的矩形，且那个别人自己的布局组也得先跑过 ⇒ 不在本工具能力内，
       如实报「算不出」，⛔ 不猜）。本包 183 个**全是 `fitInParent=1`**。
    """
    if not mb.get('fitInParent', 0):
        return None, 'fitInParent=0 ⇒ 目标件是序列化字段指的那一件（本工具不算跨子树矩形）'
    if plocal is None or own_local is None:
        return None, '祖先缩放≈0 ⇒ 父件局部尺寸恢复不出'
    pad = mb.get('padding') or {}
    px, py = pad.get('x', 0.0), pad.get('y', 0.0)
    if int(mb.get('paddingType', 0)) == 0:            # `Flat`
        tw, th = plocal[0] + px, plocal[1] + py
    else:                                             # `Multiplier`
        tw, th = plocal[0] * px, plocal[1] * py
    if own_local[0] == 0 or own_local[1] == 0:
        return None, '本件局部尺寸有 0 ⇒ 原版也算不出（Inf/NaN）'
    r = min(tw / own_local[0], th / own_local[1])
    if not math.isfinite(r):
        return None, '原版的有限性检查会 return'
    if r == 0.0:                                      # 见 docstring：`Approximately(ratio, 0)` 实际等价于此
        return None, 'ratio == 0 ⇒ 原版不施加（`Mathf.Approximately` 那一关）'
    return r, ''


def apply_self_fitters(b, mono, rt, parent_rect, scale, sidx=None, bidx=None, keep_scales=True):
    """把 `ILayoutSelfController` 那几件的**回写**做掉（**就地改 `rt`**）。

    返回 `(notes, est)`：`notes` = 人类可读的短标签列表（表里印出来），
    `est` ∈ `{None, 'ok', 'unk'}` —— `'unk'` = **这件挂了自适配组件、但目标值算不出**
    （文字度量之类）⇒ 表里它自己的尺寸**仍然是模板值**，别照抄。

    🔴 **2026-10-06 补齐 4 个实现者**（A107）。判据 = 原版 dump 里 `ILayoutSelfController`
    的**传递闭包**（一共有 6 个类），逐类核过：
    | 类 | 本包实例 | 建模 |
    |---|---|---|
    | `AspectRatioFitter` | 2136 | ✅ 本函数（2026-10-05） |
    | `ContentSizeFitter` | 795 | ✅ 本函数（2026-10-05） |
    | **`ContentSizeFitterMinMax`**（`: ContentSizeFitter`，`dump.cs:104095`） | **87** | ✅ 本次：base 的 CSF 回写 **+ `ClampSize()`**（见 `_csf_clamp`） |
    | **`RectSizeLimiter`**（`dump.cs:113121`） | **2** | ✅ 本次：把**自己的 `rect.size`** 钳进 `[m_minSize, m_maxSize]` |
    | **`UIScaleToFit`** | **183** | ✅ 本次：算比值**改写自己的 `localScale`**（见 `usf_scale`） |
    | `TileSizeFitter` | **0** | 只留一条注释（没有实例，抓不到真数据；补了也没法验） |

    逐行对照：

    **`ContentSizeFitter`**（`ContentSizeFitter.cs:83-104`）
    ```csharp
    if (fitting == MinSize)       SetSizeWithCurrentAnchors(axis, LayoutUtility.GetMinSize(m_Rect, axis));
    else /*PreferredSize*/        SetSizeWithCurrentAnchors(axis, LayoutUtility.GetPreferredSize(m_Rect, axis));
    ```
    每个轴独立（`m_HorizontalFit` / `m_VerticalFit`，`0 = Unconstrained` 那一档**什么都不做**）。

    **`ContentSizeFitterMinMax`**（反汇编 VA `0x1807FF140/0x1807FF160/0x1807FF080`）
    ```csharp
    public override void SetLayoutHorizontal() { base.SetLayoutHorizontal(); ClampSize(); }
    public override void SetLayoutVertical()   { base.SetLayoutVertical();   ClampSize(); }
    ```

    **`RectSizeLimiter`**（反汇编 VA `0x18084FA10`（横）/`0x18084FB00`（纵））
    ```csharp
    if (m_maxSize.x > 0 && rectTransform.rect.width  > m_maxSize.x) SetSizeWithCurrentAnchors(Horizontal, m_maxSize.x);
    if (m_minSize.x > 0 && rectTransform.rect.width  < m_minSize.x) SetSizeWithCurrentAnchors(Horizontal, m_minSize.x);
    // 纵轴同形（读 rect.height，写 Vertical）
    ```
    ⚠️ 它读的是 **`rectTransform.rect`（局部尺寸）**、写的是 `SetSizeWithCurrentAnchors`
    —— **不是** `sizeDelta`（与 `ContentSizeFitterMinMax` 恰好相反，别混）。

    **`UIScaleToFit`**（反汇编 VA `0x180872B00`）—— 见 `usf_scale()`。
    🔴 **注意它的 `SetLayoutHorizontal/Vertical` 不是空壳**：VA `0x180872E30` 的字节是
    `xor edx,edx; jmp 0x180872b00`（= **尾跳进 `Fit()`**，`dump.cs` 里它还和 `Start`/`OnEnable`
    共用同一个 RVA —— il2cpp 把**函数体完全一样**的这几个方法合并了）。
    ⇒ 走一次布局就会跑一次 `Fit()`，`localScale` 是**运行期算出来的**，序列化值不是运行时值。

    **`AspectRatioFitter`**（`AspectRatioFitter.cs:119-176` 的 `UpdateRect`）
    ```csharp
    case HeightControlsWidth: SetSizeWithCurrentAnchors(Horizontal, rect.height * m_AspectRatio);   // 模式 2
    case WidthControlsHeight: SetSizeWithCurrentAnchors(Vertical,   rect.width  / m_AspectRatio);   // 模式 1
    case FitInParent: case EnvelopeParent:            // 模式 3 / 4
        anchorMin = (0,0); anchorMax = (1,1); anchoredPosition = (0,0);
        if ((parentSize.y * aspectRatio < parentSize.x) ^ (mode == FitInParent))
             sizeDelta = (0, parentSize.x / aspectRatio - parentSize.y);
        else sizeDelta = (parentSize.y * aspectRatio - parentSize.x, 0);
    ```
    ⚠️ 模式 1/2 **只写一个轴的 `sizeDelta`**（另一个轴原样不动）；
       模式 3/4 **四个量全写**（`Anchors | AnchoredPosition | SizeDeltaX | SizeDeltaY`）。
    ⚠️ `m_AspectMode == 0 (None)` ⇒ `UpdateRect` 里那个 `case` 是空的（只在编辑器里回填 ratio）。
    ⚠️ `m_Enabled == 0` 的一律不跑（`AspectRatioFitter.UpdateRect` 头一句是 `if (!IsActive() ...) return;`，
       `UIBehaviour.IsActive()` = `isActiveAndEnabled`）。
    ⚠️ **`TileSizeFitter`（0 个实例）没接**：它 `UpdateRect` 的判据（`m_Border`/`m_TileSize` 与
       父尺寸求整数倍）**没有实例可验**，接了也只能靠手算对不上真数据 —— 如实留着，
       哪天真出现实例再补（⛔ 不是「影响小不做」，是「判据差最后一块、且本包无从验」）。
    """
    notes = []
    est = None
    # 父的**局部**尺寸（= `GetParentSize()`）。`lossyScale(父) == 0` 时**恢复不出**来 ⇒ 见 `parent_local_size`
    ploc = MR.parent_local_size(parent_rect, scale)
    for _cp, cls, mb in components_of(b, mono, rt):
        if mb.get('m_Enabled', 1) == 0:
            continue
        if cls in ('ContentSizeFitter', 'ContentSizeFitterMinMax') \
                or {'m_HorizontalFit', 'm_VerticalFit'} <= set(mb.keys()):
            for axis, key, an in ((0, 'm_HorizontalFit', 'h'), (1, 'm_VerticalFit', 'v')):
                fit = mb.get(key, 0)
                if fit == 0:
                    continue
                which = 'min' if fit == 1 else 'pref'
                tgt, known = layout_property(_ilayout_cells(b, mono, rt, which, axis, sidx, bidx))
                if not known or ploc is None:
                    notes.append(f'CSF {an}:{FIT_MODE.get(fit, fit)}'
                                 + ('（祖先缩放≈0，父局部尺寸恢复不出）' if ploc is None else '？'))
                    est = 'unk'
                else:
                    _set_size_axis(rt, axis, tgt, ploc)
                    notes.append(f'CSF {an}:{FIT_MODE.get(fit, fit)}={tgt:g}')
            # A107：`ContentSizeFitterMinMax` = base 做完**再钳一次 `sizeDelta`**（两个轴都钳）
            if 'clampWidth' in mb or 'clampHeight' in mb:
                cl = _csf_clamp(mb, rt)
                notes.extend(cl or ['CSFMinMax（两个轴都没钳到）'])
        elif cls == 'AspectRatioFitter' or {'m_AspectMode', 'm_AspectRatio'} <= set(mb.keys()):
            mode = mb.get('m_AspectMode', 0)
            ar = mb.get('m_AspectRatio', 1.0) or 0.0
            if mode == 0:
                continue
            if ploc is None:
                notes.append(f'ARF {ASPECT_MODE.get(mode, mode)}（祖先缩放≈0，父局部尺寸恢复不出）')
                est = 'unk'
                continue
            # 模式 3/4 先把锚点铺成 (0,0)/(1,1)，再算 —— 所以传进去的就是铺好的那一对
            a_min = ((0.0, 0.0) if mode in (3, 4)
                     else (rt['m_AnchorMin']['x'], rt['m_AnchorMin']['y']))
            a_max = ((1.0, 1.0) if mode in (3, 4)
                     else (rt['m_AnchorMax']['x'], rt['m_AnchorMax']['y']))
            f = aspect_fields(mode, ar, MR.local_size(rt, parent_rect, scale), ploc, a_min, a_max)
            if 'amin' in f:
                rt['m_AnchorMin'] = {'x': f['amin'][0], 'y': f['amin'][1]}
                rt['m_AnchorMax'] = {'x': f['amax'][0], 'y': f['amax'][1]}
                rt['m_AnchoredPosition'] = {'x': f['pos'][0], 'y': f['pos'][1]}
                rt['m_SizeDelta'] = {'x': f['sd_x'], 'y': f['sd_y']}
            if 'sd_x' in f and 'sd_y' not in f:
                rt['m_SizeDelta']['x'] = f['sd_x']
            if 'sd_y' in f and 'sd_x' not in f:
                rt['m_SizeDelta']['y'] = f['sd_y']
            notes.append(f'ARF {ASPECT_MODE.get(mode, mode)}({ar:g})')
        elif cls == 'RectSizeLimiter' or {'m_maxSize', 'm_minSize'} <= set(mb.keys()):
            # 反汇编 VA 0x18084FA10 / 0x18084FB00（横 / 纵）—— 读 `rectTransform.rect`（**局部**）
            own = MR.local_size(rt, parent_rect, scale)
            mx, mn = mb.get('m_maxSize') or {}, mb.get('m_minSize') or {}
            if own is None or ploc is None:
                notes.append('RSL（祖先缩放≈0，局部尺寸恢复不出）')
                est = 'unk'
                continue
            hits = []
            for axis, p in ((0, 'x'), (1, 'y')):
                lo, hi = mn.get(p, 0.0), mx.get(p, 0.0)
                if hi > 0 and own[axis] > hi:
                    _set_size_axis(rt, axis, hi, ploc)
                    hits.append(f'{"wh"[axis]}→max {hi:g}')
                elif lo > 0 and own[axis] < lo:
                    _set_size_axis(rt, axis, lo, ploc)
                    hits.append(f'{"wh"[axis]}→min {lo:g}')
            notes.append('RSL ' + ('、'.join(hits) if hits else f'（{own[0]:g}×{own[1]:g} 在界内，没改）'))
        elif cls == 'UIScaleToFit' or {'fitInParent', 'paddingType'} <= set(mb.keys()):
            if not keep_scales:
                notes.append('USF（`--no-ancestor-scale` ⇒ 按旧口径不施加）')
                continue
            r, why = usf_scale(mb, ploc, MR.local_size(rt, parent_rect, scale))
            if r is None:
                notes.append(f'USF 未施加：{why}')
            else:
                s = rt.setdefault('m_LocalScale', {})
                s['x'] = s['y'] = r
                if 'z' in s:
                    s['z'] = r
                notes.append(f'USF ×{r:.6g}（**改写 `m_LocalScale`**：序列化值不是运行时值）')
    return notes, est


def _ignores_layout(b, mono, kid_rt):
    """这个孩子是不是**整件被排除**出父布局组的 `rectChildren`（`LayoutGroup.GetChildList`）。

    判据 = uGUI `Layout/LayoutGroup.cs:60-79`（逐行照抄）：
    ```csharp
    rect.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);
    if (toIgnoreList.Count == 0) { m_RectChildren.Add(rect); continue; }
    for (int j = 0; j < toIgnoreList.Count; j++)
        if (!((ILayoutIgnorer)toIgnoreList[j]).ignoreLayout) { m_RectChildren.Add(rect); break; }
    ```
    🔴 **口径修一处（2026-10-05，A60⑦）**：原来是「**任意**一颗 `LayoutElement` 说 ignore ⇒ 排除」，
       uGUI 是「**只要有一颗说不 ignore ⇒ 收进来**」（`for` 里 `break` 出去）。
       两者**只在「同一个节点挂了 ≥2 颗 `ILayoutIgnorer` 且标志不一致」时岔开**：
       · 全 True（含 1 颗）⇒ 两者都排除 ✔；
       · 全 False ⇒ 两者都收 ✔；
       · **混合** ⇒ uGUI **收**、改前**排**（改了）。
       ⚠️ 实测（`bundle_menus_assets_all`）：挂 ≥2 颗 `LayoutElement` 的 GO **只有 1 个**
       （`Title`：两颗都 `m_IgnoreLayout=0`）⇒ **落在岔开集合里的节点数 = 0**。
       改它是因为「规则要照抄源码」，不是为了修一个现存的错。
    ⚠️ 另有一处**故意的口径差**（不是缺口）：uGUI 收孩子时判的是
       `!rect.gameObject.activeInHierarchy`（**整条父链**都 active），
       而 `apply_layout_to_children` 那一侧判的是 `m_IsActive`（**只看孩子自己那一格**，
       因为本工具要的是「**激活之后**」的版面 —— 文件头那条口径）。
       ⇒ 一个「自己 active、祖先 inactive」的子件，uGUI 此刻会**跳过**、我们仍收进来。
       实测见 `--verify-layout` ⑦ 与 ⑮ 的说明（**「父组自己在跑」时两条规则的点数差 = 0**，
       即两者只在「组自己就不在跑」那一档岔开；那个档本工具在**组那一层**补偿）。
       🔴 **2026-10-12（A487）**：这一条以前被实现成「组不在跑 ⇒ **孩子一律不判**」，
       详见 `_rect_children` 的 docstring（真缺陷，已修）。
    ⚠️ **不看 `m_Enabled`** —— 与 `LayoutUtility.GetLayoutProperty:154` 那一条**故意不同**
       （`LayoutGroup` 收孩子时是裸读 `ignoreLayout`）。**两处规则不同，别合并。**
    """
    found = False
    for _cp, cls, mb in components_of(b, mono, kid_rt):
        if cls == 'LayoutElement':
            found = True
            if not mb.get('m_IgnoreLayout'):
                return False          # `break` 出去收进 `rectChildren`
    return found


def _rect_children(b, mono, rtpid, kids):
    """uGUI `LayoutGroup.CalculateLayoutInputHorizontal` 的 `rectChildren`（**规则只此一处**）。

    ```csharp
    for (int i = 0; i < rectTransform.childCount; i++) {
        var rect = rectTransform.GetChild(i) as RectTransform;
        if (rect == null || !rect.gameObject.activeInHierarchy) continue;      // ①
        rect.GetComponents(typeof(ILayoutIgnorer), toIgnoreList);
        if (toIgnoreList.Count == 0) { m_RectChildren.Add(rect); continue; }
        for (int j = 0; j < toIgnoreList.Count; j++)
            if (!((ILayoutIgnorer)toIgnoreList[j]).ignoreLayout) { m_RectChildren.Add(rect); break; }   // ②
    }
    ```
    返回 `(rectChildren, grp_aih)`；`grp_aih` = **这个组自己**在不在 `activeInHierarchy` 里 ——
    ⚠️ **只给调用方出声用**（行首 `⛔GRP-off` 那个标记 + 表尾那块清单），**不参与过滤**。

    🔴 **2026-10-12 修一处真缺陷（A487）：过滤判据只有一条 —— 孩子自己的 `m_IsActive`，与 `grp_aih` 无关。**
       ① 是照 uGUI 的字面量（整条父链）写的，而本工具**故意**要把「祖先 inactive 的组」也照样排
       （要的是「**激活之后**」的版面；`walk` 也走进 inactive 子树把它们列出来）——
       ⇒ 那一档下把**本组**那一格当 active 看，于是
       `activeInHierarchy(孩子) = 孩子自己 m_IsActive ∧ activeInHierarchy(本组) := 孩子自己 m_IsActive`。
       ⇒ **两种情形合并成同一条判据**（这也是改前的写法，A108 那次改坏了）。
       ⛔ **改前**写的是 `(not grp_aih) or (孩子 m_IsActive)` —— 组自己不在 `activeInHierarchy` 里时
       **把出厂 `F` 的孩子也收进 `rectChildren`**，于是那些件**既占了格、又把后面每一件都推走**。
       **错因**：「组不跑 ⇒ 整组当 active 看」说的是**本组那一格**，被误读成「孩子也一律当 active」
       （`apply_layout_to_children` 的 docstring 里那句推导本来就是对的，落地时走反了 —— 铁律 5·c）。
       实测（`AllianceMemberVariant>GeneralDetails>Content>Alliance Rating Display`，
       `Secondary Icon` 出厂 `F`、`Main Icon` / `Individual rating value` 在它右边）：
       改前 `Main Icon` **726.97..786.97** · 文本 **786.97..1099.96**（整排右移 **+113.58**）；
       改后 **613.38..673.38** · **673.38..1099.96** —— 与**序列化矩形逐值相同**
       （`--no-layout` 那一档 · `普查产出_0927/社交_联盟与好友页.md:271-278` 三份都对得上）。
    """
    grp_aih = _active_in_hierarchy(b, b.rt.get(str(rtpid)))
    # 🔴 **过滤判据只此一条**（A487，2026-10-12）：**孩子自己那一格**。
    #    ⛔ 别按 `grp_aih` 分档（改前那个 `(not grp_aih) or …` 就是 A487 那处缺陷本身）；
    #    ⛔ 也别写成 `_active_in_hierarchy(b, k)`（整条父链）—— 组自己不在跑时那会把**整组孩子全丢掉**，
    #       与「本工具照样排 inactive 子树（要的是【激活之后】的版面）」自相矛盾。
    #    推导：孩子是本组的**直接子件** ⇒ 它的祖先链 = **本组 + 本组的祖先**；
    #    「激活之后」= 只把**本组**那一格当 active ⇒ `activeInHierarchy(孩子) = 孩子自己 m_IsActive`。
    #    自检：`--verify-layout` ⑲（打桩三档：组在跑 / 组不在跑 / 整条链都在跑）。
    ks = [k for k in kids
          if (b.go.get(str(k.get('m_GameObject', {}).get('m_PathID'))) or {}).get('m_IsActive', 1)]
    return [k for k in ks if not _ignores_layout(b, mono, k)], grp_aih


def apply_layout_to_children(b, mono, rtpid, rect, scale, kids, sidx=None, bidx=None):
    """把布局组**直接子节点**的 anchor/pos/sizeDelta 就地改成「布局跑之后」的值。

    返回 (est, lgcls, lgmb)：est ∈ {'ok','unk','grid?','axis?','cust?','sp?'}（后四档 = **没排**，见下）。
    · `'ok'`   = 排了、且每个量都算得出；
    · `'unk'`  = **排了**、但子件里有算不出的量（文字度量 / 嵌套的 Grid 系 …）⇒ 尺寸**别照抄**；
    · `'grid?'`= **没排**：Grid 系（网格算法与 HOVLG 不同）；
    · `'axis?'`= **没排**：类名没解出（`LayoutGroup(?)` 指纹兜底）⇒ **不猜主轴**；
    · `'cust?'`= **没排**：类名解出了、但算法不是 HOVLG 那一套（`FlexibleGridLayout` /
      `CurvedLayout` …，A127 新增）⇒ 子件**停在模板位**，表尾那一块会点名。
    · 🔴 `'sp?'` = **没排**（A150 新增）：`EverguildLayoutGroup` 的 `evenlySpaceinBetween=1` 档里
      `CalculateSpacing()` **算不出**（只有 `n == 1` 一种情形 ⇒ 原版是 `x / 0` = ±Inf/NaN，
      原版自己就没有确定版面）⇒ 子件停在模板位。理由全文见 `everguild_spacing`。
    主轴 = Vertical→y / Horizontal→x（**靠 MonoScript 的类名判，不靠字段猜**；
    `EverguildLayoutGroup` 是例外：它的主轴在 `alignment` 字段上，判据见 `_lg_is_vertical`）。

    🔴 **2026-09-27 修一处真缺陷**：`kids` 必须**先滤掉不参与布局的子件**。
       Unity 的 `LayoutGroup.CalculateLayoutInputHorizontal` 建 `rectChildren` 时是
       `if (rect == null || !rect.gameObject.activeInHierarchy) continue;` —— **只收
       activeInHierarchy 的子节点**（**整条父链**，见 `_active_in_hierarchy`）。
       不滤的话：① 每个键分到的尺寸会**偏小**（分母多算了不参与的）；② 整组的位置也会偏。
       本条是**子代理在 Ranking 页普查时抓到并报出来的**（出处：那页有 3 类共 11 个布局组带 inactive 子节点）。
       ⚠️ **`Tab Buttons` 那条自检抓不到它**（六个键全 active）—— 所以自检绿**不等于**这条对。
       🔴 **2026-10-06 把判据从 `m_IsActive` 改成 `activeInHierarchy`**（A108，见 `_active_in_hierarchy`）：
       照 uGUI 的字面量（`!rect.gameObject.activeInHierarchy`）**等价于**下面这一行 —— 推导：
       子件是**本组的直接子件** ⇒ 它的祖先链 = **本组 + 本组的祖先** ⇒
       `activeInHierarchy(子) = 子自己的 m_IsActive ∧ activeInHierarchy(本组)`。
       ⛔ 所以「把字面量写成 `_active_in_hierarchy(b, k)`」是**错的**：那样在「本组自己就不在
       activeInHierarchy 里」时会把**整组的孩子全丢掉**，而 uGUI 是**整个组都不跑**
       （不是只丢几个孩子）—— 本工具**故意**照样排 inactive 子树（文件头：要的是「**激活之后**」的版面，
       `walk` 也走进 inactive 子树把它们列出来）⇒ 那一档下必须**把整组当 active 看**，
       否则会得到「组排了、孩子一个没排」的**自相矛盾**结果。
       🔴 **2026-10-12 补（A487）：上面那段推导是对的，但 A108 落地时走反了。**
       正确结论是 **`grp_aih` 根本不该进过滤** —— 「把整组当 active 看」= 只改**本组**那一格，
       于是 `activeInHierarchy(子) := 子自己的 m_IsActive`；A108 的实现把那一档写成「**全都留着**」，
       等于**把出厂 `F` 的孩子也当 active**（那是**孩子**那一格，不是**本组**那一格）。
       改后过滤判据**只此一条**（在 `_rect_children` 里），改前/改后的实据也写在那个 docstring 里。
       ⚠️ 实测（`bundle_menus_assets_all`）：**父组自己在 activeInHierarchy 里**时，两种写法**逐点同值**
       （差集 = **0** —— 这正是 A108 记的那个「= 0」，**成立**）；
       真正岔开的是**另一件**：**1710 个子件挂在「自己就不 active」的组下面**
       （例：`Deck Editing Menu/…/Deck Information cost drawer` 整条抽屉 `m_IsActive=0`）
       ⇒ 那一档 uGUI 此刻**不跑这个组的布局**，本表给的是「它被激活之后」的值 ——
       逐行/逐组都会标出来（行首 `⛔GRP-off`、表尾有一块清单），**不静默**。
    """
    # 🔴 `grp_aih` = **这个组自己**在不在 `activeInHierarchy` 里（= 原版此刻跑不跑它的布局）：
    #    `False` ⇒ 本表仍按「激活之后」算，但**必须出声**（见上面那一段与表尾那块清单）。
    #    ⛔ **A487（2026-10-12）：它只用来出声，不参与过滤** —— 过滤判据见 `_rect_children`（只此一处）。
    #    ⚠️ 过滤规则**只此一处**（`_rect_children`）—— `_group_axis_totals` 递归时用的是同一份，
    #       两处写两条判据 = 迟早不一致。
    kids, grp_aih = _rect_children(b, mono, rtpid, kids)
    # 🔴 **2026-09-27 再修一处真缺陷（同一族的第二条）**：还要滤掉
    #   **`LayoutElement.m_IgnoreLayout == 1`** 的子节点 —— Unity 建 `rectChildren` 时同样跳过它们
    #   （`ILayoutIgnorer.ignoreLayout`；`LayoutGroup` 收孩子那一步两者都判）。
    #   ⚠️ 这条是**普查聊天窗时抓到的**（子代理实测被它坑到 3 行）：`ChatPanel/Chat/Enter Text`
    #   的 `Background` 与 `Button`（真值都是**整行全拉伸 / 40×40 锚右中**，被算成 1120×0）、
    #   以及 `ChatMessageRow/RowBackground`（真值全拉伸，被算成 0 高并把**行内 y 全带偏**）。
    #   ⚠️ **自检抓不到它**：`--verify-layout` 那两个 fixture 的子节点都没有 `LayoutElement`。
    #   （这一条现在也在 `_rect_children` 里，与上面那条同一个函数。）
    #
    # 🔴 **2026-10-04 补 `m_ChildScaleWidth` / `m_ChildScaleHeight`（A36 审查挑出来的第三个缺口）**：
    #   uGUI 里 **`useScale` 为真时，子件在主轴上的一切量都要乘 `child.localScale[axis]`** ——
    #     · `CalcAlongAxis`：`min *= sf; preferred *= sf; flexible *= sf;`
    #       （`HorizontalOrVerticalLayoutGroup.cs:106-112`）⇒ **`tot_{min,pref,flex}` 全变**；
    #     · 主轴步进：`pos += childSize * scaleFactor + spacing`（同文件 `:203-216`）；
    #     · 落位：`LayoutGroup.SetChildAlongAxisWithScale`（`LayoutGroup.cs:249-267`）
    #       `anchoredPosition[0] = pos + sizeDelta.x * pivot.x * scaleFactor`
    #       （纵轴 `-pos - sizeDelta.y * (1 - pivot.y) * scaleFactor`）；
    #     · 交叉轴（`alongOtherAxis` 支）：`GetStartOffset(axis, requiredSpace * scaleFactor)`
    #       + 同一个落位算式。
    #   本文件原来**全程只读 `m_SizeDelta`**（`useScale` 当不存在）⇒ 带缩放子件的布局组**全算偏**。
    #   实测（`ReRollPopup Variant` 的内层 `Price Display`：`scaleW=1` + `icon.localScale.x = 1.2`）：
    #   `icon` 中心 **1169.25 ⇒ 1174.85**、价钱文字左 **1202.75 ⇒ 1213.95**（= 当时这一条单独的效果）。
    #   ⚠️ **`--verify-layout` 的 fixture ③ 就是为这条加的**（前两个 fixture 都不带缩放子件 ⇒
    #   这条路径以前**从没被验过**）。`useScale = 0` 时 `sf` 恒 1 ⇒ 算式退化回原来那一套、输出不变。
    #
    # 🔴 **2026-10-05 补第四条（主轴 `childSize` 格 / `offsetInCell`）—— 上面那两个 1174.85/1213.95 已作废**：
    #   同窗还有一层外层 `Buttons`（`ctrlW=0 expandW=1 align=4`）夹在中间 ⇒ 一旦 `flexible` 修正成
    #   真正的 uGUI 值（见 `_child_sizes`），那两个**绝对**坐标会再变一次。四态实测：
    #
    #   | 状态 | `icon` 中心 x | `text` 左边缘 |
    #   |---|---|---|
    #   | W0 = 补 `m_ChildScale` 之前 | 1169.25 | 1202.75 |
    #   | W1 = 补 `m_ChildScale` 之后（未补 flexible） | 1174.85 | 1213.95 |
    #   | W2 = 只补 `flexible`（uGUI 里**不存在**的中间状态） | 1137.15 | 1176.25 |
    #   | **W3 = 完整 uGUI（= 现状）** | **1193.70** | **1232.80** |
    #
    #   ⇒ **结论：别单独用 W1 那两个数**（A59 早期记的就是它）。出处与全量影响面（29 窗口/376 节点）
    #   → `资料/普查产出_1004/menu_dump_影响面_1005.md`；fixture ③ 的期望值同表。
    lg = None
    for _cp, cls, mb in components_of(b, mono, b.rt[str(rtpid)]):
        # 🔴 `m_Enabled=0` 的布局件**原版不跑** —— 跑了会把子节点整体挪走。
        #    实例：本窗的 `Menu Area` 挂着 HorizontalLayoutGroup 但 `m_Enabled=0`，
        #    照跑会把 `Tab Buttons` 从 180.24 顶到 187.24（正本 §2·1 早标了这条，2026-09-27 踩了一次）。
        if mb.get('m_Enabled', 1) == 0:
            continue
        # 🔴 **2026-10-06 A127：判据换成「是不是布局组」的那张表（`LGE_GROUP`）** ——
        #    原来只认「类名以 `VerticalLayout`/`HorizontalLayout` 开头」或名字里带 `GridLayoutGroup`，
        #    于是**三个自定义布局组一个都认不出来**（实测 `bundle_menus_assets_all` 里各 3/3/1 个）：
        #      · `EverguildLayoutGroup`（3 个）· `FlexibleGridLayout`（3 个）—— **旧过滤整个漏掉**
        #        ⇒ 落到 `lg is None`、子件**停在模板位**，而且**表里一个字都不提**（静默）；
        #      · `EverguildGridLayoutGroup`（1 个）—— 旧过滤靠 `'GridLayoutGroup' in cls`
        #        **碰巧**认出来（子串），这次改成走同一张表，**结论不变**（仍 `[grid?]`）。
        #    ⚠️ `LGE_GROUP` 与 `_ilayout_cells`（算 `ILayoutElement` 三项那一路）**共用同一张表**
        #      —— 两处各写一份 = 迟早不一致（这正是本条 bug 的形状：`_ilayout_cells` 早就知道
        #      `EverguildLayoutGroup` 是布局组（在 `LGE_GROUP` 里），而这里不知道）。
        if cls.startswith('VerticalLayout') or cls.startswith('HorizontalLayout') \
                or 'GridLayoutGroup' in cls or cls == 'LayoutGroup(?)' or cls in LGE_GROUP:
            lg = (cls, mb)
            break
    if lg is None:
        return None, None, None, grp_aih
    if not kids:
        # 🔴 **A487 补这一档**（原来与上面那条合并成一个 `if`）：**有布局组、但一个可排子件都没有**
        #    （孩子全被 `_rect_children` 的 ① / ② 两条滤掉）—— uGUI **照样跑**这个组、只是没有孩子落位。
        #    `est` 仍给 `None`（表里既不标「排了」也不标「没排」，那个清单是给**有孩子**的组用的），
        #    但 **`lgcls` 非空** ⇒ 调用方据此知道「本节点**有**布局组」——
        #    `⛔GRP-off` 那条判据要它（否则「组自己不在 `activeInHierarchy` 里」会**静默消失**，
        #    表尾那个计数也会跟着少）。改前这一档在 A108 的 bug 下**到不了**（孩子全收 ⇒ 不会空）。
        return None, lg[0], lg[1], grp_aih
    cls, mb = lg
    if 'm_CellSize' in mb or 'GridLayoutGroup' in cls:
        return 'grid?', cls, mb, grp_aih
    # 🔴 **2026-10-06 A127**：主轴改用与 `_ilayout_cells` **同一个**函数判（`_lg_is_vertical`）——
    #    原来这里写的是「类名以 VerticalLayout 开头就纵、否则横」，与那一份**是两条判据**，
    #    自定义子类（`EverguildLayoutGroup`）在这一份里直接落 `axis?`。
    #    现在三档：
    #      · `True/False` ⇒ 照 HOVLG 排（`EverguildLayoutGroup` 只在 `evenlySpaceinBetween == 0`
    #        时才判得出主轴，判据见 `_lg_is_vertical` 的 docstring）；
    #      · 类名没解出（`LayoutGroup(?)` 指纹兜底）⇒ `axis?`（主轴判不出 ⇒ 不排、不猜）；
    #      · 类名解出了、但**算法不是 HOVLG 那一套**（`FlexibleGridLayout` / `CurvedLayout` …）⇒ `cust?`
    #        （**本工具没排它** ⇒ 子件停在模板位，表尾那一块会点名）。
    #    🔴 **2026-10-06 A150 再拆一档**：`EverguildLayoutGroup` 的 `evenlySpaceinBetween=1`
    #       从前也落 `cust?`（「spacing 运行期才算」）。现在**子件落位这一路放行**它 ——
    #       它的主轴仍然是 `alignment`（flag 只改 `spacing`，不改主轴），而 `spacing` 的算式
    #       在**本函数所处的这一帧**上**恰好可算**（推导见下一段 + `everguild_spacing`）：
    #         · `n >= 2` ⇒ 照算式排（`est` 落 `ok`/`unk`，与普通 HOVLG 一样）；
    #         · `n == 1` ⇒ `sp?`（原版 `x/0` = NaN，原版自己就没有确定版面）；
    #         · 尺寸恢复不出 ⇒ `unk`（老的、别处的同一档）。
    iv = _lg_is_vertical(cls, mb)
    eg = None                       # A150：`EverguildLayoutGroup` + flag=1 ⇒ 主轴照 alignment、spacing 照算式
    if iv is None and cls == 'EverguildLayoutGroup' and mb.get(EG_FLAG) \
            and mb.get('alignment') in (0, 1):
        eg = (mb.get('alignment') == 1)
    if iv is None and eg is None:
        return ('axis?' if cls == 'LayoutGroup(?)' else 'cust?'), cls, mb, grp_aih
    axis = 1 if (iv if iv is not None else eg) else 0
    pad = mb.get('m_Padding', {}) or {}
    spacing = mb.get('m_Spacing', 0) or 0
    ctrl = bool(mb.get('m_ChildControlWidth' if axis == 0 else 'm_ChildControlHeight'))
    fexp = bool(mb.get('m_ChildForceExpandWidth' if axis == 0 else 'm_ChildForceExpandHeight'))
    # uGUI `useScale`（`m_ChildScaleWidth` / `m_ChildScaleHeight`）—— 真是 1 才乘子件 localScale。
    use_scale = bool(mb.get('m_ChildScaleWidth' if axis == 0 else 'm_ChildScaleHeight'))
    # 🔴 **`m_ReverseArrangement`**（2026-10-05 补；本文件在此之前**完全不读这个字段**）。
    #   判据 = uGUI `HorizontalOrVerticalLayoutGroup.SetChildrenAlongAxis:153-155` +
    #          `:198` 的循环条件（逐行照抄）：
    #     ```
    #     int startIndex = m_ReverseArrangement ? rectChildren.Count - 1 : 0;
    #     int endIndex   = m_ReverseArrangement ? 0 : rectChildren.Count;
    #     int increment  = m_ReverseArrangement ? -1 : 1;
    #     ...
    #     for (int i = startIndex; m_ReverseArrangement ? i >= endIndex : i < endIndex; i += increment)
    #     ```
    #   ⚠️ **`pos` 的推进方向不变**（`:216` `pos += childSize * scaleFactor + spacing;` 是无条件的，
    #      初值也仍是 `padding.left`/`padding.top`，`:182`）—— 变的是**哪一颗子件来消费当前这个 `pos`**：
    #      `reverse=1` 时树序**最后一个**子件落在**起点**（HLG = 最左 / VLG = 最上），
    #      树序**第一个**落在终点。所以「整排的 x 集合」通常不变、**名字与位置整体镜像**。
    #   ⚠️ **只翻主轴这一处**，另两处**不要**跟着翻（都读过源码确认与顺序无关）：
    #      · `CalcAlongAxis:100-128` 三个总量是**求和**（`totalMin += …`）⇒ 与顺序无关；
    #      · 交叉轴 `alongOtherAxis` 支（`:156-179`）：每颗子件**各自算自己的 `GetStartOffset`**
    #        （只依赖它自己），循环次序不影响结果。
    #   踩过的坑：`项目任务.md` §三 第 29 条 **A91③** 那份 ready patch 就是拿本工具**镜像后**的读数推的
    #      （`bundle_menus_assets_all` 的 `Deck info Popup/Deck Options` 那颗 HLG，`reverse=1`）。
    rev = bool(mb.get('m_ReverseArrangement'))
    # 🔴🔴 **2026-10-06 修一处真缺陷（「布局组算出来的尺寸被乘了两次父链缩放」）**：
    #    这里原来是 `csz = MR.rect_of(...)[1]` = **屏幕框**（`rect.size × lossyScale(父)`），
    #    而 uGUI 的布局算法**全程在【本件的局部单位】里算** ——
    #      · `SetChildrenAlongAxis`：`float size = rectTransform.rect.size[axis]`（局部）；
    #      · `LayoutGroup.SetChildAlongAxisWithScale`：`rect.sizeDelta[axis] = size`（写进局部字段）；
    #      · `GetStartOffset`：`availableSpace = rectTransform.rect.size[axis]`（局部）。
    #    ⇒ 拿屏幕框当 `size` 用 ⇒ **写进 `sizeDelta` 的值已经含了一次 `lossyScale(父)`**，
    #      而 `rect_of` 又要对它乘一次 ⇒ **屏幕列偏小一个 `lossyScale(父)`**。
    #    ⚠️ 这条是 **A60⑤⑨（给 `rect_of` 接上 `scale`）带出来的回归**：修之前 `rect_of` 给的
    #      那个数既不是局部也不是屏幕（`anchorDiff×父屏幕 + sizeDelta×1`），错得没这么整齐。
    #    实据（`Audio Settings`，VLG `ctrlW=1 ctrlH=0`，父链 `lossyScale=0.9`）：
    #      · 组自己的局部宽 = **684.194**（= `--no-ancestor-scale` 那一栏，也是 uGUI 真值）；
    #      · 修前写进子件 `sizeDelta.x` 的是 **615.775**（= 684.194×0.9）⇒ 屏幕列再 ×0.9 = **554.20**；
    #      · 修后写 **684.194** ⇒ 屏幕列 **615.77** ✔。
    #    ⚠️ **不是只有 x 会错**：错的是「**布局往 `sizeDelta` 里写哪个轴**」——
    #      主轴 `ctrl=1`（写 `cell`）与交叉轴 `ctrl=1`（写 `req`）**都会**；`ctrl=0` 的那一轴不写
    #      `sizeDelta` ⇒ 那一轴的**尺寸列**看上去是对的（但 `pos` 仍受影响，见 `surplus` 那条分支）。
    csz = MR.local_size(b.rt[str(rtpid)], rect, scale)
    if csz is None:
        # 父链缩放≈0 ⇒ 局部尺寸恢复不出（见 `menu_rect.parent_local_size`）⇒ **不许瞎填**：
        # 一整组一个数都不改，如实标 `unk`（尾部的 🔴 清单会把它列出来）。
        return 'unk', cls, mb, grp_aih
    size_main = csz[axis]
    size_other = csz[1 - axis]

    # 🔴🔴 **2026-10-06 A150：`EverguildLayoutGroup` 的 `CalculateSpacing()` 在【这一帧】可算。**
    #   「单遍模型下 spacing 取哪一帧」这个问题，判据是 uGUI 的**四阶段**次序
    #   （`LayoutRebuilder.Rebuild`，`Layout/LayoutRebuilder.cs:83-89`）：
    #     ```
    #     PerformLayoutCalculation(CalculateLayoutInputHorizontal)   // ① 自下而上（子先父后）
    #     PerformLayoutControl   (SetLayoutHorizontal)               // ② 自上而下（父先子后）
    #     PerformLayoutCalculation(CalculateLayoutInputVertical)     // ③ 自下而上
    #     PerformLayoutControl   (SetLayoutVertical)                 // ④ 自上而下
    #     ```
    #   四个覆写体**各在开头调一次** `CalculateSpacing()` ⇒ `m_Spacing` 一个 pass 里被重算 **4 次**，
    #   而**真正决定子件落位**的是 `SetChildrenAlongAxis` 里那一次 ——
    #     · 轴 0（横）⇒ 第 **②** 次（`SetLayoutHorizontal` 的头一句）；
    #     · 轴 1（纵）⇒ 第 **④** 次（`SetLayoutVertical` 的头一句）。
    #   那一帧的**状态**是（三件事都可证）：
    #     ① 父级**已经**写过本组的 `sizeDelta`（②/④ 都是自上而下，父先于子）；
    #     ② 本组自己的 `ILayoutSelfController`（CSF/ARF/…）**已经跑过**（`PerformLayoutControl`
    #        先跑 `ILayoutSelfController` 再跑 `ILayoutController`，同文件 `:106-124`）；
    #     ③ 本组的**子件还没被本组写过**（②/④ 里本组的控制器先于子件的控制器）。
    #   ⇒ 拿本函数的三个已知量对齐：`csz`（第 ① 条 —— `walk` 是父先子后，且 `apply_self_fitters`
    #      在 `apply_layout_to_children` **之前**跑，故 `csz` 正是第 ② 条的产物）+ `kids[0]` 当前的
    #      `rect`（第 ③ 条 —— 单遍模型里子件的 dict 此刻**一个字都还没被改**）。
    #   ⇒ **这一帧与 uGUI 那一帧逐条对齐，所以这一档可以在本函数里算**（不是「看着像」）。
    #   ⚠️ **同一组的「自己的三项总量」不在这一帧**（那是 `CalcAlongAxis`，在第 ①/③ 阶段，
    #      本组的尺寸这时**还没被父级写**）⇒ `_ilayout_cells` 仍标 `LG_UNK`，见 `_lg_is_vertical` ④。
    #   ⚠️ 口径与整个工具一致：本工具模拟的是**「刚实例化之后那一遍布局」**（子件还在 prefab 值上），
    #      不是稳态不动点（`walk` 全程只走一遍，任何布局组都是这么处理的）。
    if eg is not None:
        first = kids[0] if kids else None
        # 子件自己的 `RectTransform.rect.size`（局部单位）—— 拿「本组的局部框」当父框、缩放恒 1，
        # 理由与 `local_size` 自己的 docstring 同：uGUI 的 `rect.size` 是**局部量**，与任何缩放无关。
        fl = MR.local_size(first, (0.0, 0.0, csz[0], csz[1]), (1.0, 1.0)) if first is not None else None
        spacing, _why = everguild_spacing(
            True, spacing, len(kids), None if fl is None else fl[axis], csz[axis])
        if _why is not None:
            # **出声、不出数**：这一档本工具排不了 ⇒ 子件一个字节都不改（此刻还没写过任何东西）。
            # ⚠️ `'size?'` 这一支**实际到不了**（`local_size` 在缩放恒 (1,1) 时不会返回 `None`）——
            #    留着是**防御**（万一将来 `local_size` 改了返回值语义）；真出不来也不会静默：
            #    它会落 `unk`（本来是给「排了但算不准」的档，此处借用 = 宁可多喊一声）。
            return ('sp?' if _why == 'n==1' else 'unk'), cls, mb, grp_aih

    # 每个子件带上它在**主轴**上的 `scaleFactor`（`p[5]`）—— `CalcAlongAxis` 里 min/pref/flexible 都要乘它。
    per = [(k,) + _child_sizes(b, mono, k, axis, ctrl, fexp, sidx, bidx) + (_axis_scale(k, axis, use_scale),)
           for k in kids]
    pad_main = (pad.get('m_Left', 0) + pad.get('m_Right', 0)) if axis == 0 \
        else (pad.get('m_Top', 0) + pad.get('m_Bottom', 0))
    n = len(per)
    tot_min = sum(p[1] * p[5] for p in per) + spacing * max(0, n - 1) + pad_main
    tot_pref = sum(p[2] * p[5] for p in per) + spacing * max(0, n - 1) + pad_main
    tot_pref = max(tot_min, tot_pref)
    tot_flex = sum(p[3] * p[5] for p in per)
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
    # `SetChildrenAlongAxis:150` 把 `alignmentOnAxis` 算一次，下面主轴的**两条落位路**共用它。
    alg = align_on_axis(axis, mb.get('m_ChildAlignment', 0))
    # `reverse=1` ⇒ 按 `rectChildren` 的**倒序**消费 `pos`（理由与出处见上面 `rev` 那一段注释）。
    seq = list(reversed(per)) if rev else per
    for (k, mn, pf, fx, u, sf) in seq:
        unk = unk or u
        # 🔴 `cell` = uGUI `:205-206` 的 `childSize`（`Mathf.Lerp(min, preferred, minMaxLerp)`
        #    再 `+= flexible × itemFlexibleMultiplier`）—— 与 `controlSize` **无关**。
        #    2026-10-05 之前本文件**没有这个变量**：直接把「要写回的 `sizeDelta`」当成它，
        #    于是 `ctrl=0` 时步进用的是 `sizeDelta` 而不是「格」，整组摆不开。
        cell = mn + (pf - mn) * minmax + fx * fmul
        pv = k['m_Pivot']
        k['m_AnchorMin'] = {'x': 0, 'y': 1}
        k['m_AnchorMax'] = {'x': 0, 'y': 1}
        if ctrl:
            # `:209` 4 参版 `SetChildAlongAxisWithScale`：写回 `sizeDelta = childSize`，按 `pos` 落位。
            new = cell
            place = pos
        else:
            # `:213-214` 3 参版：`sizeDelta` **一个字都不动**，落位点在格内再按交叉对齐偏移
            # `offsetInCell = (childSize - child.sizeDelta[axis]) × alignmentOnAxis`。
            # 🔴 这一条 2026-10-05 才补上（原来漏了 ⇒ `align` 非左/上时子件**贴着格的边**画，
            #    与「格内居中对齐」差 `(childSize - sizeDelta) × 0.5`）。
            new = k['m_SizeDelta']['x'] if axis == 0 else k['m_SizeDelta']['y']
            place = pos + (cell - new) * alg
        # `SetChildAlongAxisWithScale`：落位与步进都要乘 `scaleFactor`（`sizeDelta` 本身**不乘**）
        if axis == 0:
            k['m_SizeDelta']['x'] = new
            k['m_AnchoredPosition']['x'] = place + new * pv['x'] * sf
        else:
            k['m_SizeDelta']['y'] = new
            k['m_AnchoredPosition']['y'] = -place - new * (1 - pv['y']) * sf
        # `:216` **步进用的是带 flexible 的 `childSize`**（不是 `sizeDelta`，也不是 `new`）。
        pos += cell * sf + spacing

    # ---- 交叉轴（`alongOtherAxis` 支）----
    other = 1 - axis
    o_ctrl = bool(mb.get('m_ChildControlWidth' if other == 0 else 'm_ChildControlHeight'))
    o_align = align_on_axis(other, mb.get('m_ChildAlignment', 0))
    o_use_scale = bool(mb.get('m_ChildScaleWidth' if other == 0 else 'm_ChildScaleHeight'))
    o_pad = (pad.get('m_Left', 0) + pad.get('m_Right', 0)) if other == 0 \
        else (pad.get('m_Top', 0) + pad.get('m_Bottom', 0))
    for (k, mn_, pf_, fx_, u_, _sf) in per:
        gmn, gpf, gfx, _ = _child_sizes(b, mono, k, other, o_ctrl, bool(mb.get(
            'm_ChildForceExpandWidth' if other == 0 else 'm_ChildForceExpandHeight')), sidx, bidx)
        gsf = _axis_scale(k, other, o_use_scale)
        inner = size_other - o_pad
        # `:167` `requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred)`
        # —— 用**逐行照抄**的 `_clamp`（`min > max` 时与 `max(lo, min(v, hi))` 不同值）。
        req = _clamp(inner, gmn, size_other if gfx > 0 else gpf)
        # `GetStartOffset(axis, requiredSpace * scaleFactor)`
        start = (pad.get('m_Left', 0) if other == 0 else pad.get('m_Top', 0)) \
            + (size_other - (req * gsf + o_pad)) * o_align
        if o_ctrl:
            target = start
            k['m_SizeDelta']['x' if other == 0 else 'y'] = req
        else:
            sd = k['m_SizeDelta']['x' if other == 0 else 'y']
            target = start + (req - sd) * o_align
        if other == 0:
            k['m_AnchoredPosition']['x'] = target + (
                k['m_SizeDelta']['x'] * k['m_Pivot']['x'] * gsf)
        else:
            k['m_AnchoredPosition']['y'] = -target - (
                k['m_SizeDelta']['y'] * (1 - k['m_Pivot']['y']) * gsf)
    return ('unk' if unk else 'ok'), cls, mb, grp_aih


# ================================================================ 走树
def new_stats():
    """走树时顺带记的三个数（**只为了让结尾能出声**，不影响表里任何一个数）。

    🔴 为什么要它（2026-10-05）：`walk` 遇到 `depth > maxdepth` 是**静默 return** 的 ——
      默认 `--depth 6` 下 `Alliance Trophy Info Popup` 的 `Fill Area`/`Fill`/`end`/`CheckMark`
      **四件一个都不印**，而输出里**一个字都没提**（不报错、不提示）。
      照默认输出搭树就会**少一层**，而且是**看不出来**的少（那次要 `--depth 12` 才全出）。
      ⇒ 本条按纪律「**不许静默失败**」补：截断了就说清「还有几个、要到第几层」。
    """
    return {'cut_nodes': 0,        # 因深度上限**没印**的节点数（含它们的整棵子树）
            'cut_deepest': 0,      # 这些节点里最深的是第几层（= 该用的 `--depth`）
            'cut_roots': 0,        # 被截掉的第一层有几个（>= len(cut_tops)）
            'cut_tops': [],        # 被截掉的第一层，最多列 8 个 (缩进, 名字, 层)
            't_kids': 0,           # 纯 `Transform`（3D，没有 RectTransform）子件数 —— 也不在表里
            'miss': 0,             # 子 pid 在 `RectTransform/` 里**查不到**的个数（= t_kids + 真缺）
            'fit': 0,              # 挂了自适配组件（`ILayoutSelfController` 那几件）且**回写成功**的件数
            'fit_unk': 0,          # 挂了自适配组件、但**目标值算不出**（表里仍是模板值）的件数
            'rot': 0,              # `m_LocalRotation` 的 z ≠ 0 的件数（表里的矩形是**未旋转帧**）
            'rot_sub': 0,          # 落在**某个旋转件下面**的件数（它们的位置也未按旋转重算）
            'rot_xy': 0,           # **只绕 x/y** 转（z=0）的件数 —— 本工具连「转了」都没报（A110 尾巴）
            'anc_off': 0,          # 自己 `m_IsActive=1` 但**祖先有 inactive** 的件数（A108：uGUI 也不画它）
            'grp_off': 0,          # **自己就不在 `activeInHierarchy` 里**的布局组数（A108：原版此刻不跑它）
            'usf': 0,              # `UIScaleToFit` **施加成功**（改写了自己 `localScale`）的件数
            'usf_no': 0,           # `UIScaleToFit` 在、但**没施加**（原版那条路也会 return）的件数
            'rsl': 0,              # `RectSizeLimiter` 真的改了尺寸的件数
            'csfmm': 0}            # `ContentSizeFitterMinMax` 真的钳到 `sizeDelta` 的件数


def _count_subtree(b, rtpid, depth, _guard=0):
    """只**数**这棵子树有多少个能被印出来的节点、最深到第几层（不建表、不改任何 dict）。

    🔴 **A620（2026-10-14）：实现已挪到 `menu_rect.count_subtree`（只此一份），本函数 = 转发。**
       理由 = 本仓红线「两处写同一条规则 = 迟早不一致」：`menu_rect.walk` 的深度截断
       （A620）也要数子树，而它**不能**反向调本文件的那一份 —— `menu_dump` 在模块级
       `import menu_rect` ⇒ 会成环。做法与 `_active_in_hierarchy` → `MR.active_in_hierarchy`
       （A499）逐字同一条：**谁被两个工具都要，谁就住在 `menu_rect`**。
    ⚠️ 口径一个字没变（`RectTransform/` 里查不到的**不算**、`_guard` 防 `m_Children` 成环）
       —— 那两句原文现在住在 `menu_rect.count_subtree` 的 docstring 里。
    """
    return MR.count_subtree(b, rtpid, depth, _guard)


def walk(b, mono, rtpid, rect, scale, depth, maxdepth, out, indent,
         apply_layout, sidx, bidx, force_root_rect=None, stats=None, rot_anc=False,
         keep_scales=True, act_anc=None):
    rt = b.rt.get(str(rtpid))
    if rt is None:
        # 子件是**纯 `Transform`**（3D，例：卡片的 3D 体）⇒ 它没有 RectTransform、
        # **本来就不该进这张 UI 表**。但「表里少了东西一个字不说」也是静默失败 ⇒ 记个数、结尾出声。
        if stats is not None:
            stats['miss'] += 1
            if os.path.exists(os.path.join(b.path, 'Transform', f'Transform_{rtpid}.json')):
                stats['t_kids'] += 1
        return
    if depth > maxdepth:
        # 🔴 **深度截断不许静默**（2026-10-05）：把「还有多少没印、要到第几层」记下来，见 `new_stats`。
        if stats is not None:
            # 🔴 **A621（2026-10-14）：这里取名字原来走 `b.go_name(gopid)`**（按 **pid** 认 GO）
            #    —— 撞车包下 `self.go[pid]` 只留得下**第一份** ⇒ **表尾那份「被截掉的件」清单
            #    会印成另一个 CAB 的名字**（表里那一行 A499 已经改了，这一处漏了）。
            #    改走 `go_name_of_rt`（按**这颗 RT** 认它的 GO）：没有撞车时两者指向同一个对象
            #    ⇒ 输出逐字节不变；`<RT {rtpid}>` 那个兜底与 `menu_rect.walk` 同口径。
            nm = b.go_name_of_rt(rtpid) or f'<RT {rtpid}>'
            n, deep = _count_subtree(b, rtpid, depth)
            stats['cut_nodes'] += n
            stats['cut_deepest'] = max(stats['cut_deepest'], deep)
            stats['cut_roots'] += 1
            if len(stats['cut_tops']) < 8:
                stats['cut_tops'].append((indent, nm, depth))
        return
    # 🔴 **2026-10-13（A499 追加）：名字与 `g` 改成按【这颗 RT】认 GO**（`MR.Bundle.go_obj_of_rt`）——
    #    原来走 `b.go.get(rt.m_GameObject.m_PathID)`，而 **pid 是分包/分内层 CAB 局部的**：
    #    一个目录里可以有两份同号 GO（本包实测 `bundle_scenes_scenes_mainmenuwarpforge` 的
    #    `Icon_238.json` 与 `Player Level.json` 都自称 `m_GameObject.m_PathID = 238`）⇒
    #    撞车时**名字 / `m_IsActive` / 组件列会整套取到另一个 CAB 的那一份上**，
    #    而且原来 `_scan_go` 是「后扫覆盖先扫」⇒ 取到哪个**看 `os.listdir` 顺序**（不可复现）。
    #    ⚠️ 没有撞车时两条路指向**同一个对象** ⇒ 输出逐字节不变。
    #    🔴 **A621（2026-10-14）**：这一行原来下面还挂着 `gopid = rt.get('m_GameObject', {})…`
    #       —— A499 改完之后它**再没有读者**（死局部），本件顺手删掉：留着会让下一个人以为
    #       「按 pid 认 GO」这条路还在这个函数里活着（A621 正是要清掉这类残留）。
    g = b.go_obj_of_rt(rtpid) or {}
    name = (g.get('m_Name') or g.get('_name')) or f'<RT {rtpid}>'
    active = g.get('m_IsActive', 1)
    # 🔴 **祖先的可见性**（A108）：`m_IsActive` 只管**自己那一格**，uGUI 的一切（`activeInHierarchy`）
    #    看**整条父链** ⇒ 「自己 active、祖先 inactive」的件**原版一个像素都不画**。
    #    `walk` 故意会走进 inactive 子树把件列出来（要如实列出），但**必须标出来**，
    #    否则读表的人会以为那件**是可见的**。
    if act_anc is None:
        par = b.parent(rtpid)
        act_anc = _active_in_hierarchy(b, par) if par else True
    anc_off = bool(active) and not act_anc

    # 🔴 **自适配组件先跑**（`ILayoutSelfController` 那几件，次序见 `apply_self_fitters`）——
    #    必须在**算自己的矩形之前**，否则表里给的是模板尺寸（它们就是来改那个尺寸的）。
    fit, fit_unk = [], None
    if apply_layout:
        fit, fit_unk = apply_self_fitters(b, mono, rt, rect, scale, sidx, bidx,
                                          keep_scales=keep_scales)
        if stats is not None:
            if fit:
                stats['fit'] += 1
            if fit_unk:
                stats['fit_unk'] += 1
            for lab in fit:
                if lab.startswith('USF ×'):
                    stats['usf'] += 1
                elif lab.startswith('USF 未施加'):
                    stats['usf_no'] += 1
                elif lab.startswith('RSL ') and '没改' not in lab:
                    stats['rsl'] += 1
                elif lab.startswith('CSFMinMax ') and 'clamp→' in lab:
                    stats['csfmm'] += 1

    r, (w, h) = MR.rect_of(rt, rect, scale)
    if depth == 0 and force_root_rect is not None:
        r = force_root_rect
        w, h = r[2] - r[0], r[3] - r[1]
    scl = rt.get('m_LocalScale') or {'x': 1, 'y': 1}

    details = []
    for _cp, cls, mb in components_of(b, mono, rt):
        sp, ti, tx, ex = describe(cls, mb, sidx, bidx)
        details.append(dict(cls=cls, sprite=sp, tint=ti, text=tx, extra=ex, mb=mb))

    kids = [b.rt.get(str(c)) for c in b.children(rtpid)]
    kids = [k for k in kids if k]
    est = None
    lgcls = None
    lgreverse = False
    grpoff = False
    if apply_layout and kids:
        est, lgcls, _mb, _gaih = apply_layout_to_children(b, mono, rtpid, rect, scale, kids,
                                                          sidx, bidx)
        # 给结尾那张「哪些组是倒排的」清单用（`--no-layout` 时恒 False —— 那种模式下没算布局）。
        # ⚠️ `est` 那一项是 **A487** 加的：新拆出的「有布局组、但没有可排子件」那一档里
        #    `_mb` 非空而 `est` 为 `None` —— 没有子件的组不该进那张「名字↔位置镜像」清单。
        lgreverse = bool(est and _mb and _mb.get('m_ReverseArrangement'))
        # 🔴 A108：**这个组自己不在 `activeInHierarchy` 里**（原版此刻不跑它的布局）
        #    —— 本表仍按「激活之后」算，但行首标 `⛔GRP-off`、表尾单列一块（不静默）。
        #    🔴 **A487（2026-10-12）：判据带上 `lgcls`** —— `est is None` 有两种情形：
        #       ① 「**没有**布局组」（`lgcls is None`）；
        #       ② 「**有**布局组、但一个可排子件都没有」（`lgcls` 非空）。
        #       ② 也必须出声 —— 否则「组不在 `activeInHierarchy` 里」这条**静默消失**、
        #       表尾那个计数跟着少（改前那一档到不了，见 `apply_layout_to_children` 的早退分支）。
        grpoff = (est is not None or lgcls is not None) and not _gaih
        if grpoff and stats is not None:
            stats['grp_off'] += 1

    out.append(dict(ind=indent, name=name, rect=r, w=w, h=h, active=active, rt=rt, scl=scl,
                    details=details, est=est, lgcls=lgcls, lgreverse=lgreverse,
                    fit=fit, fit_unk=fit_unk, lossy=scale, rot=_rot_z(rt), rot_anc=rot_anc,
                    anc_off=anc_off, rot_xy=_rot_xy(rt), grpoff=grpoff))
    if stats is not None:
        if out[-1]['rot']:
            stats['rot'] += 1
        elif rot_anc:
            stats['rot_sub'] += 1
        if out[-1]['rot_xy']:
            stats['rot_xy'] += 1
        if anc_off:
            stats['anc_off'] += 1
    for c in b.children(rtpid):
        ks = (scale[0] * scl.get('x', 1), scale[1] * scl.get('y', 1)) if keep_scales \
            else (1.0, 1.0)
        walk(b, mono, c, r, ks, depth + 1, maxdepth, out, indent + 1, apply_layout, sidx, bidx,
             stats=stats, rot_anc=rot_anc or bool(out[-1]['rot']), keep_scales=keep_scales,
             act_anc=(act_anc and bool(active)))


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

    # ---- ③**带缩放子件**的回归用例（`m_ChildScaleWidth` / `m_ChildScaleHeight`）----
    # 🔴 为什么要单独一条：前两个 fixture 的子件 `localScale` 全是 1（`useScale` 那一步**乘不乘都一样**）
    #    ⇒ 2026-10-04 补 `m_ChildScaleWidth` 之前，这条路径**从没被验过**。
    #    判据 = `ReRollPopup Variant` 的内层 `Price Display`（MB `519935136994325635`：`scaleW=1 scaleH=1`、
    #    `spacing=5.5`、`align=4 (MiddleCenter)`、`pad=(0,0,10,10)`、`ctrlW=0 ctrlH=1`）——
    #    它的 `icon` 是 `m_SizeDelta = (56,0)` + **`m_LocalScale = (1.2,1.2,1)`** + `pivot (0.5,0.5)`。
    #
    # 🔴 **2026-10-05 拆成两条 + 期望值从 W1 换成 W3**：原来这里只钉**绝对**坐标
    #    （`icon` 中心 1174.85 / `text` 左 1213.95），而那两个数**同时**被外层 `Buttons` 组的行为决定
    #    ⇒ 一旦 `_child_sizes` 的 `flexible` 修正（uGUI 里 `expandW=1` 时 `flexible` 恒 ≥1，见该函数），
    #    这一条会红，**但红的是外层、不是它本职的 `m_ChildScale`**。⇒ 拆成：
    #      (a) **内层相对几何** —— 组左边缘到 `icon` 中心 = 33.6、到 `text` 左 = 72.7（**不受外层影响**）；
    #      (b) **绝对位置** —— 组左边缘 = 1160.10（**含外层 `Buttons` 的 `offsetInCell`**）。
    #    两条的期望值**都照 uGUI 源码手算**（不是本工具算出来的）：
    #
    #    (a) 内层 `Price Display`（HLG，`ctrlW=0 expandW=0 scaleW=1 spacing=5.5 pad 左=0`，
    #        自己的 `sizeDelta.x = 0` ⇒ 组宽 0）：
    #      · `tot_pref = 56 + 0 + 5.5 = 61.5 > 组宽 0` ⇒ `surplusSpace ≤ 0` ⇒ `pos = padding.left = 0`
    #        （**余量那条分支根本不进** ⇒ 与 `align` 无关）；
    #      · `icon`：`ctrl=0` ⇒ `offsetInCell = (56 − 56) × 0.5 = 0`，落位
    #        `LayoutGroup.SetChildAlongAxisWithScale`（`LayoutGroup.cs`）
    #        `anchoredPosition.x = pos(0) + 56 × 0.5 × 1.2 = 33.6` ⇒ **中心 = 组左 + 33.6**
    #        （**旧算式漏了 ×1.2** ⇒ 28，差 5.6px）；
    #      · 步进（`HorizontalOrVerticalLayoutGroup.cs:216`）：`pos += 56 × 1.2 + 5.5 = 72.7`
    #        ⇒ **`text` 左 = 组左 + 72.7**（旧算式用 `sizeDelta` 61.5，差 11.2px）；
    #      · `icon` 的 **y 中心不变**（交叉轴的 `GetStartOffset(axis, req × sf)` 被同一个 1.2 加权，
    #        两个 5.6 正好抵消）⇒ 这一格只该往右挪、不该往下挪。
    #
    #    (b) 组左边缘 = **1160.10** —— 由外层 `Buttons`（HLG `ctrlW=0 expandW=1 align=4`、
    #        `spacing=0 pad=0`、`size = 775.40`、两颗 350 宽子件）算出：
    #      · `flexible = max(0,1) = 1`（`expandW=1`，**与 `ctrl=0` 无关**）⇒ `tot_flex = 2`；
    #        `tot_pref = 350 + 350 = 700` ⇒ `surplus = 75.40` ⇒ `itemFlexibleMultiplier = 37.70`，
    #        `pos` 从 `pad_left = 0` 起（**不再走 `GetStartOffset` 那条对齐偏移**）；
    #      · 每格 `cell = 350 + 37.70 = 387.70`；第 2 颗 `Price Display`：
    #        `offsetInCell = (387.70 − 350) × 0.5 = 18.85` ⇒ 起点 `387.70 + 18.85 = 406.55`
    #        ⇒ 组左 = `Buttons.x1 (572.30) + 406.55` = **978.85**；内层 `Price Display` 距它 **181.25**
    #        （锚点/`sizeDelta` 决定，与本条无关）⇒ **1160.10**
    #      · 合计：`icon` 中心 = 1160.10 + 33.6 = **1193.70**、`text` 左 = 1160.10 + 72.7 = **1232.80**。
    #      （⚠️ W1 = 只补 `m_ChildScale`、未补 `flexible` 时是 1174.85 / 1213.95 —— **那两个数已作废**；
    #       W2 = 只补 `flexible` 是 1137.15 / 1176.25，是 uGUI 里**不存在**的中间状态。
    #       四态实测表 → `资料/普查产出_1004/menu_dump_影响面_1005.md`）
    b3 = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    gop3 = b3.find_go('ReRollPopup Variant')
    rt3 = b3.rt_of_go(gop3) if gop3 is not None else None
    if rt3 is None:
        print('  ❌ fixture ③ 取不到 `ReRollPopup Variant`（缩放子件那条路径**没验成**）')
        return 1
    out3 = []
    walk(b3, mono_index(verbose=False), rt3, (0.0, 0.0, 1920.0, 1080.0), (1.0, 1.0),
         0, 6, out3, 0, True, {'by_pid': {}, 'ambiguous': {}}, {})
    icon3 = next((e for e in out3 if e['name'] == 'icon' and e['ind'] == 6), None)
    tx3 = next((e for e in out3 if e['name'] == 'text' and e['ind'] == 6), None)
    pd3 = next((e for e in out3 if e['name'] == 'Price Display' and e['ind'] == 5), None)
    if icon3 is None or tx3 is None or pd3 is None:
        print(f'  ❌ fixture ③ 取不到 `icon`/`text`/内层 `Price Display`'
              f'（{icon3 is not None}/{tx3 is not None}/{pd3 is not None}）')
        return 1
    icx = (icon3['rect'][0] + icon3['rect'][2]) / 2.0
    icy = (icon3['rect'][1] + icon3['rect'][3]) / 2.0
    gl = pd3['rect'][0]                       # 内层 `Price Display` 的**左边缘**（两条共用的基准）
    # (a) **内层相对几何** —— 只由 `m_ChildScale` + `spacing` 决定，**与外层 `Buttons` 无关**。
    ga = (abs((icx - gl) - 33.6) < 0.01 and abs((tx3['rect'][0] - gl) - 72.7) < 0.01
          and abs(icy - 579.00) < 0.01)
    ok = ok and ga
    print(f'  {"✅" if ga else "❌"} 缩放子件③a 内层相对几何（与外层无关）· 组左 {gl:.2f}'
          f' ⇒ `icon` 中心 {icx - gl:.2f}（要 33.6 = 56/2×1.2）· `text` 左 {tx3["rect"][0] - gl:.2f}'
          f'（要 72.7 = 56×1.2 + 5.5）· `icon` 中心 y {icy:.2f}（要 579.00 = 不该动）')
    # (b) **绝对位置** —— 含外层 `Buttons` 的 `flexible` / `offsetInCell`（`_child_sizes` 那两条修正）。
    # 🔴 **2026-10-06 就地订正这个期望值：1160.10 → 1123.75**（A110 接上嵌套布局组总量之后）。
    #    原因不是「布局算法变了」，而是**内层 `Price Display` 的宽从 0 变成了 72.7**：
    #      · 旧：内层 `Price Display` 是**布局组**，它的 `HOVLG.preferredWidth` 原来一律 `LG_UNK`
    #        ⇒ 它自己的 `ContentSizeFitter(hFit=PreferredSize)` **算不出 ⇒ 一个字节都不回写**
    #        ⇒ 它的 `sizeDelta.x` 停在模板值 **0**（所以旧读数那个框是**退化的 0 宽**）；
    #      · 新：A110 递归算出 `tot_pref = icon 56×1.2 + spacing 5.5 = 72.7` ⇒ CSF 回写
    #        `sizeDelta.x = 72.7`。
    #    ⇒ 父组给它的**落位点（枢轴/`anchoredPosition`）没变**（仍是 `1160.10`），
    #      而 `pivot.x = 0.5` ⇒ 左缘 = 落位点 − 宽/2 = **1160.10 − 36.35 = 1123.75** ✔
    #      （36.35 = 72.7/2，**正好一半** ⇒ 可反证「错的是宽、不是位置」）。
    #    ⚠️ ③a 那两条**相对**几何（`icon` 中心 = 组左+33.6、`text` 左 = 组左+72.7）不受影响、仍是绿的。
    gb = abs(gl - 1123.75) < 0.01
    ok = ok and gb
    print(f'  {"✅" if gb else "❌"} 缩放子件③b 绝对位置 · 内层 `Price Display` 左 {gl:.2f}'
          f'（要 1123.75 = 落位点 1160.10 − 宽/2；宽 {pd3["w"]:.2f} 要 72.7）'
          f'⇒ `icon` 中心 x {icx:.2f}（要 1157.35）· `text` 左 {tx3["rect"][0]:.2f}（要 1196.45）')

    # ---- ④ **`m_ReverseArrangement=1`** 的回归用例（子件按【树序倒排】）----
    # 🔴 为什么要单独一条（2026-10-05）：本文件在此之前**完全不读 `m_ReverseArrangement`** ⇒
    #    凡 `reverse=1` 的组，打出来的「名字 ↔ 位置」是**镜像**的
    #    （`项目任务.md` §三 第 29 条 A91③ 那份 ready patch 就是拿这份镜像读数推的）。
    #    前三个 fixture 的布局组**全是 `reverse=0`** ⇒ 这条路径以前**从没被验过**（同③的处境）。
    #    判据 = uGUI `HorizontalOrVerticalLayoutGroup.SetChildrenAlongAxis` 的
    #    `:153-155`（startIndex/endIndex/increment）+ `:182`（`pos` 初值）+ `:198`（循环条件）
    #    + `:216`（`pos` 无条件递增）。
    #    fixture = `Deck info Popup/Deck Options`（MB `-6741779870866962008`：`spacing=-50`、
    #    `align=5 (MiddleRight)`、**`m_ReverseArrangement=1`**、`ctrlW=0 ctrlH=0`、
    #    `expandW=1 expandH=1`、`scaleW=0 scaleH=0`、`pad=0`）——
    #    5 颗子件 `sizeDelta` 全 `(74.38600158691406, 75.6050033569336)`、pivot 全 `(0.5,0.5)`、
    #    树序（= RT `8880949758083631528` 的 `m_Children` 序）= `[Switch Deck Info Button,
    #    Duplicate Button, Share Button, Share On Chat, Delete Button]`。
    #
    #    **期望值照 uGUI 源码手算**（不是本工具算出来的）：
    #      · `tot_min = tot_pref = 5×74.38600158691406 + 4×(−50) = 171.9300079345703`
    #        （`min == pref` ⇒ `minMaxLerp = 0`）
    #      · `surplus = 519.8800048828125 − 171.9300079345703 = 347.9499969482422`；
    #        `tot_flex = 5 × max(0, 1) = 5 > 0`（`expandW=1`，**与 `ctrl=0` 无关**）
    #        ⇒ `itemFlexibleMultiplier = 69.58999938964844`、`pos` **不走 `GetStartOffset`**、仍 = `pad.left = 0`
    #      · `cell = 74.38600158691406 + 69.58999938964844 = 143.9760009765625`；
    #        `pos += cell + spacing(−50)` ⇒ 步进 **93.9760009765625**
    #      · `ctrl=0` ⇒ `offsetInCell = (cell − sizeDelta.x) × align(1.0) = 69.58999938964844`
    #        ⇒ **格 i** 的左边缘（组内）= `93.9760009765625×i + 69.58999938964844`
    #        = 69.590 / 163.566 / 257.542 / 351.518 / 445.494（末格右缘 519.880 = 组宽 ✓）
    #      · 🔴 **`reverse=1` ⇒ 树序第 i 颗落在「格 4−i」** ⇒ 树序
    #        `[Switch…, Duplicate, Share, Share On Chat, Delete]` 的组内左边缘依次
    #        = 445.494 / 351.518 / 257.542 / 163.566 / **69.590**
    #        ⇒ **`Delete Button` 是这一排最左那颗**（旧输出给的 1709.2 = 最右，正好是镜像）
    #      · 交叉轴（`ctrlH=0`、`align` 竖直半分 `0.5`、`requiredSpace = Clamp(150, 75.605, 150) = 150`）：
    #        组内顶 = `(150 − 75.6050033569336) × 0.5 = 37.1974983215332`、底 = 112.8025016784668
    #        —— **与 `reverse` 无关**（交叉轴每颗各自算 `GetStartOffset`，循环次序不影响结果）
    b4 = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    gop4 = b4.find_go('Deck Options')
    rt4 = b4.rt_of_go(gop4) if gop4 is not None else None
    if rt4 is None:
        print('  ❌ fixture ④ 取不到 `Deck Options`（**倒排那条路径没验成**）')
        return 1
    base4, _p4, sc4 = MR.parent_rect_of(b4, rt4, (0.0, 0.0, 1920.0, 1080.0))
    out4 = []
    walk(b4, mono_index(verbose=False), rt4, base4, sc4, 0, 1, out4, 0, True,
         {'by_pid': {}, 'ambiguous': {}}, {})
    grp4 = next((e for e in out4 if e['ind'] == 0), None)
    kids4 = [e for e in out4 if e['ind'] == 1]
    want4 = [('Switch Deck Info Button', 445.494), ('Duplicate Button', 351.518),
             ('Share Button', 257.542), ('Share On Chat', 163.566), ('Delete Button', 69.590)]
    if grp4 is None or len(kids4) != 5:
        print(f'  ❌ fixture ④ 取不到 `Deck Options` 的 5 颗子件'
              f'（组 {grp4 is not None}，子 {len(kids4)}/5）')
        return 1
    gx4, gy4 = grp4['rect'][0], grp4['rect'][1]
    for e, (wn, wdx) in zip(kids4, want4):
        g = (e['name'] == wn and abs(e['rect'][0] - (gx4 + wdx)) < 0.02
             and abs(e['w'] - 74.38600158691406) < 0.01
             and abs(e['rect'][1] - (gy4 + 37.1974983215332)) < 0.02
             and abs(e['rect'][3] - (gy4 + 112.8025016784668)) < 0.02)
        ok = ok and g
        print(f'  {"✅" if g else "❌"} 倒排④ 树序 `{e["name"]}` 组内左 {e["rect"][0] - gx4:8.3f}'
              f'（要 {wdx:8.3f}）· 顶 {e["rect"][1] - gy4:7.3f} · 宽 {e["w"]:.2f}'
              + ('' if e['name'] == wn else f' ⚠️ 名字也对不上（期望 `{wn}`）'))
    # 🔴 **这一条就是 A91③ 的验收锚**：树序**最后**那颗必须落在最左。
    left4 = min(kids4, key=lambda e: e['rect'][0])
    right4 = max(kids4, key=lambda e: e['rect'][0])
    gd = (left4['name'] == 'Delete Button' and right4['name'] == 'Switch Deck Info Button')
    ok = ok and gd
    print(f'  {"✅" if gd else "❌"} 倒排④ **最左 = `{left4["name"]}`、最右 = `{right4["name"]}`**'
          f'（要 `Delete Button` … `Switch Deck Info Button` = 树序反过来；'
          f'镜像读法会得到相反的一对）')

    print('✅ 布局算法与正本 §2·1 的手算值逐位一致（含纵轴对齐、缩放子件、倒排三条回归用例）' if ok
          else '❌ 与 §2·1 不一致 —— **别用这套布局结果**')

    # ---- ⑤ `rect_of` 的 `scale`（A60⑤⑨）----
    # 🔴 为什么单独一条：`scale` 原来是**死参**，前四条 fixture 的父链**缩放全是 1**
    #    ⇒ 那条路径以前**从没被验过**（同③④的处境）。期望值**照公式手算**，逐条写在下面。
    pr = (100.0, 200.0, 500.0, 800.0)          # 父：屏幕 400×600
    r5 = {'m_AnchorMin': {'x': 0.5, 'y': 0.5}, 'm_AnchorMax': {'x': 0.5, 'y': 0.5},
          'm_Pivot': {'x': 0.5, 'y': 0.5}, 'm_AnchoredPosition': {'x': 10.0, 'y': 0.0},
          'm_SizeDelta': {'x': 100.0, 'y': 50.0}}
    got, (gw, gh) = MR.rect_of(r5, pr, (1.0, 1.0))
    # 手算（scale=1）：参考点 = (100+0.5×400, 200+0.5×600) = (300,500)；枢轴 = (300+10, 500) = (310,500)
    #   布局框 = 0×400+100 = **100** × 0×600+50 = **50** ⇒ rect = (260,475)→(360,525)
    want = (260.0, 475.0, 360.0, 525.0)
    g5a = all(abs(got[i] - want[i]) < 1e-9 for i in range(4)) and abs(gw - 100) < 1e-9 and abs(gh - 50) < 1e-9
    ok = ok and g5a
    print(f'  {"✅" if g5a else "❌"} ⑤a `rect_of` scale=1 · {got[0]:.2f},{got[1]:.2f}→{got[2]:.2f},{got[3]:.2f}'
          f'（要 260.00,475.00→360.00,525.00）· 布局框 {gw:.2f}×{gh:.2f}（要 100×50）')
    got, (gw, gh) = MR.rect_of(r5, pr, (2.0, 2.0))
    # 手算（lossyScale(父)=2）：枢轴 = (300+10×**2**, 500) = (320,500)；
    #   布局框 = 0×400 + 100×**2** = **200** × 50×2 = **100** ⇒ rect = (220,450)→(420,550)
    want = (220.0, 450.0, 420.0, 550.0)
    g5b = all(abs(got[i] - want[i]) < 1e-9 for i in range(4)) and abs(gw - 200) < 1e-9 and abs(gh - 100) < 1e-9
    ok = ok and g5b
    print(f'  {"✅" if g5b else "❌"} ⑤b `rect_of` scale=2 · {got[0]:.2f},{got[1]:.2f}→{got[2]:.2f},{got[3]:.2f}'
          f'（要 220.00,450.00→420.00,550.00）· 布局框 {gw:.2f}×{gh:.2f}（要 200×100）')
    r5c = {'m_AnchorMin': {'x': 0.0, 'y': 0.0}, 'm_AnchorMax': {'x': 1.0, 'y': 1.0},
           'm_Pivot': {'x': 0.0, 'y': 0.0}, 'm_AnchoredPosition': {'x': 0.0, 'y': 0.0},
           'm_SizeDelta': {'x': 100.0, 'y': 100.0}}
    got, (gw, gh) = MR.rect_of(r5c, (0.0, 0.0, 400.0, 300.0), (2.0, 2.0))
    # 手算：拉伸锚点 + `sd=100` ⇒ 布局框 = 1×400 + 100×**2** = **600** × 1×300+100×2 = **500**
    #   ⚠️ **这一条专抓「sizeDelta 忘了乘 scale」**：错写法给 400+100 = 500 / 300+100 = 400。
    g5c = abs(gw - 600.0) < 1e-9 and abs(gh - 500.0) < 1e-9
    ok = ok and g5c
    print(f'  {"✅" if g5c else "❌"} ⑤c 拉伸锚点 + `sd=100` ⇒ 布局框 {gw:.2f}×{gh:.2f}'
          f'（要 600×500 = 400+100×2；错写法给 500×400）')
    g5d = MR.local_size(r5, pr, (2.0, 2.0)) == (100.0, 50.0) \
        and MR.parent_local_size(pr, (2.0, 2.0)) == (200.0, 300.0) \
        and MR.local_size(r5, pr, (0.0, 0.0)) is None
    ok = ok and g5d
    print(f'  {"✅" if g5d else "❌"} ⑤d 局部单位换算 · 父局部 {MR.parent_local_size(pr, (2.0, 2.0))}、本件局部 {MR.local_size(r5, pr, (2.0, 2.0))}'
          f'（要 (100,50)）；`lossyScale=0` ⇒ {MR.local_size(r5, pr, (0.0, 0.0))}（要 None，不许抛）')
    # ⑤e **回归锚**：`--no-ancestor-scale` 必须逐位复现 2026-10-05 之前的读数。
    #    判据 = 本文档「旧口径」那一列的实测值（修之前跑出来的，见 `资料/普查产出_1005/块13_工具四件.md`）。
    b5 = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    g5 = b5.find_go('Generic Multi Card Display')
    rt5 = b5.rt_of_go(g5) if g5 else None
    o5, o5b = [], []
    # 🔴 **A626②（2026-10-14）：这两处 `MR.walk` 是【按下标】读那个元组的**（下面 `e[0]/e[1]/e[3]`
    #    = 缩进 / 名字 / 布局宽）—— ⛔ 以后谁往 `menu_rect.walk` 的 `out` 元组里塞东西，
    #    **只能加在末尾**（中间插一项 ⇒ 这里**静默取错列**，`--verify-layout` 会红得莫名其妙）。
    #    📌 元组当前次序写在 `menu_rect.walk` 那个 `out.append(...)` 上面（同一条规矩、两处都标）。
    if rt5:
        base5, _n5, sc5 = MR.parent_rect_of(b5, rt5, (0.0, 0.0, 1920.0, 1080.0))
        MR.walk(b5, rt5, base5, sc5, 0, 6, o5, 0, keep_scales=True)
        base5b, _n5b, sc5b = MR.parent_rect_of(b5, rt5, (0.0, 0.0, 1920.0, 1080.0),
                                               keep_scales=False)
        MR.walk(b5, rt5, base5b, sc5b, 0, 6, o5b, 0, keep_scales=False)
    tx5 = next((e for e in o5 if e[1] == 'Text' and e[0] == 5), None)
    tx5b = next((e for e in o5b if e[1] == 'Text' and e[0] == 5), None)
    g5e = (tx5 is not None and tx5b is not None
           and abs(tx5[3] - 25693.57) < 0.05 and abs(tx5b[3] - 115.1455) < 1e-3)
    ok = ok and g5e
    print(f'  {"✅" if g5e else "❌"} ⑤e 卡片链（祖先 `CardUI Reference` scale=223.14）· '
          f'`Text` 布局宽 {tx5[3] if tx5 else float("nan"):.2f}（要 25693.57）· '
          f'`--no-ancestor-scale` 退到 {tx5b[3] if tx5b else float("nan"):.4f}（要 115.1455 = 旧口径）')

    # ---- ⑥ `layout_property` 的优先级（A60②）----
    # 🔴 期望值**照 uGUI `LayoutUtility.GetLayoutProperty` 逐行手算**（推导见 `layout_property`）。
    cells6 = [
        ([(1, 50.0), (1, 30.0)], 50.0, True, '同级 ⇒ 取 max'),
        ([(1, 50.0), (2, 30.0)], 30.0, True, '高优先级**更小也覆盖**'),
        ([(2, 50.0), (1, 30.0)], 50.0, True, '低优先级直接忽略'),
        ([(1, LG_SKIP), (0, 20.0)], 20.0, True, '`prop<0` 的 continue **不抬** maxPriority'),
        ([(0, 20.0), (1, LG_SKIP)], 20.0, True, '同上（后者根本没进循环体）'),
        ([(1, 50.0), (0, LG_UNK)], 50.0, True, '高优先级已定 ⇒ 0 级的算不出**不参与**（关键）'),
        ([(0, LG_UNK), (1, 50.0)], 50.0, True, '同上'),
        ([(1, LG_SKIP), (0, LG_UNK)], 0.0, False, 'SKIP 让位给「算不出」⇒ 不确定'),
        ([(1, 50.0), (1, LG_UNK)], 50.0, False, '同级「算不出」⇒ 值可能更大 ⇒ 不确定'),
        ([], 0.0, True, '空 ⇒ `defaultValue` = 0'),
    ]
    bad6 = [(c, layout_property(c), (w, k)) for (c, w, k, _d) in cells6
            if layout_property(c) != (w, k)]
    ok = ok and not bad6
    print(f'  {"✅" if not bad6 else "❌"} ⑥ `layout_property` 十条手算全中'
          + ('' if not bad6 else f' —— 错的是 {bad6}'))
    # ⑥b **真数据**：`bundle_menus_assets_all` 里**唯一**一个挂 ≥2 颗 `LayoutElement` 的 GO
    #     （来自全包普查 —— `Title_-8615221960607866717`，1333 颗里就这一个）
    #     ⇒ 两颗都 `m_IgnoreLayout=0` ⇒ uGUI「有一颗说不忽略就收」⇒ `_ignores_layout` **必须 False**。
    #     ⚠️ 旧写法（任意一颗说忽略就排）在这一件上**同值** —— 所以这一条抓不到口径差，
    #        真正抓口径差的是 ⑦ 的打桩用例；这一条是「真数据上确实是 False」的锚。
    b6 = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    m6 = mono_index(verbose=False)
    pid6 = next((k for k, j in b6.go.items()
                  if j.get('_name') == 'Title_-8615221960607866717'), None)
    rt6 = b6.rt_of_go(pid6) if pid6 else None
    nle6 = 0
    if rt6:
        nle6 = len([1 for _p, cl, _mb in components_of(b6, m6, b6.rt[rt6]) if cl == 'LayoutElement'])
    g6b = rt6 is not None and nle6 >= 2 and _ignores_layout(b6, m6, b6.rt[rt6]) is False
    ok = ok and g6b
    print(f'  {"✅" if g6b else "❌"} ⑥b 真数据 · `Title`（全包唯一挂 {nle6} 颗 `LayoutElement` 的 GO）'
          f' ⇒ `_ignores_layout` = '
          f'{None if rt6 is None else _ignores_layout(b6, m6, b6.rt[rt6])}'
          f'（要 False = 收进布局；两颗都 `m_IgnoreLayout=0`）')

    # ---- ⑦ `_ignores_layout` 的口径（A60⑦）----
    # uGUI `LayoutGroup.cs:60-79`：**只要有一颗 `ILayoutIgnorer` 说「不忽略」就收进来**。
    # ⚠️ 这里用**打桩**的方式直接测那条规则（真数据里**没有**混合标志的节点 ⇒ 测不出来）：
    #    `[True]` ⇒ 排 · `[False]` ⇒ 收 · `[True,False]` ⇒ **收**（旧写法给「排」）· `[]` ⇒ 收
    _saved = globals()['components_of']

    def _mk(flags):
        def _f(b, mono, rt):
            return [(str(i), 'LayoutElement', {'m_IgnoreLayout': int(v)})
                    for i, v in enumerate(flags)]
        return _f
    cases7 = [([True], True, '[True] ⇒ 排除'),
              ([False], False, '[False] ⇒ 收'),
              ([True, False], False, '[True,False] ⇒ **收**（uGUI 是 break 出去收；旧写法给「排」）'),
              ([False, True], False, '[False,True] ⇒ 收（顺序无关）'),
              ([True, True], True, '[True,True] ⇒ 排除'),
              ([], False, '[] ⇒ 没有 ignorer ⇒ 收')]
    bad7 = []
    for flags, want7, why in cases7:
        globals()['components_of'] = _mk(flags)
        got7 = _ignores_layout(None, None, {})
        if got7 is not want7:
            bad7.append((flags, got7, want7, why))
    globals()['components_of'] = _saved
    ok = ok and not bad7
    print(f'  {"✅" if not bad7 else "❌"} ⑦ `_ignores_layout` 六种标志组合（含旧写法会错的混合档）'
          + ('' if not bad7 else f' —— 错的是 {bad7}'))

    # ---- ⑧ `aspect_fields`（`AspectRatioFitter`，A60④）----
    # 期望值**照 `AspectRatioFitter.UpdateRect` 手算**（推导见 `aspect_fields` 的 docstring）。
    cases8 = [
        ((2, 4.0, (456.9613, 93.5475), (5000.0, 1000.0), (0.0, 1.0), (0.0, 1.0)),
         {'sd_x': 374.19}, '模式 2（高控宽）⇒ 只写宽：93.5475×4 − 5000×0 = 374.19'),
        ((1, 2.0, (400.0, 50.0), (1000.0, 600.0), (0.0, 0.0), (1.0, 1.0)),
         {'sd_y': -400.0}, '模式 1（宽控高）⇒ 只写高：400/2 − 600×1 = −400'),
        ((3, 1.0, (100.0, 100.0), (800.0, 600.0), (0.0, 0.0), (1.0, 1.0)),
         {'sd_x': -200.0, 'sd_y': 0.0}, '模式 3 FitInParent ⇒ (600×1<800)^True=False ⇒ 600−800 = −200'
                                        '（局部尺寸 600×600 = **内接**）'),
        ((4, 1.0, (100.0, 100.0), (800.0, 600.0), (0.0, 0.0), (1.0, 1.0)),
         {'sd_x': 0.0, 'sd_y': 200.0}, '模式 4 EnvelopeParent ⇒ ^False=True ⇒ 800−600 = 200'
                                       '（局部尺寸 800×800 = **外接**）'),
    ]
    bad8 = []
    for args, want8, why in cases8:
        f8 = aspect_fields(*args)
        for k8, v8 in want8.items():
            if f8 is None or abs(f8.get(k8, 1e30) - v8) > 1e-4:
                bad8.append((args[0], k8, f8, v8, why))
    ok = ok and not bad8
    print(f'  {"✅" if not bad8 else "❌"} ⑧ `aspect_fields` 四档手算全中'
          + ('' if not bad8 else f' —— 错的是 {bad8}'))

    # ---- ⑨/⑪ `AspectRatioFitter` 真数据端到端 ----
    # 判据分两条，**都是照 `UpdateRect` 手推出来的恒等式**（不是本工具算出来的值）：
    #   ⑨ 模式 1⇒ `SetSizeWithCurrentAnchors(Vertical, rect.width / ar)` ⇒ 算完必有
    #        `rect.height == rect.width / ar`，即 **`w == h × ar`**（屏幕框与局部框只差同一个
    #        `lossyScale(父)`，比例相同 ⇒ 可以直接比 `w`/`h`）。
    #   ⑪ 模式 2 + `ar = 1` ⇒ `rect.width = rect.height × 1` ⇒ **`w == h`**；
    #        且它读的是**布局跑之后**的 `rect.height`（父 `Price Display` 是 HLG、交叉轴给了
    #        59.8704）⇒ 结果**不等于**模板值 93.5475（等于就说明读的是模板、次序错了）。
    b11 = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    o11 = []
    g11 = b11.find_go('Daily Reward Popup')
    rt11 = b11.rt_of_go(g11) if g11 else None
    if rt11:
        base11 = MR.parent_rect_of(b11, rt11, (0.0, 0.0, 1920.0, 1080.0))
        walk(b11, mono_index(verbose=False), rt11, base11[0], base11[2], 0, 13, o11, 0, True,
             {'by_pid': {}, 'ambiguous': {}}, {})
    AR_9 = 4.88480281829834
    pd9 = [e for e in o11 if e['name'] == 'Price Display']
    bad9 = [e for e in pd9 if abs(e['w'] - e['h'] * AR_9) > 0.05]
    g9 = len(pd9) >= 2 and not bad9
    ok = ok and g9
    print(f'  {"✅" if g9 else "❌"} ⑨ 真数据 · `Price Display`（ARF 模式 1、ar={AR_9:g}）'
          f' **{len(pd9) - len(bad9)}/{len(pd9)}** 处满足恒等式 `w == h×ar`'
          f'（`UpdateRect` 直接推出来的；误差上限 0.05px）'
          + ('' if not bad9 else f' —— 破例：{[(round(e["w"], 2), round(e["h"], 2)) for e in bad9[:4]]}'))
    ic11 = [e for e in o11 if e['name'] == 'icon' and e['fit']]
    g11b = len(ic11) >= 2 and all(abs(e['w'] - e['h']) < 0.01 for e in ic11) \
        and all(abs(e['w'] - 93.54754638671875) > 1.0 for e in ic11)
    ok = ok and g11b
    print(f'  {"✅" if g11b else "❌"} ⑪ 真数据 · `Daily Reward Popup` 的 {len(ic11)} 颗 `icon`'
          f'（ARF 模式 2 + ar=1）⇒ `w == h`、且都**不等于**模板值 93.5475'
          f'（= 它读的是布局跑之后的 `rect.height`）'
          + ('' if not ic11 else f'（实测 {ic11[0]["w"]:.4f}×{ic11[0]["h"]:.4f}，'
                                 f'`⚙{",".join(ic11[0]["fit"])}`）'))

    # ---- ⑩ 旋转：四角与 AABB ----
    # 手算：布局框 (100,100)→(200,150)（100×50）、pivot (0.5,0.5)、scale 1 ⇒ 枢轴 (150,125)；
    #   绕枢轴转 **+90°**（本坐标系 y 向下 ⇒ 顺时针）⇒ 100×50 的框变成 **50×100**（宽高互换）
    #   ⇒ 四角 = (125,175)/(125,75)/(175,175)/(175,75)、AABB = **(125,75)→(175,175)**。
    _e10 = {'rect': (100.0, 100.0, 200.0, 150.0), 'w': 100.0, 'h': 50.0, 'rot': 90.0,
            'rt': {'m_Pivot': {'x': 0.5, 'y': 0.5}, 'm_LocalScale': {'x': 1.0, 'y': 1.0}}}
    pts10, ab10 = rot_corners(_e10)
    g10 = (all(abs(ab10[i] - v) < 1e-9 for i, v in enumerate((125.0, 75.0, 175.0, 175.0)))
           and abs((ab10[2] - ab10[0]) - 50.0) < 1e-9 and abs((ab10[3] - ab10[1]) - 100.0) < 1e-9)
    ok = ok and g10
    print(f'  {"✅" if g10 else "❌"} ⑩ 旋转 90° ⇒ AABB {ab10[0]:.1f},{ab10[1]:.1f}→{ab10[2]:.1f},{ab10[3]:.1f}'
          f'（要 125.0,75.0→175.0,175.0 = 100×50 转成 50×100）')
    # ⑩b `_rot_z` 的四元数双覆盖：`(z,w) = (-0.9999, -0.0404)` 与 `(0.9999, 0.0404)` 是同一个姿态
    g10b = (abs(_rot_z({'m_LocalRotation': {'z': 0.0, 'w': 1.0}})) < 1e-9
            and abs(_rot_z({'m_LocalRotation': {'z': 0.0, 'w': -1.0}})) < 1e-9
            and abs(abs(_rot_z({'m_LocalRotation': {'z': 0.7071068, 'w': 0.7071068}})) - 90.0) < 0.01
            and abs(_rot_z({'m_LocalRotation': {'z': -0.7071068, 'w': 0.7071068}}) + 90.0) < 0.01)
    ok = ok and g10b
    print(f'  {"✅" if g10b else "❌"} ⑩b `_rot_z`：`(z=.7071,w=.7071)` ⇒ '
          f'{_rot_z({"m_LocalRotation": {"z": 0.7071068, "w": 0.7071068}}):.2f}°（要 90）· '
          f'`(z=-.7071,w=.7071)` ⇒ {_rot_z({"m_LocalRotation": {"z": -0.7071068, "w": 0.7071068}}):.2f}°（要 -90）· '
          f'`w=-1` ⇒ {_rot_z({"m_LocalRotation": {"z": 0.0, "w": -1.0}}):.2f}（要 0，双覆盖）')

    # ---- ⑤f **布局组算出来的尺寸不许被父链缩放乘两次**（2026-10-06 修的回归锚）----
    # 🔴 判据（**手推，不是本工具算的**）：`Audio Settings` 是 **VLG** ⇒ 主轴 = y、**交叉轴 = x**；
    #    它 `m_ChildControlWidth=1`、`padding=0` ⇒ uGUI `SetChildrenAlongAxis` 交叉轴那一支
    #    `requiredSpace = Mathf.Clamp(innerSize, min, …)`，`innerSize = rect.size.x - 0`
    #    ⇒ 每颗子件的**宽（局部）= 组的宽（局部）**；而「屏幕 = 局部 × lossyScale(父)」
    #    ⇒ **组的屏幕宽与子件的屏幕宽必须相等**（同一个坐标系，缩放只该出现一次）。
    #    错写法（拿 `rect_of` 的**屏幕框**当局部尺寸喂进布局）⇒ 子件 `sizeDelta` 已经含了一次缩放、
    #    屏幕列再乘一次 ⇒ 子件屏幕宽 = 组屏幕宽 × **0.9**（实测 554.20 vs 615.77，正好少 10%）。
    #    ⚠️ 这一件**必须带非 1 的祖先缩放**，否则两种写法同值（本包 `lossyScale(父) = 0.9`）。
    b6f = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    g6f = b6f.find_go('Audio Settings')
    rt6f = b6f.rt_of_go(g6f) if g6f else None
    base6f, _p6f, sc6f = (None, None, (1.0, 1.0))
    if rt6f:
        base6f, _p6f, sc6f = MR.parent_rect_of(b6f, rt6f, (0.0, 0.0, 1920.0, 1080.0))
    o6f = []
    if rt6f:
        walk(b6f, mono_index(verbose=False), rt6f, base6f, sc6f, 0, 1, o6f, 0, True,
             {'by_pid': {}, 'ambiguous': {}}, {}, stats=new_stats())
    grp6 = next((e for e in o6f if e['ind'] == 0), None)
    kid6 = [e for e in o6f if e['ind'] == 1]
    g6f = (grp6 is not None and len(kid6) == 3
           and abs(sc6f[0] - 0.9) < 1e-6                       # 这一件必须带缩放，否则测不出
           and all(abs(e['w'] - grp6['w']) < 0.01 for e in kid6))
    ok = ok and g6f
    print(f'  {"✅" if g6f else "❌"} ⑤f 交叉轴 `ctrlW=1` · 组宽 {grp6["w"] if grp6 else float("nan"):.4f}'
          f'（lossyScale(父)={sc6f[0]:.4g}）⇒ 三颗子件宽 '
          f'{"/".join("%.2f" % e["w"] for e in kid6) if kid6 else "-"}（要与组宽**相等**；'
          f'把屏幕框当局部尺寸的错写法会各给 **×0.9 = {grp6["w"] * 0.9:.2f}**）')

    # ---- ⑫ **嵌套布局组的总量**（A110）----
    # 🔴 期望值 = `资料/普查产出_1005/块13_工具四件.md` §件3 **照 uGUI 手算**出来的那一组
    #    （不是本工具算的）：`Normal Missions`（HLG `spacing 0 align 0 pad 0 ctrlW=1 ctrlH=0
    #    expandW=1 expandH=1`，自身 1519×723.8）的两颗子件 ——
    #    `Special Missions`（`scl=1.15`、挂 `ContentSizeFitter(hFit=PreferredSize)` + 自己的 HLG）
    #    与 `Daily Missions`（`scl=1.15`、`LayoutElement` 只声明 `m_FlexibleWidth=120`）。
    #    `Special Missions` 的可排子件只有 `Daily Skulls Mission Container`（`Daily Login Container`
    #    是 `m_IsActive=0`），它给出 `min = pref = 347.64` ⇒ 父组：
    #      `tot_min = tot_pref = 347.64×1.15 = 399.786`、`tot_flex = (1+120)×1.15 = 139.15`、
    #      `minMaxLerp = 0`、`itemFlexibleMultiplier = (1519−399.786)/139.15 = 8.04322` ⇒
    #      格 `Special Missions = 347.64 + 1×8.04322 = 355.683`、步进 `355.683×1.15 = 409.036`；
    #      格 `Daily Missions = 347.64+120×8.04322 …`（其 `m_FlexibleWidth=120`）⇒ 宽 **965.186**。
    #    随后 `Special Missions` 自己的 CSF 把它的宽改回 `pref` 那一档 = **347.64**。
    #    ⚠️ 修之前（布局组总量一律 `LG_UNK`）工具给的是 **10.92 / 384.9** —— 差一整截。
    b12 = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    g12 = b12.find_go('Normal Missions')
    rt12 = b12.rt_of_go(g12) if g12 else None
    base12, _p12, sc12 = (None, None, (1.0, 1.0))
    if rt12:
        base12, _p12, sc12 = MR.parent_rect_of(b12, rt12, (0.0, 0.0, 1920.0, 1080.0))
    o12 = []
    if rt12:
        walk(b12, mono_index(verbose=False), rt12, base12, sc12, 0, 1, o12, 0, True,
             {'by_pid': {}, 'ambiguous': {}}, {}, stats=new_stats())
    sp12 = next((e for e in o12 if e['name'] == 'Special Missions' and e['ind'] == 1), None)
    dm12 = next((e for e in o12 if e['name'] == 'Daily Missions' and e['ind'] == 1), None)
    g12ok = (sp12 is not None and dm12 is not None
             and abs(sp12['w'] - 347.64) < 0.01 and abs(dm12['w'] - 965.186) < 0.01
             and abs((dm12['rect'][0] - sp12['rect'][0]) - 409.036) < 0.02)
    ok = ok and g12ok
    print(f'  {"✅" if g12ok else "❌"} ⑫ 嵌套布局组总量（A110）· `Special Missions` 宽 '
          f'{sp12["w"] if sp12 else float("nan"):.4f}（要 347.64 = 它的 CSF 那一档）· '
          f'`Daily Missions` 宽 {dm12["w"] if dm12 else float("nan"):.4f}（要 965.186）· '
          f'两件左缘差 {(dm12["rect"][0] - sp12["rect"][0]) if (sp12 and dm12) else float("nan"):.4f}'
          f'（要 409.036 = 355.683×1.15；修前工具给 10.92 / 384.9）')

    # ---- ⑬ `Image` 的首选尺寸（A109）----
    # 判据 = uGUI `Image.cs:1844-1879`（本地那份）：`Simple/Filled` ⇒ `rect.size / pixelsPerUnit`；
    #    `Sliced/Tiled` ⇒ `DataUtility.GetMinSize(sprite) / pixelsPerUnit` = `(border.x+border.z, …)`。
    #    `pixelsPerUnit` = `sprite.pixelsPerUnit / canvas.referencePixelsPerUnit`（后者取默认 100）。
    _mb13 = {'m_Type': 0}
    _mb13s = {'m_Type': 0, 'm_Sprite': {'m_FileID': 0, 'm_PathID': 7}}          # Simple
    _mb13t = {'m_Type': 2, 'm_Sprite': {'m_FileID': 0, 'm_PathID': 7}}          # Tiled
    _sx = {'by_pid': {'7': 'T'}, 'ambiguous': {}}
    #  假图：rect 200×100、border (10,20,30,40)、ppu 100 ⇒ `Simple` pref = (200,100)；
    #  `Sliced/Tiled` pref = ((10+30)/1, (20+40)/1) = (40,60)。
    _bx = {'T': (200.0, 100.0, (10.0, 20.0, 30.0, 40.0), 100.0, (0.0, 0.0))}
    p13a, _w13a = image_pref_size(_mb13, _sx, _bx)          # 无 sprite ⇒ (0,0)
    p13b, _w13b = image_pref_size(_mb13s, _sx, _bx)
    p13c, _w13c = image_pref_size(_mb13t, _sx, _bx)
    p13d, w13d = image_pref_size(_mb13s, _sx, {})           # 索引缺 ⇒ None + 原因
    p13e, _w13e = image_pref_size(_mb13s, _sx,
                                  {'T': (200.0, 100.0, (0, 0, 0, 0), 180.0, (0, 0))})
    g13 = (p13a == (0.0, 0.0) and p13b == (200.0, 100.0) and p13c == (40.0, 60.0)
           and p13d is None and w13d and abs(p13e[0] - 200.0 / 1.8) < 1e-6
           and abs(p13e[1] - 100.0 / 1.8) < 1e-6)
    ok = ok and g13
    print(f'  {"✅" if g13 else "❌"} ⑬ `Image` 首选尺寸 · 无图 {p13a}（要 (0,0)）· `Simple` {p13b}'
          f'（要 (200,100) = rect/ppu）· `Tiled` {p13c}（要 (40,60) = border 和/ppu）· '
          f'ppu=180 的 `Simple` {tuple(round(x, 4) for x in p13e)}（要 (111.111,55.556) = ÷1.8）· '
          f'索引缺 ⇒ {p13d}（要 None，原因 = {w13d!r}）')
    # ⑬b **真数据**：`Price Display` 的内层 HLG（`ctrlW=0 scaleW=1`）里 `icon` 那件是**无图**的
    #     Image ⇒ pref 必须 **0**（不是 UNK）—— 这条钉住「无 sprite 那支**不许**退成 UNK」。
    b13 = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    g13r = b13.find_go('ReRollPopup Variant')
    rt13 = b13.rt_of_go(g13r) if g13r else None
    o13 = []
    if rt13:
        base13, _p13, sc13 = MR.parent_rect_of(b13, rt13, (0.0, 0.0, 1920.0, 1080.0))
        walk(b13, mono_index(verbose=False), rt13, base13, sc13, 0, 5, o13, 0, True,
             {'by_pid': {}, 'ambiguous': {}}, {}, stats=new_stats())
    pd13 = next((e for e in o13 if e['name'] == 'Price Display' and e['ind'] == 5), None)
    g13b = pd13 is not None and abs(pd13['w'] - 72.7) < 0.01
    ok = ok and g13b
    print(f'  {"✅" if g13b else "❌"} ⑬b 真数据 · 内层 `Price Display`（HLG+CSF）宽 '
          f'{pd13["w"] if pd13 else float("nan"):.4f}（要 72.7 = `icon` 56×1.2 + `spacing` 5.5；'
          f'修前它的 CSF 算不出 ⇒ 宽 0）')

    # ---- ⑭ `UIScaleToFit` / `ContentSizeFitterMinMax` / `RectSizeLimiter`（A107）----
    # `usf_scale()` 三档手算（判据 = 反汇编 VA 0x180872B00）：
    #  · 父件 800×600、本件 400×300、`Flat`+padding(0,0) ⇒ ratio = min(2, 2) = **2**
    #  · `Multiplier`+padding(1.35,1.35) ⇒ 目标 = 1080×810 ⇒ ratio = min(2.7, 2.7) = **2.7**
    #  · 父件 800×600、本件 400×0（高为 0）⇒ 除法 Inf ⇒ **原版也 return**（返回 None + 原因）
    #  · 本件 800×600 与父件一样大 ⇒ ratio = 1（**会写回**，只是值等于 1）
    r14a, _ = usf_scale({'fitInParent': 1, 'paddingType': 0, 'padding': {'x': 0.0, 'y': 0.0}},
                        (800.0, 600.0), (400.0, 300.0))
    r14b, _ = usf_scale({'fitInParent': 1, 'paddingType': 1,
                         'padding': {'x': 1.35, 'y': 1.35}}, (800.0, 600.0), (400.0, 300.0))
    r14c, w14c = usf_scale({'fitInParent': 1, 'paddingType': 0, 'padding': {'x': 0.0, 'y': 0.0}},
                           (800.0, 600.0), (400.0, 0.0))
    r14d, _ = usf_scale({'fitInParent': 0, 'paddingType': 0, 'padding': {'x': 0.0, 'y': 0.0}},
                        (800.0, 600.0), (400.0, 300.0))
    # `_csf_clamp()` 手算：`sizeDelta = (500, 120)`、`widthMin/Max = 0/291.22`、
    #   `heightMin/Max = 0/150` ⇒ **(291.22, 120)**（宽被上限钳、高在界内不动）。
    _rt14 = {'m_SizeDelta': {'x': 500.0, 'y': 120.0}}
    _n14 = _csf_clamp({'clampWidth': 1, 'widthMin': 0.0, 'widthMax': 291.22,
                       'clampHeight': 1, 'heightMin': 0.0, 'heightMax': 150.0}, _rt14)
    g14 = (abs(r14a - 2.0) < 1e-9 and abs(r14b - 2.7) < 1e-9 and r14c is None and w14c
           and r14d is None and _rt14['m_SizeDelta'] == {'x': 291.22, 'y': 120.0} and len(_n14) == 1)
    ok = ok and g14
    print(f'  {"✅" if g14 else "❌"} ⑭ A107 纯函数 · `UIScaleToFit` ratio {r14a}（要 2）· '
          f'`Multiplier` {r14b}（要 2.7 = ×1.35）· 本件高 0 ⇒ {r14c}（要 None）· '
          f'`fitInParent=0` ⇒ {r14d}（要 None，原因 = {usf_scale({"fitInParent": 0, "paddingType": 0, "padding": {"x": 0, "y": 0}}, (800., 600.), (400., 300.))[1][:24]!r}）· '
          f'`CSFMinMax` 钳后 `sizeDelta` = {_rt14["m_SizeDelta"]}（要 x=291.22, y 不动）')
    # ⑭b **真数据**：`ReRollPopup Variant` 里那颗 `icon`（`m_LocalScale=1.2`）的
    #     `AspectRatioFitter` 回写后应当 `w == h`（模式 2 + ar=1）；同窗 `Button Text` 那颗挂了
    #     `ContentSizeFitterMinMax`（`clampWidth=1 widthMax=560`）⇒ `sizeDelta.x` **不许超过 560**。
    cm14 = [e for e in o13 if any(x.startswith('CSFMinMax') for x in e['fit'])]
    g14b = all(d['mb'].get('clampWidth', 0) == 0 or e['rt']['m_SizeDelta']['x'] <= 560.0 + 1e-6
               for e in cm14 for d in e['details'] if d['cls'] == 'ContentSizeFitterMinMax')
    ok = ok and g14b
    print(f'  {"✅" if g14b else "❌"} ⑭b 真数据 · `ReRollPopup Variant` 里 {len(cm14)} 处挂了 '
          f'`ContentSizeFitterMinMax`，逐处核 `sizeDelta.x ≤ widthMax`（本窗 560）')

    # ---- ⑮ `activeInHierarchy`（A108）----
    # uGUI 判的是**整条父链**（`LayoutGroup.cs:60-79` 上一句），本文件 2026-10-06 前只看 `m_IsActive`。
    # 打桩：造三件 —— 父 inactive、子 active ⇒ 子**必须** False。
    _saved_co = globals()['components_of']
    _b15 = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    _rtp, _rtc = '9001', '9002'
    _b15.rt[_rtp] = {'m_GameObject': {'m_PathID': '9001'},
                     'm_Father': {'m_PathID': 0}, 'm_Children': []}
    _b15.rt[_rtc] = {'m_GameObject': {'m_PathID': '9002'},
                     'm_Father': {'m_PathID': '9001'}, 'm_Children': []}
    _b15.go['9001'] = {'m_Name': 'P', 'm_IsActive': 0}
    _b15.go['9002'] = {'m_Name': 'C', 'm_IsActive': 1}
    _b15.__dict__.pop('_aih_cache', None)
    _v15a = _active_in_hierarchy(_b15, '9002')                # 自己 active、父 inactive ⇒ 必须 False
    _v15b = _active_in_hierarchy(_b15, '9001')
    g15 = (_v15a is False and _v15b is False
           and _active_in_hierarchy(_b15, '999999') is True)  # 查不到 ⇒ 不冤枉它
    _b15.go['9001']['m_IsActive'] = 1
    _b15.__dict__.pop('_aih_cache', None)
    _v15c = _active_in_hierarchy(_b15, '9002')
    g15 = g15 and (_v15c is True)
    # 真数据：本包「自己 active、祖先 inactive」的**可排子件**数 = 0（uGUI 与旧写法同值）
    _n15 = 0
    _b15b = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    _mono15 = mono_index(verbose=False)
    for _pid, _rt in list(_b15b.rt.items()):
        if _pid.startswith('RectTransform_') or not _rt.get('m_Father', {}).get('m_PathID'):
            continue
        if (_b15b.go.get(str(_rt.get('m_GameObject', {}).get('m_PathID'))) or {}).get('m_IsActive', 1):
            if not _active_in_hierarchy(_b15b, _rt):
                _n15 += 1
    # 🔴 「两个规则真的岔开」的那个数 = **0**（可证，见 `apply_layout_to_children` 的 docstring）：
    #    孩子是组的**直接子件** ⇒ `activeInHierarchy(孩子) = 孩子自己 ∧ activeInHierarchy(组)`
    #    ⇒ 组在跑时字面量 ≡ `m_IsActive`。下面**数一遍**把这个证明钉住（不是嘴上说）。
    #    ⚠️ 真正要出声的是**另一半**：**组自己就不在 `activeInHierarchy` 里**的那一组
    #    （本包 1710 个子件的父组如此）—— uGUI 此刻**根本不跑那个组**，本工具仍按「激活之后」算。
    _n15b = 0          # 两个规则**取值不同**的点（字面量排除、旧写法收）
    _n15c = 0          # 两边同值的点
    _n15e = 0          # ⚠️ 其中**父组自己在跑**的点 —— **必须是 0**（= 证明本身）
    _n15d = 0          # **组自己不在 activeInHierarchy 里**的 V/H 布局组数（要出声的那一半）
    _b15c = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    for _pid, _rt in list(_b15c.rt.items()):
        if _pid.startswith('RectTransform_'):
            continue
        if not [_c for _c in components_of(_b15c, _mono15, _rt)
                if _c[1].startswith('VerticalLayout') or _c[1].startswith('HorizontalLayout')]:
            continue
        _g = _active_in_hierarchy(_b15c, _rt)
        if not _g:
            _n15d += 1
        for _c in b.children(_pid):
            _k = _b15c.rt.get(str(_c))
            if _k is None:
                continue
            _self = bool((_b15c.go.get(str(_k.get('m_GameObject', {}).get('m_PathID'))) or {})
                         .get('m_IsActive', 1))
            _lit = _active_in_hierarchy(_b15c, _k)          # uGUI 的字面量（整条父链）
            if _self != _lit:
                _n15b += 1
                if _g:
                    _n15e += 1                              # ← 这一格必须为 0
            else:
                _n15c += 1
    globals()['components_of'] = _saved_co
    # 🔴 **证明本身也要断言住**（不是打印一下）：两个规则取值不同的点，**必须全部落在
    #    「父组自己就不在 activeInHierarchy 里」那一档**（`_n15e == 0`）—— 因为
    #    `activeInHierarchy(直接子件) = 自己 ∧ 组在跑` ⇒ 组在跑时字面量 ≡ `m_IsActive`。
    #    那一档本工具在**组那一层**补偿（组不跑 ⇒ 整组当 active 看）⇒ 净输出变化 = 0。
    ok = ok and g15 and (_n15e == 0)
    print(f'  {"✅" if (g15 and _n15e == 0) else "❌"} ⑮ `activeInHierarchy`（A108）· 打桩：自己 active + 父 inactive '
          f'⇒ {_v15a}（要 False）、父放开后 ⇒ {_v15c}（要 True）· '
          f'本包实测：「自己 active、祖先 inactive」的节点 **{_n15}** 个；'
          f'两个规则**取值不同**的点 **{_n15b}** 个，其中**父组自己在跑**的 = **{_n15e}** 个（要 0 ⇒ 证明成立）；'
          f'两值相同的 {_n15c} 个；真正要出声的另一半 = **{_n15d}** 个'
          f'「**组自己就不在 `activeInHierarchy` 里**」的 V/H 布局组'
          f'（uGUI 此刻不跑它，本表按「激活之后」算 ⇒ 表尾 `⛔GRP-off` 那一块）')

    # ---- ⑯ `m_Enabled=0` 在**每一类**组件上都要看得见（A98）----
    # 判据：`Image` 那一支原来就印（在 `tint` 里），而 TMP / `Text` / 布局组 / 其余**一个字不提**
    #   —— 原版 `Unlock` 那颗 `Text` 的 TMP 组件就是 `m_Enabled=0`（文本 `Debug Unlock`），
    #   那行字**原版根本画不出来**，但表里看不出来 ⇒ 有人会反过来「修」出一个原版没有的字。
    _d16a = describe('TextMeshProUGUI', {'m_text': 'Debug Unlock', 'm_fontSize': 30.0,
                                         'm_Enabled': 0}, {'by_pid': {}, 'ambiguous': {}}, {})
    _d16b = describe('TextMeshProUGUI', {'m_text': 'X', 'm_fontSize': 30.0, 'm_Enabled': 1},
                     {'by_pid': {}, 'ambiguous': {}}, {})
    _d16c = describe('Image', {'m_Sprite': {'m_PathID': 0}, 'm_Enabled': 0},
                     {'by_pid': {}, 'ambiguous': {}}, {})
    g16 = ('m_Enabled=0' in ''.join(_d16a) and 'm_Enabled=0' not in ''.join(_d16b)
           and ''.join(_d16c).count('m_Enabled=0') == 1)
    ok = ok and g16
    print(f'  {"✅" if g16 else "❌"} ⑯ `m_Enabled=0` 可见性 · TMP 禁用 ⇒ {_d16a[2]!r} '
          f'（要含 `m_Enabled=0`）· TMP 启用 ⇒ 不提 ✔ · `Image` 禁用 ⇒ 只印**一次**'
          f'（`{_d16c[1]}`）')

    # ---- ⑰ TMP 的 `pref` = `LG_UNK`（不再把「不知道」写成「0、确定」）----
    # 🔴 2026-10-06（调度台裁定）：TMP 系的 `m_minWidth/m_preferredWidth/m_flexibleWidth`（+ Height）
    #    **在本包 35014 个 MB 里一个都没有** ⇒ 旧写法 `mb.get(tmp_field, 0.0)` 恒 0 ⇒
    #    `ctrl=1` 的组按「文字宽 0」算总量、挂 TMP 的 CSF 回写 `sizeDelta = 0`（静默错）。
    #    判据 = `TMP_Text` 的 ILayoutElement 三项（`minWidth = 0` · `preferredWidth` 要**字体度量** ·
    #    `flexibleWidth = -1` ⇒ `LayoutUtility` 直接 `continue`）—— 与本文件顶部那张表一致。
    _saved_co17 = globals()['components_of']
    globals()['components_of'] = lambda _b, _m, _r: [
        ('0', 'TextMeshProUGUI', {'m_text': 'x', 'm_fontAsset': {'m_PathID': 1}})]
    c17min = _ilayout_cells(None, None, {}, 'min', 0)
    c17pref = _ilayout_cells(None, None, {}, 'pref', 0)
    c17flex = _ilayout_cells(None, None, {}, 'flex', 0)
    globals()['components_of'] = _saved_co17
    v17min, k17min = layout_property(c17min)
    v17pref, k17pref = layout_property(c17pref)
    v17flex, k17flex = layout_property(c17flex)
    g17 = (c17min == [(0, 0.0)] and c17pref == [(0, LG_UNK)] and c17flex == [(0, LG_SKIP)]
           and k17min is True and k17pref is False and k17flex is True
           and v17min == 0.0 and v17pref == 0.0 and v17flex == 0.0)
    ok = ok and g17
    print(f'  {"✅" if g17 else "❌"} ⑰ TMP 的 `pref` · cells min={c17min} / pref={c17pref} / flex={c17flex}'
          f'（要 [(0,0.0)] / [(0,UNK)] / [(0,SKIP)]）· `layout_property` 的「确定」= '
          f'{k17min}/{k17pref}/{k17flex}（pref 那一格要 **False** = 算不出）')
    # ⑰b **真数据**：`DraftLeaderboardPopup` 里 `Name Holder`（一组，子件是 TMP）——
    #     改前工具的 `tot_pref` 按「文字宽 0」算 ⇒ 标 `ok`；改后如实标 `unk`。
    b17 = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    g17r = b17.find_go('DraftLeaderboardPopup')
    rt17 = b17.rt_of_go(g17r) if g17r else None
    o17 = []
    if rt17:
        base17, _p17, sc17 = MR.parent_rect_of(b17, rt17, (0.0, 0.0, 1920.0, 1080.0))
        walk(b17, mono_index(verbose=False), rt17, base17, sc17, 0, 9, o17, 0, True,
             {'by_pid': {}, 'ambiguous': {}}, {}, stats=new_stats())
    nh17 = [e for e in o17 if e['name'] == 'Name Holder']
    g17b = bool(nh17) and all(e['est'] == 'unk' for e in nh17)
    ok = ok and g17b
    print(f'  {"✅" if g17b else "❌"} ⑰b 真数据 · `DraftLeaderboardPopup` 里 {len(nh17)} 个 `Name Holder`'
          f' ⇒ est = {sorted({e["est"] for e in nh17})}（要 ["unk"] —— 子件是 TMP、pref 算不出）')

    # ---- ⑱ **三个自定义布局组**（A127 起 · A150 收尾）----
    # 🔴 为什么单开一条：`apply_layout_to_children` 原来只认「类名以 `VerticalLayout`/`HorizontalLayout`
    #    开头」（外加 `'GridLayoutGroup' in cls` 这条**子串**）⇒ 实测本包的
    #    `EverguildLayoutGroup`(3) 与 `FlexibleGridLayout`(3) **一个都认不出来** ⇒
    #    它们的子件**停在模板位**（`steps` 那 4 个 `Weekly Mission Milestones Step` 全在 (0,0)/70×70），
    #    而表里**一个字都不提**（静默）。判据全部读自反编译，逐条写在 `_lg_is_vertical` 的 docstring 里。
    #    🔴 **2026-10-06 A150**：`EverguildLayoutGroup` 的 `evenlySpaceinBetween=1` 档**已能排**
    #    （判据 = `CalculateSpacing` 的四次调用时机 + `LayoutRebuilder` 的四阶段次序）⇒
    #    ⑱b 的期望值从「没排（`cust?`）」翻成「排了且排对」；`n == 1` 另走 `sp?`（⑱d）。
    cases18 = [
        (('EverguildLayoutGroup', {'alignment': 0, 'evenlySpaceinBetween': 0}), False,
         'alignment=0(Horizontal) + flag=0 ⇒ 横向（flag=0 时与 HOVLG **逐位等价**）'),
        (('EverguildLayoutGroup', {'alignment': 1, 'evenlySpaceinBetween': 0}), True,
         'alignment=1(Vertical) ⇒ 纵向'),
        (('EverguildLayoutGroup', {'alignment': 0, 'evenlySpaceinBetween': 1}), None,
         'flag=1 ⇒ **本组自己的三项总量**算不出（`spacing` 是运行期算的，`CalcAlongAxis` 那一帧'
         '与工具不同）⇒ `_ilayout_cells` 走 `LG_UNK`。⚠️ 但**子件落位**这一路已放行（A150 / ⑱b）'),
        (('FlexibleGridLayout', {'rows': 2, 'columns': 200}), None,
         '**另一套算法**（`bool[,] grid` 打包；三个 `SetLayout*`/`CalculateLayoutInputVertical` 是空壳）'),
    ]
    bad18 = [c for c in cases18 if _lg_is_vertical(*c[0]) != c[1]]
    ok = ok and not bad18
    print(f'  {"✅" if not bad18 else "❌"} ⑱a 自定义布局组的主轴（A127）· `EverguildLayoutGroup` '
          f'flag=0/align=0 ⇒ {_lg_is_vertical("EverguildLayoutGroup", {"alignment": 0, "evenlySpaceinBetween": 0})}'
          f'（要 False）· align=1 ⇒ {_lg_is_vertical("EverguildLayoutGroup", {"alignment": 1, "evenlySpaceinBetween": 0})}'
          f'（要 True）· **flag=1 ⇒ {_lg_is_vertical("EverguildLayoutGroup", {"alignment": 0, "evenlySpaceinBetween": 1})}'
          f'（要 None = 算不出）**· `FlexibleGridLayout` ⇒ {_lg_is_vertical("FlexibleGridLayout", {})}（要 None）'
          + ('' if not bad18 else f' —— 错的是 {bad18}'))
    b18 = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    m18 = mono_index(verbose=False)
    hits18 = {}
    for _pid18, _rt18 in list(b18.rt.items()):
        if _pid18.startswith('RectTransform_'):
            continue
        for _cp18, _cls18, _mb18 in components_of(b18, m18, _rt18):
            if _cls18 in ('EverguildLayoutGroup', 'FlexibleGridLayout', 'EverguildGridLayoutGroup'):
                hits18.setdefault(_cls18, []).append((_pid18, _mb18))
    egpid, egmb = hits18.get('EverguildLayoutGroup', [(None, None)])[0]
    gridpid, _gridmb = hits18.get('EverguildGridLayoutGroup', [(None, None)])[0]
    # (b) 🔴 **2026-10-06 A150：它现在【排】了** —— 断言从「没排」翻成「排了、而且排对了」。
    #     期望值全部**手推**（不是抄工具输出）：本组 `alignment=0`（Horizontal）、`n=4`、
    #     子件各 70×70（`ctrlW=0` ⇒ 不会被子件自己的 ILayoutElement 改）、组自己**拉伸锚点**
    #     （`aMin=(0,0) aMax=(1,1) sd=(0,0)`）⇒ 组局部宽 `W` = 父的局部宽 = 工具算出来的那个屏幕宽
    #     （父链 `lossyScale=1`，本包实测）。于是：
    #       · `spacing = (W − 4×70) / 3`（判据 = `EverguildLayoutGroup.CalculateSpacing` 反汇编）；
    #       · `m_ChildAlignment = 4 (MiddleCenter)` + 内容**刚好填满** `W` ⇒ 首个左缘 = 组左缘、
    #         末个右缘 = 组右缘（`GetStartOffset` 的 `(available − required) × 0.5` = 0）；
    #       · 相邻子件左缘差 = `70 + spacing`（uGUI `pos += childSize × sf + spacing`）。
    #     ⚠️ **本包验不了的那一格**：这 3 个实例的**序列化 `m_Spacing` 恰好等于算式输出**
    #     （组是拉伸锚点 ⇒ 组尺寸在任何一帧都一样）⇒ 「取哪一帧」这条**在本包无差别**，
    #     只能靠 ⑱c 的纯函数 + `_lg_is_vertical` 的推导钉住。**别把这个包当帧判据。**
    egkids = [b18.rt[str(_c18)] for _c18 in b18.children(egpid)] if egpid else []
    snap18 = json.dumps(egkids, sort_keys=True)
    base18, _p18, sc18 = MR.parent_rect_of(b18, egpid, (0.0, 0.0, 1920.0, 1080.0)) if egpid else (None, None, None)
    r18 = MR.rect_of(b18.rt[str(egpid)], base18, sc18)[0] if egpid else None
    est18, cls18, _mq18, _a18 = apply_layout_to_children(b18, m18, egpid, r18, sc18, egkids)
    moved18 = (json.dumps(egkids, sort_keys=True) != snap18)          # 「排了」= 真的动过
    W18 = (r18[2] - r18[0]) if r18 else 0.0
    H18 = (r18[3] - r18[1]) if r18 else 0.0
    sp18 = (W18 - 4 * 70.0) / 3.0                                     # 手推
    xs18 = [MR.rect_of(k, r18, sc18)[0][0] for k in egkids] if r18 and len(egkids) == 4 else []
    ys18 = [[MR.rect_of(k, r18, sc18)[0][1], MR.rect_of(k, r18, sc18)[0][3]] for k in egkids] \
        if r18 and len(egkids) == 4 else []
    ok18b = bool(egkids) and len(egkids) == 4 and est18 == 'ok' and cls18 == 'EverguildLayoutGroup' \
        and moved18 \
        and all(abs((xs18[i + 1] - xs18[i]) - (70.0 + sp18)) < 0.01 for i in range(3)) \
        and abs(xs18[0] - r18[0]) < 0.01 and abs((xs18[3] + 70.0) - r18[2]) < 0.01 \
        and all(abs(y[0] - (r18[1] + (H18 - 70.0) * 0.5)) < 0.01 for y in ys18) \
        and abs(sp18 - egmb.get('m_Spacing', 0)) < 0.001
    o18 = []
    if egpid:
        walk(b18, m18, egpid, base18, sc18, 0, 1, o18, 0, True,
             {'by_pid': {}, 'ambiguous': {}}, {}, stats=new_stats())
    # (c) `EverguildGridLayoutGroup`（GridLayoutGroup 的子类）仍走 `grid?` —— 判据没变
    gkids = [b18.rt[str(_c18)] for _c18 in b18.children(gridpid)] if gridpid else []
    gbase18, _gp18, gsc18 = MR.parent_rect_of(
        b18, gridpid, (0.0, 0.0, 1920.0, 1080.0)) if gridpid else (None, None, None)
    gr18 = MR.rect_of(b18.rt[str(gridpid)], gbase18, gsc18)[0] if gridpid else None
    gest18 = apply_layout_to_children(b18, m18, gridpid, gr18, gsc18, gkids)[0] if gridpid else None
    cnt18 = {k: len(v) for k, v in hits18.items()}
    g18 = (cnt18 == {'EverguildLayoutGroup': 3, 'FlexibleGridLayout': 3, 'EverguildGridLayoutGroup': 1}
           and ok18b and egmb.get(EG_FLAG) == 1
           and bool(o18) and o18[0]['est'] == 'ok'
           and gest18 == 'grid?')
    ok = ok and g18
    print(f'  {"✅" if g18 else "❌"} ⑱b 真数据（A150）· 本包三类实例数 {cnt18}（要 3/3/1）· '
          f'`EverguildLayoutGroup`（flag={egmb.get(EG_FLAG)}，{len(egkids)} 个子件）'
          f'⇒ `apply_layout_to_children` = **{est18}**（要 `ok` = **排了**）· '
          f'子件真的动过 = {moved18}（要 True —— 改前这里断的是「**没**动」）· '
          f'手推 `spacing` = {sp18:.7f}（要与序列化 `m_Spacing` {egmb.get("m_Spacing", 0):.7f} '
          f'相等 —— 本件恰好相等，见注释里的那条坑）· 四子左缘 = '
          f'{["%.2f" % x for x in xs18]}（要等距 {70.0 + sp18:.2f}、首 = 组左缘 {r18[0]:.2f}、'
          f'末右缘 = 组右缘 {r18[2]:.2f}）· 走树那一行的 est = '
          f'{o18[0]["est"] if o18 else None} · `EverguildGridLayoutGroup` ⇒ {gest18}（要 `grid?`）')

    # ---- ⑱c **`everguild_spacing` 的纯函数**（A150）----
    # 判据 = `EverguildLayoutGroup__CalculateSpacing.c` 的**逐行反汇编**（见 `everguild_spacing`）。
    # 🔴 单开一条的理由：⑱b 那三个实例「序列化值 == 算式输出」⇒ **读序列化值也能蒙对**
    #    ⇒ 必须有一条把「真的在算」钉死的断言（下面第 3 格：喂一个**假的**序列化值，
    #    输出仍必须按算式走）。**这就是「改坏法」**：
    #      · 把 `/(n − 1)` 改成 `/n` ⇒ 第 2 格 197.109 vs 262.811 ⇒ 红；
    #      · 把 `self − n·first` 写成 `self − first` ⇒ 红；
    #      · 把 `n == 1` 那档放过去（返回个数）⇒ 第 5 格红；
    #      · flag 读反 / 键名打错（大写 I）⇒ 第 1、3 格红（`flag=0` 才对序列化值照单全收）。
    num18c = {'W': 1068.4340, 'one': 70.0, 'ser': 262.8113098144531}
    # ⚠️ `W` 只取到 4 位（工具打印的精度）⇒ 算式给 262.8113333，与序列化值差 **2.35e-5**
    #    （真值 `W = 3×262.8113098 + 280 = 1068.4339294`）—— 这正好说明「本件恰好相等」是
    #    **float32 量级**的相等，不是逐位相等。容差 1e-4。
    cases18c = [
        ((0, num18c['ser'], 4, num18c['one'], num18c['W']), (num18c['ser'], None),
         'flag=0 ⇒ `CalculateSpacing` 整条 no-op ⇒ **序列化值就是运行期值**'),
        ((1, num18c['ser'], 4, num18c['one'], num18c['W']), (262.8113333, None),
         'flag=1 ⇒ (1068.434 − 4×70)/3 = 262.81133（与序列化值差 2.35e-5 = float32 舍入 + `W` 取整）'),
        ((1, 999.0, 4, num18c['one'], num18c['W']), (262.8113333, None),
         '喂**假的**序列化值 ⇒ 输出**仍按算式**（证明它不是回抄序列化值）'),
        ((1, num18c['ser'], 4, num18c['one'], 1314.9), ((1314.9 - 280.0) / 3.0, None),
         '换一档组宽（1314.9 ⇒ spacing = 1034.9/3 = **344.9667**）'
         '⇒ **序列化值不再是运行期值**（那条坑的实证）'),
        ((1, 5.0, 0, None, 100.0), (5.0, None), 'n=0 ⇒ 原版 `return`（不动 `m_Spacing`）'),
        ((1, 5.0, 1, 70.0, 70.0), (None, 'n==1'), 'n=1 ⇒ 原版 `x / 0` = ±Inf/NaN ⇒ 本工具不跟着算'),
        ((1, 5.0, 4, None, 100.0), (None, 'size?'), '首个子件尺寸恢复不出 ⇒ 出声不出数'),
    ]
    bad18c = []
    for args18c, want18c, _why18c in cases18c:
        got18c = everguild_spacing(*args18c)
        a, b_ = got18c
        wa, wb_ = want18c
        same = (a is None and wa is None) or (a is not None and wa is not None and abs(a - wa) < 1e-4)
        if not (same and b_ == wb_):
            bad18c.append((args18c, got18c, want18c, _why18c))
    ok = ok and not bad18c
    print(f'  {"✅" if not bad18c else "❌"} ⑱c `everguild_spacing` 七格手推全中（A150）· '
          f'flag=0 ⇒ {everguild_spacing(0, num18c["ser"], 4, 70.0, num18c["W"])}（要序列化值）· '
          f'flag=1 ⇒ {everguild_spacing(1, 999.0, 4, 70.0, num18c["W"])}（喂 999 也要给算式值）· '
          f'n=1 ⇒ {everguild_spacing(1, 5.0, 1, 70.0, 70.0)}（要 `(None, \'n==1\')`）'
          + ('' if not bad18c else f' —— 错的是 {bad18c}'))

    # ---- ⑱d **`n == 1` 那一档真的走 `sp?` 且子件一个字节都不动**（A150）----
    # 用**真节点**、只把 `kids` 截成 1 个（`apply_layout_to_children` 的 `kids` 是入参 ⇒ 不用打桩）。
    # ⚠️ 必须在 ⑱b **之后**跑（那时子件已经是布局位）—— 正好也验「第二次进来不会把已排的位再动一次」。
    snap18d = json.dumps(egkids[:1], sort_keys=True)
    est18d = apply_layout_to_children(b18, m18, egpid, r18, sc18, egkids[:1])[0] if egpid else None
    still18d = (json.dumps(egkids[:1], sort_keys=True) == snap18d)
    g18d = (est18d == 'sp?' and still18d)
    ok = ok and g18d
    print(f'  {"✅" if g18d else "❌"} ⑱d `n == 1` 那一档（A150）· 把 `steps` 的 `kids` 截成 1 个 ⇒ '
          f'est = **{est18d}**（要 `sp?` = 没排；原版这里是 `x / 0`）· 那一颗子件 JSON '
          f'**逐字节未变** = {still18d}（要 True —— 一个字都不许写）')

    # ---- ⑲ 布局组【跳过出厂 `F` 的子件】—— 与「组自己跑不跑」**无关**（A487）----
    # 判据 = uGUI `LayoutGroup.CalculateLayoutInputHorizontal`（**本机那份**
    #        `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/LayoutGroup.cs:52-79`）：
    #   `if (rect == null || !rect.gameObject.activeInHierarchy) continue;`
    # 本工具算的是「**激活之后**」的版面（文件头那条口径）⇒ 只把**本组**那一格当 active ⇒
    #   判据退化成「**孩子自己的 `m_IsActive`**」。
    # ⛔ 改前写的是 `(not grp_aih) or (孩子 m_IsActive)` —— 组自己不在 `activeInHierarchy` 里时
    #   **把出厂 `F` 的孩子也当 active**（那是**孩子**那一格）⇒ 那些件既占格、又把后面每一件推走。
    # 样本（真数据、本包）：`AllianceMemberVariant>GeneralDetails>Content>Alliance Rating Display`
    #   —— `Secondary Icon` 出厂 `F`（现读确认），`Main Icon` / `Individual rating value` 在它右边。
    #   改前：`Main Icon` **726.97..786.97** · 文本 **786.97..1099.96**（整排右移 **+113.58**）；
    #   改后（= 序列化矩形，`--no-layout` 与 `普查产出_0927/社交_联盟与好友页.md:271-278` 都对得上）：
    #   `Main Icon` **613.38..673.38** · 文本 **673.38..1099.96** · `Secondary Icon` 留在 **708.09**。
    # 🔴 **两档都跑**（祖先 `F` / 祖先放开）并把两次**几何**比一遍 —— 改前那两档**必然不同**，
    #   所以这一条是 A487 的真回归断言，不是「跑一下看着对」。
    # ⚠️ 「布局结果 == 序列化值」**只对这一颗成立**（这个 prefab 本来就是按「跳过 `F` 子件」排的）
    #   —— ⛔ 别把它推广成通用不变式（别的组多半不等）。
    b19 = MR.Bundle(os.path.join(BUNDLES, 'bundle_menus_assets_all'))
    m19 = mono_index(verbose=False)

    def _kid19(rtpid, nm):
        # 🔴 **A621（2026-10-14）**：「按 pid 认 GO」的残留全清掉了 —— 下面三处（本函数、
        #    `_go19`、`_rectkids19`）原来走 `b.go_name(pid)` / `b.go.get(pid)`：撞车包下
        #    `self.go[pid]` 只留得下**第一份** ⇒ 这个自检会**找错件**（然后「绿」得没有意义）。
        #    改走 `go_name_of_rt` / `go_obj_of_rt`（按**这颗 RT** 认它的 GO）。
        #    ⚠️ 本包（`bundle_menus_assets_all`）**没有撞车** ⇒ 两条路指向同一个对象、读数不变。
        for _c in b19.children(rtpid):
            _r = b19.rt.get(str(_c))
            if _r and b19.go_name_of_rt(_r.get('_pid')) == nm:
                return _r
        return None

    _rt19 = b19.rt.get(str(b19.rt_of_go(b19.find_go('AllianceMemberVariant')))) if b19.find_go('AllianceMemberVariant') else None
    _gd19 = _kid19(_rt19['_pid'], 'GeneralDetails') if _rt19 else None
    _ct19 = _kid19(_gd19['_pid'], 'Content') if _gd19 else None
    _ad19 = _kid19(_ct19['_pid'], 'Alliance Rating Display') if _ct19 else None
    # ⚠️ `_go19` 是**要就地改 `m_IsActive` 的那一件**（②/③ 两档假祖先态）⇒ 认错件 = 整个 ⑲
    #    在**别的节点**上做实验（A621：原来走 `b.go.get(pid)`，撞车包下就是这个下场）。
    _go19 = b19.go_obj_of_rt(_rt19.get('_pid')) if _rt19 else None

    def _walk19(apply19):
        _o = []
        if _ad19 is not None:
            _base19, _pr19, _sc19 = MR.parent_rect_of(b19, _ad19['_pid'], (0.0, 0.0, 1920.0, 1080.0))
            walk(b19, m19, _ad19['_pid'], _base19, _sc19, 0, 1, _o, 0, apply19,
                 {'by_pid': {}, 'ambiguous': {}}, {}, stats=new_stats())
        return {e['name']: (round(e['rect'][0], 2), round(e['rect'][2], 2)) for e in _o}, _o

    _rectkids19 = None
    if _ad19 is not None:
        # 🔴 **A621**：`go_name_of_rt`（按这颗 RT 认 GO）—— 这张名单要跟 ⑲ 的真值表**逐名对**，
        #    名字取错一个就会在「没撞车」的包里也看着像是实现错了。
        _rectkids19 = [b19.go_name_of_rt(k.get('_pid'))
                       for k in _rect_children(b19, m19, _ad19['_pid'],
                                               [k for k in (b19.rt.get(str(c)) for c in b19.children(_ad19['_pid'])) if k])[0]]
    _ser19, _ = _walk19(False)                       # ① 序列化（模板）那次 —— 布局一个字都不写
    if _go19 is not None:
        _go19['m_IsActive'] = 0                      # ② 祖先 `F`（出厂态）
        b19.__dict__.pop('_aih_cache', None)
    _ancF19, _outF19 = _walk19(True)
    if _go19 is not None:
        _go19['m_IsActive'] = 1                      # ③ 祖先放开（`grp_aih=True`）
        b19.__dict__.pop('_aih_cache', None)
    _ancT19, _outT19 = _walk19(True)
    if _go19 is not None:
        _go19['m_IsActive'] = 0                      # 还原出厂态（后面的块别再读脏值）
        b19.__dict__.pop('_aih_cache', None)
    _grpF19 = next((e.get('grpoff') for e in _outF19 if e['name'] == 'Alliance Rating Display'), None)
    _grpT19 = next((e.get('grpoff') for e in _outT19 if e['name'] == 'Alliance Rating Display'), None)
    _want19 = {'Secondary Icon': (613.38, 708.09), 'Main Icon': (613.38, 673.38),
               'Individual rating value': (673.38, 1099.96)}
    # ⚠️ 三次走树的那本 dict **含根节点自己**（`Alliance Rating Display`）⇒ 比的时候只取那三颗子件
    #    （根那一行的值本来就不该被这几档影响）。
    _only19 = lambda _d: {_k: _v for _k, _v in _d.items() if _k in _want19}   # noqa: E731
    g19 = (_rectkids19 == ['Main Icon', 'Individual rating value']       # ① 只收 active 那两个
           and _only19(_ser19) == _want19                                # ② 三档几何 == 序列化值 == 真值
           and _only19(_ancF19) == _want19                               #    （`--no-layout` 那次没被布局写过）
           and _only19(_ancT19) == _want19
           and _grpF19 is True and _grpT19 is False)                    # ③ `⛔GRP-off` 标记没被吞掉
    ok = ok and g19
    print(f'  {"✅" if g19 else "❌"} ⑲ 布局组跳过出厂 `F` 子件（A487）· `Alliance Rating Display` 的 '
          f'`rectChildren` = {_rectkids19}（要只收 active 那两个，**不认 `Secondary Icon`**）· '
          f'序列化/祖先`F`/祖先`T` 三档的子件几何 = ({_only19(_ser19) == _want19},'
          f' {_only19(_ancF19) == _want19}, {_only19(_ancT19) == _want19})（都要 True —— '
          f'改前「祖先 `F`」那一档会把 `Main Icon` 推到 **726.97**）· 三档实读 = '
          f'{_only19(_ancF19)}（要 {_want19}）· '
          f'`⛔GRP-off`：祖先`F` ⇒ {_grpF19}（要 True）、祖先`T` ⇒ {_grpT19}（要 False）')

    # ---- ⑳ 多组件节点的 `m_Enabled=0` 必须【绑到它自己那个组件】（A499）----
    # 🔴 **为什么要有这一条**：一个节点挂 ≥2 个组件时，注里后四格是**按组件并排拼起来的**
    #    （`sp`/`ti`/`tx`/`ex` 四个 `join`）⇒ **看不出哪一格属于哪个组件**，而 `m_Enabled=0`
    #    原来是**裸标记** ⇒ 读者会把「被禁」记到**另一个组件**头上。
    #    **真代价（记档）**：`资料/普查产出_1013/A表现核_块1.md` §A389 把 `holder/Image` 写成
    #    「`Image` 挂着 `m_Enabled = 0`（原版根本不画它）」—— **错**：那颗 `Image` 是 `m_Enabled: 1`
    #    （`MonoBehaviour_-5355200480893724929.json`），被禁的是同一节点上的 `Outline`
    #    （`MonoBehaviour_7171112632542172927.json`，实测两份 MB 原文）。
    # 样本（真数据、本包）：GO `-2290505579751386358`（名 `Image`）= `Image`(**启用**) + `Outline`(**禁用**)。
    # ★ **改坏法**：把 `describe()` 的标记里的 `` `{cls}` `` 去掉 ⇒ 「标记必须带类名」那一断言红；
    #   把 `multi_comp_flag` 的 `n >= 2` 条件去掉 ⇒ `n == 1` 那一条红。
    _rt20 = b19.rt.get(str(b19.rt_of_go('-2290505579751386358')))
    _det20 = []
    if _rt20:
        for _cp20, _cls20, _mb20 in components_of(b19, m19, _rt20):
            _sp20, _ti20, _tx20, _ex20 = describe(_cls20, _mb20, {'by_pid': {}, 'ambiguous': {}}, {})
            _det20.append(dict(cls=_cls20, sprite=_sp20, tint=_ti20, text=_tx20, extra=_ex20, mb=_mb20))
    _img20 = next((d for d in _det20 if d['cls'] == 'Image'), None)
    _out20 = next((d for d in _det20 if d['cls'] == 'Outline'), None)
    _on20 = lambda _d: ' '.join((_d['sprite'], _d['tint'], _d['text'], _d['extra'])) if _d else ''  # noqa: E731
    _f20 = multi_comp_flag({'details': _det20})
    # ① 真数据：`Image` 那一格**不许**有标记、`Outline` 那一格**必须有且带类名**；
    # ② `⛔MULTI-COMP` 两组件有禁用 ⇒ 出声；
    # ③ **单组件而且它就是禁用的** ⇒ **不**出声（四格同属一件、没有歧义）—— 钉住 `n >= 2` 那个门；
    # ④ 两组件但**都没禁用** ⇒ 也不出声（打桩：把 `Outline` 那份的 `m_Enabled` 改成 1）—— 钉住 `k` 那个门。
    _det20c = [_out20] if _out20 is not None else []          # `Outline` 单独一份（**它是禁用的**）
    _det20b = [_img20, dict(_out20, mb=dict(_out20['mb'], m_Enabled=1))] if (_img20 and _out20) else []
    g20 = (_img20 is not None and _out20 is not None
           and 'm_Enabled=0' not in _on20(_img20)
           and 'm_Enabled=0' in _on20(_out20) and '`Outline`' in _on20(_out20)
           and _f20 != '' and multi_comp_flag({'details': _det20c}) == ''
           and multi_comp_flag({'details': _det20b}) == '')
    ok = ok and g20
    print(f'  {"✅" if g20 else "❌"} ⑳ 多组件的 `m_Enabled=0` 绑到它自己那个组件（A499）· '
          f'真数据 GO `Image` 的两个组件 = {[d["cls"] for d in _det20]}（要 [\'Image\', \'Outline\']）· '
          f'`Image` 那格的 `m_Enabled=0` = {"有（❌ 改坏）" if "m_Enabled=0" in _on20(_img20) else "没有 ✔"}'
          f'、`Outline` 那格 = {"带类名 ✔" if "`Outline`" in _on20(_out20) else "没带类名（❌ 改坏）"}'
          f' · 行内标记 = {_f20!r}（要 `⛔MULTI-COMP(2组件·1禁用)`）· 单组件且**它自己就是禁用的** ⇒ '
          f'{multi_comp_flag({"details": _det20c})!r}（要 `\'\'` —— 没有歧义就不许出声）· 两组件都没禁用 ⇒ '
          f'{multi_comp_flag({"details": _det20b})!r}（要 `\'\'`）')

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
    ap.add_argument('--no-ancestor-scale', action='store_true',
                    help='🔴 **别当默认**（= 2026-10-05 之前的口径）：父链上的 `m_LocalScale` 一律不乘。'
                         '用途 = 读「未缩放帧」的设计值（原版 prefab 里存着动画/隐藏态，'
                         '本包 390 个 RT 的 scale 是 `(0,0)`、1476 个是 `(0.01,0.01)`）。')
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
    # 🔴 **A499 追加：GO pid 撞车必须出声**（判据只此一份 = `MR.go_coll_warning`）——
    #    撞了 ⇒ 「按 pid 认 GO」在这个目录里不够用。写 **stderr**（stdout 是表，会被
    #    `menu_redoc.py` 切块 ⇒ 别污染它）。
    MR.go_coll_warning(b)
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
        # 🔴 **A499 追加：走 `find_rt`**（pid 撞车时另一份 GO 不在 `find_go` 那张索引里 ⇒
        #    按名字找会整个找不到；实例 `bundle_scenes_scenes_mainmenuwarpforge` 的 `Resource Counter Item`）。
        rtpid = b.find_rt(args.root)
        if rtpid is None:
            sys.exit(f'找不到 GameObject「{args.root}」—— 或它没有 RectTransform')

    keep = not args.no_ancestor_scale
    base_rect, pname, base_scale = MR.parent_rect_of(b, rtpid, root_rect, keep_scales=keep)
    if pname:
        print(f'# （已沿 `m_Father` 爬父链：被查节点的父 = 「{pname}」 '
              f'{base_rect[0]:.2f},{base_rect[1]:.2f} → {base_rect[2]:.2f},{base_rect[3]:.2f}）')
        if abs(base_scale[0] - 1) > SCL_EPS or abs(base_scale[1] - 1) > SCL_EPS:
            print(f'# ⚠️ 父链上有 `m_LocalScale` ⇒ `lossyScale(父)` = {base_scale[0]:.4g},'
                  f'{base_scale[1]:.4g}，下面的矩形都按它换算（A60⑤⑨ 那条修复）')
    if not keep:
        base_scale = (1.0, 1.0)
        print('# 🔴 `--no-ancestor-scale`：父链上的 `m_LocalScale` **一律不乘**'
              '（= 2026-10-05 之前的口径）')

    sidx = {'by_pid': {}, 'ambiguous': {}} if args.no_sprite else sprite_index()
    bidx = {} if args.no_sprite else border_index()

    stats = new_stats()
    out = []
    walk(b, mono, rtpid, base_rect, base_scale, 0, args.depth, out, 0,
         not args.no_layout, sidx, bidx,
         force_root_rect=(root_rect if args.root_size else None), stats=stats, keep_scales=keep)

    ox, oy = (out[0]['rect'][0], out[0]['rect'][1]) if (args.relative and out) else (0.0, 0.0)

    def nm(e):
        return e['name']

    # 🔴 **A145（2026-10-07）：两列读数的口径写在表头**（原来只散在文末的若干警告块里、而且只覆盖了
    #    其中的一半）—— 抄表的人第一眼就该看到「哪些列不是 prefab 字段 / 不是画出来的大小」。
    #    ⚠️ 两种模式都打；`--md` 那支用 `#` 开头是**故意的**（`工具/menu_redoc.py` 按
    #    「`#`/`|`/`⚠️`/`🔴` 开头 + 缩进 + 空行」切表区，换成别的开头会把表区**截断**）。
    read_note = (
        '# 🔴 读数口径（A145）：① 「宽×高」「绝对矩形」= **布局框**（设计值）；画出来的是 '
        '**布局框 × 这一件自己的 `m_LocalScale`** ⇒ 表里那一格 `×1.2 → 视觉 …`（缩放 1 的行 = `—`）。\n'
        '#    ② 「锚点 / anchoredPosition / sizeDelta」三列，对**被布局组管的节点**是'
        '**本工具按 uGUI 算法算出的【布局后】值、不是 prefab 字段** '
        '（原地写回点 `apply_layout_to_children():1878-1929` · `apply_self_fitters():1348/1508-1515` · '
        '`_set_size_axis():1272`）⇒ **拿这三列的数去 prefab JSON 里纯数值搜索搜不到**；'
        '要 prefab 原值用 `--no-layout` 或 `工具/menu_rect.py`。\n'
        '#    ③ 🔴 **一个节点挂多个组件时，后四格是「按组件并排拼起来」的**（sp/tint/文字/其它四格各是'
        '一次 `join`）⇒ **看不出哪一格属于哪个组件**。凡出现 `⛔MULTI-COMP(…)` 的行都属此列：'
        '此时 `m_Enabled=0` 那条**只说明「有一个组件被禁」**、且**已经带上类名**'
        '（`（`Outline` 组件被禁）`），⛔ **别把它记到别的组件头上**（A499 追加）。')

    if args.md:
        print(read_note)
        print('| 缩进 | 名字 | 绝对矩形 x1,y1→x2,y2 | 宽×高 | 局部缩放→视觉框 | 锚点 min→max | pivot | '
              'anchoredPosition | sizeDelta | act | 组件（类名） | sprite（名 + 原尺寸 + 九宫格） | '
              '贴图模式/颜色 | 文字（字号/对齐/色） | 其它参数 |')
        print('|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|')
        for e in out:
            if args.active_only and not e['active']:
                continue
            rt = e['rt']
            a, aM, p = rt['m_AnchorMin'], rt['m_AnchorMax'], rt['m_Pivot']
            pos, sd = rt['m_AnchoredPosition'], rt['m_SizeDelta']
            r = e['rect']
            sx, sy = _scl_of(rt)
            sp = ' / '.join(d['sprite'] for d in e['details'] if d['sprite'])
            ti = ' / '.join(d['tint'] for d in e['details'] if d['tint'])
            tx = ' / '.join(d['text'] for d in e['details'] if d['text'])
            ex = ' ; '.join(d['extra'] for d in e['details'] if d['extra'])
            mark = '' if e['est'] in (None, 'ok') else f' ⚠️{e["est"]}'
            # 🔴 **A499 追加**：≥2 个组件且有禁用的 ⇒ 行内出声（判据只此一份 = `multi_comp_flag`；
            #    本行那条 `m_Enabled=0` 因此**只属于被禁的那一个组件**，别记到别的组件头上）。
            mm = multi_comp_flag(e)
            multm = f' **{mm}**（后四格是按组件并排拼的）' if mm else ''
            fitm = '' if not e['fit'] else f' ⚙{" ; ".join(e["fit"])}'
            rotm = '' if not e['rot'] else f' ↻rot={e["rot"]:.2f}°'
            if e.get('rot_xy'):
                rotm += f' ↻xy={e["rot_xy"]}（**只绕 x/y 转，本工具不重算**）'
            if e.get('anc_off'):
                rotm += ' **⛔ANC-off**（祖先 inactive ⇒ 原版不画它）'
            if e.get('grpoff'):
                rotm += ' **⛔GRP-off**（这个布局组自己不在 `activeInHierarchy` 里：原版此刻不跑它，本表给的是「激活之后」的值）'
            lsm = '' if _scl_is_one(*e['lossy']) else f' ⇲ls={e["lossy"][0]:.4g},{e["lossy"][1]:.4g}'
            print(f'| {"·" * e["ind"]}{e["ind"]} | {_md(nm(e))} '
                  f'| {r[0] - ox:.2f},{r[1] - oy:.2f}→{r[2] - ox:.2f},{r[3] - oy:.2f} '
                  f'| {e["w"]:.2f}×{e["h"]:.2f} | {_scl_cell(e, sx, sy)}{rotm}{lsm} '
                  f'| ({a["x"]:g},{a["y"]:g})→({aM["x"]:g},{aM["y"]:g}) '
                  f'| ({p["x"]:g},{p["y"]:g}) | ({pos["x"]:g},{pos["y"]:g}) '
                  f'| ({sd["x"]:g},{sd["y"]:g}) | {"T" if e["active"] else "**F**"} '
                  f'| {_md(",".join(d["cls"] for d in e["details"]))}{mark}{multm}{fitm} '
                  f'| {_md(sp)} | {_md(ti)} | {_md(tx)} | {_md(ex)} |')
    else:
        print(f'# {args.root or args.rt}  ' + ('相对根左上角' if args.relative else '绝对矩形')
              + '（1920×1080 · 左上原点 · y 向下）')
        print(f'# 出处 {path}')
        print(f'# ⚠️ 「组件（类名）」那一列**不截断**（长类名会把右边的列推走、按 ` | ` 找字段）；')
        print(f'#    最后一列「参数」上限 **{BODY_MAX} 字符**，被砍时会印 `……[+N]`（N = 被砍掉的字符数）。')
        print(f'# 行内标记：`⚠️某某` = 布局组那一档（`unk` = **排了**但量算不准 · `grid?`/`axis?`/`cust?`/`sp?` = '
              f'**本工具没排**、那些子节点还是模板位 —— 表尾有逐条清单）· '
              f'`⚙…` = 该件挂了自适配组件（`ILayoutSelfController`：CSF/ARF/**CSFMinMax**/**RSL**/**USF**）· '
              f'`↻rot=` = 自带 z 旋转（矩形是**未旋转帧**）· `↻xy(x)` = **只绕 x/y 转**（本工具不重算）· '
              f'`⇲ls=` = **父链上有缩放**，矩形已按它换算 · '
              f'`ANC✗` = 自己 active 但**祖先 inactive**（原版不画它）· '
              f'`⛔GRP-off` = 这个**布局组自己**不在 `activeInHierarchy` 里（原版此刻不跑它，本表按「激活之后」算）· '
              f'`⛔MULTI-COMP(n组件·k禁用)` = 这一行挂了 **n 个组件**、其中 **k 个 `m_Enabled=0`** ⇒ '
              f'后四格是**按组件并排拼的**，那条 `m_Enabled=0` **只属于被禁的那一个组件**（表里带类名）')
        print(read_note)
        print(f'{"深":<3}{"名字":<38}{"x1":>8}{"y1":>8}{"x2":>8}{"y2":>8}'
              f'{"宽":>8}{"高":>8}  {"act":<5}{"组件（类名）":<30}参数')
        for e in out:
            if args.active_only and not e['active']:
                continue
            r = e['rect']
            # 🔴 **A145（2026-10-07）**：这一格原来只印**系数**（`scl=1.15`）⇒ 与 `--md` 那一列**不等价**
            #    （那边给的是「布局框 → 视觉框」）⇒ 看文本表的人算不出画出来多大。现在
            #    **文本模式 / `--md` 表 / `menu_rect.py` 三种输出共用 `menu_rect.visual_cell` 这一份判据**。
            sx, sy = _scl_of(e['rt'])
            vc = MR.visual_cell(sx, sy, e['w'], e['h'])
            sc = '' if vc == '—' else f' 视觉框={vc}'
            cls = ','.join(d['cls'] for d in e['details'])
            mark = '' if e['est'] in (None, 'ok') else f'⚠️{e["est"]} '
            fitm = '' if not e['fit'] else f'⚙{",".join(e["fit"])} '
            rotm = '' if not e['rot'] else f'↻{e["rot"]:.2f}° '
            axym = '' if not e.get('rot_xy') else f'↻xy({e["rot_xy"]}) '
            ancm = '' if not e.get('anc_off') else 'ANC✗ '
            grpm = '' if not e.get('grpoff') else '⛔GRP-off '
            # 🔴 **A499 追加**：≥2 个组件且有禁用的 ⇒ 行内出声（判据只此一份 = `multi_comp_flag`）——
            #    本行后四格是按组件并排拼的，那条 `m_Enabled=0` **只属于被禁的那一个组件**。
            mm = multi_comp_flag(e)
            multm = f'{mm} ' if mm else ''
            lsm = '' if _scl_is_one(*e['lossy']) else f'⇲ls={e["lossy"][0]:.4g},{e["lossy"][1]:.4g} '
            body = ' | '.join(x for x in (
                ' / '.join(d['sprite'] for d in e['details'] if d['sprite']),
                ' / '.join(d['tint'] for d in e['details'] if d['tint']),
                ' / '.join(d['text'] for d in e['details'] if d['text']),
                ' ; '.join(d['extra'] for d in e['details'] if d['extra']),
            ) if x)
            # 🔴 **2026-10-05：类名那一列不再截断**（A60③）。原来写 `cls[:29]` ⇒ 文本模式**静默**丢字符，
            #    超过 29 字符的长类名（实测 `Deck Editing Menu/Done` 有 **191** 字符、9 个组件）
            #    后半个组件表在输出里**根本看不出来被砍过**，还留下一条假纪律
            #    「核类名必须用 `--md`」（见 `资料/待办判据_阶段二与联机.md` A60，**那条已作废**）。
            #    `--md` 一直是全的 ⇒ 现在两种模式同口径。列宽 30 只是**最小宽度**，
            #    超长的行会把 `mark`/`sc`/`body` 往右推 —— 按 ` | ` 找字段即可。
            body_show = body if len(body) <= BODY_MAX \
                else f'{body[:BODY_MAX]}……[+{len(body) - BODY_MAX}]'
            print(f'{e["ind"]:<3}{"  " * e["ind"] + e["name"]:<38}'
                  f'{r[0] - ox:>8.1f}{r[1] - oy:>8.1f}{r[2] - ox:>8.1f}{r[3] - oy:>8.1f}'
                  f'{e["w"]:>8.2f}{e["h"]:>8.2f}  {"" if e["active"] else "INACT":<5}'
                  f'{cls:<30}{mark}{multm}{fitm}{rotm}{axym}{ancm}{grpm}{lsm}{sc} {body_show}')

    shown = [e for e in out if not (args.active_only and not e['active'])]

    # ---- ① **本表不全** 的两条：深度截断 / 纯 Transform 子件 ----
    # 🔴 2026-10-05 补：这两个原来**一个字都不说**（`walk` 静默 return）⇒ 照表搭树会少件/少层，
    #    而且看不出来。判据 = 本文件头部纪律「不许静默失败」。
    if stats['cut_nodes']:
        print(f'\n🔴 **本表不全：`--depth {args.depth}` 把子树截断了** —— 还有 '
              f'**{stats["cut_nodes"]}** 个节点没印（那儿最深到第 **{stats["cut_deepest"]}** 层）'
              f'⇒ 要看全用 `--depth {stats["cut_deepest"]}`（**没印 ≠ 不存在**）：')
        for ind, nm_, d in stats['cut_tops']:
            print(f'    {"  " * ind}{nm_}')
        if stats['cut_roots'] > len(stats['cut_tops']):
            print(f'    …… 被截掉的第一层共 {stats["cut_roots"]} 处')
    if stats['t_kids']:
        print(f'\n⚠️ 另有 **{stats["t_kids"]}** 个纯 `Transform` 子件（3D，例：卡片的 3D 体）'
              f'**不在本表里** —— 它们没有 RectTransform，本来就不属于这张 UI 表（不是没查到）。')
    if stats['miss'] > stats['t_kids']:
        print(f'\n🔴 **{stats["miss"] - stats["t_kids"]}** 个子 pid 在 `RectTransform/` 与 '
              f'`Transform/` 里**都查不到** —— 那是**真缺件**（导出可能不全），别当成「3D 件」略过。')

    # ---- ② **布局框 ≠ 视觉框**（自带 `localScale` 的件）----
    # 🔴 2026-10-05 补：表里「宽×高」「绝对矩形」印的是**布局框**，画出来要乘这一件自己的
    #    `m_LocalScale`。文本模式原来只有个 `scl=`，`--md` 表**连那个都没有** ⇒ 极易抄错
    #    （实例：`icon` 布局 56 ⇒ 实际 67.2；`Special Missions` 布局 660.43 ⇒ 实际 759.5）。
    # 🔴 **A145（2026-10-07）**：现在**两种模式的行末都给「布局框 → 视觉框」**（同一个
    #    `menu_rect.visual_cell`），块首也补了一句直说 —— 这一块留作**清单**（谁要乘、乘多少）。
    sc_nodes = [e for e in shown if not _scl_is_one(*_scl_of(e['rt']), eps=SCL_WARN_EPS)]
    if sc_nodes:
        print(f'\n⚠️ **上面「宽×高」「绝对矩形」是【布局框】，不是画出来的大小** —— '
              f'这 {len(sc_nodes)} 处自带 `localScale`，**视觉框 = 布局框 × localScale**'
              f'（两种模式的行末都逐行标着 `视觉框=`）：')
        for e in sc_nodes[:SCL_WARN_MAX]:
            sx, sy = _scl_of(e['rt'])
            vw, vh = MR.visual_size(sx, sy, e['w'], e['h'])   # 🔴 A145：算式只此一处
            print(f'    {"  " * e["ind"]}{e["name"]}  布局 {e["w"]:.2f}×{e["h"]:.2f}'
                  f'  ×{sx:.4g}' + ('' if abs(sx - sy) < 1e-9 else f',×{sy:.4g}')
                  + f'  ⇒ 视觉 {vw:.2f}×{vh:.2f}')
        if len(sc_nodes) > SCL_WARN_MAX:
            print(f'    …… 还有 {len(sc_nodes) - SCL_WARN_MAX} 处（行末「布局框→视觉框」那一格'
                  f'逐行都标了；阈值 = 差 0.1% 以上）')

    # ---- ③ **父链上有缩放**（`lossyScale(父) ≠ 1`）----
    # 🔴 2026-10-05 补（A60⑤⑨）：`rect_of` 修好之后，凡父链上有 `m_LocalScale` 的件，
    #    它的「绝对矩形」都是**按 `lossyScale(父)` 换算过**的屏幕像素（不再是「当缩放=1」的值）。
    #    逐行标 `⇲ls=`，这里再单列一块 —— 因为**这一批数字与 2026-10-05 之前不可直接比**。
    lsn = [e for e in shown if not _scl_is_one(*e['lossy'])]
    if lsn:
        zer = [e for e in lsn if abs(e['lossy'][0]) < 1e-3 or abs(e['lossy'][1]) < 1e-3]
        print(f'\n⇲ **{len(lsn)} 处的父链上有 `m_LocalScale`（逐行标了 `⇲ls=`）** —— '
              f'这些行的「绝对矩形」「宽×高」是**按 `lossyScale(父)` 换算成的屏幕像素**，'
              f'与 2026-10-05 之前的读数**不可直接比**。')
        print(f'   · 要**未缩放帧**的设计值（亚像素版面常用这个）就把它们**除以 `ls=`**；'
              f'`menu_rect.py --no-ancestor-scale` 可直接整份按旧口径算。')
        if zer:
            print(f'   🔴 **其中 {len(zer)} 处的祖先缩放≈0（或极小）** —— 它们下面整棵子树在本表里'
                  f'被压成 0 / 极小，**那是原版 prefab 里存下的姿态**（多半是动画/隐藏态），'
                  f'不是「这件没有尺寸」：')
            for e in zer[:8]:
                sx, sy = _scl_of(e['rt'])
                print(f'      {"  " * e["ind"]}{e["name"]}  ls={e["lossy"][0]:.4g},{e["lossy"][1]:.4g}'
                      f'  ⇒ 未缩放帧的布局框 = {e["w"] / (e["lossy"][0] or 1):.2f}×'
                      f'{e["h"] / (e["lossy"][1] or 1):.2f}')
            if len(zer) > 8:
                print(f'      …… 还有 {len(zer) - 8} 处')

    # ---- ④ **自适配组件**（`ContentSizeFitter` / `AspectRatioFitter`）----
    # 🔴 2026-10-05 补（A60④）：这两件**直接改写自己这个 RectTransform 的尺寸**，本文件原来
    #    一个字都不读 ⇒ 表里给的是**模板值**。现在逐行标 `⚙…`，这里再交代清楚。
    fitn = [e for e in shown if e['fit']]
    if fitn:
        print(f'\n⚙ **{len(fitn)} 处挂了自适配组件（`ContentSizeFitter`/`AspectRatioFitter`）** —— '
              f'表里的尺寸是**回写之后**的值（逐行标了 `⚙`）：')
        for e in fitn[:10]:
            print(f'    {"  " * e["ind"]}{e["name"]}  {", ".join(e["fit"])}'
                  f'  ⇒ {e["w"]:.2f}×{e["h"]:.2f}')
        if len(fitn) > 10:
            print(f'    …… 还有 {len(fitn) - 10} 处')
    fun = [e for e in shown if e['fit_unk']]
    if fun:
        print(f'\n🔴 **{len(fun)} 处挂了自适配组件、但目标值算不出**（要 Unity 的字体度量或图集尺寸，'
              f'或它自己就是布局组）—— **这些件的尺寸仍然是模板值，别照抄**：')
        for e in fun[:10]:
            print(f'    {"  " * e["ind"]}{e["name"]}  {", ".join(e["fit"])}')
        if len(fun) > 10:
            print(f'    …… 还有 {len(fun) - 10} 处')

    # ---- ⑤ **`m_LocalRotation` 的 z ≠ 0** ----
    # 🔴 2026-10-05 补（A60⑩）：本文件原来**完全不读**这个字段 ⇒ 转过 90° 的件在表里
    #    跟没转过的一模一样、**一个字都不提**。本工具**不重算旋转后的位置**（那要一条有向框链），
    #    但把「转了、转多少、四角实际在哪」全给出来，**不静默**。
    rotn = [e for e in shown if e['rot']]
    if rotn:
        print(f'\n↻ **{len(rotn)} 处带 `m_LocalRotation`（绕 z ≠ 0）** —— 表里那几行的「绝对矩形」'
              f'是**未旋转帧**的布局框（`rectTransform.rect` 本身就不含旋转），'
              f'**画出来的是一块转了 `rot` 度的四边形**。旋转中心 = **枢轴**；'
              f'下表的四角与 AABB 是**视觉框**（= 布局框 × 本件 `m_LocalScale`）转过之后的结果：')
        for e in rotn[:12]:
            pts, ab = rot_corners(e)
            print(f'    {"  " * e["ind"]}{e["name"]}  rot={e["rot"]:.2f}°  →  AABB '
                  f'{ab[0]:.1f},{ab[1]:.1f}→{ab[2]:.1f},{ab[3]:.1f}'
                  f'（{ab[2] - ab[0]:.1f}×{ab[3] - ab[1]:.1f}）'
                  f'  四角 ' + ' '.join(f'({p[0]:.1f},{p[1]:.1f})' for p in pts))
        if len(rotn) > 12:
            print(f'    …… 还有 {len(rotn) - 12} 处（`--md` 表里逐行标了 `↻rot=`）')
    if stats['rot_sub']:
        print(f'\n🔴 **另有 {stats["rot_sub"]} 个节点落在旋转件【下面】** —— 它们的绝对坐标'
              f'**没按父级的旋转重算**（本工具只标不转）⇒ 那几个数字**别照抄**：')
        for e in [x for x in shown if x['rot_anc'] and not x['rot']][:8]:
            print(f'    {"  " * e["ind"]}{e["name"]}')
        print(f'    （⚠️ `bundle_menus_assets_all` 实测**一个都没有** —— 190 个旋转件全是叶子；'
              f'这一块是给别的包/别的 depth 留的保险）')

    # ---- ⑥ **`UIScaleToFit`：运行期改写自己的 `localScale`**（A107）----
    # 🔴 2026-10-06 补：`Fit()` 把 `localScale` 改成 `min(目标宽/本件宽, 目标高/本件高)`
    #    ⇒ **序列化值不是运行时值**，而且它**连带改了整棵子树**（`lossyScale` 传播）。
    #    表里逐行标 `⚙USF …`，这里再单列 —— 因为**这一档的「局部缩放」列与 `⇲ls=` 列都被它改过**。
    usfn = [e for e in shown if any(x.startswith('USF ×') for x in e['fit'])]
    if usfn:
        print(f'\n🔴 **{len(usfn)} 处挂了 `UIScaleToFit`，它的 `localScale` 是【运行期算出来的】'
              f'（序列化值不算数）** —— 判据 = 反汇编 `Fit()`（VA `0x180872B00`）：'
              f'`ratio = min(targetRect.rect.w / 本件rect.w, targetRect.rect.h / 本件rect.h)`，'
              f'`fitInParent=1` 时 `targetRect` = **父件**；`paddingType` 0=Flat（+padding）/1=Multiplier（×padding）；'
              f'最后 `transform.localScale = (ratio, ratio, ratio)`：')
        for e in usfn[:12]:
            r = [x for x in e['fit'] if x.startswith('USF ×')][0]
            sx, sy = _scl_of(e['rt'])
            print(f'    {"  " * e["ind"]}{e["name"]}  {r}  ⇒ `m_LocalScale` 现在按它算'
                  f'（本行「局部缩放→视觉框」= ×{sx:.4g}）')
        if len(usfn) > 12:
            print(f'    …… 还有 {len(usfn) - 12} 处（`--md` 表里逐行标了 `⚙USF`）')

    # ---- ⑦ **祖先 inactive**（A108）----
    # 🔴 2026-10-06 补：`m_IsActive` 只管**自己那一格**，而 uGUI / 渲染判的是
    #    **`activeInHierarchy`（整条父链）** ⇒ 这些件**原版一个像素都不画**。
    #    `walk` 故意会走进 inactive 子树把它们列出来（要如实列出），但**必须标出来** ——
    #    否则读表的人会把它们当成「可见」的。
    ancn = [e for e in shown if e.get('anc_off')]
    if ancn:
        print(f'\n🔴 **{len(ancn)} 处「自己 `m_IsActive=1`、但祖先里有 inactive」**（逐行标了 `ANC✗`）'
              f'—— **uGUI 与渲染判的都是 `activeInHierarchy`（整条父链）**，'
              f'所以这些件**原版根本不画**（它们进不了任何布局组的 `rectChildren`，也不参与任何渲染）：')
        for e in ancn[:12]:
            print(f'    {"  " * e["ind"]}{e["name"]}')
        if len(ancn) > 12:
            print(f'    …… 还有 {len(ancn) - 12} 处')
        if stats.get('anc_off'):
            print(f'    （本次走树共 {stats["anc_off"]} 处；本表这一块按 `--active-only` 过滤后的行数算）')

    # ---- ⑧ **只绕 x/y 转**（A110 尾巴）----
    # 🔴 2026-10-06 补：本文件只建模了**绕 z** 的旋转；`(x=-1,w=0)` 这种**翻转 180°** 的件
    #    在表里和没转过的一模一样 —— **转了却报成没转 = 静默失败**。
    xyn = [e for e in shown if e.get('rot_xy')]
    if xyn:
        print(f'\n↻ **{len(xyn)} 处只绕【x/y】转（z = 0）**（逐行标了 `↻xy(…)`）—— 本工具**只建模 z 旋转**，'
              f'这些件的「转了 180°/倾斜」不会体现在任何一列里（矩形、四角、AABB 全是未旋转帧）：')
        for e in xyn[:12]:
            q = e['rt'].get('m_LocalRotation') or {}
            print(f'    {"  " * e["ind"]}{e["name"]}  四元数 x={q.get("x"):g} y={q.get("y"):g} '
                  f'z={q.get("z"):g} w={q.get("w"):g}')
        if len(xyn) > 12:
            print(f'    …… 还有 {len(xyn) - 12} 处')
        print('    ⚠️ 这一档**只报不改**（与 z 那一档同口径）：要正确的框得按有向框链重算，本工具不做。')

    # ---- ⑨ **布局组自己不在 `activeInHierarchy` 里**（A108 的「另一半」）----
    # 🔴 2026-10-06 补：uGUI 收孩子的判据是 `!rect.gameObject.activeInHierarchy`（**整条父链**）。
    #    本工具**故意**把 inactive 子树也排（要的是「激活之后」的版面）—— 那一档下**必须出声**：
    #    原版此刻**不跑这个组的布局**，表里那些子件的坐标是「把它打开之后」才会出现的值。
    #    实测本包有 **1710 个子件**挂在这种组下面（例：`Deck Editing Menu` 的
    #    `Deck Information cost drawer` 整条抽屉 `m_IsActive=0`）。
    grpn = [e for e in out if e.get('grpoff')]
    if grpn:
        print(f'\n⛔ **{len(grpn)} 个布局组【自己不在 `activeInHierarchy` 里】**（逐行标了 `⛔GRP-off`）—— '
              f'uGUI 此刻**根本不跑它们的布局**（收孩子那一句是 `!rect.gameObject.activeInHierarchy`，'
              f'整条父链）；本表按「**把它们激活之后**」算，所以下面那些子件的坐标**不是**这一刻的原版状态：')
        for e in grpn[:12]:
            print(f'    {"  " * e["ind"]}{e["name"]} → {e["lgcls"]}')
        if len(grpn) > 12:
            print(f'    …… 还有 {len(grpn) - 12} 个')
        print(f'    （本次走树共 {stats["grp_off"]} 个；`--no-layout` 时恒为 0 —— 那种模式本来就没算布局）')

    lg = [e for e in out if e['est']]
    laid = [e for e in lg if e['est'] in ('ok', 'unk')]
    nolaid = [e for e in lg if e['est'] not in ('ok', 'unk')]
    if laid:
        print('\n⚠️ 布局组（子节点位置**由布局算**，上面已是**布局跑之后**的值）：')
        for e in laid:
            print(f'    {"  " * e["ind"]}{e["name"]} → {e["lgcls"]} [{e["est"]}]')
    # 🔴 **2026-10-06 A127 拆出这一块**：原来这整张清单顶着上面那句「子节点位置**由布局算**」——
    #    而 `grid?` / `axis?` / `cust?` 三档的字面意思恰恰是「**没排**」（子件停在模板位）。
    #    认出来的自定义布局组越多，那个错句坑的人越多（A127 一次就多认 6 个），所以拆开写明。
    if nolaid:
        why = {'grid?': '网格系（Grid 算法与 HOVLG 不同，本工具不套）',
               'axis?': '类名没解出（指纹兜底）⇒ **不猜主轴**',
               'cust?': '类名解出了、但算法不是 HOVLG 那一套（自定义打包器 / `spacing` 运行期才算）',
               'sp?': '`EverguildLayoutGroup` 的 `CalculateSpacing()` 算不出（**只有 `n == 1` 一种情形**：'
                      '原版是 `(本组尺寸 − 1×首个子件尺寸) / 0` ⇒ ±Inf/NaN，**原版自己就没有确定版面**）'}
        print(f'\n🔴 **下面这些布局组本工具【没排】**（{len(nolaid)} 个）—— 它们那些子节点的坐标'
              f'**仍是模板位**（prefab 里存的 `m_AnchoredPosition`/`m_SizeDelta`），⛔ **别照抄**：')
        for e in nolaid:
            print(f'    {"  " * e["ind"]}{e["name"]} → {e["lgcls"]} [{e["est"]}]：'
                  f'{why.get(e["est"], "")}')
    unk = [e for e in out if e['est'] == 'unk']
    if unk:
        print('\n🔴 **下面这些布局组的主轴尺寸算不准**（子节点里有文字/嵌套布局件/ScrollRect，'
              '首选尺寸要 Unity 的字体度量）—— 表里那几个子节点的值**别照抄**：')
        for e in unk:
            print(f'    {"  " * e["ind"]}{e["name"]}')

    # ---- ③ **`m_ReverseArrangement=1`** 的组（子件「名字 ↔ 位置」是镜像的）----
    # 🔴 2026-10-05 补：本文件在此之前**完全不读这个字段**，`reverse=1` 的组按树序正排输出 ⇒
    #   「名字 ↔ 位置」整体镜像，**而且看着很像对的**（`项目任务.md` §三 第 29 条 **A91③**
    #   那份 ready patch 就是拿这份镜像读数推的）。表里现在**算对了**，但这一条要单列一块出声 ——
    #   读表的人若按「树序第一个 = 最左」去读，仍会读错（列的顺序**仍是树序**，只是坐标反了）。
    lgrev = [e for e in out if e.get('lgreverse')]
    if lgrev:
        print(f'\n🔴 **`m_ReverseArrangement=1`（子件按【树序倒排】）的布局组 {len(lgrev)} 个** —— '
              f'这些组的子件**名字与位置是镜像的**：树序**最后一个**子件在最左（HLG）/最上（VLG）。'
              f'本表的坐标**已按此算过**，但**别按「树序第一个 = 最左」去读名字**：')
        for e in lgrev:
            print(f'    {"  " * e["ind"]}{e["name"]} → {e["lgcls"]}'
                  f'（判据 = uGUI `HorizontalOrVerticalLayoutGroup.SetChildrenAlongAxis:153-155`）')
    return 0


if __name__ == '__main__':
    sys.exit(main())
