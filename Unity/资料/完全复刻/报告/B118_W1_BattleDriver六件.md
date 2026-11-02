# B118-W1 · 执行写手报告（`BattleDriver` 六件）

> 2026-11-02。独占文件：`CardPresentation/Battle/BattleDriver.cs` · `Core/CardFeel.cs`（**零改动**）·
> 点击音涉及的 HUD 面板件（`SettingsPanel` / `CardChoicePanel` / `ChatPopupPanel` / `CardDisplayWindow` / `ReplayBar`，
> 均在 `文件所有权_当前.md` §六 里**不在别人名下**）。新建 `CardPresentation/Editor/B118W1_Assert.cs`（未接线）。

## 逐件

### B117-T02 · 42 处战斗按钮点击音
- **结论**：**已做完我能碰的全部 17 处挂载**（7 + 5 + 2 + 1 + 1 + 1）。原版口径 = `battlearena1` 里 **39 颗 `EverguildButton` 逐颗现读**
  （本代理自己解析 `MonoBehaviour/*.json` 的 `useDefaultAudioClipIfNull` / `m_onClickSound` + 反查 GameObject 父链）：
  默认音 **33** · 自带 cue **4** · **静音 2**（`Voice Over Button` MB 4852 / `Continue Button` MB 4897，两者 `useDefaultAudioClipIfNull = 0`）。
- **改动清单**
  - `BattleDriver.cs:29132/29427/29532`（`BuildHud`）+ `:30209/30244/30286/30353`（`BuildHudExtras`）— 7 颗 HUD 钮全部加 `WindowSounds.DefaultButtonCue`
  - `SettingsPanel.cs:1009` `Generic Close Button` → `DefaultCloseCue`（原版 pid 8084683923307806969 = `ButtonClick Back`）
  - `SettingsPanel.cs:1062` `Resign Button` → `NegativeCue`（pid 1387228150816026206 = `ButtonClick Negative`）
  - `SettingsPanel.cs:1145 / :1185` `Auto Zoom Toggle`（+label）→ `SelectToggleCue`（pid 8881786964274599484 = `Select Toggle`）
  - `SettingsPanel.cs:2193 / :2197` `ChatToggle`（+label）→ `SelectToggleCue`（同一 pid）
  - `SettingsPanel.cs:1100` `SkipTutorial Button` → `DefaultButtonCue`（原版 MB 4945 无自带 cue）
  - `CardChoicePanel.cs:609` —— 只有 `HideChooseButton`（MB 4387）带默认音；`Continue Button` / `BG` / `ContinueButton` **保持静音**
  - `ChatPopupPanel.cs:542` —— 六颗 `ChatButton (0..5)`（MB 4585/4652/5103/4985/4669/4981，全默认档）加 `DefaultButtonCue`
  - `CardDisplayWindow.cs:619 / :680` —— **保持静音**并就地写明判据（`cdw_background_close` 非按钮；`Voice Over Button` 是原版静音档）
  - `ReplayBar.cs:261` —— 上一波已带 cue，本波**未动**
- **验收**：`B118W1_Assert` A 组 **13 条**（A0 前提 · A1 七颗全带 · A2a–A2f 设置面板六处 · A3a/A3b 选卡面板一响三静 ·
  A4 聊天六颗 · A5a/A5b 详情窗两处必静 · A6 四条 cue 名逐字对上）。🧨 A3b 与 A3a 互为反相 ⇒ 一刀切 / 全不加必红。
- **原版判据出处**：`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_{4038,4356,4387,4468,4643,4784,4837,4852,4895,4897,4945,5244,5291,4733}.json`；cue 名表 = `Shell/WindowSounds.cs:13-16`。

### B117-T06 · 回放三缺
- **结论**：**三件全做完**。
- **改动清单**
  - ① 按下态换图：四张 `_pushed` PNG 已导进 `Resources/Art/ui/`（`40K_replay_bt_{restart,play,pause}_pushed.png` +
    `40K_replay_bt_next_ushed.png`，逐张带 `.meta`）；`ReplayBar.cs:230-233`（`AttachClicks`）+ `:413`（`BindPressedFeedback`）
    —— `BattleButtonFx.Swap(piece, …, normal, normal, pressed)` + 三条 `EventTrigger`（Down/Up/Exit）。
  - ② `BattleDriver.cs:8718`（`ApplyPendingReplay` 的 `Btn.Step`）补 `SetReplaySpeed(ReplayFastForwardScale)`；`Play`/`Pause`/`Replay` 三支补 `ResetReplaySpeed()`。
  - ③ `BattleDriver.cs:1397` 终局 `!same` 补 `Debug.LogWarning`（原 `Debug.Log` 保留一行摘要）。
