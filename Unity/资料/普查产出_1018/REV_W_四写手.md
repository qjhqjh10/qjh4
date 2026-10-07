# REV-W · 四写手改动 · 独立审查

> 独立审查代理 `REV-W`，2026-10-18。⛔ 没跑 Unity / ⛔ 没动 git 写操作 / ⛔ 没改白名单外任何文件（本文件是唯一产出）。
> 审查对象 = 工作区**未提交**的 7 个 `.cs`（基线 = `HEAD fe239e9`）· 判据正本 = `DC/DA/DB/WC/WD` 五份只读诊断。
> ⛔ 未审 `RuleEngine/Editor/RuleEngineTest.cs` 与 `RuleEngine/Editor/DeckRulesTest.cs`（另有写手在改）—— 只核了「某条断言**有没有**」，没审内容。

---

## 1. 结论（找到 **8** 条；按严重度排）

| # | 严重度 | 一句话 |
|---|---|---|
| **F1** | **中** | **三处 `文件:行号` 指针被本批自己的改动挪失效**：`BattleScene.cs:13864` / `:14730` 引 `BattleDriver.cs:8649`（实际 **8656**）· `:14948` 引 `BattleDriver.cs:11110`（实际 **11117**）。WB 自己的报告也全用旧号。 |
| **F2** | **中** | **DC §6·1 点名的两句「出声」只补了一半**：`BattleDriver.cs:8323` 补了，**`BattleDriver.cs:8708` 那句仍然是裸 `Ctx.Log`** ⇒ 批处理里那句「玩家做完了脚本等着的那一步」依然一个字看不到。 |
| **F3** | **低** | `CardBaseDemo.cs:604` 那行的**理由写错了**：注释说 `"Ember"` 不在表里 —— 实测它**就在表里**（`CardText.cs:452`），`CardText.Faction` 对非空 key **永不回退** ⇒ 这一行对那条断言**毫无作用**（多余的改动 + 一条错的论据）。 |
| **F4** | **低** | `BattleScene.cs:14957` 那条断言的期望值 `selBefore` **与被断言值同源** ⇒ 单看它，**「`SelectRow` 整个机制死了」也照样绿**；目前靠上一条（14954）钉住绝对基线才成立（**脆弱面 = 只删掉 14954 就退化成恒真**）。写法是判据正本 DC §附**逐字给的**。 |
| **F5** | **低** | 新增的 5 处 `!driver.TutorialWaitingForTip` 里有 4 处是**「顺带为真」**（`14280` / `14408` / `14583` / `14738` / `14753`）—— 它们只能因「`DismissTutorialTip` 清位坏了」而为假，而那时**同段的另一条断言已经先红**；文案自称「两态判别式」对 `14280`/`14408` 略强。 |
| **F6** | **低** | **判据正本 DC §附的替换点表把 `14676` 也列进去了** —— 照做会在 `14679 Check(ov.TipVisible)` 当场新造一条红。**WB 这一处判对了**（见 §4）；另外 DC §2 行 53537 引的 `14682`/`14671` 与 HEAD 实际（`14687`/`14679`）差 ~8 行。 |
| **F7** | **低** | `DeckScene.cs:4861-4862` 仍挂着与本批订正**相反**的判据（「缺 `decks` 键 ⇒ `JsonUtility` 给 null」）—— **正是本批证伪的那句**。该文件本批未改（`git status` 无它）⇒ 铁律 5「grep 副本一起改」漏了一处。 |
| **F8** | **低** | `CardBaseDemo.cs:598` 括注「只给了 `faction = "Ember"/"Tide"`」**与源码不符**（`Placeholder` 给了 10 个字段）—— 同一段注释里唯一一处不严谨的措辞（行号本身 `93-117` 是对的）。 |

---

## 2. 逐条

