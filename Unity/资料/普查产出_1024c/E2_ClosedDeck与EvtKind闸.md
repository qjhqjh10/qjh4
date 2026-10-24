# E2 · `ClosedDeck` 参数档 ＋ `EvtKind` 登记闸（第七十三会话 · 2026-10-24）

> 执行代理 **E2**。工程根 `d:/4/Unity`。白名单内只动了三个文件（见 §二）。
> ⛔ 没跑 Unity 批处理、没跑 ruleprobe、没动 git、没动 `CLAUDE.md` / `项目任务.md`。

---

## 〇 · 一句话结论

1. **`GameplayVariables.For(ClosedDeck)` 补上了** —— 轮抽那一档不再靠 `default` 静默落进经典（30 张那套），
   改为**显式一档**，参数**复用 `Skirmish`（12 张那套）**。🔴 **原版真值在服务端、本地 0 命中**
   ⇒ 这一档的数值**是我们挑的，不是原版真值**（逐字段来源见 §三）。
2. **`EvtKind` 三处 `default` 的闸：本轮开工时【已经被前序代理做完了】**（`A2` 起头、`F2` / `G1b` 补，
   已提交在 HEAD 里）。本代理**复核**了它，并补上它**没覆盖的那一格**：
   「登记在 `TimedKinds` 里、但 `switch` 里没有 `case`」时，`IsSourced` 仍会静默回 `true`
   ⇒ 新增 `EventTiming.IsTimedKind` + `ReportMissingCase` + 一条**全枚举逐档**的断言。
