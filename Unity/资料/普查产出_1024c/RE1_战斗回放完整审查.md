# RE1 · 战斗回放全链路审查（2026-10-24）

> 角色：只读审查代理 **RE1**。**没改任何代码 / 数据 / 场景**，只写本文件。
> 审查范围：录制 → 落盘 → 列表 UI → 读取 → 开局重建 → 动作驱动 → 结束/指纹校验（+ 联机/教程两个变体）。
> 判据顺序：① 全量反编译 `d:/2/tools/decomp_full/` → ② 解包资源 → ③ 实拍/运行日志。粉丝规则书不作判据。
> 本机实据来源：`C:/Users/qjh36/AppData/LocalLow/Everguild/Warpforge/Player-prev.log`（用户 2026-10-24 16:31 真机进回放的那一趟）· 同目录 `WarpforgeBattleLog/battlelog.json` · `d:/4/_tmp_view/replays_selftest/`（自检产出的 5 份录像样本）· `d:/4/_tmp_view/20261024_sync7_menu3.log`（17:15 那批自检）。
> ⚠️ 行号一律**按符号位置读**，不要当永久坐标（本仓每轮都在变）。

---

## 0. 结论先说

1. 🔴 **用户报的那条是【真缺陷】，根因在录制端，不在回放端**：
   `BattleDriver.RecBegin` 收的是 **`Begin` 的形参**（`BattleDriver.cs:4530`），而产品那条开局路
   （`BeginFromDeckLibrary` → `Begin(seed:, myDeck:, foeDeck:)`，`BattleDriver.cs:3722`）**一个阵营都没传**
   ⇒ 录像头里的 `start.hostFaction` / `start.clientFaction` **恒为空串**。
   回放时 `Begin(myFaction: "", foeFaction: "")`（`BattleDriver.cs:3260-3261`）把 `_foeFaction` 覆盖成 `""`
   （`BattleDriver.cs:4112`）⇒ 对手那副「自动凑」的牌按**空阵营**筛卡池 ⇒ **0 张**
   （`BattleDriver.cs:4432` → `:10790-10880` → `DeckBuilder.cs:36-48` / `:74`）。
   ⇒ 对手 `手 0 / 库 0 / 弃 0`，第 0 条换牌动作就分叉、`break` 掉整条动作流 ⇒ 回放「0 回合」= **没法进行回放**。
   **实据**：`Player-prev.log:6209-6210`（`阵营  没有 hero 卡`／`阵营 `` 在卡池里一张卡都没有 … 返回空表`）·
   `:6268`（`P2 能0 手0 库0 弃0`）· `:6265`（`分叉点 = 第 0 条`）· `:6271`（`演完了：0 回合`）。
2. 🔴 **第二个 P0（用户数据丢失）**：`ProfileDataAssertE13` 的 `finally` **先**把 `ReplayStore.OverrideDir = null`、
   **再**调 `ReplayStore.ResetForTest()`（`:66` / `:69`）⇒ 删的是**玩家真录像目录**。而 `ResetForTest` 自己
   **没有「没设 OverrideDir 就拒绝」的硬挡**（`ReplayStore.cs:637-645`，同族 `BattleLogData.DeleteStoreForTest`
   `:314-324` 有）。**本机现状对得上**：`WarpforgeReplays/` **空的**，而 `battlelog.json` 里那一行
   `ReplayFile = 20261024_163126_安维尔_凯尔托克_vs_碎骨者_斯拉卡.json` **文件已不存在**；
   E13 确实在 17:15 那批自检里跑过（`20261024_sync7_menu3.log:136600 / :136833`，47 通过 / 0 失败）。
3. **回放局「进得去、出不来」**（P1）：回放整块跳过结算（这是**对的**，`BattleDriver.cs:27338`），
   但出口闸 `ExitReady` 绑在结算面板上（`BattleDriver.cs:1658` → `EndPanel.cs:184`）
   ⇒ 点/ESC **永远不响应**；`Space`/`.` 那套时间控制排在 `Ctx.IsOver` 早退**之后**（`:12005` vs `:12031`）
   ⇒ 回放里**永远收不到键**；原版那 4 颗回放钮又从不显示（`:24562 Setup(false)`）。唯一出路是 R（调试键）。
4. 一句话总评：**「录制的是入口参数，不是开局事实」** 是这条链的结构性病根；
   自检从来没抓到，是因为**自检走的是「阵营显式给全」的那条路**（`BattleScene.cs:27580`），
   产品的「阵营全空」那条一条断言都没有 ⇒ 自检全绿 ≠ 这条链能用（灭自证）。

---

## 1. 用户报的那条：对手牌库空 / 没法回放

### 1.1 判定

| 问题 | 结论 |
|---|---|
| 是缺陷吗 | **是**。不是「原版就这样」，也不是数据坏了 |
| 丢在哪一环 | **录制端**（`RecBegin` 记了错的阵营）＋ **重建端**（按阵营重算牌库，而不是按录像里的牌重建） |
| 只在回放坏、对局正常？ | 是。对局里 `_foeFaction` 是默认 `Goff`（`BattleDriver.cs:1132`），所以对手有**一副正常的牌**；录像里那一格是 `""` |
| 影响面 | **所有「对手牌由引擎自动凑」的单机局**（排位/遭遇/练习默认那几档）；从练习窗**手选过对手卡组**的局不受影响（那副是 `PlayerDeck`，会整副记进 `host/clientDeckJson`） |
| 附带影响（即使两副牌都记了） | 对手阵营那格仍是 `""` ⇒ `FactionOf(seat)`（`BattleDriver.cs:7636`）给对手返回空阵营 ⇒ **对手的卡背 / 描边 / 阵营色 / 默认环境卡（`EnvSOFor`，`:7640-7644`）按空阵营取** —— 画面上「对面颜色不对」，但牌是齐的（这条比 A1 更隐蔽，容易当成美术问题） |
| 玩家自己那一侧为什么没空？ | 因为 `myDeck` 是存档里的 `PlayerDeck`，整副录进了 `hostDeckJson`，回放按 id 展开得出来（`BattleDriver.cs:10794-10869`）；**但如果玩家那副也是自动凑的（`myDeck == null`），玩家侧同样会空** —— 同一根因的另一面 |

