# G1 · 战斗线「Tips 与兜底审计」执行报告（2026-10-24）

> 执行代理 **G1**（有写权限）。白名单内改动：`CardPresentation/Battle/**` + `Editor/BattleScene.cs`。
> ⛔ 未跑 Unity（红线）；每批改完都跑了 `TMPDIR=/tmp/wf_g1 bash 工具/typecheck.sh`，最后一次 **运行时 0 / 编辑器 0**。
> ⛔ 未动 git、未动 `Core/**` · `RuleEngine/**` · `Shell/**` · `Net/**` · `Hand/**` · `Effects/**` · `WarpforgeVFX/**` · `工具/**`，
> 未动 `CLAUDE.md` / `项目任务.md` / 任何 `任务总表.md`，未动 `Pinned*Md5`。

## 总表

| # | 条目 | 状态 |
|---|---|---|
| 1 | `新账-58`：24 条 `Battle/Tips/*` 接入 | **20 条已在位**（逐条给出消费点）· **4 条客观做不了**（见 §1·3）· **新增覆盖表断言**（§1·4） |
| 2a | `W64·B-13` 缩时 | ✅ A19 已做完，本轮**只复核**（未重做） |
| 2b | `W64·B-14` 效果召唤飞行 | ✅ 已在位（`CompletePendingDeploy` + `StartAiDeployFlight`）；本轮**只订正一句过期注释** |
| 2c | `W64·B-21` 攻击三钮 / 底板 | ✅ A23 已做完，本轮**只复核**（未重做） |
| 2d | `W64·B-24` 「继续」右对齐 | ✅ **本轮做完**（量真字宽，删掉「半字宽 −60」近似） |
| 2e | `W64·B-25` 多卡展示窗横向滚动 | ✅ **本轮做完**（拖拽 + 惯性 + 视口裁切，删掉「缩到 45% 地板」） |
| 2f | `W64·B-26` `BattleDoors`/`EndBattlePanel` 父子 | ✅ A23 已做完，本轮**只复核**（未重做） |
| 2g | `W64·B-30` 多卡展示窗入口 | ⛔ **原版入口做不了**（要 `RuleEngine` 发效果 470 事件，黑名单）· ✅ 过期注释已订正 |
| 3 | 虫群合并那一拍 | ✅ **本轮做完**（合并件 + 存活那张虫群图标 + 3D 音 cue），含 4 条如实标注的偏离 |

---

## 1 · 新账-58：24 条 `Battle/Tips/*`

### 1·1 结论

`C1_战斗线判据_卡牌与Tips消费点.md` §二 给的 24 条键，**20 条今天已经有产品消费点**（不是「待接」），
**4 条做不了**且原因分两档（见 §1·3）。本轮**没有为了「看起来 24/24」去硬接一条近似键**。

### 1·2 逐条现状（判据 = C1 §二 + 我现读代码）

| 键 | 消费点（现读） |
|---|---|
| `AttackBlock` / `AttackBlockFlying` / `AttackBlockStealth` / `CantAttackDestroyer` / `CantAttackFlank` / `CantAttackWithZero{Melee,Ranged}Attack`（7 条） | `RuleCodes.Terms` 逐档映射（`RuleCodes.cs:339-345`）→ `BattleDriver.HintForCode` → `NotifyCantDoAction`（攻击被拒那一支 = `BattleDriver` 里 `RuleCodes.DeclareAttack` 失败后的 `NotifyCantDoAction(...HintForCode(code))`） |
| `NotEnoughMana` / `NotYourTurn` / `NoTargetAvailable` / `NotEnoughRoom` | 同上（`RuleCodes.cs:329-332`；路径 = `DoPlay` 被拒 → `NotifyCantDoAction(HintForCode(code))`） |
| `DamageFatigue` / `DamageFatigueEnemy` | `BattleDriver.DamageFatigueTextWithKey`（键在表里走 `CardText.Term`，否则兜底句） |
| `DragToTarget` | `BattleDriver.DragToTargetTerm` 的两个消费点 |
| `GoFirst` | `MulliganPanel.TurnFirstTerm` |
| `HandFull` | `BattleDriver.HandFullTerm` + `HandFullText()` |
| `InvalidTarget` | 技能目标落空那一支（`kind == Ability && code == ErrTarget`） |
| `PendingBerzerk` | `EndPlayerTurn` 的狂战闸 |
| `PleaseWait` | `WaitingForBlockingAction` 两处 |
| `UnitNotReady` | `OpenCommand` 的 `Exhausted` 闸 |

