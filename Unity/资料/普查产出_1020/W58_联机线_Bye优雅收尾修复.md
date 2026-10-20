# W58-NETFIX · 联机线 `T05` —— `bye` 优雅收尾四条（②③④⑤）

> 2026-10-20 · 执行代理 **W58-NETFIX** · 判据 = `资料/普查产出_1020/W58_联机线_Bye丢包点与探针.md`
> 白名单 = `Unity/MyGame/Assets/CardPresentation/Net/*.cs`（实际只动 **`NetTransport.cs` / `NetSession.cs`**）
> ⛔ 没跑 Unity / 没跑 `*.Run` / 没真连 / 没动 git / 没改正本 / 没碰 `Editor/`（铁律 12）。
> ✅ 只跑了秒级类型检查（结果 §七）。

---

## 一、结论

**②③④⑤ 四条全部落地，`typecheck` 0/0。**

- **②** `CloseGracefully` 现在在 `Shutdown(Send)` 之后、等对端 FIN 之前，**有界地确认本机发送缓冲不再阻塞**
  （`Poll(SelectWrite)`，与 FIN 等待**共享同一个 200 ms 预算**）；成功/超时/异常三个都出声。
  ⚠️ **语义边界已写死在注释里**：`Poll(SelectWrite)` 只证明「**本机内核**不再阻塞发送」，
  **不证明对端 ACK、更不证明应用层收到** —— 端到端确认仍然只有 `drain-eof`。
- **③** 收 `bye` 的一方从 `_t.Close()`（硬拆）改成**同一个收口点**的优雅档（真 TCP：`Shutdown(Send)` + 有界排空 + `Close()`）。
  **同族顺手**：客机收到 `ack ok=false` 的那一支（与 ③ 同形状、原报告没点名）也一并改成优雅档。
- **④** `case Proof` 拒绝支接住 `Send(Ack)` 的返回值：**真写出去了才走优雅档**，没写出去就照旧直接关（不白等 200 ms）。
- **⑤** `TcpTransport.Close()` 现在**清空收件箱**，并给读线程入队口加了 `_closed` 闸 —— 两道合起来堵死
  「`Close()` 之后那句迟到的 `bye` 又被 `Pump` 交给 `Handle`」的整个窗口（R6 的根）。
  接口改动走**能力接口 `IPendingFrameDropper`**（⛔ 没往 `INetTransport` 加必选成员，见 §五）。
- **⑥ / ① 未做**（原因见 §六）。
- **31 处 `NET/BYE-*` 判别日志全部保留，前缀一个没改**（清单见 §四；本轮**新增** 4 个前缀，见同节）。

---

## 二、②–⑤ 逐条（改前 / 改后 + 符号）

### ② 输出侧确认发送缓冲 · `TcpTransport.CloseGracefully(int drainMs)`

**改前**：`Shutdown(Send)` → 有界等**对端 FIN**（`Poll(Read)` + `Available==0`，≤200 ms）→ `Close()`。
只关心「对面关没关」，**从不确认我方发送缓冲是否已经腾空**。发送队列非空时拆 socket，
Windows 可能走 abortive（RST），把挂起数据一起丢掉 —— 即 NETPROBE 候选 ①。

**改后**（`NetTransport.cs` · `CloseGracefully`）：
1. `Shutdown(SocketShutdown.Send)`（不变）。
2. 🆕 **①·b 输出侧确认**：在**同一个 `Stopwatch` 预算**内（≤ `GracefulCloseDrainMs`，⛔ 不是另加 200 ms）
   循环 `s.Poll(slice*1000, SelectMode.SelectWrite)`：
   - 为真 ⇒ `Debug.Log` **`NET/BYE-S5/send-buffer-drained`** + `SendBufferConfirmedCountForTest++`；
   - 预算耗尽 ⇒ `Debug.LogWarning` **`NET/BYE-S5/send-buffer-timeout`** + `SendBufferTimeoutCountForTest++`，
     照旧继续收尾（有界）；
   - 抛异常 ⇒ `Debug.LogWarning` **`NET/BYE-S5/send-buffer-error`**，跳到 FIN 等待。
3. 原有 FIN 等待（预算已被上一步用掉一部分；`drain-eof` / `drain-timeout` / `drain-error` 前缀不变）。
4. `Close()`。

**符号**：`TcpTransport.CloseGracefully` · 新读口 `SendBufferConfirmedCountForTest` / `SendBufferTimeoutCountForTest`。

