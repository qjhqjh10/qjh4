# B121-WB 执行报告 · 拖牌 / 帧率 / AI（2026-11-03）

> 执行写手 `B121-WB`。改到的文件（全在白名单内）：`Board/BoardLayout.cs` · `Hand/CardInteraction.cs` ·
> `Battle/Card3DAnimationController.cs` · `Editor/B120_W10_Assert.cs`（只改 Q07）· 新建 `Editor/B121_WB_Assert.cs` ·
> 新建 `RuleEngine/Editor/RuleEngineTest_B121WB.cs` · 账补丁 `账/_parts/B121_WB.tsv`。
> ⛔ 没碰 `BattleDriver.cs` / `Editor/*Scene.cs` / 账主表 / `活状态表.md` / 正本 / `Scenes/` / `ProjectSettings/` · ⛔ 没跑 Unity / 没动 git。

## §〇 一页结论

| 条 | 结论 | 落点 | 需要调度台做的 |
|---|---|---|---|
| **Q04c3** 删两个自定容差 | ✅ **改完**（字段删净、2D 档改回「候选集 + 2D 欧氏 + 无容差」，`allowNearest` 两取同解） | `BoardLayout.cs:504-540` | 🔴 **改 4 份既有夹具的期望**（见 §五·1，都不在本件白名单） |
| **Q04c6** `maxHeightOfUnplayableCard` | ✅ **改完**（25.0 原版世界单位 × 桥 = 3.4340；「拖太高 + 打不出去 ⇒ 收回手牌」） | `CardInteraction.cs:2609/2615`，调用点 `:1084` | 无 |
| **Q04c4b** 提交后拖回撤销 | ⛔ **没做**（结构性，卡在 `BattleDriver` + `DeploySequence`，见 §四） | — | 🔴 派 `BattleDriver` 的写手（前提在 §四） |
| **Q07** 帧率默认档 | ✅ **改完**（删掉 `fps <= 0 ⇒ 60` 钳制，照抄原版 −1） | `Card3DAnimationController.cs:414` | 同步点首次 `BattleScene.Run` 时看 §五·3 |
| **E07** AI 难度端到端 | ✅ **改完**（真开局 + LiveOps 夹具；FTUE 三格「不建死字段」机检） | `RuleEngine/Editor/RuleEngineTest_B121WB.cs` | 🔴 **接线**（见 §三） |

---

## §一 `B120-Q04c3` · 删两个自定容差

**判据（现读复核）**：`cpp2il/out_il2csharp/Assembly-CSharp/MinionManager.cs:321-390`（`GetClosestAvailableSlot`：
候选 = 每侧「最内空格 + 内圈已占格」· **2D 欧氏** · `bestIdx < 0` 只在「一个候选都没有」时出现 ⇒ **无容差**）·
互验 `decomp_full/MinionManager__GetClosestAvailableSlot.c:35-145`。

| | 改前 | 改后 |
|---|---|---|
| 字段 | `snapTolerance = 0.036f` · `snapToleranceY = 0.09f`（🔶 自设计） | **两个字段已删**（原 8 行就地换成 8 行说明 ⇒ `:174/:372` 两个账落点**不漂**） |
| 2D 兜底档 | 先过 y 带（`snapToleranceY`）、再取 `Abs(dx)`、最后 `best <= snapTolerance` | `ArenaSlots.IsCandidate` 收窄候选 → **2D 欧氏**（`sqrt(dx²+dy²)`）→ **无容差**；`false` 只在**候选集为空**时出现 |
| `allowNearest` | true/false 两档判据不同 | **两取同解**（签名保留只为调用点兼容；断言 A5 钉住） |

**产品路径零变化**：`Release` / `UpdateDrag` 早在 `B120-W24` 就传 `allowNearest:true`（本来就无容差）⇒
这次动的只是 `ExplainDrop` / `DropRejectKindForTest` / `CardBaseDemo` 那批**探针与夹具**的语义。
**行数守恒**：`BoardLayout.cs` 699 行（改前 699）· 方法体 23 行 → 23 行。

---

## §二 `B120-Q04c6` · `maxHeightOfUnplayableCard`（拖太高就放弃）

**回源确认了值 = 25.0（不是 3.0）**：
- `工具/read_varsglobal.py` 2026-11-03 现读 `maxHeightOfUnplayableCard = 25.0` —— 这是**序列化资产**
  （`Warpforge_Data/sharedassets0.assets`）的值，运行时优先；
