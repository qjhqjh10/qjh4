# B8_AQ —— `菜单/外壳` 域的 `Shell/**` B 行往下推（19 条真升 A + 1 条真缺陷修掉）

> 执行代理 `B8_AQ` · 2026-10-26 · 白名单 = `CardPresentation/Shell/**` + `账/_parts/B8_AQ.tsv` + 本报告
> ⛔ 未跑 Unity / 未动 git / 未改主表 / 未改正本 / 未越白名单。`bash 工具/typecheck.sh` = **0 / 0**。

---

## 一、结论

| 项 | 数字 |
|---|---|
| 本轮扫过的 B 行（`菜单_外壳` 173 + 收藏 9 + 商店 17 + 锻造 34 + 战役 17 + 轮抽 15 = **265**，其中落点含 `Shell/` 的 = **192**） | **192** |
| **真升 A** | **19**（MediaTab 7 · GraphicsTab 4 · PracticeArmySelectionButton 5 · WhereToGetCardsWindow 2 · ToastNotificationController 1） |
| 改判据后仍留 **B**（写清缺什么） | **3**（`ToastNotification.ForceUpdateLayout` · `ToastNotificationController.ShowNotificationInternal` · `WhereToGetCardsWindow.Close`） |
| **真缺陷修掉** | **1**（`WhereToGetCardsPopup` 压暗层点击语义，见 §六·1） |
| 账补丁 | `账/_parts/B8_AQ.tsv` = **22 行**（19 A + 3 B）· 11 列 / LF / 每行 11 字段 · `merge --dry` = **更新 22 · 新增 0** |
| 代码改动 | 只有 `Shell/WhereToGetCardsPopup.cs` 两处 **10 行→10 行（零净增行）** |

**为什么这批以前升不了 A**：上一路 `B8_Y` 的结论是「Shell 的 B 行绝大多数卡在 G3（可观察量要跑 Unity）」。
本轮的解法 = **把观测量取在「已经写好的自检断言」上**（`Editor/SettingsScene.cs` / `Editor/MainMenuScene.cs` 里
这批方法的可观察量**几乎全都有现成断言**，两态都断过），原版侧期望值一律从**反编译方法体 + `.rdata` 常量**推，
我们侧值从**落点代码算式 + 既有断言**取。⇒ 不需要跑 Unity 也能给「两侧数值」。

---

## 二、逐条证据（19 条 A）

