# W · 音效线 28 条缺口修复（写手报告）

> 日期：2026-10-18 · 修复写手（音效线）· 输入 = `资料/普查产出_1018/AUD_音效线_完整性复核.md`（只读复核，408 行）。
> 范围：**按 AUD 报告逐条修**（A1/A2/A3 · B1–B13 · C1–C9 · D 三条 cue）。
> 纪律：**没有跑 Unity / 没有跑 `*.Run` / 没有起 player / 没有动 git 写操作 / 没有改正本**；
> 只跑了秒级类型检查（自己的 `TMPDIR=/tmp/wf_audio`）：**运行时 0 / 编辑器 0**（改完首轮与末轮各跑一次，都是 0/0）。
> 没查到的写「没查到」，判据冲突/禁碰文件的写进 §四，**没有猜**。

---

## §一 逐条表（28 条）

| # | 窗/件 | 原版值（cue / 组 / 字段，带 pid） | 改了什么（文件:行） | typecheck |
|---|---|---|---|---|
| **A1** | `CardDetailPopup` 开窗音 | 场景 `bundle_scenes_scenes_mainmenuwarpforge` 的 `Card Displayer Menu For Menu` 根 MB（`CardDisplayWindow : GameWindow`，pid **2033**）`openSound = PPtr(fileID 6, pid 4006604330728631164)` = cue **`CardExpand`**（clip `CardExpand`，pitch 0.8–1.1，ttp 0.1）· **FX(0)** | `Shell/CardDetailPopup.cs:662` `Open()` → `WindowSounds.PlayFx(CardExpandCue, …)`；常量 `Shell/WindowSounds.cs:80` | 0/0 |
| **A2** | `RankedDivisionChangeWindow` 开窗音 | `bundle_menus_assets_all` MB `-4938671765695524530`（`RankedEventDivisionChangeWindowNewSystem : GameWindow`，prefab `Ranked Division Change Window`）`soundOnOpen = PPtr(fileID 6, pid -2236843390636958589)` = cue **`Division Change`**（clip `Division Increase`，ttp 0.01）· 播放点 `_DoPromoteAnimations_d__22.MoveNext.c:32-33` `Play2D(cue, 3)` = **Jingles(3)** | `Shell/RankedDivisionChangeWindow.cs:204` `Open()` → `WindowSounds.PlayJingles(DivisionChangeCue, …)`；常量 `Shell/WindowSounds.cs:100` | 0/0 |
| **A3** | 卡组编辑 3 颗页签（Cards / Deck info / Cosmetics）默认音 | `Deck Editing Menu/Content Area/Sidebar/Window Options/Buttons/{Cards,Cosmetics,Info}` = `EverguildToggle`，空 cue + `useDefaultAudioClipIfNull = 1` ⇒ 切页播默认 `ButtonClick`；`allowClickWhenAlreadySelected = 0` | ✅ **收尾批补齐**（`Deck/DeckRuntime.cs`）：`ClickCueById` 加 `tab_0..2 → DefaultButtonCue`（`:5161-5163`）；`HandleButtons` 已选中守卫（`:5178-5185`）；`:5147-5153` 错推理已订正 —— 见 **§六** | 0/0 |
| **B1** | `CardDetailPopup` 关窗音 | 同 pid 2033 根 MB `closeSound = PPtr(fileID 6, pid 3524674544779823761)` = cue **`Close Card Display`**（ttp 0.1）· **FX(0)**；`useDefaultCloseSoundIfNull = 1` 只在 closeSound 为空时兜底 | `Shell/CardDetailPopup.cs:698` `Close()` → `PlayFx(CloseCardDisplayCue, …)`（闸 = `gameObject.activeSelf`，= 原版 Close 首句）＋ `:607` `useDefaultCloseSoundIfNull = false` 防叠默认 `ButtonClick Back`；常量 `WindowSounds.cs:88` | 0/0 |
| **B2** | 卡牌详情 `Craft`（创建副本） | `…/Card Options Panel/Panel/Crafting Panel/Content/Craft`（EverguildButton）`m_onClickSound = 8549838806721213354` = **`ButtonClick Positive`**（`Upgrade` 同值） | `Shell/CardDetailPopup.cs:1187` 补 `clickCue: WindowSounds.PositiveCue` | 0/0 |
| **B3** | 反馈窗关窗钮 | `Give Feedback Popup menu/Window/Generic Close Button` → `ButtonClick Back`（pid 8084683923307806969） | `Shell/GiveFeedbackPopup.cs:493` 补 `clickCue: WindowSounds.DefaultCloseCue` | 0/0 |
| **B4** | 反馈窗 41 颗复选框 | `…/Questions container/Section */S*Q*/Questions List/Checkbox*` 共 **41** 颗 `EverguildToggle`，`m_onClickSound = 8881786964274599484` = **`Select Toggle`** | `Shell/GiveFeedbackPopup.cs:627` 补 `clickCue: WindowSounds.SelectToggleCue` | 0/0 |
| **B5** | 活动承载窗 P2 表头返回 | `Two Sides Event Window/Event On Going Tab/Game Mode Header With Back Button/Header Back Button` = 空 cue + `useDefaultAudioClipIfNull = 1` ⇒ **默认 `ButtonClick`** | `Shell/ParentEventWindow.cs` P2 返回钮改成**不传 cue**（`MenuDraw.Hit(back, "Hit", …, () => Close())`，去掉原来的 `DefaultCloseCue`） | 0/0 |
| **B6** | 活动承载窗 Collect | `Two Sides Event Window/…/Reward Display/Scoring Bar Event Score Info/Generic Simplified UI Button` → `ButtonClick Positive`（pid 8549838806721213354） | `Shell/ParentEventWindow.cs:907` `clickCue: WindowSounds.PositiveCue` | 0/0 |
| **B7** | 高级商店关窗钮 | `Purchase Premium Window/Generic Close Button Orange` → `ButtonClick Back` | `Shell/PurchasePremiumWindow.cs:608` 补 `clickCue: WindowSounds.DefaultCloseCue` | 0/0 |
| **B8** | 通用弹窗 1 按钮版 | `MessagePopupWindow/Window/Buttons/Generic UI Button` → `ButtonClick Positive` | `Shell/PopUpGameWindow.cs:458` `MakeButton(..., clickCue: PositiveCue)`；形参/赋值 `:491` | 0/0 |
| **B9** | 通用弹窗 2 按钮版 | `MessagePopupWindow2Buttons/Window/Buttons/ButtonLeft` = **Negative**（pid 1387228150816026206）· `ButtonRight` = **Positive**（pid 8549838806721213354） | `Shell/PopUpGameWindow.cs:440/:443` 左 `NegativeCue` / 右 `PositiveCue` | 0/0 |
| **B10** | 排位/遭遇活动「Create deck」 | `RankedEventWindowV2/Ranked Deck Selection/No Deck Text/Generic Simplified UI Button`（`SkirmishModeEventWindow` 同名节点同 cue）= **Positive** | `Shell/LiveOpsEventWindow.cs:586` 补 `clickCue: PositiveCue` | 0/0 |
| **B11** | 练习模式 `Battle!` | `Practice Mode Menu/DeckSelectionContinueButton/Continue Button` → **`MenuStartGame`**（pid -1545367626748338049，clip `StartMatch`） | `Shell/PracticeModePopup.cs:921` `HitOn(..., WindowSounds.StartGameCue)`（`HitOn` 新增 `clickCue` 形参 `:2426`；`StartGameCue` 常量**首次有消费点**） | 0/0 |
| **B12** | 商店卡包格点击 | `Shop Menu Variant/…/Comsetic Item Shop Container/raycast target` = **`Purchase Click`**（价格钮 = 默认） | `Shell/ShopTabPage.cs:563` 补 `clickCue: PurchaseCue`（语义差异仍归界面线） | 0/0 |
| **B13** | 决斗弹窗 `Button Skirmish` | `MessagePopupWindowDuel/Window/Buttons/Button Skirmish` = 空 cue + `useDefault=1`；`Button Classic` = Positive | `Shell/DuelPopupWindow.cs:185` `DuelButton` 新增 cue 形参，Skirmish 传 `null`、Classic 传 `PositiveCue` | 0/0 |
| **C1** | 练习模式返回钮 | `Practice Mode Menu/Back button` = 空 cue + **`useDefaultAudioClipIfNull = 0`** ⇒ 静音 | `Shell/PracticeModePopup.cs:876` `backWb.playClickSound = false` | 0/0 |
| **C2** | 练习模式卡组行 | `…/Deck info/Deck List Drawer/Content/Deck Selector Card Info button_ref` = 空 + `useDef=0` | `Shell/PracticeModePopup.cs:1470` `rowWb.playClickSound = false` | 0/0 |
| **C3** | 活动承载窗 P1 返回 | `Two Sides Event Window/Select Team Tab/Back button` = 空 + **`useDef=0`** | `Shell/ParentEventWindow.cs:677` `p1BackWb.playClickSound = false`（去掉原来的 `DefaultButtonCue`） | 0/0 |
| **C4** | 卡牌详情 `Voice Over Button` | 空 cue + **`useDef=0`** ⇒ 静音（它只播语音） | `Shell/CardDetailPopup.cs:780` `voiceWb.playClickSound = false` | 0/0 |
| **C5** | 卡牌详情 `Show Card Text`（眼睛） | 空 cue + **`useDef=0`** | `Shell/CardDetailPopup.cs:794` `eyeWb.playClickSound = false` | 0/0 |
| **C6** | 反馈窗 `Free feedback/Input field` | `Image + TMP_InputField + EverguildInputField`（**没有 EverguildButton**） | `Shell/GiveFeedbackPopup.cs:519` `freeWb.playClickSound = false` | 0/0 |
| **C7** | 反馈窗开放题 `Text Area` | `Image + EverguildInputField`（无 EverguildButton） | `Shell/GiveFeedbackPopup.cs:701` `openWb.playClickSound = false` | 0/0 |
| **C8** | 卡组信息窗督军钮 | `Deck info Popup/Info Panel/Deck List/Content/Deck Selector Hero Card Info button` = 空 + **`useDef=0`** | ✅ **收尾批补齐**（`Shell/DeckInfoPopup.cs:1366-1373`）：`warlordWb.playClickSound = false` —— 见 **§六** | 0/0 |
| **C9** | `allowClickWhenAlreadySelected = 0` 守卫（64 颗） | `EverguildToggle__OnPointerClick.c` 第一段：`allowClickWhenAlreadySelected==0 && isOn` ⇒ 连按钮音一起跳过 | ✅ 机制：`Shell/WindowButton.cs:907`（字段）/`:914`（`isOn`）/`:843`（守卫，排在 `PlayClickSound()` 之前）。已接线 **11 个站点**（见 §一·C9 表） | 0/0 |
| **D1** | cue `CardExpand` 导入 | 包装 `soundcollection_assets_all/MonoBehaviour/CardExpand.json`（pid 4006604330728631164） | `工具/import_original_sfx.py` `EXTRA_CUES` 收录并**已重跑**（输出「已拷 3 条」）；`Resources/animfx_sounds.json` 现有 **458 条**（455 → 458） | — |
| **D2** | cue `Close Card Display` 导入 | 包装 pid 3524674544779823761 | 同上（clip `Close Card Display`） | — |
| **D3** | cue `Division Change` 导入 | 包装 pid -2236843390636958589（clip `Division Increase`，ttp **0.01**） | 同上；**播放走 Jingles**（见 A2） | — |

