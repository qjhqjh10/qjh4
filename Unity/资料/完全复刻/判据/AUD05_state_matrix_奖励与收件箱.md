# AUD05 · 状态 × 参数矩阵 ——【第二窗】**奖励窗族（四扇）**

> 代理：`B103_A05-W3`（只读查判据） · 日期：2026-10-30 · 只读 `d:/2` 与 `Unity/MyGame/Assets`，**未跑 Unity、未跑 typecheck、未动 git、未改任何 `.cs`/账/正本**。
> 触发口径：铁律 5·c「**一个值 ≠ 全部情况**」；模板 = `判据/AUD05_state_matrix_卡组编辑.md`（结构/口径/红线照它）。
> **本文件只覆盖这四族窗**：① `Reward Window`（`RewardWindow`）② `Campaign Reward Window`（`CampaignRewardsWindow`）③ `Daily Reward Popup`（`DailyRewardPopup`）+ `Daily Streak Popup`（`DailyStreakWindow`）④ `Inbox Menu`（`InboxWindow`）+ `Rewards Base Submenu Variant`（奖励子菜单壳）。
> **不重做**卡组编辑窗；通用结论（`smallScreenUI`、CanvasScaler 1920×1080 **Expand**、`GameWindow.extraScaleSmallScreen` 恒 1）直接引用 `AUD05_state_matrix_卡组编辑.md` §1·2 / §附录B。

**出处缩写**（全表通用）：
`GO/名` = `D:/2/新解包资源/assets_full/bundle_menus_assets_all/GameObject/<名>.json` · `RT/<pid>` = 同目录 `RectTransform/RectTransform_<pid>.json` · `MB/<pid>` = 同目录 `MonoBehaviour/MonoBehaviour_<pid>.json` · `MS/` = `bundle_Waprforge_monoscripts/MonoScript/` · `DF:` = `D:/2/tools/decomp_full/` · `CP:` = `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/` · `CFG:` = `CP:Everguild.LiveOps/`。
所有「px」= **画布参考单位**（CanvasScaler ref 1920×1080），未乘任何窗口缩放。

---

## §0 结论摘要（先看这 5 行）

| # | 窗 | 查出几种状态 | 判 |
|---|---|---|---|
| 1 | **`Reward Window`** | **4 个独立开关**（`IsPreview` / `IsPremiumLocked∧有高级档` / `OnCollect==null` / 逐件抽屉三跳）+ 1 条**宽屏档**（遮罩起始 950→1250） | ✅ 我方 5 处**全对**；唯一 `?` = 逐件 `DrawerOverride` 与 `GetOwnedCount` 的映射（§6·1） |
| 2 | **`Campaign Reward Window`** | **6 个开关**（两列显隐 / 单列居中 / 已领两枚 / 可点 / 点数 / 高级锁）⇒ 两列 × 3 钮态 × 2 警告态 | ✅ 公式与判定顺序**全对**；`IsPremiumLocked` 我方**恒 false**（用户 2026-09-22 裁定全解锁） |
| 3 | **`Daily Reward Popup` + `Daily Streak Popup`** | DRP：**4 态抽屉 × 5 件** + 进度条「只在 index>0」+ `progressStatusImage` **换图** + 侧栏 2 态 + 计时 2 态；Streak：**2 面板互斥** + 格数 `cur+7−collected` | 🔴 **缺 4 档**（§4·3）：`Extra Reward Indicator`、抽屉空档整颗隐藏、`MainMenu/DayIndicator` 词条、连登格数公式 |
| 4 | **`Inbox Menu` + `Rewards Base Submenu Variant`** | Inbox：**空/非空** + 行 3 态（未读/选中/悬停）+ 正文 5 态（无奖励/可领/已领/未到点/阵营锁）；Rewards 壳：**4 键**（第 4 键运行期隐藏）+ 页签选中 + 3 颗红点 | 🔴 **缺 2 档**：领奖钮**倒计时文本**、`rewardWarningText`（阵营/扩展未解锁 —— 我方全解锁 ⇒ 恒不命中） |

> ⚠️ **口径提醒**：本文件回答「**哪个状态对应哪套参数**」，**不回答「我们画得对不对」**（那要靠并排实拍 = 调度台验收）。

---

## §1 权威公式 / 布局来源

### 1·1 全族共用（四扇窗都吃这几条）

| 项 | 值 | 出处 |
|---|---|---|
| CanvasScaler | `ScaleWithScreenSize` · ref **1920×1080** · `ScreenMatchMode = Expand(1)` · `MatchWidthOrHeight = 0` | `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2577.json`（同 `AUD05_R2` §2） |
| `extraScaleSmallScreen` | **四扇窗全 = 1.0** | `MB/-1684643839753787023`(Reward) · `MB/7664330643585539206`(Campaign) · `MB/-1697206695437574193`(DailyReward) · `MB/8654310213240890027`(Streak) · `MB/-4166270025616161998`(Inbox) · `MB/1349677669050291967`(Rewards) |
| ⇒ 小屏档 | **整支不命中**：`GameWindow__Open` 里那条缩放器分支的判据含 `max(extraScale,1)` 与 `1−extraScale` 的比较，`extraScale = 1` 时 `1−1 = 0` ⇒ **不加 `TransformScalerBySmallScreenUI`** | `DF:GameWindow__Open.c:22-64`（同一分支内 `TransformScalerBySmallScreenUI__SetScale` 只在命中时被调） |
| 压暗层 | `Menu Dark Background`：`Image` **无 sprite**、色 **(0,0,0,0.773)**、`sz=(4574.6,2572.36)`、`anchor=pivot=(.5,.5)` | 四窗 prefab 逐读一致（Reward/Campaign/Daily/Streak/Inbox 全有） |
| 窗底渐晕 | `Menu Vignette`：`Image` 无 sprite、**(0,0,0,0.58)**、`type=Sliced`、铺满 | `GO/Reward Window.json`、`GO/Campaign Reward Window.json`（Daily/Streak 无此节） |

### 1·2 `Reward Window` —— 布局与揭示动画

- 内容根 `Content`：`a=(0,.5)-(1,.5) p=(.5,.5) pos=(0,−25) sz=(0,800)` ⇒ 实检 **x 0..1920 · y 165..965**。
- `Scroll View` / `Viewport` **同矩形**：`a=(0,0)-(1,1) pos=(0,−51.26) sz=(0,−167.5)` ⇒ **x 0..1920 · y 300..932.5**（632.5 高）。
- `Content`（= `listHolder`）上的 `HorizontalLayoutGroup`：**pad L/R 60 · T 30 · B 50 · spacing 40 · align=4(MiddleCenter) · ctrlW=0 · ctrlH=1 · forceW=0 · forceH=1**。
- `Viewport` 的 `RectMask2D`：`m_Padding=(0,0,0,0)`、**`m_Softness=(200,0)`**（左侧 200px 软边）。
- **揭示动画（`DoRewardAnimation`，仅非预览态）**——三个字面量全是读出来的：

```c
// DF:RewardWindow__DoRewardAnimation.c:54-81
local_a8  = _DAT_1834b32e0;   // (950, 0, ...)     ← 0x1834b32e0 / 0x1834b32e4
uStack_a0 = _UNK_1834b32e8;   // (950, 0, ...)     ← 0x1834b32e8 / 0x1834b32ec
if (W/H > 1.77778) {          // 0x1834b32d0
    t = clamp01((W/H − 1.77778) / 0.555555);      // 0x1834b32cc
    x = z = t * 300 + 950;                        // 0x1834b326c = 300, 0x1834b32d4 = 950
}
RectMask2D.padding = 上述 Vector4;
DOTween.To(padding → zero, animTime = 0.8, Ease.Linear);   // MB animTime = 0.800000011920929
```

⇒ **遮罩从「左右各 950（宽屏最多 1250）」线性收到 0**，时长 **0.8 s**。`16:9` 与 **`21:9`**（t=1）是两个端点，中间线性。
- 逐件 punch：`DOPunchScale(t, 0.4f, 5, …)`、延迟 `normalize = |drawer.x| / viewport.x` 夹 `[0,1]`、`delay = normalize × 0.75`、`*0.5` 那截见 §6（不影响状态矩阵）。
- `Collect Button`：`a=(.5,0) p=(.5,.5) pos=(0,25) sz=(245,75)` ⇒ **x 837.5..1082.5 · y 902.5..977.5**（底边**压出 `Content` 下沿 12.5px**，原版如此）。
- `Title`：`a=(.5,1) p=(.5,**1**) pos=(0,−28.3) sz=(600,75)` ⇒ **pivot 在顶边** ⇒ x 660..1260 · y **193.3..268.3**（⚠️ 别按 pivot=0.5 算，会得到 155.8..230.8）。
- `Premium Disclaimer`：`a=(.89,.05) p=(1,.5) sz=(0,80)` ⇒ 右沿 **1708.8** · 竖 **885..965**。
- `Tap To Continue`：`a=(.5,0) p=(.5,**1**) sz=(768,80)` ⇒ **x 576..1344 · y 965..1045**（**在 `Content` 之下**、压到屏幕底边外 35px）。

### 1·3 `Campaign Reward Window` —— 两列与居中

- `Content`：`a=(0,.5)-(1,.5) pos=(0,−25) sz=(0,800)` ⇒ **y 165..965**（同 Reward）。
- `Scroll View`：`a=(0,0)-(1,1) pos=(0,−45) sz=(0,−150)` ⇒ **y 285..935**（650 高）；`Viewport` 同矩形、`RectMask2D soft=(200,0)`。
- 两列（`Scroll View/Viewport/Content` 下）：
  · `Base Rewards`：`a=(0,0)-(0.5,1) p=(1,.5)` ⇒ **x 0..960**；
  · `Premium Rewards`：`a=(0.5,0)-(1,1) p=(0,.5)` ⇒ **x 960..1920**。
