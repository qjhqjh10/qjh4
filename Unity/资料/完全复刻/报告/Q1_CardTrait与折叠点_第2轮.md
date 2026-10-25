# Q1 · CardTrait `_stackable` 真值 + 折叠点 + `addTrait` 实现判据（第 2 轮）

> 产出两份判据（都已在盘上）：
> · **`判据/CardTrait_stackable_真值表_第2轮.md`** —— ① 的真值表
> · **`判据/战斗逻辑驱动_Stage2前置_第2轮.md`** —— ②（折叠点）+ ③（`addTrait` 实现清单）
> 本报告只给结论 / 证据坐标 / 改动清单 / 读数 / 没查清 / 顺手发现。
> ⛔ 本轮**没改任何 `.cs`、没改任何 `账/*.tsv`、没碰正本**；只写了上面两份 + 就地订正了 `判据/战斗逻辑驱动_AddEffect分派器.md` 的两个格子（见 §三）。

---

## 一、结论

### ① `CardTrait._stackable` **不是占位 `10`，是实读真值** ⇒ K2/K3 的前置**已解除**

- 真值：**75 个有 SO 的 trait 里 74 个 = `add(10)`、1 个 = `count(20)`（`markOfChaos`）**；另有 **2 个 pair 根本没有 SO**（`berzerk(360)` / `dodge(240)`，原版如此）。
- 硬证据：`工具/export_trait_collection.py --check` **现跑只读模式**，从原版 `sharedassets0.assets` 逐对象解析 → **`bytesRead == byte_size` 73/73 全通过**、`✓ 与盘上逐字节相同 (73087 B, md5 cbb9809d7fb9796527c49118b33eba27)`。
  `_stackable` 之后还跟着 float + 3 个变长 string + 一个变长数组 ⇒ **字段宽度错 4 字节就必然读飞、字节数对不上** ⇒ 这个校验足以排除「默认值/占位」。
- 枚举值另有一级独立来源：`dump.cs:21302-21308`（**带显式值**）：`replace=0 / add=10 / count=20`。
- 上一批那句「那个 10 是『未解』的占位编码」**是错的**（`判据/战斗逻辑驱动_AddEffect分派器.md:171/174`），本轮已就地订正。

### ② `meleeAttackValueOptions` → `meleeAttackChange` 的折叠 = **快照**（在 `PlayAbility` 那一刻折）

- 折叠点唯一：`AbilityLogic.PlayAbility` → **`AdjustCardEffect(ref effect, …)`**（`AbilityLogic.cs:2450-2474`；Ghidra 里 `AbilityLogic__PlayAbility.c:839 / 918 / 1188 / 1359` 四个调用点）。
- 作用对象是 **`CardEffect.GetNewInstanceOfThisEffect()` 克隆出来的新实例**（`.c:836`），折完再交给 `AddEffect`/`AddSummonUnit`。
- 存进 `activeEffects` 的记录**已经是具体数字**；所有读点（`CurrentMeleeAttack` / `CurrentRangeAttack` …）**只累加 `*Change`，从不回读 `*ValueOptions`**。
- **`240 WhileInPlayVariable` = 专为「快照会过期」存在的刷新档**：靠**反复重挂**（每次广播 → 重新 `PlayAbility` → 重新克隆 + 重新折叠），重挂时**整份替换**（键 = `enchantingCard` + `IsSameEffect`），不是叠加。重挂点 **8 处**已逐条列出（主入口 = `BattleManager.FinishResolvingAction:35318` 的 `BroadcastWhileInPlay`）。
- 🔴 **顺带解了 `E1` 搁置的那张卡**：`SAU27 Scarab Swarm` = `trigger 240` + `buffType 70` + `meleeAttackValueOptions 50 (ThisCardHealth)` ⇒ **原版每个动作边界自动重挂、自动重读当时的 Health**，**不需要我们发明刷新点**；重挂没做之前**保持现状**（只在 `FieldAttack` 读 Health）比「一次性快照」更接近原版 —— E1 的判断是对的。

### ③ K2/K3 `addTrait(368)` 的实现判据已备齐（11 步流程 + 9 条 trait 特例 + 与 `removeTrait(4)` 的对称表）

