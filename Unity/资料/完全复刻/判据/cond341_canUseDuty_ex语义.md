# cond 341 `canUseDuty` 的 `ex` 语义（B9_R1DUTY · 2026-10-27 · 只读代理产出）

> **一句话**：`ex` **不是阈值、不是索引、不是「≥N / 恰好 N」** —— 它是原版 `TargetCriteria.excludeCondition`（`+0x3D`）
> 的 **0/1 取反位**，在 `MeetsConditionCriteria` **出口处取反一次**。
> 我们这一层的实现（`ConditionEval.Meets` 统一取反）**与原版逐字同形，⛔ 不要改**；
> 但同一张表里查出**两条真偏离**：① 状态位取窄了（`DutyUsed` ≠ `usedActiveAbility`）；
> ② **`cond`/`ex` 在「原版根本不求值」的 `targetsAffected` 档上被我们套用了**（`BL64` 打不出去的真因）。
> 第一权威 = `d:/2/tools/decomp_full/`；交叉 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`；真值 = 原始 SO 快照。

---

## 一、结论

### 1.1 `ex` 是什么 —— `TargetCriteria.excludeCondition`（0/1）

| 事实 | 判据 |
|---|---|
| `ex` 不是「条件值」：`UnitConditions` 是**裸枚举**（35 个成员、无附加字段） | `d:/2/tools/il2cpp_out/dump.cs:20735-20774`（`canUseDuty = 341` 在 `:20772`） |
| `ex` 也不是「条件值」：`TargetCriteria` 的 38 格里**没有任何「条件数值」字段** —— 只有 `conditionsFilter // 0x38` 与 `excludeCondition // 0x3D` | `d:/2/tools/il2cpp_out/dump.cs:23823-23881`（`:23850` / `:23853`） |
| 生成器里的 `ex` 就是 `excludeCondition`（`0/1`，非 0/1 报 `bad`） | `d:/4/Unity/工具/gen_cards_engine.py:2599`（落值口径）· `:2880-2886`（取值 + 校验） |
| 原版求值器的形参就是它（第三参 `exclude`） | `decomp_full/FilterMethods__MeetsConditionCriteria.c:2-3` · 调用点 `FilterMethods__CheckIfMeetsCriteria.c:298-300`（传 `*(undefined1 *)(param_4 + 0x3d)`） |

⇒ **数据上根本没有位置放「阈值 / 索引」**。这一点比「读代码推断」更硬：原版没有那个字段。

### 1.2 `ex` 怎么参与判定 —— 出口处 **XOR 取反一次**

`FilterMethods__MeetsConditionCriteria.c:384-389`（全文最后 6 行）：

```c
LAB_18093443a:
  if ((param_3 != 0) && (bVar3 == 0)) { return 1; }   // ex=1 且 match=假 ⇒ 保留
  return (param_3 ^ 1) & bVar3;                       // 否则：ex=0 ⇒ match；ex=1 ⇒ !match
```

ILSpy 同形（可读版）：`out_cpp2il_dev_ilspy/FilterMethods.cs:1190-1196`
`if (excludeTrait && !flag2) goto IL_0c29 /* return true */; object obj12 = (excludeTrait ? 1 : 0) ^ 1; return (obj12 & flag2) != 0;`

**真值表（cond=341，主语 = 被筛的那个单位）**

| `ex` | 保留条件 | 白话 |
|---|---|---|
| `0` | `match == true` | **保留「主动能力还能用」的单位**（`usedActiveAbility == false`） |
| `1` | `match == false` | **保留「主动能力已经用过了」的单位**（`usedActiveAbility == true`） |

- **不是「必须 ≥ N」也不是「恰好 N」** —— 这里没有 N。
- `ex` 只在**出口取反一次**；⛔ 调用方不得再取反（我们的 `ConditionEval.Meets` 已按这条写死，`:169-175`）。

### 1.3 `ex = 0` 是什么档

`ex = 0` = **不取反**（原样）—— 与「这一格没写」**不等价**：
- `conditionsFilter == 0 (none)` = **这一格没有条件**（整行被生成器省略，缺行 = 放行）；
- `conditionsFilter == 341, excludeCondition == 0` = **有条件，方向是「能用的那些」**。

两者在消费端必须分得开（我们的 `CardDef.ConditionSpec` 走 `Condition = 0` / `Exclude = false` 两格，已经分开了）。

