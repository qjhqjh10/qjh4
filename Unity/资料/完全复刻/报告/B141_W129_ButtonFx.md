# B141 · W129 · P6 包 —— 六窗按钮 / 反馈 7 条落地报告（`B141-W34`）

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · 执行写手 `B141-W34`。
> 输入判据 = `判据/B141_W129_对账_卡组色彩.md` §二 **P6 包**（`5-03 · 5-06 · 5-07 · 5-08 · 5-14 · 5-17 · 5-18`）
> ＋ `报告/B138_审计汇总_问题清单.md` §5 对应行 ＋ 原版 prefab / SO（**全部现读**，行号与数值以今天盘上为准）。
> ⛔ 未跑 Unity · 未动 git · 未改 `d:/2` · 未动账主表 / 活状态表 / 所有权表。
> 写入 = 4 个白名单 `.cs` ＋ 新建 `Editor/B141_W34_Assert.cs` ＋ 本文件。

---

## 〇、一句话结论

| 编号 | 状态 | 落点 |
|---|---|---|
| **5-03** | ✅ 已做（四态绿 + 禁用色 + 色偏目标） | `Shell/GiveFeedbackPopup.cs` |
| **5-06** | ✅ 机制已做 + **2 个可证站点**已接线；其余 13 颗**判据在、落点不在本包** | `Shell/WindowButton.cs` · `ParentEventWindow.cs` · `GiveFeedbackPopup.cs` |
| **5-07** | ✅ 机制已做（`selectedArt` / `_selectedTex`）；**77 颗的落点全在别包**（本包 4 个文件里 0 颗） | `Shell/WindowButton.cs` |
| **5-08** | ✅ 已做（`DisabledKey` + α 两档 + setter 闸） | `Shell/WindowButton.cs` |
| **5-14** | ✅ 页签悬停色已做（内容页签 + 事件钮两处）；**「另 10 颗绿底钮」❓ 仍不闭合**（见 §5-14） | `Shell/ParentEventWindow.cs` |
| **5-17** | ✅ 已做（649 颗三元组**全扫**，补 3 条表项 + 说明哪 2 族必须逐颗显式） | `Shell/WindowButton.cs` |
| **5-18** | ✅ 已做（6 档色表 + 接到 `_Outline`；两个异常值原样不 clamp） | `Shell/BoosterPackOpenWindow.cs` |

**行数变化**（相对本写手开工那一刻，非 `HEAD`）：

| 文件 | 开工 | 现在 | Δ |
|---|---:|---:|---:|
| `Shell/WindowButton.cs` | 1176 | 1281 | **+105** |
| `Shell/GiveFeedbackPopup.cs` | 1216 | 1249 | **+33** |
| `Shell/ParentEventWindow.cs` | 2695 | 2734 | **+39** |
| `Shell/BoosterPackOpenWindow.cs` | 1830 | 1872 | **+42** |
| `Editor/B141_W34_Assert.cs` | —— | 新建 | +383 |

> ⚠️ **账会漂**：这 4 个文件都被 `账/*.tsv` 按 `文件:行号` 引用过。本写手**无权改账主表**
> ⇒ 请调度台在「没有 `.cs` 写手」时跑一次 `check_ledger_points --all-A --fix`。
> 新增符号（便于按符号名重定位）：`WindowButton.ColorMultiplier / DisabledKey / GrayOnDisableAlpha / DisabledKeyNow / SelectedTexForTest / MissingSelectedArt / SelectedMissingPrefix`
> · `ParentEventWindow.TabHoverKey / TabDisabledKey / WireTabHover`
> · `GiveFeedbackPopup.CheckNormal / CheckHover / CheckPressed / CheckSelected / CheckDisabled`
> · `BoosterPackOpenWindow.BoosterRarityColors / BoosterRarityColor`

**验证**：`TMPDIR=/tmp/wf_b141_w34 bash 工具/typecheck.sh`
—— 本写手 4 个文件 **0 错**（末次两轮：运行时 0/0、编辑器 0/0；中途出现过 `Core/CardView.cs` 的 2 条
`ApplyCardAaScale` 未定义 ⇒ **那是别的写手（5-21）正在写的文件**，与本包无关，随后自行消失）。
**断言 49 条**（A 组 25 · B 组 24），`Editor/B141_W34_Assert.cs`，`Run()` 零前提。

