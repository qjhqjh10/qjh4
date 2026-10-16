# FD · 清幂等包装 + 过期注释（F10 余三处 / W74 / G1-3 / G1-5）（修波）

> 执行：执行子代理 `FD` · 2026-10-16 · ⛔ **未跑 Unity**（只跑了秒级 `typecheck`）/ ⛔ **未动 git**（只跑过只读的 `git diff --numstat`）/ ⛔ 未改任何白名单外文件与两份正本。
> 白名单（5 个 `.cs` + 本报告）：`Battle/BattleDoors.cs` · `Battle/CardChoicePanel.cs` · `Battle/SettingsPanel.cs` · `Battle/HudCanvas.cs` · `Core/VoiceLines.cs`。
> 输入 = `R1_面板迁移审查_报告.md`（§②-10 / §④·1 / §③ `G1-3` `G1-5`）· `G1_BattleDriver收口_报告.md`（§③）· `_收口台账_第四十二会话.md`（`F10` / `W74`）。

---

## ① 结论 —— 逐条「做了 / 没做 + 为什么」

| # | 条目 | 结论 |
|---|---|---|
| **F10-a** | `Battle/BattleDoors.cs` `PairCanvas()` 的幂等包装 | ✅ **做了**（删包装 + 订正过期话 + 留痕）。⚠️ **保留了两行**（见下「三处共同点」） |
| **F10-b** | `Battle/CardChoicePanel.cs` `Build()` 的子画布包装 | ✅ **做了**（同上） |
| **F10-c** | `Battle/SettingsPanel.cs` `Create()` 的子画布包装 **＋** 那句 const doc | ✅ **都做了**（两处：那句过期话在 `:657-664`，包装在 `:705-708`） |
| **F10-d** | `Battle/MultiCardDisplay.cs:204-207`（第 4 处） | ⛔ **没做** —— **不在本件白名单**（简报点名「在别人的文件里」）。→ §③ #2 |
| **W74** | `HudCanvas` `SubCanvasRank` 表头补一句 | ✅ **做了**（`:535-539`） |
| **G1-5** | `HudCanvas` 加只读判据口 `IsRanked` | ✅ **做了**（`:581-584`）。🔑 **签名逐字 = `G1` 报告 §③ 自己建议的那个** |
| **G1-3** | `Core/VoiceLines.cs` `ForChatButton` doc 后半句过期 | ✅ **做了**（`:366-372`）；🔑 **行号是现核的**（`G1` 写 `:364-366` ✓ 未漂） |

### 三处共同点（**这是本件最重要的判断，请主核对一眼**）

三处的包装**都长这个样子**（以 `SettingsPanel` 为例）：

```csharp
HudCanvas.Ensure(LayoutSpace.Cam);          // ← ① 保留
var cv = HudCanvas.SubCanvas(SubCanvasName); // ← ② 包装：删
var cvRt = cv != null ? (RectTransform)cv.transform : HudCanvas.EnsureSubCanvas(…); // ← ② 包装：删
if (cvRt == null) { …; DestroyImmediate(go); return null; }  // ← ③ 保留
```

⇒ **逐处核过「删掉的那部分 = `EnsureSubCanvas` 自己做的事」**：`EnsureSubCanvas:489-496` 的复用路径
（`SubCanvas(name)` 命中 ⇒ 对齐层/order ⇒ `ApplySubCanvasRank` ⇒ 返回 `(RectTransform)transform`）
**就是**包装那两行想做的事，且**多做了两件它该做的**（对齐 + 重排次）。✅ 等价（唯一极微差异见 §⑤·1）。

**⚠️ 但我【没有】把整个方法照抄成一行** —— 两处**不属于包装**的东西**原样留了**，理由是简报的「多做了事就别删」：

