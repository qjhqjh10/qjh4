# R · 界面审查 B —— 商店 / 设置 / 奖励四页 / 战役 / 锻造厂 / 事件窗族 / 战斗入口四窗

> **代理 B · 只读审查**（2026-10-18）。⛔ 未改任何代码/资料，未跑 Unity、未起 player、未跑 `*.Run`。
> **判据顺序**：原版 prefab 实读（`d:/2/新解包资源/assets_full/bundle_menus_assets_all/`）> 全量反编译（`d:/2/tools/decomp_full/`）>
> 施工图（`资料/阶段二_*.md`、`日常_原版规格.md`）> 成品卡图/参考实拍（**只当验收，不当参数源**）。
> **量具**：`python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all <名> --depth N --relative`（本报告新跑的几条命令逐条写在行内）·
> `d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json`（现读 **10,854** 条）· `d:/4/Unity/MyGame/Assets/CardPresentation/Core/Loc.cs`（手写 `Table`）。
>
> ⚠️ **本报告最重要的一条上下文（下面很多行都是它的表现）**：
> `Loc.T` 在**中文/英文档只查手写的 `Table`**（`Core/Loc.cs:3231-3300`；官方 13 语言表只在「非中英」档参与）。
> ⇒ 一条 UI 词条**即使已在本机官方表里**，只要没被手写进 `Table`（或没能进 `Resources/official_names.json` 的 bulk），
> 中英档就取不到 —— 而**很多窗口当前写死的英文，正好就是这些「官方表里有、Table 里没有」的键**。
> 这不是「查不到」，是**消费侧没接**。第 53/54 行给了根因与量化。

---

## 一、差异表

> 结论用四档：**偏差**（原版有/不同，我们做错或做缺）· **缺失**（原版有，我们完全没有）· **一致** · **判不出**（本地无判据）。
> 严重度：高 = 屏上可见的错/整件缺；中 = 文案/局部几何与官方不符；低 = 已记账的尾巴或影响面小。

### 1-A 商店（`Shop Menu Variant` + 三页）

| # | 检查点 | 原版值（出处） | 我们值（出处） | 结论 | 严重度 |
|---|---|---|---|---|---|
| 1 | `Available Counter` **横向对齐** | **`Center/Midline`**（2026-10-18 现跑 `menu_dump.py bundle_menus_assets_all "Catalog Item Shop Container" --depth 4 --relative --no-sprite` 实读：`'Available: 1/5' 字号=16.0 基准=39.0 auto[16.0~31.5] 对齐=Center/Midline 折行=0`。⚠️ **`资料/阶段二_商店_原版规格.md:143` 写的 `Right` 是旧工具 hAlign 错位留下的错值** —— 同 `阶段二_战斗入口_原版规格.md` 文件头那条横幅） | `MenuDraw.AlignRight(al, ar)`（`Shell/ShopTabPage.cs:618`） | **偏差**（框 58.48..277.12 的中心 = 格中心 167.8 ⇒ 居中才是原版；右对齐把整串往右推 ~55px） | 中 |
| 2 | `Available:` 文案 | 原版那颗 TMP **没有 `Localize`**（`menu_dump` 组件列 = `TextMeshProUGUI,EverguildButtonMaterialModifier,LayoutElement`，无 Localize；全包 `mTerm` 里 `MenuShop/Available` **0 命中**）⇒ **原版是否走词条 = 判不出**；官方表另有 `MenuShop/Available` = `Available`/`已拥有的` | 写死英文 `"Available: " + …`（`Shell/ShopTabPage.cs:605`） | **偏差**（中文档印英文；原版口径判不出，见 §三） | 中 |
| 3 | 拥有数 `x{Owned}` | `Text (TMP)` `Center/Middle` · fs23 基准32 auto[18~72] 折行=1（同一份 dump） | 不调 `Align*` + `SetVAlign(Midline)`（`Shell/ShopTabPage.cs:630-641`） | 一致（N59 已修，现读在位） | — |
| 4 | 价签 = 金币图标 + 数字 | `Price Display` 86.36,416.86→247.86,442.54；子件 `icon` = `40k_topmarquee_currency_gold`（N59 判据） | 同矩形同图（`Shell/ShopTabPage.cs:646-655`） | 一致 | — |
| 5 | 栅格 / 滚动 / 软边 | cell **335.6×475** · padT **7** · 2 列 · `RectMask2D(softness 0,25)` · `TimeCounter` 367.47..678.87（施工图 §二） | 同（`Shell/ShopTabPage.cs:86,100-113,399-446`） | 一致 | — |
| 6 | `TimedOffer` / `New` 角标 · `price-bg` · `WebShop Button Square` | 出厂 INACT / `m_Enabled=0`；两个角标按锚点算出落在格**外面**（施工图 §三、§六 #5/#6） | **不建**（`Shell/ShopWindow.cs:56-59`） | 一致（纪律①） | — |
| 7 | 页签表 / 图标 / 商品数据 | 服务端 store 驱动；本地 16 个 LiveOps SO 里只有 1 个与三页无关的真 store（施工图 §九） | 我们挑的、逐条标注（`Shell/ShopData.cs:75-107`） | 判不出（已标注） | — |
| 8 | 窗参 `useDefaultCloseSoundIfNull` | 代码注释称 prefab 实读 = 0（`Shell/ShopWindow.cs:118-120`） | `false` | 判不出（本轮未复核该字段，见 §三） | 低 |

