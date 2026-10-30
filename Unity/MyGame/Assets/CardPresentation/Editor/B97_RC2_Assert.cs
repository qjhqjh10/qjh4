// B97_RC2_Assert.cs — 执行代理 `B97_RC2` 的三件（2026-10-29 · 第九十七会话）
//
// 本件是 `RuleEngineTest` 的**分册**（`public static partial class RuleEngineTest`，与 `B97_RC_Assert.cs`
// 同一种形状）—— `RuleEngine/Editor/RuleEngineTest.cs` 本批**不在白名单**
// ⇒ 测试体全写在这里，接线只需在 `RuleEngineTest.cs` 里加一处：
//      把 `RuleEngineTest.cs:1359` 那一行（`CheckB93G1PlayedCardTail(); CheckB97RCGaps();`）
//      改成 `CheckB93G1PlayedCardTail(); CheckB97RCGaps(); CheckB97RC2Gaps();`
// ⚠️ 本方法名**不以 `Test` / `Report` 打头** ⇒ 不会被 `RunCoverageCompiledCases()` 当成「有 Test* 但没 Step」判红。
//
// ============================ 它盯的是哪几条 ============================
//   A 账 `战斗逻辑驱动:1248`（**平手选靶那一半**）—— `ResolveBerzerkAttacks` 的候选枚举次序
//      判据 = `BattleManager__ResolveEndTurn.c:64` → `AI__SelectBestAttackAction.c:25`
//             → `BattleManager__GetAvailableActions.c` 里那句 `GetAvailableAttackActions(bm, 攻方, 0)`
//             → `BattleManager__GetAvailableAttackActions.c:100-190` **正序**扫 `mgr+0x470`（= `unitsInPlay`）
//             ＋ `AI__SelectBestAction.c` 严格 `>`（平手留**先出现**的）
//      ⇒ 我们要的是 **`RuleCore.ValidTargetSlots`（进场序正序）**，⛔ **不是**
//        `BattleManager.GetAvailableAttackTargets` 的倒序（那个只服务 `EndTurnClick` / `GetRandomAttackTarget`）。
//   B 结构差 `#30`（**守方那一半**）—— `RuleCore.cs` 的 `targetWillDieFromThis` 口径
//      判据 = 原版那一格读 `CardScript.EnoughPendingDamageToDie(守方)`（待结算量口径）
//      ⇒ 我们的等价物 = `RuleCore.WouldKillByEntries`（`…WithDamageValues` 的等价物），
//        旧写法 `DamageAfterReduction(...) >= Health` 漏 幸存者 / 堡垒 / 空投舱 三档。
//   C 账 `战斗逻辑驱动:816`（**谓词收口**）—— 「回合开始会到期吗」这条谓词原来有**两份**
//      （`UnitState.RevertBuffs(atTurnEnd:false,…)` 与 `RuleCore.EnqueueTurnSetupEffectRemovals`）
//      ⇒ 已提成 `TempBuff.IsDueAtTurnStart(player)` 一处（`UnitState.cs` 文件尾那份 `partial` 段）。
//
// ============================ 🔴 灭自证（每条带 🧨）============================
//    ① **前提断言与结论断言并列写**，⛔ 不写 `if (前提) Check(结论)`；
//    ② 期望值一律**字面量**，⛔ 不从被测实现反算；
//    ③ 每条都配一条「两边一起改回去仍然绿」的对照断言（A 的常量评分器 / B 的无 survivor 对照 / C 的旧副本 grep）。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口。</summary>
    public static void CheckB97RC2Gaps()
    {
        Section("🆕 B97_RC2：berzerk 选靶次序(1248) · 守方判死口径(#30 下半) · 到期谓词收口(816)");
        B97RC2BerzerkTargetOrder();
        B97RC2TargetWillDiePredicate();
        B97RC2DuePredicateSingleSource();
    }

    // ------------------------------------------------------------ A：berzerk 的候选枚举次序
    /// <summary>
    /// 判别式 = **进场序 ≠ 格号序**，且**两个目标评分平手**：
    /// 敌方先摆 8 号格、再摆 2 号格（⇒ 进场序 `[8, 2]`，格号序 `[2, 8]`），
    /// 打分器给这两张**同一个数** ⇒ 「打谁」**只**由枚举次序决定。
    /// <para>新口径（进场序正序）⇒ 先遇到 **8 号格** ⇒ `dead/3`；</para>
    /// <para>旧口径（格号升序 0→8）⇒ 先遇到 **2 号格** ⇒ `3/dead`（🧨 正是本条要挡的回归）。</para>
    /// <para>🔴 **2026-10-30（`B100_F37` · 铁律 5 订正）**：原写「敌方 4 号格上永远是督军，但它**没有进场号**
    /// （`PlayOrder = -1`）⇒ `StableSortByPlayOrder` 把没取号的排末尾 ⇒ 它不会抢在 t8 前面」—— **这句是错的**：
    /// `RuleCore.cs:663` 自 2026-10-21（`08e1d726`，比本断言早 8 天）起就给督军 `ctx.TakePlayOrder()` ⇒
    /// 它的进场号**最小**；而 `ValidTargetSlots`（`RuleCore.cs:17188`）按 `PlayOrder` 升序枚举 ⇒
    /// 平手时**督军第一个被选中**（实测实得 `3/3`、`t8`/`t2` 满血）。⇒ 夹具必须显式把 4 号格压到最低分
    /// （`TieExceptSlotScorer`），判别式才仍然只由「那两个目标之间的枚举次序」决定。</para>
    /// </summary>
    static void B97RC2BerzerkTargetOrder()
    {
        // 钩子是**进程级**全局挂载点 ⇒ 用完必须换回来（否则污染同一次自检后面那几条）。
        var saved = AiScorerHook.Scorer;
        try
        {
            // 两个目标**同分**（平手）；敌方 4 号格的督军压到最低分
            // —— 它一定在候选表里、而且进场号最小（`RuleCore.cs:663`）⇒ 不压分它会第一个被选中（见上面那段订正）
            var tie = new TieExceptSlotScorer { ExcludeSlot = RuleCore.WarlordSlot, TieScore = 1f, ExcludeScore = 0f };
            AiScorerHook.Scorer = tie;
            string r = B97RC2BerzerkTrace();

            CheckTrue(tie.Calls >= 2,
                $"★（前提）狂暴选靶**真的问了评分器**（实测 {tie.Calls} 次，要求 ≥ 2 = 两个目标各一次；"
              + "本夹具实际是 **3** 次 —— 第三个候选是敌方 4 号格的督军，由 `ExcludeSlot` 压到 0 分）"
              + "｜🧨 候选表空了 / 钩子没装上 ⇒ 下面的结论是假的 ⇒ 红");
            Check(r, "dead/3",
                "★★ berzerk 的候选按**进场序正序**枚举 ⇒ 平手时打**先进场**的那张（8 号格）"
              + "（判据 `BattleManager__GetAvailableAttackActions.c:100-190` 正序扫 `mgr+0x470` ＋ `AI__SelectBestAction.c` 严格 `>`）"
              + "｜🧨 改回「敌方格号升序 0→8」⇒ 先遇到 2 号格 ⇒ 实得 `3/dead`（账 1248 的 B 档就是这一格）"
              + "｜🧨 夹具不给督军压分 ⇒ 平手第一个是**督军** ⇒ 实得 `3/3`（2026-10-30 订正的正是这一格）");
        }
        finally
        {
            AiScorerHook.Scorer = saved;
        }
    }

    /// <summary>A 的夹具：狂暴单位（5 攻）在 3 号格，敌方**先摆 8 号格、再摆 2 号格**
    /// 各一个**完全等价**的 3 费 3 血目标（谁被打死只可能由「枚举次序」决定，因为这两张的评分被钉成同一个值；
    /// 🔴 2026-10-30 起敌方 4 号格的督军由 `TieExceptSlotScorer` 压到最低分 —— 它不再参与平手，见上面的订正）。
    /// 返回 `"8 号格那张的生死/2 号格那张的生死"` —— 用血量/生死而不是格号，
    /// 避开「阵亡补位会把单位挪走」那一层（本工程棋盘是连续无洞模型）。</summary>
    static string B97RC2BerzerkTrace()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
        RuleCore.BeginTurn(ctx);
        var z = Place(ctx, 0, 3, Unit("Zerker2", 1, 5, 9));
        z.AddKeyword(RuleCore.Berzerk, 1);
        var t8 = Place(ctx, 1, 8, Unit("B97RC2T8", 3, 0, 3));   // 先进场（进场号更小）
        var t2 = Place(ctx, 1, 2, Unit("B97RC2T2", 3, 0, 3));   // 后进场（格号更小）
        // 🔴 2026-10-30（`B100_F37` · 铁律 5）：原注释写「敌方 4 号格的督军**没有进场号**（`PlayOrder = -1`）」
        //   —— **实际是错的**（`RuleCore.cs:663` 自 2026-10-21 起就 `TakePlayOrder()`）⇒ 先钉死这个前提，
        //   免得下一个人再按那句错注释去改夹具。
        var wl = ctx.Players[1].Warlord;
        CheckTrue(wl != null && ctx.Players[1].Board[RuleCore.WarlordSlot] == wl,
                  "★（前提）敌方 4 号格上常驻督军 —— 它是狂暴的**第三个**候选（`RuleCore.cs:655`）");
        CheckTrue(wl != null && t8.PlayOrder > wl.PlayOrder,
                  "★★（前提）督军**有**进场号、而且比 t8 小 ⇒ 不给它压分的话平手第一个就是它（实得 `3/3`）"
                + "｜🧨 有人把督军改回 `PlayOrder = -1` ⇒ 红（那时本夹具的两个目标才够用，`ExcludeSlot` 变成空转）");
        RuleCore.EndTurn(ctx);
        return (t8.IsAlive ? t8.Health.ToString() : "dead")
             + "/" + (t2.IsAlive ? t2.Health.ToString() : "dead");
    }

    /// <summary>自检用的假评分器：**除了 `<see cref="ExcludeSlot"/>` 那一格，其余目标全部同分**
    /// （`ExcludeSlot` 拿最低分）。
    /// <para>为什么要它：本件 A 的判别式要求「两个目标平手、谁被选中只由枚举次序决定」，而**敌方 4 号格上
    /// 永远有督军**（`RuleCore.cs:655`）且它的进场号**最小**（`RuleCore.cs:663`）⇒ 用「人人同分」的
    /// `PreferSlotScorer` 时平手第一个是**督军**，判别式会被它抢走（2026-10-30 实测实得 `3/3`）。</para>
    /// <para>⚠️ 它**不改**判别式的形状：两个目标之间仍是严格平手 ⇒ 仍由枚举次序决定。</para></summary>
    sealed class TieExceptSlotScorer : IAiScorer
    {
        public int ExcludeSlot;
        public float TieScore = 1f, ExcludeScore = 0f;
        public int Calls;

        public float ScoreAttack(BattleContext ctx, int side, int slot, int foeSlot, bool ranged)
        {
            Calls++;
            return foeSlot == ExcludeSlot ? ExcludeScore : TieScore;
        }
    }

    // ------------------------------------------------------------ B：`targetWillDieFromThis` 的判死口径
    /// <summary>
    /// 判别式 = **幸存者**：`DamageAfterReduction(survivor, 5) >= Health(3)` 为真（旧口径判「必死」），
    /// 而 `WouldKillByEntries(survivor, [5])` 为假（幸存者吃掉这一击 ⇒ 它没死 ⇒ **反击照打**）。
    /// 三条一起断：旧口径真 / 新口径假 / **无 survivor 的对照两条都真**
    /// （最后那条挡「把新口径写成恒 `false`」—— 那也能让第 2 条变绿）。
    /// </summary>
    static void B97RC2TargetWillDiePredicate()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int foe = 1 - ctx.Active;
        var surv = Place(ctx, foe, 2, Unit("B97RC2Surv", 1, 1, 3, "Survivor 1"), exhausted: true);
        var plain = Place(ctx, foe, 6, Unit("B97RC2Plain", 1, 1, 3), exhausted: true);

        Check(surv.Health, 3, "★（前提）幸存者那个夹具 **3 血** ｜🧨 血量改了 ⇒ 下面两条的判别式失效 ⇒ 红");
        Check(surv.Survivor, 1,
            "★（前提）`survivor` 读出来是 **1** ｜🧨 关键词写法换了（大小写 / 数值格式）⇒ 读不到 ⇒ 红");
        Check(plain.Survivor, 0, "★（前提）对照单位**没有** survivor ｜🧨 夹具串了 ⇒ 红");

        CheckTrue(RuleCore.DamageAfterReduction(surv, 5) >= surv.Health,
            "★（前提）**旧口径**（`DamageAfterReduction >= Health`）把这一记判成「会死」—— 正是改前误判的那一格"
          + "｜🧨 伤害 / 血量 / 关键词改了 ⇒ 红");
        CheckTrue(!RuleCore.WouldKillByEntries(surv, new[] { 5 }),
            "★★ **新口径**（`WouldKillByEntries` ≈ 原版 `EnoughPendingDamageToDie(守方)`）判**不会死** —— 幸存者吃掉这一击"
          + "（原版那一格要的是「待结算量能不能判死」，含 幸存者 / 堡垒 / 空投舱 三档）"
          + "｜🧨 改回 `DamageAfterReduction(target, atk) >= target.Health` ⇒ 结构差 #30 的守方那一半复发");
        CheckTrue(RuleCore.DamageAfterReduction(plain, 5) >= plain.Health
               && RuleCore.WouldKillByEntries(plain, new[] { 5 }),
            "★★ **灭自证**：没有 survivor 的同类单位，两条口径**都**判「会死」"
          + "｜🧨 把新口径写成恒 `false`（只为让上一条变绿）⇒ 这里红 —— 结构上不可能同时满足");

        // 结构断言：调用点真的换了（行为那半埋在 `DeclareAttack` 的私有体里，没有纯函数入口）
        string src = B97RCSourceOf("RuleEngine/Core/RuleCore.cs");
        CheckTrue(src != null,
            "★（前提）读得到 `RuleEngine/Core/RuleCore.cs` 的源码文本（读不到 ⇒ 下一条红，⛔ 不静默跳过）"
          + "｜🧨 文件被挪走 / 改了名 ⇒ 红");
        CheckTrue(src != null && src.Contains("WouldKillByEntries(target, new[] { atk })"),
            "★★ `targetWillDieFromThis` 那一句用的是 `WouldKillByEntries(target, …)`"
          + "｜🧨 有人改回 `DamageAfterReduction(target, atk) >= target.Health` ⇒ 红");
    }

    // ------------------------------------------------------------ C：到期谓词只有一份
    /// <summary>
    /// 三件事：① 谓词本身的真值表（四档字面量）；② **两个消费者判定同一批**
    /// （`RuleCore.EnqueueTurnSetupEffectRemovals` 与 `UnitState.RevertBuffs(atTurnEnd:false,…)`）；
    /// ③ 源码里**旧的那份三元表达式已经没有了**（⛔ 防「加了个新方法，旧副本还留着」）。
    /// </summary>
    static void B97RC2DuePredicateSingleSource()
    {
        // ---- ① 唯一判据的真值表（字面量；⛔ 不从被测实现反算）----
        var myNext = new UnitState.TempBuff { Name = "attack", Value = 1, Owner = 0, UntilMyNextTurn = true };
        var oppNext = new UnitState.TempBuff { Name = "attack", Value = 1, Owner = 0, UntilOpponentNextTurn = true };
        var turnEnd = new UnitState.TempBuff { Name = "attack", Value = 1, Owner = 0 };
        CheckTrue(myNext.IsDueAtTurnStart(0),
            "★ `until my next turn` ∧ 轮到施放者 ⇒ **到期**（原版 `OnTurnSetup` 的 `+0x33` 档）"
          + "｜🧨 `Owner == turnPlayer` 写反 ⇒ 红");
        CheckTrue(!myNext.IsDueAtTurnStart(1),
            "★ `until my next turn` ∧ 轮到的**不是**施放者 ⇒ 不到期"
          + "｜🧨 谓词漏了 `Owner` 那一半 ⇒ 红");
        CheckTrue(oppNext.IsDueAtTurnStart(1),
            "★ `until opponent next turn` ∧ 轮到的**不是**施放者 ⇒ **到期**（`+0x34` 档）"
          + "｜🧨 `Owner != turnPlayer` 写反 ⇒ 红");
        CheckTrue(!oppNext.IsDueAtTurnStart(0),
            "★ `until opponent next turn` ∧ 轮到施放者自己 ⇒ 不到期"
          + "｜🧨 `Owner != turnPlayer` 写反 ⇒ 红");
        // 两位同时为真 ⇒ **`+0x34` 优先**（改前两处的三元表达式就是这个次序，⛔ 别在收口时改掉）
        var both = new UnitState.TempBuff { Name = "attack", Value = 1, Owner = 0, UntilMyNextTurn = true, UntilOpponentNextTurn = true };
        CheckTrue(both.IsDueAtTurnStart(1) && !both.IsDueAtTurnStart(0),
            "★ 两个标志都为真 ⇒ 按 `until opponent next turn` 那档走（`+0x34` 优先）：轮到 `1` 到期、轮到 `0`（施放者）**不**到期"
          + "｜🧨 收口时把优先级写反（先判 `UntilMyNextTurn`）⇒ 这里两个都翻 ⇒ 红");
        CheckTrue(!turnEnd.IsDueAtTurnStart(0) && !turnEnd.IsDueAtTurnStart(1),
            "★ 两个标志都为假（= 回合末到期）⇒ **回合开始不动它**"
          + "｜🧨 把回合末那一档并进来 ⇒ 红（那会让「本回合结束撤」的增益在**回合开始**就没了）");

        // ---- ② 两个消费者判定同一批 ----
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        int seat = ctx.Active, other = 1 - seat;
        var a = Place(ctx, seat, 2, Unit("B97RC2DA", 1, 1, 5));
        var b = Place(ctx, seat, 3, Unit("B97RC2DB", 1, 1, 5));
        var c = Place(ctx, other, 2, Unit("B97RC2DC", 1, 1, 5));
        var d = Place(ctx, seat, 6, Unit("B97RC2DD", 1, 1, 5));
        // 四档各一条（与 `B97_RC_Assert.cs` 的 D 同一套夹具口径）
        a.AddTempBuff(new UnitState.TempBuff { Name = "attack", Value = 1, Owner = seat, UntilMyNextTurn = true });        // 到期（+0x33）
        b.AddTempBuff(new UnitState.TempBuff { Name = "attack", Value = 1, Owner = other, UntilMyNextTurn = true });      // 不到期
        c.AddTempBuff(new UnitState.TempBuff { Name = "attack", Value = 1, Owner = other, UntilOpponentNextTurn = true }); // 到期（+0x34：施放者=`other`，轮到 `seat` ⇒ `Owner != turnPlayer`）
        d.AddTempBuff(new UnitState.TempBuff { Name = "attack", Value = 1, Owner = seat });                               // 回合末到期

        int expectDue = 0;
        foreach (var u0 in new[] { a, b, c, d })
            foreach (var tb in u0.TempBuffs)
                if (tb != null && tb.IsDueAtTurnStart(seat)) expectDue++;
        Check(expectDue, 2,
            "★（前提）按唯一判据数下来**恰好 2 条**到期（A 的 `+0x33` 档 ＋ C 的 `+0x34` 档）"
          + "｜🧨 夹具的四档串了 ⇒ 红");

        int queued = RuleCore.EnqueueTurnSetupEffectRemovals(ctx, seat);
        Check(queued, 2,
            "★★ 入队那一路（`EnqueueTurnSetupEffectRemovals`）也判**同样这 2 条**"
          + "｜🧨 谓词抄错一档 ⇒ 1 或 3 ⇒ 红");

        int reverted = a.RevertBuffs(false, seat) + b.RevertBuffs(false, seat)
                     + c.RevertBuffs(false, seat) + d.RevertBuffs(false, seat);
        Check(reverted, 2,
            "★★ 撤除那一路（`UnitState.RevertBuffs(atTurnEnd:false,…)`）判的也是**同样这 2 条** —— "
          + "两边共用 `TempBuff.IsDueAtTurnStart`（铁律 6：两处写同一条规则 = 迟早不一致）"
          + "｜🧨 只改了一侧（另一边还留着旧三元表达式）⇒ 这里会得 1 / 3 ⇒ 红");

        // ---- ③ 旧副本必须已经不在了 ----
        string unitSrc = B97RCSourceOf("RuleEngine/Core/UnitState.cs");
        string coreSrc = B97RCSourceOf("RuleEngine/Core/RuleCore.cs");
        CheckTrue(unitSrc != null && coreSrc != null,
            "★（前提）两份源码都读得到 ｜🧨 文件被挪走 / 改名 ⇒ 红");
        CheckTrue(unitSrc != null && unitSrc.Contains("b.IsDueAtTurnStart(player)"),
            "★★ `UnitState.RevertBuffs` 的回合开始那一档**转调**了 `TempBuff.IsDueAtTurnStart`"
          + "｜🧨 改回内联三元表达式 ⇒ 红");
        CheckTrue(coreSrc != null && coreSrc.Contains("b.IsDueAtTurnStart(seat)"),
            "★★ `RuleCore.EnqueueTurnSetupEffectRemovals` 也**转调**同一个口 ｜🧨 同上 ⇒ 红");
        CheckTrue(unitSrc != null && !unitSrc.Contains("b.UntilOpponentNextTurn ? (b.Owner != player)")
               && coreSrc != null && !coreSrc.Contains("b.UntilOpponentNextTurn ? (b.Owner != seat)"),
            "★★ **旧的那份三元表达式两份都不见了** —— 这才是「收成一处」"
          + "｜🧨 加了新方法却把旧副本留着（那正是改前的形状）⇒ 红");
    }
}