- `VarsGlobal__.cctor.c` 里 `+0x80 = 0x40400000` = **3.0** 只是 ctor 默认值，**会被反序列化覆盖**
  ⇒ 上一手账里记「25.0」是对的，记 3.0 会差 8.3 倍。

**判据（亲读）**：`cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:25788-25822`（`case 2`，还按着）：
```
if (UIstate == movingHandCard) {                        // = 还没提交
  if (mouse.y <= HandLimitArea.y)                                       { …卡跟手… }
  else if (mouse.y <= pointerCursorStart.y + maxHeightOfUnplayableCard) { …卡跟手… }
  else if (!CanPlayCard(cardTracked, showTips:1, true, 0)) { MoveBackToHand(); … }
}
```
机器码同形：`decomp_full/BattleManager__Update.c:291-303`（`+0x80`）。

**落地**：`CardInteraction.MaxPlayHeightWorld = 25 × 桥(0.13736) = 3.4340 我们的单位 ≈ 370.9 px`
（`:2609`）· `TryGiveUpTooHigh`（`:2615`）挂在提交判定之后（同一物理行 `:1084`，**行数零变化**）：
`_dragging != null` ∧ `!dropOk`（= 引擎已判「打不出去」）∧ 指针在出牌区里 ∧ 高出按下点 > 3.4340 ⇒ `AbortDrag`
（= `MoveBackToHand` + `HandCardReleased` 的等价物，⛔ 不播 cantdo —— 原版这一支没有提示）。

**顺手查实（写进代码注释）**：原版那条「太高 + **能打** ⇒ 卡不再跟手」的子支**不可达** —— 同一帧里出牌闸问
`CanPlayCard(showTips:0)`、放弃闸问 `CanPlayCard(showTips:1)`，**同一个谓词、只差提示开关** ⇒ 能打的话上面早就提交了。
⇒ 唯一可观测行为 = 「太高 + 打不出去 ⇒ 收回手牌」，本件按此落地（⛔ 没有造「停跟手」）。

---

## §三 `B120-E07` · AI 难度端到端

判据/做法 = `报告/B120_W8_执行.md` §一/§二/§三/§七·5（它当时如实记着「端到端没验」）。本件补的正是那一条。

| 段 | 内容 |
|---|---|
| A | `SimpleAI.BotTierFtue` 的成员集：`WinCount` / `Knobs` 在；**`waitTime` / `justForRanked` / `shouldUseFirstGameLogic` 一个都没建**（反射，⛔ 死字段） |
| B | **真开局** + `GameModesHandler` 夹具（`{"Classic":{"lossStreak":5}}`）⇒ `driver.aiDifficulty ≡ SelectNormal(RankedData.Points 夹 int, 5)`，且 **≠ 兜底 `Normal`**；换夹具（连败 0）⇒ 难度**跟着换**（灭自证） |
| C | FTUE 那一支：`playMode: OfflinePractice` + `{"ResultCounter":{"Practice":{"Won":3}}}` ⇒ `≡ SelectFtue(3)`（先在断言里钉 `IsInFTUEState` 的排除集与 `≤ max` 前提） |

**接线**：入口 `RuleEngineTest_B121WB.Run(BattleDriver driver)`。建议接在 `Editor/BattleScene.cs` 聚合行**靠后**、
`B120_W8_Assert.Run();` 之后（同一件事的相邻两件）：
```csharp
RuleEngineTest_B121WB.Run(driver); pass += RuleEngineTest_B121WB.Pass; fail += RuleEngineTest_B121WB.Fail;
```
⚠️ 本件会：① 往 `LiveOpsManager` 注册表塞一个夹具 `GameModesHandler`（`finally` 里把原来那份注册回去）；
② **重开一局**（`driver.Begin(... playMode: Classic / OfflinePractice)`）⇒ 别接在依赖「当前局面」的断言之间。

---

## §四 `B120-Q04c4b` · 「提交后拖回线下撤销」—— ⛔ **没做，卡在哪写清楚**