---

## 5-03 · Give Feedback 41 颗绿复选框完全没染色 → 已做

**原版判据**（`Assets/Give Feedback Popup menu.prefab`，41 颗 `Checkbox` / `Checkbox (n)` 逐颗实读、**逐值相同**）：
`EverguildToggle` · `m_Transition = 1`(ColorTint) · `m_TargetGraphic` = **子件 `Toggle` 的 Image**（`40K_dropdown_bg`，`m_Color` 白）
· `toggle.graphic` = 子件 `CheckMark`（**不在色偏名单里**）。

| 格 | 原版 | 改前（我们） | 改后 |
|---|---|---|---|
| `m_NormalColor` | (0.28627452, 0.9647059, 0.6862745, 1) | 白 (1,1,1,1) | `CheckNormal` 逐位同 |
| `m_HighlightedColor` | (0.29214135, 0.9528302, 0.6816804, 1) | 0.9607843 灰（`WindowButton` 缺省） | `CheckHover` |
| `m_PressedColor` | (0.30584726, 0.8207547, 0.61052626, 1) | 0.7843137 灰 | `CheckPressed` |
| `m_SelectedColor` | (0.2901961, 0.9529412, 0.68235296, 1) | 0.9607843 灰 | `CheckSelected` |
| `m_DisabledColor` | prefab 存 (0.78431374 ×3, **a 0.5019608**) | 无 | `CheckDisabled` = **(×3, a 1.0)** |
| `m_ColorMultiplier` | 1.0 | 1.0 | 1.0 |

🔴 **禁用色的 α 为什么是 1.0 而不是 prefab 里的 0.5019608**：这 41 颗 **`colorTintGreyOnDisable = 1` +
`colorTintGreyOnDisableAlpha = 1.0`**，而原版 `EverguildButton__Awake.c:45` / `__SetUpButtonColors.c:40` 在
`colorTintGreyOnDisable(+0x17A) && m_Transition(+0x50) == 1` 时会把 `m_DisabledColor.a` **重写成
`colorTintGreyOnDisableAlpha(+0x17C)`** 再 `set_colors`（ctor 里 `+0x17C = 0x3f800000` = 1.0）⇒ 运行时成品 α = 1.0。

**改法**：`BuildSingle`/`BuildMulti` 的复选框行 ——
`togQ` 接住 `Toggle` 那颗 quad，命中区（整行，= 原版 `Checkbox` 根）的 `WindowButton` 赋四态 + 禁用色，
再 `TintOn(togQ)`（勾 `CheckMark` **不染**，原版只染 `m_TargetGraphic`）。

---

## 5-06 · `m_Colors.m_ColorMultiplier`（15 颗 = 2.0） → 机制已做，2 站已接

**原版判据**：`bundle_menus_assets_all` 里 1276 颗 `Selectable` 的 `m_Colors.m_ColorMultiplier`
**现读分布 = {1.0: 1252, 2.0: 15}**。15 颗的节点名（`ExportedProject` 的 prefab YAML 逐颗解 `m_GameObject`）：

| # | prefab ▸ 节点 | `m_Transition` | 我们的落点 | 本波 |
|---:|---|---:|---|---|
| 1 | `DeckSelectionContinueButton ▸ Continue Button` | 1 | `PracticeModePopup` 的 `BattleHit` | ⛔ 落点不在白名单 |
| 2 | `Practice Mode Menu ▸ …/Continue Button` | 1 | 同上 | ⛔ 同上 |
| 3-4 | `RankedEventWindow ▸ …/Continue Button` ×2 | 1 | 该窗未建这颗（我们走 `To Battle Button` = `UI_Button_Mulligan` 那族） | ❓ |
| 5 | `RankedEventWindowV2 ▸ …/Continue Button` | 1 | 同上 | ❓ |
| 6 | `SkirmishModeEventWindow ▸ …/Continue Button` | 1 | 同上 | ❓ |
| 7 | `Generic Multi Card Display Combat ▸ Button` | 1 | `Core/MultiCardDisplay.cs` | ⛔ 不在白名单 |
| 8-9 | `Draft Mode Ongoing State ▸ Button` / `▸ Debug Battle Button Button` | 1 | `Debug…` 是调试件（不建） | ❓ |
| 10-11 | `Draft Mode Timed Mode Window` / `Draft Mode Menu Demo ▸ Debug Battle Button Button` | 1 | 同上 | ❓ |
| 12 | `Feedback Scoring Button`（根） | **0** | `GiveFeedbackPopup` 评分钮 | ✅ 已接 |
| 13-15 | `Two Sides Event Window ▸ Two Side Event Select Team A/B` · `Two Side Event Select Team`（根） | 1 | `ParentEventWindow.BuildTeamCard` | ✅ 已接 |

