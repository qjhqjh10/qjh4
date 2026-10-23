# RuleEngineTest 第三轮同步 4 红 —— 只读诊断

- 日志：`d:/4/_tmp_view/t67_sync/ruleengine4.log`（`109817`：`6732/6736 通过，**4 条失败** ❌`）
- 诊断方式：**只读**（源码 + 全量反编译 + 卡表数据 + 日志），未跑 Unity、未改任何源码/测试。
- 分类口径：**(α)** 断言过期/写错 · **(β)** 实现缺陷 · **(γ)** 本批回归 · **(δ)** 夹具前提。

## 〇、结论一览

| # | 失败 | 分类 | 一句话 |
|---|---|---|---|
| 1 | `A992OathB` 远程 | **α** | B-31 拆码是**对的**；夹具 `rangedAttack=0` ⇒ 远程路先撞上「远程值<1」⇒ 22，断言仍写 7（本批没同步）。 |
| 2 | `A992FeroB` 远程 | **α** | 同上，**同一段代码同一行**（两个夹具共用）。 |
| 3 | `B-16 · 110` 攻击 5 > Bastion 3 | **β** | `ConditionEval` 把「**施法者**的攻击」算成了「**目标自己**的攻击」（`u` ↔ `thisUnit` 取反了）。断言对、实现错。 |
| 4 | `cond=277` 判不了 ⇒ 不筛 | **α** | 277 本批已**真的判得了**（单位卡 subtype 数据齐），断言仍拿它当「判不了」的样本；应换一个真缺口值（240/250/…）。 |

**要改实现的只有 1 条（#3）**；#1/#2/#4 **只改测试**。

---

## 一、#1 / #2：`A992*B` 的远程打不了（7 → 22）

### 失败原文

```
RE    ✗ ★ A992OathB 的远程**也打不了**（两道打法同一个查询口）（这个单位不能进行远程攻击） —— 期望 [7]，实得 [22]
RE    ✗ ★ A992FeroB 的远程**也打不了**（两道打法同一个查询口）（这个单位不能进行远程攻击） —— 期望 [7]，实得 [22]
```

（`ruleengine4.log:83682` / `:83757`；汇总处 `:109818` / `:109819`；栈指 `RuleEngineTest.cs:30700`）

### 分类：α（断言/夹具过期；实现返回 22 是**正确**的）

### 根因链

1. 两个夹具用**位置传参**建卡（`RuleEngineTest.cs:30663-30666`）：

   ```csharp
   var oath = new CardDef("A992OathB", "A992OathB", "unit", "",
                          "common", "Test", 1, 3, 9, 0, new[] { "oath" }, subtype: "Infantry");
   var fero = new CardDef("A992FeroB", "A992FeroB", "unit", "Ferocity: Gain +2 Attack",
                          "common", "Test", 1, 3, 9, 0, new[] { "Ferocity" }, subtype: "Infantry");
   ```

   `CardDef` 构造器第 10 个 int 是 `rangedAttack`（`CardDef.cs:609-612`），`UnitState` 建实例时
   `RangedAttack = card.RangedAttack`（`UnitState.cs:640`）⇒ 这两个夹具 **近战 3 / 远程 0**。

2. 本批 B-31 把攻击值那一档按打法拆成两个码（`RuleCore.cs:5019-5025`）：

   ```csharp
   if (FieldAttack(ctx, p, u, ranged) <= 0)
       return ranged ? RuleCodes.ErrNoRangedAttack : RuleCodes.ErrNoMeleeAttack;
   ```

   远程值 0 ⇒ 返回 `ErrNoRangedAttack = 22`（`RuleCodes.cs:122-125`）。

