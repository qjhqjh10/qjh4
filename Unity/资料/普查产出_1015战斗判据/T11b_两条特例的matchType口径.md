# T11b · `playerAlwaysWins` / `preventPlayerResign` 的 matchType 口径（2026-10-15 · 只读普查）

## 结论（3~5 句）

1. **`playerAlwaysWins` 那两条读点的 `IsCampaignLike` 不是笔误、是刻意的**：原版 `GetWinnerAfterBattleEnd.c:49` 与 `DeadHero.c:270`
   都写 `IsCampaignLike(bm)`（= `matchType ∈ {Campaign 130, Scene 190}`）⇒ **在教程局（`Tutorial 100` / `TutorialReplay 140`）上永不触发**。
   它「看着像教程机制」是因为下一句调的 `GetCurrentTutorialStage` **名字叫 tutorial、实际在 130/190 上返回 `currentCampaignStage`**
   （`GetCurrentTutorialStage.c:19-57`；而 `currentCampaignStage` 的类型**就是** `TutorialStage`，`dump.cs:85903`）⇒ 真语义 = 「**战役/过场关**的 `+0xB8`」。
2. ⇒ **`playerAlwaysWins`：我们与原版【不等价】**。我们 `RuleCore.cs:8727` 判 `ctx.Tutorial != null`，而 `ctx.Tutorial` **只在 `GameMode.Tutorial(4)` ⇒ `matchType 100` 上非 null**
   （`RuleCore.cs:140` 唯一写点 ← `BattleDriver.cs:2599/2610` 唯一 `new TutorialScript`）⇒ 两边触发集合**互不相交**（原版 {130,190} ∩ 我们 {100} = ∅）。
3. ⇒ **`preventPlayerResign`：教程这一档【等价】，战役那一档【我们缺】**。原版用的是 `IsTutorialStageAvailable() || IsCampaignStageAvailable()`
   （`ClickExitBattle.c:50-54`｜= `{100,140}` ∪ `{130,190}`，**含教程**），我们那一支落在它之内 ⇒ 教程局上同效；
   差的是 ① 原版还有 `turnCounter > 0` 且 `turnCounter-1 < turnScriptedData.Count` 两个条件（= **只在脚本回合窗口内才挡**）② 被挡之后的 `actionOnPlayerResign` 我们没做 ③ 战役档。
4. **今天两边都不会现形**：6 关 SO 的 `playerAlwaysWins` / `preventPlayerResign` **全是 false**；而且我们**一局 130/190 都跑不到**
   （`GameMode.Campaign` / `GameMode.CutScene` 全工程**只有枚举定义 + `MatchTypes.For` 两处、零开战入口**，§五）。
5. **该改的是我们这一侧**（§六），但要说清：改完在**当前可达的模式下依旧一次都不触发** —— 它是「先把口径摆正、等战役线」的那种账，不是「今天画面对不上」。

---

## 一、原版两个判据的定义

**`MatchType` 全表**（`d:/2/tools/il2cpp_out/dump.cs:27369-27391`，TypeDefIndex 563）：
`Undefined 0 · Ranked 10 · RankedBot 20 · PracticeOffline 50 · EventAI 80 · Practice 90 · **Tutorial 100** · ClosedDeck 110 · ClosedDeckBot 120 ·
**Campaign 130** · **TutorialReplay 140** · Duel 150 · Replay 160 · Unranked 170 · UnrankedBot 180 · **Scene 190** · FastMode 200 · FastModeBot 210 · FastModeRanked 211`。
⚠️ **`0xA0 = 160 = Replay`** —— 它**不在**这两个判据里；它只出现在 `ClickExitBattle.c:43`（见 §三）。（T11 §六那句「0xA0 也出现过」指的是那里，不是 `IsCampaignLike`。）

