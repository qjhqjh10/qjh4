// B98N3_Assert.cs — 执行代理 `B98_N3`（第九十八会话 · 波 12 · 引擎线）的断言
//
// ============================ 它盯的是什么 ============================
//   用户裁定（2026-10-29）：「这三个我们现在**逐条复刻，原版里有吗，一切按照原版**。」
//   只读代理 `B98_N2` 查清（⛔ 别重查）→ `资料/完全复刻/判据/B98_N2_ward与resolution.md`：
//     · 原版对 `ward`(140) / `wardWb`(145) / `tainted`(160) / `summonRift`(730) 四词**都有真闸**；
//       **原版没有 `Implemented` 这种清单**（那是我们自建的脚手架）⇒ 判据只能是「有没有执行分支」。
//     · `ward` / `wardWb` / `tainted` 我们**已与原版同覆盖**；`summonRift` **缺一处攻击侧闸** ⇒ 本批补上。
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · 攻击侧 `summonRift` = `d:/2/tools/decomp_full/BattleManager__IsValidAttackTarget.c:211-213`
//     （`HasCurrentTrait(param_3 /*目标*/, 0x2da) ⇒ return 0`；**裸闸、不分侧、不分近战/远程**）
//   · 能力侧那两条 = `BattleManager__IsValidActiveAbilityTarget.c:55-56`（同形裸闸）
//   · 落点 = `RuleEngine/Core/RuleCore.cs` 的 `IsValidTarget` 返回表达式（行内块，行数零变化）
//           ＋ `RuleEngine/Core/CardDef.cs` 的 `KeywordTable.Implemented`
//
// ============================ 怎么用（宿主）============================
//   `B98N3_Assert.Run(); pass += B98N3_Assert.Pass; fail += B98N3_Assert.Fail;`
//   · 纯引擎断言：**不建 GameObject、不建资产、不写盘**，只用 `new BattleContext(seed)` 的内存局面。
//   · 接线由**调度台**做（本件⛔ 不改宿主）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · B1：删掉 `IsValidTarget` 尾闸里 `SummonRift` 那半 ⇒ 红。
//   · B2：把闸写成 `if (!ranged && target.Has(...))` ⇒ 红（这是「不分近战/远程」的唯一判别式）。
//   · B3：B1/B2 若因**别的闸**才拒（例如把夹具摆成了先锋）⇒ B3 也红 ⇒ 说明 B1/B2 是空转。
//   · B4：把判据写成 `attacker.Has(SummonRift)`（读错侧）⇒ 红。
//   · C1/C2：能力侧被削弱（同侧放行）⇒ C2 红。
//   · D1/D2：从 `Implemented` 删任一词 ⇒ D1 红；把 `UnimplementedKeywords` 改成恒空 ⇒ D2 红。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B98_N3`：`summonRift` 攻击侧闸 + 四词入 `KeywordTable.Implemented`。
/// 计数口径照 `CheckSink`（与 `B98A2_Assert` / `B98G2_Assert` 同一套）。</summary>
public static class B98N3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[N3] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具
    /// <summary>一张普通单位卡（关键词走 `CardDef` 构造的第 11 个形参，与 `RuleEngineTest.Unit` 同一口径）。</summary>
    static CardDef Unit(string id, params string[] kws)
    {
        return new CardDef(id, id, "unit", "", null, "Test", 1, 1, 5, 0, kws);
    }

    /// <summary>把一个单位摆到场上（跳过部署流程；口径同 `RuleEngineTest.Place` / `B98G2_Assert.Place`）。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>读回格号（⛔ 不假设位置）。</summary>
    static int SlotOf(BattleContext ctx, int p, UnitState u)
    {
        for (int s = 0; s < BoardSpec.Size; s++)
            if (ReferenceEquals(ctx.Players[p].Board[s], u)) return s;
        return -1;
    }

    // ============================================================ 入口
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        MenuCheck.Section(_sink, "B98_N3 `summonRift` 攻击侧闸 + ward/wardWb/tainted/summonRift 四词入 `Implemented`");

        SectionA_前提();
        SectionB_攻击侧();
        SectionC_能力侧();
        SectionD_名单生效();
    }

    // ============================================================ A · 前提（硬断言先行）
    static void SectionA_前提()
    {
        MenuCheck.Section(_sink, "A · 前提（登记表 + 夹具真的挂上了）");

        Tr(KeywordTable.Implemented.Contains(KeywordTable.SummonRift),
           "A1 ★ 前提：`summonRift` 已在 `KeywordTable.Implemented`（否则攻击侧闸再对，也没「登记」这一层）");

        Tr(KeywordTable.Implemented.Contains(ConditionEval.Ward)
           && KeywordTable.Implemented.Contains(ConditionEval.WardWb)
           && KeywordTable.Implemented.Contains(ConditionEval.Tainted),
           "A2 ★ 前提：`ward` / `wardWb` / `tainted` 三词也已在 `Implemented` —— 🧨 拿掉任一条 ⇒ 红");

        var rift = Unit("N3Rift", "summonRift");
        Tr(rift.Keywords.ContainsKey(KeywordTable.SummonRift),
           "A3 ★ 前提：夹具卡的 `Keywords` 真的有 `summonRift`（`Normalize` 没把它吃掉）"
         + "｜🧨 这一条红 ⇒ 下面的 B1/B2 是**空转**（别把绿的当成对）");
    }

    // ============================================================ B · 攻击侧（被测）
    static void SectionB_攻击侧()
    {
        MenuCheck.Section(_sink, "B · 攻击侧 `summonRift` 闸 — 判据 `BattleManager__IsValidAttackTarget.c:211-213`");

        var ctx = new BattleContext(0xB98_0031);
        ctx.Players[0] = new PlayerState(); ctx.Players[1] = new PlayerState();
        ctx.Active = 0;
        var atk = Place(ctx, 0, 3, Unit("N3Atk"));
        var rift = Place(ctx, 1, 5, Unit("N3RiftTgt", "summonRift"));
        var plain = Place(ctx, 1, 3, Unit("N3PlainTgt"));
        int atkSlot = SlotOf(ctx, 0, atk);
        int riftSlot = SlotOf(ctx, 1, rift);
        int plainSlot = SlotOf(ctx, 1, plain);

        Tr(atkSlot >= 0 && riftSlot >= 0 && plainSlot >= 0,
           "B0 ★ 前提：三个夹具都读回了格号（≥0）—— ⛔ 别让下面拿 -1 去调（那会变成「参数非法」的假红）");

        C(RuleCore.IsValidTarget(ctx, 0, atkSlot, 1, riftSlot, ranged: false), RuleCodes.ErrTarget,
          "B1 ★★★ 近战攻击**带 `summonRift` 的敌方目标** ⇒ `ErrTarget`"
        + "（原版 `:211-213` `HasCurrentTrait(param_3, 0x2da) ⇒ return 0`）"
        + "｜🧨 删掉 `IsValidTarget` 尾闸里 `SummonRift` 那半 ⇒ 红");

        C(RuleCore.IsValidTarget(ctx, 0, atkSlot, 1, riftSlot, ranged: true), RuleCodes.ErrTarget,
          "B2 ★ 远程**也**拒 —— 原版那道闸是**裸的**、不区分攻击类型"
        + "｜🧨 写成 `if (!ranged && target.Has(SummonRift))` ⇒ 红（「不分近战/远程」的唯一判别式）");

        C(RuleCore.IsValidTarget(ctx, 0, atkSlot, 1, plainSlot, ranged: true), RuleCodes.OK,
          "B3 对照：**同一局面**下普通敌方目标仍合法 ⇒ B1/B2 的拒绝确实来自 `summonRift` 那一词，不是别的闸");

        // ---- 灭自证：原版读的是**目标**（param_3），不是攻方（param_2）----
        var ctx2 = new BattleContext(0xB98_0032);
        ctx2.Players[0] = new PlayerState(); ctx2.Players[1] = new PlayerState();
        ctx2.Active = 0;
        var riftAtk = Place(ctx2, 0, 3, Unit("N3RiftAtk", "summonRift"));
        var tgt2 = Place(ctx2, 1, 5, Unit("N3Tgt2"));
        int riftAtkSlot = SlotOf(ctx2, 0, riftAtk);
        int tgt2Slot = SlotOf(ctx2, 1, tgt2);
        Tr(riftAtkSlot >= 0 && tgt2Slot >= 0, "B4 前提：夹具读回格号（≥0）");
        C(RuleCore.IsValidTarget(ctx2, 0, riftAtkSlot, 1, tgt2Slot, ranged: true), RuleCodes.OK,
          "B4 ★ 灭自证：**攻方自己**带 `summonRift` ⇒ 攻击普通目标**照常合法**"
        + "（原版读 `param_3` 目标，不是 `param_2` 攻方）"
        + "｜🧨 把判据写成 `attacker.Has(SummonRift)` ⇒ 红");
    }

    // ============================================================ C · 能力侧不许被削弱
    static void SectionC_能力侧()
    {
        MenuCheck.Section(_sink, "C · 能力侧**不许被削弱**（`RuleEngineTest_EngineE7.E7Case2SummonRift` 的镜像）");

        var ctx = new BattleContext(0xB98_0033);
        ctx.Players[0] = new PlayerState(); ctx.Players[1] = new PlayerState();
        ctx.Active = 0;
        var caster = Place(ctx, 0, 3, Unit("N3Caster"));
        var foeRift = Place(ctx, 1, 5, Unit("N3FoeRift", "summonRift"));
        var allyRift = Place(ctx, 0, 1, Unit("N3AllyRift", "summonRift"));
        int foeRiftSlot = SlotOf(ctx, 1, foeRift);
        int allyRiftSlot = SlotOf(ctx, 0, allyRift);
        Tr(foeRiftSlot >= 0 && allyRiftSlot >= 0, "C0 前提：两个 `summonRift` 夹具读回格号（≥0）");

        C(RuleCore.CanUseAbilityTarget(ctx, 0, caster, 1, foeRiftSlot), RuleCodes.ErrTarget,
          "C1 ★ 异侧 `summonRift` 候选仍不可点（`BattleManager__IsValidActiveAbilityTarget.c:55-56`）");

        C(RuleCore.CanUseAbilityTarget(ctx, 0, caster, 0, allyRiftSlot), RuleCodes.ErrTarget,
          "C2 ★ **同侧**也拒 —— 那道闸**不分侧**，与 E7Case2 同一判别式，⛔ 别让它被削弱");
    }

    // ============================================================ D · 入表生效
    static void SectionD_名单生效()
    {
        MenuCheck.Section(_sink, "D · 入表生效（`UnimplementedKeywords` 不再报这四词）");

        var four = Unit("N3Four", "ward", "wardWb", "tainted", "summonRift");
        Tr(four.Keywords.Count >= 4,
           "D0 ★ 前提：四词都进了这张卡的 `Keywords`（≥4）—— 少了说明 `Normalize` 吃掉了某个词");
        C(RuleCore.UnimplementedKeywords(new[] { four }).Count, 0,
          "D1 ★ 一张同时带四词的卡 ⇒ 「未实现关键词」**0 条**"
        + "｜🧨 从 `Implemented` 删掉任一词 ⇒ 红");

        var bogus = Unit("N3Bogus", "firststrike");
        Tr(bogus.Keywords.ContainsKey("firststrike"),
           "D2 前提：`firststrike` 认得出来（`Prefixes` 有它、**故意不进 `Implemented`**）");
        Tr(RuleCore.UnimplementedKeywords(new[] { bogus }).Count >= 1,
           "D3 ★ 灭自证：`firststrike` **仍然**被报成未实现"
         + "｜🧨 把 `UnimplementedKeywords` 改成恒返回空 ⇒ 红（那会让 D1 也变成假绿）");
    }
}
