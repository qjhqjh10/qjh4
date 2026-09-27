# 联机（P2P）—— 设计与交接（**这条线的唯一正本**）

> 立项：用户 2026-09-22。**选型定案：用户 2026-09-26**（四条，见 §〇）。
> 任务清单那一侧：`项目任务.md` **§三 第 14 条**（只留索引与待办，判据全文在本文件）。
> 读完本文件 + 那一条，就知道联机做到哪、下一步干什么。
>
> ⚠️ **本文件写的是「我们怎么接」** —— 原版那半（原版有没有联机、怎么匹配）**不在这里**，
> 在 `资料/加时与冲突模式_原版规格.md` §2.3（那里有 `FastMode`/`MatchType`/12 秒链的判据）。

---

## 〇、四条已拍板（用户 2026-09-26 选，**别再改**）

| # | 决定 | 被否的那两个为什么不行 |
|---|---|---|
| 1 | **联机页挂在「设置窗」里**，设置窗照原版建 —— **先做 图像 / 音频 / 联机 三页** | 「只建联机页」不像原版；「独立成窗」等于把设置窗永远欠着 |
| 2 | **主机权威 + 动作定序**：客机动作先发主机 → 主机应用 → 广播 → 客机才应用 | 「确定性锁步」手感好但**没有回滚手段**（引擎没有快照，见 §四·六）；「主机算客机只收状态」要把整局状态写成可序列化快照，工作量最大 |

> 🔴 **2026-09-26 落地时把 ② 的实现细化了一步**（**结论没变，路数变了**，如实记在这儿）：
> 原方案是「客机**先发主机、等回执再落地**」—— 但**一个回合只有一个行动方**
> （加上 TCP 有序），动作顺序**天然就一致**，那个 RTT 是白等的。
> ⇒ 现在改成：**本地动作乐观落地**（手感与单机一致）→ 同时发给对面；
> **主机照样把客机的动作过一遍自己的引擎** —— 引擎拒了就 `reject` + **当场中止**（不静默、不假装）。
> 「主机权威」体现在**拒绝权与定序权**（`seq` 由主机发、权威动作流由主机持有、重连靠它重放），
> 而不是「客机等回执」。⚠️ **代价如实说**：真出现非法动作时**没有回滚**，只能中止这一局。
| 3 | **自写最小协议跑 TCP**（零依赖） | LiteNetLib 要多一个第三方 DLL（**传输层封成 `INetTransport`，将来要换只换实现**）；Unity 官方包是为 GameObject 同步设计的，与我们的确定性引擎不搭 |
| 4 | **首版只做「经典」1v1** | 遭遇（Skirmish）引擎已通，多接一条模式选择即可；轮抽（Draft）**整个模式还没做**（`项目任务.md` §三 第 17 条） |

## 一、用户给的规格（**照抄，别自己改**）

- 在**设置界面**新增一个「**联机**」子界面（**跟图像、音频一样的子界面**，同一个设置面板里再开一页）。
- 玩家在里面**勾选成为「主机」或「客机」**：
  - **主机**：「IP 地址」输入框 + 「密码」输入框 → 它们下面一个「**保存**」按钮；
    **「IP 地址」输入框右边还有一个刷新按钮**（样式由我设计）—— 点击后**自动填入当前 IP 地址**。
  - **客机**：「IP 地址」输入框 + 「密码」输入框 → 它们下面一个「**检查连接**」按钮 ——
    点击后**自动保存 IP 与密码**，并**尝试与主机连接、反馈连接成功与否**。
- 另交代：**先看有没有现成的工具/软件，能下载或改造就接入；没有就自己写一个**（铁律 8 允许自行下载）。

## 二、我按默认定、用户可否决的几条（**标清楚是「我们定的」**）

| # | 默认 | 理由 / 出处 |
|---|---|---|
| 1 | **密码 = 握手口令**：连接时用（协议版本 + 一次性随机数 + 密码）算一个 HMAC 发过去，主机比对。**不加密流量** | 局域网/学习用途；加密要引入密钥交换，收益与成本不成比例。**密码留空 = 不校验**（主机可只对局域网开放） |
| 2 | **默认端口 `47777`**，可在联机页改 | 我们自己挑的（原版没这个概念，它走服务器）。⚠️ 端口写在设置里，别散落 |
| 3 | **掉线允许重连**（用户 2026-09-26 明确要）—— 机制见 §5·6：**主机持有权威动作流，客机重连 = 从种子重建 + 全量重放**。⚠️ 主机在对方掉线期间**暂停推进**；等太久由玩家选「继续等 / 结束这局」 | 「掉线即判负、不做重连」是省事，但用户要重连；「快照同步」要先把整局状态写成可序列化快照，否决理由同 §四·6 |
| 4 | **不做 NAT 穿透**。公网自连的两条现成路线写进联机页的说明文字：① 路由器端口映射 ② 虚拟局域网工具（Tailscale / ZeroTier / 蒲公英 之类） | 用户问的「现成的工具」那一半；代码侧零成本 |
| 5 | **战场由主机定**（= 主机的督军阵营那一场） | ⚠️ **这是我们定的**：原版 `GetBattleArena(army)` 里那个 `army` 在 PvP 下取谁，本地判不出（服务端/匹配数据）。两端必须**载入同一场**，否则两边看到的战场不一样 |
| 6 | **出牌倒计时以主机的时钟为准** —— 主机随每条广播捎上「本回合剩余秒数」 | 原版 `DefaultScenario.clockTimeLimit = 60`（三份资产实测，见 §2.6）；各算各的会两边不一样 |

## 三、已经查清的现状（判据在此，**别重复查**）

### 3·1 已经建好的（**不要再做一遍**）

| 件 | 实现 | 出处 |
|---|---|---|
| 主菜单 5 张模式卡（**3 张可点**：Practice / Skirmish / Ranked；Tutorial 与 Draft **没有点击区**） | `MainMenuRuntime.BuildGameModes` | `Shell/MainMenuRuntime.cs:445`（分发器 `OpenMode` `:480`） |
| 练习窗 / 遭遇窗 / 排位窗 / 找对手窗 / 匹配弹窗 / 选卡组窗 / 活动窗基类 | | `Shell/{PracticeModePopup,SkirmishEventWindow,RankedEventWindow,SearchingOpponentWindow,SearchingMatchPopup,DeckSelectionPopup,LiveOpsEventWindow}.cs` |
| **12 秒匹配链**（原版常量 12.0，`工具/read_literal.py` 读出） | 三条入口**落点相同**：练习 `PracticeModePopup.cs:643` · 遭遇/排位 `LiveOpsEventWindow.cs:618` → 等满 → `StartBotBattle` → `SceneManager.LoadScene` | `Shell/SearchingMatchPopup.cs:69` |
| 「暂无服务器」的**如实提示** | 唯一弹给玩家的一处：排位窗的 toggle（`Shell/RankedEventWindow.cs:126`）。其余都是日志/注释 | |
| **原版开战链**（P2P 要接的正是它） | `MatchMakerManager.StartMatch` → 显示 `Searching Oponent Popup` 等 **12 秒** → `StartBotBattle` → `SearchOpponentManager.StartBattle` → `GetBattleArena(army)` → **`LoadScene`（全二进制唯一的一个 LoadScene 点）** | `资料/加时与冲突模式_原版规格.md` §2.3 |

### 3·2 🔴 **网络层：0 行**（这就是「多人没复刻」的全部含义）

在 `Unity/MyGame/Assets/**/*.cs` 全量搜过、**全部 0 命中**：
`System.Net` · `Socket` · `TcpClient` · `UdpClient` · `NetworkStream` · `UnityWebRequest` ·
`HttpClient` · `Unity.Netcode` · `ClientWebSocket` · `IPAddress` · `Dns` · `Steamworks`。
（`Mirror`/`Relay`/`Ping`/`Photon`/`PlayFab` 的命中**全是误命中/注释**：`ArenaMirror` 是战场平面反射、
`Relayout` 是手牌布局、`PingPong` 是单测里的单位名。）

⇒ 点 `Battle!` → 开 `Searching Oponent Popup` → 等 12 秒 → **永远**落到本地 bot。

### 3·3 引擎侧只有两个模式值（**原版有 15 个**）

- 代码里唯一的枚举：`GameMode { Classic = 0, Skirmish = 13 }`（`RuleEngine/Core/GameplayVariables.cs:25`）；
  卡组上的模式是裸 `int`（`PlayerDeck.GameMode`，取值照原版 `PlayModes`）。
