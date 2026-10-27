# B9_W27ENG —— `cond`/`ex` 的 `ta` 短路闸 ＋ `+0x4c`「主动能力本局已用」· 执行报告（2026-10-27）

> 判据正本 = `资料/完全复刻/判据/cond341_canUseDuty_ex语义.md`（只读代理 `B9_R1DUTY` 产出）。
> 本报告只记**做了什么 + 读数**，判据不在这里抄第二份。
> 改动文件（**严守文件所有权**）：`Core/EffectResolver.cs` · `Core/UnitState.cs` · `Core/RuleCore.cs` ·
> `Core/ConditionEval.cs` · `Core/TargetCriteriaEval.cs`。**没动** `Aura.cs` / `CardDef.cs` / `Enchantment.cs` /
> `CardInstance.cs` / `RuleEngineTest.cs` / `BattleDriver.cs`。

---

## 一、结论

### A（`BL64` 打不出去的真因）—— ✅ 已修

在 `EffectResolver` 的 cond-only 旁路（`ApplyStructuredConditionFilter`，经 `ApplyTargetFilters` 被
`ResolveTargets` / `IsLegalPick` / `PlayTargetPool` 调用）**之前**加了 `ta` 闸：

- **短路集合只有一份** = `TargetCriteriaEval.EarlyReturnTargetsAffected` / `NeverFilteredTargetsAffected`
  （新加在 `TargetCriteriaEval.cs` **类尾**），唯一判定口 = `TargetCriteriaEval.ConditionIsDeadByTargetsAffected(ta)`。
  `EffectResolver.CondDeadByTa` **转调它**，⛔ 没有第二张数字表。
- **判不了（`ta` 读不到）⇒ 不闸 + 出声**（`StructuredConditionNotice`，同 key 只喊一次）。
- 顺带对齐的第二族：`AbilityLogic__GetTargets.c` 里**建池侧根本不筛**的那批 `ta`
  （120/130/160/161/162/170/190/220/230/240/245）—— 同一原理（也走不到第 ⑧ 步）。**今天 0 条数据落在这一族**
  （见 §二·3 的普查），加它只是防未来数据静默偏离；若调度台认为超出本件范围，删掉那一个数组即可。

**正面证明（仓库外夹具，⛔ 未进仓库）** —— `BL64 Murderous Desires`，我方 3 部队（A/B 受伤、C 满血）+ 督军：

| | 可选目标集合 | `CanPlayTactic`（点受伤的 FIX_A） |
|---|---|---|
| 改动前（把死字段 `cond=341 ex=1` 当活字段套） | **0 个**（A/B/C/督军全部被筛掉） | **18 = `ErrNoTargetAvailable`** |
| 改动后 | **2 个**：`FIX_A`(hp2/4) · `FIX_B`(hp3/5) | **0 = OK** |

> 满血的 `FIX_C` 在改动后**仍然不是合法目标**（`CanPlayTactic` 返 18）⇒ 这不是「什么都能点」，
> 而是回落到了卡面/卡级判据「**一个受伤的友方单位**」（正文规格 `side=own damagedOnly=True`，
> 原版卡级 `cardTargetCriteria = {ta:10, friendly, cond:40 damaged, ex:0}`）。
> 夹具源码：`d:/tmp/wf_b9ta/{TaProbe.cs,Census.cs,TaProbe.csproj}`（仓库外，可复跑 `dotnet run`）。

### B（`+0x4c usedActiveAbility`）—— ✅ 已复刻

- `UnitState` **类尾**新增 `public bool ActiveAbilityUsed;`（doc 写全：写点 / 清点 / **不随回合重置** / `pray` 不置 / 与 `DutyUsed`·`UsedActiveAbilityThisTurn` 的分工）。
- **置位三处**：`UseAbilityCore` · `UseOathAbilityCore`（`UsedActiveAbilityThisTurn = ActiveAbilityUsed = true;`）、
  `UseAlternativeCore`（`u.ActiveAbilityUsed |= keyword != KeywordTable.Pray;` —— `pray` **不置**）。
