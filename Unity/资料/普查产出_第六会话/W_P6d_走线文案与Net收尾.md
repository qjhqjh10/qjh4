# W_P6d · 走线文案与 `Net/` 收尾（动手写手交件 · 第六会话 · 2026-10-19）

> 范围 = **一笔**：`A1038`（7 处走线文案）· `A1081`（握手失败那 3 条的走线那一半）·
> `A1079`（3 处接线后仍剩的玩家可见中文）· `A1080`（后台线程上的 `Loc.T`，**只读核**）。
> ⛔ **没跑 Unity**（用户口径：待办没做完之前不跑自检）；✅ 秒级类型检查跑了两趟（0 错）。
> ⚠️ 简报里的行号**全部已漂**（上一批写手在后）⇒ 本件一律**按现读定位 + 按符号名索引**。

---

## ① 结论（四笔各一句）

1. **`A1038`（7 处走线文案）**：✅ **全部接完** —— 线上现在发的是**词条键**（要带参数走 `键|参1|参2`），
   收侧在**四条收包路**上 `NetWireText.Unpack` 按**本机语言**取词；`NetSession.Close` 的 `reason`
   语义已从「一句现成的文案」改成「词条键」（本机那半也走同一个键，⛔ 不留裸串）。
2. **`A1081`（`BadHello`/`VersionMismatch`/`WrongPassword`）**：✅ **走线那一半接完**，
   裁定 = **共用 `St/*`**（⛔ 没另开 `Wire/*`）—— 判据见 §③·2。
3. **`A1079`（3 处残留）**：✅ **三处全接上了**（都不是停手）—— 新键 4 条 + 顺手第 8 条 `Wire/*`，
   两处「碎片 vs 改父键形状」的选择 = **碎片建成独立键**（代价只落在自己这一格），理由见 §③·3。
4. **`A1080`（后台线程 `Loc.T`）**：✅ **只读核完 ⇒ 【核不出洞】，一行都没加** ——
   「半张表」不存在（CLR 静态构造保证），且**没有任何可达路径**让后台线程先到 `Loc.Load()`
   （逐条证据与那两个「窄口子」见 §⑤）。

---

## ② 逐处表

### 2·A `A1038` —— 7 处走线点（现读行号 | 消息/字段 | 原来发什么 | 改成什么 | 方向）

| `文件:现读行号` | 消息 / 字段 | 原来发什么 | 改成什么 | 方向 |
|---|---|---|---|---|
| `Net/NetSession.cs:474` | `MsgBye.reason`（`case NetKind.Reconnect`，主机收坏 token） | 「这把钥匙对不上这一局」 | `NetWireText.Pack("Settings/Online/Wire/BadKey")` | 主机→客机 |
| `Net/NetSession.cs:482` | `MsgBye.reason`（同一 case，没挂 `ResumeProvider`） | 「主机这边没有这一局的记录」 | `…Wire/NoRecord` | 主机→客机 |
| `Net/NetSession.cs:504` | `Close(true, …)` ⇒ 经 `Close` 拼进 `MsgBye.reason`（`case NetKind.Resume`） | 「重连包解不出来」 | `…Wire/BadResume` | 🔴 **客机→主机**（唯一反向） |
| `Net/NetSession.cs:629` | `MsgBye.reason` = `key`（`Close(say,reason)` 的兜底） | 「对面结束了这一局」 | `…Wire/PeerDone`（**只兜底那一份**；本机状态字**仍是** `St/Off`） | 两端（看调用方） |
| `Net/NetBattle.cs:793` | `_s.Close(true, …)` ⇒ `MsgBye.reason` | 「这一局的联机房间已经散了」 | `…Wire/RoomGone` | 主机→客机 |
| `Net/NetBattle.cs:1006` | **`MsgReject.reason`** | 「不是你的回合」 | `…Wire/NotYourTurn`（`Pack` 无参） | 主机→客机 |
| `Net/NetRuntime.cs:187` | `Session.Close(true, …)` ⇒ `MsgBye.reason` | 「主机重开了」 | `…Wire/HostRestarted`（**措辞已中性化**，见 2·D） | 两端（`Reset()` 主/客都调） |
| 🆕 `Battle/BattleDriver.cs:6158` | `s.Close(true, …)`（`LeaveNetRoom()`） | 「对面离开了这一局」 | `…Wire/PeerLeftMatch`（**本族第 8 条**，见 2·D） | 两端 |

