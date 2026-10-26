# B8_L · 联机层 30 条 B 行升 A ＋ `CancelMatchWithoutResult` 的「离场」那一半

> 执行代理 **L**（代号 `B8_L`），2026-10-26。白名单 = `CardPresentation/Net/**`。
> 账补丁 = `资料/完全复刻/账/_parts/B8_L.tsv`（34 行：**25 行 B→A**，9 行保持 B 并写清原因）。
> 代码 = `CardPresentation/Net/NetBattle.cs` **+85 行 / −0 行**（3 处，`git diff --numstat` 读数）。

---

## §0 结论（一句话 + 数字）

1. **25 条真升 A**：`AttemptReconnect` · `CancelMatch` · `CancelMatchWithVictory` · `CancelMatchWithoutResult` ·
   `CommunicateBattleAction` · `ConnectionStatusChanged` · `CreateRemoteAction` · `DisconnectedDuringBattle` ·
   `FailedToReconnectAfterDisconnect` · `ForfeitDisconnectedEnemy` · `IsNetworkedGame` · `ParseEnemyAction` ·
   `ParsePlayerAction` · `ReceiveEnemyDesync` · `ReceiveEnemyMulliganAndEnvEffect` · `ReceivePlayerAction` ·
   `ResendMulligan` · `SendMulliganCards` · `ShouldWaitForOpponentLastActions` · `ShowDisconnectionPopup` ·
   `StopWaitingOpponentReconnection` · `SuccessfulReconnection` · `TurnExtensions.GetTurn` · `TurnExtensions.SetTurn`
   ＋（`战斗界面_HUD` 域那一条）`BattleErrorUIManager.OpponentDesyncCancelMatch`。
2. **9 条保持 B** 并逐条写清「缺的是哪一段 / 为什么要落到别人的文件」：`ReceiveEnemyForfeit` ·
   `ReceiveMatchFinished` · `ReceiveMulliganAck` · `RegisterPlayerAction` · `RegisterPlayerActionCancelled` ·
   `WaitOpponentLastActions` · `TurnExtensions.GetFinishedTurn` · `GetTurnStart` · `SetFinishedTurn`。
3. **任务 2 完成**：`NetBattle.LeaveWithoutResult()` 落地（类尾），并接到唯一该接的那一档
   （`case NetKind.Hash` 的**状态指纹对不上**）。判据链逐跳见 §1。
4. **门禁**：`check_ledger_points.py B8_L.tsv` = **A 25 · 通过 25 · 漂移 0 · 失败 0 · 告警 1**（PROSE，只验形态）；
   `verify_annot.py` = **缺注解 0**；`check_a_vs_diff.py` = 13 条弱信号**全部是本补丁之外的旧行**（无一条来自 B8_L）；
   `merge_ledger_parts.py --dry` = **新增 0 · 无跳过/无歧义**（只有 1 行域被归一成 `战斗界面/HUD`，符合预期）。
5. **typecheck**：编辑器程序集 **0**；运行时程序集那几条错误**全部落在别人的文件**
   （`RuleEngine/Core/EffectResolver.cs`、`RuleEngine/Core/UnitState.cs` —— 本轮有别的代理正在写），
   与本笔无关（`Net/**` 一条都没有）。

---

## §1 任务 2 —— 「不判结果直接离场」（缺的那一半）

### 1·1 原版那条链，逐跳

