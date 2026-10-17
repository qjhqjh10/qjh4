# W_T18 排位赛季三窗 + 反馈问卷（四扇整窗）

> 2026-10-17 · 界面线 T18 收尾 · 执行写手报告
> 白名单：`Shell/RankedDivisionChangeWindow.cs` · `Shell/RankedNewSeasonWindow.cs` ·
> `Shell/RankedSeasonEndedWindow.cs` · `Shell/GiveFeedbackPopup.cs` · `Shell/WindowsManager.cs` ·
> `Editor/MainMenuScene.cs` · 本报告。
> ⚠️ **实际落了 4 个新 `.cs` 而不是任务里写的「`Shell/RankedSeasonWindows.cs` 三窗一个文件」** ——
> 工程硬规矩：`MonoBehaviour` 子类**必须类名 == 文件名**，否则存盘烘成 `!u!115` 存根、player 加载即崩
> （CLAUDE.md §二 · A1506/A1508）。三窗各自单独成文件是那条规矩的必然结果。

---

## 一、结论（先看这段）

1. **四扇整窗都建出来了**，都是 `GameWindow` 子类、都能从 `WindowsManager` 开出来：
   `OpenRankedDivisionChange` / `OpenRankedNewSeason` / `OpenRankedSeasonEnded` / `OpenGiveFeedback`
   （全部走 `OpenByRef`，键 = 各自 prefab 根名，**两处同源**）。
2. **窗参逐值照 prefab 根 MB**：三窗 + 反馈窗的 `type / windowsPlacement / closeOnESC / updateNavPanel /
   extraScaleSmallScreen / openSound / closeSound / useDefaultCloseSoundIfNull` 全部实读填好
   （见 §二 表）。**`Ranked Season Ended Window` 的 `type = 0`（Fullscreen）是三分窗里的特例**。
3. **节点树 / 文案 / 可见性逐条对原版**（§二 每扇一份树）。服务端数据（段位名 / 段位图 / 评分 /
   印玺进度 / 赛季截止时刻 / 名次）**全部留空、显示 `--` 或整层不画，并各出声一次**，⛔ 一个数字都没编。
4. **`bash d:/4/Unity/工具/typecheck.sh` = 0/0**（运行时 + 编辑器两个程序集）。按用户口径**没有跑
   `*.Run`、没有起 player**。
5. 🔴 **任务简报里两条与本地事实不符，已按事实处置**（见 §五）：
   ① 反馈问卷的 `Demo/FeedbackSurvey/*` 词条**不在**本地官方 13 语言表里（`grep` = **0**）；
   ② 「42 颗钮」与 prefab 实读不符（实为 **41 颗多选钮 + 9×5 = 45 颗评分钮**）。

---

## 二、四扇窗的节点树与窗参（全部本地实读）

复跑命令（都在 `d:/4/Unity/` 下）：
```
"D:/2/Warpforge_tools/py312/python.exe" -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "<根名>" --depth 9
```

### 2·1 `Ranked Division Change Window` → `RankedEventDivisionChangeWindowNewSystem`

根 MB = `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-4938671765695524530.json`

| 窗参 | 值 | 出处 |
|---|---|---|
| `type` | **1**（Popup） | 根 MB |
| `windowsPlacement` | **15**（Popup） | 根 MB |
| `closeOnESC` | **1** | 根 MB |
| `updateNavPanel` | 0 | 根 MB |
| `extraScaleSmallScreen` | 1.0 | 根 MB |
| `openSound` / `closeSound` | 空 PPtr | 根 MB |
| `useDefaultCloseSoundIfNull` | 1 | 根 MB |
| `backgroundCloseButton.window` | **0** ⇒ 点暗底**不**关窗 | 子 MB `2768677375183130958` |
| `soundOnOpen` | 非空 PPtr（动画用，不是 `GameWindow.openSound`） | 根 MB |

树（1920×1080 · 左上原点 · px）：