- 池内实测 **370 条**（`addTrait` **368** + `addTraitRemnant` **2**），与账上的 368 逐数吻合。
- **`_stackable` 三档落法**：`add` ⇒ `+=`（74 条的全部常规情形）· `count` ⇒ `+=1`（仅 `markOfChaos`，且新挂时强制 `traitValue=1`）· `replace` ⇒ `max`（**本 build 0 条**，零可观察量）。
- **「身上有没有」≠「值多少」**：前者由 `CardTraitData`（按 trait 去重，`AddTraitSilently` 先 `Any(同trait)` 判重）表示、与 `_stackable` 无关；后者 = `CardTraitData._traitValue + Σ addTrait.traitValue`。

---

## 二、判据（证据坐标）

| 主题 | 坐标 |
|---|---|
| `TraitStackType` 枚举 | `D:/2/tools/il2cpp_out/dump.cs:21302-21308`（显式值）· `out_cpp2il_dev_ilspy/CardTrait.cs:9-14` |
| `CardTrait` 序列化布局 | `dump.cs:21327-21346`（8 字段声明序 + 托管偏移；`_stackable` @ `0x24`） |
| 逐对象真值 | `工具/export_trait_collection.py --check`（**只读**）→ `数据/游戏数据/trait_collection.json`；77 pair / 73 SO / 2 null |
| `_stackable` 的唯一读点 | `CardScript.AddEffect` 的挂载去重循环：`CardScript.cs:17937-18092`；`.c:495-560`（`lVar8 + 0x24` 就是 `_stackable`） |
| `addTrait`（`case 3`）全流程 | `.c:460-680`；`CardScript.cs:17622-18230` |
| `removeTrait`（`case 4`） | `.c:680-698`（`RemoveActiveEffects(4,trait)` → `Add` → `RemoveBuffedTrait` → `RemoveTrait` → `blind` 清位 → 公共尾） |
| `AddTraitSilently` / `RemoveTrait` / `RemoveTraitSilently` / `RemoveBuffedTrait` / `RemoveEffect` / `RemoveActiveEffects` | `CardScript.cs:19301` / `:19361` / `:19273` / `:19215` / `:19082` / `:18455` |
| `HasCurrentTrait`（含「效果记录也算带上」） | `EntityScript.cs:3116-3219`（普通单位 = `CurrentTraits` ∪ `activeEffects` 里的 `addTrait`） |
| `GetCurrentTraitValueWithModifiers` | `EntityScript.cs:3406-3487`（`CardTraitData._traitValue + Σ addTrait.traitValue`，夹 ≥0） |
| 折叠点 | `AbilityLogic.cs:2450-2474`（`AdjustCardEffect`）· `:2483-…`（`NewBuffStat`）· `AbilityLogic__PlayAbility.c:836-840` |
| 读点只读 `Change` | `EntityScript.cs:580-637`（近战）· `:639-700`（远程，`blind⇒0`；`setX` 命中即 return） |
| 重挂链 | `CardScript.cs:16018`（`UpdateWhileInPlay`）→ `RawCardScript.cs:1239-1253`（20 + 240 两档）→ `RawCardScript.cs:1754-1814`（`TriggerAbility` → `PlayAbility`） |
| 重挂点（8 处） | `BattleManager.cs:35318`（`FinishResolvingAction`，**主入口**）· `BattleManagerSupport.cs:870-939`（广播本体）· `BattleManager.cs:29859/29946`（transform）· `:412-423`（transform from remnant）· `:11675`（summon）· `:5147`（auto summon）· `:6990/6998/7009`（exchange）· `:11513/11516`（steal）· `CardScript.cs:20117`（`ResetToValuesInHand`） |
| extrinsic 两档去重 | `CardScript.cs:17466-17560`；`.c:87-130`（`extrinsic = +0x30`、`extrinsicVariable = +0x31`）；`CardEffect.IsSameEffect`（字段表见判据 §1·5） |
| `ChooseValueOptions` 值 | `dump.cs`（`Normal 0 … ThisCardHealth 50 … ThisCardMarksOfChaos 200`） |

---

## 三、改动清单（**这是「要改什么」，不是我改的**）

> 逐条带判据 → 详见 `判据/战斗逻辑驱动_Stage2前置_第2轮.md` §2·5。**按「对战/引擎优先」排序**（用户口径：主要功能优先 = 对战、卡组编辑）。

