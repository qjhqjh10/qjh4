# -*- coding: utf-8 -*-
"""生成 B95_WPUMP 的账补丁（12 行）。只读主表 + 写 _parts/B95_WPUMP.tsv。
规矩：二进制读、裸 split("\t")、先把内容算进变量再写。"""
import io, os

LEDGER = r"d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B95_WPUMP.tsv"

b = io.open(LEDGER, "rb").read()
lines = b.decode("utf-8").split("\r\n") if b"\r\n" in b else b.decode("utf-8").split("\n")
hdr = lines[0].split("\t")
assert len(hdr) == 11, len(hdr)
rows = {}
for i, l in enumerate(lines):
    if not l.strip():
        continue
    c = l.split("\t")
    assert len(c) == 11, (i + 1, len(c))
    rows[(c[1].strip(), c[2].strip())] = c

TAG = "[B95_WPUMP 2026-10-29]"

# (原版类, 方法) -> (差异追加, 轮次追加, 备注追加, 落点追加)
PATCH = {}

PATCH[("BattleManager", "AddTriggerCourtesy(CardScript targetCard, CardScript actingCard, int priority, bool blockPlayerActions)")] = (
    TAG + " 复核：两残余仍闭（① `CardDef` 侧 = `KeywordTable.Courtesy`（`CardDef.cs:7773`）+ `Prefixes` 的 "
    "`{\"courtesy\",\"courtesy\"}`（`CardDef.cs:8682`）；② `EffectResolver` 侧 = `RunGrantedTriggerRow`"
    "（`EffectResolver.cs:18488`）由 `RunActiveRowOpsOrGrantedTrigger` 分派）。本件侧再核："
    "`EnqueueGrantedTrigger`（`ActionPump.cs:2013`）的「纯入队 + `RequireActorInPlayOrDying` 出队闸 + "
    "三槽（this=target / acting=caster / chosen=null）」逐句对 `AddTriggerCourtesy.c:20-79` + "
    "`ResolveTriggerCourtesy.c:28-264`；关键词→名字 = `ExtraTriggerName` 的 `courtesy`→`Courtesy`(43)"
    "（`ActionPump.cs:1908`）。⇒ 只剩「休眠」一档（池内 `courtesy` 0 张 / `chosenAbility==400` 0 行）。",
    None,
    TAG + " 零代码改动（只复核）。⛔ 别再把「缺 `chosenAbility 400` 分派」当待办派活。",
    None)

PATCH[("BattleManager", "AddTriggerRelentless(CardScript targetCard, CardScript actingCard, int priority, bool blockPlayerActions)")] = (
    TAG + " 复核：与 L178 **同一份入口**（`ActionPump.EnqueueGrantedTrigger`，两份 `.c` 去常数后逐字节同形），"
    "名字那一跳 = `ExtraTriggerName` 的 `relentless`→`Relentless`(230)（`ActionPump.cs:1909`）；"
    "`KeywordTable.Relentless` / `Prefixes` 都在（`CardDef.cs:7773` / `:8682`），`EffectResolver` 侧 370 分派在位。"
    "⇒ 只剩「休眠」一档（池内 `relentless` 0 张 / `chosenAbility==370` 0 行）。",
    None,
    TAG + " 零代码改动（只复核）。",
    None)

