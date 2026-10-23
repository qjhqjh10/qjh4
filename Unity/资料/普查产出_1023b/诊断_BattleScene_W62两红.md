# BattleScene W62 两条「前提」红 —— 只读诊断（2026-10-23）

> 诊断代理：只读。**没改任何源码 / 测试 / 台账，没跑 Unity，没动 git。**
> 日志：`d:/4/_tmp_view/t67_sync/battle.log`（`BattleScene.Run` 4479 通过 / 3 失败）
> 断言源码：`Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`
> W62 报告：`Unity/资料/普查产出_1023b/W62_十处自造元素_执行报告.md`

## 〇、一句话结论

**两条都是 α（断言写错），不是 δ，也不是 β。**
断言的「实例在」那一条**绿了** —— 窗口确实在、而且**建得完整**；
错的是**扫描对象**：两条都拿 `窗口.transform` 去扫字，而这两个窗口的
`transform` 是**世界空间那层空壳**，**一个字都不装**；真正的字在 `_panel`
（子画布下那个节点）。改成 `窗口.PanelRect` 即可，**不用补 `Open()`、不用改生产代码**。

🔴 **连带发现（比两条红更重要）**：③④ 与 ⑤ 的**断言本体**（`:34974` / `:34997`）
用的是**同一个错对象** ⇒ 它们现在**恒绿**（空扫）—— 把 `settings_title` / `AI Difficulty` /
`mcd_hint` 原样建回去，这两条**照样绿**。W62 想抓的回归，目前**一条也抓不到**。

---

## 一、③④ 设置面板「收得到字 / 节点」红

### 分类
**α —— 断言写错（查的对象不对）**。不是 δ，不是 β。

### 证据链

| # | 事实 | 出处 |
|---|---|---|
| 1 | 断言读 `driver.Settings`，再扫 **`spW62.transform`** | `Editor/BattleScene.cs:34955`、`:34959-34960` |
| 2 | `SettingsPanel` 组件挂在 **`"SettingsPanel"` 世界壳**上 | `Battle/SettingsPanel.cs:895`（`var p = go.AddComponent<SettingsPanel>();`，`go` = 名称 `"SettingsPanel"`） |
| 3 | 面板全部图/字挂在 **`p._panel`**（`"BattleSettingsPanel"`），它的父是 **`cvRt`（子画布）**，**不是** `transform` | `SettingsPanel.cs:920-926`（`pgo.transform.SetParent(cvRt, false)`） |
| 4 | `cvRt` 由 `EnsureSubCanvas(null, …)` 建，父 = **根 canvas `RootRect`** ⇒ 与壳是**兄弟** | `Battle/HudCanvas.cs:511-523`；调用点 `SettingsPanel.cs:913` |
| 5 | **生产代码自己就写着**「`transform` 那层壳 2026-10-16 块 6 之后**一个件都不装**」，并明确给出自检口 `PanelRect` | `SettingsPanel.cs:863-877`（原文：「🔴 命中判定凡是要『面板内 px』的，一律用**它**的局部空间（⛔ 不是 `transform` —— 那是**世界空间那层壳**…）」；`public RectTransform PanelRect` 在 `:877`） |
| 6 | 面板的每一颗字都在 `_panel` 上建（`_resignText` / `_skipText` / `_azLabel` / 语言行 / 静音行 / 三根滑块标签…） | `SettingsPanel.cs:1058`、`:1093`、`:1161`、`:1526`、`:1549`、`:2112-2129`、`:2246`；滑块 `WfSlider.Create(transform, …, uguiHost: _panel)` `:1226/1232/1238` |
| 7 | `Label.Text` 返回**已存的字符串**（与 `activeSelf` 无关）⇒ 窗口没开也能读到字 | `Battle/Label.cs:236`（`public string Text { get { return _text; } }`）；扫描用 `GetComponentsInChildren(…, true)`（`BattleScene.cs:34893/34898`） |

