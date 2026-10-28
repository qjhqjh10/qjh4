# B89_E10b · B4「落已有文件」那一半 + P0 XP 发放链调查 —— 交件报告

> 执行代理 `B89-E10b` · 2026-10-28 · 判据 = `判据/B88_R10_基础设施58条施工单.md` §2.2/§2.4/§六
> 原版体 = `d:/2/tools/decomp_full/*.c`（Ghidra）＋ `cpp2il/out_cpp2il_dev_ilspy/*.cs`（ILSpy）＋ `il2cpp_out/stringliteral.json`。
> 账补丁 = `账/_parts/B89_E10b.tsv`（**7 行** · 11 列 · LF · 与其它补丁**无冲突**）。

---

## §1 🔴 P0：`GetXPFromMatch` 的发放链（本报告最重要的一节）

### 1.1 结论（一句话）

**原版赛后 XP 不是客户端算的** —— 它由**服务器回包**里的 `RewardInfo.grantedXp` 给，
经 `RewardService.Collect` 累加后交给 `AddCollectorXp` / `ShowGrantedXpToast`。
`GetXPFromMatch` 在整个程序集里 **0 调用点**，**不是**这条链的一环。
我们的对位落点 = `Battle/BattleDriver.cs` 的 `RecordBattleLog()`（局终钩子），那个文件 E5b 在写 ⇒ **本波没接，只写待接点**。

### 1.2 原版逐跳（每条都能打开核）

| # | 跳 | 判据（现读） |
|---:|---|---|
| ① | `BattleManager.ProcessMatchResult()`（PostBattle 分支） | `cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:19194`（方法头）；`:19180/:19185` 是它的两个调用点 |
| ② | → `GameModes.CombatFinished(_currentMatchResult, FinishedProcessingMatchResult)` | 同文件 **`:19282`**（`handler.CombatFinished(_currentMatchResult, callback);`） |
| ③ | liveops 事件 → CloudScript `RecordMatchFinished` → **回包** `OnlineMatchResult` | `Everguild.LiveOps/RankedV2Event.cs:51`（`<RecordMatchFinished>g__HandleSuccess_007C0(OnlineMatchResult result)`）、`:65` 把回包交给下一跳 |
| ④ | `PlayerDataManager.ProcessMatchResult(IPlayEvent, MatchType, OnlineMatchResult, float, bool)` | `cpp2il/out_cpp2il_dev_ilspy/PlayerDataManager.cs:9470` |
| ⑤ | → `RewardService.Collect(matchResult.EndMatchData.Rewards, showAnimation: false, …)` | 同文件 **`:9487`** |
| ⑥ | `RewardService` 把 `rewards` 里所有 `RewardInfo.grantedXp` **求和** | `RewardService.cs:357-363`（`:363` = `Enumerable.Sum(rewards, func6)`，`func6` 就是 `x => x.grantedXp`，`RewardInfo.cs:16` 的 `private int grantedXp` / `:26` 的公开 getter） |
| ⑦ | → `AddCollectorXp(grantedXp, showToast: false)` **＋** `ShowGrantedXpToast(grantedXp)` | `RewardService.cs:382`（加 XP）· `:402` / `:456`（弹「+N 收藏点数」） |
| ⑧ | 两个方法体 | `PlayerDataManager.cs:15335`（`AddCollectorXp`：`xp += grantedXp` + 升级信号 + 升级 toast）· `:15376`（`ShowGrantedXpToast`：词条 + `{0}` → 弹 CollectionGainXP 档） |

### 1.3 为什么说 `GetXPFromMatch` 不在这条链上（反证）

- **ILSpy 全树** `grep -rn "GetXPFromMatch" --include=*.cs` = **2 条**：定义处（`PlayerDataManager.cs:9765`）
  与方法自己那句告警串（`:9827`）⇒ **0 个调用点**。