- 每列内部 `Rewards`（挂 HLG 的那一层）：`pad 30/30/25/85 · spacing 25 · align 4 · forceW/H=1`；基础列 `a=(1,0)-(1,1) p=(1,.5) pos=(−65,7.5) sz=(0,−35)`（贴列右），高级列 `a=(0,0)-(0,1) p=(0,.5) pos=(65,7.5)`（贴列左）。
- **`CenterHolder(holder)`**（`DF:CampaignRewardsWindow__CenterHolder.c`）：把 **holder 及其父** 的 `anchorMin/Max/pivot/anchoredPosition` **全置 0** 再 `ForceRebuildLayoutImmediate` ⇒ 单列时该列居中。

### 1·4 `Daily Reward Popup` —— 抽屉状态公式

```csharp
// CP:DailyRewardItemContainer.cs:181-232（逐字）· CFG 同式
normal  = (index < progress.collectedRewards)
            ? Collected
            : (milestone.targetValue <= progress.CurrentValue ? Unlocked : Locked);
premium = !scoringEvent.IsPremium                                  ? PremiumLocked
        : (normal == Collected && progress.premiumCollectedRewards <= index) ? Unlocked
                                                                            : normal;
```

- `SetState(state)` 是**唯一开关**（`CP:DailyRewardDrawerController.cs:339-368`），五件表见 §3·3。
- 另有两条**只在 `Initialize` 里设、`Refresh()` 不管**（`CP:DailyRewardItemContainer.cs:127-141`）：
  · `progressSlider.gameObject.SetActive(index > 0)`（第 0 天没有进度条）、`min=0 max=1 wholeNumbers=true`、`value = (state != Locked) ? 1 : 0`；
  · `progressStatusImage.sprite = (normal == Locked) ? progressStatusOff : progressStatusOn` —— **换图，不是显隐**（两张图在 MB 字段里：`40k_missions_milestone_off` / `_on`）。
- 序号/文案：`dayText = string.Format(Loc("MainMenu/DayIndicator"), ★)`、`targetValueText = milestone.targetValue.ToString()`（`★` 取不到，见 §6·4）。

### 1·5 `Daily Streak Popup` —— 两态 + 格数

```csharp
// CP:DailyStreakWindow.cs:85-137（Open）/ 139-228（RefreshRewards）
streakFailedPanel.SetActive(HasFailed);                 // 0xC0
if (HasFailed) streakLost.text = string.Format(Loc("Rewards Menu/DailyStreak/StreakLost"), FailedValue);
streakSuccessPanel.SetActive(!HasFailed);               // 0xB0
/* m.HasFailed = false;  ← 一次性消费（第二次 Open 必进连胜态） */
…
currentStreakText.text = progress.currentValue.ToString();
for (int i = progress.collectedRewards; i < progress.currentValue + 7; i++) {   // ★ 格数 = cur + 7 − collected
    var item = Instantiate(dailyStreakItemContainer, rewardContentAnchor);
    if (i == progress.collectedRewards) item.transform.localScale *= scaleMultiplierFirstElement;  // 1.2（在条目自身 1.2 之上 ⇒ 1.44）
    item.Initialize(milestone.rewards,
        (progress.currentValue >= milestone.targetValue) ? Unlocked : Locked, CollectRewardClicked);
}
```

- **条目自身 `scl=(1.2,1.2)`**（`RT/-7087434954534740863`）与 `scaleMultiplierFirstElement=1.2` **相乘** ⇒ 首格 **1.44**。
- 条目 `sz=(379.816 × 516.301)`；`Rewards Content` HLG：`pad L2/R0/T58/B0 · spacing −64 · align=3`。
- `Rewards Scroll View` 的 `ScrollRect`：**`m_Enabled=0`、`h=0`、`v=0`**（⇒ 用户**不能滚**，位移只有 `FocusOnItem` 一条路；我方已按此不登记指针层）。

### 1·6 `Inbox Menu` —— 空/非空 + 可领式

```csharp
// CP:Everguild.LiveOps/InboxWindow.cs:64-291（Open）
resetButton.gameObject.SetActive(false);                       // 永远先关
messages = handler.events.OfType<IInboxEvent>().Cast().ToList();
if (messages == null || messages.Count < 1) {                  // ★ 空态
    noNewsWarning.SetActive(true);
    display.gameObject.SetActive(false);                       // 关的是 Message Display，不是列表
} else {                                                       // ★ 非空
    noNewsWarning.SetActive(false);
    cur = (data as IGameNews) ?? GetCurrentMessage();          // 最后一条「已读」，否则第一条
    cur.SelectEvent();                                         // 接口 slot 6
    display.Initialize(cur); list.Initialize(messages, cur);
    list.OnMessageSelected += OpenMessage;
}
```

```csharp
// CP:Everguild.LiveOps/MessageDisplay.cs:200-289（CheckMessageCollectable）
if (save == null) { …NRE… }                     // 原版会抛，我方不照抄
if (claimedAt > 0 || !hasReward)          return false;
if (RedeemableAt() > ServerNow)           return false;
if (IsArmyHidden(army) || IsExpansionHidden(exp)) { rewardWarningText.SetActive(true); return false; }
return true;
```

```csharp
// CP:Everguild.LiveOps/MessageDisplay.cs:37-78（Update，每帧）
if (!hasReward) return;
if (claimedAt <= 0) {
    collectButton.interactable = CheckMessageCollectable();
    collectButton.text = (RedeemableAt > now) ? GetTimeString(RedeemableAt − now, eventTimer:true)
                                              : Loc("MainMenu/General/Claim");
} else collectButton.interactable = false;
```

### 1·7 `Rewards Base Submenu Variant` —— 页签机制

- `TabButtons.Initialize` **第一件事**：`tabButtonPrefab.gameObject.SetActive(false)`（母版 = 左栏第 4 键）⇒ **第 4 键恒不可见**（`CP:TabButtons.cs:118-121`）。
- 选中态：`EverguildToggle.graphic` = 该键 `Highlight` 的 `Image`、`toggleTransition=1`（淡入淡出）；`m_TargetGraphic` = `Icon`；四键出厂 `m_IsOn` **全 0**、`onColor/offColor` 不生效（`go/Rewards Base Submenu Variant` 逐键 MB）⇒ **「谁被选中」只能由运行时决定**（§二·3 已记，本条复核一致）。
- 运行时**加页签**：`TabButtons.AddTabButton(name, icon)` → `Instantiate(tabButtonPrefab, group)` + `SetActive(true)` + `SetSiblingIndex(0)`（`CP:TabButtons.cs:314-345`）⇒ **动态页签插在最前**（不是末尾）。
- 红点：每键一颗 `UiBadgeNotification`（`showCounter=1`），定义逐键不同（§2·6）。

---

## §2 各窗 prefab 序列化值（**未乘任何系数的那一档**）

> 一张表一扇窗。`act` = `m_IsActive` 出厂值（**不是运行时最终值**，运行时几乎全被上面的代码覆盖）。

### 2·1 `Reward Window`（`GO/Reward Window` · `RewardWindow` MB `-1684643839753787023`）

| 字段 | 值 |
|---|---|
| `type` / `windowsPlacement` / `closeOnESC` / `updateNavPanel` | **1(Popup)** / **15** / **1** / **0** |
| `extraScaleSmallScreen` / `animTime` | **1.0** / **0.8** |
| `openSound` / `closeSound` / `useDefaultCloseSoundIfNull` | null / null / **1** |
| 引用 | `listHolder`→`Scroll View/Viewport/Content` · `closeButton`→压暗层 `BackgroundCloseButton` · `collectButton`→`Collect Button` · `premiumWarning`→**`Premium Disclaimer` 那颗 TMP 自身** · `tapToContinueText`→`Tap To Continue` · `mask2D`→`Viewport` 的 `RectMask2D` · `scrollRect`→`Scroll View` · `particleOnAppear`→(模板) · `soundOnAppear`→外链 AudioCue |
| 无字段读取的 context 项 | `CollectedRewards` / `CratePrefab` —— `Open()` **一次没读**（§5·5 N-1） |

| 节点 | act | 关键值 |
|---|---|---|
| `Menu Dark Background` | T | `Image` 无 sprite · (0,0,0,0.773) |
| `Content` | T | `a=(0,.5)-(1,.5) pos=(0,−25) sz=(0,800)` |
| ├ `Reward Background Get Reward` | **T** | `40k_general_popup_simple red` · Sliced |
| ├ `Reward Background Preview Reward` | **F** | `40k_general_popup_simple greyscale` · Sliced |
| ├ `Scroll View` | T | `ScrollRect h=1 v=0 mode=1 inertia=1 elasticity=0.1 sens=1.0` · `Image` α=0 |
| │ └ `Viewport` | T | `RectMask2D pad=0 soft=(200,0)` |
| │　└ `Content` | T | HLG 60/60/30/50 · spacing 40 · align 4 · ctrlH=1 · forceH=1 |
| ├ `Title` → `Glow Get reward` | **T** | `40k_bt_underbutton` (0.802,0.0794,0.0794) · PA=1 |
| ├ `Title` → `Glow Preview reward` | **F** | 同图 (0.528,0.356,0.356) |
| │ ├ `Text Get Reward` | T | TMP **'Rewards claimed' fs 50** auto 25→50 · H=2 |
| │ └ `Text Preview Reward` | T | TMP **'You will get' fs 50** auto 25→50 · H=2 |
| ├ `Collect Button` | T | `UI_Button_Mulligan` · `EverguildButton m_Transition=2(SpriteSwap)` · HL/Pressed sprite 齐 |
| │ └ `Button Text` | T | TMP **'Collect' fs 40** auto 10→40 · `AspectRatioFitter` |
| ├ `Premium Disclaimer` | T | TMP **'Upgrade to premium to unlock' fs 36** · autosize **0** · H=4 · `ContentSizeFitter` |
| │ └ `Premium Icon` | T | `40k_campaign_Premium-icon` 78.55² |
| ├ `Tap To Continue` | T | TMP **'Click to continue' fs 55** · `BlinkGraphic` |
| └ `Menu Vignette` | T | 无 sprite (0,0,0,0.58) Sliced |

