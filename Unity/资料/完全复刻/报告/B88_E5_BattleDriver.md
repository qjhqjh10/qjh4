# B88 波 1 · E5 —— `Battle/BattleDriver.cs`（战斗表现/接线）

> 会话 2026-10-27 · 执行代理 `E5` · 白名单：`CardPresentation/Battle/BattleDriver.cs`（+ 类尾新增成员）。
> 账补丁：`资料/完全复刻/账/_parts/B88_E5.tsv`（8 行 · 11 列 · `merge_ledger_parts.py --dry` 已验：更新 8 / 新增 0）。

## 〇、`PlayAsks` 接线（第一件，单独先做）

**改了哪一行**：`BattleDriver.cs:10974`，`PlayAsks(CardDef card)` 的第一句。

- 改前：`var all = RuleCore.PlayerChooseOps(card);`
- 改后：`var all = RuleCore.PlayerChooseOps(Ctx, _me, card);`
- **现在等于**：整张卡的 ask 仍由旧一参版 `PlayerChooseOps(card)` 算（判据只此一处），再过一道
  `EffectResolver.NeedsToChooseFromPool(Ctx, _me, card, null)` 的原版闸 —— 闸判否（不是这侧回合 / 带 jam /
  法力不够付誓约）⇒ 返回**空表**并 `Ctx.Log` 出声（一句都不静默）。
- ⛔ **没有换 `AbilityAsks`**：`UM97 Suppressor` 的唯一 ask 在 `Oath 1:` 正文里，接在 `AbilityAsks` 上会被误吞
  （判据 = `EffectResolver.cs:17538-17543` 那段）。
- 行数守恒（原地改写一行，不新增行）⇒ 不推账行号。`typecheck` 0/0。

## 一、结论

- 本轮**落地/确认 8 条** C（其中 4 条是本笔真改代码、4 条是复核确认早已在盘上），全部写进补丁。
- 其余 **35 条**保持 C，逐条给出阻塞原因（见 §三）。**没有一条是「不想做」** —— 每条都落在
  「原版死码 / 判据在别人文件 / 下一波明确划走 / 数据或素材本地缺」这四类之一。
- `typecheck`：**本笔三个 .cs 改动落完之后跑出 运行时 0 / 编辑器 0**。收尾时复跑两次出现 **2 条错，全在
  `CardPresentation/Battle/CardDisplayWindow.cs`（E6 的独占文件）**，且两次行号不同（924→922）⇒ **另一个代理正在写那个文件**
  （半成品编进来），⛔ 按简报「错误全在别人文件上就别去改」处理。本笔只动 `BattleDriver.cs`，无错。
- 行尾：整篇 **CRLF 36287 行 / 裸 CR 0**（二进制核过，未被翻）。

## 二、改动清单（真动代码的 4 条）

| 账行 | 改了什么 | 落点 |
|---|---|---|
| 其他_基础设施:589 `PlayerManager.ToggleCurrentMana` | 能量数字在**伏击（面朝下）**时印 `?/{total}`，否则 `{unused}/{total}` | 两处 `SetText` 改走 `EnergyTextOf`（`:31816` / `:31898`）；类尾 `AmbushHidesEnergy`:36242 / `EnergyTextOf`:36254 |
| 战斗界面_HUD:696 `BattleManager.CanShowCemetery` | 补第一道闸 `duringBattleFlag(+0x25c)` 的等价物 `Ctx != null && !Ctx.IsOver` | `CemeteryRowClickAllowed` getter 首句（`:12363`） |
| 战斗界面_HUD:725 `SetupDataDependentElements` ⑤ | 对局起始毫秒 `battleStartMillis` 在 `Begin` 取 UTC 毫秒 | `Begin` 与 `ResetReplaySpeed()` **同一行**（`:4937`）；读口 `BattleStartMillisForTest`:36277 |
| 战斗界面_HUD:731 `StartBattleManager` ② | `DOTween.Init(false, true, Default)` + `SetTweensCapacity(200, 10)` | 同一行调用 `InitDotweenAsOriginal()`（`:4937`）；实现 :36279（静态闩、幂等） |

**复核确认（零改动，补丁改状态 A）**：
- 特效动画:505 `ApplyMovementDisappearAnimRotations` → 现读 `:23848` 就是 `Quaternion.LerpUnclamped`，与 `.c:54` 一致。
- 战斗逻辑驱动:532 `ResolveRemoveEffect` → `PlaySignal` 的 `EnchantmentRemoved` 支在 `:22885`，②③ 都在。
- 日常_奖励 `BattleEventsController.CheckHealth()` / `Update()` → 与本文件 `Initialize` 那一行**同一套内联**
  （`UpdateHud` 每帧比 `_foeSkullHpSeen` 缓存），原版两条 `HealthChangeSignal` 的消费者已内联为
  `_foeSkullCount` / `_myWarlordMinHp` ⇒ 无 signal 对象但无行为差。

