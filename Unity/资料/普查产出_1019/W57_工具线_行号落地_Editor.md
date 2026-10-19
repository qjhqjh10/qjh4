# W57 · 工具·文档线 — 行号落地（Editor 族）报告

> 执行代理 **TOOL-3** · 第五十七会话 · 2026-10-19。本波口径 = **只做不验**：
> ⛔ 没跑 Unity / `*.Run` / `_run_8_checks.sh` / player；⛔ 没动 git；⛔ 没改正本；
> 只跑了一次秒级 `typecheck.sh`。改动范围 = `Unity/MyGame/Assets/CardPresentation/Editor/*.cs`，
> **排除** `BattleScene.cs` / `ShopScene.cs` / `MainMenuScene.cs`（别人正在改）。
> 只改注释 / 字符串里的**写死行号**，**没改任何可执行逻辑、数值、断言期望**（C# 里反引号不可能是代码 ⇒ 所有命中天然在注释或字符串里）。

## 〇 · 一句话结果

- **应用 241 处**（§2 表 **227** + §3·3 `A1220` **14**）· **跳过 0 处** · 覆盖 12 份文件。
- `typecheck`：**运行时 0 / 编辑器 0**。
- census 正则复量（12 份文件合计）：`文件.cs:NNN` 引用 **486 → 259（−227）**，
  与 §2 表逐文件行数**逐一对上**（见 §一）。
- 行尾 / BOM **逐份原样**（`DeckScene` / `CardBaseDemo` / `CardFaceProbe` = CRLF；其余 LF；
  `CollectionScene` / `RewardsScene` / `SettingsScene` 保留 BOM）；`git diff --numstat` 增量
  = 每改 1 行就 +1/−1，**没有整篇翻**（见 §五）。

## 一 · 总表（文件 / 应用 / 跳过）

| 文件 | 应用合计 | §2（文件.cs:行号） | §3·3（A1220 裸 :NNN） | 跳过 | 引用计数 前→后 |
|---|---:|---:|---:|---:|---|
| `Editor/CollectionScene.cs` | 53 | 52 | 1 | 0 | 85 → 33 |
| `Editor/ShellScene.cs` | 53 | 49 | 4 | 0 | 71 → 22 |
| `Editor/RewardsScene.cs` | 52 | 48 | 4 | 0 | 118 → 70 |
| `Editor/NetSelfTest.cs` | 26 | 23 | 3 | 0 | 25 → 2 |
| `Editor/DeckScene.cs` | 22 | 22 | 0 | 0 | 86 → 64 |
| `Editor/NetBattleTest.cs` | 9 | 9 | 0 | 0 | 43 → 34 |
| `Editor/SettingsScene.cs` | 7 | 7 | 0 | 0 | 31 → 24 |
| `Editor/IconSetup.cs` | 6 | 5 | 1 | 0 | 6 → 1 |
| `Editor/MenuCheck.cs` | 6 | 5 | 1 | 0 | 5 → 0 |
| `Editor/CardBaseDemo.cs` | 4 | 4 | 0 | 0 | 9 → 5 |
| `Editor/CardFaceProbe.cs` | 2 | 2 | 0 | 0 | 6 → 4 |
| `Editor/T09Probe.cs` | 1 | 1 | 0 | 0 | 1 → 0 |
| **合计** | **241** | **227** | **14** | **0** | **486 → 259（−227）** |

> 「引用计数」= census §五① 那条正则 `[A-Za-z_][A-Za-z0-9_./!-]*\.(cs|c):[0-9]+` 在本文件里的匹配数。
> 计数前后差 **227** = §2 的行数（A1220 是裸 `:NNN`，不进这条正则）⇒ 与表**逐文件相等**。

## 二 · 落地规则（本波实际执行口径，逐条可复跑）

1. **定位用内容锚点、不用表里的行号** —— 表里的行号已漂（例：`CollectionScene` 表 9274 → 现 9443；
   `RewardsScene` 表 9657 → 现 9732；`NetSelfTest` 表 2474 → 现 2481；`RewardsScene` 表 11439 → 现 11514）。
2. **命中数核验按「左边界 token」**：排除更长路径的前缀匹配（例：`SearchingMatchPopup.cs:438`
   不再把 `Shell/SearchingMatchPopup.cs:438-440` 算成一次）。
3. **右续行号整 token 合并**：原串后若紧跟 `-NNN` / `,NNN` / `/NNN` / `/:NNN` 的续写，
   把**同一个反引号 token 整段**一起换掉（否则会留下 `-153` / `,1086` 这类残号）。本波共 **21 处**命中这种续写。
4. 整串必须**位于反引号内**才替换；241 处全部满足（`lb`/`rb` 双真）。
5. 多命中行：用表里「本行原文」消歧；同一行两条的按表顺序逐条处理。
6. `A1220`：按表里「宿主行原文」**整行定位**，把 `:NNN`（含续写）整个反引号 token 换成建议串；
   **同文件先做 A1220 再做 §2**（否则 §2 会先改掉宿主行、导致 A1220 定位失败）。
7. **H 类**：本波 12 份文件里**没有 H 行** —— H 的 5 条全在 `MainMenuScene` / `ShopScene` / `Shell` 族（不在白名单）。
   **一条 H 都没碰**（§3·1 逐条核过宿主不在白名单）。

## 三 · 逐条改动清单

> 行号 = **改后的现读行号**（本波只换 token、不增删行，行号与改前同一行）。
> `类` 列：`S/F` 来自 §2 表；`A1220-S/F` 来自 §3·3。

### `Editor/CollectionScene.cs` — 应用 53（§2 52 + A1220 1）

