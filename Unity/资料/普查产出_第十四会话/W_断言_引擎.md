# W_断言_引擎 —— `RuleEngine/Editor/RuleEngineTest.cs` 宿主那批断言的落地报告

> 第十四会话（2026-10-10）· 执行代理（写手）报告。
> **产出**：新建 `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_S14.cs`
> （**1678 行 / LF / 29 个方法 / 250 处断言调用**；`RuleEngineTest.cs` **一个字都没动**）。
> **验收**：`TMPDIR=/tmp/wf_as14 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**
> （**跑过两次** —— 第二次是在另一个写手改了 `Core/RuleCore.cs` 之后，仍然 0 错）。
> 🔴 **额外的验法（本轮独有）**：**把这 29 个方法整份拿到离线探针里真跑了一遍**（只编 `RuleEngine/Core` +
> `Data/SimpleAI.cs` + 本文件），**272 通过 / 0 失败**（250 处调用里，循环那几处（`#36` 的 5 条取样、
> `#55`/`#56` 的 9 张卡）各跑多次 ⇒ 执行数 272）。做法与产物见 §六。

---

## 一、方法名 → 规格表条目

| 方法名 | 断的是哪几条（规格表 `#` 号） | 新增/已有 |
|---|---|---|
| `TestS14SurvivorRescueAndDestroyBypass` | `#1` `#2` | 新增 |
| `TestS14EcstasyPrintedVersusGranted` | `#4`（`#3` **当对照**） | 新增（`#3` 已有） |
| `TestS14StunWatcherGainsAttack` | `#8` | 新增（补「攻 +1」这半步） |
| `TestS14ApplyDeployTurnState` | `#9` `#10` `#11` | 新增（`#9` 的**另一入口**） |
| `TestS14PoisonAtOwnTurnEnd` | `#12` `#13` | 新增 |
| `TestS14PoisonBranchDoesNotSkipStunGate` | `#14` | 新增 |
| `TestS14BastionAbsorbsWholeHit` | `#15` `#16`（含边界那格） | 新增 |
| `TestS14CounterIgnoresBastion` | `#17` | 新增 |
| `TestS14DropPodPool` | `#18` | 新增 |
| `TestS14DropPodTakesPrecedenceOverBastion` | `#19` | 新增 |
| `TestS14VulnerableWithBastion` | `#20` | 新增 |
| `TestS14SacrificeOnOwnTurnRescue` | `#21` | 新增（**改断法**，见 §四①） |
| `TestS14SacrificeNotTriggeredOnEnemyTurn` | `#22` | 新增 |
| `TestS14SacrificeNotTriggeredOnRealDeath` | `#23` | 新增 |
| `TestS14SurvivorTriggerAlwaysFires` | `#24` `#25` | 新增（`#24` **改断法**，见 §四①） |
| `TestS14WouldKillByEntries` | `#26` | 新增（**只落一半**，见 §四②） |
| `TestS14HandAndBoardInstancesDisjoint` | `#28` | 新增 |
| `TestS14PronounTargetSide` | `#33` | 新增 |
| `TestS14PronounSideUnknownStaysUnknown` | `#34` | 新增 |
| `TestS14PronounSideChains` | `#35` | 新增 |
| `TestS14ParseIsIdempotent` | `#36` | 新增 |
| `TestS14ScoreGivingChargeSide` | `#37` `#38` | 新增（`#39` 已有） |
| `TestS14KeywordNumberAndHasNumber` | `#40` `#41` `#42` `#43` | 新增 |
| `TestS14PaidPrefixCarry` | `#44` `#45` `#46` | 新增 |
| `TestS14OathActivationChargesOnce` | `#47` `#48` | 新增 |
| `TestS14CostSharedChargesOnce` | `#49` `#50` | 新增 |
| `TestS14FaithCheckCondition` | `#51` `#52` `#53` | 新增 |
| `TestS14WhenBodyTargetIsSubjectless` | `#54` | 新增 |
| `TestS14FaithCostKind` | `#55` `#56` | 新增 |

**写得下 46 条 / 跳过 10 条**（`#3` `#5` `#6` `#7` `#27` `#29` `#30` `#31` `#32` `#39`，理由见 §三）。

