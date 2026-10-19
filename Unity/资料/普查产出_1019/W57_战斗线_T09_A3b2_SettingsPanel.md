# W57 · 战斗线 `T09` 步骤 A3-b2 —— `SettingsPanel` 输入切 uGUI

> **日期** 2026-10-20（第五十七会话）· **代理** `T09-A3b2`（执行）
> **判据入口** `资料/普查产出_1019/W57_战斗线_T09迁移计划.md` §2 步骤 A3-b（`:178`）
> **前序** `资料/普查产出_1019/W57_战斗线_T09_A3b_WfSlider.md`（滑块那一半）
> **白名单** = `Unity/MyGame/Assets/CardPresentation/Battle/SettingsPanel.cs`（**唯一改动文件**，`git diff --numstat` = **+452 / −154**）
> **本波口径 = 只做不验**：未跑 Unity / 未起 player / 未跑 `*.Run` / 未跑 `_run_8_checks.sh`；只跑秒级 typecheck。
> 未动 git、未改正本、未删文件、**未改** `Editor/**` / `BattleDriver` / `HudCanvas` / `WfSlider` / `HudPiece` / `BattleHudButton`。

---

## 〇、一句话

本面板的**输入**从「驱动层每帧拿世界点做几何命中」切成 **uGUI 射线**：
五颗钮 / 难度行 / 语言框 / 每一条语言行 / 两颗开关行 = `BattleHudButton`（`IPointerClickHandler`）；
三根滑块 = **本件不再喂**（`WfSlider` 自己的 `EventTrigger`，A3-b 上一批）；
全屏暗底 = **原版 `Dark Shade`**（`raycastTarget=1`，点了不关窗）；语言列表开着时另有 **TMP `CreateBlocker` 的等价物**。
**动作 / 状态 / 开关窗 / 回调语义逐条照旧**（动作体与 `BattleDriver.SettingsClickAt` 的每一支逐字对应）。

---

## 一、逐件对照表（原版判据 → 我们的落点）

### 1·1 五颗钮（+ 我们自己的难度行）

| 件 | 原版判据（实读） | 改前输入路径 | 改后输入路径 | 动作（逐字照旧） |
|---|---|---|---|---|
| `Resign Button` | `EverguildButton`（uGUI `Selectable`）；rect 300×90、t=2 SpriteSwap | `BattleDriver.SettingsClickAt` → `HitResign` 几何 | `_btnResign` = `BattleHudButton`（rect 300×90，**padding 0**） | `Hide()` → `OnResign()`（= `Forfeit`） |
| `SkipTutorial Button` | 与 Resign 逐字段同形（uGUI `Selectable`）、300×90 | 同上 → `HitSkipTutorial` | `_btnSkip`（rect 300×90，padding 0） | `Hide()` → `OnSkipTutorial()`（⚠️ **新回调口，要 D 接线**，见 §三·1） |
| `Generic Close Button` | 圆底 `MB_4346` **`m_RaycastTarget=0`**；叉图 `MB_5056` **=1** + `m_RaycastPadding (−20,−20,−20,−20)` ⇒ 命中 96.37×94.50 | 同上 → `HitClose`（圆底 ∪ 叉图原版框） | `_btnClose` 挂**叉图**那颗，`raycastPadding` 由 `AttachClick` **现算**成原版 96.37×94.50（我们的实绘 31.5 高 ⇒ pad ≈ −32.4/−31.5） | `Hide()` |
| `Auto Zoom Toggle` | `EverguildToggle`；命中 = ① 勾选框 Image（74.0616×57.6656）② TMP（229.291×75.641），**中间 4.94px 缝照留** | 同上 → `HitAutoZoom`（两块） | ① `_btnAutoZoom` 挂 `_azBox` + padding 补到 74.0616×57.6656；② `_azLabelHit` = **全透明 Image** 盖原版文字那一格（我们的 `Label` 出厂不吃射线）、单独一颗 `BattleHudButton` | `ToggleAutoZoomFromPanel()` |
| `ChatToggle` | 命中 = ① `Background` Image（180.76×56.80）② `'Mute opponent'` TMP（346.20×53.00） | 同上 → `HitMuteChat`（两块） | ① `_btnMuteChat` 挂 **OFF 那半**（`_muteOffBg` 恒 active；ON 那半 `raycastTarget=false` ⇒ 射线穿过去）+ padding 补到原版矩形；② `_muteLabelHit` 透明片盖文字那一格 | `ToggleMuteChatFromPanel()` |
| `AI Difficulty`（**我们加的**，原版无此入口） | —— | 同上 → `HitDifficulty`（实绘矩形） | `_btnDifficulty` 挂 `_diffBtn`（rect = 实绘矩形，padding 0） | `OnCycleDifficulty()`（不关窗） |