- 原版 `PlayModes` 全 15 个值**只存在于文档**：`资料/加时与冲突模式_原版规格.md:214`。
- ⇒ **别把 `PlayModes` 当成已有代码**；要用哪个值先看那一条。

### 3·4 🔴 主菜单设置窗**没建**，齿轮点了没反应（**这是一条缺陷，不是待办**）

- `Shell/MainMenuRuntime.cs:359` 只建了齿轮的图，**没接任何点击** ⇒ 玩家点了**什么都不发生**（连提示都没有）。
- ⇒ 本线**顺带把它建起来**（用户 2026-09-26 拍板：先做 图像/音频/联机 三页）。
  🔴 **建完必须给齿轮接上点击**，并配一条断言（不然修完还是静默失败）。

### 3·5 🆕 原版设置窗的实读规格（**2026-09-26 普查；建之前看这一节**）

根：`Main Menu Settings Window`（`资料/说明书/04_界面UI/菜单全树.md:5480`）· 类 **`SettingsMenu : GameWindowWithTabs`**
· pid `-7066813013973172314` · 底图 `40k_popup`（9-slice，border 169,160,169,160）+ 内衬 `40k_popup_texture`。

**五个页签**（GO 名 / 解包里 `Tab Title` 的**实际显示串** / 该页有什么）：

| # | GO 名 | 显示串（解包原样） | 页里的控件 |
|---|---|---|---|
| 1 | `General` | `General`（默认选中） | `VersionText` · `Language Selector` · 三个勾选（`Disable Bots` / `Disable Notifications` / `Touch input`）· 底部 `Redeem Code` / `Exit Game` |
| 2 | `Media` | **`Multimedia`** | **`Audio Settings` = 三根滑块**（Music / Sound effects / Voice Over）+ `Visual Settings` = `WindowMode Selector` 下拉 |
| 3 | `Account` | `Cuenta`（页内 `Tab Title` 写的是 `Account`） | 玩家 id · 登录/注册表单 · 社媒外链 · 一串账号按钮 |
| 4 | `Graphics` | `Gráficos` | `Quality DropDown` · `Text in Hand DropDown` · 一串勾选（`Small Screen Size` / `Auto Zoom` / `Hi FPS` / `super sampling` / `Vsync` / `FPS Limit`） |
| 5 | `Support` | `Soporte`（页内 `Tab Title` 写的是 `Support`） | FAQ / Contact / Support 外链 |

- 🔴 **显示串是西班牙语**（`Cuenta`/`Gráficos`/`Soporte`/`Multimedia`）—— 解包那份是西语构建；
  **英文正式文案本地拿不到**（页签 TMP **没挂 I2 词条**，正式文本在远端语言表）。⇒ 我们**自己定英文/中文**，
  **在代码注释里标清「这是我们挑的」**（铁律 3）。
  ⚠️ **别把 `General/Media/Account/Graphics/Support` 当成显示名**：第 2 页显示串是 **`Multimedia`**。
- **图全在本地**：页签底 `40K_settings_button{,_hover,_pressed,_selected}`（`bundle_duplicateassetisolation_assets_all/Sprite/`）·
  页签图标 `40K_settings_button_{general,quality,account,graphics,support}`（`bundle_atlasindividual_assets_0_mainmenu/Sprite/`，
  ⚠️ **Media 页用的就是 `_quality` 那张**）· 页签预制体 `bundle_menus_assets_all/GameObject/Settings Menu Tab Toggle.json`。
  我们工程里**已有一份**：`Assets/CardPresentation/Art/原版/0_mainmenu/40K_settings_button_*.png` + `Art/原版/去重资源/40k_popup.png`。
  ⚠️ **`Resources/Art/` 在 `.gitignore` 里**（大件不进 git）⇒ **新克隆的仓库要跑一次**
  `python 工具/import_original_art.py --only-menu`（本轮的 8 张设置窗图已加进 `MENU_IMAGES`）+ `ArtBaker.ApplyImportSettings`，
  否则那扇窗的页签底/图标**取不到图**（`SettingsWindow.Tex()` 会打 `[Settings] 图缺了：…` 的警告，不静默）。
- **原版还带一整套 `Debug Buttons`**（Console / Add coins / **Season Check** / Score on Leaderboard …）—— 调试层，**不做**。

### 3·6 🆕 原版「多人相关界面」清单（我们建了 / 还差哪些）

| 界面 | 干什么 | 我们 |
|---|---|---|
| 模式卡区 · 练习窗 · 遭遇窗 · 排位窗 · 找对手窗 · 匹配弹窗 | 见 §3·1 | ✅ 全建了 |
| `SearchingOpponentWindow` 的 **`Found Player Container`** | **匹配到对手**那一态（配到了才显示） | ✅ **2026-09-26 建完**（⚠️ **这一态原版是死代码** ⇒ 时机/时长/名字是**我们挑的**，判据 → `阶段二_多人界面_原版规格.md` §6·4） |
| `Ranked Division Info` 的 `Content` 整棵 | 段位名 / 评分 / 阶梯 / `Ends in` | ✅ **2026-09-27 建完**（`Shell/RankedDivisionInfo.cs`；**数据全留空** · `Timer` 不建 —— 用户口径） |
| `Ranked Division Change` / `New Season` / `Season Ended` 三窗 | 升段与赛季首尾 | ❌（**赛季相关的按用户口径不做**） |
| 四个排行榜（`RankedSkirmish` / `RankedClassic` / `Draft` / 嵌入的 `Ranked Leaderboard Display`） | 榜首榜 + `Last season` | ✅ **2026-09-27 四棵全建完**（`Shell/LeaderboardWindow.cs`；🔴 **嵌入版原版零引用 ⇒ 建了但不编入口**；`Last season` 建了但照原版规则关着） |
| `Player Profile Window`（6 页签：Profile/Avatar/Title/Battle Log/Trophies/Ranked） | 玩家档案 | ✅ **2026-09-27 六页全建完**（入口 = 顶栏头像的 `OpenWindowButton`，已接点击） |
| `Social Submenu`（联盟 + 好友）· 好友/联盟行族 6 件 | 社交 | ✅ **2026-09-27 建完**（入口 = 左栏 `SOCIAL`，已接点击） |
| `ChatPanel` / `ChatPreview` / `Match Log` / `Battle Log Popup` / `MessagePopupWindowDuel` | 聊天 / 战绩 / 好友挑战 | ✅ **2026-09-27 建完**（`ChatPreview` 已接点击；`Battle Log Popup` 的入口是**我们拍板补的** —— 接在档案窗那一页，原版打开点查不到） |
| 投降 · 断线 | — | ✅ 投降建了；🔴 **原版没有断线界面**（全在 Photon 层）⇒ 掉线提示**是我们新增的**，别写成复刻 |

> 📌 另一条与「赛季倒计时」有关的实读：模式卡那行 = `Timer Description 'Starts in:'` + Clock Icon + `Timer Text`，
> **数据是 liveop 服务器下发**（`LiveopMenuContainer__OnInitialize` → `TimerDisplay.InitializeWitLocKey(GetEndDate(), …)`，
> 用 `PlayerDataManager.CurrentServerDateTime` 服务端时间）⇒ **本地没有数据源**，我们**没画**（`MainMenuRuntime.cs:576` 出声）。
> ⚠️ 我们**画了**倒计时的地方（用户若指的是这些，说一声就改）：任务页周挑战的 `Ends in`（值是**我们编的**）·
> 日常页 · 战役页 · 匹配那 12 秒。

## 四、引擎侧的五条接缝（**为什么这件事可行 —— 全是现成的，别另造一套**）

