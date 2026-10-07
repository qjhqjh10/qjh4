# REV-SHELL · 外壳族五写手 · 独立审查

> 独立审查代理 **REV-SHELL** · 2026-10-18 · 工程根 `d:/4/Unity/MyGame/Assets/CardPresentation`
> **只读**：没跑 Unity、没动 git、**一个字都没改**（本文件是唯一写出的文件）。
> 审查对象 = 工作区未提交的 S1–S5 改动（`git -C d:/4 diff`）+ 判据正本
> `资料/普查产出_1018/{RS_外壳族8条现核,S1_A840与A867,S2_A851槽号,S3_A833夹沿断言,S4_A840A867断言,S5_A866窗头收口}.md`。
> 🔴 行号 = **我读的那一刻**工作区坐标；别的写手在动别的文件 ⇒ 请按**节点名 / 语句**认。

---

## 1. 结论（找到 7 条；按严重度排）

| # | 严重度 | 一句话 |
|---|---|---|
| **F1** | 🔴 **高 —— 必红** | `Editor/MainMenuScene.cs:5840` / `:9269` 的 `CheckNear(ex1, ry1, 0.5f)` **期望值取错**：拿**参照行**那颗字的顶点上沿当压边行的上沿。两行是**不同节点、差整整一个行距**（排行榜 **115px** / 对局历史 **228.2px**），而 `TmpSpanPx` 给的是**绝对画布 px** ⇒ 这两条注定红（会把 4 条 `MainMenuScene.Run` 断言判死）。 |
| **F2** | 🟡 中 | `Shell/ViewportClip.cs:205` 与 `:219` 两处**旧口径没撤**，与**同一个文件的文件头**（`:69-77` 已写「那一列现在是空的」）自相矛盾。该文件是写手绝对禁区 ⇒ 归调度台。 |
| **F3** | 🟡 中 | A867 那 4 条「登记表不涨」**结构上可以「两边一起改回去还是全绿」**：`nScroll > 0` 只证明**表非空**、不证明**本窗登记过**（删 `RegisterScroll` + 删 `UnregisterOwnedBy` ⇒ 仍绿）。缺一条「本窗确实登记了」的伴随断言。 |
| **F4** | 🟢 低 | S3 的 A833 回归网仍缺三档（`LeaderboardRow.Name` · **上沿**那一档 · `BattleLogTab` 那一扇宿主），且**所有数值断言从没跑过**（作者已自陈）。 |
| **F5** | 🟢 低（观察） | 批外 `.cs` **11 个**（见 §6）；其中 `Editor/CardFaceProbe.cs` 在我审查期间（mtime **23:55:52**）**又被人改了一次** —— 它正是 S2 报告 §6·1 点名的那处同族 `meshInfo[0]`。 |
| **F6** | 🟢 低（归属） | `Editor/ShellScene.cs:229-240` 那段注释订正，**S4 记成「S2 改的」、S2 明写「我没改它」** ⇒ 两份报告打架（内容本身对）。若真是 S2 改的 = **越界**（S2 白名单只有 `RewardsScene.cs`）。 |
| **F7** | 🟢 低（疑似） | (甲) 里还原用的是**字面量** `vpFull435`，而本文件 ③ 那条用 `ShopTabPage.ScrollView` —— 两处还原源不同（亚像素级）。置信度 0.5，**建议统一**，不是缺陷。 |

**没有一条**是「S5 四窗被静默统一」或「`ViewportClip.cs` 顺手改了实现」—— 那两项我逐份核过，干净（§5 / §6）。

---

## 2. 逐条