- **Ghidra**：`decomp_full/` 里只有 `PlayerDataManager__GetXPFromMatch.c` 一个文件（26,282 个方法体的树里
  **没有任何文件**引用它）。
- 语义上也自洽：`GetXPFromMatch` 的入参是 `(MatchType, matchWon)`，而赛后那条链**从头到尾没算过 XP 值**，
  只是把服务器给的 `Rewards` 发下去。

⇒ 🔴 **对上一波「P0：赛后 XP 一分不发」的订正**：真缺口是**整条服务器奖励链（P3）**，
**不是**「少了一次 `GetXPFromMatch` 调用」。把 `GetXPFromMatch` 接到结算处 = **造一个原版没有的调用点**。

### 1.4 我们的对位与建议（需要调度台拍一下）

- **我们的局终钩子** = `Battle/BattleDriver.cs:32444 RecordBattleLog()`（结算处唯一一处「记这一局」）。
  ⇒ XP 如果要发，就发在它之后。**该文件归 E5b ⇒ 本波一个字没动。**
- 我们的收藏 XP **反推自背包**（账 340：`CardTierConfig` 从 `GetTotalPlayerXPFromInventory` 重算，**没有增量入口**）
  ⇒ 就算拿到值也**没有「加到 XP 上」的口**。这两件必须一起做，否则值是空的。
- **建议的三件套**（调度台裁定后再动手，⚠️ 第一件是替代物、必须打 `🔶`）：
  1. 🔶 **自己设计（非原版）**：用 `PlayerDataMath.GetXPFromMatch(Ctx.MatchType, 赢 ? 1f : 0f)` 充作
     「服务器会给的那个数」（原版这个数来自服务器；离线没有它就是没有）。
     ⚠️ 只在「原版这条链断在服务器」的前提下成立，**判据必须写全**（铁律 §四 P3 口径）。
  2. 给 `CardTierConfig` 加一个**增量**入口（原版 `AddCollectorXp` 的形状，账 340 目前是 B）。
  3. `ToastNotifications.ShowGrantedXpToast(v)`（本波已建，账 509 ⇒ A）。

---

## §2 逐条结论（本波 7 条）

| 账行 | 原版判据 | 改动（文件:行号·符号） | 状态建议 |
|---:|---|---|---|
| **486** `SetAltPorts(bool)` | `PlayerDataManager__SetAltPorts.c` 三句 | `Net/NetConfig.cs:392 SetAltPorts` · `:385 DontUseAltPortSetting` · `:28 NetConfigData.altPorts` | **A** |
| **509** `ShowGrantedXpToast(int)` | `PlayerDataManager__ShowGrantedXpToast.c` 全文 | `Shell/ToastNotifications.cs:1486 ShowGrantedXpToast` | **A** |
| **354** `BlockGame()` | `PlayerDataManager__BlockGame.c` 全文 | `Shell/WindowsManager.cs:1577 BlockGame` · `:1596 QuitForDuplicateConnection` | **A** |
| **507** `ShouldShowRatePopup()` | `PlayerDataManager__ShouldShowRatePopup.c` 全文 | `Core/PlayerDataMath.cs:370 ShouldShowRatePopup` | **A** |
| **423** `IsTherePendingMatch()` | `PlayerDataManager__IsTherePendingMatch.c` → `OnlineData__IsTherePendingMatch.c` | `Core/PlayerDataMath.cs:393 IsTherePendingMatch` · `:408 PendingMatchKey` | **A** |
| **555** `showDevTools()` | `PlayerDataManager__showDevTools.c` 全文 | `Shell/PlayerProfileState.cs:160 DevToolsEnabled` | **B**（数据源未查清） |
| **460** `QuitApplication()` | `PlayerDataManager__QuitApplication.c` + `__FullyQuitApplication.c` | 既有 `Shell/MainMenuRuntime.cs:1758 QuitGame`（**未改**，E12 占用） | **B**（缺两小步） |

### 2.1 三条零行数变化（⛔ 不是随手写的，是硬约束）