3. 类型检查 **运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_e2 bash 工具/typecheck.sh`）。

---

## 一 · 判据出处

| 事 | 判据 |
|---|---|
| `GameMode.ClosedDeck = 7` | 原版 `enum PlayModes`（TypeDefIndex 961），已逐字登记在 `GameplayVariables.cs` 的枚举注释里 |
| 轮抽参数在服务端、本地拿不到 | `DraftEventData.gameplayVariables` 由服务端下发；`资料/普查产出_1024b/S2_轮抽模式数据.md`（`+0x98/+0x9C/+0xA0` 全 0 命中）· `S3_轮抽窗原版规格.md` §五 |
| 「12 张」这一档的数值来源 | 原版**游戏内文案**（`资料/索引与盘点/解包资源使用地图.md:533`）：`12-card decks` / `Up to 4 Legendaries` / `Warlords start with 10 less Health` / `Gain +2 Maximum Energy each turn` / `No mulligan`；`handLimit 8` / `startingHand 4` / `manaAccumulation 1` 只有**粉丝规则书**（非官方）一条 |
| `EvtKind` 三处 `default` 是缺陷 | `资料/普查产出_1023e/J2_引擎结构自检.md` §四·3（现 19 档；`ShowCardsWindow` 恰好需要「非阻塞」） |
| `ShowCardsWindow` 必须保持零时长 | 原版 `FinishResolvingAction` 紧跟其后、无 `yield`（`BattleDriver.FireEventAll` 里 `OpenShowCardsWindow` 那段判据）→ `EventTiming.cs` 的 `ZeroDurationKinds` 注释 |

---

## 二 · 改动清单（改前 / 改后）

### A · `RuleEngine/Core/GameplayVariables.cs`（`git diff --numstat` = **65 / 4**，CRLF 未翻：0 / LF 518）

| 位置（改后行号） | 改前 | 改后 |
|---|---|---|
| `:429-476` **新增 `public static GameplayVariables ClosedDeck`** | 不存在 | 惰性单例，**`Skirmish.Clone()`**；doc 里逐字段写清「值 / 来源」，并写明**「我们挑的，不是原版真值」** |
| `:476` `static GameplayVariables _closedDeck` | 不存在 | 新增（与 `_skirmish` / `_tutorial` 同形） |
| `:497-508` `For(GameMode)` | 只有 `Skirmish` / `Tutorial`+`TutorialReplay` 两条改道，**其余全 `return Classic`** ⇒ `ClosedDeck` **静默落经典**（30 张 / 起手 3 / 传说上限 1 / 有换牌） | 新增 `if (mode == GameMode.ClosedDeck) return ClosedDeck;`（**在 `default` 之前显式接住**）；`For` 的 doc 同步改成「四份实例 / 15 档落点」 |
| `:1` 文件头 | 「经典 / 遭遇 两套实例」（早已过期：`Tutorial` 早已存在） | 「四套实例：经典 / 遭遇 / 教程 / 轮抽」 |

**改后行为**：
- `For(ClosedDeck)` ⇒ `deckSize = 12` 那一套（`IsSkirmish == true`），**不再**是 30 张那套。
- **今天所有生产调用路径的行为 = 一个字没变**（如实记，别当成大改）：
  全仓 `For(...)` 只有两处调用 —— `BattleDriver.cs:3688`（参数是按**这副牌**判 `IsSkirmish` 取 `Skirmish|Classic`，
  **不传 `ClosedDeck`**）与 `DeckEditorState.cs:607`（传的是卡组自己的 `GameMode`，而**现读全仓没有任何
  `PlayerDeck.GameMode = ClosedDeck` 的赋值** —— 轮抽开战那条路在 `DraftModeWindow.cs:1186` 把 `gameMode`
  写成了 `Skirmish` 顶住，见 §七·1）。
  ⇒ 本改动是**把「静默错口径」的坑填上**，不是改一局正在跑的行为。
- 唯一**将来会**可观测的那一格：`DeckEditorState.AssignDefensiveCardsInEditMode` 对一份 `GameMode = ClosedDeck`
  的卡组，会从「防御卡不列」（经典档 `false`）变成「列」（遭遇档 `true`）—— 与「12 张那套」自洽。
  ⚠️ 用户 2026-09-27 拍板的偏离（**经典模式即使字段 false 也列**）**没被碰**。

### B · `CardPresentation/Core/EventTiming.cs`（`git diff --numstat` = **54 / 7**，LF 未翻：0 / LF 912）

**先记事实**：`IsSourced` / `SourceOf` / `DurationOf(EvtKind)` 三处「查登记表 + 出声 + 计数」**是 HEAD 里已有的**
（`A2` 的注释 + `F2` 的「清计数 ≠ 清闩」+ `G1b` 的 `StatusApplied` 登记），断言在
`CardPresentation/Editor/BattleScene.cs:1611-1690`（含「往枚举加一档不登记 ⇒ 红」）。
⇒ **本代理没有重写它**，只补了它答不出来的那一格：

| 位置（改后行号） | 改前 | 改后 |
|---|---|---|
| `:270-276` **新增 `IsTimedKind(EvtKind)`** | 只有 `IsRegisteredEventKind`（两张表任一命中 ⇒ true） | 新增「这一档是不是登记成**有时间**」；`IsRegisteredEventKind` 改为复用它 |
| `:332-350` **新增 `ReportMissingCase(kind, where)`** | 只有 `ReportUnregisteredKind`（「没在登记表里」） | 新增**另一种病**的诊断：**「登记了、但 `switch` 里没有 `case`」**；同样 `UnregisteredKindHits++` + 每档只喊一次（共用同一个闩，⛔ 不加第二个计数器） |
| `:442-446` `DurationOf(EvtKind)` 的 `default:` | `零时长 ⇒ 0f`；否则一律按「未登记」出声 | 分三层：**零时长 ⇒ 0f** / **登记成有时间却无 `case` ⇒ `ReportMissingCase`** / **未登记 ⇒ `ReportUnregisteredKind`** |
| `:879-890` `SourceOf(EvtKind)` 的 `default:` | `零时长 ⇒ 「（无）」`；否则一律按「未登记」出声 | 同上分三层；「登记了没 `case`」返回**另一句**说明（⛔ 不再误说成「没在登记表里」） |
| `:804-812` `IsSourced` 的 `default:` | 只查 `IsRegisteredEventKind` | 行为不变（仍回 `true`），但**写明这条答不了「有没有 `case`」**，并指向两处兜底（`ReportMissingCase` + `EngineClosedDeckAssert §B`） |
| `:209-226` 登记表的说明 | 「有值 → `DurationOf` 的 `switch` 里加 `case`」 | 补成「`DurationOf` **和 `SourceOf`** 各加一个 `case`；只登记不加 `case` 也算病、有断言会红」 |

**为什么这条补充不是多余的**：`TimedKinds` 只是**声明**。一个人把新事件登记进 `TimedKinds`、
却忘了在 `DurationOf` 里加 `case` 时：
- `DurationOf` / `SourceOf` 会落回 `default:`（出声 —— `A2` 那一版已经救回这一半）；
- 但 **`IsSourced` 会因为「登记表里有名字」回 `true`** ⇒ `BattleScene` 的 `unsourced == 2` **不会红**
  ⇒ 那一格**仍然是静默的**（正是 J2 §四·3 的形状）。
本代理加的 `§B` 断言逐档调 `DurationOf` 并盯**未登记计数**，把这一格变成红。

### C · 新建 `RuleEngine/Editor/EngineClosedDeckAssert.cs`（+ `.meta`，guid `f86818fd436c42598e3406f1735fa4fc`）

独立入口 `public static void Run()`（全局命名空间，可 `-executeMethod EngineClosedDeckAssert.Run` 单跑，
也可在宿主里 `EngineClosedDeckAssert.Run(); pass += EngineClosedDeckAssert.Pass; fail += EngineClosedDeckAssert.Fail;`）。
内容 = §A `ClosedDeck` + §B `EvtKind` 登记闸（见 §四）。

---

## 三 · `ClosedDeck` 参数表（逐字段：值 / 来源）

**来源口径**（本文件那套照旧）：【文案】= 原版**游戏内文案**（官方）·【规则书】= **粉丝实体版，非官方** ·
【推断】= 我们从别的判据反推 ·【缺省】= 本地没有真值，沿用字段默认（与经典同值）。

| 字段 | ClosedDeck | 来源 |
|---|---|---|
| `deckSize` | **12** | 【文案】`12-card decks`（轮抽侧另有「4 包 × 3 张」的结构一致：`CardsPerPack=3` 原版真值 × `RoundsPerDraft=4` 我们挑的） |
| `maxLegendaries` | **4** | 【文案】`Up to 4 Legendaries (Warlord not included)` |
| `warlordLifeChange` | **−10** | 【文案】`Warlords start with 10 less Health` |
| `manaPerTurn` | **2** | 【文案】`Gain +2 Maximum Energy each turn` |
| `startingManaSecond` | **2** | 【推断】文案 `P1 3 Energy P2 4 Energy` 的那 1 点差 |
| `showMulligan` | **false** | 【文案】`No mulligan` |
| `shouldReshuffle` | **false** | 【文案】`No mulligan`（同族推断） |
| `assignDefensiveCardsInEditMode` | **true** | 【文案】`Random Defence card`（⚠️ 该文案**不是**这个字段的证据，见字段注释；我们只读不实现） |
| `handLimit` | **8** | 【规则书，**非官方**】:58 |
| `startingHand` | **4** | 【规则书，**非官方**】:58 |
| `manaAccumulation` | **1** | 【规则书，**非官方**】:60 |
| `overtimeTurn` | **10** | 【推断】（本地无服务端真值；同经典） |
| `startingMana` | 1 | 【缺省/推断】（同经典；配合 `startingManaSecond=2` 得到文案 `P1 3 / P2 4`） |
| `numberOfCopiesOtherRarities` | 2 | 【缺省】（同经典；原版真值在服务器） |
| `numberOfCopiesLegendary` | 1 | 【缺省】（同上） |
| `drawCardsPerTurn` | 1 | 【缺省】（同上；加时多抽那 1 张是另一支 `DeckRules.OvertimeExtraDraw`） |
| `slotsPerSide` | 9 | 【缺省】（⚠️ 与原版 `slotsPerSide` **同名不同义**，见字段注释 `A2099`） |
| `addedManaSecond` | 0 | 【缺省】（机制已实现、值在服务器） |
| `manaBonusSeat0/1` | 0 | 对局状态字段，非模式参数 |
| `alternativeTraitLogic` | 0 | ⛔ 未解（原版别的机制，两边都填 0） |
| `secondExtraCards` | 1 | 【原版】`ScenarioVariables.secondExtraCards`（**不按模式变**，两模式同值） |
| `IsSkirmish`（派生） | **true** | 判据 = `deckSize < 30` ⇒ 语义是「12 张那套」，**不是模式号** |

🔴 **总表口径**：**`deckSize / maxLegendaries / warlordLifeChange / manaPerTurn / showMulligan` 那几条有官方文案撑；
`handLimit / startingHand / manaAccumulation` 只有粉丝规则书；其余是经典缺省。**
**没有一条是「原版轮抽参数表真值」** —— 那句真值在 `DraftEventData.gameplayVariables`，本地 0 命中。

---

## 四 · 断言清单（`EngineClosedDeckAssert`）

### §A `GameplayVariables.For(ClosedDeck)`

| 编号 | 断的是什么 | 灭自证（怎么改会红） |
|---|---|---|
| A1 | **`deckSize != Classic.deckSize`**（12 ≠ 30）且 **`!ReferenceEquals(For(ClosedDeck), Classic)`** | 删掉 `For` 里 `case GameMode.ClosedDeck:`（落回 `return Classic`）⇒ **两条同时红** |
| A2 | 逐字段字面量：12 / 4 / −10 / 2 / `startingManaSecond` 2 / 无换牌 / 不洗回 / 手牌上限 8 / 起手 4 / 结转 1 / `IsSkirmish` / `overtimeTurn` 10 | 把任一格改成经典值 ⇒ 红（期望值**不从 `Skirmish` 取**，⛔ 不是自证） |
| A3 | `For(ClosedDeck)` 幂等（两次同一实例） | 改成每次 `new` 一份 ⇒ 红 |
| A4 | **15 档逐档落点表**：遭遇 1 · 轮抽 1 · 教程 2 · 经典 11；每档必须是**对应那份单例**（`ReferenceEquals`） | 删掉 `ClosedDeck` 那一行 ⇒ 它掉进经典桶（12）⇒ 红；`GameMode` 枚举不是 15 档 ⇒ 红 |
| A5 | 回归：`Skirmish` 仍 12 张 / `Classic` 仍 30 张 / `Tutorial` 仍起手 3 | 改这三档 ⇒ 红 |

### §B `EvtKind` 登记闸（复核 + 补「登记了没 `case`」）

| 编号 | 断的是什么 | 灭自证 |
|---|---|---|
| B2 | 每一档**零时长**：`DurationOf == 0` · `SourceOf == 「（无）」` · `IsSourced == true` | 把零时长档塞进 `TimedKinds` ⇒ B5 红 |
| B3 | 每一档**有时间**：`DurationOf` **不涨未登记计数**（= `switch` 里真有 `case`）· `SourceOf` 不含「未登记 / 没有它的 `case`」 | 保留 `TimedKinds` 但删掉 `DurationOf`（或 `SourceOf`）的 `case` ⇒ **本条红**（这正是 `IsSourced` 答不出来、`A2` 那一版兜不住的那一格） |
| B4 | `Death` / `Ability` 显式 `IsSourced == false` **且带原因**；其余登记档 `true` 且无原因 | 把 `IsSourced` 改回恒 `true` ⇒ 红 |
| B5 | **全枚举**：未登记 0 档 · 重复登记 0 档（现读枚举，⛔ 不写死 19） | 往枚举加一档不登记 / 同时塞进两张表 ⇒ 红 |
| B6 | **灭自证**：`(EvtKind)999` ⇒ `IsSourced=false`+带原因 · `DurationOf=0` **但计数涨** · `SourceOf` 含「未登记」 | 任一处改回 `return true;` / `return 0f;` / `return "（无）";` ⇒ 红 |
| B7 | `ShowCardsWindow` **显式零时长**（`IsSourced=true` · `DurationOf=0` · `SourceOf=「（无）」`） | 把它从 `ZeroDurationKinds` 删掉 ⇒ 它变成「未登记」噪音源 ⇒ 红 |

> ⚠️ 与 `BattleScene.cs:1611-1690` 那组**不重复**：那边断「登记表齐全 + 未登记三处出声 + 清计数没清闩」，
> 这边断「**登记表与 `switch` 的 `case` 一致**」+ `ClosedDeck`。两边都跑才是完整的。

---

## 五 · 验了什么 / 没验什么

- ✅ `TMPDIR=/tmp/wf_e2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**
  （已确认新文件在源清单里：`/tmp/wf_e2/wf_csc_editor.rsp` 命中 `EngineClosedDeckAssert`）。
  ⚠️ 中途有一次跑出 **编辑器 13 条错**，**全部集中在 `CardPresentation/Editor/RankedArtAssert.cs`**
  （`RankedDivisionInfo.RankArtName` / `RankedTab.DivisionArtUsed` 等**别的代理正在写**的成员）
  —— 按铁律 13·3 **没去碰**，隔 60 s 重跑即 **0 / 0**。