### ③ 收 `bye` 的一方走优雅收尾 · `NetSession.Handle` `case NetKind.Bye`

**改前**：`SetState(Closed, why)` 之后 **`_t.Close()`** —— 硬拆。对端可能还在它那 200 ms 排空里，
一个 RST 打过去会让它看到 `NET/BYE-S5/drain-error`。

**改后**：`CloseTransportAfterBye(false, true)` —— 走**唯一收口点**的优雅档：
- 真 `TcpTransport`：`CloseGracefully()`（`Shutdown(Send)` + 有界排空 + `Close()`；本机没有 `bye` 要发，
  `sentBye:false` 只表示「不需要为发出 `bye` 额外等」，`graceWithoutBye:true` = 「给对端半关闭礼节」）；
- 夹具 transport：照旧 `Close()`，多一行 **`NET/BYE-S5/not-tcp`** 说明（设计内，不是缺陷）。

**同族顺手（③ 的孪生，原报告 §五 未点名）**：`case NetKind.Ack` 的 `!m.ok` 支原来也是 `_t.Close()`。
主机在 ④ 之后会优雅收尾，客机这一记硬拆会把主机的排空打成 `drain-error`，而**那句拒绝理由**正是它要送来的
⇒ 一并改成 `CloseTransportAfterBye(false, true)`。

**符号**：`NetSession.Handle`（`case Bye` / `case Ack`）· `NetSession.CloseTransportAfterBye(bool sentBye, bool graceWithoutBye = false)`（新参数）。

### ④ `case Proof` 拒绝支 · `NetSession.Handle`

**改前**：`Send(NetKind.Ack, ok=false, 理由)` 之后**立刻** `_t.Close()`（与 `bye` 旧形状同构）
⇒ 那句拒绝理由可能到不了对面（对面只看到「连上了又断了」）。原来 `Send` 的返回值被丢弃。

**改后**：
```csharp
bool sentAck = Send(NetKind.Ack, new MsgAck { ... });
if (why != null)
{
    LastError = why;
    SetState(NetState.Closed, ...);
    CloseTransportAfterBye(sentAck);   // ← 原来这一行是 _t.Close()
    ...
}
```
- `sentAck == true` ⇒ 真 TCP 上 `Shutdown(Send)` + 有界排空（让 `ack` 先出去）；
- `sentAck == false` ⇒ 没有要排空的字节，照旧直接关（⛔ 不白等 200 ms）。

**符号**：`NetSession.Handle` `case NetKind.Proof`。

### ⑤ `Close` 清 `_inbox`（R6 根治）· `TcpTransport.Close` + 读线程入队口

**改前**：`TcpTransport.Close()` = 停监听 + `ClosePeer()`；`_inbox` 原封不动。
而 `NetSession.Pump()` 是**先把帧交给 `Handle`、之后才判状态** ⇒ 本机 `Close()` 之后，
队列里一句迟到的 `bye` 仍会演一遍「对面离开了」（`NET/BYE-R6/bye-after-local-close`）。

**改后**（两道闸，缺一不可）：
1. **已经进队的**：`TcpTransport.Close()` 末尾 `DropPendingFrames("Close")` —— 取空 `_inbox`；
   有丢弃时 `Debug.Log` **`NET/BYE-R6/inbox-cleared`**，累计到 `DroppedPendingFrameCountForTest`。
2. **正要进队的**：新字段 `volatile bool _closed`（`Close()` 置真、`Setup()` 清假）；`ReadLoop` 入队前
   `if (_closed)` ⇒ **不再入队**并记 **`NET/BYE-R6/inbox-cleared`**（含代次 + 被丢帧的 kind，≤32 字）。
   ⛔ 没用 `_closing`：那个也覆盖 `ClosePeer()`（掉线/重连），那一档读线程手里的最后一帧仍可能要。

**接口怎么改的**：新加**能力接口** `IPendingFrameDropper { int DropPendingFrames(string cause); }`，
由 `TcpTransport` 实现（`class TcpTransport : INetTransport, IPendingFrameDropper`）。
⛔ **没往 `INetTransport` 加必选成员** —— 它的另外两个实现（`Editor/BattleScene.cs` 的 `RecordingTransport`、
`Editor/NetSelfTest.cs` 的 `ScriptedTransport`）在**本轮白名单外**；默认接口实现（DIM）在 Unity Mono 上
支持面不确定，**不拿「大概支持」下注**。⇒ 能力接口 + 类型探测；夹具没有跨帧积压语义，照旧 `Close()`。

