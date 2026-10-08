# WNet · 双语 P6 接线（`Net/` 整片 · 2026-10-19 · 动手写手交件）

> 白名单（**8 份里的 7 份动了**，第 8 份 `NetBattleTest.cs` **核过、一个字不用改**；别的一个字没动）：
> `Net/{NetSession,NetRuntime,NetMatchmaking,NetTransport,NetConfig,UpnpPortMapper}.cs` ·
> `Editor/{NetSelfTest,NetBattleTest}.cs`。
> ⛔ 没跑 Unity · 没碰 git（只读 `git diff --numstat` / `git show HEAD:`）· 没改 `Core/Loc.cs` ·
> 没改 `Net/NetBattle.cs` / `NetApply.cs` / `NetProtocol.cs` · 没改两张正本。
> 判据 = `资料/普查产出_第四会话/施工单_双语_Net整片_P6.md` §②（82 处权威）+ §⑤/§⑥ ·
> `资料/普查产出_第五会话/查证_23双语键盘点.md` 表 B6 · `施工单_双语③逐处换key.md` §⑧ ·
> `资料/普查产出_第六会话/查证_P6d协议兼容面.md`（**这份是交件中途才读到，直接影响下面 ⑤·1**）。

---

## ① 结论（逐条一句话）

1. ✅ **生产代码 6 份共落键 77 处**（`NetSession` 29 · `NetMatchmaking` 18 · `NetTransport` 16 行/15 条 ·
   `UpnpPortMapper` 9 · `NetRuntime` 2 · `NetConfig` 2），**全部用现成的键**，一条都没自造。
2. ✅ **旧断言同批改了 11 条**（全在 `Editor/NetSelfTest.cs`），改成**随语档**（断 `Loc.T(键)`／
   新增的 `TermHead(键)` 前缀），并在同文件补了 **1 个 helper（`TermHead`）** 与
   **1 个新方法（`TestNetTermBilingual`：79 条键 —— ①都在表里 ②C1/C2 灭自证 ③两语档取值不同 ④提示行 40 字预算）**。
3. ✅ **`Editor/NetBattleTest.cs` 一个字都不用改** —— 它那 11 条按中文子串断的断言，**全部**读的是
   `NetBattle.cs`（未改）或**自检自己喂进去**的串（`:630/:632/:638/:652/:655/:884/:893/:901/:994/:1105/:1107/:1193/:1195`），
   P6 换键**一条都不会带红**（逐条依据见 ③·3）。⛔ 按「不许越界改」**没动**。
4. ⏱ **秒级类型检查 0 错**（运行时 0 / 编辑器 0），行尾 **CRLF 全程 0**，`git diff --numstat` 是**小数字**（不是整篇重写）。
5. 🔴 **3 处「玩家可见的中文直传」留着没接** —— **表里确实没有对应的键**，按简报「找不到键就停手报回来、⛔ 不许自造键」处理，逐条见 ⑤·2/⑤·3/⑤·4。
6. 🔴 **1 处口径冲突要在 ⑤·1 里裁**：`NetSession.cs` 那三句 `why`（BadHello/VersionMismatch/WrongPassword）
   **同时是 `MsgAck.reason` 的走线串** —— 我按「施工单 §② 判它 ①」落了键，**如实标出它同时过 TCP**。

---

## ② 改动清单（逐条 · **行号 = 改后现读**）

### 2·A 生产代码（77 处 / 6 份）

#### `Net/NetRuntime.cs`（2 处 · 施工单 §② 第 1 行）

| 文件:行（改后） | 改前字面量 | 改成的键 |
|---|---|---|
| `NetRuntime.cs:109`（+3 行注释） | `wm.ShowPopUp(t, "知道了", null)` | `wm.ShowPopUp(t, Loc.T("MainMenu/General/OK"), null)` |
| `NetRuntime.cs:141` | `wm.ShowPopUp(text, "知道了", null)` | `wm.ShowPopUp(text, Loc.T("MainMenu/General/OK"), null)` |

> ⚠️ **玩家可见的变化**：中文档那颗钮从「知道了」变成**「确定」**（= `MainMenu/General/OK` 的 ZH 列，`Core/Loc.cs:997`）；
> 已在代码里就地标出（`NetRuntime.cs:109` 上面那 3 行注释）。
> ⛔ `NetRuntime.cs:182` 的 `"主机重开了"`（`Reset()` 的 `bye` 理由）**没动** = 简报圈定的 7 处走线文案之一。