| # | 接缝 | 出处 | 为什么正好 |
|---|---|---|---|
| 1 | **`AiAction`** = 「一条动作」的数据结构（`Kind/HandIdx/HandInst/Slot/TargetP/TargetSlot/Ranged/AltKeyword/Score/ManaCost`） | `RuleEngine/Data/SimpleAI.cs:72` | **协议直接照它的形状**：人打出来的动作和 AI 的动作**同一套 `ExecuteAction` 落地**（`SimpleAI.cs:912`，它按 `ctx.Active` 认行动方） |
| 2 | **`AiAction` 的字段够用**：`Kind/HandIdx/Slot/TargetP/TargetSlot/Ranged/AltKeyword` | `RuleEngine/Data/SimpleAI.cs:72` | 协议照抄这些字段。🔴 **别用 `CardInstance.Id`**（第一版的设计）：它按**座位顺序**发号，**号码不承载语义** ⇒ 线上引用「哪一份牌」用的是**手牌下标**（两端状态逐位相同，下标一样准），见 `NetApply.Apply` 里那条注释 |
| 3 | **`BattleDriver.Begin(seed, myDeck, foeDeck, vars)`** 已经吃全部开局参数 | `Battle/BattleDriver.cs:776` | 联机开局只是「参数从网络来」，不是新入口 |
| 4 | **种子由主机抽、发给客机** | 原版就是这么做的：`SearchOpponentManager.StartBattle` 抽完写 `MatchData.playerGoesFirstRandomInt`，**PvP 里再经 Photon 同步** | 先手那枚硬币**两端同一枚**（`BattleDriver.cs:743` 现在是本地现抽，联机时改成「主机抽完下发」） |
| 5 | **面板答案 `ctx.ChoosePicks` / `ChooseCardIds`** = 动作之前排好的一队整数 | `Core/BattleContext.cs:719,723`；表现层填在 `BattleDriver.cs:1835` | **跟着动作一起发**即可。⚠️ `AI 回合不问`（同一处注释）—— 见 §五·5 的近似 |

**另外两条要记住：**

6. 🔴 **座位：两端同一套绝对编号**（主机 0 / 客机 1）—— 客机靠 `BattleDriver.SetMySeat(1)` 翻**视图**。
   ⚠️ **原来这里写的是「两端都以自己为 0 号位、协议里做翻译」—— 那条已作废**（镜像跑不通，见 §五·3 的更正块）。
7. **引擎是同步的**：`ResolveOps` 一口气跑完，**没有「停下来问」的能力**（`BattleContext.cs:705` 那段注释）
   ⇒ **不可能做「暂停等对面确认」**，只能靠「答案先排好」。这条决定了协议长什么样。

## 五、协议（v1 草案，**动手前先看这一节**）

### 5·1 分层

```
设置窗「联机」页 ── NetConfig（主机/客机 · IP · 密码 · 端口，落盘）
                      │
模式窗 Battle!  ──────┤ NetSession（状态机：Idle→Listening/Connecting→Handshake→Lobby→InBattle→Closed）
                      │
对局内 ───────────────┴─ INetTransport（TcpTransport：长度前缀 + UTF8 JSON）
                            └─ RemotePlayer（把收到的动作喂给引擎，替掉 SimpleAI）
```

**文件落点（新目录 `Assets/CardPresentation/Net/`）**：
`NetConfig.cs`（存取设置）· `INetTransport.cs` + `TcpTransport.cs`（收发帧、连接、心跳）·
`NetProtocol.cs`（消息 DTO + 座位翻译 + 状态指纹）· `NetSession.cs`（握手/匹配/断线）·
`RemotePlayer.cs`（对局内的对手驱动）。

### 5·2 消息表

| 方向 | 消息 | 载荷 | 说明 |
|---|---|---|---|
| C→H | `hello` | protoVer · gameVer · name · `passProof` | `passProof = HMAC(密码, 主机给的 nonce)`；主机先发 `helloChallenge{nonce}` |
| H→C | `helloAck` | ok / reason | 版本不符 / 密码错 / 房已满 —— **理由要能显示给玩家** |
| 双向 | `deckSubmit` | `PlayerDeck`（JsonUtility，`DeckStore` 同款） | 各自选完卡组 |
| H→C | `startMatch` | **seed** · mode · 双方卡组 · **arenaScene** · 谁先手 | 两边都 ready 才发 |
| C→H | `action` | `ActionDto` + `choicePicks[]` + `choiceCardIds[]` | 客机每次操作 |
| H→C | `actionApplied` | `seq` + `ActionDto` + 两条答案队列 | 主机**定序后**广播；客机**收到才应用** |
| H→C | `reject` | `seq` + 理由 | 引擎拒了（不是你的回合 / 非法落点）—— **出声**，别静默吞 |
| 双向 | `mulligan` | `indices[]` | 换牌阶段 |
| 双向 | `endTurn` / `resign` | — | 回合结束 / 投降 |
| 双向 | `hash` | `turn` + `hash` | **每回合末**，不一致 ⇒ 大声报错 + 中止 |
| 双向 | `ping`/`pong` | t | 心跳（**真掉线才判** —— 见 §5·6） |
| C→H | `reconnect` | `sessionToken` + `lastSeq` | 客机重连（token 是开局时主机发的） |
| H→C | `resume` | seed · 双方卡组 · mode · arena · **权威动作流 `actions[]`（含 seq）** | 主机灌给重连的客机 —— **客机从种子重建 + 全量重放** |
| 双向 | `bye` | reason | 主动退出 |

**`ActionDto`**：`AiAction` 的镜像，但 `HandInst` → **`handId`(int)**（接缝 2），外加 `sequence`。

### 5·3 座位：**两端同一套绝对座位**（🔴 2026-09-26 大改，**镜像那套已作废**）

**现在的做法**：**主机 = 座位 0 · 客机 = 座位 1**，两端跑的是**同一套编号**。

- 引擎侧：两端都调 `RuleCore.NewBattle(主机那副, 客机那副, seed, firstSeat: 主机抽的那个)` ——
  **参数完全一样** ⇒ 同一局逐位相同；`targetP` / `slot` / `handIdx` / `winner` / `active` **全都不用翻**。
- 视图侧：客机 `BattleDriver.SetMySeat(1)` —— `_me` 是**唯一**决定「哪一侧画在下面」的东西
  （驱动里 100 处 `_me` 全是相对的，**一处写死座位号都没有**；`SyncBoard(owner, views, mine)` 也是参数化的）。
- 协议里**没有座位翻译**了（`NetProtocol.Seat()` 留着但**没人用**）。

> 🔴 **为什么推翻了第一版**（2026-09-26 自检当场抓到，**别再走回头路**）：
> 第一版是「两端都把自己当 0 号位」的**镜像**（视图不用翻，看着更省事）。
> 自检跑到**第 40 步**分叉，根因是 **`Unstable`（随机自爆）**：它挑目标的候选单位表
> **是按座位顺序 0,1 拼出来的**，镜像之后**同一个随机下标在两边选中了不同的单位**
> ⇒ 主机那边炸了 `Terminator`、客机那边炸了 `Weirdboy`。
> 这类「按座位顺序建候选表再抽 `ctx.Rng`」在引擎里**到处都是**（随机打一个敌人 / 随机弃一张 /
> 挑 3 张候选…）⇒ **镜像不是打个补丁就能修好的，是这条路本身不通**。
> 📌 全文与判据 → `资料/已知的坑.md`（「镜像端点」那条）+ `NetProtocol.Fingerprint` 的注释。

### 5·4 状态指纹（防「悄无声息地打岔」）

每回合末两端各算一个数字哈希发过去，**内容**（**全是可复现的东西**）：
回合号 · 行动方 · 双方督军攻/血/甲 · 双方能量 · 手牌数 · 牌库数 · 弃牌堆数 ·
**场上每一格**（`CardDef.Id` + `CardInstance.Id` + 攻/血/是否已行动）· `Events.Count`。
**不含**：`ShowRng`/`AiRng`（表现与 AI 那两路，两端本来就会花得不一样）、
`System.Random` 的内部状态（**读不出来** ⇒ 这是已知盲区，写在这里免得下一个人又找一遍）。

不一致的处理：**报错 + 中止**（不做静默重同步 —— 没有快照，同步不了）。日志要把两条指纹与回合号打出来。

> 🆕 **可选加强（便宜、值钱）**：`System.Random` **不是 sealed**、`Next*` 都是 virtual ⇒
> 可以包一层 `CountingRandom : Random` 数「这一局抽了几个随机数」，把它加进指纹。
> 这样**分歧在当场就露**，而不是等它变成棋盘差异。做的时候只包 `ctx.Rng`（`ShowRng`/`AiRng` 不数）。

### 5·5 🔴 **已知的近似 / 不做**（写清楚，别让下一个人以为是漏做）