**机制**（`WindowButton`）：`public float ColorMultiplier = 1f;`，在 `ApplyTint` 里
`k = Clamp01(_k * ColorMultiplier)`（**先乘后夹**）。
🔴 **为什么要夹**：UGUI 是 `StartColorTween(tintColor * colors.colorMultiplier)`，落点是 `CanvasRenderer.SetColor`
= **Color32 顶点色**（逐通道 0..1 饱和）；而我们的 `ImageQuad.SetTint` 走 `material.color`，**不夹**
⇒ 不夹会让 `0.9608 × 2 = 1.9216` 这种值进材质（原版画面里根本不会出现）。

**两站的实测效果**：
- `Two Sides` 队伍卡（`trans=1`）：四态 ×2 后 **全部 ≥1 被夹成 1** ⇒ 悬停/按下**零明暗变化**；
  改前缺省 1.0 ⇒ 悬停把整张队伍卡压暗 4%（那是与原版不符的那一档）。
- `Feedback Scoring Button`（`trans=0`）：原版**四态一像素都不变**；我们乘 2 后同样被夹平 ⇒ 与 `trans=0` 等价
  （改前悬停会把这颗压暗 4%）。

**❓ 未闭合**：上表 1/2/7 三颗（`Practice` 的 `BattleHit`、`MultiCardDisplay` 的 `Button`）**落点不在本包白名单**；
3/5/6/8-11 那 6 颗**我们的树里没有对应节点**（Ranked/Skirmish 走 `To Battle`；`Debug…` 不建）
⇒ **交给调度台按文件重新派**（这三颗要先把「四态底座色」那一半一起做，即 `B131` A15；单加乘数没视觉）。

---

## 5-07 · `m_SpriteState.m_SelectedSprite`（77/1276 非空） → 机制已做，落点全在别包

**原版判据**：`bundle_menus_assets_all` 1276 颗里 **77 颗** `m_SelectedSprite` 非空（**71 颗 `trans=2` + 6 颗 `trans=0` 死值**）。
把 77 颗按 prefab 展开后（含嵌套实例重复计 = 79 处）**只用到 4 张图**：

| 选中图 | 出现的节点族（颗数） |
|---|---|
| `40K_settings_button_hover` | 设置/档案/排行榜/成就/草稿页签与 `Settings Menu Tab Toggle` · `Orange Tab Toggle` 一族（≈60） |
| `40K_dropdown_field_opened` | `GenericDropdown` · `Quality DropDown` / `LanguagesDropdown` / `Text in Hand DropDown` / `Privacy Dropdown`（≈9） |
| `40K_dropdown_item_selected` | `Item`（设置 4 · 社交 6 · 通用 1） |
| `40k_general_bt_yellow` | `Match Log ▸ ReplayButton / PinButton` · `Player Profile ▸ 同名两颗` · `Deck info ▸ Switch Deck Info Button` |
| `7c987b77…`（`CircleButton`） | Continue 那颗圆钮（`DeckSelectionContinueButton` / `Practice Mode Menu` / `Draft Mode *` / `Generic Multi Card Display Combat` / `RankedEventWindow ▸ Ranked Play Mode Button`） |
| `1d28baa3…`（`Generic Tab UI Button`） | 社交子菜单 4 颗 + 牌组选择弹窗 2 颗 |

**改法**（`WindowButton`）：
- 新增 `Texture _selectedTex`；`TexOf(BtnState.Selected) → _selectedTex ?? _normalTex`
  （缺省 null = 原版那 1199 颗，**逐位不变**）。
- `Bind / BindNine / BindSelf` 末尾加 `string selectedArt = null` —— **只逐颗显式给，⛔ 不按名字推**
  （上面 4 张图与常态图之间没有命名规律）。
- 新增 `MissingSelectedArt`（**只在「调用方显式给了名字却取不到」时记**；原版 1199 颗 null 不是缺口）+ 自检口 `SelectedTexForTest`。

