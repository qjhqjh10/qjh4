# B97_AI · `SimpleAI.cs` 的 AI 选靶次序（2026-10-29 · 第九十六会话）

> 执行代理 `B97_AI`（有写权限）· 白名单文件 = `RuleEngine/Data/SimpleAI.cs`（＋新建断言 `CardPresentation/Editor/B97_AI_Assert.cs`、账补丁 `账/_parts/B97_AI.tsv`、本报告）
> 判据输入（**已读**）：`报告/B97_RC.md` §七·2 · `判据/B97_结构差与休眠_再裁.md`（#22 / §137 的 9c）·
> `decomp_full/BattleManager__GetAvailableActions.c` · `BattleManager__GetAvailableAttackActions.c` ·
> `BattleManager__GetAvailableAttackTargets.c` · `BattleManager__GetUnitsInPlayPooledList.c` ·
> `BattleManager__GetRandomAttackTarget.c` · `BattleManager__EndTurnClick.c` ·
> `cpp2il/out_cpp2il_dev_ilspy/{AI.cs,BattleManager.cs}` · `cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs`
> ⛔ 没碰：`RuleCore.cs`（`B97_RC2` 在写）· `EffectResolver*` · 账主表 · 正本 · `Pinned*` · `Editor/RuleEngineTest.cs` · git · Unity 批处理。

---

## 〇 · 一句话

**简报的前提不成立** —— 原版 AI **不**用 `BattleManager.GetAvailableAttackTargets`（那个是**倒序**、且只服务 `EndTurnClick` 的 UI 提示与一个 **0 调用方**的死方法）。
AI 的真实路径是 `AI.PlayTurn` → `GetAvailableActions` → `GetAvailableAttackActions`，它按 `unitsInPlay`（`0x470`）**正序**枚举攻击者与目标。
⇒ 我**没有**把 `SimpleAI` 转调 `RuleCore.AvailableAttackTargetsDesc`（那样会做错），
而是把 `EnumerateActions` 的攻击段从「格号序」改成原版的「**进场序（`PlayOrder` 升序）**」，并留了断言与前后读数。

| 项 | 结果 |
|---|---|
| 转调 `AvailableAttackTargetsDesc` | ⛔ **没做**（与 `.c` 冲突，反证见 §二） |
| 真实缺口（枚举次序） | ✅ **已改**：`SimpleAI.cs:362`（`EnumerateActions` 攻击段）＋ 类尾 `SimpleAI.PlayOrderedSlots` |
| 断言 | ✅ 新建 `CardPresentation/Editor/B97_AI_Assert.cs`（`CheckB97AIGaps()`，7 条带 🧨） |
| `typecheck` | ✅ **0 / 0** |
| `ai actions` 前后读数 | ✅ 有差（攻击者次序变了，见 §三） |
| `play all dump` 前后 | ✅ **逐字节相同**（md5 `c2e2284d…`，见 §四；`play all dump` 结构上看不见 AI，⛔ 不当验收证据） |

---

## 一 · 逐处判：`SimpleAI` 里「挑攻击目标」的地方

`SimpleAI.EnumerateActions`（= 原版 `BattleManager.GetAvailableActions` 的等价物）一共 **5 处**会按目标枚举，逐一判：

| # | 位置（现读） | 我们做什么 | 原版对应口 | 判 |
|---|---|---|---|---|
| ① | `SimpleAI.cs:362-386` 「场上单位：近战 / 远程各一条」 | 逐 (攻击者, 目标, 近战/远程) 一条 | `GetAvailableActions.c:132` → `GetUnitsInPlayPooledList`（攻击者，`0x470` 正序）＋ `GetAvailableAttackActions.c`（目标表 `0x580`，`0x470` 正序） | **就是本条**（原先格号序 ⇒ 已改进场序） |
| ② | `SimpleAI.cs:343-360` 战术卡 `PlayCard`（每个 `slot` 一条） | 逐格问 `CanPlayCard` | `GetAvailableActions.c:103` `RawCardScript.GetValidTargets(card)` 后逐个 | **同轴但未改**（原版也走 `0x470` 正序；见 §八 待接线） |
| ③ | `SimpleAI.cs:398-411` 替代行动（`CanPickAlternativeTarget`） | 逐格 | `GetAvailableActions.c:195-215`（遍历 `0x470` 正序） | **同轴但未改**（§八） |
| ④ | `SimpleAI.cs:420-430` 普通主动技能（`CanUseAbility`） | 逐格 | `GetAvailableActions.c:178-215`（遍历 `0x470` 正序） | **同轴但未改**（§八） |
| ⑤ | `SimpleAI.cs:1571-1600` `NextAction` → `TweakAvailableActions` → `SelectBestAction` | 打分 → 难度旋钮 → 严格 `>` | `AI.PlayTurn` → `TweakAvailableActions` → `SelectBestAction` | **次序判据的消费端**：平手留**先枚举**的那个 ⇒ ① 的次序直接决定决策 |

