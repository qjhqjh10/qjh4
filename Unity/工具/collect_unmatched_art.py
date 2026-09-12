#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""collect_unmatched_art.py — 把**配不上立绘的那批卡**收集成一个给人看的文件夹

`import_original_art.py` 按卡名在解包资源里反查立绘，配不上的会打出来（目前 18 张）。
那些卡多半是**文件名和卡表对不上**（拼写差一个字母、词序反、同名不同版本），要人工对一眼。
这个工具把它们摊开：

    <输出目录>/
    ├── README.md          要你做的事 + 每张卡的候选清单（表格）
    ├── 原版卡面/          `<序号>_<卡名>.png` —— 原版官方卡面（**上面印着卡名**，一眼能认）
    └── 候选插图/          `<序号>_<卡名>__<候选文件名>.png` —— 同阵营里名字最像的几张

看的时候：拿「原版卡面」上印的名字，去「候选插图」里挑出对的那张，告诉我哪张对哪张。

用法：
    python collect_unmatched_art.py [--check]

⚠️ 里面全是原版资产 → 输出目录在 `.gitignore` 里（别提交）。
"""
import argparse
import difflib
import glob
import json
import os
import re
import shutil
import sys

sys.stdout.reconfigure(encoding='utf-8')

UNPACK = 'd:/2/新解包资源/assets_full'
FACES  = 'd:/2/Warpforge部队卡片'              # 官方卡面（900×1200，印着卡名）
CARDS  = 'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json'
OUT    = 'd:/4/Unity/资料/待人工对_插图'
TOPK   = 5                                    # 每张卡给几个候选

FACTION_BUNDLE = {
    'Ultramarines':     'bundle_spacemarinesultramarinescardassets_assets_all',
    'Goff':             'bundle_orksgoffcardassets_assets_all',
    'SaimHann':         'bundle_aeldarisaimhanncardassets_assets_all',
    'Genestealers':     'bundle_genestealercultscardassets_assets_all',
    'DarkAngels':       'bundle_spacemarinesdarkangelscardassets_assets_all',
    'BlackLegion':      'bundle_chaosspacemarinesblacklegioncardassets_assets_all',
    'Sautekh':          'bundle_necronssautekhcardassets_assets_all',
    'TauEmpire':        'bundle_tauempirecardassets_assets_all',
    'Leviathan':        'bundle_tyranidsleviathancardassets_assets_all',
    'AstraMilitarum':   'bundle_astramilitarumcardassets_assets_all',
    'Sororitas':        'bundle_sororitascardassets_assets_all',
    'EmperorsChildren': 'bundle_chaosspacemarinesemperorschildrencardassets_assets_all',
    'SpaceWolves':      'bundle_spacemarinesspacewolvescardassets_assets_all',
}


def norm(s):
    return re.sub(r'[^a-z0-9]', '', (s or '').lower())


def unmatched_cards():
    """复刻 `import_original_art.py` 的匹配，返回**配不上**的 (阵营, 卡名) 列表。"""
    cards = json.load(open(CARDS, encoding='utf-8'))['cards']
    out = []
    for fac, bundle in FACTION_BUNDLE.items():
        folder = os.path.join(UNPACK, bundle, 'Texture2D')
        if not os.path.isdir(folder):
            continue
        files = [p for p in glob.glob(os.path.join(folder, '*.png'))
                 if 'Cardframe' not in os.path.basename(p)]
        nf = {p: norm(os.path.basename(p)[:-4]) for p in files}
        taken = set()
        for c in cards:
            if c.get('faction') != fac:
                continue
            n = norm(c['name'])
            if not n:
                continue
            hit = None
            for p in files:
                if p in taken:
                    continue
                if n in nf[p] and (hit is None or len(n) > len(norm(os.path.basename(hit)[:-4]))):
                    hit = p
            if hit is None:
                best, score = None, 0.0
                for p in files:
                    if p in taken:
                        continue
                    r = difflib.SequenceMatcher(None, n, nf[p]).ratio()
                    if r > score:
                        best, score = p, r
                if best is not None and score >= 0.86:
                    hit = best
            if hit is None:
                out.append((fac, c['name']))
            else:
                taken.add(hit)
    return out


def face_for(fac, card_name):
    """在官方卡面库里按名字找这张卡的卡面（模糊匹配，取最像的）。"""
    n = norm(card_name)
    best, score = None, 0.0
    for p in glob.glob(os.path.join(FACES, '**', '*.png'), recursive=True):
        base = os.path.basename(p)
        if 'Cardback' in base:          # 卡背那批不算卡面
            continue
        r = difflib.SequenceMatcher(None, n, norm(base[:-4])).ratio()
        # 也试试「卡名是文件名的一部分」（文件名带前缀/后缀）
        if n and n in norm(base[:-4]):
            r = max(r, 0.9)
        if r > score:
            best, score = p, r
    return (best, score) if score >= 0.55 else (None, score)


def candidates(fac, card_name, k=TOPK):
    """同阵营里名字最像的 k 张插图（不管有没有被别的卡占掉 —— 给人眼看的）。"""
    bundle = FACTION_BUNDLE.get(fac)
    if not bundle:
        return []
    folder = os.path.join(UNPACK, bundle, 'Texture2D')
    files = [p for p in glob.glob(os.path.join(folder, '*.png'))
             if 'Cardframe' not in os.path.basename(p)]
    n = norm(card_name)
    scored = sorted(((difflib.SequenceMatcher(None, n, norm(os.path.basename(p)[:-4])).ratio(), p)
                     for p in files), reverse=True)
    return [(p, s) for s, p in scored[:k]]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true', help='只列出来，不写文件')
    args = ap.parse_args()

    todo = unmatched_cards()
    print(f'配不上的卡：{len(todo)} 张')
    if args.check:
        for fac, name in todo:
            print(f'   {fac}/{name}')
        return 0

    face_dir = os.path.join(OUT, '原版卡面')
    cand_dir = os.path.join(OUT, '候选插图')
    for d in (OUT, face_dir, cand_dir):
        os.makedirs(d, exist_ok=True)

    lines = ['# 配不上立绘的卡 —— 人工对一下', '',
             f'共 **{len(todo)}** 张。这些是 `工具/import_original_art.py` 按卡名反查时**没配上**的，',
             '多半是文件名和卡表对不上（拼写差一个字母 / 词序反 / 同名不同版本）。', '',
             '**怎么看**：`原版卡面/` 里那张是**原版官方卡面**，上面印着真正的卡名；',
             '照着它去 `候选插图/` 里挑出对的那张，把「序号 → 选哪个候选」告诉我就行。', '',
             '> ⚠️ 全是原版资产，只在本机看；这个目录在 `.gitignore` 里。', '',
             '| # | 阵营 | 卡名 | 官方卡面 | 候选（按名字相似度） |',
             '|---|---|---|---|---|']

    n_face = n_cand = 0
    for i, (fac, name) in enumerate(todo, 1):
        stem = f'{i:02d}_{re.sub(r"[^0-9A-Za-z]+", "_", name)}'

        face, score = face_for(fac, name)
        face_cell = '—— 卡面库里也没找到'
        if face:
            ext = os.path.splitext(face)[1]
            shutil.copyfile(face, os.path.join(face_dir, stem + ext))
            face_cell = f'`原版卡面/{stem}{ext}`（相似度 {score:.2f}）'
            n_face += 1

        cands = candidates(fac, name)
        cell = []
        for j, (p, s) in enumerate(cands, 1):
            ext = os.path.splitext(p)[1]
            fn = f'{stem}__候选{j}_{re.sub(r"[^0-9A-Za-z]+", "_", os.path.basename(p)[:-4])[:40]}{ext}'
            shutil.copyfile(p, os.path.join(cand_dir, fn))
            cell.append(f'{j}. `{fn}`（{s:.2f}）')
            n_cand += 1
        lines.append(f'| {i} | {fac} | {name} | {face_cell} | ' + '<br>'.join(cell) + ' |')

    lines += ['', '---', '',
              '## 对完之后怎么办', '',
              '把对应关系告诉我（例如「11 Veldras the Sublime → 候选2」），我会：',
              '1. 在 `工具/import_original_art.py` 里加一张**别名表**（卡名 → 真正的贴图名），',
              '2. 重跑导入，`art_<卡名>.png` 就都有了；',
              '3. 自检里那 18 张会变成 0 张（`配不上` 的数量会打出来）。', '']
    open(os.path.join(OUT, 'README.md'), 'w', encoding='utf-8').write('\n'.join(lines))
    print(f'写好：{OUT}（卡面 {n_face} 张 / 候选 {n_cand} 张）')
    return 0


if __name__ == '__main__':
    sys.exit(main())