### 1.2 证据链（逐跳，带出处）

| # | 事实 | 出处 |
|---|---|---|
| ① | 产品开局窗那条路**不传阵营**：`Begin(seed:, myDeck: saved, foeDeck: PracticeModePopup.TakePendingOpponentDeck(), deckNote:, vars:, playMode:)` —— `myFaction` / `foeFaction` 都用默认 `null` | `BattleDriver.cs:3722-3723` · `:4024`（签名） |
| ② | `Begin` 只在**非 null** 时才写字段：`if (myFaction != null) _myFaction = myFaction; if (foeFaction != null) _foeFaction = foeFaction;` | `BattleDriver.cs:4111-4112` |
| ③ | 实际用的阵营是 `_myFaction`（按我方督军反解，`:4400-4405`）与 `_foeFaction`（默认 `Goff`，必要时对调，`:4410-4411`） | `BattleDriver.cs:1131-1132` · `:4400-4411` |
| ④ | 记录像头时**传的却是形参**：`RecBegin(myDeck, foeDeck, myFaction, foeFaction, seed, …)` ⇒ 产品路径下 `f0/f1` 都是 `null` | `BattleDriver.cs:4530-4538` · `RecBegin` 定义 `:608-653`（写 `hostFaction = f0; clientFaction = f1;`，`:642-643`） |
| ⑤ | 回放按录像头还原：`pb.Seat0Faction = s.hostFaction; pb.Seat1Faction = s.clientFaction;`（`JsonUtility` 把 `null` 写成 `""`、读回 `""`） | `Net/NetBattle.cs:108-120` |
| ⑥ | `BeginFromPendingCore` 把这两格原样喂 `Begin` | `BattleDriver.cs:3260-3261` |
| ⑦ | 回放时 `_foeFaction` 被**空串覆盖**（`"" != null`），于是不再走「默认 Goff」那支 | `BattleDriver.cs:4112` · `:1132` |
| ⑧ | 对手 `foeDeck == null` ⇒ 走「自动凑」：`StarterDeck(pool, faction="")` ⇒ 一张都筛不出 ⇒ 空表 + 两句日志 | `BattleDriver.cs:4432` · `:10879` · `RuleEngine/Data/DeckBuilder.cs:36-48 / :71-75` |
| ⑨ | 实跑日志（用户那次） | `Player-prev.log:6209` `[RuleEngine] 阵营  没有 hero 卡，牌组用默认督军`（阵营名是**空**，注意两个空格）· `:6210` `阵营 `` 在卡池里一张卡都没有 … 返回空表` · `:6268` `P2 能0 手0 库0 弃0 督军24 [4:Warlord(24)]`（`Warlord` = 占位督军）· `:6265` `分叉点 = 第 0 条（kind=100，actor=1…）` · `:6271` `演完了：0 回合 … 不是同一局` |
| ⑩ | 对照：`Restart()` 那条路**传的是已解析的字段** `Begin(_myFaction, _foeFaction, _seed+1, …)` ⇒ **代码上推得**「按 R 重开的局录出来的录像能放」，从入口窗开的第一局不能（⚠️ 这一条**没实测**，是代码推断；它同时解释了为什么这个缺陷看起来「时好时坏」） | `BattleDriver.cs:10657` |

> ⚠️ 反证检查（排除「其实记了牌、只是没画」）：如果 `clientDeckJson` 非空，`ResolveDeck(saved != null)` 会展开那副牌，
> 日志里就不会出现 `StarterDeck` 那两句（`:74`），P2 也不可能是 0/0/0。⇒ **录像里对手那副确实没记。**

### 1.3 复现路径（最短）

1. 主菜单 → 排位（或遭遇）→ 选一副**存档里的自建/预组卡组** → `Battle!` → 打完一局。
2. 档案 → `Battle Log` 页 → 那一行点「回放」。
3. 期望：双方都有 30 张、整局演出来。实际：对手手/库/弃全 0，日志 `分叉点 = 第 0 条`、`演完了：0 回合`。
4. 日志关键字（Player.log）：`阵营` `凑不出牌组` `分叉点` `演完了：0 回合` `不是同一局`。

### 1.4 修法（两级，建议都做）

**A1（最小修，一行）**：`RecBegin` 传**已解析的字段**而不是形参：
```csharp
// BattleDriver.cs:4530
RecBegin(myDeck, foeDeck, _myFaction, _foeFaction, seed, …)
```
- 为什么安全：`RecBegin` 在 `Begin` 内部、`_myFaction/_foeFaction` 已按「绝对座位 0/1」落位
  （客机的视图侧交换在 `Begin` **返回之后**才做，`:3269`）⇒ 与 `MsgStart` 的绝对座位口径一致。
- 与 `Restart()`（`:10657`）**同一条写法** ⇒ 不是新发明，是把两条路统一。
- 旧录像（`hostFaction == ""`）行为不变（照样空）—— 如实；它们本来就是坏档。

**A2（结构修，建议同批做，判据 = 原版）**：录像头**再记一份「实际用的牌」**（双方 card id 列表），
回放时走 `Begin` 已有的 `exactMine / exactFoe`（`BattleDriver.cs:4028`，教程局就是走这条，绕开 `ResolveDeck`）。
- 判据：原版 `BattleRecordData` **双方各存一份牌组**（`playerData +0x40` / `oppData +0x38`，`DUMP:77673` 段；
  映射见 `资料/普查产出_0927/回放_入口与数据链.md` §C·①）—— 原版从来不是「回放时再算一遍对手的牌」。
