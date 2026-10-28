# E1 · `RuleCore.cs` 落点「判据没读完」17 条逐段读完（2026-10-28 · 只读代理，调度台落盘）

> 选行口径：账 `战斗逻辑驱动.tsv` 里 `状态 ∈ {B,C}` ∧ 落点第一个 `文件:行` 指向 `RuleCore.cs` = **87 行**；其中差异/备注含「未逐句核/未核/未逐条核/逐段读/未逐句比/未查/未逐档」= **17 行**（本件全覆盖，无剩余）。
> 全部**读全文**，没有停在半截。

## §判决汇总

| 账行 | 原版类.方法 | 判决 | 一句话 |
|---|---|---|---|
| L169 | `BattleManager.AddSummonUnit` | ②可做 | `.c` 全文 = 构造 `SummonUnit(20)` action + 入队；我们缺 `enchantment` 形参（`DeployFreeCore:4754/4974` 都没有），附魔分两拍 |
| L516 | `BattleManager.ResolveFatigue` | ②可做 | 原版走 `DealMultiDamage`：**易伤加量、护甲 `max(1,·−Armor)`、盾不参与**；我们 `RuleCore.cs:3095` 直接扣血，三项都没做 |
| L592 | `ShouldActivatInitialEnchantments` | ②可做 | 原版 = `matchType ∈ {50,80}` 纯谓词；我们 `SetupInitial` 恒 false；`MatchType` 枚举现成，关卡数据五整数缺〔⚠️ 2026-10-28 更正：见本文件 §7 第 7 条脚注 —— 偏移是 `+0xA8..+0xB4` **四个 int**，且**教程侧早已落字段并已接消费者**；缺的是**战役**关卡 SO 本体〕 |
| L607 | `StartBattlePhase` | ②可做 | MoveNext 全文 = 开局演出 + 初始附魔两跳 + **督军武器卡进手** + `ActionsInQueue` 让行；我们缺后三件（落点还被错记到 `BeginTurnCore`） |
| L617 | `TryUsingActiveAbility` | **已一致（核销 A）** | 两支已在 `BattleDriver.cs:15766-15784` 逐句落；落点应改指 BattleDriver |
| L618 | `TryUsingActiveAbilityOfCard` | ②可做 | 同上已一致，只差 `forceSelectCard` 写 `+0x410/+0x338`（= `_selectedSlot`） |
| L654 | `BattleManagerSupport.BroadcastUnitMob` | **已一致（核销 A）** | 接收方 `TriggerOtherCardMob.c:14` 自排除的是**传入单位**，不是 caster；我们的 `SelfExcludesOnKeywordTrigger` 同形 |
| L724 | `CardScript.ClearPendingDamage` | **已一致（核销 A）** | `.c` 全函数只有「日志 + 清表」，无规则；⛔ 不需要建 pendingDamage 台账 |
| L742 | `CardScript.FinishAfterAttack` | **已一致（核销 A）** | 复位三件 + inPlay + 血嗜四闸都在（`CardFeel.AttackRecover` / `EmitBloodThirst:12441-12445`） |
| L774 | `HasEnoughPendingDamageAndIsRemnant` | **已一致（核销 A）** | 三合一谓词拆在 `:7699` 与翻面支 `:9836`，行为同 |
| L801 | `IsValidActiveAbilityTarget` | **已一致（核销 A）** | 账上「camouflage / trait 0x46 没核」两格 = `isCurrentlyWarded`(`:11369`) 与 stealth(`:11364`)，都已落 |
| L816 | `CardScript.OnTurnSetup` | ②可做 | 原版按 `+0x33/+0x34/+0x35` 三档清 effect；我们只有一档 `UntilMyNextTurn`，**缺「对面回合撤」那一档** |
| L915 | `ShouldTriggerCompanion` | **已一致（核销 A）** | 三条逐项同构（`:4373-4377`）；账上「我们读印刷」已过期 |
| L926 | `ShowDropPodFigures` | ②可做 | 原版是纯查询（状态五档 ∧ dropPod）；我们只有动作 `OpenDropPod`，缺谓词本体 |
| L960 | `TriggerUnitBacklashActions` | **已一致（核销 A）** | 原版 criteria 不满足**仍入队**（只置跳过位 + LogWarning），我们入队前挡；净同 |
| L1209 | `RawCardScript.CanPlayAbilities` | ②可做 | 原版是逐条 OR；我们折成卡级一条闸，目标档要拆成「候选集非空」 |
| L1347 | `ShuffleCardList` | **已一致（核销 A）** | 钉位支未实现但数据 1351 张全 0；零钉位时就地重排 = 重建新表 |

**计数**：②可做 **8** · 已一致核销 A **9** · 判据仍空 0 · 原版不可达 0。

## §可派写手的 8 条（含改点与做法）

1. **L516** `RuleCore.cs:3095` —— 把 `ps.Fatigue` 换成「先加易伤、再过 `Math.Max(1, x − Armor)`」；`EmitHit` 仍用**减甲前**的量（对齐 `.c:141` 早于 `:154`）。
   ⇒ 顺带答了账上「护甲/盾参不参与」：**护甲参与、盾不参与**。
