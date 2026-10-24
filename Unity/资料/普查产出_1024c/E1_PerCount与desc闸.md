# E1 · 触发层 `PerCount` 增量 + 整条 `desc` 路的替代行动块闸（2026-10-24 · 第七十三会话）

> 执行代理 **E1**（有写权限）· 工程根 `d:/4/Unity`。
> 判据 = ① `d:/2/tools/decomp_full/` ② 解包资源（本件未用到）③ 成品卡图（本件未用到）。
> ⛔ 没跑 Unity 批处理 / 没跑 ruleprobe / 没动 git / 没改正本（`CLAUDE.md` · `项目任务.md`）。
> ✅ `TMPDIR=/tmp/wf_e1 bash 工具/typecheck.sh`：**运行时 0 / 编辑器 0**（本件末次读数）。
> ⚠️ 中间有一次读到编辑器 6 条错，全部集中在 `CardPresentation/Editor/RankedArtAssert.cs`
> （`RankedDivisionInfo` 缺 `DivisionArtCount` / `RankArtCount`）—— 那是别的代理当时正在写的文件，
> 与本件白名单无关，**按铁律 13·3 只报不改**；下一次跑已归 0。

---

## 一 · 结论（两件都做完）

| # | 件 | 裁定 | 一句话 |
|---|---|---|---|
| 1 | 触发层 `PerCount` 增量型**整条丢失** | **✅ 改完（`C3` §1·1 那笔账核销）** | 把出牌路那套（`基础 + 计数 × 增量` ＋ 结构化 `mult` 优先 ＋ 载荷回写 ＋ 递副本）抽成 `TryFoldPerCount`，触发路改调它；删掉 `trigger-repeat-percount` 那条 skip。**今天 0 张可达**（加法型全池 4 张全是战术卡、`trigger` 全 `[0]`）⇒ 行为零变化，靠合成正文的增量断言钉住语义。 |
| 2 | 整条 `Parse(Desc)` 路仍给「替代行动块」贴 `cardAbilities.ta` | **✅ 改完（`E6` §3·4 那笔账核销）** | 整条 desc 分支加**两道路口同一口径**的闸：① 调用方标了 `alternativeActionBody`；② 这张卡的整条 `Desc` 就是某个替代行动词的**裸写正文**（`CardDef.IsBareAlternativeActionBody()`）；外加**按块**的段首关键词闸（`Duty:` / `Pray:` / `Ferocity:` / `Agenda:` / `Oath N:` 那几块不贴）。词表只有一份 = `CardDef.IsActiveActionKeyword`。⚠️ **`E6T2` 的期望必须由主对话改（它在白名单外）** —— 见 §七·1。 |

---

## 二 · 件 1：触发层 `PerCount` 增量

### 2·1 判据（照抄 `C3` §1·1，⛔ 不要重新论证）

| 事实 | 出处 |
|---|---|
| 原版这一格是 `AbilityLogic.multiplier` / `furtherMultiplier` → **数值**放大 | `AbilityLogic__PlayAbility.c:181-236`（`local_178` = `GetTargets(...).Count`，`ta∈{210,220}` 改 traits 累加，最后 `if (0 < furtherMultiplier) local_178 *= furtherMultiplier`） |
| 伤害族 = **一个** `DamageInfo` 一次 `DealMultiDamage`（倍率进数值） | `PlayAbility.c:1461-1508`（`case 10/13`） |
| 只有 `doDamageMultiRandom` 才是「跑 N 遍」 | `PlayAbility.c:1511-1530`（`case 12`，`local_178` 次循环） |
| 增益族 = `计数 × (change × multiplier)` | `NewBuffStat.c:61-67`（`Normal`）· `:207-232`（`200/201/202/203` 族）· 数据实证 `BL43`（两条 logic：基础 +2 ＋ `meleeAttackValueOptions=201` 那条 `Dark Pact 数 × 2`） |
| 改前我们只在**出牌路**折叠，触发路把 `PerCount` **整条丢掉** | `EffectResolver.cs`（改前）`:496-533` vs `:6484-6498`：后者只打一条 `trigger-repeat-percount` 提示、`opRepeats` 保持 1；而 `ResolveOne/ResolveOneCore` **完全不读 `PerCount`** |

