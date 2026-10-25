# CH — 战斗线「对手聊天：联机收发」

> 执行代理 `CH` · 2026-10-25 · 白名单内动手 · 未跑 Unity · 未动 git · 未改正本/任务总表。
> 判据来源：`资料/普查产出_1025/E_判据_引擎与战斗缺口.md` §11 ＋ 本轮**逐句重读**的三份反编译
> （`BattleCommsManager__SendChatMessage.c` / `BattleCommsManager__PUNReceiveChatMessage.c` /
> `VoiceLinesController__DisplayWarlordChatMessage.c` / `VoiceLinesController__CanChat.c`）。

---

## 一、协议形状（已落地）

| 项 | 值 | 判据 |
|---|---|---|
| 消息 kind | `"chat"`（`NetKind.Chat`） | 新增；不改任何既有消息格式 |
| 载荷 | `MsgChat { int seat; int chatId; }` | `SendChatMessage.c:20-37` 线上只有**一个 int**；`seat` 是我们补的（P2P 没有 Photon 的发送者标识） |
| 不带文本 | 台词 = 词条 + 语音资产，两端各按自己语言取词 | `DisplayWarlordRegularChatMessage` 那一链；同 `NetWireText` 的既有口径 |
| 不带时长 | clip 长度是**收侧自己那颗 clip** 的长度 | `DisplayWarlordChatMessage.c:145`（`BattleManager +0x4e0`，记流水时才取） |
| 不带 isEnemy | = `seat != 本地座位` | 同上（`param_3` 就是 isLocal） |
| 版本号 | **不 +1**（理由写在 `NetKind.Chat` 的 doc 里） | 新增 kind 不改既有格式；旧端会走 `Dispatch` 末尾「不认识的联机消息」**出声**并丢掉，不崩不影响对局。同族先例 = `NetWireText`（P6d 换编码也没 +版本，靠收侧出声兜底）。⚠️ 若以后给 `MsgChat` **加字段**，那就必须 +1 |

`chatId` 取值 = **原版 `ChatMessage` 枚举**：`5…10` = 6 颗台词钮（`Greet/Threat/WellPlayed/Taunt/Sorry/Oops`）。
判据 = `VoiceLinesPopupSelector__SetupChatOptions.c:54,112`（写死起点 **5**，按按钮下标 +1）＋
`VoiceLinesController._ChatAIResponse_d__60__MoveNext.c:47` 上面那两行（`Range(5, 11)`）。

---

## 二、改动清单（全部在白名单内）

| 文件 | 规模 | 内容 |
|---|---|---|
| `Assets/CardPresentation/Net/NetChat.cs`（**新增** + `.meta`） | 新文件 | 判据正本（文件头逐句列了原版五段语义）· `INetChatHost` 可选接口 · `NetChat` 常量与纯函数：`FirstButtonChatId=5` / `LastButtonChatId=10` / `ReplayMatchType=0xA0` / `ActionType=0x37` · `IsButtonLine` / `ButtonIndex` / `IsRelayable`（原版 `:65` 的 0/1 闸）· `ShouldRecord(chatId, isReplay)`（原版 `:138` 的三道闸） |
| `Net/NetProtocol.cs` | +27 行 | `[Serializable] MsgChat`（`:178`）· `NetKind.Chat = "chat"`（`:223`，含「为什么不 +版本」） |
| `Net/NetBattle.cs` | +71 行 | ① `OnLocalChat(int chatId)`（`:1053`）：只发 5…10，越界**出声不发**，`seat = MySeat`；② `Dispatch` 的 `case NetKind.Chat`（`:1289`）四道闸 + 交给宿主 |
| `Editor/NetBattleTest.cs` | +77/−1 行 | `BareHost : INetBattleHost, INetChatHost` + `Chats` 列表 · 新 §4c 断言 17 条（`:299` 起） |
| `Battle/BattleDriver.cs`（**第二笔**：协调者 2026-10-25 追加，AU1 已收工） | +152 行 | 见 §四·2：四处接线 + 两条气泡序号缺陷 + 回放那一支 |

`git diff --numstat`：`NetBattleTest.cs 77/1` · `NetBattle.cs 71/0` · `NetProtocol.cs 27/0`
· `BattleDriver.cs 1251/95`（**这一份含 AU1 早先的改动，没法单独隔离我的那 152 行**；判据 = 行数
30979 → 31131 恰 +152，且 `CRLF 31131 / LF 31131` ⇒ **没有混行尾**）。
⇒ 四个文件都**没有整篇重写**。

### 收发语义（逐条对上原版）

