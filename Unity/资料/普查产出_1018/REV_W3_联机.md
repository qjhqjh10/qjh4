# REV_W3 · 独立审查（`A914` + 「`Send()` 静默丢」 + `A961`）—— 2026-10-18

> 审查者 = `REV_W3`（**只读、只写本文件**；⛔ 没改任何工程文件 / 正本 · 没跑 Unity · 没动 git）。
> 审查对象 = 写手 `W3` 的工作区改动（交件报告 = `资料/普查产出_1018/W3_联机收尾交件.md`，303 行，18:22:52）。
> 本报告审的是下面这一版快照（⚠️ 审查期间作者仍在写，`NetSelfTest.cs` 的 md5 与我开始读时**不同**过一次 —— 见 §5）：

```text
260d77b8a96b16f1d9df8f2fec3ea44b  Net/NetBattle.cs      (994 行)
60d0a89d816d3dcad771e49beb60633a  Net/NetSession.cs     (598 行)
0d7168b59ca30d194e9c608a42365878  Net/NetTransport.cs   (402 行)
b676c95ca0cb227c0465e23a22f65a9f  Net/NetProtocol.cs    (421 行)
4fd0884825f810f87e54063d78c6ce50  Net/NetMatchmaking.cs (612 行)  ← 第 7 个文件（报告 §10 已追认）
c844bfc5b4bd4b6e4b6faaf06ae05b8f  Editor/NetSelfTest.cs (1388 行)
c7f8f89655a935bd6f6b2eda9d89d90b  Editor/NetBattleTest.cs (1371 行)
```

---

## 1. 审查结论（一句话）

**三笔改动本身（`A914` 的 `Close` · `Send` 出声 · 六处 `ClampPeerText`）逐条对着原版判据核过、都站得住；但新补的 20 条断言里，A914 的第 3 条是「重复」，报告里有一句「灭自证」声称不成立，另有 3 条低危疑点（A914 判据只覆盖了两端共用方法里的**一端** · 死条件 · `Close` 那句 `bye` 仍是静默丢）。⇒ 7 条问题：2 条真问题、5 条存疑（无一条会把这次同步点的自检搞红）。**

---

## 2. 逐条问题表

