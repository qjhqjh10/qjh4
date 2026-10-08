# RECON · 引擎 / 规则族 A 表账 · **现状现核**（第三会话）

> 只读现核代理 · 2026-10-18 · ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改任何仓内文件（本文件是唯一产出）
> 判据口径：**① 全量反编译 `d:/2/tools/decomp_full/` 方法体（第一权威）→ ② 解包资源字段 → ③ 本仓既有判据**
> ⚠️ **一律按符号认，⛔ 不按行号认**（本仓行号每轮都在漂；本文给的行号只是**读数时的坐标**）。

---

## 0. 一句话结论（可派活清单）

| 账 | 状态 | 一句话 |
|---|---|---|
| **`A858`** | 🔴 **未做**（裁定「不该挪」是对的，**那两件收尾活一件没做**） | 注释 `RuleCore.cs:3147` **仍写着「没查清」**；断言**不存在** |
| **`A947`** | 🔴 **未做** | `noncombatant` 全仓**零定义**（`KeywordTable` 里连常量都没有） |
| **`A963`**（止血件） | 🔴 **未做** | `criteria` 仍是 `HandTroopCriteria`（会**扩散给后进手牌的部队**） |
| **`A976`** | 🔴 **未做** | `LastCreated` **不在 `ResolveOps` 入口清**（现有 5 个 `Clear()` 点全在别处） |
| **`A974`** | 🔴 **未做** | 两处 `NotePlayed` 仍排在 `BroadcastWhen(Play)` **之前** |
| **`A962` ③** | 🔴 **未做** | `BroadcastWhen(Die)`/`FlushDeathWatches` 仍排在 `FireTriggerAt(Backlash)` **之前** |
| **`A962` ②** | ✅ **已做**（`DrawnThisTurn` 已挪到广播之后 + 判别式在位） | 见 `RuleEngineTest.cs:19690` |
| **`A973`** | ✅ **已修**（`Company Master` 降费真生效，走 `DrawReferentScope`） | 见 `RuleCore.cs:1153` + 断言 `RuleEngineTest.cs:19812` |
| **`A896`** | ✅ **已做**（守卫已改读**指针**，`dt = 0` 判别式齐全） | 见 `CardInteraction.cs:285` + `BattleScene.cs:2497` |
| **`A899`** | 🟡 **注释那一半已做**；演出那一半归 `A940` | `TutorialScript.cs:711-760` · `BattleDriver.cs:7956-7971` |
| **`A338`**（卡组子集 5 处 + `A895` 5 处） | 🔴 **未做**（漂号坐实）；「≈44」**不能派** | 见 §2 |
| `A949` / `A950` / `A954` | ✅ 已办（在 `资料/历史/本波已办结论_1018.md` 里）；⚠️ `A950` 三态对照仍欠 | — |

---

## 1. 逐条

### A858 —— 状态：**未做**（行为确实不用改；**要做的两件都还在**）
- 证据（现读）：
  ```
  RuleCore.cs:3147   //    还没查清的那一格：原版那个计数的写入点在哪 —— **没查清**（如实标着）。
  RuleCore.cs:3148   ctx.DiedThisTurn++;      // `For each one that dies …` 按它计数（回合开始清零）
  ```
  ⇒ ① **注释仍是「没查清」**（`R-ENG` 已把判据查实：写点 = 反噬入队那一跳）
  ② **断言不存在**（全仓 `DiedThisTurn` 只有 `RuleEngineTest.cs:4617/4623` 两条，量的是「打死一个 = 1」，**不管反噬期间**）
- 判据：`CardScript__TriggerUnitBacklashActions.c:70-82`（写 `turnDied`）+ `CardScript__HasDiedThisTurn.c` ⇒ **在「从棋盘移除」之前、更在「进墓地」之前** ⇒ 我们留在反噬之前是**对的**。
- 要改的落点：注释 = `RuleCore.cs` 的 `CleanupDeaths` 里 `ctx.DiedThisTurn++` 那一段（**按符号认**，别按行号）；断言 = `RuleEngineTest.cs` 的 `TestDeathAccountsAfterBacklash` 旁边**新开一条**。
- 会碰到谁：**零行为改动** ⇒ **不动任何既有断言**。
- 没查清：① 「计数」vs「每卡一个 `turnDied` 再数」**是否逐位等价**（要造「打死又被捞回」的用例）；② **变残骸那一支**原版也写 `turnDied`、我们不 `++`，**是否算偏离未判**（`R-ENG` §6·2）。