| # | 跳 | 判据（`d:/2/tools/decomp_full/`） |
|---|---|---|
| ① | `BattleErrorUIManager.OpponentDesyncCancelMatch(MatchWon)` = `WindowsManager.ShowPopUp(EnemySyncError, localizeTexts:false, closeOnEsc:false, SubmitButton, onSubmit)`；闭包里取 `BattleManager` 再调 `CancelMatch(MatchWon)` | `Everguild.BattleErrorUIManager__OpponentDesyncCancelMatch.c:23-47` · `…__c__DisplayClass18_0___OpponentDesyncCancelMatch_b__0.c:14-27` |
| ② | `BattleManager.CancelMatch(false)` ⇒ **`BattleNetworkManager.LeaveBattle()` 然后 return**（这一支**没有** `AddResignAction` / `DeadHero` ⇒ **按定义不产生结果**）；`true` 那一支才是 `AddResignAction` + `DeadHero(对面,5)` | `BattleManager__CancelMatch.c:16-26` / `:28-29` |
| ③ | `LeaveBattle()` ⇒ `RemoveRoomAfterLeaving()`（房间 `EmptyRoomTtl = 0`）+ **`NetworkCustomManager.LeaveRoom()`** ⇒ 之后 `FinishedLeavingBattle()` | `BattleNetworkManager__LeaveBattle.c:38-47` / `:57` |
| ④ | **`LeaveRoom()` 不捎任何告别** ⇒ 对端走 `OnPhotonPlayerDisconnected` → `EventPlayerDisconnected` → `SetOpponentDisconnected`（弹窗 + 停表 + **它自己的** `ForfeitDisconnectedEnemy` 倒计时）⇒ **`max(15, 30 − 3n)` 秒之后才判我们弃权** | `BattleNetworkManager__EventPlayerDisconnected.c:45-52` · `BattleManager._ForfeitDisconnectedEnemy_d__322__MoveNext.c:57-61,140-144` |
| ⑤ | 另一条同形链：`CancelMatchWithoutResult(errorLog)` = 记错 + `UIstate=finishBattleSequence` + 连接状态置 `Disconnected` + `PlayerDataManager.CancelMatch`（**服务端**）+ `OnOpponentDesync(false)`（⇒ 回到 ①，`MatchWon=false`） | `BattleManager__CancelMatchWithoutResult.c:19-66` |
| ⑥ | 第三条同形链：`FailedToReconnectAfterDisconnect` 在 **`battleStage == 0`**（还没开打）时**整段走 ⑤ 的同一批语句** | `BattleManager__FailedToReconnectAfterDisconnect.c:54-108` |

### 1·2 为什么「关会话」这一跳必须是 `Close(say:false)`

- 我们**已经**有 `bye` 这条路：`NetSession.Close(say:true,…)` ⇒ `Send(NetKind.Bye)` ⇒ 对端 `NetBattle.HandlePeerClosed`
  ⇒ `ForfeitPeerIfLeftMidGame()` ⇒ **当场判我们弃权**（`NetBattle.cs:649-672`）。
  这与原版 ④ 的「等对端自己那个 15/30 秒窗口」**相反**。
- `NetSession.Close(say:false)` 是**硬断**（`sentBye == false` ⇒ `CloseTransportAfterBye` 走 `HardCloseRequestedForTest` 那一档，
  `NetSession.cs:1055-1076` / `:1089`）⇒ 对端只能靠心跳超时发现（`Pump:329-344`）⇒ 进 `HandlePeerLost`
  ⇒ 起它自己的倒计时 ⇒ 到点判 —— **与原版 ④ 同形**。

### 1·3 落地（`NetBattle.cs` 三处）

| 处 | 行 | 内容 |
|---|---|---|
| ① 调用点 | `case NetKind.Hash`（`:1584-1589`） | 指纹对不上 ⇒ 原样保留 `Abort(...)`（告知窗口不变），**再**调 `LeaveWithoutResult(...)` |
| ② 新成员（类尾） | `:1779-1846` | `NoResultLeavesForTest`（自检读口）· `_noResultLeaves` · `_leftWithoutResult`（只认一次）· `LeaveWithoutResult(string why)`（`:1824-1846`）：记错 + 提示行 + **`_s.Close(false, "Settings/Online/Wire/RoomGone")`** |
| ③ `CancelReconnectCountdown` | `:1127-1147` | 补上原版 `StopWaitingOpponentReconnection` 的**第二句** `StopCoroutine` —— `_finishWaitLeft = -1f; _finishWaitAcc = 0f;`（见 §3·A5） |

**⛔ 没有加判负/判胜支**：这条路按定义不产生结果（原版 ② 的 false 支里没有 `DeadHero`）；
本机只是离场，结果由**对端**在它自己的倒计时到点之后判。

**⚠️ 与原版的两处差（已写进账的「差异」列）**：
① 原版要**玩家点了 `SubmitButton`** 才 `CancelMatch(false)`；我们由 `Abort` 先弹一扇告知窗、**调用即离场**
（再等一次点击会变成两扇窗叠着等）。
② `PlayerDataManager.CancelMatch` / `RecordMatchWonByCancel(Enemy)` 是**服务端**那一笔账，本机不落。

**行为变化（要说清）**：「状态指纹对不上」那一档原来**挂着不结束**（只报不打分），现在**离场不判结果**；
对端会走它的 15/30 秒窗口后判我们弃权 —— 这正是原版 ④ 的形状。`NetBattleTest §7` 那条
`Ok(hNB.Aborted || cNB.Aborted)` 仍然成立（`Abort` 先跑，`_aborted` 已立）。

