// RuleEngineTest_N1.cs — 引擎线 `N1`（2026-10-24）三件的自检体：
//   ① `GSC55 Eve of the Uprising`「本局阵亡」修正（**墓地**取件，不是棋盘扫描）的补充场景；
//   ② `T03·d` 的 `P2-8`：`berzerk` 候选的**口径**（原版是**黑名单**，不是 `IsUnit` 白名单）；
//   ③ `AttackGiantKiller`（trait 340）的引擎建模。
// ②③ 的池内命中都是 **0 张**（铁律 11 例外①：零行为变化，补的是「将来写得出」那一层）。
//
// ⚠️ 本文件里的方法**刻意不叫 `Test*` / `Report*`**：`工具/gen_diag_runner.py` 的用例清单是
//    「`static` ＋ 无参 ＋ 返回 `void` ＋ 名字 `Test`/`Report` 打头」⇒ 叫别的名字就**不进用例表**，
//    也就不必重生成 `RuleEngineTest_Diag.cs`（那个工具不在本笔白名单里）。
//    它们由既有用例**显式调用**（`TestW64ShowCardsWindow` / `TestBeginTurn` / `TestFlyingMeleeBlocked`），
//    ⇒ **真的会被跑到**，不是死代码。
//
// 🔴 每条断言都带 `🧨`（什么改动能让它红）。
using System;
using System.Collections.Generic;
using System.Reflection;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>把一个单位**真的弄死** —— 走 `RuleCore.Hurt` 这条真入口（死亡 → 进弃牌堆 + 登记
    /// `DeadUnits` 全在它末尾的 `CleanupDeaths` 里）。
    ///
    /// ⚠️ `Hurt` 是 `private` ⇒ 反射（先例 = `RuleEngineTest_EntryPump.cs` 的 `W62` 那一组）。
    /// ⛔ 别改成 `ApplyDamage` —— 那个**不含死亡链**（它自己 doc 写着），夹具会变成「血 ≤ 0 但还在场上」。
    /// </summary>
    static void N1Kill(BattleContext ctx, UnitState u)
    {
        var mi = typeof(RuleCore).GetMethod("Hurt", BindingFlags.NonPublic | BindingFlags.Static);
        CheckTrue(mi != null,
                  "前提：`RuleCore.Hurt` 还在（改名 ⇒ 本组夹具造不出阵亡，⛔ 别静默跳过）");
        if (mi == null || u == null) return;
        mi.Invoke(null, new object[] { ctx, u, 99, "N1Kill", -1, false, null, UnitDeathType.Unknown });
    }

    /// <summary>这一个 `UnitState` 是不是还在**这一方**的棋盘上（按**对象身份**比，⛔ 不比名字）。</summary>
    static bool N1OnBoard(BattleContext ctx, int p, UnitState u)
    {
        if (ctx == null || u == null) return false;
        for (int s = 0; s < BoardSpec.Size; s++)
            if (ReferenceEquals(ctx.Players[p].Board[s], u)) return true;
        return false;
    }

    // ==================================================================
    //  ① `GSC55`：墓地 / 棋盘**混场**（活人一张都不许被收走）
    // ==================================================================

    /// <summary>
    /// **`GSC55 Eve of the Uprising`（`Return up to 5 friendly troops that died this game to your deck
    /// and lower their cost by 3`）的混场场景。**
    ///
    /// <para>**判据（第一权威）**：
    ///   · 卡数据 `cards_engine.json` 的 `GSC55.abilityTargets[0].ta = [60, 150, 150]`
    ///     （`60 = inCemeteryFiltered`）、`abilityMultipliers[0].fm = [5, 0, 0]`；
    ///   · `AbilityLogic__GetTargets.c:949-960`（`0x3c` 支）→ `BattleManager__GetCardsInCemetery.c`
    ///     —— 候选取自**墓地列表**（正序 = 最早进墓的在前）；
    ///   · `CardScript__GoToCemetery` → `PlayerHand.RemoveFromGameCard`：**回牌库时那张牌离开墓地**
    ///     （`TakeFromGraveyard` 的口径：`Discard` 与 `DeadUnits` **一起**撤）。</para>
    ///
    /// <para>🔴 **这一条是 `DoReturn` 那次修正的主判别式**：改之前 `DoReturn` 走
    /// `ResolveTargets`（棋盘扫描）⇒ 棋盘上那 2 张**活人**会被收进牌库、墓地那 3 张反而不动。
    /// ⇒ 下面「活人还在棋盘上」「活人不在牌库」两条会**同时红**。</para>
    ///
    /// <para>⚠️ 为什么由 `TestW64ShowCardsWindow` 调用：那条用例已经打了一遍 `GSC55`
    /// （并断言「摊开哪几张」），本函数只补**混场**与**费用**两半 ——
    /// 不新开用例方法的原因见文件头（`gen_diag_runner` 不在本笔白名单）。</para>
    /// </summary>
    static void N1Gsc55CemeteryReturn(CardDef eve)
    {
        Section("🆕 N1 · `GSC55` 混场：3 张在墓地 + 2 张活着 ⇒ **只收墓地那 3 张**");

        if (eve == null) { CheckTrue(false, "前提：`Eve of the Uprising` 拿得到（上一条已报）"); return; }

        var ctx = ProbeBattle(new[] { eve }, new CardDef[0]);
        ToP1Turn(ctx, 2);                    // → P1 的回合（战术卡打在自己回合）
        int p = ctx.Active;
        ctx.Players[p].Energy = 99;

        // ---- 先摆 3 张「会死」的（**5 费** —— 后面靠 `lower their cost by 3` 验 5 → 2）----
        var deadNames = new List<string> { "N1DeadA", "N1DeadB", "N1DeadC" };
        var dead = new List<UnitState>
        {
            Place(ctx, p, 3, Unit(deadNames[0], 5, 1, 9)),
            Place(ctx, p, 5, Unit(deadNames[1], 5, 1, 9)),
            Place(ctx, p, 6, Unit(deadNames[2], 5, 1, 9)),
        };
        foreach (var u in dead) N1Kill(ctx, u);

        int deadRecs = 0;
        foreach (var d in ctx.DeadUnits) if (d != null && d.Owner == p) deadRecs++;
        Check(deadRecs, 3, "前提：那 3 张真的进了 `DeadUnits`（= 「本局阵亡」那份表）"
                         + "｜🧨 夹具改成 `ApplyDamage`（不含死亡链）⇒ 本行实得 0，下面全是假绿");

        // ---- 再摆 2 张「活着」的（**在弄死之后摆** —— `CleanupDeaths` 会让棋盘补位，
        //      放在它之前摆会被搬走，断言就会指错对象）----
        var aliveNames = new List<string> { "N1AliveA", "N1AliveB" };
        var alive = new List<UnitState>
        {
            Place(ctx, p, 3, Unit(aliveNames[0], 1, 2, 9)),
            Place(ctx, p, 5, Unit(aliveNames[1], 1, 2, 9)),
        };
        int boardBefore = 0;
        for (int s = 0; s < BoardSpec.Size; s++) if (ctx.Players[p].Board[s] != null) boardBefore++;
        Check(boardBefore, 3, "前提：棋盘上 = 督军 + 那 2 张**活的**（3 个）");

        int handIdx = HandIdx(ctx, p, "Eve of the Uprising");
        CheckTrue(handIdx >= 0, "前提：`Eve of the Uprising` 在施放者手上");
        if (handIdx < 0) return;

        ctx.ClearSignals();
        CheckCode(RuleCore.PlayTactic(ctx, p, handIdx), RuleCodes.OK, "前提：`Eve of the Uprising` 打得出去");

        // ---- ★ 判别式 1：棋盘上的活人**一张都没动** ----
        int aliveStill = 0;
        foreach (var u in alive)
            if (u != null && u.IsAlive && N1OnBoard(ctx, p, u)) aliveStill++;
        Check(aliveStill, 2,
              "★ **棋盘上那 2 张活人一张都没被收走**（卡面写的是 `troops that **died this game**`）"
            + "｜🧨 把 `DoReturn` 换回棋盘扫描（`ResolveTargets`）⇒ 本行实得 0，且下面「不在牌库」也红");

        // ---- ★ 判别式 2：活人**没有**进牌库 ----
        int aliveInDeck = 0;
        foreach (var inst in ctx.Players[p].Deck)
            if (inst != null && inst.Card != null && aliveNames.Contains(inst.Card.Name)) aliveInDeck++;
        Check(aliveInDeck, 0, "★ 那 2 张活人**没有**进牌库｜🧨 同上（棋盘扫描会把它们搬走）⇒ 本行红");

        // ---- ★ 墓地那 3 张：进了牌库、墓地里没有了 ----
        int deadInDeck = 0;
        var deadInDeckInsts = new List<CardInstance>();
        foreach (var inst in ctx.Players[p].Deck)
            if (inst != null && inst.Card != null && deadNames.Contains(inst.Card.Name))
            { deadInDeck++; deadInDeckInsts.Add(inst); }
        Check(deadInDeck, 3, "★ 墓地那 3 张**进了牌库**（`up to 5` 只取到 3 张）"
                           + "｜🧨 只在墓地取走却忘了搬进牌库 ⇒ 本行实得 0（那 3 张会凭空消失）");
        int deadLeft = 0;
        foreach (var d in ctx.DeadUnits) if (d != null && d.Owner == p) deadLeft++;
        Check(deadLeft, 0, "★ 墓地记录**一起被撤掉**（`TakeFromGraveyard`：`Discard` 与 `DeadUnits` 必须一起移）"
                         + "｜🧨 只移一边 ⇒ 同一张卡能被捞两次（`TakeFromGraveyard` 自己的 doc 点名的就是这个）");
        int discLeft = 0;
        foreach (var inst in ctx.Players[p].Discard)
            if (inst != null && inst.Card != null && deadNames.Contains(inst.Card.Name)) discLeft++;
        Check(discLeft, 0, "★ 弃牌堆里也没有它们了（搬去牌库 = 离开墓地）｜🧨 同上 ⇒ 本行红");

        // ---- ★ `lower their cost by 3` 钉在【那 3 份】上（`DoReturn` 末尾填的 `LastCreated`）----
        //    判据：那 3 张卡面费用 5 · 原版这一条 `abilityLogic` 是 `changeCost` 挂在那几张牌上
        //    ⇒ 现算费用 = 5 − 3 = **2**。⛔ 别拿「按卡 id 匹配全部副本」充数（同名未回的牌也会被降）。
        if (deadInDeckInsts.Count == 3)
        {
            int allTwo = 0;
            foreach (var inst in deadInDeckInsts)
                if (RuleCore.CostOf(ctx, p, inst) == 2) allTwo++;
            Check(allTwo, 3, "★ 回牌库的那 3 份**费用各降 3**（5 → 2，按**实例**钉住）"
                           + "｜🧨 `DoReturn` 忘了填 `ctx.LastCreated`（或填的是卡模板）⇒ 本行实得 0；"
                           + "🧨 改成「按卡 id 匹配全部副本」⇒ 同名卡会一起被降（另一条账，别在这里放宽）");
        }

        // ---- ★ 摊开的窗 = 那 3 张（顺序 = **死亡先后**，原版 cemetery 列表正序）----
        BattleEvent win = null;
        int nWin = 0;
        foreach (var e in ctx.Signals) if (e.Kind == EvtKind.ShowCardsWindow) { nWin++; win = e; }
        Check(nWin, 1, "★ 混场也只发**一条** `ShowCardsWindow`｜🧨 每个目标短语各发一条 ⇒ 本行红");
        if (win != null && win.CardNames != null)
        {
            Check(win.CardNames.Count, 3, "★ 摊开 3 张（= 刚从墓地回去的那批）｜🧨 把活人也算进去 ⇒ 本行实得 5");
            bool orderOk = win.CardNames.Count == 3
                        && win.CardNames[0] == deadNames[0]
                        && win.CardNames[1] == deadNames[1]
                        && win.CardNames[2] == deadNames[2];
            CheckTrue(orderOk,
                      "★ 顺序 = **死亡先后**（`N1DeadA → B → C`；原版 `GetCardsInCemetery` 按 cemetery 列表正序）"
                    + "｜🧨 改成倒序（`DeadUnits` 从尾往前扫）⇒ 本行红；"
                    + "🧨 从 `Discard` 按名字随便取 ⇒ 顺序会漂");
        }
    }

    // ==================================================================
    //  ② `T03·d` / `P2-8`：`berzerk` 候选 = **黑名单**（不是 `IsUnit` 白名单）
    // ==================================================================

    /// <summary>
    /// **`BattleManager.GetPendingBerzerkUnit` 的候选口径。**
    ///
    /// <para>**判据（第一权威，逐句现读）** = `BattleManager__GetPendingBerzerkUnit.c:49-57`：
    ///   `iVar2 = get_cardType(u); if (((iVar2 != 0x14 /*Tactic 20*/) &amp;&amp; (iVar2 != 0x28 /*Whispers 40*/))
    ///   &amp;&amp; (*(char*)(u + 0x65 /*isRemnant*/) == '\0')) { … IsInPlay() … }`
    ///   ⇒ **黑名单两档**，其余一律入候选（`CardTypeOptions` = Minion 0 / Hero 10 / Tactic 20 / Whispers 40，
    ///   `dump.cs:45710-45717`）。</para>
    ///
    /// <para>🔴 **为什么要专门为它写一条**：我们原来是**白名单** `Card.IsUnit`（`Type ∈ {unit, hero}`）。
    /// 本池 `type` 只有四种（`unit / tactic / hero / defence`，`defence` = 原版 `Whispers 40`）
    /// ⇒ **两种写法在池内逐张等价、行为差 = 0**；但口径不等价（铁律 11：都做）。
    /// ⇒ 判别式只能造一个**池里没有的类型**（这里用 `"token"`）：黑名单会收它、白名单不收。</para>
    ///
    /// <para>⚠️ 池内带 `berzerk` 的卡 **0 张**（`cards_engine.json` 1127 张现扫）⇒ 这条用例全用夹具。</para>
    /// </summary>
    static void N1BerzerkCandidateFilter()
    {
        Section("🆕 N1 · `berzerk` 候选口径 = **黑名单两档**（原版 `GetPendingBerzerkUnit.c:49-57`）");

        var ctx = Battle(new[] { Unit("A", 1, 1, 1), Unit("B", 1, 1, 1), Unit("C", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        RuleCore.BeginTurn(ctx);                 // → P1 第 1 回合（`PendingBerzerkUnits` 只认当前行动方）

        var uUnit = Place(ctx, 0, 3, Unit("N1BzUnit", 1, 3, 5));
        uUnit.AddKeyword(RuleCore.Berzerk, 1);

        // ⚠️ 类型 `token` **不在本池的四种取值里** —— 这一格就是「黑名单 vs 白名单」的判别式
        var tokCard = new CardDef("N1BzToken", "N1BzToken", "token", "", null, "Test", 1, 3, 5, 0, null);
        var uToken = Place(ctx, 0, 5, tokCard);
        uToken.AddKeyword(RuleCore.Berzerk, 1);

        // 原版 `Whispers(40)` = 我们的 `defence` 档 ⇒ **必须排除**
        var defCard = new CardDef("N1BzDef", "N1BzDef", "defence", "", null, "Test", 1, 3, 5, 0, null);
        var uDef = Place(ctx, 0, 6, defCard);
        uDef.AddKeyword(RuleCore.Berzerk, 1);

        // 正对照：`unit` 档、但没有 berzerk ⇒ 不该进
        Place(ctx, 0, 7, Unit("N1BzPlain", 1, 3, 5));

        var pending = RuleCore.PendingBerzerkUnits(ctx, 0);
        CheckTrue(pending.Contains(uUnit), "★ `unit` 型的狂暴单位在候选里（正对照）");
        CheckTrue(pending.Contains(uToken),
                  "★ **`token` 型的狂暴单位也在候选里** —— 原版是**黑名单**（只排 `Tactic 20` / `Whispers 40`）"
                + "｜🧨 把 `PendingBerzerkUnits` 换回 `Card.IsUnit` 白名单 ⇒ **本行红**（白名单会把它漏掉）");
        CheckTrue(!pending.Contains(uDef),
                  "★ `defence`（= 原版 `Whispers 40`）型的狂暴单位**不在**候选里（黑名单那一档）"
                + "｜🧨 把黑名单写成「只排 tactic」⇒ 本行红");
        Check(pending.Count, 2,
              "★ 候选恰好 2 个（`unit` + `token`；`defence` 与没有 berzerk 的都被排掉）"
            + "｜🧨 少了/多了上面任何一条 ⇒ 本行红（和逐条断言互为交叉校验）");
    }

    // ==================================================================
    //  ③ `AttackGiantKiller`（trait 340）的引擎建模
    // ==================================================================

    /// <summary>
    /// **巨人杀手（`giantkiller`，`DefinedTrait.giantKiller = 340` / 反编译 `0x154`）。**
    ///
    /// <para>**判据（第一权威，逐支现读）**：
    ///   · `BattleManager__IsValidAttackTarget.c:182-190` —— 攻方带 `0x154` 时，
    ///     目标**必须**是 `CardScript.IsDefender` 或 `cardType == 10`（督军）；
    ///   · `CardScript__IsDefender.c` = 在场 ∧ 非隐身 ∧ `HasVanguardTrait`（= 非隐身 ∧ 带 `vanguard`）；
    ///   · 词条 `Battle/Tips/AttackGiantKiller`（字面量 `DAT_184289890` → `stringliteral.json`）
    ///     = `You must attack the Warlord or a Front Line unit` / **`你必须攻击战将或前线单位`**
    ///     （官方本地化 en/zh 两列都在）。</para>
    ///
    /// <para>⚠️ 池内 **0 张**卡带该 trait ⇒ 全用夹具（铁律 11 例外①：零行为变化）。</para>
    /// </summary>
    static void N1GiantKiller()
    {
        Section("🆕 N1 · `AttackGiantKiller`（trait 340）：只能打**战将**或**前线（vanguard）**单位");

        // ---- ⓪ trait 名真的被解析出来了（⛔ 别只测结算、不测解析）----
        var gk = Unit("N1GK", 1, 3, 5, "GiantKiller");
        CheckTrue(gk.Has(KeywordTable.GiantKiller),
                  "★ `GiantKiller` 这个关键词**认得出**（`KeywordTable.GiantKiller`；"
                + "漏登记 `Prefixes` ⇒ 构造期就被静默丢掉，本行红）"
                + "｜🧨 把 `Prefixes` 里那两条删掉 ⇒ 本行红");
        // 另一种拼法（带空格）—— 同 `drop pod` / `droppod` 那一档
        var gkSpaced = Unit("N1GK2", 1, 3, 5, "Giant Killer");
        CheckTrue(gkSpaced.Has(KeywordTable.GiantKiller),
                  "★ 带空格那种拼法（`Giant Killer`）也认得出（`Normalize` 是前缀匹配，⛔ 只登记一条会漏）");
        CheckTrue(KeywordTable.Implemented.Contains(KeywordTable.GiantKiller),
                  "★ 它进了 `KeywordTable.Implemented`（有真读点；不进 ⇒ 卡面白打 `*`、未实现关键词多报一条）");

        // ---- ① 目标 = 普通部队 ⇒ 拒 ----
        {
            var ctx = Battle(new[] { Unit("GKA", 1, 3, 5), Unit("F1", 1, 1, 1) },
                             new[] { Unit("Plain", 1, 1, 5), Unit("F2", 1, 1, 1) });
            ToP1Turn(ctx, 3);
            Place(ctx, 0, 3, Unit("GKA", 1, 3, 5, "GiantKiller"));
            Place(ctx, 1, 3, Unit("Plain", 1, 1, 5));
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3, ranged: false),
                      RuleCodes.ErrAttackGiantKiller,
                      "★ 巨人杀手打**普通部队** ⇒ `ErrAttackGiantKiller`"
                    + "｜🧨 把 `IsValidTarget` 里那一段删掉 ⇒ 本行实得 `OK`");
        }

        // ---- ② 目标 = 前线（vanguard）⇒ 允许 ----
        {
            var ctx = Battle(new[] { Unit("GKB", 1, 3, 5), Unit("F1", 1, 1, 1) },
                             new[] { Unit("Front", 1, 1, 5, "Vanguard"), Unit("F2", 1, 1, 1) });
            ToP1Turn(ctx, 3);
            Place(ctx, 0, 3, Unit("GKB", 1, 3, 5, "GiantKiller"));
            Place(ctx, 1, 3, Unit("Front", 1, 1, 5, "Vanguard"));
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3, ranged: false), RuleCodes.OK,
                      "★ 打**前线（`vanguard`）**单位是合法的（`CardScript.IsDefender` = 带 vanguard）"
                    + "｜🧨 把这一档一起拒掉（只认督军）⇒ 本行红");
        }

        // ---- ③ 目标 = 敌方督军 ⇒ 允许 ----
        {
            var ctx = Battle(new[] { Unit("GKC", 1, 3, 5), Unit("F1", 1, 1, 1) },
                             new[] { Unit("F2", 1, 1, 1) });   // 敌方棋盘上只有督军
            ToP1Turn(ctx, 3);
            Place(ctx, 0, 3, Unit("GKC", 1, 3, 5, "GiantKiller"));
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, BoardSpec.WarlordSlot, ranged: false), RuleCodes.OK,
                      "★ 打**敌方督军**是合法的（原版 `cardType == 10` 那一支）"
                    + "｜🧨 把 `!target.IsWarlord` 那一半去掉 ⇒ 本行红");
        }

        // ---- ④ 负对照：**没有** giantkiller 的攻击者打普通部队 ⇒ 允许 ----
        {
            var ctx = Battle(new[] { Unit("Nrm", 1, 3, 5), Unit("F1", 1, 1, 1) },
                             new[] { Unit("Plain2", 1, 1, 5), Unit("F2", 1, 1, 1) });
            ToP1Turn(ctx, 3);
            Place(ctx, 0, 3, Unit("Nrm", 1, 3, 5));
            Place(ctx, 1, 3, Unit("Plain2", 1, 1, 5));
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 3, ranged: false), RuleCodes.OK,
                      "★ 负对照：没有 `giantkiller` 的攻击者**照样**能打普通部队"
                    + "｜🧨 把闸写成「凡攻击者都要前线/战将」⇒ 本行红");
        }

        // ---- ⑤ 拒码 → 词条键 / 中文名 ----
        Check(RuleCodes.TermKey(RuleCodes.ErrAttackGiantKiller), "Battle/Tips/AttackGiantKiller",
              "★ `ErrAttackGiantKiller` → `Battle/Tips/AttackGiantKiller`（原版 `DAT_184289890`）"
            + "｜🧨 漏 `Terms` 那行 ⇒ 表现层拿不到文案（本行实得空）");
        CheckTrue(RuleCodes.Describe(RuleCodes.ErrAttackGiantKiller) != null
               && RuleCodes.Describe(RuleCodes.ErrAttackGiantKiller).IndexOf("未知", StringComparison.Ordinal) < 0,
                  "★ 诊断面有中文名（漏 `Names` 那行 ⇒ `Describe` 吐「未知错误码 28」，本行红）");
    }
}
