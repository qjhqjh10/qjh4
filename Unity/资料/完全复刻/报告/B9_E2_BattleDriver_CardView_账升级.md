# B9_E2 — 战斗逻辑驱动 · BattleDriver / CardView 切片账升级（第八十三会话）

> 切片来源：`工具/ledger_slice.py --domain 战斗逻辑驱动 --status B --file BattleDriver`（70 行）/ `--file CardView`（111 行）。
> 本轮按「账行号升序」做了 **BattleDriver 那批的前 40 条**（物理行 **8 – 361**）；**CardView 那批一条没动**。
> 产出补丁：`账/_parts/B9_E2.tsv`（40 行，含表头共 41 行；11 列；LF；键 = 域 + 原版类 + 方法）。

---

## ① 结论

| 结果 | 条数 | 说明 |
|---|---|---|
| **升 A** | **19** | 判据读到方法体 + 我们侧落点能指到**实现那几行**（不是类入口） |
| **判 C** | **7** | 原版有、离线可做、我们缺或不同 —— 每条都写了「差什么 / 该改哪个文件哪个方法」 |
| **判 D** | **13** | 6 条「调试/纯日志/空方法体」+ 5 条「渲染架构不同 ⇒ 用别的方式覆盖」（都写了覆盖说明）+ 2 条（Get2DPosOfCard / Get2DPosOfSpecificItemInCard） |
| **留 B** | **1** | `DisplayHurryUpChatMessage`：落点有了、但原版那道闸（`isPopUpShown`）**本轮没定位**，不用猜的顶判据 |

**落点闸（机检，只读）**
```
python -I -X utf8 工具/check_ledger_points.py --warn-getters 资料/完全复刻/账/_parts/B9_E2.tsv
⇒ A=19 通过=19 漂移=0 告警=0 失败=0
```
> 第一次跑出 **5 条漂移**、**1 条告警**，已改：`路径:行` 里的行号必须是**括号里那个声明的行**（不是方法体里那一行）；`*ForTest` 口不能当实现落点（`Get2DWorldPosFromBoardPos` 已改指生产路径 `CardHit:31897`）。

**为什么大批 B 能升 A（本轮的关键）**：这 40 行里有 **29 行**原先是模板句「表现层 / HUD / 动画 / 输入 / 声音（原版方法体是 Transform/Tween/TMP/相机）」+ 落点只写到「B 域：BattleHud · CardFeel · CardView · SettingsPanel」这种**类级命中** —— 按 A 的定义那只能记 B。本轮把每条的原版方法体都读了，落点逐条下到方法内部的行。

---

## ② 逐条证据（只列 C 与疑点）

### C（7 条 —— 都要做，只有先后之分）

