# `B116_R6` · `B-211`「`cardAbilities` 逐 ability 派发」施工面

> 判据时点 2026-11-02 · 只读 · 未改代码
> 只读代理 `B116_R6`｜唯一产出 = 本文件。⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改任何 `.cs` / `.json` / 账 / 活状态表。
> 行号全部**本次现读**（`cards_engine.json` v47 · 1,129 卡 · `triggerAbilityIndices` 1,622 行）。
> 数据现读复核：`trigger 0` = **936 行 / 587 卡**（tactic 748 行 · unit 131 · defence 55 · enchantment 2）·
> `ca ∈ {320,325,360,380,480}` 落 `trigger 0` = **37 行 / 34 卡**（320×15 · 325×1 · 360×18 · 380×3）。

## 〇 一句话结论

原版 `trigger 0`（`AbilityTrigger.ThisCardPlayed`）的派发形状 = **遍历 `cardAbilities(+0x290)` 逐条 `CanTriggerAbility` → 逐条 `PlayAbility`**；
我们只有「解析 `desc` 成一条扁平 op 流」这一条路 ⇒ **缺的不是某个 `ca` 的执行器，而是「一行 ability 一份结算」这一层**。
⇒ 施工面 = 在**两处出牌点**各挂一次**卡级**派发行（不是 `UnitState` 级：战术卡没有 `UnitState`，这是 `B-255` 结构上接不上的真因），
并用**一条纯函数闸**决定「这一行归 ability 层还是归文本层」。**首刀只切 `ca 380`（3 行 / 3 卡）**，其余 `ca` 分在后续步，⛔ 不要一把全开。

---

## A 派发点（`文件:行号` | 时机 | 谁来调）

| # | 文件:行号 | 时机（相对正文 op） | 谁来调 | 备注 |
|---|---|---|---|---|
| A1 | `RuleCore.cs:4222`（该物理行**第 1 句** `FireTriggerOnBoard(ctx, unit, KeywordTable.Rally, chosen)`） | **正文入队之后**（同一行第 2~4 句是 600/590/`ApplyPlayedCardTailEffects`）。正文本身也是**入队**，出队才结算 | `PlayCardCore`（`:4038`）→ `FireTriggerOnBoard`（`:13196`）→ `FireTriggerAt`（`:12705`）→ `u.FxOps("rally")`（`:12717`）→ `EnqueueAbility`（`:12897`）→ `FireTriggerDeferred`（`:12851` 的 `body`）→ `ResolveOps`（`:12933`） | 单位卡的「打出时」正文 = `_triggerOps["rally"]`；`rally → ThisCardPlayed(0)` 的换算在 `ActionPump.cs:1099` |
| A2 | `EffectResolver.cs:2802-2805`（`EnqueueAbility(ctx, ActionPriority.Instant, "tactic-body:" + card.Name, …)`） | **正文入队之后**；正文实体 = `PlayTacticBodyDeferred`（`:2815`）里的 `ResolveOps(… abilityFamilyTrigger: "ThisCardPlayed")`（**现读 `:2835`**，账里写的 `:2832` 已漂 3 行） | `PlayTactic`（`:2579`）→ `PlayTacticCore`（`:2596`）→ 入队 `:2802` | 战术 / 防御卡无 `UnitState` ⇒ 必须是卡级（`CardDef` + `CardInstance`）入口 |
| A3 | ⛔ **不要碰** `EffectResolver.cs:2917-2928` | 战术卡 `320` 的**表现拍**（`FollowUpSchedulerOf(card)=="ThisCardPlayed"` ⇒ `EmitFollowUpResolution`），硬编码 18 张表 | `PlayTacticBodyDeferred` 尾部 | 这是 `B-255` 排他闸的**让路方**（见 B5 / C4），不是新增点 |
| A4 | ⛔ **不要碰** `EffectResolver.cs:2971` | 突触把同一批 `ops` 在**相邻单位**身上重跑一遍 | `RepeatTacticOnAdjacent`（`:2946`） | 原版那一跳是 `OnCardPlayedWithTargetJustThisCardPlayed`（相邻单位**自己**的 trigger 0），不是本卡的 ability 行 ⇒ 本层**不得**在突触里重跑（否则 AM54 的 380 会被写 1+N 次） |
| A5 | ⛔ **不要碰** `RuleCore.cs:4213` / `EffectResolver.cs:2733` | `trigger 0` 的**附魔载体窄口**（`Enchantments.OnCarrierPlayed`） | 两条出牌点各一句 | 现读只覆盖 85 模板 / 135 行（见 `Enchantment.cs:1190`）；它与本层是**并列**的两条派发，⛔ 不要合并 |
| A6 | 参考（本轮**不**动） | `FireTriggerByNumberWithSink`（`:21067`）→ `FireTriggerRowsByChosenAbility`（`:21385`） | 生产调用只有 `60/185/187/210/410` | 它要 `UnitState` ⇒ **战术路结构上永远到不了**；本轮新派发器写成卡级后，**它也应该转调新派发器**（避免两份 `TriggerRowKindOf`） |

