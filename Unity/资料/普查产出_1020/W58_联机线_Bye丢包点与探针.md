# W58-NETPROBE · 联机线 `T05` —— `bye` 丢包点普查与判别性探针

> 2026-10-20 · 执行代理 **W58-NETPROBE** · 白名单 = `Unity/MyGame/Assets/CardPresentation/Net/*.cs`
> 口径：**只加日志、不改行为**（不提前 return、不改状态机、不发新包）。
> ⛔ 没跑 Unity / 没跑 `*.Run` / 没真连 / 没动 git / 没改正本（铁律 12）。
> ✅ 只跑了秒级类型检查（结果 §六）。

---

## 一、结论（S6 是否已静态锁定）

**结论：S6 在【代码上可判】，而且就 F5 那份 X2 日志而言 —— S6 可以被【排除】，不是那次 `bye` 丢失的原因。**

理由只有三步（判据全是现成代码，见 §三证据链）：

1. `AcceptLoop` 拒收（S6）一旦发生 ⇒ 那条**新连接不会被 `Setup`**、`AcceptedCount` **不变**、
   主机**拿不到** `proof` / `reconnect` 任何一帧。
2. 主机 `InBattle` **只有一条路**能到：`NetSession.Handle` 的 `case NetKind.Reconnect`
   （`NetSession.cs:495-509`；`EnterBattle()` 是测试自己在第 ① 步之前调的，而第 ① 步已断言主机离开过 `InBattle`）。
   那一支的前提是主机在新连接上**已经收过** `proof` 并回过 `ack`。
3. F5 的 E3/E4：断言那一刻（`netbattle.log:5303`）**主机是 `InBattle`**、`PeerLost == false`；
   到 `:5340` 才是 `WaitingReconnect`（`_t.PeerLost` 支）。⇒ 主机确实在**新的那条连接上**握完了手、
   收了 `reconnect`、发了 `resume` ⇒ **那条新连接没有被 `AcceptLoop` 拒收**。

⚠️ 这条结论**推翻 W57 报告 §三「S6 最可疑」**（`资料/普查产出_1019/W57_联机线_静态尾巴.md:127-133`）。
错因：W57 只看了「旧代码这一支静默 + 形状吻合」，**没有拿主机状态迁移反查 S6 的必要前提**。
👉 建议主对话在 W57 那份上按铁律 5 就地订正（我的白名单不含它，没动）。

**那 S6 还是不是真缺陷？是。** 它只是**不是这一次 X2 的解释**：
当主机「旧连接的 `Fail` 还没跑、新连接已经到了」时，新连接会被 `AcceptLoop` 关掉
（客户端 `Connect` 已成功 ⇒ 它会以为连上了）。这一档现在有 `NET/BYE-S6/rejected` 一行，真连即可判。

**静态上剩下的候选（按可能性排）：**

| # | 候选 | 为什么还站着 | 下一次真连看哪一行 |
|---|---|---|---|
| ① | **发送侧「写完就拆」**（旧形状；2026-10-19 已改成 `CloseGracefully`，但只排空 200ms、不保发送缓冲） | `NetSession.Close` 旧形状 = `Send(bye)` 后立刻 `_t.Close()`；Windows 在「接收缓冲仍有未读数据」时拆 socket 可能走 abortive（RST），**挂起的发送数据一起丢** | 客户端：`NET/BYE-S1/sent` → `NET/BYE-S5/drain-eof` / `drain-timeout` / `shutdown-failed` / `drain-error` |
| ② | **接收侧「读到了但没进应用层」** | 半截帧 / 旧代帧 / 换连接清队列 / 会话被 `Reset` 换掉，都会让字节到了却不到 `Handle` | 主机：`收到 bye`（到了应用层） vs `NET/BYE-R1/truncated-*` / `NET/BYE-R2/*` / `NET/BYE-S6/read-loop-superseded` / `NET/BYE-R7/session-reset` |
| ③ | **S6（新连接被拒）** | 仍是真缺陷，只是**不是那一次** | `NET/BYE-S6/rejected`（含对端地址 + 连接位/计数快照） |

