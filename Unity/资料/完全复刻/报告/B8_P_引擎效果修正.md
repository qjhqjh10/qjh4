# B8_P · 引擎效果修正（批次 8 · 执行代理 P）

> 2026-10-26 · 白名单：`RuleEngine/Core/EffectResolver.cs` · `RuleEngine/Core/RuleCore.cs` · `RuleEngine/Data/AiScoring.cs`
> 账补丁：`账/_parts/B8_P.tsv`（5 行：`AI.ScoreFromBuffedAttack` · `AbilityLogic.AdjustManaEffect` · `AbilityLogic.GetTargets`(CardScript 重载) · `BattleManager.GetAdjacentUnits` · `EntityScript.EnemyReturnsAttack`）
> 门禁：`typecheck` **0 / 0** · `ruleprobe check` **解析差异 0 行 · 哨兵 0 条** · `play check` **rc=1（预期）**

---

## ① 结论（五条各修成什么样 + 新指纹）

| # | 条目 | 修成什么样 | 观测（改前 → 改后） |
| --- | --- | --- | --- |
| 1 | **`GOF97` 把自己眩晕** | **已修**。锚点不再落回「这条能力的主语」：`PreviousTarget` 在没有 `chosen`、且 `ctx.LastTarget` 只是能力入口种下的 `source` 时，按**目标池第一个**当锚点（原版 `ta=110` 的锚点是**这条 ability 的 target**，`param_5 == null` ⇒ 候选为空）。另加一道「锚点本体也要过阵营筛」（`AnchorInSet` 不再无条件入集） | `play GOF97`：改前 `锚点 Gargantuan Squiggoth（自己）→ 1 个目标：Gargantuan Squiggoth`（**眩晕自己**）；改后 `锚点 FIX_E1（1 方 3 号格）→ 2 个目标：FIX_E1、FIXWarlord_Goff` |
| 2 | **`ASH73` 的「相邻」打到自家督军** | **不改实现（原报告的前提不成立）**，判据见 §②·2：原版这一行的 `ta=110` criteria 是 `friendlyUnits=1 enemyUnits=1 includeHero=1 includeMinions=1 includeRemnants=1 maxTargets=2` ⇒ **友方与督军本来就在目标集里**。读数 `P0 WARLORD 28→26` 的成因是夹具给「Deal 5 damage to a troop」（卡面**不带** `enemy`、Side=any）自动挑了**我方** `FIX_A`，它的邻居正是自家督军 ⇒ 这正是原版行为。**同族真缺陷（锚点落错边）已在 #1 一并修掉**（`SOR43` / `TL27` / `UM105` 与 GOF97 同形） | 改后 `play ASH73` 读数**逐字不变**（`WARLORD 28→26`），并用 `play SOR43` 佐证锚点已落敌方：`锚点 FIX_E1（1 方 3 号格）→ 2 个目标：FIX_E1、FIXWarlord_Sororitas` |
| 3 | **相邻目标的邻居多要一道 `IsAlive`** | **已修**。邻居那一跳不再看死活（原版 `MinionManager__GetAdjacentUnits.c:52-70` 只在那一侧的**部队列表**里按引用找锚点取 `i±1`，死亡单位要等 `MoveToCemetery` 才离开列表）。⚠️ 只删 `IsAlive` 不够 —— `pool` 是 `AddSide` 建的（**活的**），死邻居仍会被 `pool.Contains` 挡掉 ⇒ 新增 `AdjacentNeighborCounts`：活着的走老路，**刚死的**按**同一套筛**（阵营 ＋ `troopOnly` ＋ `ApplyTargetFilters` ＋ `ApplyTargetCriteriaFilter`）单独判一次 | `play check` ①-b 电池 **5 张卡**变化，其中两张纯由这一条引起：`GOF67`（Mob: Give +1 Health to adjacent units）`net 1 → 3`，多出的那条 = `✎ P0[3] FIX_A 2/0/0/-997 → FIX_A 2/0/0/-996`（`-997` = **待死哨兵**）⇒ **待死窗口里的邻居现在真吃到 +1 Health**；`ASH16`（Strike: Give Invulnerable to adjacent troops）同形 `net 2 → 4` |
| 4 | **`AdjustManaEffect`（C → A）** | **已修**。阵营资源（`gainfaith/gainspirit/gainquest/gainenergy`）的乘数**乘进 `Amount`**、只结算 **1 遍**（判据 `AbilityLogic__AdjustManaEffect.c:5-7`：`amount(+0x10) *= multiplier`，随后 `AddManaBuff` 只建**一条** `BattleAction(0x22)`）。`…, N times`（`RepeatTimes > 1`）**不折**（另一条判据，仍走原循环）。出牌路与触发路**共用** `FoldManaMultiplier` | `play SOR49`（`fm=[3,0]`）：改前**三行**「信仰 +1（现 13/14/15）」；改后**一行**「信仰 +3（现 15）」。两侧净增量都是 **12→15**（数值同、**事件/广播次数 3→1**）⇒ `SOR38` 的「When you gain Faith」改前响 3 次、改后 1 次 |
| 5 | **`shuriken(1120)` 免反击通道接线** | **已接**（一行）：`AiScoring.cs:582` 改调 `CounterAttack.EnemyReturnsAttack`（预测五道）。⛔ 没有动 `NoCounter`（实际结算仍是四道 —— 原版这两条路**本来就不一致**）。⚠️ **状态仍留 B**：这一条**没有离线机器读数口**（见 §④） | 代码级：`BestAttackScore`（option 5 带 buff 那条入口）现在走五道；`SimpleAI.cs:604` 那条入口仍四道 |