1. **「对手要选的决策」仍然由引擎随机挑**。今天单机就是这样：效果里那些「本该问玩家」的点
   由**发动方的表现层**先答好；AI 回合没人答 ⇒ 回落 `ctx.Rng`（`BattleContext.cs:715`）。
   联机**照旧**：发动方答自己的那一队，**问不到对面**。⇒ 主机那边的 `ctx.Rng` 挑。
   **不许静默** —— 战斗日志里那行「有几次是引擎替玩家挑的」保留。
   🆕 ✅ **2026-09-27 批处理已验**（`NetBattleTest` 第 6c 节，**另开一局**逼出一次选择点）：
   塞一张带三选一的战术卡（`Exemplary Warrior`）进两边手牌 → 打出去 ⇒
   `ChooseSites = 1`（**真的问了**）· `ChooseAnswered = 0`（这一支走的是**引擎兜底**）·
   **两端两个计数一致** + **指纹仍然一致**（判据 = `BattleContext` 的两个计数器 + `NetProtocol.Fingerprint`）。
   ⚠️ **为什么另开一局**：主那一局打到 `IsOver` 才停（收尾时打不出牌），而且**塞卡是「手改局面」**
   —— 塞进主那一局会当场把重连那两节的重放对账打红（本轮先试过，实证）；
   ⚠️ **也是同一条理由**：`SimpleAI.EnumerateActions` **不会**枚举出战术卡（它按格位枚举，战术卡在 `-1` 档）
   ⇒ 那一段照**驱动那条路**直接构造 `AiAction`。
2. ~~**不做重连**（掉线 = 判负）~~ ⛔ **2026-09-26 当天就推翻了**（用户点名要重连）——
   机制见 **§5·6**、落地与实测见 **§六 的 N5**。
   **仍然不做**：**观战**（⚠️ 原版到底有没有观战**还没查过** —— 在查）·
   **网络聊天功能**（用户 2026-09-26 原话：「**网络聊天功能。暂时不需要。**」）。
   🔴 ⚠️ **「不做聊天功能」≠「不建聊天界面」** —— `ChatPanel` 按用户口径「有什么复刻什么」
   属于 **`项目任务.md` §三 第 18 条 第 4 件**，**界面照原版建、数据留空态**。
   ⛔ **别拿本条当理由把那扇窗跳过**（这两句原来互相打架，2026-09-26 说清楚）。
3. **不做 NAT 穿透**（§二·4）。
4. **不做「排位分/赛季/奖励」** —— 用户 2026-09-26 明确：多人界面里**不要**积分与红水晶。

### 5·6 🆕 掉线与重连（**用户 2026-09-26 点名要**）

> **为什么不用快照**：引擎没有序列化整局状态的能力，但有**更强的两样东西** ——
> **确定性**（同种子 + 同动作序列 ⇒ 同状态）与**动作就是全部历史**。
> ⇒ **重连 = 从种子重建 + 全量重放**，不需要快照。

**机制**（主机持权威，客机是重放方）：

1. 开局时主机给客机一个 **`sessionToken`**（随机串），主机把它连同**权威动作流**（`List<ActionDto>`，每条几十字节）
   留在内存里。**动作流 = 这一局的完整历史**，几千条也无所谓。
2. 心跳（默认 10 秒）断了 ⇒ **不立刻判负**：
   - **主机**：**暂停推进**（不接受新动作、不进新回合），界面上如实显示「**对手掉线，等待重连…**」
     —— 🔴 红线：**不许静默**，也不许**假装没事继续打**。
   - **客机**：显示「**与主机断开，正在重连…**」，按 1s / 2s / 4s… 退避重试。
3. 客机重连上 ⇒ 发 `reconnect{sessionToken, lastSeq}`。主机认 token（认不出 = 不是这局的人 ⇒ 拒绝）⇒ 回 `resume{seed, 双方卡组, mode, arena, actions[]}`。
4. **客机收到 `resume` 后必须做的是**：**丢掉本地引擎状态**，用同一套开局参数**重建**，然后把 `actions[]` **按序全部重放**（纯引擎结算、不放动画），追平后再恢复收发。
   ⚠️ **必须全量重放，不许增量补** —— 掉线可能正好落在「我方动作已发出、主机还没广播」那一刻，
   增量补会把那条动作漏掉或做两次。
5. **主机自己掉线**（客机在等）：客机的本地状态**不可信**（它是重放方）⇒ 主机回来以后走**同一条流程**：
   重新握手 ⇒ 主机发 `resume` ⇒ 客机重建重放。主机自己的状态没变，不用做任何事。

**兜底**（都要能显示给玩家、不许静默）：

- **等太久**：超过 **2 分钟**仍未回来 ⇒ 由**玩家选**「继续等 / 结束这局」（主机选结束 ⇒ 判客机负；
  客机选结束 ⇒ 判自己负 = 投降）。**不自动判负**。
- **重放校验**：追上之后**立刻互换一次状态指纹**（§5·4）。不一致 ⇒ 大声报错（红线）+ 中止。
- **`sessionToken` 不进日志**（它等于这局的钥匙）。

## 六、落地计划（**一笔一验，别混着做**）

| 笔 | 做什么 | 怎么验（自检） | 状态 |
|---|---|---|---|
| **N1** | `Net/` 四件：`NetConfig` · `TcpTransport` · `NetProtocol` · `NetSession` | 🆕 `NetSelfTest.Run`：**一个进程里开两个 socket 走 loopback**，握手 + 密码对/错 + 版本不符 + 收发 1000 条 + 心跳超时 + **掉线重连**。批处理下没有帧循环 ⇒ **显式 Tick** | ✅ **2026-09-26 完成**（`exit=0`；**条数只写一处** → `资料/阵营推进_清单与交接.md` §一）。四个文件：`Assets/CardPresentation/Net/{NetConfig,NetTransport,NetProtocol,NetSession}.cs` + `Editor/NetSelfTest.cs` |
| **N2** | **设置窗**（照原版壳 + 页签）+ **图像 / 音频 / 联机 三页**；齿轮接上点击 | `SettingsScene.Run`：齿轮点得开 · 三个页签切得动 · **联机页的控件都在且位置有出处** · 图像/音频页的三根音量滑块能通到 `WarpforgeAudio` | ✅ **2026-09-26 完成**（`exit=0`；**条数只写一处**，同上）。文件：`Shell/SettingsWindow.cs`（窗 + 三页 + `MenuInputField`）· `Net/NetRuntime.cs`（会话常驻宿主）· `Editor/SettingsScene.cs`。齿轮接线在 `Shell/MainMenuRuntime.cs` 的 `OpenSettings()`，并在 `MainMenuScene.Run` 里加了断言（**它原来点了没反应**） |
| **N3** | 开局链接线：模式卡的 `Battle!` 在「联机已配置且连上」时**不跑 12 秒 bot 链**，改走 `NetSession` 匹配 | `NetBattleTest` 里跑一遍「两边交卡组 → 主机定种子/先手/战场 → 两端同参数建局」 | ✅ **2026-09-26 完成**（`Net/NetMatchmaking.cs`；两个窗各接一处，没连上时**照旧打 bot、单机行为一字不改**） |
| **N4** | 对局内：替掉 `SimpleAI`；动作收发 + 座位 + 指纹 | `NetBattleTest`：**两个裸 `BattleContext` 走 loopback 真打一局**（54 步 / 15 回合），打完指纹一致；故意改一个数要**报出来** | ✅ **2026-09-26 完成**（条数只写一处，同上；🆕 2026-09-27 又涨了 —— 补「主机掉线」+「选择点/引擎兜底」两支）（`Net/NetBattle.cs` + `Net/NetApply.cs` + 驱动的 `LocalAct`/`ApplyLoggedAction`） |
| **N5** | 投降 / 掉线 / **重连**（§5·6：权威动作流 + 全量重放） | 自检：**掐断 → 自动重连 → 重放**，指纹要追平 | ✅ **2026-09-26 完成**（自检实测：主机进「等待重连」**不判负** → 客机自动连回 → 主机灌 **57 条**权威动作流 → 客机重建+全量重放 → **指纹与主机一致**）。<br>🆕 **2026-09-27 补上「主机掉线」那一支**（`NetBattleTest` 第 6b 节）：`hs.Transport.ClosePeer()` ⇒ **两边都进等待重连、主机不退出对局**、客机自动连回来、重放追平、两边回 `InBattle`。<br>🔴 同轮**修掉两条恒真的断言**：重放那条比的原来是**掉线前那个旧 context**（`NetReplayFromNet` 里 `Ctx = Rebuild()` 换成了新建的那个）⇒ 现在比**活着的** `cb.Ctx`。**条数只写一处** → `资料/阵营推进_清单与交接.md` §一 |

