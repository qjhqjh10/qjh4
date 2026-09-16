# 子代理读报: 原版战斗 C# 运行时行为 (行为序列说明书)

> 子代理: csharp 行为深读 · 2026-08-27
> 输入: `D:/2/Warpforge_code/Scripts/Assembly-CSharp/*.cs`(ILSpy 空壳) + **真实行为体** = IL2CPP 原生二进制
> 用途: Godot 4.7.2 战斗重建的行为还原依据。每节 = 行为序列(触发→表现→数据流)+代码证据+Godot 落点。

## 0. 证据体系(必读——为什么旧会话读不到行为)

`Warpforge_code/Scripts/Assembly-CSharp/` 的 .cs 是 **ILSpy 反编译的引用程序集空壳**(方法体全部 `{ }`,只有字段/签名)——因为这是 **IL2CPP 构建**(Unity 只发布参考 DLL,逻辑全在原生 `GameAssembly.dll`,76,765,184 字节,metadata v31,294,942 方法)。旧会话"弧高构成在 C# 方法体不可得"即因此。

本会话完整恢复方法体:
- **Il2CppDumper v6.7.46**(支持 metadata v31)→ `Warpforge_tools/tmp/dumper/out/{dump.cs, script.json, stringliteral.json}`
- **Ghidra 12.1.3 headless**: 导入 GameAssembly.dll(+JDK21+PyGhidra 不可用→自写 Java GhidraScript:`Renamer.java` 用脚本名标注全部 264,362 方法 + 26,569 字符串标签;`Decomp.java` 按地址表反编译;`FindRefs.java` 反查引用)
- 产物: `Warpforge_tools/data/decomp_il2cpp_0827/decomp_out/`(1,812 个方法) + `decomp_out2/`(224 个协程 MoveNext)

**证据引用格式**(因无 PDB/原始行号): `脚本名$$方法名 @0xRVA`(dump.cs/script.json RVA);字段用 `类.字段 //0x偏移`;序列化参数用场景/数据 JSON(路径+字段名);硬编码常量 = 从二进制的 `.rdata` 读出的 float(方法: PE 区段映射 + struct.unpack)。

**当前 build 与旧空壳 dump 的类名差异**(踩坑预警):
- 当前类名 `EndBattleDoors`(旧空壳文件叫 `BattleEndDoors.cs`,注意)。
- `EvgToggle`/`CardTextsInHandOptions`/`TargetSelector`/`BattleStage类` 在当前 build **不存在**;`MulliganOptions`/`BattleWinner`/`AttackTypes`/`CardStateOptions`/`UIstateOptions`/`MatchType` 是**枚举**(值见 §附录)。
- 事件总线重做:`BattleEventsController`(v31)只剩血量轮询信号;广播走 `Scaffold.Core.Events.SignalBus`(ISignal),`IBattleSignal` 有 20 个信号类。

---

## 1. 对局启动序列(场景加载→换牌→首回合)

**行为序列**(RVA 均在 dump.cs BattleManager 段):

```
BattleManager.Awake → SetupSingleton(static Instance)
→ Initialize()(InitializableBehaviour 链)
→ OnInitializationComplete()
   → SetupBoardPhase()
   → StartBattleSequence  (BattleManager.<StartBattleSequence>d__331$$MoveNext @0x9C4680)
        loadingMenu.LoadingFade(false)   // BattleManager.loadingMenu //0x188
        cameraAnim.Play()                // BattleManager.cameraAnim //0x190(+400)
        tutorial/campaign → SetupStartingTroops
        premulligan scripted 可写 → 播前置剧情(ExecuteScriptedTurn)
        否则 → battleStage 检查 → 直接进入 SetupMulliganPhase / TutorialStartSequence
→ StartBattlePhase (d__334)
   → ApplyOffensiveAndDefensiveEffects (d__337)   // 环境效果先结算
   → SetupMulliganPhase (d__341 @0x9C3120):
        if battleStage != PostBattle(0x3c) → battleStage = Mulligan(0x28)
        wait globalVars(×)…
        matchData.gameEvent(+0xa8)+0x49(双方换牌标记) 为真:
           Shade.SwitchShade(shade //0x230, true, true)  // 暗幕
           PlayerHand.AddCardsToMulligan(playerHand //0xd0, playerGoesFirst //0x247,
                                         tutorialForcedPlayer, 0)
           PlayerHand.AddCardsToMulligan(enemyHand //0xd8, !goesFirst, forcedEnemy, 0)
           WaitForSeconds(startingHand × globalVars.X)   // 发牌动画时长
        else BattleManager.ClickMulliganDone()           // 无换牌模式直接继续
        …停 1 帧…
        MulliganManager.ActivateMulligan(mulliganManager //0x1e8, goesFirst)
        matchType==Replay(0xa0) → SetupReplayMulliganStart
        StartCoroutine("MulliganCountdown")
        断线检查
→ MulliganCountdown (d__347 @0x9A9E50):
     matchData.gameEventMode==PracticeOffline(0x32/50) → 直接跳过
     Play SFX soundCollection.Mulligan
     每秒循环: UIstate==mulligan(11) && !waitingWhileDisconnected:
        counter<1 → CustomDebug.Log("Calling ClickMulliganDone after countdown")
                  → MulliganManager.ProcessMulliganDone()   // 0 秒自动完成
        counter<10 → MulliganManager.SetMulliganTimer(counter)   // 只有最后 9 秒显示数字
        user 多早完成都接受(ProcessMulliganDone on click)
→ MulliganFallbackCountdown (d__348 @0x9AA0B0): WaitForSeconds(4.0f)
     若收到敌方换牌(receivedEnemyMulligan //0x29c)与 ack 均无 → 重发(每轮 RequestMulliganResend)
     超过 15 次(final >0xf) → CancelMatchWithVictory("MulliganFallbackCountdown …")
→ (双方) SendMulliganCards/ReceiveEnemyMulliganAndEnvEffect + ResendMulligan(BattleCommsManager RPC)
→ FinishMulliganFirstPhase (d__349 @0x9A9160) / FinishMulliganFinalPhase (d__351 @0x9A8A00)
   → PlayerHand.FinishMulligan(fade)(d__52, 见 §2)
→ SetupEnviromentalEffectPhase (d__352)   // 环境卡牌上场(若 PvE 有)
→ NextTurn(playerTurn) → TurnStartCardDraw(§3) → 首回合(先手方 UI 显示)
```

**关键数值**(全部来自数据资产,不是代码常量):
- **换牌张数** = `BaseCardsInHandCount = scenarioVariables.startingHand(0x1C) + heroExtra(英雄RawCardScript+0x118,若<0则忽略)`;后手再加 `secondExtraCards(0x20)`;教程可强制(forcedMulliganCards)。
- **DefaultScenario 值**(`解包整理/09_游戏数据/去重定义/MonoBehaviour/DefaultScenario.json`): startingMana=**1**, startingHand=**3**, secondExtraCards=**1**, maxMana=**10**, maxCardsInHand=**10**, questPointsForTrigger=**3**, clockTimeLimit=**60.0**, clockTimeLimitReduced=**10.0**, clockCountdownSec=**15**。Tutorial: 1000/1000/10;Editor: 150/30/15。
- 手牌最大 = `BattleManager.MaxCardsInHand()` = scenarioVariables.maxCardsInHand(10)。
- 牌库= `playerShuffledDeck/enemyShuffledDeck`(List<CardScript>),由 `CreatePlayerDeck(isPlayer)` 洗牌生成,`CheckForHeroDeckTransform` 定锚点。

**Godot 落点**: BattleManager 单例 + battleStage(Mulligan=40/MatchProper=50/PostBattle=60) + UIstate 状态机;mulligan 计时用 60s 主时钟/最后 9s 显示数字/0s 自动完成;发牌用 per-card 延迟(globalVars 字段,≈0.3s/卡)。

---

## 2. 手牌布局 CardsHorizontalLayout(唯一权威——公式全解明)

类 RVAs: GetPosition @0x8438A0 / GetPositionAndRotation @0x8437A0 / GetRotation @0x844070 / GetClosestInHandSlot @0x843690 / GetParentScaleModifier @0x843770 / GetCardsInHandNumberWhereDistanceBetweenThemMustBeReduced @0x8435A0 / get_Scale @0x5409F0 / .cctor @0x844760。

### 2.1 序列化参数(battlearena1 场景 JSON 权威)

**玩家手牌 = MonoBehaviour_5271**(旧注释已定:CardsInHand):

