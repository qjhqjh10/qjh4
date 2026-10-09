# N1 · 只读诊断：`NetBattleTest.Run` 那三条红（182 通过 / 3 失败）

> 只读代理 N1 的产出。**只诊断、未改任何文件**（除本文件）。⛔ 没跑 Unity、没动 git 写操作。
> 日志 = `D:/4/_tmp_view/netbattle.log`（2026-10-09 13:13:11 → 13:13:34）· 测试 = `Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs`
> · 产品 = `Unity/MyGame/Assets/CardPresentation/Net/{NetSession,NetTransport,NetBattle}.cs`

---

## 结论一句话

三条 ✗ **同一个根因**：③ 那一刻客机的 `bye` **根本没进到主机的收包队列**（主机走的是「对面掉线」那条 `_t.PeerLost` 支，不是 `case NetKind.Bye`）
⇒ 后面两条（文案 / 提示行）只是它的下游。
根因落在**测试夹具的时序**上（② 只等主机、不等客机把 `resume` 收完）+ 一条**真实的 socket 层脆弱点**（对端还有未读入站数据时关台 = abortive close，刚写出去的 `bye` 会被丢掉）。
**不是 γ（本批回归）** —— 这条链上本会话**零功能改动**（只有注释 + 词条值），而同一套代码**今天早些时候还是 185/0**。

---

## ① 逐条判定（α 断言错 / β 实现缺陷 / γ 本批回归 / δ 夹具前提不成立 / ε 抖动）

| # | 断言（日志行） | 判定 | 判据 |
|---|---|---|---|
| 1 | `（对面离开）主机收到 bye ⇒ 会话关上（实际 WaitingReconnect）` `:5384` | **(δ) 夹具前提不成立 +（ε）抖动**（同一件事的两面）—— 附带查出一条**真·脆弱点**（见 ⑤·B） | 失败点**不在**本会话改过的任何代码上：主机走的是 `NetSession.cs:205-214` 那条 `_t.PeerLost` 支（栈 `Pump:209/213` 逐字对上），而 `case NetKind.Bye`（`:514-530`）**一次都没走到**。`bye` 未进收包队列 = 传输层/时序问题，不是断言算错（断言期望的行为是对的、也是我们自己的口径） |
| 2 | `★（对面离开）那条话里带着对面报的理由` `:5409` | **同一根因的下游（δ/ε）**，无独立成因 | 它读的是 `NetRuntime` 通知队第一条，实得「对手掉线了 —— 正在等他回来。…」= `HandlePeerLost` → `SayPeerGone`（`NetBattle.cs:428` ← `:328`）那一句。⚠️ 注意上一格 `✓ ★（对面离开）弹了一条`（`:5397`）**是绿的** —— 因为**两条路径都恰好弹 1 条** ⇒ 数条数那一格**认不出是哪条路径**，真正判别的是这一格的 `Contains`。修好 #1（或 `bye` 送达）即绿 |
| 3 | `★（对面离开）提示行也说了` `:5423` | 同上 | `hb9.LastSay` 实得「对手掉线了 —— 正在等他回来（27 秒后判他弃权）」= 掉线那条的 `NetSay` |

**三个都不是 α**：三条期望的都是「对面明确离开 ⇒ 会话关上 + 说起来说的是『离开』」，与 `NetBattle.HandlePeerClosed` 的口径一致，且 `A912①/③/⑤`（`NetBattleTest.cs:1184/1221/1274`）在**同一份代码里是绿的**（日志 `:8822/:9333/:10313`）。

---

## ② `bye` 这条路本来该怎么走（逐句，`文件:行号`）

1. 客机 `NetSession.Close(say:true, reason)` → `NetSession.cs:619-641`：`say` 为真且「有活连接且不是 `Off`」时 `Send(NetKind.Bye, new MsgBye{reason=key})`（`:632`），否则**打警告**（`:625-627`）；随后 `_t.Close()`（`:634`）。
2. → `NetSession.Send`（`:564-586`）→ `TcpTransport.Send`（`NetTransport.cs:344-399`）：`s.Write + s.Flush`（`:391`）。守卫不过 / 写失败各有**两处出声**（`NetTransport.cs:379-392` · `NetSession.cs:578-582`）。
3. 主机 `TcpTransport.ReadLoop`（`NetTransport.cs:300-330`）：读到 `Bye` 帧 → `_inbox.Enqueue`（`:325`）；下一轮 `ReadFull` 拿 `n<=0` ⇒ `Fail(St/PeerClosed)`（`:306`）或抛异常 ⇒ `Fail(St/ReadAbort)`（`:305`）→ `Fail` 里 `_peerLost = true` 且清连接位（`:326-331`）。
4. 主机 `NetSession.Pump`（`:196-215`）：**先** `_t.Pump(_buf)` 把 inbox 抽干（`:198`）→ **逐帧 `Handle`**（`:199`）→ **才**判掉线（`:205`）。
   `Handle` 的 `case NetKind.Bye`（`:514-530`）：`SetState(NetState.Closed, why)`（`:527`）→ `_t.Close()`（`:528`）→ `OnClosed(why)`（`:529`）。
