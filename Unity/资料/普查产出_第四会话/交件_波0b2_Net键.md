# 交件 · 双语③ **波 0b2** —— `Net/` 整片（P6）的键并进 `Core/Loc.cs`

## ① 结论
- **新增 76 条**：`Settings/Online/St/*` **45** · `Lobby/*` **17** · `Cancel/*` **3** · `Echo/*` **2** · `Upnp/*` **9**。一条不漏、零重名。
- `Loc.EntryCount`：**301 → 377**（现读实测。简报里写的「302」是**旧数** —— 那句注解自己就写着「之后删掉了 1 条孤儿键 `MenuDeck/HUD/DefaultCardback`」，删完正是 301）。
- 秒级类型检查（`TMPDIR=/tmp/wf_0b2 bash d:/4/Unity/工具/typecheck.sh`）：**运行时 0 / 编辑器 0**；`git diff --numstat` = **569 0**（纯增；本波 +178 行 = 76 条 + 注释）。`Loc.cs` **1506 → 1684 行 / 165,511 → 191,291 B**，**纯 LF**（CRLF = 0）。
- 只动 `Core/Loc.cs`；⛔ 没碰调用点 / `Editor/*` / `Net/*` / 文档正本 / git。**今天界面一个字都不变**（本节只加键）。
- 🔴 **落表前先逐键 diff 过既有 301 条**（脚本按 `{ "键",` + `new Entry` 取全表键名）：**0 处同名**。

## ② 总键表（76 条 · 键名 | EN | ZH | 来源 | 依据）
> 来源：**全 76 条键名 + 英文列都是自拟**（采纳 P6 §③ 的英文提案逐字 —— 仅 `NoResponse`/`NoService`/`Rejected` 三条的 `【怎么联机】` 改成 `[How to connect]`，见 ⑤）；中文列 = **调用点原话逐字**（⇒ 切成这些键之后**中文档零变化**）。`\n` = 真换行 · `{0}`/`{1}`/`{2}` = 运行期值。依据列缩写：`S`=`Net/NetSession.cs` · `M`=`NetMatchmaking.cs` · `T`=`NetTransport.cs` · `C`=`NetConfig.cs` · `U`=`UpnpPortMapper.cs`。

