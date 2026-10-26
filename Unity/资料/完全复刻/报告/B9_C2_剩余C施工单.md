# B9 · 剩余 C 114 条 · 施工单总表

> 判据代理 `C2`（第八十三会话「完全复刻」）· 2026-10-26 · **只读核**（未改任何 `.cs`、未跑 Unity、未动 git、未改主账表）
> 配套逐条施工单：**`判据/B9_剩余C_施工单.tsv`**（12 列 × 114 行，逐条「原版步骤 / 我们缺什么 / 施工步骤 / 怎么验 / 目标文件 / 风险 / 建议等级」）
> 判据来源：🥇 `cpp2il/out_cpp2il_dev_ilspy` 真方法体 · 🥈 `decomp_full/*.c` · 🥉 `assets_full` 字段

---

## §0 账的覆盖边界（必读）

- **114 = `账/战斗逻辑驱动.tsv` 状态 `C` 的全部行**（2026-10-26 现读）。分母不是「原版全部方法」（那是 13,182 类账 / 3,101 条可指名实现），**只是本域判为「原版有该行为、我们缺或不同」的那批**。
- ⚠️ **主表正在被并行代理改**：本代理执行期间出现过列错位（判据要点写进「状态」列）与状态瞬时翻转；**现读（收尾时）已核对**：只有 **#953 `TriggerOtherCardSpiritStone` 是 A**（他人改判），其余 113 行全是 C。另外 **`HasPendingActions` / `PlayNextScriptedAction` / `PlayScriptedTurn` 各有两个重载** —— tutorial 重载（`TurnScriptedData`）是 A，**campaign 重载（`List<ScriptedActionCampaignData>`）仍是 C、仍要做**（本表跟的是 campaign 重载，行号 #105/#109/#111）。
- ⇒ 本表已按「**原版类 + 完整方法签名**」重新定位过行号；**方法名 + 形参才是稳定键，行号只是快照**。
- **114 条里 108 条读到了原版方法体**（含状态机）；**6 条只能部分**（清单见 §6「判据拿不到的」）。
- 统计口径：卡池张数一律**不写死**（本表出现的「池内 N 张」全部是判据代理现读的快照）。

---

## §1 分级：现账 → 建议（本节括号内数字 = **账行号**）

| | P0 | P1 | P2 | 合计 |
|---|---|---|---|---|
| 现账 | 3 | 24 | 59 + 未定级 27 + 中 1 | 114 |
| **建议** | **3** | **38** | **73** | 114 |

- **未定级的 27 + 「中」1 = 28 条已全部定级**：→ P1 **12** 条、P2 **15** 条、「中」→ P2 **1** 条。
- **P2 → P1 4 条**（玩家主链可见/会改数值）：`AdjustCardEffect`(64) · `HeroLandIntoField`(782) · `RemoveCostEffects`(850) · `TransformFromRemnant`(938)。
- **P1 → P2 2 条**（原版本方法 **0 调用点**、两侧今天都不可观测，按铁律 11 仍要做、但排在长尾）：`GetDefensiveAttackValue`(12) · `SuicideHeroAttack`(39)。
- **调度台裁定后的口径（2026-10-26 收尾）**：8 条离开 C —— `HasWhileInPlayAdjacentEffect` → **A**；`ClearPendingDamage` / `EnoughPendingHealingToFull` / `SetCardState` / `RecordPendingDamage` → **B**；`RemoveCardFromCemetery` / `RemoveInHandEffects` → **D**；`TriggerOtherCardSpiritStone` → **A**（他人）。若全部落地，本域 **C 剩 106 条**（`RecordPendingHealing` 经复核**保持 C**：`UnitConditions.damaged(40)` 过滤器 27 行 + 原版「粘性累计预测表」语义）。
- **已被并行代理解决（本表保留、不施工）**：`TriggerOtherCardSpiritStone`(953) —— 现账已 A。（`HasPendingActions` / `PlayNextScriptedAction` / `PlayScriptedTurn` 的 **tutorial 重载**是 A，但 **campaign 重载**仍是 C，见 §0。）

---

## §2 按「目标文件」分组的工期估计（写手分配）

> 判据：**一个文件同一时刻只能一个写手**；`RuleCore.cs` 与 `EffectResolver.cs` 是硬瓶颈。工时为「判据已齐、只差照着做」的量级估计（含 typecheck 与自测，不含 Unity 批量验收）。

### 2·1 引擎侧（`RuleEngine/**`）共 **57** 条

| 目标文件 | 条数 | 其中 P0/P1/P2 | 估工时 | 写手建议 | 说明 |
|---|---|---|---|---|---|
| `Core/RuleCore.cs` | 18 | 2 / 2 / 14 | **3.5–4.5 人日** | 独占 1 写手 | P0 两条（203/217 广播口）+ 血量写点（716）+ 洗回清费用（850）+ 随机/动作队列 |
| `Core/EffectResolver.cs` | 10 | 0 / 6 / 4 | **2.5–3 人日** | 独占 1 写手 | 与 RuleCore **共享广播口/结算口** ⇒ 建议同一写手串行做两文件，或先 RuleCore 后 EffectResolver |
| `Core/Enchantment.cs` | 7 | 0 / 2 / 5 | 1.5–2 人日 | 可与 EffectResolver 同写手 | 摘附魔入队链（481/532）+ 两条谓词（986/989） |
| `Core/BattleContext.cs` | 5 | 0 / 3 / 2 | 1–1.5 人日 | 与随机源同写手 | 36/102/106 是**同一件事**（全局随机源），**必须一次改完** |
| `Core/UnitState.cs` | 4 | 0 / 2 / 2 | 1.5–2 人日 | 独占（改 `Has` 语义） | 99 会改全部 `Has` 读点 ⇒ 必须全池对拍 |
| `Data/AiScoring.cs` | 4 | 0 / 0 / 4 | 0.5 人日 | 任意 | 四条都「只加函数不接线」（0 调用点） |
| `Core/CardDef.cs` | 2 | 0 / 0 / 2 | 0.3 人日 | 与数据生成器同写手 | 需扩 `cards_truth` / `cards_engine` 列 |
| `Core/ActionPump.cs` | 1 | 0 / 1 / 0 | 0.3 人日 | 与 Aura 同写手 | 54 只差「打开接线口」+ 基线裁定 |
| `Core/CountEval.cs` | 1 | 1 / 0 / 0 | 0.8 人日 | 与 ActionPump 同写手 | 55 四条缺口（RandomChance / maxTargets / playerTurn / 卡级条件） |
| `Core/WhenEvent.cs` | 1 | 0 / 0 / 1 | 0.2 人日 | 任意 | 93 现已是 A（保留记录） |
| `Core/BoardSlots.cs` | 1 | 0 / 1 / 0 | 0.5 人日 | 与 SimpleAI 同写手 | 107 有符号格号 + 接线到 AI 部署 |
| `Data/SimpleAI.cs` | 3 | 0 / 0 / 3 | 0.3 人日 | 任意 | 三旋钮只做「标 🔶 + 注释」（值无本地判据） |

### 2·2 表现侧（`CardPresentation/**`）共 **51** 条