### 1.4 cond 341 的 match 本体 —— `!usedActiveAbility`

`FilterMethods__MeetsConditionCriteria.c:170-174`：

```c
if (param_2 == 0x155) {               // 0x155 = 341
  if (param_1 == 0) goto LAB_1809344c4;      // 原版这里直接抛（FUN_1803f47a0）
  bVar3 = *(char *)(param_1 + 0x4c) == '\0'; // +0x4C = usedActiveAbility
  goto LAB_18093443a;
}
```

ILSpy：`out_cpp2il_dev_ilspy/FilterMethods.cs:899-900` `bool flag4 = !usedActiveAbility__BackingField; flag2 = flag4;`

佐证 `+0x4C` 的语义（三条独立）：
- 字段名/偏移：`il2cpp_out/dump.cs:21979` `private bool <usedActiveAbility>k__BackingField; // 0x4C`（邻位 `:21981` `usedActiveAbilityTimesPerTurn // 0x50`）
- 读点 = `Duty` 徽标的「未激活」态：`decomp_full/CardTraitDuty__IsActive.c:19` `return *(char *)(param_2 + 0x4c) == '\0';`
- 闸点 = 「duty 单位还能不能再发动」：`decomp_full/BattleManager__CanUseActiveAbility.c:93-94`（`HasCurrentTrait(0x4c4 duty) && +0x4c != 0` ⇒ 拒）

### 1.5 我们这边的现状（三层核对，全部通过）

| 层 | 我们的实现 | 与原版 |
|---|---|---|
| ① match 本体（未取反） | `Assets/RuleEngine/Core/ConditionEval.cs:548-550` `case 341: … return !u.DutyUsed;` | 同形（`!usedActiveAbility`） |
| ② 取反（`ex`） | `ConditionEval.cs:169-175` `return exclude ? !match.Value : match.Value;` | 同形（`:384-389`） |
| ③ `ex` 有没有被传进来 | `MeetsUnit/MeetsCard` 的 `exclude` 形参；四个消费口全传了：`CardCriteria.cs:265-266`（`ExcludeCondition`）· `EffectResolver.cs:2357`（`row.Exclude`）· `TargetCriteriaEval.cs:315-316`（`cond.Exclude`）· `AiTargets.cs:634`（`c.excludeCondition`）· `CountEval.cs:508/540/624/739`（传 `false` —— **今天不可区分**：池内 6 条带 cond 的 `count` 行 `ex` 全 0；原版计数那一跳到底取不取反 **没查清**，见 §三·4） | ✅ |

⇒ **「`ex` 算错了」这个假设不成立**（第四批报告 §E-(a) 的更正方向是对的：**不要改 `ConditionEval.cs`**）。

### 1.6 但查出两条真偏离（都不是 `ex` 本身）

#### 偏离 A（决定性）：`cond`/`ex` 在**原版根本不求值**的 `targetsAffected` 档上被我们套用了

原版 `FilterMethods__CheckIfMeetsCriteria.c` 是**先按 `targetsAffected` 短路、再走 36 格**的：

| `ta` | 原版行为 | 行号 |
|---|---|---|
| `0 any` | 直接 `return 1`（其余 37 格全不看） | `:57-59` |
| `20 self` | 返回「候选 == thisCard」 | `:60-75` |
| **`30 target`** | **返回「候选 == targetCard」—— 直接 return，走不到第 ⑧ 步** | **`:76-91`** |
| `40 attacker` / `70 abilityOwner` | 返回「候选 == thisCard」 | ILSpy `FilterMethods.cs:150-153` |
| `80 actingCard` | 直接 `return true` | ILSpy `:133-136` |
| `200 / 201 / 235` | 静态表 / 天赋卡比较 | `:92-157` |
| 其余（`10 / 60 / 90 / 100 / 150 …`） | 才落到 36 格，**第 ⑧ 步**才是 `conditionsFilter`+`excludeCondition` | `:158-408`，⑧ 在 `:298-303` |

⇒ 对 `ta=30` 的 criteria，`conditionsFilter` / `excludeCondition` 是**死字段**。

