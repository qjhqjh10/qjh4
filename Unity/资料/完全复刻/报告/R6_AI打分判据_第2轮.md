# R6 · AI 打分与动作选择判据（第 2 轮）· 执行报告

> 第八十会话 · 代理 **R6**（只查判据 / 只写判据文件）。
> 交付物：`资料/完全复刻/判据/AI打分与动作选择_第2轮.md`（逐函数判据正本）。
> 本轮**没有改任何 `.cs`、没有动 `账/*.tsv`、没有动 git、没有跑 Unity、没往 `D:/2` 写任何东西**。

---

## §一 一句话结论

**「判据已在本地、只是没人去读」这个前提只对一半**：
`.c` 在 AI 打分这条链上**只给骨架、不给算式**（`AI__ScoreFromCriteria.c` / `AI__ScoreFromAttack.c`
的函数返回值与**每一处浮点累加**都被 Ghidra 丢了，签名甚至被反成 `void`），
`cpp2il` 则**整个 switch 体都没有**。
⇒ **真正的判据只能从 `GameAssembly.dll` 的指令流读**。本轮已把
`ScoreFromCriteria` 的 **20 个有实体的 `scoringOption` 分支**（含本会话新解的 36 项跳转表 `RVA 0x93B148`）
逐条读出来落成公式；其余 7 个函数（`GetActionScore` / `ExecuteAction` / `MeetsConditionCriteria` /
`MeetsStatCriteria` / `Evaluation.Check` / `GetValueFromCriteria` / `CanPlayAbility`）的 `.c` 是**完整**的，
已逐句落表。

---

## §二 读了哪些函数（逐条一句话）

| # | 函数 | 结论一句话 |
|---|---|---|
| 1 | `AI.ScoreFromCriteria`（RVA `0x93A290`，0xEAF 字节） | 20 个 `scoringOption` 分支全部读出：`acc` 初值 = `baseValueFriend/Enemy`（按 `targetCard` 是否异阵营选），循环支一律 `acc += f(t)·multiplierCount·(同阵营? multFriend : multEnemy)`；只有 `multiplierCriteria.targetsAffected != any` 时 `multiplierCount != 1`；`option 9/10` **覆盖**累加器而非累加；`option 35` 的 side 系数只比 `thisCard.side == 0`；牌库空 ⇒ `-2 − 该方疲劳`。 |
| 2 | `AI.GetActionScore`（RVA `0x938970`） | `.c` **完整**。按 `actionType` 分派：`playCard→ScoreFromPlayingCard` / `1,2→ScoreFromAttack(+合击斩督军 1e4)` / `5→先 nonTarget(+0x58) 再 cardTarget(+0x50，需 targetCard≠null)` / `6→50.0f` / `3,4→0`；`GetActiveAbility()==null` 只 `LogWarning("Null ability for "…)` 且该支 0 分。 |
| 3 | `AI.ExecuteAction`（RVA `0x9382B0`） | `.c` **完整**。全部落到 `BattleManager` 动作队列：`playCard→BattleActionType 5`（小兵额外取 `GetNextSlotCheckingAdjacents()`）/ 近战远程 `ChangeAttackType+AddAttackAction` / `activeAbility→30` / `clickWaystone→76`；末尾 `PlayRandomAIVoiceLine`；未知类型 `LogError("Can't ExecuteAction for type "…)`。 |
| 4 | `FilterMethods.MeetsConditionCriteria`（390 行） | `.c` **完整**。`UnitConditions` 35 个成员逐值落表（**其中 `bloodAngelsOrUltramarines(290)` / `mutation(300)` / `specialQuest(310)` 原版没实现，落 `default → LogError→false`**）；尾判 `exclude ? !r : r`；`flying` 实为 `(unstoppable‖flying) && !vanguard`。 |
| 5 | `FilterMethods.MeetsStatCriteria`（34 行） | `.c` **完整**。11 档逐值一致；`case 6` 加 `altValue` 后**掉进 case 3**；**没有 `case 8 RandomChance` ⇒ 恒 `return true`（不掷骰）**。 |
| 6 | `Evaluation.Check`（123 行） | `.c` **完整**。🔴 **它不是胜负/威胁评估** —— 全量反编译里**唯一调用点**是 `Everguild.LiveOps.AttributeCondition.Validate`（活动条件），与战斗 AI 无关，我方**不需要实现**。 |
| 7 | `AbilityLogic.GetValueFromCriteria`（366 行） | `.c` **完整**。26 个 `ChooseValueOptions` 逐值落表；随机只出现在 `Normal(0)` 且走 `BattleManager.GetRandomInt`（可复现）；多数支有 `min==0 ? value : min*value` 的收尾。 |
| 8 | `AbilityLogic.CanPlayAbility`（76 行） | `.c` **完整**。**只有 7 档需要目标**（`doDamage/doDamageMultiRandom/doDamageFast/doDamageAndDestroySelf/doHeal/buffOthers/buffOthersNotResetPool`），其余**一律 true**。 |
| 9 | `AbilityLogic.GetTargets`（1427 行） | 分派键 = `targetsAffected`，Ghidra 摊成 if 链（**34 个分支值**已落表）+ `targetSelectMethod` 一处真 `switch`；**返回的是静态池**；通用尾链 = `CheckIfMeetsCriteria` 反序过滤 → `maxTargets` + `targetSelectMethod` 截断（`default` 含 `random`/`cardTarget` = 随机挑）。 |
| 10 | 附带（`ScoreFromAttack` / `ScoreFromBuffedAttack` / `ScoreFromDamagingBuffedUnit` / `ScoreFrom*` 八条 / `PlayTurn` / `TweakAvailableActions` / `SelectBestAction` / `GetAiMulliganCards`） | 公式已落 `判据` §6；`scoreFromBuffedAttack` 的 `.c` 是**正常 float 版**（可整份当真）。 |

