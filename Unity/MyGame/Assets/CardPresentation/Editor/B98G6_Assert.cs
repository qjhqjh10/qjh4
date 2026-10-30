// B98G6_Assert.cs — 执行代理 `B98_G6`（第九十八会话 · 波 5d · 战斗线）的断言集
//
// ============================ 它盯的是什么 ============================
//   A · 账 `战斗逻辑驱动:676` 的**计数路径**（`RuleEngine/Core/CountEval.cs` 的 `MeetsBoardCriteria`）：
//       原版 `MeetsCriteria` 数候选走的是 `AbilityLogic.GetTargets(…)` ⇒ 候选池**同样**要过末尾两段：
//       ① `traitsFilter == 780` 的单次变异去重（内层，`AbilityLogic__GetTargets.c:155-172` / `:1370-1385`）
//       ② `selectFromChooseResolution` 的收窄（最外层，`:1101-1140`）
//       然后才轮到 `maxTargets` 截断（`:1164`，本来就有）。
//     🆕 本笔把两跳接进计数路（`CountEval.cs` 的 `needCands` 加两个条件 + 一行串联），并把 `SetGap`
//       从「见旗就 gap」收窄成「**非棋盘族**（候选不是 `UnitState`）才 gap」。
//   B · 账 `1282` 的 (a) 格（`CardDef.AttachByAbilityKind`，本笔落在新文件
//       `RuleEngine/Core/CardDefPartial_B98G6.cs`）：
//       ① `AM22` —— `Give A or B` 折成的 `chooseone + RandomPick` **单条** op 盖着两族分支 ⇒ 两族都空；
//       ② `SOR13` —— 同族两行 `ta=10/120` ≠ 两条 `give` op ⇒ 旧口径「逐格相同」永远歧义。
//
// ============================ 判据出处（⛔ 别在这里抄第二份）============================
//   `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:155-172 / :721-746 / :1101-1140 / :1164 / :1370-1385`
//   `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:262 / :1179-1234`
//   `d:/4/Unity/资料/完全复刻/判据/B98_R5_波5剩余判据.md` §一（本轮施工单）
//   `d:/4/Unity/资料/完全复刻/判据/B92_战斗判据_B9.md §4·2:424-446`
//   `d:/4/Unity/资料/完全复刻/报告/B98_G6.md`（G6：含「为什么 SOR13 **当时**仍落 gap」；`B98_G8` 之后已挂上）
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · A1（前提）：棋盘上有我方 2 + 敌方 1 个单位 ⇒ 计数 **≥ 3**。没有它，下面「变 1」分不清是收窄
//     还是棋盘本来就空。
//   · A2：`selectFromChooseResolution = 1` ＋ `ctx.CardChosenInSelection = u2.Card` ⇒ 计数**恰好 1**
//     ｜🧨 把 `CountEval.cs` 那两跳删掉（回到「计数池不过末尾两段」）⇒ 实测仍是 count0 ⇒ 红。
//     ⚠️ 2026-10-30 `B100_F34`：这里原来写 `u1.Card` —— 但 `IncludeSelf = 0` 会把 acting unit（u1）淘汰
//        ⇒ 选中 u1 必得 0。改成选 u2（全池 88/88 张带该旗的卡 `includeSelf` 都是 0 ⇒ 从不选自己）。
//   · A3：旗开着、选中项**不在棋盘上** ⇒ 计数 **0**（原版筛完就是空表，⛔ 不当成「全部候选」）。
//   · A4（灭自证 · 反向）：同一棋盘、同一选中项，但 `selectFromChooseResolution = 0` ⇒ 计数**仍是 count0**
//     且 `ctx.CardChosenInSelection` **没被清空** ｜🧨 把收窄写成「无条件执行」⇒ 两条同时红。
//   · A5：旗开着、候选池**本来就空** ⇒ 计数 0 且选中项**被清空**（原版 `.c:1101-1140` 的清空与池空不空无关）
//     ｜🧨 把 `ApplySelectFromChooseResolution` 的 `pool.Count == 0` 早退加回去 ⇒ 本条红。
//   · A6/A7：`traitsFilter = 780` 剔掉已挂过同一条单次变异的候选（A6），`traitsFilter = 0` 一个都不剔（A7）
//     ｜🧨 把 `needCands` 里的 `traitsFilter == 780` 去掉 ⇒ A6 红。
//   · A8/A9/A10：「旗开着但候选不是棋盘单位」仍 **gap**（手牌 `ta=50` / 墓地 `ta=60`），
//     而 `ta=50` + 旗关 ⇒ 不 gap ｜🧨 把 `SetGap` 那一句**整条撤掉**（= 把判不了静默当成没这个位）
//     ⇒ A8/A9 红；🧨 把收窄写成「连手牌族也放行」⇒ A8/A9 也会红（它们会拿到一个数而不是 gap）。
//   · A11：`selectFromChooseResolution = Unknown(-1)`（没落）＋棋盘族 ⇒ **gap**（⛔ 不拿 -1 当 0）
//     ｜🧨 把 Unknown 从 `SetGap` 的条件里去掉 ⇒ 本条红。
//   · B1/B2（AM22）：挂上之后**总共有且只有 1 条** op 带 `ActiveAbilityRow`，且它挂在 `ca=30` **那一行**
//     （行 1）；**行 0（`ca=10`）一条都没有** ⇒ 执行器（`RuleCore.UseActiveAbilityCore:15482-15492`：
//     `ca==30` 行执行**全部已挂 op**、其它行只跑本行）实际只结算**一次**
//     ｜🧨 把二选一那条 op 挂到行 0（= 按族匹配的老路，施工单的字面写法）⇒ B2 红，且玩家会看到**两次**二选一。
//   · B3（SOR13 · 判据侧）：`RowMatchesOpTarget` 对两条真 op 各自**唯一命中**一行
//     （`ta=10 ↔ troop` / `ta=120 ↔ warlord`）｜🧨 把两个函数参数对调或返回常量 ⇒ B3 红。
//   · B4（SOR13 · 现状）：**已 `attached`**（`B98_G8` 把 `RuleCore` 的 `ca∈{30,31}` 分支改成「只跑本行 op」，
//     耦合闸随之删掉）⇒ 两条 `give` 各挂一行、各结算**一次**。本条红 = 闸又被加回 / 逐 op 配对坏掉
//     （那时 SOR13 要么一条不挂、要么变成 +2 而不是 +1）。
//
// ============================ 怎么跑（⚠️ 执行代理无权跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B98G6_Assert.Run -logFile "d:/4/_tmp_view/b98g6.log"
//   判绿 = 合计那行 `失败 0`。想并进别的宿主就两行：
//     `B98G6_Assert.Run(); pass += B98G6_Assert.Pass; fail += B98G6_Assert.Fail;`
//   ⚠️ 本文件**不建场景 / 不建资产 / 不进任何 `Editor/*Scene.cs`**；只造内存里的 `BattleContext`。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>`B98_G6`：账 `676` 的计数路径 + 账 `1282(a)` 的两张挂载。计数口径照 `CheckSink`。</summary>
public static class B98G6_Assert
{
    static readonly CheckSink _sink = new CheckSink("[G6] ") { Near = MenuNearStyle.Compact2 };

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
            MenuCheck.Section(_sink, "B98_G6 账 676（计数池去重 + 收窄）+ 账 1282(a)（AM22 / SOR13 挂载）");
            SectionA_计数池();
            SectionB_主动能力挂载();
            MenuCheck.Section(_sink, "§C（`play check` 指纹 & 旧断言翻面）由调度台在同步点跑 —— 本代理不跑 Unity/ruleprobe");
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
    //  A · 账 676 的计数路径
    // ==================================================================
    static void SectionA_计数池()
    {
        MenuCheck.Section(_sink, "A 计数池过「单次变异去重（内层）→ selectFromChooseResolution 收窄（外层）」"
                               + "＋ `SetGap` 只挡非棋盘族");

        var ctx = Battle();
        var u1 = Place(ctx, 0, 2, Unit("G6CntA", 1, 1, 5), exhausted: false);
        var u2 = Place(ctx, 0, 3, Unit("G6CntB", 1, 1, 5), exhausted: false);
        Place(ctx, 1, 3, Unit("G6CntFoe", 1, 1, 5), exhausted: true);

        var setPlain = Set(10, 0);
        int count0 = Measure(ctx, setPlain, u1, null);
        Tr(count0 >= 3, $"★ A1 （前提）棋盘上我方 2 + 敌方 1 ⇒ 计数 ≥ 3（实测 {count0}）"
                      + "｜🧨 若 `CountSet` 压根没收集候选/棋盘没摆上 ⇒ 本条红（下面所有「变 1」都会失去意义）");

        // ---- A2：旗开着 + 选中 u2 ⇒ 恰好 1 ----
        // 🔴 2026-10-30（`B100_F34` · 320645 · **α 夹具**）：选中的必须是**候选池里真有**的那一张。
        //   `Set(...)` 把 `IncludeSelf` 写死 `0`，而 `TargetCriteriaEval.cs:246-253` 在 `includeSelf == 0`
        //   时**把 acting unit 自己（u1）淘汰**（`if (self && set.IncludeSelf == 0) return false;`）
        //   ⇒ 原来「选中 u1」时 u1 永远不在池里 ⇒ 收窄后必为 0（`B100_F26` 把 `count` 改成收窄后的数之后才显形）。
        //   **为什么改「选 u2」而不是「IncludeSelf 改 1」**：现读全池 —— `selectFromChooseResolution == 1`
        //   的 **88/88 行 `includeSelf` 全 = 0**（`cards_engine.json`）⇒ 原版用这个旗时**从不允许选自己**，
        //   所以保留 `IncludeSelf = 0` + 改选一个非自身的候选（u2）**更贴原版**。
        var setSel = Set(10, 1);
        int cSel = Measure(ctx, setSel, u1, u2.Card);
        C(cSel, 1, "★★ A2 `selectFromChooseResolution = 1` ＋ 选中 **u2**（非 acting unit ⇒ 在候选池里）⇒ 计数**恰好 1**"
                 + "｜🧨 把 `CountEval.MeetsBoardCriteria` 里那两跳（去重 → 收窄）删掉 ⇒ 实测等于 A1 的 "
                 + count0 + " ⇒ 红");

        // ---- A3：选中项不在棋盘上 ⇒ 0 ----
        var setSel2 = Set(10, 1);
        int cOff = Measure(ctx, setSel2, u1, Unit("G6NotOnBoard", 1, 1, 5));
        C(cOff, 0, "★ A3 旗开着、选中的那张**不在棋盘上** ⇒ 计数 **0**（原版筛完是空表；⛔ 不是「全部候选」）");

        // ---- A4：灭自证 · 旗关 ⇒ 不收窄、也不清选中项 ----
        ctx.CardChosenInSelection = u1.Card;
        bool? r4 = CountEval.MeetsBoardCriteria(ctx, 0, Gate(setPlain, 2, 1), u1, out _, true, 0);
        Tr(r4 == true, "★ A4 前提：旗关 + 棋盘上有候选 ⇒ 闸成立（`>= 1`）");
        Tr(ctx.CardChosenInSelection != null,
           "★★ A4 灭自证：**旗关时选中项不被清空、候选也不被收窄** ｜🧨 把收窄写成「无条件执行」⇒ 本条红"
         + "（同时 A2 之外的路径也会被静默改窄）");
        ctx.CardChosenInSelection = null;

        // ---- A5：候选池本来就空 + 旗开 ⇒ 选中项仍要被清空 ----
        var setFoeOnly = Set(10, 1, includeHero: 0, includeMinions: 1);   // 只数「敌方部队」，且排除督军
        setFoeOnly.FriendlyUnits = 0;          // 敌方部队一个都没摆 ⇒ 候选池**本来就是空的**（与督军无关）
        var ctxEmpty = Battle();               // 新局：一张单位都不摆
        ctxEmpty.CardChosenInSelection = u1.Card;
        int cEmpty = Measure(ctxEmpty, setFoeOnly, null, u1.Card, 0);
        C(cEmpty, 0, "★ A5 旗开着、候选池**本来就空** ⇒ 计数 0（前提）");
        Tr(ctxEmpty.CardChosenInSelection == null,
           "★★ A5 灭自证：池空时**也要清** `cardChosenInSelection`（原版 `.c:1101-1140` 的清空与池空不空无关）"
         + "｜🧨 把 `ApplySelectFromChooseResolution` 的 `pool.Count == 0` 早退加回去 ⇒ 选中项会留给下一条效果 ⇒ 红");

        // ---- A6/A7：`traitsFilter == 780` 的单次变异去重 ----
        u1.Instance.ActiveEffects.Add(new ActiveCardEffect { EnchantmentCard = u2.Card, BuffType = 15 });
        var setMut = Set(10, 0, traits: 780);
        int cMut = Measure(ctx, setMut, u1, null);
        C(cMut, count0 - 1, "★★ A6 `traitsFilter = 780` ⇒ 剔掉**已经挂过同一条单次变异**的候选（"
                          + count0 + " → " + (count0 - 1) + "）"
                        + "｜🧨 把 `needCands` 里的 `|| set.TraitsFilter == 780` 去掉 ⇒ 这条永远不减 ⇒ 红"
                        + "｜🧨 把去重挪到收窄**之后**也行（本条测不出次序，见 §C；次序由代码位置保证）"
                        // 🔴 2026-10-30（`B100_F34` · 320744 · `报告/B100_X15.md` §四 · #14）：**本条现在红，是【产品侧】缺口、不是本文件写错**。
                        + "｜⏳ 现状 = 红：**依赖产品补 `TargetCriteriaEval.TraitNameOf` 的 `case 780: return \"mutation\";`**"
                        + "（见 `报告/B100_X15.md` §四 · #14）—— 未补前 `TraitPasses` 判不了 ⇒ 整条 criteria 回 **-1** ⇒ 本条必红。"
                        + "⚠️ 补 `case 780` 之后**期望值也要重定**：原版 `FilterMethods__MeetsTraitCriteria.c` 会先按 mutation trait "
                        + "筛候选，而本夹具的单位都没挂该 trait ⇒ 结果会变 **0**、不是 `count0-1`（届时夹具 + 期望一起改，⛔ 别只改期望值）");
        var setPlain2 = Set(10, 0);
        int cNoMut = Measure(ctx, setPlain2, u1, null);
        C(cNoMut, count0, "★ A7 `traitsFilter = 0` ⇒ 一个都不剔（⛔ 别在别处无条件去重 —— 那会把目标池剔一半）");
        u1.Instance.ActiveEffects.Clear();

        // ---- A8/A9/A10：非棋盘族仍判不了 ----
        var setHandSel = Set(50, 1, includeHero: 0, includeMinions: 1, includeSpells: 1);
        string g8 = GapOf(ctx, setHandSel, u1);
        Tr(g8 != null && g8.Contains("selectFromChooseResolution"),
           "★★ A8 手牌族（`ta=50`）＋旗 ⇒ **仍 gap**（候选是 `CardInstance` 不是 `UnitState`，与账 1282(b)(c) 同一根）"
         + "｜🧨 把 `SetGap` 那一句整条撤掉（= 把判不了静默当成「没这个位」）⇒ 本条红；实测说明 = " + (g8 ?? "(没有 gap)"));
        var setCemSel = Set(60, 1);
        string g9 = GapOf(ctx, setCemSel, u1);
        Tr(g9 != null && g9.Contains("selectFromChooseResolution"),
           "★★ A9 墓地族（`ta=60`）＋旗 ⇒ **仍 gap**（同上）｜🧨 同上 ⇒ 红");
        var setHandPlain = Set(50, 0, includeHero: 0, includeMinions: 1, includeSpells: 1);
        // 🔴 2026-10-30（`B100_F29` · #26 · α 夹具）：先把前提写死 —— 手牌候选也要带 `cardTraits`，
        //   否则 `RawCardMeetsTraitCriteria` 在「旗」那一支**之前**就先判不了（`TargetCriteriaEval.cs:1033-1040`，
        //   次序 = 原版 `FilterMethods__RawCardMeetsTraitCriteria.c:12-73`），A10 测到的就不再是旗那一位。
        var hand = ctx.Players[0].Hand;
        Tr(hand != null && hand.Count > 0,
           "★ A10 前提：P0 手牌非空（`ta=50` 手牌族才有候选；⛔ 手牌空时本条会**空转绿**）");
        CardDef hand0 = (hand != null && hand.Count > 0) ? hand[0].Card : null;
        Tr(u1.Card.CardTraitsKnown && hand0 != null && hand0.CardTraitsKnown,
           "★ A10 前提：夹具卡与手牌候选**都挂了 `cardTraits`**（空表）⇒ trait 那一格判得了"
         + "｜🧨 去掉 `Unit()` 里的 `AttachCardTraits(new string[0])` ⇒ 本条红（手牌族退回 `cardTraits` gap，"
         + "A10 会变成「测夹具」而不是「测旗」）");
        string g10 = GapOf(ctx, setHandPlain, u1);
        Tr(g10 == null,
           "★ A10 **灭自证**：手牌族 + **旗关** ⇒ 不因 `selectFromChooseResolution` gap（证明 A8/A9 的 gap 是**这一位**在起作用，"
         + "不是把整族一律挡死）" + (g10 != null ? "｜实测 gap = " + g10 : ""));

        // ---- A11：`Unknown(-1)` ≠ 0 ----
        var setUnk = Set(10, CardDef.CountSetSpec.Unknown);
        string g11 = GapOf(ctx, setUnk, u1);
        Tr(g11 != null && (g11.Contains("selectFromChooseResolution")),
           "★★ A11 `selectFromChooseResolution = Unknown(-1)`（没落）＋棋盘族 ⇒ **gap**（⛔ 不把 -1 当 0 放行）"
         + "｜🧨 把 Unknown 从 `SetGap` 那个条件里去掉 ⇒ 本条红；实测 = " + (g11 ?? "(没有 gap)"));

        // ---- A12：放行只给「接了两跳」的那一条路（`mult` 那一支必须照旧 gap）----
        var setMulQ = Set(10, 1);
        int? rMul = CountEval.CountTargets(ctx, 0, setMulQ, 0, u1, out string gMul);
        Tr(rMul == null && gMul != null && gMul.Contains("selectFromChooseResolution"),
           "★★ A12 **灭自证**：`CountTargets`（`role=\"mult\"`）**没有**那两跳 ⇒ 同一份 criteria 在那一支**仍然 gap**"
         + "｜🧨 把 `SetGap` 的 `boardPoolWired` 参数去掉（= 两支一起放行）⇒ 那一支会不报 gap 并**静默不收窄** ⇒ 本条红"
         + "；实测 = " + (rMul == null ? (gMul ?? "(gap 文案为空)") : "值 " + rMul));
    }