| # | 严重度 | 结论 | 证据（文件:行号 + 片段） | 该照哪个判据 | 建议怎么改 | 置信度 |
|---|---|---|---|---|---|---|
| **F1** | 🔴 高 | **两条断言的期望值取错 ⇒ 必红（假红）** | `Editor/MainMenuScene.cs:5840` `CheckNear(ex1, ry1, 0.5f, "…而且**只截了下沿那一侧**：上沿仍停在排版位（参照行 {ry1:F2} vs 压边行 {ex1:F2}）")`；同形一条在 `:9269` `CheckNear(ex1, ry1, 0.5f, …)`。**量法**：`ShellScene.TmpSpanPx` → `Editor/ShellScene.cs:262` `LayoutSpace.ToPixel(tmp.transform.TransformPoint(vm[v + k]))` = **绝对画布 px**（不是局部量）。**两行不同节点**：`Editor/MainMenuScene.cs:5732-5736` 用 `LayoutSpace.PxY(rt.position.y)` 选「最低的两颗」⇒ `a833Edge` 比 `a833Ref` **低** `a833D = a833EdgeCy − a833RefCy`（排行榜 = 行高 100 + 行距 15 = **115**；对局历史 = 203.2 + 25 = **228.2**）。而作者**同一条块的上一行**就是这么算的：`:5827` `CheckTrue(ry2 + a833D > VpBot + 5f, "**不裁**的话…顶点底边…")` ⇒ 压边行**不裁时的上沿**只能也是 `ry1 + a833D`。 | S3 报告 §2.1 的**量纲说明**自己写着「`ry1`（参照行实测）**+ 行距差** = 压边行**不裁时**的顶点位置」；工程既有先例 `Editor/ShellScene.cs:1987` `CheckTrue(Mathf.Abs(rMinX − nMinX) <= 0.6f, "…而**左沿没动**")` 比的是**同一个 label 的前后两态**，不是两颗不同节点 | `CheckNear(ex1, ry1 + a833D, 0.5f)`（`:5840`）· `CheckNear(ex1, ry1 + mD, 0.5f)`（`:9269`）——`mD` 就是 `:9254` 那个变量 | **0.9** |
| **F2** | 🟡 中 | **同一个文件里两份说法打架（铁律 5/6）** | `Shell/ViewportClip.cs:205` `/// ⚠️ **`false` 的那一批是【还没接上】的站点**（清单 → 文件头 §① 那条订正）` · `:219` `<para>⚠️ **今天的调用点 = 0**（那几处都在别的文件…⇒ 只报不改）` vs 同文件 `:69-77`「**那一列现在是空的**…当天按 `grep` 现扫出的生产站点是 **7 处**，**逐处都补了显式 `CaptureNow()`**」 | 铁律 5（发现文档记错了就地改掉）+ 铁律 6（数字/清单只留一处） | 调度台改 `ViewportClip.cs:205`（删「`false` 的那一批…」→「现在**没有** `false` 的视口节点」）与 `:219`（「今天的调用点 = **7**（A840 那 7 处）」）—— S1 无权改（禁区），它也没改 ✅ | 0.95 |
| **F3** | 🟡 中 | **「灭自证」缺口：把两处一起改回去仍全绿** | `Editor/ShellScene.cs:4486` `int nScrollSk = PointerLayer.ScrollCountForTest;` / `:4489` `CheckTrue(nScrollSk > 0, "（前提）滚动登记表非空…")` / `:4492` `Check(PointerLayer.ScrollCountForTest, nScrollSk, "…一条都不涨")`；同形 `:4910/:4914` · `:4976/:4980` · `Editor/CollectionScene.cs:2441/:2442/:2446`。**为什么弱**：`> 0` 只断**表非空**（表里可能有别人的条目）；若把 `Shell/DeckSelectionPopup.cs:446` 的 `RegisterScroll(Scroll)` 与 `:356` 的 `UnregisterOwnedBy` **一起**删掉，基线 = 测量值 ⇒ **照样绿** | 铁律 §三「**灭自证**：若被测实现和它的检测器用同一个口，把两边一起改回旧写法依然全绿」 | 每条再补一条**归属**判据：按「`Owner == 本窗根`」数条数（或断言本窗那颗 `Scroll Rect` 节点在登记表里），使「没登记」也红。**注意**：作者点名的改坏法（只删 `UnregisterOwnedBy`）确实能红 ⇒ 这条是**加强**，不是推翻 | 0.8 |
| **F4** | 🟢 低 | A833 覆盖仍不完整（作者已自陈，我复核属实） | `Editor/MainMenuScene.cs:5811-5846`（只覆盖 `Ranking`+`Points`）· 缺 `Name`（`:5795` 那一档下它与 `Guild Name` **互斥**，作者 §3.1 有交代）· 缺「上沿」档（作者 §6·2）· `BattleLogTab` 那一扇（作者 §6·4） | 铁律 11（有缺漏要记录、之后完全复刻） | 我的判：**不算缺陷**（作者已如实登记为待办），但 `资料/项目任务.md` §三 里该有这三条的落点，别让它只活在写手报告里 | 0.85 |
| **F5** | 🟢 低 | 批外 `.cs` 11 个（`git diff --numstat`）；无一个属 S1–S5 白名单 | `Battle/BattleDriver.cs 17/4` · `Editor/BattleScene.cs 87/28` · `Editor/CardBaseDemo.cs 11/0` · `Editor/CardFaceProbe.cs 47/8` · `Editor/DeckScene.cs 7/2` · `RuleEngine/{Core/CardInstance.cs 8/1,Core/EffectResolver.cs 14/2,Core/RuleCore.cs 20/2,Data/DeckStore.cs 34/2,Editor/DeckRulesTest.cs 10/0,Editor/RuleEngineTest.cs 135/9}`。**`Editor/CardFaceProbe.cs` mtime 23:55:52**（我 23:51 第一次 `git status` 时它还**不在**名单里）⇒ **审查期间新写入** | 铁律 13·3①（一个文件一个写手）+ 简报⑥（越界要报） | 调度台确认这批是否属**另一条线**（我猜是「战斗/引擎」那一波 + S2 §6·1 建议的那一条账）；若属本批 ⇒ 越界。⚠️ 它进同一份 diff ⇒ 同步点 `git status` 要按**所有权表**逐个数 | 0.7（归属）／0.95（事实） |
| **F6** | 🟢 低 | 同一处改动的**归属**两份报告打架 | `Editor/ShellScene.cs:229-240`（`SpanOfTmp` 的 doc 里那段「有意不收的同族」订正，含「A851 收口 · 铁律 5」「⇒ 它们仍然『不收』的理由只剩一条：逐点序列」）。S4 报告 §4·3 写「`ShellScene` 进场时是 `6/2`（**S2 的 A851 注释订正**，不是我）」；**S2 报告 §6·3 写「`ShellScene.cs:232-235` 那份清单现在半过期…**我没改它**（不是我的白名单），请调度台把那份注释订正」** | 铁律 13·3③（子代理不许改正本；越界要报）+ 简报⑤ | 若 S2 自陈「请调度台做」= 它**不该**自己动 ⇒ 那条 `6/2` 若是 S2 留下的**就是越界**；若是调度台合并时做的 ⇒ 无问题。**内容本身正确**（与 S2 建议逐字一致，且留了更正痕迹）—— 只需调度台认一下归属 | 0.6（判不出是谁） |
| **F7** | 🟢 低 | (甲) 的**还原源**与 ③ 那条不一致 | `Editor/ShopScene.cs:4726` `var vpFull435 = new PxRect(329.76f, 127.62f, 1920.00f, 1080.00f);` + `:4735` `MenuDraw.ApplyPxRect(vp435, vp435.parent, vpFull435); // 还原` vs `:4769` `MenuDraw.ApplyPxRect(vp435, vp435.parent, ShopTabPage.ScrollView); // 还原`（③ 那条，改前就在）。而 ① 那四条只断 `ClipPx` **在 0.5px 内** ≈ 字面量（`:4704-4707`）⇒ 节点真矩形**不保证逐位等于**字面量 | 铁律 §三「两处写同一条规则 = 迟早不一致」 | 把 `:4726-4728/:4735` 的字面量换成 `ShopTabPage.ScrollView`（与 ③ 同一份）—— 一行的事，且顺带证明「还原回的是**宿主那一份**」 | 0.5 |