我们的 `TargetCriteriaEval` 36 格那一侧**是对的**（`TargetCriteriaEval.cs:117-121` `case 30` 直接比身份），
但**另有一条只筛 cond 的旁路**不看 `ta`：`EffectResolver.ApplyStructuredConditionFilter`（`:2348-2377`）
经 `PlayTargetPool`（`:2091`）与 `ResolveTargets`（`:1703`）无条件套用 `op.TargetCondition`；
而 `op.TargetCondition` 的贴法（`EffectText.AssignAbilityIndices`，`:1698-1835`）**也没有 ta 闸**。

**实证**：池内 `role=target & cond!=0` 的行里，**18 行 / 16 张卡**落在会短路的 `ta`（0/20/30）上
（清单见 §五），其中就是 **`BL64 Murderous Desires`**：
它的 `abilityLogic[0].targetCriteria = {ta:30, conditionsFilter:341, excludeCondition:1}`
⇒ 我们拿这条**死字段**去筛出牌目标池 ⇒ 友方池被 `ex=1`（要「已用过主动能力」）筛空 ⇒ `ErrNoTargetAvailable`。

原版对 `BL64` 的「合法目标」判据其实在**卡级** `cardTargetCriteria`（`RawCardScript__IsValidTarget.c:44`
拿 `rawCardData +0x280` 调 `CheckIfMeetsCriteria`），原始 SO 真值 =
`{ta:10, friendlyUnits:1, includeMinions:1, includeHero:1, conditionsFilter:40 damaged, excludeCondition:0}`
⇒ **「一个受伤的友方单位」**（`ta=10` ⇒ 36 格真的会跑，`damaged` 真的生效）—— 与卡面
「Make a damaged friendly unit attack by itself / 使一支受伤的友方单位自行攻击」一致。

#### 偏离 B：状态位取窄了 —— `DutyUsed` ≠ `usedActiveAbility`

| 事实 | 判据 |
|---|---|
| 原版 `+0x4c` 在**任何主动能力发动**时置 1，闸只有 `hasActiveAbility(+0x220)` | `CardScript__ResolveActiveAbilityPlayed.c:23-30`（`:36` `*(undefined1 *)(param_1 + 0x4c) = 1;`）· ILSpy `CardScript.cs:14764-14769` |
| 四族共用同一条发动链（duty 1220 / oath 1275 / ferocity 1265 / agenda 1235 在**同一个方法**里分支），紧随 `ResolveActiveAbilityPlayed` 之后调用 | `CardScript__UsedActiveAbility.c:29`（duty）`:34`（0x4d3 = agenda 1235）`:41`（oath）`:50`（ferocity）· 调用点 `BattleManager._ResolvePlayActiveAbility_d__479__MoveNext.c`（ILSpy `BattleManager.cs:9428-9431`） |
| **`+0x4c` 不随回合重置**：`OnTurnEnd` 只清 `+0x48 attacksThisTurn` 与 `+0x50 usedActiveAbilityTimesPerTurn` | `CardScript__OnTurnEnd.c:209-210` |
| 清零只有三处：卡初始化 / 变形回手牌 / **`ResolveResetDuty`（装填 duty 那个动作 0x57）** | `CardScript__CardSetup.c:116-123` · `CardScript__ResetToValuesInHand.c:131` · `BattleManager__ResolveResetDuty.c:6`（ILSpy `BattleManager.cs:26520`） |
| `pray(1185)` **不置** `+0x4c`（走的是另一条 `ResolveTriggerPray`，`decomp_full/BattleManager__ResolveTriggerPray.c` 里 `+0x4c` 0 命中） | 同上 |

我们的现状：
- `DutyUsed` 只在 `keyword == Duty` 时置 1（`RuleCore.cs:11960`），**oath / ferocity / agenda 用过之后不置** ⇒ cond 341 对它们恒为「能用」；
- 另一个 `UsedActiveAbilityThisTurn`（`UnitState.cs:760`）**每回合重置**（`:1809`），却在 doc 里被写成「`+0x4c`，清零 = `CardScript__OnTurnEnd.c:209`」—— **那条注释是错的**（`:209` 清的是 `+0x50`）。
  ⇒ `UnitState.cs:748-760` 这段注释要按铁律 5 就地订正。

池内量：带替代行动关键词的**单位卡 86 张** = duty 23 · oath 18 · agenda 13 · ferocity 9 · pray 23（互斥，无叠加）；
其中会置 `+0x4c` 的 = duty+oath+agenda+ferocity = **63 张**，我们的 `DutyUsed` 只覆盖 23 张 ⇒ **40 张**在语义上漏记。

