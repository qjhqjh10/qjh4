# B115_W14 · 拒支回滚（`SAN-23`）· `Tactic` 判据（`B-309`）· `ENG-16` 两跳

> 写手 = `B115_W14`。独占文件：`CardPresentation/Battle/BattleDriver.cs` · `RuleEngine/Core/RuleCore.cs` · `RuleEngine/Core/EffectResolver.cs`。
> 三个文件 **改前改后行数逐字相同**（`BattleDriver.cs` 40,288 · `RuleCore.cs` 18,265 · `EffectResolver.cs` 21,484），行尾未翻（`BattleDriver`/`EffectResolver` = CRLF、`RuleCore` = LF，两份数据见 §五）。

---

## 〇 · 一句话结论（三件活的状态）

| 活 | 状态 | 一句话 |
|---|---|---|
| `B-309` `Tactic` 位判据 | ✅ **已改** | `card.Type == "tactic"` → `card.CardType == 20`（原版卡型闸）。全池 **41 张补上、0 张丢失**；**结算层零变化**（指纹逐字节同基线），只有 `TacticsPlayedOf` 的读数口径变。⚠️ **会让 2 条既有断言变红**（夹具缺 `cardType`），补丁见 §六·①。 |
| `SAN-23` 引擎拒支回滚 | ✅ **已改** | 拒支分支 `return;` 之前补 `SyncBoard(_me,…)` / `SyncBoard(1-_me,…)`，**零行数变化**（追加在同一物理行、且在 `//` 之前）。 |
| `ENG-16` ②（`AbilityLevelGateApplies` + `AbilityLevelGateTarget` 加 `swarm`） | ✅ **已改** | 620 族接上 target 闸，`targetCard` = **留下的 host**。**全池零变化**（`play check` 逐卡一致）。 |
| `ENG-16` ①（`RuleCore.cs:7058` 补 `chosen: target`） | ⛔ **试过、已回退（有反证）** | 判据（原版确实传 `targetCard`）成立，但今天接不上：会让 `TL30` 的 8 次随机伤害**全变 0**。根因 = 触发正文的 op 拿不到 `ta` ⇒ 落进 `ResolveTargetsCore` 的 `ta < 0` 兜底被 `chosen` 顶掉。证据/两条候选修法见 §三。 |

---

## 一 · `B-309`：`Tactic` 位判据（`RuleCore.CardPlayedTagMask`）

### 改前 / 改后（同一物理行，`RuleCore.cs:3999`，零行数变化）

```csharp
// 改前
bool tactic = string.Equals(card.Type, "tactic", StringComparison.Ordinal);
// 改后
bool tactic = card.CardType == CardDef.CardTypeTactic;   // 判据 = 原版 CardPlayedCount__OnCardPlayed.c:24-29 的卡型闸
```

**判据（第一权威 = 全量反编译，逐句）**：`d:/2/tools/decomp_full/CardPlayedCount__OnCardPlayed.c:24-29`

```
if (!anyCardType(+0x60) && RawCard.cardType(+0x94) != attackingType(+0x68)) return;
```

