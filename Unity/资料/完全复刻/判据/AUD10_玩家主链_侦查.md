# AUD10 — 玩家主链（E2E-01）静态侦查

> 代理 `B105_R8`（只读）· 2026-10-31 · 产出 = 本文件 + `报告/B105_R8.md`
> 范围：**主菜单 → 建/选卡组 → 选模式 → 匹配/开战 → 换牌 → 打一局 → 结算 → 返回主菜单 → 再开一局**，
> 逐模式各一条，逐跳给「入口 → 调什么 → 返回值谁消费 → 失败出口 → 状态是否复位」。
> ⛔ 没跑 Unity、没跑 typecheck、没动任何 `.cs`/账/正本。行号 = 本日现读。

---

## 〇、判定口径

| 档 | 含义 |
|---|---|
| **真缺口** | 生产路径上某一跳**不存在**（0 调用点 / 返回值被丢 / 失败无出口），且有玩家可见后果 |
| **等价** | 原版有、我们没有同名的那一跳，但本地有等价实现且已接线（写清等价物） |
| **判不了** | 判据在远端 / 数据实例本地不存在 / 必须真机才能分辨 —— 写清缺什么 |

两条前提（沿用 `B104_本会话待办与裁定.md` §二）：
1. 账里的 **A = 「我们有等价实现」，不等于「生产走到它」** —— 本次侦查的全部价值都在**边**上。
2. 「窗口活着」这件事在本仓成立：**壳 `DontDestroyOnLoad`**（`ShellRuntime.cs:345`），
   而 `WindowsManager` / 三个锚点 / 所有窗都挂在壳下（`WindowsManager.cs:99-108, 124-154`）
   ⇒ **模式窗跨战场场景存活**。这一条是下面几条「状态残留」的物理前提。

---

## 一、链路图（逐模式）

### 公共前段（所有模式共用）

| 跳 | 入口 | 调什么 | 返回值消费 | 失败出口 |
|---|---|---|---|---|
| M0 | `MainMenuRuntime.Awake` `:55` | `LiveOpsManager.EnsureStarted()`（注册 `LiveOpsLocalHandler` + `GameModesHandler`，`LiveOpsManager.cs:92`） | — | 注册失败 `LogError`（`LiveOpsManager.cs:392`） |
| M1 | `MainMenuRuntime.Start` `:57-61` | `Build()` `:401` → `RegisterMainMenuWindow` `:694`；`IngestPracticeResultsAtMenuStart` `:4581` | — | handler 取不到 ⇒ `LogWarning`（`:4586`） |
| M2 | 左竖导航 `OnNavClick` `:2690+`：0 Home `:2701` · 1 收藏 `:2704` · 2 商店 · 3 奖励 · 4 社交 · 5 活动 `:2853` | `OpenHome/OpenCollection/OpenShop/OpenRewards/OpenSocial/WindowsManager.OpenEventHub(Parent)` | — | `default` 支出声 |
| M3 | 模式卡 `OpenMode(kind)` `:2686` | tutorial `:2703` · practice `:2710` · skirmish `:2715` · ranked `:2721` · draft `:2737` · event `:2748`，全部走 `WindowsManager.OpenByRef`（已开 ⇒ 复用；已关 ⇒ **销毁并新建**，`WindowsManager.cs:943-963`） | — | 无 `WindowsManager` ⇒ `LogWarning` + 返回（`:2689`） |

### ① 练习（OfflinePractice 6）