---

## 二、证据（文件:行号 / 资产路径 + 字段）

| # | 结论 | 证据 |
|---|---|---|
| 1 | `UnitConditions` 是裸枚举，`canUseDuty = 341` | `d:/2/tools/il2cpp_out/dump.cs:20735-20774`（`:20772`） |
| 2 | `TargetCriteria` 只有 `conditionsFilter // 0x38` 与 `excludeCondition // 0x3D`，**没有条件数值字段** | `il2cpp_out/dump.cs:23823-23881`（`:23850` `:23853`） |
| 3 | 求值器形参 = `(card, UnitConditions conditionsFilter, bool exclude, CardScript thisCard)` | `il2cpp_out/dump.cs:22735` · `decomp_full/FilterMethods__MeetsConditionCriteria.c:2-3` |
| 4 | `ex` 的取反在出口一次（XOR） | `decomp_full/FilterMethods__MeetsConditionCriteria.c:384-389` · ILSpy `FilterMethods.cs:1190-1196` |
| 5 | 调用点把 `+0x38`（cond）/`+0x3d`（ex）传进去 | `decomp_full/FilterMethods__CheckIfMeetsCriteria.c:298-300` |
| 6 | 341 的 match = `+0x4c == 0` | `decomp_full/FilterMethods__MeetsConditionCriteria.c:170-174` · ILSpy `FilterMethods.cs:899-900` |
| 7 | `+0x4C = usedActiveAbility` | `il2cpp_out/dump.cs:21979`（邻位 `:21981` `+0x50`）· `CardTraitDuty__IsActive.c:19` · `BattleManager__CanUseActiveAbility.c:93-94` |
| 8 | `+0x4c` 写点 = `ResolveActiveAbilityPlayed`，闸 = `hasActiveAbility(+0x220)` | `CardScript__ResolveActiveAbilityPlayed.c:23-30` · ILSpy `CardScript.cs:14764-14769` |
| 9 | 四族同一链：duty/oath/ferocity/agenda（1220/1275/1265/1235） | `CardScript__UsedActiveAbility.c:29/34/41/50` · `il2cpp_out/dump.cs:45843/45852/45854/45846` · ILSpy `BattleManager.cs:9428-9431` |
| 10 | `+0x4c` **不**随回合重置 | `CardScript__OnTurnEnd.c:209-210`（只有 `+0x48` / `+0x50`） |
| 11 | `+0x4c` 清零三处 | `CardScript__CardSetup.c:116-123` · `CardScript__ResetToValuesInHand.c:131` · `BattleManager__ResolveResetDuty.c:6` |
| 12 | `ta=30` 短路「候选 == targetCard」，**不查 cond** | `decomp_full/FilterMethods__CheckIfMeetsCriteria.c:76-91` · ILSpy `FilterMethods.cs:117-123` · 我们的同形实现 `TargetCriteriaEval.cs:117-121` |
| 13 | `ta` 短路表（0/20/30/40/70/80/200/201/235） | `CheckIfMeetsCriteria.c:57-157` · ILSpy `FilterMethods.cs:117-155` |
| 14 | 建池后逐候选过 `CheckIfMeetsCriteria` | `decomp_full/AbilityLogic__GetTargets.c:1086-1095` |
| 15 | 战术卡「合法目标」判据 = 卡级 `cardTargetCriteria`（`+0x280`） | `decomp_full/RawCardScript__IsValidTarget.c:44` |
| 16 | 4 张 cond=341 卡的**原始 SO 值**（含 `excludeCondition`） | 见 §五 表；出处 `d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`（`bundle` = `D:/2/关服前存档/新建文件夹/Everguild_Warpforge/54bdb7b1114a2de41519cac560a4bce4/ea8bca4f5c6909d3124fd4975dc96173/__data`，1,351 张 SO） |
| 17 | 我们的 `ex` 取反实现 | `Assets/RuleEngine/Core/ConditionEval.cs:169-175`（注释 `:22-23` 也写死了「只许取反一次」） |
| 18 | 我们的 341 match | `Assets/RuleEngine/Core/ConditionEval.cs:545-550` |
| 19 | 四个消费口都传了 `Exclude` | `CardCriteria.cs:265-266` · `EffectResolver.cs:2357` · `TargetCriteriaEval.cs:315-316` · `AiTargets.cs:634` |
| 20 | cond-only 旁路**不看 ta** | `EffectResolver.cs:2348-2377`（`ApplyStructuredConditionFilter`）· 调用 `:2091`（`PlayTargetPool`）`:1703`（`ResolveTargets`）· 贴法 `EffectText.cs:1698-1835` |
| 21 | 我们的 `DutyUsed` 只在 duty 置 | `RuleCore.cs:11960`（`UseAlternativeCore`）· 另两处只置 `UsedActiveAbilityThisTurn`：`:11533`（`UseAbilityCore`）`:11737`（`UseOathAbilityCore`） |
| 22 | 我们每回合重置的是 `UsedActiveAbilityThisTurn`（注释误标成 `+0x4c`） | `UnitState.cs:748-760`（doc）· `:1807-1809`（`RefreshForNewTurn`）· `:2696`（`ResetToValuesInHand`） |
| 23 | 装填 duty 只清 `DutyUsed`（原版清 `+0x4c`，不看 trait） | `EffectResolver.cs:15039-15040` · 原版 `BattleManager__ResolveResetDuty.c:6` |
| 24 | 自检把 341 钉在 `DutyUsed` 上 | `Assets/RuleEngine/Editor/RuleEngineTest.cs:37249-37253`（`u.DutyUsed = true` ⇒ 假） |

