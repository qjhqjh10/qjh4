# B90_C2b · 5 条 b 档缺的 prefab 几何（2026-10-28 · 只读代理实测，调度台落盘）

> 出处：代号 **C2** 只读代理第二份产出。**未新建目录、未动 `d:/2` 一字节**（全部流式/只读）。
> 复现命令见 §方法。

## §0 两个必须先说的结论（影响这 5 条全部后续工作）

**1. 这 5 份 prefab 全都在「本地安装」的解包 dump 里，不是远端独有。**
方法 = **按 `m_Script` pathID 反查**（不是按类名/prefab 名）：`d:/2/新解包资源/assets_full/bundle_Waprforge_monoscripts/MonoScript/MonoScript_<pathID>.json` 的 `m_ClassName` 与远端 `apk135/monoscripts.json` 的 pathID **完全一致**（同一份脚本集、722 条），再用 pathID 去 `assets_full/bundle_menus_assets_all/MonoBehaviour/*.json` 搜 `"m_PathID": <pathID>`：
`TwoSidesTeamDrawer` 5 实例 · `AllianceView` 2 · `CreateAllianceMenu` 1 · `RankedArmyBonusContainer` 1 · `RankedMenuContainer` 2 · `DraftModeDeckInfoPanel` 3 · `DeckInfoAllianceTab` 1 · `AlliancesEventScorePanel` 1。
⇒ 🔴 **账里「RankedMenuContainer / RankedArmyBonusContainer 的 prefab 在远端」这句要就地订正**：远端包里确实有，但**本地 dump 也有同一棵树** ⇒ 直接用 `menu_dump.py`，**不必解 739 MB**。

**2. 类名订正**：`AllianceEventScorePanel` 这个类**不存在**；真名 = **`AlliancesEventScorePanel`**（多一个 `s`，script pathID `-1801456746368500805`）。这就是上一轮「它不在任何包 MonoScript 引用里」的根因 —— **按错名字查的**。它的 GameObject 名确实叫 `Alliance Event Score Panel`，**名字与类名不同**。

**两条口径（⚠️ 写进纪律）**
- ① `assets_full` 的 JSON **带 MonoBehaviour 字段名** ⇒ **对本地有效**；**远端 bundle 的 typetree 已剥**（`legendaryRankDivisionInfoObjects` / `armyContentAnchor` 在 `menus_assets_all.bundle` 里 grep = 0）⇒ **⛔ 不能用「grep 字段名 = 0」否定远端资产**。
- ② 判「本地没有」⛔ 不按类名搜：走 `m_Script.m_PathID` → `MonoScript_<pid>.json` 的 `m_ClassName`。本轮 5 条全是这么找到的。

**读数口径（`menu_dump.py` A145 头注）**
- 「绝对矩形」= **布局框**（设计值）；画出来 = 布局框 × 该件自己的 `m_LocalScale`。
- 被 `LayoutGroup` 管的节点，其 `anchoredPosition/sizeDelta` 是**工具按 uGUI 算出的布局后值**，不是 prefab 字段 ⇒ 要 prefab 原值加 `--no-layout`。
- **兄弟次序 = 表里行的先后**（按 `m_Children` 输出）；压层判断看它，⛔ 别按名字排序。

---

## §A → `CardPresentation/Shell/DraftModeWindow.cs`（账 #2 `DraftModeDeckInfoPanel.ToggleAllianceInfo`）

**挂载关系（pathID 可直接反查）**

| 对象 | GameObject pid | MB pid | script pathID | RectTransform pid |
|---|---|---|---|---|
| DraftModeDeckInfoPanel（3 实例） | — | `-1258886042170896974` / `-4683282722869380411` / `5994191405725149930` | `-1515315020393681768` | — |
| Alliance Detail View | `4119050740229393842` | `5279035319116090802`（`DeckInfoAllianceTab`） | `2973877969895633253` | `7570858989128699314` |
| Alliance Event Score Panel | `-2870818101310992974` | `-2754075171290100302`（**`AlliancesEventScorePanel`**） | `-1801456746368500805` | `8556397043092802994` |