**收侧（4 条路，次序一律 = 先 `ClampPeerText` 再 `Unpack`）**：

| 收包路 | 现读行号 | 原来印什么 | 现在印什么 |
|---|---|---|---|
| `case NetKind.Bye` | `Net/NetSession.cs:522-523` | `ClampPeerText(m.reason)` | `NetWireText.Unpack(ClampPeerText(m.reason))` |
| `case NetKind.Ack` | `Net/NetSession.cs:434-435` | `ClampPeerText(m.reason)` | 同上 |
| `case NetKind.Reject` | `Net/NetBattle.cs:1062` | `NetSession.ClampPeerText(m.reason)` | `NetWireText.Unpack(NetSession.ClampPeerText(m.reason))` |
| `case NetKind.Proof`（**发**侧，`MsgAck.reason`） | `Net/NetSession.cs:427` | 已渲染的 `why` | `NetWireText.Pack(whyKey, whyArgs)` |

### 2·B `A1081` —— 握手失败那 3 条（同时是本机显示 + `MsgAck.reason`）

| 键 | 共用/另开 | 线上发什么 | 本机印什么 | 收侧 |
|---|---|---|---|---|
| `Settings/Online/St/BadHello` | **共用 `St/*`** | `Pack(键)` | `Loc.T(键)` | `Unpack` → 本机语言 |
| `Settings/Online/St/VersionMismatch` | **共用 `St/*`** | `Pack(键, m.protoVer, NetProtocol.Version)` | `string.Format(Loc.T(键), args)` | `Unpack` → `string.Format(Loc.T(键), args)` |
| `Settings/Online/St/WrongPassword` | **共用 `St/*`** | `Pack(键)` | `Loc.T(键)` | 同上 |

- `Send(NetKind.Ack, …)` 现在发 `reason = whyKey == null ? "" : Pack(whyKey, whyArgs)`，
  `ok` 判据也换成 `whyKey == null`（原来靠 `why == null`）—— 语义等价，但**本机渲染与走线载荷已分离**。
- ✅ `Loc.cs:1664-…`（`Wire/*` 那节）里「`St/*` 与 `Wire/*` 是两族」那句**已就地订正**（铁律 5，留了订正痕）。

### 2·C `A1079` —— 3 处残留（现读行号 | 原来 | 改成）

| `文件:现读行号` | 原来是什么 | 改成什么 | 形状选择 |
|---|---|---|---|
| `Net/NetSession.cs:450` | 裸串「连接成功 —— 可以直接开战了」（→ `OnCheckDone` → 设置窗 `_flash`） | `Loc.T("Settings/Online/St/CheckOk")` | 新建独立键（它**不是**碎片） |
| `Net/NetMatchmaking.cs:188` | 裸串「对面掉线了」（喂 `Lobby/DeferToBattle` 的 `{0}`） | `Loc.T("Settings/Online/Lobby/PeerLostFrag")` | **碎片建成独立键** |
| `Net/NetMatchmaking.cs:215` | 裸串「对面离开了：」（同上） | `Loc.T("Settings/Online/Lobby/PeerLeftFrag") + body` | **碎片建成独立键**（取词在拼句**之前**） |
| `Net/UpnpPortMapper.cs:245` | 裸串「，你家的外网地址是 」+ wan（喂 `Upnp/Ok` 的 `{1}`） | `string.Format(Loc.T("Settings/Online/Upnp/OkWanSuffix"), wan)` | **碎片建成独立键** |

### 2·D 顺手改掉的口径（都在白名单内）