---

## 三、没查清的部分（⛔ 不猜）

1. **ferocity / agenda 是否 100% 走 `_ResolvePlayActiveAbility_d__479` 那条 action**：
   正面证据 = `UsedActiveAbility()`（紧随 `ResolveActiveAbilityPlayed` 之后）里**有这两族的分支**（`:37` / `:60`），
   且 `CardScript.cs:6678` 把 ferocity/oath 一起设 `canAct`（active 档）—— 反证「这两族不发动主动能力」。
   ⚠️ 但我**没有**逐行核它们的 action 准入点（`:46730` 那道闸只写了 duty / oath 两格）；
   要闭合需要读 `BattleActionType.playActiveAbility` 的所有准入分支。
2. **`cardTargetCriteria`（卡级）在我们引擎里的消费面**：我只核到「`role=cardtarget` 只出现在
   `CardDef` 的角色白名单 + 一条钉表自检里，没有引擎消费者」。⇒ 原版「合法目标」那一层
   （`RawCardScript__IsValidTarget`）我们是不是用**正文解析**（`DamagedOnly` 等）顶的、覆盖多少张，
   **没查清**（那是另一条账，不属本件）。
3. **装填 duty 的作用域**：原版 `ResolveResetDuty` 对**传进来的那张卡**无条件写 `+0x4c = 0`；
   我们 `DoReloadDuty` 先按 `Has(duty)` 挑候选、再只清 `DutyUsed`。⇒ 若将来把 cond 341 改读新状态位，
   这里要同步清（本报告已列进改动清单），但「原版装填是否也清 oath/ferocity 单位的 `+0x4c`」
   **取决于传进来的目标集**，我没追那个 action 的建参点。
4. **`count` 行的 `ex` 该不该取反**：我们 `CountEval.cs:508/540/624/739` 一律传 `false`；
   而原版 `BoardAnalysis__MeetsCriteria.c` 里**一次都没有** `MeetsConditionCriteria` 调用（grep 0 命中）
   —— 计数那一跳的 cond 大概率是走 `AbilityLogic.GetTargets(countCriteria)` → `CheckIfMeetsCriteria`
   的第 ⑧ 步（那样 **`ex` 会生效**）。今天池内带 cond 的 `count` 行只有 **6 条、`ex` 全 0** ⇒ 两种口径
   **不可区分**；在 `ex=1` 的 `count` 行进数据之前必须查清（本件不改它，只立账）。

---

## 四、该怎么改

> 🔴 **先划清**：`ex` 这一层**不要动**（`ConditionEval.Meets` 的取反、四个消费口传 `Exclude` 全部与反编译同形）。

### 改动 A（必修 · 决定 `BL64` 能不能打）：给 cond-only 旁路加 `ta` 闸