| # | 原版方法 | 缺口（证据） | 该改哪儿 |
|---|---|---|---|
| 47 | `AIBotsConfig.BotConfig.GetBotDecks()` | 原版把该难度档的预组池交给 `MatchData`，再 `GetRandomDeck(selectableBotDecks, possibleArmies, currentEvent)` **随机挑一副**（`cpp2il/out_cpp2il_dev_ilspy/MatchData.cs:1253 / 1428`）；我们 = 模式窗给的那副，否则**按阵营池随机凑**（`BattleDriver.cs:4552 / :5307`）⇒ 机器人用的牌与原版不同 | `BattleDriver.ResolveDeck`（:5307）加一支「按难度档从 `Resources/prebuilt_decks.json` 的预组池挑」。**离线可做**（数据本地就有） |
| 110 | `AiScripted.GetHorizontalOffset(...)` | **载体错**：原版读 `VarsDevice.tutorialTipHorizontalOffset(+0x34)` / `…ForElements(+0x38)`（`dump.cs:120056-120059`；`VarsDesktop.json` = **20 / 18**、`VarsMobile.json` = **18 / 16**），小屏再 ×1.3；我们拿 `CardView.Height/Width` 当偏移（`BattleDriver.cs:16201-16208`）⇒ 数值完全不同 | 新建 `VarsDevice` 等价数据件，`BattleDriver.TutorialTipOffset` 改读它 |
| 258 | `BattleManager.CancelAnimFromUnitFromQueue(...)` | 原版「某张卡离场 ⇒ 把它待播的动画动作从队列撤掉」（方法体亲读，`BattleManager.cs:34196-34290`）；我们的 `_timeline` 只有 `:7983` / `:19794` 两处 `RemoveAt`（单步取一条），**没有按卡撤** ⇒ 退场/死亡单位的待播动画仍会播 | `Battle/BattleDriver.cs` 的 `_timeline` 加 `CancelSignalsOf(unit)`，挂在「卡离场」那几处 |
| 259 | `BattleManager.CancelAnimsFromUnit(...)` | 同上；原版是对**四条队列**各调一次（`actionQueue`/`instantQueue`/`interruptQueue`/`delayedInstantQueue`），我们只有**一条** `_timeline` | 与上一行一起做，4 队列 → 1 时间线的映射要写清 |
| 281 | `BattleManager.ClickExitBattle()` | **缺「回放 / 教程回放 ⇒ 直接 `LeaveBattle()`」那一支**（`ClickExitBattle.c:43`）：原版这两种 `matchType` 跳过整段投降/判负；我们 `Forfeit()`（`:12031-12099`）只有 `Ctx==null` / `Ctx.IsOver` 两道早退 ⇒ 回放局里点那颗（文案已是 `Battle/Settings/Exit`，`SettingsPanel.cs:2466-2473`）**会判负**而不是离开 | `BattleDriver.Forfeit()` 开头加：`Ctx.MatchType ∈ {Replay, TutorialReplay} ⇒ LeaveBattle(); return;` |
| 321 | `BattleManager.DisplayBigChat(string, CardScript)` | **未做**（我们自己记过：`BattleDriver.cs:16737-16739`）。教程动作 `PlayerChatBig(55)` 落到我们这里只走普通聊天 | `Battle/UnitChatPanel.cs` 加「大框」一档或新建 `Battle/BattleBigChat.cs`。⚠️ **显示参数还没拿到**（见 ③） |
| 322 | `BattleManager.DisplayBigChat(string, Sprite)` | 同上一行的 Sprite 重载 | 同上 |

### D 里的两类边界（请调度台裁一下口径）

**甲类 · 有硬判据的 6 条**（调试 / 纯日志 / 空方法体）：
- `DebugDrawBattle` / `DebugWinBattle`：唯一调用点是设置窗的调试钮，而那组钮在 `BattleSettingsWindow.Awake` 被 `debugButtons.SetActive(false)`（`cpp2il/out_cpp2il_dev_ilspy/BattleSettingsWindow.cs:46-48`）⇒ **发行版不可达**（死代码）。
- `DebugActionsInQueue` / `DebugActionsInQueueCompressed` / `DebugQueue` / `DebugQueueCompressed`：体里只有 `CustomDebug.Log`，不改任何状态 ⇒ 唯一可观察效果是一行日志。
- `DrawTestScreenLines`：全是 `Debug.DrawLine`（`GetBoardMousePos` 开头无条件调一次），**发行包不渲染** ⇒ 纯编辑器可视化。
- `ClickSettings`：**方法体只有 `return;`**（`decomp_full/BattleManager__ClickSettings.c:1-4`；RVA `0x6C2B50` ≠ 共享空桩 `0x4B33B0`，所以不是「被剥掉」，是真空）。

**乙类 · 「用别的方式覆盖」的 5 条**（渲染架构不同，⚠️ **若调度台认为 D 只收「离线不可用 / 死代码 / 纯编辑器」，这 5 条应改 C**）：
| 方法 | 覆盖说明 |
|---|---|
| `BattleManager.Awake()` | 体里**只有** `SetupSingleton()`（`BattleManager.cs:15102-15105`）；我们无静态单例，一局一个 `BattleDriver`（`:4865` / `:4033`，`:4234` 摘静态钩子） |
| `DestroyHiddenCards()` | 原版为牌库/墓地每张卡保留视图再 `SetActive(false)`；我们**没有这些常驻视图**（牌堆 = 一张 HUD 底板 `_myDeckPlate`，墓地 = `BattleLogPanel` 文本行） |
| `Get2DPosOfCard(...)` | 原版把战场 3D 点投到 HUD 2D 平面去摆特效；我们**按锚点挂载**（`EnsureAnchorSpace` + `CardEffects.Fire(vfx, zero, anchor)`，`:22576-22577`）⇒ 没有消费者。同数学的桥在生产路径上确实存在（`CardHit:31897` 的 `:31931-31933`） |
| `Get2DPosOfSpecificItemInCard(...)` | 同上；锚点等价物 = `CardView.EffectAnchorBoard`（`Core/CardView.cs:2046`）。⚠️ **但「近战/远程/生命三个子锚」我们是缺的**（`BattleDriver.cs:16262-16264` 已如实记「只到卡面这一层」）—— 这一条归卡面/特效线 |
| `GetBoardCardMousePos(float)` | 原版 `Physics.Raycast` 撞 collider 取 `bounds.center`；我们的「屏幕 → 棋盘」= `TargetReticle.ResolveAim`（`Battle/TargetReticle.cs:511`，两平面求交）+ 投影命中（`CardHit`/`HitSlot`） |

