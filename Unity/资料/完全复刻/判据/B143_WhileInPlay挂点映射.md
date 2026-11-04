# B143 · while-in-play 广播挂点映射表（原版 6 处 ↔ 我们）

> 只读代理 `B143-R1` · 2026-11-05 · 消费者 = `B143-W7`（把 `BumpWhileInPlayAccum` 从 `RecomposeCore` 挪出来）
> ⛔ 未改代码/账。判据源：`d:/2/tools/decomp_full/` + `Unity/MyGame/Assets/RuleEngine/`（现读）。

## 〇 · 结论（给 W7 的一句话）

原版 `BroadcastWhileInPlay` 共 **6 处**；我们有 **5 处能对上**（其中 **1 处要补**：`DoTakeControl` 现在**根本没有** `Auras.Recompose`），
另有 **10 处 `Auras.Recompose` 是「棋盘写入/能力结算/教程」——它们不该 bump**。
⇒ **bump 只挂 6 个点**：`ActionPump.cs:143`（动作边界）· `RuleCore.cs:2121`（回合开始）· `EffectResolver.cs:17418`（exchange）·
`EffectResolver.cs:15858`（reanimate）· `TransformUnit_B94B12.cs:274`（transform）· **`EffectResolver.cs:9711`（steal，新增）**。
⚠️ 动作边界那一处还有一个**时序缺口**：`RefreshInPlayNumericAndKeywords` 是 `numericOnly`，**不重登记费用档** ⇒ 降费类（Aun'Shi/Piranha）会在「没有棋盘变化的动作」上滞后一步，见 §五·1。

---

## 一 · 原版 6 处（`BattleManagerSupport__BroadcastWhileInPlay` 的调用点）

**函数语义**（`decomp_full/BattleManagerSupport__BroadcastWhileInPlay.c:44-118`）：
① 遍历**全场** `unitsInPlay(+0x470)`，`IsInPlay ∧ HasWhileInPlayAbility` ⇒ `CardScript.UpdateWhileInPlay`；
② 再遍历**当前回合方手牌**同两跳；③ 再遍历**另一方手牌**同两跳，但**跳过 `param_3` / `param_4` 两张指定的牌**
（`:97/:103` 的 `op_Inequality(x, param_3/param_4)`）。
⇒ 6 个调用点**都对全场 + 双方手牌广播**，后 4 个只是「手牌那半排除刚单独更新过的那 1~2 张」。

| # | 文件:行 | 所在方法 | 语境 | 实参（`bm, bool, excl1, excl2`） |
|---|---|---|---|---|
| ① | `BattleManager__FinishResolvingAction.c:91` | `BattleManager.FinishResolvingAction` | **动作收尾**；**闸 = 四个动作队列计数之和 < 1**（`:56-57` 把 `+0x360/+0x368/+0x358/+0x350` 的 `Count` 相加后判 `< 1`） | `(bm,0,0,0,0)` 全量 |
| ② | `BattleManager._NextTurn_d__395__MoveNext.c:404` | `<NextTurn>d__395.MoveNext`（换回合协程） | **回合开始**（同段前面 `:399/:402` 先对**两个督军**各 `UpdateWhileInPlay`） | `(bm,0,0,0,0)` 全量 |
| ③ | `BattleManager._ResolveExchangeMinion_d__552__MoveNext.c:344` | exchange-minion 动作协程 | **换人动作**（先 `:332/:337` 对换过来的两张各 `UpdateWhileInPlay`） | `(bm,0,card0,card1)` 手牌排除这两张 |
| ④ | `BattleManager._ResolveStealMinion_d__551__MoveNext.c:122` | steal-minion 动作协程 | **抢人动作**（`:119` 先 `ResetSummonSickness` + `:121` `UpdateWhileInPlay(抢来的)`） | `(bm,0,stolen,0)` 手牌排除抢来的那张 |
| ⑤ | `BattleManager__ResolveTransformUnit.c:1726` | `BattleManager.ResolveTransformUnit` | **变形**（结算体尾部） | `(bm,0,transformed,0)` |
| ⑥ | `BattleManager.__c__DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:23` | `ResolveTransformFromRemnant` 的完成回调 | **残骸翻面（回生）**：`ResetToValuesInHand → BroadcastUnitReanimated → SetAsUnitInPlay → … → UpdateWhileInPlay → CheckStun → UpdateFigures → 本行` | `(bm,0,revived,0)` |

