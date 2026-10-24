# E8 · `RequireActorInPlay` / `abilityContext`「缺行 = `Play(0)`」的前提②（2026-10-24）

> 执行代理 `E8`。判据不由本报告论证 —— 判据正本 = `资料/普查产出_1024c/A2_引擎战斗判据总表.md` **Q3**。
> 本报告只写：**改了什么 · 为什么这么落 · 逐 trigger 结论 · 断言 · 预期行为差 · 没查清 · 顺手发现**。

---

## 一、判据（出处，⛔ 不重新论证）

| # | 结论 | 出处 |
|---|---|---|
| 1 | 原版 `abilityContext` 的闸在**入队前**（广播那一刻同步判），`IsRightContext` 全库只有一个调用者 | `d:/2/tools/decomp_full/RawCardScript__TriggerAbilities.c:108,207` · `CardAbility__CanTriggerAbility.c:126` |
| 2 | 六档语义（`0 Play` 三闸 = `IsInPlay ∧ !EnoughPendingDamageToDie ∧ !isRemnant`） | `CardAbility__IsRightContext.c:18-72` · `AbilityContext.cs:1-9` |
| 3 | 我们的 `QueuedAction.RequireActorInPlay`（出队复核）**只是那道闸的出队旧近似**，原版没有出队那一道 | `Core/ActionPump.cs:775-776` · `A2` Q3 §2 |
| 4 | **「缺行 = `Play(0)`」必须同时满足两条前提**：① 表 `version ≥ 14`；② 该 trigger **在这张卡的 `abilityTriggers` 里**（`AbilityTriggerPresence == 1`） | `A2` Q3 §4 · 判据链 `CardAbility__CanTriggerAbility.c:126`（逐 ability 行判） |
| 5 | skip 表：trigger `0/80/145/220/500` 整段不读；`600/605` 在**余额前**显式查 | `CardAbility__CanTriggerAbility.c:74-83,86-95,114-124,126` |

**为什么前提② 是硬要求**：生成器只落 `context != 0` 的行（`工具/gen_cards_engine.py` 的
`ABILITY_CONTEXT_DEFAULT = 0`），这条口径的立足点是「**那一行本来存在**」。
若这张卡**没有**这条 trigger 的 ability，则「查不到行」的成因是「没有这条 ability」而不是「`0` 被省略」
⇒ 推成 `Play(0)` 就是**给一条不存在的 ability 编口径**。
同一条纪律其实早就写在 `EffectResolver.AbilityContextOfAbilityV14` 的「调用前提」那段
（`Core/EffectResolver.cs:8899-8904`），只是**没被执行**。

---

## 二、改前 / 改后

### 改前（`ActionPump.cs:361-372`，2026-10-23 `W63` / 2026-10-24 `A1` 那一版）

```csharp
int c = AbilityContextOfAbility(card, triggerName, keyword);
if (c != Unknown) return c;
string tn = string.IsNullOrEmpty(triggerName) ? TriggerNameOfKeyword(keyword) : triggerName;
if (!string.IsNullOrEmpty(tn) && card.AbilityContextsVersioned)
    return CardDef.AbilityContextPlay;      // ← 只有前提①，「认得出 trigger 名」被当成前提②
return CardDef.AbilityContextUnknown;
```

后果（现读 `cards_engine.json` **v24 / 1127 张**）：`Slay(120)` 只有 **16** 张卡真有，
但 `AbilityContextOfAbilityV14(任意卡, "Slay", "slay")` 对 **1127 张全部**返回 `0`
（`RuleEngineTest_Priority.cs:495-500` 的「全池每一张卡的 `Slay` 都读成 `Play(0)`」就是旧口径的画像）。

### 改后（`ActionPump.cs:390-408`）

```csharp
int c = AbilityContextOfAbility(card, triggerName, keyword);
if (c != Unknown) return c;                        // ①真值行优先（40/50 照旧）
string tn = string.IsNullOrEmpty(triggerName) ? TriggerNameOfKeyword(keyword) : triggerName;
if (string.IsNullOrEmpty(tn) || !card.AbilityContextsVersioned) return Unknown;   // 前提①
if (card.AbilityTriggerPresence(tn) != 1) return Unknown;                        // 前提②（本次新增）
return CardDef.AbilityContextPlay;
```

- 唯一取值口仍是 `RuleCore.AbilityContextOfAbilityV14`（⛔ 没有第二份实现）：消费端
  `ActorMustBeInPlayForTrigger`（`:294`）、`RuleCore` 的四处入队点、`EffectResolver` 的四处
  全部**自动**继承这条前提，调用点一行没改。
