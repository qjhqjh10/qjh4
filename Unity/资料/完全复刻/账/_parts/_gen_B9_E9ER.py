# -*- coding: utf-8 -*-
# 生成 B9_E9ER.tsv（只写本次改过的 6 行；键 = (原版类, 方法) 精确串）
import io, os

LEDGER = r"d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B9_E9ER.tsv"
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

b = io.open(LEDGER, "rb").read()
nl = "\r\n" if b.count(b"\r\n") == b.count(b"\n") and b.count(b"\r\n") else "\n"
rows = [l.split("\t") for l in b.decode("utf-8-sig").split(nl) if l.strip()]
by_key = {(r[1], r[2]): r for r in rows[1:]}

PATCH = {}

# ---- ③ CheckIfCodexTriggered：闸② 的战术调用点已接 ----
k = ("BattleManager", "CheckIfCodexTriggered(CardScript card, int manaLeft, CardScript targetCard, bool shouldCheckCost)")
r = list(by_key[k])
r[5] = "A"
r[6] = ("RuleEngine/Core/RuleCore.cs:13083（CheckCodex）（等价实现 = `CheckCodex(ctx, p, playedCard, shouldCheckCost)`："
        "闸① `Energy != 0` 早退 · 闸② `CostOf(...) < 1 ⇒ return`；"
        "两个调用点都接上了 —— 单位卡 `RuleCore.cs:4214`、战术卡 `RuleEngine/Core/EffectResolver.cs:2739`）")
r[7] = ("✅ **2026-10-26 批次 9（`E9ER`）补完最后一个调用点**：战术卡那条路改调 "
        "`CheckCodex(ctx, p, inst, shouldCheckCost: true)`（`inst` = 刚打出的那一份 CardInstance）。"
        "改前那里只有 `(ctx, p)` ⇒ `playedCard == null` ⇒ 闸② 放行（0 费战术卡仍会触发 Codex，比原版宽）。"
        "`shouldCheckCost` 缺省仍 `false`（誓约两条路原版不设这道闸）。")
r[10] = (r[10].rstrip() + " ｜ 2026-10-26 批次 9 `E9ER`：战术调用点已接（`EffectResolver.cs:2739`）；"
         "`play check` 指纹仍 `16db33fa…`（= E7P06 后的已确认基线，未因本笔变化）—— 夹具里没有"
         "「0 费战术打出后能量恰好归零 ∧ 场上有 Codex 单位」的组合 ⇒ 本闸在夹具上不可观测。")
PATCH[k] = r

# ---- ④ + ② IsValidSpellTarget：camouflage 收窄到「施放者是战术卡」；目标身份按对象而非格号 ----
k = ("BattleManager", "IsValidSpellTarget(CardScript spell, CardScript target)")
r = list(by_key[k])
r[6] = ("RuleEngine/Core/EffectResolver.cs:2140（IsLegalPlayTarget）（等价实现 = 战术/法术目标合法性；"
        "本批另补两处：`AddSide`（:2897）的 `tacticCaster` 形参 · `IsLegalPick`（:2005）的同名形参）")
r[7] = ("🔴 **2026-10-26 批次 9（`E9ER`）改了两格**：\n"
        "① camouflage 闸**按原版口径收窄**：原版 `RawCardScript__IsValidTarget.c:16-40` 的 camouflage 支多一个"
        "「施放者 `cardType == 0x14 (§Tactic)`」，而我们改前对**所有**来源都挡（比原版严）⇒ 现在只在"
        "「施放者是战术卡」时挡（`AddSide(..., tacticCaster:)`，由 `IsLegalPick`/`PlayTargetPool` 按那张牌判）。"
        "池内 camouflage 0 张可达 ⇒ 零行为变化。\n"
        "② **目标身份按「对象」而不是「格号」解**（`Side == \"any\"` 那 19 张卡打不到对面）：原版本方法的形参"
        "就是**目标 CardScript**（侧由目标自带），而 `CanPlayTactic` / `PlayTacticCore` 改前只拿格号反查一侧、"
        "「任意一侧」恒读我方 ⇒ 玩家点敌方却打到自己人同号格。现在 `CanPlayTactic`/`PlayTactic` 收了 `UnitState chosen`"
        "（`RuleCore.PlayCardCore` 那一句同时递格号与实例），格号只在没有实例时兜底（`PickedBySlot`）。")
r[10] = ("L5 2026-10-26：只做落点订正 + 判据要点补齐，未逐句核 ⇒ 保持 B（下一轮按 .c 71 行逐句对）。"
         "｜ 批次 9 `E9ER`：camouflage 那格已按原版收窄；`Side==\"any\"` 的 19 张卡量具 = "
         "`bash 工具/ruleprobe.sh play ASH73 preset pick=1`（改后打敌方 `FIX_E1` 6→1；改前打自己督军 28→26）。")
