# F5 · `NetBattleTest` 第 ③ 步三条红 —— 夹具同步弱点（(δ) 成立）

> 执行代理 F5（第十一会话）。判据输入 = `资料/普查产出_第十一会话/D1_九条红诊断.md` #7/#8/#9 + X2。
> **本件改了一个文件**（白名单内）：`Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs`。
> **没跑 Unity**、**没动 git**、**没碰任何别的文件**、没写两张正本。

---

## 一、一句话结论 · 以及**我是怎么证的**

**是夹具的同步弱点（`D1` 判的 (δ) 成立，且 `NetBattleTest.cs` / `Net/` 本会话零改动这一条我复核过）。**
第 ② 步的等待条件只查**【主机】**一台 ⇒ 主机的 `InBattle` 与它发出的 `Resume` 是**同一次 `Pump`**
（`NetSession.cs:487-494`）⇒ 客机还没读到 `Resume`、`cond()` 就已为真、循环当轮退出 ⇒
第 ③ 步在「**客机仍 `WaitingReconnect`**」这个**没验证过的状态**下关会话。

**证法 = 5 跑 5 对的对照（同一份源码、同一台机器）：**

| 跑次 | 日志 | 第 ② 步那行印的 | 第 ③ 步三条 ★ |
|---|---|---|---|
| 2026-10-10 **全套 12 条** | `_tmp_view/netbattle.log:5303` | **主机 InBattle / 客机 WaitingReconnect** | **3 红** |
| 2026-10-09 净跑 | `netbattle_rerun1.log:5734` | 主机 InBattle / 客机 InBattle | 3 绿 |
| 2026-10-09 净跑 | `netbattle_rerun2.log:5411` | 主机 InBattle / 客机 InBattle | 3 绿 |
| 2026-10-08 净跑 | `netbattle2_1020.log:5276` | 主机 InBattle / 客机 InBattle | 3 绿 |
| 更早 净跑 | `netbattle2.log:5229` | 主机 InBattle / 客机 InBattle | 3 绿 |

⇒ **「客机有没有追上」与「第 ③ 步三条红不红」完全同相**（5/5），而第 ② 步那行代码的**文案本来就写着「两边回到对局」**
（`NetBattleTest.cs:637`，改前）——**文案说两边、判据只有一边**，这正是 `D1` 判 (δ) 的形状。

**非回归（复核过 `D1` 的 git 论据，成立）**：
`git log --oneline 2324f18..81941b5 -- Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs` = **空**；
`git log --oneline 2324f18..81941b5 -- Unity/MyGame/Assets/CardPresentation/Net/` = **空**；
`git diff --numstat d6c4111 HEAD -- …/Editor/NetBattleTest.cs` = **空** ⇒ 该文件自 **d6c4111（2026-10-09 00:39）**
起一个字没动，**上面 5 次跑的是同一份夹具源码**。

⚠️ **但有一样不背书**：`D1` 给机制 (i) 的理由（「客机接收缓冲里还压着没被读线程取走的 `Resume`」）**站不住**，
见 §三·C4。我**照它的判法执行**（先修夹具、单跑一次看是否复现），**但理由要另找证据**。

---

## 二、逐处改动（`Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs`，共 2 处）

### 改动 ① `:636-660`（原 `:635-637`）—— 第 ② 步：只查主机 → **两边都查** + 超时出声

**改前**

```csharp
// ---- ② 对面**回来了** ⇒ 提示行说一句（原版那一刻是 `CloseAllWindows`）----
Ok(PumpUntil2(hs9, cs9, hNB9, cNB9, () => hs9.State == NetState.InBattle, 25000),
   $"（对面回来）客机自动重连、两边回到对局（主机 {hs9.State} / 客机 {cs9.State}）");
```

**改后**

```csharp
bool bothBack9 = PumpUntil2(hs9, cs9, hNB9, cNB9,
    () => hs9.State == NetState.InBattle && cs9.State == NetState.InBattle, 25000);
if (!bothBack9)
    Debug.LogWarning($"{P} ⚠️（对面回来）**等两边都回到对局·超时了**（主机 {hs9.State} / 客机 {cs9.State}）…");
Ok(bothBack9, $"（对面回来）客机自动重连、**两边**回到对局（主机 {hs9.State} / 客机 {cs9.State}） —— 🧨 改坏法：…");
```
（改后中间另有 12 行注释，写明根因链、5 跑对照、以及「⛔ 别改回只查主机」；`Ok` 的文案里补了改坏法。）