| 字段 | 值 | |
|---|---|---|
| m_betweenElementsSpacing | **1.45**(中心距!) | m_scale **0.73** |
| m_maxHeight | **0.7**(乘数×曲线, 非直接高度!) | m_numberOfCardsForMaxHeight **12** |
| m_maxLayoutSize / SmallScreen | **0.59 / 0.51**(视口宽占比) | m_useViewportSize **1** |
| m_verticalOffsetLookTo | **−330**(朝向下方的视线焦点) | m_useRotation **1** / m_invertRotation **0** |
| useZOrder **1** | useExtraSpaceOnSelectedCard **1** | allowSelectCardLayoutOverFlow **1** |
| m_inverted 0 | renderCamera = 棋盘相机(透视/正交皆支持) | |

**曲线**(JSON 键值, 直接照抄即可复刻):
- m_yAxisCurve(卡序归一化 x→y 乘数): `(0.0692,−3.0193)(0.0852,−1.8870)(0.5003,−0.0004)(0.8923,−1.7444)(0.9852,−3.0425)` —— **中心平、两端 −3:卡端部下垂(月牙笑弧)**。
- m_heightModifierBasedOnTotalCards: `(0,0.0024)(0.2073,0.1497)(0.3976,0.3595)(0.5989,0.5888)(1,1.0024)`。
- m_rotationModifierBasedOnTotalCards: `(0,0)(0.264,0.826)(0.4969,0.999)(1,1)`。
- maxLayoutSizeAspectRatioModifier(仅透视相机用): `(1.7,0)(2.33,0.1013)`;x=相机 aspect。

**敌方卡背阵列 = MonoBehaviour_4053**: spacing **0.6** / scale **0.54** / maxHeight **−0.78** / numMax **10** / verticalOffsetLookTo **+74.5** / useRotation=1 / **invertRotation=1** / useZOrder=1 / maxLayoutSize 0.51;yAxisCurve 5 键对称(−3.39/−2.26/0.011/−2.37/−3.39);heightMod `(0,0.0024)(0.3987,0.1813)(1,1.0024)`;rotationMod 线性 0→1。

**另一处 CardsHorizontalLayout = MonoBehaviour_4052/4881**(换牌行待确认): spacing **502.2**(UI 像素)/ scale **183.41** / maxHeight 0.44 / numMax 7 / maxLayoutSize 0.98 / useRotation=0 / yAxis 全 0 → 一排平平的 UI 单位布局。

### 2.2 GetPosition 公式(反编译 GetPosition@0x8438A0 解出,旧会话"方法体不可得"已解)

```
parent = m_elemetsParent(0x20)                    // 必须存在, 否则 LogError+null
P = parent.transform.position
if m_inverted(0x7c): i = n-1-i                    // 反向序号
scale(parentLossy = parent.lossyScale.x)
spacing = m_betweenElementsSpacing(0x48) × parentLossyScale.x   // 世界中心距

if m_useViewportSize(0x28)==0:                          // 直接复用缓存
    maxLayout = m_calculatedMaxLayoutSize(0x80) × parentLossy
else:
    if renderCamera(0x40) 不存在: maxLayout = 屏幕相关(视口宽×m_maxLayoutSize(0x2C))
    正交相机:
        maxLayout = (2 × aspect × orthographicSize) × m_maxLayoutSize(0x2C)    // ★×视口世界宽!
    透视相机:
        dist = |layoutPos − cameraPos|; tan(fov/2); aspect
        maxLayout = (maxLayoutSizeAspectRatioModifier.Evaluate(aspect) + m_maxLayoutSize)
                    × 2 × aspect × tan(fov/2) × dist
    m_calculatedMaxLayoutSize(0x80) = maxLayout

extraShift = 0
if useExtraSpaceOnSelectedCard(0x7e) && selected(≥0) && selected≠i && selected<n:
    k = GetCardsInHandNumberWhereDistanceBetweenThemMustBeReduced(spacing)   // 最小 i: i×spacing≥m_maxLayoutSize(0x2C), 有 MaxCardsInHand 上限
    t  = (n−k) / (MaxCardsInHand−k)
    e  = 静态options曲线.Evaluate(t)                    // 额外让距, 来自静态卡文本options的那条曲线
    extraShift = (i<selected) ? −e : +e                 // 左右让位

span = n × spacing
if !allowSelectCardLayoutOverFlow(0x7f): span += 2×extraShift
if maxLayout < span:                    // ★自动收紧间距
    if !allowSelectCardLayoutOverFlow: maxLayout' = maxLayout − 2×extraShift
    spacing = maxLayout' / n            // ★ 注意除以 n 不是 n−1(原版小怪癖)!
else: extraShift = 0

halfSpan = (n−1) × spacing × 0.5        // ★ 0.5 (rdata 0x1834b2bb4=0.5)
X = P.x − halfSpan + i × spacing + extraShift

if m_yAxisCurve != null:
    t = (n==1) ? 0.5f : i/(n−1)
    Y = P.y + m_yAxisCurve.Evaluate(t) × m_maxHeight(0x74)
            × m_heightModifierBasedOnTotalCards.Evaluate((n−1)/(m_numberOfCardsForMaxHeight−1))
else Y = parent.y（近似）

Z = P.z + i × Z_OFFSET_BY_POSITION_INDEX(0.001)   // useZOrder(0x7d); Z_OFFSET=0x3a83126f=0.001f (cctor)
```

**静态 JSON 上没有、只有代码知道**:
1. m_maxLayoutSize 与 m_maxHeight 都只是**乘数**:布局宽度=视口世界宽×0.59(正交)/弧差=−3.02×0.7×mod ≈ 2.1 世界单位。
2. 间距是"中心距",且**先×父级 lossyScale**;超过 maxLayout 时**自动收紧**(/n)。
3. 选中卡额外让位的曲线来自**静态 options 对象**(非场景字段),随 (n−k)/(Max−k) 缩放。
4. Z 步进 = 0.001/卡(useZOrder)。
5. 所有 X 均以 m_elemetsParent 世界坐标为**中心对称基准**。

### 2.3 GetRotation/@0x844070

```
if m_useRotation(0x60)==0 → Quaternion.identity
dir = cardPos − (layoutPos + (0, m_verticalOffsetLookTo(0x70), 0))   // LookAt 下方 −330 → 端部内倾
q   = Quaternion.LookRotation(dir, Vector3.up)
if m_verticalOffsetLookTo > 0: q = AxisAngle(axisZ?, …) × q          // 正向(敌方 +74.5)额外轴转
t   = m_rotationModifierBasedOnTotalCards.Evaluate((n−1)/(numMax−1))
q   = Quaternion.Lerp(identity, q, t)                                // 少牌=少旋转
if m_invertRotation(0x61): q = AxisAngle(…) × q                      // 敌方翻面
```

### 2.4 GetClosestInHandSlot(@0x843690)

遍历 0..cardsInHand−1 调 GetPosition(无需选中),取 `|手点击X−卡X|` 最小(初始 1,000,000f),返回槽位索引——**只比 X**。

### 2.5 PlayerHand 侧动画入口

- `MoveCardsInHandToPosition(selectedCard=-1, cardList)`(d__76)= 全体卡重新布局的协程;`PositionCardInHand(card, index, n, timeToPositionCard, selected=-1)` 逐卡;`timeToDraw`/`CurrentSizeMultiplier`(小屏 `smallScreenSizeMultiplier` 系列)。
- 拖卡空隙: `DisplayHandWithCardSpace(cardToExclude, mousePos)` / `DisplayHandMinusCard`;`StartMovingHandCard`/`HandCardReleased`;`HandCardMovedToNewIndex(card, newIndex)`(原版手牌可拖到别的槽位换序!);`HideHand`;`ShowHandSize(showY)`(cardsInHandText);`IsThereSentinel`(哨戒);`CanDrawTurnStartCard(removeEffectAfter)`。

**★Godot 落点**: 照抄 §2.2 公式(4 条 Curve 资源 + /n 收紧 + 0.001 Z + 0.5 halfSpan)+ §2.3;卡缩放 = m_scale(0.73)由别处设置(不在布局公式内);弧线曲线用 Curve 逐点复刻。

---

## 3. 回合切换(ClockManager + END TURN 按钮 + 时钟)

类 RVAs: GetTotalTime @0x622000 / SetEndTurnText @0x622600 / SetEnemyTurnUI @0x622920 / EndTurnClick @0x621F20 / EndTurn @0x621F90 / StartTimer @0x622EF0 / SetStartUi @0x622AF0 / Update @0x6236E0 / SetFillPercentage @0x621990 / StartCountdown @0x622DE0 / SetNoActionsUi @0x622960 / Pause/Unpause @0x622550/0x623660 / LogPlayerAction @0x622120。

