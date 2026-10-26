# B8_AE · 广播跳数（5 条）× 相邻目标解析（ta=113）× `AdjustCardEffect` 折值判定

> 执行代理 **B8_AE**（完全复刻批次 8）· 2026-10-26 · 白名单：`RuleEngine/Core/EffectResolver.cs` · `RuleEngine/Core/RuleCore.cs` · `RuleEngine/Core/EffectText.cs`
> 账补丁：`账/_parts/B8_AE.tsv`（**10 行** · 11 列 · LF）
> 门禁：`typecheck` **0 / 0** · `ruleprobe check` **rc=1（解析差异 1 行 = 本代理有意改的那 1 张，见 §①·3）** · `play check` **rc=1（指纹变，预期）**

---

## ① 结论（四条各修成什么样 ＋ 新指纹）

| # | 条目 | 结论 | 读数（改前 → 改后） |
|---|---|---|---|
| 1 | 5 条广播多跳了手牌（影响 11 张监听卡） | ✅ **已修**：5 个 kind 的**全部 9 个发射点**都加 `boardAndPersistentOnly: true`（只跑棋盘跳 ＋ 常驻效果跳） | `play all dump`（1128 主档 ＋ 586 电池）改前/改后 **逐字节相同**（`diff` = IDENTICAL）⇒ **现有夹具测不到这条**（见 §④·1，不是「没改」而是「量具盲区」）。`play check` 指纹不变 |
| 2 | `your warlord **and adjacent units**` 被判成 `Kind=warlord` ⇒ 邻居永远收不到 | ✅ **已修（2 处）**：① `EffectText` 对 `Adjacent && Kind==warlord` 放宽成 `unit`（锚点/`AnchorInSet` 不动）；② **顺手查出并修掉** `DoGive` 尾部那道「`Count==1` ⇒ 只留第一个」的裁剪（它把相邻那一圈又抹掉，只剩锚点） | `play f8260229254a09746af6d9ff7a9b0b54 Ethereal Supreme`：改前「锚点 FIXWarlord_TauEmpire → **1 个目标**：FIXWarlord_TauEmpire」、督军血 28→29（FIX_A 2/2、FIX_B 3/3 一动不动）；改后「锚点 FIXWarlord_TauEmpire → **3 个目标**：FIXWarlord_TauEmpire、FIX_A、FIX_B」、督军 28→29 · **FIX_A 2→3 · FIX_B 3→4**。同一条裁剪豁免也让 `SOR60 Holy Fire`（`Give +2 Attack to your Warlord and +1 Attack to adjacent troops`）的 other 3→4 |
| 3 | `give/gain/lose` 的 `for each` 该不该「计数 × 数值」 | ⛔ **不修**，判据**只清楚了一半**（§③）。账里 `AbilityLogic.AdjustCardEffect` 改 **C / P2**，缺的三条证据逐条写明 | 今天 18 张 `for each`＋give/gain/lose 卡里 **13 张已走 `PerCount` 折叠**；其余净读数与「计数 × 数值」**一致**：`play BL40` 4 遍 = 攻 2→6（= +1×4）· `DA45` 4 遍 = 2→6 · `TL68` 计 0 遍 = 什么都不给（= 原版 ×0）· `SW51` 两次都落在同一个敌人上 ⇒ `{huntmark2}` |
| 4 | `ResolveAdjacentAnchorFromPool` 的「池首个」兜底要打 `🔶 自己设计` | ✅ **已打标（零行数变化，就地改写已有注释行）**：`EffectResolver.cs:15917` = `/// <para>🔶 自己设计（非原版）：目标池取首个兜底 · 原版 param_5==null ⇒ 候选为空 · 调度台 2026-10-26 裁定 ｜ …` | 依赖它的卡（`ta=110` 全池 17 张，夹具逐张实测）：**真的走到兜底的只有 2 张 —— `GOF97` / `TL27`**；另 11 张由 `chosen` 或前一条 op 的目标给出锚点；5 张（`57bf…` / `AM23` / `SOR35` / `SW16` / `UM105`）**本轮没跑到那个时机 ⇒ 未测**（§④·4） |