**为什么**
- **根因**：`PumpUntil2` 的循环体是 `hs.Pump(); cs.Pump(); hNB.Tick(); cNB.Tick(); if (cond()) return true;`
  （本文件 `:1423`，本次未改）。主机在**同一次 `Pump`** 里干完两件事：`Send(NetKind.Resume)`（`NetSession.cs:488`）
  与 `SetState(InBattle)`（`:493`）。⇒ `cond()` 判的是 `hs9`，主机一转 `InBattle` 就**当轮退出**，
  客机那一侧**还没来得及 `Pump()` 去消化 `Resume`**。
- **为什么必须两个都查**：第 ③ 步（`:669` 起）的三条 ★ 判的是「**对面的 `bye` 到了主机**」，
  它们的前提是「两边都在对局中、收发都没积压」。客机没追上时这个前提不成立 ⇒ 那三条红是**假的**。
- **超时出声（⛔ 不许静默）**：`PumpUntil2` 超时会返回 `false`（`Ok` 会记一条 `✗`），但**光有那条 ✗ 说不清因果** ——
  我补的 `Debug.LogWarning` 明确写出「**先看这一条，再看第 ③ 步的红，别去修传输层**」，
  免得下一个会话照着第 ③ 步的三条红跑去改 `NetSession.Close`（那正是本件最想避免的误导）。
- **为什么保留 25000 ms**：这是「客机重连退避 2000 ms + 握手 + 灌动作流」的整条链，
  4 次净跑都在这个窗口里跑完；不动它。
- ⛔ **没有**为了变绿把第 ③ 步的三条 ★ 改松 —— 那三条守的是**真需求**
  （对面点了退出、我这边必须收到通知，否则只能干等 10 秒心跳超时；`D1` §#7 ⑤ 也这么说）。

### 改动 ② `:666-673`（**插在原有 `:665` 那行注释之后**）—— 第 ③ 步：**只加注释**（`pumpOne` 代码一字未动；`NetRuntime.DrainNoticesForTest()` 现在在 `:675`）

新增 8 行注释，结论是 **`pumpOne`（只推主机）是对的，⛔ 别改成推两台**：

1. **不需要**：改动 ① 现在保证**客机也 `InBattle`**（它的 `Resume` 已被自己 `Pump()` 消化、
   收发两条都不再积压）⇒ `cs9.Close(true, …)` 把 `bye` 写出去之后，**客机这一侧没有任何事要做**
   （它在 `Close` 里已经 `Off`）。
2. **有反作用**（这条是新查出来的，也是**唯一**能推翻「顺手改成推两台」的理由）：
   `NetSession.Pump()` 里那道「掉线检测」闸（`NetSession.cs:205`）写的是
   `if (_t.PeerLost && State != NetState.Closed && State != NetState.WaitingReconnect)` ——
   **它只挡 `Closed` / `WaitingReconnect`、不挡 `Off`** ⇒ 客机若被推起来、而它这一侧 `_t.PeerLost` 为真，
   就会**再报一次** `OnPeerLost` ⇒ 下面 `n9c.Length == 1` 那条变成 2、**假红**（与本节要验的东西无关）。
3. **判据只跟主机有关**：`bye` 到没到主机、主机弹了几条、主机提示行说了什么。

⛔ 顺带说明：第 ① 步（`:623` 掉线那半）与第 ④ 步（`:696`/`:704` `OnDestroy` 那半）也用 `pumpOne`，
**本次没动**，理由见各自已有的注释（同一条「别让对面也报一次」）。

---

## 三、🔑 X2 —— `bye` 丢在哪一跳（**查到哪一步、两个候选各自的证据/反证、还差什么**）

### A. 我**确证**的（都是从日志与源码实读出来的硬事实）