| # | 严重度 | 结论 | 证据（文件:行号 + 片段） | 该照哪个判据 | 建议怎么改 | 置信度 |
|---|---|---|---|---|---|---|
| **F1** | 中 | 三处指针因**本批自己的 +7 行**失效 | `git diff` 只有 1 个 hunk：`@@ -8311,9 +8311,16 @@`（BattleDriver.cs）；`git show HEAD:…BattleDriver.cs \| grep -n "_tutTipUp) return"` = **8649**，工作区 grep = **8656**；`BattleLogPanel.Create` HEAD = **11110**、工作区 = **11117**。而 `BattleScene.cs:13864` / `:14730` 写「`BattleDriver.cs:8649`」、`:14948` 写「`BattleDriver.cs:11110`」。WB 报告 `WB_教程21红修.md:16,55,125,178,188` 全用这两个旧号 | 铁律 5（记错就地改）+ 5·b（判据要能复查）；DC §附末段把 `8649` 当「⛔ 不许动」的地标用，说明这个号本身就是判据 | 三处改成 `8656` / `11117`（或写成「HEAD 时 8649，本批 +7 后 8656」）；**五处同源，别只改一处** | 高 |
| **F2** | 中 | 死代码修活的「出声」只补了一句 | DC §6·1 原文点两句：「没有一条是「提示挂着」/「玩家做完了脚本等着的那一步」……**建议**：那两句 `Ctx.Log` 补一条 `Debug.Log`」。工作区：`BattleDriver.cs:8323` 有 `Debug.Log(waitMsg)`；`grep -n "玩家做完了脚本等着的那一步"` **只命中 8708**。`RuleEngine/Core/BattleContext.cs:1248-1252` = `Events.Add` + 环形裁剪，**无 `Debug.Log`** | DC §6·1；工程红线「不许静默失败」 | `8708` 那句照 8314-8323 的写法补一句 `Debug.Log`（同一个 `waitMsg` 变量那套） | 高 |
| **F3** | 低 | 注释里的论据是错的（且该行是**多余改动**） | `CardBaseDemo.cs:604` = `d.faction = "Ultramarines";   // 换成真阵营 key（Placeholder 给的是 "Ember"，不在表里 ⇒ 只能回退原样）`。实测：`StarterCards.EmberFaction = "Ember"`（`RuleEngine/Data/StarterCards.cs:28`）；`CardText.cs:452` = `{ StarterCards.EmberFaction, "余烬" }` ⇒ **Ember 在表里**、走中文名那一支；`CardText.Faction`（`:505-511`）对**非空** key 恒返回非空 ⇒ `_army` 层**本来就会建**（`CardView.cs:1760`），`armySeen` 从来不是红的成因（红的只有 `raceSeen`） | 红线「不许把猜测写成结论」；铁律 5 | 二选一：① 删掉这一行；② 保留但把理由改成「让探针用真阵营 key」并**删掉「不在表里」那半句** | 高 |
| **F4** | 低 | 期望值与被断言值同源 ⇒ 单条无鉴别力（**目前**被邻居兜住） | `BattleScene.cs:14955` `int selBefore = driver.LogSelectedRow;` → `:14957` `Check(ateB && driver.LogSelectedRow == selBefore, …)`。`selBefore` 由 14954 刚断过 = `-1`（`SelectRow` 是裸赋值 `BattleLogPanel.cs:436`），所以**等价于 `== -1`**；若 `SelectRow` 整条链从不被调，本条照样绿 —— 只有上一格的 `14932 Check(… != null && LogSelectedRow == 0)` 能抓它 | DC §附给的写法**逐字如此**（「`int selBefore = driver.LogSelectedRow;`」）；判别力判据 = 本工程「断言要能分两态」 | 改成 `driver.LogSelectedRow == -1`（此刻 `selBefore` 恒等于它），或保留但**显式写明「本条的判别力依赖 14954」** | 高 |
| **F5** | 低 | 4 处 `!TutorialWaitingForTip` 是「顺带为真」，文案自称两态偏强 | `14280`（⑧）/ `14408`（⑦）/ `14583`（⑬）/ `14738` / `14753`。这些位置的 `_tutTipUp` 刚被**同一句里的 `DriveStep`** 点掉（或本条动作根本不产提示，如 `EndTurn`/`Attack`）⇒ 该合取项**只能**在 `DismissTutorialTip` 的 `_tutTipUp = false`（`BattleDriver.cs:8133`）被删时才为假，而那时 `14733` / `== WaitingForActor` **已经先红**。另：它**挡不住**「`waitForTip` 不再置位」这一档（那时标志恒 false，本项目仍绿）—— 挡那一档的是 `14712` | 本工程「断言自证 / 弱断言」三条 | 可保留（不是假断言）；但把 `14280` / `14408` 文案里的「**两态判别式**」改成「**与 `ActionCounter` 合起来**才分得出真推了一条」，别让人以为它挡得住提示那一档 | 高 |
| **F6** | 低 | 判据正本自己的表有误（写手**没**照抄，是对的） | DC §附：「替换点……`14676` `14691` `14701` `14702` `14704`（⑭⑥⑦）」。HEAD `14676` = `Check(driver.DriveTutorialScriptForTest() == Advanced, …)`，紧邻的 `14679` = `Check(ov.TipVisible, …)` ⇒ 用会消提示的 `DriveStep()` 替换 `14676`，`14679` 当场红。另：DC §2 行 53537 引 `14682`/`14671`，HEAD 实际是 `14687`/`14679` | 判据正本内部一致性（下一轮会照那份表做） | 在 DC §附那一行**加一句**「⛔ `14676` 不能换 —— 紧随其后 4 条断言要读「提示正挂着」」；顺手订正 §2 那两行号 | 高 |
| **F7** | 低 | 同一条订正的副本没跟着改（铁律 5 漏一处） | `CardPresentation/Editor/DeckScene.cs:4861-4862`：「盘上放一段**确定性**的坏存档（合法 JSON、缺 `decks` 键 ⇒ `JsonUtility` 给 null ⇒ 必落 `Empty` 那一支）」—— 与 `DeckStore.cs:138-143`（本批新写的订正）**正相反**。`git status` 里**没有** DeckScene.cs ⇒ 本批没碰它 | 铁律 5「顺手 `grep` 那句话的关键词，把复制到别处的地方一起改」；DA §3 第 5 步 | 把那半句改成「缺键 ⇒ `JsonUtility` 给**空表非 null** ⇒ 旧判据恒假、新判据看**原文的 JSON 键**」。⚠️ 它的**断言**没坏（①-c 两个码都收，`DeckScene.cs:4849-4851`）—— 坏的是**理由**，而那正是下个会话会照抄的东西 | 高 |
| **F8** | 低 | 括注与源码不符 | `CardBaseDemo.cs:598`：「（`CardView.cs:93-117` 只给了 `faction = "Ember"/"Tide"`）」；`CardView.cs:93-117` 的 `Placeholder` 实际给 `id/title/cost/melee/ranged/health/keywords/isUnit/frame/faction` **10 个**字段 | 铁律 3（照解包/源码来）+ 红线「不许把猜测写成结论」 | 改成「**不填 `subtype`**（`Placeholder` 里没有这个字段）」 | 高 |