**符号**：`TcpTransport.Close` · `TcpTransport.DropPendingFrames(string)` · `TcpTransport._closed` · `ReadLoop` 入队闸 ·
`IPendingFrameDropper` · 读口 `PendingFrameCountForTest` / `DroppedPendingFrameCountForTest`。

**没做、如实记着**：`NetSession.Inbox`（**对局消息**的 List，与传输层 `_inbox` 不是同一个东西）本轮没动 ——
任务原文的 `_inbox` 指传输层那个；会话层的清场属于 `Reset`/`NetBattle` 的语义，不在本笔范围内。

---

## 三、静态可验证的测试口（全是新增只读口，生产路径一处都不读）

| 读口 | 在哪 | 断言什么 |
|---|---|---|
| `NetSession.GracefulCloseRequestedForTest` | `NetSession.cs` | 本轮「**该走优雅档**」的次数（`sentBye` 或 `graceWithoutBye`）—— ③④ 的**收尾意向**，夹具也验得到 |
| `NetSession.HardCloseRequestedForTest` | `NetSession.cs` | 「没有告别、也不需要对端礼节」⇒ 直接硬关的次数（`Close(false)` 那一档，**行为未变**） |
| `TcpTransport.SendBufferConfirmedCountForTest` | `NetTransport.cs` | ② 输出侧确认成功的次数 |
| `TcpTransport.SendBufferTimeoutCountForTest` | `NetTransport.cs` | ② 确认超时的次数（那一档必带 `LogWarning`） |
| `TcpTransport.PendingFrameCountForTest` | `NetTransport.cs` | ⑤ 此刻收件箱里还没被 `Pump()` 取走的帧数 |
| `TcpTransport.DroppedPendingFrameCountForTest` | `NetTransport.cs` | ⑤ 累计丢弃帧数 |

⚠️ 这些口**不区分夹具/真 TCP 的「真的排空了吗」** —— 那只有真连或真 socket 能验；
夹具上验的是**收尾语义**（该不该走优雅档），不是 socket 行为（同 `NetSelfTest` §O/§P 的既有口径）。

### 可粘贴块（`Editor/NetSelfTest.cs` 在白名单外，本轮没动；主对话决定要不要粘）

```csharp
// ==================================================================
//  §W58（2026-10-20 · W58-NETFIX）：bye 收尾四条（NETPROBE ②③④⑤）
//  粘进 Editor/NetSelfTest.cs 后，在 Run 里加一句 TestByeGracefulClose(FreePort());
// ==================================================================
static void TestByeGracefulClose(int port)
{
    // ③ 收 `bye` ⇒ 优雅档（不是硬拆）：夹具上验「收尾意向」
    {
        var tr = new ScriptedTransport();
        var s = new NetSession(tr);
        tr.Inject(NetKind.Bye, NetProtocol.Pack(new MsgBye { reason = "W58-test" }));
        s.Pump();
        Ok(s.State == NetState.Closed, "W③① 收到 bye ⇒ 会话 Closed");
        Eq(s.GracefulCloseRequestedForTest, 1, "W③② 收 bye 走的是**优雅收尾**（NETPROBE ③）");
        Eq(s.HardCloseRequestedForTest, 0, "W③③ 没有走硬关");
    }

    // ④ `case Proof` 拒绝支：Ack 写出去了 ⇒ 优雅档（理由要先出门）
    {
        var tr = new ScriptedTransport();
        var s = new NetSession(tr);
        Ok(s.StartHost(Cfg(port, "")), "W④① 主机起来（夹具）");
        int closed = 0; s.OnClosed = _ => closed++;
        tr.Inject(NetKind.Proof, NetProtocol.Pack(new MsgProof
        { protoVer = NetProtocol.Version + 1, gameVer = "W58", name = "W58", proof = "" }));
        s.Pump();
        Ok(s.State == NetState.Closed, "W④② 版本不符 ⇒ 拒绝并 Closed");
        Eq(closed, 1, "W④③ `OnClosed` 叫了一次");
        Eq(s.GracefulCloseRequestedForTest, 1, "W④④ 拒绝支走**优雅收尾**（ack 先出去再拆）");
        // ⚠️ 别断 `HardCloseRequestedForTest == 0`：`StartHost` 内部那句 `Close(false)` 本来就会计一次。
    }

    // ⑤ 真 TCP 回环：Close() 清收件箱（R6 根治口）
    {
        var a = new TcpTransport();
        var b = new TcpTransport();
        a.Listen(port);
        Ok(a.IsListening, "W⑤① 监听起来了");
        Ok(b.Connect("127.0.0.1", port, 3000), "W⑤② 裸传输连得上");
        for (int i = 0; i < 400 && !a.IsConnected; i++) Thread.Sleep(5);
        Ok(a.IsConnected, "W⑤③ 接受线程把连接发布了");
        b.Send("test.w58", "{}");
        for (int i = 0; i < 400 && a.PendingFrameCountForTest == 0; i++) Thread.Sleep(5);
        Eq(a.PendingFrameCountForTest, 1, "W⑤④ 帧已经进了收件箱（还没人 Pump）");
        a.Close();
        Eq(a.PendingFrameCountForTest, 0, "W⑤⑤ Close() 之后收件箱清空");
        Eq(a.DroppedPendingFrameCountForTest, 1, "W⑤⑥ 丢弃计数 = 1（`NET/BYE-R6/inbox-cleared` 的来源）");
        b.Close();
    }
}
```