## 三、没做完的 35 条（保持 C · 逐条阻塞）

### (a) 原版本身是死码（0 调用点；按铁律 11 例外① —— 实现出来就是死代码）
- **HUD:713 `GetUnitSizeRatio`** —— `decomp_full` + `out_cpp2il_dev_ilspy` + `out_il2csharp` 三源**只有定义那一份文件**，0 调用点。
- **战斗逻辑驱动:698 `CanPlayWithOathNowInPlay`** —— 同上，三源 0 调用点。
- **特效动画:1172 对应物 `PlayerHand.HideHand`** —— 同上（1 个文件 = 它自己）。
- **HUD:759 `HighlightBloodThirst`** —— `bloodThirstFrame(+0x268)` 恒 null（既有 D 行的结论仍成立）；且该行在账里**重复**（见 §五·2）。

### (b) 判据/生产端在别人文件（本波白名单外）
- HUD:20 `SetupActiveSkill` —— 只剩 ability 级 `targetsAvailableCounter` 的数据列（`CardDef`/`CardDatabase`，E4）。
- HUD:695 `CanMoveHandCard` —— `playerHand.animFlag` 等价物在 `CardInteraction`/`HandLayout`（E6）。
- HUD:709 `GetCurrentTutorialStage` —— `Ctx.CampaignStage` 写点在 `RuleCore`（E1）。
- HUD:722 `ResolveDisplayFromHand` —— 渲染侧已备；生产者 `enemyhand` 支在 `EffectResolver.DoChooseCard`（E2）。
- HUD:741 `Update` B08 —— 要 `TutorialScript` 新谓词（引擎侧）；且原版这一支**只打一条 LogWarning、零局面影响**。
- HUD:755 `GetCurrentLocalizedAbilityDescription` —— 落点 `SkillPanel.cs`（E6）。
- 战斗逻辑驱动:196 `ApplyEnvEffect` —— ① `TriggerOtherCardAbility` 引擎侧缺（`RuleEngine/**`）。
- 特效动画:208 `GetEnvCardListDraft` —— 进攻半已接；防御半数据是 `CardDef` 而 `ChoicesForDraft` 给 `OffensiveCards.Card`（**类型不同构**）。
- 特效动画:403 `SwarmVFXController.Initialize` —— `CardMaterialHelper.ConfigureMaterial`（跨文件）。
- 特效动画:503 `ActivateTraitParticlesFromCodeInTarget` —— 位置错一格，落点在特效侧。
- 特效动画:508 `DissolveWholeCard` —— 三句都在卡面 API（`CardView`）。
- 特效动画:516 `ResolveAttackAnimationEffects` —— 三档命中音 + 命中粒子（音频/特效侧）。

### (c) 本波简报明确划给下一波
- HUD:762 `OrientToTargetingDirection` / HUD:768 `SetCardMovement` / HUD:777 `CardScript.Update` —— **朝向的「旋转」**（瞄准/拖卡只做了位移）。
- 特效动画:486 `AddCardAnim` —— **四道 priority 插队语义**。
- 特效动画:506 `CanContinueAnim` —— **78 处收口成 1 条共用闸**。

### (d) 数据 / 素材 / 时序判据本地缺
- HUD:378 `ChooseCardMenu.Setup` ④ 回放自动选 —— 我们的回放**重放的是动作流、不弹 ask**；这套接线在当前回放数据下无触发路径。
- HUD:684 `ActiveWarlordAbility.OnTouchUpAsButton` ④ —— `PressedBtn` 按压件本地全树只命中一个 `MonoScript`，判据缺。
- HUD:693 `CanDisplayHandCard` —— `UIstate==1` 等价物未定；且代码里明写「⛔ 别拿 `BattleUiIsNormalBattle` 挡轻点开大卡窗」（换牌期原版照样开）。
- HUD:750 `DisplayOffensiveCardInCenter` / HUD:756 `GetEnemyCardPlayedDisplayPosition` —— 只剩「`enemyHand` 世界点 → 我们的点」这一处 🔶 残差。
- 战斗逻辑驱动:89 `CheckIfWaitingToClickCard` —— 谓词无消费者（F5 已补轻点灵族灵魂石），缺它不构成可观测差异。
- 战斗逻辑驱动:351/352 —— 数据源是服务端 LiveOps（P3，已裁定不复刻），行为等价。
- 战斗逻辑驱动:439 `LeaveBattle` —— `ReleaseCardbacks`（我们架构里没有「借出卡背」）+ 教程首两关重开链。
- 战斗逻辑驱动:1037 `GetThisCardPlayedDamage` —— 引擎侧取数口（`RuleEngine/**`）。
- 教程_引导:133 `ClickContinue` —— 6 关 462 条动作 `tipWithContinue` **全 false**（零可见影响）；且钮在 `TutorialOverlay`（非白名单）。
- 特效动画:485 `ShowPlayerActionAnim` —— 三处如实差（同卡多份挑谁 / 攻近远标记 / 指针恒在），横跨教程执行器。
- 特效动画:492 `ResolveAnimInfoRangeAttack` —— `animInfo+0x80` 的 `UnitTweenSO` 序列。
- 特效动画:493 `ResolveBloodThirstAnim` —— `cardState == 5` 那一项我们无等价物（濒死已离场）。
- 特效动画:507 `CardToMinionAnim` —— `PlayInvokeAnimation` 的 clip 名是静态块字符串（未解出）。
- 菜单_外壳:211 `IsPointerOverUIObject` —— 原版 = 「指针压在任何可射线件上」（`RaycastAll`），建议落点是 `Shell/PointerLayer`（白名单外）。

