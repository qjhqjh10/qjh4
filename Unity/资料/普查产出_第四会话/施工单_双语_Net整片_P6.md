# 施工单 · 双语 · `Net/` 整片（P6 只读普查 · 2026-10-18）

> 判据：`施工单_双语③逐处换key.md` §③/§④ · `交件_波1_P3_社交与商店.md` §⑥ · 正本 `资料/联机P2P_设计与交接.md`。
> 代码正本 = `Assets/CardPresentation/Net/`（9 份 `.cs`，**逐份现读**）。行号 = **现读 `HEAD` 行号**。
> 🔴 「在不在表」= **我这一刻**读的 `Core/Loc.cs`（`EntryCount` **191**，波 0b 正在加键 ⇒ 会涨）。
> 判定四分：**① 玩家可见（要翻）** · **② 日志/诊断（⛔ 不翻）** · **③ 协议串/资源名/玩家数据（⛔ 不翻）** · **Ⓦ 走线文案（跨端上屏，⛔ 待裁决，见 ⑥·1）**。
>
> 🔴 **2026-10-09 现核订正（铁律 5 · `A1029` ③）**：
>
> - **P6a（`NetSession.cs` + `NetRuntime.cs`）已落地** ⇒ 本单**所有 `NetSession.cs` 行号都是执行【前】的读数**，
>   ⛔ **别再照抄** —— 一律按**符号名 / 原字面量**现读（⛔ 也别顺手把它们改成新行号：行号会再漂，制造新的死数字）。
> - **本表唯一就地改过的一行 = `St/Handshaking` 那一行**（原写 `:181` `:354`）⇒ 见 ② 表里那一行。
> - **现读两个落点**（按符号找，不按行号）：`Net/NetSession.cs` 的 `InternalConnect`（连上之后那一句）与
>   `Proof` 那一支（按 `NetKind.Proof` 找）—— 两处**已共用同一条键** `Loc.T("Settings/Online/St/Handshaking")`。

## ① 文件清单（无 `.asmdef` ⇒ 全部 `public`）

| 文件 | ① 处 | ②/③ | 玩家看见它的那条路（**现核**） |
|---|---|---|---|
| `NetRuntime.cs` | **3** | 6 | `WindowsManager.ShowPopUp(正文, 钮字)` ⇒ 弹窗（钮 = `知道了`） |
| `NetSession.cs` | **34** | 12 | `StatusText` → `Shell/SettingsWindow.cs:2587 RefreshOnline()` 那一行（每帧刷）· `LastError` → `:2457 _flash` |
| `NetMatchmaking.cs` | **19** | 24 | 提示行 = `OnHint` → `Shell/SearchingMatchPopup.ShowHint`（`:408 HintLineMaxChars = 40`！）· 弹窗 = `NetRuntime.Notice` |
| `NetTransport.cs` | **15** | 5 | `_lastError` → `NetSession.LastError` → 上面那条 `_flash` / `StatusText` |
| `UpnpPortMapper.cs` | **9** | 7 | `:125 NetRuntime.Notice(r.message)` ⇒ 弹窗 |
| `NetConfig.cs` | **2** | 5 | `r.detail` → `SettingsWindow.cs:2393 EchoText()` 末尾 |
| `NetBattle.cs` | 12 | ≈60 | 提示行 `NetSay` + 弹窗 `Notice` —— ⛔ **已有裁定「不接」**（`:578-650` 写明两条理由），见 ⑥·2 |
| `NetApply.cs` | **0** | 5 | 唯一消费者 `BattleDriver.cs:272 s => Debug.Log(...)` ⇒ ② |
| `NetProtocol.cs` | **0** | 2 | 全是 `Debug.LogWarning` + 协议常量（`:172-182 "hello.challenge"` 等 = ③） |

## ② 逐处表（键名 **自拟族** = `Settings/Online/*`，先例 = 表里那条 `Settings/Online/Title`，也是 `Loc.cs:187`）

