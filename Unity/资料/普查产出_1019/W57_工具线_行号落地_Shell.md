# W57 · 工具·文档线 — `A1123` / `A1220` 行号落地（Shell 族）

> 执行代理 **TOOL-2** · 第五十七会话 · 2026-10-19。**只做不验**：⛔ 没跑 Unity / `*.Run` / player / `_run_8_checks.sh`，⛔ 没动 git，⛔ 没改正本与 `Editor/**`。
> 唯一跑过的检查 = **秒级类型检查**（`TMPDIR=/tmp/wf_tool2 bash d:/4/Unity/工具/typecheck.sh`）⇒ **运行时 0 / 编辑器 0**。
> 判据来源 = `资料/普查产出_1019/W57_工具线_行号普查.md` §2 / §3·3；白名单 = `CardPresentation/Shell/*.cs` **去掉 `CollectionWindow.cs`**（另一代理在改）。

## 〇 · 结果一句话

**63 个文件 · 224 条引用已替换（186 个不同行）· 跳过 2 条（= H 类纠错记录，按口径不改）· 其余 0 跳过。**

- 分类：**S 64 条** · **F 149 条** · **A1220（§3·3）11 条**。
- 全部替换都是**行内定位**（先验证宿主行仍含原串，再只改那一行）⇒ 不会因为同串在别处出现而误伤。
- 每个文件 `git diff --numstat` 的增删数**相等且很小**（≤13）⇒ 没有整篇翻行尾；全族 `\r\n = 0`（纯 LF 保持不变）。
- 2 条 H（`Shell/CollectionData.cs:325` 的 `Core/Loc.cs:1050-1065` · `Shell/MenuDraw.cs:46` 的 `CampaignTab.cs:342`）**原样保留**。

## 一 · 总表（按文件）

| 文件 | 应用条数 | 修改行数 | S | F | A1220 | 跳过 | 跳过原因 |
|---|---:|---:|---:|---:|---:|---:|---|
| `Shell/AllianceMemberOptionsPopup.cs` | 3 | 3 | 1 | 2 | 0 | 0 |  |
| `Shell/AllianceMemberTab.cs` | 2 | 2 | 1 | 1 | 0 | 0 |  |
| `Shell/AlliancePanelWindow.cs` | 3 | 3 | 1 | 2 | 0 | 0 |  |
| `Shell/BaseOfferPopup.cs` | 2 | 2 | 1 | 1 | 0 | 0 |  |
| `Shell/BattleLogData.cs` | 2 | 2 | 2 | 0 | 0 | 0 |  |
| `Shell/BattleLogPopup.cs` | 1 | 1 | 1 | 0 | 0 | 0 |  |
| `Shell/BoosterInfoPopup.cs` | 3 | 2 | 1 | 2 | 0 | 0 |  |
| `Shell/BoosterPackOpenWindow.cs` | 1 | 1 | 0 | 1 | 0 | 0 |  |
| `Shell/CampaignTab.cs` | 2 | 2 | 0 | 2 | 0 | 0 |  |
| `Shell/CardDetailPopup.cs` | 13 | 11 | 4 | 9 | 0 | 0 |  |
| `Shell/ChatPanel.cs` | 2 | 1 | 0 | 2 | 0 | 0 |  |
| `Shell/ChatTab.cs` | 6 | 3 | 0 | 5 | 1 | 0 |  |
| `Shell/CollectionData.cs` | 7 | 6 | 3 | 3 | 1 | 1 | H class (纠错记录) — 不改 |
| `Shell/DailyData.cs` | 2 | 2 | 1 | 1 | 0 | 0 |  |
| `Shell/DailyRewardPopup.cs` | 1 | 1 | 1 | 0 | 0 | 0 |  |
| `Shell/DailyStreakPopup.cs` | 1 | 1 | 1 | 0 | 0 | 0 |  |
| `Shell/DeckEditingWindow.cs` | 3 | 3 | 1 | 2 | 0 | 0 |  |
| `Shell/DeckInfoPopup.cs` | 6 | 6 | 3 | 3 | 0 | 0 |  |
| `Shell/DeckSelectionPopup.cs` | 15 | 10 | 3 | 8 | 4 | 0 |  |
| `Shell/DuelPopupWindow.cs` | 2 | 1 | 0 | 2 | 0 | 0 |  |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs` | 5 | 3 | 1 | 4 | 0 | 0 |  |
| `Shell/FTUESelectDeckWindow.cs` | 1 | 1 | 1 | 0 | 0 | 0 |  |
| `Shell/GameWindow.cs` | 4 | 4 | 1 | 3 | 0 | 0 |  |
| `Shell/GenericOptionsPanel.cs` | 1 | 1 | 0 | 1 | 0 | 0 |  |
| `Shell/GiveFeedbackPopup.cs` | 2 | 2 | 1 | 1 | 0 | 0 |  |
| `Shell/InboxWindow.cs` | 4 | 3 | 0 | 4 | 0 | 0 |  |
| `Shell/ItemDrawer.cs` | 2 | 2 | 2 | 0 | 0 | 0 |  |
| `Shell/LeaderboardRow.cs` | 1 | 1 | 0 | 1 | 0 | 0 |  |
| `Shell/LeaderboardWindow.cs` | 8 | 5 | 0 | 8 | 0 | 0 |  |
| `Shell/LiveOpsEventWindow.cs` | 5 | 5 | 1 | 4 | 0 | 0 |  |
| `Shell/LoadingMenu.cs` | 1 | 1 | 0 | 1 | 0 | 0 |  |
| `Shell/MainMenuRuntime.cs` | 9 | 9 | 2 | 7 | 0 | 0 |  |
| `Shell/MainMenuSubmenuWindow.cs` | 5 | 5 | 2 | 3 | 0 | 0 |  |
| `Shell/MatchLogRow.cs` | 6 | 2 | 2 | 0 | 4 | 0 |  |
| `Shell/MenuDraw.cs` | 13 | 8 | 5 | 8 | 0 | 1 | H class (纠错记录) — 不改 |
| `Shell/MenuScroll.cs` | 6 | 3 | 0 | 6 | 0 | 0 |  |
| `Shell/MissionsTab.cs` | 4 | 4 | 2 | 2 | 0 | 0 |  |
| `Shell/OfferContainer.cs` | 1 | 1 | 0 | 1 | 0 | 0 |  |
| `Shell/PlayerProfileWindow.cs` | 2 | 1 | 0 | 2 | 0 | 0 |  |
| `Shell/PointerLayer.cs` | 5 | 5 | 2 | 3 | 0 | 0 |  |
| `Shell/PopUpGameWindow.cs` | 3 | 3 | 0 | 3 | 0 | 0 |  |
| `Shell/PracticeModePopup.cs` | 2 | 2 | 1 | 1 | 0 | 0 |  |
| `Shell/ProfilePage.cs` | 4 | 3 | 0 | 3 | 1 | 0 |  |
| `Shell/ProfileTab.cs` | 2 | 2 | 0 | 2 | 0 | 0 |  |
| `Shell/PromptPopup.cs` | 3 | 3 | 0 | 3 | 0 | 0 |  |
| `Shell/PurchasePremiumWindow.cs` | 2 | 1 | 0 | 2 | 0 | 0 |  |
| `Shell/RankedRewardEventWindow.cs` | 2 | 1 | 0 | 2 | 0 | 0 |  |
| `Shell/RewardWindow.cs` | 1 | 1 | 0 | 1 | 0 | 0 |  |
| `Shell/SearchingMatchPopup.cs` | 1 | 1 | 1 | 0 | 0 | 0 |  |
| `Shell/SettingsWindow.cs` | 13 | 13 | 4 | 9 | 0 | 0 |  |
| `Shell/ShellRuntime.cs` | 2 | 1 | 0 | 2 | 0 | 0 |  |
| `Shell/ShopData.cs` | 1 | 1 | 0 | 1 | 0 | 0 |  |
| `Shell/SocialPage.cs` | 1 | 1 | 0 | 1 | 0 | 0 |  |
| `Shell/SocialView.cs` | 1 | 1 | 0 | 1 | 0 | 0 |  |
| `Shell/SocialWindow.cs` | 1 | 1 | 1 | 0 | 0 | 0 |  |
| `Shell/ToastNotifications.cs` | 1 | 1 | 0 | 1 | 0 | 0 |  |
| `Shell/TopBar.cs` | 2 | 2 | 1 | 1 | 0 | 0 |  |
| `Shell/TrophyInfoPopup.cs` | 3 | 2 | 1 | 2 | 0 | 0 |  |
| `Shell/TutorialModePopup.cs` | 7 | 7 | 2 | 5 | 0 | 0 |  |
| `Shell/UIBakeScreen.cs` | 1 | 1 | 1 | 0 | 0 | 0 |  |
| `Shell/WhereToGetCardsPopup.cs` | 1 | 1 | 0 | 1 | 0 | 0 |  |
| `Shell/WindowSounds.cs` | 1 | 1 | 0 | 1 | 0 | 0 |  |
| `Shell/WindowsManager.cs` | 7 | 7 | 5 | 2 | 0 | 0 |  |
| **合计（63 份）** | **224** | **186** | **64** | **149** | **11** | **2** | H 类 2 条 |

> ⛔ `Shell/CollectionWindow.cs` **不在本批**（普查里有 10 条，`§3·3` 里另有 2 条）—— 该文件当时正被另一代理修改（mtime 差 2.8 分钟，`git status` 中原就有 62/10 的未提交改动），按调度口径**整份跳过**。

## 二 · 逐条改动清单

> `次数` = 该条在表里对应的引用数（同一行同串多引用时 >1）。`行` = 改动后的宿主行号（本批不增删行 ⇒ 与改动前相同）。


### `Shell/AllianceMemberOptionsPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 68 | F | `Shell/AllianceMemberTab.cs:1357` | `Shell/AllianceMemberTab.cs` | 1 |
| 108 | S | `Editor/ShopScene.cs:4453` | `Editor/ShopScene.cs` 的 `OctagonUI_Filled_SDF` | 1 |
| 477 | F | `AllianceMemberTab.cs:1357` | `AllianceMemberTab.cs` | 1 |

