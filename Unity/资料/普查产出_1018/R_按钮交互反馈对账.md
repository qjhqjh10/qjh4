# R · 按钮交互反馈对账（2026-10-18）

> 只读审查产出。**不改代码、不改正本**。
> 触发：用户 2026-10-18 点名「**为什么点击/悬停主界面模式卡一点反应都没有**」。
> 本文件分五节：① 原版规格表 ② 我们现状表 ③ 缺口清单（严重度排序）④ 模式卡专项（根因候选 + 判据）⑤ 没查清。
>
> **判据口径**：① 原版 prefab/MB 字段（`d:/2/新解包资源/assets_full/`）· ② 反编译方法体（`d:/2/tools/decomp_full/`）·
> ③ 我们自己的源码（`d:/4/Unity/MyGame/Assets/CardPresentation/`）。
> ⚠️ 本文件里的「我们」= 本仓当前 HEAD `87c5fe5`。

---

## 〇、结论速览

1. **原版 UI 的悬停/按下反馈只有两种机制：`ColorTint`（505/1276）与 `SpriteSwap`（630/1276）**；
   另有 **141 颗 `m_Transition = 0`（悬停什么都不变）**。**`m_Transition = 3 (Animation)` 一颗都没有**。
2. **原版菜单/战斗 UI 里没有 Animator**。`menus` 包、主菜单场景包、`battlearena1` 里
   **每一个 GameObject 的每一个组件 pid 都能落到导出文件上（未解析组件 = 0）**，且三包都没有 `Animator/` 目录
   ⇒ 不存在「悬停播 Animator 缩放/位移」这回事。
   Legacy `Animation` 组件只有 19 个（menus）+ 2 个（主菜单场景），全部挂在**装饰件**上
   （开包窗的卡、`Searching Oponent Popup/Window/Skull`、`Cardback`、`Ranked Division Change Window`），**没有一个挂在按钮上**。
3. **模式卡原版的「悬停动态」= 4% 变暗**（`EverguildButton.m_Transition = 1` · `m_HighlightedColor = 0.9607843` ·
   `m_FadeDuration = 0.1` · `m_TargetGraphic` = `Background Image`）。**没有缩放 / 位移 / 描边 / 光晕 / 换图 / Animator**。
   这一条 2026-10-17 已由 `A1493④` 查实，**本次复核逐字段重读，结论不变**，且**我们的实现与原版逐值相同**（见 §四）。
4. **我们缺反馈最重的族**：**战斗内按钮全族**（原版 89 颗里 59 颗 ColorTint / 27 颗 SpriteSwap，我们一条都没有）、
   **四扇子窗的左栏页签**（原版 `EverguildToggle` t=1、`HL = (0.6085,0.9426,1)` 青色、`m_TargetGraphic = Icon`，我们没接）、
   **设置窗的 4 行开关**（原版绿色 ColorTint）、**所有滑条**（原版 Slider ColorTint）、
   **收藏窗的卡牌格 / 卡组格 / 装饰格**（原版是 `Card2DController.ToggleHighlight` 的 0.2s alpha 淡入 / 卡组格 `localScale ×1.05`）。
5. **根因不在 `WindowButton`——它本身是完整的 UGUI `Selectable` 等价物**（Normal/Highlighted/Pressed/Selected 四态 ·
   0.1s 补间 · 换图 + 色偏两条路）。**根因在「命中区上没有可见图形」**：
   `MenuDraw.Hit(..., target: null)` 建的命中区是**一颗透明 quad**，而 `WindowButton.Collect()` 只收**自己子树**里的 quad
   ⇒ 色偏打在透明件上，**画面零变化**（= 等价于原版 `m_Transition = 0`）。
   全仓**生产侧只有 2 处 `TintOn(...)`**（`Shell/MainMenuRuntime.cs:1037` 五颗导航钮 · `:2592` 模式卡）——其余全靠 `Bind(...)` 换图。
6. **模式卡点击是通的（有实证）**：`_tmp_view/click_log.txt` 2026-10-18 10:49:44–50:22 四条记录显示
   四张模式卡全部「命中 `Hit`/Viewport/Content/… · 队列 2931」并「已派发 `onClick`」，日志分别是
   `[Menu] 模式卡 PRACTICE ⇒ 开 Practice Mode Menu` / `TUTORIAL ⇒ Tutorial Mode Menu` / `排位 ⇒ RankedEventWindowV2` /
   `选秀 ⇒ 如实提示「暂无服务器」`。**同一份日志 10:49:51 起还看到 `MainMenu → Background` 支与退出弹窗** ⇒ 那一轮玩家确实点开了窗。
7. **因此「模式卡点了/悬停了一点反应都没有」的最强解释是**：
   **悬停 = 4% 变暗本来就几乎看不见**（原版也一样），**按下 = 22% 变暗只在按住的那几帧**，而点击的结果是一扇**新窗盖上来**
   （主菜单被 `SetActive(false)`）⇒ 玩家来不及看见按下态。**这不是「没实现」**。
   ⚠️ 若用像素比对确认「连 4% 都没有」，则按 §四·3 的候选根因逐条排除（最可疑：**FTUE 暗幕**，见 ④·3·D）。
8. **点击音效**：壳侧几乎全覆盖（`WindowButton.Click()` 一律播 `clickCue ?? ButtonClick`；全仓 **88 处** `clickCue:` 显式覆盖）。
   **没接的**：**战斗内全部按钮**（`BattleDriver`/`SettingsPanel` 的点击路径不经过 `WindowButton`，也不播 cue）·
   **卡组编辑**（`DeckRuntime` 只给 4 个 id（`hdr_back/hdr_clear/foot_done/hdr_filters`）+ 12 颗筛选 Toggle 播了音，
   其余按钮/卡格**不播**）。