| # | 行 | 类 | 原串 | → 新串 |
|---:|---:|---|---|---|
| 1 | 4042 | A1220-F | `:490` | `Shell/DeckSelectionPopup.cs` |
| 2 | 30 | F | `Editor/RewardWindowFixture.cs:12-14` | `Editor/RewardWindowFixture.cs` |
| 3 | 96 | S | `Shell/DeckInfoPopup.cs:213-218` | `Shell/DeckInfoPopup.cs` 的 `HitZFront` |
| 4 | 433 | S | `Core/CardText.cs:290` | `Core/CardText.cs` 的 `sprite` |
| 5 | 441 | S | `Shell/MenuDraw.cs:1144-1190` | `Shell/MenuDraw.cs` 的 `ClipTmpMesh` |
| 6 | 720 | S | `Shell/MenuDraw.cs:74-78` | `Shell/MenuDraw.cs` 的 `t.parent.lossyScale` |
| 7 | 721 | F | `Shell/MenuDraw.cs:57-61` | `Shell/MenuDraw.cs` |
| 8 | 768 | F | `Shell/MenuDraw.cs:74-78` | `Shell/MenuDraw.cs` |
| 9 | 814 | S | `Shell/PointerLayer.cs:47-48` | `Shell/PointerLayer.cs` 的 `WindowButton` |
| 10 | 924 | F | `WarpforgeArena1/Editor/AnimFXCheck.cs:341-366` | `WarpforgeArena1/Editor/AnimFXCheck.cs` |
| 11 | 961 | F | `Editor/MainMenuScene.cs:416-440` | `Editor/MainMenuScene.cs` |
| 12 | 1146 | S | `Battle/Label.cs:784` | `Battle/Label.cs` 的 `NewText` |
| 13 | 1152 | S | `Battle/Label.cs:454-464` | `Battle/Label.cs` 的 `RefreshBounds()` |
| 14 | 1422 | S | `Shell/PointerLayer.cs:488` | `Shell/PointerLayer.cs` 的 `isActiveAndEnabled` |
| 15 | 1846 | F | `Shell/PointerLayer.cs:492` | `Shell/PointerLayer.cs` |
| 16 | 1943 | S | `Shell/DeckInfoPopup.cs:1228` | `Shell/DeckInfoPopup.cs` 的 `wl != null ? CardText.Name(wl) : Loc.T("MenuDeck/Error/NoWarlord")` |
| 17 | 1947 | S | `Core/Loc.cs:927` | `Core/Loc.cs` 的 `Loc.T(键)` |
| 18 | 1952 | F | `Shell/DeckInfoPopup.cs:1228` | `Shell/DeckInfoPopup.cs` |
| 19 | 2506 | S | `Shell/DeckInfoPopup.cs:1168-1201` | `Shell/DeckInfoPopup.cs` 的 `showGmIcon` |
| 20 | 2553 | F | `Editor/MainMenuScene.cs:4067` | `Editor/MainMenuScene.cs` |
| 21 | 2830 | F | `Shell/DeckInfoPopup.cs:207-209` | `Shell/DeckInfoPopup.cs` |
| 22 | 3753 | S | `Shell/DeckInfoPopup.cs:270-274` | `Shell/DeckInfoPopup.cs` 的 `IsItemShown` |
| 23 | 3987 | F | `Editor/RewardsScene.cs:8126-8141` | `Editor/RewardsScene.cs` |
| 24 | 4002 | F | `Shell/DeckSelectionPopup.cs:356` | `Shell/DeckSelectionPopup.cs` |
| 25 | 4018 | S | `Shell/PointerLayer.cs:492` | `Shell/PointerLayer.cs` 的 `CollectHits` |
| 26 | 4042 | F | `Shell/DeckSelectionPopup.cs:357-363` | `Shell/DeckSelectionPopup.cs` |
| 27 | 4055 | F | `Shell/DeckSelectionPopup.cs:490-493` | `Shell/DeckSelectionPopup.cs` |
| 28 | 4258 | S | `Shell/DeckInfoPopup.cs:1284` | `Shell/DeckInfoPopup.cs` 的 `Loc.T("MenuDeck/Error/HiddenCards") + hiddenWhy` |
| 29 | 4336 | F | `Shell/MenuDraw.cs:57-61` | `Shell/MenuDraw.cs` |
| 30 | 4530 | S | `Core/Loc.cs:1128-1129` | `Core/Loc.cs` 的 `ImportBadString` |
| 31 | 4680 | S | `Shell/CollectionWindow.cs:225-242` | `Shell/CollectionWindow.cs` 的 `CardsCounterH` |
| 32 | 5276 | F | `Editor/CardFaceProbe.cs:273-296` | `Editor/CardFaceProbe.cs` |
| 33 | 6028 | S | `Shell/PointerLayer.cs:488` | `Shell/PointerLayer.cs` 的 `WindowButton` |
| 34 | 6792 | F | `Shell/CollectionWindow.cs:128` | `Shell/CollectionWindow.cs` |
| 35 | 6920 | S | `Shell/DeckSelectionPopup.cs:731` | `Shell/DeckSelectionPopup.cs` 的 `pick.OwnIndex == DeckIndex` |
| 36 | 7404 | S | `Shell/CardDetailPopup.cs:1398-1424` | `Shell/CardDetailPopup.cs` 的 `LatinCardBody` |
| 37 | 8250 | F | `Shell/CollectionWindow.cs:1547` | `Shell/CollectionWindow.cs` |
| 38 | 8571 | F | `Shell/MenuDraw.cs:1614` | `Shell/MenuDraw.cs` |
| 39 | 8584 | S | `Shell/ViewportClip.cs:196` | `Shell/ViewportClip.cs` 的 `PosInDesignSpace(transform)` |
| 40 | 8586 | F | `Shell/MenuScroll.cs:398` | `Shell/MenuScroll.cs` |
| 41 | 9364 | S | `Shell/AvatarTab.cs:167` | `Shell/AvatarTab.cs` 的 `vc.CaptureNow();` |
| 42 | 9364 | S | `Shell/TitleTab.cs:140` | `Shell/TitleTab.cs` 的 `vc.CaptureNow();` |
| 43 | 9394 | F | `Shell/AvatarTab.cs:167` | `Shell/AvatarTab.cs` |
| 44 | 9397 | F | `Shell/TitleTab.cs:140` | `Shell/TitleTab.cs` |
| 45 | 9443 | F | `Shell/ChatPanel.cs:152` | `Shell/ChatPanel.cs` |
| 46 | 9574 | S | `Editor/MainMenuScene.cs:172` | `Editor/MainMenuScene.cs` 的 `BackgroundHit` |
| 47 | 9623 | F | `Shell/PracticeModePopup.cs:494` | `Shell/PracticeModePopup.cs` |
| 48 | 9670 | F | `DeckRuntime.cs:254` | `DeckRuntime.cs` |
| 49 | 9943 | S | `Editor/BattleScene.cs:210` | `Editor/BattleScene.cs` 的 `Debug.Log` |
| 50 | 10473 | F | `Shell/ViewportClip.cs:246` | `Shell/ViewportClip.cs` |
| 51 | 10573 | S | `Shell/DeckInfoPopup.cs:1438` | `Shell/DeckInfoPopup.cs` 的 `ExportSuccesful` |
| 52 | 10784 | F | `Shell/WindowsManager.cs:1007` | `Shell/WindowsManager.cs` |
| 53 | 10792 | F | `Shell/WindowsManager.cs:1007` | `Shell/WindowsManager.cs` |