---

## §三 关键发现（按重要性）

### 3·1 🔴 `AI__ScoreFromCriteria.c` **不是「完整 367 行」**（要改 G3 的结论）

- 签名被反成 **`void`**（真函数返回 `float`，`AI__GetActionScore.c:85` 拿它当 float 用）；
- **每一处浮点累加都丢了**：`case 1` 两次 `Count()` 的结果被丢弃、`case 2` 的 `GetValueInPlay` 返回值被丢弃、
  `case 3/4/5/…` 的 helper 返回值全被丢弃；
- 函数尾只剩 `return;`（`:364-365`）。
- **同族**：`AI__ScoreFromAttack.c`（93 行）也是 `void`；而 `AI__ScoreFromBuffedAttack.c`（134 行）**是正常的 float**。

⇒ G3 §三·A #3 与 §四·4 里「`.c` 完整」、以及「判据已在本地，只是没人去读」的口径，
**在这几条上要改成**「`.c` 给骨架；算式读 RVA」。

### 3·2 cpp2il 的 `ScoreFromCriteria` 缺的段（逐项）

只有「prologue + `codexEffect` 闸 + 两次 `GetTargets` + `return default`」。缺：
① 整个 `switch(scoringOption)`（20 个有实体分支）；
② `baseValue` 选择（只剩一个空心 `if (targetCard != null) {…}`）；
③ `multiplierCount` 的计算（**cpp2il 里根本没有这个量**）；
④ `acc += f(t)·M·mul(t)` 的累加模式；
⑤ 返回值（`return default` ⇒ 恒 0）；
⑥ 顺带：`ScoreFromAttack` 里的 criteria 参数被塌成 `null`，真值是 `actingCard.rawCardData.attackScoringCriteria`（`RawCardScript + 0x300`）。

### 3·3 本会话新解的两件「结构性事实」

1. **`ScoreFromCriteria` 的 switch 是 RVA 跳转表**：`RVA 0x93B148`，36 个 4 字节 RVA 项，
   `cmp eax,0x23; ja default`。⇒ 36 个 `scoringOption` 的落点**全部可枚举**（已在判据 §1·4 列表）。
