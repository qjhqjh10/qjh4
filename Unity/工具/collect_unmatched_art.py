#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""collect_unmatched_art.py — 把**真管线判为「配不上立绘」的卡**收集成一个给人看的文件夹

🔴 **2026-10-06（A153）重写：本工具原来是个「误导源」，现在只做真管线的一层包装。**

原来它**自己复刻了一套旧匹配器** —— 不读 `ART_NAME_ALIAS` / `ART_ID_ALIAS`、用**整个文件名**的
相似度（不是真管线的滑窗 `best_window_ratio`）、不做「长名先认领」⇒ 读数比真管线**多**：
2026-10-06 实测它报 **25 张**，而真管线报 **0 张**。它还照这个多出来的清单去写
`资料/待人工对_插图/` 的 README 与候选图 ⇒ **新会话看到「N 张待人工对」就会重做一遍早已做完的活**
（`项目任务.md` 的 A116 就是这么来的）。

现在改成**直接调用真管线** `工具/import_original_art.py` 的 `portrait_jobs()`
（`import_original_art.py --check` 走的是同一个函数）⇒ 本工具读数与 `--check` **恒等**，
不可能再打架（「两处写同一条规则 = 迟早不一致」⇒ 这条规则现在只有一处）。

🔴 **真判据永远只认这一条命令**（本工具只是它的一个视图）：
    python d:/4/Unity/工具/import_original_art.py --check
    → 插图：配上 1126 张，配不上 0 张，缺 sprite rect 0 张      （2026-10-06 实测）

🔴 **2026-10-07（A159 连带）**：`portrait_jobs()` 那天起**返回三元组**
   `(jobs, unmatched, no_rect)`（此前只 `return jobs`，另两份**只 print**）⇒ 本工具改成
   **直接取返回值**那两份清单，不再从 stdout 的 `   配不上: ` / `   没裁（…` 行读回
   （原来那条形状的由来见 `资料/普查产出_1006/A153_过期工具修.md`）。stdout 仍原样转出、汇总行仍当**交叉校验**。

⚠️ `资料/待人工对_插图/` 里原来的东西是 **2026-09-12 的过期快照**，A116 已逐张结案，
   **不要照着它干活**。本工具默认写盘前会**先清掉它自己上一次的产物**，所以跑过一次之后，
   那个目录里只剩「当前真实缺口」（现在 = 空）。目录在 `.gitignore` 里（全是原版资产）。

用法：
    python collect_unmatched_art.py --check     # 只读数，不写盘（想看现状就用它）
    python collect_unmatched_art.py             # 清掉本工具的旧产物 + 按当前读数重写
    python collect_unmatched_art.py --clean     # 只清旧产物，不写