| 保留项 | 为什么留 | 删了会怎样 |
|---|---|---|
| `HudCanvas.Ensure(LayoutSpace.Cam)` | 三处都有，且**正是收口波 `W8`/`W43` 那族刚补上的必需项**（「`EnsureSubCanvas` 之前先 `Ensure`」） | 会退回 `W43` 修掉的**半迁移态**（Glow 贴图缺失档静默拿不到子画布） |
| 各处的 `cvRt == null` guard | **三处的 guard 都不是「出声」一件事**：`CardChoicePanel` 会 `SetVisible(false); return;` · `SettingsPanel` 会 `DestroyImmediate(go); return null;` · `BattleDoors` 给的是**本对专用**的错误文案 | 各扇的「建不起来时不建这一件」那一半**会消失**（`EnsureSubCanvas` 自己只出声 + 返 `null`，**不会**替你撤件） |

⇒ **删的只有「先按名字查、没有再建」这一层**，三处的方括号结构与四行结论**与改之前逐位相同**。

---

## ② 改动清单 —— 文件:行号 + 一句话

| 文件（行号**现核**） | 一句话 |
|---|---|
| `Battle/BattleDoors.cs:195-217` | `F10`：`PairCanvas` 里那三行包装（`SubCanvas` 查 + 早返回 + 兜底建）**删掉**，改为**直接** `return HudCanvas.EnsureSubCanvas(null, CanvasName, PopUps, 0);`；`:195-199` 那段过期 doc（原写「`EnsureSubCanvas` 每调一次就新建 ⇒ 先按名字找」）**就地订正 + 留痕** |
| `Battle/CardChoicePanel.cs:218-223` | `F10`：`Build()` 的 `var cv = SubCanvas(...)` + 三元兜底**删掉**，`cvRt` 直接吃 `EnsureSubCanvas(...)`；加一段**订正痕**（原话「`EnsureSubCanvas` 不去重」已过期） |
| `Battle/SettingsPanel.cs:657-664` | `F10`：**const doc 那句过期话**订正 + 留痕（原写「`EnsureSubCanvas` 不会去重，所以 `Create` 里先按名字找一次」） |
| `Battle/SettingsPanel.cs:705-708` | `F10`：`Create()` 里同形的两行包装**删掉** + 订正痕 |
| `Battle/HudCanvas.cs:535-539` | `W74`：`SubCanvasRank` **表头补一句**（逐字含简报要的那句「跨层先后由 `(排序层, order)` 决定；本表的前后只代表【同层之内】的先后」＋ 点明**数组前 4 项是 `PopUps` 层、排在最前只为拿最小兄弟序**） |
| `Battle/HudCanvas.cs:571-584` | `G1-5`：新增 **`public static bool IsRanked(string name)`**（body = `!string.IsNullOrEmpty(name) && System.Array.IndexOf(SubCanvasRank, name) >= 0`），doc 明写**用途 = 给「登记后读回自检」用**、并声明**只读不改表**、且**表里没有 ≠ 建错了**（`LoadingMenu`/`TooltipManager` 本就不该进表） |
| `Core/VoiceLines.cs:366-372` | `G1-3`：`ForChatButton` doc 后半句（「…放不下；**要补得先改那个消费点**」）**已过期** ⇒ 就地订正 + 留痕（现读消费点 `BattleDriver.cs:4202` **已带 `"default"` 第二跳**）+ 保留 `A8` 建议的「更干净的收法**未做**」 |

### `git diff --numstat` ＋ **逐文件行尾核对**（**改前 / 改后都二进制读过**）

判据 = `io.open(p,'rb')` 数 `b'\r\n'` vs `b'\n'`（⛔ **不是**文本模式 —— 那个两数恒相等）。

| 文件 | **改前** LF / CRLF | **改后** LF / CRLF | 判定 | `git diff --numstat`（vs `HEAD`） |
|---|---|---|---|---|
| `Battle/BattleDoors.cs` | 482 / **482**（纯 CRLF） | 490 / **490**（纯 CRLF） | ✅ **没翻**（行尾比例 1:1 恒定） | `12 4`（净 +8 = 本例贡献） |
| `Battle/CardChoicePanel.cs` | 621 / **0**（纯 LF） | 623 / **0** | ✅ 没翻 | `6 4`（净 +2 = 本例贡献） |
| `Battle/SettingsPanel.cs` | 2167 / **0** | 2171 / **0** | ✅ 没翻 | `10 6`（净 +4 = 本例贡献） |
| `Battle/HudCanvas.cs` | 629 / **0** | 649 / **0** | ✅ 没翻 | `111 10`（⚠️ **含本批之前 `G3` 的在飞改动**；本例净 +20） |
| `Core/VoiceLines.cs` | 584 / **0** | 590 / **0** | ✅ 没翻 | `90 31`（⚠️ **含本批之前 `A8` 的在飞改动**；本例净 +6） |

