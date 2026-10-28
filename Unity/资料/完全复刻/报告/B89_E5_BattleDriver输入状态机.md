# B89 第 1 波 执行代理 `E5` —— R9 批次 8「BattleDriver 输入状态机」（Chain D 第 1 棒）

- 独占文件：`MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`（**只改了这一个文件**）
- 账行：`L236` · `L237` · `L303` · `L420` · `L446` · `L477` · `L478` · `L625`
- 补丁：`账/_parts/B89_E5.tsv`（11 列 · 8 行 · 已逐行数过列数）
- 行尾 / 行数：`\r\n` 全篇一致（`crlf 36565 / lines 36565 / bare_cr 0`）；**既有行号零漂移**（`ResolvePlayTargetByUnit` 改前后都在 `:8235`），新增成员**全部追加在类尾**（`:36432-36565`）

---

## §1 逐条结论

| 账行 | 原版判据（现读） | 改动（文件:符号名 + 现读行号） | 状态建议 |
|---|---|---|---|
| **L236** `CancelAbilityTargeting()` | `decomp_full/BattleManager__CancelAbilityTargeting.c:5-11`；调用点 `BattleManager__Update.c:1213`（非法目标支）、`BattleManager__ExecuteActiveAbility.c:41`（演出未收尾支） | 新建 `CancelAbilityTargeting()` `BattleDriver.cs:36458`；接线 ① `CancelTargetChoosingByInvalidClick` `:14838`（= `Update.c:1213`，取代原来内联的 `skillPanel.FinishActing()`）② `Resolve` 的阻塞早退 `:16260`（= `ExecuteActiveAbility.c:41`） | **A** |
| **L237** `CancelActionStates()` | cpp2il `BattleManager.cs:25244-25330`（`.c` 同体）；调用点 `__EndTurnClick.c:66` | 新建 `CancelActionStates()` `:36489`；接线 `EndPlayerTurn` `:16671`（取代原来那句 `ClearSelection()`） | **A** |
| **L303** `EndTurnClick(bool)` | `__EndTurnClick.c:46-56,66,69-91,123-152` + cpp2il `BattleManager.cs:20996-21146` | 无新增代码：四个状态位逐条核对（落点见下），只有 ④ 那一跳由 `ClearSelection()` 换成 `CancelActionStates()`（同一条原版语句） | **A**（纯审计） |
| **L420** `IsPlayingCardState()` | `BattleManager__IsPlayingCardState.c` + cpp2il `:20968-20994` | 新建读口 `public bool IsPlayingCardState` `:36516` | **A**（原版 0 调用者的死读口） |
| **L446** `NeedToChooseMinionTarget(...)` | `BattleManager__NeedToChooseMinionTarget.c:7-19` · `__Update.c:1058-1061` · `_ResolvePlayCardFromHand_d__447__MoveNext.c:849-860,903-970` | **零改动**：谓词早就在 `NeedsUnitTargetAfterDeploy` `:7923`；残差 = 撤回语义，**要引擎口** | **B（维持）** |
| **L477** `RegisterPlayerAction(...)` | `__RegisterPlayerAction.c:19-34`；**可达写点** `__AddAutoActionToQueue.c:151-165`；读者 `__EndTurnClick.c:143-151`；归零 `_NextTurn_d__395:221` / `SetupInitialState.c:149` | 新建 `RegisterPlayerAction()` `:36543`；写点 `LocalAct` `:331`；`ResetTurnActionCounter()` `:36561` ← `ResetClock` `:14514`；读口 `TurnActionCounter` `:2928`（= `Ctx.ThisTurnActionCounter`） | **A** |
| **L478** `RegisterPlayerActionCancelled(...)` | `__RegisterPlayerActionCancelled.c:19-34`；调用点 = 三条结算协程 `_ResolveAttack_d__438:226/588/1365` · `_ResolvePlayActiveAbility_d__479:456` · `_ResolvePlayCardFromHand_d__447:341/1049` | 新建 `RegisterPlayerActionCancelled()` `:36554`（写同一格，**今天 0 调用者且结构上正确**，见 §1·L478） | **B（理由收窄）** |
| **L625** `WaitingForBlockingAction(bool)` | cpp2il `BattleManager.cs:44962…`；Replay 短路 `__WaitingForBlockingAction.c:26-30` | **零改动**（审计）：属性在 `:20157`，消费端 10 处在；transformUnit 本体在 `EffectResolver.cs:3407` | **A（B→A）** |

### L303 的四个 UI 状态位逐条对（本次审计的核心）