| # | 结论 | 证据（文件:行号 + 片段） | 该照哪个判据 |
|---|---|---|---|
| **R1** | **真问题 · 弱断言（净增鉴别力 = 0）** | `Editor/NetBattleTest.cs:919` `Ok(!hsC.WasInBattleForTest, "★（A914）…并且「掉线前他是我对手」那个标记也清了 ⇒ 客机那条**每 2 秒重连**的退避循环就此止住 … 上面两条钉不到它")`。① `hsC` 是**主机**会话（`:826 NetBattle.Attach(hbC, hsC, isHost: true)`），而那条退避支的判据是 `Role == NetRole.Client`（`Net/NetSession.cs:305`）⇒ **它钉的不是那个循环**；② 会话内 `_wasInBattle` 只在 `Close`(`:546`)/`EnterBattle`(`:531`)/两条 Resume(`:455`/`:465`) 被清 ⇒ 这条路径上任何把 `State` 移出 `WaitingReconnect` 的实现都必经 `Close` ⇒ **它与 `:913` 不可能分离**（`:913` 红它才红）。 | 作者自陈：CLAUDE.md §三「断言自证/同义反复 · 弱断言分不出两种状态」。**修法**：要钉「客机每 2 秒重连」，应在 `csC`（Role=Client）上让客机自己的倒计时到点，断 `!csC.WasInBattleForTest`，并配一条正例（`_wasInBattle` 为真 + `WaitingReconnect` 时 `Connect` 真被调过）——「删掉实现必红」在本条**成立**，但「上面两条钉不到它」**不成立** |
| **R2** | **真问题 · 「灭自证」声称不成立** | 报告 `W3_联机收尾交件.md:254`（同文 `Editor/NetSelfTest.cs:180` 注释）：「删掉 `NetSession.cs:368` 的 ⇒ **两条一起红**」。**不成立**：`Q⑯` 读的 `FoeName` 不经过 `PeerName` —— 客机侧 `FoeName = ClampPeerText(st.myName)`（`Net/NetMatchmaking.cs:487`），而 `st.myName = _myName = ProfileData.PlayerName`（`NetMatchmaking.cs:371` → `:559 myName = _myName`）；`PeerName` 是 `MsgProof.name`（`Net/NetSession.cs:375`）。⇒ 删 `NetSession.cs:375` 只会让 **`Q⑮` 红**，`Q⑯` 仍绿 | 同一句的另一半「删 `NetMatchmaking.cs:487` ⇒ 只有 `Q⑯` 红」✅ 成立（我逐跳核过）。⛔ 别照这句去判两条断言的相关性 |
| **R3** | **存疑 · 判据只覆盖了两端共用方法的【一端】**（需主对话裁） | `Net/NetBattle.cs:641-642` `if (_s != null && _s.State != NetState.Off) _s.Close(true, "对手掉线判负 —— 离开这一局的联机房间");` 落在 `ReconnectCountdownExpired` —— 这个方法**两端共用**（谁观察到对面掉线谁就跑）。所引判据 `…_d__322__MoveNext.c:142-144` 是「**判对面**弃权」那一端。另一端的原版对应物 = `d:/2/tools/decomp_full/BattleManager__FailedToReconnectAfterDisconnect.c:14 StopCoroutine(AttemptReconnect)` + `:26-27 AddResignAction(param_1,1)` + `DeadHero(param_1,1,3)` —— **全函数没有 `LeaveBattleRoom` 这一跳**（现读确认），而且它判的是**自己**负 | 原版：两端两条路**行为不同**（一端离开房间 + 判对面负；另一端停重连 + 判自己负）。我们这一跳在「本机是断线那一端」时**多做了原版没有的『离开房间』**，而修复动机里那条「客机每 2 秒重连」**正是这一端**（原版停那循环靠 `StopCoroutine`）。⇒ 建议裁：是否把 `Close` 限在「对面掉线判负」那一端，另一端只停重连 |
| **R4** | **存疑 · 低（死条件 + 静默 vs 出声）** | `Net/NetBattle.cs:641` 的 `_s.State != NetState.Off` 走不到假：上面 `:571` 那道闸对 `Closed/Off` 已经 `return` ⇒ 到 `:641` 时 `_s != null` 必有 `State ∉ {Closed, Off}`。且同族原版那道「已经离开过就不再走」的闸是**出声**的（`BattleManager__LeaveBattle.c:40 CustomDebug.LogWarning`，见 `Battle/BattleDriver.cs:5447-5451` 自己的注释），我们这一句静默 | 无害，但注释把位置说成「硬的（必须排在闸之后）」时没提这半句是死条件（`A914` 判据 `…_d__322:146-161` 只管闸，不管这句） |
| **R5** | **存疑 · 低（残留静默，作者未记）** | `Net/NetSession.cs:538` `if (say && _t != null && _t.IsConnected && State != NetState.Off) Send(NetKind.Bye, …);` —— 条件不成立时**一声不响**；而 `Send` 的新警告（`:514-522`）**永远走不到**这一步。⇒ 本账「丢包一定出声」在**我们最需要对面收到的那一句话**上不成立（作者已记 `NetTransport` 两处半静默，这一处没记） | 红线「不许静默失败」。作者自己在 `NetSelfTest` §Q①-③ 里把「对面收到的 `bye`」当成了被保护的消费方 ⇒ 这条与它自己的口径不一致 |
| **R6** | **存疑 · 低（语句顺序与原版相反，未标）** | 原版：`…_d__322__MoveNext.c:142 BattleNetworkManager__LeaveBattleRoom(mgr,0)` → `:143 AddResignAction` → `:144 DeadHero(对面,3)`（现读该 `.c` 三行确认）。我们：`Net/NetBattle.cs:603 _d.NetRemoteResign()` → `:642 _s.Close(...)` —— **反的**。注释（`:608-614`）照抄了原版顺序，没写「实现顺序与之相反」 | 在我们这套里对端只看得见 `bye` ⇒ 不可观测；但注释与实现不一致（铁律 3 要求把「我们挑的」标出来） |
| **R7** | **存疑 · 低（文案会给对面看，且视角是反的）** | `Net/NetBattle.cs:642` 的 reason 走 `MsgBye.reason` → 对端 `NetSession.cs:477-481` → `NetBattle.HandlePeerClosed` → `SayPeerGone("联机对局结束：" + body …)`（`NetBattle.cs:358-362`，提示行 + 弹窗）；同一刻对端 `Ctx.IsOver == false` ⇒ `ForfeitPeerIfLeftMidGame()`（`:375-394`）**反过来判我们弃权**（对端弹「判他弃权，你赢了」）。⇒ 那句「对手掉线判负」显示在**对面**界面上（对面看到的是「对手掉线判负」= 描述它自己），读起来是反的 | 作者已在报告 §8·5 记了「两端各判自己赢」的时序品种（未跑到实况）；这条是同一格里**文案**那一半。字数 19 < 40 ⇒ 钳位不影响 |

