# `B93_DG1` · `RuleEngineTest.Run` 8 条红 —— 逐条定性

> **只读诊断**（不改 `.cs` / 账 / 正本）。判据源 = `d:/2/tools/decomp_full/`（第一权威）· `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/` · `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`（枚举桩）。
> 我们侧 = `Unity/MyGame/Assets/RuleEngine/**`、`CardPresentation/Shell/SaveKeys.cs`。

## 〇 · 复核（自己 `grep "✗"` 过，不是抄简报）

- 日志 `d:/4/_tmp_view/ruleengine.log`：`=== 结束：9825/9833 通过，**8 条失败** ❌ ===`（`:280568`）。
- 同一份日志里有**两趟**完全相同的失败清单（`:139020` 段 + `:280599` 段）⇒ **稳定复现，不是已知间歇**。
- 8 条与简报一致，行号/措辞以上面两段日志为准（下面引用的都是**现读**行号）。

---

## ① `遭遇：P1 第 2 回合 6 点` —— 期望 6 / 实得 5

- **定性档**：**γ 回归**（`B92_B1b` 引入，**已在 HEAD `6c4a111f`**，不是本工作树）
- **证据**：
  - 断言：`RuleEngineTest.cs:4848-4849`（HEAD 里是 `:4798-4799`）
  - 我们实现：`Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs:2766`
    `p.SetSavedMana((ctx.Turn > 1 && ctx.Vars.manaAccumulation > 0 && p.Energy > 0) ? min(Energy, cap) : 0);`
  - 原版（第一权威 · 逐句现读）：
    - `decomp_full/BattleManager._NextTurn_d__395__MoveNext.c:175` —— **先把 `turnCounter(+0x3f8)` 自增并写回**（`CodeStage…ObscuredInt__op_Increment` → 写 `+0x3f8…+0x408`）；
    - 同文件 `:149-158` —— 自增**之前**打的那行日志打的是 `turnCounter + 1`（= 「这次要开第几回合」）⇒ 坐实「自增后 = 新回合号」；
    - 同文件 `:290-293` —— `if (1 < iVar10) BattleManager__UpdateManaLeftAccumulation(...)`，`iVar10` = **自增后**的值；
    - `decomp_full/BattleManager__UpdateManaLeftAccumulation.c:37-39`（cap==0 早退）`:51-57`（`min(cap, GetUnusedMana())` → `SetSavedMana(…,1)`）。
  - 用户实况判据：`Core/GameplayVariables.cs:245-253`（2026-09-26 用户给的 `4 → 7 → 9 → 11`）。
  - **等价换算**：原版闸在「开第 N+1 回合」时看到 `N+1`，`1 < N+1` ⇔ `N ≥ 1`；我们的 `EndTurnCore` 里 `ctx.Turn` = **刚结束**的回合号 `N` ⇒ 等价闸 = `ctx.Turn > 0`（只有 `N = 0` 那种「开局前那次 EndTurn」才该跳过）。
- **该改什么**：改**实现** —— `Core/RuleCore.cs:2766` 把 `ctx.Turn > 1` 改成 `ctx.Turn > 0`（**同一行内改，零行数变化**，铁律 18·b）。
  同时按铁律 5 订正判据文档 `资料/完全复刻/判据/B92_战斗判据_B1.md:151-153`：那两句「只在 `turnCounter > 1` 时才调 ⇒ **第 1 回合结束不再结转**」与「**还差什么：无**」都是**错读**（把自增前的值当成了闸的入参）。
- **反证**：
  - 不是 **α（断言错）**：期望值来自**用户原版实况**，且 `GameplayVariables.cs:245-253` 白纸黑字记着同一条曲线。
  - 不是 **δ（夹具前提）**：夹具走的是真实回合边界（`BeginTurn`→`EndTurn`），不是手写棋盘/野值。
  - 不是「本工作树的新改动」：`git show HEAD:…/RuleCore.cs` 里就已是 `> 1`；本工作树对这一行没动。

## ② `★ ② 遭遇后手 MaxEnergy 曲线 4 → 7 → 9 → 11` —— 实得 `4,6,9,11`