---

## 3. 每处新增/修改断言的【判别力】逐条核

> 问法 = 「**把哪一处实现改坏会让它红**」。答不出来的**单列在表末**。

| 断言（工作区行） | 性质 | **改坏哪一处会让它红** | 判别力 |
|---|---|---|---|
| `BattleScene.cs:14279-14285`（⑧①）`st0 == Advanced && tW.ActionCounter == 1 && !TutorialWaitingForTip` | 驱动换 `DriveStep()` + **新增第三项** | ① `RuleCore/TutorialScript` 不推进 ⇒ `ActionCounter` 停 0；② 删 `DismissTutorialTip` 的 `_tutTipUp = false`（`BattleDriver.cs:8133`）⇒ 第三项假 | ✅ 有（第三项**冗余**，见 F5） |
| `:14287-14291`（⑧②） | 仅驱动换成 `DriveStep()`，**断言一字未改** | 删 `IsNextActionScriptedPlayerAction` 那道分支 ⇒ 返回非 `WaitingForActor` | ✅ 原有 |
| `:14347-14351`（⑧⑤） | 三处驱动换成 `DriveStep()`，**断言未改**（`ActionCounter == 4 && == WaitingForActor`） | 同上 | ✅ 原有 |
| `:14406-14410`（⑦） | 驱动换 + **新增第三项** | 同 `14279` 那一行 | ✅ 有（同 F5 冗余） |
| `:14426-14429` | 驱动换，**断言未改** | 把记账口改成「凡动作都记」 | ✅ 原有 |
| `:14578-14586`（⑬） | 驱动换 + **新增 `!TutorialWaitingForTip`** | ① 提示没点掉 ⇒ 驱动只能返回 `Advanced`（`== WaitingForActor` 假）；② 清位坏 ⇒ 第三项假 | ✅ 有 |
| **`:14711-14715`（⑭⑥）** | **裸驱动 + 新增 `ActionCounter == 1 && driver.TutorialWaitingForTip`** | ① 被 `_tutTipUp` 挡住 ⇒ `ActionCounter` 停 0；② 删 `_tutTipUp = true;`（`BattleDriver.cs:8311`）或删整个 `if (tp.waitForTip)` 块（`:8309-8324`）⇒ 第二项假 | ✅✅ **真正的两态**，本批最好的一条 |
| **`:14733-14736`** | **全新断言** | 删 `DismissTutorialTip` 的清位（`:8133`）⇒ `!TutorialWaitingForTip` 假 **且** 返回值仍为 `true` ⇒ **只靠第二项抓得住**（文案里那句 🧨 说得对） | ✅ 强 |
| `:14737-14740`（⑦） | 驱动换 + 新增第三项 | 主判据 = `== WaitingForActor`（提示挂着时是 `Advanced`）；第三项只在「清位坏」时为假，而那时**本条第一项已先红** | ⚠️ 第三项**重复**上一条 |
| `:14749-14750` | 驱动换，无断言 | — | — |
| `:14752-14756` | 驱动换 + 新增 `ActionCounter == 4 && !TutorialWaitingForTip` | 前两步的提示没被点掉 ⇒ 这里返回 `Advanced` | ✅ 有（第三项同 F5） |
| `:14953-14954` | **全新**（`SelectRow(-1)` + 绝对基线断言） | 面板被 `Begin()` 重建（`SelectedRow` 回 -1）⇒ 14954 绿但**下面那条会假绿**（见下） | ⚠️ 见下一条 |
| `:14957`（改自 `== -1`） | **改写**（`== selBefore`）+ 出处行号订正 | ① 闸门写错成「放行」⇒ 调 `SelectRow(0)` ⇒ `selBefore` 变 ⇒ 红 ✅；② **但**「`SelectRow` 整条链从不被调」⇒ 本条**绿**（要 `14932` 才抓得住） | ⚠️ 单条**分不出**「闸门拦住写」与「根本没人写」（F4） |

