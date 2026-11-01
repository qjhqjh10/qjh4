// B116W31_Assert.cs — 执行代理 `B116_W31`（2026-11-02 · 第一百一十六会话 · 引擎线）
//   文件所有权 = `RuleEngine/Core/RuleCore.cs`（**类尾追加 + 同一物理行内改**）· 本文件（新建）·
//                `Editor/RuleEngineTest.cs`（**同一物理行追加**）。
//
// ============================ 它盯的是什么 ============================
//  ① 活状态表 `SAN-04` 残余：`ta = 150` 的**手牌半**（实现已由 `B115_W23` / `B115_W21` 落地，
//     **这一半从来没有断言**）。
//     · 效果层：`EffectResolver.ResolvePreviousAbilityTargets` 对**手牌候选**要过
//       `CheckIfMeetsCriteria`（`CountEval.CardPoolCandidatePasses`），`ta = 140` **不过筛**；
//     · 表现层：`RuleCore.AbilityTargetListFromCriteria` 要把手牌候选以 **`Slot = -1`** 四元组输出，
//       而 `publishTable`（效果层吃的那张表）**只收棋盘项**。
//  ② 活状态表 `SAN-27` 的 7 行结论（`ta=65` = 牌库池 / `ta=100` = 相邻 / `ta=0` 被数据筛空）
//     —— 本文件只把**判据侧**能机检的那一段钉住（`ta=100` 的 36 格判法 + `ta=0` 的空池语义），
//     其余写在报告里（零可达 + 为什么）。
//  ③ 活状态表 `B-333`：`UnimplementedKeywords` 必须**走**表现层关键词白名单（与卡面打星同一口径）。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//  · `decomp_full/AbilityLogic__GetTargets.c:867-883`（140/150 都 `AddRange` 同一张静态表）
//  · `decomp_full/FilterMethods__CheckIfMeetsCriteria.c:172-183`（150：Contains 不中就 0，中了**继续走常规格**）
//    vs `:495-510`（140：`return List.Contains(...)` —— **不落**常规格）
//  · `decomp_full/AbilityLogic__GetTargets.c:1000-1008`（`ta=100`：池 = `GetAdjacentUnits(thisCard)`）
//  · `dump.cs:20777-20819`（`TargetsAffected` 枚举：`65 inDeckFiltered` / `100 adjacentToSelfIfMeetsCriteria` / `0 any`）
//  · `报告/B115_W23_卡级目标W1整批.md` §1（实现落点）· `报告/B115_W21_卡级目标P1.md` §4（为什么表现层缺读数口）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A 段：把 `case 140` 也写成 `break`（落常规格）⇒ A3 红；只保留 `ta=150` 的 Contains 就 `return true`
//    ⇒ A4 红。**A3/A4 只差 `ta` 一个字段 ⇒ 两种错实现不可能同时满足这一对。**
//  · B 段：手牌候选不过筛 ⇒ B1/B2 实得同号 ⇒ B1 红；`ta=140` 也过筛 ⇒ B5 红；
//    「判不到 criteria 行就筛空」⇒ B7 红（期望 1）。
//  · C 段：手牌候选不输出 ⇒ C1 实得 1 ⇒ 红；输出但 `Slot` 写成格号 ⇒ C2 红；
//    `publishTable` 把 `Slot=-1` 也塞进去 ⇒ C4 红。
//  · D 段：白名单不接进 `UnimplementedKeywords` ⇒ D3 红；恒返回空表 ⇒ D4 红（假词必须还在）。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B116_W31`：`SAN-04`（`ta=150` 手牌半的断言）· `SAN-27`（7 行的判据侧）· `B-333`（报表口径）。</summary>
public static class B116W31_Assert
{
    const string P = "[B116W31] ";

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
        var d0 = new List<CardDef> { Hero("B116W31Warlord0") };
        var d1 = new List<CardDef> { Hero("B116W31Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    /// <summary>一张普通部队（**必须挂 `cardTraits`** —— 手牌候选要走 `MatchesRawCard`，那里
    /// 「`cardTraits` 没喂 ⇒ 判不了」⇒ 不挂就测不到「筛掉」那一支）。</summary>
    static CardDef Troop(string id)
    {
        var c = new CardDef(id, id, "unit", "", "common", "Test", 1, 2, 5, 0);
        c.AttachCardTraits(new string[0]);
        return c;
    }

    /// <summary>往卡的关键词表里**强写**一格（见 §D 里那段「为什么用强制转换」）。
    /// 底层对象就是构造里建的那个 `Dictionary&lt;string,int&gt;`（`Keywords` getter 直接返回它）。</summary>
    static CardDef WithKeyword(CardDef c, string kw)
    {
        var d = c.Keywords as Dictionary<string, int>;
        if (d != null) d[kw] = 1;
        return c;
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        // ⚠️ `Exhausted = false` 必须显式写：`UnitState` 的 ctor 恒 `true`（同 `B102R1_Assert.Place`）。
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>`ta` 那一行 36 格。
    /// <para>🔴 **每一格都必须显式写**：`CountSetSpec` 的整数字段默认值是 `Unknown(-1)`（**不是 0**，
    /// 见 `CardDef.CountSetSpec` 的字段声明）—— 漏写一格就等于「这一格没落 ⇒ 判不了 ⇒ `Matches` 返 `null`」。
    /// 本夹具的口径 = 「除了判别式那一格，其余都填成**不筛**」。`0` 在各维上确实都是「不筛」
    /// （`StatNotApplicable / traitsFilter == 0 / armyFilter == 0 / keywordFilter == 0`）。</para></summary>
    static CardDef.CountSetSpec Set(int ta, int excludeNotSelf)
    {
        return new CardDef.CountSetSpec
        {
            TargetsAffected = ta,
            PlayerTurn = 0,                     // `Both`（与 `ta` 正交；`Matches` 不读它）
            ChosenArmy = 0,
            ArmyFilter = 0,
            KeywordFilter = 0,
            TraitsFilter = 0,
            MaxTargets = 0,                     // `<= 0` = 不截断
            TargetSelectMethod = 0,
            AttackFilter = 0,
            AttackFilterOption = 0,
            CurrentHealthFilter = 0,
            CurrentHealthFilterOption = 0,
            ManaCostFilter = 0,
            ManaCostFilterOption = 0,
            RarityFilter = 0,
            RarityFilterOption = 0,
            SpellType = 0,                      // `Normal`；`Matches` 只在 `!= 0` 时才比
            FriendlyUnits = 1,                  // 候选在**我方**棋盘 ⇒ 走这一格
            EnemyUnits = 0,
            IncludeHero = 0,
            IncludeMinions = 1,                 // 我们这一批夹具全是 `type = unit`
            IncludeSpells = 0,
            IncludeWhispers = 0,
            IncludeWeapons = 0,
            IncludeRemnants = 0,
            IncludeHeroParts = 0,
            IncludeSelf = 1,                    // 候选==thisCard 时**不**因为这一格被淘汰
            ExcludeNotSelf = excludeNotSelf,    // 1 = 「不是 thisCard 就淘汰」= 判别式
            ExcludeSimilarCards = 0,
            ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0,
            ExcludeEnemyHero = 0,
            ExcludeRace = 0,
            ExcludeTrait = 0,
            SelectFromChooseResolution = 0,
        };
    }

    /// <summary>造一张「`cardAbilities[0].abilityLogic[0]` 带一行 `targetCriteria`」的部队。</summary>
    static CardDef CritCard(string id, CardDef.CountSetSpec set)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 2, 5, 0,
            abilityTargetCriteria: new[] { new CardDef.TargetCriteriaRowSpec { Ability = 0, Trigger = 0, Logic = 0, Criteria = set } },
            abilityTargetCriteriaVersioned: true);
    }

    static EffectOp Op150() => new EffectOp { Verb = "give", Source = "B116W31", AbilityIndex = 0, LogicIndex = 0, SubjectTargetsAffected = 150 };
    static EffectOp Op140() => new EffectOp { Verb = "give", Source = "B116W31", AbilityIndex = 0, LogicIndex = 0, SubjectTargetsAffected = 140 };

    // ============================================================ 入口
    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            SectionA();
            SectionB();
            SectionC();
            SectionD();
        }
        catch (System.Exception e)
        {
            Fail++; Debug.LogError(P + "   ✗ 异常（后面的都没跑）: " + e);
        }
        Debug.Log(P + $"合计 {Pass} 通过 / {Fail} 失败");
    }

    // ------------------------------------------------------------ A · 140/150 分叉
    static void SectionA()
    {
        Sec("A · `FetchMethods.CheckIfMeetsCriteria` 的 140/150 分叉（判据 `:172-183` vs `:495-510`）");

        var ctx = NewCtx();
        int me = ctx.Active;
        var src = Place(ctx, me, 3, Troop("B116W31A_src"));
        var candidate = Place(ctx, me, 5, Troop("B116W31A_cand"));
        Ok(src != null && candidate != null, "（前提）两侧各摆好一张部队（`source` 在 3、候选在 5）");

        var set150Pass = Set(150, 0);
        var set150Fail = Set(150, 1);
        var set140Fail = Set(140, 1);
        string gap;

        ctx.PreviousAbilityTargets.Clear();
        ctx.PreviousAbilityTargets.Add(CardRef.Of(candidate));

        Ok(TargetCriteriaEval.Matches(ctx, me, set150Pass, candidate, null, src, null, out gap) == true,
           "A1 ★ `ta=150` + 候选**在表里** + 常规格全过 ⇒ **true**（实际 gap=" + (gap ?? "无") + "）"
         + "｜🧨 把 150 写成「Contains 中就直接 true」在本条看不出来 —— 靠 A3 挡");

        ctx.PreviousAbilityTargets.Clear();
        Ok(TargetCriteriaEval.Matches(ctx, me, set150Pass, candidate, null, src, null, out gap) == false,
           "A2 ★ `ta=150` + 候选**不在表里** ⇒ **false**（`Contains` 那一道闸；判据 `:172-183` 的 `cVar2 == '\\0' ⇒ return 0`）"
         + "｜🧨 删掉 Contains 闸 / 恒过 ⇒ 实得 true ⇒ 红");

        ctx.PreviousAbilityTargets.Clear();
        ctx.PreviousAbilityTargets.Add(CardRef.Of(candidate));
        Ok(TargetCriteriaEval.Matches(ctx, me, set150Fail, candidate, null, src, null, out gap) == false,
           "A3 ★★ `ta=150` + 候选**在表里**但**常规格不过**（`excludeNotSelf=1` 且候选≠thisCard）⇒ **false**"
         + "（= 中了 Contains 之后**继续落常规 36 格**，判据 `:184-420`）"
         + "｜🧨 把 `case 150` 改成 `return PrevTargetsContains(...)` ⇒ 实得 true ⇒ 红");

        Ok(TargetCriteriaEval.Matches(ctx, me, set140Fail, candidate, null, src, null, out gap) == true,
           "A4 ★★ `ta=140` + **同一份** 36 格 ⇒ **true**（140 只做 `List.Contains` 就 `return`，**不落**常规格；判据 `:495-510`）"
         + "｜🧨 把 `case 140` 也写成 `break`（落常规格）⇒ 实得 false ⇒ 红。⚠️ 与 A3 **只差 `ta` 一个字段** ⇒ 两种错实现不可能同时满足这一对");

        ctx.PreviousAbilityTargets.Clear();
        Ok(TargetCriteriaEval.Matches(ctx, me, set140Fail, candidate, null, src, null, out gap) == false,
           "A5 ★ `ta=140` + 候选**不在表里** ⇒ **false**（140 也不是「恒真」—— 它仍然要过 `Contains`）");

        // SAN-27 的判据侧：`ta=100` 的常规格尾巴（`candidate` 是不是 thisCard 的相邻）
        // ⚠️ 相邻是**按棋盘侧内的稠密序**判的（`CandidateAdjacentTo` / `BoardSlots.SideOf`），不是裸格号 `slot±1`：
        //    `src` 在 3（左侧、贴督军那一格 ⇒ 稠密下标 0）⇒ 它的邻居 = **2 号格** 与 **督军(4)**，⛔ 不是 5 号格。
        ctx.PreviousAbilityTargets.Clear();
        var far = Place(ctx, me, 2, Troop("B116W31A_far"));
        Ok(TargetCriteriaEval.Matches(ctx, me, Set(100, 0), far, null, src, null, out gap) == true,
           "A6 ★ `ta=100 adjacentToSelfIfMeetsCriteria`：候选**就在 thisCard 相邻**（`src`@3 ⇒ 邻居 2/督军，取 2）⇒ **true**"
         + "（判据 `CheckIfMeetsCriteria.c:411-425` + `GetTargets.c:1000-1008`；`UM15` 那一行的目标集就是这一档）");
        var notAdj = Place(ctx, me, 0, Troop("B116W31A_notadj"));
        Ok(TargetCriteriaEval.Matches(ctx, me, Set(100, 0), notAdj, null, src, null, out gap) == false,
           "A7 ★ `ta=100`：候选**不在** thisCard 相邻（0 号格，同一侧但隔了两格）⇒ **false**"
         + "｜🧨 把 100 当成「只走常规格」不判相邻 ⇒ 实得 true ⇒ 红");

        // SAN-27 的判据侧：`ta=0 any` —— 36 格**一律不参与**
        Ok(TargetCriteriaEval.Matches(ctx, me, Set(0, 0), far, null, src, null, out gap) == true,
           "A8 ★（前提）`ta=0 any` + 一份**放行**的 36 格 ⇒ true");
        var emptySet = Set(0, 0);
        emptySet.IncludeMinions = 0;          // `UM45 Gather the Company` 那两行的原样：include* 全是 0
        emptySet.IncludeHero = 0;
        emptySet.ExcludeNotSelf = 1;          // 再叠一条「本该淘汰」的格，证明它也不看
        Ok(TargetCriteriaEval.Matches(ctx, me, emptySet, far, null, src, null, out gap) == true,
           "A9 ★★ `ta=0` ⇒ **36 格一格都不看**（原版 `CheckIfMeetsCriteria.c:56-59` 直接 `return 1`，`any` = 「disregards all other filters」）"
         + "⇒ `UM45` 那两行 `maxTargets=3` 之所以**选不出人**，是因为**建池那一侧**（原版 `GetUnitsInPlayList` 的 include 位全 0）"
         + "给了一张空池，⛔ **不是**这 36 格筛空的 —— 这一点必须分清，否则会去改错地方"
         + "｜🧨 把 `case 0` 改成落常规格 ⇒ 实得 false ⇒ 红");
    }

    // ------------------------------------------------------------ B · 效果层：手牌候选要过筛 / 140 不过筛
    static void SectionB()
    {
        Sec("B · `ResolvePreviousAbilityTargets` 的**手牌候选**（`SAN-04` ①的效果层半）");

        // ---- B1/B2：同一张候选、只差 criteria 里 `excludeNotSelf` 一格 ----
        var ctx = NewCtx();
        int me = ctx.Active;
        var srcFail = Place(ctx, me, 3, CritCard("B116W31B_srcFail", Set(150, 1)));
        var hand = ctx.NewInstance(Troop("B116W31B_hand"));
        ctx.Players[me].Hand.Clear();
        ctx.Players[me].Hand.Add(hand);

        ctx.PreviousAbilityTargets.Clear();
        ctx.PreviousAbilityTargets.Add(CardRef.Of(hand));
        var refs1 = new List<CardRef>();
        var got1 = RuleCore.ResolveTargets(ctx, me, null, srcFail, null, null, Op150(), null, refs1);
        Eq(got1.Count, 0, "B1 ★ `ta=150`：棋盘目标是 0（候选在手里，`ta=150` 绕开文本 spec）");
        Eq(refs1.Count, 0,
           "B1′ ★★ **手牌候选过不了 36 格 ⇒ 从 `refsOut` 里被筛掉**（`CountEval.CardPoolCandidatePasses` 的 `excludeNotSelf` 那一档）"
         + "｜🧨 手牌候选统统不过筛（只 `refsOut.Add`）⇒ 实得 1 ⇒ 红");

        var ctx2 = NewCtx();
        int me2 = ctx2.Active;
        var srcPass = Place(ctx2, me2, 3, CritCard("B116W31B_srcPass", Set(150, 0)));
        var hand2 = ctx2.NewInstance(Troop("B116W31B_hand2"));
        ctx2.Players[me2].Hand.Clear();
        ctx2.Players[me2].Hand.Add(hand2);
        ctx2.PreviousAbilityTargets.Clear();
        ctx2.PreviousAbilityTargets.Add(CardRef.Of(hand2));
        var refs2 = new List<CardRef>();
        RuleCore.ResolveTargets(ctx2, me2, null, srcPass, null, null, Op150(), null, refs2);
        Eq(refs2.Count, 1,
           "B2 ★★ 只把 `excludeNotSelf` 改成 0（其余逐字相同）⇒ 手牌候选**留下**"
         + "｜🧨 手牌那一支恒淘汰 ⇒ 实得 0 ⇒ 红。**B1/B2 只差一格 = 这一对把「筛真的跑了」钉死**");
        Ok(refs2.Count == 1 && ReferenceEquals(refs2[0].Inst, hand2),
           "B2′ ★ `refsOut` 里装的是**那一份手牌实例**（不是按名字/模板配的）");

        // ---- B3：判不了 / 不在任一方手牌 ⇒ **保留**（⛔ 不静默淘汰）----
        var ctx3 = NewCtx();
        int me3 = ctx3.Active;
        var srcFail3 = Place(ctx3, me3, 3, CritCard("B116W31B_srcFail3", Set(150, 1)));
        var stray = ctx3.NewInstance(Troop("B116W31B_stray"));   // ⛔ 不进任何一方 Hand
        ctx3.PreviousAbilityTargets.Clear();
        ctx3.PreviousAbilityTargets.Add(CardRef.Of(stray));
        var refs3 = new List<CardRef>();
        RuleCore.ResolveTargets(ctx3, me3, null, srcFail3, null, null, Op150(), null, refs3);
        Eq(refs3.Count, 1,
           "B3 ★★ 候选**不在任一方手牌**（已离场 / 牌库 / 墓地）⇒ **不过筛、原样保留**"
         + "（原版 `CardScript` 手牌与场上同型，池 = 那张静态表；⛔ 不许把「不在棋盘」当「不存在」）"
         + "｜🧨 `seat < 0` 时直接 `continue` 丢掉 ⇒ 实得 0 ⇒ 红");

        // ---- B4/B5：`ta=140` 不过筛（与 B1 同一份 criteria，只差 ta）----
        var ctx4 = NewCtx();
        int me4 = ctx4.Active;
        var srcFail4 = Place(ctx4, me4, 3, CritCard("B116W31B_srcFail4", Set(140, 1)));
        var hand4 = ctx4.NewInstance(Troop("B116W31B_hand4"));
        ctx4.Players[me4].Hand.Clear();
        ctx4.Players[me4].Hand.Add(hand4);
        ctx4.PreviousAbilityTargets.Clear();
        ctx4.PreviousAbilityTargets.Add(CardRef.Of(hand4));
        var refs4 = new List<CardRef>();
        RuleCore.ResolveTargets(ctx4, me4, null, srcFail4, null, null, Op140(), null, refs4);
        Eq(refs4.Count, 1,
           "B4 ★★ `ta=140` + **同一份**会淘汰的 criteria ⇒ **仍然保留**（140 不过筛；判据 `GetTargets.c:867-883` 的 0x8c 支）"
         + "｜🧨 140 也走筛 ⇒ 实得 0 ⇒ 红。⚠️ 与 B1′ **只差 `ta` 一个字段**");

        // ---- B5/B6：棋盘候选同样要过筛（对照：证明 B1 的 0 不是「手牌根本进不来」）----
        var ctx5 = NewCtx();
        int me5 = ctx5.Active;
        var src5 = Place(ctx5, me5, 3, CritCard("B116W31B_src5", Set(150, 1)));
        var buddy = Place(ctx5, me5, 5, Troop("B116W31B_buddy"));
        ctx5.PreviousAbilityTargets.Clear();
        ctx5.PreviousAbilityTargets.Add(CardRef.Of(buddy));
        var refs5 = new List<CardRef>();
        var got5 = RuleCore.ResolveTargets(ctx5, me5, null, src5, null, null, Op150(), null, refs5);
        Eq(got5.Count, 0, "B5 ★ 棋盘候选过不了 36 格（`excludeNotSelf=1` 且候选≠thisCard）⇒ 棋盘目标 0");
        Eq(refs5.Count, 0, "B5′ ★ 同上，`refsOut` 里也没有它（棋盘项在收口那层折进 `refsOut`）");

        var ctx6 = NewCtx();
        int me6 = ctx6.Active;
        var src6 = Place(ctx6, me6, 3, CritCard("B116W31B_src6", Set(150, 0)));
        var buddy6 = Place(ctx6, me6, 5, Troop("B116W31B_buddy6"));
        ctx6.PreviousAbilityTargets.Clear();
        ctx6.PreviousAbilityTargets.Add(CardRef.Of(buddy6));
        var refs6 = new List<CardRef>();
        var got6 = RuleCore.ResolveTargets(ctx6, me6, null, src6, null, null, Op150(), null, refs6);
        Eq(got6.Count, 1, "B6 ★ 放行的那一份 ⇒ 棋盘目标 1（B5/B6 只差一格 ⇒ 证明筛真的跑了）");
        Ok(refs6.Count >= 1 && ReferenceEquals(refs6[0].Unit ?? refs6[0].ResolveUnit(ctx6), buddy6),
           "B6′ ★ `refsOut[0]` 就是那张棋盘单位");

        // ---- B7：读不到 criteria 行 ⇒ **不过筛**、照收（⛔ 不是「筛空」）----
        var ctx7 = NewCtx();
        int me7 = ctx7.Active;
        var plain = Place(ctx7, me7, 3, Troop("B116W31B_plain"));   // 没有任何 abilityTargetCriteria 行
        var hand7 = ctx7.NewInstance(Troop("B116W31B_hand7"));
        ctx7.Players[me7].Hand.Clear();
        ctx7.Players[me7].Hand.Add(hand7);
        ctx7.PreviousAbilityTargets.Clear();
        ctx7.PreviousAbilityTargets.Add(CardRef.Of(hand7));
        var refs7 = new List<CardRef>();
        RuleCore.ResolveTargets(ctx7, me7, null, plain, null, null, Op150(), null, refs7);
        Eq(refs7.Count, 1,
           "B7 ★ `ta=150` 但这一行**读不到 `targetCriteria`** ⇒ **不过筛**、候选照收（出声，⛔ 不拿目标短语反推）"
         + "｜🧨 把「判不了」当「不成立」筛空 ⇒ 实得 0 ⇒ 红");
    }

    // ------------------------------------------------------------ C · 表现层的表（`Slot = -1`）
    static void SectionC()
    {
        Sec("C · `AbilityTargetListFromCriteria` 的 `Slot = -1`（`SAN-04` ①的表现层半）");

        var ctx = NewCtx();
        int me = ctx.Active;
        var src = Place(ctx, me, 3, Troop("B116W31C_src"));
        var board = Place(ctx, me, 5, Troop("B116W31C_board"));
        var hand = ctx.NewInstance(Troop("B116W31C_hand"));
        ctx.Players[me].Hand.Clear();
        ctx.Players[me].Hand.Add(hand);

        ctx.PreviousAbilityTargets.Clear();
        ctx.PreviousAbilityTargets.Add(CardRef.Of(board));
        ctx.PreviousAbilityTargets.Add(CardRef.Of(hand));

        ctx.AbilityTargetTable = null;
        var list = RuleCore.AbilityTargetListFromCriteriaForTest(ctx, me, Set(150, 0), src, null, publishTable: false);
        Ok(list != null, "C0 **前提**：`ta=150` 在生产侧的档位集合内（返回非 `null`；`null` = 这一档没接、调用方退回单数路）");
        if (list == null) return;
        Eq(list.Count, 2, "C1 ★★ 表里 = **棋盘 1 + 手牌 1**（手牌候选不再被丢掉）"
                        + "｜🧨 `r.Unit == null` 就 `continue` ⇒ 实得 1 ⇒ 红");

        int iHand = -1, iBoard = -1;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Slot < 0) iHand = i; else iBoard = i;
        }
        Ok(iHand >= 0, "C2 ★★ 手牌那一条的 `Slot` = **-1**（`BattleEvent.cs:877-885` 的四元组：`-1` = 不在棋盘上）"
                     + "｜🧨 写成格号 / 写成 0 ⇒ 本条与 C3 一起红");
        Ok(iHand >= 0 && list[iHand].Player == me && list[iHand].InstanceId == hand.Id,
           "C2′ ★★ 手牌那一条的 `Player` = 持有者座位、`InstanceId` = 那张**实例号**（表现层按实例号认视图）");
        Ok(iBoard >= 0 && list[iBoard].Player == me && list[iBoard].Slot == 5 && list[iBoard].InstanceId == board.InstanceId,
           "C3 ★ 棋盘那一条不变：`Player/Slot/InstanceId` 仍是格号那套（`Slot >= 0`）");

        ctx.AbilityTargetTable = null;
        RuleCore.AbilityTargetListFromCriteriaForTest(ctx, me, Set(150, 0), src, null, publishTable: true);
        var table = ctx.AbilityTargetTable;
        Eq(table != null ? table.Count : 0, 1,
           "C4 ★★ `publishTable: true`（效果层吃的那张表）**只收棋盘项** —— 手牌项不进"
         + "（效果层 `ResolveTargets` 只收 `UnitState`；⛔ `Slot=-1` 塞进去会在下游变成「空位」）"
         + "｜🧨 把 `handRefs` 也塞进 `tableUnits` ⇒ 实得 2 ⇒ 红");
        Ok(table != null && table.Count == 1 && ReferenceEquals(table[0], board),
           "C4′ ★ 而且那一项就是棋盘上那张（不是手牌那张的替身）");
    }

    // ------------------------------------------------------------ D · B-333 报表口径
    static void SectionD()
    {
        Sec("D · `B-333`：`UnimplementedKeywords` 走表现层白名单（与卡面打星同一口径）");

        var wl = RuleCore.PresentationOnlyKeywords;
        Eq(wl != null ? wl.Length : 0, 1, "D1 ★ 白名单今天只有 1 项（池内唯一的表现层关键词）");
        Ok(wl != null && wl.Length == 1 && wl[0] == "massive", "D1′ ★ 那一项 = `massive`（`BattleDriver.MassiveKeyword`）");

        Ok(RuleCore.IsPresentationOnlyKeyword("massive") && RuleCore.IsPresentationOnlyKeyword("Massive")
           && RuleCore.IsPresentationOnlyKeyword("  MASsive  "),
           "D2 ★ 读口归一化：大小写 / 首尾空白都认（与 `BattleDriver.IsPresentationOnlyKeyword` 同一口径）");
        Ok(!RuleCore.IsPresentationOnlyKeyword(null) && !RuleCore.IsPresentationOnlyKeyword("")
           && !RuleCore.IsPresentationOnlyKeyword("flying"),
           "D2′ ★ `null` / 空串 / 别的词 ⇒ false（⛔ 不把空串当成一个词）");

        // ⚠️ **夹具为什么用强制转换**：`CardDef.Keywords` 只暴露 `IReadOnlyDictionary`，而
        //   `massive` **还没登记进 `KeywordTable.Prefixes`**（`ENG-07` 的残项）⇒ 由构造函数喂进来的
        //   关键词会被 `KeywordTable.Normalize` 的前缀匹配丢掉，**池内 0 张卡能自然带出它**。
        //   底层对象就是构造函数建的那个 `Dictionary<string,int>`（`Keywords` getter 直接返回 `_keywords`）
        //   ⇒ 转回 `Dictionary` 写一格，造出「将来一有卡就会静默错标」的那个状态。
        var massive = WithKeyword(Troop("B116W31D_massive"), "massive");
        var bogus = WithKeyword(Troop("B116W31D_bogus"), "b116w31bogus");
        Ok(massive.Keywords.ContainsKey("massive") && bogus.Keywords.ContainsKey("b116w31bogus"),
           "（前提）两张夹具的关键词**真的进表了**（否则 D3/D4 是无意义的空转）");
        var un = RuleCore.UnimplementedKeywords(new[] { massive, bogus });
        Ok(!un.Contains("massive"),
           "D3 ★★ 带 `massive` 的卡**不再**进「未实现关键词」报表（卡面那边 `B116_W29` 已经不打星 ⇒ 报表现在与它同口径）"
         + "｜🧨 白名单没接进 `UnimplementedKeywords` ⇒ 本条红");
        Ok(un.Contains("b116w31bogus"),
           "D4 ★★ 但**真的**未实现的词照样报出来（⛔ 不是把整张表清空）"
         + "｜🧨 改成 `return new List<string>()` ⇒ 本条红。**D3/D4 结构上不可能同时被「恒空」满足**");

        // 与表现层那一份**逐项相等**（两处各写一份名单 = 迟早不一致）
        var other = CardPresentation.BattleDriver.PresentationOnlyKeywordsForTest;
        bool same = other != null && other.Length == (wl != null ? wl.Length : -1);
        if (same)
            for (int i = 0; i < other.Length; i++)
                if (other[i] != wl[i]) { same = false; break; }
        Ok(same,
           "D5 ★★ 引擎侧白名单与 `BattleDriver.PresentationOnlyKeywords` **逐项相等**（"
         + (wl != null ? string.Join(",", wl) : "?") + " vs " + (other != null ? string.Join(",", other) : "?") + "）"
         + "｜🧨 只改一边 ⇒ 本条红（`ruleprobe` 只编 `RuleEngine/Core`，所以两份结构上必须并存 —— 靠这条断言防漂）");
    }
}