### N1 实测踩到的三个坑（**都已在代码里堵掉，别再改回去**）
1. 🔴 **握手包必须「边沿触发」**：主机原来写成「状态 == 等重连 && 连接活着 ⇒ 发 challenge」——
   那是**电平**，于是他在等客机报进度的那几帧里**反复重发**，两边来回打转（实测日志里
   主机和客机各循环了两轮）。现在改成看 `INetTransport.AcceptedCount` **变没变**。
   自检 H⑧「追平之后稳住不抖」就是给这条立的桩。
2. 🔴 **主机的密码要在开台那一刻定格**：原来核对时读 `NetConfig.Current.password`（全局单例）——
   中途改设置、或同进程开两桌，**已经开打的那一桌会换密码**。现在存 `_hostPassword`。自检 J 盯这条。
3. 🔴 **`OnPeerReady` 第一次写的时候压根没调**（状态切到 Lobby 却没通知上层）——
   自检 C③/C④ 当场抓到。⚠️ **教训**：状态机光切状态不算数，**回调也要有断言盯着**。

### N3~N5 实测（2026-09-26）—— 一句话：**镜像端点这条路走不通，改成绝对座位**

- 🔴 **最大的那条**：第一版「两端都把自己当 0 号位」（镜像）**跑到第 40 步就分叉**，
  根因 = **`Unstable` 的候选单位表按座位顺序拼、镜像后同一个随机下标选中不同单位**。
  ⇒ 改成**两端同一套绝对座位**（主机 0 / 客机 1，客机 `SetMySeat(1)` 翻**视图**不翻**引擎**）。
  全文 → `资料/已知的坑.md` + `NetProtocol.Fingerprint` 的注释。
- 🔴 **`MsgAction.actor` 必须是绝对座位**：客机发自己的动作时若落在默认的 `0` 上，
  两边引擎都会把它当成**主机**的动作 ⇒ 当场「不是你的回合」。
- 🔴 **换牌必须由主机定序**（`Mulligan` 掷 `ctx.Rng`）：两边各自提交、主机算完再下发，
  而且**顺序固定**（先座位 0 再座位 1）。自检验过：换牌后两端指纹一致。
- 🔴 **指纹里不许出现 `CardInstance.Id`**（按座位顺序发号 ⇒ 号码不承载语义）；
  更要命的是**第一批指纹把 `Active`/`Winner`/按座位累加**都放了进去 —— 那是**镜像专属**的坑，
  现在座位是绝对的，直接比就对。
- ✅ **客机视图那一层批处理里也能验**：`BattleScene.Run` 末尾加了一段
  「`SetMySeat(1)` 之后手牌/`MyUnits` 认的是不是座位 1 那一方」（`_me` 翻、引擎不翻）。

### N2 实测踩到的三条（**坑的全文在 `资料/已知的坑.md`，这里只留指针**）

1. 🔴 **原版根上的 `m_LocalScale = 0.9`（设置窗独有）不能靠设 `localScale` 复刻** ——
   我们的 `ImageQuad` 按**世界尺寸**画，父级缩放对它**无效**（实测 75px 的钮在 0.9 根下仍渲 75px，
   而**位置**却按 0.9 移了 ⇒ 站对地方、大 11%）。**做法 = 把 0.9 烘进每一个矩形**
   （`SettingsWindow.Screen()`，所有画图包装统一先过它）。全文 → `资料/已知的坑.md`。
2. 🔴 **编辑模式（自检）不跑 `Awake`** ⇒ `NetRuntime.Ensure` 里建完 `Instance` 还是 null、`Session` 也没建。
   已在 `Ensure` 里显式 `Init()` + 登记（**同族先例 = `WindowsManager.EnsureHost` 里那段更正**）。
3. 🔴 **齿轮原来真的「点了没反应」** —— 修完在 `MainMenuScene.Run` 里补了断言
   （「接了点击」+「点了真能开设置窗」）：**只验矩形的断言抓不到这种缺陷**。
   ⚠️ 页签文案（Graphics / Audio / Online）**是我们定的** —— 解包里那几个 `Tab Toggle Title` 的
   `m_text` 是**西班牙语**、TMP 没挂 I2 词条，英文正式文案本地拿不到（正本 §九）。
4. 🔴 **自检会把工程设置写脏** —— 跑完 `git status` 多出 `QualitySettings.asset`（`m_CurrentQuality` 被改）
   与 `ProjectSettings.asset`（`runInBackground` 被改）。**已改成注入点 + 批处理下不设 runInBackground**，
   并补了「自检没把真值改掉」的断言。全文 → `资料/已知的坑.md`。

### 🔴 还没做的（N2 的尾巴，别当成做完了）

- **原版那 5 页里我们只建了 3 页**：`General`（语言/退出游戏）· `Account`（整页走服务器）·
  `Support`（外链）**都没建**，页签也没摆 —— 切不过去（不静默：建窗日志里写着）。
- **图像页只建了 3 件**（`Quality` / `VSync` / `FPS limit`）—— 原版那 7 行里其余的
  （`Text in Hand` / `Small Screen Size` / `Auto Zoom` / `Hi FPS` / `super sampling`）**我们没那功能，不摆假开关**。
- **音频页缺 `WindowMode Selector`**（窗口模式），同上。
- **联机页的输入框只在真 Play 里能打字**（键盘走 `PointerLayer.BeginText`）；批处理里靠 `SetText`。
- 原版勾选图 / 下拉框底的 **sprite PathID 没解出名字**（`-5728790147372056906` / `4411787853012002210` /
  `4198243566598287219`，UnityPy 按 PathID 反查没查到）⇒ 勾选改用 `40K_toggle_on/off`、下拉框底用 `40K_button`。

**每笔做完都要复跑那八条既有自检**（见 `资料/命令速查.md`），碰了外壳再加跑 `SettingsScene.Run`。

## 七、命令

```bash
UNITY="D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe"
unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod <入口> -logFile -
# 联机自检（N1 起）
#   NetSelfTest.Run        —— 传输/握手/协议（不需要两台机器）
#   SettingsScene.Run      —— 设置窗三页（N2 起）
```
⚠️ **同一工程同一时刻只能跑一个 Unity 实例** ⇒ 这些自检**必须串行**（`CLAUDE.md` 铁律 1·b）。

## 八、坑（踩了就往这里写）

- ⛔ **下面两条 2026-09-26 当天就被推翻了 —— 留痕，别再照做**：
  · 原写「**别改 `BattleDriver._me`**（座位靠协议翻译，不靠镜像视图）」—— 🔴 **反了**：
    座位改成**绝对编号**之后，**客机就是靠 `SetMySeat(1)` 改 `_me`**（改的是**视图**；引擎两端同一套编号）。
    见 §四·6 与 §5·3。
  · 原写「**别用下标引用手牌**（见 §四·2）：用 `CardInstance.Id`」—— 🔴 **也反了**：
    实际走的就是**手牌下标**（`Net/NetApply.cs` 的 `RuleCore.PlayCard(..., m.handIdx, ...)`）；
    `MsgAction.handId` 是**只写不读**的死字段。见 §四·2。
- **`ExecuteAction` 按 `ctx.Active` 认人**（`SimpleAI.cs:915`）⇒ 主机**必须先验行动方**。
- **`ChoosePicks` / `ChooseCardIds` 两条队列必须同进同出**（`BattleContext.cs:733`）—— 少发一条会让后面每一处**全部错位、而且不报错**。
- **批处理下没有帧循环** ⇒ 网络收发一律**显式 Tick**，别依赖 `Update`。
- **`Electron`/`ELECTRON_RUN_AS_NODE`**、行尾、`.ps1` 编码那几条通用坑 → `CLAUDE.md` §二，不在这里抄第二份。

## 九、还欠 / 查不到的

- **原版 PvP 的匹配细节**（谁开战场、`GetBattleArena(army)` 取谁的 army）—— **服务端**，本地判不出（§二·5 因此是我们定的）。
- **设置窗页签的英文正式文案** —— 解包里那份 `m_text` 是**西班牙语**（`Cuenta`/`Gráficos`/`Soporte`/`Multimedia`），
  TMP **没挂 I2 词条**、正式文本在远端语言表 ⇒ **英文原文本地拿不到**。
  ⇒ 我们那三个页的文字**自己定**，代码注释里标清「这是我们挑的」（§3·5）。
