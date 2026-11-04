# B141 · W128 对账 —— 引擎 RuleCore / 多目标 / T21 / SAU55

> 只读对账代理 `R4` · 2026-11-04 · 批次 `B141` · 会话第一百二十八。
> ⛔ 未改任何源码/数据/正本/所有权表/账；只写本文件。⛔ 未跑 Unity / ruleprobe / git。
> 行号一律**本文写作时现读**（`Unity/MyGame/Assets/**`、`d:/2/tools/decomp_full/`、`Resources/cards_engine.json`）。

---

## 0 · 一句话结论

| ID | 判定 | 一句话 |
|---|---|---|
| **SAN-04** | 🟠 真没做（残余 1 件） | ① `ta=150` 手牌半**已做完**（代码产出 `Slot=-1` + 消费侧有手牌回落），只剩旧注释过期；② **605/600「先算后贴」helper 仍未建**（三处 emit 仍在结算之后、返回值全被丢弃） |
| **SAN-27** | 🟠 真没做（残余 1 件） | ① `targetSelectMethod 2/3` **已于 2026-11-04 `B140_WA` 做完**；② 手牌半做完；③ 多行 ability：**效果层**已按行重挂、**事件/表现层仍只挑一行**（结构近似；另见 🔴 陈旧断言） |
| **ENG-22** | ⛔ 判据本身错 | 「建 `BattleContextTrial`」这条前提**不成立**（`B115_S6` §三·1 已判：原版无统一 `CanApply`/快照，二者皆自设计、铁律 18·0 禁）；真缺陷 A/B **已修**，C/D 无需动 ⇒ 实际剩余 = 0 |
| **B123-CA50SIX / SAU55** | 🟠 真没做 | 主语 = **己方（施放者）战将的天赋卡**；我们 **`ta=235 warlordsTalent` 未建模** ⇒ SAU55「才能降 1 费」无等价实现（ca 50 行被正文闸挡掉、正文解出的 `lowercost` 对象认不出） |

---

## 1 · SAN-04 · T21 逐卡专属动画触发面「先算后贴」残余②

**现读证据**

- 断言接线（现读已挪位）：`CardPresentation/Editor/BattleScene.cs:45405` —— `B104_T21_Assert.RunEngine(driver.Ctx); B104_T21_Assert.RunPresentation(driver);`，写在 `RunR13();` 之后、`CheckManaBuffHud` 之前（`B106_W80` 修的就是「接在 `DestroyImmediate` 之后」的静默跳过）。
- **残余 ①（ta=150 手牌半）已做完**：
  - 生产：`Core/RuleCore.cs:18027-18047` `case 140/150` 从 `ctx.PreviousAbilityTargets` 分两路 —— 棋盘项进 `pool`、手牌/离场项进 `handRefs`；
  - 输出：`:18148-18149` `for (...) list.Add(new BattleEventTarget { ..., Slot = -1, CardId = hr.Name, InstanceId = hr.InstanceId });`
  - 存储：`Core/BattleContext.cs:1134` `public readonly List<CardRef> PreviousAbilityTargets`（**已是 `CardRef`**，能手牌实例）；并集由 `EffectResolver.cs:11498` `NotePreviousAbilityTargets` 并入 `LastHandTargets` / `LastHandTarget`；
  - 消费：`CardPresentation/Battle/BattleDriver.cs:36185 AbilityTargetView(...)`，`:36192` `if (player >= 0 && instanceId > 0) { var hv = HandCardViewOf(player, null, instanceId); ... }`（`B115_W21`）。
- **残余 ②（605/600 先算后贴）未做**：
  - `EffectResolver.cs:8569` `EmitAbilityUseTrigger` **已改返回 `BattleEvent`**（`B115_W23 · C1`），但**全部调用点丢弃返回值**：`:8626`（战术 600）、`:8654/:8668/:8673`（信仰 605）、`:9318`（灵魂石 600）、`:9501`（信仰 deferred）。
  - 三处 emit 仍在 `ResolveOps` **之后**：`SpiritAbilityDeferred`（`:9316-9318` 先 `ResolveOps` 再 emit）、`FaithAbilityDeferred`（`:9493` 结算、`:9501` emit）、`EmitTacticAbilityUseTriggers`（`RuleCore.cs:2870` 在正文结算之后调用）。
  - 全仓 `grep "scratch|Scratch|先算后贴"`（`RuleEngine/Core/*.cs`）= **0 命中** ⇒ helper 不存在。