5. → `NetBattle.WireSession` 的 `_s.OnClosed += HandlePeerClosed`（`NetBattle.cs:283`）→ `HandlePeerClosed`（`NetBattle.cs:341-...`）→ `SayPeerGone`（`:428`）。
6. **对照**（掉线那条）：`_s.OnPeerLost += HandlePeerLost`（`NetBattle.cs:282`）→ `HandlePeerLost`（`:326`）→ `StartReconnectCountdown`（`:550`）+ `SayPeerGone(ReconnectHint, ReconnectPopup)`（`:328`）。
   ⇒ 两条**确实是两个回调**，测试那条注释没错。

**关键推论**：`Pump` 是「先抽干 inbox、再判 PeerLost」，而 ③ 的 `pumpOne`（`NetBattleTest.cs:615-620`）整整跑了 **4000 ms**、每 3 ms 一次 `Pump`（≈1300 次）。
**只要 `bye` 曾经进过 `_inbox` 一次，其中任意一次 `Pump` 都会把它 `Handle` 掉 ⇒ `State = Closed` ⇒ #1 必绿**。
它 4 秒里一次都没绿 ⇒ **`bye` 从未进过主机的 `_inbox`** ⇒ **它没到、或被丢弃**（不是「被 PeerLost 抢先」，那个顺序在代码上不成立）。

---

## ③ 本会话改过的文件有没有落在这条链上 —— **没有**

链路涉及的文件：`Net/NetTransport.cs` → `Net/NetSession.cs` → `Net/NetBattle.cs` → `Net/NetRuntime.cs`（+ `Core/Loc.cs` 提供文案）。

| 文件 | 本会话改动 | 是否在链上 | 影响 |
|---|---|---|---|
| `Net/NetSession.cs` | **0**（`git diff HEAD --stat` 未列出） | 是 | 无 |
| `Net/NetTransport.cs` | **0** | 是 | 无 |
| `Net/NetRuntime.cs` | **0** | 是 | 无 |
| `Net/NetBattle.cs` | **4 增 1 删，全在 `//` 注释块**（`git diff` 逐行看过：只把一句「本文件的旧行号 `:952`」改成指符号名） | 是 | 无功能影响 |
| `Core/Loc.cs` | 135 行，**全是 `new Entry` 的取值 + 注释**（改动行里**没有一行是代码**） | 是（提供文案） | 与本次失败**无关**：失败那一跳走的是 `HandlePeerLost` 的文案，`Handle(Bye)` / `HandlePeerClosed` **一次都没执行** |
| `Battle/*` · `RuleEngine/Core/*` · `Shell/*` | 有 | **否** | 这些类在本跳里**根本没有被调用**（③ 用的是 `BareHost`，不是 `BattleDriver`；socket 读在**后台线程** `NetTransport.ReadLoop` 上） |

⇒ **这条链上一个本会话改过的【功能】代码都没有**（只有注释与词条值）。唯一可能的影响是**时序扰动**（改了热路径上 `Loc.T` 的调用密度/耗时 ⇒ 只能改变这场竞态的**赔率**，改变不了结果语义）。
（`Net/NetSelfTest.cs` +59 是另一条自检的夹具，不参与本进程；`Editor/NetBattleTest.cs` 本会话**没被改**，`git status` 里没有它。）

---

## ④ 是不是抖动：是（有据），但**「27 秒 vs 23 秒」这条线索要如实否掉**