- 🔴 **原版没有「联机 / 网络」页** —— 搜过 `Online`/`Network`/`Server`/`Connect`/`Region`/`Ping`/`Multiplayer`/`Matchmak`，
  设置窗里**一个都没有**（命中的都在别处）。⇒ **「联机」这一页是我们新增的设计，不是复刻**，
  界面上要如实标出来，**别写成「原版就是这样」**。
- **原版没有断线/重连的界面**（全在 Photon 层：`AttemptReconnect` / `ForfeitDisconnectedEnemy`）——
  ⇒ 我们的「对手掉线，等待重连…」**也是新增的**。

### 9·1 🆕 **观战：原版压根没有**（2026-09-26 查实）—— 所以我们的「不做观战」**是忠实，不是省事**

> 原先把「不做观战」记在「**我们的选择**」那一栏 —— 🔴 **那条定性错了，已改**：
> **原版就没有观战**，我们不做它**不是偏离、是复刻**。

**搜索范围与自证**（报「没有」必须打出搜过什么）：
- 词：`spectat*` / `spectate` / `observe*` / `watch*` / `viewer` / `audience` / `broadcast` / `referee` / `live-match`；
- 目录：`GameAssembly.dll`(76.7 MB) · `il2cpp_data/Metadata/global-metadata.dat`(20.8 MB) ·
  `d:/2/tools/decomp_full/`(**25096 个函数**) · `d:/2/Warpforge_code/Scripts/`(全部程序集桩) ·
  `d:/2/新解包资源/assets_full/`(**246680 个资产**) · `资料/说明书/`(菜单全树 + 13 份战场全树)。
- **对照实验**（证明搜法有效）：同样的搜法，`photon` 命中 **1352 个文件**。
- 结果：反编译 **0/25096**；元数据里 8 处 `spectat` **全是 Steamworks 的通用 P/Invoke 桩**
  （`ISteamGameServer_SetSpectatorPort` 之类，与游戏逻辑无关）；资产 `-iname *spectat*` **0**；
  界面树（**含 inactive 节点**，菜单树 2535 个 `(inactive)`）**0 命中**；
  `MatchMakerManager` 的方法只有 `FindMatch/StartMatch/StartBotBattle/StartReplayMatch/CancelSearch/
  ChangeToBotBattle/CanMatchWithHumans`，**没有任何 join-as-observer 入口**。
- ⚠️ **唯一盲区（如实报）**：多语言**文案**是运行时下发的（连 `Ranked mode`/`Match Log` 这类界面标签
  在本地都是 0 命中）⇒ 「观战」理论上还能以「服务器下发的一条纯文本、客户端零代码零按钮」存在，
  **无法 100% 排除**，但**没有任何正面证据**。

### 9·2 🆕 **回放：原版有，而且是一整套**（同上一路查出来的）—— ✅ **2026-09-27：本地录像/回放【已做】**（`Battle/ReplayStore.cs` + `BattleDriver.PlayReplay`；界面早就有 = `Battle/ReplayBar.cs`）⇒ 这一节现在是**原版语义的出处**，不是待办。判据与「怎么录/怎么放/留 50 局」→ `项目任务.md` §三 第 18 条 第 6 件

**入口**：对局历史条目上的 `ReplayButton` —— `菜单全树.md:3232`（`Match Log` 行内）· `:16130`（`Battle Log Tab` 下）。
**代码**：`ReplayHud`（`Setup`/`Initialize`/`PlayButtonClicked`/`PauseButtonClicked`/`StepButtonClicked`/
`ReplayButtonClicked`）· `BattleLogItem.ClickReplayButton` ·
`MatchMakerManager.StartReplayMatch` → `MatchData.SetMatchRecording(...)` → `BattleManager.IsReplayMatch` /
`SetToReplayMode` ⇒ **本地重演**。
**配套字段**：`ReplaysVersion` · `ErrorLoadReplay` · `matchReplayType` · `playerIdForReplay` ·
`matchNameForReplay` · `ReplayShare`。
🔴 **2026-09-27 更正**：原来这里写「`ReplayShare`（与 `DeckShare` 同组的**分享面板**）」—— **错，没有这个面板**。
`ReplayShare` 是 **`ChatMessageType.ReplayShare = 140`**（与 `DeckShare = 150` 同属**聊天消息类型**枚举，
判据 → `资料/普查产出_0927/回放_界面真值.md` §⑤ 与 `回放_入口与数据链.md` §E·1；
**资产全库 0 命中 · `decomp_full` 0 命中**，旁证是 `DeckShare` 有 I2 词条而 `ReplayShare` 没有）。
**错因**：把它当成与 `DeckShare`「同组的面板」，而那一组的真身是**消息类型**、不是 UI。
⇒ **这一件里【没有】「分享回放」这个界面**，别去找。
**资产**：13 个战场场景各有 `Replay` / `ReplayButtons`（**13 场逐像素相同**，判据 → `回放_界面真值.md` §B）；
图集 `40K_replay_bt_{play,pause,next,restart}`（**10 张全部已在工程里**，不用再导）。
**定性**：**看自己打完那一局的录像**，**不是**看别人实时对局（与观战无关）。
📌 **对我们的意义**：我们的 `NetBattle` 里**本来就持有「权威动作流」**（重连重放用的那串 `ActionDto`）
⇒ 引擎侧有现成的地基 —— ✅ **2026-09-27 正是走这条路做出来的**（本地录像 = 同一个形状；**原版整套在 PlayFab 服务器上**，我们这版是「**加功能**、不是复刻」）。

## 十、多人界面那一批（用户 2026-09-26 追加：「**有什么复刻什么，具体的数据和排名这些可以空着**」）

⇒ **原版有的多人界面全部照原版建，服务器数据一律留空态/空值**（不编数字 —— 铁律 3）。
清单与状态 → §3·6 那张表；任务清单 → `项目任务.md` **§三 第 18 条**。
⚠️ 与用户口径冲突的两类**不做**：**赛季倒计时/赛季三窗**（用户明确不要）· **积分与红水晶**（结算里早就删了）。

---

## 十一、🆕 **「网友怎么连上」—— 联机入口那一套的完善**（2026-09-26，用户：「我们可能是网友需要联机」）

> 用户提的流程：**主机点一个按钮自动填本机 IP → 写密码；客机填主机的 IP + 密码 → 连上**。
> 这套流程**本来就有**（N2 建的）；这一轮做的是**让它真的靠得住**，并把「公网怎么走」讲清楚。

### 11·1 🔴 先修一个真 bug：**【刷新】永远填不出真地址**（这条最要命）

- 原来 `NetConfig.LocalIPv4()` 走 **`Dns.GetHostAddresses(Dns.GetHostName())`**，
  **实测在批处理里直接抛** `String conversion error: Illegal byte sequence encounted in the input.`
  ⇒ 它**回落到 `127.0.0.1`**（回环！填给对面等于没填）。
- ⚠️ **而自检是绿的** —— 那条断言只判「填进去的是不是**合法 IP**」，而 `127.0.0.1` **合法**。
  ⇒ **又一条「自检绿、功能坏」**（同族：`OvertimeEnergy` 死常量、`FromView` 翻座位）。
- **修法**：改成**枚举网卡**（`NetworkInterface.GetAllNetworkInterfaces()`）——
  不查 DNS、不依赖机器名。**顺手拿到了网卡名**，那正是多网卡时要给玩家看的。
- **断言加严**（`SettingsScene`）：断「**找得到地址**」+「**填的不是回环**」+「循环里落不到回环」。
  📌 **实测**：修前 `127.0.0.1` ⇒ 修后 **`WLAN  192.168.2.101`**。

### 11·2 【刷新】改成**可循环**（多网卡是常态）

多网卡（有线 + 无线 + VPN + 虚拟机）时**机器自己挑不准**，而**挑错的代价是朋友连不上**。
⇒ 现在**每点一次换下一个**，flash 里报「第 k/n 个 · **网卡名** · 是不是虚拟网卡」。
⚠️ **循环只在「能用的」里面转**（`LocalAddr.Usable`）——
回环（`127.0.0.1` / `::1`）与 v6 链路本地（`fe80::`）**不进循环**（第一版没排除，实测第 2 下就落到回环了）。
🔴 **2026-09-26 又补了两类**（用户问「有 IPv6 是不是就行」时发现漏的）：
**IPv4 链路本地 `169.254.x.x`**（网线没插/没拿到 DHCP 时会有，**而且常排在真地址前面**）
与 **IPv6 唯一本地 `fc..` / `fd..`（ULA）** —— 后者**看着很像公网地址，但公网路由不到**，
是最容易骗过人的那一类。现在四类一起排除，并有断言盯着。
📌 **虚拟网卡不排除、只排后面并标出来** —— 玩家用 Tailscale/ZeroTier 时，
**那张虚拟网卡的地址才是要给对面的**。

