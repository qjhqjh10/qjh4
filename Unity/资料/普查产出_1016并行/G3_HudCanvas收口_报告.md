# G3 · `HudCanvas.cs` + `Label.cs` 收口（收口波）

> 执行代理 `G3` · 2026-10-16 · 白名单 = `Battle/HudCanvas.cs` + `Battle/Label.cs` + 本报告。
> ⛔ 没跑 Unity（不跑 `*.Run`、不起 player）· ⛔ 没动 git · ⛔ 白名单外零改动。
> ✅ 类型检查 = `TMPDIR=/tmp/wf_g3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完跑过 3 遍，全 0/0）。
> 🔢 输入 = `资料/普查产出_1016并行/_收口台账_第四十二会话.md`（整份读过）。

---

## ① 结论

| 台账号 | 条目 | 做了没 | 说明 |
|---|---|---|---|
| **`W9`** | `SubCanvasRank` 的 `OverHand` 段插 `"ActiveSkillDesc Canvas"` | ✅ **做了** | 落在 **`HudCanvas.cs:549`**，位置 = **`Drag Attack Selector`(0) 之后、`Unit Chat`(2) 之前**（即 `Mulligan`(3) 之前 ✓） |
| **`W31`** | 同段**最前**插 `"Drag Attack Selector Canvas"` | ✅ **做了** | **`:548`**（本段第一条） |
| **`W44`** | 同段插 `"Unit Chat Canvas"`(2/14) + `"ChatPopup Canvas"`(4/7，紧跟其后) | ✅ **做了** | **`:550`** / **`:551`**（都在 `Mulligan Canvas` 之前 ✓）；顺带**订正 `:525` 那句已过期的注释**（见下） |
| **`W51`** | `"AboveShader Canvas"` **之后**补 `"Alliance Panel Canvas"`(13/14) | ✅ **做了** | **`:557`**；并**把「将来 7/9/11/12 要插在它【前面】」写进表旁注释**（`:558-560`） |
| **`W62`** | `Label.cs:156/162` 那句注释就地订正 | ✅ **做了**（**只改注释**） | 现读落在 **`Label.cs:156-177`**（原文 9 行 → 现 24 行；**行为一个字没动**） |
| **`W63`** | `HudCanvas.CenterPx` 的 doc 与实现不符 ⇒ 改 doc | ✅ **做了** | **`:333-346`**（doc 块），举了 `AlliancePanelWindow` 那个先例 |
| **`A12` 加口** | 在 `HudCanvas` 里加一对共用口（`SetAnchorPx` / `AnchorApPx`） | ✅ **做了**（**只加口，⛔ 没碰那三个面板文件**） | **`:251-292`**；doc 里写明谁应改用本口 + 一件另开的跨文件替换账（见 ③） |

**`W10`（`SubCanvasRank` 那段 doc 里的一句过期注释：原 `:464` ⇒ 改后 `:525`）** 也一并做了（它是 `W9/W44` 的附带项，台账里另编一号）。

🔴 **表里没有重名、没有漏项** —— 逐项对过 `grep -rn "SubCanvasName = " Battle/ Shell/`（10 个常量）＋
`grep -rn "EnsureSubCanvas("`（15 个调用点）；**今天所有 `OverHand`/order 0 的子画布，一个不落全在表里**
（`LoadingMenu Canvas` 是 `Blocking`/1000、`TooltipManager Canvas` 是 `Blocking`/0
⇒ **不同层，跨层先后由层定**，按 `HudCanvas` 自己那条 doc 本来就不该进这张表 ✓）。

---

## ② 改动清单

### `Unity/MyGame/Assets/CardPresentation/Battle/HudCanvas.cs`（**+90 / −9**）

| 行（改后现核） | 是什么 |
|---|---|
| `:213-216` | ⚠️ **本段抬头的补注**：那段标题写「面板族迁移的**四个**换算助手」，而收口波又在这段里补了一对 ⇒ 加一行写明「本段现在实际 **6 个**口；**抬头那个『四个』是 `T09` §五·补⑩ 那张配方表的口径、不是口数** ⇒ 改它要跟 `T09` 同批」。**纯补注、⛔ 没改那个数字**（我没权改 `资料/`） |
| `:251-264` | **新增 `AnchorApPx(xPx, yPxFromTop, ax, ay)`** —— 「枢轴点 → 相对点锚那个角的 `anchoredPosition`」（式子 `ap = 目标点 − 锚点`；doc 写明与 `PosPx` 的区别、与 `pivot` 无关） |
| `:267-292` | **新增 `SetAnchorPx(rt, ax, ay, xPx, yPxFromTop)`** —— 把一件摆成「点锚 + 枢轴点落在这个设计 px」；doc 写明**⛔ 必须写 `anchoredPosition`**、不管 `pivot`/`sizeDelta`、**为什么补齐这个口**（同式子在两个文件里写了两遍）、**谁应改用本口**、**本波不改那几个文件** |
| `:333-346` | **`CenterPx` 的 doc 就地订正（`W63`）**：原写「**中心的设计 px**」⇒ 改「**枢轴（`pivot`）那一点**」，并写明「**对非居中 pivot 的节点会读成枢轴坐标**」+ 先例 `AlliancePanelWindow`（读成 `(-1.04, +25.27)`、真中心 `(300.52, 179.80)`、偏 `(+301.6, +154.5)`）+ 出处 + 「要矩形中心再补 `(0.5 − pivot) × sizeDelta`（先例 `RootCenterLocal`）」 |
| `:520-534` | **`OverHand` 那段 doc 就地订正（`W10`）**：原写「⚠️ `SkillPanel`(1) / `Unit Chat`(2) **还没迁**，迁了要**插到前面**」⇒ 改「**已过期**，那两扇连同 (0)/(13) 都已迁完并按序落表」+ 列出**还没迁的四格 = `DebugButtons`(7) / `PopupAnchor`(9) / `Tutorial`(11) / `LoadingMenu`(12)**（要插在 `Alliance Panel` **前面**） |
| `:535-564` | **`SubCanvasRank` 表**（`static readonly string[]` 那一块的整体范围；条目行 = `:544-553`）：补 5 条（`Drag Attack Selector Canvas` / `ActiveSkillDesc Canvas` / `Unit Chat Canvas` / `ChatPopup Canvas` / `Alliance Panel Canvas`）+ 表头抄上**完整 14 子件序**（逐行序号有出处）+ 表尾两条注释（**「将来 7/9/11/12 插在 `Alliance Panel` 前面」** · **重名/漏项自查法 + 「对不上的项 `RootRect.Find` 永远 null、静默不排」**） |

### `Unity/MyGame/Assets/CardPresentation/Battle/Label.cs`（**+17 / −2**，**只改注释**）

| 行（改后现核） | 是什么 |
|---|---|
| `:156-168` | `CreateCore` 里「取『锚在父的中心 + 枢轴居中 + 零尺寸』⇒ `localPosition` **就是**相对父中心的偏移」那句 ⇒ **补前提「当父件的枢轴居中时」**（`W62`）；接一段 **2026-10-16 就地订正（铁律 5）**：写清原句错在哪（`localPosition` 原点 = **父件枢轴**、不是中心；`anchoredPosition` 才是相对**锚点**的）+ 先例 `AlliancePanelWindow` 偏 `(+301.566, −154.528)` px + 出处（`T09_AlliancePanel_锚点补正_报告.md` / `T09` §八·补 陷阱 1、3）+ **「⛔ 本波只改注释、不动行为」** |
| `:174-177` | `:162` 那句「锚点恒在中心 ⇒ `localPosition` 与 `anchoredPosition` 同值」⇒ **就地订正**：**只在父件枢轴居中时成立**（锚点 `(0.5,0.5)` 只保证**锚点**落父件中心，`localPosition` 原点仍是**父件枢轴**）。**下一行那句赋值本身一字未动** |

### ⚠️ `git diff --numstat` + 行尾核对（改完立刻对）

```
90      9       Unity/MyGame/Assets/CardPresentation/Battle/HudCanvas.cs
17      2       Unity/MyGame/Assets/CardPresentation/Battle/Label.cs
```

| 文件 | 改前 | 改后 | 判 |
|---|---|---|---|
| `HudCanvas.cs` | `CRLF=0 / LF=548` | `CRLF=0 / LF=629` | **纯 LF，没翻** ✓ |
| `Label.cs` | `CRLF=1894 / LF=1894`（= 全 CRLF） | `CRLF=1909 / LF=1909` | **纯 CRLF，没翻** ✓（新增 15 行 CRLF = 净 +15 行，与 diff `17/−2` 对得上） |

> ⚠️ **`Label.cs` 是 CRLF** ⇒ 那一处**没有用 Edit 工具**，走的是 python **二进制/`newline=''`** 那条路
> （先算进变量、`count(old) == 1` 才写，改完立刻数行尾）—— 按 CLAUDE.md「改已有文件先看它是什么行尾」那条办。
> 🧳 备份：`/tmp/Label.cs.bak`（改前原样）。

---

## ③ 接线清单（白名单外谁该改哪一行）

### ③·1 🔴 **新增一件（`A12` 那条的兑现）：三扇面板改用 `HudCanvas.SetAnchorPx` / `AnchorApPx`**（**另开**）

**为什么另开**：这是一次**跨文件机械替换**，不是本波的范围（我的白名单只有两个 `.cs`）。逐处现核如下 ——

| 文件:行（现核） | 现在是什么 | 换成什么 | 是「机械替换」吗 |
|---|---|---|---|
| `Battle/UnitChatPanel.cs:482-485` | `static Vector2 ApPx(float xPx, float yPxTop, float ax, float ay)`（**与共用口逐字同式**） | 删掉本函数、4 个调用点（`:465` / `:505` / `:525`）改调 `HudCanvas.AnchorApPx`；两处 `Move` 的 `_ugui` 分支（`:510-511` / `:529-530`）改调 `HudCanvas.SetAnchorPx` | ✅ **是**（`RefW/RefH = 1920/1080`，与 `HudCanvas.ReferenceResolution` **同值** ⇒ 逐位等价） |
| `Battle/ChatPopupPanel.cs:434-435` | `PanelApPx` 编译期常量（同式） | 该常量**不能**直接删（它是 `static readonly`，且被 `:311` 用）⇒ 可改写成 `HudCanvas.AnchorApPx(PanelOriginPx.x, PanelOriginPx.y, PanelAnchorX, PanelAnchorY)` | ✅ **是**（同值） |
| `Battle/ChatPopupPanel.cs:455-463` | `Place` 里**又内联了一遍**同式（`:457-458`） | 同上 | ✅ **是** |
| 🔴 `Shell/AlliancePanelWindow.cs:357-358` | `RootCenterLocal = ((0.5−pivot.x)·sd.x, (0.5−pivot.y)·sd.y)` | **⛔ 不是同一样东西** —— 那是「**枢轴 ≠ 中心**」的补偿，**不在 `SetAnchorPx` 的范围内**；且它的根锚是**字面常量**（`RootAnchorMin/Max` · `RootAnchoredPos`，逐值照抄原版 `RT_3052`），**没有重写这个算式** | ❌ **不是**（简报里「三处同形」这句要就地收窄成**两处**，见 ⑤·4） |

> 🔴 **简报的一处前提订正（现核）**：简报说「`UnitChatPanel`/`ChatPopupPanel` 两个文件各写了一遍同形算式」✓ 成立；
> 但它要 doc 里写「`…/…/AlliancePanelWindow` 应改用本口 ✕ —— **`AlliancePanelWindow` 没有这个算式**
> （`grep -n "1920f\|1080f\|RefW\|RefH" Shell/AlliancePanelWindow.cs` ⇒ **0 命中**，
> 它只有 `:357-358` 那个**另一种**补偿）。⇒ 我在代码 doc 里**按事实写**（只列前两扇 + 明说第三扇不属于这一档），
> 避免把一句查得到反例的话钉进源码。

### ③·2 🔴 **`"Drag Attack Selector Canvas"` 这个字面量现在有【两个抄写点】—— 必须同字**

- 台词本（我这张表）：`HudCanvas.cs:548` = `"Drag Attack Selector Canvas"`。
- 生产侧（`W30`，由 `G1` 在 `BattleDriver.BuildHud()` 里加）：
  `selector.UseHost(HudCanvas.EnsureSubCanvas(null, "Drag Attack Selector Canvas", OverHand, 0))`。
- ⚠️ **`AttackSelector.cs` 里【没有】`SubCanvasName` 常量**（现核：只有 `:388 public const string FrameName = "Drag Attack Selector"`，
  而 `FrameName` **不含 `" Canvas"` 后缀**）⇒ 那个字符串将**只存在于 `BattleDriver` 那一行**
  ⇒ 与我这表**两处各写一遍字面量**。
- ✅ **建议（两选一，主对话定）**：
  - **方案 1（推荐）**：给 `AttackSelector` 补 `public const string SubCanvasName = "Drag Attack Selector Canvas";`
    （同 `SkillPanel:291` / `UnitChatPanel:116` / `ChatPopupPanel:157` / `MulliganPanel:385` / `AlliancePanelWindow:132` 的现成样子），
    `BattleDriver` 引用它；**我这张表继续用字面量**（`SubCanvasRank` 是 `static readonly string[]`，
    写 `AttackSelector.SubCanvasName` 在 C# 上**合法**，但会让「表」与「常量」两处都能改、反而更难核 —— 收口表要的是**一眼可比**）。
  - **方案 2**：不加常量，`BattleDriver` 那一行**照抄本表这 27 个字符**（`"Drag Attack Selector Canvas"`，含两个空格），
    并在那行注释里指回 `HudCanvas.cs:548`。
  ⇒ 无论哪个方案：**两处的字符串必须逐字相同**，否则**静默不排**（`RootRect.Find` 返 `null`、无任何报错）。

### ③·3 本波涉及的**其余接线**（都来自台账，**不是我新增的**，列全以免漏）

| 台账 | 落点 | 一句话 |
|---|---|---|
| `W7` / `W55` | `BattleDriver.cs`（`BuildHud()`，现核 `:14434` 一带 / `:14430`） | `skillPanel.UseHost(HudCanvas.EnsureSubCanvas(null, "ActiveSkillDesc Canvas", OverHand, 0))` —— **不加这一行，`ActiveSkillDesc`(1) 那一格今天【永不建】**，我表里那一条**恒 `Find == null`、静默不排** |
| `W30` | `BattleDriver.BuildHud()` 收尾 | `selector.UseHost(HudCanvas.EnsureSubCanvas(null, "Drag Attack Selector Canvas", OverHand, 0))` —— 同上，不加则**我表里第一条永不生效** |
| `W42` | `BattleDriver.cs:14485` / `:15088` | 两扇（`Unit Chat` / `ChatPopup`）各补 `HudCanvas.Ensure(LayoutSpace.Cam);` + `Create(root, EnsureSubCanvas(null, <SubCanvasName>, OverHand, 0))` —— 不加则**我表里 (2/14) 与 `ChatPopup` 两条永不生效** |
| `W50` | `BattleDriver.cs:15084` | `AlliancePanelWindow.Create(root, "Alliance Panel", EnsureSubCanvas(null, SubCanvasName, OverHand, 0))` —— 不加则**我表里 (13/14) 那条永不生效**；⚠️ `cv == null` 时**别退回世界档** |
| `W8` / `W43` | `BattleDriver.cs:14430 / :14624 / :15193 / :15240 / :15487` | 五处 `EnsureSubCanvas` **之前都没有 `Ensure`**（靠 `BuildEnergyLights()` 兜、而它会提前 `return`）⇒ 半迁移态。**与我这张表的关系**：`Ensure` 缺失 ⇒ 子画布**建不出来** ⇒ 表里对应项静默不排 |

> 🔑 **一句话给主对话核**：**我这张表只负责「建出来的那几块谁在上面」**；
> **「建不建得出来」全在 ③·3 那五条线上** —— 表对了、线没接，画面照样不对（而且**不报错**）。

---

## ④ 判据与出处

### ④·1 改完的 `SubCanvasRank` **整张表原文**（`HudCanvas.cs:535-564`，逐字）

```csharp
        static readonly string[] SubCanvasRank =
        {
            // ── PopUps（order −1 → 0 → 1；同为 0 的两块按原版子件序）──
            "CemeteryLogPanel Canvas",          // PopUps / −1
            "BattleSettingsPanel Canvas",       // PopUps /  0（原版 FrontCanvas(3) 在前）
            "BattleDoors Canvas",               // PopUps /  0（原版 BattleDoors(4) 在后）
            "UI Error Message Controller Canvas",// PopUps / 1
            // ── OverHand（全 order 0，按原版 14 子件序升序）──
            //  ⚠️ **逐行的序号 = 原版 `Safe area FrontCanvas` 的 `m_Children` 下标**，完整 14 格是：
            //     `[0] Drag Attack Selector · [1] ActiveSkillDesc · [2] Unit Chat · [3] Mulligan ·
            //      [4] Generic Multi Card Display Combat · [5] ChooseCardMenu · [6] Card Display Window ·
            //      [7] DebugButtons · [8] AboveShader · [9] PopupAnchor · [10] BattleSettingsPanel ·
            //      [11] Tutorial · [12] LoadingMenu · [13] Alliance Panel`
            "Drag Attack Selector Canvas",       // 0/14（裁定「另起一块、⛔ 不并进 Mulligan」→ `T09` §八·3）
            "ActiveSkillDesc Canvas",            // 1/14
            "Unit Chat Canvas",                  // 2/14
            "ChatPopup Canvas",                  // 4/7（**在 `Unit Chat` 内部** ⇒ 按「子件画在父件之上」紧跟其后）
            "Mulligan Canvas",                   // 3/14
            "Generic Multi Card Display Combat Canvas",  // 4/14
            "ChooseCardMenu Canvas",             // 5/14
            "Card Display Window Canvas",        // 6/14
            "AboveShader Canvas",                // 8/14
            "Alliance Panel Canvas",             // 13/14
            // 🔴 **将来迁 `[7] DebugButtons` / `[9] PopupAnchor` / `[11] Tutorial` / `[12] LoadingMenu` 时，
            //    一律插在上一行 `"Alliance Panel Canvas"` 【前面】**（它们的原版子件序都 < 13）。
            //    ⛔ 别顺手加在表尾 —— 表尾 = 画得**最上面**的一格。
            //  ⚠️ 重名/漏项的自查：本表每一项都必须是**某扇面板 `SubCanvasName` 的字面量**（现核清单 →
            //     `grep -rn "SubCanvasName = " Battle/ Shell/`）；对不上的项 `RootRect.Find` 永远 null、
            //     **静默不排**（这正是本表最容易被写坏的方式）。
        };