### 1-B 设置（`Main Menu Settings Window`）

| # | 检查点 | 原版值（出处） | 我们值（出处） | 结论 | 严重度 |
|---|---|---|---|---|---|
| 9 | 页签栏 | 原版 **5 键** `General / Media / Account / Graphics / Support`（`Main Menu Settings Window > Menu Area > Mask Tabs buttons > Tab Buttons`，`SettingsWindow.cs:40-58` 实读） | **5 格逐位**照原版 + 内容页 6（`Online` 自加、入口在 General 页钮上）（`Shell/SettingsWindow.cs:1737-1741,1855-1861`） | 一致（Online 是用户裁定自加、已标注） | — |
| 10 | `Mask Tabs buttons`（键列的父级遮罩） | [338.2545,132.5485]–[1592.6241,956.3875]（`Image(40k_popup,Sliced,ppuMul .76)` + `Mask(showMaskGraphic=0)`） | `ViewportClip.Hang` 已接（`Shell/SettingsWindow.cs:28-33,190`） | 一致（A1204） | — |
| 11 | 键几何（VLG） | `padTop 13` · `spacing 8.92` · `MiddleCenter` · 每键 **165×157.6835** · 首键顶 139.10（`SettingsWindow.cs:40-58` 实读 MB） | 同一套常量（`Shell/SettingsWindow.cs:204-266`） | 一致 | — |
| 12 | 页签/页标题文案 | prefab 的 `Tab Toggle Title` 是西语占位（`Gráficos/Soporte/Cuenta`，无 I2）；官方 `Settings/*/Title` 现读有值 | 走 `Loc.T(lk*)`（`Shell/SettingsWindow.cs:2000-2142`） | 一致 | — |
| 13 | `Debug Buttons` | `SettingsMenu.Awake` 里 `SetActive(false)`（文件头 §83） | **不建** | 一致（纪律①） | — |

> 设置窗本轮**没有发现新的版面/缺件偏差**。它的每一条常量都能在 `SettingsWindow.cs` 的 doc 注释里追溯到 prefab 字段。
> ⚠️ 但「设置窗/卡组编辑/主菜单右侧/社交/收件箱还没逐屏与参考图对底纹」是 `T29` 已开的账（另有代理在做），本报告不重复。

### 1-C 奖励四页（布道所 / 活动 / 锻造厂 / 每日连胜）

