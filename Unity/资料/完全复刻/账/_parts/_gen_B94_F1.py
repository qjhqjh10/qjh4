# -*- coding: utf-8 -*-
"""B94_F1 的账补丁生成器（只读派活表 + 只读主表；只写 _parts/B94_F1.tsv）。

为什么用生成器而不是手写 TSV：
  · `方法` 列必须与主表**逐字一致**（裸名/全角括号都会被 merge 脚本拒）⇒ 直接抄派活表里那一格；
  · `等级` 列在主表里有 P0/P1/P2 三档（派活表里没有这一列）⇒ 必须回主表取，⛔ 不能写空（会丢数据）；
  · 顺手做「读→写→再读」逐列往返校验（本仓踩过 csv 转义污染账的坑）。

用法（无参数就是干跑 + 写盘）：
    python -I -X utf8 d:/4/Unity/资料/完全复刻/账/_parts/_gen_B94_F1.py
"""
import io
import os
import sys

DISPATCH = r"d:/4/Unity/资料/完全复刻/判据/B94_派活.tsv"
LEDGER = r"d:/4/Unity/资料/完全复刻/账"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B94_F1.tsv"
GROUP = "F1_RuleCore"
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]
NCOL = len(COLS)


def split_rows(path):
    b = io.open(path, "rb").read()
    assert b.count(b"\r") - b.count(b"\r\n") == 0, "bare CR in " + path
    text = b.decode("utf-8-sig")
    return [l.split("\t") for l in text.split("\n") if l.strip()]


# ---------------------------------------------------------------- overrides
# key = 原版类 + "|" + 方法（方法串逐字取自派活表）
# val = dict(状态=..., 判据出处=..., 判据要点=..., 差异=..., 备注=...)
OVR = {}


def add(cls, method, **kw):
    k = cls + "|" + method
    assert k not in OVR, "dup key " + k
    OVR[k] = kw


add("PlayerManager", "ActivateHero(bool isPlayerTurn)",
    状态="A",
    判据出处="decomp_full/PlayerManager__ActivateHero.c:1-12（全文现读）· 被调体 decomp_full/CardScript__ActivateMinion.c:14-82",
    判据要点="全文一句转调：`CardScript__ActivateMinion(本方督军卡 +0x28, isPlayerTurn)`，无其它语句。`ActivateMinion` 本体：闸 = 本方回合 ∧ `cardState(+0x228)==2` ∧ (`attacksThisTurn(+0x48)==0` ∨ (`trait 0xdc` ∧ `==1`)) ∧ `!stun(100)` ∧ `!displaySummonSickness`；把结果**一起**写 `canAct(+0x230)` / `canAttack(+0x23C)`；末尾 `ferocity(0x4f1)`/`oath(0x4fb)` 第二写点**只重写 `canAct` = `!stun`**（忽略召唤病），并在召唤病 ∧ `attackType(+0x120)!=4` 时把攻击型改 4（+表现副作用）。",
    差异="（等价）原版在每动作末（`FinishResolvingAction`）刷督军那两位；我们：① 回合起点 `BroadcastActivateMinions` 对 `ps.Warlord` 调 `RecomputeActAndAttack`（`RuleCore.cs:3531`，= 同一份 `ActivateMinion` 体：两位一起写、ferocity/oath 第二写点同式）② 每次改词（`AddKeyword`/`RemoveAll`）也调它 ③ 出手/用技能的口**各自现算**（`CanAttackNow` 的 `AttacksThisTurn >= ActionQuota`、`IsStunned`）⇒ 存位的两位**只服务表现层**，引擎侧无可观测差异（5 条结构差已在 `RecomputeActAndAttack` 的 doc 里逐条记着）。",
    备注="2026-10-29 `B94_F1` 亲读 `.c` 复核。休眠面：督军 `actedOut` 只在它真出手后为真。")

add("BattleManager", "BroadcastActivateMinions(bool playerTurn)",
    状态="A",
    判据出处="decomp_full/BattleManager__BroadcastActivateMinions.c:1-33（全文现读）· 被调体 decomp_full/MinionManager__BroadcastActivateMinion.c:2-47",
    判据要点="两支：`playerTurn==0` ⇒ 敌方 `MinionManager.BroadcastActivateMinion(0)`(:9) + **敌方督军** `ActivateMinion(0)`(:12)；`!=0` ⇒ 先 `PlayerHand.LightPlayableHand()`(:18) → 我方 MM `BroadcastActivateMinion`(:20) → **我方督军** `ActivateMinion`(:23)。**不是事件广播**。",
    差异="（等价）按 `ctx.Active` 取侧（原版侧别由 `MinionManager` 实例定，实参只是它的判据）；两排逐格 `RecomputeActAndAttack`（跳过督军格）+ 督军单独一次；`LightPlayableHand` = 表现层（我们的可打性在 `CanPlayCard` 取用时现算）。",
    备注="2026-10-29 `B94_F1` 现读全文；调用点 = `BeginTurnCore:2262`（`ResolveAtTurn(\"turn_start\")` 之后、`BroadcastTurnStarted` 之前，次序 = `_NextTurn_d__395:371→:384→:385`）。")