---

## B 不双发判据（条件 | 走 ability 层 | 走文本层 | 判据出处）

**唯一判定函数**（新成员，纯函数、可单测）：`AbilityLayerOwnsRow(CardDef card, TriggerAbilityIndexSpec row, out string why)`。

| # | 条件 | 走 ability 层 | 走文本层 | 判据出处 |
|---|---|---|---|---|
| B1 | `card.TriggerAbilityRowsOf(0) == null`（旧表 `< v42`／列未知） | ⛔ 不进 | ✅ 照旧 | `CardDef.cs:9250-9266`（三态 `null` = 未知，⛔ 别合并成「没有」） |
| B2 | `rows.Length == 0`（明确没有这条 trigger） | ⛔ 不进 | ✅ 照旧 | 同上（`[]` = 明确没有） |
| B3 | `row.Ca == -1`（`lg == -1` / 缺键 / 手写夹具） | ⛔ 不进（**改前行为**） | ✅ 照旧 | `EffectResolver.cs:21357-21359`；现池触发 0 上 **1 行** = `GOF97`（`ab0/lg-1`） |
| B4 | `row.Ca ∈ {320, 325, 360, 380, 480}`（**本体是 meta-op，卡面文本结构上表达不出来**） | ✅ 本层拥有 | ⛔ 不得再跑同一行（文本层也没有对应 op） | `EffectResolver.cs:21361-21362`（现状把它们归 `OwnLane` = `continue`）+ 原版 5 支：`:2128`(380)/`:2244`(320)/`:1994`(360)/`:2373`(325)/`:1960`(480) |
| B5 | `ca == 320` ∧ 卡是战术/防御（`FollowUpSchedulerOf` 硬编码表命中） | ✅ 本层派 `320`（执行体 = **现成的** `EmitFollowUpResolution`） | 硬编码块**让路**（`EffectResolver.cs:2917` 加让路条件） | 原版 `:2244` + `:2473-2475 AddFollowAfterResolution` → `CardScript__FollowUpResolutionPlayed.c:5-9`（`OnTrigger(0x19a=410)`） |
| B6 | `ca` 为其余真效果种类（`0/10/20/30/…`） | ⛔ 不进（**这正是今天的 `BodyOp` 出声支，不动**） | ✅ 文本层负责 | `EffectResolver.cs:21363` + 头注释 ②（`:21326-21329`） |
| B7 | **双保险**：本层真要跑这一行，但触发桶里存在 `op.AbilityIndex == row.Ab` 的 op（= 文本已覆盖该 ability） | ⛔ **跳过 + 出声**（不静默） | ✅ 文本已在跑 | `MapTriggerOpsByNumber` 按 `op.AbilityIndex == row.Ab` 分桶（`CardDatabasePartial_B98H7.cs:207-212`）；现池触发 0 上**桶非空只 14 张、桶空 0 张、其余 573 张连表都没挂**（`B115_S2` 探针）⇒ 本闸今天对 37 行**恒不命中**，是给将来数据兜底 |
| B8 | **每行一道 `CanTriggerAbility`**（原版逐条判，不是整块 AND） | 用 `ActionPump.AbilityCriteriaAllowsAt`（`:2372`）；`true` ⇒ 跑、`false` ⇒ 跳过、`null` ⇒ **判不了 ⇒ 不跑 + 出声**（保守） | 不受影响 | 原版 `RawCardScript__TriggerAbilities.c:201-215`（循环内逐条 `CanTriggerAbility`）→ `CardAbility__CanTriggerAbility.c:71`（先比 trigger）；现池 37 行**全部**落在有 `abilityTargetCriteria` 行的卡上（现读 37/37） |
| B9 | 单位卡（A1） | 同上，`actingCandidate` / `thisCard` **都有** ⇒ 三态闸可判 | —— | —— |