### 2·2 `Campaign Reward Window`（`GO/Campaign Reward Window` · MB `7664330643585539206`）

| 字段 | 值 |
|---|---|
| `type` / `placement` / `closeOnESC` / `updateNavPanel` / `extraScale` | **1** / **15** / **1** / **0** / **1.0** |
| `premiumUnlockWarning` / `premiumExtraTip`（`LocalizedString`） | **`Rewards Menu/PremiumUnlockWarning`** / **`Rewards Menu/PremiumExtraTip`** |
| 引用 | `baseRewardsHolder` / `premiumRewardsHolder` / `baseUnlockButton` / `premiumUnlockButton` / `premiumWarning`(TMP) / `layoutFixer` / `closeButton` |

| 节点 | act | 关键值 |
|---|---|---|
| `Reward Background Get Reward` | **F** | red（与 Reward 窗一致，但**出厂反着**） |
| `Reward Background Preview Reward` | **T** | greyscale |
| `Base Rewards` / `Premium Rewards` | **T / T** | 两列各半（§1·3） |
| 列内 `Rewards` | T | `UI_Deck_Information_submenu_Back` · HLG 30/30/25/85 · spacing 25 · forceW/H=1 |
| `Unlock Button` | T | `UI_Button_Mulligan` 245×45 · `CampaignUnlockButton`（`claimedKey=Rewards Menu/Claimed` · `claimKey=MainMenu/General/Claim`） |
| ├ `Icon Campaign Points Drawer Variant` | **F** | `CampaignPointDrawer`（付费档才有） |
| ├ `Point Count` | **F** | TMP '100' **fs 32.3** auto 10→32 · H=1 |
| ├ `Claimed Text` | T | TMP 'Change Deck' **fs 35** auto 10→35 |
| └ `Warning`（= `premiumWarning`） | **F** | TMP 'Warning or Tip' **fs 47.5** auto 18→30 · H=2 |
| Premium 列 `Badge` | T | `40k_campaign_Premium-icon` 100² · PA=1 |
| `Title` → Get/Preview glow | **F / T** | 同 Reward 窗；文本 **'Campaign Rewards' / 'Available rewards'** fs 50 |
| `Tap To Continue` | **F** | 'Click to continue' fs 55 |

### 2·3 `Daily Reward Popup`（`GO/Daily Reward Popup` · MB `-1697206695437574193`）

| 字段 | 值 |
|---|---|
| `type` / `placement` / `closeOnESC` / `updateNavPanel` / `extraScale` | **0(Fullscreen)** / **15** / **1** / **0** / **1.0** |
| 引用 | `header`(ArmyHeader) · `armySelector` · `rewardsTrack`(DailyRewardSelector) · `closeButton` · `trackSidePanel` · `timerText`(TimerDisplay) |

| 节点 | act | 关键值 |
|---|---|---|
| `Menu Dark Background` → `Image` | **F** | 1920×1080 · 无 sprite · (1,1,1,0.651) —— **谁开它 = 查不到**（§6·3） |
| `Tracks` | T | `a=(0,.5)-(1,.5) pos=(129.3,−18.89) sz=(−203.4,812.1)` |
| `Tracks/bg` | T | 无 sprite + `UIGradient` 1676×399.5 |
| `Rewards Scroll View` | T | `ScrollRect h=1 v=0 mode=1` · `GraphicRaycaster` · `DailyRewardSelector` |
| └ `Viewport` | T | `RectMask2D soft=(0,0)` |
| 　└ `Rewards Content` | T | HLG `pad 2/0/58/0 · spacing −64 · align 3 · ctrlW/H=0` · CSF |
| 　　└ `Daily Reward Popup Entry` | T | **315.028 × 812.1**（模板，运行期按里程碑数实例化） |
| `Tracks Side Bar` | T | 260.5×812.1 · `DailyRewardTrackPanel` · `BG=UI_Login_Tracker` 290.7×824.7 |
| ├ `Free Track` / `Premium Track` | T / T | 各 236.2×252.4 · pos y **+202.5 / −202.5** |
| └ `Generic Round Button Variant` | T | 124² · `40k_UI_bt_back`（关闭） |
| `Army Selector` | T | `ScrollRect h=1 v=0` + `ArmySelector` + `CustomRaycaster`；`Viewport soft=(42,0)`；HLG spacing −14 · pad b5 |
| `Header Header` | T | `WF_Campaign_Info_Background` 604.8×152.8 · `ArmyHeader` |
| `Timer` | T | `TimerDisplay`，三件（'More Rewards In' / 钟图 / 时间） |
| 条目内 `NormalReward` / `Premium Reward` | T / T | 各 327.4×423.1 · **`scl=0.8`** · `DailyRewardDrawerController` |
| 　├ `Highlight` | T | `OctagonUI Border SDF 2` · α **0.6235** |
| 　├ `Extra Reward Indicator` | **F** | `40k_main_bt_rewards` 100² |
| 　├ `Shadow` | **F** | 同 BG 图 · (0.142,0.142,0.142,0.502) |
| 　├ `Premium Indicator` | T | `WF_Login_CornerBanner` + `WF Lock Icon Simple` |
| 　├ `Gacha Reward Claimed` | T | `WF_Special offer_Value` (0,0.9,0.29,0.95) + 'Claimed Tex' |
| 　└ `colider` | T | 同 BG 图 **α=0.00** + `EverguildButton`（**真点击区**） |
| `Personal Progression` | T | 100 高；`Progress Bar`（Slider，**出厂 F**）+ `Image`（= `progressStatusImage`）+ `Counter`（TMP fs36） |
| `Day Title` | T | TMP 'Day 3' **fs 48** auto **关** · H=2 · **scl 0.8** |

### 2·4 `Daily Streak Popup`（`GO/Daily Streak Popup` · MB `8654310213240890027`）

| 字段 | 值 |
|---|---|
| `type` / `placement` / **`closeOnESC`** / `updateNavPanel` / `extraScale` | **0(Fullscreen)** / **15** / **0（ESC 不关！）** / **0** / **1.0** |
| `scaleMultiplierFirstElement` | **1.2** |

| 节点 | act | 关键值 |
|---|---|---|
| `Menu Dark Background` | T | 无 sprite α 0.773（**无内层 Image**，与 Daily 不同） |
| `bg` | T | 无 sprite + `UICornersGradient` 812.1 高 |
| **`Streak Failed`** | **T** | 断签面板（出厂亮） |
| ├ `Daily Streak Broken` | T | TMP 'STREAK BROKEN' **fs 128.1** |
| ├ `Current Streak Lost count` | T | TMP 'Streak lost: 10' **fs 66.9** |
| ├ `Info` | T | fs 36（词条 `Rewards Menu/DailyStreak/Help`） |
| └ `Generic Simplified UI Button` | T | `UI_Button_Mulligan` 464.6×103 · 'Reset Streak' fs 55 |
| **`Streak Successful`** | **F** | 连胜面板 |
| ├ `Fill Line` | T | `40k_generial_bar_fill` (0.434,0,0.0567,0.624) · **scl 1.2** |
| ├ `Current Streak` → `Value` | T | 'Current streak:' fs 70 → 值 fs 80 |
| ├ `Rewards Scroll View` | T | **`ScrollRect m_Enabled=0 · h=0 · v=0`** · `Viewport RectMask2D soft=(89,0)` · HLG spacing −64 |
| ├ `Info` | T | fs 36 |
| └ `Timer` | T | `TimerDisplay` + 'More Rewards In' fs 36 + 钟 40² + 时间 fs 36 |
| `Header With Back Button` | T | 标题 'Daily Streak' **fs 67.55** · 返回钮 `UI_Button_Menu_Back` |
| 模板 `Daily Streak Reward Popup Entry` | — | **379.816 × 516.301 · scl 1.2**（独立 prefab 根） |

### 2·5 `Inbox Menu`（`GO/Inbox Menu` · MB `-4166270025616161998`）

| 字段 | 值 |
|---|---|
| `type` / `placement` / `closeOnESC` / `updateNavPanel` / `extraScale` | **1(Popup)** / **15** / **1** / **1** / **1.0** |

| 节点 | act | 关键值 |
|---|---|---|
| `Content` | T | **1775.86 × 1040** · pos (0,−20) · `UI_Deck_Information_Back` |
| `Title` | T | 'Inbox' **fs 48** auto 18→48 · H=1 |
| `Message List` | T | `ScrollRect h=0 v=1 mode=1` + `MessageList` · **628.707 × 778.798** |
| └ `Viewport` | T | `RectMask2D soft=(0,25)` |
| 　└ `Content` | T | VLG **spacing 7** · align 0 · forceW/H=1 · CSF v=1 |
| `Message Display` | T | `UI_Deck_Information_submenu_Back` · **1059.77 × 896.87** |
| ├ `Title` | T | fs 58 · color (0.953,0.663,0.404,1) |
| └ `Content/Scroll View/Viewport/Content` | T | VLG spacing 6 align 1 CSF v=1 |
| 　├ `Header` / `Footer` | T / T | `spr=null` PA=1 · **972.8 × 243.2**（运行期按 HeaderImage/FooterImage 贴图） |
| 　├ `Message` | T | TMP fs 45 · autosize **关** · wrap 1 |
| 　└ `Claim Button`(容器) | T | 958.86×216.21；内含 `Claim reward warning`（fs37）+ `Claim Button`(245×67.64, 'Claim' fs40) |
| `Generic Close Button Orange` | T | 74.386×75.605 |
| `Reset Button` | **T**（但 `Open()` 首句必关） | 141.9×70.8 · 'DEBUG RESET' fs 23.2 |
| `No News Warning` | T | TMP fs 50 · H=2 · 'Game announcements will be displayed here' |
| 条目模板 `Message Container`（**另一 prefab**） | T | **628.7 × 140.7**；`Content` 图 `40K_settings_button` 色 **(1,0.5724,0,1)** `trans=2`（HL `_hover` / P `_pressed`）；`Title` fs 50 · `Date` fs 40 · `New`='New!' fs 40 H=4；`normalMessageSprite=40K_settings_button` / `selectedMessageSprite=40K_settings_button_selected` |

