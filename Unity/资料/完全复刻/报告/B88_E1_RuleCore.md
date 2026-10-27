# B88_E1 报告 —— 引擎 · `RuleCore.cs`（第八十八会话 · 波 1）

**范围**：`python ledger_slice.py --file RuleCore --status C` 里**落点含 `RuleEngine/Core/RuleCore.cs`** 的
**24 行**（`账/战斗逻辑驱动.tsv`）。另一行 `特效动画.tsv` 的 `ResolveBloodThirstAnim` 同在本切片里，
但它的缺口落在 `CardPresentation/Battle/BattleDriver.cs`（E5）⇒ **未纳入本批补丁**，见 §五·6。

**结论一句话**：24 行里 **10 行升 A**（4 行本次真修、6 行复核确认早已落地）· **4 行升 B**（修掉一半）·
**9 行保持 C 并写清反证/移交**（缺口落在别的文件或 `WhenEvent.cs`）· **1 行判 D**（原版死代码，三源证据）。

---

## 一、结论（逐档）

| 档 | 行数 | 行 |
|---|---|---|
| **A** | 10 | `SetPlayerDataAndWarlord` · `SetupFullHero` · `BroadcastCardDrawn` · `BroadcastCollectFaith` · `BroadcastCollectSpiritStone` · `CheckAndApplyCurrentPack` · `EnoughPendingHealingToFull` · `HasPackBuffTrait` · `Heal` · `RemoveDuplicitous` |
| **B** | 4 | `BroadcastTurnSetup`（相位钩子已接，三跳广播移交）· `BroadcastUnitAttacked`（①修 ②移交）· `ResolveUnitAttacked`（②修 ①移交）· `ResolveActiveAbility`（执行器扩容，无 `play check`） |
| **C** | 9 | `ResolveCardAutoSummoned`（→E5）· `ResolveMissAction`（→E2）· `ResolveTriggerSacrifice`（→E2/E4）· `BroadcastDeadUnit`（→E2）· `BroadcastUnitDamaged`（→E2）· `BroadcastUnitPoisoned`（→E2/E4）· `CheckStun`（→E2+B01/B02/B05）· `ResolveUnitSummoned`（→E2/E4）· `OnTroopKilled`（→BattleContext 写权） |
| **D** | 1 | `ResolveTransient`（原版死代码：三源 0 调用点 + 池内 0） |

**本次真改代码的 4 处（引擎行为变化）**：

1. **督军 summon trait 常规开局激活**（A 两行）—— 影响**每一局**。
2. **`pack` 计数改 `Count-1`**（A 两行同一处修）—— 影响池内 **10 张带 pack** 的部队。
3. **被攻击即翻开伏击**（B）—— 原版第三条 `SetAmbush(false)` 出口。
4. **主动能力执行器扩容**（B）—— 从「2/86 个 `chosenAbility` 码」扩成「凡 `CardDef` 已配到正文 op 的行都跑」。

---

## 二、逐条证据（文件:行号）

### A 档（10）