- 好处：A1 只修好「阵营」，A2 才修好「牌」本身 —— 将来 `StarterDeck` / 卡池顺序 / 补防御卡逻辑一改，
  A1 的旧档仍会静默变一局；A2 不会。
- 版本：纯**新增**字段（与 `tutorialStage` / `botSeatPlus2` / `mulliganPlus1` 同一条哨兵纪律，
  `ReplayStore.cs:171` / `:198` / `:224`）⇒ **`version` 不必 +1**；老档读出来是空 ⇒ 退回 A1 那条路。

**A3（同族，顺带）**：录像头里的 `arena` 记的是**场景名** `"Battle"`（`BattleDriver.cs:4536`
`SceneManager.GetActiveScene().name`），**13 场战场本身没记**，回放时靠「我方阵营」重新推
（本次日志 `§27 战场：battlearenaaeldari（我方阵营 SaimHann）`）。阵营一错战场也跟着错。
A1 之后这条自然对齐；若要彻底，把战场 id 也记进头（纯新增字段）。

### 1.5 验收（断言 + 真机）

- **断言（灭自证型，必须新增）**：在 `Editor/BattleScene.cs` 回放那一节（现判据在 `:27580` / `:27661` / `:27708`）
  加一条**走产品形状**的用例：
  ```
  driver.Begin(seed: 20261024, myDeck: <一副存档 PlayerDeck>);   // ⛔ 不传 factions、不传 foeDeck
  …打完（SimulateAiTurn 推完）→ 记 LastReplayFile
  rec = ReplayStore.Load(...); 断 rec.start.clientFaction 非空、rec.start.hostFaction 非空
  same = driver.PlayReplay(rec); 断 same == true
  断 driver.ReplayDivergenceForTest < 0
  断 回放后 Ctx.Players[1] 的 手+库+弃+场上 张数 == 录制时同一格（而不是 0）
  ```
  **灭自证**：再补一条负例 —— 把 `clientFaction` 强行改成 `""` 再放，必须**红**
  （证明这条断言量的就是阵营那一格，而不是恒真）。
- **真机**：按 §1.3 走一遍，看对手 HUD 的牌库/手牌数非 0、对局能演到终局；
  日志里**不再出现** `阵营  没有 hero 卡` 与 `分叉点`。

---

## 2. 链路图（现状 / 原版 / 差距）

| 段 | 现状（落点） | 原版（判据） | 差距 |
|---|---|---|---|
| **① 录制** | 三个记账口（玩家 `LocalAct` / AI `SimpleAI.Executed` / 教程 `TutorialScript.Executed`）+ 换牌/投降/进攻卡伪 kind；**只记种子，不记 RNG 序列**（`BattleDriver.cs:459-477`） | `BattleManager` 全程只往 `matchData.playerActionsLog/enemyActionsLog` 写；`BattleRecordData` 带 **`rngNum(int[])` + 双方牌组 + 双方换牌 + 双方环境卡**（`DUMP:77673`；§C·① 那张表） | 🔴 **双方牌组只记了「有存档的那份」**（A1/A2）；RNG 只记种子（不是缺陷，但更脆）；聊天没记（原版 `actionType=55`，见 E4） |
| **② 落盘** | `Save` → `persistentDataPath/WarpforgeReplays/<时间>_<我>_vs_<敌>.json`，紧凑 JSON；`MaxKept=50`，超出删最旧；版本闸 `version=4` 不认就出声返回 null（`ReplayStore.cs:334-357 / :545-579 / :618-634`） | 没有本地文件：客户端只存 `recordingIndex` + `replaysVersion`，正文在 PlayFab CloudScript 430/440/620/650（§C/§D） | 形态不同是**我们自建**（用户 2026-09-27 拍板），不算差距；但**删档无硬挡、无出声**（B1/B2/B3）与原版「用户数据不静默丢」的精神冲突 |
| **③ 列表 UI** | 档案窗 `Battle Log` 页 → `MatchLogRow`；行来源 = 内存表 `BattleLogData.All` + 「盘上有录像但表里没记录」的兜底行（`BattleLogData.cs:335-393`；`BattleLogTab.cs:125 / :216-246`） | `BattleLogItem.Initialize`；`replayButton.interactable` 由 `matchLog.replaysVersion(+0x78)` 决定（`DF/BattleLogItem__Initialize.c:205-212`；方向仍欠账，见 `回放_入口与数据链.md` §E·3） | 🔴 我们**没有可点门槛**（D1）；点击失败**屏上零反馈**（D2）；battlelog 与 replays **各自裁 50**（D3）；兜底行快照不刷新（D4） |
| **④ 读取** | `MatchLogRow.OnReplay` → `ReplayStore.Load` → `PendingPlay` → `LoadScene(rec.start.arena)`（`MatchLogRow.cs:404-420`；`BattleDriver.cs:2250-2255`） | `RecoverMatchRecording(recordingIndex, …)` → 服务器取回 → `StartReplayMatch(BattleRecordData)`（§C·③） | 我们走的是本地文件，等价物；差的是失败反馈与场景名兜底（D2/G2） |
| **⑤ 重建** | `PlayReplay` → `NetPendingBattle.FromStart(start, isHost: mySeat==0)` → `BeginFromPendingCore`（或 `BeginTutorial`）→ `RuleCore.NewBattle`（`BattleDriver.cs:771-833`） | `MatchMakerManager.StartReplayMatch` 重建 `MatchData`（`matchType=160`）→ `BattleManager` 走同一条开局链 | 🔴 阵营/牌组（A1-A4）；🔴 **我们回放的 `MatchType` 不是 160**（由 `PlayMode` 派生，`BattleContext.cs:926`）⇒ 原版按 `matchType==0xA0` 才开的东西我们全没有（C3） |
| **⑥ 驱动** | 动作**一口气**灌完（`for` 循环 + `ApplyLoggedAction`→`NetApply.Apply`），画面靠 `_timeline` 慢慢演；逐条比 `DeepHash` 找分叉（`BattleDriver.cs:871-967`） | `NextReplayAction` / `ReplayPlayAction` 协程逐条演；`ReplaySkipAnims` 跳动画；4 颗钮 `Play/Pause/Step/Restart` | 🔴 4 颗钮不显示 + 键盘控制不可达（C2/C3）；`Restart` 语义不是「重播这一份」（C4） |
| **⑦ 结束** | 回放**跳过整块结算**（不写对局记录/不推日常/不弹面板 —— 正确，`BattleDriver.cs:27330-27343`）；末了比 `StateHash` 与轨迹摘要（`:977-984`） | `RequiresResultRecording` / 白名单里**没有 Replay(160)** ⇒ 回放不会再存一份（§C·① 那张白名单表） | 口径一致 ✔；但**没有出口**（C1），玩家只能按 R |