### `Shell/AllianceMemberTab.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 407 | F | `Editor/MainMenuScene.cs:4675` | `Editor/MainMenuScene.cs` | 1 |
| 922 | S | `Shell/RankedTab.cs:149` | `Shell/RankedTab.cs` 的 `ScoreEmpty` | 1 |

### `Shell/AlliancePanelWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 78 | S | `Battle/HudCanvas.cs:33-35` | `Battle/HudCanvas.cs` 的 `EventSystem` | 1 |
| 454 | F | `Battle/HudCanvas.cs:33-35` | `Battle/HudCanvas.cs` | 1 |
| 794 | F | `Battle/BattleDriver.cs:15084` | `Battle/BattleDriver.cs` | 1 |

### `Shell/BaseOfferPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 13 | S | `Editor/SettingsScene.cs:488` | `Editor/SettingsScene.cs` 的 `extraScaleSmallScreen = 1.2` | 1 |
| 184 | F | `Shell/LeaderboardWindow.cs:199` | `Shell/LeaderboardWindow.cs` | 1 |

### `Shell/BattleLogData.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 25 | S | `Battle/BattleDriver.cs:12807` | `Battle/BattleDriver.cs` 的 `RecordBattleLog()` | 1 |
| 26 | S | `BattleDriver.cs:12844` | `BattleDriver.cs` 的 `BattleLogData` | 1 |

### `Shell/BattleLogPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 185 | S | `PointerLayer.cs:183-187` | `PointerLayer.cs` 的 `Instance == null` | 1 |

### `Shell/BoosterInfoPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 342 | F | `Shell/ReferralPopupWindow.cs:386-387` | `Shell/ReferralPopupWindow.cs` | 1 |
| 342 | F | `Shell/BaseOfferPopup.cs:742-743` | `Shell/BaseOfferPopup.cs` | 1 |
| 346 | S | `MenuDraw.cs:30-31` | `MenuDraw.cs` 的 `ImageQuad.Create` | 1 |

### `Shell/BoosterPackOpenWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 729 | F | `Core/CardFan.cs:104-105` | `Core/CardFan.cs` | 1 |

### `Shell/CampaignTab.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 1339 | F | `ForgeTab.cs:572/609` | `ForgeTab.cs` | 1 |
| 1362 | F | `Shell/ForgeTab.cs:572/609` | `Shell/ForgeTab.cs` | 1 |

