# -*- coding: utf-8 -*-
"""B92_B1 产出生成器：判据 tsv（7 列）+ 账补丁 tsv（11 列）。
⛔ 可复跑、幂等；只读主表、只写两个产物文件。"""
import io, os, sys

BASE = r"d:/4/Unity/资料/完全复刻"
SRC = os.path.join(BASE, "判据", "B92_战斗_待查判据_全量.tsv")
OUT_J = os.path.join(BASE, "判据", "B92_战斗判据_B1.tsv")
OUT_P = os.path.join(BASE, "账", "_parts", "B92_B1.tsv")

def read_tsv(p):
    b = io.open(p, "rb").read()
    t = b.decode("utf-8-sig").replace("\r\n", "\n")
    return [l.split("\t") for l in t.split("\n") if l.strip()]

def write_tsv(p, rows):
    body = "\r\n".join("\t".join(r) for r in rows) + "\r\n"
    data = b"\xef\xbb\xbf" + body.encode("utf-8")
    d = os.path.dirname(p)
    if d and not os.path.isdir(d):
        os.makedirs(d)
    io.open(p, "wb").write(data)

MINE = """AddAttackAction AddCardToDeck AddSummonUnit AllowResolveAttack BroadcastActivateMinions
BroadcastQuestResolved BroadcastSacrificeResolved BroadcastUnitDamaged BroadcastUnitRegiment
BroadcastUnitSynapse CanPlayCard CancelBattleActionFromQueue CancelEffectsFromDisappearingUnit
CancelEffectsFromDisappearingUnitFromQueue CheckUnitsCancellingAttack DealMultiDamageAndDestroySelf
ExecuteNextAction GetDeathType RecordAttackPendingDamage ResolveAction ResolveCardAutoSummoned
ResolveDisappearFromHand ResolveFatigue ResolveFollowAfterResolution ResolveJamUnit ResolveManaBuff
ResolveMinionDeath ResolvePlayActiveAbility ResolveRemoveEffectsFrom ResolveRemoveFromCemetery
ResolveTriggerMob ResolveTriggerRegiment ResolveTriggerRequiem ResolveTriggerSacrifice
SetUnitOrderInPlay ShuffleDeck StartBattlePhase UpdateManaLeftAccumulation""".split()

src = read_tsv(SRC)
hdr, rows = src[0], src[1:]
sel = [r for r in rows if r[1] == "BattleManager" and r[2].split("(")[0] in MINE]
assert len(sel) == 39, len(sel)

# ---- 7 列判据表：(key=方法全签名) -> (原版行为, 判据, 我们现状, 施工落点, 还差什么, 结论档)
J = {}

J["AddAttackAction"] = (
 "SetupAttack + 双方 ClearPendingDamage + RecordAttackPendingDamage(recordReturnDamage=1) + 建 BattleAction(attack=1) + AddAutoActionToQueue",
 "decomp_full/BattleManager__AddAttackAction.c:30/:31/:33/:34/:36/:43/:91",
 "有（等价实现 = RuleCore.DeclareAttack；差异 = 入队 vs 同步，属 E4 族2/族3）",
 "RuleEngine/Core/RuleCore.cs.DeclareAttack",
 "无。只剩结构差（攻击逐段现结、无卡实例待结算缓冲）",
 "可做（已落地）")

J["AddCardToDeck"] = (
 "0<=pos<Count ⇒ List.Insert(pos)，否则追加；随后 reparent 到该侧牌库根 + SetAsDeck(pos) + SetActive(pos==0)",
 "decomp_full/BattleManager__AddCardToDeck.c:29-37/:45-63；同族 BattleCardManager__AddCardToDeck.c:30-50（无 reparent）",
 "无（我们只有 decktop 与 InsertIntoDeckRandom 两个专用口，无通用「按下标插入」）",
 "（仅当判 D 被推翻时）RuleEngine/Core/PlayerState.cs 新增 InsertAt(index, inst)",
 "可达性四源全负：decomp_full 0 调用点 / ILSpy 只有定义 / out_il2csharp 只有定义 / assets_full 全库 json 0 命中",
 "判 D 候选（(c) 四源全负 · 本 build / 2025-07-30 快照 / 安卓 1.35.0 · 出现生产点即回 C）")

J["AddSummonUnit"] = (
 "LogWarning(前缀+summonedCard 名+…+caster 名) + 建 BattleAction(summonUnit=20) 带 summoned/isPlayer/pos/caster/enchantment + 入队",
 "decomp_full/BattleManager__AddSummonUnit.c:45/:46/:95-111（0x14=20 → BattleActionType.cs:21）",
 "有（RuleCore.DeployFree/DeployFreeCore；enchantments 形参 2026-10-28 已补）",
 "RuleEngine/Core/RuleCore.cs.DeployFreeCore",
 "只剩入队 vs 同步；那句 LogWarning 属诊断日志、无意等价",
 "可做（已落地）")

J["AllowResolveAttack"] = (
 "七段闸：scriptedAttack(0x34)/redirectedAttack(0x4a)/blockPlayerActions 三个短路 → 教程闸 → 主段(hasAvailableActions/非stun/IsUnit/攻>0/非150非880/cardState==2或正在攻击) → IsValidAttackTarget → 目标isPlayer则要求 !IsAttacking 且 matchType!=160；失败写 errorMessage(+0x528)",
 "decomp_full/BattleManager__AllowResolveAttack.c:92/:134/:176/:226/:236-248/:333/:440/:561",
 "合并式：RuleCore.CanAttackNow + IsValidTarget（返回 RuleCodes 而非写 errorMessage）。有 stun/150/880/攻>0/合法目标/三个短路 + **hasAvailableActions（现核订正：= ActionQuota + AttacksThisTurn，逐值同构、不是缺口）**；缺 4 条 = cardState==2||IsAttacking、教程闸、⑦（目标 isPlayer ⇒ !IsAttacking && matchType!=160）、errorMessage 回写；多出 pindown/cannotAttack/p==Active（原版在 CardScript.CanAttackNow 里）",
 "RuleEngine/Core/RuleCore.cs.CanAttackNow + RuleCore.DeclareAttack",
 "四条缺口的判据已到行：cardState 属附录 C·①；教程闸口我们没有；⑦ 里的 IsAttacking() 原版指 cardState∈{3,0x11}，我们的 IsAttackingOnBoard 是「在场且活着」⇒ 不是同一个量、⛔ 不许拿它顶；errorMessage 语义（字符串 vs 码）属另一笔",
 "可做")

J["BroadcastActivateMinions"] = (
 "按实参选侧：false ⇒ 敌 MinionManager + 敌督军 ActivateMinion(不点手牌)；true ⇒ 先 playerHand.LightPlayableHand + 我 MinionManager + 我督军",
 "decomp_full/BattleManager__BroadcastActivateMinions.c:9/:12/:18/:20/:23；ActivateMinion 本体 CardScript__ActivateMinion.c:18-42/:43-69",
 "RuleCore.BroadcastActivateMinions：逐卡 RecomputeActAndAttack + 督军单独一跳 + playerTurn==false 不点手牌（对）；手牌高亮在表现层 RefreshHandPlayable（本地近似、不走 CanPlayCard）。**实参座位现核订正：_NextTurn:382-384 传的就是 isPlayerTurn，恒取 ctx.Active 是对的、不是缺口**",
 "RuleEngine/Core/RuleCore.cs.BroadcastActivateMinions",
 "只剩 ② 一处：原版「已行动」时 canAct/canAttack **两位同写 false**，我们只压 Exhausted（刻意收窄、已标）；③ 已坐实 +0xdc = 220 = bloodThirst（DefinedTrait.cs:21）⇒ ActionQuota 与 ActivateMinion.c:19-22 的 +0x48 闸逐值同构",
 "可做")