**一句话判法（同机两进程）**：
- 客户端只看三行：`NET/BYE-S1/sent`（写进传输层）· `NET/BYE-S4/write-failed`（写失败）· `NET/BYE-S5/*`（收尾细节）。
- 主机只看一行：`收到 bye`。
  - 客户端有 `sent`、主机**有** `收到 bye` ⇒ 闭环，问题在别处；
  - 客户端有 `sent`、主机**没有** `收到 bye`，且主机有 `NET/BYE-S6/rejected` ⇒ **S6**；
  - 客户端有 `sent`、主机没有，且客户端有 `drain-timeout` / `shutdown-failed` / `drain-error` ⇒ **候选 ①**；
  - 两边都没有任何相关行 ⇒ 会话没在泵（R5）或被 `Reset` 换掉（看 `NET/BYE-R7/session-reset`）。

---

## 二、静默丢弃点清单（文件 + 符号 + 触发条件 + 新日志前缀）

### 2·1 本轮**新加**探针（原来是静默 / 日志不具判别性）

| # | 文件 · 符号 | 触发条件 | 丢弃了什么 | 新前缀（唯一） |
|---|---|---|---|---|
| 1 | `NetTransport.AcceptLoop`（`NetTransport.cs:236`） | 接受新连接时 `_state` 的连接位仍立着 | **整条新连接**（上面若有 `bye`/握手包，一字节不读） | `NET/BYE-S6/rejected`（W57 已有该行；本轮**加前缀 + 对端地址 + `_state` 快照**） |
| 2 | `NetTransport.AcceptLoop` 的 `catch` | `AcceptTcpClient` 抛出；`_listener != null` = 非本机主动关 | 之后**所有新连接** | `NET/BYE-S6/accept-loop-error`（监听仍开着）/ `NET/BYE-S6/accept-loop-exit`（正常停） |
| 3 | `NetTransport.Setup`（`:355`）的 `catch` | accept 成功后 `GetStream()` / 起线程抛异常 | 这条**已接受**的连接 | `NET/BYE-S6/setup-failed` |
| 4 | `NetTransport.Setup` 的 `old.Close()` | 新连接顶替旧连接 | 旧 socket 里**还没读到的字节** | `NET/BYE-S6/old-connection-replaced` |
| 5 | `NetTransport.Setup` 的 `_inbox` 清空 | 换连接时队列里还有旧代帧 | **已入队**的旧代帧（最坏是 `bye`） | `NET/BYE-R2/stale-inbox`（原 W57 行，加前缀） |
| 6 | `NetTransport.ReadLoop`（`:433`）的 `while` 正常退出 | `_gen` 变了（本代被顶替） | 旧 socket 上**还没读到的字节** | `NET/BYE-S6/read-loop-superseded` |
| 7 | `NetTransport.ReadLoop` 帧头 `ReadFull` | 返回 `0 < n < 4`（半截帧头） | 半截帧头被当长度解（可能把半帧当整帧） | `NET/BYE-R1/truncated-header` |
| 8 | `NetTransport.ReadLoop` 帧体 `ReadFull` | 返回 `0 < n < len`（半截帧体） | 半截缓冲被 `Parse`（多半 `BadEnvelope`） | `NET/BYE-R1/truncated-body` |
| 9 | `NetTransport.ReadLoop` 入队前的代校验 | 读到帧之后发现代已变 | 旧代最后一帧（最坏是 `bye`） | `NET/BYE-R2/frame-dropped`（原 W57 行，加前缀） |
| 10 | `NetTransport.Fail`（`:508`）`gen != _gen` | 旧读线程迟到的失败 | 不改共享状态（**正确**） | `NET/BYE-R1/stale-reader-exit`（原 A1268 行，加前缀） |
| 11 | `NetTransport.Fail` | `_lastError` 已被占着、新原因不同 | **第二个原因**（字段是粘的） | `NET/BYE-R1/cause-superseded` |
| 12 | `NetTransport.Fail` 的 `_closing` 支 | 本机主动拆时读线程失败 | 「为什么失败」原来连日志都没有 | `NET/BYE-R1/local-close-failure`（**`Debug.Log` 级、不含「传输层掉线」**——`NetSelfTest` R⑨/R⑭ 的口径不变） |
| 13 | `NetTransport.Send`（`:578`）的守卫 | `_stream == null || !IsConnected` | 这一条报文 | `NET/BYE-S3/no-connection`（原行，加前缀） |
| 14 | `NetTransport.Send` 的 `catch` | `Write/Flush` 抛异常 | 这一条报文 | `NET/BYE-S4/write-failed`（**兜住**「同一原因第二次 `NetSession.Send` 认不出来」那条缝） |
| 15 | `NetTransport.CloseGracefully`（`:677`）无 socket 支 | `_client == null` 却要求优雅关闭 | 没有可排空的 socket | `NET/BYE-S5/no-socket` |
| 16 | `NetTransport.CloseGracefully` 的 4 条收尾 | Shutdown 失败 / 排空报错 / 超时 / 等到 EOF | ——（原已有日志） | 原行尾补 `NET/BYE-S5/{shutdown-failed,drain-error,drain-timeout,drain-eof}` |
| 17 | `NetSession.Pump`（`:195`）的 `Handle` 循环 | `Handle` 抛异常 | 本帧 + 队列里剩下的帧 | `NET/BYE-R1/handle-threw`（**照旧 `throw;`**，行为不变） |
| 18 | `NetSession.Handle` `case Challenge`（`:366`） | `MsgChallenge` 解不出（`m == null`） | 按**空 nonce** 继续握手（会算错口令） | `NET/BYE-R1/challenge-unparsable` |
| 19 | `NetSession.Handle` `case Bye`（`:546`） | 收到 `bye` 时会话已是 `Closed`/`Off` | 不是丢弃，是**第二遍告别**（R6） | `NET/BYE-R6/bye-after-local-close` |
| 20 | `NetSession.Send`（守卫 / 传输层失败） | 无活连接 / 写失败 | 该报文 | `NET/BYE-S2/no-live-link` · `NET/BYE-S4/net-session-send-failed` |
| 21 | `NetSession.Close` 的 `say` 闸 | `_t == null` / 未连 / `State == Off` | 这句 `bye` | `NET/BYE-S1/no-live-link`（原行，加前缀） |
| 22 | `NetSession.Close` 的成功行 | `bye` 已交给传输层 | ——（不是丢弃，是**发送侧唯一观测口**） | `NET/BYE-S1/sent`（原行，加前缀 + 客户端/主机两侧对账说明） |
| 23 | `NetSession.CloseTransportAfterBye`（`:738`） | 夹具 transport（非 `TcpTransport`） | 没有 `Shutdown`/排空 | `NET/BYE-S5/not-tcp`（说明这是正常档） |
| 24 | `NetRuntime.Reset`（`:180`） | 换会话，旧会话 `_inbox` 里还有帧 | 迟到的 `bye` / 对局帧 | `NET/BYE-R7/session-reset` |
| 25 | `NetMatchmaking.PumpLobby` `case Deck` | 卡组包解不出 / 空 | 这一条卡组 | `NET/BYE-R1/deck-unparsable`（原行，加前缀） |
| 26 | `NetMatchmaking.PumpLobby` `case Start`（`:641`） | `MsgStart` 解不出 → 原来**光秃秃 `break`** | 开局包（对面已进战场 ⇒ 两端分叉） | `NET/BYE-R1/start-unparsable` |
| 27 | `NetMatchmaking.HostStartMatch`（`:755`） | 没有会话 → 原来静默 `return` | 这一局开不了 | `NET/BYE-R7/no-session` |
| 28 | `NetBattle.WireSession`（`:307`） | `_s == null` → 原来静默 `return` | 之后 `bye`/掉线/`resume` **全部** | `NET/BYE-R7/no-session` |
| 29 | `NetBattle.Dispatch` `case Mulligan`（`:1212`） | 换牌包解不出 → 原来当「换了 0 张」 | 换牌结果（两端牌序会分叉） | `NET/BYE-R1/mulligan-unparsable` |
| 30 | `NetBattle.Dispatch` `case Resign`（`:1249`） | `_d == null` → 判负走不了 | 这次投降的结果 | `NET/BYE-R7/no-host` |
| 31 | `NetBattle.OnResumed`（`:1307`） | 主机收到 `resume`（方向反了） | 这一帧 | `NET/BYE-R1/resume-wrong-direction` |

