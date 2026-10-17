# W_E · 设置窗三个下拉（T17）

> 写手：T17 写手代理 · 2026-10-17
> 只改了 `Unity/MyGame/Assets/CardPresentation/Shell/SettingsWindow.cs`（一个文件、无 partial/辅助文件）。
> 参考实现 = 同文件 A862 的语言下拉（`BuildLangTemplate` / `ShowLangList` / `HideLangList` / `ChooseLanguage`）。

---

## 一、结论

三颗下拉**全部建成真下拉**（点开列表 → 点选项生效 → 点空白/ESC 收起），并**逐条照 `TMP_Dropdown` 的实现链**：

| 件 | 改前 | 改后 |
|---|---|---|
| 画质 `Quality DropDown` | `Hit → CycleQuality()`（点一下换下一个），无 `Template` | `TMP_Dropdown` 语义：点开列表、选项生效、空白/ESC 收起；`Template` 子组建好、恒关 |
| `WindowMode Selector` | **整行没建**（只在日志里说「不做假开关」） | 整行补建：`Visual Settings > WindowMode Selector > Dropdown`（400.67×59.40）+ `Template`；PC 档显示 |
| `Text In Hand  Selector` | **整行没建**（旧注释记「出厂关」） | 整行补建：`Graphics Tab > Text In Hand Selector > Text in Hand DropDown`（400.67×59.40）+ `Template`；**运行时显示**（见 §二·2 的实据） |

`typecheck`：**运行时 0 错 / 编辑器 0 错**（§四）。

### 改动清单（`Shell/SettingsWindow.cs`）

| 行 | 内容 |
|---|---|
| 821–900 | 新增「通用下拉」常量块：`LstItemGap` / `DdPanelHFull` / `DdTplDown` / `DdTplRightInset` / `DdViewportInsetR` / `DdCapTop` / `DdCapBottom` / `DdArrowTop` / `DdArrowRightInset` / `DdArrowW`；`Tih*`、`Vs*`、`Win*` 三行矩形的设计值（逐值附屏幕 px 与反算算式） |
| 3506–3904 | `DropdownBox` 状态机 + `BuildDropdown` / `BuildDropdownSubtree` / `BuildDropdownItem` / `ShowDropdown` / `HideDropdown` / `HideAllDropdowns` / `ChooseDropdown` / `EnsureDdBlocker` / `RefreshDropdownRows`；自检只读口 `DropdownCount` / `DropdownTemplateNode` / `DropdownListNode` / `DropdownFieldNode` / `DropdownOpen` / `DropdownRowCount` / `DropdownRowBg` / `DropdownRowLabel` / `DropdownRowHit` / `DropdownBlockerNode`（⚠️ **按 `DdQuality/DdWindowMode/DdTextInHand` 这三个【名字编号】取**，⛔ 不是 `_dds` 下标 —— 建页次序 `General→Audio→Graphics…` 使 Audio 那颗先入列） |
| 3737–3869 | 三颗的「选项 / 当前值 / 选中动作」：`QualityOptionLabels` · `ChooseQualityOption` · `WindowModeOptionLabels` · `ChooseWindowModeOption` · `TextInHandOptionLabels` · `ChooseTextInHandOption` · `TextInHand` 静态值类（`PlayerPrefs "CardsTextInHand"` + `PersistOverride`） |
| 2290 | `UiTerm(key, en, where)`：与 `AcTerm` 同形状的两步漏斗（表里有→用表；没有→原版英文 + 出声一次） |
| 4079–4124 | 画质那颗接上真下拉（`Hit → ToggleDropdown`）；新补 `Text In Hand Selector` 整行 |
| 5020–5075 | 新补 `Visual Settings > WindowMode Selector` 整行；把「音量走 AudioMixer」那行**我们自己的**说明挪到它下面 |
| 1544 / 1575 / 1893 / 3432 / 1617 | 生命周期收口：`Close()` / `ESCPressed()` / `OpenTab()` / `ShowLangList()` 都会先收通用下拉；`Build()` 清 `_dds`；`RefreshTexts()` 刷三颗的行内文字与勾 |
| 4352 | 删 `CycleQuality()`（旧行为、0 调用点，改成注释指向 `ChooseQualityOption`） |
| 3352 | 🔴 **就地更正（铁律 5）**：语言下拉列表的面板底图原来 tint 传 `null`（白）—— 原版 `Template.Image.m_Color` 实读 `(0.286,0.965,0.686,1)`（贴图本身是灰的 `(44.6,44.6,44.6)`）⇒ 改传 `GenFieldTint`（与三颗新下拉同一个模板） |
| 5538 | `NineOut(...)` 新增可选 `Color? tint = null`（默认 null ⇒ 既有调用逐位不变） |