add("BattleManager", "GetMulliganCard(bool isPlayer)",
    状态="A",
    判据出处="decomp_full/BattleManager__GetMulliganCard.c:1-45（全文现读）· out_il2csharp/Assembly-CSharp/BattleManager.cs:3388-3406（干净体）· 两个调用点 PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:93 / PlayerHand._FinishMulligan_d__52__MoveNext.c:79",
    判据要点="🔴**就地订正本行旧判据**（旧文写「取那张要换的卡」= 误读）：从**预先洗好的牌库**取顶上那张 —— `isPlayer ? playerShuffledDeck(+0x310) : enemyShuffledDeck(+0x318)`；`Count>1` 先 `list[1].gameObject.SetActive(true)`（预览下一张），再 `list[0].SetActive(true)` + `RemoveAt(0)` 并返回该卡；`Count<1` ⇒ `LogError(\"Not enough cards to Mulligan for player {isPlayer}\")` + 返回 null。它是 mulligan 阶段的**抽牌原语**（`AddCardsToMulligan` 发整份换牌牌、`_FinishMulligan` **每弃一张补抽一张**，两处都调它）。",
    差异="（等价）我们的等价物 = `NewBattle` 的起手发牌 + `RuleCore.Draw`（取牌库顶；`Deck` 镜像约定「末尾=顶」）⇒ 抽出的**张数与分布**相同。⚠️ `out_il2csharp` 那段 `RemoveAt(0); return list[0];` 是反编译**返回错位**（`.c` 是先取出再 `RemoveAt`）—— 以 `.c` 为准。",
    备注="2026-10-29 `B94_F1`：旧判据要点「取那张要换的卡」**已就地订正**（铁律 5）—— 弃回牌库那一半在 `ReplaceMulliganCard`。")

add("BattleManager", "ResolveFatigue(bool playerDrawing)",
    状态="A",
    判据出处="decomp_full/BattleManager._ResolveFatigue_d__587__MoveNext.c:88-150（真体，现读；`BattleManager__ResolveFatigue.c` 只是协程 kickoff）",
    判据要点="自增疲劳计数（`+0x378..0x388` 的 ObscuredInt，`op_Increment`）→ 用**新值**弹一条本地化错误条（`UIMessageController.ShowError`）→ 造 `DamageInfo(抽牌方督军, 计数, damageType 2, -1)` → `BattleManager.DealMultiDamage(bm, dmg, [督军], priority 2, checkIfCasterInPlay 0, caster null, blockPlayerActions 0)`。即：疲劳伤害**走常规伤害管线**、打**抽牌那一方**的督军。",
    差异="（等价）`MoveDeckTopToHand` 疲劳支：`ps.Fatigue++` → `EmitHit`（= `EvtKind.Hit` ＋ `damaged` 广播，**排在扣血之前** = 原版 `.c:141` 的 `BroadcastUnitDamaged`）→ `ApplyFatigueDamage(:16561)`（逐句复刻 `_ReceiveDamage`：挡伤三族 dodge/invuln/shield 整段挡 + 护甲 `max(1,x-Armor)` + 易伤独立一发 + dodge/shield 被消耗）→ `NoteWarlordHealthForSkulls` → `CheckWinner`。**不**走 `ApplyDamage`（那会把堡垒/空投舱/伏击拉进来，督军身上不出现）；**不**记伤害统计（原版那笔只在 `BroadcastUnitDamaged` 的「有来源」支，疲劳 caster=null ⇒ 原版也不记）。",
    备注="2026-10-29 `B94_F1` 现读 `.c:88-150`；「盾族参与」是 2026-10-28 `W1` 按用户裁定（以 `.c` 为准）补齐的。")

add("BattleManager", "ShuffleDeck(List<CardScript> deckToShuffle, bool checkStratagem)",
    状态="A",
    判据出处="decomp_full/SupportMethods__ShuffleCardList.c + decomp_full/SupportMethods__GetRandomOrder.c:24-96（逐行现读）",
    判据要点="原版把洗牌委托给 `SupportMethods.ShuffleCardList(deck, matchData, checkStratagem)`；排序算法 = `GetRandomOrder`：逐个位置 `GetRandomInt(0, n-i)` 取「未用表里第 r 个空位」，`stratagemList` 前缀**钉位**且钉位占掉抽签面，末位不掷。",
    差异="（等价）我们的 `Shuffle(:1192)` → `ShuffleByRandomOrder` → `RuleCorePartial_B93G1.cs` 的 `ShuffleByRandomOrderCore`（逐句照 `GetRandomOrder.c:24-96`；`stratagemList` 传 null = 池内 `_stratagem` 0 张）。⚠️ 旧实现是 Fisher–Yates（分布同、**同一条随机流给出的排列不同**），已换掉。",
    备注="2026-10-29 `B94_F1` 复核（算法本体 2026-10-28 `B93_G1` 已落）。")