### 2·2 本来就是「有明确日志」的（核对过，**没动行为**；只补了前缀的见上表）

| 文件 · 符号 | 触发条件 | 现有日志 |
|---|---|---|
| `TcpTransport.ReadLoop` 读异常 / EOF / 坏帧长 / 坏信封 | `ReadAbort` / `PeerClosed` / `BadFrame` / `BadEnvelope` | `Fail` → `_noteWarn`「传输层掉线：原因 = …」（A1267） |
| `TcpTransport.Fail` 旧代迟到 | 旧读线程退出 | `_noteLog`（A1268） |
| `NetSession.Handle` `case Proof` | 版本 / 密码 / 坏包 | `SetState(Closed, 拒绝…)` + `Send(Ack ok=false, 词条键)` |
| `NetSession.Handle` `case Ack` | `!ok` | `SetState(Closed, why)` + `OnClosed` |
| `NetSession.Handle` `case Reconnect` | token 不符 / 无 `ResumeProvider` | 发 `bye` + `SetState(Closed)` |
| `NetSession.Handle` `case Resume` | 坏包 | `Close(true, Wire/BadResume)` |
| `NetSession.Pump` 主机握手边沿 | `Challenge` 发不出去 | 「握手 `Challenge` **没发出去**…」 |
| `NetMatchmaking.PumpLobby` `default` | 大厅阶段收到不认识的消息 | 「大厅阶段收到不认识的消息」 |
| `NetBattle.Dispatch` `case Action` / `Applied` / `Reject` / `Hash` / `Resign` | 各种坏包 / 方向反 / 无 `Ctx` | 各自 `LogWarning` / `Abort` |
| `NetBattle.Tick` | `Dispatch` 抛异常 | `LogError`（逐帧 try/catch，**不会**丢后面几帧） |
| `NetProtocol.Parse` / `Unpack` | JSON 解不出 | `LogWarning`「帧/载荷解不出来」 |
| `NetApply.Apply` | `default` kind | 返回 `ErrUnimplemented`，调用方 `Reject` + `Abort` |