**这一段前后的上下文**（`:5300-5440`）：
```
:5303 ✓（对面回来）客机自动重连、两边回到对局（主机 InBattle / 客机 WaitingReconnect）   ← 注意括号里客机仍是 WaitingReconnect
:5315 ✓ ★（对面回来）提示行改成「回来了」
:5327 [Net] Off：对面离开了这一局        ← NetSession.Close (:638) ← NetBattleTest.cs:645（= cs9.Close）
:5340 [Net] WaitingReconnect：对手掉线了，正在等他回来…（对局已暂停）   ← Pump (:209) ← Test:618
:5354 [Net] 对手掉线：**27 秒**倒计时开始（…第 2 次掉线）            ← HandlePeerLost (:326) → StartReconnectCountdown (:550)
:5369 [Net] 对手掉线了 —— 正在等他回来（27 秒后判他弃权）              ← HandlePeerLost (:328) → SayPeerGone (:428)
:5384 ✗ / :5409 ✗ / :5423 ✗
:5436 起：`hs9.Close(false)` → 下一段（OnDestroy 夹具）全部 ✓ 到收尾
```
- **这一块三条 ✗ 之外还有别的 ✗ 吗**：**没有**。`grep -c "^\[NetBattle\]  ✗"` = **3**，与「182 通过 / 3 失败」（`:10852`）一致；总数 185 = 基线 185 ⇒ **测试没变、数量没变**。
- **它依赖时序吗**：**依赖，但不是依赖秒数**。失败发生在 `:5246`（主机 `Send(resume)`，`case NetKind.Reconnect` → `NetSession.cs:492-505`）与 `:5327`（客机 `Close`）之间那一小段——测试在两者之间只做了 `Ok(...)` + `NetRuntime.DrainNoticesForTest()` + `hb9.LastSay = null`（`NetBattleTest.cs:638-645`），量级是**几十微秒**；而客机读线程 `ReadLoop`（后台线程）把 `resume` 从 socket 收队列搬到 `_inbox` 需要一次**线程唤醒**。这是一场**双方都在跑的竞态**。
- 🔴 **「27 秒 vs 只跑 23 秒」= 假线索（如实否掉）**：那个 27 是**算出来就打印的显示值** —— `BattleManager.enemyDisconnects` 那一档 `ReconnectSecondsFor(n) = max(15, 30 − 3×n)`，本次 `n = 2`（① 那次掉线已经计过一次，见 `:5022` 的「第 1 次」与 `:5354` 的「第 2 次」），**不是流逝时间**（`NetBattle.cs:530-551`）。测试从头到尾**没让倒计时走一秒** ⇒ 这条**不构成**「抖动」的证据，也不矛盾。
- **支持 ε 的硬证据**：① `资料/历史/交接_第六会话.md:49` —— **2026-10-09（今天）**全套跑过 `NetBattleTest 185/0`；② `资料/历史/A表已收口_1018第三会话.md:127,220` 与 `A表已收口_1018.md:28` 也都是 `NetBattleTest 185/0`；③ §9 这段夹具**自 `2cfb357`（2026-10-17）起没被改过**（`git show fe239e9` / `d6c4111` 的 hunk 落在别的行段：`~885-1129` / `~991`），而 `2cfb357` 的提交信息自称「全套 12 条 = 全绿 / 0 失败」。⇒ 同一套代码**先前绿过**。

---

## ⑤ 修法建议（最小改法；⛔ 我没有改）

### A. 夹具（**修这条红的最小、且有现成先例** —— 建议先做这个）
§9 ② 的判据只等**主机**：
```csharp
// NetBattleTest.cs:636
Ok(PumpUntil2(hs9, cs9, hNB9, cNB9, () => hs9.State == NetState.InBattle, 25000), ...)
```
而 §6 那一族等的是**客机真的追平**：`PumpUntil2(..., () => cb.Replayed, 20000)`（`:353` / `:380`，`BareHost.Replayed` 由 `NetReplayFromNet` 置真 —— `:92-93`）。
⇒ 最小改法 = §9 那条谓词改成
`() => hs9.State == NetState.InBattle && cb9.Replayed`（或最起码 `&& cs9.State == NetState.InBattle`）。
**为什么这样就好**：那时客机的 `Pump` 已经跑过、`resume` 已从 socket 收队列搬走（收队列空）⇒ `Close()` 是 graceful close ⇒ 刚写的 `bye` **必达**。
⚠️ 顺带：`:5303` 那条断言消息本身就把「客机 WaitingReconnect」印出来了（有人当时看见了但没当判据）—— 可以**顺手把它变成断言**（§6 的 `Eq`/`Ok(cb.Replayed, …)` 就是这个形状）。

