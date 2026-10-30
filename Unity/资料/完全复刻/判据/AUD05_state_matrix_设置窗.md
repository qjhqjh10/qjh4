# AUD05 · 状态 × 参数矩阵 ——【设置窗】`Main Menu Settings Window`（原版类 `SettingsMenu : GameWindowWithTabs`）

> 代理：`B103_A05-W5`（只读查判据） · 日期：2026-10-30 · 只读 `d:/2` 与 `Unity/MyGame/Assets`，未跑 Unity、未动 git、未跑 typecheck。
> 触发口径：铁律 5·c「一个值 ≠ 全部情况」。模板 = `判据/AUD05_state_matrix_卡组编辑.md`（结构/口径/红线照它）。
> **本文件只覆盖「设置窗」这一件**：5 个原版页 + 我方自加的 `Online` 页（单列 §4·2）、页签栏、各页控件、下拉、账号登录/删号弹窗、小屏档。不覆盖战斗内那扇 `BattleSettingsWindow`。

---

## §0 结论摘要（先看这 9 行）

| # | 结论 | 强度 |
|---|---|---|
| 1 | **真实页数 = 5，页序 = General / Media / Account / Graphics / Support**（页签栏的物理序）。`tabs` 序列化序是 `General, Media, Account, Support, Graphics`（后两格与物理序不同，见 §1·1），**默认页 = General** | ✅ 已核实（prefab `TabButtons.options` + `GameWindowWithTabs.GetStartingTab`） |
| 2 | **默认页只在【第一次】打开时是 General**：`CurrentTab` 一旦赋值就一直留着 ⇒ 关窗再开**回到上次那一页**（`GameWindowWithTabs.Open:174-184`） | ✅ 已核实（方法体） |
| 3 | 本窗 **`extraScaleSmallScreen = 1.2`（≠1）** ⇒ 小屏 UI 开着时，`GameWindow.Open` 会挂 `TransformScalerBySmallScreenUI` 并 `SetScale(1.2)` ⇒ 窗根 `localScale` 0.9 → **1.08**。**当窗不实时变**（只在开窗那一刻挂/改，与模板 §4·1 的结论同源） | ✅ 已核实（`GameWindow__Open.c` + `GameWindow.cs:84-122` + prefab MB） |
| 4 | **每个页都有运行时 `SetActive` 改写 prefab 值**：General 隐藏 `Disable Notifications`；Graphics 隐藏 `Hi FPS` / `Android extra compatibility`、显示 `Vsync`+`FPS` 行；Account 由 `Refresh` 切 6 颗 + 登录态改输入框；Support 由 `PlatformBasedComponents`(PC 档) + `TermsOfServiceUrl==null` 决定 | ✅ 已核实（各 Tab 方法体 + prefab 平台组） |
| 5 | **小屏档还改字号**：prefab 里 **83 颗 `EverguildTextController`** 带 `maxFontSize/minFontSize`，小屏时 `Awake` 把字号上限改成 `maxFontSize`、下限改成 `minFontSize`（例：`Tab Title` 62/10、`VersionText` 32/5、勾选行 50/10、大钮文字 44/12） | ✅ 已核实（`EverguildTextController.cs:8-34` + 逐颗 MB 字段，见 §3·11） |
| 6 | **语言下拉 = 13 项**（`GameStaticData.languagesDefinition`，含 Turkish / Simplified Chinese，序见 §1·4），**不是** `AvailableLanguages` 的 12 项 | ✅ 已核实（`LanguageSelector.ResetLanguagesDropdown` + `GameStaticData.cs:2474`） |
| 7 | 画质下拉 = 5 档（VeryLow…Ultra，默认 **Ultra**）；手牌文字 = 3 档；FPS 滑块 = 0/1/2 三档（30 / 不限 / 60，默认 **2**） | ✅ 已核实（枚举 + `PlayerDataManager.cs:10254-10268`） |
| 8 | **PC 上 `Delete Button` 被关掉**（`Buttons` 节点的 `PlatformBasedComponents`：PC=0 / android·iOS=1），且 `PlatformBasedComponents.Start` **无条件只跑 PC 组** ⇒ 原版 PC 上点不到删号 | ✅ 已核实（`MB_-5737942901599141978` + `PlatformBasedComponents.cs:63-97`） |
| 9 | 我方 **3 条明确缺口 + 1 条全工程缺口**：语言列表 12/13 与序（G1）· `Disable Notifications` 仍显示（G2）· PC 未关 `Delete Button`（G3）· 小屏字号全工程 0 调用点（G5，设置窗 83 处受它影响） | 🔴 见 §4·3 |

---

## §1 权威公式 / 页序

### 1·1 页数、页序、默认页

```
tabs 序列化值（SettingsMenu MB）            = [General, Media, Account, Support, Graphics]       // 见 §2
页签按钮物理序（VerticalLayoutGroup 子件序） = General, Media, Account, Graphics, Support
TabButtons.options 序                        = 同 tabs 序 = [G, M, A, Support, Graphics]
```

- **默认页**：`SettingsMenu` 的 `windowOptions` 为 **null**（MB 里没有该字段）⇒ `GameWindowWithTabs.GetStartingTab()` 走最后那句 `return Enumerable.FirstOrDefault(tabs)` ⇒ **`tabs[0]` = GeneralTab**（`GameWindowWithTabs.cs:207-274`）。
- **只第一次生效**：`Open()` 只在 `CurrentTab__BackingField == null` 时取默认页；之后 `ChangeTabCO` 一直留着它（`GameWindowWithTabs.cs:161-185 / 363-388`）。
- **首开时把 prefab 的 active 标志归一**：`SetupTabs()` 末尾 `ForceCloseTabs()` 把 5 个页全部 `SetActive(false)`，随后 `CurrentTab.TryOpenTab()` 只开默认页（`GameWindowWithTabs.cs:153 / 403-428`）。
  > ⚠️ prefab 快照里 **`Media Tab` 的 `m_IsActive = True`**（另 3 页 False、General True）—— 这与「General 按钮 `m_IsOn=1`」不一致，是**存的快照不干净**；运行期被 `ForceCloseTabs` 洗掉，**不必照它做**（§附录 A·5）。
- **页切换**：`TabButtons.Toggle(state, tab)` → `window.ChangeTab(tab)` → `from.CloseTab()` + `to.TryOpenTab()`（`TabButtons.cs:269-282` + `GameWindowWithTabs.cs:363-388`）。页签是 `ToggleGroup`，同一时刻只有一个 `isOn`；`allowClickWhenAlreadySelected = 0` ⇒ 点当前页不会关掉它。

### 1·2 小屏缩放公式（本窗是 1.2 家族）

```c
// D:/2/tools/decomp_full/GameWindow__Open.c  —— 逐句等价于：
if (!GameStaticData.smallScreenUI) return;                  // 开关关 ⇒ 什么都不做
if (|extraScaleSmallScreen − 1| <= ε) return;               // 1.0 ⇒ 不覆盖 prefab 里的 menuScale
if (没有 TransformScalerBySmallScreenUI 组件) {
    AddComponent<TransformScalerBySmallScreenUI>();
    SetScale(extraScaleSmallScreen);                        // ← 本窗 = 1.2
}
```
```csharp
// TransformScalerBySmallScreenUI（ilspy 同名文件）
SetScale(x)   { menuScale = x; Initialize(); }
Initialize()  { enabled = menuScale != 1f && GameStaticData.smallScreenUI; }
LateUpdate()  { 若 localScale 与上次记的不同 ⇒ localScale = localScale × menuScale（逐分量），回读记账; }
```
⇒ 本窗：`smallScreenUI = true` 且**窗口是在开关开着之后打开的** ⇒ 窗根 `0.9 × 1.2 = 1.08`。**关窗再开才生效**；窗开着时拨开关，老窗不变（`LateUpdate` 已把 1.08 记成「上次值」，不会再乘；但 `enabled` 也不会被重判）。

### 1·3 非 16:9 的取景

CanvasScaler（主菜单场景，与模板同一颗）：`UiScaleMode = ScaleWithScreenSize` · ref **1920×1080** · `ScreenMatchMode = Expand(1)` · `MatchWidthOrHeight = 0`（模板 §2 已核，出处 `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2577.json`）。
⇒ 画布**只会 ≥1920 宽** ⇒ 窗口设计尺寸（Menu Area 1274.39×843.08）不变，**在更宽的屏上相对更小**；**不会触发布局换档**。⚠️ 算术推断、未实拍（同模板 §6·2）。

### 1·4 语言列表（原版真值 = 13 项）

`LanguageSelector.ResetLanguagesDropdown` 遍历 **`GameStaticData.languagesDefinition`**（`LanguageSelector.cs:52-121`）：

