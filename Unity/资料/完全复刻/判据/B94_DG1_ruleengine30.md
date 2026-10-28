# B94_DG1 · `RuleEngineTest.Run` 30 条失败逐条定性

> **基线**：`d:/4/_tmp_view/ruleengine.log`（**2026-10-29 03:14** 收尾，`10584 / 10614`）。
> 同批源文件 mtime 全部 **≤ 02:25**（`RuleCore.cs` 02:25:17 · `EffectResolver.cs` 02:25:40 ·
> `RuleEngineTest.cs` 02:25:46）⇒ **日志与本报告读的是同一份快照**，行号可直接引用。
> 本报告**只诊断、零代码改动**。

---

## §一 汇总

| 档 | 条数 | 说明 |
|---|---:|---|
| **α 断言过时**（实现对、期望值旧） | **4** | #8 #9 #10（T26 泵点计数）· #12（卡表 version） |
| **β 实现缺陷**（我们真错了） | **1** | #28（`GetTraitFromAbilityTrigger` 欠 `Enum.ToString` 的数值兜底） |
| **γ 本批回归**（本会话改动打破） | **11** | #1 #2 #3 #5（誓约双扣）· #6 #7 #11（`ResolveAtTurn` 被泵队）· #4（`TransformUnit` 新入口）· #13 #14（破坏计数口径）· #15（覆盖闸排在 `Step` 之前） |
| **δ 夹具前提不成立** | **14** | #16–#27（裸 `ApplyDamage` 不走离场结算）· #29 #30（对照卡 `abilityTriggersVersioned=false`） |
| **合计** | **30** | |

**四条根因**（14+11 条里 20 条出自这四条，修完即大面积转绿）：

1. **誓约被收两遍钱**（#1 #2 #3 #5）—— `PayCardPlayedCostOath` 与 `ChargePaidGroups` 重复扣。
2. **`ResolveAtTurn` 裸调开始泵队**（#6 #7 #11）—— 新加的 `BroadcastTurnStartHandHops` 自带 `ActionScope`。
3. **夹具用裸 `ApplyDamage` 当「打死」用**（#16–#27）—— 它明文不跑离场结算。
4. **合成夹具没跟上「口径改数据面」的两处**（#13 #14 的 `SpellType` · #29 #30 的 `versioned`）。

---

## §二 逐条（30 行）