- **定性档**：**γ 回归**（**与 ① 同一个根因**，`B92_B1b`）
- **证据**：
  - 断言：`RuleEngineTest.cs:4904-4906`（HEAD `:4854-4856`）
  - 同一处实现：`Core/RuleCore.cs:2766`
  - 逐格推演（按夹具真实循环 `for (i=0..7) { if (Active==SecondSeat) record; EndTurn; BeginTurn; }`）：
    - `i=0` `EndTurn` 时 `ctx.Turn = 0` ⇒ 闸假 ⇒ 不结转（**这一格本来就该跳过**）；
    - `i=1` `EndTurn` 时 `ctx.Turn = 1` ⇒ **闸假 ⇒ 漏掉后手第一回合那 1 点**（原版此时看到的是 2，闸真）；
    - 之后 `Turn ≥ 2` 闸都为真 ⇒ 曲线变成 `4,6,9,11`；正确应是 `4,7,9,11`。
- **该改什么**：同 ①，一行改完两条一起绿（`> 1` → `> 0`）。
- **反证**：不是夹具的 `EndTurn`-在-`Turn=0` 那一拍本身的问题 —— 那一拍在原版里对应「开第 1 回合」的那次 `_NextTurn`，闸本来就假；改 `> 0` 后它仍被跳过，行为一致。

---

## ③ `★ SaveKeys.VictoryCounters 在 All 里是第 44 项` —— 期望 44 / 实得 43

