# -*- coding: utf-8 -*-
"""生成 `账/_parts/B92_B3.tsv` —— B92 波 4 · B3（战斗判据）的账补丁。

做法（⛔ 不手抄 11 列）：
  1. **读主表** `账/战斗逻辑驱动.tsv`，按「原版类 + 方法签名前缀」定位这 22 行；
  2. 对每行**只改我要改的那几列**（状态 / 轮次 / 判据要点 / 差异 / 我们的落点 / 备注），其余列**原样保留**；
  3. 写出**整行 11 列**（= 整行覆盖），表头逐字照抄主表；UTF-8-BOM + CRLF。

校验（不过就抛异常，⛔ 不静默写）：
  · 每行必须 11 列 · `方法` 列必须带 `(` · 状态列必须是 A/B/C/D 单字母 ·
  · 补丁行数必须等于 22 · 每行都能在主表里找到唯一一条
"""
import io
import sys

MAIN = r'd:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv'
OUT = r'd:/4/Unity/资料/完全复刻/账/_parts/B92_B3.tsv'

ROUND = 'B92_B3'

# key = 原版类 + '.' + 方法签名前缀（到 '(' 为止，含重载区分用的形参首词）
# 值 = {列序号(0-based): 新值}；None 表示「在原文后追加」由 APPEND 处理
APPEND = {}   # key -> {列序号: 追加文本}
SET = {}      # key -> {列序号: 整体替换}

# ---------------------------------------------------------------- 列序号
C_DOMAIN, C_CLASS, C_METHOD, C_SRC, C_POINT, C_STATE, C_WHERE, C_DIFF, C_LEVEL, C_ROUND, C_NOTE = range(11)


def ap(method, src=None, point=None, where=None, diff=None, note=None):
    d = {}
    if src is not None:
        d[C_SRC] = src
    if point is not None:
        d[C_POINT] = point
    if where is not None:
        d[C_WHERE] = where
    if diff is not None:
        d[C_DIFF] = diff
    if note is not None:
        d[C_NOTE] = note
    APPEND[method] = d


def st(method, state):
    SET.setdefault(method, {})[C_STATE] = state


# ================================================================ 1) 三条核销 A
st('LogActivatedTrigger(bool isPlayer, AbilityTrigger trigger)', 'A')
ap('LogActivatedTrigger(bool isPlayer, AbilityTrigger trigger)',
   point='｜【B92_B3 2026-10-28】`ChallengeLogMgr__LogTriggerAbility.c:1-19` 全文：`if (param_3 != 0x14 && != 0xf0 && != 0x276) ⇒ Signal.Raise(new TriggerAbilitySignal(trigger, isPlayer))`。'
         '🆕 **`challengeLog` 在 Setup 里【无条件】** `new ChallengeLogMgr()`（`out_cpp2il_dev_ilspy/BattleManager.cs:15276`）⇒ 那道 `!= null` 闸在真对局里**恒真**，'
         '**不是**「只有挑战模式才记」。原版两个调用点 = `CardScript.cs:14959`（`AbilityTrigger.Survivor`）/ `RawCardScript.cs:1791`（通用 `ability.trigger`）。',
   diff='｜【B92_B3 2026-10-28 **判 A（等价实现）**】🔴 账里「引擎侧 `AchievementTally` **未过滤**那三条」**已过期** —— 2026-10-27 `E7ENG` 已加同款 20/240/630 过滤'
        '（`AchievementTally.cs:195-197`，doc 直接引了 `ChallengeLogMgr__LogTriggerAbility.c` 原文）⇒ 引擎侧 + 表现层（`B2AU`）两半现在都对齐原版。',
   note='｜【B92_B3 判 A】剩一条**断言**（不是缺口）：原版那两个调用点是否都从 `EvtKind.Trigger` 单口走过（`Ach_*` 计数完整性）。')