```
1 English · 2 Spanish · 3 Russian · 4 French · 5 German · 6 Portuguese (Brazil) ·
7 Italian · 8 Korean · 9 Simplified Chinese · 10 Japanese · 11 Turkish · 12 Polish · 13 Czech
```
（`GameStaticData.cs:2474` 的 `array3`，13 个元素；`languagesDefinition` 全反编译**只有 cctor 一处赋值** ⇒ 运行期长度固定 13。）
- 每项文案 = `LocalizationManager.GetTermTranslation("MainMenu/Settings/LanguageName/" + languagesDefinition[i])`（键里带的是**这几个英文名原样**，含带空格的 `Portuguese (Brazil)` / `Simplified Chinese`）。
- 当前值 = `LocalizationManager.CurrentLanguage` 在数组里的下标（`ResetLanguagesDropdown` 末尾 `languagesDropdown.value = value`）。
- 切换 = `OnLanguageChanged(option)`：写 `LocalizationManager.CurrentLanguage` → 重建下拉 → 若在战斗中，通知 `BattleManager.BroadcastChangedLanguage()` 与 `BattleHud.BroadcastChangedLanguage()`（`LanguageSelector.cs:28-50`）。
- ⚠️ `AvailableLanguages` 枚举（12 项，无 Turkish）**不是下拉来源** —— 那是另一条轴（见 §附录 A·4）。

---

## §2 prefab 的原始序列化值（未乘任何系数的那一档）

> 全部来自 `D:/2/新解包资源/assets_full/bundle_menus_assets_all/`。**GameObject 的 JSON 文件名是 `m_Name`**，MonoBehaviour/RectTransform 的文件名是 pathID。

### 2·1 窗口根 / 组件引用

| 字段 | 值 | 出处 |
|---|---|---|
| 根 GameObject | `Main Menu Settings Window` · `m_IsActive = true` · 4 个组件 | `GameObject/Main Menu Settings Window.json` |
| 根 RectTransform | `sizeDelta=(0,0)` · anchors `(0,0)-(1,1)` · **`m_LocalScale = 0.9`** · pivot (0.5,0.5) | `RectTransform/RectTransform_-7066813013973172314.json` |
| `SettingsMenu` MB | `type=1` · `windowsPlacement=5`(Canvas) · `closeOnESC=1` · **`extraScaleSmallScreen=1.2`** · `ApplyGraphicsChangesOnClose=0` | `MonoBehaviour/MonoBehaviour_2730265326332837798.json` |
| `tabs[]`（5） | `5160047533284622246`(General) · `-827371763307282522`(Media) · `-2994381932052054106`(Account) · `7781154552317837222`(Support) · `5246199328614809510`(Graphics) | 同上（`m_Script` 经 monoscripts 反查） |
| `closeButton` | `-6660940743103250522` = `Generic Close Button` | 同上 |
| `backgroundCloseButton` | `-2150562177459191898` = `Menu Dark Background` | 同上 |
| `debugButtons` | `2183822808398659494` = `Debug Buttons` | 同上 |
| `supportUINotification` | `-1422674440177090650` = `Support > Badge Highlight` | 同上 |
| `TabbedWindowComponents` | `tabButtons=-6549404459375099994`(`Tab Buttons`) · `tabPrefab=0` · `tabHolder=-3896449222272450650`(**`Tab Content`**) | `MonoBehaviour/MonoBehaviour_309241485349519270.json` |
| `TabButtons.options`（**5 对**） | `[General↔toggle 2348637581993934758, Media↔-3923264254360584282, Account↔-2400866827126341722, Support↔-7224982752548520026, Graphics↔-8519784757761769562]` | `MonoBehaviour/MonoBehaviour_-6549404459375099994.json` |
| `tabButtonPrefab` | **0（null）** ⇒ 全反编译 `SettingsMenu*` 也**没调** `AddTabButton` ⇒ 这 5 颗是**预置**的，`Settings Menu Tab Toggle` 那件**本窗不用** | 同上 + `TabButtons.cs:314-407` |

> **按钮↔页 的绑定已核**：`General` 钮 `m_IsOn=1`，其余 0（`MonoBehaviour_2348637581993934758.json` 等）。

### 2·2 布局参数

| 件 | 值 | 出处 |
|---|---|---|
| `Menu Area` | `sizeDelta=(1274.39, 843.08)` · anchors (0.5,0.5)-(0.5,0.5) · `anchoredPosition=(5.3, −4.65)` | `GameObject/Menu Area.json` 的 RectTransform |
| `Menu Dark Background` | `sizeDelta=(4574.6, 2572.36)`（压暗层，远大于屏） | `GameObject/Menu Dark Background.json` |
| `Mask Tabs buttons` | `sizeDelta=(−20.02, −19.25)` · anchors (0,0)-(1,1) ⇒ 页签列的裁切框 | `GameObject/Mask Tabs buttons.json` |
| `Tab Buttons`（VLG） | `m_Padding=(0,0,13,0)` · **`m_ChildAlignment=5`(MiddleCenter)** · **`m_Spacing=8.92`** · `ChildScaleHeight=1` · `ControlWidth=0` | `GameObject/Tab Buttons.json` 的 VLG MB |
| `Tab Buttons` 自身 RT | `sizeDelta=(2.8, 19.24)` · anchors (0,0)-(0.14,1) · `anchoredPosition=(−10.15, −0.18)` | `GameObject/Tab Buttons.json` |
| 单个页签键 | **165 × 157.68**（5 个逐颗同值）；`Separators` = 2.9×40 | 各 `GameObject/General|Media|Account|Graphics|Support.json` |
| `Tab Content` | `sizeDelta=(0,0)` · anchors (0.14,0)-(0.95,1) ⇒ 页容器 = 0.81×1274.39 ≈ **1032.26 宽**、843.08 高 | `GameObject/Tab Content.json` |
| 5 个页根 | 全 `sizeDelta=(0,0)` · anchors (0,0)-(1,1)（铺满 Tab Content） | 各 `GameObject/<X> Tab.json` |

### 2·3 各页控件（原始值）

| 页 | 控件 / 组 | 序列化值 | 出处 |
|---|---|---|---|
| General | `Language Selector` | `sizeDelta=(834.85, 59.4)` · anchors (0,0.5) · `pos=(90, 176)` | `GameObject/Language Selector.json` |
| General | `LanguagesDropdown` | `400.67 × 59.4` · `Template` `sizeDelta=(−5, 573.96)` · `pos=(−2.5,−22)`；Template 组件 = `Image,ScrollRect,`**`DropdownList`** | `GameObject/LanguagesDropdown.json` / `Template` |
| General | `Checkboxes`（VLG） | `sizeDelta=(834.85, 292.15)` · **`m_Spacing=5`** · `ChildAlignment=0` · `ControlWidth=1`；3 行各 **75.641** 高 | `GameObject/Checkboxes.json` |
| General | 3 个勾选 | `Disable Bots` / `Disable Notifications` / `Touch Input`，均 `EverguildToggle` + `m_IsOn=0` + `m_Interactable=1` | `GameObject/Disable Bots.json` 等 |
| General | `Bottom Buttons`（HLG） | **`m_Spacing=40`** · `ChildAlignment=0` · 两颗各 300×90 | `GameObject/Bottom Buttons.json` |
| Media | `Audio Settings`（VLG） | `sizeDelta=(−90, 350.8)` · anchors (0,0.5)-(0.75,0.5) · `pos=(45, 89.1)` · `ControlWidth=1` | `GameObject/Audio Settings.json` |
| Media | 三行容器 | `Music Container` 105 · `FX Container` 106 · `Voiceovers Container` 106 高 | 各同名 GO |
| Media | 三根滑块 | **`m_MinValue=0` · `m_MaxValue=1` · `m_WholeNumbers=0` · `m_Value=1.0`** · `m_Direction=0` | `GameObject/Music Slider.json` / `Sound effects Slider` / `Voice Over Slider` |
| Media | `Visual Settings`（行） | `889.08 × 298.55` · `pos=(90, −235.58)`；内含 `WindowMode Selector`（834.85×59.4）→ `Dropdown` 400.67×59.4 | `GameObject/Visual Settings.json` |
| Media | `WindowMode` 下拉 | `TMP_Dropdown`：`m_Value=0` · `m_Options` 空（运行期填 2 项） | `GameObject/Dropdown.json` |
| Account | `Account Form` | `920 × 411.92` · `pos=(90, 65.54)`；邮箱/密码输入框各 920×60 | `GameObject/Account Form.json` |
| Account | `Buttons` | **100×100 空容器**（挂 `PlatformBasedComponents`）· 两个 HLG 组 + 两颗中缝钮 | `GameObject/Buttons.json` |
| Account | `Social Media Links`（HLG） | `637.23 × 80.9` · 5 颗（Discord/IG/FB/Twitter/Youtube） | `GameObject/Social Media Links.json` |
| Account | `Login Window` | `1208.58 × 400` · `m_IsActive=False` · `closeOnESC=1` | `GameObject/Login Window.json` / MB `6952589498206683046` |
| Support | `bottom links`（HLG） | `Terms of Service` + `Privacy Policy` 两棵 | `GameObject/bottom links.json` |
| Graphics | `Content`（VLG） | `sizeDelta=(0.11, −144.6)` · `pos=(0.05, −72.3)` | `GameObject/Graphics Tab.json > Content` |
| Graphics | `Quality Selector` / `Text In Hand Selector` | 各 834.85×59.4 · `pos=(45,−29.7)` / `(45,−112.09)`；下拉各 400.67×59.4 | 同名 GO |
| Graphics | `Scroll View` | `987.37 × 521.51` · `pos=(538.68,−425.54)`（ScrollRect，纵向） | `GameObject/Scroll View.json` |
| Graphics | 勾选行 | 各 `455.21 × 75.64`；`Hi FPS toggl` / `Android extra compatibility` 也在（靠代码关） | 同名 GO |
| Graphics | `FPS Limit` 行 | `455.21 × 105` · `pos=(227.6,−455.7)`；滑块 **`m_MinValue=0` · `m_MaxValue=2` · `m_WholeNumbers=1` · `m_Value=0`** | `GameObject/FPS Limit.json` / `FPS Slider.json` |
| Graphics | 两个下拉 | `TMP_Dropdown`：`m_Value=0` · `m_Options` 空（运行期按枚举填 5 / 3 项） | `GameObject/Quality DropDown.json` / `Text in Hand DropDown.json` |