1. **发送**：只有「本地玩家开口」那一路调 `OnLocalChat`；`seat = MySeat`（绝对座位，⛔ 不翻）。
2. **接收**：
   - `seat == MySeat` ⇒ **不显示、不回发**（原版 `PUNReceiveChatMessage` 的发送者过滤 ＋ 回环防护）。
   - `seat` 既不是本机也不是对面 ⇒ 出声丢掉。
   - `chatId` 不在 5…10 ⇒ 出声丢掉（⛔ 不拿对端给的数去索引台词/语音表 —— 那是「把一句伪造的话
     说成督军台词」的入口）。
   - 合法 ⇒ `INetChatHost.NetRemoteChat(chatId)`；宿主没实现该接口 ⇒ **出声**（不静默）。
3. **回环防护**：`Dispatch` 那一支**结构上不存在发送**（原版同理：`DisplayWarlordChatMessage.c:53`
   那个 `isLocal != 0` 的发送支，收到那一档 `isLocal = 0` ⇒ 根本不进）。
4. **不改局面**：不进 `Ctx`、不消耗 `Rng`、不进 `Log`、不参与指纹 ⇒ 重连重放也不需要补发。

---

## 三、断言清单（`NetBattleTest.Run` §4c，17 条）

| # | 断言 | 钉的是什么 |
|---|---|---|
| 1 | 两端 `Chats` 都为 0 | 负例基线 |
| 2 | 客机 `OnLocalChat(8)` ⇒ 主机**收件口在 2 秒内收到**（`PumpUntil2` 条件等待，不靠固定短睡） | 发送能过网 |
| 3 | …再多推 150 ms ⇒ 主机 `Chats` **恰好 +1** | **一次**（防重复显示） |
| 4 | …且值 = 8 | 内容对得上（不是别的一句） |
| 5 | 说话那一端**不被自己那条再显示一遍** | 原版发送者过滤 |
| 6 | 伪造「本机座位」的 chat 打进来 ⇒ 主机**不显示** | 发送者过滤 |
| 7 | …且主机**没有回发**（客机 `Chats` 不变，给足 200 ms） | **回环防护**（判别式：在 `Dispatch` 那一支加一句回发 ⇒ 红） |
| 8 | `chatId=99` / `0` ⇒ 主机**一句都不显示** | 越界闸（防拿对端数据索引台词表） |
| 9-11 | 主机说第 10 句 ⇒ 客机**照样收到**、**只 1 条**、值 = 10 | **对照 / 灭自证**：防「一律丢掉」也能全绿 |
| 12-13 | 本机 `OnLocalChat(0)` ⇒ 两端都不多出一句 | 原版 `:65` 两道闸的同族 |
| 14 | `ShouldRecord(5, isReplay:true) == false` | 回放（`0xA0`）不记 |
| 15 | `ShouldRecord(0/4, false) == false` | 原版 `:138` 写死的两个数 |
| 16 | `ShouldRecord(5/10, false) == true` | 6 颗钮那一档照记 |
| 17 | `ButtonIndex(5) == 0` · `ButtonIndex(10) == ForChatButton.Length-1` | 3 张表（枚举 / 6 颗钮 / `ForChatButton`）一一对上 |

> 负例（5/6/7/8/12/13）一律**给足 200 ms** 才判「没有」—— 短睡判「没到」是假绿。

---

## 四、状态与要主对话做的事

### 4·1 跑哪条自检

- **`NetBattleTest.Run`** —— 协议 / 收发 / 过滤器 / 越界，就是 §三 那 17 条。
- **`BattleScene.Run`** —— 🔴 **第二笔之后必须跑**：动了 `BattleDriver` 的聊天显示链与录像流
  （`SpeakChat` / `SpeakAiChat` / `SpeakIntro` 三处气泡序号、`RecKindChat` 的录制与回放）。
- `RuleEngineTest.Run` **不需要**（引擎零改动）。
- `NetSelfTest.Run` / `SettingsScene.Run` **不需要**（没动 `NetSession` / 握手 / 帧格式 / 设置窗）。
- 其余宿主不用（没动 Shell / 菜单 / 卡组 / 战场场景）。

### 4·2 ✅ 已做：`BattleDriver` 那半边接线（协调者 2026-10-25 追加；AU1 已收工）

