#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""env52_montage.py —— 把 `_tmp_view/env52/` 里的 55 张逐态图拼成总览表。

用法（用带 PIL 的那个 python）：
    D:/2/Warpforge_tools/py312/python.exe 工具/env52_montage.py

产出：
    _tmp_view/env52/_总览_13场4环境.png     ← 13 行（每场一行）× 最多 5 列（默认 + 效果态）
    _tmp_view/env52/_单场_<场名>.png        ← 每场一条横带（原尺寸，供放大看）

⚠️ 标签只用 ASCII（PIL 默认字体不保证有中文字形）——**列名用文件名**，中文那列会渲染成方块，
   所以中文列一律标成 `DEFAULT`（＝默认态）。
"""
import os
import re
import sys

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, '..', '_tmp_view', 'env52')
SRC = os.path.abspath(SRC)

ORDER = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
         'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
         'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
         'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']

THUMB_W = 300
LABEL_W = 250
ROWH = 0


def states_for(arena):
    """返回 [(列标签, 路径)]，**默认态排第一**。"""
    out = []
    default = os.path.join(SRC, '%s__默认.png' % arena)
    if os.path.exists(default):
        out.append(('DEFAULT', default))
    pat = re.compile(r'^%s__(.+)\.png$' % re.escape(arena))
    rest = []
    for f in os.listdir(SRC):
        m = pat.match(f)
        if m and m.group(1) != '默认':
            rest.append((m.group(1), os.path.join(SRC, f)))
    out += sorted(rest)
    return out


def main():
    rows = [(a, states_for(a)) for a in ORDER if states_for(a)]
    if not rows:
        print('没找到图，检查目录：%s' % SRC)
        return 1

    first = Image.open(rows[0][1][0][1])
    scale = THUMB_W / float(first.size[0])
    th = int(round(first.size[1] * scale))
    ncol = max(len(s) for _, s in rows)
    W = LABEL_W + ncol * THUMB_W
    H = 24 + len(rows) * (th + 20)
    sheet = Image.new('RGB', (W, H), (24, 24, 24))
    d = ImageDraw.Draw(sheet)

    y = 24
    for arena, states in rows:
        d.text((6, y + th // 2), arena, fill=(255, 220, 120))
        x = LABEL_W
        for lab, path in states:
            im = Image.open(path).convert('RGB').resize((THUMB_W, th), Image.LANCZOS)
            sheet.paste(im, (x, y))
            d.text((x + 3, y + th - 13), lab, fill=(255, 255, 255))
            x += THUMB_W
        y += th + 20

    out1 = os.path.join(SRC, '_总览_13场4环境.png')
    sheet.save(out1)
    print('总览 → %s  (%dx%d)' % (out1, sheet.size[0], sheet.size[1]))

    # 每场一条原尺寸横带
    for arena, states in rows:
        ims = [Image.open(p).convert('RGB') for _, p in states]
        w = sum(i.size[0] for i in ims)
        h = max(i.size[1] for i in ims)
        strip = Image.new('RGB', (w, h), (0, 0, 0))
        x = 0
        for i in ims:
            strip.paste(i, (x, 0))
            x += i.size[0]
        p2 = os.path.join(SRC, '_单场_%s.png' % arena)
        strip.save(p2)
    print('单场横带 → %s/_单场_*.png（13 张）' % SRC)
    return 0


if __name__ == '__main__':
    sys.exit(main())
