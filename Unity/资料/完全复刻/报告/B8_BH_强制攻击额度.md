# B8_BH · 强制攻击退还行动额度（+ R1 残骸 trait / 灵魂石收集 成对修）

> 执行代理 `B8_BH` · 2026-10-26 · 白名单：`RuleEngine/Core/RuleCore.cs`（+ 账补丁 + 本报告）
> 反编译来源标记：🥇 = `d:/2/tools/decomp_full/*.c`（真方法体）· 🥈 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs`（唯一有真体的 Cpp2IL 产物）

---

## ① 结论

### 任务 1 —— `CardScript.RemoveAttackThisTurn` 已实现（退还时机定在「攻击成功之后、且攻击前计数 == 0」）

**原版这一条不在攻击结算里，是「入队预扣 + 结算后补偿」两跳**，读完四份方法体后定案：

| 步 | 文件:行（🥇） | 做什么 |
|---|---|---|
| ① | `BattleManager__AddForceAttack.c:39-43` | `local_111 = 0; if (param_7 /*dontCountAttack*/ != 0) { local_111 = *(int*)(card+0x48) == 0;   /* = wasFree（写进 BattleAction +0xF7 = extraValue）*/  CardScript__RemoveAttackThisTurn(card); }` |
| ② | `CardScript__RemoveAttackThisTurn.c:5-7` | `if (0 < attacksThisTurn(+0x48)) attacksThisTurn--`（**下限 0**） |
| ③ | `BattleManager._ResolveForceAttack_d__504__MoveNext.c:34/61/69/73-79` | AI 挑目标 → `ChangeAttackType` → 排 `AddScriptedAttack`（真打）→ **`:73` 若 `extraValue(+0x117 = BattleAction+0xF7)` 为真，再排一条 `AddResetActions`** |
| ④ | `CardScript__ResetActions.c:18-34` | `IsUnit` ∧ `cardState ∈ {2,3,15,17}` 才继续 → `attacksThisTurn--` → `ActivateMinion(card, IsPlayerTurn()==card.isPlayer)` = **重算 canAct / canAttack** |

**净语义**（两条合起来才完整）：
- 攻击前 `attacksThisTurn == 0` ⇒ `wasFree = true`：①的预扣是空操作，③真打 `+1`，④ 再 `-1` 并重算两位 ⇒ **这一次强制攻击不消耗单位的行动**。
- 攻击前 `attacksThisTurn > 0` ⇒ `wasFree = false`：预扣 `-1` 与真打 `+1` 抵消、**不排 ④** ⇒ 计数不变 ⇒ 同样不额外消耗。

⇒ 我们的**同步**引擎里两条并成一句：**只在「攻击前计数 == 0」且攻击成功之后，把计数减回去并重算两位**。

**`dontCountAttack`（= 我们提示里的 `param_7`）到底什么时候为真**（实参位置按 `AddForceAttack.c` 形参表逐个对过；Ghidra 在调用点多印一个尾参 `0`，`AddForceAttackFriendlyUnits`/`AddScriptedAttack`/`AddRedirectedAttack` 三个调用点全有，是同一形状，不影响位次）：

| `chosenAbility` | 分支 | `dontCountAttack` | 池内张数 |
|---|---|---|---|
| `0xe6` (230) | `AddForceAttack(..., 0)`（`AbilityLogic__PlayAbility.c:2641`，`in_stack…` 被 `& 0xff` 清成 0） | **false ⇒ 不退还** | **0 张** |
| `0xe7` (231) | `AddForceAttack(..., 1, …)`（`:2659`） | **true ⇒ 退还** | 2 张 |
| `0xe9` (233) | `AddForceAttackFriendlyUnits(...)`（`:2679`）—— 该族**根本不含**退还那一句（全文无 `RemoveAttackThisTurn`） | 无此参数 ⇒ **不退还** | **0 张** |
| `0xed` (237) | `AddScriptedForceAttack(..., 1, 0)`（`:2748`） | **true ⇒ 退还** | 1 张 |
| `0xee` (238) | `AddScriptedForceAttack(..., 1, 0)`（`:2764`） | **true ⇒ 退还** | 7 张 |

⇒ **池内每一张「强制攻击」卡都退还**（10 张，全在 231/237/238）。我们全池唯一的强制攻击口是 `EffectResolver.TryAttackOnce` → `DeclareAttack(forced: true)`（`grep` 现核，只有这一个调用点传 `forced`）⇒ **无条件按「退还」实现，与今天 1,128 张逐张对得上**；将来若出现 230 族的卡，再加 `dontCountAttack` 形参。

**`wasFree` 的落地位置已坐实**（不是猜）：`AddForceAttack` 里那个 `local_111` 在栈帧 `-0x111`，而 `BattleAction` 基址 `local_208` 在 `-0x208` ⇒ 结构体偏移 `0xF7`；🥇 `d:/2/tools/il2cpp_out/dump.cs:118995` 写明 `+0xF7 = public bool extraValue`；状态机 `_ResolveForceAttack_d__504` 把该结构体整体拷到 `this+0x20`（ctor 里 2×`0x80` 字节）⇒ 读 `param_1+0x117` 正是它。

### 任务 2 —— `PayCardCost` 已按调度台裁定升 `A`，并新立一条 C（`EnergySpentSignal`）

主表 `战斗逻辑驱动.tsv:498` 状态 `C → A`；`差异` 列补齐「`EnergySpentSignal` 本仓无等价事件」；**新立一行 C**（`原版类 = EnergySpentSignal`，等级 `P2`）记成就/计量侧缺口。

---

## ② 逐条证据

### 2·1 任务 1 的判据链（全部 🥇 现读）
- `BattleManager__AddForceAttack.c` 全文 92 行：`:36` `BattleAction___ctor(local_208, 0x2a /*forceAttack*/, …)` → `:39-43` `wasFree` + `RemoveAttackThisTurn` → `:91` `AddAutoActionToQueue`。
- `BattleManager__AddScriptedForceAttack.c` 同一形状（`:41-48`），区别是 `param_2 = actingCard`、`param_3 = targetCard` 显式传入（`AddForceAttack` 那边 `param_2` 是**要出手的单位**，由 AI 挑受害者）。
- `CardScript__RemoveAttackThisTurn.c` 全文 10 行（`:5-7` 就是那两句）。
- `BattleManager._ResolveForceAttack_d__504__MoveNext.c` 全文 122 行：`:73` `if (*(char *)(param_1 + 0x117) != '\0')` → `:77` `BattleManager__AddResetActions` ⇒ **`wasFree` 为假就不补偿**。
- `CardScript__ResetActions.c` 全文 36 行：`:18` `RawCardScript.IsUnit` · `:22-27` 四档 `cardState` 否则只 `LogWarning` 后返回 · `:29-31` `--` · `:34` `ActivateMinion`。
- `CardScript__ActivateMinion.c`：`canAct(+0x230) = canAttack(+0x23C) = isPlayer==isPlayerTurn ∧ cardState==inPlay ∧ (attacksThisTurn==0 ∨ bloodThirst∧==1) ∧ !stun(100) ∧ !displaySummonSickness`；`:43-69` 是 `ferocity(0x4f1)/oath(0x4fb)` 的**第二写点**（只抬 `canAct`）⇒ 我们的等价物 = `RuleCore.RecomputeActAndAttack`（`RuleCore.cs:3492-3503`，逐句同形）。
- `attacksThisTurn` 是**已用次数**（不是剩余）：`CardScript.cs:5988/6663` 的 `CanAttack` 闸是 `attacksThisTurn == 0 || (bloodThirst && == 1)`，写点全是 `+1`（`CardScript.cs:536 / 4223 / 4308 / 12172 / 12889 / 20395`）。

### 2·2 任务 1 的读数（诚实标注：**这个量现有探针照不到**）
- **A/B 实测（隔离退款这一项）**：`play all dump` 在「退款关 / 开」两次跑，**全池 0 行差异**
  （`d:/4/_tmp_view/refund_off.tsv` vs `refund_on.tsv`；同一份 R1 改动两次都在）。
  原因（读代码确认，不是猜）：`PlayProbe.Capture` 只记 `Energy/Cells(攻/远/甲/血/关键词)/Hand/Deck/Disc/HandCost/Faith/Spirit/Quest/Dmg/Troops`，
  **`AttacksThisTurn` / `Exhausted` 一个都不记**；而电池里其后的每一发攻击都是 `forced: true`（本来就跳过配额闸）⇒ 结构上看不见。
- **可达性旁证**（不是读数）：`play SAU70` 的 `V3` 段 `atkM=21, atkR2=0` —— `atkM` 失败（失败发生在 `AttacksThisTurn++` **之前**）⇒ `atkR2` 那一刻计数仍为 0 ⇒ `wasFree = true` ⇒ 退还分支确实被走到。
- **要真读数必须先加探针列**（`PlayProbe.cs` 不在本笔白名单）⇒ 见 §⑥。

### 2·3 R1（残骸 trait / 灵魂石收集，成对修）
- 原版收集判据 = **阵营 ∧ isRemnant，与 trait 无关**：`SupportMethods__IsAeldariRemnant.c:1-14`（`*(int*)(rawCard+0x2c) == 0x1e(30 SaimHann)` ∧ `*(byte*)(card+0x65)`）。
- 原版翻面**摘光 trait**：`CardScript__TransformIntoRemnant.c:54-75` 倒序 `CurrentTraits` 逐条 `RemoveTraitSilently`（82 行全文现读，四步：`:29` 一次写 2 字节 `isRemnant/isLeavingRemnant` · `:33-53` `remnantMaxHealth(=1)` 写进两组生命字段 · `:54-75` 倒序摘 trait（每条还打一条 debug log）· `:77` `ChangeCardToRemnant(ui,1,0,0)`）。
- **成对性**：不换收集判据，摘光 trait 之后 `CanCollectWaystone` 的 `Has(Waystone)` 会恒假 ⇒ 灵族收灵魂石整条路断（用户可见）。
- **【本笔额外发现并一起修的第 4 处】**：`RuleCore.DestroyRemnants` 判据也是 `Has(KeywordTable.Remnant)` ⇒ 摘光 trait 之后**死灵残骸永远不消失**（静默）。已按原版换成**阵营**：`SupportMethods__ShouldRemnantDestroyOnTurnEnd.c:1-27` = `cardArmy(+0x2c) == 40(Sautekh) ∧ isRemnant ∧ !HasCurrentTrait(1190)`，收成 `RuleCore.IsNecronRemnant`。

**R1 读数（改前 / 改后，`play <任意 unit 卡>` 电池的 `collect=` 段 —— `rem` = 残骸、`+way` = 身上还带 `waystone`）**
```
改前：… end1=0, collect=0@1/rem+way,  draw=1, …     ← 0 = OK：残骸**还带着 waystone**，被当成可收
改后：… end1=0, collect=17@1/rem,     draw=1, …     ← 17 = ErrNotWaystone：关键词已清空
```
两处证据各读一次：`ClearAllKeywords()` 生效（`rem+way` → `rem`）；`CanCollectWaystone` 改读阵营（17 而不是 0，因为夹具那张卡阵营是 `Fixture`，不是 `SaimHann` —— **这正是原版语义**）。
实测卡：`SAU70` / `SW9` / `DA25` / `EC12` / `EC22` 五张，改后**全部**出现 `collect=17@1/rem`（或多格位的 `@2/rem`）。

---

## ③ 改了哪些文件哪些行

**只改了 `d:/4/Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`（总行数 14,044 → 14,187，+143）**

| # | 位置（改后行号） | 改动 | 新增行 |
|---|---|---|---|
| 1 | `:6203` | `DeclareAttack` 包装：`DeclareAttackCore(...)` → `DeclareAttackRefundingForced(...)`（**单行替换**） | **0** |
| 2 | `:9144` | `DestroyRemnants`：`Has(KeywordTable.Remnant)` → `IsNecronRemnant(u)`（附两条注释就地订正） | **+1** |
| 3 | `:9581` | 翻面支：`ps.Board[slot] = rem;` → `rem.ClearAllKeywords(); ps.Board[slot] = rem;`（**同行内改，零行数变化**） | **0** |
| 4 | `:12029` | `CanCollectWaystone`：`!u.IsRemnant \|\| !u.Has(Waystone)` → `!u.IsAeldariRemnant`（**同行内改**） | **0** |
| 5 | `:14045-14157`（类尾） | 新增 `DeclareAttackRefundingForced` / `RemoveAttackThisTurn` / `RefundForcedAttackAction` + 判据注释块 | **+113** |
| 6 | `:14158-14186`（类尾） | 新增 `public const string NecronFaction` / `public static bool IsNecronRemnant` + 判据注释块 | **+29** |

**行号漂移（给调度台的 `--fix` 参考）**：唯一影响下游的是第 2 处 ⇒ **`:9143` 之后的所有 `RuleCore.cs:N` 引用整体 +1**；类尾两段在第 5 条账行提的 `RuleCore.cs:14039-14045` **之后**追加，该行区间仍然包住 `AddToCemetery`（现 `:14040`）⇒ 只需 `--fix` 一到即可。

**账补丁**：`d:/4/Unity/资料/完全复刻/账/_parts/B8_BH.tsv`（表头逐字照抄主表、LF、每行 11 字段、7 行）
- `CardScript.RemoveAttackThisTurn()` → 保持 **C**（G3/G4 观测量缺口，理由见 §②2·2）
- `BattleManager.PayCardCost(...)` → **C 升 A**（调度台裁定）+ `差异` 列补 `EnergySpentSignal` 那一句
- **新建** `EnergySpentSignal` 行 → **C / P2**（成就·计量侧）
- `CardScript.TransformIntoRemnant()` → **C 升 B**（③ 已接；G5 未跑满 + 合并字典差异未消）
- `SupportMethods.ShouldRemnantDestroyOnTurnEnd(...)` → 保持 B，落点/差异按「真的改成阵营」订正
- `SupportMethods.IsAeldariRemnant(...)` → 保持 A，记「已收一处（`CanCollectWaystone`）、还待收两处」
- `BattleManager.CanUseWaystone(...)` → 保持 A，落点/差异订正（原文写的是关键词口径）

---

## ④ 没查清的部分（如实）

1. **退款的观测量**：`AttacksThisTurn` / `Exhausted` 在 `ruleprobe` 里**没有任何读数口** ⇒ G3/G4 拿不到「两侧数值」。本笔**不拿推理冒充实测**。
2. **原版「预扣了但目标不合法 ⇒ 白扣一点行动」**（`_ResolveForceAttack…:34-60` 不合法直接 `FinishResolvingAction`，补偿永远排不上）—— 已确认是**原版 bug**，本笔**不照抄**（照抄会在我们的多候选重试里多扣 N 次）。此决定请调度台复核。
3. **`cardState ∈ {2,3,15,17}` 那一档**：我们没有 `cardState` 字段，用「还在棋盘上 ∧ `IsAlive`」代理；原版 `:22-27` 的 `LogWarning` 分支（已离场/已死）我们静默跳过（同一个结果：一个字段都不写）。
4. **`Combined`（合并字典）那一处差异**（`TransformIntoRemnant` ③）：原版只摘 `CurrentTraits`，`activeEffects` 授予的那份仍在；我们 `RemoveAll(silent:true)` 两份一起摘。**可达性没核**（「翻面那一刻身上带 `buffType 3` 授予的 trait 的残骸有几张」没算）。
5. **`RuleEngineTest` 未跑**（硬红线：子代理不许跑 Unity/批处理）。按静态读，**至少这 4 处会红**（见 §⑥ 的第 3、4 条），需要主对话跑一次确认。

---

## ⑤ 顺手发现（不在本笔范围，只报不改）

1. 🔴 **`play check` 现在的红是「夹具阵营」造成的**：`PlayProbe.FixtureWaystoneTroop()`（`工具/ruleprobe/PlayProbe.cs:1706-1708`）的阵营写死 `"Fixture"`，而原版收集要求 `SaimHann`（阵营 30）⇒ 改阵营口径后电池的 `collect` 步对**所有 unit 卡**都返回 17。这是**正确的原版语义**，但会让 `play check` 的 `①-b` 出现 **546 张差异**（全部是 `collect=0@1/rem+way → 17@1/rem`）。把 `:1707` 的 `"Fixture"` 改成 `"SaimHann"` 就能一次性消掉这 546 行。
2. `play check` 的 `①-a`（主档）**有 7 张卡变了**，但**不是本笔**：差异内容是「随机召唤目标不同」（`TAU28`/`TAU11`、`GOF91`/`GOF79`）或「部署时多授予一个关键词」（`TL27` 多 `vulnerable2`、`SOR60` 少 `FIX_B` 的加成）；本笔三处改动**都不授予关键词、也不改随机消费**。而且改前那一版现算就已不是基线（`e11aa1c2…` ≠ 基线 `a93ffb8f…`）⇒ 怀疑是同期其它写手的在飞改动 / 基线本来就漂，建议同步点一并看。
3. `BattleDriver.cs:32556` 与 `CardFeel.IsRemnantFamily` 是**两件不同的事**：前者是「点这一格走不走收集」（= `IsAeldariRemnant` 的第三份副本，应转发）；后者读的是 `CardDef.Has(...)`（**卡 SO 印的关键词**），摘 `UnitState._keywords` **不影响**它 ⇒ 那里**不用改**（别顺手改）。
4. `ConditionEval.cs:540-542`（`case 340 onlyRemnants`）内联了 `u.IsRemnant && card.Faction == "Sautekh"` —— 与 `RuleCore.IsNecronRemnant` 是同一句的第二份，应合并。
5. 账行 `战斗逻辑驱动.tsv:1510` 原来写着「我们按 `Faction == 死灵阵营` 判」，**而代码当时是 `Has(Remnant)`**（关键词） —— 账与代码打架，已就地订正（见补丁行）。

---

## ⑥ 给调度台的请求

1. **加一个读数口**（`PlayProbe.cs`，不在本笔白名单）——任选其一即可让 `RemoveAttackThisTurn` 那行升 A：
   - 最小改动：`PlayProbe.Cell(UnitState u)` 在 `攻/远/甲/血` 之后追加两格 `·a{AttacksThisTurn}e{(u.Exhausted?1:0)}`（会改**全部** `E|` 与 `pf` 行 ⇒ 要重钉基线，一次性代价）；
   - 或：`play <id>` 的电池段额外打一段 `ep` 自己的 `ctx.Events` 尾巴（本笔那条 `重置行动：本回合攻击次数 {before} → {after}` 日志就会浮出来）—— 这条**不动指纹**，推荐先做这条。
2. **转派测试夹具**（`RuleEngineTest*.cs` / `PlayProbe.cs` 都不在本笔白名单）：
   - `PlayProbe.cs:1707`：`FixtureWaystoneTroop()` 的 `"Fixture"` → `"SaimHann"`（消掉 `play check` 的 546 行红）；
   - `RuleEngineTest.cs:4062`：`Unit("Stone", 1, 1, 1, "Waystone")`（`Unit(...)` 助手在 `:2392-2396` 把阵营写死 `"Test"`）⇒ 该卡必须变成 `SaimHann` 阵营；不改则 `:4089` 起整段（`CanCollectWaystone==OK` / `CollectWaystone==OK` / 石头 +1 / 残骸消失 / 进弃牌堆）全红；
   - 同族**还会红**的三处（同一根因，别漏）：`RuleEngineTest.cs:21080`（`Unit("W57Stone", …)` ⇒ `:21092` 红）· `RuleEngineTest.cs:28732`（`Unit("AS", …)` ⇒ `:28746` 的 AI 动作表/ScoreAction 红）· `RuleEngineTest_G1b.cs:49`（`Unit("G1bWaystone", …)` ⇒ `:104` 红）。
   - ⚠️ 建议给 `Unit(...)` 助手加一个**可选 `faction` 形参**（默认仍是 `"Test"`），只在这 4 处传 `"SaimHann"` —— 改动面最小、也不会连带影响那 400+ 处 `Unit(...)`。
3. **`RuleEngineTest` 需要主对话跑一次**（我跑不了）：重点看
   - ④ 路标石那一段（`RuleEngineTest.cs:4062` 起）与 ④b（`:4129`，残骸寿命）；
   - ⑦ `Nemesor Zahndrekh`（`:18579-18600`）—— **这条注释里明写「这两具必须带 `Remnant` 关键词」**，现在判据换成阵营了，夹具的两具残骸多半也要补阵营；
   - `RuleEngineTest.cs:15972-16028`（回合末自动摧毁）· `RuleEngineTest_G1b.cs` 收集那条。
   若红，**先判是不是上面第 2 条那批夹具没改造成的**（本条已按此顺序给出待改点）。
4. **重钉指纹**：本笔改完那一刻的读数（⛔ 我没动任何 `Pinned*Md5`）：
   - `ruleprobe check`：**与改前逐字相同** —— 解析差异 1 行（`f8260229…` Ethereal Supreme，改前就有）· 哨兵失败 1 条（`[输出指纹]` 现算 `18439d965d14288fe28cfbe5986dc445` ≠ 钉住 `193c1b25b0b4073f3da99f7085d6558d`，改前就是这样）· 卡池 md5 `d78f557e4272c2c571a9327139c7202a` · 基线 md5 `5ebfa4bfff1a20a814de6a3e8bebcd50`。
   - `play check`：**结算指纹 改前 `e11aa1c263e933dbdd67718133c8e5b2` → 改后 `ad104ff312d92c616a6af73f1d877063`**（基线文件 `a93ffb8f344517a9ac611ef06de17a3a`，改前就已经不同）· `①-a` 7 张（非本笔，见 §⑤2）· `①-b` **546 张**（本笔 R1，见 §⑤1）。
5. **两处判据「收成一处」的落点**（`R4`，本笔只能做引擎侧那一半）：
   - `BattleDriver.cs:32556` → `u.IsAeldariRemnant`；
   - `BattleDriver.cs:148` `SpiritStoneFaction` → 转发引擎侧唯一常量（建议放 `PlayerState`，先例 `PlayerState.QuestPointsFaction:346`，注意 `UnitState.cs` / `PlayerState.cs` 都不在本笔白名单）；
   - `ConditionEval.cs:540-542` → `RuleCore.IsNecronRemnant(u)`。

---

## ⑦ R1 / R4 交回点名

- **R1 三处：全部改了。** ① 翻面支 `rem.ClearAllKeywords();` 已加（`:9581`，同行内改零行数漂移）；② `CanCollectWaystone` 已换成 `!u.IsAeldariRemnant`（`:12029`）；③ `RuleEngineTest.cs:4062` **没改**（不在白名单）—— 需要改成：`Unit(...)` 造出来的那张 `Stone` 的阵营必须是 `SaimHann`（助手 `:2392-2396` 默认 `"Test"`；建议加 `faction` 形参、只在这 4 处传 `"SaimHann"`）。**另加第 4 处（R1 的必要连带）**：`DestroyRemnants` 判据由关键词改阵营，否则摘光 trait 后死灵残骸永不消失。
- **R4：收到 3 处，本笔收了 2 处（引擎侧各只剩一份）**：
  - ✅ `RuleCore.CanCollectWaystone` → 转发 `UnitState.IsAeldariRemnant`；
  - ✅ `RuleCore.DestroyRemnants` → 转发新增的 `RuleCore.IsNecronRemnant`（+ `RuleCore.NecronFaction` 常量）；
  - ⛔ `BattleDriver.SpiritStoneFaction` / `BattleDriver.cs:32556` / `ConditionEval.cs:540-542` —— **不在白名单，没动**，改法见 §⑥5。
- **`SOR66 Armorium Cherub`**：本笔未碰到，按调度台给出的口径（照原版补 `pray`、账里记「卡面未印」）留给对应写手。
- **`GetExtraAbilityTraits` / `CheckCurrentTraits`**：未碰到，保持 B。
