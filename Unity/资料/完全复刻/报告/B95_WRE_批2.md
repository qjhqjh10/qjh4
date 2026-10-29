# B95_WRE 批 2 — `RuleCore.cs` ＋ `EffectResolver.cs`

> 文件所有权（本批）：`Assets/RuleEngine/Core/RuleCore.cs` · `Assets/RuleEngine/Core/EffectResolver.cs`
> ＋ `EffectResolverPartial_B88E2.cs` · 新建 `Assets/RuleEngine/Editor/RuleEngineTest_EngineB95WRE.cs`。
> ⛔ 没碰 `Editor/RuleEngineTest.cs` / `EffectText.cs` / `BattleContext.cs` / 账主表 / 正本。没跑 Unity、没跑 ruleprobe、没动 git。
> **typecheck**：`TMPDIR=/tmp/wf_WRE2 bash d:/4/Unity/工具/typecheck.sh` ⇒ 本批结束时 **运行时 0 / 编辑器 0**（随后出现的几条都在 `CardPresentation/**`，是别的写手的文件）。

---

## 一、结论

| # | 任务 | 结果 |
|---|---|---|
| 1 | 账 647「外加那一支」 | ✅ **已实现**（`RuleCore.EmitHit` 带 `attacker` ＋ `EffectResolver.BroadcastUnitDamagedExtraAttacker`） |
| 2 | 账 521③ 改真调用 | ⛔ **驳回并给反证**（既有口径就是 `Auras.RefreshInPlayNumericAndKeywords`；改成 `FireTriggerAt` 会叠 buff、且缺「算不在场」那一格） |
| 3 | 账 658 生产点核对 | ✅ **已核 = 不一致**（原版闸是 `buffType == 200`，我们从未建模；生产点也窄一条）⇒ 未改代码，理由见账 |
| 4 | 账 517② 施工单 | ✅ 写在本文 §3（**没有**加形参） |

**顺带修掉一处编译级缺陷**：`RuleCore` 的 `internal ApplyDamage` 只有 7 个形参，而 `HurtCore:9063` 按 **8 个**实参调 ⇒ `CS1501`、**整个 `RuleCore.cs` 编不过**（`B95_WRC` 上一批留下的）。本批补齐 `int attackerSeat = -1`。

---

## 二、改动清单（全部**零行数变化**或**类尾追加**）

### 1. `Assets/RuleEngine/Core/RuleCore.cs`（**17,139 → 17,139 行，逐字未变**）

| 行（改后现读） | 改了什么 |
|---|---|
| `:8458` | `ApplyDamage`（public）尾部内联追加 `, UnitState attacker = null` |
| `:8460` | 同一行把 `attacker` 透传给 `ApplyDamageCore` |
| `:8465` | `ApplyDamageCore` 尾部内联追加 `, UnitState attacker = null` |
| `:8948` | `EmitHit(ctx, u, _mainPart, kind: kind, attacker: attacker)` |
| `:8949` | `EmitHit(ctx, u, vulnSecondPart, vulnerableSecond: true, attacker: attacker)` |
| `:9063` | `HurtCore` 的 `ApplyDamage(...)` 末位再带 `actingCard`（= 施害卡） |
| `:9357` | `EmitHit` 形参表尾部内联追加 `, UnitState attacker = null` |
| `:9396` | `BroadcastWhen(..., actingCard: attacker)` |
| `:17048` | `internal ApplyDamage` 形参尾部补 `int attackerSeat = -1, UnitState attacker = null`（**修 CS1501**） |
| `:17050` | 同一行把两者透传给 `ApplyDamageCore` |

### 2. `Assets/RuleEngine/Core/EffectResolver.cs`（**19,065 → 19,128 行，+63 = 类尾追加**）

| 行 | 改了什么 |
|---|---|
| `:10025` | 内联追加第 4 句：`if (!boardAndPersistentOnly && subject != null && kind == WhenEventKind.Damaged) BroadcastUnitDamagedExtraAttacker(ctx, subject, actingCard, damageType);` |
| `:19066-19128` | **类尾追加**新函数 `BroadcastUnitDamagedExtraAttacker(...)` ＋ 判据注释（追加点 = 原来的类尾 `SummonRequestSlotFor` 之后） |