---

## 3. 每处新断言的【判别力】逐条核

### 3·1 `Editor/ShellScene.cs`（S4 · 8 个块 / 约 30 条）

| 块 | 条 | 「把哪一处实现改坏会让它红」 | 判 |
|---|---|---|---|
| A · `:4413-4445` A840#4 | `:4421` `HasBaseRect` | 删 `Shell/LiveOpsEventWindow.cs:614` 的 `CaptureNow()` | ✅ **能答** |
| | `:4432` `armFrame` = 原版字面量 | 改 `LiveOpsEventWindow.ArmView*` 任一个 | ✅（**灭自证**：下面那条拿 `armFrame` 当期望值，本条先证它是不是原版值） |
| | `:4439` `BaseRect` 逐值 | ①删 `CaptureNow()`（`if` 进不去，连带 `:4421` 一起红）②**记成错帧**（挪完再抓） | ✅ 且**分层正确**（②只有本条抓得住） |
| B · `:4477-4499` A867#2 | `:4489` 基线 `>0` | ——（前提条，红了说明夹具失效） | ✅ 声明清楚 |
| | `:4492` 不涨 | 删 `Shell/LiveOpsEventWindow.cs:378` | ✅ **但见 F3**（`RegisterScroll` 一起删就不红了） |
| C · `:4627-4681` A840#5/#6 + 第三轴 | `:4634` / `:4637` `HasBaseRect` **分别两颗** | 删 `PracticeModePopup.cs:883` / `:1051` 各自那一句 | ✅ **且刻意不写成「至少一颗对」** |
| | `:4648` / `:4660` `BaseRect` 逐值 | 记成错帧 / 改常量 | ✅（两颗可见面不同 ⇒ 「记成同一份」也抓得到） |
| | `:4673-4677` `LiveDerivations` 差分 = 0 | 删 `PracticeModePopup.cs:883` ⇒ 差分 +1（走回实时反推支） | ✅ **第三个观测轴**（代码路径，不是状态）——**全批唯一一条**，很好 |
| D · `ShopScene.cs:4666-4690` | `:4674` / `:4682` | 删 `ShopWindow.cs:428` / 记错帧 | ✅ |
| E · `ShopScene.cs:4710-4737`（毒药订正） | `:4729` `!Intersects` | `MenuScroll.Intersects` 写回 `MenuDraw.Visible(onScreen, Viewport)` | ✅ 见 §4(甲) |
| F · `CollectionScene.cs:2433-2451` | `:2442` / `:2446` | 删 `DeckSelectionPopup.cs:356` | ✅ 但见 F3 |
| G · `CollectionScene.cs:6141-6178` | `A840Check` ×2 页 ×3 条 | 见 S4 §2·G 自陈：删 `CaptureNow()` **只有 `★` 红**（`★★` 在 `if` 里被跳过） | ✅ 作者已如实标注（`:261-263`），非缺陷 |
| H · `:4931-4988`（新节） | `:4955`/`:4965`/`:4981`/`:4983` | 删 `TutorialModePopup.cs:510` / `:324` | ✅（见 §4(乙)） |

**答不出来的：无**（三条「前提」条都写了「红了说明夹具失效，不是实现缺陷」，不算弱断言）。

### 3·2 `Editor/MainMenuScene.cs`（S3 · 22 + 19 条）