## 四、没查清的部分（如实留）

1. **HUD:696 的行为差异**：原版按「进/出事件」翻一个 bool，多张伏击时**第一次翻开就还原**成真值；我们按**当前棋盘**算（还有面朝下的就继续藏）。已写进代码注释与补丁差异列，未推翻原版逻辑的意图。
2. **HUD:693 `UIstate==1`**：原版 10 个 UIstate 值到我们的「模态/演出」状态没有一一映射表，未裁。
3. **特效动画:208 防御半**：两种数据类型不同构，需要先裁「用 `OffensiveCards.Card` 还是卡池 `CardDef`」，未动。

## 五、顺手发现（⛔ 未越界改）

1. **`StartBattleManager` 那行的「Assets 全树 0 处写 `Time.timeScale`」已过期** —— 回放速度档后来加了写口
   （`SetReplaySpeed` / `ResetReplaySpeed`，本文件 `:35954` 一带）。它的 ① 那半其实早已被 `Begin` 首行的
   `ResetReplaySpeed()` 覆盖。
2. **账里 `战斗界面_HUD:759 HighlightBloodThirst()` 有**两**行**（一行 D、落点 `—`；一行 C、落点 `PlayTraitPulse`）。
   同 `(原版类, 方法)` 两行 ⇒ `merge_ledger_parts.py` 会判「命中 >1」拒绝，需要调度台**手工去重**。
   另：C 行那句「我们**额外**播了徽标脉冲」疑已过期 —— `PlayTraitPulse` 走的是 `TraitFrames.DisplayTrigger`，
   正对应原版的 `DisplayTriggerAnim(0xdc,1,0)`（本文件 `:25038` 已记它在）。
3. **`GetUnitSizeRatio` / `CanPlayWithOathNowInPlay` / `PlayerHand.HideHand` 三源 0 调用点** —— 本 build 的死码。
   若将来要「补齐形状」，实现出来只会是不可达代码；建议按铁律 11 例外① 结案（需调度台确认）。
4. **日常_奖励 里 `CheckHealth()` / `Update()` / `Initialize()` 三行是同一个机制**（都内联在 `UpdateHud`）。
   本笔把前两行也判 A 并与 `Initialize` 行对齐；后续别再当三条独立缺口重复施工。
5. **HUD:725 ⑦ `isReplayPlayActive` 的等价物是 `_replayPaused`（反向语义）**，两者命名相反 —— 将来若有代理
   按「0x521 恒 false / true」直接找字段会找不到，已在代码与补丁里写明。

---

# 第二批：朝向旋转 + `AnimGate` + AddCardAnim

> 2026-10-27 · 同一代理 `E5` · 补丁 `账/_parts/B88_E5b.tsv`（5 行 · `merge_ledger_parts.py --dry` 验：**更新 5 / 新增 0**）。
> 白名单扩到 `Hand/CardInteraction.cs`；新建 `Battle/AnimGate.cs`。`typecheck` **运行时 0 / 编辑器 0**。

## 〇、一句话小结

| 活 | 结果 | 状态 |
|---|---|---|
| 1 朝向「旋转」整条 | **落地**：瞄准期 3D slerp（2D 化）+ 拖卡三轴倾斜（数值从 ctor 挖到） | 762 / 768 / 777 → **A** |
| 2 `CanContinueAnim` 收口 | **共用闸 `AnimGate` 已建**；`BattleDriver` 的 8 处动画闸全接 | 506 → **C**（剩余是别的文件） |
| 3 `AddCardAnim` 四道 priority | **骨架落地**（lane + 消费序）；调用点映射未普查 | 486 → **C** |

## 一、活 1：朝向的旋转

### 1.1 瞄准期（`OrientToTargetingDirection`，账 762 / 777）
- 落点：`BattleDriver.TickTargetingLean`（`:16041` 一带）。写完 `localPosition` 之后：
  `localRotation = Quaternion.Slerp(当前, q, clamp01(dt / CardFeel.ChargeTime))`；`_leanHomeRot` 在起手那一刻捕获（`:16007`），
  收手时 slerp 回它并把 `_leanActive` 清掉。