| 跳 | 位置 | 说明 |
|---|---|---|
| 1 | `PracticeModePopup` 建窗 `Open()` `:1241` | **重设** `PickedPrebuilt`/`DeckIndex`/`ArmyIndex`/`SelectedGameMode` + `PrebuiltDecks.ClearPendingBattleDeck()`；**不重设 `StartedBattle`**（见 §三 R1） |
| 2 | `BattleHit` `:1440` → `StartBattle()` `:2944` | 督军闸 `:2952-2963`（有可见弹窗）；`SaveSelectedDeck()` `:2969`；`StartPracticeMatchNoCheck(deckId)` `:2975`（返回值**未消费**，见 §二 #6） |
| 3 | 联机接管支 `:2997-3028` | `NetMatchmaking.TryStart` 真 ⇒ 窗内搜索窗 `BeginNetWait`，**不跑 12 秒**，**不设** `PendingPlayMode`（模式随开局包走） |
| 4 | 搜索 `:3029-3047` | 有 `OpponentDeck` ⇒ 1 秒（原版 `waitLoadTime=1`），否则 12 秒；推进方 = 本窗 `Update`（`:3086`） |
| 5 | `StartBotBattle()` `:3091` | `CollectionData.Select(DeckIndex)` **失败 ⇒ 回滚 + 错误弹窗 + 挡住开战**（`:3116-3124`，B104_P4 已修）；`SetPendingOpponentDeck`；`SetPendingPlayMode(PlayMode)` `:3149`；`StartedBattle = true` `:3150` |
| 6 | 切场 `:3161-3165` | 批处理 return；真机 `ShellRuntime.LoadSceneWithFade`（有 try/catch，`ShellRuntime.cs:205-220`） |

### ② 遭遇（Skirmish 13）

| 跳 | 位置 | 说明 |
|---|---|---|
| 1 | `LiveOpsEventWindow.PlayButtonClicked` `:2329` → `StartMatch()` `:1555` | 模式闸 `:1561`（可见弹窗）· 督军闸 `:1568` · 联机支 `:1580-1599` |
| 2 | `_search.OnSearchDone = () => { OnSearchFinished(); StartBotBattle(); }` `:1601` | `OnSearchFinished` 覆写 = `SkirmishEventWindow.cs:852`，里面 `SetPendingPlayMode(Skirmish)` `:855` |
| 3 | `StartBotBattle()` `:1685` | `CollectionData.Select(DeckIndex)` **返回值被丢**（`:1693`，见 §二 #2）；`StartedBattle=true` `:1694`（**无读者**）；切场 `:1707` |
| 4 | 取消 `CancelSearch` `:1637` | `_search.AbortSearch()` + 联机拆局；`Close()` `:578` 会先 `_search.Cancel()` |

### ③ 排位（Classic 0）

| 跳 | 位置 | 说明 |
|---|---|---|
| 1 | `RankedEventWindow.StartMatch()` `:379` → `base.StartMatch()` | 同上闸；`OnSearchFinished` 覆写 `:427` 里 `SetPendingPlayMode(Classic)` `:430` |
| 2 | 搜索窗 `OpenSearchWindow` `:398` | 全屏 `SearchingOpponentWindow`；12 秒由常驻 `SearchOpponentManager` 推（W64 修） |
| 3 | 结算 | `BattleDriver` 结算块 `:32516-32525`：`RankedData.IsRanked` 为真才 `ApplyMatchResult`（`_isRanked` **默认 false** ⇒ 新档打排位不计分，需玩家开 `ChangeRankedToggle`） |

### ④ 教程（Tutorial 4）

| 跳 | 位置 | 说明 |
|---|---|---|
| 1 | `TutorialModePopup.PlayTutorial()` `:730` | `SetPendingTutorialStage(StageIndex)` `:735` + `SetPendingPlayMode(Tutorial)` `:736` |
| 2 | 切场 `:747` | **裸 `SceneManager.LoadScene`**（不像练习/遭遇走 `LoadSceneWithFade`）⇒ 场景缺失时无兜底（见 §二 #7） |
| 3 | 战场 `Start` → `BeginFromDeckLibrary` `:4514` → `TakePendingTutorialStage` `:4523` → `BeginTutorial` `:4524` | 三处 `LogError` + `return`（关卡/两副牌取不到就**不开局**，`BattleDriver.cs:4676/4692/4703`）⇒ 玩家停在空战场（见 §二 #8） |
| 4 | 结算 | 与普通局同一套；`TutorialSkipsNormalEnd()` 决定跳过开门演出 |
| 5 | 教程进度 | **不记**（见 §二 #5） |

### ⑤ 活动（活动枢纽）

| 跳 | 位置 | 说明 |
|---|---|---|
| 1 | 左导航第 6 颗 `OnNavClick(5)` `:2853` → `WindowsManager.OpenEventHub(Parent)` | 只开 UI 壳 |
| 2 | `ParentEventWindow` / `Gacha` / `Raid` / `ExpansionPass` | **全部无战斗入口**；本地无在期事件 ⇒ 页面留空并出声（`ParentEventWindow.cs:46,54,98`） |