### ①·3 三个指纹（全部交调度台统一重钉，⛔ 本代理一个都没改）

| 指纹 | 改前（钉住 / 上一路） | 本代理改后 | 说明 |
|---|---|---|---|
| 解析输出 `PinnedParseMd5` | `193c1b25b0b4073f3da99f7085d6558d` | **`18439d965d14288fe28cfbe5986dc445`** | `ruleprobe check` = 「解析差异 **1 行**（`f826…` Ethereal Supreme）」。逐 token 对过：**只有一格** `Kind=warlord` → `Kind=unit`，**别的字段一个没动** |
| 结算 `PinnedPlayMd5` | `350c16b2761e9a662b1f29ebaf69500e`（上一路推到这；基线文件里钉的还是 `a93ffb8f…`） | **`a0e9f98b734fc7e29f198bdaccfb866e`** | 本代理**自己的**贡献（07:42 测）。①-a 主档 **4 张**（`GOF97` / `SOR60` / `TL27` / `f826…`，前两张里 `GOF97`·`TL27` 属 `B8_P`）、①-b 电池 **5 张**（`ASH16`/`GOF67`/`GOF97`/`TL27`/`UM105`，全属 `B8_P`）；**新增 0 · 消失 0** |
| 🔴 现读（**含别人的改动**） | —— | `e11aa1c263e933dbdd67718133c8e5b2` | 07:47 之后 `RuleEngine/Core/CreatePool.cs` 被**别人**改过（mtime 07:47，⛔ 不在我白名单）。多出的 4 张 = 主档 `1688c6146abb1e04f98e540b4cbbabd7` / `GOF105` / `TAU64` ＋ 电池 `GOF96`。**重钉请以调度台同步点那次的读数为准**，不是我这个 |

---

## ② 逐条证据

### ②·1 任务 1：原版那 6 条广播确实「只有棋盘一跳」（🥇 `decomp_full/`，本代理复验）

`grep -c GetCardsInHandRef` / `GetCardsInHandList` 逐份全文现读：

| 原版广播 | `GetCardsInHandRef` | 实际形状（全文行数） |
|---|---|---|
| `BattleManagerSupport__BroadcastCollectFaith.c` | **0**（`GetCardsInHandList` 也 0） | `:22` 对 `param_2` 预调一次 ＋ `:23-42` 遍历 `+0x470`（无 `IsInPlay`、有 `op_Inequality` 自排除）；44 行 |
| `…CollectQuestPoints.c` | **0** | 同形（预调 `:22` ＋ 棋盘 `:23-42`）；44 行 |
| `…CollectSpiritStone.c` | **0** | 同形；44 行 |
| `…SabotageCreated.c` | **0** | 同形（`:22` ＋ `:23-40`）；42 行 |
| `…SecretCreated.c` | **0** | 同形（`:28` ＋ `:29-46`）＋ 末尾 `Signal.Raise(CreateSecretSignal)`；52 行 |
| （对照，已收窄的）`…EarnedSkull.c` | 0 | 只有棋盘一跳（`B8_P`/F2 已做） |

**每个 kind 在本工程里的全部发射点**（`grep WhenEventKind.<kind>` 全仓现读，除 `WhenEventKind.cs`/测试/表现层映射）：