**❗ 落点**：这张表里**没有一颗落在本包 4 个文件**（`GiveFeedbackPopup` 的复选框是 `EverguildToggle` 的
`onSprite` 那一类、不在 77 里；页签是 `40K_settings_button_hover` 但那是**悬停**图，见 `ParentEventWindow.TabHoverKey`）。
⇒ 机制已就绪，**接线请按 `活状态表` 派给** `Shell/SettingsWindow.cs`（设置/档案/成就页签 + 三个下拉）·
`Shell/PlayerProfileWindow.cs` · `Shell/ChatPanel.cs` · `Shell/LeaderboardWindow.cs` · `Shell/MatchLogRow.cs` ·
`Shell/DraftModeWindow.cs` · `Shell/DeckInfoPopup.cs` · `Shell/DeckSelectionPopup.cs`。

---

## 5-08 · 禁用色 / 禁用 α 四族全缺 → 已做

**原版判据**：1276 颗逐颗 `m_Colors.m_DisabledColor`，现读分布：

| 颜色 | 颗数（α 分布） |
|---|---|
| `(0.7843, 0.7843, 0.7843)` | **1208**（a=1.0 696 · a=0.5019608 504 · a=0.0 8） |
| `(0.8018868 ×3)`（青族，= A12） | **19**（a=1.0） |
| 纯白 | **49**（a=1.0） |

**α 的两条判据**（`EverguildButton__Awake.c:45` / `__SetUpButtonColors.c:40`）：
`colorTintGreyOnDisable(+0x17A) && m_Transition(+0x50) == 1` ⇒ α 被**重写成 `colorTintGreyOnDisableAlpha(+0x17C)`**（出厂 1.0）；
否则沿用 prefab 原值。**两档都真的存在**：`grey=1` 那一族（15 颗 `m_ColorMultiplier=2` 全 `α=1.0`）·
`grey=0 + trans=1 + a=0.5019608` **66 颗**（例：`Main Menu Settings Window ▸ Small Screen Size Toggle`，MB `-1083006033978469938`）。

**改法**（`WindowButton`）：
- `public Color DisabledKey = (0.7843137 ×3, a 0.5019608)`（= 最大那一族）+ `public float GrayOnDisableAlpha = 1f`；
- `KeyOf` 第一句 `if (!_interactable) return DisabledKeyNow;`，`DisabledKeyNow` 只在 `ColorTintGreyOnDisable` 时把 α 换成 `GrayOnDisableAlpha`；
- `Interactable` setter 的 `ApplyState()` 闸加 `|| tintOnHover`（**纯色偏那一族原来置灰时颜色完全不动**）；
- 页签的 `0.8018868` 那一族在 `ParentEventWindow.WireTabHover` 里给（= A12 的「青族」）。
- 语义边界（照 UGUI）：**只有 `ColorTint`（`tintOnHover == true`）那一档会乘禁用色**；
  `SpriteSwap` 的 630 颗与 `None` 的 141 颗**原版根本不走色偏**（`SetTarget` 的 `!tintOnHover` 早退就是这条闸）。

---

## 5-14 · 页签悬停色（+ 10 颗绿底钮） → 悬停色已做；10 颗 ❓

**原版判据**（`Assets/Parent Event Window.prefab ▸ Menu Navigation Panel Button`，`EverguildToggle` 逐字段）：

| 字段 | 原版 | 改前（我们） | 改后 |
|---|---|---|---|
| `m_Transition` | 1 (ColorTint) | 无悬停 | 已接 |
| `m_TargetGraphic` | **子件 `Icon` 的 Image** | —— | 色偏只打 `Icon` 那颗 quad |
| `m_HighlightedColor` | **(0.6084906, 0.94261944, 1, 1)** | 0.9607843 灰 | `TabHoverKey` |
| `m_DisabledColor` | (0.8018868 ×3, a 1) | 缺省 0.7843 | `TabDisabledKey` |
| N / PR / SEL | 白 / 0.78431374 / 0.9607843 | 缺省同值 | 不动 |

**改法**：新增 `static void WireTabHover(Transform tab, Transform hit)`（一份实现），
在**内容页签**（`Build()` 的循环）与**事件钮**（`AddEventTabButton`）两处各调一次；
`WireTabHover` 里 `tab.Find("Icon")` 取那颗 quad，有就 `TintOn(q)`、没有就不调
（运行期页签的图标是 liveops sprite、本地没有 ⇒ **不建** ⇒ 悬停只打在透明命中区 = 原版那张空 `Icon` 的同一结果）。