| # | 断言文案（截断） | 期望 / 实得 | 归档 | 证据 | 该怎么修 |
|---:|---|---|---|---|---|
| 1 | ★ **oath 效果真的生效**（友军 +1 生命） | 6 / **5** | **γ** | `EffectResolver.cs:2587`（F2 新加）＋ `RuleCore.cs:3945`（F1 新加）＋ `ChargePaidGroups:732`（`CostKindKnown:763` 的名单里**有** `"oath"`） | 见 §三·γ1 |
| 2 | ★ **只付卡费 1**（2 → 1；誓约那 1 点没扣） | 1 / **0** | **γ** | 同上 | 见 §三·γ1 |
| 3 | ★ 誓约效果照旧生效（与改前逐字一致） | 6 / **5** | **γ** | 同上 | 见 §三·γ1 |
| 4 | ★★【A1990】没有清单外的新单位落地入口 | 0 / **1** | **γ** | 新件 `TransformUnit_B94B12.cs:251`（`new UnitState(`）；pinned 表 `RuleEngineTest.cs:30123-30145` | 见 §三·γ4（⚠️ **先补钩子**再加名字） |
| 5 | ★ **能量只掉 3**（1 卡费 + 2 誓约） | 17 / **16** | **γ** | `EffectResolver.cs:2587`（本该只收 2，多收 1） | 见 §三·γ1 |
| 6 | ★ T24 裸调 `ResolveAtTurn` ⇒ 敌方督军一点血都没掉 | 30 / **28** | **γ** | `EffectResolver.cs:8828` / `:8909` 新挂 `BroadcastTurnStartHandHops`；其体内 `using (BeginAction(ctx))` 在 `:18882` | 见 §三·γ2 |
| 7 | ★ T24 那条 at-turn 作业**在队列里** | True / **False** | **γ** | 同上（队列被 `:18882` 的作用域退出时抽干） | 见 §三·γ2 |
| 8 | ★ T26 `RuleCore.cs` 的 `BeginAction` = 19 | 19 / **20** | **α** | 现读 20（本地按 `W61CountPumpPoints` 正则复算一致）；多的一处 = `RuleCore.cs:17050`（`ApplyDamage(…, out killedByVulnerable)` 壳，B94_B22） | 期望改 **20**，文案补一行「v+1 = B94_B22 的 `ApplyDamage` 取易伤死因的壳」 |
| 9 | ★ T26 `EffectResolver.cs` = 4 | 4 / **7** | **α**（含 γ 依赖） | 现读 7 = 4 旧 + `:18841 HandHops` + `:18882 BroadcastTurnStartHandHops` + `:18983 BroadcastUnitDamagedHandHops`；其中 **`:18882` 是 γ2 的病灶** | 先做 γ2（删 `:18882` 那层）⇒ 现读 6；期望写 **6**，文案点名 `HandHops` / `BroadcastUnitDamagedHandHops` |
| 10 | ★ T26 **泵点总数 = 23** | 23 / **27** | **α**（同上） | 20 + 7 = 27；修完 γ2 = 20 + 6 = **26** | 期望写 **26**（并写明「≠ 23 的两处是 B94_Z2 的共用口 + B94_B22 的壳」） |
| 11 | 🧪 W67T3 夹具自检（隔离局）作业**排进了队列** | 1 / **0** | **γ** | 同 #6（`RuleEngineTest_EngineP1.cs:260` 数的是 `ctx.ActionQueue.Count`，被 `:18882` 泵空） | 见 §三·γ2 |
| 12 | ★ A1 卡表 `version` = 39 | 39 / **40** | **α** | `Resources/cards_engine.json` 现读 `version=40`（`count=1129`）；`工具/gen_cards_engine.py:6439` 也是 40（v40 = `abilityBuffs` 列 · B94_B23） | `RuleEngineTest_EngineD4.cs:101` 期望改 **40**，文案顶部补一条 v40 changelog |
| 13 | X18-1g 造 1 张破坏 ⇒ `Tally.SabotageOf(我方) == 1` | 1 / **0** | **γ** | `EffectResolver.cs:4216`（B94_B14 把判据从 subtype 词改成数据面 `SpellType == 230`）；夹具 `RuleEngineTest_EngineX18.cs:91-92` 只传了 `subtype: "Sabotage"` ⇒ `SpellType` 是 ctor 缺省 **-1** | 见 §三·γ3 |
| 14 | X18-1i `Ach_65` +1 | 1 / **0** | **γ** | 同 #13（`AddSabotage` 没被调到 ⇒ 成就格不动） | 见 §三·γ3 |
| 15 | `Run()` 真的跑遍了编译出来的 367 个用例方法 | 0 / **2**（少 `TestB94F4EngineGaps`、`TestF2B94`） | **γ** | 两条 `Step(...)` 在 `RuleEngineTest.cs:1333` / `:1337`，而覆盖闸 `{ VerifyRunCaseCoverage(); }` 在 **`:1278-1280`**（更早） | 见 §三·γ5 |
| 16 | 前提：两张都进了墓地、且墓地查询都查得到 | True / **False** | **δ** | `RuleEngineTest_EngineB94F1.cs:50-51` 裸调 `RuleCore.ApplyDamage`；该方法 doc `RuleCore.cs:8446-8447` 明写「少了两步：受伤触发 + **离场结算**」 | 见 §三·δ1 |
| 17 | 前提：它的 `DeathTurn = 1` | 1 / **-1** | **δ** | `RuleEngineTest_EngineB94W3.cs:283` 同因 | 见 §三·δ1 |
| 18 | 前提：`W3Prev` 的 `DeathTurn = 2` | 2 / **-1** | **δ** | `:292` | 同上 |
| 19 | 前提：`W3Now` 的 `DeathTurn = 3` | 3 / **-1** | **δ** | `:301` | 同上 |
| 20 | 前提：`DeadUnits` 里恰好 3 条 | 3 / **2** | **δ** | 同上（两笔在建**下一回合的** `BeginTurn` 清尸时才落账 ⇒ `DeathTurn` 被记成 2 / 3） | 同上 |
| 21 | 前提（复核）：`W3Early.DeathTurn` 仍是 1 | 1 / **2** | **δ** | 同上（`ctx.Turn` 在记账号那一刻被读成 2） | 同上 |
| 22 | 前提（复核）：`W3Prev.DeathTurn` 仍是 2 | 2 / **3** | **δ** | 同上 | 同上 |
| 23 | 前提（复核）：`W3Now.DeathTurn` 仍是 3 | 3 / **-1** | **δ** | 同上（第三次死亡**根本没被任何一次清尸扫到**） | 同上 |
| 24 | ★★ 真死**本回合** ⇒ 221 = true | True / **False** | **δ** | 依赖 #17–#23 的 `DeadUnits` 前提 | 见 §四（修完前提要重跑；仍红才转 β） |
| 25 | ★★ 真死**本回合** ⇒ 222 = true（析取项①） | True / **False** | **δ** | 同上 | 同上 |
| 26 | ★★ 真死**上一个回合** ⇒ 221 = false | True / **False** | **δ** | 同上 | 同上 |
| 27 | ★★ 两回合前死的 ⇒ 222 = false | True / **False** | **δ** | 同上 | 同上 |
| 28 | trigger 1225（Regiment）⇒ 1225 regiment | 1225 / **0** | **β** | `CardDefPartial_B94B16.cs:150-153`：`AbilityTriggerNameOf(1225)` = null ⇒ 直接 `return 0`；原版 `.c:36-45` 是 `Enum.ToString(param)` → lowerFirst → `Enum.TryParse<DefinedTrait>` | 见 §三·β1（⚠️ **连带要改 `RuleEngineTest_EngineB94B16.cs:86`**） |
| 29 | 🧨 前提：对照卡 `AbilityTriggerPresence("TurnSetup") == 0` | 0 / **-1** | **δ** | `RuleEngineTest_EngineB94B21.cs:43` 的 `noSetup` 没传 `abilityTriggersVersioned` ⇒ `CardDef.cs:2462-2466` 恒 `-1` | 见 §三·δ2 |
| 30 | 🧨 前提：对照卡 `AbilityTriggerPresence("Support") == 0` | 0 / **-1** | **δ** | `RuleEngineTest_EngineB94B21.cs:94` 的 `plain` 同因 | 见 §三·δ2 |

