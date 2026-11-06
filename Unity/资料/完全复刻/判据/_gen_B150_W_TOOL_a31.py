# -*- coding: utf-8 -*-
"""B150_W_TOOL · A31-58 / A31-63 重核器（**纯只读**，不写任何账/源码）。

把 `AUD02` 三份裁定的 `？`(58) 与 `❌`(106) 逐行按「我们落点(文件:行)」回读源码，
用**按字段的 setter 关键词**在 ±70 行内找落地证据，输出重核清单。

⚠️ 这是**启发式**（与 `B146_A31_A32_审计.md` §三 自己用的口径一致）：
   命中 ≠ 一定做对，未命中 ≠ 一定没做。**它只用来把「明显已落 / 明显没落」分开**，
   剩下的必须人读。脚本里每个结论都带 `证据=文件:行` 供复核。

用法：
    python -I -X utf8 d:/4/Unity/资料/完全复刻/判据/_gen_B150_W_TOOL_a31.py            # 打到 stdout
    python -I -X utf8 d:/4/Unity/资料/完全复刻/判据/_gen_B150_W_TOOL_a31.py --md <file> # 同时落 md
"""
import io
import os
import re
import sys
from collections import Counter, defaultdict

JUDG = r"D:/4/Unity/资料/完全复刻/判据"
ASSETS = r"D:/4/Unity/MyGame/Assets/CardPresentation"
MAIN = os.path.join(JUDG, "AUD02_serialized_params.tsv")
SRC = [os.path.join(JUDG, "AUD02_裁定_卡UI.tsv"), os.path.join(JUDG, "AUD02_裁定_卡组编辑.tsv")]
B106 = os.path.join(JUDG, "B106_AUD02_余问分级.md")
WIN = 70

# 字段 → 落地关键词（正则）
KW = {
    'm_VerticalAlignment': r'SetVAlign',
    'm_HorizontalAlignment': r'AlignLeft|AlignRight|AlignCenter|SetHAlign|SetAlign',
    'm_fontSize': r'SetAutoFitBox|SetGlyphHeight|fontSize',
    'm_fontSizeBase': r'SetAutoFitBox|SetGlyphHeight',
    'm_enableAutoSizing': r'SetAutoFitBox|autoSize',
    'm_Type': r'NineSlice|CreateTiled|MenuDraw\.Nine|MenuDraw\.Tiled|SetType',
    'm_PreserveAspect': r'SetAspect|PreserveAspect|FitHeight',
    'm_RaycastTarget': r'Raycast|raycast',
    'm_enableWordWrapping': r'SetWrapping|WrappingMode|SetWrapWidth',
    'm_PixelsPerUnitMultiplier': r'NineSlice|ppuMul|border',
    'm_margin': r'Margin|margin',
    'm_ChildAlignment': r'SetChildAlign|childAlignment',
    'm_ChildControlHeight': r'ChildControl',
    'm_ChildForceExpandHeight': r'ChildForceExpand',
}


def load_tsv(f):
    b = io.open(f, 'rb').read().decode('utf-8-sig')
    L = [x.split('\t') for x in b.replace('\r\n', '\n').split('\n') if x.strip()]
    return L[0], L[1:]


def norm_path(p, pref):
    return p[len(pref) + 1:] if p.startswith(pref + '/') else p


def collect():
    h, M = load_tsv(MAIN)
    idx = {(r[0], norm_path(r[1], r[0]), r[2], r[3], r[4]): i for i, r in enumerate(M)}
    ver = {}
    for f in SRC:
        _, RR = load_tsv(f)
        for r in RR:
            ver[idx[(r[0], norm_path(r[1], r[0]), r[2], r[3], r[4])]] = r[9]
    t = io.open(B106, 'rb').read().decode('utf-8')
    for ln in t.replace('\r\n', '\n').split('\n'):
        if not re.match(r'^\| \d+ \|', ln):
            continue
        c = [x.strip() for x in ln.strip().strip('|').split('|')]
        if len(c) != 8:
            continue
        comp, field = (c[3].split('.', 1) if '.' in c[3] else ('', c[3]))
        ver[idx[(c[1], norm_path(c[2].strip('`'), c[1]), comp, field, c[4])]] = c[5]
    return M, ver