**原版值 vs 我们值**

| 项 | 原版 | 我们 |
|---|---|---|
| 605/600 目标表 | 事件 = `TriggerAbilities` 那一跳 `GetTargets` 的表（`RawCardScript__OnCardPlayedWithTarget.c:73` / `CardScript__ResolveUnitSummoned.c:51`），逐目标播 | emit 在结算后，`TriggerTargetSink` **arm 不上**；事件 `Target*` 恒为 null（表现层只播一份在施放者身上） |
| ta=150 手牌目标 | 手牌与场上同型 `CardScript`（`dump.cs:35370`） | ✅ 已产出 `Slot=-1` + 消费侧按 `InstanceId` 认手牌 |

**该改哪里（派写手）**

- 只在 **605/600** 三族加「先算后贴」：`ResolveOps` 前用 `EmitAbilityUseTrigger` 先发事件并持引用，`ResolveWithTriggerTargetSink(ctx, evt, () => ResolveOps(...))` 包住结算（或结算期用 scratch 事件捕获首个目标、emit 后抄回）。落点 = `EffectResolver.cs` 的 `SpiritAbilityDeferred` / `FaithAbilityDeferred` / `EmitTacticAbilityUseTriggers`（后者在 `RuleCore.cs:2870` 调用、实现应在 `EffectResolver`）。⚠️ 多付费组（26 张里 3 张）是「一条正文发多条事件」，须先裁「多组 605 取哪个目标」（`B107_T21四族目标列表.md` §三·③）。
- 顺带：`RuleCore.cs:17923-17924` 那段「`ta=150` 落到手牌的那一半不产」**已过期**（代码已产），应就地订正（铁律 5）。

**还差什么**：多组 605 的目标归属裁断；`EmitAbilityUseTrigger` 返回值消费链（现全丢弃）。

---

## 2 · SAN-27 · 多目标能力逐目标播放残余

**现读证据**

- 消费端：`BattleDriver.cs:19760-19830` `ExpandAbilityBeats` 逐目标排拍（`tgtList.Count` 展开、`AbilityTargetView(beatTgtPlayer, beatTgtSlot, beatTgtInstance)`）。
- 生产端：`RuleCore.cs:17963 AbilityTargetListFromCriteria`（接 `ta ∈ {0,10,20,30,110,140,150}`）· `:15351` 递 criteria · `:15625 publishTable`。
- **残余 ① `targetSelectMethod 2/3` —— ✅ 已做完（2026-11-04 `B140_WA`）**：
  - `RuleCore.cs:18102-18108` `case 2: case 3: ... pool = KeepByValueInPlay(pool, method, max); break;`
  - `RuleCore.cs:18517 KeepByValueInPlay` —— 键 = `SimpleAI.ValueInPlay`，`method==2` 降序 / `3` 升序；判据 = `out_il2csharp/Assembly-CSharp/AbilityLogic.cs` 的 `switch (targetSelectMethod - 1)`。
  - 旧「`return null`」已不存在。
- **残余 ② `ta=150` 手牌半 —— ✅ 已做完**（同 SAN-04 ①：`:18027-18047` + `:18148-18149` + `AbilityTargetView:36192`）。
- **残余 ③ 多行 ability 只挑一行 —— 🟠 部分做完**：
  - **效果层**已按行重挂：`RuleCore.cs:15632 PublishRowAbilityTargetTable(...)`（`B140_WA · ②`，只挂零随机消费档 `ta ∈ {0,10,110,140,150}` ∧ 不限张数）；
  - **事件/表现层仍是一张表**：`RuleCore.cs:18165-18171 ActiveAbilityTargetCriteriaForPlan` 的 doc 自陈「多行目标表按行独立、不合并……只能挑**一行**的整张表（⛔ 不取并集/交集）」—— 即 `BattleEvent.AbilityTargetList` 仍只承载一行。

**原版值 vs 我们值**