### 1·3 做不了的 4 条（**分两档，理由各不同**）

| 键 | 为什么做不了 | 出处 |
|---|---|---|
| `CantAttackCoward` | ① 现池 `coward` **0 张**（全池扫 `keywords`/`cardTraits`/`desc` = 0 命中）；② **官方 I2 表里也没有这个键**（37 条 `Battle/Tips/*` 里没有它）⇒ 原版那一档本来就是**空文本**。建一个永不返回的码 = 死码（`RuleCodes.cs:101-106` 已按这个口径记着） | `RuleCodes.cs:105`；`数据/游戏数据/官方本地化_en+zh-CN.json` 现读 |
| `CantAttackTainted` | 同上：`tainted` 现池 **0 张** + 官方表**没有**该键 | `RuleCodes.cs:103`；同上一份 JSON |
| `AttackGiantKiller` | 现池 `giantKiller` **0 张** ⇒ `IsValidAttackTarget` 那一支永远命中不了。（**但这条键在官方表里、也在我们 `Loc` 表里** —— 值齐了，只缺「成因」的引擎建模） | `RuleCodes.cs:102`；`cards_engine.json` 现扫 0 命中 |
| `MuteEnemyChat` | 消费点在**对手聊天**那条链上（`VoiceLinesController__DisplayWarlordChatMessage.c`：第 5 条之后弹 SmallTip）——**我们还没有「对手聊天」这个功能**（`SettingsPanel.SetMuteChat` 的日志自陈「那条链我们还没做」）⇒ 无处可接 | `VoiceLinesController__DisplayWarlordChatMessage.c`（亲读，tip 分支在 `+0x68` 未置位 ∧ 计数 > 4 那一段）；`Battle/SettingsPanel.cs` 的 `SetMuteChat` doc |

> ⚠️ **这三条 trait 类（Coward / Tainted / GiantKiller）要做，得先补 trait 建模 + 加码 + `Terms` 一行**，
> 三处**全在 `RuleEngine/**`**（本轮黑名单）⇒ 已作为待办写进 §5「没做/卡住」。

### 1·4 加了什么断言（`Editor/BattleScene.cs` §3d，现读在 `:6137` 起）

1. ★ **24 条键表长度 = 24**，且 **22 条 `Loc.HasEntry` 为真、缺的恰好是 `CantAttackCoward,CantAttackTainted`**
   —— 🧨 表被削掉任何一条 / 给那两条编一个译文 ⇒ 红。
2. ★ **11 个引擎码 → 各自的原版键** 1:1（`RuleCodes.TermKey`）—— 🧨 把某档换回 `ErrTarget` 那种笼统码 ⇒ 红。
3. ★ **不该响时不响**：`TermKey(ErrSlot) == null`（它只剩「参数非法」，原版无词条）—— 🧨 给它硬套
   `NotEnoughRoom` / `NoTargetAvailable` ⇒ 红（会把参数错说成规则错）。
4. ★ **该响时响**：`HintForCode(ErrCost) == Loc.T("Battle/Tips/NotEnoughMana")` 且**不以 `Battle/` 开头**
   （⛔ 不是键名）；`HintForCode(ErrSlot) == Describe(ErrSlot)`（无键码仍走人话）。
   🧨 把 `HintForCodeWithKey` 的 `Loc.HasEntry` 闸删掉 ⇒ 印键名 / 空串 ⇒ 红。

> 端到端（语音 + 抬头一次调用）那一层，**既有 §3c 已经钉着**（`UnitNotReady` / `PleaseWait` 两态 + `NotifyCantDoAction` 调用点静态普查 ≥10 处），本轮不重复。

---

## 2 · W64 兜底七条

### 2·1 `B-13` 缩时 —— 复核 A19 的落地（未重做）