| 条 | 「改坏哪里会红」 | 判 |
|---|---|---|
| `:5738`/`:5741`/`:5787`/`:5806`/`:5830` 等「前提 / 参照行」 | ——（前提组，作者逐条写了红的含义） | ✅ |
| `:5762` `nOn>0 && nOff>0` | 删 `Shell/MenuDraw.cs:1431` `if (partial) ClipNineChildren(go, clip.Value);`（我核过：真身就在 `:1431`，`ClipNineChildren` 对完全出框的子块 `SetActive(false)`、对部分出框的截段——`Shell/MenuDraw.cs:1455-1478`） | ✅ |
| `:5768`/`:9212` `bgBot == VpBot` | 把 `LeaderboardRow.cs:154` / `MatchLogRow` 的 `clip: c.Clip` 换成 `MenuDraw.NoClip` | ✅ |
| `:5773`/`:9217` 只截下沿 | 一刀切式裁切 | ✅（有牙，弱一档） |
| `:5799` `Guild Name` 整块不建 + `:5806` 参照行建出来 | 删 `MenuDraw.Text` 开头那道闸 | ✅ **且成对**（`§3` 的自陈准确：少了 `:5806` 会让 `:5799` 假绿） |
| `:5832`/`:5834` `ex2 == VpBot` | 喂 `NoClip` / 删 `ClipText` | ✅ |
| `:5840` / `:9269` `ex1 == ry1` | ——**答不出来（它自己就是错的）** ⇒ **见 F1** | ❌ |
| `:5843` / `:9271` 真被截短过 | 只改 alpha 不动顶点 | ✅（但与 F1 那条**配对使用**才有意义；单独留它会退化成弱断言） |

### 3·3 `Editor/CollectionScene.cs` / `Editor/ShopScene.cs` / `Editor/RewardsScene.cs`

| 条 | 判 |
|---|---|
| `CollectionScene.cs:2433-2451`（A867） | ✅ 有牙（见 F3 的加强建议） |
| `CollectionScene.cs:6141-6178`（A840 ×2 页） | ✅ 逐页各一条、不写成「至少一页对」 |
| `ShopScene.cs:4666-4737` | ✅（①②③ 三条变异源分开下毒，`:4740-4776` 那条量**渲出来的真几何**） |
| `RewardsScene.cs`（S2 · A851） | ⚠️ **答不出来 —— 作者自己也算出来了**：`S2 §5②`「把三处改回 `meshInfo[0]` ⇒ **今天一条都不红**」（8 个调用点全在单槽 ⇒ 逐位同值）⇒ **本笔的回归网是空的**。我复核：`Editor/RewardsScene.cs:241-253` 那段自陈写的就是这件事，且作者**拒绝**加一条「量一颗混排字」的假断言（怕写成弱断言）—— 这个判断我同意（该工程反复踩「绿了也证明不了」）。**建议**：按作者 §5 末尾那条，`RewardsScene.Run` 时顺手打一次 `textInfo.meshInfo.Length`，≥2 才值得补牙；否则如实记「本工程不可达」 |

---

## 4. S4 自报两处的独立判定

### (甲) `ShopScene.cs` A465 毒药就地订正 —— **推理成立，改法选型正确**

1. **推理成不成立**：成立。链路我逐句读过 ——`Shell/MenuScroll.cs:404-410` `Intersects(onScreen)` = `MenuDraw.Visible(onScreen, ClipNode != null ? ClipNode.State.RenderClip : Viewport)`；`ClipNode` = `ShopWindow` 建滚动区时喂的那颗**视口节点**（`Shell/ShopWindow.cs:550-552`）；`ClipState.RenderClip` 由 `Shell/ViewportClip.cs:250-291` 的 `ClipPx` 决定；A840 给这颗节点补了 `CaptureNow()`（`Shell/ShopWindow.cs:428`）后 `HasBaseRect == true` ⇒ 走 **① 支**（`:255-271`：中心取 `_baseRect.CX/CY`，**不读实时 transform**）⇒ 光挪 `localPosition` 时**框纹丝不动** ⇒ 旧毒药下 `!Intersects` 必红 = **假红**。✅ S4 的结论对。
2. **有没有把命题改弱/改歪**：**没有**。原命题 = 「`Intersects` 的框来自**节点**（不是 `MenuScroll.Viewport` 那个字段）」。改后：`MenuDraw.ApplyPxRect(vp435, vp435.parent, 整框上移 800px)`（`:4727`）同时改**活 transform** 与**写进节点的 `BaseRect`**（`Shell/MenuDraw.cs:180-190` 尾句 `vc.SetBaseRect(r)`），`inVp435 = (500,400,700,500)` 与新框 `927.62..1880` 无交集 ⇒ `!Intersects` 成立。**判别力没丢**：把 `Intersects` 写回读 `Viewport` 字段（= 老写法）时，框会回到 `329.76,127.62→1920,1080`，`inVp435` 落在里面 ⇒ 仍红 ✅。
   ⚠️ 一处**要如实说**：新毒药**同时**改了「活 transform」与「BaseRect」⇒ 它不再能分辨两条**节点衍生**路（① / ②）；但**那不是本条的命题**（本条只问「节点 vs `Viewport` 字段」）⇒ 不算改弱。
   ⚠️ 另见 **F7**（还原用字面量、③ 用 `ShopTabPage.ScrollView`）。