| # | 检查点 | 原版值（出处） | 我们值（出处） | 结论 | 严重度 |
|---|---|---|---|---|---|
| 14 | 左栏四键位置 | y = 190.94 / 370.94 / 550.94 / 730.94（VLG padTop 120 · 每键 180，`日常_原版规格.md:143-147`） | 同（`Shell/MainMenuSubmenuWindow.cs` 的 `BuildBar` + `RewardsWindow.Buttons`） | 一致 | — |
| 15 | 第 4 键 | 原版第 4 格 = `Menu Navigation Panel Button`（**母版**，运行期 `TabButtons.Initialize` 一进来就关掉；`日常_原版规格.md:141,163-167`） | 第 4 格 = **自建「每日连胜」页**，母版顺位第 5（`Shell/RewardsWindow.cs:114-141`） | 偏差（**用户 2026-10-15 拍板、已标注**） | — |
| 16 | 骷髅卡刻度 / 周常 6 格 | 实拍：骷髅卡 `3 10 25 50 100`、周常 6 个宝箱 `5 10 15 20 25 30`（`日常_原版规格.md:302-307,349-358`） | 已按 A2070③ 落地 | 一致（按实拍；刻度本身是数据驱动） | — |
| 17 | 布道所三张卡的 **info tooltip 正文** | 原版每颗 `40K_generic_bt_info` 挂 `EverguildTooltipTrigger`；对应官方词条 **`Missions/Info/DailyMissionsHelp` / `DailySkulls` / `DailyLogin` / `WeeklyChallenge` 现读全部有值**（`d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json`） | `Tooltip.Show(" ", …)` —— **空正文**（`Shell/MissionsTab.cs:1472-1481`） | **缺失**（面板照弹、里面一个字都没有） | 中 |
| 18 | 卡头 `Refill Counter`（「N 可用」） | 官方 `Missions/RefillCount` = `{0} Available`/`{0} 可用`；实拍「3 可用」 | `Loc.T("Missions/RefillCount")`（`Shell/DailyData.cs:304-311`） | 一致（第五十一会话已修） | — |
| 19 | `Premium Panel` 三态 | `ChangeState`：state 2 = 只留 `Title` + `Timer`，`pointsObject`/`collectButton` 关（反编译 `CampaignPremiumPointsPanel__ChangeState.c:42-51`） | `ApplyPremiumState(LocalPremiumState())`（`Shell/CampaignTab.cs:1178+`，state 2 为本机口径） | 一致（T20 已修） | — |
| 20 | Premium Panel `Title` | prefab 该 TMP **挂 `Localize`**，`mTerm = Missions/PremiumDailyCampaignPoints`（`bundle_menus_assets_all/MonoBehaviour/*.json` 里 **2 处**命中）＝ `Premium Campaign daily bonus` / **`高级战役每日奖励`**（实拍同字） | 写死英文 `"Premium Campaign daily bonus"`（`Shell/CampaignTab.cs:1002`） | **偏差**（中文档印英文） | 中 |
| 21 | Premium Panel `Timer` 前缀 | `MainMenu/General/NextWithTwoPoints`（prefab 字段实读 `useExtraText=1 / extraText=` 该键）＝ `Next:` / **`下一个：`**；⚠️ **官方表现读有这条**（代码注释 `CampaignTab.cs:1138-1139` 称「官方表 0 命中」**已过期**） | `"Siguiente: "`（西班牙语占位，`Shell/CampaignTab.cs:1122,1140`） | **偏差** | 中 |
| 22 | `Campaign Header / Info Button` tooltip | 官方 `MainMenu/Campaign/Help` **现读有值**（`Complete Missions to progress through the Campaign…`） | 空正文（`Shell/CampaignTab.cs:1317` `Tooltip.Show(" ", …)`） | **缺失** | 中 |
| 23 | 战役地图（节点 / 连线 / 6 状态色 / 视口裁切） | 施工图 §十三（6 色逐值）+ §九#4 | 逐值复刻（`Shell/CampaignTab.cs:667-694` 等） | 一致 | — |
| 24 | 锻造厂三张奖励卡 | 原版走 `ItemDrawer.Draw(rewardTransform, …)`（施工图 §五）；实拍有品阶底色/阵营名/数量/底部小徽 | 已走 `ItemDrawer`（T21，`Shell/ForgeTab.cs`） | 一致 | — |
| 25 | 锻造厂 `Selected Army Info / ArmyText` | 原版 = 阵营**本地化名**（实拍「太空野狼」）；官方 `Armies/SpaceWolves` = `Space Wolves`/`太空野狼`，**且已在手写表 `Core/Loc.cs:2690`** | 印**阵营 id 原文**：`_armyText.SetText(a)`（`Shell/ForgeTab.cs:743`） | **偏差**（`SpaceWolves` vs `太空野狼`） | 中 |
| 26 | 锻造厂 `LevelText` | 原版 `string.Format("{0} {1}/{2}", 本地化("MainMenu/Level"), …)`（`ForgeWindowTab__RefreshLevel.c:40-50`；`MainMenu/Level` = `Level`/`等级`，**`Core/Loc.cs:2747` 已有**） | 写死 `"Level " + n + "/50"`（`Shell/ForgeData.cs:182-184`） | **偏差**（中文档印英文；实拍是「等级 2/50」） | 中 |
| 27 | 锻造厂格子 `Generic UI Button / Button Text`（Claim） | 官方 `MainMenu/General/Claim` = `Claim`/`索取`（**`Core/Loc.cs:2746` 已有**；同族 `SkirmishEventWindow.cs:283-286` 已改走词条） | 写死 `"Claim"`（`Shell/ForgeTab.cs:885`） | **偏差** | 低 |
| 28 | 锻造厂 `Help Icon` tooltip | `EverguildTooltipTrigger.text = "MainMenu/Forge/Help"`（prefab 实读）；**该键整段正文已在本工程 `Core/Loc.cs:2745`**（「在你的收藏中升级张牌…」/英文全句） | 正文 `" "`（`Shell/ForgeTab.cs:564`）；注释称「词条表在远端 CCD、本地一个 value 都没有」**已过期**；自检还钉着 `ShownBody.Trim() == ""` | **缺失**（判据就在同一个工程里） | 中 |
| 29 | 轨道 / 圆牌两态 / `Ready for level up` 光效 | 施工图 §五 | 一致（`Shell/ForgeTab.cs`，纪律④/⑤） | 一致 | — |
| 30 | 「每日连胜」页（第 4 页） | 原版没有这一页（是另一扇 live-ops 全屏窗） | 自建并逐条标注（`Shell/DailyStreakTab.cs`） | 一致（自建） | — |

