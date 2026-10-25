# E9 · `RuleCore`（= `Core/ActionPump.cs`）三处真偏差 —— 执行报告

> 执行代理 **E9** · 2026-10-25 · 判据来源 = `资料/普查产出_1025/J5_判据_48行与DA22.md`
> §二 F5 / F5·b / §三·4 / §七·2（⛔ 本报告不重新论证判据，只给「做了什么 + 证据 + 读数」）。
> **白名单内动过的文件**（5 个，全部 `git diff --numstat` + 二进制数过行尾）：
>
> · `Unity/MyGame/Assets/RuleEngine/Core/ActionPump.cs`：**LF 不变**；numstat 相对 HEAD 是 **+422/−1**，
> ⚠️ **含本批之前 `E2`/`E3` 已经落进去的在飞改动** —— 本代理新增的每一段判据注释都带
> 「执行代理 `E9`」标记，`grep -n "执行代理 " ActionPump.cs` 可精确定位我改的那四段。
> · `Unity/MyGame/Assets/RuleEngine/Core/TargetCriteriaEval.cs`：**LF 不变**；numstat **+27/−3**，
> 其中 +11/−1 是**本批之前 `F1` 的 `huntMark` 订正**（不是我的）；我的是 `ta=30/90` 两支 + 文件头一行。
> · `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`：**CRLF 保住**（纯 CRLF，37156→37162 行 ⇒ **+6 行**）。
> · `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineE9.cs`：**新建**，LF，387 行。
> · `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_Diag.cs`：用生成器重生成，LF，见 §五·1。
>
> ⛔ **没碰**：`RuleCore.cs` · `EffectResolver.cs` · `EffectTargets.cs` · `CardPresentation/**` · 任何正本 / 任务总表。

---

## 一 · 结论速览

| 件 | 状态 | 一句话 |
| --- | --- | --- |
| **E9-1** 第 ⑧ 格 `cond` 恒传 `null` | ✅ **已做** | 两道闸都按 `(ab, lg=-1, role)` 把 `unitConditions` 拼回那 36 格；`null` 语义仍保留（缺行 = 明确无条件 / 旧表 = 哨兵 `ConditionUnknown` ⇒ 判不了） |
| **E9-2** `ta=30/90` 合并早返 | ✅ **已做** | 拆成两支：30 = 比身份后 `return`；90 = 只挡身份、**继续走常规格** |
| **E9-3** 战术卡 `actor == null` 静默跳过 | ✅ **通道已做 + 已测**；⛔ **调用点故意没接** | `abilityHostCard` / `abilityOwnerSeat` 两个实参 + target 闸能跑 + acting 闸出声；**但给 `tactic-body` 补 `abilityTrigger` 今天会让 19 张战术/防御卡的正文整条消失**（F4 歧义，见 §四） |
| **E9-4** `FollowUpSchedulerOf` 漏 6 张 | ⛔ **未做（文件所有权冲突）** | 它在 **`EffectResolver.cs:7065`**（简报以为在独立文件里）= **`E8` 正在改的文件** ⇒ 按铁律 13·3 一个文件同一时刻只能一个写手 ⇒ 现成补丁见 §四，请调度台转交 |