| 原版状态位 | 出处 | 我们的对等物 | 结论 |
|---|---|---|---|
| `UIstate == choosingResolutionCard(14)` ⇒ 静默 return | `__EndTurnClick.c:44-46` | `HudClickBlocked` → `HudButtonsBlocked`（`_choosePanel` / `_cardDisplay` / `_multiCards` 任一开着）`:13463`/`:13470` | 等价 |
| `resolvingTurnEndFlag(+0x245) == 0` | `:46`（置位 `:123`，清位在 `ResolveEndTurn:336` 的 2 字节写 `+0x244/+0x245` 一起清） | **无显式 latch** —— 我们是**同步**结算（`RuleCore.EndTurn` + `BeginTurn` 一次做完），没有「正在结算回合结束」这段窗口 | 净等价（结构上不需要） |
| `allowPlayerActionsFlag(+0x244) != 0` | `:46`；写点 `_NextTurn_d__395:380`（按座位）、清点 `ResolveEndTurn:336` / `StopBattleAfterBattleEnd:48` | `ClickEndTurnHud` 的 `if (Ctx.Active != _me) return;` `:5991` | 等价 |
| `!WaitingForBlockingAction(showTips:false)` | `:46-48` | `EndPlayerTurn` 的 `if (fromUi && WaitingForBlockingAction) return;` `:16651`；超时支另有独立闸 `:14691` | 等价（**静默**，与原版 `showTips=0` 一致） |
| 教程闸 `CheckIfPlayerActionPermittedInTutorial(endTurn(0xe))` | `:54-60` | `if (!TutorialPermits(TutAttemptEndTurn()))` `:16656` | 等价 |
| 狂暴拦截（`param_2 == 0` 才拦） | `:69-91` | `if (fromUi && !timeOut && RuleCore.HasPendingBerzerkAttack(Ctx))` `:16679` | 等价（`timeOut` 对应 `param_2`） |
| 超时缩时（`counter == 0` ⇒ `+0x4DD = 1`） | `:133-151` | `ShouldReduceNextTurn(...)` `:16696`，第 4 实参已换成 `TurnActionCounter`（原 `_actionsThisTurn`） | 等价（值不变） |

### L478 为什么「0 调用者」是对的
原版 `++` 发生在**牌落位那一刻**（`AddAutoActionToQueue`），`--` 发生在**取消落位 / 取消选目标**（三条结算协程）。我们的两段式是「点定目标之后才 `RuleCore.PlayCard`」⇒ 一手没成的牌**从没进过计数器**，也就没有可减的。两个时刻之间的瞬态值**没有任何读者**（唯一读者是 `EndTurnClick` 的超时支）⇒ 净效果相同；差异只在**过程可见性**（= L446 那条「先落位 vs 点完才落位」）。⛔ 别为了「让它有调用者」而在表现层自造撤回。

---

## §2 验证读数