现读：`BattleDriver.ShouldReduceNextTurn(...)`（判据只此一处）· `_reducedNextTurn` · `TurnSecondsForThisMatch`
里 `if (_reducedNextTurn) v = reducedTurnSeconds;` **排在 matchType 覆盖之前** · `LocalAct` 里 `code == OK ⇒ _actionsThisTurn++`。
⇒ A19 的三条断言（真值表 / 三步覆盖序 / `++`）都还在。**没有残留要补的**。
⚠️ 唯一如实项（A19 已记，本轮复核仍成立）：原版 `--` 那一跳（`RegisterPlayerActionCancelled`）**我们没有对应路径**（动作当场结算，没有「排队→取消」两段）。

### 2·2 `B-14` 效果召唤飞行 —— 已在位，只订正过期注释

**现读结论**：`CompletePendingDeploy` 里 `DeployFrom == DeploySource.AutoPlayFromHand` 那一支**已经**订了飞行票
（`autoPlay = true`），`StartAiDeployFlight` 会先跑居中展示再飞、落地接现有登场链。
⇒ 任务的「只补剩下的部分」= **没有剩余功能**；只有一句注释是过期的：
`BattleDriver` 里原写「**手牌飞行本身还没做** —— 那要复用 `StartAiDeployFlight` 的手牌载体」。
已按铁律 5 就地改写成「已完成 + 只剩我方退路」。🧨 **这条是纯注释，没有断言**（不改变行为）。

### 2·3 `B-21` 攻击三钮 —— 复核 A23 的落地（未重做）

现读：`AttackSelector.BgTopAboveAnchorPx = BarH*0.5 − BgDropY = 42.7485`、`ButtonRowCenterPxFromAnchor` /
`BackgroundTopPxFromAnchor` 两个真几何探针、`RefreshLayout` 的按档枢轴补偿都在；`BattleScene` 里那三条断言（`§B-21`）也在。

### 2·4 `B-24` 「继续」右对齐 —— ✅ 本轮做完

**判据**（`C1-A07`）：原版 `Text`(`RT_3020`) 框 `287.33×44.856` · `m_HorizontalAlignment = 4 (Right)` ·
`m_margin = 0` ⇒ **字形右沿 = 框右沿**；框右沿在组内 `−223.362`，换成「相对父件右下角」= **−247.588**。
⛔ 不需要猜「半字宽」。

**改了什么**（`Battle/MultiCardDisplay.cs`）
- 新增 `BtnTextRightEdgeX = BtnApX + BtnTextFrameRx`（= −247.588）；旧的 `BtnTextCx` 改名 `BtnTextCxFallback`（**只剩兜底**，产品路径不读）。
- 新增 `LayoutContinueText()`（`MultiCardDisplay.cs:707`）：`cx = 框右沿 − Label.WorldW/2`；量不到（`NaN`/`Inf`/≤1/≥4000）
  ⇒ 退回兜底值**并 `Debug.LogWarning`**（不静默）。`Build` 里原来那句 `AnchorBottomRight(..., BtnTextCx, ...)` 换成调它。
- 新增自检口 `ContinueTextWidthForTest` / `ContinueTextCxForTest` / `ContinueTextRightEdgePxForTest`
  —— **实时读那颗 `RectTransform.anchoredPosition.x` 与 `Label.WorldW`**（⛔ 不读本件记账字段，避免「事后被谁重新锚过」验不出来）。

**断言**（`Editor/BattleScene.cs` `§B-24`，`~:12708`）
1. 前提：量到的字宽 ∈ (1, 4000) px —— 🧨 删掉/不调 `LayoutContinueText` ⇒ 宽 = 0 ⇒ 红。
2. ★ 字形右沿 = **−247.588 ± 1 px**（**期望值独立写死**，判据算术写在注释里）—— 🧨 基准取成「条的右沿」/ 不按字宽居中 ⇒ 红。
3. ★ 字心 **不等于**旧近似 `−307.588`（±1 px）—— 🧨 改回 `BtnTextCxFallback` ⇒ 红。

### 2·5 `B-25` 多卡展示窗横向滚动 —— ✅ 本轮做完