- `q` 的来源 = 「我方单位 → 对方督军视图（`ViewAt(1-_me, BoardSpec.WarlordSlot)`）」的屏幕角 + `CardFeel.ChargeAngleDeg`。
- **🔶 自己设计（非原版）已写进代码注释**：原版是 3D `LookRotation`（3D 卡转身面向目标），我们是 2D 卡，
  取「卡体 +Y 指向目标」的 Z 角；**逐帧 `t` / `slerp` / 角度来源都照抄，只有 3D→2D 的投影方式是我们挑的**。
  目标视图取不到 ⇒ 保持原朝向（不猜角度）。
- 原版侧符号 `isPlayer ? +1 : −1` 恒取 `+1`（本方法只跑我方 `_myUnits`）。

### 1.2 拖卡倾斜（`SetCardMovement`，账 768）
- 落点：`CardInteraction.UpdateDrag`（`:987` 一带，位置更新之后）。
- 公式照抄 `.c:69-96`：`rx = (max−min)×clamp01(Δy/reduction+0.5)+min`；`ry` 用 **|Δx|**（原文清符号位）。
- 🔑 **判据不是猜的**：`maxCardRotation=(20,20)` / `minCardRotation=(−20,−20)` / `movingRotationReduction=10`
  出自 `decomp_full/CardScript__.ctor.c:41-45`（`0x41A00000 / 0xC1A00000 / 0x41200000`）；这三个字段在
  `CardScript.cs:1437-1443` **没有 `[SerializeField]`** ⇒ 预制体不覆盖 ⇒ **ctor 值即真值**。
  这与 `chargeAttackAngle` 那三个**不同**（那些有 `[SerializeField]`，取预制体值 —— 见 `CardFeel` 的常量表）。
- ⚠️ **仍没抄的三处**（如实记）：① 原版「距离过小/过大 ⇒ position 硬吸附 + rotationTarget 归零」那一支
  （我们的跟手是 🔶 平滑，没有硬吸附）；② `forcedExtraRotation` 取零；③ 原版写**世界**四元数，我们写**局部** X/Y
  并保留原 Z（否则手牌扇形那点倾角被抹平）。
- 收手复位靠 `CardView.SetPose`（整篇写 `localRotation`）⇒ **不需要**另写复原。

## 二、活 2：`AnimGate`（账 506）

- 新文件 **`CardPresentation/Battle/AnimGate.cs`**：`CanContinue(CardView)` / `CanContinue(Component)` / `Probe`。
  判据 = `CardScript__CanContinueAnim.c` 全 3 句。
- `BattleDriver.cs` **8 处动画闸已接**（`:24430 · :24504 · :24779 · :24882 · :24951 · :25018 · :25028 · :25140`）。
- 🔴 **就地订正 R24 的一句话**（铁律 5）：它记「**78 处**各写 `ViewAt(...) == null`」。**实测**：
  `BattleDriver.cs` 里 `ViewAt(` 共 **79 个调用**，其中**「取到局部量 + 紧接 `== null` 早退」形状只有 11 处**——
  **8 处是动画闸（已接）**，**3 处是读数据/读时长（:33575 返回时长 · :34373 取 `TraitFrames` · :18881 目标解析）**，
  ⛔ **不能换成 AnimGate**（读坐标/读数据不看可不可见）。
  另有约 10 处 `ViewAt` 经三目算式喂给动画（`:23458 / :23462 / :24007 / :24511 / :25228` 等），**需逐点判**。

### 逐文件待接清单（给下一个代理，⛔ 本波那三个文件 E6 正在写）

| 文件 | 与 `AnimGate` 的关系 | 要做的事 |
|---|---|---|
| `CardPresentation/Battle/CardDisplayWindow.cs` | **不使用 `ViewAt`**（那是 `BattleDriver` 的私有助手）⇒ **没有同一形状的缺口** | 若要接：只在其**自身动画入口**（如 `ChangeCardPosition` 一族）按「目标卡还在不在且激活」判；⛔ 别动 E6 正在写的行 |
| `CardPresentation/Hand/HandLayout.cs` | 同上（只有 `c == null` / `prev == null` 这类**布局**判据，例如 `:751`、`:765`） | 布局判据**不是**动画闸 ⇒ **不需要**改 |
| `CardPresentation/Core/CardView.cs` | 同上（内部 `_art == null` / `mf == null` 等**渲染件**判据） | **不需要**改 |
| `BattleDriver.cs` 剩余 | 3 处读数据型 + ~10 处三目喂值 | 逐点判「这一步是演还是问」；是「演」才换 `AnimGate.CanContinue` |