```

**（给主对话核 `G1` 登记用）表里的 5 条新项 × 对应常量 —— 逐条现核字面量：**

| 序号 | 表里的字符串 | 常量在哪 | 值 |
|---|---|---|---|
| 0/14 | `Drag Attack Selector Canvas` | 🔴 **无常量**（`AttackSelector.cs:388` 只有 `FrameName`，且不含 `" Canvas"`） | 见 ③·2 |
| 1/14 | `ActiveSkillDesc Canvas` | `Battle/SkillPanel.cs:291` | `public const string SubCanvasName` ✓ 逐字相同 |
| 2/14 | `Unit Chat Canvas` | `Battle/UnitChatPanel.cs:116` | `public const string SubCanvasName` ✓ 逐字相同 |
| 4/7 | `ChatPopup Canvas` | `Battle/ChatPopupPanel.cs:157` | `public const string SubCanvasName` ✓ 逐字相同 |
| 13/14 | `Alliance Panel Canvas` | `Shell/AlliancePanelWindow.cs:132` | `public const string SubCanvasName` ✓ 逐字相同 |

### ④·2 每条的出处

| 结论 | 出处 |
|---|---|
| **原版 14 子件序**（`[0]`–`[13]` 那串） | `资料/任务线/战斗线/任务/T09_整层换uGUI.md:700-707`（§八 开头，「原版这一组的子件序（`Safe area FrontCanvas` 的 `m_Children`，`T09b` §A·3）」） |
| `[0] Drag Attack Selector` 必须**另起一块画布**且排**最前** | `T09_整层换uGUI.md` §八·3（**`:737-750`**，含两条就地订正痕迹：「并进 `Mulligan` 会被 (1)(2) 一起盖住」；原版那个节点**身上确实带 `Canvas` 组件** = `CV_2759`，`overrideSorting=false`）＋ `普查产出_1016并行/T09_AttackSelector_报告.md:122`（`"Drag Attack Selector Canvas", // 0/14`） |
| `[1] ActiveSkillDesc` 要插在 `[3] Mulligan` **之前** | `普查产出_1016并行/T09_SkillPanel_报告.md`（`OverHand`/0/兄弟序 **1/14**）＋ 台账 §2·2 `W9` |
| `[2] Unit Chat` + 它是 `ChatPopup` 的父件（`ChatPopup` = `Unit Chat` 的 `m_Children` **4/7**） | `普查产出_1016并行/T09_UnitChat_报告.md:105-110`（「`ChatPopup` 是 `Unit Chat` 的**子件**（`RT_3280.m_Children` 第 **4/7** 个）⇒ 按「子件画在父件之上」⇒ 紧跟 `Unit Chat` 之后」）＋ `:152-153`（两扇的宿主逐条） |
| `[13] Alliance Panel` 排在 `[8] AboveShader` **之后**、且将来 7/9/11/12 要插在它**前面** | `普查产出_1016并行/T09_AlliancePanel_报告.md:103`（`// 13/14（原版 Safe area FrontCanvas 的 m_Children[13]）`）＋ `:250`（「⚠️ 将来的 7/9/11/12 要插在它**前面**」）＋ `T09c_面板到原版宿主映射.md:39` |
| **`CenterPx` 读的是枢轴、不是中心** | `普查产出_1016并行/T09_AlliancePanel_锚点补正_报告.md:212-215`（`A14` ⑥·3，含 `(-1.04,+25.27)` vs `(300.52,179.80)`）＋ `T09_整层换uGUI.md:799-801`（§八·补 **陷阱 2**） |
| **`Label.cs` 那句只在父件枢轴居中时成立** | `T09_AlliancePanel_锚点补正_报告.md:195-200`（`A14` ⑥·1）＋ `T09_整层换uGUI.md:803-805`（§八·补 **陷阱 3**） |
| **`ApPx` 同式两处**（`UnitChatPanel` / `ChatPopupPanel`） | `UnitChatPanel.cs:482-485` · `ChatPopupPanel.cs:434-435` ＋ `:455-463`；两处 `RefW/RefH` 现核 = `1920f/1080f`（`UnitChatPanel.cs:79` · `ChatPopupPanel.cs:407`）= `HudCanvas.ReferenceResolution`（`HudCanvas.cs:55`） |
| **`AlliancePanelWindow` 里【没有】同式** | `grep -n "1920f\|1080f\|RefW\|RefH\|0.5f - " Shell/AlliancePanelWindow.cs` ⇒ 唯一命中 `:357-358`（`RootCenterLocal`，**另一种**算式） |
| 表里每一项都是**真子画布名** | `grep -rn "SubCanvasName = " Battle/ Shell/`（10 条）＋ `grep -rn "EnsureSubCanvas(" Battle/ Shell/`（15 处）；逐条比过 |
| **`LoadingMenu`/`TooltipManager` 不在本表** 且**是对的** | `BattleDriver.cs:14631-14632`（`LoadingMenu Canvas` = **`Blocking` / 1000**）· `:15200-15201`（`TooltipManager Canvas` = **`Blocking` / 0**）⇒ 不同层 ⇒ 按 `HudCanvas` 自己那条 doc「跨层先后不靠这个表」 |

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. ⛔ **没跑 Unity / 没起 player**（本波口径「只做不验」）⇒ 上表**只保证「表内容与判据一致」**，
   **不保证「画面上谁压谁对」** —— 那要等 ③·3 那五条接线落地 + 真机（`T09` §五 那套三档 + `wf_click.py`）。