### `Editor/ShellScene.cs` — 应用 53（§2 49 + A1220 4）

| # | 行 | 类 | 原串 | → 新串 |
|---:|---:|---|---|---|
| 1 | 4830 | A1220-F | `:1393` | `Shell/MenuDraw.cs` |
| 2 | 4830 | A1220-F | `:1386` | `Shell/MenuDraw.cs` |
| 3 | 5998 | A1220-F | `:417` | `Shell/RankedTab.cs` |
| 4 | 7444 | A1220-F | `:549` | `Shell/LiveOpsEventWindow.cs` |
| 5 | 25 | F | `Editor/RewardWindowFixture.cs:12-14` | `Editor/RewardWindowFixture.cs` |
| 6 | 97 | F | `Shell/MenuDraw.cs:241-242` | `Shell/MenuDraw.cs` |
| 7 | 98 | S | `Shell/ViewportClip.cs:169-191` | `Shell/ViewportClip.cs` 的 `MenuDraw.QuadRectPx` |
| 8 | 104 | S | `MenuDraw.cs:249` | `MenuDraw.cs` 的 `SameRectNear` |
| 9 | 654 | S | `Shell/MenuDraw.cs:74-78` | `Shell/MenuDraw.cs` 的 `t.parent.lossyScale` |
| 10 | 655 | F | `Shell/MenuDraw.cs:57-61` | `Shell/MenuDraw.cs` |
| 11 | 695 | F | `Shell/MenuDraw.cs:74-78` | `Shell/MenuDraw.cs` |
| 12 | 952 | F | `WarpforgeArena1/Editor/AnimFXCheck.cs:341-366` | `WarpforgeArena1/Editor/AnimFXCheck.cs` |
| 13 | 1521 | S | `ShellScene.cs:620` | `ShellScene.cs` 的 `MessageText` |
| 14 | 1937 | S | `Editor/ShellScene.cs:1327` | `Editor/ShellScene.cs` 的 `R2_A799` |
| 15 | 2448 | S | `Shell/MenuWindowBase.cs:284` | `Shell/MenuWindowBase.cs` 的 `parent` |
| 16 | 2951 | F | `Editor/MainMenuScene.cs:10406-10443` | `Editor/MainMenuScene.cs` |
| 17 | 2955 | S | `MainMenuRuntime.cs:509` | `MainMenuRuntime.cs` 的 `WindowType` |
| 18 | 3684 | S | `Editor/RewardsScene.cs:1511` | `Editor/RewardsScene.cs` 的 `RenderClip` |
| 19 | 4161 | F | `Shell/GenericOptionsPanel.cs:462-492` | `Shell/GenericOptionsPanel.cs` |
| 20 | 4358 | S | `Shell/BattleLogTab.cs:172-177` | `Shell/BattleLogTab.cs` 的 `WindowsManager.OpenByRef` |
| 21 | 4466 | F | `Shell/PracticeModePopup.cs:494` | `Shell/PracticeModePopup.cs` |
| 22 | 4830 | F | `Shell/MenuDraw.cs:1409` | `Shell/MenuDraw.cs` |
| 23 | 5206 | F | `Shell/InboxWindow.cs:54` | `Shell/InboxWindow.cs` |
| 24 | 5332 | S | `Shell/LiveOpsEventWindow.cs:614` | `Shell/LiveOpsEventWindow.cs` 的 `vc.CaptureNow();` |
| 25 | 5340 | S | `Shell/LiveOpsEventWindow.cs:614` | `Shell/LiveOpsEventWindow.cs` 的 `vc.CaptureNow();` |
| 26 | 5396 | F | `Editor/RewardsScene.cs:8126-8141` | `Editor/RewardsScene.cs` |
| 27 | 5413 | F | `Shell/LiveOpsEventWindow.cs:378` | `Shell/LiveOpsEventWindow.cs` |
| 28 | 5546 | S | `Shell/PracticeModePopup.cs:883` | `Shell/PracticeModePopup.cs` 的 `deckVc.CaptureNow();` |
| 29 | 5553 | S | `Shell/PracticeModePopup.cs:883` | `Shell/PracticeModePopup.cs` 的 `armyVc.CaptureNow();` |
| 30 | 5556 | S | `Shell/PracticeModePopup.cs:1051` | `Shell/PracticeModePopup.cs` 的 `deckVc.CaptureNow();` |
| 31 | 5586 | F | `Shell/ViewportClip.cs:433-440` | `Shell/ViewportClip.cs` |
| 32 | 5597 | S | `Shell/PracticeModePopup.cs:883` | `Shell/PracticeModePopup.cs` 的 `armyVc.CaptureNow();` |
| 33 | 5823 | F | `Editor/RewardsScene.cs:8126-8141` | `Editor/RewardsScene.cs` |
| 34 | 5835 | F | `Shell/PracticeModePopup.cs:754` | `Shell/PracticeModePopup.cs` |
| 35 | 5863 | F | `Shell/TutorialModePopup.cs:510` | `Shell/TutorialModePopup.cs` |
| 36 | 5865 | F | `Shell/TutorialModePopup.cs:170` | `Shell/TutorialModePopup.cs` |
| 37 | 5875 | F | `Shell/TutorialModePopup.cs:510` | `Shell/TutorialModePopup.cs` |
| 38 | 5890 | S | `Shell/TutorialModePopup.cs:324` | `Shell/TutorialModePopup.cs` 的 `Open()` |
| 39 | 5891 | F | `Editor/RewardsScene.cs:8126-8141` | `Editor/RewardsScene.cs` |
| 40 | 5901 | F | `Shell/TutorialModePopup.cs:324` | `Shell/TutorialModePopup.cs` |
| 41 | 5998 | F | `Shell/RankedTab.cs:285` | `Shell/RankedTab.cs` |
| 42 | 6223 | S | `Shell/LeaderboardWindow.cs:210-224` | `Shell/LeaderboardWindow.cs` 的 `AttachToAnchor` |
| 43 | 6227 | F | `LeaderboardWindow.cs:488` | `LeaderboardWindow.cs` |
| 44 | 6764 | S | `Battle/CombatCameraZoom.cs:85` | `Battle/CombatCameraZoom.cs` 的 `EventSystem` |
| 45 | 6771 | S | `Shell/CampaignTab.cs:459` | `Shell/CampaignTab.cs` 的 `localScale` |
| 46 | 6941 | F | `Shell/WindowsManager.cs:1342-1460` | `Shell/WindowsManager.cs` |
| 47 | 6980 | S | `RankedEventWindow.cs:243` | `RankedEventWindow.cs` 的 `SearchingOpponentWindow` |
| 48 | 7444 | F | `Shell/LiveOpsEventWindow.cs:536` | `Shell/LiveOpsEventWindow.cs` |
| 49 | 7505 | F | `Editor/BattleScene.cs:17694` | `Editor/BattleScene.cs` |
| 50 | 7656 | S | `Shell/EnergySinglePlayerOnlyEventWindow.cs:694-700` | `Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `txFit` |
| 51 | 7873 | F | `Editor/CollectionScene.cs:6144` | `Editor/CollectionScene.cs` |
| 52 | 8003 | F | `Shell/MainMenuRuntime.cs:496` | `Shell/MainMenuRuntime.cs` |
| 53 | 8161 | F | `UIBakeScreen.cs:59` | `UIBakeScreen.cs` |

### `Editor/RewardsScene.cs` — 应用 52（§2 48 + A1220 4）

| # | 行 | 类 | 原串 | → 新串 |
|---:|---:|---|---|---|
| 1 | 5672 | A1220-S | `:892` | `Shell/WindowsManager.cs` 的 `ClearReuseCacheForTest` |
| 2 | 7342 | A1220-S | `:248` | `Shell/CampaignTab.cs` 的 `NoIconRewards` |
| 3 | 8268 | A1220-F | `:852` | `Shell/DailyData.cs` |
| 4 | 11514 | A1220-S | `:582` | `Shell/CampaignTab.cs` 的 `NodeRectForTest` |
| 5 | 27 | F | `Editor/RewardWindowFixture.cs:12-14` | `Editor/RewardWindowFixture.cs` |
| 6 | 760 | S | `Shell/MenuDraw.cs:74-78` | `Shell/MenuDraw.cs` 的 `t.parent.lossyScale` |
| 7 | 799 | F | `Shell/MenuDraw.cs:74-78` | `Shell/MenuDraw.cs` |
| 8 | 831 | S | `Shell/PointerLayer.cs:47-48` | `Shell/PointerLayer.cs` 的 `Awake/OnEnable` |
| 9 | 951 | S | `Shell/WindowsManager.cs:716` | `Shell/WindowsManager.cs` 的 `WindowsManager.OpenWindow` |
| 10 | 953 | F | `Shell/PointerLayer.cs:1093-1097` | `Shell/PointerLayer.cs` |
| 11 | 988 | F | `WarpforgeArena1/Editor/AnimFXCheck.cs:341-366` | `WarpforgeArena1/Editor/AnimFXCheck.cs` |
| 12 | 1031 | S | `MainMenuSubmenuWindow.cs:498` | `MainMenuSubmenuWindow.cs` 的 `MenuDraw.GradientV` |
| 13 | 1034 | F | `Editor/MainMenuScene.cs:13110-13126` | `Editor/MainMenuScene.cs` |
| 14 | 2035 | S | `Shell/MissionsTab.cs:540` | `Shell/MissionsTab.cs` 的 `NodeD(parent, "Progress Bar", pb)` |
| 15 | 2195 | F | `Core/Loc.cs:945` | `Core/Loc.cs` |
| 16 | 2199 | F | `Editor/CollectionScene.cs:4650-4655` | `Editor/CollectionScene.cs` |
| 17 | 2203 | F | `Core/Loc.cs:1028-1035` | `Core/Loc.cs` |
| 18 | 3046 | S | `Shell/MissionsTab.cs:248-252` | `Shell/MissionsTab.cs` 的 `DestroySafe` |
| 19 | 3047 | F | `Shell/MenuWindowBase.cs:170-176` | `Shell/MenuWindowBase.cs` |
| 20 | 3049 | F | `Shell/MissionsTab.cs:1237` | `Shell/MissionsTab.cs` |
| 21 | 3234 | F | `Shell/MainMenuRuntime.cs:254` | `Shell/MainMenuRuntime.cs` |
| 22 | 3267 | F | `Editor/MainMenuScene.cs:8578` | `Editor/MainMenuScene.cs` |
| 23 | 3325 | S | `Shell/ForgeTab.cs:367-371` | `Shell/ForgeTab.cs` 的 `FocusClaimable` |
| 24 | 3600 | S | `Shell/ForgeTab.cs:706` | `Shell/ForgeTab.cs` 的 `ForgeArmyItem_` |
| 25 | 4786 | F | `Shell/DeckInfoPopup.cs:966` | `Shell/DeckInfoPopup.cs` |
| 26 | 5255 | F | `Shell/MenuScroll.cs:183-191` | `Shell/MenuScroll.cs` |
| 27 | 5632 | F | `Shell/WindowsManager.cs:1059-1060` | `Shell/WindowsManager.cs` |
| 28 | 5635 | F | `Shell/RewardsWindow.cs:222` | `Shell/RewardsWindow.cs` |
| 29 | 5672 | S | `Shell/WindowsManager.cs:875` | `Shell/WindowsManager.cs` 的 `SetupData(null)` |
| 30 | 5744 | F | `Shell/CampaignRewardWindow.cs:246-260` | `Shell/CampaignRewardWindow.cs` |
| 31 | 6457 | F | `CampaignRewardWindow.cs:62` | `CampaignRewardWindow.cs` |
| 32 | 6713 | S | `Shell/MenuDraw.cs:121` | `Shell/MenuDraw.cs` 的 `GameWindow.Clip` |
| 33 | 6718 | F | `Editor/CollectionScene.cs:2435` | `Editor/CollectionScene.cs` |
| 34 | 6827 | S | `Shell/ItemDrawer.cs:888-893` | `Shell/ItemDrawer.cs` 的 `sizeDelta (50,50)` |
| 35 | 6843 | S | `Shell/MenuDraw.cs:1229-1238` | `Shell/MenuDraw.cs` 的 `MenuDraw.Rect` |
| 36 | 6968 | F | `Shell/WindowsManager.cs:480-486` | `Shell/WindowsManager.cs` |
| 37 | 6976 | F | `Shell/RewardWindow.cs:1006-1023` | `Shell/RewardWindow.cs` |
| 38 | 7342 | F | `Shell/CampaignTab.cs:215-233` | `Shell/CampaignTab.cs` |
| 39 | 8268 | S | `Shell/DailyData.cs:909` | `Shell/DailyData.cs` 的 `_streakClaimed` |
| 40 | 8538 | S | `Shell/DailyRewardPopup.cs:215-219` | `Shell/DailyRewardPopup.cs` 的 `DestroySafe` |
| 41 | 8646 | F | `Shell/RewardWindow.cs:273-276` | `Shell/RewardWindow.cs` |
| 42 | 8941 | F | `Shell/TimerDisplay.cs:218-223` | `Shell/TimerDisplay.cs` |
| 43 | 9732 | S | `Shell/DailyData.cs:1960-1962` | `Shell/DailyData.cs` 的 `ResetMissionsForTest` |
| 44 | 9747 | S | `DailyData.cs:177-178` | `DailyData.cs` 的 `RewriteMarks` |
| 45 | 9909 | F | `Shell/BaseOfferPopup.cs:742` | `Shell/BaseOfferPopup.cs` |
| 46 | 9909 | F | `Shell/ReferralPopupWindow.cs:386` | `Shell/ReferralPopupWindow.cs` |
| 47 | 10089 | S | `Shell/InboxWindow.cs:396` | `Shell/InboxWindow.cs` 的 `SetActive(false)` |
| 48 | 10583 | F | `Shell/DailyData.cs:1960-1962` | `Shell/DailyData.cs` |
| 49 | 11229 | F | `Shell/PointerLayer.cs:47-48` | `Shell/PointerLayer.cs` |
| 50 | 11425 | F | `Shell/CampaignTab.cs:581-584` | `Shell/CampaignTab.cs` |
| 51 | 11440 | S | `CampaignTab.cs:325` | `CampaignTab.cs` 的 `TrackScroll` |
| 52 | 11514 | S | `Shell/CampaignTab.cs:581` | `Shell/CampaignTab.cs` 的 `ToDesignPixel` |

### `Editor/NetSelfTest.cs` — 应用 26（§2 23 + A1220 3）

| # | 行 | 类 | 原串 | → 新串 |
|---:|---:|---|---|---|
| 1 | 939 | A1220-F | `:745` | `Battle/BattleDriver.cs` |
| 2 | 2012 | A1220-F | `:10530` | `Battle/BattleDriver.cs` |
| 3 | 2481 | A1220-F | `:247` | `Net/NetSession.cs` |
| 4 | 28 | F | `Editor/SettingsScene.cs:240` | `Editor/SettingsScene.cs` |
| 5 | 102 | F | `Editor/SettingsScene.cs:1544` | `Editor/SettingsScene.cs` |
| 6 | 938 | S | `Battle/ReplayStore.cs:190` | `Battle/ReplayStore.cs` 的 `ActionLog` |
| 7 | 939 | S | `Battle/BattleDriver.cs:440` | `Battle/BattleDriver.cs` 的 `ActionLog` |
| 8 | 940 | S | `RuleCore/Core/RuleCore.cs:6587-6589` | `RuleCore/Core/RuleCore.cs` 的 `ctx.Events` |
| 9 | 1476 | S | `Shell/SearchingMatchPopup.cs:438-440` | `Shell/SearchingMatchPopup.cs` 的 `OnDisable` |
| 10 | 1492 | F | `SearchingMatchPopup.cs:438` | `SearchingMatchPopup.cs` |
| 11 | 1683 | S | `NetTransport.cs:219` | `NetTransport.cs` 的 `_connected` |
| 12 | 1766 | S | `NetSession.cs:247` | `NetSession.cs` 的 `_t.AcceptedCount == _lastAccepted` |
| 13 | 1845 | S | `NetMatchmaking.cs:487` | `NetMatchmaking.cs` 的 `MsgStart.myName` |
| 14 | 1955 | S | `NetProtocol.cs:157` | `NetProtocol.cs` 的 `MsgReject.reason` |
| 15 | 1956 | S | `NetProtocol.cs:77` | `NetProtocol.cs` 的 `NetPendingBattle.FromStart` |
| 16 | 1960 | F | `NetBattle.cs:237` | `NetBattle.cs` |
| 17 | 1961 | F | `Editor/NetBattleTest.cs:473` | `Editor/NetBattleTest.cs` |
| 18 | 2008 | S | `NetMatchmaking.cs:453` | `NetMatchmaking.cs` 的 `s.PeerName` |
| 19 | 2010 | S | `NetMatchmaking.cs:487` | `NetMatchmaking.cs` 的 `MsgStart.myName` |
| 20 | 2011 | F | `Shell/SearchingOpponentWindow.cs:83` | `Shell/SearchingOpponentWindow.cs` |
| 21 | 2012 | F | `Battle/BattleDriver.cs:10303` | `Battle/BattleDriver.cs` |
| 22 | 2014 | S | `NetMatchmaking.cs:487` | `NetMatchmaking.cs` 的 `ClampPeerText` |
| 23 | 2060 | S | `NetMatchmaking.cs:453` | `NetMatchmaking.cs` 的 `s.PeerName` |
| 24 | 2072 | S | `NetMatchmaking.cs:487` | `NetMatchmaking.cs` 的 `MsgStart.myName` |
| 25 | 2075 | S | `NetMatchmaking.cs:487` | `NetMatchmaking.cs` 的 `ClampPeerText` |
| 26 | 2481 | F | `NetSession.cs:450` | `NetSession.cs` |

### `Editor/DeckScene.cs` — 应用 22（§2 22 + A1220 0）

| # | 行 | 类 | 原串 | → 新串 |
|---:|---:|---|---|---|
| 1 | 46 | F | `Editor/RewardWindowFixture.cs:12-14` | `Editor/RewardWindowFixture.cs` |
| 2 | 359 | S | `Shell/WindowsManager.cs:381` | `Shell/WindowsManager.cs` 的 `GameWindow.TryOpen` |
| 3 | 359 | S | `Shell/TrophyInfoPopup.cs:217` | `Shell/TrophyInfoPopup.cs` 的 `GameWindow` |
| 4 | 765 | F | `WarpforgeArena1/Editor/AnimFXCheck.cs:341` | `WarpforgeArena1/Editor/AnimFXCheck.cs` |
| 5 | 897 | F | `Shell/CollectionData.cs:317` | `Shell/CollectionData.cs` |
| 6 | 897 | F | `PrebuiltDecks.cs:261` | `PrebuiltDecks.cs` |
| 7 | 898 | F | `BattleDriver.cs:2045` | `BattleDriver.cs` |
| 8 | 898 | F | `MainMenuRuntime.cs:328` | `MainMenuRuntime.cs` |
| 9 | 1728 | S | `DeckEditorState.cs:431` | `DeckEditorState.cs` 的 `int ccost = ClampPoolCost(c.Cost);` |
| 10 | 2181 | S | `Deck/DeckEditorState.cs:248` | `Deck/DeckEditorState.cs` 的 `MaxDeckCount` |
| 11 | 2189 | S | `Deck/DeckEditorState.cs:299` | `Deck/DeckEditorState.cs` 的 `ClearDefensive` |
| 12 | 2196 | S | `DeckRuntime.cs:4018` | `DeckRuntime.cs` 的 `DeckEntries()` |
| 13 | 2213 | S | `Deck/DeckEditorState.cs:315` | `Deck/DeckEditorState.cs` 的 `ClearDefensive` |
| 14 | 2309 | F | `Deck/DeckRuntime.cs:2847` | `Deck/DeckRuntime.cs` |
| 15 | 3006 | F | `DeckRuntime.cs:6170` | `DeckRuntime.cs` |
| 16 | 3599 | F | `Shell/MenuDraw.cs:71` | `Shell/MenuDraw.cs` |
| 17 | 5206 | F | `Shell/MenuWindowBase.cs:420` | `Shell/MenuWindowBase.cs` |
| 18 | 6149 | F | `DeckRuntime.cs:4218` | `DeckRuntime.cs` |
| 19 | 7390 | F | `Shell/PopUpGameWindow.cs:238` | `Shell/PopUpGameWindow.cs` |
| 20 | 7585 | S | `Deck/DeckRuntime.cs:7243` | `Deck/DeckRuntime.cs` 的 `LatinCardBody` |
| 21 | 7696 | S | `DeckStore.cs:55` | `DeckStore.cs` 的 `Contains("失败")` |
| 22 | 8225 | F | `Editor/MainMenuScene.cs:1337` | `Editor/MainMenuScene.cs` |

### `Editor/NetBattleTest.cs` — 应用 9（§2 9 + A1220 0）

| # | 行 | 类 | 原串 | → 新串 |
|---:|---:|---|---|---|
| 1 | 44 | F | `RuleCore.cs:3685` | `RuleCore.cs` |
| 2 | 48 | F | `RuleCore.cs:3685` | `RuleCore.cs` |
| 3 | 643 | F | `NetSession.cs:487-494` | `NetSession.cs` |
| 4 | 665 | F | `NetSession.cs:487-494` | `NetSession.cs` |
| 5 | 677 | S | `NetSession.cs:205` | `NetSession.cs` 的 `WaitingReconnect` |
| 6 | 727 | F | `Battle/BattleDriver.cs:22` | `Battle/BattleDriver.cs` |
| 7 | 730 | S | `Editor/BattleScene.cs:12976-12986` | `Editor/BattleScene.cs` 的 `InBattle` |
| 8 | 731 | F | `Shell/WindowsManager.cs:65-70` | `Shell/WindowsManager.cs` |
| 9 | 1206 | S | `RuleCore.cs:3685` | `RuleCore.cs` 的 `RuleCore.Forfeit` |

### `Editor/SettingsScene.cs` — 应用 7（§2 7 + A1220 0）

| # | 行 | 类 | 原串 | → 新串 |
|---:|---:|---|---|---|
| 1 | 37 | F | `Editor/RewardWindowFixture.cs:12-14` | `Editor/RewardWindowFixture.cs` |
| 2 | 585 | F | `WarpforgeArena1/Editor/AnimFXCheck.cs:341-366` | `WarpforgeArena1/Editor/AnimFXCheck.cs` |
| 3 | 2539 | S | `Editor/ShellScene.cs:1699-1786` | `Editor/ShellScene.cs` 的 `Cam.aspect` |
| 4 | 3149 | F | `Shell/WindowsManager.cs:100` | `Shell/WindowsManager.cs` |
| 5 | 3216 | F | `Battle/WfSlider.cs:158` | `Battle/WfSlider.cs` |
| 6 | 4160 | F | `Editor/CollectionScene.cs:4259-4277` | `Editor/CollectionScene.cs` |
| 7 | 5501 | F | `Shell/PointerLayer.cs:47-48` | `Shell/PointerLayer.cs` |

### `Editor/IconSetup.cs` — 应用 6（§2 5 + A1220 1）

| # | 行 | 类 | 原串 | → 新串 |
|---:|---:|---|---|---|
| 1 | 1240 | A1220-S | `:448` | `Core/CardIcons.cs` 的 `_rxFaithCostZh` |
| 2 | 314 | F | `Core/CardIcons.cs:238` | `Core/CardIcons.cs` |
| 3 | 864 | S | `BattleDriver.cs:11959` | `BattleDriver.cs` 的 `IsNullOrEmpty` |
| 4 | 866 | S | `BattleDriver.cs:11975` | `BattleDriver.cs` 的 `KeywordSegment` |
| 5 | 901 | S | `BattleDriver.cs:11975` | `BattleDriver.cs` 的 `CardText.KeywordSegment` |
| 6 | 1240 | S | `Core/CardIcons.cs:227` | `Core/CardIcons.cs` 的 `RewriteMarks` |

### `Editor/MenuCheck.cs` — 应用 6（§2 5 + A1220 1）

| # | 行 | 类 | 原串 | → 新串 |
|---:|---:|---|---|---|
| 1 | 15 | A1220-S | `:2454` | `Shell/MenuDraw.cs` 的 `ShadeRuleOk` |
| 2 | 9 | S | `Editor/RewardWindowFixture.cs:12-14` | `Editor/RewardWindowFixture.cs` 的 `static` |
| 3 | 15 | S | `Shell/MenuDraw.cs:2284` | `Shell/MenuDraw.cs` 的 `CheckShadeRule` |
| 4 | 23 | F | `Shell/MenuDraw.cs:2274` | `Shell/MenuDraw.cs` |
| 5 | 27 | F | `Editor/RewardWindowFixture.cs:8` | `Editor/RewardWindowFixture.cs` |
| 6 | 139 | F | `RewardsScene.cs:124` | `RewardsScene.cs` |

### `Editor/CardBaseDemo.cs` — 应用 4（§2 4 + A1220 0）

| # | 行 | 类 | 原串 | → 新串 |
|---:|---:|---|---|---|
| 1 | 694 | S | `CardView.cs:1797-1811` | `CardView.cs` 的 `SubtypeLine` |
| 2 | 700 | F | `Core/CardText.cs:452` | `Core/CardText.cs` |
| 3 | 1930 | F | `Editor/BattleScene.cs:22522` | `Editor/BattleScene.cs` |
| 4 | 2037 | F | `Core/CardText.cs:625-629` | `Core/CardText.cs` |

### `Editor/CardFaceProbe.cs` — 应用 2（§2 2 + A1220 0）

| # | 行 | 类 | 原串 | → 新串 |
|---:|---:|---|---|---|
| 1 | 732 | F | `Core/TmpFont.cs:163` | `Core/TmpFont.cs` |
| 2 | 747 | S | `Core/CardText.cs:290` | `Core/CardText.cs` 的 `sprite` |

### `Editor/T09Probe.cs` — 应用 1（§2 1 + A1220 0）

| # | 行 | 类 | 原串 | → 新串 |
|---:|---:|---|---|---|
| 1 | 96 | S | `Battle/BattleDriver.cs:14113-14134` | `Battle/BattleDriver.cs` 的 `ToCardData` |

## 四 · 跳过清单

**本波没有「表里的行照表落地却被跳过」的条目 —— 跳过 0。**

但有 **4 条预检时「裸子串计数」与表不一致**，全部按下面口径消化（不是猜改）：

| # | 宿主 | 原串 | 表 | 裸子串 | 处理 |
|---:|---|---|---:|---:|---|
| 1 | `ShellScene.cs` 5553 / 5597 | `Shell/PracticeModePopup.cs:883` | 2 | 3 | 第 3 处是 5546 行复合 token `:883/:1051`；按 token 计数 = 2 = 表 ⇒ 应用（两行都换） |
| 2 | `RewardsScene.cs` 11439 | `Shell/CampaignTab.cs:581` | 1 | 2 | 另一处是 11425 行 `:581-584`（属 #11350 的整串）；按 token 计数 = 1 ⇒ 应用 |
| 3 | `NetSelfTest.cs` 1492 | `SearchingMatchPopup.cs:438` | 1 | 2 | 另一处是 `Shell/SearchingMatchPopup.cs:438-440` 的前缀；左边界排除 ⇒ 1 ⇒ 应用 |
| 4 | `ShellScene.cs` A1220 #24 | `:1386` | 1 | — | 与 #23 `:1393` 同一宿主行；#23 先改后整行原文不再逐字匹配 ⇒ 用「表里宿主行号 + 行内仍有该 token」回退定位，成功 |

## 五 · 验证（只跑了秒级 typecheck + 静态复量）

```text
$ TMPDIR=/tmp/wf_tool3 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