---

## §三 β 与 γ 单独展开（这两类要动代码，给最小改动）

### γ1 · 誓约被收**两遍**钱（#1 #2 #3 #5 · 4 条）

**机理（三句）**

1. `Oath N:` 在我们的文本层是**一个付费组**：`EffectText.StampPaidCost` 给组里每条 op 打
   `CostKind = "oath"`，钱由 `ResolveOps` 顶端的 `ChargePaidGroups`（`EffectResolver.cs:732`）
   一次收掉；`CostKindKnown` 里 `"oath"` 是**认得**的货币名（`:11455` 那一带）。
2. 本会话 `B94_F1` / `B94_F2` 又按原版 `.c` 补了两个调用点：
   `RuleCore.cs:3945`（单位卡）与 `EffectResolver.cs:2587`（战术卡），它们调
   `PayCardPlayedCostOath`（`RuleCore.cs:16960`）**无条件**再扣一次 `KwValue(oath)`。
3. 后果是**两件事同时坏**：
   - **钱多扣**（#2 `2 → 0`、#5 `20 → 16`）；
   - **誓约正文反而没了**：`PayCardPlayedCostOath` 跑在**入队之前**，先把余能烧到 0 ⇒
     轮到队列体里 `ChargePaidGroups` 收费时**付不起** ⇒ `paidOk[i] = false`
     （`ChargePaidGroups` 的 `ok[i] = false` 那支）⇒ 誓约组那两条 op **一条都不结算**（#1 #3 的 `+1 生命` 消失）。
     日志里那串「这份付费前缀**付不了**（余额不够）⇒ 同属它的其余效果**一并跳过**」就是它。

**一句话定性**：原版没有「付费组」这个概念（那边是**两个变体 SO**），`PayCardPlayedCostOath` 就是
**唯一**的誓约扣费点；我们的文本层**已经**用一个付费组实现了它 ⇒ 再补一次调用 = 双计。

**最小改动（推荐 A）**

A. 在 `PayCardPlayedCostOath`（`RuleCore.cs:16960`）里加一道「**这一格是否已由付费组收过**」的闸：
`inst.Card.Desc` 解析后若存在 `op.Cost > 0 && EffectText.CostKindOf(op.CostKind) == "oath"`
⇒ **跳过 `SpendNormalMana`**，**仍然**执行末尾那句
`CheckCodex(ctx, p, inst, shouldCheckCost: false)`（`.c:24` 那一跳，与收不收钱无关）。
一个改动点同时覆盖 `RuleCore.cs:3945` 与 `EffectResolver.cs:2587` 两个调用点。
（若能接受改签名，更好的形状是调用点把「我已经解析过、组会去收」这个事实传成
`bool alreadyPaidByGroup:` 参数，省掉二次 `Parse`。）