### `Shell/CardDetailPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 682 | S | `Core/CardTween.cs:57-60` | `Core/CardTween.cs` 的 `SetLink(link.gameObject)` | 1 |
| 868 | S | `Core/CardView.cs:5574` | `Core/CardView.cs` 的 `ArtTexture` | 1 |
| 869 | F | `Battle/BattleDriver.cs:4766` | `Battle/BattleDriver.cs` | 1 |
| 870 | F | `Shell/CollectionWindow.cs:1756-1757` | `Shell/CollectionWindow.cs` | 1 |
| 1225 | F | `Shell/DeckInfoPopup.cs:281` | `Shell/DeckInfoPopup.cs` | 1 |
| 1235 | F | `Shell/CampaignRewardWindow.cs:820-849` | `Shell/CampaignRewardWindow.cs` | 1 |
| 1435 | F | `CollectionWindow.cs:1646-1648` | `CollectionWindow.cs` | 1 |
| 1531 | S | `Shell/DeckInfoPopup.cs:1295` | `Shell/DeckInfoPopup.cs` 的 `CheckOwnership` | 1 |
| 1532 | F | `Shell/BoosterPackOpenWindow.cs:631` | `Shell/BoosterPackOpenWindow.cs` | 1 |
| 1532 | F | `Shell/CollectionWindow.cs:246` | `Shell/CollectionWindow.cs` | 1 |
| 1568 | S | `Shell/CollectionWindow.cs:1570-1572` | `Shell/CollectionWindow.cs` 的 `leftStyleButton` | 1 |
| 1720 | F | `Shell/CollectionWindow.cs:900` | `Shell/CollectionWindow.cs` | 1 |
| 1720 | F | `Deck/DeckRuntime.cs:1595` | `Deck/DeckRuntime.cs` | 1 |

### `Shell/ChatPanel.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 308 | F | `Shell/ReferralPopupWindow.cs:386-387` | `Shell/ReferralPopupWindow.cs` | 1 |
| 308 | F | `Shell/BaseOfferPopup.cs:742-743` | `Shell/BaseOfferPopup.cs` | 1 |

### `Shell/ChatTab.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 122 | F | `SocialWindow.cs:319-329` | `SocialWindow.cs` | 1 |
| 170 | F | `AllianceMemberTab.cs:532` | `AllianceMemberTab.cs` | 1 |
| 170 | F | `AlliancesTab.cs:531` | `AlliancesTab.cs` | 1 |
| 170 | A1220 | `:1346` | `Shell/AllianceMemberTab.cs` 的 `PadL` | 1 |
| 171 | F | `BattleLogPopup.cs:237` | `BattleLogPopup.cs` | 1 |
| 171 | F | `FriendsTab.cs:338` | `FriendsTab.cs` | 1 |

### `Shell/CollectionData.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 70 | F | `RuleEngine/Data/DeckLibrary.cs:117-119` | `RuleEngine/Data/DeckLibrary.cs` | 1 |
| 71 | S | `Deck/DeckRuntime.cs:726` | `Deck/DeckRuntime.cs` 的 `DeckLibrary.Load()` | 1 |
| 74 | S | `Shell/CollectionWindow.cs:3032` | `Shell/CollectionWindow.cs` 的 `CollectionData` | 1 |
| 85 | F | `Deck/DeckRuntime.cs:5941-5952` | `Deck/DeckRuntime.cs` | 1 |
| 272 | F | `Deck/DeckRuntime.cs:3950/3951/3982` | `Deck/DeckRuntime.cs` | 1 |
| 464 | S | `Editor/CollectionScene.cs:6780` | `Editor/CollectionScene.cs` 的 `Length == 0` | 1 |
| 464 | A1220 | `:6891` | `Editor/CollectionScene.cs` | 1 |

### `Shell/DailyData.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 956 | S | `Shell/MainMenuRuntime.cs:254` | `Shell/MainMenuRuntime.cs` 的 `TryDailyReset()` | 1 |
| 1492 | F | `Editor/MainMenuScene.cs:7602` | `Editor/MainMenuScene.cs` | 1 |

### `Shell/DailyRewardPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 252 | S | `Shell/PopUpGameWindow.cs:26-32` | `Shell/PopUpGameWindow.cs` 的 `ShadeHit` | 1 |

### `Shell/DailyStreakPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 374 | S | `MainMenuRuntime.cs:875-881` | `MainMenuRuntime.cs` 的 `SetCornerColors` | 1 |

### `Shell/DeckEditingWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 39 | S | `Deck/DeckRuntime.cs:4997` | `Deck/DeckRuntime.cs` 的 `LeaveByHostWindow()` | 1 |
| 40 | F | `CollectionWindow.cs:3622/3698` | `CollectionWindow.cs` | 1 |
| 41 | F | `LiveOpsEventWindow.cs:1163` | `LiveOpsEventWindow.cs` | 1 |

### `Shell/DeckInfoPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 1799 | F | `DeckRuntime.cs:3119` | `DeckRuntime.cs` | 1 |
| 1825 | S | `DeckEditorState.cs:282` | `DeckEditorState.cs` 的 `DefensiveId` | 1 |
| 1944 | F | `DeckRuntime.cs:3189` | `DeckRuntime.cs` | 1 |
| 1961 | S | `DeckRuntime.cs:3207` | `DeckRuntime.cs` 的 `bool showX = n > 1;` | 1 |
| 2053 | S | `Battle/Label.cs:173-182` | `Battle/Label.cs` 的 `SetColor` | 1 |
| 2462 | F | `Warpforge_code/Scripts/Assembly-CSharp/DeckInfoPopup.cs:112` | `Warpforge_code/Scripts/Assembly-CSharp/DeckInfoPopup.cs` | 1 |