### 2·6 `Rewards Base Submenu Variant`（MB `1349677669050291967` + `TabbedWindowComponents -4243648091049945345`）

| 字段 | 值 |
|---|---|
| `type` / `placement` / `closeOnESC` / `updateNavPanel` / `extraScale` | **0(Fullscreen)** / **5(Canvas)** / **0** / **1** / **1.0** |
| `tabs`（`GameWindowWithTabs`） | **3 项**：`MissionsTab` / `ForgeWindowTab` / `CampaignWindowTab` |

| 键 | act | 红点定义（`UiBadgeNotification.notificationDefinitions`） |
|---|---|---|
| `MissionsRewardsButton` | T | **`MissionsBadge`** · `useFilter=1` |
| `CampaignRewardsButton` | T | **`CampaignBadge`**(无 filter) + **`MissionsBadge`**(filter) |
| `Forge Button` | T | **`ForgeBadge`** |
| `Menu Navigation Panel Button`（母版，**运行期隐藏**） | T | **空数组** ⇒ 永不亮 |
| 四键 `OneText`（角标数字） | T | fs 34 · 纯白 · 初始 `''` |
| `Tabs/Missions Tab` / `Forge Tab` / `Campaign Tab` | **T / F / F** | — |

---

## §3 状态 × 参数矩阵（**主表**）

> 每格带出处；**没查到的写 `?`**，⛔ 不拿默认值顶替。四扇窗**分四张主表**（第三族一窗两表、第四族一窗一表 + 子菜单一表）。

### 3·1 表 A · `Reward Window`

| # | 状态 | 判定条件（代码） | 参数 / 显隐 | 出处 |
|---|---|---|---|---|
| **A1** | **预览态** | `ctx.IsPreview == true`（`RewardService.Preview(...)` → ctor `isPreview:1`） | `Reward Background Preview Reward` **T** / `Get Reward` **F**；`Glow Preview reward` **T** / `Glow Get` **F**；文案 = 'You will get'；`Tap To Continue` **F**；`claimRewardParticles` **F**；**不跑 `DoRewardAnimation`** ⇒ 遮罩 padding 停在 0（无揭示）；`Collect Button` **F**（Preview 的 ctor 传 `onCollect=null`） | `DF:RewardWindow__Open.c:180-310` · `CP:RewardService.cs:809-821` |
| **A2** | **领取态（默认）** | `ctx.IsPreview == false`（`RewardService.Collect(showAnimation:1)`） | `Get Reward` **T** / `Preview` **F**；文案 = 'Rewards claimed'；`Tap To Continue` **T**；`claimRewardParticles` **T**；跑 `DoRewardAnimation`（遮罩 950→0 / 0.8s + 逐件 punch） | 同上 + `DF:RewardWindow__DoRewardAnimation.c:54-81` |
| **A3** | 高级锁定 **∧** 有高级档奖励 | `ctx.IsPremiumLocked && Rewards.Any(r => r.rewardTier == Premium(10))` | `Premium Disclaimer` **T**；否则 **F** | `DF:RewardWindow__Open.c:160-186` |
| **A4** | **有领奖回调** | `ctx.OnCollect != null` | `Collect Button`（整颗）**T**，且 `interactable = true`；点一次后 `interactable=false`；为空时**整颗不显示** | `DF:RewardWindow__Open.c:189-200` · `CP:RewardWindow.cs:815-825` |
| **A5** | 自定义标题 | `!string.IsNullOrEmpty(ctx.CustomTitle)` | 把**当前正显示的那一颗**（preview 取 `previewRewardText`、否则 `getRewardText`）的 `Localize.Term` 改成该值 | `DF:RewardWindow__Open.c:270-300` |
| **A6** | 逐件：**高级档** | `r.rewardTier == Premium` | `ItemDrawer.TogglePremiumHighlight(true)`（该格右上角 premium 角标） | `DF:RewardWindow__Open.c:100-110` |
| **A7** | 逐件：**折换** | `r.convertedInto != null` | `SetConvertedItem(convertedInto)`（显示"换成的东西"），**优先于** A8 | `DF:RewardWindow__Open.c:112-132` |
| **A8** | 逐件：**限时** | `convertedInto == null && r.IsEphemeral` | `SetEphemeralDisplay(EphemeralState)`（时钟 + '24 hours'） | 同上（`RewardInfo.IsEphemeral` 判据 `CP:RewardInfo.cs:36+`） |
| **A9** | 逐件：**按数量展开** | `!ItemDrawerOptions.Stackable && convertedInto == null && Quantity > 1` | 同一条奖励**画成 `Quantity` 格**（每格都做 A6） | `DF:RewardWindow__Open.c:112-146` |
| **A10** | 逐件：**已拥有** | `!Stackable && convertedInto==null` 且 `InventoryManager.GetOwnedCount(item) …`（比较对象 `iVar12 < 2`） | 换 `DrawerOverride` 档（哪种**未查清**，§6·1） | `DF:RewardWindow__Open.c:150-158` |
| **A11** | 排序 | 始终 | `Rewards.OrderBy(r => r.rewardTier)` ⇒ **Basic(0) 在前、Premium(10) 在后** | `DF:RewardWindow__Open.c` 首段 |
| **A12** | **宽屏档** | `Screen.W/H > 1.77778` | 遮罩起始 `pad = 950 + clamp01((a−1.77778)/0.555555) × 300`（21:9 ⇒ 1250）；`16:9` 及更窄 ⇒ 950 | `DF:RewardWindow__DoRewardAnimation.c:54-81` + `read_literal` 五个常量 |
| **A13** | 重复打开 | 领两次 | `RewardService.Collect` 先 `GetOpenWindow<RewardWindow>()` 有则 `Close()` 再开新的 ⇒ 场上恒 1 扇（我方 `Show` 也复用实例） | `CP:RewardService.cs:411-437` |

> **⛔ 死字段（照抄 = 不实现）**：`RewardWindowContext.CollectedRewards`（0x40）、`CratePrefab`（0x48）—— `Open()`/`DoRewardAnimation()` **零读取**（§5·5 N-1）。

### 3·2 表 B · `Campaign Reward Window`

| # | 状态 | 判定条件 | 参数 / 显隐 | 出处 |
|---|---|---|---|---|
| **B1** | **两列显隐** | `hasBase = Rewards.Any(tier==Basic)`；`hasPrem = Rewards.Any(tier==Premium)` | `baseRewardsHolder.gameObject.SetActive(hasBase)` · `premiumRewardsHolder…SetActive(hasPrem)`（**两列独立**，都可能关） | `DF:CampaignRewardsWindow__Open.c:60-100` |
| **B2** | **单列居中** | `hasBase xor hasPrem` | `CenterHolder(那一列)`：holder 与**其父**的 anchor/pivot/anchoredPos **全置 0** + 重排 | 同上 + `DF:CampaignRewardsWindow__CenterHolder.c` |
| **B3** | **预览 / 已领文案** | `stillToCollect` = `hasBase ? (!BaseCollected ? true : (hasPrem ? !PremiumCollected : false)) : true` | `true` ⇒ `Preview*` 亮 / `Get*` 暗；`false` ⇒ 反之 | `DF:CampaignRewardsWindow__Open.c:110-150` |
| **B4** | **只有高级列 ⇒ 恒预览** | `hasPrem && !hasBase` | `stillToCollect` **恒 true**（哪怕 `PremiumCollected=true`）—— **原版如此，⛔ 别"修好"** | 同上（`flag5` 初值 true 且 `!hasBase` 直接跳出口） |
| **B5** | 基础钮：**付点解锁** | `!BaseCollected && PointCost > 0` | `ToggleTexts(true)`（`pointDrawer`+`costText` **T**、`claimedText` **F**）· `costText = PointCost` · `pointDrawer.DrawForArmy(Army)` · `interactable = Claimable` | `DF:CampaignRewardsWindow__SetBaseButton.c` · `CP:CampaignUnlockButton.cs:70-79` |
| **B6** | 基础钮：**免费领** | `!BaseCollected && PointCost <= 0` | `SetAsFreeClaim(canUnlock: **true** 硬编码)` ⇒ `claimedText = MainMenu/General/Claim`、`interactable = **true**`（**不看 `Claimable`**） | 同上（`:38-40` 那个硬编码 `1`） |
| **B7** | 基础钮：**已领** | `BaseCollected` | `SetAsClaimed()` ⇒ `claimedText = Rewards Menu/Claimed`、`interactable = false` | `DF:CampaignRewardsWindow__SetBaseButton.c` |
| **B8** | 高级钮：**锁** | `!PremiumCollected && IsPremiumLocked` | `premiumWarning.SetActive(true)`（文本 = `Rewards Menu/PremiumUnlockWarning`）+ 高级 `Unlock Button` **F** | `DF:CampaignRewardsWindow__SetPremiumButton.c:20-45` |
| **B9** | 高级钮：**免费领** | `!PremiumCollected && !IsPremiumLocked` | 警告 **F** + 钮 **T** + `SetAsFreeClaim(canUnlock = **BaseCollected**)` ⇒ **基础档领了才点得动**（⛔ 与基础钮的门不同） | 同上 `:55-90` |
| **B10** | 高级钮：**已领** | `PremiumCollected` | 警告 **F** + 钮 **T** + `SetAsClaimed()` | 同上 `:5-18` |
| **B11** | 高级警告文本的 else 支 | `IsPremiumLocked == false` | `premiumWarning.text = premiumExtraTip` —— **此时节点已关**（`SetActive(false)` 在上一步）⇒ 屏幕零变化（死写） | 同上（§5 施工建议 2·F） |
| **B12** | 点钮后 | 任意钮点击 | `button.interactable = false`；回调 `OnCollect(tier)`（Basic/Premium） | `CP:CampaignUnlockButton.cs:60-68` |
| **B13** | 强制重排 | 尾句 | `layoutFixer.FixLayout()` | `DF:CampaignRewardsWindow__Open.c` 尾 |
| **B14** | **状态来源（上游）** | 由 `CampaignWindowTab.OnNodeClicked(node)` 组 context | `baseCollected = state ∈ {baseCollected(20), allCollected(30)}` · `premiumCollected = state == 30` · `claimable = points ≥ NodeCost && state ∈ {unlocked(10), repetable(40)}` · `isPremiumLocked = !campaign.IsPremium` · `pointCost = NodeInfo.NodeCost(0x18)` · `army = armySelector` 当前阵营 | `DF:CampaignWindowTab__OnNodeClicked.c:30-60`（**cpp2il 版这四个实参全丢成 `default`**，⛔ 别照它） |