`DraftModeDeckInfoPanel` 字段：`energyInfoToggle` · `cardListToggle` · `allianceToggle` · `energyBalancePanel` · `cardListPanel` · `allianceInfoPanel` · **`alliancesScorePanel`**。
实例 A：`allianceInfoPanel` = MB `5279035319116090802`；`alliancesScorePanel` = MB `-2754075171290100302`。
`DeckInfoAllianceTab` 字段只有一格：**`progressBar` → `2Armies Progress Bar`**。

**A-2 `Alliance Detail View`** 绝对矩形 **1490.62,105.20 → 1906.08,890.92**（415.47×785.72）；子件次序：`2Armies Progress Bar` → `Alliance Event Score Panel`。

**A-3 `2Armies Progress Bar`**（`Slider` + `ArmyProgressBar`；字段 `leftArmyImage`/`rightArmyImage`/`slider`/`text`），相对 Detail View 根：

| 缩进 | 名字 | 矩形 | 组件 | sprite |
|---|---|---|---|---|
| 1 | 2Armies Progress Bar | 24.09,85.86→391.38,215.34 | `Slider`,`ArmyProgressBar` | — |
| ·2 | Background | 108.76,162.80→306.76,178.35 | `Image`,`EverguildButtonMaterialModifier`,`Mask` | 无图 |
| ··3 | Fill Area | 108.76,162.80→306.76,178.35 | — | — |
| ···4 | Fill | 108.76,178.35→108.76,178.35 | — | 无图 |
| ····5 | end | 85.03,163.42→114.46,195.28 | Image | **`40k_campaign_bar_end`** 22×18 |
| ·2 | counter | 97.55,124.70→317.92,189.45 | `EverguildTextMeshPro` | — |
| ·2 | Outline | 24.09,58.28→391.38,241.39 | `Image`,`EverguildButtonMaterialModifier` | **`UI_Draft event result bar holder`** 633×282 |
| ·2 | leftArmyIcon | 34.33,136.58→108.11,210.55 | `Image`,`AspectRatioFitter`（高控宽 0.997387） | 无图（运行时按阵营填） |
| ·2 | rightArmyIcon | 308.87,135.54→382.93,209.79 | 同上 | 无图 |

> ⚠️ 与旧账的 y 差：账 `轮抽.tsv:58` 记的 `Outline=367.29×183.11`、`2Armies Progress Bar 1513.7,227.1→1881.0,356.6` 是**另一个基准根**。本件统一以 **Alliance Detail View 根 = 0,0** 为基准；⛔ 两套坐标别混用，要绝对坐标就加 1490.62 / 105.20。

**A-4 `Alliance Event Score Panel`（`AlliancesEventScorePanel`）**
+ 🔴 **`m_LocalScale = 0.95581`（三轴）** ⇒ 视觉框 = 布局框 × 0.9558（⛔ 别当 1）
+ `sizeDelta = 383.587 × 511.516`，pivot (0.5,0.5)，相对 Detail View 根 **15.94,217.00→399.53,728.52**
+ 字段 8 格：`scoreBar` · `leaderboardButton` · `leaderboardButtonNotInAlliance` · `allianceNameText` · `inAllianceGO` · `notInAllianceGO` · `joindAllianceButton` · `leaderboardPopup`

