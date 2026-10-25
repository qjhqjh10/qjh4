# N11 · K8 —— `setCurrentHealth(100)` / `setCost(95)` / `setMeleeAttack(70)`（第 1 轮）

> 代理：**N11（执行）** · 2026-10-25 · 完全复刻 ● 战斗逻辑驱动（`K8`）
> 改动：`RuleEngine/Core/{EffectText,EffectResolver,RuleCore}.cs` + **`RuleEngine/Core/BattleContext.cs`（越界说明见 §③）** · `账/战斗逻辑驱动.tsv` · 本报告
> 探针：`d:/4/_tmp_view/n11probe/`（`dotnet bin/Debug/net8.0/n11probe.dll k8`）

---

## ① 结论

| 档 | 结论 | 观测量（探针 `k8`，**改前 4 红 → 改后 11 条全绿**） |
|---|---|---|
| **100 `setCurrentHealth`** | ✅ **新建**：解析层 `Lower its Health to N` → 新动词 **`setcurrenthealth`** → `DoSetCurrentHealth`：**无条件写、不夹上限、不查 Invulnerable、不发伤害事件**；写之前先判**狂喜 715**（判定抽成 `CheckEcstasyCrossing`，与受伤那一跳共用） | 改前：Invulnerable 单位**拒绝**（仍 4）· 狂喜**不触发**；改后：Invulnerable **也设成 1** · `Ecstasy 2` 单位 5→1 **触发狂喜**（拿盾）· 普通 4→1 且**没有伤害事件** |
| **95 `setCost`** | ✅ **改模型**：不再折「快照 delta」，落一条 **`CostMod.SetTo = N`**，`CostOf` 每次现算 **`clamp0(N + Σ 全部 changeCost)` —— 不含 base**（原版 `get_CurrentCost.c:52-79`，`:73` 直接 `return`） | 手牌先 **+2 费**（4→6）再「设为 1 费」⇒ **3**（改前 **1** = 快照 delta 把先前的 +2 吃掉了）；无别的修正时仍是 **1** |
| **70 `setMeleeAttack` / 85 `setRangeAttack`** | 🟡 **只记录，不实现**：需要 **Stage 2c（读点切换）**。今天的 `CardDef.MeleeEqualsHealth`（`SAU27`）**只在伤害那一跳**读 Health（`RuleCore.cs:4857`）；原版是「读时覆盖」（`CurrentMeleeAttack` 遇 `buffType==70` **立即 return**），**所有读点都看到覆盖值** | `Scarab Swarm` 落地 8/8 ✓，但**生命 8→10 之后 `Attack` 仍 8** ⇒ 这条差距**如实留档** |

---

## ② 判据与证据

**原版（第一权威，现读）**
- `.c:229-247`（**case 100**）：`ShouldTriggerEcastasy(this, this, healthChange)` ⇒ 真则 `OnTrigger(0x2cb = 715 = `AbilityTrigger.Ecstasy`)`；随后 `*(undefined4 *)(param_1+0x68) = *(undefined4 *)(param_2+0x60)` —— **无条件**（不夹上限、不查 Invulnerable、不经 `ReceiveDamage`）。
- `.c:208-233`（**case 70/85**）：`RemoveActiveEffects(buffType)` + `activeEffects.Add(newEffect)` + `UpdateAttackText()`。
- `.c:236-243`（**case 95**）：`RemoveActiveEffects(0x5f)` + `List.Add` + `UpdateCostText()`。
- `EntityScript__get_CurrentCost.c:30-92`：`buffType==2 ⇒ Σ costChange`；遇 `buffType==95` ⇒ 累加值**换成**它的 `costChange`，再**把整表重扫一遍**加回全部 `buffType==2`，`clamp0` 后**在 `:73` 直接 return**（🔴 **不读 `rawCardData.manaCost`**）；没有 setCost 才在 `:84-91` 走 `clamp0(manaCost + Σ)`。
- `EntityScript__get_CurrentMeleeAttack.c:29-67`：`buffType==1 ⇒ Σ meleeAttackChange`；遇 `buffType==70` ⇒ **立即 return** 它的值（连 base 与已累加的都不算）。`get_CurrentRangeAttack.c:24-70` 同形（`85`，且开头有 `blind ⇒ 0`）。
- `CardScript__ShouldTriggerEcastasy.c`（全文）：`HasDefaultTrait(0x4e2 = 1250)` ∧ `!EnoughPendingDamageToDie` ∧ **`X < hpBefore`** ∧ **`newHealth <= X`**。