### §一·C9 已接线站点（全部 `allowClickWhenAlreadySelected = false` + `isOn` 闭包）

| 窗 | 状态源 | 文件:行 |
|---|---|---|
| 奖励 / 商店 / 收藏 / 社交 左栏页签（共用 `BuildTabButton`，**一次覆盖四扇窗**） | `tabButtons.CurrentVisualIndex` | `Shell/MainMenuSubmenuWindow.cs:712` |
| 设置窗 5 颗页签 | `Current` | `Shell/SettingsWindow.cs:1944` |
| 玩家档案 6 颗页签 | `tabButtons.CurrentVisualIndex` | `Shell/PlayerProfileWindow.cs:368` |
| 成就（Trophies）类型页签 | `_filter`（`Select()` 里那句早退的**声音**半边现在也挡在派发之前） | `Shell/AchievementsMenu.cs:256` |
| 聊天 2 颗频道键 | `tabButtons.CurrentVisualIndex` | `Shell/ChatPanel.cs:438` |
| 排行榜页签 | `CurrentTab` | `Shell/LeaderboardWindow.cs:665` |
| 活动承载窗页签 | `CurrentPageIndex` | `Shell/ParentEventWindow.cs:518` |
| 主菜单 5 颗导航钮 | `SelectedNav` | `Shell/MainMenuRuntime.cs:1028` |
| 锻造页阵营格 | `ForgeData.Selected` | `Shell/ForgeTab.cs:729` |
| 战役页阵营格 | `CampaignData.Selected` | `Shell/CampaignTab.cs:752` |
| 高级商店 `Army Container` | `_selectedArmy`（🆕 新增字段，`Initialize()` 落 0、`SelectArmy` 更新） | `Shell/PurchasePremiumWindow.cs:681` |

