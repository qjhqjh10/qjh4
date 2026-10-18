# AUD · 音效线完整性复核（只读）

> 日期：2026-10-18 · 只读代理 · **没有改任何代码/资源/正本，没有跑 Unity / typecheck / git**。
> 目标：证明或推翻 `资料/任务线/音效线/任务总表.md` 的「**本线当前无未完成活**」。
> 方法：
> 1. 用 UnityPy 直读原版 bundle（`d:/2/Warhammer 40k Warpforge/.../aa/StandaloneWindows64/`），
>    扫 `menus` / `generalgamewindows` / `scenes_scenes_mainmenuwarpforge` 的**全部** `MonoBehaviour`，
>    按 `m_Script → MonoScript.m_ClassName` 反查类名，按 `m_GameObject → GameObject.m_Name` 取节点名，
>    按 `m_Father` 反解节点全路径。产出 `/tmp` 下的 `windows.tsv` / `allbuttons.tsv`（临时文件，未入库）。
> 2. cue PPtr（`m_PathID`）→ cue 名：在 `bundle_soundcollection_assets_all` 的 610 个 cue 包装上按 PathID 反查
>    （不是只在 `AudioClip/` 里按 pid 找 —— 那条是 2026-10-17 之前的错因）。
> 3. 我方接线：`Shell/*.cs` 的 `clickCue` / `WindowButton` / `MenuDraw.Hit` / `GameWindow.AddHit` 逐站读；
>    cue 表以 `Shell/WindowSounds.cs` 为准，导入表以 `Resources/animfx_sounds.json` 为准。

**实测基线（可复现）**：原版侧 **102 个**带 `openSound/closeSound` 的窗实例 ·
**1022 个** `EverguildButton|EverguildToggle`（869 + 153）· **442 颗显式 `m_onClickSound`** · **10 个 cue 名**
（`ButtonClick Positive` 150 · `Select Toggle` 115 · `ButtonClick Back` 68 · `Purchase Click` 67 ·
`ButtonClick` 15 · `SelectAvatar` 11 · `ButtonClick Negative` 8 · `MenuStartGame` 6 · `UI Send Message` 1 · `UI Purchase 2` 1）。

---

## §一 结论

**「本线当前无未完成活」不成立。** S01/S02/S03 已落的接线都在、没有回退，但**它没覆盖到的壳侧站点**仍有缺口：

| 组 | 条数 | 摘要 |
|---|---|---|
| **该响没响** | **3** | 卡牌详情窗 `openSound=CardExpand` · 段位升降窗 `soundOnOpen=Division Change`（Jingles）· 卡组编辑 3 颗页签（原版默认按钮音，我们一声不响） |
| **响错 cue** | **13** | 卡牌详情关窗/Craft · 反馈窗关窗 + 41 颗复选框 · 活动承载窗（P2 返回 / Collect）· 高级商店关窗 · **通用弹窗 1/2 按钮版（每次 ShowPopUp 都中）** · 排位活动「Create deck」· 练习模式 `Battle!` · 商店卡包格 · 决斗窗 Skirmish |
| **多响** | **9** | 练习模式返回 / 卡组行 · 活动承载窗 P1 返回 · 卡牌详情 `Voice Over` / `Show Card Text` · 反馈窗两处输入框 · 卡组信息窗督军钮 · **`allowClickWhenAlreadySelected=0` 守卫未实现（64 颗 Toggle）** |
| **cue 缺失** | **3** | `CardExpand` / `Close Card Display` / `Division Change` 三条 cue 不在 `animfx_sounds.json`（455 条里都没有；clip 本地都在，可导入） |

> 另有 **5 条界面线缺口**（不是音效接线错，而是原版那颗钮我们压根没建 ⇒ 无音可响）见 §五·5。
> 另有 **1 条既有普查记录是错的**（R3 §六·7 说 Toast 原版没有 `EverguildButton`）见 §四·2。

**已确认没有问题的（不改）**：
- 默认开窗音链（`GameWindow.TryOpen` 只在 `openSound != null` 时播）· 默认关窗音链（`GameWindow.Close` 守卫后、`PlayCloseSound`）·
  `useDefaultCloseSoundIfNull=0` 的两扇（`CollectionWindow.cs:155` / `ShopWindow.cs:120` 已显式关掉）。
- TFUE 开窗音（`FTUESelectDeckWindow.cs:233` · `Draft Mode Progress Jingle` · FX 组）· 开包窗 12 条（W_G）。
- S01 的 73 处壳侧站点 + `DeckRuntime` 12 颗（逐组核对在 §二）。
- 压暗层不响（`MenuDraw.ShadeHit` → `WindowButton.PlayClickSound` 里 `WasShadeHit` 跳过，`WindowButton.cs:883`）；
  吸收层不响（`absorbOnly`，`:880`）；开包窗的卡不响（`playClickSound=false`，`BoosterPackOpenWindow.cs:704`）。
- Toast 的 4 颗命中区**是默认按钮音（正确）**——原版 `Toast` 等 4 个 prefab 根就是 `EverguildButton`（§四·2）。

---

## §二 逐窗覆盖表

> 「原版」列 = prefab 根的窗级声音字段 + `EverguildButton|EverguildToggle` 统计（`总数/显式数`，显式 = `m_onClickSound` 非空）。
> 「我们」列 = 已接的显式 cue 站点（`文件:行`）；没写 = 该窗没有显式 cue 站点（走默认音，通常即正确）。
> 只列**我们建过/注册过**的窗；同一 prefab 的多个变体合并成一行并注明。