---

## 二、逐条清单

> 「判据出处」一栏给的是**原版反编译方法体**（第一权威，`D:/2/tools/decomp_full/`）——
> 期望值一律从那儿来，⛔ 没有一条是从我们自己的实现里读回来的（那就是自证）。
> 「灭自证」一栏写的是**本笔实际补的那一条**（规格表里写「报告未给」的，本笔**没有自己发明口径**，
> 如实标着「未给」）。

| # | 条目 | 写在哪个方法里 | 期望值的判据出处 | 灭自证那一半怎么做的 |
|---|---|---|---|---|
| 1 | 幸存者救回 | `TestS14SurvivorRescueAndDestroyBypass` ① | `CardScript__CheckIfDead.c:108-165`（`:113` 那个 `if` 为假 ⇒ `:160-165 UseSurvivor`）· `CardScript__UseSurvivor.c:29-42,71` | `object.ReferenceEquals(原来那份, 现在那格上的)` ＋ 断弃牌堆/`DeadUnits`/`DiedThisTurn`/`Death` 事件 |
| 2 | 摧毁路绕过幸存者 | 同上 ② | `BattleManager__ResolveDestroyUnit.c` 全文无 `CurrentSurvivor`/`UseSurvivor`/`CheckIfDead`（`CleanupDeaths` 的 `maySurvive` 形参注释） | 与 `#1` 走**两个不同入口**（`DoDestroy` vs `Hurt`）：去掉 `maySurvive &&` ⇒ `#1` 必红 |
| 4 | 运行时授予的 `ecstasy` 不触发 | `TestS14EcstasyPrintedVersusGranted` 夹具 A | `CardScript__ShouldTriggerEcastasy.c` 第一条守卫 `HasDefaultTrait(raw, 0x4e2)` | 夹具 **B（印在卡面）对照**：同一句伤害，一个触发 +1、一个不变；阈值摆成**正好跨过** `Card.EcstasyX`（实测 1）⇒ 退回 `u.Has` 必红 |
| 8 | 眩晕观察者 **+1 攻** | `TestS14StunWatcherGainsAttack` | `CardScript__Stun.c:48 AddTraitSilently(…,100,…)` + `BroadcastUnitStunned`（我们的落点 = `DoStun` 的 `BroadcastKeywordEvent(GetsStun)`） | 与既有那半（`WhenFired == 1` = **广播发过**）**成对**：广播发了而结算没落时，那一半仍绿、本笔红 |
| 9/10/11 | `ApplyDeployTurnState` 三格 | `TestS14ApplyDeployTurnState` | `CardScript._MinionPlayedIntoField_d__314__MoveNext.c:69-71`（`if (*(char*)(p+0x30)=='\0') *(char*)(lVar4+0x58)=1;`） | **`#10` 判别力 = 先把 `SummonSickness` 摆回 `true`**；另附一格「不预置 + `keepStatus:true`」实得 `false`（写进消息）证明判别力只来自那一句 |
| 12/13 | 毒：本方回合末摧毁 / `resistant` 豁免 | `TestS14PoisonAtOwnTurnEnd` | `CardScript__OnTurnEnd.c:110-113`（三条守卫）→ `:229-262` → `:258 DestroyUnit(…, deathType poison(40))` | **「B 方回合末不摧毁」那一格**：删掉「只扫当前行动方」那道守卫 ⇒ 只断 A 方时它照样绿 |
| 14 | 毒支**不跳**「摘闸门」那块 | `TestS14PoisonBranchDoesNotSkipStunGate` | `CardScript__OnTurnEnd.c` 三条守卫的跳转都落到 `:113` 标签、毒支机器码排在标签**之前**（无跳转指令）⇒ 直落 | `resistant`+`stun`+`StunnedAtStartOfTurn` 的同一回合末：**毒被豁免** 且 **眩晕闸门照摘**（两条合起来） |
| 15 | 堡垒吃整份伤害 | `TestS14BastionAbsorbsWholeHit` ① | `_ReceiveDamage…:126-156` + `RemoveBastionDamage.c`（`remaining >= 0` ⇒ 生命不动） | 格首记 `hp0` 并断 `Health == hp0`（只断「没死」的话「扣掉再补回来」也绿）＋ 恰好一条 `Hit{0}` |
| 16 | 堡垒打穿 / 边界 3 打 3 | 同上 ②③ | 同上（`remaining == 0` 那一格原版 `health += 0`、基础值钳 0） | 溢出量写错时 `#15` 仍绿而本条必红；边界那格单独钉「`bastion` 也摘掉」 |
| 17 | 反击不吃堡垒 | `TestS14CounterIgnoresBastion` | `_ReceiveDamage…:63` 的 `deathType == combatAttacker(10)` 守卫 | 删掉 `ignoreBastion:true` ⇒ 本条必红；且它与 `#15/#16` 走**不同入口**（`DeclareAttack` vs 直调 `ApplyDamage`） |
| 18 | 空投舱血池三格 | `TestS14DropPodPool` | `_ReceiveDamage…:60-61`（dropPod 优先）+ `:159-183` + `CheckIfDead.c:76-96`（池 <1 ⇒ `RemoveDropPod` 后 `return`） | **第 ② 格**：**同时**断「摘掉了」与「血一点没动」（只断 ①③ 时删掉摘除那句仍全绿） |
| 19 | 空投舱优先于堡垒 | `TestS14DropPodTakesPrecedenceOverBastion` | `_ReceiveDamage…:60-61` 那个 `if (dropPod) … else if (bastion<1‖dt==10) … else 堡垒` 的**次序** | 两个 `if` 对调 ⇒ 本条红而 `#15/#16/#18` 全绿 |
| 20 | 易伤 + 堡垒（建议·非必须） | `TestS14VulnerableWithBastion` | 同上（没打穿那一档，易伤被 `bVar17` 抑制，两边一致） | **未给**；⚠️ 只断「打不穿」这一种，⛔ 不把「打穿后少算易伤」写成期望（那是我们的缺口） |
| 21 | 献祭（本方回合救回） | `TestS14SacrificeOnOwnTurnRescue` | `CheckIfDead.c:152-158`（`HasCurrentTrait(0x1d6)` ∧ `IsPlayerTurn(bm) == card.isPlayer`）→ `TriggerSacrifice.c:41-43 OnTrigger(0xdc)` | 见 §四①：「攻 +1」那半**不可达**，改钉**事件本身**（`Trigger{sacrifice}` 有且排在 `Trigger{survivor}` 之前） |
| 22 | 敌方回合献祭不触发 | `TestS14SacrificeNotTriggeredOnEnemyTurn` | 同上那条 `IsPlayerTurn == card.isPlayer` | 删掉 `ctx.Active == p` ⇒ 本条必红（`#21` 仍绿） |
| 23 | 真死时献祭不触发 | `TestS14SacrificeNotTriggeredOnRealDeath` | `CheckIfDead.c:151` 那个 `}`（献祭在 `CurrentSurvivor < 1` 收尾**之后**，与真死互斥） | 「位置」的唯一判别式：挪到「真死」那支 ⇒ 本条必红 |
| 24 | `survivor` 触发 id（有正文） | `TestS14SurvivorTriggerAlwaysFires` ① | `CardScript__UseSurvivor.c:73-75 OnTrigger(0x1a4 = AbilityTrigger.Survivor = 420)`（排在 `:71` 之后） | 删掉 `CleanupDeaths` 末尾那条 `FireTriggerAlways(Survivor)` ⇒ 本条红、`#21-23` 仍绿；⛔ **不**只断 `Has("survivor")==false`（那是 `UseSurvivor` 的效果） |
| 25 | 没有正文也要表态 | 同上 ② | `UseSurvivor.c:43-46 HighlightTraitIcon` + `DisplayTriggerAnim`（在「有没有那条 ability」**之外**） | 换回 `FireTriggerAt` ⇒ 本条必红（没正文时不发事件），而 `#24`（有正文）两边都绿 |
| 26 | `WouldKillByEntries` 补 `Survivor`+`Bastion` | `TestS14WouldKillByEntries` | `EnoughPendingDamageToDieWithDamageValues.c` 的循环（`:161/206` 第一层清零、`:231-248` 幸存者当第二层）＋ 外层闸 `EnoughPendingDamageToDie.c:41-45` | 见 §四②：「`IsAlive` 一族」那半**没落**（会打死 `#1`）；落的是 `WouldKillByEntries` / `EnoughPendingDamageToDie` 的行为 |
| 28 | Hand/Board 实例不重合 | `TestS14HandAndBoardInstancesDisjoint` | 结构性不变量（规格表自标「结构性排除已足够」） | 未给；按**引用**比（`object.ReferenceEquals`），⛔ 不用 `HashSet` 默认相等性（防 `CardInstance` 哪天覆写 `Equals` 后静默变松） |
| 33 | 代词目标的侧（先行词明写） | `TestS14PronounTargetSide` | `EffectTargetSpec.PrevSide` / `ResolvedSide` 的 doc（`LinkPrevAntecedent` 从紧邻上一条抄侧） | 同时断 `ops[0].Target.Side == "enemy"`（先行词本身）—— `PrevSide` 改成从别处取时这一对不可能同时绿 |
| 34 | 代词判不出侧就不许编 | `TestS14PronounSideUnknownStaysUnknown` | 同上（`ResolvedSide` 的「其余 ⇒ `null`」那一支） | 与 `#33` 并排；「一切代词都填 `own`」的实现必红 |
| 35 | 代词侧连环顺延 | `TestS14PronounSideChains` | `LinkPrevAntecedent` 的第 ② 条（顺延上一条已抄到的侧） | 去掉顺延 ⇒ `ops[2]` 变「判不出来」而 `ops[1]` 仍绿（能分清哪半坏） |
| 36 | 解析幂等 | `TestS14ParseIsIdempotent` | 「回填（代词侧 / 相邻锚点 / `repeat.BaseOps`）不幂等 = 静默错」 | 未给（一红就是静默错）；连跑 3 次比**形状签名**，5 条取样 |
| 37/38 | AI 打点的「侧」 | `TestS14ScoreGivingChargeSide` | `AI__ScoreFromGivingCharge.c:11-14`（先 `CanActNow`、为假再读裸 `+0x58`）· `AI__ScoreFromCriteria.c:297-300 case 0x1e` | **同一格号、两边值不同（7 vs 4）**；`#38` 的 `any`/`null`⇒0 与 `"own"`⇒4 **并排**（挡「一律返 0」） |
| 40–43 | `KeywordTable` 的数/切分口 | `TestS14KeywordNumberAndHasNumber` | `CardTrait.GetNewTrait(…, defaultValue)` 那一族的形状（我们口径 = 「`':'` 之前那半段」） | `#40`/`#41` **方向相反**（恒 1 / 取整串都必红其一）＋ `#42` 钉兜底 1 ＋ `#43` 把 `HasNumber` 与 `Parse` 钉在同一个切分口 |
| 44–46 | 付费前缀跨句承接 | `TestS14PaidPrefixCarry` | 「一条 ability = 一个 cost ＋ 一串 logic」（`CardAbility.cs` / `ActiveAbility.cs` / `PayActiveAbilityCostOath.c:22` 只调一次 `UseMana`） | `#45`/`#46` 两条**反向沙包**（`Slay:` / `Pray:` 是另一条 ability 的开头 ⇒ 必须清零）—— 挡住「无条件继承」 |
| 47/48 | 誓约一次只扣一次钱 | `TestS14OathActivationChargesOnce` | 同上 + `ResolveOneCore` 的 `CostShared` 支（① 不查余额 ② 不再扣 ③ 不重复广播） | 前提先断 `OathOps.Count == 2`（只收一条时「只扣 3」会**假绿**）＋ 同时断「两条效果都发生」（目标死 + 拿到 `Shield`） |
| 49/50 | 静态形状 ＋ 结算只收一次 | `TestS14CostSharedChargesOnce` | 同 `#44` | **反向**：抹掉 `CostShared` ⇒ `Energy` 必掉两次（`UM93` −5 / `SOR6` −10）；同时断两条效果都发生 |
| 51/52/53 | `faithcheck` | `TestS14FaithCheckCondition` | `EffectText.MentionsFaith`（`faith` / `☀`）→ `CostKindOf` 同源；`ConditionHolds` 的 `case "faithcheck"` 读 `ps.Faith` | **两个字段必须反向**：主判据（5/99、6/0）＋ **两格反向对照**（99/5、0/99）把「读错字段」两个方向都堵死 |
| 54 | `When …` 执行层 `Subjectless` | `TestS14WhenBodyTargetIsSubjectless` | `CardDef.WhenTriggers[i].Ops[0].Target`（`EffectTargetSpec.Subjectless` 的 doc） | **断言钉在 `WhenTriggers` 这一层**本身就是判别式：⛔ 拿 `Parse(整条 desc)` 那一路去断会得到**恰好相反**的答案（实测那条 `Target == null`） |
| 55/56 | `☀` 的货币是 `faith`，扣 `ps.Faith` | `TestS14FaithCostKind` | `EffectText.CostKindOf` / `MentionsFaith`；结算层 `if (kind == "faith") ps.Faith -= op.Cost;` | **全池逐 op 归一后为空 = 0**（现测 `paidOps=89 / faith=25 / 空=0`）＋ **运行期**：真打 `SOR43`，**同时**断「信仰 −2」与「能量只掉卡费 1」 |