**日志原文（`battle.log:109196` / `:109209`）**
```
BT    ✓ （前提）W62③④：设置面板实例在
BT    ✗ （前提）W62③④：设置面板里收得到字 / 节点（字 0 段 / 节点 1 个）
```
**「节点 1 个」正是铁证**：`w62Names` 用的是 `GetComponentsInChildren<Transform>(true)`，
它**包含 root 自己**（`BattleScene.cs:34898`）⇒ 1 = **只有壳自己、零个子件**，
与 `SettingsPanel.cs:893` 那句「这一层**一个件都不装**（空壳）」逐字对上。

### 最小修法（二选一，推荐 A）

**A. 改扫 `PanelRect`（1 处 2 行，零生产改动）**
```csharp
// Editor/BattleScene.cs:34959-34960
var t = w62Texts(spW62.PanelRect);      // 原来： spW62.transform
var n = w62Names(spW62.PanelRect);      // 原来： spW62.transform
```
`PanelRect` 是**既有**自检口（`SettingsPanel.cs:877`，doc 里写明「自检要量面板帧…都要走这个口」），
`RectTransform` 可隐式转 `Transform` ⇒ `w62Texts/w62Names` 的签名不用动。

**B. 不建议**：扫子画布（`"BattleSettingsPanel Canvas"`）—— 那会把**整屏压暗 `_shade`**
（与 `_panel` 是兄弟，`SettingsPanel.cs:869` 的 doc）一起扫进来，范围比「面板」大。

### 修完的预期
* `t.Count` ≈ 10+（`Resign` / `Skip Tutorial` / `Auto Zoom` / 语言行 / 静音行 / 三根滑块标签…）
* `n.Count` ≈ 20+（`settings_bg` / `settings_close` / … / `BattleSettingsPanel` 自己）
* **不需要** `Open()` / `Show()` —— 见证据 7。

### 风险
* ③④ 的**本体**（`:34974`，禁词 `Settings` / `设置` / `AI Difficulty` / `Difficulty` / `settings_title*` / `settings_diff*`）
  改完会**第一次真的扫到字**。若它转红，先看越界串是不是**合法件**（例如某个语种名恰好含 `Difficulty`）——
  就本次现读，`_panel` 上的字是 `Resign` / `Skip Tutorial` / `Auto Zoom` / 语言名 / 静音三态，**都不含**这些串。
  ⚠️ 这条**必须先修**：不修的话本体那条是**空扫恒绿**（见 §三·1）。

---

## 二、⑤ 多卡窗「收得到字」红

### 分类
**α —— 断言写错（查的对象不对）**。不是 δ，不是 β。

### 证据链

| # | 事实 | 出处 |
|---|---|---|
| 1 | 断言读 `driver.MultiCards`，再扫 **`mcdW62.transform`** | `BattleScene.cs:34986`、`:34990` |
| 2 | `MultiCardDisplay` 组件挂在 **`"MultiCardDisplay"` 世界壳**上 | `Battle/MultiCardDisplay.cs:349-351`（`var w = go.AddComponent<MultiCardDisplay>();`，`go` 名称 `"MultiCardDisplay"`） |
| 3 | 画布本体 = **`_panel`**（`"Generic Multi Card Display Combat"`），父 = `cvRt`（子画布），**不是** `transform` | `MultiCardDisplay.cs:372-381`；`_panel` 的字段注释就写着「画布本体：标题 / 继续条 / 圆钮 / 两个字 / 提示」（`:269`） |
| 4 | 世界壳 `_root = transform` 上**只有一张整屏压暗** `mcd_mask`（无字） | `MultiCardDisplay.cs:396-406`（`_mask = ImageQuad.Create(_root, …)`） |
| 5 | 两颗字都建在 `_panel`：`_header`（出厂空串）与 `_barText`（`Loc.T(MulliganPanel.DoneTerm)`，**非空**） | `MultiCardDisplay.cs:422`、`:519-520` |
| 6 | 既有自检口 `PanelRect` | `MultiCardDisplay.cs:321`（`public RectTransform PanelRect { get { return _panel as RectTransform; } }`） |
| 7 | 窗口没开也有字：`Create` 里 `Build()` **先建全部件**，`Hide()` 只 `SetActive(false)`（`SetChrome(false)`），**不销毁** | `MultiCardDisplay.cs:383`（`w.Build(); w.Hide();`）、`:582-592`（`SetChrome` 全是 `SetActive`）、`:706-714`（`Hide`） |