## 三、活 3：`AddCardAnim` 四道道（账 486）

- **骨架已落地**（全在 `BattleDriver.cs`，**没跨去 `CardDisplayWindow.cs`**）：
  `PendingSignal.lane`（`:19842`）· 消费循环并列判据（`:20681`）· `AnimLaneRank`（`:36334`，`2→0 · 1→1 · 3→2 · 0→3`）·
  `ScheduleAction(..., lane)`（`:22161`）· 自检口 `AnimLaneRanksForTest`。
- ⚠️ **现有调用点一律不传 lane（= 0）** ⇒ 全部落最低 rank、并列仍「先来先播」⇒ **与改动前逐字等价**（零回归）。
- 🔴 **仍缺**：`AddCardAnim` 的**原版调用点 → priority** 映射表（还没普查）。查到后只需各调用点传 `lane:`，
  ⛔ 别另写排序。

## 四、`typecheck`

- 本批改完：**运行时 0 / 编辑器 0**（E6 的 `CardDisplayWindow.cs` 那两条半成品错此时已消）。
- 行尾：`BattleDriver.cs` **CRLF 36359 行 / 裸 CR 0**；`CardInteraction.cs` **CRLF 2163 行 / 裸 CR 0**；
  `AnimGate.cs` **LF 55 行**（同目录 `BattleSfx.cs` 也是 LF，混行尾是本仓常态，未翻任何既有文件）。

---

# 第三批：`AddCardAnim` priority 普查 + 三目 `ViewAt` 逐点判

> 2026-10-27 · 同一代理 `E5` · 补丁 `账/_parts/B88_E5c.tsv`（2 行 · `--dry` 验：**更新 2 / 新增 0**）。
> 产出一张判据表 `判据/B88_AddCardAnim_priority表.tsv`（12 行）。**本批零 `.cs` 改动**（理由见下，零回归是硬要求）。

## 活 1：原版 `AddCardAnim` 调用点普查

**12 处 / 6 个方法**（🥇 `.c` 逐处读实参；ILSpy 把这些常量**全渲染成 `default`**，按铁律「ILSpy 丢常量」必须回 `.c`）：

| # | 原版类.方法 | priority | 判据 |
|---|---|---|---|
| 1–2 | `AbilityLogic.PlayAnim`（ILSpy `:1304` / `:1374`） | **数据驱动** `AbilityData.priority` | `.c:223` `*(param_3 + 0x44)`，`param_3 = AbilityData`；`RawCardScript__TriggerAbility.c:32` 从 `CardAbility.priority(+0x18)` 灌入；主动技那支固定 1（`ResolveActiveAbility.c:47`） |
| 3–6 | `CardScript.OnTurnStart`（`:7412/:7418/:7512/:7518`） | **1** | `OnTurnStart.c:189/206/230/246`，实参 `CONCAT44(uVar7,1)` |
| 7–8 | `CardScript.ResolvePsyker`（`:7741/:7747`） | **1** | `.c:21/:30` |
| 9–10 | `CardScript.ResolveReactor`（`:7765/:7771`） | **1** | `.c:21/:30` |
| 11 | `CardScript.ProcessTide`（`:14587`） | **1** | `.c:47` |
| 12 | `CardScript.ProcessCompanion`（`:14655`） | **1** | `.c:59` |

**为什么一个 `lane:` 都没传**（零回归）：
- ② 那两处是**我们唯一有对应**的（`PlayAbilityBeat` / `ExpandAbilityBeats`），但 `EvtKind.Ability` **不携带** priority
  ⇒ 要 `BattleEvent` 补一列（引擎白名单外）。
- 其余 10 处对应的动画（plasma / psychic 能量件 · GSC `Tide` / `Companion`）**我们根本没有** ——
  `AnimAnchors.cs:125/244` 白纸黑字「我们 HUD 里没有 Psychic / SpiritStone / Plasma 三种能量的件」；
  `cards_engine.json` 里 `"tide"`/`"companion"` 只作卡名出现。

**🔴 铁律 5 就地订正（账 486 的原差异句）**：原文说「我们时间线没有道」——**只对表现层成立**。
**引擎侧四道早就有了**：`RuleEngine/Core/ActionQueue.cs`（`ActionPriority` 枚举 + 消费序
`Interrupt(2)→Instant(1)→DelayedInstant(3)→Player(0)` + 同道具名 FIFO + 不抢占）与
`ActionPump.ListenerLane(card, triggerName)`（`ActionPump.cs:1413`，`EffectResolver` **13 处**消费、`RuleCore` 3 处）。
⇒ `CardAbility.priority` 的**分道已经接上**；缺的只是「**表现层同一时刻多条动画**的先后」那一层（批 2 加的 `PendingSignal.lane`）。

## 活 2：三目喂给动画的 `ViewAt` 逐点判（12 处，全部不换）

