# B9 · E2P11 · `BattleDriver.cs` 施工报告（第八十四会话 · 2026-10-26）

独占文件：`d:/4/Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（+534 / −33 行，行尾仍为纯 CRLF：33823 CRLF = 33823 LF，裸 CR 0）

---

## ① 结论

| 项 | 数字 |
|---|---|
| 派活包 P-11 的 28 条 | **落地 15 条 / 判 D（原版死代码或判据为空）5 条 / 观测等价（已在，只核不改）6 条 / 仍缺件 2 条** |
| 施工单外追加的「撤单位 → 回手到原索引」 | 1 条：表现层已天然满足，缺口落在 `EffectResolver`（不是本件） |
| 调度台中途追加 | **3 件全部做完**：`CheckForTutorialPointer` + 订阅 · 施工单 89 同款注释 · `SpeakTutorialChat` 的 `*ChatBig` 两档接 `BattleBigChat.Display` |
| 账补丁 | `资料/完全复刻/账/_parts/B9_E2P11.tsv` · **29 行 × 11 列** · `check_ledger_points.py` 现跑 **失败 0 / 漂移 0 / 告警 0（A=13 条）** |
| L0 类型检查 | `TMPDIR=/tmp/wf_E2P11g bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（收工前最后一次也是 0/0） |
| 指纹 `ruleprobe play check` | **变了**，但**不是本件**：①-a 主档 1128 张**逐字节全等**；①-b 只有 **BL18 / UM18** 两张不同（`UM18 raw 46→50 / net 32→36`）⇒ 见下面「归因」 |

**关于调度台提到的两条 `CS0103 TutTipOffsetForElementsPx / TutTipOffsetPx`**：那是**两次编辑之间的瞬时态**（我先改了 `TutorialTipOffset` 的用法、下一次编辑才加常量）。两条常量已在 `:16545/:16548`，此后每一次 typecheck（含收工前最后一次）都是 **0/0**。

**指纹归因（A/B 隔离的结构性证据，不需要换文件重跑）**：`工具/ruleprobe/ruleprobe.csproj` 的 `<Compile Include>` 清单里**没有 `CardPresentation/` 任何文件**（只有 `RuleEngine/**` + 探针自身），`PlayProbe.cs:3257` 也明写「`CardPresentation/` 下的改动改不成这个 md5」。⇒ 本件（纯表现层）**结构上不可能**动这个指纹。而 `git status` 现读：`RuleEngine/Core/{RuleCore,ActionPump,CountEval,BoardSlots,TutorialScript}.cs` 与 `RuleEngine/Data/{AiScoring,SimpleAI}.cs` **今天都有未提交改动**（别的写手在飞）；`BL18/UM18 的 net 被 +4` 正是 `CLAUDE.md` §三 记过的那条签名（光环重算的副作用）。⇒ **归因 = 引擎侧在飞的那几笔；本件不认领。**

---

## ② 逐条（结论 + 证据）

> 行号是**收工前最后一遍现读**。判据全文写在代码注释里（本报告只给结论与坐标）。

### 无依赖组（派活包给的第一批）