---

## 三、我跳过的（**已有、没写第二份**）

| # | 为什么跳过 | 我现读到的位置 |
|---|---|---|
| `#3` 狂喜（印在卡面） | **已有**：`FixtureEcstasy` 3→2 断攻 1→2（含「跨越」与「治回再打」两格） | `RuleEngineTest.cs:17543-17605`（`TestUnstableEcstasyCruelty` ②） |
| `#5` `③′` 打**带 `Remnant`** 的 2 血单位 ⇒ 晕挂在翻面后的残骸上 | **已有**（含 `!killed.IsStunned` 那半） | `RuleEngineTest.cs:4482-4501`（`TestAttackKeywords`） |
| `#6` `③″` 不带 `Remnant`/`Waystone`（真死）⇒ 一个被晕的都没有 | **已有**（整格在） | `RuleEngineTest.cs:4503-4522` |
| `#7` `③‴` `Waystone` 换成 `Remnant` ⇒ 同结果 | **已有**（整格在） | `RuleEngineTest.cs:4524-4538` |
| `#27` `AboutToAttack(0x118)` ⇒ 返回 `OK` 但不出伤害 | 🔴 **规格表的「新增」是假结论** —— 这条链（替身 / `TryRedirectAttackToBodyguard`）**已经有 4 格断言**（重定向落伤害、督军不掉血、打法不变、带 Stealth 照样挨）。规格表是 `grep "AboutToAttack"` 零命中**才判成「新增」的**，而那个串只在引擎注释里出现、RET 里断的是**行为** | `RuleEngineTest.cs:5636-5740+`（`FixtureBodyguard` ②③④，`TestA1154*`）· 实现 `RuleCore.TryRedirectAttackToBodyguard`（`RuleCore.cs:3560`） |
| `#29` `A1279` 那 21 张 `Slay` 消费者 | **断法待调度台定**（`R1` 只说「补真卡夹具」、没写断什么） | —— |
| `#30`/`#31`/`#32` 三个付费洞 | **宿主待调度台定**（`R7` 没点名宿主） | —— |
| `#39` 3 参重载那两格 | **已有**：`(ctx,0,3)==4f` 与 `ScoreOps` 含 `give fast` 那一档。缺的那半（`"any"`⇒0）**就是 `#38`**，收在 `TestS14ScoreGivingChargeSide` 里 | `RuleEngineTest.cs:23235` / `:23254`（`TestA1098SummonSicknessReads` Ⓒ） |