---

## 3. 问题清单（按根因分组）

> 每条：`编号 | 现象 | 判据 | 我们现状 | 修法 | 验收 | 建议写手`

### 根因 A —— 录像头记的是「入口参数」，不是「开局事实」（P0）

| 编号 | 现象 | 判据 | 现状 | 修法 | 验收 | 写手 |
|---|---|---|---|---|---|---|
| **A1** | 单机局回放对手牌库 0 张、第 0 条分叉、演 0 回合（= 用户报的那条） | 原版 `BattleRecordData` 双方各存牌组（`DUMP:77673`，`oppData +0x38` / `playerData +0x40`） | `RecBegin(myDeck, foeDeck, myFaction, foeFaction, …)` 传形参，产品恒 `null` ⇒ `start.*Faction = ""`（`BattleDriver.cs:4530` / `:642-643` / `:4111-4112` / `:3722`） | §1.4-A1：改传 `_myFaction / _foeFaction` | §1.5 那条产品形状用例 + 负例 | 战斗线写手（1 行） |
| **A2** | 即使阵营修好，对手牌仍是「回放时重算」的 ⇒ 算法/卡池一漂移旧档静默变局 | 同上（原版存的是牌本身） | 只存 `PlayerDeck` JSON，自动凑的那副一张都不存（`BattleDriver.cs:640-641`） | §1.4-A2：新增 `hostCardIds/clientCardIds`，回放走 `exactMine/exactFoe`（`BattleDriver.cs:4028` 已有这条路） | 同一局录→放，逐张卡 id 列表相等；改 `StarterDeck` 顺序后旧档仍放得出 | 战斗线 + 引擎线（各半） |
| **A3** | 战场不是按录像还原，而是按「我方阵营」重推 | 原版 `MatchData` 里带环境/战场上下文（`BattleRecordData.playerEnviromentalCard +0x28` 等，`DUMP:77673`） | 录像头 `arena` = 场景名 `"Battle"`（`BattleDriver.cs:4536`），真战场靠阵营推 | 头里加 `arenaId`（纯新增），重建时直接读 | 录一场 `battlearena3`，回放日志 `§27 战场` 与录制时相同 | 战斗线写手 |
| **A4** | 玩家那副若是「自动凑」的（`myDeck == null`），回放里**我方也空** | 同 A1 | `ResolveDeck(myDeck=null)` → 同样按 `_myFaction=""` 自动凑 ⇒ 空 | A1 自动修好（传 `_myFaction`） | 用「没有存档卡组」的夹具开一局 → 录 → 放 | 随 A1 |

### 根因 B —— 自检会删玩家的真录像（P0，数据丢失）

| 编号 | 现象 | 判据 | 现状 | 修法 | 验收 | 写手 |
|---|---|---|---|---|---|---|
| **B1** | 跑一趟 `MainMenuScene.Run` 就清空 `persistentDataPath/WarpforgeReplays/` | 同族正确写法就摆在同一目录：`NetSelfTest.cs:925-929` / `:1023-1027`（`ResetForTest` 必须**还在临时目录上**时调，再还原 `OverrideDir`，注释白纸黑字写着「⛔ 顺序反了 = 删掉玩家的真录像」） | `ProfileDataAssertE13.cs:66` 先 `ReplayStore.OverrideDir = null;`，`:69` 再 `ReplayStore.ResetForTest();` | 调换顺序：先 `ResetForTest()`（还在临时目录上）→ 再还原 `OverrideDir`（照 `NetSelfTest` 抄） | 断言：临时目录里放 1 份录像 → `OverrideDir=null` → `ResetForTest()` → 断言文件**仍在**（靠 B2 的硬挡） | 自检断言线写手（2 行） |
| **B2** | `ReplayStore.ResetForTest()` 没有「没设 OverrideDir 就拒绝」的硬挡 | `BattleLogData.DeleteStoreForTest`（`:313-324`）与 `DailyData` 都有硬挡并 `LogError`；这是本仓自己的纪律 | `ReplayStore.cs:637-645` 直接删 | 抄 `BattleLogData.DeleteStoreForTest` 的守卫：空 ⇒ `LogError` + `return`（把「删真档」变成编译期挡不住的**运行期硬挡**） | 单测：`OverrideDir=null` 调它 ⇒ 返回且真目录里的样本文件还在 | 自检断言线写手 |
| **B3** | 删用户数据**一声不响**（`ResetForTest` 一行日志都没有） | 铁律「不许静默失败」；`TrimToLast` 删文件时是会 `Debug.Log` 的（`ReplayStore.cs:627`） | `:642` 直接 `File.Delete` | 每删一份打一行 `[Replay] 自检清空：<路径>`；并且**只许在临时目录**时打 | 日志里能看出「是谁删的、删了哪些」 | 自检断言线写手 |