2. **`AbilityLogic.GetTargets` 返回的是静态池 `pooledPossibleTargets`**（入口先 `Count=0 + Array.Clear` 再填）。
   ⇒ **调用方不许缓存返回值**；这是我们 `Data/AiTargets.cs` 必须对齐的一条硬约束。

### 3·4 常量真值（本会话读 `.dll` 字节复核，全部与我现在方实现一致）

`10000.0f`（合击斩督军，RVA `0x34B32F4`）· `50.0f`（`clickWaystone`，`0x34B2ECC`）·
`0.1f`（挨反击仍存活，`0x34B2DC4`）· `0.7f`（poisonous 追加分，`0x34B3188`）· `1e6`（斩督军）。

### 3·5 `Evaluation.Check` 的定位要纠正

任务简报写「`Evaluation.Check`（胜负/威胁评估）」——**不对**。
它是 `bool Check(object x, object y, Operation op)` 的**通用比较器**，
唯一调用点是 `Everguild.LiveOps.AttributeCondition.Validate`（LiveOps 活动条件）。
**战斗侧 0 调用**。⇒ 这一条可以从「要复刻」的清单里划掉（不是我方缺漏）。

### 3·6 `.c` 里的日志字符串已全部解出（`stringliteral.json` 按 RVA 查）

`"Ignoring perfection scoring for "` · `"Null ability for "` · `"Can't ExecuteAction for type "` ·
`"No target criteria defined"` · `"Null evaluated card"` · `"Unhandled trait to check if meets criteria "` ·
`"Ignore trap "` · `"check if quest completed "` · `"Skip AI action by maxsteps/minsteps: "` ·
`"%%%% Analyzing card … for mulligan, discard: "` · `"Trying to divide by 0! in ScoreFromDamagingUnit: …"`。

---

## §四 需要改代码的清单（9 条，可直接派）

> 格式：`文件:行 | 现在怎样 | 应改成 | 判据 | 风险`。详细表在 `判据` §8，这里是摘要。

| # | 落点 | 现在 | 应改成 | 风险 |
|---|---|---|---|---|
| **R6-01** | `RuleEngine/Data/AiScoring.cs:268`（`default` 支） | `option 7/8/11/20/30/40` 一律 `return baseValue` | 按 `判据`§1·5 逐支实现（7 Jam / 8 Random / 11 Buff / 20 Poison / 30 Charge / 40 ChangeWeapon） | **低**（池内 0 条用，行为零变化；铁律 11 仍要做） |
| **R6-02** | `Data/AiScoring.cs:195`（option 5 循环） | 没判 `t.mightAct` | 循环体首句 `if (!t.MightAct) continue;` | **低** |
| **R6-03** | `Data/SimpleAI.cs:584 ScoreFromAttack` | 没有 `sacrifice(470)` 支 | `u.Health <= 守方按档攻击力 && !Dodge && !Shield` ⇒ 非 special `+2f`，special ⇒ `+ ScoreFromCriteria(rawCard.attackScoringCriteria)` | **中** |
| **R6-04** | 同上 | 没有 `blast` / `markerlight` / `poisonous` 三条加分 | `+ CurrentBlast × 邻接数` · `+ target.CurrentMarkerlight`（仅远程）· `+ ScorePoisoning(target) × 0.7f`（仅毒且这一下打不死） | **中** |
| **R6-05** | `Data/SimpleAI.cs:741` | 小兵部分伤害 = `V·0.6·(扣甲后伤害)/MaxHealth` | 原版 = `V·0.6·(扣甲前伤害)/(maxHealth + buffHealthChange + bastion)` | **中** |
| **R6-06** | `Data/SimpleAI.cs:607` | 挨反击存活判据用「扣甲后伤害 < 血」 | 原版用**扣甲前**的 `tgtDmg` 比 | **低** |
| **R6-07** | `Data/SimpleAI.cs:698 ValueInPlay` | `2*Cost+1+…` | 漏 `rawCard.overrideDefaultValueInPlay ? manualValueInPlay : manaCost` | **低**（真值分布未查，§五-6） |
| **R6-08** | `Data/SimpleAI.cs:1294 TweakAvailableActions` | 从表尾逐条掷一次；旋钮按难度硬编码 | 原版「删一条→再往前挪一条重掷」，旋钮来自 `MatchData.GetBotConfig{Min,Max}Skips/SkipChance` | **低**（RNG 消耗数变了会影响既有回放；我方注释已承认，属「已知偏离」） |
| **R6-09** | `Data/AiTargets.cs`（整份） | — | **返回值若是新表 ⇒ 与原版「静态池」不同**；至少要在 doc 里写清「原版是池，别照抄池语义」 | **低**（语义上更安全，但属与原版不同） |
| **R6-10** | `Data/AiScoring.cs:265`（option 35） | `base + M·mult·delta` | 指令流 = `base + M·mult·(delta + base)`（`base` 进乘法一次、再在外面加一次） | **低**（两者恒差 `M·mult·base`） |