### A947 —— 状态：**未做**
- 证据（现读）：全仓 `grep -i noncombatant` = **0 命中**（`CardDef.cs` 里连 `NonCombatant` 常量都没有）；`cantattack` 的**查询时**禁令在
  ```
  RuleCore.cs:2035   if (attacker.Has(KeywordTable.CantAttack)) return RuleCodes.ErrNoAttack;   // 在 IsValidTarget 里
  ```
  ⚠️ **账上写的落点 `RuleCore.cs:2002` 已漂到 `:2035`**（按符号认：`RuleCore.IsValidTarget`）。
  ⚠️ **`CanAttackNow`（`RuleCore.cs:2106`）里两道禁令一道都没有** —— 原版是 `CanAttackNow` **自己**读 `0x96`/`0x370`。
- 判据：`CardScript__CanAttackNow.c:25`（`0x96` = 150 `cantAttack`）/`:27`（`0x370` = 880 `nonCombatant`）；`dump.cs:45721-45896` 枚举全表。原版 = **缓存位（`canAct`）+ 查询时两道禁令** ⇒ 与我们同构，**只差补 `noncombatant`**。
- 要改的落点：`CardDef.cs` 的 `KeywordTable`（加 `NonCombatant = "noncombatant"` + 进 `Implemented`，写法照 `:1917 CantAttack` / `:2269 Implemented`）· `RuleCore.cs` 的 `CanAttackNow`（补一条查询时禁令；`IsValidTarget` 要不要同步，**未裁**）· `GivePayload.Give`/`CardText` 词表若要「认得出」也各加一行（**今天没有卡面授予它**）。
- 会碰到谁：`RuleEngineTest.cs:1736` 那条 `cantattack` 断言附近新开夹具；**不动既有断言**。
- 🔴 **收口必须造夹具** —— 但**账上的前提一半错了（本轮实测）**：
  - `noncombatant`：全池 **0 命中**，**必须造合成夹具**；
  - `cantattack`：**不是 0 命中**！全池有 **1 张真卡**带它 —— **`GSC9 Concealed Explosives`**（`keywords = ["Can't Attack","Ambush"]`，`cards_engine.json`）。`R-ENG` 那句「各 0 命中」是**按小写串 grep 出来的假象**（原始词面是 `Can't Attack`，落到 `KeywordTable.Normalize` 才成 `cantattack`）⇒ **`cantattack` 那半边可以用真卡当夹具**。
- 没查清：`CanAttackNow` 与 `IsValidTarget` **哪一处才是落点**（原版两处合一，我们拆成了两个函数）—— 需先裁一句。

### A963（止血件）—— 状态：**未做**
- 证据（现读）：
  ```
  EffectResolver.cs:5249   AttachHandEffect(ctx, inst, o.Source, o.Payload, o.Duration,
  EffectResolver.cs:5250                    null, false, false, HandTroopCriteria);
  EffectResolver.cs:2764   static readonly EffectTargetSpec HandTroopCriteria = new EffectTargetSpec
  EffectResolver.cs:2765   { Raw = "(手牌里的部队)", Side = "own", Kind = "unit", Count = 0, Auto = true, };
  ```
  `AttachHandEffect` 把 `criteria` 原样存进 `entry.Target`（`EffectResolver.cs` 的 `entry` 初始化块），并**无条件登记进 `PlayerState.HandEffectRecords`** ⇒ 之后进手牌的部队靠 `RuleCore.SetupCardInHand` → `HandEffectFits` **都会吃上**（`RuleCore.cs:3750`）。