### 2·4 平台组（`PlatformBasedComponents`，枚举 `android=0 · iOS=10 · PC=20`）

| 宿主 | 组内容 | 出处 |
|---|---|---|
| `Media Tab` | 只控 `Visual Settings`：android=**0** · iOS=**0** · **PC=1** | `MonoBehaviour_1952712477060333478.json` |
| `Support Tab` | 10 件：PC ⇒ `Tab Title/Faq Text/Faq Button/Contact Text/Contact Button/Email Text/bottom links`=1，`Support Button/Faq Text Mobile/Email Text Mobile`=0；iOS/android ⇒ `Tab Title/bottom links/Support Button/两行 Mobile`=1，其余 0 | `MonoBehaviour_-495934671143534682.json` |
| Account `Buttons` | 只控 `Delete Button`：android=**1** · iOS=**1** · **PC=0** | `MonoBehaviour_-5737942901599141978.json` |

> 🔴 `PlatformBasedComponents.Start` **无条件只跑 `platformType == PC` 那一组**（`PlatformBasedComponents.cs:63-97`，`if (platformGameObjects.platformType == PlatformDisplay.PC) …DoToggle()`；参数化的 `DisplayPlatform(platform)` 才是按实际平台那支，本窗没用到）⇒ **在任何平台上，PC 组都会生效**。

### 2·5 `EverguildTextController`（小屏字号）—— 83 颗，逐颗字段见 §3·11

| 组件 | 字段 | 小屏时的行为 | 出处 |
|---|---|---|---|
| `EverguildTextController`（独立 MB，挂在本窗每颗 TMP 上） | `maxFontSize` · `minFontSize` | `Awake`：`if (!Application.isPlaying || !smallScreenUI) return;` → `maxFontSize>0` 时（无 autosize ⇒ `fontSize=max`；有 autosize ⇒ `fontSizeMax=max`）；`minFontSize>0` 时 `fontSizeMin=min` | `EverguildTextController.cs:8-34` |

> ⚠️ 这是**另一套**与 TMP 自身 `m_fontSizeMin/m_fontSizeMax` 并列的字段（两者数值常不同，例：`Disable Bots > Label` 的 TMP = `fs42/min29/max42/auto=1`，而 EC = **max50/min10**）。

### 2·6 调试层 / 死节点

| 件 | 状态 | 出处 |
|---|---|---|
| `Debug Buttons`（17 颗） | prefab `act=True`，**`SettingsMenu.Awake` 里 `SetActive(false)`** ⇒ 运行时永远不显示 | `SettingsMenu.cs:104-112` |
| `old menu`（含 `Steam Button`） | prefab `act=False`，静态死节点 | `GameObject/old menu.json` |

---

## §3 状态 × 参数矩阵（主表，按页分）

> 每格写出处；**查不到写 `?`**，⛔ 不拿序列化默认值顶替。数值单位 = 画布参考单位（CanvasScaler ref 1920×1080）。

### 3·1 窗级状态

| 状态 | 判定条件 | 参数 / 取值 | 出处 |
|---|---|---|---|
| **W0 首次打开 · 默认页** | `CurrentTab == null` | 默认 = **General**；`ForceCloseTabs` 先把 5 页全关；窗根 `localScale=0.9` | `GameWindowWithTabs.cs:161-185 / 207-274` |
| **W1 之后打开** | `CurrentTab != null` | 回到**上次那一页**；其余页仍关 | `GameWindowWithTabs.cs:174-184` |
| **W2 关闭** | 关窗（`Close Button` / `Menu Dark Background` / `ESC`） | 依次：`SavePlayerData()` → `base.Close()` → 若 `ApplyGraphicsChangesOnClose` ⇒ `QualitySettingsManager.ApplyGraphicsQuality()` 并把脏位清零 | `SettingsMenu.cs:173-183` |
| **W3 小屏档 ON** | `GameStaticData.smallScreenUI == true`（并重新开窗） | 窗根 `0.9 × 1.2 = **1.08**`（`TransformScalerBySmallScreenUI`）；**另**每颗文字按 §3·11 换字号上下限 | `GameWindow.cs:84-122` + `TransformScalerBySmallScreenUI.cs` + `EverguildTextController.cs:8-34` |
| **W4 小屏档 OFF（桌面默认）** | `smallScreenUI == false` | 窗根 0.9；文字用 TMP 自身 auto-size（`m_fontSizeMin/Max`） | 同上 |
| **W5 非 16:9** | CanvasScaler Expand + ref 1920×1080 | 窗口设计尺寸不变、画布变宽 ⇒ 相对更小；**不换档** | §1·3（算术、未实拍） |
| **W6 压暗层点击** | 点 `Menu Dark Background` | = 关窗（`backgroundCloseButton` 绑 Close） | `SettingsMenu.cs:77-96` |
| **W7 `ESC`** | `closeOnESC=1` | 关窗（先收起语言下拉/其它下拉/登录弹窗，我方实现） | prefab + `SettingsWindow.cs:1596-1613` |

### 3·2 页签栏

| 状态 | 判定条件 | 参数 / 取值 | 出处 |
|---|---|---|---|
| **T0 默认** | 首开 | `General` 钮 `m_IsOn=1`，其余 0 | prefab |
| **T1 切换到第 i 页** | 点键 | 目标页 `SetActive(true)`、其余 `SetActive(false)`；`ToggleGroup` 保证单选 | `TabButtons.cs:269-282` + `GameWindowWithTabs.cs:363-388` |
| **T2 点当前页** | — | `allowClickWhenAlreadySelected=0` ⇒ 不会关掉（`ChangeTab` 里 `from==to` 只重开） | prefab 字段 + `ChangeTabCO` |
| **T3 页签 5 键** | 恒 | 物理序 **General / Media(Multimedia) / Account / Graphics / Support**；每键 **165×157.68**；VLG 居中 + 间距 8.92 + 上内边距 13 | `GameObject/Tab Buttons.json` |
| **T4 键上的字（小屏档）** | `smallScreenUI` | `Tab Toggle Title`：EC **max 38 / min 10**（正常档用 TMP 自身档） | prefab EC 字段（§3·11） |
| **T5 Support 键的徽标** | `SettingsMenu.Awake` 注册 `SupportNotification` | `Badge Highlight` 35×35（右上角偏移 0.742 / 9.601）；**亮不亮取决于服务端通知计数** ⇒ 本地 `?` | `SettingsMenu.cs:113-152` + prefab |
| **T6 页签文案** | 恒 | prefab `m_text` 是**西班牙语**（`General/Multimedia/Cuenta/Gráficos/Soporte`），运行时由 `Localize` 词条覆盖 | 各 `Tab Toggle Title` MB |

### 3·3 General 页