3. 这句排在 `if (u.CannotAttackThisTurn) return RuleCodes.ErrNoAttack;`（`RuleCore.cs:5035`）**之前** ——
   **顺序是对的**，与原版一致：
   - `CardScript__CanAttackNow.c:19-22`：**先算** `CurrentMeleeAttack`/`CurrentRangeAttack`（两个都 <1 才退），
     后面 `:57-74` 才读 `canAct(+0x230)` → `canAttack(+0x23C)`（= 我们的 `CannotAttackThisTurn`）；
   - `BattleManager__IsValidAttackTarget.c:57-70 / :72-86`：近战 ≤0 / 远程 <1 是**最前面两支**，
     各带各的词条 `CantAttackWithZeroMeleeAttack` / `CantAttackWithZeroRangedAttack`
     （`RuleCodes.cs:93-94` 的 B-31 表已逐字核过，字面量命中）。
   ⇒ 一个「远程值 0」的单位被问「能不能远程打」，原版给的就是 `CantAttackWithZeroRangedAttack` 那一句，
   不是「本回合不能攻击」。**22 不但合法，而且比 7 更贴原版。**

4. 断言（`RuleEngineTest.cs:30698-30701`）写的是 `ErrNoAttack`，**HEAD 那版就在**（`git show HEAD:…:30521`），
   本批拆码后没有同步 ⇒ 红。

### 最小修法（**推荐 A**）

**A. 给夹具补远程值，期望保持 7**（`RuleEngineTest.cs:30664` / `:30666`，把 `1, 3, 9, 0` 改成 `1, 3, 9, 3`）：

- 远程 3 ⇒ 越过 `FieldAttack <= 0` 那道闸，真正走到 `CannotAttackThisTurn` ⇒ 返回 `ErrNoAttack(7)`，
  断言与文案**一字不改**。
- 🔴 **为什么推荐它**：直接改成「期望 22」会让这条断言**失去判别力** —— 远程值 0 本身就会返回 22，
  **把 `RuleCore.cs:5035` 那句 `CannotAttackThisTurn` 删掉，断言照样绿**（正是本工程反复强调的「自证」形态）。
  补远程值后，删掉 5035 ⇒ 远程返回 `OK` ⇒ 红，灭自证力恢复。
- 影响面：甲段（`plain`/`fast`，走 `Unit()` 助手，远程仍 0）只断近战，不受影响；乙段回合刷新后只断
  `CanAttackNow(..., false) == OK`（近战 3），不受影响。

**B. 只让码对上**：把期望改成 `RuleCodes.ErrNoRangedAttack` 并把文案改成「远程值 0 ⇒ 22（B-31 拆码）」。
改动更小，但**判别力下降**（见上），只在「这条断言只想钉『两道打法共用同一个查询口』」时可接受。

### 风险

- A 案没有实现改动，风险仅限「夹具不再代表『纯近战单位』」——本段测的是攻击闸，不是攻击面，可接受。
- 若采用 B 案，建议另加一条「远程值 > 0 + `CannotAttackThisTurn` ⇒ `ErrNoAttack`」的断言补回判别力。

---

## 二、#3：`B-16 · 110`（Bastion）判反了 —— **实现缺陷**

### 失败原文

```
RE    ✗ ★ B-16 · 110：攻击 5 > Bastion 3 ⇒ 假 —— 期望 [True]，实得 [False]
```

（`ruleengine4.log:108602`；汇总 `:109821`；栈 `RuleEngineTest.cs:35136`）

### 分类：β（实现缺陷；**断言是对的**）

### 根因

`ConditionEval.cs:245-257`（110 分支）：

```csharp
if (u.Has(KeywordTable.DropPod)) return null;
int combat = u.IsRemnant ? 0
           : (u.CurrentAttackType == UnitState.AttackTypeRanged ? u.RangedAttack : u.Attack);
return combat <= u.Bastion;
```

**主语取反了**：`combat` 与 `DropPod`/`IsRemnant` 两道守卫都读的是 `u`（**候选/被打的目标**），
而原版读的是 `thisUnit`（**施法者/攻方**）。

原版逐句：

- `FilterMethods__MeetsConditionCriteria.c:57-67`，`case 0x6e`（=110）：
  `p100 = MeetsConditionCriteria(card, 100, 0, thisCard)`；`p100` 真 ⇒ 真，否则
  `HasBastionHigherThanAttack(card, thisCard)`。