st('MulliganFallbackCountdown()', 'A')
ap('MulliganFallbackCountdown()',
   point='｜【B92_B3 2026-10-28】协程真体 163 行全解码：`WaitForSeconds(`**4.0**`)` → 循环（`waitingWhileDisconnected` 时 `yield manager+0x4a8` 且**不计数**；'
         '否则 `attempts(+0x30)++` → 没收到对面换牌 ⇒ `BattleCommsManager.RequestMulliganResend(attempts)` / 收到 ⇒ `ConfirmMulliganReceived(...)`）'
         '→ `attempts > `**15**` 且仍没收到 ⇒ `CancelMatchWithVictory("MulliganFallbackCountdown " + matchData.opponentId)`（**判本机胜**）；退出格 = `+0x29c` / `+0x2a8`。'
         '两个数现读：`0x1834b2df0` = **4.0**（4B float）· `15` = `.c` 里的 `0xf < counter`。',
   diff='｜【B92_B3 2026-10-28 **判 A（等价实现）**】🔴 账里「还差：**超时 ⇒ 判胜那一支**」**已过期** —— `NetBattle.TickMulliganFallback`（`:1962` 一族）已逐条对齐：'
        '`MulliganFallbackInterval = 4f`（`:1869`，与原版字面量 `0x1834b2df0 = 4.0` 逐值吻合）· `MulliganFallbackAttempts = 15`（`:1871`，对应 `.c` 的 `0xf < counter`）· '
        '`attempts > 15` 且 `!_peerMulliganSeen` ⇒ `AbortWithVictory(...)`（= 原版 `CancelMatchWithVictory` ⇒ 判本机胜）；掉线期间由 `TickReconnectCountdown` 抢先 = 原版 `waitingWhileDisconnected` 不计数。',
   note='｜【B92_B3 判 A】两处本地等价已如实标（不发 confirm 帧 / 重发自己的换牌）。')

st('Get2DPosOfCard(CardScript card, bool useCenter)', 'A')
ap('Get2DPosOfCard(CardScript card, bool useCenter)',
   diff='｜【B92_B3 2026-10-28 **判 A —— 按 A 尺子 ①**】本体逐句实现（`BattleDriver.Get2DPosOfCard` + `IsInPlayOrDying`，`PlaneDistance = 20.3f`，'
        '判据 `decomp_full/BattleManager__Get2DPosOfCard.c:1-97` 全文）；🔴 **6 个调用点未接** —— 原版这条有 **7 个活调用点**，我们只接了 `ShowPlayTargetPrompt` 那 1 个'
        '（其余：狂暴单位指针 · 教程指针 `from` 的 6 档 · 目标准星）⇒ **上游登记为 B/C**，⛔ 别写成「已完成」。',
   note='｜【B92_B3】A 尺子 ①：零调用者 / 上游未接 ⇒ A，差异列已写明上游是谁、账在哪一条。')

# ================================================================ 2) 三条判 D
st('AllUnitsInPlay(List<CardScript> cards)', 'D')
ap('AllUnitsInPlay(List<CardScript> cards)',
   point='｜🔴【B92_B3 2026-10-28 现读 · 就地订正】`decomp_full/BattleManager__AllUnitsInPlay.c:19-38`：项为 **null** 时**不是**返回 false —— 它 `break` 落到 `FUN_1803f47a0`（不返回）= **抛 NRE**；'
         '**只有「不在场」（`IsInPlay()` 假）才返回 false**。ILSpy `:41180-41196` 同（`cardScript == null` ⇒ `NullReferenceException`）。`cards == null` 也走 NRE。',
   where='｜⚠️【B92_B3】**名字撞车，⛔ 别当已复刻**：`RuleEngine/Data/AiTargets.cs:399 AddAllUnitsInPlay` 是 **AI 候选池**（`ta` 140/150 那一档，判据 `GetUnitsInPlayPooledList.c:70-73`），与原版这条「整表都在场吗」的谓词**语义无关**。',
   diff='｜【B92_B3】四源全负已逐源复核：`decomp_full` / `out_cpp2il_dev_ilspy` / `out_il2csharp` / `assets_full` 的 `m_MethodName` 都是 **0 调用点**，且 `private bool`（无反射 / UnityEvent 入口）。',
   note='｜【B92_B3 2026-10-28 **判 D**】(c) 四源全负 · 时间限定 = **本 build**（`GameAssembly.dll` 2026-09-17）+ **2025-07-30 资源快照** · '
        '可逆备注 = 一旦发现生产点（含事件订阅 / 状态机丢体 / `ResolveAction` 内联）即回 C；等价位 = `Units()` 逐项 `IsAlive && !IsPendingDeath`。')

