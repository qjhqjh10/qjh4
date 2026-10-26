# B8_G · 动作泵与杂项引擎（`Core/*.cs` 19 个文件的 B→A）

> 执行代理 **B8_G** · 2026-10-26 · 白名单 = `RuleEngine/Core/` 下 19 个文件 · 补丁 = `账/_parts/B8_G.tsv`（15 行）
> 判据口径：`判据/战斗逻辑驱动_B2A_口径.md`（G0–G8，**只适用于 Add*/Resolve* 一族**）+ 方案 §4.1（其余方法）。

---

## 一、结论（先看这 6 行）

| 项 | 数 |
|---|---|
| **升 A** | **9 行** |
| 判 **D**（死代码，带出处） | **1 行** |
| 保持 B、但**订正落点 / 补全判据 / 补反例读数** | **5 行** |
| 改的 `.cs` | **4 个**（`CounterAttack.cs` +106 行 · `ConditionEval.cs` **零行数变化** · `DeckRules.cs` **零行数变化** · `FirstSeatRules.cs` **零行数变化**） |
| typecheck | **运行时 0 / 编辑器 0** |
| `ruleprobe check` / `play check` | **rc=0 · 哨兵失败 0** · `play check` 指纹仍是钉住的 **`a93ffb8f…`**（⇒ 本批改动对全池结算**零影响**） |

⚠️ **没到 10 条**：本批真正卡住的 8 类原因见 §四（每一条都点名「卡在哪、回来的条件」），
⛔ 我没有为了凑数把「读到了但接不上线」的行硬判 A。

---

## 二、逐条证据（升 A / D 的行）

### A 档 9 条

| # | 账行（战斗逻辑驱动） | 原版方法 | 判据（方法体，🥇 = `decomp_full`） | 我们的落点（行号范围） | 可观察量（两侧） |
|---|---|---|---|---|---|
| 1 | 901 | `CardScript.IsProtectedFromDamageOrSurvivor()` | `CardScript__IsProtectedFromDamageOrSurvivor.c:20-97`（全文 106 行逐句；5 个 id → `:97` `MeetsTraitCriteriaFromList`） | `ConditionEval.cs:287-290（case 120）` | `ruleprobe ai` 远程②：`GSC34`（远程 8 · 守方血 8）⇒「② 反击 = 0（豁免）」；`SW29`（7 < 8）⇒「② 反击/存活 = 0.1（counter=0 · 存活奖励=0.1）」 |
| 2 | 854 | `CardScript.GetCurrentDamage()` | `CardScript__GetCurrentDamage.c:6-27` + `EntityScript__get_currentMaxHealthProcessed.c:19-40` | `ConditionEval.cs:231-233（case 40）` | `play TL68` ⇒ 落场 `2/2/0/5`；`play TL68 preset chipenemy=1` ⇒ `8/8/0/8` |
| 3 | 908 | `CardScript.IsWarlordEquivalent()` | `CardScript__IsWarlordEquivalent.c:8` + `GameStaticData__IsWarlordEquivalent.c:3-5`（`+0x94 == 10`） | `ConditionEval.cs:419-420（case 270）`（275 档已改为转调本档） | 无独立观测量（`cond=270` 池内 0 行）⇒ 走 §4.1 |
| 4 | 580 | `BattleManager.ResolveReassembleMinions(bool)` | `BattleManager._ResolveReassembleMinions_d__455__MoveNext.c:1-23` + `MinionManager__ReassembleMinions.c:97,154` | `BoardSlots.cs:1-45`（连续无洞模型）+ `BoardSlots.cs:362（RemoveAt）` | `play BL50 preset chipenemy=1`：P1 由 `[.][.][.][FIX_E1][WARLORD][FIX_E2][FIX_E3][.][.]` ⇒ `[.][.][.][.][WARLORD][FIX_E2][FIX_E3][.][.]`（删人后每侧仍无洞）；反例 `play BL50` ⇒ `ErrNoTargetAvailable` |
| 5 | 136 | `BattleManager.AddDamageStat(bool,int)` | `out_cpp2il_dev_ilspy/BattleManager.cs:24526-24529`（🥈 转调）+ `ChallengeLogMgr__DamageStat.c:4-20`（🥇 真体） | `AchievementTally.cs:149-157（AddDamage）` | 结算侧 `AchievementTally.DamageOf(seat,kind)`（探针不暴露）⇒ 走 §4.1 |
| 6 | 351 | `BattleManager.GetAdjacentUnits(CardScript)` | `BattleManager__GetAdjacentUnits.c:1-19` + `MinionManager__GetAdjacentUnits.c:1-110` | `EffectResolver.cs:1701-1771` + `BoardSpec.cs:61-71（AdjacentSlots）` | 修后 `play check` 主档 5 张卡变化（`ASH73`+2 / `ASH54`+4 / `SAU56`+3 / `Particle Whip`+3 / `None Must Know` 眩晕督军 1）＋ B8_C 的 `play AM74` / `ASH73` / `GOF97` 逐卡档 |
| 7 | 1169 | `EntityScript.GetCombatDamage(AttackTypes)` | `EntityScript__GetCombatDamage.c:1-46` | `CounterAttack.cs:81-88`（CurrentCombatAttack）+ `CounterAttack.cs:129-135`（新增 `CombatDamage`） | `ruleprobe ai ASH6`：「我方 atk=1」（近战）/「我方 atk=3」（远程） |
| 8 | 872 | `CardScript.HasBastionHigherThanAttack(CardScript)` | `CardScript__HasBastionHigherThanAttack.c:1-29` + `EntityScript__get_CurrentBastion.c:5` | `CounterAttack.cs:113-117` + `CounterAttack.cs:99（CurrentBastion）`；条件 110 档已改为转调 | 无（`cond=110` 池内 0 行）⇒ 走 §4.1 |
| 9 | 1180 | `EntityScript.HasCompletedQuest()` | `EntityScript__HasCompletedQuest.c:8-18` + `EntityScript__HasQuests.c` | `ConditionEval.cs:496-509（case 320）` | 无（`cond=320` 池内 0 行、1,351 张 `_quest` 全 0）⇒ 走 §4.1 |

