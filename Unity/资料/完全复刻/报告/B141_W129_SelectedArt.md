# B141 · W129 · 5-07 —— 77 颗 `m_SpriteState.m_SelectedSprite` 接线报告（`B141-W44`）

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · 执行写手 `B141-W44`。
> 输入判据 = `报告/B141_W129_ButtonFx.md` §5-07（W34 的「节点族 → 选中图」表）· `报告/B138_审计汇总_问题清单.md` §5 的 5-07
> ＋ **本写手对原版 prefab 的第二次全量重扫**（2026-11-04 现读，见 §一；用来把「族」落成「逐个节点」）。
> 机制（`_selectedTex` / `Bind(..., selectedArt)` / `SelectedTexForTest` / `MissingSelectedArt`）由 `B141-W34` 做在
> `Shell/WindowButton.cs` —— **本写手一个字节没动那个文件**。
> ⛔ 未跑 Unity · 未动 git · 未改 `d:/2` · 未改账主表 / 活状态表 / 所有权表。
> 写入 = 8 个白名单 `.cs` ＋ 新建 `Editor/B141_W44_Assert.cs` ＋ 本文件。

---

## 〇、一句话结论

| 口径 | 数 |
|---|---:|
| 原版 `m_SpriteState.m_SelectedSprite` 非空的 prefab 实例（本次重扫） | **77 颗 / 79 处**（79 = 含嵌套 prefab 重复计；减 2 颗 `Debug Reactivate Event Window` 的调试页签 = 77） |
| 用到的**不同**选中图 | **6 张**（W34 报的 4 张切片名 ＋ `CircleButton` / `Generic Tab UI Button` 两个 guid，本报告已把 guid 反查成真名） |
| **本批接线（prefab 实例口径）** | **47 / 77** |
| 本批接线的**代码站点** | **15 个**（8 个文件） |
| 余下 **30 颗** | **判据在别的文件 / 我们没建那颗** —— 逐族见 §三（⛔ 不是「不做」） |
| `typecheck` | `TMPDIR=/tmp/wf_b141_w44 bash 工具/typecheck.sh` ⇒ 运行时 **0/0** · 编辑器 **0/0**（4 轮，末轮含断言文件） |
| 断言 | **41 条**（`Editor/B141_W44_Assert.cs`，`Run()` 零前提） |

**六张选中图（本报告已把 W34 表里那两串 guid 解成真名）**：

| 选中图（切片名） | guid | 颗数（79 处口径） | 我们这一侧能不能取到 |
|---|---|---:|---|
| `40K_settings_button_hover` | `b301d2f9e044ec84ca5aa5ac6e609cce` | 36 | ✅ `Art/ui_menu/` |
| `40K_dropdown_field_opened` | `6c66073c79553944781686192164bbb1` | 11 | ✅ `Art/ui_menu/` |
| `40K_dropdown_item_selected` | `9c5732feaf7e3b141a4c76f00cacfc0b` | 11 | ✅ `Art/ui_deck/` |
| `40K_tab_button_overwindow`（= W34 表里的 `1d28baa3…`「Generic Tab UI Button」） | `1d28baa3594c64345b44bddf02fd710d` | 8 | ✅ `Art/ui_deck/` |
| `40k_UI_bt_play`（= W34 表里的 `7c987b77…`「CircleButton」） | `7c987b776b4dcec48826ebf0203b5d9c` | 8 | ✅ `Art/ui/` |
| `40k_general_bt_yellow` | `33cd05c425d32274595c8c51b527dfef` | 5 | ✅ `Art/ui/` |

> ⚠️ **与 W34 表的一处措辞订正（铁律 5）**：W34 表把后两张写作「`7c987b77…`（`CircleButton`）」「`1d28baa3…`（`Generic Tab UI Button`）」。
> 真名（按 `Sprite/*.meta` 的 guid 反查）分别是 **`40k_UI_bt_play`** 与 **`40K_tab_button_overwindow`** ——
> `CircleButton` / `Generic Tab UI Button` 是**节点名**，不是图名。⛔ 别拿节点名去 `CardArt.MenuUi`。
> 另：W34 写「71 颗 `trans=2` + 6 颗 `trans=0`」，本次重扫逐颗核对 **79 处 = 71 颗 `trans=2` + 8 颗 `trans=0`**；
> 差额那 2 颗正是 `Debug Reactivate Event Window` 的两个页签（不建），而它们也是 `trans=0`
> ⇒ **77 颗里 `trans=0` 的仍是 6 颗**（牌组选择弹窗 2 + 社交子菜单 4），与 W34 一致。