| 项 | 原版 | 我们 |
|---|---|---|
| `targetSelectMethod 2/3` | `OrderByDescending(GetValueInPlay)` / `OrderBy(GetValueInPlay)` 后 `Take(maxTargets)`（`AbilityLogic__GetTargets.c:1215-1261`，键链见 IL2CSharp） | ✅ 同形（`KeepByValueInPlay`）。池内 `targetSelectMethod` 现读仅 `0`(191 行)/`7`(2 行) ⇒ 休眠 |
| 多行 ability 逐行播放 | 每行各一次 `GetTargets + PlayAnim`（`AbilityLogic__PlayAbility.c:262/273`） | 效果层 ✅ 按行；事件/表现层 ❌ 一张表挑一行 |

**该改哪里（派写手）**

- 残余 ③：若要事件层也按行，需把 `BattleEvent` 的单一 `AbilityTargetList` 扩成「按行多表」或用 `AbilityIndex` 兜住。⚠️ 结构改会命中 `Editor/RuleEngineTest.cs:33120/:33141` 的 `EvtKind.Ability` 字面量断言（`B140_WA` 报告 §3·② 明示这两个文件在它白名单外），须同批改断言。
- 🔴 **必须先修的陈旧断言**（见顺手发现 1、2）—— 否则 `RuleEngineTest.Run` 会红。

**还差什么**：多行事件的按行承载（结构）；陈旧断言清理。

---

## 3 · ENG-22 · `BattleContextTrial` 施工面

**现读证据**

- 全仓 `grep -rn "BeginTrial|RollbackTrial|BattleContextTrial|TrialScope"`（`Unity/MyGame/Assets`，排除 `.meta`）= **0 命中**（`BattleContextTrial` 确实不存在）。
- **真缺陷 A 已修**：`RuleCore.cs:11760-11764` —— `var spec = u.Ability;`（注释自陈「先取规格、紧接着校验目标，再写状态」）→ `:11761 if (EffectTargets.NeedsPick(spec.Target) && !BoardSpec.IsValid(targetSlot)) return RuleCodes.ErrTarget;` → **之后**才 `:11762 u.LastAttackType = 4; :11764 RecomputeAttackTypeAfterActiveAbility(u);`。
- **真缺陷 B 已修**：`RuleCore.cs:11963 bool ok = ResolveOathAbility(...);` → `:11964-11971 if (!ok) { ... return RuleCodes.ErrAbilityRejected; }` → **成功之后**才 `:11975 u.LastAttackType = 4; :11976 RecomputeAttackTypeAfterActiveAbility(u);`（注释「两条状态写入挪到『成功』之后」）。
- **C（同形零可观测，无需动）**：`RuleCore.cs:12172-12175` `UseAlternativeCore` 写早于结算，但函数后段**无 `return Err*`** ⇒ 今天不算「先试后拒」。
- **D（原版形状，别动）**：`RuleCore.cs:15609-15614` `UseActiveAbilityCore` 先写后结算 —— 原版 `ResolveActiveAbilityPlayed.c:19-43` 同样先写（`+0x50`/`+0x4C`）后结算。
- **判据原文**（`判据/B115_S6_四条引擎欠账施工面.md` §三·1）：原版**既无统一 `CanApply`、也无通用快照**；靠 47 个机制级 `Can*`/`IsValid*` + 个别显式回滚（`_ResolvePlayCardFromHand…:940-960`）。§三·1 明写「账上那『二选一（谓词/快照）』两个选项**都是自己设计**，按铁律 18·0 两个都不该选」。

**原版值 vs 我们值**

| 项 | 原版 | 我们 |
|---|---|---|
| 统一试算/回滚 | 无（每机制前置谓词 + 逐例回滚） | 无（也不该建） |
| 4 处落点次序 | 先校验后写 | A ✅ 已调 · B ✅ 已调 · C 同形无需 · D 原版如此 |

**该改哪里**：**不建 `BattleContextTrial`**（与判据/铁律 18·0 冲突）。对应账行可直接核销（A/B 已修、C/D 无需动）。若仍想收口，只能做「原版真的回滚」的那一处显式撤销（今天唯一候选 = 出牌后选目标失败 ⇒ 表现层已重摆，引擎侧按 `_ResolvePlayCardFromHand…:940-960` 核一遍再定，属另一笔）。

**还差什么**：无（除非调度台裁定要做「显式回滚」那一笔）。

---

## 4 · B123-CA50SIX —— 只剩 `SAU55`

**现读证据**

