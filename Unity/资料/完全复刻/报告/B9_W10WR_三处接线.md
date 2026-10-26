# B9_W10WR —— 三处「一行接线」+ 夹具 + 追加件（残骸原地翻回）

> 执行代理 `W10WR` · 2026-10-26 第八十四会话 · 批次 9
> 白名单：`RuleEngine/Core/EffectResolver.cs` · `RuleEngine/Core/ConditionEval.cs` · `RuleEngine/Editor/RuleEngineTest.cs`
> 账补丁：`资料/完全复刻/账/_parts/B9_W10WR.tsv`（**7 行**，首行 11 列表头，`check_ledger_points.py --all-A` 过：A 1 · 通过 1 · 失败 0）

---

## ① 结论摘要

| # | 派活项 | 结果 | 一句话 |
|---|---|---|---|
| 1 | `ConditionEval.cs` `case 40` 加 `&& !EnoughPendingHealingToFull` | ✅ **做了** | 判据逐字核过（`FilterMethods__MeetsConditionCriteria.c:87-98`）；`play all dump` 前后**逐字节相同** |
| 2 | `EffectResolver` 手牌「弃置」那条路加 `RemoveCostEffects` | ⛔ **没有这条路** | `EffectResolver` 里根本没有「手牌弃置」的结算路；真缺口是 **BL37 的弃牌代价没做**（跨 parse 层，不是一行） |
| 3 | 「没有合法目标」那一支调 `MissActions` | ⛔ **派活判据被证伪** | 原版 `MissActions` 的唯一入口是 `chosenAbility 290`（池内 **0 张**）；接在「没有合法目标」那三支上会**方向反**（把施放者当打空者） |
| 4 | `RuleEngineTest.cs` 夹具阵营 `"Test"` → `SaimHann` | ✅ **做了** | `RuleEngineTest.cs:4191`（`AshRem` 那具路标石残骸） |
| 5 | **追加件**：`DoReanimate` 两处 `new UnitState(rem.Instance, false)` → `TransformBackFromRemnant()` | ✅ **做了**（现读**只有 1 处真代码**） | 改成**原地翻**（新原语 `ReanimateRemnantInPlace`，类尾）；顺手清 `Instance.QuestCounter`（由 `ResetToValuesInHand` ③）+ 清附魔两本账 |
| 6 | 🔴 **本笔新查出的缺陷** | ⚠️ **必须配对修** | `pendingHealing(+0x100)` 原版**治疗完成就清空**，我们不清 ⇒ `case 40` 会静默错（详见 §四） |