**只有 ① 是「AI 挑攻击目标」**；②③④ 是「技能 / 战术卡挑目标」（同属 `unitsInPlay` 正序，但**不在本批范围**，见 §八）。

`AI.SelectBestAttackAction`（`RuleCore.ResolveBerzerkAttacks` 那条狂暴路）**不在本文件**，它的选靶仍按格号序 ⇒ 仍是 `报告/B97_RC.md` §七·1 的待接线（未动）。

---

## 二 · 判据与反证（为什么**不**转调 `AvailableAttackTargetsDesc`）

### 2.1 原版 `GetAvailableAttackTargets` 的调用方（三源现扫）

| 源 | 命中 |
|---|---|
| `decomp_full/` | `BattleManager__EndTurnClick.c:78` · `BattleManager__GetRandomAttackTarget.c:15` |
| `cpp2il/out_cpp2il_dev_ilspy/` | `BattleManager.cs:21054`（`EndTurnClick` 里，狂暴提示 `PendingBerzerk` 的指针要打哪）· `BattleManager.cs:42314`（`GetRandomAttackTarget`） |
| `cpp2il/out_il2csharp/` | `Assembly-CSharp/BattleManager.cs:20652`（只有**定义**；`script.json:12760` 是方法索引） |

而 `GetRandomAttackTarget` **三源 0 调用方**（另 `d:/2/tools/assetripper_out/ExportedProject/.../BattleManager.cs:6782` 也只有定义）
⇒ **两条路都不是 AI 决策**：一条是「狂暴时提示玩家该点谁」的 UI，一条是死代码。

### 2.2 原版 AI 的真实路径（逐行现读）

```
AI.PlayTurn(manager, mode)                         AI.cs:110
  availableActions = manager.GetAvailableActions(isPlayer:false)        AI.cs:118
    GetAvailableActions                                   BattleManager__GetAvailableActions.c:132
      GetUnitsInPlayList(manager, 0x570, …)  →  GetUnitsInPlayPooledList（0x470 从下标 0 往大扫）
        for each unit:  GetAvailableAttackActions(manager, unit, allowAuto)   .c:141-152
                        建「可打目标表 0x580」：也是 0x470 下标 0 起、正序过滤
                        then 逐目标：先近战(attType=1)、再远程(attType=2)
  TweakAvailableActions（降序）                           AI.cs:216
  SelectBestAction（严格 `>` ⇒ 平手留**先枚举**的）        AI.cs:588
```

⇒ **攻击者与目标都按 `unitsInPlay` 的进场追加序（正序）**，与 `#91` 那条（`RawCardScript.GetValidTargets` 走 `0x470` 正序）**同一个轴**。
倒序只出现在 `GetAvailableAttackTargets`（`.c:23-33` `iVar1 = count; iVar1--`）—— 一个 AI 不用的口。

### 2.3 结论

- `报告/B97_RC.md` §七·2 与 `判据/B97_结构差与休眠_再裁.md` 里那句话（「AI 选靶原版走 `GetAvailableAttackTargets`，倒序」）**当作待订正**：
  我按 `.c` 反证，未照它做（CLAUDE.md：判据与 `.c` 冲突 ⇒ 以 `.c` 为准、宁可拒做）。
- 真正该改的是**格号序 → 进场序**（`UnitState.PlayOrder` 升序，`-1` 排末尾，与 `RuleCore.ValidTargetSlots` 同口径）。

---

## 三 · 改动

### 3.1 `RuleEngine/Data/SimpleAI.cs`（行数零漂移：362–382 共 21 行，改后仍 21 行）

```csharp
// ---- 场上单位：近战 / 远程各一条（🔴 次序 = 原版 `unitsInPlay` 进场序 `PlayOrder` 升序，⛔ 不是格号序；判据 → `PlayOrderedSlots` 的 doc）----
var atkOrder = PlayOrderedSlots(mine.Board); var tgtOrder = PlayOrderedSlots(foeBoard);
for (int ai = 0; ai < atkOrder.Count; ai++)
{
    int s = atkOrder[ai]; var u = mine.Board[s];
    if (u == null || !u.IsAlive || u.Exhausted || u.IsStunned) continue;
    for (int ti = 0; ti < tgtOrder.Count; ti++)
    {
        int t = tgtOrder[ti]; if (foeBoard[t] == null) continue;
        …（内层 r = 0/1 与 `IsValidTarget` 判据**一字未动**）
```