J["BroadcastQuestResolved"] = (
 "两跳：棋盘 unitsInPlay 逐张 IsInPlay ⇒ CardScript.QuestResolved；双方手牌(GetCardsInHandList 六个 1)逐张 != questCard ⇒ 同上；而 QuestResolved = OnTrigger(0x1EA=490 QuestTriggered)",
 "decomp_full/BattleManager__BroadcastQuestResolved.c:26-31/:34-50/:45；CardScript__QuestResolved.c:6 → RawCardScript__OnQuestResolved.c:6；唯一调用点 CardScript__RemoveEffect.c:254",
 "无这条广播。我们有的是 AddQuestPoints 那条链的另一半（EffectResolver.QuestPointThreshold，只跑督军自己的 490）；EffectResolver.cs:18079 自述「BroadcastQuestResolved + questCounter=0 仍缺」",
 "RuleEngine/Core/EffectResolver.cs（附魔摘除的 0xC/0xF/0x10 档）+ 新增 RuleCore.BroadcastQuestResolved（trigger 名 CardDef 已有 {\"QuestTriggered\",490}）",
 "需先补上游：① 附魔摘除处要判 cardType==0x28 && spellType==0x78 && !fromJam（fromJam 形参已在 RemoveEffectRecord 预留）② 490 的广播出口（FireTriggerAt 按关键词走，490 不是关键词）",
 "需先补上游")

J["BroadcastSacrificeResolved"] = (
 ".c 全文 8 行 = 一行纯转调 BattleManagerSupport.BroadcastUnitSacrifice（Support 版 = 棋盘一跳无 IsInPlay 闸 + 双方手牌，接收端 OnTrigger(225 OtherCardSacrifice)）",
 "decomp_full/BattleManager__BroadcastSacrificeResolved.c:5；CardScript__ReactToUnitUsedSacrifice.c:7",
 "有（RuleCore.FireTriggerAlways(... Sacrifice) + triggers:sacrifice → OtherCardSacrifice(225)；待死那一半 includePendingDeath 已补）",
 "RuleEngine/Core/RuleCore.cs（FireTriggerAlways）+ RuleEngine/Core/EffectResolver.cs（triggers:sacrifice 映射）",
 "唯一残差 = 无卡（Card==null）单位那一档快照差；池内 0 张带 220 的同款 ⇒ 零可观测。🔴 状态与 B92_B5 同步 = A",
 "可做（已落地）→ A")

J["BroadcastUnitDamaged"] = (
 "Support 广播(false) → source 非空则 ChallengeLogMgr.DamageStat(source.isPlayer, damageValue) → 同条件 LogDamage(抬 DamageSignal(damageInfo, 被打者))",
 "decomp_full/BattleManager__BroadcastUnitDamaged.c:13/:19/:24-27/:29；ChallengeLogMgr__LogDamage.c:30-35",
 "① 有（RuleCore.EmitHit → PreCallBoardListener + 广播，棋盘跳排除被打者已由 VA 坐实）② 有（ctx.Tally.AddDamage，RuleCore.cs:8946）③ 无（没有 DamageSignal 这个信号）",
 "RuleEngine/Core/AchievementTally.cs（若要做 ③：RaiseDamageSignal + 消费端归成就/任务线）",
 "需先补上游：DamageSignal 的消费方登记在挑战/成就域、本机未找到订阅表；另原版第三条支（棋盘卡不在场时的 damageType==4 支）我们没有该维度 ⇒ 结构不可观测",
 "需先补上游")

J["BroadcastUnitRegiment"] = (
 ".c 8 行 = 一行纯转调 BattleManagerSupport.BroadcastUnitRegiment（棋盘一跳 + 双方手牌两跳；接收端 OtherCardRegiment=646，关键词侧 Regiment=301）",
 "decomp_full/BattleManager__BroadcastUnitRegiment.c:5",
 "有（RuleCore.FireTriggerAt(... KeywordTable.Regiment) + triggers:regiment → 646；2026-10-25 补的映射）",
 "RuleEngine/Core/RuleCore.cs.FireTriggerAt",
 "① 与 Support 版共享代码段 ⇒ 口径层只能 B（§4.1）② 646 池内 0 张 ⇒ 休眠（⛔ 不判 D）",
 "可做（休眠）")

J["BroadcastUnitSynapse"] = (
 ".c 8 行 = 一行纯转调 BattleManagerSupport.BroadcastUnitSynapse（棋盘一跳 + 双方手牌两跳、无预调；接收端 TriggeredSynapse=630）",
 "decomp_full/BattleManager__BroadcastUnitSynapse.c:5",
 "有（BroadcastKeywordEvent(Triggers(Synapse)) + triggers:synapse → 630），但多一个 SynapseBusy 单层递归守卫（原版无）",
 "RuleEngine/Core/EffectResolver.cs（BroadcastKeywordEvent / SynapseBusy 那一处）",
 "E4 族10 已裁：去掉 SynapseBusy，改 1000 层深度上限防崩（🔶 自己设计，原版会栈溢出），触顶出声；池内 synapse 3 张，「A↔B 互带」可构造性未跑",
 "可做")

J["CanPlayCard"] = (
 "七段闸：initiateAction 且 actionQueue 非空→PleaseWait；教程闸；侧闸(allowPlayerActions/allowEnemyActions/resolvingTurnEnd)；WaitingForBlockingAction；HasEnoughMana；IsAvailableSlot；法术 targeted 要有合法目标（提示键 NotYourTurn/NotEnoughMana/NotEnoughRoom/NoTargetAvailable）",
 "decomp_full/BattleManager__CanPlayCard.c:35-60/:226-255/:270-380；字段名与本地化键 out_cpp2il_dev_ilspy/BattleManager.cs:45193-45320",
 "RuleCore.CanPlayCard(ctx,p,handIdx,slot,showTips,initiateAction,actionAlreadyStarted) 返回码。有 ③ 侧闸（= p != ctx.Active，现核订正：+0x244/+0x246 由 _NextTurn:382-384 每回合写）、费用（先费用后格位，与原版同序）、格位 HasRoomFor、战术卡 CanPlayTactic（含目标存在性）、Cost<0/IsEnchantment 两条自加前置；🆕 ①④ 也接上了（① 阻塞队列 PleaseWait、④ WaitingForBlockingAction）",
 "RuleEngine/Core/RuleCore.cs.CanPlayCard（①④ 已接线）+ RuleEngine/Core/RuleCodes.cs（新开 ErrWaitingBlockingAction=30）+ Editor/RuleEngineTest_Priority.cs（断言 W61T33 六条）",
 "①④ 🆕 B92_B1d 已接线：RuleCodes.ErrWaitingBlockingAction = 30（新开一档、没动已有 30 个）+ CanPlayCard 两行闸 + 断言 W61T33CanPlayCardWaitingBlocking（阻塞在时不可出牌 / 不在时可出牌 / 查询式调用不受 ① 影响 / Replay 不挡）。① 只在 initiateAction:true 生效（原版 AI/查询侧传 false）⇒ 现有调用点行为不变；showTips 气泡未做（表现层）；actionAlreadyStarted 只为签名对齐、体内没读；② 上游 = 教程闸口（CheckIfPlayerActionPermittedInTutorial 我们没有）",
 "可做（①④ 已接线；剩 ② 教程闸）")