| 缩进 | 名字 | 矩形（相对 Score Panel 根） | 组件 | sprite / 文本 |
|---|---|---|---|---|
| 0 | Alliance Event Score Panel | 0,0→383.59,511.52（×0.9558 → 366.64×488.91） | `AlliancesEventScorePanel` | — |
| ·1 | **In Alliance** | 24.41,218.75→391.05,707.66 | `VerticalLayoutGroup`（spacing 0 · align 1 · pad 0,0,0,-22 · expandH=1） | — |
| ··2 | Alliance Name | 24.22,218.75→391.25,266.54 | `EverguildTextMeshPro` | 运行时填 |
| ··2 | View Leaderboard Button | 90.65,279.74→324.82,344.39 | `Image`,`EverguildButton`,`EverguildButtonMaterialModifier` | **`UI_Button_Mulligan`** 410×124 九宫 333,96,333,96 |
| ···3 | Button Text | 101.83,284.57→312.88,339.55 | TMP + `Localize` | 词条 key = `mTerm`（**未取到**） |
| ··2 | Alliance Event Score Info | 70.77,357.60→344.69,715.48 | **`AllianceScoreBar`**（`progressBar`,`scoringLines`） | — |
| ···3 | Progress Bar | 171.41,338.61→244.05,734.84 | `Slider` | — |
| ····4 | Background | 191.94,364.45→223.53,709.00 | Image | **`MiniBar_01`** 59×648 |
| ···3 | **Score Levels** | 159.94,345.34→255.52,654.83 | `VerticalLayoutGroup` **reverse=1** · spacing 0 · align 4 · expandH=1 | — |
| ····4 | Level 1 | 66.46,594.98→349.01,652.78 | 字段 `chestOpen`/`chestReward`/`score` | Chest **`40k_Crate_Tier1_Iron`**；Skull `40K_missions_icon_Daily skulls` 222×198 |
| ····4 | Level 2 | 66.46,533.08→349.01,590.89 | 同上 | Chest **`40k_Crate_Tier2_Copper`** |
| ····4 | Level 3 | 66.46,471.18→349.01,528.99 | 同上 | Chest **`40k_Crate_Tier3_Silver`** |
| ····4 | Level 4 | 66.46,409.28→349.01,467.09 | 同上 | Chest **`40k_Crate_Tier4_Gold`** |
| ····4 | Level 5 | 66.46,347.39→349.01,405.20 | 同上 | Chest **`40k_Crate_Tier5_Warp`** |
| ·····5 | 每行两子件 | Chest 231.33,585.08→318.34,662.68（按行位移）；Skull 135.39,598.91→191.38,648.85 | Image | 见右 |
| ·1 | **No Alliance**（整块空 GO） | 24.41,228.30→391.05,717.22 | — | `inAllianceGO` / `notInAllianceGO` 切这两块 |
| ··2 | Background | 41.45,228.30→374.02,717.22 | `Image` | **`UI_Deck_Information_submenu_Back_opaque`** 69×63 九宫 18,18,18,18 |
| ··2 | Join Alliance text | 24.41,228.31→391.44,459.05 | TMP + `Localize` | 词条在 `mTerm` |
| ··2 | Join Alliances Button | 90.65,469.34→324.82,533.99 | Image+EverguildButton+MatModifier | `UI_Button_Mulligan` 410×124 |
| ··2 | Leaderboard Button | 90.84,541.02→325.02,605.68 | 同上 | 同上 |

---

## §B → LiveOps 族（`Shell/ParentEventWindow.cs` 的 `BuildChooseSidePage` 支；账 #8 `TwoSidesTeamDrawer.Initialize`）

**根 prefab = `Two Side Event Select Team`**（535×737.78，父为空）
RT pid **`520161063944934852`** · MB pid **`-1293532157455979002`** · script pathID **`-9102396389962747900`**
组件：`EverguildButton` + `TwoSidesEventSelectTeamContainer` + **`TwoSidesTeamDrawer`**
字段仅 3 格：`teamNameText` → `Team Name` · `teamCaptainText` → `Team captain` · `teamImage` → **`Foreground Image`**（Image，无图，运行时按队伍填）

| 缩进 | 名字 | 矩形（相对根） | 组件 | sprite / baked 文本 |
|---|---|---|---|---|
| 0 | Two Side Event Select Team | 0,0→535.00,737.78 | 见上 | — |
| ·1 | Background | 0,0→535.00,737.78 | `EverguildButtonMaterialModifier`,`SkewedImage`,`Mask`,`UICornersGradient` | 无图 |
| ··2 | Background noise | -46.83,-62.66→653.83,737.78 | Image | **`UI Dirt And Noise skratches`** 1024×1024 |
| ··2 | **Foreground Image** | -179.28,0→714.28,893.56 | Image | 无图（= `teamImage`） |
| ··2 | Bottom Glow | -98.92,2.38→633.92,735.23 | Image | **`Smooth background lateral`** 32×32 九宫 3,0,3,0 |
| ·1 | Bottom Text | 2.13,629.39→532.95,735.26 | `EverguildButtonMaterialModifier` | — |
| ··2 | **Team Name** | -18.77,588.27→510.48,666.09 | TMP | `'Red Team'` 58.8/36（auto 18~86.4）居中 Capline 白 |
| ··2 | Tap to select | -24.30,670.17→500.34,725.88 | TMP + `Localize` | `'Select'` 46.74/36 色 (1,0.656,0.656,1) |
| ·1 | Border | 0,0→535.00,737.78 | MatModifier,`SkewedImage` | **`40k_square_border`** 64×64 九宫 13 |
| ·1 | Top | 24.50,0→557.67,100.00 | — | — |
| ··2 | **Team captain** | 44.12,11.09→548.95,88.91 | TMP | `'#squidmar'` 58.8/36 居中 Capline 白 |