⚠️ **可粘贴块没跑过**（本轮不跑 `*.Run`、不跑 Unity）—— 主对话在同步点决定是否执行。
若 W⑤④ 偶发 0：`b.Send` 与 a 侧读线程之间有正常调度延迟，循环给了 2 s；仍失败再查 `a.LastError`。

---

## 四、保留的探针清单（31 处 `NET/BYE-*`，前缀一个没改）

> W58-NETPROBE 的 31 条全部原样保留（只**新增**日志，没删、没改前缀）。按文件：

- `NetTransport.cs`：`S6/rejected` · `S6/accept-loop-error` · `S6/accept-loop-exit` · `S6/setup-failed` ·
  `S6/old-connection-replaced` · `R2/stale-inbox` · `S6/read-loop-superseded` · `R1/truncated-header` ·
  `R1/truncated-body` · `R2/frame-dropped` · `R1/stale-reader-exit` · `R1/cause-superseded` ·
  `R1/local-close-failure` · `S3/no-connection` · `S4/write-failed` · `S5/no-socket` ·
  `S5/{shutdown-failed,drain-error,drain-timeout,drain-eof}`
- `NetSession.cs`：`R1/challenge-unparsable` · `R1/handle-threw` · `R6/bye-after-local-close` ·
  `S1/no-live-link` · `S1/sent` · `S2/no-live-link` · `S4/net-session-send-failed` · `S4/write-failed` ·
  `S5/not-tcp`
- `NetRuntime.cs`：`R7/session-reset` · `NetMatchmaking.cs`：`R1/deck-unparsable` · `R1/start-unparsable` · `R7/no-session` ·
  `NetBattle.cs`：`R1/mulligan-unparsable` · `R1/resume-wrong-direction` · `R7/no-host` · `R7/no-session`

**本轮新增（全部 `NET/BYE-*` 同族，便于同一条 grep 捞到）**：

| 前缀 | 级别 | 含义 |
|---|---|---|
| `NET/BYE-S5/send-buffer-drained` | `Debug.Log` | ② 发送缓冲已确认不再阻塞（**不**代表对端收到） |
| `NET/BYE-S5/send-buffer-timeout` | `LogWarning` | ② 有界预算内没能确认（对端接收窗口可能被堵） |
| `NET/BYE-S5/send-buffer-error` | `LogWarning` | ② 确认过程中 socket 报错（照旧往下走） |
| `NET/BYE-R6/inbox-cleared` | `Debug.Log` | ⑤ `Close()` 清收件箱 / 读线程被 `_closed` 闸拦下的迟到帧 |

⚠️ `NET/BYE-R6/bye-after-local-close` 的原有文案**原样保留**，只在其后追加了一句
「⚠️ 2026-10-20 W58-NETFIX ⑤ 之后：`Close()` 已清收件箱 + 入队口加了 `_closed` 闸，这一档只剩
『本机没走 Close 的换会话/夹具路径』」——**前缀与判据不变**。

---

## 五、⑥ / ① 为什么没做

