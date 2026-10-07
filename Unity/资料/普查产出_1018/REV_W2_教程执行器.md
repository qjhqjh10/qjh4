# REV_W2 · 教程执行器（`A938`）独立审查

> 独立审查代理 REV_W2，2026-10-18。**只读、只找错**（⛔ 一个字没改；唯一产出 = 本文件）。
> 审查对象 = `资料/普查产出_1018/W2_教程执行器交件.md` 的 7 个文件（`BattleDriver`(493/2) ·
> `Core/TutorialScript.cs`(417/12) · `RuleEngine/Core/UnitState.cs`(44/1) · `ReplayStore.cs`(30/0) ·
> `Editor/BattleScene.cs`(236/8) · `RuleEngine/Core/RuleCore.cs`(43/0) · `RuleEngine/Editor/RuleEngineTest.cs`(296/0)）。
> 工作区里 `Net/*` · `Deck/*` · `Shell/*` · `Core/Loc.cs` · `Editor/{CollectionScene,DeckScene,MainMenuScene}.cs`
> 是**别的写手**的改动，**本审查一律不涉及**。

---

## 0. 一句话结论

**真问题 11 条 · 存疑 2 条 · 作者自陈不成立 2 条（§9·4/§10·2 的判据前提 + §7 里 ⑦ 那半条「灭自证」）。**

- 🔴 **最重要的一条（必查 1）**：作者 §9·4 自陈的取舍**前提是错的** —— 原版**不会**替玩家执行那一条；
  但**他的实现因此恰好是照原版的**（行为对、判据写错）。⇒ 代码不用改行为，**要改的是 2 处代码注释 + 交件正文**。
- 其余真问题集中在：**ChangeTo/攻击型的持久状态缺 6 个写点里的 1 个** ·
  **⑭ 只做了一半** · **① 比原版严**（7 条数据会受影响） · **两条断言没有鉴别力** ·
  **同一句错话还留在别处（铁律 5）**。

---

## 1. 🔴 必查 1：§9·4 那条取舍 —— **判据级结论**

### 1.1 我做了什么

`AiScripted::PlayScriptedTurn` **有两份同名重载**（`dump.cs:24562/24566`，教程那份 RVA `0x944060`、
战役那份 `0x944190`），`decomp_full/AiScripted__PlayScriptedTurn.c` 落盘的是**战役**那份（用 `+0x14`、
`param_3` 当 `List` 用）—— 所以**教程那份的 `.c` 读不到**。
⇒ 我按 CLAUDE.md「`.c` 读不出来 ≠ 拿不到（VA 反汇编）」**直接反汇编 `VA 0x180944060`**（capstone，
`d:/2/unity_run_ref/GameAssembly.dll`，PE 节表换算 RVA→文件偏移）。

### 1.2 判据（教程 `PlayScriptedTurn` 的完整机器码，原文）

```
1809440aa  mov  eax, [rax+0x18]        ; rax=[turnData+0x20]=scriptedActions(✓dump.cs TurnScriptedData@0x20)
1809440ad  cmp  [rbx+0x10], eax        ; ★ rbx=this(AiScripted) ⇒ [rbx+0x10] = actionCounter
1809440b0  jge  0x180944176            ; >= count ⇒ return false
1809440f3  call 0x180851630            ; ScreenHighlightPosition.*(manager+0x218)  ← 先关高亮
1809440f8  mov  rcx, [rdi+0x20]        ; scriptedActions
18094410c  mov  edx, [rbx+0x10]        ; index = actionCounter
18094410f  call 0x180f4ab90            ; List<ScriptedAction>.get_Item
180944125  cmp  byte ptr [rax+0x40], r9b   ; ★ r9b=0 ⇒ 与 action.playerAction(✓dump.cs ScriptedAction@0x40) 比
180944129  jne  0x180944171                ; playerAction != 0  ⇒  跳去 0x180944171
18094412b  call 0x180940dc0                ; 否则 ExecuteAction(= 落盘那份 AiScripted__ExecuteAction.c)
180944130  inc  dword ptr [rbx+0x10]       ; 且只有这一支 ++actionCounter
...
180944171  call 0x180944290                ; ★ = ShowPlayerActionAnim（dump.cs:24634 同名 RVA 0x944290）
180944176  xor  al, al ... ret             ; ★ 返回 false —— 这一支【没有】ExecuteAction、【没有】++
```

### 1.3 结论（逐条回答必查 1 的 ①②③）