`attackingType` 是 `CardTypeOptions`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardTypeOptions.cs`）= `Minion 0 / Hero 10 / **Tactic 20** / Whispers 40`；
`Ach_89` 的 SO 现读 `anyCardType = 0` + `attackingType = 20` ⇒ **原版判的就是 `cardType == 20`**。
我们的 `Type`（`unit/tactic/hero/defence/enchantment`）是**自己的映射**，不是原版字段。

### 影响面量化（`Resources/cards_engine.json` 现读 `count` = **1129**）

| 口径 | 张数 |
|---|---|
| `type == "tactic"`（改前的判据） | **446** |
| `cardType == 20`（改后的判据） | **487** |
| ⇒ **改后【多算】的** | **41**（= 487 − 446） |
| ⇒ **改后【少算】的** | **0**（446 张 `type=="tactic"` **全部** `cardType == 20`） |

**41 张的构成**（`type` × 可玩性）：

| 类别 | 张数 | 能不能被真正打出 | 现实影响 |
|---|---|---|---|
| `type == "defence"` | **39** | **能** —— 防御卡不进卡组（`inv=5`，`DeckRules.CannotAddToDeck` 走 defensive 路由），但由引擎发进**手牌**（`RuleCore.SetDefensiveCard` / `GrantDefenceCard`），打出时走 `PlayCardCore` → 非单位 ⇒ 分流 `PlayTactic`（`RuleCore.cs:4046`） | **原版 `LogPlayTactic` 就在这条打法路上**（`ChallengeLogMgr__LogPlayTactic` 的唯一调用者 = `_ResolvePlayCardFromHand` 的战术支）⇒ 改前我们**漏算这 39 张** |
| `type == "enchantment"` | **2**（`Reconnaissance Mission` / `Pray Enchantment`） | **不能** —— `CardDef.IsEnchantment` 的契约是「不进卡组/手牌/收藏」 | 0（结构上到不了 `CardPlayedTagMask`） |

<details><summary>41 张全表（id | 卡名 | type | cardType | spellType）</summary>

```
ASH55 Aspect Shrine | defence | 20 | 210      AM61 Defence Batteries | defence | 20 | 210
ASH56 Inspiring Legacy | defence | 20 | 210   AM62 Munitorum Supplies | defence | 20 | 210
ASH57 Webway Entrance | defence | 20 | 210    AM60 Timely Reinforcements | defence | 20 | 210
BL56 Throne of the Heretic | defence | 20 | 210    DA60 Defensive Turrets | defence | 20 | 210
DA59 Grim Effigy | defence | 20 | 210        DA61 Plasma Generator | defence | 20 | 210
EC59 Vial Tanks | defence | 20 | 210         EC60 Armoury of Excess | defence | 20 | 210
EC61 Decadent Throne | defence | 20 | 210    GSC63 Alien Idol | defence | 20 | 210
GSC65 Pilfered Supplies | defence | 20 | 210  GSC64 Rusted Vent | defence | 20 | 210
SAU60 Divination Menhir | defence | 20 | 210  SAU61 Resurrection Vault | defence | 20 | 210
GOF53 Extractor Rig | defence | 20 | 210     GOF49 Gretchin Tower | defence | 20 | 210
GOF52 Toxic Bonfire | defence | 20 | 210     SOR61 Guidance of the Saints | defence | 20 | 210
SOR60 Holy Fire | defence | 20 | 210         SOR59 Sacred Altar | defence | 20 | 210
SW60 Fenrisian Runestones | defence | 20 | 210  SW61 Halls of Legend | defence | 20 | 210
SW59 Orbital Defences | defence | 20 | 210    TAU64 Tidewall Droneport | defence | 20 | 210
TAU63 Tidewall Gunrig | defence | 20 | 210    TAU62 Watch Tower | defence | 20 | 210
TL54 Digestion Pool | defence | 20 | 210    TL56 Toxic Vapours | defence | 20 | 210
UM52 Firestrike Turrets | defence | 20 | 210  UM53 Hive Factory | defence | 20 | 210
UM54 Light Cover | defence | 20 | 210        BL57 Hellfire Torch | defence | 20 | 210
BL58 Hellfire Pit | defence | 20 | 210       SAU59 Awakened Obelisk | defence | 20 | 210
TL55 Protective Bio-structure | defence | 20 | 210
e8cf1eb55ca41534abe38efc90b302e9 Reconnaissance Mission | enchantment | 20 | 0
9218576fd3e26554db5b4c239a36dbc7 Pray Enchantment | enchantment | 20 | 0
```
</details>

**结论：这是「修偏差」，不是「引入偏差」** —— 唯一的反向风险（`type=="tactic"` 却 `cardType≠20`）**现池 0 张**；41 张里 39 张是**原版按打法算战术、我们漏算**的防御卡，另 2 张结构上打不出去。

**不受影响的三处（逐条核过）**：
1. `Ach_89`（`ElixirTactic`）：`elixir ∧ tactic` 的 6 张 CombatElixir `type/cardType` 双满足 ⇒ 改前改后同一格；且 `ProfileData.cs:745` 早就按 `attackingType == CardTypetactic` 判（`B115_W9` 那笔）。
2. 生产侧唯一读数口 `AchievementTally.TacticsPlayedOf(seat)` **今天没有任何生产消费者**（`grep` 只命中定义 + 自检）⇒ 玩家端零可见变化。
3. 结算层：`play check` **逐卡逐字节同基线**（指纹 `cdf608b5…`，见 §五）。

---

## 二 · `SAN-23`：引擎拒支没有回滚

### 改前 / 改后（`BattleDriver.cs:14090`，**同一物理行**，零行数变化、且在 `//` 之前）