### 3.2 类尾新助手（追加在文件末尾 ⇒ 所有 `SimpleAI.cs:NNNN` 落点**零漂移**）

`static List<int> PlayOrderedSlots(UnitState[] board)`：只留非空格、按 `RuleCore.StableSortByPlayOrder`（`PlayOrder` 升序、`-1` 排末尾）排。
注释里写死了「判据 / 为什么不是格号序 / ⛔ 别换成倒序口」三件事。

**行数证据**：改前 `2228` 行 → 改后 `2260` 行（+32 全在类尾）；`362–382` 之外**没有任何行被移动**；行尾纯 LF（`CRLF 0 / bareCR 0`）。

---

## 四 · `ai actions` 前后读数（3 张卡）

夹具（`AiProbe.BuildActionsFixture`）：我方 3 号格探针单位（先进场）、督军 4 号格（`PlayOrder` 更小）、敌方 3 / 5 号格。
⇒ **攻击者**这一段正好能分辨（督军 vs 探针 的「格号序 ≠ 进场序」）；目标那一段夹具里两序一致 ⇒ 不作判别（由断言兜，见 §五）。

| 卡 | 改前（attackMelee 行次序） | 改后 |
|---|---|---|
| `SOR49` | `@3→P1@3` · `@3→P1@4` · `@3→P1@5` · `@4→P1@3` · `@4→P1@4` · `@4→P1@5` | `@4→P1@4` · `@4→P1@3` · `@4→P1@5` · `@3→P1@4` · `@3→P1@3` · `@3→P1@5` |
| `SOR43` | 同上（3 号格 3 条在前） | 同上（4 号格 3 条在前） |
| `SOR38` | 同上 | 同上 |

**读法**：`slot=4` = 督军（`PlayOrder` 更小、进场更早）⇒ 原版口径下它**先被枚举**。改前是 `slot=3`（格号小）先、改后是 `slot=4`（进场早）先 —— **正是本条要修的那一格**。

**决策层没变**（这 3 张卡的 `[最高分]` / `[SelectBestAction]` / `[NextAction(Normal)]` 前后**完全相同**：`SOR49`/`SOR43` = `attackMelee @3→P1@4 = 3.1`；`SOR38` = `playCard hand#3 @5 = 17`）——
因为这几条最高分**不并列**（严格 `>` 不受次序影响）。**只有同分并列时**次序才改结果 ⇒ 由 §五 的断言专门钉住那一档（比 `ai actions` 的固定夹具更有判别力）。

⚠️ 如实记：`ai <卡id>`（打分档）**不调** `EnumerateActions` ⇒ 它前后**必然逐字节相同**，本批**没拿它当读数**（那样会是假绿）。

---

## 五 · `play all dump`

- 改前 md5 `c2e2284d4abd67d084e9c25adbb2a32c`（1,130 行）
- 改后 md5 `c2e2284d4abd67d084e9c25adbb2a32c`（1,130 行）⇒ **逐字节相同**
- `rc=0`，事件电池自检全过 ✅

⛔ **这不是「改动没生效」的证据，也不是「改动正确」的证据** —— `play all dump` 是**结算层**读数、结构上没有 AI 列
（见 `工具/ruleprobe.sh` 头部与 `AiProbe.cs` 头注）。它在这里只起「**没有误伤结算**」这一个作用。

---

## 六 · 断言

新建 `MyGame/Assets/CardPresentation/Editor/B97_AI_Assert.cs`（`public static partial class RuleEngineTest`，与 `B97_RC_Assert.cs` 同形）。

- 夹具：攻击者先摆 **7** 号格、后摆 **3** 号格（进场序 `[7,3]` vs 格号序 `[3,7]`）；目标先摆 **8** 号格、后摆 **2** 号格（进场序 `[8,2]` vs 格号序 `[2,8]`）；
  攻击者 `SummonSickness = true` 把敌方督军（4 号格）挡出目标表（判据 `IsValidAttackTarget.c:199-202`）。
- 7 条断言（每条带 🧨）：
  1. 前提 ×2：两侧的「格号序 ≠ 进场序」真的成立；
  2. 前提：督军确在 4 号格；`ValidTargetSlots` 给 `[8, 2]`；
  3. **★★ 结论 1**：先进场的攻击者（7）**先被枚举**（`i7 < i3`）；
  4. **★★ 结论 2**：对同一攻击者，先进场的目标（8）**先被枚举**；
  5. **★★ 结论 3**：4 条同分近战动作交给 `SelectBestAction` ⇒ 选中 **`Slot=7` ∧ `TargetSlot=8`**（= 先枚举的那个）；
  6. **★★ 结构断言**：源码里必须还含 `PlayOrderedSlots(mine.Board)`、且**不含** `AvailableAttackTargetsDesc(`（防「实现 + 断言一起改回去」）。