### 根因 C —— 回放局进得去、出不来；播放控制不可用（P1）

| 编号 | 现象 | 判据 | 现状 | 修法 | 验收 | 写手 |
|---|---|---|---|---|---|---|
| **C1** | 回放里点鼠标 / 按 ESC **都不退出**，只能按 R（调试键） | 原版 `BattleManager.matchFinishedAndWaitingToLeave(+0x510)` 是**结算链**置的；回放不是「真打完」，但我们也没给回放自己的出口 | `ExitReady = _endPanel != null && _endPanel.ExitReady`（`BattleDriver.cs:1658`）；`EndPanel.ExitReady = Visible && _doors.Finished`（`EndPanel.cs:184`）；回放**整块不调 `_endPanel.Show`**（`BattleDriver.cs:27338-27343`）⇒ 恒 `false` | 回放专属出口：`_replaySession && _timeline.Count == 0`（演完）时允许点/ESC → `LeaveBattle()`；演到一半按 ESC 也应该能走 | 真机：进回放 → 点一下 ⇒ 回主菜单；演到一半 ESC ⇒ 回主菜单 | 战斗线写手 |
| **C2** | 回放里 `Space`（暂停）/ `.`（单步）**没反应** | `Update` 的分支次序就是判据（同一个函数里） | `Ctx.IsOver` 早退在 `:12005`，`HandleTimeControlKeys()` 在 `:12031` —— 回放一开始 `IsOver` 就恒真（动作一口气灌完）⇒ 永远到不了 | 把时间控制挪到 `IsOver` 分支**之前**（或在该分支里加一句 `if (_replaySession) …`）；⚠️ 不能顺带放开别的输入 | 真机：回放中按 Space ⇒ 日志 `键盘 Space ⇒ 暂停`、画面停住；按 `.` ⇒ 推进一条 | 战斗线写手 |
| **C3** | 原版那 4 颗回放钮（`ReplayButtons`）在**真回放里从不出现** | 原版 `ReplayHud.Setup()` = `objHolder.SetActive(BattleManager.matchType == 0xA0)`（`资料/普查产出_0927/回放_入口与数据链.md` §A·3；`ReplayBar.cs:327-330`）；回放局 `matchType=160`（`StartReplayMatch`，§C·③） | `BuildHud` 里 `_replayBar.Setup(false)` 写死（`BattleDriver.cs:24562`），注释还写着「我们**从不进回放局**」——**已过期**；`Setup(true)` 全仓只有自检在调用（`BattleScene.cs:10374 / :18406`） | 进回放时 `_replayBar.Setup(true)`、`Begin` 时 `Setup(false)`；同时**如实记**：我们 `MatchType` 由 `PlayMode` 派生（`BattleContext.cs:926`），永远不是 160 ⇒ 原版那些 `matchType==0xA0` 分支（`ReplaySkipAnims` / `SetupReplayMulligan` / `RequiresResultRecording`）我们没有对等物 | 真机：回放局左上角 4 颗可见、4 颗都能点 | 战斗线写手 |
| **C4** | 回放里点 `Replay`（或按 R）= **开一局全新的对局**，不是「重播这一份录像」 | 原版 `ClickRestartReplay` = 重载当前场景（同一份回放重来） | `ApplyPendingReplay` → `Restart()`（`BattleDriver.cs:6694`）→ `Begin(_myFaction, _foeFaction, _seed+1, …)` ⇒ 新种子、`_replaySession` 清零（`:4168`） | `PlayReplay` 里把 `rec` 存成字段（如 `_replayRec`）；`Restart` 在 `_replaySession` 时改为重新 `PlayReplay(_replayRec)` | 真机：回放中按 `Replay` ⇒ 同一份又重新演一遍（日志同一 `savedAt`/同一 `finalHash`） | 战斗线写手 |

### 根因 D —— 列表行与录像文件不同步（P1/P2）

| 编号 | 现象 | 判据 | 现状 | 修法 | 验收 | 写手 |
|---|---|---|---|---|---|---|
| **D1** | 没录像 / 文件已被删 / 版本不认的行，**回放钮照样能点**（点了什么也不发生） | 原版 `replayButton.interactable` 由 `replaysVersion(+0x78)` 决定（`DF/BattleLogItem__Initialize.c:205-212`；方向仍有欠账，见 `回放_入口与数据链.md` §E·3） | `MatchLogRow.Build` 无条件建钮 + 无条件挂 `Hit`（`MatchLogRow.cs:278-283`）；`OnReplay` 空 `ReplayFile` 只打日志（`:406-411`） | 建钮时判：`ReplayFile` 非空 **且** `File.Exists(Path.Combine(ReplayStore.Dir, m.ReplayFile))` 才给可点态；否则画成不可点（原版就是 `interactable=false`） | 自检：造一行 `ReplayFile=""` → 断这颗钮不可点；真机：删掉一份文件重进页 → 钮变灰 | 界面线写手 |
| **D2** | 点回放失败**屏上零反馈**（只有 Console） | 「不许静默失败」；原版点钮→`RecoverMatchRecording` 有 `onErr` 回调（§C·③ 那条链，`onErr=null` 是它自己那行的实情，但**不代表玩家无感**） | `ReplayStore.Load` 只 `Debug.LogWarning`（`:550` / `:559`），`OnReplay` 直接 `return`（`MatchLogRow.cs:412-413`） | 至少复用现成的提示通道（`SetHint`/toast）说一句「这份录像已不在/版本不认」；⚠️ 原版失败文案**没有判据** ⇒ 用我们自己的话并标明 | 真机：删文件后点 ⇒ 屏上有字，不是「点了没反应」 | 界面线写手 |
| **D3** | battlelog 与 replays **各自裁 50**：一边删了、另一边还留着 ⇒ 死行 | 两处上限被刻意绑成同一个数（`BattleLogData.cs:39-40` / `:283`，`ReplayStore.MaxKept=50`），但**裁剪动作没有互相通知** | `ReplayStore.TrimToLast`（`:618-634`）删文件；`BattleLogData.Trim`（`:280-284`）删记录 —— 各删各的 | 裁录像时把对应 `ReplayFile` 从 battlelog 里清掉（或依赖 D1 的 `File.Exists` 兜住） | 造 51 份录像 → 最旧那一行不可点 | 界面线写手（跟 D1 一起） |
| **D4** | 盘上兜底行（`_extra`）**只拍一次快照**；`RefreshRows()` 全仓**零调用** | 代码自身 | `BattleLogTab.cs:125` 在 `Build()` 里拍一次；`:250-254` 的 `RefreshRows` 没有任何调用点 | 在页面打开/切页时调 `RefreshRows()`（或在 `ProfilePage` 的打开钩子里） | 真机：进页时有 N 行 → 退出去打一局 → 再进页 ⇒ 多一行 | 界面线写手 |