#### `Net/NetSession.cs`（29 处 · 施工单 §② 第 2/3/4/5/6/7/8/9/10/11 行）

| 文件:行（改后） | 改前字面量 | 改成的键 |
|---|---|---|
| `:102` | `StatusText = "未连接";` | `St/Off` |
| `:133` | `"主机没起来：" + LastError` | `St/HostFailed`（`{0}` = LastError） |
| `:143` | `$"主机已就绪，在 {_t.Port} 端口等客机（把本机 IP 告诉对方）"` | `St/Listening`（`{0}` = `_t.Port`） |
| `:177` | `$"正在连 {cfg.ip}:{cfg.port} …"` | `St/Connecting` |
| `:181` | `"连上了，正在核对协议版本与密码…"` | `St/Handshaking` |
| `:209`（2 行） | `(_wasInBattle ? "对手掉线了，正在等他回来…（对局已暂停）" : "连接断了：" + why)` | `St/PeerLostInBattle` / `St/Disconnected` |
| `:256`（2 行） | `? "有连接进来，正在核对是不是刚才那个人…" : "有客机连进来了，正在核对…"` | `St/PeerBack` / `St/PeerJoined` |
| `:290` | `$"{SilentTimeoutMs / 1000} 秒没收到对面的任何消息"` | `St/SilentTimeout` |
| `:294`（3 行） | `_wasInBattle ? "对手掉线了…（对局已暂停）" : "连接断了：" + LastError` | `St/PeerLostInBattle` / `St/Disconnected` |
| `:312` | `"正在重连主机…"` | `St/Reconnecting` |
| `:316` | `"连上了，正在补上这一局的进度…"` | `St/CaughtUp` |
| `:320` | `"重连失败，稍后再试：" + _t.LastError` | `St/ReconnectFailed` |
| `:355`（+3 行注释） | `"正在核对协议版本与密码…"` | `St/Handshaking`（**与 `:181` 共用一条键** ⇒ 见下 ⚠️） |
| `:364`→`:371`（+6 行注释） | `why = "对面发来的握手包解不出来";` | `St/BadHello` |
| `:373` | `$"两边版本不一样（对面协议 v{m.protoVer}，本机 v{NetProtocol.Version}）—— 要用同一份构建"` | `St/VersionMismatch` |
| `:377` | `why = "密码不对";` | `St/WrongPassword` |
| `:390` | `"拒绝了这次连接：" + why` | `St/Refused` |
| `:397` | `$"「{PeerName}」连回来了，正在等他报进度…"` | `St/PeerBackWaitReport` |
| `:401` | `$"「{PeerName}」进来了 —— 各自选好卡组就能开战"` | `St/PeerInLobby` |
| `:415` | `: "对面拒绝了连接";` | `St/PeerRefused` |
| `:430` | `"已经连上主机，正在等他补这一局的进度…"` | `St/ResumedWaitProgress` |
| `:434` | `"连上主机了 —— 各自选好卡组就能开战"` | `St/ClientLobby` |
| `:445` | `LastError = "重连被拒：钥匙对不上";` | `St/RejectBadKey` |
| `:453` | `LastError = "重连被拒：主机没有权威动作流";` | `St/RejectNoLog` |
| `:464` | `$"「{PeerName}」回来了 —— 已把这一局的 {actions?.Count ?? 0} 条动作发过去"` | `St/ResumeSent` |
| `:474` | `$"追上了 —— 重放这一局的 {actions.Count} 条动作"` | `St/ResumeCaughtUp` |
| `:488` | `: "对面退出了";` | `St/PeerLeft` |
| `:560`（3 行） | `"对局中" + (Host ? "（本机是主机，动作由本机定序）" : "（客机：操作由主机确认）")` | `St/InBattle` + `St/HostSide` / `St/ClientSide` |
| `:585` | `reason ?? "未连接"` | `St/Off`（**只换本机显示那一份**；走线那份见 ⑤·1） |

> ⚠️ **一处可见变化**：`:355` 与 `:181` 共用 `St/Handshaking`，而两处原文差一个前缀「连上了，」
> ⇒ 落键后 `:355`**也跟着带上了那个前缀**（施工单 §② 第 41 行已预告这件事，`Core/Loc.cs:1401-1402` 也记着）。
> 已在 `:355` 上面就地标注。