### 2·2 改前 / 改后

| 位置 | 改前 | 改后 |
|---|---|---|
| `RuleEngine/Core/EffectResolver.cs:493-508`（出牌路） | 56 行内联折叠块（计数 → 结构化 `mult` 优先 → `eff = Amount + n×PerCount` → `ScalePayload` → 递副本） | 4 行：`if (IsPerCountIncrement(op)) { TryFoldPerCount(...) → Prepaid → ResolveOne }`。**逐字搬走**，行为不变 |
| `EffectResolver.cs:6437-6481`（触发路） | `PerCount>0 ∧ CountRef≠"" ∧ verb∉{deal,heal}` ⇒ `skipWhy="percount"`，打一条「本重载尚未实现」的 `StructuredConditionNotice`，`opRepeats=1`，原样递 `amtOp`（**增量丢失**） | 同一个入口：`IsPerCountIncrement(amtOp)` ⇒ `TryFoldPerCount(..., actor: source, ...)`；失败（计数数不出来）⇒ `continue`；成功 ⇒ 递 `runOp`。**`percount` 那条 skip 删掉**（不再需要；折叠本身有 `ctx.Log`，不是静默）。`died` 那一档**原样保留**（触发本身就是那次死亡事件，再乘 = 平方） |
| `EffectResolver.cs:11735-11749`（新） | —— | `IsPerCountIncrement(op)`：**判据只此一份**（出牌路 / 触发路都调；`deal`/`heal` 不算这一档） |
| `EffectResolver.cs:11751-11825`（新） | —— | `TryFoldPerCount(ctx, owner, source, by, op, actor, sourceCard, unresolved, out folded)`：`false` = 计数数不出来（日志/`unresolved` 已写，调用方跳过）；`true` 时 `folded` 在原样时**是同一个对象**、折叠时是**副本**（⛔ 不动解析产物） |

⚠️ **`actor` 的口径与 `TryComputeRepeats` 逐字相同**：出牌路传 `chosen`（改前原样）、触发路传**触发的那张单位** `source`（`CountEval.CountTargets` 的 `includeSelf/excludeNotSelf` 用它）。

### 2·3 今天可达性

**0 张可达**（实测 `cards_engine.json` 现读 1127 张）：

| 卡 | 类型 | `abilityTargets[0].trigger` | 说明 |
|---|---|---|---|
| `BL43` | tactic | **0** | `Give +2 [Melee] …, and an additional +2 [Melee] for each Dark Pact on it` |
| `BL47` | tactic | **0** | `Give Armour 1 …, and an additional Armour 1 for each Dark Pact on it` |
| `GOF74` | tactic | **0** | `Give Armour 1 to your Warlord, plus +1 Health for each damaged enemy`（走 `, plus` 那条路） |
| `8f2da557…`（Alpha Warrior） | tactic | **0** | `… plus an additional 1 for each friendly troop`（同上） |

⇒ 触发路今天**没有一张真卡**会走到这条新分支；这条改动是**把口径补正 + 防将来接错**（铁律 11）。
行为零变化的另一半证据：出牌路那 4 张走的仍是**同一份**算法（逐字搬过去的），只是换了个门。

---

## 三 · 件 2：整条 `desc` 的替代行动块

### 3·1 判据（`E6` §3·4 ＋ `CardDef` 现读）

- 替代行动（`Duty` / `Pray` / `Ferocity` / `Agenda` / `Oath`）的效果在
  **`activeAbility/alternativeAbility.activeAbilityLogic`** 里（`CardDef.ApplyActiveAbilityLogic`），
  **不是** `cardAbilities` ⇒ 它的块**不许**贴 `cardAbilities[b]` 的 `ta` / `fm`。
- 词表唯一一份 = `CardDef.ActiveActionKeywordNames`（`CardDef.cs:2354-2357`）＋ `IsActiveActionKeyword`
  （本笔把它从 `static` 开成 **`public static`**，`CardDef.cs:2366`）。
- 两种会走到「整条 desc」的入口（`E6` §3·2 那 6 个调用点的子集）：
  1. **裸写正文**：`CardDef.CollectBareKeywordBody` → `EffectText.Parse(Desc, card: this)` ⇒ `IsWholeDesc` 为真；
  2. **卡面自带 `<词>:` 头**（9 张督军 + 战术卡的 `Oath N:`）⇒ 整条 desc 里那一块。