**原版判据（亲读，非二手）**：`cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:25991-25998`（`case 3` = `playingMinion`）——
**还按着**（`sub_180617800(...) != null`）且指针掉回线以下（`mouse.y <= HandLimitArea.y`）⇒
`cardTracked.OutFromPlayingAreaAnim(); RemoveSwarm(0); UIstate = movingHandCard; ReassembleMinions(0);`
⇒ 卡**回到手牌拖拽态**、`UIstate` 退回 2（同一形状在 `case 4` / `case 5` 各一份）。
`OutFromPlayingAreaAnim` 的体 = `CardScript__OutFromPlayingAreaAnim.c:11-42`（`MoveBackToHand` 那一对动作 +
`localScale = GetCardInHandMovingScale()`（只 `targeted` 支）+ `cardState = 9` + `PlayerHand.LightPlayableHand()`）。

**为什么我们这边落不了（4 条前提，缺一不可）**：
1. **提交即不可逆**：我们的提交 = `CardInteraction.Release` → `DeploySequence.Play`（单位卡 1.3 s）⇒ 引擎真的出牌
   发生在序列末尾 `onDone → OnCardDeployed → DoPlay`。要在飞行**中途**撤销，得能**杀掉那条 DOTween 序列**
   并**撤掉已排程的动作** —— 会动的文件是 `Board/DeploySequence.cs`（不在本件白名单）。
2. **冻结闸挡住撤销窗口**：`OnPlayerDeployFlightStart` 会 `BeginBlockingWindow(_clock + duration + margin)`
   （`BattleDriver.cs:13823`），而 `interaction.Frozen = () => PlayerInputFrozen || WaitingForBlockingAction`（`:5782`）
   ⇒ 整段飞行里 `CardInteraction.Update` 在 `BlockedThisFrame()` 就 return，**根本轮不到撤销判定**
   （原版没有这个冻结：`case 3` 是逐帧跑的）。要撤得改 `BattleDriver` 的开闸时机。
3. **0.95 s 之后卡已经不是「手里的那张」**：`DeploySequence.Handoff.Apply()`（`:266/:306` 附近）把卡
   `SetParent(boardRoot)` + `SetFace(Board)` + `SetLayer(ArenaLayer)` ⇒ 撤销还要一条「换回手牌卡面」的逆操作。
4. **已响过的副作用**：`_playerFlightTakeoffPlayed` / `animateFeel` 那条 `ScheduleAction(MoveTime, StartHandToBoardClip)`
   都在 `BattleDriver` 里 ⇒ 撤销后要一并清。

**结论**：这条链必须由 `BattleDriver`（+ `DeploySequence`）的写手做，形状建议 = 新增
`BattleDriver.CancelDeployFlight(CardView)`：杀序列 → 撤 blocking window → 撤已排程 clip → 对 `card` 调
`CardInteraction.ApplyOutFromPlayingArea`（**已存在**，`B120-W28` 落的）→ `_placed.Remove(land)` → `_cards` 插回原位 →
`Relayout(true)`；`CardInteraction` 侧只需一个「已提交 + 仍按着 + 指针回线下」的轮询口（约 30 行，本件没写 —— 写了也接不上）。

---

## §五 受影响面（⛔ 都不在本件白名单，请调度台分流）

1. **Q04c3 删容差 ⇒ 4 份既有夹具的期望要改**（产品行为不变，只有探针/夹具语义变了）：
   - `Editor/BattleScene.cs:19115-19121`：`DropRejectKindForTest` 对「错侧/空白点」的期望；
   - `Editor/HandAssertE8.cs:568-590`（5 处）：2D 档现在**任意点都给最近候选格** ⇒ 「解不出格」只在候选集空时出现；
   - `Editor/B120_W24_Assert.cs:154`：注释里拿 `snapTolerance 0.036` 当「远离最近格」的尺子（字段已不存在）；
   - `Editor/B120_W16_Assert.cs` A 组：前提断言「落点解析到第 4 格」**现在必红** —— 两侧皆空（`-1,-1`）时候选只有 `{3,5}`、
     督军位恒非候选 ⇒ 应改成 `TryResolveSlot(drop, out s, 3, 3)`（每侧已占 3 ⇒ 8 格全候选）或改判 `s == 3`。
2. **Q07 删钳制 ⇒** `Editor/B117_G10_11_Assert.cs:203-208` 括号里「批处理 `targetFrameRate = -1` ⇒ 裕量按 60 fps 算」
   **事实已变**（C3 仍会绿，只是理由文案过期）。
