# B-179 verb 普查

> 对象：`Assets/RuleEngine/Core/EffectResolver.cs` 里全部 `ResolveTargets(...)` 调用点。
> 问题：**还有没有别的 verb 像 `forceattack` 一样「criteria 描述的不是 pick 目标」**。
> 做法：逐个调用点读它传的 `spec` / `structuredCondition` / `op`，判 criteria 到底筛的是哪一池。
> 红线：只读；所有行号一律 2026-11-01 现读（`grep -n "ResolveTargets(" EffectResolver.cs`），行号会漂，引用前请现读。

---

## 〇、口径说明（判「传没传 criteria」用的两条通道）

`ResolveTargets` 的 criteria 有**两条**进入路径，判的时候必须一起看：

1. **显式形参** `structuredCondition`（第 6 个参数）→ `ApplyTargetFilters(... structuredCondition ...)` → `ApplyStructuredConditionFilter`（`EffectResolver.cs:1754` / `:2349` / `:2448`）。
2. **`op` 形参**（第 7 个参数）→ `ApplyTargetCriteriaFilter(ctx, pool, op, ...)`（`EffectResolver.cs:1759` / `:2364`）。它**无条件**读 `op.TargetCriteriaSet` 与 `op.TargetCondition`（`:2368` / `:2400`）；另外 `TargetsAffectedOf(op)` 也参与 `ApplyTargetFilters`（`:1754`）。

⇒ **只把 `structuredCondition` 传 `null` 并不等于「这条 criteria 不生效」**：只要 `op` 也传了且它带 `TargetCriteriaSet`，36 格 + `cond` 仍会在 `ApplyTargetCriteriaFilter` 里再判一次。下表把两条通道分别写出来。

### 调用点计数（与 `B111W_A` 的 25 差 1 的原因）

现读 `EffectResolver.cs` 里非注释、非声明的 `ResolveTargets(ctx` = **24 处**；加上 `:1440` 的**方法声明行** `public static List<UnitState> ResolveTargets(...)` 才是 `B111W_A` 记的 **25**。
`EffectResolverPartial_*.cs` / `RuleCorePartial_*.cs` 另有 **5 处**真实调用（不在 `EffectResolver.cs`，但属同一套 verb 解析，见 §一·B）。全仓真实调用共 **29 处**（不含 `Editor/RuleEngineTest*.cs` 的夹具调用）。

---

## 一、总表

### A. `EffectResolver.cs` 的 24 处真实调用