**符号口径（本波最容易翻车的一条，已踩过一次）**：uGUI `raycastPadding` **正值内缩、负值外扩**
（判据 = `Shell/MenuDraw.PaddedRect:421` 的「全工程唯一一份」+ `WfSlider.SetupUguiInput` 手柄 `−25` 外扩）。
`AttachClick` 的补法是 `pad = (我们的 rect − 原版 rect) / 2` ⇒ 我们的件画得小 ⇒ **负值 = 外扩**。
⛔ 写反成 `(原版 − 我们)` 会把命中框**内缩**，画面完全正常、只有点不到（本波自查时抓到过一次，已改）。

### 1·2 三根音量滑块

| 环节 | 改前 | 改后 |
|---|---|---|
| 按下 / 拖动 | 宿主 `SettingsPanel.PointerFrame` 每帧：`WfSlider.Contains(world)` → `SetFromPointer(world)`，自己维护 `_dragSlider` | **整段删除**（`_dragSlider` / `UpdateSliderHover` / `UpdateSliderPress` 三处一起删） |
| 下落点 | 宿主喂 `SetHover` / `SetPressed` | `WfSlider` 自己的 `EventTrigger`（`PointerDown/Drag/PointerUp/Enter/Exit`，A3-b 上一批） |
| 本件还剩什么 | —— | 只保留 `ResetSliderFeedback()`（**状态复位**：关窗 / 开列表时清悬停+按下）与 `SetVisible` |

**双触发排除**：宿主通向 `WfSlider.SetFromPointer` 的调用点**已清零**（全文件只剩注释）
⇒ 同一根滑块只有 **一条**输入路径。A3-b 报告里那个「两路同值、误差 2e-6」的**过渡态已结束**。
另外 `UpdateSliderHover/Press` 一并删掉 ⇒ 不会再出现「uGUI 事件写一套、宿主每帧又写回另一套」的抢状态。

### 1·3 语言下拉

| 件 | 原版判据（实读） | 改前 | 改后 |
|---|---|---|---|
| 框 `LanguagesDropdown` | Image `m_RaycastTarget=1`、padding 0、色 (0.1216,0.9725,0.5373)、挂 `TMP_Dropdown`（`MB_5260`） | `PointerFrame` → `LangPointerFrame` 手搓「按下记起点 / 抬起判命中」 | `_btnLangField` = `BattleHudButton` → `ToggleLangList()` |
| 12 行 `Item Background` | Image `m_RaycastTarget=1`、padding 0、色 (0,0.8314,0.5255)；`Item` 上挂 `Toggle`（`MB_4939`） | 同上（`_langRowArmed` 边沿） | 每行一颗 `BattleHudButton` → `ChooseLanguage(i)`（内部第一句 `HideLangList()` = 原版 `OnSelectItem` 末尾 `Hide()`） |
| 行外 / 空白 | TMP `CreateBlocker`：全屏 `Image(Color.clear)` + `Button.onClick → Hide` | `HitLangBlank` 几何 | `_langBlocker`（见 §1·4）→ `HideLangList()` |
| 「按下行、抬到别处不选」 | uGUI `pointerPress` 语义 | 手搓 `_langRowArmed` | **`EventSystem` 天然给出**（click 只发给按下的那一件） |
| 悬停高亮 | `Toggle.m_SpriteState.m_HighlightedSprite = 40K_dropdown_item_hover` | `RefreshLangHover(world)` 每帧几何 | **原样保留**（只换贴图、不派发动作；见 §三·3） |
| ESC 先收列表 | `TMP_Dropdown.OnCancel → Hide()` | `PointerFrame → EscDown()` | **原样保留**（未动） |

### 1·4 全屏 Blocker（两件，对应原版两层）