```
Ranked Division Change Window
├─ Menu Dark Background          4574.60×2572.36 @ −1327.3,−746.2
├─ Title                         53.2,0→1866.8,112        'PROMOTED!'
│   term Event_Description/Ranked/Promoted · fs87 auto[30~87] base87 · Center/Midline(4096)
│  ├─ Game Mode Icon Left        860,6→960,106            100×100（m_Sprite = 0）
│  └─ Game Mode Icon Right       960,6→1060,106           100×100（m_Sprite = 0）
├─ Division Image                534.3,149.3→1385.7,1000.7  851.41²（m_Sprite = 0）
│  └─ Rank Icon                  926.9,424.4→994.3,491.8    67.418² ×1.090329 ⇒ 视觉73.51
│     └─ Rank Icon Glow          923.5,421→997.7,495.2      74.14² · **出厂 inactive**
├─ Text Content                  （整屏容器）
│  ├─ New division reached Text  257.9,105.1→1662.1,203.2  fs51.4 auto[18~51.4] base57
│  ├─ Ranking Points Title       469.3,655.7→1450.7,735.0   **inactive** · 'Rating:'
│  ├─ Rank Points                758.2,735→1161.8,835       **inactive**
│  │  ├─ Background              627.3,743.5→1292.7,826.5   40K_main_rank_display
│  │  ├─ Rank Icon               857.6,735→957.6,835        100²
│  │  └─ Ranking Points Quantity 957.6,745.4→1062.4,824.7   fs50
│  └─ Tap To continue            336.9,1000.7→1583.1,1080   'Click to continue' + BlinkGraphic
├─ Particles DivisionUp          （5 个 ParticleSystem —— 归特效线，只建容器）
│  ├─ Glow below / Sparks background / Sparks Burn（+ Glow Big / Promote Letter Effect / Sparks fast）
└─ Mission Milestones Progress   292.5,418.7→1627.5,661.3
   └─ steps  4× RankedSealStep   每颗视觉 249.98×606.64 @ x=418.5 / 696.2 / 973.8 / 1251.5
      └─ Empty（Rank Skull Empty）· Fill（Rank Skull · 出厂 alpha 0）
```

行为（`RankedEventDivisionChangeWindowNewSystem__Open.c`）：`GameWindow.Open()` → `get_windowData()` 为空
整条链早退；有数据才喂段位图/评分/印玺/两段协程。⇒ 我们这一档 = 「没有 windowData」那一档的画面。

### 2·2 `Ranked New Season WIndow` → `RankedEventStartSeasonWindow`

根 MB = `MonoBehaviour_7393637103769976266.json`

| 窗参 | 值 |
|---|---|
| `type` | **1**（Popup） |
| `windowsPlacement` | **15** |
| `closeOnESC` | **1** |
| `updateNavPanel` / `extraScaleSmallScreen` | 0 / 1.0 |
| 声音两格 / `useDefaultCloseSoundIfNull` | 空 / 1 |
| `backgroundCloseButton.window` | **0** ⇒ 点暗底不关窗 |

```
Ranked New Season WIndow                         （⚠️ 原版拼写 WIndow，大写 I）
├─ Back Background        −761.3,−427.1→2681.3,1507.1  3442.55×1934.17 · (0,0,0,0.7725)
├─ Title                  53.2,0→1866.8,207.69        'NEW RANKED SEASON'
│  term MainMenu/RankedWindow/NewRankedSeason · fs87.01 auto[18~72] base87.01 · Center/Middle
└─ Timer                  211.5,977→1708.5,1080       1496.98×102.96 · HLG spacing15.1 · MiddleCenter
   ├─ Timer Icon          920.2,996.2→984.7,1060.8    64.555² · WF_icon_clock
   └─ Timer               999.8,977→…                 'Ends in: 23d 5h\n' · fs50 auto[18~72] base50
```

行为（`RankedEventStartSeasonWindow__Initialize.c`）：`timeToEnd − CurrentServerDateTime()` →
`GetTimeString` → `Format(I2("Ends in: {0}"), …)`。**截止时刻是服务端数据**。

### 2·3 `Ranked Season Ended Window` → `RankedEventSeasonEndedWindow`

根 MB = `MonoBehaviour_-3630938164723221848.json`

| 窗参 | 值 |
|---|---|
| `type` | 🔴 **0**（Fullscreen —— 三分窗里唯一一个） |
| `windowsPlacement` | **15** |
| `closeOnESC` | **1** |
| `updateNavPanel` / `extraScaleSmallScreen` | 0 / 1.0 |
| 声音两格 / `useDefaultCloseSoundIfNull` | 空 / 1 |
| `backgroundCloseButton.window` | **0** ⇒ 点暗底不关窗 |
| `canvasGroup` | `CanvasGroup_-3969323529196569944.json`：`m_Alpha = 1` · 可交互 · 挡射线 |