#### `Net/NetMatchmaking.cs`（18 处 · 施工单 §② 第 12～21 行）

| 文件:行（改后） | 改前字面量 | 改成的键 |
|---|---|---|
| `:62` | `why = "联机没连上（这一局本来就没走联机）"` | `Cancel/WhyNoLink` |
| `:63` | `why = "这一局还没进入联机匹配"` | `Cancel/WhyNotMatching` |
| `:67` | `why = "这一局**已经开局了**（…）"` | `Cancel/WhyStarted` |
| `:197`（4 行 → 2 行） | 提示行 `"对面掉线了，" + tail + "（两边回来各点一次 \`Battle!\`）"` + 弹窗 3 行拼串 | `Lobby/PeerLostHint` / `Lobby/PeerLost`（**都是 `string.Format(…, tail)`**） |
| `:207` | `string.IsNullOrEmpty(why) ? "对面退出了" : why` | `St/PeerLeft`（**§② 没列、我补的**，见 ③·4） |
| `:210`（4 行 → 2 行） | 提示行 `"联机结束：" + body + " —— " + tail` + 弹窗 3 行拼串 | `Lobby/PeerLeftHint` / `Lobby/PeerLeft` |
| `:229` | `what + " —— 已开局、正在进战场（后面由对局那一层说）"` | `Lobby/DeferToBattle` |
| `:245` | `had ? "这一局的匹配已经撤销" : "（本机本来就没在匹配这一局）"` | `Lobby/MatchRevoked` / `Lobby/NotMatchingThisGame` |
| `:356` | `why = "联机没连上"` | `Lobby/BotNoLink` |
| `:358` | `why = $"联机会话现在是 \`{s.State}\`（还没握手完）"` | `Lobby/BotSessionNotReady` |
| `:359` | `why = "这副牌是空的"` | `Lobby/BotEmptyDeck` |
| `:398`（5 行 → 4 行） | `"这一局**打的是电脑，不是联机**。\n原因：" + why + … + (Host ? "【保存】" : "【检查连接】") + …` | `Lobby/PlayedVsBot`（`{1}` = `"【" + Loc.T("Settings/Online/{Save,CheckConnection}") + "】"`） |
| `:422` | `"对面回来了 —— 联机已恢复。…"` | `Lobby/LobbyRestored` |
| `:461`（2 行） | `"对面在你取消之后开局了 —— …"` | `Lobby/StartAfterCancel` |
| `:496`（2 行） | `"对面在你开局之后才点了取消 —— …"` | `Lobby/MissedCancel` |
| `:502`（2 行） | `"对面取消了这一局的匹配 —— …"` | `Lobby/PeerCancelled` |
| `:531`（2 行） | `"两边选的模式不一样：本机是「" + mode + "」，对面是「" + _foeMode + "」。…"` | `Lobby/ModeMismatch` |
| `:578` | `"开局参数没能解析出来，这一局开不了。…"` | `Lobby/StartParseFailed` |

> ⛔ **`NetMatchmaking.cs:184` 的 `DeferToBattleLayer("对面掉线了")` 与 `:208` 的 `"对面离开了："` 没动** —— 见 ⑤·3。

#### `Net/NetTransport.cs`（16 行 / 15 条 · 施工单 §② 第 22/23/24/25 行）

| 文件:行（改后） | 改前字面量 | 改成的键 |
|---|---|---|
| `:176` / `:182` | `(dualStack ? "（双栈）" : "（仅 IPv4）")` | `St/StackDual` / `St/StackV4Only` |
| `:210` | `_lastError = "没有填 IP 地址"` | `St/NoIp` |
| `:226` | `$"连接 {host}:{port} 超时（{timeoutMs} 毫秒）—— …"` | `St/ConnectTimeout` |
| `:282` | `"连上了但拿不到流：" + e.Message` | `St/NoStream` |
| `:307` / `:316` | `Fail("读取中断：" + e.Message)`（**同一行两处，一起换**） | `St/ReadAbort` |
| `:308` / `:317` | `Fail("对面关掉了连接")`（**同一行两处，一起换**） | `St/PeerClosed` |
| `:311` | `Fail($"帧长度不合理（{len} 字节）—— …")` | `St/BadFrame` |
| `:319` | `Fail("收到的帧解不出信封")` | `St/BadEnvelope` |
| `:372` | `_lastError = "还没连上，发不出去"` | `St/NotConnected` |
| `:393` | `Fail("发送失败：" + e.Message)` | `St/SendFailed` |
| `:420` | `$"端口 {port} 已被占用 —— …"` | `St/PortBusy` |
| `:422` | `$"对面拒绝了连接（{port} 端口没人在听）—— …"` | `St/ConnRefused` |
| `:424` | `"这个 IP 地址找不到——检查一下有没有抄错"` | `St/HostNotFound` |
| `:425` | `"网络错误：" + se.SocketErrorCode` | `St/SocketError` |