**日志原文（`battle.log:109235` / `:109248`）**
```
BT    ✓ （前提）W62⑤：多卡展示窗实例在
BT    ✗ （前提）W62⑤：多卡窗里收得到字（实得 0 段）
```

### 最小修法
```csharp
// Editor/BattleScene.cs:34990-34991
var t = w62Texts(mcdW62.PanelRect);     // 原来： mcdW62.transform
var n = w62Names(mcdW62.PanelRect);     // 原来： mcdW62.transform
```
同样**零生产改动**、**不需要 `Show()`**（证据 7：`Hide` 不销毁件，`Label.Text` 读的是存值）。

### 修完的预期
* `t.Count ≥ 1`（`mcd_continue` = `Continue`/`继续`）—— `_header` 出厂是空串，被扫描器**跳过**（`:34894/34899` 的 `IsNullOrEmpty` 判断），不影响
* `n.Count ≥ 2`（`mcd_mask` 在壳上、`mcd_header`/`mcd_bar`/`mcd_continue`… 在 `_panel` 上）

### 风险
* ⑤ 的**本体**（`:34997`，禁词 `再点一下` / `点「继续」` / 节点 `mcd_hint`）同样会**第一次真的扫到字**。
  注意它查的是**整句** `点「继续」`（不是裸 `继续`）⇒ 原版那颗 `Continue` 按钮字**不会误伤**。
* 同 §一：不修这条，本体那条**恒绿**。

---

## 三、其余 W62 断言的「前提不成立 / 恒真」风险清单（只报，不改）

一次现读：W62 那一节实际有 **25 条 `Check`**（`BattleScene.cs:34883`–`:35177`，`grep -c` 现数），
不是 W62 报告 §二 写的「13 条」（报告大概按**断言组**数的）—— 文档口径可顺手订正。

| # | 断言 | 风险 | 证据 |
|---|---|---|---|
| 1 | **③④ 本体** `:34974` | 🔴 **恒绿（空扫）** —— 扫的是空壳，`settings_title*` / `settings_diff*` / `AI Difficulty` 全在 `_panel` 上 ⇒ **建回来也抓不到** | 同 §一 |
| 2 | **⑤ 本体** `:34997` | 🔴 **恒绿（空扫）** —— 同上，`mcd_hint` 会在 `_panel` 上 | 同 §二 |
| 3 | **⑥ 的 `TextForTest(4) != null`** `:35017` | 🔴 **同义反复（永远 null）**：`TextForTest` 的 `switch` **只有 0/1/2/3**，其余 `return null`（`CardDisplayWindow.cs:450-459`）⇒ `TextForTest(4)` **恒 null**，这一句**永远不会**往 `bad` 里加东西。W62 报告 §二 第 56 行「把那颗提示建回来（哪怕换节点名）⇒ `TextForTest(4)` 非 null ⇒ 红」**不成立** | `CardDisplayWindow.cs:450-459`；`BattleScene.cs:35017` |
| 4 | ⑥ 的**文字/节点扫描** `:35015-35016` | ✅ **有效**（这个窗的件确实在 `transform` 下：`_root = transform` `CardDisplayWindow.cs:543`，`_fxRoot = Node(_root, …)` `:683`，`TitleText` 非空 `:796`）⇒ ⑥ 靠这两句仍有意义 | — |
| 5 | ③④ 前提的 **`n.Count > 0`** `:34961` | 🟡 **恒真**：`w62Names` 含 root 自己 ⇒ 空壳也 ≥ 1。本次「节点 1 个」就是那个恒真的证明。改成 `PanelRect` 后建议加一个**真锚**（如 `n.Contains("settings_bg")`） | `BattleScene.cs:34898` |
| 6 | ② 敌方 `foeFac.Length == 0 \|\| …` `:34944` | 🟡 **条件性空转**：阵营名为空时**恒绿**。今天不空（`_foeFaction = DefaultFactionB`，`BattleDriver.cs:940`）⇒ 现在是有效的；但夹具哪天换掉就静默失效。建议补一条 `Check(foeFac.Length > 0, "（前提）…")` | `BattleDriver.cs:940`、`:26123` |
| 7 | ⑦⑧ 源码扫描 `:35172/:35177` | 🟡 只跳过**行首** `//`（`L.StartsWith("//")`，`:35153`）⇒ **块注释 `/* … */` 里的字面量会被当成代码命中**（假红）。另外只扫 `CardPresentation/`（`RuleEngine/**` 不在范围）。今天绿，是**假阴性/假阳性都可能的脆弱实现** | `BattleScene.cs:35152-35154` |
| 8 | ⑩ 注入后 `driver.ShowBattleLog()` `:35043` | 🟡 注入的事件**撤了**（`:35085` `RemoveInjectedLogEventsForTest`），但**日志面板被留在显示态**。因为紧接着就 `DestroyImmediate(driver)`（`:35131`），**无实际污染** —— 仅记一笔 | — |
| 9 | `w62Texts` 双计 | 🟡 `Label.Text` 与它子件的 `TMP_Text.text` **各收一次** ⇒ 段数虚高。只影响计数文案、不影响 pass/fail | `BattleScene.cs:34893-34896` |
| 10 | ② / ⑩ / ⑦⑩词条 / 灭自证 | ✅ 本次**真的执行了**（日志全绿且前提带实得值：名牌有字、`Play` 出真行、306 个 `.cs`、13 语言 0 条）⇒ 无前提风险 | `battle.log` 对应行 |