---

## 四、待调度台定

### ① `#21` / `#24` 的「攻 +1 / +2」那半 **今天不可达**（我改了断法，没有自己发明口径）

**实测**：拿一张 `keywords = {"survivor 3", "Sacrifice: Gain +1 Attack"}` 的卡，
`Card.Has("sacrifice")` 是真的，但 **`Card.TriggerOps("sacrifice")` 返回 `null`**；
`Survivor: Gain +2 Attack` 同理（`TriggerOps("survivor") == null`）。
**根因**：`CardDef.RoutableTriggers`（`Core/CardDef.cs:172` 那张表）里**没有** `survivor` / `sacrifice`
（也没有 `BodyKeywords` 那 20 个词里）⇒ `AddTriggerOp` 不收它们的正文。
而那些触发点走的是 `FireTriggerAlways`，它命中「`u.FxOps(kw) == null && u.Effect(kw) == null`」那一支
⇒ **只发 `EvtKind.Trigger`、不结算任何效果**。
⇒ 我把 `#21`/`#24` 的断法改成钉**事件本身**（有几个、先后次序），⛔ 没有把「攻 +1/+2」写成期望
（写下去就是把一个到不了的效果固化成断言）。**要不要把这两个词补进 `RoutableTriggers`，请调度台定**
（那是引擎源码，本轮黑名单）。