- 🔴 **`BattleDoors.cs` 是纯 CRLF**（简报提醒「别信我、以现核为准」是正确的）—— 用 `Edit` 工具改，**改后 CRLF 计数与总行数仍相等** ⇒ 没混行尾、**不是整篇重写**（净 +8 ≪ 490）。
- ⚠️ **`HudCanvas.cs` / `VoiceLines.cs` 的 numstat 不能当「本件改了多少」**（那两个文件在我开工前**就已是 `M`**：`G3` +90/−9、`A8` +84/−31）⇒ 上表「本例贡献」那一列是按**文件行长差**现算的（净 +20 / +6，与我的编辑逐行对得上）。
- ✅ **改前 md5**（供主对话回查）：`HudCanvas.cs` = **`73fc43593bb7b2ce1d6a7d51121e64fa`** —— **与 `R1` 报告 §0·1 记的那个逐字相同**（⇒ `R1` 审的就是我这一版，本件没让它过期）。
- ✅ **改后 md5**：`BattleDoors.cs` `b0cd3bb2b3c101280202269f5671e235` · `CardChoicePanel.cs` `adae5c3d83e3e600d86e5b59cdbd24b6` · `SettingsPanel.cs` `1a7c4995c19bfccdc9d9bd508702a1dc` · `HudCanvas.cs` `03d51d6dbd7101ece0110a1a09e1f94c` · `VoiceLines.cs` `cfbb07388596037208f494fe17c98d97`。

### `typecheck`（`TMPDIR=/tmp/wf_fd bash d:/4/Unity/工具/typecheck.sh`）

- **跑了两遍**（全部编辑完后一次；最后一处一字订正后又跑一次）：**运行时错误数: 0 / 编辑器错误数: 0** ✅
- ⚠️ **零次「别人的文件」串扰**（本波没遇到简报预告的「源码互相可见」假错）。

---

## ③ 接线清单（**白名单外**该改的 —— **只报不改**）

| # | 落点（**现核**） | 该改什么 | 为什么 |
|---|---|---|---|
| **FD-1** | `Battle/BattleDriver.cs:14677-14686`（`EnsureRankedSubCanvas`）＋ 它的 doc `:14671-14676` | ① **补一条真检查**：`if (!HudCanvas.IsRanked(name)) Debug.LogWarning($"[Battle] 子画布 \`{name}\` 不在 \`HudCanvas.SubCanvasRank\` 表里 ⇒ 它永远排不上次序…");` ② 订正 doc `:14674-14676`：那句「**想把它变成真检查，得让 `HudCanvas` 开一个 `public static bool IsRanked(string name)`（📌 已记进接线清单）。在那之前…靠人工核**」—— **前半句本件已办**（`HudCanvas.cs:581` 现读就有这个口，**签名逐字相同**），只剩「**接上去**」这一步 | 那处自检**今天只验「刚建出来的名字读得回来」**（`HudCanvas.SubCanvas(name) == null`，`:14681`）—— 刚 `EnsureSubCanvas` 过、必然读得回来 ⇒ **它查不到「这个串在不在排次表里」**（表是 private）。`G1-5` 要的正是这个口，现在口有了、**线没接** |
| **FD-2** | `Battle/MultiCardDisplay.cs:204-207` | 同 `F10`：删「`SubCanvas` 查 + 三元兜底」两行 ⇒ 直接 `EnsureSubCanvas`；⚠️ **`cvRt == null` 那段 `DestroyImmediate(go)` guard 保留**（同 §① 表） | **`F10` 的第 4 处**，本件白名单外（简报已点名不归我） |
| **FD-3** | 🆕 `Battle/BattleLogPanel.cs:743-755`（`ReuseOrMakeCanvas()`） | ~~同法可删~~ → ✅ **主对话追加后【已办】**（白名单已扩到该文件）：见下面「**补批**」一节 `FD-4`。**此处保留原判据**（`R1` 的 ②-10 与台账 `F10` 都只列了 4 处、**漏了这一处**；它与 `PairCanvas` **逐句同形**） | ~~§⑥·1 的落盘；若主对话认可，与 `FD-2` 同批做掉更省~~ ⇒ **已完成** |
| **FD-4** | `Editor/BattleScene.cs`（`F6` 那条**待补**的 `SubCanvasRank` 跨画布次序断言） | **可选**：那条断言若要顺带断「表里每项都真的建得出来」，判据侧可直接用 `HudCanvas.IsRanked(name)`（⛔ **别**再自己 `Array.IndexOf` 或抄一份名字清单） | 同一条规矩的第二份写法；`F6` 归 `BattleScene.cs` 的写手，本件只提名可用口 |