- **清零两处**：`EffectResolver.DoReloadDuty`（**无条件**清，⛔ 没写进 `DutyUsed` 的 `continue` 之后）、
  `UnitState.ResetToValuesInHand`（与 `+0x50` 那一族一起清）。**⛔ 没有**在 `RefreshForNewTurn` 里清。
- `ConditionEval` 的 `case 341` 改读 `ActiveAbilityUsed`（**未取反**的 match；取反仍只由 `Meets` 做一次 —— `ex` 那一层**一个字没动**）。
- `UnitState.cs:746-759` 那段错注释就地订正（**零行数变化**，见 §四）。

### 读数

| 检查 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_w27 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（0/0） |
| `bash 工具/ruleprobe.sh check` | **rc=0** · 解析差异 0 行 · 哨兵失败 0 条（解析层未受影响，符合预期） |
| `bash 工具/ruleprobe.sh play check` | **rc=1（指纹变）**，但**只有 1 张卡变**（`BL64`），逐列归因见 §五 |
| 基线 / `PinnedPlayMd5` | **一个字没动**（⛔ 重钉只有调度台能做） |

---

## 二、证据（文件:行号）

### 1. 改动落点（我们的代码）

| # | 位置 | 内容 |
|---|---|---|
| 1 | `Core/TargetCriteriaEval.cs:1161-1209`（**类尾**） | `EarlyReturnTargetsAffected` ＝ `{0,20,30,40,70,80,140,200,201,235}` · `NeverFilteredTargetsAffected` ＝ `{120,130,160,161,162,170,190,220,230,240,245}` · `ConditionIsDeadByTargetsAffected(int)` |
| 2 | `Core/EffectResolver.cs:16672` / `:16684` / `:16704`（**类尾**） | `TargetsAffectedOf(EffectOp)` · `TargetsAffectedForOp(IReadOnlyList<EffectOp>, EffectTargetSpec)` · `CondDeadByTa(ctx, row, ta, quiet)` |
| 3 | `Core/EffectResolver.cs:2360` | 闸本体：`if (CondDeadByTa(ctx, row, targetsAffected, quiet)) return;`（`ApplyStructuredConditionFilter` 的第一句之后） |
| 4 | `Core/EffectResolver.cs:2168` · `:2357` · `:2011` | 三个形参（`ApplyTargetFilters` / `ApplyStructuredConditionFilter` / `IsLegalPick`）—— 默认 `CountSetSpec.Unknown` ⇒ 老调用点零影响 |
| 5 | `Core/EffectResolver.cs:1703` · `:1999` · `:2091` · `:2096` · `:16143` | 五个调用点把 `ta` 传进去（`TargetsAffectedOf(op)` / `TargetsAffectedForOp(ops, spec)`）—— 高亮与结算读**同一条 op** |
| 6 | `Core/RuleCore.cs:11534` · `:11738` · `:11964` | 置位三处 |
| 7 | `Core/EffectResolver.cs:15071` | `DoReloadDuty` 清 `ActiveAbilityUsed` |
| 8 | `Core/UnitState.cs:2712` | `ResetToValuesInHand`：`UsedActiveAbilityThisTurn = ActiveAbilityUsed = false;` |
| 9 | `Core/UnitState.cs:3188`（**类尾**） | `public bool ActiveAbilityUsed;` |
| 10 | `Core/ConditionEval.cs:556` | `case 341: return !u.ActiveAbilityUsed;` |
| 11 | `Core/UnitState.cs:746-759` | 错注释就地订正（零行数变化） |

### 2. 原版判据（与正本一致，不在此展开）

`FilterMethods__CheckIfMeetsCriteria.c:57-157`（早返 switch）· `:76-91`（`ta=30` 直接 `return 候选 == targetCard`）·
`:298-303`（第 ⑧ 步）· `FilterMethods__MeetsConditionCriteria.c:170-174 / :384-389` ·
`CardScript__ResolveActiveAbilityPlayed.c:23-36` · `CardScript__UsedActiveAbility.c:29/34/41/50` ·
`CardScript__OnTurnEnd.c:209-210` · `CardScript__ResetToValuesInHand.c:131` · `BattleManager__ResolveResetDuty.c:6` ·
`il2cpp_out/dump.cs:21979 / :22043 / :22159 / :22735`。全文 → 判据正本。