**❓ 仍不闭合：「另 10 颗绿底钮」**（`判据` §六 的同一子项）。本写手又搜了一遍
`ParentEventWindow` / `GachaEventWindow` / `RaidEventWindow` / `ExpansionPassEventWindow` /
`TwoSides*` 的 `BtnGreen` / `0.3686275` / `0.89411765` = **0 命中**；`Assets/Generic Multi Card Display Combat.prefab`
那一颗在**战斗侧**（`Core/MultiCardDisplay.cs`，不在本包）。⇒ **判据指原版 prefab 名、我们侧无落点**，
按铁律 2 **不猜**，留给调度台按 prebab 逐个反查后再派。

---

## 5-17 · 630 颗 SpriteSwap 未逐颗映射 → 已做（全扫 + 补 3 条表项）

**做法**：把 `ExportedProject` 里**全部 `m_Transition = 2`** 的 `Selectable` 的
`(m_TargetGraphic.m_Sprite, m_SpriteState.m_HighlightedSprite, m_PressedSprite)`
**三元组一次扫全**（`Sprite/*.asset.meta` 的 guid → 图名）—— 结论：

> **649 处实例 / 只有 21 种不同三元组**。逐条与本类「表 + `<常态图>_hover` / `_pressed` 后备」对账：

| 颗数 | 常态图 | 高亮图 | 按下图 | 我们的规则命中 |
|---:|---|---|---|---|
| 309 | `40K_button` | `40K_button_hover` | `40K_button_pressed` | ✅ 后备规律 |
| 125 | `UI_Button_Mulligan` | `UI_Button_Mulligan_hover` | `UI_Button_Mulligan_Pressed` | ✅ 表（大写 P） |
| 67 | `40k_general_bt_yellow` | `40k_general_bt_yellow_hover` | `40k_general_bt_yellow_pressed` | ✅ 表 + 后备 |
| 35 | `40K_button_square` | `40K_button_square_hover` | `40K_button_square_pressed` | ✅ 后备规律 |
| 23 | `40K_settings_button` | `40K_settings_button_selected` | `40K_settings_button_pressed` | ✅ 表 |
| **13** | `40K_settings_button_hover` | `40K_settings_button_selected` | `40K_settings_button_pressed` | ✅ **本波新补** |
| 12 | `40k_UI_bt_back_1` | `40k_UI_bt_back_hover` | `40k_UI_bt_back_pressed_1` | ✅ 后备规律 |
| **11** | `40K_dropdown_field_closed` | `40K_dropdown_field_opened` | `40K_dropdown_field_opened` | ✅ **本波新补按下图** |
| 11 | `40K_dropdown_item` | `40K_dropdown_item_hover` | `40K_dropdown_item_press` | ✅ 表（`_press`） |
| 9 | `40k_bt_close` | `40k_bt_close_hover` | `40k_bt_close_pressed` | ✅ 后备规律 |
| 8 | `UI_Button_Menu_Back` | `UI_Button_Menu_Back_Hover` | `UI_Button_Menu_Back_Pressed` | ✅ 表（大写） |
| 8 | `40k_UI_bt_play` | `40k_UI_bt_play_hover` | `40k_UI_bt_play_pressed` | ✅ 后备规律 |
| **5** | `40k_UI_bt_back` | **`40k_UI_bt_back_hover_back`** | `40k_UI_bt_back_pressed` | ⚠️ **必须逐站显式**（同一条常态图另有 12 颗用 `_hover`） |
| 3 | `40k_UI_bt_deck_change` | `40k_UI_bt_deck_change_hover` | `40k_UI_bt_deck_change_pressed` | ✅ 后备规律 |
| 3 | `40k_bt_eye` | `40k_bt_eye_hover` | `40k_bt_eye_pressed` | ✅ 后备规律 |
| **2** | `UI_Button_Organe_Square_Normal` | `UI_Button_Organe_Square_Hover` | `UI_Button_Organe_Square_Pressed` | ✅ **本波新补按下图** |
| 1 | `UI_Button_Round_background` | `40k_bt_close_hover` | `40k_bt_close_pressed` | ⚠️ 逐站显式（设置窗关闭钮已传） |
| 1 | `40K_settings_button` | **`40K_settings_button_hover`** | `40K_settings_button_pressed` | ⚠️ **必须逐站显式**（同一条常态图另有 23 颗用 `_selected`） |
| 1 | `40k_UI_bt_deck_1` | `40k_UI_bt_deck_hover_1` | `40k_UI_bt_deck_pressed_1` | ✅ 后备规律 |
| 1 | `UI_Army_Selection_Back` | `UI_Army_Selection_Back_Hover` | `_Pressed` | ❓ 我们未建这颗 |
| 1 | `40k_UI_bt_deck` | `40k_UI_bt_deck_hover` | **`40k_UI_bt_back_pressed`**（原版就是把 back 的按下图挂到 deck 上） | ⚠️ 逐站显式 |

