# B9_W18AR · 光环给的 Flank/Fast 单位每次刷新被打回疲劳（`RE1`–`RE4`）

> 执行型引擎写手 `W18AR` · 2026-10-27 · 独占文件 = `RuleEngine/Core/ActionPump.cs` · `RuleEngine/Core/UnitState.cs`
> 未跑 Unity · 未动 git · 未改正本 · 未写主账表（只写 `账/_parts/B9_W18AR.tsv`）
> 输入 = `报告/D1DG_32条红定性.md` §一·A（RE1–RE4 已定性，判据链完整，本轮**未重新查一遍**，只做现读复核）

---

## ① 结论

**改了什么（两处，都在我自己的文件里）**

1. `UnitState.cs` 的两个「光环收回」口，把 `RuleCore.RecomputeActAndAttack` 换成新窄口
   `RequestActRecomputeAfterAuraChange()`：
   - `ClearAuraGrants`（`:1578`，原 `RuleCore.RecomputeActAndAttack(this)`）
   - `ClearAuraNumericAndKeywords`（`:2345`，同上）
2. `UnitState.BeginAuraRecompute/EndAuraRecompute`（`:2094-2111`）加一条**前后差**判据：
   - Begin（`:2099`）快照 `_auraExemptBefore = RuleCore.SummonSicknessExempt(this)`（= `Has(fast) || Has(flank)`，**判据只此一处**）；
   - End（`:2110`）**只有豁免真的丢了**（`_auraExemptBefore && !现在是豁免`）才 `RecomputeActAndAttack`。
3. 新增成员（**追加在类尾**，`:3081-3124`）：字段 `_auraExemptBefore`（`:3111`）+ `RequestActRecomputeAfterAuraChange()`（`:3119`），外加那一段判据说明。
4. `ActionPump.cs:143` **就地**补一句不变式（零行数变化）：本行**不动** `Exhausted` / `CannotAttackThisTurn`；⛔ 别为了躲这个回归把这一行注释回去（它是账 669 的接线）。

**为什么不新增规则（关键）**

- 这条修法**不引入任何新的裁定**，它只是把一条**已经存在的**判据（2026-10-25 加的「摘掉 `fast`/`flank` ⇒ 重算两位」）从「**中途**有没有摘掉」改成「**整趟重算前后豁免是否真的丢了**」：
  - 我们 `Auras.Recompose` / `RefreshInPlayNumericAndKeywords` 的形状是「① 整份摘掉 → ③ 原样重挂」（`Aura.cs:641-773`）。
    在 ① 里按「摘下后」的中间态重算，结算态必然停在**中间态**（因为 `AddAuraKeyword :1535` 在 ③ 重挂时**不重算**）⇒ 这就是回归本身。
  - 净零的那一趟（棋盘没变、只是把同样的关键词摘了又挂回）⇒ 两位**保持原值**，等于「这条刷新不碰 act 两位」（`D1DG` 给的修法 (b)）。
  - **真丢**（光环来源离场 / 滤镜不再命中）⇒ 前后差为「真→假」⇒ 照旧重算 ⇒ 2026-10-25 那条判据的语义**一字不变**。
- ⛔ **没有走 (a)**（让 `AddAuraKeyword` 重挂时也重算）：`RecomputeActAndAttack` 的 doc 自己写着「光环给的关键词走哪条路**还没查清**」⇒ 那会**新增规则**（把「光环新挂上侧翼」也做成「当回合就能动」），判据不足。
- ⛔ **没有走**「把 `ActionPump.cs:142` 注释回去」：那是账 669（`BL18`/`UM18`）的接线。

**副作用（如实标）**：`_recomputingAura` 为真的那一趟，**不会**再因为「光环新挂上 fast/flank」而重算两位 —— 这与**改前**（今天之前）的可观察行为**逐字相同**（改前那条重算只在「摘」的那一侧），所以**不是新差异**，只是仍然**未闭合的判据**（见 §⑤·5）。`RuleCore.RecomputeActAndAttack` 的 doc 里「光环那条路没接 / 还没查清」这句话，现在要拆成两半写（见 §⑤·3）。

---

## ② 判据链

**原版（`.c` 现读，未重查，只复核路径）**