### ② `#26` 的「`IsAlive` 一族改成 `Health > 0 || CurrentSurvivor > 0`」**我没落**

`UnitState.IsAlive` 现在是 `Health > 0`（只读血量），而 `RuleCore.CleanupDeaths` 正是靠
`if (u == null || u.IsAlive) return;` **进**幸存者那一支的 —— 把 `IsAlive` 改成「有幸存者也算活着」，
**幸存者支路整段变死代码**（`#1` 立刻红，救回这件事再也不发生）。
⇒ 我判那句的正确读法是**「判死那一家」**（`EnoughPendingDamageToDie` / `WouldKillByEntries`），
而它们**已经**是 `health > 0 || CurrentSurvivor != 0` 的形状 ⇒ 我在 `TestS14WouldKillByEntries`
里断的是**它的行为**。**「`IsAlive` 字面上改不改」请调度台定**（要动 `RuleEngine/Core/UnitState.cs`）。

### ③ `#50` 里 `SOR6` 的 `instead` 语义（不是本笔的账，但会挡住 `#32`）

实测：打出 `SOR6`（`… 4 Energy: Give +2 [Attack] and +2 [Ranged] instead and Heal them 1`）之后，
我方单位近战从 1 变成 **4**（= 1 + 第一段的 +1 + 付费段的 +2）—— 卡面那个 `instead` **本该替换掉第一段**。
⇒ 本笔只断「付费那半真加上了（≥ 3）」并把实得值写进消息，**没有把 4 固化成期望**。
`#32`（「`instead` 跳过付钱那条」）的宿主若定在这里，这一格就是它的现场。

