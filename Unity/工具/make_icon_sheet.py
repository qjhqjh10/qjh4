#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
make_icon_sheet.py —— 把 `资料/关键词图标/icons/` 那 79 个图标拼成**一张总览图**。

## 为什么要有这个脚本

排查「卡面这个图标到底是哪个关键词」时，**一次看全 79 个**比逐张打开快得多
（2026-09-13 认 `questPoints1.png` vs `artifice.png` 就是靠它一次看清的）。

⚠️ **产物不进仓库**：那张图是**原版美术的拼图**，按仓库红线只能留在本地
（`.gitignore` 已经把 `资料/关键词图标/icons/` 整目录排除了，理由写在里面）。
所以：**脚本进仓库、图不进** —— 谁要谁跑一遍。

## 用法

    python Unity/工具/make_icon_sheet.py

产物：`_tmp_view/keyword_icon_sheet.png`（10 列，每格 80×80 + 一行文件名）
"""

import io, os, sys

try:
    sys.stdout.reconfigure(encoding='utf-8')
except Exception:
    pass

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # d:/4/Unity
SRCDIR = os.path.join(ROOT, '资料', '关键词图标', 'icons')
OUT = os.path.join(os.path.dirname(ROOT), '_tmp_view', 'keyword_icon_sheet.png')  # d:/4/_tmp_view/

COLS, CELL, PAD = 10, 80, 26


def main():
    try:
        from PIL import Image, ImageDraw
    except ImportError:
        print('需要 Pillow：pip install pillow')
        return 1

    if not os.path.isdir(SRCDIR):
        print(f'找不到图标目录：{SRCDIR}')
        print('（它在本地是 gitignore 的 —— 用 资料/关键词图标/关键词与图标_对照表.md')
        print('  里记的图集路径重新切一份即可）')
        return 1

    files = sorted(f for f in os.listdir(SRCDIR) if f.lower().endswith('.png'))
    if not files:
        print(f'{SRCDIR} 里没有 PNG')
        return 1

    rows = (len(files) + COLS - 1) // COLS
    sheet = Image.new('RGB', (COLS * CELL, rows * (CELL + PAD)), (24, 24, 28))
    dr = ImageDraw.Draw(sheet)

    for i, f in enumerate(files):
        im = Image.open(os.path.join(SRCDIR, f)).convert('RGBA')
        x, y = (i % COLS) * CELL, (i // COLS) * (CELL + PAD)
        bg = Image.new('RGBA', im.size, (24, 24, 28, 255))
        bg.alpha_composite(im)                      # 图标带透明边，垫个深底才看得清
        sheet.paste(bg.convert('RGB'), (x, y))
        dr.text((x + 2, y + CELL + 4), f[:-4][:15], fill=(210, 210, 210))

    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.save(OUT)
    print(f'{len(files)} 个图标 → {OUT}  {sheet.size[0]}×{sheet.size[1]}')
    print('⚠️ 这张是原版美术的拼图，**别提交**（.gitignore 已排除 icons/ 目录）')
    return 0


if __name__ == '__main__':
    sys.exit(main())
