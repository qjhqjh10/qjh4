# B-193 三处 criteria 静默丢弃裁定

> 对象：`Assets/RuleEngine/Core/EffectResolver.cs` 三个**没传 criteria** 的 `ResolveTargets` 调用点
> （`:1380 SetReferenceCards`（能力 380）· `:3597 DoEachUnitDeal` · `:6762 DoReturn`）
> ＋ `:159 InsteadSkips` 只传 `cond` 不传 `op` 那一条。
> 输入普查 → `判据/B112W_B179_verb普查.md` §三。
> 红线：只读；**所有行号一律 2026-11-01 现读**（本文件写完后别当现行行号用，引用前现读）。
> 池内统计口径：`Assets/RuleEngine/Resources/cards_engine.json`（现读 `version 45` · `count 1129`）。

---

## 一、总表

| # | 调用点（`EffectResolver.cs`） | 原版会不会传 criteria | 池内可达张数 | 判定 | 改法 |
|---|---|---|---|---|---|
| 1 | `:1380` `SetReferenceCards`（能力 **380** `setReferenceCard`） | **会** —— 原版这一跳读的是 `AbilityLogic.PlayAbility` 算好的 `*puVar1`，而它 = `AbilityLogic.GetTargets(...)`（**逐候选过 criteria**） | **0**（`activeAbilityLogic` 里 `ca=380` = **0 / 1,129**；池内含该码的 3 张全在 `cardAbilities`，本引擎不执行） | **不可达**（池内 0 张） | 休眠件：给 `SetReferenceCards` 加 `EffectOp op = null` → `ResolveTargets(..., op.TargetCondition, op)`；调用点 `:20823` 传 `ops[0]`。**可做，但今天零可观测** |
| 2 | `:3597` `DoEachUnitDeal`（`eachunitdeal`） | **会，但筛的不是这条调用点建的池** —— 原版 260 `triggerEffectInCard` 的 criteria 筛的是**出伤/重复单位池（己方）**；`:3597` 建的是**敌人池** | 产出 `eachunitdeal` 的 **3 张**（`ASH43` / `GOF56` / `UM82`），其中**真贴了 criteria 的 2 张**（`ASH43` / `UM82`） | **等价**（对这条调用点：⛔ **绝不能传**，传了会把敌人池筛空） | **不传**。真正要修的是**出伤循环**（另一半，见 §二·2）：对每个开火单位过一遍 `TargetCriteriaEval.Matches(..., op.TargetCriteriaSet, ...)` |
| 3 | `:6762` `DoReturn`（`return`） | **会** —— 原版 140/150/300/301 四档都遍历 `*puVar1`（同一份 criteria 过滤后的表） | 产出 `return` 的 **17 张**，其中 **只有 `ASH13` 的 return op 真贴了 criteria**（`ta=20 self`），其余 16 张因**多 op / 多 ability** 而 `AttachTargetCriteria` 不贴 | **等价**（今天可观测差异 = **0**） | ⛔ **别一刀切传 `op`**（一条 op 拆两个 spec 的形状会让第 2 个 spec 共用第 1 个的 criteria）。今天不传 = 与原版同结果 |
| 附 | `:159` `InsteadSkips` | 会（同一份 `*puVar1`） | 含 `instead` 的 **12 张**（1 张是静态规则 `SAU33`，真效果 **11 张**）；它们的 base op 的 criteria 全是 `ta=30/90` 或**根本没贴** | **等价**（今天可观测差异 = 0） | 不改；理由见 §三 |

---

## 二、逐条明细（含证据）

### 2·0 先立一条共同事实：原版**所有** verb 的目标表都过了 criteria

判据（第一权威，`d:/2/tools/decomp_full/`）：

- `AbilityLogic__PlayAbility.c:262-264`
  ```c
  uVar14 = AbilityLogic__GetTargets(param_2,uVar12,param_3,1,0);
  puVar1 = (ulonglong *)(param_1 + 0x78);
  *puVar1 = uVar14;            // ← 这条 logic 的目标表
  ```
  `uVar12 = *(undefined8 *)(param_1 + 0x28)`（`AbilityLogic.targetCriteria`，`AbilityLogic.cs:16-17` 的 `+0x28`；本仓口径见 `CardDef.cs:2581`）。