**结构**: `Clock` GO(树: Clock [-142,953 134x86])内 `TurnBtn` [−137,955 131x80](Button)+ `TurnText` [−132,955 119x80] 'END TURN'。组件: clockButton(EverguildButton 0x28)/clockText(0x30)/turnButtonImage(0x50)/countdownAnimPrefab+parent(0x38/0x40)/timeRunningOutMaterial(0x48)/noActionsAnim(0x58)/inAnim(0x60)/inOutAnim(0x68)。

**行为序列**:
1. `StartTimer()`(我方回合计时开始)→ `SetStartUi()`: **SetEndTurnText(true)**(TurnText = loc 'Battle/HUD/EndTurn')+ 按钮 interactable=true。
2. `SetEnemyTurnUI()`: **SetEndTurnText(false)**(TurnText = loc 'Battle/HUD/EnemyTurn')+ **clockButton.interactable=false**。
3. `SetEndTurnText(isPlayer)`: 从 LocalizationManager 读 key(`Battle/HUD/EndTurn`/`Battle/HUD/EnemyTurn`)写 TurnText(Localize 组件,非硬编码)。
4. **点击链路**(EndTurnClick@0x621F20 反编译,且 FindRefs 证实**没有代码调用者**→ TurnBtn 的 Button.onClick 由 Inspector 绑定):
   `TurnBtn.onClick → ClockManager.EndTurnClick → BattleManager.EndTurnClick(timeOut=false)`:
   - UIstate==choosingResolutionCard(14) → return
   - 非我方回合/等待阻塞动作 → 播 `SoundAssetCollection.EndTurnButtonInactive` 音 + LogWarning → false
   - 否则 播 `SoundAssetCollection.EndTurnButton` 音 → (教程校验)→ CancelActionStates() → **狂暴检查 GetPendingBerzerkUnit**: 有狂暴单位→highlight(DisplayTriggerAnim trait 0x168=Berserk)+ 提示 loc 'Battle/Tips/PendingBerzerk' + TutorialPointer(画箭头到候选目标) → WARN,不结束回合
   - 通过 → `ClockManager.StopClock()` → **`BattleManager.AddEndTurnAction(isPlayer=true)`**(动作队列入 [EndTurn] 动作)→ 返回 true
   - `ClockManager.EndTurn(timeOutFlag)` = 调用 BattleManager.EndTurnClick(timeOutFlag) + `StopAllCoroutines()`(停倒计时协程)。
5. **时钟 Update(@0x6236E0)**: 由 GetTotalTime(**60.0s**;shouldCountReduced(加时后)=clockTimeLimitReduced**10.0**;matchType EventAI(80)→**240**;PracticeOffline(50)→**600**) 驱动;`SetFillPercentage(clock%)` 填环形图(`fillAmount`);剩 `clockCountdownSec`(**15s**)→ `StartCountdown()`(countdownAnimPrefab 数字 + timeRunningOutMaterial 换红);`timeToHurryUp`(置为 reduced=10)→ `ChangeMaterialHurryUp(true)`;到期 `EndTurn(timeOut=true)`。~~**超时后再超** → BattleHud.DisplayOvertime()(OvertimeUi,§加时)。~~
   > ⚠️ **2026-09-16 更正（本文件这一行有四处错，别照它做）**：
   > ① 「**超时后再超 → DisplayOvertime()**」**是错的** —— `DisplayOvertime` 全盘**只有一个调用点**（`_NextTurn:209`），超时路径走到的是 `EndTurnClick(timeOut=true)`（`ClockManager__EndCountdown.c:51`），**从不调 DisplayOvertime**。
   > ② 「`shouldCountReduced`(**加时后**)」**是错的** —— 它的条件是「**超时结束回合 + 该回合零动作 + 非 AI 对局**」（`BattleManager__EndTurnClick.c:134-155`），**与加时无关**；我们打 AI ⇒ **永不触发**。
   > ③ 「`timeToHurryUp`(置为 reduced=10)→ `ChangeMaterialHurryUp(true)`」**没证据** —— `timeToHurryUp` 只在 `ClockManager..ctor` 写成 **35.0f**，读它的是 `ClockManager__Update.c:46-54` → `BattleManager.DisplayHurryUpChatMessage()`（**发一条聊天提示**）；而 `ChangeMaterialHurryUp` **全盘无调用点**。
   > ④ `clockTimeLimitReduced` / `clockCountdownSec` 的**值**（10 / 15）已在**本地资产**里核实（`DefaultScenario.json`）。
   > **唯一出处 = `Unity/资料/加时与冲突模式_原版规格.md` + `资料/战斗规则与数值_出处.md` §二。**
6. **加时 Overtime**: BattleManager.IsOvertime(0x258)→ NextTurn 里若加时:~~`ClockManager.StartTimer`(变短时钟 10s)+~~ 每回合起手多抽 1 卡(§§TurnStartCardDraw);界面 OvertimeIndicator(OvertimeUi: icon+banner CanvasGroup fade+enteringOvertimeSound;FindRefs: BattleHud.DisplayOvertime@0x99C620 由 NextTurn 调用)。
   > ⚠️ **2026-09-16 更正**：本条的判定式补全为 `turnCounter >= matchData.GameplayData.overtimeTurn`（`Nullable<int>`，**每回合开始判一次**，`IsOvertime` 置 true 后不再判）；而「**`ClockManager.StartTimer`（变短时钟 10s）**」**是错的** —— `StartTimer` 在 `_NextTurn:430` 的 `if (yourTurn)` **常规回合起钟**分支里，与 `IsOvertime` 无数据依赖。加时**只多抽一张、不动能量**。**唯一出处 = `Unity/资料/加时与冲突模式_原版规格.md`**。
7. `LogPlayerAction(BattleAction)`: 玩家每动作记 `playerActionsTakenThisTurn=true`;无动作可做 → `SetNoActionsUi`(noActionsAnimPrefab 播放"无事可做"脉冲)→ 提示玩家结束回合。
8. Pause/Unpause(0x622550/0x623660)+PauseForAnim/UnpauseForAnim+OnApplicationPause(timestampPause 扣后台时间):**动画期间与后台时间不计时**——ShouldUpdateSecondsPassed(real vs discount)。

**静态 JSON 没有、只有代码**: ① 计时总长来自 ScenarioVariables 资产(60/10/240/600),不在代码;② "仅敌回合按钮不可点(interactable=false)" ③ 倒计时动画从 15s 起、材质变红 10s 起(clockCountdownSec/timeToHurryUp 语义);④ 无行动时播 noActionsAnim;⑤ ~~加时=10s 短钟+OT 提示+每回合多抽 1 卡~~。
> ⚠️ **2026-09-16 更正**：① 的 60/10/15 **已在本地资产核实**（`DefaultScenario.json`，见 `资料/战斗规则与数值_出处.md` §二）；③ 的「材质变红」**没证据**（见上 §5 更正③）；⑤ **作废**（加时 ≠ 10s 短钟；加时=OT 提示 + 每回合多抽 1 卡）。

**Godot 落点**: 主计时 = 60s(数据表读);`BUTTON` interactable 随回合;fill 用 `TextureProgressBar`(环形);倒计时数字 15s;红闪 10s;点击→动作队列 [EndTurn];狂暴有"不能结束回合"提示;等待层见 §3b。

### 3b. WaitText / 等待层

- 场景 GO: `WaitText (inactive) [-665,1215 1344x79]` + 全屏 `Dark Shade [-1975,-428 3964x3366]`(BackCanvas 下)。
- 组件 `YourTurnUI: bg(Image 0x20)/text(0x28)/timeToFadeStep(0x30)/timeToWaitStep(0x34)`;`Setup()` 隐藏;`DisplaySign()` 协程 = bg+text 淡入(timeToFadeStep)→停留(timeToWaitStep)→淡出→FinishAnim。
- **调用者未知(FindRefs: 只经方法指针表被调用)** → 由 UnityEvent/inspector 回调(类似 WaitText 自己的状态事件)驱动;与之并列的是 `BattleManager.mulliganWaitText(0x1F0)`(换牌等待文本,'Battle/Mulligan/WaitEnemy')+ `MulliganManager.SetWaitingForEnemy()`(Term='Battle/Mulligan/WaitEnemy'+ 显示文本对象)。
- 结论对重建很重要:**"我的回合/等待对方"大横幅 = YourTurnUI fade 序列**,触发不在常规流程代码(实现时给 WaitText 上层挂「回合切换动画」即可,内容= loc('Your turn'/…)后由 BattleHud 的 Animator/BattleHudShow 控制)。

---

## 4. 能量(ManaManager / ManaTypeHolder,"10/10" / 能量累积 VFX)