### 就地订正的三条旧记录（铁律 5）
1. **物理行 8** 的差异 ③「原版那一路可能抽到空卡，**我们不产空**」—— **错**。`Core/OffensiveCards.cs:72 Choices` 下标 0 就是「不使用进攻卡」那张（`idx = -1`），`PickOffensiveForAi` 抽得到。已订正并升 A。
2. **物理行 47** 的落点原写「`Resources/prebuilt_decks.json`（由 Shell 侧取）」—— 那是**玩家**那副（`TakePendingBattleDeck`，`:4503`），不是机器人的。已订正。
3. **物理行 211 / 287 及那 29 行**的判据要点是模板句（「表现层 / HUD / 动画 / 输入 / 声音」）—— 与实际方法体不符（`Awake` 只有一句 `SetupSingleton`；`ClickSettings` 是空方法）。已按体订正。

### 其余「同段代码 / 同 setter」的两处（用了 A 的例外条款）
- 物理行 **284 / 285**（`ClickPauseReplay` / `ClickPlayReplay`）→ 同一个 `SetReplayPaused(on)` 的两个极性；落点写 `:7966-7971`，判据要点写清各覆盖哪一半。
- 物理行 **297 / 298**（`CreateAnim` / `CreateAnimFromAnimFxController`）→ 297 落 `:22484 PlayAbilityBeat`（实例化 + 挂锚点段），298 落 `:3989 ResolveAnimAnchor`（= `GetAnimTransform` 那两次解锚点）。

---

## ③ 没查清的部分（⛔ 没有拿猜测填空）

1. **`VoiceLinesController.isPopUpShown`（`+0x78`）到底是什么** —— 它挡着 `DisplayHurryUpChatMessage`（判据 `VoiceLinesController.cs:1344-1369`：`CanChat(isPlayer:true)` = `IsDuringBattle(includeMulligan:false)` ∧ `isPopUpShown`）。所以那一行**留 B**：落点（`:7631 SpeakHurry`）有了，闸不能断言一致。
2. **`DisplayBigChat` 的显示参数**：Cpp2IL 把真正的显示调用**吞了**（体里只剩参数校验 + `talkingUnit.rawCardData.fullHeroSprite` 取值 + 两条日志）。要拿参数得对 `BattleManager__DisplayBigChat`（RVA 见 `dump.cs`）做 VA 级反汇编 —— 本轮没做（越界到工具链）。
3. **`CardAnimController.Initialize` 的补间参数**：`CreateAnim` 的 `.c` 里有 `Method not found @180D34D30`（间接调用没解出来）。我们只落了「终值」那一支（`pt == boardCenter` 的 Z 角），补间本身靠 `_timeline` 逐帧推 —— 已在差异列写明。
4. **`DebugActionsInQueueCompressed` / `DebugQueueCompressed` 的判据出处行范围是估算**（引用的是 `BattleManager.cs` 相邻区间 + 同名 `.c`）。它们判 D，不影响结论；要精确可再核。
5. **`DestroyHiddenCards` 的对象模型**：我判 D 的依据是「我们没有牌库/墓地的逐卡常驻视图」。如果调度台认为**这套对象模型本身也该复刻**（牌库里应当有真实卡视图、开局逐颗关掉），那这条要改 C —— 那是「牌堆/墓地表现」的另一件事，不在本行能裁的范围内。

