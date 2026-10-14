# -*- coding: utf-8 -*-
"""第三十会话 · 2026-10-14 · 把四条乙档引擎语义账的【已查实判据】追加进各自那一行（原文保留 = 留痕），
并立两条消息账 + 给 A1962 补一例。
判据来源：只读查证代理（第一权威 = d:/2/tools/decomp_full/ 反编译方法体）+ 主对话采用。
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_yi4.py
"""
import io

TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
MAIN = r'D:\4\项目任务.md'

SEG = {}

SEG['A1155'] = (
    " 🔴 **2026-10-14 判据已查实（只读查证 · 第一权威 = 反编译方法体）**："
    "**① 原版不是「只触发一个单位」** —— `BattleManager__ResolveTriggerPlayerCodex.c:97` 扫 `BattleManager.CardsInPlay`"
    "（**全场**，与 `BroadcastUnitSummoned.c:19` 同一个字段）、`:286-509` 那个 `do{…}while(true)` **循环到 `list.Count` 为止、"
    "触发之后没有早退**（那里的 `break` 只出现在 `get_Item` 之后的 null 守卫）；判据 = `:392 IsInPlay` · "
    "`:445 *(char*)(card+0x40) == *(char*)(local_140+0x40)`（同队；`+0x40` = `EntityScript._isPlayer`）· `:496 CardScript__TriggerCodex` · "
    "`:500 HasCurrentTrait(card, 1000 /*codex*/)`。入口 = `BattleManager__PayCardCost.c:30 → CheckIfCodexTriggered.c:31`，"
    "闸 = **付款后剩余能量 == 0**（`CheckIfCodexTriggered.c:31 if (param_3 == 0)`）。卡面旁证：`UM35 Codex Discipline`"
    "「…trigger **their** Codex abilities」· `UM95 Duty's End`「Trigger the Codex ability of **all** friendly units」。"
    "⇒ **我们多了一道 `break`（= 少触发）**：现读 `RuleEngine/Core/RuleCore.cs:7936  break; // ② 一次只触发第一个` "
    "⇒ **要做**（去掉 break，逐张带 `Codex` 的单位各发一次）。⚠️ 顺手：`AbilityLogic__PlayAbility.c:1694 AddTriggerCodex` 落在一个 "
    "`while` 循环里（能力 `0x19a = 410 = FollowUpResolution`）⇒「强制触发 Codex」那条路**可能也是多目标**（对应 `UM35`/`UM95` 那两句卡面）—— **未逐句核**。"
    "**② `Prayed` 时长（已核）**：原版是 `EntityScript.isPraying`（`dump.cs:22005`，`+0x67`），**置真一处、置假两处** ⇒ "
    "真值区间 = 「祈祷那一刻」→ **下一个回合开始（不分敌我）** 或 **自己挨一次伤害**，以先到者为准。"
    "置真 = `BattleManager__ResolveTriggerPray.c:266 set_isPraying(target, 1)`（排在 `:217 OnTrigger(0x28f/*Pray=655*/)` 之后）；"
    "置假① = `CardScript__OnTurnStart.c:100 *(param_1 + 0x67) = 0;`（**无条件** —— `BroadcastTurnStart.c:20-51` 对 `+0x470` 全场卡 + 双方手牌逐张 `OnTurnStart`）· "
    "置假② = `CardScript__ResolveDamageDealt.c:168`（闸 = 受伤者 == 自己）。`grep -rn \"+ 0x67)\" decomp_full/` **全量只有这 3 个写点**。"
    "⇒ **要做两处**：**「清」要改成「每次回合开始、不分敌我」**（我们 `RuleEngine/Data/UnitState.cs:1211 Prayed = false;` 在按回合重置那一批里，"
    "且 `:1200-1210` 的注释写明**只刷当前行动方**）+ **补「自己受伤害就清」**（我们完全没有）。"
    "⚠️ `RuleCore.cs:7505-7507` 那句自标「时长尚未核」的 doc **现在可以改成已核**。"
    "⚠️ 另两条顺手：`decomp_full/CardScript__SetPray.c` 的**内容不是 `SetPray`**（「文件名与内容对不上」又一例 ⇒ 并入 `A1962`）· "
    "`RuleCore.cs:7919-7927` 的注释把 Codex 入口**指错文件**（现读真入口 = `PayCardCost.c:30 → CheckIfCodexTriggered.c:31`，"
    "`CanPlayWithCodexNow` 只是「能不能打」的查询）⇒ **一并订正**。"
)