**同脚本另有 4 个实例**（建树时别挑错）：`RT 7666018153708695046`（MB `-7667961802465038842`，535×737.78，父 `-2524807581905408506`）· `RT -2379004296510460410`（535×737.78 同父）· `RT 4963441752012140038`（684.27×871.14）与 `RT -7001374241831604730`（731.52×871.14，父 `514381665508044294`）⇒ 后两个是**更宽的变体**（阵型/胜负面），**本轮未展开**。

---

## §C → `Shell/AllianceSearchTab.cs` / `AllianceMemberTab.cs`（账 #5 `AllianceView.Initialize(string groupId)`）

script pathID **`-5419969195901021718`**，**2 个实例**（同一棵 `GeneralDetails`，两个父变体）：

| 实例 | GO | RT pid | 尺寸 | 父（绝对矩形） | MB pid |
|---|---|---|---|---|---|
| 1（非成员） | GeneralDetails | `-8680982849342087005` | 1586.33×917.98 | `AllianceNotMemberVariant` 332.17,70.90→1919.50,1080.02 | `6267388025831022755` |
| 2（成员） | GeneralDetails | `-4327119531760820061` | 1588.83×891.22 | `AllianceMemberVariant` 330.67,70.87→1920.50,1080.05 | `-6226043772329533277` |

字段 14 格：`content` · `icon` · `nameText` · `regionText` · `ratingText` · `draftRatingText` · `descriptionText` · `languageDropdown` · `privacyDropdown` · `toggleEditButton` · `saveButton` · `cancelButton` · `memberList` · `memberOptionsPopup`