| 文件:行号 | 原字串（首段，逐字见该行） | 判定 | 该用的键 | 在表 |
|---|---|---|---|---|
| `NetRuntime.cs:109` · `:138` | `知道了`（弹窗钮） | ① | **`MainMenu/General/OK`** | ✅ |
| `NetRuntime.cs:179` | `主机重开了`（`Close` 的 `bye` 理由） | Ⓦ | `Settings/Online/Wire/HostRestarted` | ❌ |
| `NetMatchmaking.cs:62` `:63` `:67` | `联机没连上（这一局本来就没走联机）` / `这一局还没进入联机匹配` / `这一局**已经开局了**…` | ① | `Settings/Online/Cancel/WhyNoLink` · `…/WhyNotMatching` · `…/WhyStarted` | ❌ |
| `NetMatchmaking.cs:197` `:212` | 提示行：`对面掉线了，{tail}（两边回来各点一次 Battle!）` / `联机结束：{body} —— {tail}` | ① | `Settings/Online/Lobby/PeerLostHint` · `…/PeerLeftHint` | ❌ |
| `NetMatchmaking.cs:198-200` `:213-215` | 弹窗：`对面掉线了 —— 联机断开。\n…` / `联机结束：…\n…` | ① | `…/PeerLost` · `…/PeerLeft`（**两段**，含原版那句出处说明） | ❌ |
| `NetMatchmaking.cs:249` | `这一局的匹配已经撤销` / `（本机本来就没在匹配这一局）` | ① | `…/MatchRevoked` · `…/NotMatchingThisGame` | ❌ |
| `NetMatchmaking.cs:233` | `{what} —— 已开局、正在进战场（后面由对局那一层说）` | ① | `…/DeferToBattle` | ❌ |
| `NetMatchmaking.cs:360` `:362` `:363` | `联机没连上` / ``联机会话现在是 `{0}`（还没握手完）`` / `这副牌是空的` | ① | `…/BotNoLink` · `…/BotSessionNotReady` · `…/BotEmptyDeck` | ❌ |
| `NetMatchmaking.cs:402-406` | `这一局**打的是电脑，不是联机**。\n原因：{0}。\n…`（整段） | ① | `…/PlayedVsBot`（3 行整段，`{0}` = 上面三条之一） | ❌ |
| `NetMatchmaking.cs:427` | `对面回来了 —— 联机已恢复。要开这一局，两边重新各点一次 Battle!` | ① | `…/LobbyRestored` | ❌ |
| `NetMatchmaking.cs:466-467` `:502-503` `:509-510` `:539-540` `:587` | 五条 `Notice`（迟到开局 / 迟到取消 / 对面取消 / 模式不一样 / 开局包解不出） | ① | `…/StartAfterCancel` · `…/MissedCancel` · `…/PeerCancelled` · `…/ModeMismatch` · `…/StartParseFailed` | ❌ |
| `NetSession.cs:102` `:573` | `未连接` | ① | `Settings/Online/St/Off` | ❌ |
| `NetSession.cs:133` | `主机没起来：{0}` | ① | `St/HostFailed` | ❌ |
| `NetSession.cs:143` | `主机已就绪，在 {0} 端口等客机（把本机 IP 告诉对方）` | ① | `St/Listening` | ❌ |
| `NetSession.cs:177` | `正在连 {0}:{1} …` | ① | `St/Connecting` | ❌ |
| `NetSession.cs` `:181` `:354` | `连上了，正在核对协议版本与密码…` / `正在核对协议版本与密码…` | ① | `St/Handshaking`（**一条键两处**） | ❌ ⚠️ **2026-10-09 现核订正**：后一处现读 **`:358`**（按 `NetKind.Proof` 那一支找，别按行号）；两处**已共用同一条键**（`Loc.T("Settings/Online/St/Handshaking")`）⇒ 落键后**两句都带上了「连上了，」那个前缀**，属**已落到实处的可见变化**（代码里已如实标注） |
| `NetSession.cs:210` `:294` | `对手掉线了，正在等他回来…（对局已暂停）` | ① | `St/PeerLostInBattle` | ❌ |
| `NetSession.cs:211` `:295` | `连接断了：{0}` | ① | `St/Disconnected` | ❌ |
| `NetSession.cs:256` | `有连接进来，正在核对是不是刚才那个人…` / `有客机连进来了，正在核对…` | ① | `St/PeerBack` · `St/PeerJoined` | ❌ |
| `NetSession.cs:289` | `{0} 秒没收到对面的任何消息` | ① | `St/SilentTimeout` | ❌ |
| `NetSession.cs:311` `:315` `:319` | `正在重连主机…` / `连上了，正在补上这一局的进度…` / `重连失败，稍后再试：{0}` | ① | `St/Reconnecting` · `St/CaughtUp` · `St/ReconnectFailed` | ❌ |
| `NetSession.cs:361` `:363` `:367` `:380` | `对面发来的握手包解不出来` / `两边版本不一样…` / `密码不对` / `拒绝了这次连接：{0}` | ① | `St/BadHello` · `St/VersionMismatch` · `St/WrongPassword` · `St/Refused` | ❌ |
| `NetSession.cs:387` `:391` | `「{0}」连回来了，正在等他报进度…` / `「{0}」进来了 —— 各自选好卡组就能开战` | ① | `St/PeerBackWaitReport` · `St/PeerInLobby` | ❌ |
| `NetSession.cs:405` `:478` `:420` `:424` `:454` `:464` | `对面拒绝了连接` / `对面退出了` / `已经连上主机，正在等他补这一局的进度…` / `连上主机了 —— 各自选好卡组就能开战` / `「{0}」回来了 —— 已把这一局的 {1} 条动作发过去` / `追上了 —— 重放这一局的 {0} 条动作` | ① | `St/PeerRefused` · `St/PeerLeft` · `St/ResumedWaitProgress` · `St/ClientLobby` · `St/ResumeSent` · `St/ResumeCaughtUp` | ❌ |
| `NetSession.cs:435` `:443` | `重连被拒：钥匙对不上` / `重连被拒：主机没有权威动作流` | ① | `St/RejectBadKey` · `St/RejectNoLog` | ❌ |
| `NetSession.cs:550` | `对局中` + `（本机是主机，动作由本机定序）`/`（客机：操作由主机确认）` | ① | `St/InBattle` · `St/HostSide` · `St/ClientSide` | ❌ |
| `NetSession.cs:434` `:442` `:461` `:567` | `这把钥匙对不上这一局` / `主机这边没有这一局的记录` / `重连包解不出来` / `对面结束了这一局` | Ⓦ | `Settings/Online/Wire/{BadKey,NoRecord,BadResume,PeerDone}` | ❌ |
| `NetTransport.cs:420` `:422` `:424` `:425` | `端口 {0} 已被占用 —— …` / `对面拒绝了连接（{0} 端口没人在听）—— …` / `这个 IP 地址找不到——检查一下有没有抄错` / `网络错误：{0}` | ① | `Settings/Online/St/{PortBusy,ConnRefused,HostNotFound,SocketError}` | ❌ |
| `NetTransport.cs:210` `:226` `:282` | `没有填 IP 地址` / `连接 {0}:{1} 超时（{2} 毫秒）—— …` / `连上了但拿不到流：{0}` | ① | `St/NoIp` · `St/ConnectTimeout` · `St/NoStream` | ❌ |
| `NetTransport.cs:307` `:308` `:311` `:315` `:316` `:319` `:372` `:393` | `读取中断：{0}`(×2) · `对面关掉了连接`(×2) · `帧长度不合理（{0} 字节）—— …` · `收到的帧解不出信封` · `还没连上，发不出去` · `发送失败：{0}` | ①（**间接**：经 `_lastError`→`St/Disconnected`） | `St/{ReadAbort,PeerClosed,BadFrame,BadEnvelope,NotConnected,SendFailed}` | ❌ |
| `NetTransport.cs:176` `:182` | `（双栈）` / `（仅 IPv4）`（`_lastError` 前缀） | ① | `St/StackDual` · `St/StackV4Only` | ❌ |
| `NetConfig.cs:303` `:307` | `两个方向都没探到 —— …` / `探测出错：{0}`（进 `EchoText`） | ① | `Settings/Online/Echo/{NoEcho,ProbeError}` | ❌ |
| `UpnpPortMapper.cs:108` `:122` `:164` `:178` `:216` `:219` `:221` `:230` `:237` | 九条 `r.message`（跳过 / 出错 / SSDP 0 回应 / 无服务 / 端口已占 / 725 / 被拒 / CGNAT / 成功） | ① | `Settings/Online/Upnp/{Busy,Error,NoResponse,NoService,PortTaken,NotPermitted,Rejected,Cgnat,Ok}` | ❌ |
| `NetBattle.cs` 十二处（见 ⑥·2） | `联机对局中止：…` / `对面离开了这一局` / `对手掉线了 —— 正在等他回来（{0} 秒后判他弃权）` 等 | ① | 候选 = `Battle/HUD/{LostConnectionMsg,WaitOpponentConnectionMsg,PleaseWaitConnection,MatchAlreadyFinished}`（**原版 13×4 场实测存在**） | ⛔ **已有裁定不接** |
| `NetBattle.cs:992` `:789` | `不是你的回合`（`MsgReject.reason` 走线）/ `这一局的联机房间已经散了` | Ⓦ | `Settings/Online/Wire/{NotYourTurn,RoomGone}` | ❌ |