---

## 一、判据：原版 77 颗的逐个清单（2026-11-04 重扫）

扫描法：`d:/2/tools/assetripper_out/ExportedProject/Assets/**/*.prefab` 里 `m_SelectedSprite` 非 `{fileID: 0}` 的
`Selectable` 实例 → 由 `m_GameObject` 反查节点名、由 `m_Father` 拼父链、由 sprite `guid` 反查切片名。
（同一份 prefab 被别的 prefab 嵌套引用时**两边都算**，与 W34 的「79 处」口径一致。）

| # | prefab | 节点（父链简写） | 选中图 | 我们这一侧 |
|---:|---|---|---|---|
| 1 | Main Menu Settings Window | `Menu Area/Mask Tabs buttons/Tab Buttons` ▸ **Account / General / Graphics / Media / Support**（5 颗） | `40K_settings_button_hover` | ✅ `SettingsWindow.cs:2008` |
| 2-5 | 〃 | `Tab Content/General Tab/Language Selector/LanguagesDropdown` ＋ `…/Template/Viewport/Content/Item` | `_field_opened` ＋ `_item_selected` | ✅ `SettingsWindow.cs:2331` · `:3734` |
| 6-9 | 〃 | `Graphics Tab/…/Quality DropDown` ＋ `Item` · `…/Text in Hand DropDown` ＋ `Item` | 同上两张 | ✅ `:4463` · `:3972` · `:4502` |
| 10-13 | 〃 | `Media Tab/…/WindowMode Selector/Dropdown` ＋ `Item` | 同上两张 | ✅ `:5621` · `:3972` |
| 14 | Player Profile Window | `Menu Area/Tab Buttons` ▸ **Profile Button / Avatar Button / Title Button / Battle Log Button / Trophies / Ranked**（6 颗） | `40K_settings_button_hover` | ✅ `PlayerProfileWindow.cs:415`（`Ranked` 按用户口径隐藏 ⇒ 实际生效 5 颗） |
| 15-19 | 〃 | `Trophies Tab/buttons/Achievement Type Toggle (0..4)`（5 颗） | `40K_settings_button_hover` | ❌ 在 `Shell/AchievementsMenu.cs`（W44 白名单外） |
| 20-21 | 〃 | `Battle Log Tab/…/Match Log` ▸ **PinButton / ReplayButton** | `40k_general_bt_yellow` | ✅ `MatchLogRow.cs:324` · `:342` |
| 22-23 | Deck Selection Popup with Tabs | `Alliance Header Buttons/Tab buttons` ▸ **Generic Tab UI Button / Generic Tab UI Button 1** | `40K_tab_button_overwindow`（**`trans=0` 死值**） | ✅ `DeckSelectionPopup.cs:774` |
| 24 | Deck info Popup | `Deck Options/Switch Deck Info Button`（五颗里只有这一颗） | `40k_general_bt_yellow` | ✅ `DeckInfoPopup.cs:1685` |
| 25-27 | Draft Mode Deck Info Panel | `Buttons` ▸ **Deck Info Toggle / Card List Toggle / Alliance Info Toggle** | `40K_settings_button_hover` | ✅ `DraftModeWindow.cs:1888` |
| 28-31 | Draft Mode Menu Demo | 3 颗嵌套（同上）＋ `Debug Battle Button Button/CircleButton` | `_hover` ×3 ＋ `40k_UI_bt_play` ×1 | ✅ 3（嵌套同一组）· ❌ 1（`Debug…` 原版强制隐藏、我们不建） |
| 32-33 | Draft Mode Ongoing State | `Battle Button Button/Button/CircleButton` ＋ `Debug Battle Button Button/CircleButton` | `40k_UI_bt_play` ×2 | ❌ 我们用 `UI_Button_Mulligan`（见 §三） |
| 34-37 | Draft Mode Timed Mode Window | 3 颗嵌套（同 #25-27）＋ `Debug Battle Button Button/CircleButton` | `_hover` ×3 ＋ `40k_UI_bt_play` ×1 | ✅ 3 · ❌ 1 |
| 38-39 | DraftLeaderboardPopup | `Tab Buttons` ▸ **Alliances / Armies** | `40K_settings_button_hover` | ✅ `LeaderboardWindow.cs:673`（`Kind = Draft`） |
| 40-41 | GameObject/GenericDropdown | 根 ＋ `Template/Viewport/Content/Item` | `_field_opened` ＋ `_item_selected` | ✅ 由设置窗 4 个下拉开的那一族覆盖（模板件本身不是独立 UI） |
| 42-43 | GameObject/Match Log | **PinButton / ReplayButton** | `40k_general_bt_yellow` | ✅ `MatchLogRow.cs`（两扇宿主共用这一份 builder） |
| 44 | Generic Multi Card Display Combat | `BattleContinueButton/CircleButton` | `40k_UI_bt_play` | ❌ 战斗侧 `Core/MultiCardDisplay.cs` |
| 45 | ChatPanel | `Holder/Chat/Tab Buttons/Orange Tab Toggle` | `40K_settings_button_hover` | ✅ `ChatPanel.cs:495` |
| 46 | Orange Tab Toggle（模板 prefab） | 根 | `40K_settings_button_hover` | ✅ 同一族（聊天窗两个频道键） |
| 47 | Settings Menu Tab Toggle（模板 prefab） | 根 | `40K_settings_button_hover` | ✅ 同一族（设置窗 5 颗页签） |
| 48-50 | RankedSkirmishLeaderboardPopup | `Tab Buttons` ▸ **Player / Armies / Alliances** | `40K_settings_button_hover` | ✅ `LeaderboardWindow.cs:673`（`Kind = Skirmish`） |
| 51-52 | RankedClassicLeaderboardPopup Variant | `Tab Buttons` ▸ **Player / Armies** | `40K_settings_button_hover` | ✅ 同上（`Kind = Classic`） |
| 53-56 | Social Submenu Variant | `Alliances Tab/…/Tab buttons` ▸ **Generic Tab UI Button Info / Trophies / Create / Search** | `40K_tab_button_overwindow`（**`trans=0` 死值**） | ❌ 在 `Shell/AllianceMemberTab.cs` · `AllianceSearchTab.cs`（白名单外） |
| 57-68 | Social Submenu Variant | `LanguagesDropdown` ×3 ＋ `Privacy Dropdown` ×3 ＋ 各自的 `Item` ×6 | `_field_opened` ×6 ＋ `_item_selected` ×6 | ❌ 同上（`AllianceMemberTab.cs` 的社交下拉开法） |
| 69 | DeckSelectionContinueButton（模板 prefab） | `CircleButton` | `40k_UI_bt_play` | ❌ 我们的「`Practice Mode` 继续钮」在 `Shell/PracticeModePopup.cs`（白名单外） |
| 70 | Practice Mode Menu | `DeckSelectionContinueButton/CircleButton` | `40k_UI_bt_play` | ❌ 同上 |
| 71 | RankedEventWindow | `DeckSelectionContinueButton/Ranked Play Mode Button` | `40k_UI_bt_play` | ❌ 在 `Shell/RankedEventWindow.cs`（白名单外） |
| 72 | Achievement Type Toggle（模板 prefab） | 根 | `40K_settings_button_hover` | ❌ 同上（`AchievementsMenu.cs`） |
| 73-74 | Debug Reactivate Event Window | `Alliance Header Buttons/Tab buttons` ▸ Active / Inactive Events Toggle | `40K_tab_button_overwindow` | ⛔ **不计入 77**（调试件，我们不建；见 §〇 的口径） |