| 层 | 原版判据（`bundle_scenes_scenes_battlearena1` 亲读） | 我们的落点 | 点击行为 |
|---|---|---|---|
| **暗底**（模态层） | `GameObject/Dark Shade.json`：`RectTransform_3451` = 3963.53×2692.97 中心锚；`MonoBehaviour_4739` = Image **`m_Color (0,0,0,0.612)`** · **`m_RaycastTarget = 1`** · sprite = null；`MonoBehaviour_4927` = **`BackgroundCloseButton`**（脚本 PathID `-7656576091067567576`），序列化 **`window` PPtr = null**、**`onClick.m_PersistentCalls.m_Calls = []`** ⇒ 反编译 `BackgroundCloseButton__…OnPointerClick.c`：window 空 ⇒ 不关窗；onClick 空 ⇒ 无事发生 | 现有 `_shade`（本来就是全屏 `(0,0,0,0.62)`、`FillParent`）**置 `raycastTarget=true`** + 挂 `BattleHudButton`（`_btnShade`） | **不关窗**（照原版）。列表开着时兜底 `HideLangList()` |
| **列表 Blocker** | 本机 uGUI 源码 `com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Dropdown.cs:1009-1074` `CreateBlocker`：`"Blocker"` → 铺满**根 canvas** → `Image(Color.clear)` → **`Button.onClick → Hide`**（+ `CanvasGroup.ignoreParentGroups`）；排序 = dropdown 画布 order − 1 ⇒ 压在**列表以外的一切**之上 | `_langBlocker`（**首次开列表才建**，全透明 Image，尺寸**现读 `_cvRt.rect.size`** 铺满整块画布），兄弟序 `LangBlockerZ = −0.55`（面板各件 z ∈ [−0.51,−0.43] 之下、语言框开列表时 −0.60 之上） | `HideLangList()`（= 原版 `Blocker.onClick`） |

**「点面板空白不穿透到背后 HUD 钮」**：`_shade` 现在整屏吃射线、且在 `PopUps` 层（高于 HUD 根画布）
⇒ HUD 那颗 uGUI 钮（A2 已置射线靶）收不到。列表开着时由 `_langBlocker` 接管整屏。
**「列表开着时滑块/五颗钮不许响应」**：`_langBlocker`（全屏、z 在面板各件之上）盖住它们
—— 等价于原版那颗 `Blocker`。

---

## 二、改动清单（`SettingsPanel.cs`，+452 / −154，**LF 2733 / CRLF 0**）

### 2·1 新增
- `public Action OnSkipTutorial;`（**唯一新回调口** —— 见 §三·1）。
- 字段：`_btnResign/_btnSkip/_btnDifficulty/_btnClose/_btnAutoZoom/_btnMuteChat/_btnLangField/_btnBlocker/_btnShade` ·
  `List<BattleHudButton> _btnLangRows` · `_azLabelHit` / `_muteLabelHit`（透明命中片）· `_langBlocker` + `LangBlockerZ = −0.55`。
- `static BattleHudButton AttachClick(HudPiece, Action, string node, float hitW = −1, float hitH = −1)`
  —— 置射线靶 + 可选「padding 补成原版矩形」。挂不上**逐条出声**。
- 动作：`OnResignClicked / OnSkipTutorialClicked / OnDifficultyClicked / OnCloseClicked / OnAutoZoomClicked /
  OnMuteChatClicked / OnLangFieldClicked / OnShadeClicked / OnLangBlockerClicked`（与 `SettingsClickAt` 逐支对应）。
- `EnsureLangBlocker()` / `ShowLangBlocker()`。
- **Editor-only 探针（整块 `#if UNITY_EDITOR`）**：`ClickCountForTest(node)` · `ClickForTest(node)` ·
  `ResetClickCountForTest(node)` · `LangRowClickCountForTest(i)` · `LangRowClickForTest(i)` ·
  `ButtonRaycastTargetForTest(node)` · `LangRowRaycastTargetForTest(i)` · `ShadeRaycastTargetForTest` ·
  `LangBlockerRaycastTargetForTest` · `LangBlockerPieceForTest` · `CloseRaycastPaddingForTest`。
  `node` 取值：`Resign Button` / `SkipTutorial Button` / `AI Difficulty` / `Generic Close Button` /
  `Auto Zoom Toggle` / `ChatToggle` / `Language Selector` / `Blocker` / `Dark Shade`。
- 挂点：`Build` 里 6 处、`BuildLanguageRow` 1 处、`BuildLangItem` 1 处、`BuildMuteChatRow` 2 处、`ShowLangList` 1 处。