---

## §2 G15 那一族（`OnOpponentDesync` 四步 + CancelMatch 两支）

### C1 `BattleErrorUIManager.OpponentDesyncCancelMatch(bool)` → **A**
- 判据：`…__OpponentDesyncCancelMatch.c:23-47`（造闭包 + `ShowPopUp(EnemySyncError, …, SubmitButton, onSubmit)`）·
  闭包 `.c:14-27`（`CancelMatch(MatchWon)`）。
- 落点：`NetRuntime.cs:181（ShowSyncErrorPopup）· NetBattle.cs:344（AbortWithVictory）· :1824（LeaveWithoutResult）`。
  两条文案走**官方词条**：`Battle/EnemySyncError` + `MainMenu/General/OK`（`NetRuntime.cs:187`）。
- 观测量：`NetRuntime.SyncErrorPopupsShownForTest`（期望 1）＋真档宿主 `ResignCalls==1`／假档 `NoResultLeavesForTest==1`。
- 两档的提交：`true` ⇒ `NetRemoteResign(Cancelled)`（原版 `DeadHero(对面,5)`）；`false` ⇒ `LeaveWithoutResult`（原版 `LeaveBattle`）。

### C2 `BattleManager.CancelMatch(bool)` → **A**
两档分别 = `AbortWithVictory`（`:344-373`）/ `LeaveWithoutResult`（`:1824-1845`）。判据 `BattleManager__CancelMatch.c:16-29`。

### C3 `CancelMatchWithVictory(string)` → **A**
`AbortWithVictory` 逐跳：`_desyncClaimed`（= `alreadyCancelledMatch`）→ `Send(NetKind.Desync)`（= `commsManager.SendDesync()`，`.c:46`）
→ `ShowSyncErrorPopup`（= `OnOpponentDesync(true)`，`.c:48-51`）→ 提交 `NetRemoteResign(Cancelled)`（= `DeadHero(对面,5)`）。
差：原版先查 `battleStage==60` 早退（我们的三个调用点只在对局中可达，这条闸由 `_desyncClaimed` + `Ctx.IsOver` 间接覆盖）；
`RecordMatchWonByCancel` 是服务端那一笔账。

### C4 `CancelMatchWithoutResult(string)` → **A**（本轮新建）
原版 ⑤ 的六句对上我们的四句（详见 §1·3）。差：服务端记账不落；等点击 vs 调用即离场；`Disconnected` 广播 → `NetState.Off`。

### C5 `ReceiveEnemyDesync()` → **A**
判据 `BattleManager__ReceiveEnemyDesync.c:19-41`（置 `desyncCancelledGame` → `RecordMatchWonByCancelEnemy` → **不动本机局面**）。
我们本地无服务器 ⇒ `AcceptPeerDesync`（`NetBattle.cs:381-401`）判**本机负**（理由码 `Cancelled`）把局收掉 —— 这是**本地等价**，
⛔ 不是原版那一句（不这么做本机永远挂着）。观测量 `DesyncAcceptsForTest`（期望 1）+ `Ctx.ForfeitedBy == MySeat`；
反例：`Ctx.IsOver` 时不再判（`:394-398`）、重复消息由 `_desyncAccepted` 挡。

---

## §3 重连 / 掉线族（A0–A6，全部有现有断言可读）

> 这些行的判据与落点**都逐句读过**；下面只列「机制差异 + 观测量（含两读数）+ 反例」。

### A0 `DisconnectedDuringBattle(bool)` → A
- 判据 `…DisconnectedDuringBattle.c:27-94`：`battleStage==60` 且**对手**掉线 ⇒ 直接 return（不弹窗/不停表/不改结果）；
  对局中 ⇒ `ShowDisconnectionPopup(isPlayer)`（内含 `PauseClock` + 起/重启 `ForfeitDisconnectedEnemy`）；`:74-89` 若队列里还有动作就先重启 `WaitOpponentLastActions`。
- 落点 `HandlePeerLost`（`:525-550`）＋`StartReconnectCountdown`（`:786-809`）。
- 差：原版按 `battleStage` 三档（40/50/60）分流，我们只有 `Ctx.IsOver` 一个布尔。
- 观测量（两读数）：`NetBattleTest §14①`（结算段掉线 → `DrainNoticesForTest` **0 条**、`EnemyDisconnects` 不变、`Winner` 不变）
  vs `§10③-1`（对局中 → 倒计时 **30 秒**开跑、提示行含「30」）。反例：`§14②`（本机掉线同样静默）、`§11`（接上时已掉线要补报一次 / Lobby 接上不补）。

