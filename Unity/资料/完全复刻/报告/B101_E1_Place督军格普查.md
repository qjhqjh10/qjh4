# B101_E1 — `Place(..., 4, ...)`（督军格）全仓普查

> 2026-10-30 · 执行代理 `B101_E1`（转岗 · **只读**）· ⛔ 未改任何 `.cs` / 账 / 正本 · ⛔ 未跑 Unity / `ruleprobe` · ⛔ 未动 git
> 起因：`B101_R1` 查出 `Place` 夹具不检查督军格；同批它**已经修掉** `B97W19_Assert.cs` 的一处（`4` → `5`，见 §四）。

## 一、结论摘要

**现读总数 = 27 处** `Place(<ctx>, <seat>, 4, …)`（正则 `\bPlace\([^,]+,[^,]+,4,`，全仓 `Assets/**/*.cs`，2026-10-30 现读）。

| 分类 | 处数 | 含义 |
|---|---|---|
| **笔误（真有害）** | **17** | `ctx` 是 `RuleBattle.NewBattle` 出来的（**督军真的在 `Board[4]`**）⇒ 摆上去把督军挤出盘面；而该处断言**只按对象判**、压根没用到「4 号格」这件事 ⇒ 4 是随手挑的 |
| **本意（改要走两处）** | **4** | 4 号格被后续代码**显式当参数用**（`PlayCard(…,4,…)` / `CanUseAbility(…,4,4)` / `FireTriggerAtForTest(…,4,…)` / 「格号序 vs 进场序相反」的设计）⇒ 能改，但要连带改调用实参 |
| **无影响（撞不到）** | **6** | `ctx` 是手搓的 `new BattleContext + new PlayerState()` —— **根本没有督军**，4 号格只是一格空格 ⇒ 不是笔误，不用改 |

> ⚠️ `B101_R1` 那份清单里写「16 处」但实际枚举了 **19** 个行号（1+3+4+6+1+1+1+1+1=19）。**19 个现读全部仍在**（行号**未漂**，逐个核过）；本报告另外**新发现 8 处**：`B98G2_Assert:163/186/187/214/309`、`B99E9_Assert:317`、`RuleEngineTest_EngineE8:123/265` ⇒ 19 + 8 = **27**。
> 另外 `B97W19_Assert.cs` 那处（原 `:195`）**已被 `B101_R1` 改成 5**，所以不在 27 里。

**最该先修**（按「静默错的后果」排，不按「改起来容易」）：
1. **`RuleEngineTest_EngineX1:249`** 与 **`RuleEngineTest:37963`** —— 这两处挤掉的是**敌方督军**，而它们所在的测试正好是「按盘面/相邻关系判」的（`ta=100` 尾巴 `CandidateAdjacentTo` / 反击豁免），将来一旦有人给这几条加一句「敌方督军」类断言就会**静默**读到部队。改法最机械（`4` → `5`）。
2. **`RuleEngineTest_EngineE3` 六处 + `B99E9_Assert:317` + `B97_ER_Assert` 三处**（共 10 处）—— 全是 `ProbeBattle` 夹具、`4` 都只当「第二张己方卡」用，统一 `4` → `5` 即闭合，且**同一夹具里的 `3` 保持不动**。
3. **`EnchantmentTemplateCheck:701` / `EngineE8:265` / `EngineX6b:142` / `Order:431`**（本意档）—— 要连调用实参一起改，优先级低于前两批但**不该长期留着**：它们的 4 号格现在是「一个部队替督军站桩」。
4. **`B98G2_Assert` 六处**：**不用改**（无督军可撞）。

## 二、督军被挤出盘面之后，引擎会怎样（只读源码）

**开局写点**：`RuleCore.cs:655` `p.Board[BoardSpec.WarlordSlot] = p.Warlord` —— 只写这一次（`NewBattle` 里）。`Place` 夹具直接 `ctx.Players[p].Board[slot] = u`（`RuleEngineTest.cs:2748`）**不检查格号** ⇒ 后写覆盖前写，`Board[4]` 变成部队；督军对象仍在 `PlayerState.Warlord` 字段上（**没被判死、也没离场寄存器**）。

**被谁读 / 后果**（现读，逐条给位置）：