| 账行 | 判据（原版） | 我们的落点 | 本批动作 |
|---|---|---|---|
| `SetPlayerDataAndWarlord` | `out_cpp2il_dev_ilspy/BattleManager.cs:15842`（= `SetupFullHero` + `heroCard.ActivateTraitsOnSummonOrEnchantment(onSummon:true, playSound:false)`） | `RuleCore.cs:605`（`BuildPlayer`）+ **:645**（新增那一句）；`ActivateTraitsOnSummonOrEnchantment` 声明 = `RuleCore.cs:3596` | **修**：`:645` 行尾接 `ActivateTraitsOnSummonOrEnchantment(p.Warlord)` |
| `SetupFullHero` | `out_cpp2il_dev_ilspy/BattleManager.cs:16459` | 同上（建督军 + 生命/上限 + 落 `BoardSpec.WarlordSlot` + 进场序号 早就对；缺的就是第二半） | 同上（同一笔） |
| `BroadcastCardDrawn` | `out_cpp2il_dev_ilspy/BattleManagerSupport.cs:704-744` · `decomp_full/BattleManagerSupport__BroadcastCardDrawn.c:20-80` | `RuleCore.cs:2941`（发 `Draw`）＋ `EffectResolver.cs:9546-9677`（`DrawHandHopTakes` 三跳） | 复核：`B9_E6ENG` 已落地（隔离实测 `b671acdb…`→`517be27d…`）⇒ A。**残余另计**：trigger **250**（`OnTrapDrawn`）未实现 → §五·1 |
| `BroadcastCollectFaith` | `decomp_full/BattleManagerSupport__BroadcastCollectFaith.c:1-44` | `EffectResolver.cs:12688-12689` / `:12754-12755` · `RuleCore.cs:1648-1649` | 复核：三处口同形（两态 `play check` 逐字节相同 `79057e33…`）⇒ A |
| `BroadcastCollectSpiritStone` | `decomp_full/BattleManagerSupport__BroadcastCollectSpiritStone.c:22`（预调）+ `:23-42`（棋盘排除自己）；调用点 `_ResolveUseWaystone_d__480__MoveNext.c:131` | `RuleCore.cs:12403`（新增预调）+ `:12404`（`card`/`subject` 改真值 + `preCalled: u`） | **修**：补上「被收那颗残骸卡自己」的预调 |
| `CheckAndApplyCurrentPack` | `out_cpp2il_dev_ilspy/CardScript.cs:16040-16076`（`UpdateWhileInPlay` 内联体：Hero ⇒ `Count`、其余 ⇒ `Count-1`、闸 `cardState != inHand`）；`decomp_full/CardScript__CheckAndApplyCurrentPack.c:1-57` 是同一段逻辑的另一份产物 | `RuleCore.cs:5034`（`FieldAttack` 的 pack 支） | **修**：`if (!u.IsWarlord) n--;` |
| `EnoughPendingHealingToFull` | `decomp_full/CardScript__EnoughPendingHealingToFull.c:2-70` · `FilterMethods__MeetsConditionCriteria.c:87-98` | `RuleCore.cs:14686`（谓词）· `:9320`（`HealUnit` 收尾清表）· `ConditionEval.cs:233`（消费点） | 复核：配对修已在（`W12MN` 2026-10-26，两条出口 `:9304` / `:9320`）⇒ A |
| `HasPackBuffTrait` | `decomp_full/CardScript__HasPackBuffTrait.c` + `CardScript.__c___HasPackBuffTrait_b__440_0.c` | `RuleCore.cs:5025-5034` | **判原版死代码**（三源 0 调用点，见 §四·2）⇒ 我们的闸 `Has("pack")` ∧ 只在场单位 = 与 `UpdateWhileInPlay` 的真实闸同形 ⇒ A |
| `Heal` | `decomp_full/CardScript__Heal.c:1-32` + `_Heal_d__468__MoveNext.c:73-81` | `RuleCore.cs:9273`（`HealUnit`：四道闸 `:9285`/`:9291`/`:9298`/`:9300-9305`，收尾清表 `:9320`，事件 `:9309`） | 复核：逐条一致 ⇒ A |
| `RemoveDuplicitous` | `decomp_full/CardScript__RemoveDuplicitous.c:1-10`；**唯一调用点** `BattleManager._ResolveAttack_d__438__MoveNext.c:512-519` | `RuleCore.cs:6641`（调用点）＋ `RuleCorePartial_B88E1.cs:48`（`RemoveDuplicitousTrait`） | **修**：补调用点（被打者带 `tainted` ⇒ 摘一次，这一记照常结算） |

### B 档（4）