| # | 文件 | 要改什么 | 判据 |
|---|---|---|---|
| **D1** 🔴 | `RuleEngine/Core/UnitState.cs:1093-1101` | **`_removedTraits.Clear()` 全清 ⇒ 改成 `_removedTraits.Remove(keyword)`**（只清同名）。现在的注释写「原版不带 trait 过滤、看着像 bug 但照抄」—— 那个口径来自 Ghidra **少印了第 3 个实参**；两份独立 IL 反编译都是 `(BuffType.removeTrait, newEffect.trait)`。**这是行为错，不只是注释错** | `CardScript.cs:17651` · `out_il2csharp/CardScript.cs:10855` · `CardScript__RemoveActiveEffects.c:34-40` |
| **D2** | `RuleEngine/Core/EffectResolver.cs:11482`（`give <trait> N`） | 给 `_stackable == count` 留口子：`markOfChaos` ⇒ **`+=1`**（不看 N）；其余 `add` ⇒ `+= N`。`replace` 本 build 0 条，**可以只写注释不写分支** | 判据 §2·3 |
| **D3** | 同上（加 trait 的落点） | 补 **6 条逐 trait 特例**：`poisoned×resistant` 双向 · `survivor ⇒ 摘 survivorSpent` · `tide/companion` 计数器快照 · `markerlight ⇒ BroadcastMarkerlightResolved` · `huntMark ⇒ DisplayTriggerAnim + BroadcastHuntMarkResolved`（后者要核与既有 `EmitStatusApplied` / `GetsHuntMark` 是否**同一跳、会不会双响**） | 判据 §2·2 |
| **D4** | `UnitState.AddKeyword` 或 `EffectResolver`（**只选一处**） | `poisoned` 与 `resistant` **两个方向都拦**：① 加毒时身上有抗性 ⇒ 这次不加毒（并播抗性动画）② 加抗性时身上有毒 ⇒ **整条摘掉毒** | `.c:469-` · `CardScript.cs:17631-17651` |
| **D5** | `EffectResolver.cs:11505-11520`（`lose <trait>`） | 补 ① `stealth ⇒ BroadcastUnitLoseStealth` ② **`RemoveBuffedTrait` 语义**（摘掉该 trait 的**所有非 extrinsic 授予**；我们目前是单标量，等价于「值清零」+ 逐条 `TempBuff` 撤，要确认不重复撤）③ `blind ⇒ BlindedAtStartOfTurn=false`（**已有**） | 判据 §2·4 |
| **D6** | 不实现 | `CheckPossibleCombinations`（原版每次 `AddEffect` 成功都调）**判据空**：读的是 `PlayerDataManager._traitCombinationsList`（`TraitCombinationsList` SO），**本地解包无该资产实例**（`find … -iname '*TraitCombination*'` = 0，只有 `MonoScript_3059.json` 这个脚本定义）⇒ 按铁律 11 例外① 记「原版数据本地不存在」，**不建、不写断言** | 判据 §四·1 |
| **D7** | 确认项（大概率**不用改**） | `_keywords` 是**单标量**，原版是「每条 `CardEffect` 各带时机」；**值相加的净效果相同**，但**逐条到期**要靠 `TempBuff` ⇒ 只需确认「限时授予到期时按各条自己那份减」，⛔ **不要**到期时 `RemoveAll` | 判据 §2·5 D7 |
| **D8** | 确认项（**现状正确，不用改**） | 卡**默认**带的数值（`Blast 3` 等）= `CardDef.Keywords` ← `KeywordTable.Parse`（`FirstNumber` 兜底 1）。本轮逐张对过：与 SO `cardTraits._traitValue` **0 处不同**；唯一裸词 `TAU13 Pathfinder` 的 `Companion`（SO=2）**已由 `CollectCompanionKeyword` 从 `desc` 补回 2** | 判据 §2·5 D8 |
| **D9** | 执行顺序 | **D1 + D2 是 `addTrait` 的最小可开工集**（改完即可认领 K2/K3 主体）；**D3/D4/D5 是「逐 trait 特例 + 对称性」**，同批做完再收口 | — |

### 本轮已就地改的文档（不是代码）

`判据/战斗逻辑驱动_AddEffect分派器.md` 两格（铁律 5）：
- `:171`（枚举/常量真值）—— 「10 是占位」⇒「**10 就是 `add`**；真值 = 74×add / 1×count / 2 无 SO；证据与复算命令写进去」。
- `:174`（置信度）—— 划掉「缺 `_stackable` 逐 trait 真值」。

> ⚠️ 同一文件 `:172` 的**行号引用已过期**（`UnitState.AddKeyword :954→1090` · `Has :806→942` · `Keywords :816→952`）。本轮**没动**那一格（避免与别的写手撞车），**记在判据文件的「顺手发现 #2」里**。