**本波改动**（`WindowButton` 的两张表）：
- 新增 `40K_settings_button_hover → 40K_settings_button_selected`（13 颗）
- 新增 `UI_Button_Organe_Square_Normal → UI_Button_Organe_Square_Pressed`（2 颗；
  改前按下图缺 ⇒ 退回高亮图，**画面差一张**）
- 新增 `40K_dropdown_field_closed → 40K_dropdown_field_opened`（11 颗；改前退回高亮图 = 同一张，等价，但表里写清）

🔴 **两条不能进表**（写进注释）：`40k_UI_bt_back` 与 `40K_settings_button` 这**两条常态图在原版各有两个不同的高亮**
（`_hover` / `_hover_back`；`_selected` / `_hover`）⇒ 进表会打错另一族，**必须由调用点逐颗显式传 `hoverArt`**
（今天这两族都已显式传：`DailyRewardPopup.cs:686` · `DraftModeWindow.cs:2019` · `SettingsWindow` 页签）。

**❓ 仍开着**：`UI_Army_Selection_Back`（1 颗，我们没建）；
`GenericDropdown ▸ Item` 等下拉项**我们没建下拉弹层**（属别包）。

---

## 5-18 · 开包稀有度两套 SO 未实现 → 已做

**原版判据链（逐跳现读）**：
1. `Booster Pack Open Window.prefab` 的 5 颗 `CardInBoosterPack UI 1..5` 上 `cardRarityColors` → guid `5b6d184a…` = **`CardRarityColorsSO`**
   （`bundle_menus_assets_all/MonoBehaviour/CardRarityColorsSO.json`，pid `764426219195092032`，本包只被这 5 份引用）。
2. `CardInBoosterPack__Initialize.c:63`：`SupportMethods.GetCardRarityFromInt(card.rarity)` → `CardRarityColorsSO__GetColor(so, r)` →
   **`Card2DController.ChangeHighlightColor(color, 0, /*instant*/1, 0)`**。
3. `ChangeHighlightColor` 的 `param_3 == 0` 取 `this + 0x48` = 字段 `highlightAndShadowColorChanger`
   = 节点 **`Card Highlight And Shadow`** 的 `UIImageMaterialColorChanger`（prefab 实读：`image` → 那颗 Image）。
4. 它 `DoColorChange(..., propId = *(Card2DController + 0x84), ...)`；而 `Card2DController..ctor`
   里 `+0x84 = Shader.PropertyToID(<_Outline>)`（对照项：`ChangeShadowColor` / `ToggleCardShadow` 用的是 `+0x80` = `_ShadowColor`）
   ⇒ **落点 = SDF 那层的 `_Outline`**，我们的等价物 = **`CardView.SetOutline(color)`**。

**六档值**（SO 逐字，**⛔ 不 clamp**）：

| 档 | r | g | b | a |
|---|---:|---:|---:|---:|
| 0 None | 1 | 1 | 1 | 1 |
| 1 Common | 0.036712352 | 0.26070666 | 0.31132078 | **0** ← B11 异常值，原样 |
| 2 Rare | 0.07176104 | 0.56603777 | 0.056069776 | 0.66666669 |
| 3 Epic | 0.56470591 | 0.054901958 | 0.52589482 | 0.66666669 |
| 4 Legendary | **1.2821634** ← >1，原样 | 0.47049004 | 0 | 0.66666669 |
| 5 Special | 0.82352942 | 0.16078432 | 0 | 0.63921571 |

