# EW5 · 画布档字号档统一为 7.56（`T09` 收尾）

施工日 2026-10-16 · 白名单 = `Unity/MyGame/Assets/CardPresentation/Battle/Label.cs`（改）+ 本报告（新建）
⛔ 未跑 Unity · 未动 git · 未改白名单外任何文件 · `TMPDIR=/tmp/wf_ew5 bash d:/4/Unity/工具/typecheck.sh` = **运行时错误数 0 / 编辑器错误数 0**

---

## ① 结论

### 改了什么（一句话）
`Label.cs` 里**画布档**那一侧的「一个 `scale` 档 = 多少 px」常量 `CapPxOfScale` 从 **`7f`** 改成
**`CapHeightWorldOfScale × PxPerWorld` = 7.559999942779541（= 7.56 设计 px/档）**；
并把 `CapUnitOfScale` 的**世界档那一支**从「`CapPxOfScale ÷ PixelsPerUnit`」改成**直接用世界档常量 `CapHeightWorldOfScale`**。

> 🔴 **后半句是本件唯一的坑**：只改常量不改那一支的话，`CapPxOfScale / PixelsPerUnit` = 7.56/100 = **0.0756**，
> **世界档会跟着一起涨 8%**（红线：世界档一字不动）。现在的写法让世界支取到的仍是 `7f/100f` = **同一个 float**（逐位不变）。

**世界档：零改动** —— `CapHeightWorldOfScale = 7f / PixelsPerUnit` 那一行**一个字没动**（`Label.cs:307`）。

### 裁定与理由（主对话 2026-10-16 定 `(B)`，本件只是执行）
`T09` 反复要求「**16:9 逐位不变**」⇒ 两档在 16:9 下必须**渲出同样大**：

| | 一个 `scale` 档 | 16:9 下渲出 |
|---|---|---|
| 世界档 | `CapHeightWorldOfScale` = 7/100 = **0.07 世界单位** | `0.07 × PxPerWorld(108)` = **7.56 屏 px** |
| 画布档（改前） | `CapPxOfScale` = **7** 设计 px | 7.00 设计 px ⇒ **偏小 7.4074%** |
| 画布档（改后） | **7.56** 设计 px | 7.56 设计 px ⇒ **与上表逐位相同** |

比例：新/旧 = **1.07999999（+8.0000%）**；旧值相对正确值小 **7.4074%**（`(7.56−7)/7.56`）。
⚠️ 「+8%」和「小 7.4%」**不是矛盾**，分母不同（前者以旧值为基、后者以正确值为基）——
`EW4` 报告 `:229` 与 `EW1` 报告 `:222-223` 用的都是后者。

### 影响面 · 🔴 **受影响的：14 个建字点 / 约 25 个 Label 实例 / 7 个文件**

**判据 = 画布档（`Label.CreateUgui` / `HudCanvas.MakeText`）＋ 裸 `scale` 定字号**：既没调 `SetAutoFitBox`，
也没调 `SetCapHeight` / `SetGlyphHeight` / `SetScriptHeight`（含 `ApplyLangFont` / `SetScriptFont` 那两条转发）。
这类件**只有** `CapWorld` 一条路，因而直接吃 `CapUnitOfScale` ⇒ **字号与渲出尺寸 ×1.08**。