- `AbilityLogic__GetTargets.c:1086-1095`：建完池后**逐候选**调
  `FilterMethods__CheckIfMeetsCriteria(param_3, uVar8, param_5, param_2, 0)`；
  `CheckIfMeetsCriteria.c:48-59` 第一句就是 `param_4（= criteria）+0x10`（`targetsAffected`）`== 0 ⇒ return 1`（`any` 档不筛），其余逐格判。
- ⇒ **`PlayAbility` 里每一个 `case`（0x17c / 0x104 / 300 / 140 / 150 …）拿到的 `*puVar1` 都是「已过 criteria」的那一份表**。
  这是三条裁定的共同前提：**原版会传，我们这三处都没传**。差别只在「这份 criteria 描述的是哪个池」。

---

### 2·1 `EffectResolver.cs:1380` `SetReferenceCards`（能力 380）

**代码（现读）**
```csharp
// :1380
var got = ResolveTargets(ctx, owner, spec, source, chosen);      // 无 structuredCondition、无 op
```

**原版会不会传 —— 会**
- `AbilityLogic__PlayAbility.c:2128` `if (iVar8 != 0x17c) { return; }` → `:2129-2160` 就是 380 那一支；
  它遍历的 `*puVar1`（`:2130` 的 `if (*puVar1 == 0)`、`:2144-2153` 的循环）正是 `:262` 交下来的**那张 criteria 过滤后的表**。
- ⇒ 380 **不该自己重新解析目标**，它读的就是「这条 logic 的 `targetCriteria` 结果」——
  与**我们方法头自己写的那句**（`:1349-1352`「目标表从哪来：就是这条 logic 自己的 `targetCriteria`」）**字面一致**，只是代码没照它做。

**池内可达张数 = 0**
- `cards_engine.json` 现扫 `activeAbilityLogic` 的 `ca` 直方图 = `0/10/20/30/40/60/70/75/100/130/140/238/300/320/325/420/462/467/468` —— **无 380**（其中 132 张卡的 `activeAbilityLogic` 非空）。
- 池内含 `380` 的只有 **3 张**：`AM54 At All Costs` / `GOF93 Hunta Rig` / `GOF104 Drag it Down`（顶层 `chosenAbility` 数组里出现）。
  但这三张的 `activeAbilityLogic` = **`[]`**（`activeAbilityTargets` 也 = `[]`）⇒ 380 落在 **`cardAbilities`（trigger 410）**里，
  而本引擎的触发式能力**只按正文 op 跑**（`CardDef.TriggerOpsByNumber` / `FireTriggerAt`，见 `EffectResolver.cs:21012`），
  不按 `cardAbilities[].chosenAbility` 跑 ⇒ **380 这一支没有任何生产入口**。
- 唯一的调用链 `RunSetReferenceCardRow`（`:20819`）← `RunActiveRowOpsOrGrantedTrigger`（`:18716` 的 `if (ca == 380)`）
  ← `RuleCore.UseActiveAbilityCore`（`:15569-15589`）**只读 `activeAbilityLogic`** ⇒ 0 行。
  `SetReferenceCards` 其余调用点全在自检夹具里（`Editor/RuleEngineTest_G5.cs:256/306` · `Editor/B102EXEC_Assert.cs:178`）。

**判定：不可达（池内 0 张）—— 但代码口径确实与原版分叉**
（若将来 `activeAbilityLogic` 有了 380，这条就是静默少筛：三张卡的 380 criteria 是 `ta=200/201/50`，
`ta=50` 那档会走常规格 —— `GOF104` 的 `friendlyUnits=1 / includeMinions=1` ⇒ 会筛掉一部分）。
注意 `ta=200/201` 在 `TargetCriteriaEval` 里是**返回 `null`（判不了）**、不是筛（`TargetCriteriaEval.cs` 的 `case 200/201`），
所以就算传了也不会走错支。

**可粘贴改法（休眠件对齐，风险极低）**
1. `EffectResolver.cs:1366` 的签名加一个可选形参：
   ```csharp
   public static int SetReferenceCards(BattleContext ctx, int owner, UnitState acting,
                                       EffectTargetSpec spec, UnitState source = null,
                                       UnitState chosen = null, EffectOp op = null)
   ```
2. `:1380` 改一行：
   ```csharp
   var got = ResolveTargets(ctx, owner, spec, source, chosen, op != null ? op.TargetCondition : null, op);
   ```