3. **有没有更好的改法**：**没有更好的**。三条候选我判过：① 临时把 `BaseRect` 清掉再挪 `localPosition`（要加/改 API，且只剩②支 ⇒ 更弱）；② 改断 `vc435.ClipPx`（那就不是「毒药」而是换命题）；③ 就是它选的这条 —— 且**本文件下面 ③ 那条（`:4754`/`:4769`）与 `Editor/CollectionScene.cs:7147-7160`（A811 先例）用的都是 `ApplyPxRect`** ⇒ 选型与全仓既有下毒法一致 ✅。

### (乙) `ShellScene.cs:4931-4988` 新开一节直建教程模式窗夹具 —— **该保留**

1. **该不该在本宿主直建**：**该**。宿主既有先例**确实同族**：`ShellScene.cs:4592-4595` `PracticeModePopup.Create(shell.Windows)` + `shell.Windows.OpenWindow(pw)`；另有 `PromptPopup.Create`（`:1165`）· `ImportDeckPopup.Create`（`:3718`）· `SkirmishEventWindow.Create`（`:2919`/`:4397`）—— 全是「`GameWindow` 直系 + 只吃一台 `WindowsManager`」。我另核了被建方：`Shell/TutorialModePopup.cs:275-290` `Create(WindowsManager mgr)` **只吃 `mgr`**（`Manager = mgr` + `AttachToAnchor`），`Build()`（`:325-346`）只依赖常量表 `Rows` + 取图口 + `CollectionData` ⇒ **没有** `MainMenuRuntime` / 事件数据依赖 ✅。
2. **有没有污染别的节**（我逐项查的）：
   - **全局/静态字段**：`TutorialModePopup.LastOpened`（`Shell/TutorialModePopup.cs:271/:295`）本段会写 —— 但全仓**唯一的读者**是 `Editor/MainMenuScene.cs:2973`，那是**另一个进程**（`MainMenuScene.Run`）；`ShellScene` 内**零读者**（grep 过）⇒ **不污染**。
   - **`WindowsManager` 残留**：本段以 `shell.Windows.OpenWindow(tutW)` 开、`tutW.Close()`（`:4988`）收，**不** `DestroyImmediate` —— 与同文件既有纪律**逐字一致**（`:4922` / `:4987` 的注释、`:5116`/`:5134` 的「只 `Close()`、`shell.Dump()` 要遍历窗表」）⇒ 窗表里多留一扇 `Closed` 的窗，**是本文件一直在做的事** ✅。
   - **登记表**：本段把本窗那条 `MenuScroll` 留在 `PointerLayer`（`Close()` 不撤）—— 与 S1 的 A867 同一个已知面；**后面没有任何节读条数**（`ScrollCountForTest` 在 `ShellScene` 只有 `:4486/:4910/:4976` 三处，全在本段**之前**）⇒ 观测不到 ✅。
   - **与 `MainMenuScene §A853` 重复吗**：不重复 —— 那一节断的是**树 / 参数 / 关卡表**（走生产真路「点教程卡」），本段只断 **A840 的框记录 + A867 的登记表不涨**，并明确「⛔ 不重复断任何树结构」✅。
3. **该保留还是该删**：**保留**。它补的是**唯一**覆盖 A840#7 / A867#4 的回归网（`TutorialModePopup` 全仓只有 `MainMenuScene` 会建，而那个文件当时正被写）。
   ⚠️ **必须如实标的一条**：这是**第一次在 `ShellScene` 里建 `TutorialModePopup`** —— 我静态核过它自足，但**没实跑**；`TutorialModePopup.Build()` 在别处（`MainMenuScene`）是绿的 ⇒ 风险低，但这次的 `ShellScene.Run` 就是第一次实跑，红的话先看是不是夹具前提（不是实现）。

---

## 5. S5 的「行为不变」逐份核（`Shell/MenuWindowBase.cs` 新共用件 + 4 处转调）

我把 4 扇的 spec 与 `git show HEAD:` 的原实现**逐字段**比过（不是照抄 S5 的表）：

