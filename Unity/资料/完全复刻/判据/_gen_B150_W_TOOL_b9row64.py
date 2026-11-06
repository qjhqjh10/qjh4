# -*- coding: utf-8 -*-
"""B150_W_TOOL · `工具-54` 行 64 就地订正（铁律 5）：`仍缺` → `⛔ 判据本身错`。

判据（链）：
  · `数据/游戏数据/ability_anim_durations.json` 现读 13 条，`SW9/ASH74/GOF77/SOR23` **0 命中**；
  · `CardPresentation/Core/AbilityAnimDurations.cs:116` `NoEntryFallback = 0f` ——
    表里没有这张卡 ⇒ **0 秒**，依据 `AbilityLogic__PlayAnim.c:46-58/230-236`（`cardAnimations` 表空 ⇒ 协程首调即止）；
  · 活状态表 `工具-54` 那一行已记 `WK` 2026-11-04 现读结论（同一条）。
"""
import io

P = r"D:/4/Unity/资料/完全复刻/判据/B9_旧任务回核_结论.tsv"
b = io.open(P, 'rb').read()
assert b.count(b'\r\n') == 0, '该文件不是 LF，先别改'
t = b.decode('utf-8-sig')
L = t.split('\n')
while L and L[-1] == '':
    L.pop()
hdr = L[0].split('\t')
assert len(hdr) == 8, hdr

row = L[64 - 1].split('\t')
assert len(row) == 8, len(row)
assert row[0] == '64', row[0]
assert row[3] == '仍缺', row[3]

row[3] = '⛔ 判据本身错'
row[5] = ('无（原判据不成立）：`SW9/ASH74/GOF77/SOR23` 原版 `cardAnimations` **全空 ⇒ 0.0 s**，'
          '我们已 0.0（`CardPresentation/Core/AbilityAnimDurations.cs:116` `NoEntryFallback = 0f`，'
          '判据 = `decomp_full/AbilityLogic__PlayAnim.c:46-58/230-236`）；'
          '`SOR23` 的「tg=-1 单点播」前提不成立（其 3 条全 `tg=605`）。'
          '本文件现读 13 条、这 4 张 **0 命中**。'
          '⚠️ 2026-11-05 `B150_W_TOOL` 就地订正（铁律 5）：原写「R3（ability 桶 tg=-1 单点播 59 行/31 卡）· '
          'R12（7 张卡 1.0 s 兜底 + 未归口件）」。')
row[6] = '无（判据错，不派活）'
row[7] = '—'

L[64 - 1] = '\t'.join(row)
out = '\n'.join(L) + '\n'
io.open(P, 'wb').write(out.encode('utf-8'))
print('已订正 %s 第 64 行' % P)