| 窗（类名） | 我们建的文件 | 原版 prefab ▸ 声音字段（open / close / 默认关 / 按钮 总/显式） | 我们的接线 | 差异 |
|---|---|---|---|---|
| `GameWindow`（基类） | `Shell/GameWindow.cs` | 家族普查 **102** 实例：95 空+默认 · 3 个 `useDef=0`（Collection/Shop/MainMenu）· 2 个显式关（`Login Window`×2 = `ButtonClick Back`，等价默认）· 1 个显式开（FTUE）· 1 个开+关都显式（`Card Displayer Menu For Menu`） | `closeSound`/`useDefaultCloseSoundIfNull` `:79/95` · 关窗音 `:695-699,759-768` · 开窗音 `:490` | **`CardDisplayWindow` 的 open/close 未接**（§三 A1/B1）；其余一致 |
| `WindowButton`（全局命中件） | `Shell/WindowButton.cs` | 每颗 `EverguildButton|Toggle`：`m_onClickSound` 有→播它；空且 `useDefaultAudioClipIfNull=1`→默认 `ButtonClick`；`useDefault=0`→**静音**；Toggle 的 `allowClickWhenAlreadySelected=0 && isOn`→**连 OnPointerClick 都不进**（`EverguildToggle__OnPointerClick.c` 第 1 段） | `clickCue` `:876` · `playClickSound` `:869` · `PlayClickSound` `:878-885` | **`allowClickWhenAlreadySelected` 未建模**（64 颗 Toggle，§三 C9） |
| `CollectionWindow` | `Shell/CollectionWindow.cs` | `Collection Menu Variant`：open/close 空 · **`useDef=0`** · 33/29（SelectToggle 16 · Positive 7 · ButtonClick 4 · Back 2） | `:155` 显式关默认关窗音 · `:1878` Back（左右箭头）· `:2450/:2616/:3659` SelectToggle（筛选/搜索/卡背阵营）· `:2941/:3233/:3401/:3403` Positive（清筛选/关窗/导入/新建） | ✅ |
| `ShopWindow` / `ShopTabPage` | `Shell/ShopWindow.cs` | `Shop Menu Variant`：open/close 空 · **`useDef=0`** · 4/2（`Comsetic Item Shop Container/raycast target` = Purchase Click · tab toggle = ButtonClick+allowClick=0） | `:120` 显式关默认关窗音；tab 走默认音（正确） | ⚠️ 卡包格 `InfoHit` 播默认音，原版容器是 `Purchase Click`（§三 B12，语义也不同） |
| `SettingsWindow` | `Shell/SettingsWindow.cs` | `Main Menu Settings Window`：16/57（SelectToggle 14 · Back 2）· 5 颗页签 allowClick=0 | `:1733/:3110` Back · `:1937/:3176/:3177/:4430` SelectToggle | ✅（allowClick 见 C9） |
| `ChatPanel` | `Shell/ChatPanel.cs` | `ChatPanel`：8/8（Positive 5 · Send · Back · SelectToggle） | `:288/:324/:353/:432` | ✅ |
| `PlayerProfileWindow` + 页 | `Shell/PlayerProfileWindow.cs` · `AvatarTab` · `TitleTab` · `AchievementsMenu` · `ProfileTab` | `Player Profile Window`：19/33（SelectToggle 11 · SelectAvatar 4 · Positive 2 · Back 2）· 6 页签 allowClick=0 | `:362/:415` · `AvatarTab:289/305` · `TitleTab:229/290` · `AchievementsMenu:248`（5 颗循环）· `ProfileTab:979` | ⚠️ **2 颗 `Ranking/*/LeaderboardButton` = Positive 未建**（界面缺口，§五） |
| `LeaderboardWindow` | `Shell/LeaderboardWindow.cs` | `RankedSkirmish/ClassicLeaderboardPopup` + `DraftLeaderboardPopup`：13 显式（SelectToggle 7 · Positive 3 · Back 3） | `:658/:714/:736`（循环覆盖） | ✅（Draft 变体未建） |
| `DeckInfoPopup` | `Shell/DeckInfoPopup.cs` | `Deck info Popup`：12/8（Positive 3 · Back 5）· 1 颗 `Deck Selector Card Info button` **`useDef=0`（静音）** | `:1535` Positive（3 颗循环）· `:1642` Back（5 颗循环）· `:1711` Back | ⚠️ `WarlordHit:1366`（= Hero Card Info button，`useDef=0`）播默认音 → 多响（§三 C8） |
| `DeckSelectionPopup` | `Shell/DeckSelectionPopup.cs` | `Deck Selection Popup` 1/1 Back；`…with Tabs` 7/2（Back + Positive） | `:584` Positive · `:676` Back | ✅ |
| `DeckEditingWindow` / `DeckRuntime` | `Shell/DeckEditingWindow.cs` · `Deck/DeckRuntime.cs` | `Deck Editing Menu`：18/12（SelectToggle 9 · Positive 3）· 3 颗 Window Options 页签 = **空 cue + 默认音 + allowClick=0** · 3 颗 Deck Selector 卡 = 静音 | `ClickCueById` `:5149-5152` → `:5160`；筛选 `:7391/:7555` | ⚠️ 3 颗页签（`tab_0..2`）**一声不响**（§三 A3） |
| `CardDetailPopup` | `Shell/CardDetailPopup.cs` | 场景 `Card Displayer Menu For Menu`（`CardDisplayWindow`）：**open=`CardExpand` · close=`Close Card Display`** · 6/4（Back 2 = 异画左右 · Positive 2 = Craft/Upgrade）· **`Voice Over Button` / `Show Card Text` 都是 `useDef=0`（静音）** | `:649` Open（无音）· `:674` Close→基类默认 · `:1153` Craft（无 cue）· `:1206/:1221` Positive · `:1551` Back · `:751` VoiceHit · `:763` ShowTextHit | ❌ **5 条**：开窗音缺 / 关窗音错 / Craft 错 / Voice 多响 / Eye 多响（§三） |
| `BaseOfferPopup` | `Shell/BaseOfferPopup.cs` | 21 个 `General Basic Offer Popup*` 变体：每个 4-6/3（Back · Positive(Preview) · Purchase） | `:770` Back · `:985` Purchase · `:1055` Positive | ✅ |
| `OfferContainer` | `Shell/OfferContainer.cs` | `* Offer Container`：1/1 Purchase Click | `:1364` Purchase | ✅ |
| `BoosterInfoPopup` | `Shell/BoosterInfoPopup.cs` | 3/2（Back + Purchase；1 颗默认） | `:379/:615` | ✅ |
| `BoosterPackOpenWindow` | `Shell/BoosterPackOpenWindow.cs` | 窗级空+默认；其余声音在子 prefab 字段（clickSound / newCardSound / contentByRarities / openBoosterPack / soundOnClose） | `:417/:480/:785/:829/:860` + `:704` 卡不响 | ✅（W_G 已复核；12 条 cue 都在表里） |
| `BaseEventPopup` | `Shell/BaseEventPopup.cs` | 6 变体：每个 1 颗整屏 `Collider` EverguildButton（空 cue → 默认音）· openSound 可由 **LiveOps 资产 index 8** 运行期喂 | `:315` ColliderHit（默认音）· `:288` 暗底 ShadeHit（不响） | ✅（`OpenSound` 资产在远端，本地无数据 —— §四·3） |
| `PopUpGameWindow` | `Shell/PopUpGameWindow.cs` | `MessagePopupWindow`：1/1 = **Positive**；`MessagePopupWindow2Buttons`：2/2 = **Left Negative · Right Positive** | `:480` 手工 `AddComponent<WindowButton>()`，**无 cue** | ❌ **全部弹窗都响错**（§三 B8/B9） |
| `PromptPopup` | `Shell/PromptPopup.cs` | `GenericPromptWindow`：2/2 都空 cue → 默认音 | `:255` 手工 WindowButton（默认） | ✅ |
| `GenericOptionsPanel` | `Shell/GenericOptionsPanel.cs` | 1/1 Positive | `:494` | ✅ |
| `MissionRerollPopup` | `Shell/MissionRerollPopup.cs` | `ReRollPopup Variant`：2/1（ButtonLeft = Negative；`Generic UI Button` 默认） | `:342` Negative · Confirm 走默认 | ✅ |
| `DuelPopupWindow` | `Shell/DuelPopupWindow.cs` | `MessagePopupWindowDuel`：3/2（Back 关 · **Skirmish = 空 cue 默认 · Classic = Positive**） | `:221` Back · `:248` **两颗都 Positive** | ❌ Skirmish 响错（§三 B13） |
| `DailyRewardPopup` | `Shell/DailyRewardPopup.cs` | `Daily Reward Popup`：10/0（全默认音） | `:401/:456/:470` 手工 WindowButton（默认） | ✅ |
| `DailyStreakPopup` | `Shell/DailyStreakPopup.cs` | 2/1 Positive | `:622` | ✅ |
| `RewardWindow` | `Shell/RewardWindow.cs` | 1/1 Positive | `:1192` | ✅ |
| `RewardsWindow`（壳） | `Shell/RewardsWindow.cs` | `Rewards Base Submenu Variant`：61/10（4 颗页签 ButtonClick+allowClick=0 · Positive 3 · Purchase 3） | 无站点（页签默认音；Positive/Purchase 在三个子页） | ✅/⚠️ 子页见 `MissionsTab` |
| `MissionsTab` | `Shell/MissionsTab.cs` | `Missions Tab`：52/3（3 颗容器 `raycast target` = Purchase Click，其余默认） | 垃圾桶 `:812`（默认音，与原版 `Trash mission` 一致）；tooltip `:1543` 默认音 | ⚠️ 3 颗 Purchase Click 未建（界面缺口）；tooltip 颗原版是否 Everguild 待核（§四·5） |
| `CampaignTab` | `Shell/CampaignTab.cs` | `Campaign Tab` 3/3 Positive；`Campaign Army Item Button` SelectAvatar | `:528/:1116` Positive · `:746` SelectAvatar | ✅ |
| `ForgeTab` | `Shell/ForgeTab.cs` | `Forge Menu Reward Button` Positive · `Forge Army Item Button` SelectAvatar | `:892/:723` | ✅ |
| `AllianceEventScorePanel` | `Shell/AllianceEventScorePanel.cs` | 3/3 Positive | `:136/:171/:182` | ✅ |
| `AllianceMemberOptionsPopup` | `Shell/AllianceMemberOptionsPopup.cs` | `Member Options Panel` 8/8 Positive | `:422`（8 颗循环） | ✅ |
| `FriendsTab` / `SocialWindow` | `Shell/FriendsTab.cs` · `SocialPage.cs` | `Social Submenu Variant` 30/4（Add Friend/Instant duel = Positive；2 颗页签 = ButtonClick+allowClick=0）· 6 颗 allowClick=0 | `:203/:214` Positive；页签默认音 | ✅（allowClick 见 C9） |
| `BattleLogPopup` | `Shell/BattleLogPopup.cs` | 1/1 Back | `:168` | ✅ |
| `ImportDeckPopup` | `Shell/ImportDeckPopup.cs` | 3/1 Back | `:284` | ✅ |
| `InboxWindow` | `Shell/InboxWindow.cs` | `Inbox Menu` 3/3（Back 1 · Positive 2） | `:435` Back；行 `:565` 默认音 | ⚠️ Reset / Claim（Positive）未建（界面缺口） |
| `ItemDisplayWindow` | `Shell/ItemDisplayWindow.cs` | **0 颗按钮** · close 空 + 默认关 | 只有 ShadeHit/Absorb（都不响） | ✅ |
| `TutorialMessagePopup` | `Shell/TutorialMessagePopup.cs` | 1/1 空 cue（`…/Collider` EverguildButton → 默认音） | `:322` ColliderHit 默认音 | ✅ |
| `WelcomePopup` | `Shell/WelcomePopup.cs` | 1/1 空 cue（`…/Collider`） | `:233` 默认音 | ✅ |
| `WhereToGetCardsPopup` | `Shell/WhereToGetCardsPopup.cs` | 2/2（Close = Back · Header Toggle = ButtonClick） | `:359/:545` | ✅ |
| `FTUESelectDeckWindow` | `Shell/FTUESelectDeckWindow.cs` | **open=`Draft Mode Progress Jingle`** · close 空+默认 · 2 颗按钮都空 cue（默认音） | `:233` open · `:302` Continue 默认音 · `FTUESelectDeckContainer:253` Select | ✅ |
| `RankedDivisionChangeWindow` | `Shell/RankedDivisionChangeWindow.cs` | **`soundOnOpen` = `Division Change`（Jingles 3）**（`DoPromoteAnimations.MoveNext` 播）· open/close 空 · **0 颗按钮** | `Open()` `:188` 无音 | ❌ 该响没响 + cue 缺失（§三 A2） |
| `RankedNewSeasonWindow` | `Shell/RankedNewSeasonWindow.cs` | open/close 空 · 默认关 · 0 颗按钮 | 无 | ✅ |
| `RankedSeasonEndedWindow` | `Shell/RankedSeasonEndedWindow.cs` | 同上（0 颗按钮） | 无 | ✅ |
| `RankedEventWindow` | `Shell/RankedEventWindow.cs` | `RankedEventWindow` 11/6（Positive 3 · MenuStartGame 2 · SelectToggle 1）· `RankedEventWindowV2` 见下行 | `:120` Positive · `:147` SelectToggle | ⚠️ `Continue Button`（MenuStartGame）未建（界面缺口） |
| `LiveOpsEventWindow`（抽象） | `Shell/LiveOpsEventWindow.cs` | `RankedEventWindowV2` 12/9（Back 4 · Positive 3 · MenuStartGame 1 · SelectToggle 1）· `SkirmishModeEventWindow` 16/8（Back 4 · Positive 3 · MenuStartGame 1） | `:515` Back（4 颗循环）· `:806` SelectAvatar · `:946` Positive · `:969` Help 默认音 | ❌ `CreateDeckHit:582` 默认音，原版 `No Deck Text/Generic Simplified UI Button` = Positive（§三 B10） |
| `SkirmishEventWindow` | `Shell/SkirmishEventWindow.cs` | 见上（SkirmishModeEventWindow） | `:302` Positive（Collect） | ✅ |
| `EnergySinglePlayerOnlyEventWindow` | `Shell/EnergySinglePlayerOnlyEventWindow.cs` | 7/1 Positive | `:464` | ✅ |
| `RankedRewardEventWindow` | `Shell/RankedRewardEventWindow.cs` | 1/1 Back | `:398` | ✅ |
| `ReferralPopupWindow` | `Shell/ReferralPopupWindow.cs` | 2/2（Back + Positive） | `:405/:535` | ✅ |
| `TrophyInfoPopup` | `Shell/TrophyInfoPopup.cs` | 2/2（Back + SelectToggle） | `:341/:459` | ✅ |
| `TutorialModePopup` | `Shell/TutorialModePopup.cs` | 2/1 Positive | `:696` | ✅ |
| `PracticeModePopup` | `Shell/PracticeModePopup.cs` | `Practice Mode Menu` 8/2（**Continue = MenuStartGame** · Deck info General = Positive）· **Back + Deck Selector Card Info = `useDef=0`（静音）** | `:871` Back（无 cue→默认音）· `:914` Battle（无 cue→默认音）· `:1459` 卡组行（默认音）· `:930` Game mode toggle 默认音 | ❌ 3 条（§三 B11/C1/C2） |
| `PurchasePremiumWindow` | `Shell/PurchasePremiumWindow.cs` | `Purchase Premium Window` 3/1（**Close = Back** · Army Container 默认+allowClick=0 · Price Display Button 默认） | `:596` Close（无 cue→默认音）· `:664` Army（默认）· `:716` Price（默认） | ❌ 关窗响错（§三 B7）；Army 多响见 C9 |
| `GiveFeedbackPopup` | `Shell/GiveFeedbackPopup.cs` | `Give Feedback Popup menu` 43/42（**41 颗 Checkbox = Select Toggle** · **Close = Back** · Submit = 默认）· 评分钮 prefab = `Feedback Scoring Button`（默认+allowClick=0） | `:490` Close（默认）· `:615` 复选框（默认）· `:482` Submit（默认，正确）· `:647` 评分（默认，正确）· `:511/:685` 输入框（默认→多响） | ❌ 4 条（§三 B3/B4/C6/C7） |
| `ParentEventWindow` | `Shell/ParentEventWindow.cs` | `Parent Event Window` 1/1（tab toggle = ButtonClick+allowClick=0）· `Two Sides Event Window` 10/1（**Back = `useDef=0` 静音** · Header Back 默认 · **Collect = Positive** · 其余默认） | `:512` tab 默认音 · `:666` P1 Back 默认音 · `:718` 队伍卡默认音 · `:765` P2 Header Back = **Back cue** · `:890` Collect 默认音 | ❌ 3 条（§三 B5/B6/C3） |
| `SearchingMatchPopup` / `SearchingOpponentWindow` | 同名文件 | `Searching Oponent Popup` 1 颗默认 · `SearchingOpponentWindow` 1 颗默认 | `SearchingMatchPopup:258` · `SearchingOpponentWindow:325`，都默认音 | ✅ |
| Toast（非窗） | `Shell/ToastNotifications.cs` | 场景 `Toast` / `Mission Toast Notification` / `Toast Collection Get XP` / `Toast Collection Level Up`：**根都是 `EverguildButton`，cue 空 + `useDef=1` ⇒ 默认音** | `:522/:578/:634/:674` 四颗 Hit 默认音 | ✅（**R3 §六·7 的记录是错的**，§四·2） |
| `MainMenuWindow` / 导航 | `Shell/MainMenuWindow.cs` · `MainMenuRuntime.cs` · `TabButtons.cs` | 场景 `MainMenu` 根 `useDef=0`（不关窗，无关）· 导航钮 `Main Menu Navigation Button - *` = Toggle 空 cue 默认 + allowClick=0 · 顶栏 Inbox/Settings/Feedback = 默认 | `MainMenuRuntime:1022/:2596` 手工 WindowButton（默认音）· `:1910` ChatHit 默认音 | ✅ cue 对；allowClick 见 C9 |