| # | 落点（现读行号） | 内容 |
|---|---|---|
| 1 | `:22` | 类声明 → `: MonoBehaviour, INetBattleHost, INetChatHost` |
| 2 | `:544` | `const int RecKindChat = NetChat.ActionType;`（`0x37` = 55，与 100-103 / 200-202 不撞） |
| 3 | `:6517-6528` | **`BubbleSideOf(seat)`** = 座位 → **气泡序号**的唯一换算（`seat == _me ? 0 : 1`），判据与三处调用点都写在 doc 里 |
| 4 | `:6533-6592` | `SpeakChat` 拆成「显示 + 发送 + 记流水」；显示那一半抽成 **`SpeakWarlordLine(seat, idx)`**（本地 / 对面 / 回放**共用这一份**） |
| 5 | `:6791` | `SpeakAiChat` 的气泡序号也改走 `BubbleSideOf` |
| 6 | `:6821-6883` | `public void NetRemoteChat(int)` → `DisplayRemoteChat(chatId, record)`；`ShowReplayChat(chatId, seat)` |
| 7 | `:1192-1200` | 回放泵 `RecKindChat` 分支（**排在 `ApplyLoggedAction` 之前** + `ObserveReplayFullTrace`） |
| 8 | `:9474` | `SpeakIntro` 的气泡序号改走 `BubbleSideOf` |

**收发语义（已落）**

- **收到（联机）**：`NetBattle.Dispatch` → `INetChatHost.NetRemoteChat` → `CanAiChat()`
  （= 原版 `CanChat(isLocal=0)`：对局中 + 未静音；⚠️ 还含我们挑的「开场独白期间不说话」护栏）
  → 显示在**敌方**气泡 → `NoteEnemyWarlordChat()`（第 5 条 SmallTip，**复用既有计数器**）
  → `RecRaw(RecKindChat, seat, envSlot: chatId)`。
- **本机开口**：`SpeakChat` = 显示 → `_net.OnLocalChat(5 + idx)` → `RecRaw(RecKindChat, _me, envSlot: 5 + idx)`
  （次序照原版：显示 `:52` → 发送 `:67` → 记流水 `:147`）。
- **回放**：`seat == _me` 只显示（⛔ 不发、⛔ 不排 AI 回话、⛔ 不计数）；`seat != _me` 走
  `DisplayRemoteChat(record: false)` —— 静音闸与第 5 条 tip 照原样复现，但**不再记流水**。

**如实记（三处偏离 / 待裁）**

1. 原版那格 `PlayerAction` 还带 **clip 长度**（`+0x4e0`），我们**没有第二格 int**（`MsgAction` 是上网结构，
   `ReplayStore.cs` 文件头明确禁加字段）⇒ **不记**；回放按**同一颗 clip** 现算，时长本来就相同。
   要逐字段一致就得给录像换一个**只在本地**用的结构 —— 那是 `ReplayStore` 的活（不在本笔范围）。
2. **AI 那几句没进录像**（`SpeakAiChat` 的两个入口：回话 / 自发）。原版 `isLocal == 0` 那一档**也记**，
   本笔只接了我方 + 联机对面两条。补法是一行（`SpeakAiChat` 成功后
   `RecRaw(RecKindChat, 1 - _me, envSlot: 5 + idx)`），但**会让既有单人录像多出条目**
   （自检里那几处「录一段再放回去」的夹具要一起看）⇒ **留给主对话裁**。
3. `CanAiChat()` 里多一条**我们挑的**「开场独白期间不说话」护栏（原版是 `popUpEnabled` 那类闸，我们没搬）
   ⇒ 开场独白那几秒对面发来的聊天**不显示**（会出声）。要更贴原版得单独做 `popUpEnabled`。

### 4·3 正本 / 台账

- `资料/联机P2P_设计与交接.md` §五（消息表）应补 `chat` 一行（本笔不改正本）。
- `资料/任务线/战斗线/任务总表.md` 的「对手聊天」行：**代码侧已闭环**；剩一次
  「两台客户端互说一句」的真机验收（进 `资料/真Play待验清单.md`）。

---

## 五、判据层面的两处订正（铁律 5）

1. 🔴 **`E_判据…` §11 把原版那句「回发」写成了「远端档回发」—— 实际是【本机档】才发**：
   `DisplayWarlordChatMessage.c:53` 的 `if (local_res18[0] != '\0')`（= `isLocal != 0`）里才有
   `SendChatMessage`（`:67`）。收到别人消息那一档 `isLocal = 0` ⇒ **根本不进发送支** ⇒
   「防回环」不是一道额外判据，而是**结构上不可能**。本笔的 `NetBattle.Dispatch` 就是照这个形状写的。