### `Shell/DeckSelectionPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 57 | F | `Shell/DeckInfoPopup.cs:66-70` | `Shell/DeckInfoPopup.cs` | 1 |
| 57 | A1220 | `:109` | `Shell/DeckInfoPopup.cs` | 1 |
| 355 | F | `Editor/MainMenuScene.cs:2651` | `Editor/MainMenuScene.cs` | 1 |
| 355 | A1220 | `:2670` | `Editor/MainMenuScene.cs` | 1 |
| 471 | F | `Shell/MenuScroll.cs:79` | `Shell/MenuScroll.cs` | 1 |
| 474 | S | `Shell/PointerLayer.cs:223-231` | `Shell/PointerLayer.cs` 的 `SetActive(false)` | 1 |
| 477 | F | `Shell/DeckSelectionPopup.cs:536-544` | `Shell/DeckSelectionPopup.cs` | 1 |
| 477 | A1220 | `:573-579` | `Shell/DeckSelectionPopup.cs` | 1 |
| 478 | F | `Editor/CollectionScene.cs:2418` | `Editor/CollectionScene.cs` | 1 |
| 479 | F | `Shell/InboxWindow.cs:405` | `Shell/InboxWindow.cs` | 1 |
| 629 | S | `Editor/MainMenuScene.cs:2651/2670` | `Editor/MainMenuScene.cs` 的 `ScopeText` | 1 |
| 646 | F | `Shell/InboxWindow.cs:386-387` | `Shell/InboxWindow.cs` | 1 |
| 646 | F | `Shell/ChatPanel.cs:307-308` | `Shell/ChatPanel.cs` | 1 |
| 1000 | S | `Editor/MainMenuScene.cs:2651` | `Editor/MainMenuScene.cs` 的 `ScopeText` | 1 |
| 1000 | A1220 | `:2670` | `Editor/MainMenuScene.cs` | 1 |

### `Shell/DuelPopupWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 197 | F | `Shell/InboxWindow.cs:386-387` | `Shell/InboxWindow.cs` | 1 |
| 197 | F | `Shell/ChatPanel.cs:307-308` | `Shell/ChatPanel.cs` | 1 |

### `Shell/EnergySinglePlayerOnlyEventWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 63 | F | `Shell/SkirmishEventWindow.cs:74` | `Shell/SkirmishEventWindow.cs` | 1 |
| 63 | F | `Shell/RankedEventWindow.cs:64` | `Shell/RankedEventWindow.cs` | 1 |
| 337 | F | `Shell/SkirmishEventWindow.cs:74` | `Shell/SkirmishEventWindow.cs` | 1 |
| 337 | F | `Shell/RankedEventWindow.cs:64` | `Shell/RankedEventWindow.cs` | 1 |
| 457 | S | `Shell/AlliancePanelWindow.cs:373` | `Shell/AlliancePanelWindow.cs` 的 `lb.SetWrapping(false)` | 1 |

### `Shell/FTUESelectDeckWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 19 | S | `GameWindow.cs:488` | `GameWindow.cs` 的 `WindowSounds.PlayFx` | 1 |

### `Shell/GameWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 139 | S | `Editor/RewardsScene.cs:1511` | `Editor/RewardsScene.cs` 的 `RenderClip` | 1 |
| 322 | F | `RewardsScene.cs:1511` | `RewardsScene.cs` | 1 |
| 607 | F | `MainMenuWindow.cs:175` | `MainMenuWindow.cs` | 1 |
| 827 | F | `Editor/ShellScene.cs:1861-1862` | `Editor/ShellScene.cs` | 1 |

### `Shell/GenericOptionsPanel.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 518 | F | `Shell/CampaignRewardWindow.cs:844` | `Shell/CampaignRewardWindow.cs` | 1 |

### `Shell/GiveFeedbackPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 32 | F | `Core/Loc.cs:3365-3385` | `Core/Loc.cs` | 1 |
| 896 | S | `Core/Loc.cs:3365-3385` | `Core/Loc.cs` 的 `OfficialCurrent` | 1 |

### `Shell/InboxWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 450 | F | `Shell/ReferralPopupWindow.cs:386-387` | `Shell/ReferralPopupWindow.cs` | 1 |
| 450 | F | `Shell/BaseOfferPopup.cs:742-743` | `Shell/BaseOfferPopup.cs` | 1 |
| 453 | F | `Shell/BoosterInfoPopup.cs:342` | `Shell/BoosterInfoPopup.cs` | 1 |
| 713 | F | `Battle/Label.cs:786-805` | `Battle/Label.cs` | 1 |

### `Shell/ItemDrawer.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 1208 | S | `MenuDraw.cs:1846` | `MenuDraw.cs` 的 `MenuDraw.Text` | 1 |
| 1352 | S | `Battle/Label.cs:1325` | `Battle/Label.cs` 的 `ReclipNow()` | 1 |

### `Shell/LeaderboardRow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 166 | F | `LeaderboardWindow.cs:738` | `LeaderboardWindow.cs` | 1 |

### `Shell/LeaderboardWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 450 | F | `Shell/PracticeModePopup.cs:852` | `Shell/PracticeModePopup.cs` | 1 |
| 686 | F | `Shell/ReferralPopupWindow.cs:386-387` | `Shell/ReferralPopupWindow.cs` | 1 |
| 686 | F | `Shell/BaseOfferPopup.cs:742-743` | `Shell/BaseOfferPopup.cs` | 1 |
| 703 | F | `Editor/CollectionScene.cs:2054-2077` | `Editor/CollectionScene.cs` | 1 |
| 793 | F | `CollectionWindow.cs:1788` | `CollectionWindow.cs` | 1 |
| 793 | F | `BattleLogTab.cs:164` | `BattleLogTab.cs` | 1 |
| 793 | F | `RankedTab.cs:317` | `RankedTab.cs` | 1 |
| 822 | F | `CollectionWindow.cs:1834` | `CollectionWindow.cs` | 1 |

### `Shell/LiveOpsEventWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 390 | F | `Shell/MenuScroll.cs:79` | `Shell/MenuScroll.cs` | 1 |
| 393 | S | `Shell/PointerLayer.cs:223-231` | `Shell/PointerLayer.cs` 的 `SetActive(false)` | 1 |
| 395 | F | `Shell/InboxWindow.cs:405` | `Shell/InboxWindow.cs` | 1 |
| 396 | F | `Shell/WindowsManager.cs:513-519` | `Shell/WindowsManager.cs` | 1 |
| 927 | F | `Core/Loc.cs:471` | `Core/Loc.cs` | 1 |

### `Shell/LoadingMenu.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 115 | F | `Editor/ShellScene.cs:6641` | `Editor/ShellScene.cs` | 1 |