| 目标文件 | 条数 | 其中 P0/P1/P2 | 估工时 | 写手建议 | 说明 |
|---|---|---|---|---|---|
| `Battle/BattleDriver.cs` | 28 | 0 / 8 / 20 | **4–5 人日** | 独占 1 写手（最大瓶颈之二） | 入场/离场演出、拖拽首帧、AI 延迟、联机退出、手牌/墓地 |
| `Core/CardFeel.cs` | 5 | 0 / 3 / 2 | 1.5–2 人日 | 与 CardView 同写手 | 溶解↔复原一对（63/94/72/89） |
| `Core/CardView.cs` | 4 | 0 / 3 / 1 | 1–1.5 人日 | 同上 | 秘密面（85）+ CreatedBy 行（86）+ 攻击高亮（96） |
| `Battle/AbilityIconAnim.cs`（新） | 2 | 0 / 1 / 1 | 1 人日 | 与 BattleDriver 同写手 | 41/46 同一层 |
| `Battle/BattleBigChat.cs`（新） | 2 | 0 / 0 / 2 | 0.8 人日 | 与 UI 线同写手 | ⚠️ 显示细节判据拿不到（见 §4） |
| `Battle/BattleSfx.cs` | 2 | 0 / 0 / 2 | 0.5 人日 | 与音频线同写手 | 42/43 一条 FIFO |
| `Hand/CardInteraction.cs` | 2 | 0 / 1 / 1 | 1 人日 | 与手牌线同写手 | 108 首帧无洞布局 + 109 早退/教程指针 |
| `Core/TutorialScript.cs` | 2 | 0 / 0 / 2 | 0.3 人日 | 教程线 | 一条写注释（13）、一条待裁（16） |
| `Core/UnitTweenTable.cs` | 1 | 0 / 0 / 1 | 0.2 人日 | 与 CardFeel 同写手 | 64 抖位置 vs 抖缩放 |
| `Board/DeploySequence.cs` | 1 | 0 / 0 / 1 | 0.5 人日 | 与 BattleDriver 同写手 | 70 orderInPlay 语义 |
| `Core/ScreenUtilities.cs` | 1 | 0 / 0 / 1 | 0.4 人日 | 与 102 同批（同一数据件） | 39 `VarsDevice` 等价件 |
| `Shell/FTUESelectDeckWindow.cs` | 1 | 0 / 0 / 1 | 0.5 人日 | 教程线 | 6 FTUE bot 池（依赖教程线） |

### 2·3 待建 / 建议不落点：**6** 条

| 条目 | 条数 | 说明 |
|---|---|---|
| 战役线执行器（待建：17/20/21/22） | 4 | 强依赖战役线整体（20/21/22 已被并行代理改 A，剩 17 GetCurrentWaitTime 归战役线） |
| 建议不落点（79/84） | 2 | 见 §5「该改判」 |

**总计约 22–28 人日**（不含 Unity 批量验收；判据全部已落进 TSV，写手不必重查）。

---

## §3 最该先做的 10 条（本节 # = §5 总表序号，不是账行号）

| 序 | # | 方法 | 为什么先做 | 依赖 / 风险 |
|---|---|---|---|---|
| 1 | 26 | `BroadcastCardCreated` (P0) | 休眠机制但**结构最简单**（场上 IsInPlay + 双方手牌六开关全 1 两跳）⇒ 一次把「广播口」这套骨架搭好 | 与 27/51/52/53/54 共用同一套骨架 ⇒ **先做它，其余 5 条照抄** |
| 2 | 27 | `BroadcastTrapResolved` (P0) | 同骨架 + 唯一的「手牌跳排除陷阱自身」 | 紧随 26 |
| 3 | 55 | `BoardAnalysis.MeetsCriteria` (P0) | 卡级 `conditionsFilter` 未落 ⇒ 卡级 board 条件**恒按 0 数**（不受 triggerIf==8 的数据闸限制） | ③ RandomChance 依赖 36/102/106（随机源）；与 54 同属 ActionPump 面 |
| 4 | 36+102+106 | 随机源三面（P1） | 原版 = **开局预烧 100 + 下标取**；我们流式 ⇒ 洗牌/随机部署/RandomizedOnResolution 分布全不同。**一次改完，别分三次** | 🔴 改的是全局随机源 ⇒ 全部对局指纹变、旧录像失配（要用户知情）；也是 55③ 的前置 |
| 5 | 99 | `HasCurrentTrait` (P1) | 残骸闸缺失 ⇒ 残骸「看得见卡面 trait」；池内 buffType 90 有 **2 张**可达 | 🔴 改 `Has` 语义 ⇒ 全部读点（AI/目标/光环）都要对拍 |
| 6 | 54 | `BroadcastWhileInPlay` (P1) | **接线口已备好**（`Aura.RefreshInPlayNumericAndKeywords` 零副作用已修）⇒ 只差打开 + 基线裁定 | 打开后 BL18/UM18 事件电池各 +4 ⇒ 要调度台判「是否重钉基线」 |
| 7 | 51 | `BroadcastCardDrawn` (P1) | 三处行为差异（缺预调 / 手牌跳集合错 / 棋盘跳多待死闸），池内 2 张监听卡可达 | 中：会改 Draw 事件消费面；原版第 4 跳漏 `!` 的 bug **照抄不修** |
| 8 | 10 | `AdjustCardEffect` (P1) | 多目标 buff 现在**重跑 N 遍**而不是「算一次 × multiplier」⇒ addTrait+stackable 会挂 N 条实例 | 🔴 动所有多目标 buff 卡 ⇒ 全池对拍；别碰已折好的 PerCount/buffMana |
| 9 | 107 | `GetNextSlotCheckingAdjacents` (P1) | 函数已建好三类支但**没接线** ⇒ AI 部署永远走最外一格（原版会贴「相邻效果」单位） | 中：改 AI 部署 ⇒ 指纹变；有符号↔绝对格号别写反 |
| 10 | 47 | `BotConfig.GetBotDecks` (P1) | 机器人现在按阵营池乱凑，原版从**预组池随机挑**；本地 `prebuilt_decks.json` 103 副数据齐 | 中：对手牌组变 ⇒ 练习局指纹变、旧回放分叉 |

> 紧随其后的 5 条（第 11–15）：87 `ShouldShowTargetsAvailable`（消费点没接线，1,111 张卡的提示行受影响）· 109 `HandCardMovedToNewIndex`（只差教程指针一句）· 850 `RemoveCostEffects`（洗回后减费复活）· 450 `OnApplicationQuit`（联机退出不发投降）· 782 `HeroLandIntoField`（督军直接出现在格里）。

---

## §4 施工依赖图（本节数字 = §5 总表序号；同一批必须一起改的）

- **广播口一族**：26 → 27 → 51 / 52 / 53 / 54（同一套「场上 / 当前回合方手牌 / 另一侧手牌」两跳·三跳结构，先做 26 定骨架）。⚠️ 51 的原版第 4 跳漏 `!` ⇒ **照抄不修**。
- **随机源三面**：36 + 102 + 106（一次改完，别分三次；55③ 依赖它）。
- **按卡撤队列 + 摘效果一族**：238 / 239 / 45 / 47 / 481 / 532（一条链；`shouldIgnoreDestroy` 别漏）。
- **溶解↔复原 + 状态行一族**（表现）：63 / 72 / 94 / 95 / 806 / 908 / 972 / 938（含「原地翻回」vs「重建对象」的架构修正）。
- **技能图标动画**：41 / 46（同一层，别各写一套）。
- **音效队列**：42 / 43（一条 FIFO，入队 + 出队）。
- **教程点击推进**：12（改严闸）/ 108 / 109 / 13（谓词无消费者、只注释）。

---

## §5 114 条总表

> 列：`# | 方法 | 一句话缺什么 | 目标文件 | 建议等级`（逐条的「原版步骤 / 施工步骤 / 怎么验 / 风险」在 `判据/B9_剩余C_施工单.tsv`）