### A1 `AttemptReconnect()` → A
- 判据 `BattleManager__AttemptReconnect.c:11-16`（起协程）＋ `…_d__320__MoveNext.c:40-216`：
  窗口 `Math.Max(10, 30 + (attempts+1)×(−3))`（`:61`）、**每 3 拍**调一次 `TryReconnecting`（`:119-152`）、失败 ⇒ `FailedToReconnectAfterDisconnect`（`:198`）、成功 ⇒ `SuccessfulReconnection`（`:204`）。
- 落点 `NetSession.Pump`（`:210`）的客机退避支 `:351-368` ＋ `NetBattle.ReconnectCountdownExpired`（`:958`）。
- **差两处（已记账）**：间隔 2000ms（`NetSession.ReconnectEveryMs`，`NetSession.cs:24` 已是 🔶自己设计）vs 原版 3 秒；
  窗口地板 15s vs 原版 10s（原版那个 15 是 `ForfeitDisconnectedEnemy` 的地板，被我们借用到这一条）。
- 观测量：`NetSession.ReconnectEveryMs`（现读 2000）／`NetBattle.ReconnectSecondsFor(0)==30`；两读数用 `§10③-1`（30）与 `§10③-4`（27）。

### A2 `ForfeitDisconnectedEnemy()` → A
- 判据 `…_d__322__MoveNext.c:43-165`：`Math.Max(0xf=15, 30 + enemyDisconnects×(−3))`（`:57-61`）· 算完 `:67` 自增 ·
  `:99-117` 每秒一拍（`WaitForSecondsRealtime(1.0)`）· `:119` 置状态 3 · `:140-144` **仅当连接状态 == 70** 才 `LeaveBattleRoom(false)`+`AddResignAction`+`DeadHero(对面,3)` · `:146-161` 否则只 `LogError`。
- 落点 `TickReconnectCountdown`（`:813`）＋`ReconnectSecondsFor`（`:748`）＋`ReconnectCountdownExpired`（`:958`）。
- 观测量（两条**异源**断言）：`§10③-1 ReconnectCountdownLeft==30` → `§10③-4 ==27` 且 `EnemyDisconnects==2`；`§10③-2` 每秒 30→29。
  反例：`§14①`（结算段不起倒计时、次数不涨）；到点理由码 `Disconnect(3)`（`§10③-5'` 两个观测口）。

### A3 `FailedToReconnectAfterDisconnect()` → A
- 判据 `…FailedToReconnectAfterDisconnect.c:20-108`：停 `AttemptReconnect`（`:20`）→ 状态置 3（`:21-28`）→
  `battleStage≠0` ⇒ `AddResignAction(1)`+`DeadHero(我,3)`（`:30-32`）；`=0` ⇒ 走 ⑤ 那条「不判结果」链（`:54-108`）。
- 落点 `NetSession.StopReconnecting`（`:1113`）＋`ReconnectCountdownExpired` 本机支（`:1073-1087`）＋（stage==0 那一半）`LeaveWithoutResult`。
- 观测量：`§10③-7/③-8` 四条 —— `SelfResignCalls==1`、`ResignCalls==0`、`!csC.WasInBattleForTest`、会话仍 `WaitingReconnect`；
  两读数 = `§10③-5` 前半那条对照（到点前 `WasInBattleForTest` 真 / 到点后假）。

### A4 `ShowDisconnectionPopup(bool)` → A
- 判据 `…ShowDisconnectionPopup.c:14-36`：`iVar3 = (param_2^1)+1`（⇒ 本机=1 / 对手=2）· 状态变了才回调 ·
  对手档 `PauseClock()` + 重启 `ForfeitDisconnectedEnemy`；本机档重启 `AttemptReconnect`。
- 落点 `ReconnectPopup`（`:949`）＋`ReconnectHint`（`:932`）＋`StartReconnectCountdown`（`:786`）。
  文案 = **官方词条** `Battle/HUD/LostConnectionMsg` + `PleaseWaitConnection`（本机档换行）/ `WaitOpponentConnectionMsg`（对手档）
  ＋每秒再换行 `mm:ss`（原版 `…__UpdateReconnectWindow.c:27-30`）。
- 观测量：`§10③-1` 提示行含「30」（断言在案）；弹窗正文**静态可算**
  = `Loc.T("Battle/HUD/WaitOpponentConnectionMsg") + "\n00:30"`（批处理里没有窗口系统，这一格只能静态核）。
