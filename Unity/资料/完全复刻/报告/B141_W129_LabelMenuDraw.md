# B141 · W129 · W9 包 —— `Label` + `MenuDraw` 的 TMP 排版公共口（4-11 / 4-12 / 4-13）

> 执行写手 `B141-W14` · 2026-11-04 · 第一百二十八会话 · 波次 `W129` · 批次 `B141`。
> 输入判据 = `判据/B141_W129_对账_UI动画.md` §4-11 / §4-12 / §4-13（`W9` 包）＋ `报告/B138_审计汇总_问题清单.md` §4。
> 白名单 = `Battle/Label.cs` · `Shell/MenuDraw.cs` · 新建 `Editor/B141_W14_Assert.cs` · 本报告。⛔ 未动账 / 活状态表 / 所有权表 / `d:/2` / git；未跑 Unity。
> 原版判据 = `d:/2/tools/assetripper_out/ExportedProject/Assets/**/*.prefab` **2026-11-04 现扫**（339 份带 TMP 的 prefab，逐字段脚本汇总，见 §一）。

---

## 〇 · 结论（一句话）

**段距（4-11）从「全仓无 API」变成「有唯一实现 + Shell 转发口」，默认值 = 0（不调用 ⇒ 与改前逐位相同）；4-13 在本包内接上了两颗真实站点（`Deck Name` → Truncate、`Banned Text` → Bold|UpperCase）；4-12 只验证了入口能表达原版那几档值，逐站点接线不在本包（无白名单窗口）。**

| 条目 | 改前 | 改后 | 本包是否闭环 |
|---|---|---|---|
| **4-11** `m_paragraphSpacing` | 全仓 0 处、**无 API** | `Label.SetParagraphSpacing` + `MenuDraw.SetParagraphSpacing`（默认 0） | ✅ 接口闭环；**业务站点 0**（见 §五·1） |
| **4-12** `m_margin` | 入口已有（`Label.SetMargin` / `SetMarginPx`），业务调用 **1 处**（`DeckRuntime.cs:2575` → `SetTmpMargin`） | **不变**（本包无可改站点） | ⚠️ 未闭环：**站点接线 + 第三入口收口都不在本包**（见 §五·2） |
| **4-13** 行距/字距/样式/Truncate | 行距 9 · 字距 90 · 样式 3 · **Truncate 0** | 行距 9 · 字距 90 · 样式 **4** · **Truncate 1** | ⚠️ 部分：本包内只有 2 颗真实站点；其余按窗派（见 §五·3） |

---

## 一 · 原版逐件真值（2026-11-04 现扫 339 份带 TMP 的 prefab）

> ⚠️ 口径：这是**组件实例数**（同族变体 prefab 各算一颗）。审计表里的「约 150 / 400 / 320 颗」是同一口径的旧数，本表是现数；**引用时用本表**。

### 1·1 `m_paragraphSpacing` 非 0（**160** 颗）

| 值 | 颗数 | 节点族 / 出处 |
|---|---|---|
| **8** | **132** | 卡面战术卡：`DescTextTactic` / `DescTextTactic Big` / `DescTacticSmall`（`2DCard` · `Card Drawer` · `CardPrefab` · `Booster Pack Open Window` · `Card Shop Tab` · `Card Alternate Art Drawer` …） |
| **60** | **10** | `DescriptionText`：`Base Event Popup` · `Base Expiration Popup` · `Event Help Popup{ Small Image,} Variant` · `Full Screen Event Announcement Variant` · `Rate Popup` · `Sunset Popup`(×2) · `Tutorial Message Window PopUp` · `WelcomeScreen Alpha` |
| **20** | 4 | toast 正文：`Toast.prefab` · `Mission Toast Notification` · `Toast Collection Get XP` · `Toast Collection Level Up` |
| **50** | 2 | `Purchase Premium Window / Description Content` · `Updated Tooltip Variant - ranked / Text (TMP)` |
| **25** | 2 | `Main Menu Settings Window / {Email Text, Email Text Mobile}` |
| **22** | 1 | `Liveop Event Help Variant / DescriptionText` |
| **10** | 1 | `Updated Tooltip Variant / Text (TMP)` |
| **0.08** | **8** | `Deck Editing Menu` / `Collection Menu Variant` 的 `…/Counter`（万能卡计数，4+4） |

### 1·2 `m_margin` 非 0（**445** 颗，前 8 族）