```
Ranked Season Ended Window
├─ Menu Dark Background      −1327.3,−746.2→3247.3,1826.2   4574.60×2572.36
├─ Division Image            582.5,266.1→1337.5,1021.1      754.958²（m_Sprite = 0）
│  └─ Rank Icon              926.9,493→994.3,560.4          67.418² ×1.090329 ⇒ 视觉73.51（m_Sprite = 0）
├─ Title                     53.2,0→1866.8,111.55          'SEASON ENDED!'
│  term Event_Description/Renked/SeasonEnd（原版拼写 Renked）· fs87.01 · Center/Capline(8192)
├─ Percentage Or Score text  −0.1,200.2→1920.1,266.1       'You are in the top {0}% of players.' · Center/Capline
├─ Tap To Continue           −0.1,985.5→1920.1,1080        'Click to continue' + BlinkGraphic · Center/Capline
├─ Glow below                909,543→1009,643              100² ×250（ParticleSystem —— 归特效线）
└─ Rank Points               237.8,100.2→1682.108,200.2    1444.308×100 · HLG · MiddleCenter
   ├─ Ranking Points Title   797.9,110.5→917.3,189.8        'Rating:' · fs40 · Center/Middle
   ├─ GameMode Icon          917.3,100.2→1017.3,200.2       100² · 40k_UI_icon_ranked_Skirmish
   └─ Ranking Points Quantity 1017.3,110.5→1122.1,189.8     '12153' · fs50 base40 · Left/Capline
```

行为（`RankedEventSeasonEndedWindow__Open.c`）：赛季统计 → 段位图/段位图标（`RankedDivisions`）→
`score` = 评分整数 → `positionOrPercentageText` = `EndFinishPercentage` 或 `EndFinishScore`（二选一）→
`canvasGroup.DOFade(...).From(...)`。⇒ 全在服务端。

### 2·4 `Give Feedback Popup menu` → `GiveFeedbackMenuDemo`

根 MB = `MonoBehaviour_5403621168596701646.json`

| 窗参 | 值 |
|---|---|
| `type` | **1**（Popup） |
| `windowsPlacement` | **15** |
| `closeOnESC` | **1** |
| `updateNavPanel` / `extraScaleSmallScreen` | 0 / 1.0 |
| 声音两格 / `useDefaultCloseSoundIfNull` | 空 / 1 |
| 根上的词条键 | `thanksForFeedbackKey = Demo/FeedbackSurvey/OnSubmitMessage` · `thanksPopupButtonKey = Demo/Feedback/CloseThanksPopup` · `onCloseKey = Demo/FeedbackSurvey/BeforeClose` |
| `Menu Dark Background/BackgroundCloseButton.window` | **0** ⇒ 点暗底不关窗 |

树（顶层 + 问卷结构；**逐行 rect 见 `Shell/GiveFeedbackPopup.cs` 的常量与 `Sec[]/Item[]` 表**）：