### 2·2 删除（手搓输入路径，全部清零）
- `PointerFrame` 里的滑块段（`_dragSlider` 判据 + `foreach Sliders.Contains`）、`UpdateSliderHover`、`UpdateSliderPress`、字段 `_dragSlider`。
- `LangPointerFrame` + `_wasDown` / `_langArmed` / `_langRowArmed` / `_langBlankArmed`、`HitLangBlank`。
- `SetActive(false)` 里那三句手搓状态清空（滑块状态归 `SetVisible` / `ResetSliderFeedback`）。

### 2·3 `PointerFrame` 语义变更（**必读**）
- **改前**：返回「滑块/语言那一行接住了这一下」。
- **改后**：`Visible` 时**对每一次 `down` 都返回 true**（= 模态接管）。
  唯一用途 = 让 `BattleDriver.HandleSettings` 置 `_pressCaptured`，从而**吞掉同一按的松手沿**。
- ⛔ 不许改回「只有点在某个件上才 true」—— 那会让 uGUI 点击与 `SettingsClickAt` **各做一次**（`T09` §四 R1）。

---

## 三、双触发排除（灭自证）

### 3·1 五颗钮 / 语言 / Blocker

链路（现读行号）：

1. `BattleDriver.cs:3890` `bool captured = _settingsPanel.PointerFrame(WorldPointer(), PointerHeld());`
2. `BattleDriver.cs:3891` `if (captured) _pressCaptured = true;`
3. `BattleDriver.cs:3896` `if (ReleasedThisFrame()) SettingsClickAt(WorldPointer());`
4. `ReleasedThisFrame()`（`~:21690`）只在 `_upEdge` 为真时成立；`_upEdge`（`~:21670`）
   = `!held && _held && !_pressCaptured && !_swallowNextRelease`。

⇒ 面板开着时，**按下那一帧** `PointerFrame` 恒 true ⇒ `_pressCaptured = true` ⇒ **松手那一帧 `_upEdge` 恒 false**
⇒ `SettingsClickAt` **在产线上不可达**（它只剩自检直调那几条）。动作只由 `BattleHudButton.OnPointerClick` 派发一次。

**残余边界（如实记，非本波引入）**：若**按下那一帧** `HandleSettings` 被更早的早退分支挡掉、
而**松手那一帧**没被挡（现有分支里只有 `TickCardWinPointerExit` 是这种「单帧真」形状），
`SettingsClickAt` 仍可能被调用一次。这些分支全是「另一扇窗开着」的模态态；
本波未逐一穷举 17 条分支 × 两帧的组合（不跑自检也验不了）—— 记为 §四·2。

### 3·2 滑块
宿主到 `SetFromPointer` 的调用点 = **0**（全文件只剩注释）⇒ 单一路径。`WfSlider` 的 `1e-4` 阈值仍在其内部。

### 3·3 语言
`LangPointerFrame` / `HitLangBlank` / `_langRowArmed` 全删 ⇒ 不存在「手搓边沿 + uGUI click」两套。
`RefreshLangHover` 保留为**纯视觉**（只 `SetTexture`，不调任何动作方法）。

---

## 四、没查清的部分（⛔ 不猜）

1. **`OnSkipTutorial` 必须由 D 接线（本波唯一的「行为缺口」）**：
   `BattleDriver.SkipTutorialFromSettings` 是 `private`，而 `BattleDriver.cs` 不在本件白名单。
   本件已暴露 `public Action OnSkipTutorial`，并在未接线时 `LogWarning` 说清「这一下没有效果」。
   **接法**（D 的一行）：`SettingsPanel.Create` 增加第三个回调并把 `SkipTutorialFromSettings` 传进去
   （或建完后 `_settingsPanel.OnSkipTutorial = SkipTutorialFromSettings;`）。
   ⛔ 别用 `ApplyTutorialSkipForTest()` 顶（它跳过 `TutorialOverlay.TrySkip` 那道闸，语义不同）。
2. **17 条早退分支 × 按下/松手两帧的组合未穷举**（§3·1 残余边界）。判据缺口 = 需要跑 `BattleScene.Run` 才验得到。
3. **悬停仍走几何**（`RefreshLangHover` / `UpdateButtonFeedback`）：它们**只改视觉**、不派发动作，
   本波有意不动。原版那颗行 `Toggle` 的 highlight 由 uGUI 事件给；我们是每帧比矩形。
   两者在当前几何下等价（行 rect 与 uGUI 可点 rect 同值），但**如果将来行的 rect 与可点 rect 分家，这里会不一致**。
