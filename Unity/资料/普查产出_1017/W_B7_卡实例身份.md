# W_B7 · 卡实例身份 —— `SetupCardInHand` 那一套

> 2026-10-17 · 写手代理（引擎侧）· **未跑 Unity**（只跑秒级类型检查）。
> 账目原文 = `资料/卡实例身份_爆炸半径.md` §五 第 6 条（原引 `EffectResolver.cs:3464`，行号已漂到 `:3802`）。
> 验收靶 = `EffectResolver.ResolveDeploy` 注释里那句「手牌里那些等着被 `SetupCardInHand` 挂效果的**还没做**」+ `TAU54 Dynamic Offensive`。

## ① 原版判据（逐条 `文件:行号`；全部逐句读过方法体）

**A. `SetupCardInHand` 何时调、挂什么、挂在哪** —— `d:/2/tools/decomp_full/PlayerHand__SetupCardInHand.c`
- `:19-23` 传 null 直接 `CustomDebug.LogError`（不静默）。
- `:24-47` **界面那半边**：`ShowBoardObjects` / 设排序层 / `SetParent` 到手牌锚点（`+0x88`）/ `ShowCardBack`。
- `:38-77` **效果那半边**：遍历 `activeEffects`（`PlayerHand +0x48`），逐条
  `FilterMethods.CheckIfMeetsCriteria(词, card, card, handEffect.targetCriteria, 0)`（`:58-59`）
  → 命中且 `CardScript.AlreadyContainsEffect` 为假（`:65`）→ **`CardScript.AddEffect(card, cardEffect)`（`:71`）**。
  ⇒ 效果存在**那张牌自己的** `List<CardEffect>`（`CardScript +0x108`）。
- **调用点四个**（都是「牌进入手牌那一刻」）：`PlayerHand._AddDrawnCardToHand_d__37__MoveNext.c` ·
  `…_AddCardNotDrawnToHand_d__39__MoveNext.c` · `…_AddCardNotDrawnToHandAtIndex_d__40__MoveNext.c` ·
  `…_AddActiveTrapToHand_d__38__MoveNext.c`。
- 托管桩：`Warpforge_code/Scripts/Assembly-CSharp/PlayerHand.cs:519 activeEffects` · `:647/:651/:655/:659/:667/:671/:675`
  那七个方法；`HandEffect.cs:6-14` = `{CardEffect; PlayersAffected; TargetCriteria; bool limitedUses; int numberOfUses;}`；
  `CardEffect.cs:103 isHandBuff`。

**B. 怎么存 / 怎么摘 / 离手与打出怎么处理**
- `PlayerHand__AddHandEffect.c`：`:89` 打 `isHandBuff = 1`；`:90-110` 收进 `activeEffects`；
  **`:126-135` 立刻遍历 `currentHand`（`+0x50` = `List<CardScript>`），对过筛的每一张牌
  `CardScript.AddEffect(那张牌, cardEffect)`** ⇒ **逐张牌挂**，不是记一条全局修正。
  `PlayerHand__CheckEffectsOnNewCard.c:26-43` 同形（新牌进来时按同一条 criteria 挂）。
- 摘：`PlayerHand__RemoveHandEffectAt.c:30-40` 遍历 `currentHand` **逐张** `RemoveEffect`；
  `PlayerHand__RemoveEffectsFromCard.c:88-105` 反向由一张牌找回它头上那条 `HandEffect`。
- 🎯 **打出时**：`PlayerHand__RemoveCardFromHand.c:20-21` **只是把这个 `CardScript` 从 `currentHand` 里
  `Remove`**（不销毁、不重建）⇒ **手上那张牌打到场上还是同一个对象**，效果**跟着上场**
  （旁证：棋盘两侧 minion 也是 `List<CardScript>`；我们自己的 `Core/BoardSlots.cs:4` 抄自 `dump.cs:39326`）。
  次数上限由 `PlayerHand__CardPlayedWithEffects.c:28-45` 核销（`limitedUses` ⇒ `numberOfUses--`，到 0 摘）。
- 过期：`PlayerHand__UpdateCardEffects.c:119-166`（按 `BattleManager.IsPlayerTurn` 分档）· `:332` 摘。

**C. 代词槽「一条能力一覆盖」**（本件顺带修的那条）—— `AbilityLogic__SetPreviousAbilityTargets.c:17-30`
每条能力**先 `Clear()` 再 `AddRange()`**，调用点 `AbilityLogic__PlayAbility.c:2745`
⇒ 原版**没有**「槽里留着上上一次的」这回事。

## ② 改了什么（只动白名单内两个文件）

**`Core/EffectResolver.cs`**（`git diff --numstat` = 284/25）
1. `ResolveOps(List<EffectOp>)` 入口加 `ctx.LastTargets.Clear()`（在 `DrawnThisResolve.Clear()` 旁）——
   按 C 的原版判据，代词槽按「一条能力」清零。**只清一批那个，不动 `LastTarget`（一个）**。
   ⚠️ **本件唯一有回归面的一处**：影响 `PlayTactic` / `RepeatTacticOnAdjacent` / `RuleCore.FlushDeathWatches`。
   已核：全仓测试**没有**断言 `LastTargets`（只有 `LastTarget`，没动）。
2. 新增 `HandReferents(ctx, owner)` = `DrawnThisResolve ∩ 手牌`（本次结算进手牌、**且还在手里**的那几份）。
   `BattleContext.cs` 不在白名单 ⇒ **不另开槽**，复用既有的 `DrawnThisResolve`。