| # | 调用点（文件:行号） | verb / 方法 | 传的 criteria（两条通道） | 是「筛 pick 目标」吗 | 实际用途 | 判定（等价 / 真反例 / 需裁定） |
|---|---|---|---|---|---|---|
| 1 | `EffectResolver.cs:159` | `InsteadSkips`（`instead` 配对） | `structuredCondition = ops[baseIdx].TargetCondition`；**无 `op`** | **是**（对象 = 被替换那条 op 的目标池） | 只算目标、不结算：用被替换 op 的目标当 `instead` 条件的 referent | 等价（但 36 格 `TargetCriteriaSet` 没传，见 §五·2） |
| 2 | `EffectResolver.cs:1380` | `SetReferenceCards`（能力 380） | **无**（`ResolveTargets(ctx, owner, spec, source, chosen)`，既无条件也无 `op`） | **否（criteria 根本没传）** | 只按 callers 给的 `spec` 解析目标表，填 `acting.ReferencedCards` | **需裁定**（疑似漏传：方法头自己写着「目标表就是这条 logic 的 targetCriteria」，但 36 格没走） |
| 3 | `EffectResolver.cs:3054` | `DoDeal` | `op.TargetCondition` + `op` | **是** | 伤害目标池 | 等价 |
| 4 | `EffectResolver.cs:3309` | `DoForceAttack`（攻击者支） | `critOnDefender ? null : op.TargetCondition` + `critOnDefender ? null : op` | **条件性**：`critOnDefender == false` 时是；`true` 时**否** | `true` ⇒ criteria 描述的是**被打者**（`Target2` 池）⇒ 本处故意不拿它筛攻击者 | **真反例**（forceattack 本尊） |
| 5 | `EffectResolver.cs:3396` | `DefenderPool`（forceattack 受害者支） | `withCriteria ? op.TargetCondition : null` + `withCriteria ? op : null`；`spec = op.Target2` | **否**（对本调用点自身是「筛受害者」；相对 `op.Target` 的 pick 池**不是**） | criteria 筛的是**受害者池**（`Target2`），不是 `op.Target` 的攻击者池 | **真反例**（forceattack 本尊的另一半） |
| 6 | `EffectResolver.cs:3597` | `DoEachUnitDeal`（`eachunitdeal` / Shuriken 族） | **无**（`ResolveTargets(ctx, owner, all, null, null)`，无 `op`） | **否（criteria 没传）** | 只为把 `Count` 放开成 0、扫候选池；每个开火单位再自己随机挑 | **需裁定**（疑似漏传，见 §三·2） |
| 7 | `EffectResolver.cs:3674` | `DoHeal` | `op.TargetCondition` + `op` | **是** | 治疗目标池 | 等价 |
| 8 | `EffectResolver.cs:4270` | `DoDestroy` | `op.TargetCondition` + `op` | **是** | 摧毁目标池 | 等价 |
| 9 | `EffectResolver.cs:4317` | `DoStun` | `null` + `op` | **是**（若 `op.TargetCriteriaSet` 非空，仍由 `ApplyTargetCriteriaFilter` 生效） | 眩晕目标池；`null` 只关掉 `ApplyStructuredConditionFilter` 那一路 | 等价（附注：`null` 不等于关掉 criteria） |
| 10 | `EffectResolver.cs:4452` | `DoBlind` | `null` + `op` | **是**（同上） | 失明目标池 | 等价（同上） |
| 11 | `EffectResolver.cs:6762` | `DoReturn` | **无**（`ResolveTargets(ctx, owner, spec, null, chosen)`） | **否（criteria 没传）** | 按 `op.Payload` 拆出的词组逐个解析回手/回牌库目标 | **需裁定**（疑似漏传，见 §三·3） |
| 12 | `EffectResolver.cs:9469` | `DoExtraTrigger` | `op.TargetCondition` + `op` | **是** | 额外触发额度挂到目标身上 | 等价 |
| 13 | `EffectResolver.cs:11878` | `DoGive` | `null` + `op` | **是**（若 criteria 存在仍生效） | `give/gain/lose` 的目标池；`op.Target == null` 时退回「己方全体」 | 等价（**与 forceattack 不同族**：这是「无目标 = 己方全体」的已知近似，不是 criteria 换池） |
| 14 | `EffectResolver.cs:15006` | `DoSetCurrentHealth` | `null` + `op` | **是**（同上） | `setCurrentHealth(100)` 目标池 | 等价 |
| 15 | `EffectResolver.cs:15057` | `DoSetHealth` | `null` + `op` | **是**（同上） | `setHealth(9)` 目标池 | 等价 |
| 16 | `EffectResolver.cs:15152` | `DoDouble` | `op.TargetCondition` + `op` | **是** | 翻倍目标池 | 等价 |
| 17 | `EffectResolver.cs:15213` | `DoFerocityStay` | `op.TargetCondition` + `op` | **是** | 打「留在场上」标记的目标池 | 等价 |
| 18 | `EffectResolver.cs:15264` | `DoTriggerAbility` | `op.TargetCondition` + `op` | **是** | 强行触发关键词的目标池 | 等价 |
| 19 | `EffectResolver.cs:16243` | `DoJam` | `op.TargetCondition` + `op`；`spec = op.Target ?? 默认敌方一个` | **是** | Jam 目标池 | 等价 |
| 20 | `EffectResolver.cs:17273` | `DoExchange` | `op.TargetCondition` + `op` | **是**（criteria 描述 `op.Target`；另一半是固定的施放者，不走 criteria） | 交换的两个目标里，criteria 只管 `op.Target` 那一个 | 等价 |
| 21 | `EffectResolver.cs:18733` | `RunGrantedTriggerRow`（ca 370/400） | `null` + `ops[0]`；`spec = ops[0].Target` | **是**（criteria 属于 `ops[0]`，与 `spec` 同一条 op） | 逐目标登记 relentless/courtesy 触发 | 等价 |
| 22 | `EffectResolver.cs:19418` | `RunTransformUnitRow`（ca 85） | `null` + `ops[0]`；`spec = ops[0].Target` | **是**（同上） | 变身类行执行器 | 等价 |
| 23 | `EffectResolver.cs:19519` | `RunRemoveFromCemeteryRow`（ca 350） | `null` + `ops[0]`；`spec = ops[0].Target` | **是**（同上） | 墓地放逐行执行器 | 等价 |
| 24 | `EffectResolver.cs:20870` | `RunHighlightBloodThirstRow`（ca 480） | `null` + `ops[0]`；`spec = ops[0].Target` | **是**（同上） | 嗜血高亮行执行器 | 等价 |

### B. 其它文件里的 5 处真实调用（同一套 verb 解析，一并判）