| 行 | 写法 | 判定 | 为什么 |
|---|---|---|---|
| `15208` | `BoardViewAt(slot) != null ? DropTargetWorld : null` | **问** | 取落点坐标 |
| `15594` | `ApplyOwnAttackType(..., ViewAt(...))` | **演，但传 null 有「清空」语义** | 换 `AnimGate` 会把「刷新底光」变成「清掉底光」 |
| `19675` | `self` / `target` 取视图喂 `ExpandAbilityBeats` | **混合** | 后面还要读下标 / 目标数，null 有别的含义 |
| `23305` | `pSelf` / `pTarget` 喂 play-bucket 偏移 | **混合** | 同上 |
| `23458` / `23462` | `PlayAbilityBeat` 的 `self` / `target` | **混合（⛔ 不能加闸）** | `self == null` 是**合法输入**：战术/非单位卡走中央展示锚（`:23464-23492`）⇒ 加闸会吞掉法术动画 |
| `24007` | `PlayMinionMovement(self ?? ViewAt(...), …)` | **演** | 但函数本身容忍 null（内部自行降级），加了只多一次判断、不改结果 |
| `24511` | `PlayMinionMovement(v, …, ViewAt(...) …)` | **演** | 同上 |
| `25228` | `attacker` 取受击方向 | **问** | 取方向 + null 回退 |
| `27758` | `FoeHandViewAt` | **问** | 取件 |
| `34250` | `TrackedCard = … ViewAt(...)` | **问** | 记状态 |
| `35873` | 助手返回视图 | **问** | 通用取件口 |

⇒ **结论：`BattleDriver` 里真正的「演不演」闸只有批 2 已接的 8 处**，加上判「问」的 3 处
（`:33575` 返回时长 · `:34373` 取 `TraitFrames` · `:18881` 目标解析）。**本批据此零改动**，
把判定表落进账 506 的差异列。

## 本批交付

- 新文件：`资料/完全复刻/判据/B88_AddCardAnim_priority表.tsv`（12 行 · 6 列）。
- 账补丁：`B88_E5c.tsv`（486 / 506 两行，状态仍 C，差异列补上「查到哪一步 / 还差什么」）。
- `.cs` 改动：**0**（两条活都判出「现在不该动」）。
- `typecheck`：本批**不改 `.cs`** ⇒ 批 2 落完时是 **0/0**。收尾复跑时出现 **7 条错，全在 `CardView.cs`（`ApplyFrameMaskFade` / `_frameMaskM…`）
  与 `SettingsWindow.cs`（`InactivateAccount`）** —— 都是别的代理正在写的文件，且两次行号不同（`4412` ↔ `12258`）⇒ 半成品编进来，
  ⛔ 按简报「错误全在别人文件上就别去改」。本代理三个文件（`BattleDriver.cs` / `CardInteraction.cs` / `AnimGate.cs`）**零错**。

---

# 第四批：`BattleEvent.Priority` 列 + 表现侧消费（486 的最后半刀在别人白名单）

> 2026-10-27 · 同一代理 `E5` · 补丁 `账/_parts/B88_E5d.tsv`（1 行 · `--dry` 验：**更新 1 / 新增 0**）。
> `typecheck`：本笔两处 `.cs` 改完之后**跑出过 运行时 0 / 编辑器 0**。⚠️ 之后每次复跑都有**别人正在写的文件**在报错，
> 且每次是不同文件 / 不同行号（先后撞到 `NetSession.cs`（E8）· `LiveOpsLocalCatalog.cs` / `LiveOpsState.cs`（Shell 线））⇒
> **全是半成品编进来，本代理没去改**。本笔两个文件（`BattleEvent.cs` / `BattleDriver.cs`）在每次运行里都**零错**。

## 做了什么

| 文件 | 改动 |
|---|---|
| `RuleEngine/Core/BattleEvent.cs` | 新增 `public int Priority;`（:787）—— doc 里写全四条判据链（`AbilityLogic__PlayAnim.c:223` → `AbilityData.priority` → `RawCardScript__TriggerAbility.c:32` → `CardAbility.priority(+0x18)`；主动技固定 1 = `ResolveActiveAbility.c:47`；缺省 1 = `CardAbility__.ctor.c:5`）。**只加这一列及其注释**，⛔ 没动别的。 |
| `CardPresentation/Battle/BattleDriver.cs` | 三处排期带道：`PlaySignals` 主信号 `:19558`、`ExpandAbilityBeats` 的每一拍 `:19724`、空拍兜底 `:19745` —— 都写 `lane = e.Priority`。 |

**零回归**：发侧一处都没接 ⇒ 所有事件 `Priority = 0` ⇒ `AnimLaneRank(0) = 3`（最低道）、并列仍「先来先播」⇒
与批 2 之后**逐字等价**（今天的行为一个字都没变，只是**通路**铺好了）。