**为什么 B5 不会双发（现读实证）**：硬编码表 18 张里 **17 张**在数据上确有 `ca 320`；**`ASH73` 只有 `ca 325`** ⇒
让路条件必须写成「**数据里确有 320 行** ∧ 本层真的派了它」才算让路，否则**保留硬编码那一拍**（否则 ASH73 从 1 拍变 0 拍）。

---

## C 施工顺序（步 | 改哪个文件哪一处 | 验收方式 | 回滚点）

| 步 | 改哪里 | 内容 | 验收方式 | 回滚点 |
|---|---|---|---|---|
| **C1** | **新文件** `RuleEngine/Core/CardAbilityDispatch_B116.cs`（`public static partial class RuleCore`；类名必须 == 文件名） | ① `public static bool AbilityLayerOwnsRow(CardDef, TriggerAbilityIndexSpec, out string why)`（B 表唯一判定）；② `internal static int DispatchCardAbilityRows(BattleContext ctx, CardDef card, CardInstance inst, int trigger, int owner, int slot, UnitState actingCard, UnitState targetCard)`：取 `card.TriggerAbilityRowsOf(trigger)` 逐行 → 过 B1..B8 → `ca` 分派（**首刀只接 `380` 与 `320`**，`325/360` 出声不跑）；③ 逐行发**带 `abilityIndex`** 的 `EvtKind.Trigger`（`EmitCardTrigger(..., abilityIndex: row.Ab, ...)`，`:8488`，原版是**逐 ability 一条动画**）；④ 把 `FireTriggerRowsByChosenAbility`（`:21385`）改成转调它（保留 `UnitState` 版签名） | `typecheck 0/0` + 纯函数单测（`AbilityLayerOwnsRow` 用 6 组夹具覆盖 B1..B6） | 删新文件即回滚（零调用点） |
| **C2** | `RuleCore.cs:4222`（**紧接该行之后，新起一行**） | `RuleCore.DispatchCardAbilityRows(ctx, card, inst, 0, p, slot, actingCard: unit, targetCard: chosen);` | 见 D1/D2 | 删这 1 行 |
| **C3** | `EffectResolver.cs:2805`（`EnqueueAbility(...tactic-body...)` **之后，新起一行**） | 第二个 `EnqueueAbility`（道 = `Instant`，label `"cardax:"+card.Name`），体内调 `DispatchCardAbilityRows(ctx, card, inst, 0, p, targetSlot, actingCard: chosen, targetCard: chosen)`。**必须是独立队列动作、排在正文之后** | 见 D1/D3 | 删这 1 行 |
| **C4** | `EffectResolver.cs:2917` 的 `if (FollowUpSchedulerOf(card) == "ThisCardPlayed")` | 改成 `&& !RuleCore.AbilityLayerOwns320Row(card)`（B5 让路条件；三态：数据 `null` ⇒ **不让路**） | D4（每张卡只 1 拍） | 把条件还原成原句（单行） |
| **C5** | 数据面（`ca 380` 目标集） | 确认 `SetReferenceCards`（`:1366`，已收 `actingInst`）与 `ResolveTargets` 的 `ta=140/150` 支（`:1469`，实体 `:21527`）对**战术卡实例**可用；`ta=30` 走 `RunSetReferenceCardRow:20895` 的 `chosen` 支（**该支未判 ta 之外的 spec，⛔ 别复用整条方法**，卡级入口自己按 `ta` 分档） | D2 | 无改动可回滚 |
| **C6** | 账 / 活状态表 | 由**调度台**合并：`B-211` 状态 + `B-255` 收窄为「排他闸已落 + 断言已钉」；本文件**不写**账 | —— | —— |

**⚠️ 行号守恒**：C2 / C3 各**新增 1 行** ⇒ `RuleCore.cs` / `EffectResolver.cs` 下游 A 行**整体偏移**。
两条纪律：① 新增行必须**自成一行**，⛔ 不许追加到 `:4222` 那行的行尾 —— 那里已有 `//` 注释，
追加会**落进注释里**（CLAUDE.md 记录过 2026-10-30 一天踩 6 次，`typecheck` 与账闸**都发现不了**）；
② 偏移由调度台在「无 `.cs` 写手 + 补丁已合并」时跑 `check_ledger_points --all-A --fix` 统一重钉，⛔ 写手自己别动。