---

## 四、修复清单（给执行代理，改一处即可）

只动 `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`，**4 行**：

| 行 | 现在 | 改成 |
|---|---|---|
| `:34959` | `w62Texts(spW62.transform)` | `w62Texts(spW62.PanelRect)` |
| `:34960` | `w62Names(spW62.transform)` | `w62Names(spW62.PanelRect)` |
| `:34990` | `w62Texts(mcdW62.transform)` | `w62Texts(mcdW62.PanelRect)` |
| `:34991` | `w62Names(mcdW62.transform)` | `w62Names(mcdW62.PanelRect)` |

**⛔ 不需要**：补 `Open()` / `Show()`（窗口本来就已经建全；`Label.Text` 与 `activeSelf` 无关）。
**⛔ 不要**：把「前提」两条删掉了事 —— 它们现在是唯一能证明「下面那条不是空扫」的东西，
真正要修的是**扫描对象**（删了前提，本体仍然空扫）。

**可选加强（同期一起做更省一轮）**
* `:34918` 灭自证**已经是** `w62Texts`/`w62Names` 的正控，**它绿了** ⇒ 扫描器本身没坏，这一条**不用动**。
* `:34961` 的 `n.Count > 0` → 建议再加一句真锚 `Check(n.Contains("settings_bg"), …)`。
* `:35017` 的 `TextForTest(4)` 恒 null —— 建议删掉该子句并在注释里写明「`TextForTest` 只到 3」，
  或换成「扫 `_panel` 下的 `Label` 名字里没有 `cdw_hint`」（现已有同效的文字/节点扫描）。

---

## 五、没查清 / 如实记

* **③④ 改扫 `_panel` 之后 `t.Count` 的精确值**没跑过（只读、不跑 Unity）—— 按代码数
  `_panel` 上至少 9 颗非空字（`Resign` / `Skip Tutorial` / `Auto Zoom` / 语言名 ×2 / 静音 ×3 / 三根滑块标签），
  预期 `> 0` 稳过。
* **`WfSlider.Create(transform, …)` 的第一参**是否真被忽略没逐行读 `WfSlider.cs`
  —— 但**日志「节点 1 个」已经是实测**：壳上**零个子件**，与 `SettingsPanel.cs:893` 的注释一致，
  结论不受影响。
* W62 报告 §二 的「13 条」与实际 24 条 `Check` 的差：只核了条数，**没核报告那张表有没有漏列断言**。