SEG['A1335'] = (
    " 🔴 **2026-10-14 判据已查实（只读查证 · 反编译 `CardScript._ReceiveDamage_d__381__MoveNext.c` 逐行）** —— **够写代码**："
    "原版是**两次独立的 `DamageInfo`**，第二次在第一次**结算体内部**现场构造并入队。**逐行算式与写回顺序**："
    "① **主伤害三档路由**：`:60 HasCurrentTrait(card, 0xe6 /*230 = dropPod*/)` 非 0 ⇒ `:159-183` 走空投舱池（读 `+0xcc = _currentDropPodHealth`、写回 `:178-182`，**不减护甲、生命一点不动**）；"
    "否则生命档：`:62-63`（`CurrentBastion < 1` 或 `damageType == 10`）⇒ `:75-94` 若 `armour ≥ 1 && damageValue ≥ 1` 则 **`damageValue = Max(1, damageValue − armour)`（原地改 `DamageInfo+0x18`）** → `:111-124` `health(+0x68) -= damageValue`；"
    "否则（有堡垒）`:128-131 iVar8 = RemoveBastionDamage(card, damageValue)`、**`bVar17 = (iVar8 >= 0)`**（= 被堡垒挡下）、`:145-153 iVar8 < 1` 时 `health += iVar8`（负 = 溢出打进血）。"
    "② **易伤第二发（`:188-258`）**：闸 `:188-191` = `CurrentVulnerable > 0` ∧ 无 `dodge(240)` ∧ 无 `shield(80)` ∧ 无 `invulnerable(310)` ∧ **`!bVar17`（堡垒没挡下）**；"
    "`:197-198 IsVulnerableDamage(damageType) = ((d−7) < 2)`（`DamageType.Vulnerable = 7 / VulnerableCombat = 8`）⇒ **已是易伤伤害就不再递归**；"
    "`:200 damageValue > 0` · `:210-211 currentHealth > 0 && isRemnant(+0x65) == 0`（**主伤害落完之后血还 >0 且不是残骸**）。"
    "**造第二发** `:226` = `DamageInfo(actingCard = 原+0x10, damageValue = CurrentVulnerable(card), damageType = (原 == 4 /*Attack*/ ? 8 : 7), secondaryValue = -1, absorbed = false)`。"
    "**投递** `:252-254` = `DealMultiDamage(bm, newDmgInfo, List{被打的那张}, priority 2, false, 被打的那张, false, false)`；"
    "`BattleManager__DealMultiDamage.c` 里**先** `AddAutoActionToQueue(action, 2, 0)`、**再** `RecordPendingDamage(target, newDmgInfo)`（写 `card+0xf8`）。"
    "③ 第二发**再走一遍同一个结算体** ⇒ 也套 `Max(1, V − armour)`。**写回顺序** = 主伤害写 `+0x68` → 判 `>0 && !remnant` → 构造易伤 Info → 入队 + `RecordPendingDamage` → 该 action 结算时再 `Max(1, V−A)` 写 `+0x68`。"
    "**堡垒档差别**：没打穿 ⇒ `bVar17 = true` ⇒ **第二发不发**；打穿 ⇒ 溢出进血 **且**第二发照发。"
    "我方现状：`RuleCore.cs:4756 VulnerableDamagePart`（`Max(1, v − armor)`）· `:4920 actual += VulnerableDamagePart(u)`；doc `:4743-4758` 自列三档未做。"
    "🔴 **同一次【改账文一条】**（铁律 5）：doc 写三档「**今天都不可达**（全池 0 张卡带 dropPod/bastion）」—— ①②（空投舱 / 堡垒）确实不可达，"
    "**但 ③（`:211`「主伤害后血还 >0」那道闸）与那两个模型无关、是纯【顺序】问题**，而 `vulnerable` 是可达的 ⇒ "
    "「**易伤 + 这一下主伤害本身就打死**」这局**今天就能构造**（⚠️ 判据是读出来的、**还没跑对局验证**）。"
    "⚠️ **未查**：`DealMultiDamageVariable` / `DealMultiDamageAndDestroySelf` 有没有同样的第二发 · `priority = 2` 在动作队列里插到哪儿 · `bVar17` 的全部赋值点（只读到 `:130`）。"
)