| 项 | Tutorial | LiveOps | DailyStreak | Energy | 判 |
|---|---|---|---|---|---|
| `RootName` | 缺省名 | `RootNameGameMode` | 缺省 | 缺省 | ✅ 逐字同 HEAD |
| `RootRect`/`PlateRect`/`TitleRect`/`WingRect`/`BackRect` | 都是**原式常量的原样搬运**（`HdrL/HdrBgT/HdrBgR…`、`HdrBg1L…`、`HdrBackL…`、`TitleL…`） | 同（含 `plateR = 690.86f`、`titleL + 369.36f`、`HdrT + 16.36f / +99.01f`） | `HeaderRoot`/`H_Plate`/`H_Title`/`H_Bg`/`H_Back` | `HdrR`/`HdrBgR`/`TitleR`/`HdrBg2R`/`BackR` | ✅ |
| `TitleText` | `TitleText` | `"Game mode"` | `DailyData.StreakWindowTitle()` | **`"Game Mode"`（大写 M）** | ✅ **大写 M 没被抹平** |
| `TitleMode` | `FitAfterSpacing`（字距→自适应） | `FitAfterSpacing` | `SpacingOnly`（**不调自适应**） | `FitBeforeSpacing`（自适应→字距） | ✅ 与 HEAD 的**语句次序**逐句相同（Tutorial/LiveOps HEAD = `SetCharSpacing` 在上；Energy HEAD = `SetAutoFitBox` 在上） |
| `TitleVAlign` | 不设 | 不设 | `Capline`（`SetVAlign` 排在 `AlignLeft` **之后**） | 不设 | ✅ 次序同 HEAD |
| `WingName` | 缺省 `Header Background (1)` | **`"Nine"`** | 缺省 | 缺省 | ✅ **LiveOps 那颗缺省名没被「顺手统一」**（S5 §7·1 已登记为独立一笔） |
| `BackStyle` | `QuadOnHeader` | `QuadOnHeader` | **`BoundOnQuad`**（`WindowButton` 挂图上、**无 `BackHit`**） | **`QuadInOwnNode`**（多一层具名节点 + 子件 `Bg`） | ✅ 三种结构各自成立 |
| `BackKeepAspect` | `true` | `true` | **`false`** | `true` | ✅ **且 `false` 那档是对的**：HEAD 的 `MenuDraw.Rect(h, …, "Header Back Button", QContent)` **没传** `keepAspect`，而 `Shell/MenuDraw.cs:1341` 的默认值**就是 `false`** ⇒ 逐位相同 |
| `BackSwapArt` | **空**（不设） | `ArtHeaderBack` | `ArtBackBtn` | `ArtBack` | ✅ = HEAD 的「有没有换图」逐扇一致 |
| 队列 `Q*` | `QArt/QArt1/QText/QArt2/QHit` | 同 | `QPanel/QPanel/QText/QContent/—` | `QArt/QArt1/QText/QArt1/QHit` | ✅ 逐窗照抄 |
| `OnBack` | `() => Close()` | `() => Close()` | `() => { StreakAutoCollect(); Close(); }` | `() => Close()` | ✅ 逐字 |
| 树/兄弟序 | 根→[底板(`Window Title` 挂底板下)]→尖角→返回钮→BackHit | 同（+ `Game Mode Icon` 补挂 `parts.Plate`，仍排在 `Window Title` **之后**） | 同 | 同（`QuadInOwnNode`：`Header Back Button` → 子件 `Bg`；`BackHit` 仍挂那颗节点） | ✅ **LiveOps 那处建法顺序变了但树逐位相同**（图标是 `plate` 的子件、不是 `hdr` 的；`hdr` 子件序不变） |
| `MenuDraw.Hit` 的 `target` | 传 `null, null` | 传 `backQuad, ArtHeaderBack` | 走 `BoundOnQuad` 支（`BindSelf`） | 传 `backQuad, ArtBack` | ✅ **S5 §5·1 那条自证我复核成立**：`Shell/MenuDraw.cs:1851` `if (target != null) wb.Bind(target, art, hoverArt, pressedArt);` ⇒ 只传 target 不传 art 会进 `Bind` ⇒ 与 HEAD 的「都不传」**不是**同一件事；共件写 `string.IsNullOrEmpty(BackSwapArt) ? null : backQuad` 正确 |
| 常量收口 | `LiveOpsEventWindow.{HeaderBorder,HeaderTexW/H}` / `DailyStreakPopup.{HeaderBorder,HeaderTexW/H}` / `EnergySingle….HdrBorder` **三份删净** | ✅ 我 grep 全仓：除三处「指针注释」与 `MenuWindowBase` 里的**一处定义**外**零引用** ⇒ 不破坏任何调用点；`PlateBorder/PlateTexW/H` 值 = `(335,0,395,0)` / `740×167`，与删掉的三份逐值相同 |
| `AlignLeft`/`SetVAlign` 的 null 安全 | —— | —— | ✅ **S5 §5·1 第二条我复核成立**：`Shell/MenuDraw.cs` 的 `AlignLeft` 头一句 `if (lb != null)`、`SetVAlign` 同为 `if (lb != null)` ⇒ 「挪进 `if (title != null)`」与 HEAD 的「无条件调」**等价** |

⇒ **四窗行为逐点不变，成立**。唯一**实质**差异是 `Tex(...)` 调用次数 2→1（Tutorial/LiveOps 的底板+尖角共用同一个 `Texture2D` 实参）—— 取图口是「查缓存 + `MissingArt` 去重记账」⇒ 读数与清单都不变（S5 §5·3 的自陈我认可）。
⚠️ 我要补一条 S5 没说的：**本批没有为 `WindowHeader` 新增任何断言**，四扇的净 = 既有断言（`MainMenuScene.cs:2992-3011` / `:4576/:4613/:4619/:4621` / `:9809/:9874` · `RewardsScene.cs:7023-7122`）。这是**可接受**的（它们本来就盖这四颗），但同步点跑 `MainMenuScene.Run` + `RewardsScene.Run` 时**那几条必须真绿**——那是本笔唯一的牙。

---

## 6. 越界 / 禁改项 / 行尾 三项体检

### 6·1 文件所有权：S1–S5 的改动**都在各自白名单内**（除 **F6** 那处归属不清）