### 3·3 表 C · `Daily Reward Popup`

| # | 状态 | 判定条件 | 参数 / 显隐 | 出处 |
|---|---|---|---|---|
| **C1** | 抽屉 **Locked** | `index ≥ collected && cur < targetValue` | `Premium Indicator` **F** · `Gacha Reward Claimed` **F** · `Shadow` **F** · `Highlight` **F** · `colider` **F**（不可点） | `CP:DailyRewardDrawerController.cs:339-368` |
| **C2** | 抽屉 **Unlocked** | 同上但 `cur ≥ targetValue` | `Highlight` **T** · `colider` **T** + `interactable=true`；其余三件 **F** | 同上 |
| **C3** | 抽屉 **Collected** | `index < collected` | `Gacha Reward Claimed` **T** · `Shadow` **T**；其余 **F** | 同上 |
| **C4** | 抽屉 **PremiumLocked** | `!mission.IsPremium`（**整条 mission**，与 progress 无关） | `Premium Indicator` **T**；其余 **F** | 同上 + `GetCurrentState` |
| **C5** | 高级列的首领判定 | `IsPremium && normal==Collected && premiumCollected ≤ index` | 该格 premium 抽屉 = **Unlocked**（可再领一次） | `CP:DailyRewardItemContainer.cs:209-226` |
| **C6** | 进度条**显隐** | `milestone.Index != 0` | `progressSlider.gameObject.SetActive(index > 0)` —— **第 0 天没有进度条**；`min 0 / max 1 / wholeNumbers`；`value = (normal != Locked) ? 1 : 0` | `CP:DailyRewardItemContainer.cs:127-141` |
| **C7** | 进度图标 **换图** | `normal == Locked` | `progressStatusImage.sprite = off`；否则 `= on`（**换图、非显隐**） | 同上 |
| **C8** | 逐格 **额外奖励角标** | `rewards.Count > 1`（该抽屉那一档的奖励条数） | `Extra Reward Indicator.SetActive(true)` | `CP:DailyRewardDrawerController.cs:215-233` |
| **C9** | 逐格 **该档奖励为空** | 该 tier 的 list 为空 | **整个抽屉 `gameObject.SetActive(false)`**（不是留在 Locked） | `CP:DailyRewardDrawerController.cs:212-214` |
| **C10** | 格子文案 | 始终 | `rewardNameText = "{quantity} {name}"`；`CosmeticItemTitle` → `PlayerProfile/Title`、`PlayerAvatar` → `PlayerProfile/Avatar`、其余 → `item.LocalizedName` | `CP:DailyRewardDrawerController.cs:234-301` |
| **C11** | 进度数字 | 始终 | `targetValueText = milestone.targetValue.ToString()`（Counter fs36） | `CP:DailyRewardItemContainer.cs:176-178` |
| **C12** | 天数标题 | 始终 | `Loc("MainMenu/DayIndicator")` + `string.Format`（实参取不到，§6·4）；prefab 出厂 'Day 3' | 同上 `:167-175` |
| **C13** | 侧栏 **无 Premium 商品** | `mission.PremiumItem == null` | `premiumTrackHolder.SetActive(false)` | `DF:DailyRewardTrackPanel__Initialize.c:20-30` |
| **C14** | 侧栏 **有商品** | 否则 | `purchaseButton.Setup(PremiumOffer)` → `purchaseButton.gameObject.SetActive(!HasItem(premiumItem) && **mission.IsActive()**)` | 同上 `:31-60`（⚠️ 旧文档写 `ShowInMainMenu`，见附录A·1） |
| **C15** | 买到之后 | 购买成功回调 | **忽略买到的东西**，只把首个 `MissionChallengeProgress.canCollect = true` ⇒ premium 里程碑变可领 | `CP:DailyRewardTrackPanel.cs:63-95` |
| **C16** | 计时器 | `DailyLoginChallenge.GetNextUpdate(progress) > ServerNow` | 真 ⇒ `timerText.Initialize(next)`；假 ⇒ `timerText.gameObject.SetActive(false)` | `CP:DailyRewardPopup.cs:159-218` |
| **C17** | **自动关窗** | `!LiveOp.IsActive()` **或** `!CurrentChallenges.Any()` | 走**基类** `LiveOpsEventWindow<T>.Close()`（**不触发** `TryCollect`） | `CP:DailyRewardPopup.cs:123-157` |
| **C18** | 关窗 = **先收后关** | 点关闭钮 / 背景 | `LiveOp.TryCollect(() => base.Close())`（把当前可领的收掉再关） | `CP:DailyRewardPopup.cs:249-258` |
| **C19** | **聚焦滚动** | `第一格挑战的里程碑数 ≥ 6` | `FocusOnItem(container[count−3])`（把第 `count−3` 格滚进视野） | `CP:DailyRewardSelector.cs:90-136` |
| **C20** | **懒初始化** | `index < 7` 或 `refreshAllRewards` | 只有这些格跑 `Initialize`（第 8 格起、非全刷时**先实例化不初始化**） | `CP:DailyRewardSelector.cs:224-288` |
| **C21** | 格数 | 数据驱动 | 一天一格 = 里程碑条数（**本地拿不到** ⇒ 我方 `DailyData.RewardDays = 4` 是我们挑的） | `CP:DailyRewardSelector.cs:224-288` |
| **C22** | 换阵营 | `armySelector.OnArmySelected` | `SetArmy → SetSelectedArmy + RefreshOnArmyChange`（**不重建窗**，只 `RefreshData(refreshAll:true)` + 换 `ArmyHeader` 图标/名字） | `CP:DailyRewardPopup.cs:220-247` |

### 3·4 表 C' · `Daily Streak Popup`

| # | 状态 | 判定条件 | 参数 / 显隐 | 出处 |
|---|---|---|---|---|
| **S1** | **断签态** | `MainMenuMission.HasFailed == true`（任务对象 `0x30`） | `Streak Failed` **T** / `Streak Successful` **F**；`streakLost.text = format(Loc("Rewards Menu/DailyStreak/StreakLost"), FailedValue)` | `CP:DailyStreakWindow.cs:85-137` |
| **S2** | **连胜态** | `HasFailed == false` | 反过来的两个 `SetActive`；**`HasFailed` 读完即抹（一次性消费）** ⇒ 第二次 `Open()` 必进连胜态 | 同上 |
| **S3** | **断签态隐藏计时器** | 由 S1 推出 | `Timer` 在 `Streak Successful` 子树里 ⇒ 断签态**看不见倒计时**（监听照挂） | prefab 结构 `GO/Daily Streak Popup` |
| **S4** | **格数** | `cur = progress.currentValue`、`c = progress.collectedRewards` | **`cur + 7 − c` 格**（不是恒 7）；编号从 `i = c` 起；`GetMilestoneOrDefault(i)` 取里程碑 | `CP:DailyStreakWindow.cs:139-228` |
| **S5** | 逐格状态 | `cur ≥ milestone.targetValue` | `Unlocked`（亮 `Collect` + `Highlight`）否则 `Locked`（两者都关，`claimButton` GO 关） | `CP:DailyStreakItemContainer.cs`（`SetState`，判据 = `state == Unlocked`） |
| **S6** | 首格放大 | `i == c`（**本次生成的第一个**） | `localScale *= 1.2`（在条目自身 `1.2` 之上 ⇒ **1.44**） | `CP:DailyStreakWindow.cs:202-209` |
| **S7** | 当前连登数 | 始终 | `currentStreakText.text = cur.ToString()` | `:166-170` |
| **S8** | **重置** | 点 `Reset Streak`（只在断签面板） | 面板互换 + `RefreshRewards()`；**不写 `HasFailed`、不改 `cur`、不发网络** | `CP:DailyStreakWindow.cs:63-68` |
| **S9** | 关窗 | 背景 / 返回钮 | `Close()` = `TryCollect(RefreshRewards) → base.Close()`（**先收后关**，同 C18） | `:295-304` |
| **S10** | 自动关窗 | `!IsActive()` / `!CurrentChallenges.Any()` | 基类 `Close()`（不 `TryCollect`） | `:98-136` |
| **S11** | **不可滚动** | 始终 | `ScrollRect m_Enabled=0 · h=0 · v=0` ⇒ 玩家**滚不动**；位移只有 `FocusOnItem` | prefab `RT/-2700378129518681429` 组件字段 |
| **S12** | 计时器 | `DailyLoginChallenge.GetNextUpdate > now`（`ToFocus`） | `timerDisplay.Initialize(next)`（**没有**"过期就隐藏"那一支，与 C16 不同） | `CP:DailyStreakWindow.cs:251-293` |
| **S13** | ESC | `closeOnESC = 0` | **ESC 不关窗**（只能点背景/返回钮） | MB `8654310213240890027` |

### 3·5 表 D · `Inbox Menu`