4. **`_langBlocker` 的排序与原版的差异**：原版 `CreateBlocker` 给 blocker 建了**自己的 Canvas**
   （`overrideSorting`，order = dropdown − 1）；我们用**兄弟序**顶（`_langBlocker` 在 `_panel` 子树内、
   z 夹在面板件与语言框之间）。视觉/命中在本件几何下等价，但**不是逐位同构**（同 `ShowLangList` 里那条
   「原版 `Dropdown List` 是自己的 canvas」的既有如实记）。
5. **批处理里 `EventSystem`/`GraphicRaycaster` 能否真打中本面板的件**：本波不验。
   A3-b 报告 §三·1 已记同族风险（编辑模式下 `AddComponent` 不派 `OnEnable`）⇒ 第一次真跑
   `BattleScene.Run` 时先确认 `RaycastAll` 打得中 `_langField` / 五颗钮。
6. **`_shade` 的 `rect.size` 在非 16:9 下是否真铺满**：它走 `HudCanvas.FillParent`（既有），本波未改；
   `_langBlocker` 走 `_cvRt.rect.size` 现读，**没在 21:9 下实测**。

---

## 五、对 `Editor/BattleScene.cs` 的影响（T 那一批要改；本波**未改** Editor）

以下用例**断言的是已删除的手搓路径**，会红 / 变恒真（行号 = 2026-10-20 现读）：

| 行 | 现在断言什么 | 为什么会变 |
|---|---|---|
| `:12171` | `!sp.PointerFrame(AutoZoomBoxWorldPos, true)`（A445「不在 PointerFrame 里」） | `PointerFrame(down)` 现在恒 true（模态接管） |
| `:12321` | `sp.PointerFrame(lfp, true) == false` | 同上 |
| `:12324` | `sp.PointerFrame(lfp, false)` ⇒ 列表开 | 松手沿不再驱动语言（改由 uGUI click） |
| `:12506-12508` | 同上（开列表） | 同上 |
| `:12561-12566` | 按行 / 抬行 ⇒ 选语言 | 改由 `_btnLangRows` 的 uGUI click |
| `:12593-12596` | 按空白 / 抬空白 ⇒ 收列表 | 改由 `_langBlocker` / `_shade` 的 click |
| `:12938` | `Check(cap, …)`「滑块接住这一下」 | 会**恒真**（不再是判据）；应改打 `PressCapturedForTest` 或 uGUI 探针 |
| `:12941-12946` / `:12954-12956` | 靠 `PointerFrame` 拖滑块改值 | 不再改值（改由 `WfSlider` 的 EventTrigger） |
| `:12960` | 面板外的点**接不住** | 现在恒 true（模态接管） |
| `:13000-13008` | 用 `PointerFrame` 点滑区 1/4、右沿 | 同上 |

**仍然绿**（不需要改）：所有 `drv.SettingsClickAt(...)` 直调的用例（`:12041/12175/12185/12249/13056/23494/23504`）、
`HitResign/HitClose/HitAutoZoom/HitMuteChat/HitSkipTutorial/HitLanguage/HitLangRow` 的**几何**用例
（`:12039/12150/12153/12242/12245/24663/25229` 等）、ESC 用例（`:12603`，`EscDown()` 未动）。

建议的替身探针：`ClickForTest(node)` / `ClickCountForTest(node)` / `LangRowClickForTest(i)` /
`ButtonRaycastTargetForTest(node)` / `ShadeRaycastTargetForTest` / `LangBlockerRaycastTargetForTest`。

---

## 六、typecheck 结果

```bash
cd d:/4/Unity/MyGame
TMPDIR=/tmp/wf_t09a3b2 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 编译时**没有**其它代理的半成品源码报错（未出现「等 60 秒重跑」的情况）。
- `UNITY_EDITOR` 已在两份 rsp 里定义（现核 `/tmp/wf_t09a3b2/wf_csc.rsp` 与 `wf_csc_editor.rsp`）
  ⇒ Editor-only 探针块确实被编到了。

---

## 七、行尾 / 规模

```
git diff --numstat -- Unity/MyGame/Assets/CardPresentation/Battle/SettingsPanel.cs
452     154     Unity/MyGame/Assets/CardPresentation/Battle/SettingsPanel.cs
bytes 229616 · CRLF 0 · LF 2733      （改前 2435 行、纯 LF ⇒ 未翻行尾）
```