PATCH[("BattleManager", "CancelBattleActionFromQueue(List<BattleAction> queue, int actionIndex)")] = (
    TAG + " 🔴 **残差 ① 已接（等价物，不再是待接点）**：`ActionQueue.CancelAttackSideEffect`"
    "（`ActionQueue.cs:599`）逐句对 `CardScript__CancelAttack.c:1-48` 四格 —— "
    "① 清待结算伤害缓冲（`.c:19-34`）= 本仓伤害**逐段现结、无缓冲** ⇒ 无物可清；"
    "② `cardState ∉ {3 inPlayAttacking, 0x11}` ⇒ `LogWarning` + return（`.c:35-40`）= "
    "`RuleCore.IsAttackingOnBoard`；③ `param_2 != 0 && +0x350 == 0` ⇒ 我们的调用方"
    "（`CancelBattleActionFromQueue.c:37`）恒传 **0** ⇒ 结构上不进；④ `PrepareAttack(0,1,0)`（`.c:45`）= "
    "帧状态机、本仓攻击**在出队时才结算** ⇒ 撤掉这条动作 = 这一记从来没打出去，无局面量要退回。"
    "新增 `ActionQueue.RemoveQueuedActionAt(BattleContext, ActionPriority, int)`（`ActionQueue.cs:582`）"
    "把 ctx 带进 `ApplyCancelSideEffects`（旧的**两参**重载保持原样：拿不到 ctx ⇒ 仍走「待接点」出声，"
    "既有断言 `RuleEngineTest_EngineB94F4.cs:189` 不受影响）。⚠️ **残差 ② 不变**：`0x17 createHandCard` ⇒ "
    "表现层 `SetActive(false)`（`RuleEngine` 里没有 `GameObject`）⇒ 仍出声 + `CancelSideEffectsPendingForTest++`。"
    "⇒ 状态仍 **B**（只因 ②）。",
    "｜B95_WPUMP",
    TAG + " 改的是 `ActionQueue.cs`（本笔白名单）+ `ActionPump.cs:2165` 改调带 ctx 的重载。"
    "⚠️ 行为零变化：`actionType == 1` 的入队点只有两处（伏击续攻 / 落地续攻），而 `cancelEffects…FromQueue` "
    "**按原版 ② 不撤 attack(1)** ⇒ 今天没有任何路径会走到这个副作用。",
    "· RuleEngine/Core/ActionQueue.cs:582（带 ctx 重载）· :599（CancelAttackSideEffect）")

PATCH[("BattleManager", "CancelEffectsFromDisappearingUnit(CardScript disappearingUnit, CardScript targetCard, bool shouldIgnoreDestroy)")] = (
    TAG + " 复核：四道调用序 / 0 调用点不变（原版四个调用点里已接 1 = `EffectResolver.DoJam` 的 ⑤′）。"
    "本笔把下游 `…FromQueue` 的撤销改走带 ctx 的重载（`ActionPump.cs:2165`）⇒ `attack(1)` 的撤销副作用"
    "现在能判状态闸；但原版这一路**本来就不撤 `attack(1)`**（② 那一支）⇒ 行为零变化。",
    None,
    TAG + " 零功能改动（只有一行调用形态变化）。四个调用点的接线仍在 `EffectResolver` / `RuleCore`（白名单外）。",
    None)

PATCH[("BattleManager", "CancelEffectsFromDisappearingUnitFromQueue(CardScript jammedUnit, CardScript targetCard, List<BattleAction> queue, bool shouldIgnoreDestroy)")] = (
    TAG + " 复核：倒序 / 三条命中条件 / 三处结束整条泳道的豁免 / `RemoveQueuedActionAt` 补副作用 全部在位"
    "（`ActionPump.cs:2135`）。本笔把 `q.RemoveQueuedActionAt(lane, i)` 改成 "
    "`q.RemoveQueuedActionAt(ctx, lane, i)`（`ActionPump.cs:2165`）⇒ 撤动作时能拿到 `BattleContext`。"
    "⚠️ 三档豁免 ⓐ(0x2d+poison) / ⓑ(0x1a) 仍**无生产者**（现读 12 处 `actionType:` 回填里无 45/26，"
    "`QueuedAction.DeathType` 全树 0 写点）⇒ 结构上走不到，纯增量。",
    None,
    TAG + " 一行改动（调用形态），行为零变化：`actionType == 1` 本来就被 ② 跳过。",
    None)

PATCH[("BattleManager", "ResolveAction(BattleAction battleAction)")] = (
    TAG + " 复核（零代码改动）：`ActionType` 回填现数 **12 处**（本文件 `EnqueueGrantedTrigger` 的 0x41/0x3e · "
    "`RuleCore` 8 处 = 1/1/4/16/51/61/79/87 · `EffectResolver` 3 处 = 77/77/16），"
    "「剩 7 处保持 0」的口径不变（结构缺口，⛔ 不编枚举语义）。两条消费者现状不变"
    "（`ApplyCancelSideEffects` 的 `1` 那一支本轮**已接等价物**，见 L240；`0x17` 仍待表现层）。",
    None,
    TAG + " 只复核。⛔ 别新增 `EvtKind` / 别按 91 型逐条补枚举语义。",
    None)