| # | 原版方法 | 结论 | 证据 / 落点 |
|---|---|---|---|
| 47 | `BotConfig.GetBotDecks()` | **B · 落地** | 新增 `PickBotDeck`（`:33603`）+ `PickFrom`（`:33636`）；调用点 = `Begin` 里 `foeDeck ?? PickBotDeck(...)`（`:5340` 一带）。过滤器 = 阵营 + `gameMode` + `complete`，`System.Random(seed+2)` 取下标。⚠️ 原版的 `selectableBotDecks` 名单与 `currentEvent` 禁卡表本地没有 ⇒ 标 🔶（`差异` 列已写） |
| 450 | `BattleManager.OnApplicationQuit()` | **A · 落地** | `:33568`；`_net.InBattle` 才发，`_quitForfeitSent` 幂等，`Begin` 里复位（`:5236`） |
| 259 | `BattleManager.ClickExitBattle()` | **A · 落地** | `ExitForfeitIfReplay`（`:33542`），接线 = `Forfeit` 首句 `:12263`。判据 `BattleManager__ClickExitBattle.c:32-43` |
| 914 | `CardScript.ShouldShowTargetsAvailable(bool)` | **A · 已在（只核不改）** | 落点 `ShowSkillPanel`（`:15300`）里 `:15305` 已有上一批的接线；原版唯一调用点传 `inPlay=false`（`BattleCardUI__ShowAvailableTargets.c:34`）⇒ 我们同口径 |
| 88 | `AiScripted.CheckIfWaitingToChangeDesiredAttack(...)` | **A · 落地** | 新增 `ScriptedChangeAttackMatches`（`:16320`），由 `TutorialCompleteChangeAttack`（`:16280`）调用。**顺序照原版**：先 `SetCurrentAttackType`（点错档也照写），再比对决定推不推指针；兜底方向 = 主语解不出**推进**（`AiScripted.cs:1136-1139`） |
| 782 | `CardScript.HeroLandIntoField(bool, float)` | **B · 部分落地** | 新增 `HeroLandInto`（`:33742`），接线 = `TickTutorialView` 第二拍（`SetActive(true)` 之后，与 `_TutorialStartSequence:80` 同序）。三常量用 `工具/read_literal.py` 现读：`0x1834b2bb0=0.2` · `0x1834b2dc8=0.3` · `0x1834b2dc4=0.1` · `0x1834b2bb8=1.0`。只落「段0 等待 + 段1 LandOnGround 震镜」；缩放/alpha 两半按我们的形状是空操作（见 ③） |
| 401 | `BattleManager.HideHeroAtStart(bool)` | **D · 原版死代码** | 三份反编译全库只有定义（`decomp_full` 只有 `.c` 自身；`cpp2il dev BattleManager.cs:35387`；`il2csharp BattleManager.cs:15640`；`script.json:12232` 是方法登记）⇒ 零调用点。**不依赖 E3**（那个前提不成立） |
| 46 | `AIBotsConfig.GetActionDelay()` | **A · 落地** | `NextAiActionDelay`（`:33666`），两处 `_aiTimer` 赋值改造；区间 🔶 0.45–0.65（均值保持原 0.55） |
| 53 | `AIBotsConfig.GetEndTurnDelay()` | **A · 落地** | `AiEndTurnSoon`（`:33676`）+ `_aiEndTurnWait`（`:33687`）+ `DriveAiTurn` 顶部挂起支；三个收手点改调它；`DriveAiTurnForTest` 把挂起清 0（`:32131`） |
| 54 | `AIBotsConfig.GetFirstActionDelay()` | **A · 落地** | `NextAiFirstActionDelay`（`:33668`），回合开始两处（`:361` / `:16212`） |

### 链上组