**✅ 核过没问题的（不是「总体不错」，是逐条核过）**：
`ClampPeerText` 六个入口（`NetSession.cs:375`/`405`/`478` · `NetBattle.cs:118-119`/`:898` · `NetMatchmaking.cs:487`）与 `NetProtocol.cs:222 MaxPeerTextChars=40` 的注释都**如实标着「我们自拟、原版没有先例」**（判据 = 原版无对端文本进界面先例 + 玩家名约束在 PlayFab 服务端）✅；`MaxPeerTextChars` 是 `40` 这个数的**出处写在注释里**（`SearchingMatchPopup.cs:408` 的 `HintLineMaxChars`）× 数值照抄不引 `Shell/` ✅；`_lastAccepted` **不挪** + 防再犯头注释（`NetSession.cs:232-238`）✅；`NetSelfTest` 的夹具**把进程级静态全还了回去**（`ProfileData.PlayerName` / `NetPendingBattle.Current` / `NetMatchmaking.Reset` / `rt.AttachForTest(keep)`，`:1290-1300`）✅；`GoToBattle` 在批处理下先 `return`（`NetMatchmaking.cs:576`）⇒ 不会撞 `HoldForPresentation` ✅；7 个文件**行尾全 LF、无整篇翻转**（我自己数的：`CRLF=0`，改动数远小于行数）✅。

---

## 3. 三条必查 —— 逐条结论 + 我查了哪些面

### ① 断言鉴别力
- **A914 三条（`NetBattleTest.cs:902-925`）**：求值**确实在 `hNBC.Tick()`（`:890`）之后** ✅（R3 点名的那条 `:886` 在 `Tick()` 之前 —— 作者标注属实）。
  - `:913 State != WaitingReconnect`：删实现 ⇒ 红 ✅（无实现时停在 `WaitingReconnect`，`Tick()` 之后没有任何东西再动 `hsC.State`）。
  - `:916 StatusText.Contains("离开这一局的联机房间")`：删实现 ⇒ 红 ✅。⚠️ 它与实现**共用同一个字面量** ⇒ 改措辞会假红（脆弱，不是假绿）——「换别的 `Close` 调用会红」这个说法要成立，前提是别处 `Close` 传的是别的 reason ✅ 成立。
  - `:919 !WasInBattleForTest`：删实现 ⇒ 红 ✅（我逐跳推过：`:843` 掉线 → `NetSession.cs:207 _wasInBattle = State == InBattle` = 真 ⇒ 中间 `:455` 那条 Resume 会清、`:874` 第二次掉线又置真 ⇒ 到 `:890` 时仍是真）；但**独立性不成立**（见 **R1**）。
  - **「两边一起改回去会不会全绿」**：这一族**没有**这种口子。我按最能构造的三种都试了 —— (a) `WasInBattleForTest` 是新增读口、只读实现写的那个字段，但没有「期望值从实现算出来」这回事；(b) `StatusText` 那条是两个独立字面量；(c) 断言里的数值上限（`MaxPeerTextChars+1/+20`）确实与实现共用常量（`NetProtocol.cs:222`）—— **单看上限是同源**，但每条都另有 `TAIL-MARK` 尾巴判法（`huge` = 200×`A` + `TAIL-MARK`，钳到 40 ⇒ 尾巴必不在）⇒ 上限只是附带条件，**扛鉴别力的是尾巴** ✅ 这点作者做对了。