### 1-D 每日奖励 / 每日连登（事件弹窗）

| # | 检查点 | 原版值（出处） | 我们值（出处） | 结论 | 严重度 |
|---|---|---|---|---|---|
| 31 | 每日奖励 `Day Title` | prefab 占位 `Day 3`；官方 `Missions/DayCounter` = `Day {0}` / **`第{0}天`** | `"Day " + (day + 1)`（`Shell/DailyRewardPopup.cs:288`） | **偏差** | 低 |
| 32 | 双轨标题（Free / Premium Track） | 官方表有 `Missions/FreeTrack` / `Missions/PremiumTrack` ＝ `Free Track`/`免费轨道` · `Premium Track`/`高级轨道`；包内 `mTerm` 这两个键各命中 **1 处**（⚠️ 未逐颗解父链证明就挂在 `Free Track`/`Premium Track` 两颗 TMP 上）。prefab 里的 `Ruta Gratuita` / `Ruta Premium` 是**葡语占位**（`日常_原版规格.md:457-460`） | 直接印葡语占位（`Shell/DailyData.cs:789-792`） | **偏差**（中英档都印西/葡语） | 中 |
| 33 | `Gacha Reward Claimed / Claimed Tex` | 官方 `Rewards Menu/Claimed` = `Claimed`/`已认领`（**`Core/Loc.cs` 已有**） | 写死 `"Claimed"`（`Shell/DailyData.cs:787`） | **偏差** | 低 |
| 34 | 连登窗五条正文 | prefab **挂 `Localize`** 实读：`Rewards Menu/DailyStreak/CurrentStreak`（1 处）/`Help`（2）/`Broken`（1）/`ResetStreak`（1）；官方两列 = `Current streak:`/当前连续登录： · `STREAK BROKEN`/连胜终结 · `Reset Streak`/重置连胜；`Help` 全句 = 「Log in daily and keep your streak going to unlock bigger rewards. Skip a day, and your streak resets.」 | 全部写死英文（`Shell/DailyData.cs:1003,1026,1027,1028,1034-1035`）；其中 `StreakInfoText` 还**比原版短一截**（少了 `unlock bigger rewards / skip a day resets` 两句） | **偏差**（文案内容 + 未本地化） | 中 |
| 35 | 连登格 `Collect Text` | 官方 `ShopOffers/GeneralClaim` = `Claim`/`索取`（**现读存在**；`ShopOffers/*` 共 130+ 条） | `Loc.BulkOr("ShopOffers/GeneralClaim","Claim")`（`Shell/DailyData.cs:1029-1033`）—— 注释称「本地 13 语言表与 en+zh 表里都不存在」**已过期**；且 bulk 不含 `ShopOffers/*` ⇒ 恒回退 `Claim` | **偏差**（中文档印 `Claim`） | 低 |
| 36 | 两态面板 / 奖格 / 首格 1.2 / Collect 只在可领时出现 | 施工图 §五（`Streak Failed` 出厂亮；`scaleMultiplierFirstElement 1.2`；`Collect` 出厂 true） | 一致（`Shell/DailyStreakPopup.cs:684-783`；`Collect` 只在 `unlocked && !claimed` 时建） | 一致 | — |

### 1-E 事件窗族

| # | 检查点 | 原版值（出处） | 我们值（出处） | 结论 | 严重度 |
|---|---|---|---|---|---|
| 37 | `Parent Event Window` 外壳 + 两页 | `TabbedWindowComponents` + 2 个 tab PPtr（`Select Team Tab` / `Event On Going Tab`）；窗参 `type=0 / placement=15 / closeOnESC=0`（`Shell/ParentEventWindow.cs:6-24` 实读） | 已建、逐值（T28） | 一致 | — |
| 38 | 活动承载窗的**文案** | 官方表现读：`Event_Description/BattleForWarpforge/{Title,TeamAName,TeamACaptainName,TeamBName,TeamBCaptainName}` + `TwoSidesEvent/{WelcomeScreen,YourTeam}` **共 7 条**（例：`TeamAName` = `Team Squidmar`/`战队 Squidmar`；`Title` = `Battle for Warpforge`/`为Warpforge战斗`；`WelcomeScreen` 是带 `<color=#E27E1B>` 的**整段**） | prefab 出厂占位：`BATTLE FOR WARPFORGE` / `Red Team` / `Blue Team` / `#squidmar` / 截短版 `Choose your side…`（`Shell/ParentEventWindow.cs:247-252,329-330`）；文件头 `:46` 写「官方表 9,441 条里 0 命中」**已过期**（现表 10,854 条，命中） | **偏差** | 中 |
| 39 | 卡牌来源窗 `WhereToGetCardsPopup` | 原版两个事件 tab 持有 `getCardsWindow` AssetReference | 已建；五条已知偏离同 `N51`；**无生产入口**（承载 tab 未建） | 已记账 | 低 |
| 40 | 物品展示窗 `Item Display Window` | 窗参 / 树 / 三格字 / 风味条（`Shell/ItemDisplayWindow.cs:1-55` 全实读） | 逐值一致 | 一致 | — |
| 41 | 物品展示窗 `Type` 一行 | 值 = `ItemDrawerOptions.TypeString`（在 `ItemDrawerConfig` 资产里；该资产**不在解包树**，84 包逐字节 0 命中） | 留空 + 出声（`Shell/ItemDisplayWindow.cs:231-235`） | 缺失（判据空，合规） | 低 |
| 42 | 物品展示窗 `Title`（卡背名） | 官方 `Cardbacks/*` 现读 **181** 条 | `Loc.BulkOr("Cardbacks/" + so, …)`（`Shell/ItemDisplayWindow.cs:180`），而 `Resources/official_names.json` 只有 **157** 条 `Cardbacks/*` ⇒ **24 条会回退「反推名」**（见第 53 行） | **偏差**（bulk 过期） | 中 |