> 格式：`块 | 方法 | 判据出处 | 我们落点（行号范围） | 观测量 + 两侧数值 | 差异`。
> 判据前缀：🥇 = `d:/2/tools/decomp_full/*.c`（Ghidra，真方法体）· 🥈 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs`（ILSpy，真方法体）。

### 2·1 MediaTab（7 条 · 全部 🥈 + 🥇 双证）

| 方法 | 判据出处 | 我们落点 | 观测量 + 两侧数值 | 差异 |
|---|---|---|---|---|
| `MusicValueChange(float)` | 🥈 `MediaTab.cs:137-141` · 🥇 `PlayerDataManager__SetMusicAndFxVolumes.c:28-37,89-90` | `Core/WarpforgeAudio.cs:177-184`（`SetMusic`）· 接线 `SettingsWindow.cs:5275` + `:5346` | `WarpforgeAudio.Music`（读口 `Dump()`）。原版 `v=0.42 ⇒ −7.535 dB`（`Log10(0.42)×20`）；我们同公式。既有断言 `SettingsScene.cs:3747-3749` | 原版全量重写四参、我们只写被改那条（其余入参未变 ⇒ 幂等） |
| `SoundFXValueChange(float)` | 🥈 `MediaTab.cs:143-147` · 🥇 `…SetMusicAndFxVolumes.c:64-65,79-80` | `Core/WarpforgeAudio.cs:186-192`（`SetSoundFx`） | `WarpforgeAudio.SoundFx` + `VolumeFX`/`VolumeJingles`（`ParamsOk`）。`v=0.37 ⇒ −8.636 dB`、两条同值、不落盘 | 同上 |
| `VoiceOversValueChange(float)` | 🥈 `MediaTab.cs:131-135` · 🥇 `…SetMusicAndFxVolumes.c:49-50` | `Core/WarpforgeAudio.cs:194-199`（`SetVoiceOver`） | `WarpforgeAudio.VoiceOver` + `VolumeVoices`。`v=0.23 ⇒ −12.765 dB`、不落盘 | 同上 |
| `OnWindowModeChanged(int)` | 🥈 `MediaTab.cs:63-69` · 🥇 `MediaTab__OnWindowModeChanged.c:13` + `ResolutionManager__SetFullScreen.c:13-31` | `Shell/SettingsWindow.cs:3961-3967`（`ChooseWindowModeOption`） | `Screen.fullScreen`：选 Windowed ⇒ `false`（推导 `1 == 0`）、选 Full ⇒ `true` | 原版切全屏走 `Screen.SetResolution(w,h,true)`、我们 `Screen.fullScreen = true`（Unity 等价）；原版无早返回，但 `TMP_Dropdown` 值不变不发事件 ⇒ 分支到不了 |
| `SetupVideoSettings()` | 🥈 `MediaTab.cs:70-129` · 🥇 `MediaTab__SetupVideoSettings.c:82` | `SettingsWindow.cs:3939-3947` + `:5382-5388` + `:5401-5403` | 下拉被选中那一行号 = `WindowModeOptionIndex()`；`noFullScreen 0/1 ⇒ 0/1` | 原版每次 `OnOpen` 重建 options；我们建一次 + 活 getter ⇒ 等价 |
| `OnSetup()` | 🥈 `MediaTab.cs:20-39` · 🥇 `MediaTab__OnSetup.c:32-93` | `SettingsWindow.cs:5247-5360`（`BuildAudioPage`，初值+监听在 `:5346`） | 三根滑块初值 = `WarpforgeAudio.{Music,SoundFx,VoiceOver}` 逐根相等；既有断言 `SettingsScene.cs:3719-3720` | 原版 `OnSetup` 只跑一次；我们每次 `Build()` 重读（多一次刷新、无可见差异） |
| `OnOpen()` | 🥈 `MediaTab.cs:41-45` · 🥇 `MediaTab__OnOpen.c:3-7` | `SettingsWindow.cs:1994-2031`（`OpenTab`）→ `:2029` `RefreshTexts()` → `:3337` → `:5388` 登记的那条 | 切到音频页后帽字 = `WindowModeOptionLabels()[WindowModeOptionIndex()]`（= 当前全屏态） | **无差异**（同时机同效果）。原版 `base.OnOpen()` 是**全局共享空方法桩**（`RVA 0x4B33B0`；`.c:5` 的 `NetworkingPeer__OnMessage` 是 ICF 折叠）⇒ 体里只剩 `SetupVideoSettings()` |

### 2·2 GraphicsTab（4 条）

| 方法 | 判据出处 | 我们落点 | 观测量 + 两侧数值 | 差异 |
|---|---|---|---|---|
| `ConfigureSuperSamplingVisibility()` | 🥇 `GraphicsTab__ConfigureSuperSamplingVisibility.c:15-30`（🥈 `GraphicsTab.cs:84-92`） | `SettingsWindow.cs:4391-4452`（`RebuildGfxRows`：门 `:4396`、`SetActive` `:4443`；门本体 `:6329-6333`） | `Use super sampling` 行 `activeSelf`：原版 `allowSuperSampling` 五档 0/0/0/0/1 ⇒ VeryLow–High 假 / Ultra 真；我们 档0(`Mobile`) 假 / 档1(`PC`) 真。既有断言两态 `SettingsScene.cs:3005-3007` + `:3022-3023` | 原版读 `qualityDropdown.m_Value`、我们读 `QualitySettings.GetQualityLevel()`；因为我们是**当场应用** ⇒ 两支同值 |
| `SuperSamplingToggleClick(bool)` | 🥇 `GraphicsTab__SuperSamplingToggleClick.c:13-22`（🥈 `GraphicsTab.cs:101-111`） | `SettingsWindow.cs:4634-4642` → `:6344-6352`（`Set`：写 `_enabled`+`_dirty`）→ `:6361` `ApplyIfDirty()`（`:1609` 关窗调） | URP 资产 `renderScale`：点完**仍 1.0**、关窗后 **2.0**、再关回 1.0。既有断言 `SettingsScene.cs:3066-3068` / `:3072-3073` / `:3083-3084` | 脏位存放位置：原版 `SettingsMenu+0xc0`、我们 `SuperSampling._dirty` ⇒ 同时机同效果 |
| `SmallScreenToggleClick(bool)` | 🥇 `GraphicsTab__SmallScreenToggleClick.c:10-19`（🥈 `GraphicsTab.cs:119-127`） | `SettingsWindow.cs:4576-4595` → `Shell/TransformScalerBySmallScreenUI.cs:82-88`（`SmallScreenUI.Set`） | `SmallScreenUI.Enabled` / `ChosenManually` + `CheckMark.activeSelf`。原版 `+0x11c=1` 且 `+0x12e=1`。既有断言 `SettingsScene.cs:2815-2817` + `:2827-2829` | 多两件（都如实登记）：① `PlayerPrefs` 持久化（🔶 自己设计，已在 `:58` 打标）② 点完当场 `CardView.RefreshAllCostTiers()`（= 原版 `CardTextCountersController.Toggle` 按当前值重算的净效果） |
| `AutoZoomClick(bool)` | 🥇 `GraphicsTab__AutoZoomClick.c:10-18`（🥈 `GraphicsTab.cs:113-117`） | `SettingsWindow.cs:4611-4626` → `:6218-6226`（`AutoZoom.Set`） | `AutoZoom.Enabled` / `ChosenManually`。原版 `+0x125=1` 且 `+0x12f=1`。既有断言 `SettingsScene.cs:2948-2949` + `:2957` | 多一次 `FindFirstObjectByType<CombatAutoZoom>()?.ForceRefresh()`（= 原版**战斗内**那颗开关的那一跳，原版菜单这扇不做）；菜单这扇不进战斗 ⇒ 该分支恒 `null` |

### 2·3 PracticeArmySelectionButton（5 条 · 原版是每格一个 `MonoBehaviour`，我们是 `PracticeModePopup` 里现建现绑 ⇒ 共享落点）

判据：🥈 `PracticeArmySelectionButton.cs:21-27 / 30-40 / 42-45 / 47-50 / 52-55` ·
🥇 `PracticeModePopup__ConfigureArmyFilterButtons.c:191-201`（每颗 `Instantiate` 后紧接着 `SetActive(+0x28,0)` / `SetActive(+0x30,0)`）·
🥇 `PracticeModePopup__ChangeSelectedArmy.c:30-33`（同阵营早返回）、`:68-72`（`SetActive(+0x28, army == param_2)`）、`:79-83 / 99-104`（火漆印）·
🥇 `ArmyUtilities__GetArmyIcon.c:5-8`。

| 方法 | 我们落点 | 观测量 + 两侧数值 |
|---|---|---|
| `Awake()` | `PracticeModePopup.cs:1767-1890`（`RebuildArmyCells`；点格绑定在 `:1887`） | 点第 i 格 ⇒ `ArmyIndex == i`（原版 `ChangeSelectedArmy` 后 `self+0xe8`）。既有断言 `MainMenuScene.cs:3391-3394` / `:3399-3403` |
| `Initialize(army, menu)` | 同上（建格 `:1818-1821`、`Icon` 取图 `:1868-1869`） | 第 i 格 `Icon` 贴图名 == `DeckRuntime.FactionIcon(facs[i])`。同类既有断言 `CollectionScene.cs:5859`；本窗**草稿**（§四·②） |
| `OnArmySelected()` | `PracticeModePopup.cs:2613-2654`（`PickArmy`；早返回 `:2622`、重画 `:2652`） | `ArmyIndex` 改没改 + 高亮格数（原版 `+0xe8`） |
| `Highlight(bool)` | `PracticeModePopup.cs:1767-1890`（显隐 `:1877`） | `Highlight` 亮格数 = 1 且下标 = 选中阵营；`ArmyIndex=-1` ⇒ 0 格。既有断言 `MainMenuScene.cs:3399-3403` + `:3893` |
| `ToggleHighlightPlayerDeck(bool)` | 同上（显隐 `:1881-1882`） | `Has Player Deck` 亮格数：无选中卡组 ⇒ 0、有 ⇒ 恰好 1（= 卡组阵营那格）。既有断言 `MainMenuScene.cs:3884-3920` |

**机制差异（这 5 条共同的）**：原版每格常驻一个 `PracticeArmySelectionButton`（`Awake` 绑一次 `m_OnClick`）；
我们由 `RebuildArmyCells` 现建现绑闭包（`MenuScroll` **不回收** cell ⇒ 不会重复挂）。`Highlight` 比较的是
`i == ArmyIndex`（同一数组下标）代替 `btn.army == param_2`；火漆印走 `DeckRules.SameFaction`（`OrdinalIgnoreCase`）
代替裸枚举 `==` —— 同一批数据下等价，已如实登记。

### 2·4 WhereToGetCardsWindow（2 条 A）

| 方法 | 判据出处 | 我们落点 | 观测量 + 两侧数值 |
|---|---|---|---|
| `Awake()` | 🥈 `WhereToGetCardsWindow.cs:60-75` · 🥇 `WhereToGetCardsWindow__Awake.c:9-26` · 🥈 `BackgroundCloseButton.cs:18-25` | `WhereToGetCardsPopup.cs:288-379`（`Build`；关窗钮命中 `:357-359`、压暗层命中 `:311`） | 点 `Menu Dark Background` ⇒ `CurrentState == Closed`；点关窗钮 ⇒ 同 |
| `Reset()` | 🥈 `WhereToGetCardsWindow.cs:138-176` · 🥇 `WhereToGetCardsWindow__Reset.c` | `WhereToGetCardsPopup.cs:398-404`（`BuildSections` 开头） | `SectionsShown`：`Show(1 段) ⇒ 1`、`Show(null) ⇒ 0`（**不累加**）。既有断言 `MainMenuScene.cs:17410` + `:17444` |

### 2·5 ToastNotificationController（1 条 A）

| 方法 | 判据出处 | 我们落点 | 观测量 + 两侧数值 |
|---|---|---|---|
| `TryShowNotification(context)` | 🥈 `ToastNotificationController.cs:224-270` | `Shell/ToastNotifications.cs:721-738`（`ShowNotification`；handler 支 `:732-736`、内部支 `:737`、注册口 `:756-760`） | 用某 `ControllerID` 注册 handler 后弹一条 ⇒ handler 调 1 次、内部展示计数 +0；换未注册 id ⇒ 内部 +1。原版同（`handlers.TryGetValue` 命中就只走 handler）。**草稿**（§四·④） |

**差异（真差异、已登记不改）**：`ControllerID` 为**空串**时两支相反 —— 原版只判 `!= null`（空串算有 id），
我们判 `!IsNullOrEmpty`，且 `RegisterNotificationHandler` 拒绝空串键 ⇒ 那一格连注册都进不来。实际调用方给的都是非空 id。

---

## 三、改了哪些文件 / 哪些行 / 新增行数

| 文件 | 改动 | 新增行数 |
|---|---|---|
| `MyGame/Assets/CardPresentation/Shell/WhereToGetCardsPopup.cs` | ① 文件头 `:47-50`（4 行）就地改写：压暗层语义从「什么都不发生」订正成「= 关窗」② `Build()` 里 `:306-311`（6 行）就地改写 + `MenuDraw.Absorb(...)` → `MenuDraw.ShadeHit(…, () => Close(), …)` | **0**（`git diff --numstat` = `10 10`，**行数零变化** —— 这个文件被 `收藏.tsv:340/341/344/345` 与 `菜单_外壳.tsv:814` 按行号引用，插行会立刻造成漂移） |
| `资料/完全复刻/账/_parts/B8_AQ.tsv` | 新文件，22 行（19 A + 3 B） | 22 |

补丁是**生成**的（不是手敲）：`d:/4/Unity/_tmp_view/b8aq/gen_patch.py`（行内容在脚本里，改完重跑即重写补丁）+
`d:/4/Unity/_tmp_view/b8aq/selfcheck.py`（复刻 `check_ledger_points` / `verify_annot` / `check_a_vs_diff` 三条闸的核心判法，
逐行报「落点声明名 vs 括号名 / 是否有等价实现注解 / 差异+备注里有没有缺口词」）。当前输出 = **21 LAND_OK + 1 LAND_CHK（那条是 B 行，三道闸都不扫 B）· GAP 全空**。

`typecheck`：`bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