**判据**（`C1-B05` 逐字段）：原版根上 uGUI `ScrollRect` —— `Horizontal` · `Vertical=0` · **`Clamped`** ·
`Elasticity .1` · `Inertia=1` · **`Decel .135`** · `Sens 1.0` · 无滚动条；`Content` 挂
`HorizontalLayoutGroup(spacing 500, MiddleCenter)` + `ContentSizeFitter`（宽运行时算）。
⇒ 屏幕间距 = 500 × `Viewport` 的 0.7 = **350**（A23 已改）。

**改了什么**（`Battle/MultiCardDisplay.cs`）
- **删掉「装不下就等比缩到 45% 地板」整段**（`MinShrink` 常量一并删）；现在卡**恒按自然尺寸**画。
- 新增 `ViewportClipPx`（= `x[0,1920] y[BandTop,BandBottom]`，= 窗口带）· `ApplyCardLayout()`（唯一摆位 + 重裁入口）·
  `TickScroll(world, held, dt)`（拖拽 + 惯性）· `ScrollDecelerationRate = 0.135f` · `DragSlopPx = 6f`。
- `Show()` 改为：算自然总宽 → `ContentWidthPx` = 自然宽、`UsedScale` = 自然 `baseScale`、
  `_maxScrollPx = max(0, 自然宽 − 1920)`、偏移归 0；卡建完调 `ApplyCardLayout()`。
- **排布**：装得下 ⇒ 居中（原版 `MiddleCenter`）；装不下 ⇒ 从视口左沿起 + 减 `_scrollPx`。
- `BattleDriver.TickMultiCards()` 每帧多喂一条 `_multiCards.TickScroll(WorldPointer(), PointerHeldRaw(), Time.deltaTime)`。
- `RequestCloseByUgui()` 新增闸：**这一次按下累计横向拖过 6 px ⇒ 不关窗**（原版 `ScrollRect` 吃掉这段拖拽）。

**断言**（`Editor/BattleScene.cs` `§B-25①~⑤`，`~:12590` 起）——原来那条「一排宽 ≤ max(可用宽, 45% 地板宽)」**整段重写**：
1. ★ 一排宽 = **自然宽**（±2 px）—— 🧨 把「缩到 45%」加回来 ⇒ 红。
2. ★ `UsedScale` = 自然 `baseScale`（±1e-5）—— 🧨 同上 ⇒ 红。
3. ★ 滚动上限 = `max(0, 自然宽 − 1920)` —— 🧨 忘减视口宽 ⇒ 红。
4. ★ 卡的 `CardView.Clip` = 窗口带四边（±0.5 px）—— 🧨 `SetPose` 不传 `clip` ⇒ 红。
5. ★ 真的能滚：按一下 → 往左拖 120 px ⇒ `ScrollPx ∈ (20, 上限]`、`DragTravelPx > 6`；
   **拖完抬手点背景关闭件不关窗**（计数不变）；往回猛拖 ⇒ 夹回 0。
   🧨 不夹上限 / 不按 `dx` 改偏移 / 删掉 `RequestCloseByUgui` 里那句 `if (DraggedSincePress) return;` ⇒ 分别红。
   ⚠️ 牌库张数太少（上限 ≤ 20 px）时这一支**跳过并出声**（如实记，不当绿）；①②③④ 不受影响。
6. 原「卡心距 = (自然卡宽 + 350) × k」那条**保留**（现在 `k` 恒 1）。

**已知偏离（如实记，写进代码注释）**
- 原版 `Clamped` 档带 `m_Elasticity = 0.1` 的**越界回弹**，我们**硬夹**（不回弹）。
- **拖拽阈值 6 px 是我们加的**（原版由 `ScrollRect` 自己分派点/拖；我们抬手会被全屏背景关闭件吃掉，必须自己分）。
- 原版 `CardCollider.sendEventsToScrollRect` 把**卡片上的拖拽**转给 `ScrollRect`；我们的指针是驱动层世界坐标轮询
  ⇒ 「在卡上拖」与「在空处拖」都能滚（覆盖面更大）。
- **性能**：拖动时每帧重摆 + 重裁一次 N 张卡（`CardView` 的视口裁切会重建网格 + `ForceMeshUpdate`）。
  真实机手感未验（本代理不跑 Unity）。