| 文件 | 谁 | 与它自陈一致吗 |
|---|---|---|
| `Shell/{ShopWindow,AvatarTab,TitleTab,LiveOpsEventWindow,PracticeModePopup,TutorialModePopup,DeckSelectionPopup}.cs` | S1 | ✅（S1 自陈 7 文件；`TutorialModePopup` 超出**账上**点名、S1 已自陈「可单独回退」；`DeckSelectionPopup` 是 RS §顺手发现 4 点名要它判的候选） |
| `Shell/MenuWindowBase.cs` + `Shell/{TutorialModePopup,LiveOpsEventWindow,DailyStreakPopup,EnergySinglePlayerOnlyEventWindow}.cs` | S5 | ✅（`TutorialModePopup`/`LiveOpsEventWindow` **与 S1 同文件不同区域**，我逐 hunk 看过 —— 两笔**区域不重叠、都完好**，没互相覆盖） |
| `Editor/{ShellScene,CollectionScene,ShopScene}.cs` | S4 | ✅（**但 `ShellScene.cs` 另有一段不是它的**，见 F6） |
| `Editor/RewardsScene.cs` | S2 | ✅（且它自陈没碰 `Editor/ShellScene.cs`，见 F6） |
| `Editor/MainMenuScene.cs` | S3 | ✅（diff = `327/0`，两个 A833 块；`MainMenuScene` 在本批**只有 S3 一个写手**，我逐 hunk 确认无第二路） |
| `Shell/ViewportClip.cs` | 调度台 | ✅ **只改了文件头注释**：`git diff` 的 hunk 落在 `:66-77` 那段**注释**里（`9/5`），实现体（`:180-323`）**一个字节没动** —— 我另核了 `Hang`/`CaptureNow`/`ClipPx`/`SetBaseRect` 全未变 ⚠️ **但见 F2**（文件里另两处旧口径还在） |
| **批外 11 个 `.cs`** | 非 S1–S5 | ⚠️ 见 **F5**（含审查期间新写入的 `Editor/CardFaceProbe.cs`） |

### 6·2 行尾：**16 个在批文件全部纯 LF、CRLF=0**（二进制读 `\r\n` vs `\n`），且 `numstat` 远小于行数 ⇒ **没有翻车**

```
Shell/ShopWindow.cs              0/907      Shell/MenuWindowBase.cs        0/812
Shell/AvatarTab.cs               0/377      Shell/ViewportClip.cs          0/442
Shell/TitleTab.cs                0/313      Editor/ShellScene.cs           0/5657   (193/2)
Shell/LiveOpsEventWindow.cs      0/1177     Editor/CollectionScene.cs      0/7224   (59/0)
Shell/PracticeModePopup.cs       0/1997     Editor/ShopScene.cs            0/4823   (41/5)
Shell/TutorialModePopup.cs       0/749      Editor/RewardsScene.cs         0/10508  (61/20)
Shell/DailyStreakPopup.cs        0/647      Editor/MainMenuScene.cs        0/11215  (327/0)
Shell/DeckSelectionPopup.cs      0/755
Shell/EnergySingle…EventWindow.cs 0/610
```
（`numstat` 逐文件：`ShopWindow 7/0` · `AvatarTab 7/0` · `TitleTab 7/0` · `LiveOps 58/62` · `Practice 23/0` · `Tutorial 46/38` · `DailyStreak 44/64` · `DeckSel 13/0` · `Energy 35/23` · `MenuWindowBase 247/0` · `ViewportClip 9/5` —— **无一接近文件行数**。）

### 6·3 禁改项

- ✅ `Shell/ViewportClip.cs` **只动注释**（见 6·1）。
- ✅ `Editor/**` 的改动只在 S2/S3/S4 各自的白名单里（`RewardsScene` / `MainMenuScene` / `{ShellScene,CollectionScene,ShopScene}`）。
- ✅ 两张正本：`CLAUDE.md` **不在** `git status` 里；`项目任务.md` **在**（`48/24`）—— 按铁律 13·3③ 那是**只有主对话能写**的 ⇒ 我没把它算作 S1–S5 的越界（判据：它改的是 A 表条目那类内容，与五写手的题面无关）。

---

## 7. 我核过但【没问题】的部分（一行一条）