9. **不能拿「原版是 UGUI，我们没有 EventSystem」当借口**：`PointerLayer` 已经把
   `IPointerEnter/Exit/Down/Up/Click` 那套语义完整复刻（含 10px 拖拽阈值、`currentSelectionState` 优先级），
   缺的是**各站点有没有把「可见图形」交给它**。
10. **本轮没有真机验证**（按现行口径「只做不验」）。所有「我们有什么」都是静态判据；标 ⚠️ 的地方是**没查到/待实测**。

---

## 一、原版规格表

### 1·1 全局普查（可复现）

| bundle | Selectable 总数 | t=0 None | t=1 ColorTint | t=2 SpriteSwap | Animator |
|---|---:|---:|---:|---:|---|
| `bundle_menus_assets_all`（所有菜单窗 prefab） | **1276** | 141 | 505 | 630 | **0** |
| `bundle_scenes_scenes_mainmenuwarpforge`（主菜单场景） | **29** | 2 | 20 | 7 | **0** |
| `bundle_scenes_scenes_battlearena1`（战斗 HUD） | **89** | 3 | 59 | 27 | **0** |
| `bundle_generalgamewindows_assets_all` | 22 | 2 | 6 | 14 | **0** |
| `bundle_battlesharedresources_assets_all` | 2 | 0 | 0 | 2 | **0** |
| `bundle_mainmenualwaysloaded_assets_all` | 2 | 0 | 2 | 0 | **0** |

- **类分布（menus 1276）**：`EverguildButton` 855 · `EverguildToggle` 151 · `Slider` 132 · `Button` 50 ·
  `EverguildInputField` 28 · `Scrollbar` 16 · `TMP_InputField` 12 · `Toggle` 12 · `TMP_Dropdown` 11 ·
  `EverguildButtonStateFollower` 8 · `InputField` 1。
- **「有没有 Animator」的判据**（本轮的查法，可复现）：遍历每个 `GameObject/*.json` 的 `m_Component`，
  拿 pid 去所有导出类型目录里找；**找不到的 = 0 条**（menus / mainmenuwarpforge / battlearena1 三包都是 0）。
  ⇒ 凡「悬停有缩放/抬升」的说法，在这三包里**没有组件承载**。
- 另有 19（menus）/ 2（场景）个 legacy `Animation` 组件，全部在装饰件上（清单见本轮脚本输出；不赘列）。

### 1·2 逐族规格表

> `HL` = `m_Colors.m_HighlightedColor`（**乘**在 `m_TargetGraphic` 的顶点色上）· `PR` = `m_PressedColor` ·
> 全部 `m_FadeDuration = 0.1` · `m_ColorMultiplier = 1`（除标注）。