---

## D 断言清单（要断什么 | 落在哪个宿主 | 灭自证）

| # | 要断什么 | 落点宿主 | 🧨 灭自证（改哪一句会红） |
|---|---|---|---|
| **D1** | **局面只变一次**：同一张 `GOF104` 打出去，「派发层开 / 关」两局 **`NetProtocol.StateHash(ctx)` 相同**（`NetProtocol.cs:603`；不含 `Events.Count`） | `RuleEngineTest`（`RuleEngineTest_B116G2.cs` 同族新文件） | 若本层把 `ca 30` 之类的正文行也跑了一遍 ⇒ 伤害/生命变 ⇒ 哈希分叉 ⇒ 红。**它与 D2 必须成对**：只有 D1 会「什么都没跑也绿」 |
| **D2** | **本层真的跑了（正向）**：`GOF104` 打出后 `ctx.Players[p].Discard` 里那份 `CardInstance.ReferencedCards`（`CardInstance.cs:566`）**恰 1 条**，且 `CardRef.Inst` **引用相等**被点定的那名敌人 | `RuleEngineTest` | ① 把 `TriggerRowKindOf(380)` 改回 `OwnLane` ⇒ 表空 ⇒ 红；② 把 `ta` 判据写成「无条件 = `chosen`」⇒ 换成 `ta=150` 夹具（`AM54`）时条数/成员错 ⇒ 红 |
| **D3** | **不双发（B7 双保险）**：人造夹具 —— 一张卡同 `ab` 既有 `ca=380` 行、`_triggerOps` 里又有 `AbilityIndex == ab` 的 op ⇒ 本层**不跑**（`ReferencedCards` 为 0）+ 文本那一遍**只跑一次** | `RuleEngineTest`（夹具直调 `DispatchCardAbilityRows`） | 删掉 B7 那一句 ⇒ 表变 1 条 ⇒ 红；把 B7 改成恒 `true`（全让路）⇒ D2 的真实卡红 ⇒ **两侧不可能同时绿** |
| **D4** | **`B-255` 排他闸**：`FollowUpSchedulerOf` 命中表的 18 张，各打一次，`EvtKind.Trigger` 且 `Keyword` 含 `FollowUpResolution` 的条数 **恰 = 1** | `RuleEngineTest` | 让路条件删掉 ⇒ 2 条（双发）；让路条件写成无条件 ⇒ `ASH73` 0 条（它数据里只有 `325`）⇒ 两个方向都红 |
| **D5** | **突触不重复写表**（A4）：带 `Synapse` 的战术 + 相邻单位夹具 ⇒ `ReferencedCards.Count` 仍 = 1 | `RuleEngineTest` | 把派发也塞进 `RepeatTacticOnAdjacent`（`:2967` 那一趟）⇒ 变 1+N ⇒ 红 |
| **D6** | **单位侧同形**（A1）：`GOF93 Hunta Rig`（unit，380 `ta=150`）打出后表 = 1 条且等于它自己的目标 | `RuleEngineTest` | 只在战术路接线 ⇒ 本断言红（挡「只接一半」） |
| **D7** | **表现层**：每行一条带 `AbilityIndex` 的 `EvtKind.Trigger`（原版逐 ability 一条动画），`GOF104` 打出后 `abilityIndex == 0` 的那条 ≥ 1、且 `AbilityIndex` 不为 -1 | `BattleScene`（`B116W4_Assert.cs` 同族） | 把 `abilityIndex` 参数去掉（默认 -1）⇒ 红 |

**`play check` 预期**：本层只见 `380` 与 `320` 两支；`380` 只写引用表（`CardInstance.ReferencedCards` **不进任何指纹**，`CardInstance.cs:562` 自述），
`320` 只是把**已有**的那一拍从硬编码挪到本层 ⇒ **`PinnedPlayMd5` 应当逐字节不变**（`FingerprintRow` 只打局面，`PlayProbe.cs:169-198`）。
⇒ **一变就是 bug**（双发或次序破），⛔ 不要拿「预期漂移」解释它。逐卡归因 = `play all dump` 的 **`LogTail` 列**：
预期只有 `AM54` / `GOF93` / `GOF104` 三行多一条「引用表 = N 张」日志，其余行逐字节相同。

---

## 还差什么（`？` 的格 + 为什么）