| 事实 | 出处 |
|---|---|
| 派生位 = `+0x58 && !fast(0x28) && !flank(0x1cc)` | `decomp_full/EntityScript__get_displaySummonSickness.c:1-16`（整个方法体，本轮亲读） |
| **摘** `fast`/`flank` ⇒ 走同一跳重算 | `decomp_full/CardScript__RemoveTrait.c:66-68` → `UpdateSummonSicknessAnims` → `ActivateMinion` |
| **加** `fast`/`flank` ⇒ 同一跳 | `decomp_full/CardScript__AddEffect.c:570-572`（`case 3` 加 trait） |
| 240 刷新 = `RemoveExtrinsicEffectsFrom` + 重新 `AddEffect` | `out_cpp2il_dev_ilspy/RawCardScript.cs:1239-1253`（`FinishResolvingAction` 尾部连发 20 / 240） |
| 光环给的关键词走哪条路 | **未闭合** ⇒ 本轮刻意不据它做任何事（见 §① 的 (a) 被否掉的理由） |

**我们的落点（改后）**

| 位置 | 作用 |
|---|---|
| `UnitState.cs:1535` `AddAuraKeyword` | 重挂**不重算**（本轮不动） |
| `UnitState.cs:1556` `ClearAuraGrants`（判据段 `:1569-1577`，调用 `:1578`） | 收回侧：周期内**延后**、周期外**立即**重算 |
| `UnitState.cs:2331` `ClearAuraNumericAndKeywords`（调用 `:2345`） | 同上（窄口刷新专用） |
| `UnitState.cs:2094/2103` `BeginAuraRecompute` / `EndAuraRecompute`（`:2099` / `:2110`） | 前后差快照与结算 |
| `UnitState.cs:3119` `RequestActRecomputeAfterAuraChange` | `_recomputingAura` 判定**只此一处**（⛔ 别在调用点各写一份） |
| `ActionPump.cs:143` | `Auras.RefreshInPlayNumericAndKeywords(_ctx)`（**保留**，账 669） |
| 完整说明段 | `UnitState.cs:3081-3108`（类尾「光环重算周期内 act 两位的重算时机」） |

**「周期外仍立即重算」的那条路**（不能被这次改动带走）：`UnitState.RemoveAllAppliedEffectsForJam` → `ClearAuraGrants()`（`UnitState.cs:2247` 附近）**没有**包在 `Begin/EndAuraRecompute` 里 ⇒ `_recomputingAura == false` ⇒ 照旧**立即**重算（那是**真移除**）。

---

## ③ 我预期哪些断言转绿、为什么

**逐条（行号是现读，`RuleEngineTest.cs` 另有写手在改，会漂）**

| 断言 | 文件:行 | 改后为什么绿 |
|---|---|---|
| ★ 残骸翻回来 + 光环给的侧翼 ⇒ 那一份能行动（**RE1**） | `RuleEngineTest.cs:16142` | `DoReanimate → DeployFree` 的顺序是「`Auras.Recompose`（`:4830`）→ `ApplyDeployTurnState(unit)`（`:4845`）」。Recompose 里该单位 `_auraExemptBefore=false`、③ 挂上 flank ⇒ 按我的判据**不重算**（不碰）⇒ 紧接着 `ApplyDeployTurnState` 写 `SummonSickness=true` 再 `RecomputeActAndAttack` ⇒ `displaySummonSickness = true && !flank` = **假** ⇒ `Exhausted=false`。之后动作收尾的窄口刷新是**净零** ⇒ 不再被打回疲劳 |
| ★ 判别式：已行动过的单位没被顺手放活（**RE2**） | `:16155` | `acted` 全程「有光环 flank → 仍有光环 flank」⇒ 两次 Recompose（`T_RemKill` 那次它还没挂上；`T_Raise` 那次摘了又挂回）**净零** ⇒ 一次都不重算 ⇒ 保持夹具摆进去的 `Exhausted=true`。（改前是中途重算把它判成 `SummonSickness(假) \|\| actedOut(0)` = 假 ⇒ 被放活） |
| ★ 免费部署 + 光环给的侧翼 ⇒ 那一份能行动（**RE3**） | `:12822` | 同 RE1 的前半：`DeployFree` 的 `ApplyDeployTurnState` 把 `fd.Exhausted` 写成假；`DeployFree` 外层 `ActionScope.Dispose` 的窄口刷新净零 ⇒ 不再打回 |
| ★ 新来的那一份照样不疲劳（**RE4**） | `:12837` | 同上（`fd2` 走同一条链）；且它的判别式前提 `fd.Exhausted`（`:12838`，夹具手动置真）也**不会被顺手放活**，因为净零 |