`Shell/ToastNotifications.cs`（16 条账行引用行号）· `Shell/WindowsManager.cs`（**85 条**）·
`Core/PlayerDataMath.cs` / `Shell/PlayerProfileState.cs`（本波新建、无引用）——
前两个文件的改动**一律追加在类尾**；`Net/NetConfig.cs`（5 条引用 `:21/:47/:56`）的新字段
`altPorts` 写在 **`:28` 那一行的行尾**（`public int port = …; public bool altPorts = false;`），
**全文件行数不变** ⇒ 三条账行号一个都没漂。

### 2.2 逐条要点

1. **`SetAltPorts`** —— 三句照抄。🔴 第 ① 句 `dontUseAltPortSetting = !dontUseAltPortSetting` 是**取反**（不是赋值），
   语义为什么这样**没解出来**（施工单 §六·4 也挂着），注释里写死「照抄」。
   我们的落法：全局开关暂寄 `NetConfig.DontUseAltPortSetting`（本仓没有 `GameStaticData` 类）、
   `altPorts` 进 `NetConfigData`（**会落盘**）、第 ③ 句 = `NetConfig.Save()`。原版 **0 调用点**（唯一提到它的
   `NetworkCustomManager.ConnectToPhoton` 那一族我们没有）。
2. **`ShowGrantedXpToast`** —— 与既有 `ShowCollectionGainXP`（`:812`，**只听声不弹**）的关系在代码里写清了：
   后者那句「本地没有数据源」只对「XP 从哪儿来」成立，而**原版这个方法把 XP 当实参收** ⇒ 实参到了就该弹。
   词条 `MenuCollection/Toast/CollectorPoints` 现读官方 13 语言表 = `+{0} 收藏点数`（**带 `{0}`**，13 种语言都有）。
3. **`BlockGame`** —— 两个词条实读：`0x425CEA8` = `CustomErrors/DuplicateConnection`、`0x42BE318` = `MainMenu/General/Confirm`。
   🔴 **只有一颗按钮、没有取消**。那颗按钮 = 原版 `UIGenericEventCatcher.Source`（`GameBlocked` 支）⇒ **退出游戏**。
   第 ① 句 `loginManager.DuplicateConnection()` **落不了**（`LoginManager` 全仓 0 命中）⇒ 出声 + 省略。
4. **`ShouldShowRatePopup`** —— 语义只有两句：**通关过 ⇒ 不弹；否则只在第 1 关弹**（`campaignStageIndex == 1`）。
   数据源 `TipsData` = 服务器只读 key 14 ⇒ 做成「给两个值就判」的纯函数（⛔ 不塞假 TipsData）。
5. **`IsTherePendingMatch`** —— 键名 = `((PlayerDataKey)18).ToString()` = **`"LastOpponentInfo"`**
   （`dump.cs:78412` 的枚举 18 号；非 flag 枚举 `ToString()` 就是成员名）。做成「给一张只读字典就判」的纯函数。
6. **`showDevTools`** —— 体只有「读一个静态 `ObscuredBool`」。**数据源没查清**（施工单 §六·5 挂着），
   所以只落了一个 `DevToolsEnabled` 占位开关（**恒 false、没有写入方**），并写明「最可能来自
   `SaveKeys.ActivateDevTools`（= `PlayerDataKey.ActivateDevTools(2)`，我们 `Shell/SaveKeys.cs:43/247` 已有）」。
   ⛔ 按铁律 §18「还没查清该写 TODO，不许用 `🔶 自己设计` 蒙混」⇒ **没打 `🔶`、写的是「还没查清」**。
   ⇒ 这一条建议**保持 B**，等数据源定出来再接上写入方就是 A。