| # | 件（`Label` 名 / 变量） | 处 | 档 | 大写高（设计 px）旧 → 新 | TMP 字号 旧 → 新 | 备注 |
|---|---|---|---|---|---|---|
| 1 | `_turnLabel`（`TurnLabel`） | `Battle/BattleDriver.cs:13959` | 4 | 28.00 → **30.24** | 41.00 → 44.28 | **我们自加**（原版无此件） |
| 2 | `_clockLabel`（`TurnClock`） | `Battle/BattleDriver.cs:14331` | 2 | 14.00 → **15.12** | 20.50 → 22.14 | **我们自加** |
| 3 | `_resultLabel`（`ResultLabel`） | `Battle/BattleDriver.cs:14542` | 7 | 49.00 → **52.92** | 71.74 → 77.48 | **我们自加** |
| 4 | `_title` | `Battle/AttackSelector.cs:587` | 3 | 21.00 → **22.68** | 30.75 → 33.21 | 该件**还有一条世界档支**（`:642`）⇒ 改后两档才真的一样大 |
| 5 | `_note` | `Battle/AttackSelector.cs:590` | 2 | 14.00 → **15.12** | 20.50 → 22.14 | 同上（世界支 `:646`） |
| 6 | `_captions[i]`（array，3 条） | `Battle/AttackSelector.cs:618` | 4 | 28.00 → **30.24** | 41.00 → 44.28 | 同上（世界支 `:664`） |
| 7 | `_rowTexts[i]`（array，`RowCount` = 10 条） | `Battle/BattleLogPanel.cs:726` | 3 | 21.00 → **22.68** | 30.75 → 33.21 | 该文件**没有任何字号调用** |
| 8 | `_hint`（`cdw_hint`「再点一下关闭」） | `Battle/CardDisplayWindow.cs:462` | 2 | 14.00 → **15.12** | 20.50 → 22.14 | 走同文件 `Text(...)` 助手，`fontPx=0` + `boxW=0` ⇒ 落回裸档 |
| 9 | `_title`（`end_title`） | `Battle/EndPanel.cs:270` | 6 | 42.00 → **45.36** | 61.49 → 66.41 | 该件注释自己写着「两档画出来一样大」（见 ③·3） |
| 10 | `_sub`（`end_sub`） | `Battle/EndPanel.cs:272` | 2 | 14.00 → **15.12** | 20.50 → 22.14 | 同上 |
| 11 | `_hint`（`mcd_hint`） | `Battle/MultiCardDisplay.cs:441` | 2 | 14.00 → **15.12** | 20.50 → 22.14 | 同文件另两颗（`_header`/`_barText`）走 `SetScriptHeight` = **不受影响** |
| 12 | `_title`（`settings_title`） | `Battle/SettingsPanel.cs:776` | 4 | 28.00 → **30.24** | 41.00 → 44.28 | 该面板别的标签多走 `ApplyLangFont` |
| 13 | `_diffLabel`（`settings_diff_label`） | `Battle/SettingsPanel.cs:889` | 4 | 28.00 → **30.24** | 41.00 → 44.28 | 同上（**没有** `ApplyLangFont` 调用） |
| 14 | `_diffValue`（`settings_diff_value`） | `Battle/SettingsPanel.cs:893` | 4 | 28.00 → **30.24** | 41.00 → 44.28 | 同上 |

> 口径说明：上表「大写高」= `CapPxOfScale × scale`（画布档 1 单位 = 1 设计 px，16:9 下 = 1 屏 px）。
> 「TMP 字号」= `大写高 ÷ TmpFont.CapPerFontSize ÷ 10`（`Wcap ≈ 0.0683` 是**运行时实测缓存**的文档值，
> 本会话没跑 Unity ⇒ 这一列**是文档值算的近似**；**但 ×1.08 这个比例与它无关、是精确的**）。
> 汉字会比它高 `Wglyph/Wcap ≈ 1.388` 倍（例：档 3 ⇒ 29.15 → 31.48 px）。

**⚠️ 与 `EW4` 报告的差异（如实记）**：`EW4` 报告 `:238, :272` 说这一改会动「**几十处**」——
那是**估的、没有普查**。本件现核（`grep` 全部 47 个 uGUI 建字点 + 逐个查同一变量后续的
`SetAutoFitBox` / `SetCapHeight` / `SetGlyphHeight` / `SetScriptHeight` / `ApplyLangFont` / `SetScriptFont`）
= **14 个建字点、约 25 个实例**。**别按「几十处」去复跑**。

### 影响面 · 🔴 **不受影响的（三类，都是结构性理由，不是「大概」）**

