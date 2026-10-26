# B8_AJ —— 9 条账行「我们侧落点」重定位报告

> 代理：判据代理 `B8_AJ` · 2026-10-26
> 输入：调度台把 9 条被机器机械回溯到**无关方法**的 A 档行回退成 B，要求逐条按语义重找我们侧方法级落点。
> 产出补丁：`Unity/资料/完全复刻/账/_parts/B8_AJ.tsv`（9 行 · 11 列 · LF · 表头同主表）
> ⛔ 本轮**未改任何 `.cs`**、未动账主表、未碰 git、未跑 Unity/批处理。纯账补丁 ⇒ 无需 typecheck。

---

## 一、9 条逐条结论

| # | 原版 类.方法 | 我们侧真实落点（行号已核） | 状态 | 一句话理由 |
|---|---|---|---|---|
| 1 | `CardScript.AttackMeleeAnim` | `CardFeel.cs:1104-1135`（`MeleeAttack`）· `CardFeel.cs:2260-2291`（`AttackRecover`＝收招）· `BattleDriver.cs:21390-21518`（`ScheduleAttackPresentation`；命中帧 = `:21467` 的 `ScheduleAction(HitFrameLag)`） | **A** | 旧落点 `1104-1104` 只是方法体内单行；三段（序列 / 命中帧 / 收招）现在各自指到方法声明，且 5 段 + 3 闭包可逐句对上 |
| 2 | `CardScript.GetEffectAnchor` | `CardView.cs:2115-2117`（`EffectAnchor(bool)`）· `:2091`（`EffectAnchorHand`=FaceRoot）· `:2097-2110`（`EffectAnchorBoard`）· 选支 = `AnimAnchors.cs:275-283` + `BattleDriver.cs:3960-3965` | **A** | 旧落点 `EffectsAnchorHeight` 只是「锚点到底边」的标量，不是这个二选一；真等价物是一对锚点 + 一个选支方法（我们**两支都有**，旧文案「恒取特效锚点」写错了，已改对） |
| 3 | `MinionManager.BroadcastActivateMinion` | `RuleCore.cs:2052-2056`（`BeginTurnCore:1680` 里的逐格圈，**非独立方法**）· `UnitState.cs:1732-1774`（`RefreshForNewTurn`）· `RuleCore.cs:3493-3502`（`RecomputeActAndAttack` ＝ 原版 `ActivateMinion` 整份体） | **B** | 落点对了，但**观测量只有自检断言**（G3 明令不算、且自检暂停），离线探针不输出 `Exhausted/AttacksThisTurn` ⇒ 拿不到我们侧实测值 |
| 4 | `PlayerHand.LightPlayableHand` | `BattleDriver.cs:26819-26843`（`RefreshHandPlayable`）· 驱动点 `:26246`（`SyncHand` 内 ← `RefreshAll:18451`/`SyncHand:26158`） | **A** | 旧落点指到 `ReapplyFoeHandPoses`（敌方卡背摆位），完全无关；真等价物是这一个函数，差量三条已写清 |
| 5 | `PlayerHand.RemoveCardFromHand` | `RuleCore.cs:3908`（`PlayCardCore` 内**一条语句**）· 视图复位 = `BattleDriver.cs:26158-26262`（`SyncHand` 整段） | **B** | 原版一个方法做两件事，我们这两半分居两层、**各自都不是独立方法** ⇒ 只能结构性等价 |
| 6 | `PlayerHand.StopLightPlayableHand` | `BattleDriver.cs:26819-26843`（`RefreshHandPlayable` 的 `myTurn==false` 极，与 #4 **共用落点**） | **A** | 原版是全灭入口，我们是同一纯判据的另一极；共用落点在差异列写明了各满足哪一极 |
| 7 | `BattleManager.ResolveRecallToDeck` | `EffectResolver.cs:6409-6584`（`DoReturn`）· `:6621-6691`（`ReturnFromCemetery`）· `:4604-4932`（`DoChooseCard` 的 `case "return"` :4862-4871 / `case "shuffle"` :4873-4882 = **手牌来路**） | **A** | 旧落点起点 `6383` 在 `DoReturn` 的 doc 注释里；真声明 `6409`。原版「三档来路一个方法」我们拆成「目标区 → 三个口」，三条都在上面 |
| 8 | `BattleManager.ResolveRecallToHand` | `EffectResolver.cs:6409-6584`（`DoReturn`；在场来路 `:6543-6546` `case "hand"`）· `:6621-6691`（`ReturnFromCemetery`；墓地来路 `:6673-6677`） | **A** | 同上（旧落点起点在注释里）；手牌上限那一支仍为静态未跑，已在备注保留 |
| 9 | `CardHighlight.SetCombatHighlights` | `CardView.cs:8421-8449`（`SetWillDie(bool)`）· 生产调用点 `BattleDriver.cs:15523-15529` / `:15486-15509` | **A** | 旧落点 `8416` 是 `WillDieAnimTime` 常量附近的行、不是声明；判据出处也只写了 `CardHighlight.cs:90`（无来源无行号），已补 `decomp_full/…:1-12（🥇）` |

**合计：7 A / 2 B。**

---

## 二、判「只能 B」的两条及原因