3. 新增 `AttachEffectToHandInstances(...)`：载荷**逐份**登记进 `ctx.HandBuffs`
   （`HandBuff { Instance, Ops, Source }`）；兑现点仍是既有 `RuleCore.ApplyHandBuffs`（**没动 RuleCore**）。
   同份+同源+同载荷不重挂（对应原版 `AlreadyContainsEffect`）。
4. `DoGive`：`targets.Count == 0` 且 spec 是 `prev` ⇒ 改走 `HandReferents`；挂上了就不报「没有合法目标，空过」。
5. `ConditionHolds` 的 `targethaskw`：场上那批（`ts`）为空时**改判手牌那几份** —— 复用同一份
   `asKw/asKind/kindWord/sideOf`，关键词读 `CardDef.Has`、兵种走同一个 `CreatePool.MatchesKind`、阵营按「在谁手里」。
6. **就地订正两处陈旧注释**（铁律 5）：`BroadcastCostWhen` 的「① 按卡 id 匹配」——**代码 2026-09-18
   起已是实例级**（`Key="*"` + `HandInstanceId`），注释是旧的；`ResolveDeploy` 的「手牌…**还没做**」
   —— 改成「判据已读、机制已落地一半；仍没做的是把手牌那两跳接上」。

**`Editor/RuleEngineTest.cs`**（+121 行）：新 `TestHandInstanceEffects()`，已注册进 `Run()`
（`Section("卡实例身份 · 手牌效果（原版 `PlayerHand.SetupCardInHand` 那一套）")`）。

## ③ 断言（每条盯什么）

| # | 断言 | 盯什么 |
|---|---|---|
| 1 | `Crisis Battlesuit` 的 `Subtype=="Battlesuit"` ∧ 本来没 `flank` | 夹具前提 |
| 2 | 打 `Dynamic Offensive` 返回 `OK` | 真卡打得出去（`_tmp_view/tactic_unparsed.txt` 实测战术卡 445/445 完全解析） |
| 3 | 两份战斗服都抽上手 | `DoDrawType` |
| 4 | `HandBuffs` **恰好 2 条**、且 `ReferenceEquals` 命中那两份 | **投递到实例**（不是模板） |
| 5 | **★ 判别式**：同一 `CardDef` 的**第三份**（还在牌库里）**没挂到** | 「按 `CardDef.Id` 记一条全局修正」在这条上**必红** |
| 6 | **★ 判别式（结算侧）**：打出第三份 ⇒ **没** `Flank` ∧ 照常 `Exhausted` | 同上；顺带盯「别多给」 |
| 7 | 打出**被指的那一份** ⇒ 有 `Flank` ∧ 不疲劳 | 端到端（打出时兑现） |
| 8 | 打第三份后 `HandBuffs` 仍 2 条、打被指那份后剩 1 条 | 兑现即摘、不误摘 |
| 9 | 混编（1 战斗服 + 1 普通部队）⇒ `HandBuffs.Count == 0` | `If they are Battlesuits` 读作「**都**符合」 |
| 10 | **★** 同时断言日志含 `判的是**手牌里那` | 区分「判过了=不成立」与「判不了」（后者也满足 9 —— 只断 9 就是同义反复） |

判别式 = **5 / 6 / 10**。

## ④ 没查清 / 没做的（如实）

1. **只做到「打出时兑现」，没做到「在手里就生效」**。原版那条 `CardEffect` 挂在 `CardScript +0x108`，
   在手里那一段**就能被读**（改费/改数值/算关键词）；我们只在 `RuleCore.ApplyHandBuffs` 那一刻跑。
   对 `give Flank` / `give +1` 两类**等价**（原版也落进 `CardScript` 字段），
   但「手里就有运行时关键词」这个**查询口我们没有消费者** —— 不假装有。
2. **过期 / 次数上限没做**：`UpdateCardEffects`（回合末摘）与 `CardPlayedWithEffects`（`limitedUses`）
   两条原版语义**未实现**；我们的 `HandBuff` 登记后不过期（**与既有 `GrantHandBuff` 同一条近似**）。
3. **「广播到**手牌**」那两跳没做**：`BattleManagerSupport__BroadcastUnitSummoned.c:53/:65` 仍未接 ——
   「我手里那张牌在听 `OtherUnitSummoned`」还不会响；`资料/常驻效果_数据与设计.md` §六 那张表仍列着它。
4. **`GOF50 Tide of Muscle`（`Draw two troops and give them +1`）**：投递通道这次通了，但载荷是**裸 `+1`**
   （图标在文本导出里丢了）⇒ 语义在文本层不可恢复（CLAUDE.md §七）。**它仍打不出效果，原因与本件无关。**
5. **`CardInstance.cs` 文件头整段过期**（写着「第 2/3/4 步还没做」），而 `卡实例身份_爆炸半径.md` §八
   记着 2026-09-18 五步全做完。**该文件不在白名单 ⇒ 没改**，请主对话按 §八 订正。
6. **`HandReferents` 的兜底判据是「本次结算抽到手」**：全卡池只 3 张卡同时出现 `draw` 与 `them`
   （`TAU54` / `GOF50` / `AM63`，后者那句是 `deploy`），无「先抽牌、再用 `them` 指场上单位」的卡 ⇒ 不误伤。
   **但这不是原版那种通用代词槽**；通用解要在 `BattleContext` 上加「手牌实例代词槽」，那要动白名单外的文件。

## ⑤ 类型检查（原样贴）

```
$ TMPDIR=/tmp/wf_b7 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

行尾：两份 `.cs` 改后仍是**纯 CRLF**（`CRLF == LF` 计数相等）；`git diff --numstat` = `284/25` · `254/1`
（后者基线本来就带别人的 1 处删除）。⛔ 没跑 Unity、没动 git、没改正本。