---

## §二 关键 diff 说明

1. **三条 cue 的导入链**：`工具/import_original_sfx.py` 的 `EXTRA_CUES` 尾部新增 `CardExpand` / `Close Card Display` / `Division Change`（连判据注释：原版 MB pid + 消费点）。
   重跑后：`Resources/Art/audio/sfx/` 多出 3 个 clip（`Art/` 在 `.gitignore:91` 里，属正常），
   `Resources/animfx_sounds.json` 由 **455 → 458** 条；逐条核对：
   `CardExpand`(pitch 0.8–1.1, ttp 0.1) · `Close Card Display`(1–1, ttp 0.1) · `Division Change`(clip `Division Increase`, ttp **0.01**)。
   ⚠️ `Division Change` **必须走 Jingles**：`WindowSounds.PlayFx` 的播放器写死 FX 组，所以用 `PlayJingles`（= `WarpforgeAudio.PlayJingle`）。

2. **`WindowSounds` 加三个常量**（`Shell/WindowSounds.cs:80/88/100`）——表仍然**只有这一份**；三个常量都带原版 pid / 组号 / clip 参数注释。
   其中 `CardExpand` 的组判据 = `GameWindow.TryOpen` 的 `Play2D(cue, 0)`（与 FTUE 开窗音同一条指令流）；`Division Change` 的组判据 = `_DoPromoteAnimations.MoveNext.c:32-33` 的 `Play2D(cue, 3)`。