| # | 读者 | 现读行为 |
|---|---|---|
| ① | `EffectResolver.cs:1672`（`ta = 120 friendlyHero` 那一支） | `var hero = ctx.Players[owner].Board[WarlordSlot]` ⇒ **取到的是那支部队**当作「己方督军」。这是**最会静默出错**的一条（当前 27 处夹具都没走到这一档，但它是「最该先修」的理由） |
| ② | `BoardSlots.cs:453`（`IsContiguous`） | 直接 `why = "督军不在 4 号格"` ⇒ 返回 **false**。`RuleEngineTest` 里有 **14 条**把 `IsContiguous` 当**前提断言**（`:5781/6752/6943/12664/18552/25884/25885/28921/28922/30984/31040/31946/33249/34671`）—— 27 处夹具**都不在这 14 条的范围里**，所以今天没有直接变红 |
| ③ | `BoardSlots.Insert`（`:307`）/ `RemoveAt`（`:362`） | 不连续 ⇒ 走**降级分支**（不推人、直写空位），并 `WarnHoley`（`:280`）打一条 **`Debug.LogError`**。⇒ 在这类夹具里**任何真部署 / 召唤 / 离场**都会刷一条错误日志，且单位不再内/外移 |
| ④ | `BoardSlots.cs:497`（`InteractiveUnitsCount`）· `RuleCore.cs:16500`（`BroadcastActivateMinions`）· `AiScoring.cs:1096`（AI 主动技费用打分） | 这些循环**按格号 `continue` 掉 4 号格**（「督军不在原版那两条 minions 表里」）⇒ **站在 4 号格的部队被当成督军跳过**：`BroadcastActivateMinions` 不给它 `RecomputeActAndAttack`（回合开始不重置行动/攻击态），AI 打分也看不见它。⚠️ `InteractiveUnitsCount` 目前 0 调用方（死代码，其 doc 已注明） |
| ⑤ | `RuleCore.cs:13196`（`SpentAllMana` 触发）· `:14985`（指纹 `ca:` 前缀） | 用 `w = PlayerState.Warlord` 配格号 `WarlordSlot` 发事件/写指纹 ⇒ 事件里「格 4」对应的是**不在盘上的督军**，与盘面不一致（对指纹类断言是**静默**的） |
| ⑥ | `RuleCore.FindUnit`（`:9258`）→ `RemoveIfDead`（`:9239`）→ `CleanupDeaths`（`:9589`） | 全按「单位 → 在谁的哪一格」找；**督军不在盘上 ⇒ `FindUnit` 返回 false ⇒ 直接 return** ⇒ 督军的**死亡流水线整条不跑**（`Die` 广播 / Slay 记账 / 进坟场 / 演出）。而 `RemoveIfDead` 的 doc 第一句就写着「督军除外 —— 它留在槽 4」 |
| ⑦ | `PlayerState.IsDefeated`（`:164`）= `Warlord == null \|\| Warlord.Health <= 0` | 读的是**对象**，所以**输赢仍然判得出来**（`CheckWinner` 照常）。⇒ 「督军被挤掉」**不会让胜负失效**，坏的是「死亡事件链」与「督军类目标」 |
| ⑧ | `PlayerState.Units()`（`:172`）· `EffectResolver.AddSide`（`:3005`） | 按槽遍历 + 用 `IsWarlord` **认人**（不按格号跳）⇒ 4 号格的**部队照样算进去**，而被挤掉的**督军算不进去**（它已不在数组里）⇒ 「给全场加/减」类的目标列表**少一个督军**（`troopOnly` 那一档本来就排除督军，所以那档反而无差别） |

**一句话**：胜负不受影响，**死亡链路 + 督军作目标的链路 + 回合开始的重算 + 棋盘连续性降级**受影响；这四样恰好是「测试以为自己覆盖了、其实没覆盖」的高发区。

## 三、逐处清单（27 行）

> 「上下文有督军」= 该夹具的 `BattleContext` 是否由 `NewBattle` 建出来（⇒ `Board[4]` 上真有督军）。