**改法**：`BoosterPackOpenWindow.BoosterRarityColors`（6 档）+ `BoosterRarityColor(int)`（越界 ⇒ 第 0 档），
在 `BuildSlot` 建完 `CardView` 之后 `v.SetOutline(BoosterRarityColor(RarityInt(Cards[i])))`。

**⚠️ 如实记的两处差异 / 未闭合**：
1. **补间**：原版 `instant = 1`（立刻落值），我们走 `CardView.SetOutline` 的 **0.2 s** 补间
   （= `CardHighlight.OutlineAnimTime`，本工程既有口径）⇒ 翻牌后 0.2 s 内由透明渐到该色，**终值一致**。
2. **❓ 档位口径**：`CardRarity` 枚举是 `None 0 · Common 1 · Rare 2 · Epic 3 · Legendary 4 · Special 5`，
   但本窗数据侧只有 `card.rarity` 整数（`0/1` 都算 common 那一档）；我们 `CardDef.Rarity` 是字符串、
   `RarityInt` 把 common 落成 **0** ⇒ **`colors[1]`（Common，α=0）这一档从我们的数据里走不到**。
   ⛔ 不拿猜测补映射，如实留在这里。

---

## 灭自证（改坏法 → 哪条红）

| 改坏法 | 变红的断言 |
|---|---|
| `CheckNormal` 改回 `Color.white` | `A1` + `B1` |
| 删 `cbWb.TintOn(togQ)` | `B2` |
| 把底色顺手写成绿（`Rect(..., CheckNormal, ...)`） | `B3` |
| `ApplyTint` 删 `Clamp01` | `A2` + `B5` |
| `ColorMultiplier` 缺省改成 2 | `A4c` + `B5b` |
| 删 `teamWb.ColorMultiplier = 2f;` / `scoreWb.ColorMultiplier = 2f;` | `B6` / `B6b` |
| `KeyOf` 的禁用档改回 `NormalKey` | `B7` |
| `DisabledKeyNow` 无条件写 α | `B8` |
| 去掉 `Interactable` setter 里「或上 `tintOnHover`」那一条 | `B8b` |
| `TexOf(Selected)` 改回无条件 `_normalTex` | `B9` |
| 删 `MissingSelectedArt` | `B10b` |
| 删任一处 `WireTabHover(...)` | `B12` |
| 稀有度表加 `Clamp01` 或把 r 改成 1.0 | `A6e` / `A6f` / `B14` |
| 删掉 `v.SetOutline(BoosterRarityColor(...))` | `B13` |
| 删 `hoverArt ?? HoverNameFor(art)` 的显式覆盖路 | `B15` |

---

## 顺手发现（只记，不改；⛔ 不在本包白名单内）

1. **`GiveFeedbackPopup` 的评分钮原版是 `m_Transition = 0`** ⇒ 悬停/按下**一个像素都不变**；
   我们改前是「色偏兜底」（悬停压暗 4%）。本波用 `ColorMultiplier = 2` 把四态夹平，
   **副作用上等于 `trans=0`**，但严格做法应是 `tintOnHover = false`（那是 P6 之外的表达）。
   ⚠️ 两者视觉等价（都是「四态不变」），但**语义上 `tintOnHover=false` 更准** —— 记在这里备查。
2. **`Feedback Scoring Button` 的 `m_DisabledColor`**：prefab 里是 `(0.78431374 ×3, a 0.5019608)` 且
   `colorTintGreyOnDisable = 1 / alpha 1.0` ⇒ 与 5-03 同一条「Awake 重写 α」的口径（本波未单独接线，`trans=0` 下是死值）。
3. **`Two Side Event Select Team A/B` 的 `m_NormalColor` 也是 `(0.3686275, 0.89411765, 0.5882353, 1)`**
   （= A16「另 10 颗绿底钮」之一）—— 我们队伍卡现在用的是 `UICornersGradient` 四角色 + 白 `Hit`，
   **没有把 `m_Colors` 那四态接上** ⇒ 这正是 5-14 的「10 颗绿底钮」子项的一半，**判据已在本报告里给出**（可复用）。
4. **`40K_settings_button_hover` 被原版当「常态图」用（13 颗）** —— 也就是说原版有 `常态图 = 我的悬停图` 的实例，
   这说明那 13 颗的「悬停态」其实靠 **选中态/`onSprite`** 表达，别把 5-07 与 5-17 混为一谈。