| kind | 发射点（改后行号） | 归属的原版广播 |
|---|---|---|
| `GainFaith` | `EffectResolver.cs:12688-12689`（`DoFactionResource` 信仰支）· `:12754-12755`（`FinishActiveAbility`）· `RuleCore.cs:1636-1637`（`GrantPrayEnchantmentFaith`） | 三处都落到 `BattleManagerSupport__BroadcastCollectFaith`（`BattleManager__BroadcastCollectedFaith.c` / `BattleManager__ProcessManaBuff.c:91` 都是**纯转调**或直调） |
| `GainQuest` | `EffectResolver.cs:12673-12675` | `BroadcastCollectQuestPoints`（调用点 `ProcessManaBuff.c:59,70`） |
| `GainSpirit` | `RuleCore.cs:12079-12080`（`CollectWaystoneCore`） | `BroadcastCollectSpiritStone`（调用点 `_ResolveUseWaystone_d__480__MoveNext.c:131`） |
| `CreatesSabotage` | `EffectResolver.cs:4029-4031` | `BroadcastSabotageCreated` |
| `CreatesSecret` | `EffectResolver.cs:4020-4022` · `:4847-4849` · `:12993-12995` | `BroadcastSecretCreated`（三处都对应同一个广播器：`_ResolveCreateHandCard…` / `_ResolveCreateDeckCard…:113-119` / DA44 的 `todeck`） |

⚠️ **逐处确认过的**（简报要求「别照抄行号」）：`EffectResolver.cs:12713`（现 `12754`）那条**不是**另一条广播 —— 它是 `CardScript__FinishActiveAbility.c:21` → `BattleManager__BroadcastCollectedFaith`，而后者 `.c` 全文 2 行 = `return BattleManagerSupport__BroadcastCollectFaith(...)` ⇒ 同一条、同样 0 手牌跳。`RuleCore.cs:1632`（现 `1636`）那条同理（祈祷附魔的 `buffMana{faithMana:1}` → `ProcessManaBuff.c:91`）。

**为什么这是真缺陷（不是「多醒一次」）**：`BroadcastHandWhen`（`EffectResolver.cs:10575+`）对手牌里的单位卡会走 `c.FireWhen(...)` → `ResolveOps`，**真的结算**。11 张可达监听卡 = `SOR38`（665，1 张）· `DA5/DA12/DA6`（695，3 张）· `ASH80/ASH28/ASH78/ASH25`（610，4 张）· `GSC68`（701，1 张）· `DA15/DA66`（700，2 张）。例：`SOR38 Paragon Warsuit` 躺在手里、你获得 1 点信仰 ⇒ 改前会对敌督军造成 4 点伤害，原版不会。

### ②·2 任务 2：两处修（缺任何一处，卡面都还是少的）

1. **解析侧** `EffectText.cs:10219`：`if (spec.Adjacent && spec.Kind == "warlord") spec.Kind = "unit";`
   - 症状来源：同一段 `t` 里既命中「谁的」那一支（`t.Contains("your warlord")` → `Kind=warlord`，`EffectText.cs:10113`）又命中「哪种」那一支（`t.Contains("warlord")` → `Kind=warlord`，`:10122`）；而 `ResolveTargetsCore` 对 `Kind=="warlord"` 只把**督军一个人**放进候选池（`EffectResolver.cs:1676-1681`），相邻那一圈的成员判定 `AdjacentNeighborCounts(..., pool)` 正是拿这个池当**筛**⇒ 邻居永远不在池里。
   - 判据：卡面中文「[[ephemeral]]。给你的战将和相邻单位增加+1生命值」/ 英文 `Give +1 Health to your Warlord and adjacent units`（`cards_engine.json` ＋ PnP 成品卡图同文）；原版结构数据 `ta=10 (F1 E0 h1 m0)` ＋ `ta=113 (F1 E0 h0 m1)` = 「督军」＋「督军周边的部队」两段。
   - 不影响另一张：全池只有 2 句同时出现 `warlord` 与 `adjacent`，`SOR60 Holy Fire` 的两条 op 各自一句（`Adjacent` 只在第二句上）⇒ 这条判断碰不到它第一句。