J["CancelBattleActionFromQueue"] = (
 "越界 LogError 返回；type==attack(1) ⇒ actingCard(+0x08).CancelAttack()；type==createHandCard(0x17=23) ⇒ 目标对象 SetActive(false)；最后 RemoveAt（其它类型只 RemoveAt）",
 "decomp_full/BattleManager__CancelBattleActionFromQueue.c:28-40/:43-55/:57-77/:80",
 "口已建 ActionQueue.RemoveQueuedActionAt（先补副作用再 RemoveAt、越界 LogError）；两个副作用仍是待接点（走到就 LogWarning + CancelSideEffectsPendingForTest++）。⚠️ 施工判据写 CancelAttack(targetCard) 与 .c 不符，.c 读 +0x08=actingCard",
 "RuleEngine/Core/ActionQueue.cs.ApplyCancelSideEffects + 新建 RuleCore.CancelAttack(UnitState)（状态闸 cardState∈{3,0x11} 的等价物 IsAttackingOnBoard 已在）",
 "需先补上游：QueuedAction.ActionType 回填（B1-05），否则 case 1 / 0x17 结构上走不到；另需表现层开口（0x17 的 SetActive）",
 "需先补上游")

J["CancelEffectsFromDisappearingUnit"] = (
 "四道各扫一遍 FromQueue，实参队列序 = +0x350 → +0x358 → +0x368 → +0x360",
 "decomp_full/BattleManager__CancelEffectsFromDisappearingUnit.c:7-12",
 "有（ActionPump.CancelEffectsFromDisappearingUnit：四道 Player(0)→Instant(1)→Interrupt(2)→DelayedInstant(3)，= 声明序不是消费序）",
 "RuleEngine/Core/ActionPump.cs.CancelEffectsFromDisappearingUnit",
 "只剩调用点：原版四个（RemoveSelfBuffsFromQueues / ResolveAction 的 jam 支 / ResolveJamUnit / ResolveTransformUnit），B91-W2 已接 ResolveJamUnit",
 "可做")

J["CancelEffectsFromDisappearingUnitFromQueue"] = (
 "倒序扫；三条命中（actingCard==jammed / casterCard(+0xD8)==jammed / targetCard 非空则相等）；三处整道结束豁免(0x2d && deathType==0x28；0x1a；0x2d)；命中且不豁免 ⇒ LogWarning + CancelBattleActionFromQueue",
 "decomp_full/BattleManager__CancelEffectsFromDisappearingUnitFromQueue.c:32/:77/:79/:84/:89/:104-105",
 "有（ActionPump 同名方法逐句对齐）；两处如实标：casterCard 落在 QueuedAction.Actor、targetCard 过滤今天几乎全 null",
 "RuleEngine/Core/ActionPump.cs + ActionQueue.QueuedAction.{Target,DeathType}",
 "需先补上游：ActionType / DeathType 恒 0 ⇒ 三处豁免与两个副作用结构上走不到（B1-05 同一根因）",
 "需先补上游")

J["CheckUnitsCancellingAttack"] = (
 "扫全场 unitsInPlay，逐张 HasCancelAttackAbility(那张, acting, target)，排除 trait 0x82(130=jam)，Find 去重，返回 List；调用点 _ResolveAttack:531 在非 redirectedAttack 且表非空时：CancelAttack + 双方 ClearPendingDamage + RegisterPlayerActionCancelled + 对每个命中单位 ResolveCancelAttack(单位, acting, target, 1)",
 "decomp_full/BattleManager__CheckUnitsCancellingAttack.c:38-45/:47/:57-66；调用点 BattleManager._ResolveAttack_d__438__MoveNext.c:531-600",
 "🆕 2026-10-28 B92_B1b 已建**查询口** RuleCore.CheckUnitsCancellingAttack（扫双方棋盘、IsUnitInPlayOrDying 闸、排除 jam、无日志=不动 Fingerprint）+ 私有 HasCancelAttackAbility（附魔用无闸的 FindCancelAttack(UnitState)、自己那半用 FxOps/Effect）。**0 调用点 by design**（接线要先定「替身走哪一条」，见还差什么）。原有：TryCancelAttackByOwnAboutToAttack（攻方自己那条通道）+ CardDef.Bodyguard + TryRedirectAttackToBodyguard（取槽号最小直改目标）",
 "RuleEngine/Core/RuleCore.cs 新增 CheckUnitsCancellingAttack(ctx, attacker, target)，插在 TryCancelAttackByOwnAboutToAttack 之前；替身那条改由候选表的 AboutToAttack 正文产生 redirectedAttack",
 "接线四件缺：① CancelAttack/ClearPendingDamage（我们无待结算缓冲）② RegisterPlayerActionCancelled（联机/回放账层）③ redirectedAttack(74) 的出队体不存在（要 ActionType 回填 + 「改目标重发」的结算口）④ **口径未定：替身已经走 Bodyguard 直改目标那条路，若再接「取消+跑正文」会让同一张卡两条路各走一次** ⇒ 需调度台裁。另：扫的容器不同（原版 unitsInPlay 是双方共用追加序表，我们 P0/P1 按格号）",
 "可做")

J["DealMultiDamageAndDestroySelf"] = (
 "建 BattleAction(doMultiDamageAndDestroySelf=0x4b=75，AddRange 拷目标表)；入队前 casterCard.SetAsUnitDestroyed() ⇒ cardState=5(将死)；入队；最后对每个目标 RecordPendingDamage",
 "decomp_full/BattleManager__DealMultiDamageAndDestroySelf.c:39/:43/:50/:98/:100-110；CardScript__SetAsUnitDestroyed.c:19-31",
 "EffectResolver.DoDeal 的 destroyedSelf 支 + RuleCore.ApplyDamage：先伤害、后自毁；没有「先标将死」",
 "RuleEngine/Core/EffectResolver.cs（DoDeal 的 destroyedSelf 支）",
 "需先补上游：UnitState 无 cardState/将死位（附录 C·①）。事件序差异可观测（原版自毁那一刻施放者已 state=5）；池内「and destroy itself」样本卡仍未定位",
 "需先补上游")

J["ExecuteNextAction"] = (
 "拼调试串；index < log.Count ⇒ CreateRemoteAction(self, log[index], isPlayer) 并 index++；否则 LogWarning(越界)",
 "decomp_full/BattleManager__ExecuteNextAction.c:52/:60/:61/:64",
 "引擎侧没有「按动作日志下标执行」这条路（联机走 NetBattle 报文、回放走 ReplayStore）。CreateRemoteAction 全仓只有一条注释（BattleDriver.cs:37139）；越界出声 2026-10-28 B91_W3 已补在回放链",
 "（若要建）RuleEngine/Core/ActionPump.cs 新增 ExecuteRemoteActionAt(ctx,index,isPlayer)，由 ReplayStore/NetBattle 切过来",
 "需先补上游：CreateRemoteAction 本体未读、无对应物；要不要建这条路是架构决定（联机线已用另一套序列化）",
 "需先补上游")

J["GetDeathType"] = (
 "返回点：来源是法术⇒30 tactic；来源 isPlayer==IsPlayerTurn⇒10 否则 15；damageInfo 起因∈{3,7,11,12}⇒20 ability；actionType==45(destroyUnit)⇒直接取该动作自带 deathType(+0xF0)；其余 0",
 "decomp_full/BattleManager__GetDeathType.c:146/:366/:369/:372/:417/:455",
 "RuleCore.DeathTypeOfSource 只分 20/30（法术来源⇒30、其余⇒20）；10/15 由调用点显式传；④「动作自带 deathType」没有（QueuedAction.DeathType 恒 0）",
 "RuleEngine/Core/RuleCore.cs.DeathTypeOfSource（补 ②③⑤）+ RuleEngine/Core/ActionQueue.cs（DeathType 回填）",
 "① ② 可直接补（我们有 owner 可判侧）② ③ 需要 DamageInfo 的「起因」维度（我们传的是 UnitDeathType 常量，没有那个 {3,7,11,12} 掩码）④ 依赖 B1-05",
 "可做")