| 项 | 原来 | 现在 | 为什么 |
|---|---|---|---|
| `Wire/HostRestarted` 的 ZH/EN | 「主机重开了」/ `Host restarted` | 「对面重开了联机」/ `The opponent restarted the session` | 🔴 **原措辞在客机那条路上是语义颠倒的**：`Reset()` 被【保存】（主机）与【检查连接】（**客机**）都调过，客机捎给主机的是一句说「主机」重开了的话。改后对**收方**恒成立（同 `NetBattle` 那条 R7 中性化纪律）。⚠️ 这是本族**唯一一条 ZH 列 ≠ 调用点原文**的，如实标出 |
| `Battle/BattleDriver.LeaveNetRoom()` 的键 | 无（裸串「对面离开了这一局」） | 🆕 `Wire/PeerLeftMatch`（**本族第 8 条**） | ⛔ **没有并进 `Wire/PeerDone`**（那一条是「对面结束了这一局」）—— 并过去 = **悄悄改掉这一跳的措辞**；本族「一说话一条键」只多一行，可见文案**零变化** |
| `NetBattle.cs` 那段清单注释 | 标题句 = 「本文件面向玩家的中文硬串**【全部】**在这里，**一条都不接** `Loc.T`」+ 两处写死的行号（`:749`/`:952`，**已漂**） | 改成「下面那批仍然不接（`Wire` 两条**除外**）」+ **按符号名索引**（`ReconnectCountdownExpired` / `Dispatch` 的 `case NetKind.Action`）+ 逐条订正痕 | 原句**已经不成立**；行号是 2026-10-18 读数、本轮现读就对不上 |
| `NetBattle.cs` 里 `:952` 那两条「仍然不接」的判据 | ①「wire 载荷 ⇒ 接 `Loc.T` 会把发送方语言灌到接收方」= 不接的理由 | ①**不再是不接的理由**（那正是本件要修的病；解法 = 线上发**语言无关的键**、收侧取词）；②「原版那条是 HUD 提示行、不是拒绝码」**照旧成立** ⇒ 用**自拟的 `Wire/*`**，⛔ **没复用** `Battle/Tips/NotYourTurn` | 主对话 2026-10-19 已裁 |
| `NetProtocol.MaxPeerTextChars` 的 doc | 「三个入口」+ 写死行号（`:428`/`:336`/`:362`，**全漂**） | 「**四个**入口」（补上 `MsgReject.reason`）+ **符号名索引** + 补一句「先钳后取词」的次序 | 铁律 5 |
| `NetMatchmaking.DeferToBattleLayer` 的 40 字预算账 | 「两个来源：`:184`…·`:199`…」 | 改成按**函数名**引 + 现核「两个数都不变（本轮实测仍 5/6 字 ⇒ 最长 38 ≤ 40）」+ 如实记下「**英文档本来就超**」（模板 86 字，接线前就这样） | 行号已漂；且本件改动**没有**让它变长 |

---

## ③ 三个裁定与依据

### 3·1 协议编码 & 兼容面（照主对话三条裁定执行，⛔ 一条没翻）

- **①** **不 + 协议版本号**（`NetProtocol.Version` 仍是 **2**）。收侧一律
  `Loc.HasEntry(x) ? Loc.T(x) : x`（**由 `NetWireText.Unpack` 一处实现**）——
  旧端收新键 ⇒ **印键名**（7+1 条键 27~34 字，⛔ **连 `ClampPeerText` 都不会截**）、不崩、不忽略；
  新端收旧中文串 ⇒ **原样回显**（白赚）。⛔ 没改成「不认识的串落固定本地句」。
- **②** **取词在拼句【之前】**：`NetMatchmaking.HandleLobbyPeerClosed` 里 `body` 是**已取词**的
  （`NetSession` 的收包段就 `Unpack` 了），再拼 `Lobby/PeerLeftFrag` ⇒ `what` = 14 字 ⇒ **38 ≤ 40** ✅
  （若把取词推到拼句之后 = 6 + 29 = 35 ⇒ **59 > 40** ❌，每次都 `LogWarning`）。
- **③** `Close(bool say, string reason)` 的 `reason` = **词条键**：
  · 线上发 `reason` 本身（`MsgBye.reason`）；· 本机 `SetState(Off, NetWireText.Unpack(reason))`。
  ⚠️ **一处必须记牢**：`reason == null` 时**线上兜底**是 `Wire/PeerDone`，而**本机状态字仍是**
  `St/Off`（「未连接」）—— 这两半**不能并成一个 `key`**（并了会让 `Close(false)` 这条常用路上
  状态字变成「对面结束了这一局」）。代码里留了硬注释。