2. **结算侧** `EffectResolver.cs:11545`：`DoGive` 的裁剪加 `&& !op.Target.Adjacent`。
   - 原裁剪：`if (targets.Count > 1 && op.Target.Count == 1 && !op.Target.Auto) targets.RemoveRange(1, …)` —— 而 `ResolveTargetsCore` 的相邻支**已经**按同一判据裁过（`keep = (spec.AdjacentAll || spec.AnchorInSet) ? 0 : spec.Count`）。再裁一次 = 只剩锚点。
   - 实证（这是本条最硬的证据）：**只改解析侧、不改这一处**时，日志已经是「「相邻」：锚点 FIXWarlord_TauEmpire → 3 个目标：FIXWarlord_TauEmpire、FIX_A、FIX_B」，但**棋盘上只有督军 28→29**（`FIX_A` / `FIX_B` 一点没动）、`DoGive` 打的是「给了 1 个目标」。两处都改后才 `FIX_A 2→3` / `FIX_B 3→4`。

### ②·3 任务 3：判据读到哪一步（为什么停在 C）

**清楚的那一半**（🥇 `.c` ＋ 🥈 dev ILSpy，两份互证）：

- `decomp_full/AbilityLogic__AdjustCardEffect.c:12-58`：闸 = `buffType(+0x38) ∈ {1,2,3,10,70,85,90,95}`，然后把**五对**字段各走一次 `NewBuffStat(valueOption(+0x70), cur, base, …)` 并写回 base 位。
- 🥈 `out_cpp2il_dev_ilspy/AbilityLogic.cs:2450-2478` 给出**命名形参**（G3 第 5 轮「五对字段未配对」由此闭合）：`meleeAttackChange/meleeAttackValueOptions` · `rangeAttackChange/…` · `healthChange/…` · `armourChange/…` · `costChange/…`；`:2493-2501` 的 `AdjustManaEffect` = `manaAmount * multiplier`。
- `AbilityLogic__NewBuffStat.c:61-67`：`uVar10 = param_6; if (param_4 != 0) uVar10 = param_4 * param_6;` ⇒ **`uVar10 = change × multiplier`**；`valueOption == 0` 那一支直接 `return change × multiplier`，`0x50` 那一支 `return uVar10`，其余是「另一把尺子（近战/远程/血/护甲/费用现值）× uVar10」。
- `AbilityLogic__PlayAbility.c:181-236`：`local_178` 的来源 —— `AbilityLogic.multiplier`（`TargetCriteria`）`targetsAffected == 0(any)` ⇒ `local_178 = 1`；`0xd2(210)`/`0xdc(220)` 且 `+0x34`（trait）非 0 ⇒ **逐目标累加该 trait 的值**；否则 `local_178 = 目标表长度`；最后 `if (furtherMultiplier > 0) local_178 *= furtherMultiplier`。⚠️ `AbilityLogic.cs` 的字段 Tooltip 写死了语义：**“If different from 'Any', multiply by number of targets meeting criteria”**。
- `PlayAbility.c:1160-1195`（chosenAbility `0x1e/0x1f` 那一支）：**对每个 target 调一次** `AdjustCardEffect(…, local_178, …)` —— 传的是**完整**乘数，**不是**循环 N 次挂 N 份。

**不清楚的那一半**（就是「不动手」的理由，缺哪条写哪条）：

1. `AbilityLogic__GetTargets` 的**第二实参**是不是 `AbilityLogic.multiplier` —— `PlayAbility.c:181` 传的是 `*(param_1 + 0x30)`，我按 `AbilityLogic` 的字段顺序与偏移（`+0x10 targetsAffected` / `+0x34 traitsFilter`）推断是它，但**没读到 `GetTargets` 内部**怎么用这个参数。
2. **事件型 / 累积型计数**在原版靠哪个字段重复 —— `for each troop drawn`（`SW51`）· `Repeat for each Secret you played this game`（`DA73`）· `For each troop drawn`（`DA49`/`SOR49`）这三类**表达不进 `TargetCriteria`**；它们的 `multiplier` 很可能是 `any(0)`（⇒ 原版 `local_178 = 1`）。若真是这样，**我们按正文数 N 遍本身就是另一个机制**，折成「计数值」反而更错。
3. 上面那 4 张卡的 `multiplier` / `furtherMultiplier` **真值**（`cards_engine.json` 有这两列，但归位门 `AbilityIndex` 在本仓还不可靠 —— `TryStructuredMultiplier` 的注释里已记「归位门没过时按唯一一条有值的 `mult` 行兜底」）。

