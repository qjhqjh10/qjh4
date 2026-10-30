// B100F26_Assert.cs — 执行代理 `B100_F26`（第一百会话 · 战斗线 · K4）的断言集
//
// ============================ 它盯的是什么 ============================
//   账 `战斗逻辑驱动:676` 的**计数路径**（`RuleEngine/Core/CountEval.cs` 的 `MeetsBoardCriteria`）
//   有一个**静默**缺陷：`selectFromChooseResolution` / `traitsFilter == 780` 两跳**收窄了候选池**
//   （`CountEval.cs:363`），但**从不把结果写回 `count`**；紧接的 `ApplyMaxTargets` 在
//   `maxTargets < 1` 时**原样回 `count`**（`CountEval.cs:938`：`if (max < 1 || count <= max) return count;`）
//   ⇒ **计数永远是收窄之前那个数**，两跳在计数路上等于没接。
//
// ============================ 判据出处（⛔ 别在这里抄第二份）============================
//   🥇 `d:/2/tools/decomp_full/BoardAnalysis__MeetsCriteria.c:101-103`
//      ```
//      lVar2 = AbilityLogic__GetTargets(param_1,lVar2,param_3,param_4,param_5,1,0);
//      if (lVar2 == 0) goto LAB_1806077b2;
//      iVar6 = *(int *)(lVar2 + 0x18);      // ← 计的数 = GetTargets **返回表**的 List.Count
//      ```
//      ⇒ 原版数的就是 **`GetTargets` 返回之后**那张表的 `Count`(`+0x18`)，而 `traitsFilter=780`
//        去重（`AbilityLogic__GetTargets.c:155-172` / `:1370-1385`）、`selectFromChooseResolution`
//        收窄（`:1101-1140`）、`maxTargets` 截断（`:1164-1365`）**全在 `GetTargets` 里面**、
//        都在 `return` **之前** ⇒ 计数必然是**收窄之后**的数。
//   ⚠️ 改法**只动 `CountEval.cs:363` 这一行**（同一物理行内加花括号 + `count = cands.Count`）——
//      行数守恒，账里 9 条 `CountEval.cs:<行>` 落点全部不漂移。
//   判据全文 / 诊断 → `d:/4/Unity/资料/完全复刻/报告/B100_X9.md` 的 #23–#25 三行 + K4 聚类。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · F1（前提）：棋盘上我方 2 + 敌方 1 ⇒ 计数 **3**。没有它，「变 1」分不清是收窄还是棋盘本来就空。
//   · F2：`selectFromChooseResolution = 1` ＋ 选中 u1 ⇒ 计数 **恰好 1**
//     ｜🧨 把 `CountEval.cs:363` 的 `count = cands.Count` 删掉（退回「收窄了但计数不跟着走」）
//        ⇒ 实测回到 F1 的 3 ⇒ 红。
//   · F3（**结构性 · 判别式**）：同一份 criteria `EqualTo(1)` **成立** 且 `EqualTo(3)` **不成立** ——
//     **两条结构上不可能同时满足** ⇒ 光「把期望值改成 3」或者「让两边一起回旧写法」都救不回来。
//   · F4：选中项**不在棋盘上** ⇒ 计数 **0**（原版筛完是空表，⛔ 不是「全部候选」）
//     ｜🧨 同上（计数不跟着收窄走 ⇒ 仍是 3 ⇒ 红）。
//   · F5：`traitsFilter = 780` 剔掉一个候选 ⇒ 3 → **2**
//     ｜🧨 同上（去重不生效 ⇒ 仍是 3 ⇒ 红）。
//   · F6（灭自证 · 反向）：同一棋盘、`select = 0` 且 `traits = 0` ⇒ 计数**仍是 3**
//     ｜🧨 把 `count = cands.Count` 写成「无条件执行」而不看「收窄有没有真发生」也**不该**影响本条；
//        但 🧨 若有人把收窄写成「永远执行」⇒ 本条红。
//   · F7（灭自证 · 非棋盘族 · 🔶 不许静默失败）：`ta=50`（手牌池）＋ `traitsFilter = 780` ⇒
//     候选是 `CardInstance`、不是 `UnitState` ⇒ 收窄**重算不了** ⇒ 必须 **gap**（⛔ 不许拿收窄前的数顶）
//     ｜🧨 把那一跳的 `else if (narrowingRequested && candFaithful != count) { gap=…; return null; }`
//        删掉 ⇒ 本条会拿到一个数而不是 gap ⇒ 红。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B100F26_Assert.Run -logFile "d:/4/_tmp_view/b100f26.log"
//   判绿 = 合计那行 `失败 0`。想并进别的宿主就两行：
//     `B100F26_Assert.Run(); pass += B100F26_Assert.Pass; fail += B100F26_Assert.Fail;`
//   ⚠️ 本文件**不建场景 / 不建资产 / 不进任何 `Editor/*Scene.cs`**；只造内存里的 `BattleContext`。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>`B100_F26`：`MeetsBoardCriteria` 收窄候选后**必须重算 `count`**（账 `676` 计数路径）。</summary>
public static class B100F26_Assert
{
    static readonly CheckSink _sink = new CheckSink("[F26] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>本趟通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>本趟失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }
    /// <summary>本趟实际执行的断言条数（= Pass + Fail；证明断言真被跑到）。</summary>
    public static int RunCount { get { return _sink.Pass + _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0;
        try
        {
            MenuCheck.Section(_sink, "B100_F26 · 账 676 计数路径：收窄（去重 → select）之后 count 必须跟着走");
            SectionF_收窄后重算计数();
            MenuCheck.Section(_sink, "§G（`play check` 指纹）由调度台在同步点跑 —— 本代理不跑 Unity 自检宿主");
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
    //  F · 收窄之后必须重算计数
    // ==================================================================
    static void SectionF_收窄后重算计数()
    {
        var ctx = Battle();
        var u1 = Place(ctx, 0, 2, Unit("F26CntA", 1, 1, 5), exhausted: false);
        var u2 = Place(ctx, 0, 3, Unit("F26CntB", 1, 1, 5), exhausted: false);
        Place(ctx, 1, 3, Unit("F26CntFoe", 1, 1, 5), exhausted: true);

        // ---- F1：前提 ----
        int count0 = Measure(ctx, Set(10, 0), u1, null);
        C(count0, 3, "F1 （前提）棋盘上我方 2 + 敌方 1 ⇒ 计数 = 3");

        // ---- F2：selectFromChooseResolution 收窄 ⇒ 计数必须变 1 ----
        int cSel = Measure(ctx, Set(10, 1), u1, u1.Card);
        C(cSel, 1, "★★ F2 `selectFromChooseResolution = 1` ＋ 选中 u1 ⇒ 计数**恰好 1**"
                 + "（改前 = 收窄前一那个数 " + count0 + "）"
                 + "｜🧨 删掉 `CountEval.cs:363` 的 `count = cands.Count` ⇒ 回到 " + count0 + " ⇒ 红");

        // ---- F3：结构性判别式（EqualTo(1) 成立 且 EqualTo(3) 不成立）----
        var setSel = Set(10, 1);
        ctx.CardChosenInSelection = u1.Card;
        bool? eq1 = CountEval.MeetsBoardCriteria(ctx, 0, Gate(setSel, 1, 1), u1, out _, true, 0);
        ctx.CardChosenInSelection = u2.Card;
        bool? eq2 = CountEval.MeetsBoardCriteria(ctx, 0, Gate(setSel, 1, 3), u1, out _, true, 0);
        ctx.CardChosenInSelection = null;
        Tr(eq1 == true && eq2 == false,
           "★★ F3 判别式：`EqualTo(1)` **成立** 且 `EqualTo(3)` **不成立**（收窄成 1 张后，选哪张都只数 1）"
         + "｜🧨 两条结构上不可能同时满足：把期望值改成 3、或让实现与断言一起回旧写法都会让其中一条红");
        if (eq1 != true || eq2 != false)
            Debug.Log(_sink.P + $"   F3 实测：EqualTo(1)={eq1?.ToString() ?? "null"} · EqualTo(3)={eq2?.ToString() ?? "null"}");

        // ---- F4：选中项不在棋盘上 ⇒ 0 ----
        int cOff = Measure(ctx, Set(10, 1), u1, Unit("F26NotOnBoard", 1, 1, 5));
        C(cOff, 0, "★ F4 旗开着、选中的那张**不在棋盘上** ⇒ 计数 **0**（原版筛完是空表；⛔ 不是「全部候选」）"
                 + "｜🧨 计数不跟着收窄走 ⇒ 仍是 " + count0 + " ⇒ 红");

        // ---- F5：traitsFilter = 780 去重 ⇒ 3 → 2 ----
        u1.Instance.ActiveEffects.Add(new ActiveCardEffect { EnchantmentCard = u2.Card, BuffType = 15 });
        int cMut = Measure(ctx, Set(10, 0, traits: 780), u1, null);
        C(cMut, count0 - 1, "★★ F5 `traitsFilter = 780` 剔掉**已挂过同一条单次变异**的候选（"
                          + count0 + " → " + (count0 - 1) + "）"
                        + "｜🧨 去重生效但计数不跟着走 ⇒ 仍是 " + count0 + " ⇒ 红");
        u1.Instance.ActiveEffects.Clear();

        // ---- F6：灭自证 · 反向（两个位都关 ⇒ 计数不变）----
        int cPlain = Measure(ctx, Set(10, 0), u1, null);
        C(cPlain, count0, "★★ F6 灭自证：`select = 0` 且 `traits = 0` ⇒ 计数**仍是 " + count0 + "**（没被误收窄）"
                        + "｜🧨 把收窄写成「永远执行」⇒ 本条红");

        // ---- F7：非棋盘族 + 收窄要求 ⇒ 必须 gap（⛔ 不许静默拿收窄前的数顶）----
        //   先断**前提**（手牌池里真数得出 > 0 张）—— 否则「count = 0 ⇒ 与 candFaithful 相等 ⇒ 不 gap」
        //   会让本条空转到绿（项目纪律：先写前提断言，再写被测断言）。
        var setHand = Set(50, 0, includeHero: 0, includeMinions: 1);
        int handCount = Measure(ctx, setHand, u1, null);
        Tr(handCount >= 1, "F7 前提：手牌池（`ta=50`）里真数得出候选（实测 " + handCount + " 张；若 ≤ 0 本条无效）");
        var setHandMut = Set(50, 0, traits: 780, includeHero: 0, includeMinions: 1);
        string g7 = GapOf(ctx, setHandMut, u1);
        Tr(g7 != null && g7.Contains("traitsFilter=780"),
           "★★ F7 非棋盘族（`ta=50` 手牌池）＋ `traitsFilter = 780` ⇒ **gap**（候选是 `CardInstance`、不是 "
         + "`UnitState` ⇒ 收窄后重算不了，⛔ 不拿收窄前的数顶）｜🧨 删掉那一跳的 `else if (…) { gap=…; return null; }` "
         + "⇒ 本条会拿到一个数而不是 gap ⇒ 红；实测 gap = " + (g7 ?? "(没有 gap)"));
    }

    // ==================================================================
    //  读数辅助
    // ==================================================================
    /// <summary>用 `triggerIf = EqualTo(n)` 线性探出**确切计数**（`-1` = 判不了 / `-2` = 超出探测上限）。</summary>
    /// <remarks>每次探之前**重设** `ctx.CardChosenInSelection` —— 收窄成功时会把它清空（原版 `.c:1138`），
    /// 不重设的话第二次探就测的是「没选过牌」那一档。</remarks>
    static int Measure(BattleContext ctx, CardDef.CountSetSpec set, UnitState actor, CardDef chosen, int maxProbe = 20)
    {
        for (int n = 0; n <= maxProbe; n++)
        {
            ctx.CardChosenInSelection = chosen;
            bool? r = CountEval.MeetsBoardCriteria(ctx, 0, Gate(set, 1, n), actor, out _, true, 0);
            if (r == null) return -1;
            if (r.Value) return n;
        }
        return -2;
    }

    /// <summary>取「这一档判不了」的那句 gap（判得了 ⇒ `null`）。</summary>
    static string GapOf(BattleContext ctx, CardDef.CountSetSpec set, UnitState actor)
    {
        var r = CountEval.MeetsBoardCriteria(ctx, 0, Gate(set, 2, 1), actor, out string gap, true, 0);
        return r == null ? (gap ?? "(gap 文案为空)") : null;
    }

    static CardDef.BoardGateSpec Gate(CardDef.CountSetSpec set, int statOption, int referenceValue)
    {
        return new CardDef.BoardGateSpec
        {
            CountOption = 0,
            StatOption = statOption,
            ReferenceValue = referenceValue,
            CountSet = set,
        };
    }

    // ==================================================================
    //  夹具（与 `B98G6_Assert` / `RuleEngineTest` 的同名夹具**同形**）
    // ==================================================================
    /// <summary>一份**全字段已知**的 `CountSetSpec`（`Unknown(-1)` 会被判成 gap ⇒ 夹具必须先填实）。</summary>
    static CardDef.CountSetSpec Set(int ta, int selectFromChooseResolution, int traits = 0, int maxTargets = 0,
                                    int includeHero = 1, int includeMinions = 1, int includeSpells = 0)
    {
        return new CardDef.CountSetSpec
        {
            PlayerTurn = 0,
            TargetsAffected = ta,
            ChosenArmy = 0,
            ArmyFilter = 0,
            KeywordFilter = 0,
            TraitsFilter = traits,
            MaxTargets = maxTargets,
            TargetSelectMethod = 0,
            AttackFilter = 0,
            AttackFilterOption = 0,
            CurrentHealthFilter = 0,
            CurrentHealthFilterOption = 0,
            ManaCostFilter = 0,
            ManaCostFilterOption = 0,
            RarityFilter = 0,
            RarityFilterOption = 0,
            SpellType = 0,
            FriendlyUnits = 1,
            EnemyUnits = 1,
            IncludeHero = includeHero,
            IncludeMinions = includeMinions,
            IncludeSpells = includeSpells,
            IncludeWhispers = 0,
            IncludeWeapons = 0,
            IncludeRemnants = 0,
            IncludeHeroParts = 0,
            IncludeSelf = 0,
            ExcludeNotSelf = 0,
            ExcludeSimilarCards = 0,
            ExcludeTargetted = 0,
            ExcludeFriendlyHero = 0,
            ExcludeEnemyHero = 0,
            ExcludeRace = 0,
            ExcludeTrait = 0,
            SelectFromChooseResolution = selectFromChooseResolution,
            FilterSpecificCardIds = new string[0],
            ExcludeEnchantmentIds = new string[0],
        };
    }

    static CardDef Unit(string name, int cost, int atk, int hp, params string[] kws)
    {
        return new CardDef(name, name, "unit", "", null, "Test", cost, atk, hp, 0, kws);
    }

    /// <summary>督军（`subtype` 必须给 `"Warlord"` —— 与 `RuleEngineTest.HeroOf` 同一口径）。</summary>
    static CardDef Hero()
    {
        return new CardDef("F26FixtureWarlord", "F26FixtureWarlord", "hero", "", null, "Test", 0, 2, 30, 0, null,
                           subtype: "Warlord");
    }

    static List<CardDef> Deck()
    {
        var d = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d.Add(Unit("f26filler" + i, 1, 0, 1));
        return d;
    }

    static BattleContext Battle()
    {
        var ctx = RuleCore.NewBattle(Deck(), Deck(), seed: 0, shuffle: false);
        ToP0Turn1(ctx);
        return ctx;
    }

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

    /// <summary>把一个单位直接摆到场上（跳过部署流程，只为构造计数场景）—— 与 `B98G6_Assert.Place` 逐句同形。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card, bool exhausted)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = exhausted };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }
}