#### `Net/NetConfig.cs`（2 处 · 施工单 §② 第 26 行）

| 文件:行（改后） | 改前字面量 | 改成的键 |
|---|---|---|
| `:303` | `"两个方向都没探到 —— **可能是回显站被网络挡了**（…）。"` | `Echo/NoEcho` |
| `:307` | `"探测出错：" + e.Message` | `Echo/ProbeError` |

#### `Net/UpnpPortMapper.cs`（9 处 · 施工单 §② 第 27 行）

| 文件:行（改后） | 改前字面量 | 改成的键 |
|---|---|---|
| `:108` | `"上一次「向路由器要端口」还没跑完 —— 这次先跳过。"` | `Upnp/Busy` |
| `:122` | `"向路由器要端口时出错：" + e.Message` | `Upnp/Error` |
| `:164`（3 行） | `"没能从路由器那里问到端口映射…"` | `Upnp/NoResponse` |
| `:176`（2 行） | `"路由器回应了，但它**没有提供端口映射服务**…"` | `Upnp/NoService` |
| `:213`（2 行） | `"路由器说 **" + port + " 这个端口上已经有别的映射了** ⇒ …"` | `Upnp/PortTaken` |
| `:215` | `"路由器不接受「永久」映射（错误码 725）…"` | `Upnp/NotPermitted` |
| `:217`（2 行） | `"路由器**拒绝了**端口映射请求（错误码 " + code + "）。…"` | `Upnp/Rejected` |
| `:225`（3 行） | `"✅ 端口映射要到了，**但你这台大概率在「大内网」(CGNAT) 里**…"` | `Upnp/Cgnat` |
| `:230`（+4 行注释 → 3 行） | `"✅ 已经在路由器上开好了 " + port + " 端口（TCP）" + ("，你家的外网地址是 " + wan) + " —— …"` | `Upnp/Ok`（`{1}` = **仍是中文小段** ⇒ 见 ⑤·4） |

### 2·B 断言（14 条 · 全在 `Editor/NetSelfTest.cs`）

| # | 文件:行（改后） | 改前 | 改后 |
|---|---|---|---|
| 1 | `NetSelfTest.cs:25` 之后（+11 行） | ——（原来没有） | **新增 `static string TermHead(string key)`**（形状逐字照 `Editor/SettingsScene.cs:240`）；断言锚 |
| 2 | `:65` 之后（+1 行） | ——（原来没有） | `Run()` 里加一句 `TestNetTermBilingual();` |
| 3 | `:85` 之前（+108 行） | ——（原来没有） | **新增 `static void TestNetTermBilingual()`**：79 条键 · `Loc.HasEntry` 全中（Eq 一条）· C2（`Loc.T(键) != 键`）· C1（`!Loc.HasCjk(Loc.EnOf(键))`）· 两语档取值**不同且都非空** · 语言**逐值放回** · **两条提示行词条的 ZH 长度 ≤ `SearchingMatchPopup.HintLineMaxChars`(40)** |
| 4 | `:235`（B③） | `b.LastError.Contains("占用")` | `Contains(string.Format(Loc.T("…St/PortBusy"), p))` **且** 前缀是 `St/StackDual` 或 `St/StackV4Only`（钉 3 条键） |
| 5 | `:286`（D③） | `checkWhy.Contains("密码")` | `checkWhy.Contains(Loc.T("…St/WrongPassword"))` |
| 6 | `:311`（E④） | `host.LastError.Contains("版本")` | `Contains(string.Format(Loc.T("…St/VersionMismatch"), NetProtocol.Version + 1, NetProtocol.Version))` |
| 7 | `:477`（J③） | `got[0].Contains("打的是电脑")` | `Contains(TermHead("…Lobby/PlayedVsBot"))` **且** `Contains(Loc.T("Settings/Online/Save"))`（后者钉 `{1}` 真的被替换） |
| 8 | `:489`（J④） | `got2[0].Contains("检查连接")` | `Contains(Loc.T("Settings/Online/CheckConnection"))` |
| 9 | `:547`（L⑨） | `n1[0].Contains("对面取消了")` | `n1[0] == Loc.T("…Lobby/PeerCancelled")`（**逐字**） |
| 10 | `:565`（L⑭） | `w2.Contains("已经开局")` | `w2.Contains(Loc.T("…Cancel/WhyStarted"))` |
| 11 | `:688`（M⑦） | `n1[0].Contains("断开")` | `Contains(TermHead("…Lobby/PeerLost"))` **且** `Contains(Loc.T("…Lobby/MatchRevoked"))` |
| 12 | `:693`（M⑧） | `LastHint.Contains("掉线")` | `LastHint.Contains(TermHead("…Lobby/PeerLostHint"))` |
| 13 | `:713`（M⑭） | `LastHint.Contains("回来")` | `LastHint.Contains(TermHead("…Lobby/LobbyRestored"))` |
| 14 | `:907`（N④） | `LastHint.Contains("掉线")` | 同 #12（那一格同时断「`OnHint` 推来的 == `LastHint`」） |

