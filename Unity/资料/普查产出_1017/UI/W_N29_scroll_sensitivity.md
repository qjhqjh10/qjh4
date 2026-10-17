# W_N29 · 全壳滚动灵敏度按视图落地（`m_ScrollSensitivity`）

日期：2026-10-17 · 执行写手报告 · 判据正本 = `D_N29_滚动灵敏度逐视图.md`
（本文件只记**落点 / 改了什么 / 没查清**；判据不复制第二份，见正本。）

## 一、结论

- `Shell/MenuScroll.cs` 的 **`NotchK = 0.4f` 已从全壳常量改成每实例一份**：
  · 新增 `public readonly float NotchPx`（**每格滚轮滚多少画布像素 = 原版 `m_ScrollSensitivity`**）；
  · `public readonly float NotchK` = `NotchPx / ScrollUnitsPerNotch`（新常量 120）；
  · 构造 / `LeftAligned` / `TopAligned` / `ProfilePage.NewScroll` 都加了 `notchPx` 形参；
  · **缺省值 `DefaultNotchPx = 48f`**（= 旧手感 `0.4 × 120`），注释写明「缺省 = 未现读的视图，⛔ 不是原版参数」。
- **生产调用点 34 处**已按 D_N29 的原版值传参（见下表）。**改前一律 = 48 px/格**（全壳 0.4）。
- 未改 / 不能改的 5 处，逐条见 §四（黑名单 2 · 无判据 2 · 缺滚动实现 1）。
- `bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑了两轮，末轮原文见 §五）。

## 二、逐视图表（我们的落点 → 原版值）

> 「改前」列全部是 **48 px/格**（旧 `NotchK = 0.4` × 120），故只写「48」。
> 「原版 prefab 路径」按 D_N29 ／本机重扫（`menu_rect.Bundle` 沿父链解）对过。

| 视图 | 我们的落点 `文件:行号` | 原版 prefab 节点 | 原版值 | 改前 | 改后 |
|---|---|---|---|---|---|
| 收藏窗 · 卡牌页 | `Shell/CollectionWindow.cs:951` | `Collection Menu Variant/…/CardsTab/Collection Display/Scroll View` | 100 | 48 | 100 |
| 收藏窗 · 卡背页 | `Shell/CollectionWindow.cs:1251` | `…/Cardback Tab/Cardback Display/Scroll View` | 100 | 48 | 100 |
| 收藏窗 · 异画页 | `Shell/CollectionWindow.cs:1762` | `…/Alternate Art Tab/Collection Display/Scroll View` | 100 | 48 | 100 |
| 收藏窗 · 筛选项 | `Shell/CollectionWindow.cs:2011` | `…/Card Filters/Scroll View`（Cards 与 AltArt 两份都是 50） | 50 | 48 | 50 |
| 收藏窗 · 卡组页 | `Shell/CollectionWindow.cs:3217` | `…/Select Deck Tab/Decks Tab/…/Deck Scroll View` | 100 | 48 | 100 |
| 战役页 · 阵营条 | `Shell/CampaignTab.cs:208` | `Campaign Tab/Campaign Army Selector` | 1.0 | 48 | 1 |
| 战役页 · 轨迹 | `Shell/CampaignTab.cs:369` | `Campaign Tab/Campaign Track` | 1.0 | 48 | 1 |
| 锻造页 · 奖励轨道 | `Shell/ForgeTab.cs:399` | `Forge Tab/Rewards Scroll View` | 1.0 | 48 | 1 |
| 锻造页 · 阵营条 | `Shell/ForgeTab.cs:441` | `Forge Tab/Forge Army Selector` | 1.0 | 48 | 1 |
| 奖励窗 · 内容 | `Shell/RewardWindow.cs:1107` | `Reward Window/Content/Scroll View` | 1.0 | 48 | 1 |
| 连登奖励弹窗 | `Shell/DailyStreakPopup.cs:457` | `Daily Streak Popup/Streak Successful/Rewards Scroll View` | 1.0 | 48 | 1 |
| 收件箱 · 消息列表 | `Shell/InboxWindow.cs:487` | `Inbox Menu/Content/Message List` | 1.0 | 48 | 1 |
| 练习窗 · 阵营选择 | `Shell/PracticeModePopup.cs:1134` | `Practice Mode Menu/Deck Selector/Army Selector` | 50 | 48 | 50 |
| 练习窗 · 卡组列表 | `Shell/PracticeModePopup.cs:1304` | `Practice Mode Menu/Deck Selector/Deck Buttons/Decks Scroll view` | 100 | 48 | 100 |
| 教程模式窗 · 阵营选择 | `Shell/TutorialModePopup.cs:534` | `Tutorial Mode Menu/Army Selector` | 50 | 48 | 50 |
| 选卡组弹窗 | `Shell/DeckSelectionPopup.cs:528` | `Deck Selection Popup/…/Deck Scroll View`（带页签那份也是 100） | 100 | 48 | 100 |
| `Purchase Premium Window` | `Shell/PurchasePremiumWindow.cs:524` | 同名 prefab `Scroll View` | 30 | 48 | 30 |
| 排位加分活动窗 | `Shell/RankedRewardEventWindow.cs:447` | `Ranked Boost Reward Event Window/window/Scroll View` | 100 | 48 | 100 |
| 商店 · 货架（所有页） | `Shell/ShopTabPage.cs:379` | `Card/Daily/Item/Shop Tab/Packs Scroll View` 等 | 1.0 | 48 | 1 |
| 设置 · 画质页 | `Shell/SettingsWindow.cs:4245` | `Main Menu Settings Window/…/Graphics Tab/Content/Scroll View` | 1.0 | 48 | 1 |
| 好友列表 | `Shell/FriendsTab.cs:244` | `Social Submenu Variant/…/Friends List/Friends Container` | 50 | 48 | 50 |
| 排行榜（弹窗三扇） | `Shell/LeaderboardWindow.cs:398` | `RankedSkirmish / RankedClassic / DraftLeaderboardPopup …/Content/Scroll View` | 50 | 48 | 50 |
| 排行榜 · 阵营条 | `Shell/LeaderboardWindow.cs:461` | `…/Content/Army Selector`（三扇都是 1.0） | 1.0 | 48 | 1 |
| 排行榜（嵌入版） | `Shell/LeaderboardWindow.cs:620` | `Ranked Leaderboard Display/Content/Scroll View` | 50 | 48 | 50 |
| 活动窗（遭遇 / 排位）· 阵营条 | `Shell/LiveOpsEventWindow.cs:675` | `RankedEventWindowV2` / `SkirmishModeEventWindow` 的 `Ranked Army Selector/Army Selector`（都是 1.0） | 1.0 | 48 | 1 |
| 战斗日志弹窗 | `Shell/BattleLogPopup.cs:181` | `Battle Log Popup/Content/Matches` | 1.0 | 48 | 1 |
| 联盟 · 奖杯窗 | `Shell/AllianceMemberTab.cs:473` | `…/AllianceMemberVariant/TrophiesWindow/Scroll Rect` | 50 | 48 | 50 |
| 联盟 · 成员列 | `Shell/AllianceMemberTab.cs:1196` | `…/GeneralDetails/Content/MemberList/Scroll View` | 50 | 48 | 50 |
| 联盟 · 开放联盟列表 | `Shell/AllianceSearchTab.cs:366` | `…/AllianceNotMemberVariant/List View/List Area/Open Alliances` | 1.0 | 48 | 1 |
| 成就页 | `Shell/AchievementsMenu.cs:189` | `Achievements Tab/Scroll` | 1.0 | 48 | 1 |
| 档案 · 头像页 | `Shell/AvatarTab.cs:181` | `Player Profile Window/…/Avatar Tab/Item Display Panel/Scroll Rect` | 50 | 48 | 50 |
| 档案 · 战绩页 | `Shell/BattleLogTab.cs:96` | `Player Profile Window/…/Battle Log Tab/Matches` | 50 | 48 | 50 |
| 档案 · 排位页（阵营列） | `Shell/RankedTab.cs:422` | `Player Profile Window/…/Ranking Tab/AllFactions/scroll rect` | **10** | 48 | 10 |
| 档案 · 称号页 | `Shell/TitleTab.cs:141` | `Player Profile Window/…/Title Tab/Item Display Panel/Scroll Rect` | 50 | 48 | 50 |

**漏斗一处**：`Shell/ProfilePage.cs:213` 的 `NewScroll(...)` 增加了 `notchPx` 形参（缺省
`MenuScroll.DefaultNotchPx`）；上面五个档案页各自显式传值（Achievements 页也走它）。

## 三、断言（只加在**白名单里的** 5 个 Editor 宿主）

| 宿主 | 新增断言 | 原版字面量 |
|---|---|---|
| `Editor/CollectionScene.cs` | 卡牌页 / 卡背页 / 异画页 / 卡组页 / 筛选项 各一条 | 100 · 100 · 100 · 100 · 50 |
| `Editor/RewardsScene.cs` | 锻造阵营条 / 锻造轨道 / 战役阵营条 / 战役轨迹 / 连登轨 / 收件箱列表 / 领奖窗 | 1.0 ×7 |
| `Editor/ShopScene.cs` | 商店货架灵敏度 | 1.0 |
| `Editor/SettingsScene.cs` | 画质页列 | 1.0 |
| `Editor/DeckScene.cs` | **0 条** —— 该宿主里**没有 `MenuScroll`**（卡组编辑的滚动是 `DeckRuntime` 自己那份，见 §四·7） | — |

写法：`CheckNear(sc.NotchPx, <原版字面量>, 0.001f, ...)`，**不读 `DefaultNotchPx`/不读我们自己的常量**
（⛔ 自证）；`sc == null` 时先断「前提·不静默」。

⚠️ **`Editor/ShopScene.cs` 里原有一条断言必须同步改**：它写死「滚一格 = **48px**」（`gs.Wheel(-120f)` ⇒
期望 48）。原版商店货架 = **1.0 px/格** ⇒ 改成**喂 6 格、期望 +6px**（本页只有 4.62px 可滚，
这样才越得出去、后面的 Elastic 回弹断言仍然成立）。这不是新增行为，是**同一条灵敏度断言跟着原版值改**。

## 四、没查清 / 没改（逐条）

1. 🔴 **`Shell/MainMenuRuntime.cs:2149` 没改**（黑名单：另一个写手正在改 T16）。
   原版值**已现读**：`bundle_scenes_scenes_mainmenuwarpforge` 的
   `Game UI/Main Canvas/MainMenu/MainMenu/Safe area All/GameModes` = **1.0（h=1 v=0）** ⇒
   补一行 `, 1f` 即可。**留给 T16 那位写手 / 下一批**。
2. 🔴 **`Shell/GiveFeedbackPopup.cs:502` 没改**（黑名单：别人正在建）。原版
   `Give Feedback Popup menu/Window/Scroll Rect` = **50** ⇒ 补 `, 50f`。
   （本会话期间该文件**新出现了** `MenuScroll.TopAligned(ScrollR, ContentR.H)` 调用点。）
3. ⚠️ **`Shell/ChatTab.cs:115` 保留缺省 48 px/格 + 出声**（`Debug.LogWarning` 只打一次）。
   原版聊天列表 `ChatContentView` 是 **OSA 虚拟列表**（不是 UGUI `ScrollRect`）——
   搜过：`bundle_menus_assets_all` **全部 142 个** `m_ScrollSensitivity` 的父链路径里**没有聊天气泡列表**；
   `ChatPanel` 子树里唯一命中 = `ChatPanel/Holder/Chat/Enter Text/InputField` = **1.0**，那是**输入框**。
   ⇒ 判据为空（铁律 11 例外①），⛔ 不是「原版就是这样」。
4. ⚠️ **任务页（`Shell/MissionsTab.cs`）没有 `MenuScroll`**（全文件零命中）⇒ 无可传参。
   原版侧现读：`bundle_menus_assets_all` 的 142 个 ScrollRect **没有一条**路径含
   `Missions` / `Daily Missions`（搜过父链全路径）⇒ **MissionsTab 原本可能就不滚 / 或走别的机制**，
   按「没查到」记，⛔ 没编一个值。
5. ⚠️ **`Shell/DailyRewardPopup.cs`（每日奖励弹窗）没有 `MenuScroll`** ——
   原版 `Daily Reward Popup/Tracks/Rewards Scroll View` = **1.0**，但我们这一侧**根本没建滚动区**
   （只有 `Node` + 手摆格）。这是**缺滚动实现**、不是灵敏度问题 ⇒ 另立账（不在本件范围）。
6. ⚠️ **`Choose Army Deck FTUE Window`（原版 `Armies` = 100）窗还没建** ⇒ 无处可传，记着。
7. 🔴 **`Deck/DeckRuntime.cs` 的 A1543 注释有一处过期**（`Deck/**` 不在本件白名单，**没改**）：
   它写「筛选栏 / 卡背那两处**查不到判据**（那两棵树里没有 `ScrollRect`）」——
   本机重扫**找到了**：
   · `Deck Editing Menu/Content Area/Card Display/Card Filters/Scroll View` = **50**
   · `Deck Editing Menu/Content Area/Cosmetic Display/Scroll View` = **100**
   ⇒ 建议下一个 Deck 线任务把 `FilterScrollPxPerNotch`（现 48）改成 **50**、卡背页按 **100**。
8. ℹ️ **143 vs 142**：D_N29 记 `bundle_menus_assets_all` 有 **143** 个 ScrollRect；本机用
   `menu_rect.Bundle` 重扫 = **142** 个带 `m_ScrollSensitivity` 的 MonoBehaviour，逐条值一致，
   **差的 1 个没定位**（按「没查到」记）。不影响本件任何一条取值。
9. ℹ️ **其余 Editor 宿主（`Editor/MainMenuScene.cs` · `Editor/ShellScene.cs`）不在白名单** ⇒
   练习窗 / 选卡组弹窗 / 排行榜 / 社交 / 档案 / 排位活动 / 购买高级版 / 教程 / 成就 这些视图
   **暂时没有灵敏度断言**（生产代码已传值）。等下一批把断言补到对应宿主。

## 五、typecheck 原文（末轮）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