| # | 文件:行 | 摆的是什么 | 该断言想验什么 | 4 是本意还是笔误 | 建议格（为什么不撞） | 风险 |
|---|---|---|---|---|---|---|
| 1 | `CardPresentation/Editor/B97_ER_Assert.cs:127` | `Unit("B97ERMate",1,2,5)` · 部队 · `Test` 阵营 · 非督军 | ② `OtherUnitSummoned` 的单元级闸：acting=**己方** ⇒ 不触发 + 出声 | **笔误**（有督军；4 只当「另一张己方卡」；夹具里 `own`→3 / `foe`→1(P1)） | **5**（P0 右贴督军格，整侧空着） | 无（只比 `actingCard` 的敌我）；改后 `Board[4]` 恢复督军，`IsContiguous` 若有人加前提也才成立 |
| 2 | `B97_ER_Assert.cs:166` | `Unit("B97ERMate2",1,2,5)` | ③ 解耦：`abilityActingTrigger` 不点亮第二道闸 | **笔误**（`own`→3，其余空） | **5** | 同 ① |
| 3 | `B97_ER_Assert.cs:199` | `Unit("B97ERHandMate",1,2,5)` | ④ 无 `actor` 的宿主卡/座位支：己方 acting ⇒ 不触发 | **笔误**（`foe`→1(P1)） | **5** | 同 ① |
| 4 | `CardPresentation/Editor/B98G2_Assert.cs:162` | `Unit("G2Attacker")` | A4/A5：`BroadcastUnitAttackedHandHops` 扫两侧手牌的次数 | **无影响**（ctx 是 `new BattleContext + new PlayerState()`，**没有督军**） | 不用改 | 无 |
| 5 | `B98G2_Assert.cs:163` | `Unit("G2Attacked")`（P1） | 同上 | **无影响**（同上） | 不用改 | 无 |
| 6 | `B98G2_Assert.cs:186` | `Unit("G2Attacker2")`（ctx2） | A6 灭自证：无 trigger 的卡 ⇒ attempts 0 | **无影响**（手搓 PlayerState） | 不用改 | 无 |
| 7 | `B98G2_Assert.cs:187` | `Unit("G2Attacked2")`（P1, ctx2） | 同上 | **无影响** | 不用改 | 无 |
| 8 | `B98G2_Assert.cs:214` | `Unit("G2CasterCard")` | B1/B2：`GrantCasterCardName` 取 `ActingUnit` 优先 | **无影响**（手搓 PlayerState；`u` 只被赋给 `ctx.ActingUnit`） | 不用改 | 无 |
| 9 | `B98G2_Assert.cs:309` | `Unit("G2Bare240")` | D1/D2：`WhileInPlayVariable` 判不了 ⇒ **不摘** | **无影响**（手搓 PlayerState；且下一行 `WhileInPlayVariableAbilityRunnable(ctx,0,4,uBare)` **显式用 4**） | 不用改（改要连带改那一行的 `4`） | 无 |
| 10 | `CardPresentation/Editor/B99E9_Assert.cs:317` | `Unit("B99E9RallyFriend",1,1,3)` · 部队 | ② Rally 负例：目标是**己方**（criteria 要敌方）⇒ 不触发 + 出声 + 没拿到 Vanguard | **笔误**（`ProbeBattle` 有督军；`carrier`→3 / `foe`→1(P1)） | **5** | 无（断言按对象 + `B99E9KeywordLayers(friend,…)`） |
| 11 | `RuleEngine/Editor/EnchantmentTemplateCheck.cs:701` | `BareCard("E26Carrier")` · 部队 · `Test`/Infantry | G/H 全节：打出 `SAU53`/`TL47`/`AM51`/`SW56` ⇒ 目标身上真的挂上附魔模板（`ActiveEnchantments` 计数/条目/`WhileInPlayAnim`） | **本意**（`PlayCard(ctx,0,idx, **4**, carrier)` 把 4 当**目标格**传下去） | **5**，且把 `placeCarrier ? 4 : 0` 一起改成 `5` | ⚠️ 要**两处同改**；只改一处 ⇒ 目标格落空（`carrier` 在 5、PlayCard 找 4）⇒ G2 那组全红。改后行为应逐字不变（目标仍是一个普通部队） |
| 12 | `RuleEngine/Editor/RuleEngineTest.cs:26717` | `Unit("W58SQ-Doomed",1,0,1)` + `exhausted:true` | ③ Slay 出队复核三档（a 在盘上 / b 待死 / c 已离场）：`live`→2、`gone`→5 | **笔误**（有督军；4 不被断言引用） | **3**（左贴督军格）—— 这样左侧 2,3 连续、右侧 5 连续，**顺带把这块夹具变成连续棋盘** | ⚠️ 该夹具**本来就是有洞的**（2 号有人、3 号空）⇒ `Insert` 早已在降级分支；改到 3 是净改善。断言只读 `PlayOrder`/`DyingThisAction`，不依赖相邻 |
| 13 | `RuleEngineTest.cs:37947` | `Unit("FixtureTerror",1,5,5,"terror")` | ① 反击豁免四道：`terror` 守方 `currentCombatAttack 2 < 5` ⇒ 免反击（`sneak`→3 / `lr`→5 / `sn`→6） | **笔误**（有督军；4 只当连排里的一格） | **7**（右侧 5,6,7 连续；左侧保持 3） | 无（`CounterAttack.NoCounter` 全显式传参） |
| 14 | `RuleEngineTest.cs:37963` | `Unit("FixtureShield",1,2,10,"shield")`（**P1**） | ① Sniper 反例：守方有 Shield ⇒ **不**免反击（P1 `target`→3 / `terrorBig`→5） | **笔误**（有督军；挤掉的是**敌方**督军） | **6**（P1 右侧 5,6 连续） | 无今天；但**将来接「敌方督军」断言会静默读到部队** ⇒ 排第一优先 |
| 15 | `RuleEngineTest.cs:37987` | `twins`（同名两份之一） | ③ `diedThisTurn` **按实例**分得清（`a`→3） | **笔误**（有督军） | **5** | 无（只比 `InstanceId`） |
| 16 | `RuleEngine/Editor/RuleEngineTest_EngineE3.cs:249` | `Unit("E3GateMate",1,2,5)` | ③ 单元级闸正/负例（`own`→3 / `foe`→1(P1)） | **笔误**（`ProbeBattle`） | **5** | 无 |
| 17 | `RuleEngineTest_EngineE3.cs:348` | `Unit("E3OldMate",1,2,5)`（ctx4） | ③ 旧表（列没落）⇒ 闸**不筛**＝与改前逐字相同 | **笔误** | **5** | 无 |
| 18 | `RuleEngineTest_EngineE3.cs:415` | `Unit("E3MateAtk",1,3,8)` —— **是「攻击者」本身** | ④a 负例：**己方**攻击 ⇒ acting=己方 ⇒ 被 `enemyUnits=1` 筛掉 ⇒ 监听者不结算 | **笔误**（`meB`→3 / `foe`→1(P1)） | **5** | 无（广播全显式传 `mateAtk`；`SumHealth` 读的是 P1，未受影响） |
| 19 | `RuleEngineTest_EngineE3.cs:441` | `Unit("E3DMate",1,2,8)` | ④b 正例：acting=**监听者自己** ⇒ `ta=20 self` 通过 ⇒ 结算 | **笔误**（`me`→3 / `foeU`→1(P1)） | **5** | 无 |
| 20 | `RuleEngineTest_EngineE3.cs:459` | `Unit("E3DMate2",1,2,8)` | ④b 负例：acting=**另一个友方** ⇒ `ta=20 self` 不通过 | **笔误** | **5** | 无 |
| 21 | `RuleEngineTest_EngineE3.cs:477` | `Unit("E3DMate3",1,2,8)` | ④b 灭自证：acting=null ⇒ 判不了 ⇒ 放行 + 出声 | **笔误** | **5** | 无 |
| 22 | `RuleEngine/Editor/RuleEngineTest_EngineE8.cs:123` | `E8bAbilityCard("E8bAnySide",…)` · 部队（合成卡，带 ability） | ① `AbilityPickSideOf` 四档：`friendlyUnits=1`+`enemyUnits=1` ⇒ **Any(3)**（同排 `cEnemy`→2 / `cFriend`→3 / `cUnknown`→5） | **笔误**（`ProbeBattle`；4 只是「第三格」） | **6**（右侧 `5,6` 连续；左侧 `2,3` 连续） | 无（只比 `AbilityPickSideOf` 的返回值） |
| 23 | `RuleEngineTest_EngineE8.cs:265` | `E8bAbilityCard("E8bSelfAlly",…)` | ⑥ 友方**含自己**：点**施法者自己那一格** ⇒ 可点 + 可放 + 自愈 2→4 | **本意**（`CanUseAbility(ctx2, me2, **4**, **4**, out owner)` / `UseAbility(ctx2, me2, **4**, **4**)` 都显式用 4） | **3**，并把这**两行**的 `4,4` 一起改成 `3,3` | ⚠️ 漏改任意一处 ⇒ 「点自己那格」变成点空/点别人 ⇒ ⑥ 那三条红。**改的不是数字而是三处耦合** |
| 24 | `RuleEngine/Editor/RuleEngineTest_EngineP1.cs:226` | `TAU6 Gun Drone`（真卡 · TauEmpire · 部队） | W67T3：有 `TurnStart` 动画行的卡**照旧发 1 条**（对照 `GOF86`→2） | **笔误**（ctx 走 `Battle(...)`=`NewBattle`；4 只当「第二格」） | **5**（右侧；左侧 `GOF86` 在 2，本就有洞） | ⚠️ 该夹具**本来就有洞**（2 号有人、3 号空）；`ResolveAtTurn` 按**全槽**遍历（`EffectResolver.cs:8725+` 的 `for s < BoardSpec.Size`，**不跳 4**）⇒ 今天 `droneN == 1` 才过。改 5 后仍全槽可扫 ⇒ 计数不变 |
| 25 | `RuleEngine/Editor/RuleEngineTest_EngineX1.cs:249` | `Unit("Foe",1,2,4)`（**P1**） | ③ `ta=100` 常规格 + 尾巴 `CandidateAdjacentTo`（`mine`→3 / `ally`→5 在 P0） | **笔误**（挤掉**敌方**督军；4 只当「对面一格」） | **3** 或 **5**（P1 那一侧整侧空着） | 无（②③ 的 `false/true` 由「不同盘」与阵营闸决定，与格号无关）⇒ 但与 #14 同属「挤敌方督军」，**排第一优先** |
| 26 | `RuleEngine/Editor/RuleEngineTest_EngineX6b.cs:142` | 真卡 `ASH30` · 部队 | ③ 去重：同卡**第二次**触发 ⇒ 不再出声（前一批 `carrier`→3 / 5 / 6） | **本意**（`FireTriggerAtForTest(ctx, carrier2, Rally, **0**, **4**, …)` 把 4 当槽位传） | **7**，并把那行的 `0, 4` 改成 `0, 7` | ⚠️ 漏改 ⇒ 触发槽位对不上（只影响事件里的槽号与 `AnimFx`，断言只数 `AbilityFamilyGateWarningsForTest`）⇒ 今天**可能不红**，但事件槽号会指向别人 |
| 27 | `RuleEngine/Editor/RuleEngineTest_Order.cs:431` | 真卡 `TAU10` · 部队 | T21④ 回合末触发**按进场序**而不是**格号序**（夹具刻意 `6→5→4` 让两个键相反） | **本意**（注释写明「格号序（4 < 5 < 6）与进场序相反」） | **7 / 6 / 5**（保持「格号 5<6<7 与进场序相反」同一构型） | ⚠️ 断言比的是 `PlayOrder`、不比格号 ⇒ 改后仍成立；但**注释里那句 `4 < 5 < 6` 要一起改**，否则文档与代码打架（铁律 5）。另：`EndTurn` 会在有洞棋盘上跑，若触发 `Insert/Remove` 会刷 `WarnHoley` 的 `LogError` |