---

## 二、三个下拉逐件

### 1) 画质下拉（`Graphics Tab > Quality  Selector > Quality DropDown`）

**原版判据**

- 结构：`Image + TMP_Dropdown`；`Template` 出厂 `m_IsActive = 0`；`Label`（caption）fs18/base14/auto18–40/折行0/灰(0.67)。
- 选项：prefab 里 `m_Options` **是空表**，运行时由 `GraphicsTab__OnSetup.c:44-114` 用
  `Enum.GetValues(EverguildQualitySetting)` + `I2 GetTermTranslation("Settings/Graphics/" + 枚举名)` 填。
  枚举（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/EverguildQualitySetting.cs`）：
  `VeryLow(0) / Low(1) / Medium(2) / High(3) / Ultra(4)`；词条在官方本地化表里逐条命中
  （`Settings/Graphics/{VeryLow,Low,Medium,High,Ultra}`）。
- 行为：`GraphicsTab__GraphicsQualityDropdownChange.c` = `Enum.Parse(选项文本)` → 写 `GameStaticData.graphicsQuality`(+0x120)
  + 置脏 → `ConfigureSuperSamplingVisibility()`（`:58`）。
- 模板几何（`menu_dump` 实读）：`Template` 屏幕 `[592.37,341.46]–[948.47,858.03]`
  ⇒ 设计 `[551.52,319.40]–[947.18,893.36]`（= 框左、框右−5、框心+22、高 573.96）。

**我们的实现**

- 选项 = **`QualitySettings.names`（本工程 2 档：`Mobile` / `PC`）**；当前值 = `QualitySettings.GetQualityLevel()`；
  选中 ⇒ `ApplyQuality(i)`（`SetQualityLevel` + `SuperSampling.Apply()`）→ `RebuildGfxRows()`（= 原版 `:58` 的
  `ConfigureSuperSamplingVisibility`）→ `_flash` 出声。
- 🔴 **这是一处与原版的差，不是「读不到」**：原版 5 档，我们引擎只有 2 档；`资料/普查产出_1007/A170_设置窗滚动视图.md:246`
  已查定「`Mobile/PC` 与 `VeryLow…Ultra` **没有对应关系**」⇒ 硬编 5 项 = 自己发明一套画质系统
  （A176 之后也只把 `PC` 当「非移动」**那一层门**，不是当 Ultra）。⇒ 本笔列**引擎真有的两档**，
  `点哪一档都真生效`，⛔ 不编 5 个假选项。**待办**：真做 5 档画质系统后再换成原版五档。

### 2) `Text In Hand  Selector`（`Graphics Tab` 第二行）

**原版判据**

- 结构：`Graphics Tab > Content > Text In Hand  Selector`（注意原版节点名里是**两个空格**）
  > `Text in Hand DropDown`（`Image + TMP_Dropdown`，400.666×59.3985）+ `Template`；
  右侧说明字节点名**也叫** `Quality selector text`（`Localize.mTerm = Settings/Graphics/SelectTextOfCardsInHand`）。
- 几何：屏幕 `[592.37,369.09]–[1343.73,422.55]` ⇒ 设计 `[551.52,350.10]–[1386.37,409.50]`
  （行顶 = 画质行底 327.10 + VLG spacing **23.0**）。
  下拉框右沿 = 设计 **952.18/952.19**；caption `[561.52,357.10]–[942.18,403.50]`（fs18/base14/auto18–40/折行0/**灰 (0.67)**）；
  箭头 20×20 @ `[927.19,369.79]`；说明字 `[978.86,350.10]–[1386.37,409.50]`（fs42/base36/auto29–42/折行1）。
- 选项：`CardTextsInHandOptions`（`Default=0 / OnlyDescription=1 / OnlyName=2`），文案 =
  `Settings/CardTextInHand/{Default,OnlyDescription,OnlyName}` → `Full text / Only description / Only name`
  （官方本地化表逐条命中）。改动点 = `GraphicsTab__CardsTextInHandDropdownChange`（写 `GameStaticData+0x140` 与 `+0x144`）。
- 🔴 **运行时到底显不显示（本笔查清）**：`GraphicsTab` 的序列化字段序与偏移一一对应
  （`0x30 smallScreenToggle` · `0x38 hiFPS` · `0x40 superSampling` · `0x48 extendedCompatibility` ·
  `0x50 autoZoom` · `0x58 qualityDropdown` · **`0x60 cardsTextInHandDropdown`** · `0x68 vsync` · `0x70 fpsLimit`）；
  `GraphicsTab__OnSetup` 尾部链式 `SetActive` 只碰 **0x38(关) / 0x48(关) / 0x68(开) / 0x70 的父(开)**，
  **没有一处碰 0x60 或它的父** ⇒ **运行时显示**（prefab 出厂也是 `m_IsActive = 1`）。
  ⇒ 任务卡里记的「字段对不上号」= 当时没把字段序按偏移排开；现在排开了。

**我们的实现**：整行照原版建（节点名用单空格 `Text In Hand Selector` —— 与既有 `Quality Selector` 的处理一致，
原版双空格是导出名的写法）；选项 3 项、当前值 = `TextInHand.Value`（`PlayerPrefs`，出厂 0）；
选中 ⇒ 存值 + **如实出声**。

⚠️ **如实记**：我们工程里**还没有消费者**（`CardTextsController.ToggleSimplifiedView` 那一支手牌卡面没接）
⇒ 选完**画面上不会变**，日志里明说。

### 3) `WindowMode Selector`（`Media Tab > Visual Settings`）

**原版判据**

- 结构：`Media Tab > Visual Settings`（VLG，顶 = `Audio Settings` 底 **630.947**）
  > `WindowMode Selector` > `Dropdown`（`Image + TMP_Dropdown`，400.666×59.3985）+ `Template`；右侧 `Label`。
- 几何：组 `[596.52,630.94]–[1485.60,929.50]`；行 `[596.52,630.94]–[1431.37,690.34]`；
  下拉框右沿 **997.19**；caption `[606.52,637.94]–[987.19,684.34]`（fs18/base14/auto18–40/折行0/**白 (1,1,1,1)**）；
  箭头 @ `[972.19,650.64]`；右侧 `Label` `[1023.86,630.94]–[1431.37,690.34]`
  （fs42/base36/**auto 1–42**/折行1；`Localize.mTerm = Settings/Media/WindowMode`）。
- 选项：`MediaTab__SetupVideoSettings.c:70-92` = `AddOptions([GetTranslation(…Fullscreen), GetTranslation(…Windowed)])`
  → `Settings/Media/WindowMode/Fullscreen` = `Full screen`、`/Windowed` = `Windowed`；
  顺序由 `MediaTab__OnWindowModeChanged.c:13` `SetFullScreen(param_2 == 0)` 反证：**0 全屏 / 1 窗口化**；
  默认值 = `GameStaticData` 里那个「非全屏」bool（prefab `m_Value = 0`，运行时 `SetValueWithoutNotify` 覆盖）。
- 显隐：`Media Tab` 根的 `PlatformBasedComponents` = `android(0)→Visual Settings enable 0`、
  `iOS(10)→0`、**`PC(20)→1`** ⇒ **PC 档显示**。

**我们的实现**：整行照原版建（含 `Visual Settings` 包一层，与父链一致）；选项 2 项；
当前值 = 运行时 `UnityEngine.Screen.fullScreen`（0 全屏 / 1 窗口化）；
选中 ⇒ `Screen.fullScreen = (i == 0)`（= 原版 `ResolutionManager.SetFullScreen`）。
自检注入点：`SettingsWindow.FullScreenGetter` / `FullScreenSetter`（同 `QualitySetterOverride` 那一族的形状）。

⚠️ **不落盘**：原版把值存在玩家存档（`noFullScreen`）并在启动时应用；我们的显示值 = **运行时真值**，
选了就当场生效。⛔ 不做「存了但不应用」那种假持久化。

---

## 三、没做的 / 没查清的

1. **画质选项只有 2 项**（引擎真档），不是原版 5 项 —— 原因与出处见 §二·1；属「之后完全复刻」那一档（要先有 5 档画质系统）。
2. **`Text In Hand` 的值没有消费者** —— 下拉能选、值能存，但手牌卡面不跟着变（已出声，不静默）。
3. **下拉列表的「超出画布翻转」没做**（`TMP_Dropdown.Show()` 的 `FlipLayoutOnAxis`）：
   三颗列表在 1920×1080 内都装得下（最靠下的 WindowMode 列表底 = 设计 765.2 < 966），**不触发**；
   将来若换极小分辨率/极端加高布局，需要补。
4. **淡入不做**（同 A862：本工程 `ImageQuad`/`Label` 没有 alpha 动画口、批处理也没有帧循环）。
5. **语言那颗列表的面板高度**：原版 `Show()` 会把面板缩到内容高（12 项 = 491.30），
   既有实现画的是 573.96 —— **已知偏离，本笔不碰**（自检 `Editor/SettingsScene.cs:1593` 把那个矩形钉死了）。
   新三颗**实现的是原版行为**（面板 = `min(573.96, n×40.8707 + 0.8519)`）。
6. **7 条原版词条键不在 `Core/Loc.cs`**（该文件不在本件白名单）：
   `Settings/Media/WindowMode` · `…/Fullscreen` · `…/Windowed` ·
   `Settings/CardTextInHand/{Default,OnlyDescription,OnlyName}` · `Settings/Graphics/SelectTextOfCardsInHand`。
   现走 `UiTerm`：缺键 → **原版英文原文** + 一次 `warning`，⛔ 不印键名。补键后会自动切到中文。

---

## 四、typecheck

命令（按简报指定）：

```
TMPDIR=/tmp/wf_e_settings bash d:/4/Unity/工具/typecheck.sh
```

结果（每轮改完都跑，最后一轮）：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（中途一次报 9 条 `_ddQuality/_ddWindowMode/_ddTextInHand 未定义` —— 是漏声明三个字段，补上后归零；
最后两次都是 0/0。）

---

## 五、顺手发现（⛔ 我一个都没改）

1. 🔴 **`Editor/SettingsScene.cs:1692` 与 `:2634` 会红**：两处 `Click(FindChild(root,"Graphics Tab"), "QualityHit")`
   之后断言「画质切到下一档」。真下拉建成后，那一下**只开列表、不换档** ⇒ 需要改成
   「点 `QualityHit` → `Click(win.DropdownRowHit(SettingsWindow.DdQuality, 目标行号))`」。
   第一处还要把 `qWant = (q0+1)%names.Length` 改成「点另一档」的写法。
   （新加的自检口见 §一改动清单：`DropdownRowHit` / `DropdownRowCount` / `DropdownOpen` …）
2. **`Editor/SettingsScene.cs:2627` 的注释**仍写「点画质行 ⇒ `CycleQuality` → …」—— `CycleQuality` 已删，
   注释该改成 `ChooseQualityOption`。
3. **`Core/Loc.cs:2366` 的注释**里也还挂着 `CycleQuality`（只是注释，不影响编译）。
4. **语言下拉面板高度偏离**（§三·5）：`SettingsScene.cs:1593` 的期望值把 573.96 钉死了；
   若哪天要修成原版的「缩到内容高 491.30」，那条断言要同步改。
5. **`Media Tab` 的 `Note`（我们自己的说明行）换位置了**：原来在 `[596.52,650.95]–[1280.71,690.95]`
   —— 那个位置正是原版 `WindowMode Selector` 的行矩形，必须让开。现在挪到 `[596.52,706.34]–[1280.71,746.34]`
   （`Visual Settings` 下方，原版那里是空的）。**位置是我们挑的**，不冒充原版。
   自检按名字 `FindChild(Media Tab, "Note")` 找、不按矩形 ⇒ 不受影响（已核 `SettingsScene.cs:3929`）。
6. **新加的字号都在既有「全窗字号扫描」的允许集里**（`SettingsScene.cs:4295` 的
   `{49.5, 39.6, 36, 33.3, 32.4, 31.5, 30.6, 23.4, 16.2, 12.6, 10.8}`）：
   caption fs18/base14 → **12.6**、行标签 fs42/base36 → **32.4**、列表行 fs30/base14 → **12.6**。
7. **`AccountMissingTermKeys` 现在会多出 7 条**（§三·6 那批键）—— 已在它的 doc 里就地写明「这是预期、补键后应变空」。
8. **`Editor/SettingsScene.cs:1720` 的日志串**仍写「原版那几行里 `Text In Hand Selector` / `Hi FPS` /
   `Android extra compatibility` 运行时**都不在**」—— 前一件**已查清是在的**（§二·2），后两件仍然成立；
   该句要拆开写（只是日志，不影响断言）。