| 账行 | 已做 | 还欠（移交对象） |
|---|---|---|
| `BroadcastTurnSetup` | 相位钩子 `ResolveAtTurn(ctx, "turn_setup")` 接进 `BeginTurnCore`（`RuleCore.cs:1965`，位置 = 抽牌三道闸之前，与原版 `_NextTurn`：TurnStart → BroadcastTurnSetup → 抽牌 一致） | 原版那三跳（棋盘 `IsInPlay` + 双方手牌）走的是 **trigger 155 广播**：需 ① `WhenEventKind` 常量（`Core/WhenEvent.cs`）② `TriggerNameOfWhenKind` 映射（E2）。且 `EffectText.SplitAtTurn` 只产 `turn_start`/`turn_end` ⇒ 这条相位暂无消费者（池内 155 = 0） |
| `BroadcastUnitAttacked` | **①** 去掉 `!ReferenceEquals(target, attacker)` 闸（`RuleCore.cs:6999`）—— 判据 `BroadcastUnitAttacked.c:29` 无条件预调攻击者 ⇒ 自打自己两次 | **②** `Attack` 广播不携带 `attackType`：`WhenEvent` 没有该载荷列（`WhenEvent.cs`）+ 广播体在 `BroadcastWhenCore`（E2） |
| `ResolveUnitAttacked` | **②** 补上原版**第三条** `SetAmbush(false)` 出口（`CardScript__ResolveUnitAttacked.c:104-115`：带 `ambush` ∧ `cardState ∈ {2,15,3,17}` ⇒ 翻开，**在读伤害之前** ⇒ 0 伤害也翻）—— `RuleCore.cs:7006` + `RuleCorePartial_B88E1.cs:79` | **①** `untilNextAttack` 到期档：需 `UnitState.EffectRecord` 加位（**E4**）+ 解析层落这位（E2） |
| `ResolveActiveAbility` | 执行器扩容：`RuleCore.cs:15397`（plan）+ `:15466`（执行）+ `RuleCorePartial_B88E1.cs:103/:140`（`ActiveRowHasOps` / `RunActiveRowOps`）。判据 = 「这一行已被 `CardDef.AttachByAbilityKind` 按 `chosenAbility` 的**族名**配到正文 op（含 `ta`/`fm`/36 格 criteria）」⇒ **数据决定做什么、不自己造数值**；`ta ∈ {30,220}` 才吃调用方点的目标（`AbilityLogic__GetTargets.c:192-207`） | `AttachByAbilityKind` 配不上的族（`ActiveAbilityMap = gap:kind-*`）仍无执行器 ⇒ `CardDef`/数据侧（E4）。**且本批无 `play check` 读数**（会动指纹 ⇒ 必须由调度台在「无 `.cs` 写手」窗口重钉基线） |

### C 档（9，全部给反证 + 移交对象）

| 账行 | 缺口 | 为什么本批做不到（出处） |
|---|---|---|
| `ResolveCardAutoSummoned` | 「在手牌就先播打出动画」那一拍 | **表现层** ⇒ `CardPresentation/Battle/BattleDriver.cs`（E5）。引擎侧 5 跳其余四跳已对 |
| `ResolveMissAction` | `chosenAbility == 290` 的执行口 | 通用 ability 派发（`EffectResolver.ResolveOps` / ability 链）⇒ E2。⛔ 仍不得改在「无合法目标」出口调 |
| `ResolveTriggerSacrifice` | 225 `OtherCardSacrifice` 的**独立广播** | 需 `WhenEventKind` 常量（`WhenEvent.cs`）+ 广播调用点（E2）⇒ E2/E4 |
| `BroadcastDeadUnit` | 手牌跳内第 4 支（`!=null ∧ !=某张 ∧ !IsInPlay`） | 手牌两跳在 `EffectResolver.BroadcastWhenCore` ⇒ E2 |
| `BroadcastUnitDamaged` | 「攻击者必收一次」（哪怕不在场 / 自己就是被打者） | 需 `DamageInfo.damageType == Attack(4)` 进载荷（`WhenEvent` 无此列；我们 `DamageKind` 也没有这一维）+ 绕开 `PreCallBoardListener` 的 `FindUnit` 闸 ⇒ E2 |
| `BroadcastUnitPoisoned` | 290 从不发 | 发射口在挂毒链（E2）+ kind 常量（`WhenEvent.cs`） |
| `CheckStun` | 「已带 stun + 后得 unstunnable ⇒ 立刻摘」 | 插入点 = 变回单位那一刻：`EffectResolver.DoReanimate` / `ReanimateRemnantInPlace`（E2）+ 变身路整条未实现（同 `AddTransformCardWithBuff`） |
| `ResolveUnitSummoned` | 550 `Support` 整条 | 触发名已有（`CardDef.AbilityTriggerNumberOfName` 的 `{"Support",550}`），但广播体/调用点在 E2 + 需 kind 常量 |
| `OnTroopKilled` | `DeadUnit` 不带击杀方 | 字段在 `Core/BattleContext.cs`、计数格在 `Core/AchievementTally.cs`（两个写点 `RuleCore.cs:9795`/`:10255` 无处可存） |

