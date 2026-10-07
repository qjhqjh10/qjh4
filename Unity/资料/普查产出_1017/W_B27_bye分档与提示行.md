# W_B27 · `bye` 分档判弃权（A912）+「提示行」的消费方（A925）—— 写手报告（2026-10-17）

## ① A912 —— **按「有没有说再见」分档判弃权**（照主对话裁定落地）

**一句话**：对局中对面消失那**两路**现在都收得住 —— **A 路**（点了投降/退出那颗钮）先发 `Resign` ⇒
对面立刻判他负（**这一路一个字没动**）；**B 路**（没点投降就没了）只发 `bye` ⇒ **本件改成：收到 `bye`
且对面侧 `Ctx.IsOver == false` ⇒ 直接判他弃权**；**心跳超时（没有 `bye`）保持原样**（仍走 A900 那 30 秒）。

| # | 文件 · 方法 | 改了什么 |
|---|---|---|
| 1 | `Net/NetBattle.cs` · `HandlePeerClosed` | 加 `bool wasWaiting = _peerGone \|\| _countdownOn;` + `CancelReconnectCountdown(hideWindow: wasWaiting)`（先把「等他回来」那扇窗收掉、再换一句话说）+ 调判弃权 + **两档文案**（判了 / 已打完） |
| 2 | `Net/NetBattle.cs` · **新方法** `ForfeitPeerIfLeftMidGame()` | 判据一格：`_d == null \|\| _d.Ctx == null` ⇒ 出声、不判；`Ctx.IsOver` ⇒ 不判；否则 `_d.NetRemoteResign()`（**复用现成入口**）并返回 `true` |
| 3 | `Net/NetBattle.cs` §A880/A900 段头 | 把 B13 那句「⚠️ 还差一条…**这条口径没有改**」改成「✅ 2026-10-17（B27·A912）已裁、已落地」+ 两档摘要（旧结论就地作废，铁律 5） |
| 4 | `Net/NetMatchmaking.cs` | （② 顺带）`SayHintOnly` 收口到新的 `SetHint`；`Reset()` 把那行提示收掉 |

🔴 **口径注释落在哪** = `NetBattle.cs` 里 **`HandlePeerClosed` 头上那一大段 XML doc**：`<list>` 列 A/B 两路（带原版判据
`ClickExitBattle.c:44-49` 先 `SendForfeit`、`QuitApplication.c:23-35` **不发** forfeit）· 判据一格 ·「**这是【我们自己的口径】，
不是复刻**」（原版两条走**同一个** Photon 回调、且 **`bye` 这条协议消息是我们设计的**）· ⛔ 别接 `StartReconnectCountdown`
的理由 · 撤窗那道闸。`ForfeitPeerIfLeftMidGame` 的 doc 指回它（不写第二份）。

**为什么这么分档**：
- 判据**就一格**：对面侧 `Ctx.IsOver == false` =「**他没投降就没了**」。投降那一路本机已经 `RuleCore.Forfeit` 过 ⇒
  `IsOver == true` ⇒ 自然放过（**不需要第二个标志位**——多一个 = 第二份判据，迟早不一致）。
- ⛔ **不能把 `bye` 接到 `StartReconnectCountdown`**：`ReconnectCountdownExpired` 那道「会话 `Closed`/`Off` ⇒ 不判他弃权」的闸
  抄自原版 `…_d__322__MoveNext.c:146-161`，而 **`bye` 必定把会话置 `Closed`** ⇒ 净效果是「**数了 30 秒，然后什么都不做**」
  （B23 警告过的坑）⇒ 分档点落在**收到 `bye` 那一刻**。
- **顺带**：`Lost→bye` 那一档（客机重连被拒时**先** `OnPeerLost` **再**收 `bye`）现在会先把倒计时收掉 —— 不收的话它会继续跑、
  **每秒把弹窗正文改回**「对手掉线了…（N 秒后判他弃权）」，把刚说的话顶掉（说错话 = 另一种静默）。

**没动的**：四条口径（绝对座位 · 换牌主机定序 · 联机局不跑 AI · `FromWire` 不翻座位）**一条没动**；
**没新增协议消息**；**单机路径零影响**（这两条回调只在 `NetBattle.Attach` 之后才挂，单机压根没有 `NetBattle`）。

## ② A925 —— 「提示行」的消费方接上了

**原版判据（2026-10-17 现读，逐条）**：
1. 那一族里**唯一一行** = 四扇战斗入口窗共用的 `Searching Oponent Popup`（类 `SearchingMatchWindowDemo`）；全量反编译里它
   **只有两颗** TMP 文本（字段表 `d:/2/tools/il2cpp_out/dump.cs:108646-108695`）：`searchingText`(0x90) / `notEnoughPlayersText`(0xA0)。