| 值 `(x,y,z,w)` | 颗数 | 节点族 |
|---|---|---|
| **(0, 0.0077514946, 0, −0.8828125)** | **299** | `Button Text` 全族（`Missions Tab` · `Rewards Base Submenu Variant` · `Social Submenu Variant` · `Main Menu Settings Window` · `MessagePopupWindow*` · `Expansion Pass *` …） |
| **(4.6, 0, 0, 0)** | 42 | `Новинка!`（`Booster Pack Open Window` · `* General Basic Offer *` · `Draft Mode *` · `Expansion Pass Tab` …） |
| **(18, 0, 18, 0)** | 28 | `Army`（`General Basic Offer Popup Variant Single Item Type` 等） |
| **(0, 0, 1.83, 0)** | 13 | 同族右缩进 |
| **(0, 0, 0, 0.31549266)** | 6 | 同族下缩进 |
| **(10.365997, 9.329266, 10.365906, 7.25613)** | 5 | `description text`（`Give Feedback Popup menu` · `Social Submenu Variant`） |
| **(22.06, 0, 8.67, 0)** | 2 | `Title`（`Collection Menu Variant / Army`） |
| **(0, −5, 0, −5)** | 4 | 负边距（外扩） |

### 1·3 `m_lineSpacing` 非 0（**335** 颗）

| 值 | 颗数 | 节点族 |
|---|---|---|
| **5** | **234** | `DescTextUnit*` / `DescTextTactic*` / `DescTacticSmall`（各 44）· `DescriptionText`(11) · `Lore Text` |
| **−8** | 44 | `NameTextUnit No Description Big` |
| **−5** | 28 | 卡面名族 |
| **15** | 10 | `Faq Text` · `MessageText`（`MessagePopupWindow*` / `ReRollPopup`）· `Message` · `Claim reward warning` |
| **50** | 8 | `Draft Mode *` 的 `Description 1` / `Game mode instructions` / `Help Text` / `SubTitle` |
| **10 / 20 / 174 / 14.5** | 6 / 2 / 2 / 1 | 零散 |

### 1·4 `m_overflowMode = 3 (Truncate)`（**45** 颗 / 31 组 `prefab×节点` / 9 个节点名族）

| 节点名 | 颗数 | 出处（去重） |
|---|---|---|
| `TabButtonLabel` | 14 | `Base Submenu` · `Collection Menu Variant`(×4) · `Menu Navigation Panel Button` · `Parent Event Window` · `Rewards Base Submenu Variant` · `Shop Menu Variant` · `Social Submenu Variant` … |
| `Time` | 6 | 商店页签（`Card Shop Tab` / `Shop Menu Variant` / `Daily Shop Tab` / `Item Shop Tab` …） |
| `Player Title` | 6 | `Player Profile Window` · `Profile Player Info` |
| `description text` | 5 | `Give Feedback Popup menu` · `Social Submenu Variant` |
| **`Deck Name`** | **5**（**6 组**） | **`Collection Deck` · `Collection Deck With Highlight` · `Deck Drawer` · `General Basic Offer {Container,Popup} Variant Deck_cardback_avatar`** ← **本包已接** |
| `RefreshText` | 4 | 商店页签 |
| `LevelLabel` / `PointsLabel` / `Pack title` | 4 | `Forge Menu Reward Button` · `Raid Reward Container` · `Prebuilt Pack UI` |

> 🔴 **铁律 5 更正**：审计口径写「Truncate **5 颗**」——**现扫是 45 颗**（31 组、9 个节点名族）。「5」大概是**按 `prefab` 去重后只数了某一族**；引用这条时用本表。
> ⚠️ `m_overflowMode = 1 (Ellipsis)` 另有 4 颗（`ChatPreview / text` ×2 · `Social Submenu Variant / text` ×2）—— 我们已接（`BuildChatPreview` 两行）。

### 1·5 `m_FontStyle` 非 `Normal`（**363** 颗）

| 值 | 颗数 | 节点族 |
|---|---|---|
| **1 Bold** | 159 | `text`(48) · `Title`(38) · `title`(14) · `Title (1)`(12) · `TraitCounter`(7) · `Warlord Name` · `Avatar Name` · `Ranking Points Quantity Text` … |
| **16 UpperCase** | 81 | `Army Text`(17) · `Army Text 2`(17) · `Title`(15) · `TabButtonLabel`(14) · `Window Title`(8) · `TitleText` · `Button Text` … |
| **17 Bold+UpperCase** | **47** | **`Banned Text`（唯一一族；47 份 prefab 全同值）** ← **本包已接** |
| **2 Italic** | 40 | — |
| **64 Strike** | 22 | — |
| **4 Underline** | 12 | — |
| **7**（Bold+Italic+Underline） | 2 | — |