- 「本机/对手」分档判据 = `_s.Role == Client`（**🔶自己设计**，原版两端各有 `AttemptReconnect`）。

### A5 `StopWaitingOpponentReconnection()` → A（**本轮补了半句实现**）
- 判据 `…StopWaitingOpponentReconnection.c:10-11`：**两句** `StopCoroutine`
  （`ForfeitDisconnectedEnemy` + `WaitOpponentLastActions`；句柄与 `ShowDisconnectionPopup` 的两个 DAT 对照认出）。
- 原来我们只做了第一句（倒计时闸）；**本轮补上第二句** = `CancelReconnectCountdown` 里
  `_finishWaitLeft = -1f; _finishWaitAcc = 0f;`（`NetBattle.cs:1127-1152`，注释引 `.c:10-11`）。
- 观测量：`§10③-3` 两条 ★（`!ReconnectCountdownRunning` + 提示行含「回来」）＋ `WaitsForPeerLastActions == false`；
  两读数 = 回来前 `ReconnectCountdownRunning` 真 / 回来后假。

### A6 `SuccessfulReconnection(bool)` → A
- 判据 `…SuccessfulReconnection.c:19-64`：三个 `StopCoroutine`（`:19,26,27`）· 状态回 0 并回调（`:35-42`）·
  `ClockManager.UnpauseClock()`（`:51`）· `stage==12` 时 `SendMulliganCards`（`:53-55`）· 非本机回合 `ResendLostActions`（`:56-63`）。
- 落点 `Tick` 的「回来了」边沿（`:1403`，块 `:1414-1423`）＋`CancelReconnectCountdown`（`:1127`）。
- 差：`ResendLostActions`（补发近期动作）与 `stage==12 重发换牌` 在我们这里由**整局全量重放**取代。
- 观测量：`§10②` 三条 ★（`!drvB.NetClockPaused`、`!nbB.ReconnectCountdownRunning`、`HidePopupCallsForTest` 增长）。

---

## §4 换牌族

### M1 `ReceiveEnemyMulliganAndEnvEffect(int[], string)` → A
- 判据 `…ReceiveEnemyMulliganAndEnvEffect.c:19-69`：空值 `LogWarning`（`:19`）→ 存 `matchData` 两格（`:36-46`）→
  `ConfirmMulliganReceived`（`:48`）→ `receivedEnemyMulligan = true`（`:51`）→ 藏 `mulliganWaitText`（`:58`）；空值另一支 `LogError`（`:68`）。
- 落点 `HostResolveMulligan`（`:1715`）＋`Dispatch` 的 `case NetKind.Mulligan`（`:1552-1564`）。
- 差：原版「存 + 回执」两跳；我们「存 + 立刻定序 + 广播定序结果」（**回执这条消息在我们的协议里不存在**）。
- 观测量：`§4`「换牌之后两端指纹一致」+ 每座位手牌数相等；两读数 = 定序前 `MulliganOpen==true`（`:254`）→ 定序后 `!MulliganOpen`（`:258`）。

### M2 `ResendMulligan()` → A
- 判据 `…ResendMulligan.c:17-53`：标志真才 `SendMulliganCards`（`:17-19`）；`receivedEnemyMulligan` 真才 `ConfirmMulliganReceived`（`:23-30`）。
- 落点 `ResumeData`（`:1742`）＋`OnResumed`（`:1750`）。差：我们**整条动作流全量重放**（换牌那两条伪动作也在流里）⇒ 不需要重发。
- 观测量：`§6` `cb.Replayed==true` + 重放后两端 `Fingerprint` 相等（`Eq(…hBefore)` 与 `Eq(…cBefore)` 两条 ★）。

### M3 `SendMulliganCards()` → A
- 判据 `…SendMulliganCards.c:13-38`：前置标志假 ⇒ `LogError` 返回；否则 `BattleCommsManager.SendMulliganCards(marks, envCard)`。
- 落点 `OnLocalMulligan`（`:1212`）。差：原版两端对等各发一包；主机在我们这里就是定序方、不给自己发包 ⇒ 包数少一半。
- 观测量：`§4` 两条（`MulliganOpen` 先后转假 + 两端指纹相等）；两读数 = 提交前/定序后。

---

## §5 动作族 / 收尾窗口 / 回合号