2. ⚠️ **`PUNReceiveChatMessage.c:24` 印的是 `String_op_Inequality(...) == 0 ⇒ 显示`**，
   按字面读 = 「发送者**等于**本地才显示」。那会把**自己那条回显显示两遍**、**对面那条永不显示**
   （自己那条在 `isLocal != 0` 支里已经显示过），与 Photon `RpcTarget.All` 的广播形状直接矛盾 ⇒
   判据取 **「不等才显示」**（与 §11 结论一致）。**仍不确定的是**这一处到底是
   il2cpp 包装函数名在反编译产物里错配，还是 `param_3+0x18` 取的**不是** sender id ——
   要坐实得读 `global-metadata.dat` 里的 `PhotonMessageInfo` 布局（**仍然查不到**，见 §七）。

---

## 六、顺手发现（第一笔只记账 → **第二笔已全部修掉**）

1. ✅ **`SpeakChat` 把 `_me` 当气泡序号用**（旧 `:6527`）：`_unitChat.Speak(_me, …)`。
   `UnitChatPanel.BubbleFor` 里 **0 = `PlayerChatDisplay`（我方）/ 1 = `EnemyChatDisplay` / 2 = `RadioChat`**，
   是**视图侧**序号、不是座位 ⇒ **客机（`_me == 1`）自家台词画进「敌方」气泡**。
   ⇒ 第二笔已改为 `BubbleSideOf(seat)`（`SpeakWarlordLine` 那一份），并把 `SpeakAiChat` 一起收口。
2. ✅ **`SpeakIntro(int side)`（旧 `:9322`）把绝对座位直接当气泡序号** ⇒ 客机档下**两句都反**
   （座位 0 = 对面 → 进我方气泡；座位 1 = 自己 → 进敌方气泡）。⇒ 第二笔已改为 `BubbleSideOf(side)`。
   （`side` 在 `SpeakIntro` 里**同时**是座位（`Ctx.Players[side]` / `side == _me`）和显示方向 ——
   现在**只把显示那一处**换算，数据仍按座位取，别把两者混回一处。）
3. ✅ `NetChat.ClipMsOf` **已删**（没有消费者：我们决定不记 clip 长度，见 §四·2 偏离 ①）。

---

## 七、仍然查不到（⛔ 不许猜）

1. `PhotonView.RPC` 的**方法名字符串**（反编译里只有 metadata 引用 `DAT_18427c0d8`）。
   ⇒ 我们不是 Photon，**用不着**；只要语义/顺序一致。
2. `PhotonMessageInfo` 的字段布局（`param_3+8 → +0x18` 到底是 `sender.UserId` 还是别的）。
   ⇒ 影响 §五·2 那条判据的「字面意思」，不影响我们的实现（我们用显式 `seat`）。
3. `BotEmotesConfig` 四个概率值（远端 RemoteConfig）⇒ 与本件无关（那是 AI 自发发言的频率）。

## 八、红线自查

- ⛔ 未跑 Unity（只跑了 `TMPDIR=/tmp/wf_ch bash 工具/typecheck.sh` ⇒ 运行时 0 / 编辑器 0，
  第一笔一跑、`BattleDriver` 那一笔之后再跑一次，**两次都绿**）。
- ⛔ 未动 git；未改正本 / 任务总表；未碰 `Core/**` / `RuleEngine/**` / `Shell/**` / `Editor/BattleScene.cs`。
  `BattleDriver.cs` 是**协调者 2026-10-25 明确追加**的白名单（AU1 收工之后）。
- 改文件：`Net/*` / `Editor/NetBattleTest.cs` 走 Edit/Write；`BattleDriver.cs` 是 **CRLF**，
  用 python `wb`（先在内存里做完全部替换、逐个断言命中恰好 1 次、写临时文件再 `os.replace`）
  ⇒ 现读 `CRLF 31131 / LF 31131 / CR 31131`，**没有混行尾**、也没有整篇重写。
- 查不到的三项已如实列在 §七，没有用猜测填空。

## 九、收尾读数（2026-10-25 06:56）

`TMPDIR=/tmp/wf_ch bash 工具/typecheck.sh` ⇒ **运行时程序集 0 错**（我的 `BattleDriver` + `Net/*` 全在里面）；
**编辑器程序集 6 错，全部落在 `Editor/BattleScene.cs:27354/27360`**
（`CS0103: CaptureBoard / ShadeTestW / ShadeTestH` 未定义）——
**那是别人正在写这份文件**：它的 mtime = `06:56:10`（就在这次 typecheck 前 20 秒），
新加的代码带着 `🆕 2026-10-25（M1）` 标记，而三个辅助函数还没落地。
⇒ 按 `CLAUDE.md` §13·3 规则 2：**不是我的活、也没改过这个文件**（我最后一次动它是 0 次）；
我那半边要等它落完再复跑一次才能拿到编辑器的绿。