PATCH[k] = r

# ---- ⑥ CheckForSkullEarned：发射点接上 ----
k = ("CardScript", "CheckForSkullEarned()")
r = list(by_key[k])
r[5] = "A"
r[6] = ("RuleEngine/Core/EffectResolver.cs:16331（NoteWarlordHealthForSkulls）"
        "（等价实现 = `NoteWarlordHealthForSkulls`：`DeckRules.SkullsFor(血)` = 原版 `GetEnemySkullCount`；"
        "每局附加状态（`ConditionalWeakTable`）按**督军的主人**记「跨到第几档」，**变大才**广播一次；"
        "接收端 = `EffectResolver.cs:15705` 的 `BroadcastEarnedSkull`（只跑棋盘跳 + 常驻效果跳）。"
        "两个真实血量写点已调它：`RuleCore.cs:8849`（`Hurt` 的 `u.Health -= actual` 之后）· "
        "`RuleCore.cs:3076`（牌库抽空的疲劳支））")
r[7] = ("✅ **2026-10-26 批次 9（`E9ER`）把缺的三件补齐**：① 计数（每局 CWT 按督军主人记 0..3，"
        "与 `+0x354` 同语义：治回去不回退）② 血量写点之后的检查③ 广播（`boardAndPersistentOnly: true`）。"
        "口径照 `CardScript__CheckForSkullEarned.c:1-69` 逐句：只对 `cardType == 10`（我们 = `IsWarlord`）· "
        "会死就不广播（我们 = `!IsAlive` 早退）· **跳几档都只广播一次**（⛔ 不是逐档 for 循环）· "
        "得主 = **被打那个督军的对手**（`who = 1 - owner`，`GetEnemySkullCount` 的语义）。"
        "⚠️ 残留（如实记）：原版 `EnoughPendingDamageToDie` 看的是**待结算伤害**，我们是「血已扣完且 ≤ 0」等价判。")
r[10] = ("【批次3·E2】改动请求（非白名单）：`RuleCore.ApplyDamageCore`/`HealUnit` 写完督军血量后调一行 "
         "`EffectResolver.BroadcastEarnedSkull(ctx, p)`；计数放 `PlayerState`。判据链已核完（3 个方法体 + 2 个广播器）。"
         "⚠️ 骷髅是**胜负条件**，建议优先 ｜ 批次 9 `E9ER`：**已接完**，计数没动 `PlayerState`（改用每局 CWT 派生状态，"
         "不参与指纹/存档）。池内 0 张可达（`abilityTriggers` 含 640 的卡 0 张）⇒ `play check` 指纹不变。")
PATCH[k] = r

# ---- 追加项 1148 AddCardNotDrawnToHandAtIndex：两个调用点已由批次 6 G14 改完 ----
k = ("PlayerHand", "AddCardNotDrawnToHandAtIndex(CardScript card, int indexInHand)")
r = list(by_key[k])
r[5] = "A"
r[6] = ("RuleEngine/Core/RuleCore.cs:14162（AddCardToHandAtWithinLimit）（等价实现 = `Hand.Insert(at, inst)` + 满手进墓地；"
        "调用方两处都已改：`RuleEngine/Core/EffectResolver.cs:3670`（deploy-from-hand 打不出去 ⇒ 原位插回，"
        "下标 = 拿走前的 `Hand.IndexOf`）与 `:3873`（`autoPlayFromHand` 从牌库/墓地来的那份 ⇒ 原位 = 队尾））")
r[7] = ("✅ **2026-10-26 批次 9（`E9ER`）核销**：E2P11 指出的两个 `Hand.Add` 调用点已由**批次 6 `G14`** 改成 "
        "`AddCardToHandAtWithinLimit`（行号现读 `:3659/:3670` 与 `:3859/:3873`）。"
        "另核：引擎里剩下的**唯一** `ps.Hand.Add(`（`EffectResolver.cs:3859` 的 `autoPlayFromHand` 第一跳）"
        "语义上就是「队尾」——原版那条协程本身就是 append（`PlayerHand._AddCardNotDrawnToHand_d__39__MoveNext.c:33` 走 `List.Add`，"
        "与带下标的 `…AtIndex` 变体 `:40` 是两支），⇒ 不改。"
        "⚠️ 撤单位回手（`DoReturn` 的 `dest == hand`）走 `AddCardToHandWithinLimit`（append）也**是对的**："
        "棋盘上的单位没有「原手牌下标」，原版回的也是无下标那一支。")
r[10] = (r[10].rstrip() + " ｜ 批次 9 `E9ER`：**已核销（两个调用点都在）**；`Hand.Add` 现读只剩 autoPlay 那一处（= 原版的 append 支）。")
PATCH[k] = r