| # | 族 | 代表节点（prefab 内路径/资产） | 类 | trans | HL / PR | 换图（SpriteState） | 悬停额外件 | 点击音 |
|---|---|---|---|---|---|---|---|---|
| 1 | **左栏导航钮（主菜单 5 颗）** | 场景 `…/Navigation Panel/Buttons Container/Main Menu Navigation Button - {Home,Rewards,Shop,Collection,Social}` | `EverguildToggle` | **1** | **HL `(0.7217,0.8152,1)` 浅蓝** · PR `0.7843` · SEL `0.9608` | 无（四格 null） | **无**。选中 = 子件 `Highlight`（红 `#FF0000`，由 Toggle 的 `graphic` 开关）；`isOn` 时 `OnPointerEnter` **直接 return**（`EverguildToggle__OnPointerEnter.c`） | `m_onClickSound = null` + `useDefaultAudioClipIfNull = 1` ⇒ **默认按钮音** |
| 2 | **模式卡 / 活动卡** | `Base Game Mode Container 1x1` 根 · MB `4126037038486929469` | `EverguildButton` | **1** | HL `0.9608` · PR `0.7843` · SEL `0.9608` · NOR 白 | **无** | **无**（无 Animator、无缩放、无描边）。`m_TargetGraphic` = pid `7827625613328400445` = 子件 **`Background Image`**（`UIParallaxImage : Image`，`m_RaycastTarget = 1`）。点击 = `LiveopMenuContainer.OnClick`（`LiveopMenuContainer__OnClick.c`）→ 事件虚方法开窗 | 默认 |
| 3 | **顶栏图标**（齿轮/收件箱/挑战/反馈/头像/边框） | 场景 `…/Upper bar/{SettingsBtn, TopBarButtons/{InboxBtn,Challenge button,Feedback Button}, Player Profile/{Avatar Item Small,Profile border}}` | `EverguildButton`（Feedback 上还叠一颗 `Button`） | **1** | HL `0.9608` · PR `0.7843`（`Profile border` HL 是 **`(0.7877,0.9225,1)` 淡蓝**） | 无 | 无 | 默认（`useDefaultAudioClipIfNull = 1`） |
| 4 | **四窗左栏页签**（奖励/收藏/商店/社交） | `Rewards Base Submenu Variant/Content Area/Tab Buttons/MissionsRewardsButton` | `EverguildToggle` | **1** | **HL `(0.6085,0.9426,1)` 青蓝** · PR `0.7843` · SEL `0.9608` · DIS `0.8019` | 无 | 无。`m_TargetGraphic` = pid `7205249508978923263` = 子件 **`Icon`**（白，`m_RaycastTarget = 0`）；选中 = 子件 `Highlight`（红 `#FF0000`，`graphic` 字段 = pid `-1415613743483119873`）；`colorTintOnValueChange = 0` / `changeSpriteOnValueChange = 0` | **默认**（`m_onClickSound` 显式指向 `SoundManager.defaultButtonSound` 那条 cue 的 pid `6176553109044381692`） |
| 5 | **设置窗页签（5 颗）** | `Main Menu Settings Window/Menu Area/Mask Tabs buttons/Tab Buttons/*` | `Button`/`EverguildButton` | **2** | （ColorTint 死值） | `40K_settings_button` ↔ **`40K_settings_button_selected`**（注意：原版这其实是**悬停**图，见 §五） | 无 | 默认 |
| 6 | **关闭钮三族** | `Generic Close Button Orange` / `Generic Close Button` / `Generic Close Button Green` | `EverguildButton` | **2** | 死值 | Orange：按钮自己的 `Image = UI_Button_Round_background` ↔ `40k_general_bt_yellow_hover`；Green：`40k_bt_close` ↔ `40k_bt_close_hover`（P 也非空） | 无 | 默认 |
| 7 | **商店购买 / 价签** | `Catalog Item Shop Container/…/Price Display Button/Generic UI Button` | `EverguildButton` | **2** | 死值 | `40K_button` ↔ `40K_button_hover` · P = `40K_button_pressed` | 无 | 默认 |
| 8 | **奖励收集** | `Rewards Base Submenu Variant/…/Generic UI Button` 一族 | `EverguildButton` | **2** | 死值 | `40K_button` ↔ `_hover` | 无 | 默认 |
| 9 | **卡牌格（收藏/卡牌详情）** | `Collection Card` / `Collection Card With Highlight` / 卡组编辑卡格 | **无 Selectable**（整棵 0 个） | — | — | — | **`Card2DController.ToggleHighlight(bool)`**：把卡面材质某个颜色属性的 **alpha 在 0.2s 内补到 1/0**（`Card2DController__ToggleHighlight.c` + `DAT_1834b2bb0 = 0.2f`） | 无（由父级 `CollectionCard.OnPointerEnter` 之类驱动；⚠️ 驱动点未逐条查） |
| 10 | **卡组格（收藏窗卡组页）** | `Collection Deck` / `Collection Deck With Highlight` | **无 Selectable** | — | — | — | **`CollectionDeck.HoverHighlight(bool)`：`transform.localScale` 在 1.0 ↔ 1.05 之间切换**（`CollectionDeck__HoverHighlight.c`，常数 `DAT_1834b3254 = 1.05f`，直接 set、无补间；`+0xb8` 为真时整段跳过） | 无 |
| 11 | **装饰格（收藏-装饰页）** | `Collection Cosmetic` | **无 Selectable** | — | — | — | `CollectionCosmetic.HoverHighlight(bool)`：`UIImageMaterialColorChanger.DoColorChange(color, propId, 0.2f, …)`，alpha = 悬停位（`CollectionCosmetic__HoverHighlight.c`） | 无 |
| 12 | **卡组编辑行/钮** | `Deck Editing Menu`（**29 颗**：`Slider` 9 · `EverguildToggle` 12 · `EverguildButton` 6 · `EverguildInputField` 2） | `EverguildToggle` / `EverguildButton` / `Slider` / `EverguildInputField` | toggle：**9 颗 t=0 / 7 颗 t=1**；button：**3 颗 t=2 / 3 颗 t=1** | 卡组行两颗 HL = **`0.6887` 灰**（普查 §块 3） | 3 颗 t=2 有图 | 无 | **显式 cue**：12 颗 Toggle 里 9 颗带 `m_onClickSound`；3 颗 t=1 的 Button `useDefaultAudioClipIfNull = 0`（**无声**） |
| 13 | **滑条** | `Main Menu Settings Window/…/Sound effects Slider` · `…/FPS Slider` · 战斗 `BattleSettingsPanel/Volume Sliders/*` | `Slider` | **1** | HL `0.9608` · PR `0.7843` · DIS `0.7843, a=0.502` | 无 | 无 | 无（Slider 不是 `EverguildButton`） |
| 14 | **复选框 / 开关** | `Main Menu Settings Window/…/Graphics Tab/…/{Small Screen Size Toggle, Auto Zoom Toggle, Use super sampling, Vsync}` · 战斗 `BattleSettingsPanel/Auto Zoom Toggle` | `EverguildToggle` | **1** | **NOR `(0.2863,0.9647,0.6863)` 绿** · HL `(0.2921,0.9528,0.6817)` · PR `(0.3058,0.8208,0.6105)` · SEL `(0.2902,0.9529,0.6824)` | 无 | 无（`colorTintOnValueChange = 0` ⇒ **值变化不改色**；悬停/按下仍走 ColorTint） | 默认 |
| 15 | **战斗内按钮** | `Clock/TurnBtn` · `BattleSettingsPanel/Bottom buttons/Resign Button` · `BattleSettingsPanel/Generic Close Button` · `Mulligan/ButtonsGroup/*` · `ChooseCardMenu/ButtonsGroup/*` | `EverguildButton` 30 · `Button` 40 · `EverguildButtonStateFollower` 2 · `ChatPopupButton` 6 … | t=1 **59** / t=2 **27** / t=0 **3** | 例：`TurnBtn`/`Resign`/战斗关闭钮 = **t=2 换图**（HL/PR PPtr 均非空）；`SettingsBtn`/`MulliganContinueButton/Button` = t=1 默认灰或自定义绿 | **有**（27 颗） | 无 Animator | 默认（EverguildButton 一族） |
| 16 | **Toast 通知** | `Toast Notifications` 根 | `EverguildButton` | **0** | — | — | — | **无**（原版本来就没有反馈；我们的实现一致，**不是缺陷**） |
| 17 | 开局动画（`level0` = Intro） | — | — | **0 个** | — | — | — | — |

### 1·3 原版**没有**的反馈（⚠️ 防「编一套」）