---

## ④ 交接给下一个代理

**这一轮留下的现状**
- 补丁 `账/_parts/B9_E2.tsv` 已写好并通过落点闸（A=19 · 通过 19 · 漂移 0 · 告警 0 · 失败 0）。
- `工具/check_patch_conflicts.py` 当时报「✅ 没有跨补丁重复键（1 份补丁）」—— 合并时**必须带 `--only B9_E2.tsv`**（不得整目录合并）。
- 主账 `账/战斗逻辑驱动.tsv` 我**一个字节都没写**（只读）。注意工作树里它本来就有别人未提交的改动（`git diff --numstat` = 145/145），**那不是我**。
- 本轮**没有改任何 `.cs`**、没跑 Unity、没动 git。

**下一批建议（接着做，别重查）**
1. **CardView 切片一条都没做**（111 行）。它里面混着两类：① `--file` 是**子串匹配**，所以大部分行其实是 `BattleManager` 的（落点里带 `CardView` 字样）；② 真·CardView/CardFeel 那批在物理行 421 以后。⚠️ 简报里的那条铁则继续有效：**不能用 `default(CardData)` 顶替判据**（它走「空卡位」分支，缺卡框/立绘层）。
2. **C 的 7 条**里，性价比最高、判据最全的是 **`ClickExitBattle` 的回放分支**（一行 if + `LeaveBattle()`，判据 `ClickExitBattle.c:43`）和 **`GetHorizontalOffset` 的载体**（两份 SO 实值已抄在补丁的判据要点里，直接照做）。
3. **`CancelAnimFromUnitFromQueue` / `CancelAnimsFromUnit`** 是**新面**（时间线的「按卡撤队列」），做之前先 `grep` 谁会遍历 `_timeline`，并给每个离场入口都配一条断言（铁律 10 第 5 条：一个对象多个入口时状态要在每个入口显式设置）。

**复用价值（写进下一轮简报的「已知什么」）**
- 那 29 行的模板句判据要点 = **误标**，`Awake` / `ClickSettings` 的真体分别是「只有 `SetupSingleton()`」和「空方法」。
- `VarsDevice` 在本工程**没有对应类**（`Core/ScreenUtilities.cs:28` 已记「零消费者」）；它的两份 SO 实值：桌面 20/18、移动 18/16（`tutorialTipHorizontalOffset` / `…ForElements`），`warlordStartSizeFactor` 1.3/1.5。
- `BattleSettingsWindow.Awake` 第一句就是 `debugButtons.SetActive(false)`（`:46-48`）⇒ **Win / Draw / Console 三颗调试钮在发行版不可达**，以后碰到 `DebugWinBattle` / `DebugDrawBattle` / `ConsoleButtonOnClick` 一律先看这一句。
- `BattleSettingsWindow.Open` 用 `skipTutorialButton.SetActive(matchType - 100 == 0)`（`:225-232`）⇒ **只有教程局显示「跳过教程」**（旧账里「那颗钮常驻」的说法已被订正过）。


---

# §CardView 批（同一会话续做 · 物理行 362 – 1113）

> 切片：`ledger_slice.py --domain 战斗逻辑驱动 --status B --file CardView --limit 0`。
> 本轮开做时该切片 **66 行**（上轮 111 → 68 → 66：我的 40 行升级 + 别人又并了几条，且 **1069 `TransformUnitInPlayToCard` 已被别人改成 C**，不在本批）。
> 产出补丁：`账/_parts/B9_E2c.tsv`（66 行，11 列，LF）。

## ① 结论

| 结果 | 条数 |
|---|---|
| **升 A** | **26** |
| **判 C** | **14** |
| 判 **D** | **14** |
| 留 **B** | **12** |

**落点闸（只读机检）**
```
python -I -X utf8 工具/check_ledger_points.py --warn-getters 资料/完全复刻/账/_parts/B9_E2c.tsv
⇒ A=26 通过=26 漂移=0 告警=0 失败=0
```
`check_patch_conflicts.py` ⇒ 无跨补丁重复键（2 份补丁）。66 个键在主账里**都还是 `B`**（没和别人撞）。