### 3. 数据面普查（仓库外探针 `census`，单份口径 = `cards_engine.json`）

池 **1128** 张 · **挂着 `TargetCondition` 的 op = 83 条**：

| 档 | 量 | 说明 |
|---|---|---|
| **① 闸会命中**（原版不求值 `cond`/`ex`） | **20 条 / 19 张**（`ta=0`：6 · `ta=20`：4 · `ta=30`：10） | 含 `BL64`（341,ex=1）· `BL50 Execution`（40,ex=0）· `SAU17`/`SAU19`/`SAU41`/`SAU20`/`SAU36`（340）· `EC55`/`GOF61`/`GSC17`/`TL75`/`UM3`/`SW22`（220,ex=1）… |
| **② 判不了**（`ta` 读不到 ⇒ **不闸 + 出声**） | **12 条** | 两类：`role=target` 但 `(ab,lg)` 在 `abilityTargetCriteria` 里没有行（`AM69`/`GOF12`/`GOF24`/`GOF17`/`TL37`/`TL30`/`UM15`/`AM21`）· `role=cardtarget`（`ab=-3`，卡级 criteria 我们没建模：`SAU61`/`Feeding Frenzy`） |
| **③ 照旧筛**（原版确实要值） | `ta=10`：49 条 · `ta=60`：1 · `ta=90`：1 | 与正本「18 行在 `{0,20,30}` 上」互相印证（口径差：我按**挂上 op 的**条数算，正本按 `unitConditions` 行算） |

> ⚠️ ①的 20 条是**闸的覆盖面**，不等于「20 条行为都变了」：不少 handler 根本不把 `op.TargetCondition`
> 传给 `ResolveTargets`，所以实际改变只有 `play check` 看得见的那 1 张（`BL64`）。
> ②的 12 条会在战斗日志里各喊一次 `[RuleEngine] 结构化条件（W64·B-16）：…`（同 key 去重，**不是刷屏**）。

---

## 三、没查清的部分（⛔ 不猜、没有拿猜测填空）

1. **「`DutyUsed` 那道闸」现在与 `+0x4c` 不同源了**：`CanUseAlternative` 对 `duty` 仍按 `DutyUsed` 拦
   （`RuleCore.cs:11895`）。原版 `BattleManager__CanUseActiveAbility.c:93-94` 读的是 **`+0x4c`**
   ⇒ 若某单位用掉了 `Ability:`（`UseAbilityCore` 只置 `ActiveAbilityUsed`/`UsedActiveAbilityThisTurn`、**不置 `DutyUsed`**），
   原版会拒它再用 duty，我们**不会**。**这条今天零数据面**（池内同一张卡同时有 `Ability:` 与 `duty` 的组合未普查），
   但它是同一族的偏离 ⇒ 立账，本件不改（会动 `UseAlternative` 的准入码，风险高于收益，且判据只看过一处）。
2. **`role=cardtarget` 的 `ta`**（12 条里的 `ab=-3` 那几条）：原版那一位是**卡级** `cardTargetCriteria.targetsAffected`
   （`RawCardScript__IsValidTarget.c:44`），我们**没建模** ⇒ 闸判不了、维持旧行为 + 出声。属正本 §三·2 那条账。
3. **`ferocity` / `agenda` 是否 100% 走 `_ResolvePlayActiveAbility_d__479`**：沿用正本 §三·1 的结论
   （正面证据齐、**action 准入点没逐行核**）。我的置位是按「四族同一链」写的，如果这一条将来被推翻，置位点要跟着改。
4. **`count` 行的 `ex` 该不该取反**（正本 §三·4）：本件未动 `CountEval`，池内 6 条带 cond 的 `count` 行 `ex` 全 0 ⇒ 今天不可区分。

---

## 四、改动清单（行数增量 —— 供 `check_ledger_points --fix` 对照）