- **没有 `m_Transition = 3`（Animation）的按钮**（全库 0 颗）。
- **没有 Animator**（三包未解析组件 = 0）。
- **模式卡没有缩放/抬升/描边/光晕**（`m_Transition=1` 是唯一动态；`UIParallaxImage` 是**常驻**的屏幕位置视差，不随鼠标变）。
- **没有「右键」反馈**：UGUI `Button.OnPointerClick` 首行 `if (eventData.button != Left) return;`（我们已照做）。
- **`SpriteSwap` 与 `ColorTint` 是互斥的一条**：一颗 `Selectable` 只有 `m_Transition` 里那一种，不会两条同时上。

---

## 二、我们现状表

### 2·1 共同件（先说清楚「管线是通的」）

| 件 | 位置 | 做什么 | 关键限制 |
|---|---|---|---|
| `PointerLayer` | `Shell/PointerLayer.cs`（`Update` 269-383 · `HoverAt` 1206-1214 · `HitButton` 1328-1341 · `CollectHits` 1373-1401） | 唯一一条真鼠标路径：滚轮 / 按下-抬起同件才算点击 / **悬停 Enter-Exit** / 10px 拖拽阈值 / 键盘+手柄导航 / 选中态 | 命中 = **轴对齐矩形**（`HitBoxPx`：quad 中心 + `WorldW/H` × 父链缩放），**旋转不参与**；命中顺序按**渲染队列↓**、同队列比 z |
| `WindowButton` | `Shell/WindowButton.cs` | UGUI `Selectable` 的等价物：`Enter/Exit/Press/Release/SetSelected` → `ApplyState`（四态优先级 **Pressed > Selected > Highlighted > Normal**）· 色偏 0.1s 补间 · `Bind` 换图（自动关色偏）· `Click()` 派发 + **播放按钮音** | **色偏只打在「自己子树里的 quad」+ `TintOn(...)` 显式追加的 quad 上**（`Collect()` 535-557）。命中区节点是叶子 ⇒ **没传 `target`/没 `TintOn` ⇒ 色偏看不见** |
| `MenuDraw.Hit` | `Shell/MenuDraw.cs:2048-2113` | 建 `Hit` 节点 + 透明 quad（`MakeHitQuad` 2132-2145）+ `WindowButton`；`target != null` 时 `wb.Bind(...)` | **`target == null` 时 `art/hoverArt/pressedArt` 全部被忽略**（第 2112 行 `if (target != null)`） |
| `GameWindow.AddHit` | `Shell/GameWindow.cs:293-299` | 转发 `MenuDraw.Hit`（带上 `Clip` 与 `ClipPad`） | 同上 |
| `WindowSounds` | `Shell/WindowSounds.cs` | cue 表 + 播放（FX/Jingles 两组） | 表里只有壳侧 cue；战斗/卡组编辑不在表内 |

### 2·2 逐族现状表

| # | 族 | 我们怎么建 | 悬停 | 按下 | 选中 | 点击音 | 判据（file:line） |
|---|---|---|---|---|---|---|---|
| 1 | 左栏导航钮 | `MainMenuRuntime.NavButton`：`WindowButton` 挂在**透明 `Hit`** 上 + `TintOn(iconQ)` + `HighlightKey = NavHoverKey` | ✅ **浅蓝 tint**（`(0.7217,0.8152,1)`，**逐值同原版**） | ✅ 0.7843 | ✅ `SetSelected` 灰 0.9608 + 独立红底图层 `RepaintNavHighlight` | ✅ 默认 | `Shell/MainMenuRuntime.cs:941-1038`（`HighlightKey` 1036 · `TintOn` 1037） |
| 2 | **模式卡** | `BuildModeCard` 第⑥步：`Hit` + `WindowButton` + **`TintOn(cardArtQ)`**；`HighlightKey` 用默认灰 | ✅ **0.9608 tint**（= 原版） | ✅ 0.7843（= 原版） | ✅ 0.9608（`PointerLayer.Select`，按下即选中） | ✅ 默认 | `Shell/MainMenuRuntime.cs:2560-2603`（`TintOn` 2592 · 判据注释 2579-2602） |
| 3 | 顶栏 5 颗 | `TopBar.Wire`：`WindowButton` **直接挂在可见 quad 的 GameObject 上** | ✅ 默认灰 tint（可见，因为 quad 就是可见件） | ✅ 0.7843 | ✅ | ✅ 默认 | `Shell/TopBar.cs:279-291` |
| 4 | **四窗左栏页签** | `MainMenuSubmenuWindow.BuildTabButton` → `MenuDraw.Hit(b,"Hit",…, QPanel, …)` **不传 target** | ❌ **无**（tint 打在透明 Hit 上） | ❌ 无 | ✅ 红底 `Highlight` 有（`res.highlight`） | ✅ 默认 | `Shell/MainMenuSubmenuWindow.cs:516-671`（`Hit` 665；注释 663-664 断言「原版这几颗没有悬停」——**与 prefab 不符**，见 §三·P0-1） |
| 5 | 设置窗页签 | `SettingsWindow.BuildTabs` → `Hit(..., bg, ArtTabBg, hoverArt…)` **传了 target** | ✅ 换图 | ✅ 换图 | ✅ | ✅ | `Shell/SettingsWindow.cs:1936` |
| 6 | 关闭钮 | 多数窗 `Hit(..., target, art)` 或 `Bind/BindSelf` | ✅ 换图（**取决于该窗有没有传**；见 §三·P1-6 清单） | ✅ | ✅ | ✅ 默认/`DefaultCloseCue` | 例：`Shell/BoosterInfoPopup.cs:379` · `DailyRewardPopup.cs:453,490` |
| 7 | 商店购买 | `ForgeTab` 类站点 `AddHit(..., btnQ, "40K_button", …)` | ✅ 换图 | ✅ | ✅ | ✅ | 例：`Shell/ForgeTab.cs:889` |
| 8 | 奖励收集 | 同上（`40K_button`） | ✅ 换图 | ✅ | ✅ | ✅ | 普查 §块 4 |
| 9 | **收藏-卡牌格** | `CollectionWindow` 卡格 = `AddHit(..., "CardHit_i")` **无 target** | ❌ **无**（原版是 `ToggleHighlight` alpha 0.2s） | ❌ | ❌ | ✅（若走 AddHit） | `Shell/CollectionWindow.cs:1146` · 文件自己写着「我们既没有 hover」`:906` |
| 10 | **收藏-卡组格** | `CollectionWindow` 卡组格 `AddHit(..., "Hit")` 无 target | ❌ **无**（原版 `localScale ×1.05`） | ❌ | ❌ | ✅ | `Shell/CollectionWindow.cs:1409`（另 `:2266` 自陈没有 hover） |
| 11 | **收藏-装饰格** | `AddHit(..., "AltArtHit_…")` 无 target | ❌（原版材质 alpha 0.2s） | ❌ | ❌ | ✅ | `Shell/CollectionWindow.cs:2051` |
| 12 | 卡组编辑行 | 自己一套 `DeckRuntime` 指针路径（不走 `WindowButton`） | ✅ **`RowHoverK = 0.6887`**（逐值同原版卡组行） | ❌ 无按下态 | ✅ 有选中 | ⚠️ 只有 4 个 id + 12 颗筛选 toggle 播 cue | `Deck/DeckRuntime.cs:4365-4383`（应用）· `:8267`（常量）· `:5088-5108`（cue 表） |
| 13 | **滑条** | `Battle/WfSlider.cs` | ❌ **完全没有**（无 tint / 无换图 / 无按下） | ❌ | ❌ | ❌ | `Battle/WfSlider.cs`（`grep -n "Hover\|Tint\|Pressed"` **零命中**） |
| 14 | **设置窗 4 行开关** | `SettingsWindow.BuildCheckRow` → `Hit(n,"Hit",…)` **无 target**（可见件是 `Toggle` 那张图） | ❌ **无** | ❌ | ✅ 换图（点完 `SetTexture`） | ✅ `SelectToggleCue` | `Shell/SettingsWindow.cs:4404-4440`（`Hit` 4423） |
| 15 | **战斗内按钮** | `BattleDriver` / `SettingsPanel` 自己的命中（`HitPaddedRect`/`RectContains`），**不经过 `WindowButton`** | ❌ **无**（唯一例外：语言下拉行的 hover 换图 `SettingsPanel.RefreshLangHover`） | ❌ | ❌ | ❌（战斗侧不播按钮音） | `Battle/BattleDriver.cs:8257`（`HitEndTurn`）· `Battle/SettingsPanel.cs:1544`（仅语言行）· `:2114`（`HitResign`） |
| 16 | Toast | `MenuDraw.Hit(root,"Hit",…, null)` | ✅ **正确地没有**（原版 trans=0） | — | — | 无 | `Shell/ToastNotifications.cs:522`（注释写明 `m_Transition = 0`） |