`L0`：`TMPDIR=/tmp/wf_W10WR bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完两轮各跑一次）。

---

## ② 逐条：判据 · 落地 · 为什么

### ②·1 `case 40`（`damaged`）—— ✅ 已接

**判据（第一权威，逐句现读）**：`decomp_full/FilterMethods__MeetsConditionCriteria.c:87-98`

```
if (param_2 == 0x28 /*40*/) {
    if (CardScript__GetCurrentDamage(param_1) < 1)       bVar3 = 0;
    else { bVar3 = CardScript__EnoughPendingHealingToFull(param_1) == '\0'; }
}
```

`GetCurrentDamage` = `currentMaxHealthProcessed − currentHealth`（`CardScript__GetCurrentDamage.c:6-27`）。
我们的 `UnitState.MaxHealth` 就是那个投影 ⇒ `Health < MaxHealth`。

**落地**（`ConditionEval.cs:233`，**零行数变化** —— 该文件被账引用到 `:608`）：

```csharp
return u.Health < u.MaxHealth && !RuleCore.EnoughPendingHealingToFull(ctx, u);
```

同处三行注释就地改写（原文那句「待处理治疗恒真」**已经不再成立**，按铁律 5 改掉，并写进它依赖的那条配对修）。

**读数**：`play all dump` 前后 **md5 相同**（`b8f713d023097114c7859e3c975e158a`）。⇒ 派活单预期的「接上会变」**在探针的可达形状上没有发生**。
原因（不是没接上）：那句只在「候选单位**待治疗累计 ≥ 当前缺口**，同时 `Health < MaxHealth`」时翻转；而探针夹具里没有任何一步是「先治满、再受伤」。⚠️ **零变化 ≠ 已验证** —— 见 §四，这条接了以后**真实对局里会错**。

### ②·2 手牌「弃置」那条路 —— ⛔ `EffectResolver` 里不存在

**证伪证据（三条，互相独立）**：

1. **卡面侧**：`cards_engine.json` 1,128 张**逐张扫 `desc`，含 `discard` 的 = 0 张** ⇒ 文本解析层**不可能**产出「弃置」这个 op。
2. **引擎侧**：`EffectResolver` 全文的手牌离手点只有三个 —— 打出战术卡（`:2498` `Hand.RemoveAt` + `:2709` `Discard.Add`）· 从手牌/牌库/墓地**上场**（`:3666`/`:3868`）；`DoChooseCard` 的 `act` 分派里**没有 `discard`**（现读 11 档：`""`/`inccost`/`hand`/`draw`/`deploy`/`decktop`/`todeck`/`return`/`shuffle`/`enemyhand`/`copies`）。「进弃牌堆」另两处在 `RuleCore`（手牌上限 / 开局），都不是原版的弃牌动作。
3. **原版数据侧**：`AddDiscardFromHand` 的入队口**有两个**（账行 130 只写了 165，**不全**）：
   - `AbilityLogic__PlayAbility.c:588-603` = `chosenAbility **160** discardFromHand` ⇒ 池内 **1 张** = **`Abaddon's Chosen`（BL37）**；
   - `:325-345` = `chosenAbility **165** discardWholeHand` ⇒ 池内 **0 张**。

**所以真缺口是**：BL37 的原版流程 = 「挑一张手牌里的 Dark Pact → **弃掉它** → 给一个友方部队 Dark Pact」；我们现在走 `choicesDefinition`（从原版指定的 4 张里挑**种类**，`ruleprobe play BL37` 日志：`从原版 choicesDefinition 指定的卡选了「Dark Pact of Excess」（任意卡）`）—— **整段弃牌没有**。
⇒ 要做它得先建「手牌弃置」这条效果路（parse 侧 + 弃置时摘费用修正 + 进弃牌堆），**不是**这里加一行。已按铁律 5·b 写进账行 850 的差异列。

**没有改动任何一行**（⛔ 没有为了「看起来接上」在某处塞一句 `RemoveCostEffects`：那会把「打出战术卡」或「手牌上限」当成弃置，与原版相反 —— 原版的 `_AddCardNotDrawnToHand` 是**直接 `SendToCemetery`**，那张牌根本没进过手牌）。

### ②·3 「没有合法目标」那支调 `MissActions` —— ⛔ 判据被证伪，**不能接**

派活单（转述 `E11RC` §四·③）说「原版在『没有合法目标』那一支入队 missAction」。**逐句核过，不成立**：

| 事实 | 出处（现读） |
|---|---|
| `BattleManager.AddMissAction` 全库**只有 1 个调用点** | `grep -rn AddMissAction` ⇒ `AbilityLogic__PlayAbility.c:2525`（`if (iVar8 != 0x122) return` / `0x122 = 290 = AbilityEffect.missAction`） |
| 动作槽 `miss(0x32=50)` 全库**只有 1 个构造点** | `for f in $(grep -rl BattleAction___ctor); …0x32…` ⇒ 只有 `BattleManager__AddMissAction.c:27` |
| 它把 `+1` 记在**目标卡**上，不是施放者 | `AddMissAction(manager, targetCard←possibleTargets, priority, this.checkIfCasterInPlay, casterCard←abilityData+0x10, block)`（`:2525-2529`）→ `ResolveMissAction(target)` → `CardScript.MissActions(target)` |
| 当前卡池 **0 条** `chosenAbility 290` | `cards_truth.json` 1,351 张 / **2,078 条 `abilityLogic`** 全扫：`290` 缺席（`max = 480`） |