⚠️ ③④⑤⑥ 是**额外**的一次广播（同一动作收尾时 ① 若队列已空还会再广播一次）—— 所以原版在这些动作上**会 bump 两次**（先是特殊事件、后是动作收尾）。映射时**不要合并成一次**。

---

## 二 · 我们侧：`Auras.Recompose` 的 15 处活跃调用点 + 动作边界

> 「活跃」= 非注释行；另有动作边界那一处走的是 `Auras.RefreshInPlayNumericAndKeywords`（它内部也调 `RecomposeCore`）。

| # | 文件:行 | 所在方法 | 语境 | 分类 |
|---|---|---|---|---|
| 0 | **`Core/ActionPump.cs:143`** | `ActionScope.Dispose`（最外层动作收尾） | **动作边界**：`WhileInPlayVariablePass → TriggerOncePass → EmitWhileInPlayHandPass → Pump → RefreshInPlayNumericAndKeywords` | **动作边界（= 原版①）** |
| 1 | `Core/RuleCore.cs:2121` | `RuleCore.BeginTurnTail` | **回合开始**（改完棋盘后重算） | **回合开始（= 原版②）** |
| 2 | `Core/EffectResolver.cs:17418` | `EffectResolver.DoExchange` | 两侧棋盘都变了（交换单位） | **特殊事件（= 原版③）** |
| 3 | `Core/TransformUnit_B94B12.cs:274` | `TransformUnit_B94B12.TransformUnit` | 变形（棋盘写入之后） | **特殊事件（= 原版⑤）** |
| 4 | `Core/EffectResolver.cs:15858` | `EffectResolver.DoReanimate` | 残骸翻面（`ps.Board[slot]=back` 之后） | **特殊事件（= 原版⑥）** |
| — | **（无）** | `EffectResolver.DoTakeControl`（`:9604-9713`） | 抢单位：`BoardSlots.RemoveAt` + `SetOwnerSeat`，**全文没有 `Auras.Recompose`** | **原版④ 的挂点缺失** |
| 5 | `Core/EffectResolver.cs:6853` | `EffectResolver.DoReturn` | 回手（`BoardSlots.RemoveAt`） | 棋盘写入 —— 不该 bump |
| 6 | `Core/EffectResolver.cs:9332` | `EffectResolver.SpiritAbilityDeferred` | 灵魂石能力结算（棋盘可能变） | 能力结算 —— 不该 bump |
| 7 | `Core/EffectResolver.cs:9429` | `EffectResolver.ResolveOathAbility` | 誓约能力结算（`paid && !IsOver`） | 能力结算 —— 不该 bump |
| 8 | `Core/EffectResolver.cs:9515` | `EffectResolver.FaithAbilityDeferred` | 信仰能力结算 | 能力结算 —— 不该 bump |
| 9 | `Core/RuleCore.cs:4143` | `RuleCore.PlayCardCore` | 出牌部署（棋盘写入） | 棋盘写入 —— 不该 bump |
| 10 | `Core/RuleCore.cs:4744` | `RuleCore.SwarmMergeNow` | 虫群合并（棋盘写入） | 棋盘写入 —— 不该 bump |
| 11 | `Core/RuleCore.cs:4955` | `RuleCore.DeployFreeCore` | 免费部署（棋盘写入） | 棋盘写入 —— 不该 bump |
| 12 | `Core/RuleCore.cs:9971` | `RuleCore.CleanupDeaths` | 死亡清理（棋盘写入） | 棋盘写入 —— 不该 bump |
| 13 | `Core/RuleCore.cs:10435` | `RuleCore.MinionDeathBookkeepingDeferred` | 死亡记账（棋盘写入） | 棋盘写入 —— 不该 bump |
| 14 | `Core/RuleCore.cs:12343` | `RuleCore.UseAlternativeCore` | 替代能力结算（棋盘写入） | 能力结算 —— 不该 bump |
| 15 | `Core/TutorialScript.cs:1567` | `TutorialScript.PlaceTroops` | 教程摆兵（棋盘写入） | 教程 —— 不该 bump |