**改哪个方法**：`Assets/RuleEngine/Core/EffectResolver.cs` `ApplyTargetFilters(... structuredCondition)`（`:2159`）
—— 在调 `ApplyStructuredConditionFilter`（`:2348`）之前加一句：

```
// 原版 FilterMethods__CheckIfMeetsCriteria.c:57-157 先按 targetsAffected 短路，
// 0/20/30/40/70/80/200/201/235 这几档根本走不到 :298-303 的第 ⑧ 步 ⇒ cond/ex 是死字段。
// ta 从 op.TargetCriteriaSet（v25 的 36 格）取；取不到就不闸（＝维持现状 + 出声，⛔ 不猜）。
```

- 判据源：`TargetCriteriaEval.cs` 已有的 `set.TargetsAffected`（`CardDef.CountSetSpec.Unknown = -1` 表示没落）。
- 短路集合常量建议与 `TargetCriteriaEval` 那几档**共用一份**（两处写死两条规则 = 迟早不一致）。
- ⛔ 别只修 `BL64` 一张卡（那是按卡打补丁）；池内 **18 行 / 16 张卡**同病（§五）。

### 改动 B（必修 · 复刻 `+0x4c`，影响 AI 打分）：补一位「本局用过主动能力」

**不要改 `DutyUsed` 的语义**（它还兼着 `duty` 那道闸：`RuleCore.cs:11895` `CanUseAlternative`）——
新增一位（⛔ 按纪律**追加到类尾**，别插在成员之间，否则 `check_ledger_points` 会整片漂）：

| 位置 | 做什么 |
|---|---|
| `Assets/RuleEngine/Core/UnitState.cs` **类尾** | `public bool ActiveAbilityUsed;`（doc 写清 = 原版 `EntityScript.usedActiveAbility(+0x4C)`，**本局一次、不随回合重置**，清除点 = 装填 duty / 变形回手牌） |
| `RuleCore.cs:11533`（`UseAbilityCore`） | 置 `u.ActiveAbilityUsed = true;`（现在只置了 `UsedActiveAbilityThisTurn`） |
| `RuleCore.cs:11737`（`UseOathAbilityCore`） | 同上 |
| `RuleCore.cs:11960-11963`（`UseAlternativeCore`） | `keyword` ∈ {duty, ferocity, agenda} 时置；**`pray` 不置**（`ResolveTriggerPray` 那条链不写 `+0x4c`） |
| `EffectResolver.cs:15039-15040`（装填 duty） | 与 `DutyUsed = false` **同一行附近**清 `ActiveAbilityUsed = false`（原版 `ResolveResetDuty.c:6` 就是 `+0x4c = 0`） |
| `UnitState.cs:2696` 附近（`ResetToValuesInHand`） | 与 `UsedActiveAbilityThisTurn = false` 一起清（原版 `:11395` 两格一起清） |
| `UnitState.cs:1807-1809`（`RefreshForNewTurn`） | **不要**清这一位（原版 `OnTurnEnd` 不清 `+0x4c`） |
| `ConditionEval.cs:548-550` | `return !u.ActiveAbilityUsed;`（**未取反**的 match，取反仍由 `Meets` 做） |
| `UnitState.cs:748-760` doc | 就地订正那两句错注释（`+0x4c` **不**是每回合重置；`OnTurnEnd.c:209` 清的是 `+0x50`） |

### 改完哪些读数会变

| 卡 | `ex` | 变了什么 |
|---|---|---|
| `BL64` | 1 | **改动 A**：出牌目标池不再被死字段筛空（有受伤友方就能打）⇒ `playRc` 由 `18 ErrNoTargetAvailable` 变 `0`；`play all dump` 该卡一行变化 |
| `AM47` / `3a52…（Cadian Honour）` | 1 | **改动 B**：AI 打分把 oath/agenda/ferocity 单位也计入「已用过主动能力」⇒ 这两张卡的 AI 估值变化（只在场上真有这类单位时） |
| `UM42` | 0 | **改动 B**：oath/agenda/ferocity 单位用过能力后不再被当成「还能用 duty」⇒ AI 目标评分变化 |
| 其余 1,124 张 | — | 不动 |

### 不该做的

- ⛔ 不要为 `BL64` 把 `ex` 读成 0（那是「按卡面直觉改数据/改判据」，与原版反编译不符）；
- ⛔ 不要把 `ex` 当阈值去和「用过几次」比（原版没有那个字段，`+0x50` 只是每回合计数、且 341 不读它）；
- ⛔ 不要在 `ConditionEval` 里再取反一次（`Meets` 已经取反）。