⇒ **「活动」今天不构成一条战斗链**（判不了：数据在远端）。它只验「枢纽能开、不崩」。

### ⑥ 自建卡组（= 练习/遭遇/排位里选「我的卡组」）

| 跳 | 位置 | 说明 |
|---|---|---|
| 1 | 主菜单 → 收藏 `OpenCollection` | 卡组页 |
| 2 | `CollectionWindow.GoEdit(i)` → `RouteDeckEntry` → `DeckEditingWindow.OpenFrom` | **有壳 ⇒ 开窗、不切场景**；无壳才 `LoadScene("DeckEditor")`（`CollectionWindow.cs` 的 `GoEdit` 段） |
| 3 | 编辑器 Done → `DeckRuntime.TryClose`（查脏）`：见 `DeckRuntime.cs:5680` | 脏 ⇒ 弹窗；`LeaveByHostWindow` `:5300` 先 `InvalidateLibrary` 再关窗，底下收藏窗由 `ShowPreviousWindow` 带回 |
| 4 | 再进任一模式 → `StartBotBattle` 的 `CollectionData.Select` + 开战 `PickSavedDeck` | **写盘失败 ⇒ 战斗拿盘上旧卡组**（练习侧已挡；事件侧未挡，§二 #2） |

### 公共后段（战场 → 结算 → 回菜单）

| 跳 | 位置 | 说明 |
|---|---|---|
| B1 | 战场 `Start` `:2998` | 回放优先 `ReplayStore.TakePending()`，否则 `BeginFromDeckLibrary()` `:3015` |
| B2 | `BeginFromDeckLibrary` `:4514` | `TakePendingTutorialStage/TakePendingPlayMode` **先取再分支** `:4523` · 教程 `:4524` · 联机包 `:4528` · 预组 `:4543` · 否则 `PickSavedDeck` `:4551`（读不出来 ⇒ 提示行说明「自动凑一副」）· 种子每局换 `:4582` · `Begin(...)` `:4592` |
| B3 | 换牌 `TickMulligan` `:8883` / `OpenMulligan` `:8976` | 倒计时归零 ⇒ 走「点完成」同一条路 `:8890-8896` |
| B4 | 结算闸 `UpdateHud` 的 `if (Ctx.IsOver && …)` `:32235` | `_settled` 一局一次 `:32281`；回放支只演不记账 `:32284-32298` |
| B5 | 真打记账 `:32308-32364` | `StopBattleAfterBattleEnd` → `ShowEndPanelAndFreezeInput` `:32318` → `DailyData.UpdateChallengeProgress` `:32347` → `RecordBattleLog(); NotifyPracticeMatchWonIfAny(); RecordVictoryCounterIfAny();` `:32356` → `AwardAchievements(); FeedMissionChallengeSignals(); GrantMatchXp()` `:32360` → `RecFinish` `:32364` |
| B6 | `ShowEndPanelAndFreezeInput` `:32389` | `CloseAllWindows()` `:32402`（**模式窗在这里被关**）· 联机等对手窗 `:32415-32429` · 排位结算 `:32516` · `EndPanel.Show` `:32548` · `TouchInputManager.Toggle(false)` `:32576` · `ReleaseCardbacks` `:32581` |
| B7 | 出口 `HandleEndBattleExit` `:12758`（闸 = `EndPanel.ExitReady` `EndPanel.cs:208` = `Visible && Doors.Finished`；无视频 ⇒ 立刻开闸 `BattleDoors.cs:354-356`） | 左键任意处 / ESC ⇒ `LeaveBattle()` |
| B8 | `LeaveBattle` `:12807` | `LeaveNetRoom` · 批处理只记账 · 真机 **裸 `SceneManager.LoadScene("MainMenu")`** `:12824`（**没有** `CanStreamedLevelBeLoaded` 守卫，见 §二 #7） |
| B9 | 回到主菜单 | 新 `MainMenuRuntime` → `Start` → `Build` + 练习战绩回灌 `:4581`；`TakePendingReturn()` `:608`，`Collection` 意图 ⇒ 重开收藏窗 `:621-633` |
| B10 | 再开一局 | 见 §三：`BattleDriver` 随场景重建（每局字段全清，`:5081-5167`），但**静态通道**与**壳下常驻窗**不重建 |

---

## 二、断点清单

| # | 跳点 | 现象 | 判据（原版 / 我方） | 玩家可见后果 | 定性 |
|---|---|---|---|---|---|
| **1** | B5 ↔ `GameModesHandler` | **结算从不调 `GameModes.CombatFinished`**；且 `SetAsCurrentlyPlayingEvent` **全仓 0 生产调用** ⇒ `CurrentPlayingEvent` 恒 null ⇒ 即便调了也早退 | 原版 `decomp_full/BattleManager__ProcessMatchResult.c:177` `GameModes__CombatFinished(...)`（非教程 / 非 0xA0 / 非 0x96）；`_tmp_view/decomp_b91/PlayEvent_object__object_.StartMatchNoCheck__1127900.c` 首句 `GameModes__set_CurrentPlayingEvent`。我方 `GameModesHandler.cs:163` / `:315` 只被 `Editor/GameModesCheck.cs` 调 | 事件级结算（`FastMode` 胜负计数 / 连败 / 里程碑；`PracticeEvent.OnMatchFinished` 那一跳）**全部不触发**。练习/轮抽/排位各自另有本地回灌（`BattleLogData` / `RankedData`）⇒ 今天**只有「遭遇（FastMode）」这一族完全没人记账**（`FastModeBaseEvent.cs:291` 的 `OnMatchFinished` → `:306 RecordMatchFinished` → `:220/228/234` 三格，全部只经由这条死链） | **真缺口**（调用点缺失；且本地无 `FastMode` 事件实例 ⇒ 就算接上也无对象可分派 —— 判据侧要分两笔：① 调用点 ② 事件实例来源）。⛔ 与账 215/221（`其他_基础设施.tsv`）不冲突：那两行 A 只说「有等价实现」 |
| **2** | ②-3 / ⑥-4 `LiveOpsEventWindow.cs:1693` | `if (pre == null) CollectionData.Select(DeckIndex);` —— **返回值被丢** | 原版 `DeckUtility` 写盘那半边在服务器；我方 `CollectionData.Select` 明确返回 `false` = 「内存换了、盘上没换」（`CollectionData.cs` 的 `Select`，`Lib.Save()` 失败支） | 选卡组写盘失败 ⇒ **这一局打的是盘上旧卡组**，界面显示新卡组。练习侧已按同一条 B104_P4 修好（`PracticeModePopup.cs:3116-3124`）⇒ **同族两处只修了一处** | **真缺口**（已知项 `B104 §一 #4` / P5 家族，**尚未修**；此处给出 P4 修法可对照） |
| **3** | ③/② 结算 | 排位计分闸 `_rankedSettled` **`Begin()` 不复位**（全类只在 `:32522` 写一次；`Begin` 的清零块 `:5081-5167` 里没有它） | 对照同族：`_settled` `:5086`、`_leaving` `:5128`、`_endPanelGateDone` `:5137` 都在这里清 | 同一台 driver 跑第二局 ⇒ **排位分数不再结算**。今天唯一在场景内再 `Begin` 的入口是 `Restart()`（`R` 调试键 `:14403`）⇒ 产品路径**当前不可达**，但任何「场景内再来一局」的入口（含未来的「继续」）都会静默丢分 | **真缺口（潜伏）** |
| **4** | B6 `CloseAllWindows` `:32402` | 用 `WindowsManager.Instance` 而不是统一读口 `Get()`（`:82`） | `WindowsManager.Get()` 的注释明写「凡是要**用**窗口层的读点一律走这里」 | 结算那一刻若 `Instance` 为 null（壳被拆那一支），**收窗整条跳过** ⇒ 模式窗留在屏上盖住结算面板 | **判不了**（需要真机/时序证据；静态上「壳 DDOL」⇒ 通常非 null） |
| **5** | ④-5 教程进度 | 教程关打完**不记完成**：`TutorialModePopup.CompletedCount()` **硬编码 0**（`:669`）；`DemoDataStore.SetTutorial`（`DemoData.cs:158`，原版 `DemoData.tutorialsCompleted` 的本地等价）**唯一写点是菜单侧 FTUE**（`MenuTutorialFlow.cs:83/436`），与 6 关教程不同键空间 | 原版写入口是云脚本 `CloudHandle.SetTutorialState`（`cpp2il/…/PlayerDataManager.cs:4252` 一带）⇒ **P3**；本地读口 `DemoData.IsTutorialCompleted`（`DemoData.cs:61`）有 | `Completed: n/6` 恒 `0/6`，打完一关回到窗里看不出任何进展 | **判不了 / 服务器相关**（18·0 允许本地模拟：接 `DemoDataStore.SetTutorial(stage)` 即可；但触发时机 = 原版 `TutorialEvent` 完成位，本地无实例） |
| **6** | ①-2 `PracticeModePopup.cs:2975` | `StartPracticeMatchNoCheck(...)` 的 `bool`（2026-10-31 `B104_P3` 刚改成有返回值，表示「`matchStarted` 有没有真写进盘」）**被丢**；方法内三处写盘点都过了 `NotePracticePersist`（会 `LogError`），所以只是「返回值没人用」 | 对照同族处理过的 `RecordBattleLog`/`Select` 口径 | 无直接玩家可见后果（日志里已有 `LogError`）；属「返回值没消费」的**同族残余** | **等价/低危**（建议记一笔，不必单开） |
| **7** | ②-3 / ④-2 / B8 切场景 | **三条路三种写法**：练习/遭遇/排位走 `ShellRuntime.LoadSceneWithFade`（try/catch + 兜底淡回，`ShellRuntime.cs:205-220`）；教程 `TutorialModePopup.cs:747` 与 `LeaveBattle` `:12824` 是**裸 `SceneManager.LoadScene`**；`DeckRuntime.BackToMenu:5274` 有 `CanStreamedLevelBeLoaded` 守卫 | 同一件事三种口径（铁律「两处写同一条规则 = 迟早不一致」） | 场景不在 Build Settings 时：**教程点 Play 后黑屏/停在菜单无提示**；**打完一局出不去（结算面板永远停在那儿，`_leaving` 已置真 ⇒ 再点也没反应）** = 软锁 | **真缺口（④ 没补失败出口）** |
| **8** | ④-3 `BattleDriver.cs:4676/4692/4703` | `BeginTutorial` 三处失败只 `LogError` + `return` ⇒ 战场场景已加载、`Ctx == null`，`Update` 首句 `if (Ctx == null) return;`（`:14264`）⇒ **没有任何 UI、没有出口** | 同族先例：`BeginFromPendingCore(null)` 也是只 `LogError`（`:4078`） | 玩家停在**空战场**，只能杀进程 | **真缺口（④）** —— 同 #7 一族：失败出口应回主菜单或弹窗 |
| **9** | ⑤ 活动 | 活动枢纽**没有战斗入口**（本地无在期事件） | `ParentEventWindow.cs:46/54/98` 文件头自述 | 玩家在活动页看不到任何可玩内容（有出声，不是静默） | **判不了（远端数据）** |
| **10** | M2 `OpenEventHub` `:2853` | 左导航第 6 颗点开 `ParentEventWindow`，与模式卡 `"event"` 支（`:2748`）**同一条路** —— 已收口，无第二份 | — | — | **等价**（记一条「已收口」，防止后人再建第二份） |