3. **卡牌详情窗（5 条一起收）**：开窗 `PlayFx(CardExpand)`；关窗 `PlayFx(CloseCardDisplay)` + `useDefaultCloseSoundIfNull = false`（原版只会响一条；我们的 `closeSound` 是 `AudioClip`、cue 带节流参数 ⇒ 走 cue 表，同 FTUE 先例）。
   关窗那一声加了 `gameObject.activeSelf` 闸 —— 原版 `GameWindow.Close` 首句就是它，关一扇本来就关着的窗**不播音**。
   `Voice Over` / `Show Card Text` 用 `playClickSound = false`（原版 `useDefaultAudioClipIfNull = 0`）。
   `Craft` 补 `PositiveCue`（原版 pid 与 `Upgrade` 同值）。

4. **通用弹窗（B8/B9）**：`MakeButton` 新增末尾可选形参 `clickCue = null`（既有调用只在本文件，两处都显式传了值）；1 钮版 Positive、2 钮版左 Negative / 右 Positive。
   顺手把 `:377` 那条过期日志（「开/关音效没做…默认音在远端音频表」）改成当前事实。

5. **`WindowButton.allowClickWhenAlreadySelected` + `isOn`**：守卫位置 = `absorbOnly` → `interactable` → **本守卫** → `PlayClickSound()` → `onClick()`。
   缺省 `allowClickWhenAlreadySelected = true` ⇒ **既有站点行为逐位不变**；只有逐站点显式置 false + 给 `isOn` 才生效。
   ⚠️ 原版把这条守卫放在 `base.OnPointerClick`（含 `interactable` 判定）**之前**；我们排在 `interactable` 之后（差别只在「不可交互 + 已选中」时我们多打一条既有告警，**都不出声**）。

6. **`HitOn`（Practice）/ `AddHit`（CampaignTab）/ `DuelButton` 的签名扩展**：都只在末尾加可选形参或补返回值（CampaignTab 的局部 `AddHit` 由 `void` 改 `Transform`，既有语句式调用零行为变化），没有改任何既有实参的语义。

---

## §三 归界面线的 5 条（**本批没做**，无音可响）

判据 → `AUD` §五·4：这 5 条不是接线错，是**原版那颗钮我们压根没建**。