- **`A961`/`Send` 那批（`NetSelfTest` §Q，`:1048-1310`）**：`TAIL-MARK` **能**区分「钳位删掉」✅ —— 判的是**模型里的字符串**（`StatusText`/`PeerName`/`OnClosed` 入参/`NetSay`/`Notice` 队列），不经过任何渲染层截断（Label 的 AutoFit / 弹窗换行都碰不到）⇒ 删钳 ⇒ 尾巴裸露 ⇒ 红。**同义反复**：逐条看过，**没有一条**期望值是从被测实现算出来的；最接近的两条是 `Q①/③/④/⑥/⑬/⑭` 的**数值上限**（与实现共用常量，见上）与 `Q⑪` 的 `+20` 余量（前缀 7+10=17 + 41 = 58 ≤ 60 ✅）。
  - `Q⑧/⑨/⑩`（`:1112-1124`）：`!sent` ✅ 真判返回值；`caught.Count > 0` 较弱（任何一条日志都能满足）但 `Q⑩` 用「没发出去」把它收紧 ✅；抓日志流的做法**恰是 R3 要求的「悄无声息时必红」** ✅。
  - `Q⑤` 我特意验过它**不是**靠「什么都没发生」：`MsgProof` 的 `protoVer` 与 `Version` 相等 ⇒ 不走拒绝支 ⇒ 走 `NetSession.cs:391 SetState(Lobby, "「{PeerName}」进来了…")` ⇒ `StatusText` **真的带** `PeerName` ⇒ 去掉钳位就会红 ✅（这条我一开始怀疑是假绿，核完是真判）。
  - `Q⑮/Q⑯`（`:1183-1260`）：走**整条大厅真路**（`PumpLobby`）✅；`Q⑯` 删 `NetMatchmaking.cs:487` ⇒ 红 ✅（`FoeName` = 209 字 > 41）；补的夹具前提 `Ok(NetMatchmaking.HasOpponent)`（`:1211`）**必要且到位** ✅（没它 `Q⑯` 可能因为没走到那一行而假绿 —— 这正是「靠什么都没发生的假断言」的正确防法）。
  - ⚠️ **20 条新断言一条都没被执行过**（铁律 12 攒批，作者如实记了：§10·5·4）。我**静态**把可推的期望值都推了一遍（上面逐条），`Q①-Q⑭` + `Q⑮/Q⑯` + A914 三条**没有发现假红源**。**唯一验证不了的一格** = `Application.logMessageReceived` 在 batchmode 下对 `Debug.LogWarning` 是否同步回调（`Q⑨/Q⑩` 靠它）；本仓 `Editor/BattleScene.cs` 有 8 处同款用法 ⇒ 大概率没事，但**我跑不了 Unity**，如实记着。

### ② 有没有引入新的静默失败
- **`Send` `void`→`bool` 的全部调用点**（我 grep 了 `Net/` 与整个 `Assets/`）：`NetSession.cs:265/336/346/376/419/434/442/449/539` · `NetMatchmaking.cs:72/384/570` · `NetBattle.cs:774`（包装）+ 经包装的 11 处 · 自检里的 `host.Send("test.bulk")`（`NetSelfTest.cs:190`，只记 kind 的桩）与 `raw.Send(...)`（`:167`，那是 `TcpTransport`、不是 `NetSession`）—— **只有 `NetSession.cs:250`（`Challenge`）接了返回值**。C# 允许丢弃返回值 ⇒ 不影响编译；**其余调用点靠 `Send` 内部那句 `LogWarning` 出声** ✅（与账上「只补出声」一致，**不是新静默** —— 是旧静默转成有声）。
  - 但 `NetBattle.cs:774 _s.Send(kind, msg);` 把返回值丢了：账上那条「调用方可以据此重试」**实际只兑现了一处**（文档注释 `NetSession.cs:497-499` 说得比现状满）。⇒ 归 R-不列（低）：**不是缺陷**，是注释与现状的落差；真要更硬的一条是「掉了一个 `MsgStart`（`NetMatchmaking.cs:570`）玩家只有日志」。