### 2·3 静默但**属于设计内**（只登记，不建议改）

- `NetSession.Pump`：`State == Closed/Off` 的早退在 **Handle 之后** ⇒ 已入队的帧仍会被处理（**不是丢弃**）。
- `NetBattle.OnLocalAction/OnLocalRawAction`：`_aborted` 后直接 return（中止那一刻已 `LogError` + 弹窗）。
- `NetRuntime.Update`：`Session == null` / `LobbyHandled == false` 的早退（没有帧可丢 / 交给对局层）。
- `NetMatchmaking.PumpLobby`：`s == null` 早退（它每帧都跑，打日志会刷屏）。
- `NetBattle.Tick`：`_s == null` 早退（同上）。

---

## 三、证据链

### 3·1 S6 为什么**不是** F5/X2 那次的原因（完整链）

```
① AcceptLoop 拒收的充要前提
   NetTransport.AcceptLoop（现读 `NetTransport.cs:269-289` 附近）
     int stateNow = Volatile.Read(ref _state);
     if ((stateNow & ConnBit) != 0) { …Enqueue(S6)…; c.Close(); continue; }
   ⇒ 被拒的连接【不会】走 Setup(c) ⇒ AcceptedCount 不变 ⇒ 读线程不启动 ⇒ 收不到任何帧。

② 主机到达 InBattle 的唯一路径
   NetSession.cs:254-282 主机握手边沿：AcceptedCount 变化 → Send(Challenge) → SetState(Handshaking)
   NetSession.cs:390-436  case Proof 成功 → SetState(Lobby)（`_wasInBattle` 时是 WaitingReconnect，**不是 InBattle**）
   NetSession.cs:495-509  case Reconnect（token 对）→ Send(Resume) → SetState(InBattle)
   ⇒ 主机 InBattle ⇔ 主机在新连接上**收过 proof、回过 ack、收过 reconnect**。

③ F5 的观测（资料/普查产出_第十一会话/F5_NetBattle夹具.md §三 A）
   E3/E4：:5303 主机 InBattle、PeerLost == false；:5340 才 WaitingReconnect（PeerLost 支）。
   而第 ① 步已断言主机先离开过 InBattle（当前夹具 Editor/NetBattleTest.cs §9① 仍这样断言）。
   ⇒ ②+③ ⇒ 那次重连的连接**被接受了** ⇒ S6 不成立。
```