## ③ 缺键表（**全部自拟**：联机层是我们自建的，原版走 PlayFab 服务端 ⇒ 两张表都搜过、无对应，见 ⑥·3）

🔴 **两条省字规则，写手照做**：**ZH 列一律 = ② 表里该行原字串【逐字照抄】**（去掉 `**` 强调标记、保留 `\n`/`{0}`）；**英文列提案如下**（⛔ 不许含汉字 —— 灭自证 C1 要断 `!Loc.HasCjk(en)`）。
- `Settings/Online/St/`：`Off`=Not connected · `HostFailed`=Host failed: {0} · `Listening`=Host ready — waiting for a client on port {0} (give your IP to the other player) · `Connecting`=Connecting to {0}:{1} … · `Handshaking`=Connected — checking protocol version and password… · `PeerLostInBattle`=Opponent disconnected — waiting for them (the battle is paused) · `Disconnected`=Connection lost: {0} · `PeerBack`=A connection came in — checking whether it is the same player… · `PeerJoined`=A client connected — checking… · `SilentTimeout`=No message from the opponent for {0} seconds · `Reconnecting`=Reconnecting to the host… · `CaughtUp`=Connected — catching up on this match… · `ReconnectFailed`=Reconnect failed, will retry: {0} · `BadHello`=The opponent's handshake packet could not be parsed · `VersionMismatch`=Different versions (opponent on protocol v{0}, this build on v{1}) — both sides need the same build · `WrongPassword`=Wrong password · `Refused`=This connection was refused: {0} · `PeerBackWaitReport`=`{0}` is back — waiting for their progress… · `PeerInLobby`=`{0}` joined — pick your decks and you can fight · `PeerRefused`=The opponent refused the connection · `PeerLeft`=The opponent left · `ResumedWaitProgress`=Connected to the host — waiting for this match's progress… · `ClientLobby`=Connected to the host — pick your decks and you can fight · `ResumeSent`=`{0}` is back — sent {1} actions from this match · `ResumeCaughtUp`=Caught up — replaying {0} actions from this match
- `Settings/Online/St/`（续）：`RejectBadKey`=Reconnect refused: the key does not match · `RejectNoLog`=Reconnect refused: the host has no authoritative action log · `InBattle`=In battle · `HostSide`= (this machine is the host — it orders the actions) · `ClientSide`= (client: actions are confirmed by the host) · `PortBusy`=Port {0} is already in use — pick another port, or close the other running instance · `ConnRefused`=The opponent refused (nothing is listening on port {0}) — the host must press Save and keep the game open · `HostNotFound`=This IP address was not found — check for a typo · `SocketError`=Network error: {0} · `NoIp`=No IP address filled in · `ConnectTimeout`=Timed out connecting to {0}:{1} ({2} ms) — no host there, or a firewall is blocking · `NoStream`=Connected but could not get the stream: {0} · `ReadAbort`=Read interrupted: {0} · `PeerClosed`=The opponent closed the connection · `BadFrame`=Bad frame length ({0} bytes) — what the opponent sent is not a frame of this protocol · `BadEnvelope`=The received frame has no parseable envelope · `NotConnected`=Not connected yet, cannot send · `SendFailed`=Send failed: {0} · `StackDual`=(dual stack) · `StackV4Only`=(IPv4 only)
- `Settings/Online/Lobby/`：`PeerLostHint`=Opponent disconnected, {0} (both of you press Battle! again after they return) · `PeerLost`=Opponent disconnected — the connection is gone.\n{0}, back to the lobby.\n(After the opponent returns, both of you press Battle! once more. At that moment the original ran `SearchOpponentManager.CancelSearchForDisconnect`: popup + cancel search.) · `PeerLeftHint`=Match ended: {0} — {1} · `PeerLeft`=Match ended: {0}\n{1}, back to the lobby.\n…（同上那句原版出处，照抄） · `MatchRevoked`=This match's setup has been revoked · `NotMatchingThisGame`=(this machine was not matching this match anyway) · `DeferToBattle`={0} — the match has already started, entering the arena (the battle layer will continue) · `BotNoLink`=Not connected · `BotSessionNotReady`=The session is now `{0}` (handshake not finished) · `BotEmptyDeck`=This deck is empty · `PlayedVsBot`=**This match is against the AI, not online.**\nReason: {0}.\nYou have configured online play — go to Settings → Online and press {1} once, then press `Battle!` again. · `LobbyRestored`=The opponent is back — online play restored. Both sides press `Battle!` once more to start. · `StartAfterCancel`=The opponent started the match after you cancelled — **this one did not go through**.\nThe other side will stay on the waiting screen; please arrange it again. · `MissedCancel`=The opponent cancelled after you started — **this match starts anyway**.\nThey will see "already started, cannot cancel"; the only way out is to resign during the battle. · `PeerCancelled`=The opponent cancelled this match — **neither side started**, back to the lobby.\nYou can each press `Battle!` again. · `ModeMismatch`=You picked different modes: this machine chose "{0}", the opponent chose "{1}".\nThe match did not start — please pick **the same mode** and each press `Battle!` again. · `StartParseFailed`=The match parameters could not be parsed, so this match cannot start.\nBoth of you go back to the main menu and press `Battle!` again.
- `Settings/Online/Cancel/`：`WhyNoLink`=Not connected online (this match was never an online one) · `WhyNotMatching`=This match has not entered online matchmaking yet · `WhyStarted`=**This match has already started** (the start packet was sent/received) — it cannot be cancelled; to leave, resign during the battle.
- `Settings/Online/Echo/`：`NoEcho`=Neither direction got a response — **the echo sites may be blocked by your network** (this does **not** mean "you have no public address"). · `ProbeError`=Probe error: {0}
- `Settings/Online/Upnp/`：`Busy`=The previous "ask the router for a port" has not finished — skipping this time. · `Error`=Error while asking the router for a port: {0} · `NoResponse`=Could not get a port mapping from the router (UPnP is off, or the router does not support it).\n→ To let a friend connect: ① turn **UPnP on** in the router admin page and press Save again; ② or both install the same virtual-LAN tool (Tailscale / ZeroTier, see 【How to connect】). · `NoService`=The router answered, but it **does not offer a port-mapping service** (not a typical home router firmware).\n→ This one can only go through a virtual-LAN tool (see 【How to connect】). · `PortTaken`=The router says **port {0} already has another mapping** ⇒ try a different port (or delete that old mapping in the router admin page). · `NotPermitted`=The router does not accept "permanent" mappings (error 725) — this one has to be mapped manually on the router. · `Rejected`=The router **refused** the port-mapping request (error {0}).\n→ Some firmwares block inbound mappings even with UPnP on; this one has to take another route (see 【How to connect】). · `Cgnat`=✅ The port mapping was granted, **but this machine is very likely behind a carrier-grade NAT (CGNAT)** —\nthe router's own WAN address is {0} (**a private range**) ⇒ outside connections still will not get in.\n→ Call your ISP and ask for a "public IP", or use a virtual-LAN tool. · `Ok`=✅ Port {0} (TCP) is now open on the router{1} — give the **public address + port** to a friend and they can connect.
- `Settings/Online/Wire/`（Ⓦ，**裁决后再做**）：`HostRestarted` · `BadKey` · `NoRecord` · `BadResume` · `PeerDone` · `NotYourTurn` · `RoomGone`
- 原版**有键名**、值在远端（⛔ 只有在决定接 `NetBattle` 那 12 处时才加）：`Battle/HUD/{LostConnectionMsg,WaitOpponentConnectionMsg,PleaseWaitConnection,MatchAlreadyFinished}` · `CustomErrors/{ErrorSavingMatch,DuplicateConnection}` · `CustomErrors/InternetUnreachable`（后者是 `NetMatchmaking.cs:102` 注释点名的原版弹窗键）