`git diff --numstat`（本波只动了这 12 份；+行/-行 增量 = 每改 1 行 ±1，无整篇翻）：

```text
4/4 CardBaseDemo · 2/2 CardFaceProbe · 220/51 CollectionScene · 30/19 DeckScene · 5/5 IconSetup
5/5 MenuCheck · 9/9 NetBattleTest · 33/26 NetSelfTest · 122/47 RewardsScene · 7/7 SettingsScene
49/49 ShellScene · 1/1 T09Probe
```

## 六 · 复跑 / 核对命令

```bash
# ① census 正则复量（12 份文件合计应为 259）
cd d:/4/Unity/MyGame/Assets/CardPresentation
grep -hoE "[A-Za-z_][A-Za-z0-9_./!-]*\.(cs|c):[0-9]+" Editor/CollectionScene.cs Editor/ShellScene.cs \
  Editor/RewardsScene.cs Editor/NetSelfTest.cs Editor/DeckScene.cs Editor/NetBattleTest.cs \
  Editor/SettingsScene.cs Editor/IconSetup.cs Editor/MenuCheck.cs Editor/CardBaseDemo.cs \
  Editor/CardFaceProbe.cs Editor/T09Probe.cs | wc -l

# ② 抽查：下面几条应当全部 0 命中（都已替换）
grep -cF "Shell/DeckInfoPopup.cs:213-218" Editor/CollectionScene.cs   # 期望 0
grep -cF "RuleCore.cs:3685" Editor/NetBattleTest.cs                  # 期望 0
grep -cF "Shell/PracticeModePopup.cs:883" Editor/ShellScene.cs       # 期望 0
grep -cF "Editor/RewardWindowFixture.cs:12-14" Editor/DeckScene.cs   # 期望 0
grep -cF ":490-493" Editor/CollectionScene.cs                        # 期望 0
```