**编码格式**：`键` 或 `键 + '|' + 参数1 + '|' + 参数2`。分隔符取 `|`（ASCII 可打印 ⇒ `JsonUtility` 不动它；
词条键全是 `[A-Za-z/]`、不可能含它）。
**次序硬**：**先 `ClampPeerText`（钳对端可控的原串）→ 再 `Unpack`（取词）**。反过来的话，
40 字钳的是**我们自己的文案**（`St/VersionMismatch` 的英文列 95 字会当场被截）。
**健壮性**：`Unpack` 把 `string.Format` 包了 `try/catch (FormatException)` —— 参数个数对不上
（对面伪造 / 被钳断）⇒ 退回模板 + `LogWarning`，⛔ **不让异常抛进收包路径**（会打死线程且一声不响）。

### 3·2 `A1081` 为什么**共用 `St/*`** 而不是另开 `Wire/*`

判据（写成了 `Loc.cs` 那节的新口径，**取代**原来那句「`St/*` 与 `Wire/*` 是两族」）：

> **分族的真正判据不是「本机显示 vs 发给对面」，而是「两个角色下要印的那句话措辞是否相同」**：
> **不同** ⇒ 两条键（`Wire/*` 存在的理由）；**相同** ⇒ **一条键两用**。

`St/{BadHello,VersionMismatch,WrongPassword}` 落在**第二类** —— 本机印「密码不对」，
对面（英文档）该印的也是「密码不对」的**它自己语言**版；而**走线上的是键、键是语言无关的**
⇒ 另开 `Wire/*` 只会多出一份**迟早会漂**的副本（铁律 6）。
反例（第一类）已经在本表里：`St/RejectBadKey`「重连被拒：钥匙对不上」是本机那句，
`Wire/BadKey`「这把钥匙对不上这一局」是发给对面那句 —— **措辞不同 ⇒ 两条键**。

### 3·3 `A1079` ②③ 为什么选「**碎片建成独立键**」而不是「改父键形状」

**只读核清的代价**（这是选路的依据）：
- **改父键形状**（把 `Lobby/DeferToBattle` 拆成两条整句键 / 把 `Upnp/Ok` 拆成「有外网地址 / 没有」两条）
  ⇒ 会动**已有断言**：`Editor/NetSelfTest.cs` 的键清单里**逐条列着** `Lobby/DeferToBattle` 与 `Upnp/Ok`，
  且 `DeferToBattle` 那条 40 字预算的账（`NetMatchmaking` 注释里逐字引它）也要重算 ⇒ **代价落在别的格上**。
- **碎片键** ⇒ 代价**只落在自己这一处**，而且 `{0}`/`{1}` 那个「有就填、没有就空串」的形状**原样保留**。
⇒ 选后者。三条碎片键：`Lobby/PeerLostFrag` · `Lobby/PeerLeftFrag` · `Upnp/OkWanSuffix`。

**原版两张表查证（A1079 三条新键 + `OkWanSuffix` + `PeerLeftMatch` 共 5 条，逐条都查过）**：
- 表① `d:/2/新解包资源/assets_full/**/MonoBehaviour/*.json` 的 `mTerm`：
  本轮**全库重扫了一次**（`rg -o '"mTerm"\s*:\s*"[^"]*"'` ⇒ **488 条唯一值**；
  前缀分布 `Demo 104 · MainMenu 83 · Event_Description 43 · MenuDeck 42 · Settings 34 · Battle 28 · …`）。
  按 `Connect|Lobby|Match|Disconnect|Opponent|Online|Address|Port|Success|Wait` **逐词搜过**：
  唯一沾边的是 `Battle/HUD/{LostConnectionMsg,PleaseWaitConnection,WaitOpponentConnectionMsg}`、
  `Battle/HUD/MatchAlreadyFinished`、`CustomErrors/{DuplicateConnection,ErrorSavingMatch}`、
  `Demo/DeckSelectionDemo/FewPlayersOnline` —— **`Settings/Online/*` 整个族 0 命中**（原版联机走 PlayFab）。
- 表② `d:/2/tools/il2cpp_out/stringliteral.json`（**26,507 条**）：按
  `连接成功/可以开战/外网地址/你家的/对面离开/对面掉线`（中）+ `Connected+battle`、`public address`、
  `Opponent disconnected`、`Opponent left`（英）**逐词搜过 ⇒ 全 0 命中**。