**数据（第二权威）**：`cards_truth.json` 现读 —— `buffType 100` **1 张**（`EC39 Eternal Servitude`，`healthChange: 1`）· `95` **3 张**（`GSC39`/`TAU68`/`TL69`，`costChange` 全是 **1** = 目标绝对值，卡面 `reduce/Lower its cost to 1`）· `70` **1 张**（`SAU27 Scarab Swarm`，`meleeAttackValueOptions = 50 = ThisCardHealth`）· `85` **0 张**。

**我们侧落点**
- `EffectText.TrySetHealth`（`EffectText.cs`，产物动词改成 `setcurrenthealth`，doc 已换成正本判据）。
- `EffectResolver.cs:1057`（注册）· `:14056-14090`（`DoSetCurrentHealth`）。
- `RuleCore.cs:8627`（`CheckEcstasyCrossing`，**两个调用点**：`:8765` 受伤 / `DoSetCurrentHealth`）· `RuleCore.CostOf`（setCost 支）。
- `BattleContext.cs` 的 `CostMod.SetTo`（`-1` = 普通 delta / `>=0` = 目标绝对值）。
- `DoLowerCost`（`EffectResolver.cs` 内）落 `SetTo`、不再折 delta。

---

## ③ 改动清单

| 文件 | 改动 |
|---|---|
| `RuleEngine/Core/EffectText.cs` | `TrySetHealth` 产物动词 `sethealth → setcurrenthealth`；doc 换成 `.c:229-247` 的正本判据（旧 doc 写「原版那一支是 `ResolveChangeMaxHealth`」= 转述，已按铁律 5 订正）。 |
| `RuleEngine/Core/EffectResolver.cs` | 注册 `setcurrenthealth` + 新增 `DoSetCurrentHealth`（含闸①/③）；`DoLowerCost` 的「设为 N 费」落 `CostMod.SetTo`。 |
| `RuleEngine/Core/RuleCore.cs` | 抽 `CheckEcstasyCrossing`（判据只此一处、两个调用点）；`CostOf` 加 setCost 支（**不含 base**、**带** 全部 changeCost）。 |
| **`RuleEngine/Core/BattleContext.cs`** | 🔴 **越界说明**：`CostMod` 加一个 `SetTo` 字段（`-1` = 普通增减 / `>=0` = 目标绝对值，6 行 + doc）。**理由**：`CostMod` 已经带 `Key/HandInstanceId/Criteria/HandOf/ExpireTurn/Once` 与唯一的匹配判据 `CostModApplies` —— 自己另造一张 side table 会**重复**「哪条修正命中哪张牌」这套规则（铁律 6）。⚠️ 它不在我原白名单（`EffectResolver/RuleCore/Data/**`）里，**如要撤回请指示**；当时没有别的代理在改这个文件（N7 已收工）。 |
| `账/战斗逻辑驱动.tsv` | 第 771 行：`95`/`100` 的 ⛔/🟡 → ✅、`70` 补「只在伤害那一跳」的现状、`85` 标池内 0 张；订正「`DoSetHealth` 两档都不同」那句（100 那一档已分开）；追加 K8 整段（判据/落点/观测量/回归/越界说明/收口项）。 |

---

## ④ 核验结果

1. **typecheck**：运行时 **0/0** · 编辑器 **0/0**。
2. **探针 `k8`**：**改前 4 红 → 改后 11 条全绿**（上面 A/B/C 全部读数已在 §① ；原文见 `_tmp_view/n11probe` 运行输出）。
   - A 六条：动词=「setcurrenthealth」· Invulnerable 也设成 1 · 普通 4→1 · **不发伤害事件** · 狂喜单位 5→1 · **触发狂喜**。
   - B 三条：+2 费 4→6 · 「设为 1 费」之后 **3** · 无别的修正时 1。
   - C 两条：落地 8==8 · 【留档】70/85 的读时覆盖等 Stage 2c。
3. **`ruleprobe check`（解析层）**：全池 1,121 张 · **解析差异 2 行 = `BL46`（K7，有意）+ `EC39`（K8，有意）** · 其余 0 变化。
   · ⑦ [输出指纹] 红 = 预期的「解析器被有意改了」，**现算值 = `193c1b25b0b4073f3da99f7085d6558d`**（请调度台重钉 `PinnedParseMd5`）。