| # | 原版方法 | 结论 | 证据 / 落点 |
|---|---|---|---|
| 238 | `CancelAnimFromUnitFromQueue(...)` | **B · 表现层这一半落地，无调用点** | `CancelPendingAnimsOf`（`:33790`）。原版两处**活**调用点都在「某张卡上的效果被移除」族（`ResolveRemoveEffect.c:7-10` · `ResolveAction.c:1727-1734`），我们没有那条引擎信号（与 `RemoveDestroyableAnims` 同一卡点）⇒ 等引擎批 |
| 239 | `CancelAnimsFromUnit(...)` | **D · 原版死代码** | 四路查证（`decomp_full` 全库只有定义 · `cpp2il dev:34080` 只有定义 · `il2csharp:14916` 只有定义 · `assets_full` 0 命中） |
| 483 | `RemoveSelfBuffsFromQueues(...)` | **B · 表现层一半** | `RemoveSelfBuffsFromQueues`（`:33817`）；`shouldIgnoreDestroy` 的语义在引擎（buff 记账 + `Destroyed` 触发）⇒ 等引擎批 |
| 236 | `CancelAbilityTargeting()` | **B · 已在 + 施工单前提订正** | 失败支的等价物 `CancelTargetChoosingByInvalidClick`（2026-10-25 `N4` 逐句接的 `FinishActing` 等五句）。🔴 **施工单的前提不成立**：成功支原版**不**调 `FinishActing`（`ExecuteActiveAbility.c:145-151` 只有 `DisableUsableLight` + `SetupActiveAbility`；`FinishActing` 全库只有 `CancelAbilityTargeting` 与失败支两个调用者）⇒ 「白层先褪再收面板」那条差异不存在 |
| 304 | `ExecuteActiveAbility(...)` | **B · ②闸已在，③入队是结构性差异** | `DoResolve`（`:15854`）。② 的等价闸 = `CommitCommand`/`Resolve` 遇 `WaitingForBlockingAction` ⇒ `PleaseWait`（`:5602` 一带注释现读）；① 的等价 = `RuleCore.UseAbility` 自身合法性 + 拒因出声；**③ 入队 vs 同步结算**没有可观测出口（引擎没有动作队列），⛔ 不造表现层队列 |
| 940 | `TransformUnitInPlayToCard()` | **A · 观测等价** | `PlayReturnFeel`（`:24799`，E3 的 `ReturnToHand`）+ `SyncHand`。末尾 `BattleCardUI.ResetTeleportCardOutOfBattle`（`decomp_full` 全文读过：恢复父节点 `SetActive(true)` + `localPosition=zero` + 子件 `NestedFadeGroup α=1`）**我们从不建那类临时传送壳** ⇒ 无落点、也无差异 |
| 973 | `UndissolveToDeck()` | **C · 无落点** | `decomp_full/CardScript__UndissolveToDeck.c:8-21` 五步都需要「卡停在半溶」这一态；我们的离场是销毁、再抽到是新建视图（一上来全不透明）⇒ 该态在我们这里不存在。E3 的 `CardFeel.UnDissolve`（`CardFeel.cs:3579`）是给那个态的终点口，今天零消费者 |
| 819 | `OutFromPlayingAreaAnim()` | **A · 落地（借 E3 的件）** | 接线 = `ResolvePlayTargetByUnit` 的取消支（`:8126` 调 `CardFeel.MoveBackToHand`，`:8117-8126` 有逐句出处）；④ `LightPlayableHand` 的等价 = `SyncHand → RefreshHandPlayable`（`:26501` 一带，`RefreshAll` 每批必调）⇒ 已存在 |
| 884 | `SetAsCemetery(int)` | **A · 已在/不可观测** | 位置+缩放在 `CardFeel.GoToCemetery`（`CardFeel.cs:2526`，由 `DeathExplosion` 第四拍调）。**关键现读**：`CemeteryManager__AddCardToCemetery.c:45-52` 在 `SetAsCemetery` 之后**同帧** `SetActive(false)` ⇒ 排序层/序号/关碰撞三句**画面上一帧都看不到**（施工单那条「三张墓地卡叠放」的验收标准在原版不成立） |
| 936 | `ToggleCombatState(bool)` | **C · 无消费者 ⇒ 不造死代码** | 原版两个消费者（`SetCombatHighlights` = `minionWillDieObject(+0x40).SetActive(false)`；`OnTouchExit` 的 `changeAttackType = !IsAnimatingCombat`）在我们这儿都无落点：我们把「会打死它」只挂在悬停准星上；且我们**故意**不在清选择时熄攻击型底光（`AU1` 口径） |
| 1172 | `PlayerHand.HideHand()` | **D · 原版死代码** | 三份反编译全库只有定义（`cpp2il dev PlayerHand.cs:3727` · `il2csharp:1331` · `script.json:20900` 是登记） |
| 1181 | `PlayerHand.PlaySentinelAnims(BattleAnims)` | **B · 零落地（判据缺）** | ② `BattleAnims.json` 的 `sentinelDeckAnim` 是**空 PPtr**（PathID 0；同文件只有 `heal` 非空）⇒ 本 build 原版那一支也不执行；③ 的 cue 在 `BattleSfx.cs`（别人的文件）；① 需先普查 sentinel 特性可达性 |
| 101 | `AiScripted.GetDelay(..., bool delayBefore)` | **B · 落地（真机档）** | `DriveTutorialScript` 的 pre-wait 支（`:17859` 起，`_tutPreTurn/_tutPreCounter` 闩在 `:33707` 后）；后置等待 = `StepTutorialScript` 末尾 arm 支。**批处理口径不变**（见 ③ 边界） |
| 115 | `AiScripted.SkippableActionWait(...)` | **B · 落地（真机档）** | 复用 `_tutWaitArmed` 机制（累加 + `RequestTutorialSkip` 立刻收 + `FinishTutorialActionWait` 收尾），arm 条件改为 `animateFeel ‖ (我方 ∧ 聊天五档)`（`:17943`） |
| 91 | `AiScripted.CloseChatBoxes(...)` | **A · 落地 + 旧注订正** | `CloseTutorialChatBoxes`（`:17971`）补了 `_unitChat.StopVoice()`；「原版是否停音没核」那句就地改掉（`UnitsVoiceLinesPanel__HideChatBox.c:11` 就是 `AudioSource.Stop()`） |
| 94 | `AiScripted.ExecuteAction(..., ScriptedActionCampaignData)` | **C · 依赖战役线** | 战役执行器未建（P-15 整包）。本件顺带把其中「大框」那层接到教程路径（见下面调度台追加项） |
| 102 | `AiScripted.GetHorizontalOffset(...)` | **A · 落地（含一处量纲订正）** | `TutorialTipOffset`（`:16489`）+ 常量 `:16545/:16548`。**判据由「推断」变成「实读」**：`VarsDesktop.json` 的 `tutorialTipHorizontalOffset=20` / `...ForElements=18`，`VarsMobile.json` = 18/16。⚠️ **可见变化**：原来量的是 `CardView.Width/Height`（3.33/2.09 世界单位 ≈ 360/226 设计 px），现在 = 20/18 设计 px ⇒ 真机需看一次教程提示摆位 |
| 596 | `ShouldUseEnviromentalEffects()` | **A · 只打标记** | 🔶 标记写在 `offensiveCardEnabled` 的 summary 首行（`:8953` 那个字段；落点列指向方法 `BeginOffensivePhaseIfAny` `:8983`，因为字段不参与账闸的名字核）。行为未改：两扇面板默认关 ⇒ 与原版恒 false 等价 |