| 状态 | 判定条件 | 参数 / 取值 | 出处 |
|---|---|---|---|
| **G0 默认** | 首开 | `Disable Notifications` 被 `SetActive(false)`；`Touch Input` `SetActive(true)`；`Disable Bots` `SetIsOnWithoutNotify(noBotsModeOn=false)`；`Touch Input` 勾 = `touchInput`（PC 默认 false） | `GeneralTab.cs:85-120` |
| **G1 版本号** | 恒 | `versionText.text = "v" + PlayerDataManager.gameVersionForShowingToPlayers`；`Right/Middle` | `GeneralTab.cs:101-103` |
| **G2 语言下拉·当前值** | 恒 | 13 项（§1·4）；caption = `Loc.LanguageName(CurrentLanguage)`；`value = 当前语言下标` | `LanguageSelector.cs:52-121` |
| **G3 语言下拉·展开** | 点下拉框 | 克隆 `Template`（395.67×573.96 设计）为 `Dropdown List` 浮层；**不改变页布局** | prefab `Template` + `TMP_Dropdown.Show()` |
| **G4 换语言** | 选一项 | 写 `LocalizationManager.CurrentLanguage` → 重建列表；战斗中额外广播给 `BattleManager`/`BattleHud` | `LanguageSelector.cs:28-50` |
| **G5 Disable Bots 开关** | 点击 | `GameStaticData.noBotsModeOn = value`（默认 false） | `GeneralTab.cs:122-125` |
| **G6 Disable Notifications** | — | **永远不显示**（OnSetup 关掉）；切换只写 `notificationsOn` | `GeneralTab.cs:96-97 / 127-134` |
| **G7 Touch Input** | 点击 | 写 `touchInput` + `touchInputChosenManually=true`；SteamDeck 上还改 `Cursor.visible = !touchInput` | `GeneralTab.cs:136-145` |
| **G8 两颗底钮** | 恒 | `Redeem Code` → 弹输入框（`Settings/General/RedeemCodePrompt`）；`Close Game` → `SettingsMenu.ExitGamePopup()`（弹 `Demo/MainMenu/ExitGame`） | `GeneralTab.cs:147-222` + `SettingsMenu.cs:185-201` |
| **G9 文字字号（小屏）** | `smallScreenUI` | `Tab Title` 62/10 · `VersionText` 32/5 · 语言下拉 caption 45/10 · `SelectLanguage` 50/10 · 勾选行 50/10 · `Button Text` 44/12 | §3·11 |

### 3·4 Media 页（原版 `Media Tab` = 音频 + 窗口模式）

| 状态 | 判定条件 | 参数 / 取值 | 出处 |
|---|---|---|---|
| **M0 默认值** | 每次 `OnSetup` | 三根滑块 `value ← GameStaticData.{Music, SoundFx, VoiceOvers}Volume`（出厂 **1 / 1 / 1**） | `MediaTab.cs:20-24` + `GameStaticData.cs:2337-2340` |
| **M1 滑块拖动** | 任一滑块 | 写对应 volume → `PlayerDataManager.SetMusicAndFxVolumes()`（Music→Music/Jingles? 见实现；FX→FX+Jingles；Voice→Voices） | `MediaTab.cs:131-147` + `PlayerDataManager.cs:10431-10440` |
| **M2 滑块极值** | 0 / 1 | 区间 0..1、**不取整**（`m_WholeNumbers=0`）⇒ 连续值；两端就是静音/满音量 | prefab §2·3 |
| **M3 窗口模式下拉·选项** | `OnOpen` 重建 | 2 项：`Settings/Media/WindowMode/Fullscreen`（index 0）· `.../Windowed`（index 1） | `MediaTab.cs:70-129` |
| **M4 窗口模式·当前值** | 恒 | `value = noFullScreen ? 1 : 0`（默认 `noFullScreen=false` ⇒ **Fullscreen**） | 同上（`flag=!noFullScreen; value=!flag`） |
| **M5 窗口模式·切换** | 选一项 | `ResolutionManager.SetFullScreen(option == 0)` | `MediaTab.cs:63-68` |
| **M6 `Visual Settings` 显隐** | `PlatformBasedComponents` PC 组 | **PC=1**（显示）· android/iOS=0（隐藏）；`Start` 无条件跑 PC 组 | §2·4 |
| **M7 下拉展开** | 点下拉框 | 与 G3 同形（TMP 浮层，不动页布局） | prefab `Template` |
| **M8 文字字号（小屏）** | `smallScreenUI` | `Tab Title` 62/10；三行滑块 `Label` + 窗口模式行 `Label` 50/10；窗口模式下拉 caption 45/10（见 §3·11） | §3·11 |

### 3·5 Account 页

| 状态 | 判定条件 | 参数 / 取值 | 出处 |
|---|---|---|---|
| **A0 `Refresh()` 六颗** | `playFabEmail` 是否空 | `Register` = `!loggedIn` · `Login Button ` = **恒关** · `Switch Account` = **恒开** · `Reset Password` = `loggedIn` · `Forgot Password` = **恒关** · `Subscribe Newsletter` = `loggedIn`；`errorMessage.text = ""` | `AccountTab.cs:342-383` |
| **A1 未登录** | `playFabEmail` 空 | 输入框可交互、`emailInput`/`passwordInput` 不动文本（prefab 空）；`Register` 可见 | 同上 |
| **A2 已登录** | `playFabEmail` 非空 | `passwordInput.text = "**********"`（固定 10 星）· `emailInput.text = playFabEmail`· **两框 `interactable=false`**；若 `!demoData.suscriptionPopupShowed && AllTutorialsCompleted()` ⇒ 自动弹订阅弹窗 | `AccountTab.cs:342-383` |
| **A3 登录中** | 点 `Switch Account` → 登录窗 `LoginButtonClick` | `loginButton.interactable=false`；`waitingResponseObject.SetActive(true)`；错误行显示 `MainMenu/Login/TryingToLogin` | `BasicLoginWithEmailWindow.cs:84-124` |
| **A4 输入长度闸** | 输入时 | `emailValid = email.Length > 4` · `passwordValid = password.Length > 5` · `loginButton.interactable = 两者都真` | `BasicLoginWithEmailWindow.cs:139-184` |
| **A5 登录失败** | 服务器回调 | 错误行显示并染红、`waitingResponseObject=false` | `BasicLoginWithEmailWindow.cs:103-120` |
| **A6 登录成功** | 回调 | 关登录窗 | 同上 |
| **A7 Switch Account** | 点钮 | 打开 `Login Window`（`m_IsActive=true`）；`Open` 会清空两输入框、关错误块、`loginButton.interactable=true` | `AccountTab.cs:505-511` + `BasicLoginWithEmailWindow.cs:31-63` |
| **A8 删除账户** | 点 `Delete Button`（**PC 上不可见**，见 A11） | `InactivateAccount` → `DisplayDeleteAccountMessage(text, DeleteAccount, Back, OnConfirm)`：两钮 popup；确定后 → `DeleteAccountWait` 等待弹窗 + PlayFab `DeactivateAccountProcess`（服务器） | `AccountTab.cs:541-662` |
| **A9 社交五钮** | 恒 | 5 个 `UrlButton`，URL 在 `OnSetup` 逐条 `Initialize`（Discord/Instagram/Facebook/Twitter/Youtube） | `AccountTab.cs:246-307` |
| **A10 Twitch** | — | `Twitch Button` prefab `act=False`，`Refresh` 不碰 ⇒ **恒关**；URL 运行时用 `sessionToken/playFabId` 拼 | prefab + `AccountTab.cs:300-307` |
| **A11 Delete Button 平台档** | `Buttons` 的 `PlatformBasedComponents` PC 组 | **PC=0（关）** · android/iOS=1；`Start` 无条件跑 PC 组 ⇒ **PC 上删号钮永远不显示** | §2·4 + `PlatformBasedComponents.cs:63-97` |
| **A12 Player Id** | — | prefab `act=False`，`Refresh` **不碰** ⇒ **恒关**（原版永远不显示） | prefab + `AccountTab.cs:342-383` |
| **A13 Logout / Login Button** | — | prefab `act=False`，`Refresh` 不碰 ⇒ 恒关 | prefab |
| **A14 文字字号（小屏）** | `smallScreenUI` | `Tab Title` 62/10 · `EmailText/PasswordText` 47/10 · `Reset/Forgot` 42/10 · `Error Message` 45/10 · `Subscribe` 45/10 · 大钮 `Button Text` 44/12 | §3·11 |

### 3·6 Graphics 页（可见行随画质档变）