### `Shell/MainMenuRuntime.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 257 | F | `Editor/MainMenuScene.cs:6312` | `Editor/MainMenuScene.cs` | 1 |
| 277 | F | `Deck/DeckRuntime.cs:1223` | `Deck/DeckRuntime.cs` | 1 |
| 824 | S | `Editor/MainMenuScene.cs:676` | `Editor/MainMenuScene.cs` 的 `var nav = menu.Find("Navigation Panel")` | 1 |
| 896 | F | `Editor/MainMenuScene.cs:10902` | `Editor/MainMenuScene.cs` | 1 |
| 1339 | F | `Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs:170` | `Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs` | 1 |
| 1401 | F | `Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs:327-335` | `Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs` | 1 |
| 2954 | F | `Shell/RankedTab.cs:327` | `Shell/RankedTab.cs` | 1 |
| 2964 | S | `Shell/AllianceMemberTab.cs:896` | `Shell/AllianceMemberTab.cs` 的 `ScoreEmpty` | 1 |
| 2998 | F | `Shell/RankedTab.cs:327` | `Shell/RankedTab.cs` | 1 |

### `Shell/MainMenuSubmenuWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 51 | F | `ParentEventWindow.cs:179-186` | `ParentEventWindow.cs` | 1 |
| 54 | S | `ParentEventWindow.cs:492` | `ParentEventWindow.cs` 的 `MenuDraw.GradientV` | 1 |
| 76 | F | `Shell/MainMenuSubmenuWindow.cs:663` | `Shell/MainMenuSubmenuWindow.cs` | 1 |
| 491 | S | `Shell/SocialWindow.cs:178` | `Shell/SocialWindow.cs` 的 `lastIsMaster` | 1 |
| 506 | F | `ParentEventWindow.cs:492` | `ParentEventWindow.cs` | 1 |

### `Shell/MatchLogRow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 194 | S | `BattleLogPopup.cs:238` | `BattleLogPopup.cs` 的 `_content` | 1 |
| 194 | A1220 | `:177` | `Shell/BattleLogPopup.cs` | 1 |
| 194 | A1220 | `:163` | `Shell/BattleLogPopup.cs` | 1 |
| 195 | S | `BattleLogTab.cs:215` | `BattleLogTab.cs` 的 `_content` | 1 |
| 195 | A1220 | `:111` | `Shell/BattleLogTab.cs` | 1 |
| 195 | A1220 | `:89` | `Shell/BattleLogTab.cs` 的 `Build` | 1 |

### `Shell/MenuDraw.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 612 | S | `LayoutSpace.cs:164` | `LayoutSpace.cs` 的 `ToPixel` | 2 |
| 1883 | F | `Battle/Label.cs:549-570` | `Battle/Label.cs` | 1 |
| 2238 | S | `DeckInfoPopup.cs:625` | `DeckInfoPopup.cs` 的 `CloseHit` | 1 |
| 2239 | S | `TrophyInfoPopup.cs:236` | `TrophyInfoPopup.cs` 的 `CloseHit` | 1 |
| 2241 | F | `ImportDeckPopup.cs:103` | `ImportDeckPopup.cs` | 1 |
| 2241 | F | `TrophyInfoPopup.cs:202` | `TrophyInfoPopup.cs` | 1 |
| 2931 | S | `MainMenuScene.cs:64` | `MainMenuScene.cs` 的 `EdgeInset` | 1 |
| 2939 | F | `CollectionScene.cs:143` | `CollectionScene.cs` | 1 |
| 2939 | F | `MainMenuScene.cs:13504` | `MainMenuScene.cs` | 1 |
| 2940 | F | `SettingsScene.cs:290` | `SettingsScene.cs` | 1 |
| 2940 | F | `RewardsScene.cs:115` | `RewardsScene.cs` | 1 |
| 2940 | F | `ShopScene.cs:1111` | `ShopScene.cs` | 1 |

### `Shell/MenuScroll.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 108 | F | `MainMenuRuntime.cs:2206` | `MainMenuRuntime.cs` | 1 |
| 109 | F | `LeaderboardWindow.cs:461` | `LeaderboardWindow.cs` | 1 |
| 109 | F | `RewardWindow.cs:1121` | `RewardWindow.cs` | 1 |
| 109 | F | `ForgeTab.cs:399/443` | `ForgeTab.cs` | 1 |
| 110 | F | `DailyRewardPopup.cs:257` | `DailyRewardPopup.cs` | 1 |
| 110 | F | `CampaignTab.cs:208/372` | `CampaignTab.cs` | 1 |

### `Shell/MissionsTab.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 227 | S | `Shell/DailyData.cs:1165` | `Shell/DailyData.cs` 的 `CanCollectLogin()` | 1 |
| 463 | S | `Battle/Label.cs:1325` | `Battle/Label.cs` 的 `ReclipNow()` | 1 |
| 1038 | F | `Editor/RewardsScene.cs:2209` | `Editor/RewardsScene.cs` | 1 |
| 2127 | F | `TmpFont.cs:498-503` | `TmpFont.cs` | 1 |

### `Shell/OfferContainer.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 826 | F | `Editor/ShopScene.cs:2024` | `Editor/ShopScene.cs` | 1 |

### `Shell/PlayerProfileWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 393 | F | `Shell/ReferralPopupWindow.cs:386-387` | `Shell/ReferralPopupWindow.cs` | 1 |
| 393 | F | `Shell/BaseOfferPopup.cs:742-743` | `Shell/BaseOfferPopup.cs` | 1 |

### `Shell/PointerLayer.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 79 | S | `Battle/RemnantSfx.cs:160` | `Battle/RemnantSfx.cs` 的 `DontDestroyOnLoad` | 1 |
| 80 | F | `Net/NetRuntime.cs:34` | `Net/NetRuntime.cs` | 1 |
| 999 | F | `Battle/BattleDriver.cs:3596-3636` | `Battle/BattleDriver.cs` | 1 |
| 1073 | F | `Battle/SkillPanel.cs:248` | `Battle/SkillPanel.cs` | 1 |
| 1365 | S | `Editor/ShellScene.cs:6641` | `Editor/ShellScene.cs` 的 `LoadingMenu` | 1 |

### `Shell/PopUpGameWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 60 | F | `Shell/RewardWindow.cs:10` | `Shell/RewardWindow.cs` | 1 |
| 208 | F | `Shell/WindowsManager.cs:1517` | `Shell/WindowsManager.cs` | 1 |
| 270 | F | `Shell/WindowsManager.cs:1517` | `Shell/WindowsManager.cs` | 1 |

### `Shell/PracticeModePopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 1027 | F | `Editor/MainMenuScene.cs:2240` | `Editor/MainMenuScene.cs` | 1 |
| 1038 | S | `Editor/CollectionScene.cs:4043` | `Editor/CollectionScene.cs` 的 `ToggleHit` | 1 |

