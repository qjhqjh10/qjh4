# B9_W69ENG4 · 引擎真缺口第三批（前三条）· 2026-10-27

> 任务 = 《引擎真缺口_施工判据》§三 第三批的 1/2/3：G23① `Uprising` 过触发 · G22② 死亡路径不摘外来增益 · G07 `MissActions` 入队点。
> 写权：`RuleEngine/Core/RuleCore.cs` · `Core/EffectResolver.cs` · `Core/UnitState.cs` + 本报告 + `账/_parts/B9_W69ENG4.tsv`。
> **实际动了 1 个文件 1 处代码**：`RuleCore.cs`（G22② 接线 + 新方法）。G23① **已在**（前一轮 `F1` 落地）· G07 **判据被推翻**（详下，⛔ 未按施工判据接线）。

---

## 一、结论

| # | 条目 | 判定 | 一句话 |
|---:|---|---|---|
| 1 | **G23①** `Uprising` 过触发 | ✅ **已在（不是本代理改的）+ 本代理独立复核通过** | 闸 = `RuleCore.cs:13392` 的 `t.DeployedTurn != ctx.Turn ⇒ continue`（`F1` 2026-10-26 落地）。仓库外夹具证明它**承重**：无闸版在后续回合仍打掉 1 血，现行代码 0 血。⚠️ **仓库里没有这一档断言**（`TestUprising` 只测「之后部署 / 自指 / 同回合两次」）。 |
| 2 | **G22②** 死亡不摘外来增益 | ✅ **本代理已修**（真缺口，有真实后果） | `CleanupDeaths` 在「观察者 Die 广播 + 死因三族之后」调新方法 `SweepExtrinsicEffectsOnSourceDeath`：双方棋盘每张牌（含督军、排除死者）摘掉「来源 = 死者」的外来增益。仓库外夹具：改前 `Attack=4`（没摘）→ 改后 `2`；负例（来源还活着）`+3` 保留。 |
| 3 | **G07** `MissActions` 入队点 | ⛔ **判据不成立 · 未改代码** | 原版**没有**「无合法目标 ⇒ 记一次空放」这条路：`MissActions` 的唯一生产者是 `AbilityLogic.PlayAbility` 里 **`chosenAbility == 290 (AbilityEffect.missAction)`** 那一支，对**每个目标**各入队一条 action 50。而池内 `chosenAbility == 290` = **0 / 4156 条** ⇒ 休眠机制、不可观测。**按原施工判据接线会造出一条原版没有的行为**（详见 §二·3 与 §三）。 |

必跑读数：

| 检查 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_w69 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（改动后跑过两次） |
| `ruleprobe.sh check` | **rc=0** · 解析差异 0 行 · 哨兵失败 0 条 · **解析指纹 `c9a64798…` 与钉住的一致**（未动基线） |
| `ruleprobe.sh play check` | **rc=1**，差异卡 **只有 `BL64` + `DA42`**（= 本波开始前就存在的已知差异）· 事件电池 586 张逐字节一致 · **本轮零新增差异卡** |
| 仓库外夹具（`D:/tmp/wf_w69/probe`，⛔ 不进仓库） | G22 `PASS` · G23 `PASS` · G07 后果读数（`g07` 档）；对照版（改前）两档各自 `FAIL`（见 §二） |

> ⚠️ **「play check 零新增」不等于「本轮改动没生效」**：play 夹具的两种量法（「打出去那一拍」+ 五类事件电池）**结构上不产生单位死亡** —— 实测本轮 `play check` 全量输出里 `离场 ⇒ 场上摘掉` 这条新日志 **0 次命中**。所以 G22② 的读数只能由仓库外夹具给（已给）。

---

## 二、逐块：原版判据 / 改前行为 / 改后行为 / 怎么验

### 2·1 G23① `Uprising`：监听者必须「本回合上场」

**原版判据**（`decomp_full/CardScript__ResolveUnitSummoned.c:82-105`，逐句）：
`iVar4 = *(this + 0x2E0)`（`playedIntoFieldTurnCounter`，`dump.cs:35495`）· `iVar5 = *(bm + 0x3F8)`（`turnCounter`，`dump.cs:30871`）⇒ **`iVar4 == iVar5` 才往下**，然后才是 `刚落地那张 != 我` ∧ 同侧 ∧ `cardType == 0 (unit)` ⇒ `OnTrigger(0x2a8 = 680 Uprising)`。