1. `Shell/ViewportClip.cs` 实现体未被 `CaptureNow` 之外任何改动碰到；`CaptureNow()` 本体（`:222`）与 `Hang`（`:312-323`）逐字未变。
2. S1 的 7 处 `CaptureNow()` 位置正确：一律在 `AddComponent<ViewportClip>()` 之后、`padding/softness` 之后、**建子件之前** —— 与 `ViewportClip.Hang:314-321` 的形状逐句同形；**没有**人把它塞进 `OnEnable`（文件头 `:76-77` 明令禁止）。
3. S1 的 4 处 `PointerLayer.UnregisterOwnedBy(gameObject)` 都在 `Build()` **首句**（清子件之前），与 `Shell/InboxWindow.cs:405` 同形；`Shell/PointerLayer.cs:201-209` 的实现对 `Instance == null` 早退 ⇒ **幂等、无副作用**。
4. `PracticeModePopup` / `LiveOpsEventWindow` / `TutorialModePopup` / `DeckSelectionPopup` 的 `Owner` 我逐处核过都是 `gameObject`（窗根）：`PracticeModePopup.cs:891/:1061` · `LiveOpsEventWindow.cs:624` · `TutorialModePopup.cs:500` · `DeckSelectionPopup.cs:443` ⇒ 「`Build()` 只清子件 ⇒ `Owner` 不死 ⇒ 兜不住」这条根因**成立**，修法对症。
5. S1 **没**给 `Shell/ShopWindow.cs` 加 `UnregisterOwnedBy`（因为那条 `Owner` 是**页节点**、会被 `DestroyChildren` 连带销毁 ⇒ 自动可清）—— 这个「不加」是对的（加了会是空操作），我核过 `Shell/ShopWindow.cs:550` `_gridScroll.Owner = _root.gameObject` 与 `Shell/MenuWindowBase.cs:353` 的 `DestroyChildren`。
6. S1 **没**动 `Shell/DailyStreakPopup.cs` 的登记（该窗**压根不登记** —— 文件 `:215-221` 自己写着原版那颗 `ScrollRect` `m_Enabled = 0`）⇒ RS §顺手发现 4 那条「形状一致」的推断被 S1 正确否掉，我 grep 确认该文件零 `RegisterScroll`。
7. S4 对 `sk.Open()` 的副作用判断正确：`Shell/LiveOpsEventWindow.cs:341-352` 的 `Open()` = `Build()`（先清子件）+ `SearchingMatchPopup.Attach` ⇒ 上一次那个搜索弹窗**被 `Build()` 连子件一起销毁**，**不累积**；且 `Shell/SearchingMatchPopup.cs` 不碰 `PointerLayer`（S4 §6·3 的核对我复核同意）。
8. S4「先吸基线、后测量」的写法与 `Editor/RewardsScene.cs:8133-8136`（A510 先例）同形。
9. S3 的夹具数值自洽：排行榜 `a833Off = 288.59 + 6×115 + 100 − 937.83 − 55 = **85.76** ≤ `MaxOffset`（8 行 = 905 − 649.24 = **255.76**）✅；对局历史 `mOff = 130 + 3×228.2 + 203.2 − 963 − 28 = **26.8**` ✅。
10. S3 的「`Guild Name` 整块出框 ⇒ 连节点都不建」几何成立：`Shell/LeaderboardRow.cs:107` `GuildR = (290, 50 → 1000.79, 96)`（行内），可见带 = 行内 `0..45` ⇒ 整块在框下 ✅；两颗被测字（`RankR = (12,14.04→112,85.96)`、`PointsR = (1060,11.27→1200,89.24)`）**跨过 45** ⇒ 会建、会被夹 ✅（夹具设计成立）。
11. S3 的节点名逐一对上实现：`Shell/LeaderboardRow.cs:120` `"PlayerRankingRow"` · `Shell/MatchLogRow.cs:195` `"Match Log"` · `:240` `"Player Info"/"Enemy Info"` · `:253` `"Alliance Name"` ✅。
12. `Check<T>(got, want, msg)` 的实参序（`Editor/ShellScene.cs:27` 等）与 S4 的 `Check(PointerLayer.ScrollCountForTest, 基线, …)` 一致 ✅。
13. `Shell/MenuDraw.cs:1431` (`if (partial) ClipNineChildren(go, clip.Value);`) 与 `:1455-1478`（`ClipNineChildren`：完全出框 `SetActive(false)`、部分出框截段 + 逐子块）**都存在且行为与 S3 的「改坏法」描述一致** ⇒ S3 那三条的牙是真的。
14. S2 的三处取槽改法**不是我发明的口径**：与 `Shell/MenuDraw.cs:1148-1150`（生产侧 `ClipTmpMesh` 按 `materialReferenceIndex` 裁）**同源** —— 量法必须读「被裁的那一份数组」✅。
15. S2 的注释订正留了**完整更正痕迹**（「原来写 X / 为什么不对 / 当天就改了」），符合铁律 5 的写法；`ShellScene.cs:229-240` 那段同理。
16. S5 的三处常量删除**留了指针注释**（不是静默删），符合铁律 5。
17. `Shell/MenuWindowBase.cs` 新增的 `WindowHeader` 是**同一个文件内**的静态类（调度台指定「优先复用已有文件」），没有新开文件 ✅；`Spec`/`Parts` 是 `public` 且**没有**动 `MenuWindowBase` 既有成员。
18. S5 把 `LiveOpsEventWindow` 的尖角名显式写成 `"Nine"`（= 缺省名）**是对的**：`Shell/MenuDraw.cs:1405` 的 `Nine(...)` 末参默认就是 `"Nine"` ⇒ 逐字同 HEAD（改行为的那一笔被正确地留成独立待办）。
19. `Editor/ShopScene.cs` 的 ③ 块（`:4740-4776`）与 ④ 块（`:4778-4800`）**未被 S4 改动**，两处下毒法（`ApplyPxRect`）与 ② 那条现在**同形** ✅。
20. 本批**没有**发现任何「把实现删掉断言还绿」的**空断言**（各条的「前提」组都是显式 `CheckTrue`，取不到就红；`S4 §2·G`、`S3` 的 `continue` 都配了前置红）。