J["RecordAttackPendingDamage"] = (
 "登记序 = 星镖(+0x25 标记) → 标记光(+0x27 标记，值取攻方的) → 主伤害(type4) → 反击(recordReturnDamage 且 EnemyReturnsAttack) → 溅射(type6，被打方那侧 MinionManager 的相邻)；星镖/标记光各自若 EnoughPendingDamageToDie 就直接 return（后面全不记）",
 "decomp_full/BattleManager__RecordAttackPendingDamage.c:124-140/:62-79/:79-92/:93-110",
 "没有那个缓冲：结算侧按真实结算序（星镖→主伤害→标记光）逐段现结、星镖打死即跳过；判死口 RuleCore.EnoughPendingDamageToDie 唯一；预览 AttackDamageEntries 只喂 WouldKillByEntries；反击 EnemyReturnsAttack；溅射 blast 在 RuleCore:7040",
 "无需新建缓冲（若只对齐登记序 ⇒ AttackDamageEntries 加 pendingOrder 变体，但用户 2026-10-09 已裁「预览照真实结算序」，⛔ 别改）",
 "① 结构差 List<int> vs 卡实例缓冲（E4 族2 记 B 的前提现读成立：判死口唯一）② 两个 DamageInfo 标记位（+0x25 星镖 / +0x27 标记光）我们无对应字段；出现第二个独立判死读点须改回台账",
 "可做（已落地，结构差）")

J["ResolveAction"] = (
 "91 型 actionType 的巨型 switch（.c 9,300 行）：switch 里出现 76 个 case（0x0b,0x0d-0x27,0x29-0x36,0x37-0x48,0x4a-0x5b，其中 0x34/0x35/0x4a 共用体）；attack(1)/doDamage(8)/doMultiDamage(9) 等 15 型不在本 switch；多型被内联进 case",
 "decomp_full/BattleManager__ResolveAction.c:688（switch）+ 附录 A 全表；出队口 BattleManager__ResolveActionFromQueue.c:34-40",
 "分派点搬到入队时的闭包（QueuedAction.Execute）；四条道与消费序对上；QueuedAction.ActionType 字段已存在（本批前恒 0）",
 "RuleEngine/Core/ActionPump.cs.EnqueueAbility（形参 int actionType 已由 B5 加）+ RuleCore.cs 侧 8 处已回填",
 "映射表已给（附录 A 76 行）。🆕 B92_B1b/B1c 已回填 RuleCore 侧 8/15：虫群合并=79 · 伏击/落地续攻=1 · resetDuty=87 · 回合末到期:限时增益=61 · removeEffect=16 · minionDeath=4 · recall-to-deck=51。剩 7 处保持 0 —— 调度台 2026-10-28 裁定：那是【能力自带 actionType】的结构缺口（原版 PlayAbility 之后由那条能力自己决定、每卡不同），不是缺常量 ⇒ ⛔ 不许编枚举语义（mob-extra 是 ExtraTriggers 派生、不是原版 81）。另：EffectResolver 侧调用点属 B4",
 "可做（RuleCore 侧 8/15 已回填；余 7 处结构性）")
J["ResolveCardAutoSummoned"] = (
 "协程：IsAvailableSlot 闸 → 在手牌则先播打出演出(PlayPlayerCard/PlayEnemyCard) → SetAmbush → GetNextSlotWithoutDisplacing → RemoveCardFromHand → PlayMinionFromHand → MinionPlayedIntoField → GetCopyOfCardEffects → CardPlayedWithEffects → UpdateWhileInPlay → BroadcastUnitSummoned → 收尾",
 "decomp_full/BattleManager._ResolveCardAutoSummoned_d__511__MoveNext.c:83/:89/:110/:128/:169/:192-205/:245/:287-292/:353",
 "RuleCore.DeployFreeCore（落格→部署广播→挂附魔）；表现层那一拍已有落点（BattleDriver.CompletePendingDeploy 的 AutoPlayFromHand → StartAiDeployFlight 三段）",
 "RuleEngine/Core/RuleCore.cs.DeployFreeCore",
 "① 我方 autoPlay 的飞行载体是场上样式视图（要新造手牌形态 CardView + 真机验收）= 表现层 ② GetCopyOfCardEffects+CardPlayedWithEffects 我们落成 Auras.Recompose（等价未逐句证）",
 "可做（引擎侧已落地，残差在表现层）")

J["ResolveDisappearFromHand"] = (
 "协程：IsInHand 为假只 LogWarning 不早退 → 溶解粒子 + DissolveCardInHand 音效 → RemoveCostEffects → DestroyCard(…,1) 协程 → PlayerHand.RemoveCardFromHand → Finish。去向=GoToCemetery 对 trait5 直接 SetActive(false) ⇒ 移出游戏不进弃牌堆",
 "decomp_full/BattleManager._ResolveDisappearFromHand_d__545__MoveNext.c:37/:110-135/:132/:134/:150-155；CardScript__GoToCemetery.c:14-21",
 "有（RuleCore.SweepEphemeral 回合边界一次扫 + RemoveCardFromHandToRemoved 含 RemoveCostEffects 等价；判据链 2026-10-25 X12 已逐跳引全）",
 "RuleEngine/Core/RuleCore.cs.SweepEphemeral",
 "① 结构差：原版逐个入队(动作 56)+状态机，我们回合边界一次扫（E4 族3 已裁）② 表现层缺 DissolveCardInHand 音效 + 溶解粒子（全仓 0 命中）",
 "可做（已落地）")

J["ResolveFollowAfterResolution"] = (
 "actingCard 为空 ⇒ LogError 后仍收尾；否则 Log + CardScript.FollowUpResolutionPlayed(actingCard, <第二个卡实参>) → Finish。而 FollowUpResolutionPlayed = OnTrigger(0x19A=410 FollowUpResolution)",
 "decomp_full/BattleManager__ResolveFollowAfterResolution.c:86-135/:185/:229/:380；CardScript__FollowUpResolutionPlayed.c:5-8",
 "有（EffectResolver.EmitFollowUpResolution 发 410；调用点 EffectResolver.cs:2864/:10407、RuleCore.cs:12829）",
 "RuleEngine/Core/EffectResolver.cs.EmitFollowUpResolution",
 "① 第二个卡实参没带（.c 里是 local_f8、Ghidra 未建模来源 ⇒ 需 VA 反汇编坐实是否 targetCard）；池内 30 张带 410 ⇒ 需核这 30 张有没有读「另一个参与方」② HasTriggerSource 闸 vs 原版无条件 OnTrigger，需一次现读确认不漏「有 410 无动画行」的卡",
 "可做")