---

## ④ 判据与出处

1. **`F10` 三处「等价」的判据 = 代码本身**（现读，不是转抄）：
   - 被删的包装：`BattleDoors.cs:208-210`（改前）· `CardChoicePanel.cs:218-221`（改前）· `SettingsPanel.cs:701-704`（改前）。
   - 替它的那个口：`HudCanvas.cs:480-514` `EnsureSubCanvas`，**复用路径 `:489-496`**（`SubCanvas(name)` 命中 ⇒ 对齐 `overrideSorting`/`sortingLayerID`/`sortingOrder` ⇒ `ApplySubCanvasRank()` ⇒ 返回 `(RectTransform)transform`）。
   - 「那句注释已过期」的判据 = `EnsureSubCanvas` 自己的 doc `:475-479`（**2026-10-16 就地订正**那段，明写「现在改成【按名字幂等】」「⛔ 调用点不要再自己包一层」）。
   - **`R1` §②-10 末行**「4 处 `SubCanvas(name)+EnsureSubCanvas` 包装**删掉** + 把过期注释改掉」= 本条的任务来源。
2. **`W74`**：台账 §2·21 `W74` + `R1` §④·1（`R1` 原话：建议在表头补「数组顺序 = 组内先后；跨层由 (层, order) 决定，本表的前后**不代表**跨层先后」）。层序判据 = `HudCanvas.SortingLayer` 那段的注释（`Default < … < OverHand < PopUps < Blocking`）—— **`OverHand` 整组确实在 `PopUps` 之下**，故表尾那句只在同层内成立。
3. **`G1-5`**：`G1` 报告 §③ `G1-5` 一行（它给的签名建议 = `public static bool IsRanked(string name)`（= `System.Array.IndexOf(SubCanvasRank, name) >= 0`））· `BattleDriver.cs:14671-14676` 那段 doc 自己写着「得让 `HudCanvas` 开一个 …」。**⛔ 表的内容与顺序一个字没动**（`G3` 补全 / `R1` 审过 —— `HudCanvas.cs` 改前 md5 与 `R1` §0·1 逐字相同，可反证）。
4. **`G1-3`**：`G1` 报告 §③ `G1-3` · 判据 = **消费点现读**：`BattleDriver.cs:4202` = `TryPick(w.Card.Id, new[] { VoiceLines.ForChatButton[idx], "default" }, null, …)`（`G1` 本波所改，**我逐字读过**）。
5. **行尾**：`io.open(p,'rb')` 数 `b'\r\n'` / `b'\n'`（改前改后各一次，见 §② 表）。CLAUDE.md §二 明写「判行尾**必须**二进制读（文本模式两数恒相等）」。

---

## ⑤ 没查清 / 没做的 —— ⛔ 不猜