```csharp
// 改前
if (ReferenceEquals(_fieldDissolvedCard, card)) UndissolveFieldCard(card);
return;
// 改后（同一行尾部追加两句）
if (ReferenceEquals(_fieldDissolvedCard, card)) UndissolveFieldCard(card); SyncBoard(_me, _myUnits, true); SyncBoard(1 - _me, _foeUnits, false);   // 说明见行内注释
return;
```

### 为什么是这两句（而不是「补一行撤预览」）

- `CardInteraction.Release`（`Hand/CardInteraction.cs:1381-1390`）现在**先**算 `DropAccepted = 合法 ∧ 单位卡`，**再** `ClearDropIndicator()`；
  而 `EndPreviewReturn`（`BattleDriver.cs:27083-27098`）在 `DropAccepted == true` 那一档**只清账、不送回**（「放下去 ⇒ 引擎马上会把它插进去」）。
- ⇒ 「判据层说合法、引擎却拒」这一档：`_previewMoved` 已被清空，**没有任何一处把被推开的单位摆回真格位** ⇒ 停在预览的假格位上。
  这正是 `SAN-23` 记的症状；`CardInteraction.cs:1396-1398` 已清 `_previewMoved` ⇒ 「补一行撤预览」确实是空转（前人判断正确）。
- `SyncBoard` 的末尾就是 `v.SetPose(pose, 0f, poseScale)`（`BattleDriver.cs:26799` 附近）—— 按**棋盘真值**重摆，天然就是回滚；
  写法照 `:19440-19441` / `:4530-4531` / `:20837-20838` 三处既有调用点，**逐字同形**。
- 引擎既然拒了，棋盘一个字节都没变 ⇒ 这次 `SyncBoard` 的净效果**只有位姿回正**（不会重建/销毁任何视图）。

### 验证

`BattleDriver.cs` 不在 `ruleprobe` 的编译面（探针只编 `RuleEngine/Core`）⇒ 只能靠宿主自检；无新增断言（断言宿主 `Editor/BattleScene.cs` 归 `B115_W4`/`B115_W7B`，不在本笔白名单）。**要跑的宿主见 §五**。

---

## 三 · `ENG-16`：两跳

### ② 已改：620 `TriggeredSwarm` 接上 ability 级 target 闸（`EffectResolver.cs:7826 / 7840` + `RuleCore.cs` 两处广播）

**判据（第一权威，逐句；与 `资料/普查产出_1025/E6_十三行接闸.md:153-156` 一致）**：

```
CardScript.<>c__DisplayClass401_0.<ExecuteSwarm>b__0:45/51
  → BroadcastUnitSwarm(bm, param_2 = 被销毁那张, param_3 = 留下的那张)
  → CardScript__TriggeredSwarm(observer, param_2, param_3)
  → OnTrigger(0x26c /*620*/, …, actingCard = param_2, targetCard = param_3)
```
⇒ `just` = `actingCard`、**`host`（留下的）= `targetCard`**。

**改法（三处，全部零行数变化）**：

| # | 位置 | 改前 | 改后 |
|---|---|---|---|
| ① | `EffectResolver.cs:7826` | `return false;` | `return kind == WhenEventKind.Triggers(KeywordTable.Swarm);`（⛔ 不能写成 `switch` 的 `case` label —— `Triggers(kw)` 是**函数**） |
| ② | `EffectResolver.cs:7840` | `if (kind == WhenEventKind.Attack) return target;` | `if (kind == WhenEventKind.Attack \|\| kind == WhenEventKind.Triggers(KeywordTable.Swarm)) return target;` |
| ③ | `RuleCore.cs:12977`（`FireTriggerAt` 尾部广播）与 `:13091`（`FireTriggerAlways` 的「没正文」支） | `BroadcastWhen(ctx, Triggers(keyword), owner, u.Card, subject ?? u, …)` | 加 `target: keyword == KeywordTable.Swarm ? u : null` |