### 3. 新建 `Assets/RuleEngine/Editor/RuleEngineTest_EngineB95WRE.cs`（87 行）

入口 `CheckB95WREAttackerPlumbing()`，两条用例：
- **缺省 `attacker = null` ⇒ 行为与改动之前逐字相同**（伤害照落 5→3、信仰 0 变化）。
- **施害卡在场且未待死 ⇒ 本支早退**（⛔ 不许双响）。

🔴 **接线还差一行**（该文件归调度台）：`Editor/RuleEngineTest.cs` 的 `Run()` 里加
`Step(CheckB95WREAttackerPlumbing);`

### 4. 账补丁 `账/_parts/B95_WRE.tsv`

**14 行**（批 1 的 13 行原样保留 ＋ 新增 658 一行；其中 647 / 521 / 517 三行在批 1 文本后面追加了本批结论）。
`merge_ledger_parts.py --dry` 已验：**更新 14 · 新增 0 · 无变化 0**，无列数错、无签名拒收。

---

## 三、账 517② 施工单（**没有加形参** —— 只加形参 = 死代码）

**目标**：让 `RawCardScript.OnTriggerEffectInCard` 那一族（账 1278）与 `FollowUpResolutionPlayed(410)`（账 517）能带**两个卡槽**（`actingCard` / `targetCard`）跑正文。

**为什么现在做不到**：我们的 `FireTriggerAt` 只按**卡面关键词**从 `CardDef.TriggerOps(keyword)` 取正文；而 trigger **410** 在数据里是一条 `cardAbilities[].trigger`，卡面上**没有**对应的关键词句 ⇒ `TriggerOps` 恒 `null` ⇒ 加了形参也没人读。

**三步**（⛔ 顺序不能换）：

1. **`RuleCore` 开一个「按 trigger 数值取正文并结算」的执行口**
   等价物 = 原版 `RawCardScript.TriggerAbilities(trigger, …)`。
   取 ability 的判据已经在：`CardDef.UniqueAbilityIndexForTrigger(triggerName, out why, out ambiguous)`
   （`CardDef.cs:2709`；⛔ 别在别处再扫一遍 `AbilityTargets`）。
   缺的是「ability 下标 → 这条 ability 的 op」—— 现在 `_triggerOps` **只按卡面关键词建键**。
   ⇒ 要么生成器补一列「逐 ability 的正文 op」，要么解析层按 `abilityTargets[i]` 的 `trigger` 把 `EffectText.Parse` 的产物**归位**到 ability 下标（推荐前者：它是数据，不依赖文本猜）。

2. **那个口要能带两个卡槽**
   原版 `OnTrigger(trigger, mgr, thisCard, actingCard, targetCard, …)`（`RawCardScript.cs:373` 的具名形参表）。
   我们已有 `ctx.EventActingCard`（B94_B22 建的）＋ `ctx.EventTarget` ⇒ 两个槽**都在**，
   缺的是让「按 trigger 数值跑正文」那条路也走 `EventSlotsScope.TargetCardAndActing(...)`。

3. **最后**才把 `EffectResolver.EmitFollowUpResolution` 的 `targetCard` 透传进去（那一跳现在只发表现层事件，
   `EffectResolver.cs:7389` 明写「⛔ 不在这里结算任何效果」）。

**可达性**：池内 `abilityTriggers` 含 **410** 的有 24~30 张（各批口径不一，用前现扫），其中 **19 张**的第二半效果已被卡面正文覆盖 ⇒ 真受益面 ≤ 11 张。**用前必须现读**（判据会过期）。

---

## 四、账 521③ 的反证（为什么不按「改真调用」做）

原本的设想是「把 `RuleCore.FireTriggerAt` 开成 `internal`，在 `ResolveJamUnit` 里替掉 `Auras.RefreshInPlayNumericAndKeywords`」。**现读判据推翻了它**：