**`play check` 新结算指纹 = `350c16b2761e9a662b1f29ebaf69500e`**（旧 = `a93ffb8f344517a9ac611ef06de17a3a`）。
⚠️ **没有**自行改 `PinnedPlayMd5`（按简报要求，交调度台统一重钉）。
变化面：①-a 主档 **2 张**（`GOF97` / `TL27`）· ①-b 电池 **5 张**（`ASH16` / `GOF67` / `GOF97` / `TL27` / `UM105`）· 新增 0 · 消失 0。

---

## ② 逐条证据

### 2·1 任务 1（`GOF97` 晕自己）：锚点为什是施放者

- 解析产物（`ruleprobe seg "Stun an enemy and adjacent units"`）：
  `verb=stun … T={Raw=an enemy and adjacent units | Side=enemy | Kind=unit | Count=1 | Adjacent=1 | Anchor=PreviousTarget | AnchorInSet=1 | AdjacentAll=1}`
- 改前（旧行号 `:1712`）：`var anchor = ResolveAdjacentAnchor(...)` → `ResolveAdjacentAnchor`（`:1219-1245`）的 `PreviousTarget` 支返回 `chosen ?? ctx.LastTarget`；
  而 `ResolveOps`（能力层入口，`:6972-6995`）的 `prime = seed != null ? seed : source` 会把 **`ctx.LastTarget = source`（这条能力的主语）** 种进去 ⇒ Rally 时 `chosen == null` ⇒ **锚点 = 施放者自己**。
- 原版判据（🥇 `decomp_full/AbilityLogic__GetTargets.c:1046-1057`）：
  `if (*(int *)(param_2 + 0x10) == 0x6e) { cVar3 = UnityEngine_Object__op_Inequality(param_5,0,0); if (cVar3 != '\0') { uVar8 = BattleManager__GetAdjacentUnits(param_1,param_5,0); … } }`
  —— `0x6e = 110`，锚点 = **`param_5` = 这条 ability 的 target**；`param_5 == null` 时**连候选都不填**。
  同函数 `ta=105`（`0x69`）才是「上一批目标的首个，**否则** `param_3`（拥有者）」—— 而**卡池里 `ta=105` 一行都没有**（技能行现读：110: 14 · 100: 17 · 113: 3 · 106: 1 · **105: 0**）⇒ 我们解析层的 `PreviousTarget` 实际对应的全是原版 **110**。