**我们的现状**（现读）：
- 广播点唯一：`RuleCore.cs:4156` `FireTriggerOnSide(ctx, p, KeywordTable.Uprising, unit, requireDeployedThisTurn: true)`；
- 闸本体：`RuleCore.cs:13392` `if (requireDeployedThisTurn && t.DeployedTurn != ctx.Turn) continue;`
- `DeployedTurn` 的四个写入点全在：`RuleCore.cs:3986`（手牌打出）· `:4822`（`DeployFree`）· `EffectResolver.cs:15417`（残骸翻回）· `TutorialScript.cs:1478`（教程）；
- `ctx.Turn` 的递增 = `RuleCore.cs:1700` `ctx.Turn++`（`BeginTurn` 开头）⇒ 与「全局回合号」同义。

**改前 / 改后（仓库外夹具，`D:/tmp/wf_w69/probe`）**：

```
# 现行代码（有闸）
[G23] 第1回合部署起义单位 rc=0 · DeployedTurn=1 · Turn=1
[G23] 同回合再部署 rc=0 ⇒ 敌方总血变化 = 1（期望 1 = 起义响了）
[G23] 已推进到 Turn=3 · Active=0 · 起义单位 DeployedTurn=1
[G23] 后续回合部署 rc=0 ⇒ 敌方总血变化 = 0   →  [G23] PASS

# 同一夹具 · 对照版（把 requireDeployedThisTurn 改 false = F1 之前的形状，`D:/tmp/wf_w69/core_noGate`）
[G23] 后续回合部署 rc=0 ⇒ 敌方总血变化 = 1   →  [G23] FAIL（后续回合仍触发 1 次）
```

⇒ ① **同回合内**照旧响（闸不会把正常触发掐掉）；② **后续回合不再响**（原版语义）；③ 关掉闸就复现 bug ⇒ 这道闸**是承重的**。

**怎么验**：`cd D:/tmp/wf_w69/probe && dotnet build -v:q && dotnet bin/Debug/net8.0/wfg22probe.dll g23`（对照版 = `probe_nogate`）。

⚠️ **本代理没有改这一块**：它在工作树里**已经是修好的**（`F1_第4轮.md` §① 记的 `RuleCore.cs` 那一处），本代理只做独立复核 + 补「改前/改后」读数。**账行 876 的「① 缺闸」已过期**，补丁里已就地把 ① 标成 ✅。

### 2·2 G22② 死亡路径不摘「来源 = 死者」的外来增益

**原版判据**（`decomp_full/CardScript__ResolveDeadCard.c:231-244`，逐句）：
```
:236  CardScript__TriggerOnMinionDeath(观察者, cardPlayed, 死者, 死因, …)
:242  if (死者 != 观察者)
:244      CardScript__RemoveExtrinsicEffectsFrom(观察者, 死者)
```
- 调用者 `BattleManagerSupport__BroadcastDeadUnit` **遍历 `allCardsInPlay` 逐张调 `ResolveDeadCard`**（`EffectResolver.cs:7902` 的既有注释也这么记）⇒ 净效果 = **场上每一张牌各自摘掉「来源是死者」的那些效果**。
- `CardScript__RemoveExtrinsicEffectsFrom.c:28-63` 的体 = 遍历**观察者自己**的效果表（`+0x108`）、**从后往前**、只摘 `extrinsic(+0x30)` 为真且 `enchantingCard(+0x18) == 实参那张` 的（`RemoveEffect(观察者, 那一条, 0, 0)`）。同一方法在 `CardScript__AddEffect.c:93` 也被用（挂新效果前先摘同源的旧的，实参同样是 `enchantingCard`）⇒ **实参语义 = 「那个当来源的卡」**，不是「观察者」。⚠️ 这条**订正**了 `EffectResolver.cs:6859` 那句「`BattleManager__RemoveExtrinsicEffectsFrom` = 场上/手牌那一份，调用点 `ResolveDeadCard.c:244`」：`:244` 调的是 **`CardScript__` 那个单卡版**，`BattleManager__` 那个版**只在 `RawCardScript__TriggerAbilities.c:217`（trigger 240 那条）被调**，与死亡无关（见 §五·1）。

**我们的现状（改前）**：`CleanupDeaths` 只做 `SweepHandEffectsOnRemoval`（手牌那一套：`HandEffectExpired` 的 extrinsic 支线靠惰性判 `IsInPlay(施放者)`）⇒ **棋盘上的外来增益一条都不摘**，来源卡死了增益还挂在别人身上。