| 缩进 | 名字（路径） | 矩形（相对 GeneralDetails 根） | 组件 | sprite / 文本 |
|---|---|---|---|---|
| 0 | GeneralDetails | 0,0→1586.33,917.98 | `AllianceView` | — |
| ·1 | Content | 0,0→1586.33,917.98 | — | — |
| ··2 | DEBUG_TEXTS | 622.32,-86.54→1121.68,-15.46 | TMP | `'Season: 10\n10/30/200 29:00\n'` 37.5/50 红 —— **原版 `Awake` 里被关掉的那件**（`AllianceView.cs:98`） |
| ··2 | **BadgeDrawer** | 27.72,8.44→278.28,243.56 | `ItemDrawerComponents`,`AllianceBadgeDrawer` | — |
| ···3 | Frame / Badge | 27.72,8.44→278.28,243.56 | Image | 无图 |
| ··2 | **Alliance name text** | 282.21,15.44→781.57,86.52 | TMP | `'Alliance Name bla bla'` 50/50 左 Midline 白 |
| ··2 | **Alliance Rating Display** | 282.21,75.34→768.79,156.16 | `HorizontalLayoutGroup`,`AllianceRatingDisplay`（`ratingIcon`,`ratingText`） | — |
| ···3 | Secondary Icon | 282.21,75.34→376.92,156.16 | Image + `LayoutElement` | **`40k_UI_icon_ranked_Skirmish`** 128×128 |
| ···3 | Main Icon | 282.21,75.34→342.21,156.16 | 同上 | 同上 |
| ···3 | Individual rating value | 342.21,75.34→768.79,156.16 | TMP + LayoutElement | `'-------'` 42/31.38 |
| ··2 | **Draft Rating Display** | 282.21,156.17→768.79,236.99 | `HorizontalLayoutGroup`,`AllianceRatingDisplay` | — |
| ···3 | Main Icon | 282.21,156.17→342.21,236.99 | Image + LayoutElement | **`40k_battle_Win Skull`** 66×73 |
| ··2 | **Config fields** | 746.86,20.98→1529.68,80.98 | `HorizontalLayoutGroup`（align 4） | — |
| ···3 | **extra_info**（= `regionText`） | 1075.99,20.98→1529.68,80.98 | TMP | `'English / Private'` 40/48 **右** Middle |
| ···3 | LanguagesDropdown | 797.22,21.58→1047.22,80.98 | `Image`,`TMP_Dropdown` | **`40K_dropdown_field_closed`** 727×102 九宫 60,35,60,35；Label 18/14；Arrow **`40K_dropdown_arrow_closed`** 46×19；Template **`40K_dropdown_bg`** 119×102 九宫 23,20,23,20 |
| ···3 | Privacy Dropdown | 1061.72,21.58→1311.72,80.98 | 同上 | 同上 |
| ···3 | Edit button | 1326.22,20.98→1384.38,80.98 | Image,`EverguildButton` | **`40k_general_bt_yellow`** 71×71；Button Text `'X'` 30/12；Image **`40k_general_bt_yellow_edit`** |
| ···3 | Confirm button | 1398.88,20.98→1457.03,80.98 | 同上 | `40k_general_bt_yellow` |
| ···3 | Cancel button | 1471.53,20.98→1529.68,80.98 | 同上 | 同上 |
| ··2 | Description input text | 796.77,100.02→1545.53,293.25 | `Image`,`EverguildInputField` | **`40K_dropdown_bg`** 119×102 |
| ···3 | Text Area | 806.77,107.02→1535.53,287.25 | `RectMask2D` | — |
| ··2 | Divisor line members | -0.50,301.02→1586.83,304.70 | Image | **`40k_Separator Fade Sides Horizontal`** 128×4 九宫 63,0,63,0 |
| ··2 | **MemberList** | 38.25,257.72→1549.75,917.98 | `AllianceMemberList` | — |
| ···3 | members label | 38.25,258.19→633.08,299.61 | TMP | `'Members: --/20'` 40/40 左 |
| ···3 | Scroll View | 38.50,304.80→1549.50,917.98 | `Image`,`ScrollRect`（该行 `⛔MULTI-COMP(2 组件·1 禁用)`） | `Background` 32×32 九宫 10,10,10,10 ppu=200 |

> ⚠️ `Config fields` / 两个 `AllianceRatingDisplay` 都是 **LayoutGroup 管的孩子** ⇒ 三列是**布局后值**；要 prefab 原值跑 `--no-layout`。
> ⚠️ `Scroll View` 那行 = **有两组件、其中一个被禁**，⛔ 别把「禁用」记到 `ScrollRect` 头上。

---

## §D → 同族（账 #7 `CreateAllianceMenu.Initialize`）

**根 = `Create Alliance View`**（1513.91×914.90）
RT pid **`-9064162221997825885`** · MB pid `2928508950716080291` · script pathID `-5956949439289075468` · 父 = `AllianceNotMemberVariant`（**与 §C 实例 1 同一个父** = 同一扇窗的两页）
字段 → 节点（**全解出**）：`nameInput`→`Name Input`(`-2986694751790047069`) · `descriptionInput`→`Desc Input`(`4677582473422758051`) · `languageDropdown`→`LanguagesDropdown`(`-455315133275447133`) · `privacyDropdown`→`Privacy Dropdown`(`971300103518937251`) · **`createButton`→`Price Display Button`**(`-8656787415366115165`)