- 判据：`PlayerHand__AddHandEffect.c:88-139`（`:112-139` 按 `targetCriteria @+0x20` 逐张贴给 `currentHand`）；`PlayerHand__SetupCardInHand.c:58/71`（后进手牌的牌也按 criteria 补）。
- 要改的落点：**只那一处调用** = `EffectResolver.cs` 的 `BroadcastHandWhen` 里 `HandListenerSelfOp` 那一段的 `AttachHandEffect(...)`（**第三个参数组**：把 `HandTroopCriteria` 换成一个**带 `NameFilter = c.Name` 的规格**，`EffectTargetSpec.NameFilter` 定义在 `EffectText.cs:777`，`CardCriteria.FromTarget` 读它 → `CardCriteria.Name`）。
  ⚠️ 「只匹配它自己」**在卡模板这一层只能收窄到「同名卡」**（`CardCriteria.Matches` 比 `CardDef`）—— 第二份同名 `Rubric Marine` 进手牌仍会吃上，**这是这一层能做到的下限，要在注释里如实标**。⛔ **不是「不登记记录」**（`RegisterHandEffectRecord` / 那条记录要留着）。
- 会碰到谁：`RuleEngineTest.cs:4074` 那条 `HandEffectFits` 兵种筛断言一族（**只加负例，不改它们的期望值**）；⛔ **今天全仓没有 `Rubric Marine` / `BL15` 的测试** ⇒ 正例/负例要**新写**。
- 没查清：`BL15` 卡 SO 里的 `AbilityEffect` 是 40（`buffSelf`）还是 50（`buffHand`）—— 判据在**本地缺失的 `allcards` bundle**（`R-ENG` §6·1）。⛔ **不许照原版改**。

### A976 —— 状态：**未做**
- 证据（现读）：`ResolveOps` 入口清的是 `DrawnThisResolve` / `LastHandTargets` / `LastHandTarget` / `LastTargets` / `LastChosenCard`（`EffectResolver.cs:54/66/67/84/90`），**`LastCreated` 不在其中**；5 个 `Clear()` 点全在**别的 writer 的入口**：
  ```
  EffectResolver.cs:1825  ctx.LastCreated.Clear();   // DoCreate
  EffectResolver.cs:2301  ctx.LastCreated.Clear();   // DoChooseCard
  EffectResolver.cs:3739  ctx.LastCreated.Clear();   // DoDrawRef
  EffectResolver.cs:3825  ctx.LastCreated.Clear();   // DoReturn（回手那一支）
  EffectResolver.cs:3906  ctx.LastCreated.Clear();   // DoReturn（放回牌库那一支）
  ```
  ⇒ **`DoDraw` 不碰它** ⇒ 上一条 resolve 留下的残值**跨卡存活**，而 `DoLowerCost` 的 `(指代上一张)` 支是 **`LastCreated` 优先**（`:7001`：`ctx.LastCreated.Count > 0 ? ctx.LastCreated : ctx.DrawnThisResolve`）⇒ **抢班**（`S10` 真卡真池实测：抽到那张 `CostOf` 仍 5，被降的是 `Reanimation Protocol`）。
- 判据：**原版没有「上一批造出来的」这个跨卡窗口** —— `it/they` 一律是**上一条能力**的目标（`AbilityLogic.SetPreviousAbilityTargets.c:19-28` 先 `Clear()` 再 `AddRange()`），我们已在 `LastTargets`/`LastHandTargets` 上照抄了同一条口径 ⇒ `LastCreated` 漏了。
- 要改的落点：`EffectResolver.cs` 的 `ResolveOps` 入口（与 `ctx.LastTargets.Clear()` 同一段）；**注释订正在 `EffectResolver.cs:6997-7000`**（那段写着「`LastCreated` 在 `DoCreate` 入口清」⇒ **不成立**）。
  🔴 **账上把这段注释记成 `Core/BattleContext.cs:848-854` —— 指错了文件**；现读 `BattleContext.cs:848-861` 那段讲的是 `LastHandTargets`，**内容是对的**，⛔ 不用改。
