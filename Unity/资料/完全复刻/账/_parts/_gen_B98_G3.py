# -*- coding: utf-8 -*-
# B98_G3 的账补丁生成器：以主表当前第 324 / 325 行为基线，只改「状态 / 落点 / 差异 / 轮次 / 备注」五列。
# ⛔ 只写 `账/_parts/B98_G3.tsv`；主表由调度台合并（`merge_ledger_parts.py --only B98_G3`）。
import io

MAIN = r'd:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv'
OUT = r'd:/4/Unity/资料/完全复刻/账/_parts/B98_G3.tsv'

b = io.open(MAIN, 'rb').read()
assert b.count(b'\r') == b.count(b'\r\n'), '主表里有裸 CR ⇒ 行号会算错，停手'
rows = b.decode('utf-8-sig').split('\n')
hdr = rows[0].split('\t')
assert len(hdr) == 11, len(hdr)
assert hdr == ['域', '原版类', '方法', '判据出处', '判据要点', '状态', '我们的落点', '差异', '等级', '轮次', '备注'], hdr

# ---------------- 324 ----------------
r324 = rows[324].split('\t')
assert len(r324) == 11, len(r324)
assert r324[1] == 'BattleManager' and r324[2].startswith('GetAvailableAttackActions('), (r324[1], r324[2])
assert r324[5] == 'B', r324[5]

LAND324 = ('RuleEngine/Core/RuleCorePartial_B98G3AI.cs:65（AttackTargetGate）'
           ' · RuleEngine/Data/SimpleAI.cs:374（唯一调用点，同一物理行追加 `||`）'
           ' · RuleEngine/Core/RuleCorePartial_B97W34.cs:46（AttackActionPowerGate，攻方那两道闸只此一份）'
           ' · RuleEngine/Core/RuleCore.cs:5770（CanAttackNow 改调同一函数）')

DIFF324 = (' ｜[B98_G3 2026-10-29 **B→A**] 目标侧那一闸落到了**动作表自己的**一份判据上：'
           '新建 `RuleCorePartial_B98G3AI.cs:65 AttackTargetGate(ctx,p,attacker,target,ranged)` —— '
           '① `attacker == null ⇒ ErrNotUnit`；② `target == null || EnoughPendingDamageToDie(target) ⇒ ErrNotUnit`'
           '（原版 `GetAvailableAttackActions.c:126` 近战 / `:152` 远程各判一次 ⇒ **两档都不排**；'
           '⚠️ `EnoughPendingDamageToDie(null)` 返 true，故 null 单独判）；'
           '③ 攻方 `pindown(0x3ca)` / 攻击力 ≤0 **转调**既有 `AttackActionPowerGate`（⛔ 不复制第二份）。'
           '调用点**唯一** = `SimpleAI.cs:374`（同一物理行追加 `||`，**零行数变化**）。'
           '⛔ **不再落 `IsValidTarget`**（实测 `play check` ①-b 50→414 张，`RuleCore.cs:5625` 有回退记录）。'
           '**`allowAutoActions == true` 我们无等价物**（`EnumerateActions` 的回合归属闸直接读 `ctx.Active`，`SimpleAI.cs:326`）'
           '⇒ 如实记「无等价物」，⛔ 不给 `EnumerateActions` 加一个没人能置真的形参（死代码）。')

NOTE324 = (' ｜[B98_G3 2026-10-29 波 5b] `typecheck` 0/0（运行时 / 编辑器两个程序集）。'
           '断言 = `CardPresentation/Editor/B98G3_Assert.cs`：§0 控制组（近战 3 ⇒ 表里**真有**那条）· '
           'A1 近战/远程 0 攻 ⇒ 两档都不排 · A1-c **灭自证**（同一次调用里 `IsValidTarget` 对同一对仍返 OK ⇒ 证明闸在动作表、不在共用判据）· '
           'A2 目标将死 + 救回 · A3 `pindown` 只禁近战 · A4 `CanAttackNow` 返回码回归。'
           '⚠️ 本代理不跑 Unity / ruleprobe ⇒ **A5 `play check` 指纹由调度台在同步点跑**。'
           '⚠️ **顺手发现（未改）**：`CardPresentation/Editor/B97W34_Assert.cs` 的 A1–A7 仍按「闸在 `IsValidTarget` 里」断，'
           '而那一版 2026-10-29 已被调度台回退 ⇒ 那几条是**过期断言**、且与本批 A1-c 直接矛盾，须由该文件的所有者改。')