**③ 为什么必须改两处、且为什么把 host 塞进 `target:`**：`AbilityLevelGateTarget` 只拿得到 `(subject, target)` 两个槽；
620 的 `subject` 是**被并掉那张**（不能拿来筛 —— 那是原版 `actingCard`，筛错人），所以 host 只能走 `target:` 这一格。
两条广播路（载体有正文 → `FireTriggerAt` 尾部；没正文 → `FireTriggerAlways` 自己发）**必须同改**（只改一支 = 「两处写同一条规则」）。
其余关键词**维持 `null`**（它们的 `targetCard` 是哪一格没查清 ⇒ ⛔ 不猜）。

**安全边界（现读全池 1129 张）**：`abilityTriggers` 含 **620** 的只有 **2 张** ——
`TL15 Tyranid Prime`（有 1 行 `abilityLevelCriteria`）· `TL75 Termagant Brood`（0 行）。
`TL15` 那一行是 `{trigger 620, ca "buff", targetsAffected 10, friendlyUnits 1, includeMinions 1}` ⇒ `targetCard = host`（永远是自己方的合法部队）⇒ **闸必然放行**；
`TL75` 没有行 ⇒ 「明确没有条件」放行。⇒ **`play check` 全池逐卡一致（0 变化）**，见 §五。

**仍未接的那一半（如实记，别当成做了）**：`E6` 记「620 真正的缺口在**载体自己的入队口**」（`FireTriggerDeferred` 的 `EnqueueAbility(abilityTarget: chosen)`，swarm 那一路 `chosen` 恒 `null`）。
本笔接的是**监听者广播那一侧**（`BroadcastWhen` → `AbilityLevelGateTarget`），**载体那一侧仍未接** —— 而它今天也接不了，原因与 ① 完全相同（见下）。

### ① 试过、**已回退**：`RuleCore.cs:7058` 补 `chosen: target`

**判据成立**：原版 `CardScript__ResolveUnitAttacked.c:99-101` 亲读

```
RawCardScript__OnTrigger(*(raw), 0x244 /*580 = Strike*/, thisCard, param_1 /*actingCard = 攻方*/,
                         param_2 /*targetCard = 被打者*/, param_3, 0);
```
⇒ `chosen`（= 我们的 `targetCard`）**确实应该传被打者**，同族样板 = 本文件 `:8249` 的 `AboutToAttack`。

**实测反证（`bash Unity/工具/ruleprobe.sh play check`）**：接上后事件电池 **3 张卡变化**

| 卡 | 变化 | 判读 |
|---|---|---|
| `TAU18 Fire Warrior Marksman`（`Strike: If the target survives, give it Markerlight 2`） | `netFull` 里多出 `markerlight2` | **像修好**（`it` = targetCard） |
| `GSC17 Locus`（`Strike: Stun a random enemy`） | `raw 50→49 · net 2→3`，`{stun1}` 位置变 | 待判（可能是修好） |
| `TL30 Tyrannofex`（`Strike: Deal 1 damage to a random enemy, 8 times`） | **`netDmg 25 → 0`（8 次随机伤害全部消失）** | 🔴 **明确回归** |

**根因（定位到行）**：
- 触发正文的 op **拿不到 `ta`**（`EffectResolver.cs` / `CardDef.AssignAbilityTargets` 这一层没给它挂上；`ruleprobe scan` 的 ext 块里 `subjTa` 一次都没打出来 ⇒ 现池这族值的可观测性也不足）；
- `ResolveTargetsCore` 的兜底把 `ta < 0`（= 未知）当作「调用方点了目标」：

```csharp
// EffectResolver.cs:1966 附近的兜底（今天必须是这个形状 —— 见下）
if (chosen != null && (op == null || op.SubjectTargetsAffected < 0 || op.SubjectTargetsAffected == 30 || op.SubjectTargetsAffected == 220)
    && spec.Count == 1 && list.Count > 0 && !list.Contains(chosen))
{ list.Clear(); list.Add(chosen); }        // ← 「a random enemy」被整表换成 chosen（= 已被打死/正在离场的那一张）
```
原版 `AbilityLogic__GetTargets.c:192-207` 里**只有 `ta = 30(target)` / `220` 两支**读 `param_5`，`ta = 10` 的「随机敌人」**必须忽略 `targetCard`**。

