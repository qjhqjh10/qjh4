# W58-DIAG-NET · `NetBattleTest.Run` 2 条失败诊断

> 只读诊断。**没改任何代码**（本文件是唯一落盘物）。没跑 Unity、没跑 `*.Run`、没动 git、没真连。
> 输入：`d:/4/_tmp_view/netbattle.log`（3,730,862 B · 2026-10-20 21:21）·
> `Editor/NetBattleTest.cs`（工作区 == HEAD，本文件不在本轮 diff 里）·
> `Net/NetSession.cs` / `Net/NetTransport.cs` 的 `git diff`（`git diff -U8`）。

---

## 〇、总结论（先给答案）

**两条失败同一个根因，且不是本轮联机改动引入的回归 —— 是夹具（fixture）的观察点选错了。**

夹具在 **`Pump()` 边界**上等一个**瞬态状态** `WaitingReconnect`；而客机的「重连退避」分支
在**同一次 `Pump()` 调用里**就把状态继续推进到 `Handshaking`：

- `NetSession.Pump()` 先执行掉线闸（`NetSession.cs:242-252`）：`WaitingReconnect` + `_wasInBattle = true`；
- **同一个方法**继续往下执行到客机退避支（`NetSession.cs:351-366`）：因为 `_lastReconnMs` 初值 = 0、
  而 `now` 是**进程级**秒表 ⇒ `now - _lastReconnMs >= ReconnectEveryMs(2000)` 恒成立 ⇒ **立刻重连**；
  回环 `Connect` 成功 ⇒ `SetState(Handshaking)`。

⇒ `Pump()` 返回时状态已经是 `Handshaking`，`PumpOne` 的 `cond()`（`NetBattleTest.cs:1709`
= `s.Pump(); if (cond()) return true;`）**永远看不到 `WaitingReconnect`**。
之后主机那一台**没有被推**（`PumpOne` 只推客机），握手不可能走完 ⇒ 状态卡在 `Handshaking`
直到 8 秒超时，断言把「实际 Handshaking」打出来。

**被测行为本身是对的**：同段里 5 条 ★ 断言全绿 —— 客机**确实**收到了掉线（`OnPeerLost` 触发、
`NetBattle.HandlePeerLost` 执行、结算段静默 / 对局中倒计时照常起），只是夹具用来「取证」的那个状态
是一瞬间的中间态。

**三条假设全部排除**（逐条证据见 §二）。

**不是本轮引入的**：

- `NetSession.cs:351-366`（客机重连退避）与 HEAD **逐字相同**（`git diff` 里这一段是上下文行）；
- `Editor/NetBattleTest.cs:1445-1449 / 1474-1477` 与 HEAD **逐字相同**（该文件不在本轮 `git diff --stat` 里）；
- 该夹具由 `8f77ff7a`（W56 · 2026-10-19 21:53）引入；而 `_tmp_view` 里**没有任何一份包含本夹具的旧日志**
  （`final_NetBattleTest.Run.log` 是 2026-10-19 **02:08**，早于 W56 提交；其余 `netbattle*.log` 里
  `grep -c "结算掉线"` 全 = 0）⇒ **没有「它曾经绿过」的证据**，不能宣称是回归。

---

## 一、逐条

### 编号 1 · `（结算掉线②）夹具：客机确实看到连接断了（实际 Handshaking）`

- 位置：`Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs:1447`
- 断言原文：`PumpOne(f.cs, () => f.cs.State == NetState.WaitingReconnect, 8000)`
- 根因：**夹具等的是瞬态状态**。客机（`Role == Client`）在同一次 `Pump()` 里完成
  `InBattle → WaitingReconnect →（重连退避）→ Handshaking`，`Pump()` 返回时已经不是 `WaitingReconnect`。
- 证据（`_tmp_view/netbattle.log`，行号 = 文件行号）：
  - `:45088` 传输层掉线：`原因 = 对面关掉了连接`（`NetTransport.cs:1107`）
  - `:45102` `WaitingReconnect：对手掉线了…（对局已暂停）` ← `NetSession.cs:247`
  - `:45123` `NetBattle.HandlePeerLost()`（结算段静默那一支，打印「对手掉线，但这一局已经结算完了」）
  - `:45130` `WaitingReconnect：正在重连主机…` ← `NetSession.cs:358`（**同一个 `Pump()`**）
  - `:45144` `Handshaking：连上了，正在补上这一局的进度…` ← `NetSession.cs:362`
  - `:45158` ✗ 断言失败，实际 `Handshaking`
  - 对照：`（结算掉线①）`（推的是**主机** `f.hs`，`Role == Host` 没有退避支）**通过**：
    `:44635 ✓ …（实际 WaitingReconnect）` —— 这个不对称正是根因的直接佐证。