### ④ `#56` 第二半的「**逐分句**」口径

`R7` 原话是「全池**逐分句** `kind==''` 从 6 → 0」。`EffectText.Parse` 只暴露**逐 op** 的结果
（分句→op 不是一对一的）⇒ 我按**逐 op** 写的（现测 `paidOps=89 / 归 faith 25 / 归一后为空 0`）。
口径若必须逐分句，请调度台说明分句的取法。

---

## 五、顺手发现（**一条一句话 + 出处**；⛔ 一条都没顺手改）

1. 🔴 **`KeywordTable.Parse` 是「同名后写覆盖」而不是相加** —— 一张卡同时印 `Survivor 3` 与
   `Survivor: Gain +2 Attack` 时，后者的头（`HeadOf` = `Survivor`）**兜底值 1** 会把 `survivor` **冲成 1**
   （实测：救回后生命 **1** 而不是 3）。出处 = `Core/CardDef.cs:3012-3034` 的 `kws[name] = FirstNumber(HeadOf(item));`。
   本笔的 `#24` 夹具靠「把带数值那条写在数组最后」绕开，⛔ 没有把这个行为固化成期望。
2. 🔴 **`CardDef.RoutableTriggers` 里缺 `survivor` / `sacrifice`** ⇒ 这两个词的卡面正文**永远收不下来**，
   `FireTriggerAt` 那条路恒空转（详见 §四①）。出处 = `Core/CardDef.cs:172-200` + `:332-350`。
   今天全池 0 张卡带这两个词 ⇒ 不可观测，但**一旦有卡就会被静默吞掉**。
3. **`Survivor:` / `Sacrifice:` 的「正文」与「事件」今天不同步**：事件发得出来（`FireTriggerAlways`），
   正文不执行（见上条）—— 表现层会看到「触发了」，而数值上什么都没有。