add("BattleManager", "ShuffleDeck(bool isPlayer)",
    状态="A",
    判据出处="decomp_full/BattleManager__ShuffleDeck.c:1-74（全文现读）",
    判据要点="空表 ⇒ `LogError`；否则 `SupportMethods.ShuffleCardList(该方牌库, matchData, checkStratagem)`；末尾把**首张** `SetActive(true)` + `CardScript.SetupInDeck`、其余 `SetActive(false)`（「看得见的只有顶上那张」）。",
    差异="（等价）洗牌那半 = 上一条同一个 `Shuffle`；`SetActive`/`SetupInDeck` 属表现层。⚠️ 如实记：原版「空表 ⇒ LogError」这一句**未复刻**（我们的 `Shuffle` 对 0/1 元素是安全 no-op、也不掷随机数，净效果一致；要逐字出声需另立一条低优先账，⛔ 且 `Shuffle` 受「零净行数变化」约束）。",
    备注="2026-10-29 `B94_F1` 现读全文。")

add("BattleManager", "UpdateManaLeftAccumulation(bool isPlayer)",
    状态="A",
    判据出处="decomp_full/BattleManager__UpdateManaLeftAccumulation.c:1-62（全文现读）",
    判据要点="`GameStaticData` 的 `manaAccumulation(+0xa8 → +0x40)` 为 0 ⇒ **整条早退**（不写）；否则取该方 `PlayerManager`，写 `SetSavedMana(min(manaAccumulation, GetUnusedMana()))`。",
    差异="（等价）`RuleCore.cs:2766`（`EndTurnCore` 尾）：`p.SetSavedMana(ctx.Turn > 0 && manaAccumulation > 0 && p.Energy > 0 ? min(p.Energy, manaAccumulation) : 0)` —— `GetUnusedMana` 的等价物 = `p.Energy`；`+0x40 == 0` 那档写 0（`BeginTurnCore` 先清 0 ⇒ 净效果同）；「只在自增后 `turnCounter>1` 时才被调用」⇒ 等价闸 `ctx.Turn > 0`（2026-10-28 `B93_FX1` 已订正此前的差一档）。",
    备注="2026-10-29 `B94_F1` 现读 `.c` 全文并与 `:2766` 逐项对表。")

add("CardScript", "CheckStun()",
    状态="A",
    判据出处="decomp_full/CardScript__CheckStun.c:2-23（全文现读）",
    判据要点="`HasCurrentTrait(100 stun) ∧ HasCurrentTrait(0xfa=250 unstunnable)` ⇒ ① `RemoveTrait(100)` ② `BattleCardUI.RemoveStatusAnim(100)` ③ `RemoveActiveEffect(100)`。两道闸读的都是**当前** trait。",
    差异="（等价）`RuleCore.CheckStun(:16623)`：谓词同式、副作用 ①③ 都落（② 是表现层，我们没有 UI 层、如实留缺口），另把「收记录」显式写成倒序 `ExpireTraitEffect` + `RemoveAll` 两步。两个调用点（`ResolveTransformUnit` / 残骸翻回）在 `EffectResolver.cs`（非本代理白名单）。池内 `unstunnable(250)` = 0 张 ⇒ 今天恒不触发（休眠件，铁律 11 照建）。",
    备注="2026-10-29 `B94_F1` 现读全文；接线状态 = 口已建、调用点在 `EffectResolver.cs`。")

add("CardScript", "TransformIntoRemnant()",
    状态="A",
    判据出处="decomp_full/CardScript__TransformIntoRemnant.c:1-84（全文现读）· 配套 decomp_full/SupportMethods__IsAeldariRemnant.c:1-14",
    判据要点="四步：① `*(u16*)(+0x65) = 1`（= `isRemnant` 置 1 **同时**把 `isLeavingRemnant(+0x66)` 清 0）② `GameStaticData.remnantMaxHealth(+0x298)` 写进 **maxHealth(+0xa4..0xb4)** 与 **currentHealth(+0x68..0x78)** 各 5 个 ObscuredInt 字 ③ **倒序**遍历 `CurrentTraits(+0x128)` 逐条 `RemoveTraitSilently`（每条一条 debug log）④ `BattleCardUI.ChangeCardToRemnant(ui,1,0,0)`。",
    差异="（等价）`CleanupDeaths(:9857-9876)`：新造 `UnitState{IsRemnant=true, Health=1, MaxHealthBase=1, Exhausted=true, PlayOrder 沿用}` ⇒ ①②（`remnantMaxHealth` 全池 = 1）＋ ③ `ClearAllKeywords()`（= 倒序 `RemoveTraitSilently` 的净效果：静默摘、顺序无可观测差）＋ ④ 表现层另链。⚠️ ③ 的「逐条 debug log」未复刻（调试噪声，不属行为）。",
    备注="2026-10-29 `B94_F1` 现读 `.c` 全文。翻面条件按 `SupportMethods__HasToTransformIntoRemnant.c`（`Remnant`/`Waystone` 词），阵营那一跳在 `IsNecronRemnant`（见 `:9436` 注释）。")