- 会碰到谁（**这一格最要紧**）：**同一条 resolve 内跨 op 的指代必须逐个核一遍** —— 一号靶 = `Master of Manoeuvre`（`Return a friendly Vehicle to your hand. It costs 4 less`，断言 `RuleEngineTest.cs:3078-3132`）与夹具 `T_Return`（`:7184-7202`）。这两条都是**同一次 resolve 里 op1 写 / op2 读** ⇒ 入口清**不影响**（writer 自己会在写前 `Clear()`）。
  ⚠️ **风险点**：**嵌套 `ResolveOps`**（`Repeat this effect` / `Choose one`）会在**内层入口**清掉外层 writer 的残值 —— 但 `LastTargets`/`DrawnThisResolve`/`LastHandTargets` **今天已经是这个语义**，属同构，不是新增风险。
  ⚠️ **已知局限（要如实标）**：清入口**修不了**「同一条 resolve 内『先 create、后 draw』」—— 那要靠 writer 不覆盖，**这次不动**（`S10` 报的那条是真·跨卡的）。
- 没查清：**卡片池里有没有「同一条 resolve 内先 create 后 draw 再指代」的写法** —— 没做全池扫描（只核了 `S10` 报的那一张链）。

### A974 —— 状态：**未做**
- 证据（现读，**按符号找**）：
  ```
  RuleCore.cs:1442        ctx.NotePlayed(p, inst, false);          // 单位卡
  RuleCore.cs:1499        BroadcastWhen(ctx, WhenEventKind.Play, p, card, unit);
  EffectResolver.cs:1014  ctx.NotePlayed(p, inst, true);           // 战术卡
  EffectResolver.cs:1037  BroadcastWhen(ctx, WhenEventKind.Play, p, card, null);
  ```
  ⇒ **两处都是「记账在广播之前」**，与原版相反。
- 判据：`BattleManager._ResolvePlayCardFromHand__MoveNext.c` 的 `BroadcastCardPlayed`（`:1426`）在 `CemeteryManager.AddPlayCardAction`（`:1503`）**之前** —— ⚠️ 两点之间隔着若干分支，**没逐跳读完**（`S7` 也标「置信度中」）。
- 要改的落点：单位那一处**不能只挪一句** —— `PlayCard` 里 `NotePlayed` 之后还紧跟 `ApplyHandBuffs` → `Auras.Recompose` → `HasDeployExemption` 重算（`RuleCore.cs:1449-1462`），挪位要挪到 `BroadcastWhen(Play)` **之后、`BroadcastWhen(Deploy)` 之前**；战术那一处同理（挪到 `:1037` 之后）。
- 会碰到谁：**`A920` 一族**（账上要求同批排）。**本轮核过：那条依赖【复现不出来】** —— 唯一的收口断言是 `RuleEngineTest.cs:411 → :19172 TestPlayedCardsExcludesCompanionAndTide`，它量的是 `PlayCard` **返回之后**的 `PlayedCards.Count` 差；而 `NotePlayed` 全仓**只有这 2 个调用点**、没有任何**广播监听器**能在这段里再打一张牌（效果动词表里没有「打出」）⇒ 挪位**不改它的读数**。⚠️ 但**没跑 Unity**，仍建议与 `A920` 同批复跑 `RuleEngineTest.Run` + `BattleScene.Run`。
- 没查清：原版那两点之间**逐跳**是些什么（会不会中间已经写了别的日志表）。

### A962 ③ —— 状态：**未做**
- 证据（现读，同一条路径上，**次序仍是反的**）：
  ```
  RuleCore.cs:3148  ctx.DiedThisTurn++;
  RuleCore.cs:3155  ctx.Emit(EvtKind.Death, p, slot, u.Name);        // ← 表现层那一跳（要【单独裁】）
  RuleCore.cs:3162  BroadcastWhen(ctx, WhenEventKind.Die, p, u.Card, u);   // ← 「别的卡监听」
  RuleCore.cs:3167  FlushDeathWatches(ctx, u, p);                    // ← BL77 的追加登记（我们挑的）
  RuleCore.cs:3184  UnstableBlast(ctx);                              // ← 我们照 gd 的次序
  RuleCore.cs:3189  FireTriggerAt(ctx, u, KeywordTable.Backlash, p, slot); // ← 反噬
  ```
  ⇒ `BroadcastWhen(Die)` / `FlushDeathWatches` **仍排在反噬之前**（账上记的 `:3151/:3156/:3178` 已各漂 ±11~22）。