- **⑥ 有界 `bye` 回执（新协议消息）⇒ 不做**：要新增 `bye.ack` 一类的消息种类与等待/重发语义，
  会**改线上协议**（旧端的 `Handle` 不认识新 kind ⇒ 落到「其余交给上层」/ 大厅 default 告警），
  属于需要**用户/主对话拍板**的协议变更（NETPROBE §五 ⑥ 自己也这么标注）。
  本轮范围只有「收尾语义」，⛔ 没有夹带协议消息。要不要做、做成 `bye.ack` 还是「有界重发」，
  等拍板后另开一笔。
- **① 真连验证 ⇒ 不做**：任务口径明确「不真连」；本轮只做静态修复 + 静态读口。
  真连的判据/配方见下面 §六（原 NETPROBE §三·2 已按本轮新日志更新）。

---

## 六、下次真连的 grep 配方（按本轮新日志更新）

```bash
# 发送方（客机/主机都可能有）—— 顺序读
grep -nE "NET/BYE-S1/sent"                       client.log   # ① 写进传输层
grep -nE "NET/BYE-S5/send-buffer-(drained|timeout|error)" client.log  # ② 输出侧确认（新）
grep -nE "NET/BYE-S5/(drain-eof|drain-timeout|shutdown-failed|drain-error)" client.log
grep -nE "NET/BYE-S4/(write-failed|net-session-send-failed)|NET/BYE-S3/no-connection|NET/BYE-S2/no-live-link" client.log

# 接收方（主机）—— 唯一「真收到」的判据
grep -nE "收到 \`bye\`"                          host.log
grep -nE "NET/BYE-(S6/rejected|R2/(frame-dropped|stale-inbox)|R1/(truncated-header|truncated-body)|R7/session-reset)" host.log
grep -nE "NET/BYE-R6/inbox-cleared"              host.log      # ⑤ 生效（收尾清场，不是缺陷）
grep -nE "NET/BYE-R6/bye-after-local-close"      host.log      # ⑤ 之后正常真连应当**看不到**它
```

**判别矩阵（本轮新增的两行）**：

| 观测 | 结论 |
|---|---|
| 发送方 `sent` + `send-buffer-timeout`，接收方**没有** `收到 bye` | ② 的新信号：对端接收窗口被堵 / 不读；这时 `drain-timeout` 多半也同时出现 ⇒ 先查对面为什么没在消费 |
| 发送方 `sent` + `send-buffer-drained` + `drain-eof`，接收方**有** `收到 bye` | 闭环；②③ 都走完 |
| 发送方 `sent`，接收方没有 `收到 bye`，但有 `NET/BYE-R6/inbox-cleared` | ⑤ 生效：告别到了但本机已经在收尾（如果本机**没**主动关过，才怀疑时序） |
| 收 `bye` 的主机日志里出现 `NET/BYE-S5/drain-error` | 对面在硬拆（RST）；若对面是**旧构建**，这属于 ③ 修复前的已知形状 |

⚠️ `_noteWarn` / `_noteLog` 仍是「后台入队、主线程 `Pump()` 打印」；**关客户端前先停一秒**再关
（否则最后几行可能来不及打印）—— 与本轮改动无关，原 NETPROBE §三·2 的口径不变。

---

## 七、typecheck / 改动清单 / 行尾

```
TMPDIR=/tmp/wf_netfix bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

`git diff --numstat`（只这两份是本笔动的；数字含 W58-NETPROBE 上一轮的探针改动，工作区累积口径）：

```
109  15  Unity/MyGame/Assets/CardPresentation/Net/NetSession.cs
243  16  Unity/MyGame/Assets/CardPresentation/Net/NetTransport.cs
28    2  Unity/MyGame/Assets/CardPresentation/Net/NetBattle.cs      ← 上一轮 NETPROBE 的探针，本笔没动
26    3  Unity/MyGame/Assets/CardPresentation/Net/NetMatchmaking.cs ← 同上
10    1  Unity/MyGame/Assets/CardPresentation/Net/NetRuntime.cs     ← 同上
```

**行尾**（二进制读）：`NetTransport.cs` **CRLF 0 / LF 882** · `NetSession.cs` **CRLF 0 / LF 888** —— 没翻行尾。

**没碰**：`NetApply.cs` / `NetProtocol.cs` / `NetConfig.cs` / `UpnpPortMapper.cs` / `NetBattle.cs` /
`NetMatchmaking.cs` / `NetRuntime.cs`（本笔）；`BattleDriver.cs` · `RuleEngine/**` · `Shell/**` · `Editor/**` ·
正本 / `项目任务.md` / `CLAUDE.md` 一个字节没碰；没跑 Unity、没跑 `*.Run`、没动 git。