2. **L1209** `RuleCore.cs:11140 CanUseAbility` —— 目标档拆成「先 `CanStartAbility` 放行、再在 `NeedsPick` 时问候选集是否非空」；`CanUseAbilityTarget` 不动。
3. **L816** `UnitState.cs:1694 RevertBuffs` —— 给 `TempBuff` 加第二到期档 `UntilOpponentNextTurn`（判 `Owner != player`），并在 `RuleCore.cs:1909` 的扫场里一并跑；155 触发维持休眠。
4. **L618** `BattleDriver.cs:15766` —— 把「面板点开的那张卡」显式写进 `_selectedSlot`（若已写则本行核销）。
5. **L926** `RuleCore` 类尾 —— 加 3 行谓词 `ShowDropPodFigures(UnitState)`（`IsInPlayOrDying` ∧ `Has(DropPod)`）；消费者接 `OpenDropPod` 的调用方。
6. **L169** `RuleCore.cs:4754 DeployFreeCore` —— 加「随召唤一起挂上的附魔」形参（或 `IReadOnlyList<CardDef>`），落格那一刻调挂载；`:4974` 重载转发。⛔ 别开第三条挂附魔的路。
7. **L592** `RuleCore.cs:422` —— 加 `ctx.MatchType == PracticeOffline || EventAI` 门。⚠️ 关卡数据五整数（`+0xa4..+0xb4`）我们没有落字段 ⇒ 门开了也只能**如实出声 + 不挂**，⛔ 不许拿默认值顶。
   > ⚠️ **2026-10-28 更正（铁律 5 · `B91_P3` 现核）：上面这句有两处错。**
   > ① **偏移/个数**：`TutorialStage` 里是**四个 `int`** —— `startingPlayerMana(+0xA8)` / `startingEnemyMana(+0xAC)` /
   > `startingPlayerDamage(+0xB0)` / `startingEnemyDamage(+0xB4)`；`+0xA0` 是 `enemyStartingTroopsInHand` 这个 **`List`**（旧判据把起点写成 `+0xa4`，跨到了 List 的尾字节）。
   > ② **「我们没有落字段」不成立**：教程侧三件都已就位 —— 数据 `Resources/tutorial_stages.json`（`gen_tutorial_stages.py` 带出）·
   > DTO `Core/TutorialScript.cs:319-322` · 消费者 `TutorialScript.cs:1639 SetupInitialManaAndDamage` / `:1658 ManaBonus` ← `RuleCore.cs:1768`
   > （6 关四值实测全 0 ⇒ 今天不改变局面，但机制照做）。
   > ⇒ **真缺口在另一边**：**战役关卡 SO 本体从未提取**，且 `BattleContext.CampaignStage`（`Core/BattleContext.cs:922`）**零写点**（`RuleCore.cs:1241/:2799` 自承）。
   > **错因**：拿 `+0xa4..+0xb4` 这一段在 `EntityScript` 上的「五个属性镜像」当成了关卡数据，没去核 `TutorialStage` 的字段表。判据全文 → `判据/B91_P3_战斗数据列缺口.md` §二·4。
8. **L607** `RuleCore.cs:419-422`（补两跳，依赖 L592 数据）+ `:645` 之后（督军武器进手，⚠️ 武器卡 `+0x148` 在卡表里对应哪一列未定）。

## §反对清单（账要订正，零开发量）

1. **L654**：账写「排除的是 caster，不是被触发那个单位」—— `TriggerOtherCardMob.c:14` 是 `op_Inequality(param_2, param_1)` = 接收者 vs **传入的那个单位**；`BroadcastUnitMob.c` 全函数只有 2 个形参。⇒ 改 PTS、删差异、判 A。
2. **L617 / L618 落点归错**：账指 `RuleCore.cs:11550`（= 结算 `UseAbilityCore`），而这条原版是 **UI 入口** ⇒ 应改指 `CardPresentation/Battle/BattleDriver.cs:15766`（或双落点）。
3. **L960**：PTS 少了半句 ——「不满足 ⇒ LogWarning + skip」漏了 `.c:181` **无条件入队**（跳过位随 action 走）。
4. **L742 / L915 差异列过期**：L742 的「逐句 3/4」、L915 的「我们读卡面印刷」都不成立（`F1` 已改成 `unit.Has`）⇒ 可直接回账。
5. **L724**：账记 B「结构不同没逐句对过」，本轮对完 = **无行为差**，建议 A。

## §交接
本批 **17/17 有结论**。若要扩到原计划的 46 条，其余 **33 条不在 `RuleCore`**：`EffectResolver.cs` 10 · `BattleDriver.cs` 9 · 无落点 9 · 其余小件 —— 需另派（E2 已覆盖 EffectResolver 那一族）。
可直接回账：**9 条 A** + 上面 5 条订正。缺数据但**不阻塞写代码**的两处：L592/L607 的关卡数据五整数 · L607 的督军武器卡列。

> ⚠️ **2026-10-28 更正（铁律 5 · `B91_P3` 现核）：这句里的「关卡数据五整数」要改成两件事。**
> ① **教程侧不缺**（四个 int 在 `tutorial_stages.json` → `TutorialScript.cs:319-322` → `:1639/:1658` ← `RuleCore.cs:1768` 全链已通，6 关实测全 0）；
> ② **战役侧**缺的是「关卡 SO 本体没提取 + `ctx.CampaignStage` 零写点」，属**另一条线（战役关卡提取）**，不是给卡表加列。
> ⇒ 真正「缺数据但不阻塞写代码」的只剩 **L607 的督军武器卡列**（`+0x148` ↔ 卡表哪一列仍未定）。详见本文件 §7 第 7 条脚注。