- 落点：`EffectResolver.cs:1716`（改调 `ResolveAdjacentAnchorFromPool`）＋ `15877-15893`（新方法）＋ `1727`（`AnchorInSet` 过阵营筛，判据 `AbilityLogic__GetTargets.c:1100-1110` 逐候选 `CheckIfMeetsCriteria`）。
- 卡面（`cards_engine.json` `desc` / `descZh`）：`Rally: Stun an enemy and adjacent units` / 「[[rally]]：[[stun]]一名敌人及其相邻单位」⇒ **锚点必须是那个敌人**，卡面不打 `*`（解析是成功的）⇒ 改前属**静默错打**。
- 结构数据佐证：`GOF97.ab=1` 两行 —— `lg=0 ta=30`（target，F0E0）+ `lg=1 **ta=110** E1 h1 m1 r1`（相邻那圈**只含敌方 + 督军 + 残骸**）⇒ 我们 `Side=enemy` 的筛法与数据一致。

### 2·2 任务 2（`ASH73`）：原报告前提不成立

- `ASH73.abilityTargetCriteria` 现读：`ab=0/lg=0 ta=30 (F0 E0)`（第一句的 target，**没有**阵营筛）· `ab=0/lg=1 ta=10 (F1 E1 h1 m1 r0)` · **`ab=1/lg=0 ta=110 (F1 E1 h1 m1 r1 maxTargets=2)`**（`FollowUpResolution` = 「If the target dies, deal 2 damage to adjacent units」）。
  ⇒ 相邻那圈**两方都算、含督军、含残骸**，`maxTargets=2`（左右紧邻两格）。
- 卡面：`Deal 5 damage to a troop. If the target dies, deal 2 damage to adjacent units` / 「对一支军队造成5点伤害。如果目标死亡，对相邻单位造成2点伤害」—— 第一句**没写 `enemy`**（解析 `Side=any`，与 `ta=30 F0E0` 一致）。
- 因此 `play ASH73` 的 `P0 WARLORD 28→26` 是**正确复刻**：夹具自动挑的我方 `FIX_A`（3 号格）被 5 点打死，它右邻 4 号格就是自家督军 ⇒ 2 点落到督军身上（`includeHero=1`）。
- ⚠️ 真缺陷是**同族的「锚点落错边」**（`GOF97` / `SOR43` / `TL27` / `UM105` / `AM23` / `9238…` / `UM115` 那 7 张单 op 卡），已由 #1 修掉；`ASH73` 的两条 op 里锚点来自**前一条 op**，本来就是对的（`play ASH73` 改后日志逐字不变）。

### 2·3 任务 3（邻居那一半）

- 判据（🥇 `decomp_full/MinionManager__GetAdjacentUnits.c:52-70`，`G14` 已逐句读过）：在 `+0x60`（己方部队列表）里按引用找锚点、取 `i-1`/`i+1`；删元素只在 `MoveToCemetery` ⇒ **待死窗口内的邻居仍在列表里**。
- 改前（旧行号 `:1725`）：`if (u == null || !u.IsAlive || list.Contains(u)) continue;` + `if (!pool.Contains(u)) continue;`
  —— 两道都会挡住死邻居（`AddSide` 见 `:2835` 的 `!u.IsAlive`）。
- 改后：`1729-1741` —— 邻居集合只判「空位 / 已在本集合」，成员判定收进 `AdjacentNeighborCounts`（`15916-15931`）：`pool.Contains` → 收；活着但不在池 → 不收；**刚死的** → 阵营（`AnchorOnSpecSide`）＋ `spec.Kind == "troop"` 排督军 ＋ `ApplyTargetFilters` ＋ `ApplyTargetCriteriaFilter` 重判一次（**都是原来那套筛，只是不看死活**，⛔ 没加新闸）。
- 反例读数（`play check` ①-b）：`GOF67` 的 `netFull` 多出 `✎ P0[3] FIX_A 2/0/0/-997 → FIX_A 2/0/0/-996`、`ASH16` 的 `net 2 → 4` —— `-997` = 待死哨兵，即**待死窗口里的邻居现在收到增益**。另 `B-16` 一侧的既有口径也印证「待死单位本来就该收到 buff」（`EffectResolver.cs:11222` 那段注释：拿 `IsPendingDeath` 当 state 5 会**反向误杀**那两族）。
- ⚠️ 未改的一处（**不属于本条**，见 §⑤·4）：`DoDeal` 仍然 `if (t == null || !t.IsAlive) continue;`（`EffectResolver.cs:2865`）⇒ 死邻居**吃伤害**那一半仍不动（增益/关键词那半已动）。判据没查清，故**没顺手改**。