### 2·3 计数（生产侧，`Assets/CardPresentation/Shell/*.cs`）

- `MenuDraw.Hit(...)` **99 处**：**62 处传了 `target`**（会 `Bind` 换图）· **37 处没传**（其中一部分原版本来就是 `trans=0`/无 Selectable，不是缺陷）。
- `GameWindow.AddHit(...)` **31 处**：**20 处传了 `target`** · **11 处没传**。
- 生产侧 `TintOn(...)` **仅 2 处**：`MainMenuRuntime.cs:1037`（导航 5 颗）· `:2592`（模式卡）。
- `WindowButton` 的换图口：`.Bind(...)` **18 处** · `BindSelf(...)` **8 处** · `BindNine(...)` **11 处**（壳侧换图的主力；另有 62 处 `MenuDraw.Hit` 是经 `target` 参数间接触发 `.Bind`）。
- `clickCue:` 显式覆盖 **88 处**。

---

## 三、缺口清单（严重度排序）

> 严重度判据：**玩家会不会因此以为「点不动 / 没点上」** > 缺的是「看得见的反馈」 > 缺的是「细微反馈」。

### P0（**整族没有反馈**，且原版明确有）

| # | 族 | 原版有什么（判据） | 我们有什么 | 根因（file:line） |
|---|---|---|---|---|
| P0-1 | **四窗左栏页签**（奖励/收藏/商店/社交，每窗 2–5 颗） | `EverguildToggle` **t=1** · **HL `(0.6085,0.9426,1)` 青蓝** · PR 0.7843 · fade 0.1 · `m_TargetGraphic` = 子件 **`Icon`**（MB `-4134137194770196737`，pid 实解；收藏窗 `Tab Buttons/{Deck,Cards}` 逐值同） | **悬停/按下都没有**（只有选中红底） | `Shell/MainMenuSubmenuWindow.cs:665` 的 `MenuDraw.Hit(...)` 没传 `target`；且同处 `:663-664` 的注释断言「原版这几颗**没有**悬停/按下（`m_Transition` → 见 `HoverTint`）」——**与 prefab 字段相反**，而且它引的 `HoverTint` **全仓不存在**（`grep HoverTint` 零命中，是注释过期）。错因推断：把「`colorTintOnValueChange = 0`（**值变化**不改色）」误读成「没有 ColorTint」 |
| P0-2 | **战斗内按钮全族**（结束回合 / 投降 / 设置 / 关闭 / 换牌 / 选牌 / 聊天…） | 89 颗 Selectable：**59 颗 t=1（ColorTint）· 27 颗 t=2（SpriteSwap）**；例：`Clock/TurnBtn` t=2（HL/PR 图均非空）· `Resign Button` t=2 · `BattleSettingsPanel/Generic Close Button` t=2 · `MulliganContinueButton/Button` t=1 | **悬停 / 按下 / 选中 / 按钮音 一条都没有**（只有回合态的静态染色 `myTurn ? 白 : 灰`） | 战斗不走 `PointerLayer`/`WindowButton`：`Battle/BattleDriver.cs:8257`（`HitEndTurn`）· `Battle/SettingsPanel.cs:2114`（`HitResign`）等全是**纯矩形判定**；`Battle/WfSlider.cs` 连轨道都没有状态 |
| P0-3 | **设置窗 4 行开关**（Small Screen UI / Auto Zoom / Super sampling / VSync） | `EverguildToggle` t=1 · **NOR 绿 `(0.2863,0.9647,0.6863)` / HL `(0.2921,0.9528,0.6817)` / PR `(0.3058,0.8208,0.6105)`** | 无悬停/无按下（点完只换开/关图） | `Shell/SettingsWindow.cs:4423` 的 `Hit(...)` 没传 `target`（可见件是 `Rect(n,"Toggle",…, box)`，box 在 Hit 的**兄弟**位置） |
| P0-4 | **所有滑条**（设置 3 根音量 + FPS · 战斗 3 根音量） | `Slider` **t=1** · HL 0.9608 · PR 0.7843 · fade 0.1（`m_TargetGraphic` = 手柄） | 无悬停/无按下/无选中 | `Battle/WfSlider.cs`（整个文件零 `Hover/Tint/Pressed`） |
| P0-5 | **收藏窗三类格子**（卡牌格 / 卡组格 / 装饰格） | 卡牌：`Card2DController.ToggleHighlight` → 材质颜色 **alpha 0.2s 补到 1**；卡组格：**`localScale ×1.05`**（常数实读 1.05）；装饰：`DoColorChange(…, 0.2f)` | 三种都**没有悬停** | `Shell/CollectionWindow.cs:1146`（卡格）· `:1409`（卡组格）· `:2051`（装饰格）——`AddHit` 全无 `target`；文件自己在 `:906`/`:2266` 写着「我们没有 hover」 |

