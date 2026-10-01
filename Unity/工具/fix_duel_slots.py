# ⚠️ 一次性修复脚本（2026-10-01 已跑过，结果已进仓库）。
# 重新跑是**幂等**的（已经是合法局面的地方不会再改），但**只在** RuleEngineTest.cs
# 回到「有洞棋盘」状态时才有意义；`patch_manual.py` 依赖前两个脚本先跑过。
# -*- coding: utf-8 -*-
"""Duel() 那一族的夹具改成合法局面：默认槽位 1/1 → 3/3，并把用例里的槽位引用一起改。"""
import re, io

P = 'd:/4/Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs'
src = [l.decode('utf-8') for l in io.open(P, 'rb').read().split(b'\r\n')]


def test_bounds(s):
    meth = re.compile(r'^\s{0,8}(?:static\s+|public\s+|private\s+)*(?:void|bool|int|string|UnitState|BattleContext|List<\w+>)\s+(\w+)\s*\(')
    out = []; cur = None; st = 0
    for i, l in enumerate(s, start=1):
        m = meth.match(l)
        if m and m.group(1) != 'Run':
            if cur:
                out.append((cur, st, i - 1))
            cur = m.group(1); st = i
    if cur:
        out.append((cur, st, len(s)))
    return out


# 1) Duel 默认槽位
changed = 0
for i, l in enumerate(src):
    if 'int aSlot = 1, int bSlot = 1' in l:
        src[i] = l.replace('int aSlot = 1, int bSlot = 1', 'int aSlot = 3, int bSlot = 3')
        changed += 1
print('Duel 默认槽改了几处:', changed)

# 2) 用默认槽位调 Duel 的用例：把槽位 1 的引用改成 3
n = 0
for name, a, b in test_bounds(src):
    body = '\n'.join(src[a - 1:b])
    if 'Duel(' not in body:
        continue
    # 只要有用例**没带显式 aSlot/bSlot**，就按「决斗双方在 3 号格」处理
    if re.search(r'Duel\([^;]*?aSlot\s*:', body, re.S):
        pass
    for i in range(a, b + 1):
        l = src[i - 1]
        if 'Duel(' in l and re.search(r'aSlot\s*:', l):
            continue
        orig = l
        l = re.sub(r'(Board\(\s*\w+\s*,\s*[01]\s*,\s*)1(\s*\))', r'\g<1>3\g<2>', l)
        l = re.sub(r'(DeclareAttack\(\s*\w+\s*,\s*0\s*,\s*)1(\s*,\s*1\s*,\s*)1', r'\g<1>3\g<2>3', l)
        l = re.sub(r'(DeclareAttack\(\s*\w+\s*,\s*1\s*,\s*)1(\s*,\s*0\s*,\s*)1', r'\g<1>3\g<2>3', l)
        l = re.sub(r'(Players\[[01]\]\.Board\[)1(\])', r'\g<1>3\g<2>', l)
        # 技能 / 替身行动 / 合法目标 —— 也都指决斗双方（1/1 → 3/3）
        l = re.sub(r'((?:Can)?UseAbility\(\s*\w+\s*,\s*[01]\s*,\s*)1(\s*,\s*)1(\s*[,)])', r'\g<1>3\g<2>3\g<3>', l)
        # ⚠️ 只有**施法者那一格**要改：目标格（第 4 参）本来就写在对方那一侧（2/3/8…），别动
        l = re.sub(r'((?:Can)?UseAbility\(\s*\w+\s*,\s*[01]\s*,\s*)1(\s*[,)])', r'\g<1>3\g<2>', l)
        l = re.sub(r'(CanStartAbility\(\s*\w+\s*,\s*[01]\s*,\s*)1(\s*[,)])', r'\g<1>3\g<2>', l)
        l = re.sub(r'(AvailableAlternative\(\s*\w+\s*,\s*[01]\s*,\s*)1(\s*[,)])', r'\g<1>3\g<2>', l)
        l = re.sub(r'(IsValidTarget\(\s*\w+\s*,\s*[01]\s*,\s*)1(\s*,\s*[01]\s*,\s*)1(\s*[,)])', r'\g<1>3\g<2>3\g<3>', l)
        # 事件里带的「施法者/攻击者格位」
        l = re.sub(r'\((ab|strike|death|back)\.Slot, 1,', r'(\g<1>.Slot, 3,', l)
        if l != orig:
            src[i - 1] = l; n += 1
print('槽位引用改动行数:', n)
io.open(P, 'wb').write(b'\r\n'.join(x.encode('utf-8') for x in src))