- **定性档**：**α 断言错**（0 基/1 基混用；断言是本批新增的 `B93_F5`）
- **证据**：
  - 断言：`RuleEngineTest_N1.cs:543-545`（工作树新增，`git show HEAD:` 里**没有**这一行）
  - 我们实现：`CardPresentation/Shell/SaveKeys.cs:244`（`Count = 51`）`:247-299`（`All`，`VictoryCounters` 在 `:292`，注释写着 `// 44`）
  - 原版：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/PlayerDataKey.cs:4-56` —— `undefined = 0` 是哨兵，`ProfileVersion = 1` … `CollectorLevel = 43`、**`VictoryCounters = 44`**；ILSpy `PlayerDataKey.cs:47` 同。
  - `All` 顺序与枚举 `1..51` **逐条对齐**（`All[0] = ProfileVersion` ⇒ **下标 = 枚举值 − 1**）。
- **该改什么**：改**断言** —— `Check(System.Array.IndexOf(...) + 1, 44, "…在 All 里是第 44 项（0 基索引 43）…")`（或直接断 `== 43`）。⛔ 别去动 `SaveKeys.All` 的顺序（它是对的）。
- **反证**：
  - 不是「有人插了新 key 改了序号」：`All` 现读 51 项、与枚举逐条同序；`git show 24b0b132 -- SaveKeys.cs` 只改了注释（4 行增 1 删，全在 `BlockedPlayers` 的文档块）。
  - 不是 β：实现侧 key 常量与顺序都正确。

---

## ④ `牌库顶（末尾，抽牌从末尾抽）那张就是被复制的那张卡` —— 期望 `CopyMe` / 实得 `filler17`

- **定性档**：**α 断言过期**（实现已按 `.c` 改成「随机下标」，断言还是旧口径；实现改动 `B92_B2` 在 HEAD 里）
- **证据**：
  - 断言：`RuleEngineTest.cs:7419-7421`
  - 我们实现：`Core/EffectResolver.cs:4159-4167`（`case "decktop"` 的 `create` 支 → `InsertIntoDeckRandom`）+ `Core/EffectResolver.cs:13182-13189`

    ```csharp
    int origIndex = ps.Deck.Count > 0 ? ctx.GetRandomInt(0, ps.Deck.Count + 1) : 0;
    ps.Deck.Insert(ps.Deck.Count - origIndex, inst);   // 我们的列表「末尾 = 顶」⇒ 镜像下标
    ```

  - 原版（三段实读）：
    - `decomp_full/BattleManager._ResolveCreateDeckCard_d__513__MoveNext.c:111` —— `AddCardInPlayToDeck(bm, card, 0, 0)`，第 3 个实参（`toTop`）= **字面 0**；
    - `decomp_full/BattleManager__AddCardInPlayToDeck.c:37` —— 牌库非空时 `GetRandomInt(0, Count + 1)`；
    - 同文件 `:49-52` —— `iVar9 = 0; if (param_3 == '\0') iVar9 = 随机值;` ⇒ `toTop == 0` ⇒ **随机下标 Insert**。
    - ⇒ 卡面写 `at the top of your deck`、代码根本没实现 = **原版自己的 bug**；按铁律 18·0 照抄。
  - 夹具为什么看到 `filler17`：牌库里是 filler + 被复制那张，随机下标落到了别处，`Deck[Count-1]` 自然不是 `CopyMe`。
- **该改什么**：改**断言** —— 位置判据换成「牌库里有一张 `CopyMe`」（`CheckTrue(Deck.Exists(x => x.Card.Name == "CopyMe"), …)`）。
  ⚠️ 想真钉住「随机」而不是「置顶」，靠位置**钉不住**（单种子下两者都是确定结果）⇒ 加一条**随机游标**判据：`InsertIntoDeckRandom` 每张各调一次 `ctx.GetRandomInt` ⇒ 断 `ctx.RandomNumbersIndex` 的消耗量（+1）。断言文案里写清「原版卡面写 top、代码随机插，照抄」。
- **反证**：
  - 不是 β：`.c` 三处实读都指向「随机」；也**没有**「放哪一格」的动作字段（`createAtPos` / `moveToPos` 只喂演出坐标）。
  - 不是 γ（本批）：那条实现改动在 `git show HEAD:…EffectResolver.cs:4159-4165` 里就有（`B92_B2`），**红的是断言没跟着改**。

---

## ⑤ `★ sacrifice 一次都不触发 —— 它和「真死」那一支互斥` —— 期望 0 / 实得 1

- **定性档**：**γ 本批回归**（`B93_F7`：工作树新文件 `Core/RuleCorePartial_B93F7.cs` + `Core/RuleCore.cs:10109` 那半句调用）
- **证据**：
  - 断言：`RuleEngineTest_S14.cs:929-932`（方法 `TestS14SacrificeNotTriggeredOnRealDeath` @ `:910`）
  - 我们实现：`Core/RuleCore.cs:10109`
    `if (leftPlay) { SweepExtrinsicEffectsOnSourceDeath(ctx, u); TriggerSacrificeOnDeath(ctx, u, p, slot); }`
    · `Core/RuleCorePartial_B93F7.cs` 的 `TriggerSacrificeOnDeath`（闸 = `u.Has(Sacrifice)` ∧ `ctx.Active == owner`）
  - 原版：
    - `decomp_full/CardScript__ResolveDeadCard.c:242`（`targetCard == this` 才进这一支）`:286-292`（`HasCurrentTrait(0x1d6 sacrifice)` ∧ `IsPlayerTurn == card.isPlayer` → `TriggerSacrifice`）；
    - **入口闸** = `decomp_full/BattleManagerSupport__BroadcastDeadUnit.c:82-90` —— 遍历 `unitsInPlay`，**每张都必须先过 `CardScript__IsInPlay`**，过了才在 `:147` 调 `ResolveDeadCard`；
    - `decomp_full/CardScript__IsInPlay.c:5-9` —— 只认状态 **2 / 15 / 3 / 17**；
    - `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardStateOptions.cs:8` —— `waitingToDie = 5`，而真死前 `CardScript__CheckIfDead` 把状态置成 `waitingToDie`（ILSpy `out_cpp2il_dev_ilspy/CardScript.cs:15300`）。
    ⇒ **一张「真的死了」的卡（状态 5）走不到 `ResolveDeadCard`**，那一支对它**不可达**；它能被走到的形态只有「**仍在场（2/15/3/17）、且是本次 action 的 `targetCard`**」的观察者。
  - 池内现状：`cards_engine.json` 1129 张里 **带 `sacrifice` 的 = 0 张**（`keywords` / `cardTraits` 现扫）⇒ 这一整族**今天生产不可观测**，红的只有自检。
- **该改什么**：改**实现（撤回那一跳）** —— 去掉 `Core/RuleCore.cs:10109` 里的 `TriggerSacrificeOnDeath(ctx, u, p, slot);`（保留 `SweepExtrinsicEffectsOnSourceDeath`），并把 `TriggerSacrificeOnDeath` 记成「**口已建、暂无正确调用点**」进 `资料/完全复刻/待接线与排队项.md`，理由写全上面的入口闸证据（`IsInPlay` 只认 2/15/3/17 vs 真死 = 5）。
  ⛔ **不要**为了让它「可达」而去改 `CleanupDeaths` 的位次或给它加闸 —— 原版这一段本来就不该由「死者自己」跑。
- **反证**：
  - 不是 **α**：同一实现下 `#22`（敌方回合不触发）仍绿 ⇒ 两道闸本身写对了；红的是**挂错了位置**（真死这一支）。
  - 不是 **δ**：「本方回合 + 无 survivor + 真死」正是 `CardScript__CheckIfDead.c:146-175`（幸存者支）之后的真死那一支，期望 0 正确。
  - 也不是「F7 的做法只是另一种读法」：`ResolveDeadCard.c:286` 确实存在（F7 没编），但**它的宿主不是死者**（见上面的 `IsInPlay` 闸）。F7 自己在文件头把这条标成「可达性存疑」，本诊断给出结论：**对「真死」这一形态不可达**。