2. ⛔ **`Label.cs` 的【行为】我没改**（也**没判**它该不该改）：
   - 现状：`CreateCore` 一律写 `go.transform.localPosition = pos`。
   - 只有**父件枢轴非居中**时它才与「相对父件中心」不等 —— 而**今天**所有调用点要么在世界档（父件枢轴居中）、
     要么挂在自己 `FillParent`/中心锚的容器下。
   - 🔴 **将来给「非居中 pivot 的父件」（如 `Alliance Panel` 那块根）挂 `Label` 时，这条路会静默偏**（先例量级 `(+301.6, −154.5)` px）。
   - **要不要现在就加「按父件 pivot 自动补一项」的口 = ⛔ 本波没做、也没判**（它是共享件、改行为要全量回归 ⇒ 归主对话裁）。
3. ⛔ **`CenterPx` 的【实现】没改**（只改 doc）—— 改成「真中心」会让**所有现存读点**在非居中 pivot 的件上突然换值
   （先例 `UnitChatPanel.BubbleCenterPx` 在用它，但那扇枢轴居中 ⇒ 不受影响）；改不改 = **另裁**。
4. 🔴 **简报的一处前提，现核不成立**（已按事实写进代码 doc）：
   简报说那对口的适用面是「`UnitChatPanel` / `ChatPopupPanel` / `AlliancePanelWindow` **三个文件**」——
   **第三扇不重复这个算式**（证据见 ④·2 末两行）⇒ 真正的机械替换账是 **2 个文件 / 4 处**
   （`UnitChatPanel.cs:482-485` + 3 个调用点；`ChatPopupPanel.cs:434-435` + `:455-463`）。
   ⚠️ **这不是「少做一件」** —— 换成 `SetAnchorPx` 对第三扇**本来就不成立**（它的锚是字面常量）。