### 2·4 任务 4（`AdjustManaEffect`）

- 判据（🥇 `decomp_full/AbilityLogic__AdjustManaEffect.c:5-7`，全文 11 行）：
  `lVar1 = *param_2; if (lVar1 != 0) { *(int *)(lVar1 + 0x10) = param_3 * *(int *)(lVar1 + 0x10); return; }`
  ⇒ `ManaBuff.amount(+0x10) *= multiplier`，**一次乘法、无分支**。随后 `BattleManager__AddManaBuff.c:22` 只 `BattleAction___ctor(&local_208, 0x22, …)` **建一条 action** ⇒ 一次加值、一次广播。
- 结构数据：`SOR49` `abilityMultipliers = [{"trigger":0,"fm":[3,0]}]`（第 2 条 logic 的 `furtherMultiplier = 3`）＋ `unitConditions[0] = {ab:0, lg:1, role:"mult", multiplierSet:{ta:150, F1, min1}}`（= 数「抽到的部队」，夹具里 = 3）。
  ⚠️ 我们这条路上 `TryStructuredMultiplier` 没接上（`op.fm = -1`，见 `ruleprobe seg` 的 `ext={… fm=-1 …}`）⇒ 走的是**正文计数** `countRef=[any] countScope=[draw]` = 3，数值恰好一致。
- 落点：`EffectResolver.cs:597-600`（出牌路）＋ `7118-7120`（触发路）＋ `15934-15947`（`IsManaAmountOp`）＋ `15961-15971`（`FoldManaMultiplier`）。
- `play SOR49`（改后）：`「For each Troop drawn, gain 1 ☀」按 any 计 3 遍` → `原版是**乘进数值**、不是重跑 3 遍 ⇒ 1 × 3 = 3，结算 1 遍` → `信仰 +3（现 15）`（**一行**）。

### 2·5 任务 5（shuriken 接线）

- 判据：`decomp_full/AI__ScoreFromBuffedAttack.c:77`：`cVar2 = EntityScript__EnemyReturnsAttack(param_2, param_3, 1, param_6, 0);` ⇒ option 5 的估值走**预测版（五道）**。
- `decomp_full/EntityScript__EnemyReturnsAttack.c:60-73`：第 5 道 `HasCurrentTrait(param_1, 0x460 /* shuriken = 1120 */)` → `IsUnit(守方)` → `DamageKillsTarget(攻方, 守方, GetCurrentTraitValueWithModifiers(0x460))` → `!IsProtectedFromDamageOrSurvivor` ⇒ 免反击（**不要求远程**）。
- 改动：`AiScoring.cs:582` `if (CounterAttack.EnemyReturnsAttack(ctx, t.Side, u, foe, target, false))`（原来是 `CounterAttack.NoCounter(..., RuleCore.WouldKill(target, atk))`）。
  ⚠️ 顺手对齐了一处口径：`targetWillDie` 现在由方法内部按 `CombatDamage(攻方, attackType)` 算（原版 `.c:18-23/48-56` 同源），**不再**传带 buff 的 `WouldKill(target, atk)`。
- ⛔ 没有动 `CounterAttack.NoCounter`（实际结算四道）；`SimpleAI.cs:604` 仍是四道（**不属我的白名单**，且原版两条入口本就不同源）。

---

## ③ 改了哪些文件哪些行（新增行数）