## 七 · 顺手发现（只报不改）

1. **表的新串在多处与句子已有内容重复**（照表逐字落地的副作用）：
   **14 行是同一符号紧邻重复**；放宽到「符号前缀/同串相邻」共 **39 行**。例：
   - `CollectionScene.cs:96` → `` `Shell/DeckInfoPopup.cs` 的 `HitZFront` 的 `HitZFront` 就是… ``
   - `NetSelfTest.cs:2060/2072` → `` `NetMatchmaking.cs` 的 `s.PeerName` 的 `s.PeerName` ``
   - `MenuCheck.cs:15` → `` `CheckShadeRule` 的 `CheckShadeRule(MenuCheck chk, …)` ``
   - `DeckScene.cs:1728` → `` `int ccost = ClampPoolCost(c.Cost);` 的 `int ccost = ClampPoolCost(c.Cost);` ``
   建议下一波：句子里已经有同名符号时，把新串尾部的「的 `Symbol`」删掉（本波按表逐字落地，没有自作主张）。
2. **复合 token 的分身**：`ShellScene.cs:5546` 原 `` `Shell/PracticeModePopup.cs:883/:1051` `` 整 token 被换成
   `` `Shell/PracticeModePopup.cs` 的 `deckVc.CaptureNow();` `` ⇒ `/:1051` 那半（armyVc 那一路）随之消失；
   同文件 `:5556` 那条是另一行、单独覆盖了 `:1051`。若下一波要保留两路的对应关系，需补一句。