st('HandleOfflineBattleResult()', 'D')
ap('HandleOfflineBattleResult()',
   point='｜🔴🔴【B92_B3 2026-10-28 就地订正 —— **上面那句判据要点不成立**】真体（`out_cpp2il_dev_ilspy/BattleManager.cs:19590-19641`，字段名齐全）= '
         '`BattleFinished()` → **若 `_currentMatchResult.battleWinner == BattleWinner.Player`** ⇒ '
         '① `onlineMatchResult.EndMatchData.EarnedRating = 30` · `.NewRating = 30`；'
         '② `PlayerPrefs.SetInt("xp", 30)` · `PlayerPrefs.SetInt("elo", 30)`；'
         '③ 若 `matchData.matchType == MatchType.Campaign` ⇒ `tipsData.campaignStageIndex += 1`（等于 `campaignSets[0].campaignStages.Length` 时走 `@1805E3920`，**方法体未解出**）→ `PlayerPrefs.SetInt("campaignIndex", idx)`；'
         '④ 最后 `_currentMatchResult.resultSaved = true` · `.finishedProcessingResult = true`。'
         '`.c` 同体（`decomp_full/BattleManager__HandleOfflineBattleResult.c:1-108`）可用 `0x1e = 30` / `0x82 = 130 Campaign` 交叉验证。**错因** = 只读了 `decomp_full` 那份（无字段名，看不出一整套本地结算）。',
   diff='｜🔴【B92_B3 订正】「上报属 P3」**不成立**：`EarnedRating`/`xp`/`elo` 确实是服务器语义，但 **本地结算那半（`campaignStageIndex++` / `campaignIndex` / 两个 result 标志）原版就写在本地**、不是远端调用。'
        '我们**没有对应物** —— `campaignStageIndex` 只在 `RuleEngine/Core/PlayerDataMath.ShouldShowRatePopup(campaignCompleted, campaignStageIndex)` 里当**入参**（3 处），**没有任何推进点**。',
   note='｜【B92_B3 2026-10-28 **判 D**】(c) 四源全负（`decomp_full` / `out_cpp2il_dev_ilspy` / `out_il2csharp` / `assets_full` 的 `m_MethodName`；`BattleFinished` 另有活调用方 `decomp_full/BattleManager__DeadHero.c`，本方法没有）· '
        '时间限定 = 本 build + 2025-07-30 资源快照 · 可逆备注 = **一旦发现调用点 ⇒ 回 C，且本地结算那半要按真体实现**（并按铁律 18·0 打 `🔶 自己设计（服务器关服）`）。')

st('MobileAdjPos(Vector3 pos)', 'D')
ap('MobileAdjPos(Vector3 pos)',
   point='｜【B92_B3 2026-10-28】🔴 **它不是死代码**：被**内联进 `BattleManager.Update()`**（`out_cpp2il_dev_ilspy/BattleManager.cs:48046-48053`）—— '
         '`GetMousePosInWorldSpaceCanvas()` → y 加 `desktopVariables.targetYOffset` → `GetTargetInPos(...)`，即**每帧鼠标命中判定**那条链。'
         '判法 = 搜**字段特征** `targetYOffset`（搜方法名 0 命中、搜字段命中 1 处）⇒ 直接调用点 0 是**内联假象**，⛔ 别据此判死代码。',
   diff='｜【B92_B3】值已钉死：`desktopVariables` 序列化指向 **`VarsDesktop.asset`**（`Battle Arena 1.unity` 的 BattleManager 块 guid `4e557a09c91a039418270e7d751c31b9`），其 `targetYOffset = `**0.0**；`VarsMobile = 0.2`。'
        '⇒ 桌面构建下 **加 0 = 恒等变换**。另：它读的是 **desktop** 那一份（**不是** `mobileVariables`），名字与行为不符。',
   note='｜【B92_B3 2026-10-28 **判 D**】(b) 数据恒 0（**桌面** `VarsDesktop.targetYOffset = 0.0`；移动端 0.2）· 时间限定 = 本 build 的场景序列化 + `VarsDesktop.asset` · '
        '可逆备注 = **移动端构建下值非 0 ⇒ 回 C**；做 `DeviceVariables` 那一行时顺手转调即可，⛔ 别单开一轮。')