> ⛔ **`NetSelfTest.cs:596`（M⑳，`Contains("自检：对面离开了这一局")`）没动** —— 那个串是**自检自己在 `:590`
> 传给 `cli.Close(true, …)` 的**，经 `:579` 走线原样回显（`查证_P6d` §2·D 第 2 行同判：绿）。见 ③·3。
> ⛔ **`:1225/:1250/:1441/:1456/:1474` 五条没动** —— 它们断的是 **`Debug.Log` 的正文**（②类，本轮不翻）⇒ **不会红**。

---

## ③ 证据

1. **逐处键名的出处** = `施工单_双语_Net整片_P6.md` §② 那张表的对应行（上表每节标题都标了「第 N 行」）。
   **键本身**逐条现读于 `Core/Loc.cs`：`⑬·A St/*`(`:1406-1475`) · `⑬·B Lobby/*`(`:1483-1512`) ·
   `⑬·C Cancel/*`(`:1516-1521`) · `⑬·D Echo/*`(`:1525-1527`) · `⑬·E Upnp/*`(`:1537-1554`) · `MainMenu/General/OK`(`:997`)。
2. **键存在性（机械核过一遍，不是眼看）**：把本批 79 条键列成清单、对 `Core/Loc.cs` 的 `new Entry(` 逐键抽两列
   ⇒ **缺键 0 · EN 列为空 0 · EN 列含汉字 0**（这把新加的那三条 C1/C2/两语档断言**不会一上来就红**）。
3. **`NetBattleTest.cs` 为什么不用改（逐条核过）**：它那 11 条中文子串断言的**取值来源**是
   `:630/:1105` `DrainNoticesForTest()` ← `NetBattle.SayPeerGone`（`NetBattle.cs:385-389`，**未改**）·
   `:632/:638/:655/:884/:893/:901/:1107/:1193/:1195` `BareHost.LastSay` ← `NetBattle.NetSay`（**未改**）·
   `:652` `Contains("离开")` ← **自检自己**在 `:645` 传的 `"对面离开了这一局"` ·
   `:994` `StatusText.Contains("这一局的联机房间已经散了")` ← `NetBattle.cs:789` 传给 `Close` 的**字面量**（**未改**）。
   ⇒ **P6 换键一条都带不到它**。（`查证_P6d` §2·D 也判 `:994` 是「改 `NetBattle.cs` 才红」，那是 **P6d** 的账。）
4. **超出 §② 的两笔**（都用了**现成的键**，没造键）：
   · `NetMatchmaking.cs:207` `"对面退出了"` → `St/PeerLeft`（**值与键 ZH 列逐字相同**；它是 `PeerLeftHint`/`PeerLeft` 的 `{0}`，**玩家看得见**，§② 漏列）；
   · `NetSelfTest.cs` 新增的 bilingual 一节（§⑤ 要求的「三条必备 + 灭自证 C1/C2」，§② 不管断言）。
5. **行尾判定（二进制读数的两个数）**：7 份**改前 / 改后**都是 `CRLF 0`（见 ④）⇒ 一个行尾都没翻；⛔ 全程用 python `wb`，**没用过 `sed -i`**。

---

## ④ 验证

### 4·A 秒级类型检查（`TMPDIR=/tmp/wf_net bash d:/4/Unity/工具/typecheck.sh`，原样两行）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⚠️ 这次没出现「错全在别人正在写的文件上」那种情形（0 错，无需分辨归属）。

