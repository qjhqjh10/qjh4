// B145_E2_Assert.cs — 执行代理 `B145-E2`（第一百三十一会话 · 完全复刻 · 离线批）的断言集
//
// ============================ 它盯的是什么 ============================
//   `资料/完全复刻/判据/B145_施工单汇总_离线.md` §二 的 `RuleCore.cs` 两条：
//     · **A-01**：`targetSelectMethod = 5 (lastAdded)` 少了一步**逆序**。
//       原版 `AbilityLogic__GetTargets.c:1274-1313`：`iVar15 = pool.Count - 1` 起**递减**
//       取到 `max(0, pool.Count - max)`，逐个 `result.Add` ⇒ 结果 = **表尾 N 张、逆序**。
//       改前我们只 `pool.RemoveRange(0, pool.Count - max)`（= 表尾 N 张、**原序**）。
//     · **A-03**：`pindown` 闸放得太靠上 ⇒ **绿光也被它影响**。
//       原版 `/` 三层分工（逐句现读）：
//         ① `CardScript__CanAttackNow.c` —— **全文 0 处 `0x3ca`**（它只查攻击值 / `cantattack(0x96)` /
//            `noncombatant(0x370)` / cardState / canAct / canAttack）⇒ **绿光判据不看 pindown**；
//         ② `BattleManager__IsValidAttackTarget.c:219-222` —— `HasCurrentTrait(攻方,0x3ca) ∧ param_5 == 1`
//            （近战）⇒ `return 0` ⇒ **「近战打某目标」这一层管**；
//         ③ `BattleManager__GetAvailableAttackActions.c:138` —— 近战那一支的合取项 ⇒ **「排近战动作」这一层管**。
//       改前 `RuleCore.CanAttackNow` 调 `AttackActionPowerGate`（含 pindown）⇒ 一个「近战 3 / 远程 0 + pindown」
//       的单位 `CanActNow` 会变假（绿光灭），而原版这里是亮的。
//
// ============================ 每条断言的「灭自证」============================
//   A-01：
//     · §A-01-a 前提：`ta=10` + `EnemyUnits=1` + `MaxTargets=3` ⇒ 表**非空且正好 3 条**
//       （没有这一条，下面的「顺序」可能只是空表/全表）。
//     · §A-01-b `method=5` ⇒ `[E5, E3, E2]`（槽 0/1/2/3/5 里**表尾 3 张逆序**）；
//       🧨 把 `pool.Reverse()` 删掉 ⇒ 实得 `[E2, E3, E5]` ⇒ 红。
//     · §A-01-c **灭自证（同一次调用里换 method）**：`method=7 (firstAdded)` ⇒ `[E0, E1, E2]`
//       （表头 3 张、**不逆序**）—— 证明 b 那一行的逆序是 **method=5 特有**，不是整表被谁翻了。
//       b / c 在同一个池、同一批候选上**结构上不可能同时满足**（一个取尾、一个取头，且只有尾那支逆序）。
//     · §A-01-d 不截断档（`MaxTargets=9 > pool.Count`）⇒ 原序 `[E0,E1,E2,E3,E5]` ——
//       证明 `Reverse()` 只在**截断发生**时才跑（⛔ 别把逆序做成无条件）。
//   A-03：
//     · §A-03-0 前提：夹具攻击者**真的带 `pindown`**（关键词表命中，否则下面全是空转）；
//       另一张同样的卡**不带** pindown 作控制组。
//     · §A-03-a **绿光/选择路径不管**：`CanAttackNow(近战) == OK` 且 `CanActNow == true`；
//       🧨 若把 pindown 并回 `CanAttackNow` ⇒ 实得 `ErrPindown` / `false` ⇒ 红。
//     · §A-03-b **近战打某目标这一层管**：`IsValidTarget(近战) == ErrPindown`、远程 `== OK`；
//       🧨 若把 pindown 又从 `IsValidTarget` 拿掉（本次改前）⇒ 实得 `OK` ⇒ 红。
//       a / b 同一次调用结构上不可能同时满足（改前 b 红、改后 a 红 —— 只有「a 绿 b 红」这一态对）。
//     · §A-03-c **排近战动作这一层仍然管**：`SimpleAI.EnumerateActions` 里**没有**近战那条、**有**远程那条
//       （走 `AttackTargetGate` → `AttackActionPowerGate`，本笔一个字没动它）。
//     · §A-03-d 端到端回归：`DeclareAttack(近战) == ErrPindown`、`DeclareAttack(远程) == OK`
//       —— 与 `RuleEngineTest` ⑥ 那条断言同码（那条盯的是**出口返回码**，本笔不改变它）。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B145_E2_Assert.Run -logFile "d:/4/_tmp_view/b145e2.log"
//   判绿 = 合计那行 `失败 0`。
//   想并进别的宿主就两行：`B145_E2_Assert.Run(); pass += B145_E2_Assert.Pass; fail += B145_E2_Assert.Fail;`
//   ⚠️ 本文件**不建场景 / 不建资产 / 不进任何 `Editor/*Scene.cs`**；只造内存里的 `BattleContext`。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>`B145-E2`：`RuleCore` 的 A-01（lastAdded 逆序）＋ A-03（pindown 三层落点）。计数口径照 `CheckSink`。</summary>
public static class B145_E2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[E2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>本趟通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>本趟失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }
    /// <summary>本趟实际执行的断言条数（= Pass + Fail；证明断言真被跑到）。</summary>
    public static int RunCount { get { return _sink.Pass + _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    const int MyAtkSlot = 5;   // 我方攻击单位（⛔ 别用 4 = 督军格）
    const int FoeTgtSlot = 3;  // 敌方目标

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0;
        try
        {
            MenuCheck.Section(_sink, "B145-E2 · RuleCore A-01/A-03"
                                   + "（lastAdded 表尾 N 张逆序 · pindown 三层落点）");
            SectionA01_LastAdded逆序();
            SectionA03_Pindown分层();
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
    //  A-01 —— `targetSelectMethod = 5 (lastAdded)`：表尾 N 张、**逆序**
    //  判据 = `AbilityLogic__GetTargets.c:1274-1313`
    // ==================================================================
    static void SectionA01_LastAdded逆序()
    {
        MenuCheck.Section(_sink, "A-01 `lastAdded`（`.c:1274-1313`）：递减取 ⇒ 表尾 N 张**逆序**；"
                               + "对照 `firstAdded`（表头 N 张不逆序）与「不截断」两档");

        var ctx = Battle();
        var src = Place(ctx, 0, 3, Unit("E2A01Src", 1, 0, 9), exhausted: false);
        // 敌方 5 张、槽号升序（`AddSide` 按 0..8 遍历）⇒ 池 = [E0,E1,E2,E3,E5]
        Place(ctx, 1, 0, Unit("E2A01E0", 1, 1, 9), exhausted: true);
        Place(ctx, 1, 1, Unit("E2A01E1", 1, 1, 9), exhausted: true);
        Place(ctx, 1, 2, Unit("E2A01E2", 1, 1, 9), exhausted: true);
        Place(ctx, 1, 3, Unit("E2A01E3", 1, 1, 9), exhausted: true);
        Place(ctx, 1, 5, Unit("E2A01E5", 1, 1, 9), exhausted: true);

        // ---- method 5：表尾 3 张、逆序 ----
        var c5 = Criteria(tA: 10, max: 3, method: 5);
        var l5 = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, c5, src, null);
        Tr(l5 != null, "★ §A-01-a 前提：`ta=10` + `EnemyUnits=1` + `maxTargets=3` ⇒ 这条口能建出表（非 `null`）"
                     + "｜🧨 把 `FriendlyUnits/EnemyUnits` 写丢 ⇒ 建池支 `return null` ⇒ 本条红（下面的顺序就全是空转）");
        C(l5 == null ? -1 : l5.Count, 3,
          "★ §A-01-a 前提：截断后**正好 3 条**（5 张里取 3）");
        C(Names(l5), "E2A01E5,E2A01E3,E2A01E2",
          "★ §A-01-b `method=5` ⇒ 表尾 3 张**逆序** `[E5,E3,E2]`（原版 `.c:1274-1313` 从 `Count-1` 递减取）"
        + "｜🧨 把 `pool.Reverse()` 删掉 ⇒ 实得 `[E2,E3,E5]` ⇒ 本条红");

        // ---- method 7 对照：表头 3 张、不逆序 ----
        var c7 = Criteria(tA: 10, max: 3, method: 7);
        var l7 = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, c7, src, null);
        C(Names(l7), "E2A01E0,E2A01E1,E2A01E2",
          "★ §A-01-c **灭自证（同池换 method）**：`method=7 (firstAdded)` ⇒ 表头 3 张**原序** `[E0,E1,E2]`"
        + "（原版 `.c:1339-1361`）—— 证明 b 的逆序是 method=5 特有，⛔ 不是整表被谁翻了"
        + "｜🧨 把 `Reverse()` 挪到 `case 7` 上（或对整表无条件逆序）⇒ 本条红、b 仍绿 ⇒ 结构上不可能同时满足");

        // ---- 不截断档：Reverse() 只在截断发生时跑 ----
        var cWide = Criteria(tA: 10, max: 9, method: 5);
        var lWide = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, 0, cWide, src, null);
        C(Names(lWide), "E2A01E0,E2A01E1,E2A01E2,E2A01E3,E2A01E5",
          "★ §A-01-d `maxTargets=9 > pool.Count` ⇒ **不截断**、原序不变 `[E0,E1,E2,E3,E5]`"
        + "｜🧨 把 `Reverse()` 做成无条件（挪到 `if (pool.Count > max)` 外面）⇒ 本条红");

        Debug.Log(_sink.P + $"A-01 读数：m5(max3)=[{Names(l5)}] · m7(max3)=[{Names(l7)}] · m5(max9)=[{Names(lWide)}]");
    }

    // ==================================================================
    //  A-03 —— `pindown` 三层落点
    //    ① 绿光（`CanAttackNow`）**不管**；② 近战打某目标（`IsValidTarget`）**管**；
    //    ③ 排近战动作（`AttackTargetGate`）**管**。判据 = `.c:219-222` / `.c:138` / `CanAttackNow.c` 无 `0x3ca`
    // ==================================================================
    static void SectionA03_Pindown分层()
    {
        MenuCheck.Section(_sink, "A-03 `pindown` 三层：绿光不管（`CanAttackNow`）/ 近战打某目标管"
                               + "（`IsValidTarget`）/ 排近战动作管（`EnumerateActions`）");

        // ---- 控制组：不带 pindown 的同一张卡 ----
        {
            var ctx = Battle();
            Place(ctx, 0, MyAtkSlot, Ranged("E2Clean", 1, 3, 5, 2), exhausted: false);
            Place(ctx, 1, FoeTgtSlot, Unit("E2Prey0", 1, 0, 9), exhausted: true);
            C(RuleCore.CanAttackNow(ctx, 0, MyAtkSlot, false), RuleCodes.OK,
              "★ §A-03-0a 前提控制组：**不带** pindown ⇒ `CanAttackNow(近战)` = `OK`（证明下面那条 `ErrPindown` 是 pindown 造成的）");
            C(RuleCore.IsValidTarget(ctx, 0, MyAtkSlot, 1, FoeTgtSlot, false), RuleCodes.OK,
              "★ §A-03-0b 前提控制组：**不带** pindown ⇒ `IsValidTarget(近战)` = `OK`");
        }

        // ---- 带 pindown：绿光不管、近战目标管 ----
        var pinCtx = Battle();
        var pinned = Place(pinCtx, 0, MyAtkSlot, Ranged("E2Pindown", 1, 3, 5, 2, "pindown"), exhausted: false);
        Place(pinCtx, 1, FoeTgtSlot, Unit("E2Prey1", 1, 0, 9), exhausted: true);
        Tr(pinned.Has("pindown"),
           "★ §A-03-0c 前提：夹具攻击者**真的带上了 `pindown`**（关键词表命中，否则下面全是空转）");

        C(RuleCore.CanAttackNow(pinCtx, 0, MyAtkSlot, false), RuleCodes.OK,
          "★ §A-03-a **绿光路径不看 pindown**：近战 3 + pindown ⇒ `CanAttackNow(近战)` 仍 `OK`"
        + "（原版 `CardScript__CanAttackNow.c` 全文 **0 处 `0x3ca`**）"
        + "｜🧨 把 pindown 并回 `CanAttackNow`（本次改前那版 `AttackActionPowerGate`）⇒ 实得 `ErrPindown` ⇒ 红");
        Tr(RuleCore.CanActNow(pinCtx, 0, MyAtkSlot),
           "★ §A-03-a2 `CanActNow` = **绿光亮不亮** ⇒ 近战 3 + pindown 必须为 `true`"
         + "｜🧨 同上（pindown 留在 `CanAttackNow`）⇒ 近战/远程两问：近战 `ErrPindown`、远程（远程 2）OK ⇒ "
         + "这一条其实仍 true —— 所以**真正的灭自证是上一行**；本行锁住「绿光不因压制而灭」的对外语义");

        C(RuleCore.IsValidTarget(pinCtx, 0, MyAtkSlot, 1, FoeTgtSlot, false), RuleCodes.ErrPindown,
          "★ §A-03-b **「近战打某目标」这一层管**：近战 3 + pindown ⇒ `IsValidTarget(近战)` = `ErrPindown`"
        + "（原版 `BattleManager__IsValidAttackTarget.c:219-222`：`0x3ca` ∧ `param_5 == 1` ⇒ `return 0`）"
        + "｜🧨 把 pindown 从 `IsValidTarget` 拿掉（本次改前）⇒ 实得 `OK` ⇒ 红");
        C(RuleCore.IsValidTarget(pinCtx, 0, MyAtkSlot, 1, FoeTgtSlot, true), RuleCodes.OK,
          "★ §A-03-b2 **远程不受压制**：同一对、远程 2 ⇒ `IsValidTarget(远程)` = `OK`"
        + "（原版那一格 `param_5 == 1` 才拦）｜🧨 把 `!ranged` 去掉（远近都拦）⇒ 本条红");

        // ---- 排近战动作那一层：本笔没动它 ----
        var acts = SimpleAI.EnumerateActions(pinCtx);
        Tr(!HasAttack(acts, MyAtkSlot, 1, FoeTgtSlot, false),
           "★ §A-03-c **「排近战动作」这一层仍然管**：`SimpleAI.EnumerateActions` 里**没有**「近战 5→1/3」那条"
         + "（走 `AttackTargetGate` → `AttackActionPowerGate`，判据 `.c:138`，本笔一个字没动）"
         + "｜🧨 顺手把 `AttackActionPowerGate` 的 pindown 也删掉 ⇒ 本条红");
        Tr(HasAttack(acts, MyAtkSlot, 1, FoeTgtSlot, true),
           "★ §A-03-c2 远程 2 ⇒ 动作表里**有**「远程 5→1/3」那条（压制只禁近战）");

        // ---- 端到端：出口返回码不变（`RuleEngineTest` ⑥ 那条盯的就是它）----
        var endCtx = Battle();
        Place(endCtx, 0, MyAtkSlot, Ranged("E2PindownEnd", 1, 3, 9, 3, "pindown"), exhausted: false);
        Place(endCtx, 1, FoeTgtSlot, Unit("E2Prey2", 1, 0, 20), exhausted: true);
        C(RuleCore.DeclareAttack(endCtx, 0, MyAtkSlot, 1, FoeTgtSlot, ranged: false), RuleCodes.ErrPindown,
          "★ §A-03-d 端到端 `DeclareAttack(近战)` = `ErrPindown`（出口码与 `RuleEngineTest` ⑥ 一致："
        + "`CanAttackNow` 放行后由 `IsValidTarget` 拦下，净结果不变）");
        C(RuleCore.DeclareAttack(endCtx, 0, MyAtkSlot, 1, FoeTgtSlot, ranged: true), RuleCodes.OK,
          "★ §A-03-d2 端到端 `DeclareAttack(远程)` = `OK`（压制只禁近战）");
    }

    // ==================================================================
    //  夹具（与 `B98G3_Assert` / `RuleEngineTest` 的同名夹具**同形**：只摆局面，⛔ 不复刻任何规则判断）
    // ==================================================================
    static CardDef Unit(string name, int cost, int atk, int hp, params string[] kws)
    {
        return new CardDef(name, name, "unit", "", null, "Test", cost, atk, hp, 0, kws);
    }

    static CardDef Ranged(string name, int cost, int atk, int hp, int ranged, params string[] kws)
    {
        return new CardDef(name, name, "unit", "", null, "Test", cost, atk, hp, ranged, kws);
    }

    static CardDef Hero()
    {
        return new CardDef("E2FixtureWarlord", "E2FixtureWarlord", "hero", "", null, "Test", 0, 2, 30, 0, null,
                           subtype: "Warlord");
    }

    /// <summary>督军 + 20 张垫牌（不使用手牌 ⇒ 开局抽到什么都不影响本节两条活）。</summary>
    static List<CardDef> Deck()
    {
        var d = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d.Add(Unit("e2filler" + i, 1, 0, 1));
        return d;
    }

    static BattleContext Battle()
    {
        var ctx = RuleCore.NewBattle(Deck(), Deck(), seed: 0, shuffle: false);
        ToP0Turn1(ctx);
        return ctx;
    }

    /// <summary>推进到「P0 的第 1 个回合」。</summary>
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

    /// <summary>把一个单位直接摆到场上（跳过部署流程，只为构造场景）。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card, bool exhausted)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = exhausted };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>A-01 用：`ta=10` 的最小 criteria。其余格留 `Unknown`(-1) ⇒ `Matches` 在 ③ 返回 `null` ⇒ 全保留。</summary>
    static CardDef.CountSetSpec Criteria(int tA, int max, int method)
    {
        return new CardDef.CountSetSpec
        {
            TargetsAffected = tA,
            FriendlyUnits = 0,
            EnemyUnits = 1,
            MaxTargets = max,
            TargetSelectMethod = method,
        };
    }

    /// <summary>把目标表拼成 `"卡名,卡名,…"`（`null` ⇒ `"<null>"`，⛔ 不抛异常、也不静默跳过）。</summary>
    static string Names(List<BattleEventTarget> list)
    {
        if (list == null) return "<null>";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(list[i].CardId);
        }
        return sb.ToString();
    }

    /// <summary>动作表里有没有「`atkSlot` 用 <paramref name="ranged"/> 那档打敌方 `tgtSlot`」这一条。</summary>
    static bool HasAttack(List<AiAction> list, int atkSlot, int tgtP, int tgtSlot, bool ranged)
    {
        if (list == null) return false;
        var kind = ranged ? AiActionKind.AttackRanged : AiActionKind.AttackMelee;
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null || a.Kind != kind) continue;
            if (a.Slot == atkSlot && a.TargetP == tgtP && a.TargetSlot == tgtSlot && a.Ranged == ranged) return true;
        }
        return false;
    }
}