### `Shell/ProfilePage.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 199 | F | `AvatarTab.cs:147` | `AvatarTab.cs` | 1 |
| 199 | A1220 | `:239` | `Shell/AvatarTab.cs` | 1 |
| 201 | F | `AvatarTab.cs:205` | `AvatarTab.cs` | 1 |
| 203 | F | `AvatarTab.cs:224` | `AvatarTab.cs` | 1 |

### `Shell/ProfileTab.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 665 | F | `Shell/RankedEventWindow.cs:110-120` | `Shell/RankedEventWindow.cs` | 1 |
| 1006 | F | `Shell/DeckInfoPopup.cs:1020` | `Shell/DeckInfoPopup.cs` | 1 |

### `Shell/PromptPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 8 | F | `Editor/ShellScene.cs:909` | `Editor/ShellScene.cs` | 1 |
| 49 | F | `Editor/ShellScene.cs:909` | `Editor/ShellScene.cs` | 1 |
| 139 | F | `Shell/PopUpGameWindow.cs:26-32` | `Shell/PopUpGameWindow.cs` | 1 |

### `Shell/PurchasePremiumWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 662 | F | `Shell/InboxWindow.cs:386-387` | `Shell/InboxWindow.cs` | 1 |
| 662 | F | `Shell/ChatPanel.cs:307-308` | `Shell/ChatPanel.cs` | 1 |

### `Shell/RankedRewardEventWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 369 | F | `Shell/InboxWindow.cs:386-387` | `Shell/InboxWindow.cs` | 1 |
| 369 | F | `Shell/ChatPanel.cs:307-308` | `Shell/ChatPanel.cs` | 1 |

### `Shell/RewardWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 253 | F | `Shell/RewardWindow.cs:1120` | `Shell/RewardWindow.cs` | 1 |

### `Shell/SearchingMatchPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 561 | S | `Editor/MainMenuScene.cs:4713` | `Editor/MainMenuScene.cs` 的 `BeginNetWait()` | 1 |

### `Shell/SettingsWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 5 | F | `MainMenuRuntime.cs:359` | `MainMenuRuntime.cs` | 1 |
| 846 | F | `Editor/SettingsScene.cs:1593` | `Editor/SettingsScene.cs` | 1 |
| 858 | F | `SettingsScene.cs:1593` | `SettingsScene.cs` | 1 |
| 1351 | S | `Shell/ForgeTab.cs:98` | `Shell/ForgeTab.cs` 的 `QArmyIcon` | 1 |
| 1547 | F | `Battle/SkillPanel.cs:248` | `Battle/SkillPanel.cs` | 1 |
| 1726 | F | `Shell/InboxWindow.cs:386-387` | `Shell/InboxWindow.cs` | 1 |
| 1851 | S | `Editor/SettingsScene.cs:702` | `Editor/SettingsScene.cs` 的 `BarSepT` | 1 |
| 1862 | S | `Editor/SettingsScene.cs:702` | `Editor/SettingsScene.cs` 的 `BarSep` | 1 |
| 1939 | S | `MainMenuRuntime.cs:1507-1509` | `MainMenuRuntime.cs` 的 `Show()/Hide()` | 1 |
| 1941 | F | `Shell/MainMenuSubmenuWindow.cs:626-632` | `Shell/MainMenuSubmenuWindow.cs` | 1 |
| 1942 | F | `Shell/TopBar.cs:197` | `Shell/TopBar.cs` | 1 |
| 2113 | F | `Battle/SettingsPanel.cs:123-143` | `Battle/SettingsPanel.cs` | 1 |
| 2381 | F | `Loc.cs:257` | `Loc.cs` | 1 |

### `Shell/ShellRuntime.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 117 | F | `LiveOpsEventWindow.cs:1152` | `LiveOpsEventWindow.cs` | 1 |
| 117 | F | `PracticeModePopup.cs:2075` | `PracticeModePopup.cs` | 1 |

### `Shell/ShopData.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 308 | F | `Shell/ShopWindow.cs:791` | `Shell/ShopWindow.cs` | 1 |

### `Shell/SocialPage.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 213 | F | `AllianceMemberTab.cs:1451` | `AllianceMemberTab.cs` | 1 |

### `Shell/SocialView.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 51 | F | `SocialWindow.cs:257` | `SocialWindow.cs` | 1 |

### `Shell/SocialWindow.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 186 | S | `Shell/MainMenuSubmenuWindow.cs:465` | `Shell/MainMenuSubmenuWindow.cs` 的 `BuildShell` | 1 |

### `Shell/ToastNotifications.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 1177 | F | `Battle/BattleCameraSreenSize.cs:38` | `Battle/BattleCameraSreenSize.cs` | 1 |

### `Shell/TopBar.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 69 | S | `Editor/DeckScene.cs:1141-1164` | `Editor/DeckScene.cs` 的 `TestLayout` | 1 |
| 136 | F | `Battle/BattleDriver.cs:3596-3636` | `Battle/BattleDriver.cs` | 1 |

### `Shell/TrophyInfoPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 315 | F | `Shell/InboxWindow.cs:386-387` | `Shell/InboxWindow.cs` | 1 |
| 315 | F | `Shell/ChatPanel.cs:307-308` | `Shell/ChatPanel.cs` | 1 |
| 404 | S | `Battle/Label.cs:519-527` | `Battle/Label.cs` 的 `AlignLeftOn` | 1 |

### `Shell/TutorialModePopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 332 | F | `Shell/MenuScroll.cs:79` | `Shell/MenuScroll.cs` | 1 |
| 335 | S | `Shell/PointerLayer.cs:223-231` | `Shell/PointerLayer.cs` 的 `SetActive(false)` | 1 |
| 338 | F | `TutorialModePopup.cs:293-298` | `TutorialModePopup.cs` | 1 |
| 339 | F | `Shell/WindowsManager.cs:513-519` | `Shell/WindowsManager.cs` | 1 |
| 341 | F | `Shell/InboxWindow.cs:405` | `Shell/InboxWindow.cs` | 1 |
| 379 | F | `Shell/BoosterInfoPopup.cs:316` | `Shell/BoosterInfoPopup.cs` | 1 |
| 517 | S | `Shell/ViewportClip.cs:69-73` | `Shell/ViewportClip.cs` 的 `grep -rn "AddComponent<ViewportClip>"` | 1 |