### D 档（1）

`ResolveTransient` —— 原版 **死代码**，三源合判（见 §四·2）。⛔ **不新增**我们自己的「回合末一次性单位」扫描
（那会造出原版没有的触发点）；若将来查到真实调用点，落点 = 与 `SweepEphemeral` 并列、死因 20。

---

## 三、改动清单

| 文件 | 改动 | 净行数 |
|---|---|---|
| `MyGame/Assets/RuleEngine/Core/RuleCore.cs` | **8 处就地编辑，全部零净行数**（`15587` → `15587`，行尾仍为纯 LF：`b.count(b'\r\n')=0`、`bare_cr=0`） | **±0** |
| `MyGame/Assets/RuleEngine/Core/RuleCorePartial_B88E1.cs` | **新建**（`public static partial class RuleCore` 的扩展；新成员全在这里 ⇒ 不动源文件行号） | +新文件 |

逐处（`RuleCore.cs`）：
- `:645` `BuildPlayer` 督军落格后接 `ActivateTraitsOnSummonOrEnchantment(p.Warlord)`
- `:1965` `BeginTurnCore` 抽牌前接 `ResolveAtTurn(ctx, "turn_setup")`
- `:5034` `FieldAttack` pack 支 `if (!u.IsWarlord) n--;`
- `:6641` `DeclareAttackExecute` 结算段开头 `RemoveDuplicitousTrait(ctx, target)`
- `:6999` 去掉 `!ReferenceEquals(target, attacker)` 闸（`if (true)` + 判据注释）
- `:7006` 攻击广播之后 `FlipAmbushOnAttacked(ctx, target)`
- `:12403-12404` `CollectWaystoneCore` 预调那颗残骸 + 棋盘跳 `preCalled: u`
- `:15397` / `:15466` `UseActiveAbilityCore` 的 plan 与执行（`RunActiveRowOps`）

**账补丁**：`资料/完全复刻/账/_parts/B88_E1.tsv` —— 24 行 + 11 列表头；键与 `ledger_slice --file RuleCore --status C`
里落点含 `RuleCore.cs` 的 24 行**逐一配对（missing 0 / extra 0）**。

---

## 四、没查清的部分

1. **`play check` 基线未重钉** —— 本代理不跑 Unity（红线）。凡本批改动都会动指纹：
   ① 督军 trait（可能改攻防位）② pack 计数（10 张卡）③ 伏击翻开（`EvtKind.AmbushExit` 事件）
   ④ 主动能力扩容（最多 87 张卡）⑤ 灵魂石预调（4 张监听卡，预期零变化）⑥ `tainted`（池内 0）。
   ⇒ **调度台必须在「无 `.cs` 写手」窗口跑一次 `play check` 并重钉基线**。
2. **`HasPackBuffTrait` / `ResolveTransient` 的「死代码」结论有内联风险** —— `E8/E9 rel32` 全 `.text`
   扫描对**被内联**的方法看不到调用点。两条都如实记在账里（前者不影响结论：我们按语义实现；
   后者若真有调用点，落点已写清）。
3. **`ResolveActiveAbility` 扩容后的语义边界**：`AttachByAbilityKind` 在「同族多行」时只挂
   `use[0].Value` 并留 `TargetCriteriaGap = "active-multi-row"` ⇒ 那些行的 36 格 criteria 缺失，
   本批执行时沿用 op 自己原有的目标规格。**该多行情形今天分布多少张、有没有卡实际用到** —— 没查。