J["ResolveJamUnit"] = (
 "闸(IsInPlayOrDying) → ReactToUnitJammed → BroadcastUnitJammed → CancelEffectsFromDisappearingUnit(self, jammed, null, 0) → PlayerHand.UpdateCardEffects(我方) → PlayerHand.UpdateCardEffects(敌方) → Finish",
 "decomp_full/BattleManager__ResolveJamUnit.c:28/:167/:215/:263/:265/:267",
 "有（EffectResolver.ResolveJamUnit：闸→ApplyJam→按来源撤 extrinsic→致死 CleanupDeaths→BroadcastWhen(UnitJammed)→CancelEffectsFromDisappearingUnit→Auras.RefreshInPlayNumericAndKeywords）",
 "RuleEngine/Core/EffectResolver.cs.ResolveJamUnit",
 "需先补上游：① 三档豁免读 QueuedAction.ActionType（B1-05）② 两侧 PlayerHand.UpdateCardEffects 的全量重算口不存在（附录 C·③）③ 池内 jam 生产者 0 张 ⇒ 休眠",
 "需先补上游")

J["ResolveManaBuff"] = (
 "两个独立 if：friendlyPlayer ⇒ ProcessManaBuff(…, 回调=FinishResolvingAction)；enemyPlayer ⇒ ProcessManaBuff(…)。分档：spiritStone / faith / questPoints / createMana / refillMana；正 amount 次数循环、负 amount ⇒ DestroyManaSlot 或 UseMana(1)；末尾调回调",
 "decomp_full/BattleManager__ResolveManaBuff.c:140-160/:100-140；out_cpp2il_dev_ilspy/BattleManager.cs:34298-34375 + :34376-34510（ProcessManaBuff 五档 + 正负两循环）",
 "EffectResolver.DoFactionResource（按 kind 字符串分派）",
 "RuleEngine/Core/EffectResolver.cs.DoFactionResource",
 "① 逐档对表未做（5 档 + 负 amount 的两个动作 + untilEndOfTurn；friendlyPlayer/enemyPlayer 可同时为真 ⇒ 原版跑两遍，我们是否两遍未核）② 两个 Signal（CollectSpiritStone/CollectFaith）我们无 ③ BroadcastCollectFaith/BroadcastCollectQuestPoints 两跳落点",
 "可做")

J["ResolveMinionDeath"] = (
 "闸 cardState==5(将死) → MinionManager.RemoveMinion → unitsInPlay.Remove → AddReassembleMinionsOrder(显式重排动作) → ChallengeLogMgr.TroopKilled → 仅 activeSelf 时起 CardScript.UnitDeath 演出 → 双方 PlayerHand.UpdateCardEffects → Finish",
 "decomp_full/BattleManager._ResolveMinionDeath_d__454__MoveNext.c:38-45/:54-56/:58-60/:61/:113/:115-125/:145/:147/:149",
 "RuleCore.CleanupDeaths 离场段 + MinionDeathBookkeepingDeferred：以 Health<=0 为闸、棋盘 RemoveAt 隐式补位、Tally.AddTroopKilled（2026-10-28 B88·A2）",
 "RuleEngine/Core/RuleCore.cs.MinionDeathBookkeepingDeferred + CleanupDeaths",
 "需先补上游：① cardState==5 闸（附录 C·①）② AddReassembleMinionsOrder 显式重排动作（我们隐式补位，同批多离场的次序可观测差异）③ 双方 UpdateCardEffects 全量口（附录 C·③）④ UnitDeath 演出属表现层",
 "需先补上游")

J["ResolvePlayActiveAbility"] = (
 "协程：ReadyToUseActiveAbility → 失败 CancelActiveAbility/CancelMatchWithVictory → 选目标(ChooseCardMethod/NeedsToChooseFromPool) → IsValidActiveAbilityTarget → 失败 CancelActiveAbility + RegisterPlayerActionCancelled → FinishActiveAbility → CommunicateBattleAction → EnemyTargetingAnim → ResolveActiveAbilityPlayed → UsedActiveAbility → StopDisplayingActiveAbility → FinishActiveAbility → FinishResolvingAction",
 "decomp_full/BattleManager._ResolvePlayActiveAbility_d__479__MoveNext.c:161/:181/:240/:367/:382/:405/:456/:459/:516/:525/:583/:586/:591/:596/:651",
 "RuleCore.UseAbilityCore：闸→记攻击型→RecomputeAttackTypeAfterActiveAbility→疲劳/配额→EmitBloodThirst→UsedActiveAbility 两位→发 EvtKind.Ability→ResolveEffect。ResolveActiveAbilityPlayed 只落了攻击型重挑那一半",
 "RuleEngine/Core/RuleCore.cs.UseAbilityCore",
 "需先补上游：① ResolveActiveAbilityPlayed 整段（含誓约「再来 N 次」那一支；我们放在 UseAlternative 的 oath 路，需确认 Ability: 这一路不需要）② UsedActiveAbility 与 FinishActiveAbility 原版是两拍，我们合并；③ StopDisplayingActiveAbility/EnemyTargetingAnim 属表现层",
 "需先补上游")

J["ResolveRemoveEffectsFrom"] = (
 ".c 全文 9 行 = 两句：BattleManagerSupport.BroadcastRemoveEffectsFrom(self, actingCard, 0) → FinishResolvingAction(0x2f=47)；真正的「摘全部效果」在 Support 的三趟里（棋盘 IsInPlay 全员 + 当前方手牌 + 另一方手牌）",
 "decomp_full/BattleManager__ResolveRemoveEffectsFrom.c:5-6",
 "有但作用面不同：UnitState.RevertGrantsFrom(source) + Enchantment.RemoveByCard（按来源撤），原版是一次全场 extrinsic 刷新",
 "RuleEngine/Core/UnitState.cs + RuleEngine/Core/Enchantment.cs",
 "需先补上游：零副作用的 extrinsic 全量重算口（Auras.Recompose 有副作用、⛔ 不能挂到动作边界）",
 "需先补上游")

J["ResolveRemoveFromCemetery"] = (
 "卡字段非空 ⇒ CardScript.RemoveCardFromCemetery(那张)，为空 ⇒ LogWarning，然后 Finish；而 CardScript.RemoveCardFromCemetery 的全部实体只有一句：this+0x24b = 0（清「正在从墓地取出」重入闸）",
 "decomp_full/BattleManager__ResolveRemoveFromCemetery.c:82-134/:182/:229；CardScript__RemoveCardFromCemetery.c:5（全文一句）",
 "没有这个状态位（RuleCore.GetCardsInCemetery 的 doc ⑦ 逐字写着「+0x24b 我们无对应物：同步结算、无取出途中中间态」）",
 "无（若将来引入异步取出窗口：RuleCore.GetCardsInCemetery + 取出完成处）",
 "可达性：AddRemoveFromCemetery 产出点确实存在（AbilityLogic.PlayAbility / _ResolveDrawCard / _ResolveRecallToDeck / _ResolveRecallToHand，动作 0x3c=60）⇒ 不是死代码；但全部可观测效果只是清一个我们架构里不存在的闸位",
 "判 D 候选（(d) 架构性不适用 🔶 机制不同 · 本 build / 2025-07-30 快照 / 安卓 1.35.0 · 出现异步取出窗口即回 C）")

J["ResolveTriggerMob"] = (
 "闸(IsInPlayOrDying) → RawCardScript.OnTrigger(300 Mob) 自己 → BroadcastUnitMob(别人，接收端 OtherCardMob=645) → Finish；param_3 区分动作 80/81",
 "decomp_full/BattleManager__ResolveTriggerMob.c:30/:79/:218/:306；CardScript__TriggerOtherCardMob.c:14",
 "有（RuleCore.FireTriggerAt(... Mob) + triggers:mob → OtherCardMob(645)，2026-10-25 已补映射）；位置不同：我们挂 DeclareAttack 同步触发",
 "RuleEngine/Core/RuleCore.cs（DeclareAttack 的 Mob 段）/ EffectResolver.TriggerNameOfWhenKind",
 "① 旧账「监听侧名字映射缺」已作废 ② 「再触发一次」原版是动作 81，我们用 ExtraTriggers 额度 + ExtraTriggerDepth 重入守卫（机制不同语义等价）③ 结构差同步 vs 队列",
 "可做")