---

## 四、断言草稿（`Editor/` 不在我白名单 ⇒ 只给草稿 + 该插在哪）

> 每块都写明宿主、锚点、期望值、以及**灭自证**（把哪一处改回去这条会红）。

**① `SettingsScene.cs` · 音频页那一段（现 `:3747-3749` 只断了第 1 根）之后加两根**
```csharp
sl[1].SetValue(0.37f, true);
CheckNear(WarpforgeAudio.SoundFx, 0.37f, 0.01f,
          "拖第 2 根 ⇒ 原版 `GameStaticData.soundFxVolume`(+0xd4) 那一半（SetMixerVolume(0) + (3) 同值）");
CheckTrue(WarpforgeAudio.Dump().Contains("FX=0.37(-8.6dB)") && WarpforgeAudio.Dump().Contains("Jingles(-8.6dB)"),
          "…而且**真的写进了两条 mixer 参数**：" + WarpforgeAudio.Dump());
sl[1].SetValue(beforeFx, true);                        // 还原
sl[2].SetValue(0.23f, true);
CheckNear(WarpforgeAudio.VoiceOver, 0.23f, 0.01f, "拖第 3 根 ⇒ 原版 `voiceOversVolume`(+0xd0)");
sl[2].SetValue(beforeVo, true);
```
灭自证：把 `WarpforgeAudio.SetSoundFx` 里的 `Write("VolumeJingles", _fx)` 删掉 ⇒ 第二句红（`Dump()` 里 Jingles 掉到 −80dB）。