⇒ 这与**账行 146（`AddMissAction`）已经判 `D`** 完全一致（「本 build 卡池没有 290 这一档 ⇒ 离线无入口」）。
⇒ **不许**把 `RuleCore.MissActions` 接到 `DoDeal:2917` / `DoTakeControl:9239` / `DoGive:11504` 那三个「没有合法目标」分支上：那三支手上的 `unit` 是**施放者**，接上去 = 「施放者打空」而原版是「**目标**打空」，**方向相反**，而且会把 `AttacksThisTurn` 加到一个原版根本不会加的单位上。
⇒ 已把账行 805 的差异列就地订正（并指向 146 的同口径 D）。

### ②·4 夹具阵营 —— ✅ 已改

`RuleEngineTest.cs:4191`：`Unit("AshRem", 1, 1, 1, "Waystone")` → `UnitFaction("AshRem", 1, 1, 1, "SaimHann", "Waystone")`。
判据 = `RuleCore.IsAeldariRemnant` 走**阵营**（`cardArmy == 30 SaimHann ∧ isRemnant`，`SupportMethods__IsAeldariRemnant.c:1-14`），而 `Unit(...)` 把阵营写死 `"Test"` ⇒ 这一格原来**碰不到灵族那条支**（同族修法：`:4105` / `:4174` / `:21141` 早已分别是 `UnitFaction`，只有这一处漏了）。
同一行尾补了注解（**单行内**，零行数变化）。

---

## ③ 追加件：`DoReanimate` 改成**原地翻回**

**改动**：`EffectResolver.cs:15315`

```csharp
var back = ReanimateRemnantInPlace(rem);       // 改前：new UnitState(rem.Instance, false)
```

新原语加在**类尾**（`EffectResolver.cs:16374`，`EffectResolver.cs` 是 `partial class RuleCore`，与 `RuleCore.cs` 同一个类）—— **零行号漂移**（该文件账落点最深到 `:16331`，全在插入点之前）。

**判据（🥇 全量反编译，逐句）**：

- `CardScript__TransformFromRemnant.c:1-19`：三句 —— `*(undefined2*)(+0x65) = 0`（**两个字节一次写**）· 换模型（表现层）· `UpdateHealthText`；**方法体里不恢复任何属性**。
- 完成回调 `BattleManager.__c__DisplayClass555_0___ResolveTransformFromRemnant_b__0.c:7-31`：**第一句 `ResetToValuesInHand(true)`** → 广播 Reanimated → `SetAsUnitInPlay` → `ResetSummonSickness` → `ActivateTraitsOnSummonOrEnchantment` → `UpdateWhileInPlay` → `CheckStun`。
- `CardScript__ResetToValuesInHand.c:45-70`：`activeEffects.Clear()` ＋ `activeEnchantments.Clear()` —— **两次 `List.Clear()`** ⇒ 翻回**清掉 buff / 附魔 / 标记**，只保留没被它碰的 `companionCounter(+0x60)` / `tideCounter(+0x5C)` / `orderInPlay(+0x44)` / `isPraying(+0x67)`。

**因此本次改动关掉账行 938 的两条旧差异**：
- ② `Instance.QuestCounter = 0` —— 现由 `ResetToValuesInHand` 的 ③ 做掉；
- ③ 附魔两本账（`CardInstance.ActiveEffects` / `ActiveEnchantments`，`ResetToValuesInHand` 自己够不到）—— 由 `ReanimateRemnantInPlace` 按 `:45-70` 那两次 `List.Clear()` 清掉。