### 计数复核
- 笔误 **17**：#1 #2 #3 #10 #12 #13 #14 #15 #16 #17 #18 #19 #20 #21 #22 #24 #25
- 本意 **4**：#11 #23 #26 #27
- 无影响 **6**：#4 #5 #6 #7 #8 #9
- 合计 **27** ✓（与 §一 一致）

### 行号复核（写完后再查一次）
本波有多个写手同时在改 `BattleDriver.cs` / `CardView.cs` / `B97W19_Assert.cs` / `B98G1_Assert.cs` / `B98G6_Assert.cs` / `B99E9_Assert.cs` / `ItemDrawer*.cs` / `EffectResolver.cs`。**写完报告后又重跑了一次正则：仍是 27 处、行号逐个未漂**（2026-10-30 现读）。⚠️ 若之后有人改了这些文件，**用正则重数**、别拿本表的行号直接用。

### 已修先例（供照着做）
- `CardPresentation/Editor/B97W19_Assert.cs:195`：`B101_R1` 已把 `Place(ctx, 1, 4, …)` 改成 **`5`**，并在注释里留了完整理由 + **实测后果**（改前那条 280 路「声明攻击 OK、280 实得 0」= 压根没进被测的那条路）。这是本批**唯一**有「改前静默无效」实证的一处 —— 也说明这类笔误**真的会让断言空转**。
- 判据链条据此可复用：`DeclareAttackExecute`（`RuleCore.cs:6678`）里 target 就是 `Board[tgtSlot]`（现读 `:6686`）；`TryCancelAttackByUnits`（`:17409`）第一句要 `target.IsWarlord`。两处都**按格号取盘面**，所以「4 号格上是谁」直接决定被测的那一路跑不跑。