### 1-F 战斗入口四窗

| # | 检查点 | 原版值（出处） | 我们值（出处） | 结论 | 严重度 |
|---|---|---|---|---|---|
| 43 | 练习窗（`Practice Mode Menu`）结构 | 施工图 §二 A（两抽屉互斥、`Generic Window Red Background Big` 761.63,187→1831.93,869、卡列表 grid 231×27.88 pad(22,0,4,0)…） | 逐项一致（`Shell/PracticeModePopup.cs` 文件头 + 常量段） | 一致 | — |
| 44 | 练习窗 `Lore Text`（卡组简介） | 原版喂 `DemoDeckInfoSO.Lore`（= `loreLocalizationKey`）；**官方表现读 `Demo/*Deck*` 共 92 条**（含 `Demo/DeckSelectionDemo/*` 3 条，其余是 `Demo/<阵营>Deck<n>` 这类卡组简介全句；例 `Demo/SpaceWolvesDeck1`…`Deck5`，中英齐全），82 份 `Demo DeckInfo *.json` 本地也在 | **不建**（`Shell/PracticeModePopup.cs:64` 的理由是「正文在远端 CCD ⇒ 判据（文本）本地就是空的」）—— **这条阻塞已解除**（只差把 82 个 SO 的 key 解出来接一次） | **缺失（判据已到位）** | 中 |
| 45 | 排位窗左列文案 | prefab 挂 `Localize`：`MainMenu/RankedWindow/Leaderboard`（**16 处**）/`Unranked`（2 处）/`Ranked`（2 处）；官方两列 `Leaderboard`/排行榜 · `Unranked`/未排名 · `Ranked`/排名（`Rank` 标题本地没有独立键，判不出） | 写死 `"Rank"` / `"Leaderboard"` / `"Unranked"` / `"Ranked"`（`Shell/RankedEventWindow.cs:102,116,136,140`） | **偏差** | 低-中 |
| 46 | 排位窗背景 / 段位块 / Leaderboard 入口 / ChangeRankedToggle / 全屏搜索 | 施工图 §二 C + §六 | 一致（`Shell/RankedEventWindow.cs` + `Shell/RankedDivisionInfo.cs`） | 一致 | — |
| 47 | 遭遇战 `Reward Display` 标题与说明 | prefab 挂 `Localize`：`Ranked/Skirmish/ProgressionTitle`（**3 处**）/`Ranked/Skirmish/Progression`；官方 = `Progression`/进步 · `Win battles to progress in the event and unlock rewards.`/赢得战斗以在活动中取得进步并解锁奖励。 | 写死英文（`Shell/SkirmishEventWindow.cs:153,166`） | **偏差** | 低 |
| 48 | 遭遇战 `Timer` 前缀 | 该 TMP **挂 `Localize`**，`mTerm = Event_Description/General/EndsIn`＝`Ends in:`/结束于：（该键**已在 `Resources/official_names.json` 的 bulk 里**，`Loc.T` 直接可解） | `"Termina en: "`（西语占位，`Shell/SkirmishEventWindow.cs:360`） | **偏差** | 中 |
| 49 | 遭遇战 `Score` / `Collect` / `Banned card in deck` | 施工图 §二 B + A1221 | 已建（`Collect` 已走 `Loc.T`；`Banned` 建了但主动关 —— 本地无禁用卡表） | 一致 | — |
| 50 | 两扇活动窗 `Factions Title` | prefab 挂 `Localize`，`mTerm = MainMenu/Factions`（**4 处**）＝`Factions`/派系 | 写死 `"Factions"`（`Shell/LiveOpsEventWindow.cs:640`） | **偏差** | 低 |
| 51 | 搜索对手弹窗文案 | prefab 挂 `Localize`：`Demo/DeckSelectionDemo/Searching`（**5 处**；官方 `Searching...`/搜索中...）· `MainMenu/General/Cancel`（1 处；`Cancel`/取消） | 写死 `"Searching"`（`Shell/SearchingMatchPopup.cs:93`）· `"Cancel"`（`:248`） | **偏差** | 低 |
| 52 | 四窗入口 | 本地**无入口**（liveop 服务端驱动；施工图 §〇 已查到底） | 模式卡即入口（用户 2026-09-24 拍板、已标注） | 一致（已标注） | — |