> 🔴 五个文件全在账的 `文件:行号` 引用里。**新成员一律追加到类尾**；方法体内的必要行**没有为凑零行数而牺牲可读性**。
> 下面给的是**各插入点之后整体下移多少行**（下游 `A` 档落点会漂这么多）。

| 文件 | 净增量 | 插入点（漂移从这里开始） |
|---|---|---|
| `TargetCriteriaEval.cs` | **+50** | 全部在**类尾**（`EarlyReturnTargetsAffected` 起）⇒ **零漂移** |
| `UnitState.cs` | +65 | `:746-759` doc（**零行数变化**）· `:2712` 附近 **+2** ⇒ 2712 之后 +2 · 类尾字段（零漂移） |
| `RuleCore.cs` | +5 | `:11533` **+1** · `:11738`（同行改，+0）· `:11961-11964` **+4** ⇒ 11533 之后整体 +1，11961 之后 +4 |
| `ConditionEval.cs` | +6 | `:547` 注释 **+6** ⇒ 547 之后 +6（该文件账引用 >547 的只有 **1** 条） |
| `EffectResolver.cs` | +14（不含类尾） | `:2011` **+2** · `:2168` **+2** · `:2357` **+1** · `:2360` **+1** · `:15071` **+4** ⇒ 2011 之后 +2、2168 之后 +4、2357 之后 +5、15071 之后 +9；类尾三方法（零漂移） |

> 账引用分布（现读）：`EffectResolver.cs` 341 条（>2351 的 **308**）· `RuleCore.cs` 516 条（>11533 的 **112**）·
> `UnitState.cs` 72 条（>760 的 64）· `ConditionEval.cs` 22 条（>547 的 **1**）· `TargetCriteriaEval.cs` 17 条。
> ⚠️ **`--fix` 只能在「没有 `.cs` 写手 + 补丁已合并完」时由调度台跑一次**（CLAUDE.md 18·b/§三）。

**账补丁**：`账/_parts/B9_W27ENG.tsv`（11 列 · 表头在第一行 · 3 行）
1. `收藏 | FilterMethods | CheckIfMeetsCriteria(...)` —— **就地追加**到 `备注`（这条是**同一方法的另一个消费侧**：cond-only 旁路不看 `ta`）。
2. `战斗逻辑驱动 | CardScript | UsedActiveAbility()` —— 就地追加（第一格 `+0x4c = 1` 已补齐）。
3. `战斗逻辑驱动 | EntityScript | set_usedActiveAbility(bool value)` —— **新行**（全账 0 命中），记新位 `ActiveAbilityUsed` 的写点/清点/不重置。
> `python -I -X utf8 工具/check_patch_conflicts.py` ⇒ ✅ 没有跨补丁重复键。

---

## 五、指纹影响表（`play check` 变红的**逐张逐列**归因）

**结算指纹**：当次现算 `b731235b470bd9fc697c8d7ec05177e2` · 基线 `0766fbe1f712696f4e57d2b7cfeb8f38`（不同）。
**主档 1128 张里恰好 1 张变**：`BL64`（新增 0 · 消失 0）。**事件电池 586 张逐字节一致**（✅ 没有一张变）。

`BL64 Murderous Desires`（tactic · 21 列对拍，8 列变）：

| 列 | 基线 | 现在 | 归因 |
|---|---|---|---|
| `playRc` | 18 | **0** | 18 = `ErrNoTargetAvailable`。改动 A 之前目标池被死字段 `cond=341 ex=1` 筛空 ⇒ 打不出去 |
| `played` | 0 | **1** | 同上：真的打出去了 |
| `mech` | 0 | **3** | 结算真的跑了 3 条机械变化（`forceattack` 那一支：强制攻击 + 目标掉血 + 攻击方反击致死） |
| `other` | 0 | **5** | 其余状态变化 5 条（同上，同一次结算的产物） |
| `energyDelta` | 0 | **-4** | 卡费 4（`expectedPay` 两版都是 4）⇒ 只在这一版真的扣了 |
| `handCostDelta` | 0 | **-4** | 同上（手牌总价） |
| `dmg` | 0 | **2** | 被迫攻击那一下的 2 点伤害（`FIX_A` 2 攻打 `FIX_E1`） |
| `changes` | 0 | **9** | `mech + other` 的合计口径 |