**改后**：`RuleCore.cs:10086`
```csharp
if (leftPlay) SweepExtrinsicEffectsOnSourceDeath(ctx, u);
```
位置 = 「观察者 Die 广播（`:10038`）+ 死因三族（`:10059-10071`）之后、`FlushDeathWatches`（`:10091`）之前」——照原版 `:236` 早于 `:242`；⛔ 没放在 `BoardSlots.RemoveAt` 那一刻（那是死者自己的死亡族之前，与原版次序相反）。

新方法 `RuleCore.cs:15197`（**追加在类尾**，铁律 18·b）：双方棋盘逐格、排除死者、`t.RemoveExtrinsicEffectsFrom(dead.Name)`，把条数汇总成一条日志。

**改前 / 改后（仓库外夹具）**：
```
[G22 改前=死前] ALLY.Attack=4（基准 2 + 2 来自 FIX_SRC）· OTHER.Attack=4（基准 1 + 3 来自 FIX_OTHER）
[G22 死亡] FIX_SRC 挨 99 伤 · Health=-97 · 仍在场=False
[G22 改后=死后] ALLY.Attack=2 · OTHER.Attack=4
  OK ALLY 的 +2 被摘掉
  OK 负例：来源还活着的 +3 原样保留
[G22] PASS

# 对照版（把新接线注释掉 = 改前，`D:/tmp/wf_w69/core_old`）
[G22 改后=死后] ALLY.Attack=4 · OTHER.Attack=4
  X ALLY 仍带死者给的 +2（实得 4，应为 2）        →  [G22] FAIL 1
```
「改前=死前」这一格是**精确**的等价值：旧代码在死亡时**什么都不做**，所以死亡后的读数与死亡前逐字相同（同一夹具、同一 seed）。

**怎么验**：`dotnet bin/Debug/net8.0/wfg22probe.dll g22`（对照版目录 `probe_old`）。

⚠️ **如实标着的两条限制**（也写进了方法 doc）：
1. **按卡名匹配**：`TempBuff.SourceCard` / `_statMods.Source` 存的是**名字**（写入侧 `EffectResolver.cs:11045 / 12157 / 14738`），原版比的是**对象引用** ⇒ 同名两张牌死一张时我们会**多摘**。要修得先让写入侧存实例 id，⛔ 本笔没另写一套匹配。
2. **只覆盖两本账**（限时增益 + 带 `Source` 的数值修饰）：`RecordGrant` 那条路没记施放者卡名（`UnitState.cs:2311-2314` 的既有 doc 已写死），所以「按施放者整份撤」本来就只做到一半 —— 那是**前一笔**留下的限制，本笔没扩大也没缩小。

### 2·3 G07 `MissActions` 的入队点 —— **判据被推翻，未改代码**

**原施工判据写的**：《引擎真缺口_施工判据》§二 G07「入队点 = `AbilityLogic.PlayAbility.c` 的『**没有合法目标**』那一支」，施工步骤 = 在 `EffectResolver` 的「目标解析为空 ⇒ 空过」出口（`DoTakeControl` / `DoDeal` / 主动能力）调 `RuleCore.MissActions`。

**取证（全量反编译，逐层）**：

1. `dump.cs:20700` **`public const AbilityEffect missAction = 290;`**（0x122），而 `AbilityLogic` 的 `+0x10` 字段 = `chosenAbility`（`dump.cs` `AbilityLogic` 字段表第 1 项）。
2. `AbilityLogic__PlayAbility.c` 里 `param_1 + 0x10` = 上面那个 `chosenAbility`；**`BattleManager__AddMissAction` 的唯一调用点** = `:2525`，落在
   ```
   :2513  else {
   :2514    if (iVar8 != 0x122) { return; }        // ← iVar8 = chosenAbility
   :2518    while (… iVar20 < possibleTargets(+0x78).Count …) {
   :2525      BattleManager__AddMissAction(manager, possibleTargets[i], …);
   ```
   ⇒ **一个 `missAction` 效果、对它的每个目标各入队一条 action 50**，action 携带的「卡」= **那个目标**。
