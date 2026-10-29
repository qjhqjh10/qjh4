// RuleEngineTest_B99E6.cs — 第九十九会话 · 波 1 · 执行代理 `B99_E6`
//
// 为什么另开文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆）。
// 本件只放本批新增判据的断言；宿主 `RuleEngineTest.cs` 里只需要加一处接线
//   （建议 `Step(RunB99E6);` —— 见报告里的「宿主接线指令」，方法名**不用 `Test`/`Report` 打头**，
//    否则会被 `VerifyRunCaseCoverage` 的反射清单判成「少跑一个用例」）。
//
// ============================ 本件覆盖哪一条账 ============================
//  `账 522 BattleManager.ResolveMinionDeath`
//  （账号口径：`资料/完全复刻/账/战斗逻辑驱动.tsv` 的**数据行序号** 522 = **文件第 523 行**
//   —— `判据/B97_R8_波4判据补全.md` 里写的「战斗逻辑驱动:523」是**文件行号**，同一行）。
//  判据（全文 → `判据/B97_R8_波4判据补全.md` §1，⛔ 本文件不抄第二份）：
//    · `d:/2/tools/decomp_full/BattleManager._ResolveMinionDeath_d__454__MoveNext.c:40/56/58/61/112`
//    · `d:/2/tools/decomp_full/MinionManager__RemoveMinion.c`（`List.Remove` + `RefreshOccupationSlots`）
//    · `d:/2/tools/decomp_full/MinionManager__ReassembleMinions.c`（**纯表现**，不写规则状态）
//  施工单 = `判据/B98_J0_战斗剩余终局.md` §三 的「522」那一条（把 `B97_R8` 的三条夹具落成可跑自检）。
//
//  原版形状（逐跳）：**每个死者一条动作** ——
//    `:40` `cardState != 5` ⇒ 只打警告 + 收尾（不做事）
//    `:56` `MinionManager.RemoveMinion`（= `List.Remove`，末尾 `RefreshOccupationSlots` ⇒ **立即补位**）
//    `:58` 从 `unitsInPlay` 移除 · `:61` 入队一条「显式重组」动作（**纯表现**）
//    `:112` `ChallengeLogMgr.TroopKilled`（**无条件**，不看凶手死活 / 回合归属 / 是否残骸）
//    `:119` 死亡协程 · `:145` 两手 `UpdateCardEffects` · `:149` `FinishResolvingAction`
//  我们侧的等价物（**规则侧不改**，判据已核）：
//    `RuleCore.cs:10226 MinionDeathBookkeepingDeferred` · `:10325 FlushDeaths` · `:9547 CleanupDeaths`
//    · `BoardSlots.RemoveAt`（连续无洞的「外侧补位」）
//
// ============================ 三条判别式（= 原判据里的 G4 / G5 / G7）============================
//  G4 **同批死两个**：同一批（`using (RuleCore.SimultaneousDamage(ctx))`）里两个单位一起归零，
//     ① 死讯按**入队序**（= 伤害落下的先后），⛔ 不是按 (方, 格号) 排序；
//     ② 两个都真的离场，且摘第二个时它已经**补位**到前一个的格号上（按身份现查，不是按记下的格号）；
//     ③ 两个都记一次「部队阵亡」，记给**凶手座位**（原版 `:112` 无条件）。
//  G5 **死亡里再死一个**：外层死者的 Backlash 打死同方的另一个人 —— 原版队列 **FIFO**：
//     新死者追加**队尾** ⇒ 外层死讯 < 嵌套死讯（⛔ 不是「A→C→B」）。
//  G7 **残骸那一档**：① 1 血带 `Remnant` 的单位被打死 ⇒ **翻面成残骸留在格位**（不记账、不计阵亡）；
//     ② 再打残骸 ⇒ 摧毁：格位被摘 + **外侧内移**、`TroopsKilled` +1、`DeadUnits` +1 且
//        **`IsRemnant == true`**（原版 `GetCardsInCemetery.c:60` 无条件排除它）、
//        ⛔ **不跑** Backlash / 死亡监听器（残骸一条 trait 都没有）。
//
// ============================ 🔴 纪律（本文件自己的）============================
//  · **先断前提、再断结论** —— ⛔ 不用 `if (x != null) Check(...)` 那种「前提不成立就静默跳过」的写法。
//  · 棋盘一律**连续无洞**（本仓模型：放牌 = 插入推人 · 离场 = 外侧补位）；⛔ 不摆有洞的棋盘。
//  · 期望值写死字面量，⛔ 不从被测实现反算。
//  · 每条都带 `🧨`（**什么改动能让它红**）。
//
// ============================ ⚠️ 断言状态（如实记）============================
//  本代理**不许跑 Unity 批处理**（铁律 13·3）⇒ 这三段**还没被执行过**，
//  由调度台跑一次 `RuleEngineTest.Run` 才算数（报告里会写明）。
//  能在不跑 Unity 的前提下核对的都核过了：`typecheck` 0/0 · 依赖的符号逐个现读（见报告）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一处接线调；⛔ 不用 `Test`/`Report` 打头）。</summary>
    public static void RunB99E6()
    {
        Section("🆕 B99_E6：账 522 `ResolveMinionDeath` —— 同批死两个 / 死亡里再死 / 残骸档");
        B99E6G4TwoDeathsInOneBatch();
        B99E6G5NestedDeath();
        B99E6G7RemnantTier();
    }

    // ==================================================================
    //  小工具（全部带 `B99E6` 前缀 —— 本类是 partial，⛔ 别和别的分册撞名）
    // ==================================================================

    /// <summary>`ctx.Signals` 里「**这一份**的死讯」的下标；没有 ⇒ `-1`。
    /// ⚠️ 死讯（`EvtKind.Death`）从 2026-10-22 起因 `W62·R2` 改成**入队**发的
    /// （`CleanupDeaths` 里 `EnqueueAbility(… "death-signal:" …)`）⇒ 直接调伤害入口的夹具
    /// **必须自己 `RuleCore.PumpActionQueues(ctx)` 一次**，否则读到的永远是 0 条（那是「没泵队」，不是缺陷）。</summary>
    static int B99E6DeathSignalIdx(BattleContext ctx, UnitState u)
    {
        if (ctx == null || u == null) return -1;
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e == null || e.Kind != EvtKind.Death) continue;
            if (e.InstanceId == u.InstanceId) return i;
        }
        return -1;
    }

    /// <summary>`ctx.DeadUnits`（阵亡登记）里「**这一份**」的下标；没有 ⇒ `-1`。
    /// 用**实例号**匹配，⛔ 不按卡名（同名两张卡是两份不同的记录）。</summary>
    static int B99E6DeadUnitIdx(BattleContext ctx, UnitState u)
    {
        if (ctx == null || u == null) return -1;
        for (int i = 0; i < ctx.DeadUnits.Count; i++)
        {
            var d = ctx.DeadUnits[i];
            if (d == null) continue;
            if (d.InstanceId == u.InstanceId) return i;
        }
        return -1;
    }

    // ==================================================================
    //  G4 · 同批死两个（判据 → `B97_R8` §1 的「G4」段）
    //
    //  形状：P1 右侧 5 / 6 号格各一个 1 血单位，**同一批**里一起被打到 0。
    //  为什么挑**右侧**：出队顺序 = 入队顺序，而右侧「贴督军那格 = 5」在**前**
    //  ⇒ 先摘 5 号格那个，6 号格那个会补到 5 号格上 —— 正好踩中「按格号定位会指空」那条路。
    // ==================================================================
    static void B99E6G4TwoDeathsInOneBatch()
    {
        var ctx = Battle(new[] { Unit("B99E6G4Hero", 0, 9, 9) }, new[] { Unit("B99E6G4Foe", 0, 0, 1) });
        ToP1Turn(ctx, 2);
        var x = Place(ctx, 1, 5, Unit("B99E6G4x", 0, 0, 1), exhausted: true);
        var y = Place(ctx, 1, 6, Unit("B99E6G4y", 0, 0, 1), exhausted: true);
        RuleCore.PumpActionQueues(ctx);   // 清掉开局/推进回合可能留下的队列动作（免得下面读信号时混进别人的）
        ctx.ClearSignals();

        // ---- 前提（⛔ 不许省：前提不成立时下面每一条都会「看着对」）----
        string why;
        Check(BoardSlots.CountOnSide(ctx.Players[1], 1), 2,
              "G4 前提：P1 **右侧**摆着两个单位（5 / 6 号格）");
        CheckTrue(BoardSlots.IsContiguous(ctx.Players[1], out why),
                  "G4 前提：摆放后右侧**连续无洞**（" + (why ?? "无洞") + "）");
        Check(ctx.Tally.TroopsKilled[0], 0, "G4 前提：动手前 P0 的「部队阵亡」计数是 0");
        CheckTrue(x.Health > 0 && y.Health > 0, "G4 前提：两个都还活着（1 血）");

        // ---- 同一批里两个一起归零 ----
        //  🔴 `killer: 0` 是**故意**传的：原版 `:112 TroopKilled` 记给**凶手座位**
        //     （消费者 `TroopsKilledCount` 只读 `killer.isPlayer`）；不传的话 `SeatOf(null) == -1`
        //     ⇒ `AddTroopKilled(-1)` 是空转 ⇒ 下面那条「== 2」会**永远测不到东西**。
        using (RuleCore.SimultaneousDamage(ctx))
        {
            RuleCore.HurtWithSourceForTest(ctx, x, 5, "夹具", killer: 0);
            RuleCore.HurtWithSourceForTest(ctx, y, 5, "夹具", killer: 0);
        }
        RuleCore.PumpActionQueues(ctx);   // 死讯/反噬/记账都是**队列动作** ⇒ 不泵队读不到

        CheckTrue(x.Health <= 0 && y.Health <= 0, "G4 前提：两个都被打到 0 血（同一批）" + LogTail(ctx));

        // ---- 判别式 ②：两个都真的离场，且右侧仍然连续 ----
        Check(Board(ctx, 1, 5), null, "G4② 5 号格空了");
        Check(Board(ctx, 1, 6), null,
              "G4② 6 号格也空了 —— **同一批里死两个，两个都要处理**"
            + "（按格号定位的旧写法会漏掉第二个：它补位之后那个格号已经指空）");
        Check(BoardSlots.CountOnSide(ctx.Players[1], 1), 0, "G4② 右侧一个不剩（`CountOnSide == 0`）");
        CheckTrue(BoardSlots.IsContiguous(ctx.Players[1], out why),
                  "G4② 摘掉两个之后右侧仍然**连续无洞**（" + (why ?? "无洞") + "）"
                + "；🧨 把 `CleanupDeaths` 的摘除改成「按格号 `Board[slot] = null`」（不走 `BoardSlots.RemoveAt`）"
                + " ⇒ 补位不成立，本行红");

        // ---- 判别式 ①：死讯按**入队序**（= 伤害落下的先后）----
        int dX = B99E6DeathSignalIdx(ctx, x);
        int dY = B99E6DeathSignalIdx(ctx, y);
        CheckTrue(dX >= 0 && dY >= 0,
                  "G4① 前提：两条 `EvtKind.Death` 都在（x=" + dX + " / y=" + dY + "）" + LogTail(ctx));
        CheckTrue(dX < dY,
                  "G4① ★★ 死讯顺序 = **入队序**：x(" + dX + ") < y(" + dY + ")"
                + "；🧨 在 `FlushDeaths` 里给 `pending` 加一句 `Sort(按 (方,格号))` ⇒ 当入队序 ≠ 格号序时本行红"
                + "（判据 = `W58-SLAYFIX`·D4，`FlushDeaths` 的 doc 逐句）" + LogTail(ctx));

        // ---- 判别式 ③：两个都记一次「部队阵亡」，记给凶手座位 ----
        Check(ctx.Tally.TroopsKilled[0], 2,
              "G4③ P0 的「部队阵亡」计数 == 2 —— 原版 `_ResolveMinionDeath:112` **每个死者无条件一次**"
            + "；🧨 把计数挪回 `TryCreditKillCore`（那一族带 6 道闸）⇒ 本行少计");
        Check(ctx.Tally.TroopsKilled[1], 0, "G4③ 反例：P1 那侧是 0（我们只记凶手座位）");

        // ---- 补充：阵亡登记的**先后**也得是入队序（结构上独立于 `ctx.Signals` 的另一条观测口）----
        Check(ctx.DeadUnits.Count, 2, "G4 补充：阵亡登记恰好两条（两个死者各一条）");
        int gX = B99E6DeadUnitIdx(ctx, x), gY = B99E6DeadUnitIdx(ctx, y);
        CheckTrue(gX >= 0 && gY >= 0 && gX < gY,
                  "G4 补充：阵亡登记的下标也是 x(" + gX + ") < y(" + gY + ") —— 与死讯同序"
                + "（`MinionDeathBookkeepingDeferred` 按死亡动作的 FIFO 逐条入账）");
    }

    // ==================================================================
    //  G5 · 死亡里再死一个（判据 → `B97_R8` §1 的「G5」段）
    //
    //  形状（照既定夹具 `TestW58NestedDeathInterleave` ① 的骨架，⚠️ 那条在 `RuleEngineTest.cs:26931`，
    //  本文件**没有**重复它的全部断言，只补这一条「顺序」判别式）：
    //    P0：攻击者（1 攻 / 10 血）+ C（2 血）
    //    P1：A（1 血 / `Backlash: Deal 4 damage to all enemies`）
    //    A 的 Backlash 4 点：攻击者 10 → 6（不死）、**同方的 C 2 → −2（死，嵌套）**。
    //  原版次序 = 队列 **FIFO**（`AddAutoActionToQueue`）：外层 A 的动作整条跑完（含死讯）才轮到 C。
    //  ⇒ 我们的形状 = `FlushDeaths` 把嵌套死者**追加到本地队列尾部**，⛔ 不是递归 flush、⛔ 不是头插。
    // ==================================================================
    static void B99E6G5NestedDeath()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        var atk = Place(ctx, 0, 3, Unit("B99E6G5Atk", 0, 1, 10), exhausted: false);
        var c = Place(ctx, 0, 5, Unit("B99E6G5C", 0, 0, 2), exhausted: true);
        var a = Place(ctx, 1, 3, Unit("B99E6G5A", 0, 0, 1,
                                      "Backlash: Deal 4 damage to all enemies"), exhausted: true);
        RuleCore.PumpActionQueues(ctx);
        ctx.ClearSignals();

        // ---- 前提 ----
        //  🧨 这条**必须**先断：Backlash 正文没解析出来时 A 的死**不会**溅死 C，
        //     而下面「A 死讯 < C 死讯」在「C 根本没死」时会退化成两条 -1 比较（红，但错因不清）。
        CheckTrue(a.Has(KeywordTable.Backlash),
                  "G5 前提：A 身上确实挂着 `backlash`（`Unit(..., \"Backlash: Deal 4 damage to all enemies\")` 解析成功）");
        Check(a.Health, 1, "G5 前提：A 只有 1 血（攻击者 1 攻一击必死）");
        Check(c.Health, 2, "G5 前提：C 有 2 血（Backlash 的 4 点必然打死它 = 嵌套死亡）");
        CheckTrue(atk.Health > 4, "G5 前提：攻击者的血 > Backlash 的 4 点（它自己**不许**死，否则次序观测被搅乱）");

        // ---- 打 ----
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3), RuleCodes.OK,
                  "G5 攻击者砍 A（A 的 Backlash 会溅死同方的 C）");
        CheckTrue(!a.IsAlive && !c.IsAlive,
                  "G5 前提：A（外层死者）与 C（嵌套死者）都死了" + LogTail(ctx));
        CheckTrue(atk.IsAlive, "G5 前提：攻击者活着（10 − 4 = 6）");

        // ---- 判别式：外层死讯 < 嵌套死讯 ----
        int dA = B99E6DeathSignalIdx(ctx, a);
        int dC = B99E6DeathSignalIdx(ctx, c);
        CheckTrue(dA >= 0 && dC >= 0,
                  "G5 前提：两条 `EvtKind.Death` 都在（A=" + dA + " / C=" + dC + "）" + LogTail(ctx));
        CheckTrue(dA < dC,
                  "G5 ★★ 外层 A 的死讯（" + dA + "）**先于**嵌套 C 的死讯（" + dC + "）"
                + " —— 原版 FIFO：新死者追加**队尾**"
                + "；🧨 在 `FlushDeaths` 里递归 `FlushDeaths`（或把新死者**头插**）⇒ 变成 A→C→B / C 抢到 A 之前，本行红"
                + LogTail(ctx));

        Check(ctx.DeadUnits.Count, 2, "G5 补充：阵亡登记两条（A 与 C 各一条）");
        int gA = B99E6DeadUnitIdx(ctx, a), gC = B99E6DeadUnitIdx(ctx, c);
        CheckTrue(gA >= 0 && gC >= 0 && gA < gC,
                  "G5 补充：阵亡登记的下标也是 A(" + gA + ") < C(" + gC + ")"
                + "；🧨 嵌套死亡抢先入账 ⇒ 本行红");
    }

    // ==================================================================
    //  G7 · 残骸那一档（判据 → `B97_R8` §1 的「G7」段）
    //
    //  ① 1 血、带 `Remnant` 的单位被打死 ⇒ **翻面成残骸**（留在格位、不记账、不计阵亡）
    //  ② 再打残骸 1 点 ⇒ 残骸被**摧毁**：格位被摘 + **外侧内移**、`TroopsKilled` +1、
    //     `DeadUnits` +1 且 **`IsRemnant == true`**、⛔ **不跑** Backlash / 死亡监听器。
    //  🔴 我们侧两个分支的判据（现读 `RuleCore.cs`）：
    //     · 翻面支：`:9868 DiedThisTurn++` → `:9870 if ((Has(Remnant) || Has(Waystone)) && !IsRemnant)`
    //       ⇒ `ps.Board[slot] = rem`（**还在格位上**）、`leftPlay` 保持 false（⇒ 不进弃牌堆 / 不登记阵亡 / 不计 TroopsKilled）
    //     · 摧毁支：`:9792 if (u.IsRemnant)` ⇒ `BoardSlots.RemoveAt` + `AddTroopKilled` + `DeadUnits.Add(IsRemnant:true)` + `return`
    //       （⚠️ 它在 `:9868 DiedThisTurn++` **之前** return ⇒ 残骸被摧毁**不再**加「本回合阵亡数」）
    // ==================================================================
    static void B99E6G7RemnantTier()
    {
        var ctx = Battle(new[] { Unit("B99E6G7Hero", 0, 9, 9) }, new[] { Unit("B99E6G7Foe", 0, 0, 9) });
        ToP1Turn(ctx, 2);
        var r = Place(ctx, 1, 5, Unit("B99E6G7Rem", 0, 0, 1, "Remnant"), exhausted: true);
        var bg = Place(ctx, 1, 6, Unit("B99E6G7BG", 0, 0, 3), exhausted: true);   // 陪站：用来观测「外侧内移」
        RuleCore.PumpActionQueues(ctx);
        ctx.ClearSignals();

        // ---- 前提 ----
        string why;
        CheckTrue(r.Has(KeywordTable.Remnant), "G7 前提：`r` 带着 `remnant` trait");
        CheckTrue(!r.IsRemnant, "G7 前提：它此刻**还不是**残骸（是正常单位）");
        Check(BoardSlots.CountOnSide(ctx.Players[1], 1), 2, "G7 前提：P1 右侧 5 / 6 号格各一个");
        CheckTrue(BoardSlots.IsContiguous(ctx.Players[1], out why),
                  "G7 前提：右侧连续无洞（" + (why ?? "无洞") + "）");
        Check(ctx.DeadUnits.Count, 0, "G7 前提：阵亡登记是空的");
        Check(ctx.Tally.TroopsKilled[0], 0, "G7 前提：P0 的「部队阵亡」计数是 0");
        int died0 = ctx.DiedThisTurn;

        // ---- ① 打死它 ⇒ 翻面成残骸（留在格位）----
        RuleCore.HurtWithSourceForTest(ctx, r, 5, "夹具", killer: 0);
        RuleCore.PumpActionQueues(ctx);

        CheckTrue(r.Health <= 0, "G7① 前提：原来那个单位被打到 0 血" + LogTail(ctx));
        CheckTrue(Board(ctx, 1, 5) != null,
                  "G7① ★ 5 号格**没空** —— 残骸留在格位上（原版「受伤害或控制者回合结束时被摧毁」）"
                + "；🧨 把翻面支写成「`BoardSlots.RemoveAt` + 直接离场」⇒ 本行红" + LogTail(ctx));
        Check(Board(ctx, 1, 5) != null ? Board(ctx, 1, 5).Name : null, "B99E6G7Rem",
              "G7① 5 号格上那张还是**原来那张卡**（翻面 = 沿用同一个实例，⛔ 不是换牌）");
        CheckTrue(Board(ctx, 1, 5) != null && Board(ctx, 1, 5).IsRemnant,
                  "G7① ★ 它现在是**残骸**（`IsRemnant == true`）"
                + "；🧨 删掉 `(Has(Remnant) || Has(Waystone)) && !IsRemnant` 那道判断 ⇒ 它直接真死、5 号格空 ⇒ 红");
        Check(BoardSlots.CountOnSide(ctx.Players[1], 1), 2,
              "G7① 右侧仍然 2 个（残骸**占着**格位）");
        Check(ctx.DeadUnits.Count, 0,
              "G7① ★ **翻面不登记阵亡**（`DeadUnits` 不增 —— 原版 `GoToCemetery` 只在死亡协程最后一跳跑，翻面走的是另一条路）");
        Check(ctx.Tally.TroopsKilled[0], 0, "G7① ★ **翻面不计「部队阵亡」**（`leftPlay` 仍是 false）");
        Check(ctx.DiedThisTurn, died0 + 1,
              "G7① ★ `DiedThisTurn` **+1** —— 原版 `CardScript +0x250`（`TriggerUnitBacklashActions.c:82`）"
            + "两个分支都写（`CheckIfDead.c:135` 死亡支 / `:141` 变残骸支）"
            + "；🧨 只有「真离场」那支 `++` ⇒ 本行少 1（`A858` 那笔就是这个）");

        // ---- ② 再打残骸 1 点 ⇒ 残骸被摧毁 ----
        var rem = Board(ctx, 1, 5);
        CheckTrue(rem != null, "G7② 前提：5 号格上确实有一个对象（残骸）可打");
        if (rem == null) return;   // 前面已经红过；这里只防 NRE（⛔ 不是静默跳过 —— 上面那条会红）
        int died1 = ctx.DiedThisTurn;
        RuleCore.HurtWithSourceForTest(ctx, rem, 1, "夹具", killer: 0);
        RuleCore.PumpActionQueues(ctx);

        CheckTrue(!rem.IsAlive, "G7② 前提：残骸被打到 0 血" + LogTail(ctx));
        Check(Board(ctx, 1, 5) != null ? Board(ctx, 1, 5).Name : null, "B99E6G7BG",
              "G7② ★★ **外侧内移**：陪站那张从 6 号格补到 5 号格（`BoardSlots.RemoveAt` 的语义 = 原版 `List.Remove`）"
            + "；🧨 改成 `Board[slot] = null` ⇒ 它留在 6 号格、5 号格变空洞 ⇒ 本行红");
        Check(Board(ctx, 1, 6), null, "G7② 最外那一格空出来了");
        Check(BoardSlots.CountOnSide(ctx.Players[1], 1), 1, "G7② 右侧只剩 1 个");
        CheckTrue(BoardSlots.IsContiguous(ctx.Players[1], out why),
                  "G7② ★ 摧毁之后右侧仍然**连续无洞**（" + (why ?? "无洞") + "）");
        CheckTrue(bg != null && ReferenceEquals(Board(ctx, 1, 5), bg),
                  "G7② 补位上来的是**同一份**陪站单位（按对象比，⛔ 不是「名字相同就算」）");

        Check(ctx.DeadUnits.Count, 1, "G7② ★ 阵亡登记 **+1**（残骸被摧毁这一档才登记）");
        CheckTrue(ctx.DeadUnits.Count > 0 && ctx.DeadUnits[ctx.DeadUnits.Count - 1].IsRemnant,
                  "G7② ★★ `DeadUnits` 里那条 **`IsRemnant == true`** —— 原版 `CemeteryManager__AddCardToCemetery` 不清 `+0x65`，"
                + "而 `BattleManager__GetCardsInCemetery.c:60` **无条件排除**它"
                + "；🧨 写成 `IsRemnant = false` ⇒ 墓地查询会多出这张（原版查不出来）");
        Check(ctx.Tally.TroopsKilled[0], 1,
              "G7② ★ 残骸被摧毁**也算一次「部队阵亡」**（记给凶手座位 0）—— 原版 `:112 TroopKilled` 无条件"
            + "；🧨 挪回 `TryCreditKillCore`（那支 `if (victim.IsRemnant) return false`）⇒ 本行少 1（`B94_Y1` 那笔）");

        // ⚠️ **本步故意不断 `DiedThisTurn`**（如实记，别把它写成断言）：
        //    现读 `RuleCore.cs` 的 `if (u.IsRemnant)` 摧毁支在 `ctx.DiedThisTurn++` **之前** `return`
        //    ⇒ 今天「残骸被摧毁」**不**加「本回合阵亡数」；而原版那一跳走的是 `CardScript__CheckIfDead.c:135`
        //    （死亡支，同样写 `+0x250 = turnNumber`）⇒ **今天算不算一致还没查清**。
        //    🔴 铁律 2：查不清就**不写成断言**（写死一边 = 把没核实的口径钉死，工程吃过这个亏）。
        //    已记进报告「没查清的部分」，由调度台决定要不要立账。
        //    （`died1` 保留只作对照读数，⛔ 别拿它去做 `Check`。）
        if (died1 != ctx.DiedThisTurn)
            UnityEngine.Debug.Log("[B99_E6] 提示（不是断言）：残骸摧毁这一步改动了 DiedThisTurn —— "
                                  + died1 + " → " + ctx.DiedThisTurn + "；口径未核，见报告「没查清的部分」");
    }
}