| 键名 | EN | ZH | 来源 | 依据 |
|---|---|---|---|---|
| `Settings/Online/St/Off` | Not connected | 未连接 | 自拟 | `S:102/573` |
| `Settings/Online/St/HostFailed` | Host failed: {0} | 主机没起来：{0} | 自拟 | `S:133` |
| `Settings/Online/St/Listening` | Host ready — waiting for a client on port {0} (give your IP to the other player) | 主机已就绪，在 {0} 端口等客机（把本机 IP 告诉对方） | 自拟 | `S:143` |
| `Settings/Online/St/Connecting` | Connecting to {0}:{1} … | 正在连 {0}:{1} … | 自拟 | `S:177` |
| `Settings/Online/St/Handshaking` | Connected — checking protocol version and password… | 连上了，正在核对协议版本与密码… | 自拟 | `S:181/354` |
| `Settings/Online/St/PeerLostInBattle` | Opponent disconnected — waiting for them (the battle is paused) | 对手掉线了，正在等他回来…（对局已暂停） | 自拟 | `S:210/294` |
| `Settings/Online/St/Disconnected` | Connection lost: {0} | 连接断了：{0} | 自拟 | `S:211/295` |
| `Settings/Online/St/PeerBack` | A connection came in — checking whether it is the same player… | 有连接进来，正在核对是不是刚才那个人… | 自拟 | `S:256` |
| `Settings/Online/St/PeerJoined` | A client connected — checking… | 有客机连进来了，正在核对… | 自拟 | `S:256` |
| `Settings/Online/St/SilentTimeout` | No message from the opponent for {0} seconds | {0} 秒没收到对面的任何消息 | 自拟 | `S:289` |
| `Settings/Online/St/Reconnecting` | Reconnecting to the host… | 正在重连主机… | 自拟 | `S:311` |
| `Settings/Online/St/CaughtUp` | Connected — catching up on this match… | 连上了，正在补上这一局的进度… | 自拟 | `S:315` |
| `Settings/Online/St/ReconnectFailed` | Reconnect failed, will retry: {0} | 重连失败，稍后再试：{0} | 自拟 | `S:319` |
| `Settings/Online/St/BadHello` | The opponent's handshake packet could not be parsed | 对面发来的握手包解不出来 | 自拟 | `S:361` |
| `Settings/Online/St/VersionMismatch` | Different versions (opponent on protocol v{0}, this build on v{1}) — both sides need the same build | 两边版本不一样（对面协议 v{0}，本机 v{1}）—— 要用同一份构建 | 自拟 | `S:363` |
| `Settings/Online/St/WrongPassword` | Wrong password | 密码不对 | 自拟 | `S:367` |
| `Settings/Online/St/Refused` | This connection was refused: {0} | 拒绝了这次连接：{0} | 自拟 | `S:380` |
| `Settings/Online/St/PeerBackWaitReport` | `{0}` is back — waiting for their progress… | 「{0}」连回来了，正在等他报进度… | 自拟 | `S:387` |
| `Settings/Online/St/PeerInLobby` | `{0}` joined — pick your decks and you can fight | 「{0}」进来了 —— 各自选好卡组就能开战 | 自拟 | `S:391` |
| `Settings/Online/St/PeerRefused` | The opponent refused the connection | 对面拒绝了连接 | 自拟 | `S:405` |
| `Settings/Online/St/PeerLeft` | The opponent left | 对面退出了 | 自拟 | `M:213-215` |
| `Settings/Online/St/ResumedWaitProgress` | Connected to the host — waiting for this match's progress… | 已经连上主机，正在等他补这一局的进度… | 自拟 | `S:420` |
| `Settings/Online/St/ClientLobby` | Connected to the host — pick your decks and you can fight | 连上主机了 —— 各自选好卡组就能开战 | 自拟 | `S:424` |
| `Settings/Online/St/ResumeSent` | `{0}` is back — sent {1} actions from this match | 「{0}」回来了 —— 已把这一局的 {1} 条动作发过去 | 自拟 | `S:454` |
| `Settings/Online/St/ResumeCaughtUp` | Caught up — replaying {0} actions from this match | 追上了 —— 重放这一局的 {0} 条动作 | 自拟 | `S:464` |
| `Settings/Online/St/RejectBadKey` | Reconnect refused: the key does not match | 重连被拒：钥匙对不上 | 自拟 | `S:435` |
| `Settings/Online/St/RejectNoLog` | Reconnect refused: the host has no authoritative action log | 重连被拒：主机没有权威动作流 | 自拟 | `S:443` |
| `Settings/Online/St/InBattle` | In battle | 对局中 | 自拟 | `S:550` |
| `Settings/Online/St/HostSide` |  (this machine is the host — it orders the actions) | （本机是主机，动作由本机定序） | 自拟 | `S:550` |
| `Settings/Online/St/ClientSide` |  (client: actions are confirmed by the host) | （客机：操作由主机确认） | 自拟 | `S:550` |
| `Settings/Online/St/PortBusy` | Port {0} is already in use — pick another port, or close the other running instance | 端口 {0} 已被占用 —— 换一个端口，或先关掉已经在跑的那个实例 | 自拟 | `T:420` |
| `Settings/Online/St/ConnRefused` | The opponent refused (nothing is listening on port {0}) — the host must press Save and keep the game open | 对面拒绝了连接（{0} 端口没人在听）—— 主机那边要先点「保存」并保持游戏开着 | 自拟 | `T:422` |
| `Settings/Online/St/HostNotFound` | This IP address was not found — check for a typo | 这个 IP 地址找不到——检查一下有没有抄错 | 自拟 | `T:424` |
| `Settings/Online/St/SocketError` | Network error: {0} | 网络错误：{0} | 自拟 | `T:425` |
| `Settings/Online/St/NoIp` | No IP address filled in | 没有填 IP 地址 | 自拟 | `T:210` |
| `Settings/Online/St/ConnectTimeout` | Timed out connecting to {0}:{1} ({2} ms) — no host there, or a firewall is blocking | 连接 {0}:{1} 超时（{2} 毫秒）—— 对面没开主机，或防火墙挡住了 | 自拟 | `T:226` |
| `Settings/Online/St/NoStream` | Connected but could not get the stream: {0} | 连上了但拿不到流：{0} | 自拟 | `T:282` |
| `Settings/Online/St/ReadAbort` | Read interrupted: {0} | 读取中断：{0} | 自拟 | `T:307/315` |
| `Settings/Online/St/PeerClosed` | The opponent closed the connection | 对面关掉了连接 | 自拟 | `T:308/316` |
| `Settings/Online/St/BadFrame` | Bad frame length ({0} bytes) — what the opponent sent is not a frame of this protocol | 帧长度不合理（{0} 字节）—— 对面发的不是本协议的帧 | 自拟 | `T:311` |
| `Settings/Online/St/BadEnvelope` | The received frame has no parseable envelope | 收到的帧解不出信封 | 自拟 | `T:319` |
| `Settings/Online/St/NotConnected` | Not connected yet, cannot send | 还没连上，发不出去 | 自拟 | `T:372` |
| `Settings/Online/St/SendFailed` | Send failed: {0} | 发送失败：{0} | 自拟 | `T:393` |
| `Settings/Online/St/StackDual` | (dual stack) | （双栈） | 自拟 | `T:176` |
| `Settings/Online/St/StackV4Only` | (IPv4 only) | （仅 IPv4） | 自拟 | `T:182` |
| `Settings/Online/Lobby/PeerLostHint` | Opponent disconnected, {0} (both of you press Battle! again after they return) | 对面掉线了，{0}（两边回来各点一次 `Battle!`） | 自拟 | `M:197` |
| `Settings/Online/Lobby/PeerLost` | Opponent disconnected — the connection is gone.\n{0}, back to the lobby.\n(After the opponent returns, both of you press Battle! once more. At that moment the original ran SearchOpponentManager.CancelSearchForDisconnect: popup + cancel search.) | 对面掉线了 —— 联机断开。\n{0}，回到大厅。\n（对面回来之后，两边各自重新点一次 `Battle!`。原版那一刻走的是 `SearchOpponentManager.CancelSearchForDisconnect`：弹窗 + 取消搜索。） | 自拟 | `M:198-200` |
| `Settings/Online/Lobby/PeerLeftHint` | Match ended: {0} — {1} | 联机结束：{0} —— {1} | 自拟 | `M:212` |
| `Settings/Online/Lobby/PeerLeft` | Match ended: {0}\n{1}, back to the lobby.\n(To play again: both of you press Battle! once more. At that moment the original ran SearchOpponentManager.CancelSearchForDisconnect: popup + cancel search.) | 联机结束：{0}\n{1}，回到大厅。\n（要再打一局：两边重新各点一次 `Battle!`。原版那一刻走的是 `SearchOpponentManager.CancelSearchForDisconnect`：弹窗 + 取消搜索。） | 自拟 | `M:213-215` |
| `Settings/Online/Lobby/MatchRevoked` | This match's setup has been revoked | 这一局的匹配已经撤销 | 自拟 | `M:249` |
| `Settings/Online/Lobby/NotMatchingThisGame` | (this machine was not matching this match anyway) | （本机本来就没在匹配这一局） | 自拟 | `M:249` |
| `Settings/Online/Lobby/DeferToBattle` | {0} — the match has already started, entering the arena (the battle layer will continue) | {0} —— 已开局、正在进战场（后面由对局那一层说） | 自拟 | `M:233` |
| `Settings/Online/Lobby/BotNoLink` | Not connected | 联机没连上 | 自拟 | `M:360` |
| `Settings/Online/Lobby/BotSessionNotReady` | The session is now `{0}` (handshake not finished) | 联机会话现在是 `{0}`（还没握手完） | 自拟 | `M:362` |
| `Settings/Online/Lobby/BotEmptyDeck` | This deck is empty | 这副牌是空的 | 自拟 | `M:363` |
| `Settings/Online/Lobby/PlayedVsBot` | **This match is against the AI, not online.**\nReason: {0}.\nYou have configured online play — go to Settings → Online and press {1} once, then press `Battle!` again. | 这一局**打的是电脑，不是联机**。\n原因：{0}。\n你在设置里配过联机了 —— 请到「设置 → 联机」点一次{1}，再回来点 `Battle!`。 | 自拟 | `M:402-406` |
| `Settings/Online/Lobby/LobbyRestored` | The opponent is back — online play restored. Both sides press `Battle!` once more to start. | 对面回来了 —— 联机已恢复。要开这一局，两边重新各点一次 `Battle!` | 自拟 | `M:427` |
| `Settings/Online/Lobby/StartAfterCancel` | The opponent started the match after you cancelled — **this one did not go through**.\nThe other side will stay on the waiting screen; please arrange it again. | 对面在你取消之后开局了 —— 这一局**没有进**。\n对面那边会停在等待界面上，请重新约一次。 | 自拟 | `M:466-467` |
| `Settings/Online/Lobby/MissedCancel` | The opponent cancelled after you started — **this match starts anyway**.\nThey will see \"already started, cannot cancel\"; the only way out is to resign during the battle. | 对面在你开局之后才点了取消 —— 这一局**照旧开始**。\n对面那边会看到「已经开局、取消不了」，要退出只能在对局里投降。 | 自拟 | `M:502-503` |
| `Settings/Online/Lobby/PeerCancelled` | The opponent cancelled this match — **neither side started**, back to the lobby.\nYou can each press `Battle!` again. | 对面取消了这一局的匹配 —— **双方都没有开局**，退回大厅。\n可以各自重新点一次 `Battle!`。 | 自拟 | `M:509-510` |
| `Settings/Online/Lobby/ModeMismatch` | You picked different modes: this machine chose \"{0}\", the opponent chose \"{1}\".\nThe match did not start — please pick **the same mode** and each press `Battle!` again. | 两边选的模式不一样：本机是「{0}」，对面是「{1}」。\n这一局没有开成 —— 请两位换成**同一个模式**，再各自点一次 `Battle!`。 | 自拟 | `M:539-540` |
| `Settings/Online/Lobby/StartParseFailed` | The match parameters could not be parsed, so this match cannot start.\nBoth of you go back to the main menu and press `Battle!` again. | 开局参数没能解析出来，这一局开不了。\n请两边都退回主菜单，重新点一次 `Battle!`。 | 自拟 | `M:587` |
| `Settings/Online/Cancel/WhyNoLink` | Not connected online (this match was never an online one) | 联机没连上（这一局本来就没走联机） | 自拟 | `M:62` |
| `Settings/Online/Cancel/WhyNotMatching` | This match has not entered online matchmaking yet | 这一局还没进入联机匹配 | 自拟 | `M:63` |
| `Settings/Online/Cancel/WhyStarted` | **This match has already started** (the start packet was sent/received) — it cannot be cancelled; to leave, resign during the battle. | 这一局**已经开局了**（开局包已经发出/收到）—— 取消不了；要退出请在对局里投降。 | 自拟 | `M:67` |
| `Settings/Online/Echo/NoEcho` | Neither direction got a response — **the echo sites may be blocked by your network** (this does **not** mean \"you have no public address\"). | 两个方向都没探到 —— **可能是回显站被网络挡了**（不是「你没有公网地址」）。 | 自拟 | `C:303` |
| `Settings/Online/Echo/ProbeError` | Probe error: {0} | 探测出错：{0} | 自拟 | `C:307` |
| `Settings/Online/Upnp/Busy` | The previous \"ask the router for a port\" has not finished — skipping this time. | 上一次「向路由器要端口」还没跑完 —— 这次先跳过。 | 自拟 | `U:108` |
| `Settings/Online/Upnp/Error` | Error while asking the router for a port: {0} | 向路由器要端口时出错：{0} | 自拟 | `U:122` |
| `Settings/Online/Upnp/NoResponse` | Could not get a port mapping from the router (UPnP is off, or the router does not support it).\n→ To let a friend connect: ① turn **UPnP on** in the router admin page and press Save again; ② or both install the same virtual-LAN tool (Tailscale / ZeroTier, see [How to connect]). | 没能从路由器那里问到端口映射（UPnP 没开、或路由器不支持）。\n→ 想让网友连进来：① 去路由器管理页把 **UPnP 打开** 再点一次【保存】；② 或者两边装同一个虚拟局域网工具（Tailscale / ZeroTier 这类，见【怎么联机】）。 | 自拟 | `U:164` |
| `Settings/Online/Upnp/NoService` | The router answered, but it **does not offer a port-mapping service** (not a typical home router firmware).\n→ This one can only go through a virtual-LAN tool (see [How to connect]). | 路由器回应了，但它**没有提供端口映射服务**（不是常见的家用路由器固件）。\n→ 这条只能走虚拟局域网工具那条路（见【怎么联机】）。 | 自拟 | `U:178` |
| `Settings/Online/Upnp/PortTaken` | The router says **port {0} already has another mapping** ⇒ try a different port (or delete that old mapping in the router admin page). | 路由器说 **{0} 这个端口上已经有别的映射了** ⇒ 换一个端口再来（或者去路由器管理页把那条旧映射删掉）。 | 自拟 | `U:216` |
| `Settings/Online/Upnp/NotPermitted` | The router does not accept \"permanent\" mappings (error 725) — this one has to be mapped manually on the router. | 路由器不接受「永久」映射（错误码 725）—— 这一台得手动在路由器上做端口映射。 | 自拟 | `U:219` |
| `Settings/Online/Upnp/Rejected` | The router **refused** the port-mapping request (error {0}).\n→ Some firmwares block inbound mappings even with UPnP on; this one has to take another route (see [How to connect]). | 路由器**拒绝了**端口映射请求（错误码 {0}）。\n→ 有些固件即使开着 UPnP 也不放行入站映射，这条只能走别的路（见【怎么联机】）。 | 自拟 | `U:221` |
| `Settings/Online/Upnp/Cgnat` | ✅ The port mapping was granted, **but this machine is very likely behind a carrier-grade NAT (CGNAT)** —\nthe router's own WAN address is {0} (**a private range**) ⇒ outside connections still will not get in.\n→ Call your ISP and ask for a \"public IP\", or use a virtual-LAN tool. | ✅ 端口映射要到了，**但你这台大概率在「大内网」(CGNAT) 里** ——\n路由器自己的外网地址是 {0}（**私网段**）⇒ 外面照样连不进来。\n→ 这种情况打客服电话要「公网 IP」才有用，或走虚拟局域网工具。 | 自拟 | `U:230` |
| `Settings/Online/Upnp/Ok` | ✅ Port {0} (TCP) is now open on the router{1} — give the **public address + port** to a friend and they can connect. | ✅ 已经在路由器上开好了 {0} 端口（TCP）{1} —— 把**外网地址 + 端口**给朋友就能连进来。 | 自拟 | `U:237` |

## ③ 与波 0b / 既有 301 条的重叠核验（逐键 diff）
- 76 条逐键比 301 条既有键 ⇒ **0 处同名**（脚本核，非目测）。🔴 调度台点名的那条重叠风险（P6 的 `Settings/Online/*` 会不会撞 P2 同一族）：**同前缀、键名全不相交** ——
  最近的两条是 P2 的 `Settings/Online/HostFailed`（「主机没起来：」，**不带占位符**）vs 本波 `Settings/Online/St/HostFailed`（「主机没起来：{0}」）⇒ **不同名**，不构成「同名不同值」那种要停手的冲突；两条并存（要不要合并见 ⑦·5）。
- ⛔ `NetRuntime.cs:109/:138` 那两颗钮**没有建键** —— 它们该用**已在表**的 `MainMenu/General/OK`（P6 §② 的判定，本波照做）。

## ④ 按 P6a / P6b / P6c 归属
- **P6a**（`NetSession.cs` + `NetRuntime.cs`）：`St/*` **30** 条（`S` 那一列的全部）。
- **P6b**（`NetMatchmaking.cs` + `NetBattle.cs`）：`Lobby/*` 17 + `Cancel/*` 3 = **20** 条。
- **P6c**（`NetTransport.cs` + `NetConfig.cs` + `UpnpPortMapper.cs`）：`St/*` **15**（`T` 那一列）+ `Echo/*` 2 + `Upnp/*` 9 = **26** 条。
- 45 = 30 + 15（`St/*` 一个族、两批分用；三批都只读这一族，互不冲突）。

## ⑤ 查不到 / 自拟 / 我改过提案的地方
- **没重查原版**（简报 §④ 明说别重复查）：沿用 P6 §⑥·3 的查证结论 —— 两张表全库扫，`Settings/Online/*` **0 命中**。⚠️ 我亲手复核了其中一半（表 2）：`grep -c "Settings/Online" d:/2/tools/il2cpp_out/stringliteral.json` = **0**；`assets_full` 整树用 `Grep` 工具扫 = **0**。
- ✅ **阴性结论跑过阳性对照**（防无效否定）：同一把 grep 对 `Battle/HUD/LostConnectionMsg` = **13 个文件** · `MainMenu/General/OK` = **18 个文件**，与 P6 记的数逐字吻合 ⇒ 扫描有效。
- **中文列来源**：全部 = 调用点原话，**逐行现读** `Net/*.cs` 核对（不是抄 P6 §② 的「首段」摘要 —— 那边只给首段）。🔴 **改过的英文提案（1 处、3 条键）**：`Upnp/{NoResponse,NoService,Rejected}` 提案里有 `【How to connect】`，`【`/`】`=U+3010/U+3011 落在 `Loc.HasCjk` 的 `0x3000–0x303F` ⇒ 照抄会让「英文列不许含汉字」那条断言（灭自证 C1）**直接红** ⇒ 写成 `[How to connect]`；其余英文**逐字照提案**。
- `PlayedVsBot` 的 `{1}` = 那颗钮名（主机「【保存】」/ 客机「【检查连接】」）；`Upnp/Ok` 的 `{1}` = 有外网地址时那句「，你家的外网地址是 X」，没有时空串。

## ⑥ 没做到的
- ⛔ `Wire/*` 7 条 · `Battle/Log/*` 12 句 · `Demo/MainMenu/OK` · `Battle/CardWindow/TapToClose` —— 按调度台 §③ 的裁定**一条没建**。
- ⛔ `NetBattle.cs` 那 12 处的候选键（`Battle/HUD/*` / `CustomErrors/*`）**没建**（P6 §⑥·2 已有裁定「不接」，那 4 条原版键只有在决定接时才该加）。
- ⛔ **没跑任何 Unity 自检**（本波零调用点改动 ⇒ 逻辑零变化）。⚠️ 但 `Core/Loc.cs` 是**共用件**：跑哪几条由调度台在同步点按铁律 12 的三条判据定。

## ⑦ 顺手发现（⛔ 一个字都没顺手改）
1. 🔴 **`**` 要不要留 —— 我按「逐字」留着，与 P6 §③ 的「去掉 `**`」相反，请裁。** 理由三条：① 那 12 条键的中文里 `**` 是**调用点 C# 字面量里就有的字符**（`NetMatchmaking.cs:402/:466/:502/:509/:539` · `NetSession.cs:363` 等，逐行现读过），不是文档排版；② 去掉它 = **悄悄改中文档**（与「ZH 逐字照抄 ⇒ 中文档零变化」自相矛盾）；③ 本表既有 **14 条**同款（含**已提交**的 `MenuDeck/Error/EffectOnlyCard`），P6 §③ 自己的**英文**提案里也满是 `**`。⚠️ 若裁决「去掉」⇒ 那 12 条（+ 波 0b 那 14 条）要一起改，且**调用点也得同时去**，否则表与代码打架。
2. 🔴 **P6 §③ 的族条数与实际不符**：`St/*` 写 **39**（实为 **45** —— 差的 6 条正是它自己在 §② 标「①（**间接**：经 `_lastError`→`St/Disconnected`）」那一段：`ReadAbort/PeerClosed/BadFrame/BadEnvelope/NotConnected/SendFailed`）· `Lobby/*` 写 **18**（实为 **17**）。同 P6 §⑦·1 那一类（它已经抓到 P3 的条数对不上）。
3. ⚠️ **`St/Handshaking` 是「一条键两处」，但两处原文不同**：`NetSession.cs:181` = 「连上了，正在核对协议版本与密码…」· `:354` = 「正在核对协议版本与密码…」（少了「连上了，」）。本表取 `:181` 那一版（与 P6 §③ 的英文提案同形）⇒ **P6a 落键时 `:354` 那句会跟着变**（`NetSelfTest.cs:150` 断的是子串「密码」，仍过）。
4. ⚠️ **P6 §② 那一格的 `NetSession.cs:211` 行号漂了一行**：`:211` 实际是 `LastError = why;`，字面量在 **`:210`**（`"连接断了：" + why`）。同族还有 `:295`↔`:294`。
5. ⚠️ **P2 的 `Settings/Online/HostFailed` 与本波 `…/St/HostFailed` 语义重复、形状不同**（不带/带 `{0}`；前者是设置页那半句、后者是状态字整句）⇒ 要不要合并请裁（现在两条并存，不会互相覆盖）。
6. ⚠️ **既有 377 条里、我核到的 376 条中有 8 条英文列会踩 `Loc.HasCjk`**（脚本按 `HasCjk` 的区间全表核过；漏核那 1 条 = `MainMenu/PurchasePremium/Description`，正则没吃下去）：**本波 0 条**；8 条全是**既有**的 —— 6 条含全角空格 `　`（U+3000，波 0b 的 `PublicAddress/Mismatch` · `ClickAgain` · `VirtualNic` · `HowToConnect/{VirtualLan,PublicDirect,DontUseTestSite}`）· 1 条含两个**真汉字**（波 0b 的 `MenuDeck/Error/PrebuiltMissing`：英文里写着 `python 工具/gen_prebuilt_decks.py`）· 1 条是既有的 `Battle/Log/TurnPrefix`（U+3000）。⇒ 将来 P6 §⑤ 那条 C1 断言一跑，**先红的是波 0b 那批，不是本波**。