- 判据：`_ResolveBacklash_d__451__MoveNext.c`：`:88 ResolveDeadCard` → `:94 TriggerUnitDeathActions` → **`:144 BroadcastDeadUnit`（= 别的卡监听）** → `:175 FinishResolvingAction`；反噬入队 = `TriggerUnitBacklashActions.c:181`。⚠️ 方向**置信度中-高、非 100%**（「反噬伤害 vs `ResolveDeadCard` 谁先」在协程体里**没直接看见**）。
- 要改的落点：`RuleCore.cs` 的 `CleanupDeaths`（**按符号**）—— 把 `BroadcastWhen(Die)` + `FlushDeathWatches` 移到 `FireTriggerAt(Backlash)` **之后**。
- 会碰到谁：`RuleEngineTest.cs` 的 `TestDeathAccountsAfterBacklash`（`:17579`）—— 它量的探测器读 `ctx.DeadUnits`，而 `DeadUnits`/`Discard` 两笔账**已下移到反噬之后**（`RuleCore.cs:3228` / `:3241`）⇒ 挪广播**不改**它的读数；⚠️ 但 `UnstableBlast`（`:3184`）夹在中间，「自爆 vs 别的卡监听」的相对次序会**跟着变** ⇒ 必须同批核。
- 🔴 **单独裁的那一处**：`RuleCore.cs:3155` 的 `ctx.Emit(EvtKind.Death, …)` 是**表现层**那一跳（原版锚点是「趁格位还有意义时播阵亡特效」）—— **⛔ 别跟广播一起挪**。
- 没查清：`FlushDeathWatches`（`BL77 Spreading Corruption`）的次序是我们**自己挑的**（注释里明写）⇒ 要不要跟着挪**需先裁**。

### A962 ② —— 状态：**✅ 已做**
- 证据：`RuleCore.cs:1153-1154` `using (DrawReferentScope.Seed(ctx, inst)) BroadcastWhen(…Draw…)`；`RuleCore.cs:1181 inst.DrawnThisTurn = true;` ⇒ **记账在广播之后**（注释 `:1159-1180` 带逐行判据 + 「今天观测不到差异」的如实标注）。
- 判别式（**灭自证**，三条反制 + 两条结构断言）：`RuleEngineTest.cs:19690 TestS7BroadcastBeforeBookkeeping`（新次序⇒1 / 旧次序⇒0 / 找不到⇒−1；再扫真文件 `RuleCore.cs` 的 `Draw` 正文）。已挂进宿主（`RuleEngineTest.cs:453 Step(...)`）。
- 归账：**它会归到 `A962` ②的销账**（`S7` 交件）。

### A973 —— 状态：**✅ 已修**
- 证据：`RuleCore.cs:1153` 的 `DrawReferentScope`（保存/清/种/还原，`EffectResolver.cs:4720` 定义）⇒ `draw` 广播期间 `ctx.DrawnThisResolve` = **恰好刚抽到的那一份**。
- 断言：`RuleEngineTest.cs:19812 TestS10CompanyMasterLowerCost`（① 钉「种」：`Company Master` 在场、抽 5 费牌 ⇒ `CostOf` = 4；② 钉「还原」：`For each troop drawn` 仍数得到），已挂宿主（`:462`）。
- 归账：**`S10` 那一件销 `A973`**（`资料/普查产出_1018/S10_CompanyMaster降费.md`；⚠️ 它自己标了 `git diff` 里 S7 那两处**一个字没动**）。
- 没查清：`S10` 只做了离线变异验证，**没跑 Unity**（要 `RuleEngineTest.Run` 复跑一次才算收口）。

