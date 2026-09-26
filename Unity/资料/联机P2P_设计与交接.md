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
| `SearchingOpponentWindow` 的 **`Found Player Container`** | **匹配到对手**那一态（配到了才显示） | ❌ **只做了空态** ⇒ 🔴 **P2P 的刚需，要建** |
| `Ranked Division Info` 的 `Content` 整棵 | 段位名 / 评分 / 阶梯 / `Ends in` | ❌ 只有 `Rank Title` + 两个钮 |
| `Ranked Division Change` / `New Season` / `Season Ended` 三窗 | 升段与赛季首尾 | ❌（**赛季相关的按用户口径不做**） |
| 四个排行榜（`RankedSkirmish` / `RankedClassic` / `Draft` / 嵌入的 `Ranked Leaderboard Display`） | 榜首榜 + `Last season` | ❌ 点了只出声 |
| `Player Profile Window`（6 页签：Profile/Avatar/Title/Battle Log/Trophies/Ranked） | 玩家档案 | ❌ 入口是顶栏头像的 `OpenWindowButton` |
| `Social Submenu`（联盟 + 好友）· 好友/联盟行族 6 件 | 社交 | ❌ 左栏 `SOCIAL` 点了只打日志 |
| `ChatPanel` / `ChatPreview` / `Match Log` / `Battle Log Popup` / `MessagePopupWindowDuel` | 聊天 / 战绩 / 好友挑战 | ❌（`ChatPreview` 画了没接点击） |
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
2. **不做重连**（掉线 = 判负）· **不做观战** · **不做聊天/表情**（原版有 chat，那是另一条线）。
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
| **N1** | `Net/` 四件：`NetConfig` · `TcpTransport` · `NetProtocol` · `NetSession` | 🆕 `NetSelfTest.Run`：**一个进程里开两个 socket 走 loopback**，握手 + 密码对/错 + 版本不符 + 收发 1000 条 + 心跳超时 + **掉线重连**。批处理下没有帧循环 ⇒ **显式 Tick** | ✅ **2026-09-26 完成：50 条断言全绿**（`exit=0`）。四个文件：`Assets/CardPresentation/Net/{NetConfig,NetTransport,NetProtocol,NetSession}.cs` + `Editor/NetSelfTest.cs` |
| **N2** | **设置窗**（照原版壳 + 页签）+ **图像 / 音频 / 联机 三页**；齿轮接上点击 | `SettingsScene.Run`：齿轮点得开 · 三个页签切得动 · **联机页的控件都在且位置有出处** · 图像/音频页的三根音量滑块能通到 `WarpforgeAudio` | ✅ **2026-09-26 完成：67 条断言全绿**（`exit=0`）。文件：`Shell/SettingsWindow.cs`（窗 + 三页 + `MenuInputField`）· `Net/NetRuntime.cs`（会话常驻宿主）· `Editor/SettingsScene.cs`。齿轮接线在 `Shell/MainMenuRuntime.cs` 的 `OpenSettings()`，并在 `MainMenuScene.Run` 里加了断言（**它原来点了没反应**） |
| **N3** | 开局链接线：模式卡的 `Battle!` 在「联机已配置且连上」时**不跑 12 秒 bot 链**，改走 `NetSession` 匹配 | `NetBattleTest` 里跑一遍「两边交卡组 → 主机定种子/先手/战场 → 两端同参数建局」 | ✅ **2026-09-26 完成**（`Net/NetMatchmaking.cs`；两个窗各接一处，没连上时**照旧打 bot、单机行为一字不改**） |
| **N4** | 对局内：替掉 `SimpleAI`；动作收发 + 座位 + 指纹 | `NetBattleTest`：**两个裸 `BattleContext` 走 loopback 真打一局**（54 步 / 15 回合），打完指纹一致；故意改一个数要**报出来** | ✅ **2026-09-26 完成：23 条断言全绿**（`Net/NetBattle.cs` + `Net/NetApply.cs` + 驱动的 `LocalAct`/`ApplyLoggedAction`） |
| **N5** | 投降 / 掉线 / **重连**（§5·6：权威动作流 + 全量重放） | 自检：**掐断 → 自动重连 → 重放**，指纹要追平 | ✅ **2026-09-26 完成**（自检实测：主机进「等待重连」**不判负** → 客机自动连回 → 主机灌 **57 条**权威动作流 → 客机重建+全量重放 → **指纹与主机一致**） |

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

- **别改 `BattleDriver._me`**（见 §四·6）：座位靠协议翻译，不靠镜像视图。
- **别用下标引用手牌**（见 §四·2）：用 `CardInstance.Id`。
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

## 十、多人界面那一批（用户 2026-09-26 追加：「**有什么复刻什么，具体的数据和排名这些可以空着**」）

⇒ **原版有的多人界面全部照原版建，服务器数据一律留空态/空值**（不编数字 —— 铁律 3）。
清单与状态 → §3·6 那张表；任务清单 → `项目任务.md` **§三 第 18 条**。
⚠️ 与用户口径冲突的两类**不做**：**赛季倒计时/赛季三窗**（用户明确不要）· **积分与红水晶**（结算里早就删了）。