| # | 状态 | 判定条件 | 参数 / 显隐 | 出处 |
|---|---|---|---|---|
| **D1** | **空态** | `messages == null` **或** `Count < 1` | `No News Warning` **T** · `Message Display`（整块）**F** · **`MessageList.Initialize` 根本不调**（列表停在出厂 0 子件） | `CFG:InboxWindow.cs:64-291`（`:144-173` 那一支） |
| **D2** | **非空态** | `Count ≥ 1` | 警告 **F** · `display.Initialize(cur)` + `list.Initialize(messages, cur)` · `list.OnMessageSelected += OpenMessage` | 同上 `:224-289` |
| **D3** | 当前消息的选取 | 始终 | `data as IGameNews` 优先；否则 `LastOrDefault(IsRead) ?? FirstOrDefault`（**"最后一条已读"**，⛔ 别按直觉改） | `CFG:InboxWindow.cs:293-317` |
| **D4** | `Reset Button` | `Open()` 首句 | **恒 `SetActive(false)`** ⇒ 运行时看不见；即便看见 `m_OnClick` 也是空的（死钮） | `CFG:InboxWindow.cs:87-99` + `MB/7902057683169346354` |
| **D5** | 行：**未读** | `!message.IsRead()` | `unreadMark`（TMP **'New!'** fs40 H=4）`SetActive(true)`；已读 ⇒ 关 | `CP:MessageContainer.cs:73-96` · `:112-121` |
| **D6** | 行：**选中** | 该条 == `current` | `background.sprite = selectedMessageSprite`（`40K_settings_button_selected`）；否则 `normalMessageSprite`（`40K_settings_button`） | `CP:MessageContainer.cs:112-128` |
| **D7** | 行：**悬停/按下** | `Selectable.m_Transition=2`（SpriteSwap） | `HL=40K_settings_button_hover` · `P=40K_settings_button_pressed` | prefab `Message Container/Content` |
| **D8** | 行：**入场** | 首次 `Initialize` | `alpha 0 → 1`（`animationTime`）+ `scale 从 ×?（curve 缓动）`；延迟 `(index+1) × animationDelay`；到点若在屏幕内播 `slottingSound` | `CP:MessageContainer.cs:130-170` |
| **D9** | 行：**未读扫光** | `!IsRead()` 且计时到 | 每 `shineTime = effectTimer × (1 ± 0.2)` 秒播一次 `shinyEffect` | `CP:MessageContainer.cs:172-189` |
| **D10** | 正文：**无消息** | `message == null` | `Message Display` 整块 **F** | `CFG:MessageDisplay.cs:80-198`（头支） |
| **D11** | 正文：**头/尾图** | `HasAsset(msg, HeaderImage=3 / FooterImage=4)` | 分别 `SetActive` + 贴 sprite + `Image.enabled = (sprite != null)`；**没配就关 + 一条 warning，不 fallback** | 同上 + `CFG:LiveOpsAssetUtility` |
| **D12** | 正文：**奖励框** | `save == null` | `claimButtonHolder.SetActive(false)`（整块关） | 同上 |
| **D13** | 正文：**有奖励** | `hasReward = msg.HasRewards` | `claimButtonHolder.SetActive(hasReward)`；`collectButton.interactable = hasReward && CheckMessageCollectable()` | 同上 |
| **D14** | 正文：**可领/不可领（实时）** | 每帧 `Update()`：`claimedAt<=0` | `interactable = CheckMessageCollectable()` · 文本 = `RedeemableAt>now ? GetTimeString(差) : Loc("MainMenu/General/Claim")`；已领 ⇒ `interactable=false` | `CFG:MessageDisplay.cs:37-78` |
| **D15** | 正文：**阵营/扩展未解锁** | `IsArmyHidden / IsExpansionHidden` | `rewardWarningText`（词条 `MainMenu/Inbox/HiddenArmyWarning`）**T** + 返回 false（不可领） | `CFG:MessageDisplay.cs:278-288` |
| **D16** | 正文：**正文里的链接** | 点击落在 TMP 链接上 | `TextLinkProcessing.TryProcessText(body, eventData)` | `CFG:MessageDisplay.cs:327-330` |
| **D17** | 关闭 | 关钮 / 压暗层 / ESC | 三者都进 `InboxWindow.Close()` → 摘监听 → 基类 `Close()` | `CFG:InboxWindow.cs:324-339` |
| **D18** | **红点（顶栏 `InboxBtn`）** | 未读条数 `N > 0` | `UiBadgeNotification`：`showCounter && count>0` ⇒ 亮 + 数字 `N.ToString()`；`Awake` 先把 `alpha` 设 0 再渐入 | `CP:UiBadgeNotification` + `CFG:Inbox.CheckNotification`（§调用链 §三·C） |

### 3·6 表 D2 · `Rewards Base Submenu Variant`（奖励子菜单壳）

> 本窗的**内容页**（Missions / Forge / Campaign / 自建的 DailyStreak）各有自己的规格文档；本表只覆盖**壳**这一层。

| # | 状态 | 判定条件 | 参数 / 显隐 | 出处 |
|---|---|---|---|---|
| **R1** | **第 4 键（母版）恒隐藏** | `TabButtons.Initialize` 首句 | `tabButtonPrefab.gameObject.SetActive(false)` | `CP:TabButtons.cs:118-121` |
| **R2** | **动态页签** | 每个 live-ops 事件一条 | `AddTabButton(name, icon)`：`Instantiate(tabButtonPrefab)` + **`SetSiblingIndex(0)`** ⇒ 插在最前 | `CP:TabButtons.cs:314-345` |
| **R3** | 页签选中 | `EverguildToggle.isOn` | 选中 = `Highlight`（`40k_main_bt_selected BW` #FF0000）淡入（`toggleTransition=1`）；**不是换资产** | 四键 MB 逐读 |
| **R4** | 红点：Missions | `MissionsBadge` 计数 > 0 | 亮 + `OneText` = 数字（fs34） | `MB/-1206074868823222529` |
| **R5** | 红点：Campaign | `CampaignBadge`(无 filter) **或** `MissionsBadge`(filter) > 0 | 同上 | `MB/1045802136805447423` |
| **R6** | 红点：Forge | `ForgeBadge` > 0 | 同上 | `MB/-960798465449912577` |
| **R7** | 红点：第 4 键 | `definitions = []` | **永不亮**（`UpdateCount` 列表空即 return） | `MB/5091015673712023295` |
| **R8** | 内容页显隐 | `GameWindowWithTabs` 切页 | `Tabs/Missions Tab` 出厂 **T**、`Forge Tab` / `Campaign Tab` 出厂 **F** | `GO/Rewards Base Submenu Variant` |

---

## §4 我方落点与档位

### 4·1 `Reward Window`（`Shell/RewardWindow.cs`）

| 状态 | 我方落点 | 现在哪一档 | 判 |
|---|---|---|---|
| A1/A2 预览 ↔ 领取 | `:821 Show` / `:854 ShowCollected` / `:869 ShowPreview` · `:1253 _tap.SetActive(!IsPreview)` · `:1480 ConfigureIsPreviewState` | 两态齐 | ✅ |
| A3 高级锁定 | `:1252 _premium.SetActive(ctx.IsPremiumLocked && HasPremium(...))` | 齐 | ✅ |
| A4 领奖钮 | `:1254 _collect.SetActive(ctx.OnCollect != null)` · `:1200` | 齐（点后 `_collectArmed=false`） | ✅ |
| A5 自定义标题 | 见 `Build()` 的 Title 段（`ctx.CustomTitle`） | 齐 | ✅ |
| A6-A9 逐件三跳 + 展开 | `:1400-1470`（第 2/3/4 跳） | 齐 | ✅ |
| **A10 已拥有档** | `:1435` **恒传 `DrawerOverride.Default`** | **未接**（判据本身也未清，§6·1） | ⚠️ `?` |
| A12 宽屏遮罩 | `:1043-1045` + `:289-290`（`WideAspect 1.77778 / WideSlope 0.555555 / WidePerAspect 300`） | 齐 | ✅ |
| A13 复用 | `Show` 复用实例 | 齐 | ✅ |

### 4·2 `Campaign Reward Window`（`Shell/CampaignRewardWindow.cs` + `Shell/CampaignTab.cs`）

| 状态 | 我方落点 | 现在哪一档 | 判 |
|---|---|---|---|
| B1/B2 两列 + 居中 | `CampaignRewardWindow.cs:502-532 LayoutColumns` | 齐（含 `CenterHolder` 语义） | ✅ |
| B3/B4 预览公式 | 同上 `:521-527`（三分支逐个照 `bVar9`） | 齐（含「只有高级列恒预览」） | ✅ |
| B5-B10 三种钮态 | `:763-860`（`SetAsClaimed` / `SetAsFreeClaim` / `SetUnlockCost` 三支 + 两列不同的门） | 齐 | ✅ |
| B11 else 支文本 | 未写 | 缺（屏幕不可见，低优先） | ⚪ |
| B14 上游 context | `CampaignTab.cs:1271-1284 BuildContext` | 齐；`IsPremiumLocked = false`（用户 2026-09-22 裁定全解锁） | ✅（口径注明） |

### 4·3 `Daily Reward Popup` / `Daily Streak Popup` —— **缺口清单（一句一档）**