| 问 | 答 | 判据 |
|---|---|---|
| ① `PlayScriptedTurn` 在「这一条是玩家动作」时到底执不执行？ | **不执行**。它走 `ShowPlayerActionAnim` 并 `return false`，**不调 `ExecuteAction`、也不 `++actionCounter`** | `0x180944125-0x180944129` → `0x180944171` → `return false` |
| ② `ExecuteAction` 里真的没有 `playerAction` 守卫吗？ | **确实没有**（作者这一半是对的） | `grep -n "param_3 + 0x40" AiScripted__ExecuteAction.c` = **0 命中**；该文件按 `param_3+0x48`(List<ScriptedActionData>) + `data[0]+0x10`(ScriptedActionType) 判，**就是教程那份重载**（战役数据类 `ScriptedActionCampaignData` 的字段只到 `+0x38`，没有 `+0x48`） |
| ③ 与 `CheckIfPlayerActionPermittedInTutorial` 矛不矛盾？ | **不矛盾，而且正好互补**：脚本**停住不碰**玩家那一步 ⟺ 闸门**只放行**那一步。`_ExecuteScriptedTurn` 那句「无条件调 `PlayScriptedTurn`」之所以没坏事，正是因为**守卫在 `PlayScriptedTurn` 里面** | 闸门全文 `BattleManager__CheckIfPlayerActionPermittedInTutorial.c:39-95`（我逐句比过作者 `TutorialScript.cs:946-965`，**6 步一一对上**，含 `0x46 PlayerChoice` / `0xaa ActivateHandCards` 两条短路与 `CheckIfMatchesActionData` 的位置） |

### 1.4 判据级结论

- **作者的实现（`DriveTutorialScript` 先判 `IsNextActionScriptedPlayerAction` ⇒ 返回 `WaitingForActor`、
  不调 `ExecuteAction`）⇒ 行为上【照原版】。**
- **但作者的判据前提（"原版无条件执行、ExecuteAction 里没有守卫、所以必须偏离"）是【错的】**：
  原版确实"无条件调 `PlayScriptedTurn`"，可**那条路径在函数内部就分岔了**。
  ⇒ 这是**「结论蒙对、理由写错」**，必须按铁律 5 **就地订正**，否则下一个会话会照着错理由继续推理
  （作者自己就已经据此写下"这条取舍要主对话点头"）。
- **连带两条真偏离**（作者没提）：
  1. 原版那道守卫**在 `PlayScriptedTurn` 函数内**，我们的在**调用方**（见真问题 R2）；
  2. 玩家动作那一支原版还会调 `ShowPlayerActionAnim(manager, action)` + 关 `ScreenHighlightPosition`
     —— 我们这一支**什么都没有**（归 A940 的表现层，但**调用点在执行器里**，要写进 A940 的账）。

---

## 2. 逐条问题表