> 表里 #40-41、#46、#47、#69、#72 是**模板 prefab**（被别的 prefab 当模板克隆或直接复用）。
> 它们与「用它的那扇窗」算同一条判据 ⇒ 这里只在有对应落点时打 ✅，没有的（#69/#72）落❌。

---

## 二、本批接线：15 个代码站点（8 个文件）

| 文件 | 站点 | 行 | 传的选中图 | 覆盖的原版颗数 |
|---|---|---|---:|---:|
| `Shell/SettingsWindow.cs` | 5 颗页签（`Hit` 包装新增 `selectedArt` 形参） | `:2008` | `ArtTabBgSel` = `40K_settings_button_hover` | 5 |
| 〃 | 语言下拉框（`LanguagesDropdown`） | `:2331` | `ArtLangFieldSel` = `40K_dropdown_field_opened` | 1 |
| 〃 | 画质下拉框（`Quality DropDown`） | `:4463` | 同上 | 1 |
| 〃 | 手牌下拉框（`Text in Hand DropDown`） | `:4502` | 同上 | 1 |
| 〃 | 窗口模式下拉框（`Media ▸ Dropdown`） | `:5621` | 同上 | 1 |
| 〃 | 语言列表项生成器（12 行） | `:3734` | `ArtLstItemSel` = `40K_dropdown_item_selected` | 1 个模板（运行时 12 行） |
| 〃 | 通用下拉项生成器（各下拉的选项数） | `:3972` | 同上 | 3 个模板（运行时 3×N 行） |
| `Shell/PlayerProfileWindow.cs` | 6 颗页签（`Ranked` 被用户口径隐藏 ⇒ 建 5 颗） | `:415` | `ArtTabOn` = `40K_settings_button_hover` | 6（生效 5） |
| `Shell/LeaderboardWindow.cs` | `TabsOf(Kind)` 的页签（Skirmish 3 / Classic 2 / Draft 2） | `:673` | `ArtTabOn` = `40K_settings_button_hover` | 7 |
| `Shell/ChatPanel.cs` | 2 个频道键（`Orange Tab Toggle`） | `:495` | `TabArtOn` = `40K_settings_button_hover` | 1（模板）+ 2（运行时） |
| `Shell/MatchLogRow.cs` | `ReplayButton` | `:324` | `ArtBtn` = `40k_general_bt_yellow` | 1 |
| 〃 | `PinButton` | `:342` | 同上 | 1 |
| `Shell/DraftModeWindow.cs` | 3 颗 Deck Info toggle | `:1888` | `ToggleArtOnOff[0]` = `40K_settings_button_hover` | 3（＋两处嵌套复用） |
| `Shell/DeckInfoPopup.cs` | `Switch Deck Info Button`（五颗里**只有这一颗**，其余四颗原版 null） | `:1685` | `40k_general_bt_yellow` | 1 |
| `Shell/DeckSelectionPopup.cs` | 2 颗 `Generic Tab UI Button[ 1]`（九宫格 ⇒ `BindNine`） | `:774` | `TabOffArt` = `40K_tab_button_overwindow` | 2 |