### 2·6 `B-26` `EndBattlePanel` / `BattleDoors` 父子 —— 复核 A23 的落地（未重做）

现读：`BattleDoors.CreateEnvelope()` / `CanvasTransform` / `PairCanvas()` 都在；`EndPanel` 走外骨架 + 子画布。
`BattleScene` §A218 的六条树形断言也在。

### 2·7 `B-30` 多卡展示窗入口 —— ⛔ 原版那一半做不了

**判据**（`C1-A08`）：原版**唯一入口** = `AbilityEffect.showCardsWindow(470)` → 动作 86 →
`ResolveAction` case 86 → `DisplayMultiCardView`（只在 `IsPlayerTurn` 时显示）。全池 1,351 张里
**只有 `GSC55`「Eve of the Uprising」一张**用它（`cards_raw.json` 现扫 1/1351）。

**做了什么**
- 订正**两处过期注释**（铁律 5）：`MultiCardDisplay.cs` 文件头 ⑤ 与 `BattleScene.cs` 多卡那一节开头 ——
  原来都写「原版从 `ResolveAction` 打开（**环境卡 / 战绩卡组**）」⇒ 那两条**不成立**。
  现在写明：唯一入口 = 效果 470 / 只有 GSC55，并标明「点我方牌堆」**是我们挑的额外入口**。

**为什么没接原版入口**：`RuleEngine` 今天**根本不认识效果 470**（`EffectResolver` 没有 `showCardsWindow` 分支，
引擎侧连「动作 86」这个事件都不发）⇒ 表现层没有任何东西可订阅。要接必须先在 `RuleEngine/**` 里
把 470 解析成一条事件（并带上「要摊开哪些卡」+ 标题键 `Card_Abilities/<名>`），**那不在本件白名单**。
⇒ 记为待办（§5）。

---

## 3 · 虫群合并那一拍 —— ✅ 本轮做完

**判据链**（全部亲读）
- **引擎**：`RuleCore.SwarmMergeNow` 尾部 `FireTriggerAlways(ctx, host, KeywordTable.Swarm, p, hostSlot, just)`
  ⇒ 发 `EvtKind.Trigger`、`Keyword = "swarm"`、`Slot` = **合并后还活着那一格**（按身份现查，连续棋盘补位后仍准）。
- **原版**：`CardScript__ExecuteSwarm.c:39-79` —— 在**被并掉那张**的世界位姿上 `Instantiate`
  `Swarm Card Merge VFX`（= `CardTrait "Swarm"`(650) 的 `customObject`），`localScale` 取被并掉那张的；
  该 prefab 的 `SwarmVFXController` 靠 clip `Swarm Triat Merge.anim` 的 **1.2 s 动画事件 `OnAnimationEndEvent`** 收尾
  （A31 已补接收口）⇒ ⛔ 不传 `autoDestroySeconds`。
- **音**：原版 `sound` = cue **`Trait Swarm Tyranid`**，走 `Play3D` ⇒ `is2d = false`。
  ⚠️ **必须单独播**：`Swarm Card Merge VFX` **没有 `AnimFXController`**（效果库 控制器数 0），
  而 `WarpforgeEffectPlayer.BuildSounds()` 只吃 `AnimFXController.sounds[]` ⇒ cue 在表里也没人播
  （**G3 2026-10-24 已把 cue 接进 `animfx_sounds.json`**，460/460 解析通过）。
- **图标**：同一跳把**存活那张**的 `CardView.SetSwarm(true)`（`SetSwarm` 此前**游戏内 0 调用点**）。

**改了什么**
- `BattleDriver.PlayFeel` 的 `case EvtKind.Trigger:`（`BattleDriver.cs:19616`）加一句 `PlaySwarmMergeFeel(e);`
- 新增 `PlaySwarmMergeFeel(BattleEvent)`（`BattleDriver.cs:19801`）+ 常量 `SwarmMergeVfxName` / `SwarmMergeCue`
  + 自检只读计数 `SwarmMergeFeelCount` / `SwarmMergeLastSlot` / `SwarmMergeCueFound`。
- 新增自检口 `BoardViewForTest(owner, slot)` / `SimulateSwarmMergeTriggerForTest(player, slot, keyword)`
  （**直调产品同一个函数**，⛔ 不在自检里另写一份）。