| 方法 | 方法体（逐句） | 出处 |
|---|---|---|
| `IsCampaignLike()` | 取 `MatchData.matchType`（`PlayerDataManager +0xB8 → +0x20 → +0x1C`）；**`return matchType == 0x82(130) \|\| matchType == 0xbe(190)`**；没有别的条件 | `BattleManager__IsCampaignLike.c:11-29` |
| `IsTutorialMatch()` | 同一个字段；**`return matchType == 100 \|\| matchType == 0x8C(140)`** | `BattleManager__IsTutorialMatch.c:18-23` |
| `GetCurrentTutorialStage()` 🔴**名字骗人** | `100/140 → PlayerDataManager.get_currentTutorialStage`；**`130/190 → get_currentCampaignStage`**；其余 → `null` | `BattleManager__GetCurrentTutorialStage.c:19-57` |
| `IsTutorialStageAvailable(bm)` | `IsTutorialMatch && currentTutorialStage != null && turnCounter(+0x3F8, ObscuredInt) > 0 && turnCounter-1 < turnScriptedData(+0x48).Count` | `BattleManager__IsTutorialStageAvailable.c:22-75` |
| `IsCampaignStageAvailable(bm)` | **逐句同构**，把上面两处换成 `IsCampaignLike` / `currentCampaignStage` | `BattleManager__IsCampaignStageAvailable.c:22-75` |

**两者共用同一个类**：`TutorialStage`（`dump.cs:42050-42081`）—— `playerAlwaysWins bool 0xB8` · `preventPlayerResign bool 0xB9` ·
`actionOnPlayerResign ScriptedAction 0xC0` · `turnScriptedData List<TurnScriptedData> 0x48` · `uniqueIdCampaign string 0x18`（**战役关就是它**，有 `uniqueIdCampaign`）。
`get_currentTutorialStage` / `get_currentCampaignStage` 的返回类型都是 `TutorialStage`（`dump.cs:85898` / `:85903`）。

**别处的用法（旁证这是「一大类一起判」）**：`IsCampaignLike` 全仓 **8 个调用点**（grep `decomp_full`），其中
`_StartBattleSequence_d__331__MoveNext.c:22-23` 与 `ClockManager__StartTimer.c:15-19`、`EnemyInfoTouch__OnTouchDown.c:10-16` 都是
**`IsTutorialMatch() || IsCampaignLike()`** 的写法（= 「本局是脚本关」的总闸）—— **`preventPlayerResign` 用的正是这个并集**（经两个 `*StageAvailable`）。

## 二、两个读点的上下文（逐字）

**`GetWinnerAfterBattleEnd(bm, param_2 = 该方是否倒下, param_3 = BattleResult)`** —— `BattleManager__GetWinnerAfterBattleEnd.c`：

- `:23-48` 先算**默认赢家**：`param_2 == 0 ⇒ 10 (Player)`、否则 `20 (Enemy)`；中间还有两条覆盖（`+0x228 == 5` ⇒ `0x1e 平局`；血量<1 或 `+0x228==6` 或 `param_3==6` ⇒ 也 `0x1e`）。
  （`BattleWinner`：`Undefined 0 · Player 10 · Enemy 20 · Draw 30`，`dump.cs:45419-45427`。）
- `:49-68` **这就是那一条**：`IsCampaignLike(bm)` → `GetCurrentTutorialStage(bm) != null` → **`*(char*)(stage + 0xB8) != 0`** ⇒ `CustomDebug.LogWarning` + **`uVar6 = 10`（覆盖成玩家胜）**。
- `:69` `return uVar6;` —— **没有任何教程局的读点**。

**`DeadHero(bm, isPlayerDying, BattleResult)`** —— `BattleManager__DeadHero.c:270-284`：**与上面逐字同构的 4 句**
（`:270` `IsCampaignLike` · `:273` `GetCurrentTutorialStage` · `:276` `op_Inequality` · `:280` `*(char*)(stage+0xB8)` · `:281` LogWarning · `:282` `iVar14 = 10`）。
它后面接着 `RankedMatchResult..ctor` / `SetBattleResult` / `SaveMatchWinner` / `BattleFinished`（`:326-363`）—— **覆盖的就是要落盘的赢家**。
⚠️ 同一段也是 `BattleManager.ClickSkip`（跳过整套教程，`playMode == 100` ⇒ `DeadHero(bm, 0, 4)`）走的路 ⇒ **教程局里连「跳过」都不会被这条改成玩家胜**。

⇒ 两处**精确条件相同**：`matchType ∈ {130, 190}` && `currentCampaignStage != null` && `stage.playerAlwaysWins == true`。

## 三、`preventPlayerResign` 的守卫（`BattleManager__ClickExitBattle.c` 全文 91 行）