- `CardScript__HasBastionHigherThanAttack.c:16-26`：
  `iVar2 = CurrentBastion(param_1 = card/*候选*/);`
  `iVar3 = currentCombatAttack(param_2 = thisCard/*施法者*/);`
  `return iVar3 <= iVar2;`（且 `param_2 == null` ⇒ `return false`，见下节「顺手发现」）。
- `EntityScript__get_currentCombatAttack.c`：`0xe6 = dropPod`（`:22`）、残骸 `+0x65`（`:36-38`）
  都是对 **param_1 = 这个单位自己（施法者）** 的属性。

### 为什么第一条断言是「假绿」

`tgt` 攻击 1 / Bastion 3（`RuleEngineTest.cs:35131`），错的实现用 `tgt.Attack = 1` ⇒ `1 <= 3` ⇒ true ⇒
第一条（施法者 2 ≤ 3 ⇒ 真）**以错误的理由通过**；随后 `atk.Attack = 5` 完全不参与计算 ⇒ 第二条仍得 true ⇒ 红。
（真正的原因在日志里也能看出来：两条断言之间只改了 `atk`，而失败恰好出现在改 `atk` 之后。）

### 最小修法

把 110 分支的后半段整体换到 `thisUnit`：

```csharp
if (thisUnit.Has(KeywordTable.DropPod)) return null;      // 原版：currentCombatAttack(thisCard) 的 0xe6 分支
int combat = thisUnit.IsRemnant ? 0
           : (thisUnit.CurrentAttackType == UnitState.AttackTypeRanged
              ? thisUnit.RangedAttack : thisUnit.Attack);
return combat <= u.Bastion;                                // 原版：CurrentBastion(card=候选)
```

- 修后：2 ≤ 3 ⇒ true（第一条仍绿，理由变正）；5 > 3 ⇒ false（第二条转绿）；`thisUnit == null` ⇒ null（第三条不变）。
- `atk` 的 `CurrentAttackType` 建实例时 = `ChooseAttackTypeAutomatically`（`UnitState.cs:682`，近战 2 vs 远程 0 ⇒ 近战 1），
  测试直接改 `atk.Attack = 5` 不改攻击型 ⇒ 走的仍是近战 ⇒ 5。✔

### 顺手发现（不计入本次 4 红，但按铁律 5·b / 11 要记）

1. 🔴 **`thisUnit == null` 时原版是 `false`，我们是 `null`**：
   `CardScript__HasBastionHigherThanAttack.c:16-17,26`：`op_Equality(param_2, 0)` 为真（施法者为空）⇒ **`return false`**。
   ⇒ 原版的 110 在「没有施法者」时 = **只看 100**（dodge/invulnerable/dropPod），**不是判不了**。
   我们 `ConditionEval.cs:249` 返回 `null`，而 `RuleEngineTest.cs:35137` 又把这条 `null` 钉成了断言。
   当前卡池 `unitConditions` 里 **cond=110 共 0 行**（全 1127 张扫过）⇒ 影响潜伏。
   按「一切照原版」应改成 `return false` 并把第三条断言改成「= 100 的值」；若要保留 null，
   必须在 doc 里写明这是**故意偏离原版**（现在注释写的是「⛔ 不猜」，但原版这里**不是猜不出来，是明写 false**）。
2. 🔴 **生产路径拿不到施法者**：`RuleCore.MeetsUnitCondition`（`EffectResolver.cs:10106-10109`）只有 4 个形参，
   转发 `ConditionEval.MeetsUnit(ctx, u, cond, exclude)` ⇒ 生产调用点（`EffectResolver.cs:1776` / `:10216`）
   **永远传不进 `thisUnit`** ⇒ cond=110 在生产上**只能是 `p100` 或 `null`**。
   同样因为 0 行数据今天无影响；做 110 时必须连同这个形参一起补（否则只能得到 null）。

---

## 三、#4：`cond=277` 「判不了 ⇒ 不筛」—— **它已经判得了**

### 失败原文

```
RE    ✗ ★ `cond=277`（`isInfrantryOrBeast`，`B-06` 那一族）本波**判不了 ⇒ 不筛**（⛔ 不把池子筛空）｜🧨 判不了时改成「全排除」⇒ 本行红 —— 期望 [4]，实得 [0]
```

