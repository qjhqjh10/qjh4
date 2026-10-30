// B101R5_Assert.cs — 执行代理 `B101_R5`（第一百零一会话 · 账 637 三路机制的真残差）的断言集
//
// ============================ 它盯的是什么 ============================
//  账 `战斗逻辑驱动` 637 `BattleManagerSupport.BroadcastRemoveFromGame` 的**三条真残差**（W4 只记账、W5 只改注释）：
//    · W1 —— `DoTakeControl`（`EffectResolver.cs:9436`）补上原版第 ⑤ 跳
//            `CardScript.RemoveAllExtrinsicEffects()`（`IL:BattleManager.cs:38713` ← 体 `IL:CardScript.cs:11949-11968`）：
//            **摘全部 extrinsic（不按来源）** ⇒ 抢来的单位不再带着前主人给的增益跑过去。
//    · W2 —— `DoExchange`（`EffectResolver.cs:17146`）补上原版 4 次 `PlayerHand.RemoveFromGameCard`
//            （`IL:BattleManager.cs:35383-35387`，体 `IL:PlayerHand.cs:720-748`）= 摘「extrinsic ∧ enchantingCard == 那张」
//            的**手牌效果** ⇒ 我们的等价口 `RemoveHandEffectsCastBy` ×2（自扫两方）。
//    · W3 —— 手牌目标的 `ResolveTransformUnit`（`DoBecome`，`EffectResolver.cs:3534`）：
//            broadcast（`C:BattleManager__ResolveTransformUnit.c:416`）→ 两手撤手牌效果（`:417-516`）→
//            `keepEffects` 两支（`:559-703`）→ 换卡（`:791`）。原版是**同一个 `CardScript`**、`uniqueInGameID(+0x22c)`
//            **不变**（`IL:BattleManager.cs:11385` 起的 `obj8`）⇒ 我们换实例但**沿用旧 `Id`**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A4：把 `:9436` 那一句 `t.RemoveAllExtrinsicEffects();` 删掉 ⇒ 抢过来的单位仍带着 extrinsic +2 血 ⇒ A4 红。
//  · B3：把 `:17146` 的 `RemoveHandEffectsCastBy` ×2 删掉 ⇒ 日志回到「⇒ 跳过」/ 没有新句子 ⇒ B3/B4 红。
//  · C4：把 `:3534` 改回 `ps.Hand[i] = ctx.NewInstance(pick);` ⇒ 变身后的实例号是**新号** ⇒ C4 红。
//  · C3 是**前提**断言（两张战术必须真变了身）——⛔ 不是 `if (前提) Check(结论)`；前提不成立时它自己先红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/BattleScene.cs` 的**方法尾聚合行**（现读 `:46699`，`Debug.Log(P + $"=== 合计…` 之前）
//  同一物理行追加：
//      B101R5_Assert.Run(); pass += B101R5_Assert.Pass; fail += B101R5_Assert.Fail;
//
// ============================ 判据 / 报告 ============================
//  · 原版：`d:/2/tools/decomp_full/{BattleManager__ResolveTransformUnit.c,BattleManagerSupport__BroadcastRemoveFromGame.c}`
//          `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/{BattleManager.cs,CardScript.cs,PlayerHand.cs}`
//  · 报告：`资料/完全复刻/报告/B101_R5_三路机制.md`
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B101_R5`：账 637 W1 / W2 / W3 三条真残差。</summary>
public static class B101R5_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B101R5] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具
    static List<CardDef> _pool;

    static List<CardDef> Pool()
    {
        if (_pool != null && _pool.Count > 0) return _pool;
        try { _pool = CardDatabase.Load(); }
        catch (Exception e)
        {
            Debug.LogWarning("[B101R5] `CardDatabase.Load()` 抛了：" + e.Message);
            _pool = new List<CardDef>();
        }
        return _pool;
    }

    static CardDef U(string id, int cost, int atk, int hp)
    {
        return new CardDef(id, id, "unit", "", null, "Test", cost, atk, hp, 0, null);
    }

    static int _heroSeq;

    static CardDef Hero(string faction)
    {
        string nm = "B101R5Hero_" + faction + "_" + (_heroSeq++);   // 两侧督军同名会干扰按名排查 ⇒ 序号区分
        return new CardDef(nm, nm, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }

    static CardDef Tac(string id)
    {
        return new CardDef(id, id, "tactic", "Draw a card", "", "Test", 1, 0, 0, 0, null);
    }

    /// <summary>督军 + 20 张 filler + 末尾那几张（与 `RuleEngineTest.DeckOf` 同形：抽牌从列表末尾拿）。</summary>
    static List<CardDef> Deck(string faction, params CardDef[] hand)
    {
        var d = new List<CardDef> { Hero(faction) };
        for (int i = 0; i < 20; i++) d.Add(U("f" + faction + i, 1, 0, 1));
        for (int i = hand.Length - 1; i >= 0; i--) d.Add(hand[i]);
        return d;
    }

    /// <summary>推到 P1（座位 0）的第 1 个回合；推不到就返回 false（由断言出声，⛔ 不静默）。</summary>
    static bool ToP1(BattleContext ctx)
    {
        int guard = 0;
        while (ctx.Turn != 1 || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            else RuleCore.EndTurn(ctx);
            if (++guard > 12) return false;
        }
        return true;
    }

    static int HandIdx(BattleContext ctx, int p, string name)
    {
        var hand = ctx.Players[p].Hand;
        for (int i = 0; i < hand.Count; i++)
            if (hand[i] != null && hand[i].Card != null && hand[i].Card.Name == name) return i;
        return -1;
    }

    static string Events(BattleContext ctx)
    {
        if (ctx == null) return "";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < ctx.Events.Count; i++) sb.Append(ctx.Events[i]).Append('\n');
        return sb.ToString();
    }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B101_R5 账 637：W1 steal 全摘 extrinsic · W2 exchange 手牌清扫 · W3 手牌变身沿用实例号");
        SectionA_StealRemoveAllExtrinsic();
        SectionB_ExchangeHandSweep();
        SectionC_HandTransformKeepsId();
        Debug.Log(_sink.P + "B101R5 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · W1（有真卡可达）
    static void SectionA_StealRemoveAllExtrinsic()
    {
        MenuCheck.Section(_sink, "A · W1 `DoTakeControl` 补 `RemoveAllExtrinsicEffects`（原版 :38713）");

        var pool = Pool();
        Tr(pool.Count > 0, "A0 **前提**：卡池读得到（读不到 ⇒ 本节全部无意义，⛔ 不静默跳过）");
        if (pool.Count == 0) return;

        // 用**真卡**的解析结果当 op（`GSC49` 的正文里那一条 `takecontrol`），别手搓目标条件 —— 免得测法失真。
        var td = CreatePool.FindByName(pool, "Telepathic Domination");
        Tr(td != null, "A1 **前提**：池里有 `Telepathic Domination`（`GSC49`）");
        if (td == null) return;
        List<EffectOp> parsed = EffectText.Parse(td.Desc, out _, out _);
        EffectOp tc = null;
        for (int i = 0; parsed != null && i < parsed.Count; i++)
            if (parsed[i] != null && parsed[i].Verb == "takecontrol") tc = parsed[i];
        Tr(tc != null, "A2 **前提**：正文解出 `takecontrol` op（解不出 ⇒ 下面的结论不成立）");
        if (tc == null) return;

        var atk = U("R5Atk", 1, 2, 3);
        var vicCard = U("R5Vic", 1, 2, 5);
        var ctx = RuleCore.NewBattle(Deck("Test", atk), Deck("Test", vicCard), seed: 0, shuffle: false, cardPool: pool);
        ctx.Active = 0;
        ctx.Players[0].Energy = 20;
        int aslot, vslot;
        Tr(RuleCore.DeployFree(ctx, 0, atk, out aslot), "A3a **前提**：己方单位上场（slot=" + aslot + "）");
        Tr(RuleCore.DeployFree(ctx, 1, vicCard, out vslot), "A3b **前提**：敌方单位上场（slot=" + vslot + "）");
        var v = ctx.Players[1].Board[vslot];
        Tr(v != null, "A3c **前提**：拿到被抢的那个 `UnitState`");
        if (v == null) return;

        int baseMax = v.MaxHealth;
        int baseAtk = v.Attack;
        v.AddStatMod("health", 2, null, extrinsic: true);      // 前主人给的 extrinsic +2 血
        v.AddStatMod("attack", 1, null, extrinsic: false);     // 非 extrinsic 的近战 +1（⛔ 不该被摘）
        C(v.MaxHealth, baseMax + 2, "(前提 P1) extrinsic +2 血生效 ⇒ 上限 +2");
        C(v.Attack, baseAtk + 1, "(前提 P2) 非 extrinsic 近战 +1 生效");

        RuleCore.ResolveOps(ctx, 0, null, new List<EffectOp> { tc }, "B101R5");

        bool onP0 = false;
        for (int s = 0; s < BoardSpec.Size; s++) if (ctx.Players[0].Board[s] == v) onP0 = true;
        Tr(onP0, "★ A4 被抢的单位到了**我方**棋盘上（归属 = 待在谁的 `Board[]` 里）");
        Tr(!(ctx.Players[1].Board[vslot] == v),
           "★ A4b 它**离开了对面原来那一格**（原版 `RemoveMinion` = 摘 List、外侧补位）——" + Events(ctx));
        C(v.MaxHealth, baseMax, "★★ A4c 抢过来之后 **extrinsic +2 血被摘**（原版 `IL:BattleManager.cs:38713`"
                              + " `RemoveAllExtrinsicEffects`；判据体 `IL:CardScript.cs:11949-11968`）"
                              + "｜🧨 删掉 `EffectResolver.cs:9436` 那半句 ⇒ 停在 " + (baseMax + 2) + " ⇒ 红");
        C(v.Attack, baseAtk + 1, "★★ A4d 灭自证/边界：**非** extrinsic 的近战 +1 **仍在**（只摘 extrinsic，⛔ 别用"
                               + " `RemoveAllAppliedEffectsForJam` 顶 —— 那个会连它一起摘 ⇒ 实得 " + baseAtk + " ⇒ 红）");
    }

    // ============================================================ B · W2（0 张可达，验接线 + 日志口径）
    static void SectionB_ExchangeHandSweep()
    {
        MenuCheck.Section(_sink, "B · W2 `DoExchange` 两次 `RemoveHandEffectsCastBy`（原版 :35383-35387 四次）");

        var pool = Pool();
        var a = U("R5ExA", 1, 2, 4);
        var b = U("R5ExB", 1, 2, 4);
        var ctx = RuleCore.NewBattle(Deck("Test", a), Deck("Test", b), seed: 0, shuffle: false, cardPool: pool);
        ctx.Active = 0;
        int aslot, bslot;
        Tr(RuleCore.DeployFree(ctx, 0, a, out aslot), "B1 **前提**：己方单位上场（slot=" + aslot + "）");
        Tr(RuleCore.DeployFree(ctx, 1, b, out bslot), "B2 **前提**：敌方单位上场（slot=" + bslot + "）");
        var ua = ctx.Players[0].Board[aslot];
        var ub = ctx.Players[1].Board[bslot];
        Tr(ua != null && ub != null, "B2b **前提**：两个 `UnitState` 都拿得到");
        if (ua == null || ub == null) return;

        var op = new EffectOp
        {
            Verb = "exchange", Source = "B101R5",
            Target = new EffectTargetSpec { Raw = "enemy", Side = "enemy", Kind = "any", Count = 1 },
        };
        // ④ 2026-10-30（`B102_D1` 顺手项 · B4 原来靠「方法名出现在日志里」⇒ 只删调用、留日志照样绿）：
        //   夹具里真挂两条手牌记录 —— ① `extrinsic` 由 A 施放（换位后**应被摘**）；② **非** extrinsic 同施放者（**不该被摘**）。
        //   判据 = `EffectResolver.cs:17488` 的 `RemoveHandEffectsCastBy`：`deadIds.Count == 0 ⇒ continue`（那句日志都不打）
        //   ⇒ **没有 ① 这条夹具，日志根本不会有那句话**，B4 就退化成「盯文案」。
        int recExt = ctx.NextHandEffectRecord();
        int recKeep = ctx.NextHandEffectRecord();
        ctx.Players[0].HandEffectRecords.Add(new CardInstance.HandEffect { RecordId = recExt, Source = "B101R5", Extrinsic = true, ActingUnit = ua });
        ctx.Players[0].HandEffectRecords.Add(new CardInstance.HandEffect { RecordId = recKeep, Source = "B101R5", Extrinsic = false, ActingUnit = ua });

        RuleCore.ResolveOps(ctx, 0, ua, new List<EffectOp> { op }, "B101R5", chosen: ub);

        Tr(ctx.Players[0].Board[aslot] == ub, "★ B3 `exchange` 真的换了位：敌方的 B 出现在我方的 a 号格");
        Tr(ctx.Players[1].Board[bslot] == ua, "★ B3b ……而 A 到了对面 b 号格（归属 + 格位一起对调）");
        string log = Events(ctx);
        Tr(log.IndexOf("RemoveHandEffectsCastBy", StringComparison.Ordinal) >= 0,
           "★★ B4 原版 4 次 `PlayerHand.RemoveFromGameCard` 的等价口**跑过了**（新日志点了 `RemoveHandEffectsCastBy`）"
         + "｜🧨 删掉 `EffectResolver.cs:17146` 那两次调用 ⇒ 这一条红");

        // ④ 2026-10-30（`B102_D1` 顺手项 · 选 (a)：夹具里真挂记录 ⇒ B4 从「盯文案」变成「盯行为」）。
        bool recExtLeft = false, recKeepLeft = false;
        for (int i = 0; i < ctx.Players[0].HandEffectRecords.Count; i++)
        {
            var r = ctx.Players[0].HandEffectRecords[i];
            if (r == null) continue;
            if (r.RecordId == recExt) recExtLeft = true;
            if (r.RecordId == recKeep) recKeepLeft = true;
        }
        Tr(!recExtLeft, "★★ B4a（强）施放者被换位 ⇒ 由它施放的 **extrinsic** 手牌记录**被摘掉**（原版 4 次 `PlayerHand.RemoveFromGameCard` 的等价口）"
                      + "｜🧨 删掉 `EffectResolver.cs:17146` 那两次 `RemoveHandEffectsCastBy` ⇒ 记录还在 ⇒ 红");
        Tr(recKeepLeft, "★★ B4b 灭自证/边界：**非** extrinsic（同施放者）那条记录**仍在**（只摘 extrinsic，⛔ 别改成整表清空）");
        Tr(log.IndexOf("手牌记录表上摘掉 1 条", StringComparison.Ordinal) >= 0,
           "★★ B4c 日志里出现「手牌记录表上摘掉 1 条」（`RemoveHandEffectsCastBy` 只在 `deadIds.Count > 0` 时打这一句）"
         + "｜🧨 只删调用留文案 ⇒ B4a/B4c 同时红");
        // 🔴 2026-10-30 订正（`B102_D1` 三条红 ② · 负向 token 太宽）：查子串 `⇒ 跳过` 会命中**新**日志自己
        //   （`EffectResolver.cs:17147-17148` 逐字引用了旧文案「没有对象 ⇒ 跳过」）⇒ 这条永远红、零判别力。
        //   换成旧文案的**独有片段** `在我们模型里**没有对象**`（新文案里没有这一段）。
        Tr(log.IndexOf("在我们模型里**没有对象**", StringComparison.Ordinal) < 0,
           "★★ B5 灭自证：日志里**不再**出现旧的「在我们模型里**没有对象**（在场单位本来就不在 `Hand` 列表里）⇒ 跳过」"
         + "｜🧨 把 `:17147-17148` 的文案改回去 ⇒ 红");
    }

    // ============================================================ C · W3（1 张可达：SW18 的 Rally 手牌变身）
    static void SectionC_HandTransformKeepsId()
    {
        MenuCheck.Section(_sink, "C · W3 手牌目标的变身沿用原版 `+0x22c` 实例号（`EffectResolver.cs:3534`）");

        var pool = Pool();
        Tr(pool.Count > 0, "C0 **前提**：卡池读得到");
        if (pool.Count == 0) return;
        var hrolf = CreatePool.FindByName(pool, "Hrolf the Ironhowl");
        Tr(hrolf != null, "C0b **前提**：池里有 `Hrolf the Ironhowl`（`SW18`，正文 `Stratagems in your hand become …`）");
        if (hrolf == null) return;

        var t1 = Tac("R5Strat1");
        var t2 = Tac("R5Strat2");
        var ctx = RuleCore.NewBattle(Deck("SpaceWolves", t1, t2, hrolf), Deck("Test"), seed: 0, shuffle: false, cardPool: pool);
        Tr(ToP1(ctx), "C1 **前提**：推进到 P1 回合（推不到 ⇒ 本节无意义）");
        ctx.Players[0].Energy = 20;

        var idsBefore = new HashSet<int>();
        for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
            if (ctx.Players[0].Hand[i] != null) idsBefore.Add(ctx.Players[0].Hand[i].Id);
        int handBefore = ctx.Players[0].Hand.Count;

        int idx = HandIdx(ctx, 0, "Hrolf the Ironhowl");
        Tr(idx >= 0, "C2 **前提**：Hrolf 在手里");
        if (idx < 0) return;
        int code = RuleCore.PlayCard(ctx, 0, idx, 0);
        C(code, RuleCodes.OK, "C3 **前提**：Hrolf 打出去了（`PlayCard` 返回 OK）");
        if (code != RuleCodes.OK) return;

        var wolves = new List<CardInstance>();
        for (int i = 0; i < ctx.Players[0].Hand.Count; i++)
        {
            var h = ctx.Players[0].Hand[i];
            if (h == null || h.Card == null) continue;
            if (h.Card.Name == "Hunting Wolf" || h.Card.Name == "Fenrisian Wolf") wolves.Add(h);
        }
        C(wolves.Count, 2, "★★ C3b 两张战略卡**都**变了身（`DoBecome` 跑到了）｜🧨 句首 `⚡` 剥不掉 / `become` 解不成 ⇒ 0 或 1 ⇒ 红");
        // 🔴 2026-10-30 订正（`B102_D1` 三条红 ③ · 期望值错）：`handBefore` 是在**打出 Hrolf 之前**取的，
        //   里面**含 Hrolf 自己**；`PlayCard` 把这张**单位卡**从手牌摘掉（`RuleCore.cs:3956` `ps.Hand.RemoveAt(handIdx)`），
        //   两张战术卡是**就地换**（`ps.Hand[i] = __ni`，±0）⇒ 变身本身保张数，但总数 = `handBefore − 1`。
        //   原版判据 = `decomp_full/BattleManager__ResolveTransformUnit.c:1880` 起那段只换定义、不碰手牌 List。
        C(ctx.Players[0].Hand.Count, handBefore - 1,
          "（前提）手牌张数 = 打出前的 " + handBefore + " **减掉打出去的那一张** = " + (handBefore - 1)
        + "（变身是**就地换牌**不是抽/弃 ⇒ 两张战术卡 ±0；⛔ 别写成 `handBefore`：那会把打出去的 Hrolf 也算在手里）");
        for (int i = 0; i < wolves.Count; i++)
            Tr(idsBefore.Contains(wolves[i].Id),
               "★★ C4 变身后的「" + wolves[i].Card.Name + "」实例号 = " + wolves[i].Id
             + " **沿用原手牌那一份**（原版 `ResolveTransformUnit` 是同一个 `CardScript`、`+0x22c` 不变；"
             + "`IL:BattleManager.cs:11385`）｜🧨 把 `:3534` 改回 `ctx.NewInstance(pick)`（= 改前实现）⇒ 新号 ⇒ 红");
    }
}