### A896 —— 状态：**✅ 已做**
- 证据：`CardPresentation/Hand/CardInteraction.cs:285` `if (LayoutSpace.ToNormalized(world).y < board.lineY)`（**量的是形参 `world` = 指针**，⛔ 不再是 `t.position`）；上方 `:263-284` 是 20 行判据注释（含「我们这道守卫原版没有对应物」的如实标注）。
- 断言：`CardPresentation/Editor/BattleScene.cs:2497` 起一整块 —— 前提三条 + **判别式**「指针抬到棋盘线以上 ⇒ 让位不该再跑」，**清一色喂 `dt = 0`**（`Mathf.Exp(0)` ⇒ 卡纹丝不动），并逐条写了 🧨 改坏法。
- 归账：销 `A896`（`R-ENG` 裁定一已落地）。
- 没查清：`HandLimitArea` 的 y=779 与我们 `BoardLayout.lineY` 的**换算**仍未验（`R-ENG` §6·3）—— 本件**没顺手换阈值**，是对的。

### A899 —— 状态：**🟡 注释那一半已做；落码那一半归 `A940`**
- 证据：`RuleEngine/Core/TutorialScript.cs:711-760`（战役 `GetCurrentWaitTime` / 教程 `GetDelay` / `SkippableActionWait` 三条语义全写进去了）· `CardPresentation/Battle/BattleDriver.cs:7956-7971`（**已订正**原来那句错的「教程那份 `GetCurrentWaitTime` 重载缺失」）· `:7998` 钉住 `CanSkipAction` 五档 = `IsTutorialChatKind` 五档。
- 要改的落点：**实际落码在演出节拍那一批**（`BattleDriver` 的 `_postTimer` 一族 / `A940`）—— 本条不再派。
- 没查清：`SkippableActionWait` 那条链在界面上的落点（`+0x228` 那个 UI 对象是谁）—— `R-ENG` §6·7；而且我们**根本没有「演出等待可被点击跳过」这一层**。

### `A338` 卡组子集 5 处 + `A895` 5 处 —— 状态：**🔴 未做**（漂号坐实）
- 证据（现读，**都还是旧行号**）：`Shell/PointerLayer.cs:12`（引 `DeckRuntime.cs:732`/`:1379`）· `Shell/MenuScroll.cs:29`（引 `:1106-1113`）· `Shell/CollectionWindow.cs:1924`（引 `:2982`/`:3110`）· `Editor/DeckScene.cs:2341`（引 `:1606-1610`）· `Editor/BattleScene.cs:5944`（引 `DeckRuntime.cs:2081`；⚠️ `R-ENG` 记的 `:5885` 又漂了 +59）。
- 要改的落点：一律改成**按符号认**（`X.cs` 的 `成员名`）；真身表 = `资料/普查产出_1018/R-ENG_引擎族7条现核.md` §4·A。
- ⛔ 「≈44 处」那批**不能派**（账本身不成立，见 `R-ENG` §4·C）。
- 没查清：`A895` 那 5 处（`Editor/BattleScene.cs`）**没逐处重读**（那文件正被别的写手改）。

---

## 2. §引擎族 里**仍开着**的其它条目（`资料/待办判据_1018.md` §497 起）

| 条 | 现核状态 | 落点 |
|---|---|---|
| `A948`（面板开着时被打出的牌**还在 `ps.Hand`**） | 🔴 **仍未判**（`D5` 没读 `_ResolvePlayCardFromHand` 那段方法体） | `RuleCore.PlayTactic` / `BattleDriver` 面板链 |
| `A951`（`chooseone`/`chooseeffect` 没有 `CardDef` 可指、只有计数盖着） | 🔴 **够不够没查** | `EffectResolver` 的 choose 支 |
| `A950`（压缩态夹具） | ✅ **夹具已在**（`BattleScene.cs:10517-10553` ⑥c `FillTo(9)`，量的是原版 2.0 世界单位 × 14.835）；⚠️ **账上「三态对照仍欠」仍成立**（要临时改 `HandLayout.cs` 再还原） | `Editor/BattleScene.cs` |
| `A949`（三词表并一处） | ✅ **已办**（`UnitState.cs:330` 已并；`EffectResolver.cs:4503-4512` 判「**不能并**」并留订正痕迹） | — |
| `A954`（第三入口 `DoReanimate`） | ✅ **已办**（`EffectResolver.cs:7246` `back.Exhausted = !RuleCore.HasDeployExemption(back);`） | — |
| `R-ENG` §5·2（**`oath`(1275) 原版也写 `canAct=true`**，我们 `HasDeployExemption` 三词表**没收 `oath`**） | 🆕 **仍未判**（机制坐实，`RuleCore.cs:1387`；「该不该并」无人裁） | `RuleCore.HasDeployExemption` |
| `R-ENG` §5·5（抽牌三件事原版发生在「已出牌库、还没进手牌」） | ⚠️ **已判「不成立」**（`RuleCore.cs:1176-1180` 那段已就地订正） | — |
| `R-ENG` §5·1（`0x82` = 130 = `jam`） | ✅ 判据已解开（`EffectResolver` 的 `HandEffectExpired` 注释里仍写「`0x82` 本笔没查清」⇒ 可顺手订正，**纯注释**） | `EffectResolver.HandEffectExpired` 注释 |