### B. 产品侧（**要落盘成账**，铁律 5·b / 11；这一条不是这条红的成因，但是查出来的真问题）
- **现象**：`NetSession.Close(say:true)` 注释里承诺「对面会收到」；但对端此刻还有**未读入站数据**时，`TcpTransport.ClosePeer`（`NetTransport.cs:289-296`，直接 `_stream.Close()/_client.Close()`）在 Windows 上是**abortive close（RST）** ⇒ 几百微秒前刚写进发送缓冲的 `bye` **会被丢掉**，对面只看到「连接被重置」⇒ 走掉线那条链。
- **与原版的关系（口径要写清）**：原版**没有**「离开 vs 掉线」这个分档（`NetBattle.HandlePeerClosed` 的 doc 自己写着：两条走同一个 `OnPhotonPlayerDisconnected`；`bye` 是我们自己设计的协议消息）⇒ **丢掉 `bye` 的后果 = 退回原版行为**（弹「等他回来」+ 30/27 秒倒计时 → 到点判弃权）。所以这**不是「与原版不符」**，而是**与我们自己写下的那句承诺不符**（两条注释 + `NetBattleTest` §9 的期望）。
- **可选落点**（三选一，请调度台定口径；我不动代码）：
  1. **认「bye 是尽力而为」**：把 `Close` 的 doc 与 §9 的期望都写成「尽力」（夹具按 A 改、断言措辞改），并在 `Pump` 的 PeerLost 支**加一行日志**（见 C）。
  2. **让 close 变 graceful**：`ClosePeer()` 在 `Close()` 之前先把**接收队列排空**（`_client.Client.Available`/非阻塞 `Receive` 到空再关）。⚠️ 这是「关台前的清道夫」，**需要自己的判据**（且要说明它读掉的是**我们即将丢弃**的字节）。
  3. **不去赌时序**：收侧把「连接被重置」+「本机处于 `InBattle`」当**同一档**处理（即原版那一档），把 `bye` 只当**加分项**（有 `bye` 就说得更准）。这与 1 等价，只是落点写在收侧。
- **落点坐标**：`Net/NetSession.cs:619-641`（`Close`）· `Net/NetTransport.cs:289-296`（`ClosePeer`）· `Net/NetTransport.cs:344-399`（`Send`）· `Net/NetBattle.cs:341+`（`HandlePeerClosed` doc 里「原版没有这个分档」那一段）。

### C. 诊断性（顺带，强烈建议）
把 `_t.LastError` 在掉线那一跳**打出来**：`NetSession.cs:205-213` 目前在对局中**只印固定句** `St/PeerLostInBattle`，把 `why`（= `_t.LastError`，`St/PeerClosed`「对面关掉了连接」/ `St/ReadAbort`「读取中断：{0}」）**吞进了 `LastError` 字段、日志里一个字都没有** ⇒ 下一次再出这种事**照样判不了**（这次就是卡在这一步，见 ⑥）。

---

## ⑥ 判不了的 / 缺什么

1. **「主机那次 `Fail` 到底是 graceful 还是 reset」——判不了**。判别式就在 `TcpTransport._lastError`（`St/PeerClosed` vs `St/ReadAbort`，`Loc.cs:1583-1584`），但它在对局中的 PeerLost 支**不打印**（只进 `LastError` 字段）。全日志 grep「对面关掉了连接」/「读取中断」**零命中** ⇒ **需要 C 那一行日志，或插桩复跑一次**。本报告的「RST」是**推理**（下文将说明它为什么是唯一自洽的机制），**不是实测**。
2. **`resume` 当时到底在哪——判不了**。日志只能证明**客机没有 `Pump`**（缺 `[Net] InBattle：追上了 —— 重放这一局的 N 条动作`；同族夹具 `:2661/:3143/:6313/:6974` 都有），**不能**证明字节在**socket 收队列**里还是已被读线程搬进 `_inbox`。读线程不打印。⇒ 「abortive close」这条机制**依赖这个前提**（若是后者，close 就是 graceful、`bye` 必达，与观察到的现象矛盾）—— 所以它是**唯一与全部观察自洽的机制**，但仍是**未实测的推理**。
3. **能不能稳定复现——判不了**（我不跑 Unity）。建议**复跑 `NetBattleTest.Run` 1–2 次**：若绿的次数 ≥1 ⇒ ε 坐实；若**次次红** ⇒ 我的 δ 判断要推翻，改按 β（产品侧必须让 `bye` 可靠）处理。
4. **日志没有逐行时间戳** ⇒ 「`Send(resume)` 到 `Close()` 之间到底隔了多少毫秒」给不出；只能按代码量级（几十微秒）估。
5. **`bye` 有没有被主机的**内核**收到过**（收下了但被 RST 前的丢弃/被后面的 `Fail` 清掉）—— 需要抓包（Wireshark/loopback）才能定，本机没做。

---

## 附：一条**要如实否掉**的初始猜测

盘面给的线索里「总数仍是 185 ⇒ 是产品行为变了（或抖动）」与「27 秒 vs 23 秒对不上 ⇒ 可能是抖动」——
**前半截**成立（185 = 182+3，测试没变）；**后半截不成立**（27 是算出来的显示值，见 ④）。
而「大概是眩晕/失明那批 RuleEngine 改动带坏的」**更不成立**：那些类在这一跳里**一次都没被调用**（失败点在 socket 读线程 + `NetSession`，且用的是 `BareHost` 不是 `BattleDriver`）。