### P1（**单窗/单站点**缺反馈）

| # | 站点 | 原版 | 我们 | 根因 |
|---|---|---|---|---|
| P1-6 | **`GiveFeedbackPopup`** 的 `Submit button`（`40K_button` 九宫）与 `Generic Close Button` | 该 prefab 根 `Give Feedback Popup menu` 有 **46 颗 t=2**（换图） | **无悬停/无按下** | `Shell/GiveFeedbackPopup.cs:482`（`Hit(_submit,"Hit",…, Submit)`）· `:490`（`Hit(_close,…)`）都没传 `target`；可见件是 `_submit`/`_close` 的**兄弟/父级** |
| P1-7 | `LiveOpsEventWindow.BuildHelp` 的 `Help Button` | t=1（ColorTint） | 无 | `Shell/LiveOpsEventWindow.cs:969` `Hit(root,"HelpHit",…)` 无 target（可见图 `MenuDraw.Rect(root, Tex(ArtHelp),…)` 是 Hit 的兄弟） |
| P1-8 | `LeaderboardWindow` 的阵营头像钮（`Army Item Button`） | 普查：`trans=1`（ColorTint） | 无 | `Shell/LeaderboardWindow.cs:575` `Hit(node,"Hit",…)` 无 target（`node` 下的 `Icon` 是兄弟） |
| P1-9 | `PurchasePremiumWindow` 的 `Army Container` 整卡点击 | （该族容器是可选中的；⚠️ 具体 trans 未逐颗核 → §五） | 无 | `Shell/PurchasePremiumWindow.cs:664` `MenuDraw.Hit(cn,"Hit",on,QHit,…)` 无 target |
| P1-10 | `MainMenuRuntime` 的 `ChatHit`（聊天预览） | **t=0 ⇒ 原版无反馈** | 无 = **正确** | `MainMenuRuntime.cs:1902`（列在这里只为「别当缺陷修」） |
| P1-11 | `ReferralPopupWindow` 输入框点击 | 输入框不是按钮反馈族 | 无 = 正确 | `Shell/ReferralPopupWindow.cs:513` |
| P1-12 | **ToastNotifications** 四处 `Hit` | `m_Transition = 0` | 无 = **正确** | `Shell/ToastNotifications.cs:522,578,634,674` |
| P1-13 | 其余无 `target` 的站点（`BaseEventPopup:315` · `BoosterInfoPopup:565` · `ChatPanel:52,281` · `FTUESelectDeckContainer:245` · `MenuDraw:2483,2495` · `OfferContainer:1242` · `PurchasePremiumWindow:716`（价签已 `BindNine`，无碍）· `SettingsWindow:3538`（下拉 blocker，**应当无反馈**）· `SettingsWindow:6175,6260` · `ShopTabPage:558`（我们自加的 `InfoHit`）· `TutorialMessagePopup:322` · `TutorialModePopup:644` · `WelcomePopup:233`） | ⚠️ **未逐条核原版 trans**（这 18 处本轮只列出来源，没有逐颗对 prefab） | — | 见 §五 |

### P2（**反馈存在但「看不见/不生效」的同族风险**）

| # | 现象 | 判据 |
|---|---|---|
| P2-14 | **`Bind` 过的按钮若悬停图取不到 ⇒ 零反馈**（`Bind` 会关 `tintOnHover`，`SwapTo(null)` 是静默早退） | `Shell/WindowButton.cs:301-319`（`tintOnHover = false` 与 `MissingSwapArt` 记录）；**今天到底有没有缺图 = 只有运行时读 `MissingSwapArt` 才知道**（§五） |
| P2-15 | 命中区与可见件**同队列**时谁吃指针不确定（历史上已吃过多次） | `PointerLayer.HitButton` 1328-1341 + `MenuDraw.Hit` 的队列参数；本文件不展开（已有多份账） |
| P2-16 | **按钮音覆盖不全** | 战斗全族无按钮音（P0-2 连带的）· 卡组编辑只 4 个 id（`DeckRuntime.cs:5088-5108`）· 原版默认音是 **1022/1022 颗 `useDefaultAudioClipIfNull = 1`**（`WindowButton.cs:844-866` 的普查） |