### 3·2 改前 / 改后

| 位置 | 改前 | 改后 |
|---|---|---|
| `RuleEngine/Core/EffectText.cs:1971`（`AssignAbilityTargets` 的 **full** 分支） | 只过 G1/G2，然后按块下标 `b` 直接贴 `abil[b].ta` / `fm` | ① `if (alternativeActionBody \|\| card.IsBareAlternativeActionBody()) return;`（两道路口、同一口径）；② 逐块 `if (IsAlternativeActionBlockHead(b, blockStartSeg, segLeadKeyword)) continue;` |
| `EffectText.cs:1940-1944` | 形参表到 `alternativeActionBody` 为止 | 新增可选 `List<string> segLeadKeyword`（`Parse` 传，唯一调用点） |
| `EffectText.cs:2514` | —— | `SegResult.HeadKeyword`（块首关键词，小写；三处落值见下） |
| `EffectText.cs:3075 / :3099 / :3152 / :3199` | —— | `Talent:` ⇒ `"talent"`；通用付费前缀 ⇒ `= PaidKind`（`energy/faith/spirit/oath/""`）；`Oath N:` ⇒ `"oath"`；可路由触发头 ⇒ `mtr.Groups[1]`（覆盖前面那个付费词） |
| `EffectText.cs:2028-2077` | `BuildAbilityBlocks(segOps, segHead, segPaid)` | 加 `out List<int> blockStartSeg`（**每块第一段的下标**）；新增 `IsAlternativeActionBlockHead(...)`（判据转调 `CardDef.IsActiveActionKeyword`，⛔ 不另立词表）。三个调用点各传 `out _` / `out var` |
| `RuleEngine/Core/CardDef.cs:2985-2995` | ①/①-bis/② 三道闸内联在 `CollectBareKeywordBody` | 抽成 `BareBodyKeyword()`（`CardDef.cs:3043`，**唯一一份**），`CollectBareKeywordBody` 调它 |
| `CardDef.cs:3021-3022` | `EffectText.Parse(bare ?? Desc, out _, out _, card: this)`（**无** flag） | `alternativeActionBody: IsActiveActionKeyword(only)` —— 与 `AddTriggerOp` 那条片段路**同一条判据** |
| `CardDef.cs:3103-3106` | —— | 新增 `public bool IsBareAlternativeActionBody()` = `IsActiveActionKeyword(BareBodyKeyword())` |

**为什么整条 desc 也要两道路口**（别只留 flag）：`E6T2` 那种探针 / 生产侧的 `CollectBareKeywordBody`
都是**独立的一次 `EffectText.Parse`**，flag 只由 `CardDef` 那一侧传；而 `EffectText` 自己
（`Parse` 被别处直接调用时）只能靠 `card.IsBareAlternativeActionBody()` 判「这张卡的整条 desc 就是替代行动正文」。
两条判据**同源**（都归到 `IsActiveActionKeyword`），不构成第二套口径。

### 3·3 今天可达性

- **生产侧 0 个读者**（`E6` §3·1 现核）：`op.SubjectTargetsAffected` 全仓只有 3 个生产读点，
  两个要求「无施放者」（单位触发的 `source` 恒非空）、第三个只要 `ta ∈ {200,201}`。
- 形状分布（`cards_engine.json` 现读 1127 张，本笔现算）：
  - 单位卡 `keywords` 带替代行动词、`desc` **无** `<词>:` 头 = **11 张**
    （`AM5/AM11/AM16/AM23` · `DA18/DA29/DA71` · `SOR13` · `SW4/SW11/SW21`）；
    其中 `abilityTargets` 非空的只有 **`SOR13`（`ta=[20]`）** 与 **`DA29`（`ta=[20]`）**
    ⇒ 改前这两张的整条 desc 产物会带一个**错来源**的 `20`（其余 9 张空表 ⇒ `AssignAbilityTargets` 第 3 句就 return）。
  - 非单位卡里 desc 带 `<词>:` 块的 = **9 张督军**（`AM1/AM2/AM3` · `DA3/DA75` · `SOR1/SOR2/SOR3/SOR65`），
    督军从不出牌 ⇒ 走不到 `PlayTacticCore`。
  - 战术卡的 `Oath N:` 是**付费前缀**那一支（`PaidKind="oath"`）⇒ 同样不吃 `cardAbilities` 的值。