### 1-G 跨窗（根因 / 全局）

| # | 检查点 | 原版值（出处） | 我们值（出处） | 结论 | 严重度 |
|---|---|---|---|---|---|
| 53 | `Resources/official_names.json`（`Loc.BulkOr` 的唯一数据面来源） | 现跑的官方表应产出 **3,156** 条（`python -I -X utf8 d:/4/Unity/工具/_loc_official/gen_official_names.py` **干跑**输出：Titles 459 · Avatars 498 · Achievements 212 · Cardbacks 181 · PrebuiltDecks 215 · Event_Description 144 · Modular_Description 342 · Card_Name 1198） | 盘上 **2,758** 条（2026-10-15 生成，早于 `K07` 2026-10-18 的并表）⇒ **少 ~398 条**；逐族缺口：`Card_Name` **133** · `PrebuiltDecks` **141** · `Titles` **91** · `Avatars` **79** · `Card_Trait` **57** · `Event_Description` **33** · `Cardbacks` **24**（判据 = 两文件逐键差集） | **偏差**（重跑 `gen_official_names.py --write` 即补齐；不跑则 `Loc.BulkOr` 的兜底族持续缺键） | 高 |
| 54 | `Loc.T` 的中英档词条面 | 官方表 **10,854** 条 | 中英档**只查手写 `Table`**（`Core/Loc.cs:3238-3240`），随后只兜 bulk；本报告 §一 里 `Missions/Info/*` · `MainMenu/Campaign/Help` · `Rewards Menu/DailyStreak/*` · `Missions/FreeTrack/PremiumTrack` · `Missions/DayCounter` · `MainMenu/General/NextWithTwoPoints` · `MenuShop/Available` · `Demo/DeckSelectionDemo/Searching` · `MainMenu/Factions` · `Ranked/Skirmish/*` · `MainMenu/RankedWindow/{Ranked,Unranked}` · `Missions/PremiumDailyCampaignPoints` · `ShopOffers/GeneralClaim` · `Event_Description/TwoSidesEvent/*` 全部**不在 `Table`** | **偏差（机制）**：补法 = 把要用的键逐条加进 `Table`（键名照 prefab `Localize.mTerm`），或给 `Loc.T` 开一条「中英也查 13 语言表」的路（后者要先定口径，见 §三） | 高 |

**已经一致、不必再报的（点名列出，免得下个会话重查）**：商店栅格/滚动/软边/计时/价签/拥有数 · 设置窗全树与 5+1 页 · 奖励左栏几何与页签机制 · 战役节点/连线/6 状态色/三态 Premium Panel · 锻造圆牌两态/光效/轨道 · 连登两态/奖格缩放 · 物品展示窗全树 · 练习窗两个抽屉 + 费用曲线 + `Battle!` 链 · 排位/遭遇战背景与左列结构 · 搜索对手窗（含转盘自转 37 键曲线）· 四窗 `Battle!` → 12 秒 → bot → 切战场。

---

## 二、功能入口表（控件 → 原版行为 → 我们的行为）

> 「入口在 X，动作是 Y」按任务书要求写清；**光打日志不弹任何可见提示**的，标「仅日志」并给严重度。