### 11·3 监听改成**双栈**（打开 IPv6 直连那条路）

原来只绑 `IPAddress.Any`，注释给的理由是「双栈会带来 `::1` 还是 `127.0.0.1` 这种麻烦」——
⚠️ **那个理由站不住**：**有公网 IPv6 的玩家不做端口映射就能直连**，这是「网友联机」最省事的一条。
⇒ 现在**先试双栈**（`IPv6Any` + `DualMode`，一张 socket 同时收 v4/v6），**不行再退回 IPv4**。

🔴 **2026-09-26 定下走 IPv6 之后，又挖出两个会让这条路整条死掉的坑**（都已修 + 有断言）：

1. 🔴 **`new TcpClient()` 在 Unity(Mono) 里建的是 IPv4 socket** ⇒ **拿 IPv6 地址去连必定失败**。
   原来 `Connect` 用的就是它 —— 也就是说**双栈监听白开了，客机根本连不上 v6**。
   **修法**：先 `ResolveHost` 把地址解析出来（**字面量优先**，含 `[::1]` 方括号写法），
   再**按它的 `AddressFamily` 建客户端**。断言：`NetSelfTest` 的 **K①~K⑤**（含「IPv6 字面量必须解析成 IPv6」）。
2. 🔴 **「端口被占」不能退回 IPv4** —— 退回会**在同一端口上起出第二台主机**
   （v6 双栈占的是 v6 那份，v4 还能绑上）⇒ **自检 B② 当场抓到**。
   **修法**：只有「双栈这条路本身不可用」才退；`SocketError.AddressAlreadyInUse` 时**直接放弃**。

🔴 **IPv6 不是「有就能连」—— 三条缺一不可**（回答用户 2026-09-26 的追问）：
1. **两边都要有公网 IPv6**（不只是主机）；
2. 主机那个地址必须是**公网全局**（`2000::/3`）——`fe80::`（链路本地）与 `fc..`/`fd..`（唯一本地 ULA）
   **都出不了公网**（后两者最像公网地址，最容易骗人）；
3. **路由器的 IPv6 防火墙要放行那个 TCP 端口** —— ⚠️ **IPv6 省掉的是 NAT/端口映射，
   省不掉「在路由器上开个口」**（家用路由器几乎都默认丢弃入站 v6）。这一条最容易忽略。

📌 为了让他**一眼看出自己行不行**，联机页那扇「怎么联机」弹窗末尾加了**本机检测**两行：
**公网 IPv6 有/没有**（有就把地址列出来）· **虚拟局域网工具装没装**（认网卡）。

### 11·4 🔴 **实测：用户那台机器现在没有公网 IPv6**（2026-09-26）

| 查什么 | 结果 |
|---|---|
| 系统上的 IPv6 | **有** —— 4 个 `fe80::`（链路本地，4 块网卡）+ `::1` |
| **公网全局（`2000::/3`）** | 🔴 **一个都没有** |
| WLAN 的地址 | 只有 `192.168.2.101`（IPv4），网关 `192.168.2.1` |

⇒ **「IPv6 公网直连」那条路现在走不通**，而且它**要求两边都有**。
⇒ **网友联机实际只剩两条**：**虚拟局域网工具**（推荐，两边装同一个）· **端口映射**（要求有公网 IPv4）。

🔴 **2026-09-27 复测：环境变了 —— 这一次【有】公网 IPv6**（同一台机器、同一个自检）。
`SettingsScene` 那批**逐地址**断言（「循环第 1…4 下」）实测填出的候选里出现了
**`2408:845d:1f30:89e9:5c7c:4713:9140:52bf`** 与 **`…:54bd:d201:2619:4751`**
（`2408::/16` 是中国电信的**全局单播**段，属 `2000::/3`）⇒ **系统当时确实拿到了公网 IPv6**。
⚠️ **两次实测都是真的，差别在「当时网络的状态」**（路由器/运营商有没有下发前缀）——
⇒ **这条别当"永远成立"**：要用之前**现查**（设置窗点一次【刷新】，循环一遍就知道）。
⇒ 上面那句「实际只剩两条」也要跟着读成「**取决于当时有没有公网 IPv6**」。
📌 顺带一条**自检口径**：`SettingsScene` 的**断言条数随网卡/地址数变**（那批是逐地址跑的）——
  所以 §一 里记它的数时要知道**它不是个定值**（2026-09-26 跑出 75、2026-09-27 跑出 80）。

⚠️ 想开 IPv6 的话：**登路由器管理页（那台是 `192.168.2.1`）看 IPv6 那一项** ——
真开了的话 WLAN 上会冒出 `240e:` / `2409:` 开头的地址（电信 / 移动的段）；没有就是运营商没给。
📌 **这条结论只在「用户这台机器 + 这个网络」上成立**，换机器/换网要重测（联机页那两行会自己报）。

🔴 **2026-09-27 第四次实测（用户问「我在测试网站上看得到 IPv6，为什么你的工具看不到」）——
查清了：那是【两件事】，用户没看错、我们的工具也没坏，是路由器在做 IPv6 NAT。**

| 问的是谁 | 手段 | 实测结果 |
|---|---|---|
| **本机网卡上挂着什么** | `ipconfig` + `Get-NetIPAddress -AddressFamily IPv6`（全量、无过滤） | **只有** ULA `fd00:485f:860:13a4::/64`（+ 一个 `Random` 后缀的临时地址）+ 4 个 `fe80::` + `::1` ⇒ **一个 `2xxx:` 都没有** |
| **外网看到的源地址是什么** | `curl -6 https://api6.ipify.org`（**成功**） | `2409:8a5c:1e47:11a0:4a5f:8ff:fe60:13a4` |

🔴 **判据（可复查）**：那个外网地址的**接口标识 `4a5f:08ff:fe60:13a4` 与默认网关的链路本地地址
`fe80::4a5f:8ff:fe60:13a4%9` 完全相同**（EUI-64 反解出的 MAC 都是 `48:5f:08:60:13:a4`）
⇒ **它是路由器的地址，不是这台机器的** ⇒ **路由器在做 IPv6 NAT（NAT66 / NPTv6）**：
它自己有全局 IPv6，给内网只发 ULA，出站时把源地址换成自己那条。

⇒ **三条读法（别读错）**：
1. **测试网站显示 IPv6 ✅ 真的** —— 但那是**路由器的**地址；
2. **我们工具说「本机没有公网 IPv6」✅ 也真的** —— 本机网卡上确实没挂 ⇒ **别人连不进来**；
3. 🔴 **所以「IPv6 直连」在这台机器当前这个网络状态下走不通**，而且**这次不是「等运营商下发」**，
   是**路由器没把前缀发给内网、自己在 NAT66**（路由器的 IPv6 开关那一页能看出来）。
   ⚠️ **但别把它写成"永远如此"** —— **09-27 白天本机网卡上确实拿到过 `2408:845d:…` 全局地址**
   （自检枚举出来的），⇒ **路由器/运营商的行为会变，前面那句「用之前现查」照旧成立**。

🔴 **同一轮查出我们工具的两条不足**（已记进 `项目任务.md`；修不修见那儿）：
- **真缺陷**：`NetConfig.LocalAddr.Usable` 只判 `2000::/3` ⇒ **`2001:0::/32`（Teredo）与 `2002::/16`（6to4）
  会被当成「公网 IPv6」列出来**，而这两种**都不能用于入站直连**（隧道 / 需要中继）⇒ **误报**。
- **口径缺一句**：界面只说「公网 IPv6：无」，玩家会像用户一样困惑
  ⇒ 该补一次**「外网看到的地址」**的探测 + 一句「**那是路由器的，不一定连得回来**」。

### 11·5 「公网怎么走」必须在界面上说清（点一下弹窗）

联机页那一行说明**只剩 ~70px 高，六行塞不下** ⇒ 页面上留一行提示，**点它弹窗**说三条路：

| 路 | 怎么做 | 前提 |
|---|---|---|
| ① 同一局域网 | 直接填主机那台机器的地址 | 无 |
| ② 虚拟局域网工具 | 两边装同一个（Tailscale / ZeroTier / 蒲公英），填它给的地址 | 两边都装 |
| ③ 公网直连 | 主机有**公网 IPv6** ⇒ 直接填 v6 地址；否则路由器**端口映射** | ⚠️ 很多宽带**没有公网 IP**，那条只能走 ② |