add("MinionManager", "ActivateMinions(bool isPlayerTurn)",
    状态="A",
    判据出处="decomp_full/MinionManager__ActivateMinions.c:1-49（全文现读）· 被调体 decomp_full/CardScript__ActivateMinion.c:1-140",
    判据要点="左表 `leftMinions(+0x60)` → 右表 `rightMinions(+0x68)` 逐张 `CardScript.ActivateMinion(isPlayerTurn)`，**末尾** `RefreshOccupationSlots()`。",
    差异="（等价）`BroadcastActivateMinions` 的棋盘那一圈（逐格 `RecomputeActAndAttack`、跳过督军格）；`RefreshOccupationSlots` = 表现层（`BattleDriver.TickAutoZoom`，`CardPresentation/Battle/BattleDriver.cs:19403`，正主在 `CombatAutoZoom.cs`）。",
    备注="2026-10-29 `B94_F1` 现读全文；与 `MinionManager.BroadcastActivateMinion` 是同一份体的两个入口。")

add("MinionManager", "BroadcastActivateMinion(bool playerTurn)",
    状态="A",
    判据出处="decomp_full/MinionManager__BroadcastActivateMinion.c:2-47（全文 48 行现读）· 被调体 decomp_full/CardScript__ActivateMinion.c:14-82",
    判据要点="本侧两排（`+0x60` / `+0x68`）逐张 `card.ActivateMinion(playerTurn)`；被调体重算 `canAct(+0x230)`/`canAttack(+0x23C)`。**不是事件广播**（旧账那句「广播 OnMinionAddedOrRemoved」是误读，已订正）。",
    差异="（等价）同一个 `RecomputeActAndAttack`；两排 = 我们的棋盘格（跳过督军格）。",
    备注="2026-10-29 `B94_F1` 现读全文。")

add("MinionManager", "RemoveSwarm(CardScript exceptionCard)",
    状态="A",
    判据出处="decomp_full/MinionManager__RemoveSwarm.c:1-83（全文现读）+ decomp_full/CardScript__RemoveSwarm.c:1-21（全文现读）",
    判据要点="两侧在场表（`+0x60`/`+0x68`）逐张：`CardScript.IsInPlay` ∧ `!= exceptionCard` ⇒ `CardScript.RemoveSwarm(卡)`；而 `CardScript.RemoveSwarm` **全文只有一句** `SwarmIconController.Toggle(false)`（`ui +0x318` 非空才调）⇒ **纯表现层**，不摘单位、不动属性。",
    差异="（引擎侧无可复刻物）原版这一整条方法只关虫群图标；我们的 `TrySwarmMerge(:4568)` 是**引擎侧合并**（另一条链），虫群图标开关在视图侧 ⇒ 引擎账上「无水可倒」，判等价而非缺口。",
    备注="2026-10-29 `B94_F1` 现读两份 `.c` 全文；⛔ 别把「没有引擎副作用」误记为「没做」。")

add("PlayerHand", "CheckIfMulliganCardsCorrect(int[] mulliganCardsToDiscardId)",
    状态="A",
    判据出处="decomp_full/PlayerHand__CheckIfMulliganCardsCorrect.c:20-80（现读）",
    判据要点="外层逐条走请求 id、内层按 `uniqueInGameID(+0x22c)` 扫**整份**起始手牌；**一条找不到 ⇒ `LogError(\"Unable to find card discarded in mulligan \"+id)` + `return false`**（整批不动）。不做去重检查。",
    差异="（等价）`MulliganCollectIndices(:14226)` + `Mulligan(:444)` 的**整批拒绝**（`bad.Count>0` ⇒ `Debug.LogError` + `ctx.Log` + 返回 -1）；重复下标只算一次（= 原版按 id 扫的天然幂等）。",
    备注="2026-10-29 `B94_F1` 现读 `.c`；2026-10-26 批次 4 `F1` 已把「逐条静默跳过」升级成「整批拒绝」。")

add("PlayerHand", "CompleteMulliganPhase()",
    状态="A",
    判据出处="decomp_full/PlayerHand__CompleteMulliganPhase.c:1-101（全文现读）",
    判据要点="把 `currentMulligan(+0x58)` 的每张移进 `currentHand(+0x50)`（`Add` 语义 + `+0x1c++`），逐张 `SetStateInHand` → `ToggleOverrideSorting(true)` → `SetSortingLayer` → `FinishMulliganUi` → `ToggleTooltips(false)`；之后**重建一个空的** `currentMulligan` 并 `StartCoroutine(_FinishMulliganFinalPhase)`。**洗牌与 `StartBattlePhase` 不在这个方法里**。",
    差异="（等价）`EndMulligan(:507)` 是「`CompleteMulliganPhase` + `StartBattlePhase`」合并后的入口（doc 已写明）：关标志 + 发后手防御卡 + `StartBattlePhaseHops`；洗牌与补抽在 `Mulligan(:444)`。表现五步（排序层/FinishMulliganUi/Tooltip）在 `CardPresentation/**`。",
    备注="2026-10-29 `B94_F1` 现读全文；原版 `_FinishMulliganFinalPhase` 的次序（FinishMulligan 双方 → ShuffleDeck 双方 → CompleteMulliganPhase 双方 → StartBattlePhase）已记在 `:433-435`。")