**最要紧的 3 条**：`R6-03`（sacrifice 直接改「这一刀该不该打」的分数）＞ `R6-04`（blast/markerlight/poisonous，同样是正向加分且池内真实存在）＞ `R6-05`（有护甲单位的估值口径）。
其余 7 条要么今天零行为变化（R6-01/02/07/09），要么只在小数点后 / 只在池内 0 条数据时（R6-06/08/10）。

---

## §五 没查清（⛔ 别拿猜测填空）

1. **`AI.ScoreFromHandBuff`（RVA `0x93B7A0`）的完整算式** —— cpp2il 版 30+ 条 `Unmanaged memory load` 占位、`.c` 是一长串结构体拷贝。要读指令流（约 0x7E0 字节）。**这是唯一一个「打分族里还缺一整份公式」的**。
2. **`AI.ValueOfTraits` 的遍历源** —— 只读出「Σ `CardTrait._aIScore`」和 `CardTraitCollection.GetTrait`，**遍历哪个集合读不出**。
3. **`GetTargets` 每个分支内部怎么拼候选集** —— 只到「分支 → 调了哪个 manager 方法」。
4. **`Data/AiTargets.cs`（1061 行）与原版 `GetTargets` 的逐支对账** —— 本轮只核原版侧。
5. **`CheckIfMeetsCriteria` 的 32 步顺序 vs 我方 `Core/TargetCriteriaEval.cs`** —— 没逐行比（本轮核了它的两个子件）。
6. **`rawCard.overrideDefaultValueInPlay` / `manualValueInPlay` 在池内 1,127 张里的分布** —— 没查（决定 R6-07 是不是真缺口）。
7. **`AbilityLogic.GetValueFromCriteria` 我方有没有对应实现** —— 没查。
8. `ScoreFromPoisoning` 的 `health <= 2` 阈值是**外推**（`0x18093C4E0` 指令流没读）。
9. `ScoreFromAttack` 的 `blast` / `markerlight` 两条算式是**外推**（`.c` 只留了 getter 调用，增量在 cpp2il）。

---

## §六 顺手发现（不在简报范围，调度台分流用）

1. **`ScoringCriteria.baseValueFriend/Enemy` 是【无条件】加进累加器的** ——
   `[Tooltip("baseValue is always added if targetCriteria finds at least one target")]` 这句话
   **代码里没有对应的判断**。⇒ 若以后有人「照 tooltip 加个 targets.Count>0 的闸」，那是加戏。
2. **`option 9 / 10` 会【覆盖】累加器**（不是 `+=`）—— 意味着这两档下 `baseValue` 被丢弃。
   我们 `AiScoring.cs:220/223` 已经是对的，但很容易被后来人「改成 += 」。
3. **`option 35 / 40` 的 side 系数判的是 `thisCard.side == 0`，不是「目标阵营」** ——
   这两支没有目标，所以原版就是拿「自己是不是 AI 侧」来选 `multiplyByFriend/Enemy`。
   我们 `AiScoring.cs:265` 硬取 `multiplyByFriend`（因为调用方恒为 AI 侧），今天等价，
   但注释里要把这条前提写死（已有半句）。