- **偏差说明（铁律 5）**：`A2` Q3 的「建议落点」写的是 `CardDef.AbilityContextOfTrigger/OfKeyword`。
  **没有照做** —— 那两个是**原始访问器**（`_EngineA1.cs:431` 明确断言它必须返回 `-1`，用来证明
  「确实是补 0 那一跳在供值」）。把前提② / 补 0 塞进去会毁掉这对分工。
  ⇒ 落在唯一消费口 `AbilityContextOfAbilityV14`，与 `EffectResolver` 那段「转发、别另写」的注释一致。
- `CardDef.cs:1708-1714` 补了一段「两个前提」的合同说明（指向唯一实现）。
- `ActorMustBeInPlayForTrigger` 的 doc / 注释补齐 `-1` 的**第三、四条来路**
  （认不出 trigger 名 · 这张卡本来没有这条 trigger，`:265-269` / `:305-310`）。

---

## 三、逐 trigger 结论表（状态 → 结论）

现读 `cards_engine.json` v24（1127 张 / `abilityContext` 157 卡 170 行；
`context` 分布 `{50:82, 40:71, 10:16, 30:1}`，**没有 0 行** = 生成器只落非 0）。

| trigger | 名字 | 原版怎么处理 | 我们落点 | 卡数（`abilityTriggers` 里有） | 非 0 行 |
|---|---|---|---|---|---|
| 0 | ThisCardPlayed | **skip（整段不判）** | `TriggerSkipsAbilityContext` → `false` | 586 | 24（被 skip 忽略，符合判据） |
| 80 | ThisUnitDeath | **skip** | 同上（名字 + 关键词 `backlash`） | 6 | 0 |
| 145 | Codex | **skip** | 同上（名字 + 关键词 `codex`） | 34 | 3（同上） |
| 220 | Sacrifice | **skip** | 同上（名字 + 关键词 `sacrifice`） | **0**（今天零可达） | 0 |
| 500 | TriggerEnchantment | **skip** | 同上（名字） | 4 | 0 |
| 600 | UseSpiritStone | **余额前显式查** | `EffectResolver.cs:7752`（`ResolveSpiritAbility`，`requireActorInPlay:false` 只当 `-1` 回退档） | 29 | 13(50) |
| 605 | UseFaith | **余额前显式查** | `EffectResolver.cs:7929`（`ResolveFaithAbility`，同形） | 26 | 14(50) |
| 50 | UnitAttack | `:126` 兜底 | `RuleCore.cs:6075/6084`（同时传 `requireActorInPlay` + context） | 13 | 9(40) |
| 120 | Slay | `:126` | `RuleCore.cs:9344/9353`（Slay 那一跳还叠 `RequireActorInPlayOrDying`） | 16 | **0**（全族 `0` ⇒ 旧 5 词表里也有 `slay`） |
| 300 | Mob | `:126` | `RuleCore.cs:11145/11152` | 16 | 0 |
| 560 | Artifice | `:126` | 关键词路 `RuleCore.cs:11045/11049` | 19 | 0 |
| 580 | Strike | `:126` | 关键词路（同上） | 30 | 5(40) |
| 195 | Rally | `:126` | 关键词路 | 1 | 0 |
| 280 | AboutToAttack | `:126` | 关键词路 | 1 | 0 |
| 301/670/715 | Regiment/Ambush/Ecstasy | `:126` | 关键词路 | 13/12/10 | 13/11/8 |
| 410/81/90/180/570/… | 死亡 / 监听族 | `:126` | `EffectResolver.cs:6983`（`DeadUnitTrigger` 牌桌循环）· `:8955`（`PendingDeathAllowsListener`） | 30/18/28/4/5 | 19/18/14/1/1 |

**判据 → 我们这条的新结论**：
- skip 五族：**不判**（返回 `false`），且**不看** `presence`（原版那几族整段跳过 `IsRightContext`）；
- 600/605：**逐卡查**，有非 0 行（50）⇒ 50；缺行且 `presence == 1` ⇒ `Play(0)`；`presence != 1` ⇒ `-1`（回退）；
- 其余：有非 0 行 ⇒ 那一行；缺行 ∧ 前提①②都成立 ⇒ `Play(0)`；否则 `-1`。

---

## 四、断言（新增 `RuleEngine/Editor/RuleEngineTest_EngineP3.cs`）

> 入口 `TestEngineP3()`；子用例 `P3…` 打头（⛔ 不用 `Test*` —— 反射覆盖率闸会要求 `Run()` 里各有 `Step`）。
> 期望值全部写死字面量（`0` / `-1` / `40` / `50`），⛔ 不从实现常量反算。

