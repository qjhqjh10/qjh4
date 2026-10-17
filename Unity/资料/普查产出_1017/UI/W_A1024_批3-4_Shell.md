# W_A1024 批 3/4 —— Shell 的 R1/R2 站点换面（施工报告）

> 日期 2026-10-17 · 执行写手 · 施工输入 = `资料/普查产出_1017/UI/R_A1024_字体按用途普查.md` §二 / §三。

## 一、数量与口径

- 改动**文本创建点 140 个**，覆盖普查表 **139 行**。
  - 其中 `Shell/MissionsTab.cs:504` 在我们代码里**形式上是 R0**（`Txt1(...)` 的三格实参全缺省 ⇒ `wrapPx=0 / autoMinPx=0`）；
    按普查表的节点判定同样把 face 形参接上了，但**当前没有 `name` 调用点走 `Txt1`**。
- 风险分布：**自适应（R2）132 站 · 只折行（R1）7 站**。
- 全部只加 `face:`（漏斗）或 `lb.SetFace(...)`（`Label.Create` 那两处）：**框 / 字号 / 对齐 / 折行参数一处未动**。
- ⚠️ 换到 Pragati（`LatinCardBody`）后，R2 站会「行盒 1.437→0.947 em ⇒ 装得下 ⇒ 字号变大」——**这是预期**（A1927 同机制），
  未做任何框/字号补偿。后续版面断言若在这些站上变红，先按「字号变大」解释，别回头改框。
- 判定口径：
  - **R1** = `TextBox`（恒折行）但 `autoMinPx = 0`；或 `Text` 只传了 `wrapPx`。
  - **R2** = `TextBox` 传了非 0 的 `autoMinPx` 常量；或 `Text` 同时传了 `wrapPx` + `autoMinPx`。
  - 期望面严格照普查表那一行的「应换成」：`Asar → LatinUi`，`Pragati → LatinCardBody`；
    ⛔ 没有按节点名批量套（`Title` / `Window Title` 等逐行分到 Ui 或 CardBody 两档，见下表）。
- 行号：普查表有几处漂移（`ParentEventWindow` +28~31、`WhereToGetCardsPopup` +4、`MissionsTab` helper +1/+2），
  一律按**节点名现读**定位；下表「→ :N」是改动后现读行号（无箭头 = 与普查表同一行）。

## 二、逐行表（文件:行号 | 节点 | 原版字体 | R1/R2 | 改后）