**答不出来的：0 条。** 上表 12 处**都能**给出「改坏哪一处会红」；标 ⚠️ 的三处不是「答不出」，而是**判别力与邻居重复 / 单条不足以分两态**，已单列为 F4、F5。

**补一句（本批**没有**新增断言的改动）**：`RuleCore.cs` 那处**逻辑**改动（死代码修活）与 `DeckStore.cs` 那处**逻辑**改动，**在本批 7 个文件里都没有配套断言** —— 两者的配套断言分别被 WC / WD 明确推给了测试文件。已核：**都已另有写手在补**（`RuleEngineTest.cs:19364-19390` 的「判不出来 ⇒ 出声」· `:19804-19813` 的「`SaveAll(0 套)` → `None`」）。⚠️ 按简报**我未审那两个文件**，只核了「有没有」。

---

## 4. WB 那处【刻意偏离】的独立判定

**偏离是什么**：DC §附把 HEAD `14676`（⑭⑥ 段第一次驱动）列进「换成 `DriveStep()`」的替换点。WB **没换**，保持裸驱动、改成更强的两态判别式（`14711-14715`），并把「消提示」挪到 ⑦ 段开头**显式**点一次（`14733`）。

### ① WB 的理由成不成立 —— **成立**（我自己读了那几行）

HEAD/工作区逐字：

```
14676 / 14711   Check(driver.DriveTutorialScriptForTest() == Advanced, "（前提）第 1 关第 1 回合第 0 条是 SmallTip…")
14678 / 14716   var tipAct = driver.Ctx.Tutorial.CurrentTurn.actions[0];
14679 / 14717   Check(ov.TipVisible, "★ 走到 SmallTip ⇒ 提示亮起来（原版 BattleManager.ShowTutorialTip）");
14680 / 14718   Check(ov.TipText == (tipAct.arg ?? ""), …);
14684 / 14722   Check(ov.TipContinueText == "", …);
```