**读数**（改后，与改前逐字相同 ⇒ 本批零回归）：
· `ruleprobe check` → **rc=0 · 哨兵 0 条失败 · 解析差异 0 行**
· `ruleprobe play check` → **rc=0 · ①-a 逐卡差异（空）· ①-b 事件电池差异（空）· ② 哨兵过**
· `TMPDIR=/tmp/wf_e9 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**
· 离屏自证探针（§五·2）：**51 条断言全绿**；同一批断言在「把三处修复改回去」的源码副本上 **4 条红**（§五·3）

> ⚠️ **「改了但读数不动」是预期**：今天全仓**只有 1 个调用点**传 `abilityTrigger`
> （`EffectResolver.cs:9466` = `FireBoardWhenListener`，`grep "abilityTrigger:"` 现读 = 1 处），
> 而它能走到 ⑧ 格的卡当前是 0 张（见 §二）。⇒ 本批是**把管子接好**，不是「今天就有行为差异」。

---

## 二 · E9-1 第 ⑧ 格 `cond`（原 `ActionPump.cs:662` / `:725` 恒传 `null`）

### 改了什么

1. **新增唯一拼接口** `RuleCore.AbilityLevelConditionRow(card, triggerName, role)`（`ActionPump.cs`，就在
   `ActingLevelCriteriaAllows` 之后）—— 三态照 `RuleCore` 里 `ActiveAbilityPickGate` 那个先例
   （`RuleCore.cs:10413-10426`）**逐字同口径**：
   · 有 `(ab, -1, role)` 行 ⇒ 原样返回（`Condition` + `Exclude`）；
   · 没行 + `ConditionsVersioned` ⇒ `null` = **明确没有这一格条件**（`none(0)` ⇒ 放行）；
   · 没行 + 旧表 ⇒ `Condition = ConditionUnknown(-1)` 哨兵 ⇒ `ConditionEval` 回 `null`
     ⇒ `Matches` 第 ⑧ 步「判不了」⇒ 调用点**放行 + 出声**（⛔ 不拿「未知」冒充「没有条件」）。
   **定位只用 `CardDef.UniqueAbilityIndexForTrigger`**（全仓唯一那一份，⛔ 没另写）。
2. 两道闸各自把 `null` 换成 `AbilityLevelConditionRow(card, triggerName, "acting" | "target")`：
   · acting 闸 → `role="acting"`（原版 `+0x28` 那个 `TargetCriteria` 的第 ⑧ 格）
   · target 闸 → `role="target"`（原版 `+0x30`）

### 证据

· 数据侧现读（`cards_engine.json` v28 · 池 1127 张）：`role="target"` + `lg=-1` + 所属 ability
  真有 `abilityLevelCriteria` 行 = **24 行**；同口径 `role="acting"` = **1 行**（`DA22 ab=1`）。
  已写进断言（`E9T1CondJoinCoverage`）。
· 24 行里**触发器能唯一定位**（⇒ ⑧ 今天真的走得到）的只有 4 行：
  `EC40 ab=1 (410, cond 220 ex=1)` · `GOF104 ab=2 (410, 220 ex=0)` ·
  `SW22 ab=0 (580, 220 ex=1)` · `TAU18 ab=0 (580, 220 ex=1)` —— 四条都钉了断言的逐格读数。
  **其余 20 行落在 trigger 0 的歧义卡上**（`J5` §二 F4）⇒ 闸在 ⑧ 之前按「定位不唯一 ⇒ 不触发」走掉。
· 灭自证（结构上不可能两边同时绿）：同一个候选，**传真 `cond` ⇒ `false`、传 `null` ⇒ `true`**；
  而生产的 `EnqueueAbility` 那条返回 `false` ⇒ 生产侧一旦又传 `null`，这两条会打架（第一条红）。

---

## 三 · E9-2 `ta = 90` 不再早返（`TargetCriteriaEval.cs:116-119`）

### 原版逐句（`d:/2/tools/decomp_full/FilterMethods__CheckIfMeetsCriteria.c`）

· `iVar17 == 0x1e`（= **30**，`:76-94`）：`candidate == targetCard` 的结果**直接 `return`**（满足 1 / 不满足 0）。
· `iVar17 == 0x5a`（= **90**，`:193-201`）：`candidate != targetCard ⇒ return 0`；
  **相等 ⇒ 不 return，落到 `:203` 起的常规格**（①excludeSimilar … ⑮army 全都要判）。

### 改了什么

`switch (ta)` 里拆成两支：`case 30` 保持 `return ReferenceEquals(...)`；
`case 90` = `if (!ReferenceEquals(...)) return false; break;`（`break` 落进常规档）。
文件头的判据表同步订正（原来第 26 行写的「90 ⇒ 候选 == targetCard」已被推翻，按铁律 5 就地改）。

### 断言（`E9T2Ta90VersusTa30`）

两张合成探针卡**除 `targetsAffected`（90 / 30）逐字相同**，同一张棋盘、同一对输入：
· `ta=30` + 友方候选 + 身份相符 ⇒ **true**（常规格被早返跳过）
· `ta=90` + 同样的输入 ⇒ **false**（`friendlyUnits=0 & enemyUnits=1` 真的跑了）← **改前实得 true**
· `ta=90` + 敌方候选 + 身份相符 ⇒ true；候选 ≠ targetCard ⇒ false（身份闸本身还在）
⇒ 两条方向相反，**任何单边实现都过不了**。

---

## 四 · E9-3 战术卡（`actor == null`）· E9-4 跟随调度表 —— **两处都卡在文件所有权**

### E9-3 · 已做的那一半（`ActionPump.cs`）

**原版 `acting` 是谁（逐句读）**：
`RawCardScript__OnCardPlayedWithTarget.c:40-50` 调
`TriggerAbilities(0, manager, thisCard=param_3, cardPlayed=param_4, targetCard=param_5)`；
`RawCardScript__TriggerAbilities.c:207` 再调
`CardAbility__CanTriggerAbility(ability, trigger, manager, thisCard=param_4, actingCard=param_5, targetCard=param_6, …)`
（形参表 `d:/2/tools/il2cpp_out/dump.cs:20360` / `:23463` / `:23475`）
⇒ **战术牌自己发、自己是 `thisCard`、`actingCard` 也是它自己（= `cardPlayed`）**，
`targetCard` = **玩家点选的那个目标**。

据此：
1. 新增两个实参 **`abilityHostCard`（宿主卡 `CardDef`）/ `abilityOwnerSeat`（座位）** ——
   缺省 `null` / `-1` ⇒ 单位那条路**行为逐字不变**（`hostCard = actor.Card`、座位仍现查 `FindBoardCell`）。
2. **`targetCriteria`（`+0x30`）闸在战术卡上跑得起来**：候选 = `abilityTarget`（真 `UnitState`），
   `owner = abilityOwnerSeat`，`thisCard = null`（我们没有「已打出的那张牌」的单位态，
   由 `Matches` 自己那套 `thisCard == null` 契约处置）。
3. **`actingCardCriteria`（`+0x28`）闸**：候选是**那张牌自己** ⇒ 我们**判不了**
   ⇒ 按三档契约 **放行 + 出声**（新增 `else if` 分支；改前这条 `if` 没有 `else`，**完全静默**）。
4. 座位求不出（`actor == null` 且 `abilityOwnerSeat < 0`）/ 宿主卡拿不到（`hostCard == null`）
   ⇒ 同样 **放行 + 出声**（改前是整段静默跳过）。

### 🔴 E9-3 的调用点**故意没接** —— 接上去今天会**倒退**

`EffectResolver.cs:2360` 的 `EnqueueAbility(… "tactic-body:" + card.Name, …)` **一个闸参都没传**。
按 J5 的判据本该补 `abilityTrigger: "ThisCardPlayed"` + `abilityTarget: _tacChosen` +
`abilityHostCard: card` + `abilityOwnerSeat: p`。**但现在接 = 静默掐死一批卡**：

· 现读全池：`type ∈ {tactic, defence}` 且**有 `abilityLevelCriteria` 行**的卡 **30 张**；
  其中 **trigger 0 的 ability 定位不唯一 = 19 张**
  （`dc7e575c…` `1ff4815a…` `AM75` `DA43` `b9665ead…` `EC50` `GSC46` `SAU40` `GOF104`
   `64a657fc…` `b4d6d186…` `SOR39` `SW43` `SW53` `SW42` `TAU58` `4cdc1b5f…` `EC60` `SAU59`）。
· 我们的口径是 **「定位不唯一 ⇒ 不触发」**（`ActionPump.cs` 三档里那一档）
  ⇒ 这 19 张的 `tactic-body` 会被**整条取消**（只有一条 warning）。
· J5 §二 F4 早就点过这件事（「按 E5 的方案直接给 `tactic-body` 那一跳补 `abilityTrigger`，
  这 32 行对应的卡会整条效果静默消失」）—— **今天仍然成立，且我复算到 19 张卡**。

⇒ **接的条件 = F4 口径裁定**（甲：多 ability 版 `ABILITY…` 取 OR；乙：按 `chosenAbility` 家族键逐 op 判）。
⛔ 在这之前**不要**给 `tactic-body` 补那四个实参。通道已经建好、也用**定位唯一**的探针测过了，
裁定落地后就是「加 4 个具名实参」一行事。

### E9-4 · `FollowUpSchedulerOf`（**未动，文件所有权冲突**）

· 落点 = **`Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs:7065-7086`**（简报写的「跟随调度那份文件
  自己 grep」—— grep 出来它**没有独立文件**，就在 `EffectResolver.cs` 里）。
· 而 `EffectResolver.cs` 是**简报的黑名单**（「代理 E8 在改」）⇒ 按铁律 13·3①「一个文件同一时刻只有一个写手」
  **我没有碰它**（改了会静默盖掉在飞的活，且无人审查）。
· 另外 `FollowUpSchedulerOf` 是 **`internal static`**，全仓**没有 `InternalsVisibleTo`**
  ⇒ 编辑器测试程序集**调不到它** ⇒ 也无法用断言「先把它钉红」。

**现成全池对账（可复算，⛔ 不是猜）**：用 `资料/普查产出_1018/allcards_1350/cards_raw.json` 的
`cardAbilities[].abilityLogic[].chosenAbility ∈ {320, 325}` 与当前 1127 池取交集 = **25 张**；
现有硬编码表 **19 条**；**缺的正好是 J5 说的那 6 张**，逐张开出的正确值：

| 卡 | id | 320/325 挂在 | 应 return |
| --- | --- | --- | --- |
| Embrace the Pain | `EC40` | `ab0 trigger 0` | `"ThisCardPlayed"` |
| Beautiful Death | `EC42` | `ab0 trigger 0` | `"ThisCardPlayed"` |
| Cloud of Smoke | `GOF31` | `ab0 trigger 0` | `"ThisCardPlayed"` |
| Hunta Rig | `GOF93` | `ab0 trigger 0` | `"ThisCardPlayed"` |
| Mucolid Spore | `TL3` | `ab0 trigger 580` | `"Strike"` |
| Angels of Death | `UM64` | `ab1 trigger 145` | `"Codex"` |

**现成补丁**（交给 `EffectResolver.cs` 的当前写手 `E8`、或调度台在 E8 收工后落地）：

```csharp
                // ---- 战术 / 防御：320 挂在 trigger 0（= `ThisCardPlayed`）的 ability 里 ----
                case "ASH73": case "AM54": case "AM58": case "EC47": case "EC49":
                case "SAU61": case "GOF104": case "GOF102":
                case "EC40": case "EC42": case "GOF31": case "GOF93":      // 🆕 E9：漏的 4 张（trigger 0）
                case "3ebb159036778d14ebd1c48451aa6ca0":   // Greatest Warboss
                case "SW56": case "TAU40": case "UM39":
                    return "ThisCardPlayed";

                // ---- 单位：320 挂在各自那条能力里（trigger 号见每条注释）----
                case "ASH81": return "OtherCardSpiritStone";   // 455
                case "AM8":   case "AM21": return "Regiment";  // 301
                case "GSC14": return TN_OtherCardPlayed;       // 10
                case "GSC17": return "Strike";                 // 580
                case "SAU11": return "Artifice";               // 560
                case "GOF59": return "Mob";                    // 300
                case "TL3":   return "Strike";                 // 580  🆕 E9：漏
                case "UM64":  return "Codex";                  // 145  🆕 E9：漏
                default: return null;