---

## 二 · 改了什么（逐文件）

### 2·1 `Battle/Label.cs`（2883 → **2937** 行，+54；**类尾追加**）

| 新成员 | 说明 |
|---|---|
| `public void SetParagraphSpacing(float paragraphSpacing)` | **唯一实现**：`_tmp.paragraphSpacing = v` + `ForceRelayout()`（与 `MenuDraw.SetLineSpacing` 同一条收尾）；点阵后端 `NoteDotBackendLacks("段距", …)` 出声后返回。单位 = **字体单位**（原版值直接传）。 |
| `public float ParagraphSpacingNow` | 自检读口 = **TMP 真值**（点阵后端恒 0）。 |
| `public float ParagraphSpacingRequested` | 「我们请求过什么」那一侧的记账（只为灭自证能只写一边）。 |
| `public static bool DisableParagraphSpacingWriteForTest` | 🧨 灭自证开关：开 ⇒ 只记字段、不写 TMP（= 没有这个口之前的行为）。默认 false。 |
| `float _paragraphSpacing` | 上面那个读口的底层字段。 |

**默认值 = `TMP_Text.m_paragraphSpacing` 的出厂值 0** ⇒ **不调用就与改前逐位相同**（A 段断言守着这条）。
**零行号漂移**：`Label.cs` 的账落点最深 `:2696`，本笔全部在 `:2884` 之后。

### 2·2 `Shell/MenuDraw.cs`（3879 → **3901** 行，+22）

| 改动 | 行 | 说明 |
|---|---|---|
| `Deck Name` 接 **Truncate** | `:3467`（**同一物理行内**改，0 行差） | `nl.SetCharSpacing(-2f); nl.SetOverflow(TMPro.TextOverflowModes.Truncate);` — 原版 6 份 prefab 逐份 `m_overflowMode = 3` |
| `Banned Text` 接 **Bold\|UpperCase** | `:3885`（同一物理行内改，0 行差） | `txt.SetFontStyle(TMPro.FontStyles.Bold \| TMPro.FontStyles.UpperCase);` — 原版 47 份 prefab 全 = `m_FontStyle 17` |
| `public static void SetParagraphSpacing(Label lb, float paragraphSpacing)` | 类尾（新） | Shell 侧转发口，**只转发**（唯一实现在 `Label`） |

> ⚠️ **`Banned Text` 这一颗在生产上休眠**：`DeckBan.Apply` 的第一道闸是 `IsBanned(...)`，而 `IsBanned` 首句 `DeckRuntime.CurrentEventData == null ⇒ return false`（禁卡表在 LiveOps 远端）。⇒ 接线现在**不会**改任何画面；断言用 `B121_C3` 那套「临时注入 `PlayEventData`」夹具证明机制是通的（`finally` 还原）。

---

## 三 · 覆盖计数（改前 → 改后）

| 参数 | 改前业务调用 | 改后 | 备注 |
|---|---|---|---|
| `paragraphSpacing` | **0**（且无 API） | **1 个 API + 1 个转发口；业务站点 0** | 站点全在窗里（§五·1） |
| `m_margin` | **1**（`DeckRuntime.cs:2575`） | **1**（不变） | 本包无窗口可改 |
| `lineSpacing` | **5** 个真调用点（`ItemDisplayWindow:374` · `MissionRerollPopup:338` · `SettingsWindow:3155` · `TutorialMessagePopup:304` · `WelcomePopup:210`） | **5**（不变） | ⚠️ 对账里那个「9」= **`grep -c SetLineSpacing` 含定义行与 3 行注释**；真调用是 5（本表按真调用记） |
| `characterSpacing` | **34** 个真调用（18 个文件） | **34**（不变） | ⚠️ R8 报的「90」不是这个分母（全量 `grep -o SetCharSpacing` = 124 次，含文档/注释）⇒ **引用时按「真调用 34」** |
| `fontStyle` | **3**（`FilterPanelModel:990` · `BoosterPackOpenWindow:1742` · `CardView:3685`） | **4**（+`Banned Text`，休眠） | — |
| `Truncate` | **0** | **1**（`Deck Name`，生产路径真的会走） | 原版 45 颗 / 9 族，其余按窗派 |

---

## 四 · 账漂移（⚠️ 需调度台在同步点处理）

`MenuDraw.cs` 的新转发口只能加在**本类类尾**（`:3642` 之后前），而**本类后面还有 `MenuUniqueIds` / `DeckBan` 两个类**、且账里有 4 个落点指着它们 ⇒ 整体下移 **+22 行**：