---

## §三 缺口明细

### A. 该响没响

**A1 · 卡牌详情窗开窗音 `CardExpand`（FX）—— 完全没播，且 cue 没导入**
- 原版：场景 `bundle_scenes_scenes_mainmenuwarpforge` 的 `Card Displayer Menu For Menu` 根 MB（class `CardDisplayWindow : GameWindow`，pid `2033`）
  `openSound = PPtr(fileID 6, pid 4006604330728631164)` → cue **`CardExpand`**（clip `CardExpand`，pitch 0.8–1.1，`timeToPlayAgain 0.1`）。
  消费者 = 继承来的 `GameWindow.TryOpen`（`openSound != null` ⇒ `SoundManager.Play2D(..., MixerType 0)`）。
- 我们：`Shell/CardDetailPopup.cs:649` `public override void Open() { Build(); }` —— 没有 `openSound` 赋值、没有 `WindowSounds` 调用；
  `Resources/animfx_sounds.json` 里**没有 `CardExpand`**（455 条 cues 里查无）。
- 怎么改：① `工具/import_original_sfx.py` 的 `EXTRA_CUES` 加 `"CardExpand"` 并重跑导入器（clip `CardExpand.wav` 在
  `bundle_soundcollection_assets_all/AudioClip/` 里，本地有）；② `WindowSounds` 加常量；③ `CardDetailPopup.Open()` 里
  `WindowSounds.PlayFx(...)`（FX 组，判据同上）。