1. 🔴 **唯一的极微行为差异（如实报，请主核）**：删包装后，**「复用路径」多做了两件事** —— 原来包装命中 `SubCanvas(name)` 时是**原样返回**（不对齐层/order、不调 `ApplySubCanvasRank`），现在走 `EnsureSubCanvas` 的复用路径 ⇒ **会对齐层/order ＋ `ApplySubCanvasRank()`**。
   - **为什么我判它「不算改行为」**：① 这三处传进去的 `(层, order)` **本来就是它们要的那一对**（`PopUps/0` · `OverHand/0` · `PopUps/0`）⇒ 对齐是**恒等赋值**；② `ApplySubCanvasRank()` 是**把「谁先建」的影响抹平**，**不制造**新次序 ⇒ 方向是「更对」；③ 这正是 `EnsureSubCanvas` doc `:478` 明写的设计（「复用 + 把层/order 对齐到本次传的值」）。
   - **⚠️ 没查清的部分**：**没跑 Unity**（现行口径）⇒ 「实际有没有可观察差异」**没有实跑核过**；严格说这是**静态核**的结论。若主对话要零风险，可回退成「保留包装」—— 但那与本件任务（`F10`）直接冲突。
2. ⛔ **没做（白名单外）**：`F10` 第 4 处（`MultiCardDisplay.cs:204-207`）· 🆕 同族第 5 处（`BattleLogPanel.cs:750-755`）· `EnsureRankedSubCanvas` 的接线（`BattleDriver.cs`）—— 全部见 §③。
3. ⚠️ **`CardChoicePanel` 里那句「理由（写在别处）」是我加的措辞** —— 改前那句过期话**原文不在 `CardChoicePanel.cs` 里**（它只写代码、没写理由），我在订正痕里照实标了「写在别处」；**理由的原文在 `SettingsPanel.cs:659-660`（旧）与 `BattleDoors.cs:196`（旧）**。⇒ **这一句不是判据、只是叙述**，主对话若嫌含糊可直接删我这半句。
4. ⛔ **本件没做任何「断言」**（`SubCanvasRank` 的跨画布次序断言 = `F6`，归 `BattleScene.cs` 的写手；`IsRanked` 只是把口备好，见 §③ `FD-4`）。⇒ **本件的改动【零断言覆盖】** —— 与 `R1` §④/§项7 报的同一个缺口同源。

---

## ⑥ 顺手发现（**只报不改**）

1. 🔴 **`F10` 那一族的幂等包装其实是【5 处】不是 4 处** —— `Battle/BattleLogPanel.cs:750-755` 的 `ReuseOrMakeCanvas()` 与 `BattleDoors.PairCanvas()` **逐句同形**（`SubCanvas` 查 + 早返回 + `EnsureSubCanvas` 兜底），`R1` §②-10 与台账 `F10` **都漏了它**。佐证：`HudCanvas.cs:477` 自己举的例子里就有这个函数名（「各自在外层写了一个幂等包装：`PairCanvas` / `ReuseOrMakeCanvas` / …」）。→ §③ `FD-3`。
2. ⚠️ **`HudCanvas.cs:477` 那半句现在会读成「过期」**（本件造成）：它写「批 2 三个写手各自…写了一个幂等包装：`PairCanvas` / `ReuseOrMakeCanvas` / …」—— 作为**历史叙述**仍成立，但 `PairCanvas` **这个名字还在**、包装已删 ⇒ 下一个人可能拿它当「现役包装」的例子。**建议**（⛔ 我没动，按简报「顺手觉得该改的 → 报告⑥」）：在那行后面补一句「（`PairCanvas` 如今只剩名字，包装已删 —— 见 `BattleDoors.PairCanvas` 的 doc）」。**落点在我白名单内，主对话一句话我就改。**
3. ⚠️ **`CardDisplayWindow.cs:303` / `MulliganPanel.cs:598` 的 doc 是本族「已经做对」的样板**（「`EnsureSubCanvas` 按名字幂等 ⇒ 直接调它、别自己再包一层」）—— 本件删掉三处包装之后，**全仓这种「已做对」的面板从 2 处变成 5 处**（`CardChoicePanel` / `SettingsPanel` / `BattleDoors` 现在的措辞与它们同口径）。⇒ 剩下**唯一**还对不上的就是 §⑥·1 那两处（`MultiCardDisplay` / `BattleLogPanel`）。
4. ✅ **顺手复核：`BattleDoors.PairCanvas()` 的两个调用点不受影响** —— `BattleDoors.cs:233`（`Create` 的 host 兜底）与 `EndPanel.cs:182`（`var cv = BattleDoors.PairCanvas();` + `:183` 的 `cv == null` 分支）。两处吃的都是**返回值**、语义一字未变（`null` ⇒ 「不建这一件」那条路仍是 `PairCanvas` 自己出声）✓。
5. ✅ **顺手复核：`CardChoicePanel` / `SettingsPanel` 里那个局部变量 `cv` 没有第二处消费** —— `grep "\bcv\b"` 现核：`CardChoicePanel` 只有 `:218-221`（改前）那一族 · `SettingsPanel` 只有 `:701-704`（改前）＋ `cvRt` 的 `:715-716` 消费 ⇒ **删 `cv` 不会留下未使用变量、也不会漏一处赋值**（`typecheck` 0/0 亦印证）✓。
6. ⚠️ **`SettingsPanel.cs:32` 的文件头也提到过子画布**（`// · **子画布** = HudCanvas.EnsureSubCanvas(null, "BattleSettingsPanel Canvas", …`）—— 现读**它没写「不会去重」那句**（那句只在 `:659-660`）⇒ **本件不用动它**（如实记，免得主对话以为漏了一处）。