| 行 | 结论 | 判据 | 落点 | 机制差异（一句） | 观测量（两读数） |
|---|---|---|---|---|---|
| `CommunicateBattleAction` | A | `…CommunicateBattleAction.c:78-160`（`:78` 转换、`:157` `RemoteSendPlayerAction`、`:121-133` Replay160 不记日志） | `OnLocalAction:1161`＋`OnLocalRawAction:1191`＋`ToWire:495` | 原版「先转模型→记日志→发包」；我们「先乐观落地→发包」，定序权只在主机 | `Log.Count` 两端相等 + `LastSeq` +1 |
| `CreateRemoteAction` | A | `…CreateRemoteAction.c:25-78`（`kind=0x24`、`:77 AddAutoActionToQueue`） | `FromWire:522`＋`Apply:24` | 原版入队由帧循环播（可取消）；我们同步落地（净结果等价） | 落地后两端 `Fingerprint` 相等（`§5` 每步 + 终局） |
| `ParseEnemyAction` | A | `…ParseEnemyAction.c:44-421`（`:115/141/222/286`、`:400-407` 非法联网 ⇒ `SendDesync`） | `Apply:24`＋Dispatch 拒/打岔支 `1479-1494` | 原版只在对端收包侧改对象；我们两端各重放一次 ⇒「谁先算出差异」不同 | `§5` 每步指纹相等 / `§7` 负例故意改一边 ⇒ 不等 |
| `ParsePlayerAction` | A | `…ParsePlayerAction.c:45-828`（`:607 SpacesInTheBoard` 槽位换算） | `ToWire:495`＋`FromWire:522` | 原版要换算槽位；我们绝对编号 ⇒ 只剩字段拷贝 | `ToWire→FromWire` 后 7 字段逐一相等；`§5` 终局指纹与 `Log.Count` 相等 |
| `ReceivePlayerAction` | A | `…ReceivePlayerAction.c:22-60`（`:24` matchType==0xa0 跳日志、`:26` 入队） | `Tick:1403`（循环 `1426-1433`）＋`Dispatch:1436` | 原版追加进远端动作表再入队；我们直接落地；Replay 那一档不进联机层 | `§5` 每步指纹 + `Log.Count` |
| `ShouldWaitForOpponentLastActions` | A | `…ShouldWaitForOpponentLastActions.c:16-28`（`matchFinished \|\| ActionsLeftInQueue()>0`） | `TickFinishWait:1347` | 第二项我们用 `Inbox.Count` **代理**（注释已标死，`NetBattle.cs:1350`） | `R(N5)` 立刻结束 / `R(N6)` 约 7 步 / `R(N7)` 超时自结束 |
| `WaitOpponentLastActions` | **B** | `…_d__316__MoveNext.c:19-67` | `BeginFinishWait:1339`＋`TickFinishWait:1347` | 主体同常量同节奏，但收尾两支（`:47-65` 清 `matchFinished` + `ShowDisconnectionPopup(false)`）**未落地** ⇒ 门槛③ 保持 B | — |
| `GetTurn(RoomInfo)` | A | `cpp2il/out_cpp2il_dev_ilspy/TurnExtensions.cs:32-*` | `Fingerprint:555`＋`NetBattle:1582-1587` | 原版「查表读数」；我们「重算 + 对账」（回合号作为指纹第一分量随 `hash` 包走） | `§5 Eq(cliCtx.Turn, hostCtx.Turn)`；反例：回合号不符只记日志不判（`:1582`） |
| `SetTurn(Room,int,bool)` | A | `…TurnExtensions.cs:13-30` | `SendFingerprint:1370` | 原版「状态广播（照抄）」；我们「校验广播（重算比对）」，`TStart` 无对应量 | 每条 `MsgHash.turn` + `§5` 两端 `Turn` 相等 |

---

## §6 保持 B 的 9 行（逐条说清「缺的是哪一段」）