> 现读计数：`Auras.Recompose(` **活跃 15 处**（`EffectResolver` 6 · `RuleCore` 7 · `TransformUnit_B94B12` 1 · `TutorialScript` 1），
> 与 `Aura.cs` 注释自述的「12 处棋盘写入点 + 3 处兜底」同量级（不逐字相等，别拿注释当计数）。

---

## 三 · 一对一映射

| 原版 | 我们 | 说明 |
|---|---|---|
| ① `FinishResolvingAction:91`（闸 = 队列全空） | **`ActionPump.cs:143`** `ActionScope.Dispose` | ✅ 语义对得上：`ActionScope.Dispose` 只对**最外层动作**跑一次（`ActionPump.cs:93` 的 `_outermost`），= 原版「四个队列都空」那条闸。**但当前这里只跑 `numericOnly` 刷新**（费用档不重登记，见 §五·1）。 |
| ② `_NextTurn:404` | **`RuleCore.cs:2121`** `BeginTurnTail` | ✅ 一对一同位。 |
| ③ `_ResolveExchangeMinion:344` | **`EffectResolver.cs:17418`** `DoExchange` | ✅；原版手牌排除两张，我们的模型没有「手牌排除」这个概念（全量重算），语义等价。 |
| ④ `_ResolveStealMinion:122` | **无** → `EffectResolver.cs:9711`（新增） | 🔴 **映射不上（我们有缺口）**：`DoTakeControl` 现读**没有任何 `Auras.Recompose`** ⇒ 抢来的单位身上的光环/关键词要等下一次动作边界（且那是 numeric）才刷新。 |
| ⑤ `ResolveTransformUnit:1726` | **`TransformUnit_B94B12.cs:274`** | ✅。 |
| ⑥ `ResolveTransformFromRemnant b__0:23` | **`EffectResolver.cs:15858`** `DoReanimate` | ✅。 |
| — | 其余 **10 处**（§二 #5–#15） | **原版没有对应**：棋盘写入/能力结算/教程的普通重算 ⇒ **不该 bump**（原版那些位置不广播）。 |

---

## 四 · W7 把 6 个 bump 放哪（逐行，尽量零行数变化）

> 建议先在 `Aura.cs` 加一个**公开的窄口**（把现有 `BumpWhileInPlayAccum` 暴露出来即可，不改语义）：
> `public static void NoteWhileInPlayBroadcast(BattleContext ctx) => BumpWhileInPlayAccum(ctx);`
> 然后把 `RecomposeCore` 第一句的 `BumpWhileInPlayAccum(ctx);`（`Aura.cs:656`）**删掉/移走**。

| # | 文件:行 | 现在这一行长什么样 | 改成 |
|---|---|---|---|
| 1 | `ActionPump.cs:143` | `RuleCore.WhileInPlayVariablePass(_ctx); … PumpActionQueues(_ctx); Auras.RefreshInPlayNumericAndKeywords(_ctx); /* … */` | 在 `Auras.RefreshInPlayNumericAndKeywords(_ctx);` **之前**同物理行插 `Auras.NoteWhileInPlayBroadcast(_ctx);`（零行数变化；⛔ 别拆行——本行有账落点） |
| 2 | `RuleCore.cs:2121` | `            Auras.Recompose(ctx);` | `            Auras.NoteWhileInPlayBroadcast(ctx); Auras.Recompose(ctx);` |
| 3 | `EffectResolver.cs:17418` | `            Auras.Recompose(ctx);` | 同 #2（前置） |
| 4 | `EffectResolver.cs:15858` | `                    Auras.Recompose(ctx); BroadcastWhen(ctx, WhenEventKind.Reanimated, …); …` | 在 `Auras.Recompose` 前插 `Auras.NoteWhileInPlayBroadcast(ctx); ` |
| 5 | `TransformUnit_B94B12.cs:274` | `            Auras.Recompose(ctx);                                  // 棋盘变动 ⇒ 光环重算（9 个写入点同一纪律）` | 同 #2 |
| 6 | **`EffectResolver.cs:9711`**（steal，新增） | `            ctx.Log($"{by}：「{op.Source}」抢到 {n} 个单位");` | 改为 `            Auras.NoteWhileInPlayBroadcast(ctx); Auras.Recompose(ctx); ctx.Log($"{by}：「{op.Source}」抢到 {n} 个单位");`（零行数变化） |