- 订正一处过期注释（铁律 5）：`PlayTraitParticles` 上面那段原写「`swarm` 合并那一下**不发 `EvtKind.Trigger`**」
  —— A1356 收口之后**已不成立**。

**断言**（`Editor/BattleScene.cs` `~:37127`，W58 那一批之后）
1. ★ **不该响时不响**：`keyword = synapse` ⇒ 计数不动 —— 🧨 函数不判关键词 ⇒ 红。
2. ★ **该响时响**：`keyword = swarm` ⇒ 计数 +1 —— 🧨 主体被删/对 swarm 早退 ⇒ 红。
3. ★ 落点 = **还活着那一格**（`LastSlot == e.Slot`）—— 🧨 写死 `slot+1` / `slot` ⇒ 红。
4. ★ 存活那张 `SwarmVisible` 为真 —— 🧨 删掉 `v.SetSwarm(true)` ⇒ 红。
5. ★ cue `Trait Swarm Tyranid` 在 `WFSoundBank` 里取得到 —— 🧨 删掉 `TryGetCue` 那一段 ⇒ 红。
6. ★ **静态接线普查**：`BattleDriver.cs` 里 `case EvtKind.Trigger:` 那一行**真的**含 `PlaySwarmMergeFeel(`
   —— 🧨 把那句删掉（或挪到别的事件分支）⇒ 红。（②③④ 是直调，覆盖不到「事件来不来」这一跳 ⇒ 必须静态查。）
   ⚠️ 这一条**与「场上有没有视图」无关**，放在两支之外，所以永远跑得到。

**如实标注的偏离（4 条，都写进代码注释）**
1. **落点 = 存活那格**：原版实例化在**被并掉那张**的 pos/rot / scale ⇒ 我们是**相邻一格的近似**。
   要精确得让引擎把被并掉那张的位姿带出来（`RuleEngine/**`，黑名单）。
2. **混音组号**：原版 `MixerType` 实参没逐值解，`WFSoundPlayer` 无此概念（统一 `FxGroup`）。
3. **3D 空间曲线**：未与原版对照（默认衰减）。
4. 原版 `ToggleBody3D(false)`（合并期间藏卡 3D 本体）与材质交接 `ConfigureMaterial` 需要目标卡 UI 引用 ⇒ **没做**。

---