**A. 开了 `SetAutoFitBox` 的 —— 逐位不变**
`fontSizeMin = cur × minPx/nomPx`、`fontSizeMax = cur × maxPx/nomPx`，而
`cur = TmpFontSize() ∝ CapWorld`、`nomPx = FontSizeToPxNow(cur) ∝ CapWorld`
（`Label.cs:1177-1190` 与 `:809`）⇒ **商与 `CapWorld` 无关**、写入 TMP 的上下限**一个 bit 不变**，
自适应收敛到同一个字号（`fontSize` 的 setter **不对 min/max 做钳制** —— 现读
`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Text.cs:464-468`，无 clamp）。
覆盖：`BattleDriver.Hud()` 那 21 颗里**有框的 18 颗** · `MulliganPanel` 三颗 · `SettingsPanel` 那 5 颗
（`_muteOffText`/`_muteOnText`/`_muteLabel`/滑块标签 `/` 语言行标签）· `UnitChatPanel` 气泡 ·
`CardDisplayWindow` 的 `_fxTitle`/`_fxWho`/`_fxWhat`/`_lore` · `CardDisplayWindow.Text` 里 `boxW>0` 那一支 ·
`AlliancePanelWindow:691` 那族 · 其余走 `MenuDraw` 的**世界档**面板（与本次改动无关）。

**B. 显式定了字号的 / 走语种判据的 —— 结构性无关**
`CapWorld` 是 `_capHeight > 0 ? _capHeight : CapUnitOfScale × scale`（`Label.cs:339`）⇒
只要调过 `SetCapHeight` / `SetGlyphHeight`（`SetScriptHeight`、`ApplyLangFont`、`SetScriptFont` 都是它的转发），
`CapUnitOfScale` **根本不参与**。覆盖：`BattleDriver._overtimeSplashText`（`SetScriptHeight`）·
`_titleMe`/`_titleFoe`（`SetGlyphHeight`）· `_handLabel`（后补框）· `CardChoicePanel` 三颗 ·
`ChatPopupPanel` 按钮字（两支都写 `SetGlyphHeight(30f)`）· `MultiCardDisplay._header`/`_barText` ·
`SkillPanel` · `UnitChatPanel` · `Tooltip`（`SetCapHeight(BodyCap)`）· `AlliancePanelWindow` ·
`ErrorMessageBanner` 两条（`SetScriptHeight`）· `TutorialOverlay`（`SetCapHeight(scale × 7.56)`，**它本来就是 7.56**）。

**C. 全仓世界档 —— 一个字不动**
世界支取的是 `CapHeightWorldOfScale`（`7f/100f`，**与改前的 `CapPxOfScale / PixelsPerUnit` 是同一个 float**）。
（现数：`Label.Create(` 调用点 **33 处**（不含 `Editor/`）· **61 处**（含 `Editor/`），另有走 `MenuDraw.Text` 那一族 ——
它们全部落在世界档 ⇒ 与本次改动无关。）
点阵后端同理（`SetSizes` 的取整也走 `CapUnitOfScale`，但画布档**不会**退化点阵 —— `CreateCore` 那一支出声返回 null）。

---

## ② 改动清单

**文件：`Unity/MyGame/Assets/CardPresentation/Battle/Label.cs`（唯一改动文件）**

| 处（改后行号） | 改前 | 改后 |
|---|---|---|
| `:301-307`（`CapHeightWorldOfScale` 的 doc + 常量） | doc「点阵那条的字号：7×scale 像素高」· 行尾注「每个 scale 档 = 0.07 世界单位」 | doc 改成「0.07 世界单位 = **7.56 屏 px**」+ 加一条「⛔ 别改写成 `CapPxOfScale / PxPerWorld`（浮点上不等：0.070000000298023 vs 0.06999999950）」；**常量本身逐字未动** |
| **`:309-322`**（`CapPxOfScale`） | `const float CapPxOfScale = 7f;` | **`public static readonly float CapPxOfScale = CapHeightWorldOfScale * PxPerWorld;`**（= 7.559999942779541）+ doc 重写（裁定、推导、`0x40F1EB85` 三条写法同位、`T09` §五·补⑨ 四 那条） |
| **`:325-329`**（`CapUnitOfScale`） | `_ugui ? CapPxOfScale : CapPxOfScale / PixelsPerUnit` | **`_ugui ? CapPxOfScale : CapHeightWorldOfScale`**（世界支不变值、不再复用画布档常量）+ doc |
| `:1138`（`SetSizes` 里的行内注释） | 「按模式取 0.07 世界单位 / **7 px**」 | 「… / **7.56 设计 px**」 |
| `:1218-1221`（`FontSizeFor` 的 doc） | 「uGUI 档的字号要按 `CapPxOfScale`(**7 px**) 算」 | 「`CapPxOfScale`（**7.56 设计 px / 档**，2026-10-16 起；旧注释里那个 7 已作废）」 |