add("PlayerHand", "GetMulliganCardsToDiscard()",
    状态="A",
    判据出处="decomp_full/PlayerHand__GetMulliganCardsToDiscard.c:1-41（全文现读）",
    判据要点="新建 `List<CardScript>`，扫 `currentMulligan(+0x58)`，收 `cardState(+0x228) == 0xe(14 = inMulliganDiscarded)` 的那些。**全树 0 调用点**（真跑的是兄弟 `GetMulliganCardsToDiscardIds`）。",
    差异="（等价 / 无消费者）我们「要弃哪几张」= `MulliganCollectIndices(:14226)` 收出的下标表，`Mulligan(:444)` 直接用；原版这个 List 版没有调用点 ⇒ 落点就是同一份名单。",
    备注="2026-10-29 `B94_F1` 现读全文；旧判据那句「全树 0 调用点」已复核成立。")

add("PlayerHand", "GetMulliganCardsToDiscardIds()",
    状态="A",
    判据出处="decomp_full/PlayerHand__GetMulliganCardsToDiscardIds.c:19-60（全文现读）",
    判据要点="同上筛子（`state == 0xe`），把每张的 `id(+0x22c)` 收成 `int[]`（`new int[count]` 逐格写）= 联机时把「我换了哪几张」发给对端。",
    差异="（等价）我们的联机载荷传**手牌下标**（`Mulligan(handIndices)`），原版传**卡 id**；两端都是同一份 `MulliganCollectIndices` 名单、语义同源。⚠️ 如实记：**线格式不同**（下标 vs id）—— 只有要与原版协议互通时才需要换成 id。",
    备注="2026-10-29 `B94_F1` 现读全文。")

add("PlayerHand", "ReplaceMulliganCard(CardScript cardsToDiscard)",
    状态="A",
    判据出处="decomp_full/PlayerHand__ReplaceMulliganCard.c:1-70（全文现读）· 配对 PlayerHand._FinishMulligan_d__52__MoveNext.c:79",
    判据要点="取 `BattleManager.GetDeck(isPlayer)`；把这张挂到该方牌库锚点（`isPlayer ? manager+0x108 : manager+0xf0`）→ **追加**进牌库列表（`+0x1c++` + `Add`/`AddWithResize`）→ `ToggleOverrideSorting(true)` + `FinishMulliganUi`。⇒ 只做「**弃回牌库**」，**不在这里补抽**。",
    差异="（等价）`Mulligan(:479-483)`：倒序 `ps.Deck.Add(ps.Hand[i])` + `RemoveAt` ⇒ 一次性弃回、再洗、再补抽同样张数。原版是**逐张**「弃回 + `GetMulliganCard` 补抽」再统一 `ShuffleDeck` 双方 ⇒ **净张数与分布相同**，随机数消耗也相同（都是那一次洗牌）。",
    备注="2026-10-29 `B94_F1` 现读全文；⚠️ 与 `GetMulliganCard` 配对读才看得出「逐张补抽」这一层。")

add("PlayerHand", "SetMulliganCardsToDiscard(int[] mulliganCardsToDiscardId)",
    状态="A",
    判据出处="decomp_full/PlayerHand__SetMulliganCardsToDiscard.c:16-40（现读）",
    判据要点="`ids == null` ⇒ false；先 `CheckIfMulliganCardsCorrect(ids)`，false ⇒ `LogError` + false（整批不动）；通过后对 `currentMulligan` **每一张**调 `CardScript.SetStateAsMulligan(卡, ids.Contains(卡.id(+0x22c)))`（= **打标记**，不搬牌）。",
    差异="（等价）`Mulligan(handIndices)` 把「校验 + 打标记 + 搬牌」三步合一（同步引擎没有「先标记、等 Done 再搬」那段 UI 停顿）；「标记」这一层在我们这里没有存位（也不需要）。",
    备注="2026-10-29 `B94_F1` 现读 `.c`。")

add("PlayerHand", "SetMulliganCardsToDiscardStart(int[] mulliganCardsToDiscardId)",
    状态="A",
    判据出处="decomp_full/PlayerHand__SetMulliganCardsToDiscardStart.c:16-60（现读）",
    判据要点="同一道整批校验（false ⇒ `LogError` + false）；通过后对命中 id 的每一张调 `CardScript.ClickMulliganSelected(卡)`（走点击那条 UI 路，含「30 档不许换」那道点击闸）。",
    差异="（等价）`Mulligan(:444)` 的整批校验与整批拒绝**已落**（2026-10-26 批次 4 `F1`）；`ClickMulliganSelected` 的点击 UI 在 `CardPresentation/**`；「30 档不许换」我们保留在批量入口（`MulliganCollectIndices` 的 doc 已记这条结构性对齐）。",
    备注="2026-10-29 `B94_F1` 现读 `.c`；⚠️ 旧判据要点「缺这一道整批校验」**已过期**（校验在 `:463` 的 `MulliganCollectIndices`）。")