| 文件:行（改前→现读） | 节点 | 原版字体 | 风险 | 改后 |
|---|---|---|---|---|
| `Shell/AllianceEventScorePanel.cs:153` | Join Alliance text | Pragati | R2 | LatinCardBody |
| `Shell/AllianceMemberOptionsPopup.cs:365` | Name | Pragati | R2 | LatinCardBody |
| `Shell/AllianceMemberOptionsPopup.cs:388` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/BaseEventPopup.cs:326` | TitleText | Asar | R2 | LatinUi |
| `Shell/BaseEventPopup.cs:332` | DescriptionText | Pragati | R2 | LatinCardBody |
| `Shell/BaseEventPopup.cs:340` | Tap To continue | Pragati | R2 | LatinCardBody |
| `Shell/BaseOfferPopup.cs:870` | Descripton | Pragati | R2 | LatinCardBody |
| `Shell/BoosterInfoPopup.cs:607` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/CampaignTab.cs:244` | Title | Asar | R2 | LatinUi |
| `Shell/CampaignTab.cs:277` | Points | Pragati | R2 | LatinCardBody |
| `Shell/CampaignTab.cs:1002` | Title | Pragati | R2 | LatinCardBody |
| `Shell/CampaignTab.cs:1096` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/CardDetailPopup.cs:737` | LoreText | Asar | R2 | LatinUi |
| `Shell/CardDetailPopup.cs:899` | Craft Explanation | Pragati | R2 | LatinCardBody |
| `Shell/CardDetailPopup.cs:933` | Upgrade Need | Pragati | R2 | LatinCardBody |
| `Shell/CardDetailPopup.cs:940` | Upgrade Cost | Pragati | R2 | LatinCardBody |
| `Shell/CardDetailPopup.cs:948` | Upgrade Explanation | Pragati | R2 | LatinCardBody |
| `Shell/CardDetailPopup.cs:1023` | No Upgrade Title | Pragati | R2 | LatinCardBody |
| `Shell/CardDetailPopup.cs:1210` | Buy Original Card | Pragati | R2 | LatinCardBody |
| `Shell/CardDetailPopup.cs:1404` | Counter | Pragati | R2 | LatinCardBody |
| `Shell/CardDetailPopup.cs:1415` | Slash | Pragati | R2 | LatinCardBody |
| `Shell/CardDetailPopup.cs:1419` | Duplicates text | Pragati | R2 | LatinCardBody |
| `Shell/CardDetailPopup.cs:1664` | nodeName | Pragati | R2 | LatinCardBody |
| `Shell/DailyRewardPopup.cs:413` | Price | Pragati | R2 | LatinCardBody |
| `Shell/DailyRewardPopup.cs:481` | Title | Pragati | R2 | LatinCardBody |
| `Shell/DailyRewardPopup.cs:488` | Sub-Title | Pragati | R2 | LatinCardBody |
| `Shell/DailyStreakPopup.cs:595` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/DeckSelectionPopup.cs:492` | Instructions 2 | Asar | R2 | LatinUi |
| `Shell/DeckSelectionPopup.cs:666` | Tab Text " + (own ? "Own" : "Pre") | Pragati | R2 | LatinCardBody |
| `Shell/DuelPopupWindow.cs:175` | MessageText | Pragati | R1 | LatinCardBody |
| `Shell/DuelPopupWindow.cs:238` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:399` | Reward Tile | Pragati | R2 | LatinCardBody |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:402` | Reward Help | Pragati | R2 | LatinCardBody |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:589` | Vicotries title | Pragati | R2 | LatinCardBody |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:605` | Total Victories | Pragati | R2 | LatinCardBody |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:717` | Timer | Pragati | R2 | LatinCardBody |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:772` | Scoring Instructions | Pragati | R2 | LatinCardBody |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:778` | Energy Instructions | Pragati | R2 | LatinCardBody |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:805` | Factions Title | Asar | R2 | LatinUi |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs:821` | Faction instructions | Pragati | R2 | LatinCardBody |
| `Shell/FTUESelectDeckContainer.cs:213` | Army Text | Asar | R2 | LatinUi |
| `Shell/FTUESelectDeckContainer.cs:225` | SelectedText | Pragati | R2 | LatinCardBody |
| `Shell/FTUESelectDeckContainer.cs:234` | Army Description | Pragati | R2 | LatinCardBody |
| `Shell/FTUESelectDeckWindow.cs:258` | EverguildTextMeshPro | Pragati | R2 | LatinCardBody |
| `Shell/FTUESelectDeckWindow.cs:266` | EverguildTextMeshPro | Pragati | R2 | LatinCardBody |
| `Shell/FTUESelectDeckWindow.cs:290` | Text (TMP) | Pragati | R2 | LatinCardBody |
| `Shell/ForgeTab.cs:461` | ArmyText | Pragati | R2 | LatinCardBody |
| `Shell/ForgeTab.cs:463` | LevelText | Pragati | R2 | LatinCardBody |
| `Shell/ForgeTab.cs:839` | LevelLabel | Pragati | R1 | LatinCardBody |
| `Shell/ForgeTab.cs:873` | PointsLabel | Pragati | R2 | LatinCardBody |
| `Shell/ForgeTab.cs:885` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/GenericOptionsPanel.cs:408` | Name | Pragati | R2 | LatinCardBody |
| `Shell/GenericOptionsPanel.cs:421` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/GenericOptionsPanel.cs:478` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/ItemDisplayWindow.cs:283` | Title | Asar | R2 | LatinUi |
| `Shell/ItemDisplayWindow.cs:285` | Description | Pragati | R2 | LatinCardBody |
| `Shell/ItemDisplayWindow.cs:287` | Type | Pragati | R2 | LatinCardBody |
| `Shell/ItemDisplayWindow.cs:316` | LoreText | Asar | R2 | LatinUi |
| `Shell/LeaderboardWindow.cs:385` | Title | Asar | R2 | LatinUi |
| `Shell/LeaderboardWindow.cs:609` | Title | Asar | R2 | LatinUi |
| `Shell/LeaderboardWindow.cs:733` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/LeaderboardWindow.cs:745` | Last Season Text | Pragati | R2 | LatinCardBody |
| `Shell/LiveOpsEventWindow.cs:533` | Deck Name | Asar | R2 | LatinUi |
| `Shell/LiveOpsEventWindow.cs:535` | Deck Warlord | Asar | R2 | LatinUi |
| `Shell/MissionRerollPopup.cs:334` | MessageText | Pragati | R2 | LatinCardBody |
| `Shell/MissionsTab.cs:420 → :421` | name | Asar | R2 | LatinUi（Txt 加 face 形参；3 处 name 调用点传） |
| `Shell/MissionsTab.cs:504 → :506` | name | Asar | R2 | LatinUi（Txt1 加 face 形参，无 name 调用点） |
| `Shell/ParentEventWindow.cs:540 → :568` | TabButtonLabel | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:553 → :583` | OneText | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:595 → :625` | Title | Asar | R2 | LatinUi |
| `Shell/ParentEventWindow.cs:607 → :638` | Description | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:616 → :647` | Text | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:645 → :676` | Team Name | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:648 → :679` | Tap to select | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:662 → :693` | Team captain | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:697 → :728` | Window Title | Asar | R2 | LatinUi |
| `Shell/ParentEventWindow.cs:736 → :767` | Your Team Text_Team A | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:743 → :774` | Your Team Text_Team B | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:758 → :789` | Timer Text | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:775 → :806` | name == "Team A" ? "Team A captain name" : " | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:813 → :844` | Score | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:827 → :858` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:837 → :868` | Reward Tile | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:840 → :871` | Reward Help | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:846 → :877` | Vicotries title | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:854 → :885` | Total Victories | Pragati | R2 | LatinCardBody |
| `Shell/ParentEventWindow.cs:870 → :901` | isA ? "Team A Name" : "Team B Name | Pragati | R2 | LatinCardBody |
| `Shell/PromptPopup.cs:121` | MessageText | Pragati | R1 | LatinCardBody（Label.Create ⇒ lb.SetFace） |
| `Shell/PromptPopup.cs:249` | Button Text | Pragati | R1 | LatinCardBody（Label.Create ⇒ lb.SetFace） |
| `Shell/PurchasePremiumWindow.cs:479` | Title | Asar | R2 | LatinUi |
| `Shell/PurchasePremiumWindow.cs:484` | SubTitle | Pragati | R2 | LatinCardBody |
| `Shell/PurchasePremiumWindow.cs:680` | Description title | Pragati | R2 | LatinCardBody |
| `Shell/PurchasePremiumWindow.cs:683` | Description Content | Pragati | R2 | LatinCardBody |
| `Shell/PurchasePremiumWindow.cs:705` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/PurchasePremiumWindow.cs:713` | text | Pragati | R2 | LatinCardBody |
| `Shell/PurchasePremiumWindow.cs:726` | Purchased Text | Pragati | R2 | LatinCardBody |
| `Shell/PurchasePremiumWindow.cs:754` | Army Name | Asar | R1 | LatinUi |
| `Shell/PurchasePremiumWindow.cs:773` | Premium Text | Asar | R2 | LatinUi |
| `Shell/RankedDivisionInfo.cs:88` | DivisionText | Asar | R2 | LatinUi |
| `Shell/RankedRewardEventWindow.cs:403` | Title | Pragati | R2 | LatinCardBody |
| `Shell/RankedRewardEventWindow.cs:405` | Description | Pragati | R2 | LatinCardBody |
| `Shell/RankedRewardEventWindow.cs:423` | Bonus points text | Pragati | R2 | LatinCardBody |
| `Shell/RankedRewardEventWindow.cs:583` | Army Text | Asar | R2 | LatinUi |
| `Shell/ReferralPopupWindow.cs:411` | Referral Title | Pragati | R2 | LatinCardBody |
| `Shell/ReferralPopupWindow.cs:416` | Label | Pragati | R2 | LatinCardBody |
| `Shell/ReferralPopupWindow.cs:422` | error | Pragati | R2 | LatinCardBody |
| `Shell/ReferralPopupWindow.cs:427` | Label | Pragati | R2 | LatinCardBody |
| `Shell/ReferralPopupWindow.cs:429` | Name | Pragati | R2 | LatinCardBody |
| `Shell/ReferralPopupWindow.cs:443` | Title | Pragati | R2 | LatinCardBody |
| `Shell/ReferralPopupWindow.cs:460` | Descripton | Pragati | R2 | LatinCardBody |
| `Shell/ReferralPopupWindow.cs:465` | counter number | Pragati | R2 | LatinCardBody |
| `Shell/ReferralPopupWindow.cs:501` | Placeholder | Pragati | R2 | LatinCardBody |
| `Shell/ReferralPopupWindow.cs:507` | Text | Pragati | R2 | LatinCardBody |
| `Shell/ReferralPopupWindow.cs:528` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/SearchingMatchPopup.cs:238` | Main Search message | Pragati | R2 | LatinCardBody |
| `Shell/ShopTabPage.cs:522` | PlaceholderName | Pragati | R2 | LatinCardBody |
| `Shell/ShopTabPage.cs:546` | Type | Pragati | R2 | LatinCardBody |
| `Shell/ShopTabPage.cs:555` | Name | Pragati | R2 | LatinCardBody |
| `Shell/ShopTabPage.cs:578` | Available Counter | Pragati | R2 | LatinCardBody |
| `Shell/SkirmishEventWindow.cs:153` | Reward Tile | Pragati | R2 | LatinCardBody |
| `Shell/SkirmishEventWindow.cs:164` | Reward Help | Pragati | R2 | LatinCardBody |
| `Shell/SkirmishEventWindow.cs:178` | Vicotries title | Pragati | R2 | LatinCardBody |
| `Shell/SkirmishEventWindow.cs:194` | Total Victories | Pragati | R2 | LatinCardBody |
| `Shell/SkirmishEventWindow.cs:362` | Timer | Pragati | R2 | LatinCardBody |
| `Shell/SkirmishEventWindow.cs:428` | Text | Pragati | R2 | LatinCardBody |
| `Shell/TrophyInfoPopup.cs:358` | Title | Asar | R2 | LatinUi |
| `Shell/TrophyInfoPopup.cs:362` | Descripton | Pragati | R2 | LatinCardBody |
| `Shell/TrophyInfoPopup.cs:389` | counter | Pragati | R2 | LatinCardBody |
| `Shell/TrophyInfoPopup.cs:425` | Next Tier | Pragati | R2 | LatinCardBody |
| `Shell/TrophyInfoPopup.cs:454` | Label | Pragati | R2 | LatinCardBody |
| `Shell/TutorialMessagePopup.cs:299` | DescriptionText | Pragati | R2 | LatinCardBody |
| `Shell/TutorialMessagePopup.cs:304` | Tap To continue | Pragati | R2 | LatinCardBody |
| `Shell/TutorialModePopup.cs:662` | Completed Text | Pragati | R2 | LatinCardBody |
| `Shell/TutorialModePopup.cs:692` | Button Text | Pragati | R2 | LatinCardBody |
| `Shell/WelcomePopup.cs:206` | DescriptionText | Pragati | R2 | LatinCardBody |
| `Shell/WelcomePopup.cs:214` | Tap To continue | Pragati | R2 | LatinCardBody |
| `Shell/WhereToGetCardsPopup.cs:311 → :315` | Text | Asar | R2 | LatinUi |
| `Shell/WhereToGetCardsPopup.cs:484 → :488` | Title | Pragati | R1 | LatinCardBody |
| `Shell/WhereToGetCardsPopup.cs:498 → :502` | Counter | Pragati | R1 | LatinCardBody |

## 三、自适应站清单（R2，132 站）

逐条见上表 `R2` 行；按文件计数：

| 文件 | R2 站数 |
|---|---|
| Shell/ParentEventWindow.cs | 20 |
| Shell/ReferralPopupWindow.cs | 11 |
| Shell/CardDetailPopup.cs | 11 |
| Shell/EnergySinglePlayerOnlyEventWindow.cs | 9 |
| Shell/PurchasePremiumWindow.cs | 8 |
| Shell/SkirmishEventWindow.cs | 6 |
| Shell/TrophyInfoPopup.cs | 5 |
| Shell/ShopTabPage.cs | 4 |
| Shell/RankedRewardEventWindow.cs | 4 |
| Shell/LeaderboardWindow.cs | 4 |
| Shell/ItemDisplayWindow.cs | 4 |
| Shell/ForgeTab.cs | 4 |
| Shell/CampaignTab.cs | 4 |
| Shell/GenericOptionsPanel.cs | 3 |
| Shell/FTUESelectDeckWindow.cs | 3 |
| Shell/FTUESelectDeckContainer.cs | 3 |
| Shell/DailyRewardPopup.cs | 3 |
| Shell/BaseEventPopup.cs | 3 |
| Shell/WelcomePopup.cs | 2 |
| Shell/TutorialModePopup.cs | 2 |
| Shell/TutorialMessagePopup.cs | 2 |
| Shell/MissionsTab.cs | 2 |
| Shell/LiveOpsEventWindow.cs | 2 |
| Shell/DeckSelectionPopup.cs | 2 |
| Shell/AllianceMemberOptionsPopup.cs | 2 |
| Shell/WhereToGetCardsPopup.cs | 1 |
| Shell/SearchingMatchPopup.cs | 1 |
| Shell/RankedDivisionInfo.cs | 1 |
| Shell/MissionRerollPopup.cs | 1 |
| Shell/DuelPopupWindow.cs | 1 |
| Shell/DailyStreakPopup.cs | 1 |
| Shell/BoosterInfoPopup.cs | 1 |
| Shell/BaseOfferPopup.cs | 1 |
| Shell/AllianceEventScorePanel.cs | 1 |

**只折行（R1，7 站）**：`DuelPopupWindow:175 MessageText` · `ForgeTab:839 LevelLabel` ·
`PromptPopup:121 MessageText` · `PromptPopup:249 Button Text` · `PurchasePremiumWindow:754 Army Name` ·
`WhereToGetCardsPopup:484 Title` · `WhereToGetCardsPopup:498 Counter`。

## 四、断言（零风险抽查）

在 `Editor/RewardsScene.cs` 里、A1024③ 批 1/2 那条 R0 抽查之后，新增
`Section("★ A1024③ 第 3/4 批：R1/R2 站点换面抽查（原版 Asar / Pragati）")`，4 条（都在本批内、都是 R2）：

1. `Daily Reward Popup/Header Header/Title` → `Pragati-Regular SDF`
2. `Daily Reward Popup/Header Header/Sub-Title` → `Pragati-Regular SDF`
3. `Daily Reward Popup/Tracks Side Bar/Premium Track/Price Display Button 2 Variant/Price` → `Pragati-Regular SDF`
4. `Daily Streak Popup/Streak Failed/Button Text` → `Pragati-Regular SDF`

每条同时核两样：**TMP 的 `font.name`**（期望 = 原版资产名，⛔ 不读 `TmpFont.FaceResource` 加载表 = 避免自证）
＋ **`Label.ActualFace`**。**改坏法**：删掉对应站点那一行的 `face:` ⇒ 该条退回 `NotoSerifCJK-Regular SDF` 而红。
⛔ 未动 `Editor/MainMenuScene.cs`。

## 五、验证

只跑了秒级类型检查（按要求不跑 `*.Run`、不起 player）：

```
$ bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

