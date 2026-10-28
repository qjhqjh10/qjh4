# -*- coding: utf-8 -*-
# B93_U1c — 列修复补丁：B93_U1 那轮把「判据要点」正文误写进了【判据出处】列（off-by-one，r[3] 应为 r[4]）。
# 本补丁对「受损且不在 B93_U1b 里」的行，把【判据出处】恢复为 HEAD 原值、正文并回【判据要点】。
# 不碰状态 / 落点 / 差异 / 等级；键与 B93_U1b 完全不重叠（无跨补丁冲突）。
import io

MAIN = 'd:/4/Unity/资料/完全复刻/账/未分类_其他.tsv'
HEADT = 'd:/4/_tmp_view/b93u1/main_head.tsv'
U1 = 'd:/4/Unity/资料/完全复刻/账/_parts/B93_U1.tsv'
U1B = 'd:/4/Unity/资料/完全复刻/账/_parts/B93_U1b.tsv'
OUT = 'd:/4/Unity/资料/完全复刻/账/_parts/B93_U1c.tsv'
TAG = '｜[B93_U1c 2026-10-28] '


def load(p):
    d = {}
    for l in io.open(p, 'rb').read().decode('utf-8').split('\n')[1:]:
        if l.strip():
            f = l.split('\t')
            d[(f[1], f[2])] = f
    return d


main = io.open(MAIN, 'rb').read().decode('utf-8').split('\n')
hdr = main[0].split('\t')
assert len(hdr) == 11
cur = load(MAIN)
head = load(HEADT)
u1keys = [l.split('\t')[1:3] and (l.split('\t')[1], l.split('\t')[2]) for l in
          io.open(U1, 'rb').read().decode('utf-8').split('\n')[1:] if l.strip()]
u1bkeys = {(l.split('\t')[1], l.split('\t')[2]) for l in
           io.open(U1B, 'rb').read().decode('utf-8').split('\n')[1:] if l.strip()}

out = ['\t'.join(hdr)]
n_repair = 0
n_skip_ok = 0
for k in u1keys:
    if k in u1bkeys:
        continue                                     # 由 B93_U1b 一并修
    r = list(cur[k])
    misplaced = r[3]                                  # U1 误放进「判据出处」的正文
    orig_src = head[k][3] if k in head else r[3]
    if misplaced == orig_src:
        n_skip_ok += 1
        continue                                      # 该行没被写坏
    assert orig_src.strip(), ('HEAD 出处为空', k)
    old_pt = r[4]
    if not old_pt.strip():
        new_pt = misplaced
    elif misplaced.startswith(old_pt):
        new_pt = misplaced                            # U1 的正文就以旧要点开头 ⇒ 直接用它
    elif old_pt[:12].strip() and misplaced.startswith(old_pt[:12]):
        new_pt = misplaced                            # 同一出处前缀起头（如 `UIExtension.cs:36…`）⇒ 也只留正文
    else:
        new_pt = old_pt + '｜🆕 ' + misplaced
    r[3] = orig_src
    r[4] = new_pt
    r[9] = (r[9] + ' · B93_U1c') if ('B93_U1c' not in r[9]) else r[9]
    r[10] = (r[10] + TAG + '列修复：B93_U1 把「判据要点」正文误写进了【判据出处】列（off-by-one）⇒ '
             '本轮恢复出处 = `' + orig_src[:60] + '`，正文并回判据要点。状态/落点/差异**未动**') if r[10].strip() \
        else (TAG + '列修复（同上）')
    assert len(r) == 11, (k, len(r))
    out.append('\t'.join(r))
    n_repair += 1

io.open(OUT, 'wb').write(('\n'.join(out) + '\n').encode('utf-8'))

chk = io.open(OUT, 'rb').read().decode('utf-8')
cl = chk.split('\n')
assert cl[0].split('\t') == hdr
m = 0
for l in cl[1:]:
    if not l.strip():
        continue
    f = l.split('\t')
    assert len(f) == 11, ('cols', len(f))
    k = (f[1], f[2])
    assert k not in u1bkeys, ('撞 B93_U1b', k)
    assert k in head, ('不在 HEAD', k)
    assert f[3] == head[k][3], ('出处没恢复', k)
    assert f[3].strip() and f[3] != f[4], ('出处仍是正文', k)
    m += 1
assert '\r' not in chk
print('repaired rows', m, '| skipped(未受损)', n_skip_ok)