**为什么把 `CapPxOfScale` 从 `const` 升成 `public static readonly`**：
① 让「7.56」这个数**只由世界档真值推导一次**（三种写法 `0.07f × 108f` · `7.56f` · `7f/100f × 108f`
的**浮点位完全相同**：`0x40F1EB85` = 7.559999942779541，本会话用 `struct` 现算核过三条）；
② `public` 是为了让 `TutorialOverlay.CapPxPerScale` 与将来的自检**引用它**（见 ③·2）。
全仓引用点只有本文件一处（`grep -rn "CapPxOfScale"` 的另一处是 `TutorialOverlay.cs:209` 的**注释**）。

### ⚠️ 行尾核对（`git diff --numstat`）
```
114     30      Unity/MyGame/Assets/CardPresentation/Battle/Label.cs     ← 对 HEAD（含 EW4 的未提交改动）
```
**本波自身 = +35 / −18 行（3 个 hunk）**，文件总行数 1961 → **1978**（+17，与 35−18 相符）。
行尾二进制度回读：**`\r\n` = 1978 · `\n` = 1978 · 裸 LF = 0** ⇒ **纯 CRLF、没被翻**（改前也是 1961/1961）。

---

## ③ 接线清单（白名单外，主对话改）

| # | 文件:行 | 现在写的 | 该改成 |
|---|---|---|---|
| **1 🔴 必改** | `资料/任务线/战斗线/任务/T09_整层换uGUI.md:499` | 「② `CapHeightWorldOfScale`（`Label.cs:207` = `7f/100f` 世界单位/档 → uGUI 档是 **7 px/档**）」 | 「② `CapHeightWorldOfScale`（`Label.cs:307` = `7f/100f` 世界单位/档 → uGUI 档是 **7.56 设计 px/档**，= `0.07 × 108`；判据 → `Label.CapPxOfScale` 的 doc）」 —— ⚠️ 同一行的 `Label.cs:207` 行号**早就过期**（现 `:307`；同段 `:1412`/`:1089` 也过期：现 `PxPerWorld` 在 `:1749` 附近、`UsableBounds` 走 `MeasureBoundLimit`） |
| **2** | `Battle/TutorialOverlay.cs:215` | `const float CapPxPerScale = 7f / 100f * Px;` | `static readonly float CapPxPerScale = Label.CapPxOfScale;`（**值逐位相同**，已现算核过）。⚠️ 连带要改它上面 `:208-214` 那条 doc —— 它现在写着「**为什么不是 `Label.CapPxOfScale` 那个 7**」，**那个前提已作废**（`Label` 那侧现在也是 7.56）。⚠️ 改 `const`→`static readonly` 前先确认没有 const 上下文（`:215` 只被 `:574` 用） |
| 3 | `Battle/EndPanel.cs:265-267` | 「· **字号档一个没动**（`Label` 那三处「带单位的常数」内部按模式取 · uGUI 档 = **7 px/档**）⇒ 6 档在两种空间下**画出来一样大**（16:9 下逐位相同）。」 | 「uGUI 档 = **7.56 设计 px/档**（= 世界档 0.07 × 108）」—— **这件的结论句在改完之后才第一次成立** |
| 4 | `Battle/CardDisplayWindow.cs:457` | 「`scale: 2` 照旧 —— 两个空间里「一个 scale 档」都是 **7 px 的大写高**（`Label.CapUnitOfScale`）」 | 「…世界档 0.07 世界单位 · 画布档 **7.56** 设计 px（16:9 下都是 7.56 屏 px）」 |
| 5 | `Battle/MultiCardDisplay.cs:101` | 「档位换算：点阵/TMP 每档 = **7 px 大写字高**…`27.4 / 7 ≈ 3.9` ⇒ 最近整数档 = **4**」 | 算式里的 7 改成 **7.56**（`27.4 / 7.56 ≈ 3.62` ⇒ **仍取 4**，结论不变）。⚠️ 这两颗字其实走 `SetScriptHeight` ⇒ **无行为影响**，纯文档 |

