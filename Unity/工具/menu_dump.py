#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""menu_dump.py — 解包里某个 prefab 子树的**逐节点参数表**（sprite / 文本 / 字号 / 颜色 / 显示状态）。

为什么要有它（2026-09-23，阶段二第 3 层）：
    `menu_rect.py` 只给**矩形**；而「层 × 参数」表（`项目任务.md` §三 10·3 第 0 层）还要
    **sprite 名 · 文本 · 字号 · 颜色 · activeSelf 的出现条件 · 九宫格/Simple · 组件**。
    这些东西在解包 JSON 里**全都有**，但上一个会话是**逐段手抄**的（正本 §二/§四 那些表）。
    手抄一次能忍，商店那几件（三页签 + 商品卡 7 变体 + 报价弹窗 8 变体）手抄必错
    ⇒ 判据做成脚本，一次跑出全表（同 `menu_rect.py` 的理由）。

🔴 **sprite 名要按 pid 反查**（这一步只有读**原始 .bundle** 才有）：
    解包目录是按**名字**存文件的（`Sprite/40K_button.json`），**文件名里没有 pid**，
    而 Image 组件存的是 `m_Sprite: {m_PathID: …}` ⇒ 只读解包目录**建不出反查表**。
    这里用 UnityPy 读真包建 `pid → 名字`，缓存到 `_tmp_view/sprite_pids_<包名>.json`（第二次秒回）。

用法：
    PY=D:/2/Warpforge_tools/py312/python.exe
    $PY d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Campaign Reward Window" --depth 9
    $PY d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Shop Menu Variant" --depth 6 --active-only
选项：
    --depth N        最大深度（默认 4）
    --active-only    只打出厂 `m_IsActive=1` 的节点
    --no-sprite      不读真包（跳过 sprite 名，纯解包目录也能跑）
