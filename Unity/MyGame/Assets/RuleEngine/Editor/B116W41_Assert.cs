// B116W41_Assert.cs — 执行代理 `B116_W41`（2026-11-02 · 第一百一十六会话 · 引擎线 · 开窗前最后一批离线活）
//   文件所有权 = `RuleEngine/Core/CardPoolCriteriaFilter.cs`（类尾追加）· `Core/ManaBuffDeadBranch_B116W41.cs`（新建）·
//                `Core/PlayerState.cs`（类尾追加）· `Core/RuleCore.cs`（**同一物理行内改**）· 本文件（新建）·
//                `Editor/RuleEngineTest.cs`（**同一物理行追加**）。
//
// ============================ 它盯的是什么 ============================
//  ① 活状态表 `B-340`：`ta=65 inDeckFiltered` 的**抽牌支**（`Draw two troops …` → `EffectResolver.DoDrawType`）
//     要过同一条 36 格 criteria（原版 `AbilityLogic__GetTargets.c:963` 建池 → `:1086-1095` 筛）。
//     A 段钉的就是新口 `CardPoolCriteriaFilter.DeckDrawCandidates`：**不筛** / **筛** / **判不了不筛** /
//     **重复模板按多重集回落** / **顺序 = 牌库顶往下**。
//  ② `ENG-07` `psyker`(620) / `reactor`(625) 的死支（`Core/ManaBuffDeadBranch_B116W41.cs`）：
//     flag **写得进去**（B1/B4）而 `Process` **不消费它**（B2：两个池一个字节都不动）；
//     侧别守卫只扫行动方（B3）；值 `<= 0` 不产（B6）。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//  · `decomp_full/AbilityLogic__GetTargets.c:963`（`0x41 = 65 inDeckFiltered`，池 = `GetCardsInDeck`）
//    ＋ `:1086-1095`（建池后对每个元素一律 `CheckIfMeetsCriteria`）
//  · `decomp_full/CardScript__OnTurnStart.c:177`（psyker）/ `:218`（reactor）/ `:101-103`（侧别守卫）
//  · `decomp_full/BattleManager__AddPsychicMana.c:29-44` · `__AddPlasmaMana.c:29-44`（造 `ManaBuff` 写 flag）
//  · 🔴 `decomp_full/BattleManager__ProcessManaBuff.c:27-49`（**一次都不读** `+0x19`/`+0x1a`）
//  · `判据/B116_R7_ENG07剩余四条施工面.md` §2（整链 + 「87 张」证伪）· `报告/B116_W31_SAN04_27收尾.md` §2（`maxTargets` 语义）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A3：把 criteria 筛去掉（`Apply` 不调）⇒ 候选 3、期望 1 ⇒ 红。
//  · A5：把「判不了 ⇒ 整池原样」改成「判不了 ⇒ 筛空」⇒ 候选 0、期望 3 ⇒ 红。
//  · B1 vs B5/B6：只写 flag、不看闸 ⇒ B5/B6 红；只看闸、不写 flag ⇒ B1 红（两条结构上不能同时满足）。
//  · B2：把 `Process` 改成「加到 psychic 池」⇒ B2 红（**那正是原版没有的行为**）。
//  · B3：去掉侧别守卫（扫双方）⇒ B3 红。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B116_W41`：`B-340`（`ta=65` 抽牌支接 36 格）＋ `ENG-07` `psyker`/`reactor` 死支。</summary>
public static class B116W41_Assert
{
    const string P = "[B116W41] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    static bool Eq(int got, int want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + $"   ✓ {msg}（实际 {got}）"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}｜实际 {got}，期望 {want}"); return false;
    }

    static void Sec(string t) { Debug.Log(P + "── " + t); }

    // ============================================================ 夹具
    static CardDef Hero(string id) => new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0);

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B116W41Warlord0") };
        var d1 = new List<CardDef> { Hero("B116W41Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    /// <summary>一张普通部队（**必须挂 `cardTraits`** —— `MatchesRawCard` 在 `cardTraits` 没喂时判不了，
    /// 那样 A3 那段会落到「不筛」上去，测不到筛选）。</summary>
    static CardDef Troop(string id, int cost = 1)
    {
        var c = new CardDef(id, id, "unit", "", "common", "Test", cost, 2, 5, 0);
        c.AttachCardTraits(new string[0]);
        return c;
    }

    /// <summary>一张战术卡（`type=tactic` ⇒ 文本筛 `troops` **不认**它）。</summary>
    static CardDef Tactic(string id, int cost = 1)
    {
        var c = new CardDef(id, id, "tactic", "", "common", "Test", cost, 0, 0, 0);
        c.AttachCardTraits(new string[0]);
        return c;
    }

    /// <summary>一条**判得出来**的 36 格：只留「卡种白名单 = 只要 unit」＋可选「费用 ≥ N」。
    /// ⚠️ 其余格必须**逐个填 0**（`CountSetSpec` 的整数字段默认是 `Unknown(-1)` ⇒ 留着的格会让
    /// `MatchesRawCard` 判不了、整池原样返回，那样 A3 就成了假绿）。</summary>
    static CardDef.CountSetSpec Set65(int manaMin = 0)
    {
        var s = new CardDef.CountSetSpec();
        s.TargetsAffected = 65;             // inDeckFiltered
        s.PlayerTurn = -1;                  // 卡面级函数不读它（`MatchesRawCard`）
        s.ChosenArmy = 0; s.ArmyFilter = 0;
        s.KeywordFilter = 0; s.ExcludeRace = 0;
        s.TraitsFilter = 0; s.ExcludeTrait = 0;
        s.MaxTargets = 0; s.TargetSelectMethod = 0;
        s.AttackFilter = 0; s.AttackFilterOption = 0;
        s.CurrentHealthFilter = 0; s.CurrentHealthFilterOption = 0;
        s.ManaCostFilter = manaMin; s.ManaCostFilterOption = manaMin > 0 ? 2 : 0;   // 2 = EqualOrHigher
        s.RarityFilter = 0; s.RarityFilterOption = 0;
        s.SpellType = 0;
        s.IncludeHero = 0; s.IncludeMinions = 1; s.IncludeSpells = 0;
        s.IncludeWhispers = 0; s.IncludeWeapons = 0; s.IncludeRemnants = 0; s.IncludeHeroParts = 0;
        s.IncludeSelf = 0; s.ExcludeNotSelf = 0; s.ExcludeSimilarCards = 0; s.ExcludeTargetted = 0;
        s.ExcludeFriendlyHero = 0; s.ExcludeEnemyHero = 0;
        s.SelectFromChooseResolution = 0;
        return s;
    }

    static void Put(BattleContext ctx, int owner, params CardDef[] cards)
    {
        var ps = ctx.Players[owner];
        for (int i = 0; i < cards.Length; i++) ps.Deck.Add(ctx.NewInstance(cards[i]));
    }

    static string Ids(List<CardInstance> list)
    {
        string s = "";
        for (int i = 0; i < list.Count; i++) { if (s.Length > 0) s += ","; s += list[i].Card != null ? list[i].Card.Id : "?"; }
        return s;
    }

    // ============================================================ 入口
    public static void Run()
    {
        Sec("A · `B-340`：`ta=65` 抽牌支的 36 格（`CardPoolCriteriaFilter.DeckDrawCandidates`）");
        ASection();
        Sec("B · `ENG-07` `psyker`(620) / `reactor`(625) 死支（写 flag · 没人读 · 侧别 · 闸）");
        BSection();
    }

    // ------------------------------------------------------------ A
    static void ASection()
    {
        var ctx = NewCtx();
        var t1 = Troop("W41A_c1", cost: 1);
        var t2 = Troop("W41A_c2", cost: 2);
        var t3 = Troop("W41A_c3", cost: 3);
        var sp = Tactic("W41A_sp", cost: 3);
        Put(ctx, 0, t1, t2, t3, sp);                    // 牌库 = [c1,c2,c3,sp] ⇒ **顶（末尾）= sp**
        var deck = ctx.Players[0].Deck;

        Ok(CreatePool.MatchesKind(t1, "troops") && !CreatePool.MatchesKind(sp, "troops"),
           "A0 （前提）文本筛 `troops` 认部队、**不认**战术卡（否则下面几条测的是别的东西）");

        var opNone = new EffectOp { Verb = "drawtype", Source = "W41A", TargetCriteriaSet = null };
        var candNone = CardPoolCriteriaFilter.DeckDrawCandidates(ctx, opNone, deck, "troops");
        Eq(candNone.Count, 3, "A1 ★ 没有 criteria 行（`TargetCriteriaSet == null`）⇒ 只走文本筛（3 张）");
        Ok(candNone.Count == 3 && candNone[0].Card == t3 && candNone[2].Card == t1,
           "A1′ ★ 顺序 = **牌库顶往下**（c3 → c2 → c1）；🧨 反过来（从底往上）⇒ 本条红");

        var opTa10 = new EffectOp { Verb = "drawtype", Source = "W41A", TargetCriteriaSet = Set65() };
        opTa10.TargetCriteriaSet.TargetsAffected = 10;   // 棋盘档 ⇒ 不是卡池档
        var candTa10 = CardPoolCriteriaFilter.DeckDrawCandidates(ctx, opTa10, deck, "troops");
        Eq(candTa10.Count, 3, "A2 ★ `ta=10`（棋盘档）⇒ **不筛**（`IsCardPoolTargetsAffected` 为假 ⇒ 原样返回）");

        var opCost = new EffectOp { Verb = "drawtype", Source = "W41A", TargetCriteriaSet = Set65(manaMin: 2) };
        var candCost = CardPoolCriteriaFilter.DeckDrawCandidates(ctx, opCost, deck, "troops");
        Eq(candCost.Count, 2, "A3 ★★ `ta=65` + 费用 ≥2 ⇒ 只留 c3/c2（原版同一句 `CheckIfMeetsCriteria`）"
                            + "｜🧨 不调 `Apply`（= 改前行为）⇒ 实得 3、本条红");
        Ok(candCost.Count == 2 && candCost[0].Card == t3 && candCost[1].Card == t2,
           "A3′ ★ 留下的仍是**牌库顶往下**那两张（筛选只删、不重排）");

        // A4：同一模板两份 ⇒ 按**多重集**回落（同名两张只留被留下的那几张，⛔ 不按引用去重成一张）
        var ctx2 = NewCtx();
        var dup = Troop("W41A_dup", cost: 1);
        var hi = Troop("W41A_hi", cost: 3);
        Put(ctx2, 0, dup, hi, dup);                     // 顶 = dup(2) → hi → dup(1)
        var opA4 = new EffectOp { Verb = "drawtype", Source = "W41A", TargetCriteriaSet = Set65(manaMin: 3) };
        var candA4 = CardPoolCriteriaFilter.DeckDrawCandidates(ctx2, opA4, ctx2.Players[0].Deck, "troops");
        Eq(candA4.Count, 1, "A4 ★ 同名两张都被筛掉时只留 1 张高费的（多重集回落，⛔ 不把同模板认成一张）");

        var ctx3 = NewCtx();
        Put(ctx3, 0, Troop("W41A_x", cost: 9), Troop("W41A_y", cost: 9));
        var opA4b = new EffectOp { Verb = "drawtype", Source = "W41A", TargetCriteriaSet = Set65(manaMin: 2) };
        var candA4b = CardPoolCriteriaFilter.DeckDrawCandidates(ctx3, opA4b, ctx3.Players[0].Deck, "troops");
        Eq(candA4b.Count, 2, "A4′ ★ 两张**都过筛**时一张都不少（重复保留 = 原版「逐份投影、保留重复」）");

        // A5：某一格判不了 ⇒ **整池原样返回**（⛔ 不当「不成立」筛空）
        var opA5 = new EffectOp { Verb = "drawtype", Source = "W41A", TargetCriteriaSet = Set65(manaMin: 2) };
        opA5.TargetCriteriaSet.ManaCostFilterOption = CardDef.CountSetSpec.Unknown;   // ⇒ `MatchesRawCard` 判不了
        var candA5 = CardPoolCriteriaFilter.DeckDrawCandidates(ctx, opA5, deck, "troops");
        Eq(candA5.Count, 3, "A5 ★★ 判不了（`manaCostFilterOption` 未知）⇒ **整池原样**（不是筛空）"
                          + "｜🧨 把「判不了」当成「不成立」⇒ 实得 0、本条红");

        // A6：空/坏输入不炸
        Ok(CardPoolCriteriaFilter.DeckDrawCandidates(ctx, opNone, null, "troops").Count == 0
           && CardPoolCriteriaFilter.DeckDrawCandidates(null, opNone, deck, "troops").Count == 3,
           "A6 `deck == null` ⇒ 空表；`ctx == null` 也能跑（生产侧永远给 ctx，这里只防 NRE）");
    }

    // ------------------------------------------------------------ B
    static void BSection()
    {
        var ctx = NewCtx();
        var ps = ctx.Players[0];
        var psy = new CardDef("W41B_psyker", "W41B Psyker", "unit", "", "common", "Test", 2, 2, 5, 0);
        psy.AttachCardTraits(new[] { "psyker 2" });
        var unit = new UnitState(psy, false);
        ps.Board[2] = unit;

        Ok(psy.HasDefaultTrait("psyker", out _) && psy.GetDefaultTraitValue("psyker", out _) == 2,
           "B0 （前提）夹具的 `cardTraits` 真的带上了 `psyker 2`（否则下面几条是无意义的空转）");
        Eq(ManaBuffProcess.CurrentTraitValueOf(unit, ManaBuffProcess.Psyker), 2,
           "B0′ `CurrentTraitValueOf` 读得出 2（= 原版 `get_CurrentPsyker`）");

        var made = ManaBuffProcess.AddPsychicReactor(ctx, 0);
        Eq(made.Count, 1, "B1 ★★ 行动方棋盘上一张 `psyker 2` ⇒ 造出 **1** 条 `ManaBuff`（原版 `:177`）");
        Ok(made.Count == 1 && made[0].PsychicMana && !made[0].PlasmaMana && made[0].ManaAmount == 2
           && made[0].FriendlyPlayer,
           "B1′ ★ 那条 buff 的 flag **真的写上了**：`psychicMana=1` / `plasmaMana=0` / `manaAmount=2`"
         + "｜🧨 只建对象不写 flag ⇒ 本条红");

        Ok(ps.PsychicMana == 0 && ps.PlasmaMana == 0,
           "B2 ★★ `Process` **不消费**那两个 flag ⇒ 两个池一个字节都不动（原版 `ProcessManaBuff.c:27-49`）"
         + "｜🧨 把 flag 接到池上（「+N 灵能」）⇒ 本条红 —— **那正是原版没有的行为**");

        Eq(ManaBuffProcess.AddPsychicReactor(ctx, 1).Count, 0,
           "B3 ★★ 侧别守卫：同一条链对**对手**跑 ⇒ 0 条（原版 `OnTurnStart.c:101-103`）"
         + "｜🧨 改成扫双方 ⇒ 本条红");

        var ctx2 = NewCtx();
        var rea = new CardDef("W41B_reactor", "W41B Reactor", "unit", "", "common", "Test", 2, 2, 5, 0);
        rea.AttachCardTraits(new[] { "reactor 3" });
        ctx2.Players[0].Board[3] = new UnitState(rea, false);
        var madeB4 = ManaBuffProcess.AddPsychicReactor(ctx2, 0);
        Ok(madeB4.Count == 1 && madeB4[0].PlasmaMana && !madeB4[0].PsychicMana && madeB4[0].ManaAmount == 3,
           "B4 ★ `reactor 3` ⇒ `plasmaMana=1`、`psychicMana=0`、值 3（两种 flag **不混**）");

        var ctx3 = NewCtx();
        ctx3.Players[0].Board[4] = new UnitState(Troop("W41B_plain"), false);
        Eq(ManaBuffProcess.AddPsychicReactor(ctx3, 0).Count, 0,
           "B5 ★ 没有这两个 trait ⇒ **0 条**（⛔ 不无条件造）");

        var ctx4 = NewCtx();
        var zero = new CardDef("W41B_zero", "W41B Zero", "unit", "", "common", "Test", 2, 2, 5, 0);
        zero.AttachCardTraits(new[] { "psyker" });     // 没印数字 ⇒ 值 0
        ctx4.Players[0].Board[5] = new UnitState(zero, false);
        Eq(ManaBuffProcess.AddPsychicReactor(ctx4, 0).Count, 0,
           "B6 ★ 值 `<= 0` ⇒ 不产（原版闸 `get_CurrentPsyker() > 0`）");

        // B7：Process 撞上「另外五格」时**拒绝**（那些格的实作在既有落点，⛔ 不在这儿复制第二份）
        Ok(!ManaBuffProcess.Process(ctx, 0, new ManaBuff { ManaAmount = 1, CreateMana = true }),
           "B7 `createMana` 那一档 ⇒ `Process` 返回 false（⛔ 不在这里造第二个 `DoEnergy`）");
        Ok(ManaBuffProcess.Process(ctx, 0, new ManaBuff { ManaAmount = 1, PsychicMana = true }),
           "B7′ 只有 psychic flag ⇒ `Process` 正常走默认支（空转）并返回 true");

        // B8：`AbilityUsesPsychicEnergy`（bool，来自 `activeAbility.psychicCost`）恒 false 时，
        //     主动能力付费闸**行为不变**（这条死支没有给它开口子）。⛔ 与 `ActiveAbilityCurrencyGate` 分开测
        //     —— 那是**另一条**链（`RuleCore.cs:17716`），本笔没动它。
        var plain = new CardDef("W41B_plainCost", "W41B Plain", "unit", "", "common", "Test", 2, 2, 5, 0);
        plain.AttachActiveAbilityCost(manaCost: 2, psychicCost: 0, plasmaCost: 0, targeted: 0);
        var plainU = new UnitState(plain, false);
        Eq(RuleCore.ActiveAbilityCurrencyGateForTest(ctx, 0, plainU), RuleCodes.OK,
           "B8 ★★ 卡表标 0（= `AbilityUsesPsychicEnergy` 假）⇒ 付费闸放行（**行为不变**）");
        var psyCost = new CardDef("W41B_psyCost", "W41B PsyCost", "unit", "", "common", "Test", 2, 2, 5, 0);
        psyCost.AttachActiveAbilityCost(manaCost: 2, psychicCost: 1, plasmaCost: 0, targeted: 0);
        Eq(RuleCore.ActiveAbilityCurrencyGateForTest(ctx, 0, new UnitState(psyCost, false)), RuleCodes.ErrCost,
           "B8′ ★ 一旦真标了 psychic 币种 ⇒ 闸**拒绝**（⛔ 不因为本笔建了池就静默放行）"
         + "｜🧨 拿本笔新建的池去放行 ⇒ 本条红");
    }
}