### 施工单外追加

| 项 | 结论 | 证据 |
|---|---|---|
| 单位卡「撤单位 → 回手到原索引」 | **表现层已满足，缺口在 `EffectResolver`** | 引擎侧那一口已存在：`RuleEngine/Core/RuleCore.cs:14144 AddCardToHandAtWithinLimit`（`Hand.Insert(at, inst)` + 满手进墓地，注释里写着「现读 `EffectResolver.cs:3504-3511` / `:3704-3711` 仍用 `Hand.Add`」）⇒ **调用方归 EffectResolver 写手**。表现层：`SyncHand` 按引擎 `Hand` 列表重排（`:26364` 起），我们**不另存下标** ⇒ 原索引天然保持 |

### 调度台中途追加（3 件全做完）

| 项 | 落点 |
|---|---|
| `CheckForTutorialPointer()` | `:16897`。三道闸 = 教程局 / `TutorialView != null && gameObject.activeSelf` / `CurrentAction.playerAction`（判据 `BattleManager__CheckForTutorialPointer.c:41`）；**不闩** `_tutViewTurn/_tutViewCounter` |
| 订阅 `OnHandCardMovedToNewIndex` | 装配块里 `interaction.OnHandCardMovedToNewIndex -= / +=`（挨着 `OnTapped` 那一对），具名方法 `OnHandCardMovedToNewIndex`（`:33707`）—— 用**具名方法**而不是 lambda，保证 `-=` 能摘同一个委托实例（那块可重入） |
| 施工单 89 同款注释 | `TapBoardCard` 方法体首段（`:32955` 一带），逐句复述 `TutorialScript.cs:1282-1291` 那五行的判据（`CheckIfWaitingToClickCard` 六档 + 我们无消费者 ⇒ **不新增**） |
| `*ChatBig` → `BattleBigChat.Display` | `SpeakTutorialChat`（`:17066`）里算完 `text` 之后：两档走 `BattleBigChat.Display(_unitChat, text, who)` 并 **return**（原版这两支不再走 `DisplayChatBox`），失败退回普通气泡并出声；同时把「那层我们没做」的旧注就地订正 |