3. 调用点 `:20823`：
   ```csharp
   int n = SetReferenceCards(ctx, p, u, spec, u, chosen, ops.Count > 0 ? ops[0] : null);
   ```
   （与同族 `RunGrantedTriggerRow:18733` / `RunTransformUnitRow:19418` / `RunRemoveFromCemeteryRow:19519` /
   `RunHighlightBloodThirstRow:20870` 的 `ResolveTargets(..., null, ops[0])` 对齐。）
4. ⚠️ 三个形参都有默认值 ⇒ 两个自检夹具的调用**不用改**（行为不变）。

---

### 2·2 `EffectResolver.cs:3597` `DoEachUnitDeal`（`eachunitdeal`）

**代码（现读）**
```csharp
// :3591-3597
var all = new EffectTargetSpec
{
    Raw = op.Target.Raw, Side = op.Target.Side, Kind = op.Target.Kind, Count = 0,
    DamagedOnly = op.Target.DamagedOnly, PrayedOnly = op.Target.PrayedOnly,
    SubtypeFilter = op.Target.SubtypeFilter, KeywordFilter = op.Target.KeywordFilter,
};
var pool = ResolveTargets(ctx, owner, all, null, null);      // 敌人池；无 criteria、无 op
```

**原版会不会传 —— 会，但那 criteria 描述的是【出伤/重复单位池】，不是这条调用点建的敌人池**

- `eachunitdeal` 对应原版 **260 `triggerEffectInCard`**（`PlayAbility.c:2534` 的 `if (iVar8 == 0x104)`）：
  - `:2536-2545`：新建一张表，把 `*puVar1`（criteria 过滤后的目标）逐个加进去；
  - `:2851-2876`（`LAB_18081e1c0`）：对表里每个目标调 `CardScript__TriggerOtherCardAbility(target, …)` ⇒ **每个「目标」各跑一次这条效果**。
  - `CardScript__TriggerOtherCardAbility.c` → `RawCardScript__OnTriggerEffectInCard` → `RawCardScript__TriggerAbilities`（trigger `0`）⇒ 一次完整的效果重放。
- **决定性判据 = `ASH46 Aerial Dominion`**（池内 260 的样例，B91 表也用它）：
  ```
  卡面：Deal 3 damage to a random enemy. Repeat this effect for every friendly Vehicle
  abilityTargets: [{trigger:0, ta:[10,10]}]   chosenAbility: [10, 260]
  lg=0 criteria: targetsAffected=10, enemyUnits=1, includeHero=1, includeMinions=1, includeRemnants=1
  lg=1 criteria: targetsAffected=10, keywordFilter=10(Vehicle), friendlyUnits=1, includeMinions=1
  ```
  ⇒ **lg=1（260 那条）的 criteria 是「己方 Vehicle」= 重复次数 / 出伤单位池**；
  **lg=0（伤害本身）才是「敌方」池**。两条 logic 各自一份 criteria，**不共用**。

⇒ 结论：`:3597` 这条 `ResolveTargets` 建的是「随机敌人」池，**若把 260 的 criteria 传给它，会当场筛空**：
`TargetCriteriaEval.Matches` 的 ② 阵营闸（`TargetCriteriaEval.cs:222-237`）拿 `friendlyUnits=1 / enemyUnits=0` 判，
**所有敌方候选都会 `return false`** ⇒ `ASH43`/`UM82` 直接 0 伤害（静默变哑）。**现有写法是对的。**

**池内可达张数**
- 现扫 `desc`（三条正则与 `EffectText.TryEachUnitDeal` 同形）：产出 `eachunitdeal` 的只有 **3 张** ——
  `ASH43 Sudden Assault`（①`equal to its Shuriken`）· `UM82 Sergeant Gadriel`（②定值 `1-2`）· `GOF56 Deffkopta`（③`Other friendly <X>`）。
- **真贴得上 criteria 的只有 2 张**（`AttachTargetCriteria` 只在「块内恰好 1 条 op」时贴，`EffectText.cs:2106/2152`）：
  - `ASH43` criteria `ab=0 lg=0` = `ta=10, friendlyUnits=1, includeHero=1, includeMinions=1`（**无 includeRemnants / 无 includeSelf**）；
  - `UM82` criteria `ab=0 lg=0` = `ta=10, friendlyUnits=1, includeHero=1, includeMinions=1, includeSelf=1`；
  - `GOF56` 有 **2 条 ability** ⇒ 片段路 `abil.Count != 1` ⇒ **不贴**（`EffectText.cs:2132` 的 `if (abil.Count != 1) return;`）⇒ 与本条无关。