⇒ **键名 + 两列全自拟**（ZH 列 = 调用点原话逐字 ⇒ **中文档零变化**）。

### 3·4 断言面的改动

| 断言 | 改不改 | 为什么 |
|---|---|---|
| `Editor/NetBattleTest.cs`（原 `:994`，现 `:994` 附近） | 🔴 **必改，已改** | 原来断**写死的中文子串**「这一局的联机房间已经散了」⇒ 现在本机状态字 = `Loc.T(键)`，**英文档必红** ⇒ 改成断 `Loc.T("Settings/Online/Wire/RoomGone")` |
| `Editor/NetSelfTest.cs` M⑳（现 `:751`） | ✅ **不用改** | 它断的是**对面报的任意串**经收侧回显 —— 自检传的是 `"自检：对面离开了这一局"`（**不是键**）⇒ `HasEntry` 假 ⇒ **原样回显** ⇒ 绿。⛔ 这条恰好是「保留兜底回显」那半边的**活判据** |
| `Editor/NetBattleTest.cs:652` | ✅ **不用改** | 同 M⑳ 的条件（它传的是 `"对面离开了这一局"`，也不是键） |
| `Editor/NetSelfTest.cs` E④（现 `:315`） | ✅ **不用改** | 断的是 `host.LastError` = **发送侧本机显示**（`string.Format(Loc.T(St/VersionMismatch), 3, 2)`）⇒ 逐字不变 |
| `Editor/NetSelfTest.cs` D③（现 `:288`） | ✅ **不用改** | 断的是 `checkWhy` = **接收侧显示**（客机收 `MsgAck.reason` 后取词）⇒ 取出来的就是 `Loc.T(St/WrongPassword)`，逐字相同 |
| 🆕 `Editor/NetSelfTest.TestWireTextCodec()` | ✅ **新加**（4 条） | `Wire/*` 这一族**今天之前零条断言**（`grep "Online/Wire" Editor/*.cs` = 0）。四条：① 收侧印的是**词条值、不是键名**（`got == Loc.T(键) && got != 键` —— 只断 `== Loc.T` 会在「键没进表」时也绿）· ② **带参数**那条按收侧模板填回去 · ③ 参数个数对不上**不抛异常** · ④ 认不出的串**原样回显**（钉住主对话「⛔ 不许落固定本地句」那条） |
| `Editor/NetSelfTest` 键清单 | ✅ 已补 | 补进本件新接/新键的 **13 条**（`St/CheckOk` · `Upnp/OkWanSuffix` · `Lobby/{PeerLostFrag,PeerLeftFrag}` · `Wire/*` 8 条），让既有那条「键都在表里 + 英文列无汉字 + 两档取值不同」的静态判据**覆盖到它们**。⚠️ `St/{BadHello,VersionMismatch,WrongPassword}` 原来就在清单里（P6a 建的），⛔ 没重复 |

---

## ④ 验证

**秒级类型检查**（`TMPDIR=/tmp/wf_p6d bash d:/4/Unity/工具/typecheck.sh`，跑了两趟：改动中 / 收尾）：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

**行尾（改前 → 改后，二进制读数 `b.count(b'\r\n')` vs `b.count(b'\n')`）**：

| 文件 | 改前 | 改后 |
|---|---|---|
| `Net/NetProtocol.cs` · `NetSession` · `NetBattle` · `NetRuntime` · `NetMatchmaking` · `UpnpPortMapper` · `Core/Loc.cs` · `Editor/NetSelfTest` · `Editor/NetBattleTest` | `CRLF=0 / LF=n`（纯 LF） | **不变，仍 `CRLF=0`** |
| `Battle/BattleDriver.cs` | `CRLF=13884 / LF=13884` | **`CRLF=13890 / LF=13890`**（纯 CRLF，两个数**相等** ⇒ 没混行尾）—— 改这一处用的是 **python `rb`/`wb` + 自己写 `\r\n`**，⛔ 没碰 `sed -i` |

**`git diff --numstat`**（⚠️ 含**上一会话未提交的改动**，那不是同批写手 ⇒ 两列都列出来，方便区分）：