| 缩进 | 名字 | 矩形（相对根） | 组件 | sprite / 文本 |
|---|---|---|---|---|
| 0 | Create Alliance View | 0,0→1513.91,914.90 | `CreateAllianceMenu` | — |
| ·1 | TopAnchor | 0,0→1513.90,61.87 | — | — |
| ··2 | Name input title | 64.00,92.53→756.95,142.53 | TMP + `Localize` | `'Alliance Name'` 40/36 |
| ···3 | Name Input | 64.00,142.83→964.00,202.23 | Image,`EverguildInputField` | **`40K_dropdown_bg`** 119×102 九宫 23,20,23,20 |
| ····4 | Text Area | 74.00,149.83→954.00,196.23 | `RectMask2D` | — |
| ··2 | Desc input title | 64.01,237.87→756.96,287.87 | TMP + Localize | `'Alliance Description'` 40/36 |
| ···3 | Desc Input | 64.01,289.43→964.01,493.88 | Image,`EverguildInputField` | `40K_dropdown_bg` |
| ····4 | Text Area | 74.01,296.43→954.01,487.88 | `RectMask2D` | — |
| ··2 | Create Alliance Text | 59.62,501.70→309.62,558.48 | TMP + Localize | `'Create alliance'` 40/36 |
| ···3 | **Price Display Button** | 59.62,549.09→309.67,627.87 | **`PriceDisplayButton`**（`blinkEffect`,`button`,`priceDisplay`） | — |
| ····4 | Generic UI Button | 59.62,549.09→309.67,627.87 | Image,`EverguildButton`,MatModifier,`UIEffect`,`UIEffectTweener` | **`40K_button`** 489×107 九宫 234,46,234,46 |
| ··2 | Select Language | 1071.95,85.75→1321.95,142.53 | TMP + Localize | `'Select language'` 40/36 |
| ···3 | LanguagesDropdown | 1071.95,142.52→1321.95,201.92 | Image,`TMP_Dropdown` | `40K_dropdown_field_closed` 727×102；Label 18/14；Arrow 46×19；Template `40K_dropdown_bg` |
| ··2 | Select Privacy | 1071.95,232.38→1321.95,289.16 | TMP + Localize | `'Select privacy'` 40/36 |
| ···3 | Privacy Dropdown | 1071.95,289.16→1321.95,348.56 | Image,`TMP_Dropdown` | 同上 |

**`1000` 的出处（这格以前是「写死 1000 = 样例值」，现在有实据）**
`Price Display` RT `-1193917768153177949`，`sizeDelta (0,-13.596)`，anchors `(0.0378,0.1085)→(0.9543,0.8765)`，pivot (0.5,0.5)；两个子件 `icon`（46.91²）、**`text`（67.06×46.91）baked 文本 = `"1000"`**
⇒ **1000 是 prefab 里烘的样例字**，不是配置值（`AlliancesConfig.createAlliancePrice` 本地确实无资产）。

---

## §E → `Shell/MainMenuRuntime.cs`（账 #22 `Clean` / #23 `OnInitialize` / #24 `ToggleLegendaryRankInfo`）

### E-1 `Reward Event Container 1x1` = `RankedArmyBonusContainer`
RT pid **`-6548592895001984001`**（**prefab 根**，父空）· MB pid `5217132012044452863` · script pathID `7570601426325040590`
`sizeDelta = 535 × 414`，pivot (0.5,0.5)，`anchoredPosition (0,-43.8868)`
组件：`EverguildButton` · `FlexibleLayoutSizeOption` · `RectMask2D` · `CustomLocalizeCallback` · **`RankedArmyBonusContainer`**
字段 9 格：`backgroundButton` · `title` · `timer` · `backgroundImage` · `foregroundImage` · **`armyPrefab`** · **`armyContentAnchor` = `Content`** · **`infiniteScroll` = `Dynamic Content` 上的 `UI_InfiniteScroll`** · **`thresholdForAutoScroll = 2`**