| 1 | GetDefensiveAttackValue(CardScript actingCard, CardScri… | 整档缺失：AiScoring 没有「督军互砍 ⇒ 0 分」这一档；原版本方法 0 调用点（全树 grep 为空）⇒ 两侧今天都不可观测 | RuleEngine/Data/AiScoring.cs（ScoreFromAttack 同族） | P2 |
| 2 | ScoreFromManaBuff(BattleManager manager, CardScript thi… | 后半段（⑥⑦）缺：EffectSpec/CardDef 上**没有主动技费用字段**（原版 ActiveAbility.manaCost +0x14）⇒ 只有手牌那一段（⑤） | RuleEngine/Data/AiScoring.cs + RuleEngine/Core/C… | P2 |
| 3 | ShouldPlayComboCard(BattleManager manager, ScoringCrite… | 整档缺失：AiScoring 无 combo 判据；且池内 combo 卡 0 张、trait 1130 无实现路径 ⇒ 今天不可观测 | RuleEngine/Data/AiScoring.cs | P2 |
| 4 | SuicideHeroAttack(CardScript actingCard, CardScript tar… | 整档缺失：AI 督军对砍只靠 ScoreFromAttack 的伤害分自然表达；原版本方法 0 调用点 ⇒ 今天不可观测 | RuleEngine/Data/AiScoring.cs | P2 |
| 5 | BotConfig.GetBotDecks() | 机器人不用预组池：foeDeck==null 时我们走 `DeckBuilder.StarterDeck` 按阵营池自动凑（BattleDriver.cs:12660 ResolveDeck → :12749 … | CardPresentation/Battle/BattleDriver.cs（ResolveD… | P1 |
| 6 | BotConfigFTUE.GetBotDecks() | FTUE bot 池与胜场状态机未建（`IsInFTUEState` / `BotListFTUE` / `CurrentModeVictories` 全仓 0 实现，仅 SimpleAI.cs:2095 注释… | CardPresentation/Shell/FTUESelectDeckWindow.cs 同… | P2 |
| 7 | GetActionDelay() | 机制缺：BattleDriver.aiStepDelay = 定值 0.55f（BattleDriver.cs:2744），无区间随机；原版上下界在远端配置里（本地 0 文件命中）⇒ 值只能自定 | CardPresentation/Battle/BattleDriver.cs（aiStepDe… | P2 |
| 8 | GetEndTurnDelay() | 整项缺失：收手走同一个 aiStepDelay，没有独立档；上下界本地无数据 ⇒ 值自定 | CardPresentation/Battle/BattleDriver.cs | P2 |
| 9 | GetFirstActionDelay() | 整项缺失：无「第一个动作前的延迟」这一档；上下界本地无数据 ⇒ 值自定 | CardPresentation/Battle/BattleDriver.cs | P2 |
| 10 | AdjustCardEffect(ref CardEffect basicEffect, AbilityDat… | 除 PerCount>0 与 buffMana 外，我们仍**重跑 N 遍效果**而不是「算一次 × multiplier 挂 N 份」；三种不等价：addTrait+stackable（原版一条实例、我们 N… | RuleEngine/Core/EffectResolver.cs（AdjustCardEffe… | P1 |
| 11 | GetRefCard() | ref card 通路整条没做：没有取 `PreviousAbilityTargets[0]` 的口，也没有它服务的四档 valueOptions（100 RefCardAttack / 101 BaseRef… | RuleEngine/Core/EffectResolver.cs（数值求值口）+ RuleEn… | P1 |
| 12 | CheckIfWaitingToChangeDesiredAttack(BattleManager manag… | **不同**：我们只判 `Kind ∈ {ChangeToRanged, ChangeToMelee}`（BattleDriver.cs:16028），**不看玩家点的是哪一档、也不看是哪张卡** ⇒ 点错档也… | CardPresentation/Battle/BattleDriver.cs:16028（攻击… | P1 |
| 13 | CheckIfWaitingToClickCard(BattleManager manager, TurnSc… | 谓词本身无实现（我们靠 `TutorialScript.Matches` 的 TapCard 档 + `BattleDriver.TapBoardCard` 推进）；F5 已补轻点灵族灵魂石那一支，软锁解除 ⇒… | RuleEngine/Core/TutorialScript.cs（注释）+ CardPrese… | P2 |
| 14 | CloseChatBoxes(ScriptedAction currentAction) | 关气泡本身有等价物（BattleDriver.cs:17567 CloseChatBoxes），但**不碰音频** ⇒ 跳过后台词继续响；且代码里有一句「是否停音没核」的旧注（铁律 5 要就地订正） | CardPresentation/Battle/BattleDriver.cs:17567（Cl… | P2 |
| 15 | ExecuteAction(BattleManager manager, ScriptedActionCamp… | 6 分支全缺（战役没有执行器；`AiScripted.ExecuteAction` 对应物不存在）⇒ **依赖整条战役线**，不能单独做 | CardPresentation/Battle/BattleDriver.cs（表现四口）+ 战… | P1 |
| 16 | GetChosenCard(List<CardScript> availableUnits, RawCardS… | **故意不同**：我们找不到时不退回 `list[0]`，而是**不执行并出声**（`TutorialScript.cs:1580/:1624`；840/1091 已写成红线）—— 原版会降级到第一张 | RuleEngine/Core/TutorialScript.cs:1580 / :1624 | P2 |
| 17 | GetCurrentWaitTime(BattleManager battleManager, List<Sc… | 整条缺（战役未做）；与 GetDelay 是**两张表**（这张按音频时长算，那张按动作自带的 before/after 字段） | 战役线执行器（待建） | P1 |
| 18 | GetDelay(BattleManager manager, TurnScriptedData turnDa… | **不同**：原版每条动作都等（before/after 两个字段），我们只有聊天五档一个 waitAfter 窗口；现数据里 `waitBefore != 0` 的脚本条目 **314 条** ⇒ 差异真实可… | CardPresentation/Battle/BattleDriver.cs:17542（等待… | P2 |
| 19 | GetHorizontalOffset(PositionReference positionReference… | 我们用**卡的宽高**当偏移（BattleDriver.cs:16201 TutorialTipOffset 取 CardView.Height/Width）—— 原版用的是 `VarsDevice` 里的两个… | CardPresentation/Battle/BattleDriver.cs:16201（Tu… | P2 |
| 20 | HasPendingActions(List<ScriptedActionCampaignData> turn… | 战役线未建（无动作游标、无战役回合列表） | 战役线执行器（待建） | P1 |
| 21 | PlayNextScriptedAction(BattleManager manager, List<Scri… | 战役线未建 | 战役线执行器（待建） | P1 |
| 22 | PlayScriptedTurn(BattleManager manager, List<ScriptedAc… | 战役未做；⚠️ 别拿这一份当教程口径（教程是 TutorialScript） | 战役线执行器（待建） | P1 |
| 23 | SkippableActionWait(ScriptedAction action, float time) | **不同**：我们只有聊天五档一个 waitAfter 窗口、且只在我方回合；原版每条动作都等、不分敌我，并且带「可跳过」语义（tutorialManager.canSkip） | CardPresentation/Battle/BattleDriver.cs:17542（等待… | P2 |
| 24 | AddResetActions(CardScript targetCard, int priority, bo… | 我们做成**同步调用**（RuleCore.cs:14139 RefundForcedAttackAction，由 DeclareAttackRefundingForced :14102 在攻击结算里直接调）⇒… | RuleEngine/Core/RuleCore.cs:14139 + RuleEngine/C… | P2 |
| 25 | AlreadyPlayedTroopThisTurn(bool isPlayer) | 只有**本局**计数（BattleContext.cs:330 TroopsPlayed，写点 RuleCore.cs:3963 / EffectResolver.cs:3807）；「本回合」粒度没有落点 | RuleEngine/Core/BattleContext.cs（新字段）+ RuleEngin… | P2 |
| 26 | BroadcastCardCreated(CardScript createdCard) | 引擎里 `CardCreatedInHand` / 210 **全仓 0 命中**：没有这条广播，也没有消费口。可达性：现读 1,128 张池 `abilityTriggers` 含 210 的 = **0 张… | RuleEngine/Core/RuleCore.cs（广播口）+ RuleEngine/Cor… | P0 |
| 27 | BroadcastTrapResolved(CardScript trapCard) | 引擎里 `TrapResolved` / 270 **全仓 0 命中**：有手牌陷阱机制（`HandTrapWhens` / `Jammed Communications`），但**没有「一张陷阱结算 ⇒ 广播… | RuleEngine/Core/RuleCore.cs + RuleEngine/Core/Ef… | P0 |
| 28 | CancelAbilityTargeting() | 关面板本身有等价物（BattleDriver 的 SetActing/ClearSelection），但**顺序不同**：原版成功结算那一支是 `FinishActing`（白层 0.3s 褪）**早于**面板… | CardPresentation/Battle/BattleDriver.cs（DoResolv… | P2 |
| 29 | CancelAnimFromUnitFromQueue(CardScript removeAnimdUnit,… | 我们只有一条 `_timeline`（ScheduleAction / ScheduleVfxCue / SchedulePresentationCallback 三种 cue 共用一个泵），**没有「按卡撤掉… | CardPresentation/Battle/BattleDriver.cs（_timelin… | P2 |
| 30 | CancelAnimsFromUnit(CardScript removeAnimUnit) | 原版有 **4 条动作队列**；我们只有 1 条 `_timeline` ⇒ 既没有「4 队列」的划分，也没有按卡撤队列 | CardPresentation/Battle/BattleDriver.cs（_timelin… | P2 |
| 31 | ClickExitBattle() | 缺「回放 / 教程回放 ⇒ 直接离开」那一支：`Forfeit()`（BattleDriver.cs:12031-12099）只有 `Ctx==null` / `Ctx.IsOver` 两道早退 ⇒ **回放局… | CardPresentation/Battle/BattleDriver.cs:12031（Fo… | P1 |
| 32 | DisplayBigChat(string chatMsgText, CardScript talkingUn… | 整层未做：教程动作 PlayerChatBig(55) / AiChatBig(65) 落到我们这里只走普通聊天（同族 PlayerChat/AiChat），没有大框那一层。数据现读 6 关**一条都没有**（… | CardPresentation/Battle/BattleBigChat.cs（新）+ Bat… | P2 |
| 33 | DisplayBigChat(string chatMsgText, Sprite talkingUnitSp… | 与 294 同一条（大框那一层整层未做） | CardPresentation/Battle/BattleBigChat.cs（新） | P2 |
| 34 | ExecuteActiveAbility(CardScript actingCard, CardScript … | 同步 vs 异步：原版那一拍是**入队**，与面板收起之间隔着白层 0.3s 褪色 + FadeOut；我们 `DoResolve` 同步结算 ⇒ 这个「关面板窗口」不存在 | CardPresentation/Battle/BattleDriver.cs（主动技结算 + … | P2 |
| 35 | GetGameState() | 全仓 `grep GetGameState` 0 命中：没有「双方督军数据 + 出场/手牌/牌库」拼一条状态串的出口（战报/调试用途） | RuleEngine/Core/RuleCore.cs（GetGameState）+ RuleE… | P2 |
| 36 | GetRandomInt(int minValueIncl, int maxValueExcl) | **口径不同**：我们用 `System.Random` 流式 `ctx.Rng.Next`（BattleContext.cs:378）⇒ 分布/序列与原版不同；影响 `ShuffleDeck` / `GetR… | RuleEngine/Core/BattleContext.cs（随机源）+ RuleEngin… | P1 |
| 37 | HideHeroAtStart(bool isPlayer) | 我们开局督军**直接以实体出现** ⇒ 少了「从溶解态浮现」这一拍（`CardFeel.DeathDissolveMinion` 那套溶解没有用在开局） | CardPresentation/Battle/BattleDriver.cs（开场链）+ Ca… | P1 |
| 38 | LogActivatedTrigger(bool isPlayer, AbilityTrigger trigg… | 全仓 `grep LogTriggerAbility` = 0 命中：只记了对局结束那一跳（LogMatchEnd），**触发级流水缺** | RuleEngine/Core/RuleCore.cs（触发记账点）+ CardPresenta… | P2 |
| 39 | MobileAdjPos(Vector3 pos) | `VarsDevice` 本工程**没有对应类**（Core/ScreenUtilities.cs:28 记「零消费者」）⇒ 这条 y 偏移整条没做 | CardPresentation/Core/ScreenUtilities.cs + Resou… | P2 |
| 40 | OnApplicationQuit() | 全仓 `grep OnApplicationQuit` = 0 命中（只有 `OnApplicationPause` :32802，那是时钟补偿）⇒ **退出游戏时不发投降** ⇒ 联机局对面要等掉线判定才收场 | CardPresentation/Battle/BattleDriver.cs（新增 OnApp… | P1 |
| 41 | PlayIconAnim(CardScript card, bool waitForAnim, Ability… | 整层缺：全仓 `grep ActivateIconAnim` = 0 命中；技能钮只有静态贴图 + 亮/灭（ArmyIcons / activeAbilityIcons），没有「播图标动画」 | CardPresentation/Battle/AbilityIconAnim.cs（新）+ R… | P1 |
| 42 | PlayNextSoundInQueue() | `Battle/BattleSfx.cs:50` 是**立刻播 + 按 cue 名去重**（_lastAt / timeToPlayAgain），**没有队列** ⇒ 同一拍多条音效会叠在一起（原版串行） | CardPresentation/Battle/BattleSfx.cs | P2 |
| 43 | PlaySoundAsset(SoundAsset_OLD soundAsset) | 同 461：`BattleSfx` 没有 `soundsToPlay` 队列，我们是「立刻播」 | CardPresentation/Battle/BattleSfx.cs | P2 |
| 44 | RemoveEnchantment(CardScript card, CardEffect enchantme… | 缺「入队」这一层：`Enchantment.cs:269 Remove` / RemoveByCard / RemovePray 都在调用点**同步**摘除 ⇒ 排序语义不同（原版排在同批其它动作之后）；另 R… | RuleEngine/Core/Enchantment.cs:269 + RuleEngine/… | P2 |
| 45 | RemoveSelfBuffsFromQueues(CardScript card) | 全仓 `grep CancelEffectsFromDisappearingUnit` = 0 命中：同 238/239 那一族，缺「按卡撤队列」+ 这一层 `shouldIgnoreDestroy` 语义 | CardPresentation/Battle/BattleDriver.cs（_timelin… | P2 |
| 46 | ResolveAbilityIconAnim(BattleAction battleAction) | 同 460（技能图标动画整条没做） | CardPresentation/Battle/AbilityIconAnim.cs（新）+ B… | P2 |
| 47 | ResolveRemoveEffect(CardScript targetCard, CardEffect c… | 我们只做了第一段（UnitState.RevertGrantsFrom + Enchantments.Remove 分担）与第四段的等价物；②（四次撤队列）③（RemoveDestroyableAnims）没有 | RuleEngine/Core/Enchantment.cs + RuleEngine/Core… | P2 |
| 48 | SetupWarlordsLifeAndEnchantment() | ① 那半有（RuleCore.cs:601 BuildPlayer，:634-640 改 MaxHealthBase 并回满）；② `playerMaxLife` / `enemyMaxLife` 覆盖**没有… | RuleEngine/Core/RuleCore.cs:601（BuildPlayer）+ Ru… | P2 |
| 49 | ShouldUseEnviromentalEffects() | 我们是**反过来的**：BattleDriver.cs:8870 `BeginOffensivePhaseIfAny` 按既有 §25 无条件弹「选进攻卡/防御卡」面板 + AI 随机挑 ⇒ 玩家会看到一个原版… | CardPresentation/Battle/BattleDriver.cs:8870（Beg… | P1 |
| 50 | TryUsingWaystone(CardScript actingCard) | ③ UIstate 闸 2026-10-26（B8_A）已落地；④「哪张残骸能收」原版判据 = `rawCard.cardArmy(+0x2c)==30(SaimHann)` ∧ `isRemnant`（**不… | RuleEngine/Core/RuleCore.cs（Waystone 收集）+ CardPr… | P2 |
| 51 | BroadcastCardDrawn(BattleManager manager, CardScript dr… | 三处差异：① 没有预调 ⇒ 被抽那张收不到自己的 Draw；② 我们手牌两跳 = 当前→**对手**（原版当前 2 遍、对手 0 遍）⇒ 对手手牌会误响、当前只响 1 遍；③ 我们棋盘跳加了「待死」闸，原版没有… | RuleEngine/Core/EffectResolver.cs:9546-9677（三跳广播… | P1 |
| 52 | BroadcastCollectFaith(BattleManager manager, CardScript… | 已收窄到 `boardAndPersistentOnly`，不再扫双方手牌；仍差两处结构：① 我们 subject **没有预调**（原版先对收集者自己跑一次、棋盘循环再排除它）⇒ 顺序不同；② 原版棋盘跳不看… | RuleEngine/Core/EffectResolver.cs:12688/:12754 +… | P1 |
| 53 | BroadcastRemoveFromGame(BattleManager manager, CardScri… | 整条缺：① 没有「移出游戏」这个状态（cardState 11 全仓只有注释）；② 没有 RemoveFromGame 调用点；③ 按来源撤 extrinsic 只做一半（ResolveJamUnit + Re… | RuleEngine/Core/RuleCore.cs + RuleEngine/Core/Un… | P2 |
| 54 | BroadcastWhileInPlay(BattleManager manager, bool unitJu… | 仍差两块：① 数值光环只刷新了 `MeleeEqualsHealth` 那一趟，其余 `Aura.Recompose` 仍只在 9 个棋盘写入点 + BeginTurn 手工挂 ⇒ 「非棋盘变动影响光环」会停在… | RuleEngine/Core/ActionPump.cs:118（接线）+ RuleEngin… | P1 |
| 55 | MeetsCriteria(BattleManager manager, BoardCriteria boar… | 四条明确缺口：③ RandomChance(8) 掷法未实现（false 且真命中 8 ⇒ null + 出声）；④ maxTargets 未截断；⑤ playerTurn 缺 ctx/owner 时返回 nu… | RuleEngine/Core/CountEval.cs:234（MeetsBoardCrite… | P0 |
| 56 | AddEnchantment(CardEffect newEffect) | 第五步（⑥ 激活附魔带来的 trait）缺；且缺「附魔卡 → 关键词」数据通路。可达性：附魔载荷 132 条里 37 条非零、涉 33 张卡，但**全池附魔载荷无 fast/flank/ferocity/oat… | RuleEngine/Core/Enchantment.cs:190-268（Attach 尾部… | P2 |
| 57 | AlreadyContainsEffect(CardEffect effect, bool heroCheck… | `IsSameHandEffect`（EffectResolver.cs:5488-5523）比原版少两维 ⇒ 「同 buffType + 同施放者 + 同到期、但来自两份不同副本的同一附魔卡」会被判成同一条、… | RuleEngine/Core/EffectResolver.cs:5488-5523（IsSa… | P1 |
| 58 | CheckForSkullEarned() | 缺三件：① 督军侧 `earnedSkull` 计数（`DeckRules.SkullsFor` 只是纯函数，没运行时记账）；② 血量变化后的检查点；③ 广播（只发场上）。阈值 20/10/0 与原版 `Get… | RuleEngine/Core/RuleCore.cs（血量写点）+ RuleEngine/Co… | P1 |
| 59 | CheckMeetsCombination(TraitCombination traitCombination… | 整条未做：全仓 `TraitCombination｜Combination` 0 命中；且我们的印记表示是 `darkpact` 代理键，不是 markOfKhorne/Nurgle/… ⇒ 做之前要先统一这一… | RuleEngine/Core/EffectResolver.cs + RuleEngine/C… | P2 |
| 60 | CheckPossibleCombinations() | `ApplyOneGain` 末尾没有这一步 ⇒ 组合永不触发（同 718） | RuleEngine/Core/EffectResolver.cs（AddEffect 尾部） | P2 |
| 61 | CheckStun() | 缺「已带 stun 的单位后来获得 unstunnable ⇒ 立刻摘 stun」这一支（变回单位那一刻）。我们有施加前拦截（RuleCore.cs:5021-5030）和回合末摘 stun（:2616-261… | RuleEngine/Core/EffectResolver.cs（trait 获得链）+ Ca… | P2 |
| 62 | ClearPendingDamage() | 我们没有 pendingDamage 台账：判死用 `EnoughPendingDamageToDie` 现算（RuleCore.cs:7536）⇒ 行为上等价于「恒已清空」，但没有可指的落点 | （建议不落点）RuleEngine/Core/RuleCore.cs:7536 注释里写「原版两… | P2（建议改 B） |
| 63 | DissolveMinionCard(bool dissolveY) | 明确缺陷：缺「溶回来（1→0）」那一支。我们回手走 `ReturnToHand` 的反向 clip 时间轴（另一套机制）⇒ 可见面 = 「卡溶到一半又回手」那一帧停在半溶 | CardPresentation/Core/CardFeel.cs:3133-3160 + Ca… | P1 |
| 64 | DoUnitShake(int shakeScale, float duration) | 原版抖位置（DOShakePosition，13 段）；我们这张表那一档走 **DOShakeScale**（UnitTweenTable.cs:495-505），位置抖另有镜头抖 ShakeCameraFor… | CardPresentation/Core/UnitTweenTable.cs:495-505 … | P2 |
| 65 | EnoughPendingHealingToFull() | 预判型谓词未建（我们没有 pendingHealing 台账；治疗由 `HealUnit` 的 `min(请求,缺口)` 逐次回答）⇒ 没有「这次多目标治疗够不够回满」的预判口 | （建议不落点）RuleEngine/Core/RuleCore.cs（HealUnit 注释）+… | P2（建议改 B） |
| 66 | GetQuestTimes() | 全仓没有「取第一条 questTimes > 0」的取值口；`Enchantment.FilterByQuestCounter`（:420）是「逐条累计 + 比 questTimes」的另一件事 | RuleEngine/Core/Enchantment.cs（新增取值口） | P2 |
| 67 | HasCancelAttackEffect(CardScript actingCard, CardScript… | `Enchantment.FindCancelAttack`（:576）只判「附魔卡有没有 AboutToAttack 正文」，**没有** CanTriggerAbility 的那几条 ability 级闸（… | RuleEngine/Core/Enchantment.cs:576（FindCancelAtt… | P2 |
| 68 | HasWhileInPlayAdjacentEffect() | **我们已实现且口径一致**：`UnitState.cs:2429 HasWhileInPlayAdjacentEffect(out why)` ① requiem(760) ⇒ true、② 无 WhileI… | RuleEngine/Core/UnitState.cs:2429（只补注释） | P2（建议改 A） |
| 69 | HeroLandIntoField(bool showStats, float waitMult) | 8 段只落了 1 段（BattleDriver.cs:25419-25428 只有 FadeAllTraitsIcons 一句）⇒ 督军「直接出现在格里」：无弹性缩放、无 LandOnGround、无淡入、无等… | CardPresentation/Battle/BattleDriver.cs（督军入场）+ C… | P1 |
| 70 | MinionPlayedIntoField(float delay, int unitInPlayCounte… | 时序逐值对上（handoff 0.4s）；四处差异：① orderInPlay 原版写**召唤实参号**、我们用自增 PlayOrder；② summonAnimDelayed 与 m_OnDisplaySum… | CardPresentation/Board/DeploySequence.cs:75-101/… | P2 |
| 71 | MissActions() | 逐句 3 句全缺：不 +1 攻击配额、不告警、不区分状态 | RuleEngine/Core/RuleCore.cs（回合结算）+ RuleEngine/Co… | P2 |
| 72 | MoveBackToHand() | 明确缺陷（3 句 + 1 个缺失方法）：① SetSortingLayer(手牌层) ② StopAnimsInPlay 在回手链上无对应调用 ③ `UnDissolveWholeCard` 全仓 0 命中（见… | CardPresentation/Core/CardFeel.cs + CardPresenta… | P1 |
| 73 | OnTurnStarted(bool playerTurn) | 我们**根本不发 160**（全仓 0 命中）⇒ 所有「回合开始时」监听卡收不到 | RuleEngine/Core/RuleCore.cs（BeginTurnCore）+ Rule… | P2 |
| 74 | OutFromPlayingAreaAnim() | 分步形状不同：原版一个方法包办 4 步，我们由 ReturnToHand + 手牌重排分担；但第 4 步 LightPlayableHand 在手牌链上**没有等价入口**（我们的可打高亮由 BattleDri… | CardPresentation/Battle/BattleDriver.cs（回手链 + Re… | P2 |
| 75 | ReactToUnitPoisoned(CardScript poisonedUnit, CardScript… | 既不广播 290、也无监听端（池内 290 监听 = 0 张，3 个直调点 VA 现扫） | RuleEngine/Core/EffectResolver.cs（加毒链）+ RuleEngi… | P2 |
| 76 | RecordPendingDamage(DamageInfo newDamage) | 我们伤害**立即落地**、没有 pendingDamage 窗口 ⇒ 所有「攻击结算前读 pending」的消费者（原版 8 处）在我们这里没有对应物 | （建议不落点）RuleEngine/Core/RuleCore.cs（伤害管线注释） | P2（建议先查再判） |
| 77 | RecordPendingHealing(HealInfo newHeal) | 同 840（没有待治疗窗口；治疗由 HealUnit 立即结算） | （建议不落点）RuleEngine/Core/RuleCore.cs（治疗管线注释） | P2（建议先查再判） |
| 78 | RemoveAttackThisTurn() | 2026-10-26 已实现「退还」（B8_BH）并**数据化**（B8_BM：只有 231/237/238 三族退还）；剩余是「额度上限/边界」：原版只判 `>0` 才减（不会变负），要与我们的 `Recom… | RuleEngine/Core/RuleCore.cs:14139（RefundForcedAt… | P2 |
| 79 | RemoveCardFromCemetery() | 我们没有这个中间态（墓地操作不经过「正在取出」标志）⇒ 行为上等价于无操作，但没有落点 | （建议不落点） | P2（建议改 D） |
| 80 | RemoveCostEffects() | 同一实例「弃掉 → 洗回牌库 → 再抽到」时旧费用修正会**复活**（RuleCore.cs:13675-13692 洗回不做重置） | RuleEngine/Core/RuleCore.cs:13675-13692（洗回）+ Rul… | P1 |
| 81 | RemoveEffect(CardEffect removedEffect, bool fromJam) | 我们按关键词/来源摘、原版按记录摘；permanentBuff 闸只在附魔层。真正无落点的是 **5 / 45 / 40** 三档 + case3 的 dropPod(230) 与 610 两条特判 + cas… | RuleEngine/Core/UnitState.cs:1216-1295/:1371-140… | P2 |
| 82 | RemoveInHandEffects() | 我们没有这种视图件（没有 reflectedFrame 图层）⇒ 无落点；若本工程也不需要那个表现，则该行可判 D | （待核）CardPresentation/Core/CardView.cs | P2（待核后可能 D） |
| 83 | SetAsCemetery(int indexInCemetery) | 位置与缩放两半逐句同（(−8.14,−0.2,0) · 0.65 绝对值 · 0.3s · OutCubic）；缺 3 句：SetSortingLayer / SetSortingOrder(墓地序号) / S… | CardPresentation/Battle/BattleDriver.cs（墓地链） | P2 |
| 84 | SetCardState(CardStateOptions newState) | 我们没有 cardState 字段：视图状态由「在哪张表里/哪个布局」直接驱动（架构选择）⇒ 缺这个状态机（与 SetStateAsMulligan / SetupAttack 同族） | （建议不落点） | P2（建议改 B） |
| 85 | SetEnemySecret() | 我们只有卡背/伏击面一档，**没有秘密面图层** ⇒ 观感不同（敌方密令牌在原版显示秘密面，我们显示卡背） | CardPresentation/Core/CardView.cs（CardFace 枚举 + … | P1 |
| 86 | SetStateInHand() | 原版这条会把「由谁生成」那一行显示出来（`SupportMethods.GetCreatedByText` + `Battle/HUD/CreatedBy` 词条）；我们**完全没有这一行** ⇒ 生成的卡看不… | CardPresentation/Core/CardView.cs（新行）+ RuleEngin… | P1 |
| 87 | ShouldShowTargetsAvailable(bool inPlay) | 判据已落地成函数，但**消费点没接线**：`SkillPanel.SetTargetsSuffix` 的 targetsAny 由 `BattleDriver.ShowSkillPanel` 传入，而它今天不传… | CardPresentation/Battle/BattleDriver.cs（ShowSkil… | P1 |
| 88 | ShowCardBeingDrawn(float stepTime) | 整条路径不存在（弃牌/偷牌揭示那条路没有）：① `inHandMoving(9)` 档（视图不存 cardState）；② `SetSortingLayer(HandMoving)`（等价物 QHandMovi… | CardPresentation/Core/CardFeel.cs:3316-3357/:339… | P2 |
| 89 | StopAnimsInPlay() | 没有对应口 ⇒ 起播之后只能等它跑完（`safeResetOnEnd` 那一档除外） | CardPresentation/Core/CardFeel.cs + CardPresenta… | P2 |
| 90 | ToggleCombatState(bool state) | 我们没有 combatState 标记，也没有 CardHighlight 的战斗高亮那一族 ⇒ 无可比物（原落点那条是误配） | CardPresentation/Battle/BattleDriver.cs（高亮状态机）+ … | P2 |
| 91 | TransformFromRemnant(Action onComplete) | 原版是**原地翻位**（同一对象、清两标志位）；我们**换对象**（新建 `UnitState` 从卡模板重建 ⇒ 关键词、攻血全部回来）。净结果「不再是残骸 + 属性回到卡面」相同，但重建会丢该卡的运行时状态… | RuleEngine/Core/UnitState.cs + CardPresentation/… | P1 |
| 92 | TransformUnitInPlayToCard() | 状态机 3 个 state 逐句对过；差异只有末尾 `ResetTeleportCardOutOfBattle` 我们没有对应调用 —— 我们的传送只走 PlayCard 那一支，不回手 | CardPresentation/Battle/BattleDriver.cs（传送链）+ Ca… | P2 |
| 93 | TriggerOtherCardSpiritStone(CardScript spUnit) | 口径不齐（已确认仍在）：`SpiritAbility` 不在 `triggers:` 族 ⇒ `SelfExcludesOnKeywordTrigger` 返回 false ⇒ **我们未自排除**；原版有自排… | RuleEngine/Core/WhenEvent.cs:1503-1514/:1713-172… | P2 |
| 94 | UnDissolveWholeCard() | 原版有「溶解 ↔ 复原」一对；我们只有溶解那一向，复原靠**重建视图**（CardView.Create）绕过 ⇒ 语义近似、机制不同（重建会丢该卡的运行时状态） | CardPresentation/Core/CardFeel.cs + CardPresenta… | P1 |
| 95 | UndissolveToDeck() | 缺「淡入」（无 un-dissolve）⇒ 这一整条走不通（洗回牌库那一路没有对应演出） | CardPresentation/Battle/BattleDriver.cs（洗回链）+ Ca… | P1 |
| 96 | UpdateAttackText() | 🔴 2026-10-26（B8_N）订正：原版 `AttackTypeChanged` 语义 = 「逐个高亮档位」而不是「切显示格」；我们按旧理解实现（切显示）⇒ 至少高亮那一族要补；且 isPlayer 为假… | CardPresentation/Core/CardView.cs（攻击文本/高亮）+ Card… | P1 |
| 97 | GetTraitForActiveAbilityRawCard(CardArmy cardArmy) | 全仓无「按阵营推主动能力 trait」这一层（`grep GetTraitForActiveAbility` 0 命中）⇒ 池内 77 张 hasActiveAbility 里有 76 张自带那条 trait，… | RuleEngine/Core/CardDef.cs（推导）+ CardPresentation… | P2 |
| 98 | Raise() | 我们**无等价事件**（PlayCardCore 扣费段 RuleCore.cs:3900-3905 与 SpendNormalMana :12720-12747 都不发事件；EvtKind 里没有 Energ… | RuleEngine/Core/RuleCore.cs:3900-3905/:12720-127… | P2 |
| 99 | HasCurrentTrait(DefinedTrait trait) | 缺残骸闸 + 缺「印刷那份 / 授予那份」双簿：`:1068` 那条（残骸不摘 trait）让「残骸 Has(任意卡面 trait)」两侧一个真一个假；buffType 90 池内 2 张（SAU36 + ea… | RuleEngine/Core/UnitState.cs:942-945/:1068/:1090… | P1 |
| 100 | HasEnchantmentWhileInPlay() | 原版判的是 **`whileInPlayAnim`**（RawCardScript 的表现开关序列化字段）；我们 `Enchantments.HasAny`（:464）只判「身上有没有任何附魔」⇒ 语义完全不同… | RuleEngine/Core/Enchantment.cs（新增）+ RuleEngine/C… | P1 |
| 101 | HasSecretOrder(RawCardScript enchantment) | `FindSecretOrder`（Enchantment.cs:547）**没有 ② 那道闸** ⇒ 身上没有任何 SecretOrder 附魔、但 ActiveEffects 里恰有一条同卡的附魔型效果时，… | RuleEngine/Core/Enchantment.cs:547（FindSecretOrd… | P1 |
| 102 | GenerateRandomNumbers() | 机制不同：原版开局预烧 100 个 + 按下标取（同局确定、但**不可由种子复现**）；我们每次 `Next` ⇒ 分布不同。与 375/106 是同一件事的三面 | RuleEngine/Core/BattleContext.cs + RuleEngine/Co… | P1 |
| 103 | GetBotConfigMaxSkips() | 原版读配置两份；我们 `SimpleAI.KnobsOf`（:1436）按 `AiDifficulty` 四档自定（MaxSkips 3/2/1/0，🔶 我们挑的） | RuleEngine/Data/SimpleAI.cs:1436（KnobsOf） | P2 |
| 104 | GetBotConfigMinSkips() | 原版读配置；我们恒 0（理由见 KnobsOf doc：正数会把只剩一两条动作的 AI 砍光）⇒ 🔶 自定 | RuleEngine/Data/SimpleAI.cs:1436（KnobsOf） | P2 |
| 105 | GetBotConfigSkipChance() | 原版读配置；我们按难度自定（90/60/30/0）⇒ 🔶。口径复核：原版是整数转 float 再与 `GetRandomInt(0,100)`（[0,100)）比，我们 `AiRng.Next(0,100)` … | RuleEngine/Data/SimpleAI.cs:1436（KnobsOf） | P2 |
| 106 | GetRandomInt(int minValueIncl, int maxValueExcl) | 同 375：**预烧数组 + 单调下标**（同局完全确定）；我们每次 `Next` ⇒ 形状不同。另：原版数组空时**返回 0**（不重掷/不报错），我们没有这一档 | RuleEngine/Core/BattleContext.cs（RandomInt） | P1 |
| 107 | GetNextSlotCheckingAdjacents() | 原版返回**有符号格号**、我们返回绝对格号（`SlotOf(Left,i+1)/SlotOf(Right,i+1)`）；且**调用点缺失**（AI 部署落点仍走 `NextFreeWithoutDisplac… | RuleEngine/Core/BoardSlots.cs:570-583 + RuleEngi… | P1 |
| 108 | DisplayHandMinusCard(List<CardScript> cardList, CardScr… | 机制差异：原版起拖那一帧做「无洞、N-1 张间距」，下一帧起换 `DisplayHandWithCardSpace`（带洞）；我们起拖即走带洞布局（HandLayout.Refresh slots=n+1）⇒ … | CardPresentation/Hand/CardInteraction.cs:871-952… | P2 |
| 109 | HandCardMovedToNewIndex(CardScript card, int newIndex) | 列表语义已逐句对上；三处形状差：① 原版 `newIndex == IndexOf` 时不动列表、我们仍 Remove+Insert（净结果同）且**多发一次 SendCardReorder**；② 原版 `C… | CardPresentation/Hand/CardInteraction.cs:1438-14… | P1 |
| 110 | HideHand() | 全仓 `grep HideHand` = 0：没有「把当前手牌整体 SetActive(false)」的入口（手牌视图由 SyncHand 按需增删） | CardPresentation/Battle/BattleDriver.cs（手牌状态）+ C… | P2 |
| 111 | PlaySentinelAnims(BattleAnims battleAnims) | 三条都没有对应件（池内 sentinel 特性卡、`sentinelDeckAnim` prefab、`activateSentinel` cue 只出现在 BattleSfx.cs:36 的注释里 —— ⛔ … | CardPresentation/Battle/BattleDriver.cs + Battle… | P2 |
| 112 | IsCumulativeMutation() | 整套机制缺失；三重不可达：① 全量 SO 1,351 张里 `mutationType` 13,012 次、**值全 0**；② `spellType == 130 (Mutation)` **0 张**；③ … | RuleEngine/Core/CardDef.cs（字段）+ 工具/gen_cards_tru… | P2 |
| 113 | TriggerEnchantment(BattleManager manager, CardScript ac… | 原版是**两次**派发（0 与 500），我们两跳都没有 ⇒ C。⚠️ 原账落点指 `EffectResolver.cs:7103`（TN_OtherCardDrawn）= 指错方法（那是 260 的触发名，与… | RuleEngine/Core/EffectResolver.cs（新增触发口）+ RuleEn… | P2 |
| 114 | OnTroopKilled(TroopKilledSignal obj) | 缺「我方击杀的敌方部队数」计数源；原版按 killer 判归属，我们**连 killer 都没存**（`BattleContext.DeadUnit` 没有 Killer 字段，AchievementTally… | RuleEngine/Core/BattleContext.cs（DeadUnit 记录）+ R… | P2 |
## §6 判据拿不到的（如实，写清还差什么）

| 条目 | 拿不到的部分 | 还差什么 |
|---|---|---|
| 32/33 `DisplayBigChat` ×2 | **真正的显示调用**在 `.c` 与 ILSpy 体里都读不到（两份体都只剩「空文案 LogWarning / 空单位 LogError」+ 取 `fullHeroSprite`）；版面参数（位置/字号/遮罩）全缺 | 场景树里那颗「大聊天框」节点（`资料/说明书/01_战斗_对战/*`）+ prefab 字段 |
| 84 `SetCardState` | 体只有「Log + 写 `cardState`」= **18 档状态机的写入口**，各档对应的显隐/可点/动画**不在这个方法里** | 18 档各自的消费者（要逐个读 `cardState ==` 的读点） |
| 28 `CancelAbilityTargeting` / 34 `ExecuteActiveAbility` | **成功结算支**的 `FinishActing` 发起处未核（`.c` 只在警告/中止两支出现） | 成功支那条协程的调用点（VA 现扫） |
| 69 `HeroLandIntoField` | `showStats` 控制的**四组 SetActive**（`.c:87-338`）未逐句解；只到「8 段骨架」 | 那 250 行的分支逐句解 |
| 94/95 `UnDissolveWholeCard` / `UndissolveToDeck` | 复原的**曲线/时长参数**只读到 `globalVars+0xb0` 与 `ceil(×常数)`；常数未解 | `.c` 常量（`工具/read_literal.py`）+ 场景树 |
| 39/102 `VarsDevice`（`targetYOffset` / 横偏移） | 本工程**没有 `VarsDevice` 载体**，`VarsDesktop/VarsMobile` 两份 SO 未导入 | 解包 assets 里那两份 SO（取不到 ⇒ 值只能 🔶 自定） |
| 7/8/9/103/104/105 AI 延迟与 skip 三旋钮 | 原版上下界在**远端** `AIBotsConfig.PlayBehaviourConfig` / `NormalBotConfig`（本地 0 文件命中） | 远端配置；现在只能「机制照原版、值 🔶 自定」 |
| 6 `BotConfigFTUE.GetBotDecks` | FTUE bot 池 + 胜场表（远端；`BotListFTUE` 全仓 0） | 远端 + 教程线状态机 |
| 112 `IsCumulativeMutation` | `cards_truth.json` **不带** `mutationType`（真值只在 `cards_raw.json` 的 SO 原值里） | 先扩数据生成器列才能实现 |
| 82 `RemoveInHandEffects` | **已核（2026-10-26 C2）** ⇒ 判 **D**（见 §7）：`reflectedPrefab` 是空 PPtr、原版从不实例化 | — |

---


## §7 我们其实是「口径不同 / 不该记 C」的（列出来由调度台裁）

| # | 方法 | 建议 | 证据 |
|---|---|---|---|
| 68 | `HasWhileInPlayAdjacentEffect` | ~~改 A~~ **已落 A**（调度台 `_parts/B9_DESK2.tsv`） | `UnitState.cs:2429` 已逐句同原版（requiem 760 ⇒ true / 无 WhileInPlay ⇒ false / ability 级 `targetsAffected`）；原版 **ability 级 `ta==100` 行 = 0**（17 行全在 logic 级）⇒ 两侧都恒 false，不是缺口 |
| 62 | `ClearPendingDamage` | ~~改 B~~ **已落 B**（调度台 `_parts/B9_DESK2.tsv`） | 原版「累加待伤 → 到点清空」两段式；我们现算判死（`RuleCore.cs:7536`）⇒ 行为等价「恒已清空」，缺的是实现结构不是行为 |
| 65 | `EnoughPendingHealingToFull` | ~~改 B~~ **已落 B**（同上） | 谓词本身由 `HealUnit` 的 `min(请求,缺口)` 逐次回答 ⇒ 我们不需要它；它真正的消费者（`damaged` 过滤器）归 841 那一行处理 |
| 79 | `RemoveCardFromCemetery` | ~~改 D~~ **已落 D**（带时间限定） | 原版全方法 4 行，只把 `+0x24b`（正在从墓地取出的 UI 闸）清 0；我们墓地操作不经过该中间态 ⇒ 无行为可复刻 |
| 84 | `SetCardState` | ~~改 B/D~~ **已落 B**（裁定：实现结构不同、行为等价 ⇒ B 最诚实，⛔ 不记 A） | 原版 18 档状态位的唯一写入口；我们按「归属/布局」直接驱动视图（架构选择）⇒ 复刻=引入全局状态位、与现架构冲突 |
| 82 | `RemoveInHandEffects` | **已核 ⇒ D**（时间限定 = 本 build） | `BattleAnims.json`（唯一一份，`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/`）`reflectedPrefab` = `{m_FileID:0, m_PathID:0}` **空 PPtr**；`BattleCardUI.ShowReflected`(:2586) 有 `if (battleAnims.reflectedPrefab != null)` 闸 ⇒ **从不实例化**；`reflectedFrame` 只在 `StopReflected`(:2602) 被销毁 ⇒ 原版这一版**永远没有反射框**，我们无该视图 = 覆盖完整。并入 patch `_parts/B9_C2.tsv` |
| 953 | `TriggerOtherCardSpiritStone` | **已 A（他人改判）** | 主表已变 A，本表保留行号仅作索引。⚠️ 注意 `HasPendingActions` / `PlayNextScriptedAction` / `PlayScriptedTurn` 各有两 overload：tutorial 版 A、**campaign 版仍是 C**（本表跟踪的是 campaign 版） |
| 49 / 596 | `ShouldUseEnviromentalEffects` | **保持 C/P1：代码保留 + 打 🔶 标记**（2026-10-26 调度台裁定） | 原版恒 false（RVA `0x6FB110` = `xor al,al; ret`）⇒ **原版永不弹出**该面板；我们**多做了这扇面板**，但 `offensiveCardEnabled` / `defensiveCardPanelEnabled` **出厂都是 `false`**、两条分支都在 `_choosePanel.Open(...)` 之前 return false（只有自检临时置 true）⇒ **默认不弹**。⇒ 不是「我们缺」，而是「多做、默认关、仅自检用」；⛔ 别删（自检在用）；**`🔶 自己设计` 标记任务归表现侧写手**，账已改写进 `_parts/B9_C2.tsv` |
| 12/39 | `GetDefensiveAttackValue` / `SuicideHeroAttack` | **保持 C、降 P2** | 原版有行为但 **0 调用点**（两侧今天都不可观测）；按铁律 11 仍要补函数，但不接线 |
| 840 | `RecordPendingDamage` | **改 B**（2026-10-26 C2 复核） | 5 个消费者全是**内部战斗/AI 决策**（`EnoughPendingDamageToDie` ×14 引用 · `HasEnoughPendingDamageAndIsRemnant` · `…AndHasToTransformIntoRemnant` · `…ToDieWithDamageValues`），调用方全是 `BattleManager` 的攻击/结算管线（`:4890`/`:9828`/`:37438`/`:40175`/`:42604`…），**没有任何卡能力读它** ⇒ 我们「立即落地 + 现算谓词」（`RuleCore.cs:7536`）行为等价，缺的是「预计算缓冲」这个实现结构 |
| 841 | `RecordPendingHealing` | **保持 C**（2026-10-26 C2 复核） | **有真实消费者**：`FilterMethods.cs:1121` 的 `UnitConditions.damaged(40)` 过滤器 = `GetCurrentDamage() > 0 && !EnoughPendingHealingToFull()`，池内 `conditionsFilter = 40` **27 行**（`cards_truth.json` 现读）；且原版 `pendingHealing` **从不被清空**（全树无 Clear/reset，`RecordPendingHealing` 只 append、`EnoughPendingHealingToFull` 只读）⇒ 它是**粘性累计预测表**、不是瞬时窗口：被记过「够回满」待治疗的单位，此后只要缺失血量 ≤ 累计值就被判「不算受伤」。我们 `ConditionEval.cs:231` 直接 `Health < MaxHealth` ⇒ 可见差异。⛔ 施工要**照抄「永不清理」**（别修成每回合清空）|

---

---

## §8 与别份 B9 产物的边界

- 本报告**只覆盖状态 C** 的 114 条；A/B/D 三档不在本表（B 档见 `判据/B9_剩余B_路线图.tsv`，A 档见各线已收口清单）。
- 与 `判据/B9_P0_判据表.tsv` / `判据/B9_P1_判据表.tsv` 的交集：**10 条**（P0 三条 + P1 七条）已在那两份里有更细的读报，本表对它们只做「步骤 + 施工 + 验收」的收口，不重复推翻其结论；若两份冲突，以本表「原版出处」列的行号为准（本轮现读）。
- 与 `判据/B9_第三优先_判据补齐.md` 的关系：本表 **87 `ShouldShowTargetsAvailable`** 与 **100/101 EntityScript** 三条直接引用它的偏移结论（`ActiveAbility.+0x40` vs `RawCardScript.+0x298`，⛔ 别混）。
- 本代理**没有**写 `账/_parts/B9_C2.tsv`：§7 那些「建议改 A/B/D」的裁定属于调度台（简报要求「列出来由调度台裁」）；调度台点头后再落补丁。
- 未跑任何 Unity 自检（按简报红线）；所有「怎么验」列写的都是**写手落地后**该跑的断言/探针，不是本代理跑过的结果。