| 编号 | 断言 | 期望值来源 | 灭自证（🧨 怎么让它红） |
|---|---|---|---|
| ⑨ | 池内找「真有 `Slay` + 无非 0 行」的卡：原始访问器 `-1`、`V14 == 0`、`ActorMust == true` | `AbilityContext.Play = 0`（`AbilityContext.cs:1-9`）+ 生成器「只落非 0」 | 删「缺行 = Play」那一跳 ⇒ 实得 `-1` |
| ⑩ | 同一张卡（`presence(Slay)=presence(Strike)=presence(Artifice)=0`）：`V14(Strike) == -1` · `ActorMust(Strike) == false`；对照 `ActorMust(slay) == true`（旧 5 词表回退） | `A2` Q3 §4 前提② + 旧表 `LegacyActorMustBeInPlayForKeyword`（`ActionPump.cs:454-461`） | **把 `AbilityTriggerPresence(tn) == 1` 删掉 / 写成 `!= 0` / `!= -1`** ⇒ 退回「一律 Play」⇒ 两条都变 ⇒ 红 |
| ⑩·结构性 | 全池（1127 张）：「`Slay` 读成 `0`」**⇔**「真有 `Slay` ability」；`nViolation == 0`，且 `0 < nPlay < 全池` | 两列是**独立数据**（v14 `abilityContext` / v17 `abilityTriggers`） | 退回旧口径 ⇒ `nPlay` = 全池（1127≠16）⇒ 红；删「缺行 = Play」⇒ `nPlay = 0` ⇒ 红 |
| ⑪ | 有真值行：`Strike` 的 `40` 原样返回 40、`ActorMust == false`；`UseSpiritStone` 的 `50` 原样返回 50、`ActorMust == false` | `IsRightContext.c:52-55`（40 含待死）/`:66-72`（50 不含待死）；600 的「余额前显式查」= `CanTriggerAbility.c:114-124` | 写成「先补 0 再查行」⇒ 实得 `0`/`true` ⇒ 红（`W63 #2` 那一族回归口） |
| ⑫ | skip 五族逐族：找「真有该 trigger + 无非 0 行」的卡，`ActorMust == false`（`0`/`80`/`145`/`500` 各一张真卡；`80`/`145` 还各验一次**只给关键词**） | `CanTriggerAbility.c:74-83` | 删 skip 表里那一族 ⇒ 那张卡（真有 ability + 缺行）被读成 Play ⇒ 实得 true ⇒ 红。⚠️ `220` 池内 0 张 ⇒ 只用合成卡验「放行」那一半 |
| ⑬ | 合成夹具卡（无 v14/v17 列）：`presence == -1`、`V14 == -1`、`ActorMust(slay) == true` / `(strike) == false`（**旧口径回退，逐字同改前**） | `CardDef.cs:759/790` 两个 `versioned` 形参默认 `false`；`-1` 契约 | 把 `-1` 也走「缺行 = Play」，或把 `presence != 1` 写成 `presence != -1` ⇒ 实得 `0`/`true` ⇒ 红 |

**静态核对过的前提**（用 `cards_engine.json` 现算，不依赖实现的取值）：
`Slay` 真卡 **16** 张 · `presence(Slay)=presence(Strike)=presence(Artifice)=0` 的卡 **1062** 张 ·
`Strike=40` **5** 张 · `UseSpiritStone=50` **13** 张 ·
skip 族「`presence==1` 且无非 0 行」= `0:562` / `80:6` / `145:31` / `500:4` / `220:0`。

---

## 五、预期行为差（改前 vs 改后）

**方向**：改前「versioned ⇒ 一律 `Play(0)`」；改后「缺行 + 两前提成立 ⇒ `Play(0)`，否则 `-1`」。
⇒ 只有 **`presence != 1` 的卡**会从「`Play(0)`」变成「`-1`（未知 ⇒ 回退旧口径）」。

逐 trigger 的读数（`旧 Play` = 全池 − presence；`新 Play` = presence）：

| trigger | 旧 Play | 新 Play | 变化面 |
|---|---|---|---|
| 120 Slay | 1111 | **16** | 1111 |
| 580 Strike | 1097 | **30** | 1097 |
| 300 Mob | 1111 | **16** | 1111 |
| 50 UnitAttack | 1114 | **13** | 1114 |
| 600 UseSpiritStone | 1098 | **29** | 1098 |
| 605 UseFaith | 1101 | **26** | 1101 |
| 560 Artifice | 1108 | **19** | 1108 |
| 90 OtherUnitDeath | 1099 | **28** | 1099 |
| 410 FollowUpResolution | 1097 | **30** | 1097 |