## ④ 切块建议（**文件所有权零交集**；三批都能并行）

- **P6a** = `NetSession.cs` + `NetRuntime.cs`（**37 处，最重**；`Runtime` 那 2 颗钮只差换成已在表的 `MainMenu/General/OK`）
- **P6b** = `NetMatchmaking.cs` + `NetBattle.cs`（19 处 + 12 处；⛔ `NetBattle` 先看 ⑥·2 的裁定，**默认不动**）
- **P6c** = `NetTransport.cs` + `NetConfig.cs` + `UpnpPortMapper.cs`（26 处；三份互不相干，可再拆三）
- **不动**：`NetApply.cs` · `NetProtocol.cs`（① 处 = 0）
- ⚠️ **三批都依赖 `Core/Loc.cs` 里那一族新键** ⇒ 仍然「**波 0b 补完键才开**」（`Loc.cs` 一个时刻一个写手）

## ⑤ 断言宿主

`Editor/NetSelfTest.cs`（会话/握手/状态字/大厅提示行/`Notice` 队列 —— P6a+P6b+P6c）· `Editor/NetBattleTest.cs`（对局层：掉线/重连/弃权 —— P6b）· **`Editor/SettingsScene.cs`**（联机页那一行 `StatusText`/`_flash` + 测外网 + UPnP 弹窗 —— P6a+P6c）。
🔴 **改前必读**（现核，**这些断言按中文子串断，文案一改就红 ⇒ 改断言的人和改代码的人必须同批**）：
`NetSelfTest.cs` `:106 "占用"` · `:150 "密码"` · `:173 "版本"` · `:335 "打的是电脑"` · `:343 "检查连接"` · `:400 "对面取消了"` · `:417 "已经开局"` · `:540 "断开"` · `:542/:753 "掉线"` · `:560 "回来"` · `:1225/:1250 "没发出去"` · `:1441/:1456 "传输层报失败了"` · `:1474 "被忽略"`；`NetBattleTest.cs` `:630/:632/:1105/:1107 "掉线"` · `:638/:901 "回来"` · `:652 "离开"` · `:655 "结束"` · `:1193/:1195 "弃权"` · **`:994 Contains("这一局的联机房间已经散了")`**（那是 `NetBatle.cs:789` 的 `Close` 理由原文 ⇒ 换键后这条**必红**）。
📌 三条必备 + 灭自证 C1/C2 照 `施工单_双语③` §⑧；**提示行那批额外加一条长度断言**：`Loc.T(键).Length <= SearchingMatchPopup.HintLineMaxChars`（**40**，中文按字符数算 —— `SearchingMatchPopup.cs:416` 每次超了都 `LogWarning`）。