# ================================================================ 3) 其余就地订正
ap('IsCampaignStageAvailable()',
   point='｜🔴【B92_B3 2026-10-28 就地订正】真谓词**不是**「`turnScriptedData.Count >= 1`」，而是 **`IsCampaignLike() ∧ currentCampaignStage != null ∧ turnCounter(+0x3f8, ObscuredInt) > 0 ∧ turnCounter - 1 < currentCampaignStage.turnScriptedData.Count`**'
         '（`decomp_full/BattleManager__IsCampaignStageAvailable.c:1-150` 的 `0 < (int)uVar3` 与 `iVar2 + -1 < Count` 两句；`out_cpp2il_dev_ilspy/BattleManager.cs:35852-35922` 同）。'
         '**错因** = 把那个 `0 < uVar3` 当成了「Count >= 1」。',
   diff='｜【B92_B3】✅ 我们 `RuleCore.CurrentTurnScriptedData`（`:1242-1250`）**反而是对的**（`i = ctx.Turn - 1`，判 `i >= 0 && i < cs.turns.Length`）⇒ 行为上不缺，差只差 `ctx.CampaignStage` 的**唯一写点**。',
   note='｜【B92_B3】战役关卡数据：本地 `assets_full` 确认**无** campaign stage 包（只有 `campaignrewardbackgrounds`）⇒ 先挖 `D:/2/关服前存档/安卓版本warpforge，包含远端资料/…/UnityDataAssetPack.apk` 再定 P3。')

ap('LeaveBattle()',
   diff='｜🔴【B92_B3 2026-10-28 就地订正】**G13 判据 ①「`ReleaseCardbacks()` 全仓无对应物」已过期** —— `BattleDriver.ReleaseCardbacks()` 2026-10-26（`W9K01`）已落地，'
        '并在**结算面板末尾**调（`BattleDriver.cs:32448`）：语义等价、**时机比原版早**（早于玩家点屏离开）。⇒ 原版三支只剩 ③「教程前两局重开教程局」。',
   note='｜【B92_B3】③ 还差两样：① **战斗教程**前两关完成位的本地存档口径 —— 我们 `MenuTutorialFlow.FirstTwoTutorialCompleted` 读的是**菜单 FTUE** 的 Done 位，'
        '原版读 `PlayerDataManager.tutorialStages(+0xF8)` 那是**战斗教程**下标（`MenuTutorialFlow.cs:1097-1098` B91_R2 已就地订正）；② `TutorialManager.SetupTutorialMatch()` 的等价物（全仓 0 命中）。')

# ================================================================ 4) 判据补强（状态不变）
ap('ApplyEnvEffect(RawCardScript card, bool isPlayer)',
   where='｜🆕【B92_B3 2026-10-28】**执行体早就写好了、只是没接线**：`RuleEngine/Core/EffectResolver.cs.RunAbilityOfAnotherCard(ctx, abilityCard, triggerName, owner, …)`'
         '（`:17993`，doc 自己写着「本方法把入口补齐；**接线**在 `RuleCore` 里 ⇒ 本写手不动那个文件」）⇒ 生产调用点仍 = **0**。',
   diff='｜【B92_B3】🔴 订正措辞：「全仓 `TriggerOtherCardAbility` 0 命中」**只对那个 C# 名字成立**，等价入口 `RunAbilityOfAnotherCard` 已有（见落点）⇒ 缺口是**一跳接线**，不是整条没做。'
        '接线**顺序必须是「督军触发 → `ApplyEnvironment`」**（`.c:14` 在 `:20` 之前）。另：入口只跑文本正文（`CardDef.TriggerOps`），原版 `chosenAbility` 驱动档没有分派口。',
   note='｜【B92_B3】落点收窄：缺的只是 `BattleDriver.ApplyOffensiveEnvOnce()` 里那一跳（插在换环境**之前**）。')