**放置顺序的判据**：`Note` 必须**紧挨在**「本点对应的重算/刷新之前」——
因为原版是 `BroadcastWhileInPlay → UpdateWhileInPlay → AddEffect`，计数发生在**重算之前**；
我们 `WhileInPlayAccumCount` 是在 `RecomposeCore` **读**的，若先重算后 bump 会用到旧值（首趟 1、次趟 2……整体慢一拍）。

⚠️ 第 6 处是**新增行为**（`DoTakeControl` 以前不重算）—— 它会改 `play check` 指纹（抢人那几张卡）；W7 本批已经要重钉基线，**仍须在报告里单独归因**。
（若不想在本批加 Reanimate/Steal 的**状态**刷新，可只加 `NoteWhileInPlayBroadcast` 不加 `Recompose`；但那样「抢来的单位身上的光环」仍滞后 —— 与原版不等价，建议一并补。）

---

## 五 · 两个必须写进 W7 报告的口径

1. **动作边界只做 `numericOnly` ⇒ 降费类会滞后一步。**
   `RefreshInPlayNumericAndKeywords`（`Aura.cs:1088`）走 `RecomposeCore(numericOnly:true)`，②「费用登记」被 `!numericOnly` 跳过
   （`Aura.cs:666-667`）⇒ bump 之后，**费用档不会按新倍数重登记**；要等下一次**全量** `Recompose`（某个棋盘写入）才更新。
   原版 `BroadcastWhileInPlay` 是「板 + 手牌全量重跑」（`AddEffect` 立即合并）⇒ **每次动作边界就该看到 −1**。
   ⇒ 建议二选一（交调度台/W7 裁）：
   - ① 边界处 bump 后**额外**调一个窄口重登记费用（`RefreshCostModsOnly`；现无此 API，要新开）；
   - ② 接受滞后，并在账里写明「Aun'Shi/Piranha 的降费在无棋盘变化的动作上慢一拍」——**这是与原版可观测的差异**，不能静默。
2. **`DoTakeControl` 现在没有 `Recompose`**（§三）：原版 ④ 是「先对抢来的那张 `UpdateWhileInPlay`，再全场广播（手牌排除它）」；
   我们连一次重算都没有。修法见 §四#6；若不做，W7 的「6 处映射」实际只有 5 处落地。

---

## 六 · 复核边界（搜过哪些）

- 原版：`BattleManagerSupport__BroadcastWhileInPlay.c` 全文 · `BattleManager__FinishResolvingAction.c:30-95` ·
  `_NextTurn_d__395:385-415` · `_ResolveExchangeMinion_d__552:325-355` · `_ResolveStealMinion_d__551:105-135` ·
  `ResolveTransformUnit:1710-1740` · `ResolveTransformFromRemnant_b__0.c:1-35`；
  另核了 `grep -rl BroadcastWhileInPlay` 的**全部** 7 个文件（含函数自身），确认调用点只有这 6 处。
- 我方：`Auras.Recompose(` 活跃 15 处逐个取「文件:行 + 所在方法 + 上下文」（用脚本回扫最近的方法签名）；
  `DoTakeControl`（`EffectResolver.cs:9604-9713`）逐行确认**无** `Recompose`；
  `ActionPump.cs:75-160`（`ActionScope.Dispose` 的 `_outermost` 闸与整行代码）；`Aura.cs:653-710 / 850-930 / 1060-1090`。
- 未读：`PlayerHand.UpdateCardEffects` 的内部（只影响手牌表现，不影响挂点选择）；`_NextTurn` 前面对两个督军的
  `UpdateWhileInPlay` 是否需要单独 bump（原版那是「对**两张卡**单独更新」，不是广播；本表按「广播点」算，故未列）。