### E3P13 留的 6 组接线点（本节点名取舍）

| E3 的件 | 我做的那部分 | 留待下一波（点名文件:行） |
|---|---|---|
| `CardFeel.MoveBackToHand(card, handQueue, targeted)` | ✅ 已接：`ResolvePlayTargetByUnit` 的取消支 `BattleDriver.cs:8126`（`handQueue = CardView.QHandMoving`，`targeted = CardDef.Targeted == 1`；之后 `SetFace(Hand)` + 重挂父件 + `SyncHand()` 承担「④ SetStateInHand」） | — |
| `CardFeel.UnDissolveWholeCard` | ✅ 由 `MoveBackToHand` 的 targeted 分支内含（= `OutFromPlayingAreaAnim.c:26` 那一条） | — |
| `CardFeel.UnDissolve` | ❌ 不接：它服务的「卡停在半溶」这一态我们**不存在**（见上面 973） | 若将来出现复用视图带溶解态的形状，调用点 = 洗回牌库那一路（`BattleDriver.cs:24799` 附近） |
| `CardFeel.StopInPlayAnims` | ✅ 接了 1 处：`PlayDeathFeel` 起爆散之前（`:24774`） | 其余 7 处：送墓那半已在 `DeathExplosion` 内（E3）；回手/回牌库走 `ReturnToHand`（E3 已内含）；**待接** = 残骸翻面（`BattleDriver.cs:25400` 一带）、jam、`Kill` 重置（`Begin`/`Restart`）——下一波点名 `BattleDriver.cs` 的这三族 |
| `CardFeel.ShakeUnit` | ❌ 不接（数据缺）：`Resources/card_anim_order.json` 没有 `unitsShake` 字段，要重烘 ⇒ 按调度台口径**不自作** | 调用点位置 = `CardAnimOrder` 展开那一拍（`BattleDriver.cs:19000` 一带 `beat` 分支） |
| `CardFeel.ShowCardBeingDrawn` | ❌ 不接（要**新建**弃牌/偷牌两条揭示链，属「新造链路」） | 落点 = 弃牌/偷牌那两个事件分支（`EvtKind.Discard` / `Steal`，`BattleDriver.cs` 的 `PlaySignal` 大 switch）；`stepTime` 弃牌 0.2 / 偷牌 0.4 |
| 974 三处 / 804 两处 | ❌ 未做 | 974：`CardData.base*` 填充（`CardData.Simple`/`DeckRuntime` 侧）、敌方自动定档、`+0x120` 缓存；804：召唤延迟动画开关 + 召唤消息 —— 落点都在 `BattleDriver.cs` 的卡面/入场族 + `Core/CardView.cs`（E3 文件，需新口子要报调度台） |

---

## ③ 没查清 / 本轮边界（如实记）