**判定：这条调用点 = 等价（⛔ 不要传 `op`）**
**但另一半是真缺口（另立账，不在本调用点修）**：
我们复刻「出伤单位池」用的是 `for (int s = 0; s < BoardSpec.Size; s++) { var u = mine.Board[s]; … }`（`:3607-3615`），
只筛 `IsAlive` / `op.Subject` / `OtherThanSelf`，**没过 `op.TargetCriteriaSet`** ⇒ 与原版的差异是：
- 原版 `includeRemnants = 0`（`ASH43`/`UM82` 都是默认 0）⇒ **残骸不当出伤单位**；
- 我们的 `Board` 扫描**包含残骸**（残骸就留在棋盘格上，`UnitState.IsRemnant`）。
- 可观测档：`UM82` 是**定值 1-2 伤害** ⇒ 场上有残骸时我们会**多打一次 1-2 点**；`ASH43` 是 `its Shuriken`
  ⇒ 残骸没有 Shuriken（`UnitState.cs:970` 的 `IsRemnant` 支只认 `_remnantTraits`）⇒ 打 0 点、仅多一条日志。
- ⚠️ **未查清**：两份 criteria 都带 `includeHero=1` ⇒ 原版督军**是否也作为出伤单位多打一次**（`UM82` 会多 1-2 点）。
  `TriggerOtherCardAbility` 下游（`TriggerAbilities` 的 `param_4` 用法）我读到的证据不足以确认督军那一档，
  **不猜**：要么 `play check` 对拍，要么把这条写进「未查清」等实机。

**可粘贴改法（只在出伤循环里加筛，⛔ 不动 `:3597`）**
```csharp
// :3607 的循环体内，紧跟 `if (u == null || !u.IsAlive) continue;` 之后加：
if (op.TargetCriteriaSet != null)
{
    string __why;
    bool? __ok = TargetCriteriaEval.Matches(ctx, owner, op.TargetCriteriaSet, u,
                                             op.TargetCondition, ctx.ActingUnit, null, out __why);
    if (__ok == false) { filtered++; continue; }          // `null`（判不了）按既有契约保留 + 出声
}
```
（`TargetCriteriaEval.Matches` 的 `null` 契约 = 保留候选并由 `StructuredConditionNotice` 出声 —— 与
`ApplyTargetCriteriaFilter` 同一口径，别在这里另立一个「null 当 false」。）

---

### 2·3 `EffectResolver.cs:6762` `DoReturn`（`return`）

**代码（现读）**
```csharp
// :6761-6762
var targets = spec == null ? new List<UnitState>()
                           : ResolveTargets(ctx, owner, spec, null, chosen);   // 无 criteria、无 op
```

**原版会不会传 —— 会**
- `PlayAbility.c:2164` `if (iVar8 == 300) {`（`recallToDeck`）→ `:2166-2192` 遍历 `*puVar1`、逐个 `BattleManager.AddRecallToDeck`；
  `:2194-2224`（301 `recallToDeckToTop`）、`:541-586`（140 `recallToOwnerHand` / 150 `recallToCasterHand`）同一形状。
- ⇒ **回手/回牌库的目标池 = criteria 过滤后的 `*puVar1`**（同 §2·0），我们这一跳没传。

**池内可达张数 & 可观测性**
- 现扫 `desc`（按 `EffectText.TryReturn` 的两条正则 + `ReturnDests` 全等表）产出 `return` 的 **17 张**：
  `ASH13 · ASH36 · ASH72 · Path of the Seer · DA51 · Master of Manoeuvre · DA75 · GSC55 · GSC39 · GOF80 · GOF2 · SOR66 · TAU68 · TL16 · UM97 · UM110 · UM61`
  （`ASH72` 是 give-backlash，真正的 return 在后续 Backlash；`UM97` 的 `abilityTargets` 为空 = 无 criteria 列）。
- **`AttachTargetCriteria` 只贴「块内恰好 1 条 op」**（`EffectText.cs:2106` 整条 desc / `:2152` 片段）⇒ 现扫只有 **`ASH13 Warp Spider`** 的 return op 真贴上了：
  `Strike: Return this troop to your hand` → 片段路 `abil.Count == 1`（`abilityTriggers=[580]`）且 `ops.Count == 1` ⇒
  criteria `ab=0 lg=0 = { targetsAffected: 20 }`（**`self` 档**）。