文件行尾：全部 Shell 文件本来都是 **LF / 无 BOM**；改动按二进制写回（`wb`），`
` 计数仍为 **0**；
`Editor/RewardsScene.cs` 原有 **UTF-8 BOM**（`git show HEAD` 核对过）也保持不变。

## 六、没查清 / 本批未做

1. **普查表判「查不到」的 4 行未换**：`StrangerProfilePage.cs:43/48`（自建页）· `SearchingOpponentWindow.cs:295`
   （自建 Hint Line）· `LiveOpsEventWindow.cs:600`（自建）。⛔ 不发明口径。
2. `ImportDeckPopup.cs:313` 早在本项目**批 2** 已 `lb.SetFace(TmpFont.Face.LatinCardBody)`（现读 `:320`），未重复改。
3. **TopBar / ToastNotifications**（普查表 §2·5）在本批**范围之外**：我们代码里它们都是 **R0**
   （`TopBar` 的局部 `Text(...)` 与 `ToastNotifications.Tmp(...)` 都传 `wrapPx=0 / autoMinPx=0`）。
   ⚠️ 普查表给 TopBar `Player Name` 判的是 Asar，但那一行**没有风险列**；按本任务口径（R1/R2）未动。
4. **MissionsTab 的两个「站点」是文件内转发件**（`Txt` 定义 `:416-421` / `Txt1` 定义 `:497-506`），
   直接给定义行加 face 会**批量改掉整页所有文本**（⛔ 任务书明确禁止按节点名批量套）。做法：
   - 给 `Txt` / `Txt1` 各加一个可选 `TmpFont.Face face = Cjk` 形参并透传；
   - 在 3 处 `name` 调用点传 `LatinUi`：`Weekly Challenge`（`:1227`）· `Mission Header`（`:1360`）·
     三张卡的卡头 `name`（`:1453`）。依据 = 普查 mapping 里这 3 类原版节点（`Mission Header/name` ·
     `Weekly …/header/name` · `Daily Login Container/…/header/name`）**都是 Asar**。
   - `Txt1` 当前**没有任何 `name` 调用点**，形参只是把能力接上（对应普查表 `:504` 那一行）。
5. ⚠️ **Shell 里仍有一批 R0 站没有 `face:`**（我这边按普查表口径数出 **81 行** R0 / 无 face）。
   任务书说「R0 批 1/2（64 站）已完成、别重做」，但两数对不上 ⇒ 可能是批 1/2 只覆盖了一部分，
   或还有后续 R0 批次。**本批未碰**，请调度台确认。判据：本报告同一套分类脚本的 `R0 + NOFACE` 计数
   （脚本在 `d:/tmp/wf_a1024_b34/classify2.py`，可复跑）。
6. **R3（量文字宽度：TopBar 药丸 / 卡面 `FitsBox` / 居中标题条）** 按任务书另批，本批未做。