- **`NetTransport.cs:127` 重复 `Listen` 无声忽略 —— 仍在** ✅（现读：`if (_listener != null) return;`）。
- **`NetTransport.Send` 的 `catch` 半静默 —— 仍在** ✅（现读 `:366 catch (Exception e) { Fail("发送失败：" + e.Message); }`，只写 `_lastError`）。作者两处都如实记了（§9续·4）✅。
- **新发现的一处残留（见 R5）**：`Close` 的 `bye` 在 `!_t.IsConnected` 时**根本没走到 `Send`** ⇒ 静默。这条作者没记。
- 反方向查过一遍：新加的 `LogWarning` **不会**污染别人的日志型断言 —— `Editor/BattleScene.cs` 那 8 处抓日志**全都带过滤标记**（`[WaitBanner]` / `[Label]` / `[Net]…` 特定串），不是「数所有 warning」✅；`Core/ClickLog.cs:86` 会把新警告收进日志面板，但**没有任何断言按条数比它** ✅。

### ③ 「我们自拟」有没有被写成「原版」
- `40` 这个数：**判据 = `Shell/SearchingMatchPopup.cs:408 HintLineMaxChars`（我们自己的常量）+ 一句「这是我们自拟的口径，不是复刻」**，写在 `NetProtocol.cs:222` 的注释里 ✅；`NetSession.ClampPeerText`（`:86-90`）把口径指回那条注释 ✅；`NetBattle.cs:898`（`Reject`）与 `NetMatchmaking.cs:484` 各自**再声明一次「这是我们自拟的」** ✅。
- **唯一一处没就地声明**：`NetBattle.cs:110-119`（`FromStart` 的 `MyName`/`FoeName`）只写了「与另外三个入口同一处口径」，**没重复「原版无先例」** —— 链条还在（→ `ClampPeerText` → `MaxPeerTextChars`），**不算违规**，但与本笔其它四处的写法不一致（低）。
- 反方向（把我们的选择说成原版）我逐句扫过 A914 那段长注释：`LeaveBattleRoom.c:41-44`（`param_2 == false ⇒ RemoveRoomAfterLeaving`）+ `:46-49 LeaveRoom()` + `:26 Room.EmptyRoomTtl = 0` —— **我逐条现读反编译核过 ✅ 全对**；`…_d__322:146-161` 那道「status != 0x46 就什么都不做 + LogError」也**现读属实** ✅；`:143 AddResignAction` + `:144 DeadHero(对面,3)` ✅。⇒ **A914 的原版判据引用无一处夸大**（只有 R6 那个顺序问题）。

---

## 4. 作者自陈「删掉 X ⇒ 必红」逐条核验

| 自陈（出处） | 判 | 为什么 |
|---|---|---|
| A914 三条：「删掉 `ReconnectCountdownExpired` 末尾那句 `_s.Close(...)` ⇒ 下面三条全红」（`NetBattleTest.cs:909-910`） | ✅ **成立** | 三条都读 `Tick()` 之后的状态；无实现 ⇒ `State=WaitingReconnect`（`:913` 红）· 无那句 reason（`:916` 红）· `_wasInBattle` 仍真（`:919` 红） |
| A914 第 3 条：「只改状态字、不清 `_wasInBattle`（例如只调 `SetState`）⇒ 红」+「上面两条钉不到它」 | ⚠️ **前半成立、后半不成立** | `SetState` 是 `NetSession` 的私有方法，外部改不了；这条路径上「把 `State` 移出 `WaitingReconnect`」的实现**必然**经 `Close`，而 `Close` 一定清 `_wasInBattle`（`:546`）⇒ 与该条并不同源（见 **R1**） |
| `Q②/④/⑥`：删掉各入口的 `ClampPeerText` ⇒ 红 | ✅ **成立** | 三条都判尾巴标记，删钳后尾巴裸露（`Q①/⑤/⑦` 是伴生） |
| `Q⑬/Q⑭`：删掉 `FromStart` 那两次 ⇒ 红 | ✅ **成立** | `MyName`/`FoeName` 都由它写（`NetBattle.cs:118-119`） |
| `Q⑪/Q⑫`：删掉 `NetBattle.cs:898` 那句 ⇒ 红 | ✅ **成立** | `Abort` 的 `NetSay` 与 `Notice` 都吃那句钳过后的串 |
| `Q⑮/Q⑯`：删 `NetMatchmaking.cs:487` ⇒ **只有 `Q⑯` 红** | ✅ **成立** | `Q⑮` 的来源是 `s.PeerName`（另一位） |
| `Q⑮/Q⑯`：删 `NetSession.cs:368`（今 `:375`）⇒ **两条一起红** | ❌ **不成立** | `Q⑯` 的数据是 `st.myName = _myName = ProfileData.PlayerName`，**不经过 `PeerName`** ⇒ 只有 `Q⑮` 红（见 **R2**）。这句在报告 `:254` 与 `NetSelfTest.cs:180` 两处都写着，要一起改 |
| `Q⑨`：删掉 `Send` 那句 `LogWarning`（或守卫改回裸 `return`）⇒ 红 | ✅ **成立**（唯一保留 = `Application.logMessageReceived` 在批处理下确实回调，我验不了；本仓有 8 处同款先例） | 那一窗口内没有别的日志源 |
| 报告 §10·3 自陈「`:371 _myName` 不该钳（钳它反而改掉玩家自己的名字）」 | ✅ **成立且判得对** | 与 `:487`（对端发来的）区分正确 |