**做法（三处细节，都是判据逼出来的，⛔ 不是随手挑的）**

1. **`MenuDraw.Hit` 不收 `selectedArt`**（那文件不在白名单）⇒ 每个文件的本地 `Hit` / `Hit3` 包装新增一个
   **末尾可选** `string selectedArt = null`，建完 `WindowButton` 之后**按同一组常态/悬停/按下图再 `Bind` 一次**，
   只补第五个实参。`Bind` 是幂等的（重读同一张纹理、同一个长宽比、`tintOnHover` 本就是 false）⇒
   **既有站点的前四格逐位不变**（不传 `selectedArt` 的调用点零影响）。
   涉及：`SettingsWindow.Hit/Hit3` · `MatchLogRow.Hit` · `DeckInfoPopup.Hit`；
   其余 5 个文件是在 `MenuDraw.Hit` 返回的节点上直接补一次 `Bind`/`BindNine`。
2. **`DeckSelectionPopup` 的两颗是九宫格**（原版 `Image.Type = Sliced`，9 块小 quad）⇒ 走 `BindNine`
   （只换中心那格 = 边框不跟着变，见 `WindowButton.BindNine` 的 doc）。
3. **那两颗原版 `m_Transition = 0`（死值）**：悬停/按下两档**显式传当前常态图**，不是「什么都不传」也不是
   「传 `_on` 那张」。理由：`trans=0` 的语义就是「四态一像素都不变」，传当前常态图 = 等价；
   而传 `null` 会让 `Bind` 按名推出 `…_overwindow_hover`（**不存在**）⇒ 往 `MissingSwapArt` 记一条**假缺口**，
   把别的宿主的 `CheckNoMissingSwapArt` 弄红（`DeckScene` / `CollectionScene` 都有这条检查）。

> 🔴 **接线后仍不会有可见变化**：`BtnState.Selected` 是 uGUI 的「键盘/手柄选中」那一档，**指针操作下走不到**
> （W34 已在 `WindowButton` 的注释里写死同一句）。本批的意义是**把原版序列化字段照抄全**，
> 让「我们和原版的 5-07 差 0」这句话成立；**不是**给玩家新增一个悬停效果。