## 四、没查清 / 判不了的部分

1. **每一处「改 4 → 建议格」之后 `play check` 指纹会怎么变**：本轮**只读**、没跑 `ruleprobe`。绝大多数处改的是**夹具摆位**、不进产品路径 ⇒ 理论上指纹不变；但 `EnchantmentTemplateCheck` / `Order` / `EngineP1` 那几处会真的走 `PlayCard`/`EndTurn`/`ResolveAtTurn`，**改完要各跑一次对应宿主**。
2. **`WarnHoley` 的 `Debug.LogError` 会不会被自检宿主算成失败** —— 未查。`Ok()`/`Check()` 只认自己的计数，但 `BattleScene.Run` 之类若有日志即失真的断言，则 #11/#24/#27 这几处改前/改后都可能刷错误日志（改前 #24/#27 的棋盘**本来就是洞的**）。
3. **`ta=120 friendlyHero` 有没有在 27 处夹具里被真的走到** —— 没有逐卡追（这些合成卡的正文是 `Damage/Heal/Rally` 之类，未见 `friendlyHero` 档）。若将来给这些夹具加「打敌方督军」类效果，**必须**先修 #14 / #25。
4. **`B98G2_Assert` 那 6 处「无影响」的前提**是「该文件的 ctx 全是手搓 PlayerState」—— 我只核了这 5 个 ctx 的创建行（`:160/184/211/253/306`），**没逐方法确认之后没有别处给它补过督军**。若有人在同方法里 `ctx.Players[0].Warlord = …` 补回来，分类要翻。