| # | 事实 | 出处 |
|---|---|---|
| E1 | 客机**确实执行了** `Close(true, "对面离开了这一局")` | `netbattle.log:5327` `[Net] Off：对面离开了这一局`，栈 `NetSession.Close (:638)` ← `NetBattleTest.cs:645` |
| E2 | 客机**确实把 `bye` 写进传输层了**：两条「发不出去」告警**全日志零命中** —— (a) `Close` 的空档告警（`NetSession.cs:624-627`）、(b) `Send` 的「传输层报失败了」（`:582-587`）与 `TcpTransport.Send` 的「没发出去」（`NetTransport.cs:376-378`） | 我逐行解全日志（`10910` 行）搜过 `没发出去` / `传输层报失败了` / `本来要给对面捎一句` / `没有活的连接`；**只有** `:416`（另一段的一颗 ping）、`:4021`（`:469` 那一段）、`:7351`（`:932` 那一段）三处命中，**都不是本段** |
| E3 | 主机**从头到尾没有处理过任何 `Bye` 帧**：`case NetKind.Bye`（`NetSession.cs:514-531`）一定会走 `SetState(Closed, …)`，而 `SetState` **每次必打日志**（`:685`）⇒ 那一段日志里没有 `Closed：` | `netbattle.log:5340` 是 `WaitingReconnect：对手掉线了，正在等他回来…`，栈 `Pump (:209)` = **`_t.PeerLost` 那一支** |
| E4 | 主机的 `_t.PeerLost` 在断言那一刻（`:5303`）**还是 false**（那行印的是 `主机 InBattle`）；`PeerLost` 变真只能来自 `TcpTransport.Fail`（读线程 `n<=0` / 读异常 / `Send` 的 catch） | `NetTransport.cs:337-342` · `:306-316` · `:393` |
| E5 | 「客机追上了 ⇒ 三条绿；客机没追上 ⇒ 三条红」**5 跑 5 对** | §一那张表 |

⇒ **`bye` 确实「写成功了，但主机一个字节都没收到」**（E2 + E3 + E4）。丢点**在传输层或线缆上**，
**不在 `NetSession` 的收发逻辑里**（那两半 E2/E3 已经把它夹住了）。

### B. 🔴 **为什么我现在定不了** —— 缺的是一条日志

**`TcpTransport.Fail(why)` 是【静默】的**（`NetTransport.cs:337-342`）：它只
`if (_lastError == "") _lastError = why;` + `Publish(connected:false)` + `_peerLost = true`，
**一行日志都不打**。而 `_lastError` **全仓唯一**的消费点是 `NetSession.Send` 的前后对比（`:573-587`）
⇒ **「对面为什么掉了」这个原因永远不会出现在日志里**。

本次能确定 `Fail` 被调用过（E4），但**分不出**它走的是哪一支：

| `Fail` 的调用点 | 原因串 |
|---|---|
| `ReadLoop` `ReadFull(head)<=0` → `NetTransport.cs:308` | 「对面关了」（`St/PeerClosed`）——**优雅** FIN |
| `ReadLoop` `ReadFull(body)<=0` → `:316` | 同上 |
| `ReadLoop` catch（读异常）→ `:307` / `:315` | 「读中断：{msg}」——**abortive**，这里会带 `ECONNRESET` 之类的字样 |
| `Send` catch → `:393` | 「发送失败：{msg}」 |

**这就是「要证什么」**：**主机侧 `Fail` 的 `why`（以及它落在哪一支）**。
**怎么证（两条，都要动白名单外的文件 ⇒ 只报不动）**：
- **(a)** 给 `TcpTransport.Fail` 加一句 `Debug.LogWarning("[Net] 传输层掉线：" + why + " …")` ——
  **这也是生产侧该有的一刀**（现在玩家侧看到的状态字是 `St/PeerLostInBattle`，**不含原因**，
  而 `LastError` 没有任何界面读它 ⇒ 违反「不许静默失败」）。
- **(b)** 自检侧探针：在 `cs9.Close(true, …)` 前后各打一条
  「客机 `IsConnected` / `PeerLost` / `AcceptedCount` / 收到与发出的最后一条 `kind`」+
  「主机 `IsConnected` / `PeerLost` / `AcceptedCount`」。E2 只证明了**会话层**以为写成功，
  没证明**底下那一刻 socket 是活的**。

### C. 两个候选机制 —— 各自的证据 / 反证

**候选 (i)：客户端的关闭是 abortive close（RST）⇒ 主机收到 RST 时把刚到的 `bye` 一起丢掉了**
（Windows：收到 RST 会中止连接并丢弃**尚未被应用读走**的接收队列）
- **支持**：E3+E4 的形状（主机「读线程报掉线」而「`bye` 一字节都没进 `_inbox`」）**正是**这一形状；
  它与「客机没追上」还有一条机制上的联系 —— 客机没消化 `Resume` ⇒ 该帧**可能**还压在客机的接收缓冲里。