---

## 补批（主对话中途追加的 5 项）—— 全部完成

> 追加时间在首份报告落盘之后。**白名单扩到 `BattleLogPanel.cs` 与 `UnitChatPanel.cs`**（持有它们的 `G5` 已收工）。

| # | 条目（行号**现核**） | 结论 |
|---|---|---|
| **FD-1** | `Battle/BattleDoors.cs:324-331` | ✅ **做了** —— `_screenRt` 那句中心锚特化形 `new Vector2(cxPx − 960f, 540f − cyPx)` 改走 `HudCanvas.SetAnchorPx(_screenRt, 0.5f, 0.5f, cxPx, cyPx)`；⚠️ `pivot` 仍**显式写**（`SetAnchorPx` 只管锚点+位置、不碰 `pivot`），`anchorMin/Max` 由它设成同一对 `(0.5,0.5)` ⇒ **逐位等价（§④·6 复算）** |
| **FD-2** | `Battle/HudCanvas.cs:275-286` | ✅ **做了** —— 那段 doc 改成**过去时** + 留痕；补「**现状：三个副本已全删，算式全仓只剩 `AnchorApPx`＋`SetAnchorPx` 一处**」（本波 `grep ApPx\|PanelAnchorX\|RefW\|RefH` 现核过）；并把下面那段「应当改用本口的」显式标成**建立时的待办、已办结** |
| **FD-3** | `Battle/UnitChatPanel.cs:29-45`（`G1-4`） | ✅ **做了** —— 「查不到的（所以是我们挑的）」**整段订正**：① **判据有**（`GetChatSound` 的 `switch` 把枚举号→字段偏移钉死）② **行号也错**（`§三·〇 :127` → 真落点 `资料/战斗UI_原版对账表.md:**112**`）③ 「事件→后缀**是我们定的**」**不成立**；✅ **只保留仍然成立的那半句**（文本层显示选择是我们挑的，原文一字不删） |
| **FD-4** | `Battle/BattleLogPanel.cs:743-761` | ✅ **做了** —— **F10 第 5 处**（`R1` §②-10 与台账 `F10` **都漏了它**）：`ReuseOrMakeCanvas()` 那两行包装删掉、直调 `EnsureSubCanvas`；doc 一并订正（连带「同批 `BattleDoors` 用的是同一个做法」那句也过期） |
| **FD-5** | `Battle/HudCanvas.cs:478-481` | ✅ **做了** —— `:477` 那半句补一句「那几个包装本波已全删 ⇒ 只剩方法名」 |
| ℹ️ | `MultiCardDisplay.cs:204-207` · `BattleDriver.cs:14677-14686` | ⛔ **没碰**（归 `FB` / `G1`，主对话已发消息） |