---

## 四、模式卡专项

### 4·1 原版判据（本次逐字段重读，可复现）

| 项 | 值 | 出处 |
|---|---|---|
| 卡根组件 | `LiveopMenuContainer`（MB pid `4473714526825562173`）· **`EverguildButton`**（MB pid `4126037038486929469`）· `FlexibleLayoutSizeOption` · `RectMask2D` · `CustomLocalizeCallback` | `bundle_menus_assets_all/GameObject/Base Game Mode Container 1x1.json` 的 6 个组件 pid，逐个解出 |
| `m_Transition` | **1 = ColorTint** | MB `4126037038486929469` |
| `m_Colors` | NOR 白 · **HL `0.9607843`** · PR `0.7843137` · SEL `0.9607843` · DIS `0.7843137` · `m_ColorMultiplier 1` · **`m_FadeDuration 0.1`** | 同上 |
| `m_SpriteState` | 四格 **全 null**（不换图） | 同上 |
| `m_TargetGraphic` | pid `7827625613328400445` = 子件 **`Background Image`** 上的 `UIParallaxImage`（`Image` 子类；`m_RaycastTarget = 1` · `m_Color` 白 · `m_PreserveAspect = 0`） | MB `7827625613328400445` + 父链 |
| Animator / Animation | **无**（该 prefab 的 GO 组件里没有任何 Animator；全包未解析组件 = 0） | §1·1 的查法 |
| 缩放/位移/描边/光晕 | **无**（`m_Transition` 只有 ColorTint 一条；`UIParallaxImage` 是**常驻**的 `_ScreenPosition` 视差，与鼠标无关） | `MainMenuRuntime.cs:2265-2329` 已把视差算式解出来并 bake |
| 点击 | `LiveopMenuContainer.OnClick()` → `LiveopMenuContainer__OnClick.c:19-21` 接口虚调用（`LiveopMenuContainer` 的 `backgroundButton` 在 `OnInitialize` 里接） | `资料/阶段二_战斗入口_原版规格.md:28` 也记着同一条 |
| 音效 | `m_onClickSound` 空 + `useDefaultAudioClipIfNull = 1` ⇒ **默认按钮音** | MB 同文件 |

### 4·2 我们的实现（逐行）

- `Shell/MainMenuRuntime.cs:2560-2603`（`BuildModeCard` 第⑥步）：
  - `New(card,"Hit",w,h)` + `ImageQuad.Create(hitGo, Solid, Center(cx1,cx2,y,y+h), h/108f)` + `SetTint(0,0,0,0)` + **`SetRenderQueue(QOverlay + 1) = 2931`**（2574）。
  - `wb.onClick = () => OpenMode(kind)`（2578）。
  - **`wb.TintOn(cardArtQ)`（2592）** ⇒ 色偏会打在**卡面那颗可见 quad** 上。
  - 2593-2602 的注释逐条记录了「四个 `m_Colors` 与 `FadeDuration` 都已逐值核过、一个字都不用改」。
- `WindowButton` 那一侧：`Enter → ApplyState → SetTarget(HighlightKey)`，Play 里 `Update()` 用 `unscaledDeltaTime / 0.1` 逐通道 lerp（`WindowButton.cs:519-528`），`ApplyTint` 乘在 `_tqBase` 上（547-557）。
- 因此**静态判据指向「实现正确」**：悬停应为整张卡面 ×0.96，按下 ×0.7843，两者都是 0.1s 补间。

### 4·3 根因候选（按可能性排序）+ 判据

> ⚠️ 本轮**不跑 Unity、不起 player**（现行口径），所以下面是「候选 + 怎么判」，**不是实测结论**。

**A.（最强）原版反馈本身就极弱——不是缺陷。**
- 判据：`m_HighlightedColor = 0.9607843` 是**乘法**（`CrossFadeColor` 写 `CanvasRenderer` 顶点色）⇒ 整卡亮度只降 **3.9%**；
  一张大幅摄影风格卡图上这几乎不可见。**原版玩家在实机上也只能看到「按住时 22% 变暗」这一档。**
- 我们逐值相同（2593-2602 的核对记录）。
- ⇒ 若用户是拿「原版记忆里的动态」当尺子，**这把尺子在 prefab 里不存在**（A1493④ 已查过一次，结论未变）。

**B. 点击其实有反应（有实证）——「没反应」可能指的是「按下的视觉」而不是「窗没开」。**
- 判据：`_tmp_view/click_log.txt` **2026-10-18 10:49:44 / 10:49:45 / 10:49:47 / 10:49:50** 四条：
  `命中 Hit/Viewport/Content/Base Game Mode Container… · 队列 2931` → `已派发 onClick` →
  `[Menu] 模式卡 PRACTICE ⇒ 开 Practice Mode Menu` / `TUTORIAL ⇒ Tutorial Mode Menu` / `排位 ⇒ RankedEventWindowV2` / `选秀 ⇒ 暂无服务器弹窗`。
  同一天 10:49:51–53 还有 `MainMenu → Background` 支与退出弹窗 ⇒ 那一轮**主菜单是活的、窗是开得出来的**。
- **怎么判**：起 player 后开 `ClickLog`（`-wfclicklog`，见 `Core/ClickLog.cs`），点卡面正中：
  - 出现 `命中 Hit/…/Base Game Mode Container…` ⇒ 通道没问题，问题只在**视觉强度**；
  - 出现 `⚠ 这一点上没有命中任何可点的件` ⇒ 走 C 类（命中区/坐标）。