4. **`SOR6` 的 `instead` 没有抑制第一段**（详见 §四③）：实测打出后近战 1 → **4**。
   出处 = `EffectText.Parse` 产出的 `ops[0]`（`give +1 attack and +1 ranged`，`Cost == 0`）与
   `EffectResolver.ResolveOneCore` 的 `replace` / `skip` 那一段（`Core/EffectResolver.cs` 顶部）。
5. ⚠️ **规格表的「状态」列有假结论（至少两处）**：`#27`（`AboutToAttack`）与 `#3` 的家族
   —— 都是**按字面串 grep** 得出的「零命中」，而断言断的是**行为**（行为已经在了）。
   ⇒ 建议以后 grep 关键词时同时 grep **行为名**（`Bodyguard` / `Redirect` / `Fixture*`）。
6. **规格表给的 `#1` 期望值 `Hit{Amount = -3}` 不成立**：在那套夹具下实得 **-6**
   （回血量 = `Health_after − Health_before` = `3 − (2 − 5)`）。已在断言消息里写清算式。
7. `#20` 那格：`DamageAfterReduction(vulnerable 2 + bastion 3, 打 3)` = **5** ⇒ 易伤确实折进了公式；
   所以「堡垒打不穿时血不动」不是因为「伤害本来就没算易伤」，而是原版那一档被 `bVar17` 抑制。
8. `#28`：`CardInstance` 目前**没有覆写 `Equals`**（否则按 `HashSet` 比会静默变松）——
   本笔已改成按**引用**比，将来覆写了也不会变松。

---

## 六、本轮怎么验的（**下批可复用**）

🔴 **离线跑整份测试文件**（不占 Unity 实例、不走 1–9 分钟编译）：

1. 建一个 scratch csproj（**仓库外/临时目录**，只 **读** 工程源码，⛔ 不改任何工程文件），
   `Compile Include` 三项：`工具/ruleprobe/Stub.cs`（Unity 的 `Debug`/`Random` 桩）
   ＋ `RuleEngine/Core/*.cs` ＋ `RuleEngine/Data/SimpleAI.cs`；
   再 `Compile Include` **我要验的那份测试文件**（绝对路径、只读）。
2. 另写一个 `S14Harness.cs`：`public static partial class RuleEngineTest` —— 把
   `Check`/`CheckTrue`/`CheckCode`/`Unit`/`Tactic`/`Battle`/`ProbeBattle`/`ToP1Turn`/`HandIdx`/`Board`/
   `Place`/`SlotOf`/`LogTail` **按正本同一语义**重写（`Check` 用的就是
   `EqualityComparer<T>.Default.Equals`，与 `RuleEngineTest.cs:596` 逐字一致），
   再给一个 `Main` 反射调 29 个方法、把结果写进 UTF-8 文件。
3. **重编 ≈ 1 秒、整轮 ≈ 2 秒**。本轮产出：**272 通过 / 0 失败**
   （第一轮就抓到 1 条真期望值写错 —— 就是那个 `KeywordTable.Parse` 覆盖坑）。

**产物**（临时件，不在交付物里）：
`D:/4/_tmp_as14/probe/{as14probe.csproj, Probe.cs, Fixtures*.cs, S14Harness.cs, s14_result.txt}`
＋ 四轮实测输出 `fixtures*_out.txt` / `out.txt`。

⚠️ **它只是旁证**：正式验收仍然是 `RuleEngineTest.Run`（断言的真宿主）。
本轮跑过的那几条与「纯工具/纯文档零自检」的例外无关 —— **本文件是 `.cs` 断言，属于要进 12 条自检的那种**，
按铁律 12「做完再跑」，由调度台在挂完 `Step()` 之后统一跑。

---

>⚠️ **2026-10-10 后续订正（铁律 5）**：本文 **§四①** 那条「`Survivor:` / `Sacrifice:` 正文今天收不下来」**已过期** —— 另一个写手当天把 `RoutableTriggers` 与 `BodyKeywords` 都补上了这两个词（`A1386`），`TriggerOps("survivor")` / `TriggerOps("sacrifice")` **现在都非空**；连带的 `RuleEngineTest_S14.cs:677-679` 那条断言也**已翻过来**（见 `W_断言_RuleCore五笔.md`）。