## 五、已改：13 处 `4` → `5`（`B101_E1` 2026-10-30 执行）

白名单 6 文件 · **同一物理行替换**（每处 `git diff` 的 hunk 头都是 `-N +N`，**行数零变化**）。
⛔ 未动：`RuleEngineTest.cs` 那 4 处（归 `B101_R2`）· `B98G2_Assert` 6 处（无影响）· 4 处「本意」（只报）· 未动任何断言期望值 / 同一夹具里的 `3` 等其它摆放 / 产品 `.cs`。

| # | 文件:行 | 改前 | 改后 | 该夹具的督军是谁 | 判别式（把这处改回 4 ⇒ 哪条断言会空转/变绿） |
|---|---|---|---|---|---|
| 1 | `B97_ER_Assert.cs:127` | `Place(ctx, 0, 4, Unit("B97ERMate", 1, 2, 5))` | `…0, 5, Unit("B97ERMate"…))` | `ProbeBattle` ⇒ `RuleEngineTest.cs:2631` `HeroOf("FixtureWarlord","Ultramarines",2,30)` ↦ `RuleCore.cs:655` 写 `Board[4]` | **无现存判别式**：② 负例按 `actingCard` 的**归属**判（`AbilityActingGateCard`），与盘面无关 ⇒ 改回 4 仍全绿。可补结构性判别式：`CheckTrue(BoardSlots.IsContiguous(ctx.Players[0], out why), …)` |
| 2 | `B97_ER_Assert.cs:166` | `…0, 4, Unit("B97ERMate2"…))` | `…0, 5,…` | 同上 | 无（③ 比的是 `AbilityLevelCriteriaWarningsForTest` 的**计数差**） |
| 3 | `B97_ER_Assert.cs:199` | `…0, 4, Unit("B97ERHandMate"…))` | `…0, 5,…` | 同上 | 无（④ 走 `abilityHostCard/abilityOwnerSeat`，不读盘面） |
| 4 | `B99E9_Assert.cs:317` | `…0, 4, Unit("B99E9RallyFriend",1,1,3))` | `…0, 5,…` | `ProbeBattle` ⇒ `:2631` | 无（② Rally 正/负例按 `chosen` 归属 + `KeywordLayers(friend,…)` 判） |
| 5 | `RuleEngineTest_EngineE3.cs:249` | `…0, 4, Unit("E3GateMate",1,2,5))` | `…0, 5,…` | 同上 | 无（③ 比 `queued` 布尔与出声计数） |
| 6 | `RuleEngineTest_EngineE3.cs:348` | `Place(ctx4, 0, 4, Unit("E3OldMate"…))` | `…0, 5,…` | 同上（`ctx4` 也是 `ProbeBattle`） | 无（③ 旧表那一格只看「不筛」） |
| 7 | `RuleEngineTest_EngineE3.cs:415` | `Place(ctx, 0, 4, Unit("E3MateAtk",1,3,8))` | `…0, 5,…` | 同上 | 无（④a 负例比 `mateAtk.Health`；`BroadcastWhen` 是**直接广播**，不过交战的合法性/相邻判定） |
| 8 | `RuleEngineTest_EngineE3.cs:441` | `…0, 4, Unit("E3DMate",1,2,8))` | `…0, 5,…` | 同上 | 无（④b 比 `SumHealth(ctx, 1)` —— 那是 **P1** 的盘，本处动的是 P0） |
| 9 | `RuleEngineTest_EngineE3.cs:459` | `…0, 4, Unit("E3DMate2",1,2,8))` | `…0, 5,…` | 同上 | 同 #8 |
| 10 | `RuleEngineTest_EngineE3.cs:477` | `…0, 4, Unit("E3DMate3",1,2,8))` | `…0, 5,…` | 同上 | 同 #8 |
| 11 | `RuleEngineTest_EngineE8.cs:123` | `Place(ctx, me, **4**, E8bAbilityCard("E8bAnySide"…))` | **`6`**（⚠️ **5 已被同一夹具的 `cUnknown` 占**，见下一行 `:124`） | 同上（`me = ctx.Active` ⇒ 0 或 1） | 无（① 比 `AbilityPickSideOf` 的返回值，纯对象判） |
| 12 | `RuleEngineTest_EngineP1.cs:226` | `Place(ctx, 0, 4, gunDrone, exhausted: true)` | `…0, 5,…` | `Battle(...)` ⇒ `RuleEngineTest.cs:2581` `Hero("FixtureWarlord",2,30)` ↦ `RuleCore.cs:655` | 无（`droneN == 1` 靠 `ResolveAtTurn` 的**全槽遍历**，`EffectResolver.cs:8785` 附近 `for s < BoardSpec.Size`，**不跳 4**）⇒ 改回 4 也仍是 1 |
| 13 | `RuleEngineTest_EngineX1.cs:249` | `Place(ctx, 1, 4, Unit("Foe",1,2,4))` | `…1, 5,…`（P1 侧 5 空着；`:247` 的 `ally` 在**P0** 5，不冲突） | `Battle(...)` ⇒ `:2581`；本处挤掉的是 **P1** 的督军 | 无（②③ 的 `false/true` 由「不同盘」+ 阵营闸决定；`CandidateAdjacentTo` 先比盘属 ⇒ 4 或 5 一样 `false`） |