（`ruleengine4.log:107595`；汇总 `:109820`；栈 `RuleEngineTest.cs:34757`）

### 分类：α（断言过期；实现**没判错**）

### 根因

- 断言（`RuleEngineTest.cs:34755-34759`）拿 **277** 当「本版判不了」的样本，期望「整条不筛 ⇒ 4 个全留」。
  （HEAD 也有这一条：`git show HEAD:…RuleEngineTest.cs:34537`，写于 `ConditionEval` 还不存在的批次。）
- 本批新增 `Core/ConditionEval.cs`（**未跟踪的新文件**），277 已经进 `KindOf` 的 `Ok` 表
  （`ConditionEval.cs:74`），实现 `ConditionEval.cs:388-392`：

  ```csharp
  case 277:
      if (card == null) return null;
      return SubtypeIs(card, "Infantry") || SubtypeIs(card, "Beast");
  ```

- ①-a 的四个敌人夹具（`RuleEngineTest.cs:34707-34712`）全部用 `Unit(...)` 建：
  `Unit(name, cost, atk, hp, kws)` → `new CardDef(..., subtype: 缺省)`（`RuleEngineTest.cs:2039-2044`）
  ⇒ **subtype 全是空串** ⇒ 277 对四个都是 `false` ⇒ 池子被筛成 0。

### 模型数据核实（回答「277 的数据到底齐不齐」——**齐**）

原版（`FilterMethods__MeetsConditionCriteria.c:203-216`，`case 0x115`）：

```c
cVar2 = RawCardScript__IsKeyword(raw, 5);            // Infantry
if (cVar2 == '\0') bVar3 = RawCardScript__IsKeyword(raw, 0x23);   // 0x23 = 35 = Beast
```

- `Keyword` 枚举由 `d:/2/tools/il2cpp_out/dump.cs:45899-45912` 现读：
  `none=0 · Infantry=5 · Vehicle=10 · Monster=15 · Building=20 · Battlesuit=25 · Drone=30 · Beast=35 · Daemon=40`。
- 我们这边只需要 `subtype` 一个字符串。**现读 `cards_engine.json`（1127 张）**：
  - `type == unit` 共 **586** 张，subtype **全部**落在上面 7 个名字里：
    `Infantry 397 / Vehicle 135 / Monster 18 / Beast 15 / Battlesuit 8 / Drone 7 / Daemon 6`（**没有一个越界值**）；
  - `type == hero` 56 张全是 `Warlord`（`IsKeyword(5)/(35)` 都为假，判 false 正确）；
  - **11 张空 subtype 全部是 `tactic`**（`AM46/AM41/DA45/GOF39/SOR43/SOR51/SOR49/c4f5…/TL40/TL51/UM38`），
    没有一张是 unit/hero。
  ⇒ **单位这一族 277 的判据完整**：空串只出现在单位以外，判 `false` 不是猜。
  （`SubtypeIs` = 精确名、忽略大小写：`ConditionEval.cs:470-474`。）

### 最小修法（测试侧，二选一，推荐 A）

**A. 把 ①-f 的样本换成一个**仍然是 **`ModelGap`** 的值**（保持「判不了 ⇒ 整条不筛」这条不变量的测试意图）：

- 建议 **240**（`hasSecretOrder`）：`ConditionEval.cs:314-316` `return null`，`KindOf` 在
  `ConditionEval.cs:80-82` 归 `ModelGap`（`GapOf` 有现成说明 `:99-104`）。
  也可以选 250 / 280 / 320 / 330（同表）。
- 机制侧不用动：`EffectResolver.cs:1777-1791` 拿到 `null` 就 `return`（整条不筛 + 出声），断言期望仍是 4。
- 文案把「`cond=277`（`isInfrantryOrBeast`，`B-06` 那一族）本波判不了」改成新值 + 原因。