- `ManaManager`(SetMana@0x626860 / SetManaSaved@0x626830 / SetAccumulationMana@0x626700 / ToggleCurrentMana@0x626900): 当前 build 用 4 个 `ManaTypeHolder`(ManaHolder/SpiritStoneHolder/FaithHolder/QuestPointsHolder,按阵营 ToggleXxx 显示);`psychicManaText/plasmaManaText` 标 [Header("Old stuff")]=遗留。
- `ManaTypeHolder`(dump.cs 字段): text(EverguildTextMeshPro **0x20**)/useVfxOnValueChange(0x28)+vfxOnValueUp(0x30)/Down(0x38)/**manaAccumulationObj(0x40)**/**manaAccumulationVFXON(0x48)**/**manaAccumulationVFXOFF(0x50)**/**manaAccumulationImageOn(Image 0x58)**/**manaAccumulated(0x60)/manaAccumulatedUsed(0x68)** 两个 AudioCue。
- **SetMana(value, silently)**: `text.text = value.ToString()`;!silent && useTweenOnValueChange → `DoTextAnim()`(DOPunchScale: punch=0.2f, vibrato=10, elasticity=1.0, 恢复 0.3s 也可参 DOTween 默认);`TryDOVfx(target)`: useVfxOnValueChange && value≠currentValue? value>old → 播 vfxOnValueUp(0x30) else vfxOnValueDown(0x38)。
- **ToggleCurrentMana(activate, current, total)**: text = **`"{0}/{1}"`.format(current, total)** —— 这就是 '10/10' 的来源(格式串字面量,无 loc key;失活时 {0}=total)。
- **SetSavedMana(savedMana, unusedMana, totalMana, useNumberChange)**: **VFXON.SetActive(saved>0); VFXOFF.SetActive(saved<1)**(互斥);saved>0 && imageOn: useNumberChange → SetMana(unused−1, total) 显示;随后 **ToggleManaAccumulation(active, OnComplete)**。
- **ToggleManaAccumulation(option)**: Kill 现有 tween → PNG 图标(0x58 Image)缩场动画(DOTween)→ 完成后 **active? 播 manaAccumulated(0x60) : manaAccumulatedUsed(0x68)** 音效(带每帧限一次保护)。
- **SetAccumulationMana(active)**: manaAccumulationObj.SetActive(active)+ Image(0x58) 的 GO SetActive + Image alpha(切图= Off 时图像清透明)。

**"能量+1 VFX ON/OFF"触发链**: `BattleManager.UpdateManaLeftAccumulation(isPlayer)`(NextTurn 里,`turnCounter>1` 时以 `!isPlayerTurn` 调):`saved = min(object+0x40(cap), PlayerManager.GetUnusedMana(player)) → PlayerManager.SetSavedMana(player, saved, useNumberChange=1)` → ManaManager.SetManaSaved → 上面链 → **能量累积图标+VFX ON(有累积)/OFF(无)**;每回合初 `GetSavedMana>0 → RefillCustomMana(saved); SetSavedMana(0)`(下回合返还)。

**Godot 落点**: 两个独立子节点(VFXON/VFXOFF)互斥开关 + ImageOn 透明度;数字"{0}/{1}"；值变化弹跳(scale punch 0.2/10/1.0);音效 2 个 cue。

---

## 5. 牌库(DeckManager,'Cards left: X',YourTurn/NotYourTurn 图标)

- 场景: PlayerDeck [-305,850 230x230](DeckAndEnergyImage)**含 NotYourTurnImage [-118,967 24x36]**;Player Deck Size Container + **Player Deck Size Tex text:'Cards left: 2/5'**(Dummy);EnemyDeck [-318,880 200x200];各带 CardBack For Reference/Cardback Container(阴影+卡背)。
- `DeckManager`(SetupDeck@0x624550 / SetYourTurn@0x624500 / DisplayDeckSize@0x6241B0 / GetReferencePosition@0x6243F0 / GetReferenceScale@0x624430 / ToggleCardbackPreview@0x624690 / AddCardNotDrawnToDeck@0x624140):
  - 字段: cardBackReference(0x20,Transform)**yourTurnIndicator(0x28)**/**enemyTurnIndicator(0x30)**/deckSizeContainer(0x38)/deckSizeText(0x40)/isPlayer(0x48)/cardbackImage(0x50)/cardbackShadow(0x58)/cardbackContainer(0x60)。
  - **SetupDeck()**: cardBackReference.yourTurnIndicator.enemyTurnIndicator 三个 GO 全部 SetActive(false)(初始隐藏!)→ BattleManager.GetCardback(isPlayer) 拿 (卡背,阴影) → cardbackImage/cardbackShadow.sprite;cardbackContainer 隐藏。★"敌方牌库静态在屏外→运行时移入":实际 = 场景摆放即屏外/或经 `cardBackReference` 悬挂点微调 + CardScript 排序(上桌位)。`GetReferencePosition/Scale` = cardBackReference.transform 的 pos/lossyScale(供外部对齐)。
  - **DisplayDeckSize(showY, cardsLeft)**: `BattleManager.CanDisplayDeckSize()` 门禁 → showY:deckSizeContainer SetActive(true)(+敌方另 cardbackContainer true)→ deckSizeText.text = **`loc('Battle/HUD/CardsLeft') + ": " + cardsLeft`**(**'Cards left: X',没有 /Y!**;树中 '2/5' 是编辑器示例文本);showY=false → 双双隐藏。
  - **SetYourTurn(isPlayerTurn)** = yourTurnIndicator.SetActive(isPlayerTurn)+ enemyTurnIndicator.SetActive(!isPlayerTurn)。调用点: NextTurn(d__395)对 playerDeck(0xf0)与 enemyDeck(0x108)各调一次(反向)。★图标的"回合切换"在牌库组件上,不在 HUD!
  - AddCardNotDrawnToDeck(card) = 不抽牌直接放回牌库的动画协程(0x624140)。
- UpdateDeckDisplay(BattleManager)= 牌库 UI 刷新入口;BattleManager.HideDeckAtStart(isPlayer)(开局隐藏段)+ CreatePlayerDeck 调 SetupDeck(FindRefs 证实)。
- 'Cards left: X' 另属: `PlayerHand.cardsInHandText`(GO **CardsInHandText (inactive)** [-1,1027 2x1],树 'Cards left: XX' 示例)——*手牌区剩*的显示文本(与牌库文本同格式,位置在手牌行)。

**Godot 落点**: 牌库卡背=TextureRect+阴影;指示图标两个并排互切;文本=loc('Battle/HUD/CardsLeft')+': '+N(记得无 '/Y');初始三个 GO 全隐。

---

## 6. 出牌流程(手牌点击→部署→落地刷新)