> ⚠️ 依赖前提：F5 的 E3/E4 读数与夹具「第 ① 步先断言 `WaitingReconnect`」的顺序。
> 若某次真连日志与此矛盾，**以真连日志为准**（新探针会把它拍在脸上）。
> **S6 仍然是真缺陷**（时序上完全可能），只是**不是 F5 那一次**。

### 3·2 当前 `bye` 的全链观测口（真连时按这个顺序 grep）

```
【发送方】
  NET/BYE-S1/sent              ← 写进了传输层（不含「对面收到」）
  NET/BYE-S4/write-failed      ← 传输层写失败（同原因重复也能抓到）
  NET/BYE-S4/net-session-send-failed
  NET/BYE-S3/no-connection     ← 写的那一刻连接位是断的
  NET/BYE-S2/no-live-link      ← 会话层先拦下
  NET/BYE-S1/no-live-link      ← Close 的闸（原本就该出声的那一档）
  NET/BYE-S5/drain-eof | drain-timeout | shutdown-failed | drain-error | no-socket | not-tcp
【接收方】
  收到 `bye`                    ← 到了应用层（唯一「真收到」的判据）
  NET/BYE-S6/rejected          ← 这条连接被本机接受线程关掉（S6）
  NET/BYE-S6/old-connection-replaced | read-loop-superseded | setup-failed
  NET/BYE-R2/frame-dropped | stale-inbox          ← 读到了 / 入队了，但被换代丢掉
  NET/BYE-R1/truncated-header | truncated-body    ← 半截帧
  NET/BYE-R1/handle-threw      ← 应用层处理抛异常（后面几帧一起丢）
  NET/BYE-R6/bye-after-local-close ← 第二遍告别（本机先关）
  NET/BYE-R7/session-reset | no-session           ← 会话被换掉 / 没挂上
  传输层掉线：原因 = …          ← 读线程失败（FIN / RST / 超时，A1267）
```

⚠️ `_noteWarn` / `_noteLog` 是**后台线程入队、主线程 `Pump()` 打印**（`NetTransport.cs` 文件头的线程规矩）。
若进程在**下一次 `Pump()` 之前**退出，这些行可能来不及打印 —— 真连测试**关客户端前先停一秒**再关。

---

## 四、`bye` 对数缺口（只报告，**没改**）

1. **没有应用层回执/重试**：`Close(true)` 发一次 `bye` 就完，不等对端回执；丢了只能靠对端
   `SilentTimeoutMs`（10 s）发现，而那已经是「掉线」而不是「他走了」。TCP 本身不给你「已送达应用层」。
2. **发送侧只排空 200 ms、且只等「对面关」**（`CloseTransportAfterBye` → `CloseGracefully`，
   `NetTransport.cs:677-716`）：`Shutdown(Send)` 之后等对端 FIN；超时就硬拆。
   Windows 在「接收缓冲还有未读数据」时硬拆可能走 abortive（RST）⇒ **挂起的发送数据一起丢**。
   `GracefulCloseDrainMs = 200` 没在真网络量过（W57 §四·2 已记）。
3. **收 `bye` 的一方直接 `_t.Close()`**（`NetSession.cs:575`）：没有 `Shutdown(Send)`、没有排空、没有
   `Shutdown(Both)` 的半关闭礼节。它自己已经没什么要发的，但**硬拆**会在对端还在 200 ms 排空时
   丢一个 RST 过去（对端会看到 `NET/BYE-S5/drain-error`）。**建议**：收 `bye` 这条也走
   `CloseTransportAfterBye(false)` 的优雅档（见 §五·③）。