- 🔴 **反证（`D1` 没算到的一条）**：`ReadLoop`（`NetTransport.cs:299-322`）是**独立线程**，
  **一直在 `recv`**；`Resume` 一到就会被它取走并塞进 `_inbox`（`ConcurrentQueue`，与主线程 `Pump()` **无关**）。
  而从主机 `Send(Resume)`（`:5246`）到 `cs9.Close()`（`:5327`）之间隔着 2 条**带栈的** `Debug.Log`
  + 两次 `Ok` + 一次 `DrainNoticesForTest` ⇒ 一般有 **ms 量级**。
  ⇒ 「客机接收缓冲里还有未读数据」这个前提**站不住**（除非那台机器的读线程被调度延迟了 ms 级 ——
  全套跑比净跑负载重，**这是唯一还站得住的那一半**，但也只是「可能」）。
- **要什么才能定论**：只有 §B 的探针 (a)(b)。特别是：若 `why` 是「读中断：…连接被重置…」⇒ (i) 成立；
  若是「对面关了」（FIN 的正常收尾）⇒ **`bye` 是在 FIN 之前就被丢的**，那就**不是** (i)，
  而是「写成功但没发上线」，那是**另一档**（Windows 上 `closesocket` 那一刻**另一个线程正阻塞在
  `recv`** 会让拆除走 abortive 路径、挂起的发送数据可能被丢）—— 客机的读线程**正是**阻塞在 `recv` 上，
  这一档**对「两侧收发状态」没有依赖**，也就**不能**由夹具的同步弱点解释 ⇒ 若成立就必须按铁律 11 立账做。

**候选 (ii)：重连后两侧落在不同 socket 上**
- `D1` 已列、且**与日志对不上**（客机只发生过**一次**重连：`[Net] WaitingReconnect：正在重连主机…`
  全段只 1 条，`:5145`；2000 ms 退避没再触发）。**我复核过：`D1` 这一条成立，备选。**

**候选 (iii)：🆕 我新提的一条 —— `TcpTransport.Setup` **不停旧读线程**，旧读线程迟到的 `Fail` 把【新连接】标成掉线**
- 机制：`Setup`（`NetTransport.cs:263-285`）只 `old.Close()`，**不打断旧 `ReadLoop`**；旧读线程捕获的是
  **旧** `_stream`，旧 socket 一关它就 `Fail` ⇒ `Publish(connected:false)` + `_peerLost = true`
  打在**共享**状态字上。这一句若落在 `Setup` 的 `_peerLost = false` / `Publish(true)` **之后**，
  **活着的新连接**就被标成「掉了」。
- **本次只能排除一半**：若它落在两次 `Send` **之前**，主机那次 `Send` 会走 `TcpTransport.Send` 的
  空档告警（`:376`）—— **全日志零命中** ⇒ 至少在主机 `Send(Resume)`（`:5246`）那一刻连接位是真的。
  但它落在 **`Send(Resume)` 之后、`pumpOne` 之前那 1–5 ms 的窗口**排除不掉。
- ⚠️ **它解释不了 E3**（`Fail` 不碰 `_inbox`、不关新 socket ⇒ `bye` 照样该被读到）
  ⇒ **单独不成因**，但**是一条独立该记账的隐患**（`Fail` 静默，出问题时只能靠后续 `Send` 的空档告警间接发现）。

### D. 一句话给下一个会话
**先把夹具单跑一次**（§四）。**不复现 ⇒ (δ) 结案**（本次三条红是假红）。
**复现 ⇒ 才去查传输层**，而且**先补 §B 的那条日志（`Fail` 的原因）**，再谈「优雅收尾」。

---

## 四、该单跑哪一条 + 预期（**交给调度台跑**，⛔ 我没跑 Unity）

**单跑**：`NetBattleTest.Run`（12 条里的联机对局那条；产出落在 `_tmp_view/netbattle.log`）

```
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
  -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod NetBattleTest.Run -logFile -
```