### D 档 1 条

- **`BattleManager.GetPlayerGoesFirst()`**（`BattleManager__GetPlayerGoesFirst.c:1-72`，全文逐句读）
  → **D = 死代码**：`decomp_full` 全树 grep `GetPlayerGoesFirst\b` **只命中它自己的定义**；
  `cpp2il/out_cpp2il_dev_ilspy` 1 命中（声明）；`d:/2/新解包资源/assets_full` 按名字 0 命中。
  **真链 = `BattleManager__SetupBoardPhase.c:107` 直接调 `SupportMethods.GetPlayerGoesFirstWithInitiative`**
  （那一行账里已是 A，`战斗逻辑驱动.tsv:1471`）。
  🔴 顺带订正了它里面的两处错：**`140 (0x8c)` 读的是教程 stage**（不是战役）、**「其余」直接掷硬币**（不经先攻比）。

---

## 三、改了哪些 `.cs`（新增行数 / 行号影响）

| 文件 | 行尾 | 行数变化 | 内容 |
|---|---|---|---|
| `RuleEngine/Core/CounterAttack.cs` | LF | 90 → **196**（+106，**全部追加在类尾**） | 新增 `CurrentBastion`(99) · `HasBastionHigherThanAttack`(113-117) · `CombatDamage`(129-135) · `DamageKillsTarget`(143-148) · `EnemyReturnsAttack`(176-196)。既有行（43/53/60/64/75-86/81-88 被 6 条账行引用）**一行没动** |
| `RuleEngine/Core/ConditionEval.cs` | LF | **623 → 623（零）** | ① `case 110` 末跳改调 `CounterAttack.HasBastionHigherThanAttack`（原 `combat <= u.Bastion`）；② `case 275` 前半改调 `case 270`（`IsWarlordEquivalent` 判据只写一处）；③ `case 40 / 120` 的注释换成逐句判据（`.c` 行号 + 5 个 trait id）；④ `case 40 / 270 / 275` 的行数一处未增 ⇒ 账里 `:149/:181/:536/:541` 那一批落点**不漂** |
| `RuleEngine/Core/DeckRules.cs` | **CRLF（保持）** | **800 → 800（零）** | `DescribeShort` 按 `.c` 订正：其余档**回落到** `MenuDeck/Error/InvalidDeck/Short`（原版 `DeckUtility__ToShortRawLocalizationString.c:21-24` 的常量兜底），改前回的是**长档**；doc 里那句「一律回长档」同步改写 |
| `RuleEngine/Core/FirstSeatRules.cs` | LF | **93 → 93（零）** | 文件头分派表就地订正（`140` = 教程；「其余」= 直接掷硬币；并写明 `GetPlayerGoesFirst` 是死代码、真链在 `SetupBoardPhase.c:107`） |