| # | 结论 | 证据（`文件:行号`；≤3 行片段） | 该照哪个判据 |
|---|---|---|---|
| **R1** | 🔴 **真问题**（文档/判据缺陷，行为对） | `BattleDriver.cs:7090-7094`：`// · 脚本每一帧「推一条」—— AiScripted__PlayScriptedTurn.c：无条件 ExecuteAction(当前那条) 然后 *(+0x14) += 1` ·`// 先 PlayScriptedTurn（照推）、再看 IsNextActionScriptedPlayerAction`；同句副本 `BattleScene.cs:13659` | 反汇编 `VA 0x180944060`（§1.2）；**反例**：原版那一支调的是 `0x180944290 = ShowPlayerActionAnim` 且 `return false`。顺带：教程口径是 **`+0x10`**，`+0x14` 是战役那份 |
| **R2** | **真问题**（结构性偏离） | `Core/TutorialScript.cs:675-684`：`public bool PlayScriptedTurn(...) { ... ExecuteAction(ctx, a); ActionCounter++; return ...; }` —— **无 playerAction 守卫**；唯一的守卫在 `BattleDriver.cs:7164` | 原版守卫在函数内（`0x180944129`）。项目红线「两处写同一条规则 = 迟早不一致」。**现有反例**：`BattleScene.cs:13630-13631` 的 `probe.PlayScriptedTurn(cT1)` 在**玩家动作**那一条上真的执行了一次 |
| **R3** | 🔴 **真问题**（写点清单漏 1 个 ⇒ 账不全） | `UnitState.cs:205-209` 列了 5 个：`CardScript__AddEffect.c:480/:626` · `OnTurnEnd.c:217` · `ResolveActiveAbilityPlayed.c:39` · `UpdateAttackText.c:26`。**漏** `CardScript__ActivateMinion.c:71-72`：`if ((cVar3 != '\0') && (*(int *)(param_1 + 0x120) != 4)) { *(undefined4 *)(param_1 + 0x120) = 4; ... }`（`displaySummonSickness` 为真 ⇒ 写 **4**） | `grep -n "+ 0x120" CardScript__*.c EntityScript__*.c`：写点共 **7** 处（ctor=1 / CardSetup / ChangeAttackType / ChooseAttackTypeAutomatically / AddEffect×2 / **ActivateMinion** / OnTurnEnd / ResolveActiveAbilityPlayed / UpdateAttackText） |
| **R4** | **真问题**（铁律 5：同一句错话的副本没改） | `BattleScene.cs:9634`：`// 🆕 2026-09-29：**引擎那一侧真的记下了这一档打法**（原版 EntityScript.currentAttackType）` + `:9636-9637`：`"★ 打出一记近战之后 LastAttackType = 1（原版 currentAttackType；"` —— 作者只在 `UnitState.cs` 改了这句 | 作者自己的更正（`UnitState.cs:171-177`）：`LastAttackType` **不是**原版 `currentAttackType`，那是两个字段。铁律 5 明写「顺手 grep 那句话的关键词，把同一句话被复制到别处的地方一起改」 |
| **R5** | 🔴 **真问题**（两条断言没有鉴别力，「灭自证」说明不成立） | `BattleScene.cs:13632-13637`：`Check(!probe.Unhandled.ContainsKey(AttackFreeMode), "…｜🧨 把 ExecuteCore 里那两个 case 删掉 ⇒ 本条红")` + `Check(probe.Unhandled.Count == 0, …)` | **删掉那两个 `case` 不会变红**：`ExecuteCore` 的 `default: Note(ctx, a.Kind); return false;`（`TutorialScript.cs:768-770`）把这一档记进 **`StageActions`**（`grep -n "Unhandled\[" TutorialScript.cs` ⇒ **唯一写点 = `Stub`**，`:891`）⇒ `Unhandled` 照样空。这一对真正保护的是「`Unhandled` 的**记账口径**」（靠 `:13641-13645` 的 ActiveAbility 那一条），**保护不了「`Attack` 那一档实现了」** |
| **R6** | 🔴 **真问题**（⑭ 只做一半 + 可被绕过） | 原版 `BattleManager__Update.c:1066-1085`：`cVar13 = CheckIfWaitingToChangeAttack(...); if (cVar13 == '\0') { … AddAttackAction … }` ⇒ **非零就拦住这一刀**。我们 `BattleDriver.cs:7059-7066` 的 `TutorialFrameGuard` 只 `ClearSelection()`，而 `Matches` 对 `ChangeTo*` **接受 `attack`**（`TutorialScript.cs:1011-1014`，与 `ScriptedActionMatchData` 的 `case 5/6: param_2 != 0x13 && param_2 != 1 → 0` 一致） | ⇒ **脚本当前那条玩家动作是"换打法"时，玩家直接打出去照旧放行，而且 `DoResolve` 末尾的 `ContinueTutorialScript()`（`:6978`）照样把指针 +1**。另一处 `:182`（`CheckIfWaitingToUseAbility`）原版只是 `LogWarning` ⇒ 我们也可以只出声 |
| **R7** | **真问题**（我们比原版**严**） | 原版 `BattleManager__CanPlayCard.c:56-59`：`in_stack_..b8 &= 0xffffffff00000000; CheckIfPlayerActionPermittedInTutorial(param_1,5,param_2,0,in_stack_..b8,0)` ⇒ 第 5 实参（`targetPosition`）= **0**；而 `CheckIfMatchesActionData.c:34` 的 left/right 判断**要求 `param_5 != 0`** ⇒ 原版在 14 个调用点上**从不判左右**。我们 `BattleDriver.cs:6528-6536` 把 hover 槽号当 `TargetSlot` 传进去 | 数据里受影响的 = **7 条**（`PlayCard playerAction=true` 且 target ∈ {`PlayerMinionLeft(31)`: 6 条 · `NotLeft(32)`: 1 条}，我逐条 dump 过）。方向是"更严"，不是漏 —— 但按铁律 11 要如实记（没有指针教学时，表现就是"拖到那一侧拖不动"） |
| **R8** | 真问题（低危，第二条出牌路绕过闸门） | `BattleDriver.cs:11240-11246`：`public int SimulatePlay(int idx,int slot){ … LocalAct(act, () => RuleCore.PlayCard(Ctx,_me,idx,slot)); … }` —— 注释只写了"跳过鼠标拖拽，但不跳过录像"，**没提闸门** | 它是自检 / `-wfdrive` 专用（产品里出牌一律 `BeginPlay→DoPlay`，我核过 `grep DoPlay(` 的全部调用点）⇒ 不构成玩家越权，但注释要补「这一条不过教程闸门、也不推脚本指针」 |
| **R9** | 真问题（覆盖缺口） | `BattleDriver.cs:7499-7528` 那一支（`TutorialOwnsThisTurn` ⇒ `DriveTutorialScript()`；`Exhausted/WaitingForActor` ⇒ `EndTurnAndAdvance(1-_me)`；`NotOwned`(回放) ⇒ **什么都不做**）**没有任何断言** —— ⑧ 节全部走 `DriveTutorialScriptForTest()` 手动驱动 | 作者 §7 说"接线 4 条"，实际覆盖的是**执行器自身**；「脚本接管 AI 回合」这一支的 3 条行为（含"回放局不许自吞回合"）只有静态复核 |
| **R10** | 存疑（判据不足，但按"事实"写进了注释） | `ReplayStore.cs:83`：`⚠️ version 不 +1：这是一个纯新增字段，老录像读出来是默认 -1` | 这依赖「`JsonUtility.FromJson` 会跑字段初始化器」。**我在本地没找到判据**（最接近的先例 `DeckRulesTest.cs:453-460` 考的是**隐式默认 0**，证不了负初始值）。若 FromJson 不跑初始化器 ⇒ `0` ⇒ `BeginTutorial(0)` ⇒ **所有老录像都按"教程第 1 关"放**。建议：一条断言（喂一段不带该键的 JSON 给 `ReplayStore.Load`，断 `-1`）或改用 `version` 分级 |
| **R11** | 真问题（小口子，正是"静默失败"那一族） | `Core/TutorialScript.cs`：`ExecuteAction(...){ ExecutedActions++; if (a == null) return false; ...}` —— **null 动作静默消费**（指针照 ++、不进 `Unhandled`/`Unresolved`、不出声） | 工程红线「不许静默失败」。同族：`PlayReplay` 里 `if (m == null) continue;`（既有） |