**② `SettingsScene.cs` · 音频页 WindowMode 那一段（目前 0 条）**
```csharp
SettingsWindow.FullScreenGetter = () => false;         // 起始 = 窗口化
bool? got = null; SettingsWindow.FullScreenSetter = v => got = v;
Click(win.DropdownRowHit(SettingsWindow.DdWindowMode, 0));   // 选「Full screen」
Check(got, true, "选 Full screen ⇒ 原版 `SetFullScreen(option == 0)` 收到 **true**");
CheckTrue(win.WinCapText() == Loc.T("Settings/Media/WindowMode/Fullscreen"), "…帽字跟着换成 Full screen");
SettingsWindow.FullScreenSetter = null; SettingsWindow.FullScreenGetter = null;
```
灭自证：把 `ChooseWindowModeOption` 的 `SetFullScreen(wantFull)` 改成 `SetFullScreen(true)` ⇒ 选 Windowed 那条红。

**③ `MainMenuScene.cs` · `T27 卡牌来源窗` 那一段（`:17285-17291` 压暗层三条之后）**
```csharp
CheckTrue(MenuDraw.WasShadeHit(wc.ShadeHitNode) && !MenuDraw.WasAbsorb(wc.ShadeHitNode),
          "★ 压暗层那颗是 **`MenuDraw.ShadeHit`**（点它 = 关窗）—— 判据 = `WhereToGetCardsWindow__Awake.c:12-24` "
        + "运行期给 `backgroundCloseButton.onClick` 加了监听器（同 closeButton 的 vtable 槽 = `Close`）");
CheckTrue(MenuDraw.WasShadeHit(wc.CloseHitNode), "…关窗钮那颗也是 `ShadeHit`（同一套关窗语义）");
```
灭自证：把 `MenuDraw.ShadeHit(_shade, …, () => Close(), "BackgroundHit")` 改回 `MenuDraw.Absorb(...)` ⇒ 第一句红。