- ✅ 行尾没翻：`GameplayVariables.cs` 0 CRLF / 518 LF；`EventTiming.cs` 0 / 912（两者改前就是纯 LF）。
- ⛔ **没跑 Unity 批处理**（红线，只有主对话能跑）⇒ `EngineClosedDeckAssert.Run()` 本身**还没实跑过**。
  建议主对话在同步点用 `-executeMethod EngineClosedDeckAssert.Run` 跑一次（或并进 `RuleEngineTest.Run` 的收尾）。
- ⛔ 没跑 ruleprobe（红线）。
- ℹ️ 顺手跑了可选的 `WF_DOC=1` 档：**错误仍是 0/0**；格式类 warning 是**全工程存量**
  （`RuleCore.cs` 135 · `BattleDriver.cs` 90 …），`GameplayVariables.cs` 那 6 条改前就在（本代理只新增了
  `list type="bullet"` 那一段，口径与文件里其它 `list` 一致）。

---

## 六 · 没查清 / 留的口子（如实记）

1. **轮抽参数表的原版真值仍然没有** —— `DraftEventData.gameplayVariables` 在服务端。
   本轮只是**把静默落经典换成显式复用 Skirmish**；真值到手时**只改 `GameplayVariables.ClosedDeck` 这一处**（§三那张表就是改动清单）。