**为什么没有顺手把兜底的 `< 0` 去掉**（实测过）：去掉后 `play check` **主档 3 张 / 电池 18 张**全变（战术、`EC42` 这类出牌正文全靠这条兜底）⇒ 那是**另一笔**、需要自己的判据，⛔ 不在本笔范围。

**两条候选修法（给调度台裁，均不在本笔白名单）**：
1. **数据层**：让「触发正文」这条路的 op 也吃到 `abilityTargets[].ta`（`TAU18` 的 SO 里明写 `ta = 30`、`TL30` 是 `10`）⇒ 兜底不再命中，`chosen` 传下去就与 `TAU18` 一起变对。落点 = `CardDef.AssignAbilityTargets` 那一族。
2. **兜底层**：把 `ta < 0` 从「用 chosen 顶」降级为「判不了 ⇒ 出声 + 维持原目标表」，但它今天覆盖 21 张卡 ⇒ 要单独立账、单独立断言。

**代码里留了痕迹**：`RuleCore.cs:7058` 那行现在是原始调用 + 一段 `⛔ 试过、已回退` 的注释（写清反证与两条候选修法），免得下一个会话再踩一次。

---

## 四 · 顺手发现（⛔ 未改，超出白名单 / 需要裁定）

1. 🔴 **`TL15 Tyranid Prime` 的 buff 目标大概率接错了，且旧注释已过期** ——
   `cards_engine.json` 现读 `abilityTargets[620] = ta:[30]`（= 原版 `targetCard` = **留下的 host**），
   而 `FireBoardListenerDeferred` 把 **`subject`**（= 被并掉那张 `just`）当 `chosen` 传给 `ResolveOps`
   ⇒ 「give it +1 Melee and +1 Health」落在**已经离场的那张**上。
   `RuleCore.cs` 里那段长注释说「原版那张卡的能力数据本地取不到 ⇒ 必然结果」——**这句话已过期**：数据就在 `cards_engine.json` 的 `abilityTargets` 里。
   落点：`FireBoardListenerDeferred`（`EffectResolver.cs`）+ `abilityTargets` 的 ta 接线；属于同一族「ta 映射」缺口（与 §三 ① 同根）。
2. **`AchievementTally.cs:457` 的 doc 与本笔改动不一致了**（文件属 `B115_W9`，本笔没动）：
   原文「对所有 `type == "tactic"` 的卡置 `CardPlayedTag.Tactic` 位（`RuleCore.cs:3883`）」——
   既**口径过期**（现在是 `cardType == 20`），**行号也早漂**（现读 `RuleCore.cs:3999/4001`）。
   建议改成：「对所有 `cardType == 20`（`CardTypeOptions.Tactic`）的卡置位（`RuleCore.cs:3999`，判据 `CardPlayedCount__OnCardPlayed.c:24-29`）」。
3. **`RuleCore.cs:4006` 的 `Pack` 位注释自陈「按意图、不是按原版那道闸」** —— `B115_W9` 的 §五 已把 `Ach_97` 的真口径改成「`anyTrait == 1` ⇒ 每张牌都算」（`ProfileData.cs:747` 的 `AnyCard`），
   这一句注释现在是**第三种说法**，建议同批订正（本笔没动：改它要连带 `CardPlayedTag.Pack` 的生产/消费两处语义）。
4. **`E6_十三行接闸.md:157` 那句「620 真正的缺口在入队口」仍然成立** —— 本笔只补了监听者那一侧（见 §三 ②末）。

---

## 五 · 读数（本节按顺序可复跑；⛔ 没有 `--fix`、没有动账主表）