**指纹口径**：`NetProtocol.Fingerprint` 读的是 `ctx.Events.Count`（本仓多处注释写死这条）——
新增字段**不改条数** ⇒ 指纹稳定。已在施工单 §三·2 写成验收项。

## 没做的那半刀（已写成施工单）

`判据/B88_E5_AbilityPriority_发侧施工单.md` —— 三处，全在 E1/E2 白名单：
1. `BattleContext.Emit(...)`（`:1938`）加 `int priority = 0` 形参 + `Priority = priority,`。
2. `BattleContext.cs` 的那个 `Emit(EvtKind kind, …)` 是**唯一入口**，其余重载不用动。
3. 四处 `ctx.Emit(EvtKind.Ability, …)` 传实参：
   `RuleCore.cs:11621`（`UseAbilityCore`，主动技 ⇒ **1**）· `:11789`（誓约 ⇒ **1**）·
   `:12047`（`UseAlternativeCore` ⇒ **`u.Card.AbilityTriggerPriority(<trigger 名>)`，认不出传 1 并出声**）·
   `:15425`（⚠️ 形状与 ① 不同，请 E1 先判它属于哪一支）。
   ⚠️ 施工单里点名了两本字典别混（`CardDef.TriggerPriority(keyword)` = 引擎关键词 · `AbilityTriggerPriority(name)` = 原版 `AbilityTrigger` 枚举名）。

## 边界（⛔ 复述一遍，别下个会话踩）

- `CardView.cs` 当时有别人在写（我看到的 `ApplyFrameMaskFade` 错就是它）⇒ **没碰**；`Net/NetSession.cs` 同理。
- 我这一批**只**碰了上面两个文件。
- 行尾：`BattleEvent.cs` **LF 847 行 / 裸 CR 0**（原就是 LF，Edit 未翻）；`BattleDriver.cs` **CRLF 36362 / 裸 CR 0**。

---

# 第五批：重取 C 行 · 解禁后能做的都做掉

> 2026-10-27 · 同一代理 `E5` · 补丁 `账/_parts/B88_E5e.tsv`（2 行 · `--dry` 验：**更新 2 / 新增 0**）。

## 一、关掉 1 条 + 订正 1 条

### 1. `战斗界面_HUD:722 ResolveDisplayFromHand` → **A**（生产端已由 E2 落地）
- 生产端：`EffectResolver.cs:4771` `ctx.Emit(EvtKind.Draw, owner, -1, pick.Id, effect: "display")`；
- 消费端（本文件早就有）：`:12112` 的 `e.Effect == "display"` 支 → 坟场日志一行（键 `ActionDisplay{Your,Enemy}CardInHand`）；
  居中展示早就有了（`PlayAutoPlayCenterDisplay`）。
- 🔴 **本笔顺手修掉「借 `EvtKind.Draw` 当载体」带来的三处假动作**（这是真缺陷，玩家看得见/听得到）：
  1. `PlaySignals` 的「本批抽卡第几张 / 倒数第几张」计数 **跳过 display**（`:19501`）——
     不跳的话那张手牌会**从牌库方向飞出来**；
  2. 抽牌的两条音效 **跳过 display**（`:21347`；判据 = 原版 `_ResolveDisplayFromHand` 全篇没有
     `SoundAssetCollection.get_*`）；
  3. 抽牌的通用特效 **跳过 display**（`:21887`；原版那条动作零 VFX）。
- ⚠️ 如实留一格：原版 `SetCardState(10 → 1)` + `TurnCardAround` 是 3D 卡状态机，我们 2D `CardView` 没有等价物。

### 2. `特效动画:516 ResolveAttackAnimationEffects` → 仍 **C**，但**订正掉一半**
- 原行「①命中音三档」**不是缺口**：`meleeAttackHitSmall/Medium/Big` 三个 cue 在原版**指向同一个 pathID**
  （`-3640304923993188049`，判据 `BattleSfx.cs:78-80`）⇒ 我们恒喊小档 = **听感逐位等价**。
- 真实剩余只剩 **②命中粒子两档**（`+0x1b8`/`+0x1c0`）—— 属特效线（VFX prefab 装配），不在本代理白名单。

## 二、仍挡在别人文件的行（重取后逐条「等谁」）