1. **`ca 360` 的输入我们没有**：原版 `AbilityLogic__PlayAbility.c:1994-2070` 读 `manager+0x2e8`（`cardChosenInSelection`）后 `OnTriggerAlt(0,…)` 让**另一张牌**跑它自己的 trigger 0。我们**没有「选择」模型**那个槽 ⇒ 现池 **18 行**（17 tactic + `TL20`）只能**出声不跑**。⛔ 不许拿 `chosen` 顶（那会让「选中的牌」变成「被点起的单位」，静默打错人）。
2. **`ca 325` 的执行器没建**：原版 `:2373-2470` 前置 `GetRefCard()`（读**本卡引用表**）→ 不在表里只 `PlayAnim`。现池 **1 行**（`ASH73`）。前置件（引用表）本批就有，但要先定「`GetRefCard` 取表里哪一个」⇒ 本次**未查清**。
3. **战术卡的 per-ability 三态闸口径未裁定**：`AbilityCriteriaAllowsAt`（`ActionPump.cs:2372`）要 `UnitState thisCard` 与 `UnitState actingCandidate`，战术卡两者都拿不到 ⇒ 现池 37 行**全部**落在带 `abilityTargetCriteria` 行的卡上 ⇒ 逐行闸会**全部返回 `null`**。两条候选口径：(a) `null` ⇒ 不跑 + 出声（保守，本文件默认）；(b) `null` ⇒ 跑但记账「闸未判」。**要调度台裁一条**（我没裁，因为这直接决定 37 行是否结算）。
4. **`PreviousAbilityTargets` 是「每次 `ResolveOps` 的并集」，不是「上一条 ability 的快照」**：`ctx.AbilityTargetsSaved` 只在两个 `ResolveOps` 入口清空（`EffectResolver.cs:408` / `:7328`），`NotePreviousAbilityTargets`（`:11498-11513`）逐 op 取并集 ⇒ `AM54` 的 380 若排在**正文之后**，读到的是「抽牌 ⊕ 后续 op」的并集，而原版是**同步**在 lg0 那一刻就把 3 张记进 `abilityTargetsToSave`（`AbilityLogic__AddDrawCard.c:36-38`：`GetNextCardsInDeck` + `AddRange`，**抽牌动作本身还是入队的**）。
   ⇒ **本文件的 C3 次序（排在正文之后）是近似**：对 `AM54` 需要 D2 换成 `AM54` 夹具断「恰 3 张且就是抽到的那 3 张」；若断不过，改法是「在正文 `ResolveOps` 之后、`PreviousAbilityTargets` 被后续 op 覆写之前抓一次快照」——**这一格未查清，需实测**。
5. **`play` 扫掠是否能合法打出这 3 张**：`AM54` / `GOF104` 需要合法点选目标，`GOF93` 是单位。⇒「预期 `play all dump` 只有 3 行变」这一条**未验证**（本代理⛔ 不跑 ruleprobe 构建、⛔ 不跑 Unity）。若扫掠打不出，改用 `ruleprobe play <id> preset pick=1` 做读数。
6. **`MapTriggerOpsByNumber` 只从 `RoutableTriggers` 取 body** ⇒ 全池只 **44 张**挂表，战术 / 防御正文（走 `desc`）**先天不在表里** ⇒ B7 双保险对战术卡**恒不命中**（不是漏洞，是已知结构差：战术卡的「文本已覆盖」判据只能靠 `desc` 解析出的 op 上的 `AbilityIndex`，而后者只在**整卡一条 ability** 或整段 `desc` 时才有值）。⇒ 若将来要让 B7 对战术卡生效，得先给 `PlayTacticCore` 的 `EffectText.Parse(card.Desc, …)`（`:2622`）加 `AbilityIndex` 归位 —— 现读不可靠，**未查清**。
7. **搜过但没找到的东西**：本文件要求的「战术路 `FireTriggerByNumber` 编译不过」的**直接证据**没重现 —— 现读的成因是 **签名要 `UnitState`**（`RuleCore.cs:17666`），战术卡结构上没有，故编译不过；这与「实测」说法一致但**不是同一份证据**。搜过的目录：`RuleEngine/Core/*.cs` · `RuleEngine/Data/*.cs` · `工具/ruleprobe/*.cs` · `D:/2/tools/decomp_full/` · `资料/完全复刻/判据/`。