| 文件 | 改动 | 行数 |
| --- | --- | --- |
| `RuleEngine/Core/EffectResolver.cs` | ① `:1716` 改调 `ResolveAdjacentAnchorFromPool`（1↔1）② `:1723-1727` `AnchorInSet` 过阵营筛（1→5，**+4**）③ `:1732-1741` 邻居成员判定（3→8，**+5**）④ `:597-600` 出牌路折乘数（**+4**）⑤ `:7118-7120` 触发路折乘数（**+2**）⑥ 类尾追加 `15848-15973`（`ResolveAdjacentAnchorFromPool` / `AnchorOnSpecSide` / `AdjacentNeighborCounts` / `IsManaAmountOp` / `FoldManaMultiplier`，**+126**） | **净 +141** |
| `RuleEngine/Data/AiScoring.cs` | `:577-582` 反击闸改调预测版（3→6，**+3**） | **净 +3** |
| `RuleEngine/Core/RuleCore.cs` | **未改** | 0 |
| `账/_parts/B8_P.tsv` | 新建（5 行 · 11 列 · LF） | 6 行 |

- 行尾：`EffectResolver.cs` 仍 **CRLF 15973 / 纯 LF 0 / 裸 CR 0**；`AiScoring.cs` 仍 **LF**。
- ⚠️ **账落点漂移**：EffectResolver 在 `:597` 与 `:1716` 之后各插了行 ⇒ 战斗逻辑驱动里落在 `EffectResolver.cs` 的 A 行会漂（`check_ledger_points.py` 现读：A 3053 · 通过 2927（告警 92）· **漂移 118** · **失败 8**）。我的补丁把 `Leave…GetAdjacentUnits` 那条从「`1391-1758`（该行是 `}` ⇒ 判失败）」改成 `15877（ResolveAdjacentAnchorFromPool）（等价实现 = …）` ⇒ 合并后那条**失败**应当消失。

---

## ④ 没查清的部分

1. **任务 5 没有离线机器读数口**：`ruleprobe ai` 的 `[攻击分·分解]` 走 `SimpleAI.ScoreFromAttack`（四道那条），`play all dump` 按设计看不见 AI ⇒ `BestAttackScore`（option 5 带 buff）改完**量不出前后差**。因此 `EntityScript.EnemyReturnsAttack` 那一行**保持 B**（G3 ✗），等一条能抬 option 5 的读数口或 Unity 侧断言。
2. **`ResolveAdjacentAnchorFromPool` 的兜底口径是「我们挑的」**：原版由玩家/AI 把 `param_5` 传进来；我们没 `chosen` 时按「目标池第一个」（= 本工程其它「取第一个」的定死规则，⛔ 不掷骰）。**卡池里没有 `chosen` 的那条路**（AI / 联网 / 探针）都走这个兜底 ⇒ 是否要做成「AI 按 `AiScoring` 挑一个」仍未定（不是本轮范围）。
   - 代码里**没有打 `🔶 自己设计` 标记**：这条兜底改的是「原版同样存在的『目标选择』这一步」在我们这边的确定性落法，语义仍是复刻（原版也从未「锚在施放者自己」）。**若调度台判它算「自己设计」，请示下，我按铁律 18 就地补标记（零行数变化）**。
3. **`SOR49` 的 `fm=3` 结构化乘数没接上**（`op.fm = -1`）：本轮数值上恰好一致（正文数出 3），但**判据来源不同**（正文计数 vs `furtherMultiplier`）。哪一天正文与数据不一致，就会只按正文算。归 `TryStructuredMultiplier` 那条线，**本轮没碰**。
4. **死邻居「吃伤害」那一半**（§2·3 末）：`DoDeal` 仍跳过 `!IsAlive`；原版 `DealMultiDamage` 对 `Health <= 0` 的单位写不写血，**没查到判据**（未反编译到那一段的净结果）⇒ 保持现状，如实记在这里。

---

## ⑤ 顺手发现（不属本轮任务，供调度台分流）

1. **`f8260229254a09746af6d9ff7a9b0b54 Ethereal Supreme` 的「相邻单位」永远收不到 buff**（`EffectText` 侧缺陷，**不在我白名单**）。
   解析产物：`Raw=your warlord and adjacent units | Side=own | **Kind=warlord** | Adjacent=1 | Anchor=FriendlyWarlord | AnchorInSet=1`。
   `Kind=warlord` 会让 `ResolveTargetsCore` 走「只取督军」那一支（`:1676-1677`）⇒ 邻居（部队）永远不在池里 ⇒ `AnchorInSet` 只加成督军本人。
   原版数据：`ta=10 (F1E0 h1 m0)` ＋ `ta=113 (F1E0 h0 m1)` ⇒ 「督军 + 相邻部队」都该吃。
   建议转派给 `Core/EffectText.cs` 的写手（`ParseTarget` 的「`X and adjacent Y`」分支要按 `and` **后半截**判 `Kind`，而不是整串）。