SEG['A1595'] = (
    " ✅ **2026-10-14 判据已查实 ⇒ 本笔【可核销】（只读查证 · 反编译）**：**三处都不走召唤广播** —— 原版全库 "
    "`BattleManagerSupport.BroadcastUnitSummoned` **只有 2 个调用点**，都是「真的把一张新单位放进场」那两条协程："
    "`BattleManager._ResolveSummonUnit_d__510__MoveNext.c:185` 与 `_ResolveCardAutoSummoned_d__511__MoveNext.c:292`。"
    "**残骸翻回来**走 `ResolveTransformFromRemnant.c:82 → CardScript__TransformFromRemnant.c:11`（`+0x65 = 0` 翻转位）+ `:13 ChangeCardToRemnant` —— "
    "**整条链一个 broadcast 都没有**；**教程起始单位**走 `SetupStartingTroops.c:30/:40 → CreateStartingTroop.c:47-50`（只 `SetActive` / `SetAsUnitInPlay` / `CardSetup` / "
    "`ActivateTraitsOnSummonOrEnchantment` / `SetUnitOrderInPlay` / `MoveMinionIntoSlot`）—— **也无 broadcast**。"
    "⇒ **我们「三处不接」的现状是对的**。🔴 **同一次【改账文一条】**：账文写「**5 个**入口」，而源头（`资料/普查产出_第二十会话/W1_信仰阈值落地.md` §四·1）**只列了 3 处** ⇒ "
    "`RuleCore.cs` 残骸那处 · `EffectResolver.DoReanimate` · `TutorialScript.PlaceTroops`；**「5」核不到出处，按 3 记**。"
    "⚠️ 我方现状已在白名单外现读：`EffectResolver.cs:8486-8515 DoReanimate` 候选①（**不调** `ResolveFaithAbility` ✓）· `TutorialScript.cs:1273 PlaceTroops`（直接 `BoardSlots.Insert` + `Emit(Deploy)` ✓）；"
    "`RuleCore.cs:6243` 是**变成**残骸的死亡侧、不是入场、**不在本笔射程**。⚠️ `DoReanimate` 候选②（墓地兜底 `:8545` 走 `DeployFree`）**会**点火 —— 但那条路**是我们挑的、不是原版**。"
)