`DriveStep()`（`13874-13879`）= 驱动**再** `if (driver.TutorialWaitingForTip) driver.DismissTutorialTipForTest(true);` ⇒ 它会在同一句里把提示点掉 ⇒ `14717 Check(ov.TipVisible)` **当场红**。而 `ov.TipText`/`TipContinueText` 两条同理。
**佐证**：DC 自己的日志证据（§2 行 53537）正是「`14676` 那次驱动之后提示确实挂着」⇒ 那几条断言**只能**在提示没被点掉时成立。**DC §附那张表在这一点上自相矛盾**（§6·3 建议「加成两态」，但没提醒不能换）。
⇒ **WB 判断正确，⛔ 照 DC 的表做会新造至少 1 条（实际 3 条）红。**

### ② 它改完**会不会仍然新造红** —— **静态推演：不会**（逐条查了「新断言自己会不会红」）

| 新断言 | 要成立需要什么 | 静态核对 |
|---|---|---|
| `14711-14715` `== Advanced && Ctx.Tutorial.ActionCounter == 1 && TutorialWaitingForTip` | 那一次驱动**真的推了**第 0 条 `SmallTip` | `BeginTutorial(0)`（`:14691`）→ `Begin` 里 `_tutTipUp = false` / `_tutViewTurn = -1`（`BattleDriver.cs:2540-2542`）⇒ 本段第一动 = 第 0 条；数据 `turns[0].actions[0]` = `SmallTip(waitForTip=true)`（DC §3 表）⇒ 返回 `Advanced`、指针 0→1、标志置真。✅ |
| `14733` `DismissTutorialTipForTest(true) && !TutorialWaitingForTip` | 上一句之后 `_tutTipUp == true` | 同上（`ApplyTutorialActionViewForAction` 的 `8303 SetTip` 与 `8311 _tutTipUp = true` 是**同步同一段**；`DriveTutorialScript`→…→`ApplyTutorialActionView` 同步，DC 已用 `14679` 绿证过）✅ |
| `14737` `DriveStep() == WaitingForActor && !TutorialWaitingForTip` | 下一条是 `playerAction=true` | 数据 `actions[1] = AttackFreeMode(playerAction=true)`；`_tutTipUp` 已被 14733 清掉 ⇒ 不早退、走 `IsNextActionScriptedPlayerAction` 支 ⇒ `WaitingForActor`。✅ |
| `14741` `TutorialEventsContain("玩家动作出场")`（**原有**，会否被新顺序搞坏？） | `ShowPlayerActionAnim` 真的被调到 | 它有闩 `_tutViewTurn/_tutViewCounter`（`BattleDriver.cs:8662`），但 `BeginTutorial`→`Begin` 把它清成 `-1`（`:2540`）⇒ 本段 `(turn1, counter1)` 是**第一次** ⇒ 会调 ✅（与「消提示早于它」无关：14733 的 `SetTip(false)` 只收提示层，见 §6 第 5 条） |
| `14753` `ActionCounter == 4 && !TutorialWaitingForTip` | 两次 `SmallTip` 都被点掉后才能来到第 4 条 | `14749/14750` 两个 `DriveStep` 各点一次 ✅ |

**一处我**无法**静态证死、如实标**：DC §5·1（`14387`/`14395` 即 AI 那一记 `Attack` 随 R1 一起转不转绿）——**只有跑一次 `BattleScene.Run` 才能定**。这不是 WB 新造的，是**本批遗留的未定项**；若仍红，按 DC 的嘱咐**另开一条账，⛔ 别顺手改实现**。

### ③ 有没有**更小**的改法 —— **有，但只在 `14711` 那一条上**