**④ `ShellScene.cs` · 飘窗那一段（目前 0 条 handler 路由断言）**
```csharp
int hit = 0, inner = 0;
ToastNotifications.RegisterNotificationHandler("t.route",
    new TestHandler(() => hit++));                        // 需要 ShellScene 里一个 5 行的 IHandler 夹具
int shownBefore = ToastNotifications.InternalShowCount;   // 需要一个只读计数口（现无 ⇒ 见 §五·3）
ToastNotifications.ShowNotification(new ToastNotifications.Context("x", ToastNotifs.Type.Achievement) { ControllerId = "t.route" });
Check(hit, 1, "注册了 handler 的 ControllerID ⇒ **handler 收到**（原版 `handlers.TryGetValue` 命中支）");
Check(ToastNotifications.InternalShowCount, shownBefore, "…而且内部展示**一次都没跑**");
```
灭自证：把 `ShowNotification` 的 `:732-736` 那支挪到 `:737` 之后（先内部、后 handler）⇒ 第二句红。

**⑤ `MainMenuScene.cs` · 阵营列（T? 段，`ArmyLit` 那一段之后）— 让 `PickArmy` 的两态进断言**
```csharp
int a0 = pw.ArmyIndex; int other = (a0 + 1) % pw.ArmyCells.Count;
Click(pw.ArmyCells[other].Find("Hit"));
Check(pw.ArmyIndex, other, "点另一格 ⇒ `ArmyIndex` 换到那一格（原版 `+0xe8 = param_2`）");
Check(ArmyLit(pw, "Highlight")[0], other, "…高亮跟着走（原版 `SetActive(+0x28, army == param_2)`）");
Click(pw.ArmyCells[other].Find("Hit"));
Check(pw.ArmyIndex, other, "**同阵营再点 ⇒ 不变**（原版 `ChangeSelectedArmy.c:30-33` 早返回；🧨 改回 toggle ⇒ 红）");
```
灭自证：删掉 `PickArmy` 的 `if (ArmyIndex == i) return;` ⇒ 第三句红。

