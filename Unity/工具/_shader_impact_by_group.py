# -*- coding: utf-8 -*-
"""某个原版 shader「值不值得改走原件」—— 看**用了它的效果**是不是真的更差（**按台账分组控制混淆**）。

**为什么要它**：`资料/普查产出_0918/shader原件可用性_表.md` 判的是「**能不能**改走原件」，
但「**该不该**改」是另一个问题 —— 要用**台账**回答：用了它的效果，`|ln(亮度比)|` 是不是显著更差。

🔴 **必须控制混淆**：全样本直接比会得出**假信号**。实测（2026-09-18）：
`Extra Color` 全样本 中位 0.231 vs 对照组 0.052（看着差 4.4 倍），
**但按台账的 `分组` 一控制就没了** —— E 组（偏亮/偏暗）里 0.698 vs 0.698，**一模一样**。
成因：用了它的效果里有很大比例落在本来就差的 E 组。⇒ **只报全样本 = 会做错决定。**

用法：
    PY=D:/2/Warpforge_tools/py312/python.exe
    $PY d:/4/Unity/工具/_shader_impact_by_group.py "Everguild/FX/Extra Color"
    $PY d:/4/Unity/工具/_shader_impact_by_group.py --top 20        # 按用量列出所有 shader 的分组读数

⚠️ 两个输入都是 `资料/` 下的现成产物，**不跑 Unity**：
  · `普查产出_0917/效果_shader对账.tsv`（效果名 → 材质数 → 原版 shader 列表 → …）
  · `特效还原台账.tsv`（逐效果 `分组` / `判定` / `|ln|` …）
⚠️ `|ln| ≤ 0.336`（亮度比 0.7–1.4）= 台账里的「对得上」带宽；**它是分组用的线，不是验收指标**
   （带宽边界挪 0.05 就摆动几十个，见 `analyze_sweep.py` 的文件头）。
"""
import collections
import statistics as st
import sys

LEDGER = 'd:/4/Unity/资料/特效还原台账.tsv'
SHADERS = 'd:/4/Unity/资料/普查产出_0917/效果_shader对账.tsv'
REF = 0.336


def rows(path):
    return [l.lstrip('\ufeff').split('\t') for l in open(path, encoding='utf-8').read().splitlines()]


def load():
    led = rows(LEDGER)
    hdr = led[0]
    gi, ci, ji = hdr.index('分组'), hdr.index('|ln|'), hdr.index('判定')
    data = []
    for r in led[1:]:
        if len(r) <= max(gi, ci, ji):
            continue
        try:
            v = float(r[ci])
        except ValueError:
            continue
        data.append((r[0], r[gi], r[ji], v))
    sh = rows(SHADERS)
    uses = collections.defaultdict(set)
    for r in sh[1:]:
        if len(r) < 3:
            continue
        for name in r[2].split(','):
            name = name.strip()
            if name:
                uses[name].add(r[0])
    return data, uses


def stat(vals):
    if not vals:
        return 'n=0'
    return 'n=%-4d 中位 %.3f · 均值 %.3f · >%.2f 的 %d 个' % (
        len(vals), st.median(vals), st.mean(vals), REF, sum(1 for v in vals if v > REF))


def report(data, uses, shader):
    used = uses.get(shader, set())
    print('=== %s' % shader)
    print('  全样本  用了: %s' % stat([v for n, g, j, v in data if n in used]))
    print('  全样本  没用: %s' % stat([v for n, g, j, v in data if n not in used]))
    print('  🔴 **按分组控制混淆**（这才是判据）：')
    byg = collections.defaultdict(lambda: ([], []))
    for n, g, j, v in data:
        a, b = byg[g]
        (a if n in used else b).append(v)
    for g in sorted(byg):
        a, b = byg[g]
        print('     %-4s 用了: %-46s 没用: %s' % (g, stat(a), stat(b)))


def main():
    data, uses = load()
    print('台账 %d 行有 |ln| · 对账表里 %d 个 shader' % (len(data), len(uses)))
    if len(sys.argv) >= 2 and sys.argv[1] == '--top':
        k = int(sys.argv[2]) if len(sys.argv) > 2 else 20
        for name, _ in sorted(uses.items(), key=lambda kv: -len(kv[1]))[:k]:
            report(data, uses, name)
            print()
        return 0
    if len(sys.argv) < 2:
        print(__doc__)
        return 2
    report(data, uses, sys.argv[1])
    return 0


if __name__ == '__main__':
    sys.exit(main())