- **必要的那一半（不可省）**：⑦ 段开头**必须显式点掉**提示，再用 `DriveStep()`。若直接照着「一律换成 `DriveStep()`」在 ⑦ 处调，驱动会**先**被 `_tutTipUp` 挡住（返回 `Advanced`）⇒ `== WaitingForActor` 红 ⇒ **显式点掉是必需的，不是可选优化**。
- **可省的那一半**：`14711` 那条断言**本来在旧代码下就是绿的**（DC 日志里它不在 18 条红里）⇒ 严格「最小改法」= **完全不动它**、只在 ⑦ 之前插一句 `DismissTutorialTipForTest(true)`。WB 动了它，换来的是 `14712` 那条**真两态**（DC §6·3 点名的弱断言被它修掉了）—— **我认为这个「多做的」是划算的**，不算改多了。
- ⛔ 没有比 WB 更小的**正确**改法能在 ⑦ 处既消提示又不断言自证。

---

## 5. 行尾 / 越界 / 禁改项 —— 三项体检

### 5·1 行尾（二进制读，`b.count(b'\r\n')` vs `b.count(b'\n')`）

| 文件 | CRLF / LF | 总行数 | numstat | 判定 |
|---|---|---|---|---|
| `CardPresentation/Battle/BattleDriver.cs` | 12977 / 12977 | 12977 | 10 / 3 | ✅ 未翻（纯 CRLF） |
| `CardPresentation/Editor/BattleScene.cs` | 17834 / 17834 | 17834 | 87 / 28 | ✅ 未翻（纯 CRLF） |
| `CardPresentation/Editor/CardBaseDemo.cs` | 960 / 960 | 960 | 8 / 0 | ✅ 未翻（纯 CRLF） |
| `RuleEngine/Core/RuleCore.cs` | **0 / 4902** | 4902 | 20 / 2 | ✅ 未翻（纯 LF） |
| `RuleEngine/Core/EffectResolver.cs` | 7332 / 7332 | 7332 | 14 / 2 | ✅ 未翻（纯 CRLF） |
| `RuleEngine/Core/CardInstance.cs` | **0 / 357** | 357 | 8 / 1 | ✅ 未翻（纯 LF） |
| `RuleEngine/Data/DeckStore.cs` | 260 / 260 | 260 | 34 / 2 | ✅ 未翻（纯 CRLF） |

**结论：7 个文件一个都没被翻行尾**（判据两条全过：两计数各自相等 ⇒ 单一风格；numstat 远小于行数）。
字母编码：7 个文件均 **无 BOM + UTF-8 合法**（逐个 `decode('utf-8')` 过）。

### 5·2 越界（白名单之外有没有被顺手改）

`git -C d:/4 status --short` 的实际内容 = **7 个 `.cs`**（本批 4 个写手）+ **2 个测试文件**（`RuleEngine/Editor/{RuleEngineTest,DeckRulesTest}.cs`，另有写手在改）+ **2 份文档**（`Unity/资料/待办判据_1018.md` · `项目任务.md`）+ **7 个未跟踪的写手报告**（`Unity/资料/普查产出_1018/{DA,DB,DC,RS,WB,WC,WD}.md`）。
⇒ **与简报给的白名单一致，没有第八个 `.cs`、没有别的目录被动过**。✅
⚠️ 这份快照是**动的**（别的代理在同时干活）：我这一趟跑的过程中又冒出一个未跟踪的 `普查产出_1018/WE_手牌夹具与护栏.md`、`Unity/资料/已知的坑.md` 也被人改了 —— 都是**文档 / 报告**，**7 个 `.cs` 的清单本身没变**（这正是本节要断的东西）。

逐个文件的 hunk 数（确认没有藏着的第二处改动）：`BattleDriver` 1 · `BattleScene` 10 · `CardBaseDemo` 1 · `RuleCore` 3 · `EffectResolver` 2 · `CardInstance` 1 · `DeckStore` 2 —— **逐个与我通读的 diff 对得上，无多余 hunk**。
另：`EffectResolver.cs` 与 `CardInstance.cs` 的改动**全是 `///` 注释行**（无一行代码）✅。

### 5·3 禁改项 —— **Z6 闸一字未动** ✅