### 与既有清单的关系（⛔ 别当新发现重复立账）
- #2 = `B104_本会话待办与裁定.md` §一 **#4** / P5 家族（**未修**）。
- 同族已修完的：练习侧 `Select` 失败（P4）、`DeckEditingWindow.Close` 绕过查脏（P26，`DeckRuntime.cs:5305` 已走 `CloseFromRuntime`）、事件窗 `Select` 丢返回值之外的三条 P4 名字当 id。
- #1 与账 `其他_基础设施.tsv:215`（`CombatFinished` A）不矛盾，但**该行的「备注」只写了「服务器动作的编排口」**，没写「生产 0 调用点」——建议给那一行补一句，否则下个会话会拿 A 当已接线。

---

## 三、状态残留清单（返回后没复位 / 跨局泄漏）

> 前提：壳 DDOL（`ShellRuntime.cs:345`）⇒ **窗与静态字段都跨场景活着**。`BattleDriver` 本身随场景重建。

| # | 载体 | 残留 | 后果 | 可达性 | 定性 |
|---|---|---|---|---|---|
| R1 | `PracticeModePopup.StartedBattle` | **`= true` 之后再无 `= false`**（`:3150` 写、`:1316` 读、`:1203` 声明；`Open()` `:1241` 不重置） | 关窗时「不清 `PickedPrebuilt` / 不清 `PrebuiltDecks._pending`」那道闸失效 ⇒ 预组通道漏到别的模式（正是 `:1290-1292` 那段注释警告的「闸验自建、发牌预组」） | 需要一个「`StartedBattle=true` 之后窗没被 `CloseAllWindows` 关掉」的窗口 —— 即**切场景失败**（#7）那一档，或将来任何「不开战就离开」的新入口 | **真缺口（潜伏）** |
| R2 | `PrebuiltDecks._pending`（静态） | 只在「取」（`BeginFromDeckLibrary:4543`，仅非教程非联机支）与「窗关/开」时清 | 联机局/教程局**不取** ⇒ 若玩家在练习窗选过预组再走联机，通道留着；下一场单机若窗没关就串味 | 练习窗关窗会清 ⇒ 需要「联机局结束但窗没走 Close」 | **判不了**（依赖窗生命周期） |
| R3 | `BattleDriver._pendingPlayMode`（静态） | `Take` 只在 `BeginFromDeckLibrary:4523`；**回放支 `Start():3013-3014` 不取**；切场景失败也不清 | 下次开战沿用上一场的模式号（影响 `Ctx.PlayMode` ⇒ 骷髅档 / 里程碑 / `InstallMissionChallengeMatchGates`） | 先 `StartBotBattle`（设了通道）→ 场景没切走 → 放一局录像 → 再开一局 | **真缺口（潜伏）** |
| R4 | `BattleDriver._rankedSettled` | `Begin` 不复位（见 §二 #3） | 同 driver 第二局不计排位分 | `Restart()`（`R` 键） | **真缺口（潜伏）** |
| R5 | `BattleDriver._consumedPracticeMatches` + `PracticeSave` | 游标不落盘（`GameModesHandler.cs:590` 自述），每次进程启动**重放全部** `BattleLogData` 练习场次；计数不落盘 ⇒ 靠重算 | 自洽（不重复计），但 `DraftModeUnlocked` 也靠重算 | 正常 | **等价**（设计如此，别改） |
| R6 | `WindowsManager._openByRef` | 键 → 窗实例；窗「已关」⇒ 下次 `OpenByRef` **销毁重建**（`WindowsManager.cs:951-956`）⇒ 窗内状态自然作废 | — | — | **等价**（这条是 R1 能否被观察到的前提：若窗真被关掉，R1 会被「新建一扇」掩盖） |
| R7 | `SearchOpponentManager` 静态注册表 | `AbortSearch` 注销（`SearchingMatchPopup.cs` 的 `AbortSearch`），`Close()` 会先 `_search.Cancel()`（`LiveOpsEventWindow.cs:578`） | 不注销 ⇒ 关窗 12 秒后又开一局（W64 已修） | — | **等价**（已修，记一条别再退回） |
| R8 | `LiveOpsEventWindow.StartedBattle` | **写于 `:1694`，全类/全仓无读者**（`grep` 只命中声明与赋值） | 无行为；与 `PracticeModePopup` 同名不同义，容易误导后人 | — | **顺手发现（死字段）** |
| R9 | `CollectionData.PendingReturn` | `DeckRuntime.BackToMenu` 在**无意图**时兜底写 `Collection`（`DeckRuntime.cs:5262-5263`）；两条腿各消费一次（`TakePendingReturn`） | 独立场景那条腿若被跑到，回主菜单会**无条件重开收藏窗** | 生产已走「有壳 ⇒ 关窗」腿 | **等价**（有注释说明） |
| R10 | `MainMenuRuntime._pendingBaseMenu/_pendingRoot`（静态） | 每次 `Build` 覆盖；`ApplyBaseMenu` 有「不是本棵树 ⇒ 不抢」闸（`:735`） | — | — | **等价**（已挡） |