| 行 | 为什么不升 A |
|---|---|
| `ReceiveEnemyForfeit` | 判据与落点都核过（`…ReceiveEnemyForfeit.c:19-37`），但落点只有 `Dispatch` 的 `case NetKind.Resign`（`:1595-1608`）、**无独立方法** ⇒ §4.1「同一个 `文件:行` 覆盖多方法只能记 B」。抽成类尾 `AcceptPeerForfeit()` 即可升 A |
| `ReceiveMatchFinished` | 同上（`…ReceiveMatchFinished.c:16-19`；落点 `case MatchFinished:1695-1706` + `PeerMatchFinished`） |
| `ReceiveMulliganAck` | 我们的协议里**没有 ack 消息**（`MsgMulliganSync`/`NetKind.MulliganSync` 全仓零使用）；「确认」那一跳 = 主机广播 `Applied(MulliganDone)` 在客机落地（`:1496-1526`）⇒ 落点对不上任何同名/近名方法 |
| `RegisterPlayerAction` | 原版是 `+0x3bc` 上的 `ObscuredInt` 计数器（`…RegisterPlayerAction.c:19-34`，内联写点 `AddAutoActionToQueue.c:151-165`）；我们**没有「未完成动作计数」这个量**（同步结算）⇒ 疑似该记 **C** |
| `RegisterPlayerActionCancelled` | 同一计数器自减（`…RegisterPlayerActionCancelled.c:19-34`），6 个调用点全在 `Resolve*` 被取消的分支；我们**没有「撤一条已登记动作」**（引擎无回滚）⇒ 疑似 **C**（要复刻得先有回滚） |
| `WaitOpponentLastActions` | 见 §5 表（收尾两支未落地） |
| `GetFinishedTurn` | `FToA`（每玩家跑到第几回合）这个量我们没建；等价物 = `Log`/`LastSeq` + 全量重放，与 `ResendMulligan` 共用同一段落点 ⇒ §4.1 |
| `GetTurnStart` | `TStart` = **服务器时间戳**；我们没有服务器，回合计时器在 `Battle/`（`BattleDriver.TickClock`，**不在本批白名单**）⇒ 落点必须落到 `Battle/**` |
| `SetFinishedTurn` | 同 `GetFinishedTurn`（写 `FToA`） |

> 另：`ShowStatusWhenConnecting.GetConnectingDots / OnGUI`（`战斗逻辑驱动.tsv:1457/1458`）**本笔没动** ——
> 原版是 Photon 的「正在连接…」`OnGUI` 横幅，我们的等价物只是设置窗状态字 + `NetRuntime` 通知，**没有独立横幅**；
> 要落点得动 `Shell/` 的设置窗，不在本批白名单。

---

## §7 改了哪些文件、哪些行

| 文件 | 改动 | 行数 |
|---|---|---|
| `CardPresentation/Net/NetBattle.cs` | ① `case NetKind.Hash` 指纹分支加 `LeaveWithoutResult`（`:1584-1589`）② `CancelReconnectCountdown` 加「停收尾等待窗口」两句（`:1127-1152`）③ 类尾新增 `NoResultLeavesForTest` / `_noResultLeaves` / `_leftWithoutResult` / `LeaveWithoutResult`（`:1779-1845`） | **+85 / −0**（1763 → 1848 行） |
| `资料/完全复刻/账/_parts/B8_L.tsv` | 34 行补丁（25 A + 9 B） | 新建 |
| `资料/完全复刻/报告/B8_L_联机层.md` | 本文件 | 新建 |

- 行尾：`NetBattle.cs` 现在仍是 **纯 LF、裸 CR = 0**（二进制核过：`LF 1848 / CRLF 0 / bareCR 0`）。
- 新增成员**全部追加在类尾**（`:1779` 之后），⛔ 没有插进既有成员之间；`CancelReconnectCountdown` 那 9 行插在函数体里，
  **会让 `:1127` 之后的所有行号 +9**（`OnLocalAction 1152→1161`、`Dispatch 1427→1436`、`ResumeData 1733→1742` 等）——
  账里写的**都是改动后的行号**。

---

## §8 顺手发现（⛔ 本笔没修，交调度台分流）

1. **客机重连节奏与窗口地板比原版松一档**：`NetSession.ReconnectEveryMs = 2000`（原版 3 秒）·
   `ReconnectSecondsFor` 的地板 15s（原版 `AttemptReconnect` 是 10s；15 属于 `ForfeitDisconnectedEnemy`）。
   这两条已写进 `AttemptReconnect` 行的「差异」列。⚠️ 改它要动 `NetSession` 的常量与 `ReconnectSecondsFor` 的调用点。
2. **`NetKind.MulliganSync` / `MsgMulliganSync` 全仓零使用**（死协议位）—— 要么删、要么在注释里写清保留理由。
3. **`NetProtocol.Seat()` 仍在仓库里**（只剩自检 `I①~I③` 在测它自己；注释已记它退出运行路径）。
4. **`NetBattle._lastHash / _lastHashTurn` 只写不读**（`SendFingerprint` 写进去，全仓无人读）——
   若要用它做「上一回合指纹」判据，得先确认真值来源。
