# B8_AF —— `CardPresentation/Battle/**` 剩余缺口（执行代理报告）

> 白名单：`d:/4/Unity/MyGame/Assets/CardPresentation/Battle/**`（**除 `BattleDriver.cs`**）。
> 本批**只改了一个 `.cs`**：`BattlePostFx.cs`（其余缺口全部卡在白名单外或远端数据，逐条见 §④/§⑥）。
> 类型检查：`TMPDIR=/tmp/wf_b8af bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（2026-10-26 收工前最后一跑）。

---

## ① 结论

1. **简报里那两条「已查清的转派项」在本批开工时就已经做完了**（不是漏做，是简报快照过期）：
   - ① `CardDisplayWindow.ShowBattleCard` 的伏击支**已调** `SlotViews[0].SetStatsHidden(true, hideArmourCell: true)` ——
     落点 `CardPresentation/Battle/CardDisplayWindow.cs:1887`；账 `战斗界面_HUD.tsv:307` 已是 **A**（备注里就写着「本代理 2026-10-26 把 ① 藏数值接上」）。
     同批的 `:58`（`SetEmptyRarityStars` = `show` 那一条）也已 A。
   - ② 技能面板那一侧的口径注释**已在** `CardPresentation/Battle/SkillPanel.cs:2069`
     （「⛔ **别与手牌抬牌那一侧抽共用函数** —— `desc + "<i>" + 后缀 + "</i>"` 无空格 + 斜体」）。
     **手牌那一侧（`Hand/CardInteraction.cs`）没有这行注释** ⇒ 不在本代理白名单，见 §⑥-3。
2. **本批真正落地的是 `BattlePostFx` 的 `ResetBloom` / `ResetLUT` 两条 1.0 s 过渡**（账 `特效动画.tsv:340/341`，**C → A**）——
   原版这两条一条走 `DOTween.To`（`OutQuad`）、一条是 `LUTBlender.Update` 的**手写线性斜坡**，我们照两者的形状分别实现。
3. 账补丁 `账/_parts/B8_AF.tsv` **12 行**（2 行升 A · 1 行 D 指针 · 2 行 A 指针 · 5 行判据/结论订正 · 2 行封锁条件写清）。
   `merge_ledger_parts.py --dry --only B8_AF` ⇒ **更新 12 · 新增 0 · 0 错**。

**白名单内剩余 C 行只有 1 条**：`CardDisplayWindow.SetupTutorialObj`（远端 `TutorialEvent` 数据 + 父链缩放两处拿不到 ⇒ 见 §④-3，**不是「不值得做」**）。

---

## ② 逐条证据

### 2.1 `PostFXController.ResetBloom(bool instant)` → **A**

- **判据（🥇 `decomp_full/`）**
  - `PostFXController__ResetBloom.c:3-61`：`instant=0` ⇒ `DOTween.Kill(bloom,0,0)` → **两条**
    `DOTween.To(getter, setter, 目标, TIME_TO_RESET)`，目标 = `*(this+0x38)` / `*(this+0x3c)`
    （= `Start` 缓存的 `bloomOriginalIntensity` / `bloomOriginalThreshold`）；
    `instant=1` ⇒ 先判 `bloom+0x48` 写 intensity、再判 `bloom+0x40` 写 threshold，任一为空 ⇒ `FUN_1803f47a0`（NRE）。
  - `PostFXController__.cctor.c`：`TIME_TO_RESET = 0x3f800000` = **1.0f**。
  - 🥈 同体：`cpp2il/out_cpp2il_dev_ilspy/PostFXController.cs:128-168`。
- **我们的落点**：`BattlePostFx.cs:363-368`（`Reset` 纯转发）· `:461-479`（`ResetBloomCore`）
  · `:534-540`（`CompleteResetRamps`）· `:542-560`（读口）。
- **机制差异（G2）**：① 原版那两条 tween **没有 target**（`TweenerCore.target == null`）⇒ `DOTween.Kill(bloom)`
  其实杀不到东西 —— **照抄**（同 `BattleCameraSreenSize` 文件头 F 那条已记录的先例）；
  ② 缓动：原版没写 `SetEase` ⇒ 走 DOTween 全局默认；本工程 `Resources/DOTweenSettings.asset` 的
  `defaultEaseType = 6` = **OutQuad** ⇒ 我们同样不写 `SetEase`（不是我们挑的曲线）；
  ③ `instant=1` 时我们额外把两格 `overrideState` 置真 —— 那是本类既有的 `BloomAim` 口径（净结果相同）。
- **可观察量（G3）**：`BloomIntensityNow` / `BloomThresholdNow` / `ResetBloomRamp01`。
  原版侧期望：`instant=false` ⇒ 1.0 s 内按 OutQuad 从当前值走到缓存原值；`instant=true` ⇒ 当场等于缓存值。
  **我们侧实测值：本批未跑宿主**（子代理不跑 Unity）⇒ 读数口已备（`CompleteResetRamps()` 让批处理也能读）。

### 2.2 `PostFXController.ResetLUT(bool instant)` → **A**

- **判据（🥇）**：`PostFXController__ResetLUT.c:6-33`（`timeToBlend = instant ? 0 : TIME_TO_RESET`；
  `LUTBlender == null` ⇒ NRE；`SetTargetLUT(originalLUTTexture)` → `timeToBlend` / `currentTime=0` /
  `doAnimatedBlend=1` / `enabled=1`）· `LUTBlender__SetTargetLUT.c:1-26`（`Blit(combined→working)` +
  `SetTexture("_LUT2", 目标)`）· `LUTBlender__Update.c:5-22`（`currentTime += dt; DoBlend(currentTime/timeToBlend)`，
  到点 `doAnimatedBlend=0` + `enabled=0`）· `LUTBlender__DoBlend.c:1-32`（v 夹 [0,1] → `SetFloat("_Blend")` → `Blit(null→combined)`）。
- **我们的落点**：`BattlePostFx.cs:363-368` · `:486-522`（`ResetLutCore`）· `:524-531`（`LutRampComplete`）
  · `:342-351`（`BlendTo` = `DoBlend` 落点）· `:558`（`ResetLutRamp01`）。
- **机制差异（G2，逐句对过 4 份 `.c`，共 113 行）**：
  ① **「快照」我们做得隐式** —— 原版 `SetTargetLUT` 先把 combined 拷进 working（`_LUT1` 的宿主），
  但模块每次效果起手都会把 `_LUT1` 改写成 `customLUT1`（`AnimFXModulePostProcess.cs:224-231`）
  ⇒ 真正参与插值的是「**当前基准 LUT**」，不是那份快照；我们的 `_LUT1` 一直是「当前基准」（`LutBlend` 的
  `setTextures` 支负责写）⇒ **净等价、不建 workingRT**（与文件头 ④ 那条既有偏离一致）。
  ② 斜坡用一条 `Ease.Linear` 的 tween 当 `currentTime` 的泵（原版是 `LUTBlender` 那件 MonoBehaviour 自己 `Update` 累加，我们没建那件）
  —— **线性、不带缓动**，与原版同。
  ③ 材质 / 原 LUT 缺失 ⇒ **退回直接落位并出声**（原版是 NRE）。
  ④ 复位完成后把 `_LUT1`/`_LUT2` 都归位成原 LUT ⇒ 模块空闲时每帧那一发 `BlendTo(0)` 不会把结果掀回去。
- **可观察量（G3）**：`ResetLutRamp01`（0→1）+ `BlendNow`（`_Blend` 实测值）。
  原版侧期望：`instant=false` ⇒ `_Blend` **线性** 1.0 s 到 1，随后 `doAnimatedBlend=0`；`instant=true` ⇒ 下一帧即 1。
  **我们侧实测值：本批未跑宿主**。

### 2.3 复核：简报第 1 条（伏击支藏数值）
- `Battle/CardDisplayWindow.cs:1870-1895` = `ShowBattleCard` 全文：①`:1887` ②`:1877` ③`:1875-1876`
  ④`:1879` ⑥`:1886` ⑦`:1891` ⑧`:1893`，读口 `:1899 MainCardShowsAmbushArt`。
- 判据 `cpp2il/.../CardDisplayWindow.cs:657-708`（🥈）逐条对得上；账 `战斗界面_HUD.tsv:307` = **A**。
- **本批不改**（已满足），只在报告里留证。

### 2.4 复核：简报第 2 条（两处文案口径）
- `Battle/SkillPanel.cs:2069` 已写死：本处 = `desc + " " + 后缀`（**空格 + 纯文本**、与描述同一颗 TMP）；
  手牌侧 = `desc + "<i>" + 后缀 + "</i>"`（**无空格 + 斜体 + 带换行 desc**）⇒ ⛔ 别抽共用函数。
- `Hand/CardInteraction.cs` 里**没有**对应注释（全仓 `grep "别抽共用"` 只有 SkillPanel 那一处）⇒ §⑥-3。

### 2.5 判据订正：`CardDisplayWindow.SetupTutorialObj`
- 🥇 `decomp_full/CardDisplayWindow__SetupTutorialObj.c:26-72`（闸在 `:44-58`）+ 🥈 `CardDisplayWindow.cs:1097-1141`。
- **两处订正（铁律 5）**：
  1. 闸 ④ 原来写「事件里那个 Dictionary 的**某 key 计数** < 6」→ 实读是**那个 Dictionary 的 `Count` < 6**
     （`FUN_1816aaff0(dict, RuntimeMethod*)` 的返回被 `dict.Count` 用掉；两档反编译都写成 `.Count`）。
  2. `tutorialObjCost.SetActive(...)` 的判据是 `cardType != Hero && **manaCost >= 0**`
     （`.c` 里 `~(uint)manaCost >> 31` = 非负；🥈 也是 `~x >> 31`）—— **不是 `> 0`**。
     ⇒ `manaCost == 0` 的非督军卡原版**也亮**「Energy Cost」。
- 为什么本批不建：见 §④-3。

### 2.6 订正：`PlayerHand.MoveCardToMulliganPos`（B8_W 的数值结论站不住）
- 我**自己复读了序列化**（`assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/`）：
  `MB_4881`（换牌）与 `MB_4052`（选牌）**逐字段相同** —— `m_scale = 183.41` · `m_betweenElementsSpacing = 502.2` ·
  `m_maxLayoutSize = 0.98` · `m_numberOfCardsForMaxHeight = 7` · `m_maxHeight = 0.44` · `m_useRotation = 0`。
  （对照：我方手牌 `MB_5271` = 0.73 / 1.45 / 12 / 0.7 / rotation=1；敌方 `MB_4053` = 0.54 / 0.6 / 10 / −0.78 / invert=1。）
- **但**「原版卡宽 = 2.0927×183.41 = 383.8 px」这一步**没有根据**：既有 T10/A1749 的证据链里，
  **选牌**那一侧运行期探针量到中心距 **502.2 px**（说明那里 1 单位 = 1 px），而**换牌**那一侧
  用户实拍量到卡宽 **240 帧px = 292 设计px**（`MulliganPanel.cs:614-628` 有逐像素边界 372–612 / 700–940 / 1028–1268）。
  `CardsHorizontalLayout.GetPosition` 还吃 `lossyScale` / `maxLayoutSize` / 压缩分支 ⇒ **同一组序列化值不保证同一输出**（铁律 5·c）。
- ⇒ **代码不动**（`CardWidthPx = 292` / `RowSpacingPx = 414` 保持），把账里那句「偏小 24%/18%」改成
  「两套判据冲突（序列化 502.2 px vs 实拍 399 设计 px），差额 = 父链 `lossyScale ≈ 0.794` 未逐节读，**实拍优先**」。
  另外两条顺带证实：换牌行 ≤4 张 ⇒ `3×414+292 = 1534 < 0.98×1920` ⇒ **压缩分支不会触发**；`m_useRotation = 0` + 实拍共线 ⇒ **我们的平排是对的**。

### 2.7 补全：`PlayerHand.FinishMulligan` / `FinishMulliganFinalPhase` 的常量
- `PlayerHand._FinishMulligan_d__52__MoveNext.c:28-171`：抽牌等待 = `WaitForSeconds(TimeToDrawCard(isPlayer) **× 2.0**)`
  （`DAT_1834b2bbc`，`工具/read_literal.py` 现读 **2.0**）；淡出 = `FadeCardItems(CardUI, **0**, **0.3**)` + `WaitForSeconds(0.3)`
  （`DAT_1834b2dc8` = **0.3**）。
- `PlayerHand__FinishMulliganFinalPhase.c:17-35`：`if (!isPlayer) return;` 之后每张 `FadeCardItems(cardUI, **1.0f**, **0**)`
  （`DAT_1834b2bb8` = 1.0）—— **实参位置与上面那支不同，别互相照抄**。
- 另：`FinishMulligan` 的循环里「弃掉那一支」**不 `i++`**（`ReplaceMulliganCard` 之后同一下标重判）。
- 🔴 **归属订正**：这两条的落点原来都写 `Battle/MulliganPanel.cs`，但**整条收尾链在 `BattleDriver.cs:8339-8362`**
  （那段文件头自己写着「我们省掉的（全是表现层）……语义一致、节奏不同」）。`MulliganPanel` 只管行位 / 钮 / 压暗 ⇒ **归 E5**。

### 2.8 解出但还不能抄：`ScenarioMaterialFader.doFade = !doFade`
- 🥇 `ScenarioMaterialFader__DoScenarioBlend.c` 两个分支末尾各一句（`:78` / `:110`）：
  `*(bool*)(this+0x40) = !doFade`；🥈 `ScenarioMaterialFader.cs:374-375 / :392-395`。
- `Update` 首句 `if(!doFade) return;`（🥈 `:63`）⇒ **奇数次调用才真的淡、偶数次把淡取消**
  （连 `currentBlendTime` 都不递减 ⇒ `OnComplete` 永不触发）。
- ⇒ **抄之前必须知道「每条环境调几次」**；我们的计数探针 `BlendCalls / BlendCallsForward / ResetBlendCallCounters()`
  已经在 `Battle/ScenarioMaterialFader.cs:80-88`，只差**一次 Unity 运行**（子代理不跑 Unity）⇒ §⑥-5。

---

## ③ 改了哪些文件、哪些行、加了多少

**代码：只有 1 个文件** —— `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/BattlePostFx.cs`
（**423 → 562 行**；`git diff --numstat` = `+159 / −20`；行尾仍是 **LF、零裸 CR**）。

| 位置（新行号） | 改了什么 | 行数变化 |
|---|---|---|
| `:74` | `using WarpforgeVFX; using DG.Tweening;` —— `SetEase`/`IsActive`/`Complete`/`Elapsed` 是**扩展方法**，必须 `using`（同 `BattleCameraSreenSize.cs:89`） | 同一行追加 |
| `:298-302` | `LutBlend` 首句加「复位斜坡在走时丢掉空闲那一发（`lutBlend == 0`）」守卫 | **零行数变化**（改在原文那两行上） |
| `:342-351` | `BlendTo` 记 `_lastBlend`（读口 + 复位起点） | 同一行 |
| `:363-368` | `Reset(instant)` 改成**纯转发**（原 356-369，把机制搬到类尾两个 core） | −4 |
| `:378-419` | `Detach` 尾部 `Kill` 三条 tween + 清斜坡状态 | 同一行 |
| `:423-560` | **新增（类尾）**：`TimeToReset` 常量 · `_bloomTweenI/_bloomTweenTh/_lutTween/_lutRampT/_lutRampActive/_lastBlend/_lutRampFallback` · `ResetBloomCore`(461-479) · `ResetLutCore`(486-522) · `LutRampComplete`(524-531) · `CompleteResetRamps`(534-540) · 读口(542-560) | +140 |

**行号位移（供账/工具对表）**：`Attach :131` **未变** · `LutBlend :300` **未变** · `BlendTo :342` **未变** ·
`Reset :356 → :363` · `SetOriginalLut :373 → :370` · `Detach :380 → :378`。

**账**：`d:/4/Unity/资料/完全复刻/账/_parts/B8_AF.tsv`（11 列 · LF · 12 行 · 0 裸 CR）：

| # | 域 | 类.方法 | 状态变化 |
|---|---|---|---|
| 1 | 特效动画 | `PostFXController.ResetBloom(bool instant)` | **C → A** |
| 2 | 特效动画 | `PostFXController.ResetLUT(bool instant)` | **C → A** |
| 3 | 特效动画 | `LUTBlender.OnDestroy()` | A（落点 `:380 → :378`） |
| 4 | 特效动画 | `PostFXController.ResetAll(bool instant)` | D（指标 `:290-303 → :363-368`） |
| 5 | 特效动画 | `ScenarioMaterialFader.DoScenarioBlend(...)` | C（把 `doFade` 机制写准 + 阻塞条件写成「一次测量」） |
| 6 | 特效动画 | `ScenarioMaterialFader.Update()` | C（同上，指向 5） |
| 7 | 战斗界面/HUD | `CardDisplayWindow.SetupTutorialObj(...)` | C（两处判据订正 + 写清「为什么不建」） |
| 8 | 战斗逻辑驱动 | `PlayerHand.MoveCardToMulliganPos(...)` | B（**翻掉**「卡宽/间距偏小 24%/18%」） |
| 9 | 战斗逻辑驱动 | `PlayerHand.FinishMulligan(bool)` | B（补全常量 + 归属改 E5） |
| 10 | 战斗逻辑驱动 | `PlayerHand.FinishMulliganFinalPhase()` | B（补全 1.0f + 实参位置差异 + 归属改 E5） |
| 11 | 特效动画 | `AnimFXModulePostProcess.ResetPostFX(bool)` | A（下游两条过渡已落地，指针 `:290 → :363-368`） |
| 12 | 特效动画 | `AnimFXModulePostProcess.DoLUTAnim()` | A（次要指针 `:234 → :300`） |

自检：`merge_ledger_parts.py --dry --only B8_AF` ⇒ 更新 12 · 新增 0 · 无变化 0 · 0 错；
另用一份「同规则」脚本（verify_annot 的同名/异名判据 + check_a_vs_diff 的强词判据）扫过本补丁 ⇒ **problems = 0**。

---

## ④ 没查清的部分（如实记，⛔ 不拿猜测填空）

1. **DOTween 的全局默认缓动**：本工程 `Resources/DOTweenSettings.asset` 的 `defaultEaseType = 6`（= OutQuad）
   是**本工程自己生成的**资产（`Editor/DOTweenSetup.cs` 建的）⇒ 它只证明「我们这个工程的默认是 OutQuad」。
   **原版工程的 DOTweenSettings 没核过** —— 与 `WFModulePostProcess.Ease` 那条注释是同一处「我们定的」。
2. **`CameraVerticalFramer.CalculateFraming` 那 3 条 C 行**（本批未动，理由）：
   (b)「把 `originalSensorSize`/`originalLensShift` 写回相机」在 `CombatAutoZoom.MeasureCardAreaGapWorld` 里
   是**换入/量/还原**（`try/finally`）实现的，`lensShift` 那半在两次 `ViewportToWorldPoint` 的差里正好抵消；
   (c)「结尾把 `sensorSize` 还原」同在该 `finally`。
   **真正的残差只有 (a)**：`playerCardAreaSizeHelper.sizeDelta.y` 要乘 `playerHand.CurrentSizeMultiplier`，
   而我们那一位恒 1.0（`CombatAutoZoom.CurrentSizeMultiplier`）——**M 的真值（谁写 `PlayerHand+0x3c`、写多少）查不到**，
   且我们战斗 HUD 没有 `smallScreenUI` 放大档（生产调用点只在菜单两处）⇒ 两个分支都取 1.0。
   ⚠️ 这三行**没有**改成 A：不是等价，是「默认档等价、小屏档查不到」。
3. **`SetupTutorialObj` 为什么不建**：
   ① 整条闸的数据源是 **LiveOps `TutorialEvent`**，本地**任何形态都没有**（`Shell/TutorialModePopup.cs:29` 已记
      「随 PlayFab userInfo 下发」）⇒ 在我们这个离线 build 里**恒不可达**，建出来是**永远不显示的死件**，也无法验收；
   ② 树根 `TutorialObjs` 的 rect 是 `stretch 锚点 + sd 3501.37×2291.01`，只有和**父链 lossyScale** 联立才成立，
      那一串**没有逐节读** ⇒ 照抄会把四条字摆错（铁律 3：别拿一个字段当参数）。
   ⇒ 建议留教程线：等它有了「教程在跑」的本地入参 + 父链 scale 实读之后一次建完。
4. **`m_maxHeight = 0.44`（换牌行的弧高）到底生不生效**：`m_useRotation = 0` 只说明「不转」，弧高是否仍作用于 y 未核；
   实拍三张卡共线（行心 514 ↔ 锚点 511）⇒ 观感上无差，但**机制未坐实**。

---

## ⑤ 顺手发现（不属于本批改动范围，但下一个会话需要知道）

1. 🔴 **`BattlePostFx` 里有一个「复位被下一帧掀回去」的隐患（已在本批收口）**：
   模块空闲时 `DoLUTAnim` 每帧都会发一发 `lutBlend == 0` 的 `LutBlend` ⇒ 会把 `ResetLUT` 的 1.0 s 过渡
   **当场盖掉**。原版靠 `LUTBlender.enabled` + 它自己的 `Update` 做唯一写者；我们加了「斜坡在走时丢掉空闲那一发」的守卫
   （`BattlePostFx.cs:302`，真正在播的效果照旧放行）。这条**只有跑起来才看得见**，静态读代码很容易漏。
2. **`BattlePostFx` 文件头第 ③ 条**写「`_Blend` 的过渡由模块每帧算」——这条**只对「效果播放」那条路成立**；
   `ResetLUT` 在原版是 `LUTBlender.Update` 的**独立斜坡**（与模块无关）。本批按后者实现，并在类尾写清。
3. 🔴 **DOTween 在本工程是可用且已在用的**（`Assets/Plugins/Demigiant/DOTween` · `Resources/DOTweenSettings.asset` ·
   `BattleCameraSreenSize.cs:245` 起真用 `DOTween.To`）⇒ ⛔ 别在任何地方再写「我们没有 DOTween」（本批一度这么写过，已改）。
   批处理里它不推进 ⇒ 要用 `SetUpdate(UpdateType.Manual)` + `DOTween.ManualUpdate(dt)`（`Editor/DOTweenSmokeTest.cs`）
   或 `Complete()`（本批给的是 `CompleteResetRamps()`）。
4. **账 `战斗逻辑驱动.tsv:1328` 的「数值差」是只读序列化推出来的**，与既有 T10/A1749 裁定 + 用户实拍冲突 ⇒ 已翻（§2.6）。
   提醒：`CardsHorizontalLayout` 是**同一组件不同实例输出不同**的典型（铁律 5·c 那一类），⛔ 别拿 `m_scale` 当 px。
5. **账 `战斗逻辑驱动.tsv:1309/1310` 的落点指错**（写 `MulliganPanel.cs`，实际在 `BattleDriver.cs`）⇒ 已改。
6. **`BattleDriver.cs` 的行号在漂**：账里 `ShowSkillPanel:15059` 现读 **`:15065`**、`ShowJustOne` 调用点账里 `:13654` 现读 **`:14646`**。
   ⇒ 请求一律**按符号名找**，别按行号。
7. **`DeckRuntime.Say` 那条「HUD 侧要有『无战斗上下文也能弹』的入口」其实早就有了**：
   `Shell/MessageToast.cs:212 ShowError(text, localize, fallback)`（静态、`Ensure()` 自己找壳/场景根），
   底下就是 `Battle/ErrorMessageBanner` 的 `MenuPreset` 档 ⇒ 卡组编辑那边**只剩一行接线**，不需要本代理再加东西。
8. `SetupTutorialObj` 的 `EnableTutorial` 在 13 场都是 1（`B8_U` 已核），但**闸 ② 的 LiveOps 事件**才是真正的开关 ——
   别把它当成「13 场都开着所以会显示」。

---

## ⑥ 给调度台的请求

### 重跑单
- `BattlePostFx.cs` 改了 ⇒ 建议**随批次**跑一次 **`BattleScene.Run`**（它是 `CardPresentation/Battle/**` 的宿主；
  LUT/Bloom 那两条断言若存在，看它们是否还绿）。⚠️ 本代理**不跑 Unity**。
- `Tools/typecheck.sh` 已 0/0（本批只改运行时程序集里的一个文件，编辑器程序集不受影响）。

### 转派（按文件所有者）
1. **E5（`BattleDriver.cs`）**
   - `ShowSkillPanel`（现读 `:15065`）→ 传 `targetsAny: !CardFaceRules.ShouldShowTargetsAvailable(def, false)`
     （账 `战斗逻辑驱动.tsv:1040` 的消费点；池内 17 张带 `avail` 行）+ 喂 `maxTargets` / 首个合法目标名
     （`SkillPanel.SetTargetsSuffix` 的两个形参已在）。
   - `ShowJustOne`（现读 `:14646`）→ 补 `army`（`u.Card.rawCard.army`）⇒ 否则技能钮落回占位图（`AttackSelector.ShowJustOne` 的 doc 已写）。
   - **换牌收尾时序**（§2.7 的常量）：`ReturnToDeckAnim(globalVars.timeToDrawEnemyCard)` 逐张 →
     `WaitForSeconds(TimeToDrawCard(isPlayer) × 2.0)` → `FadeCardItems(CardUI, 0, 0.3)` + 等 0.3 s；
     另有 `FinishMulliganFinalPhase` 的 `FadeCardItems(cardUI, 1.0f, 0)`（**只我方**）。
   - 展示窗 `closeOnESC = 1`（窗开着按 ESC 该关；`CardDisplayWindow` 那一侧只差一个分发）。
   - `DeckRuntime.Say` → `Shell/MessageToast.ShowError(s)`（**入口已存在**，见 §⑤-7）。
2. **E7（`Core/**`）**：`ChooseDissolveMaterial` 的传说档 · `ShowOriginalRemnantCard` ·
   `BroadcastChangedLanguage` 的「卡面重排字」· 以及 `CardView` 的「重设描述」写口（手牌抬牌那一侧要它）。
3. **`Hand/CardInteraction.cs` 的所有者**：`BattleCardUI.ShowAvailableTargets` 落地的三步
   （`Core/CardView` 描述写口 + `RuleCore.PlayTargetPool` + `ApplyShowInHand` 进入支），**并在那里补一行注释**：
   「本处 = `desc + "<i>" + 后缀 + "</i>"`（**无空格 / 斜体 / 带换行 desc**）；⛔ 别与 `Battle/SkillPanel.cs:2069` 那边抽共用函数」。
   ⚠️ 落地前确认卡面那颗 TMP 的 `richText = true`，否则 `<i>` 会原样印出来。
4. **一次 Unity 运行（调度台）**：`ScenarioMaterialFader` 的 `BlendCalls` 测量 —— 换一张环境卡（进攻卡）后
   读每一件 blendable 的 `BlendCalls` / `BlendCallsForward` 增量。判据：**同一件正好 2 次 ⇒ 原版第 2 次会把淡出取消**
   （照抄等于「淡出不动」）；1 次 / 3 次 ⇒ 可以照抄。
5. **教程线**：`SetupTutorialObj` 需要「教程在跑」的本地入参 + `TutorialObjs` 父链 `lossyScale` 实读（§④-3）。
6. **账合并**：`python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B8_AF`（dry-run 已过：更新 12 · 新增 0 · 0 错）。