---

## 5. 没查清的部分

1. **`Application.logMessageReceived` 在 batchmode 下的时序**（`Q⑨/Q⑩` 唯一依赖）—— **没跑 Unity ⇒ 验不了**（红线）。本仓 `Editor/BattleScene.cs` 有 8 处同款、都绿过 ⇒ 大概率没事，但这是本批**唯一**「静态推不出」的一格。
2. **作者自称的 `typecheck` 0/0 我没复跑**（红线：⛔ 不许跑）。我改为**逐个 API 静态核对**：`INetBattleHost` 五个成员 vs `SayHost` 五个实现 ✅ · `AttemptAttach(INetBattleHost, NetSession, bool)` ✅ · `NetPendingBattle.FromStart(MsgStart, bool)` ✅ · `NetRuntime.AttachForTest` / public 字段 `LobbyHandled` ✅ · `DrainNoticesForTest()` 返 `string[]` ✅ · `NetMatchmaking.TryStart/HasOpponent/PumpLobby/Reset` ✅ · `ScriptedTransport.Connected` 是可写**字段** ✅ · `Ok/Eq` 重载 ✅。
3. **A914 那个时序品种（对面连得上、只是还没报进度）** —— 作者 §8·5 说没跑到实况；我**只读**了代码链（R3/R7），**没跑**，也**没**确认两端最终谁把胜负落盘（两端各判自己赢的**比分/结果**在联机里是两套本地状态，我们没有权威仲裁 ⇒ 我只证到「对端会立刻判我们弃权」这一跳）。
4. **`Battle/BattleDriver.cs:5417-5421` 那句过期注释** —— 我现核确认**确实过期**（它写着「`OnClosed` 全仓零接线 … `OnPeerLost` 也零接线」，而 `Net/NetBattle.cs:256-257` 已经 `+=` 两条）✅ 与作者 §8·2 一致；**不在本笔白名单，仍挂着**（转 W2 或下一波）。
5. **`A913`（理由码）/`A915`（掉线禁操作）与 A914 的耦合** —— 本笔没动（作者如实记 §8·7）⇒ **不在本次审查范围**，但 `A914` 的 `Close` 会让 `BattleDriver.LeaveNetRoom()`（`BattleDriver.cs:5476 if (s.State == NetState.Off) return;`）在「判负之后玩家再离开战场」时变成空转 —— 表现层收尾那半仍欠一盘账（**没跑到实况，只读代码推的**）。
6. ⚠️ **快照漂移**：审查期间 `NetSelfTest.cs` 的 md5 变过一次（`bd934e…` → `c844bfc5…`，作者在同步补 §Q⑥）。本报告结论以 §0 那组 md5 为准；**同步点若发现文件又变了，R1/R2 要按新内容复核一遍**（这两条是最可能被顺手改掉的）。