**三种已知的不等价**（写进账里，等证据齐了照这个查）：

| 情形 | 原版（一条实例） | 我们（N 遍） | 方向 |
|---|---|---|---|
| `addTrait` + `_stackable == count` | `traitValue = 1`（`.c:~655`） | +1 × N | **我们多给** |
| `addTrait` + `_stackable == replace` | `traitValue = change × N` | `Math.Max` N 次 = `change` | **我们少给** |
| `removeTrait`（`lose …` 那族） | **不在** AdjustCardEffect 的 8 个 buffType 里 ⇒ 不乘 | 重跑 N 遍 | 折它是**错的** ⇒ 只能给 `give`/`gain` 折 |

**改动面为什么不是一行**：`TryComputeRepeats`（`EffectResolver.cs:13606-13720`）把「正文计数 / 结构化 `mult` / `RepeatTimes` / `furtherMultiplier`」四路乘进同一个 `repeats`，而出牌路（`:594-637`）与触发路（`:7104-7137`）共用它。要折「计数」那一路、同时**保住 `RepeatTimes` 的「每遍重挑目标 / 重掷 Rng」语义**，得给它加一条分叉（照 `FoldManaMultiplier` 的形状）—— 那是结构改动，且现有夹具结果**看起来一致**（见 §① 第 3 行），没有读数能证明改对了。

### ②·4 任务 4：`🔶` 标记 ＋ 依赖它的卡

- 标记落在声明处（`ResolveAdjacentAnchorFromPool` 的 `<summary>` 内，**就地改写已有注释行 ⇒ 零行数变化**）：`EffectResolver.cs:15917`
  `/// <para>🔶 自己设计（非原版）：目标池取首个兜底 · 原版 param_5==null ⇒ 候选为空 · 调度台 2026-10-26 裁定 ｜ 兜底口径：原版「目标」由 param_5 从外面给（玩家 / AI 选）；…`
- 反查（`cards_truth.json`，1,351 口径）：`targetCriteria.targetsAffected` 直方图前几名 = `10: 501 · 30: 419 · 20: 334 · 120: 104 · 65: 66 · 150: 57 · 90: 50 · 180: 44 · 80: 35 · 50: 33 ·` **`110: 18 行 / 17 张卡`** · `100: 18` · `60: 14` · `130: 9` · `140: 9` · `113: 3` · `52: 3` · **`105: 0`**（池内 1128 口径是 **110: 14 行**；两个张数口径别互相校验）。
- `ta=110` 的 17 张：`None Must Know` · `Stormhawk Interception`(57bf…) · `Master of the Fleet`(9238…) · `AM23` · `AM74` · `ASH54` · `ASH73` · `GOF97` · `GSC72` · `SAU56` · `SOR35` · `SOR43` · `SW16` · `TL27` · `UM105` · `UM115` · `Particle Whip`(eb17…)。
- **夹具逐张实测**（`play <id>` 数它自己的日志 `按目标池取第一个`，另数 `「相邻」` 日志在不在）：
  - **真的走到兜底 = 2 张：`GOF97` · `TL27`**
  - 走到相邻解析、但锚点由 `chosen` / 前一条 op 的目标给出 = 10 张（`24fc…` `9238…` `AM74` `ASH54` `ASH73` `GSC72` `SAU56` `SOR43` `UM115` `eb17…`）
  - **本轮没跑到那个时机（未测，不是「不依赖」）= 5 张**：`57bf…` `AM23` `SOR35` `SW16` `UM105`