# ---- 仍是 B，但本批查出新东西（差额/结构），更新差异列 ----
add("ChallengeLogMgr", "TroopKilled(BattleAction battleAction)",
    状态="B",
    判据出处="decomp_full/ChallengeLogMgr__TroopKilled.c（尾段 `stats+0x10 += 1`）· out_cpp2il_dev_ilspy/TroopsKilledCount.cs:22-27 · 唯一调用点 decomp_full/BattleManager._ResolveMinionDeath_d__454__MoveNext.c:112",
    判据要点="`enemyTroopsDied++` 按**死者**选边写进**凶手那侧**的 `PlayerBattleStats(+0x10)`；消费者 `TroopsKilledCount` 只看 `killer.isPlayer` ⇒ 等价于「记给凶手座位」。本批现读调用点 = `_ResolveMinionDeath` 里**无条件一行**（每个小兵死亡一次），且方法内**只**排除「凶手卡为 null」这一档（`if (local_f8 != 0)`）。",
    差异="🔴 **本批新查出的真实差额**：我们把 `ctx.Tally.AddTroopKilled(kOwner)` 挂在 `TryCreditKillCore(:6195)` **里**，那里有 6 道闸（凶手≠死者 / 凶手活着 / 凶手在场 / 凶手=`ctx.Active` / 死者非督军 / 死者非残骸）—— 原版只有「凶手卡非空」一道 ⇒ **我们少计三档**：① 凶手不是当前行动方（反击/哨戒在对方回合打死人）② 凶手已死（同归于尽）③ 被摧毁的是残骸。",
    备注="**施工单**：把 `AddTroopKilled` 挪到「小兵真死」那一刻（`CleanupDeaths` 的 `leftPlay` 支），闸只留 `killer∈{0,1} ∧ !victim.IsWarlord`；⛔ 必须先按「零净行数」写法改（`RuleCore.cs` 下游 365 处账落点）。2026-10-29 `B94_F1` 现读 `.c` 与调用点。")

add("BattleManager", "GetDeathType(BattleAction battleAction, DamageInfo damageInfo)",
    状态="B",
    判据出处="decomp_full/BattleManager__GetDeathType.c:133-146 / :249-255（现读）· 签名 il2cpp_out/dump.cs:31800",
    判据要点="（原版 `BattleManager.GetDeathType`：先看伤害来源卡的 `cardType`，`IsSpellType`（Tactic 20 / Whispers 40）⇒ `tactic(30)`；否则按 `damageType` 分档。）",
    差异="（部分等价）`DeathTypeOfSource(:9481)` 只落「来源卡是 spell(`tactic`/`defence`/`whispers`) ⇒ `tactic(30)`，否则 `ability(20)`」这一档；原版还按 `damageType` 分出 `combatAttacker(10)` / `combatDefender(11)` 等档（`:133` 起）⇒ **只有能发 `UnitDeathByAbility(180)` 的那两档做了**，其余死因档未建。",
    备注="2026-10-29 `B94_F1` 现读；要补先把 `:133-260` 的 damageType 表逐档抄全（⛔ 别按印象补 combat 档）。")

add("RawCardScript", "GetValidTargets(BattleManager manager, CardScript thisCard)",
    状态="B",
    判据出处="decomp_full/RawCardScript__GetValidTargets.c:1-51（全文现读）",
    判据要点="`thisCard[0x50]`（= 目标筛选器）为空 ⇒ `LogError` + 返回 null；否则遍历 `manager+0x470`（= **双方全部在场卡**）逐候选调虚方法 `+0x318`(=IsValidTarget)，命中就 Add（= 枚举出整表给 UI 高亮）。",
    差异="（结构不同）我们**没有**「枚举整表」这个入口：`IsValidTarget(:5392)` 是单点判定，枚举散在四个调用方（`HasAnyValidAttackTarget(:1481)` / `ResolveBerzerkAttacks(:1508)` / `DeclareAttackCore(:6285)` / 续攻复检）。⇒ 单点判据同源，但「谁能被当目标」的**表**没有公共口。",
    备注="2026-10-29 `B94_F1` 现读全文；补法 = 类尾加一个只读枚举口、让四个调用方转调（独立小件）。")

add("BattleManagerSupport", "BroadcastTurnStarted(BattleManager manager, bool isPlayerTurn)",
    状态="B",
    判据出处="decomp_full/BattleManagerSupport__BroadcastTurnStarted.c:1-62（三跳现读）· 接收端 decomp_full/CardScript__OnTurnStarted.c:1-8（只发 0xa0=160）",
    判据要点="三跳：棋盘（`IsInPlay`）+ 当前回合方手牌 + 对手手牌，逐张 `OnTurnStarted()`；**无预调、0 条 `op_Inequality`**；接收端 `OnTrigger(0xa0 = 160)`。",
    差异="（部分等价）①（棋盘）已落 = `BroadcastTurnStarted(:14804)`；②③（双方手牌）由 `BroadcastTurnStartedToHands` 覆盖，但**只出声、不结算**（手牌没有 `abilityTriggers` 执行口，见那个方法的 doc）。池内 `abilityTriggers` 含 `TurnStarted` = 0 张 ⇒ 今天零可观测。",
    备注="2026-10-29 `B94_F1` 现读 `.c` + 现读 `BeginTurnCore:2262` 的三跳接线；缺口 = 手牌 trigger 执行口（跨文件，属效果层）。")