2. **`ReportMissingCase` 与 `ReportUnregisteredKind` 共用 `UnregisteredKindHits` 与同一个「只喊一次」的闩** ——
   这是有意的（对「报了没有」的断言来说是同一件事），但读数的**名字**（`UnregisteredKindHits`）现在覆盖两种病。
   ⛔ 没改名（`BattleScene` 在用，且两份计数器迟早不一致）。
3. **`IsSourced` 依然检验不了「`switch` 里有没有 `case`」**（那要靠执行）。靠两处兜：
   `DurationOf`/`SourceOf` 的 `default` 出声 + `§B` 的 `B3` 断言。
4. **`DelayBetween` 里也有一个 `default: return 0f;`** —— 新事件默认「与上一条无间隔」。
   它不在本件点名的三处里，语义上「0 间隔」也是多数事件的正常值 ⇒ **没动**。见 §七·4。
5. **`ClosedDeck` 是惰性快照**：第一次访问那一刻 `Skirmish` 是什么值就定死什么值。
   自检里有临时改 `Skirmish` 字段的地方（`DeckScene.cs:1806` 那处是 try/finally 同步还原）——
   已写进属性注释，⛔ 别在那样的窗口里**首次**访问它。

---

## 七 · 顺手发现（⛔ 只报不改，都在白名单外）