- 属于：**音效线**。

**A2 · 段位升降窗 `soundOnOpen = Division Change`（Jingles 3）—— 没播，且 cue 没导入**
- 原版：`bundle_menus_assets_all` MB `-4938671765695524530`（class `RankedEventDivisionChangeWindowNewSystem : GameWindow`，prefab `Ranked Division Change Window`）
  的 `soundOnOpen = PPtr(fileID 6, pid -2236843390636958589)` → cue **`Division Change`**（clip `Division Increase`，`timeToPlayAgain 0.01`）。
  播放点 = `d:/2/tools/decomp_full/RankedEventDivisionChangeWindowNewSystem._DoPromoteAnimations_d__22__MoveNext.c:32-33`：
  `AudioCue__Play2D(cue, 3)` ⇒ **MixerType 3 = Jingles**（不是 FX）。
- 我们：`Shell/RankedDivisionChangeWindow.cs:188 Open()` 只 `Build()`；该窗 **0 颗按钮**、窗级 `openSound/closeSound` 都空
  （文件头 `:178` 只记了这两个字段，**没记 `soundOnOpen`**）；`animfx_sounds.json` 里**没有 `Division Change`**。
- 怎么改：同 A1 导入 cue；`Open()` 里用 **`WindowSounds.PlayJingles`**（Jingles 组，`WFSoundPlayer` 是 FX，别复用）。
  落点照原版是「升级动画起点」，我们的动画没建 ⇒ 放 `Open()` 并在注释里写明「落点是我们的（原版挂 DoPromoteAnimations 起点）」。