new324 = r324[:5] + ['A', LAND324, r324[7] + DIFF324, r324[8], r324[9] + '·B98_G3', r324[10] + NOTE324]

# ---------------- 325 ----------------
r325 = rows[325].split('\t')
assert len(r325) == 11, len(r325)
assert r325[1] == 'BattleManager' and r325[2].startswith('GetAvailableAttackTargets('), (r325[1], r325[2])
assert r325[5] == 'A', r325[5]

LAND325 = ('RuleEngine/Core/RuleCore.cs:5402（IsValidTarget —— 本行这一格**没变**）'
           ' · RuleEngine/Core/RuleCorePartial_B98G3AI.cs:65（AttackTargetGate —— 只服务动作表那一侧）')

DIFF325 = (' ｜[B98_G3 2026-10-29] 本行这一格（我们 = `RuleCore.IsValidTarget`）**原样保留、逐句不变**；'
           '本波新加的那份 `AttackTargetGate` **只服务动作表那一侧**（`SimpleAI.cs:374`），'
           '⛔ 不并进 `IsValidTarget`（那个口同时服务**技能目标**：实测并进去 ⇒ `play check` ①-b 由 50 张变 414 张）。'
           '原版这两处用的是**同一个合取**（`IsValidAttackTarget` 与 `GetAvailableAttackActions` 都判），'
           '我们因为**没有 `AvailableAction` 中间层**而拆成两份 —— 属实现结构差异（与账 `:193` ③ 同一裁定口径），'
           '**可打集 / 排表集逐格等价**。')

NOTE325 = (' ｜[B98_G3 2026-10-29] 现读复核：`AiScoring.cs:564`（option-5 打分那条路）**没加**这道闸 —— '
           '取证见 `报告/B98_G3.md` §三：原版 `AI__GetPotentialBuffedAttackActions.c:46-52` 只调 `IsValidAttackTarget`，'
           '而 `EnoughPendingDamageToDie` 在 `BattleManager__IsValidAttackTarget.c` **0 命中**'
           '（「目标将死」那一闸**只在** `GetAvailableAttackActions` 里）⇒ 加上去就是**多拒** = 造原版没有的行为；'
           '且「攻击力 ≤0」在 `AiScoring.cs:552` 已经先 `return 0f`。'
           '⚠️ 另一条**顺手发现（未改）**：原版 `IsValidAttackTarget.c:219-222` 有「近战 + `pindown` ⇒ 不合法」，'
           '我们的 `IsValidTarget` 没有这一条（那属于该函数的判据缺口，不在本批白名单内）。')

new325 = r325[:5] + ['A', LAND325, r325[7] + DIFF325, r325[8], r325[9] + '·B98_G3', r325[10] + NOTE325]

for i, row in ((324, new324), (325, new325)):
    assert len(row) == 11, (i, len(row))
    assert all('\t' not in c and '\n' not in c and '\r' not in c for c in row), ('ctl char', i)
    assert row[2] == rows[i].split('\t')[2], ('方法 列被改了（不许）', i)

payload = '\t'.join(hdr) + '\n' + '\t'.join(new324) + '\n' + '\t'.join(new325) + '\n'
io.open(OUT, 'wb').write(payload.encode('utf-8'))

# 回读自证：11 列、状态 A、方法签名原样
back = io.open(OUT, 'rb').read().decode('utf-8').rstrip('\n').split('\n')
assert len(back) == 3, len(back)
for k in (1, 2):
    c = back[k].split('\t')
    assert len(c) == 11, (k, len(c))
    assert c[5] == 'A', c[5]
    assert c[2] == rows[323 + k].split('\t')[2]
print('OK rows', len(back) - 1, 'cols 11 each, status A, len', len(payload))