- 其余 16 张都贴不上，原因逐张可核：
  - **块内多条 op**（`Return … and lower its cost …` / `Returns … and costs 2 more` / 后接 `It costs 4 less` / `Draw a card`）：`ASH36 · Master of Manoeuvre · DA75 · GSC39 · GOF2 · SOR66 · TAU68 · TL16 · UM61 · Path of the Seer`；
  - **多 ability，片段路直接 return**（`abl.Count != 1`）：`GOF80 · UM110 · SOR66 · DA75`；
  - **`blocks.Count != abil.Count`（G1 不成立 ⇒ 连 ta 都不贴）**：`DA51`（`blocks=1` 而 `abilityTargets` 有 **2** 项）；
  - `GSC55` 走**墓地另一支**（`CemeteryTargetScope`，`:6745-6750`），根本不到这条 `ResolveTargets`；
  - `UM97` 无 `abilityTargets`。

**`ASH13` 那一条传了会怎样**：criteria `ta=20` ⇒ `TargetCriteriaEval` 的 `case 20` 走
`ReferenceEquals(candidate, thisCard)`；而这跳 `source` 传 `null`、内部回落 `ctx.ActingUnit`（`:6752` 注释），
`spec = ParseTarget("this troop")` 解出来的也是**施放者自己** ⇒ 同一个对象、同一结果。

**判定：等价（今天可观测差异 = 0）**

**⛔ 反例警告（别照「传 op」一刀切改）**：`DA51 Covert Operation`
（`Return a friendly troop and a random enemy troop to the top of their deck`）在 `DoReturn` 里把 `op.Payload`
拆成 **两个 spec**（`:6644-6649`），而 `op` 只有**一份** criteria（`ab=0 lg=0 ta=30`）。
真传下去 ⇒ 第二段（敌方）也会被 `ta=30` 的 `ReferenceEquals(candidate, chosen)` 筛 ⇒ **一个敌人都回不去**。
今天不出事**只是因为** `DA51` 的 criteria 压根没贴（G1 `blocks(1) != abil(2)`）。
⇒ 若将来要修这一条，**必须**按「`specs.Count == 1 && ReferenceEquals(spec, op.Target)` 时才传 `op`」写。

---

## 三、`InsteadSkips:159` 那一条

**代码（现读）**
```csharp
// :159-160
var bt = ResolveTargets(ctx, owner, ops[baseIdx].Target, source, null,
                        ops[baseIdx].TargetCondition);      // ← 只传 cond，不传 op
```

**这条路的形态**：`pairConditional == false`（能力层 / 触发正文，**没有 `chosen`**）⇒ 先把「被替换那条 op」的目标解出来当 `instead` 条件的 referent。

**原版会不会用 criteria**：会 —— referent 池同样是 `*puVar1`（§2·0），所以 36 格应该参与。

**池内会不会有差别 —— 不会（现读 12 张全查过）**
池内含 `instead` 的 12 张（`ASH21 · ASH30 · AM35 · DA33 · GSC46 · SAU33 · GOF95 · GOF101 · Righteous Repugnance · SOR55 · SW53 · TAU58`；
`SAU33` 是静态规则文本，不是 `instead` 效果 op ⇒ 真效果 11 张）：

| 卡 | base op 的 criteria 现状 | 传 `op` 的实际后果 |
|---|---|---|
| `ASH21` / `DA33`（`Rally:` 片段，块内 2 op） | **没贴**（`ops.Count != 1` ⇒ `logic-gate`） | 无（`op.TargetCriteriaSet == null` ⇒ `ApplyTargetCriteriaFilter` 直接 return） |
| `GOF101` / `GOF95` / `ASH30` / `GSC46` / `SW53` / `TAU58` / `SOR55` / `AM35` | 贴上的那几份全是 `ta=30`；`AM35` 的 `abilityTargets` = `[]`（无 criteria 列） | `ta=30` 要 `targetCard`，而这条路 **`chosen` 恒为 `null`** ⇒ `Matches` 返回 `null` ⇒ **整个 criteria 筛被跳过**（只会多一条 `StructuredConditionNotice`「判不了」日志） |
| `Righteous Repugnance` | `ta=10`（`friendlyUnits=1 includeHero=1 includeMinions=1`） | 但它的 `instead` 无 `Condition`（`4 ☀: … instead`）⇒ `:143-147` 走「无条件 ⇒ 恒成立」分支，**根本不进这个 `ResolveTargets`** |