B.（**不建议**）删掉那两个调用点 —— 会连带丢掉 `.c:24` 的 Codex 检查，而且将来带
`oath` trait 但卡面**没有** `Oath N:` 前缀的卡会彻底不扣誓约。

**验收判据**：改完这四处应当**一起**变绿（#1 #2 #3 #5），并且
`TestS14CostSharedChargesOnce` 的「只有 −3」「第二条不再收」两条仍绿（它们钉的就是**只收一次**）。

---

### γ2 · `ResolveAtTurn` 裸调**开始泵队**（#6 #7 #11 · 3 条）

**机理**

- `B94_Z2`（账 `B94_Z2`）在 `ResolveAtTurn` 里新挂了两次 `BroadcastTurnStartHandHops(ctx)`
  （`EffectResolver.cs:8828` 的 `total == 0` 早退支、`:8909` 的正常收尾）。
- 该方法体自己带着 `using (BeginAction(ctx))`（**`EffectResolver.cs:18882`**）。
  `ActionScope` 的 `_outermost` 判据是 `!ctx.ActionQueue.Resolving`（`ActionPump.cs:83`）——
  **裸调 `ResolveAtTurn` 时没人包外壳 ⇒ 它成了最外层** ⇒ `Dispose()` 里
  `PumpActionQueues`（`ActionPump.cs:96`）把**刚入队的那条 at-turn 作业当场结算掉**。
- 症状与 T24 的契约**逐字相反**：`foe.Health 30 → 28`（#6）、`ActionQueue.Count > 0` 变 0（#7）、
  W67T3 隔离局的「作业排进了队列 = 1」变 0（#11）。

**为什么生产路径看不出来**：`BeginTurn` / `EndTurn` 各自已经包了 `ActionScope`
（`RuleCore.cs:1664` / `:2296`）⇒ 这一层在真对局里是**嵌套 no-op**。所以这是**只被裸调夹具看见**的回归 ——
但 T24 钉的正是「公开入口只入队，泵点由调用方负责」这条契约（`RuleEngineTest_Priority.cs:146-157` 的 doc：
「这一条钉的正是 `BattleScene.cs` 那两处裸调」），所以**契约不能丢**。

**最小改动**：删掉 `BroadcastTurnStartHandHops` 里那层 `using (BeginAction(ctx))`
（`EffectResolver.cs:18882`，改成与其它广播同形的裸循环）。
理由三条：① 产品调用方**本来就有**外壳；② 它唯一的两个调用点都在 `ResolveAtTurn` 内部，
而 `ResolveAtTurn` 的两个生产调用点（`BeginTurn` / `EndTurn`）都在作用域里；③ 删完之后
手牌两跳照旧**入队**，只是**等调用方的泵点**结算 —— 与「棋盘作业」那条路完全同形。
⚠️ 现有一处直调 `RuleCore.BroadcastTurnStartHandHops(ctx)`（`RuleEngineTest_EngineB94Z2.cs:167`），
它的夹具「入队 = 0」⇒ **不受影响**。

**连带**：T26 的 `EffectResolver.cs` 计数 7 → **6**（见 §二 #9）。

**判不了的部分**：`HandHops`（`:18841`）与 `BroadcastUnitDamagedHandHops`（`:18983`）那两处作用域**保留**
（它们只经 `BroadcastWhen` 到达，绝大多数调用点在作用域内；今天没有任何断言因此变红）——
若将来发现某条 `BroadcastWhen` 的裸调用方，同样要按本条处理。

---

### γ3 · 破坏计数改了口径，夹具没跟上（#13 #14 · 2 条）

- 实现侧是对的：`B94_B14`（`EffectResolver.cs:4216`）按 `_ResolveCreateHandCard_d__512__MoveNext.c:90`
  把闸从「subtype 词」改成**数据面 `rawCardData.spellType == 0xe6(230)`**，并注明现池两口径等价（各 4 张）。