### 4·B 行尾（`io.open(...,'rb')` 数出来）

| 文件 | 改前 CRLF / LF | 改后 CRLF / LF |
|---|---|---|
| `Net/NetSession.cs` | 0 / 665 | **0 / 677** |
| `Net/NetRuntime.cs` | 0 / 195 | **0 / 198** |
| `Net/NetMatchmaking.cs` | 0 / 612 | **0 / 603** |
| `Net/NetTransport.cs` | 0 / 430 | **0 / 430** |
| `Net/NetConfig.cs` | 0 / 373 | **0 / 373** |
| `Net/UpnpPortMapper.cs` | 0 / 486 | **0 / 482** |
| `Editor/NetSelfTest.cs` | 0 / 1632 | **0 / 1789** |

### 4·C `git diff --numstat`（只读，没动 git）

```
180	23	Unity/MyGame/Assets/CardPresentation/Editor/NetSelfTest.cs
2	2	Unity/MyGame/Assets/CardPresentation/Net/NetConfig.cs
23	32	Unity/MyGame/Assets/CardPresentation/Net/NetMatchmaking.cs
5	2	Unity/MyGame/Assets/CardPresentation/Net/NetRuntime.cs
42	30	Unity/MyGame/Assets/CardPresentation/Net/NetSession.cs
17	17	Unity/MyGame/Assets/CardPresentation/Net/NetTransport.cs
14	18	Unity/MyGame/Assets/CardPresentation/Net/UpnpPortMapper.cs
```

（判据：数字远小于文件行数 ⇒ 不是整篇重写。`NetSelfTest.cs` 那 180/23 里 **108 行**是新方法本体、
**11 行**是新 helper；纯断言改动只占其余那部分。`NetBattleTest.cs` **不在表里** = 一个字节没动。）

### 4·D 收尾复查（机械）

- 再跑一遍「中文串字面量」扫描（剥掉整行注释）：6 份里**剩下的中文**只有
  ① `Debug.Log*` 正文（②类，可以留）② 简报圈定的走线串里属于本白的 5 处
  （`NetSession.cs:444` `:452` `:471` `:579` 的 `bye` 理由 + `NetRuntime.cs:182`）③ **3 处无键可接的**（⑤·2/⑤·3/⑤·4）。
- 这 6 份里 `Loc.T(` 的出现次数 = **89**（= 77 个改动位置；`:176`/`:182`/`:197`/`:209`/`:212`/`:249`/`:294`/`:402`/`:560`
  这 9 处**一条语句里查 2～3 条键**，所以次数比处数多）。
  逐份：`NetSession` 34 · `NetMatchmaking` 23 · `NetTransport` 19 · `UpnpPortMapper` 9 · `NetRuntime` 2 · `NetConfig` 2。

---

## ⑤ 没查清 / 停手的部分（逐条：卡在哪、还差什么）

1. 🔴 **`NetSession.cs` 的 `why` 是「本机显示 + `MsgAck.reason` 走线」双重载荷 —— 我按 §② 落了键，但请裁定。**
   - 「简报说什么」：`NetRuntime.cs:179` 与 `NetSession` 的 `bye` 那 4 处 = **7 处走线文案，本次一个字不许碰**（还点了 `ack`/`reject`）。
   - 「实际是什么」：**`:429` 那个 `Send(NetKind.Ack, new MsgAck { ok = why == null, reason = why ?? "" })`**
     收的就是 `:371/:373/:377` 那三句 —— 所以 BadHello / VersionMismatch / WrongPassword **同时过 TCP**（主机→客机）；
     客户机侧 `checkWhy` 就是它（`Editor/NetSelfTest.cs` D③ 断的那个值），设置窗 `_flash` 印它。
   - 「我为什么还是接了」：① `施工单 §②` 把 `:361/:363/:367` 判成 **①** 并给了键；② `Core/Loc.cs:1398` 那三条键的
     **出处注释逐字写着就是这三行**（不接 = 三条键变死键）；③ `施工单 §⑤` 把 `NetSelfTest.cs:150 "密码"`、
     `:173 "版本"` **列进「换键后必红、必须同批改」**的名单 —— 不换它那两条根本不会红。
   - 「证据」：`资料/普查产出_第六会话/查证_P6d协议兼容面.md` §2·B **正是**把这一处列成「7 处之外、同样过 TCP 的中文串
     （简报漏了）」，§⑤·4 明写「**要不要同批改 —— 简报没列，我判不了**」，§2·D 也预告「E④ ✅ 绿 **除非 Ack 族也一并改**」。
   - **落地**：已在 `NetSession.cs:364` 上面写了 6 行 ⚠️ 注释把这件事标在代码里。**要改口径 = 回退这 3 行 + D③/E④ 两条断言**（4 个 hunk，10 分钟内）。
     ⛔ 需要主对话裁；我**没有**在别处另开协议改动（`NetProtocol.cs` / `NetBattle.cs` 一个字节没动）。