---

## 四、可静态验证的断言建议（批处理可跑）

> 全部按「能钉住上面某一条断点」来写，⛔ 不写「总体不错」。宿主建议见括号。

1. **AUD10-A1（钉 §二 #1）**：`Editor/BattleScene.cs` 里放一个 `IPlayEvent` 夹具（`GameModesCheck.cs:463` 已有同类），
   `gm.SetAsCurrentlyPlayingEvent(ev)` → 打一局到 `Ctx.IsOver` → 跑结算 →
   断 `ev.finishedCalls == 1` **且** `gm.CurrentPlayingEvent == null`（原版 `CombatFinished` 末尾清空）。
   ⇒ 今天这条**必红**（红就是证据）。改法两条：① 结算块补 `CombatFinished`；② 开局那拍补 `SetAsCurrentlyPlayingEvent`。
2. **AUD10-A2（钉 §二 #2）**：给 `CollectionData` 加一个「强制 `Lib.Save()` 失败」的测试缝（已有 `SaveCenter`/`OverrideDir` 先例），
   在**事件窗**那条路上断「`Select` 失败 ⇒ 不开战 + 有可见弹窗」。练习侧已有对照断言（B104_P4）。
3. **AUD10-A3（钉 §二 #3 / R4）**：同一台 driver 连 `Begin()` 两次，断 `RankedSettled == false`（第二局起点）。
4. **AUD10-A4（钉 §二 #7）**：源码级正则断言（照 `B98H5_Assert` 的写法）：
   `BattleDriver.LeaveBattle` 与 `TutorialModePopup.PlayTutorial` 的 `LoadScene` 前必须有
   `CanStreamedLevelBeLoaded`/`try` 守卫（与 `DeckRuntime.BackToMenu:5274`、`ShellRuntime.cs:205` 同一口径）。