- ⇒ **今天零可观测差异**；本闸是**防将来接错**：① 某张「裸写替代行动正文」的单位卡拿到 `ta∈{200,201}`
  （触发路会去读 `source.ReferencedCards`）；② 出现一张**战术/防御卡**带替代行动块（`PlayTacticCore` 直接结算）。
  ⚠️ **`fm` 的门槛比 `ta` 低**（`EffectResolver` 的 `repeats` 读点**不需要** `source == null`）—— 同批一起挡住了。
- 回归面（**必须由主对话改**）：`RuleEngineTest_EngineE6.cs` 的 `E6T2` 现在会红 —— 见 §七·1。

---

## 四 · 断言清单（`MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineP2.cs`）

入口 `public static void TestEngineP2()`；`RuleEngineTest.Run()` 里加了 `Step(TestEngineP2);`
（`RuleEngineTest.cs:869-877`）。子用例用 `E1P2…` 打头（⛔ 不是 `Test*`，否则覆盖闸会要求各配一条 `Step`）。

| 用例 | 条数 | 钉什么 | **期望值从哪来** | **灭自证 / 判别式** |
|---|---|---|---|---|
| `E1P2T1TriggerPerCountIncrement` | 12（含前提） | 合成正文 `Give +2 Melee Attack to your troops, and an additional +2 Melee Attack for each friendly troop`：解析出的 `PerCount=2` / `CountRef="own\|troop\|all"` / `Amount=2`；**触发重载**（`ResolveOps(ctx, owner, source, IReadOnlyList, by, seed)`）在 1 个友方部队时给 **+4**（2→6）、2 个时给 **+6**（2→8，两张都算到）；第二次结算仍 **+4**（2→10）；解析产物 `Amount` 仍是 2 | `C3` §1·1（增量 = `基础 + 计数 × 增量`）；`+2 / +2 / "friendly troop"` 都是**正文里印的数**，`4 / 6 / 8 / 10` 是手算字面量 | 三档互斥：**丢掉增量** ⇒ 1 个 = 4、2 个 = 6；**误当跑 N 遍** ⇒ 2 个 = 6（每张 +4）；**增量正确** ⇒ 1 个 = 6、2 个 = 8。`(6, 8)` 这一对**结构上不可能**由前两种实现同时满足。另一条反向读数：同一批 op 连算两次必须是 `+4/+4`（10）而**不是** `+4/+6`（12）⇒ 谁把增量写回 `op.Amount` 必红 |
| `E1P2T2WholeDescAltBlockGate` | 11（含前提） | 三张合成探针：① `E1Duty`（`keywords=["Duty"]` + 裸写正文 `Deal 3 damage`）整条 desc 的 `ta = **-1**`，而运行期 `TriggerOps("duty")` 仍是 **30**（`activeAbilityLogic` 正确来源）、`ActiveAbilityMap = attached`；② `E1Plain`（同样正文/数据、**无**替代行动词）照旧 **25**；③ `E1Blocks`（`Deal 1 damage. Duty: Deal 3 damage`）第 0 块 **30**、第 1 块 **-1** | `E6` §3·4 那笔账（`25 → -1`）；`30` = 探针 `activeAbilityLogic.ta`；`120` = `cardAbilities[1].ta`（**不许**出现） | 三条**互斥**：删「整条 desc 闸」⇒ ①回 25；写成「整卡一律不贴」⇒ ②变 -1；删「块首闸」⇒ ③第 1 块变 120；块首闸放宽成整卡 ⇒ ③第 0 块变 -1。①②③ 四者不可能由同一份实现同时满足 |

**全部写死字面量**（`2 / 4 / 6 / 8 / 10 / -1 / 25 / 30`），⛔ 没有一处从被测实现反算。

⚠️ **本件的断言尚未实跑**（红线：不跑 Unity 批处理）⇒ 需主对话在同步点跑 `RuleEngineTest.Run`
（若引擎线也动过共用件，按铁律 12 的覆盖面判要不要连 `RuleEngineTest.Run` 一起排）。