### 补批的行尾核对 + `typecheck`

| 文件 | 改前 LF/CRLF | 改后 LF/CRLF | 判定 | numstat(vs HEAD) |
|---|---|---|---|---|
| `Battle/BattleDoors.cs` | 490/**490** | 496/**496** | ✅ 纯 CRLF 没翻（本例累计 **+14**：F10 +8、FD-1 +6） | `20 6` |
| `Battle/BattleLogPanel.cs` | 1056/**1056** | 1062/**1062** | ✅ 纯 CRLF 没翻（本例 **+6**） | `10 4` |
| `Battle/HudCanvas.cs` | 649/0 | 661/0 | ✅ 纯 LF 没翻（本例累计 **+32**） | `123 10` ⚠️含 G3 在飞 |
| `Battle/UnitChatPanel.cs` | 681/0 | 694/0 | ✅ 纯 LF 没翻（本例 **+13**） | `331 42` ⚠️含 A12/G5 在飞 |

`typecheck`（`TMPDIR=/tmp/wf_fd`）**补批又跑一遍 = 0 / 0** ✅（本件共跑 **3 遍**，全程零「别人的文件」串扰）。
**本件 7 个文件的净增合计 = +77 行**（14+6+2+4+32+13+6）。

### ⚠️ 两处我自己的判断 / 返工（如实记）

1. 🔴 **`FD-3` 我返工了一轮**：第一版只替换了原注释的**前 2 行**，把原文的「文本层」2 行**留成了孤句、并与我新写的 ✅ 段重复**；随后**又改一次**收干净（现读 `:41-45` = 我的 ✅ 段明确指「下面紧接着那两行文本层」，**原文一字不删**）。⇒ `UnitChatPanel.cs` 本件实际改了两轮。
2. 📌 **一处【判了不改】的（请主核）**：`BattleLogPanel.ReuseOrMakeCanvas()` 现在**只剩一句转发**。我**没有**把它**内联到调用点 `:636`** —— 理由：那会再动一处调用点，且会丢掉「**名字+层+order 三元组的唯一落点**」这个好处；处理方式与 `BattleDoors.PairCanvas` 一致（**保留方法体**）。若主对话要「彻底不留转发壳」，**一句话我就内联**。

---

## 可直接粘进总表的一行（≤3 行）

```
[FD] 清幂等包装 + 过期注释 ✅ —— F10【实为 5 处】全清（`BattleDoors.PairCanvas` / `BattleLogPanel.ReuseOrMakeCanvas` / `CardChoicePanel.Build` / `SettingsPanel.Create` ＋ 它的 const doc），一律改成直调 `HudCanvas.EnsureSubCanvas`；各自的 `Ensure(LayoutSpace.Cam)` 与 `cvRt==null` guard【保留】（W8/W43 必需项 ＋ 各扇「不建这一件」那一半）。
另办：W74 表头补「跨层由 (层,order) 定、本表只管同层先后」· G1-5 新增 `HudCanvas.IsRanked`（签名 = G1 建议的那个；**接线那半条归 G1**）· G1-3 `VoiceLines` 消费点订正 · G1-4 `UnitChatPanel:29` 错话+错行号（§三·〇 `:127` → 真落点 `:112`）· G5 那两笔（BattleDoors 中心锚特化形改走 `SetAnchorPx` · `HudCanvas:275` 那段 doc 改过去时 ＋ `:477` 半句）。
改 7 文件 · 净 +77 行 · 行尾逐文件二进制核过【没翻】（BattleDoors / BattleLogPanel 纯 CRLF）· typecheck 0/0 ×3 · 未跑 Unity · 未动 git。⛔ 只剩 `MultiCardDisplay:204-207`（归 FB）与 `BattleDriver:14674-14686` 接线（归 G1）。
```