7. **`QuitApplication`** —— 三跳：① `helpshiftManager` 那一下是 **RVA `0x4B33B0` 全局空桩**（什么都不做，永远不用做）
   ② `photonManager.LeaveRoom()`（对位 = `BattleDriver.LeaveNetRoom`，`Battle/BattleDriver.cs:4159`，**不许碰**）
   ③ `WaitForSeconds(**0.5**)` → `Application.Quit()`（`DAT_1834b2bb4` 实读 `0000003f` = 0.5）。
   我们已有「退出」那一跳（`MainMenuRuntime.QuitGame:1758`，经 `PromptPopup` 确认）⇒ **缺的只是前两小步**。
   ⛔ `MainMenuRuntime.cs` 归 E12 ⇒ **只核不改**。

---

## §3 验证

- **秒级类型检查**：`TMPDIR=/tmp/wf_B89E10b bash d:/4/Unity/工具/typecheck.sh`
  → **运行时错误数 0 / 编辑器错误数 0**（最终一次）。
  ⚠️ 中途两次被**别人的在飞改动**顶红：`RuleEngine/Core/EffectResolver.cs` 报
  `CS0103 BroadcastCreatedCardWithWarlordPreCall`（×2）/ `FireMobOrRegimentOnSameSide`（×1）——
  **全部集中在不属于本代理的文件**（`RuleEngine/**` 归 E2c/E3b/E6b）；等它编干净后复跑即 **0/0**，
  编辑器程序集的 `CS0006 WFCheck.dll` 也随之消失（那条只是运行时 dll 没产出的连带）。
- ⛔ **没有跑 Unity**（本波红线）⇒ 本轮新增的断言**一条都没执行过**。
- 新增断言（`Editor/PlayerDataMathCheck.cs` 的 `CheckB4PureAndBranches()`，**18 条**）：
  - `ShouldShowRatePopup` 4 档（未通关/0 关/1 关/2 关 + 已通关）；
  - `IsTherePendingMatch` 4 档（null / 空表 / 值为空串 / 非空）+ 键名常量 = `LastOpponentInfo`；
  - `DevToolsEnabled` 出厂 false / 可写 / `ResetForTest` 清回；
  - `SetAltPorts` 6 条：`altPorts` 写入 · 🔴 **全局开关「调一次翻一次」**（两次调用 = 恒等）· **落了盘** ·
    落盘 JSON 里有 `altPorts` 这一格 · 写回 false。
    ⚠️ 这一组**会把 `OverridePath` 指到 `Application.temporaryCachePath` 下的临时文件**，
    `finally` 里恢复并 `Load()` 换回玩家那份 ⇒ **不会改玩家真正的 `WarpforgeNet.json`**。

---

## §4 顺手发现（⛔ 我没改，交调度台分流）

1. 🔴 **`GetXPFromMatch` 0 调用点**（§1.3）：上一波「P0 = 赛后 XP 一分不发」的因果链**接错了**。
   真要 XP，得先有「发 XP 的口」+ 一个值来源（服务器 or 本地替代公式）。
2. 🆕 **原版「加 XP」的口本身在账里也是 B**：账 340 `AddCollectorXp` = 我们**从背包反推总 XP**，
   **没有增量入口** ⇒ 即使把 XP 值算出来也**没地方加**。这三条（413 值 → 340 口 → 509 弹窗）是**一条链**，
   建议调度台**打包派**（现在 509 已经做完，只剩 413 的接线 + 340 的增量口）。
3. **`ShowCollectionGainXP`（`ToastNotifications.cs:812`）的注释该改**：它写「本地没有数据源 ⇒ 不弹」，
   但原版那个方法**收实参**。本波新建的 `ShowGrantedXpToast` 已承担「有实参就弹」那一档；
   旧方法建议下一波改成**一行转调**（`return ShowGrantedXpToast(amount);`）——
   ⚠️ 那会让该文件行数 −3，**必须先安排账行号 `--fix`**，所以**本波没动**。