**预期（照这个逐条对，别只看退出码）**
1. **末行**：`[NetBattle] ===== 通过 185 · 失败 0 =====`（基线 185 条断言、总数**未变** —— 我只改了条件与文案，
   **没有增删断言**）· 且**不应**出现 `===== 跳过/警告 N（…）=====` 这行（本段本来就只有 `OnDestroy` 那半会 `_warn++`；
   `Application.isPlaying == false` 时**照旧**会出现 1 条 —— **那一条是既有行为、不算新红**）。
2. **第 ② 步那行**（改后 `:657` 起的 `Ok`）应为
   `✓ （对面回来）客机自动重连、**两边**回到对局（主机 InBattle / 客机 InBattle）`。
3. **第 ③ 步三条**应为
   `✓ （对面离开）主机收到 bye ⇒ 会话关上（实际 Closed）` ·
   那条话里带「**离开**」（基线文案 = 「联机对局结束：对面离开了这一局 —— 判他弃权，这一局你赢了」）·
   提示行带「**结束**」。
4. **⛔ 不应出现**我新加的那条 `⚠️（对面回来）**等两边都回到对局·超时了**` —— 一出现就说明客机连 25 秒都没追上，
   那是**另一个**问题（那时第 ③ 步的三条红**不用看**）。

**判绿红的规矩**（`CLAUDE.md` §铁律 12）：**按【行首标记】数**，别 `grep -c ✗`；本条的合计格式是
`通过 N · 失败 M`（`:1341`），退出码也是按 `_fail == 0` 给的（`:1346`）。

⚠️ **前置条件（现在不满足，见 §六·3）**：`Assets/CardPresentation/Editor/BattleScene.cs(414)` 现在有
**2 条 CS0103**（别的代理正在改那个文件）⇒ **编辑器程序集编不过** ⇒ Unity 批处理会在编译阶段就失败、
**本条根本跑不起来**。⇒ **等那几个代理收工、编辑器程序集能编过再跑。**

---

## 五、验证（本件跑了什么）

| 项 | 读数 |
|---|---|
| **秒级类型检查**（`TMPDIR=/tmp/wf_f5 bash d:/4/Unity/工具/typecheck.sh`） | **运行时错误数 0**；编辑器程序集 **2 条**，**全在 `Assets/CardPresentation/Editor/BattleScene.cs(414)`**（`CS0103` 名字 `drv` 不存在）—— **不是我改的文件**（那个文件在禁碰名单里、正被别的代理写）；隔 25 秒在独立 `TMPDIR` 重跑一次，**仍是同样这 2 条、仍是同一个文件** ⇒ 按 `CLAUDE.md` §铁律 13·3·2 的判据，**这不是我的问题**。我改的 `NetBattleTest.cs` **零错**。 |
| **行尾** | 改前 `CRLF 0 / LF 1544`，改后 `CRLF 0 / LF 1576` ⇒ **仍是纯 LF、没翻**（`python -c "io.open(p,'rb')…"` 现量，不是记的） |
| **`git diff --numstat`** | `34  2  Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs` —— 与「+32 注释行 +3 代码行 / −2 行」吻合，**不是整篇重写** |
| **改了哪些文件** | **只有** `Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs`（白名单内那一个） |
| **动的工具 / 正本 / git** | 没动 git（无 `add`/`commit`/`checkout`）· 没写 `项目任务.md` / `CLAUDE.md` · 没跑 Unity · 没碰 `d:/2/**` |

---

## 六、没查清 / 停手的部分

1. **X2（`bye` 丢在哪一跳）** —— 见 §三 B/C。**没定论**，缺的是**一条日志**（`TcpTransport.Fail` 的原因）。
   ⛔ 我**没有**为了「让它绿」去动 `NetSession.Close` 的硬 `_t.Close()`（`NetSession.cs:634`）——
   按任务书 ④，那是传输层改动、**白名单外** ⇒ **只报不动**；§三 B 写清了「要证什么、怎么证」。
2. **候选 (iii)（`Setup` 不停旧读线程）** —— 新提的隐患，**本次排除不干净**，也**解释不了丢帧**。
   `NetTransport.cs` 白名单外 ⇒ **只报不动**，建议**独立立账**（不并进这次的三条红）。
3. **`_t.PeerLost` 的时刻** —— 我只能证明「`:5303` 那刻还是 false」与「`:5340` 已经 true」，
   **没法再窄**（`Fail` 静默）。要窄就必须加探针。