> 途中被闸挡过一次：`PlayRandomAIVoiceLine` 的落点原来写 `:6844（AiEmoteChancePerAction）`——那是 `const float`，闸判「找不到声明」。已改指 `:6966（SpeakAiChat）`（真正随机抽一句那个方法）。

## ② 判 C 的 14 条（都写了「差什么 / 该改哪个文件哪个方法」）

| 原版方法 | 缺口（证据） |
|---|---|
| `HideHeroAtStart(bool)` | 原版开局把督军 `DissolveWholeCard(0.01f)` + `RemoveStatusAnim()`（**从溶解态浮现**）；我们有溶解 API（`CardFeel.Dissolve`）但**开局这一跳没有** ⇒ 少了登场那一拍 |
| `LogActivatedTrigger(bool, AbilityTrigger)` | `challengeLog.LogTriggerAbility(...)`；全仓 **0 命中**（我们只接了 `ChallengeLogMgr.LogMatchEnd`） |
| `MobileAdjPos(Vector3)` | 读 `VarsDevice.targetYOffset` 做移动端 y 偏移；`VarsDevice` **本工程没有对应类**（`Core/ScreenUtilities.cs:28` 已记「零消费者」）—— 与上批 `GetHorizontalOffset` 同一族 |
| `OnApplicationQuit()` | 原版退出即 `SendForfeit()`（联机局告诉对面）；全仓 `grep OnApplicationQuit` = **0 命中**（只有时钟那条 `OnApplicationPause`） |
| `PlayIconAnim(...)` / `ResolveAbilityIconAnim(...)` | 原版那颗**会动的技能图标**（`CardScript.ActivateIconAnim`）；全仓 **0 命中** ⇒ 我们只有静态图标 + 亮/灭 |
| `PlayNextSoundInQueue()` / `PlaySoundAsset(SoundAsset_OLD)` | 原版声音是 **FIFO 队列**（入队 / 出队逐个播）；`Battle/BattleSfx.cs` 是**立刻播 + 按 cue 名去重**，**没有队列** ⇒ 同一拍多条音效会叠（原版串行） |
| `RemoveSelfBuffsFromQueues(CardScript)` | `CancelEffectsFromDisappearingUnit(..., shouldIgnoreDestroy:true)`；全仓 **0 命中** ⇒ 与上批 `CancelAnimFromUnitFromQueue` 同一个「按卡撤队列」缺口 |
| `CardScript.SetEnemySecret()` | 敌方密令的**秘密面**；我们只有卡背/伏击面一档 |
| `CardScript.SetStateInHand()` | **部分缺**：重挂父件 / 状态已有，但 `DisplayCreatedByText`（「由谁生成」那一行）**完全没有**（`BattleDriver.cs:9359` 自查记 0 命中） |
| `CardScript.StopAnimsInPlay()` | `Card3DAnimationController` 只有 `DoAnim`(:208) 起播，**没有 Stop / 中断口** ⇒ 起播后只能等它自己跑完 |
| `CardScript.UnDissolveWholeCard()` / `UndissolveToDeck()` | 溶解只有**单向**（`CardFeel.Dissolve:3133`），`grep Undissolve|UnDissolve` = **0 命中** ⇒ 「从溶解态复原 / 回牌库」整条走不通（我们靠重建视图绕过，会丢运行时状态） |

## ③ 判 D 的 14 条（两类）

**甲 · 有硬判据（3 条）**：`PlaySkullsAnim`（**原版自己只有一句 `LogWarning("[WARNING] Implement")`**）· `SetCardTrackedRotation`（两份反编译都是空体 —— ⚠️ **未做 VA 复核**，已在备注写时间限定）· `SetupTestButtons`（体 = `devButtons.SetActive(false)`，我们根本不建那组钮）。

**乙 · 覆盖说明型（11 条，每条都写了「覆盖了什么 / 没覆盖什么 + 时间限定」）**：`GetBoardCardPosFrom2DPos` · `GetBoardMousePos` · `GetBoardPosFromScreenPos` · `GetBoardPosOfCard` · `GetHeroPos` · `GetSpecificHeroPos` · `HideDeckAtStart` · `SetupSingleton` · `UpdateDeckDisplay` · `SetMulliganSortingLayer` · `ToggleOverrideSorting`。
共同理由：原版那套是 **collider 射线求点** + **牌库/墓地逐卡实体视图** + **逐卡子 Canvas 的 `m_OverrideSorting`**；我们分别是**投影命中**（`CardHit`）、**一块 HUD 底板 + 文本日志**、**渲染队列**。