### `Shell/UIBakeScreen.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 73 | S | `Shell/MissionsTab.cs:1973` | `Shell/MissionsTab.cs` 的 `x42AB248` | 1 |

### `Shell/WhereToGetCardsPopup.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 407 | F | `MenuDraw.cs:30` | `MenuDraw.cs` | 1 |

### `Shell/WindowSounds.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 111 | F | `Shell/GameWindow.cs:488` | `Shell/GameWindow.cs` | 1 |

### `Shell/WindowsManager.cs`

| 行 | 类 | 原串 | → 新串 | 次数 |
|---:|---|---|---|---:|
| 252 | S | `Shell/MainMenuWindow.cs:77` | `Shell/MainMenuWindow.cs` 的 `override void ToBackground` | 1 |
| 512 | S | `Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs:305` | `Warpforge_code/Scripts/Assembly-CSharp/WindowsManager.cs` 的 `可见性照原版 = `private`` | 1 |
| 563 | F | `Editor/ShellScene.cs:1861` | `Editor/ShellScene.cs` | 1 |
| 605 | S | `WindowsManager.cs:1209-1222` | `WindowsManager.cs` 的 `OpenWindow(baseMenu)` | 1 |
| 610 | F | `Editor/MainMenuScene.cs:10419-10430` | `Editor/MainMenuScene.cs` | 1 |
| 646 | S | `Shell/MainMenuRuntime.cs:562-564` | `Shell/MainMenuRuntime.cs` 的 `=> WindowsManager.OpenByRef(prefabRef, create, closeAll);` | 1 |
| 670 | S | `Shell/MainMenuRuntime.cs:547-548` | `Shell/MainMenuRuntime.cs` 的 `= WindowsManager.PrefabRefChat;` | 1 |

## 三 · 跳过清单 / 命中数不一致清单

### 3·1 跳过（2 条，H 类 —— 按 §3·1 口径**不改**）

| # | 宿主 | 原串 | 为什么跳过 |
|---:|---|---|---|
| 1 | `Shell/CollectionData.cs:325` | `Core/Loc.cs:1050-1065` | H：句中「本期行号订正（铁律 5）：上面原来写的是 …」—— 行号是**被纠正的旧值**，换了就抹掉纠错痕迹 |
| 2 | `Shell/MenuDraw.cs:46` | `CampaignTab.cs:342` | H：「2026-10-11 就地订正（铁律 5，A298）：**这里原来写** `CampaignTab.cs:342`」—— 同上 |

### 3·2 命中数与表不一致的 6 条（**已用行内定位落地**，逐条核过，非盲替）

> 口径说明：这 6 条的「多出来」全部是**同族串的前缀/包含关系**（如 `X.cs:1357` 是 `Shell/X.cs:1357` 的子串），或**同一行内并列两个引用**。
> 处理方式统一为：**只在表指定的那一行替换，且替换后逐行回读校验**（结果见 §四）。没有做任何整文件全局替换。

| # | 宿主 | 原串 | 表内命中 | 现读命中 | 多出来的原因 | 结果 |
|---:|---|---|---:|---:|---|---|
| 1 | `Shell/AllianceMemberOptionsPopup.cs:477` | `AllianceMemberTab.cs:1357` | 1 | 2 | 第 68 行有更长的 `Shell/AllianceMemberTab.cs:1357`（本行是它的子串）；先处理长串后本行只剩 1 | 已替换 |
| 2 | `Shell/DeckSelectionPopup.cs:355` | `Editor/MainMenuScene.cs:2651` | 2 | 3 | 同串还出现在本文件 629 / 1000 行（各自是独立条目） | 已替换（行内） |
| 3 | `Shell/DeckSelectionPopup.cs:1000` | `Editor/MainMenuScene.cs:2651` | 2 | 3 | 同上 | 已替换（行内） |
| 4 | `Shell/MenuDraw.cs:612` | `LayoutSpace.cs:164` | 1 | 2 | **同一行并列两处**（表里也列了两条同内容条目）⇒ 2 条引用 | 已替换（两处同改） |
| 5 | `Shell/MenuDraw.cs:612` | `LayoutSpace.cs:164` | 1 | 2 | 同上（第 4 条 = 并列的第二个引用） | 已替换（同上） |
| 6 | `Shell/SettingsWindow.cs:858` | `SettingsScene.cs:1593` | 1 | 2 | 第 846 行有更长的 `Editor/SettingsScene.cs:1593`（本行是它的子串） | 已替换 |

### 3·3 落地时的必要 token 扩展（11 处，避免残留 `/609`、`,4733` 这类碎片）

> 表里的「原串」是**内容锚点**，有 11 处它只匹配到「文件:第一个行号」，后面还挂着 `/第二个行号` 或 `,第二/三/四个行号`。
> 若按字面只替换前半截，会留下 `ForgeTab.cs/609`、`BeginNetWait(),4733` 这类**碎片**（行号反而更刺眼）。
> 所以这 11 处把匹配**扩到完整行号 token**，替换串仍严格用表里给的新串 —— 语义零改动。

| # | 宿主 | 实际匹配的完整原串 | → 新串 | 若不扩展会残留 |
|---:|---|---|---|---|
| 1 | `Shell/DeckSelectionPopup.cs:629` | `Editor/MainMenuScene.cs:2651/2670` | `Editor/MainMenuScene.cs` 的 `ScopeText` | `/2670` |
| 2 | `Shell/CollectionData.cs:272` | `Deck/DeckRuntime.cs:3950/3951/3982` | `Deck/DeckRuntime.cs` | `/3951/3982` |
| 3 | `Shell/MenuScroll.cs:109` | `ForgeTab.cs:399/443` | `ForgeTab.cs` | `/443` |
| 4 | `Shell/MenuScroll.cs:110` | `CampaignTab.cs:208/372` | `CampaignTab.cs` | `/372` |
| 5 | `Shell/DeckEditingWindow.cs:40` | `CollectionWindow.cs:3622/3698` | `CollectionWindow.cs` | `/3698` |
| 6 | `Shell/CampaignTab.cs:1339` | `ForgeTab.cs:572/609` | `ForgeTab.cs` | `/609` |
| 7 | `Shell/CampaignTab.cs:1362` | `Shell/ForgeTab.cs:572/609` | `Shell/ForgeTab.cs` | `/609` |
| 8 | `Shell/GameWindow.cs:139` | `Editor/RewardsScene.cs:1511,1513,1520,2817` | `Editor/RewardsScene.cs` 的 `RenderClip` | `,1513,1520,2817` |
| 9 | `Shell/GameWindow.cs:322` | `RewardsScene.cs:1511,1513` | `RewardsScene.cs` | `,1513` |
| 10 | `Shell/ItemDrawer.cs:1208` | `MenuDraw.cs:1846,1850,1853,1856` | `MenuDraw.cs` 的 `MenuDraw.Text` | `,1850,1853,1856` |
| 11 | `Shell/SearchingMatchPopup.cs:561` | `Editor/MainMenuScene.cs:4713,4733` | `Editor/MainMenuScene.cs` 的 `BeginNetWait()` | `,4733` |