- 卡数据（`Resources/cards_engine.json`，现读 1,142 张）：`SAU55 Immortal Pride`（tactic，subtype `Talent`，cost 7）
  - `desc` = `Give Armour 1 to all your units. For the rest of this battle, your Warlords' Talent costs 1 less`
  - `descZh` = `向所有友方单位赋予 [[armour]] 1。在此战斗的剩余时间里，你的战将的才能花费减少 1`
  - `triggerAbilityIndices` = `[{trigger:0,lg:0,ca:30},{trigger:0,lg:1,ca:3},{trigger:0,lg:2,ca:50}]`
  - `abilityBuffs` 的 `lg=2` 行 = `at:"handBuff" buffType:2 costChange:-1 pa:10 criteria{targetsAffected:235}`
- 主语判据（第一权威 `.c`）：`d:/2/tools/decomp_full/FilterMethods__CheckIfMeetsCriteria.c:126-157` —— `if (iVar17 == 0xeb) {` → `BattleManager__GetHero(param_1, *(byte*)(param_1 + 0x40) /*施放者侧*/, 0)` → `CardScript__GetTalentCard(hero, 0)` → 与候选 `(*(candidate+0x28))+0x38` 做 `System_String__Equals`。
- 枚举：`d:/2/tools/il2cpp_out/dump.cs:20816` `public const TargetsAffected warlordsTalent = 235;`。`pa:10` = `playersAffected Self`（施放者自己那手，见 `RuleCorePartial_B115W20.cs:113-135`）。
- 天赋卡模型：`RuleCore.cs:13721 SpawnTalents` 每回合把**战将天赋**作为战术卡造进**玩家手牌**（`AddCardToHandWithinLimit`，`EphemeralMarked`）。

⇒ **主语 = 己方（施放者）战将的天赋卡（手上那张临时战术卡）**，不是「别人督军」、也不是「场上单位」。

**我们有没有等价实现：没有（决定性的一条 = ①；②③是同一结论的另两条链）**

1. 🔴 **决定性：`ta=235` 未建模**：`Core/TargetCriteriaEval.cs:148-150` `case 235: gap = "ta=235 warlordsTalent 本版未建模（要比督军天赋卡）"; return null;`。SAU55 的 `handBuff` 载荷行（`lg=2`，`criteria{targetsAffected:235}`）无论走哪条派发链，最终都由 `HandCardMatchesAbilityBuff`（`RuleCore.cs:11183`）判候选；`Matches` 返 `null` ⇒「判不出来 ⇒ 不放行」⇒ **一张手牌都命中不了**。这一条独立成立。
2. **ca 50 行多半进不到主路**：`EffectResolver.cs:21078` `var ops = ...TriggerOpsByNumber(...); if (ops == null || ops.Count == 0) { ... FireTriggerRowsByChosenAbility(...) }` —— 按 `B123_E1` 口径 SAU55 trigger 0 有正文 op（Armour 那句）⇒ `ca 50`（`RunHandBuffRowOfEnchantment`）整趟不进。⚠️ **未做实况**：SAU55 `abilityLogic = null`，`TriggerOpsByNumber(0)` 是否为空取决于 `MapTriggerOpsByNumber`（`Data/CardDatabasePartial_B98H7.cs:134`，按 `op.AbilityIndex == ab` 归位）—— 若为空则 ca 50 行**会**跑到，但仍被 ① 挡死，结论不变。
3. **正文路也不行**：`For the rest of this battle, your Warlords' Talent costs 1 less` 先被 `TryPersistent`（`EffectText.cs:5471`）试：非 `at the start/end`、非 `ReCostMore`、`Dispatch(body)` 出来的 `lowercost` 没有「部署时」目标 ⇒ `:5612 return false`（整句不注册常驻）；随后落到 `TryLowerCost` → `ReSubjectCostLess`（`:8406`）凭「`your` 可省」在整句上命中，`payload` = `For the rest of this battle, your Warlords' Talent`（`SubjectCostOp` 只剥开头 `the`/`your`/`next`，剥不掉前缀）。`DoLowerCost`（`EffectResolver.cs:15553`，`:15657-15690`）里它既非 `IsKindWord`、`FindByName` 也查不到 ⇒ `:15678` 报「既不是兵种词也不是卡名 —— 这条没生效」。

**该改哪里（派写手）**