4. **`GetTargets` 的 `targetSelectMethod == 4 (cardTarget)` 没有专门分支**，落 `default` = **随机挑**。
   若有人在数据里填了 4，行为是随机的 —— 值得在生成器侧看一眼有没有这种数据。
5. **`MeetsConditionCriteria` 的 `${card:+0x228}`（`none` 那一格）** 与 `${card:+0x54}` 这两个字段名没查
   （`none` 是 `UnitConditions` 里唯一「没有语义」的一格，原版靠这两个字段兜底）。
6. **`BattleManager +0x378 = playerFatigue` / `+0x38C = enemyFatigue`**，
   而 `ValueOfDrawingCard` 里 `isPlayer==0` 取的是 **`enemyFatigue`** ——
   即命名是「本地人类视角」，取的是**自己那一方**的疲劳。这是个容易读反的地方，建议在
   我们 `RuleCore` 的疲劳字段上加一句同源注释。
7. **`AI.ExecuteAction` 里那一大段 0x100 字节的 `do { puVar13[..]=puVar15[..] } while` 是结构体拷贝**，
   不是循环逻辑 —— 后来人照着 `.c` 写循环会写出 32 次拷贝。
8. **`FilterMethods.CheckIfMeetsCriteria` 里 `includeWeapons(+0x1A)` / `includeHeroParts(+0x1C)` 全文 0 个读取点**
   —— 这两个字段原版是死字段。若我们实现了它们，那是加戏。
9. **`Evaluation.Operation` 枚举的 `EnumMember` 字符串与数值错位**（`">="` 挂在 4、`">"` 挂在 5）——
   凡是按 `EnumMember` 解析的地方都会反。今天我方没实现它，记下来防将来踩。

---

## §七 交接给下一个代理

**我这个会话做到哪**：`判据/AI打分与动作选择_第2轮.md` 已落盘（10 节，含 36 项跳转表、20 条公式、
34 条 `UnitConditions`、32 步 `CheckIfMeetsCriteria`、26 条 `ChooseValueOptions`、9 条改动清单）。
**没动任何代码**。

**剩什么（按优先级）**：
1. **`AI.ScoreFromHandBuff`（RVA `0x93B7A0`）读指令流** —— 唯一还缺一整份公式的。约 0x7E0 字节，
   直接 `工具/disasm_va.py` + capstone 线性反汇编即可（本轮的 `ScoreFromCriteria` 就是这么读的，
   线性反汇编在该函数上**没有跑偏**，可以照抄方法）。
2. **`Data/AiTargets.cs` 逐支对齐原版 `GetTargets`**（判据 §5·3 有 34 行的分支表）。
3. **`CheckIfMeetsCriteria` 32 步 vs `Core/TargetCriteriaEval.cs` 逐行比**（判据 §3·3 有完整表）。
4. **R6-01…R6-09 派执行代理**（R6-01/02 零行为变化，可以先做；R6-03/04 要跑一遍自检/对局看分差）。

**踩过的坑（给下一个）**：
- **`.c` 的浮点累加会整段消失**，尤其「helper 返回值被丢弃」这一种 —— 看到 `f(x);` 单独一行就要怀疑。
- **`decomp_full` 里的 helper 名不可信**（`NetworkingPeer__OnMessage` = `il2cpp_codegen_object_new`、
  `System_Collections_Generic_List<AnimatorClipInfo>__get_Item` = `List<T>.get_Item`、
  `ItemDrawerOptions___ctor` = `CardEffect..ctor`）。
- **跳转表定位套路**：`movsxd rax,[base+imm]` → `cmp eax,N` → `ja` → `lea r8,[rip-X]`（算出 ImageBase）
  → `mov edx,[r8+rax*4+TBL]` → `jmp r8+rdx`。表项是 **4 字节 RVA**（不是 VA、不是偏移）。
- **`script.json` 的 `Address` 是 RVA**，`stringliteral.json` 的 `address` 是字面量的 **RVA**，
  两者都能直接和 `.c` 里的 `DAT_xxxxxxxx`（VA）对上：`RVA = VA − 0x180000000`。