**B. 保留 277，但改成「正例」**：把 ①-f 拆成
①-f 用 240 测「判不了 ⇒ 不筛」；①-g 用「Infantry / Beast / Vehicle 三个夹具」测 277 **真的筛得对**
（Infantry+Beast 留 2、Vehicle 掉），顺带把 `RuleEngineTest.cs:34501` 那条「空 subtype ⇒ false」补成完整覆盖。

### 风险

- 选 240 时确认 `RoleTakesUnitSubject`（`EffectResolver.cs:10078-10081`）只吃 `target`/`acting` ——
  测试里的 `Role = "target"` 没问题（240/250 的 167 条数据行里有 `role=acting/target` 的，也能走到）。
- 池内 277 只有**一行**：`Speshul Dose`（`d19e8b68…`），且 `role = "cardtarget"` ⇒ 它**不会**走
  `ApplyStructuredConditionFilter` 这条单位筛路（那是 `MeetsCard` 的领域，而 `MeetsCard` 目前生产零调用）。
  ⇒ 277 判得了这件事目前**没有生产消费者**；测试里用 240 当「判不了」的样本不损失任何真覆盖。

---

## 四、复跑建议

| 改动 | 需要跑的自检 |
|---|---|
| 只改 `RuleEngineTest.cs`（#1/#2/#4） | `RuleEngineTest.Run` 一条即可（宿主未变） |
| 改 `ConditionEval.cs`（#3） | 同上；`ConditionEval` 是 `RuleEngine/` 内共用件 ⇒ **`RuleEngineTest.Run` 必跑**（本工程规矩：动 `RuleEngine/` 必跑这一条） |
| 跑完 | 以「断言合计」判：应从 `6732/6736` 变成 **`6736/6736`**；⚠️ 别按 `✗` 计数（会命中文案里的 `✗`） |

## 五、证据索引

| 事实 | 出处 |
|---|---|
| 4 条失败与合计 | `d:/4/_tmp_view/t67_sync/ruleengine4.log:83682, 83757, 107595, 108602` / 汇总 `:109817-109821` |
| A992 夹具 `rangedAttack=0` | `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs:30663-30666`（构造器第 10 个 int） |
| B-31 拆码点 | `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs:5019-5025`；`RuleCodes.cs:91-99, 118-125` |
| `CannotAttackThisTurn` 读点（在攻击值之后） | `RuleCore.cs:5035` |
| 原版攻击值先于 canAct/canAttack | `d:/2/tools/decomp_full/CardScript__CanAttackNow.c:19-22, 57-74` |
| 原版近战/远程各自报错 | `d:/2/tools/decomp_full/BattleManager__IsValidAttackTarget.c:57-70, 72-86` |
| 110 原版判据 | `d:/2/tools/decomp_full/FilterMethods__MeetsConditionCriteria.c:57-67` · `CardScript__HasBastionHigherThanAttack.c:16-26` · `EntityScript__get_currentCombatAttack.c:22, 36-38` |
| 110 我们的实现 | `Unity/MyGame/Assets/RuleEngine/Core/ConditionEval.cs:245-257`（错在 `253-256`） |
| 110 测试 | `RuleEngineTest.cs:35131-35138` |
| 277 原版判据 + 枚举值 | `FilterMethods__MeetsConditionCriteria.c:203-216` · `d:/2/tools/il2cpp_out/dump.cs:45899-45912` |
| 277 我们的实现 | `ConditionEval.cs:388-392`；`KindOf` `:74`；`SubtypeIs` `:470-474` |
| 277 数据分布 | `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（586 units 全部 ⊆ 7 个 Keyword 名；11 张空 subtype 全是 tactic） |
| 「判不了 ⇒ 整条不筛」机制 | `EffectResolver.cs:1769-1796` |
| 生产入口不传 `thisUnit` | `EffectResolver.cs:10106-10109`（+ 调用点 `:1776`, `:10216`） |
| 数据里 110 行数 | `cards_engine.json` 全表扫 `unitConditions`：`cond=110` **0 行**；`cond=277` **1 行**（`Speshul Dose`, `role=cardtarget`） |
| HEAD 上这三条断言早就存在 | `git show HEAD:Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`（`:30521` 远程 · `:34537` 277） |