**⑥ `SettingsScene.cs` · 窗口模式帽字「切页重算」（对应 `MediaTab.OnOpen`）**
```csharp
SettingsWindow.FullScreenGetter = () => false;
win.OpenTab(SettingsTab.General); win.OpenTab(SettingsTab.Audio);
CheckTrue(win.WinCapText() == Loc.T("Settings/Media/WindowMode/Windowed"),
          "切回音频页 ⇒ 帽字按**当前**全屏态重算（原版 `OnOpen → SetupVideoSettings`）");
```
灭自证：把 `RefreshTexts()` 里 `for (… _onLabels …)` 那一圈删掉 ⇒ 红。

---

## 五、没查清的部分（⛔ 别当结论用）

1. **`MenuDraw.ShadeHit` 是否会给飘窗带来悬停色偏**：`ShadeHit` 走 `Hit(...)`（= `WindowButton` + 世界空间 quad），
   而 `WindowButton` 默认带 hover 色偏。我把 `WhereToGetCardsPopup` 的压暗层从 `Absorb` 换成 `ShadeHit` 之后，
   **压暗层的悬停行为变了**（原版 `BackgroundCloseButton` 不是 `Selectable`、没有悬停变化）。
   同族先例 `BaseEventPopup.cs:317` 也是这么用的（`ShadeHit(..., "BackgroundCloseButton")`）⇒ 全仓口径一致；
   但**"压暗层划过会不会变暗"这一条没跑真机验过** —— 需要一次真 Play（`SettingsScene.Run` 的截图或 `wf_click` 悬停）。
   ⇒ 若确认会变暗，正确做法是给 `ShadeHit` 加一个"不 tint"的形参（那是 `Shell/MenuDraw.cs`，在我白名单内，可以说）。