**UIstate 流转**(BattleManager.UIstate //0x290): normalBattle(1)→movingHandCard(2)→playingMinion(3)├ playingTargetedSpell(4)/playingNonTargetSpell(5)└ minionAttacking(6)/choosingUnitTarget(9)/targetedActiveAbility(10) /mulligan(11)/mulliganFinished(12)/choosingAbilityTarget(13)/choosingResolutionCard(14)/postBattle(8)。

**链路**(均反编译或见于方法表):
```
手牌卡触摸 (BasicCardUI.SetupCollider+回调, ColliderBigSize, DisableCollider; OnTouchDown/UpAsButton)
→ BattleManager 判定 (UIstate/IsPlayingCardState) → 建 BattleAction(PlayCardFromHand, cardPos)
→ actionQueue(0x350) 队列 → ResolveAction → ResolvePlayCardFromHand (d__447, 超长)
    ├ 校验/resolve: AllowResolveAttack 类门禁, NeedToChooseMinionTarget(连锁目标)
    ├ 费用: ManaManager(SetMana 减) 由规则引擎动作
    ├ 手牌状态: CardScript.cardState: inHand(1)→inHandMoving(9)→inHandPlaying(10)→inPlay(2)
    │    (卡 State 机: CardStateOptions 18 态, 含 inDeckDrawing/drawing/inHandShowing/inPlayAttacking/waitingToBePlayed/waitingToDie/inCemetery/removedFromGame/inPlayTransforming/inMulliganSelected/inMulliganDiscarded…)
    ├ UI: PlayerHand.RemoveCardFromHand → BattleCardUI.ChangeCardToMinion(true)(
    │     用 HAND_TO_BOARD_ANIMATION_CLIP_NAME 动画) → CardHandToBoardAnimationFinished
    ├ 落子: SetUnitOrderInPlay / GetUnitSizeInPlay(cardType, isPlayer) / 排序
    └ 广播: BroadcastCardPlayed(manager, acting, target) / BroadcastUnitLanded / BroadcastUnitSummoned(阵营收)
数值刷新: BasicCardUI.SetRawCardData → CardTextCountersController ×5
    (HealthCostMeleeRangeArmour, Counter 控件); BattleCardUI.DamageReceived(dmg);
    CardDamageCounterController(damageCounterController //0x1A8)
```
**部署区/battle 拖拽**: 用手牌拖动(BasicCardUI 的 UIGenericEventCatcher + BattleManager.pointerCursorStart(0x320)/RaycastHit2D targetOnTouchUp(0x340)/RaycastHit[] boardTargetOnTouchUp(0x348)/cardTracked(0x330));落点合法性= ResolvePlayCardFromHand 内(原版无独立 RuleCore 类!规则校验分布在各 Resolve***)。

**Godot 落点**: Godot 用 Control 拖拽 + viewport 射线;状态锁=UIstate;事件源=动作队列(§14);卡状态机=CardStateOptions 18 态(建议照搬枚举)。

---

## 7. 攻击选择器(Drag Attack Selector — 三钮+拖拽+准星)

**场景层**(树, FrontCanvas/Safe area): `Drag Attack Selector [-311,1019 622x122]`:
```
- Select Attack Background (inactive) [-390,1037 780x285]   script script script (= SelectAttackBackgroundController)
- Select Melee Button    [-370,1082 118x119] + Highlight [-399,1053 176x177]
- Select Active Skill Button [-370,1082 118x119] + Highlight + ValueText [-356,1142 92x0] text:'0'
- Select Range Button    [-370,1082 118x119] + Highlight
```
组件: `UnitOnBoardAttackTypeSelector`(buttonsController //0x20 + targetReticleController //0x28 + OnPointerEnterButton) = BattleManager.attackTypeSelector(0x1A0);battle GO `attackTargetingReticle(0x150)`。

**行为**:
- **UnitOnBoardAttackTypeSelector.Toggle(option, card) / ToggleJustOne(option, type, card)**: 显示选择器(attach 到单位);只一种攻击 → 单钮模式。
- **AttackButtonClick(attackType)**(@0x64A450 反编译): guards(无卡/同类型/tutorial 禁止/脚本教学等待)→ **CardScript.ChangeAttackType(card, type, notify)** → **AttackTypesButtonsController.HighlightSelectedAttackTypeButtons(type)**(按钮高亮)→ **TargetReticleController.SetAttackType(type)**(准星换色/形状)→ **BattleManager.StopShowingPotentialTargets + ShowPotentialTargets(card, mode: Active→2 else 0)**(格子高亮可选目标)。
- **拖拽切换**: AttackTypesButtonsController.Update/MoveButtons: `accumulatedDrag(0x50)` 累计触点位移,≥`accumulatedDragForMinDistance(0x30)` → 切到下一攻击类型 + DragOutOfButton(按钮组外松手→回退)。三钮用 `HorizontalOrVerticalLayoutGroup layoutGroup(0x28)`(**间距在 prefab=旧实现 37px**)。
- **SelectAttackBackgroundController**: 背景板 `ChangeSizeByElements(n)`/`ChangeSizeByInput(归一化拖距)` 缩放(随按钮个数变宽)。
- **TargetReticleController**(crosshair(0x20)+crosshairSprite(0x28)+CrosshairLineEffect(0x30)+colorChangeSpeed(0x38)+floorReference(0x40)+scaleOnChangeModifier(0x48)+colorPresets(0x50)):
  - 三个 Plane: floorPlane/playerMinionPlane/enemyMinionPlane(0x70/0x80/0x90)→ `RaycastToWorld(canvasPos, isPlayer)` 决定拖拽射线落在哪个平面。
  - `PrepareMovement(startBoardPos, attackType)` 起点;`UpdateTrail(origin,target)` 画攻击线(CrosshairLineEffect Material 渐变);`SetAttackType(type)` → colorPresets 换色 + sprite scale(scaleOnChangeModifier);`ToggleCrosshair(on, instant)` 显隐(ChangeAlpha fade);`AutoMovingCrosshair`(自动移动至最近合法目标)。
- **AttackTypes 枚举**: None=0/Melee=1/Ranged=2/Spell=3/Active=4。
- **确认攻击** = 拖到目标/点目标 → BattleManager.ResolveAttack(BattleAction Attack, acting, target, attackType)(d__438 @0x9ACF10,超长: ResolveAttackDamage(dealer, receiver, dealtBy, type)→ BroadcastUnitDamaged → 死亡 ResolveMinionDeath → Requiem/Mob/魂石等 trigger 链;反击/相邻(ShouldTriggerAdjacentDamage)/踩踏(ShouldTriggerStompDamage)/miss/盾等)。

**Godot 落点**: 三钮 HorizontalLayout(37px)+拖动位移阈值切换+背景缩放+准星(世界平面 raycast 3 平面)+colorPresets 每 attackType 一套色 + 攻击线 mesh。

---

## 8. 技能描述 ActiveSkillDisplay(ActiveSkillDesc)

**场景 GO**: `ActiveSkillDesc (inactive)`(FrontCanvas):
```
HideAbilityButton; AbilityContainer { NameText 'Fire Arrow'; CostPanel {CostText '10'; CostIcon}; DescText 'Deal 1 damage…' }
Lights { LightActing; LightAvailable; LightPressed } ; TargetsAvailableText '0 available'
```
组件字段: hideSkillBtn(0x20)/cardFrame(0x28)/bgLight(0x30)/actingLight(0x38)/pressingLight(0x40)/nameText(0x48)/costText(0x50)/descText(0x58)/manaIcon(0x60)/costSprite(0x68)/fadeTime(0x70)/xOffsetOffScreen(0x74)/targetsAvailableText(0x78)/parentCard(0x80)+6 个 Tweener(0x88-0xb8)/waitingToResolve(0xC8)/startPos(0xCC)/beingDisplayedFlag(0xD8)。

**行为**: `SetupActiveSkill(parentCard)`: 无 activeAbility 卡 → LogError;否则 parentCard+内容(技能名/费用/描述/图标/`0 available` 目标数文本)。`FadeIn()`: hideSkillBtn.SetActive(true)+Kill 全部 tweener+各元素 alpha 0→1(fadeTime)+ moveTween(→`GetOpenPos()`=startPos 世界位)。`GetOffScreenPos()` = xOffsetOffScreen 移出屏。`ClickDown/Drag/ClickUp()`(OnTouchDown/Drag/UpAsButton 回调): 拖→目标→`manager` 触发 ResolvePlayActiveAbility(动作排队, waitingToResolve 锁);`DisableUsableLight`/`ShowActingLight`/`HideActingLight`(Lights 组);`FadeOut/StopDisplayingSkillFromManager/FinishActing`(结束关闭)。ShowPotentialTargets Active=mode2 同链路。

**Godot 落点**: 面板(名字/费用/描述/图标/可用目标数)+6 个平行淡入 Tween+拖拽发技能+3 个状态灯(可用/按下/施放中)。

---

## 9. 聊天(ChatButton→快捷消息→气泡+语音)

- **组件**: `VoiceLinesController`(初始化 0x662EA0): **CHAT_INTERACTABLE_COOLDOWN=4**(秒)/**MAX_AUDIO_DURATION=2**(秒)常量;chatButton(0x38)/unitsVoiceLinesPanel(0x48)/communicationsManager(0x50)/voiceLinesPopupSelector(0x58);`ChatMuted`;`IsAnyVoiceLinePlaying`;`chatsReceivedInTurn`(每回合限? ResetChatsReceived() 重置)。
- **快捷消息**: `ChatMessage` 枚举= 11 条: Intro=0/MirrorMatch=1/Bored=2/ICantDoThat=3/Concede=4/Greet=5/Threat=6/WellPlayed=7/Taunt=8/Sorry=9/Oops=10。`ChatPopupButton : EverguildButton`(buttonText/四态 sprites/chatType + **OnPressed Action<ChatMessage>** 事件)。
- **点击链路**: 聊天按钮(BattleManager.chatButton 0x140)→ `VoiceLinesController.ClickChat()` → 打开 VoiceLinesPopupSelector(11 条单元)→ 选中 → `DisplayLocalChatMessage(msg, ignoreUiState)`(本地) / `BattleCommsManager.SendChatMessage(msgIndex)` → RPC `PUNReceiveChatMessage` → 对端 `DisplayWarlordChatMessage(msg, false)`。
- **显示**: `DisplayChatBox(chatMsgText, talkingUnit, audioClip)` → 单位气泡(场景 **Unit Chat**: PlayerChatDisplay/EnemyChatDisplay [13,643 649x237]: CardImage+Mask+Background+ChatText 'Here goes a chat line';PlayerChatAudioSource/RadioChatAudioSource/wave);`DisplaySummonMessage`(召唤台词);`ShowRadioMessage(tipData, sprite, audio, cardType)`(无线电消息);`ShowHeroesIntroMessage()`(英雄开场, d__34);语音播放 ≤2s,按钮 4s 冷却;`MaxAudioDuration` 截断。
- 另有 `BattleManager.DisplayBigChat(chatMsgText, talkingUnit/Sprite)`(单位喊话)。静音:`ToggleMute`/ChatMuted。
- 大字提示: `BattleTipController.ShowHeadsUpMessage / NotifyCantDoAction`(如 PendingBerzerk 提示)。

**Godot 落点**: 11 条快捷消息面板(4s 冷却)+气泡(头像+文本+wave)+语音(2s 上限)+静音开关。

---

## 10. 卡牌展示窗(CardDisplayWindow / UIMultiCardDisplay)

- `CardDisplayWindow : GameWindow`(ShowCard@0x7FBA10 / Open/Close/ToFocus/ToBackground): **`ShowCard(card, tier, showOptions, onChangeCard, warlordHealthModifier)`**;内容= cardUIs(BasicCardUI[])+cardEffectSlots+cardEffectsGroup+loreObjectText/BG+backgroundButton+displayAnimation(交换动画 relatedCardSwapTime + soundOnCardSwap)+voiceOverButton+showCardTextButton+informationPanel;`CurrentCard` 单例追踪;换卡时排序引用 referenceShortingOrders。触发: BasicCardUI.CardClicked(反编译: get CardDisplayWindow.Instance + rawCardData + tier → ShowCard;FindRefs 已证),或 Touch 长按(`allowOpenCardDisplayOnTouch`)。
- 场景 GO: `Card Display Window`(Menu Dark Background 4575x2572;Card Display 752x868;LowerSection FlavourTextBG/LoreText;Voice Over Button 89x89)。
- `UIMultiCardDisplay : GameWindow`(Initialize@0x842450 = cards, headerTitle): referenceCard+cardsContainer+mask(RectMask2D)+headerText+closeButton;`ToggleMask(option)`;**BattleHud.DisplayMultiCardView(cards, header)** 入口(环境卡/战绩卡组);场景 GO `Generic Multi Card Display Combat`(Header Text/Viewport/Content/CardUI Reference)+ **BattleContinueButton** ('Continue' 578x64 + CircleButton 80x80, [−857,1492 833x50] 右下)。

**Godot 落点**: 全屏窗=GameWindow 基类(Open/Close),卡显示=BasicCardUI 复用;多卡=横向列表+遮罩+右下 Continue。

---

## 11. 结算(EndBattleDoors / 骷髅 / 奖励)

- **触发链**: DeadHero(isPlayerDying, battleResult) → GetWinnerAfterBattleEnd(isPlayerDying, result)(BattleWinner Player=10/Enemy=20/Draw=30)→ SaveMatchWinner → ProcessMatchResult → `ShowFinishScreen(skipDelay)`(d__362)→ **BasicBattleEndSequence**(d__391: 结束特效/单位清理)→ `DestroyUnitsAfterBattleEnd`(d__392)→ **`CloseBattleDoors(matchResultData)`(d__393)** → StopBattleAfterBattleEnd;摧毁布局/隐藏手牌。
- **EndBattleDoors**(SetupDoor@0x624910 / ShowRewards@0x624E50 / HideRewardsObjects@0x624720):
  - 字段: ratingText/ratingIcon/**sealIconSprite**/armyRatingIcon/**skullsObjs[]**/skullHolderObj/rewardsHolderObj/EndBattleRewardHolder rewardHolderPrefab/ratingObj/ratingLoseMaterial/allRewardsGroup/**victoryVideo/defeatVideo/drawVideo**+VideoPlayer+videoImage/**victoryCue/defeatAudioCue/drawAudioCue**/commonAnimationScaleTime。
  - **SetupDoor(gameMode, battleWinner, matchType, grantedResult, skullsObtained, playerWarlord)**: 门开动画→胜/败/平三选(视频+音效)→**评级**(ratingText/ratingIcon/seal)→**骷髅**: skullsObtained 指定点亮 skullsObjs[](BattleScoreManager.MatchSkulls 在局内也有 'x3' 计数);**ShowRewards** 实例化 rewardHolderPrefab×N(奖励格);败北→ratingLoseMaterial 灰。
  - **骷髅规则**: `BroadcastEarnSkull(isPlayer)`(每杀一个敌方单位/英雄 = 1 骷髅, BattleManagerSupport.BroadcastEarnedSkull(manager, earnedWarlord));BattleScorer(统计)。
- **BattleEndSignal : IBattleSignal**(结算信号)。

**Godot 落点**: 结算门 = 全屏覆盖(视频用播放器/降级图)+三态音视频+骷髅数组逐个亮+奖励格实例化;骷髅数=对方单位击杀计数。

---

## 12. 错误横幅(UIMessageController / UIErrorMessageItem)

- `UIMessageController : MonoBehaviour`(**NUMBER_OF_MESSAGES=5**) `Instance`;**ShowError(messageToShow, localize)** → 循环 5 条 messageItems(UIErrorMessageItem: text+canvasGroup+background;appearScaleFromMultiplier/appearAnimationTime/timeToStartFading/timeToFade 序列化值;Show(text,color,localize))弹缩放→停→淡出。
- 全局弹错: `ErrorPopupWindowController.ShowErrorMessage(text, localize, actionAfterError)` / ShowPlayfabError(全流程错误入口)。
- 战斗内来源: 网络断线(ShowDisconnectionPopup/AttemptReconnect/FailedToReconnectAfterDisconnect/ForfeitDisconnectedEnemy)、desync(PUNReceiveEnemyDesync)、错误房间(SendWrongRoomMessage)等。

**Godot 落点**: 5 条复用的消息队列+出现/停留/淡出三段(参数挂场景);重错走 Popup。

---

## 13. 按钮通用行为(EverguildButton)

`EverguildButton : UnityEngine.UI.Button, ICancelHandler`:
- **事件**(代码委托): OnPointerEnterTriggered/Exit/Down/Up + OnInteractable/OnNoInteractable(Action 属性)+ **Click**/PointerDownEvent/PointerUpEvent/CancelEvent(SourceDelegate)+PointerPressEvent(PressDelegate)+PointerOverEvent(OverDelegate)+StateChangeEvent(StateDelegate);**RemoveClickListeners()** 清空。
- **绑定方式双轨**: ① 常规 OnClick 走 Unity Button.onClick(inspector 绑定,如 TurnBtn→ClockManager.EndTurnClick,FindRefs 证);② 组件代码级 AddListener(MulliganFrame.SetupMulligan 对 changeCardButton.onClick `UnityEvent.AddListener` 显式绑定!);③ 本类自身的事件(event +=)。
- **默认音效**: `m_onClickSound(0x170)`+`useDefaultAudioClipIfNull(0x178)`:`PlayButtonSound()`(OnPointerClick 内) = 有自配 AudioCue 且 HasAudios→播它;**否则用全局默认 UI 点击音**(SoundManager+0x30)。
- **颜色过渡**: 覆盖 **DoStateTransition(state, instant)**: 先 base 后自定义——`customColorTintTransitions(0x179)` 时按 state 选色(normal/pressed/highlighted/selected/disabled 五态色,直接读 Unity Button 的 colorBlock 字段 0x54..0xa4)→ targetGraphic CrossFade。
- **灰禁用**: `colorTintGreyOnDisable(0x17A)`+`colorTintGreyOnDisableAlpha(0x17C)`;`SwitchMaterial(interactable)` = **EverguildButtonHelper.DoMaterialRefresh(graphicsToChangeMaterial, interactable)** 切 **"Everguild/UI/Greyscale" 材质**(shader 灰阶,非颜色调制!);`EverguildButtonHelper.DisabledMaterial` 静态缓存该材质;`graphicsToAvoidSetMaterials(0x180)` 豁免列表。
- **softDisabled**(0x198): SoftDisable(bool) 视觉可用但不可交互;`changeInteractableStateOfChildButtons(0x188)` 联控子按钮(UpdateChildrenComponentsInteractable)。
- `Text`(0x1A0) TMP 属性直接改写按钮文字(但更常用 Localize 组件)。

**Godot 落点**: 包装 `Btn`(pressed/hover 信号+默认 click 音+五态颜色);"灰显"用 **CanvasItemMaterial(grayscale)** 或 modulate+alpha 组合(原版是材质,纯 modulate 会偏色——建议写个 grayscale shader);softDisable=禁用输入兼护视觉态。

---

## 14. 事件/信号体系(原版"事件文本"的真相)

- **v31 现状**: `BattleEventsController.Initialize()` 只订阅 EnemyHealthChangeSignal/PlayerHealthChangeSignal;Update→CheckHealth 比较 lastEnemyHealth/lastPlayerHealth(**引擎事件广播点,非 UI 回调中心**)。
- **总线**: `Scaffold.Core.Events.SignalBus`(Register<T>/Raise<T>/UnregisterAll) + `Signal`(引用计数);`IBattleSignal : ISignal`。**IBattleSignal 实现(20)**: AttackSignal(t?)/DamageSignal(unit,target,type?)/BattleEndSignal/VictorySignal/CardPlayedSignal/EnergySpentSignal/HealUnitSignal/MissionCompleteSignal/TroopKilledSignal/UnitReanimatedSignal/UnitSwarmSignal/TriggerAbilitySignal/AddCardEffectSignal/AddSabotageCardSignal/CreateSecretSignal/CollectFaithSignal/CollectSpiritStoneSignal/+HealthChangeSignal 2 子类。
- **广播发起**: `BattleManagerSupport.Broadcast*(48 个静态)`: BroadcastTurnSetup/TurnStart/TurnStarted/**TurnEnd**/**TurnBeforeEnd**(isPlayerTurn)/CardPlayed/CardDrawn/CardReturnedHand/CardReturnedDeck/UnitDamaged(manager, damaged, DamageInfo, dealtByAttacker)/DeadUnit/UnitAttacked(acting,target,**AttackTypes**)/UnitSummoned/UnitLanded/EarnedSkull(isPlayer)/RemoveFromGame/WhileInPlay(unitJustSummoned, excl×2)/…阵营特色(BroadcastUnitSwarm/Synapse/Requiem/Mob/Codex/Pray/Duty/Ferocity/Regiment/LoseStealth/ObtainedMark/HuntMark/Markerlight/CollectedSpiritStone/Faith/QuestPoints/SecretCreated/SabotageCreated/UsedSacrifice/Reanimated/Jammed/Stunned/Poisoned/Healed/…)。
- **★原版没有"VFX 文本日志流"**——battle.gd 的 `_fx_from_event` 事件文本是项目自造;原版对应物 = **信号类型+参数**驱动各 UI 订阅者(播 VFX/音效/数字/高亮)。战斗日志(历史行动)则是 `BattleLogPopup/BattleLogMatch/BattleLogItem/BattleLogPlayerData`(按 BattleAction 文本化)。

**Godot 落点**: SignalBus<T> 单例 + 上述信号名;每个 VFX/音效 = 对信号的订阅(signal→anim);事件文本项若要保留,只作日志用途而非触发源。

---

## 15. 旧经验复核表(旧 battle.gd vs 本次权威)

| 旧值/旧注释 | 权威值(反编译+JSON) | 判定 |
|---|---|---|
| 手牌 spacing 1.45 世界单位;间距/显示宽=0.949 → HAND_SPACING_F=0.949 | 5271=1.45;卡宽2.0927×0.73=1.527;1.45/1.527=0.949 ✓ | 一致 |
| m_maxHeight=0.7→"≈55px 最大弧高" HAND_ARC_H_F=0.194×卡高 | **弧高= yAxisCurve(−3.02)×0.7×heightMod(n)≈2.1 单位≈165px 端点下垂**;0.194 只是把 0.7 折算成卡高比例的近似;"55px"用错了 | **修正**: 端点下垂≈2.1单位, 曲线中心≈0(平);旧实现的"5 键形状"对(两端−3.04),但把 yAxisCurve 当"形状直用"而少乘了 0.7×heightMod?（旧图 HAND_ARC_H×曲线: 0.194×285×1 ≈ 55px——旧曲线单位=px,相当于 0.7×2.7? 需按新公式重写) |
| HAND_ARC_MAX_N=12 / 4053 −0.78/0.6/0.54/10 / invertRotation=1 / FOCUS_DY=330 / useZOrder | 全部与 JSON 一致 | 一致 |
| (旧注释无) useExtraSpaceOnSelectedCard、allowSelectCardLayoutOverFlow、maxLayoutSize(0.59/0.51)、aspect mod(1.7~2.33)、自动收紧间距 /n、Z 步进 0.001、0.5 halfSpan | 代码确认, 旧实现 5 项遗漏 | **5 项新增缺失** |
| 时钟 _clock_left=90.0 | **DefaultScenario.clockTimeLimit=60.0**;reduced=10;countdown=15;EventAI=240;PracticeOffline=600;"Overtime 不模拟"与 C#(IsOvertime+OvertimeUi+OT 多抽1卡)矛盾 | **修正 90→60**, 加时需实现 |
| 'Cards left: X/Y' | actual= `loc('Battle/HUD/CardsLeft') + ': ' + X`(**无 /Y**) | 修正: 无 /Y |
| 攻击选择器 Melee/Ranged/Skill 三钮 37px | AttackTypes={**Melee,Ranged,Spell,Active**};三钮 118x119/Highlight 176x177;间距 37px(prefab);支持**拖动切换**;Active 钮带 ValueText | 结构一致, 名称 Skill→Spell/Active, 增加"拖动切换+Active 模式" |
| 能量 VFX ON/OFF (战斗+1) | SetSavedMana: VFXON.SetActive(saved>0)/VFXOFF.SetActive(saved<1)+ImageOn+2 音效;UpdateManaLeftAccumulation 回合切换时 min(cap, unused) 存下 | 确认+细化 |
| NotYourTurn/YourTurn 图标 | = DeckManager.yourTurnIndicator/enemyTurnIndicator;SetYourTurn 双 GO 互切;初始 SetupDeck 全隐 | 归属=牌库组件 |
| "原版视频/坐标直读" | 本报告以 2D 层树+JSON 链式为准(如 PlayerDrawnPos [1350,1030]) | — |
| 旧"能源/灵能双文本 10/10" | ManaHolder.text = **"{0}/{1}".format(current,total)**;psychic/plasma=旧字段 | 修正格式串 |
| 换牌: "3 卡+后手1" | startingHand=3(+英雄附加+后手 secondExtra=1+教程强制;选项= normal/secondTurn/extraWarlord/whispersOfChaos) | 确认+枚举化 |
| 旧"事件文本/日志流" | 原版=SignalBus 类型信号;文本流只是 BattleLog 事后日志 | 架构更正 |

---

## 16. Godot 实现最难还原的 5 项(定案)

1. **手牌弧线公式全套**: 4 条曲线相乘(轴乘数 −3.02/高度 mod/旋转 mod/aspect mod)+ **spacing 超限自动收紧(/n 小怪癖)** + 中心距(≠边距)+ 选中卡溢出式让位(extra curve 来自静态 options) + Z=index×0.001 + 反序 `n-1-i`。建议: 原样照抄公式, 写 4 条 Curve 资源, 做单测(给定 n/i 对比参照管线运行时 tsv 截图: `Unity参照管线_0825/data/battle_hand_*.tsv`)。
2. **IL2CPP 协程动作队列 + UIstate 锁**: 原版所有行动(出/攻/伤/亡/回合/换牌/结算)经 `actionQueue/instantQueue` Resolve*** 协程(每步 await 队列排空),UI 全程一把锁(UIstate + actionInQueue 轮询 yield)。Godot 需要"动作队列 + await 排空"的解析器(而不是信号即触,旧 battle.gd 的信号链精度不够);强烈建议: 队列驱动 UI 复现"解析期锁输入"节奏(原版 EndTurnClick 会在 resolving 时弹禁用音)。
3. **拖拽攻击交互链**: 三钮+按压拖动切换(位移阈值)+CrosshairLineEffect 光线+**三平面射线**选择目标+ShowPotentialTargets 高亮+Active 模式(mode 2)+colorPresets。其中"从单位拖着手不放,准星在棋盘平面移动,松手=攻击"是纯 Unity 事件流(OnTouch 回调),Godot 需在 _input/_gui_input 重造同语义(还分移动端/桌面)。
4. **本地化 Key 体系**: END TURN/Enemy Turn/Replay/Cards left:/Mulligan(Instructions/GoFirst/secondTurn/WaitEnemy/Replace/Undo)/PendingBerzerk 全是 loc key(Localize 组件);本地化表在游戏数据(12 语言)。Godot 无本地化→需建 key 表+组件加载;**禁止把 'END TURN' 写死**。
5. **EverguildButton 材质灰阶+默认音频+五态色**: 灰=材质切换(Everguild/UI/Greyscale),Godot 需 grayscale shader/material;默认 UI 点击音来自全局 SoundManager;五态颜色来自 Button.colorBlock+自定义 CrossFade;softDisabled 与子按钮联控。

---

## 附 A: 关键常量与值(白盒直读, 报告引用锚)

| 常量 | 值 | 出处 |
|---|---|---|
| Z_OFFSET_BY_POSITION_INDEX | **0.001f** (0x3a83126f) | CardsHorizontalLayout..cctor @0x844760 |
| 0.5f (halfSpan) | 0x1834b2bb4 | GetPosition |
| 1,000,000f (初距) | 0x1834b3354 | GetClosestInHandSlot |
| abs 掩码 | 0x7fffffff | GetClosestInHandSlot |
| π/180 | 0.017453292f (0x3c8efa35) | 透视 FOV 换算 |
| FinishMulligan 逐卡淡出延迟 | **0.3f** (0x1834b2dc8) | d__52 |
| DoTextAnim punch=0.2f/vibrato=10/elasticity=1.0 | 0x1834b2bb0/0x1834b2e00/0x1834b2bb8 | ManaTypeHolder |
| MulliganFallback 首等 | **4.0f** | d__348 |
| NextTurn 教程提示停留 | 1.5f (0x1834b3090) | d__395 |
| 默认局 (DefaultScenario.json) | 起始 1 费/3 张/后手+1/上限 10/钟 60s/减 10s/倒数 15s | 09_游戏数据 |

## 附 B: 枚举速查(当前 build)

- **MulliganOptions**: normal=0/secondTurn=10/extraWarlord=20/whispersOfChaos=30(GetMulliganOption @0x6399B0 判定顺序: whispers→extraWarlord→secondTurn→normal)
- **UIstateOptions**: inactive=0/normalBattle=1/movingHandCard=2/playingMinion=3/playingTargetedSpell=4/playingNonTargetSpell=5/minionAttacking=6/finishBattleSequence=7/postBattle=8/choosingUnitTarget=9/targetedActiveAbility=10/mulligan=11/mulliganFinished=12/choosingAbilityTarget=13/choosingResolutionCard=14
- **BattleStage**: Undefined=0/Mulligan=40/MatchProper=50/PostBattle=60
- **BattleWinner**: Undefined=0/Player=10/Enemy=20/Draw=30
- **Attacks**: None=0/Melee=1/Ranged=2/Spell=3/Active=4
- **CardStateOptions**: inDeck=0/inHand=1/inPlay=2/inPlayAttacking=3/waitingToBePlayed=4/waitingToDie=5/inCemetery=6/drawing=7/inHandShowing=8/inHandMoving=9/inHandPlaying=10/removedFromGame=11/inHandJustCreated=12/inMulliganSelected=13/inMulliganDiscarded=14/inPlayTransforming=15/inDeckDrawing=16/inPlayAminingAttack=17
- **MatchType**: Undefined=0/Ranked=10/RankedBot=20/PracticeOffline=50/EventAI=80/Practice=90/Tutorial=100/ClosedDeck=110/ClosedDeckBot=120/Campaign=130/TutorialReplay=140/Duel=150/Replay=160/Unranked=170/UnrankedBot=180/Scene=190/FastMode=200/FastModeBot=210/FastModeRanked=211
- **PositionOption**(→BattleHud.GetUIAnchor/uiAnimAnchors): self=0/cardTarget=1/abilityTarget=2/friendlyHero=3/enemyHero=4/boardCenter=5/boardCenterUI=6/cemetery=7/friendlyPower=8/enemyPower=9/nextToCemetery=10/nextToClock=11/friendlyMana=12/enemyMana=13/nextFriendlyMinionSpot=14/nextEnemyMinionSpot=15/…friendlyDeck=50/enemyDeck=55/…createCardPos=140/stratagemGeneralShowCase=145/friendlyHandWorld=150/enemyHandWorld=155
- **ChatMessage**: Intro=0/MirrorMatch=1/Bored=2/ICantDoThat=3/Concede=4/Greet=5/Threat=6/WellPlayed=7/Taunt=8/Sorry=9/Oops=10
- **LocKey 清单**('./Battle/…'): HUD/EndTurn、HUD/EnemyTurn、HUD/Replay、HUD/CardsLeft、Mulligan/Instructions、Tips/GoFirst、Mulligan/secondTurn、Mulligan/WaitEnemy、Mulligan/Replace、Mulligan/Undo、Tips/PendingBerzerk、ChooseCard/Instructions

## 附 C: 未决项(需后续深读的 3 点)

1. `YourTurnUI.DisplaySign` 的调用者 = 仅方法指针表引用(推测 inspector UnityEvent/Animator 事件触发)— 影响"我方回合大横幅"何时出现:建议按"回合切换(NextTurn 成功后)驱动"实现并抽查参照管线运行截图。
2. `AttackEnemyUnit` 类在 v31 不确定(未在 dump.cs 找到;拖拽攻击确认终点 = PUNReceivePlayerAction 的字段 `(actionType, actingCardID, targetCardID, targeted, cardPlayedPos, actionCounter, attackTpye)` + ResolveAttack)— 网络包字段已证。
3. EnemyDeck"运行时移入"的具体坐标: 场景里 EnemyDeck 静态位置 [-318,880 200x200](链式原点), `cardBackReference` 挂点微调;重建时直接按此坐标摆放即可(与旧实现一致)。

---

## 附 D: 方法→RVA 锚点索引(报告核心, 供复查)

CardsHorizontalLayout: GetPosition 0x8438A0 / GetRotation 0x844070 / GetClosestInHandSlot 0x843690 / Scale 0x5409F0 / .cctor 0x844760
ClockManager: GetTotalTime 0x622000 / SetEndTurnText 0x622600 / SetEnemyTurnUI 0x622920 / EndTurnClick 0x621F20 / EndTurn 0x621F90 / StartTimer 0x622EF0 / SetStartUi 0x622AF0 / Update 0x6236E0
PlayerHand: GetMulliganOption 0x6399B0 / AddCardsToMulligan 0x638040 / FinishMulligan 0x639360 / SetupCardInMulligan 0x63BB60 / MoveCardToMulliganPos 0x63A280 / SetupCardInHand 0x63B8B0 / PositionCardInHand 0x63AB40 / MoveCardsInHandToPosition 0x63A560 / CanDrawTurnStartCard 0x6385A0
MulliganManager: ActivateMulligan 0x635B90 / SetWaitingForEnemy 0x6360A0 / ClickMulliganDone 0x635D70 / ProcessMulliganDone 0x635E10 / SetMulliganTimer 0x636020 / SetupMulliganButton 0x636110
ManaTypeHolder: SetMana 0x626E10 / SetSavedMana 0x626F80 / ToggleManaAccumulation 0x627320 / SetAccumulationMana 0x626D20 / ToggleCurrentMana 0x627250 / DoTextAnim 0x626BB0
DeckManager: SetupDeck 0x624550 / SetYourTurn 0x624500 / DisplayDeckSize 0x6241B0 / GetReferencePosition 0x6243F0 / AddCardNotDrawnToDeck 0x624140
UnitOnBoardAttackTypeSelector: AttackButtonClick 0x64A450 / Toggle 0x64AB90 / ToggleJustOne 0x64A8B0
AttackTypesButtonsController: Initialize 0x6331E0 / ToggleAttackTypesSelector 0x633730 / HighlightSelectedAttackTypeButtons 0x632E10 / MoveButtons 0x633390
TargetReticleController: PrepareMovement 0x679A30 / SetAttackType 0x679DA0 / RaycastToWorld 0x679B40 / UpdateTrail 0x67A2E0
ActiveSkillDisplay: SetupActiveSkill 0x93EF50 / FadeIn 0x93DD40 / GetOpenPos 0x93E480 / ClickDown 0x93DA50 / Drag 0x93DCE0 / ClickUp 0x93DC20
EndBattleDoors: SetupDoor 0x624910 / ShowRewards 0x624E50
YourTurnUI: DisplaySign 0x6656E0(coroutine d__5 MoveNext 0x40E0)
EverguildButton: DoStateTransition 0x8469A0 / PlayButtonSound 0x8471E0 / SoftDisable 0x8475E0 / SwitchMaterial 0x847670
BattleManager: EndTurnClick 0x961330 / NextTurn d__395@0x9AA540 / StartBattleSequence d__331@0x9C4680 / StartBattlePhase d__334@0x9C3C80 / SetupMulliganPhase d__341@0x9C3120 / MulliganCountdown d__347@0x9A9E50 / MulliganFallbackCountdown d__348@0x9AA0B0 / FinishMulliganFirstPhase d__349@0x9A9160 / FinishMulliganFinalPhase d__351@0x9A8A00 / ResolveAttack d__438@0x9ACF10 / ResolvePlayCardFromHand d__447@0x9BBFA0 / ChooseCardMethod d__449@0x9A3C40 / ShowFinishScreen d__362@0x9C3640 / BasicBattleEndSequence d__391@0x9A3670 / DestroyUnitsAfterBattleEnd d__392@0x9A74E0 / CloseBattleDoors d__393@0x9A6BE0
PlayerHand: FinishMulligan d__52@0x6489C0 / MoveCardsInHandToPosition d__76@0x648EC0 / AddCardsToMulligan d__41@0x647910
YourTurnUI: DisplaySign d__5@0x65E220
