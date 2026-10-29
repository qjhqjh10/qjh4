// B97W21_Assert.cs — 执行代理 `B97_W21`（第九十七会话 · 波 1 补位）的断言
//
// ============================ 它盯的是什么 ============================
//   战斗线施工单 `判据/B97_战斗剩余_按文件施工单.md` §1·2 里本代理做掉的三条：
//     · `:254`/`:255` ① —— **引擎侧的 `GetChoiceOptions`**（`RuleCore.BuildChoiceOptions`）
//     · `:636`        —— 动作 47 的结算体**补上手牌两跳**（`RuleCore.BroadcastRemoveEffectsFrom`）
//     · `:482` ⓑ     —— 「先查后排队」的**手牌那半**（`RuleCore.HasHandExtrinsicFrom`）
//
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_战斗剩余_按文件施工单.md` §1·2 ＋
//   本轮报告 `资料/完全复刻/报告/B97_W21.md`。
//
// ============================ 怎么用（宿主）============================
//   `B97W21Assert.Run();`
//   + 宿主里两行（照 `B94B18Assert` 的先例）：
//     `_sink.Pass += B97W21Assert.Pass; _sink.Fail += B97W21Assert.Fail;`
//   · 推荐宿主 = **`Editor/RuleEngineTest.cs` 的 `RuleEngineTest.Run`**（本文件只碰引擎，不需要 UI 树）
//     —— 具体该插的那一行见报告 §五。
//   · 本文件**不建持久资产**、不动磁盘；建的对局全是**内存里的 `BattleContext`**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1/A2：把 `RuleCore.BuildChoiceOptions` 的循环上限从 `min(池, maxTargets)` 改成「恒 1」⇒ A1 红。
//   · A3 ★：把 `EffectResolver.TakePickCard` 里那句 `var __choiceOpts = BuildChoiceOptions(ctx, cands);`
//           **删掉**（= 回到「只抽 1 下」的旧口径）⇒ `RandomNumbersIndex` 只前进 1 ⇒ **A3 红**。
//           这一条就是账 254/255 的本体（原版消耗 N 格、我们少消耗 N-1 格）。
//   · A4：把 `RemoveAll` 那行删掉 ⇒ 池里同 id 的两份都会进 options ⇒ A4 红。
//   · A5：不调 `ctx.SetChoiceOptions(opts)` ⇒ `ctx.ChoiceOptions` 空 ⇒ A5 红。
//   · B1/B2 ★：把 `BroadcastRemoveEffectsFrom` 里那两句 `RemoveHandEffectsFromSource` 删掉
//           ⇒ B2 的返回数掉到 0、记录还在 ⇒ **B1/B2 红**（这就是账 636 的手牌两跳）。
//   · B3 ★（灭自证 —— 闸的**反向**）：把 `RemoveHandEffectsFromSource` 里 `if (r == null || !r.Extrinsic) continue;`
//           删掉（= 不看 extrinsic）⇒ **非 extrinsic 的那条也被摘** ⇒ B3 红。
//   · C1/C2：把 `HasHandExtrinsicFrom` 改成恒 `false`（= 「先查」只看棋盘）
//           ⇒ C1 红（该入队的不入队）；恒 `true` ⇒ C2 红（没有命中也会入队）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B97_W21`：选牌选项（254/255 ①）＋ 按来源摘外来效果的手牌两跳（482 ⓑ / 636）的断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W21Assert
{
    static readonly CheckSink _sink = new CheckSink("[W21] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    public static void Run()
    {
        SectionA_选牌选项();
        SectionB_按来源摘外来手牌效果();
        SectionC_先查后排队的手牌半();
        Debug.Log(_sink.P + "W21 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    static CardDef UnitCard(string name)
    {
        return new CardDef(name, name, "unit", "", null, "Test", 0, 1, 3, 0, null);
    }

    static CardDef HeroCard(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }

    /// <summary>只有「督军 + 指定那几张牌」的干净对局（照 `RuleEngineTest.ProbeBattle` 的形状，但**自带**、不依赖宿主私有夹具）。</summary>
    static BattleContext NewCtx(int seed, params CardDef[] hand0)
    {
        var d0 = new List<CardDef> { HeroCard("W21FixHero0", "Ultramarines") };
        for (int i = hand0.Length - 1; i >= 0; i--) d0.Add(hand0[i]);
        var d1 = new List<CardDef> { HeroCard("W21FixHero1", "Goff") };
        return RuleCore.NewBattle(d0, d1, seed: seed, shuffle: false, cardPool: new List<CardDef>());
    }

    /// <summary>手牌里第一份实例（`null` = 这副夹具没发到手）。</summary>
    static CardInstance FirstHand(BattleContext ctx, int p)
    {
        var hand = ctx.Players[p].Hand;
        return hand.Count > 0 ? hand[0] : null;
    }

    // ============================================================ A 选牌选项（账 254/255 ①）
    static void SectionA_选牌选项()
    {
        MenuCheck.Section(_sink, "A 账 254/255 ①：引擎侧 `GetChoiceOptions`（抽 min(池,maxTargets) 张 → bot 取 options[0]，"
                                + "并**消耗同样多的随机格**）");

        const int seed = 4242;
        var pool = new List<CardDef>();
        for (int i = 0; i < 5; i++) pool.Add(UnitCard("W21Opt" + i));

        // 控制组：同 seed 的另一局，只抽第 1 下 —— 原版 `options[0]` 就该是这一张
        var ctl = NewCtx(seed);
        int ctlIdx = ctl.GetRandomInt(0, pool.Count);

        var ctx = NewCtx(seed);
        // `maxTargets` 的来源 = 正在结算那张卡（`CardDef.ChoicesMaxTargets`）
        ctx.PlayingCard = new CardDef("W21Chooser", "W21Chooser", "spell", "", null, "Ultramarines",
                                      1, 0, 0, 0, null, choicesMaxTargets: 3);
        int cursorBefore = ctx.RandomNumbersIndex;

        var opts = RuleCore.BuildChoiceOptions(ctx, pool);

        C(opts.Count, 3,
          "A1 `min(池 5, maxTargets 3) = 3` 张候选（原版 `GetChoiceOptions.c:40-43` 的 iVar1）"
        + "｜🧨 把 `BuildChoiceOptions` 的循环上限改成恒 1 ⇒ 实得 1 ⇒ 本行红");
        Tr(opts.Count > 0 && ReferenceEquals(opts[0], pool[ctlIdx]),
           "A2 ★ 抽到的**第一张** == 同 seed 控制组「第 1 次 `GetRandomInt`」抽到的那张"
         + "（= 原版敌方档取 `options[0]`（`ChoiceOfCardEnemy.c:31-37`）；与旧实现「只抽 1 下」**选中的是同一张**，"
         + "差别只在随机流的消耗）｜🧨 把循环改成「先洗牌再取前 N」⇒ 本行红");
        C(ctx.RandomNumbersIndex - cursorBefore, opts.Count,
          "A3 ★★ **随机流消耗 = N 格**（原版 `MatchData.GetRandomInt` 每取一次游标前进一格，"
        + "取几次就有意义）—— 改前我们只消耗 1 格 ⇒ 之后每一次随机全部错位"
        + "｜🧨 把 `EffectResolver.TakePickCard` 里那句 `BuildChoiceOptions(ctx, cands)` 删掉（回到旧口径）⇒ 实得 1 ⇒ 本行红");

        // 去重：同 uniqueId（`CardDef.Id`）只进一份 —— 池里放两份同 id
        var dupPool = new List<CardDef> { UnitCard("W21Dup"), UnitCard("W21Dup"), UnitCard("W21Solo") };
        var ctx2 = NewCtx(seed);
        ctx2.PlayingCard = new CardDef("W21Chooser2", "W21Chooser2", "spell", "", null, "Ultramarines",
                                       1, 0, 0, 0, null, choicesMaxTargets: 2);
        var opts2 = RuleCore.BuildChoiceOptions(ctx2, dupPool);
        var seenIds = new HashSet<string>(System.StringComparer.Ordinal);
        bool uniq = true;
        foreach (var o in opts2) if (o != null && !seenIds.Add(o.Id)) uniq = false;
        Tr(uniq && opts2.Count >= 1,
           "A4 候选里**没有同 `uniqueId` 的两份**（原版 `GetChoiceOptions.c:66-69` 抽完就 `RemoveAll` 同 uniqueId）"
         + "｜🧨 把 `left.RemoveAll(...)` 那行删掉 ⇒ 同 id 两份都会进 ⇒ 本行红");

        C(ctx.ChoiceOptions.Count, opts.Count,
          "A5 ★ `ctx.ChoiceOptions` 拿到的是**同一份** options（面板将来改读它 = 原版「同一处算」）"
        + "｜🧨 删掉 `ctx.SetChoiceOptions(opts)` ⇒ 实得 0 ⇒ 本行红");
    }

    // ============================================================ B 动作 47 的手牌两跳（账 636）
    static void SectionB_按来源摘外来手牌效果()
    {
        MenuCheck.Section(_sink, "B 账 636：`BroadcastRemoveEffectsFrom` 三跳里的**手牌两跳**"
                                + "（原版 `BattleManagerSupport__BroadcastRemoveEffectsFrom.c` 后半段）");

        var ctx = NewCtx(777, UnitCard("W21HandA"), UnitCard("W21HandB"));
        var inst = FirstHand(ctx, 0);
        Tr(inst != null, "B0 **前提断言**：P0 手牌里有实例（0 张 ⇒ B1..B3 没跑；本文件不静默跳过）");
        if (inst == null) return;

        const string src = "W21Source";
        var rec = new CardInstance.HandEffect
        {
            Source = src,
            Extrinsic = true,                                   // 原版 `CardEffect.extrinsic(+0x30)`
            RecordId = ctx.NextHandEffectRecord(),
        };
        ctx.Players[0].HandEffectRecords.Add(rec);
        inst.HandEffects.Add(rec);

        int removed = RuleCore.BroadcastRemoveEffectsFrom(ctx, src);

        C(removed, 1,
          "B1 ★ 手牌上「`" + src + "` 挂的那条 extrinsic 效果」被摘掉 **1** 条（原版棋盘跳之后还有两手牌整手）"
        + "｜🧨 把 `BroadcastRemoveEffectsFrom` 里那两句 `RemoveHandEffectsFromSource` 删掉 ⇒ 实得 0 ⇒ 本行红");
        Tr(!inst.HandEffects.Contains(rec) && ctx.Players[0].HandEffectRecords.IndexOf(rec) < 0,
           "B2 ★ **载荷与登记表两边都摘掉了**（`CardInstance.HandEffects` + `PlayerState.HandEffectRecords`）"
         + "—— 只摘一边会让「后进手牌的牌」又被补上同一条（`RemoveHandEffectsFromSource` 的 doc）"
         + "｜🧨 把 `inst.HandEffects.RemoveAll(...)` 那半删掉 ⇒ 载荷还在 ⇒ 本行红");

        // 反向（灭自证）：**非 extrinsic** 的同来源效果不该被摘
        var keep = new CardInstance.HandEffect
        {
            Source = src,
            Extrinsic = false,                                  // 非 extrinsic ⇒ 挂在**目标**身上，施放者离场也不该撤
            RecordId = ctx.NextHandEffectRecord(),
        };
        ctx.Players[0].HandEffectRecords.Add(keep);
        inst.HandEffects.Add(keep);
        int removed2 = RuleCore.BroadcastRemoveEffectsFrom(ctx, src);
        Tr(removed2 == 0 && ctx.Players[0].HandEffectRecords.Contains(keep) && inst.HandEffects.Contains(keep),
           "B3 ★★（灭自证）**非 extrinsic** 的同来源手牌效果**一条都不摘**（原版那道 `extrinsic != 0` 闸）"
         + "｜🧨 把 `RemoveHandEffectsFromSource` 里 `if (r == null || !r.Extrinsic) continue;` 删掉 ⇒ 实得 1 ⇒ 本行红");
    }

    // ============================================================ C 先查后排队的手牌半（账 482 ⓑ）
    static void SectionC_先查后排队的手牌半()
    {
        MenuCheck.Section(_sink, "C 账 482 ⓑ：「先查后排队」的**手牌两跳**（原版 `BattleManager__RemoveExtrinsicEffectsFrom.c:73/:91`）");

        var ctx = NewCtx(888, UnitCard("W21HandC"));
        Tr(FirstHand(ctx, 0) != null, "C0 **前提断言**：P0 手牌里有实例（0 张 ⇒ C1..C2 没跑）");
        if (FirstHand(ctx, 0) == null) return;

        const string src = "W21Caster";
        Tr(!RuleCore.HasHandExtrinsicFrom(ctx, src),
           "C1a 前提：还没挂任何东西 ⇒ 查不到");

        var rec = new CardInstance.HandEffect
        {
            Source = src,
            Extrinsic = true,
            RecordId = ctx.NextHandEffectRecord(),
        };
        ctx.Players[1].HandEffectRecords.Add(rec);      // 挂在**对手**手牌上（原版第二跳 `GetCardsInHandRef(!IsPlayerTurn())`）

        Tr(RuleCore.HasHandExtrinsicFrom(ctx, src),
           "C1 ★ 「先查」**看得见手牌那两跳**（只在手上挂着时也要入队那条 47）"
         + "｜🧨 把 `HasHandExtrinsicFrom` 改成恒 false（= 只查棋盘）⇒ 本行红");
        Tr(!RuleCore.HasHandExtrinsicFrom(ctx, "W21Nobody"),
           "C2 ★（灭自证）**别的来源名查不到**（谓词不是恒真）"
         + "｜🧨 把 `HasHandExtrinsicFrom` 改成恒 true ⇒ 本行红");
    }
}