2. 🔴 **`NetSession.cs:424` `"连接成功 —— 可以直接开战了"` 接不了 —— 表里没有键。**
   它经 `OnCheckDone(true, why)` 进 `Shell/SettingsWindow.cs:2882` 的 `_flash`（`"✅ " + why`）= **玩家看得见**。
   搜过 `Core/Loc.cs` 全表：`连接成功` / `可以直接开战` / `CheckOk` **全部 0 命中**；
   `Settings/Online/HostReady`（「✅ 主机已就绪，等着对面连进来。」）**语义是主机那一侧**，不能顶包。
   ⇒ 按简报「不许自造键、也不许把那句删掉」**原样留着**，写进本报告。
3. 🔴 **`NetMatchmaking.cs:184` / `:208` 的 `what` 碎片接不了 —— 表里没有键。**
   `DeferToBattleLayer(what)` 里的 `what` 只有两个来源：`:184 "对面掉线了"`、`:208 "对面离开了：" + body`；
   它们进 `Lobby/DeferToBattle` 的 `{0}`，**上的是那一行 40 字提示**（玩家看得见）。
   搜过 `Core/Loc.cs`：**裸句「对面掉线了」/「对面离开了：」都没有独立键**（`Lobby/PeerLostHint` 里那句是
   「对面掉线了，{0}…」整句、不能拆；`St/PeerLeft` =「对面退出了」语义不同）。
   ⇒ **英文档下这一格会中英混**（`what` 中文 + 后半句英文）。**如实报，等主对话裁**（要修就得建键，我不能自造）。
   ⚠️ 顺带：`查证_P6d` §⑤·3 正拿这一格算 40 字预算（「取词在拼句前还是后 ⇒ 38 或 59」），**与本条同源**，一并交给它裁更省事。
4. 🔴 **`UpnpPortMapper.cs:235` `Upnp/Ok` 的 `{1}` 碎片接不了 —— 表里没有键。**
   `{1}` = 「，你家的外网地址是 X」（没探到外网地址时是空串）。搜过 `Core/Loc.cs` 的「外网地址」「你家的」**0 命中**；
   `Settings/Online/PublicAddress/*` 那几条（「外网看到的地址（刚探的）：」…）**语义/形状都不是这一段**。
   ⇒ **那一小段如实留着中文**（英文档下这一句会中英混），已在 `:230` 上面写了 4 行注释标出。
5. ⚠️ **后台线程上新增了 `Loc.T` 调用（不是漏查，是取舍，如实标出）**：
   `NetTransport.ReadLoop`（经 `Fail`：`:307/:308/:311/:316/:317/:319`）与 `UpnpPortMapper.Map`（`:164-235`）、
   `NetConfig.ProbeExternalAsync` 的线程体（`:303/:307`）。
   - 判据：`Loc.T` 只在**首次**取值时经 `Loc.Current → Load()` 读一次 `PlayerPrefs`（`Core/Loc.cs:1820-1826`，有 `_loaded` 幂等闸）；
     生产路上「设置窗→联机页」早就用 `Loc.T` 在主线程载过表，自检路上 `TestPortBusy` 的 `StartHost`（主线程）也会先载
     （且 `UpnpPortMapper.MapAsync` 在批处理里**第一句就返回**、根本不建线程）⇒ 实际不会在后台线程上碰 `PlayerPrefs`。
   - 同族的既成事实：那两个线程**本来就在调 `Debug.Log`**（`NetTransport.cs:376`、`UpnpPortMapper.cs:126`）与
     `NetRuntime.Notice`（那边注释逐字写着「也会从后台线程调」）。
   - ⛔ 我**没有**加「预热」代码（那要多改 3 处、且属于新口径）；**如实报出来由主对话判断要不要加**。