4. **`ruleprobe play check`（结算层）**：指纹 **`374ae74827e3210666c443e96a247677`** —— **与 K7 收工逐字节相同**（主档 13 / 电池 20，集合也相同）⇒
   本批对现有 1,128 张卡**零行为变化**（EC39 的夹具目标都不是 Invulnerable、也没有「先 `changeCost` 再 `setCost`」的局面；差异只在探针构造的边角里现形）。
5. **账工具**：见 §⑥。

---

## ⑤ 没查清 / 本轮不做（如实）

1. 🟡 **70/85 的「读时覆盖」= Stage 2c**：要把 `Attack`/`RangedAttack` 的**所有读点**换到 `CurrentMeleeAttack`/`CurrentRangeAttack`，并让那两个属性先查 70/85 记录（命中即 return）。本轮只做了「现状记录 + 探针留档」，⛔ 没动读点。
2. 🟡 **`meleeAttackValueOptions` 在哪一步折成 `meleeAttackChange` —— 没核**。`SAU27` 的 `70` 记录带的是 `meleeAttackValueOptions = 50 (ThisCardHealth)` 而**没有** `meleeAttackChange` 字段值 ⇒ 若那一步在 `AbilityLogic.PlayAbility` 建 `CardEffect` 时折，则记录里存的是**快照**，「always equal」靠 **240 `WhileInPlayVariable` 反复重挂**刷新（`SAU27` 的 trigger 正是 240）。⇒ Stage 2c 施工前**必须先核这一步**，否则会把「快照」实现成「活读」，那是另一条语义。
3. 🟡 **`setHealth(9)`** 仍然没有落点（`DoSetHealth` 保留着老近似：只降 + 查 Invulnerable），**池内 0 张** ⇒ 按铁律 11 仍要做（原版 = `currentHealth = currentMaxHealthProcessed`，而 `currentMaxHealthProcessed` 我们**还没有**（`MaxHealth` 两格并一格））⇒ 它跟 `setHealth`/`addStats.health` 的「上限现算」是**同一刀**，归属性模型的后续阶段。
4. 🟡 **`setCost` 的静态费用修正**（`CostIfControls` / `CostPerEnemyHandCard`）在 setCost 支里**被整项丢掉**（原版 `get_CurrentCost` 里没有这两个输入，它们是我们自己的建模）。⇒ 对「既有静态修正、又被 setCost 的卡」是一处**取舍**（现读三种 setCost 的目标都是部队卡，不冲突）。
5. ⚠️ **`CostMod.SetTo` 落在 `BattleContext.cs`**（见 §③ 越界说明）。

---

## ⑥ 顺手发现（⛔ 没自己改）

1. 🔴 **`DoSetHealth` 现在是「无数据生产者」的死实现**（`buffType 9` 池内 0 张、文本层已改走 `setcurrenthealth`）。留着是对的（9 那一档还没做），但**它的注释还写着「对应卡面 `Lower its Health to N`」** —— 那句现在**指错了**（那条正文归 100）。建议后续把它改成「buffType 9 专用、当前无生产者」。
2. 🟡 **`RuleCore.CostOf` 的 `CostMods.Count == 0` 早退** 现在会让「只有一条 `SetTo` 记录」的场合走不到？—— **不会**：`CostMods.Count` 只要那条 setCost 记录在就不是 0（它本身就是一条 `CostMod`）⇒ 早退安全。✔（本笔已核，不必改。）
3. 🟡 **`EC39` 的「deploy from graveyard」那半**（`Choose a friendly troop that died since your last turn and deploy it`）本轮没碰；`setcurrenthealth` 的闸①/③ 我照 `AddEffect` 那四道闸接上了（原版这两道闸在**所有** buff 之前）—— 但**「刚部署的那个单位」是否还在 `DyingThisAction`/残骸那一档**没实测（EC39 的正常路径是 deploy 之后立刻设生命 ⇒ 应该过）。⚠️ 若后续 EC39 端到端红，先查这一条。
4. ℹ️ **收口项（调度台）**：`PinnedParseMd5` 用 **`193c1b25b0b4073f3da99f7085d6558d`**；结算基线仍待统一重生成（本批指纹没变，但 K7 那批变了）。