### 这 13 处有没有「改完断言会变红」的？
**没有一处需要停下上报。** 逐条核过三条可能变化的通道：
1. **相邻关系**：`3` 与 `4` 相邻、`3` 与 `5` **不相邻**。13 处里唯一吃相邻的是 **#13**（`ta=100` 尾巴 `CandidateAdjacentTo`），而它先比「候选与载体是否同一盘」⇒ `P0` vs `P1` 恒不相等 ⇒ **false 不变**；其余 12 处的效果文本（`Damage/Heal/Rally/TurnStart`）都不带相邻。
2. **`BroadcastActivateMinions` 跳过 4 号格**（`RuleCore.cs:16500`）：改后部队挪到 5 ⇒ 它**开始被重算**（改前被当督军跳过）。13 处里没有任何一条断言读「行动态/攻击态」⇒ 无红。反过来 `EndTurn/BeginTurn` 只在 #12（`ResolveAtTurn` 之前）与 #13（`ToP1Turn` 之后才摆牌）出现，都不覆盖摆位。
3. **`AddSide` / `SumHealth` 的全盘扫描**：`SumHealth` 只在 #8/#9/#10 用到，且扫的是 **P1**（本处动的是 P0）⇒ 数值不变。

⇒ 结论：**13 处都是「消除夹具与引擎模型的偏离」的预防性修复，不改任何断言的期望结果**。⚠️ 但**未跑 Unity**（本轮红线）⇒ 上述是**逐条读码推理**，不是实测绿灯。