## ⑥ 没查清 / 判不了

1. 🔴 **Ⓦ「走线文案算谁的语言」—— 判据是空的，需要裁决**（单列一类，别混进 ①）。七处（`NetSession.cs:434/442/461/567` · `NetBattle.cs:992/789` · `NetRuntime.cs:179`）**会穿过 TCP 到对端屏幕**：`bye`/`ack`/`reject` 的 `reason`。接 `Loc.T` = 把**发送方**的语言灌到**接收方**界面上；原版是服务端发 ID、客户端本地取词，**我们没有对等物、本地拿不到那一侧证据**（`NetBattle.cs:644-650` 已记同一条）。⇒ **本单判「先不动」**，键名留在 ③，**做不做等主对话拍板**。
2. ✅ **`NetBattle.cs` 那 12 处已有裁定「不接」**（`:578-650`，理由两条：① 两个语档的文案本地**都**取不到（9 条是 `LocalizedString` 字段、**值**在远端 I2 表）② 形状不同——原版是弹窗里的**通用短句**+`MainMenu/General/OK` 钮，我们是**带运行期参数的整句**、落点是 HUD 提示行）。**本单不改这条裁定**，只把键候选补齐。
3. **两张表都搜过、原版无对应**（这是 ③ 全判「自拟」的依据）：`d:/2/新解包资源/assets_full/**/MonoBehaviour/*.json` 的 `mTerm` 全库扫（**搜过的词** = `Online|Connection|Opponent|Reconnect|Search|Lobby|Match` + 全前缀 `CustomErrors/`·`MainMenu/General/`）⇒ 只命中 `Battle/HUD/{LostConnectionMsg,WaitOpponentConnectionMsg,PleaseWaitConnection,MatchAlreadyFinished}`(**各 13**)、`CustomErrors/{ErrorSavingMatch(26),DuplicateConnection(13)}`、`MainMenu/General/{OK(18),Cancel(1),Confirm(4),Select(3)}`、`Demo/{DeckSelectionDemo/Searching(5),FriendsMenu/SearchPlayer(1)}`；`d:/2/tools/il2cpp_out/stringliteral.json`（26,507 条）⇒ 同上 + `CustomErrors/InternetUnreachable`。⛔ **`Settings/Online/*` 在两张表里都没有**（原版的联机设置页走 PlayFab，根本不是这套流程）。
4. **没查**：`NetConfig.LocalAddr.Label`（`网卡名 + 地址`，网卡名是系统串，不可翻）· `Shell/SearchingOpponentWindow` 那边展示的名字/阵营走 `ProfileData`/`CardText`（**另一条线**）· `UpnpPortMapper` 的 `r.detail`（只进 `Debug.Log`，判 ②）。