⚠️ 三个文件做得「**零行数变化**」不是洁癖：`ConditionEval.cs` 有 6 条、`DeckRules.cs` 有 12 条账行按行号引用它们，
插一行就会让 `check_ledger_points` 报漂移（铁律 18·b 同一条纪律）。

---

## 四、没查清 / 接不上线的部分（⛔ 所以那些行保持 B）

| 账行 | 方法 | 卡在哪 | 回来的条件 |
|---|---|---|---|
| 1165(1166) | `EntityScript.EnemyReturnsAttack` | **G0 ✓**（82 行 + 3 个调用点读完）· 方法已建（`CounterAttack.cs:176-196`）· **调用点未接** | 把 `Data/AiScoring.cs:578` 改调 `CounterAttack.EnemyReturnsAttack`（**一行**）。那两个文件不属本代理 |
| 1252 / 1267 | `MinionManager.GetIndexOfCard` / `GetOwnPositionInBoard` | 原语 `BoardSlots.OriginalIndexOfCard:528-536` 已建且正确，**全仓无调用方** ⇒ 探针调不到 | 谁来接 `targetsAffected=150` / `filterSpecificCards` 那两族时顺手接上 |
| 391 / 393 | `BattleManager.GetHighestHealthWarlord` / `GetLowestHealthUnit` | 🔴 **原落点写错**（都写「CountEval 的血最高/最低」，而 `CountEval.cs` 里没有这两档）：前者实际在 `AiTargets.cs:303-311`（230 档，未实现）；后者在 `EffectResolver.cs:1798-1826` + `AiTargets.cs:450`（已建）。**已订正落点，保持 B** | 前者要 `EffectResolver`/`AiTargets` 的写手实现 230 档 + 一张 `ta=230` 的卡（池内 0 张） |
| 1488 | `SupportMethods.IsPlayerAffected` | 真值表读完（15 行）。🔴 **接线前必须先定一格**：`AbilityLogic__PlayAbility.c:308/324` 里 `uVar12` 到底是 `thisCard.isPlayer` 还是它的取反 —— 两种读法都自洽，定错会把「Self」抽错人的手牌 | 定下参考系后，在 `ApplyOffensiveLogic` 的 `drawCard` 支按两侧各问一次 |
| 1026 / 888 | `CardScript.SetPray(bool)` / `HideWarlordActiveAbilityForTutorial()` | 落点在 `UnitState.Prayed`（`:351` **字段声明** = 类级）与 `CardDef.HasActiveAbility`（只读）⇒ 双侧方法级做不到；后者**连写点都没有** | `UnitState` / `CardDef` 的写手各补一个「可运行时清」的位 + 写点（教程链） |
| 967 | `CardScript.RemoveDuplicitous` | `duplicitous(160)` 这个 trait 我们**整条没有**（`KeywordTable` 无位、池内 0 张） | 该关键词进数据时再补 |
| 1075 | `CardScript.TriggerOtherCardAbility` | 🔴 **落点疑似挂错**（见 §五·1） | 需重判这一行的落点 |
| 1520–1522 | `TriggerAbility.OnInitialize/OnReset/OnTriggerAbility` | 数据缺口：`MissionChallenge` 定义在 **PlayFab Title Data**（84 个 bundle 0 资产） | 铁律 11 例外② ⇒ 等远端 |
| 1316 | `PlayerHand.GetMulliganOption` | 原版**死代码**（全树 0 调用点），真值在换牌协程里按列表下标写；我们照的是协程那份 | 单开一行记「原版 `GetMulliganOption` 死代码」由调度台判 |

---

## 五、顺手发现（⛔ 都没自己改）

