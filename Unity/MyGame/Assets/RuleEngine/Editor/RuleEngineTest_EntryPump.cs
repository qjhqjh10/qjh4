// RuleEngineTest_EntryPump.cs — 引擎线 `W62·自检` 残余：**18 个顶层入口的「返回即空队列」结构自检**
//                                     ＋ 战斗线 `W64` `B-30`：**效果 470（`showCardsWindow`）发事件**
// （2026-10-24 · 执行代理 `J2`）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_Order` / `_BoardModel` / `_S14` / `_S23_*` / `_Priority` / `_Diag` 都在），而**同一时刻只能有一个写手改一个文件**
// ⇒ 本批测试体全写在这里，`RuleEngineTest.cs` **只加 `Run()` 里两行 `Step(...)`**（＋ 一条把 `EvtKind` 数从 17 改成 18）。
//
// 判据（⛔ 不要重新论证）：
//   · `资料/普查产出_1022/W62_引擎32红诊断.md` 的 **R1**（四个入口没有 `ActionScope` ⇒ 入队的触发永不结算）；
//   · `Core/ActionPump.cs` 头注释的三条设计口径（动作内部不递归泵队 / 闸在泵点 / 顶层动作外壳 = `ActionScope`）；
//   · 战斗线 `W64` `B-30` → `资料/普查产出_1023e/G1_战斗Tips与兜底.md` §2·7（原版唯一入口 = 效果 470）；
//     原版方法体 = `d:/2/tools/decomp_full/` 的 `AbilityLogic__PlayAbility.c` case `0x1d6` ·
//     `BattleManager__AddShowCardsWindowAction.c` · `BattleManager__ResolveAction.c` case `0x56` ·
//     `BattleHud__DisplayMultiCardView.c`。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律**写死字面量**（`18` / `1` / `0` / 键名字符串）—— ⛔ 不从被测实现反算（那会自证）。
using System;
using System.Collections.Generic;
using System.Reflection;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==================================================================
    //  一 · `W62·自检`：18 个顶层入口的「返回即空队列」结构自检
    // ==================================================================

    /// <summary>本批覆盖的入口条数 —— 写死，和 `Run()` 里那 18 行 `W62EntryCase` 对账（删一条就红）。</summary>
    const int W62EntryPumpCaseCount = 18;

    /// <summary>
    /// **`W62·自检` 的结构自检主场**（`Run()` 里一行 `Step(TestW62EntryPump)`）。
    ///
    /// <para>**判据**（`普查产出_1022/W62_引擎32红诊断.md` 的 R1 + `Core/ActionPump.cs` 的口径 ③）：
    /// 每一个**顶层入口**都必须自带一层 `ActionScope` —— 进去时 `Resolving = true`（它做的这一条动作
    /// 正在结算，里面产生的触发**只入队**），退出时清 flag 并**把队列取空**。少了这一层，
    /// 那一批入队的触发会**永远停在队列里不结算，而且没有告警**
    /// （`ActionScope.Dispose` 那句「泵点漏了」只在**外层**作用域退出时才发 —— 没有外层就永远沉默）。
    /// 这正是 R1 那 13 条红的成因。</para>
    ///
    /// <para>🔴 **为什么每条都要【先塞一条哨兵】**：不塞的话，入口什么都不入队时
    /// `Count == 0` 是**恒真**的（假绿）。塞一条 `Player` 道的哨兵之后：
    ///   · 入口有 `ActionScope` ⇒ 构造时先泵一次（`Resolving` 还是 false）⇒ 哨兵跑掉 ⇒ `ran == 1`、`Count == 0`；
    ///   · 入口**漏了** `ActionScope` ⇒ 哨兵留在队列里 ⇒ `ran == 0`、`Count == 1` ⇒ **两行同时红**。
    /// ⇒ 「入口返回时 `ActionQueue.Count == 0`」这句话**只有配上哨兵才有判别力**（这是本组的主设计）。</para>
    ///
    /// <para>⚠️ **夹具一律「最小局面」**（空棋盘 / 空手牌为主）—— 本组测的是**外壳**，不是那 18 个入口的业务语义
    /// （业务语义各有各的用例）。夹具让入口走到「非法参数 / 无目标」的早退分支**完全够用**：
    /// `using (BeginAction(ctx))` 是每个入口的**第一句**，早退也在作用域**里面**。</para>
    /// </summary>
    public static void TestW62EntryPump()
    {
        Section("🆕 `W62·自检` · 顶层入口结构自检（18 个入口，返回时 `ActionQueue` 必须为空）");

        _w62EntryCases = 0;

        // ---- RuleCore 那 15 个（`using (BeginAction(ctx))` 的落点，判据 = `W61T26` 的计数口径）----
        //  ① `BeginTurn` / `EndTurn` —— 回合起止（`ResolveAtTurn` 那一族触发的宿主）
        W62EntryCase("`RuleCore.BeginTurn`", W62MinBattle, c => RuleCore.BeginTurn(c));
        W62EntryCase("`RuleCore.EndTurn`", W62MinBattle, c => RuleCore.EndTurn(c));

        //  ② `Draw` —— 抽牌（`BroadcastWhen(Draw)` 的宿主）
        W62EntryCase("`RuleCore.Draw`", W62MinBattle, c => RuleCore.Draw(c, c.Active));

        //  ③ `PlayCard` —— 手牌打出单位（`BroadcastWhen(Play)` / `Deploy` 的宿主）
        W62EntryCase("`RuleCore.PlayCard`",
            () => { var c = ProbeBattle(new[] { Unit("W62E_Peon", 1, 1, 2) },
                                        new[] { Unit("W62E_Foe", 1, 1, 2) });
                    c.Players[0].Energy = 99; c.Players[1].Energy = 99; return c; },
            c => { int landed; RuleCore.PlayCard(c, c.Active, 0, 2, out landed); });

        //  ④⑤ `DeployFree` 的两个重载（传实例 / 凭空造一张）
        W62EntryCase("`RuleCore.DeployFree(… CardInstance …)`", W62MinBattle,
            c => { int slot; RuleCore.DeployFree(c, c.Active,
                        c.NewInstance(Unit("W62E_DepA", 1, 1, 1)), out slot); });
        W62EntryCase("`RuleCore.DeployFree(… CardDef …)`", W62MinBattle,
            c => { int slot; RuleCore.DeployFree(c, c.Active, Unit("W62E_DepB", 1, 1, 1), out slot); });

        //  ⑥ `DeclareAttack` —— 攻击宣言（`BroadcastWhen(Attack)` / `abouttoattack` 的宿主）
        W62EntryCase("`RuleCore.DeclareAttack`", W62MinBattle, c =>
        {
            int p = c.Active, q = 1 - p;
            Place(c, p, 3, Unit("W62E_Atk", 1, 3, 9));
            Place(c, q, 3, Unit("W62E_Def", 1, 0, 9));
            RuleCore.DeclareAttack(c, p, 3, q, 3);
        });

        //  ⑦ `TryCancelAttackByOwnAboutToAttack`（**私有** ⇒ 反射；先例 = 本文件族外 `A1983` 那一组）
        var miCancel = typeof(RuleCore).GetMethod("TryCancelAttackByOwnAboutToAttack",
                BindingFlags.NonPublic | BindingFlags.Static);
        CheckTrue(miCancel != null,
                  "前提：`RuleCore.TryCancelAttackByOwnAboutToAttack` 还在（改名 ⇒ 下面那条与 `A1983` 那组一起红，⛔ 别静默跳过）");
        if (miCancel != null)
            W62EntryCase("`RuleCore.TryCancelAttackByOwnAboutToAttack`（私有）", W62MinBattle, c =>
            {
                int p = c.Active, q = 1 - p;
                var atk = Place(c, p, 3, Unit("W62E_CAtk", 1, 3, 9));
                var def = Place(c, q, 3, Unit("W62E_CDef", 1, 0, 9));
                miCancel.Invoke(null, new object[] { c, p, 3, atk, def });
            });

        //  ⑧ `ApplyDamage` —— 只扣血的那个（**不含**死亡链；`Hurt` 才是入口）
        W62EntryCase("`RuleCore.ApplyDamage`", W62MinBattle, c =>
        {
            var u = Place(c, c.Active, 3, Unit("W62E_Dmg", 1, 1, 9));
            RuleCore.ApplyDamage(c, u, 1, "W62E");
        });

        //  ⑨ `Hurt`（**私有** ⇒ 反射）—— R1 点名的那个（死亡 / 广播全在它末尾）
        var miHurt = typeof(RuleCore).GetMethod("Hurt", BindingFlags.NonPublic | BindingFlags.Static);
        CheckTrue(miHurt != null, "前提：`RuleCore.Hurt` 还在（改名 ⇒ 下面那条红，⛔ 别静默跳过）");
        if (miHurt != null)
            W62EntryCase("`RuleCore.Hurt`（私有）", W62MinBattle, c =>
            {
                var u = Place(c, c.Active, 3, Unit("W62E_Hurt", 1, 1, 9));
                miHurt.Invoke(null, new object[] { c, u, 1, "W62E", -1, false, null });
            });

        //  ⑩⑪⑫ 三个「花掉本回合行动」的口（主动技能 / 誓约 / 信仰）
        W62EntryCase("`RuleCore.UseAbility`", W62MinBattle, c =>
        {
            Place(c, c.Active, 3, Unit("W62E_Ab", 1, 1, 9));
            RuleCore.UseAbility(c, c.Active, 3);
        });
        W62EntryCase("`RuleCore.UseOathAbility`", W62MinBattle, c =>
        {
            Place(c, c.Active, 3, Unit("W62E_Oath", 1, 1, 9));
            RuleCore.UseOathAbility(c, c.Active, 3);
        });
        W62EntryCase("`RuleCore.UseFaithAbility`", W62MinBattle, c =>
        {
            Place(c, c.Active, 3, Unit("W62E_Faith", 1, 1, 9));
            RuleCore.UseFaithAbility(c, c.Active, 3);
        });

        //  ⑬ `UseAlternative`（四个关键词那一条）
        W62EntryCase("`RuleCore.UseAlternative`", W62MinBattle, c =>
        {
            Place(c, c.Active, 3, Unit("W62E_Alt", 1, 1, 9));
            RuleCore.UseAlternative(c, c.Active, 3, KeywordTable.Ferocity);
        });

        //  ⑭ `CollectWaystone`（灵魂石）
        W62EntryCase("`RuleCore.CollectWaystone`", W62MinBattle, c =>
        {
            Place(c, c.Active, 3, Unit("W62E_Stone", 1, 1, 9));
            RuleCore.CollectWaystone(c, c.Active, 3);
        });

        // ---- `EffectResolver.cs`（仍然是 `partial class RuleCore`）那 3 个 ----
        //  ⑮ `PlayTactic` —— 战术卡（正文整段入队，是「只入队」最明显的那一个）
        W62EntryCase("`RuleCore.PlayTactic`",
            () => { var c = ProbeBattle(new[] { Tactic("W62E_Tac", 1, "Draw a card") },
                                        new[] { Tactic("W62E_TacFoe", 1, "Draw a card") });
                    c.Players[0].Energy = 99; c.Players[1].Energy = 99; return c; },
            c => RuleCore.PlayTactic(c, c.Active, 0));

        //  ⑯ `BroadcastWhen`（**公开**入口）—— R1 点名的那个
        W62EntryCase("`RuleCore.BroadcastWhen`", W62MinBattle,
            c => RuleCore.BroadcastWhen(c, WhenEventKind.Deploy, c.Active, Unit("W62E_BC", 1, 1, 1)));

        //  ⑰ `GrantDarkPact` —— R1 点名的那个（②③ 入队 + `ApplyOneGain` 还可能再入队）
        W62EntryCase("`RuleCore.GrantDarkPact`", W62MinBattle, c =>
        {
            var u = Place(c, c.Active, 3, Unit("W62E_Pact", 1, 1, 9));
            RuleCore.GrantDarkPact(c, c.Active, u, "excess", "W62E");
        });

        // ---- 覆盖度：本批真的跑了 18 条（删掉任何一条 `W62EntryCase` ⇒ 这里红）----
        Check(_w62EntryCases, W62EntryPumpCaseCount,
              $"★ `W62·自检` 覆盖了 **{W62EntryPumpCaseCount} 个顶层入口**（实得 {_w62EntryCases} 条用例）"
            + "｜🧨 删掉/注释掉任何一条 `W62EntryCase(...)` ⇒ 本行红"
            + "（⛔ 别把常数改小 —— 常数就是「泵点集合」的口径，与 `W61T26` 的 18 同源）");
    }

    /// <summary>本组跑过的用例数（只给上面那条覆盖度断言用；⛔ 不是第二个用例表）。</summary>
    static int _w62EntryCases;

    /// <summary>最小局面：双方只有督军、空手牌（`ProbeBattle` 不塞 20 张垫牌）。</summary>
    static BattleContext W62MinBattle() { return ProbeBattle(new CardDef[0], new CardDef[0]); }

    /// <summary>
    /// **一条「入口返回即空队列」的用例**（18 条共用这一段 —— ⛔ 别每条各写一份判据，
    /// 那样迟早有一条漏写「哨兵」而变成假绿）。
    ///
    /// 三步：① 塞一条 `Player` 道哨兵（`ran++`）→ ② **裸调**入口（⛔ 外面不许再包 `BeginAction`，
    /// 否则入口那层变内层、永远不泵队，测的就不是它）→ ③ 三连断言。
    ///
    /// 🧨 **灭自证**：把**任意一个**入口里的 `using (BeginAction(ctx))` 删掉 ⇒ 那一条的
    /// 「哨兵被泵掉」与「`Count == 0`」**两行同时红**（实得 `ran == 0` / `Count == 1`）。
    /// </summary>
    static void W62EntryCase(string label, Func<BattleContext> make, Action<BattleContext> call)
    {
        _w62EntryCases++;
        try
        {
            var ctx = make();
            int before = ctx.ActionQueue.Count;      // 正常应为 0（`NewBattle` 收尾就泵空了）；非 0 也不慌 —— 下面按增量判
            int ran = 0;
            bool queued = RuleCore.EnqueueAbility(ctx, ActionPriority.Player, "W62·哨兵", () => ran++);
            CheckTrue(queued, $"{label} 前提：哨兵已经排进 `Player` 道（`Player` 道最后消费 ⇒ 不会被谁抢走）");
            Check(ctx.ActionQueue.Count, before + 1,
                  $"{label} 前提：哨兵正躺在队列里（进队前 {before} 条 ⇒ 现在应 {before + 1} 条）");

            call(ctx);

            Check(ran, 1, $"★ {label} —— 返回时那条**哨兵已经被泵掉**（入口自己开了一层 `ActionScope`）"
                        + "｜🧨 把这个入口里的 `using (BeginAction(ctx))` 删掉或注释掉 ⇒ 哨兵永远不跑 ⇒ 本行实得 0");
            Check(ctx.ActionQueue.Count, 0, $"★ {label} —— 返回时 `ActionQueue.Count == 0`"
                        + "｜🧨 同上：漏一层外壳 ⇒ 哨兵留在队列里 ⇒ 本行实得 1");
            Check(ctx.ActionQueue.Resolving, false, $"{label} —— 返回时 `Resolving` 已清（闸没泄漏）"
                        + "｜🧨 谁在 `ActionScope` 之外把 `Resolving` 置真且不清 ⇒ 泵点从此一条都不取 ⇒ 本行红");
        }
        catch (Exception ex)
        {
            // ⚠️ 单条抛异常不许掐死后面 17 条（`Step()` 只兜到本方法这一层）。
            CheckTrue(false, $"{label} —— 夹具/入口**抛异常**（本条之后照跑）：{ex.GetType().Name} — {ex.Message}");
        }
    }

    // ==================================================================
    //  二 · `W64` `B-30`：效果 470（`showCardsWindow`）→ `EvtKind.ShowCardsWindow`
    // ==================================================================

    /// <summary>
    /// **效果 470（`showCardsWindow`）的引擎侧**（原版唯一入口 = `AbilityEffect.showCardsWindow(470)`
    /// → 动作 86 → `ResolveAction` case `0x56` → `BattleHud.DisplayMultiCardView`；
    /// 现池只有 `GSC55 Eve of the Uprising` 一张用它，判据 → `普查产出_1023e/G1_战斗Tips与兜底.md` §2·7）。
    ///
    /// <para>**四条**：① 数据表（全池恰好 1 张 + 标题键）· ② 负对照（别的战术卡不发）·
    /// ③ 真打一遍 `GSC55`（事件带对卡 / 带对键 / `Slot = -1`）· ④ 那批卡**从墓地**回牌库
    /// （`Discard` 与 `DeadUnits` 一起撤）· ⑤ 负对照：**棋盘上有活人、墓地空** ⇒ 一张都不许收走。
    /// 🔴 **2026-10-24（引擎线 `N1`）就地订正（铁律 5）**：本行原来写的是「④ **零行为变化**
    /// （那 3 张卡照旧回牌库）」—— 那是**按当时那个缺陷行为**描述的：`DoReturn` 走棋盘扫描。
    /// `N1` 已按原版改成**从墓地取**（判据 = `targetsAffected = 60 = inCemeteryFiltered`），
    /// 夹具随之改成「先弄死那 3 张」，并加了 ⑤ 那条主判别式。
    /// ⚠️ 「只在 `IsPlayerTurn` 时显示」这一道闸**不在引擎里**（无头引擎没有「本机座位」这个概念，
    /// 见 `EvtKind.ShowCardsWindow` 的 doc）⇒ 本节**不断言**它。</para>
    /// </summary>
    public static void TestW64ShowCardsWindow()
    {
        Section("🆕 `W64` `B-30`：效果 470（`showCardsWindow`）→ `EvtKind.ShowCardsWindow`");

        // ---- ① 数据表：全池唯一持有者 + 标题键。判据 = `cards_raw.json` 的 `chosenAbility == 470` ----
        var eve = W61PoolCard("Eve of the Uprising");
        CheckTrue(eve != null, "前提：卡池里拿得到 `Eve of the Uprising`（`GSC55`）");
        if (eve == null) return;
        Check(eve.Id, "GSC55", "前提：`Eve of the Uprising` 的 id 就是 `GSC55`");

        Check(RuleCore.ShowCardsWindowOf(eve), "EffectDisplayCardsAddToDeck",
              "★ `GSC55` 的 470 标题键 = `EffectDisplayCardsAddToDeck`（原版那一格 `buffEffect.effectText`；"
            + "官方本地化 `Card_Abilities/EffectDisplayCardsAddToDeck` = `These cards have returned to your deck`）"
            + "｜🧨 表里写成空串 / 换成 `Card_Abilities/<卡名>` ⇒ 本行红");

        var nob = W61PoolCard("Ork Nob");
        CheckTrue(nob != null, "前提：卡池里拿得到 `Ork Nob`（负对照用）");
        if (nob != null)
            Check(RuleCore.ShowCardsWindowOf(nob), null,
                  "★ 负对照：`Ork Nob` **没有** 470 ⇒ 返回 `null`"
                + "｜🧨 把 `ShowCardsWindowOf` 写成「凡战术/单位都返回一个键」⇒ 本行红");

        int holders = 0;
        var holderIds = new List<string>();
        foreach (var c in CardDatabase.Load())
            if (c != null && RuleCore.ShowCardsWindowOf(c) != null) { holders++; holderIds.Add(c.Id); }
        Check(holders, 1,
              $"★ **全池恰好 1 张**卡有 470（判据 = `cards_raw.json` 1,351 张里只有 `GSC55`；实得 {holders} 张："
            + $"{string.Join("、", holderIds)}）"
            + "｜🧨 把表扩成「凡是 `return … to your deck` 都开窗」⇒ 本行红（那是**猜**，原版是 SO 里的一条独立 abilityLogic）");

        // ---- ② 负对照：打一张**别的**战术卡 ⇒ 一条 `ShowCardsWindow` 都不发 ----
        {
            var ctx = ProbeBattle(new[] { Tactic("W62E_Plain", 1, "Draw a card") },
                                  new[] { Tactic("W62E_PlainFoe", 1, "Draw a card") });
            ctx.Players[0].Energy = 99; ctx.Players[1].Energy = 99;
            int p = ctx.Active;
            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, p, 0), RuleCodes.OK, "② 前提：那张普通战术卡打得出去");
            int n = 0;
            foreach (var e in ctx.Signals) if (e.Kind == EvtKind.ShowCardsWindow) n++;
            Check(n, 0, "★ 负对照：**没有 470 的战术卡打出后一条 `ShowCardsWindow` 都不发**"
                      + "｜🧨 把 `EmitShowCardsWindow` 的 `ShowCardsWindowOf` 闸去掉（对每张战术都发）⇒ 本行红");
        }

        // ---- ③④ 真打一遍 `GSC55`：**先在墓地摆 3 张自己的部队**，再把这张战术打出去 ----
        //
        //  🔴 **2026-10-24（引擎线 `N1`）就地订正（铁律 5）**：本段原来摆的是**活着的**部队，并在注释里
        //     写着「我们引擎今天的实现走棋盘扫描、不读 `DeadUnits`，⛔ 别改回去」—— 那句话描述的是
        //     **一个真缺陷**（卡面写 `troops that **died this game**`，而 `DoReturn` 收走的是场上活人）。
        //     本笔已按原版修正（判据 = `targetsAffected = 60 = inCemeteryFiltered`，
        //     见 `EffectResolver.ReturnFromCemetery` 的 doc），**夹具跟着改成「先弄死再回牌库」**。
        //     ⚠️ 现在这条用例同时是那次修正的判别式：谁把 `DoReturn` 换回棋盘扫描，
        //        「3 张在墓地里」这条前提与下面「棋盘只剩督军」都会红。
        {
            var ctx = ProbeBattle(new[] { eve }, new CardDef[0]);
            ToP1Turn(ctx, 2);                       // ⇒ `Active == 0`、回合 3（战术卡要打在自己回合）
            int p = ctx.Active;
            ctx.Players[p].Energy = 99;

            var retNames = new List<string> { "W62E_RetA", "W62E_RetB", "W62E_RetC" };
            var units = new List<UnitState>
            {
                Place(ctx, p, 3, Unit(retNames[0], 1, 1, 9)),
                Place(ctx, p, 5, Unit(retNames[1], 1, 1, 9)),
                Place(ctx, p, 6, Unit(retNames[2], 1, 1, 9)),
            };

            int handIdx = HandIdx(ctx, p, "Eve of the Uprising");
            CheckTrue(handIdx >= 0, "③ 前提：`Eve of the Uprising` 在施放者手上");
            if (handIdx < 0) return;
            int onBoardBefore = 0;
            for (int s = 0; s < BoardSpec.Size; s++) if (ctx.Players[p].Board[s] != null) onBoardBefore++;
            Check(onBoardBefore, 4, "③ 前提：棋盘上是「督军 + 那 3 张部队」= 4 个（⛔ 少于 4 ⇒ 下面「摊开 3 张」是假绿）");
            Check(units.Count, 3, "③ 前提：夹具摆上去的就是这 3 张");
            CheckTrue(ReferenceEquals(ctx.Players[p].Board[3], units[0])
                   && ReferenceEquals(ctx.Players[p].Board[5], units[1])
                   && ReferenceEquals(ctx.Players[p].Board[6], units[2]),
                      "③ 前提：那 3 张真的落在 3 / 5 / 6 格上（`Place` 摆的，⛔ 不是被谁顶掉了）");

            // ★ 让这 3 张**真的死掉**（走 `Hurt` 这条真入口 ⇒ `CleanupDeaths` 把它们登进 `DeadUnits`）
            foreach (var u in units) N1Kill(ctx, u);
            int deadRecs = 0;
            foreach (var d in ctx.DeadUnits) if (d != null && d.Owner == p) deadRecs++;
            Check(deadRecs, 3,
                  "③ 前提：那 3 张**进了 `DeadUnits`**（= 「本局阵亡」的那份表；⛔ 少于 3 ⇒ 下面从墓地取是假绿）");
            int onBoardAfterKill = 0;
            for (int s = 0; s < BoardSpec.Size; s++) if (ctx.Players[p].Board[s] != null) onBoardAfterKill++;
            Check(onBoardAfterKill, 1, "③ 前提：棋盘上只剩督军（3 张都已离场）");

            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, p, handIdx), RuleCodes.OK, "③ 前提：`Eve of the Uprising` 打得出去");

            BattleEvent win = null;
            int nWin = 0;
            foreach (var e in ctx.Signals) if (e.Kind == EvtKind.ShowCardsWindow) { nWin++; win = e; }
            Check(nWin, 1, "★ 打出 `GSC55` ⇒ **恰好一条** `EvtKind.ShowCardsWindow`"
                         + "｜🧨 删掉 `PlayTacticBodyDeferred` 里那句 `EmitShowCardsWindow(ctx, p, card)` ⇒ 本行实得 0"
                         + "；🧨 把它挪到 `ResolveOps` **之前**（`LastCreated` 还是上一条卡的陈旧值时更明显）⇒ 下面「摊开哪几张」那条红");
            if (win == null) return;

            Check(win.Effect, "Card_Abilities/EffectDisplayCardsAddToDeck",
                  "★ 标题键 = `Card_Abilities/EffectDisplayCardsAddToDeck`（原版 `\"Card_Abilities/\" + effectText`）"
                + "｜🧨 少拼前缀 / 拼成 `Card_Abilities/` + 卡名 ⇒ 本行红");
            Check(win.Player, p, "★ `Player` = 打这张牌的那一方（原版闸 `IsPlayerTurn` 归表现层，见 doc）");
            Check(win.Slot, -1, "★ `Slot` 恒 `-1`（这扇窗不在格位上）｜🧨 填成某个格号 ⇒ 表现层会去那儿找锚点 ⇒ 本行红");
            Check(win.CardId, "Eve of the Uprising", "★ `CardId` = **触发它的那张牌**的卡名");

            var got = win.CardNames;
            CheckTrue(got != null, "★ `CardNames` 有值（⛔ 不是 null）");
            int cnt = got != null ? got.Count : -1;
            Check(cnt, 3, "★ 摊开 **3 张**（= 刚从墓地回牌库的那批）"
                        + "｜🧨 改成「摊开全场」/「按卡池取前 N 张」⇒ 本行红");
            if (got != null)
            {
                bool allSame = true;
                foreach (var nm in retNames) if (!got.Contains(nm)) allSame = false;
                CheckTrue(allSame && got.Count == retNames.Count,
                          "★ 摊开的**就是刚从墓地回牌库的那 3 张**（按名字逐个比对，⛔ 不是「随便 3 张」）"
                        + " ｜🧨 改成从 `Hand` / 卡池里取 ⇒ 本行红");
            }

            // ---- ④ 那 3 张**从墓地**进牌库（`DeadUnits`/`Discard` 一起撤掉）----
            int stillOnBoard = 0;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (ctx.Players[p].Board[s] != null) stillOnBoard++;
            Check(stillOnBoard, 1, "★ 棋盘上仍然只剩督军（那 3 张早就离场了，本笔不碰棋盘）"
                                 + "｜🧨 在 `EmitShowCardsWindow` 里顺手改棋盘 ⇒ 本行红");
            int inDeck = 0;
            foreach (var inst in ctx.Players[p].Deck)
                if (inst != null && inst.Card != null && retNames.Contains(inst.Card.Name)) inDeck++;
            Check(inDeck, 3, "★ 3 张**照旧回到牌库**（`Deck` 里数得到 3 份）"
                           + "｜🧨 从墓地取走却没搬进牌库 ⇒ 本行红");
            int deadLeft = 0;
            foreach (var d in ctx.DeadUnits) if (d != null && d.Owner == p) deadLeft++;
            Check(deadLeft, 0, "★ 墓地里那 3 条记录**一起被撤掉**（`TakeFromGraveyard` 的口径："
                             + "`Discard` 与 `DeadUnits` 必须一起移，⛔ 只移一边 ⇒ 同一张卡能被捞两次）");
        }

        // ---- ⑤ 负对照：**棋盘上有活人、墓地里没人** ⇒ 一张都不许收走、也不发事件 ----
        //   🔴 这一条是 `GSC55` 那次修正的**主判别式**：改之前 `DoReturn` 走棋盘扫描 ⇒
        //      这三张活人会被收进牌库（下面两条断言同时红）。
        {
            var ctx = ProbeBattle(new[] { eve }, new CardDef[0]);
            ToP1Turn(ctx, 2);
            int p = ctx.Active;
            ctx.Players[p].Energy = 99;
            int handIdx = HandIdx(ctx, p, "Eve of the Uprising");
            CheckTrue(handIdx >= 0, "⑤ 前提：`Eve of the Uprising` 在施放者手上");
            if (handIdx < 0) return;

            var aliveNames = new List<string> { "W62E_LiveA", "W62E_LiveB", "W62E_LiveC" };
            var alive = new List<UnitState>
            {
                Place(ctx, p, 3, Unit(aliveNames[0], 1, 1, 9)),
                Place(ctx, p, 5, Unit(aliveNames[1], 1, 1, 9)),
                Place(ctx, p, 6, Unit(aliveNames[2], 1, 1, 9)),
            };
            foreach (var d in ctx.DeadUnits)
                CheckTrue(d == null || d.Owner != p, "⑤ 前提：这一方的墓地是**空的**");

            ctx.ClearSignals();
            CheckCode(RuleCore.PlayTactic(ctx, p, handIdx), RuleCodes.OK,
                      "⑤ 前提：墓地里没人时 `Eve of the Uprising` 也打得出去（否则下面两条是假绿）");

            int stillAlive = 0;
            foreach (var u in alive)
                if (u != null && u.IsAlive && N1OnBoard(ctx, p, u)) stillAlive++;
            Check(stillAlive, 3,
                  "★ **棋盘上的 3 张活人一张都没被收走**（卡面写的是 `troops that **died this game**`）"
                + "｜🧨 把 `DoReturn` 换回棋盘扫描（`ResolveTargets`）⇒ 本行实得 0；"
                + "🧨 把墓地分支的判断条件写反（有活人时反而收）⇒ 本行红");
            int inDeck2 = 0;
            foreach (var inst in ctx.Players[p].Deck)
                if (inst != null && inst.Card != null && aliveNames.Contains(inst.Card.Name)) inDeck2++;
            Check(inDeck2, 0, "★ 那 3 张**没有**进牌库（从墓地取才是对的）｜🧨 同上 ⇒ 本行红");
            int n = 0;
            foreach (var e in ctx.Signals) if (e.Kind == EvtKind.ShowCardsWindow) n++;
            Check(n, 0, "★ 没有可摊开的卡 ⇒ **不发** `ShowCardsWindow`（原版会开一扇空窗；本偏离已写进 "
                      + "`EmitShowCardsWindow` 的 doc，而且走 `ctx.Log` 出声 —— ⛔ 不是静默）"
                      + "｜🧨 改成「0 张也发」⇒ 本行红（那时要同步改 doc 与这一条，⛔ 别只改一边）");
        }

        // ---- ⑥⑦ 混场：3 张在墓地 + 2 张活着 ⇒ 只收墓地那 3 张（N1 专项，见 `RuleEngineTest_N1.cs`）----
        N1Gsc55CemeteryReturn(eve);
    }
}