---

## ⑥ `🧨 TurnStart(30) 双方都发 —— 本夹具只有一张 30 卡 ⇒ 恰好 1 次` —— 期望 1 / 实得 0

- **定性档**：**δ 夹具前提不成立**（+ 一条 β 级结构差异，见「该改什么」后半）
- **证据**：
  - 断言：`RuleEngineTest_EngineF2.cs:71`（夹具卡 `:36-39`，前提硬检查 `:47-53`，入口 `:16`，已接线 `RuleEngineTest.cs:1152` `Step(TestF2EffectResolverGaps)`）
  - 我们实现：`Core/EffectResolver.cs:18545-18552`（`EmitTurnStartBothPasses`，两遍 `EmitAtTurnTriggerPass`，第 2 遍 `ownerIsActiveOnly: true`）· `:8631` 起的 pass · 发射体 `:8554`（`EmitTurnTriggerNamed`）/ `:8564`（`EmitTurnTriggerRaw`）
  - **真正的拦路点**：`public static void ResolveAtTurn`（`:8701`）的作业表**只由卡面正文解析产生** ——
    `:8781` `foreach (var cl in EffectText.AtTurnClauses(u.Card.Desc))`，`:8826` `if (total == 0) return;`
    夹具三张卡的 `desc` 都是空串：`new CardDef("F2T30", "F2T30", "unit", "", null, "Test", 1, 1, 5, 0, abilityTriggers: new[]{30}, …)`（第 4 个位置实参 = `desc`，见 `Core/CardDef.cs:839`）
    ⇒ `unitJobs` / `handJobs` / `persistJobs` 全空 ⇒ **连 `EmitAtTurnTriggers` 都没进**，30 一条都不会发。
  - 原版（F2 的判据本身没问题）：`decomp_full/CardScript__OnTurnStart.c:141-146` —— **无条件** `OnTrigger(0x1e = 30)`；`:147-152` `if (param_2 != *(card+0x40)) return;` 后才 `OnTrigger(0x1f = 31)`；广播入口 `BattleManagerSupport__BroadcastTurnStart.c:20-31`（遍历 `allCardsInPlay`，`IsInPlay` 即调 `OnTurnStart`）。
- **该改什么**：
  1. **改夹具** —— 三张卡都给一段**能解析成 `each` 视角 at-turn 子句**的正文（例：`"At the start of each turn, deal 1 damage to a random enemy"`），`abilityTriggers` 仍 `{30}` / `{31}`。
     ⚠️ **必须用 `each`，不能用 `your turn`**：`:8784` 对 `view != "each" && pi != active` 直接 `continue` ⇒ 非行动方那张（`u31p1`）压根不进作业表，第 75 行那条会变成**假绿**（前提不成立时的静默跳过，正是铁律里点过的坑）。
  2. **另开一条 β 账**（不是本批 8 条红的一部分）：原版广播**不看正文**，我们整条 at-turn 发射都挂在正文解析上 ⇒ 池内 `trigger 31` 的 **BL35 / TAU3 / UM88**（`cards_truth.json` 实读：`abilities[trigger=31]` = `buff untilEndOfTurn + trait 390(flying)`）与 **`Pray Enchantment`（无 desc）** 永远收不到 `TurnStartPlayer` 事件。
     今天**不影响效果**（`Flying during your turn` 走 `Core/Aura.cs:922` 的自指光环 + `RuleCore.cs:2062-2063` 的按回合亮灭），只影响**表现层事件**。
- **反证**：不是 α（`:71/:73` 的期望与 `OnTurnStart.c:141-152` 逐句一致）；不是 β（`EmitTurnStartBothPasses` 本体两遍 + 31 的 owner 闸都对，它**根本没被调用到**）。

## ⑦ `🧨 行动方(P0) 的 31 卡收到一次 TurnStartPlayer` —— 期望 1 / 实得 0