```
Give Feedback Popup menu
├─ Menu Dark Background          −1327.3,−778.5→3247.3,1793.8   4574.60×2572.36 · (0,0,0,0.7725)
├─ Window                        −155.5,38.6→2075.5,975.7       2231×937.09
│  ├─ Generic Popup Background   38,38.6→1863,1035.5            1824.95×996.86 · 40k_popup 九宫 169,160,169,160
│  │  ├─ Mask（Image+Mask，showGraphic=0） 48.4,48→1853.1,1025.7
│  │  │  └─ Background fill      40k_popup_texture · Tiled · ppuMul=2.0（⇒ 每块 64px）
│  ├─ Title                      105.1,75.2→1710.9,153.1        'Give us your feedback!' · fs50 base36 · Left/Middle
│  ├─ Subtitle                   105.1,153.1→1710.9,193.3       'It takes less than two minutes…' · fs42.45 · Left/Middle
│  ├─ Submit button              720.6,909.2→1198.9,984.2       40K_button 九宫 234,46,234,46 · tint (0.369,0.894,0.587,1)
│  │  └─ Button Text             733.3,908.6→1185.6,983.6       'Submit' · fs45 base12 · Center/Midline
│  ├─ Generic Close Button       1816.5,10.7→1891.5,85.7        UI_Button_Round_background 237²
│  │  └─ Icon                    1825.8,21→1882.2,75.5          40k_bt_close
│  ├─ Free feedback              293.8,640.6→1626.2,870.6       **出厂 inactive**
│  │  ├─ Input field             400.5,640.6→1519.5,893.3       InputFieldBackground · 九宫 10 · ppu=200 · alpha .5725
│  │  │  ├─ Text Area (RectMask2D pad −8,−5,−8,−5)
│  │  │  │  ├─ Placeholder        'Enter text...' · fs33 · Left/Top
│  │  │  │  └─ Text               '\u200b'
│  │  │  ├─ Input Field title     'Let us know your comments and feedback' · fs38
│  │  │  └─ Character Counter     '280/500' · fs24.7 · Right/Bottom
│  └─ Scroll Rect                70.9,211.6→1836.4,885.3        1765.5×673.64 · ScrollRect h=0 v=1
│     │                          m_MovementType=1(Elastic) · m_Inertia=1 · m_Elasticity=0.1 · m_Decel=0.135
│     │                          RectMask2D: m_Padding=(0,0,0,0) · m_Softness=(0,50)
│     └─ Questions container     86.4,211.6→1831.4,6638.6       1745×**6426.93** · VLG spacing16 · pad 0,0,8,0
│        ├─ Section 1  86.4,219.6→1831.4,754.2       （S1Q1 · 5 选项）
│        ├─ Section 2  86.4,770.2→1831.4,2484.8      （S2Q1/S2Q2 评分 · S2Q3 11 选项 · S2Q4 开放）
│        ├─ Section 3  86.4,2500.8→1831.4,3700.1     （S3Q1–Q9 评分 · S3Q10 4 选项）
│        └─ Section 4  86.4,3716.1→1831.4,6638.6     （S4Q1 2 · S4Q2 6 · S4Q3 6 · S4Q4 7 选项 + 3 道开放）
```

问卷明细（prefab 逐题实读；`isRequired` 全部为 1，`maxSelectedOptions` 见括号）：

| 题 | 类型 | 选项 / 钮 | 行 rect |
|---|---|---|---|
| S1Q1 年龄 | 多选(1) | 5：`-18` `18-24` `25-34` `35-44` `45+` | 块 219.6→754.2 · 表头 80 · 行 75.64/间距10 |
| S2Q1 上手难度 | 评分 | 5 颗 | 850.2→912.5 |
| S2Q2 40K 熟悉度 | 评分 | 5 颗 | 1028.5→1090.8 |
| S2Q3 常玩游戏类型 | 多选(3) | 11：CCG / Strategy / Shooter / Action / RPG / Racing / Simulation / Puzzle / Sport / Tabletop / Casino | 1100.8→2147.6 |
| S2Q4 喜欢与不喜欢 | 开放 | 文本框 220 高 | 2157.6→2484.8 |
| S3Q1–Q9 「我在寻找…」 | 评分 ×9 | 各 5 颗 | 2580.8 起，步长 72.28 |
| S3Q10 CCG/Midcore… | 多选(1) | 4：Hardcore / Midcore / Casual / Lore | 3231.3→3700.1 |
| S4Q1 是否弃坑过 | 多选(1) | 2：Yes / No | 3716.1→3984.3 |
| S4Q2 为什么弃坑 | 多选(全选) | 6 | 3994.3→4595.5 |
| S4Q2_Extra 其他 | 开放 | 文本框 220 | 4605.5→4932.7 |
| S4Q3 为什么回来 | 多选(全选) | 6 | 4942.7→5556.7 |
| S4Q3_Extra 其他 | 开放 | 文本框 220 | 5566.7→5893.9 |
| S4Q4 回归奖励 | 多选(3) | 7：BoosterPacks / PrebuiltDecks / HardCurrency / PremiumCosmetics / PremiumCampaings / Wildcards / OtherCurrencies | 5903.9→6638.6 |

**钮数实读**：多选 **41 颗**（5+11+4+2+6+6+7）＋ 评分 **9 题 × 5 = 45 颗** = **86 颗**。
（任务简报写的「42 颗」与 prefab 不符 —— 本条已按 prefab 记。）

行为（`GiveFeedbackMenuDemo__*.c`）：`Awake` 收全部题组件 → `Start` 挂 Submit/Close 且
`submit.interactable = 0` → `Open` 首句「已提交过 ⇒ 直接 `Close()`」→ `OnAnswer` = `All(answered)` →
`Submit` = 收答案 + `ShowPopUp(thanks…, close…)` + `SendFeedbackEvent` + `UploadOnlineDemoData` + `Close`。