## ⑦ 顺手发现（⛔ 我没改任何一个字，交调度台分流）

1. 🔴 **P3 交件 §⑥·1 的条数对不上**：它写「`NetMatchmaking` **6** 条 + `NetBattle` **2** 条 = 8 条」，**现读**是 **19 + 12**；而且 `NetRuntime.cs` 那两颗钮**共 2 处**（`:109`/`:138`），不是 1 处。
2. 🔴 **「Net/ 玩家可见的入口不止弹窗」**：`P3` 只点了 `Notice` 一类，**漏了三个更常用的口** —— `NetSession.StatusText`（设置→联机页那一行，34 处）· `NetTransport._lastError`（15 处，经 `_flash`）· `NetMatchmaking.OnHint` 提示行（19 处，`SearchingMatchPopup` 那个 40 字框）。
3. ⚠️ **`NetTransport.Describe()` 是「玩家可见」而不是日志** —— 它返回的整句被写进 `_lastError`、经 `NetSession.LastError` 印在设置窗 `_flash` 上（`SettingsWindow.cs:2457`）；`NetSelfTest.cs:106/:173` 正是按它断言的。
4. ⚠️ **`NetBattle.cs:578-650` 那段注释里的行号已漂**（`A933` 那次订正自己就记着「落点漂了」）：它写 `:223`/`:227-229`/`:603`/`:607-608`/`:717`/`:729`/`:906-907`/`:952`，现读实际是 `:232`/`:236-238`/`:643`/`:647-648`/`:757`/`:769`/`:946-947`/`:992` —— **按行号找会全落空**（铁律 5/6：建议把那段改成「按符号名索引」）。
5. 📌 `Shell/SettingsWindow.cs` 联机页自己还有 **≈60 行**上屏中文（`"主机已就绪…"` `"三条路，从最省事开始…"` `EchoText` 整段 …）—— **不在 `Net/`**，但和本单同一个显示口 ⇒ **建议与 P6 合并到同一批**，否则那一页会变成「一半中文一半英文」。