### 存疑两条

- **S1（作者已如实标，但该开账）**：`PlayCard` 落点我们**恒**用 `SimpleAI.NextDeploySlot`（`BattleDriver.cs:2637`），
  原版取**当时的鼠标位置**（`MousePositionDebug.GetMouseClickPosition` → `MinionManager.GetClosestAvailableSlot`，
  `ExecuteAction.c:856-869`）。作者的理由是"我们这个环境没有真鼠标" —— 但**产品里是有鼠标的**
  （`Input.mousePosition` + 我们已有的 `GetClosestAvailableSlot` 那一族）⇒ 只有**批处理/自检**该退化。
- **S2**：⑧ 节那批断言依赖"引擎允许督军打督军 / 脚本那一记 `Attack` 能打出去"等前提，**我碰不到 Unity**（见 §7）。

---

## 3. 必查 2：闸门 14 点

**核过的面**：① 原版调用点**真的是 14 个**（`grep -rn CheckIfPlayerActionPermittedInTutorial` 去掉定义文件 ⇒ **14 行 / 14 个不同文件**，与 R2 的清单一致；⚠️ 初查时我用 `…Tutorial(` 带括号搜只拿到 5 个 —— 因为有 9 处**换行**了，记一笔防复踩）；
② 每一个调用点的**实参形状**（`param_2` = `BattleActionType`、`param_3` = actingCard、`param_4` = targetCard、`param_5` = targetPosition）；
③ 我们的落点是否覆盖同样的语义；④ 有没有绕过表现层直接改引擎状态的路。

**结论：**

1. **落点分布本身是对的、且与 R2 的建议一致** —— `RuleCore.cs` 的 43 行**全是新增**（`DeclareAttackByCurrentType` /
   `CurrentAttackIsRanged` / `SetCurrentAttackType`），**`Can*` 一个都没动**（我逐 hunk 核过）⇒
   "闸门叠在引擎之上、AI 不受影响"这条**架构判据成立**。
2. **五处合并是三处合并，不是丢**：`TutAttemptTarget` 一处覆盖 `IsValidSpellTarget`(③) / `IsValidAttackTarget`(④) /
   `IsValidActiveAbilityTarget`(⑤) / `IsValidActiveAbilityTargetTutorial`+`CanShowPotentialTargets`(⑨)，
   因为原版这五处的实参**同形**（acting=那格、target=目标那格、targetPosition=0）⇒ 合并无害 ✓。
   `CanShowPotentialTargets` 传的 `0x35`(53=attackContinuation) 与我们的 `attack`(1) 在 `Matches` 的
   `case 0`（`0x1e`: 收 1 与 0x35）与 `case 1`（位掩码 {1,19,53}）里**都收** ⇒ 无语义损失 ✓。
