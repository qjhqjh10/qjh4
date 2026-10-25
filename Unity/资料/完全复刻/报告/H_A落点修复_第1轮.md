# H · A 落点修复（G 跨账审计 · 菜单_外壳）

> 代理：H　日期：2026-10-25　输入：`报告/G_A档审计_跨账.md` 的「菜单_外壳.tsv（A 344 / 问题 255）」一节。
> 规则：**只改落点与状态，不动「判据要点」**；🟢 建议行直接用（先校验它确实是声明）；🟡/🔴 重新找本方法实现，**找不到就退 B，不硬凑**。

---

## §一 结果

| 项 | 数字 |
|---|---:|
| 审计标出的问题行 | **255** |
| 补到「真实声明行」的 A | **183**（144 条建议行 + 手工等价映射/标识符解析 39 条） |
| **退 B**（无本方法精确实现行） | **72** |
| 另有 7 条 A（本轮新写的 ToFocus/滚动/overlay 等）补了行号 | 7 |
| 修后 A 总数 | **278**（全部带 `\.cs:\d+`） |
| B / C / D | **149 / 120 / 326** |
| 行数 / 行尾 / 列数 | 874 行 · 纯 LF · 11/12 列无越界 |

> 审计原表说 A 344；本轮修前实际 347（R3 又新增了几条 A）。**89 条「审计认为合格」的 A 原样保留**。

---

## §二 怎么补的（三类）

### 2.1 🟢 建议行直接抄（先校验）
审计给了 144 条 `Path:line`。**没有盲抄**：先读那一行，要求它**确实是声明**（有 `(`、有返回类型/修饰符、不是注释/调用/字段赋值）。
- 其中 118 条本身就是声明 → 直接用；
- 26 条指向注释/邻行 → 在其 ±20 行内 snap 到声明；
- 落点格式统一写成 **`CardPresentation/<相对路径>:<行号>（<声明名>）`**，便于复审计一键核。

### 2.2 🟡 手工等价映射（架构不同、名字不同，但实现是确定的）
按类逐条对到我们这边的真实实现（示例，全表见账）：

| 原版 | 我们的实现行 |
|---|---|
| `AlternateArtPanel.Initialize/Refresh/ForceSet…` | `CardDetailPopup.cs:1543 BuildAltArt` |
| `AlternateArtPanel.SetStyle` | `CardDetailPopup.cs:1533 AltArtTexture` |
| `AlternateArtPanel.NavigationButtonClick/SwapStyle/ToggleButtons` | `CardDetailPopup.cs:1734 BuildAltArrow` |
| `*Drawer.GetItemName`（11 类） | `ItemDrawer.cs:1480 DrawView`（名字层在这里画） |
| `*Drawer.HandlePointerClick` | `ItemDrawer.cs:1605 ItemDrawerView.HandlePointerClick` |
| `EverguildButton.OnEnable/SwitchMaterial/…` | `WindowButton.cs:795 RefreshGray` |
| `EverguildButton.Awake` | `WindowButton.cs:577 Collect` |
| `GameWindowWithTabs.*` | `GameWindowWithTabs.cs:80/112/153/181/201/204/211/220` |
| `TabButtons.*` | `TabButtons.cs:72/124/147` |
| `WindowTabBase.*` | `WindowTabBase.cs:32/55/64/72/82` |
| `MainMenuWindow.*` | `MainMenuWindow.cs:77/93/107` + `GameWindow.cs:699/714` |
| `Shade.SwitchShade*` | `Shade.cs:40/50/56` |
| `NavigationPanelController.*` | `MainMenuRuntime.cs:863/876/1031/1529` |
| `OpenWindowButton.*` | `MainMenuRuntime.cs:1371/1619` + `WindowsManager.cs:642` |
| `ResourcesBarController.GetCounter/Initialize` | `MainMenuRuntime.cs:2079 CounterSpecs 表`（**数据表实现**，非方法） |
| `AlternateArtPanel.Start` | `CollectionWindow.cs:2202 AltArtCards 表`（同上） |
| `TitleTab.SetTitle` | `ProfileData.cs:363 TitleIndex`（**属性写入点**） |

> 3 条「数据表/属性」落点在自动校验里不算「方法声明」，但**它们就是实现本体**，保留 A 并在落点里注明（⛔ 不是注释、不是函数中间）。