- **定性档**：**δ 夹具前提不成立**（**与 ⑥ 同一根因**，同一个夹具、同一张作业表为空）
- **证据**：断言 `RuleEngineTest_EngineF2.cs:73`；`ResolveAtTurn` `:8781` / `:8826`（同上）；`EmitTurnStartBothPasses` `Core/EffectResolver.cs:18545`
- **该改什么**：同 ⑥ 第 1 条（夹具 desc）。改完这三条（`:71` / `:73` / `:75`）才真正考到「30 双方发 / 31 只发 owner==active」。
- **反证**：不是 β —— 31 的 owner 闸（`Core/EffectResolver.cs:8637` `ownerIsActiveOnly && owner != active ⇒ return null`）照着 `.c:147-152` 写对了；它没被跑到。

---

## ⑧ `前提③：它上场了` —— 期望 True / 实得 False

- **定性档**：**δ 夹具前提不成立**（读错了格号）
- **证据**：
  - 断言：`RuleEngineTest.cs:39517-39518`
    `var played = Board(ctx, 0, 1); CheckTrue(played != null, "前提③：它上场了" + LogTail(ctx));`
  - 我们实现：`Core/RuleCore.cs:3900` `PlayCard(ctx, p, handIdx, slot, out landedSlot, …)` → `PlayCardCore` 里 `slot = BoardSlots.Insert(ps, unit, slot)`；
    `Core/BoardSlots.cs:74` `SlotOf(side,index) = WarlordSlot + side*(index+1)`（`WarlordSlot = 4`、`Left = -1`）；
    `Core/BoardSlots.cs:196-199`（`Resolve`）—— `index = IndexOf(requestedSlot); if (index > count) index = count;`
    ⇒ 请求 `slot 1`（左侧 index 2）+ 该侧 0 人 ⇒ 夹到 index 0 ⇒ 落 **slot 3**（贴督军那格）。
  - **日志实据（夹具自己印的）**：`… P1 部署 B93G1Eph（1 费，1/3）**到槽 3**，能量剩 1`（那一行来自 `Core/RuleCore.cs:4075`）⇒ 反驳「没上场」。
  - 对照：同一测试的「前提①」用的是夹具直写 `Place(ctxCtl, 0, 1, eph)`（`RuleEngineTest.cs:2683` 直接写 `Board[slot]`，绕过 `Insert`）⇒ 所以 slot 1 在前提①能成立、在 `PlayCard` 这条真入口不成立。
- **该改什么**：改**夹具** —— 用 out 参数读真实落点：
  `CheckCode(RuleCore.PlayCard(ctx, 0, hi, 1, out int landed), RuleCodes.OK, …); var played = Board(ctx, 0, landed);`
  （或直接 `Board(ctx, 0, BoardSlots.SlotOf(BoardSlots.Left, 0))` = slot 3）。
  ⛔ **别**去改 `Insert` / `Resolve` 让它「请求哪格就落哪格」—— 连续无洞棋盘模型就是这么定的（`CLAUDE.md`「棋盘是连续无洞模型」那节）。
- **反证**：不是 β —— 部署确实发生了（日志有部署行、`CheckCode == OK`、能量扣到 1）；不是 α —— 断言文案没错，错的是它读的那一格。

---

## 汇总（α 2 条 / β 0 / γ 3 / δ 3）

| # | 条目 | 档 | 根因 | 改哪边 |
| --- | --- | --- | --- | --- |
| ① | 遭遇 P1 第 2 回合 6 点 | **γ** | `B92_B1b` 的 `ctx.Turn > 1` 差一档 | 实现 `RuleCore.cs:2766`（`>1`→`>0`） |
| ② | 遭遇后手曲线 4,7,9,11 | **γ** | 同 ① | 同 ① |
| ③ | `VictoryCounters` 第 44 项 | **α** | `Array.IndexOf` 0 基 vs 枚举 1 基 | 断言 `RuleEngineTest_N1.cs:543` |
| ④ | 复制品在牌库顶 | **α** | 断言按旧口径；实现已按 `.c` 改随机插 | 断言 `RuleEngineTest.cs:7420-7421` |
| ⑤ | `sacrifice` 真死时触发 | **γ** | `B93_F7` 把「目标卡在场上」那一支挂到了「死者」上 | 实现 `RuleCore.cs:10109`（去掉那半句） |
| ⑥ | `TurnStart(30)` 发 0 次 | **δ** | 夹具 `desc=""` ⇒ 作业表为空 ⇒ 整段没进 | 夹具 `RuleEngineTest_EngineF2.cs:36-39` |
| ⑦ | `TurnStartPlayer(31)` 发 0 次 | **δ** | 同 ⑥ | 同 ⑥ |
| ⑧ | 前提③ 它上场了 | **δ** | 连续棋盘把请求格 1 夹到 3 | 夹具 `RuleEngineTest.cs:39517` |