---

## 三、差额 30 颗：判据在别的文件 / 我们没建那颗（⛔ 不是「不做」）

| 族（颗数） | 原版选中图 | 为什么本批接不了 | 该派给谁 |
|---|---|---|---:|
| 成就页 5 颗 `Achievement Type Toggle` | `40K_settings_button_hover` | 建在 **`Shell/AchievementsMenu.cs`**（`ArtTabOn` 常量与 `Hit(..., _tabBg[i], ArtTab, ...)` 都在那儿），**不在 W44 白名单** | `AchievementsMenu.cs` 的写手 |
| 社交子菜单 4 颗 `Generic Tab UI Button`（`trans=0`） | `40K_tab_button_overwindow` | 建在 **`Shell/AllianceMemberTab.cs` / `AllianceSearchTab.cs`**（`ArtTabOn/ArtTabOff` + `Hit(head, "InfoHit"…)` 一族），白名单外 | 社交窗写手 |
| 社交子菜单 6 个下拉框 + 6 个下拉项 | `_field_opened` / `_item_selected` | 同上（那两扇窗自己有一套 `40K_dropdown_item` 的开关图逻辑） | 同上 |
| `CircleButton` 一族 5 颗（`DeckSelectionContinueButton` 模板 · `Practice Mode Menu` · `RankedEventWindow` · `Generic Multi Card Display Combat` · `Draft Mode Ongoing State`） | `40k_UI_bt_play` | ①`PracticeModePopup` / `RankedEventWindow` / `MultiCardDisplay` **不在白名单**；②**轮抽的 `To Battle` 我们没用 `CircleButton`**（是 `UI_Button_Mulligan` + 文字）⇒ 那颗圆钮在我们树里**没有对应节点** | 对应文件写手（⚠️ 轮抽那颗要先决定「要不要补建 `CircleButton`」） |
| `Draft Mode *` 的 3 颗 `Debug Battle Button Button` | `40k_UI_bt_play` | 原版 `OnEnable` **强制 `SetActive(false)`**（正式包也不显示）⇒ 我们按纪律**不建** | ⛔ 不派（原版不可见） |
| `Achievement Type Toggle.prefab`（模板 1 颗） | `40K_settings_button_hover` | 与上面成就页 5 颗同一条判据、同一落点 | 同 `AchievementsMenu.cs` |

合计 **30 颗 = 5 + 4 + 12 + 5 + 3 + 1** ⇒ 与 §〇 的 47/77 自洽。
（口径提醒：这是 **prefab 实例**口径；我们的**运行时实例**更多 —— 语言下拉 12 行、通用下拉每项一行。）

---

## 四、行数变化（相对本写手开工那一刻，非 `HEAD`）

| 文件 | 开工 | 现在 | Δ |
|---|---:|---:|---:|
| `Shell/SettingsWindow.cs` | 7414 | 7438 | **+24** |
| `Shell/PlayerProfileWindow.cs` | 628 | 633 | **+5** |
| `Shell/ChatPanel.cs` | 1347 | 1352 | **+5** |
| `Shell/LeaderboardWindow.cs` | 939 | 944 | **+5** |
| `Shell/MatchLogRow.cs` | 517 | 530 | **+13** |
| `Shell/DraftModeWindow.cs` | 4898 | 4908 | **+10** |
| `Shell/DeckInfoPopup.cs` | 3125 | 3130 | **+5** |
| `Shell/DeckSelectionPopup.cs` | 1138 | 1150 | **+12** |
| `Editor/B141_W44_Assert.cs` | —— | 新建 | +267 |

> ⚠️ **账会漂**：这 8 个文件都被 `账/*.tsv` 按 `文件:行号` 引用过。本写手**无权改账主表**
> ⇒ 请调度台在「没有 `.cs` 写手」时跑一次 `check_ledger_points --all-A --fix`。
> 新增符号（便于按符号名重定位）：`SettingsWindow.ArtLangFieldSel / BindSelected`
> · `SettingsWindow.Hit3(..., selectedArt)` · `SettingsWindow.Hit(..., selectedArt)`
> · `MatchLogRow.Hit(..., selectedArt)` · `DeckInfoPopup.Hit(..., selectedArt)` · `B141_W44_Assert.WiringSites`。

---

## 五、断言：41 条（`Editor/B141_W44_Assert.cs`）