1. **782 的 `showStats` 那一半**：`.c:97-338` 是四组嵌套循环（逐组 `SetActive` 打开状态图标/计数器），**没有逐句解** ⇒ 标 TODO，不假装做了。
2. **782 的缩放与 alpha 两半**：原版那一跳的**起止值相等**（`GetUnitSizeInPlay` 前后同值）⇒ 真机上本来就是空操作；我们更不该重写（`SyncBoard` 管在位尺度）。把 alpha 压 0 再淡入是**我们额外加的风险**（补间一被杀就是一张看不见的督军）⇒ 不做，理由写进代码。
3. **101/115 的批处理口径**：窗口靠 `TickTutorialView(dt)` 由各宿主**显式步进**；扩到「所有动作 + 两边回合」会把 `Editor/BattleScene.cs` 里那些固定步进算错 ⇒ 只在 `animateFeel`（真机演出档）开，批处理**保持改动前**。⇒ 批处理**测不到**这两条的新行为（要测得宿主配合多喂几次 `TickTutorialViewForTest`）。
4. **47 的 `selectableBotDecks` / `currentEvent` 禁卡表**：远端资产，本地 0 命中 ⇒ 过滤面比原版宽（🔶 已标）。
5. **102 的量纲映射**：`VarsDevice` 那两格是 **UI px**（同 SO 里 `cardInHandShownYOffset=30` 也是 px，判据 `Editor/CardBaseDemo.cs:5634`），我们用 `LayoutSpace.Px` 换成世界档距离。⚠️ 偏移量从 ~360px 缩到 20px 是**可见变化**，需真机核一次。
6. **1181**：`sentinelDeckAnim` 空 PPtr（判据为空）+ 音效 cue 在别人文件 + 池内可达性未普查 ⇒ 三条都没落。

---

## ④ 改动清单