1. `Player Profile Window/Ranking/{Current,Highest} Rank/LeaderboardButton`（Positive ×2）。
2. `Inbox Menu/Content/{Reset Button, Claim Button}`（Positive ×2）。
3. `Ranked Deck Selection/DeckSelectionContinueButton/Continue Button`（MenuStartGame）。
4. `Two Sides Event Window/…/Score Bar Line Level 1..5/Chest`（默认音 ×5）—— 我们只有视觉、无命中区。
5. `Missions Tab` 的 3 颗 `raycast target`（Purchase Click）—— 语义不同/未建。

---

## §四 没查到 / 需要主对话处理（禁碰文件与判据冲突）

### 四·1 `Deck/DeckRuntime.cs`（A3 + C9 三颗页签）—— ✅ **收尾批已落地（见 §六）**

> 下面是**首轮写的改法**（原文保留作对照）；实际落点行号以 §六 为准。

- **A3（该响没响）**：`ClickCueById`（`:5149-5155`）只收 `hdr_back/hdr_clear/foot_done/hdr_filters` 四条；
  `HandleButtons`（`:5185-5190`）里 `case "tab_0..2"` 调 `SetTab` 但**不播**。
  ⇒ **改法**：给 `ClickCueById` 加三条
  `{ "tab_0", WindowSounds.DefaultButtonCue }, { "tab_1", … }, { "tab_2", … }`（原版三颗 = 空 cue + `useDefaultAudioClipIfNull = 1`）。
  ⚠️ 顺带订正 `:5145` 那句注释「表外的 key（页签 `tab_*` / 改名框）**原版没有显式 cue ⇒ 不播**」：
  推理不成立（**没有显式 cue ≠ 静音**）；对**改名框**（输入框）静音才是对的（它不是 EverguildButton）。
- **C9（多响）**：`_tab`（`:981`）保存当前页；`HandleButtons` 在 `switch` **之前**就 `PlayClickCue(key)`（`:5171`）。
  ⇒ **改法**：`PlayClickCue(key)` 之前加一句「`key` 是 `tab_N` 且 `N == _tab` ⇒ 直接 `return true`」，
  或给 `tab_*` 的命中件挂 `allowClickWhenAlreadySelected = false` + `isOn = () => _tab == N`（后者与本次 11 处同形，但本文件是禁碰）。

### 四·2 `Shell/DeckInfoPopup.cs:1366-1368`（C8）—— ✅ **收尾批已落地（见 §六）**

> 下面是**首轮写的改法**（原文保留作对照）。

- 原版 `Deck info Popup/Info Panel/Deck List/Content/Deck Selector Hero Card Info button` = 空 cue + **`useDef=0`** ⇒ 静音。
- **改法**（3 行，局部 `Hit` 返回 `Transform`，`:2829`）：
  ```csharp
  var w = Hit(root, root, "WarlordHit", …, OpenWarlordDetail);
  var wb = w != null ? w.GetComponent<WindowButton>() : null;
  if (wb != null) wb.playClickSound = false;
  ```

### 四·3 判据冲突的两处 —— ✅ **主对话已裁定「照原版」，收尾批已执行（见 §六）**

> 裁定 = **按原版 `allowClickWhenAlreadySelected = 0`**：已选中再点 = 什么都不做（不播音、不改值、不取消）。
> 我们原来的「再点=取消」是自设计 ⇒ 已删。下面是首轮记录的冲突原文（保留作留痕）。

1. **反馈窗评分钮**（`GiveFeedbackPopup.BuildNumeric` 的 `Feedback Scoring Button`）：
   `AUD` §二 记它 `allowClickWhenAlreadySelected = 0`，而我们 `SetScore`（`:782-786`）的语义是
   **「再点同一颗 = 取消」**（注释写「原版 Toggle」）—— 两者**互斥**（allowClick=0 时再点根本不会派发 ⇒ 取消不了）。
   ⇒ 本批**没有**给评分钮挂守卫（挂了会**改掉取消行为**）。需要主对话/复核代理确认原版到底是「单选不可取消」还是「可取消」。
2. **`LiveOpsEventWindow` 的 `Ranked Army Selector Container V2`**（`:812` 的 `PickArmy`）：
   `AUD` 把 prefab 列进 allowClick=0 的 64 颗；但我们的 `PickArmy` 是 **`ArmyIndex == i ? -1 : i`**（再点 = 取消筛选，
   即「允许关闭」）—— 与 allowClick=0 互斥。⇒ 未挂守卫，同上待核。

### 四·4 没查到的