J["ResolveTriggerRegiment"] = (
 "闸 → Log → CardScript.TriggerRegiment()（OnTrigger(301) + BroadcastUnitRegiment）→ Finish；动作号 88",
 "decomp_full/BattleManager__ResolveTriggerRegiment.c:80/:129/:226/:273",
 "有原语（RuleCore.FireTriggerAt(... Regiment)）；原账记 A 的调用点 RuleCore.cs:6926 已核实是攻击驱动那一跳（属 CardScript.ResolveUnitAttacked）⇒ 已删、退 B",
 "RuleEngine/Core/RuleCore.cs.FireTriggerAt",
 "① 「能力入队那一跳」（动作 88 的生产者）我们没有 ② 结构差同步 vs 队列",
 "可做")

J["ResolveTriggerRequiem"] = (
 "闸 → OnTrigger(0x1AE=430 Requiem) 自己 → BroadcastUnitRequiem(别人，OtherCardRequiem=435、接收方自排除) → Finish",
 "decomp_full/BattleManager__ResolveTriggerRequiem.c:30/:79/:218/:305；CardScript__TriggerOtherCardRequiem.c:14",
 "有（RuleCore.ResolveTriggerRequiem：闸 → FireTriggerAlways(430) → BroadcastUnitRequiem(435)），2026-10-26 R4-F1 建、逐段已对",
 "RuleEngine/Core/RuleCore.cs.ResolveTriggerRequiem",
 "池内 requiem/OtherCardRequiem = 0 张 ⇒ 休眠（⛔ 不判 D）；动作 64 的生产者我们没有",
 "可做（休眠）")

J["ResolveTriggerSacrifice"] = (
 "targetCard 为空 ⇒ NRE 桩；!IsInPlayOrDying ⇒ 收尾早退；否则 CardScript.TriggerSacrifice()（OnTrigger(220) 自己 + BroadcastUnitUsedSacrifice 别人 ⇒ 225）→ Finish；动作号 63",
 "decomp_full/BattleManager__ResolveTriggerSacrifice.c:24/:30/:128/:175",
 "有 220 那一跳（RuleCore.FireTriggerAlways(... Sacrifice)，在 CheckIfDead 幸存者支）；225 OtherCardSacrifice 只有名字映射（2026-10-26 已补），广播体调用点仍缺",
 "RuleEngine/Core/EffectResolver.cs（BroadcastWhen 调用点）+ RuleEngine/Core/WhenEvent.cs（kind 常量）",
 "需先补上游：225 广播体的调用点；池内 220/225 = 0 张 ⇒ 休眠；动作 63 的生产者我们没有",
 "需先补上游")

J["ResolveFatigue"] = (
 "协程：取该侧督军(GetHero(isPlayer)) → 建一条 DealMultiDamage 打过去（疲劳走统一的伤害路 ⇒ 盾/闪避/无敌那一族挡伤逻辑对疲劳照样生效）",
 "decomp_full/BattleManager._ResolveFatigue_d__587__MoveNext.c:130-141（GetHero → DealMultiDamage）；kickoff BattleManager__ResolveFatigue.c（22 行）",
 "RuleCore.MoveDeckTopToHand 疲劳支 → RuleCore.ApplyFatigueDamage（同步、且不走 ApplyDamage）；B91-W1b·2 已按用户裁定补齐挡伤三族(dodge 0xf0/invuln 0x136/shield 0x50)、护甲 max(1, 疲劳−Armor)、易伤那一份、CheckIfDead 之后才消耗盾/闪避（invuln 不消耗）",
 "RuleEngine/Core/RuleCore.cs.ApplyFatigueDamage",
 "① 结构差：原版一条队列动作 → ResolveMultiDamage，我们同步 ② 我们不走 ApplyDamage ⇒ 堡垒/空投舱两支持久不适用督军（原版由 HasCurrentTrait 触发、督军身上不会出现）⇒ 净零 ③ 池内带 dodge/shield/invuln 的督军 0 张 ⇒ 本轮零可观察",
 "可做（已落地，只剩结构差）")

J["SetUnitOrderInPlay"] = (
 "读 self+0x3a8（ObscuredInt）当前值 → 写 card+0x44(orderInPlay) → 计数器 +1 回写 → BattleCardManager.RegisterCardInPlay(card)；三件事",
 "decomp_full/BattleManager__SetUnitOrderInPlay.c:36-42/:43/:44-62/:63-66",
 "有（BattleContext.TakePlayOrder + UnitState.PlayOrder）；差：序号落在对局级而非卡字段、RegisterCardInPlay 那一层没有、ObscuredInt 我们是 int",
 "RuleEngine/Core/BattleContext.cs.TakePlayOrder（登记那一步若需要 = CardPresentation）",
 "PlayOrder 不在 B2A 口径的可观察量清单里（play/ai 两个探针都不打印）⇒ 需调度台加列才能验收；+0x3a8 五 dword 里哪几个是 fakeValue/inited 未核（只影响反作弊）",
 "可做")

J["ShuffleDeck_LIST"] = (
 "空表 LogError 并原样返回；否则 ShuffleCardList(deck, matchData, checkStratagem) 出新表 + 表现三跳（首张 SetActive(true)+SetupInDeck，其余隐藏）；算法 = 收 _stratagem 下标进 pin 表 → GetRandomOrder → 按序重建",
 "decomp_full/BattleManager__ShuffleDeck.c:29-32/:49-51/:52-73；SupportMethods__ShuffleCardList.c:26-42/:44-88",
 "有（RuleCore.Shuffle → ShuffleByRandomOrder = GetRandomOrder 逐句；随机源 ctx.GetRandomInt = MatchData.GetRandomInt 逐句）；残差 = stratagem 钉位支未实现",
 "RuleEngine/Core/RuleCore.cs.ShuffleByRandomOrder（钉位表作可选形参）",
 "残差零可观察：_stratagem 在当前 build 1,351 张卡 SO 里一条都没有（cards_raw.json 现扫 0 命中），且 RawCardScript.stratagem 属性是常量假（RawCardScript.cs:235）；⚠️ 旧账「只有轮抽用」不成立（CreatePlayerDeck 就传 checkStratagem:true）",
 "可做（钉位支零可观察）")

J["ShuffleDeck_BOOL"] = (
 "三行转发：选该侧牌库（this+0x310 我方 / +0x318 敌方）→ 调 list 重载",
 "out_cpp2il_dev_ilspy/BattleManager.cs:48657-48671（decomp_full 只有 list 版一份文件）",
 "有（RuleCore.Shuffle(ctx, seat) 选侧在同一处）",
 "RuleEngine/Core/RuleCore.cs.Shuffle",
 "只剩表现层末尾两跳（首张 SetActive + SetupInDeck），已在 CardView 一侧有落点；引擎侧无需补",
 "可做（已落地）")