| 文件 | 本件开工时（上一会话留下的基线） | 现在 | 本件净增 |
|---|---|---|---|
| `Net/NetProtocol.cs` | 未改（0/0） | `72 / 2` | +72 / -2 |
| `Net/NetSession.cs` | `42 / 30` | `104 / 39` | +62 / -9 |
| `Net/NetBattle.cs` | 未改（0/0） | `51 / 27` | +51 / -27 |
| `Net/NetRuntime.cs` | `5 / 2` | `13 / 3` | +8 / -1 |
| `Net/NetMatchmaking.cs` | `23 / 32` | `47 / 38` | +24 / -6 |
| `Net/UpnpPortMapper.cs` | `14 / 18` | `24 / 18` | +10 / -0 |
| `Core/Loc.cs` | `134 / 7` | `165 / 7` | +31 / -0 |
| `Battle/BattleDriver.cs` | `297 / 22` | `304 / 23` | +7 / -1 |
| `Editor/NetSelfTest.cs` | `180 / 23` | `245 / 23` | +65 / -0 |
| `Editor/NetBattleTest.cs` | 未改（0/0） | `10 / 3` | +10 / -3 |

（数字都远小于各文件的行数 ⇒ **没有整篇重写的行尾事故**。）

**⛔ 没跑的**：任何 Unity 自检（`-executeMethod`）—— 按本轮口径。⇒ 上面那 4 条新断言
`TestWireTextCodec` **只过了编译、没跑过**，如实标出（= 交回调度台在同步点跑）。

---

## ⑤ 没查清 / 停手的

1. **原版到底发什么，我仍然查不到**（只读查证代理也卡在这条）：`d:/2/tools/decomp_full/` 里
   原版的联机是 **PlayFab** 那一套（`SetPlayfabDisplayName` 等服务端调用），**没有**我们这套 P2P 的
   「发理由串给对面」的对应物 ⇒ 「原版是服务端发键、客户端取词」这条**只有简报一个来源**，**本件未独立复核**。
   ⛔ 所以 `Wire/*` 的**键名 + 两列文案全是自拟**（已逐条在 `Loc.cs` 注明）。
2. **`A1080` 结论 = 【核不出洞】⇒ 一行都没加**（判据与推理）：
   - **「半张表」不存在** ✅：`Table` 是 `static readonly … = new Dictionary<…>{…}` 静态字段初始化器
     （`Core/Loc.cs:162`），**没有手写的 `static Loc()`**（`grep "static Loc()"` = 0）⇒ 由 CLR 的
     **类型初始化器**保证「在**任何**静态成员被访问之前、且只跑一次、线程安全」跑完 ⇒
     `T()` 能进门就说明 `Table` 已经全填好。
   - **没有任何可达路径让后台线程先到 `Loc.Load()`**：会从后台线程调 `Loc.T` 的只有三处 ——
     `TcpTransport.ReadLoop`（`Net/NetTransport.cs:307-319`）· `TcpTransport.Setup` 的异常支（`:282`，
     由 `Connect`【主线程】或 `AcceptLoop`【后台】进）· `UpnpPortMapper.Map`（SSDP 那条）·
     `NetConfig.ProbeExternalAsync`（`Net/NetConfig.cs:303/307`）。而
     `NetSession` 的**构造函数**第一件事就是 `Loc.T("Settings/Online/St/Off")`（`NetSession.cs:102`），
     它在**主线程**上由 `NetRuntime.Init()` 调（`Net/NetRuntime.cs:53`）——
     **任何一台 `NetSession` 存在之前，语言表就已经在主线程加载完了**；
     而 `StartHost`（发 `MapAsync` 前先 `Loc.T` 了两句）与
     `ProbeExternalAsync` 的唯一调用点（`Shell/SettingsWindow.cs:2724`，一个自己就在用 `Loc.T` 画字的窗口）
     都排在它之后。
   - ⚠️ **两个「窄口子」如实记下（不是洞，是潜在面）**：
     ① `Load()` 是 `if (_loaded) return; **_loaded = true;** int v = PlayerPrefs.GetInt(…)`
     —— **先立旗、后读盘**。真出现并发首调时，另一线程会看到 `_loaded == true` 而拿到 `_current`
     的**初始值 `Default`（中文）**，等前者读完才翻正 ⇒ **自愈、且值是出厂值**（不是半张表、不是空）。
     ② `Loc.T` 在「缺键 / 回退」那两支上会**写**共享状态（`MissingCount++` · `_warnedMissing.Add` ·
     `FallbackCount++`），而 `HashSet<T>` 与 `++` 都**不是线程安全的** —— 后台线程调 `T` 时若命中那两支
     就有竞态（**今天不会命中**：那三处传的都是**表里已有的键**，且中文/英文两档都在）。
     ⇒ **真要堵**，最小改动是「在 `NetRuntime.Init()` 里加一行 `Loc.Current;` 预热」（白名单内）。
     ⛔ **本件没加** —— 简报明写「⛔ **不许先猜着加**」「核不出洞就只报」，而上面两条都**举不出可达路径**。
     **建议**：调度台若要防将来（例如以后有人从 `NetConfig` 的探测线程先碰 `Loc`），就加那一行。