- `BaseEventPopup` 的**运行期开窗音**（LiveOps 资产 index 8）：`AUD` §四·3 已确认**本地没有判据**（远端数据），本批不编。
- `MissionsTab` 的 info 圆钮（`:1543`）是否 EverguildButton：`AUD` §四·5 **未逐颗核** ⇒ 没有动。

---

## §五 顺手发现（已落地的顺手修 + 留给调度台的）

**已顺手修（同一批文件内、判据齐、零风险）**
1. `Shell/BaseEventPopup.cs:51-52` 的过期注释（「`closeSound` / `useDefaultCloseSoundIfNull` 我们没有建模」）——
   已按 `W_G`（2026-10-17）的事实改写：整条链**已建模**，本族两字段为空 ⇒ 走默认 `ButtonClick Back`（正确）。
2. `Shell/PopUpGameWindow.cs:377` 的过期日志 —— 改成当前事实（开窗音原版为空 / 关窗默认 `ButtonClick Back` / 按钮 cue 已接）。
3. `Shell/WindowSounds.cs` 的 `StartGameCue` 常量**首次有了消费点**（`PracticeModePopup` 的 `Battle!`）；
   `Purchase2Cue` 仍 0 处使用（`Expansion Pass` 整体未建，见 `AUD` §四·7，**不是缺口**）。
4. `DeckRuntime.cs:5145` 那句错误注释（「原版没有显式 cue ⇒ 不播」）**仍留着**（禁碰文件）—— 见 §四·1，建议跟 A3 一起改。

**留给调度台/其他线**
5. `LeaderboardWindow` 的 `SelectTab` 若被程序化调用（不经 `TabButtons.Click`），`CurrentTab` 与命中件的 `isOn` 会脱钩 ——
   本批的 `isOn` 直接读 `CurrentTab`，所以**不会**误挡；只有「共享 `CurrentVisualIndex`」那几处（奖励/商店/收藏/社交/档案/聊天）
   在**绕过 `TabButtons.Click` 直接换页**时可能放过一次（最坏 = 多响一声，**不会**误挡真正的点击）。
   今天没有发现这样的调用点（`tabButtons.Click(0)` 初始化 + 点击两条路都经过 `Click`）。
6. `WindowButton.Click()` 的守卫次序（`interactable` 先于 `allowClickWhenAlreadySelected`）与原版相反 —— 见 §二·5，
   行为差异只在「不可交互 + 已选中」时多一条既有告警；**不影响声音**。如要逐字对齐可把守卫提到 `_interactable` 之前（本批没动，避免改既有告警路径）。

---

## §六 收尾批（2026-10-18 · 主对话放行后）