---

## 四、读数（全部现读）

| 项 | 数 | 怎么读的 |
|---|---|---|
| `CardTrait` SO 对象数 | **73** | `export_trait_collection.py --check`（断言 `bytesRead==byte_size` 73/73） |
| `TraitsCollection` pair 数 | **77**（含 `berzerk`/`dodge` 两个 null config） | 同上 |
| `_stackable` 分布 | **add 74 · count 1 · 无 SO 2** | 同上 + `trait_collection.json` 现读 |
| `cards_truth.json` 卡数 | **1,351** | `count` 字段 |
| 池内 `addTrait` | **368** | 整卡扫（`abilities` + `activeAbility` + `alternativeAbility`），与账吻合 |
| 池内 `addTraitRemnant` | **2**（`notDestroyRemnant`×1 · `shield`×1） | 同上 |
| `addTrait` 用到几个 trait | **28** | 判据 §2 的分布表 |
| 其中 `count` 语义的 | **1**（`markOfChaos`，7 次 / 7 张卡） | 同上 |
| 带 `traitValue > 0` 的 `addTrait` | **97 / 368** | 同上（其余 271 条 = 0） |
| `trigger 20` 下的 `addTrait` | **5**（4 张卡；Devilfish 同卡 2 条，第 2 条被幂等闸丢） | 现扫，与 `前置门与现算口径.md` §7·1 吻合 |
| `trigger 240` 下的 `addTrait` / `addTraitRemnant` | **11 / 1** | 同上 |
| `cards_engine.json` 关键词数字 vs SO `_traitValue` | **0 处不同** | 逐张对账（带数字的条目） |
| 我们侧 `TraitCombination` 命中 | **0** | `grep -rn` `Core/` `Data/` |
| 本地 `TraitCombinationsList` 资产实例 | **0**（只有 MonoScript 定义） | `find d:/2/新解包资源 -iname '*TraitCombination*'` + `grep -rl TraitCombinationsList` |

---

## 五、没查清 / 判据空

1. **`CheckPossibleCombinations` 的数据源**：`PlayerDataManager._traitCombinationsList` 是私有字段，**ILSpy 对私有 setter 会丢字段名**（`CLAUDE.md` §三 那条 2026-10-25 的坑）⇒「有没有被赋值」**没证到**；资产实例本地也不存在。⇒ 记「判据空」，**不实现**。
2. **`multiplier` 的来源语义**：只证到「`PlayAbility` 内算出的 `int`」，没逐支读 `NewBuffStat`（1,000+ 行、跳转极碎）。E1 只需要「快照 vs 活读」⇒ 不受影响；将来要接 `ValueOptions` 全表时才需要。
3. **`RandomizedOnResolution(90)` 的随机源**：`NewBuffStat` 里是**现掷**并写进快照（与「快照」不矛盾）；但掷的是不是 `manager.GetRandomInt`（可复现的 `System.Random`）**没逐行核** —— ⚠️ 若是 `UnityEngine.Random` 就撞「对局必须可复现」红线，**请 E1 顺手看一眼**。
4. **`tideCounter` / `companionCounter` 的消费语义**：原版两处快照（`CardSetup` `CardScript.cs:6581-6590`、`AddEffect` `:18152-18161`）。我们有 `CardInstance.CompanionCounter`（用途 = 伴生链剩余环数），**是不是同一件事没核**；`tideCounter` 我们这边 **0 命中**。
5. **`replace` 分支**：本 build **0 条 trait 用它** ⇒ 那一支只能读代码、无实测。**不配行为断言**。
6. **`RemoveEffect` 的方法体**（`CardScript.cs:19082`）被 Ghidra 打了 4 处 `Jump target not found` ⇒ 语义按 **IL 版**读；其中「最后一个来源消失才 `RemoveTraitSilently`」这一条**方向可信、细节未逐句核**。

---

## 六、顺手发现（不在本任务范围，但下个代理会踩）