- 夹具侧失灵：`RuleEngineTest_EngineX18.cs:91-92` 的 `sab` 只写了 `subtype: "Sabotage"`，
  `CardDef` ctor 的 `spellType` 缺省是 **-1**（`CardDef.cs:912` / `136`）⇒ 闸不成立 ⇒
  `AddSabotage` 没被调 ⇒ `SabotageOf` 与 `Ach_65` 都不动。
- **最小改动**：`subtype: "Sabotage", spellType: 230`（顺带补一条
  `CheckTrue(sab.SpellType == 230, "夹具：破坏卡的数据面 spellType = 230")` 作前提断言）。
- ⚠️ 同一文件里若其它合成卡（隐秘 `Secret` = 150）也被改过口径的闸吃，**一并补**（本批日志里没有红的，
  但 `CountKindMatches:11444` 与 `MatchesKind` 两套口径并存，改一处漏一处随时复发）。

---

### γ4 · `TransformUnit` 是新入口 —— 但**钩子还没核**（#4 · 1 条）

**这不是 α**：A1990 那条断言问的正是「**有没有新落地入口没核对钩子**」，它抓到的是本会话新建的
`TransformUnit`（`TransformUnit_B94B12.cs:251` 的 `new UnitState(`）——**它抓对了**。

**核对结论（本代理现读，只读）**：原版 `ResolveTransformUnit.c:1207` 调
`MinionPlayedIntoField(..., keepStatus, **shouldTriggerSummon: 1**, 0)` ⇒ 召唤那两跳
（`BroadcastUnitSummoned` → `OtherUnitSummoned(190)` / `ThisUnitSummoned(195)` / `UseFaith(605)`、
以及 `550 Support`）**原版会发**。
而 `TransformUnit_B94B12.cs:272-281` 只做了
`Auras.Recompose`（`:274`）· `ApplyDeployTurnState`（`:275`）· `CheckStun`（`:276`）——
**没有** `PreCallBoardListener(Deploy)` / `BroadcastWhen(Deploy)` / Rally / `BroadcastSupportOnSummon`，
对比单位出牌那条路（`RuleCore.cs:4120-4160` 那一串）**整整缺一套落地广播**。

**处置顺序（⛔ 别只把名字加进 `pinned` 了事）**：

1. 先把 `ResolveTransformUnit.c:1207` 前后与 `MinionPlayedIntoField` 的实参逐格核完（本报告 §四 已列「还差什么」）；
2. 按结果把缺的落地广播补进 `TransformUnit`（这条本身应另立一条账，属于**β 向缺漏**）；
3. **再**把 `"TransformUnit"` 加进 `RuleEngineTest.cs:30123` 的 `pinned`，条目文字写
   「③ 变身（原版 `ResolveTransformUnit.c:1076/1207`，`shouldTriggerSummon=1`）」。
   （`pinned` 的值只是给人看的说明，扫描器认的是方法名 `TransformUnit`。）

⚠️ 现池 `chosenAbility == 85`（transformUnit 的生产端）**0 张** ⇒ 休眠机制；按铁律 11 照建。

---

### γ5 · 覆盖闸跑在两条新 `Step` **之前**（#15 · 1 条）

- `VerifyRunCaseCoverage()` 记实口是 `_ranCases`（`RuleEngineTest.cs:1379` `_ranCases.Add(test.Method.Name)`），
  它比的是「**已跑过的名字** ↔ 反射清单」。
- 闸在 **`RuleEngineTest.cs:1278-1280`**（Run() 中段），而本会话补的两条接在
  **`:1333 Step(TestB94F4EngineGaps);`** / **`:1337 Step(TestF2B94);`**（闸**之后**）。
- ⇒ 这两条**确实跑了**（日志里能看到 `F2B94Case1…` 的一串 ✓），只是**闸看不见**。
- **最小改动**：把这两行 `Step(...)` **上移**到 `{ VerifyRunCaseCoverage(); }` 之前
  （即 `:1270` 之后、`:1272` 那段注释之前）。
- ⚠️ **别反过来把闸挪到末尾**：`gen_diag_runner` 只认「8 空格缩进的顶层语句」，
  闸那一行是**故意缩进深一层**的（`:1274-1277` 的注释），挪位置会撞产物对账。

---

### β1 · `GetTraitFromAbilityTrigger` 缺「数值兜底」（#28 · 1 条）

**原版逐句**（`d:/2/tools/decomp_full/CardTraitCollection__GetTraitFromAbilityTrigger.c:36-45`）：