```
$ git show HEAD:…BattleDriver.cs | awk NR==8649 | diff - <(awk NR==8656 工作区)
GATE IDENTICAL
            if (_tutTipUp) return TutorialStep.Advanced;
```
字节级相同，**只是行号从 8649 漂到 8656**（本批自己 +7 行）。那句的**语义、上下文、注释都没动**（整个文件只有 8311 一个 hunk）。
`BattleDriver.cs` 那处改动的性质 = **纯加日志**：加了 `var waitMsg`（`:8314-8316`）、`Ctx.Log(waitMsg)`（`:8317`）、4 行注释、`Debug.Log(waitMsg)`（`:8323`）——`_tutTipUp = true` / `_tutTipElapsed` / `_tutTipLimit` / `return` 三句**逐字未变**（对照 `git diff` 的 `-` 侧）✅ 与 WB 自陈「只加日志、不改逻辑」一致。

### 5·4 注释里的订正痕迹（铁律 5）—— **全部留了痕迹，一处遗漏在别的文件** ✅/⚠️

留痕正确的四处：`CardInstance.cs:161-168` · `EffectResolver.cs:2850-2859` 与 `:3095-3103` · `RuleCore.cs:3607-3610` 与 `:3649-3654` · `DeckStore.cs:72-75` 与 `:137-165`（都写了「原来写 X / 为什么不对 / 证据在哪」）。**没有一处是「直接删掉旧话」**。
⚠️ 唯一的遗漏 = **F7**（`DeckScene.cs:4861-4862` 那份副本没跟）。

---

## 6. 我核过但【没问题】的部分（一行一条）