**但「变化面」≠「可见行为差」**：`presence == 0` 意味着**原版本来就没有这条 ability**
⇒ 那一行 `IsRightContext` 在原版从不执行 ⇒ 理论上没有正文可跑。真正会变的只有**闸本身**：

1. **入队闸**（`EnqueueAbility` 的 `abilityContext`）从「要求活着在场」变成「不判」——
   只有当我们**解析出了这条 trigger、而原版真值里没有它**（= 我们多解析 / 幽灵能力）时才可见；
2. **出队旧近似**（`RequireActorInPlay`）在 `-1` 时启用：对旧 5 词表
   （`rally/mob/slay/artifice/abouttoattack`）仍是「死了就取消」，对其它关键词是「不判」——
   与改前的**唯一差别**是「入队时判」→「出队时判」（原版没有出队那一道，属已知偏差，见 `A2` Q3 §2）；
3. **监听者待死闸** `EffectResolver.PendingDeathAllowsListener:8955`：`presence != 1` 的卡现在落回
   `allowPendingDeath`（改前口径），不再被当成 `Play(0)` 挡；
4. **`EmitCardTrigger`**：`HasTriggerSource` 允许「**有动画行、没有 ability**」的卡发表现事件
   （`EffectResolver.cs:7110-7121`）⇒ 这类卡在 `:6983` 的循环里，改前会被 `Play(0)` 闸挡、改后不会。
   ⚠️ 这是本次**唯一能看见的表现侧差异**，属已知形状，未跑实况。

**`ruleprobe` 预期读数 = 0**（这条要说清）：`工具/ruleprobe/Program.cs` **没有调用**
`AbilityContextOfAbilityV14` / `ActorMustBeInPlayForTrigger`（grep 0 命中）—— 它只把
`abilityContext` / `abilityTriggers` 两列传进 `CardDef` 并在数据侧对账
（`:578-580` 的 157 张 / 170 条）。⇒ **本次改动在 `ruleprobe check` 上应当零变化**；
「全池行为差」只能从引擎侧（`RuleEngineTest.Run` / `BattleScene.Run` 的日志）读。

---

## 六、⚠️ 会变红的**既有断言**（不在 E8 白名单 ⇒ 请主对话改）

这三处断言的**画像本身就是旧口径**（「versioned ⇒ 全池 Play」），补了前提② 之后必须改：

1. **`RuleEngine/Editor/RuleEngineTest_EngineA1.cs:433`**（`A1MissingAbilityContext` ⑤）
   —— 它选的卡 = 池内第一张「`AbilityContextsVersioned` 且 `Slay`/`Strike` 都没行」的卡
   （现读 = `cards[0] ASH83 Autarch`，`abilityTriggers = [600]`）⇒ 现在 `V14(Slay)` 返回 `-1`。
   **建议改法**：选择谓词加一条 `c.AbilityTriggerPresence("Slay") == 1`（另找一张 `strikeCard`
   给 `:440` 那条），并把文案「两行都没落」改成「那一行没落、但这张卡**真有**这条 ability」。
2. **`RuleEngine/Editor/RuleEngineTest_EngineA1.cs:440`** —— `ActorMustBeInPlayForTrigger(versioned, "Strike", "strike")`
   期望 `true`。同上的卡没有 `Strike(580)` ⇒ 现在回旧表（`strike` 不在 5 词表）⇒ `false`。
   建议改成：`strikeCard`（`presence("Strike") == 1` 且无非 0 行）期望 `true`；
   再加一条**负对照**：`presence("Strike") == 0` 的卡期望 `false`（= 我在 ⑩ 里钉的那条）。
3. **`RuleEngine/Editor/RuleEngineTest_Priority.cs:495-500`**（`W61T27ActorLeftPlayCancels` ⑩）
   —— `Check(nHopPlay, pool10.Count, "全池每一张卡的 `Slay(120)` 都读成 `Play(0)`")`。
   **建议改法**：改成与我 ⑩·结构性 同形的等式：
   `nHopPlay == 全池里 `AbilityTriggerPresence("Slay") == 1` 的卡数` ∧ `nViolation == 0`；
   文案同步改成「**真有这条 ability 的卡**缺行 ⇒ `Play(0)`」，并把 `🧨` 改成
   「把 `AbilityTriggerPresence(tn) == 1` 删掉 ⇒ `nHopPlay` 变全池 ⇒ 红」。