---

## 三、改了什么（文件清单）

| 文件 | 改动 |
|---|---|
| `Shell/RankedDivisionChangeWindow.cs` | **新建**。`GameWindow` 子类；队列带 3410–3419；树 / 文案 / 印玺 / Blink 全照 §2·1。`Context{ DivisionLabel, Rating, SealCurrent, SealTotal }` + `SetSeals()`。服务端缺口出声。 |
| `Shell/RankedNewSeasonWindow.cs` | **新建**。队列带 3420–3429。`Context{ EndUtc }` + `Initialize(DateTime?)`（原版同名方法）；`--` 兜底 + 出声。 |
| `Shell/RankedSeasonEndedWindow.cs` | **新建**。队列带 3430–3439。🔴 `type = 0 Fullscreen`；`canvasGroup` 照 prefab 挂（alpha 1）；`Context{ Score, Percent, Position, DivisionLabel }`；`--` 兜底 + 出声。 |
| `Shell/GiveFeedbackPopup.cs` | **新建**。队列带 3440–3449。窗壳 + 九宫底 + 平铺底纹 + 标题/副标题 + 提交/关闭 + 出厂关着的 `Free feedback` + `Scroll Rect`（`ViewportClip.Hang`，softness (0,50)）+ 4 段问卷（数据表 `Sections[4] / Item[]`）。交互：多选（含 `maxSelectedOptions` 上限）/ 评分单选 / 重置 / 必答判定 / 提交（本地校验 + 感谢弹窗 + 关窗，**上报不实现**）。 |
| `Shell/WindowsManager.cs` | 加 4 条 `PrefabRef*` + 4 个 `Open*` 方法（照 `OpenItemDisplay` 的形状：`OpenByRef` + `Show(ctx)`；注释写明「本地没有生产入口」）。 |
| `Editor/MainMenuScene.cs` | `Run()` 末尾加一节 **`T18 收尾：排位赛季三窗 + 反馈问卷`**（27 条断言，见 §四）。 |

---

## 四、断言（`Editor/MainMenuScene.cs` 新增一节）

每扇窗：开得出来（`CurrentState == Open`）→ 窗参逐条 → 关键节点矩形/文案/图 → `Close()`。
期望值**独立取**：文案走 `Loc.HasEntry ? Loc.T : Loc.Lang13Of`（⛔ 不调被测窗的 `OfficialTerm`）；
矩形/图名来自 prefab `menu_dump` 实读的字面量。

| # | 断言 | 判据 |
|---|---|---|
| 1 | 升段窗开出来 | `OpenRankedDivisionChange` |
| 2–5 | `type=1` / `placement=15` / `closeOnESC=1` / `extraScale=1.0` | 根 MB |
| 6 | `Menu Dark Background` = 4574.60×2572.36 | dump |
| 7 | `Title` = 词条 `Event_Description/Ranked/Promoted` | prefab mTerm |
| 8 | `New division reached Text` = `--`（无段位名） | 服务端缺口 |
| 9 | 段位大图**不画**（`Division Image` 子树 0 个 `ImageQuad`） | `m_Sprite = 0` |
| 10 | `steps` = 4 颗 `RankedSealStep` | prefab |
| 11 | 新赛季窗开出来 | `OpenRankedNewSeason` |
| 12–14 | `type=1` / `placement=15` / `closeOnESC=1` | 根 MB |
| 15 | `Title` = 词条 `MainMenu/RankedWindow/NewRankedSeason` | prefab mTerm |
| 16 | `Timer` = `Ends in: --`（`{0}` 填 `--`） | 服务端缺口 + 词条 |
| 17 | `Timer Icon` 图 = `WF_icon_clock` | prefab `m_Sprite` |
| 18 | `Timer` 容器宽 = 1496.98 | dump |
| 19 | 赛季结束窗开出来 | `OpenRankedSeasonEnded` |
| 20 | 🔴 `type = 0`（Fullscreen） | 根 MB |
| 21–22 | `placement=15` / `closeOnESC=1` | 根 MB |
| 23 | `Title` = 词条 `Event_Description/Renked/SeasonEnd` | prefab mTerm |
| 24 | 名次/百分比 = `--` · 评分 = `--` | 服务端缺口 |
| 25 | `GameMode Icon` 图 = `40k_UI_icon_ranked_Skirmish` | prefab `m_Sprite` |
| 26 | `canvasGroup.alpha = 1` | CanvasGroup JSON |
| 27 | `Rank Points` 宽 = 1444.31 | dump |
| 28 | 反馈窗开出来 | `OpenGiveFeedback` |
| 29–31 | `type=1` / `placement=15` / `closeOnESC=1` | 根 MB |
| 32 | `Title` = 词条 `Demo/Feedback/Title` | prefab mTerm |
| 33 | `Subtitle` = prefab 出厂英文（本地无译文） | prefab `m_text` |
| 34 | `Submit` = 词条 `Demo/Feedback/Submit` | prefab mTerm |
| 35 | `Generic Popup Background` = 1824.95×996.86 | dump |
| 36 | `Questions container` 高 = 6426.93 | dump（CSF 后） |
| 37 | 问卷 = 4 段 | prefab |
| 38 | 滚动出厂偏移 = 0 | ScrollRect |
| 39 | 可滚（MaxOffset > 5000，实测 ≈5753） | 内容 6426.93 vs 视口 673.64 |
| 40 | `Free feedback` 出厂 inactive | prefab `m_IsActive=false` |
| 41 | 提交钮出厂**不可用** | `Start()` 的 `interactable=0` |