2. **`give` / `gain` / `lose` 的 `for each` 也走「重跑 N 遍」**，而既有 doc（`TryComputeRepeats` 上面那段判据）写着原版增益族是 `NewBuffStat.c:61-67/207-232 ⇒ 计数 × (change × multiplier)`（**乘值**，与 `AdjustManaEffect` 同一类）。今天形状上的差别：`+2 Attack for each X` 会变成「N 次、每次可能挑不同目标」而不是「一次 +2N」。⛔ 本轮**没动**（改它会动大量卡的指纹，且要先逐卡核「挑了哪些目标」）⇒ 建议单开一条账。
3. **`SimpleAI.cs:604`（`ScoreFromAttack`）仍用四道版免反击**，与本轮接好的 option 5 那条**不同源**。⚠️ 原版这两条入口**本来就不同源**（预测五道 / 实际结算四道）⇒ 要不要一起接**请调度台裁**（`SimpleAI.cs` 不在我白名单）。
4. **`play` 的 dump 列 `selfPlace` 语义没查清**：`GOF97` 改后 `selfPlace 0→1 / other 1→2 / changes 5→7`，但日志显示**没有任何效果落回自己**（眩晕的是 `FIX_E1` + 敌方督军）⇒ 这三列不能当「打到自己人」的判据用，读数要看 `ctx.Log` 的「（「相邻」…）」那两行。
5. **`AM23 Bombast Field Gun` / `UM105 Dreadnought`（Oath）/ `UM15 Apothecary Polixis`（Codex）在 `play` 的夹具里打不出「相邻」日志**（它们的效果在别的时机 / 需要别的触发），所以本轮只能拿 `SOR43` / `TL27` / `SAU56` / `Particle Whip` / `None Must Know` / `AM74` / `SOR60` / `GOF97` 当读数样本。

---

## ⑥ 给调度台的请求

1. **重钉 `PinnedPlayMd5`** → `350c16b2761e9a662b1f29ebaf69500e`（旧 `a93ffb8f344517a9ac611ef06de17a3a`）。⚠️ 这一改动**数值不变、事件次数变**，属预期；重钉前请先看 §① 的 2＋5 张卡清单。
2. **合并我的账补丁**：`python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B8_P`（`--dry` 已跑：`更新 5 · 新增 0 · 无变化 0`；`check_patch_conflicts.py` = **无跨补丁重复键**）。
3. **同步点跑一次 `check_ledger_points.py --all-A --fix`**（我净 +141 行 ⇒ `EffectResolver.cs` 那条链上的 A 行会漂移）。⚠️ 只能在「没有 `.cs` 写手 ＋ 补丁已合并完」时跑。
4. **要跑的自检**（按铁律 12 三条判据）：动了引擎 `RuleEngine/Core/**` 与 `RuleEngine/Data/AiScoring.cs` ⇒ **`RuleEngineTest.Run`**（必跑）；`BattleScene.Run` 建议同批跑（`play check` 的 7 张变化卡都在战斗夹具里）。⛔ 我没跑 Unity 批处理。
5. **转派**：
   - `Core/EffectText.cs` 写手 → §⑤·1（`Ethereal Supreme` 的 `Kind=warlord`）；
   - `Data/SimpleAI.cs` 写手 → §⑤·3（是否把 `ScoreFromAttack` 也接到预测版，需先裁定）；
   - 新开一条账 → §⑤·2（`give/gain/lose` 的 `for each` 乘值 vs 重跑）。
6. **两处请裁定**：① `ResolveAdjacentAnchorFromPool` 的「目标池第一个」是否算「🔶 自己设计」（要在代码里打标记）；② `EntityScript.EnemyReturnsAttack` 的「无读数口 ⇒ 保持 B」是否接受（若要求升 A，需要一个能抬 option 5 的 probe 或 Unity 断言）。
