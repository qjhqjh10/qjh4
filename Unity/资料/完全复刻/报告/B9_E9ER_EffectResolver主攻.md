# B9 · `E9ER` —— `EffectResolver` 主攻（完全复刻 · 批次 9 · 2026-10-26）

> 执行代理 **`E9ER`**（有写权限）· 白名单 = `RuleEngine/Core/EffectResolver.cs` · `RuleEngine/Core/RuleCore.cs`
> （+ 量具一处：`工具/ruleprobe/PlayProbe.cs`，见 §四·3，**独立旋钮、老键一字不动**）。
> ⛔ 没跑 Unity 批处理 / 没动 git / 没改正本 / 没改主账表（只写 `账/_parts/B9_E9ER.tsv`）。

## ① 结论

| # | 件 | 结论 | 读数（改前 → 改后） |
|---|---|---|---|
| ① | **`BL47 Disgustingly Resilient`** | 🔴 **没修（根因不在我的白名单里）** | 单体 `play BL47` 仍 **0 条实得**。**根因已定位到确切一行**：`EffectText.ReAdditionalForEach` 只认「数字在前」的词序（`and an additional +2 Melee`），而 BL47 是「**名词在前**」（`and an additional Armour 1`）⇒ 正则不匹配 ⇒ 落进「后置 `for each`」那一支 ⇒ `PerCount = 0`、`CountRef = "it"` ⇒ 运行时按 `TryComputeRepeats` 数出 **0 遍** ⇒ **连基础那一份也跳过**。改动请求见 §四·1（含可直接落地的正则 + 三行调用点改法）。 |
| ② | **`Side == "any"` 的 19 张卡打不到对面** | ✅ **修完（引擎侧治本）** | `play ASH73`（`Deal 5 damage to a troop`）**默认**：打**自己**督军 `WARLORD 28→26`（敌方一个数字没动、对敌伤害 0）。**带 `preset tpick=1`（= 玩家点定敌方 `FIX_E1`）**：`P1[3] FIX_E1 6→1`、对敌伤害 **5**。同一对读数就是「改前 / 改后」。 |
| ③ | **`CheckIfCodexTriggered` 的战术调用点缺闸②** | ✅ **修完（一行）** | 夹具不可观测（见下），`play check` 指纹未变 —— **这是预期**。 |
| ④ | **`IsValidTarget` 的 camouflage 闸我们比原版严** | ✅ **修完** | 池内 **0 张可达**（camouflage 只能由 `addTrait` 授予）⇒ 零行为变化。 |
| ⑤ | **`OnQuestResolved` 的 490 分支缺失** | ✅ **修完（判据升级）** | 池内 4 张 DA 督军的结果不变；「别的阵营以后加 490 能力」这一档**从此表示得出来**。 |
| ⑥ | **`BroadcastEarnSkull` 发射点缺失** | ✅ **修完** | 池内 `abilityTriggers` 含 640 的卡 **0 张** ⇒ 零行为变化（`play check` 指纹不变）。 |
| 追加 | **「撤单位 → 回手到原索引」** | ✅ **已经是做完的（批次 6 `G14`）** | E2P11 指的两个调用点现读 `EffectResolver.cs:3659/:3670` 与 `:3859/:3873` **都已改调 `AddCardToHandAtWithinLimit`**。剩下唯一的 `ps.Hand.Add(`（`:3859`）语义上就是「队尾」= 原版无下标那一支（见 §②·6）。 |
| ⑦ | **P-02 那 10 条** | ⏭ **本批没动**（理由逐条见 §③·2） | —— |

**必报三项**