| 状态 | 判定条件 | 参数 / 取值 | 出处 |
|---|---|---|---|
| **R0 下拉选项** | `OnSetup` | 画质 = `Enum.GetValues(EverguildQualitySetting)` = **5 项**（VeryLow/Low/Medium/High/Ultra）；手牌文字 = `CardTextsInHandOptions` = **3 项**（Default/OnlyDescription/OnlyName） | `GraphicsTab.cs:253-725` |
| **R1 初值** | `OnSetup` | `qualityDropdown.SetValueWithoutNotify((int)graphicsQuality)`（默认 **Ultra=4**）；`cardsTextInHandDropdown ← cardsTextInHandFormat`（`smallScreenUI?OnlyDescription:Default`） | 同上 + `GameStaticData.cs:480-497` |
| **R2 画质切换** | 选一项 | 与当前不同才置脏位 `ApplyGraphicsChangesOnClose`；写 `graphicsQuality` + `chosenManually=1`；`ConfigureSuperSamplingVisibility()` | `GraphicsTab__GraphicsQualityDropdownChange.c` |
| **R3 超采样行显隐** | `qualityDropdown.m_Value` → `GetQualityDefinition(v).allowSuperSampling` ∧ `!isMobilePlatform` | `allowSuperSampling` = **0/0/0/0/1**（仅 Ultra 为 1）⇒ **Ultra 才有 `Use super sampling` 行** | `GraphicsTab.cs:84-94` + `B94_R-G_画质档位映射.md` §三.2 |
| **R4 行集（VeryLow–High）** | 非 Ultra | `Small Screen(0) · Auto Zoom(1) · Vsync(2) · FPS Limit(3)`（4 行；Hi FPS / Android 兼容被关） | `GraphicsTab__OnSetup.c:222-257` |
| **R5 行集（Ultra）** | Ultra | `Small Screen(0) · Auto Zoom(1) · Use super sampling(2) · Vsync(3) · FPS Limit(4)`（5 行） | 同上（`ConfigureSuperSamplingVisibility` 只放行 superSampling） |
| **R6 Hi FPS / Android 兼容** | 恒 | `SetActive(false)`（`hiFPS` `+0x38`、`extendedCompatibility` `+0x48`）；`Vsync` `+0x68` 与 FPS 行 `+0x70` 的父级 `SetActive(true)` | `GraphicsTab__OnSetup.c:222-257` |
| **R7 Vsync 开关** | 点击 | 写 `GameStaticData.vsync` **+ 置脏位 `ApplyGraphicsChangesOnClose=1`**（关窗才应用） | `GraphicsTab__VSyncToggleClick.c` |
| **R8 FPS 滑块** | 拖动 | 写 `GameStaticData.FPSLimit=(int)value` **+ 置脏位**；值 0/1/2 = **30 / 不限(-1) / 60**；出厂 **2（60）** | `GraphicsTab__FPSLimitValueChanged.c` + `PlayerDataManager.cs:10254-10268` |
| **R9 超采样开关** | 点击 | 写 `useSuperSampling` **+ 置脏位**（关窗才应用）；可见时初值 = `!isMobile && Screen.width>=1920` | `GraphicsTab__SuperSamplingToggleClick.c` + `PlayerDataManager.cs:10334-10352` |
| **R10 手牌文字档** | 选一项 | 写 `cardsTextInHandFormat` + `chosenManually=1`（无脏位） | `GraphicsTab__CardsTextInHandDropdownChange.c` |
| **R11 Auto Zoom** | 点击 | 写 `useCombatAutoZoom` + `autoCombatChosenManually=1`（默认 = `DefaultTouchInput` = false） | `GraphicsTab.cs:113-117` |
| **R12 Small Screen UI** | 点击 | 写 `smallScreenUI` + `smallUIChosenManually=1`；**不重排已开窗** | `GraphicsTab.cs:119-123` + 模板 §4·1 |
| **R13 滚动** | 内容高于视口 | `Scroll View`（987.37×521.51，纵向）；内容高 = 勾选行×80.641 + 105 | prefab + `Content` VLG |
| **R14 文字字号（小屏）** | `smallScreenUI` | `Tab Title` 62/10 · 勾选行 `Label` 与两行 `Quality selector text` 50/10 · 两颗下拉 caption 45/10 · FPS `Title`+三刻度 50/10 | §3·11 |

> 🔴 **R7/R8/R9/R2 的脏位是同一颗 `SettingsMenu.ApplyGraphicsChangesOnClose`（字段 `+0xc0`）**，只有 `Close()` 才真正 `ApplyGraphicsQuality()`（内含 `ChangeResolution`/framerate）。
> ⚠️ ILSpy 里这几处写成 `_ = 1;` 之类的噪声 ⇒ **按符号名 grep 找不到生产者**（见 §附录 A·2）。

### 3·7 Support 页

| 状态 | 判定条件 | 参数 / 取值 | 出处 |
|---|---|---|---|
| **S0 PC 档** | `PlatformBasedComponents` PC 组 | 显示 `Tab Title / Faq Text / Faq Button / Contact Text / Contact Button / Email Text / bottom links`；隐藏 `Support Button / Faq Text Mobile / Email Text Mobile` | §2·4 + `SupportTab.cs:17-33` |
| **S1 mobile 档** | android/iOS 组（本机不命中，但结构要在） | 显示 `Tab Title / bottom links / Support Button / 两行 Mobile`，其余隐藏 | 同上 |
| **S2 Terms of Service** | `GameStaticData.TermsOfServiceUrl != null` | 该字段 `static readonly` 且 **cctor 里赋 null** ⇒ **恒 `SetActive(false)`** | `SupportTab.cs:26-32` + `GameStaticData.cs:2319` |
| **S3 四颗外链** | 点击 | `PrivacY Policy`/`FAQ`/`Contact` 打开 URL；`Support Button` → `HelpshiftManager.ShowFAQ`（服务器 SDK，PC 档还关着） | `SupportTab.cs:17-46` |
| **S4 文字字号（小屏）** | `smallScreenUI` | `Tab Title` 62/10 · `Faq/Contact/Email` 42~45/10 · `Terms of Service Text` 44/10 · 钮 `Button Text` 44/12 | §3·11 |

### 3·8 下拉展开 / 收起（三颗通用下拉 + 语言下拉）

| 状态 | 判定条件 | 参数 / 取值 | 出处 |
|---|---|---|---|
| **D0 收起** | 默认 | `Template` 原型 `m_IsActive=false`；页布局不变 | prefab 各 `Template` |
| **D1 展开** | 点框 | TMP 克隆 `Template` 成浮层（`Dropdown List`）并显示半透明 `Blocker`；**不动底层布局**；再点外部/`ESC` 收起 | `TMP_Dropdown.Show/Hide`（我们的等价物 `SettingsWindow.cs:3578-3608`） |
| **D2 语言下拉是**`DropdownList`** | — | 语言那颗 `Template` 组件表 = `Image,ScrollRect,`**`DropdownList`**（自定义类），其余三颗 = `Image,ScrollRect`（原生） | prefab §2·3 |
| **D3 项目高** | — | 语言 Template 的 `Content` prefab `sizeDelta=(0, 41.72)` ⇒ 单行 41.72（其余下拉同档） | prefab Template |
| **D4 列表长度** | 恒 | 语言 **13**（§1·4）· 画质 **5** · 手牌文字 **3** · 窗口模式 **2** | §1·4 / R0 / M3 |

### 3·9 登录弹窗（Account 页子窗）

| 状态 | 判定条件 | 参数 / 取值 | 出处 |
|---|---|---|---|
| **L0 关闭** | 默认 | `Login Window` `m_IsActive=false`（prefab） | prefab |
| **L1 打开** | `Switch Account` 点 | 清空两输入框、错误块 `SetActive(false)`、`loginButton.interactable=true`（刚打开时 email/password 都空 ⇒ 再算一次仍 false）、关窗钮可交互 | `BasicLoginWithEmailWindow.cs:31-63` |
| **L2 提交中** | 点 `Login Button` | `EventSystemController.Toggle(false)`（禁输入）；`loginButton.interactable=false`；`ErrorMensajeContainer` 显示但文字 = `TryingToLogin`（不是错误）；`Animated Loading Image`（骷髅/Cog）转起来 | `BasicLoginWithEmailWindow.cs:84-102` |
| **L3 出错** | 回调 | 错误行染红并写服务器错误串；隐藏加载图；`loginButton` 保持禁用 | `BasicLoginWithEmailWindow.cs:103-120` |
| **L4 成功** | 回调 | 关窗 | 同上 |
| **L5 Forgot Password** | 点 | 先隐藏错误行，再 `LoginWithEmailHelper.ResetPassword(email)`；出错只显示 | `BasicLoginWithEmailWindow.cs:125-137` |

### 3·10 平台档（三处，PC 组恒生效）

| 宿主 | 状态 | 表现 | 出处 |
|---|---|---|---|
| Media Tab | PC vs 移动 | `Visual Settings`（窗口模式那行）：PC 亮 / 移动灭 | §2·4 |
| Support Tab | PC vs 移动 | 见 §3·7 S0/S1 | §2·4 |
| Account Buttons | PC vs 移动 | `Delete Button`：PC 灭 / 移动亮 | §2·4 |

### 3·11 **小屏字号表**（`EverguildTextController.maxFontSize / minFontSize`，83 颗）

> 规则：`smallScreenUI=true` 且 `Application.isPlaying` 时，`Awake` 把字号**上限**改成 `maxFontSize`、**下限**改成 `minFontSize`（无 autosize 则直接 `fontSize=max`）。**编辑器批处理里 `Application.isPlaying=false` ⇒ 这条永远不跑**（自检必须显式调等价物）。
> 下表是**每个页出现过的 (max/min) 组合**；同值多颗以「×N」标数量（逐颗全表见本文件写作时的现读，可由 `GameObject/*` 的 EC MB 复算）。