```

⚠️ 落这段时**同时把 `FollowUpSchedulerOf` 的 doc 里「19 张」改成「25 张」，并说明 `SW9` 那 2 行
仍按「原版本身不触发」不收**（铁律 5 / 6：数字只留一处且必须等于现状）。

---

## 五 · 自证（不跑 Unity）

### 1 · `RuleEngineTest_Diag.cs` 重生成 —— 它原来是**过期的**

`PYTHONIOENCODING=utf-8 python -I -X utf8 Unity/工具/gen_diag_runner.py` ⇒
「`Run()` 用例表 **340** 条 · 用例方法（口径）340 个」。
⚠️ 重生成**顺带补进了本会话其它代理已加进 `Run()` 但没重生成过的 8 条**
（`TestA2EngineFive` `TestA3EngineAtTurnCriteria` `TestE2AbilityLevelCriteria` `TestE3AbilityActingCriteria`
`TestE4TriggerTargetAndActivePick` `TestE5SmallEngineGaps` `TestE7FourGates` `TestE6AbilityLevelGateFamilies`）
—— 那 8 条当时**已经在 `Run()` 里、却不在产物里** ⇒ `VerifyRunCaseCoverage` ② 本来就该红。
这是产物过期，不是新问题；`RuleEngineTest.cs` 我**只加了 `Step(TestEngineE9);` 一处**（CRLF 保住）。

### 2 · 离屏探针（正例）：`_tmp_view/e9probe/`（不进工程、gitignored）

把 `Core/*.cs` + `Data/CardDatabase.cs` + `ruleprobe` 的 `Stub.cs` 编成控制台程序，
**逐条复刻** `RuleEngineTest_EngineE9.cs` 的五组断言：

```text
pool = 1127
=== E9 probe: pass 51 / fail 0 ===
```

### 3 · 灭自证（负例）：`_tmp_view/e9neg/`（**源码副本**，真树一个字节没动）

在副本里把三处修复**改回原样**（`cond → null`；`ta=30/90` 合并；acting 闸回到 `actor != null` 且无 `else`）：

```text
=== E9 probe: pass 47 / fail 4 ===
  FAIL ta=90 同样的输入 ⇒ false（常规格 enemyUnits 真的跑了）
  FAIL 活着 ⇒ EnqueueAbility=false（cond 220 ex=0 不满足）
  FAIL 而且出声 1 次 (got=0 want=1)      ← ⑧ 闸这条
  FAIL 出声 1 次 (got=0 want=1)          ← ④ acting 闸那条
```
⇒ 每条修复都有**至少一条**断言会红；而且红的是**预期的那些**（`ta=30` 那条、`DA22` 那三条、
`seat=-1 ⇒ 放行` 那条在负例里仍然绿 —— 因为它们断的是别的东西，正说明断言不是互相顶替的）。

---

## 六 · 没查清 / 留给下游

1. **`ta=90` 在 `activeAbilityTargets` / logic 级那两列的行数没统计**（J5 §七·6 也留着这条）
   —— F5·b 的影响面只算了本批这 7 行。⇒ 若那边也有 `ta=90` 行，本批的拆分**同样把它们修好了**
   （`Matches` 是唯一实现），但**没有断言覆盖**。
2. **J5 的「37 行」与本报告现算的「24 行」口径不同**：J5 那个 37 = 把 `lg >= 0` 的 13 行也算进去了
   （24 + 13 = 37，已复算吻合）。本批只能消费 `lg = -1` 的那 24 行（`lg >= 0` 是 logic 级，
   走 `EffectOp.TargetCondition` 那条路，不经过这两道闸）⇒ **断言钉的是 24**，
   引用 J5 那句「37」时请按这个换算，⛔ 别把 37 当成「⑧ 一格都没接的缺口数」。
3. **战术卡 `actingCardCriteria` 的候选是「那张牌自己」**：本批只能出声 + 放行。
   要真判，得先有「已打出的那张牌的 `UnitState`（或至少 `CardDef` + 阵营 + 是否残骸）」
   这一层模型 —— 全仓今天没有，属**新模型**，不是补一个形参能解决的（已在本报告记账）。
4. **`J5` §三·1 留的时序问题**（原版 `TriggerAbilities` 是「判一条、触发一条」，而 `PlayAbility` 的伤害是
   入队语义）—— 本批没碰，仍没查清。它决定「If the target dies」那类 `cond 220 ex=0` 在第 2 条上为何能成立。