**条数口径澄清（如实说）**：`D1DG` 定的同一根因是 **4 条**（`RE1`–`RE4`，见其 §〇「4 条『光环给的侧翼/迅捷』」与 §γ 表）。简报里写「6 条」—— 我按现读**只能指到 4 条**。若调度台手上的 6 条里还有另外 2 条，判法只有一句：
> 任何一条断言，只要它**在某次顶层动作之后**读 `Exhausted` / `CannotAttackThisTurn` / `CanAttackNow`，而被读的单位身上带**光环给的** `fast`/`flank`，就属于这一簇；反之（例如 `BT9`/`BT10`/`BT11`）是别的根因。

**为什么不可能是「夹具凑绿」**：`RE2`/`RE4` 两条判别式在**结构上**要求「重算**不碰**那个已经行动过的单位」。我这套写法是「按前后差判」，与「遍历全场重算」那种错版**不可能同时绿**（`RE2` = `acted.Exhausted` 必须为真；全场重算会把它写成假）。

---

## ④ 指纹：**没变**

`bash d:/4/Unity/工具/ruleprobe.sh play check` ⇒

```
结算指纹：当次现算 = 1b40f42ab010e623e908fcd4a236205f · 基线文件 = 1b40f42ab010e623e908fcd4a236205f（一致）
①-a ✅ 没有一张卡与基线不同（1128 张逐字节一致）
①-b ✅ 没有一张卡与基线不同（586 张逐字节一致）
② 哨兵 ✓（基线内容与钉住的一致 1b40f42a…）   rc=0
```

**为什么本该不变成「再变一次」**（简报猜它会变，实测不变，原因如下，不是「改动没生效」）：
`ruleprobe` 的 `PlayProbe.Capture` **只记 攻 / 远 / 甲 / 血 / 关键词**，**不记** `AttacksThisTurn` / `Exhausted`（判据 = `RuleCore.cs:14457-14459` 的注释，原文：「`AttacksThisTurn` / `Exhausted` 都不进 `ruleprobe` 的任何指纹」）。
本笔改的是**两位布尔 + 它们被写入的时机**，而关键词集合是「摘了又原样挂回」= **净零** ⇒ 逐卡读数天然不变。
⇒ 这也正好说明：**这 4 张光环卡的语义修复，`play check` 结构上看不见**，只能靠 `RuleEngineTest` 那 4 条断言（`RE1`–`RE4`）验。

逐卡（`AM73 Vitus Gryf` · `DA30 Ravenwing Talonmaster` · `EC8 Alluress` · `TAU31 Devilfish`）：**无一张的指纹列变化**（1128 张逐字节一致），预期之内 —— 它们给的是**关键词**，不是数值。

---

## ⑤ 顺手发现

### 5·1 残骸 `Has` 新闸（`E10TB` 今天加的）的读点盘点（`Core/`，只列不改）

闸的定义 = `UnitState.cs:970`：`if (IsRemnant && !IsLeavingRemnant) return _remnantTraits.Contains(keyword);`
⇒ 「残骸期**经 `AddKeyword`** 挂上来的」才可见（`UnitState.cs:1157` 记账）。**翻面那一刻已经 `ClearAllKeywords()`**（`RuleCore.cs:9835`）⇒ 残骸的 `_keywords` 本来是空的 ⇒ **旧读点在「残骸没有关键词」这一点上改前改后同结论**。