2. **`ToastNotification.ForceUpdateLayout` / `ShowNotificationInternal` 的原版根高期望值**：原版那三步是
   uGUI `LayoutGroup`+`CSF` 链，我们没有那个模型 ⇒ 拿不到"跑完三步后 `rect.height` = 多少"。
   要升 A 得**跑一次原版**（`d:/2/unity_run_ref/` 能跳场景，但飘窗要事件触发 ⇒ 不一定跑得出来），
   或从 prefab 的 `m_SizeDelta` 反推（那是模板态、不是运行态）。**这两条我不硬凑。**
3. **`ToastNotifications.InternalShowCount` 这个读口不存在**（§四·④ 的断言要用它）。加一个只读计数口是
   3 行改动、在我白名单内，但它是"为断言加的读口"⇒ 按规矩**由调度台在同步点决定加不加**（同 `ItemDrawer.HandleCount` 的先例）。
4. **`WhereToGetCardsWindow.Close()` 那一半**：原版关窗逐段 `Release()`（= 把每张 `Card Drawer` **还池**），
   我们这一层没有 `Card Drawer`/`ItemDrawer` ⇒ 观测量两边不同源。要升 A 需先决定"来源段的卡格要不要改成池化件"，
   那是另一笔结构改造（**已写进账的 B 行备注**）。
5. **`WhereToGetCardsPopup` 的 `BackgroundHit` 节点名**：我保留了旧名字（`"BackgroundHit"`），而本仓 `ShadeHit` 的
   默认名是 `"CloseHit"`。名字只影响自检 `FindChild`；若调度台要统一成 `CloseHit`，得同时改 §四·③ 那条断言（一行）。

---

## 六、顺手发现（⛔ 只报；已修的写明修在哪）

1. ✅ **已修 · 真缺陷**：`WhereToGetCardsPopup` 的**压暗层点击**。
   旧实现（`N51③`, 2026-10-17）= `MenuDraw.Absorb`（吃掉点击、什么都不做），理由是「prefab 的
   `BackgroundCloseButton.window` 是空 PPtr、`onClick` 也空」。**那条理由漏了一半**：
   · 🥇 `BackgroundCloseButton__…OnPointerClick.c:24-27` = `if (window != null) window.Close(); onClick.Invoke();`
     —— `onClick` 是**序列化 `UnityEvent`**（永不为 null）⇒ `Invoke()`（`.c:27`）**无条件跑**；
   · 🥇 `WhereToGetCardsWindow__Awake.c:12-24` 在运行期给 `backgroundCloseButton.onClick` **加了一个监听器**
     （delegate 绑 `*param_1 + 0x1c0` —— 那是 vtable 槽 **`+0x1b8` 的 methodInfo**，`.c:24` 那颗
     `OnPointerClick` 自己调 `window.Close()` 走的也是 `+0x1b8` ⇒ **同一个槽 = `Close`**）。
   ⇒ **原版点暗底 = 关窗**。已改回 `MenuDraw.ShadeHit(…, () => Close(), …)`（零行数变化）+ 文件头就地订正。
2. **`MediaTab.SetupVideoSettings` 的旧「判据要点」是错的**：账里原写「PC 才显示的视频设置段」。
   实读方法体（🥈 `MediaTab.cs:70-129` + 🥇 `MediaTab__SetupVideoSettings.c:1-99`）**没有任何平台判断** —— 它只做「清空 + 加两条 I2 选项（`.c:40` / `:59`，`AddOptions` 在 `:82`、`set_value` 在 `:88`）+ 设选中值」。
   （"PlatformBasedComponents" 那层若存在也在 prefab/别处，不在这方法里。）已在账里订正。
3. **`MediaTab__OnSetup` 末尾也调 `SetupVideoSettings()`**（🥇 `MediaTab__OnSetup.c:89`）——
   即原版 `SetupVideoSettings` **跑两次**（`OnSetup` 一次 + 每次 `OnOpen` 一次）。我们的等价物是
   "建窗一次 + 每次 `OpenTab` 经 `RefreshTexts`/`_onLabels` 现读一次" ⇒ 净效果相同。