4. **`BroadcastTurnSetup` 的三跳广播**只做到「相位钩子」，`ResolveAtTurn` 的消费者（`SplitAtTurn`）
   今天不产 `turn_setup` ⇒ 本条在**可观察层面仍是零**（与池内 155 = 0 一致）。
5. **`BroadcastCollectSpiritStone` 预调的行为影响面**：`card` 从 `null` 改成 `u.Card` 之后，
   `WhenEvent` 的卡级筛选 / 代词 `it` 会开始看到那张残骸 —— 4 张监听卡卡面都不用代词（已核），
   但**没有 `play` 读数**证明零变化。

---

## 五、顺手发现（⛔ 未自行改动，交调度台分流）

1. **trigger 250（`trap drawn`）未实现** —— `CardScript__CardDrawn.c:23` → `OnTrapDrawn` → `OnTrigger(0xfa = 250)`；
   我们只有 260。`BroadcastCardDrawn` 那一行（B9_E6ENG）已明确把它列为**残余**。建议**单列一条新账**
   （域 = 战斗逻辑驱动，原版类 `CardScript`，方法 `CardDrawn`/`OnTrapDrawn`）。
2. **`HasPackBuffTrait`（VA `0x1805EFBF0`）与 `ResolveTransient`（VA `0x1805FA1E0`）在二进制里 0 调用点** ——
   三源（`decomp_full` / `out_cpp2il_dev_ilspy` / `E8/E9` 扫描）全 0。**很可能还有别的「只有定义、没有调用者」的方法**
   （`CheckAndApplyCurrentPack` 也是这一类，只是它的逻辑被内联进 `UpdateWhileInPlay`）。
   建议调度台把「按 `script.json` 的 Address 做一次全 `.text` 调用点扫描」做成常驻工具，
   用来批量把 D（死代码）判掉 —— 我这次用的是一次性只读脚本 `d:/4/_tmp_view/b88e1_xref.py`（不在工程内）。
3. **`decomp_full` 里同一段逻辑有两个产物**：`CardScript__CheckAndApplyCurrentPack.c`（独立方法体）与
   `out_cpp2il_dev_ilspy/CardScript.cs:16040-16076`（`UpdateWhileInPlay` 的内联体）。
   两份都读才看得出「外层闸 `cardState != inHand`」—— 只看 `.c` 会漏掉那一句。**同类坑建议写进工作准则**。
4. **`BattleManager.BroadcastUnitDamaged`（非 Support 版）在账里也存在**，判据出处写着「不是纯转调」；
   它不是本批的 C 行，但和 `BattleManagerSupport.BroadcastUnitDamaged` 的缺口**同族** ⇒ 移交 E2 时一并看。
5. **`typecheck` 有别人的半成品错误**（不是我负责的文件）：
   `Assets/CardPresentation/Shell/AllianceSearchTab.cs(494,77): error CS1503`（参数 4 无法从「数组」转换为
   `System.Action<Transform, PxRect, int>`），且行号在两次跑之间从 `488` 漂到 `494` ⇒ 那个文件**正在被改**。
   编辑器程序集那条 `CS0006 WFCheck.dll` 是它的下游（运行时程序集没编过 ⇒ 编辑器程序集没有被生成）。
   **本批两个文件零错误**。
6. **`特效动画.tsv` 的 `ResolveBloodThirstAnim`**（同在我的切片里，落点含 `RuleCore.cs:12439`）：
   RuleCore 侧 `EmitBloodThirst` 的 ①② 已对齐，缺口是演出（`BattleDriver.cs:24630`/`:24656` 的
   `DOScale` 单次脉冲 vs 原版 `DOFade ×6 Yoyo`）+ 那条 `cardState == 5` 闸。
   **归 特效动画域 / E5**，本批未纳入补丁（避免与那一域的写手撞车）。