# ---- 调度台 2026-10-29 补发的两条（**不在**派活表里，来自只读代理 `R-B1` 的判据）----
# key = (账文件名, 账行号)；val 与 OVR 同形，但可以覆盖「我们的落点」
EXTRA = {}


def addx(fn, n, cls, method, **kw):
    EXTRA[(fn, n)] = dict(kw, 原版类=cls, 方法=method)


addx("战斗逻辑驱动.tsv", 455, "BattleManager", "PayCardPlayedCostOath(CardScript card)",
     状态="B",
     判据出处="decomp_full/BattleManager__PayCardPlayedCostOath.c:1-31（全文现读 2026-10-29）· 生产端 decomp_full/AbilityLogic__PlayAbility.c:1996-2050（chosenAbility 0x168=360）· 池内可达性 = 判据/B94_R-B1_无落点_战斗.md §〇·3",
     判据要点="`card != null` ∧ `HasCurrentTrait(0x4fb = oath)` ⇒ 按 `card+0x40(isPlayer)` 取我方/敌方 `PlayerManager` → `amount = GetCurrentTraitValue(oath)` → `PlayerManager.UseMana(amount)`（Normal）→ `manaLeft = BoardAnalysis.GetManaLeft` → `CheckIfCodexTriggered(card, manaLeft, null, **shouldCheckCost: 0**)`。**这一路不发 `EnergySpentSignal`**（`.c` 全树没有 `EnergySpentSignal___ctor`）。",
     我们的落点="`RuleEngine/Core/RuleCore.cs` 类尾 **`PayCardPlayedCostOath(ctx, p, inst)`**（2026-10-29 `B94_F1` 新建）；调用点 = `PlayCardCore` 的 `ConsumeOnceCostMods(...)` 那一行（**0 净行数**接法：同一行追加 `if (card.Has(KeywordTable.Oath)) PayCardPlayedCostOath(...)`）⇒ `SpendNormalMana(..., energySpentSignal: false, costType: 0)` + `CheckCodex(..., shouldCheckCost: false)`",
     差异="**部分落地**：RuleCore 这一侧已落（扣誓约点数 + 按 `shouldCheckCost:0` 查 Codex），**但只覆盖【单位卡】** —— 战术卡在 `PlayCardCore` 开头就分流去 `PlayTactic`（`EffectResolver.cs:2524`，**不是本文件**）⇒ 战术卡的誓约付费要在 `PlayTacticCore` 里再补一处（交 F2）。**仍未落的另外两半**：① `PlayAbility` 的 `chosenAbility 360` op 分派（`EffectResolver.cs`·F2）② `BattleContext.CardChosenInSelection(+0x2E8)` 前置槽（F4，`CountEval.cs:434` 现读自己没有这一格）。⚠️ 另如实记：我们的 Codex 那一下比原版早半步（还在上场之前 ⇒ 带 `codex` 的牌自己查不到自己），改它要动行号。",
     备注="池内 **chosenAbility 360 = 18 张、其中带 `oath` 10 张**（`cards_truth.json` 单份口径，`B94_R-B1` §〇·3）⇒ **可达**。铁律 11：另外两半由 F2/F4 各自落，别在本文件重复实现。 | 2026-10-29 `B94_F1`",
     )

addx("战斗逻辑驱动.tsv", 534, "BattleManager", "ResolveRemoveFromCemetery(BattleAction battleAction)",
     状态="B",
     判据出处="decomp_full/BattleManager__ResolveRemoveFromCemetery.c:82-134/:182/:229（233 行）· decomp_full/CardScript__RemoveCardFromCemetery.c:1-8（全文一句）· decomp_full/CardScript__Awake.c:40 · decomp_full/BattleManager__GetCardsInCemetery.c:54 · 判据/B94_R-B1_无落点_战斗.md §〇·1",
     判据要点="卡字段非空 ⇒ `CardScript.RemoveCardFromCemetery(那张)`、为空 ⇒ `LogWarning`，末 `FinishResolvingAction`；而 `RemoveCardFromCemetery` **全文只有一句** `*(card+0x24b) = 0`。🔴 **那个 `+0x24b` 是 `shouldCheckIfIsInCemetery` = 永久放逐闩**（`Awake.c:40` 置 1、全库无写回 1），`GetCardsInCemetery.c:54` 只在非 0 时才把牌收进结果 ⇒ **放逐后的牌从所有墓地查询里消失**（消费方 = `AbilityLogic__GetTargets.c:951` 的 `ta=60`）。**副作用不是零**。",
     我们的落点="`RuleEngine/Core/RuleCore.cs` 类尾 **`ExileFromCemetery(ctx, inst)` / `IsExiledFromCemetery(ctx, inst)`**（2026-10-29 `B94_F1` 新建；按 `CardInstance.Id` 记）＋ 读点 = 同文件 `GetCardsInCemetery` 的 ⑥/⑦ 那一句（**同一行**追加 `|| IsExiledFromCemetery(ctx, inst)`）",
     差异="**读点 + 放逐 API 已落**；**调用点未落** —— 原版由 `ResolveAction.c:4913`（`case 0x3c` 体内联）触发的入口在我们这儿是 `BattleContext.AddRemoveFromCemetery`（**不是本文件**，`R-B1` 已另发 F4）。生产端唯一 = `PlayAbility.c:1986` 的 chosenAbility `0x15e=350`，**池内 0 张 ⇒ 休眠**（铁律 11 照建、⛔ 不判 D）。",
     备注="🔴 **2026-10-29 铁律 5 就地订正**：旧备注「可观测效果为零 / D 候选 (d)」**作废** —— `+0x24b` 清 0 后 `GetCardsInCemetery` 会永久跳过这张牌（`B94_R-B1` §〇·1 三条硬证）；`PlayerState.cs:306-309` 的同句由 F4 改。断言 = `RuleEngineTest_EngineB94F1.cs` 的 `B94F1T1CemeteryExile`。",
     )