- `TMPDIR=/tmp/wf_B89E5 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**（最后一次；中途两次报的 4 条 / 2 条 `CS0103` 全部集中在 `RuleEngine/Core/TutorialScript.cs`（`RunShowManaAura` / `RunResolveCard` / `PickFirstMinion`），**不是本代理的文件**，行号在一次重跑里从 944/1684/1695 漂到 1756/1767 ⇒ 是别人正在写，未去改）。
- 行尾：`b.count(b'\n') = 36565`、`b.count(b'\r\n') = 36565`、裸 CR = 0（二进制读）。
- 零漂移自检：`bool ResolvePlayTargetByUnit(...)` 改动前后都在 **`:8235`** ⇒ `:8235` 以下的既有落点全部不动。
- **未跑** Unity / 批处理 / ruleprobe（红线）。

---

## §3 顺手发现 + 真机待验

### 顺手发现（**没有改**，交给对应线）
1. 🔴 **判据订正（L477 族）**：原版 `RegisterPlayerAction`（`++`）**全库 0 个调用者**（`decomp_full` / `out_cpp2il_dev_ilspy` / `out_il2csharp` 三源只命中定义）；真正可达的 `++` 在 `BattleManager__AddAutoActionToQueue.c:151-165`（同一段 `ObscuredInt.op_Increment`，紧跟 `ClockManager__LogPlayerAction` 之后）。`RuleEngine/Core/BattleContextTurnCounter.cs` 的 doc 只记了前者 ⇒ 建议合并时补一句（**本代理无权改那本**）。
2. **`EndTurnButton` cue 的时机**：原版在 `__EndTurnClick.c:50`（**教程闸之前**）就响；我们排在教程闸拒之后（`BattleDriver.cs:16670` 在 `:16656` 之后）⇒ 教程拒那一下原版会先响 `EndTurnButton` 再响 `EndTurnButtonInactive`，我们**只响后者**。归音效线小账。
3. **L625 旧账的两句「残差」都已被现读推翻**：①「消费端待 W57」⇒ 消费端 10 处已在；②「transformUnit(G02) 待做」⇒ 本体在 `EffectResolver.cs:3407（DoBecome）`。⇒ 本条可转 A。
4. `Resolve` 的阻塞早退 `:16260` 与 `NotifyCantDoAction` **写在同一行**（`if (kind == AttackKind.Ability) CancelAbilityTargeting(); return RuleCodes.ErrNoAction;`）—— 这是**刻意的**：插新行会让 `:8235` 以下全部账落点 +1。

### 真机待验项（输入状态机 = 手感，断言测不出）
1. **结束回合时正等着点目标**：出一张 targeted 单位卡（例 `ASH15 Howling Banshee`）→ 落位后进入「点目标」态 → **点「结束回合」** ⇒ 期望：这一手被撤、牌回手、回合正常结束；**且不会在下一回合开始时仍处于等目标态**（改前会残留）。看点：结束时**没有** `DragToTarget` 提示（`silent` 那一路）。
2. **技能面板「正在放技能」时被挡**：放一个主动技、故意在上一手演出还没收尾时点确认 ⇒ 期望：`PleaseWait` 抬头 + 语音，**面板白层淡出**（`FinishActing`），选择器/准星不残留；再点「释放」应能重新点亮目标（原版不清 `cardSelected`）。
3. **点非法目标的技能取消**：选主动技 → 点自己人/空处 ⇒ 期望 `无效的目标` + 白层淡出 + 高亮全收（这条原来就是 A，只是换了落地口，回归验一下别退化）。
4. **拖牌中途结束回合**（覆盖 `CardInteraction` 那一档）：拖着手牌不松手去点结束回合 ⇒ 期望指挥态被收（`CancelCommand` 老路），本批不改它，**只确认没被我这几跳带坏**。

---

## §4 没查清 / 明确没做

1. **L446 撤回语义**：缺的是**引擎口**（`RuleCore` 一次性 `PlayCard` 无回滚）。没查清的部分：原版 `TransformUnitInPlayToCard` + `AddCardNotDrawnToHandAtIndex(原下标)` 在我们引擎里能否用现有 `BoardSlots.Insert/Remove` + `RuleCore.AddCardToHandAtWithinLimit`（`EffectResolver.cs:3770` 已在用）拼出来、以及「落位」对光环/触发/流血那串副作用的影响（`Auras.Recompose` 幂等 ≠ 无副作用，见 CLAUDE.md 那条）。**这条必须有引擎写手与隔离实验**，本波只改 `BattleDriver.cs` ⇒ 未动。
2. **`IsPlayingCardState` 的 3 / 5 两档**（`playingMinion` / `playingNonTargetSpell`）没有独立布尔：起飞、溶解都住在 `_blocking` 演出窗口 / `DeploySequence` 里。要精确覆盖得在演出路径上开读口（会动演出 ⇒ 另开账）。原版这个方法 **0 调用者**，所以影响面为零。
3. **`resolvingTurnEndFlag` 没有落成字段**：我们同步结算，没有那段窗口。若将来回合结束改成异步/入队形态，这条要重新做。

---

## §5 交接（Chain D 第 2 棒 = 批次 9）

### 本代理动过的符号（批次 9 请避开，或先 `grep`）
| 符号 | 现读行号 | 类型 |
|---|---|---|
| `TurnActionCounter`（只读转发 `Ctx.ThisTurnActionCounter`，**取代已删的 `_actionsThisTurn`**） | `:2928` | 属性 |
| `RegisterPlayerAction()` / `ResetTurnActionCounter()` 的调用点 | `:331`（`LocalAct`）· `:14514`（`ResetClock`） | 既有行内改 |
| `ResolvePlayTargetByUnit(..., bool silent = false)` | `:8235` | **签名变了**（新增可选形参；唯一传 `true` 的是 `CancelActionStates`） |
| `CancelTargetChoosingByInvalidClick` 里的 `FinishActing` | `:14838` | 改成 `CancelAbilityTargeting()` |
| `Resolve` 的阻塞早退 | `:16260` | 加 `CancelAbilityTargeting()`（与 `return` 同一行） |
| `EndPlayerTurn` 里那句 `ClearSelection()` | `:16671` | 改成 `CancelActionStates()` |
| `CancelAbilityTargeting()` / `CancelActionStates()` / `IsPlayingCardState` / `RegisterPlayerAction()` / `RegisterPlayerActionCancelled()` / `ResetTurnActionCounter()` | `:36458 / :36489 / :36516 / :36543 / :36554 / :36561` | **新增，全在类尾** |

### 给批次 9 的三条话
1. **`_actionsThisTurn` 已经不存在了**（改成 `Ctx.ThisTurnActionCounter` 单一持有）。批次 9 若碰缩时/动作计数，读 `TurnActionCounter`（`:2928`）。
2. **`ResolvePlayTargetByUnit` 多了一个 `silent` 形参**；别在新调用点乱传 `true`（原版只有 `CancelActionStates` 那一支不出声）。
3. 类尾 `:36432` 起是本次新增区，**批次 9 的新成员继续往 `:36565` 之后追加**，别插在中间（既有账落点会整体漂移）。

### 没做的账行（明确交出去）
- **L446**（B）：要引擎口 ⇒ 交引擎线/后续棒（判据与积木见 §4·1）。
- **L478**（B）：与 L446 同一件事的另一面 —— 只有做了「先落位」才会出现 `--` 的调用点；**两条必须同批做**。