> 另：`EW4` 报告 `:229` 与 `:236-239` 把本条列为「待裁偏差」，**主对话已裁 `(B)`、本件已落** ⇒
> 那一节应就地改成「已统一为 7.56（`EW5` 报告）」。

---

## ④ 判据与出处 + 16:9「两档渲出一样大」的逐处复算

### 判据（三条，都可现读）
1. **世界档一档 = 0.07 世界单位**：`Label.cs:307` `const float CapHeightWorldOfScale = 7f / PixelsPerUnit`
   （`PixelsPerUnit = 100f`，`Label.cs:42`）。
2. **1 世界单位 = 108 设计 px**：`Label.cs:1613` `static float PxPerWorld { get { return LayoutSpace.DesignPxH / LayoutSpace.DesignHeight; } }`，
   `LayoutSpace.cs:211` `DesignPxW = 1920f, DesignPxH = 1080f` · `:36` `DesignHeight = 10f`
   ⇒ **1080f/10f = 108f（精确）**（本会话现算，无舍入）。
3. **画布档 1 单位 = 1 设计 px**：`Label.cs` `PxPerUnit`（uGUI 档 = 1）· `T09` §五·补③ 一。

### 复算（本件自证）
- **画布档渲出的大写高（设计 px）**：`CapPxOfScale × s = (0.07 × 108) × s = 7.559999942779541 × s`
  ⇒ 档 1/2/3/4/6/7 分别 = **7.56 / 15.12 / 22.68 / 30.24 / 45.36 / 52.92 px**。
- **世界档渲出的大写高（屏 px）**：`0.07s` 世界单位 × `PxPerWorld`(108) = 同上**同一串数**。
- **两边的** *计算路径* 也确实同源：世界档 TMP 字号 = `CapWorld / Wcap`（`Label.cs:1189`，`div = 1`），
  画布档 = `CapWorld / Wcap / 10`（`div = UguiFontScale = 10`，同处）——`×10` 那一步抵消的正是
  `TMP_Text` 的 `m_isOrthographic` 差（`TMP_Text.cs:7183` × `TextMeshProUGUI.cs:802`，`EW4` 已修）。
  ⇒ 两边写进去的**目标大写高**都是 `CapWorld`（分别是 0.07s 世界单位 / 7.56s 设计 px），**值相等**。

**逐处复算（14 处，16:9）**：每一处都满足
`画布档渲出 px = 7.559999942779541 × s` **==** `世界档同档渲出 px = 0.07 × s × 108`
（两边到 float 精度内相同；差 ≤ 1e-6 px，来自 `0.07f` 本身的舍入）。
**改前**的差 = `(7.56 − 7)/7.56` = **7.4074%**（系统性偏小，且 **16:9 下看不出来**）。

---

## ⑤ 没查清 / 没做的（⛔ 不猜）

1. 🔴 **没跑 Unity**（红线）⇒ 上面 14 处的「+8% 会不会踩到既有断言」**没有实测**。
   能做的是**静态核**：`Editor/BattleScene.cs:14429` 那一组 `hudNoBox = { "TurnLabel","TurnClock","ResultLabel","HandLabel" }`
   只断「没自适应 / 没折行 / min-max 与同路对照逐位相等」——**不断字号** ⇒ 那 4 条**不会因本改动变红**；
   但它也**挡不住**本改动（= 这三颗的字号**现无任何断言覆盖**，见 ⑥·4）。
   其余 11 处我**没有**逐条把可能覆盖它的断言走完（`grep` 了 `DumpSizes` / `FontSizeFor(` / `CapHeightWorld` /
   各件名，未命中明显会红的；`BattleScene.cs:15782` 那类 `WorldW <= …` 是**上界**、且覆盖的是**有框件**，不受影响）。
2. **`TmpFont.WorldCapPerFontSize` / `WorldGlyphPerFontSize` 是运行时实测缓存**（文档值 ≈0.0683 / 0.0948）——
   本会话没跑 Unity ⇒ ① 表里「TMP 字号」两列是**文档值算的近似**（相对比例 ×1.08 与它们无关，是精确的）。
   ⚠️ 若真机复跑发现某处字号**卡到 TMP 的字号上下限**，那是另一条独立问题（我现读 `TMP_Text.cs:464-468`
   ⇒ `fontSize` 的 setter **不做钳制**，所以那条路**不成立**；只有开了自适应且 min 卡住才要担心）。