## ④ 留 B 的 12 条（每条都写了「还差什么」）

- `BattleManager.GetTargetInPos` —— 体被 Ghidra 摊成 `num * 0`，**平面求交系数读不出来** ⇒ 要 VA 反汇编。
- `BattleManager.GetViewportPointFromBoardPos` —— 我们只有 `boardCam.WorldToScreenPoint` 与 `ui.WorldToViewportPoint`（**两台不同相机**）⇒ 没有「boardCamera → viewport」这一口；要裁「按 screen 等价」还是「必须补口」。
- `BattleManager.RemoveDestroyableAnims` —— 要读 `Core/TraitFrames.cs` 的成员表（按 `buffType ∈ {15,16}` + `trait` 取特效物件那一支）。
- `BattleManager.ResolveAnimInfoRangeAttack` —— 原版按 `AnimInfo` 字段**现算**，我们按 `card_anim_order.json` 的 99 拍**表驱动** ⇒ 需要一份「表 vs 解算」的逐档比对。
- `BattleManager.ResolveBloodThirstAnim` —— `ActivateBloodThirst()` 的返回值语义 vs 我们的 `RuleCore.EmitBloodThirst` 没逐句比。
- `BattleManager.SetCardTracked` —— `cardTracked` 我们分散在 `CardHighlight`（悬停）与 `_cardWinSource`（开窗源卡）两处，原版是**单一字段 + 换人时收掉上一张的 `inHandShowing`**。
- `BattleManager.ShowTutorialTip` —— 只有 `DismissTutorialTip(:16825)` 那一半；「弹」那一半没定位。
- `CardScript.ShowCardInCenter` —— 三段补间的时长（VarsGlobal）只核到缩放那一格；手动点牌居中那条入口没定位。
- `CardScript.TryCardHighlight` —— `CardHighlight` 是静态类（`:75`），**按 UIstate 筛**那一层落在 `BattleDriver` 哪个调用点没定位。
- `CardScript.UpdateAssetInPlayPosition` / `UpdateMinionInPlayPosition` —— 落点已到 `Core/CardView.cs:9696（SetPose）`，但 ① 原版有 `DOLocalMove(reassembleTime)` **补间**（我们直接写 transform）、② `originalLocalPosInPlay` 那套「重拼偏移」没核。
- `CardScript.UpdateHealthText` —— 落点是注释（`:1486` 引过 `CardScript.UpdateHealthText`），还没找到写 `_faceStatText[StatHealth]` 的那个方法；`currentMaxHealthProcessed` 的跨帧缓存语义也没核。

## ⑤ 交接（下一批）

1. **先做 C 里最省的两条**：`OnApplicationQuit`（一个方法 + 一句 `_net.OnLocalResign()`，判据已抄在补丁里）与 `StopAnimsInPlay`（`Card3DAnimationController` 加 `Stop()`）。
2. **「按卡撤队列」现在有三条同源缺口**（上批 `CancelAnimFromUnitFromQueue` / `CancelAnimsFromUnit` + 本批 `RemoveSelfBuffsFromQueues`）—— 一起做。
3. **`VarsDevice` 也攒到三条了**（上批 `GetHorizontalOffset` + 本批 `MobileAdjPos` + 牌库/落点上的 `targetYOffset`）⇒ 值得单独立一个数据件，再回头把三条一起接上。
4. **B 里两条只差「读一个文件」**：`RemoveDestroyableAnims`（读 `TraitFrames` 成员表）、`TryCardHighlight`（读 `CardHighlight` 成员表）。
5. 磁盘上还有 **12 条 B**（上表 ④）—— 下一轮可直接照它一条条收。

**其余纪律没变**：本轮**没跑 Unity、没动 git、没改主账、没改任何 `.cs`**；补丁只写 `_parts/B9_E2c.tsv`（合并时**必须带 `--only B9_E2c.tsv`**）。