### 根因 E —— 录制/对账语义的残留缺口（P2，多数已登记）

| 编号 | 现象 | 判据 | 现状 | 修法 | 验收 | 写手 |
|---|---|---|---|---|---|---|
| **E1** | `MulliganDone` 录制端/回放端取 `DeepHash` 的时机不一致（已知开） | 契约「逐条轨迹一一对应」（`RecRaw` 的 `_rec.trace.Add` 与 `PlayReplay` 的 `replayTrace.Add` 必须同点） | 见 `资料/任务线/战斗线/任务总表.md` 已登记；`PlayReplay` 里有「开局次序」两条结构断言（`BattleDriver.cs:912-925`） | 按已登记方案收口（录制端在 `EndMulligan` 前后取点与回放端对齐） | 轨迹摘要跨进程逐位相同 | 战斗线写手 |
| **E2** | 三处自检直调 `RuleCore.*` 不经记账口（已知开） | 铁律「新增会改引擎状态的动作路径必须走两个记账口」 | `资料/任务线/战斗线/任务总表.md` 已登记 | 改走 `LocalAct` / `SimulateAiTurn` | 回放终局指纹等于录制时 | 自检断言线写手 |
| **E3** | 同一秒内两局、督军名相同 ⇒ **文件名相同 ⇒ 后写覆盖前写**（静默丢一份） | 文件名 = `yyyyMMdd_HHmmss_<我>_vs_<敌>`（`ReplayStore.cs:361-365`） | `Save` 直接 `File.WriteAllText`（`:343`），不查重 | 文件名加 3 位毫秒或短 id；或 `Save` 发现同名时加后缀并出声 | 同秒存两份 ⇒ 两个文件都在、`battlelog` 两行指向不同文件 | 战斗线写手（5 行） |
| **E4** | 回放里**没有对局中的聊天气泡/单位台词回放** | 原版把聊天写进动作流：`VoiceLinesController.AddChatToReplayLog` 追加 `PlayerAction(actionType=0x37=55)`（`DF/VoiceLinesController__AddChatToReplayLog.c:24-46`；且 `matchType==0xa0` 时直接 return） | 我们的动作流只收引擎动作（`MsgAction` 的 kind 0/1/2/4/5/6/100-103/200-202），没有聊天 kind | 加一个**只落盘**的伪 kind（同 `RecKindForfeit` 的做法），回放时只喂表现层（`UnitChatPanel`/`ChatPopupPanel`），⛔ 不动引擎 | 回放里能看到录制时的那几条气泡/台词 | 战斗线写手（跟特效/语音线协同） |
| **E5** | 原版存 `rngNum(int[])`（真随机序列），我们只存种子 | `BattleRecordData.rngNum +0x18` ← `MatchData.randomNumbers +0x20`（`DUMP:77673`） | `start.seed` 一个 int（`BattleDriver.cs:636`） | **不是缺陷**（动作逐条录，种子够用），但更脆：任何一处多抽一次随机就整条错位（E1 就是这么来的）。建议**如实记在 `ReplayStore.cs` 文件头**，别让人以为「有种子就万无一失」 | —（文档口径） | 战斗线写手（文档） |
| **E6** | 🆕 **录制端与回放端不是同一个「落地实现」**：录制走 `LocalAct(act, apply)` 里那个**手写 lambda**（`RuleCore.PlayCard/…` 直调，`BattleDriver.cs:276-298`，调用点 `:11746 / :12574 / :13318`），回放走 `NetApply.Apply`（另一份把 `MsgAction` 翻成 `RuleCore.*` 的实现） | 契约是「同一串动作用同一个引擎落一遍」；两条实现一旦漂移就是**静默另一个局面**（E1 那种分叉的温床） | 两套都在，靠「行为恰好一致」+ 轨迹哈希兜底 | **不要求合并**（`NetApply` 同时服务联机，合并风险更大），但要加一条**差分断言**：同一 seed、同一串动作，一边走 `LocalAct` 的 lambda、一边走 `ApplyLoggedAction`，**每一步 `DeepHashForTest` 必须相等**（现在没有这条；`BattleScene.cs:28081` 只验了 `ApplyLoggedAction` 自己） | 差分断言绿；将来任一侧改落地逻辑它会当场红 | 引擎线 + 自检线 |

### 根因 F —— 自检口径不闭合（为什么这条缺陷能一路全绿）