- 属于：**音效线**（与界面线动画共用一条判据）。

**A3 · 卡组编辑 3 颗页签（Cards / Deck info / Cosmetics）默认按钮音没播**
- 原版：`Deck Editing Menu/Content Area/Sidebar/Window Options/Buttons/{Cards,Cosmetics,Info}` = `EverguildToggle`，
  `m_onClickSound` 空 + `useDefaultAudioClipIfNull=1` + `allowClickWhenAlreadySelected=0` ⇒ **切到别的页时播默认 `ButtonClick`**，
  再点当前页**静音**（`EverguildToggle__OnPointerClick.c` 第 1 段提前 return）。
- 我们：`Deck/DeckRuntime.cs:5145` 注释写「表外的 key（页签 `tab_*` / 改名框）**原版没有显式 cue ⇒ 不播**」——
  **这个推理不成立**：没有显式 cue ≠ 静音，`useDefault=1` ⇒ 默认音。`tab_0..2` 的点击走 `HandleButtons`（`:5187-5189`），
  `PlayClickCue` 只认表内 4 个 id ⇒ **三颗页签一声不响**。
- 怎么改：把 `tab_0/tab_1/tab_2` 映射到 `WindowSounds.DefaultButtonCue`（或给 `PlayClickCue` 加一条「表外 key 默认音」兜底 ——
  但这样会把改名框等非按钮也带上音，**建议按 key 显式列**），并补 `allowClickWhenAlreadySelected` 守卫。
- 属于：**音效线**（S01 的「表外 key 口径」当时明确未定，现在判据齐了）。

### B. 响错 cue

**B1 · 卡牌详情窗关窗音：播 `ButtonClick Back`，应播 `Close Card Display`（FX）**
- 原版：同一 `CardDisplayWindow` 根 MB `closeSound = PPtr(fileID 6, pid 3524674544779823761)` → cue **`Close Card Display`**
  （clip `Close Card Display`）；`useDefaultCloseSoundIfNull=1` 只在 close 为空时兜底。
- 我们：`Shell/CardDetailPopup.cs:674 Close()` 只做 `ResetFanState` + `base.Close()`；基类 `closeSound == null` ⇒
  `GameWindow.cs:767` 播默认 `ButtonClick Back`。cue `Close Card Display` 也不在表里。
- 怎么改：同 A1 导入 cue；`CardDetailPopup` 里 `closeSound` 是 `AudioClip`（不是 cue 名）—— 按 FTUE 的先例，
  在 `Close()` 里先用 `WindowSounds.PlayFx("Close Card Display")`，**同时**把 `useDefaultCloseSoundIfNull` 置 false 防双声
  （或把 `closeSound` 直接赋 `CardArt` 里的 clip —— 但 cue 参数 `timeToPlayAgain` 会丢，建议走 cue 表）。
- 属于：**音效线**。

**B2 · 卡牌详情 `Craft`（创建副本）：播默认音，应播 `ButtonClick Positive`**
- 原版：场景 `Card Displayer Menu For Menu/Card Options Panel/Panel/Crafting Panel/Content/Craft`（EverguildButton）
  `m_onClickSound = 8549838806721213354` = **`ButtonClick Positive`**；`Upgrade` 同一条（pid 同值）。
- 我们：`Shell/CardDetailPopup.cs:1153` `Hit(p, "CraftHit", ...)` **没传 clickCue** → 默认 `ButtonClick`；
  同一面板的降级/升级 `:1206/:1221` 都传了 Positive。
- 怎么改：`:1153` 补 `clickCue: WindowSounds.PositiveCue`。
- 属于：**音效线**。

**B3 · 反馈窗关窗钮：默认音，应 `ButtonClick Back`**
- 原版：`Give Feedback Popup menu/Window/Generic Close Button` → `ButtonClick Back`（pid 8084683923307806969）。
- 我们：`Shell/GiveFeedbackPopup.cs:490` `MenuDraw.Hit(_close, "Hit", CloseR, QHit, Close)` 无 cue。
- 怎么改：补 `clickCue: WindowSounds.DefaultCloseCue`。
- 属于：**音效线**。

**B4 · 反馈窗 41 颗复选框：默认音，应 `Select Toggle`**
- 原版：`Give Feedback Popup menu/Window/Scroll Rect/Questions container/Section */S*Q*/Questions List/Checkbox*` 共 **41 颗**
  `EverguildToggle`，全部 `m_onClickSound = 8881786964274599484` = **`Select Toggle`**。
- 我们：`Shell/GiveFeedbackPopup.cs:615` 每一行 `MenuDraw.Hit(row, ...)` 无 cue → 默认 `ButtonClick`。
- 怎么改：`clickCue: WindowSounds.SelectToggleCue`。
- 属于：**音效线**。

**B5 · 活动承载窗 P2 表头返回钮：播 `ButtonClick Back`，应默认 `ButtonClick`**
- 原版：`Two Sides Event Window/Event On Going Tab/Game Mode Header With Back Button/Header Back Button` = EverguildButton，
  **`m_onClickSound` 空 + `useDefaultAudioClipIfNull=1`** ⇒ 默认音。
- 我们：`Shell/ParentEventWindow.cs:765` 传 `clickCue: WindowSounds.DefaultCloseCue`（`ButtonClick Back`）。
- 怎么改：改成不传（`null` = 默认 `ButtonClick`）。
- 属于：**音效线**。

**B6 · 活动承载窗 Collect：播默认音，应 `ButtonClick Positive`**
- 原版：`Two Sides Event Window/Event On Going Tab/Reward Display/Scoring Bar Event Score Info/Generic Simplified UI Button`
  → `ButtonClick Positive`（pid 8549838806721213354）。
- 我们：`Shell/ParentEventWindow.cs:890` 无 cue。
- 怎么改：补 `clickCue: WindowSounds.PositiveCue`。
- 属于：**音效线**。

**B7 · 高级商店（Purchase Premium）关窗钮：默认音，应 `ButtonClick Back`**
- 原版：`Purchase Premium Window/Generic Close Button Orange` → `ButtonClick Back`。
- 我们：`Shell/PurchasePremiumWindow.cs:596` `MenuDraw.Hit(close, "Hit", ...)` 无 cue。
- 怎么改：补 `clickCue: WindowSounds.DefaultCloseCue`。
- 属于：**音效线**。