**唯一改动的文件**：`MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（`git diff --numstat` = **+534 / −33**；CRLF 33822 / LF 33822 / 裸 CR 0）

**新增成员（全部追加在类尾，⛔ 没有插在已有成员之间）**：
`ExitForfeitIfReplay` · `OnApplicationQuit` · `_quitForfeitSent` · `PickBotDeck` · `PickFrom` · 三组 AI 延迟常量 + `NextAiActionDelay/NextAiFirstActionDelay/NextAiEndTurnDelay` · `AiEndTurnSoon` · `_aiEndTurnWait` · `NextAiDelayForTest` · `AiDelayRangeForTest` · `AiEndTurnWaitForTest` · `OnHandCardMovedToNewIndex` · `_tutPreTurn/_tutPreCounter` · `HeroLandInto` · `CancelPendingAnimsOf` · `RemoveSelfBuffsFromQueues`

**改动已有成员（都是就地改，带日期与出处）**：
`Forfeit`（首句加回放早退）· `Begin`（两处跨局复位 + 对手牌组挑选）· `NetApply` 的 `EndsTurn` 支（首步延迟）· `EndTurnAndAdvance`（首步延迟）· `DriveAiTurn`（顶部收手挂起支 + 两处动作延迟 + 一处收手点）· `FinishAiTargetingAnim`（收手点）· `DriveAiTurnForTest` · `DriveTutorialScript`（pre-wait 支）· `StepTutorialScript`（arm 条件）· `CloseTutorialChatBoxes`（+`StopVoice`）· `SpeakTutorialChat`（`*ChatBig` 两档）· `TutorialCompleteChangeAttack`（两档比对）· `ResolvePlayTargetByUnit`（取消支接 `MoveBackToHand`）· `PlayDeathFeel`（+`StopInPlayAnims`）· `TickTutorialView`（第二拍接 `HeroLandInto`）· `TutorialTipOffset`（量纲/取值订正）· `TapBoardCard`（同款注释）· `StartCameraIntro` 无关（未动）

**账补丁**：`资料/完全复刻/账/_parts/B9_E2P11.tsv`（29 行 × 11 列，LF，首行 11 列表头原样；`check_ledger_points.py` 现跑 失败 0 / 漂移 0）

---

## ⑤ 顺手发现（只报不改 / 只报给调度台）

1. 🔴 **账/施工单口径错三处**（本件已就地订正并留痕）：
   - **259**：施工单把 `0x8C/0xA0` 的名字写成 `Replay/TutorialReplay`，我们枚举是 `TutorialReplay=140 / Replay=160`（`RuleEngine/Core/BattleContext.cs:2062/2064`）—— 值集合相同、名字对调。
   - **236**：施工单的前提「成功支 `FinishActing` 早于面板 `FadeOut`」**在原版不成立**（`ExecuteActiveAbility.c:145-151` 没有那句）。
   - **884**：施工单的验收标准「三张墓地卡按 index 叠放」**在原版看不到**（`CemeteryManager__AddCardToCemetery.c:45-52` 同帧 `SetActive(false)`）。
2. 🔴 **施工单里 3 条其实是原版死代码**（401 `HideHeroAtStart` · 1172 `HideHand` · 239 `CancelAnimsFromUnit`），都是三/四路查证零调用点 ⇒ 判 **D**（铁律 11 例外①）。⛔ 不要再为它们建机制。
3. ⚠️ **`aiStepDelay` 现在没有任何读点了**（4 处赋值全改成三档随机）。字段留着是为了兼容公开面（`public`），但它已经是**没有消费口的陈旧值** ⇒ 建议调度台决定「删字段」还是「加注释说明它只是历史旋钮」（本件只加了使用处的注释，没删）。
4. ⚠️ **`CardFaceRules` 那条 914 是上一批接的**，施工单写「它今天不传（恒 false）」已过期 —— 建议把 B9 施工单里 914 那行标成 ✅（本件未改施工单，只写在这里）。
5. ⚠️ **`PlayIconAnim` / `ResolveAbilityIconAnim` 原版零调用点**（调度台已确认）⇒ 我**没有**给它们挂任何触发源，也不打算挂。
6. ⚠️ **`MoveBackToHand` 接线后的批处理风险（提醒调度台跑 `BattleScene.Run` 时注意）**：它在部队支起一条 `ArtAlpha → 1` 的 DOTween。批处理下这条补间**不会推进**；但取消出牌时那张卡的 `ArtAlpha` 本来就是 1（批处理不飞不溶）⇒ 读数是「1→1」，**预期零影响**。若自检出现「取消后手牌卡透明」一类红，先看这里。
7. 📌 **两处「我们多出来的」可以留着当证据**：`offensiveCardEnabled` / `defensiveCardPanelEnabled` 那两扇面板（596）与 `§25` 断言族 —— 现在都带 🔶 标记，别再当缺口去「补」。

---

## ⑥ 追加（同一会话 · 落点注解补丁）

`账/_parts/B9_E2P11b.tsv`（10 行 × 11 列 · LF · 首行 11 列表头）—— 只改这 10 行的 **`我们的落点`** 列，
补上 `（等价实现 = 我们的名字：一句为什么等价）` 那道注解（`verify_annot.py` 报的 10 条「缺注解」）：

`GetActionDelay` · `GetEndTurnDelay` · `GetFirstActionDelay` · `CheckIfWaitingToChangeDesiredAttack` ·
`GetHorizontalOffset` · `ClickExitBattle` · `ShouldUseEnviromentalEffects` · `OutFromPlayingAreaAnim` ·
`SetAsCemetery` · `TransformUnitInPlayToCard`

**自检（只读）**：把补丁**模拟合并**进主表的 `%TEMP%` 副本，再跑 `verify_annot.py` 的同一套判法
（⛔ 没写主表、没合并）⇒ **同名 772 · 异名已注解 2729 · 缺注解 0**（改前 2719 / 10）。
另跑 `check_ledger_points.py B9_E2P11b.tsv` ⇒ **A=10 · 通过 10 · 漂移 0 · 告警 0 · 失败 0**。

⛔ 本笔**没有碰任何 `.cs`**（`BattleDriver.cs` 已交给别人）⇒ typecheck 维持 0/0 不变。

> ⚠️ 记一条本次踩到的坑（老实交代）：第一次用 `bash` 双引号里的 `python -c "…"` 写这段说明，
> 文中的**反引号被 bash 当命令替换** ⇒ 文字被静默吞掉（`CLAUDE.md` §二 记的那条坑，我又踩了一次）。
> 已改用「Write 到临时文件 → python 读那个文件追加」重写。