4. **`BlockGame` 是一条真实的死路**：原版触发源 = Photon 房间层的「同账号重复进房」，
   我们的 P2P **不是房间模型** ⇒ 没有这条事件（`Net/NetBattle.cs` 的 T07 注早已把它记成「例外①」）。
   本波把方法建出来了（铁律 11），但**没有生产调用方**。
5. **`B89_E12.tsv` 也改了账 421 `IsBattleLogCreated`**（两行同为 A、落点同为 `PlayerDataMath.cs:220`、
   白名单 12 档与「110 在白名单」的口径**与我的完全一致**）⇒ `check_patch_conflicts.py` 报的
   这 1 条重复键**是无害的**（谁赢都一样）。

---

## §5 待接点（这些文件本波**一个字没动**）

| 账行 | 方法 | 目标文件（归属） | 还差什么（可照着做） |
|---:|---|---|---|
| **413→结算** | （XP 发放链） | `Battle/BattleDriver.cs:32444 RecordBattleLog()`（E5b） | 局终调 `PlayerDataMath.GetXPFromMatch(Ctx.MatchType, 赢?1f:0f)`（🔶 替代物，见 §1.4）→ 增量口 → `ToastNotifications.ShowGrantedXpToast(v)` |
| **460** | `QuitApplication()` | `Shell/MainMenuRuntime.cs:1758 QuitGame`（E12） | 在 `Application.Quit(0)` **之前**加：① `LeaveNetRoom()`（`BattleDriver.cs:4159`，经 `BattleDriver` 调用）② **等 0.5 s** 再退 |
| **433** | `ModifyBattleLogBotAltId(string botName, string altId)` | `Shell/BattleLogData.cs`（E12） | 战报条目加 `botAltId` 列（原版 `+0x98`）；遍历**全部**命中、**最后一条赢**、返回最后一个命中下标、无命中 `-1`（**没有 `break`**） |
| **488** | `SetDemoDeck(string deckID)` | 教程/FTUE 那一族（`Shell/FTUESelectDeckWindow.cs`，**不在本代理白名单**） | `if (demoData.currentDeck != deckID) { Log("Save deck in online data " + deckID); demoData.currentDeck = deckID; UploadOnlineDemoData(); }` —— 前半本地（A）、`UploadOnlineDemoData` = **D**（服务器）。⚠️ 字段真名 = `DemoData.currentDeck`（`+0x20`） |

---

## §6 交接

- **账补丁**：`账/_parts/B89_E10b.tsv`（7 行 · 11 列 · LF · 无冲突）。
  ⚠️ 其中 **555 建议 `B`**（数据源未查清）、**460 建议 `B`**（缺两小步、且文件被 E12 占用）——
  这两条**不是「做完了」**，请按备注里的时间限定/前置条件记。
- **自检入口**：调度台已加 `run PlayerDataMathCheck.Run pdmath.log`（16 → 17 条）；
  本波把它的断言从 ≈89 条扩到 **≈107 条**（静态调用点 71 → **89**；新增 `CheckB4PureAndBranches()` **18** 条）⇒ **入口不用再改**。
  ⚠️ 那一组里 `SetAltPorts` 会写临时文件（`finally` 已还原），批处理里跑是安全的。
- **`GameStaticData` 归位待办**：`SetAltPorts` 的全局开关暂寄 `NetConfig.DontUseAltPortSetting`。
  将来若要建一个真正的 `GameStaticData`（`LoadSharedDefaultValues` 那 13 个静态量，账 460 那一族），
  把这一格搬过去即可（**只有一个读写点**，搬迁零风险）。
- **本波没碰的**：`Battle/BattleDriver.cs` · `RuleEngine/**` · `Shell/MainMenuRuntime.cs` ·
  `Shell/BattleLogData.cs` · `Shell/CardDetailPopup.cs` · `Shell/RankedData.cs` · `Shell/VictoryCountersStore.cs` ·
  `Core/Tooltip.cs` · `Battle/TipPrebattle.cs` · 账主表 · 两张正本 · git。