ap('CanUseActiveAbility(CardScript parentCard, bool isPlayerAsking, bool showTips)',
   point='｜【B92_B3 2026-10-28 逐句现读 `decomp_full/BattleManager__CanUseActiveAbility.c:1-170`】七道闸全文：① `card.isPlayer(+0x40) == isPlayerAsking`；② 该侧 `canAct`（`+0x244/+0x246` ObscuredBool）；'
         '③ `cardState(+0x228) == 2` 且 `HasEnoughMana(pm, 该 ActiveAbility 自己的 cost, AbilityUsesPsychicEnergy(card), AbilityUsesPlasmaEnergy(card))`，不足且 `showTips && isPlayerAsking` ⇒ `NotifyCantDoAction(<I2 key>)`；'
         '④ `IsTutorialMatch ∧ 玩家侧 ∧ CheckIfPlayerActionPermittedInTutorial(0x1e = 30, card)`；⑤ `+0x418`（战斗结束）；⑥ `HasCurrentTrait(0x4c4 = 1220) && card +0x4c`；⑦ `HasCurrentTrait(0x4fb = 1275) && !CanUseOathAbility`。',
   diff='｜【B92_B3】已做：残骸 / 眩晕 / 力竭 / Duty / Oath 五道 + **数据侧 mana 闸**（`RuleCore.CanStartActiveAbility:15306`）与扣费（`:15395`），卡表 **v39 / 1,129 张**已出四列。'
        '**仍缺四格**：③ 的 psychic / plasma 资源闸（两列已落表但 **0 消费**）· ④ 教程 actionType 30 · ⑤ `+0x418` · ⑥ trait 1220。',
   note='｜【B92_B3】④ 必须接在**表现层** `BattleDriver.TutorialPermits(TutorialAttempt)`（⛔ 别塞进 `RuleCore.Can*` —— `SimpleAI.GetAvailableActions` 也在用；原版那一闸挂 `BattleManager`=UI 侧）；接之前先核 `TutorialAttempt` 有没有 activeAbility 档。')

ap('DeviceVariables()',
   point='｜【B92_B3 2026-10-28】🔴 「`.c` 不能当判据」已复核成立：`decomp_full/BattleManager__DeviceVariables.c` 全 **117 字节**，内容是 `AlternateArtCard__get_CardImage`（文件名与内容不符）。'
         '✅ **序列化真值已钉死**：`d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Battle Scenarios/Battle Arena 1.unity` 的 `BattleManager` 块 '
         '`desktopVariables: {guid: 4e557a09c91a039418270e7d751c31b9}` = `VarsDesktop.asset`（`mobileVariables` 指向 `VarsMobile.asset`，但可读四源**零消费者**）。'
         '**`VarsDevice` 9 项真值**（`assets_full/bundle_duplicateassetisolationso_assets_all/MonoBehaviour/Vars{Desktop,Mobile}.json` 现读）—— 桌面 / 移动：'
         '`cardInHandShownScale` 1.3 / 1.7 · `cardInHandShownYOffset` 30 / 25 · `cardInHandShownYOffsetMin` 20 / 12 · '
         '`extraSpaceOnSelectedCard` **恒 2.0 的平曲线** / (0,2)→(1,5) 斜率 3 · `warlordStartSizeFactor` **1.3** / 1.5 · '
         '`tutorialTipHorizontalOffset` 20 / 18 · `…ForElements` 18 / 16 · `moveCardYOffset` 0.2 / 0.7 · `targetYOffset` **0.0** / 0.2。',
   diff='｜【B92_B3】我们**没有 `VarsDevice` 数据件**；9 项里 5 项已以「写死常量 + 引解包值」散落实现（scale 1.3 `Hand/HandLayout.cs:267` · YOffset 30 `HandLayout.cs:34` · '
        'tip 20 / ForElements 18 `BattleDriver.cs:17033/17038` · moveCardYOffset 0.2 `BattleDriver.cs:36013`）；🔴 **`warlordStartSizeFactor`（桌面 1.3）全仓 0 命中 = 整条没做**。'
        '⚠️ `stub`（`d:/2/Warpforge_code/.../VarsDevice.cs`）里 `targetYOffset = 0.7f` / `moveCardYOffset = 0.7f` 的**类内默认值不是真值**（真值全在 SO），照它实现会凭空多出 0.7 的偏移。',
   note='｜【B92_B3】`warlordStartSizeFactor` 的消费者在可读四源里**没找到**（只命中 `VarsDevice.cs:13` 的定义）⇒ 接线前先确认作用点，⛔ 别按「大概是督军入场缩放」去接。数据件落点 = 新建 `CardPresentation/Core/VarsDevice.cs`（9 项 + `GetCardInHandYOffsetTouchInput()`）。')

ap('DisplayBigChat(string chatMsgText, CardScript talkingUnit)',
   diff='｜【B92_B3 2026-10-28】代码侧已等价：`Battle/BattleBigChat.cs:95` 两道 guard 逐句照 `.c`、`:115` 是唯一实现（`Speak(Radio)` + `CardArt.TryDeckHero` —— 那个函数的判据本身就是 `fullHeroSpriteReference`）。'
        '剩余**不是我们的缺口**：原版显示调用体两棵树都只有 guard（铁律 11 例外①）。',
   note='｜【B92_B3】除真机验收外无剩余 ⇒ **建议核销 A**，等调度台批（本批只批了 3 条 A，故此处不自行改档）。')