🔴 **我们不做 NAT 穿透 —— 这不是偷懒**：打洞的第一步就要有一台**公网会合点**，
而本项目**没有任何服务器**。「纯 P2P + 零服务器 + 两个陌生人」**在数学上不可能**。

### 11·7·b ✅ **B 档已实现**（2026-09-27，用户当天拍板、当天做完）

| 件 | 落在哪 |
|---|---|
| **UPnP 端口映射** | `Net/UpnpPortMapper.cs`（新）：SSDP 发现（**组播 + 给网关单播各发一份** —— 有些固件只应单播）→ 设备描述 → `GetExternalIPAddress` → `AddPortMapping(TCP)`；**全在后台线程**，主机点【保存】时由 `NetSession.StartHost` 自动发起，关台时 `Close()` 撤映射 |
| **如实报**（红线） | 四种失败各有各的人话，走 `NetRuntime.Notice`：① **一个 SSDP 回应都没有** ⇒ 路由器不支持 / UPnP 关着；② 找到设备但**没有 WAN 端口映射服务**；③ SOAP 被拒（带 UPnP 错误码，718/725 单独说）；④ **拿到 WAN 地址是私网段 ⇒ CGNAT**（`100.64/10` 等，映射了外面也进不来） |
| **`Notice` 线程安全** | 🆕 顺带把 `NetRuntime.Notice` 的队列**加了锁** —— 它现在**会从后台线程调**，而 `DrainNotices` 在主线程取（`Queue` 不是线程安全的） |
| ⚠️ **批处理里故意不跑** | `MapAsync` 里 `Application.isBatchMode` 直接返回 + **打一行日志说明** —— 这事**会改玩家路由器的映射表**，自检反复开台就是反复动人家的网络设备。⇒ 真 Play 验收 = **`真Play待验清单` E7** |
| **纯函数自检** | `NetSelfTest` 新增 **34 条**（60 → **94**）：设备描述解析（含「只有 Layer3Forwarding ⇒ 找不到服务」「PPP 型也认」「两种都在时优先 IP 型」）· 相对 `controlURL` 绝对化 · SOAP 信封 · **CGNAT/私网判定** · **Teredo/6to4 不算可用 v6** · 外网回显体的解析 |
| ⚠️ **NAT-PMP 没做**（有意） | 本机路由器走 UPnP，**NAT-PMP 那条路在这台机器上一步都跑不到** ⇒ 写了也是**没验过的代码**。真需要时再加，加的时候要说清「拿什么验的」 |

**同一轮还修的两条工具缺陷**（判据 → **§11·4**）：
① `NetConfig.LocalAddr.Usable` 把 **Teredo `2001:0::/32` 与 6to4 `2002::/16` 误判成「公网 IPv6」**（两者都在 `2000::/3` 里、却**都不能用于入站直连**）⇒ 改成**逐段判字节**；
② 联机页加了一颗 **【Test Public IP】**（`NetConfig.ProbeExternalAsync`）：把「**外网看到的地址**」与「**本机网卡上的**」摆在一起说清 —— 这正是用户这次困惑的根源（**两者不一样 = 路由器在做 NAT66**）。

### 11·6 主机点【保存】之后，**直接把要给朋友的那一行印出来**

`✅ 主机已就绪，等着对面连进来。` / `把这行给朋友 → 192.168.2.101 : 47777`
—— 别让玩家自己去拼 IP 和端口（用户的规格就是「主机点按钮，然后把它给朋友」）。

**自检**：`SettingsScene` 那批断言全绿（**条数只写一处** → `资料/阵营推进_清单与交接.md` §一；⚠️ 它**不是定值**，逐地址跑）。
⏳ **要真 Play 才知道的**（→ `资料/真Play待验清单.md` **E5**）：【刷新】在**真机器**上填出来的地址对不对、
那扇弹窗**文案会不会溢出**。

🔴 **2026-09-27 晚场第三次实测：又【没有】公网 IPv6 了**（同一天、同一台机器）——
`Get-NetIPAddress -AddressFamily IPv6` 只剩 **链路本地 `fe80::`（4 块网卡）+ `::1`**，
外加一个 **ULA 前缀 `fd00:485f:860:13a4::/64`**（`fc00::/7` = **私有的**，公网不可达），
而且**有** IPv6 默认路由（下一跳是路由器的 `fe80::4a5f:8ff:fe60:13a4`）——
⇒ **IPv6 协议是通的，只是路由器/运营商没下发全局前缀**。
我们自己的 `SettingsScene` 那批断言**当场也同意**：它枚举出来**只有 1 个地址** `192.168.2.104`（WLAN · IPv4）。
📌 三条实测连起来看（09-26 无 · 09-27 白天有 `2408:…` · 09-27 晚无）⇒ **这条结论的正确读法是：
「它随时会变，要用之前现查」**（设置窗点一次【刷新】即可）。
✅ **顺带确认一条界面纪律**：我们的【刷新】**不会**填「出不去」的地址 ——
`NetConfig.LocalAddresses` 的 `Usable` 把 `fe80::` / `fc00::/7`(ULA) / `169.254.*` 都挡掉了，
自检里那两条断言（「循环第 1/2 下也不会填「出不去」的地址」）现在**实测就是被这一条挡住的**。

### 11·7 🆕 **NAT 穿透 / 「网友怎么连到我」—— 四档**（2026-09-27 用户问「这个可以做吗」）

先把痛点说清：**要解决的是「别人怎么连到我」**，取决于两件事 ——
**有没有公网地址** ＋ **路上有没有防火墙拦入站**。四档，成本从低到高：

| 档 | 做法 | 成本 | 判断 |
|---|---|---|---|
| **A** | **虚拟局域网工具**（Tailscale / ZeroTier / 蒲公英）—— 两边装同一个，等于同一局域网，我们的 TCP 直连**一行不改** | 0 | ✅ **已写进联机页说明**，今天就能用 |
| **B** | **UPnP / NAT-PMP 自动端口映射** —— 主机启动时自己向路由器要一个映射（SSDP+SOAP 或 NAT-PMP），**不需要服务器** | 小（几百行·纯本机） | ✅ **2026-09-27 用户拍板：做**。**只解决 IPv4 入站**、**与 IPv6 那条路完全独立**（SSDP 走 `239.255.255.250:1900`，不碰双栈监听）。<br>🔴 **可行性已实测**（不是猜）：SSDP 探测收到 `192.168.2.1` 回应 = **TP-LINK WTA301**（电信定制）· `InternetGatewayDevice:1` · 设备描述里**有 `WANIPConnection:1`**（控制 URL **`/ipc`**）⇒ 标准 UPnP 端口映射**可用**。<br>⚠️ **两条必须先测/必须如实说的**：① **CGNAT** —— 若运营商给的是大内网，路由器 WAN 上那个地址也不是公网 ⇒ **映射了外面照样进不来**（判据：`GetExternalIPAddress` 拿到的 WAN 地址 **vs** 外网看到的地址，不一样就是还压着一层 NAT）；② 路由器 UPnP 开关**可能默认关** ⇒ 失败要把原因（不支持 / 关着 / CGNAT）**逐条说给玩家**，不许静默 |
| **C** | **UDP 打洞（STUN 式）+ 中继兜底** —— 这才是通常说的「NAT 穿透」 | **大** | ⛔ **暂不建议**：要 ① 传输层从 TCP 换成 UDP（自写可靠层：排序/重传/心跳）② 一台 **rendezvous 服务器** ③ 打洞失败还要**中继**转发流量 |
| **D** | **IPv6 直连** —— IPv6 没有 NAT，只有防火墙 | 0（**代码已支持**） | ✅ **有公网 IPv6 就走这条**（要求**两端都有** + 主机侧路由器放行入站）；⚠️ 本机公网 IPv6 **环境相关**（见 §11·4，用之前现查） |

**代码侧现状（D 档为什么是「已支持」）**：主机监听 **`DualMode = true`**（双栈）·
客机连接**先解析地址、再按 `AddressFamily` 建客户端**（`TcpClient` 默认是 IPv4 socket，这条踩过）·
玩家填的那串**先当字面量解析**（`192.168.1.10` / `2408:…` / `[::1]` 都认）。