_SRC_CACHE = {}


def src_lines(path):
    if path not in _SRC_CACHE:
        full = path if os.path.isabs(path) else os.path.join(ASSETS, path)
        if not os.path.isfile(full):
            _SRC_CACHE[path] = None
        else:
            b = io.open(full, 'rb').read().decode('utf-8', 'replace')
            _SRC_CACHE[path] = b.replace('\r\n', '\n').split('\n')
    return _SRC_CACHE[path]


def locate(land):
    """从「我们落点」列里取第一处 `路径.cs:行`。"""
    m = re.search(r'([A-Za-z0-9_./]+\.cs):(\d+)', land)
    return (m.group(1), int(m.group(2))) if m else (None, 0)


def scan(row):
    path, ln = locate(row[5])
    field = row[3]
    if not path:
        return 'D', '落点列里没有 `文件:行`'
    L = src_lines(path)
    if L is None:
        return 'D', '文件不存在：' + path
    kw = KW.get(field)
    if not kw:
        return 'D', '本脚本没有该字段的关键词：' + field
    pat = re.compile(kw)
    lo = max(1, ln - WIN)
    hi = min(len(L), ln + WIN)
    for i in range(lo, hi + 1):
        if pat.search(L[i - 1]):
            return 'H', '%s:%d' % (path, i)
    return 'M', '（±%d 行内没有 %s）' % (WIN, kw)


def main(argv):
    M, ver = collect()
    q = sorted([i for i, v in ver.items() if v == '？'])
    x = sorted([i for i, v in ver.items() if v == '❌'])
    out = []
    out.append('# B150_W_TOOL · A31-58 / A31-63 重核（脚本产物，⛔ 别手改，重跑会覆盖）')
    out.append('')
    out.append('> 只读启发式：对每行按「我们落点(文件:行)」回读源码，在 **±%d 行**内找该字段的 setter 关键词。' % WIN)
    out.append('> `H` = 附近有落地证据（**不等于做对**，要人读那一行）；`M` = 附近没有 ⇒ **疑似真没做**；`D` = 落点不可用。')
    out.append('')
    for name, rows in (('A31-58（`？` 未判档）', q), ('A31-63（`❌` 确认有差）', x)):
        out.append('## %s —— %d 行' % (name, len(rows)))
        out.append('')
        st = Counter()
        grp = defaultdict(list)
        for i in rows:
            s, ev = scan(M[i])
            st[s] += 1
            grp[(M[i][0], M[i][3])].append((i, s, ev))
        out.append('| 结论 | 条数 |')
        out.append('|---|---:|')
        for k, lbl in (('H', '附近有落地证据'), ('M', '附近没有 ⇒ 疑似真没做'), ('D', '落点不可用 / 无关键词')):
            out.append('| %s | %d |' % (lbl, st.get(k, 0)))
        out.append('')
        out.append('| 账行 | prefab | 节点路径 | 字段 | 原版值 | 我们落点 | 结论 | 证据 |')
        out.append('|---:|---|---|---|---|---|---|---|')
        for i in rows:
            s, ev = scan(M[i])
            r = M[i]
            out.append('| %d | %s | `%s` | %s | %s | %s | **%s** | %s |' % (
                i + 2, r[0][:24], norm_path(r[1], r[0])[-56:], r[3], r[4][:16], r[5][:28], s, ev))
        out.append('')
    txt = '\n'.join(out) + '\n'
    dst = None
    for i, a in enumerate(argv):
        if a == '--md' and i + 1 < len(argv):
            dst = argv[i + 1]
    if dst:
        io.open(dst, 'wb').write(txt.encode('utf-8'))
        print('已写 %s' % dst)
    else:
        sys.stdout.write(txt)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
