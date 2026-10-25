# R · 战斗特效档位（R8-b）与抬起沿源级断言（A462/T08）执行报告

> 执行代理 `R` · 2026-10-25 · 白名单只碰三件：`Core/VfxMap.cs` · `Editor/BattleVfxAssertE3.cs` · `Editor/BattleScene.cs`
> 红线遵守：**没跑 Unity**（`-executeMethod` 一次都没调）· **没动 git** · **没改正本/任务总表**
> typecheck：`TMPDIR=/tmp/wf_r1 bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**（最终一轮）

---

## R1 · `AN1·R8-b`：第 ④ 档仍播别 trait 的框（真缺陷）—— ✅ 已修 + 断言翻面

### 结论

`VfxMap.TriggerBeatRows` 从「只挡 `TriggerType ∈ {0,1,2,5}`」改成「**所有档一律排除 trait 行**」。
判据（照简报，未重新考据）：原版触发那一拍 = ① 卡自己的 `cardAnimations[]`（`AbilityLogic__PlayAnim`，
`RawCardScript__TriggerAbility.c:85-110`）＋ ② **一张 trait 框、只由 trait 链播一次**
（`:58-62` → `BattleManager__PlayTriggerTraitAnim.c:33` → `CardScript__ActivateTriggerTraitAnim.c:6-20`；
`BattleCardUI__DisplayTriggerAnim.c:75-82` 只认该 trait 表里 `triggerType ∈ {3,6}` 的行）。

### 关键实现决定（⚠️ 一个比简报更细的判据，**没放宽**口径）

运行期表 `card_vfx_by_card.json` **没有 `src` 这一列**（只有 `n`/`a`/`c`）⇒ 用
`TraitFrames.TriggerTypeOf`（反查 `trait_effect_bindings.json` = 原版 `CardTrait` SO）判 trait。
但「按名字一刀切」会误删 **39 行「名字撞车的卡级行」**（同一 (CardAnim, prefab) 既在 trait 表里、
又在**这张卡自己的** `animTimes[]` 里有行 —— `UM84/85/86` 的 `CodexEffect`、`GSC32` 的 `ArmourEffect`、
`AM17` 的 `ArmourEffect` …，2026-10-25 现算）。这些是原版 `AbilityLogic.PlayAnim` **会播**的卡级行，
不是 `cardtrait-so`。⇒ 新方法 `IsTraitFrameRow(p, times)`：

- 名字不在 trait 表 ⇒ 卡级行（保留）；
- 名字在、且 `AddTriggerNamesOf` 标得出 trigger 名 ⇒ **名字撞车的卡级行**（保留，再走 W6 名字闸）；
- 否则 ⇒ trait 行（**一律挡掉**）。
- ⚠️ 第 ② 档（`times` 为空）**结构上做不到这次自证** ⇒ 只能按名字判；现读「没有 animTimes」的卡，
  `trigger` 桶里**一条卡级行都没有（0 条）**⇒ 不误伤（已写进 doc，真出现时如实记「查不到更精确判据」）。

计数器：`TraitFrameDuplicates`（本拍链② 也会播同一件）保留 —— `AnimFXCheck` 的 R8⑤
`witness > 0` 那半靠它，**不能停**；新增 `TraitFramesOnOtherBeat`（本拍链② 也不播的 trait 行 =
`R8-b` 的判别计数）。

### 改动清单

| 文件 | 改动 |
|---|---|
| `Core/VfxMap.cs` | `IsNonTriggerBeatTraitRow` → **`IsTraitFrameRow(p, times)`**（判据/理由全写进 doc）；`TriggerBeatRows(bucket, keyword, times=null)` 一律挡 trait 行 + 两个计数；`TriggerBeatRowsMatching` 去掉「trait 行开后门」那一支（现在全是卡级行，统一走名字闸）；新增 `public static int TraitFramesOnOtherBeat`；`SelfCheckTriggerBeatSplit` 重写：期望值**独立算**（只用 `TriggerFrames.TriggerTypeOf` + `AddTriggerNamesOf` 两个原始判据，⛔ 不调被测实现）、②/④ 两档判据分开、新增 `TraitFramesOnOtherBeat` 增量为 0 的红线；`TryResolveTriggerPicks` ②/④ 两处调用传 `times`；相关 doc 全部改口径 |
| `Editor/BattleVfxAssertE3.cs` | `AM27` / `GOF26+unstable` / `AM15` 三条**翻面**（期望 1 件 → **0 件**）；`GOF26` 那条显式断「不含 `ConcussiveEffect`」（`R8-b` 本体）；**新增两条灭自证正例**（见下） |
| `Editor/BattleScene.cs` | `J1·D4` 同款三条夹具翻面（AM27 / GOF26 / AM15 → 0 条）；保留 `DA10`（卡级行别切过头）正例；**新增 ⑤ `GSC16` + `ambush`** 灭自证正例 |

### 新增的两条灭自证（R1 要求）

1. **「本拍链② 确实要播同一件」**（`BattleVfxAssertE3.D4` 第 ④ 条）：
   `AM15` + `keyword="Talent"` ⇒ `VfxMap` **0 件**，而 `TraitFrames.TriggerBindingsFor("Talent")`
   里确实有 `TalentEffect`/`TalentTraitTrigger` 且 `TriggerType = 3` ⇒ **两条链没有一起丢**。
   🧨 删掉绑定表那行 / 换成别的 trait 名 ⇒ 红。
2. **「别切过头」**（两处）：`DA10`（`Trait_GainQuest` 是卡级行，照旧 1 件）；
   `GSC16 Acolyte Heavy` + `keyword="ambush"` ⇒ 恰好 `Buff_GSC` / `Buff_Red`（卡级、标 `Ambush`），
   不含 `StrikeEffect`（trait 行），也不含 `CreateCard Sabotage`（标 `Strike`，不属这一拍）⇒ 同时钉住
   「trait 行全挡」「卡级行保留」「W6 名字闸」三件事。

### 行为影响（预期）

`trigger` 桶掉回整桶的那两档（② 无 `animTimes`；④ 关键词不在白名单）里，**trait 行不再由 `VfxMap` 播**：
- `AM27`/`AM15` 这类桶里**全是 trait 行**的卡 ⇒ 这一拍 `VfxMap` 输出 **0 件**（画面由链② 出，原版就该这样）；
- `GOF26` + `unstable` 不再播 `ConcussiveEffect`（`R8-b` 的修复本体）；
- 名字撞车的 **39 行卡级行保留**（不受影响）。

---

## R2 · `A462` 抬起沿的源级断言（自检断言线 T08）—— ✅ 已加

### 结论 / 证据

- 现状（原）：`Editor/BattleScene.cs` 的 `★ A462` 源级段**只扫** `ClickedThisFrame()` 并断「只剩 4 处」。
- 新增：同一段里加**对称**的 `ReleasedThisFrame()` 源级断言：
  - 白名单 **7 处**（现读，2026-10-25；⛔ **不写行号**，锚点 = **代码文本 + 所在方法名**）；
  - 每处一句话判据（为什么该按「抬起」）→ 下面这张表；
  - 新增「两条沿**互斥**」：同一行不许同时被两份名单收（按扫描到的行号交集判）。

| # | 方法 | 行锚点（代码文本） | 为什么该按「抬起」 |
|---|---|---|---|
| 1 | `HandleSettings` | `if (ReleasedThisFrame()) SettingsClickAt(WorldPointer());` | 设置窗四颗钮 = 原版 uGUI `Selectable`（`EverguildButton`/`EverguildToggle`/`Button`）⇒ `m_OnClick` = 抬起；滑块那条 `PointerFrame` 不动（原版 `Slider` = 按下即跳 + 按住拖） |
| 2 | `HandleReplaySessionExit` | `if (!ReleasedThisFrame()) return false;` | 原版 `ClickExitBattle` 全库唯一调用点 = `BattleSettingsWindow__ResignButtonOnClick`（uGUI `m_OnClick`） |
| 3 | `HandleMulligan` | `if (ReleasedThisFrame()) _mulligan.HandleClick(WorldPointer());` | 换牌三族钮都是 uGUI `Button`（`changeCardButton` = `EverguildButton` · `MulliganContinueButton`/`HideMulliganButton` = `Button`） |
| 4 | `HandleChoose` | `if (ReleasedThisFrame()) _choosePanel.HandleClick(WorldPointer());` | `ChooseCardMenu` 的 `ContinueButton`/`HideChooseButton`/每卡 `Select` 钮都是 uGUI `Button`（「点卡本身也能选」是我们加的，跟整行走） |
| 5 | `TickCardDisplayClick` | `return ReleasedThisFrame() && HandleDisplayWindowClick(WorldPointer());` | 放大窗四条支路全是 uGUI 点击（`backgroundButton`/`voiceOverButton`/`showCardTextButton`/卡格 `IPointerClickHandler`）；卡格仍是手搓，跟同一行走 |
| 6 | `DrivePlayerTurn` | `if (ReleasedThisFrame())` | ③′ 点牌堆（跟这一行）/ ③ 结束回合（uGUI `TurnBtn` 的 `m_OnClick`）/ ④ 选目标（`CardCollider.IPointerClickHandler` → `OnTouchUpAsButton`）；⑤ 记 `_pressSlot` **必须留在按下** |
| 7 | `HandleTutorialTipDismiss` | `if (!ReleasedThisFrame()) return false;` | 教程提示的「继续」是 uGUI（`TutorialTip` 的 `ContinueText`）；⚠️ **这一处没有逐字的原版沿判据**（`TutorialTipScript` 方法体拿不到）—— 取抬起是为了与棋盘那几条**同一条边**，避免一次点击被两条沿各耗一次（**如实记**） |

### 「按方法名分开」的理由（不是装饰）

`if (!ReleasedThisFrame()) return false;` 在 **2 个方法**里逐字相同 ⇒ 只按文本收会放过「把某处搬去别的方法」
那一类漂移。扫描时按 `EnclosingMethodOf(lines, i)`（类成员缩进 = 8 + Allman 以 `)` 收尾 + 排除
`if/for/while/switch/return/using/lock/catch/do/else` + 名字非空且该行无 `=`) 求所在方法名。

### 离线自测（⚠️ 不能替代 Unity 跑，只证明扫描逻辑本身）

按同一套规则在**现读的** `BattleDriver.cs` 上模拟：`ReleasedThisFrame()` 扫到 **7 处**、白名单命中 **7/7**、
白名单外 **0**；`ClickedThisFrame()` 扫到 **4 处**（与上面那条现有断言一致）；两条沿行号交集 **0**。

---

## R3 · 自检线 `B6` 夹具改走生产同口（`T03` / `A5` 三步）—— ✅ 已做

### 改动（`Editor/BattleScene.cs` 的 `★ B6·` 段）

1. **候选表改由 `VfxMap.ResolveAllTimed` 给**（原来 `VfxMap.ResolveAll` = 不带时间的老口，生产一处不用）：
   选样卡时直接用 `got6[i].Name`；`b6names` 现在 = 生产口径的名字。
   `ResolveAll` **只保留**在「带时间版 vs 老口，名字与顺序逐位相同」那条回归断言里
   （改成 `b6old = VfxMap.ResolveAll(...)` 再比 —— ⛔ 不再拿生产口和它自己比 = 自证）。
2. **前提成立断言**：`b6timed.Length >= 2` 且**至少两条 `T` 不同**（`anyGap` 由 `b6timed` 自己的 `T` 算，
   不是 `ResolveAll` 那一路的 `exp[]`）—— 否则下面那串「T == animTimes 的 t」全是空放。
3. **逐卡桶判别式**：`ResolveAllTimed(evt, faction, **null**, times, null, perCardOnly: true)` ⇒ 必须
   **与逐卡结果不同**（结构上 = 0 件）⇒ 证明候选取自**这张卡自己的桶**、不是 `ByEvent`/按阵营兜底
   （兜底永远 1 件、T=0）。
   ⚠️ 这里显式传 `perCardOnly: true`：是为了「拿掉卡身份再问一次」时**不掉兜底**，
   否则会为这条自检刷一条**假的「留白告警」**（`NoFactionVariantWarns`）。

### 现读样卡（按同一规则离线复算，供主对话对照）

老选择规则（`ResolveAll`）第一个命中的是 **`ASH83 Autarch` + 事件 `trigger`**
（`WaystoneTrigger` 0.0 / `Waystone UI Spend` 1.0 / `Buff_Red` 1.2，`animTimes[].tn` 都是 `UseSpiritStone`）。
新口径下同一张卡同一条事件仍解析出这 3 条（tier ④：`keyword` 为空 ⇒ 保守推断出唯一名字
`UseSpiritStone` ⇒ 名字闸按它筛；这 3 行都是卡级行且都标这个名）⇒ **T 仍为 0 / 1.0 / 1.2**，
前提 ≥2 + T 不同成立，逐卡判别式（无身份 ⇒ 0）成立。**最终以 `BattleScene.Run` 实跑为准。**

---

## R4 · `talent` 触发器在表现侧放行 + 记账 —— ✅ 已做（按判据；`TraitFrames` 无需改）

### 判据（读 `资料/普查产出_1025/A_引擎线_执行报告.md` §A3）

`CardScript.OnTurnStart.c:160`（`HasCurrentTrait(0x34d)`）→ `:167 OnTrigger(0x2e = 46)` →
`RawCardScript.TriggerAbility.c:60 PlayTriggerTraitAnim` → `BattleAction 0x44` →
`CardScript.ActivateTriggerTraitAnim` → `GetTraitFromAbilityTrigger(46)` = `DefinedTrait.talent(845)` →
`BattleCardUI.DisplayTriggerAnim(845)`（原版 `Talent.asset` = `triggerType 3 / TalentTraitTrigger / TalentEffect`）。
⇒ **原版这一拍「有槽」**，画面全由链② 出；**不是「原版无此槽」**。

### 改动（全部落在 `Core/VfxMap.cs`）

1. **放行**：`IsRoutedTriggerName` 新增 `case "talent":`（整段判据注释）；`MustRouteTriggerNames` /
   `MustRouteTriggerKeywords` 各加一格 **`"Talent"`**（`EmitCardTrigger` 发 PascalCase 原版枚举名；
   两张表仍**同长 18 / 同序**，已离线核过）。
2. **记账**：新增 `static readonly string[] TraitFrameOnlyTriggerNames = { "Talent" };`
   —— 这些名字 **0 条 `animTimes` 行**，画面完全由链② 承担；`SelfCheckRoutedTriggers` 对它们不再报
   「空放」，改问「**链② 里确实绑着 `TriggerType ∈ {3,6}` 且有 `Prefab` 的行吗**」（现读 talent =
   1 行，`TalentEffect` / `TriggerType 3`）。
   ⛔ 明确写了：别把它塞进 `NoEmitTriggerNames`（引擎已发），也别为了消「空放」从
   `MustRouteTriggerNames` 删掉（那会掉回整桶）。
3. **`TraitFrames` 侧零改动**（符合 `A3` 的交接口径）：`DisplayTrigger` 按 trait 名小写查表，
   key 就是 `"talent"`，绑定表里已有那行 ⇒ 不需要改 `Core/TraitFrames.cs`（它也在我的黑名单里）。

### 新增断言（`Editor/BattleVfxAssertE3.cs` 的 `TalentBeat`）

- **记账**：`TraitFrames.TriggerBindingsFor("Talent")` 里确实有 `TalentEffect`/`TalentTraitTrigger` 且 `TriggerType = 3`；
- **解析输出**：`SOR9 Preacher` + `keyword="Talent"` ⇒ `VfxMap` **0 件**（`animTimes` 里没有这一拍的行）；
- **不许凭空多出「留白告警」**：同一趟 `VfxMap.NoFactionVariantWarns` **增量必须 = 0**
  （白名单里的已知 trigger，不是未知事件/缺配）。🧨 删掉 `case "talent"` ⇒ 掉回整桶/兜底 ⇒ 告警冒出来 ⇒ 红。

---

## 需要主对话跑的自检（按覆盖面）

> 我这批只动 `Core/VfxMap.cs`（引擎共用件）+ 两个 Editor 断言文件 ⇒ **不跑 Unity**，由主对话执行。

| 条目 | 为什么 | 期望 |
|---|---|---|
| **`RuleEngineTest.Run`** | ⚠️ **没改 `RuleEngine/`**，严格说不需要；但 `RuleEngineTest_EngineC5.cs` 现在有代理 `A` 新加的 `C5Q1TalentTurnStartTrigger`（引擎侧 talent 发射口）⇒ 与本批同一件事，建议一起验 | 全绿 |
| **`BattleScene.Run`** | `Editor/BattleScene.cs` 改了：A462 抬起沿源级断言（新）、B6 夹具三步（改）、J1·D4 三条翻面 + 一条新正例 | **断言合计通过，0 ✗**；⚠️ 已知间歇 139（铁律 12 写明 `BattleScene.Run` 的 139 是已知间歇） |
| **`AnimFXCheck.Run`** | `VfxMap` 是共用件；`AnimFXCheck` 的 R5③/R5④/R8⑤（`witness > 0`）都吃本批改的过滤与计数 | 全绿（R8⑤ `pairs > 0` + `witness > 0` 都要成立） |
| `BattleVfxAssertE3` | **没有独立入口**（它挂在 `BattleScene.Run` 里，见 `BattleVfxAssertE3.Run`） | 跟 `BattleScene.Run` 一起看 `BT E3 ✓/✗` |

---

## 没查清 / 需要如实交代的部分

1. **R4「② TraitFrames 记账」我只能记在 `VfxMap` 侧**（`TraitFrameOnlyTriggerNames` + `SelfCheckRoutedTriggers`
   的绑定校验 + `BattleVfxAssertE3.TalentBeat`）。`Core/TraitFrames.cs` 在**我的黑名单**里 ⇒ 没碰；
   按 `A3` 交接口径它本来也**不需要改**（`DisplayTrigger` 按小写 key 查表，绑定表已有 `talent`）。
   如果主对话认为「记账」必须落在 `TraitFrames` 自己的 doc 里，需要另派一个有那个文件权限的代理。
2. **`B6` 的 `rng` 口径（顺手发现，未改）**：夹具的 `b6timed` 用 `rng = null`（不抽组），
   而生产 `BattleDriver.FireEventAll` 传的是 `_vfxOffsetRng`。全池唯一 `rc == 1` 的是 `GOF12 Spanner`，
   今天没被选成样卡（现读样卡 = `ASH83`），所以无害；**将来若样卡落到 `rc == 1` 的卡上，「接线」那几条
   会以「两边候选数不同」的形式红**（不是静默错）。建议：选样卡时排除 `rc == 1` 的候选，或给夹具也用同一个 rng。
3. **`TraitFrames.TriggerTypeOf` 是「按名字反查」**：若将来出现「**未归口**的 trait 名 + 卡自己那条同名动画 +
   该 `animTimes` 行标不出名」的形状，第 ② 档会按名字把它当 trait 挡掉。现读这种形状 **0 条**（已写进
   `IsTraitFrameRow` 的 doc）；出现时必须先按 `AddTriggerNamesOf` 再挡一层（不是现在能修的）。
4. **`GOF26` 自己的 `UnstableEffect` 两行在 `trigger_unmapped` 桶**（不在 `trigger` 桶）⇒ 本批改动碰不到它们；
   `VfxMap` 根本不消费 `trigger_unmapped`（全仓 grep 零命中）。简报里说「`GOF26` 自己的 trigger 81 行是
   `UnstableEffect` + `UnstableEffect_ExplosionExtra`」——**那两行在数据里属 `trigger_unmapped` 桶**，如实记。
5. **文件撞车（过程中实测）**：`Battle/BattleDriver.cs` 在本代理工作期间**被另一个代理持续写入**（mtime 00:47），
   中间有两轮 typecheck 报 `BattleDriver.cs:20141/20151 PlayHitFrameShake` 参数不匹配 —— **那是别人的半成品**，
   后来自己好了。我的三处改动在那之后 `git diff` 复核仍在（`BattleScene.cs` 仍 CRLF 44,543/44,543，未翻行尾）。
6. **断言文案里的历史计数**：`NoEmitTriggerNames` 现读 **7** 个（旧文案写 8）、件级白名单现读 **18** 个
   （旧文案写 14/15）—— 在我碰到的两处已就地改成「现读」措辞（铁律 5），**别的地方**没全仓扫。

---

## 追加（第二批）· R5–R9 · 审查代理 `AU2` / `AU4` 给的判据 —— 全部落在 `Editor/BattleScene.cs`

> 本批**只改 `Editor/BattleScene.cs`**（协调方确认「这个文件现在只有我在写」）。
> typecheck：运行时 0 / 编辑器 0；行尾仍 **CRLF 44,727 / 44,727**（没翻）。

落了几条断言：**R5 = 1 条（改 1 条旧断言）· R6 = 3 条 · R7 = 5 条 · R8 = 2 条（替换 1 条旧断言）· R9 = 7 条**，合计 **18 条**。

| 件 | 落点（`Editor/BattleScene.cs` 内） | 落了几条 | 断什么（判据 → 审查代理给的原版出处） |
|---|---|---|---|
| **R5 · B-25⑤ 前提翻面** | 多卡窗 B-25⑤ 段，原 `Check(sx0 == 0f)` **改掉** | **1 条** | 开窗横向偏移 = **`MaxScrollPx × 0.5`**（±1 px）——原版 `ScrollRect` 初始 `normalized 0.5`（`RT_3174` pivot 居中 + `anchoredPosition≈0` + `ContentSizeFitter`）。同一段「往右拖到头夹回 0」那条**一字未动**。🧨 初位改回 0 ⇒ 红 |
| **R6 · 结算压暗颜色** | 结算面板段，紧跟 `Check(end.Visible, …)` | **3 条** | ① 前提：`DimGraphicForTest` 读得到（读不到 = 没验到，当场红）；② 逐分量 = **字面量** `(0.0283019, 0.0283019, 0.0283019, 0.7529412)`，容差 1/255（⛔ 不读 `EndPanel.DimColor`）；③ 灭自证：**不是**改动前的替代值 `(0,0,0,0.97)`。🧨 α 改回 0.97 / 三通道回 0 ⇒ 红 |
| **R7 · 选牌压暗 α + 队列层级** | 选牌面板段，紧跟 `Check(panel.Visible, …)` | **5 条** | ① 前提：`ChoiceShade` 在且是**世界档 `ImageQuad`**；② `CardChoicePanel.QChoiceShade == MulliganPanel.QMulliganShade` **且** `QChoiceCard == QMulliganCard`（= 「用的是同一对常量」，防第二份写死）；③ 压暗 α == **0.8**（原版 `Shade.onAlphaLevel`，`MonoBehaviour_4598`）；④ 压暗层**真的用** `QChoiceShade`；⑤ 候选卡 ≥1 张、队列 = `QChoiceCard` **且 > 压暗队列**（原版靠 `FrontCanvas` 兄弟序保证「卡永不被压暗」）。🧨 α 回 0.55 / 队列写反 / 卡漏 `SetQueue` ⇒ 红 |
| **R8 · 多卡窗卡心 y = 540** | 多卡窗段，**替换**原来的「卡心落在窗口带里」那条 | **2 条**（替换 1 条） | ① 卡心 y ≈ **540**（±1，设计 px；旧值 571.60 低 31.6 px）；② 上下留白**对称**且 ≈ **26.16 px**（= (视口高 818×0.7 − 卡高 520.3)/2，容差 1，对称差 <0.5）——⛔ 不只断 540（只断 540 时把视口高度改坏也照样绿）。🧨 `cy` 改回 `(BandTop+标题高/2+BandBottom)/2` ⇒ 红 |
| **R9 · 窗级声音三处** | 多卡窗段末尾（关窗前），自带开/关窗状态机 | **7 条** | 多卡窗：①「**开窗不出声**」（原版 `openSound` 空，⛔ 别加）；② 关窗 = 默认 **`ButtonClick Back`** 且只播 1 次（`closeSound` 空 + `useDefaultCloseSoundIfNull=1`）；③ 关一扇本来就关着的窗 ⇒ **不出声**。放大窗：④ 关一扇本来就关着的窗 ⇒ 不出声；⑤ 开窗音 = **`CardExpand`** 且只在「关→开」响 1 次；⑥ 已经开着再 `Show` ⇒ **不响**（`TryOpen` 的 `CurrentState==0` 支）；⑦ 关窗音 = **`Close Card Display`** 且只在 Visible 时响 1 次。计数口径 = `WindowSounds.ResetCounters()` + `Played`/`LastCue`（reset 会清 `timeToPlayAgain` 去重表 ⇒ 不被上一条 0.1 s 节流误伤）。 |

**本批已知风险 / 如实记：**
- R9 的「播了几次」靠 `WindowSounds.Played`，前提是**这一小段里没有别的系统也喊窗级音**（用的是同一把 `PlayFx` 出口）。为了把窗口开得最小：多卡窗开窗那一步**不夹 `Step`**（`Show` 同步就可读 `Visible`），注释里写明了原因。
- R7 的卡队列统计用 `panel.GetComponentsInChildren<CardView>(true)`：选牌 / 选一 / 选效果**共用同一棵树**（`_choosePanel.transform`），历史残留的候选卡在 `Open` 时也会被 `LayOutCards` 盖成 `QChoiceCard` ⇒ 现读不会误判；若将来有「建了但没 `Open`」的残留卡，这条会以「队列不是 QChoiceCard」报出来（不是静默）。
- R8 的 0.7 = `Viewport` 的 `m_LocalScale`，**写在断言里当常量**（带出处注释）：它是原版资产字段，不是我们的实现常量。
- 这三批（R1/R2、R3/R4、R5–R9）都**没有跑 Unity**；`BattleScene.Run` 的实跑结果以主对话为准（尤其 R9 的计数断言，首次跑若红，先按上面两条已知风险排查）。

---

## 追加（第三批）· R10–R12 · 审查代理 `AU1` / `AU3` 的判据

> 本批**只改 `Editor/BattleScene.cs`**。typecheck 运行时 0 / 编辑器 0；行尾仍 **CRLF 44,950 / 44,950**（没翻）。

落了几条：**R10 = 18 条 `Check`（①8 · ②4 · ③4 · ④2）· R11 = 1 处插入（修 1 条既有断言）· R12 = 2 处就地订正**。

### R10① · `Return` 三档真值 + 「不再零时长」—— **8 条**

- 前提 2 条：卡池载得到 · 找得到「不带 / 带 `teleport`」各一张；
- `EvtKind.Return` **不再登记成零时长**（`IsZeroDurationKind == false` 且 `DurationOf(EvtKind.Return) > 0`）；
- 前提：`EventTiming.HasTraitOf` 挂钩可用、且对 teleport 那张卡回 true（**回 false 的话 1.30 档永远取不到 = 因错的原因绿**）；
- **普通回手 = 0.75 s**（两段 `yield` 0.55 + 0.2）；
- **带 `teleport`(1230) = 1.30 s**；
- **回牌库那一支今天按 0.75 s 排**：原版 0.85（0.55 + `MoveToDeck` 0.3），但 `BattleEvent` 没有「回手 / 回牌库」那一位 ⇒ **已知早 0.1 s** ⇒ 断言写的是「= 0.75 且 ≠ 0.85」，⛔ **没把 0.85 当精确值钉死**；
- `Slot < 0`（墓地→手牌/牌库、`ephemeral`）**照旧 0**（判据未查清那两档，没拿 0.75 硬套）。

### R10② · `StartDelay` —— **4 条**

- `cardanim_clips.json` 载进来了（前提）；
- `Cuts Brutal Warp Spider` 那行 `StartDelay == 0.5`（原版 `AnimInfo+0x28`）；
- 卡池里有 `Empyric Ambush SaimHann`（前提）；
- `CardAnimClips.Resolve` 的 `T` = **`VfxMap.T` + 0.5**（同一张卡同一条 `play` 桶、逐件比）。

### R10③ · 命中帧同帧次序 + 「震屏不依赖受击视图」—— **4 条（源级）**

> ⚠️ 没有运行期记录器，且 `BattleDriver.cs` **不在本代理白名单** ⇒ 与本节上面那条「队尾源码普查」同一做法，按**源码次序**钉（报告里已注明）。

- 前提：命中帧那个 `ScheduleAction(EventTiming.HitFrameLag(...))` 块里四件都找得到；
- **次序 = 震屏 → 命中音 → 粒子 → 后坐**（四行的行序严格递增）；
- **震屏不依赖受击视图**：调用行只受 `if (feel)` 管、不含 `tgtView/target`；`PlayHitFrameShake` 方法体里**没有** `target` 判空；而 `PlayHitFrameRecoil` 里**保留** `if (target == null) return;` ⇒ 受击方视图为空时**震屏仍 +1**、只有后坐跳过。
  🧨 把两半合回改前那个 `target == null ⇒ return` ⇒ 红。

### R10④ · `duringBattleFlag` 等价物 —— **2 条（源级）**

> ⚠️ 运行期验不了：`_cardHoldForTest` 那个自检钉死口**在闸之前短路**（设计如此），批处理又没有真指针（`PointerHeldRaw()` 恒 false）⇒ 只能按源码钉。

- 前提：读得到 `BattleDriver.cs`；
- `CardHoldPeek` 体里的**行序** = 自检钉死口 → **闸**（`Ctx == null || Ctx.IsOver || InMulligan || Ctx.Turn <= 0 ⇒ false`）→ `PointerHeldRaw()` → 「按着棋盘格 / 手牌拖拽」（扫描**限定在这一个方法的范围**内，⛔ 不扫别的方法的同名片段）。
  ⇒ 换牌期 / 第 0 回合 / 终局：**按住手牌也不出张数**；正常回合出（正例那半由既有 `SetCardHoldForTest(true)` 那几条覆盖）。

### R10⑤ · 「回合抬头 1.5 s」（**只列受影响断言，没改代码**）

AU1 落地后，**每回合开始会多 1.5 s 的阻塞事件**。会被它影响的断言分四类（文件内大致位置，⛔ 行号会漂）：

1. **「回合切换后只看 0.05 s」那类 —— 风险最大**：所有 `Step(0.05f)` 之后就断「轮到我 / 可出牌 / 手牌整理好 / 选择器可开」的节（开局链 632 / 12527 / 12699 之后那几段、`A462` 段、`B6` 的 `StepThrough(driver)`、`C1-A05` 的回合推进）。若抬头是**阻塞**的，这一步会停在抬头里 ⇒ 全部要改成 `Step(1.6f)` 或等「抬头结束」的口。
2. **绝对时刻 / 队尾类**（只要闸口在回合切换后读数就整体 +1.5 s×回合数）：`★ P1-④` 驱动级队尾（~11600-11616）· `★ B-30③` 非阻塞（~13802-13858）· `★ B1·偏差3` 队尾/现在（~36520-36532）· `probe` 时间线推进（~20946-21039）· 攻击 VFX 队尾下界（~21431-21452）· 命中帧 `while (Clock < atkAt + 0.17/0.26)`（~37061/37063）。
   **这些多数是相对量（差值）⇒ 应仍绿**；要盯的是「先取基线、再等回合、再比」的写法。
3. **等阻塞动作的循环上限**：`while (driver.WaitingForBlockingAction && g++ < 400) Step(1f/30f)`（= 13.3 s，约 20 处：33932 / 36482 / 36487 / 36514 / 36532 / 37193 / 37210 / 37231 / 37238 / 37319 / 37324 / 37417 / 37452 / 37654 / 38233 / B6 38668 …）—— 1.5 s = 45 帧，余量一般够，但要把「本来就接近 400 的那几条」重算。
4. **时钟 / 倒计时类**：`ClockLeft == TurnSecondsForThisMatch`（~16956）· `TickClockForTest(16f)` ⇒ 自动结束回合（~16895-16896）· `C1-A05` 缩时四条（~37480-37497）。
   AU1 的交接写明「抬头只在这一处补，⛔ 不顺手修时钟 / `ResetClock`」⇒ 这几条**应仍绿**，列出来当盯梢项（抬头若把时钟也推进了，这里先红）。

### R11 · `ButtonScale` 补间落地 —— 1 处插入

在 `driver.SimulateOpenCommand(retAtk);` 与 `Check(sel.ButtonScale(pickKind) > 1.2f, …)` 之间插入 **`CardTween.Advance(0.35f);`**（`CardTween.Mode` 已 = `Manual`；同文件 A28 那段就是同款）。理由：开窗那一拍只是**起播**补间（`t = 0` ⇒ `ButtonScale` 还是 1.0），批处理没有帧循环 ⇒ 不泵就恒 1.0。**断言条件一个字没改。**

### R12 · 两处就地订正（铁律 5，改注释 / 文案，不改断言条件）

1. `PlayHitFrameFeel` 已拆成 `PlayHitFrameShake` / `PlayHitFrameRecoil` ⇒ 那两处注释/文案改成新名字，并写明「这条断言量的是**后坐**那一半（对 null 视图早退）」；
2. 拖拽阈值文案：数值改指向 **`AttackSelector.PopDragThresholdWorld`** ——**原版 uGUI `EventSystem.dragThreshold` 默认 10 px**；旧值 `0.085` 屏高（≈92 px）是 `accumulatedDragForMinDistance`（**入场动画的分母**，不是弹出阈值）。断言改成 `PopDragThresholdPx == 10`，🧨 改回 0.085 ⇒ 红。

**本批已知风险：** ③④ 是**源级**断言（运行期没有仪器）——它们证明的是「代码结构/次序」，不是「这一帧真的播了几次」；`BattleDriver.cs` 里若把这几句搬进别的方法/换写法（语义不变但锚点变了），会**先红再人工重锚**（这是有意的：⛔ 不许静默漂移）。R10① 的 1.30 档依赖 `HasTraitOf` 挂钩，已补前提断言防止「因为错的原因绿」。

---

## 追加（第四批）· R13 · 回合抬头 1.5 s 的基线重定（AU1 已落地）

> 只改 `Editor/BattleScene.cs`。typecheck 运行时 0 / 编辑器 0；行尾仍 **CRLF 45,040 / 45,040**。

**重定 54 处 + 新增 7 条 `Check`。** 落点与理由：

### 重定（54 处）

| 类别 | 处数 | 改法 |
|---|---|---|
| 「进入我方回合」后的固定 `Step` | **51 + 2 = 53**（`Step(0.3f)` 51 处 · `Step(0.625f)` 2 处） | 逐处改成 `Step(BattleDriver.TurnHeadsUpHold + 0.3f / + 0.625f)`（**用常量**，⛔ 不写 1.5 字面量） |
| 选牌面板那节的 0.05 s（`driver.RefreshAll()` 之后） | **1** | `Step(BattleDriver.TurnHeadsUpHold + 0.05f)` |
| **上界**断言（B-30③ 非阻塞） | **1 条改写** | 上界从 `b30At0 + 1e-3` 改成 `max(b30At0, Clock + TurnHeadsUpHold) + 1e-3`；**判别力没丢**：允许的额外量只有抬头那 1.5 s，给 `ShowCardsWindow` 补 0.5 s 时长 ⇒ 队尾 = Clock+2.0 > 上界 ⇒ 仍然红。同一段的 `Step` 也补了 `TurnHeadsUpHold`（推过抬头才开窗） |

`DrainQueue` / `while (WaitingForBlockingAction) Step(...)` 两类夹具**自动吸收**那 1.5 s（AU1 报告口径），未改；`SimulateEndTurn`（交对手）不受影响，未改。

### 新增（7 条，全部在 `=== 合计` 之前、**重开一局**取干净基线）

1. `★ R13①`：**我方回合开始 ⇒ `TurnHeadsUpHolds` +1**（一局新开前后差 1）；
2. `★ R13②`：`TimelineTailAtForTest − Clock >= TurnHeadsUpHold − 0.05`（抬头那扇阻塞窗真的在）；
3. `★ R13③`：**本回合抽牌那一拍排在抬头之后** —— ⚠️ **与协调方原文有差别（如实记）**：
   我们**引擎抽牌是同步的**（`RuleCore.BeginTurn` 里 `Draw(ctx, ctx.Active)` 那个循环，`RuleCore.cs` 2026-10-25 现读），
   `Begin` 一返回引擎手牌数就已经 +1 ⇒「`Step(1.4f)` 时手牌数未变」**在当前实现上不成立**。
   ⇒ 改验**同一件事的可观测那一半**：`TimelinePending`（时间线待播条数）在 **1.4 s 时仍 > 0**（还排着）、
   **再 0.2 s 后回落**（已放）。灭自证仍然成立：hold 改 0 ⇒ 抽牌立刻播 ⇒ 1.4 s 时队列已无 ⇒ 红。
   🔴 **这条需要主对话/协调方裁定**：要么接受这个替代观测，要么让实现侧把 `RuleCore` 的抽牌真正推迟到 hold 之后。
4. `★ R13④a`：`SimulateEndTurn()`（交对手）⇒ `TurnHeadsUpHolds` **不涨**；
5. `★ R13④b`：`SimulateAiTurn()`（再回我方）⇒ **+1**；
6. `★ R13⑤`（前提）：读得到 `BattleDriver.cs`；
7. `★ R13⑤`（源级）：`ShowYourTurnHeadsUp` 里两条**早退都在 hold 之前** ——
   `Ctx.Active != _me`（对手回合不停，行序 13163）→ `IsTutorialMatch && Turn == 1`（教程首回合不停，13166）
   → `HoldForTurnHeadsUp()`（13170）。运行期要造一局教程对局成本高 ⇒ 这条按源码钉（已注明）。

### 另外两条（AU1-C / AU1-D）

- **AU1-C**：`BattleScene.cs` 拖拽阈值文案/断言已改成「**弹出阈值 = 10 px（`EventSystem.pixelDragThreshold` 默认）**」+ `PopDragThresholdPx == 10`（见上一批 R12②，已落）。
- **AU1-D**：本代理**没有**「`SetSelected` 零调用」类断言 ⇒ 无需翻面。
  🔴 **顺手发现（未改，`AttackSelector.cs` 不在本代理白名单）**：`Battle/AttackSelector.cs:1227` 的 doc 仍写
  「仍没有生产调用点（2026-10-25 现核）：全仓 `selector.SetSelected(` **0 命中**」——**已过期**：
  `BattleDriver.cs:13932` 现在就有 `selector.SetSelected(kind)`。按铁律 5 就地订正那一段（改 doc、不改行为），需一个能碰 `Battle/` 的代理。

**已知风险：** 53 处 `Step` 是在 **AU1 扫出的集合**（= 全部 `Step(0.3f)` / `Step(0.625f)`）上统一加的；
其中若有**不在「进入我方回合」之后**的（例如某处在一次攻击后推表），多推 1.5 s 通常无害，但若某条断言依赖
「还没演完 / 恰好演完」的边界，会在首次 `BattleScene.Run` 时暴露（这类断言都是相对的，预期仍绿）。

---

## 追加（第五批）· R14 · 回放加载失败弹窗的断言（代理 X 的 `Q5.1`）

> 只改 `Editor/BattleScene.cs`（回放那一节，紧跟 `Check(playRec != null, …)` 与那次**正常**回放）。
> typecheck 运行时 0 / 编辑器 0；行尾仍 **CRLF 45,243 / 45,243**。

**落了 25 条 `Check`**（3 条失败路 × 6 条 + 前提 1 + 原因 1 + 灭自证 3 + 负例 2）：

| 组 | 条数 | 断什么 |
|---|---|---|
| 前提 | 1 | 拿得到 `WindowsManager`（弹窗宿主；自检里没有时 `EnsureHost` 会建） |
| **①缺文件 / ②解析失败 / ③版本不认识**（三条各 6 条） | **18** | ① `PlayReplay(临时文件名)` **返回 false**；② `ReplayLoadErrorCountForTest` **+1**（累计 +1/+2/+3）；③ 弹窗 `as PopUpGameWindow` 的 **`MessageKey == BattleDriver.ReplayErrorKey`**（`CustomErrors/ErrorLoadReplay`）；④ **`PrimaryKey == ReplayErrorOkKey`**（`MainMenu/General/OK`）；⑤ `MessageShown == Loc.T(键)` **且 ≠ 键名**（= 真的本地化过）；⑥ **没进入回放**（会话 / 完成位 / 游标**一格没动**——这里用「与调用前逐项相等」，⛔ 不是断「恒 false」，因为本节前面已经放过一次录像） |
| 失败原因 | 1 | `LastReplayLoadErrorForTest` **含「版本」**（`ReplayStore.Load` 三类失败统一写「读不出来（缺失/解析/版本）」） |
| **灭自证** | **3** | ①（前提）读得到 `BattleDriver.cs`；② `ReportReplayLoadFailed` 体里**确实调了** `ShowMessagePopUp(`（删掉那行 **仍能编译** ⇒ 本条 + 上面 3 条 `MessageKey` 一起红）；③ **四条失败支路**都是 `ReportReplayLoadFailed(...)` 之后 `return false;`（现读 **4 / 4**；把任一处改成 `return true` ⇒ 本条 + 运行期 `!okR14` 一起红） |
| **负例** | **2** | 紧接着那次**正常回放**（`same = driver.PlayReplay(playRec)` 之后）：`ReplayLoadErrorCountForTest` **不变** + `popUpWindow` **一格没动**（= 一扇窗都不弹） |

**夹具纪律（照 X 报告 §4.1）**：坏文件写在 `ReplayStore.Dir`（= `OverrideDir` 自检临时目录，⛔ 不碰玩家真录像），
`try/finally` 里**删掉**那两个临时文件（否则会污染后面 `ReplayStore.Count/List` 的断言），并把失败弹窗 `Close()` 掉
（别让一扇模态窗留在战斗画面上影响后面的截图）。

**已知风险**：
1. **`PopUpGameWindow` 在战斗场景里画不画得出来，X 明说没验**（它走 `MenuDraw` / `LayoutSpace`，理论上能建）。
   若首跑 `popUpWindow` 为 null ⇒ 那 9 条（3 × ①③④⑤）会红 —— 那是**实现/场景**问题，⛔ 不是断言写错，按 `X` 报告「没跑实况」那一格处理。
2. X 报告 §五·1（顺手发现）：我们 `PopUpGameWindow.Create` 写死 `closeOnEsc = false`，而原版这条 `ShowPopUp` 传的是
   `closeOnEsc:1` ⇒ **ESC 关不掉这扇窗**。要改得动 `Shell/PopUpGameWindow.cs`（不在本代理白名单）⇒ 只记不改。
3. X 报告 §五·3：`PlayReplay(string)` 全仓零调用点，玩家真实那条路是 `MatchLogRow.OnReplay`（读盘失败**只 LogWarning、不弹窗**）。
   ⇒ 本批断言验的是 `BattleDriver.PlayReplay` 的**四支失败路**；**「玩家真实路径也弹窗」仍欠一跳**（要改 `Shell/MatchLogRow.cs`）。

---

## 追加（第六批）· R15 · `BattleScene.Run` 首跑 21 红的修（Dg1 诊断）

> 只改 `Editor/BattleScene.cs`。typecheck 运行时 0 / 编辑器 0；行尾仍 **CRLF 45,296 / 45,296**。
> **回退 5 处 · 保留 49 处 · 改 4 条测试侧过期断言。**

### P1 · 回退 5 处 `Step`（从 `TurnHeadsUpHold + x` 收回原值）

原则：**`HoldForTurnHeadsUp` 只挪「时间线 cue 的队尾」**，**不推迟**提示条（1.7 s）、黑场补间（1.25 s）、
开场运镜（按 `dt` 推）、DOTween 采样点，也不推迟「飞行中途」的墙钟进度。
⇒ 凡「推完立刻读**墙钟量**（提示/黑场/运镜/补间中途）」或「取**飞行中途**快照」的，**都要回退**；
凡「等**时间线 cue 收尾**」的，保留。

| # | 位置（现读行，⛔ 会漂） | 回退成 | 为什么 |
|---|---|---|---|
| 1 | `BeginFromDeckLibrary()` 之后（提示条那句） | `Step(0.3f)` | 下面读 **提示条**（墙钟 + 队尾 1.7 s）；+1.5 s 会把「你的回合」推过 `TipDuration` |
| 2 | `StartLoadingFadeForTest()` 之后 | `Step(0.625f)` | 读**黑场 α≈0.5 + 运镜仍在播**的中途采样（按 `dt` 推） |
| 3 | `StartCameraIntroForTest()` 之后（T12 重入前提） | `Step(0.625f)` | 同上 —— 要「离本位 > 1」的**运镜中途**值 |
| 4 | B2·残余①「0.3 s 后仍在飞」 | `Step(0.3f)` | **飞行中途快照**（四段共 0.65 s）；+1.5 s 一次推过整段 |
| 5 | B2·偏差8「0.5 s = 交接之后」 | `Step(0.3f)` | 同上（落地段中途）；连带 `:38033`/`:38057` 两条自愈，`C1-B01` 判据未动 |

### P1·自查：其余 49 处「保留」的判定

`grep "Step(BattleDriver.TurnHeadsUpHold"` 现读 **49 处**（回退后）。逐类判据：

- **保留**（49/49）：全部是「**进入/交回我方回合之后，先让时间线 cue 推进到某个收尾点**，再读**时间线量**
  （队尾 / 回放游标 / 面板可见 / 手牌视图 / 分叉点 / 已播清单）」或「**等阻塞动作窗口走完**」——
  这些**正是**抬头那 1.5 s 要推迟的对象（`ExtendTimelineTail` + `BeginBlockingWindow`），加 `TurnHeadsUpHold` 才有意义。
- **已抽查的三处「像墙钟」的**（都判定**保留**，理由）：
  · `CardTween` 换位收尾（`IsSwapping` 到点）：多推 1.5 s 只会让它**更早收尾**，断言方向不变（不红）；
  · 技能面板淡入（`Shot("06b")` 前那 0.3 s）：纯 `Shot` 前的等待，多推无害；
  · 提示泵那句「对手回合不出抬头」：读的是 `HintText` / `TipPending` 的**否定**（要多空有多空），多推只会让它更容易成立（**弱化**、但方向与判据一致）。
  ⚠️ 这三处**没有**回退，是因为 Dg1 的全量 21 红里没有它们；若后续真机/复跑在这三处冒红，按同一原则（墙钟量 ⇒ 回退）处理。

### P2 · 4 条测试侧过期（α/δ）

| # | 位置 | 改法 |
|---|---|---|
| 6 | R10② 的夹具（`Empyric Ambush SaimHann`） | 卡池**真名是 `Empyric Ambush`** ⇒ 改 `CardDatabase.FindById(pool, "dc7e575c0a328f446be1521c23ec850b")`（原来按旧名查 ⇒ 恒 null ⇒ 该条失去内容） |
| 7 | **C6·Q7** 的量具 | 原来量 `Mesh.bounds.center` —— 揭示卡那一族画在**卡框画窗**里、**上沿被夹住**，只有下沿真的动 `0.021` ⇒ 中心只走一半（实得 −0.0105）。改成量**底边**（`bounds.min.y` 之差）。**期望值一个字没改**（仍 −0.021） |
| 8 | **A1** 的负控 | 录像头新增 `hostCardIds/clientCardIds`（双方实际牌表，代理 C 加的）⇒ 回放**优先按它重建、不再过阵营** ⇒ 光拨空 `clientFaction` 的杠杆**失效**。负控**同时**把 `recF1.clientCardIds = null`（= 「没记」哨兵，回放退回老路）⇒ 空阵营杠杆重新生效（并在 `finally` 还原）。第二条「确实报了『在卡池里一张卡都没有』」原样保留（证明它是**因空阵营**而塌） |
| 9 | `C5` 的跨阵营夹具 | `c.Type == "troop"` → **`c.Type == "unit"`** —— 池里没有 `troop` 这个类型名（只有 `unit`/`tactic`/`hero`/`defence`）；督军是 `hero`，被这一格自然排除 |

**已知风险**：P2-8 的负控依赖「`clientCardIds = null` ⇒ 回放退回老路」这条既定设计（`TryExpandRecordedDecks` 的 doc）。
若将来实现改成「头里没牌表就**直接失败**」，这条会红 —— 那时按新语义重定（不是本批的判据变动）。

---

## 追加（第七批）· R16 · 卡面底光语义改成「这张卡自己的 attackType」（AU1/CF）

> 只改 `Editor/BattleScene.cs`。typecheck 运行时 0 / 编辑器 0；行尾 **CRLF 45,394 / 45,394**。
> 中途有一次 typecheck 报 **`RuleEngine/Core/EffectResolver.cs:8966` 的 `FireBoardWhenListener(actingCard:)`**（**别人正在写的半成品**）
> ⇒ 那次编辑器程序集那 86 条全是**级联假错**（拿旧运行时 DLL 编的），等它落地后重跑 = **0 / 0**。

**改了几条 = 3 条（旧语义翻面）· 新增几条 = 9 条 `Check`（3 组）· 顺手 4 处换生产口。**

### 改（旧语义必红/过期）

| # | 位置 | 改成 |
|---|---|---|
| 1 | 提交近战后「目标卡 `CurrentGem == Melee`」 | **`== None`** —— 底光 = **目标卡自己的** `attackType`；这张敌人没设过 ⇒ `None`。合法目标只走 `SetHighlight(ValidTarget)` 描边 |
| 2 | 切远程后「`== Ranged`」 | **`== None`** —— 换打法只刷**我方那张卡自己**（目标卡仍是它自己的） |
| 3 | `SimulateDeselect` 后「`== None`」 | 保留读数 `== None`，但**语义翻面**：清选择**不再熄底光**（会刷回自己的值）⇒ 断言补上「**描边**退回 `Normal`」（`State != ValidTarget`），否则这条在旧语义下会变成恒真 |

### 新增（3 组 9 条）

1. **我方单位选近战 ⇒ 自己的底光 + 数值 ×倍率（2 条）**：
   `SetOpenCommand+SimulateCommand(Melee)`（= `CommitCommand` 写自己的 `LastAttackType` + 刷自己）后：
   `CurrentGem == Melee && OwnAttackType == Melee && AttackGlowOn(false)`；
   `StatScaleNow(StatMelee) / StatBaseScale(StatMelee) ≈ AttackScaleModifier`（板卡那一族 **1.35**、另一族 1.2 —— **按这张卡自己的倍率**，⛔ 不写死一个数）。
   🧨 删掉 `BattleDriver` 里 `CommitCommand` 那拍 `ApplyOwnAttackType(…)` ⇒ 红。
2. **从没打过 / 手牌卡 ⇒ `None` + 两颗底光都不亮 + 场卡数值回基准（2 条）**：
   手牌卡 `OwnAttackType == None && CurrentGem == None && !AttackGlowOn(false) && !AttackGlowOn(true)`；
   从没设过打法的**场卡** `OwnAttackType == None` 且近战数值 ≈ 基准（没被 ×倍率）。🧨 `OwnAttackTypeOf` 给默认值（不是 `None`）⇒ 红。
3. **负例：目标自己是什么就是什么（3 条）**：把敌人 `LastAttackType = Ranged`，我方提交**近战**并 `HighlightTargets()` ⇒
   那张敌人 **仍是 `Ranged`**（不是 Melee/None），**同时** `State == ValidTarget`（合法性只走描边）；
   再 `SimulateDeselect()` ⇒ **底光仍是 `Ranged`**、描边回 `Normal`（= `ClearSelection` 不熄底光）。
   🧨 把 `HighlightTargets` 里 `ApplyOwnAttackType(side,t,v)` 改回 `SetTargetGem(legal ? 攻方打法 : None)` ⇒ 红。

### 顺手（不阻塞）

`GlowQueueProbe` / 并排图那 4 处直调旧口 `SetTargetGem(...)` ⇒ 改成 **`SetOwnAttackType(AttackTypes.Melee/None)`**
（`CardView` 保留同义转发，编得过，但旧口测的是**值域映射**、不是生产语义）。
⛔ 现在 `BattleScene.cs` 里剩下的 `SetTargetGem` 字样只在**断言文案的 🧨 子句**里（说明「改回去会红」），**没有调用点**。

---

## 追加（第八批）· R17 · 复跑只剩 3 红（5235 / 5238）

> 只改 `Editor/BattleScene.cs`。typecheck 运行时 0 / 编辑器 0；行尾 **CRLF 45,453 / 45,453**。

### R17-1 · `R10①` 的 `Slot < 0` 过期 → **拆成两档（1 条 → 2 条）**

AU1 已把**墓地取回**改成 **0.3 s**（`ReturnGraveHold = CardFeel.DealDuration`；`ResolveCopyUnitInCemetery` →
`CreateCardInGame(…, 0.3)` 且 `yield`），而 `ephemeral` 原版**无此链** ⇒ 仍 0。断言改成：
- `EvtKind.Return + Slot < 0`（非 ephemeral）= **0.3 s**（🧨 `ReturnGraveHold` 改回 0 ⇒ 红）；
- `Effect == "ephemeral"` = **0**（文案写明「原版无此链 ⇒ 铁律 11 例外①」，判别式 = `Effect` 那一格）。

### R17-2 · `C6·Q7` 的网格量具「换底边后仍是 0.0000」→ **改族常量级断言**

**结论（读源码确认）**：`ArtWindowMesh` 把每一行的上下沿分别夹到
`yB = max(FramePxYEdge(...), byB)` / `yT = min(..., byT)` ⇒ **两族的 `bounds.min` 和 `center` 都被画窗夹平**
（实测底边/中心差都是 0.0000）。族差 `−0.021` 是烘在 **`acy` → UV 的 `(y − yB0)/h`** 里的，
**网格顶点 y 上观测不到**（画窗两端都裁）。
⇒ 改断**族常量级**：`ArtFamilyDy` 与 `AttackScaleModifier` **同源**（都读 `ArtUsesFightFamily`）——
揭示卡（`CardFace.Hand`）**1.35**、参照 `Full` **1.2** ⇒ **族传错必红**。
⛔ **期望值没改**（`−0.021 = ArtUnitYFight − ArtUnitY` 仍是那条判据）；网格空间的实测改成 `Debug.Log` **出声**
（写明「这一档观测不到」，⛔ 没读成通过）。

### R17-3 · `R16` 前提不成立（9 条被跳过）→ **夹具重做**

原因：原来借 `retAtk` 上那张 Ironclad，但那一格到这一节已被前面几段用掉/清掉 ⇒
`BoardViewAt != null && HasCommand(Melee)` 失败。改法：
1. **现找一张真实近战单位**摆上场：扫卡池**按卡对象**挑（⛔ 不按名字查）——
   **本局阵营优先**、`Type == "unit"`（督军是 `hero`，自然排除）、`Attack > 0`；
2. 找一个**我方空格** `BoardSpec.IsDeployable && Board[s] == null`，`new UnitState(ctx.NewInstance(def), false) { Exhausted = false }` + `RefreshAll()`；
3. 前提拆三条并**各自出声**：① 找得到卡+空格（找不到 = **夹具问题**，下面 9 条会跳过）；② 场卡视图建出来了；③ 这一手**有「近战」**；
4. 收尾把那张单位撤掉（`Board[s] = null` + `RefreshAll()`），不影响下面「靶场还原」。

**执行到几条**：R16 的 **9 条现在都会真的执行**（前提三条绿则 9 条全跑；任一前提红 ⇒ 文案明说「被跳过」= 没验，⛔ 不当通过）。

---

## 追加（第九批）· R18 · 三轴旋转 + 战术卡 0.5 偏移（AU1 第六批）

> 只改 `Editor/BattleScene.cs`。typecheck 运行时 0 / 编辑器 0；行尾 **CRLF 45,600 / 45,600**。

**落了 13 条 `Check` + 改/换 1 条（`R10①` 的回牌库那条）。**

| 件 | 条数 | 断什么 / 怎么断 |
|---|---|---|
| **① 数据装载** | **2** | `PlacementOf("SporeMine_Explosion")` ⇒ `RotXOn == true`、`RotXDeg == 45`、`MoveBody` 开、`animTime == 0.6`；曲线三键逐值 `(0,0)(0.2373,−0.952)(1,1)`。🧨 `RtRow` 少一列 / 被钳 ⇒ 红 |
| **② 旋转到位** | **2**（数据侧 + 行为侧） | 数据侧：同一份曲线按原版式子 `SlerpUnclamped(identity, AngleAxis(45°,right), curve(1))` 算 ⇒ X = **45°**；行为侧：真事件驱动后 0.6 s 末卡 `rotation` 的 X ≈ **45°**（容差 5°） |
| **③ Unclamped 灭自证** | **2**（数据侧 + 行为侧） | 数据侧：`t01 = 0.2373` ⇒ X ≈ **−43°**（区间 −48…−38）；行为侧：整条位移里**采到的最低 X < −38°**。🧨 `SlerpUnclamped`→`Slerp`/钳 0 ⇒ 全程 ≥ 0° ⇒ 两条都红 |
| **④ `v` 项** | **2** | 数值前提：`EffectsAnchorHeight(场卡) × AttackPositionYOffset` **> 0**（证明那一项不是 0）；源级：`end.y += CardFeel.EffectsAnchorHeight(target) * CardFeel.AttackPositionYOffset;` 这句在。🧨 删掉那句 ⇒ 红 |
| **⑤ `play` 桶 0.5 四档** | **3** | 常量：`EnemyTargetingHold` **0.5** · `ReplaySkipAnimDivisor` **3** · `PlayBucketHandPlayedHold` **1.30**；源级·门：`def.IsUnit ⇒ 1.30` + `Type=="tactic" && Targeted==1 && e.Player != _me` ⇒ 0.5（🧨 门写成只看 `Type` ⇒ 红）；源级·回放 ÷3 ⇒ **0.1667** |
| **⑥ 回牌库 0.95** | **1（替换）** | 把 R10① 里那条「回牌库今天按 0.75」**换成**：临时把 `EventTiming.ReturnDestOf` 钉成 `1`（Deck），断 `ReturnHoldOf == 0.95`（0.55 + `MoveToDeck` 0.1+0.3），`finally` 还原。🧨 改 0.65（那两条路我们没有）/ 0.75（回手档）⇒ 红 |

**为什么②③④⑤有一半是「源级」**：`BattleDriver.PlayMinionMovement` / `PlayBucketOffsetOf` 都是 **private**，
而 `BattleDriver.cs` **不在本代理白名单**（没法加只读口）。②③ 另有一半走**真事件**（`Trigger` → `PlayPlacedPieces` →
`PlayPlacedOne` → `PlayMinionMovement`：把 `TL37 Spore Mine` 摆上场、发一条 `Trigger` 信号、逐帧采样它的
`rotation.eulerAngles.x`（用 `DeltaAngle` 去掉 0..360 回绕）），所以**旋转本身是行为级**；④⑤ 的门/公式是源级。
⚠️ 行为级那半的前提（TL37 在池、我方有空格、场卡视图建出来）都**单独出声**；若真事件没跑到（例如桶/归口变了），
`xLast ≈ 45` 那条会红并印出实测末值 —— **不是静默通过**。

---

## 追加（第十批）· R19 · 易伤两拍的 7 条（AU1 第七批）

> 只改 `Editor/BattleScene.cs`。typecheck 运行时 0 / 编辑器 0；行尾 **CRLF 45,747 / 45,747**。

**落了 12 条 `Check`（覆盖 7 项 + 1 条夹具前提）。**

| 项 | 条数 | 怎么断 |
|---|---|---|
| **① 两拍齐全** | **2** | 真造一个**带 `vulnerable` 的敌方单位**（`UnitState.AddKeyword("vulnerable",3)`，先摘掉护甲）→ `RuleCore.ApplyDamage(ctx, u, 5, …)`（**真引擎口**）⇒ `ctx.Signals` 里 `EvtKind.Hit` **恰 2 条**、**恰 1 条**带 `IsVulnerableSecondHit`；另一条是**判据换位前提**：`EventTiming.HitIsVulnerable` 对**主拍 false / 第二拍 true**（旧判据「受击方带 vulnerable」会对两条都 true ⇒ 红） |
| **② 主拍 1.0** | **1** | `DurationOf(hits[0]) == 1.0`（字面量）；🧨 旧判据 ⇒ 主拍也变 1.4 ⇒ 红 |
| **③ 次拍 1.4** | **1** | `DurationOf(hits[1]) == 1.4` **且** `== 1.0 + HitVulnerablePreWait`；🧨 那个常量置 0 ⇒ 红 |
| **④ 无易伤** | **1** | 摘掉 `vulnerable` 再打一次 ⇒ `Hit` **恰 1 条**、带位 false |
| **⑤ 致死** | **1** | `Health = 1` + 易伤 3 + 5 点伤害 ⇒ `Hit` **恰 1 条**（第二拍的闸 = 主伤害后仍 > 0） |
| **⑥ 第二拍不后坐 / 不震屏** | **4** | 真播一遍（先 `animateFeel = true`，`finally` 式还原）：①主拍 `CardFeel.NoAttackerFallbackCount` **+1**（夹具无攻击方 ⇒ 走退路那一格）；②第二拍那一拍该计数**不涨**；③卡的 `localPosition` 差 **≈ 0**（punch）；④**源级**：`bool recoilBeat = !meleeAttackFollow && !e.IsVulnerableSecondHit;` + `if (recoilBeat) CardFeel.ShakeCameraForHit(`（后坐与震屏共用同一个闸）。🧨 删 `&& !e.IsVulnerableSecondHit` ⇒ 一次挨打抖两下/震两次 ⇒ 红 |
| **⑦ 整条 2.4** | **1** | `DelayBetween(hits[0], hits[1]) == 0` **且** 两条 `DurationOf` 相加 = **2.4**；🧨 在中间再补 0.4 ⇒ 红 |
| 夹具前提 | **1** | 找得到单位卡 + 敌方一个空格（找不到就出声，⛔ 不读成通过） |

**夹具卫生**：④⑤ 这两趟留下的信号在断言后**摘掉**（`ctx.Signals.RemoveRange`），再把那张夹具单位撤掉 + `RefreshAll()`
—— 避免「一张夹具单位的 Hit/Death」排进时间线去影响后面那些节（协调台点名的那类「按下标取 `ctx.Signals`」/「数 `Hit`」
断言因此不会被本轮夹具多出的条目污染）。

---

## 追加（第十一批）· R20 · 主动能力候选侧的 4 条（AU1 第八批）

> 只改 `Editor/BattleScene.cs`。typecheck 运行时 0 / 编辑器 0；行尾 **CRLF 45,897 / 45,897**。
> （中途两轮 typecheck 报的是**别人正在写的文件**：`CardView.cs` 的 `UnitNameNoDesc*` ×3、`NetBattleTest.cs` 的
> `INetChatHost` ×1；它们落地后重跑 = **0 / 0**。）

**落了 13 条 `Check`（4 项 + 4 条前提 + 1 条口径说明）。**

| 项 | 怎么断 |
|---|---|
| **① 只敌方档** | 按**卡对象**扫一个样本（单位 + `Ability != null` 且 `EffectTargets.NeedsPick` + `criteria (1,0)` + `HasAlternativeAbility == 0`）→ 摆上场/选中/提交技能 ⇒ `AbilityPickSideForTest == 1`、`CommandTargetSideForTest == 1 - MySeat`、出声计数**不涨** |
| **② 友方档（AM69）** | `AM69` ⇒ `AbilityPickSideForTest == **2**`；**如实记**：它的 `Card.Ability == null` ⇒ `CommandTargetSide` 在候选侧那一支**之前**就回 **-1**（`§十五·三` item 4：可达友方 = 0）⇒ **「退回 + 出声」这一半不能拿它验** |
| **②b 友方档的「退回 + 出声」** | 扫一个**友方档且 `Ability != null`** 的样本 ⇒ 读数 **2**、`CommandTargetSideForTest` **仍 `1-MySeat`**、出声 **+1** |
| **③ (1,1) / (0,0)** | 分别 ⇒ 读数 **3** / **0**，且都**仍退回敌方** + 出声 +1 |
| **④ 判不出档** | 扫一个 `HasAlternativeAbility == GateUnknown`（⇒ `UsesAlternativeAbility` 回 `null`）的样本 ⇒ 读数 **0**、仍回敌方、出声 +1 🧨 猜默认侧 ⇒ 红 |

⛔ **没有**写「候选已含友方部队」——今天做不到（`§十五·三`：`UseAbilityCore` / `ResolveTarget` 都写死敌方）。

**两处与协调方口述不一致（按实现 + 它的 doc 为准，如实记）：**
1. **② 的 `CommandTargetSideForTest == 1 - MySeat` 对 AM69 不成立**：`CommandTargetSide` 有前置闸
   `if (u.Ability == null || !NeedsPick) return -1;`，AM69 正好落在这一支 ⇒ 拆成「② 读数」+「②b 换样本」。
2. **②b 的「再问不再涨」不成立**：`NoteAbilitySideGap` **先 `AbilitySideGapWarningsForTest++`、再查去重闩**
   ⇒ 计数是「**每次退回**都 +1」（doc 自己写「只增不减 / 一共出声过几次」），
   「每张卡每类原因一次」只作用在 **`LogWarning`**。断言写成「第二次也 +1（≥1）」并附说明；
   ⛔ 没按「不涨」写（否则会假红）。