ap('DisplayBigChat(string chatMsgText, Sprite talkingUnitSprite)',
   point='｜【B92_B3】`:21683-21700` 「两棵树都只剩 guard」已复核；`VoiceLineData` 5 形参旁证成立（气泡 + 传入 sprite + 无音频 + 时长 + 卡类型）。',
   note='｜【B92_B3】同 `DisplayBigChat(CardScript)` 那一行：除真机验收外无剩余 ⇒ 建议核销 A，等调度台批。')

ap('GetEnvEffectCards(bool shouldUseOffensive, bool justOwnedCards)',
   point='｜【B92_B3 2026-10-28 逐句 `decomp_full/BattleManager__GetEnvEffectCards.c:1-190`】取边 = `+0x247` 假 ⇒ `+0xc0` / 真 ⇒ `+200`（`:27-38`）· 空卡**先**塞 `GetEmptyOffensiveCard(army)`（`:44-73`）· '
         '`forgeLevel` = 命中那个 forge 事件的 `+0x34`（`:79-186`）· 找不到事件 ⇒ **只返回空卡那一张**。',
   diff='｜【B92_B3】已等价的部分：空卡由 `OffensiveCards.Choices(faction)[0]` 提供、`ChoicesForDraft` **不含**它（照 `.c`）。🔴 **仍缺**：`ChoicesForDraft(offensive: false)` **防御那一半未接**（仍走 `DefensiveChoicesOwnedAt`）。',
   note='｜【B92_B3】`forgeLevel` 来源 = 本地 `ForgeData.LevelOf`（原版 = LiveOps 事件字段 ⇒ P3）。')

ap('GetEnviromentalCardList(CardArmy useArmy, bool shouldUseOffensive, bool justOwnedCards, int rewardsCollected)',
   point='｜【B92_B3】🔴 第 4 形参 `rewardsCollected`（`param_5`）**在 `.c` 体内根本没被读** —— 只被塞进 `GetEnvCardList` 的第 5 实参位置 ⇒ 接线时 ⛔ 别把它当独立输入。',
   diff='｜【B92_B3】mode 分派已接（同 `GetEnvEffectCards`）；防御半 / LiveOps 源 / 本机胜场三项同上。',
   note='｜【B92_B3】同 `GetEnvEffectCards`。')

ap('GetTraitEffect(DefinedTrait trait, CardTrait.EffectTriggerType trigger, CardArmy cardArmy)',
   point='｜【B92_B3】`.c` 全文一句 + `return`（`:1-7`）；**5 个活调用点** = `BattleCardUI.cs:1708 / :1840 / :1855 / :2144` · `CardScript.cs:16735`。',
   diff='｜【B92_B3】剩余 = **返回形态差**（原版返回 `CardAnim` 资产引用，我们返回 prefab 名再二次解析）；`CardTraitCollection.GetTraitEffectObject` 的阵营回落三条已核。')

ap('IsClosedDeckGame(MatchType chosenMatchType)',
   point='｜【B92_B3】✅ 已核 `GameMode.ClosedDeck(7) → MatchType.ClosedDeck(110)`（`RuleEngine/Core/BattleContext.cs:2165`），而 110→120 只改 `MatchType`、不改 `PlayMode`（`:2229`）'
         '⇒ 现在 `Ctx.PlayMode == GameMode.ClosedDeck` 那处判断**同时覆盖 110 与 120**，不是缺陷。',
   diff='｜【B92_B3】`public static` 在可读四源里 0 调用点，但是**被内联**（对照 `AllUnitsInPlay` 才是真 0）⇒ ⛔ 不判 D。',
   note='｜【B92_B3】剩纯口径统一：`BattleContext.MatchTypes` 类尾加 `IsClosedDeck(MatchType)`，把 `OffensiveChoicesOwnedCore` 等 3 处手写比较收口；**零行为变化**。')