PATCH[("BattleManager", "ResolveRevealOrders(BattleAction battleAction)")] = (
    TAG + " 复核：四源 0 调用点不变（`AddRevealOrders` 入队、`ResolveRevealOrders` 无出队口），"
    "生产端 `chosenAbility 280` 池内 0 张 ⇒ 双死、数据一变即可达 ⇒ 保持 **B**、⛔ 不判 D。本笔零改动。",
    None,
    TAG + " 只复核。",
    None)

PATCH[("CardAbility", "CanTriggerAbility(AbilityTrigger trigger, BattleManager manager, CardScript thisCard, CardScript actingCard, CardScript targetCard, bool ignoreRandomTrigger)")] = (
    TAG + " 复核（零代码改动）：`AbilityLevelCriteriaAllows`（`ActionPump.cs:882`）/ "
    "`BoardLevelCriteriaAllows`（`ActionPump.cs:1606`）/ `IsRightContext` 全在位；"
    "残差仍是「六道闸被拆成入队参数 + 两段求值」这一**结构差** + acting 闸覆盖面（在 `EffectResolver` 的 "
    "`FireBoardWhenListener` 那一族）。600/605 的资源前置本轮维持「结构差」结论（同一口径见差异列）。",
    None,
    TAG + " 只复核。",
    None)

PATCH[("CardScript", "HasCancelAttackEffect(CardScript actingCard, CardScript targetCard)")] = (
    TAG + " 复核：完整形态 `Enchantment.cs:888` 的调用点确实是 `RuleCore.cs:8051`（传 "
    "`(ctx, attacker, attacker, target)` = 原版 `ResolveCancelAttack(攻方, 攻方, 原目标)`）；"
    "⚠️ `RuleCore.cs:16738`（`CheckUnitsCancellingAttack`）用的是**单参**重载 —— 那是该 doc 明写的口径"
    "（「只判在不在、不跑五道闸」，与攻方那一侧同口径），⛔ **不是漏切**。三条残差不变"
    "（逐条 ability 判 / `ignoreRandom` 无等价位 / 附魔卡不进卡表）。",
    None,
    TAG + " 只复核。",
    None)

PATCH[("CardScript", "TriggerOtherCardRequiem(CardScript requiemUnit)")] = (
    TAG + " 🔴 **补上「trigger 名」那一跳**：`ExtraTriggerName` 现在有 `requiem` → `Requiem`(430)"
    "（`ActionPump.cs:1909`，判据 = `CardDef.AbilityTriggerNumberOfName` 的 `{\"Requiem\", 430}`"
    "（`CardDef.cs:1932`）+ `BattleManager__ResolveTriggerRequiem.c:218` 打的是「**自己**那条 430」）。"
    "缺它 ⇒ `AbilityContextOfAbilityV14(card, null, \"requiem\")` 回 `-1` ⇒ ability 级四道闸**静默不判**、"
    "`RandomAbilityChoiceOps` 也拿不到 `tn`（与 `courtesy` / `relentless` 逐字同形的缺口）。"
    "🔴 **435（别人）走的是另一条映射口**：`WhenEventKind.Triggers(\"requiem\")` 那条 when-kind 映射"
    "（`EffectResolver.cs:7610` 的 `case \"requiem\": return \"OtherCardRequiem\"`）⇒ 两路别合并。"
    "**残差不变**：发送侧的调用点（原版 = `ResolveAction` 的 `case 0x40` / `ResolveDeadCard.c` 邻位那一支）"
    "落在 `RuleCore`（本笔白名单外）⇒ 仍是 0 调用点。状态仍 **B**（休眠：池内 `requiem` 卡面 0 张、"
    "`abilityTriggers` 含 430 的 0 张、`chosenAbility 390` 0 张）。",
    "｜B95_WPUMP",
    TAG + " 改的是 `ActionPump.cs:1909`（一行，加在既有合并行上 ⇒ **零行号漂移**）+ 类尾 §十一 记判据。"
    "⛔ 行号敏感：本文件的账落点全在 `:1909` 之后，所以解释写在类尾（`ActionPump.cs:2170-2197`）。",
    "· RuleEngine/Core/ActionPump.cs:1909（`requiem`→`Requiem` 430）· :2170（类尾 §十一 判据）")