**判定：等价（今天 0 可观测）**。结构上仍与「读 `*puVar1`」不一致，
若要补，**必须**和 §2·3 同一条纪律（只在单一 spec 时传），且要重跑 `play all dump` 对指纹。

---

## 四、汇总

- **三个调用点**：真缺陷 **0** / 等价 **2**（`DoEachUnitDeal:3597` · `DoReturn:6762`）/ 不可达 **1**（`SetReferenceCards:1380`）/ 查不到 **0**。
- **附项 `InsteadSkips:159`**：等价（0 可观测），建议只记账不修。
- **本次另查出 2 条新缺口（都不是 B-193 原本问的那件事）**：
  1. **`eachunitdeal` 出伤单位池没过 criteria**（`EffectResolver.cs:3607-3615`）：`includeRemnants=0` 的残骸我们照打
     （`UM82` 定值伤害 ⇒ 多打 1-2 点；`ASH43` ⇒ 打 0 点）。修法见 §2·2 的可粘贴段。
  2. **`AddSide`（`:3030-3044`）不排除残骸** —— 只有 criteria 的第 ⑤ 格（`TargetCriteriaEval.cs:288-289`）才会排。
     凡「没贴 criteria」的目标池，残骸都会进池。⚠️ **本条的影响面没查**（不在本单范围内），只登记不出结论。
- **对 B-179 普查的订正**：`§三` 把 `DoEachUnitDeal` 归成「criteria 根本没传（疑似漏传）」要改成
  **「criteria 没传是对的（它描述的是另一个池）」**；B179 §五·4「重建 spec 丢字段」那一条里，
  **criteria 那半句要按本文件订正**，`NameFilter/NotKeyword/AnchorInSet/Adjacent` 那半句仍成立（未受影响）。

---

## 五、顺手发现

1. **原版 `AbilityLogic` 的 criteria 是按「logic」各一份，不是按「verb」**（`ASH46` 是最好的样本：lg=0 敌方 / lg=1 己方 Vehicle）。
   ⇒ 我们「opis 一条 op 挂一份 `TargetCriteriaSet`」的形状在**一条 op 对应多条 logic**（`eachunitdeal` / `DoReturn` 的双目标 / `forceattack` 的两池）时**天然不够用** ——
   `forceattack`（B-179 §二）与本单的 `eachunitdeal` 是同一个结构性问题的两面。将来若要根治，
   得让 `EffectOp` 能存**多份 criteria + 各自的池归属**，而不是逐 op 一份。
2. **`AttachTargetCriteria` 的 G3 闸（块内必须恰好 1 条 op）是「漏传 criteria 大面积不可观测」的原因**：
   return 17 张里只有 1 张贴上、`eachunitdeal` 3 张里只有 2 张贴上。⇒ 讨论「某 verb 有没有漏筛」时，
   **先查该 verb 的卡贴没贴上**，否则会把「数据结构上就没数据」误判成「代码丢了」。
3. **`GoF93 Hunta Rig` 的 `desc` 写 `Stomp. Rally: Choose a troop in the enemy hand and deploy it. This troop attacks it.`，
   而它 `chosenAbility` = `[235, 250, 320, 380]`**（3 条 ability vs 4 个码）——同一张卡上码数与 ability 数对不齐，
   `PlanAbOf` 那条按行号取 op 的链在这种卡上会不会错位，**没查清**（不在本单范围，只登记）。
4. **`AM54` / `GOF93` / `GOF104` 的 380 都在 `cardAbilities`（trigger 410）**，而本引擎的触发式能力
   **只跑正文 op**（`TriggerOpsByNumber`）⇒ 这三张卡的 `setReferenceCard` 那一跳**整条链没实现**
   （不是「只差 criteria」）。这比 B-193 大，建议单独立账（归「能力 380 接线」那一族）。
5. **`DoEachUnitDeal` 的 `chosen` 丢弃仍有明写理由**（`:6758` 的注释把 `Shuriken` 那条列为「按语义应该丢」），
   本单没有推翻它 —— 与「criteria 该不该传」是两件事，别顺带把那句判据一起改。