| # | 调用点（文件:行号） | verb / 方法 | 传的 criteria | 是「筛 pick 目标」吗 | 实际用途 | 判定 |
|---|---|---|---|---|---|---|
| 25 | `EffectResolverPartial_B88E2.cs:883` | `DoDisappearUnit` | `op.TargetCondition` + `op` | **是** | `disappearunit` 目标池 | 等价 |
| 26 | `EffectResolverPartial_B88E2.cs:946` | `ResolveTakeControlTargets`（`stealminion` 180） | `op.TargetCondition` + `op` | **是** | 抢人目标池（185 那一支另走 `ActingUnit`，不到这里） | 等价 |
| 27 | `RuleCorePartial_B103W5.cs:52` | `B103W5RowTargets` | `null` + `ops[0]`；`spec = ops[0].Target` | **是** | 主动能力行共用的目标解析口 | 等价 |
| 28 | `RuleCorePartial_B103W5.cs:466` | `RunExchangeRandomFriendlyMinionRow`（ca 200，临时 criteria） | `null` + `null`（`ResolveTargets(ctx, p, critSpec, null, null, null, null)`） | **设计如此：否** | 原版 `.c:469-483` 的**临时 criteria** = 「己方随机一个部队」，与 op 的 criteria 无关 | 等价（合成 spec 自带 Side/Kind，不需要 op criteria） |
| 29 | `RuleCorePartial_B99E8.cs:63` | `RunTriggerRequiemRow`（ca 390） | `null` + `ops[0]`；`spec = ops[0].Target` | **是** | 取 requiem 入口目标（`targetCard` 只当入口闸） | 等价 |

---

## 二、真反例逐条（像 forceattack 那样的）

> 结论：**全仓只有 `forceattack` 这一个 verb** 出现「criteria 描述的不是 pick 目标」。它落成两个调用点（#4 / #5），是同一个机制的两半。

### 2·1 `EffectResolver.cs:3309` `DoForceAttack` 的攻击者支（criteria 被压掉）

代码（现读）：

```csharp
// :3308
// `critOnDefender` ⇒ 这一条 criteria **不**筛攻击者池（它描述的是被打的那个）。
attackers.AddRange(ResolveTargets(ctx, owner, op.Target, null, chosen,
                                  critOnDefender ? null : op.TargetCondition,
                                  critOnDefender ? null : op));
```

- `spec = op.Target` = **攻击者池**。
- 当 `critOnDefender == true` 时，`structuredCondition` 与 `op` 都被传 `null` ⇒ **criteria 完全不参与这个调用**。
- `critOnDefender` 的判据在 `:3270-3298`：`op.Target != null && op.Target2 != null && op.TargetCriteriaSet != null && Payload != "byitself"`，以及 `op.Target == null && op.Target2 != null && TargetCriteriaSet != null` 且 `ta` 不是身份/承接档的那一支。
- ⇒ 这正是「criteria 描述的不是 pick 目标」：criteria 描述的是**被打者**，所以这段故意不拿它筛攻击者。

### 2·2 `EffectResolver.cs:3396` `DefenderPool`（criteria 被挪到受害者池）

代码（现读）：

```csharp
// :3389
var spec = op.Target2;
if (spec == null)
    spec = new EffectTargetSpec
    {
        Raw = "(未写目标：全场敌方单位，由攻击合法性筛)",
        Side = "enemy", Kind = "any", Count = 0,
    };
var list = ResolveTargets(ctx, owner, spec, null, null,
                          withCriteria ? op.TargetCondition : null,
                          withCriteria ? op : null);
```

- `spec = op.Target2`（被打者）或合成的「全场敌方单位」。
- `withCriteria == true` 时把 `op.TargetCondition` + `op` 交给这个调用 ⇒ 36 格 + `cond` 筛的是**受害者池**。
- 对照 `op.Target`（攻击者池）在同一条 verb 里的解析（#4）：`critOnDefender == true` 时那边把 criteria 压成 `null`。
- ⇒ 同一个 `op` 的 `TargetCriteriaSet` 描述的是 `Target2` 池，不是 `op.Target` 池。**与 forceattack 的 reference 反例完全同形。**

### 2·3 为什么没有「别的 verb」

- `EffectOp.Target2` 的生产点只有 `EffectText.cs:7165/7175/7196/7205` 四处，**全部是 forceattack**（已知，本次现读复核 `grep -rn "Target2" Assets --include=*.cs`：`EffectResolver.cs` 之外的 `Target2` 命中全是无关类 / 测试夹具）。
- 其余 verb 的双目标语义（`DoExchange` 的「施放者 + op.Target」）里，criteria 只描述 `op.Target`，另一半是**固定对象**（施放者），不是「criteria 该筛的另一池」。
- `RunGrantedTriggerRow` / `RunTransformUnitRow` / `RunRemoveFromCemeteryRow` / `RunHighlightBloodThirstRow` 的 `spec` 与 `op` 都取同一条 `ops[0]`，不存在「spec 是一条、criteria 是另一条」的错配。