- **验收**：`B118W1_Assert` B 组 **5 条** + C 组 **4 条**。🧨 B1（四张名字互不相同）· B3（悬停图 = Normal，原版 `m_HighlightedSprite` 全等于 Normal）·
  C4（`!same` 必须是 Warning）。
- **原版判据出处**：MB 5177/4109/4709/5265 的 `m_SpriteState`；`ClickNextStepReplay.c`（`DAT_1834b3164` = 15.0）；红线「不许静默失败」。

### B117-T07 · 换牌收尾逐张节奏
- **结论**：**逐张节奏做了，逐张补牌没做**（引擎白名单外），差异已如实记账。
- **改动清单**：`BattleDriver.cs:9117`（`OnMulliganDone`）起 `BeginMulliganSettle`；`:6868`（`LateUpdate`）挂 `TickMulliganSettle`；
  `:27477`（`SyncHand`）演出期间直接返回；类尾 `CaptureMulliganSettleViews` / `BeginMulliganSettle` / `PlayMulliganSettleCard` /
  `TickMulliganSettle` / `FinishMulliganSettle`（`:40942-41010` 一带）。
- **做到**：逐张 `CardFeel.ReturnToDeck` → 每张等 `timeToDraw`(0.15) → 全部走完再等 `timeToDraw × 0.5` → 才放开输入并开打。
- **没做到**：① **逐张补牌** —— `RuleCore.Mulligan`（`RuleCore.cs:454`）一次把整批换完再一次性补抽，拆它要改 `RuleEngine/`（不在本批白名单）
  ② **面板延后消失** —— 我们在开始飞回**之前**就 `Close()`（否则 `MulliganPanel.ReapplyRowPoses` 每帧把卡按回换牌区）。
  玩家看到的次序 = 「面板收掉 → 被换的牌逐张飞回牌堆 → 新牌补进来」。
- **验收**：`B118W1_Assert` D 组 **7 条**。🧨 D2/D3（时长必须来自 `CardFeel.DealDurationFoe` = 0.15）· D4（尾拍 × 0.5）。
- **原版判据出处**：`decomp_full/PlayerHand._FinishMulligan_d__52__MoveNext.c` 全文。

### B117-T08 · 疲劳横幅 0.5 s 节拍
- **结论**：**已做**（节拍那一半）；反推那半未动（要引擎新事件，白名单外）。
- **改动清单**：`BattleDriver.cs:30963`（`NoteFatigueBanners`）记下弹过横幅的那条 `Hit`；`:19617`（`PlaySignals` 排时间线）
  在同一物理行的 `t += beatGap` **之后**再加 `FatigueBannerHold`；类尾 `const float FatigueBannerHold = 0.5f` + 字段 `_fatigueHoldEvent`。
- **为什么用对象身份不用下标**：`NoteFatigueBannersForTest` 喂的是合成事件，记下标会让**下一批**同下标事件凭空多 0.5 s。
- **验收**：`B118W1_Assert` E 组 **4 条**。🧨 E3（0.5 s 必须加在**这一拍之后**，与原版「先伤害后等」同序）。
- **原版判据出处**：`BattleManager._ResolveFatigue_d__587__MoveNext.c`；`read_literal 0x1834b2bb4 = 0.5f`。

### B117-R71 · 出牌记录卡名
- **结论**：**已做**。
- **改动清单**：`BattleDriver.cs:12101` 日志行 `Zh(e.CardId)` → `Zh(e.CardId, e.Player)`；`:12100` 的 `TargetText` 同理用 `ev.TargetPlayer`；
  类尾新增 `Zh(string name, int player)`：阵营取 `PlayerState.Warlord.Card.Faction` → `PoolFor(_pool, faction)`（阵营卡池唯一解析口）
  → 非自造阵营**逐字节退回旧口径**。
- **验收**：`B118W1_Assert` F 组 **6 条**。🧨 F5（一参写法不许回到日志行上）。
- **原版判据出处**：`RuleEngine/Data/StarterCards.cs:56`（起始卡 `Falcon`，无 `NameZh`）；`CardDatabase.Find(pool, name)`。