"""
import argparse
import io
import json
import os
import sys

try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    sys.stderr.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass

BUNDLES = 'd:/2/新解包资源/assets_full'
BUNDLE_DIR = os.environ.get(
    'WF_BUNDLE_DIR', r'D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64')
CACHE_DIR = r'd:/4/_tmp_view'


# ---------------------------------------------------------------- 真包：pid → sprite 名

def sprite_pid_map(bundle_name, quiet=False):
    """从**原始 .bundle** 建 `Sprite 的 PathID → 名字`。缓存到 _tmp_view。

    🔴 **要扫全目录、不能只扫本包**：Image 组件的 `m_Sprite` 带 `m_FileID` —— 非 0 表示
    sprite 在**另一个包**里（图集包，例：`atlasindividual_assets_0_mainmenu`）。
    实测 `bundle_menus_assets_all` 自己只有 **22** 张 sprite，而这个奖励窗引用的十几张
    分散在别的包 ⇒ 只读本包会得到一堆 `?pid…`（2026-09-23 踩过）。
    """
    cache = os.path.join(CACHE_DIR, 'sprite_pids_ALL.json')
    if os.path.exists(cache):
        try:
            return json.load(io.open(cache, encoding='utf-8'))
        except Exception:
            pass
    try:
        import UnityPy
    except ImportError:
        if not quiet:
            sys.stderr.write('⚠️ 没有 UnityPy ⇒ 这次不反查 sprite 名\n')
        return {}
    if not os.path.isdir(BUNDLE_DIR):
        if not quiet:
            sys.stderr.write('⚠️ 找不到真包目录 %s ⇒ 这次不反查 sprite 名\n' % BUNDLE_DIR)
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
        os.makedirs(CACHE_DIR, exist_ok=True)
        io.open(cache, 'w', encoding='utf-8').write(json.dumps(out, ensure_ascii=False))
    except Exception:
        pass
    if not quiet:
        sys.stderr.write('（扫 %d 个真包，读出 %d 个 Sprite 的 pid→名字，已缓存到 %s）\n'
                         % (len(files), len(out), cache))
    return out


# ---------------------------------------------------------------- 解包目录

def load_mb(bundle, pid):
    p = os.path.join(bundle, 'MonoBehaviour', f'MonoBehaviour_{pid}.json')
    if not os.path.exists(p):
        return None
    try:
        return json.load(io.open(p, encoding='utf-8'))
    except Exception:
        return None


class Bundle(object):
    """解包目录（按名字存）。复用 `menu_rect.py` 那套 pid 反推法（GO 的 `(N)` 后缀不含 pid）。"""

    def __init__(self, path, sprites=None):
        self.path = path
        self.sprites = sprites or {}
        self.rt, self.go, self.mb = {}, {}, {}
        for fn in os.listdir(os.path.join(path, 'RectTransform')):
            if fn.startswith('RectTransform_') and fn.endswith('.json'):
                try:
                    j = json.load(io.open(os.path.join(path, 'RectTransform', fn), encoding='utf-8'))
                except Exception:
                    continue
                self.rt[fn[len('RectTransform_'):-len('.json')]] = j
        for fn in os.listdir(os.path.join(path, 'MonoBehaviour')):
            if fn.startswith('MonoBehaviour_') and fn.endswith('.json'):
                pid = fn[len('MonoBehaviour_'):-len('.json')]
                if pid.lstrip('-').isdigit():
                    self.mb[pid] = os.path.join(path, 'MonoBehaviour', fn)
        for fn in os.listdir(os.path.join(path, 'GameObject')):
            if not fn.endswith('.json'):
                continue
            try:
                j = json.load(io.open(os.path.join(path, 'GameObject', fn), encoding='utf-8'))
            except Exception:
                continue
            j['_name'] = fn[:-len('.json')]
            true_pid = None
            for c in j.get('m_Component', []):
                r = self.rt.get(str(c['component']['m_PathID']))
                if r is not None:
                    tp = str(r.get('m_GameObject', {}).get('m_PathID', ''))
                    if tp and tp != '0':
                        true_pid = tp
                        break
            if true_pid:
                self.go.setdefault(true_pid, j)

    def find_go(self, key):
        if str(key) in self.go:
            return str(key)
        hits = [pid for pid, g in self.go.items()
                if g.get('m_Name') == key or g.get('_name') == key
                or g.get('_name', '').rsplit('_', 1)[0] == key]
        if not hits:
            return None
        if len(hits) > 1:
            sys.stderr.write('⚠️ 「%s」命中 %d 个，取第一个：%s\n'
                             % (key, len(hits), '、'.join(h + '(' + self.go[h]['_name'] + ')'
                                                          for h in hits[:8])))
        return hits[0]

    def go_name(self, gopid):
        g = self.go.get(str(gopid))
        return (g.get('m_Name') or g.get('_name')) if g else None

    def rt_of_go(self, gopid):
        g = self.go.get(str(gopid))
        if not g:
            return None
        for c in g.get('m_Component', []):
            cp = str(c['component']['m_PathID'])
            if cp in self.rt:
                return cp
        return None

    def children(self, rtpid):
        r = self.rt.get(str(rtpid))
        return [c['m_PathID'] for c in r.get('m_Children', [])] if r else []

    def components(self, gopid):
        g = self.go.get(str(gopid))
        return g.get('m_Component', []) if g else []


# ---------------------------------------------------------------- 一行的参数

def sprite_name(b, im):
    sp = im.get('m_Sprite') or {}
    pid = str(sp.get('m_PathID', 0))
    if pid in ('0', ''):
        return None
    return b.sprites.get(pid, '?pid' + pid)


IMG_TYPE = {0: 'Simple', 1: 'Sliced', 2: 'Tiled', 3: 'Filled'}


def node_line(b, gopid, rtpid):
    """返回这个节点上「值得抄」的参数串。"""
    parts = []
    for c in b.components(gopid):
        cp = str(c['component']['m_PathID'])
        p = os.path.join(b.path, 'MonoBehaviour', f'MonoBehaviour_{cp}.json')
        if not os.path.exists(p):
            continue
        try:
            mb = json.load(io.open(p, encoding='utf-8'))
        except Exception:
            continue
        k = mb.keys()

        # ---- TextMeshPro ----
        if 'm_text' in k:
            t = mb.get('m_text', '')
            fs = mb.get('m_fontSize')
            auto = mb.get('m_enableAutoSizing')
            lo, hi = mb.get('m_fontSizeMin'), mb.get('m_fontSizeMax')
            col = mb.get('m_fontColor') or {}
            ha = mb.get('m_HorizontalAlignment')
            s = 'TMP "%s" fs=%g' % (t, fs) if fs is not None else 'TMP "%s"' % t
            if auto:
                s += '(auto %g-%g)' % (lo, hi)
            if col:
                s += ' col=(%.3g,%.3g,%.3g,%.3g)' % (col.get('r', 1), col.get('g', 1),
                                                     col.get('b', 1), col.get('a', 1))
            if ha is not None:
                s += ' hAlign=%s' % {0: 'Left', 1: 'Center', 2: 'Right', 3: 'Justified',
                                     4: 'Flush'}.get(ha, ha)
            if mb.get('m_characterSpacing'):
                s += ' charSpacing=%g' % mb['m_characterSpacing']
            parts.append(s)

        # ---- Image ----
        elif 'm_Sprite' in k:
            nm = sprite_name(b, mb)
            typ = IMG_TYPE.get(mb.get('m_Type'), mb.get('m_Type'))
            s = 'Img[%s]' % (nm if nm else 'sprite=0')
            s += ' type=%s' % typ
            if mb.get('m_PreserveAspect'):
                s += ' preserveAspect'
            col = mb.get('m_Color') or {}
            if col and (col.get('r', 1) != 1 or col.get('g', 1) != 1
                        or col.get('b', 1) != 1 or col.get('a', 1) != 1):
                s += ' col=(%.3g,%.3g,%.3g,%.3g)' % (col.get('r', 1), col.get('g', 1),
                                                     col.get('b', 1), col.get('a', 1))
            if mb.get('m_Enabled') == 0:
                s += ' **m_Enabled=0**'
            parts.append(s)

        # ---- 布局组 / 掩码 / 其它有意思的 ----
        else:
            if 'm_Padding' in mb and ('m_Spacing' in mb or 'm_ChildAlignment' in mb):
                pad = mb['m_Padding']
                parts.append('Layout[%s spacing=%s pad=%g/%g/%g/%g align=%s]'
                             % ('HLG' if 'm_Spacing' in mb and 'm_ChildControlWidth' in mb
                                else 'VLG',
                                mb.get('m_Spacing'), pad.get('m_Left'), pad.get('m_Right'),
                                pad.get('m_Top'), pad.get('m_Bottom'), mb.get('m_ChildAlignment')))
            elif 'm_Content' in mb and 'm_Viewport' in mb:
                parts.append('ScrollRect')
            elif 'm_ShowMaskGraphic' in mb:
                parts.append('Mask(showGraphic=%s)' % mb.get('m_ShowMaskGraphic'))
            elif 'm_Alpha' in mb:
                parts.append('CanvasGroup(a=%g blocksRaycasts=%s)'
                             % (mb.get('m_Alpha'), mb.get('m_BlocksRaycasts')))
            elif 'm_Script' in mb:
                nm = mb.get('m_Name')
                if nm:
                    parts.append('MB[%s]' % nm)
    return ' | '.join(parts)


def walk(b, rtpid, depth, maxdepth, out, indent=0):
    if depth > maxdepth:
        return
    rt = b.rt.get(str(rtpid))
    if rt is None:
        return
    gopid = rt.get('m_GameObject', {}).get('m_PathID')
    g = b.go.get(str(gopid)) or {}
    out.append((indent, b.go_name(gopid) or '<RT %s>' % rtpid, g.get('m_IsActive', 1),
                node_line(b, gopid, rtpid),
                rt.get('m_LocalScale', {'x': 1, 'y': 1})))
    for c in b.children(rtpid):
        walk(b, c, depth + 1, maxdepth, out, indent + 1)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('bundle')
    ap.add_argument('root')
    ap.add_argument('--depth', type=int, default=4)
    ap.add_argument('--active-only', action='store_true')
    ap.add_argument('--no-sprite', action='store_true')
    args = ap.parse_args()

    path = args.bundle
    if not os.path.isdir(path):
        path = os.path.join(BUNDLES, args.bundle)
    bundle_name = os.path.basename(path.rstrip('/\\'))
    sprites = {} if args.no_sprite else sprite_pid_map(bundle_name)
    b = Bundle(path, sprites)

    gopid = b.find_go(args.root)
    if gopid is None:
        sys.exit('找不到 GameObject「%s」（pid 或名字）' % args.root)
    rtpid = b.rt_of_go(gopid)
    if rtpid is None:
        sys.exit('「%s」没有 RectTransform' % args.root)

    out = []
    walk(b, rtpid, 0, args.depth, out)

    print('# %s —— 逐节点参数（sprite / 文本 / 字号 / 颜色 / 组件）' % args.root)
    print('# 出处 %s' % path)
    print('# act=出厂 m_IsActive；★ = 这一行有值得抄的参数')
    for (ind, name, active, line, scl) in out:
        if args.active_only and not active:
            continue
        sc = '' if (abs(scl.get('x', 1) - 1) < 1e-6 and abs(scl.get('y', 1) - 1) < 1e-6) \
            else ' scl=%s,%s' % (scl.get('x'), scl.get('y'))
        flag = ' ' if active else 'INACT'
        print('%s%s %-40s %s%s' % ('  ' * ind, flag, name[:40], line, sc))
    return 0


if __name__ == '__main__':
    sys.exit(main())