3. 🔴 **两处真缺口 / 偏离**：**R6**（⑭ 的第二半 `CheckIfWaitingToChangeAttack` 没实现，玩家可以"不换打法直接打"）
   与 **R7**（① 比原版严）。这两条都在"判别式"之外，**作者的两条判别式（越权被拒 / AI 不受影响）核得到、
   也真能分离两种状态**（`BattleScene.cs:13678-13700`：删掉闸门 ⇒ `TutorialPermitsForTest` 恒真 ⇒ 第一条红；
   把闸门塞进 `RuleCore.CanAttackNow` ⇒ `:13687` 的引擎判定变非 OK ⇒ 第二条红）—— **这两条我认。**
4. **绕过表现层的路**：找到 **1 条** = `SimulatePlay`（**R8**，自检/`-wfdrive` 专用，不是玩家路）。
   玩家侧四个入口（出牌 / 攻击·技能 / 收灵魂石 / 结束回合）**都只有一条落地路**，且都过闸门 ✓。

---

## 4. 必查 3：第三记账口

**核过的面**：`TutorialScript.Executed` 的发出点 / 六档覆盖 / 伪 kind 的两端 / `RecAct`·`RecRaw` 是不是只记不落地 /
`NetApply.Apply` 收不收那两条 `AiAction` / 回放那一支 / `ReplayStore.cs` 注释自洽 / `tutorialStageIndex`。

- ✅ **六档全覆盖**：`Executed` 在 `ExecuteAction` 末尾发（`TutorialScript.cs:700-712`），
  接收端 `OnTutorialScriptExecuted`（`BattleDriver.cs:7336-7362`）逐档落：
  `DrawCard`→伪 kind 201 · `ChangeTo*`→伪 kind 202 · `PlayCard`→`AiAction` · `Attack`/`AttackFreeMode`→`AiAction`；
  失败（`ok=false`）不记 ✓。
- ✅ **两端对得上**：`RecAct(new AiAction{HandIdx,Slot})` → `NetProtocol.ToWire` → `NetApply.cs:77-80` 读的正是 `m.handIdx`/`m.slot`；
  `Attack*` 那条读 `m.slot/targetP/targetSlot/ranged`，作者三个字段都填了 ✓。伪 kind 的落地在 `ApplyTutorialRaw`（`BattleDriver.cs:7380-7386`），
  在 `ApplyLoggedAction` **之前**调用 ✓。
- ✅ **`RecAct`/`RecRaw` 只记账不落地**（`BattleDriver.cs:461-497`），不会重复执行 ✓。
- ✅ **回放局不自驱**：`DriveTutorialScript` 第一句 `if (_replaySession) return NotOwned;` ✓；
  且回放时 `_rec == null`（`PlayReplay` 里 `var keep = _rec; _rec = null;`）⇒ 记账口那一跳也空转 ✓。
- ✅ **`ReplayStore.cs` 的判据注释自洽**（三个口 / 两个伪 kind / 「等玩家」那 110 条不在 ③ 上）✓ 与代码一致。
- ✅ **「等玩家」那 110 条确实走 ①**：`DoPlay`/`DoResolve`/`CollectWaystone`/`EndPlayerTurn` 四处都走 `LocalAct` 或 `RecAct` ✓。
- ⚠️ **一处语义瑕疵（小）**：`PlayerChoice` 那一支 `return true` 但**从不设 `done.Kind`** ⇒
  `Executed` 会发一条 `ok=true, Kind=None`，与注释写的「只有真的改了引擎状态才 `ok=true`」不符。
  实际无害（接收端 switch 落空、什么都不记），但**"ok" 这个词与文档不一致**，容易误导下一处订阅方。
- ⚠️ **R10**：`tutorialStageIndex` 的向后兼容只有注释背书。

---

## 5. 必查 4：`ChangeTo*` / `CurrentAttackType`

- ✅ **初值判据全对且我独立核过**：`EntityScript__.ctor.c:5` = `1`；随后四处同算式
  `(CurrentMeleeAttack < CurrentRangeAttack) + 1` —— `CardScript__CardSetup.c:263-267` ·
  `ChooseAttackTypeAutomatically.c:10-12` · `UpdateAttackText.c:23-26` · `OnTurnEnd.c:212-217`（**我逐份读了**）。
  我们 `RangedAttack > Attack ? Ranged : Melee`（`UnitState.cs:306`）**等价**，平手取近战 ✓。
- ✅ **与 `SimpleAI.UseRanged`**：`UseRanged = u.RangedAttack > u.Attack`（`Data/SimpleAI.cs:1214-1217`）
  ⇒ **在任何 `ChangeTo*` 之前逐个局面等价** ✓。⚠️ **但作者注释里的「逐个局面等价」漏了限定**：
  一旦 `ChangeTo*` 改过那一格，两者**就分叉**（`CurrentAttackType`=近战而 `RangedAttack > Attack` 仍为真）
  ⇒ 那句话该写成「**在没被 `ChangeTo*` 改过之前**等价」。而且这构成**同一条规则的第 3 份写法**
  （`SimpleAI.UseRanged` / `BattleDriver.UseRanged` / `UnitState` ctor）—— 工程里已有"两处写同一条规则"的前科。