4. **没跑 Unity**（红线）⇒ 本次三条红**是否复现**、以及我的改动**是否真的修好**，
   **都还没验** —— 由调度台在 §四 单跑一次。**在那一跑之前，本件不主张三条红已修好。**
5. **没回原版核任何东西**（本件不涉及原版判据；三条 ★ 守的是我们自己的联机契约）。

---

## 七、🔑 顺手发现的（**都没动手**，交调度台分流）

1. 🔴 **`TcpTransport.Fail()` 是静的 —— 「为什么掉线」永远不会进日志**（`NetTransport.cs:337-342`）。
   它只写 `_lastError`，而 `_lastError` 全仓唯一消费点是 `NetSession.Send` 的前后对比
   ⇒ 生产侧玩家看到的状态字 `St/PeerLostInBattle` **不含原因**、`LastError` **没有任何界面读它**。
   **这次的 X2 就是卡在这里**。建议（不在白名单）：`Fail` 里加一句告警 + 让原因露面。
   > ⚠️ **2026-10-10（第十一会话 · `G2`）就地订正（铁律 5）**：上面那句「`_lastError` **全仓唯一消费点**是
   > `NetSession.Send`」**不准确** —— 现核 **`StartHost:132` / `InternalConnect:185` / 重连失败 `:320` 也读它**。
   > **结论不变**（**掉线那一条路上**它确实没人看）⇒ 建议照改，本条的**处置也没变**（`A1267` 已按它做完）。
2. 🔴 **`TcpTransport.Setup` 不停旧读线程**（`NetTransport.cs:263-285`）：旧 `ReadLoop` 的 `Fail`
   会 `Publish(connected:false)` + `_peerLost = true` 打在**共享**状态字上 ⇒ 迟到的那一句会把
   **活着的新连接**标成「掉了」。`AcceptLoop` 的「已有活对家」闸也用这个状态字（`:196`）。
   ⇒ 建议独立立账（这是**重连路径**上的隐患，本次第 ①/② 步正好走的就是这条路径）。
3. ⚠️ **`NetSession.Pump()` 的「掉线检测」闸不挡 `Off`**（`NetSession.cs:205`：
   `State != Closed && State != WaitingReconnect`）⇒ **`Close()` 之后**若这台会话再被 `Pump` 一次、
   而 `_t.PeerLost` 为真，它会**再报一次** `OnPeerLost`（状态从 `Off` 倒回 `WaitingReconnect`）。
   本次它就是我**不把客机也推起来**的理由（改动 ② 注释里写了）；生产路径上没有第二处 `Pump`，
   所以现在不现形，但这是一条「谁再 `Pump` 一次就现形」的边。
4. 📌 **`D1` 有一处理由不硬（结论没错，理由要换）**：机制 (i) 的「客机接收缓冲里还压着 `Resume`」站不住
   —— 读线程是独立线程、`Resume` 一到就被取走（见 §三 C·(i) 的反证）。
   建议 `D1` 那一行改成「客户机关闭那一刻**发送方向**有没有排空 / 是不是 abortive close」，
   并把「先补 `Fail` 的原因日志」列成前置。
5. 📌 **核实 `D1` 的 git 论据全部成立**（我逐条重跑过）：
   `2324f18..81941b5` 对 `Net/` 与 `Editor/NetBattleTest.cs` **都是空**；
   `NetBattleTest.cs` 自 `d6c4111`（2026-10-09 00:39）起**零改动**。
6. ⚠️ **`netbattle.log` 是【混编码】**：同一文件里 UTF-8 与 GBK **都有**，`utf-8` 与 `gbk` **单独解都失败**
   （`utf-8` 在 `pos 3261` 失败、`gbk` 在 `pos 21373` 失败）⇒ 读它必须**逐行 try 两种编码**，
   否则会看到一片乱码（我第一次就撞上了）。`文案表` 也说明这文件里两种都真实存在
   （`5290` 行前后是 UTF-8 的 `[Net]`，`5303` 行前后是 GBK）。
7. ⚠️ **本段的 `Ok` 文案与断言数**：我只改了**一个**条件的形态与两处文案，**断言总数仍是 185**
   （§四 的预期读数据此写的）。如果单跑出来不是 185，先看是不是**别的代理同时改了本文件**
   —— 本文件**只有我一个写手**（任务书白名单）。