- 首选（贴近原版数据路）：在 `TargetCriteriaEval.cs` 实现 `case 235` —— 候选 == `GetHero(本方)` 的 `GetTalentCard`（我方手牌里那张子类 `Talent` 的临时战术卡），并让 `SAU55` 的 `handBuff` 行可达（ca 50 的「正文非空」闸需按「正文是否已覆盖这一句」细化，⛔ 不能整趟放进——会双发）。
- 备选（正文 sentinel，仿 `SelfCostMoreMarker` `EffectText.cs:8152`）：`SubjectCostOp`/`DoLowerCost` 认 `warlords' talent` 主语 ⇒ 解析到「本方战将天赋卡实例」，`Duration` = 永久。
- ⚠️ 两种都要跑 `play check`：现在池内 `ta=235` 只此 1 张，改动会影响 SAU55 一张，须逐张归因。

**还差什么**：原版「for the rest of this battle」= 永久（`CardEffect` 无 until 位 = 常驻），我们 `handBuff` 记录的 `permanentBuff` 现读 0 —— 实现时按永久登记，并核对 `Expire`。

---

## 5 · 顺手发现（⛔ 只报不改）

1. 🔴 **陈旧断言必红**：`CardPresentation/Editor/B104_MT_Assert.cs:281-296` 的 `A10-a/A10-b` 仍断言 `targetSelectMethod 2/3 ⇒ null`，而 `RuleCore.cs:18102-18107` 已实现（`B140_WA`，2026-11-04）。`RuleEngineTest.cs:1294` 里 `B104_MT_Assert.Run();` 是接线的 ⇒ **下一次 `RuleEngineTest.Run` 会出 2 条红**。改法：把 A10-a/b 改成「截断到 1 张」或删掉（`B140_WA_Assert` C1/C2/C3 已覆盖）。
2. 🔴 **`B140_WA_Assert` 未接线**：文件在 `RuleEngine/Editor/B140_WA_Assert.cs`，但 `RuleEngineTest.cs:1294` 的聚合行**没有** `Guard("B140_WA_Assert", ...)`（`B140_WA` 报告 §6·4 自己写着「尚未接线，等调度台」）⇒ `KeepByValueInPlay` 目前**零断言覆盖**。
3. 🔴 **过期注释**：`RuleCore.cs:17923-17924` 写「`ta=150` 落到手牌的那一半不产 / `PreviousAbilityTargets` 被写死成 `List<UnitState>` / `AbilityTargetView` 只查 `_myUnits`/`_foeUnits`」—— 三条**全部已过期**（`BattleContext.cs:1134` 已是 `List<CardRef>`、`AbilityTargetView:36192` 已有手牌回落、`:18148-18149` 已产出手牌项）。同 `:17926-17927` 的「2/3 要 `null`」也过期。按铁律 5 应就地订正。
4. ℹ️ `EffectResolver.cs:9501`（信仰 deferred 的 emit）与 `:9318`（灵魂石 deferred 的 emit）是 605/600「先算后贴」的**两条主落点**；战术侧在 `RuleCore.cs:2870`（实现 = `EffectResolver.EmitTacticAbilityUseTriggers`）是第三条。
5. ℹ️ `B104_T21_Assert` 的接线行现读 = `BattleScene.cs:45405`（简报里的 `45243/45266` 是旧行号，已漂）。

---

## 6 · ❓查不到（搜过什么）

无阻塞性「查不到」。以下为**未做实况**、如实记：

- `SAU55` 第二句的**运行期**行为没跑（`ruleprobe card "Immortal Pride"` / 引擎实跑）—— 本代理只读、未跑工具；上述结论全部来自静态代码 + 数据 + `.c`，三条链（ta235 未建模 / ca50 被闸 / 正文对象认不出）**任一条**成立即无效果。
- `ta=150` 手牌半的**实况**（真卡有没有手牌目标）未跑：判据侧池内 `ta=150` 真实消费行 = 0（`BattleContext.cs:1126` 附近注释「今天池内 `ta = 150` 的真实消费行 0 条」）⇒ 代码已闭合但**零可观测**。
- `targetSelectMethod 2/3` 池内现读 `0`(191)/`7`(2) 行，**无 2/3 档的卡** ⇒ 该档只有夹具可达，实况零可观测（与 `B140_WA` 报告 §3 一致）。