---

## 三、需裁定（判不了 / 疑似缺口）的

> 这三条**不是 forceattack 同族**（不是 criteria 被挪到另一池），而是**criteria 根本没传**。判据已够坐实「现在没筛」，但「原版这条 ability 到底有没有 criteria 行」需要现读卡数据/原版方法体才能定，故列「需裁定」。

### 3·1 `EffectResolver.cs:1380` `SetReferenceCards`（能力 380）

```csharp
var got = ResolveTargets(ctx, owner, spec, source, chosen);   // 无 conditioned、无 op
```

- 方法头（`:1349-1352`）自己写着：**「目标表从哪来：就是这条 logic 自己的 `targetCriteria` —— 这里直接复用 `ResolveTargets`」**。
- 但实际调用**没传 `structuredCondition`，也没传 `op`** ⇒ 36 格 + `cond` 都不生效，只剩 `spec` 自带的 `SubtypeFilter` / `KeywordFilter` 等。
- 调用方 `:20823`（`RunSetReferenceCardRow`）手上**有** `ops[0]`，与同族 `RunGrantedTriggerRow`（`:18733`）等的写法不一致。
- 需裁定：380 的 `abilityLogic` 是否可能带 `TargetCriteriaSet` / `TargetCondition`；若可能，本调用与「同族解析」口径分叉。

### 3·2 `EffectResolver.cs:3597` `DoEachUnitDeal`（`eachunitdeal`）

```csharp
var all = new EffectTargetSpec
{
    Raw = op.Target.Raw, Side = op.Target.Side, Kind = op.Target.Kind, Count = 0,
    DamagedOnly = op.Target.DamagedOnly, PrayedOnly = op.Target.PrayedOnly,
    SubtypeFilter = op.Target.SubtypeFilter, KeywordFilter = op.Target.KeywordFilter,
};
var pool = ResolveTargets(ctx, owner, all, null, null);   // 无 criteria、无 op
```

- 复制 `op.Target` 时**只挑了 7 个字段**：`Raw/Side/Kind/DamagedOnly/PrayedOnly/SubtypeFilter/KeywordFilter`。
- **没带** `NameFilter` / `NotKeyword` / `AnchorInSet` / `Adjacent` / `Random` / `Auto` / `Subjectless`，也**没带** `op.TargetCondition` / `op.TargetCriteriaSet`（因为连 `op` 都没传）。
- `chosen` 的丢弃有明说的理由（`:6758` 的注释把 `Shuriken` 这条列为「按语义应该丢」），但 **criteria / `NameFilter` / `NotKeyword` 没有同样的说明**。
- 需裁定：`eachunitdeal` 的 ability 是否可能带 criteria 或这些被丢的字段；若可能，这是**静默少筛**（不是打错池，但同样与原版不一致）。

### 3·3 `EffectResolver.cs:6762` `DoReturn`

```csharp
var targets = spec == null ? new List<UnitState>()
                           : ResolveTargets(ctx, owner, spec, null, chosen);   // 无 criteria、无 op
```

- `spec` 来自 `op.Payload` 按 `" and "` 拆出的词组（`:6643-6650`），不是 `op.Target`。
- 该调用**只把 `chosen` 修好了**（`:6755-6760` 的注释说明 2026-10-26 才补的 `chosen`），**criteria 仍然没传**。
- 需裁定：`return` 的 ability 是否可能带 `TargetCriteriaSet` / `TargetCondition`；若可能，回手/回牌库的目标池会**少筛一层**。

---

## 四、汇总

**`EffectResolver.cs` 的 24 处真实调用：等价 19 / 真反例 2 / 需裁定 3。**
**另加 5 处 partial 文件调用：等价 5。**
**全仓合计 29 处：等价 24 / 真反例 2 / 需裁定 3。**