| 账行 | 卡在哪 | 等谁 |
|---|---|---|
| `HUD:20 SetupActiveSkill` | ability 级 `ActiveAbility.targetsAvailableCounter(+0x40)` 仍**没有数据列**（`CardDef` 只有卡级 `AvailSet`，v22） | E4（`CardDef`/`CardDatabase` 补一列）；接上后**只改** `BattleDriver.ShouldShowAbilityTargetsCounter` |
| `HUD:695 CanMoveHandCard` | `playerHand.animFlag` 等价物在 `HandLayout`/`CardInteraction` 之外的排布层 | E6 |
| `HUD:709 GetCurrentTutorialStage` | `Ctx.CampaignStage` **仍无任何写点**（`RuleCore.cs:1220/2799` 自承） | E1（战役入口把 `TutorialStageData` 灌进去） |
| `HUD:741 Update` B08 | 要 `TutorialScript` 新谓词 | E1 |
| `HUD:755 GetCurrentLocalizedAbilityDescription` | 落点在 `SkillPanel.cs` | E6 |
| 战斗`196 ApplyEnvEffect` | `TriggerOtherCardAbility` 全仓仍 **0 命中** | E1/E4 |
| 战斗`351/352` | 服务端 LiveOps 数据源（P3，已裁定不复刻） | —（不追） |
| 战斗`439 LeaveBattle` | `ReleaseCardbacks`（我们没有「借出卡背」这一层）+ 教程首两关重开链 | 需先裁「借出卡背」在我们架构里是什么 |
| 战斗`698 CanPlayWithOathNowInPlay` | 原版**三源 0 调用点**（死码） | —（铁律 11 例外①，需调度台确认结案） |
| 战斗`1037 GetThisCardPlayedDamage` | 引擎侧取数口 | E1 |
| 教程`133 ClickContinue` | 钮在 `TutorialOverlay`；且 6 关 `tipWithContinue` **全 false** | E6/教程线 |
| 日常`155 SetReward` | 结算面板奖励行未建（`EndPanel` 只有骷髅行）；**无现成几何判据** | 需先取原版 `EndPanel` prefab 的 `RewardsHolder` 布局 |
| 特效`208 GetEnvCardListDraft` | 防御半数据类型不同构（`OffensiveCards.Card` vs 卡池 `CardDef`） | 需裁「用哪一份」 |
| 特效`403/503/508` | `CardMaterialHelper` / 特效侧锚点 / 卡面 API | E8/特效线 |
| 特效`486` | 发侧 `ctx.Emit` 带 priority | E1（施工单已给） |
| 特效`492/493/507` | `UnitTweenSO` 序列 / `cardState==5` 无等价 / `PlayInvokeAnimation` clip 名未解 | 特效线 |
| 特效`506` | 本代理已判「无剩余」 | — |
| 菜单`211 IsPointerOverUIObject` | 要 `Shell/PointerLayer` 暴露「指针压在任何可登记命中区上」 | 界面/外壳线 |
| `其他:589`·`HUD:696/725/731`·`战斗:532`·`日常 Update`·`特效:505`·`HUD:762/768/777` | **本代理前四批已做完**，等补丁合并（`B88_E5.tsv` / `B88_E5b.tsv`） | 调度台合并 |

## 三、🔴 顺手发现（**正在打断整包编译，优先级最高**）

**`CardPresentation` 命名空间下新出现了一个 `VarsGlobal`，与本仓原有的 `RuleEngine.VarsGlobal` 撞名。**
- 新文件：`Unity/MyGame/Assets/CardPresentation/Core/VarsGlobal.cs`（**git 未跟踪**，等级/XP/段位数学层，另一条线新加），
  `namespace CardPresentation` + `public static class VarsGlobal`。
- 后果：`BattleDriver`（同在 `namespace CardPresentation`）里那句
  `VarsGlobal.PlayedCardDisplayOffsetPx(...)` 解析到了**新的那个**（它没有这个方法）⇒ **`CS0117`，运行时程序集整包编译失败**。
- 我已在本文件里**全限定**成 `RuleEngine.VarsGlobal.PlayedCardDisplayOffsetPx(...)`（`:35763`，加了注释说明 ⛔ 别删前缀）。
- ⚠️ **但根不在我这里**：`Editor/BattleScene.cs:48999/49031` 仍报 **`CS0104`（两个 `VarsGlobal` 不明确）** ⇒ 编辑器程序集仍然红。
  **建议由那一路的写手改名**（例如 `VarsGlobalMath` / `LevelCurve`），或把它挪进自己的命名空间
  —— ⛔ 本代理没去动那个新文件（白名单外）。

## 四、本批环境

- `typecheck`：**运行时程序集 0 错**（我这侧已干净）；**编辑器程序集 2 错**，全在 `Editor/BattleScene.cs`（上面的 `CS0104`，别人文件）。
- 另：本批期间还看到 `Shell/DuelPopupWindow.cs`（`GameMode` 未找到）· `Shell/ItemDrawer.cs`（`NodeName`）· `Net/NetSession.cs` · `Shell/LiveOps*.cs`
  的瞬时错，**每次运行都换文件、换行号** ⇒ 多路并行写的半成品，未去碰。
- 行尾：`BattleDriver.cs` **CRLF 36376 / 裸 CR 0**。