---

## ⑥ 顺手发现的（⛔ 都没自己动手，除 §2·D 那几项在账内）

1. 🔴 **`Shell/SearchingOpponentWindow.cs:78-81` 的注释**（⛔ **不在我的白名单，一个字没碰**）说
   「走到本窗的字符串**已经 ≤ `NetProtocol.MaxPeerTextChars`**」。**P6d 之后这句只对「原串」成立** ——
   现在收侧是**先钳后取词**，而取完词那一句是**我们自己的文案**（`St/VersionMismatch` 英文列 **95 字**）
   ⇒ 到那一行时**可以超过 40**。**建议**：把该段改成「本窗收到的是**已经钳过并取过词**的串；
   ≤40 那个上界管的是**对端塞进来的原串**，不是取词后的整句」。
2. 🔴 **两处「同一条句子」现在有两份**（铁律 6 的经典情形）：
   · `NetBattle.HandlePeerClosed` 里 `string body = …? "对面离开了这一局" : why;` 这句**字面量**
     与 🆕 `Wire/PeerLeftMatch` 的 **ZH 列逐字相同**——`NetBattle` 那批被 `A1036` 裁成「不接」，
     所以本件**没动**；但它迟早要接，接的时候**用同一把键**、⛔ 别再留一份。
   · `Loc` 的 `Battle/Tips/NotYourTurn` 与 `Wire/NotYourTurn` **ZH 同值**（查证代理早记过）——
     `Loc` 无反向查表，**今天无害**；⚠️ 将来若做「中文反查键」会撞车。
3. ⚠️ **英文档的提示行预算本来就超**：`Lobby/DeferToBattle` 的 EN 模板 86 字、`St/*` 若干条 EN 更长
   （`HintLineMaxChars = 40` 只按**中文列**算过）⇒ `SearchingMatchPopup.ShowHint` 在英文档会
   `LogWarning`（它**照样画**）。**本轮改动没有让它变长**（两段碎片的 ZH 与原文逐字相同），
   但这是**接线前就有的**一条真账 ⇒ 记下来（⚠️ 不在本笔白名单的改动面内 —— 要修得先定「EN 列是否也要 ≤40」）。
4. ⚠️ **`Net/NetProtocol.cs` 的文件头写着「联机协议 v1」**，而 `NetProtocol.Version = 2`
   （2026-10-01 改的）—— 一句话过期，顺手记下（本笔只改了 `MaxPeerTextChars` 那段与新增类，⛔ 没扩面去改文件头）。
5. 📌 **`Loc.cs` 里键的**计数**现在不是「7 条」了**：`Wire/*` = **8 条**（加了 `PeerLeftMatch`）；
   `Settings/Online/St/*` 多一条 `CheckOk`；`Lobby/*` 多两条 `*Frag`；`Upnp/*` 多一条 `OkWanSuffix`。
   ⇒ 凡引用「7 条走线键」的文档/注释都要跟着改（本件已改 `Loc.cs` 与 `NetBattle.cs` 那两处；
   `资料/普查产出_第六会话/查证_P6d协议兼容面.md` 里的「7 处」是**查证那一刻**的读数、**按原样留**）。