**为什么只有这 1 张**：域内命中的另外 19 张（§二·3 的①）里，`play check` 的夹具与读数口径看不见变化
（handler 没把该 `op.TargetCondition` 传给目标解析、或那一档的 cond 本来对夹具里的候选全放行）。
**B 的改动在结算层不可见**是**预期**的：`play all dump` 结构上看不见 AI（CLAUDE.md §三 那条），
而 `cond=341` 池内只有 4 张、其中 3 张只进 AI 打分。⇒ **B 的阳性证据是上面的 `MeetsUnit(341)` 读数，不是指纹。**

**没有做的事（⛔ 明说）**：没有改基线文件、没有改 `PinnedPlayMd5`、没有 `--fix`、没有动 `账/` 主表。

---

## 六、顺手发现（⛔ 均未改，交调度台分流）

1. 🔴 **必改后续（会红一条自检）**：`Core/Editor/RuleEngineTest.cs:37253` 把条件 341 钉在 `u.DutyUsed = true` 上
   （`CheckTrue(MeetsUnit(341) == false)`）。**改读 `ActiveAbilityUsed` 之后这条会红** ——
   修法就一行：`u.DutyUsed = true;` → `u.ActiveAbilityUsed = true;`（`u.DutyUsed` 那一行可留可删）。
   ⛔ **本件没有动它** —— `RuleEngineTest.cs` 不在本波的文件所有权清单里（避免与别的代理撞车）。
   **在同步点跑 `RuleEngineTest.Run` 之前请先改这一行**，否则会出现一条「假红」。
2. 🔴 **`Duty` 徽标读错位**：`CardPresentation/Battle/BattleDriver.cs:27866` 写的是
   `if (key == KeywordTable.Duty) return !u.UsedActiveAbilityThisTurn;` —— 原版 `CardTraitDuty__IsActive.c:19`
   读的是 **`+0x4c`**（= 我们的 `ActiveAbilityUsed`，**本局一次、不随回合重置**）。
   ⇒ 徽标应在用掉一次主动能力后**整局**灰着，而我们每回合就恢复。改成 `!u.ActiveAbilityUsed` 即可
   （`BattleDriver.cs` 属 W19 的域，本件没动）。
3. `UnitState.cs:1819-1822`（`RefreshForNewTurn` 里清 `UsedActiveAbilityThisTurn` 的那段注释）——
   订正后语义正确（`+0x50` 那一族按回合清），**但**那段注释仍写着「原版 `CardScript__OnTurnEnd.c:209`
   和上面那个计数器在同一行附近清零」；今天成立、且已在上面 746-759 的 doc 里写清，**建议保持原样**（再动会增加行号漂移）。
4. `Data/AiTargets.cs:574-592`：AI 那条路**已经有** `ta` 早返（`0/20/30/40/80`），所以**不需要**第二个 `ta` 闸
   （AI 走 `ai_scoring.json`，现读 763 行里 `ta=30` 的有 359 条、**带 cond 的 0 条**）。
   ⚠️ 但它**没有 `ta=70 (abilityOwner)` 那一档**（应回 `候选 == thisCard`）—— 现池 `ta=70` **0 行** ⇒ 今天无影响，立账。
5. `资料/完全复刻/判据/主档夹具缺口_评估.md:131-132` 那句「(a) 修 `cond=341` 的 `ex` 语义」**应作废**
   （方向是 `ta` 闸 + 状态位，不是 `ex`）—— 与 `B9_R1DUTY` 的报告同一条，正本 owner 未改，**请调度台一并处理**。
6. `EffectResolver.cs:15024`（`DoReloadDuty` 头注）还写着「`DutyUsed` 在原版对应的那个「本局用过」位（`+0x50` 一带）
   **没有**能一次指全的方法可引 ⇒ 判据查不到」—— 现已被 `BattleManager__ResolveResetDuty.c:6` 推翻（**能指全**，
   而且指的就是 `+0x4c`）。⛔ 为控行号漂移**没改**这句，建议与下一次该文件的大改一起订正。