**保持不动的三句**（引擎侧口径，不是原版复位的一部分）：`back.DeployedTurn = ctx.Turn` · `back.PlayOrder = ctx.TakePlayOrder()` · `RuleCore.ApplyDeployTurnState(back)`。
🔎 顺带核过：`ResetToValuesInHand` ⑤ 会把 `AttacksThisTurn` 归零 ⇒ `ApplyDeployTurnState → RecomputeActAndAttack` 里那道 `+0x48` 闸**照旧恒假**（`RuleCore.cs:3675-3677` 那段注释的**结论仍成立**，但它括号里「`UnitState` 都是新造的」这个**理由**对翻回这一入口已不精确 —— 留给 `RuleCore` 所有者顺手改字，本笔不动别人的文件）。

**一条与改前不同的、且更接近原版的行为**（如实记）：旧代码 `new UnitState(...)` 会在构造里重算 `CurrentAttackType = ChooseAttackTypeAutomatically(this)`；原地翻**不重算** —— 而原版 `ResetToValuesInHand`（①–⑩ 逐句）**也没碰** `currentAttackType(+0x120)` ⇒ 「保持原值」才是原版。影响面：只有当单位在被 buff 改过攻/远值的那一刻定过攻击型时才能看出差别（`SimpleAI`/`CounterAttack` 读它）。

**读数**：`play all dump` 前后逐字节相同；`play SAU41`（唯一的 `Reanimate` 卡）依旧「场上没有残骸、墓地里也没有单位 ⇒ 这条没生效」（**探针夹具里没有残骸**）⇒ **本件的真验点是 `RuleEngineTest.Run`**：`RuleEngineTest.cs:5002`（`★ 再从残骸翻回来` ＋ `★ 翻回来也没有换牌` 两条，正是 `DoReanimate` 场上那一支）。⛔ 我没跑 Unity（红线），这条留给同步点。

---

## ④ 🔴 本笔新查出的缺陷（**必须配对修**）：`pendingHealing` 该清没清

**发现路径**：接 `case 40` 时去核它依赖的那张表，发现账行 841 / `RuleCore.cs:14484-14496` 的注释与 `B9_C2` 的复核都写着「**全树无 Clear/reset ⇒ 粘性累计预测表**」。

**不成立。**（铁律 2 口径：两份都要打开看）

| 事实 | 出处 |
|---|---|
| `+0x100` **就是** `pendingHealing` | `EntityScript__get_pendingHealing.c`（`return *(undefined8 *)(param_1 + 0x100);`）· `__set_pendingHealing.c` |
| 它在**治疗协程收尾被清空** | `CardScript._Heal_d__468__MoveNext.c:73-81`：状态 1（`CanContinueAnim` 为真 → 治疗圈节点 `SetActive(false)` 之后、`UpdateHealthText` 之前）⇒ `lVar11 = *(longlong*)(lVar2 + 0x100) … count = 0; Array.Clear(...)`，即 **`pendingHealing.Clear()`** |

⇒ 原版这张表是「**在途治疗**」的瞬时窗口（`HealOneCharacter/HealMultipleCharacters` 先 `RecordPendingHealing` 再排队，协程跑完就清），**不是历史累计**。
`B9_C2` 之所以读成「无清空」，是因为它只 grep 了 `RecordPendingHealing` 本身 —— 清空在**另一个方法（协程完成拍）**里。

**我们的偏差**：`RuleCore.HealUnit`（`RuleCore.cs:9181`，**6 个调用点的唯一收口**：`DoHeal` 两条 / `ApplyGain` 一条 / 再生 / 猎杀标记 / `give heal`）在真正加血**之前**记一笔，然后**从不清**。
⇒ 表变成「这个实例历史上被请求治过多少」。