ap('IsPremulliganScriptAvailable()',
   point='｜【B92_B3 2026-10-28】**唯一消费者已定位**：开局协程 `out_cpp2il_dev_ilspy/BattleManager.cs:13716-13727` —— 真 ⇒ `GetPremulliganScriptedData()` → '
         '`ExecuteScriptedTurn(data, newScriptedTurn: true, inBattleActions: false, onComplete)` 并**占住协程**（`yield` 到脚本跑完）。',
   diff='｜【B92_B3】我们**没有谓词、也没有那一跳执行**（`BattleDriver.cs:17734` 只有注释）⇒ 缺的是「谓词 + `newScriptedTurn:true / inBattleActions:false` 那一跳」。',
   note='｜【B92_B3】数据 6 关 `scriptedTurn` 全 false ⇒ **恒假**（休眠，⛔ 不判 D —— 数据一变就可达）；接线照做，今天零行为变化。')

ap('IsScriptedStageAvailable()',
   point='｜【B92_B3】**唯一消费者** = `UnitOnBoardAttackTypeSelector.cs:206`。',
   diff='｜【B92_B3】教程半 = `MatchTypes.IsTutorialMatch` + `Tutorial.Stage`（`BattleDriver.cs:36853` 那一族手写守卫，可收口成一个谓词）；战役半已内联但 `ctx.CampaignStage` 恒 null ⇒ 恒 false。',
   note='｜【B92_B3】上游同 `IsCampaignStageAvailable`（战役关卡数据 / 写点）。')

ap('PlayAi()',
   point='｜【B92_B3 2026-10-28】`BattleManager._PlayAi_d__399__MoveNext.c:1-260` **九态全解码**：① 在线 bot 先等一档（回合第一步 `GetFirstActionDelay`，其余 `GetActionDelay`；'
         '`FeatureConfig.ShouldDisableNewMatchMaking()` 为真时兜底 `UnityEngine.Random.Range(0.2, 3.2)`）② `waitingWhileDisconnected` ⇒ `yield manager+0x4a8` ③ 有脚本回合 ⇒ `SkippableActionWait` + `PlayScriptedTurn` + `CloseChatBoxes` '
         '④ 否则 `+0x246`（**AI 坐本地玩家侧**）⇒ `AI.PlayTurn(mgr, enemyRawBattleData+0x78)` ⑤ `ActionsInQueue` ⇒ `yield manager+0x4b0`（**0.5**）再循环 ⑥ 循环出 ⇒ `WaitForSeconds(GetEndTurnDelay())` ⇒ `AddEndTurnAction`。'
         '字面量现读：`0x1834b2bb0` = **0.2** · `0x1834b3404` = **3.2** · `0x1834b2bb4` = **0.5**。',
   diff='｜【B92_B3】形状已对齐（`DriveAiTurn` 三步延迟 + 教程脚本支 + `WaitingForBlockingAction` 串行闸 + `AiEndTurnSoon` = 原版 `case 8`）。'
        '🔴 **仍缺两点**：① 延迟区间来源 —— 原版读 `AIBotsConfig`（LiveOps 配置），我们写死 `0.45–0.65 / 0.70–1.00 / 0.60–0.90`（`BattleDriver.cs:34622-34624`），**来源未核**；'
        '② 原版「**只对在线 bot** 才加延迟」那道 `IsAgainstOnlineBot()` 门我们没有（所有 AI 一律加同一档）。',
   note='｜【B92_B3】先查 `D:/2/关服前存档/安卓版本warpforge，包含远端资料/` 有没有 `AIBotsConfig` 的默认值；查不到 ⇒ 那三个区间标 `🔶 自己设计`。')

ap('ResolveAbilityIconAnim(BattleAction battleAction)',
   point='｜【B92_B3 2026-10-28】`BattleManager._ResolveAbilityIconAnim_d__475__MoveNext.c:1-60` 全文 + 字面量现读复核：`manager+0x4b0` = `0x1834b2bb4` = **0.5** · `manager+0x4c8` = `0x1834b2bb0` = **0.2**。',
   diff='｜【B92_B3】🔴 **`waitForAnim` 的结论升级**：`BattleAction.waitForAnim`（`BattleAction.cs:49`）在**整棵 ILSpy 树里零读者** —— `:4378 / :4538 / :4600` 那三处是 '
        '`_ResolveAnimAnimationClip` / `_ResolveAnimInfoRangeAttack` **自己的同名字段**（不同协程、不同状态机），**不是** `BattleAction` 的。'
        '⇒ 「消费点没查到」应改写成「**可读四源里根本没有读者**」⇒ **只做读口是正解，⛔ 别再派人反复查**。',
   note='｜【B92_B3】剩形态差（协程 → 自有时钟 `Clock` 的一步式）+ 读口保留。')