### #3 `MinionManager.BroadcastActivateMinion` —— 落点已对，缺**可观察量**
- 我们侧正确链路已定位：`BeginTurnCore` 的逐格圈 → `UnitState.RefreshForNewTurn` → `RuleCore.RecomputeActAndAttack`（后者就是原版 `CardScript.ActivateMinion` 的整份体，两个反向位一起写）。
- 卡在 **G3**：账上唯一写的观测量是「`RuleEngineTest` 的疲劳/召唤病/眩晕那族断言」——G3 原文「『断言绿了』不算」，且自检目前**暂停**；`ruleprobe play` 的 dump **不输出** `Exhausted` / `AttacksThisTurn`（`工具/ruleprobe/PlayProbe.cs` 里对这两个字段只有「⛔ 探针不手改」的注释，没有输出列）。
- 要升 A，需要**新增一个探针字段/输出**（`BattleContext` 侧或 `PlayProbe` 的 dump 列），这超出「只写账补丁」的白名单 ⇒ 留给后续。
- 顺带订正：本行旧差异文案「我们只存 `Exhausted`、`canAttack` 由 `CanAttackNow` 现算 ⇒ 无存储位可刷」**已过期** —— `CannotAttackThisTurn` 就是 `canAttack` 的反向位，由 `RecomputeActAndAttack` 一起写。

### #5 `PlayerHand.RemoveCardFromHand` —— 无方法级落点
- 原版一个方法做两件事：`currentHand.Remove(card)` + `ToggleSimplifiedCardView(false)` + 起协程 `MoveCardsInHandToPosition`。
- 我们这两半**分居两层**：
  - 摘手牌 = `PlayCardCore` 里**一条语句** `ps.Hand.RemoveAt(handIdx);`（`RuleCore.cs:3908`，它是 328 行的出牌大方法中的一行，不构成「做这件事的方法」）；
  - 视图复位 = 表现层 `SyncHand` 的**整段重排**（`BattleDriver.cs:26158-26262`）。
- 两半都没有独立方法 ⇒ 按方案 §4.1「落点对不上方法级 ⇒ 只能 B」处理。补丁里把两处位置都写进了「我们的落点」，并在备注写明「非方法级」。

---

## 三、顺手发现（供调度台分流，⛔ 本轮未改）

1. **（真缺陷，建议立账）`RefreshHandPlayable` 对战术卡没有「合法目标」判据** ——
   原版 `BattleManager__CanPlayCard.c:141` 会调 `BattleManager__IsValidSpellTarget` 遍历候选目标，一个都没有就返回 0（不点亮）。
   我们：`bool playable = myTurn && card.Cost >= 0 && card.Cost <= p.Energy && (card.IsUnit ? hasSlot : true);`
   ⇒ **没有合法目标的战术卡/防御卡，我们也会给绿描边**（原版不亮）。这在 #4/#6 的差异列里已如实写下。
2. **（口径坑，已就地改掉）`RefreshHandPlayable` 不是「每帧重算」** ——
   它只被 `SyncHand`（`:26246`）调一次，而 `SyncHand` 只在 `RefreshAll`（`:18485`）与「取消选目标」（`:7901`）里跑。
   旧账文案「我们靠每帧重算覆盖」不成立，已在补丁里改成「`RefreshAll → SyncHand` 驱动」。
   ⚠️ 未验的怀疑：若某条状态变化路径**没有**走 `RefreshAll`，手牌高亮会停在旧值（未实测，只记录）。
3. **（记忆点）命中帧那一块的第一行是注释**：`BattleDriver.cs:21460` 是「② 命中帧那一拍」注释，真正的 `ScheduleAction` 在 `:21467` ——
   与本轮被回退的 7 条同因（机械回溯常落到注释/常量/单语句行）。判落点时**必须看那一行是不是声明**。
4. **（判据出处缺行号，已补）** #7/#8 原来只写 `…MoveNext.c（逐段：…）`，没有行号；本轮补了协程里 6～9 处调用点的确切行号（如 `ResetToValuesInHand:108/177/276`、`MoveToDeck:352`、`TransformUnitInPlayToCard:273`、`AddCardNotDrawnToHand:55`）。
5. **（未核，留指针）** 行 9 的「上游点亮那一跳」我只核了 `SetCombatHighlights` 全文；`CardHighlight__ToggleCombatPreviewHighlight.c` 的确切行号**未逐条核**（补丁里已明写「上游行号未核」）。

---

## 四、本轮复核做过的动作（可复算）

- 每条都按**符号**（方法名 / 成员名 / 字符串字面量）在 `Unity/MyGame/Assets` 下搜索，再**打开声明处逐句读**；落点起止用花括号配对算出（避免只给单行）。
- 判据侧：`D:/2/tools/decomp_full/`（🥇）逐条查行号——`MinionManager__BroadcastActivateMinion.c:2-47`、`CardHighlight__SetCombatHighlights.c:1-12`、`PlayerHand__RemoveCardFromHand.c:1-24`、`PlayerHand__StopLightPlayableHand.c:1-29`、`PlayerHand__LightPlayableHand.c:1-47`、两份 recall 协程（365 / 309 行）、`BattleManager__CanPlayCard.c:141`。
- 补丁自检：`CR=0`、`LF=10`（1 表头 + 9 行）、每行 `10 个 tab`（11 字段）、表头与主表逐字相同。