**为什么这条现在会咬人**（接上 `case 40` 之后）：某单位 5 血上限、曾被治过 3 点（累计 3），后来被打到 3/5（缺口 2）⇒ `3 ≥ 2` ⇒ 被判定 **`damaged` = 假**。
后果（池内 25 行 `conditionsFilter = 40`，现读）举例：`BL50 Execution`「Destroy a damaged enemy troop」点不到它 · `AM8 Cadian Medic`「Heal 1 to a random friendly unit」**拒绝治它** · `UM116 No Mercy` 的 `Oath 4` 扫不到它 · `TAU75` / `TL68` / `GOF74` 的「for each damaged enemy」少数一个。

**配对修（3 行，须由 `RuleCore.cs` 的所有者落地 —— 不在本笔白名单）**：`HealUnit` 的 `return healed;` 之前（`RuleCore.cs:9217`）加

```csharp
            // 🔴 原版 `CardScript._Heal_d__468__MoveNext.c:73-81`：治疗协程收尾即 `pendingHealing.Clear()`。
            //    我们的同步引擎没有「排队 → 演出完成」两拍 ⇒ 加完血就地清（否则账行 738 的 `damaged` 会错）。
            ClearPendingHealingEntry(ctx, u);
```

并在**本文件（`RuleCore.cs`）**的类尾补（`PendingHealingAux` 是同类私有，`EffectResolver.cs` 也能调，但这条口径应在**一处**）：

```csharp
        static void ClearPendingHealingEntry(BattleContext ctx, UnitState u)
        {
            if (ctx == null || u == null) return;
            PendingHealingAux aux;
            if (!PendingHealingByCtx.TryGetValue(ctx, out aux)) return;
            aux.SumByInstance.Remove(u.InstanceId);
            aux.CountByInstance.Remove(u.InstanceId);
        }
```

⛔ **别改成「每回合清空」**（原版是每次治疗完成就清，粒度不同）。⛔ 也**别**在 `RecordPendingHealing` 里清（那会让表永远是空的、`EnoughPendingHealingToFull` 恒假 —— 虽然对我们这个同步引擎的**可观测结果**几乎等价，但那是把机制删掉，不是复刻）。
📌 我已把「依赖这条配对修」写进 `ConditionEval.cs:230` 的注释 + 账行 738/781/841。

---

## ⑤ 读数与指纹归因

### 5·1 `L0` 类型检查