- **真反例 M = 2**：都是 `forceattack`（`:3309` 攻击者支 + `:3396` 受害者支），同一 verb。
- 🔴 **2026-11-01 就地订正（`B112W_B193` 裁定后）**：上面「三处都是**漏传**」**只在 `SetReferenceCards:1380` 成立**。
  · `DoEachUnitDeal:3597` **不传是对的** —— 那份 criteria 描述的是**出伤单位池**（另一个池），传进敌人池会把敌人筛空（决定性样本 = `ASH46 Aerial Dominion`：lg0 敌人 / lg1 友好+Vehicle）。⛔ **绝不能传 op**。
  · `DoReturn:6762` **等价**（可观测差异 0：17 张产出 return，只有 `ASH13` 真贴了 criteria 且是 `ta=20 self`；其余根本没贴）。
  · `InsteadSkips:159` **等价**（12 张含 instead，base op 的 criteria 全为 `ta=30/90` 或没贴，而 `chosen` 恒 null ⇒ 筛被跳过）。
  ⇒ 真缺陷 **0** / 等价 **2** / 不可达 **1**（`SetReferenceCards` 的 ca=380 在池内 `activeAbilityLogic` = 0/1129）。裁定全文 → `判据/B112W_B193_criteria静默丢弃裁定.md`。
- **等价 N = 24**：其余 verb 的 criteria 都筛同一个 `spec` 的 pick 目标（其中 5 处只传 `null` + `op`，criteria 仍经 `ApplyTargetCriteriaFilter` 生效；1 处 `B103W5:466` 是设计如此的无 criteria 合成池）。

**对 B-179 的直接回答**：像 `forceattack` 那样「criteria 描述的不是 pick 目标」的 verb，**除 forceattack 外没有第二个**。但普查顺带查出**三条「criteria 被静默丢掉」的调用点**，性质不同、同样需要裁定（见 §三）。

---

## 五、顺手发现

1. **`structuredCondition = null` ≠ 关闭 criteria**（影响 §一里 5 个「null + op」调用点）。
   `ApplyTargetCriteriaFilter`（`:1759` → `:2364-2416`）**无条件**读 `op.TargetCriteriaSet` / `op.TargetCondition`，与第 6 个形参无关。
   ⇒ `DoStun:4317` / `DoBlind:4452` / `DoGive:11878` / `DoSetCurrentHealth:15006` / `DoSetHealth:15057` 这几处即使传 `null`，只要 `op.TargetCriteriaSet != null`，criteria 还是会生效。
   ⇒ 若当年的意图是「这几条不筛 criteria」，那**没有达到目的**；若意图只是「不在 `ApplyTargetFilters` 里重复判一次」，则行为无害（同一谓词判两次幂等）。**建议按前者复核**（这也是「null 丢 criteria」这个说法在 `B111W_A` 记录里的歧义来源）。

2. **`InsteadSkips:159` 只传 `cond`、不传 `op`**。
   它用 `ops[baseIdx].TargetCondition` 解析被替换 op 的目标，但 `ops[baseIdx].TargetCriteriaSet` 那 36 格**没参与**。
   若被替换 op 带 36 格 criteria，这里算出的 referent 目标可能与真正结算（`DoDeal:3054` 等传了 `op`）**不是同一池** ⇒ `instead` 条件可能判在另一个目标身上。
   ⚠️ 本次未逐卡验证是否有这种卡；建议下一步用「`instead` op + 36 格 criteria 非空」的卡做一条对拍（尤其 `SW53` / `TAU58` 那族）。

3. **`SetReferenceCards:1380` 与同族 Run*Row 的调用形状不一致**：后者都是 `ResolveTargets(..., null, ops[0])`（`:18733` / `:19418` / `:19519` / `:20870`），`SetReferenceCards` 却是 `ResolveTargets(..., null, chosen)`（无 `op`）。这是 §三·1 的可操作修法入口。

4. **`DoEachUnitDeal` 重建 spec 时丢的字段不止 criteria**：`NameFilter` / `NotKeyword` / `AnchorInSet` / `Adjacent` 都没复制（`:3591-3596`）。若这些字段在池内与 `eachunitdeal` 共现过，就是**静默少筛/多打**。本次未做池内共现统计（需要解析后的 op 表，不在本次只读范围内）。

5. **测试夹具也在调 `ResolveTargets`**：`Editor/RuleEngineTest.cs:19289/34745/37160/37167/37174`、`Editor/RuleEngineTest_EngineC5.cs` 等。它们是夹具、不是 verb，本次未计入 29 处；但若将来改 `ResolveTargets` 的 criteria 语义，这些插桩会跟着受影响。

6. **`ta = 60`（墓地）那一支的事实不变**：`ResolveTargets` 收口在 `:1446` 对 `op.SubjectTargetsAffected == 60` 走 `ResolveCemeteryTargetsAsUnits`，返回空 `List<UnitState>`（候选是 `CardInstance`）。这不属于「criteria 换池」，但它是 `RunRemoveFromCemeteryRow:19519` 今天解出空表的原因（`:19504-19509` 已如实标注）。