> `A1220 #33`（`Shell/DeckSelectionPopup.cs:477` 的 `:573`）同类处理：实际 token 是 `:573-579`，按 `:573-579` 整段替换成 `Shell/DeckSelectionPopup.cs`（否则残留 `-579`）。

## 四 · 验证

1. **逐行回读校验**：224 条全部通过 —— 宿主行**不再含原串**、**已含新串**；H 两行原样保留。
2. **行尾**：63 个被改文件逐个二进制读 —— `\r\n` 计数 **全为 0**（纯 LF 原样保持）。
3. **没有整篇重写**：`git diff --numstat` 每个文件增删相等（如 `MenuDraw.cs 8/8`、`SettingsWindow.cs 13/13`、`DeckSelectionPopup.cs 10/10`），最大 13 行。
4. **全部改动都落在注释/字符串**：186 个被改行里 185 行是 `//` / `///` 注释；唯一 1 行是断言文案的字符串拼接续行（`MainMenuRuntime.cs:2998` 的 `+ "…"`），**无可执行逻辑、无数值、无断言期望改动**。
5. **秒级类型检查**（`TMPDIR=/tmp/wf_tool2 bash d:/4/Unity/工具/typecheck.sh`）：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

## 五 · 复跑用的 grep 命令

```bash
cd d:/4/Unity/MyGame/Assets/CardPresentation/Shell

# ① H 两条必须还在（各应命中 1 次）
grep -nF 'Core/Loc.cs:1050-1065' CollectionData.cs
grep -nF 'CampaignTab.cs:342' MenuDraw.cs

# ② 本批替换过的引用应归零（示例如下；完整原串见 §二 清单）
grep -nF 'Editor/MainMenuScene.cs:2651' DeckSelectionPopup.cs      # 期望 0
grep -nF 'LayoutSpace.cs:164' MenuDraw.cs                          # 期望 0
grep -nF 'Editor/SettingsScene.cs:702' SettingsWindow.cs           # 期望 0
grep -nF 'Shell/DeckInfoPopup.cs:66-70' DeckSelectionPopup.cs      # 期望 0
grep -nF 'Core/Loc.cs:3365-3385' GiveFeedbackPopup.cs              # 期望 0

# ③ 整族复扫（Shell 族还剩多少「文件.cs:行号」；CollectionWindow.cs 不在本批，排除）
grep -nE '[A-Za-z_][A-Za-z0-9_./!-]*\.(cs|c):[0-9]+' *.cs | grep -v '^CollectionWindow.cs:'
```

## 六 · 顺手发现（只报不改）

1. **表里给出的新串有几处会在句中「重复一次符号」**（照表落地后读起来是 `的 X 的 X`）。这是**普查新串本身**的形态，不是替换错位。位置：`AllianceMemberTab.cs:922`（`ScoreEmpty`）、`CardDetailPopup.cs:1531`（`CheckOwnership`）、`CardDetailPopup.cs:1568`（`leftStyleButton`）、`DeckInfoPopup.cs:1961`（`bool showX = n > 1;`）、`EnergySinglePlayerOnlyEventWindow.cs:457`（`lb.SetWrapping(false)`）、`TopBar.cs:69`（`TestLayout`）、`WindowsManager.cs:646` / `:670`（`OpenByRef(...)` / `PrefabRefChat`）、`SettingsWindow.cs:1939`（`Show()/Hide()`）。**建议下一波统一做一次「去重润色」**（本波按口径不改新串）。
2. **同一句被两条独立条目各改一次 ⇒ 文件名重复**：`DeckSelectionPopup.cs:57`（`Shell/DeckInfoPopup.cs` 两遍）、`:355` / `:1000`（`Editor/MainMenuScene.cs` 两遍）、`:477`（`Shell/DeckSelectionPopup.cs` 两遍）、`ChatTab.cs:170`（`AllianceMemberTab.cs` / `Shell/AllianceMemberTab.cs` 的 `PadL`）。属 S(§2) 与 A1220(§3·3) 两条表分别指向同一句的副产物；语义没错，只是啰嗦。
3. **`WindowsManager.cs:512` 的新串自带反引号**（`… 的 `可见性照原版 = `private``），套在原有反引号里会**破坏 markdown 的行内代码配对**。照表落地，未改。
4. **同批范围内仍有「本波不动」的裸行号**（按 §3·3 口径属 `LOOKBACK` / `samefile?` / `unknown`，本波不碰）：`DeckSelectionPopup.cs:478` 的 `/:2429/:2432`；`CampaignTab.cs:1339` / `:1362` 的 `:658/674` · `:326/342` · `:619/656` · `:741/764`（后者是**迁移前原话留痕**，按 §3·3 明确剔除）；`BoosterInfoPopup.cs:346` 的 `ApplyPxRect:180-183`。
5. **`DailyRewardPopup.cs` 在我编辑前已有另一代理的未提交改动**（`N81`，+24 行，mtime 差 16 分钟）。我只改了第 252 行的一处注释；`git diff --numstat` 现为 `25/1`（= 24 行原有新增 + 我这一行），**没有覆盖对方内容**。
6. **`Shell/CollectionWindow.cs` 整份按调度口径跳过**（普查 10 条 + §3·3 的 `:113` / `:6870` 两条）。该文件在本批期间仍被另一代理改动（mtime 2.8 分钟），交接时请让那条线补落地。
7. **文件 `ProfileData.cs` 虽有未提交改动（41/2），但普查 §2 里它只是「被引用方」、不是宿主** ⇒ 本批对它**零改动**（numstat 与基线相同）。