    // ==================================================================
    //  B · 账 1282(a)：`AM22` / `SOR13` 的主动能力挂载
    // ==================================================================
    static void SectionB_主动能力挂载()
    {
        MenuCheck.Section(_sink, "B 账 1282(a)：`AM22`（chooseone 一条 op 盖两族）挂上·只执行一次；"
                               + "`SOR13`（同族两行 ta=10/120）已挂上、两行各结算一次（执行器不再跑全部 op）");

        var pool = CardDatabase.Load();
        var am22 = CardDatabase.FindById(pool, "AM22");
        var sor13 = CardDatabase.FindById(pool, "SOR13");
        Tr(am22 != null && sor13 != null, "★ B0 （前提）池里找得到 `AM22` / `SOR13`");
        if (am22 == null || sor13 == null) return;

        var ctx = Battle();
        var uAm = new UnitState(ctx.NewInstance(am22), false);
        var uSor = new UnitState(ctx.NewInstance(sor13), false);

        // ---- B1/B2：AM22 ----
        C(am22.ActiveAbilityMap ?? "(null)", "attached",
          "★★ B1 `AM22`（`Duty: Deal 2-3 damage to an enemy or give Shield to a friendly unit`）⇒ `attached`"
        + "｜🧨 把 `CardDef.cs` 的 `anyAttached` 初值改回 `false`（即不做 either-or 特例）⇒ 回到 `gap:kind-no-op` ⇒ 红");

        var opsAm0 = OpsOfRow(uAm, 0);
        var opsAm1 = OpsOfRow(uAm, 1);
        C(opsAm0.Count, 0, "★★ B2 `AM22` 的**行 0（`ca=10` 伤害族）一条 op 都不挂** —— 二选一那条 op 属于 `ca=30` 那一行"
                         + "｜🧨 把它挂到行 0（按族匹配的老路 / 施工单的字面写法）⇒ 本条红");
        C(opsAm1.Count, 1, "★★ B2' `AM22` 的**行 1（`ca=30` 增益族）恰好 1 条** op ⇒ 执行器（`ca==30` 行跑全部已挂 op）"
                         + "只结算**一次**（两次 = 二选一被结算两遍）｜🧨 同上 ⇒ 红");
        if (opsAm1.Count == 1)
        {
            var op = opsAm1[0];
            C(op.Verb ?? "(null)", "chooseone", "★ B2'' 挂上的就是那条 `chooseone`（`EffectText.EmitEitherOr` 的形状）");
            Tr(op.RandomPick, "★ B2''' 且 `RandomPick = true`（卡面没有 `choose` 字样 ⇒ 引擎随机择一，不该问玩家）");
            C(op.SubjectTargetsAffected, 90, "★ B2'''' 目标集 = 那一行的 `ta = 90`（`targetIfMeetsCriteria`；两行的 `ta` 都是 90）");
        }
        int amTotal = CountAttached(uAm);
        C(amTotal, 1, "★★ B2''''' `AM22` 全卡**总共只有 1 条** op 带 `ActiveAbilityRow` ⇒ 只结算一次（前提：真的只挂了一次）");

        // ---- B3：SOR13 的配对判据（`RowMatchesOpTarget`）对真 op 唯一命中 ----
        EffectOp troopOp = null, warlordOp = null;
        foreach (string kw in CardDef.RoutableTriggers)
        {
            var list = sor13.TriggerOps(kw);
            if (list == null) continue;
            for (int i = 0; i < list.Count; i++)
            {
                var o = list[i];
                if (o == null || o.Verb != "give" || o.Target == null) continue;
                if (o.Target.Kind == "troop") troopOp = o;
                else if (o.Target.Kind == "warlord") warlordOp = o;
            }
        }
        Tr(troopOp != null && warlordOp != null,
           "★ B3 （前提）`SOR13` 的正文解出**两条** `give`：一条目标写 `troop`、一条写 `warlord`"
         + "（卡面 `Give +1 [Attack] to your troops, and to your Warlord this turn`）");
        if (troopOp != null && warlordOp != null)
        {
            Tr(CardDef.RowMatchesOpTarget(10, troopOp) == true,
               "★★ B3-a `ta=10`（`allFiltered`）↔ 目标写 `troop` ⇒ 命中");
            Tr(CardDef.RowMatchesOpTarget(120, troopOp) == false,
               "★★ B3-b `ta=120`（`friendlyHero`）↔ 目标写 `troop` ⇒ **明确不是**（督军 ≠ 部队）");
            Tr(CardDef.RowMatchesOpTarget(120, warlordOp) == true,
               "★★ B3-c `ta=120`（`friendlyHero`）↔ 目标写 `warlord` ⇒ 命中");
            Tr(CardDef.RowMatchesOpTarget(10, warlordOp) != true,
               "★★ B3-d `ta=10`（场上按 criteria 筛的部队集）↔ 目标写 `warlord` ⇒ 不命中"
             + "｜🧨 B3-a..d 四条**结构上不可能同时满足**：把 `RowMatchesOpTarget` 的返回值取反 / 写成常量 ⇒ 至少两条红");

            int hitsTroop = 0, hitsWarlord = 0;
            int[] tas = { 10, 120 };
            for (int i = 0; i < tas.Length; i++)
            {
                if (CardDef.RowMatchesOpTarget(tas[i], troopOp) == true) hitsTroop++;
                if (CardDef.RowMatchesOpTarget(tas[i], warlordOp) == true) hitsWarlord++;
            }
            C(hitsTroop, 1, "★★ B3-e 部队那条 op 在 `{10, 120}` 里**唯一命中**一行 ⇒ 配对无歧义");
            C(hitsWarlord, 1, "★★ B3-f 督军那条 op 在 `{10, 120}` 里**唯一命中**一行 ⇒ 配对无歧义");
        }

        // ---- B4：SOR13 已挂上（2026-10-29 `B98_G8` 删掉执行器耦合闸 + 执行器改成只跑本行 op）----
        C(sor13.ActiveAbilityMap ?? "(null)", "attached",
          "★★ B4 `SOR13` 已 `attached`（`B98_G8`：`RuleCore` 的 `ca∈{30,31}` 分支改成只跑本行 op ⇒ 耦合闸删掉）"
        + "⇒ 两条 `give` 各挂一行（`ta=10` 部队 / `ta=120` 督军），各结算**一次**（+1 / +1，与卡面一致）"
        + "｜🧨 回到 `gap:kind-ambiguous` = 闸被加回 / 配对坏掉 ⇒ 红");
        C(CountAttached(uSor), 2, "★★ B4' `SOR13` 两条 `give` 各带 `ActiveAbilityRow`（0 / 1）"
        + "｜🧨 挂成 0 条 = 没挂上；挂成 4 条 = 又跑回双份那条错路 ⇒ 红");

        // ---- B5：回归 —— `ca==30` 单行的老卡不受影响 ----
        var sor66 = CardDatabase.FindById(pool, "SOR66");
        if (sor66 != null)
        {
            var u66 = new UnitState(ctx.NewInstance(sor66), false);
            C(sor66.ActiveAbilityMap ?? "(null)", "attached",
              "★ B5 回归：`SOR66`（`ca=[30, 462]`，只有**一条** `ca=30` 行）仍 `attached`"
            + "｜🧨 either-or 特例写成「凡有 `ca=30` 就抢 op」⇒ 本条或 B1 会红");
            Tr(CountAttached(u66) >= 1, "★ B5' `SOR66` 的正文 op 仍带 `ActiveAbilityRow`（≥ 1 条）");
        }
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

    /// <summary>这张卡上有几条正文 op 带 `ActiveAbilityRow`（= 会被执行器认领的那几条）。</summary>
    static int CountAttached(UnitState u)
    {
        int n = 0;
        foreach (string kw in CardDef.RoutableTriggers)
        {
            var list = RuleCore_ActiveBodyOps(u, kw);
            for (int i = 0; i < list.Count; i++) if (list[i] != null && list[i].ActiveAbilityRow >= 0) n++;
        }
        return n;
    }

    /// <summary>`RuleCore.ActiveRowOpsOf(u, row)` 的等价读数（那一格是 `internal`，编辑器程序集看不见 ——
    /// 这里只按 `ActiveAbilityRow` 比，判据仍只有 `AttachByAbilityKind` 那一份）。</summary>
    static List<EffectOp> OpsOfRow(UnitState u, int row)
    {
        var outs = new List<EffectOp>();
        if (row < 0) return outs;
        foreach (string kw in CardDef.RoutableTriggers)
        {
            var list = RuleCore_ActiveBodyOps(u, kw);
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && list[i].ActiveAbilityRow == row) outs.Add(list[i]);
        }
        return outs;
    }