```
:32  if (matchType != 0x8C (140))            →  :43  if (matchType != 0xA0 (160)) { …下面整段… } else 落到 LeaveBattle
:47    if (IsNetworkedGame) SendForfeit()                       // 联机：先告诉对面
:49    AddResignAction(bm, 1)                                   // 🔴 无条件、在被挡【之前】就记账（挡下也记了）
:50-54 if (IsTutorialStageAvailable(bm) || IsCampaignStageAvailable(bm))  ← 守卫 = 并集
          :55-57  stage = GetCurrentTutorialStage(bm)
                  if (stage.preventPlayerResign(+0xB9) == 0) → :59 DeadHero(bm, 1, /*Forfeit*/2)   // 正常判负
                  else :62 LogWarning
                       :65 if (stage.actionOnPlayerResign(+0xC0) == 0) return;                    // 静默吞掉
                       :71 AiScripted.ExecuteAction(aiScripted(+0x250), bm, actionOnPlayerResign, 0)
       else → :59 DeadHero(bm, 1, 2)                            // 两关都不在跑 ⇒ 正常判负
```

**⇒ 与 `playerAlwaysWins` 的写法【不同】**：这条用的是 **`IsTutorialStageAvailable() || IsCampaignStageAvailable()`**（含 `100/140`），
而 `playerAlwaysWins` 用的是**裸 `IsCampaignLike()`**（只有 `130/190`）。**两条特例的 matchType 面不重合**，别一起改。
（`140 TutorialReplay` / `160 Replay` 在 `:32` / `:43` 就提前 `LeaveBattle` 了 —— 但 `140` 同时又是 `IsTutorialStageAvailable` 认的一档，
只有在它**不是** replay 播放场景时才走得到；这是原版自己的两义，本报告只如实记。）

## 四、我们这一侧（逐字）

- **`RuleCore.Forfeit`（`RuleCore/Core/RuleCore.cs:8645-8671`）**：`:8655` `if (ctx.Tutorial != null && ctx.Tutorial.ResignBlocked) { …; return ctx.Winner; }`
  ⇒ 判的是「**本局挂了教程执行器**且该关 `preventPlayerResign`」；**只挡、不判负**（与原版那一支同向），
  **缺**：原版的 `turnCounter > 0` / `turnCounter-1 < turnScriptedData.Count` 两个界、以及 `actionOnPlayerResign`（T11 §八 G-1）。
- **`RuleCore.CheckWinner`（`:8691-8760`）**：`:8727` `if ((d0 || d1) && ctx.Tutorial != null && ctx.Tutorial.PlayerAlwaysWins) { ctx.Winner = 1; return ctx.Winner; }`
  ⇒ 判的是「**本局是教程局**且该关 `playerAlwaysWins`」⇒ `Winner = 1`（= 我方胜，`Forfeit` 那族里 `1` 就是座位 0 胜，与 `Forfeit` 的 `:8663` 同一套编号）。
- **`ResignBlocked` / `PlayerAlwaysWins` 的定义**：`TutorialScript.cs:1220` / `:1225`，两条都只读 `Stage.preventPlayerResign` / `Stage.playerAlwaysWins`（`TutorialStageData`，数据来自 `Resources/tutorial_stages.json`）。
- 🔴 **`ctx.Tutorial` 在哪些模式下非 null**（这是「差异面有多大」的关键）：
  **写点只有一处** —— `RuleCore.cs:140`（`ctx.Tutorial = tutorial;`，注释自己写着「唯一写点」）；值来自 `BattleDriver.Begin(…, tutorial:)`（`BattleDriver.cs:2757` 形参 → `:3099` 传给 `NewBattle`）。
  而**传 `tutorial:` 的只有两处**：`BattleDriver.cs:2610`（`BeginTutorial` 内，`:2599` `new TutorialScript(stage)`、`:2609` **`playMode: GameMode.Tutorial`**）与 `:7405`（`Restart` 原样带过去）。
  `BeginTutorial` 的调用者 = `TutorialModePopup.cs:734-735`（`SetPendingTutorialStage(i)` + `SetPendingPlayMode(GameMode.Tutorial)`）与录像回放 `BattleDriver.cs:689`（`rec.TutorialStageIndex >= 0` 时）。
  ⇒ **`ctx.Tutorial != null` ⟺ 本局 `playMode == GameMode.Tutorial(4)` ⇒ `matchType 100`**（`MatchTypes.For`：`BattleContext.cs:1525`）。**一档，仅此一档。**
