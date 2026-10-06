#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""孤儿扫描：`Resources/Art/ui_menu/` 里**盘上有、但没有任何 job 会产出**的图（A808 用）。

判据（用户 2026-10-13 派活）：**磁盘事实要能被导入器复现** —— 逐个磁盘文件问
「哪个 job 会产出它」，答不出的就是孤儿。

做法：**不抄清单**，直接把两个导入器**当模块加载**、读它们自己的表算目标名：
  · `工具/import_original_art.py` —— 四个会落 `MENU_OUT` 的清单：
      `MENU_IMAGES` · `CAMPAIGN_BGS` · `BUILTIN_IMAGES` · `MENU_FROM_ART`
      （`UI_IMAGES` / `ALT_ACTION_IMAGES` / `FX_TEXTURES` 落的是 `UI_OUT` = `Art/ui`，**不是** `ui_menu`，
       这里也照算一遍并打出它们各自的目录，免得「以为在管」）
  · `工具/sync_battle_ui_art.py` —— `NAMES_MENU` → `DST_MENU`
命名规则照两个脚本自己的那行：`name.replace(' ', '_') + '.png'`。

用法：python 资料/普查产出_1014/_orphan_scan.py
只读：**一个字节都不写**。
"""
import importlib.util
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

TOOLS = 'd:/4/Unity/工具'
MENU_OUT = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu'


def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


def dn(n):
    """脚本自己的落盘名规则：空格 → 下划线，其余一字不动。"""
    return n.replace(' ', '_') + '.png'


def main():
    art = load(os.path.join(TOOLS, 'import_original_art.py'), 'wf_import_art')
    sync = load(os.path.join(TOOLS, 'sync_battle_ui_art.py'), 'wf_sync_battle_ui')

    producers = {}          # 落盘文件名 → 谁产出的（来源说明）

    def add(names, who):
        for n in names:
            for f in (dn(n), n.replace(' ', '_') + '.png' if not n.endswith('.png') else n):
                producers.setdefault(f, who)

    # ---- import_original_art.py 的四个口 ----
    print(f'import_original_art.MENU_OUT          = {art.MENU_OUT}')
    print(f'import_original_art.CAMPAIGN_BG_OUT   = {art.CAMPAIGN_BG_OUT}')
    print(f'import_original_art.UI_OUT            = {art.UI_OUT}')
    print(f'import_original_art.MENU_FROM_ART_DIR = {art.MENU_FROM_ART_DIR}')
    assert os.path.normpath(art.MENU_OUT) == os.path.normpath(MENU_OUT)
    # ⚠️ 战役背景那个 OUT **就是** ui_menu（同一个目录）—— 也落在这里，别漏
    assert os.path.normpath(art.CAMPAIGN_BG_OUT) == os.path.normpath(MENU_OUT)
    add([n for n, _b in art.MENU_IMAGES], 'MENU_IMAGES')
    add(art.CAMPAIGN_BGS, 'CAMPAIGN_BGS')
    add(art.BUILTIN_IMAGES, 'BUILTIN_IMAGES')
    add(art.MENU_FROM_ART, 'MENU_FROM_ART')
    # 这三个落别处 —— 打出来验一下「不算进 ui_menu」是对的
    print('  · UI_IMAGES         → ' + art.UI_OUT + f'（{len(art.UI_IMAGES)} 张，**不在** ui_menu）')
    print('  · ALT_ACTION_IMAGES → ' + art.UI_OUT + f'（{len(art.ALT_ACTION_IMAGES)} 张，**不在** ui_menu）')
    print('  · FX_TEXTURES       → ' + art.UI_OUT + f'（{len(art.FX_TEXTURES)} 张，**不在** ui_menu）')

    # ---- sync_battle_ui_art.py 的菜单那一批 ----
    assert os.path.normpath(sync.DST_MENU) == os.path.normpath(MENU_OUT)
    add(sync.NAMES_MENU, 'sync_battle_ui_art.NAMES_MENU')

    disk = sorted(f for f in os.listdir(MENU_OUT) if f.lower().endswith('.png'))
    orphans = [f for f in disk if f not in producers]

    # 反过来：清单里有、盘上没有的（A629 那一类）—— 也要看得见
    notyet = sorted(set(producers) - set(disk))
    # ⚠️ 只有「源文件真的在」的才算「本该有」；源不在的另外列（那是「找不到源」）
    print()
    print(f'盘上 ui_menu/*.png          : {len(disk)}')
    print(f'任何 job 会产出的名字        : {len(producers)}')
    print(f'🔴 孤儿（盘上有、没 job 产出）: {len(orphans)}')
    for f in orphans:
        print('   ·', f)
    print(f'⚠️ 清单里有、盘上没有        : {len(notyet)}')
    for f in notyet:
        print('   ·', f, '←', producers[f])

    # ---- 孤儿里，哪些**源本来就在**（那样就是「登记漏了」，加一行即可）----
    print()
    print('---- 孤儿逐个找源（按切片缓存 / 工程的图集切片库）----')
    cache = 'd:/2/Warpforge_tools/data/ui_extract'
    idx = {}
    for dp, _d, files in os.walk(cache):
        for f in files:
            if f.lower().endswith('.png'):
                idx.setdefault(f.lower(), []).append(os.path.join(dp, f))
    # ⚠️ **别只试「全下划线」/「全空格」两种写法** —— 落盘名是**空格全部换下划线**，
    #    源名往往**只在一部分位置有空格**（`40k_UI_Banner BW` / `UI_HIghlight Internal` /
    #    `Noise Combined` / `Up Rays` / `OctagonUI Border SDF 2`）⇒ 两种极端写法都命中不了。
    #    改用**归一化比对**（只留小写字母数字），和 `portrait_jobs()` 那套一个思路。
    import re as _re

    def _norm(s):
        return _re.sub(r'[^a-z0-9]', '', s.lower())

    norm_idx = {}
    for k, v in idx.items():
        norm_idx.setdefault(_norm(k[:-4]), []).extend(v)
    artdir = art.MENU_FROM_ART_DIR
    for f in orphans:
        stem = f[:-4]
        # 候选 = 归一化同名的**所有**副本，`Sprite/` 与 `Texture2D/` 都收（打印时标出来）
        raw = norm_idx.get(_norm(stem), [])
        spr = [c for c in raw if os.path.basename(os.path.dirname(c)) == 'Sprite']
        tex = [c for c in raw if os.path.basename(os.path.dirname(c)) == 'Texture2D']
        p_art = os.path.join(artdir, f)
        if os.path.exists(p_art):
            spr = spr + [p_art]
        # 🔴 **md5 对上才算「找对了源」** —— 盘上那张是手拷的，**来源只能靠字节证明**
        #    （同名不同内容在解包里很常见，2026-09-24 那次就因为没逐字节比而翻车过）
        import hashlib

        def md5(p):
            h = hashlib.md5()
            with open(p, 'rb') as fh:
                for b in iter(lambda: fh.read(1 << 20), b''):
                    h.update(b)
            return h.hexdigest()

        want = md5(os.path.join(MENU_OUT, f))
        hit_spr = [c for c in sorted(set(spr)) if md5(c) == want]
        hit_tex = [c for c in sorted(set(tex)) if md5(c) == want]
        tag = ('✅ Sprite/' if hit_spr else ('⚠️ 只有 Texture2D/' if hit_tex else '❌ 缓存里没有'))
        print(f'   {tag}  {f}   (盘上 md5 {want[:12]})')
        for c in (hit_spr or hit_tex or sorted(set(spr + tex)))[:6]:
            mark = 'md5同' if md5(c) == want else 'md5异'
            print(f'        [{mark}] {c}')
        if not hit_spr and not hit_tex:
            # 最后一路：`assets_full`（**只在缓存全不匹配时**才走 —— 24.7 万文件，别每次都扫）。
            # ⚠️ `ui_extract` 只有 **25 个包**，`battlesharedresources_assets_all` **不在里面**
            #    （`Shine trail` 就是这么漏的）⇒ 不走这一路会把它误报成「源不存在」。
            for dp, _d, files in os.walk('d:/2/新解包资源/assets_full'):
                if os.path.basename(dp) != 'Texture2D':
                    continue
                for fn in files:
                    if _norm(fn[:-4]) == _norm(stem) and fn.lower().endswith('.png'):
                        p = os.path.join(dp, fn)
                        print(f'        [{"md5同" if md5(p) == want else "md5异"}] {p}   ← assets_full')

    # ---- 同族的另外两个目录：`ui/`（战斗 HUD）与 `ui_deck/`（卡面/卡组编辑）-------------------
    # 同一套判据（谁跑一次 sync / 导入器，这几张会不会没）。**只算静态清单** ——
    # `cards/`(立绘) `cardbacks/` `avatars/` `traits/` `altarts/` 那几批是**按目录全量扫**的，
    # 判据不同（`portrait_jobs()` / `cardback_jobs()` / `os.listdir`），本脚本不覆盖。
    print()
    print('---- 同族：ui/ 与 ui_deck/（判据同上，只算两张静态清单）----')
    ui_ok = {dn(n) for n in art.UI_IMAGES} | {dn(n) for n in art.ALT_ACTION_IMAGES} | {dn(n) for n in art.FX_TEXTURES}
    ui_ok |= {dn(n) for n in sync.NAMES}
    deck_ok = {dn(n) for n in sync.NAMES_DECK}
    for label, d, ok in (('ui', art.UI_OUT, ui_ok),
                         ('ui_deck', sync.DST_DECK, deck_ok)):
        disk_d = sorted(f for f in os.listdir(d) if f.lower().endswith('.png'))
        orph = [f for f in disk_d if f not in ok]
        print(f'   {label:8s} 盘上 {len(disk_d):4d} · 清单 {len(ok):3d} · 孤儿 {len(orph)}')
        for f in orph:
            print('        ·', f)

    return 0 if not orphans else 1


if __name__ == '__main__':
    sys.exit(main())