### B117-T12 · 手牌区 Small Screen UI × 1.365
- **结论**：**已做**（折算进 `HandLayout` 的四个字段）。
- **改动清单**：`BattleDriver.cs:4980`（`Begin`）调 `ApplySmallScreenHandScale()`；类尾 `:41065` 实现 ——
  开关开时把 `cardScale` / `baselineY` / `hoverLift` / `maxLayoutSizeSmallScreen` 一起 ×1.365（基值只抓一次 ⇒ 幂等）。
- **验收**：`B118W1_Assert` G 组 **5 条**。🧨 G2（恒 1.0 与恒 1.365 都过不了 G1+G2）· G4（四个字段都乘 m）。
- **原版判据出处**：`decomp_full/PlayerHand__Initialize.c` 全文；13/13 场 `smallScreenSizeMultiplier = 1.365`。

## 账补丁（已写 `账/_parts/B118_W1.tsv`，11 行 · 11 列 · 首行表头）
覆盖：T02 五条（HUD / 设置 / 选卡 / 聊天 / 详情窗）· T06 两条（Step 的 timeScale · 四颗按下态）· T07 一条（`B` 档，差异写明）·
T08 一条 · R71 一条 · T12 一条。`状态` 列除 T07 为 `B` 外全 `A`。

## 顺手发现（⛔ 都在别人名下，未改）
- `MultiCardDisplay.cs:632` —— `_bar`（原版 `Generic Multi Card Display Combat/BattleContinueButton/Button`，MB 5115，
  `useDefaultAudioClipIfNull = 1`）**仍走 2 参 `Attach` ⇒ 静音**，缺一声。**W3 的文件**，补丁 = 该行加 `, WindowSounds.DefaultButtonCue`。
- `MulliganPanel.cs` —— **全文件 0 处 `BattleHudButton`** ⇒ 换牌面板两颗原版钮（`HideMulliganButton` MB 4464 ·
  `MulliganContinueButton/Button` MB 4025，**都 `useDefaultAudioClipIfNull = 1`**）**从头到尾没有点击音**，且点击走的是另一条手搓路。**判据已定、无写手占用**，建议另派。
- `SettingsPanel` 的 `Dark Shade` / `Blocker` / `Language Selector` / `Language Row N` —— 原版都不是 `EverguildButton`
  （`Language Selector` 是 TMP `Dropdown`，`LanguageSelector.Initialize` 里**无任何音频调用**）⇒ **保持静音是对的**，已就地记。
- `AlliancePanelWindow.cs:920`（W3）`_shade` = 联盟窗压暗层，原版不是按钮 ⇒ 静音正确，**不需要改**。
- `Editor/BattleScene.cs` 里 `BattleHudButton.Attach` **全是注释**（0 处活代码）⇒ T02 在那份文件里没有待接线项。

## 没做 / 卡住的地方
- **T07 逐张补牌**：要拆 `RuleCore.Mulligan`（`RuleEngine/Core/RuleCore.cs:454`，白名单外）。
- **T08 疲劳判据本身**：仍靠 `EvtKind.Hit` 六道闸反推，要引擎侧单开一种事件（`RuleEngine/` 白名单外）。
- **T02 剩余 2 处**：`MultiCardDisplay.cs`（W3）· 换牌面板那两颗（无写手、未派）。