5. ⛔ **没查**：`SubCanvasRank` 里那 4 条**已存在**的 `PopUps` 项（`CemeteryLogPanel` / `BattleSettingsPanel` /
   `BattleDoors` / `UI Error Message Controller`）我**只读没核** —— 它们的顺序判据在 `T09b` §A·3，
   **不在本次 6 条范围内**（`T09` §七·13 那条「`PopUps` 三块谁在先由兄弟序定」仍开着）。
6. ⛔ **没查**：`ApplySubCanvasRank` 的**赋值方式**是否够稳（它把命中的项一路摆到**子件列表末尾** ⇒
   未登记的画布（今天 = `LoadingMenu` / `TooltipManager`）会被挤到**本组最底下**）。
   ✅ **今天无害**（那两块都在别的层）；⚠️ **将来有 `OverHand` 画布不登记就另说** —— 记在这里。

---

## ⑥ 顺手发现（不属于本件，**只报不改**）

1. **`ApplySubCanvasRank` 是「只按名字找直接子件」**（`RootRect.Find`）——
   ⛔ **子画布一旦嵌进别的画布下，这条表就永远 `null`**。`ChatPopup` 在原版是 `Unit Chat` 的**子件**，
   我们把它落成**根 canvas 的直接子件**（`EnsureSubCanvas(null, …)`）**才**能进这张表 ——
   ⇒ **将来任何「照原版嵌进去」的子画布，都要么补一张层级表、要么保持平铺**。**记账**。