3. `BattleManager__AddMissAction.c`：`BattleAction___ctor(&action, 0x32, …)` + `local_1f8[0] = param_2`（卡）⇒ 入队。
4. `BattleManager__ResolveAction.c` `case 0x32` → `CardScript__MissActions(local_338)`；`BattleManager__ResolveMissAction.c`（13 行）同构。
5. **反证「没有『无合法目标』那一支」**：全量反编译里 `ResolveMissAction` 的调用点 = **0**（只有它自己那个文件）· `FinishResolvingAction(…0x32` 的调用点 = **只有 `ResolveMissAction` 自己** · `AddMissAction` 的调用点 = **只有 `PlayAbility.c:2525`**。
6. **可达性**：`资料/普查产出_1018/allcards_1350/cards_raw.json` 全树（1351 张卡 / **4156 个 `chosenAbility` 节点**）扫描 ⇒ `chosenAbility == 290` = **0 条**。⇒ 休眠机制，与 droppod 同档：**不可观测**。

**为什么⛔不能按原施工判据接线**：`CardScript.MissActions` 的体（`CardScript__MissActions.c:20-30`）有一句**无条件**的 `attacksThisTurn(+0x48) += 1`，而 `AttacksThisTurn` 在我们的引擎里是**行动额度闸**（`RuleCore.cs:5737` `CanAttackNow` 的 `>= ActionQuota ⇒ ErrExhausted`；`RuleCore.cs:3535` `actedOut`；`ConditionEval.cs:213-216` 那两条 cond）。⇒ 在「某个 op 没解出目标」时调它，等于**一次空放就把单位的行动吃掉**，且一张卡多个 op 全空时会 +N（原版一次能力最多 1 条 action 50）。这**不是复刻，是我们自己造的行为**，而且会静默改一大片对局（每次「对敌方部队造成伤害」在对方空场时都会触发）。

**接上去的后果（仓库外夹具实测，`g07` 档）**：
```
[G07] 部署 rc=0 · AttacksThisTurn=0 · ActionQuota=1 · 额度闸命中=False
[G07] 调一次 MissActions ⇒ AttacksThisTurn=1 · 额度闸命中=True
```
（额度闸的判据 = `CanAttackNow` 里那句 `AttacksThisTurn >= ActionQuota ⇒ ErrExhausted`，`RuleCore.cs:5737`；`CanAttackNow` 本身此刻返 6 是另一道闸先挡——那不影响本读数：这条谓词从「没用过」翻成「用过了」本身就是行动被吃掉。）

**改后行为**：**无代码改动**。动作体 `RuleCore.MissActions`（`RuleCore.cs:14975`）保持原样、保留为「等结构化 ability 执行口」的落点。

**怎么验**：`grep -c '"chosenAbility": 290' 资料/普查产出_1018/allcards_1350/cards_raw.json` = 0；`cd /d/2/tools/decomp_full && grep -rln "FinishResolvingAction(.*0x32" .` 只命中 `BattleManager__ResolveMissAction.c`。

---

## 三、没查清的部分（⛔ 都没硬做）

1. **G22② 的「变残骸」那一支**：`CleanupDeaths` 里 `TransformIntoRemnant` 那条路（`leftPlay == false`，单位**还在场上**）本笔**不扫**。原版那条路的 `ResolveDeadCard` 会不会照样被广播、`enchantingCard` 比的是不是同一个 CardScript 对象（`TransformIntoRemnant` 是否换对象），**本笔没读到判据**。⇒ 我们取「还在场 ⇒ 不摘」，已在代码注释与账里标成「判据没闭合」。
2. **G23③（新发现）召唤路径不发起义广播**：`DeployFree`（效果召唤 / 自动召唤 / 残骸翻回退路）**不调** `FireTriggerOnSide(…Uprising…)`（全仓只有 `:4156` 一处，= 手牌打出那支）。原版 `ResolveUnitSummoned` 是「每次进场」都发。**半闭合点**：`_ResolveSummonUnit_d__510__MoveNext.c` 传给 `MinionPlayedIntoField(..., shouldTriggerSummon)` 的实参本笔**没读**（若传 0 则我们是对的）。⇒ 未改，已进账补丁的 876 行「差异 ③」。
3. **`Support(550)`**（G23②）：整条未实现，池内 0 —— 不在本批三条之内，未动。
4. **Requiem 入口接没接**（G22①/§四·J）：`ResolveTriggerRequiem` 存在（F1 建的），但 `CardDef.cs:7663` 说「入口还没接」、账 558 说「已建入口」—— 两条记载仍打架。本笔**没接**（不在三条之内）。