| 页 / 位置 | 文本 | max / min | 出处 |
|---|---|---|---|
| 页签栏（5） | `Tab Toggle Title` | **38 / 10** ×5 | `GameObject/*/Label/Tab Toggle Title` 的 EC |
| 各页页标题（5） | `Tab Title` | **62 / 10** ×5 | 各 `Tab Title` EC |
| General | `VersionText` | **32 / 5** | `VersionText` EC |
| General | 语言 caption `Label` | **45 / 10** | `LanguagesDropdown > Label` EC |
| General | `SelectLanguageText` | **50 / 10** | 同名 EC |
| General | 三个勾选行 `Label` | **50 / 10** ×3 | 各 EC |
| General / Account / Support | 大钮 `Button Text` | **44 / 12**（Debug Buttons 的 17 颗也是 44/12，但恒隐藏） | 各 EC |
| Media | 三行滑块 `Label` + 窗口模式行 `Label` | **50 / 10** ×4 | `Music/FX/Voiceovers Container > Label` + `WindowMode Selector > Label` 的 EC |
| Media | 窗口模式下拉的 caption `Label`（`Dropdown > Label`） | **45 / 10** | `Dropdown` 的 EC |
| Account | `EmailText` / `PasswordText` | **47 / 10** | 各 EC |
| Account | `Reset Password` / `Forgot Password` | **42 / 10** | 各 EC |
| Account | `Error Message` / `Subscribe Newsletter` | **45 / 10** | 各 EC |
| Support | `Faq Text` / `Contact Text` / 两行 Mobile | **42 / 10** | 各 EC |
| Support | `Email Text` | **45 / 10** | 同 |
| Support | `Terms of Service Text` | **44 / 10** | 同 |
| Graphics | 勾选行 `Label`（Small Screen / Auto Zoom / Hi FPS / Android 兼容 / Use super sampling / Vsync） | **50 / 10** ×6 | 各 `Content > <行> > Label` 的 EC |
| Graphics | 两行 `Quality selector text` | **50 / 10** ×2 | `Quality Selector` / `Text In Hand Selector` 的 EC |
| Graphics | 两颗下拉的 caption `Label`（`Quality DropDown > Label` / `Text in Hand DropDown > Label`） | **45 / 10** ×2 | 各 EC |
| Graphics | FPS 行 `Title` + 三个刻度 `30 FPS/60 FPS/Unlimited` | **50 / 10** ×4 | `FPS Limit > Title` / `FPS Slider > *` 的 EC |

> 落到实际渲染还要看 TMP 自身 `m_fontSizeMin/m_fontSizeMax` 与 autosize：EC 只改**上下限**，最终大小由 TMP 在 `[min,max]` 内自动求值。⚠️ 这一条与 TMP 的 `m_fontSizeMin/Max` 是**两套字段**，别混。

---

## §4 我方落点与档位（含 `Online` 页 🔶 单列）

### 4·1 原版 5 页（我方落点）

| 状态 | 我方落点 | 现在哪一档 | 判 |
|---|---|---|---|
| 默认页 General | `SettingsWindow.cs:1789` `_pages.Add(BuildGeneralPage…)` + `:1370` `Current`（enum 默认 0）+ `:1581` `OpenTab(Current)` | General | ✅ |
| 页序 General/Audio(=Media)/Account/Graphics/Support | `SettingsWindow.cs:142` `enum SettingsTab` + `:1789-1793` `_pages` | 原版物理序 | ✅ |
| 页签栏只 5 格、`Online` 不进栏 | `SettingsWindow.cs:1884` `specs` 5 条 + `:3303` 入口钮 | 5 格 | ✅（`Online` 页见 §4·2） |
| 页切换 `SetActive` | `SettingsWindow.cs:1998-2015` `OpenTab`（`_pages[i].SetActive(i==t)`） | ✅ | ✅ |
| W3 小屏 1.2 倍 | `SettingsWindow.cs:1541` `win.extraScaleSmallScreen = 1.2f`；缩放器 `Shell/TransformScalerBySmallScreenUI.cs:104-152` | 1.2 档已接 | ✅ |
| G0 隐藏 `Disable Notifications` | `SettingsWindow.cs:2315` 无条件建了三行（**含 Disable Notifications，未关**） | **显示** | 🔴 **G2** |
| G5/G7 勾选行为 | `GeneralFlags.DisableBots/TouchInput`（`SettingsWindow.cs:2312-2319`） | 已接 | ✅（行为口径另核） |
| G2/1·4 语言列表 | `Loc.Languages`（`Core/Loc.cs:196-203`，**12 项**）→ `SettingsWindow.cs:3509/3627` | **12 项、序不同、无 Turkish** | 🔴 **G1** |
| M0/M1 音量滑块 | `SettingsWindow.cs:5295-5380`（`WarpforgeAudio` 绑定，初值取当前值） | 已接（初值来自我方音频系统，不读 prefab 的 1.0） | ⚠️ 说明项 |
| M3/M4/M5 窗口模式 | `SettingsWindow.cs:5400+`（`Visual Settings` 常显，PC 档） | 已接 | ✅ |
| A0 六颗显隐 | `SettingsWindow.cs:2822-2839` `RefreshAccount()` | 逐条照 `Refresh.c` | ✅ |
| A2 已登录态（掩码/禁交互） | `SettingsWindow.cs:2822-2839`：password **清空**、未设 `interactable`、未掩码 | **未做** | 🔴 **G4** |
| A8 删号确认弹窗 | `Shell/SettingsDeleteAccountFlow.cs` 全文件（客户端两钮 + 本地不出声的服务器半） | 已接（服务器半按 P3 出声） | ✅（P3 见 §5·5） |
| A11 PC 上隐藏 `Delete Button` | `SettingsWindow.cs:2671-2674` 建了但**未关** | **PC 上仍显示** | 🔴 **G3** |
| A12 Player Id 恒关 | `SettingsWindow.cs:2527` `pid.gameObject.SetActive(false)` | ✅ | ✅ |
| R0/R1 画质/手牌下拉 | `SettingsWindow.cs:3939-3970`（画质）；手牌文字档见 `BuildGfx…` | 已接 | ✅ |
| R3/R4/R5 超采样行 | `SettingsWindow.cs:401-426` `SmallScreenRow/AutoZoomRow/SuperSamplingRow` + `GfxContentH(bool)` + `:4486` | 两种排法 | ✅ |
| R7/R8/R9/R10/R11/R12 | `SettingsWindow.cs:4590` ToggleVsync · `:5177` SetFpsIndex · `SuperSampling` 类 · `:3955` 画质脏位 | 见 **G8**（生效时机） | ⚠️ |
| R13 滚动 | `SettingsWindow.cs:4375` `MenuScroll.TopAligned` | 已接 | ✅ |
| S0/S1 平台档 | `SettingsWindow.cs:2961-2977`（Support PC 组）+ `:913-918`（Media PC） | 已接 | ✅ |
| S2 Terms 恒关 | `SettingsWindow.cs:2938` `SetAct(terms,false)` | ✅ | ✅ |
| 小屏字号 83 处 | `Battle/Label.cs:2474` `SetSmallScreenFontSizes` —— **全仓 0 调用点**；`SettingsWindow.Text` 漏斗 `:6113-6143` 不传 EC 两格 | **未接** | 🔴 **G5（全工程）** |

### 4·2 `Online` 页（**我方自加，原版没有**）🔶

> 🔶 自己设计（非原版）：P2P 联机页 · 原版没有这一页、也没有服务器入口（原版服务器已关，属允许自设计的门类；用户 2026-09-26 已拍板做 P2P）。落点 = `Shell/SettingsWindow.cs` 的 `BuildOnlinePage` / `NetConfig` / `NetRuntime`。

| 项 | 我方参数 | 出处 |
|---|---|---|
| 页号 | `SettingsTab.Online = 5`（内容第 6 页，**不在页签栏**） | `SettingsWindow.cs:142 / 1793` |
| 入口 | General 页那颗文字钮 `GenOnlineEntry`（矩形 `GenOnlineL/T` 等） | `SettingsWindow.cs:662-679 / 3285-3305` |
| 控件 | 角色 Host/Client 两档、IP 输入框、密码输入框、状态行 | `SettingsWindow.cs:1488-1500 / 5619-5805` |
| 行为 | `NetConfig.Current.role`、`NetRuntime.Ensure`、`StartHost/…` | 同上 |
| 原版对照 | **0**（原版 5 页、无 Online/Network/Region/Ping 之类） | `SettingsWindow.cs:86-90` 的普查结论 |

> 该页**不受原版状态矩阵约束**；本文只在 §4·2 列它，其余表不重复。

### 4·3 缺口清单（一句一档，带 `文件:行`）