| 缩进 | 名字 | 矩形（相对根） | 组件 | sprite / 文本 |
|---|---|---|---|---|
| 0 | Reward Event Container 1x1 | 0,0→535.00,414.00 | 见上 | — |
| ·1 | Background Image | -106.66,-167.25→641.66,581.07（748.33²） | `UIParallaxImage`,MatModifier,`AspectRatioFitter ⚙高控宽(1)` | 无图（运行时按活动填） |
| ·1 | Foreground Image | 同上 | 同上 | 无图 |
| ·1 | TextDarkening | 2.58,305.60→532.49,411.47 | `Image`(色 0,0,0,0.612),MatModifier,`VerticalLayoutGroup`（spacing -4.2 · align 3 · pad 11,0,0,0 · expandW/H=1） | — |
| ··2 | Event Title | 13.58,309.01→527.28,364.72 | TMP | `'GAME MODE TITLE'` 58.8/36 auto 18~72 左 Midline |
| ··2 | Timer With Time Description | 13.58,367.33→438.29,408.06 | **`TimerDisplay`**（`extraText`/`lastMinutesText`/`layoutGroupContentFixer`/`timer`/`timerTextDescription`/`useExtraText`/`useLastMinutesText`） | — |
| ···3 | Timer Description | 13.58,367.33→13.58,408.06（CSF 收缩，**尺寸别照抄**） | TMP,`ContentSizeFitterMinMax` | `'Starts in:'` 42.95/45 |
| ····4 | Clock Icon | 13.58,367.32→54.34,408.08 | Image | **`WF_icon_clock for shader`** 64×64 |
| ·····5 | Timer Text | 54.34,364.94→253.33,410.46 | `TextMeshProUGUI`,`EverguildTextController` | `'5d 20h 15m'` 40/30 色 (0.737²,1) |
| ·1 | Border | 0,0→535.00,414.00 | Image,MatModifier | **`40k_square_border`** 64×64 九宫 13 · **Sliced · fillCenter=0 · ppuMul=5.0** |
| ·1 | **Dynamic Content** | 2.54,11.16→532.34,291.99 | `ScrollRect`(h=1 v=0 mode 0 inertia 1 elasticity .1 decel .135)·MatModifier·**`UI_InfiniteScroll`**·`RectMask2D` | — |
| ··2 | Viewport | 2.54,11.16→532.34,291.99 | —（pivot (0,1)） | — |
| ···3 | **Content** | 2.54,11.16→532.34,291.99 | `HorizontalLayoutGroup`（spacing **20** · align 4 · ctrlH=1 · expandW/H=1） | ← `armyContentAnchor` |
| ·1 | Feature Badge | -4.95,-4.02→185.90,186.44（190.85×190.45） | Image,MatModifier,`UIEffect`,`UIEffectTweener` | **`UI_Army_Selection_Featured`** 172×172 preserveAspect |

> `Clean()` 只动两处：`DestroyAllChildren(armyContentAnchor=Content)` + 清 `currentArmyImages`。**建树时 `Content` 必建**（否则 `Clean`/`OnInitialize` 无处落）。

### E-2 `Ranked Game Mode Container 1x2` / `Skirmish Ranked Game Mode Container 1x2` = `RankedMenuContainer`（#24）
**2 个实例，几何完全相同**（535×848.8，pivot 0.5/0.5，`anchoredPosition (0,0)`）：

| 实例 | GO 名 | RT pid | MB pid |
|---|---|---|---|
| 1 | **Skirmish** Ranked Game Mode Container 1x2 | `-6745258406248085284` | `-7895716745669408548` |
| 2 | Ranked Game Mode Container 1x2 | `2814194656236313102` | `4921922172160219662` |
| — | script pathID | — | **`-5793855663760959575`** |

字段 9 格：`backgroundButton` · `title` · `timer` · `backgroundImage` · `foregroundImage` · `milestonesStepDisplay` · **`legendaryRankDivisionInfoObjects`** · `rankedPositionValue` · `ratingDisplay`

**🔴 `legendaryRankDivisionInfoObjects` 的成员（pid 级实证）**：`[ 「Position #」(GO `-7872084266019944228` / RT `-6387487148506642212`), 「Position Value」(GO `-6209140577534834468` / RT `8873524722899454172`) ]`