`TMPDIR=/tmp/wf_W10WR bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改动后跑两次，中间另有一次 0/0）。

### 5·2 解析指纹（`ruleprobe.sh check`）

| 时点 | 当次现算 | 钉住 |
|---|---|---|
| 我改之前 | `c9a647982fad19bb65896011c281c93e` | `18439d965d14288fe28cfbe5986dc445` |
| 我改之后 | `c9a647982fad19bb65896011c281c93e` | 同上 |

⇒ **逐字节不变，不是我的**。现状那 1 行解析差异 = **`BL47|Disgustingly Resilient`**，归 **`E10TB`**（派活包里已点名「本批会有别路 E10TB 修 BL47」）。⛔ 我没动 `PlayProbe.cs` 的 `PinnedPlayMd5`，也没动 `PinnedParseMd5`。
哨兵合计：**失败 1 条**（就是这条「输出指纹」，同样是 `E10TB` 的未确认改动，不是我的）。

### 5·3 结算层（`ruleprobe.sh play check` / `play all dump`）

| 读数 | 我改之前 | 我改之后 |
|---|---|---|
| `play all dump`（1,129 行） | `b8f713d023097114c7859e3c975e158a` | **`b8f713d023097114c7859e3c975e158a`（逐字节相同）** |
| 结算指纹 | `1b40f42ab010e623e908fcd4a236205f` | **`1b40f42a…`（不变）** |
| 主档差异 | 1 张（BL47） | 1 张（BL47，**同一条**） |
| 电池差异 | 3 张（BL18 / SW25 / UM18） | 3 张（**同三条**） |

**逐条归因**（沿用 `E11RC` 的 A/B 隔离结论，本笔**没有**新增任何差异行）：

| 卡 | 差异 | 归谁 |
|---|---|---|
| **BL47** | 主档：`other 0 → 1`、`✎ 改动 P0[3] FIX_A 2/0/0/2 → 2/0/1/2 {armour1}` | **`E10TB`**（修 BL47） |
| **BL18** | 电池：`netFull` 一串（净差 +4 量级） | **P-06**（`669 BroadcastWhileInPlay` 挂 `Aura.RefreshInPlayNumericAndKeywords`） |
| **UM18** | 电池：`raw 46→50` / `net 32→36` | **P-06**（同上） |
| **SW25** | 电池：`net 26→27`、`{pack1, waystone1} → {pack1}` | **P-05**（`UnitState` 的残骸闸） |

⇒ **本笔对全池结算零影响**（三处改动：`case 40` 的翻转条件在夹具里不可达 · 弃置/分流两项没改 · 原地翻回在夹具里不可达）。

### 5·4 账落点机检（`check_ledger_points.py`，只读）

主账现读：**A 3579 条 · 通过 2908（告警 128）· 漂移 660 · 失败 11** —— **全是既存状态，不是本笔**：
本笔三处改动**行号零变化**（`ConditionEval.cs` 669 行不变 · `case 40` 仍在 `:231`；`RuleEngineTest.cs` 单行替换；`EffectResolver.cs` 的类尾追加在**最后一条账落点（`:16331`）之后**，追加起点 `:16343`）。
本笔自己的补丁 `账/_parts/B9_W10WR.tsv`：`check_ledger_points.py … --all-A` ⇒ **A 1 · 通过 1 · 漂移 0 · 失败 0**。
（`EffectResolver.cs` 现读有一条既存漂移 `16149 → 16225`，**+76 行来自别人在本笔之前对该文件的改动**，不在我的插入点之前/之后的影响范围内。）

### 5·5 覆盖面（如实标）

- `case 40`：**无可达夹具**（没有「先治满再受伤」的形状）⇒ 「零变化」不能当「接对了」的证据，只能当「没弄坏别的」。
- 残骸原地翻回：**无可达夹具**（探针没有摆残骸的 preset；`play SAU41` 走的是「没残骸」那一支）⇒ 真验点 = `RuleEngineTest.Run` 的 `:5002`。
- 两项的**行为**都还没被任何自动读数验过 —— 同步点跑 `RuleEngineTest.Run` 时请盯 `:5002`（翻回）与自检里 `conditionsFilter=40` 那几条（若有）。

---

## ⑥ 顺手发现（都没顺手改，按铁律 13·4⑧ 交调度台分流）

1. **`AddDiscardFromHand` 的入队口有两个，账行 130 只写了 165**（`PlayAbility.c:588-603` 是 `160`）。建议账行 130 补一句；`160` 池内 1 张（BL37）。
2. **BL37（`Abaddon's Chosen`）的弃牌代价没做** —— 现走 `choicesDefinition` 只挑种类、不弃牌（`play BL37` 日志可证）。这是一条**新账**（要做、有判据：`PlayAbility.c:588-603` + `_ResolveDiscardFromHand_d__544__MoveNext.c`），但跨 parse 层，本笔做不了。
3. **`RuleCore.cs:3675-3677` 那句「`UnitState` 都是新造的」**对「残骸翻回」这个入口已不精确（结论仍成立：`AttacksThisTurn` 被 ⑤ 归零，闸恒假）。建议 `RuleCore` 所有者顺手改字。
4. **`RuleCore.RecordPendingHealing` 的 doc / 注释 / 账行 841 都写着「永不清理」** —— 见 §四，建议与配对修一起改掉（我已把我的订正写进账补丁的 841/781）。
5. **`check_patch_conflicts.py` 不解析 `--help`**（传 `--help` 会直接跑检查；本次实测它只读、无害）。与本笔无关，仅记录。