* **typecheck**：`TMPDIR=/tmp/wf_E9ER bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（末次读数，跑在最后一次 `.cs` 改动之后）。
* **`pool_effect_check`**：⚠️ **派活单上那条命令写法不成立** —— `--only` 收的是 **`chosenAbility` 数字**（`int(x)`），
  `--only BL47` 会 `ValueError` 崩。正确用法 = `--only 30`（BL47 的族是 `30 buffOthers`）。
  现读：`python -I -X utf8 工具/pool_effect_check.py --only 30` ⇒ **`判「错」的族: 30×1`**，
  BL47 那一行仍是 `判定=错 / 实得=一条变化都没有（other=0）` —— **符合预期（①没修）**。
* **`play check` 指纹**：**当次现算 = `16db33fa634ab9d958b968688def69ff`**（= 调度台已确认的新基线），
  逐卡差异仍只有 **`BL18` / `UM18`**（= `E7P06` 接 669 的既有归因，⛔ 未追、未改基线）。
  ⇒ **本批全部改动对全池指纹零影响**，且能逐条解释：②③④⑤⑥ 在夹具上要么不可达、要么只在「玩家点定敌方」那条**默认不走的**路上生效。

## ② 逐条证据

### ②·1 `Side == "any"` 打不到对面（治本：让「目标对象」成为唯一权威）

* **判据（第一权威）**：原版出牌时目标以 **CardScript 引用**的形式进动作 ——
  `_ResolvePlayCardFromHand_d__447__MoveNext.c:905-940` 把它写进 `action+0x20`，
  之后 `IsValidSpellTarget(spell, **target**)` / `AbilityLogic.GetTargets(…, **targetCard**, …)` 读的都是**那个对象**
  ⇒ **侧是跟着目标走的**，原版根本没有「按格号反查一侧」这一步。
* **我们的病**：`EffectText.PickTarget` 只返回 `own` / `enemy` 两档（`Any` 的规格返回 `null`）
  ⇒ `PickSide` 对「任意一侧」的卡给 `""` ⇒ `PlayTacticCore` 的 `side == "enemy" ? 1-p : p` **恒取我方**
  ⇒ 点敌方的单位，效果落在**我方同号格**上。
  ⚠️ 而「能不能选」那一层早就是**两侧并集**（`IsLegalPick` 的两支 `AddSide` 现读 `:2019/:2021` ·
  `PlayTargetPool` 同形 `:2081/:2084`）
  ⇒ 池子允许点对面、结算却解成自己人 = **高亮/合法性说能选、打出去打错人**。
* **改法（我这两个文件内收口）**：
  * `EffectResolver.PlayTactic(..., UnitState chosen = null)` / `CanPlayTactic(..., UnitState chosen = null)` /
    `PlayTacticCore(..., UnitState pickedByPlayer = null)`：**有对象就以对象为准**，格号只兜底。
  * `RuleCore.PlayCardCore` 那一句 **同时递格号与实例**：`PlayTactic(ctx, p, handIdx, ChosenTargetSlot(ctx, chosen, slot), chosen)`。
  * 新增 `RuleCore.PickedBySlot`（类尾）：没有对象时按 `spec.Side` 解一侧，**⛔ 不做「我方空着就解对面」的兜底**
    （格号不带侧信息，两侧同号格都有人时两种解都「看着对」= 又一种静默错打；宁可让调用方把对象传进来）。
  * `EmitFollowUpResolution` 的**侧**也改成按「点定那个单位归谁」（原版那一拍的动画挂在目标 CardScript 上）
    —— 否则「任意一侧」的卡会「打在敌方、动画演在我方同号格」。
* **落点**：`EffectResolver.cs:1915`（`CanPlayTactic`）· `:1996-2000`（校验支）· `:2445`（`PlayTactic`）·
  `:2462`（`PlayTacticCore`）· `:2486-2487`（`chosen` 解析）· `:2761`（follow-up 侧）· `:16271`（`PickedBySlot`）·
  `RuleCore.cs:3893`（传 `chosen`）。
* **读数**（同一张卡、同一条命令加不加旋钮）：
  ```
  bash 工具/ruleprobe.sh play ASH73                     ⇒ ✎ 改动 P0[4] WARLORD 2/0/0/28 → 2/0/0/26 · 对敌伤害 0
  bash 工具/ruleprobe.sh play ASH73 preset tpick=1      ⇒ ✎ 改动 P1[3] FIX_E1 2/0/0/6 → 2/0/0/1  · 对敌伤害 5
  ```
  `ASH73` = `Deal 5 damage to a troop`（卡面**没写 enemy** ⇒ `Side=any`，与 `ta=30 F0E0` 一致）。
  另 `SAU38`（`Deal 2 damage`）默认因结构化条件筛后只剩敌人 ⇒ 改前改后都打敌方（**不是**这张的证据）。
* **产品路径已通**：`BattleDriver.ResolvePlayTargetByUnit`（我方/敌方各试一次命中）→ `_chosenTargetForNextPlay = hit`
  → `RuleCore.PlayCard(..., chosen)` →（分流）`PlayTactic(..., chosen)`。表现层对非单位卡**不做侧收窄**
  （`SpellTargetBoardOwner` 对 `PickSide==""` 返回 -1 = 不限），所以玩家点敌方是点得到的。

### ②·2 `BL47`（没修，根因在 `EffectText.cs`）

* **卡面**：`Give Armour 1 to a friendly troop, and an additional Armour 1 for each Dark Pact on it`。
* **结构化真值**（`cards_truth.json` BL47 两条 logic，逐字）：
  ① `buff { buffType 1, armourChange 1 }` ② `buff { buffType 1, armourChange 1, **armourValueOptions 201** }`
  —— 与 `BL43` 同族（`E1` 的 `C3 §1·1` 已取证：`NewBuffStat.c:207-232` 的 `200/201/202/203` 族 = **计数 × (change × multiplier)**，
  201 = Dark Pact 数）。⇒ 期望 = **1 + n**（n = 目标身上的 Dark Pact 层数）。
* **实测根因**（`bash 工具/ruleprobe.sh card "Disgustingly Resilient"` 现读）：
  ```
  op: verb=give amount=1 payload=[armour 1] target=[a friendly troop, and an additional armour 1]
      countRef=[it] countScope=[darkpact]  perCount=0
  ```
  ⇒ 两个病同时出现：**目标短语把附加从句吞了**、**`PerCount == 0`** ⇒ `IsPerCountIncrement` 为假
  ⇒ 走 `TryComputeRepeats`（现读 `EffectResolver.cs:13719`，那条 `按 … 计 N 遍` 的日志在 `:13737`）⇒ `n = 0` ⇒ `repeats = 0` ⇒ **整条不结算**。
  对照 `BL43`（数字在前）：`基础 2 + 每个 it 加 2 × 0 = **2**` ⇒ 基础那一份在。
* **为什么不只是「倍数 0 时别跳基础」**：`TryComputeRepeats` 的「N=0 ⇒ 一次都不结算」对**重复型**是对的
  （盘上 0 个友军时 `Deal 1 damage … for each friendly unit` 就该什么都不做）。真正的错在**解析层把「加法型」判成了「重复型」**。
  改动请求见 §四·1。

### ②·3 `CheckIfCodexTriggered` 闸②（③）

* **判据**：`BattleManager__CheckIfCodexTriggered.c:30-36` —— `manaLeft == 0` 之后再判
  `shouldCheckCost ∧ get_CurrentCost(刚打出的那张) < 1 ⇒ return`。实参来源：
  `BattleManager__PayCardCost.c:30` 传 **1**（普通出牌，单位/战术都走它）· `PayCardPlayedCostOath.c:24` /
  `PayActiveAbilityCostOath.c:30` 传 **0**（誓约两条，⛔ 别全局默认 true）。
* **改法**：`EffectResolver.cs:2739` `CheckCodex(ctx, p)` → `CheckCodex(ctx, p, inst, shouldCheckCost: true)`。
  `inst` = 刚打出的那一份 `CardInstance`（`CostOf(ctx,p,inst)` 只读卡与 mods，不看它还在不在手牌）。
* **为什么指纹不变（可解释）**：夹具里没有「0 费战术打出后能量**恰好归零** ∧ 场上有 `Codex` 单位」的组合
  （`play` 夹具的棋盘只有 `FIX_A/B/C`，都不带 Codex）⇒ 本闸在夹具上**不可观测**。
  ⛔ 不能拿「指纹没变」当「没改」。

### ②·4 camouflage 闸（④）

* **判据**：`RawCardScript__IsValidTarget.c:16-40` 的 camouflage 支**多一条** `施放者.cardType == 0x14 (§Tactic)`；
  而 `IsValidSpellTarget.c:41-53`（战术卡专用那条路）**没有**这一条 —— 两支并不矛盾：后者本来就只服务法术。
  ⇒ 正确口径 = **camouflage 只在「施放者是战术卡」时挡**。
* **改法**：`AddSide(..., bool tacticCaster = false)`；`IsLegalPick(..., CardDef casterCard = null)` 与
  `PlayTargetPool` 各按「那张要打出的牌」算 `!card.IsUnit`（= cardType 20：tactic / defence / enchantment）传下去。
  `stealth` 与 `ward` 两条**没动**（它们本来就不分卡种）。
* **可达性**：`camouflage` 只能由 `addTrait` 效果授予；池内 `desc`/`descZh`/`cardTraits` 现扫 **0 张** ⇒ 零行为变化。

### ②·5 `OnQuestResolved` 的 490 分支（⑤）

* **判据**：`RawCardScript__OnQuestResolved.c:1-9` 全文 = `OnTrigger(trigger=490 QuestTriggered, …)`；
  「这张卡有没有 490 能力」的原版判据 = `RawCardScript__HasAbilityType.c:16-38`（遍历 `cardAbilities[].trigger`，
  **只看 trigger、不看 logic 空不空**）。
* **改法**：`QuestPointThreshold` 的闸从「督军阵营 == DarkAngels」升级成
  `ps.Warlord.Card.AbilityTriggerPresence(490)`（= `abilityTriggers` 列，v17 起每张卡都落）。三档：
  `1` 造 / `0` 明确不造 + 出声一次 / `-1` 旧表未知 ⇒ **退回阵营代理 + 出声**（⛔ 未知不许读成「没有」）。
* **数据核对**：`cards_engine.json` 的 `abilityTriggers` —— `DA1 [46,490]` / `DA2` / `DA3` / `DA75` 都含 490；
  ⚠️ **`cards_truth.json` 的 hero 行没有 `abilityTriggers` 这一列**（判据要读 `cards_engine`，别读 `cards_truth`）。
* **残留（如实记）**：仍**不读** 490 能力自己的 payload（`chosenAbility 270 = createCardInDeck` 的卡表），
  而是走 `CreatePool.Resolve(…, "a secret", 阵营)`；对现池 4 张 DA 督军结果相同。
  也没有新增 `WhenEventKind` 的 490 广播口 —— 现池带 `trigger 490` 的卡**只有**这 4 张督军，
  而它们那条能力正是本方法实现的那件结果 ⇒ **没有第二个听众**（加了就是没有发射口/消费口的死代码）。

### ②·6 `BroadcastEarnSkull` 发射点（⑥）+ 「回手到原索引」（追加项）

* **判据（逐句）** `CardScript__CheckForSkullEarned.c:1-69`：
  ```
  if (cardType != 10) return;                     // ① 只对督军卡（10 = Hero）
  if (EnoughPendingDamageToDie()) return;         // ② 这一下会死 ⇒ 不广播
  n = GetEnemySkullCount(myHealth);               // ③ = #{t ∈ {20,10,0} : health <= t}
  if (saved(+0x354) < n) { saved = n; BroadcastEarnSkull(...); }   // ④ 变大才播、**只播一次**
  ```
  `BattleScoreManager__GetEnemySkullCount.c` 的计数式与 `DeckRules.SkullsFor` **逐字同义**（现读核过）。
  **得主 = 被打那个督军的【对手】**（“EnemySkullCount” 的语义 —— 这是我方把对面督军打下去、我方得骷髅）。
* **改法**：新增 `NoteWarlordHealthForSkulls(ctx, warlord)`（`EffectResolver.cs:16331`；计数用**每局附加状态**
  `ConditionalWeakTable`（与 `QuestAux` 同一套模式，`PlayerState`/`BattleContext` 不在白名单）+ 派生状态不参与指纹/存档），
  两个真实血量写点各调一次：`RuleCore.cs:8849`（`Hurt` 的 `u.Health -= actual` 之后）·
  `RuleCore.cs:3076`（牌库抽空的疲劳支）。广播本身用现成的 `BroadcastEarnedSkull`（`:15705`，只跑棋盘跳 + 常驻效果跳）。
  ⚠️ **只播一次**（⛔ 不是逐档 for 循环）—— 原版 `+0x354` 就是「抬到新值 + 播一声」。
* **可达性**：池内 `abilityTriggers` 含 640 的卡 **0 张** ⇒ 零行为变化（`play check` 指纹不变）。
* **追加项（回手到原索引）**：**已由批次 6 `G14` 做完** —— E2P11 指的两处现读
  `EffectResolver.cs:3659/:3670`（`deploy` from hand 打不出去 ⇒ `AddCardToHandAtWithinLimit(handIdx0)`）与
  `:3859/:3873`（`autoPlayFromHand` 从牌库/墓地来的那份 ⇒ 原位 = 队尾）。
  我另核了两件：① 引擎里剩下的**唯一** `ps.Hand.Add(`（`:3859`）语义上就是「队尾」，原版那条协程本身就是 append
  （`PlayerHand._AddCardNotDrawnToHand_d__39__MoveNext.c:33` 走 `List.Add`，与带下标的 `…AtIndex`（`:40`）是**两支**）；
  ② `DoReturn` 的 `dest == hand` 用 `AddCardToHandWithinLimit`（append）也是对的 —— 棋盘上的单位没有「原手牌下标」，
  原版回的也是**无下标**那一支。⇒ **本项无需改动**（账已核销，见 `_parts/B9_E9ER.tsv` 的 `AddCardNotDrawnToHandAtIndex` 行）。

## ③ 没查清 / 本批没做

1. **`BL47` 的修复本身** —— 需要改 `RuleEngine/Core/EffectText.cs`（不在我的白名单）。改法见 §四·1，
   是**两处小改**（一条新正则 + `TryForEach` 里选择哪个 group），`BL43` 那条路**一字不动**。
2. **P-02 那 10 条逐条裁定（本批没动，理由）**：
   | 账行（方法） | 现状 | 本批为什么没做 |
   |---|---|---|
   | `AbilityLogic.AdjustCardEffect(...)` | C | 动**所有多目标 buff** 的共用结算量（`addTrait` 的 stackable/replace 两档 + `removeTrait` **不该折**），一改全池指纹；派活单明写「最后做、全池对拍」⇒ 需要一次专门的对拍窗口。**另核实一条**：它真正差的不是数值族（数值族「跑 n 遍」与「×n」结果等价），而是 **`addTrait` 的 `traitValue` 账**（原版一条实例 `traitValue = change × N`）—— 那一位在 `UnitState`（P-05 面）⇒ 两个包必须一起做，单做 `EffectResolver` 这半边会做出「实例少一个但 traitValue 还是 1」的半成品 |
   | `AbilityLogic.GetRefCard()` + `UM118` | C | 缺 `PreviousAbilityTargets[0]` 的取用 + `valueOptions 100/101/110/120` 四档求值，**其中一半在 `EffectText.cs`**（`equal to its [Attack]` 句型）⇒ 跨白名单 |
   | `BattleManagerSupport.BroadcastCardPlayed(...)` | B | 要动的是 `BroadcastWhenCore` 的**棋盘跳闸**（「待死」闸 vs 原版无 `IsInPlay` 过滤）—— 那是**所有 kind 共用**的一段，牵动 20+ 个广播器 ⇒ 应单独一批、单独对拍 |
   | `BattleManagerSupport.BroadcastCollectQuestPoints(...)` | **已 A** | 只剩「预调次序 / IsInPlay 闸」两处结构差（原版棋盘那一趟没有 IsInPlay），与上一条同源 |
   | `CardScript.AlreadyInPosition(...)` | **已 A** | 表现层（`CardView.SetPose`），不在白名单 |
   | `CardScript.CheckPossibleCombinations()` | C | **组合（Combo）系统整条没建**（`CheckMeetsCombination` 在 `Assets/` 0 命中）⇒ 不是「补一个调用」的量级 |
   | `CardScript.CheckReplacedAbility()` | B | 我们**从卡表列直接读**（`hasActiveAbility` + `activeAbility` 正文），设计上就不需要运行时重算入口 ⇒ 属「形状差、结果等价」，要不要改判 A 需调度台裁 |
   | `CardScript.ChooseAttackTypeAutomatically()` | **已 A** | —— |
   | `CardScript.ReactToUnitReanimated(...)` | **已 A** | —— |
   | `RawCardScript.UpdateTriggerOnce(...)` | B | 需要 `trigger 200` 的**分发口**（现池 `trigger 200 = 0` 条）⇒ 铁律 11 的休眠机制，得连同「触发号 → 分发」一起补 |
3. **`play check` 里 `BL18`/`UM18` 各 +4** —— 按调度台口径**未追**（`E7P06` 接 669 的既有归因）。
4. **要等 P-04 的**：本批**一条都没碰随机源**（`ctx.Rng` 只读不动）。
   `BL47` 修好之后也不涉及随机。**要等 P-05（`UnitState.Has` 语义）的**：camouflage/stealth 那两条闸读的是
   `u.Has(...)`，P-05 改「残骸 `Has(卡面 trait)`=false」之后**这两条闸在残骸上会跟着变** —— 到时候要复跑一次
   `play ASH73 preset tpick=1` 与 `pool_effect_check --only 30` 确认没漂。

## ④ 改动清单

1. **`RuleEngine/Core/EffectText.cs`（改动请求 · ⛔ 我没碰）**
   ```csharp
   // ① 在 ReAdditionalForEach 后面加一条（词序反过来那一支，池内只有 BL47）
   static readonly Regex ReAdditionalForEachReversed = new Regex(
       @",?\s*and an additional\s+([A-Za-z][A-Za-z ]*?)\s+\+?(\d+)\s+for (?:each|every)\s+(.+)$",
       RegexOptions.Compiled);

   // ② TryForEach（现读 :4898）那一段：
   var add = ReAdditionalForEach.Match(low);
   bool reversed = false;
   if (!add.Success) { add = ReAdditionalForEachReversed.Match(low); reversed = add.Success; }
   if (add.Success)
   {
       string countRef = add.Groups[3].Value.Trim();          // 两条正则的第 3 组都是「计数短语」，不变
       ...
       int inc = int.Parse((reversed ? add.Groups[2] : add.Groups[1]).Value);
       string unitWord = (reversed ? add.Groups[1] : add.Groups[2]).Value.Trim();
   ```
   预期读数：`play BL47` ⇒ `FIX_A 2/0/0/2 → 2/**1**/0/2`（护甲 +1，Dark Pact 0 层）；
   `pool_effect_check --only 30` 的 BL47 从「错」变「对/未验到」；
   ⚠️ **`play check` 指纹会变**（只该变 BL47 那一行，逐条可解释）。
2. **`RuleEngine/Core/EffectResolver.cs`**（16169 → 16344 行，**+175**）
   * ③ `:2739` `CheckCodex(ctx, p, inst, shouldCheckCost: true)`（+5 行注释）
   * ④ `AddSide`（`:2897`）加 `tacticCaster` 形参 + camouflage 条件（+8）· `IsLegalPick`（`:2005`）加 `casterCard`（+6）·
     `PlayTargetPool` 按 `card` 传 `tacticCaster`（+3）· 旧「已知偏离」注释就地改写成「已修」（−1/+3）
   * ② `CanPlayTactic` 形参 + 校验支（+6）· `PlayTactic` 形参（+3）· `PlayTacticCore` 形参 + `chosen` 解析（+12）·
     `EmitFollowUpResolution` 的侧（+5）· 类尾新增 `PickedBySlot`（+36）
   * ⑤ `QuestPointThreshold` 的 490 闸（+18）
   * ⑥ 类尾新增 `SkullAux` / `SkullAuxOf` / `NoteWarlordHealthForSkulls`（+72）· `BroadcastEarnedSkull` 的 doc 就地改（+2）
3. **`工具/ruleprobe/PlayProbe.cs`**（3866 → 3879 行，**+13**）：新增 **`preset tpick=1`**（非单位卡的「点定目标」）。
   🔴 **故意不复用 `pick=`** —— 那个已被 `P3` 用于单位卡、且 `pool_effect_check` 第二套夹具靠它，
   扩义会**静默改掉那批既有读数**。已验：`pick=1` 对 `ASH73` **仍是改前读数**（打自己督军 28→26）。
   ⛔ 未动 `PinnedPlayMd5`、未动任何默认路径。
4. **`RuleEngine/Core/RuleCore.cs`**（14429 → 14438 行，**+9**）
   * `:3893` 传 `chosen`（行内改，零行数变化）· `ChosenTargetSlot` 上方那段「等价」注释就地订正（+1）
   * `:8849`（`Hurt`）与 `:3076`（疲劳）各插一段骷髅检查（+5 / +3）
5. **账补丁**：`资料/完全复刻/账/_parts/B9_E9ER.tsv`（6 行，11 列，键 `(原版类,方法)` 全库**唯一命中**已核）：
   `CheckIfCodexTriggered` B→**A** · `CheckForSkullEarned` C→**A** · `AddCardNotDrawnToHandAtIndex` B→**A** ·
   `OnQuestResolved` B→**A** · `IsValidSpellTarget` 保持 B（补两处说明）· `IsValidTarget` 保持 B（camouflage 已收窄，剩 IsInPlay/IsAlive 待核）。
   生成脚本留在 `_parts/_gen_B9_E9ER.py`（`.py` 不会被 merge 脚本当成补丁）。

⚠️ **行号漂移（要调度台在同步点跑一次）**：这两个文件各插入了几十行 ⇒ 下游所有 `账/*.tsv` 的 `文件:行号` 落点会漂。
请在「没有 `.cs` 写手 + 补丁已合并完」之后跑一次
`python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A --fix`（⛔ 别在别人写 `.cs` 时跑）。

## ⑤ 顺手发现（只报不改）

1. **派活单上的验证命令写错了**：`pool_effect_check.py --only` 收的是 **`chosenAbility` 数字**（`int(x)` 解析），
   `--only BL47` 会 `ValueError` 崩；照卡查要用 `--only 30`（BL47 的族号）或在输出表里 grep `BL47`。
2. **`cards_truth.json` 缺 `abilityTriggers` 列**（hero 行尤其明显：DA1 的 490 只在 `cards_engine.json` 里有）。
   任何按「卡上有没有某 trigger」判定的代码，**判据必须读 `cards_engine`** —— 读 `cards_truth` 会一律判「未知」。
3. **`ResolveTargets` 这一层根本不挡 stealth / camouflage / ward**（`AddSide` 的 `stealthGate` 默认 `false`，
   只有 `IsLegalPick` / `PlayTargetPool` 传 `true`）⇒ **结算侧比选目标侧宽**：
   凡「自动/随机挑目标」那一族（`Random=1` / `Count>1` / 触发正文）都可能**选中一个隐身的敌人**。
   与 `CanPlayTactic` 注释里点名的老毛病同族（「高亮说能选、打出去空过」的镜像版）。**本批没动**（会改全池指纹）。
4. **`PlayTacticCore` 的 `targetSlot` 有两重身份**（`EvtKind.Play` 的 `Slot` 与「效果打谁」）：单位卡回落时
   `ChosenTargetSlot` 找不到实例会出声退回 `fallbackSlot`，而战术卡的 `targetSlot` 是**效果目标**，
   两者语义不同 ⇒ 一旦 `chosen` 离场，事件里的 `Slot` 会带着「落点格号」而不是目标格号。本批没改（表现层在用）。
5. **`EffectText.PickTarget` 对 `Side=="any"` 返回 `null`**：连带三处表现层一起「看不见侧」
   （`PickSide` → `""`、`Hand/CardInteraction.TacticTargetSide` → `""`、`BattleDriver.SpellTargetBoardOwner` → `-1`）。
   本批把**引擎**改成以对象为准（不再需要侧），但那三处的注释仍写着「`any` 归我方」—— **措辞已过期**，
   建议 `CardPresentation` 写手顺手订正（⛔ 我没碰他们的文件）。
6. **`BL47` 那条「两条 logic 行逐字重复」的说法**（`P1` 报告 §4·1）不准确：两条**不完全相同** ——
   第二条多一个 `armourValueOptions: 201`（= Dark Pact 计数来源）。这不影响结论（都该给 +1 护甲），
   但说明「原版是靠第二条的 `valueOptions` 表达 `for each`」⇒ 我们若将来接 `armourValueOptions`，
   应按**结构化数据**判、而不是只靠正文正则。