---

## 四、改动清单

| 文件 | 改动 | 行 |
|---|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs` | ① `CleanupDeaths` 里新增接线 `if (leftPlay) SweepExtrinsicEffectsOnSourceDeath(ctx, u);`（+15 行注释/代码，落在死因三族之后、`FlushDeathWatches` 之前） | `:10086` |
| 同上 | ② 新增方法 `static int SweepExtrinsicEffectsOnSourceDeath(BattleContext, UnitState)` —— **追加在类尾**（不移动任何既有行号之前的落点；本文件 `:10086` 之后的行号整体 +15/+56，账补丁里的行号都是 **2026-10-27 现读**） | `:15197` |
| `资料/完全复刻/账/_parts/B9_W69ENG4.tsv` | 3 行补丁（`ResolveMissAction` / `ResolveDeadCard` / `ResolveUnitSummoned`），11 列，`merge_ledger_parts.py --dry --only B9_W69ENG4` = **更新 3 / 新增 0 / errors 0** | — |
| 仓库外（**不进仓库**） | `D:/tmp/wf_w69/probe`（正例）· `D:/tmp/wf_w69/probe_old`（G22 改前对照）· `D:/tmp/wf_w69/probe_nogate`（G23 改前对照）· `D:/tmp/wf_w69/core_old` / `core_noGate`（对照用的 Core 副本） | — |

**没动**：`EffectResolver.cs` / `UnitState.cs`（本波白名单内但本笔无需改）· 任何 `*.unity` / `ProjectSettings` / 生成器 / 数据 JSON / 别人的文件。

---

## 五、顺手发现（只报不改）

1. 🔴 **`EffectResolver.cs:6855-6860` 的注释把两个方法混成一个**：它写「`BattleManager__RemoveExtrinsicEffectsFrom.c:36,45,80`（场上/手牌那一份，调用点 `CardScript__ResolveDeadCard.c:244` · `CardScript__AddEffect.c:823`）」——
   **实际**：`ResolveDeadCard.c:244` 与 `AddEffect.c:93` 调的都是 **`CardScript__RemoveExtrinsicEffectsFrom(this, 卡)`**（单卡版）；`BattleManager__RemoveExtrinsicEffectsFrom(manager, 卡, …)` 的**唯一调用点 = `RawCardScript__TriggerAbilities.c:217`**（`trigger == 0xF0(240)` 那条），它做的是「在 `unitsInPlay` + **双方手牌** 里找第一张含该来源效果的牌，入队一条 action」——与死亡无关。⚠️ 该文件的写权**不在本笔**，未改，仅在此报。
2. 🔴 **`Editor/RuleEngineTest.cs` 的 `TestUprising` 缺「后续回合不再响」这一档**：`F1_第4轮.md` §2·5 只给了判别式**设计**（「Turn T+1 再部署 ⇒ 仍 1 次」），仓库里没有对应断言 ⇒ 今天这颗闸**只有实现、没有锁**（本代理无该文件写权）。建议调度台补进 `TestUprising`（第 4 段）。
3. ⚠️ **`说明`**：账行 876 的既有落点 `RuleCore.cs:4034-4065 / :4092 / :4120` 全部**已过期**（现读 `:4156` 广播、`:13392` 闸、`:13376` 方法头）—— 补丁里已按现读写。
4. ⚠️ **`play` 这一类读数看不见死亡**（本笔实测）：`ruleprobe play` 的两种量法结构上不产生单位死亡 ⇒ 任何「死亡路径」的改动在它那里**必然零差异**。⇒ 以后这类账的验收口要写成「仓库外夹具 / Unity 自检」，⛔ 别拿 `play check` 绿当证据。
5. ⚠️ **`G07` 的判据错误来源**（供调度台判是否要连带核查其它 G 包）：《引擎真缺口_施工判据》§二 G07 的「入队点」是**按函数名相似度推断**的（`ResolveMissAction` 听起来像「没有目标 ⇒ miss」），而 `.c` 里它是 **`AbilityEffect.missAction` 的执行器**。同族的「按名字推断」在 §二 G22 也出现过一次（把 `CardScript__` 与 `BattleManager__` 两个同名方法混成一个，见上面 #1）⇒ 建议对「判据要点里出现『调用点 = X』的地方」抽检一遍「X 真的调的是这个方法吗」。