---

## 五、影响面（含可复现脚本）

### 5.1 池内 cond=341 —— **4 张卡 / 4 行**（`ex` 非 0 的 3 张）

| 卡 | 名称 | 生成器 role | `ex` | 对应的原版 criteria | 原始 SO 核对 |
|---|---|---|---|---|---|
| `3a52026b99bb56f40aa9dc130fc66f34` | Cadian Honour 卡迪亚荣誉 | `nontarget`（AI 打分） | **1** | `nonTargetScoringCriteria[0].targetCriteria {ta:10, cond:341, exclude:1}` | ✅ 一致 |
| `AM47` | Press the Attack 发起攻击 | `nontarget`（AI 打分） | **1** | `nonTargetScoringCriteria[0].targetCriteria {ta:10, cond:341, exclude:1}` | ✅ 一致 |
| `BL64` | Murderous Desires 杀意滔天 | `target`（出牌目标池） | **1** | `cardAbilities[0].abilityLogic[0].targetCriteria {ta:30, cond:341, exclude:1}` | ✅ 一致（**ta=30 ⇒ 死字段**） |
| `UM42` | Forward Deployment 前线部署 | `targetscore`（AI 打分） | **0** | `cardTargetScoringCriteria[0].targetCriteria {ta:90, cond:341}` | ✅ 一致 |

> ⚠️ `role = nontarget` / `targetscore` 这两族在 `cards_engine.json` 里**没有引擎消费者**
> （AI 走的是 `Assets/RuleEngine/Resources/ai_scoring.json`，里面这两张卡的 `targetCriteria` 同样写着
> `conditionsFilter:341` + `excludeCondition:true`）。⇒ 这两行只是**数据留档 + 一条钉表自检**
> （`RuleEngineTest.cs:35647-35971`），不会改行为。

### 5.2 会短路掉 cond 的 `ta`（改动 A 的作用面）—— **18 行 / 16 张卡**

`ASH30`(244) · `AM8`(40) · `AM21`(40) · **`BL50`**(40) · **`BL64`**(341, ex=1) · `EC40`(220) · `GSC46`(244) ·
`SAU71`(220) · `SAU17`(220) · `SAU11`(340) · `GOF59`(220) · `SW44`(221×2) · `TL75`(220) · `TL71`(220×2) ·
`UM15`(40) · `UM3`(220)（括号里 = cond；`ta` 见每行：30/20/0）。

### 5.3 替代行动单位（偏离 B 的作用面）—— **86 张，其中置 `+0x4c` 的 63 张**

`duty 23 · oath 18 · agenda 13 · ferocity 9 · pray 23`（互斥）。我们的 `DutyUsed` 覆盖 23 张 ⇒ **40 张漏记**。

### 5.4 可复现脚本（`python -I -X utf8`，单份口径：`cards_engine.json` 每卡一份，无 `full_raw`）

```bash
# ① 池内 cond=341 的行（4 张 / 4 行；ex 非 0 = 3 张）
python -I -X utf8 -c "
import json,io
d=json.load(io.open('d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json','r',encoding='utf-8'))
rows=[(c['id'],c['name'],r['role'],r['cond'],r.get('ex')) for c in d['cards']
      for r in (c.get('unitConditions') or []) if r.get('cond')==341]
print('cards',len(set(x[0] for x in rows)),'rows',len(rows),'ex!=0',sum(1 for x in rows if x[4]))
for r in rows: print(r)
"

# ② 哪些 cond 行落在「原版短路、cond 是死字段」的 ta 上（18 行 / 16 张）
python -I -X utf8 -c "
import json,io
d=json.load(io.open('d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json','r',encoding='utf-8'))
DEAD={0,20,30,40,70,80,200,201,235}
out=[]
for c in d['cards']:
    ta={(e.get('ab'),e.get('lg')):((e.get('criteria') or {}).get('targetsAffected')) for e in (c.get('abilityTargetCriteria') or [])}
    for r in (c.get('unitConditions') or []):
        if r.get('role')!='target' or not r.get('cond'): continue
        t=ta.get((r.get('ab'),r.get('lg')))
        if t in DEAD: out.append((c['id'],c['name'],r.get('cond'),r.get('ex'),t))
print('rows',len(out),'cards',len(set(x[0] for x in out)))
for x in out: print(x)
"

# ③ 替代行动单位（86 张；duty/oath/agenda/ferocity 会置 +0x4c = 63 张）
python -I -X utf8 -c "
import json,io,collections
d=json.load(io.open('d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json','r',encoding='utf-8'))
cnt=collections.Counter()
for c in d['cards']:
    if c.get('type') not in ('unit','hero'): continue
    ks=[k.lower() for k in (c.get('keywords') or [])]
    for w in ('duty','oath','ferocity','agenda','pray'):
        if any(x==w or x.startswith(w+':') or x.startswith(w+' ') for x in ks): cnt[w]+=1
print(dict(cnt), 'sum', sum(cnt.values()))
"
```