5. **AUD10-A5（钉 §二 #8）**：`BeginTutorial` 失败支（关号越界即可造）⇒ 断「有可见出口」（弹窗或自动回主菜单），
   而不是「停在空场」。今天只能断「打了 `LogError`」，**那条断言只证明「知道错了」，不证明玩家能走**。
6. **AUD10-A6（钉 R1）**：`PracticeModePopup` 连开连关两次，断 `StartedBattle == false` 在每次 `Open()` 之后。
7. **AUD10-A7（钉 §三 R8）**：静态断言「`LiveOpsEventWindow.StartedBattle` 有读者或删除」——防止又一处同名不同义。

---

## 五、没查清 / 顺手发现

**没查清（缺什么）**
1. **`SetAsCurrentlyPlayingEvent` 的原版调用点**：已坐实 `PlayEvent.StartMatchNoCheck` 首句会写（`decomp_b91` 现读）；
   `SkirmishEventWindow.BattleButtonClick` 一类是否**还**经 `StartMatch` 再走一次没核（`.c` 里 `BattleButtonClick` 未逐句读）。
2. **遭遇族今天到底该不该有事件实例**：本地 13 份 SO 里没有 `FastMode` 活动（`FastModeBaseEvent.cs` 文件头自述）⇒
   「`RecordLocalOutcome` 永不执行」是数据缺失还是调用点缺失，需要一次「远端 `FastMode` 活动是否存在」的判定（P3 边界）。
3. **`EndPanel` 在「模式窗未关」时的层级**：`CloseAllWindows` 若被跳过（§二 #4），面板与窗谁压谁 —— 需要真机。
4. **教程 6 关的完成位原版写点**：`SetTutorialState` 云脚本的成功回包体本地读不到（`PlayerDataManager` 泛型/异步体缺产物）⇒
   只确认「触发在服务器」，「回包里写哪一格」未证。

**顺手发现（不改，交由调度台分流）**
- `LiveOpsEventWindow.StartedBattle`（`:236/1694`）**无读者**（R8）。
- `BattleDriver` 里 `WindowsManager.Instance`（`:32402`）与全仓推荐的 `Get()` 口径不一致（§二 #4）。
- ~~`PracticeModePopup.cs:2675` `BuildCardRows(_info)` 疑似用了陈旧字段~~ ⇒ **已核、不是缺陷**：`_info` 是
  `Transform`（`PracticeModePopup.cs:627` 声明、`:669` 建出的 `Deck info` 节点），不是数据快照 ⇒
  与同方法 `:2681` 的局部 `info`（`DeckInfo` 数据）不同类，不存在「画上一副卡」那回事。留痕防后人重查。