    /// <summary>`u.FxOps(kw)` 的**同一批对象**（`RuleCore.ActiveAbilityBodyOps` 是私有的；口径见它的 doc：
    /// 按 `CardDef.RoutableTriggers` 逐个取，`oath` 跳过）。</summary>
    static List<EffectOp> RuleCore_ActiveBodyOps(UnitState u, string kw)
    {
        var outs = new List<EffectOp>();
        if (u == null || string.IsNullOrEmpty(kw) || kw == KeywordTable.Oath) return outs;
        var ops = u.FxOps(kw);
        if (ops == null) return outs;
        for (int i = 0; i < ops.Count; i++) if (ops[i] != null) outs.Add(ops[i]);
        return outs;
    }

    // ==================================================================
    //  夹具（与 `B98G3_Assert` / `RuleEngineTest` 的同名夹具**同形**）
    // ==================================================================
    /// <summary>一份**全字段已知**的 `CountSetSpec`（`Unknown(-1)` 会被 `TargetCriteriaEval` 判成 gap ⇒ 夹具必须先填实）。</summary>
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

    /// <summary>手搓一张单位卡。🔴 **`cardTraits` / `rarity` 两格必须显式给**
    /// （与 `B99E3_Assert.Card` / `B99E8_Assert` / `B99T2_Assert` / `B99C1_Assert` 同族先例同形）：
    /// <para>① `TargetCriteriaEval.RawCardMeetsTraitCriteria` 在 `traitsFilter == 0` 短路**之前**就查
    /// `HasDefaultTrait(summonRift)`（次序照抄原版 `FilterMethods__RawCardMeetsTraitCriteria.c:12-73` 的 ①②③）
    /// ⇒ `_cardTraits == null` ⇒ `CardDef.CardTraitsWhy` 那句 gap。</para>
    /// <para>② 同一方法的第 ⑤ 步**无条件**读 `RarityOf(card)`（原版那张卡恒有 `+0x28` 的 rarity ——
    /// `FilterMethods__CheckIfRawCardMeetsCriteria.c:74-79` 直接读它，`option == 0` 时由
    /// `MeetsStatCriteria` 自己放行）⇒ 我们的 `Rarity` 为空也 gap。</para>
    /// <para>⇒ 挂**空表**（= 真卡「`cardTraits` 已挂、只是空」）。⛔ 别挂 `summonRift`：那会让
    /// `traitsFilter == 0` 判 **false**（.c 第 ① 步的门），正好把 A7/A10「不筛」那条口径测反。</para>
    /// <para>⚠️ 这两格都**不参与**棋盘族的判定（`TargetCriteriaEval.Matches` 的 rarity 由
    /// `rarityFilterOption` 开闸、`TraitPasses` 读的是实例上的当前关键词）⇒ A1–A7 / A11 / A12 的前提不变。</para></summary>
    static CardDef Unit(string name, int cost, int atk, int hp, params string[] kws)
    {
        var c = new CardDef(name, name, "unit", "", "common", "Test", cost, atk, hp, 0, kws);
        c.AttachCardTraits(new string[0]);
        return c;
    }

    /// <summary>督军（`subtype` 必须给 `"Warlord"` —— 与 `RuleEngineTest.HeroOf` 同一口径）。</summary>
    static CardDef Hero()
    {
        return new CardDef("G6FixtureWarlord", "G6FixtureWarlord", "hero", "", null, "Test", 0, 2, 30, 0, null,
                           subtype: "Warlord");
    }

    static List<CardDef> Deck()
    {
        var d = new List<CardDef> { Hero() };
        for (int i = 0; i < 20; i++) d.Add(Unit("g6filler" + i, 1, 0, 1));
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

    /// <summary>把一个单位直接摆到场上（跳过部署流程，只为构造计数场景）—— 与 `B98G3_Assert.Place` 逐句同形。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card, bool exhausted)
    {
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = exhausted };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }
}