2. **`Main Search message`** = `searchingText`：文字是**固定**的 `Searching`（键 `Demo/DeckSelectionDemo/Searching`），
   由 `_TypeWriteEffect_d__14__MoveNext.c` **无限循环**打（每 `timeBetweenSearchingLetters = 0.5s` 进一格）
   —— **原版从头到尾不改这行字**。
3. 另一颗 **`Few players online message`** 由 `_ShowPlayersMessage_d__15__MoveNext.c` 等 `timeToShowNoPlayersMessage = 15` 秒后淡入
   —— 但它被 **`useFewPlayerMessage`** 挡着：本地 `bundle_menus_assets_all/MonoBehaviour/` 里 **5 份**实例（`-7876346491111824683` /
   `-4104094843614359207` / `2027333971220400048` / `3238362926157418300` / `6077665188785245554`）**5/5 都是 `0`** ⇒ **原版一次也没显示过那行字**。
4. ⇒ **原版没有「一条会变的搜索状态行」**：这类事（匹配出错 / 连接断）它走**弹窗**
   （`MatchmakerManager__MatchMakingError.c` → `WindowsManager.ShowPopUp`，按钮 = `HidePopUp` + `ShowLoading`）
   —— 那一条我们**早就有**（`NetRuntime.Notice`，B23 报告 §①）。

**落地（🔴 我们的口径，铁律 3 已标注）**：`Shell/SearchingMatchPopup.cs`
- 新增 `ShowHint(text)` / `ClearHint()`：把那句人话画在 **`Main Search message`** 上并**顶掉打字机**
  （搜索已经撤了、对面人不在，继续打「Searching」就是**说假话**）；传空 = 收回，打字机从**冻住的播放头**接着打。
  ⛔ **不接 `Few players online message`**：那一个原版 5/5 都关着（点亮它 = 把原版**关着**的功能打开），
  且它说的是「人少」，语义对不上「对面掉线了」。
- 参数**全部照原版 prefab 读**（`…/MonoBehaviour_-6561808484806384939.json` = 本窗 `searchingText` 指的那颗 TMP）：
  `m_fontSize 50` · **autosize 1 · min 4 · max 50 · base 36** · 折行开 · margin 0 · Center/Middle
  ⇒ 折行宽 = 框宽 **700**（`MsgR − MsgL`）。**原来只传 `50f`（没折行、没自适应）** ⇒ 一旦往这行放一句人话就溢出。
  对旧打字机**行为不变**（"Searching" 50px 约 225px < 700 ⇒ 自适应收敛到上限 50）。
- 订阅生命周期：**`OnEnable` 订 / `OnDisable` + `OnDestroy` 摘**（⛔ 不挂 `Show()`/`Hide()`：
  `Show()` 可重复调（`MainMenuScene.cs:4713,4733` 就是），多播委托会**订两次**）。
- `NetMatchmaking`：`LastHint` + `OnHint` 收口到**一个口**（`SetHint`）；`Reset()` 收掉那行字（生命周期 = 这一局）。
- 超长出声：`HintLineMaxChars = 40`（按框粗算）超过就 `Debug.LogWarning`（⛔ 不让它静默缩成一片糊）。

⛔ **排位那条路（全屏 `Shell/SearchingOpponentWindow.cs`）没接 —— 硬结论 + 卡在哪**：同一份包里的那扇窗只有 `Title`
（**437.76×50 · 无 auto**）、两个 `Player Name`、一颗 `Cancel Match`（`阶段二_多人界面_原版规格.md` §6 逐节点表 ＋ `菜单全树.md`）
—— **没有任何一行放得下一句状态话**，原版那扇窗连 `Searching Oponent Popup` 都没有 ⇒ **不硬塞一行假的进去**。
要接得先由主对话裁：给它加一个原版没有的节点，或让那条路改用窗内那扇弹窗。

## ③ 断言（**没跑** —— 红线：不跑 Unity，联机自检必须串行）

- `Editor/NetBattleTest.cs` **新增 §12**（12①–12⑤ + 两个工具 `SetupA912` / `PumpOne`）：
  `bye` + 局没打完 ⇒ 判他弃权（`Winner == 1` / `ForfeitedBy == 1`）· 判负入口**恰一次**（`ResignCalls == 1`）·
  **一台会话只弹一条**（大厅那一半同时挂着却不说话）+ `NetMatchmaking.PeerGone == false` · **局已打完 ⇒ `ResignCalls == 0`**、
  `ForfeitedBy` 不被改写 · **只有掉线 ⇒ 不判、照起 30 秒** · **12⑤ 对照（灭自证）**：两个半边都打开 ⇒ **2 条** +
  `PeerGone` 被置上（证明那根接线真的挂着 ⇒ 12② 不是恒真）。