1. 🔴 **`CardScript.TriggerOtherCardAbility` 的落点疑似挂错**（账行 1075）：
   原版真体 = `abilityCard.OnTriggerEffectInCard(AbilityTrigger.**ThisCardPlayed**, mgr, this, null, null)`
   （`cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:7718-7726`），触发的是**另一张卡的 `ThisCardPlayed`**，
   **不是** `PlayedThirdCardInTurn(15)`；调用点 = `AbilityLogic__PlayAbility.c` · `BattleManager__ApplyEnvEffect.c` ·
   `_ResolveSummonUnit` · `_StartBattlePhase`（与「进攻卡那一族」不是同一族）⇒ 本行落点需要重判。
2. 🔴 **相邻目标那一跳，我们仍要求「邻居活着」**（`EffectResolver.cs:1725` 的 `!u.IsAlive`）——
   原版只看「在不在 `+0x60` 列表里」⇒ **邻居刚死、同一拍还在棋盘上时我们会漏掉它**。
   G14 修的是**锚点**已死那一半；**邻居**这一半还开着（账行 351 的「差异」列已如实写上）。
3. 🔴 **`shuriken(1120)` 这条免反击通道整个不在我们代码里**（不是「值不对」，是「没有这一支」）：
   原版在**预测路径**（`EnemyReturnsAttack`）里有、**实际结算路径**（`_ResolveAttack` 四道）里没有 ⇒
   两侧不一致是**原版自己的形状**，⛔ 别把它统一成一条。反例读数：`ruleprobe ai ASH35`
   （Wraithknight · `Shuriken 8` · 远程 6 ⇒ 星镖 8 ≥ 守方血 8）打出「② 反击/存活 = 0.1…」= **没走豁免**。
4. ⚠️ **账行 242（`DeckUtility.ToShortRawLocalizationString`）在**本会话进行中**被别的代理（`B8_I`）改成了 A，
   但那一行的 `备注` 仍写着「⇒ 保持 B」** ⇒ 状态与备注自相矛盾（`check_a_vs_diff` 的弱命中里就有它）。
   我这一侧**只改代码不改那一行**：`DescribeShort` 现已按原版 `.c` 回落 `InvalidDeck/Short`，
   因此那一行 A 里写的 G2（「拼 `MenuDeck/Error/<枚举>/Short`；取不到退 `InvalidDeck/Short`」）**现在才真的成立**。
5. ⚠️ **`patch_check` 的跨补丁撞车**：`B8_C.tsv` 也改过 `GetAdjacentUnits`（同样判 A）。
   我把自己那份**并入了它的逐卡读数**再保留（本补丁里那一行的「备注」写明了合并来源）——
   合并顺序上 `B8_G` 在 `B8_C` 之后（字典序），**这一条不是语义选择**，如需以某一侧为准请显式指定。

---

## 六、给调度台的请求

1. **合并**：`python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B8_G_`
   （⚠️ 本会话进行中主表**已被别人合过一次**（`_merged_B8*` 目录在变），我的 3 行 A 曾出现在主表里；
   之后我又按 `check_a_vs_diff` 的弱命中改了措辞 ⇒ **再合一次即可**，按 `(类, 方法)` 幂等）。
   合完把 `B8_G.tsv` 归档进 `_parts/_merged_B8*/`（铁律：合并过的补丁不能留在原地）。
2. **指纹**：`play check` 仍是 **`a93ffb8f…`**（与源码常量一致）· `ruleprobe check` **哨兵失败 0** ⇒ **⛔ 不要重钉任何基线**。
3. **账漂移预期**：`ConditionEval.cs` / `DeckRules.cs` / `FirstSeatRules.cs` **零行数变化**；
   `CounterAttack.cs` **只在类尾追加**（既有落点 43/53/60/64/75-86/81-88 不动）⇒ **本批不应产生任何漂移**。
4. **两条接线请求**（各一行，都在别人的文件里）：
   · `Data/AiScoring.cs:578` ⇒ 改调 `CounterAttack.EnemyReturnsAttack`（补上 `shuriken` 那道免反击）；
   · `Data/AiScoring.cs:606` 那一段的「邻居 `IsAlive`」见 §五·2（在 `EffectResolver.cs:1725`）。
5. **一行账需要重判**：`战斗逻辑驱动.tsv` 1075（`TriggerOtherCardAbility` 的落点，见 §五·1）。