"""
import argparse
import contextlib
import difflib
import glob
import io
import os
import re
import shutil
import sys

sys.stdout.reconfigure(encoding='utf-8')

HERE  = os.path.dirname(os.path.abspath(__file__))
FACES = 'd:/2/Warpforge部队卡片'                  # 官方卡面（900×1200，印着卡名）
OUT   = 'd:/4/Unity/资料/待人工对_插图'
TOPK  = 5                                        # 每张卡给几个候选
OWN   = ['README.md', '原版卡面', '候选插图']      # 本工具自己生成的产物名（清的时候只碰这三个）

# 打在 stdout 最前面的警告 + 写进 README 开头的那一段（**同一份文案**，别抄第二遍）
STALE_WARN = (
    '本工具／本目录**不是判据**。真判据只有这一条命令：\n'
    '    python d:/4/Unity/工具/import_original_art.py --check\n'
    '`资料/待人工对_插图/` 里 2026-09-12 留下的那批（README 写「18 张待人工对」）是**过期快照**，\n'
    'A116 已逐张结案 —— **别照着它重做**。'
)


def load_pipeline():
    """把真管线 `import_original_art.py` 当模块读进来 —— **唯一判据来源**（本工具不再自带匹配器）。

    ⚠️ 模块顶层只有常量与函数定义（`from PIL import ...` 都在函数里惰性 import），import 无副作用。
    """
    if HERE not in sys.path:
        sys.path.insert(0, HERE)
    import import_original_art
    return import_original_art


def norm(s):
    return re.sub(r'[^a-z0-9]', '', (s or '').lower())


def real_pipeline_reading():
    """调真管线 `portrait_jobs()`，返回 (配上, 配不上, 缺 rect) 三份读数。

    🔴 **2026-10-07（A159 连带）**：`portrait_jobs()` 现在**返回三元组** `(jobs, unmatched, no_rect)`
    —— 那天之前它只 `return jobs`，另两份**只 print**（旧 `import_original_art.py:1163-1168`）
    ⇒ 本工具那时只能把它的 stdout 兜下来、再从 `   配不上: ` / `   没裁（…` 两行**读回**清单。
    **现在改成直接取返回值**（那两份就是过去打印的同一批字符串）。
    ⚠️ stdout 仍然兜下来**原样转出去**（让人看见真管线自己打的那一行），而且那条汇总行**留着当交叉校验**
    —— 「本工具不复制真管线的任何判据」这条没变：读数仍然全部来自它。

    🔴 **三处硬校验**：`not jobs` / 汇总行的「配上」≠ `len(jobs)` / 返回值里的「配不上」条数 ≠ 汇总行
    ⇒ **直接按失败退出**，绝不返回一个假的 0（「不许静默失败」：格式一变就必须吵，不能悄悄报 0 张）。

    配不上的一项原样是 `'<阵营>/<卡名>'`，或 `'<阵营>/<卡名>（点名表写的 X.png 不存在）'`
    —— 后者是 `ART_ID_ALIAS` 点了名、但那张贴图**当时不在解包里**，一并摊开。
    """
    pipe = load_pipeline()
    print('▶ 下面这几行是真管线 `import_original_art.portrait_jobs()` **自己**打的（本工具直接调它）：')
    buf = io.StringIO()
    with contextlib.redirect_stdout(buf):          # 先兜住，再原样转出来 —— 好判「源缺失」这一种
        jobs, um, nr = pipe.portrait_jobs()        # 🔴 A159：三元组（此前只有 jobs）
    text = buf.getvalue()
    print(text, end='')

    def die(why):
        print(f'🔴 {why}\n   本工具**不写盘**、按失败退出（宁可报错，也不报一个假的读数）。')
        sys.exit(2)

    # 🔴 真管线在「找不到卡表 / 找不到新解包资源」时**也是**返回空列表 ⇒ 那种情况下
    #    「配不上 0 张」是假读数。判据：一张都没配上就不往下走。
    if not jobs:
        die('真管线这次一张都没配上 ⇒ 读数不可信（多半是卡表或解包资源不在）。')

    m = re.search(r'插图：配上 (\d+) 张，配不上 (\d+) 张，缺 sprite rect (\d+) 张', text)
    if not m:
        die('读不到真管线那行汇总（`插图：配上 … 配不上 … 缺 sprite rect …`）'
            '—— 它的打印格式大概改过了，本工具读不了。')
    n_ok, n_bad, n_rect = (int(x) for x in m.groups())
    if n_ok != len(jobs):
        die(f'汇总行说配上 {n_ok} 张，但返回的 jobs 是 {len(jobs)} 条 —— 对不上。')

    # 配不上清单：**直接取返回值**（A159 起真管线会返回它；此前只能从 stdout 的 `   配不上: ` 行读回）
    out = []
    for item in um:
        fac, _, rest = str(item).strip().partition('/')
        note = ''
        if '（' in rest:
            rest, _, tail = rest.partition('（')
            note = tail[:-1] if tail.endswith('）') else tail
        out.append((fac, rest.strip(), note))
    if len(out) != n_bad:
        die(f'汇总行说配不上 {n_bad} 张，但返回值里是 {len(out)} 条 —— 对不上。')

    # 「缺 sprite rect」：返回值是**完整**的（真管线自己只打印前 6 个名字）⇒ 数用汇总行、名字用返回值。
    no_rect = [str(x) for x in nr]
    if len(no_rect) != n_rect:
        die(f'汇总行说缺 sprite rect {n_rect} 张，但返回值里是 {len(no_rect)} 条 —— 对不上。')
    return jobs, out, (n_rect, no_rect)


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


def candidates(pipe, fac, card_name, k=TOPK):
    """同阵营里名字最像的 k 张插图（不管有没有被别的卡占掉 —— 给人眼看的）。

    ⚠️ **这是线索、不是判据** —— 它按**整个文件名**的相似度排，而真管线用的是**滑窗**
    （`best_window_ratio`：文件名带前缀，整串比会低到 0.55）。A116 实测：那 18 张里
    **11 张的正确答案根本不在这个候选表里**（只有 7 张命中候选 1）
    ⇒ 「从候选里挑一个」这个问法对它们**不成立**，必须直接点名贴图名。
    （滑窗函数是 `portrait_jobs()` 里的**内层**函数，import 不出来；这里**不复制一份**
     —— 复制 = 两处写同一条规则，迟早不一致。）
    """
    bundle = pipe.card_bundle(fac)          # 阵营→包名也**共用真管线那一份**（原来这里抄了一份）
    if not bundle:
        return []
    folder = os.path.join(pipe.UNPACK, bundle, 'Texture2D')
    files = [p for p in glob.glob(os.path.join(folder, '*.png'))
             if 'Cardframe' not in os.path.basename(p)]
    n = norm(card_name)
    scored = sorted(((difflib.SequenceMatcher(None, n, norm(os.path.basename(p)[:-4])).ratio(), p)
                     for p in files), reverse=True)
    return [(p, s) for s, p in scored[:k]]


def others_in_out():
    """`<输出目录>` 里**不是**本工具生成的条目（人放的东西）—— 只报不动。"""
    if not os.path.isdir(OUT):
        return []
    return [e for e in sorted(os.listdir(OUT)) if e not in OWN]


def clean_own_output():
    """清掉**本工具自己生成**的那三样（`README.md` / `原版卡面/` / `候选插图/`）。

    ⚠️ 只删这三个名字，**不递归删整个目录**；目录里若出现别的名字（人放的东西），
    一律只告警、不碰。
    """
    removed = []
    for name in OWN:
        p = os.path.join(OUT, name)
        if os.path.isdir(p):
            shutil.rmtree(p)
            removed.append(name + '/')
        elif os.path.exists(p):
            os.remove(p)
            removed.append(name)
    return removed


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true', help='只读数，不写盘')
    ap.add_argument('--clean', action='store_true', help='只清掉本工具的旧产物，不写')
    args = ap.parse_args()

    print('=' * 78)
    for line in STALE_WARN.split('\n'):
        print('⚠️  ' + line if not line.startswith('    ') else line)
    print('=' * 78)

    jobs, todo, (n_rect, rect_names) = real_pipeline_reading()
    print(f'本工具读数（与上面同一份）：配上 {len(jobs)} 张，配不上 {len(todo)} 张，'
          f'缺 sprite rect {n_rect} 张')
    if rect_names:                      # 🔴 A159 起：名字来自**返回值**（完整）；数仍以汇总行为准
        print('   没裁（缺 textureRect，整张拷）:', '、'.join(rect_names))
    if not todo:
        print('✅ 配不上 = 0 张，**没有待人工对的活**。'
              f'（`{OUT}` 里若还有文件，那是 2026-09-12 的过期快照。）')
    else:
        print('⚠️ 有缺口要人工对 —— 待会儿拷的候选项只是**线索**'
              '（按整个文件名的相似度排），不是判据。')

    others = others_in_out()
    if others:
        print(f'⚠️ {OUT} 里还有**不是本工具生成的**条目，本工具不会动它们：{others}')
    if args.check:                      # 只读数：不写盘，也**不清**
        return 0
    if args.clean:
        print(f'已清（本工具的旧产物）：{clean_own_output() or "没有"} → {OUT}')
        return 0

    print(f'先清掉本工具上一次的产物（只删 {"/".join(OWN)} 这三个名字）：'
          f'{clean_own_output() or "没有"}')

    face_dir = os.path.join(OUT, '原版卡面')
    cand_dir = os.path.join(OUT, '候选插图')
    os.makedirs(OUT, exist_ok=True)
    if todo:                            # 缺口为 0 时**不建空子目录**，目录里只留这份 README
        for d in (face_dir, cand_dir):
            os.makedirs(d, exist_ok=True)

    pipe = load_pipeline()
    counts = ('真管线本次读数（`import_original_art.portrait_jobs()`，与 `--check` 同一份，'
              '**不是本工具自己算的**）：配上 %d 张 / 配不上 **%d** 张 / 缺 sprite rect %d 张。'
              % (len(jobs), len(todo), n_rect))
    lines = ['# 配不上立绘的卡 —— 人工对一下', '',
             '> 🔴 ' + STALE_WARN.replace('\n', '\n> '), '',
             counts, '']

    if not todo:
        # 🔴 这是**现在**的状态：目录里只该有这份 README（旧产物已被本工具清掉）。
        #    单开一段，别留一张空表 —— 免得下个会话以为「表是空的、活还没干」。
        lines += ['## 当前读数：0 张 —— **没有待人工对的活**', '',
                  '`资料/待人工对_插图/` 里 2026-09-12 留下的 README 与候选图是**旧匹配器**报的',
                  '（当时报 18 / 25 张），**A116 已逐张结案** —— 不要照着那批重做。',
                  '本工具每次写盘前会**先清掉它自己上一次的产物**，所以这个目录的内容',
                  '**恒等于**上面那行读数；现在缺口为 0 ⇒ 目录里只剩这份 README。', '',
                  '真要看缺口，任何时候都只认这一条命令：', '',
                  '```',
                  'python d:/4/Unity/工具/import_original_art.py --check',
                  '```', '']
    else:
        lines += ['下面共 **%d** 张（上面那行读数里「配不上」的那些）。' % len(todo), '',
                  '**怎么看**：`原版卡面/` 里那张是**原版官方卡面**，上面印着真正的卡名；',
                  '照着它去 `候选插图/` 里挑出对的那张，把「序号 → 选哪个候选」告诉我。',
                  '⚠️ **候选是按整个文件名的相似度排的、不是判据** —— A116 实测那 18 张里',
                  '11 张的正确答案**不在候选里**；看着都不像就直接说贴图名，别硬挑。', '',
                  '> ⚠️ 全是原版资产，只在本机看；这个目录在 `.gitignore` 里。', '',
                  '| # | 阵营 | 卡名 | 官方卡面 | 候选（按名字相似度，仅供参考） |',
                  '|---|---|---|---|---|']

    n_face = n_cand = 0
    for i, (fac, name, note) in enumerate(todo, 1):
        stem = f'{i:02d}_{re.sub(r"[^0-9A-Za-z]+", "_", name)}'

        face, score = face_for(fac, name)
        face_cell = '—— 卡面库里也没找到'
        if face:
            ext = os.path.splitext(face)[1]
            shutil.copyfile(face, os.path.join(face_dir, stem + ext))
            face_cell = f'`原版卡面/{stem}{ext}`（相似度 {score:.2f}）'
            n_face += 1
        if note:
            face_cell += f'<br>⚠️ {note}'

        cands = candidates(pipe, fac, name)
        cell = []
        for j, (p, s) in enumerate(cands, 1):
            ext = os.path.splitext(p)[1]
            fn = f'{stem}__候选{j}_{re.sub(r"[^0-9A-Za-z]+", "_", os.path.basename(p)[:-4])[:40]}{ext}'
            shutil.copyfile(p, os.path.join(cand_dir, fn))
            cell.append(f'{j}. `{fn}`（{s:.2f}）')
            n_cand += 1
        lines.append(f'| {i} | {fac} | {name} | {face_cell} | ' + '<br>'.join(cell) + ' |')

    lines += ['', '---', '',
              '## ' + ('将来真有缺口时怎么办' if not todo else '对完之后怎么办'), '',
              '把对应关系告诉我（例如「12 Lord Kaphrael → `CSM_EmperorsChildren_warlord_Lord Exultant`」），我会：',
              '1. 在 `工具/import_original_art.py` 里加一条**别名** —— 同名跨阵营 / 卡池重复行走',
              '   `ART_ID_ALIAS`（按 `id` 点名），其余走 `ART_NAME_ALIAS`（按卡名）；',
              '2. 重跑 `python 工具/import_original_art.py` 导入；',
              '3. 复跑 `python 工具/import_original_art.py --check`，**那行的「配不上」归零才算完**'
              '（本工具不是判据）。', '']
    open(os.path.join(OUT, 'README.md'), 'w', encoding='utf-8').write('\n'.join(lines))
    print(f'写好：{OUT}（卡面 {n_face} 张 / 候选 {n_cand} 张）'
          + ('　⚠️ 缺口为 0 ⇒ 目录里现在只有这份 README' if not todo else ''))
    return 0


if __name__ == '__main__':
    sys.exit(main())