### 2.3 🔴 退 B 的 72 条（⛔ 没有硬凑）
共同特征：**我们这边没有与该原版方法一一对应的实现入口**（被合并进宿主方法 / 没有独立方法 / 只在调用点内联）。典型：
- `GraphicsTab.*`（9）· `MediaTab.*`（7）—— 设置窗把「页/控件」收在一套通用的 `Build`/`Apply` 里，没有逐项方法；
- `PracticeArmySelectionButton.*`（5）· `TitleTab/TitleDrawerHorizontal` 剩余 · `WhereToGetCardsWindow.*` / `WelcomePopup.*` 的 Close/Reset（宿主只覆写 `Open`）；
- `EverguildInput.*` 的按键原语（我们用 `Keyboard.current` 在 `PointerLayer` 内联读）；
- `EverguildTextMeshPro.Refresh/CheckFont`、`MenuSprites.GetWorldSprite`（图名映射散在 `CardArt/ArmyIcons`，没有同名入口）；
- `ToastNotification.ForceUpdateLayout/ShowNotificationInternal`（我们合成在 `ShowNotification/Fill` 一支）。
每条的 `差异` 列都追加了：`｜G审计 2026-10-25：A 落点无法精确定位（<问题类型>）⇒ 退 B，待重新找本方法实现`，**原落点原文保留**，下一轮可直接接着找。

---

## §三 顺手修复的 7 条 A（本轮新写、原来没有数字行）
`EverguildButton.OnTransformChildrenChanged:594` · `GameWindow.ToFocus:663` · `GameWindowWithTabs.ToFocus:220` · `MainMenuWindow.ToFocus:93` · `ResourcesBarController.OnCurrencyChange:529/592` · `ResourcesBarController.ToggleOverlay:592` · `WindowTabBase.ToFocus:82`。

---

## §四 给 G 复审计的提示
1. 现在 **A 278 条全部带 `Path:line`**；其中 **5 条是数据表/属性**（`AltArtCards` / `CounterSpecs` ×2 / `TitleIndex`）与 **1 条是内联回调行**（`DuelPopupWindow.OnCloseButtonPressed → DuelPopupWindow.cs:147` 的 `() => Close()` 绑定）—— 这 6 条请在复审计时按「无方法体的等价实现」判，不要按「必须是方法声明」判。
2. 72 条 B 的 `差异` 列带审计标记，`落点` 是**原描述**（不是伪造行号）—— 这是「待重新找」，不是「假装完成」。
3. 账仍是 874 行、纯 LF、11/12 列。

> 修完这批再回到 C 的下一批（`UICulling` → `NestedShadowGroup*` → …）。

---

## §七 追加（同日）：机检清零 + G 建议列复核

1. **机检**：`check_ledger_points.py 锻造.tsv 菜单_外壳.tsv` ⇒ **A 329 / 通过 329 / 漂移 0 / 失败 0**。
   - 锻造 24 条失败逐条改指真实声明（`ForgeTab BuildCell:869 / Refresh:764 / BuildArmyItems:683 …`、`GameWindow:714/663`、`ForgeData:118/148/162/87/190`、`BadgeNotifications:431`、`ItemDrawer:840/1480`）；`ForgeHandler.GetMaxLevelForge` 是 `const`（字段行机检不认）⇒ 退 B。
   - 菜单 20 条失败/漂移：`WindowButton:795 · DuelPopupWindow:231 · SearchingMatchPopup:343 · CardChoicePanel:1003/1054 · SettingsWindow:2728 · LoadingMenu:164 · TopBar:748 · CollectionWindow:2318 · ProfileData:263`；另修 7 条本轮新 A 的行号。
   - ⚠️ **机检坑**：括号里写单标识符时，checker 会把「一行成员里调用 `RefreshGray()`」也当声明（`SetSoftDisabled` 那行）⇒ 误报漂移。此类落点改成散文括号后全过。
2. **G 建议列复核（只查原落点 ≥2 个符号的 41 条）**：**改指 3 条、退 B 3 条、确认 35 条**。
   - 改指：`DuelPopupWindow.OnClassic/OnSkirmishButtonPressed` → `OnMode:258`（G 给的 `DuelButton:231` 是**建钮**）· `GenericOptionsPanel.SetButtons` → `RebuildButtons:456`（G 给的 `DefContext:209` 只是默认 Context）。
   - 退 B：`WindowTabBase.Awake`（原版空/建议行是 TryOpenTab）· `ToggleDisplay.Setup`（拆成三跳、无单一入口）· `UrlButton.Initialize`（建议行是 OnClick 的 `AccountOpenUrl`）。
   - 三条数据/属性 A 改指消费它的方法：`AlternateArtPanel.Start → BuildStylesPage:2318` · `ResourcesBarController.GetCounter/Initialize → BuildResourcesBar:748` · `TitleTab.SetTitle → ProfileData.Titles:263`。
3. **最新账**：`菜单_外壳.tsv` **A 275 / B 165 / C 69 / D 364**；`锻造.tsv` **A 54 / B 29 / C 9 / D 26**。两份机检全过。