- ✅ **"既有攻击断言 0 条会红"——我独立判：结论成立**。理由：
  `DeclareAttack` 签名/默认值/方法体一字未动（`RuleCore.cs:1917` 附近无 hunk）；
  `UnitState` 的改动是**纯新增**（1 个字段 + ctor 末尾 1 行，且在 `Attack`/`RangedAttack` **赋值之后**）；
  第二个 ctor（`UnitState(CardDef,bool)`）**委托**给第一个 ⇒ 不留默认 0 的单位 ✓；
  全仓**没有**任何断言/测试比较整个 `UnitState` 或字段集（反射用法只有 4 处，都点名具体字段：
  `RuleEngineTest.cs:8932` `RuleCore.ChooseEffectPools` · `BattleScene.cs:3125` TMP · `NetSelfTest.cs:904` ·
  `ShellScene.cs:2990-3008`）✓。
- 🔴 **写点清单漏 1 个 = R3**（`ActivateMinion` 写 4）。
- ⚠️ **落点没改 `DeclareAttack` 默认值** ⇒ 这条判据我认（99 个调用点省略 `ranged`）✓。

---

## 6. 必查 5：断言鉴别力（逐条）

**弱/假断言：**

| 位置 | 问题 | 判据 |
|---|---|---|
| `BattleScene.cs:13632` | `Check(!probe.Unhandled.ContainsKey(AttackFreeMode), …"🧨 删掉那两个 case ⇒ 本条红")` | **删掉也不红**（`default: Note` ⇒ `StageActions`）⇒ 见 **R5** |
| `BattleScene.cs:13635` | `Check(probe.Unhandled.Count == 0, …)` | 同上，**恒真**（`Unhandled` 唯一写点是 `Stub`） |
| `BattleScene.cs:13641-13645` | `Check(!aaScript.ExecuteAction(...) && aaScript.Unhandled.ContainsKey(ActiveAbility), …)` | ✅ **这条有牙**：把 `ActiveAbility` "顺手实现" ⇒ 红（也正好兜住"记账口径"） |
| `RuleEngineTest.cs:18040` ① | 前提断言 `Check(!string.IsNullOrEmpty(why), …)` 一类 | 只是"理由非空"，与"不猜"是同一件事的两面，**可留** |
| `BattleScene.cs:13672` | `Check(nOne.Tutorial == null, "★ 普通局 ctx.Tutorial == null ⇒ 闸门那一整条链一次都不走")` | 断的是**输入**不是**行为**（没断 `TutorialPermitsForTest(任意)` 为真）。弱，但作者没把它列进"灭自证" ⇒ 只记一句 |

**作者自陈的「灭自证」8 条 —— 逐条核：**

| # | 作者的形态 | 核验 |
|---|---|---|
| ② `RuleEngineTest.cs:18074-18086` 同一句 `DeclareAttackByCurrentType` 两种伤害（40→35 / 40→39） | ✅ **成立**：只改那一格、同一句调用，结果不同（结构上不可能同时满足） |
| ③ `:18097-18099` 旧调用点（省略 `ranged`）仍打近战 1 | ✅ **成立**：把默认值改成"读当前攻击型" ⇒ 变 4 ⇒ 红 |
| ⑤ `:18124-18125` `SideOfUnitType(0) == -1` 而 `(30) == 0` | ✅ **成立**（`None` 与 `PlayerWarlord` 都不是 0 号位以外的东西…… 判别式是"认不出不许默认成 0"） |
| ③″ `BattleScene.cs:13694-13704` `NoTargetYet` 开/关 | ✅ **成立**：同一 `Action`，补上目标并写成"打自己家督军" ⇒ 拒 |
| ⑤′ `:13678-13701` 引擎说 OK + 闸门说不行（AI 那一侧也 OK） | ✅ **成立**：把闸门搬进 `RuleCore.CanAttackNow` ⇒ 两条同时红 |
| ④′ `:13706-13713` 指针该动/不该动 | ✅ **成立**：改成"无条件 +1" ⇒ 红 |
| ⑦ `:13632-13645` `AttackFreeMode` 不再未接 / `ActiveAbility` 仍未接 | ⚠️ **半条成立**：后一半（ActiveAbility）有牙；前一半**没有**（R5）。⇒ 作者标注的"🧨 删掉 `case` ⇒ 红"**是错的** |
| ⑨ `:13732-13750` 终局哈希 + 局面速写 | ✅ **成立**（速写比 `StateHash` 强一档；回放没真跑时 `BeginTutorial` 会把局面重置 ⇒ 速写必红） |