| 账里原落点 | 现读 | 说明 |
|---|---|---|
| `Shell/MenuDraw.cs:3662` | **`:3684`** | `public static class MenuUniqueIds` |
| `Shell/MenuDraw.cs:3774` | **`:3796`** | `internal static class DeckBan` |
| `Shell/MenuDraw.cs:3803` | **`:3825`** | `DeckBan.IsBanned` |
| `Shell/MenuDraw.cs:3822` | **`:3844`** | `DeckBan.Apply` |

受影响的账行 = **两行**：`账/卡组编辑.tsv:41`（A 档，三处散文引用 `3774/3803/3822`）＋ `账/未分类_其他.tsv:312`（`GameObjectUniqueIdentifier.FindComponent`，**机器落点** `:3662`）。
**机检现状（本包已跑 `check_ledger_points.py` 现读）**：
- `未分类_其他.tsv:312` 报 **漂移**（「实际在 3684 行；账里写 3662」）——**不是失败**，但带 `NEED-HUMAN`：落点解析到的是 `public static class MenuUniqueIds` 这个**声明行**，`--fix` **会跳过** ⇒ **需要人工把 3662 改成 3684**。
- `卡组编辑.tsv:41` 的机器落点是 `MenuDraw.cs:3310`（插入点**之前**，不漂）；后面那三处散文引用机检不报，但为准确建议一并改成 **3796 / 3825 / 3844**。
`Label.cs` 与 `MenuDraw.cs` 的 `:2166`（`SetLineSpacing`）与 `:3467`（`Deck Name`）三处**行号未变**（两处编辑都在插入点之前 / 是同一物理行内改）。
⚠️ `Banned Text` 那一行**自身**也是同一物理行内改（0 行差），但它在插入点**之后**（`DeckBan` 类里）⇒ 由 `:3863` 变成 **`:3885`**（+22，随 §四 那张表一起漂）。

---

## 五 · 交调度台（本包做不了的）

### 5·1 4-11 段距：业务站点（判据已给，逐窗派）

| 站点 | 原版值 | 建议落点（施工时现读） |
|---|---|---|
| toast 正文 | **20** | `Shell/ToastNotifications.cs`（`Toast.prefab / Text`；任务 toast 同） |
| 事件/欢迎弹窗 `DescriptionText` | **60** | `Shell/LiveOpsEventWindow.cs` / `ExpansionPassEventWindow.cs` / 欢迎窗（`WelcomeScreen Alpha`） |
| 设置 `Email Text` | **25** | `Shell/SettingsWindow.cs`（与 `W1` 包同一文件） |
| 高订 tooltip `Description Content` | **50** | `Shell/ShopWindow.cs`（`W4` 包） |
| 活动帮助 `DescriptionText` | **22** | `Shell/LiveOpsEventWindow.cs` |
| 卡面战术卡 `DescTactic*` | **8** | 卡面走 `Core/CardView.cs:5054`（`unit ? 0 : DescParaSpacing`），**不是**本口 —— 已覆盖，不用动 |
| 万能卡 `Counter` | **0.08** | `Deck/DeckRuntime.cs:160-172`（与 `W17` 包同文件） |

调用形状：`MenuDraw.SetParagraphSpacing(lb, 20f);`（或拿得到 `Label` 时 `lb.SetParagraphSpacing(20f)`）。

### 5·2 4-12 边距：三个入口 + 站点

- 🔴 **第三入口（审计漏的那一个）**：`Deck/DeckRuntime.cs:10047 SetTmpMargin` → `:2575` 唯一业务调用（cosmo 军队标题 `margin 22.7`）。**本包白名单外** ⇒ 应由 `W17`（`DeckRuntime.cs` 所有者）**收口到 `Label.SetMargin`**（铁律 6：同一规则只留一处），再补站点。
- 站点族（原版值见 §1·2）：`Button Text` **299 颗**（`(0, 0.0077514946, 0, −0.8828125)`，横跨 Missions / Rewards / Social / Settings / 弹窗族）· `Новинка!` **42 颗** `(4.6,0,0,0)` · `Army` **28 颗** `(18,0,18,0)` · `description text` **5 颗** `(10.366, 9.329, 10.366, 7.256)`。
- ⚠️ 单位口径：本工程 `m_margin` 的换算沿用 `A1538` 那套（原版值 × 父级 `m_LocalScale` = 设计 px）—— 接线前**先读 `Label._margin` 那段 doc**，⛔ 别再造第二套。

### 5·3 4-13 其余站点