- 原版 `CardScript__UpdateWhileInPlay.c:15-30`：`RawCardScript.UpdateWhileInPlay(raw, mgr, this, param_2 ? this : null, 0)`，然后若 `+0x59` 为真再跑 `UpdateTriggerOnce`，再做 pack buff 重算。
- `RawCardScript__UpdateWhileInPlay.c:15,18`：**只**两趟 —— `TriggerAbilities(0x14 = 20 WhileInPlay)` ＋ `TriggerAbilities(0xf0 = 240 WhileInPlayVariable)`。
- 而「`UpdateWhileInPlay ×2 + BroadcastWhileInPlay` ⇒ `Auras.RefreshInPlayNumericAndKeywords`」**已经是本仓既有的、复核过的唯一口径**：`ActionPump.cs:143`（账 669 · `B9_E7P06` 的隔离读数）与 `EffectResolver.cs:17027` 都这么写。

⇒ 改成 `FireTriggerAt(WhileInPlay)` 有**两个**具体危害：
1. 它会**重跑 while-in-play 的能力正文**（没有去重）⇒ `+N attack` 那类会**叠**；
2. `FireTriggerAt` **没有**「这张卡算不在场」那个形参（原版靠 `param_4 = null` 表达），而 jam 那一支传的正是 `false`。

⇒ 若审查仍要逐字对齐，前置是**新机制**（一个「按 `UpdateWhileInPlay(bool)` 语义跑、且带去重 + 可传『算不在场』」的窄口），得先立判据再动手。本批**不动**这一格，也**没有**把 `FireTriggerAt` 改成 `internal`（`FireTriggerAtForTest` 早就是 `public`，另开导出 = 死代码）。

---

## 五、账 658 的核对细节

| # | 原版 | 我们 | 判 |
|---|---|---|---|
| 闸 | `BattleManager__ResolveAddEffect.c`：`*(int *)(cardEffect + 0x3c) == 200` | `RuleCore.cs:16143` `ApplyTraitEffect(KeywordTable.Poisoned, …)`（`buffType = BuffTypeAddTrait = 3`） | ❌ **不同一条闸**（`BuffType 200` 全仓**从未建模**：`EffectsToShow.cs:52-60` 只登记 2/3/8/17） |
| 时机 | 效果落账**之后** | `rec != null` 之后 | ✅ 对得上 |
| 施害者 | `cardEffect.actingCard`（**效果记录自己的**） | 当场打人的 `actingCard` | ❌ 来源不同 |
| 生产点 | **通用 `addEffect`**（任何 buffType 200 落账都广播） | 只有「`poisonous` 关键词命中」**一条** | ❌ 窄一条 |
| 拒收分支 | 毒 × 抗性 ⇒ 不追加记录 | `UnitState.cs:1131` 返 `false` ⇒ `rec == null` ⇒ 不广播 | ✅ 对得上 |

**可达性**：池内 `abilityTriggers` 含 **290 = 0 张**；全池提 "poison" 的只有 `GSC10 Biophagus`，它造的是**陷阱卡 `Poisoned Supplies`**，不是毒 trait ⇒ **零可观测**。
**要逐字一致**得先把 `BuffType 200` 建模（`UnitState` 的 buff 表 + `ApplyTraitEffect` 的 buffType 语义）⇒ 属 `UnitState`/数据线。

---

## 六、`RuleCore.cs` 行号对照

**本批对 `RuleCore.cs` 的 10 处改动全部是「在同一行内追加文本」⇒ 行数 17,139 一丝未变，345 个账落点零漂移。**
机检佐证：`check_ledger_points.py 战斗逻辑驱动.tsv` ⇒ A 998 条、失败 0；漂移数在跑的过程中从 8 掉到 1（**全部在 `CardPresentation/**`，是别的写手正在改的文件**），**`RuleCore.cs` / `EffectResolver.cs` 的落点一条都没漂**。

`EffectResolver.cs` 唯一会动行号的是**类尾 +63 行**（`:19066-19128`）—— 追加点在最后一个 A 落点（`:18440`）之后 ⇒ 同样零漂移。