⇒ **8 条里 7 条成立、1 条半条不成立。**

另外两条我要点名（作者没自称"灭自证"，但值得记）：

- `BattleScene.cs:13687-13690`（AI 侧引擎判定那条）在断言里**临时改了 `aiW0.Exhausted` 与 `cW.Active` 再还原**
  —— 批处理里若这一句之后抛异常，夹具会带着被改过的局面往下跑。建议改成不影响局面的问法（或 `try/finally`）。
- `BattleScene.cs:13717` `Check(evtAfter > evtBefore, …)` 用的是 `>`（不是 `==`）⇒ 只要"至少多了一次攻击"就过。
  这一条**故意放宽是对的**（别把玩家那一记也算进来），但**文案读起来像精确计数**。

---

## 7. 必查 6：静默失败

- **真出声的**：`Stub`（`Unhandled` + 每档一次 `LogWarning`）· `Fail`（`Unresolved` + 每理由一次）·
  `Note`（`StageActions` + 每档一次）· `DriveAiTurn` 撞上 `WaitingForActor` 的 `LogWarning` ·
  `RejectByTutorial`（`Debug.Log` + `SpeakCantDo()`）· `PlayReplay` 的逐条轨迹分叉（出声）。
  **"认不出 ⇒ 不执行 + 出声 + 记账"这条纪律我认**（`RunPlayCard`/`RunAttack`/`RunChangeAttackType` 每条失败路都过 `Fail`）。
- 🔴 **一个真口子 = R11**：`ExecuteAction(a == null)` 静默（数据里真有一条 null 就无声无息）。
- ⚠️ **作者 §9·3 那条自陈（`PlayReplay` 的逐条 `DeepHash` 轨迹只出声、不置失败位）—— 我的判定**：
  **不算"静默失败"**（它 `Debug.LogWarning`/`LogError`，出声了），**但它让那条自检口径不闭合** ——
  终局 `StateHash` 相等而轨迹在中途分叉时，两条断言仍全绿。作者加的"局面速写"补了一刀（好），
  但**最硬的判据应该是轨迹**：建议把「轨迹首处分叉」变成**断言失败位**（或至少在 `VerboseTrace` 打开时置红）。
  ⚠️ 顺带：这条自检**跑起来才知道**（我碰不到 Unity）。
- ⚠️ 两点"设计上不出声"我能接受（都写了理由）：闸门 ①③④⑤ 是**查询口**（每帧问，出声会刷屏），
  拒的提示交给 ②⑪ 那两处；`TutorialFrameGuard` 的 `ClearSelection()` 不出声（原版 `:182` 也只是 `LogWarning`）。

---

## 8. 要新开的账（铁律 11：**全部要做，只分先后**）

> 正本归主对话；下面每条给「**做什么 / 判据 / 落点文件**」。