**B8 · 通用弹窗 1 按钮版（`MessagePopupWindow`）：默认音，应 `ButtonClick Positive`**
- 原版：`MessagePopupWindow/Window/Buttons/Generic UI Button` → `ButtonClick Positive`。
- 我们：`Shell/PopUpGameWindow.cs:480` `hit = q.gameObject.AddComponent<WindowButton>()`，没有 cue。
  `WindowsManager.ShowPopUp` 自 A416 起就走这扇窗 ⇒ **所有单钮提示都响错**。
- 怎么改：`MakeButton(..., clickCue)` 加形参：1 按钮版传 `PositiveCue`。
- 属于：**音效线**。

**B9 · 通用弹窗 2 按钮版：左钮应 `ButtonClick Negative`、右钮应 `ButtonClick Positive`**
- 原版：`MessagePopupWindow2Buttons/Window/Buttons/ButtonLeft` = **Negative**（1387228150816026206）；
  `ButtonRight` = **Positive**（8549838806721213354）。
- 我们：`Shell/PopUpGameWindow.cs:436-441` 两颗都走 `MakeButton`（无 cue）→ 都默认 `ButtonClick`。
- 怎么改：左 `NegativeCue`、右 `PositiveCue`。
- 属于：**音效线**。

**B10 · 排位/遭遇活动「Create deck」钮：默认音，应 `ButtonClick Positive`**
- 原版：`RankedEventWindowV2/Ranked Deck Selection/No Deck Text/Generic Simplified UI Button` = Positive；
  `SkirmishModeEventWindow` 同名节点同 cue。
- 我们：`Shell/LiveOpsEventWindow.cs:582-583` `MenuDraw.Hit(none, "CreateDeckHit", ...)` 无 cue。
- 怎么改：补 `clickCue: WindowSounds.PositiveCue`。
- 属于：**音效线**。

**B11 · 练习模式 `Battle!`：默认音，应 `MenuStartGame`**
- 原版：`Practice Mode Menu/DeckSelectionContinueButton/Continue Button` → `MenuStartGame`（pid -1545367626748338049，clip `StartMatch`）。
  同 prefab 根 `DeckSelectionContinueButton` 也是这条（`RankedEventWindow` 同族 Continue 也是它）。
- 我们：`Shell/PracticeModePopup.cs:914` `HitOn(root, root, "BattleHit", ...)` 无 cue → 默认 `ButtonClick`；
  `WindowSounds.StartGameCue` 常量**定义了但全工程 0 处使用**。
- 怎么改：`HitOn(..., clickCue: WindowSounds.StartGameCue)`（`HitOn` 需加形参，或改用 `MenuDraw.Hit` 直传）。
- 属于：**音效线**。

**B12 · 商店卡包格点击：默认音，原版容器是 `Purchase Click`**
- 原版：`Shop Menu Variant/…/Comsetic Item Shop Container/raycast target` = `Purchase Click`（另一颗价格钮 = 默认）。
- 我们：`Shell/ShopTabPage.cs:558` `MenuDraw.Hit(cell, "InfoHit", ...)` 无 cue → 默认 `ButtonClick`。
- ⚠️ **语义也不同**：我们这颗是「开 `BoosterInfoPopup`」（R3 §四·B 已记为「语义不同」），原版是 `OpenContainer`。
  若保持「开信息窗」语义，至少 cue 应落 `Purchase Click`（因为原版那颗容器的声音就是它）。
- 怎么改：补 `clickCue: WindowSounds.PurchaseCue`；语义差异另记界面线。
- 属于：**音效线**（语义 → 界面线已有账）。

**B13 · 决斗弹窗 `Button Skirmish`：播 Positive，应默认 `ButtonClick`**
- 原版：`MessagePopupWindowDuel/Window/Buttons/Button Skirmish` = 空 cue + `useDefault=1`；`Button Classic` = Positive。
- 我们：`Shell/DuelPopupWindow.cs:248` `DuelButton(...)` 对**两颗**都传 `PositiveCue`（调用点 `:179/:182`）。
- 怎么改：`DuelButton` 加 cue 形参，Skirmish 传 `null`、Classic 传 `PositiveCue`。
- 属于：**音效线**。

### C. 多响（原版没有这一声 / 原版静音，我们响了）

**C1 · 练习模式返回钮：原版 `useDefaultAudioClipIfNull=0`（静音），我们播默认音**
- 原版：`Practice Mode Menu/Back button` = EverguildButton，cue 空 **且 `useDefaultAudioClipIfNull=0`** ⇒ `PlayButtonSound` 直接静默返回。
- 我们：`Shell/PracticeModePopup.cs:871` 本地 `Hit(...)` 建 WindowButton，`playClickSound` 默认 true → 默认 `ButtonClick`。
- 怎么改：拿 `backWb.playClickSound = false`。
- 属于：**音效线**。

**C2 · 练习模式卡组行（Deck Selector Card Info button_ref）：原版静音，我们响**
- 原版：`Practice Mode Menu/Deck info/Deck List Drawer/Content/Deck Selector Card Info button_ref` = 空 + `useDef=0`。
- 我们：`Shell/PracticeModePopup.cs:1459` `HitOn(cell, ..., () => PickDeck(idx), ...)` 默认音。
- 怎么改：`rowWb.playClickSound = false`（`:1460-1461` 已经拿了 `rowWb`，加一行即可）。
- 属于：**音效线**。

**C3 · 活动承载窗 P1 返回钮：原版静音，我们响**
- 原版：`Two Sides Event Window/Select Team Tab/Back button` = 空 + **`useDef=0`**。
- 我们：`Shell/ParentEventWindow.cs:666` 传 `DefaultButtonCue`（= 默认音），等于替原版加了一声。
- 怎么改：改为 `playClickSound = false`（或 `clickCue` + 关声音，取决于实现口；目标是「点了只关窗、只有关窗音」）。
- 属于：**音效线**。

**C4 · 卡牌详情 `Voice Over Button`：原版静音（它只播语音），我们播默认按钮音**
- 原版（场景）：`…/Card Displayer Menu For Menu/Card Options Panel/.../Voice Over Button` = 空 cue + **`useDef=0`**。
- 我们：`Shell/CardDetailPopup.cs:751` `Hit(_voiceBox, "VoiceHit", ...)` 无 cue → 默认音。
- 怎么改：`voiceWb.playClickSound = false`。
- 属于：**音效线**。

**C5 · 卡牌详情 `Show Card Text`（眼睛）：原版静音，我们播默认按钮音**
- 原版（场景）：`…/Show Card Text` = 空 cue + **`useDef=0`**。
- 我们：`Shell/CardDetailPopup.cs:763` `Hit(_eyeBox, "ShowTextHit", ...)` 无 cue → 默认音。
- 怎么改：`playClickSound = false`。
- 属于：**音效线**。

