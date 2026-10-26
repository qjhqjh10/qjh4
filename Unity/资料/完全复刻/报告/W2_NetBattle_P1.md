# W2 —— `NetBattle.cs` 那 31 行 P1 判据的落地（第八十三会话 · 完全复刻）

> **输入** = `判据/B9_P1_判据表.tsv` 里 `目标文件 == CardPresentation/Net/NetBattle.cs` 的 31 行
> （33 行里另有 2 行的目标文件是 `RuleEngine/Core/RuleCore.cs`——`624 SendMulliganCards`、`496 ParsePlayerAction`——
> 按白名单**不归本笔**，未动）。
> **判据源**（只这三样，⛔ 不用我们自己的注释当判据）：
> 🥇 `D:/2/tools/decomp_full/**/*.c` · 🥈 `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/**/*.cs` · 🥉 `D:/2/新解包资源/assets_full/**`
> **改动范围**：只 `CardPresentation/Net/NetBattle.cs`（`BattleContext.cs` 也**没动**）。
> **typecheck**：`TMPDIR=/tmp/wf_w2 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
> **账补丁**：`账/_parts/B9_W2.tsv`（31 行 × 11 列，`(原版类,方法)` 在全库**唯一命中**已验）。

---

## 〇、一句话结论

**27 A / 2 C / 2 D，0 留 B。**
本轮真正写代码的是 6 处：**换牌兜底倒计时**、**对局中兜底倒计时**、**`ReceiveEnemyForfeit` 的四步**、
**`ReceiveMatchFinished` 抽方法**、**`ReceiveMulliganAck` 的 ack 格**、**`WaitOpponentLastActions` 的收尾支
＋ `DisconnectedDuringBattle` 的延后支**（这两条是一对，缺一条就永远不判弃权）。

---

## 一、31 条逐条

| 账行号 | 方法 | 结论 | 改了什么 | 账状态 |
|---|---|---|---|---|
| 1527 | `TurnExtensions.GetFinishedTurn` | 无需落点 | — | **D** |
| 1528 | `TurnExtensions.GetTurn` | 已对齐 | — | A（不变） |
| 1530 | `TurnExtensions.SetFinishedTurn` | 无需落点 | — | **D** |
| 1531 | `TurnExtensions.SetTurn` | 已对齐 | — | A（不变） |
| 458 | `BattleManager.IsNetworkedGame` | 已对齐（等价） | — | A（不变，补 `InBattle` 落点） |
| 487 | `BattleManager.MulliganFallbackCountdown` | **补做** | 新增 `BeginMulliganFallback` / `TickMulliganFallback` / `NoteMulliganResolved` | **B→A** |
| 441 | `BattleManager.InBattleFallbackCountdown` | **补做** | 新增 `TickInBattleFallback` | **B→A** |
| 535 | `BattleManager.ResendMulligan` | 已对齐（等价） | — | A（不变） |
| 209 | `BattleManager.AttemptReconnect` | 已对齐（等价）+ **两处量差仍开着** | — | A（不变，差异已写明修点在 `NetSession.cs`） |
| 264 | `BattleManager.CancelMatchWithVictory` | 已对齐（等价） | 新增 2 个调用点（两条兜底倒计时） | A（不变） |
| 265 | `BattleManager.CancelMatchWithoutResult` | 已对齐（等价） | 新增 1 个调用点（重连失败 · 还没开打那一档） | A（不变） |
| 291 | `BattleManager.CommunicateBattleAction` | 已对齐（等价） | — | A（不变） |
| 292 | `BattleManager.CompleteBattleFinished` | **有缺口** | 只核不改（落点在 `BattleDriver.cs`） | **B→C** |
| 293 | `BattleManager.ConnectionStatusChanged` | 已对齐（等价） | — | A（不变） |
| 300 | `BattleManager.CreateRemoteAction` | 已对齐（等价） | — | A（不变） |
| 320 | `BattleManager.DisconnectedDuringBattle` | **补做**（第三支） | 新增 `DeferForPeerLastActions` | A（不变，差异已消） |
| 338 | `BattleManager.FailedToReconnectAfterDisconnect` | **补做**（battleStage==0 那一档） | `ReconnectCountdownExpired` 本机支加 `LeaveWithoutResult` 分支 | A（不变，差异已消） |
| 346 | `BattleManager.ForfeitDisconnectedEnemy` | 已对齐 | — | A（不变） |
| 495 | `BattleManager.ParseEnemyAction` | 已对齐（等价） | — | A（不变） |
| 515 | `BattleManager.ReceiveEnemyDesync` | 已对齐（本地等价） | — | A（不变） |
| 516 | `BattleManager.ReceiveEnemyForfeit` | **补做**（①②③ 三步） | 新增 `AcceptPeerForfeit`，`case NetKind.Resign` 改为转调 | **B→A** |
| 517 | `BattleManager.ReceiveEnemyMulliganAndEnvEffect` | 已对齐（等价） | `case NetKind.Mulligan` 加「已定序就不再定序」的闸 | A（不变） |
| 518 | `BattleManager.ReceiveMatchFinished` | **补做**（抽方法） | 新增 `AcceptPeerMatchFinished`，`case` 改为转调 | **B→A** |
| 519 | `BattleManager.ReceiveMulliganAck` | **补做** | `NoteApplied` + `_mulliganAcked` + `MulliganAcked` 读口 | **B→A** |
| 522 | `BattleManager.RegisterPlayerAction` | 已对齐（同一格在 `BattleDriver`） | — | **B→A** |
| 523 | `BattleManager.RegisterPlayerActionCancelled` | **有缺口** | — | **B→C** |
| 654 | `BattleManager.ShouldWaitForOpponentLastActions` | 已对齐（第二项是代理量） | — | A（不变） |
| 657 | `BattleManager.ShowDisconnectionPopup` | 已对齐 | — | A（不变） |
| 686 | `BattleManager.StopWaitingOpponentReconnection` | 已对齐 | （新增一个复用点：`AcceptPeerForfeit` 第 ① 步） | A（不变） |
| 687 | `BattleManager.SuccessfulReconnection` | 已对齐（等价） | — | A（不变） |
| 702 | `BattleManager.WaitOpponentLastActions` | **补做**（收尾支） | 新增 `OnFinishWaitEnded`，`TickFinishWait` 出口转调 | **B→A** |

---

## 二、本轮改了 `NetBattle.cs` 的哪几处（**零行号漂移**）

🔴 **新成员一律追加在类尾**（1848 行之后），既有方法的改动全部做成「**同行替换/同行折行**」⇒
**既有的 `文件:行号` 落点一个都没漂**（已逐条复核：`HandlePeerLost` 525 / `ReconnectCountdownExpired` 958 /
`CancelReconnectCountdown` 1127 / `TickFinishWait` 1347 / `SendFingerprint` 1370 / `Tick` 1403 / `Dispatch` 1436 /
`case NetKind.Resign` 1595 / `case NetKind.MatchFinished` 1695 / `HostResolveMulligan` 1715 / `ResumeData` 1742 /
`LeaveWithoutResult` 1824 —— **全部与原值相同**）。文件 1848 → **2105** 行（+257，全在类尾）。

新增（类尾，行号为本轮读数）：

| 成员 | 行 | 干什么 |
|---|---|---|
| `FallbackClockTimeLimit` / `MulliganFallbackInterval` / `MulliganFallbackAttempts` / `InBattleFallbackAttempts` | 1858-1870 | 原版四个常量（各有出处注释） |
| 状态字段 + 5 个自检读口 | 1872-1901 | `MulliganFallbackRunning` / `MulliganFallbackAttemptsForTest` / `InBattleFallbackAttemptsForTest` / `MulliganAcked` / `MulliganPhaseResolved` |
| `BeginMulliganFallback()` | 1906-1919 | 原版换牌兜底的**启动点**（ILSpy `BattleManager.cs:25865`） |
| `NoteMulliganResolved(string)` | 1921-1930 | **停止点**（ILSpy `:3082` 的 `StopCoroutine`）+ 置 `+0x29c`/`+0x2a8` 两格 |
| `TickNetFallbacks()` | 1933-1938 | 泵 |
| `TickMulliganFallback(float)` | 1949-1980 | 每拍：`attempts++` → resend / confirm → `attempts>15 && 没收到对面换牌` ⇒ `AbortWithVictory` |
| `TickInBattleFallback(float)` | 1984-2018 | 180 秒后每拍 `attempts++`；`>10` 且行动方没变 ⇒ `LogError` + `AbortWithVictory` |
| `AcceptPeerForfeit()` | 2022-2042 | 原版 `ReceiveEnemyForfeit` 四步 |
| `AcceptPeerMatchFinished()` | 2044-2056 | 原版 `ReceiveMatchFinished`（只置标志） |
| `NoteApplied(MsgAction)` | 2059-2064 | `applied` 里认出「对面换牌」与「定序完成」 |
| `DeferForPeerLastActions()` | 2074-2091 | 原版 `DisconnectedDuringBattle.c:74-89` 那一支 |
| `OnFinishWaitEnded()` | 2094-2100 | 原版 `_WaitOpponentLastActions_d__316:47-65` 收尾支 |
| `PeerLastActionsWaitedForTest` | 2103 | 自检读口 |

既有方法上的 8 处**同行**改动（**行数不变**）：

1. `TickReconnectCountdown()` 的第一句 → `if (!_countdownOn) { TickNetFallbacks(); return; }`
   （`!_countdownOn` 正是原版 `waitingWhileDisconnected` 的反面 ⇒ 兜底只在「没在等重连」时数）。
2. `OnLocalMulligan` 末尾 → `BeginMulliganFallback()`。
3. `BeginFinishWait` 里 `_finishWaitLeft = _peerForfeited ? -1f : FinishWaitCap;`（对面已投降就不开窗）。
4. `TickFinishWait` 的两个出口支 → `OnFinishWaitEnded()`。
5. `HandlePeerLost` 的 `_peerLossWhy = …` 那一行 → 追加 `if (DeferForPeerLastActions()) return;`。
6. `HostResolveMulligan` 的 `Debug.Log` 那一行 → 追加 `NoteMulliganResolved(...)`。
7. `case NetKind.Applied` 的 `Log[m.seq] = m` 那一行 → 追加 `NoteApplied(m)`；
   `case NetKind.Mulligan` 两行 → 追加 `_peerMulliganSeen = true` 与 `&& !_mulliganResolved`。
8. `ReconnectCountdownExpired` 本机支前两句 → 换成「换牌阶段 ⇒ `LeaveWithoutResult` + `StopReconnecting` + return」+ 原 `Debug.Log`。
9. `case NetKind.Resign`（15 行）/ `case NetKind.MatchFinished`（12 行）→ 改成转调类尾方法，**行数逐行对齐**（靠注释增减抵平）。

---

## 三、判 C / D 的逐条「差什么」

### 523 `RegisterPlayerActionCancelled` → **C**
- 原版真体（`BattleManager__RegisterPlayerActionCancelled.c:19-34`）：**只**把 `BattleManager+0x3bc` 那个
  `ObscuredInt` 做一次 `op_Decrement`；调用点 6 处全在 `Resolve*` 被取消的分支
  （`_ResolveAttack_d__438:226/588/1365` · `_ResolvePlayActiveAbility_d__479:456` · `_ResolvePlayCardFromHand_d__447:341/1049`）。
- **差什么**：我们**没有 `-1` 那一跳** —— `BattleDriver._actionsThisTurn`（同一格 `+0x3bc`，已由 522 那行承接）
  **只增不减**、回合开始统一归零；而且我们**没有「取消已登记动作」这条路**（同步结算 + 引擎无回滚）。
- 要复刻得先有「**可取消的动作队列 + 回滚**」—— 那是引擎侧大件，不是一行能补的。
- ⚠️ 与 522 的分工：522 的计数器**已经在 `BattleDriver` 落地**（A19 那轮为缩时判定做的），
  ⛔ 本笔**没有在 `NetBattle` 另建一份计数器**（两处写同一条规则 = 迟早不一致）。

### 292 `CompleteBattleFinished` → **C**
- 原版真体（`BattleManager._CompleteBattleFinished_d__389__MoveNext.c:20-95`）四步：
  `WaitForSecondsRealtime(2.0f)`（常量 `0x1834b2bbc` = `00000040`）→ 若 `uiState+0x1c` 真再
  `WaitForSecondsRealtime(DAT_1834b2e7c)` → `battleStage(+0x290) = 8` → `BattleNetworkManager.LeaveBattleRoom(bVar6)`
  → 激活 `+0x160` 那个 `GameObject`。
- 我们的落点 = `BattleDriver.ShowEndPanelAndFreezeInput()`（现读 `BattleDriver.cs:31198`）。
- **差什么（三处）**：
  ① **一次 `WaitForSecondsRealtime` 都没有**（我们的流程只有 `TickFinishWait(Time.deltaTime)`）——
     原版那两跳的意义是「会话 `timeScale=0` 时照走」；
  ② `battleStage` 置 8 无对应格；
  ③ 原版在两段等待后**立刻** `LeaveBattleRoom`，我们是**玩家点结算面板出口**才
     `LeaveBattle() → LeaveNetRoom()`（`BattleDriver.cs:12417`，调用点 `:8348` / `:12322`）。
- **落地文件 = `CardPresentation/Battle/BattleDriver.cs`（本笔白名单外）**⇒ 报调度台另派写手。

### 1527 / 1530 `TurnExtensions.GetFinishedTurn` / `SetFinishedTurn` → **D**（铁律 11 例外①）
- 原版真体（`out_cpp2il_dev_ilspy/TurnExtensions.cs:114-160` / `:160-190`）：读写
  `room.customProperties["FToA" + player.number]`（`GetFinishedTurn` 还要求房间里先有 `Turn` 键）。
- **取证（三源）**：
  ① `TurnExtensions` 在 `out_cpp2il_dev_ilspy` 全树**只被 `PunTurnManager.cs` 调用**；
  ② `PunTurnManager` 除自身与 `IPunTurnManagerCallbacks` 外**零引用**，而那个回调接口
     **全树 0 个实现者**（`dump.cs:15998` 只有定义、`:15934` 只有一个字段）⇒ 没有任何东西能驱动它；
  ③ `assets_full` 里 `PunTurnManager` 只出现在 `globalgamemanagers/MonoScript` 的清单元数据里。
- ⇒ 那是 **Photon 回合制示例的 vendor 代码**，Warpforge 对局走自己的 `BattleCommsManager` 动作流 ⇒
  「每玩家已完成回合数 `FToA`」这个量在本作**没有消费方**，⛔ 不必为它造同步字段。
- ⚠️ **如实记一条边界**：场景 / prefab 侧的脚本引用是 **GUID**（不是类名），本轮**没做 GUID 级证否**。
  若调度台要求证否到底，补一次「取 `PunTurnManager` 的 MonoScript guid → 全库 grep」即可。

---

## 四、没查清的 / 读不到的

1. **`InBattleFallbackCountdown` 的启动点读不到**：`decomp_full` 里除自身外 **0 引用**，ILSpy 只有
   `StopBattleAfterBattleEnd` 的 `StopCoroutine("InBattleFallbackCountdown")`（`:19408`）⇒
   启动它的那个方法**没有方法体**（铁律 17 ② 那一族）。本实现按「每回合重置」对待（行动方 / 回合一变就重计 180 秒）。
2. **`ActionsLeftInQueue()` 的口径仍未读清**（与账 654 同一条）：它是四个队列长度之和
   （`BattleManager__ActionsLeftInQueue.c:10-19` 读 `+0x350/+0x358/+0x360/+0x368` 的 `+0x18`），
   但那四个队列**指哪四个**没读出来。我们仍用 `Session.Inbox.Count` 当代理（与 `TickFinishWait` **同一个**口径，
   ⛔ 没有另造第二份）。
3. **`+0x29c` / `+0x2a8` / `+0x3f8` 三个字段的**名字**没读到（只有偏移与用途）；
   用途是从「谁读谁写」反推出来的（`ReceiveMulliganAck` 写 `+0x2a8`；`MulliganFallbackCountdown` 拿它当退出条件），
   ⚠️ 语义判断有据但**没有符号名**。
4. **`ReceiveEnemyForfeit` / `DisconnectedDuringBattle` 里那两个 DAT 字符串**（`DAT_184281920`、`DAT_1842cbea0`）
   是靠**交叉比对**认出来的：`ShowDisconnectionPopup.c:27`（`param_2==0` 那一支 = 对手掉线）用 `DAT_1842cbea0`，
   `DisconnectedDuringBattle.c:74-89` 用 `DAT_184281920` ⇒ 判定它们 = `ForfeitDisconnectedEnemy` / `WaitOpponentLastActions`。
   ⚠️ 与账 613 那行的认法一致，但**没有直接读到字符串内容**。
5. **`BattleDriver.cs` 的行号正在漂**（别的写手在改）：同一处 `ShowEndPanelAndFreezeInput` 本轮三次读到
   `31033` → `31195`（M1 的读数）→ **`31198`**；`_actionsThisTurn = 0` 从 `13918` 漂到 `14083`。
   ⇒ 我 patch 里的 `BattleDriver.cs` 落点**一律带符号名**，调度台按符号认。

---

## 五、需要调度台裁 / 转派的

1. 🔴 **账补丁撞车（必须按顺序合并）**：`工具/check_patch_conflicts.py` 报 3 条跨补丁重复键，其中两条与我有关：
   - `BattleManager.CompleteBattleFinished()`：`B9_M1.tsv:23`（状态 **B**，落点 `ShowEndPanelAndFreezeInput`）vs
     `B9_W2.tsv:14`（状态 **C**，列了三条缺口）；
   - `BattleManager.InBattleFallbackCountdown()`：`B9_M1.tsv:36`（状态 **B**，**读成「到点自动结束回合 / 时钟看门狗」**，
     落点 `TickClock`）vs `B9_W2.tsv:8`（状态 **A**，按真体 = 「180 秒 + 10 拍行动方没变 ⇒ `CancelMatchWithVictory` 判本机胜」）。
   ⇒ **这两行必须以 W2 为准**：合并顺序 `--only B9_M1.tsv` 先、`--only B9_W2.tsv` 后（字典序恰好也是这个顺序，
   但请显式写）。M1 对 441 的口径我判断是**读错了**（把 `CancelMatchWithVictory` 当成「自动结束回合」），
   理由与逐行出处见本报告 §二 与那两行的 `备注`。
2. 🔴 **两条数值差要另派写手（修点在 `Net/NetSession.cs`，本笔白名单外）**——账 209 `AttemptReconnect`：
   ① 客机重连间隔 `ReconnectEveryMs` **2000ms** vs 原版 **3 秒**（`_d__320:119-152`）；
   ② 重连窗口地板 **15s** vs 原版 **10s**（`_d__320:61` 是 `Math.Max(10,…)`；15 是 `ForfeitDisconnectedEnemy` 那条的地板）。
   判据表把目标文件写成 `NetBattle.cs`，但真值在 `NetSession.cs`。
3. 🔴 **`CompleteBattleFinished` 的落地在 `BattleDriver.cs`**（§三），需另派写手。
4. ⚠️ **本轮只跑 typecheck，没有跑任何 Unity 自检**（红线）。新加的两条兜底倒计时**没有自检用例**，
   自检读口已经备好（`MulliganFallbackRunning` / `MulliganFallbackAttemptsForTest` / `InBattleFallbackAttemptsForTest` /
   `MulliganAcked` / `MulliganPhaseResolved` / `PeerLastActionsWaitedForTest`）⇒ 恢复自检时请补：
   ① 换牌阶段只推客机 ⇒ 主机侧 `MulliganFallbackAttemptsForTest` 涨、`MulliganPhaseResolved` 仍假；
   ② 把假钟推过 `16×4` 秒 ⇒ `DesyncClaimsForTest` 变 1（判本机胜）；
   ③ 对局中行动方不变 + 假钟推过 `180+11` 秒 ⇒ 同上；行动方一变 ⇒ 计数归零；
   ④ 对面投降 ⇒ `PeerMatchFinished` 真、`WaitsForPeerLastActions` **假**（`BeginFinishWait` 那道闸）。
5. ⚠️ **`IsNetworkedGame` 那条「哪些模式走 P2P」的模式表断言**没落（表在 `Net/NetMatchmaking.cs`，白名单外）。
6. ⚠️ **判 D 的两行**（1527/1530）如果调度台要求 GUID 级证否，说一声我去补（§三末）。

---

## 六、验过的

- `TMPDIR=/tmp/wf_w2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**。
- 落点行号复核：既有成员 **0 漂移**（§二那张表逐条对过）。
- 账补丁结构：31 行 × 11 列、`(原版类,方法)` 全库唯一命中 31/31、无 TAB 混入、LF 无 BOM。
- `工具/check_patch_conflicts.py`（只读）⇒ 3 条跨补丁重复键，已列在 §五·1。
- ⛔ **没跑**：任何 Unity 批处理 / 自检 / 真机（红线）。