3. **Q07 首次 `BattleScene.Run` 要重点看**这几条（它们按「第一帧还没收尾」写的）：`BattleScene.cs:13480-13482`
   （`clipCb && !Enabled && PlayingClipCount == 0 && FinishedCount == 1`）、`:13743-13747`（`PlayedCount >= 1`）。
   −1 档下 ≤1 s 的 clip **第一次 `Update` 就收尾** ⇒ 依赖「先演一会儿」的时序断言可能提前满足或翻面。
4. **`check_ledger_points`**：收工现读 **漂移 0 / 失败 0**（A 5876 通过 5876）。
   途中曾出现「37 条漂移 / 7 条失败」，全部落在 `Shell/CollectionWindow.cs`（整体 +1 行）——那是**别人正在改那个文件**，
   与本件无关（`grep BoardLayout|CardInteraction|Card3DAnimationController|B121` 在那份明细里 **0 命中**），稍后自愈。

---

## §六 验证

| 项 | 结果 |
|---|---|
| `typecheck.sh`（`TMPDIR=/tmp/wf_wb`） | **运行时 0 / 0**（本轮最后一次跑，`CardDatabase.cs` 已被它的写手修好）· **编辑器 9 条，全部在同批另一写手的 `Assets/CardPresentation/Editor/B121_C2_Assert.cs` 里**（`bad` 重复局部变量 / `PlayerDeck` 与 `int` 比较 / `LayoutSpace.FromPixelX` 不存在 / `DeckInfoPopup.EditDeckLabelTextForTest` 不存在）—— **与本件无关**；我的两个新编辑器文件在那次编译里**0 错** |
| 交叉验证（编辑器侧，更强的一次） | 用最小 rsp 只编我的两个新文件 + 引用**这一轮新编出来的** `WFCheck.dll` ⇒ **0 错误** |
| `check_ledger_points.py` | **A 5876 条 · 通过 5876 · 漂移 0 · 失败 0**（B 档只报告：8 条，与本件无关）。`--fix` ⛔ 没跑 |
| 行尾 | `CardInteraction.cs` 保持 CRLF（2628/2628）· 其余 LF · **裸 CR 0**；三处改动 `git diff` 只动目标行 |
| 断言 | `Editor/B121_WB_Assert.cs` **A 组 12 条 / B 组 9 条 / C 组 4 条**（含前提 4 条；未接线）· `RuleEngine/Editor/RuleEngineTest_B121WB.cs` **A 2 / B 6 / C 6**（含前提 4 条；未接线）· `B120_W10_Assert.cs` B7/B7′ 已按新语义重写 |
| Unity | ⛔ 没跑（红线）；断言**一条都没执行过** |

---

## §七 顺手发现（交给调度台分流）

1. 🔴 **`Editor/B120_W16_Assert.cs` A 组的前提断言「落点解析到第 4 格」现在是红的** —— `B120-W21` 改候选集之后
   （两侧皆空 ⇒ 只有 `{3,5}`，督军位恒非候选）这条就站不住了，`B120_W21` 的 §五 没列到它。见 §五·1 最后一条。
2. **`Card3DAnimationController` 的账落点已经漂了**：`特效动画.tsv:164-173` 引用 `Card3DAnimationController.cs:507/532/538/464/322`
   等行号，而**文件只有 514 行**（`:532/:538` 越界）⇒ 那不是本件造成的（本件行数守恒 514→514），建议重钉基线。
3. **`VarsGlobal` 的 ctor 默认值不能当判据**：`maxHeightOfUnplayableCard` 的 ctor 是 3.0、序列化资产是 25.0 ——
   同类（`cardInHandMovingScale` 1.0 vs 1.1、`timeToDrawPlayerCard` 1.0 vs 0.3）在 `资料/VarsGlobal_原版数值.md` 里**全部是序列化值**。
   以后谁要读 `VarsGlobal` 的数值，**一律用 `工具/read_varsglobal.py`**，⛔ 别读 `.cctor.c`。
4. **`decomp_full` 的 `BattleManager__Update.c:291-303` 与 `minMovementToPlayCard` 的 `:250-253` 写法一模一样**
   （都是 `pointerCursorStart.y + 全局字段 < mouse.y`）⇒ 两个字段（`+0x7C` 0.3 / `+0x80` 25.0）很容易互相误认，已在两处代码注释里互相点名。