---

## 3. ⛔ 判据不足 / 需先裁 / 有依赖链

**必须先裁（不裁不能派）**
1. **`A947`**：禁令加在 `RuleCore.CanAttackNow` 还是 `IsValidTarget`（还是两处都加）—— 原版两处合一，我们拆成两个函数。
2. **`A962` ③**：`RuleCore.cs:3155` 的 `ctx.Emit(EvtKind.Death…)`（**表现层那一跳**）**单独裁**，⛔ 不许跟广播一起挪；`FlushDeathWatches` 同理（次序是我们自己挑的）。
3. **`A963`**：⛔ **不能照原版改**（`BL15` 的 `buffSelf(40)` vs `buffHand(50)` 在本地缺失的 `allcards` bundle 里）；**只能做已裁的止血件**。
4. **`A858`**：收口时要不要**顺手把「变残骸那一支我们不 `++`」也一起处理**（`R-ENG` §6·2 判「未判」）。

**必须串行（有依赖链）**
- **`A974` → `A920` 一族**：账上要求同批排（本轮核过依赖**复现不出来**，但仍建议同批跑）。
- **`A962` ③ → `A858` 的断言**：两条都动 `CleanupDeaths` 同一段、且都在「反噬 vs 广播」这条链上 ⇒ **同一个写手、同一次改**。
- **`A976` → 一号靶族**：动 `ResolveOps` 入口前，必须先把「同一条 resolve 内跨 op 的指代」全盘一遍（一号靶 = `Master of Manoeuvre` / `T_Return`）。
- **`A947` 的夹具** 依赖上面第 1 条裁定。

**判据不足（⛔ 别硬做）**
- `A318`/「≈44」那批（`A338` 本体）—— 账不成立。
- `A948` / `A951` —— 判据没读透。
- `A899` 那条「可跳过」的 UI 落点 —— `R-ENG` §6·7。

---

## 4. 顺手发现（**都没改**）

1. 🔴 **`A947` 账上的「各 0 命中」一半是假的**：`cantattack` 在池里有 **1 张真卡** —— **`GSC9 Concealed Explosives`**（`keywords = ["Can't Attack","Ambush"]`）。旧记录按**小写串** grep 才得 0。`noncombatant` 才是真的 0。⇒ **夹具那一半可以用真卡**，另一半必须合成。
2. 🔴 **`A976` 的注释引错了文件**：那段错的注释（「`LastCreated` 在 `DoCreate` 入口清」）在 **`EffectResolver.cs:6997-7000`**，账上记成 `Core/BattleContext.cs:848-854`；而 `BattleContext.cs:848-861` 讲的是 `LastHandTargets`、**内容正确**、⛔ 不该改。
3. ⚠️ **`A858` 的落点行号漂了**（账上 `:3111-3115` ⇒ 现 `:3142-3148`）· **`A947` 的 `cantattack` 读点漂了**（账上 `:2002` ⇒ 现 `:2035`）· **`A962` ③ 三处漂了**（`:3151/:3156/:3178` ⇒ `:3162/:3167/:3189`）· **`Editor/BattleScene.cs` 的 `DeckRuntime.cs:2081` 引用漂到 `:5944`**。**一律按符号认。**
4. 🆕 **`R-ENG` §5·2 的 `oath`** 是一条**没入账**的真差异候选（原版 `0x4FB = oath` 也写 `canAct/canAttack`，我们三词表没收）⇒ 建议**新开一笔**。