def main():
    disp = split_rows(DISPATCH)
    h = disp[0]
    assert h[0] == "写手组" and len(h) == 11, "派活表表头变了: " + str(h)
    mine = [r for r in disp[1:] if r[0] == GROUP and len(r) == 11]
    by_key = {}
    for r in mine:
        by_key[r[5] + "|" + r[6]] = r
    print("派活行数 =", len(mine), " 覆盖 =", len(OVR))
    missing = [k for k in OVR if k not in by_key]
    assert not missing, "派活表里找不到: " + str(missing)

    # 主表缓存
    cache = {}

    def main_row(fn, n):
        if fn not in cache:
            cache[fn] = split_rows(os.path.join(LEDGER, fn))
        rows = cache[fn]
        assert len(rows) > n - 1, "行号越界 " + fn
        r = rows[n - 1]
        assert len(r) == NCOL, "主表列数 != 11: %s:%d" % (fn, n)
        return r

    out = ["\t".join(COLS)]
    for key, ov in OVR.items():
        d = by_key[key]           # 写手组 落点文件 账文件 账行号 域 原版类 方法 状态 判据出处 判据要点 落点整列
        fn, n = d[2], int(d[3])
        m = main_row(fn, n)
        assert m[1] == d[5] and m[2] == d[6], "派活行号对不上主表: %s:%d (%s|%s) vs 派活(%s|%s)" % (
            fn, n, m[1], m[2], d[5], d[6])
        round_ = m[9]
        if "B94" not in round_:
            round_ = (round_ + "｜B94_F1") if round_ else "B94_F1"
        row = [
            m[0],                              # 域（用主表自己的）
            m[1],                              # 原版类
            m[2],                              # 方法（逐字）
            ov.get("判据出处", m[3]),
            ov.get("判据要点", m[4]),
            ov["状态"],
            m[6],                              # 我们的落点（不改）
            ov.get("差异", m[7]),
            m[8],                              # 等级（回主表取，⛔ 别丢）
            round_,
            ov.get("备注", m[10]),
        ]
        assert len(row) == NCOL
        for c in row:
            assert "\t" not in c and "\n" not in c and "\r" not in c, "列里混进控制符: " + c[:60]
        out.append("\t".join(row))

    # ---- 调度台补发的两条（不在派活表里）----
    for (fn, n), ov in sorted(EXTRA.items()):
        m = main_row(fn, n)
        assert m[1] == ov["原版类"] and m[2] == ov["方法"], "补发行号对不上主表: %s:%d (%s|%s)" % (
            fn, n, m[1], m[2])
        round_ = m[9]
        if "B94" not in round_:
            round_ = (round_ + "｜B94_F1") if round_ else "B94_F1"
        row = [
            m[0], m[1], m[2],
            ov.get("判据出处", m[3]),
            ov.get("判据要点", m[4]),
            ov["状态"],
            ov.get("我们的落点", m[6]),
            ov.get("差异", m[7]),
            m[8],
            round_,
            ov.get("备注", m[10]),
        ]
        assert len(row) == NCOL
        for c in row:
            assert "\t" not in c and "\n" not in c and "\r" not in c, "列里混进控制符: " + c[:60]
        out.append("\t".join(row))

    data = ("\n".join(out) + "\n").encode("utf-8")
    assert b"\r" not in data
    # 先算好再写（io.open(p,'wb').write(...) 会先截断）
    io.open(OUT, "wb").write(data)

    # 读→写→再读：11 列、行数、键 全等
    back = split_rows(OUT)
    assert len(back) == len(out), "回读行数不等"
    for a, b in zip(out, back):
        assert a == "\t".join(b), "回读列不等:\n" + a[:200] + "\n" + "\t".join(b)[:200]
    print("WROTE", OUT, len(out) - 1, "rows;", len(data), "bytes")


main()