3. **census 没覆盖到的自家引用**（在本波白名单内、但表里没有）⇒ 下一波补表：
   - `Editor/SettingsScene.cs`：**9 处** `Shell/SettingsWindow.cs:NNN`
     （L659 / L1792 / L1937 / L2237 / L2245 / L2268 / L2904 / L3042 / L4681）
   - `Editor/RewardsScene.cs`：L7663 `Shell/DailyRewardPopup.cs:242-257` · L7664 `Editor/ShellScene.cs:1633-1677`
   - `Editor/CollectionScene.cs`：L3797 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/Everguild/MatchMakerManager.cs:136`
     （d2 签名桩，**本就不该改**）
4. **同文件裸行号（A1220 未覆盖桶）残留**：
   - `Editor/CollectionScene.cs:8637` 的 `:490-501`
   - `Editor/NetSelfTest.cs` 的 `:213`（L1754）· `:247`（L1757 / L1768）
5. **表里的行号漂移已实测**（不是偶发）：表 `CollectionScene:5183` → 现 5276 · 表 `9274` → 现 9443；
   表 `RewardsScene:9657` → 现 9732 · 表 `10508` → 现 10583；表 `NetSelfTest:2474` → 现 2481。
   ⇒ 印证「原串是锚点、行号不可信」这条口径。