> 触发：主对话把 `Deck/DeckRuntime.cs` 与 `Shell/DeckInfoPopup.cs` 的所有权**放出来**，
> 并对两处判据冲突裁定「**照原版（`allowClickWhenAlreadySelected = 0`）**」。
> 本批**没有跑 Unity / 自检 / player**，只跑秒级类型检查：**运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_audio`）。

### 六·1 A3 卡组编辑 3 颗页签（`Deck/DeckRuntime.cs`）

| 改了什么 | 文件:行 |
|---|---|
| `ClickCueById` 新增 `tab_0/tab_1/tab_2 → WindowSounds.DefaultButtonCue`（判据：原版 3 颗 `EverguildToggle` = 空 cue + `useDefaultAudioClipIfNull = 1` ⇒ 默认 `ButtonClick`） | `Deck/DeckRuntime.cs:5161-5163` |
| `HandleButtons` 在 `PlayClickCue(key)` **之前**加「已选中页签 ⇒ 整下跳过」守卫（原版 `EverguildToggle__OnPointerClick.c` 第一段：连按钮音一起不响）——键名解析 `tab_N` ↔ `_tab`（`:981`） | `Deck/DeckRuntime.cs:5175-5186` |
| 订正 `:5147-5153` 那句错推理「表外的 key（页签 `tab_*` / 改名框）原版没有显式 cue ⇒ 不播」：**没有显式 cue ≠ 静音**；页签 = 默认音，改名框（输入框、非按钮）仍然不播 | 同文件注释 |

⚠️ 本窗**不走 `WindowButton`**（点击在 `HandlePointer → HandleButtons` 这条链上）⇒ 守卫写在派发点，
与 `WindowButton.allowClickWhenAlreadySelected` 是**同一条原版判据的两个落点**（两处都写了出处）。

### 六·2 C8 卡组信息窗督军钮（`Shell/DeckInfoPopup.cs`）

| 改了什么 | 文件:行 |
|---|---|
| 捕获 `WarlordHit` 的返回值并 `playClickSound = false`（原版 `Deck Selector Hero Card Info button` = 空 cue + `useDefaultAudioClipIfNull = 0` ⇒ 静音；改前播默认 `ButtonClick` = 多响） | `Shell/DeckInfoPopup.cs:1366-1373` |

### 六·3 判据冲突①：反馈窗评分钮（`Shell/GiveFeedbackPopup.cs`）

| 改了什么 | 文件:行 |
|---|---|
| `SetScore` 删掉「再点同一分 = 取消」的自设计语义（原 `_score[qid] == idx+1 ? 0 : idx+1` → **幂等写入 `idx+1`**） | `Shell/GiveFeedbackPopup.cs:794-802` |
| 评分钮命中件挂 `allowClickWhenAlreadySelected = false` + `isOn = () => _score[qid] == idx+1`（已选中再点 = 不播音、不改分、不取消） | `Shell/GiveFeedbackPopup.cs:659-670` |

- **删除「取消」后是否有清除入口**：原版**没有**（评分是单选、一旦作答只能改分不能清空；问卷本身要求必答，
  `AllRequiredAnswered` 只看 `v != 0`）⇒ **没有补任何入口**。
- **断言/测试依赖检查**：`grep` 全 `Editor/` 无任何针对 `SetScore` / 问卷评分的断言（只有
  `Editor/MainMenuScene.cs:12727` 一条「问卷开得出来」）⇒ **无需改断言**。
- ⚠️ 既有 `AnswerCount` 用 `_score.Count` 计数、`BuildNumeric` 仍会为每题预置 `0` 条目 —— 与本次改动**无关**（改前改后同值），没动。

### 六·4 判据冲突②：LiveOps 阵营筛选格（`Shell/LiveOpsEventWindow.cs`）

| 改了什么 | 文件:行 |
|---|---|
| `PickArmy` 删掉「再点同一格 = 取消筛选」（原 `ArmyIndex = (ArmyIndex == i) ? -1 : i` → **`ArmyIndex = i`**） | `Shell/LiveOpsEventWindow.cs:1251-1260` |
| 阵营格命中件挂 `allowClickWhenAlreadySelected = false` + `isOn = () => ArmyIndex == idx`（已选中再点 = 不播音、不取消） | `Shell/LiveOpsEventWindow.cs:806-819` |
| 同步断言：`Editor/MainMenuScene.cs` 那条「再点一次 ⇒ 取消筛选（−1 / 内容高 816）」改成「**保持不变（0 / 657）**」，并写明「2026-10-18 按原版 allowClick=0 订正」 | `Editor/MainMenuScene.cs:2904-2909` |

- **删除「取消」后是否有清除入口**：**原版没有** —— 本轮 `python -I 工具/menu_dump.py bundle_menus_assets_all
  "SkirmishModeEventWindow" --depth 25 --no-sprite` 现读整棵树：`Ranked Army Selector` 下只有
  `Factions Title` + `Army Selector`（ScrollRect）→ `Viewport` → `Army Content`（`ToggleGroup` + `GridLayoutGroup`），
  **没有任何 All / Clear / Reset 节点**。初始「不限」= 我们 `Open()` 里的 `ArmyIndex = -1`（原版 ToggleGroup
  初始全关、`m_AllowSwitchOff` 的值决定能否全关，prefab 本批未再逐值读 —— **不影响本次结论**：
  原版没有清除入口，我们也不补一个）。⇒ 真要回到「不限」= **重开这扇窗**（`Open()` 会重置），与原版一致。

### 六·5 typecheck

```
TMPDIR=/tmp/wf_audio bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 --- 运行时错误数: 0
--- 编辑器程序集 --- 编辑器错误数: 0
```

### 六·6 收尾后还剩什么

- **28 条缺口全部收口**（A3/C8 本批补齐；两处冲突按原版裁定执行）。
- 仅剩 **§三 的 5 条界面线缺口**（原版那颗钮没建、无音可响）—— 归界面线，不在音效线范围。
- 仍成立的风险提示见 §五·5 / §五·6（共享 `CurrentVisualIndex` 的极端情况；守卫次序与原版相反但只影响一条既有告警）。