### 5.5 原始 SO 的复核脚本（本报告唯一一处「上游疑点」已闭合）

```bash
python -I -X utf8 -c "
import json,io
d=json.load(io.open('d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json','r',encoding='utf-8'))
for uid in ('BL64','AM47','UM42','3a52026b99bb56f40aa9dc130fc66f34'):
    c=[x for x in d['cards'] if x['uniqueId']==uid][0]
    fr=c['full_raw']
    print('=====',uid,c['cardName'])
    for ab_i,ab in enumerate(fr.get('cardAbilities') or []):
        for lg_i,lg in enumerate(ab.get('abilityLogic') or []):
            cr=lg.get('targetCriteria') or {}
            if cr.get('conditionsFilter'): print('  abilityLogic[%d][%d] ta=%s cond=%s ex=%s'%(ab_i,lg_i,cr.get('targetsAffected'),cr.get('conditionsFilter'),cr.get('excludeCondition')))
    for k in ('nonTargetScoringCriteria','cardTargetScoringCriteria'):
        for i,e in enumerate(fr.get(k) or []):
            cr=e.get('targetCriteria') or {}
            if cr.get('conditionsFilter'): print('  %s[%d] ta=%s cond=%s ex=%s'%(k,i,cr.get('targetsAffected'),cr.get('conditionsFilter'),cr.get('excludeCondition')))
    if uid=='BL64':
        ct=fr['cardTargetCriteria']
        print('  cardTargetCriteria ta=%s friendly=%s enemy=%s cond=%s ex=%s'%(ct['targetsAffected'],ct['friendlyUnits'],ct['enemyUnits'],ct['conditionsFilter'],ct['excludeCondition']))
"
```

**实测输出（2026-10-27 现读）**：
- `BL64`：`abilityLogic[0][0] ta=30 cond=341 ex=1`；`cardTargetCriteria ta=10 friendly=1 enemy=0 cond=40 ex=0`
- `AM47`：`nonTargetScoringCriteria[0] ta=10 cond=341 ex=true`
- `3a52…`：`nonTargetScoringCriteria[0] ta=10 cond=341 ex=true`
- `UM42`：`cardTargetScoringCriteria[0] ta=90 cond=341 ex=（缺键 = 0）`

⇒ 第四批报告 §E-(a) 留的那个「上游疑点（`excludeCondition` 会不会抽错）」**已闭合：没有抽错**。

---

## 六、与既有判据件的关系（避免两份说法）

- `报告/B9_E5ENG_引擎三条真偏离.md` §E-(a)：结论「`ex` 的取反与 `Case 341` 的实现都与原版逐字同形 ⇒ **不改 `ConditionEval`**」—— **本件维持该结论**，并**补两条它没查的**：
  (i) `BL64` 那条 cond 在原版是**死字段**（`ta=30`），所以「`ErrNoTargetAvailable` 是夹具缺口」**不成立**；
  (ii) `+0x4c` 的真实写点覆盖四族、且不随回合重置 ⇒ 我们 `DutyUsed` 取窄了 40 张卡，`UnitState.cs:748-760` 的注释是错的。
- `判据/T7_引擎三条_判据.md` ③（`ResolveTargets` 不挡 stealth/camouflage/ward）与本件**无交集**（那是更早的一层闸）。
- `判据/主档夹具缺口_评估.md:131-132` 那句「(a) 修 `cond=341` 的 `ex` 语义」**已按本件作废**（方向应是 ta 闸 + 状态位，不是 ex）。
