# -*- coding: utf-8 -*-
"""B150_W_TOOL · 往补丁里追加 B148-LEDGER-831 / :643 两行（**只写 _parts 补丁，⛔ 不动主表**）。

判据：`报告/B148_W7_战斗表现与窗口序.md` §四·1/§四·2 ＋ 现读 `.c`
（`decomp_full/TargetReticleController__UpdateTrail.c:58,70`）＋ 现读代码
（`CardPresentation/Battle/TargetReticle.cs:522-529`）。
"""
import io
import os

LEDGER = r"D:/4/Unity/资料/完全复刻/账/战斗界面_HUD.tsv"
PATCH = r"D:/4/Unity/资料/完全复刻/账/_parts/B150_W_TOOL.tsv"


def read_rows(p):
    b = io.open(p, 'rb').read()
    t = b.decode('utf-8-sig').replace('\r\n', '\n')
    L = t.split('\n')
    while L and L[-1] == '':
        L.pop()
    return L[0].split('\t'), [x.split('\t') for x in L[1:]]


HDR, R = read_rows(LEDGER)
assert len(HDR) == 11, len(HDR)

row831 = R[831 - 2]
row643 = R[643 - 2]
assert row831[2] == 'UpdateTrail()', row831[2]
assert row643[2].startswith('UpdateTrail(Vector3'), row643[2]

# ---- 831：判据要点订正（原写「两跳都用 hudCamera」，与 .c:58/:70 冲突）----
r831 = list(row831)
r831[4] = ('hudCamera(+0xA0) **只**用于 `WorldToScreenPoint` 投屏（`.c:58`）；'
           '出射那一跳 `ScreenPointToRay` 走 **boardCamera(+0xA8)**（`.c:70`）。'
           '⚠️ 2026-11-05 `B148-W7` 订正：原写「`WorldToScreenPoint` / `ScreenPointToRay` 都用 hudCamera」'
           '与 `.c` 冲突，那是我们改前那段错误口径的来源。')
r831[10] = (row831[10].rstrip() +
            ' ｜⚠️ 2026-11-05 订正（`B148-W7` · 铁律 5）：判据要点已按 `.c:58/:70` 改成 hud→board；'
            '现读代码 `TargetReticle.cs:522-529` 已是 `hudCam.WorldToScreenPoint` → `boardCam.ScreenPointToRay` '
            '⇒ 状态 A 维持（原来的 A 是按错口径判的，现在按正确口径**重新判过**仍是 A）。')

# ---- 643：差异列改「已修（B148-W7）」----
r643 = list(row643)
r643[7] = ('已修（B148-W7）：`ResolveAim`（`CardPresentation/Battle/TargetReticle.cs:522-529`）现读 = '
           '`hudCam.WorldToScreenPoint`（`.c:58`）→ `boardCam.ScreenPointToRay`（`.c:70`）；'
           '原来那句「两台相机对调」已过期。状态仍留 B：**修复未经真机/宿主验收**（见备注）。')

for r in (r831, r643):
    assert len(r) == 11, len(r)
    for c in r:
        assert '\t' not in c and '\n' not in c

# 追加到补丁（若已有同 key 行则替换）
buf = io.open(PATCH, 'rb').read().decode('utf-8').replace('\r\n', '\n')
L = [x for x in buf.split('\n')]
while L and L[-1] == '':
    L.pop()
hdr = L[0].split('\t')
assert len(hdr) == 11
body = [x.split('\t') for x in L[1:]]
keys = {(c[1], c[2]) for c in body}
added = 0
for r in (r831, r643):
    k = (r[1], r[2])
    if k in keys:
        body = [r if (c[1], c[2]) == k else c for c in body]
    else:
        body.append(r)
        added += 1

out = '\t'.join(hdr) + '\n' + '\n'.join('\t'.join(c) for c in body) + '\n'
io.open(PATCH, 'wb').write(out.encode('utf-8'))
print('补丁现有 %d 行（本次新增 %d）→ %s' % (len(body), added, PATCH))