| 组 | 读点 | 结论 |
|---|---|---|
| ① **`CardDef.Has`（卡面关键词），与残骸闸无关**（别误判） | `CreatePool.cs:1371` · `BattleContext.cs:1544/1582` · `WhenEvent.cs:1709` · `UnitState.cs:1099/2817/3078` | 不受影响（读的是卡模板） |
| ② 已有显式残骸守卫 | `CounterAttack.cs:85/133`（`IsRemnant && !IsLeavingRemnant ⇒ 0`）· `RuleCore.cs:9820-9835`（翻面前判定） | 不受影响 |
| ③ 会拿到残骸、但改前改后**同结论**（`_keywords` 空）—— 主要读点 | `RuleCore.cs:1064 ControlsKeyword` · `1325 HasSentinelInPlay` · `2109 BeginTurnCore` · `2531/2589/2599/2685/2691/2744 EndTurnCore` · `3435-3540 HasDeployExemption/SummonSicknessExempt/ActionQuota/RecomputeActAndAttack` · `5041/5122` · `5387-5586 IsValidTarget` · `5734 CanAttackNow` · `6071` · `6309-6526 DeclareAttackCore` · `6697-7217 DeclareAttackExecute` · `7391-7585` 伤害前判 · `8461-8586 ApplyDamageCore` · `9249 HealUnit` · `9535-10177 CleanupDeaths` · `11322-11368 CanUseAbilityTarget/IsCurrentlyWarded/HasVanguardTrait` · `12391 EmitBloodThirst` · `13195 CheckCodex` · `EffectResolver.cs:2192/2203`（关键词筛选抽目标）· `2906-2907 AddSide` · `15009/15015 DoReloadDuty` · `ConditionEval.cs:215-533`（15 处条件）· `TargetCriteriaEval.cs:714/719 TraitPasses` · `CardCriteria.cs:211/252` · `BoardSlots.cs:500` | 改前 `_keywords` 也是空 ⇒ **false 与 false**，无差异 |
| ④ 🔴 **唯一真的会变的一类**（见 5·2） | `Aura.cs:900` `t.AddAuraKeyword(op.Keyword, …)` | 光环给残骸挂的关键词：`Has` 说**没有**、`KwValue` / `Armor` / `AuraKwValue` 说**有** |

### 5·2 🔴 系统性不一致：`AddAuraKeyword` 只写 `_keywords`，不进 `_remnantTraits`

- 路径：`Auras.Recompose` ③ → `ApplyAura`（`Aura.cs:881-901`）**不判 `IsRemnant`**（只有 `RemnantStay` 那条特殊形状判）⇒ 关键词直接 `AddAuraKeyword`（`:1535` 写 `_keywords` / `_auraKw`，**不写** `_remnantTraits`）。
- 今天就有能撞上的卡：**`SAU67 Damaged Plasmacyte`「Adjacent units have Regeneration 2」**（光环、关键词型）+ 相邻的 Sautekh 残骸 ⇒
  **改前** `Has("regeneration")` = **true**（读 `_keywords`，光环那本账里有）；**改后** = **false**（闸只认 `_remnantTraits`，光环那条路不记它），而 `KwValue("regeneration")` 仍 = **2**。
  - 逐条算后果（现读，**零可观察差异，但是「两本表示打架」**）：
    `RuleCore.EndTurnCore:2531` 用 `Has` 挡掉了 ⇒ 不再进 `HealUnit(KwValue=2)`。改前会进，但 1 血 / 上限 1 ⇒ `u.Health - before = 0` ⇒ `HealUnit` 走 `healed <= 0` 那一支（`:9258-9264`：清待治疗表 + `return 0`）⇒ **不飘字、不发 `EvtKind.Heal`、不改血**。⇒ 行为同，账不同。
  - 另一条实卡对照（**反过来是对的**）：**`SAU53 Undying Legions`「when a friendly troop becomes a Remnant it gains Shield」** —— 走 `BecomesRemnant` 广播 → `ApplyOneGain` → `AddKeyword` ⇒ **有** `_remnantTraits` 簿记 ⇒ `Has("shield")` 为真 ✓（这正是 `E10TB` 那本书要服务的卡）。
- **建议（不在我白名单）**：`AddAuraKeyword` 对 `IsRemnant && !IsLeavingRemnant` 要么**整条不施加**（最接近原版 `AddEffect.c:159-170` 的残骸闸：`addTrait` 一律不放行），要么也记进 `_remnantTraits`。**二选一必须先补判据**（原版光环挂 trait 到残骸上会不会被那道闸挡住 —— 本轮没查到实据，如实标「还没查清」）。
- ⚠️ **另一处待查**：`TargetCriteriaEval.cs:719`（`TraitPasses`，抽目标用）现在也会把「带光环关键词的残骸」排除掉 ⇒ 「target a friendly unit with X」这类卡的可选目标集合会变。今天有没有这种卡（`with Regeneration` 一类的目标条件）**还没查清**。

### 5·3 `RecomputeActAndAttack` 的 doc 要按实况拆成两半（⛔ 我改不了，白名单外）

`RuleCore.cs:3522-3524` 现在写的是：
> 「⚠️ **光环那条路（`AddAuraKeyword` / `ClearAuraGrants`）没有接** —— 原版光环给的关键词走的是哪条路**还没查清**…」