```c
lVar4 = System_Enum__ToString(&local_28, 0);          // 把 boxed enum 转字符串
uVar5 = ToLowerInvariant(Chars(lVar4,0)) + Substring(lVar4,1);
cVar2 = System_Enum__TryParse<Int32Enum>(uVar5, local_res18, …);
return (cVar2 != '\0') ? local_res18[0] : 0;
```

**关键**：`Enum.ToString()` 对**不在枚举里的数值**返回**十进制字符串**（`"1225"`），
而 `Enum.TryParse<DefinedTrait>("1225")` **照样成功**并回 `1225`（数值字符串不需要是枚举成员）。
⇒ 原版 `GetTraitFromAbilityTrigger(1225) == 1225`（1225 = `DefinedTrait.regiment`），
即使 `AbilityTrigger` 枚举里 `Regiment` 是 **301**（`CardDef.cs:1930`）。

**我们的实现**（`CardDefPartial_B94B16.cs:150-153`）：

```csharp
string name = AbilityTriggerNameOf(abilityTrigger);
if (string.IsNullOrEmpty(name)) return 0;     // ← 病根：数值不在 AbilityTrigger 枚举里就 0
string camel = char.ToLowerInvariant(name[0]) + name.Substring(1);
return DefinedTraitNumber(camel);
```

**最小改动**：在 `return 0` 那一支补数值兜底（等价于 `Enum.TryParse` 接受数值字符串）：

```csharp
if (string.IsNullOrEmpty(name))
    return abilityTrigger;      // 原版 Enum.ToString(未定义值) = 十进制串 ⇒ TryParse 直接回该数
```

🔴 **连带必须一起改的断言**：`RuleEngineTest_EngineB94B16.cs:86`
`Check(CardDef.GetTraitFromAbilityTrigger(9999), 0, "不在 AbilityTrigger 枚举里的数值 ⇒ 0 none")`
—— 按原版这一条**本身就是错的**（`"9999"` 也会被 `TryParse` 接受 ⇒ 原版回 **9999**）。
它今天绿只是因为**实现也错**（两边一起错 = 「灭自证」的反面）。
⇒ 修实现的同时，把这一条改成期望 **9999**，文案写「`Enum.TryParse` 接受数值串，原版不校验成员身份」。

---

### δ1 · 裸 `ApplyDamage` 不走「离场结算」（#16–#27 · 12 条）

**机理**（三条硬证据，不必再猜）：

1. `RuleCore.ApplyDamage` 自己的 doc（`RuleCore.cs:8446-8447`）：
   「⚠️ 会**造成伤害**的地方请用 `Hurt`，别直接调这个 —— 它少了「受伤触发」和「**离场结算**」两步，
   漏掉就会出现「血是负的但人还在场上」。」
   ⇒ 它**只扣血**，`CleanupDeaths` 由 `Hurt`（`:9025`）在末尾调（`RemoveIfDead` → `CleanupDeaths`）。
2. 死亡记账本身**是队列动作**：`RuleCore.cs:10177-10180` 把
   `MinionDeathBookkeepingDeferred`（`:10226`，`DeadUnits.Add(… DeathTurn = ctx.Turn)` 在 `:10260`）**入队**，
   由泵点执行。2026-10-21 起就是这个形状（判据 → 那一段的注释：原版 `GoToCemetery` 在死亡协程最后一跳）。
3. 日志读数是**教科书级的「迟一拍」**：
   `early` 记成 **2**、`prev` 记成 **3**、`now` **-1**、`DeadUnits.Count = 2`
   ⇒ 前两笔是在**下一回合 `BeginTurn` 的清尸趟**里落账的（那时 `ctx.Turn` 已经是 2 / 3），
   第三笔**再没有清尸扫到**。这与「裸 `ApplyDamage` 尸体留在棋盘上、等下次 `CleanupDeaths`」完全吻合。

**最小改动（两处夹具，别动实现）**

- `RuleEngineTest_EngineB94W3.cs:283 / :292 / :301`：
  `RuleCore.ApplyDamage(ctx, uXxx, 9, "自检")` → **`RuleCore.HurtWithSourceForTest(ctx, uXxx, 9, "自检")`**
  （`RuleCore.cs:9172` 是公开的测试口，直接转发到 `Hurt`，自带 `ActionScope` + `RemoveIfDead`）。