# ---- ⑤ OnQuestResolved：490 gate 从「阵营代理」升级成原版那一位 ----
k = ("RawCardScript", "OnQuestResolved(BattleManager manager, CardScript thisCard, CardScript actingCard)")
r = list(by_key[k])
r[5] = "A"
r[6] = ("RuleEngine/Core/EffectResolver.cs:13006（QuestPointThreshold）（等价实现 = `QuestPointThreshold`："
        "满阈值 ⇒ 减一个阈值 + `QuestMilestone++` ⇒ 造 1 张隐秘入牌库（随机下标 `Insert`）⇒ `BroadcastSecretCreated`。"
        "**「这张卡有没有 490 能力」的判据** = `CardDef.AbilityTriggerPresence(490)`（`EffectResolver.cs:13062`）"
        "= 原版 `RawCardScript.HasAbilityType(490)`（`HasAbilityType.c:16-38` 扫 `cardAbilities[].trigger`））")
r[7] = ("✅ **2026-10-26 批次 9（`E9ER`）把「490 分支缺失」补上**：改前用「督军阵营 == DarkAngels」当代理判据 ⇒ "
        "「别的阵营以后加 490 能力」这一档表示不出来（真行为差）。现在按原版那一位判："
        "`AbilityTriggerPresence(490)`（v17 卡表 `abilityTriggers` 列，每张卡都落）三档 —— "
        "`1` 造 / `0` 明确不造并出声一次 / `-1` 旧表未知 ⇒ **退回阵营代理 + 出声**。"
        "⚠️ 残留（如实记）：仍**不读** 490 能力自己的 payload（`chosenAbility 270 createCardInDeck` 的卡表），"
        "而是走 `CreatePool.Resolve(…, \"a secret\", 阵营)` —— 对现池 4 张 DA 督军结果相同（同一批 DA 隐秘）。"
        "另：没有新增 `WhenEventKind` 的 490 广播口（现池带 `trigger 490` 的卡**只有**这 4 张督军，"
        "而它们那条能力正是本方法实现的那件结果 ⇒ 没有第二个听众）。")
r[10] = ("L4 2026-10-26：等价链订正（旧落点指向 const TN_OtherCardDrawn = 指错）。行号以现读为准"
         " ｜ 批次 9 `E9ER`：阵营代理 → `AbilityTriggerPresence(490)`（数据已核：DA1/DA2/DA3/DA75 的 "
         "`cards_engine.json` `abilityTriggers` 都含 490；`cards_truth.json` 的 hero 行没有这一列 —— "
         "判据要读 `cards_engine`，别读 `cards_truth`）。")
PATCH[k] = r

# ---- 1262 IsValidTarget：camouflage 那格已收窄（状态仍 B，理由换成「剩下的没逐句对」） ----
k = ("RawCardScript", "IsValidTarget(CardScript thisCard, CardScript targetCard)")
r = list(by_key[k])
r[7] = ("订正旧记「camouflage 与 0x46 两道闸没核清」—— 都查清了：① `0x46` = stealth(70)，已实现于 "
        "`RuleCore.cs:11107-11109`（异侧 ∧ Has(Stealth) 即拒）；② camouflage 在 `EffectResolver.AddSide` 的 stealthGate 支，"
        "🔴 **2026-10-26 批次 9（`E9ER`）已按原版收窄**：只有**施放者是战术卡**（`cardType 20`）时挡"
        "（`AddSide` 的 `tacticCaster` 形参，`IsLegalPick`/`PlayTargetPool` 传那张要打出的牌）—— "
        "与 `IsValidSpellTarget` 那支同口径。剩下没做的是「6 道闸逐句对」这一步（尤其 ② `IsInPlay` 与我们的 "
        "`IsAlive` 在「待死」那一刻的差别）⇒ 保持 B。")
r[10] = ("L4 2026-10-26：两道闸查清（camouflage 那条偏离仍成立 ⇒ 保持 B，等主对话裁「要不要按卡种收窄」）。行号以现读为准"
         " ｜ 批次 9 `E9ER`：**camouflage 已按卡种收窄**（主对话已派活，无需再裁）；状态仍 B 的理由换成「IsInPlay vs IsAlive 待核 + 未逐句对」。")
PATCH[k] = r

out = ["\t".join(COLS)]
for k, r in PATCH.items():
    assert len(r) == 11, (k, len(r))
    r = [c.replace("\r", " ").replace("\n", " ｜ ").replace("\t", " ") for c in r]
    out.append("\t".join(r))
io.open(OUT, "wb").write(("\n".join(out) + "\n").encode("utf-8"))
print("wrote", OUT, len(PATCH), "rows")