| 编号 | 现象 | 判据 | 现状 | 修法 | 验收 | 写手 |
|---|---|---|---|---|---|---|
| **F1** | 回放自检**从来不跑产品的开局形状** | 自检应该覆盖「玩家真实那条入口」，否则就是自证 | 回放那一节用 `driver.Begin("Ultramarines", "Goff", 20260927)`（`BattleScene.cs:27580`）—— **阵营是显式给的** ⇒ A1 恒不可现；`BattleMiscAssertE4` 同理 | 加 §1.5 那条「不传 factions / 不传 foeDeck」的用例（**并与现有那条并列**，不要替换） | 新用例在修 A1 前红、修后绿 | 自检断言线写手 |
| **F2** | 自检的录像样本里 `hostDeckJson/clientDeckJson` **都是空的** | 实测 `d:/4/_tmp_view/replays_selftest/*.json`：5 份样本里 4 份两份牌都是 `len 0` | 因为自检也是 `Begin(factions…)`（无 deck）⇒ 录像里没有牌，回放靠阵营重算 ⇒ 「对手牌库」这条在自检里**从来没被录像读过** | 随 F1 一起修；断言里显式比「录像里记的牌数 == 回放后的牌数」 | 同上 | 自检断言线写手 |

### G —— 补充维度（我加的）

| 编号 | 现象 | 判据 | 现状 | 修法 | 验收 | 写手 |
|---|---|---|---|---|---|---|
| **G1** | 联机局的录像能不能放？ | `NetMatchmaking` 填了 `hostFaction/clientFaction`（`:792-793`）且双方 `PlayerDeck` 都在包里 ⇒ 结构上**能放** | 座位是绝对的、`RecMsg` 覆盖对手那一半（`BattleDriver.cs:715-734`）；`FromStart(isHost: mySeat==0)` 还原朝向 | 不需要改；**但缺一条自检**：录一局联机 → 客户端放 | 客户端放联机录像，`finalHash` 相等 | 联机线写手（自检） |
| **G2** | 录像头 `arena` 是场景名；如果那场景不存在 / 名字变了 ⇒ `LoadScene` 抛 | `MatchLogRow.cs:414-419` 只做 `IsNullOrEmpty → "Battle"` | 同上 | 加 `Application.CanStreamedLevelBeLoaded(scene)` 判一下，失败出声并退回 `"Battle"` | 改一个坏 `arena` 值，点回放 ⇒ 有字、不崩 | 界面线写手（3 行） |
| **G3** | 回放局**不写对局历史 / 不推日常 / 不计成就**（这是**对的**） | 原版录制白名单**不含 Replay(160)**（§C·① 表）；我们 `_replaySession` 整块跳过（`BattleDriver.cs:27338`） | 已正确，且 `BattleScene` 有三条断言钉着（`:27810-27819`：结算账 / 对局记录 / 日常骷髅） | 不动 | — | — |
| **G4** | 回放里再按 R 会开新局并**重新开始录制**（`RecordReplays` 已还原）⇒ 会多一条对局记录 | `PlayReplay` 的 `finally` 恢复 `RecordReplays`（`:986`）；`Restart → Begin` 清 `_replaySession`（`:4168`） | 目前「R = 开新局」是调试语义（C4 要改掉）；改完之后这条自然消失 | 随 C4 | 按 R 之后不再多出一条 battlelog | 随 C4 |
| **G5** | 回放期间**棋盘输入**没有专门禁用 | 原版回放局 `MatchType=160` 走另一套（`ReplaySkipAnims` 等），玩家点击主要走 `TouchInputManager` | 因为 `Ctx.IsOver` 恒真，`Update` 在 `:12024` `return`，输入链到不了棋盘 —— **实际是安全的**（靠 `IsOver` 这条副作用，不是显式闸） | 保持；但在 C1 修「回放出口」时**注意别把整条输入链放开**（只放「点/ESC 离开」与 4 颗钮） | 真机：回放中点棋盘不产生任何动作 | 随 C1 |

**合计：28 条**（A 4 · B 3 · C 4 · D 4 · E 6 · F 2 · G 5）—— 其中 **G3 是「已正确，登记为不需动」**，
其余 **27 条要动**；**P0 = A1/A2/A4/B1/B2/F1**（用户报的那条 + 正在丢用户数据 + 让它全绿的那条自检口径）。

---

## 4. 复现与取证步骤（真机）

**① 复现「对手牌库空」**
```
1. 起 player（壳里进排位/遭遇 → 选存档卡组 → Battle!）→ 打完一局 → 回主菜单
2. 档案 → Battle Log → 那一行点「回放」
3. 看 HUD：对手手牌/牌库数是否为 0
4. 看日志（%USERPROFILE%\AppData\LocalLow\Everguild\Warpforge\Player.log）关键字：
   `[Replay] 开播` → `FromStart` 结果 → `阵营 `（空阵营那句）→ `[Replay] 分叉点 = 第 0 条` → `演完了：0 回合`
5. 取证：把 Player.log 里这 6 行拷进报告；再 `json.load` 那份录像看 `start.hostFaction/clientFaction`
   （**注意本机现在那份已经被自检删了**，要重打一局；见 ②）
```
**② 复现「自检删真录像」**
```
1. 真打一局（或手放一份 json 到 %USERPROFILE%\AppData\LocalLow\Everguild\Warpforge\WarpforgeReplays\）
2. 跑 MainMenuScene.Run（`_run_8_checks.sh` 里那条 menu）
3. 跑完看那个目录：**空了** ⇒ 复现
4. 判据：日志里没有一行「[Replay] 删了 …」；`WarpforgeBattleLog/battlelog.json` 还在、`ReplayFile` 指向不存在的文件
```
**③ 复现「回放出不去」**
```
进回放 → 点屏幕任意处 / 按 ESC ⇒ 无反应；按 R ⇒ 开了一局新的普通对局（不是重播）
```

---