**C6 · 反馈窗「Free feedback」输入框：输入框不是按钮，我们点了会响**
- 原版：`Give Feedback Popup menu/Window/Free feedback/Input field` = `Image + TMP_InputField + EverguildInputField`（**没有 EverguildButton**）。
- 我们：`Shell/GiveFeedbackPopup.cs:511` `MenuDraw.Hit(inputNode, "Hit", InputR, QHit, BeginEditFree)` → 默认 `ButtonClick`（每点一次输入框响一声）。
- 怎么改：`playClickSound=false`（输入框的点击是聚焦，不是按钮）。
- 属于：**音效线**。
- 备注：该块 prefab 出厂 `m_IsActive=false`，运行时影响仅在将来启用时。

**C7 · 反馈窗开放题 `Text Area`（EverguildInputField）：同上，点了会响**
- 原版：`…/Section 2/S2Q4/Text Area`、`Section 4/S4Q2_Extra/Text Area` = `Image + EverguildInputField`（无 EverguildButton）。
- 我们：`Shell/GiveFeedbackPopup.cs:685` `MenuDraw.Hit(box, "Hit", areaR, QHit, () => BeginEditOpen(it))` → 默认音。
- 怎么改：`playClickSound=false`。
- 属于：**音效线**。

**C8 · 卡组信息窗督军钮：原版静音，我们响**
- 原版：`Deck info Popup/Info Panel/Deck List/Content/Deck Selector Hero Card Info button` = 空 + **`useDef=0`**。
- 我们：`Shell/DeckInfoPopup.cs:1366-1368` `Hit(root, root, "WarlordHit", ...)` 无 cue → 默认音。
- 怎么改：`playClickSound=false`（同一个 helper 建的其它钮别一刀切）。
- 属于：**音效线**。

**C9 · `allowClickWhenAlreadySelected = 0` 守卫未实现（64 颗 Toggle，系统性多响）**
- 原版：`EverguildToggle__OnPointerClick.c` 第 1 段：
  `if (*(char *)(this + 0x139) == 0 && *(char *)(this + 0x120) != 0) return;`
  = `allowClickWhenAlreadySelected == 0 && isOn` ⇒ **连 `UnityEngine.UI.Toggle.OnPointerClick` 与按钮音一起跳过**。
  全量：**64 颗**（30 个 prefab 根），绝大多数在**我们已经建的**窗里：
  Social Submenu 6 · Player Profile 6 · Settings 5 · Collection 4 · Rewards Base 4 · Deck Editing 3 ·
  RankedSkirmish 3 · RankedClassic 2 · Deck Selection 2 · Draft 系 8（未建）· 其余单颗
  （`Parent Event Window` / `Shop Menu Variant` / `Base Submenu` / `Menu Navigation Panel Button` / `Orange Tab Toggle` /
  `ChatPanel` / `Purchase Premium Window/Army Container` / `Feedback Scoring Button` / `Forge Army Item Button` /
  `Army Item Button` / `Campaign Army Item Button` / `Ranked Army Selector Container V2` / `Main Menu Navigation Button` 等）。
- 我们：`WindowButton` **没有这个字段**；`Click()` 在 `onClick()` **之前**就 `PlayClickSound()`（`WindowButton.cs:840`）
  ⇒ 即使 `AchievementsMenu.cs:256` 有 `if (_filter == type) return;`，**声音已经响过了**（先响后 return）。
- 怎么改：给 `WindowButton` 加 `allowClickWhenAlreadySelected`（默认 true）+ 「已选中」状态源（toggle 类站点在建的时候是知道的），
  在 `Click()` **最前面**（`interactable` 判定之后、`PlayClickSound` 之前）提前 return；逐站点赋值。
  建议先做「所有 tab/toggle 类站点」，别一刀切到按钮。
- 属于：**音效线**（S01 已明确「不在范围，先记着」—— 现在应当立账）。

### D. cue 缺失（`animfx_sounds.json` 455 条里查无）

| cue | clip（本地文件） | 现状 | 怎么补 |
|---|---|---|---|
| `CardExpand` | `bundle_soundcollection_assets_all/AudioClip/CardExpand.wav` | 表里没有、代码没引用 | `import_original_sfx.py` → `EXTRA_CUES` + 重跑 |
| `Close Card Display` | `…/Close Card Display.wav` | 同上 | 同上 |
| `Division Change` | `…/Division Increase.wav` | 同上（且要走 **Jingles**） | 同上（导入器照 cue 的 min/max pitch 写） |

> 三条 cue 的包装 pid 与参数（现读 `soundcollection_assets_all`）：
> `CardExpand` pid `4006604330728631164`（pitch 0.8–1.1，vol 1–1，ttp 0.1）·
> `Close Card Display` pid `3524674544779823761`（1–1，1–1，0.1）·
> `Division Change` pid `-2236843390636958589`（1–1，1–1，**ttp 0.01**）。

---

## §四 没查清 / 口径订正

1. **任务简报里的 `MixerType` 枚举写错了**：简报说「0=FX、**1=Jingles**」，实际
   `d:/2/Warpforge_code/Scripts/Assembly-CSharp/MixerType.cs` = `FX=0 · Music=1 · Voices=2 · **Jingles=3** · BattleGroupLow/Medium/High=4/5/6`。
   `WindowSounds.cs` 与 W_G 报告用的是 3，**是对的**；本报告一律按 `MixerType.cs` 与指令流。
2. **R3 §六·7 的「Toast 原版没有 EverguildButton」是错的（2026-10-18 现核）**：
   场景 `bundle_scenes_scenes_mainmenuwarpforge` 里 `Toast` / `Mission Toast Notification` / `Toast Collection Get XP` /
   `Toast Collection Level Up` **四个 prefab 根都挂着 `EverguildButton`**，`m_onClickSound = {0,0}` 且
   `useDefaultAudioClipIfNull = 1` ⇒ **原版点提示条就有默认按钮音**。我们 4 处 `MenuDraw.Hit(...默认音)` **是对的**，⛔ 别按 R3 那条去关。
   （判据：本报告 UnityPy 直读场景 bundle；类名反查 `MonoScript`，字段名 `m_onClickSound` / `useDefaultAudioClipIfNull`。）
3. **`BaseEventPopup` 的运行期开窗音（LiveOps 资产 index 8）在本地查不到**：
   反编译 `BaseEventPopup__Open.c` ⑤ 是 `HasAsset(liveOps, 8 /*OpenSound*/)` 才 `AudioCue.Play2D`；
   我扫了 `bundle_cosmeticsso_assets_all/MonoBehaviour/` 的 **1261 个** SO，**0 个**含 `OpenSound` 字段/引用
   ⇒ 事件组织数据与资产映射在远端，**本地没有判据**。⛔ 不编。归类：等远端 / 无法复刻。