3. **没给这 14 处补断言**（`Editor/BattleScene.cs` 不在白名单）—— 建议见 ⑥·4。
4. **没有真机比画面**（红线：主对话本轮要求「别检查、先复刻」）⇒ 「渲出一样大」是**名义值推导 + 源码路径核对**，
   **不是实测像素比对**。

---

## ⑥ 顺手发现（只报不改）

1. **`Label.CapPxOfScale` 值改了、`TutorialOverlay.CapPxPerScale` 是第二个同值实现**（`:215`）——
   两条公式现在**逐位相等**（`0x40F1EB85`），所以**不是错的**，但是「同一条判据写两遍」⇒ 建议收口（③·2）。
2. 🔴 **`BattleDriver` 那三颗（`TurnLabel`/`TurnClock`/`ResultLabel`）的字号没有任何断言覆盖**：
   `Editor/BattleScene.cs:14429` 那一组只验「没框 / 没自适应」，`ResultLabel` 更是**原版没有的件**（`:14418`）。
   本改动让它们大 8%，**一条断言都不会红**。⇒ 建议补一条**结构上不可能两边一起变绿**的断言
   （灭自证型）：`Label` 上取「画布档裸档件的 `CapHeightWorld`」与「世界档同 `scale` 件的
   `CapHeightWorld × PxPerWorld`」相比，**差 ≤ 1e-6 px** —— 它同时挡「把 7 写回来」和「把世界档也改了」。
   （自检宿主 `Editor/BattleScene.cs`，**不在我的白名单**，故只报。）
3. **`Label.FontSizeFor(int)` 的名字会误导**：它是**世界档专属**（用 `CapHeightWorldOfScale`），
   却被 `Editor/TmpSetup.cs:255` 拿来打「HUD（scale 3）字号」。迁 uGUI 之后那行报的数**只对应世界档**。
   （低危，纯日志；判据 → `Label.cs:1218-1221` 的 doc。）
4. **`T09` §五·补⑨ 四 那张「带单位的常数」表**：本波现核，`Label.cs` 里**带单位的常数实际是 5 处**
   （表列的 3 处 + `UguiFontScale`（`EW4` 已就地补注）+ `MeasureBoundLimit`）。同一段 T09 的行号引用
   （`:207` / `:1412` / `:1089`）也**都已过期**。（③·1 里给了改法。）
5. **`EndPanel.cs:266` 那条注释的「结论」原来只在巧合下成立**：它写「字号档一个没动 ⇒ 两档画出来一样大」，
   而事实是**差 7.4%**——这类「算式里藏着一个数、结论句却写得像恒等式」的注释，是本批最容易被下一个会话
   当成判据照抄的东西。建议顺手扫一遍同族（③·3/4/5 三处就是扫描结果）。

---

## 可直接粘进总表的一行（≤3 行）

- **`EW5`（T09 收尾 · 画布档字号档统一）**：`Label.CapPxOfScale` **7f → `CapHeightWorldOfScale × PxPerWorld` = 7.56 设计 px/档**（裁定 `(B)`；世界档零改动 —— `CapUnitOfScale` 的世界支改成直接用世界档常量，否则会跟着涨 8%）· 影响面现核 = **14 个建字点 / 约 25 个 Label 实例 / 7 个文件**（`EW4` 报告里「几十处」是估的，别按它复跑）；开了 `SetAutoFitBox` 或显式定过字号的**结构性不受影响**。
- `typecheck` **0/0** · 未跑 Unity · 未动 git · 行尾纯 CRLF 未被翻 · **接线 5 条待主对话改**（首条 = `资料/任务线/战斗线/任务/T09_整层换uGUI.md:499` 那个「7 px/档」）。
- ⚠️ 本件**没补断言**（`Editor/BattleScene.cs` 在白名单外）⇒ 那三颗自加 HUD 标签的字号**目前无断言覆盖**，建议按 ⑥·2 补一条「两档大写高相等」的灭自证断言。