1. 🔴 **`Extra Reward Indicator` 整颗没建**：原版 `rewards.Count > 1` ⇒ 开（`CP:DailyRewardDrawerController.cs:215-233`）；我方 `Shell/DailyRewardPopup.cs` 全文 **0 处 `ArtExtra`/`40k_main_bt_rewards`**（`DailyStreakPopup.cs:278` 那颗是连登窗的，⛔ 别混）。落点 = `BuildDrawer`（`:470-570`）。
2. 🔴 **抽屉在「该档奖励为空」时该整颗隐藏**：原版 `CP:DailyRewardDrawerController.cs:212-214` `gameObject.SetActive(false)`；我方 `:431-432 **无条件** `BuildDrawer(...,false)` + `BuildDrawer(...,true)`。
3. 🔴 **天数标题词条**：原版 `Loc("MainMenu/DayIndicator")` + `string.Format`（`CP:DailyRewardItemContainer.cs:167-175`）；我方 `:426` 写死 `"Day " + (day + 1)`。⇒ 换语言不跟（我们自己的 `OnLanguageChanged` 会重建，但键仍是英文硬编码）。
4. 🔴 **连登格数公式**：原版 `cur + 7 − collected`（`CP:DailyStreakWindow.cs:174-226`）；我方 `DailyData.StreakDays = 7` **常量** + `StreakCollected = 5`（`DailyData.cs:1070-1072`，`:182 EntryScaleAt` 用 `i == StreakCollected` 判首格）。**今天两个数恰好使格数 = 7**（5+7−5），⇒ 观感等价、**规则不等价**（`collected < cur` 时会少格/错号）。
5. ⚪ **C19 聚焦滚动 / C20 懒初始化**：我方如实注明「4 格时恒不触发、写了是死代码」（`DailyRewardPopup.cs:984-994`、`:933-951`）—— **不算缺口**（原版这两条在 4 格数据下同样是空操作）。
6. ⚪ **C15 买到之后的 `canCollect=true`**：我方 `HandlePurchaseCompleted` 等价（`:1084` 一带，判据自注）。

### 4·4 `Inbox Menu`（`Shell/InboxWindow.cs` + `Shell/InboxData.cs`）

| 状态 | 我方落点 | 现在哪一档 | 判 |
|---|---|---|---|
| D1/D2 空 ↔ 非空 | `InboxWindow.cs:690-702 ApplyEmptyState` · `:712-732 LoadFromStore` | 两态齐 | ✅ |
| D3 当前消息选取 | `InboxData.cs:268-275 CurrentMessage`（最后一条已读，否则第一条） | 齐 | ✅ |
| D4 Reset Button | `Build()` 末 `SetActive(false)`；点击**出声说明是死钮**（`:831-836`） | 齐（照原版） | ✅ |
| D5 未读 `New!` | `TxtUnread = "New!"`（`:244`）· `RowNewFont=40` | 齐 | ✅ |
| D6/D7 选中/悬停图 | `:777-788 RefreshSelection` + `ArtRowBg/_Selected/_Hover/_Pressed`（`:158-175`） | 齐（`_pressed` 本地缺图，只出声） | ✅ |
| D8 入场动画 | `StartAnimateIn` / `AnimTick`（`:944-1006`） | 齐（批处理下落到终态并说明） | ✅ |
| D9 未读扫光 | `ShineTick`（`:1008-1019`） | 齐 | ✅ |
| D11 头/尾图 | `ShowDetail`（`:796-823`，按 `HeaderArt/FooterArt` 判空） | 齐 | ✅ |
| D12/D13 奖励框 | 同上（`:818-822`） | 齐 | ✅ |
| **D14 领奖钮倒计时文本** | `InboxData.IsWaitingRedeemable`（`:296-299`）**定义但全仓 0 调用**；`InboxWindow` 不写钮文本 | 🔴 **缺这一档**（未到 `RedeemableAt` 时原版把「Claim」换成 `GetTimeString(差)`） | |
| **D15 阵营未解锁警告** | `_mdWarn` **建了但只被关**（`:517-526` / `:800`）；`InboxData.CanCollect` 注释写「我们全解锁 ⇒ 这两支恒不命中」（`:282-283`） | ⚪ **原版分支存在、我方不命中**（用户边界②全解锁）—— 要写明理由，见 §5·5 N-3 | |
| D16 正文链接 | `OnMessageBodyClick`（`:1360`） | 齐 | ✅ |

### 4·5 `Rewards Base Submenu Variant`（`Shell/RewardsWindow.cs`）

| 状态 | 我方落点 | 档 | 判 |
|---|---|---|---|
| R1 第 4 键隐藏 / R2 动态页签 | `RewardsWindow.cs:16-33` 文件头 + `:143` 母版顺位第 5 | 齐（母版**顺位**已挪到第 5，仍被关） | ✅ |
| R3 选中态 | `:198` / `BuildShell` 只画选中的一颗 | 齐 | ✅ |
| R4-R7 红点 | `:253-280 RefreshBadges`（走 `UiBadgeNotification.Recount()` + 一条布尔覆盖 Missions） | 齐 | ✅ |
| R8 内容页显隐 | `MainMenuSubmenuWindow` / `GameWindowWithTabs` | 齐 | ✅ |
| 自建 `DailyStreak` 页 | `:143-150`（用户 2026-10-15 拍板） | **🔶 自己设计**（原版这一页由 live-ops 运行期加） | 已标注 |

---

## §5 按文件分组的施工建议（调度台按文件切给写手）

| # | 文件 | 要做什么 | 判据（本文件哪一节） | 风险 |
|---|---|---|---|---|
| **A** | `Shell/DailyRewardPopup.cs` | ① 加 `Extra Reward Indicator`（`40k_main_bt_rewards` 100²，pos (102.5,143.7)，出厂关，`tierList.Count>1` 才开）；② `BuildDrawer` 在**该档奖励为空**时把整颗抽屉 `SetActive(false)` | §3·3 C8 / C9 · §2·3 | 中：`DrawerForTest` / `DrawerCountForTest` 的期望值要同步（`:1023-1030`） |
| **B** | `Shell/DailyRewardPopup.cs` | 天数标题改走 `Loc.T("MainMenu/DayIndicator")`（词条可能带 `{0}`）而不是 `"Day " + n` | §3·3 C12 | 低（文案层） |
| **C** | `Shell/DailyData.cs` + `Shell/DailyStreakPopup.cs` | 把「7 格常量」改成 `cur + 7 − collected` 的**公式口**（数据侧出 `StreakFirstIndex` / `StreakCount`），`EntryScaleAt` 的「首格」判据跟着改成 `i == firstIndex` | §3·4 S4 / S6 | 中：`RewardsScene` 的 A509 / A1280 那批断言与 `_streakClaimed[]` 的定长数组都要跟着改 |
| **D** | `Shell/InboxWindow.cs` | 补「领奖钮倒计时」：`RedeemableAtMs > now` ⇒ 钮文本换时间串、`interactable=false`；到点自动切回 `Loc("MainMenu/General/Claim")`（原版 `Update()` 每帧重算，我们挂 `Tick` 即可） | §3·5 D14 | 低（但要有 `GetTimeString` 的等价口） |
| **E** | `Shell/InboxWindow.cs` | `_mdWarn` 的**唯一开启口**接到 `InboxData` 的「阵营/扩展未解锁」判据上（今天恒 false，但**口要在**） | §3·5 D15 · §5·5 N-3 | 低 |
| **F** | `Shell/RewardWindow.cs` | **等判据**再动 A10（不要凭猜给 `DrawerOverride` 赋值） | §3·1 A10 · §6·1 | 高（判据未清） |
| **G** | `Shell/CampaignRewardWindow.cs` | 补一句 `premiumWarning.text = premiumExtraTip`（`IsPremiumLocked == false` 那一支；屏幕不可见，纯语义完整） | §3·2 B11 | 无 |
| **H** | 断言（`Editor/RewardsScene.cs` / `ShellScene.cs` —— **调度台独占**） | 逐档补「两档各是各的」：C1-C4 四态五件、C6/C7、C9、S1/S2、D1/D2、D14 倒计时。⚠️ 期望值**不许从被测实现读回**（自证） | §3·3 / §3·4 / §3·5 | 低 |

> ⛔ 本波**不要**动 §6·1 那个 `DrawerOverride` 映射（判据未清 ⇒ 改了 = 猜）。

---

## §5·5 **不做 / 为什么**（已判定「故意不接」的清单）

### N-1 ⛔ `RewardWindowContext` 的 `CollectedRewards` / `CratePrefab` **不实现**

**判据**：`DF:RewardWindow__Open.c` 与 `DF:RewardWindow__DoRewardAnimation.c` **全文 0 次**读 `ctx+0x40` 与 `ctx+0x48`；`CP:RewardWindow.cs` 里 `CollectedRewards` 只出现在 ctor 赋值。
**结论**：原版这两个字段是**死字段**（server 侧也许在用、客户端窗不用）⇒ 我们不复刻"读它"的行为。⛔ 别为了"形似"把它们接到显示里。

### N-2 ⛔ `ConfigureIsPreviewState(bool)` **不当作活代码**

两个窗都有一份 **私有** `ConfigureIsPreviewState`，但在 `decomp_full` 里**只有它自己的 `.c`**（`CampaignRewardsWindow__ConfigureIsPreviewState.c` · `RewardWindow__ConfigureIsPreviewState.c`）⇒ **0 调用者**（`RewardWindow.Open` 是**内联**同一套四句，不是调它）。
我方 `RewardWindow.ConfigureIsPreviewState`（`:1480`）是 **public 且被内部复用**（等价语义）—— **不算偏离**，但⛔ 别以为"原版调了它"。

### N-3 ⚪ 收件箱「阵营/扩展未解锁」警告 **今天恒不命中**，但**口要留**

判据 = 用户 2026-09-17 边界②「**卡池/锻造厂/卡牌/头像全解锁**」⇒ `FeatureConfig.IsArmyHidden/IsExpansionHidden` 在我们这边恒 false ⇒ `rewardWarningText` 永远不该亮。
**做法**：按 §5 建议 E **把口接上**（判据恒 false 也接），⛔ 不要"因为没有这一态就把件删掉"（用户全解锁是一条**用户裁定**，不是原版没有）。

### N-4 ⛔ `DailyRewardSelector.AdjustView` / `Reset()` 里那句 `CustomDebug.LogWarning("[TODO] …")` **不复刻**

`AdjustView` 与 `Initialize` 尾部逐字相同且**全库 0 调用者**（死代码）；`Reset()` 的 TODO 是原版作者给 Addressables 留的（`CP:DailyRewardSelector.cs:290-329`）。我们走 `Resources` 直取、无引用计数要放 ⇒ 照抄只会天天刷一条与我们无关的 TODO。已在上游记过（`DailyRewardPopup.cs:953-956`）。

### N-5 ⛔ `Inbox Menu/Reset Button` **不接行为**

原版：`m_OnClick` 持久调用表空 + `DebugReset` 全库 0 调用者 + `Open()` 首句 `SetActive(false)` ⇒ **运行时看不见、也不起作用**。我方照原版不接，但**点了出声说明**（`InboxWindow.cs:831-836`）。

### N-6 ⛔ `Daily Reward Popup/Menu Dark Background/Image` **不接**（谁开它查不到，§6·3）

出厂 `act=false`、`1920×1080`、`(1,1,1,0.651)`；全反编译**没有任何写入点**（`grep` `DailyRewardPopup` 全部产物）。⛔ 别凭"看起来像遮罩"就打开它。

---

## §6 **未查清**（`?` —— 本文件的全部不确定项）

### 6·1 `RewardWindow` 逐件 `DrawerOverride` 与 `GetOwnedCount(item)` 的映射
`DF:RewardWindow__Open.c:150-158`：`iVar12 = InventoryManager.GetOwnedCount(item)` → `FUN_180014bc0(1, <DrawerOverride 类型>, <抽屉>, iVar12 < 2, 1)`。
- `CP:RewardWindow.cs:471` 把它译成 `(flag10 ? (DrawerOverride)1 : DrawerOverride.Default)`，而 **`DrawerOverride` 没有值 1**（`Default 0 / Icon 10 / Horizontal 15 / Shop 20 / OfferPopups 30`，`CP:DrawerOverride.cs`）⇒ **该译法是伪码，不可用**。
- `FUN_180014bc0` 是"把 bool 型实参转成某个 enum/装箱"的共享助记（`disasm_va 0x180014bc0` 是一段接口调用 + 装箱，未见分支），**没读出它到底选哪一档**。
- **还差什么**：对 `FUN_180014bc0` 的**逐指令**语义判读（或直接反汇编 `RewardWindow__Open` 该处的 `mov`/`lea` 常量）。⚠️ 在拿到之前**⛔ 别按 `owned ≥ 2 ⇒ Shop` 改代码**（那是我的推测，不是判据）。

### 6·2 `Reward Window/Menu Vignette` 的驱动
`Image` 无 sprite、`(0,0,0,0.58)`、Sliced、铺满 —— 全反编译**没找到写它的代码**（不在 `RewardWindow` 字段表里）。是「恒开的设计」还是「某处 SetActive」= **`?`**。

### 6·3 `Daily Reward Popup/Menu Dark Background/Image`（1920×1080 · α0.651 · 出厂关）
同样无写入点 ⇒ 是「窗口内二级弹窗的底」还是死件 = **`?`**（§5·5 N-6 明确不做，直到查清）。

### 6·4 `MainMenu/DayIndicator` 的实参格式
`CP:DailyRewardItemContainer.cs:171-175`：`GetTranslation("MainMenu/DayIndicator")` 后 `string.Format(translation, <value_box>)`，但**被装箱的那个值**在伪码里是 `il2cpp_value_box` 未解析；prefab 出厂文本是 `'Day 3'`。
⇒ **`?`**：不知道是 `{0}` 还是 `Day {0}`。拿到官方 13 语言表里 `MainMenu/DayIndicator` 的值即可定（本代理未查该表）。
**还差什么**：`Resources/official_loc_13lang.json` 里查该键的中/英值（一次性）。

### 6·5 小屏档是否另有「字体/滚动」全局通路
本文件只证 **`GameWindow.Open` 那条 `TransformScalerBySmallScreenUI` 分支不命中**（`extraScaleSmallScreen=1.0`）。**字号/行距/sens 是否另有全局档**（如 TMP 全局缩放、`GameStaticData.smallScreenUI` 被别处读）—— **`?`**（与 `AUD05_R2` 同一开放问题）。

### 6·6 `MessageDisplay` 倒计时串的**逐字格式**
`SupportMethods.GetTimeString(ts, eventTimer: true)` 的具体模板（`19h 23m` / `19h23m` / 是否本地化）本地**没读**（我方 `TimerDisplay` 另有一份口径）。⇒ D14 施工前要先定这一格。

---

## 附录 A · 顺手发现（不属本窗，但该记账）

1. 🔴 **`日常_调用链_三窗.md` §B-3 那句 `liveOp.ShowInMainMenu` 是错的**：`DailyRewardTrackPanel.Initialize` 的第二个条件是 **`LiveOpsEvent.IsActive()`**（vtable **slot 48 / offset 0x438**，与同文件 §一·A-2 自己标定的 `IsActive` 同一个槽）。两源同证：`DF:DailyRewardTrackPanel__Initialize.c:47-57`（`(**(code **)(*param_2 + 0x438))`）与 `CP:DailyRewardTrackPanel.cs:50`（`mission.IsActive()`）。⚠️ 只读代理无权改那份文档 ⇒ 请调度台就地订正（铁律 5）。
2. 🔴 **`日常_调用链_三窗.md` §三·B-10 写 `backgroundButton.m_OnClick += Select()`**：实际是 **`selectButton`**（`CP:MessageContainer.cs:93-95`），且 prefab 里 `MessageContainer.backgroundButton = {m_PathID: 0}`（= null）。同节「点击走 `backgroundButton`」的说法要改。
3. ⚪ **`RewardWindow.premiumWarning` 指向的是那颗 TMP 节点自身**（pid `6457714889012799857` = `Premium Disclaimer` 的 `TextMeshProUGUI`），不是父容器 —— 与 `CampaignRewardsWindow.premiumWarning`（同样是 TMP）一致。改这几件时别去找"容器 GameObject"。
4. 🟡 **`DailyRewardDrawerController.Initialize` 对空档是「整颗关」而不是「留 Locked」**（`CP:…:212-214`）—— 这条与 §3·3 C9 是同一事，但它同时解释了「为什么实拍里某些天只有一格」（另一档不存在）。
5. 🟡 **`RewardInfo.IsEphemeral` 是**计算属性**，不是序列化字段**（`CP:RewardInfo.cs:36-70`：`Quantity>0 && Item!=null && (Item 是限时的两条路之一)`）⇒ 判"这条奖励是否限时"不能去 prefab 找字段。
6. ⚪ **`Shell/InboxWindow.cs:1037-1041` 的注释已过期**：写着「点击这一半仍未做」，而同一方法往下 `:1069-1074` 已经 `onClick → SelectMessage(captured)`。请派活时就地订正（只读代理不能改 `.cs`）。
7. ⚪ **`CampaignRewardsWindow` 的 `UnlockClicked(RewardTier)` 是死私有方法**（`CP:…:774-782`，全库只命中它自己）—— 真正的回调走 `CampaignUnlockButton.OnUnlockClicked`（`CP:CampaignUnlockButton.cs:56-68`）。⛔ 别按名字把逻辑接错。
8. 🟡 **「pivot 是 0.5 还是 1」会差 37.5px —— 本文件自己先算错过一次**：`Reward Window/Title` 的 `m_Pivot=(.5,**1**)`，所以我第一版按 `pivot=0.5` 算出的 `155.8..230.8` **是错的**，正确是 **`193.3..268.3`**（与 `Shell/RewardWindow.cs:139` 的常量逐值相同）。同族另一颗 `Tap To Continue` 也是 `pivot=(.5,1)` ⇒ **在 `Content` 之下**（965..1045，压出屏幕底 35px），不是"贴着底边上面"。⇒ **量这类锚点前先读 `m_Pivot`**（同 `AUD05_R2` §附录B「量设计尺寸用 `rect.rect`」那条是同一类坑）。

---

## 附录 B · 覆盖边界（本文件到底查了多少）

| 维度 | 覆盖 |
|---|---|
| **反编译** | 读透：`RewardWindow.cs`(848) · `RewardWindowContext.cs`(271) · `CampaignRewardsWindow.cs`(813) · `CampaignRewardsWindowContext.cs`(145) · `CampaignUnlockButton.cs` · `DailyRewardPopup.cs`(271) · `DailyRewardSelector.cs`(340) · `DailyRewardItemContainer.cs`(303) · `DailyRewardDrawerController.cs`(395) · `DailyRewardTrackPanel.cs`(96) · `DailyStreakWindow.cs`(320) · `InboxWindow.cs`(350) · `MessageContainer.cs`(190) · `MessageDisplay.cs`(331) · `TabButtons.cs`（Initialize/AddTabButton 两段）· `RewardService.cs`（`Collect`/`Preview` 两段）· `RewardInfo.cs` · `MainMenuRewardsWindow.cs` |
| **`.c`（Ghidra）** | `RewardWindow__Open.c`(314) · `RewardWindow__DoRewardAnimation.c`（头 120 行）· `CampaignRewardsWindow__Open.c` · `CampaignWindowTab__OnNodeClicked.c`（全）· `DailyRewardTrackPanel__Initialize.c`（全）· `GameWindow__Open.c`（头 70 行） |
| **字面量** | 5 个（`0x1834b32e0/e8/d0/cc/326c/d4` + `1834b2bb8`）经 `工具/read_literal.py` 读出 |
| **预制体** | 6 个窗根（`GameObject/*.json`）+ 各自整棵树（RT/Image/TMP/LayoutGroup/RectMask2D/ScrollRect 逐节点）；`Message Container` 独立 prefab；4 颗 `Badge Highlight` 的 `UiBadgeNotification` 定义 |
| **未读** | `MessageList.cs`(309) 只核了「清空/建容器/ToggleSelection/AnimateIn」四条调用（逐字体未读）· `RewardService.cs` 其余 800 行 · `CampaignWindowTab.cs` 其余 · `UiBadgeNotification.cs`（只引上游结论） |
| **`?` 的数量** | **6 格**（§6·1 ~ §6·6） |
| **没跑的东西** | Unity 批处理 / 真机 / `typecheck`（按只读代理红线，一律没跑） |
| ⚠️ 提醒 | **「本文件把参数查清了」≠「原版这个件全复刻了」** —— 本文件只回答「哪个状态对应哪套参数」，不回答「我们画得对不对」（那要靠并排实拍 / 真机，属调度台的验收）。 |