2. **`HudCanvas.Group`（z 账）与 `SubCanvasRank`（跨画布账）是两套模型**，都在本文件里：
   前者按**父节点**分组（`:417-428` 那一族），后者只认**根 canvas 的直接子件**。
   ⚠️ 一个件若**同时**被 `Add`（本地 z）与它所属画布被排进 `SubCanvasRank`，两套**互不干扰**（各自一维）✓
   —— 但**新写迁移件的人容易以为「排了 z 就够了」** ⇒ 建议在 `T09` 里已写的那条之外**再点一句**
   （我不改 `资料/`，只报）。
3. ✅ **本段抬头「四个换算助手」那句话已【补注】（不是改数字）**：`HudCanvas.cs:213-216` 加了一行 ——
   本段现在实际 **6 个**口（原有 `FillParent`/`SetPxSize`/`PosPx`/`QuadFromTopPx` ＋ 新加 `AnchorApPx`/`SetAnchorPx`），
   并写明**⛔ 那个「四个」是 `T09` §五·补⑩ 那张配方表的口径、不是口数** ⇒ 要改它得跟 `T09` 同批。
   ⚠️ **为什么没直接改数字**：`资料/**` 不在我白名单 ⇒ 单方面把代码里的数改掉、而 `T09` 那处还是「四个」，
   正是「**两处写同一条规则 = 迟早不一致**」。⇒ **留给正本维护者同批改**（要改的是 `T09` §五·补⑩ 与这里那一行，一起动）。