**实况**（`D1DG` 发现 ③ 成立，本轮现读复核）：**收回那一侧早就接了** —— `UnitState.cs:1577`（现 `:1578`）/ `:2342`（现 `:2345`）自 2026-10-25 起就在调它；今天起还多了一层「周期内延后、按前后差判」（`:2110`）。
**建议替换文本**（零行数变化，只改那三行注释；`RuleCore.cs` 有别的写手 ⇒ 由调度台指派）：

```
        ///   ⚠️ **光环那条路：⚠️ 2026-10-27（B9·`W18AR`）订正 —— 「没接」已过期**：收回那一侧
        ///   自 2026-10-25 起就接了（`UnitState.cs:1578` / `:2345`）；2026-10-27 起改成「重算周期内
        ///   延到 `UnitState.EndAuraRecompute`、只有 `fast`/`flank` 豁免真的丢了才重算」（`UnitState.cs:2110`）。
        ///   `AddAuraKeyword`（重挂那一侧）**仍未接** —— 原版光环给的关键词走哪条路**还没查清**，⛔ 别照「加词就重算」顺手接。
```

### 5·4 账的行号漂移（需要调度台在同步点做）

本笔在 `UnitState.cs` 插了 **3 行**（`:2099` · `:2110` · 类尾那段起点不计入既有落点区）⇒ **23 条**账里 `UnitState.cs:…` 的落点行号整体 **+2**（现读：全仓 71 处引用 `UnitState.cs`，其中 23 处 ≥ 2108）。
⇒ 同步点跑一次 `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A --fix`（⚠️ `--fix` 只能在「没有 `.cs` 写手 + 补丁已合并完」时由调度台串行做）。`ActionPump.cs` **零行数变化**。
另有 3 行建议在合并时各追加一句指针（同一判据、别写第二份实现）：`战斗逻辑驱动.tsv` **682**（`CardScript.ActivateMinion`）· **861**（`RemoveTrait`）· **978**（`UpdateSummonSicknessAnims`）。

### 5·5 仍未闭合的判据（如实标，不算本轮欠账）

「光环**新挂上** `fast`/`flank`」（不是重挂、是真的第一次挂上）时，原版会不会重算两位 —— 未查清（`D1DG` 记的 (a) 那条）。我们**保持改前行为**：不重算（`AddAuraKeyword` 不动两位）。今天可观察的路径上由 `ApplyDeployTurnState` 兜住（四个部署入口都紧跟一次重算），所以**不构成现有差异**；但如果出现「单位已在场多回合、中途才吃到光环侧翼」的卡，就要回来补这条判据。

---

## ⑥ 改动清单

| 文件 | 行 | 改动 |
|---|---|---|
| `RuleEngine/Core/UnitState.cs` | `1569-1577`（注释）+ `:1578` | `ClearAuraGrants` 改走窄口；注释补「重算时机」与回归编号 |
| `RuleEngine/Core/UnitState.cs` | `:2345` | `ClearAuraNumericAndKeywords` 改走窄口 |
| `RuleEngine/Core/UnitState.cs` | `:2099` | `BeginAuraRecompute` 快照 `_auraExemptBefore` |
| `RuleEngine/Core/UnitState.cs` | `:2102`（summary 就地改写）+ `:2110` | `EndAuraRecompute` 按前后差补一次 act 重算 |
| `RuleEngine/Core/UnitState.cs` | `:3081-3124`（**追加在类尾**） | 判据说明段 + `_auraExemptBefore` + `RequestActRecomputeAfterAuraChange()` |
| `RuleEngine/Core/ActionPump.cs` | `:143`（就地，零行数变化） | 补「本行不动 act 两位」的不变式与理由 |
| `账/_parts/B9_W18AR.tsv` | — | 1 行（`EntityScript.get_displaySummonSickness()`，状态 **B**，P0，`B9_W18AR`）；`check_patch_conflicts.py` = ✅ 无跨补丁重复键 |

**验证读数**

- `TMPDIR=/tmp/wf_W18AR bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
- `bash d:/4/Unity/工具/ruleprobe.sh play check` ⇒ **rc=0**，指纹 `1b40f42a…` = 基线（见 §④）
- `RuleEngineTest.Run` **未跑**（调度台的活）；预期 `RE1`–`RE4` 转绿（§③）
- 行尾：两个文件仍是纯 LF（`CRLF=0 · bareCR=0`），未用 `sed -i`