## 4 · 验证

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_g1 bash d:/4/Unity/工具/typecheck.sh` | 最后一次 **运行时 0 / 编辑器 0** |
| 中途一次报错 | `Assets/RuleEngine/Core/ActionPump.cs(818,13) CS0103 EffectResolver` —— **不是我改的文件**（引擎线 `G2` 当时正在写 `EffectResolver.EmitWhileInPlayPass`）；等 45 s 重跑即 0 ⇒ **未碰** |
| Unity 批处理 / 自检 | ⛔ **未跑**（红线）。改动面 = `Battle/BattleDriver.cs` · `Battle/MultiCardDisplay.cs` · `Editor/BattleScene.cs`（只动 `Battle` 宿主）⇒ 同步点只需 **`BattleScene.Run`** |
| 行尾 | `git diff --numstat`：`BattleDriver.cs 561/43` · `MultiCardDisplay.cs 278/45` · `BattleScene.cs 951/44`（含前序未提交改动）。二进制核过：`MultiCardDisplay.cs` **本来就是 LF**（HEAD 也是 0 个 CRLF）；另两份仍全 CRLF、**没有翻** |

**跑 `BattleScene.Run` 时要重点看的三处（我最没把握的）**
1. **B-24 那条**：`Label.WorldW` 在 uGUI 档必须读出「设计 px」。若读到 0/天文数字，第 1 条前提会红并打印实测值 ——
   那时是**我这条判据的口径错**，不是产品错（`WorldW` 的 doc 说 uGUI 档单位就是设计 px）。
2. **B-25⑤ 滚动**：它真的会改这一排卡的位置（然后复原）。若 `hitContinue(0,0,0)` 意外为真（屏心落在条上），
   `_dragActive` 会是 false、偏移不变 ⇒ 第 1 条支（>20）红。**屏心与「继续」条不重叠**（条在右下角），预计没问题。
3. **虫群⑤ 静态普查**：它按行找 `case EvtKind.Trigger:` **且**同一行含 `PlaySwarmMergeFeel(`；
   今天我写的就是一行（`case EvtKind.Trigger: PlayTraitPulse(e); PlaySwarmMergeFeel(e); break;`）。
   ⚠️ 将来若有人把这一行**换行拆开**，这条会假红（届时把判据改成「跨行匹配」或改回单行）。

---

## 5 · 没做 / 卡住（逐条写清「卡在哪、回来的条件」）

| # | 是什么 | 卡在哪 | 回来的条件 |
|---|---|---|---|
| 1 | `B-30` 原版入口（效果 470 → 动作 86 → 开窗） | `RuleEngine/**` 不认识 470，根本不发事件 | 引擎线先在 `EffectResolver` 里把 470 解析成一条带「卡列表 + 标题键」的事件 |
| 2 | `AttackGiantKiller` | 现池 `giantKiller` **0 张**（键与值都在，缺的是成因的引擎建模） | 引擎线按 `IsValidAttackTarget.c:182-305` 补建模 + 加码 |
| 3 | `CantAttackCoward` / `CantAttackTainted` | 现池 0 张 **且官方 I2 表里没有这两个键**（原版那一档本来就是空文本） | 需要用户拍板「要不要为一个原版不显示的原因造一句话」 |
| 4 | `MuteEnemyChat` | 我们**没有对手聊天**这条链（`SettingsPanel.SetMuteChat` 自陈） | 先做「对手聊天消息」，再按 `DisplayWarlordChatMessage.c` 的「>4 条未静音 ⇒ 弹 SmallTip」接 |
| 5 | `B-13` 的 `--`（取消已成形动作） | 我们动作**当场结算**，没有「排队 → 取消」两段 | 引擎若引入可取消动作队列 |
| 6 | 虫群合并件落点精确化 | 引擎只给 `hostSlot` | 引擎把「被并掉那张的世界位姿 / localScale」带进事件 |
| 7 | 虫群合并的 3D 音混音组 / 空间曲线 | 原版 `MixerType` 实参未逐值解；播放器无此概念 | 解出 `MixerType` 或换一条能对照的曲线 |
| 8 | B-25 的 `m_Elasticity = 0.1` 越界回弹 | 原版那条曲线本地无逐值判据 | 找到曲线判据，或用户接受「硬夹」 |

---

## 6 · 顺手发现（不在本轮白名单 / 不影响本轮结论）

1. 🔴 **`RuleCodes.cs` 的 `Terms` doc 里有一句已过期**（`RuleCodes.cs:322-325`）：
   「`PleaseWait` / `InvalidTarget` / `HandFull` / `DamageFatigue*` / `MuteEnemyChat` —— 我们这边**没有那一档**…⇒ **不映射**」
   —— 其中 `PleaseWait` / `InvalidTarget` / `HandFull` / `DamageFatigue*` **四条今天已有产品消费点**（见 §1·2），
   只是**不走 `Terms` 表**（在调用点直接取键）。那句话会误导下一个人以为「它们没接」。
   ⚠️ 该文件在**黑名单**（`RuleEngine/**`）⇒ 没改，请主对话按铁律 5 订正。
2. ⚠️ **`BattleDriver` 里 `NotifyCantDoAction` 的静态普查（§3c②）门槛是 `>= 10`**，
   而今天实际调用点更多（`G1` 新增不了，因为虫群不走这个口）。门槛是**下限**，不会因新增而假红 —— 但**改坏一处仍会掉数**，判别力还在。
3. ⚠️ **`MultiCardDisplay` 的视口裁切是「硬边」**（`CardView` 只做硬边；`RectMask2D` 的 `softness` 我们还没有）——
   原版这一处 `m_Softness = (0,0)`（判据见 `CardView` 视口裁切那一节），**正好一致**。
4. ⚠️ **`SideMarginPx` 现在产品路径不再使用**（滚动改成整宽视口）。留着是因为旧注释/旧断言还在提它；
   真正的「两侧留白」在原版 `Content` 的 padding = 0 ⇒ **不留白是对的**。