| # | 做什么 | 判据（`文件:行号`） | 落点 |
|---|---|---|---|
| **K1** | 把 `playerAction` 守卫**搬进** `TutorialScript.PlayScriptedTurn`（玩家动作 ⇒ 调表现层钩子 + `return false`，**不 `ExecuteAction`、不 `++`**），调用方那个判据保留（只用来决定"停/放输入"） | 反汇编 `VA 0x180944125-0x180944176`（§1.2）· `AiScripted__ShowPlayerActionAnim.c` | `RuleEngine/Core/TutorialScript.cs:675-684` + `BattleDriver.cs:7164` |
| **K2** | 玩家动作那一支要调 `ShowPlayerActionAnim(manager, action)`（原版还会 `ScreenHighlightPosition.Toggle(manager+0x218, false)`）—— 归 A940 的表现层，但**调用点在执行器里** | `0x1809440f3` / `0x180944171` | `CardPresentation/Battle/TutorialTipPanel.cs`（A940 的新文件）+ `TutorialScript.PlayScriptedTurn` |
| **K3** | `CurrentAttackType` 的**其余 6 个写点**（`AddEffect:480/:626` · **`ActivateMinion:72`** · `OnTurnEnd:217` · `ResolveActiveAbilityPlayed:39` · `UpdateAttackText:26`）—— 它们才是"数值一变就自动重挑一档"的那条链 | `grep -n "+ 0x120" CardScript__*.c` | `RuleEngine/Core/UnitState.cs` + `RuleCore`（重算点）+ `CardScript` 的对应处 |
| **K4** | ⑭ 的第二半：脚本当前那条玩家动作是 `ChangeToRanged/Melee` 时**拦住玩家直接把这一刀打出去**；`CheckIfWaitingToUseAbility` 那半按原版**只出声** | `BattleManager__Update.c:1055-1085`（`:1066`）· `:175-186`（`:182`） | `BattleDriver.cs` 的 `TutorialFrameGuard` / `DoResolve` 入口 |
| **K5** | `PlayCard` 落点照原版取**当时的鼠标位置**（`GetMouseClickPosition` → `GetClosestAvailableSlot`）；**只有批处理/自检**才退化到 `SimpleAI.NextDeploySlot` | `AiScripted__ExecuteAction.c:856-869` | `BattleDriver.cs:2637`（`ResolveDeploySlot` 的装配点） |
| **K6** | `①` 那一处的取法：原版**不判左右**（`targetPosition=0`）⇒ 要么把 `TargetSlot` 传 0（照原版），要么把"我们比原版严"写进注释并说明为什么 | `BattleManager__CanPlayCard.c:56-59` + `ScriptedActionData__CheckIfMatchesActionData.c:34` | `BattleDriver.cs:6528-6536`（`TutAttemptPlay`） |
| **K7** | `replay.tutorialStageIndex` 的兼容要有**判据**（断言老 JSON ⇒ `-1`，或改用 `version`） | `ReplayStore.cs:83` 那段注释 | `ReplayStore.cs` + `BattleScene.cs` ⑨ 节 |
| **K8** | 补断言：`DriveAiTurn` 的"脚本接管 AI 回合"那一支（`Exhausted ⇒ EndTurnAndAdvance` · 回放局 `NotOwned ⇒ 不动` · 撞上 `WaitingForActor` 时出声） | `BattleDriver.cs:7499-7528` | `Editor/BattleScene.cs` ⑧ 节 |
| **K9** | 修「同一句错话的第二份」（铁律 5）：`LastAttackType` ≠ 原版 `currentAttackType` | `BattleScene.cs:9634, 9636-9637` | 就地改 |
| **K10** | 修「§1 那三条错判据」的副本：`BattleDriver.cs:7090-7094` · `BattleScene.cs:13659`（并顺手核 `TutorialScript.cs:510` 那句 `+0x14`） | §1.2 · §1.4 | 就地改 |
| **K11** | `SimulatePlay` 的注释补「不过教程闸门、不推脚本指针」（并考虑给它加一条出声） | `BattleDriver.cs:11230-11246` | 就地改 |
| **K12** | `ExecuteAction(null)` 要出声（或至少进一次记账） | 红线「不许静默失败」 | `TutorialScript.cs` `ExecuteAction` 第二句 |

---

## 9. 没查清

1. 🔴 **`JsonUtility.FromJson` 对"JSON 里没有这个键"的字段是否跑初始化器** —— **本地没查到判据**
   （最接近的 `DeckRulesTest.cs:453-460` 考的是隐式默认 `0`）。**R10 / K7 整条挂在这上面。**
2. **⑧ 节那批断言的实跑结果**（我 ⛔ 不许跑 Unity）：`RuleEngineTest.Run` + `BattleScene.Run`。
   我按静态推演标出两处**可能红**：`BattleScene.cs:13632/13635`（R5，注：这两条**不会**红，但也不会证明什么）；
   真正可能红的是 ⑧ 里依赖"引擎允许督军打督军 / 脚本那一记 `Attack` 打得出"的那几条（`:13717`、`:13724`）。
3. **`0x180851630`（教程 `PlayScriptedTurn` 开头调的那个）**：我只确认了它属于 `ScreenHighlightPosition`
   （`dump.cs:113278` 起那个类），**没逐句读它的方法体**（推测是"关掉高亮"）。
4. **`BattleManager +0x218`** 就是 `ScreenHighlightPosition`（`BattleManager__CheckForTutorialPointer.c:26` 同字段读法一致），
   但**那一格在管理器上的语义**没查。
5. **原版 `PlayCard` 落点链的消费方**（R2 §6·10 也还挂着）—— 只有落点算出来之后谁读它没查。
6. **`_NextTurn` 在**非玩家回合那条支（`*(param_1+0x28) == 0` ⇒ `StartCoroutine(DAT_18428a130)`）
   走的是哪个协程 —— 与本次无关，未查（AI 回合那条由 `_PlayAi` 覆盖，作者那半是对的）。
7. **`CheckIfMatchesActionData` 里 `param_1+0x18 / +0x28`（`RawCardScript` 引用）与我们 `ourId` 的对应关系** ——
   作者用 id 字符串比、原版比 `RawCardScript` 引用/`+0x38` 名字；我核了**分支方向**一致，
   但"`ourId` 就是那个名字"这件事**沿用 R2 的结论，我没独立证**。