4. **同时收尾（双方都 `Close(true)`）**：两边都发 `bye`、都排空，之后各自的 `Pump` 仍会处理对端那条迟到的
   `bye` ⇒ `OnClosed` 可能第二次触发（`NetBattle.HandlePeerClosed` 会再说一遍）。现在有
   `NET/BYE-R6/bye-after-local-close` 标记（W57 顺手发现 #1 的观测口）。
5. **`Reset()` 换会话时旧收件箱不排空**（`NetRuntime.cs:180`）：旧会话 `_inbox` 里的迟到 `bye` 随对象丢；
   已有 `NET/BYE-R7/session-reset`。
6. **「先回一句、再硬拆」的同族（不止 `bye`）**：`NetSession.Handle` `case Proof` 的拒绝支
   （现读 `NetSession.cs:427-441`：`Send(Ack ok=false, 理由)` **紧接着** `_t.Close()`）—— 与 `bye` 旧形状同构，
   **这句拒绝理由有可能到不了对面**（对面只看到连接断了）。`case Resume` 已走 `Close(true)`，
   `case Reconnect` 两条拒绝支已走 `CloseTransportAfterBye`，**只剩 `case Proof` 这一处**。
7. **主机倒计时到点判负后的 `Close(true, Wire/RoomGone)`** 在「对面已经没了」时必然触发
   `NET/BYE-S1/no-live-link` —— **预期噪声**（W57 顺手发现 #5），别当真丢包。

---

## 五、修复建议（**一条都没改**，等主对话裁定）

| 优先级 | 建议 | 落点 | 判据/理由 |
|---|---|---|---|
| ① | **真连一次，按 §三·2 的 grep 配方钉死** | —— | 现在唯一还缺的是实测；静态已把 S6 从「最可疑」降为「仍在但非本次」 |
| ② | 发送侧：`CloseGracefully` 的输出侧确认 —— `Shutdown(Send)` 之后**先确认发送缓冲已清**
（`Socket.Poll(SelectWrite)` + `Available` / `IOControl(SIOCOOKIE)`），再等对端 FIN | `NetTransport.CloseGracefully` | 现在只等「对面关」；若对面不关就 200 ms 后硬拆，可能 abortive |
| ③ | 收 `bye` 的一方改走 `CloseTransportAfterBye(false)`（即真 TCP 上 `Shutdown(Send)`+排空+`Close`） | `NetSession.Handle` `case Bye` | 避免对端排空期间收到 RST；也顺带覆盖「同时收尾」 |
| ④ | `case Proof` 的拒绝支：`Send(Ack)` 之后不要立刻 `_t.Close()`，走优雅收尾（同上） | `NetSession.Handle` `case Proof` | 与 `A1264` 同族（「说完就拆」）——现在只有这一处还留着旧形状 |
| ⑤ | `Close` 之后清 `_inbox`（R6 的根治） | `NetSession.Close` / `INetTransport` 加口 | 需要改接口（`RecordingTransport` 等三个实现），改动面大，按铁律 13 另开 |
| ⑥ | 把 `bye` 的送达做成**有界重试**（发 `bye` 后等 `bye.ack` 或 200 ms；超时不再重发但**记下来**） | 协议层 | 现在零回执；但这是新协议消息，需用户/主对话拍板 |

---

## 六、typecheck 与改动清单

```
TMPDIR=/tmp/wf_netprobe bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

`git diff --numstat`（只动了这 5 个文件，全部在白名单内）：

```
28   2   Net/NetBattle.cs
26   3   Net/NetMatchmaking.cs
10   1   Net/NetRuntime.cs
47   8   Net/NetSession.cs
105  13  Net/NetTransport.cs
```

行尾：改后全部 **纯 LF**（`CRLF 0`）：`NetTransport 747` · `NetSession 835` · `NetRuntime 214` ·
`NetBattle 1326` · `NetMatchmaking 859` 行 —— 没有整篇翻行尾。
**没动**：`NetApply.cs` / `NetProtocol.cs` / `NetConfig.cs` / `UpnpPortMapper.cs`（没有需要加的静默点）；
`BattleDriver.cs` / `RuleEngine/**` / `Shell/**` / 正本 / `项目任务.md` / `CLAUDE.md` 一个字节没碰；
没跑 Unity、没跑 `*.Run`、没动 git。