5. **客机重复提交换牌会再发一条 `mulligan` 包**，主机侧 `_theirMulliganMarks` 按最新一份覆盖 ⇒ 两次内容不同时，
   定序结果跟最后一次。自检没覆盖「重复提交」，**未验是否有害**。
6. **`battleStage` 这一维我们没有**：`DisconnectedDuringBattle` 的 40/50/60 三档、`FailedToReconnectAfterDisconnect`
   的 `battleStage==0` 分支都落在同一支 ⇒ **「还没开打就掉线」今天也会走 30 秒后判本机负**（原版该走「不判结果离场」）。
7. `NetBattle.cs` 里 `Abort` 的注释（`:259-267`）仍写着「这一档只报不打分、这一局挂着不结束」——
   本轮之后**指纹那一档会离场**（`case NetKind.Hash` 那条注已就地补上），但 `Abort` 自己的 doc 里那句话现在只对
   `case NetKind.Reject` 成立（**本轮没改它的 doc**，因为它说的仍是「不判胜负」这个正确的语义）。

---

## §9 没查清的部分（如实）

1. **`ActionsLeftInQueue()` 的口径**（本机待播 / 待应用 / 两者）—— 仍未读清，`ShouldWaitForOpponentLastActions`
   第二项继续用 `Inbox.Count` 代理；`DisconnectedDuringBattle.c:87` 也用了它（我们那一档走的是
   「`Ctx.IsOver` 静默 + 起倒计时」，没有按队列分流）。
2. **`IsDuringBattle(param, 1)` 的第二个实参语义**（`_d__316:54`）—— 读不出是「含战后」还是别的，
   所以 `WaitOpponentLastActions` 收尾那两支**只敢记成 B**、没有硬猜。
3. **`battleStage` 各值含义**：40/50/60 三档从 `DisconnectedDuringBattle` 的用法能推出「60 = 战后」，
   40/50 具体是哪两个阶段**没查到**（`dump.cs` 的枚举名对不上）。
4. **`GetTurnStart` / `GetFinishedTurn` / `SetFinishedTurn` 的 `TurnExtensions.cs` 行号带 `*`** ——
   ILSpy 那份是**骨架**（`out_cpp2il_dev_ilspy`），函数体的 unbox 多跳没能 100% 复原；
   判据要点只用到「读哪个键」这一层（那一层是可复核的）。

---

## §10 给调度台的请求

### 10·1 要跑的自检（我没有跑 Unity，按红线）

| 跑什么 | 为什么 |
|---|---|
| `NetBattleTest.Run` | 本笔动了 `Dispatch` 的指纹分支（§7 负例）与 `CancelReconnectCountdown`（§10②/③、§12） |
| `NetSelfTest.Run` | `Q⑪/Q⑫` 钉的是 `case NetKind.Reject → Abort` 那条路（**没动**）；`NET/BYE` 那一族**没动** —— 请确认仍全绿 |

**预期**：`NetBattleTest §7` 的 `Ok(hNB.Aborted \|\| cNB.Aborted)` **仍应绿**（`Abort` 先跑）；
两端的会话在 §7 之后会变成 `Off`（本笔新增的离场语义），而 §8 之后的用例都用**新开会话**，不受影响。

### 10·2 建议补的自检用例（`Editor/**` 不在我白名单）

1. **不判结果离场**：喂一条假 `MsgHash` 触发指纹不符 ⇒ 断言 `NoResultLeavesForTest==1`、`_s.State==Off`、
   `HardCloseRequestedForTest` 增 1、`GracefulCloseRequestedForTest` **不变**（证明没发 `bye`）、`Ctx.Winner` 仍 0。
2. **G15 两支**：`Send(Desync)` 后本机 `DesyncClaimsForTest==1` / 收方 `DesyncAcceptsForTest==1` +
   `Ctx.ForfeitedBy==MySeat`；`SyncErrorPopupsShownForTest` 恰 1（这两个读口已在代码里，但**今天没有任何用例读它们**）。
3. `WaitsForPeerLastActions`：对手回来后应立刻 `false`（本轮补的那两句）。

### 10·3 账

- 合并：`python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B8_L.tsv`
  （`--dry` 已核：34 行全部**唯一命中**、新增 0、无跳过；其中 1 行的域会被归一成 `战斗界面/HUD`）。
- 合并后**行号会漂**（本笔 +85 行在 `NetBattle.cs`）⇒ 在「没有 `.cs` 写手」的时刻跑一次
  `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A --fix`。
- 本补丁**没有**改主表、没有碰 git、没有跑 Unity。