> 其余既有断言不受影响（已逐条核）：
> `EngineA1.cs:444`（skip 表）、`EngineB3.cs:157-171`（AM9 = 50 有真值行；AM34 有 180 真值 ⇒ 仍 `Play`）、
> `EngineP2` / `EngineE6` 与本改动无交集。

---

## 七、没查清

1. **没跑 Unity**（红线）⇒ 上面 ⑨～⑬ 的**运行时红绿未验**；只做了「源码 + 数据静态核对」。
2. **`presence != 1` 的卡在引擎里实际被问到的频次**没法静态算（取决于 `TriggerOps` 解析结果与广播路径）。
   我能给的只有「旧 Play / 新 Play」的**上界读数**（第五节表）。
3. **`220 Sacrifice`** 池内 0 张卡有该 trigger ⇒ skip 表这一档**今天零可达**，只能验「放行」那一半。
4. `jamUnit` / `transformUnit` / `removeEffect` 三路「主动取消」仍零可达（既有账，不是本次引入）。
5. 没有核 `A2` Q3 提到的那 4 处「出队闸全 0」的反编译计数（1873 条）；本轮直接引用判据代理的结论。

## 八、顺手发现

1. **`ruleprobe` 看不见这次改动**（grep 无 `AbilityContextOfAbilityV14` 调用）⇒ 若台账想让
   「全池行为差」有机器读数，得给 `ruleprobe` 加一个 `contextDecision` 转储，或在引擎侧记一行。
2. **`EffectResolver.EmitCardTrigger` 的 `HasTriggerSource`**（`Core/EffectResolver.cs:7110-7121`）
   第一优先是 `HasAnimTrigger` ⇒ 「有动画行、没有 ability」的卡会**照发**表现事件并**不**出声
   （只有「有 ability、没动画行」那一支才 `ctx.Log`）。配合本次 `-1` 面变宽，这类卡在
   `:6983` 的死亡族循环里可能多发一拍。**是否真有这种卡、发出来像不像**，需要跑一次 `BattleScene.Run`
   或看实况，本报告不猜。
3. **判据 Q3 的「建议落点」与 `_EngineA1` 的原始访问器分工冲突**（见第二节「偏差说明」）——
   已在代码里记了理由，建议台账同步一句，免得下一个人照着 Q3 又把它塞回 `CardDef`。
4. `_同步点待办.md` §七 的「`RequireActorInPlay` 的等价语义」可以核销（判据 Q3 + 本次落点）；
   但**必须连带上面三条既有断言的修改**，否则 `RuleEngineTest.Run` 会挂着 3 条红。
5. `RuleEngineTest.Run()` 需要加一行 `Step(TestEngineP3)`，并跑 `python 工具/gen_diag_runner.py`
   重生成（`RuleEngineTest.cs:1076` / 覆盖率闸 ② 比的是生成物里的用例清单）。

---

## 九、改动清单（文件:行号）

| 文件 | 改动 |
|---|---|
| `RuleEngine/Core/ActionPump.cs:390-408` | `AbilityContextOfAbilityV14` 新增前提② `card.AbilityTriggerPresence(tn) == 1`，否则 `-1` |
| `RuleEngine/Core/ActionPump.cs:342-389` | 该方法的 doc 补「为什么必须有前提②」逐条 + 现读数据画像 |
| `RuleEngine/Core/ActionPump.cs:265-269` | `ActorMustBeInPlayForTrigger` doc：`-1` 的「这张卡没有这条 trigger」档从本日起真的会出现 |
| `RuleEngine/Core/ActionPump.cs:305-310` | 该方法的行内注释：`-1` 的四条来路 |
| `RuleEngine/Core/CardDef.cs:1708-1714` | `abilityContext` 那一节补「缺行 = `Play(0)` 的两个前提」合同（指向唯一实现） |
| `RuleEngine/Editor/RuleEngineTest_EngineP3.cs`（新建） | `TestEngineP3()` + 五个子用例（⑨～⑬） |
| 本文件 | 报告 |

> 未动（白名单外，或不在本件范围）：`RuleEngineTest.cs`（**主对话**加 `Step`）·
> `RuleEngineTest_EngineA1.cs` / `RuleEngineTest_Priority.cs`（**第六节那 3 条要主对话改**）·
> `EffectText.cs` / `GameplayVariables.cs` / `CardDatabase.cs` / `CardPresentation/**` / 正本两份。
> 类型检查：`TMPDIR=/tmp/wf_e8 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