| 闸 | 命令 | 读数 |
|---|---|---|
| 类型检查 | `TMPDIR=/tmp/wf_w14 bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| 账落点 | `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py` | **A 5831 · 通过 5831 · 漂移 0 · 失败 0**（告警 864 = 既有） |
| 聚合行 | `python -I -X utf8 d:/4/Unity/工具/check_aggregate_lines.py <三个文件>` | **聚合行 0 · 违规 0 · PASS** |
| 解析层 | `bash d:/4/Unity/工具/ruleprobe.sh check` | 解析差异 **0 行** · 哨兵失败 **0** · `rc=0` |
| 结算层 | `bash d:/4/Unity/工具/ruleprobe.sh play check` | 指纹 `cdf608b5da9c079e96d59e70fe549c19` **= 基线**；主档 1129 张、电池 1073 张**逐字节一致** |

**行尾/行数守恒（二进制读）**

```
RuleCore.cs       lines 18265  crlf 0      bareCR 0      （LF）
EffectResolver.cs lines 21484  crlf 21484  bareCR 0      （CRLF）
BattleDriver.cs   lines 40288  crlf 40288  bareCR 0      （CRLF）
```
改前改后**三个数完全一致** ⇒ 账的行号锚点一个都没漂（也确实体现在「漂移 0」上）。

### 要跑的宿主 / 探针，与预期

| 宿主/探针 | 为什么 | 预期 |
|---|---|---|
| `RuleEngineTest.Run` | `B-309` 改了 `Tactic` 位 | ⚠️ **预期 2 条红**：`RuleEngineTest_EngineE5.cs:624`（`TacticsPlayedOf(0) == 1`）与 `RuleEngineTest_EngineX2.cs:208`（`CardsPlayedOf(0, Tactic) == 1`）—— 两条夹具都走 `RuleEngineTest.Tactic(...)`（`RuleEngine/Editor/RuleEngineTest.cs:2623`）而**没挂 `cardType`**。**修法见 §六·①**（不是回归、是夹具不全；`E5:617/618/635` 三条仍绿）。 |
| `RuleEngineTest.Run`（附属） | 620 闸新接 | 现池 620 的可达听众只有 `TL15`/`TL75`，两条都放行 ⇒ **其余断言应无变化**（`play check` 已证全池 0 变化）。 |
| `BattleScene.Run` | `SAN-23` 改的是 `BattleDriver` | 应有 0 条新红；**这条活没有断言**，要靠**真 Play**：拖一张**判据层合法但引擎会拒**的牌（例如能量在拖拽途中被效果扣光）⇒ 被推开的单位**必须回弹到真格位**，⛔ 不许停在预览位。 |
| 真 Play（`wf_click.py`） | 同上 + `TacticsPlayedOf` 无 UI 面 | 只有 `SAN-23` 需要点；`B-309`/`ENG-16` 在玩家端**零可见变化**。 |

---

## 六 · 交给调度台 / 别的写手的补丁（本笔**没动**这些文件）

### ① 夹具补 `cardType`（文件属 `B115_W13`：`RuleEngine/Editor/RuleEngineTest*.cs`）

`RuleEngineTest.cs:2623` 的 `Tactic(...)` 工厂 —— **同一物理行**改成：

```csharp
static CardDef Tactic(string name, int cost, string desc)
{
    var c = new CardDef(name, name, "tactic", desc, "common", "Test", cost, 0, 0, 0, null);
    c.AttachCardType(CardDef.CardTypeTactic);      // 🆕 2026-11-01（B115_W14 · B-309）：生产池里每张 tactic 卡都是 cardType 20（现读 446/446）；判据 CardPlayedCount__OnCardPlayed.c:24-29
    return c;
}
```

（`Tactic` 只有一个定义、`RuleEngineTest_EngineE5.cs` 与 `CardPresentation/Editor/RuleEngineTest_EngineX2.cs` 是**同一个 partial 类**、共用它 ⇒ 改一处两条断言同时回绿。）

### ② `AchievementTally.cs:457` 的 doc（文件属 `B115_W9`）

见 §四·2 的替换文本。

---

## 七 · 账 / 活状态（⛔ 本笔只报告，主表由调度台改）

| ID | 建议状态 | 依据 |
|---|---|---|
| `B-309` | **✅ 可核销** | `RuleCore.cs:3999` 已按 `cardType == 20`；量化 41/0；`play check` 零变化；**唯一尾巴** = §六·① 那一条夹具补丁（属 `W13`） |
| `SAN-23` | **🟠 已改待真机验** | 代码已接（零行数变化），断言宿主不在白名单 ⇒ 需要 1 次真 Play（§五） |
| `ENG-16` · ② | **✅ 可核销（监听者侧）**，但**载体入队口仍未接**（`E6` 记的那半）—— 建议拆成两行 | 见 §三 ②末 |
| `ENG-16` · ① | **⛔ 判据成立、实现受阻**（不是「判据本身错」） | 卡在「触发正文 op 没有 `ta`」；候选修法两条见 §三 |