- **接线建议**：在 `RuleEngineTest.cs:1359` 那一行末尾加一格 ——
  `CheckB93G1PlayedCardTail(); CheckB97RCGaps(); CheckB97AIGaps();`（同一行、零行数变化；`CheckB97RCGaps` 已由调度台代接线）。
- ⚠️ 本代理**不跑 Unity** ⇒ 落盘 `typecheck 0/0`；`RuleEngineTest.Run` 由调度台在同步点跑。

---

## 七 · typecheck / 账补丁

- `TMPDIR=/tmp/wf_ai bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改前/改后/还原后各跑过一次）。
- 账补丁 `账/_parts/B97_AI.tsv`：**11 列 + 首行表头**，两行，均为**就地更新**（键 = `(原版类, 方法)`，在 `账/战斗逻辑驱动.tsv` 各命中 1 处）：
  - `BattleManager.GetAvailableAttackActions(CardScript actingCard, bool allowAutoActions)` —— 补「AI 侧枚举次序已改」+ 两条**未实现**的闸 + **建议降 A→B（请调度台裁定）**；
  - `BattleManager.GetAvailableAttackTargets(CardScript attacker)` —— 补「调用方只有 EndTurnClick + 死代码」⇒ ⛔ 别接进 AI。
- 自检：`python 工具/check_patch_conflicts.py` ⇒ 无跨补丁重复键；`python 工具/merge_ledger_parts.py --dry` ⇒ 无拒绝/跳过。

---

## 八 · 待接线 / 没查清（⛔ 不假装）

**待接线（有明确消费者，按原版判据）**
1. **②③④ 那三处目标枚举也还是格号序**（战术卡 / 替代行动 / 普通主动技能）。原版都走 `0x470` 正序
   （`GetAvailableActions.c:103 / :195-215 / :178-215`）。本轮只改「攻击」那一段（简报范围）：
   ② 的 `slot` 语义要按 `EffectTargets` 的「本方 / 敌方」重判，改错会把待定目标的卡排到另一侧 ⇒ **建议单独一批**。
2. **两条攻击动作的闸没实现**（账里已记）：
   - 原版只排「攻击力 > 0」那一档（近战 `CurrentMeleeAttack > 0` ∧ `!pindown(0x3ca)`；远程 `CurrentRangeAttack > 0`）；
   - 目标已 `EnoughPendingDamageToDie` ⇒ 不再排任何攻击动作。
   我们 `IsValidTarget` 不含这两条 ⇒ 动作表多出「0 攻近战 / 打将死目标」的条目（打分 0 或负 ⇒ 选不到，但表形状不同）。
   ⚠️ `pindown` 那一半 **`IsValidTarget` 已等价**（`RuleCore.cs:5770` `!ranged && Has("pindown") ⇒ ErrPindown`），只差「> 0」与 `EnoughPendingDamageToDie` 这两条。
3. `RuleCore.ResolveBerzerkAttacks` 的选靶仍按格号序（`报告/B97_RC.md` §七·1），**不在本文件**。
4. `TweakAvailableActions` 用的是 `List<T>.Sort`（**不稳定**），原版是 `Enumerable.OrderByDescending`（**稳定**）。
   降序后同分项的相对次序因此可能不同 ⇒ `SelectBestAction`（平手留先）会选到另一条。
   **本批没改**（它属于「难度旋钮」那条账、已被审计判 A，改它要连带订正那行）⇒ 请调度台决定是否单开一批。

**没查清（⛔ 不编）**
- a. `GetUnitsInPlayList` 那 11 个实参里 `param_9 = isPlayer` / `param_10 = !isPlayer` 的**精确分支**我只按
  `AI.PlayTurn` 的实际取值（`isPlayer:false`）反推「只收本方的单位」，与我们的「只扫 `mine.Board`」一致 ——
  **没有逐位穷举**所有取值组合（本批不需要）。
- b. `0x470` 的**追加点**（哪一句 `unitsInPlay.Add`）我没有逐处扫；「进场追加序 = `UnitState.PlayOrder`」沿用的是
  `#91` 已裁定的口径（`报告/B97_RC.md` §#3）与 `PlayerState.UnitsByPlayOrder()` 的 doc，**本轮未重新取证**。
- c. 我没有跑 Unity（`RuleEngineTest.Run`）⇒ 断言只是**落盘 + `typecheck` 过**，红绿由调度台在同步点定。