1. 🔴 **`UnitState.AddKeyword` 的 `_removedTraits.Clear()` 是行为偏差，不只是注释错**（`UnitState.cs:1093-1101`）—— 见 §三 D1。今天可观察量可能为 0（唯一消费者 = `ConditionEval` 的 `280 siege` → `HasRemovedTrait(reload)`），但 **K2 一做，两个错会互相掩护**（一边全清、一边以为「原版就是这样」）。
2. **`战斗逻辑驱动_AddEffect分派器.md:172` 的行号过期**（1090 / 942 / 952），本轮未改。
3. **`OathAdditionalEffect` 是三个 trait 共用的一个 SO**（pathID 498，服务 `oathDouble 1277` / `oathInAllTurns 1276` / `oathTripleActivation 1278`）⇒ 任何「按 SO 建索引」的实现都会被一个 SO 覆盖三个 key，**必须用 `DefinedTrait` 当 key**。
4. **`CardTrait词条权威表.md` 把 `NotFerocity` 列成「我们完全没有 / 没有 SO」**：SO **有**（pathID 496 = `dontReturnFerocity`）。「我们代码里没有」是另一件事（`NotFerocity` 的**机制**我们做了什么，本轮没核）。
5. **`pindown` 有两个独立行为**：① `SyncKeywordState` 把攻击型改成 Ranged（已做）② **加 trait 时还会跳过一次 `UpdateFigures`**（`.c:665`）。别把 ① 当成全部。
6. **`Devilfish` 在同一条 ability 里挂了两个 `flank`**（trigger 20）⇒ 第二条被 `ContainsExtrinsicEffectFrom` 幂等闸丢掉。**原版如此**，⛔ 别实现成「挂两层」。
7. **`IsSameEffect` 不比 `trait` / `traitValue`** ⇒ 「同一张施放卡在 240 档挂两个不同 addTrait」会被判成同一条而丢第二条。**池内今天 0 例**，只记不修。
8. **Ghidra `.c` 在本仓的实参个数不可信**（同一个 `RemoveActiveEffects` 被印成 2 / 3 / 4 个实参三种形态）。判据等级要写死在简报里：**IL（`.cs`）> Ghidra（`.c`）> 数据**，冲突时先怀疑 `.c` 的实参表。

---

## 七、交接给下一个代理

**你接手时按这个顺序读（别的别读）：**
1. `判据/CardTrait_stackable_真值表_第2轮.md` §1（真值表）+ §3（`_stackable` 语义）—— 5 分钟。
2. `判据/战斗逻辑驱动_Stage2前置_第2轮.md` §2·1（11 步流程）+ §2·2（9 条 trait 特例）+ §2·4（`removeTrait` 对称）+ §2·5（改动清单 D1–D8）。

**已经确定的（⛔ 别重查）**
- `_stackable` 真值 = 74×`add` / 1×`count` / 2 无 SO（**实读**，73/73 字节数校验）。
- `ValueOptions → Change` = **快照**，折在 `PlayAbility`；`240` 靠「每次动作边界重挂 + 整份替换」刷新；**重挂点 8 处已列**。
- `SAU27` 不用发明刷新点（`trigger 240` 自动跟血走，但**只在动作边界**）。
- `RemoveActiveEffects(4, trait)` **带 trait 过滤**（IL ×2，Ghidra 少印实参）。
- `CheckPossibleCombinations` **判据空**（本地无 SO 实例）⇒ 不实现。

**马上能开工的最小集（判据齐全、文件互不相交）**
- **D1**（`UnitState.cs` 的 `_removedTraits` 只清同名）—— **独立、最小、可先做**。
- **D2 + D3 + D4 + D5**（`EffectResolver.cs` + `UnitState.cs` 同一族）⇒ **串行做，别拆给两个代理**（同一文件）。
- **D6 / D7 / D8** = 不实现 / 确认项。

**坑（本轮踩到 / 差点踩到）**
- ⛔ 别拿 `cards_raw.json` 统计（每卡两份同构树 ⇒ 翻倍）；一律 `cards_truth.json`。
- ⛔ 判「原版谁调谁、传几个实参」**以 IL 为准**；Ghidra 的 `.c` 只用来读**控制流和数值常量**。
- ⛔ `setMeleeAttack` 在 `CurrentMeleeAttack` 里**命中即 return**，会把 `addStats` 的近战加成全部吞掉 —— 这不是 bug。
- ⛔ `_stackable` 管的是**「效果记录的 `traitValue` 怎么合」**，不是「身上有几个 trait」；后者由 `CurrentTraits` 去重。
- ⚠️ 改 `.cs` 时把新成员**追加到类尾**（插中间会让整本账行号漂）。

**还没人做、但不阻塞**
- `multiplier` 的逐支语义（§五·2）· `RandomizedOnResolution` 的随机源（§五·3）· `tideCounter`/`companionCounter` 的消费语义（§五·4）。