---

## 五、没查清 / 与任务简报不符（⛔ 没编）

1. 🔴 **反馈问卷的词条表本地没有**（与简报「24 条键在 `Demo/FeedbackSurvey/*` 官方表里」不符）：
   - `grep -c FeedbackSurvey Assets/CardPresentation/Resources/official_loc_13lang.json` = **0**（9447 键）；
   - `Doc/FeedbackSurvey` 在 `Core/Loc.cs` 的手写表里也是 **0**；
   - `grep -rl "FeedbackSurvey/S1Q1" d:/2` 只命中 **这个 prefab 自己**（`MonoBehaviour_3873017697202655694.json`）。
   ⇒ 处置：**题干**用 prefab 的英文 `m_text`；**答案**显示 **option id**（`CCG` / `18-24` …）+ 出声一次。
   **没有中文译文可抄，也不自译。** 已知受影响：`Subtitle`、`S1Q1/S2Q1…` 等 24+ 条 `mTerm`、全部答案文案。
2. 🔴 **「42 颗钮」与 prefab 不符**：实为 **41 颗多选 + 45 颗评分 = 86 颗**（§2·4 表）。
3. **文本输入层不存在**：工程没有 IME/键盘输入 ⇒ `Free feedback/Input field` 与 3 道开放题只画框和占位字，
   **不接受输入**；因此**开放题不参与必答判定**（实现里出声一次，报告在此标注为**有意偏离**）。
4. **上报不实现**：原版 `Submit → UploadOnlineDemoData → PlayFab`，本地没有后端 ⇒ 只做本地校验 + 感谢弹窗 +
   关窗，并在 `Debug.LogWarning` 出声。
5. **粒子未接**：升段窗 `Particles DivisionUp`（5 个 `ParticleSystem`）与赛季结束窗 `Glow below` 只建节点 /
   出声 —— 粒子归**特效线**，不在本白名单内。
6. **段位图名对照表本地没有**：`RankedDivisionsSO` 的「段位号→图名」不在本机 ⇒ 即使调用方给了段位名，
   段位大图**仍然不画**（出声说明）。`Resources/Art/ui_menu/01-Rook.png` 等 13 张段位图存在，但**没有
   id→图名**的映射，⛔ 不猜。
7. **问卷的 `MilestonesStepDisplay` 运行期扩颗**：prefab 只有 4 颗模板；服务端 `TotalSeals` 大于 4 时原版会
   动态加颗，我们只按 4 颗填（`SetSeals` 会夹到 4）。这是**已知未复刻的一处**（需要服务端数据才谈得上）。
8. **`RankedSeasonEndedWindow` 的 `DOFade` 淡入**未播（原版 `Open()` 末尾 `canvasGroup.DOFade(...).From(...)`）；
   画面上出厂即不透明 —— 如实标注，未自行编动画时长。

---

## 六、typecheck 原文

```
$ bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（按用户口径：**未跑 `*.Run`、未起 player**。）