- `m_lineSpacing` **335 颗**：`SettingsWindow`（`Faq Text` 15）· `MessagePopup*` / `ReRollPopup`（`MessageText` 15）· `Inbox Menu`（`Message` / `Claim reward warning` 15）· `Draft Mode *`（50）—— 按窗派，口子够用（`MenuDraw.SetLineSpacing`）。
- `m_fontStyle` **363 颗**：`Bold` 集中在 `Title` / `text` / `Warlord Name` / `Avatar Name` …；`UpperCase` 集中在 `Army Text` / `Window Title` / `TabButtonLabel`。⛔ 别一次全仓刷，按窗派。
- `Truncate` 其余 8 族（`TabButtonLabel` 14 · `Time` 6 · `Player Title` 6 · `description text` 5 · `RefreshText` 4 · `LevelLabel` / `PointsLabel` / `Pack title` 4）：`TabButtonLabel` 那颗同时缺 `fontStyle 16`（本包外的 `MainMenuSubmenuWindow.cs` / `CollectionWindow.cs`）。

### 5·4 ❓ 未查清

- `Banned Text` 的 **`m_fontAsset` 是不是 Bold 字重资产**（若原版用的是 Bold 字体资产而不是 faux-bold，`fontStyle |= Bold` 会有轻微字形差）。本包只按**序列化字段** `m_FontStyle = 17` 落地（字段是硬判据）；字形差异要真机看图，⛔ 这里不下结论。
- 端距 `0.08` 那 8 颗：TMP 的 `paragraphSpacing` 是否有下限截断（`0.08` 字体单位 ≈ 0.08 em，视觉上几乎不可见）—— 本包只保证**值写进去了**（B3 断言），画面是否可分辨未测。

---

## 六 · 验证

### 6·1 秒级类型检查

```bash
cd /d/4/Unity && TMPDIR=/d/tmp/wf_b141_w14 bash 工具/typecheck.sh
```

- **首次跑（18:30）**：`运行时错误数: 4`，全部在 **`Assets/RuleEngine/Data/DeckLibrary.cs(825/841/854/867)`** —— `Debug` 未限定命名空间（那文件没有 `using UnityEngine;`），是**别人正在飞的改动**，不是本包文件（`⛔` 不改别人的文件）。编辑器程序集因此级联 `CS0006`（拿不到运行时 DLL）。
- **复跑结果见 §6·3**。

### 6·2 断言 `Editor/B141_W14_Assert.cs`（新）

| 段 | 条数 | 盯什么 |
|---|---|---|
| **A** 出厂档 = 现状 | 8 | 段距 0 · 请求 0 · Overflow · FontStyle · Margin 全出厂；显式传 0 仍 0（含 2 条「必须走 TMP 后端」的防假绿前提） |
| **B** 原版逐件真值 | 13 | 段距 8 / 60 / 0.08 / 20 · 边距 `(4.6,0,0,0)` 与 `(0,0.00775,0,−0.8828)`；写两次不串味 |
| **C** 真实现场 | 16 | `DeckCell` 的 `Deck Name` **= Truncate**、字距 −2；**对照标签仍是 Overflow**；`Banned Text` **= Bold\|UpperCase**（注入禁卡夹具） |
| **D** 灭自证 | 6 | 关掉 TMP 写入 ⇒ 读回不变（证明真写了 TMP）；复位后同一调用落地 |

接入（不动宿主，交调度台）：推荐宿主 `Editor/CollectionScene.cs`（C 段要卡组库；它自带 14 套临时卡组）。

```csharp
B141_W14_Assert.Run(); _pass += B141_W14_Assert.Pass; _fail += B141_W14_Assert.Fail;
```

`Banned Text` 那一段的前提（`C7~C11`）是**硬断言**：库空 / 图缺 ⇒ **红**，不静默跳过。

### 6·3 复跑记录

| 时刻 | 结果 | 说明 |
|---|---|---|
| 18:30（首跑） | 运行时 **4 错** / 编辑器级联 1 错 | 全部在 `RuleEngine/Data/DeckLibrary.cs(825/841/854/867)`：`Debug` 未限定（那文件无 `using UnityEngine;`）—— **别人正在飞的改动**，与本包两个文件无关（⛔ 没动别人的文件） |
| 18:32（复跑） | **运行时错误数: 0 · 编辑器错误数: 0** | 对方把 `Debug` 限定好之后复跑通过；本包 4 个文件全部编过 |

> ⚠️ 复跑用的是**独立 `TMPDIR`**（`/d/tmp/wf_b141_w14`），避免与并发写手的 `wfcheck` 产物互相覆盖。