SEG['A1692'] = (
    " 🔴 **2026-10-14 判据已查实（只读查证 · 反编译）** —— ⚠️ **结论要改写：原版【没有「目标格被占」这一档】，不是「原版选择盖掉」**："
    "`MinionManager__GetNextSlotWithoutDisplacing.c:14-30` **全文只有 4 行逻辑**（`if (leftMinions(+0x70).Count < rightMinions(+0x78).Count) return ~leftMinions.Count; else return rightMinions.Count + 1;`）"
    "—— **只读两个 `List.Count`、不做任何占用检查**；而两侧是**无洞 `List`**（`Remove` 补位）⇒ 算出来那格**按定义就是空的**。"
    "它返回的是**有符号格号**（左 = `~(n)` = −(n+1) · 右 = `n+1`），与我们的 `ourSlot = 4 + 有符号下标`（左 `3−n` / 右 `5+n`）**吻合**；调用点 `_ResolveSummonUnit_d__510__MoveNext.c:135/:274`。"
    "旁证：`MinionManager__AvailableLeftSlot.c` / `AvailableRightSlot.c` 也只比 `List.Count`。"
    "⇒ **「加一道闸」不是复刻原版，而是补我们表示法的洞**（我们的 `CountOnSide` 数的是**非空格数**）—— 按**铁律 11 仍应做**，"
    "但代码注释必须写「**原版行为不可比**」，⛔ **别写成「照原版」**。我方现状：`RuleEngine/Core/BoardSlots.cs:239 NextWithoutDisplacing` = `SlotOf(side, CountOnSide(side))`；"
    "`RuleCore.cs:3031` 取 `dst`、**`:3044 ps.Board[dst] = unit` 无条件覆盖**（同文件 `RemoveAt` `:215-222` 有 `WarnHoley` 降级，这里没有）。"
    "⚠️ **未查**：原版有没有**别的路径**让两侧 `List` 出现「洞」（如 `MoveMinionIntoSlot` 直接指定格号）。"
)

A1975 = (
    "| 🆕 **`A1975`** | ⚠️ **教程起始单位可能少激活一格关键词**（2026-10-14 只读查证顺手报，⛔ **未核、未改**）："
    "原版 `BattleManager__CreateStartingTroop.c:47` **调了** `ActivateTraitsOnSummonOrEnchantment`，而我们 `TutorialScript.cs:1273 PlaceTroops` **没有调** ⇒ "
    "教程起始单位的关键词（如光环/前缀类）**可能少激活一格**。⇒ **要做**：先核「我们那批单位的关键词到底靠谁激活」（若靠另一条路已覆盖 ⇒ 如实判成假警报）。"
    "判据 = 反编译 `CreateStartingTroop.c:47` + 现读 `RuleEngine/Core/TutorialScript.cs:1273`。 |"
)

A1976 = (
    "| 🆕 **`A1976`** | ⚠️ **Codex 卡的张数有两说、没查为什么**（2026-10-14 只读查证顺手报）：用 `\\bCodex\\b` 扫 "
    "`Assets/RuleEngine/Resources/cards_engine.json` 的 `desc` 得 **31 张**，而 `RuleEngine/Core/CardDef.cs:603-604` 的口径写 **20 张**。"
    "⇒ **要做**：查清差额（猜想：一方含 `[Codex icon]` 写法，**未查**）—— 这条**与 `A1155①`（Codex 逐张触发）同族**，改 Codex 之前先把它弄清。"
    "判据 = 2026-10-14 现扫 + `CardDef.cs:603-604`。 |"
)


def append_seg(path, cid, seg):
    b = io.open(path, 'rb').read()
    assert b.count(b'\r\n') == 0, path
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| ') and ('**`%s`**' % cid) in l]
    assert len(hits) == 1, (cid, len(hits))
    i = hits[0]
    assert lines[i].rstrip().endswith(' |'), lines[i][:70]
    lines[i] = lines[i].rstrip()[:-1].rstrip() + seg + ' |'
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(path, 'wb').write(out.encode('utf-8'))
    print('  +%s appended' % cid)


def insert_after(path, marker, text):
    b = io.open(path, 'rb').read()
    assert b.count(b'\r\n') == 0, path
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| ') and marker in l]
    assert len(hits) == 1, (marker, len(hits))
    lines[hits[0] + 1:hits[0] + 1] = [text]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(path, 'wb').write(out.encode('utf-8'))
    print('  +inserted after %s' % marker)


def main():
    for cid in ('A1155', 'A1335', 'A1595', 'A1692'):
        append_seg(TASKS, cid, SEG[cid])
    insert_after(MAIN, '**`A1974`**', '\n'.join([A1975, A1976]))


if __name__ == '__main__':
    main()