- `RuleEngineTest_EngineB94F1.cs:50-51`：同上两处。
- 若坚持用 `ApplyDamage`（比如要精确控制扣甲前数值），就必须**紧跟**
  `RuleCore.PumpActionQueues(ctx);`（或整段包 `using (RuleCore.BeginAction(ctx))`）。

**注意**：改完之后 #17–#23 的三态前提应当全绿，`DeathTurn` 分别为 1 / 2 / 3；
而 #24–#27（221/222 谓词）是否跟着绿 —— **本代理判不了**（见 §四）。

---

### δ2 · 对照卡没开「版本化」列（#29 #30 · 2 条）

- `CardDef.AbilityTriggerPresence`（`CardDef.cs:2462-2466`）第一句就是
  `if (!AbilityTriggersVersioned) return -1;` ——「**-1 = 未知**，⛔ 未知不许读成「没有」」。
- 夹具 `RuleEngineTest_EngineB94B21.cs:43`（`noSetup`）与 `:94`（`plain`）用**默认 ctor**，
  没传 `abilityTriggersVersioned` ⇒ 恒 -1。夹具作者要的是「**明确没有**」= 0。
- **最小改动**：给这两张对照卡补
  `abilityTriggers: new int[0], abilityTriggersVersioned: true`
  ⇒ `presence == 0`，且下面「没 155 / 没 550 的卡一条都不发」那两条负例**仍然有鉴别力**
  （别改成直接把期望写成 -1 —— 那会让负例变成「未知 ⇒ 不发」，鉴不出「闸被删掉」）。

---

## §四 判不了的（差什么）

| 项 | 差什么 | 谁来做 |
|---|---|---|
| **#24–#27 的 221/222 谓词本身对不对** | 12 条前提塌了 ⇒ 谓词**一次都没被真正求值过**。要判 β/α，得先把 δ1 修好重跑；若那时仍红，再读 `ConditionEval.MeetsCard` 的 221/222 支 + 原版 `TurnDied` 那两个 case 的 `.c`（`.c` 本报告未读，⛔ 不猜）。 | 写手，重跑后 |
| **`TransformUnit` 到底该调哪些钩子**（#4 的前置） | 需要把 `BattleManager__ResolveTransformUnit.c:1207` 前后 ~100 行 + `MinionPlayedIntoField` 实参逐格核完（本报告只确认它**传了 `shouldTriggerSummon=1`**，没逐条列出「= 哪几跳」）。另外 `keepStatus` 与 `keepEffects` 同一个字段控制两件事（文件头 §一 已写），要确认 `ApplyDeployTurnState(nu, keepStatus: keepEffects)` 是不是该位唯一的消费者。 | 写手（判据代理先出施工单） |
| **T26 最终该写 6 还是 7** | 取决于 γ2 走哪条修法：删 `:18882` 那层 ⇒ **6**；若改成「保留作用域、反过来把 T24 的契约作废」⇒ 7。本代理**推荐前者**（T24 钉的是公开入口契约，不该为了一个内部 helper 让步）。 | 调度台裁定 |
| **`cards_engine.json` 的 `count` 已由 1128 → 1129** | v40 那一批除了加列还多了一张卡（`B94_B23`?）；本批日志里**没有**因为 count 变红的断言，但若有别处按张数钉死了基线，会是下一批的隐患。**差**：`gen_cards_engine.py` 的入池口径变更记录。 | 记账 |
| **`EffectResolver` 的 7 个泵点里 `HandHops` 是否真该自带作用域** | 它的三个调用者（`BroadcastTurnStartedHandHops` / `BroadcastUnitPoisonedHandHops` / `BroadcastUnitRegimentHandHops`）今天只从 `BroadcastWhen` 到达；`BroadcastWhen` 的**全部**调用点是否都在作用域内，本代理未逐个核（本批无红）。 | 下一批（按账） |

---

## §五 一句话结论

30 条里 **只有 1 条是真正的实现缺陷（#28）**；**11 条是本会话改动的直接后果**，
其中 **4 条（誓约）** 与 **3 条（`ResolveAtTurn` 泵队）** 各由一个**单点改动**修好；
**14 条是夹具前提不成立**（7 条出自「裸 `ApplyDamage` 当打死用」这一个习惯）。
先修 γ1 / γ2（两处代码），再修 δ1 / δ2 / γ3 / γ5（四处夹具），最后按裁定补 T26 / version 两个期望值，
30 条应当**一次清空**（#24–#27 需重跑确认）。