---

## 五 · 没查清 / 没做

1. **断言没实跑**（见上）。🧨 只是「改坏就红」的**设计**，不是实测结论。
2. **件 1 的「`deal`/`heal` 走重复」那一半没改**：`IsPerCountIncrement` 把 `deal`/`heal` 排除
   （与改前出牌路一字不差），它们继续走 `TryComputeRepeats`。原版这一族**也是倍率进数值**
   （`PlayAbility.c:1461-1508`），也就是说「伤害型的 `PerCount` 该不该也按增量折叠」**没有单独核过**
   —— 今天**数据里一条都没有**（`deal`+`PerCount>0` 的 op 全池 0 条），所以不改、不猜。
   要收口需要先证明「解析层会不会给 `deal` 落 `PerCount`」（`TryForEach` 的追加型对
   `give/gain/lose` 之外**不设** `PerCount`，见 `EffectText.cs:4565-4576`）—— 现读是**不会**。
3. **`E6` §六·3「块序 ≠ `cardAbilities` 序」仍开着**（`SOR65`：desc 印 `Pray … Talent …`，
   `abilityTargets` 数组序是 `cardAbilities` 序列化序）。本笔的块首闸让**替代行动块**永远不吃
   `cardAbilities` 的值 ⇒ 那一类**错位**少了一半风险，但**非替代块**（例如 `Talent` 块）仍按块下标硬配。
   要根治得有「触发头 ↔ `AbilityTrigger` 枚举」的映射表（`Pray=655 / Duty=662 / Ferocity=725 / Agenda=705 / Talent=46` 现成），
   落地位置仍缺（`AssignAbilityTargets` 拿不到「这一块属于哪条 ability」）—— 与 `E6` 同族，**留给调度台分流**。
4. **原版实机未验**（原版已关服），判据停在反编译 + 卡表两层。

---

## 六 · 顺手发现

1. 🔴 **`RuleEngineTest_Diag.cs`（生成物）已经落后**：它是 `工具/gen_diag_runner.py` 生成的
   `RunCaseNames` 表，**反射对账**那两条闸（`VerifyDiagCoverage` ② / `DiagDriftAlarm`）拿它逐条比。
   本笔新增的 `TestEngineP2`（以及 **E6 那批的 `TestEngineE6`**）都不在表里
   ⇒ 每次 Unity 起来 `DiagDriftAlarm` 会 `LogError`「诊断入口过期」（**只报警、不进 `_fail`**）。
   处置（**在白名单外**，需主对话/工具代理）：`PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/gen_diag_runner.py`。
   ⚠️ 顺手核了一下：`RuleEngineTest.cs` 自己的 `VerifyRunCaseCoverage` **两条闸都是反射**
   （`RunCoverageCompiledCases()` 与 `DiagCompiledCaseMethods()`），两边都看得见 `TestEngineP2` ⇒ **那两条不会红**。
2. ℹ️ `CardDef.CollectBareKeywordBody(IEnumerable<string> keywords)` 的 `keywords` 形参现在
   **只用于 null 守卫**（判据改走 `_keywords`）。行为等价（实测：全池 1127 张里
   「旧口径 `NormalizeKeywordName(raw)` 的 body-keyword 集合」与「新口径 `_keywords` 键」
   **逐张相同，差异 0 张** —— 唯一理论差异是 `About to attack` 那种带空格的词，现池 0 张同时命中）。
   形参保留 = 零行为变化；要清理另开一笔。
3. ℹ️ `IsActiveActionKeyword` 从 `private` 开成 `public static`（`EffectText` 要用）。全仓另一处
   近似的「四词表」在 `RuleCore.cs:10393`（**不含 `oath`**，语义是「可用的替代行动」）—— 两处**不是**
   同一件事，本笔没动它（`CardDef` 那段 doc 本来就写明了这个区别）。