4. **W_G 的「GameWindow 全族 100 颗」与本次 102 颗**：差 2 = 主菜单场景实例（`MainMenu` 根 + `Card Displayer Menu For Menu`），
   不影响结论（W_G 统计的是包内 prefab）。
5. **`MissionsTab` 的 info 圆钮**：我们 `Shell/MissionsTab.cs:1543` 建了 tooltip 命中区（`onClick=null`，默认音）。
   原版那颗 `40K_generic_bt_info` 挂的是 `EverguildTooltipTrigger`；**它是不是 EverguildButton 我没逐颗核**
   （`allbuttons.tsv` 里 Missions Tab 的 52 颗中**没有** info 那一颗）⇒ **可能多响，待核**，⛔ 不写成结论。
6. **`MenuStartGame` 的其余 4 颗**：`RankedEventWindow/Continue Button` · `RankedEventWindow/DEBUG/…` ·
   `RankedEventWindowV2/DEBUG/…` · `SkirmishModeEventWindow/DEBUG/…` —— 调试/未建，不接。
   **非调试的 `DeckSelectionContinueButton/Continue Button`** 我们没建（界面缺口）。
7. **`UI Purchase 2`（1 颗，`Expansion Pass Reward Container`）**：扩展通行证整体未建，无对应站点。
8. **搜过哪些包/词（§四用）**：
   包 = `menus_assets_all` · `generalgamewindows_assets_all` · `scenes_scenes_mainmenuwarpforge` · `mainmenualwaysloaded_assets_all` ·
   `boosterpacks_assets_all` · `soundcollection_assets_all` · `Waprforge_monoscripts` · `cosmeticsso_assets_all`。
   词/字段 = `openSound` · `closeSound` · `useDefaultCloseSoundIfNull` · `m_onClickSound` · `useDefaultAudioClipIfNull` ·
   `allowClickWhenAlreadySelected` · `soundOnOpen` · `OpenSound`。
   `openSound/closeSound` 在 `menus` 里 95 个文件命中、`generalgamewindows` 5 个、主菜单场景 2 个；`soundOnOpen` 全库仅 **1** 个。

---

## §五 顺手发现（⛔ 未改，交调度台分流）

1. **`Shell/BaseEventPopup.cs:51-52` 的注释已过期**：写着「`closeSound` / `useDefaultCloseSoundIfNull` 我们**没有建模**
   （`GameWindow` 的关窗音整条链不存在）」—— W_G（2026-10-17）已经把整条链接上了，这句该改成「已建模，prefab 两字段为空 ⇒ 走默认 `ButtonClick Back`」。
2. **`Shell/PopUpGameWindow.cs:377` 的日志字符串过期**：「⚠️ 开/关音效没做（`useDefaultCloseSoundIfNull = 1`，默认音在远端音频表）」
   —— 默认音已解出并接上；开窗音原版本来就空。建议改成「开窗音：原版为空 ⇒ 不播；关窗音：默认 `ButtonClick Back`；
   ⚠️ 按钮 cue 待接（Positive/Negative）」。
3. **`WindowSounds.StartGameCue` / `Purchase2Cue` 两个常量定义了但 0 处使用**（`Shell/WindowSounds.cs:65/67`）；
   `MenuStartGame` 有 6 颗原版钮在等它（本报告 B11 + §四·6）。
4. **界面线缺口（无音可响，不是接线错）**：
   - `Player Profile Window/Ranking/{Current,Highest} Rank/LeaderboardButton`（Positive ×2）—— 我们没建。
   - `Inbox Menu/Content/{Reset Button, Claim Button}`（Positive ×2）—— 我们没建（R3 §四·B 已记）。
   - `Ranked Deck Selection/DeckSelectionContinueButton/Continue Button`（MenuStartGame）—— 未建。
   - `Two Sides Event Window/…/Score Bar Line Level 1..5/Chest`（默认音 ×5）—— 我们只有视觉、无命中区。
   - `Missions Tab` 的 3 颗 `raycast target`（Purchase Click）—— 语义不同/未建。
5. **`ShopTabPage` 的卡包格命中区语义**：原版 `…/Comsetic Item Shop Container/raycast target` = 打开商品容器 + `Purchase Click`；
   我们是「开 `BoosterInfoPopup`」+ 默认音。语义差异属界面线（R3 §四·B 已有账），声音按 B13 补。
6. **`DeckEditingWindow` 的注释口径要改**：`Deck/DeckRuntime.cs:5145` 那句「原版没有显式 cue ⇒ 不播」是错的
   （见 A3）。同类推理在别处若还有，应按「空 cue + useDefault=1 ⇒ 默认音」纠正。
7. **两处「说没做、其实做了」的过期注释已由 W_G 修过**（`PopUpGameWindow.cs:59` / `BoosterPackOpenWindow.cs:71`）——
   本次复核没有回退，只发现上面 1/2 两处仍在。
8. **`WindowSounds` 的节流实现与原版 `AudioCue.timeToPlayAgain` 一致**（`:169-179`），但批处理里 `Time.realtimeSinceStartup`
   推进慢 ⇒ 自检里同 cue 最多响一次（W_G §四·6 已知）—— 不影响真机。

---

## §六 复核方法与可复现命令（临时脚本，未入库）

```python
# 依赖：UnityPy 1.25.3（本机已装）
# 读原版窗级字段 + 逐按钮 cue（m_FileID 1-based 由 UnityPy 的 PPtr 自动解，cue 名按 PathID 反查 soundcollection）
env = UnityPy.load(r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/menus_assets_all.bundle")
# 遍历 MonoBehaviour → m_Script.m_PathID 查 MonoScript.m_ClassName；
# 含 openSound/closeSound 的 = GameWindow 族；m_onClickSound 非空的 = 显式按钮 cue。
```
- 临时产物（**不入库**）：`%TEMP%/aud/windows.tsv`（102 行）· `allbuttons.tsv`（1022 行）· `cues.json`（610 条 cue）。
- 全部证据均为**本地只读**读取，没有修改 `d:/2` 任何文件。

---

## §七 一句话给调度台

**「音效线无未完成活」要撤回。** 先做 **A1/A2/B1/B2/D**（卡牌详情窗 + 段位窗 + 3 条 cue 导入，判据全齐、改动小、
用户最容易听出来），再做 **B8/B9/B3/B4**（通用弹窗 + 反馈窗，每次交互都中），最后处理 **C9**（64 颗 Toggle 的
`allowClickWhenAlreadySelected`，需要给 `WindowButton` 加字段并逐站点赋值，量最大）。
