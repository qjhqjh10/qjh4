// B101E2_Assert.cs — 第一百零一会话 · 执行代理 `B101_E2`（账 254 `ChoiceOfCardPlayer`：面板候选池 == 引擎结算池）
//
// ============================ 它盯的是什么 ============================
//  原版 `BattleManager__GetChoiceOptions.c:36` 的**第一句**就是 `GetChoiceFullPool(...)` ——
//  **池与 options 在同一刻算**。我们把它拆成了两刻：
//    · 面板那一刻 —— `BattleDriver.BeginPlay` → `ShowAsk`（**牌还在手**）；
//    · 引擎那一刻 —— `EffectResolver.cs:4802` → `:4736 case "hand": ps.Hand.ConvertAll(h => h.Card)`（**牌已离手**）。
//  ⇒ `ChooseSrc == "hand"` 的卡，面板池**必然多一张**（正是这张牌自己）⇒
//    ① `TakePeekedChoiceOptions` 的「候选逐项同 id 同序」复核次次失败、每局刷 `LogWarning`（`EffectResolver.cs:20231`）；
//    ② 面板会把**正在打出的那张牌**摆成选项（`Path of the Seer` 每局必现，原版永不会出现）。
//
//  本件落地 = `CardPresentation/Battle/BattleDriver.cs` 类尾之后的 `ChoicePanelPool`：
//  面板侧算候选一律走 `ChoicePanelPool.PanelChoiceCandidates`（= `RuleCore.ChooseCardCandidates` + 剔一份
//  「正在打出的那张」，三条闸：只对 `hand` 族 / 只在这一份**此刻真在手牌里** / 按 `Id` 剔一份）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · B2-b：把 `ChoicePanelPool.ExcludePendingHandCard` 里的 `cands.RemoveAt(i)` 删掉 ⇒ 面板池又回到「多一张」
//        ⇒ B2-b / B3 红（同时 C 段那两张「自己会进候选」的卡也红）。
//  · B3：变成「按位置剔」而不是「按 `Id` 剔一份」⇒ 剔到别的卡上 ⇒ B3（逐项同 id 同序）红。
//  · D 段：把闸写成「不分族一律剔」⇒ `pool` / `deck` / `enemyhand` / `dead` / `played` 那 43 张的
//        「面板池 == 原样池」当场红（它们的候选域**与自己的手牌无关**，剔了就是错剔掉一张同名卡）。
//  · E 段：把「这一份此刻真在手牌里」那道闸删掉 ⇒ 主动技能/英雄议程那一路（`_pendingInst` 在场上）
//        会把手牌里**同名的那一张**错剔掉 ⇒ E 红。
//  · A1：池里 `hand` 族不再是那 4 张（解析器把来源判错了）⇒ A1 红 —— 那是**前提**红，下面的结果不值得看。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/BattleScene.cs` 的那条聚合行里，`B100F9_Assert.Run();` 之后**同一物理行**追加：
//      B101E2_Assert.Run(); pass += B101E2_Assert.Pass; fail += B101E2_Assert.Fail;
//
// ============================ 判据 / 报告 ============================
//  · 原版：`d:/2/tools/decomp_full/BattleManager__GetChoiceOptions.c:36`（池与 options 同刻）
//  · 复核：`资料/完全复刻/报告/B100_V1.md` §「254」· `B100_X12.md` 条目 4 / 7 /「注意」第 1 条
//  · 本件报告：`资料/完全复刻/报告/B101_E2_254面板池.md`
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B101_E2`：账 254 —— 面板候选池与引擎结算池逐项同 id 同序。</summary>
public static class B101E2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B101E2] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具
    static List<CardDef> _pool;

    /// <summary>真卡池（只读；失败返回空表并出声 —— 不静默）。</summary>
    static List<CardDef> Pool()
    {
        if (_pool != null && _pool.Count > 0) return _pool;
        try { _pool = CardDatabase.Load(); }
        catch (Exception e)
        {
            Debug.LogWarning("[B101E2] `CardDatabase.Load()` 抛了：" + e.Message);
            _pool = new List<CardDef>();
        }
        return _pool;
    }

    /// <summary>垫牌（部队卡 —— `hand` 族的筛选词分别是 `card` / `troop`，部队两边都能过）。</summary>
    static CardDef Troop(string id)
    {
        return new CardDef(id, id, "unit", "", null, "Test", 1, 2, 3, 0);
    }

    /// <summary>新建一局夹具：手牌 = 传入的顺序（`pendingIdx` 就是那个下标）。</summary>
    static BattleContext MakeCtx(params CardDef[] hand)
    {
        var ctx = new BattleContext(0xB101_E2);
        ctx.Players[0] = new PlayerState();
        ctx.Players[1] = new PlayerState();
        ctx.Active = 0;
        var pool = Pool();
        if (pool.Count > 0) ctx.CardPool = pool;      // `pool` / `specific` 族的候选域就是它
        for (int i = 0; i < hand.Length; i++) ctx.Players[0].Hand.Add(ctx.NewInstance(hand[i]));
        return ctx;
    }

    /// <summary>把一串卡的 `Id` 连成一行（逐项同 id 同序的比对口径）。</summary>
    static string Ids(List<CardDef> l)
    {
        if (l == null) return "<null>";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < l.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(l[i] != null ? l[i].Id : "<null>");
        }
        return sb.ToString();
    }

    static bool HasId(List<CardDef> l, string id)
    {
        if (l == null) return false;
        for (int i = 0; i < l.Count; i++) if (l[i] != null && l[i].Id == id) return true;
        return false;
    }

    /// <summary>一张卡的全部 `choosecard` ask（= 面板 / 结算读的**同一份**解析结果）。</summary>
    static List<EffectOp> ChooseOps(CardDef card)
    {
        var outp = new List<EffectOp>();
        List<EffectOp> ops = null;
        try { ops = RuleCore.PlayerChooseOps(card); }
        catch (Exception e) { Debug.LogWarning("[B101E2] `PlayerChooseOps(\"" + card.Name + "\")` 抛了：" + e.Message); }
        if (ops == null) return outp;
        for (int i = 0; i < ops.Count; i++) if (ops[i] != null && ops[i].Verb == "choosecard") outp.Add(ops[i]);
        return outp;
    }

    static string SrcOf(EffectOp op) { return op == null || op.ChooseSrc == null ? "<null>" : op.ChooseSrc; }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B101_E2 账 254：面板候选池 == 引擎结算池（`hand` 族剔「正在打出的那张」）");
        SectionA_Coverage();
        SectionB_PathOfTheSeer();
        SectionC_AllHandFamily();
        SectionD_OtherFamiliesNoOp();
        SectionE_GuardWhenPendingNotInHand();
        Debug.Log(_sink.P + "B101E2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 覆盖（前提，硬断言）
    static void SectionA_Coverage()
    {
        MenuCheck.Section(_sink, "A 覆盖：全池 `choosecard` 卡按 `ChooseSrc` 逐族点数");

        var pool = Pool();
        Tr(pool != null && pool.Count > 0, "A0 **前提**：卡池读得到（读不到 ⇒ 本节全部无意义，⛔ 不静默跳过）");
        if (pool == null || pool.Count == 0) return;

        _hand = new List<KeyValuePair<CardDef, EffectOp>>();
        _other = new List<KeyValuePair<CardDef, EffectOp>>();
        int cards = 0;
        var perFam = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < pool.Count; i++)
        {
            var card = pool[i];
            if (card == null) continue;
            var ops = ChooseOps(card);
            if (ops.Count == 0) continue;
            cards++;
            for (int k = 0; k < ops.Count; k++)
            {
                string src = SrcOf(ops[k]);
                int n; perFam.TryGetValue(src, out n); perFam[src] = n + 1;
                if (src == "hand") _hand.Add(new KeyValuePair<CardDef, EffectOp>(card, ops[k]));
                else _other.Add(new KeyValuePair<CardDef, EffectOp>(card, ops[k]));
            }
        }

        Debug.Log(_sink.P + "扫到带 `choosecard` 的卡 " + cards + " 张 · 逐族（张/条）："
                  + "hand " + Count(perFam, "hand") + " · deck " + Count(perFam, "deck")
                  + " · enemyhand " + Count(perFam, "enemyhand") + " · dead " + Count(perFam, "dead")
                  + " · played " + Count(perFam, "played") + " · pool " + Count(perFam, "pool")
                  + " · specific " + Count(perFam, "specific"));

        Tr(cards >= 50, "A0-b **前提**：全池带 `choosecard` 的卡 ≥ 50 张（现读 " + cards
                      + "；🆕 2026-10-30 `scan` 实数 = 58 张 / 58 条 —— 低于 50 说明解析器把来源判丢了）");
        C(_hand.Count, 4, "A1 `hand` 族 = **4 张**（`Path of the Seer` · `AM4 Faeburn Vanquisher` · "
                        + "`UM74 Rapid Deployment` · `DA75 Master Zacharial`）"
                        + "｜🧨 解析器把来源判错 ⇒ 本行红，⚠️ 那是**前提**红");
        var handIds = new List<string>();
        for (int i = 0; i < _hand.Count; i++) handIds.Add(_hand[i].Key.Id);
        handIds.Sort(StringComparer.Ordinal);
        C(string.Join(",", handIds.ToArray()),
          "09bcb1b3b8794c4429d82d4b2e68fa1f,AM4,DA75,UM74",
          "A1-b `hand` 族那 4 张的 id 就是它们（⛔ 少一张/多一张都要先回源核 `ChooseSrc` 判据）");
        Tr(_other.Count >= 50, "A0-c **前提**：其余族合计 ≥ 50 条（现读 " + _other.Count
                             + "；这些是 D 段「这条闸必须是 no-op」的对象）");
    }

    static int Count(Dictionary<string, int> d, string k)
    {
        int n; return d.TryGetValue(k, out n) ? n : 0;
    }

    static List<KeyValuePair<CardDef, EffectOp>> _hand;
    static List<KeyValuePair<CardDef, EffectOp>> _other;

    // ============================================================ B · `Path of the Seer`（真缺陷 · 灭自证）
    static void SectionB_PathOfTheSeer()
    {
        MenuCheck.Section(_sink, "B `Path of the Seer`（每局必现的那张）：面板池剔掉自己 · 与引擎池逐项同 id 同序");

        var card = FindById("09bcb1b3b8794c4429d82d4b2e68fa1f");
        Tr(card != null, "B0 **前提**：池里有 `Path of the Seer`");
        if (card == null) return;
        var op = FirstOp(card, "hand");
        Tr(op != null, "B0-b **前提**：它的 `choosecard` op 的 `ChooseSrc` 是 `hand`");
        if (op == null) return;

        // 手牌：[垫牌, **待打的这张**, 垫牌, 垫牌] —— 放在中间，顺序问题才露得出来
        var ctx = MakeCtx(Troop("B101E2Fill0"), card, Troop("B101E2Fill1"), Troop("B101E2Fill2"));
        var hand = ctx.Players[0].Hand;
        var pendingInst = hand[1];
        Tr(pendingInst != null && ReferenceEquals(pendingInst.Card, card),
           "B0-c **前提**：手牌第 2 张就是待打的那一张（`PendingCard` 的取值次序与现场同源）");
        if (pendingInst == null) return;

        string sn, dt, wy;
        var raw = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);
        var panel = ChoicePanelPool.PanelChoiceCandidates(ctx, 0, op, card, pendingInst, 1, out sn, out dt, out wy);
        Tr(HasId(raw, card.Id), "B1 **真缺陷在**：原样的面板候选里**确实有这张牌自己** —— 这就是原版永远不会摆出来的那一张"
                              + "｜🧨 候选域/解析改了 ⇒ 先回源核，别当成「修好了」");

        // 引擎那一刻：牌已经离手 ⇒ 直接拿引擎那一口算，作为**期望值**（⛔ 不用本件的 helper 反填期望）
        ctx.Players[0].Hand.RemoveAt(1);
        var engine = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);
        C(raw.Count, engine.Count + 1, "B2 **真缺陷在**（相对口径，不靠绝对张数）：原样的面板候选比引擎池**多一张**"
                                     + "｜🧨 解析/候选域改了 ⇒ 先回源核，别当成「修好了」");
        C(panel.Count, engine.Count, "B2-b ★ 剔完之后面板候选 == 引擎池的张数"
                                   + "｜🧨 删掉 `ExcludePendingHandCard` 里的 `cands.RemoveAt(i)` ⇒ 本行红（回到 +1）");
        Tr(!HasId(panel, card.Id), "B2-c ★ 面板候选里**不再有**正在打出的这一张");
        C(Ids(panel), Ids(engine), "B3 ★★ 面板池 == 引擎结算池（**逐项同 id 同序**）"
                                 + "｜🧨 改成「按位置剔」⇒ 剔到别的卡上 ⇒ 本行红");
    }

    // ============================================================ C · 全池 `hand` 族
    static void SectionC_AllHandFamily()
    {
        MenuCheck.Section(_sink, "C 全池 `hand` 族（4 张）：每一张「面板池 == 引擎结算池」");

        if (_hand == null || _hand.Count == 0) { Tr(false, "C0 **前提**：A 段扫到了 `hand` 族（扫不到这一节全部无意义）"); return; }

        for (int i = 0; i < _hand.Count; i++)
        {
            var card = _hand[i].Key;
            var op = _hand[i].Value;
            var ctx = MakeCtx(Troop("B101E2C" + i + "a"), card, Troop("B101E2C" + i + "b"));
            var inst = ctx.Players[0].Hand[1];

            string sn, dt, wy;
            var raw = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);
            var panel = ChoicePanelPool.PanelChoiceCandidates(ctx, 0, op, card, inst, 1, out sn, out dt, out wy);

            ctx.Players[0].Hand.RemoveAt(1);
            var engine = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);

            C(Ids(panel), Ids(engine), "C1." + (i + 1) + " `" + card.Name + "`（src=hand what=["
                    + (op.ChooseWhat ?? "") + "]）：面板池 == 引擎池逐项同 id 同序"
                    + "｜🧨 `RemoveAt` 那行删掉 ⇒ 自己会进候选的那两张（`Path of the Seer` / `Faeburn Vanquisher`）红");
            // ⚠️ 反之：这张牌**自己不满足筛选**时（例：`Rapid Deployment` 是战术卡、筛选词是 `troop`），
            //    原样候选里本来就没有它 ⇒ 这条闸必须**什么都不做**（剔了就是错剔一张同名卡）。
            if (!HasId(raw, card.Id))
                C(Ids(panel), Ids(raw), "C2." + (i + 1) + " `" + card.Name + "`：它自己不在候选里 ⇒ 这条闸是 **no-op**"
                                      + "（⛔ 别改成「不看在不在就剔一份」）");
        }
    }

    // ============================================================ D · 其余族：这条闸必须 no-op 且池本来就相同
    static void SectionD_OtherFamiliesNoOp()
    {
        MenuCheck.Section(_sink, "D 其余族（deck / enemyhand / dead / played / pool / specific）：闸是 no-op，且池与手牌无关");

        if (_other == null || _other.Count == 0) { Tr(false, "D0 **前提**：A 段扫到了其余族"); return; }

        int bad = 0, checkedN = 0;
        for (int i = 0; i < _other.Count; i++)
        {
            var card = _other[i].Key;
            var op = _other[i].Value;
            var ctx = MakeCtx(card, Troop("B101E2D" + i + "a"), Troop("B101E2D" + i + "b"));
            // 让 `deck` / `enemyhand` 两族也有非空候选域（否则「相同」是空对空的假绿）
            ctx.Players[0].Deck.Add(ctx.NewInstance(Troop("B101E2D" + i + "da")));
            ctx.Players[0].Deck.Add(ctx.NewInstance(Troop("B101E2D" + i + "db")));
            ctx.Players[1].Hand.Add(ctx.NewInstance(Troop("B101E2D" + i + "ea")));
            ctx.Players[1].Hand.Add(ctx.NewInstance(Troop("B101E2D" + i + "eb")));
            var inst = ctx.Players[0].Hand[0];

            string sn, dt, wy;
            var raw = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);
            var panel = ChoicePanelPool.PanelChoiceCandidates(ctx, 0, op, card, inst, 0, out sn, out dt, out wy);

            ctx.Players[0].Hand.RemoveAt(0);
            var engine = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);

            checkedN++;
            bool ok = Ids(panel) == Ids(raw) && Ids(engine) == Ids(raw);
            if (!ok)
            {
                bad++;
                Debug.LogError("[B101E2] D 段失配：`" + card.Name + "` src=" + SrcOf(op)
                             + " · raw=[" + Ids(raw) + "] · panel=[" + Ids(panel) + "] · engine=[" + Ids(engine) + "]");
            }
        }
        C(bad, 0, "D1 ★ 其余族 " + checkedN + " 条：**面板池 == 原样池 == 引擎池**"
                + "（`pool` / `deck` / `enemyhand` / `dead` / `played` / `specific` 的候选域与自己手牌无关）"
                + "｜🧨 把闸改成「不分族一律剔」⇒ 本行红");
        Tr(checkedN >= 50, "D0-b **前提**：其余族实检 ≥ 50 条（现读 " + checkedN + "）—— 太少说明 A 段没扫全");
    }

    // ============================================================ E · 闸：待打的这一份**不在手牌里**时不剔
    static void SectionE_GuardWhenPendingNotInHand()
    {
        MenuCheck.Section(_sink, "E 闸：待打的这一份不在手牌里（主动技能 / 英雄议程那一路）⇒ 一律不剔");

        var card = FindById("09bcb1b3b8794c4429d82d4b2e68fa1f");
        Tr(card != null, "E0 **前提**：池里有 `Path of the Seer`");
        if (card == null) return;
        var op = FirstOp(card, "hand");
        if (op == null) { Tr(false, "E0-b **前提**：它的 `choosecard` op 是 `hand` 族"); return; }

        var ctx = MakeCtx(card, Troop("B101E2E0"), Troop("B101E2E1"), Troop("B101E2E2"));
        // 模拟「主动技能那一批」：`_pendingInst` 是**场上**的一份、`_pendingIdx = -1`
        // （`BattleDriver.Resolve` 那一支就是这么写的）⇒ 待打的这张**不在手牌里** ⇒ 手牌没变过
        var boardInst = ctx.NewInstance(card);
        string sn, dt, wy;
        var raw = RuleCore.ChooseCardCandidates(ctx, 0, op, out sn, out dt, out wy);
        var panel = ChoicePanelPool.PanelChoiceCandidates(ctx, 0, op, card, boardInst, -1, out sn, out dt, out wy);

        Tr(ctx.Players[0].Hand.IndexOf(boardInst) < 0, "E1 **前提**：那一份确实不在手牌里（夹具摆对了）");
        C(Ids(panel), Ids(raw), "E2 ★ 不剔（面板池 == 原样池）"
                               + "｜🧨 删掉「这一份此刻真在手牌里」那道闸 ⇒ 会把手牌里**同名的那一张**错剔掉 ⇒ 本行红");
        Tr(HasId(panel, card.Id), "E2-b 手牌里那张同名卡**还在**面板候选里（它没被打出去）");
    }

    // ============================================================ 小工具
    static CardDef FindById(string id)
    {
        var pool = Pool();
        for (int i = 0; i < pool.Count; i++) if (pool[i] != null && pool[i].Id == id) return pool[i];
        return null;
    }

    static EffectOp FirstOp(CardDef card, string src)
    {
        var ops = ChooseOps(card);
        for (int i = 0; i < ops.Count; i++) if (SrcOf(ops[i]) == src) return ops[i];
        return null;
    }
}