1. 🔴 **`DraftModeWindow.LaunchBattle` 那段「顶住」注释已经过期，而且可以换掉了**
   （`CardPresentation/Shell/DraftModeWindow.cs:1186-1191`）：
   现在写的是 `gameMode = (int)GameMode.Skirmish`，理由写着「`GameplayVariables.For` 里**没有 ClosedDeck 那一档**
   ⇒ 落到 Classic 30 张」。**本件之后那个理由不成立了**。⇒ 可改成 `gameMode = (int)GameMode.ClosedDeck`，
   让「卡组自己带的模式」与「入口模式号」一致（原版 `CardDeck.gameMode` 本来就该是 ClosedDeck）。
   ⚠️ **不是纯注释改动**：`PlayerDeck.GameMode` 会影响 `DeckEditorState.AssignDefensiveCardsInEditMode`、
   对局记录/录像头里的模式口径 ⇒ 要单独验一轮（建议并进下一批，别顺手改）。
   本条建议落账。
2. ⚠️ **`CardPresentation/Editor/BattleScene.cs:30982` 的注释也过期了**：
   「`GameplayVariables.For` 只对 `Tutorial 4` / `TutorialReplay 9` 改道，其余 13 档照旧」
   ⇒ 现在是「`Skirmish` / `ClosedDeck` / 教程两档」改道、其余 11 档照旧。
   那条**断言本身仍绿**（它只查经典局：`!IsSkirmish && startingHand==3`），只是文案。
3. ⚠️ **`D2` / `D1` 的报告里那张「`ClosedDeck` 走 Skirmish 顶住」的表**（`资料/普查产出_1024b/D2_轮抽UI.md:73`）
   现在应当改成「**显式**复用 Skirmish（`GameplayVariables.ClosedDeck`）」。文档不在本代理白名单。
4. ℹ️ **`DelayBetween` 的 `default: return 0f;`**（`EventTiming.cs:205`）与那三处同族，但**语义不同**：
   它答的是「两条事件之间隔多久」，0 = 紧接 —— 对绝大多数事件是正确的。若将来要拦新事件，
   建议走**同一条**登记表（`ZeroDurationKinds` 现在表达的是「不占时间」，正好也适合「不额外等」），
   ⛔ 别再开第二张表。
5. ℹ️ **`GameplayVariables.cs` 顶部那段「值的来源分三档」**只列了【文案】/【规则书】/【推断】，
   本件新增的 `ClosedDeck` 用的是同一套口径 + 「缺省」 ⇒ 已在属性 doc 里逐条标，没动文件头那段。