---

## ③ 改了哪些文件哪些行（新增行数）

| 文件 | 改动 | 行数 |
|---|---|---|
| `RuleEngine/Core/EffectResolver.cs` | ① `:4010-4021` 注释 ＋ `CreatesSecret` 调用加 `boardAndPersistentOnly: true` ② `:4029-4031` `CreatesSabotage` 同形 ③ `:4846-4849` DA44 `todeck` 支 ④ `:11533-11545` `DoGive` 相邻裁剪豁免（`&& !op.Target.Adjacent`）⑤ `:12666-12675` `GainQuest` ⑥ `:12681-12689` `GainFaith`（`DoFactionResource`）⑦ `:12751-12755` `GainFaith`（`FinishActiveAbility`）⑧ `:12992-12995` `CreatesSecret`（`QuestPointThreshold`）⑨ `:15917` `🔶 自己设计` 标记（**就地改写，0 行**） | **净 +45**（现 16,018 行；`B8_P` 报告时 15,973） |
| `RuleEngine/Core/RuleCore.cs` | `:1631-1637` `GainFaith`（`GrantPrayEnchantmentFaith`）· `:12070-12080` `GainSpirit`（`CollectWaystoneCore`） | **净 +15** |
| `RuleEngine/Core/EffectText.cs` | `:10201-10219` 相邻 warlord 放宽 ＋ 判据长注释 | **净 +18**（现 10,755 行） |
| `账/_parts/B8_AE.tsv` | 新建，**10 条数据行 ＋ 1 表头** · 11 列 · LF（`CRLF 0 / LF 11 / 裸 CR 0`） | 11 行 |