**C. 悬停连 4% 都没有（需要像素级比对才能确认）。**
- 判据方向（逐条排除）：
  1. **`cardArtQ == null`（卡图缺资源）** ⇒ `TintOn` 没被调用，色偏落回透明 `Hit` 上 ⇒ 零变化。
     判据：`BuildModeCard` 2355 `var tex = Art(art)`；取不到时 `MissingArt.Add(art)`（2377）。**运行时读 `MissingArt` 是否为空**即可。
  2. **`PointerLayer` 不在跑** ⇒ 但同一次点击若弹出了窗，即可排除。
  3. **指针其实没落在 Hit 上**（坐标换算/缩放）⇒ 用 ClickLog 的「画布 px」与卡面矩形对一眼（10-18 的日志显示换算正常：屏幕 (2559,808) → 画布 (1919.3,474)）。
  4. **`WindowButton.Update()` 没跑**（组件被禁/不在激活链）⇒ 与 2 同：点击也死。
- ⚠️ 本轮**没有**做像素比对（不跑 player）。

**D. 新变量：FTUE 暗幕盖在上面（2026-10-18 当天新增）。**
- 判据：`MenuTutorialFlow.Attach(this)` 在 `MainMenuRuntime.Build()` 末尾（`MainMenuRuntime.cs:589`，第五十一会话新增）；
  聚光灯 = `MenuTutorialHighlight`，整屏 quad，队列 **`QShade = 4100`**、`m_Color.a = 0.8235`（`MenuTutorialHighlight.cs:55-60`，判据来自原版 `ScreenHighlightPosition`）。
- 它**没有 `WindowButton`**（`grep` 零命中）⇒ **不吃点击**，但**会盖住 4% 的色偏**：卡片只剩 18% 亮度时，×0.96 等于 ×0.993 观感。
- ⇒ 若用户是在「引导亮着」时试的，**悬停必然看不出**、点击仍应开窗。
- **怎么判**：看 `MenuTutorialHighlight.IsVisible` / 屏上有没有压暗；或 `-wfskipftue` 起一次再试。

**E. 原版「有、我们没有」的项：无。**
- 逐项核过：ColorTint ✅ 同值 · Pressed ✅ 同值 · Fade 0.1 ✅ · 换图（原版没有）· Animator（原版没有）· 额外高亮件（原版没有）· 音效（默认音 ✅）。
- ⚠️ 唯一「可能算差异」的是**按下态能否被看见**：原版按下的 22% 在窗口弹出前有几帧；我们同样有几帧 —— 时长取决于 `OpenMode` 建窗耗时（`PracticeModePopup.Create` 等）。

### 4·4 结论（一句话）

**模式卡不是「没实现反馈」，而是「原版的反馈本来就只有 4%/22% 的色偏，且点击会立刻被新窗盖住」。**
我们与原版**逐值相同**；若用户在真机上确认「连按住时的 22% 都看不到」，按 §4·3-C 的四条判据查（首查 `MissingArt` 与 `cardArtQ`，再查 ClickLog 是否 `空点`）。

---

## 五、没查清（⛔ 不猜）

1. **「我们这一颗 ↔ 原版哪一颗」的映射仍然缺**（A15 那笔老账）⇒ 无法逐颗核 `m_Colors` 的非默认覆盖。
   `WindowButton.HighlightKey` 只给导航 5 颗覆盖过；**其余 145 颗非默认色**（`EverguildToggle` 一族 51 绿 / 36 浅蓝 / 19 青 / 13 灰…）仍无从对号。
2. **`MissingSwapArt` / `MissingPressedArt` 今天是否为空**：只有运行时/自检能读。**P2-14 的「Bind 了但没图 ⇒ 零反馈」是否正在发生，本轮判不了。**
3. **§三 P1-13 那 18 处无 `target` 的站点，原版对应的 `m_Transition` 没有逐条核**（只核了：Toast = 0、GiveFeedback = 2、Help Button = 1、Leaderboard 头像 = 1、ChatPreview = 0、输入框非按钮）。
4. **卡牌/卡组/装饰三格的 hover 驱动点**：原文只读到 `CollectionCard/CollectionDeck/CollectionCosmetic.HoverHighlight(bool)` 的**实现**，
   **谁在 `OnPointerEnter/Exit` 里调它、是否有开关字段（`CollectionDeck` 的 `+0xb8`）**没有逐条查 ⇒ 只知道「原版有这条反馈」，不知道它的前置条件。
5. **战斗内是否还有别的悬停层**（例如 `Card2DController` 给手牌/场上卡做的悬停）：本轮只扫了按钮族（`Selectable`），**没有扫卡的 hover 路径**。
6. **模式卡在 FTUE 暗幕下的实际观感**：没跑 player，判据只有「暗幕 α 0.8235 + 队列 4100」。
7. **`MenuDraw.Hit` 的 `target` 与 `TintOn` 两条路的相互作用**：同一颗若既 `Bind`（关 tint）又 `TintOn`，色偏会被 `Bind` 关掉 —— **本轮没有逐站点查有没有这种双写**。
8. **`EverguildButton` 的 C# 事件（`OnPointerEnterTriggered` / `PointerOverEvent`）在原 prefab 里有没有订阅者**：
   全量反编译里**只有 add/remove 定义、没有任何 `OnPointerEnterTriggered += …` 的订阅点**，但 prefab 的 persistent call 没有逐颗解过 ⇒ 只能说到「没找到订阅证据」。
9. **设置窗「页签悬停/选中两态互换」那条老账（A17/A21）今天的实际状态**：`SettingsWindow.cs:1936` 传的是 `bg + ArtTabBg`，
   具体 `ArtTabBg` 的取值与 `SetNormalTex` 的调用次序没有在本轮逐行复核 ⇒ **不敢断言已经改对**。