| 界面 | 控件 | 原版行为（出处） | 我们行为（file:line） | 结论 |
|---|---|---|---|---|
| 商店 | 主菜单 SHOP 钮 | `OpenWindowButton → WindowsManager.OpenWindow(closeAll:true)`（施工图 §一·一） | 开商店窗（`MainMenuRuntime` + `ShopWindow.Create`） | 一致 |
| 商店 | 三个页签 | 服务端 store 列表 → `CreateStoreTab` | 切 `ShopTabPage`（`ShopWindow.cs:126-132`） | 一致（表是我们挑的） |
| 商店 | 价签 `Generic UI Button` | `CatalogItemContainer.TryPurchase`（传奇先弹 `MenuShop/ExtraLegendaryWarning`） | `Buy(idx)`：传奇先确认；买 = 记账 + `Debug.Log`（边界②不扣钱）（`ShopTabPage.cs:700+`） | 一致（不扣钱是用户边界） |
| 商店 | 卡包格主图 | 原版 `ShopOfferContainer.OnClick → OpenContainer`（那一族我们没有） | 开 `BoosterInfoPopup`（**入口我们定的**，`BoosterInfoPopup.cs` 文件头） | 已标注 |
| 商店 | `WebShop Button Square` / 角标 | 出厂 INACT / `m_Enabled=0` | 不建 | 一致 |
| 设置 | 顶栏齿轮 | `Settings Menu` 的 `OpenWindowButton`（`MainMenuRuntime.cs:359` 原为死链，2026-09-26 已接） | 开设置窗 | 一致 |
| 设置 | 5 个页签 + General 页的 Online 钮 | 原版 5 页；Online 是我们自加 | `OpenTab`（`SettingsWindow.cs`） | 一致（Online 已标注） |
| 设置 | 语言 / 画质 / WindowMode / TextInHand 下拉 | 原版 `TMP_Dropdown` + `AvailableLanguages` 12 项 | 真下拉（T17） | 一致 |
| 设置 | Support 页 5 个外链钮 | `SupportTab__OnClick*` → FAQ/Contact/Privacy/ToS/Helpshift | 已建（A1183） | 一致 |
| 设置 | Account 页 7 个动作 | 全走 PlayFab | 本地模拟（PlayerPrefs）+ 明说（`SettingsWindow.cs:107-112`） | 一致（已标注） |
| 奖励 | 左栏 Missions/Campaign/Forge/DailyStreak | 前三条是 `tabs`；第 4 格是母版 | 四页（第 4 页自建） | 偏差（已拍板） |
| 奖励 | 任务行「Collect」 | `Missions.CollectChallenge` → PlayFab 云脚本 910 | `CollectThenRebuild(…, DailyData.CollectDaily)`（`MissionsTab.cs:744-747`） | 一致（本地结算） |
| 奖励 | 任务行垃圾桶（重摇） | `MissionReRollButton` | 已接重摇（`MissionsTab.cs` 垃圾桶段 + 重摇窗） | 一致 |
| 奖励 | 骷髅/登录/周常 Collect | 同上（PlayFab） | 本地结算（`MissionsTab.cs:940,1164,1319`） | 一致 |
| 奖励 | 各卡 info 圆钮 | `EverguildTooltipTrigger` → I2 正文 | 面板照弹、**正文空**（`MissionsTab.cs:1472-1481`） | **缺失**（见 1-C #17） |
| 奖励 | 每日奖励 / 连登 / 收件箱 | live-ops 自弹 / 顶栏 InboxBtn | 三窗都已建、按本地数据弹 | 一致 |
| 战役 | 阵营格 | `ArmySelector` → `SelectArmy` | 重建轨道 + 头部（`CampaignTab.cs:725-760`） | 一致 |
| 战役 | 地图节点 | `OnNodeClicked` → 开 `CampaignRewardsWindow` | 开 `CampaignRewardWindow`（`CampaignTab.cs:528,894`） | 一致 |
| 战役 | Premium Panel `Continue` | 领高级每日奖励（PlayFab） | **仅 `Debug.Log`**（`CampaignTab.cs:1114-1116`）—— state 2 下这钮本来就关着，玩家看不到；一旦进 state 0/1 就是「点了没反应」 | 偏差（低） |
| 锻造 | 阵营格 / 奖励格 Claim | `ArmySelector` / `ForgeRewardItemContainer` | 已接（`ForgeTab.cs:744,966-972`） | 一致 |
| 锻造 | `Help Icon` | `EverguildTooltipTrigger` → `MainMenu/Forge/Help` | 面板照弹、正文空（`ForgeTab.cs:564,600`） | **缺失**（见 1-C #28） |
| 战斗入口 | 三张模式卡 | 原版无本地入口（liveop） | Practice/Skirmish/Ranked 各开一窗 | 一致（已标注） |
| 战斗入口 | `Battle!` | `MatchMakerManager.StartMatch` → 12s → bot（`DAT_1834b3160=12.0`） | 开 `SearchingMatchPopup`（排位开全屏 `SearchingOpponentWindow`）→ 12s → `StartBotBattle` | 一致 |
| 战斗入口 | 搜索弹窗 `Cancel` | `MatchMakerManager.CancelSearch` | `Cancel()`（`SearchingMatchPopup.cs:258`） | 一致 |
| 战斗入口 | 排位 `ChangeRankedToggle` | 切排位/非排位 | 弹「本地没有排位赛」说明（`RankedEventWindow.cs:193-208`） | 一致（如实说明） |
| 战斗入口 | 排位 `LeaderboardButton` | 按 `playMode` 二选一榜 | 开 `LeaderboardWindow`（`RankedEventWindow.cs:179-191`） | 一致 |
| 事件窗 | `Parent Event Window` | `ParentEventHandler` / liveops 链 | **只建 + 注册，无生产入口**（`ParentEventWindow.cs:87` 已出声） | 缺失（无判据，合规） |
| 事件窗 | `WhereToGetCardsPopup` | 两个事件 tab 的 `getCardsWindow` | **只建 + 注册，无入口**（N51⑤） | 已记账 |
| 事件窗 | `Item Display Window` | 卡背页点卡背（`CardbackCollectionTab__OnItemSelected`） | 收藏窗卡背点击可开 | 一致（T25） |