| 缩进 | 名字 | 矩形（相对根） | 组件 | sprite / 文本 |
|---|---|---|---|---|
| ·1 | **Rating** | 20.19,632.88→514.81,742.40 | — | — |
| ··2 | Mission Milestones Progress | 20.19,644.38→514.81,730.89 | **`MilestonesStepDisplay`**（`stepPrefab`,`stepsHolder`） | ← `milestonesStepDisplay` |
| ···3 | steps | 20.19,644.38→514.81,730.89 | `HorizontalLayoutGroup`（spacing 3.2 · align 3 · ctrlW/H=1） | — |
| ····4 | RankedSealStep Animation Disabled | 20.19,644.38→120.19,730.89（100×86.51） | **`MilestoneStep`**（9 字段）+ Image + LayoutElement | sprite `<未解出 -300467167438017286>` |
| ··2 | **Global Rating** | 20.19,632.88→514.81,742.40 | `HorizontalLayoutGroup`（align 4）+ **`AllianceRatingDisplay`**（`ratingIcon`,`ratingText`） | ← `ratingDisplay` |
| ···3 | **Position #** | 206.50,687.64→247.50,687.64（LayoutElement pref 41） | TMP + MatModifier + `LayoutElement` | `'#'` 66.6/66.6 居中 |
| ···3 | **Position Value** | 247.50,687.64→247.50,687.64 | TMP + MatModifier | `'----'` 78/31.38 左 |
| ···3 | Secondary Icon | 105.46,632.88→165.46,690.23 | Image + LayoutElement，**`activeSelf = F`（出厂关）** | **`40k_UI_icon_ranked_Skirmish`** 128×128 |
| ···3 | Main Icon | 247.50,645.64→328.50,729.64（81×84） | Image + LayoutElement | 同上 |
| ···3 | Individual rating value | 328.50,687.64→328.50,687.64（CSF） | TMP + LayoutElement + `ContentSizeFitterMinMax` | `'----'` 78/31.38 auto 20~78 |
| ·1 | TextDarkening | 2.58,740.40→532.49,846.27 | Image(黑 0.612) + MatModifier + `VerticalLayoutGroup`（spacing -4.2 · pad 11,0,0,0） | — |
| ··2 | Event Title | 13.58,743.81→527.28,799.52 | TMP | `'GAME MODE TITLE'` 58.8/36 |
| ··2 | Timer With Time Description | 13.58,802.13→438.29,842.86 | `TimerDisplay` | — |
| ·1 | Border | 0,0→535.00,848.80 | Image,MatModifier | `40k_square_border` Sliced fillCenter=0 ppuMul 5.0 |

> **#24 的三块**（原版 `ToggleLegendaryRankInfo` = `foreach legendaryRankDivisionInfoObjects → SetActive(option)` + `ratingDisplay.gameObject.SetActive(option)`）：
> ① `Position #` ② `Position Value` ③ `Global Rating`（**连它的两个 Icon 与 Individual rating value 一起显隐**）。⛔ 别去找叫 `Legendary` 的节点。

---

## §未取到（⛔ 别当已解）

1. `View Leaderboard Button/Button Text`、`Join Alliances Button/Button Text` 的 **`mTerm` 词条 key**（只有 baked 样例串）
2. `RankedArmyBonusContainer.armyPrefab` 的 **Prefab pathID**（账里写 `Simple Army Icon`，本轮未反查）
3. `RankedSealStep Animation Disabled` 的 **sprite 名**（sprite pid `-300467167438017286` 未解出）
4. `TwoSidesTeamDrawer` 另外两个宽变体（`4963441752012140038` 684.27×871.14 / `-7001374241831604730` 731.52×871.14）的**根名与子树**
5. `Ranked Game Mode Container` 实例 1 的 `Timer Text`（depth 截断，**没实测**）

## §方法留档（下一波直接照抄）
```
# 1) 类 → pathID
python -I -c "import json;d=json.load(open('d:/4/_tmp_view/apk135/monoscripts.json'));print(d.get('<类名>'))"
# 2) pathID → 本地 MonoScript 类名（反查，⛔ 不按类名搜）
ls d:/2/新解包资源/assets_full/bundle_Waprforge_monoscripts/MonoScript/ | grep <pathID>
# 3) pathID → 实例（真·否定/肯定证据）
grep -rl '"m_PathID": <pathID>' d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/
# 4) MB → GameObject → RectTransform（--rt 用这个 pid）
# 5) 摊树
cd d:/4/Unity/工具 && python -I menu_dump.py bundle_menus_assets_all --rt <RT pid> --depth 8 --relative --md
#    （--no-layout 取 prefab 原值；--no-sprite 快）
# 6) 远端包（只在本地确实没有时才用；本批 5 条都不需要）
unzip -l "d:/4/_tmp_view/apk135/UnityDataAssetPack.apk"   # 186 条目 / 739 MB
```

## 纪律
未改任何 `.cs`/`.tsv`/账主表；未动 git；未解包出新目录；`d:/2` 全程只读；未执行包里任何东西。