### ⚠️ 顺手发现（**未改，只报**）：改完以后仍有 9 个夹具的棋盘「不连续」
`BoardSlots.IsContiguous` 的判据是「督军在 4 号格」**且**「每侧从贴督军那格（3 / 5）起连续、外侧不许有洞」。我这 13 处只修了**督军格**这一半；按 `CountOnSide` + `SlotOf` 的规则逐个推下来，改完之后：

| 仍不连续 | 原因（与本次无关的旧摆法） |
|---|---|
| #1 / #3 / #4 / #5 / #7 / #8 / #9 / #10（**8 处**） | 敌方那张卡摆在 **P1 的 1 号格**（`Place(ctx, 1, 1, …)`）—— 1 号格有人而 3、2 号格空着 ⇒ `IsContiguous` 报「左侧 1 号格有人，但它在连续段之外」。⇒ **这是另一族夹具笔误**（要摆就摆 3 或 5 起连续），本批按指令**没动**（不在 `Place(...,4,...)` 这条线里） |
| #12 `EngineP1:226` | 同一夹具里 `GOF86` 摆在 **P0 的 2 号格**（`Place(ctx, 0, 2, gof86)`），3 号格空着 ⇒ 左侧有洞。改我这一处（4→5）只恢复督军，**这个洞还在** |
| 其余 #2 / #6 / #11 / #13 | 改完后**两侧都连续** ✓ |

⇒ 建议另开一条 `Place(..., 1/2, ...)`（以及其它「外格有人、内格空」）的同类普查；本报告不改。

### 顺带：§四③（`ta=120 friendlyHero` 会不会被这几处夹具走到）—— 已定
**不会。** 现读证据：
- 这 6 个文件里 `ta=120` 的唯一来源是「正文写 `your Warlord` / friendly hero」；13 处夹具的卡全是**合成卡**（`Unit(...)` / `BareCard` / `E3Synthetic` / `E8bAbilityCard`，正文是 `Damage 2 EnemyUnit` / `Heal 2 FriendlyUnit` / `Heal 3 Self` / `Rally` / 空串）**或三张真卡**（`TAU6 Gun Drone`「At the start of your turn, deal 2 damage to a **random enemy**」· `GOF86 Unleashed Tramplasquig`「…attack a **random non Flying enemy**」· `ASH30` 那处属 `EngineX6b`，不在本次 13 处内） —— **没有一张打「己方督军」**。
- 另一条旁证：`TargetCriteriaEval` 的 ④ 格 `excludeFriendlyHero/excludeEnemyHero` 在 `B99E9_Assert.cs:88` / `EngineE8:78` / `EngineX1:272` 三处夹具的 `CountSetSpec` 里**显式写 0** ⇒ 那两格不参与判定。
⇒ 所以 §二① 那条「部队被当成己方督军」的失真，在这 13 处（以及 27 处全体）**今天都碰不到**；它仍是「最该先修」的理由（改完之后 `Board[4]` 重新是督军，这条通道才对将来安全）。