1. **Z6 闸逐字未动**：`diff` 判 `GATE IDENTICAL`（HEAD `8649` ↔ 工作区 `8656`），整份 `BattleDriver.cs` 只有 1 个 hunk。
2. **`Debug.Log(waitMsg)` 撞不到任何日志型断言**：全工程的 `Application.logMessageReceived` 处理器分别按 `DOTWEEN`（`BattleScene.cs:308-310`）/ `[WaitBanner]`+`LogType.Warning`（`:3119`、`:3145`）/ `垂直档框高`（`:3450`）/ `MatchTypes.For`（`:7950-7951`）过滤；`Core/ClickLog.cs:89-96` 只做最多 24 条快照、且读它的两处断言都是 `Contains`。
3. **`Ctx.Log` 从不 `Debug.Log` 的判据成立**：`RuleEngine/Core/BattleContext.cs:1248-1252`（`Events.Add` + 环形裁剪）。WB 注释里引的路径前缀 `Core/` 指向 `RuleEngine/Core/`（不是 `CardPresentation/Core/`）——可查到，只是略含糊。
4. **`TutorialOverlay.SetTip(false, …)` 只收提示层**：`Battle/TutorialOverlay.cs:410` 只 `SetActive(false)`，**不碰**高亮 / 光标 / 剪影 ⇒ `14733` 那一下不会污染后面 `K2` 与高亮的断言。
5. **`ShowPlayerActionAnim` 那道的闩不会挡掉 ⑦ 的 K2/高亮**：闩是 `_tutViewTurn/_tutViewCounter`（`BattleDriver.cs:8662`），而 `Begin`（`:2540`）每局清成 `-1`，`BeginTutorial` 会调 `Begin`（`:2274` 内）⇒ 本段 `(turn1,counter1)` 与 `(turn1,counter4)` 都是首次。
6. **`RuleCore.cs` 循环改写行为等价**：`loose = true` 全文件**只有 3754 一处**、且紧跟 `return false` ⇒ `isLoose == true ⇔ fit == false` ⇒ `3637-3641` 的四句与旧写法在「贴牌 / 返回值」上完全等价，只有 `loose` 终于非 0。✅ WC 的核心论断成立。
7. **`RuleCore.cs` 多写一条 `ctx.Events` 不会造成联机假分叉**：`Net/NetProtocol.cs:337` 的联机指纹确实含 `ctx.Events.Count`，但这一条由**引擎内** `SetupCardInHand` 在两端**同点各写一条**（同函数里 `AttachHandEffectCopy` 早就 `ctx.Log`，`EffectResolver.cs:3063`）；本地录像用的 `StateHash`（`:354-367`）**不含**该项 ⇒ 录像侧零影响。
8. **`DeckStore` 新判据没被写成「提前 return」**：它在 `FromJson` **之前**取值、但作为第三项**或**进判据 ⇒ 语法坏档仍走 `catch` → `ReadFailed`（`DeckScene.cs:4847-4851` 的 ①-c 两个码都收、`DeckLibrary.ClassifyLoad` 也认它）。若写成早退，①-c 会变成 `Empty` —— **这一点 WD 做对了**。
9. **`"decks":` 带冒号不会被字符串值误命中**：卡组名 `decks` 序列化成 `"decks"` 后跟 `,`/`}`（永不是 `:`）；名字里含引号时 JSON 会转义成 `decks\":`，**不同串** ⇒ 逐条核过，判据成立。
10. **`SelectRow(-1)` 安全**：`BattleLogPanel.cs:436` `public void SelectRow(int i) { SelectedRow = i; }` —— 裸赋值，无下标 / 无 clamp。
11. **R2 的根因判断成立**：`SelectedRow` 是 `private set`（`BattleLogPanel.cs:431`）⇒ 全仓**只有**构造（`:606`）与 `SelectRow`（`:436`）两处写点，`Show()/Hide()/ApplyEntries()` 不碰（全文件 grep 仅 3 处命中）⇒ `14953` 的 `SelectRow(-1)` 确实是一次有效复位。
12. **`CardBaseDemo` 修完 ①② 都该绿**：`TmpFont.NewText` 里 `t.alignment = TextAlignmentOptions.Center`（`Core/TmpFont.cs:158`）⇒ `army`/`race` 的 `verticalAlignment == 512`；`_title` 被显式设成 `VerticalAlignmentOptions.Geometry`（`CardView.cs:1705`，注释 `:1698-1701` 已说明 TMP 里这版**没有 `Midline` 这个名字**、`TextAlignmentOptions.Midline = Center | Geometry`）⇒ 数值 4096 与断言一致。
13. **`Fill` 的返回语义核过**：`CardView.cs:1871-1879` 对 `IsNullOrEmpty(text)` **直接 `return null`、不建节点**；`SubtypeLine`（`:1797-1811`）在 `case "unit"` 上 `RaceTerm(d.subtype)`，而 `RaceTerm(null)` 返回 `null`（`:1826-1835`）⇒ 「夹具没给 `subtype` ⇒ `race` 层不建 ⇒ ② 前提必红」**这条诊断成立**，修法（补 `subtype`）也对。
14. **`DeckRulesTest` 的三处旧夹具不会被新判据打红**（我读的是 **HEAD** 版，不是工作区）：`:227` 无文件（`NoSaveFile`）· `:250` 语法坏（`ReadFailed`）· `:470` 手写 JSON **带** `"decks":`（命中，`old.Count == 1` 照旧）✅。
15. **本批没有「改一条跑一遍」的迹象**：改动全落在 `BattleScene`/`RuleCore`/`DeckStore`/`BattleDriver` 四个宿主，配套自检（`BattleScene.Run` / `RuleEngineTest.Run` / `DeckScene.Run` / `CollectionScene.Run` / `MainMenuScene.Run`）**我没跑**（简报禁止）—— **这一批的自检收口必须由调度台在同步点跑**，本报告不代替它。

---

### 附：本报告用过的只读命令（可复核）

```bash
git -C d:/4 status --short
git -C d:/4 diff --numstat -- <7 files>
git -C d:/4 diff -- <7 files>
git -C d:/4 show HEAD:Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs | grep -n "_tutTipUp) return"
git -C d:/4 show HEAD:Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs | awk 'NR>=14645 && NR<=14711'
git -C d:/4 show HEAD:Unity/MyGame/Assets/Presentation/Battle/BattleDriver.cs | awk 'NR==8649' | diff - <(awk 'NR==8656' 工作区/BattleDriver.cs)
python -I -c "import io;b=io.open(r'<路径>','rb').read();print(b.count(b'\r\n'),b.count(b'\n'))"   # 7 个文件逐个，二进制读
grep -n "loose = true\|out bool loose\|loose++" Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs
grep -rn "Application.logMessageReceived" Unity/MyGame/Assets --include=*.cs
grep -rn "verticalAlignment" Unity/MyGame/Assets/CardPresentation/Core/CardView.cs
grep -rn "EmberFaction" Unity/MyGame/Assets/RuleEngine/Data/StarterCards.cs
grep -rn "dto.decks == null\|JsonUtility\` 给 null" Unity/MyGame/Assets --include=*.cs
```