**新账（不属于这 8 条，但要立）**：at-turn 发射整体挂在正文解析上，与 `BroadcastTurnStart` 的「遍历在场上所有卡」不同 ⇒ 池内 `BL35 / TAU3 / UM88 / Pray Enchantment` 永远不收到 `TurnStartPlayer`（今天只影响表现层事件）。

## 修复顺序建议

1. **① + ②（一行）** —— `RuleCore.cs:2766` `ctx.Turn > 1` → `ctx.Turn > 0`。**唯一有用户实况数据直接背书的硬缺陷**，改完两条一起绿；顺手按铁律 5 订正 `B92_战斗判据_B1.md:151-153` 的错口径。
2. **⑤（删一句）** —— 去掉 `RuleCore.cs:10109` 的 `TriggerSacrificeOnDeath(...)` 调用 + 立「口已建、无正确调用点」账。池内 0 张带该 trait ⇒ 零生产影响，风险最低。
3. **③（改一行断言）** —— `+1` 或改成 43。
4. **⑧（改夹具一行）** —— 用 `out int landed` 读真实落点。
5. **④（改断言，需要新判据）** —— 位置判据换「存在 CopyMe」+ 补一条「随机游标 +1」（单靠位置钉不住「随机 vs 置顶」）。
6. **⑥ + ⑦（改夹具 desc，最麻烦）** —— 三张卡都要造出可解析的 `each` 视角子句，且不能破坏「各自的正文结算不出副作用」；同批把 β 新账立了。

> 顺序理由：①② 是**唯一**由「原版实况数」判死的实现错；⑤ 是**本批**引入、改动最小、零生产影响；③⑧ 是纯夹具/断言的机械错；④ 需要新写判据；⑥⑦ 要设计正文，放最后。

## 没查清

1. **`ResolveDeadCard` 那条 `sacrifice` 分支的真正可达域**：我按 `BroadcastDeadUnit.c:82-90` 的 `IsInPlay` 闸 + `waitingToDie = 5` 判定「真死那张卡不可达」，能推到的**唯一**可达形态是「**仍在场上（2/15/3/17）、且是本次 action 的 `targetCard`**、但**没死**」的观察者。
   ⚠️ **没有跑原版实况验证**（池内 0 张带该 trait ⇒ 真局里观察不到）。若哪天要做对这一支，落点应是「`CleanupDeaths` 时遍历**在场上**的观察者 + 与 `action.targetCard` 比对」—— 而我们这一路**没有 `action.targetCard` 这个输入**（`CleanupDeaths(ctx, p, slot, deathType)`），得先查调用方能不能带过来。**这一步没查**。
2. **`CardScript__OnTurnStart.c:131-136` 还有一道闸**：`IsUnit && EnoughPendingDamageToDie ⇒ return`（待死单位**不跑** 30/31）。我们的 at-turn 发射里**没有**这一格。今天大概不影响（`ResolveAtTurn` 只在回合边界跑，那会儿没有「回合中途的待死结算」），但**没核**它是否等价 —— 和 ⑥⑦ 那批一起改发射闸时要一起看。
3. **B92_B2 的注释有一处与 `.c` 对不上**（不影响本次 8 条）：`EffectResolver.cs:4162-4163` 写「另外三处 `decktop`（选牌动作 / 回手回牌库 / 墓地取出）`toTop` 位是**非零**」，而 `decomp_full/BattleManager._ResolveRecallToDeck_d__543__MoveNext.c:328` 实读第 3 个实参是 `*(undefined1 *)(param_1 + 0x117)` —— **动作自带的动态位**，可 0 可 1。⇒ 那三处「确定性置顶」里可能有一处该是随机。**没逐处查完**。
4. **流程疑点（留给调度台）**：第九十二会话的收尾写着「五道闸全绿 … Unity 20 宿主全绿」，而 `RuleEngineTest.Run` 现读 8 条红（且 ①⑥⑦ 等至少 3 条的根因在**该会话自己的提交**里）。要么那次跑的 `RuleEngineTest.Run` 早于 `B92_B1b` / `B92_B2` 落地，要么「全绿」是按别的口径判的（例如只看断言合计/退出码）。**我没有去翻那次的日志**，只记在这里。