J["StartBattlePhase"] = (
 "协程 13 段：Replay(160) 档 → Boss/Skulls/CampaignBoss 三段演出等待 → 开场语音 → battleStage=50 → ObscuredBool+0x290=1 → +0x1f0 SetActive(false) → AddGoesSecondCardToDeck → 初始附魔两趟(matchData+200 逐项 TriggerOtherCardAbility+AddPlayDropSupplyAction；+208 逐项) → 双方督军武器卡(+0x148)进手 → 非教程放开按钮 → !ActionsInQueue 则 SwitchShade+SetAllCardsInHandsStart+NextTurn",
 "decomp_full/BattleManager._StartBattlePhase_d__334__MoveNext.c:39-60/:66-83/:141-160/:180-200/:213-247/:250-300",
 "RuleCore.BeginTurnCore + StartBattlePhaseHops；B91-W1b 已补武器卡进手（无 +0x148 列 ⇒ 出声不挂）与 ActionsInQueue 让行；SetupInitialEnchantments 已建门（matchType∈{50,80} 恒假）",
 "RuleEngine/Core/RuleCore.cs.BeginTurnCore / StartBattlePhaseHops / SetupInitialEnchantments",
 "需先补上游：① 关卡数据（+0x148 武器卡列、初始附魔两列 matchData+200/+208）② 开局演出（PlayBossAnim/PlaySkullsAnim/PlayCampaignBossAnim/SwitchShade/开场语音）属表现层",
 "需先补上游")

J["UpdateManaLeftAccumulation"] = (
 "manaAccumulation(+0x40)==0 ⇒ 整条早退不写；否则 SetSavedMana(min(manaAccumulation, GetUnusedMana()), useNumberChange:1)；调用点 _NextTurn:291 且带 turnCounter>1 闸",
 "decomp_full/BattleManager__UpdateManaLeftAccumulation.c:37-39/:51-57；out_cpp2il_dev_ilspy/BattleManager.cs:20851-20867；调用闸 BattleManager._NextTurn_d__395__MoveNext.c:288-292",
 "✅ 2026-10-28 B92_B1b **已按原版补齐两处**（RuleCore.EndTurnCore 那一句）：① 写 min(cap, GetUnusedMana())（= p.Energy）② 加 ctx.Turn > 1 闸（ctx.Turn ≡ turnCounter，见 RuleCore:1212 那行注释）⇒ 第 1 回合结束不再结转",
 "RuleEngine/Core/RuleCore.cs.EndTurnCore（那一句 p.SetSavedMana(...)）",
 "无（两处已改完）。行为影响只在 GameplayVariables.manaAccumulation > 0 的档（= 遭遇 Skirmish）：第 1 回合结束不再结转 1 点；经典档 manaAccumulation = 0 ⇒ 逐字节不变",
 "可做")

# ---- 组 key：把两个 ShuffleDeck 重载单独命名
def key_of(row):
    name = row[2].split("(")[0]
    if name == "ShuffleDeck":
        return "ShuffleDeck_LIST" if row[2].count(",") >= 2 else "ShuffleDeck_BOOL"
    return name

jrows = []
missing = []
for r in sel:
    k = key_of(r)
    if k not in J:
        missing.append(k); continue
    beh, ev, st, land, gap, verdict = J[k]
    jrows.append([r[1] + "." + r[2], beh, ev, st, land, gap, verdict])
assert not missing, missing
assert len(jrows) == 39, len(jrows)

write_tsv(OUT_J, [["账行(原版类.方法)", "原版行为一句话", "判据(文件:行号 或 资产路径+字段)",
                   "我们现状(有/无/差什么)", "施工落点(文件.方法)", "还差什么", "结论档"]] + jrows)
print("wrote", OUT_J, len(jrows) + 1, "lines")

# ---- 账补丁（11 列）：只写我改过 状态/落点/差异 的行
PATCH = {}
PATCH["AddCardToDeck"] = ("B",
  "RuleEngine/Core/RuleCore.cs:149（NewBattle）（等价实现 = 建库这一跳：我们建库时直接 Deck.Add、装载口按「末尾=顶」倒序喂，没有「插到指定下标」这个通用口）",
  "原版 `AddCardToDeck(card, deck, posInDeck)` = `0<=pos<Count ⇒ Insert(pos)`，越界追加；随后 reparent 到该侧牌库根、`SetAsDeck(pos)`、`SetActive(pos==0)`（`BattleManager__AddCardToDeck.c:29-37/:45-63`）。**可达性四源全负**（decomp_full 0 调用点 · ILSpy 只有定义 · out_il2csharp 只有定义 · assets_full 全库 json 0 命中 `AddCardToDeck`）⇒ 判 D 候选 (c)，**状态暂保持 B 待调度台确认**。",
  "【B92_B1 2026-10-28】四源负证据补齐；D 候选需的时间限定 = 本 build / 2025-07-30 快照 / 安卓 1.35.0，可逆备注 = 出现生产点即回 C 并补 InsertAt 通用口。")
PATCH["UpdateManaLeftAccumulation"] = ("B",
  "RuleEngine/Core/PlayerState.cs:147（ManaCarry）+ RuleEngine/Core/RuleCore.cs.EndTurnCore 里那一句 `p.SetSavedMana(...)`",
  "原版 = `manaAccumulation(+0x40)==0` 整条早退，否则 `SetSavedMana(min(manaAccumulation, GetUnusedMana()), useNumberChange:1)`（`.c:37-39/:51-57`），**且只在 `turnCounter(+0x3f8) > 1` 时被调用**（`_NextTurn_d__395__MoveNext.c:288-292`）。我们算式逐项等价，**残差两处**：① 缺 `min(cap, 剩余)`（今天 cap 唯一非零 = 1 ⇒ 数值恒等）② 缺 `turn > 1` 闸（遭遇档第 1 回合结束会多带 1 点）。旧账「规则书没写死 ⇒ 取证缺口」**已作废**。",
  "【B92_B1 2026-10-28】铁律 5 订正：行为基准改回 `.c`；残差收敛成两处一行级修正（判据已给全）。")
PATCH["BroadcastSacrificeResolved"] = ("A",
  "RuleEngine/Core/RuleCore.cs（FireTriggerAlways，在 CheckIfDead 的幸存者那一支）· RuleEngine/Core/EffectResolver.cs（triggers:sacrifice → OtherCardSacrifice(225)）",
  "`.c` 全文 8 行 = 一行纯转调 `BattleManagerSupport.BroadcastUnitSacrifice`（`:5`）。落点真实存在，行为同形。**残差（唯一）**：无卡（`Card == null`）单位那一档我们的棋盘快照会跳过、原版照调；池内 0 张带 220 的同款 ⇒ 今天零可观察。",
  "【B92_B1 2026-10-28 · 与 `B92_B5` 同步】B5 已把 `BattleManagerSupport.BroadcastSacrificeResolved` 判 A；本行（`BattleManager.`侧，体 = 一行纯转调）判据不比它深 ⇒ **跟它同步 A**（铁律 6：同一方法两处状态必须一致）。")
PATCH["BroadcastQuestResolved"] = ("B",
  "（无落点：原版是「附魔摘除时给全场每张在场卡 + 双方手牌各跑一次 490」的广播，我们没这条）",
  "原版两跳 = 棋盘 `unitsInPlay` 逐张 `IsInPlay ⇒ CardScript.QuestResolved`；`GetCardsInHandList(1,1,1,1,1,1,0)`（双方手牌、全卡种）逐张 `!= questCard` ⇒ 同上（`BroadcastQuestResolved.c:26-31/:34-50`）。而 `QuestResolved` = `OnTrigger(0x1EA=490)`（`CardScript__QuestResolved.c:6`），唯一调用点 = `CardScript__RemoveEffect.c:254`（附魔档 `0xC/0xF/0x10` 且 `cardType==0x28 && spellType==0x78 && !fromJam`）。**旧落点 `EffectResolver.QuestPointThreshold` 是错配** —— 那是 `AddQuestPoints` 那条链、只跑督军自己的 490（`EffectResolver.cs:18079` 自己就写着「BroadcastQuestResolved + questCounter=0 今天仍缺」）。",
  "【B92_B1 2026-10-28】落点已改指；需先补上游 = 附魔摘除的 cardType/spellType 闸 + 490 的广播出口（FireTriggerAt 按关键词走、490 不是关键词）。")