ap('ResolveRevealOrders(BattleAction battleAction)',
   point='｜【B92_B3 2026-10-28】`BattleManager._ResolveRevealOrders_d__549__MoveNext.c:1-90` **三态全解码**：`state0` `secret = card.HasSecretOrder(action+0xa8)`；null ⇒ `LogWarning` + `FinishResolvingAction`，'
         '否则 `card.RevealedSecret(...)` ⇒ `yield +0x4b0`（0.5）；`state1` `revealed = DisplayRevealedCard(..., card.isPlayer(+0x40))` → `ShowDescription()` ⇒ `yield +0x488`（2.0）；'
         '`state2` `card.DestroyCard(0.2)` + `StartCoroutine` → `secret.type(+0x38) == 0xf(15)` ⇒ `RemoveEnchantment(card, secret, 2)` → `CemeteryManager.AddExecuteSecretOrderAction(card, secret+0x18)` → `FinishResolvingAction`。'
         '字面量现读 `0.5 / 2.0 / 0.2`。',
   diff='｜【B92_B3】出队口**四源 0 调用点**已复核（`AddRevealOrders`（`ILSpy:38836`）入队、`ResolveRevealOrders`（`:28445`）在 `decomp_full` / `out_cpp2il_dev_ilspy` / `out_il2csharp` / `assets_full` 里都没有调用者）⇒ 依 E4 族9 保持 B。',
   note='｜【B92_B3】接线前先证明有入队生产点（`BattleActionType.revealOrders = 49`），照上面三态 + 三个常量。')

# ================================================================ 生成
def key_of(row):
    c = row.split('\t')
    if len(c) < 3:
        return None
    m = c[C_METHOD]
    i = m.find('(')
    return c[C_CLASS] + '.' + (m if i < 0 else m)  # 用完整签名做键（含形参名），确保重载不混


raw = io.open(MAIN, 'rb').read().decode('utf-8-sig')
lines = raw.replace('\r\n', '\n').split('\n')
header = lines[0].split('\t')
assert len(header) == 11, '主表表头不是 11 列: %r' % header

targets = {}
for i, ln in enumerate(lines[1:], 2):
    c = ln.split('\t')
    if len(c) != 11:
        continue
    if c[C_CLASS] != 'BattleManager':
        continue
    sig = c[C_METHOD]
    if sig in APPEND or sig in SET:
        assert sig not in targets, '主表里同签名出现两次: %s' % sig
        targets[sig] = (i, c)

missing = [k for k in (list(APPEND) + list(SET)) if k not in targets]
assert not missing, '这些键在主表里找不到: %r' % missing

out_rows = []
for sig, (lineno, c) in targets.items():
    cells = list(c)
    for col, txt in APPEND.get(sig, {}).items():
        assert isinstance(txt, str) and txt
        assert '\t' not in txt and '\n' not in txt, '列内不许有 tab/换行: %s' % sig
        cells[col] = cells[col] + txt
    for col, txt in SET.get(sig, {}).items():
        assert '\t' not in txt and '\n' not in txt
        cells[col] = txt
    # 轮次列追加本轮代号
    if ROUND not in cells[C_ROUND]:
        cells[C_ROUND] = (cells[C_ROUND] + ' · ' + ROUND) if cells[C_ROUND].strip() else ROUND
    assert len(cells) == 11, '列数不是 11: %s' % sig
    assert '(' in cells[C_METHOD], '方法列没有签名: %s' % sig
    assert cells[C_STATE] in ('A', 'B', 'C', 'D'), '状态列不是单字母: %s = %r' % (sig, cells[C_STATE])
    out_rows.append((lineno, cells))

out_rows.sort(key=lambda x: x[0])

body = '\t'.join(header) + '\r\n'
for lineno, cells in out_rows:
    body += '\t'.join(cells) + '\r\n'

assert len(out_rows) == 22, '补丁行数不是 22: %d' % len(out_rows)

data = b'\xef\xbb\xbf' + body.encode('utf-8')
io.open(OUT, 'wb').write(data)

print('OK -> %s' % OUT)
print('rows = %d  bytes = %d' % (len(out_rows), len(data)))
for lineno, cells in out_rows:
    print('  main:%d  %-14s  %s' % (lineno, cells[C_STATE], cells[C_METHOD][:60]))