- 反证（「同一次 Pump」的推理）：`PumpOne` 每轮 `Pump()` 之后立刻查 `cond()`；
  只要**任何一次** `Pump` 返回到 `WaitingReconnect`，这条断言就会绿。它连等 8 秒都没绿
  ⇒ 状态从未在 `Pump` 边界上停留过 ⇒ 迁移必然发生在**同一次** `Pump` 内。
- 修法：**改夹具（推荐）**，见 §三；⛔ 不要改实现去让第一个重连尝试晚 2 秒（理由见 §三末）。

### 编号 2 · `（结算掉线③）夹具：客机看到对面掉了（实际 Handshaking）`

- 位置：`Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs:1476`
- 根因：与编号 1 **完全相同**（同一支代码、同一个观察点；只是这一局的 `Ctx.IsOver == false`，
  所以后续走的是「对局中掉线 ⇒ 起 30 秒倒计时」那一档）。
- 证据：
  - `:45626` 传输层掉线 → `:45640` `WaitingReconnect：对手掉线了…`（`NetSession.cs:247`）
  - `:45676` 附近 `NetBattle.StartReconnectCountdown`（30 秒）
  - `:45684` `WaitingReconnect：正在重连主机…`（`NetSession.cs:358`）→ `:45698` `Handshaking`（`:362`）
  - `:45712` ✗ 断言失败，实际 `Handshaking`
  - 紧接着的 ★ 断言 `:45725 ✓`（倒计时照常起、提示照常说）、`:45805 ✓`、`:45818 ✓`
    ⇒ **被测行为正确**，红的只是「取证方式」。
- 修法：同编号 1。

---

## 二、三条假设逐条验证（全部排除）

### H1 · 「新 S6 候选路径把断线后的重连吸走了」—— ❌ 不成立

- `PumpReplacement()` **只在主机侧 armed**：`NetSession.cs:388-391`
  `bool armed = Role == NetRole.Host && (State == InBattle || (State == WaitingReconnect && _wasInBattle));`
  非主机一律 `rh.RejectPendingReplacement("not-in-battle")`（`:398-405`）。
  这两条用例推的是 **`Role == Client` 的 `f.cs`** ⇒ 候选路径根本不参与。
- 即便推主机：这两条用例里主机侧的旧连接是**被测试自己 `ClosePeer()` 主动关掉的**
  （`NetTransport.cs:870`，末尾 `Publish(connected: false)`，`:878`）⇒ `ConnBit` 已清
  ⇒ 客机重连落到**普通 accept**（`Setup`），**不会**变成候选。
  `ReplacementHandshakeTimeoutMs = 6000`（`NetSession.cs:31`）在这两条用例里**一次都没进入**。
- 日志侧：失败窗口里没有任何 `NET/BYE-S6/*` 行（可在 `netbattle.log` 里 `grep "BYE-S6"` 确认）。

### H2 · 「`Ack` 不再回传 token 让握手没完成」—— ❌ 不成立

- **握手没走完的真正原因是「主机会话根本没被推」**：`PumpOne(f.cs, …)` 只推客机
  （`NetBattleTest.cs:1706-1711`），主机 `f.hs` 在这 8 秒里一次 `Pump()` 都没跑 ⇒
  主机不会处理新连接、不会发 `Challenge`/`Ack` ⇒ 客机停在 `Handshaking`。这与 token 无关，
  在没有本轮改动之前也是同一个结构（`PumpOne` 与夹具都是 HEAD 原样）。
- token 改动只在**主机收到 `Proof` 回 `Ack` 时**生效（`NetSession.cs:694`），这一跳根本没发生。
- 客机侧「重连前提 `SessionToken` 非空」没有变；`NetSession.cs:751` 也刻意保住客机旧钥匙。
- 旁证：`NetSelfTest` 本轮 **347 通过 / 0 失败**，其中已覆盖「真掉线 → 重连握手」全链。

### H3 · 「`Close` 清 `_inbox` 把关键帧丢了」—— ❌ 不成立

- 这条路径**只调 `Transport.ClosePeer()`，不调 `Close()`**；
  `DropPendingFrames("Close")` 只在 `NetTransport.Close()` 里（`NetTransport.cs:1132`）。