- **行尾**（二进制现读）：`EffectResolver.cs` = **纯 CRLF 16,018 / LF 16,018 / 裸 CR 0**；`RuleCore.cs` = **纯 LF**；`EffectText.cs` = **纯 LF**。三份都没被翻。
- **账落点漂移**：`check_ledger_points.py`（只读）现读 **A 3102 · 通过 2911（告警 91）· 漂移 191 · 失败 0**（`B8_P` 那时 118）。本代理新落点：`ResolveAdjacentAnchorFromPool` = **15922** · `AnchorOnSpecSide` = **15941** · `AdjacentNeighborCounts` = **15961-15976** · `DoGive` = **11405**（裁剪 11545）· `IsManaAmountOp` = **15986** · `FoldManaMultiplier` = **16006** · `TryComputeRepeats` = **13606** · `TryFoldPerCount` = **13531**。⛔ 我没跑 `--fix`（要在「无 `.cs` 写手 ＋ 补丁已合并完」时由调度台跑）。
- **账补丁自查**：`merge_ledger_parts.py --dry --only B8_AE` ⇒ **更新 10 · 新增 0 · 无变化 0**（键全部命中且唯一）；`check_patch_conflicts.py` ⇒ **无跨补丁重复键（3 份补丁）**；`check_a_vs_diff.py` ⇒ **强 0 · 弱 29 · `[非缺口]` 跳过 333**（本补丁**零 A 行**，不受影响）；`verify_annot.py` ⇒ **同名 711 · 异名已注解 2391 · 缺注解 0**。
- **门禁**：`TMPDIR=/tmp/wf_b8ae bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（中途一次被别人的 `CardPresentation/Battle/BattlePostFx.cs` 与 `RuleEngine/Data/CardDatabase.cs` 半成品打断，按铁律 13·3 隔一轮重跑即 0/0）。

---

## ④ 没查清的部分（如实标着，⛔ 不当成已对齐）

1. 🔴 **任务 1 没有可观察量 —— 现有夹具是盲的**。`play all dump`（1128 主档 ＋ 586 事件电池）改前/改后**逐字节相同**（`diff` = IDENTICAL）。原因：`play` 的夹具把被验的那张卡**先打到棋盘上**，另外 10 张监听卡根本不在场；而 11 张可达监听卡在夹具里**从来不会以「手牌态」遇到这 5 个事件**。⇒ 这条的 G3 是 **✗**，账里两行 `…CollectFaith` / `…Created` 保持 **B** 并写明。
   **需要的新夹具**（建议调度台加在 `工具/ruleprobe/PlayProbe.cs` 的电池里，或给 `preset` 加一格 `hand=<卡ID>`）：把一张 `When you gain Faith, …` 的单位卡**留在手牌**，再走一次「收集信仰」那一步；断言 = 敌方督军血量**不变**（改前会 -4）。等价地，对 Secret/Sabotage/Quest/Spirit 各来一条。
2. ⚠️ **`GainSpirit` 少一条预调**（判据清楚、影响面没查清，**没改**）：原版 `_ResolveUseWaystone_d__480__MoveNext.c:131` 传的是**被收的那颗路标石卡**（`param_1 + 0x30`），`BroadcastCollectSpiritStone` 会对它预调一次、并把同一个 `param_2` 传给**每一个**监听器；我们 `BroadcastWhen(GainSpirit, p, null, null)` ⇒ `card` / `subject` 都是 null。改它要动的面：`card` 还喂「代词 `it`」（`EventSlotsScope.TargetAndCard`）与 `WhenEvent` 的卡级筛选。今天 4 张监听卡卡面都不用代词 ⇒ 零可观察差。**已写进代码注释**（`RuleCore.cs:12074-12078`），未开账（若要开，归 `BattleManagerSupport.BroadcastCollectSpiritStone` 那一行 —— 它已经是 B）。
3. ⚠️ **预调 vs 棋盘跳的顺序**：原版这 5 条都是「先对主体预调一次、棋盘循环里排除它」；我们的 `subject` **没**进 `preCalled`（`SnapshotBoardListeners` 只排除 `preCalled`/`preCalled2`）⇒ 目标**集合**相同（棋盘跳覆盖了主体），但**顺序**是「按进场序」而不是「主体先响」。这一点**没改**（改它要动 5 处调用点的 `preCalled` 语义，且 `preCalled` 一传就把 subject 从快照里排除 ⇒ 若快照里另有同名监听会漏，风险大于收益）。**没查清**：原版「主体先响」在我们这边的可观察后果（多监听者同时在场时的先后）。
4. ⚠️ **`ta=110` 里 5 张卡没测到兜底**（`57bf…` `AM23` `SOR35` `SW16` `UM105`）：它们在 `play` 夹具里**根本没打出「相邻」那条日志**（效果在别的时机 / 需要别的触发）⇒ 它们**是否依赖兜底**未知。已知依赖 = `GOF97` / `TL27`。
5. **`ResolveAdjacentAnchorFromPool` 的兜底到底该不该是「池首个」**仍未定（`B8_P` 也提过）：原版 `param_5` 是**玩家 / AI 选**的；我们的 AI 也不给它（`AiScoring` 只挑攻击目标）⇒ 「池首个」= 确定性落法。本代理只**打标**，没改行为。

---

## ⑤ 顺手发现（⛔ 都不在本代理该做的范围，供调度台分流）

1. 🔴 **`DoGive` 那道裁剪会吃掉 `Deployed` / `AttackedBySelf` 那两族的多目标**：`if (targets.Count > 1 && op.Target.Count == 1 && !op.Target.Auto)` 只看 `Count`，不看「目标是不是由别的机制（代词 / 相邻 / 已部署）给的」。我只豁免了 `Adjacent`（有实测证据）；`Deployed`（`ctx.LastTargets`，`EffectResolver.cs:1644-1656`）那一支**同样是「卡面只写一个、实际解出多个」**，理论上会被同样裁掉 —— **没验**，列在这里。
2. ⚠️ **`EffectResolver.cs:15917` 那条 `<para>` 里现在同时有 `🔶 自己设计` 标记和「兜底口径」解释**：`selfdesign_census.py` 只认 `🔶 自己设计` 这个词串（现读全仓已标注数会 +1），下次跑普查时如果它按「行」统计会看到这一行；口径没问题，只是提醒调度台**别把它当重复标注**。
3. ⚠️ **`play` 的 `preset` 只认 `dead` / `enemyhand` / `chipenemy`** —— 想验「手牌态监听」这条根本加不进参数（`enemyhand=N` 放进去的是 `FIX_*` 空卡）。这是我 §④·1 那条请求的具体落点。
4. ⚠️ **`SOR60 Holy Fire` 的读数变化会被误读**：它既在 `B8_P` 的名单里（不，`B8_P` 只报了 `GOF97`/`TL27`/`ASH16`/`GOF67`/`UM105`）也不在我的任务清单里 —— 它是「`DoGive` 裁剪豁免」的**副作用收益**（`adjacent troops` 复数本来该全给）。⛔ 别把它当回归。
5. 🟢 **`ruleprobe check` 的 `[基线指纹]` 卡池 md5 仍与钉住的不同**（`e60982b7…` vs `5ebfa4bf…`）—— 上一路就红着，**与本次改动无关**；重钉时三个指纹要一起钉（`PinnedPoolMd5` / `PinnedBaselineMd5` / `PinnedParseMd5`）。

---

## ⑥ 给调度台的请求

1. **重钉三个指纹**（⛔ 我一个都没改）：`PinnedParseMd5` = **`18439d965d14288fe28cfbe5986dc445`**（原 `193c1b25…`）· `PinnedPlayMd5`（以同步点那次读数为准；本代理的贡献是 `a0e9f98b734fc7e29f198bdaccfb866e`）· `PinnedPoolMd5` / `PinnedBaselineMd5` 按工具提示一起。
2. **合并账补丁**：`python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B8_AE`（`--dry` 已跑：更新 10 · 新增 0 · 无变化 0；跨补丁冲突 = 无）。
3. **同步点跑一次** `check_ledger_points.py --all-A --fix`（净 +78 行 ⇒ `EffectResolver` / `RuleCore` / `EffectText` 那几条链上的 A 行会漂；现读漂移 191，失败 0）。⚠️ 只能在「没有 `.cs` 写手 ＋ 补丁已合并完」时跑。
4. **要跑的自检**（按铁律 12 三条判据）：动了 `RuleEngine/Core/**` ⇒ **`RuleEngineTest.Run`** 必跑；`play check` 变化的 4 张卡（`GOF97`/`SOR60`/`TL27`/`f826…`）都在战斗夹具里 ⇒ 建议同批 **`BattleScene.Run`**。⛔ 我没跑 Unity 批处理。
   ⚠️ 我把 `RuleEngineTest.cs` 里跟这几张卡相关的断言读过一遍：`When your opponent plays a Stratagem` 那条手牌监听测试（`:3729/:3805`）走的是**另一个事件**，不受本次收窄影响；但**没有**任何测试覆盖「这 5 个事件 ＋ 手牌态监听」⇒ 不会有红，也**不会有**判别力。
5. **补一格夹具**（§④·1）：`preset hand=<卡ID>` 或电池里加一步「监听卡留手牌 + 触发该事件」。没有它，任务 1 那条差异只能靠读 `.c` 判，`play check` 永远看不见。
6. **需要裁的两条**：① §④·2 的 `GainSpirit` 预调主体（要不要顺手补，影响面未核）；② §③ 任务 3 那三条证据什么时候去查（`GetTargets` 第二实参 / 事件型计数的消费点 / 4 张卡的 `multiplier` 真值）—— 查清之前 `AdjustCardEffect` 保持 C。