---

## 三、没查清（⛔ 不许猜）

1. **商店 `Available:` 前缀原版到底走不走词条** —— 原版那颗 TMP **没有 `Localize` 组件**（`menu_dump` 组件列无 Localize；全包 `mTerm` 搜 `MenuShop/Available` 0 命中）。可能是代码里 `string.Format`，但**没找到那段反编译**。
   搜过：`bundle_menus_assets_all/MonoBehaviour/*.json` 的 `mTerm`（1,214 个带 mTerm 的文件）· `d:/2/tools/decomp_full/` 的 `CatalogItemContainer__*`（未见拼串）。
   ⚠️ 但**对齐那条（#1）是硬判据**，与词条无关，可以直接改。
2. **`Rank Title` 的英文源串** —— 原版 prefab 那颗是 `'Rank'`，其 `Localize.mTerm` 我**没定位到**（`mTerm` 全集里没有长得像 `RankedWindow/Rank` 的键）。⇒ 只报 `Leaderboard/Unranked/Ranked` 三条有 Localize 实据的。
3. **商店窗 `useDefaultCloseSoundIfNull`** —— 代码注释称实读 0，本轮**没有回 prefab MB 复核**（不在最高价值项里）。
4. **`Loc.T` 中英档要不要改成「官方表优先」** —— 这是口径问题（`Core/Loc.cs:3238` 明确写了「手写 Table 是权威：33 条刻意保留的旧译 + 我们自拟的键都在里面」）⇒ **提案而不是缺陷**：要么逐键补 `Table`（保守、可审计），要么开一条「Table 缺 → 查 13 语言表」的兜底（会改变 33 条刻意旧译的行为，**须先拍板**）。本报告只报事实，不替它选。
5. **`ParentEventWindow` 的 7 条官方词条是否就是那两个 tab 的运行时文案** —— 官方表命中 `BattleForWarpforge/*` + `TwoSidesEvent/*`，但**哪条挂哪个节点**要靠 liveops 事件数据（本地 0 命中）⇒ 只证「文案不再无判据」，不证「逐节点对应」。
6. **`Demo/*Deck*` 92 条 ↔ 82 份 `Demo DeckInfo *.json` 的逐份映射** —— 本轮只证「词条在、SO 在」，**没有逐份读 `loreLocalizationKey` 建表**（那是接 `Lore Text` 的下一步活，约 15 分钟脚本）。
7. **搜索词/包范围（按项目规矩打出来）**：
   - prefab 侧：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/{GameObject,RectTransform,MonoBehaviour}/`（`mTerm` 全集 413 个去重值；`Missions/RefillCount`、`MainMenu/Level`、`Missions/DayCounter`、`MenuShop/Available`、`Rewards Menu/DailyStreak/StreakLost` 在这份包里 **0 命中** —— 不等于别的包没有）。
   - 本地化侧：`d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json`（10,854）· `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/official_names.json`（2,758）。
   - 代码侧：`d:/4/Unity/MyGame/Assets/CardPresentation/Shell/*.cs` + `Core/Loc.cs`。

---

## 四、给下一轮的最小修复清单（按性价比排序，不是按大小分档）

1. **补 `Loc.Table` 的 16 条 UI 键**（本报告 §一 各行的键名，逐个照 prefab `Localize.mTerm` 原文）—— 一次改完，§一 里 20+ 行一起收口。
2. **重跑 `工具/_loc_official/gen_official_names.py --write`**（3,156 vs 2,758，补 398 条数据面键）。
3. **`ShopTabPage.cs:618` 的 `AlignRight` → 居中**（并同步订正 `资料/阶段二_商店_原版规格.md:143` 的 `Right`）。
4. **四个 tooltip 的正文换成 `Loc.T` 已有键**（`MainMenu/Forge/Help` 与 `MenuShop/BoosterInfo/LegendaryTooltip` **已经在 Table 里**，`Missions/Info/*`、`MainMenu/Campaign/Help` 补进去即可）—— 同时改掉钉着 `Trim()==""` 的断言。
5. **`ForgeTab` 的阵营名 / `LevelText` / `Claim`** 走 `Loc.T("Armies/"+a)` / `Loc.T("MainMenu/Level")` / `Loc.T("MainMenu/General/Claim")`（三条键都已在 `Loc.cs`）。
6. 其余文案行（连登族、每日奖励族、遭遇战/排位/搜索窗）随第 1 条一起换。