- 客机判掉线走的是 `_t.PeerLost` 标志位（`NetSession.cs:242`），**不依赖 inbox 里的帧**。
- `Setup()` 里丢的是**旧代（旧连接）残留帧**（`NetTransport.cs:506-530` 那一族注释），
  属于设计内的「不许把旧对家的 `bye` 带进新连接」，与本次失败无关。
  本轮日志里也没有 `BYE-R2/frame-dropped` 打在这两条用例的窗口内。

---

## 三、修法（改夹具，不改实现）

### 3·1 推荐改法：用「掉线事件」取证，而不是等瞬态状态

`NetSession.OnPeerLost` 是 `public Action` 字段（`NetSession.cs:52`），且 `NetBattle.WireSession()`
用 `+=` 挂了 `HandlePeerLost`（`NetBattle.cs:320`）⇒ 夹具用 `+=` 追加即可，不会顶掉生产接线。

`NetBattleTest.cs:1445-1449`（14②）改成：

```csharp
// 对面那条连接没了 —— 客机这一侧看到的与「自己断网」**同形**（这正是判不了的那一件）
bool cLost = false;
f.cs.OnPeerLost += () => cLost = true;          // 与 NetBattle 的 HandlePeerLost 并存（`+=`，不是 `=`）
f.hs.Transport.ClosePeer();
Ok(PumpOne(f.cs, () => cLost, 8000) && f.cs.State != NetState.InBattle,
   $"（结算掉线②）夹具：客机**确实看到**连接断了（`OnPeerLost` 已响；状态 {f.cs.State} = 已进入自动重连）");
f.cs.StopReconnecting();                        // 夹具止血（位置不变）
```

`NetBattleTest.cs:1474-1477`（14③）同样处理（`bool cLost = false; f.cs.OnPeerLost += …`）。

**为什么这不叫「放宽断言」**：旧断言要的证据是「客机进入了掉线态」；新断言要的是
① `OnPeerLost` **真的响了**（比「停在 WaitingReconnect」更直接地证明掉线被识别），且
② 状态**已经离开 `InBattle`**。两条合起来严格不弱于旧断言，而且不再依赖「状态在 `Pump` 边界上停留」这一偶然时序。
⛔ 单独写 `State == WaitingReconnect || State == Handshaking` **不算数**（新鲜连接也可能是 Handshaking），
必须配上事件/`LastError` 这类「确实断过」的证据。

### 3·2 备选改法（如果不想碰回调）

等 `LastError`：掉线闸里 `LastError = why`（`NetSession.cs:250`，`why` = 「对面关掉了连接」）⇒
`PumpOne(f.cs, () => !string.IsNullOrEmpty(f.cs.LastError), 8000)`。
比 3·1 弱一点（`LastError` 别的路径也会写），**必须**再配一条 `f.cs.State != InBattle`。

### 3·3 ⛔ 不建议的「改实现」做法

有人在类似情况下会想把「第一次重连尝试」推迟到 `ReconnectEveryMs` 之后
（在掉线闸里加 `_lastReconnMs = now`，或把字段初值改成 `NowMs`）。这样 `WaitingReconnect`
确实会在 `Pump` 边界上停留 2 秒，夹具就绿了 —— **但不建议**：

- 这是**为了夹具改生产时序**：真机上每次掉线都会多等 2 秒才开始重连；
- 原版没有这条判据（重连走 Photon，本地无参数），属于我们自拟行为，没有「必须 2 秒后首试」的依据；
- 夹具的**真实意图**（「客机确实看到断线」）本来就不该用瞬态状态表达。

### 3·4 收尾建议

- 这两条断言改成事件取证后，等待时间从 8 秒/条降到毫秒级（失败时本来要白等 8 秒）；
- 同段另外 5 + 3 条 ★ 断言**不需要动**（本次日志已全绿，证明被测行为正确）；
- `NetSelfTest`（347/0）不需要动；`Net/` 实现**不需要动**。

---

## 四、给下一轮的交接要点（一句话版）

> `NetBattleTest` 14②/14③ 的两条红 = 夹具在 `Pump()` 边界上等 `WaitingReconnect`，
> 而客机重连退避在**同一次 `Pump()` 内**就把它推到 `Handshaking`（`_lastReconnMs` 初值 0 ⇒ 立刻重连）。
> 不是 S6 / token / inbox 的问题，也不是本轮回归。修法 = 夹具改用 `OnPeerLost` 事件取证（§3·1）。