| 组 | 调用点 | 运行时条数 | 盯什么 |
|---|---:|---:|---|
| **A** | 25 | 25 | **逐站**接线：15 个站点各一条以上（`SettingsWindow` 拆成 11 条，因为一个文件里用了三张不同的选中图）＋ 三张常量表的名字 |
| **B** | 16 | 20 | 灭自证 / 口径：`TexOf(Selected)` 仍是 `_selectedTex ?? _normalTex`；**选中图 ≠ 常态图**（5 条）；5 张图真取得到（循环 5 次，＋`40k_UI_bt_play` 也取得到，虽本批 0 落点）；运行时探针 3 条（真图 ⇒ `SelectedTexForTest != null`／缺省 ⇒ null／给错名字 ⇒ 记进 `MissingSelectedArt` 且 `SelectedTexForTest` 仍 null） |
| **合计** | **41** | **45** | —— |

**挂法**（同一物理行追加；⛔ 别插进 `//` 之后）：
```csharp
B141_W44_Assert.Run(); pass += B141_W44_Assert.Pass; fail += B141_W44_Assert.Fail;
```
判绿红按行首 `B141W44 ✓` / `B141W44 ✗` 数（⛔ 别 `grep -c ✗`）。

**灭自证（🧨 什么坏法会让哪条红）**

| 改坏法 | 变红的断言 |
|---|---|
| 删任一站点的 `selectedArt:` 实参 | 该站点那一条 A（**逐站点各一条**，不是整体一条） |
| 把 `ArtTabOn` / `TabArtOn` 写成常态图那串 | 该文件常量条 ＋ B2 那一族 |
| `WindowButton.TexOf(Selected)` 改回无条件 `_normalTex` | **B1**（这一条正是 5-07 的题目） |
| 从 `Resources/Art/` 删掉任一张选中图 | B4 对应那条（且运行时 `MissingSelectedArt` 会非空） |
| `Bind` 里删掉 `_selectedTex` 的赋值 | **B5b** |
| `MissingSelectedArt.Add(...)` 删掉 | **B7b**（写成「什么都不记」也过不了） |

---

## 六、❓ / 给调度台的移交项

1. **❓ 轮抽的 `To Battle Button` 要不要补建 `CircleButton`？** 原版 `Draft Mode Ongoing State ▸ Battle Button Button`
   里那颗圆钮的 `m_SelectedSprite = 40k_UI_bt_play`；我们这一侧是 `UI_Button_Mulligan` + 文字（同一个按钮的另一次实现选择，
   **不是漏**）。要么把那个按钮改成原版结构（`CircleButton` + 文字），要么这颗 5-07 永远落在「没有对应节点」。
   **本写手不擅自改**（那是 5-06/5-07 之外的形态改动，且要动 4 扇模式窗）。
2. **❓ `Debug Reactivate Event Window` 的 2 颗**：本次重扫把它们算进 79 处、**排除在 77 之外**（调试件）。
   若调度台的口径是「79 处全算」，那差 2 颗可解释为「原版调试件、我们不建」（W34 报的 77 也是这个口径）。
3. **交接项（不是顺手做）**：`AchievementsMenu.cs`（5 颗）· `AllianceMemberTab.cs` / `AllianceSearchTab.cs`（16 颗）·
   `PracticeModePopup.cs` / `RankedEventWindow.cs` / `Core/MultiCardDisplay.cs`（3 颗）—— 合计 **24 颗**（另 3 颗 Debug 不建、
   2 颗模板件由同族覆盖，见 §三）。它们都是**一行式接线**（本地 `Hit` 加一个可选 `selectedArt` 透传），
   照本报告 §二 的「做法 1」抄即可。
4. **顺手发现（只记，不改）**：`SettingsWindow` 的 5 颗页签**悬停图**目前是 `40K_settings_button_hover`
   （= 后备规律推出的），而原版那 5 颗的 `m_HighlightedSprite` 是 `40K_settings_button_selected`
   （A17 那条注释也这么写，但代码没显式传 `hoverArt`）。这属于 **5-17**（悬停图逐颗映射）而不是 5-07 ⇒ 本批**没有动**，
   留给 5-17 的落点。⚠️ 同理 `PlayerProfileWindow` / `LeaderboardWindow` 的页签也没显式给 `hoverArt`
   （原版 `…_selected` vs 我们的 `…_hover`，两图不同）。