6. ⚠️ **`NetSelfTest.cs:1096` 那句 `string hint = "对面掉线了，这一局的匹配已经撤销（两边回来各点一次 Battle!）";` 没动** ——
   它是 N⑤–N⑧ 那一格**自检自己喂给 `SearchingMatchPopup.ShowHint` 的夹具串**（不是生产文案、也不被断成期望值），
   留着 = 那一格只验「窗画得出/收得回」，不掺语档。**如实报**（要不要改成 `Loc.T` 拼接由主对话定）。

---

## ⑥ 顺手发现的（⛔ 我一个字都没改）

1. 🔴 **`NetRuntime.cs:182` 的措辞在客机那条路上是错的**（与 `查证_P6d` §⑥·3 同一条，**独立复核成立**）：
   `Reset()` 被 `Shell/SettingsWindow.cs:2843`（主机【保存】）与 `:2878`（客机【检查连接】）**两边都调**，
   而它给对面捎的是「**主机**重开了」⇒ 客机点一下【检查连接】就会把一句**语义颠倒**的话送到主机屏幕上。
   ⛔ 它在 7 处走线文案里 ⇒ **本次不许碰**；记在这里给 P6d（`NetBattle.cs:784-788` 有同族的 R7 中性化纪律）。
2. ⚠️ **`NetSelfTest.cs` 里 `NetMatchmaking` 那两句注释的行号已漂**（未改，只报）：
   `NetMatchmaking.cs:224` 一带的注释写「`:184` 的「对面掉线了」= 5 · `:199` 的「对面离开了：」」——
   现读 `:208`（不是我改出来的：我改的是 `:197` 那一段汇编、`:184` 本身没动，行号漂是**我上面那些单行替换**造成的）。
3. ⚠️ **同一族的老记录**：`Net/NetBattle.cs:604/615`（「本批没有接 `Loc.T`」）vs `Core/Loc.cs:1648-1650`
   （「这 7 条会经过网络…P6d 单开」）—— `查证_P6d` §⑥·2 已报「两处口径打架」，**我复核成立、没动**。
4. 📌 **`Editor/NetSelfTest.cs` 现在有 79 条键的「本批键清单」**（新方法里那张数组）——
   将来 `Settings/Online/St/*` 或 `Lobby/*` 再加键、而这里没跟，**那一格不会自己变红**（只有「在表里」那条会）。
   已在新方法的 doc 里写明「少一条就等于少一格的判据」；要不要做成「与 `Loc.cs` 该族条数对表」的硬断言，**交主对话裁**。
5. 📌 **`Settings/Online/St/PeerLeft` 现在有两个消费点**（`NetSession.cs:488` 的 Ack/Bye 兜底 + `NetMatchmaking.cs:207` 的 `body` 兜底），
   与 `Core/Loc.cs:195`「共用键：值一改两边同时变」那条纪律同族 —— 只报，没动值。

---

## 摘要（≤300 字，给用户看的那一段）

P6 的 `Net/` 整片接线做完了。生产代码 6 份共落键 **77 处**，全部用**现成的键**、一条没自造；
`NetRuntime` 那两颗弹窗钮换成原版键 `MainMenu/General/OK`（**中文档从「知道了」变成「确定」**，已标出）。
旧断言同批改了 **11 条**（都在 `NetSelfTest`），改成随语档（断 `Loc.T(键)` / 新增的 `TermHead(键)` 前缀），
另补了一个新方法：79 条键「都在表里 + 英文列无汉字 + 两语档取值不同 + 两条提示行中文 ≤ 40 字」。
`NetBattleTest` **一个字不用改**（它那 11 条断的全是 `NetBattle.cs` 或自检自己喂的串，逐条核过）。
秒级类型检查 **0 错**；行尾 CRLF 全程 0，`git diff --numstat` 是小数字（不是重写）。

**三处接不了、留着没动**（表里真没有对应的键，按规矩不自造）：`NetSession.cs:424`「连接成功…」、
`NetMatchmaking.cs:184/:208` 的「对面掉线了」/「对面离开了：」碎片、`UpnpPortMapper.cs:235` 的
「，你家的外网地址是 X」。**一处要你裁**：`NetSession` 那三句握手失败理由（密码/版本/握手包）
**同时是 `MsgAck.reason` 的走线串** —— 我按施工单 §② 落了键并在代码里标了 ⚠️，要改口径回退 4 个 hunk 即可。