## 5. 没查清 / 还差什么

| # | 没查清 | 还差什么 |
|---|---|---|
| 1 | 原版「回放失败/录像不可用」时**玩家能不能看到提示** | 需要读 `BattleLogItem.ClickReplayButton` 那两条回调（`b__15_0` / `onErr`）的**实参**（`RecoverMatchRecording(..., lVar5, 0, ...)` 里第 4 个 `0` 是不是 onErr=null）—— Ghidra 伪码里那两个 `undefined8` 没读实 |
| 2 | 原版回放**结束**时弹不弹结算/结果屏 | `StartReplayMatch` 之后 `BattleManager` 在 `matchType==160` 下走哪一支没查到（`ReplaySkipAnims` 只说明跳动画） |
| 3 | `replaysVersion` 的方向（非空=有回放 vs 空=有回放） | `回放_入口与数据链.md` §E·3 已列为欠账；要么真 Play dump `BattleLogItem`，要么把 `DAT_184288ae8` / `DAT_1842b6638` 两个常量读出来 |
| 4 | 我们回放里 `_timeline` 的画面到底「演得像不像原版」（动作一口气灌完、画面慢慢放） | 静态读不出来 ⇒ **必须真 Play 看一遍**；本次日志里 0 回合所以连录像都没看到 |
| 5 | `RefreshRows` 零调用的真实后果（页面到底何时重建） | 需要追 `PlayerProfileWindow` / `ProfileTab` 的页面生命周期（本次没读完），或真机「打一局再进页」验 |
| 6 | E13 删档的**精确时刻** | `ResetForTest` 不打日志（B3），只能靠目录 mtime（17:12）与 menu3 批次（17:15）对齐；代码层面的因果关系是确定的，分钟级对不上不影响结论 |

---

## 6. 顺手发现（不在回放链上，但值得记账）

1. **`ReplayStore.Load` 里的旧卡 id 迁移闸（`:564-614`）今天实际是死路**：它跑在版本闸（`:555`）**之后**，
   而带旧 token 的录像全是 `version ≤ 3` ⇒ 在闸那里就被拒了。**今天不会误伤**，但注释里那句
   「不迁的话这份录像必然报不是同一局」会让人以为老档还能救 —— 其实救不了（`trace` 里的 hash 是用旧 id 算的，
   迁完牌名对得上、hash 依然不等）。建议在注释里补一句「本闸只对『用旧 token 录出来的 v4 档』兜底」，别当它是老档的复活通道。
2. **`BattleDriver.cs:6656` / `:23861` / `:24558` 三处注释还写着「我们**从不进回放局**」** —— 2026-09-27 起就已经进了
   （`PlayReplay`）；这类「过期注释」下一轮会直接误导 `ReplayBar.Setup` 的取舍（C3 就是被它锁住的）。
3. **`_replayPaused` 在 `Begin` 里被复位（`:4274`）**，但回放开局不会自动置回 false 以外的状态 —— 目前没问题，
   等 C2 把键盘控制挪到前面之后要重新核一遍「进回放时是播放态」。
4. **`ReplayStore.LastSavedFile` 只留一份**，崩溃后无法从内存找；`battlelog.json` 是唯一索引 —— 这也解释了
   B1 一旦发生，那局录像就**彻底找不到**（列表那行只知道文件名，文件没了）。
5. **`FallbackRows` 的 `StoreOn` 门**（`BattleLogData.cs:205-209` / `:356`）设计是对的（批处理不读玩家真录像），
   但它同时意味着：**批处理自检永远看不到「盘上真录像」那一类行**，D1/D3 这类问题也只能真机验。

---

## 7. 建议派活与顺序（给调度台）

| 顺序 | 活 | 为什么先它 | 建议写手 |
|---|---|---|---|
| 1 | **A1 + F1**（一行修 + 一条产品形状断言） | 用户报的那条当场好；断言先于修复落地才好验 | 战斗线 + 自检线（可合一个执行代理，文件不冲突：`BattleDriver.cs` 一处 + `BattleScene.cs` 一处） |
| 2 | **B1 + B2 + B3**（自检删档 3 条） | 用户数据正在丢，且它让「先打一局再验回放」变得不可靠（打完被下一次自检删掉） | 自检断言线（一个文件 `ProfileDataAssertE13.cs` + `ReplayStore.cs` 守卫，和 1 不冲突） |
| 3 | **C1 + C2**（回放出口 + 键盘控制） | 没有出口，真机验回放只能靠杀进程 | 战斗线（同在 `BattleDriver.Update` 一带，**必须串行于 1**，因为同一文件） |
| 4 | **D1 + D2 + D3**（列表门槛 + 反馈 + 裁剪同步） | 界面线；和战斗线文件不冲突，可并行 | 界面线（`MatchLogRow.cs` / `BattleLogData.cs` / `BattleLogTab.cs`） |
| 5 | **A2 + A3**（录像记「牌本身」+ 战场 id） | 结构修，改 `ReplayRecord` + `RecBegin` + `PlayReplay`（同一文件，串行于 1/3） | 战斗线 + 引擎线 |
| 6 | **C3 + C4**（回放条 + 重播语义） | 依赖 C1/C2 落地后再动（同一段 `Update`/`ReplayBar`） | 战斗线 |
| 7 | **E3 / D4 / G2 / E4** | 低成本零碎，凑一批 | 按文件分派 |

> ⚠️ 上面 1、3、5、6 都动 `BattleDriver.cs` ⇒ **同一时刻只能有一个写手**（铁律 13·3）。
> ⚠️ 另外 **2（B1-B3）与 5（A2 新增录像字段）都动 `ReplayStore.cs`** ⇒ 这两批也要串行；
> 想并行就把 5 的字段声明交给另一人先落、守卫交给 2，**别两批同时写同一个文件**。