4. ℹ️ 战术卡的 `Oath N:` 块会因 `PaidKind = "oath"` 被块首闸挡住。**今天无影响**（那些卡的
   `blocks.Count` 基本过不了 G1；即便过了，`PlayTacticCore` 结算的 `Oath` op 本身也该由
   `CollectOathOps` / 激活路消费）。⚠️ 若同步点跑 `RuleEngineTest.Run` 时看到 `Oath` 相关断言变红，
   先回来看这一条（本笔**没有**逐张核 15 张 `Oath` 战术卡的 `blocks == abilities` 是否成立）。

---

## 七 · 要主对话做的（逐条，都在本代理白名单外）

1. 🔴 **改 `CardPresentation/Editor/RuleEngineTest_EngineE6.cs` 的 `E6T2`**（本笔的件 2 会让它红）。
   `:142-146` 现在是：
   ```csharp
           CheckTrue(fop != null && fop.SubjectTargetsAffected == 25,
                 "★★ ② **整条 `desc` 那条路仍会把 `cardAbilities` 的 `ta` 贴到替代行动块上** —— "
               + "这里贴到的是合成表里 `abilityTargets[0].ta = 25`（E1 只堵了**片段**分支）"
               + "｜🧨 给 full 分支也加同一口径的替代行动闸 ⇒ 变 `-1` ⇒ 红；"
               + "**那时把这条改成 `-1`、并在报告里核销这笔账**（⛔ 别只是删掉它）");
   ```
   ⇒ 改成（`== -1` + 文案改成「已核销」，⛔ 别删这条）：
   ```csharp
           CheckTrue(fop != null && fop.SubjectTargetsAffected == -1,
                 "★★ ② **整条 `desc` 那条路的替代行动块不再贴 `cardAbilities` 的 `ta`** —— "
               + "合成表里 `abilityTargets[0].ta = 25` **不许**出现，留 `-1`（读不到）"
               + "（2026-10-24 `E1` 把 full 分支的闸补上了，`E6` §3·4 那笔账**已核销**）"
               + "｜🧨 撤掉 `AssignAbilityTargets` full 分支那道替代行动闸 ⇒ 回到 `25` ⇒ 红");
   ```
   同文件 `:94-99` 那段 doc（「整条 desc 的产物带 **25**，而运行期结算的那条 op 是另一个对象、带 30」）
   也要跟着改成「整条那条现在是 **-1**（闸挡住），运行期仍是 30」。
2. ⚠️ **`RuleEngineTest_Diag.cs` 重生成**（见 §六·1）。
3. ✅ 同步点跑 `RuleEngineTest.Run`（本件的 23 条断言首次实跑；同时会把 `E6T2` 的旧期望暴露出来 ——
   改完 §七·1 再跑，否则那条必红）。

---

## 八 · 改动清单（全部在白名单内）

| 文件 | 改动 | 行尾 |
|---|---|---|
| `RuleEngine/Core/EffectResolver.cs` | 出牌路折叠块抽成 `TryFoldPerCount`（`:493-508`）；触发路改调它并删 `percount` skip（`:6437-6481`）；新增 `IsPerCountIncrement` / `TryFoldPerCount`（`:11735-11825`） | **CRLF**（13611/13611，改完现数） |
| `RuleEngine/Core/EffectText.cs` | `SegResult.HeadKeyword`；`segLeadKeyword` 收集；`BuildAbilityBlocks` 加 `out blockStartSeg`；`AssignAbilityTargets` 整条 desc 分支的两道路口闸 ＋ 块首闸 ＋ `IsAlternativeActionBlockHead` | **LF**（10277/10277） |
| `RuleEngine/Core/CardDef.cs` | `IsActiveActionKeyword` → `public static`；`BareBodyKeyword()`（从 `CollectBareKeywordBody` 抽出，唯一一份）；`IsBareAlternativeActionBody()`；`CollectBareKeywordBody` 给 `Parse` 传 `alternativeActionBody` | **LF**（7079/7079） |
| `RuleEngine/Editor/RuleEngineTest.cs` | `Step(TestEngineP2);` ＋ 一段注释（用例体在新分册） | **CRLF**（36979/36979） |
| `RuleEngine/Editor/RuleEngineTest_EngineP2.cs`（新建，+`.meta` `795f3cbb904041eca36aa422bf5ee16a`） | `TestEngineP2()`，两段 23 条断言 | **LF** |
| `资料/普查产出_1024c/E1_PerCount与desc闸.md` | 本报告 | LF |