- `BareHost` 加 `ResignCalls` 计数。**为什么要它**：`RuleCore.Forfeit` 自己那条「已经判过就不再判」的早退
  （`RuleCore.cs:3685`）会把「**已经打完的局又判了一次**」这个错**完全吃掉** ⇒ 只盯 `Winner` 是**测不出来**的。
- `Editor/NetSelfTest.cs` **新增 §N**（N①–N⑨）：口是活的（`OnHint` 推来的与 `LastHint` 逐字相同）· `Show()` ⇒ 订阅者 +1
  （数 `GetInvocationList()`，因为本段自己也订了一根探针）· 提示落到那行字上 · **推 2 秒打字机不许把它顶掉** ·
  `Reset()` ⇒ 收回 + 打字机接着打 · `Hide()` ⇒ 订阅者归 0。
- 🔴 **判别式**（删掉对应实现就必红）：① 删 `HandlePeerClosed` 里那句 `ForfeitPeerIfLeftMidGame()` ⇒ 12① 红；
  ② 去掉 `ForfeitPeerIfLeftMidGame` 里 `_d.Ctx.IsOver` 那道闸 ⇒ 12③ 红；③ 把 `bye` 并回 `HandlePeerLost`（或反之）⇒ 12④ 红；
  ④ 删 `SearchingMatchPopup.OnEnable` 的 `+=` ⇒ N⑤/N⑥ 红；⑤ 去掉 `Tick` 里 `_hint == null` 那道闸 ⇒ N⑦ 红；
  ⑥ 删 `OnDisable`/`OnDestroy` 的 `-=` ⇒ N⑨ 红。
- 每对都配了**不同源**的对照：「该判的判了」(`Winner`) vs「打完的不许再判」(`ResignCalls`)；「弹了几条」vs「另一半有没有偷偷记账」(`PeerGone`)；「来了会画」vs「走了会收」。
- ⚠️ **N⑤–N⑨ 要真建一扇窗**（`SearchingMatchPopup`）—— `NetSelfTest` 至今**一行 UI 都不建**，这是头一次在那儿建 Shell 窗口。
  建不出来时**出声 + `_warn++`（不当失败）**（先例 = `NetBattleTest.NoteProbeThrow`），那种情况下这一格要挪到
  `ShellScene` / `MainMenuScene` 去验。**这是本件最可能出岔子的一格，先如实说。**

## ④ 没查清 / 没做

1. **排位那条路（全屏窗）没有提示行** —— 判据（prefab 逐节点）已查清，但「要不要给它新造一个节点」要主对话裁（见 ② 末）。
2. **`HintLineMaxChars = 40` 是我按框算的粗值**（700×148 · 自适应 4~50 ⇒ 50px 约 14 字 × 约 3 行）；越界只**出声**、不是硬闸。
3. **B23 那几句 hint 文案没改**（47 字那句在 40 之上 ⇒ 真跑时会出声警告一次）。压不压短由主对话定（压短会碰 `NetSelfTest` M⑧/M⑭ 的 `Contains`，两条都能过）。
4. **真 Play 没验**（红线），那行字的实际观感（会不会与「Searching」打架、字数够不够）只能真 Play 看；
   原版那行字的**措辞**也取不到（键在**远端 CCD**、本地无词条表）⇒ 文案是我们的，已如实标。
5. ⚠️ **判负入口的效果差异照旧**（B17 已记、本件沿用）：原版到点是 `DeadHero(对面, 3)`、对面自己投降是
   `DeadHero(对面, 2)`，而我们的 `RuleCore.Forfeit` **没有理由码那个参数** ⇒ 两档走同一个口（效果相同：立刻判对面负、不看督血）。

## ⑤ 类型检查（原样贴）
```
$ TMPDIR=/tmp/wf_b27 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
⚠️ 中途一次跑出过 **1 条编辑器错、不在本件的文件上**（别人正在写的半成品），隔几分钟重跑已消失：
`Assets\CardPresentation\Editor\DeckScene.cs(4765,34): error CS0136: 无法在此范围中声明名为"before"的局部变量…`

行尾：五份**改前改后都是纯 LF**（`CRLF = 0` 逐份数过）。⛔ **没碰**：`RuleEngine/**` · `Deck/**` · `Core/**` ·
`Shell/WindowsManager.cs` / `MenuWindowBase.cs` · **`Battle/BattleDriver.cs`（一个字节没动 —— 判负入口复用现成的 `NetRemoteResign()`）** · 正本。