1. 🔴 **G1 语言下拉 12 vs 13**：`Core/Loc.cs:196-203` 的 `Loc.Languages` 是 **12 项**（序 `English, Spanish, French, German, Portuguese, Russian, Italian, Korean, Japanese, Czech, Polish, Chinese`），而原版 `LanguageSelector` 读 `GameStaticData.languagesDefinition` = **13 项**、序为 `English, Spanish, Russian, French, German, Portuguese (Brazil), Italian, Korean, Simplified Chinese, Japanese, Turkish, Polish, Czech`。⇒ **缺 Turkish、序不同、中文位次不同**；`SettingsWindow.cs:3509` 的列表行数与 `AUD03_reuse_paths.tsv:97` 的断言（`LangRowCount == 12`）都钉在 12 上，**断言也要一起改成 13**。
2. 🔴 **G2 `Disable Notifications` 仍显示**：`SettingsWindow.cs:2315` 无条件建了三行；原版 `GeneralTab.OnSetup` 第一句就 `disableNotificationsToggle.gameObject.SetActive(false)`（`GeneralTab.cs:96-97`）⇒ 原版 General 页**只有两颗可见勾选**（Disable Bots / Touch Input）。
3. 🔴 **G3 PC 上未关 `Delete Button`**：`SettingsWindow.cs:2671-2674` 建出该钮且留着；原版 `Buttons` 的 `PlatformBasedComponents`（PC 组=0）在 `Start` 把它关掉（§2·4）。⇒ PC 上我方多一颗「Delete account」。
4. 🔴 **G4 已登录态没做**：`RefreshAccount`（`SettingsWindow.cs:2822-2839`）只清密码、给邮箱；原版 `AccountTab.Refresh` 还做：`passwordInput.text="**********"`、`emailInput.interactable=false`、`passwordInput.interactable=false`（`AccountTab.cs:342-383`）。⇒ 我方登录后密码空着且两框仍可编辑。
5. 🔴 **G5 小屏字号全工程没接**：`Battle/Label.cs:2474` `SetSmallScreenFontSizes` **0 调用点**（`grep -rn SetSmallScreenFontSizes` 只有定义）；设置窗 `Text()` 漏斗（`:6113-6143`）只接 TMP 自己的 `m_fontSizeMin/Max`，**不传 `EverguildTextController.maxFontSize/minFontSize`** ⇒ §3·11 那 83 颗小屏字号一条都不生效。
6. 🟡 **G6 登录弹窗没有「等待中 / 输入长度闸」**：`SettingsWindow.cs` 的 `LoginWindowSubmit`（`:2799-2808`）只判非空、随后本地即时登录；原版有 `email>4 / password>5` 的按钮闸（`BasicLoginWithEmailWindow.cs:139-184`）与 `waitingResponseObject` 的等待态（`:84-102`）。**客户端状态、不属服务器半**（可做）。
7. 🟡 **G7 登录后的订阅自动弹窗没做**：原版 `Refresh` 在「已登录 + 未弹过 + 教程全通」时 `TrySubscribeNewsletter()`（`AccountTab.cs:342-383 / 664-698`）；我方 `RefreshAccount` 无此分支。
8. ⚠️ **G8 图形改动生效时机**：原版 `画质/超采样/Vsync/FPS` 四处**只写值 + 置脏位 `ApplyGraphicsChangesOnClose`**，真正应用要等**关窗**（`SettingsMenu.cs:173-183`）；我方 `ToggleVsync`/`SetFpsIndex`（`SettingsWindow.cs:4590-4596 / 5177-5190`）**立即**写 `QualitySettings.vSyncCount` / `Application.targetFrameRate`。⇒ 生效时刻不同（我方即时、原版关窗）。
9. 🟡 **G9 音量初值来源**：原版三根 slider 初值写死 `GameStaticData.{Music,SoundFx,VoiceOvers}Volume`（出厂 1.0，`MediaTab.cs:20-24`）；我方初值取 `WarpforgeAudio.*` 当前值（`SettingsWindow.cs:5303`）—— 只要 `WarpforgeAudio` 的默认也是 1.0 就等价，**需一次校验**。

---

## §5 按文件分组的施工建议（调度台按文件切给写手）

| # | 文件 | 要做什么 | 判据（本文件哪一节） | 风险 |
|---|---|---|---|---|
| **A** | `CardPresentation/Core/Loc.cs`（+ `Shell/SettingsWindow.cs` 只读改引用）+ `Editor/SettingsScene.cs` 断言 | 把语言下拉的来源/顺序/条数改成原版 **13 项**（`languagesDefinition` 序）；`Loc.Languages` 或新增一份 `LanguageDropdownOrder`；**别动 `AvailableLanguages` 枚举本身**（那是另一条轴） | §1·4 · §4·3 G1 | 中：改序会动 `ChooseLanguage(i)` 的行号↔语言映射；`AUD03` 的 `LangRowCount==12` 断言必须同批改成 13 |
| **B** | `Shell/SettingsWindow.cs` `BuildGeneralPage`（`:2312-2319`） | 照原版把 `Disable Notifications` 三行里**第二行关掉**（照原样建、`SetActive(false)`，⛔ 不删件），与 `Support Button`/`Terms` 同一条做法 | §3·3 G0 · `GeneralTab.cs:96-97` | 低（行高由 VLG 式现算，关掉不影响其余两行位置？⚠️ 我方三行是绝对矩形，关掉第三行会留空 —— 应把 Touch Input 上移到第 2 行，**需按 VLG 跳过 inactive 重算**） |
| **C** | `Shell/SettingsWindow.cs` `BuildAccountPage`（`:2671`） | 照 PC 平台档把 `Delete Button` `SetActive(false)`（结构留着，换平台可亮） | §2·4 · §3·5 A11 | 低 |
| **D** | `Shell/SettingsWindow.cs` `RefreshAccount`（`:2822-2839`）+ `AcInput`（`:6475+`） | 已登录态：密码显示 `**********`、两输入框 `interactable=false`；登出态复原 | §3·5 A2 · `AccountTab.cs:342-383` | 中：`AcInput` 目前**没有 interactable 概念**，要给它加一个禁用态（不改输入行为、只挡点击/编辑） |
| **E** | `Battle/Label.cs`（已有机制，仅接线）+ `Shell/SettingsWindow.cs` `Text()` 漏斗 | 给 `Label` 传 `EverguildTextController` 的 `maxFontSize/minFontSize`（新形参或建完调 `SetSmallScreenFontSizes`）；**先接设置窗 83 处**，再推广到其他窗 | §3·11 · §4·3 G5 | **高**：`Text()` 是全窗唯一漏斗（15+ 调用点）；`SetSmallScreenFontSizes` 必须排在基线字号/自适应之后（`Label.cs` doc 已写死）；且批处理里不生效 → 断言要读 `SmallScreenSizeAppliedForTest` 或直接调 |
| **F** | `Shell/SettingsWindow.cs` `LoginWindowSubmit`（`:2799`） | 补输入长度闸（email>4 / password>5）与「等待中」态（按钮禁用 + 加载图 + `TryingToLogin` 文案）；本地点登录保持瞬时完成 | §3·9 L1-L2 · `BasicLoginWithEmailWindow.cs` | 中：加载图/`Cog` 已建（`:3124-3139`），只是没在提交时显示 |
| **G** | `Shell/SettingsWindow.cs` `ToggleVsync` / `SetFpsIndex`（`:4590` / `:5177`） | 二选一：①照原版只写值+脏位、关窗统一应用（`ApplyGraphicsQualityOnClose` 已有，`SuperSampling` 同理）；②保持即时应用但在文档/断言里写明这是主动偏离 | §3·6 R7-R9 · §4·3 G8 | 中：影响真机「改完当刻」的观感；若改，`SettingsScene` 的即时读值断言要一起改 |
| **H** | `Shell/SettingsWindow.cs` `RefreshAccount` | 补「已登录 + 未弹过 + 教程全通 ⇒ 订阅弹窗」分支（弹窗本身是客户端 UI，勾选后打服务器的半条按 P3 出声） | §3·5 A2 · `AccountTab.cs:664-698` | 低-中 |

> ⛔ **本波不要动** `Settings Menu Tab Toggle` 那件（`tabButtonPrefab=0`、本窗不用）；也不要给页签栏加第 6 格（`Online` 入口已定在 General 页，用户裁定）。

---

## §5·5 **不做 / 为什么**（已裁定「故意不接」的清单）

> 每条带判据 + 反证，⛔ 不许留口头结论（铁律 5·b / 11 例外①/②）。

| # | 项 | 判据 / 反证 |
|---|---|---|
| N-1 | `Debug Buttons` 那 17 颗 | `SettingsMenu.Awake` 无条件 `SetActive(false)`（`SettingsMenu.cs:104-112`）⇒ 原版永远不显示；我方不建 = 等价。⛔ 别为了「结构 1:1」把它建出来再关（会多 17 个节点/命中区）。 |
| N-2 | `Player Id` | prefab `act=False`、`Refresh` 不碰 ⇒ 原版恒不显示；我方照建 + 照关（`SettingsWindow.cs:2527`）。 |
| N-3 | `Logout Button` / `Unregistered Login Button ` / `Twitch Button` | 都在 `Refresh` 之外（不碰）+ prefab `act=False` ⇒ 恒关；我方同样不碰（`SettingsWindow.cs:2664-2670`）。⚠️ `Twitch` 的 URL 是运行时拼 token，属服务器侧。 |
| N-4 | `Support Button`（Helpshift） | PC 档 `PlatformBasedComponents` 关掉（§2·4）；且其实现是服务器 SDK ⇒ 本地只出声（`SettingsWindow.cs:1261`）。 |
| N-5 | `Terms of Service` | `TermsOfServiceUrl` cctor 赋 null ⇒ 原版恒关；我方 `SetAct(terms,false)`（`SettingsWindow.cs:2938`）。⛔ 别编一条 URL。 |
| N-6 | Support 键徽标「亮起来」 | 由服务端通知计数决定（`SettingsMenu.Awake` 只注册类型，见 `SettingsWindow.cs:1955-1976`）⇒ 本地 alpha 0 + 出声；属 P3/服务器半。 |
| N-7 | 删号确认之后的 `DeleteAccountWait` / PlayFab 删号 | 服务器域（PlayFab CloudScript）；我方只做客户端确认弹窗 + 本地说明（`SettingsDeleteAccountFlow.cs` 文件头）。铁律 11 例外②（服务器相关可自设计）。 |
| N-8 | `MediaTab.OnDestroy` 的可疑写法 | 原版 `OnDestroy` 对 `windowModeDropdown` 写的是 `AddListener`（不是 `RemoveListener`，`MediaTab.cs:58-60`）——**原版自身的 bug**、无用户可见效果；复刻与否都行，⛔ 别在文档里写成「原版是 Remove」。 |
| N-9 | Tab 切换动画/音效 | 本窗 `TabButtons` 无动画分支；`EverguildToggle` 有 `onValueChanged` 但没挂音效调用。**未查到有额外表现**，按无动画做。 |

