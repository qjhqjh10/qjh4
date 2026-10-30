// B103W1_Assert.cs — 第一百零三会话 · 波 1 · 执行代理 `B103_W1`
//   账 `日常/奖励:39`（`ChallengeLogMgr.LogPlayTactic` 的下游一环）· 账 `日常/奖励:786`（`CardPlayedCount` 整族）
//
// ============================ 它盯的是什么 ============================
//  ① **`CardPlayedSignal` 这个 DTO 的形状**（原版 `dump.cs:58900-58911` 四格，**其中 `CurrentCost(+0x28)` 整格不建** ——
//     原版零消费方，施工单 `判据/B102_R2_日常奖励B类五条.md` §一·b 三源普查）。
//  ② **`CardPlayedSignals` 的发点语义**：`IsPlayer = (seat == LocalSeat)`（承载原版 `BattleAction.isPlayer`），
//     两侧都发、由消费者第一道闸过滤。
//  ③ **`CardPlayedCount.OnCardPlayed` 的六道闸**（顺序 = 判据，⛔ 不许重排）：
//     ① `IsPlayer` ② `cardType` ③ `withTrait` 全含 ④ `keyword` ⑤ **`costCondition` 比 `RawCard.manaCost`（卡面费用）**
//     ⑥ `onlyCountCompanions` ⑦ `spellType` ⇒ `UpdateProgress(1)`。
//     判据 = `decomp_full/CardPlayedCount__OnCardPlayed.c`（全文 77 行）+ `cpp2il/out_cpp2il_dev_ilspy/CardPlayedCount.cs:67-124`。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  🔴 **接 `CardPresentation/Editor/ShellScene.cs` 的聚合行**（`MissionChallenge` 那一族都接在那儿：
//     `ShellScene.cs:11023` 那一行已经挂着 `B98K3Assert.Run()` / `B98A11_Assert.Run()` / `B94B4Assert.Run(...)`）
//     —— 在**同一物理行末尾追加**，⛔ 别加行：
//       B103W1_Assert.Run(); _sink.Pass += B103W1_Assert.Pass; _sink.Fail += B103W1_Assert.Fail;
//  ⚠️ 本文件 `Run()` **自建夹具**（不依赖宿主已建窗），接 `ShellScene` 是因为挑战一族都在那儿，不是为了宿主上下文。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//  · **B4 的两条并置**（seat 0 推进 / seat 1 不推进，`LocalSeat = 0`）：🧨 把 `IsPlayer` 写成恒 `true`
//    ⇒ seat 1 那条**会推进** ⇒ 红。结构上不可能同时满足「只记本地玩家」与「两侧都记」。
//  · **D1 与 D2 并置**（同一张 `Cost = 3` 的卡，`costCondition.Cost = 3` 通过、`= 5` 不通过）：
//    🧨 把 cost 闸改成读**实付/常量**（或干脆删掉这道闸）⇒ 其中必有一条红。
//  · **C3 的 `withTrait = ["ephemeral","pack"]` 全含才过**：🧨 把 `All` 写成 `Any` ⇒ 「只带 ephemeral」那条会推进 ⇒ 红。
//  · **C6 的伴生两向**（非伴生不推进 / 伴生推进）：🧨 删掉 `onlyCountCompanions` 那道闸 ⇒ 前半红。
//  · **A2 的 DTO 字段表**（恰 3 格、名字逐个对上）：🧨 有人「补回」`CurrentCost` ⇒ 字段数变 4 ⇒ 红
//    （这条就是**专门挡**「为了让形状像原版去造一个没人读的死格」那一改）。
//
// ============================ 判据 / 报告 ============================
//  · 施工单 → `资料/完全复刻/判据/B102_R2_日常奖励B类五条.md` §一（§一·① 原版逐句 · §一·④ 六道闸）
//  · 原版字段表 → `d:/2/tools/il2cpp_out/dump.cs:58900-58911`（`CardPlayedSignal`）· `:58863-58876`（`CardPlayedCount`）
using System;
using System.Reflection;
using UnityEngine;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B103_W1`：`CardPlayedSignal` / `CardPlayedSignals` / `CardPlayedCount` 三件的行为自检。</summary>
public static class B103W1_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B103W1] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    /// <summary>一个「存档行 + 事件」的小夹具（原版真链路要的就是这两件，照 `B98A11_Assert.Build` 的形状）。</summary>
    sealed class Fixture
    {
        public MissionEvent ev;
        public MissionChallengeProgress progress;
        public int Value { get { return progress == null ? -1 : progress.currentValue; } }
    }

    /// <summary>造「一个挑战 + 一条进度行 + 一个事件」—— 走**原版的装载口** `MissionEvent.UpdateSaveGame`
    /// （它会 `OnProgressUpdate += handler` + `challenge.Initialize()` ⇒ 信号订阅走的是真链路）。</summary>
    static Fixture Build(MissionChallenge c)
    {
        var persist = new MissionPersistence();
        var p = new MissionChallengeProgress();
        p.entryId = "b103w1";
        p.challengeId = c.GetID();
        p.currentValue = 0;
        p.Challenge = c;
        persist.challengeProgress = new MissionChallengeProgress[] { p };

        var ev = new MissionEvent();
        ev.UpdateSaveGame(persist);

        var f = new Fixture();
        f.ev = ev;
        f.progress = p;
        return f;
    }

    /// <summary>原版卡（`CardDef` 是 `RawCardScript` 的等价物）：`Cost` = `manaCost(+0x78)`。</summary>
    static CardDef Card(string id, int cost, int cardType, int keywordId, int spellType, string[] traits)
    {
        var card = new CardDef(id, id, "unit", "", null, "Test", cost, 1, 1, 0, (string[])null, spellType: spellType);
        card.AttachCardType(cardType);
        card.AttachKeywordId(keywordId);
        if (traits != null) card.AttachCardTraits(traits);
        return card;
    }

    /// <summary>默认参数全开（`anyCardType = true` 由 `.ctor` 给）+ `costCondition` 放宽到 `HigherOrEqual 0`
    /// ⇒ 除了被测那一道闸，别的闸都不挡。</summary>
    static CardPlayedCount Make(string id)
    {
        var c = new CardPlayedCount();
        c.id = id;
        c.rewards = new[] { new ChallengeMilestone { difficulty = 1, targetValue = 5 } };
        c.costCondition = new CostCondition { ComparisonMethod = ChallengeComparison.HigherOrEqual, Cost = 0 };
        c.anyCardType = true;
        c.anyTrait = true;
        c.anyKeyword = true;
        c.anySpellType = true;
        c.onlyCountCompanions = false;
        return c;
    }

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        S("B103_W1：`CardPlayedSignal` / `CardPlayedSignals` / `CardPlayedCount`（账 `日常/奖励:39` · `:786`）");

        // 静态状态的两格借出入口（`ValidateMatchConditions` 的注入点）—— 别让本机玩家进度影响判绿
        var savedFirstTwo = MissionChallenge.FirstTwoTutorialCompleted;
        var savedPlayMode = MissionChallenge.MatchPlayMode;
        var savedReplay = MissionChallenge.MatchIsReplay;
        var savedTutorial = MissionChallenge.MatchIsTutorial;
        MissionChallenge.FirstTwoTutorialCompleted = () => true;
        MissionChallenge.MatchPlayMode = null;      // `null` ⇒ `ValidateMatchConditions` 直接放行（原版「没在打局」那一支）
        MissionChallenge.MatchIsReplay = null;
        MissionChallenge.MatchIsTutorial = null;

        CardPlayedSignals.ResetForTest();           // 全局静态总线：本段自己管，⛔ 别带上别的宿主的残留

        try
        {
            SectionA_DtoShape();
            SectionB_PrereqAndDefaults();
            SectionC_SixGates();
            SectionD_CostUsesPrintedCost();
            SectionE_RaiseFromCardPlayed();
        }
        finally
        {
            CardPlayedSignals.ResetForTest();
            MissionChallenge.FirstTwoTutorialCompleted = savedFirstTwo;
            MissionChallenge.MatchPlayMode = savedPlayMode;
            MissionChallenge.MatchIsReplay = savedReplay;
            MissionChallenge.MatchIsTutorial = savedTutorial;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  A · DTO 形状（原版四格去掉零消费方那一格 ⇒ 恰 3 格）
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void SectionA_DtoShape()
    {
        S("A · `CardPlayedSignal` 的字段表");

        var fs = typeof(CardPlayedSignal).GetFields(BindingFlags.Public | BindingFlags.Instance);
        T(fs.Length == 3, "A1 公开实例字段**恰 3 格**（原版四格里 `CurrentCost(+0x28)` 零消费方 ⇒ 不建；实得 " + fs.Length + "）");

        bool hasIsPlayer = false, hasRawCard = false, hasCompanion = false, hasCost = false;
        for (int i = 0; i < fs.Length; i++)
        {
            if (fs[i].Name == "IsPlayer") hasIsPlayer = true;
            if (fs[i].Name == "RawCard") hasRawCard = true;
            if (fs[i].Name == "CurrentCardIsCompanion") hasCompanion = true;
            if (fs[i].Name == "CurrentCost" || fs[i].Name == "Cost") hasCost = true;
        }
        T(hasIsPlayer, "A2 有 `IsPlayer`（原版 `+0x10`）");
        T(hasRawCard, "A3 有 `RawCard`（原版 `+0x18`，我们 = `CardDef`）");
        T(hasCompanion, "A4 有 `CurrentCardIsCompanion`（原版 `CurrentCard(+0x20)` 唯一被读的那一位）");
        T(!hasCost, "A5 ⛔ **没有** `CurrentCost` / `Cost` 字段（原版零消费方 —— 造它就是没人读的死格）");

        // 载荷能造出来、三格取值如实映射
        var card = Card("B103W1Dto", 3, CardDef.CardTypeMinion, 35, 260, new[] { "Ephemeral" });
        var sig = new CardPlayedSignal(true, card, true);
        T(sig.IsPlayer && ReferenceEquals(sig.RawCard, card) && sig.CurrentCardIsCompanion,
          "A6 构造函数三格原样落位（`isPlayer` / `rawCard` / `currentCardIsCompanion`）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  B · 前提 + `.ctor` 默认值（原版逐字：`HigherOrEqual` + `anyCardType = true`）
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void SectionB_PrereqAndDefaults()
    {
        S("B · 前提（硬断言）与 `.ctor` 默认值");

        var fresh = new CardPlayedCount();
        T(fresh.costCondition != null, "B1 默认值：`costCondition` 缺省已 new（原版 `.ctor` 逐字）");
        T(fresh.costCondition != null && fresh.costCondition.ComparisonMethod == ChallengeComparison.HigherOrEqual,
          "B2 默认值：`costCondition.ComparisonMethod = HigherOrEqual(10)`（⛔ 不是 EqualTo）");
        T(fresh.costCondition != null && fresh.costCondition.Cost == 0, "B3 默认值：`costCondition.Cost = 0`（原版没给）");
        T(fresh.anyCardType, "B4 默认值：`anyCardType = true`（= 不筛卡型；原版 `.ctor` 逐字）");
        T(!fresh.anyTrait && !fresh.anyKeyword && !fresh.anySpellType,
          "B5 默认值：`anyTrait/anyKeyword/anySpellType` 全 `false`（⇒ 默认实例什么都数不到，原版就是如此）");

        var c = Make("b103w1-prereq");
        var f = Build(c);
        T(f.progress != null, "B6 （前提）进度行取到了（原版 `UpdateSaveGame` 按 `challengeId` 找行）");
        T(CardPlayedSignals.ListenerCount > 0, "B7 （前提）`Initialize()` 之后信号总线上有监听器（= `OnInitialize` 真注册了）");
        if (f.progress == null)
        {
            S("B6 前提不成立 ⇒ C/D/E 无法执行");
            return;
        }
        T(f.Value == 0, "B8 起点：`currentValue == 0`");
        c.OnReset();
        T(CardPlayedSignals.ListenerCount == 0, "B9 `OnReset()` 之后监听器摘光了（`Unregister` 生效）");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  C · 六道闸（顺序 = 判据；⛔ 不许重排）
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void SectionC_SixGates()
    {
        S("C · `OnCardPlayed` 的六道闸");

        // 一台「全开」的卡与挑战（除被测闸外都不挡）
        var card = Card("B103W1All", 3, CardDef.CardTypeMinion, 35, 260, null);

        // ① IsPlayer
        {
            var c = Make("b103w1-g1"); var f = Build(c);
            if (f.progress == null) { T(false, "C1 前提：进度行取不到"); }
            else
            {
                CardPlayedSignals.Raise(new CardPlayedSignal(false, card, false));
                T(f.Value == 0, "C1 闸①：`IsPlayer = false` ⇒ 不变（`.c:23`）");
                c.OnReset();
            }
        }

        // ② cardType
        {
            var c = Make("b103w1-g2");
            c.anyCardType = false; c.attackingType = CardDef.CardTypeTactic;   // 只要战术卡
            var f = Build(c);
            if (f.progress == null) { T(false, "C2 前提：进度行取不到"); }
            else
            {
                CardPlayedSignals.Raise(new CardPlayedSignal(true, Card("B103W1Minion", 3, CardDef.CardTypeMinion, 35, 260, null), false));
                T(f.Value == 0, "C2 闸②：卡型 minion ≠ 要的 tactic ⇒ 不变（`.c:24-29`）");
                CardPlayedSignals.Raise(new CardPlayedSignal(true, Card("B103W1Tactic", 3, CardDef.CardTypeTactic, 35, 260, null), false));
                T(f.Value == 1, "C3 闸②：卡型 tactic 命中 ⇒ +1（⛔ 不是按卡表 `Type` 字符串比）");
                c.OnReset();
            }
        }

        // ③ withTrait 全含
        {
            var c = Make("b103w1-g3");
            c.anyTrait = false; c.withTrait = new[] { "ephemeral", "pack" };
            var f = Build(c);
            if (f.progress == null) { T(false, "C4 前提：进度行取不到"); }
            else
            {
                CardPlayedSignals.Raise(new CardPlayedSignal(true, Card("B103W1T1", 3, CardDef.CardTypeMinion, 35, 260, new[] { "Ephemeral" }), false));
                T(f.Value == 0, "C4 闸③：只带 ephemeral（缺 pack）⇒ 不变（`All`，⛔ 不是 `Any`）");
                CardPlayedSignals.Raise(new CardPlayedSignal(true, Card("B103W1T2", 3, CardDef.CardTypeMinion, 35, 260, new[] { "Ephemeral", "Pack" }), false));
                T(f.Value == 1, "C5 闸③：ephemeral + pack 全含 ⇒ +1");
                c.OnReset();
            }
        }

        // ④ keyword
        {
            var c = Make("b103w1-g4");
            c.anyKeyword = false; c.keyword = 35;   // `Keyword.Beast`
            var f = Build(c);
            if (f.progress == null) { T(false, "C6 前提：进度行取不到"); }
            else
            {
                CardPlayedSignals.Raise(new CardPlayedSignal(true, Card("B103W1Kw12", 3, CardDef.CardTypeMinion, 12, 260, null), false));
                T(f.Value == 0, "C6 闸④：`keyword` 12 ≠ 35 ⇒ 不变（`.c:40-45`）");
                CardPlayedSignals.Raise(new CardPlayedSignal(true, Card("B103W1Kw35", 3, CardDef.CardTypeMinion, 35, 260, null), false));
                T(f.Value == 1, "C7 闸④：`keyword` 35 ⇒ +1");
                c.OnReset();
            }
        }

        // ⑤ costCondition（卡面费用）—— 详细两向在 D 段，这里只断「不过 ⇒ 不变」
        {
            var c = Make("b103w1-g5");
            c.costCondition = new CostCondition { ComparisonMethod = ChallengeComparison.HigherOrEqual, Cost = 4 };
            var f = Build(c);
            if (f.progress == null) { T(false, "C8 前提：进度行取不到"); }
            else
            {
                CardPlayedSignals.Raise(new CardPlayedSignal(true, Card("B103W1C3", 3, CardDef.CardTypeMinion, 35, 260, null), false));
                T(f.Value == 0, "C8 闸⑤：`Compare(HigherOrEqual, 3, 4)` 为假 ⇒ 不变（`.c:48-52`）");
                c.OnReset();
            }
        }

        // ⑥ onlyCountCompanions
        {
            var c = Make("b103w1-g6");
            c.onlyCountCompanions = true;
            var f = Build(c);
            if (f.progress == null) { T(false, "C9 前提：进度行取不到"); }
            else
            {
                CardPlayedSignals.Raise(new CardPlayedSignal(true, card, false));
                T(f.Value == 0, "C9 闸⑥：`onlyCountCompanions` 且**不是**伴生 ⇒ 不变（`.c:53-66`）");
                CardPlayedSignals.Raise(new CardPlayedSignal(true, card, true));
                T(f.Value == 1, "C10 闸⑥：是伴生 ⇒ +1");
                c.OnReset();
            }
        }

        // ⑦ spellType
        {
            var c = Make("b103w1-g7");
            c.anySpellType = false; c.spellType = 260;   // `CombatElixir`
            var f = Build(c);
            if (f.progress == null) { T(false, "C11 前提：进度行取不到"); }
            else
            {
                CardPlayedSignals.Raise(new CardPlayedSignal(true, Card("B103W1Sp0", 3, CardDef.CardTypeMinion, 35, 0, null), false));
                T(f.Value == 0, "C11 闸⑦：`spellType` 0 ≠ 260 ⇒ 不变（`.c:67-72`）");
                CardPlayedSignals.Raise(new CardPlayedSignal(true, Card("B103W1Sp260", 3, CardDef.CardTypeMinion, 35, 260, null), false));
                T(f.Value == 1, "C12 闸⑦：`spellType` 260 ⇒ +1");
                c.OnReset();
            }
        }

        // 全开 ⇒ 累加（不是覆盖）
        {
            var c = Make("b103w1-all"); var f = Build(c);
            if (f.progress == null) { T(false, "C13 前提：进度行取不到"); }
            else
            {
                CardPlayedSignals.Raise(new CardPlayedSignal(true, card, false));
                CardPlayedSignals.Raise(new CardPlayedSignal(true, card, false));
                CardPlayedSignals.Raise(new CardPlayedSignal(true, card, false));
                T(f.Value == 3, "C13 六闸全过 ⇒ 三条信号累加到 3（`UpdateProgress(1)` 推进不是覆盖）");
                c.OnReset();
            }
        }

        // 空 costCondition：原版 NRE ⇒ 我们出声 + 不推进
        {
            var c = Make("b103w1-nullcost");
            c.costCondition = null;
            var f = Build(c);
            if (f.progress == null) { T(false, "C14 前提：进度行取不到"); }
            else
            {
                CardPlayedSignals.Raise(new CardPlayedSignal(true, card, false));
                T(f.Value == 0, "C14 空 `costCondition`：不推进（原版 NRE ⇒ 同样不计数），⛔ 不崩");
                c.OnReset();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  D · cost 闸读的是**卡面费用**（`RawCard.manaCost`），⛔ 不是实付 / 常量
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void SectionD_CostUsesPrintedCost()
    {
        S("D · 闸⑤ 读**卡面费用**（原版 `RawCardScript.manaCost(+0x78)`，⛔ 不是 `CurrentCost`）");

        var card3 = Card("B103W1Cost3", 3, CardDef.CardTypeMinion, 35, 260, null);

        {
            var c = Make("b103w1-cost-eq3");
            c.costCondition = new CostCondition { ComparisonMethod = ChallengeComparison.EqualTo, Cost = 3 };
            var f = Build(c);
            if (f.progress == null) { T(false, "D1 前提：进度行取不到"); }
            else
            {
                CardPlayedSignals.Raise(new CardPlayedSignal(true, card3, false));
                T(f.Value == 1, "D1 卡面 3 费 + `EqualTo 3` ⇒ +1（读的就是 `CardDef.Cost`）");
                c.OnReset();
            }
        }
        {
            var c = Make("b103w1-cost-eq5");
            c.costCondition = new CostCondition { ComparisonMethod = ChallengeComparison.EqualTo, Cost = 5 };
            var f = Build(c);
            if (f.progress == null) { T(false, "D2 前提：进度行取不到"); }
            else
            {
                CardPlayedSignals.Raise(new CardPlayedSignal(true, card3, false));
                T(f.Value == 0, "D2 同一张 3 费卡 + `EqualTo 5` ⇒ 不变（与 D1 并置 = 灭自证：闸若读常量/实付，必有一条红）");
                c.OnReset();
            }
        }
        {
            // `LowerOrEqual` 的另一侧（`Compare` 的判定树在本类之外，这条只钉「用的是卡面费用」这一个量）
            var c = Make("b103w1-cost-le");
            c.costCondition = new CostCondition { ComparisonMethod = ChallengeComparison.LowerOrEqual, Cost = 3 };
            var f = Build(c);
            if (f.progress == null) { T(false, "D3 前提：进度行取不到"); }
            else
            {
                CardPlayedSignals.Raise(new CardPlayedSignal(true, card3, false));
                T(f.Value == 1, "D3 卡面 3 费 + `LowerOrEqual 3` ⇒ +1（边界值也算）");
                c.OnReset();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════
    //  E · 发点：`RaiseFromCardPlayed` 的 `IsPlayer = (seat == LocalSeat)`
    // ═══════════════════════════════════════════════════════════════════════════════════════
    static void SectionE_RaiseFromCardPlayed()
    {
        S("E · 发点（`CardPlayedSignals.RaiseFromCardPlayed`）与 `LocalSeat`");

        var card = Card("B103W1Raise", 1, CardDef.CardTypeMinion, 35, 260, null);
        var inst = new CardInstance(card, 1);

        var c = Make("b103w1-raise"); var f = Build(c);
        if (f.progress == null) { T(false, "E0 前提：进度行取不到"); return; }

        CardPlayedSignals.LocalSeat = 0;
        CardPlayedSignals.RaiseFromCardPlayed(1, card, inst);   // 对面打的
        T(f.Value == 0, "E1 `LocalSeat = 0`：座位 1 打的 ⇒ 不推进（`IsPlayer = false`）");

        CardPlayedSignals.RaiseFromCardPlayed(0, card, inst);   // 本机打的
        T(f.Value == 1, "E2 `LocalSeat = 0`：座位 0 打的 ⇒ +1（与 E1 并置 = 灭自证：`IsPlayer` 恒 true 时 E1 会红）");

        CardPlayedSignals.LocalSeat = 1;                        // 模拟联机客机
        CardPlayedSignals.RaiseFromCardPlayed(1, card, inst);
        T(f.Value == 2, "E3 `LocalSeat = 1`：座位 1 打的 ⇒ +1（证明判的是 `LocalSeat`、不是硬编码 0）");

        CardPlayedSignals.LocalSeat = 0;                        // 还原（后面的断言/别的宿主都按单机口径）
        c.OnReset();

        // 发点本身（`RaiseFromCardPlayed`）在无监听器时不许抛、不许出声
        CardPlayedSignals.ResetForTest();
        CardPlayedSignals.RaiseFromCardPlayed(0, card, null);
        T(CardPlayedSignals.ListenerCount == 0, "E4 无监听器时发一条（`inst = null`）⇒ 安静返回、不抛（提交/回放路径不会因为没人订而炸）");

        // `card == null` ⇒ 直接不发（调用点拿不到卡片时别发一条必 NRE 的信号）
        int before = _sink.Fail;
        CardPlayedSignals.RaiseFromCardPlayed(0, null, null);
        T(_sink.Fail == before, "E5 `RawCard` 为 null ⇒ 不发信号（原版这一档消费者会 NRE）");
    }
}