## 三道闸
- `TMPDIR=/tmp/wf_w1 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑 4 次，早期几次的红全在 `UnitChatPanel.cs` = W3 在飞）。
- `python -I -X utf8 工具/check_aggregate_lines.py <我改的 6 个 .cs>` ⇒ **PASS**（聚合行 0 · 违规 0）。
- **行号守恒**：`SettingsPanel` 2906 · `CardChoicePanel` 1501 · `ChatPopupPanel` 1591 · `CardDisplayWindow` 2735 · `CardFeel` 4362 **与 HEAD 逐行相同**；
  `BattleDriver` 40960 → 41178、`ReplayBar` 380 → 474，**新增全部在类尾**（`BattleDriver` 最大账落点 `:40001` 及 `ReplayBar :162/:330`
  逐字复核未漂；`ChoicePanelPool` 那个第二顶层类在 40001 之后、无任何账落点）。
- 行尾（`io.open(p,'rb')` 逐字节数）：`BattleDriver.cs` **CRLF 41178 / LF 41178 / 裸 CR 0**（**整篇一致 CRLF**，新增段同样 CRLF）；
  其余 5 个 `.cs` 与 `B118W1_Assert.cs` **CRLF 0 / 裸 CR 0**（纯 LF）。**没有一个文件被翻行尾**。

## § 第二轮补丁（点击音两处）

调度台放开 `MultiCardDisplay.cs` / `MulliganPanel.cs` 之后补完的 T02 剩余两处。

### 1. `Battle/MultiCardDisplay.cs`（现读后改，W3 已改过该文件）
- `MultiCardDisplay.cs:632`（`BuildContinueBar`）—— `BattleHudButton.Attach(_bar, RequestCloseByUgui)` → 加第 3 参
  `WindowSounds.DefaultButtonCue`。判据：原版 `Generic Multi Card Display Combat/BattleContinueButton/Button`（MB 5115，
  `useDefaultAudioClipIfNull = 1`、`m_onClickSound` 空）⇒ 默认按钮音。
- **同文件 `:596` 的 `_bgClose`（`Menu Dark Background`）保持静音**：原版那是压暗层、不是 `EverguildButton`。
- 行数 1280（**1:1 就地改，零行数变化**）；账落点 `27/33/153/179/190/199/204/348/479/751/844` 逐条复核**未漂**。

### 2. `Battle/MulliganPanel.cs`（现读后改，W2 已改过该文件）
- 全文件原来 **0 处 `BattleHudButton`** ⇒ 两颗原版钮从来没有点击音。**不改命中链**（那是 `T09` A4 的活），
  只在 `HandleClick` 的两支**动作之后**各补一声（原版 `EverguildButton.OnPointerClick`：先动作、后 `PlayButtonSound`）：
  - `MulliganPanel.cs:1558`（`HandleClick` 的 `HitDone` 支）—— `OnDone(marks)` 之后 `PlayButtonCue("MulliganContinueButton")`
  - `MulliganPanel.cs:1570`（`HandleClick` 的 `HitEye` 支）—— `SetContentHidden(!_contentHidden)` 之后 `PlayButtonCue("HideMulliganButton")`
  - 类尾 `:2001` 新增 `static void PlayButtonCue(string node)` → `WindowSounds.PlayFx(WindowSounds.DefaultButtonCue, …)`
- 判据：`Mulligan/ButtonsGroup/HideMulliganButton`（MB 4464）与 `Mulligan/ButtonsGroup/MulliganContinueButton/Button`
  （MB 4025）**都是 `useDefaultAudioClipIfNull = 1` 且 `m_onClickSound` 空**。
- 行数 1983 → **2006**（+23，**全部在类尾**）；账落点 `131/251/458/965/973/979/1011/1171/1287/1439/1479/1516/1550` 逐条复核**未漂**。
- ⚠️ `WindowSounds.PlayFx` 内部对「cue 表里查不到」**只报一次**（不刷屏）；同 cue 0.1 s 内不重播（原版 `AudioCue.timeToPlayAgain`，不是失败）。

### 断言（`Editor/B118W1_Assert.cs` 追加 5 条，A 组内）
`A7a` 前提（读得到 `MulliganPanel.cs`）· `A7` 两颗钮各播一次 · `A8` 🧨 那一声必须是 `WindowSounds.DefaultButtonCue` ·
`A9` 多卡窗 `_bar` 带 cue · `A10` 🧨 多卡窗 `_bgClose`（压暗层）**不带** cue —— A9 与 A10 互为正反，一刀切必红。
断言文件 386 → **413** 行，仍未接线。

### 三道闸（第二轮）
- `TMPDIR=/tmp/wf_w1 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（中途一次 `CS0136`：断言里 `bar` 与 A3 组重名，改名 `mcdBar` 后 0/0）。
- `check_aggregate_lines.py MulliganPanel.cs MultiCardDisplay.cs B118W1_Assert.cs` ⇒ **PASS**（聚合行 0 · 违规 0）。
- 行尾：两个 `.cs` 与断言文件全部 **纯 LF · 裸 CR 0**。

### 顺手发现（第二轮）
- `MulliganPanel` 的两颗原版钮**仍是手搓矩形命中**（不是 uGUI）。点击音已补齐，但「uGUI 单一所有者」这条
  （`T09` A4 的口径）在这一面板上**仍未做** —— 属另一件活，建议单独记账（`Batt`/`HUD` 域）。
- `MultiCardDisplay` 的 `_continueBtn`/`_bgCloseBtn` 都已走 uGUI，但**没有按下态换图**（原版 MB 5115 `m_Transition` 未逐槽核）。