- **我们的 `MatchType` 枚举**：`BattleContext.cs:1483-1504`，**逐字照抄那 19 档**，一个数都没改；
  `MatchTypes.For`（`:1517-1543`）是唯一的「`GameMode` → `MatchType`」表，**没有把 130/190 与 100 混在一处**。
  ⚠️ 但**全工程没有 `IsCampaignLike` 的对应物**（grep `IsCampaignLike` 在我们 `Assets/` 下 = **只命中 `RuleCore.cs:8721` 一句注释**）⇒ **我们这侧根本表达不了「战役/过场关」这件事**。

## 五、差异的实际影响（逐 matchType 一档一行）

先定「**我们跑得到哪几档**」：开战入口只有 5 条 —— 排位 `RankedEventWindow.cs:276`（`Classic 0`）· 遭遇 `SkirmishEventWindow.cs:292`（`Skirmish 13`）·
练习 `PracticeModePopup.cs:1860`（`OfflinePractice 6` / `OwnDeckTraining 12`）· 教程 `TutorialModePopup.cs:735`（`Tutorial 4`）。
⇒ 可达的 `matchType` = **`10/20`（排位，回合 1 由 `MatchTypes.Effective` 翻成 `20`）· `50`（两处练习）· `200/210`（遭遇）· `100`（教程）**。

| matchType | 原版 | 我们 | 玩家能看到的不同 |
|---|---|---|---|
| `0 / 10 / 20 / 50 / 80 / 90 / 110 / 120 / 150 / 170 / 180 / 200 / 210 / 211`（**收得到的那 4 档都在这里**） | 两条特例**都过不去守卫**（两个 `*StageAvailable` 假、`IsCampaignLike` 假）⇒ 无任何特例行为 | `ctx.Tutorial == null` ⇒ 两条都恒空转 | **零** |
| **`100 Tutorial`**（我们收得到） | `playerAlwaysWins` **永不触发**；`preventPlayerResign` **触发**（`IsTutorialStageAvailable` 为真时） | 两条都按关卡的 flag 触发 | 今天：**零**（6 关两 flag 全 false）。**潜在**：若哪关把 `playerAlwaysWins` 置 1，我们会当场判玩家胜，原版**不会**；若在脚本回合窗口外（`turnCounter-1 >= turns.Count`）投降，原版会判负、我们会挡下 |
| `140 TutorialReplay` | `playerAlwaysWins` 不触发；`preventPlayerResign` 认（`IsTutorialMatch` 含 140，但不是 `ClickExitBattle:32` 那条 replay 播放路时） | 我们**没有这一档的开战入口**（`GameMode.TutorialReplay` 全仓只有 `GameplayVariables.cs:374` 一处引用） | 收不到 |
| `130 Campaign` | **`playerAlwaysWins` 唯一真正生效的档**（`campaignStage != null && +0xB8`）；`preventPlayerResign` 也生效（`IsCampaignStageAvailable`） | 我们**收不到这一档**（`GameMode.Campaign` 只有枚举 + `For` 两处、零开战入口；`CampaignTab` 是奖励页，不开战） | 收不到；**但将来接战役线时，两条都会被我们漏掉** |
| `160 Replay` | 两条都到不了（`ClickExitBattle.c:43` 提前 `LeaveBattle`） | 无此模式 | 零 |

**另外一条与 matchType 无关但同族的差异**（顺带记，来自 §四）：我们两条守卫**都没有「脚本回合窗口」这个界** —— 原版靠 `IsTutorialStageAvailable`
把 `turnCounter == 0`（开局/换牌期）与「跑到脚本回合之外」两种情况**排除在机制之外**，我们一律算数。

## 六、明确结论

**不等价**（不是「各有道理」，是两边判的东西不是一回事）：

1. **`playerAlwaysWins` —— 不等价，该改我们这一侧。**
   依据：原版两处读点（`GetWinnerAfterBattleEnd.c:49` / `DeadHero.c:270`）逐字是 `IsCampaignLike`，而 `GetCurrentTutorialStage` 在 130/190 上取的是 `currentCampaignStage`
   （`GetCurrentTutorialStage.c:36-56`），⇒ **原版这条机制的作用面 = 战役/过场关**，**教程局上恒不触发**。
   我们 `RuleCore.cs:8727` 写的是「教程局」，**恰好是原版唯一不生效的那一档**。改法 = 把守卫换成「本局跑的是**战役/过场关**（`matchType 130/190`）且该关 `playerAlwaysWins`」；
   ⚠️ 由于我们**没有战役关这个概念**（§四最后一条），这句话的落地 = **先有战役关那条线**；在那之前，**最小正确改动** = 让这一支**不再对 `GameMode.Tutorial` 生效**（照原版），并**出声**说明「本工程还没有战役关 ⇒ 这条机制当前恒不触发」。