PATCH["CheckUnitsCancellingAttack"] = ("B",
  "RuleEngine/Core/RuleCore.cs（新增 CheckUnitsCancellingAttack；现有等价物 = TryCancelAttackByOwnAboutToAttack + CardDef.Bodyguard/TryRedirectAttackToBodyguard）",
  "原版 = 扫**全场** `unitsInPlay` 逐张 `HasCancelAttackAbility(那张, acting, target)`、排除 `trait 0x82(130 = jam)`、`Find` 去重，返回 List（`.c:38-45/:47/:57-66`）。调用点 `_ResolveAttack…:531`：非 `redirectedAttack(0x4a)` 且表非空 ⇒ ① `CancelAttack` ② `actingCard.ClearPendingDamage` ③ `targetCard.ClearPendingDamage` ④ `RegisterPlayerActionCancelled` ⑤ 对表里**每个**单位 `ResolveCancelAttack(单位, acting, target, 1)`（替身那条正文才是 `AddRedirectedAttack`）。我们只有「攻方自己」那条通道 + Bodyguard 直改目标（取槽号最小）。",
  "【B92_B1 2026-10-28】旧账「每个命中单位各排一条 redirectedAttack、终态由队列序定」半对：还漏了 CancelAttack + 双方清待结算 + RegisterPlayerActionCancelled 四句。依赖 QueuedAction.ActionType 回填 redirectedAttack(74)。")
PATCH["ResolveRemoveFromCemetery"] = ("B",
  "（无落点：我们同步结算、没有「正在从墓地取出」这个中间态）",
  "原版 `ResolveRemoveFromCemetery` = 卡字段非空则调 `CardScript.RemoveCardFromCemetery(那张)`、为空则 `LogWarning`，然后 `FinishResolvingAction`（`.c:82-134/:182/:229`）；而 **`CardScript.RemoveCardFromCemetery` 的全部实体只有一句 `this + 0x24b = 0`**（`CardScript__RemoveCardFromCemetery.c:5`，清「正在从墓地取出」的重入闸）。⇒ 判 D 候选 **(d) 架构性不适用**（🔶 机制不同：我们同步结算、该闸位不存在；`RuleCore.GetCardsInCemetery` 的 ⑦ 已逐字记同一结论）。旧账把它当 `RemoveFromSource` 的对齐候选是**错配**。",
  "【B92_B1 2026-10-28】可达性成立（`AddRemoveFromCemetery` 有 4 个产出点，动作 0x3c=60）⇒ 不是死代码，但可观测效果为零。时间限定 = 本 build / 2025-07-30 快照 / 安卓 1.35.0；可逆备注 = 引入异步取出窗口即回 C。**状态暂保持 B 待调度台确认。**")
PATCH["ResolveTriggerMob"] = ("B",
  "RuleEngine/Core/RuleCore.cs:12496 FireTriggerAt(Mob)",
  "原版 = 闸(`IsInPlayOrDying`) → `OnTrigger(300 Mob)` 自己 → `BroadcastUnitMob` 别人（接收端 `OtherCardMob=645`）（`.c:30/:79/:218/:306`）。**旧账「监听侧名字映射缺」已作废** —— `EffectResolver.TriggerNameOfWhenKind` 的 `case KeywordTable.Mob: return \"OtherCardMob\"` 2026-10-25 已补。剩：原版「再触发一次」是**动作 81**，我们用 `UnitState.ExtraTriggers` 额度 + `ctx.ExtraTriggerDepth`（机制不同、语义等价）；结构差同步 vs 队列。",
  "【B92_B1 2026-10-28】铁律 5 订正「名字映射缺」。")
PATCH["ResolveFollowAfterResolution"] = ("B",
  "RuleEngine/Core/EffectResolver.cs:7919（EmitFollowUpResolution）",
  "原版 = `CardScript.FollowUpResolutionPlayed(actingCard, <第二个卡>)` → `OnTrigger(0x19A=410)`（`CardScript__FollowUpResolutionPlayed.c:5-8`）；actingCard 为空时 `LogError` 后仍收尾。我们发 410 但**不带第二个卡参与方**（`.c` 里是 `local_f8`、Ghidra 未建模它的来源 ⇒ 需 VA 反汇编坐实是否 `targetCard`）；池内 **30 张**卡带 410 ⇒ 要核这 30 张有没有读「另一个参与方」。另有 `HasTriggerSource` 闸 vs 原版无条件 `OnTrigger`，需一次现读确认不漏「有 410 无动画行」的卡。",
  "【B92_B1 2026-10-28】差异列本轮补齐（旧账为空）。")
PATCH["ResolveAction"] = ("B",
  "RuleEngine/Core/ActionPump.cs:1436（PumpActionQueues）· RuleEngine/Core/ActionQueue.cs:292（TryDequeue）",
  "原版 = 91 型 actionType 巨型 switch（`.c` 9,300 行，**switch 里 76 个 case**：0x0b/0x0d-0x27/0x29-0x36/0x37-0x48/0x4a-0x5b，其中 0x34/0x35/0x4a 共用体；`attack(1)`/`doDamage(8)`/`doMultiDamage(9)` 等 15 型不在本 switch）。我们把分派点搬到入队闭包；`QueuedAction.ActionType` **字段已存在但恒 0**（无入队点写）。**映射表已产出**（`判据/B92_战斗判据_B1.md` 附录 A：76 行 = 型/枚举名/case 行/主调用）。",
  "【B92_B1 2026-10-28】施工单已齐 = 附录 A + `EnqueueAbility` 加一个 int actionType 形参 + 66 处 `.Enqueue(` 回填（跨 ActionPump/EffectResolver/RuleCore 三文件 ⇒ 按文件切块、一人一文件）。")
PATCH["BroadcastActivateMinions"] = ("B",
  "RuleEngine/Core/RuleCore.cs:16403（BroadcastActivateMinions）· :3518（RecomputeActAndAttack）",
  "原版按**实参**选侧（`false` ⇒ 敌 MM + 敌督军，**不点手牌**；`true` ⇒ 先 `playerHand.LightPlayableHand` + 我 MM + 我督军，`.c:9/:12/:18/:20/:23`）。我们恒取 `ctx.Active`（实参被忽略），逐卡 `RecomputeActAndAttack` + 督军一跳。另：原版「已行动」时 `canAct` 与 `canAttack` 两位同写 false（`ActivateMinion.c:18-42`），我们只压 `Exhausted`（刻意收窄、已标）；手牌高亮在表现层 `BattleDriver.RefreshHandPlayable`，且它用**本地近似**不走 `CanPlayCard`（见报告 §6）。",
  "【B92_B1 2026-10-28】实参座位 = 一行修正。")

ph = ["域", "原版类", "方法", "判据出处", "判据要点", "状态", "我们的落点", "差异", "等级", "轮次", "备注"]
prows = []
for r in sel:
    base = r[2].split("(")[0]
    if base in PATCH:
        st, land, diff, note = PATCH[base]
        prows.append([r[0], r[1], r[2], r[3], r[4], st, land, diff, r[8], r[9], note])
assert len(prows) == len(PATCH), (len(prows), len(PATCH))
write_tsv(OUT_P, [ph] + prows)
print("wrote", OUT_P, len(prows) + 1, "lines")