4. **`MatchType.Campaign(130)` 在客户端【没有开战入口】—— 战役是一条奖励轨，不是一局战斗**（这条回答上一路的 §四·2）。
   证据链：🥈 `CampaignWindowTab.cs:676-720`（`OnNodeClicked`，🥇 `decomp_full/CampaignWindowTab__OnNodeClicked.c` 同形）= 组 `CampaignRewardsWindowContext`（`availablePoints` /
   `nodeCost` / `state == unlocked`）→ **开奖励窗**；我们同形（`CampaignTab.cs:1153-1178` `ClickNodeForTest`）。
   全仓产线**没有任何一处**把 `GameMode.Campaign` 写进 `PlayState`（`RuleCore.cs:13587`、`BattleContext.cs:915`
   都写着"没有战役关"）。⇒ 上一路那 4 条 `C`（`LandWarlordsInCampaign` / `TimedCampaignSetup` /
   `TimedCampaignStartSequence` / `ShowHeroesIntroMessageCampaign`）**很可能应判 D**（"原版客户端里到不了"），
   而不是"落点不在我白名单" —— 建议调度台按 `铁律 11 例外①`（原版本身没有这条路）复核后再定，⛔ 别直接照抄我的判断。
5. **`ToastNotifications` 的 `ToastRootHeight` 上界注释是错的**（`:1075` 写"上界原来是 400px"，
   而 `:1073` 代码里的上界是 `100000f`）—— 已经是"改后值"，注释里那句是历史；**不影响行为**，但下个会话会误读。
6. **`PracticeArmySelectionButton` 与 `PracticeModePopup` 的职责对调**：原版每格的 `Highlight`/`Has Player Deck`
   写点在 **`PracticeModePopup.ChangeSelectedArmy`**（不在 `PracticeArmySelectionButton` 自己身上）——
   账里 5 条按类名找落点会一直找不到"同名方法"。已在落点列写明"等价实现 = `RebuildArmyCells` / `PickArmy`"。

---

## 七、给调度台的请求

1. **合并补丁**（同步点做，⚠️ 前缀写 `--only B8_AQ`，末尾**没有下划线**）：
   ```bash
   python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry  --only B8_AQ   # 已跑：更新 22 · 新增 0
   python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py        --only B8_AQ
   ```
   合并完按规矩把补丁归档到 `_parts/_merged_B8_AQ/`。
2. **跑 `SettingsScene.Run`（10 s）+ `MainMenuScene.Run`（24 s）+ `ShellScene.Run`（9 s）** 并加 §四 的 6 条草稿
   （其中 ①②⑥ 在 SettingsScene、③⑤ 在 MainMenuScene、④ 在 ShellScene）。**注意次序**：§四·④ 需要先加
   `ToastNotifications.InternalShowCount` 读口（§五·3）—— 要不要加请拍板。
3. **一条真 Play 待验（新增）**：`WhereToGetCardsPopup` 的压暗层**悬停会不会变暗**（§五·1）。
   这是本轮改动唯一没跑过的行为面。
4. **战役那 4 条 C 的定级复核**（§六·4）：如果"战役 = 奖励轨、客户端无开战入口"成立，按铁律 11 例外① 应判 **D**；
   我**没有**改那 4 行的状态（不在我这个域、也不该由我判）。
5. **账务一条**：`MediaTab.SetupVideoSettings` 的「判据要点」我已就地订正（"PC 才显示"→"建两条选项 + 设选中值"），
   若调度台另有口径请覆盖我这一格。
6. **`WhereToGetCardsPopup` 改动与 `N51③` 的冲突**：我推翻了 `N51③`（2026-10-17）的结论并给了反证链（§六·1）。
   如果调度台不同意，**必须给出比 `Awake` 加监听器更强的反证**（例如反汇编 `+0x1c0` 那个槽确认它不是 `Close`），
   ⛔ 不要只按"上一批这么定过"回退。