---

## §6 **未查清**（`?`，本文件全部不确定项）

1. **`?` 1 —— `allowSuperSampling` 的运行时真值**：数值（0/0/0/0/1）间接引自 `判据/B94_R-G_画质档位映射.md` §三.2（`Intro.unity` 的 `QualitySettingsManager` MB `&346`）。本会话在 `assets_full` 里**没找到** `QualitySettingsManager` 的脚本资产（不在 `bundle_Waprforge_monoscripts` 的 722 类里，`grep -rl allowSuperSampling` 全 bundle 0 命中）⇒ **要坐实需去 `assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity` 现读**（B94 已读过）。
2. **`?` 2 —— 非 16:9 实拍**：§1·3 是 CanvasScaler 语义推断，本机无原版运行期菜单 dump（`runtime_ui_dump_*` 只覆盖 Battle/Intro）。
3. **`?` 3 —— 语言默认值**：`LanguageSelector` 把 `value` 设成 `CurrentLanguage` 的下标，但 `CurrentLanguage` 在无存档/无 PlayerPrefs 时的初值由 I2 决定（系统语言/`I2 Language` 键）——**本机未核**。我方默认 `Chinese`（`Core/Loc.cs:178`）是**自选**，不是原版判据。
4. **`?` 4 —— Support 徽标亮灭**：见 N-6，服务端通知计数本地拿不到。
5. **`?` 5 —— `Media Tab` 根 `m_IsActive=True` 的来历**：prefab 快照与「General 按钮 `m_IsOn=1`」不一致（§附录 A·5），运行期被 `ForceCloseTabs` 归一；**到底是导出脏数据还是某个会话里真的开着**，未追（不影响运行期）。
6. **`?` 6 —— `Tab Title` 等页标题的 `Localize.mTerm`**：本文件只核了 prefab `m_text`（西语）与组件表，**词条键未逐条抄**（属于本地化线，见 `AUD07_loc_keys`）。
7. **`?` 7 —— `Graphics` 页 `Scroll View` 的 `Viewport` 高与内容高换档**：我方用 `GfxContentH(withSS)` 现算（`:481`）；原版 VLG + `ContentSizeFitter` 的实际内容高**未在本会话逐字段复算**（结构与公式已知，数值未与 prefab 对账）。

---

## 附录 A · 顺手发现（不属本窗主线，但值得记账）

1. 🔴 **`SettingsWindow.cs:938` 的一条注释与 prefab 不符**：它写「`Logout Button` / `Delete Button` / `Login Window` / `Player Id` … **三者都 `m_IsActive=0`**」。实读：**`Delete Button` 的 `m_IsActive=True`**（`GameObject/Delete Button.json`），它最终不可见是靠 `Buttons` 的 `PlatformBasedComponents`（PC=0），**不是靠 prefab 开关**。⇒ 建议就地订正该注释（铁律 5），否则会误导「PC 上删号钮本来就不显示」。
2. 🔴 **`ApplyGraphicsChangesOnClose` 的生产者 ILSpy 里看不见**：按符号名 grep 只有 `SettingsMenu.cs` 四处（读/清）。真身在 `decomp_full`：`GraphicsTab__GraphicsQualityDropdownChange.c:44-45` · `__SuperSamplingToggleClick.c:14-18` · `__VSyncToggleClick.c` · `__FPSLimitValueChanged.c` 都写 `*(baseWindow + 0xc0) = 1`（`baseWindow` = `WindowTabBase +0x28`）。⇒ 它是**活的脏位**，不是死支；判「字段有没有生产者」不能只信 ILSpy（同 CLAUDE.md 那条方法论）。
3. 🟡 **`MediaTab.OnDestroy` 写的是 `AddListener`**（`MediaTab.cs:58-60`）——原版自身 bug（应为 Remove）。无用户可见效果，别写成「移除监听」。
4. 🔴 **`Core/Loc.cs` 的两条论断需订正**：`:8-15` 与 `:123` 说「下拉用 `AvailableLanguages`（12 项）、Turkish 只当数据不进下拉」。真身是 `LanguageSelector` 读 `GameStaticData.languagesDefinition`（13 项，含 Turkish）。⇒ 与 G1 同一条，订正落点 = `Loc.cs` 注释 + 列表实现。
5. 🟡 **prefab 的 `Media Tab` 是 active**（其余非当前页 inactive）——快照不干净；运行期首开被 `ForceCloseTabs` 洗掉。别照快照的 active 标志复刻。
6. ⚪ **`TabButtons.options` 序（…Support, Graphics）≠ 物理子件序（…Graphics, Support）**：两处只影响 `List<WindowTabBase>` 的遍历顺序（默认页取 `FirstOrDefault` = General），**绑定本身按每个 option 自己的 toggle 走**，所以绑定是对的、画面也按物理序。
7. ⚪ **`Settings Menu Tab Toggle` 那件本窗不用**：`TabButtons.tabButtonPrefab=0`、全反编译 `SettingsMenu*` 0 处 `AddTabButton` ⇒ 那 5 颗是预置的。别照着那个 prefab 重造页签。
8. ⚪ **`Quality DropDown` 的选项文案键 = `Settings/Graphics/<枚举名>`**（不是数字下标）；手牌文字 = `Settings/CardTextInHand/<枚举名>`（`GraphicsTab.cs:454-456 / 660-662`）。

---

## 附录 B · 覆盖边界（本文件到底查了多少）

| 维度 | 覆盖 |
|---|---|
| **读透的文件（反编译）** | `SettingsMenu.cs`(202) · `GameWindowWithTabs.cs`(595，只读页序/默认/切换/关闭) · `WindowTabBase.cs`(111) · `TabButtons.cs`(505) · `GeneralTab.cs`(320) · `MediaTab.cs`(148) · `AccountTab.cs`(861，只读 OnSetup/OnOpen/Refresh/删除/订阅) · `SupportTab.cs`(47) · `GraphicsTab.cs`(726) · `LanguageSelector.cs`(122) · `AvailableLanguages.cs`(15) · `BasicLoginWithEmailWindow.cs`(220) · `PlatformBasedComponents.cs`(约 130) · `EverguildTextController.cs`(35) · `TransformScalerBySmallScreenUI.cs`(约 80) · `GameStaticData.cs` 相关段 + `PlayerDataManager.cs`（FPS/超采样/默认值） · `decomp_full`：`GameWindow__Open.c`、`GraphicsTab__OnSetup/VSync/FPS/Quality/SuperSampling/*Click.c`、`SettingsMenu__Close`（经我方注释引用） |
| **预制体** | 窗口根 + 5 页 + 页签栏 + 三颗下拉 + 四根滑块 + 十余个勾选 + 三处平台组（逐字段）；**83 颗 `EverguildTextController` 的 max/min 全扫** |
| **未读** | `GraphicsTab` 的 `ConfigureSuperSamplingVisibility` 之外的 `QualitySettingsManager` 全量 · `AccountTab` 的 `Register/Login/LogOut` 协程全量（只读 Refresh/删除/订阅） · `BasicLoginWithEmailWindow` 的 `CheckLoginButtonState` 以外辅助 · `SettingsDebugButtons` 的 17 颗行为（本窗不显示） |
| **反编译源** | ① `cpp2il/out_cpp2il_dev_ilspy/*.cs`（主力）② `decomp_full/*.c`（脏位/SetActive/小屏缩放，**多处只有 .c 能读对**）③ `d:/2/Warpforge_code`（枚举） |
| **`?` 的数量** | 7 格（§6·1–§6·7） |
| **没跑的东西** | Unity 批处理 / 真机 / `typecheck`（按只读代理红线，一律没跑） |
| ⚠️ 提醒 | **「本文件把状态参数查清了」≠「原版这个窗全复刻了」** —— 本文件只回答「哪个状态对应哪套参数/显隐/字号」，不回答「我们画得像不像」（那要靠并排实拍/真机，属调度台验收）。 |
