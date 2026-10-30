// B98G2_Assert.cs — 执行代理 `B98_G2`（第九十八会话 · 波 5b · 战斗线写手）的断言集
//
// ============================ 它盯的是什么（四条 + 一）============================
//   账 `战斗逻辑驱动` 的四条（全部落在 `RuleEngine/Core/EffectResolver.cs`，本次都追加在**类尾**）：
//     · `:645`  `BattleManagerSupport.BroadcastUnitAttacked` 的 ④⑤ **手牌两跳**
//                → `RuleCore.BroadcastUnitAttackedHandHops(ctx, attacked, actingCard)`
//     · `:832①` `RecordGrant` 的**施放者实参**（原版 `CardEffect.enchantingCard(+0x18) = AbilityData.thisCard(+0x10)`）
//                → `RuleCore.GrantCasterCardName(ctx)`
//     · `:534②` `ta = 60`（`inCemeteryFiltered`）的**墓地候选**
//                → `RuleCore.ResolveCemeteryTargets(ctx, owner, spec, op)` ＋ `ResolveTargets` 不再扫棋盘
//     · `:482ⓒ` `WhileInPlayVariable(240)` 的「跑不跑得起来」= **逐 ability 真链**
//                → `RuleCore.WhileInPlayVariableAbilityRunnable(ctx, owner, slot, u)`
//
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_R10_波5残差归属.md` §二
//                            ＋ `资料/完全复刻/判据/B98_R4_波5补判据.md` §482
//                            ＋ `资料/完全复刻/报告/B98_G2.md`
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A3：把 `BroadcastUnitAttackedHandHops` 里的 trigger 名从 `"UnitAttack"` 改回施工单里那个
//         `"UnitAttacked"` ⇒ `AbilityTriggerPresence` 一路回「未知」 ⇒ A4/A5 的 `attempts` 变 0 ⇒ 红。
//   · A5：把两跳合成一跳（只扫 `ctx.Active` 那一侧手牌）⇒ A5（2）红、A4（1）仍绿。
//   · B1：把 `GrantCasterCardName` 的优先级翻过来（先 `CreationSource`）⇒ B1 红、B2 仍绿。
//   · C4：把 `ResolveCemeteryTargets` 换成「扫棋盘/扫 `DeadUnits`」的实现 ⇒ C4 红（那张被放逐的牌会重新出现）。
//   · C6：把 `ResolveTargets` 的 `ta == 60` 分支删掉（退回扫棋盘）⇒ C6 得 1 张（棋盘那个单位）而期望 0 ⇒ 红。
//   · D3/D4：删掉 `WhileInPlayVariableAbilityRunnable` 里的 `IsRightContext` 那一句
//         ⇒ D3 从 `false` 变 `true` ⇒ 红；把「判不了」当成 `false` ⇒ D1 红。
//
// ============================ 怎么用（宿主）============================
//   `B98G2_Assert.Run(); _sink.Pass += B98G2_Assert.Pass; _sink.Fail += B98G2_Assert.Fail;`
//   · 推荐宿主 = `RuleEngine/Editor/RuleEngineTest.cs` 的 `RuleEngineTest.Run`（账的引擎面都在那儿）；
//     ⚠️ **本批白名单不含任何宿主** ⇒ 这一行要**调度台**加，加之前本文件一条都不跑（如实标）。
//   · 本文件**不建 GameObject、不建资产、不写盘**；只用 `new BattleContext(seed)` 的**内存局面**
//     （`CardInstance.Detached` 不走对局计数器时用 `ctx.NewInstance`，两者都在内存里）。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>B98_G2：`EffectResolver.cs` 那四条的断言（账 645 / 832① / 534② / 482ⓒ）。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B98G2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[G2] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }
    public static int RunCount { get { return _sink.Pass + _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具
    static CardDef Unit(string id)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 3, 0, null, subtype: "Infantry");
    }

    static CardDef Tactic(string id)
    {
        var c = new CardDef(id, id, "tactic", "", "common", "Test", 1, 0, 0, 0, null);
        c.AttachCardType(CardDef.CardTypeTactic);        // `GetCardsInCemetery` 的 ④ 闸读的是**这一格**（`-1` 未知 ⇒ 放行）
        return c;
    }

    /// <summary>一张挂了 `trigger 50 (UnitAttack)` 的卡；<paramref name="ctx"/> = 它的 `abilityContext`。</summary>
    static CardDef AttackTriggerCard(string id, int ctx)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 3, 0, null, subtype: "Infantry",
                           abilityContext: new[]
                           {
                               new CardDef.AbilityContextSpec { Trigger = 50, Name = "UnitAttack", Context = ctx },
                           },
                           abilityContextsVersioned: true,
                           abilityTriggers: new[] { 50 },
                           abilityTriggersVersioned: true);
    }

    /// <summary>一张挂了 `trigger 240 (WhileInPlayVariable)` 的卡。<paramref name="ctx"/> = `-2` ⇒ **不落** context 行。</summary>
    static CardDef WhileInPlayVariableCard(string id, int ctx)
    {
        var contexts = new List<CardDef.AbilityContextSpec>();
        if (ctx != NoContextRow)
            contexts.Add(new CardDef.AbilityContextSpec { Trigger = 240, Name = "WhileInPlayVariable", Context = ctx });
        return new CardDef(id, id, "unit", "", "common", "Test", 2, 2, 5, 0, null, subtype: "Infantry",
                           abilityContext: contexts,
                           abilityContextsVersioned: true,
                           abilityTriggers: new[] { 240 },
                           abilityTriggersVersioned: true,
                           abilityTargets: new[]
                           {
                               new CardDef.AbilityTargetSpec { Trigger = 240, TargetsAffected = new[] { 30 } },
                           });
    }

    const int NoContextRow = -2;

    /// <summary>把一个单位摆到场上（跳过部署流程；口径同 `RuleEngineTest.Place`）。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static EffectTargetSpec Spec(string side, string kind)
    {
        return new EffectTargetSpec { Raw = side + " " + kind, Side = side, Kind = kind, Count = 1 };
    }

    // ============================================================ 入口
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        MenuCheck.Section(_sink, "B98_G2 `EffectResolver.cs` 四条：645 手牌两跳 / 832① 施放者实参 / "
                              + "534② `ta=60` 墓地候选 / 482ⓒ `240` 逐 ability 真链");

        // ---- D0：批处理前提（本文件不写盘，但宿主跑在批处理里才有意义：Edit 模式没有对局循环）
        Tr(Application.isBatchMode,
            "D0 ★ 前提：**必须在 `-batchmode` 下跑**（本文件只碰内存局面，非批处理不会落盘 —— "
          + "红这一条说明宿主接错了，下面的结论请重跑一次再读）");

        try
        {
            SectionA_645手牌两跳();
            SectionB_832施放者实参();
            SectionC_534墓地目标();
            SectionD_482逐ability真链();
        }
        finally
        {
            EffectResolverAsRuleCore_ResetHandCounters();
        }
        Debug.Log(_sink.P + "B98_G2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败（共 " + RunCount + " 条）");
    }

    static void EffectResolverAsRuleCore_ResetHandCounters()
    {
        RuleCore.ResetHandAbilityCountersForTest();
    }

    // ============================================================ A · 账 645
    static void SectionA_645手牌两跳()
    {
        MenuCheck.Section(_sink, "A · 账 645 `BroadcastUnitAttacked` 的 ④⑤ 手牌两跳");

        // A1/A2 前提：trigger 名必须是 `UnitAttack`（施工单里那个 `UnitAttacked` 是错的）
        C(CardDef.AbilityTriggerNumber("UnitAttack"), 50,
          "A1 ★ 前提：`AbilityTrigger` 名字表里 **50 = `UnitAttack`**（`dump.cs:20250` / `CardDef.cs:1920`）");
        Tr(CardDef.AbilityTriggerNumber("UnitAttacked") < 0,
           "A2 ★ 前提：`UnitAttacked` **不是**合法的 trigger 名（< 0）—— 施工单/账 645 备注里那个写法"
         + "会让 `AbilityTriggerPresence` 一路回「未知」⇒ 手牌跳**一条都不入队**（静默空转），本笔用的是 `UnitAttack`");

        // A3 前提：夹具确实挂上了这条 trigger
        var c50 = AttackTriggerCard("G2Hand50", CardDef.AbilityContextHand);
        C(c50.AbilityTriggerPresence("UnitAttack"), 1,
          "A3 ★ 前提：夹具卡的 `AbilityTriggerPresence(\"UnitAttack\") == 1`（否则下面两条测的是别的闸）");

        var ctx = new BattleContext(0xB98_0001);
        ctx.Players[0] = new PlayerState(); ctx.Players[1] = new PlayerState();
        ctx.Active = 0;
        var attacker = Place(ctx, 0, 4, Unit("G2Attacker"));
        var attacked = Place(ctx, 1, 4, Unit("G2Attacked"));

        // A4：只在一侧手牌放一张 ⇒ 恰好扫到 1 次（证明两跳真的走到了手牌、且 presence 闸放行）
        ctx.Players[0].Hand.Add(ctx.NewInstance(c50));
        RuleCore.ResetHandAbilityCountersForTest();
        int n1 = RuleCore.BroadcastUnitAttackedHandHops(ctx, attacked, attacker);
        C(n1, 0, "A4 手牌跳返回 0（手牌卡没有正文 ⇒ 不入队 —— 原版 `OnTrigger` 内部同样空转）");
        C(RuleCore_HandAttempts(), 1,
          "A4（同族）`HandAbilityTriggerAttempts += 1` —— 两跳**真的扫了这一侧手牌**且 presence 闸放行");

        // A5：两侧各一张 ⇒ 恰好 2 次（灭自证：把两跳合成一跳 ⇒ 这里读到 1）
        ctx.Players[1].Hand.Add(ctx.NewInstance(c50));
        RuleCore.ResetHandAbilityCountersForTest();
        RuleCore.BroadcastUnitAttackedHandHops(ctx, attacked, attacker);
        C(RuleCore_HandAttempts(), 2,
          "A5 ★ 两侧手牌各 1 张 ⇒ `attempts == 2`（原版 ④ `GetCardsInHandRef(IsPlayerTurn())` + ⑤ `GetCardsInHandRef(!IsPlayerTurn())`）"
        + " —— 🧨 只扫一侧 ⇒ 读到 1 ⇒ 红");

        // A6 灭自证：没有这条 trigger 的卡 ⇒ 一次都不尝试（证明 A4/A5 读的是 presence 闸、不是「扫了就计数」）
        var noTrig = Unit("G2NoTrig");
        var ctx2 = new BattleContext(0xB98_0002);
        ctx2.Players[0] = new PlayerState(); ctx2.Players[1] = new PlayerState();
        ctx2.Active = 0;
        var atk2 = Place(ctx2, 0, 4, Unit("G2Attacker2"));
        var tgt2 = Place(ctx2, 1, 4, Unit("G2Attacked2"));
        ctx2.Players[0].Hand.Add(ctx2.NewInstance(noTrig));
        ctx2.Players[1].Hand.Add(ctx2.NewInstance(noTrig));
        RuleCore.ResetHandAbilityCountersForTest();
        C(RuleCore.BroadcastUnitAttackedHandHops(ctx2, tgt2, atk2), 0, "A6 没有 `UnitAttack` 的卡 ⇒ 返回 0");
        C(RuleCore_HandAttempts(), 0,
          "A6 ★ 灭自证：`attempts == 0` —— 闸挡在**presence**那一层（不是「扫了就计数」）");

        // A7 空值安全（不许抛）
        C(RuleCore.BroadcastUnitAttackedHandHops(null, attacked, attacker), 0, "A7 `ctx == null` ⇒ 0（不抛）");
        C(RuleCore.BroadcastUnitAttackedHandHops(ctx, null, attacker), 0, "A7（同族）`attackedUnit == null` ⇒ 0（不抛）");
    }

    static int RuleCore_HandAttempts()
    {
        return RuleCore.HandAbilityTriggerAttemptsForTest;
    }

    // ============================================================ B · 账 832 ①
    static void SectionB_832施放者实参()
    {
        MenuCheck.Section(_sink, "B · 账 832① `RecordGrant` 的施放者 = 原版 `CardEffect.enchantingCard(+0x18)`");

        var ctx = new BattleContext(0xB98_0011);
        ctx.Players[0] = new PlayerState(); ctx.Players[1] = new PlayerState();
        var casterCard = Unit("G2CasterCard");
        var playedCard = Unit("G2PlayedCard");
        var u = Place(ctx, 0, 4, casterCard);

        var prevActing = ctx.ActingUnit;
        var prevCreation = ctx.CreationSource;
        try
        {
            // B1：`ActingUnit` 优先（= 原版 `AbilityData.thisCard` = **能力属主**）
            ctx.ActingUnit = u;
            ctx.CreationSource = playedCard;
            C(RuleCore.GrantCasterCardName(ctx), casterCard.Name,
              "B1 ★ `ActingUnit`（能力属主）优先 ⇒ 施放者 = 它的卡名"
            + " —— 🧨 把优先级翻成 `CreationSource` 优先 ⇒ 读到 G2PlayedCard ⇒ 红");

            // B2：战术卡那条路（没有棋盘单位）⇒ 回落到 `CreationSource`
            ctx.ActingUnit = null;
            C(RuleCore.GrantCasterCardName(ctx), playedCard.Name,
              "B2 没有 `ActingUnit` 时回落到 `CreationSource`（= 战术卡 / 常驻效果那条路）");

            // B3：两边都没有 ⇒ null（⛔ 不编一个名字）
            ctx.CreationSource = null;
            C(RuleCore.GrantCasterCardName(ctx), null,
              "B3 `ActingUnit` 与 `CreationSource` 都为空 ⇒ **null**（⛔ 不猜、不拿 `PlayingCard` 顶）");

            // B4：`ctx == null` 不抛
            C(RuleCore.GrantCasterCardName(null), null, "B4 `ctx == null` ⇒ null（不抛）");
        }
        finally
        {
            ctx.ActingUnit = prevActing;
            ctx.CreationSource = prevCreation;
        }
    }

    // ============================================================ C · 账 534 ②
    static void SectionC_534墓地目标()
    {
        MenuCheck.Section(_sink, "C · 账 534② `ta = 60`（inCemeteryFiltered）的墓地候选 + `ResolveTargets` 不扫棋盘");

        var ctx = new BattleContext(0xB98_0021);
        ctx.Players[0] = new PlayerState(); ctx.Players[1] = new PlayerState();
        ctx.Active = 0;
        var mineUnit = Unit("G2MineUnit");
        var mineTactic = Tactic("G2MineTactic");
        var foeUnit = Unit("G2FoeUnit");
        var mineInst = ctx.NewInstance(mineUnit);
        ctx.Players[0].Discard.Add(mineInst);
        ctx.Players[0].Discard.Add(ctx.NewInstance(mineTactic));
        ctx.Players[1].Discard.Add(ctx.NewInstance(foeUnit));

        // C1 前提：墓地里确实是 3 张（1 张被放逐之前）
        C(RuleCore.GetCardsInCemetery(ctx, true, true, true, true, true, true).Count, 3,
          "C1 ★ 前提：`GetCardsInCemetery(全开)` 读到 3 张（我方单位 + 我方战术 + 敌方单位）");

        // C2：己方 troop ⇒ 只我方那张单位卡
        var mine = RuleCore.ResolveCemeteryTargets(ctx, 0, Spec("own", "troop"), null);
        C(mine.Count, 1, "C2 `Side=own, Kind=troop` ⇒ 1 张（战术卡被卡种筛掉）");
        Tr(mine.Count == 1 && mine[0] != null && mine[0].Card != null && mine[0].Card.Id == mineUnit.Id,
           "C2（同族）命中的就是我方那张单位卡（" + (mine.Count == 1 && mine[0] != null && mine[0].Card != null ? mine[0].Card.Id : "—") + "）");

        // C3：敌方 ⇒ 对面那张（侧别翻转）
        var foe = RuleCore.ResolveCemeteryTargets(ctx, 0, Spec("enemy", "troop"), null);
        C(foe.Count, 1, "C3 `Side=enemy` ⇒ 1 张（对方墓地）");
        Tr(foe.Count == 1 && foe[0] != null && foe[0].Card != null && foe[0].Card.Id == foeUnit.Id,
           "C3（同族）命中的是对方那张（" + (foe.Count == 1 && foe[0] != null && foe[0].Card != null ? foe[0].Card.Id : "—") + "）");

        // C4 灭自证：被「永久放逐」的牌从墓地查询里消失（= 原版 `+0x24b shouldCheckIfIsInCemetery == 0`）
        ctx.Players[0].MarkRemovedFromCemetery(mineInst);
        var afterExile = RuleCore.ResolveCemeteryTargets(ctx, 0, Spec("own", "troop"), null);
        C(afterExile.Count, 0,
          "C4 ★ 灭自证：把那张标成「已从墓地取走」(`MarkRemovedFromCemetery` = 原版 `+0x24b = 0`) ⇒ 候选变 0"
        + " —— 🧨 换成「手扫 `Discard` / `DeadUnits`」的实现 ⇒ 它又会冒出来 ⇒ 红");

        // C5/C6：`ResolveTargets` 碰上 `ta = 60` **不再扫棋盘**（候选不在棋盘上 ⇒ 返回空表）
        ctx.Players[0].Board[5] = null;
        Place(ctx, 0, 5, Unit("G2BoardDecoy"));            // 棋盘上放一个「长得对」的单位当诱饵
        var decoySpec = Spec("own", "troop");
        var opNoTa = new EffectOp { Verb = "return", Source = "G2Fixture", Target = decoySpec, SubjectTargetsAffected = -1 };
        var opTa60 = new EffectOp { Verb = "return", Source = "G2Fixture", Target = decoySpec, SubjectTargetsAffected = 60 };
        C(RuleCore.ResolveTargets(ctx, 0, decoySpec, null, null, null, opNoTa).Count, 1,
          "C5 前提（对照组）：`ta` 不是 60 时，同一个 spec 在棋盘上选中那个诱饵 ⇒ 1 张");
        C(RuleCore.ResolveTargets(ctx, 0, decoySpec, null, null, null, opTa60).Count, 0,
          "C6 ★ `ta == 60` ⇒ **0 张**（墓地里的牌不在棋盘上；原版这一支的候选来自 `GetCardsInCemetery`）"
        + " —— 🧨 删掉那条分支（退回扫棋盘）⇒ 读到 1 ⇒ 红");
    }

    // ============================================================ D · 账 482 ⓒ
    static void SectionD_482逐ability真链()
    {
        MenuCheck.Section(_sink, "D · 账 482ⓒ `WhileInPlayVariable(240)` 的「跑不跑得起来」= 逐 ability 真链");

        // D1 前提 + D2：列没落 ⇒ `判不了` ⇒ **不摘**（⛔ 不许把「判不了」当成「不满足」）
        var ctx = new BattleContext(0xB98_0031);
        ctx.Players[0] = new PlayerState(); ctx.Players[1] = new PlayerState();
        ctx.Active = 0;
        var bare = Unit("G2Bare240");
        var uBare = Place(ctx, 0, 4, bare);
        Tr(bare.AbilityRowsForTrigger("WhileInPlayVariable") == null,
           "D1 ★ 前提：这张卡**没落** `abilityTargets` 列 ⇒ `AbilityRowsForTrigger` 回 `null`（= 判不了）");
        Tr(RuleCore.WhileInPlayVariableAbilityRunnable(ctx, 0, 4, uBare),
           "D2 ★ 判不了 ⇒ **返回 true（不摘）** —— 🧨 把「判不了」当 `false` ⇒ 整族 240 的 extrinsic 效果被静默摘掉 ⇒ 红");

        // D3：`abilityContext = 10 (InHand)` 的 240 卡**在场上** ⇒ 明确跑不起来（`IsRightContext` 拒）
        var handCtx = WhileInPlayVariableCard("G2Trig240Hand", CardDef.AbilityContextHand);
        var uHand = Place(ctx, 0, 5, handCtx);
        var rows = handCtx.AbilityRowsForTrigger("WhileInPlayVariable");
        Tr(rows != null && rows.Count == 1, "D3 ★ 前提：那张卡的 `abilityRows(240)` 恰好 1 行");
        C(handCtx.AbilityTriggerPresence("WhileInPlayVariable"), 1,
          "D3 ★ 前提：`AbilityTriggerPresence(\"WhileInPlayVariable\") == 1`");
        Tr(!RuleCore.WhileInPlayVariableAbilityRunnable(ctx, 0, 5, uHand),
           "D3 ★ `abilityContext = 10 (InHand)` 的 240 卡**在场上** ⇒ 跑不起来 ⇒ **返回 false（摘）**"
         + " —— 🧨 删掉那条 `IsRightContext` 闸 ⇒ 变 true ⇒ 红");

        // D4 灭自证：同一张卡、只是**不落** context 行 ⇒ v14「缺行 = Play(0)」⇒ 在场合法 ⇒ 跑得起来
        var noRow = WhileInPlayVariableCard("G2Trig240Play", NoContextRow);
        var uNoRow = Place(ctx, 0, 6, noRow);
        Tr(RuleCore.WhileInPlayVariableAbilityRunnable(ctx, 0, 6, uNoRow),
           "D4 ★ 灭自证：同样的卡**不落** `abilityContext` 行 ⇒ 缺行 = `Play(0)` ⇒ 在场合法 ⇒ 返回 true"
         + " —— 🧨 说明 D3 的红不是「卡没数据」，而是那条区域闸真的在判");

        // D5 空值安全
        Tr(RuleCore.WhileInPlayVariableAbilityRunnable(ctx, 0, -1, null), "D5 `u == null` ⇒ true（不摘，不抛）");
    }
}