2. **`preventPlayerResign` —— 教程档等价、两处不全。** 该改我们这一侧：
   ① 补原版那两个界（`turnCounter > 0` 且 `turnCounter-1 < turnScriptedData.Count`）—— 我们的 `TutorialScript` 有回合指针，判据现成；
   ② 补 `actionOnPlayerResign`（原版被挡不是「什么都不做」，`ClickExitBattle.c:71`；T11 §八 G-1 已记）；
   ③ 战役档同 1（等战役线）。
   ⛔ **别把这两条特例的 matchType 面写成同一个** —— 它们的并集/交集关系见 §三、§五。

## 七、没查清的

1. **原版**战役关（`uniqueIdCampaign != ''`）的 SO 里 `playerAlwaysWins` / `preventPlayerResign` 到底是 1 还是 0 —— **本地没有**。
   搜过：`d:/2/新解包资源/assets_full` 全库（91 个 bundle）`grep -rl playerAlwaysWins` / `preventPlayerResign` / `uniqueIdCampaign`，
   命中**只有 7 份**，全在 `bundle_tutorialso_assets_all/MonoBehaviour/`：`Warpforge_TutorialStage1..6.json` + `Scenary_Test.json`
   （`Scenary_Test` = 一份过场关（`bg=newBg_Tutorial`、1 个 `turnScriptedData`），两 flag **都是 0**；7 份的 `uniqueIdCampaign` **全是空串**）。
   ⇒ 战役关的 SO 不在离线包体里（与本地化表同类：由远端下发）。**因此「原版这条机制在生产数据上是否真的被用过」判不出来**。
2. **`GetCurrentTutorialStage` 那个「非 100/140/130/190 就返回 0」的分支在调用点上的实际后果**没逐点追（两个读点都有 `IsCampaignLike` 前置，所以理论上到不了 `null` 那一支）。
3. **`AddResignAction(bm, 1)`（`:49`）做什么**没查（它在被挡之前就执行 ⇒ 挡下也记了一笔；是不是「通知对面 / 记战斗日志」没读）。

## 八、顺手发现（照实报，⛔ 我没改任何东西）

1. 🔴 **`GetCurrentTutorialStage` 这个名字是错的**（`BattleManager__GetCurrentTutorialStage.c:19-57` 明写 130/190 → `get_currentCampaignStage`）。
   T11 §六把这条机制的语义写成「`GetCurrentTutorialStage` = 教程关」，**读名字会读反**；T11 §六末句「关掉它的是 `IsCampaignLike`」结论本身是对的。
2. **`IsCampaignLike` 还有 6 个别的调用点**，全是「战役/过场关特判」：`ResolveEndTurn.c:434`（回合结束时的另一条分支）·
   `ClockManager__StartTimer.c:15/19`（**战役/过场关与教程局都不启计时器**）· `EnemyInfoTouch__OnTouchDown.c:10/16`（两种局都**点不开敌人信息**）·
   `_StartBattleSequence_d__331__MoveNext.c:23`（与 `IsTutorialMatch` 并集 = 起手序列）· `IsCampaignStageAvailable.c:22` · 上面那两处。
   ⇒ 将来接战役线时，**这几条会一起冒出来**，别只当 `playerAlwaysWins` 一条。
3. **`ClickExitBattle.c:32/:43`**：`matchType == 140`（TutorialReplay）与 `== 160`（Replay）**提前 `LeaveBattle`**，
   而 `AddResignAction` / 判负 / `actionOnPlayerResign` 那整段都进不去 ⇒ **回放态没有「投降」这条路**。
4. `RuleCore.cs:8721` 那句注释**已经照原版写对了**（写着 `IsCampaignLike`），只是**代码判的是 `ctx.Tutorial`** —— 注释与实现不一致，改的时候以注释为准即可。