PATCH[("EntityScript", "EnemyReturnsAttack(CardScript attackedCard, bool isAttacker, AttackTypes attackType)")] = (
    TAG + " 🔴 **残差 ⓑ 闭合（dropPod 那一格不再「未建模 ⇒ 返 null」）**："
    "`GameStaticData.dropPodAttack` = **0**、`dropPodMaxHealth` = **2** —— 判据 = "
    "`GameStaticData__.cctor.c:272-273`（`+0x1f8 = 0` / `+0x1fc = 2`），对齐锚点 = 紧随其后的 "
    "`+0x200 = 0x14(20 guildMaxUsers)` / `+0x204 = 0xb4(180)` / `+0x208 = 0x14(20)` / "
    "`+0x20c = 0x3f800000(1.0f TimeToClosePopups)` 与字段序（`GameStaticData.cs:255-263`）逐个吻合；"
    "两个字段都是 `static readonly` ⇒ **没有序列化覆盖**。"
    "落点：`CounterAttack.DropPodAttack` / `DropPodMaxHealth`（类尾 `CounterAttack.cs:214/:218`）；"
    "`CurrentCombatAttack`（`:85`）与 `CombatDamage`（`:132`）都改成读它，且 dropPod 那道闸按原版"
    "`get_currentCombatAttack.c:22-35` **排在残骸那一档之前**。"
    "⚠️ **仍未建模的只剩「每实例差额」**：`CardScript__AddEffect.c:304-322` 的 `+0x48` 会加/减 "
    "`currentDropPodAttack`，我们没有那个每实例量 ⇒ 只能给出厂值（池内 `dropPod` 0 张 ⇒ 今天不可观察）。"
    "⇒ 状态仍 **B**：残差 ⓐ（我们伤害逐段现结 ⇒ 无「待结算伤害队列」这个对应物）是结构差，"
    "ⓑ 只剩差额层。",
    "｜B95_WPUMP",
    TAG + " 三处调用点现读确认全接（行号 = 调用那一行）：`Data/SimpleAI.cs:604` / `Data/AiScoring.cs:582` / "
    "`CardPresentation/Battle/BattleDriver.cs:34424`。🔴 **顺手发现（别的代理的文件）**：`Data/AiScoring.cs:578-581` 的注释"
    "仍写着「原版这两条路本来就不同源，⛔ 别统一」—— 那句已被账 L1025 现核**撤回**"
    "（`AI__ScoreFromAttack.c:30` 整段转调 `ScoreFromBuffedAttack`，后者用 `EnemyReturnsAttack` ⇒ 两条入口同源）⇒ "
    "该注释应与 `SimpleAI.cs:601` 同口径改成「同源」，建议给 `Data/` 的写手。",
    "· RuleEngine/Core/CounterAttack.cs:214（DropPodAttack）/ :218（DropPodMaxHealth）/ :85 / :132")

PATCH[("EntityScript", "HasEnchantment(AbilityTrigger trigger)")] = (
    TAG + " 复核（零代码改动）：三个闸（`HasWhileInPlayGate` `Enchantment.cs:1479` / `HasMobGate` `:1490` / "
    "`HasRageGate` `:1500`）逐句对 `EntityScript__HasEnchantment.c` 成立；全仓 grep 确认**仍 0 个生产调用点**"
    "（只有 `Editor/RuleEngineTest_EngineB94B3.cs:335` 一处断言）。三处调用点在 `RuleCore` / `EffectResolver`"
    "（本笔白名单外）⇒ 保持 **B**。池内附魔 0 张 ⇒ 休眠，⛔ 不判 D。",
    None,
    TAG + " 只复核。",
    None)

# ---- 生成 ----
MISSING = [k for k in PATCH if k not in rows]
assert not MISSING, MISSING

out = ["\t".join(hdr)]
for k, (diff, rnd, note, land) in PATCH.items():
    c = list(rows[k])
    if diff:
        c[7] = (c[7] + " ｜ " + diff) if c[7].strip() else diff
    if rnd:
        c[9] = (c[9] + rnd) if c[9].strip() else rnd.lstrip("｜")
    if note:
        c[10] = (note + " ｜ " + c[10]) if c[10].strip() else note
    if land:
        c[6] = (c[6] + " " + land) if c[6].strip() else land
    assert len(c) == 11, len(c)
    for f in c:
        assert "\t" not in f and "\n" not in f and "\r" not in f
    out.append("\t".join(c))

data = ("\n".join(out) + "\n").encode("utf-8")
io.open(OUT, "wb").write(data)
print("wrote", OUT, len(PATCH), "rows")
