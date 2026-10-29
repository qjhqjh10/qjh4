// B98G3_Assert.cs — 执行代理 `B98_G3`（第九十八会话 · 波 5b · 战斗线）的断言集
//
// ============================ 它盯的是什么 ============================
//   账 `战斗逻辑驱动 :324 / :325` —— **AI 攻击动作表（`SimpleAI.EnumerateActions` 的攻击那两档）的「攻击目标」判据**：
//     原版 `BattleManager.GetAvailableAttackActions` 每个 (攻方, 目标) 对都过**同一个合取**：
//       · **目标侧**：`!CardScript.EnoughPendingDamageToDie(目标)`（`BattleManager__GetAvailableAttackActions.c:126` 近战 / `:152` 远程）
//                  ⇒ 目标已带致死待结算伤害 ⇒ **近战 / 远程两档都不排**；
//       · **攻方侧**：`0 < get_CurrentMeleeAttack` ∧ `!HasCurrentTrait(0x3ca pindown)`（`:128-140`）/
//                  `0 < get_CurrentRangeAttack`（`:154-166`，**远程没有 pindown 那一条**）。
//   我们的落点（本笔）：
//     · 新建 `RuleEngine/Core/RuleCorePartial_B98G3AI.cs` 的 `RuleCore.AttackTargetGate(ctx,p,attacker,target,ranged)`
//       —— 上面三条的**唯一一份**判据（攻方那两条**转调**已有的 `AttackActionPowerGate`，⛔ 不复制第二份）；
//     · `RuleEngine/Data/SimpleAI.cs:374`（**同一物理行**追加，零行数变化）—— 攻击枚举那一格**唯一**的调用点。
//   ⛔ **没有**落进 `RuleCore.IsValidTarget`：那个口同时服务**技能目标**，2026-10-29 实测落进去 ⇒
//     `play check` ①-b 由 50 张变 **414 张**（回退记录在 `RuleCore.cs:5625`）。
//   ⛔ **玩家路径没加**：原版那两条闸只作用于动作表；玩家点目标走 `DeclareAttackCore` → `CanAttackNow`，
//     攻方那两道闸已经在那儿（A4 段就是这条回归）。
//
// ============================ 判据出处（⛔ 别在这里抄第二份）============================
//   `d:/2/tools/decomp_full/BattleManager__GetAvailableAttackActions.c:126 / :152 / :128-140 / :154-166`
//   `d:/4/Unity/资料/完全复刻/判据/B97_R10_波5残差归属.md` §三·1（本批的施工表）
//   `d:/4/Unity/资料/完全复刻/报告/B98_G3.md`（本代理：含 `AiScoring` 为什么**不加**的取证）
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · §0（**前提 / 控制组**）：正常攻击者（近战 3）对活目标 ⇒ 枚举里**真有**近战那条。
//     没有这一段，下面所有「没有近战那条」都分不清是**闸在起作用**还是**表本来就是空的**。
//   · A1：近战 0 ⇒ 近战那条**消失**；**同一次调用**里 `IsValidTarget` 对同一对**仍返 `OK`**
//     —— 这一条就是「闸在**动作表**那一侧、不在共用判据里」的**灭自证**（把 `AttackTargetGate` 并进
//     `IsValidTarget` 就会让这一条红，同时 `play check` 的 ①-b 会从 50 张跳到 414 张）。
//   · A1b：同一棋盘只把近战改成 1 ⇒ 近战那条**必须回来**。A1 / A1b **结构上不可能同时满足**。
//   · A2：目标 `Health = 0`（前提：`EnoughPendingDamageToDie(目标) == true`）⇒ 近战那条消失；A2b 救回来 ⇒ 回来。
//   · A3：近战 3 + `pindown`、远程 2 ⇒ 近战那条消失、**远程那条照排**（原版远程那一支没有 pindown）。
//     🧨 把 `AttackActionPowerGate` 的 `!ranged &&` 去掉（远近都禁）⇒ 远程那一条红。
//   · A4（回归）：`CanAttackNow` 对「0 攻近战」仍 `ErrNoMeleeAttack`、对「近战 3」仍 `OK`、
//     对「近战 3 + pindown」仍 `ErrPindown` —— 证明本笔**没有动**玩家那条路。
//   · A5（`play check` 指纹不变）由**调度台**在同步点跑 —— 本代理无权跑 Unity / ruleprobe，故不在此实现。
//
// ============================ 账 `:324` 的「其余」—— `allowAutoActions == true` ============================
//   原版 `GetAvailableActions(CardScript, bool allowAutoActions)` 那个形参我们**没有等价物**
//   （`SimpleAI.EnumerateActions` 的两档回合闸直接读 `ctx.Active`，见 `SimpleAI.cs:326` 那一行）。
//   ⇒ **如实记「无等价物」**，⛔ **不给 `EnumerateActions` 加一个没人能置真的形参**（那就是死代码）。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B98G3_Assert.Run -logFile "d:/4/_tmp_view/b98g3.log"
//   判绿 = 合计那行 `失败 0`。
//   想并进别的宿主就两行：`B98G3_Assert.Run(); pass += B98G3_Assert.Pass; fail += B98G3_Assert.Fail;`
//   ⚠️ 本文件**不建场景 / 不建资产 / 不进任何 `Editor/*Scene.cs`**；只造内存里的 `BattleContext`。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>`B98_G3`：AI 攻击动作表的「攻击目标」判据（账 `:324 / :325`）。计数口径照 `CheckSink`。</summary>
public static class B98G3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[G3] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>本趟通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>本趟失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }
    /// <summary>本趟实际执行的断言条数（= Pass + Fail；证明断言真被跑到）。</summary>
    public static int RunCount { get { return _sink.Pass + _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    const int MySlot = 3;      // 我方攻击单位（`ourSlot = 4 + 原版有符号下标` ⇒ -1 = 督军左侧第一格）
    const int FoeSlot = 3;     // 敌方目标（同号，另一侧棋盘）

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0;
        try
        {
            MenuCheck.Section(_sink, "B98_G3 账 324/325：AI 攻击动作表的「攻击目标」判据"
                                   + "（目标将死不排 / 攻方 0 攻不排 / 近战 pindown 不排）");
            Section0_控制组();
            SectionA1_零攻击力();
            SectionA2_目标将死();
            SectionA3_pindown只禁近战();
            SectionA4_回归CanAttackNow();
            MenuCheck.Section(_sink, "A5（`play check` 指纹不变）由调度台在同步点跑 —— 本代理不跑 Unity/ruleprobe");
        }
        catch (System.Exception e)
        {
            _sink.Fail++;
            Debug.LogError(_sink.P + "✗ 断言过程抛异常（后面的条目没跑完）：" + e);
        }
        Debug.Log(_sink.P + $"===== 合计 通过 {_sink.Pass} / 失败 {_sink.Fail} —— "
                          + (_sink.Fail == 0 ? "✅ 全绿" : "❌ 有红") + " =====");
    }

    // ==================================================================
    //  §0 前提 / 控制组 —— 「表里本来就有攻击动作」这一条必须先成立
    // ==================================================================
    static void Section0_控制组()
    {
        MenuCheck.Section(_sink, "§0 前提：正常攻击者（近战 3 / 远程 0）⇒ 枚举里**真有**近战那条、**没有**远程那条");

        var ctx = Battle();
        Place(ctx, 0, MySlot, Unit("G3Atk3", 1, 3, 5), exhausted: false);
        Place(ctx, 1, FoeSlot, Unit("G3Prey", 1, 0, 9), exhausted: true);
        var acts = SimpleAI.EnumerateActions(ctx);

        Tr(HasAttack(ctx, acts, MySlot, FoeSlot, false),
           "★ §0-a **控制组**：近战 3 的攻击者 ⇒ 枚举里**有**「近战打敌方 3 号格」那一条"
         + "｜🧨 若攻击枚举那一整段没了（或 `PlayOrderedSlots` 没把单位排进来）⇒ 本条红 —— "
         + "没有本条，下面所有「没有近战那条」都分不清是闸在起作用还是表本来就空");
        Tr(!HasAttack(ctx, acts, MySlot, FoeSlot, true),
           "★ §0-b 同一张卡远程 = 0 ⇒ 远程那条**不在**（两档各自判攻击力，⛔ 不是「有一条就两档都算」）");
        C(RuleCore.IsValidTarget(ctx, 0, MySlot, 1, FoeSlot, false), RuleCodes.OK,
          "★ §0-c 前提：共用判据 `IsValidTarget` 对这对**本来是合法**的（下方 A1 的「仍 OK」才有意义）");
        Dump("§0", acts);
    }

    // ==================================================================
    //  A1 —— 攻方 0 攻：动作表不排，但共用判据照旧放行（灭自证）
    // ==================================================================
    static void SectionA1_零攻击力()
    {
        MenuCheck.Section(_sink, "A1 近战 0（远程 0）的攻击者 ⇒ 动作表里两档**都没有**；"
                               + "同一次调用里 `IsValidTarget` 对同一对**仍返 OK**（闸在动作表、不在共用判据）");

        var ctx = Battle();
        Place(ctx, 0, MySlot, Unit("G3NoAtk", 1, 0, 5), exhausted: false);
        Place(ctx, 1, FoeSlot, Unit("G3Prey1", 1, 0, 9), exhausted: true);
        var acts = SimpleAI.EnumerateActions(ctx);

        Tr(!HasAttack(ctx, acts, MySlot, FoeSlot, false),
           "★ A1-a **近战 0 ⇒ 近战那条不排**（原版 `GetAvailableAttackActions.c:128-140` 的 `0 < CurrentMeleeAttack`）"
         + "｜🧨 把 `SimpleAI.cs:374` 追加的 `RuleCore.AttackTargetGate(...)` 那半删掉 ⇒ 实得「有」⇒ 红");
        Tr(!HasAttack(ctx, acts, MySlot, FoeSlot, true),
           "★ A1-b **远程 0 ⇒ 远程那条不排**（`.c:154-166`）—— 与 A1-a 是**两支**，不是同一条");

        // 🔴 灭自证：闸**不在**共用判据里（并进去 ⇒ 技能目标会被误挡，实测 50→414 张）
        C(RuleCore.IsValidTarget(ctx, 0, MySlot, 1, FoeSlot, false), RuleCodes.OK,
          "★ A1-c **灭自证**：`IsValidTarget`（共用口）对同一对**仍返 OK** —— 证明这次拒绝只发生在**动作表**那一侧"
        + "｜🧨 把这两道闸并进 `RuleCore.IsValidTarget` ⇒ 本条红（历史实测：`play check` ①-b 50→414 张）");

        // A1d：同一棋盘只把近战改成 1 ⇒ 近战那条必须回来
        Place(ctx, 0, 2, Unit("G3Atk1", 1, 1, 5), exhausted: false);
        var acts2 = SimpleAI.EnumerateActions(ctx);
        Tr(HasAttack(ctx, acts2, 2, FoeSlot, false),
           "★ A1-d **灭自证**：同一棋盘、同一刻，另一个格位放一张**近战 1** ⇒ 近战那条**必须回来**"
         + "（与 A1-a 结构上不可能同时满足；证明 A1-a 拒的确实是「攻击力 ≤ 0」这一条）");
        Dump("A1", acts2);
    }

    // ==================================================================
    //  A2 —— 目标已带致死待结算伤害：两档都不排
    // ==================================================================
    static void SectionA2_目标将死()
    {
        MenuCheck.Section(_sink, "A2 目标已经要死了（`EnoughPendingDamageToDie`）⇒ 动作表**两档都不排**"
                               + "（原版 `GetAvailableAttackActions.c:126 / :152` 是**两支各判一次**）");

        var ctx = Battle();
        Place(ctx, 0, MySlot, Unit("G3AtkA2", 1, 3, 5), exhausted: false);
        var prey = Place(ctx, 1, FoeSlot, Unit("G3Dying", 1, 0, 9), exhausted: true);

        var before = SimpleAI.EnumerateActions(ctx);
        Tr(HasAttack(ctx, before, MySlot, FoeSlot, false),
           "★ A2 前提：目标还活着时近战那条**在**（否则下面的「消失」分不清是谁在起作用）");

        prey.Health = 0;
        Tr(RuleCore.EnoughPendingDamageToDie(prey),
           "★ A2 前提：夹具真的把目标摆成「已带致死待结算伤害」了（`Health≤0 && Survivor≤0`）");

        var after = SimpleAI.EnumerateActions(ctx);
        Tr(!HasAttack(ctx, after, MySlot, FoeSlot, false),
           "★ A2-a **目标将死 ⇒ 近战那条不排**"
         + "｜🧨 把 `AttackTargetGate` 里 `EnoughPendingDamageToDie(target)` 那半删掉 ⇒ 红");
        Tr(!HasAttack(ctx, after, MySlot, FoeSlot, true),
           "★ A2-b **目标将死 ⇒ 远程那条也不排**（原版 `.c:152` 与 `.c:126` 各判一次，不是只不排近战）");

        prey.Health = 9;
        var third = SimpleAI.EnumerateActions(ctx);
        Tr(HasAttack(ctx, third, MySlot, FoeSlot, false),
           "★ A2-c **灭自证**：把它救回来之后近战那条**必须回来**"
         + "｜🧨 把 `EnoughPendingDamageToDie(target)` 写成常量 `true`（一律拒）⇒ 本条红 —— 与 A2-a 结构上不可能同时满足");
        Dump("A2(救回后)", third);
    }

    // ==================================================================
    //  A3 —— pindown 只禁近战，远程照排
    // ==================================================================
    static void SectionA3_pindown只禁近战()
    {
        MenuCheck.Section(_sink, "A3 近战 3 + `pindown(0x3ca)`、远程 2 ⇒ 近战那条不排、**远程那条照排**");

        var ctx = Battle();
        Place(ctx, 0, MySlot, RangedUnit("G3Pindown", 1, 3, 5, 2, "pindown"), exhausted: false);
        Place(ctx, 1, FoeSlot, Unit("G3Prey3", 1, 0, 9), exhausted: true);
        Tr(ctx.Players[0].Board[MySlot].Has("pindown"),
           "★ A3 前提：夹具的攻击者真的带上了 `pindown`（关键词表命中，否则下面两条全是空转）");

        var acts = SimpleAI.EnumerateActions(ctx);
        Tr(!HasAttack(ctx, acts, MySlot, FoeSlot, false),
           "★ A3-a **近战被 pindown 压制 ⇒ 近战那条不排**（原版 `.c:128-140` 的第三个合取项 `!HasCurrentTrait(0x3ca)`）"
         + "｜🧨 把 `AttackActionPowerGate` 里 `!ranged && attacker.Has(\"pindown\")` 那半删掉 ⇒ 红");
        Tr(HasAttack(ctx, acts, MySlot, FoeSlot, true),
           "★ A3-b **远程不受压制**：远程 2 ⇒ 远程那条**照排**（原版 `.c:154-166` 那一支**没有** pindown 这个 trait）"
         + "｜🧨 把那道闸写成「远近都禁」（去掉 `!ranged &&`）⇒ 本条红");
        Dump("A3", acts);
    }

    // ==================================================================
    //  A4 —— 回归：玩家那条路（`CanAttackNow`）的返回码一个字没变
    // ==================================================================
    static void SectionA4_回归CanAttackNow()
    {
        MenuCheck.Section(_sink, "A4 回归：`CanAttackNow`（玩家点目标走的那条）返回码不变 —— "
                               + "0 攻近战 `ErrNoMeleeAttack` · 近战 3 `OK` · 近战 3 + pindown `ErrPindown`");

        var ctx = Battle();
        Place(ctx, 0, MySlot, Unit("G3CNoAtk", 1, 0, 5), exhausted: false);
        Place(ctx, 0, 2, Unit("G3CAtk3", 1, 3, 5), exhausted: false);
        Place(ctx, 0, 5, RangedUnit("G3CAtk3P", 1, 3, 5, 2, "pindown"), exhausted: false);
        Place(ctx, 1, FoeSlot, Unit("G3CPrey", 1, 0, 9), exhausted: true);

        C(RuleCore.CanAttackNow(ctx, 0, MySlot, false), RuleCodes.ErrNoMeleeAttack,
          "★ A4-a `CanAttackNow`：0 攻近战仍返 `ErrNoMeleeAttack`（与 `AttackActionPowerGate` 同一个码，改前改后逐字一致）");
        C(RuleCore.CanAttackNow(ctx, 0, 2, false), RuleCodes.OK,
          "★ A4-b `CanAttackNow`：近战 3 仍返 `OK`（灭自证：别把 A4-a 做成「一律拒」）");
        C(RuleCore.CanAttackNow(ctx, 0, 5, false), RuleCodes.ErrPindown,
          "★ A4-c `CanAttackNow`：近战 3 + pindown 仍返 `ErrPindown`（原版次序：先 trait 后攻击力）");
    }

    // ==================================================================
    //  读数辅助
    // ==================================================================
    /// <summary>枚举里有没有「`atkSlot` 用 <paramref name="ranged"/> 那档打敌方 `tgtSlot`」这一条。</summary>
    static bool HasAttack(BattleContext ctx, List<AiAction> list, int atkSlot, int tgtSlot, bool ranged)
    {
        if (list == null) return false;
        var kind = ranged ? AiActionKind.AttackRanged : AiActionKind.AttackMelee;
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null || a.Kind != kind) continue;
            if (a.Slot == atkSlot && a.TargetP == 1 && a.TargetSlot == tgtSlot && a.Ranged == ranged) return true;
        }
        return false;
    }

    /// <summary>把这一趟枚举出来的**攻击类**条目打一行日志（红了以后一眼看是哪条漏了/多了）。</summary>
    static void Dump(string tag, List<AiAction> list)
    {
        var sb = new System.Text.StringBuilder();
        int n = 0;
        if (list != null)
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a == null || (a.Kind != AiActionKind.AttackMelee
                               && a.Kind != AiActionKind.AttackRanged)) continue;
                n++;
                sb.Append($" [{a.Slot}→{a.TargetP}/{a.TargetSlot}{(a.Ranged ? "远" : "近")}]");
            }
        Debug.Log(_sink.P + $"{tag} 攻击类动作 {n} 条：{(n == 0 ? "（无）" : sb.ToString())}");
    }

    // ==================================================================
    //  夹具（与 `RuleEngineTest` 的同名夹具**同形** —— 它是 `partial` 的私有件，跨类拿不到；
    //  这里只摆「攻击者 + 目标」这一种最小局面，⛔ 不复刻任何规则判断）
    // ==================================================================
    static CardDef Unit(string name, int cost, int atk, int hp, params string[] kws)
    {
        return new CardDef(name, name, "unit", "", null, "Test", cost, atk, hp, 0, kws);
    }

    static CardDef RangedUnit(string name, int cost, int atk, int hp, int ranged, params string[] kws)
    {
        return new CardDef(name, name, "unit", "", null, "Test", cost, atk, hp, ranged, kws);
    }

    /// <summary>督军（`subtype` 必须给 `"Warlord"` —— 与 `RuleEngineTest.HeroOf` 同一口径）。</summary>
    static CardDef Hero()
    {
        return new CardDef("G3FixtureWarlord", "G3FixtureWarlord", "hero", "", null, "Test", 0, 2, 30, 0, null,
                           subtype: "Warlord");
    }

    /// <summary>督军 + 20 张垫牌（不使用手牌 ⇒ 开局抽到什么都不影响攻击那两档）。</summary>
    static List<CardDef> Deck()
    {
        var d = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d.Add(Unit("g3filler" + i, 1, 0, 1));
        return d;
    }

    static BattleContext Battle()
    {
        var ctx = RuleCore.NewBattle(Deck(), Deck(), seed: 0, shuffle: false);
        ToP0Turn1(ctx);
        return ctx;
    }

    /// <summary>推进到「P0 的第 1 个回合」（与 `RuleEngineTest.ToP1Turn(ctx,1)` 同构）。</summary>
    static void ToP0Turn1(BattleContext ctx)
    {
        int guard = 0;
        while (ctx.Turn != 1 || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            else { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }
            if (++guard > 40) { Tr(false, "回合推进超限，疑似死循环"); return; }
        }
    }

    /// <summary>把一个单位直接摆到场上（跳过部署流程，只为构造攻击场景）—— 与 `RuleEngineTest.Place` 逐句同形。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card, bool exhausted)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = exhausted };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }
}