4. ⚠️ **本波 5 条插进表里的项，有 4 条的画布今天【建不出来】**（`ActiveSkillDesc` / `Drag Attack Selector` /
   `Unit Chat` / `ChatPopup`）—— 见 ③·3 的 `W7/W30/W42`。
   ⇒ **主对话若只核「表对不对」而不核「线接没接」，会得到一个全绿但画面不动的结论**（这是本件最要紧的一条提醒）。
5. ⚠️ `Battle/SettingsPanel.cs` 那扇的宿主是 **`PopUps`/0**（`:704`），而原版 `BattleSettingsPanel` 在
   **14 子件序里是 `[10]`**（`OverHand` 组）—— 两者**不是同一个地方**（原版那一格是 `OverHand` 的一份、
   `PopUps` 那份在根 `FrontCanvas` 里）。⚠️ **我没判谁对**（属 `T09`/`T09b` 的账）⇒ **只报**，⛔ 别当缺陷。

---

## 附：可直接粘进总表的一行（≤3 行）

`G3`（收口波）**`HudCanvas.SubCanvasRank` 补全 + `Label.cs`/`CenterPx` 两处注释订正已落地**：
`OverHand` 段按原版 14 子件序补 **5 条**（`Drag Attack Selector`(0) · `ActiveSkillDesc`(1) · `Unit Chat`(2) ·
`ChatPopup`(4/7，紧跟 `Unit Chat`) · `Alliance Panel`(13)，插在 `AboveShader` 之后；**7/9/11/12 将来要插在 `Alliance Panel` 前面**已写进表旁注释）；
`Label.cs:156/162` 与 `HudCanvas.CenterPx` 的 doc 均按「原点 = 父件**枢轴**」就地订正（**只改注释、行为未动**）；
另新增共用口 `HudCanvas.AnchorApPx` / `SetAnchorPx`（**⛔ 本波没改那三个面板文件** ⇒ 跨 2 文件 4 处的替换**另开一件**）。
`typecheck` **0/0**（跑 3 遍）· `HudCanvas` **+90/−9**（纯 LF）· `Label.cs` **+17/−2**（纯 CRLF）· ⛔ 没跑 Unity、没动 git、白名单外零改动。
🔴 **表对了但线没接**：那 5 条里有 4 条的画布**今天建不出来**（`W7`/`W30`/`W42`）⇒ 只核表会得到「全绿但画面不动」。
