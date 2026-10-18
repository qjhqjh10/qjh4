// RuleEngineTest_S23_Resolve.cs — 第二十三会话（2026-10-12）· 写手 `W2` · **结算层**三笔账
//
//   · `A1682`：`SAU72 Immortals Phalanx` 的 `When reanimated, deploy a copy of this troop`
//     —— 事件层早就收下了（`WhenTriggers` 一条），**结算层拒收这个载荷** ⇒ 棋盘一个单位都不多。
//     本笔在 `DoDeployOnce` 补上「复制指代的那一个」那一支。
//   · `A1685`：能力层那个 `ResolveOps`（`IReadOnlyList` 重载）**从不消费 `EffectOp.Instead`**
//     ⇒ `instead` 在能力层两条都跑（离线实测 **9 伤**，出牌那条路是 **6 伤**）。
//     本笔把配对规则收进 `InsteadSkips`，**两个重载共用**。
//   · `A1617`（≡ `A1395②`）：带 `landing` ⇒ **取消这一记攻击**（原版 `CancelAttack` 那一跳）。
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（已有兄弟
// `RuleEngineTest.cs` / `_S14` / `_Diag` / `_BoardModel` / `_S23_Assert` / `_S23_Parse`），
// 而**同一时刻只能有一个写手改一个文件** ⇒ 本批一律写在这里，`RuleEngineTest.cs` **一个字都没动**
// （`Run()` 里的 `Section` / `Step` 由调度台挂）。
//
// 🔴 **期望值的取法**（铁律 2 / 铁律 10）：先查**原版反编译方法体**，**再**用离线探针实测，
//    然后才写进断言 —— 所以下面每一句的期望值都是**量出来的**，不是从实现里读回来的。
//    探针 = `D:/tmp/wf_w2/`（`dotnet` 直编 `RuleEngine/Core/*.cs`，不进工程、不占 Unity 实例）·
//    另有 `D:/tmp/wf_w2_base/` 那一份「HEAD 的 `EffectResolver.cs`/`RuleCore.cs` + 当前其余文件」
//    = **A/B 对照**：本笔三笔的 firing 面在那里逐字复现改前读数（证明「改前是这样」不是回忆）。
//    逐条读数见 `资料/普查产出_第二十三会话/W2_结算层A1682A1685A1617.md`。
//
// 🔴 **两个记账口**（本仓铁律）：本文件里凡**改棋盘 / 直接调 `RuleCore.*`** 的，**都是夹具直调**，
//    不是走 `BattleDriver.LocalAct` / `SimpleAI.ExecuteAction` 那两个记账口 ⇒ **不会被录进本地录像**。
//    本批全程如此（这是断言不是对局），不逐条重复标注。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==================================================================
    //  A1682 · `deploy a copy of this troop`
    // ==================================================================

    /// <summary>
    /// 🔴 **本格的被测对象** = `Core/EffectResolver.cs` 的 `DoDeployOnce` 里
    /// 「`pool.CopyOfPrev` ⇒ 复制 `ctx.LastTarget` 那一份并部署」那一支。
    ///
    /// **判据**（原版那条路，逐条核过 —— 全文与字节码出处抄在 `DoDeployOnce` 那段注释里）：
    ///   · 卡面 = `When reanimated, deploy a copy of this troop`（`SAU72`；中文「被复生时，部署此部队的一个复制。」）；
    ///   · 原版 = `AbilityEffect.summonUnit(70)` + `SummonCriteria`
    ///     → `AbilityLogic__PlayAbility.c:733-1010` → `BattleManager__AddSummonUnit`
    ///     → `BattleManager._ResolveSummonUnit_d__510__MoveNext.c` → **`BattleCardManager__CreateCard(RawCardScript)`**
    ///     ⇒ **复制品 = 卡模板的一份新牌**（原始定义那一档：卡面数值 / 关键词 / 满血），
    ///       **不是**把场上那个单位连状态搬过去。
    ///   · 落点 = `MinionManager__GetNextSlotWithoutDisplacing()`（`…_d__510__MoveNext.c:135`）
    ///     —— 我们走 `DeployFree` → `BoardSlots.NextWithoutDisplacing`，**同一个函数**。
    ///
    /// **怎么才能红**：把 `DoDeployOnce` 里 `if (pool.CopyOfPrev)` 那一整段删掉
    /// ⇒ 本格实得「棋盘单位 2 个、日志打『部署不了 …（这条没生效）』」。
    /// 🧨 **灭自证**：本格同时钉住①**多出来的那一个是【新实例】**（`Instance.Id` 不同、不是同一个对象）
    /// ②**原来那个还在**（原件没被搬走）③**总数恰好 3**（不是 2、也不是 4 —— 复制品不许自己再触发一次）。
    /// 「把单位搬过去」或者「沿用同一个 `CardInstance`」这两条歪路都过不了 ①②。
    /// </summary>
    static void TestS23ResolveDeployCopyOfThisTroop()
    {
        // ---- ① 真打一次：广播 `reanimated`（`DoReanimate` 那条路发的是**同一条**广播）----
        {
            var phalanx = new CardDef("S23R_PHALANX", "S23R Phalanx", "unit",
                                      "When reanimated, deploy a copy of this troop",
                                      "common", "Test", 2, 3, 7, 0,
                                      new[] { "Remnant", "Regeneration 1" }, subtype: "Infantry");
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 2);
            int p = ctx.Active;
            // ⚠️ **槽号必须摆成「连续无洞」模型里那几格**（贴督军那格 = 左 3 / 右 5，
            //    `BoardSlots.SlotOf(side, index) = 4 + side*(index+1)`）——
            //    手摆到「有洞」的位置（例如右 6 而右 5 空着）会让 `NextWithoutDisplacing` 把
            //    **已经被占的那一格**当成空格返回 ⇒ 复制体会**静默盖掉**那个单位（本笔实测踩过）。
            var u = Place(ctx, p, 3, phalanx);
            Place(ctx, p, 5, Unit("S23R_Friend", 1, 1, 5));

            // (前提) 事件层确实收下了这一条（改前就是对的，⛔ 别把这一半改坏）
            CheckTrue(phalanx.WhenTriggers != null && phalanx.WhenTriggers.Count == 1,
                      "（前提）`When reanimated, deploy a copy of this troop` 被事件层收成 1 条监听器"
                    + "｜🧨 改坏 `WhenEvent.Parse` 的 `reanimated` 那一支 ⇒ 本行实得 0 条");
            var wt = phalanx.WhenTriggers[0];
            CheckTrue(wt.Ops != null && wt.Ops.Count == 1 && wt.Ops[0].Verb == "deploy"
                   && wt.Ops[0].Payload == "copy of this troop",
                      "（前提）那条监听器的正文 = 一条 `deploy`、载荷 = `copy of this troop`"
                    + "（**结算层就是按这个载荷认的**，⛔ 别改成按卡名认）");

            int before = S23RCountOf(ctx, p, phalanx);
            ctx.ClearSignals();
            int logFrom = ctx.Events.Count;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Reanimated, p, phalanx, u);
            string logs = S23RTail(ctx, logFrom);

            Check(before, 1, "（前提）广播前棋盘上这张卡 **1 份**");
            Check(S23RCountOf(ctx, p, phalanx), 2,
                  "★★ `A1682`：广播 `reanimated` 之后**多出一个复制体**（1 → 2 份）"
                + "｜🧨 删掉 `DoDeployOnce` 的 `if (pool.CopyOfPrev)` 那一整段 ⇒ 本行实得 1" + logs);

            var copy = S23RCopy(ctx, p, phalanx, u);
            CheckTrue(copy != null,
                      "★ 多出来的那一个**就是这张卡的复制品**（按卡名找得到，且不是原件那一份）" + logs);
            if (copy != null)
            {
                Check(copy.Attack, u.Attack, "★ 复制品是**原始定义那一档**：近战 = 3（卡面值）");
                Check(copy.Health, u.Health, "★ ……生命 = 7（**满血**，不是「照抄当前受伤状态」）");
                CheckTrue(copy.Card == phalanx, "★ ……用的是同一个卡模板（同一张卡）");
                CheckTrue(copy.Has(KeywordTable.Remnant) && copy.Has("regeneration"),
                          "★ ……卡面关键词也带过来了（`Remnant` + `Regeneration`）");
                // 🧨 **灭自证**：复制品必须是**另一份牌**
                CheckTrue(!ReferenceEquals(copy, u) && copy.Instance != null && u.Instance != null
                       && copy.Instance.Id != u.Instance.Id,
                          "★★ 灭自证：复制品是一个**独立的新实例**（实例号不同）"
                        + "｜🧨 改成沿用原件那个 `CardInstance`（`DeployFree` 的实例重载）⇒ 本行实得两号相同"
                        + "（那会让回手 / 洗回牌库 / 阵亡记账互相带跑）");
                CheckTrue(S23RCountOf(ctx, p, phalanx) == 2 && Board(ctx, p, 3) == u,
                          "★★ 灭自证：**原件还在原来那一格**（复制 ≠ 搬走）"
                        + "｜🧨 把实现写成「把那个单位挪一格」⇒ 本行实得另一格");
            }
            CheckTrue(Board(ctx, p, 5) != null && Board(ctx, p, 5).Name == "S23R_Friend",
                      "★★ 灭自证：复制的落点是**空格**，陪衬那个**没被盖掉**"
                    + "｜🧨 夹具把单位摆到「有洞」的位置（右 5 空着、右 6 有人）⇒ "
                    + "`NextWithoutDisplacing` 会把**被占的那格**当空格返回、复制体盖掉它 ⇒ 本行实得 false");
            CheckTrue(HasLog(ctx, "复制"), "★ 而且**出声**：日志说明白「复制了几份并部署」"
                      + "｜🧨 不写这句日志 ⇒ 本行实得 true→false（本仓红线：不许静默）");
        }

        // ---- ② 端到端：残骸 → 真打 `Reanimate a friendly Remnant` ----
        //  和 ① 走**同一条广播**，但这里走的是**真入口链**（翻面 → `DoReanimate` → `BroadcastWhen`），
        //  证明本笔在真链路上也成立（夹具直调广播 ≠ 真链路，两边都要有）。
        {
            var phalanx = new CardDef("S23R_PHALANX2", "S23R Phalanx", "unit",
                                      "When reanimated, deploy a copy of this troop",
                                      "common", "Test", 2, 3, 7, 0,
                                      new[] { "Remnant" }, subtype: "Infantry");
            var raise = Tactic("S23R_Raise", 0, "Reanimate a friendly Remnant");
            var ctx = ProbeBattle(new[] { raise }, new CardDef[0]);
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 3, Unit("S23R_Friend2", 1, 1, 5));        // 贴督军那格（连续无洞模型）
            var rem = Place(ctx, 0, 5, phalanx);                     // 右侧贴督军那格
            rem.IsRemnant = true;                        // 合成一具残骸（照 `RuleEngineTest.cs:3180` 的写法）
            var instBefore = rem.Instance;

            Check(S23RCountOf(ctx, 0, phalanx), 1, "（前提）打之前棋盘上这张卡 1 份（就是那具残骸）");
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "S23R_Raise"), -1), RuleCodes.OK,
                      "打出一张 `Reanimate a friendly Remnant`");
            Check(S23RCountOf(ctx, 0, phalanx), 2,
                  "★★ `A1682` 真链路：翻回来**那一下也带出了复制体**（1 → 2 份）" + LogTail(ctx, 8));
            var back = Board(ctx, 0, 5);
            CheckTrue(back != null && !back.IsRemnant && ReferenceEquals(back.Instance, instBefore),
                      "（前提）翻回来的那个**还是原来那份实例**（`DoReanimate` 走 `rem.Instance`）");
            var copy2 = S23RCopy(ctx, 0, phalanx, back);
            CheckTrue(copy2 != null && copy2.Instance != null && !ReferenceEquals(copy2.Instance, instBefore),
                      "★ 复制体是**另一份**（实例号与原件不同）");
        }

        // ---- ③ 出声那一半不许被改坏：**真不支持的形状**仍然要报 ----
        {
            var bogus = new CardDef("S23R_BOGUS", "S23R Bogus", "unit",
                                    "When reanimated, deploy a Zzz Nonexistent Thing",
                                    "common", "Test", 1, 1, 5, 0, new[] { "Remnant" },
                                    subtype: "Infantry");
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 2);
            int p = ctx.Active;
            var u = Place(ctx, p, 3, bogus);                          // 贴督军那格（连续无洞模型）
            int logFrom = ctx.Events.Count;
            RuleCore.BroadcastWhen(ctx, WhenEventKind.Reanimated, p, bogus, u);
            Check(S23RCountOf(ctx, p, bogus), 1, "（前提）负例：棋盘上这张卡还是 1 份");
            CheckTrue(HasLog(ctx, "部署不了"),
                      "★★ 负例：**认不出的载荷照样出声**（`部署不了`）"
                    + "｜🧨 把 `DoDeployOnce` 的 `!pool.Ok` 那一支改成静默 return ⇒ 本行实得 false"
                    + "（本仓红线：不许静默失败）" + S23RTail(ctx, logFrom));
            CheckTrue(HasLog(ctx, "这条没生效"),
                      "★ ……而且带上「这条没生效」那句（玩家看得见）");
        }
    }

    // ==================================================================
    //  A1685 · 能力层也要消费 `instead`
    // ==================================================================

    /// <summary>
    /// 🔴 **本格的被测对象** = `Core/EffectResolver.cs` 的能力层重载
    /// （`ResolveOps(…, IReadOnlyList&lt;EffectOp&gt; ops, string by, UnitState seed)`）
    /// 顶端那句 `var skip = InsteadSkips(ctx, owner, ops, null, pairConditional: false);`
    /// （以及它下面 `if (skip[i]) continue;`）。
    ///
    /// **判据**：`instead` 的语义就是「**跳掉前一条**」（`EffectOp.Instead` 的 doc；
    /// `EffectText.PeelInstead` 只负责把旗标贴上去）。**出牌 / 战术卡那条重载一直这么做**
    /// —— 本格把这个「两条路行为一致」钉住。
    ///
    /// **怎么才能红**：把能力层顶端那句 `InsteadSkips` 删掉（或把 `skip[i]` 那个 `continue` 删掉）
    /// ⇒ 本格实得**打掉 9 点**（`3 + 6` 两条都结算）。
    /// </summary>
    static void TestS23ResolveInsteadOnAbilityLayer()
    {
        // ---- ① 能力层：`Rally:` 体里的无条件 `instead` ----
        {
            var inst = new CardDef("S23R_INST", "S23R Instead Probe", "unit",
                                   "Rally: Deal 3 damage to an enemy. Deal 6 damage instead",
                                   "common", "Test", 1, 1, 5, 0, new[] { "Rally" });
            var ctx = ProbeBattle(new[] { inst }, new CardDef[0]);
            ToP1Turn(ctx, 2);
            ctx.Players[0].Energy = 20;
            // ⚠️ **不按某一个敌人的血量断言**：`Deal N damage to an enemy` 是**自动挑**目标
            //    （`-health` 那一档，实测在有 20 血小兵和 30 血督军时会挑**督军**）
            //    ⇒ 判据改成「**敌方那一侧掉的血总数**」＋「哪几条伤害日志出现过」。
            Place(ctx, 1, 3, Unit("S23R_Foe6", 1, 0, 20), exhausted: true);

            // (前提) 这一格确实走**能力层**那条路（`FireTriggerAt` → `IReadOnlyList` 重载）
            CheckTrue(inst.TriggerOps(KeywordTable.Rally) != null
                   && inst.TriggerOps(KeywordTable.Rally).Count == 2
                   && inst.TriggerOps(KeywordTable.Rally)[1].Instead,
                      "（前提）`Rally:` 体收成 2 条 op、第二条带 `Instead`（载荷逐字相同）");

            int logFrom = ctx.Events.Count;
            int hpBefore = S23RSumHealth(ctx, 1);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "S23R Instead Probe"), 2),
                      RuleCodes.OK, "从手牌打出这张单位");
            string logs = S23RTail(ctx, logFrom);
            CheckTrue(HasLog(ctx, "触发 RALLY"),
                      "（前提）走的是**触发层**那条路（`FireTriggerAt`），不是出牌那条" + logs);
            Check(hpBefore - S23RSumHealth(ctx, 1), 6,
                  "★★ `A1685`：能力层上 `instead` **替换掉**了前一条（敌方总共只该掉 6 点血）"
                + "｜🧨 删掉能力层顶端那句 `InsteadSkips` ⇒ 本行实得 **9**（`3 + 6` 都结算了）"
                + logs);
            CheckTrue(!HasLog(ctx, "对 1 个目标造成 3 点伤害"),
                      "★★ 判别式：**基数那 3 点那一条根本没结算**（日志里没有它）"
                    + "｜🧨 同一处改坏（去掉能力层的 `InsteadSkips`）⇒ 本行实得 true" + logs);
        }

        // ---- ② 对照：出牌 / 战术卡那条路（同一个形状）—— 两条路必须**同值** ----
        {
            var t = Tactic("S23R_TINST", 0, "Deal 3 damage to an enemy. Deal 6 damage instead");
            var ctx = ProbeBattle(new[] { t }, new CardDef[0]);
            ToP1Turn(ctx, 2);
            ctx.Players[0].Energy = 20;
            Place(ctx, 1, 3, Unit("S23R_TFoe6", 1, 0, 20), exhausted: true);
            int hpT0 = S23RSumHealth(ctx, 1);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "S23R_TINST"), 3), RuleCodes.OK,
                      "打出一张同形状的战术卡");
            Check(hpT0 - S23RSumHealth(ctx, 1), 6,
                  "★★ 对照：**出牌那条路**本来就是 6（本笔把规则收进 `InsteadSkips` 之后**一字不变**）"
                + LogTail(ctx, 5));
        }
    }

    /// <summary>
    /// 🔴 **2026-10-18（T02）起：能力层【也配】有条件的 `instead`** —— 判据不再是旧的
    /// 「不配」，而是「**先按被替换那条的目标判条件、再决定跳谁**」：
    /// 能力层没有 `chosen`，且 `ctx.LastTargets` 在入口被种成**施放者**（`prime`）⇒
    /// 直接判会「判施放者自己」（`ASH31` 那个陷阱）。`InsteadSkips` 现在先用 `ResolveTargets`
    /// 解出**被替换那条**的目标、把它临时当 referent 判条件，判完还原。
    ///
    /// 本格夹具照抄真卡 `ASH31 Crimson Hunter`（施放者**有** `Flying`、目标**没有**）：
    /// 正解 = 目标没飞行 ⇒ 只结算基数那一下 = **3**；
    /// 若拿**施放者**去判 ⇒ 恒成立 ⇒ 只结算 6 —— 那是【把对的改错】。
    /// 同函数的第二个分支（②）把「目标**有** Flying」那一支也钉住（应为 **6**，改回不配对则 9）。
    ///
    /// **怎么才能红**：把 `InsteadSkips` 里「能力层先解 base 目标」那段删掉 / 改用施放者判
    /// ⇒ ② 得 9 或 6，本格对应行红。
    /// </summary>
    static void TestS23ResolveInsteadConditionalNotPairedOnAbilityLayer()
    {
        var hunter = new CardDef("S23R_HUNTER", "S23R Crimson Hunter-ish", "unit",
                                 "Rally: Deal 3 damage to an enemy. If it has Flying, deal 6 damage instead",
                                 "common", "Test", 1, 1, 5, 0, new[] { "Rally", "Flying" });
        var ctx = ProbeBattle(new[] { hunter }, new CardDef[0]);
        ToP1Turn(ctx, 2);
        ctx.Players[0].Energy = 20;
        Place(ctx, 1, 3, Unit("S23R_Plain", 1, 0, 30), exhausted: true);

        int logFrom = ctx.Events.Count;
        int hpC0 = S23RSumHealth(ctx, 1);
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "S23R Crimson Hunter-ish"), 2),
                  RuleCodes.OK, "打出一张「施放者带 Flying、Rally 体里带条件式 instead」的单位");
        string logs = S23RTail(ctx, logFrom);

        CheckTrue(Board(ctx, 0, 3) != null && Board(ctx, 0, 3).Has("flying"),
                  "（前提）施放者**带 `Flying`**（这条条件要是拿它去判，就恒成立 —— 那正是陷阱）");
        CheckTrue(Board(ctx, 1, 3) != null && !Board(ctx, 1, 3).Has("flying"),
                  "（前提）这一侧摆的那个**没有** `Flying`");
        Check(hpC0 - S23RSumHealth(ctx, 1), 3,
                  "★★ **有条件**的 `instead` 在能力层**不在结算前配对**：条件在结算闸上判 —— "
                + "那时前一条 op 已经结算、`ctx.LastTargets` 正是**它打中的那个目标** ⇒ 判「它没有 Flying」⇒ "
                + "只结算基数 3"
                + "｜🧨 把 `InsteadSkips(…, pairConditional: false)` 改成 `true` ⇒ 本行实得 **6**"
                + "（条件拿**施放者**去判、恒成立 —— 那是【把对的改错】）" + logs);

        // ---- ② 🔴 **2026-10-18（T02）新增：条件【成立】那一支** ——
        //   原来能力层不配有条件的 instead ⇒ 基数 3 先结算、instead 那道条件闸在**后面**才判
        //   ⇒ 目标带 Flying 时两条都结算 = **9**。现在 `InsteadSkips` 先把被替换那条的目标解出来、
        //   用**它**判条件 ⇒ 只结算 instead 的 **6**。
        {
            var hunter2 = new CardDef("S23R_HUNTER2", "S23R Crimson Hunter-ish 2", "unit",
                                      "Rally: Deal 3 damage to an enemy. If it has Flying, deal 6 damage instead",
                                      "common", "Test", 1, 1, 5, 0, new[] { "Rally" });
            var ctx2 = ProbeBattle(new[] { hunter2 }, new CardDef[0]);
            ToP1Turn(ctx2, 2);
            ctx2.Players[0].Energy = 20;
            Place(ctx2, 1, 3, Unit("S23R_Flyer", 1, 0, 30, "Flying"), exhausted: true);

            int logFrom2 = ctx2.Events.Count;
            int hpF0 = S23RSumHealth(ctx2, 1);
            CheckCode(RuleCore.PlayCard(ctx2, 0, HandIdx(ctx2, 0, "S23R Crimson Hunter-ish 2"), 2),
                      RuleCodes.OK, "打出一张「目标带 Flying、Rally 体里带条件式 instead」的单位");
            string logs2 = S23RTail(ctx2, logFrom2);

            CheckTrue(Board(ctx2, 1, 3) != null && Board(ctx2, 1, 3).Has("flying"),
                      "（前提）目标**带 `Flying`**（条件应当成立）");
            Check(hpF0 - S23RSumHealth(ctx2, 1), 6,
                  "★★ `T02`：能力层有条件 `instead` 的**条件成立**时，只结算替代那一条（敌方共掉 **6**）"
                + "｜🧨 改回「不配对」⇒ 本行实得 **9**（`3 + 6` 两条都结算）" + logs2);
            CheckTrue(!HasLog(ctx2, "对 1 个目标造成 3 点伤害"),
                      "★★ 判别式：基数那 3 点那一条根本没结算（日志里没有它）" + logs2);
        }
    }

    // ==================================================================
    //  A1617 · 带 `landing` ⇒ 取消这一记攻击
    // ==================================================================

    /// <summary>
    /// 🔴 **本格的被测对象** = `Core/RuleCore.cs` 的 `DeclareAttack` 里那段
    /// 「`if (openedPod &amp;&amp; attacker.Has(KeywordTable.Landing)) return RuleCodes.OK;`」，
    /// 以及为它把 `OpenDropPod` 改成返回 `bool` 的那一处。
    ///
    /// **判据** = `BattleManager._ResolveAttack_d__438__MoveNext.c:339-379`（VA 逐条读过）：
    ///   `0x1809ad4c8 mov edx,0xe6`（`dropPod`）→ `je 0x1809ad847`（**没有就整段跳过**）；
    ///   有 ⇒ `RemoveDropPod` → `0x1809ad50e mov edx,0x302`（`landing`）→
    ///   带 `landing` ⇒ `CancelAttack` + 两次 `ClearPendingDamage` + `OnTrigger(0x1b8)`
    ///   ⇒ `FinishResolvingAction`（**这一下不打**）。
    ///   ⇒ **闸是「有没有 `dropPod`」**，`landing` 只是在那个分支里再分一次；
    ///     **位置**在协程 `yield` 之后 ⇒ 排在**扣配额 / 现身 / 失去伪装之后**（与 `abouttoattack` 相反）。
    ///
    /// **怎么才能红**：把那段 `if (openedPod &amp;&amp; …) return RuleCodes.OK;` 删掉
    /// ⇒ 本格第 ① 格实得守方 `6/9`（被打 3）。
    /// ⚠️ **可达性**：今天全池 **0 张**卡带 `landing` ⇒ 只能靠夹具立起来（铁律 11：
    /// 这是把「**还没做**」变成「已做」，不是「不做」）。
    /// </summary>
    static void TestS23ResolveLandingCancelsAttack()
    {
        // ---- ① 带 `DropPod` + `Landing` ⇒ 这一记取消 ----
        {
            var lz = new CardDef("S23R_LAND", "S23R Land Prober", "unit",
                                 "Landing: Gain +2 Attack", "common", "Test", 1, 3, 9, 0,
                                 new[] { "Rally", "Landing", "DropPod" });
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 2);
            int p = ctx.Active, q = 1 - p;
            var atk = Place(ctx, p, 3, lz, exhausted: false);
            var def = Place(ctx, q, 3, Unit("S23R_DEF", 1, 0, 9), exhausted: true);

            int logFrom = ctx.Events.Count;
            CheckCode(RuleCore.DeclareAttack(ctx, p, 3, q, 3), RuleCodes.OK, "出手（合法目标）");
            string logs = S23RTail(ctx, logFrom);

            Check(def.Health, 9,
                  "★★ `A1617`：带 `landing` ⇒ **一个伤害点都不出**（守方还是 9/9）"
                + "｜🧨 删掉 `DeclareAttack` 里那段 `if (openedPod && attacker.Has(Landing)) return OK;`"
                + " ⇒ 本行实得 **6**（照常打 3）" + logs);
            CheckTrue(!atk.Has(KeywordTable.DropPod),
                      "（前提）开舱那一跳照旧发生了（`dropPod` 已摘掉）");
            CheckTrue(HasLog(ctx, "触发 LANDING"),
                      "★ 而且 `Landing: <正文>` 那一跳**照旧发**（原版是 `CancelAttack` 之后再 `OnTrigger(0x1b8)`，"
                    + "⛔ 不是「取消了就连正文都不结算」）" + logs);
            Check(atk.Attack, 5,
                  "★ ……那半句真的落地了（`Landing: Gain +2 Attack` ⇒ 3 → 5）");
            CheckTrue(atk.Exhausted,
                      "★ **配额照扣**（「取消」排在消耗之后 —— 原版那一跳在协程 `yield` 之后；"
                    + "⛔ 别把它挪到 `IsValidTarget` 之前那一档，那是 `abouttoattack` 的位置）");
            CheckTrue(HasLog(ctx, "这一记攻击取消"), "★ ……并且**出声**（红线：不许静默）");
        }

        // ---- ② 反例 A：只带 `DropPod`、**不带** `landing` ⇒ 照常打 ----
        {
            var nz = new CardDef("S23R_NOLAND", "S23R NoLand Prober", "unit", "", "common", "Test",
                                 1, 3, 9, 0, new[] { "DropPod" });
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 2);
            int p = ctx.Active, q = 1 - p;
            Place(ctx, p, 3, nz, exhausted: false);
            var def = Place(ctx, q, 3, Unit("S23R_DEF2", 1, 0, 9), exhausted: true);
            CheckCode(RuleCore.DeclareAttack(ctx, p, 3, q, 3), RuleCodes.OK, "出手");
            Check(def.Health, 6, "★ 反例：不带 `landing` ⇒ **照常打 3**（开舱只是等一帧）" + LogTail(ctx, 4));
        }

        // ---- ③ 反例 B：只带 `landing`、**不带** `DropPod` ⇒ 照常打 ----
        //  这一格钉的是「闸 = `openedPod`」—— 原版那两跳在**同一个 `has dropPod` 分支里**。
        {
            var lo = new CardDef("S23R_LANDONLY", "S23R LandOnly Prober", "unit",
                                 "Landing: Gain +2 Attack", "common", "Test", 1, 3, 9, 0,
                                 new[] { "Landing" });
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 2);
            int p = ctx.Active, q = 1 - p;
            Place(ctx, p, 3, lo, exhausted: false);
            var def = Place(ctx, q, 3, Unit("S23R_DEF3", 1, 0, 9), exhausted: true);
            CheckCode(RuleCore.DeclareAttack(ctx, p, 3, q, 3), RuleCodes.OK, "出手");
            Check(def.Health, 6,
                  "★ 反例：带 `landing` 但**不带 `dropPod`** ⇒ **照常打**"
                + "（原版那两跳在 `has dropPod` 分支里；`landing` 只是分支内的第二次分岔）"
                + "｜🧨 把闸从 `openedPod && u.Has(Landing)` 改成只看 `u.Has(Landing)`"
                + "（或把 `OpenDropPod` 的返回值固定成 `true`）⇒ 本行实得 **9**"
                + "（一个「只带 landing」的单位会永远打不出伤害 —— 原版没有这条）" + LogTail(ctx, 4));
        }
    }

    // ==================================================================
    //  🆕 波 1（2026-10-19）：`RuleEngine/Core` 小改那一批的回归点
    //  `A1426`（DoGive 的在场上判据）· `T01 §六·6`（护甲两种表示）·
    //  `A1692`（DeployFree 不覆盖）· `A1690`（When 正文的相邻锚点）
    //
    //  ⚠️ 期望值的判据（逐条写在断言文案里）：
    //   · `A1426` → `资料/普查产出_第二十五会话/R1_引擎判据收集.md` §`A1426`；
    //   · 护甲 → `A2015` 那一条（`EffectResolver.ApplyOneGain` 的 `case "armour"`）；
    //   · `A1692` → `T02` §二（**原版没有这一档**，这一闸是补我们表示法的洞）；
    //   · `A1690` → `T02` §二（`AbilityLogic__GetTargets.c:1001/:1046`：100 = Self · 110 = 事件宾语）。
    // ==================================================================

    /// <summary>
    /// **`A1426`**：`Sacrifice:` / `Backlash:` 触发那一刻单位 `Health <= 0` 但**还在场上**
    /// （`cardState` 仍是 `2 inPlay`）⇒ 原版会加、我们改前静默丢掉。
    /// 🔴 判别式：**不是「删闸」** —— 已经离场的（`{5,6,11}`）仍然不许给；
    ///    `Give +N Health` 会因为这条复活尸体的话就是歪路。
    /// </summary>
    static void TestS23ResolveA1426GiveToDyingOnBoard()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 2);
        int p = ctx.Active;
        var u = Place(ctx, p, 3, Unit("S23R_DYING", 1, 1, 5));
        u.Health = -1;                      // 死了、但还没来得及离场（Sacrifice 那一刻的形状）

        var ops = EffectText.Parse("Gain +1 Attack", out _, out _);
        List<string> un;
        int n = RuleCore.ResolveOps(ctx, p, u, ops, null, out un);
        Check(n, 1, "（前提）无主语 `Gain +1 Attack` 结算了一条");
        Check(u.Attack, 2,
              "★★ `A1426`：**血 ≤ 0、但还在场上**的施放者拿到 +1（改前 `DoGive` 的 `!IsAlive` 把它丢掉）"
            + "｜🧨 把 `DoGive` 的判据改回 `!t.IsAlive` ⇒ 本行实得 1" + LogTail(ctx, 4));

        // 🧨 反例（判别式的另一半）：**已经不在棋盘上**的（`cardState = 5/6/11`）不许给 ——
        //    放开成「只要对象存在」会让 `Give +N Health` 复活墓地里的牌。
        var gone = new UnitState(ctx.NewInstance(Unit("S23R_GONE", 1, 1, 5)), false);
        gone.Health = -1;
        var ops2 = EffectText.Parse("Gain +1 Attack", out _, out _);
        RuleCore.ResolveOps(ctx, p, gone, ops2, null, out un);
        Check(gone.Attack, 1,
              "★★ 灭自证：**不在任何棋盘上**的施放者拿不到（原版 `cardState ∈ {5,6,11} ⇒ return 0`）"
            + "｜🧨 把判据换成 `Health > 0 || 对象非空` ⇒ 本行实得 2（= 复活一条歪路）");
    }

    /// <summary>
    /// **`T01 §六·6`**：护甲只有**一份判据**（`_keywords["armour"]`），`Armor` 字段必须跟它逐值相等。
    /// 三处写点：`AddKeyword`（本来就同步）· `RemoveKeyword`（改前**不同步**）·
    /// `ApplyGrant`（黑暗契约 / 光环的授甲路，改前**只写字段**）。
    /// </summary>
    static void TestS23ResolveArmourTwoRepresentations()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 2);
        var u = Place(ctx, ctx.Active, 3, Unit("S23R_ARMOUR", 1, 1, 5));

        u.AddKeyword(KeywordTable.Armour, 2);
        Check(u.Armor, 2, "（前提）`AddKeyword(armour,2)` ⇒ `Armor` = 2");
        u.RemoveKeyword(KeywordTable.Armour, 1);
        Check(u.KwValue(KeywordTable.Armour), 1,
              "★ `A1426` 同族：`RemoveKeyword(armour,1)` 后关键词还剩 1");
        Check(u.Armor, 1,
              "★★ `T01 §六·6`②：**减一层也要同步 `Armor`**（改前它停在 2 —— 减伤照旧按 2 算）"
            + "｜🧨 把 `RemoveKeyword` 的同步那一句删掉 ⇒ 本行实得 2");

        u.RecordGrant("armour", 3, "S23R_PACT");
        Check(u.KwValue(KeywordTable.Armour), 4, "（前提）黑暗契约授甲：关键词 1 → 4");
        Check(u.Armor, 4,
              "★★ `T01 §六·6`①：`ApplyGrant` 的 `case \"armour\"` 也要同步关键词（改前关键词仍是 1）"
            + "｜🧨 把那个 case 改回裸写 `Armor` ⇒ 本行实得 4 但 `KwValue` 仍是 1（下一行会红）");
        u.RevertGrantsFrom("S23R_PACT");
        Check(u.Armor, 1, "★ 整份收回：`Armor` 回到 1");
        Check(u.KwValue(KeywordTable.Armour), 1, "★ 整份收回：关键词也回到 1（两边逐值相等）");
    }

    /// <summary>
    /// **`A1692`**：`DeployFree` 的目标格被占时**不许静默覆盖**。
    /// 🔴 原版**没有这一档**（两侧是连续 `List`，`GetNextSlotWithoutDisplacing` 不做占用检查）
    /// ⇒ 这一闸是**补我们定长数组表示法的洞**，⛔ 别写成「照原版」。
    /// </summary>
    static void TestS23ResolveA1692DeployFreeNoOverwrite()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 2);
        int p = ctx.Active;
        // 造「有洞」局面：右侧只有 **6 号格**（右 index 1）有人、5 号格空着；
        // 左侧 3 号格有人（让两侧人数打平 ⇒ `NextWithoutDisplacing` 选右）。
        var hole = Place(ctx, p, 6, Unit("S23R_HOLE", 1, 1, 5));
        Place(ctx, p, 3, Unit("S23R_LEFT", 1, 1, 5));

        int slot;
        int logFrom = ctx.Events.Count;
        bool ok = RuleCore.DeployFree(ctx, p, Unit("S23R_SUMMON", 1, 1, 5), out slot);
        string logs = S23RTail(ctx, logFrom);
        CheckTrue(ok, "（前提）免费部署成功（该侧还有空格）" + logs);
        Check(slot, 8,
              "★★ `A1692`：目标格 6 被占 ⇒ **换到该侧最外的空格 8**"
            + "｜🧨 删掉 `DeployFree` 里那道 `ps.Board[dst] != null` 闸 ⇒ 本行实得 6" + logs);
        CheckTrue(Board(ctx, p, 6) == hole && hole.Name == "S23R_HOLE",
                  "★★ 灭自证：原来那一格的人**还在**（没有被覆盖、没有换对象）" + logs);
        CheckTrue(HasLog(ctx, "不覆盖"),
                  "★ 而且**出声**（本仓红线：不许静默失败）" + logs);
    }

    /// <summary>
    /// **`A1690`**：`When …` 正文里裸写的 `adjacent` 锚点。
    /// 判据 = `AbilityLogic__GetTargets.c:1001`（100 → 相对 `thisCard`）/ `:1046`（110 → 事件宾语）。
    /// 🔴 **两层口径要分清**：事件层（真正执行的那批 op）现在定得下来；
    ///    而 `IsFullyParsed(整条 desc)` 的**覆盖率层**仍要把没被事件层认领的 `When … adjacent …`
    ///    判成半懂（`F1` 的回归点）—— 它故意不是同一个答案。
    /// </summary>
    static void TestS23ResolveA1690WhenBodyAdjacentAnchor()
    {
        // ① 事件**不带宾语** ⇒ 原版 100 = Self
        var noObj = new CardDef("S23R_WHEN", "S23R When", "unit",
                                "When a friendly troop dies, give +1 Attack to an adjacent friendly troop",
                                "common", "Test", 1, 1, 9, 0, null, subtype: "Infantry");
        S23RAnchorCheck(noObj, AdjacentAnchor.Self,
            "★★ `A1690`：事件不带宾语 ⇒ 裸 `adjacent` 锚 = **Self**（原版 `adjacentToSelfIfMeetsCriteria = 100`）"
          + "｜🧨 删掉 `FillAdjacentAnchors` 的 `whenBody` 分支 ⇒ 锚点退回 `Unset`（本行红）");
        CheckTrue(!EffectText.IsFullyParsed(noObj.Desc),
                  "★★ 覆盖率层**仍判半懂**（`F1` 的回归点：没被事件层接管的那句不冒充干净）"
                + "｜🧨 顺手把 ⑤ 那一支也删掉 ⇒ 本行红");

        // ② 事件**带宾语** ⇒ 原版 110 = 事件宾语（`Long Fang` 的同族形状）
        var withObj = new CardDef("S23R_WHEN2", "S23R When2", "unit",
                                  "When this unit attacks an enemy with Hunt Mark, deal 3 damage to adjacent enemies",
                                  "common", "Test", 1, 1, 9, 0, null, subtype: "Infantry");
        S23RAnchorCheck(withObj, AdjacentAnchor.TargetIfMeetsCriteria,
            "★★ `A1690`：事件带宾语 ⇒ 裸 `adjacent` 锚 = **TargetIfMeetsCriteria**（110）");
    }

    /// <summary>取这张卡**事件层那批 op** 里第一个相邻锚点，断言它的 `<see cref="AdjacentAnchor"/>`。</summary>
    static void S23RAnchorCheck(CardDef card, AdjacentAnchor want, string msg)
    {
        CheckTrue(card.WhenTriggers != null && card.WhenTriggers.Count > 0,
                  "（前提）事件层收下了这张卡的监听器：" + card.Desc);
        EffectTargetSpec spec = null;
        if (card.WhenTriggers != null)
            foreach (var t in card.WhenTriggers)
                if (t.Ops != null)
                    foreach (var o in t.Ops)
                        if (o.Target != null && o.Target.Adjacent) spec = o.Target;
        CheckTrue(spec != null, "（前提）正文里解出了一个裸 `adjacent` 目标：" + card.Desc);
        Check(spec == null ? AdjacentAnchor.Unset : spec.Anchor, want, msg);
    }

    /// <summary>
    /// **`A1312`（残半）**：`ApplyDeployTurnState` 的两个位要**跟着 `+0x58` 走**。
    /// 判据 = `CardScript__ActivateMinion.c:14-42`（`canAct = canAttack = !displaySummonSickness`）＋
    /// `EntityScript__get_displaySummonSickness.c`（`+0x58 && !fast && !flank`）＋
    /// `ActivateTraitsOnSummonOrEnchantment.c:86-99`（`ferocity`/`oath` 只抬 `canAct`）。
    /// ⚠️ 今天**没有调用点传 `keepStatus = true`** ⇒ 本格是「形状补齐」的护栏，不是现有行为变化。
    /// </summary>
    static void TestS23ResolveA1312KeepStatusTurnState()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 2);

        // ① `keepStatus = false`（今天所有调用点）⇒ 召唤病真 ⇒ 两位都是「不能」
        var sick = new UnitState(ctx.NewInstance(Unit("S23R_SICK", 1, 1, 5)), false);
        RuleCore.ApplyDeployTurnState(sick, false);
        CheckTrue(sick.SummonSickness && sick.Exhausted && sick.CannotAttackThisTurn,
                  "★ `keepStatus=false`：召唤病真 ⇒ `Exhausted` / `CannotAttackThisTurn` 都是真（老行为）");

        // ② `keepStatus = true` 且 `+0x58` 保持假 ⇒ 原版 `canAct = canAttack = true`
        var keep = new UnitState(ctx.NewInstance(Unit("S23R_KEEP", 1, 1, 5)), false);
        RuleCore.ApplyDeployTurnState(keep, true);
        CheckTrue(!keep.SummonSickness && !keep.Exhausted && !keep.CannotAttackThisTurn,
                  "★★ `A1312`：`keepStatus=true` 且 `+0x58` 为假 ⇒ **两位都是假**（改前仍按关键词谓词写成真）"
                + "｜🧨 把那两句改回 `!HasDeployExemption` / `!HasAttackDeployExemption` ⇒ 本行实得真");

        // ③ `ferocity` 只抬 `canAct`（`Exhausted` 假）、`canAttack` 不动（真）
        var fer = new UnitState(ctx.NewInstance(
            new CardDef("S23R_FER", "S23R Fer", "unit", "", "common", "Test", 1, 1, 5, 0,
                        new[] { "Ferocity" }, subtype: "Infantry")), false);
        RuleCore.ApplyDeployTurnState(fer, false);
        CheckTrue(!fer.Exhausted && fer.CannotAttackThisTurn,
                  "★★ `ferocity`：**能动、不能攻击**（原版 `ActivateTraitsOnSummonOrEnchantment.c:86-99` 只抬 `canAct`）"
                + "｜🧨 把 `Exhausted` 那半也写成 `displaySummonSickness` ⇒ 本行实得「不能动」");
    }

    // ==================================================================
    //  本批专用的小工具（名字一律带 `S23R` 前缀，⛔ 不与既有私有辅助重名）
    // ==================================================================

    /// <summary>棋盘上**这张卡**有几份（**按卡模板数**，不数别人 —— 夹具里督军也在棋盘上，
    /// 用「棋盘总数」会被它带偏）。</summary>
    static int S23RCountOf(BattleContext ctx, int p, CardDef card)
    {
        int n = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && u.Card == card) n++;
        }
        return n;
    }

    /// <summary>找**复制品**：卡模板 = <paramref name="card"/>、**不同格**、**不同实例**。
    /// 找不到返回 null（找不到本身就是一条失败，见调用点的 `CheckTrue`）。</summary>
    static UnitState S23RCopy(BattleContext ctx, int p, CardDef card, UnitState original)
    {
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u == null || u.Card != card) continue;
            if (ReferenceEquals(u, original)) continue;
            if (original != null && u.Instance != null && original.Instance != null
                && u.Instance.Id == original.Instance.Id) continue;
            return u;
        }
        return null;
    }

    /// <summary>某一侧**棋盘上所有单位（含督军）当前生命之和**。
    /// ⚠️ **别按「某一个敌人的血量」断言**：`Deal N damage to an enemy` 是**自动挑**目标的
    /// （`-health` 那一档 —— 实测同一种夹具在不同回合数下会挑到不同的人），
    /// 断言会变成「挑人规则」的断言，而不是被测那一条。
    /// ⚠️ **含督军**：督军就站在 `Board[4]`（`RuleCore.cs:528`）。</summary>
    static int S23RSumHealth(BattleContext ctx, int p)
    {
        int n = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && u.Health > 0) n += u.Health;
        }
        return n;
    }

    /// <summary>`ctx.Events` 从 <paramref name="from"/> 起的那些行（拼成一段，好塞进断言的失败文案）。</summary>
    static string S23RTail(BattleContext ctx, int from)
    {
        var sb = new System.Text.StringBuilder("｜本次新增日志：");
        for (int i = System.Math.Max(0, from); i < ctx.Events.Count; i++)
            sb.Append("「").Append(ctx.Events[i]).Append("」");
        return sb.ToString();
    }
}
