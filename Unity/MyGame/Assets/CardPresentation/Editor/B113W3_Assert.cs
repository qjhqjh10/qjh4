// B113W3_Assert.cs — 执行代理 `B113W3`（第一百一十三会话 · 第 1 波）· 账 `B-126`
//
// ============================ 它盯的是什么 ============================
//  **限时关键词在效果投影里印两行。** 生产端 `EffectResolver.ApplyOneGain.c` 的限时档（`EffectResolver.cs:12454-12472`）
//  **同时**做两件事：① `ApplyTraitEffect(...)` 往 `_traitEffects` 里落/并一条 `TraitEffectRecord`，
//  ② 把那**同一条记录**挂到 `TempBuff{TraitRec = appliedTrait}` 上。
//  而投影 `EffectsToShow.Of` 的两支各出一行 Trait：
//    · ② 支（`EffectsToShow.cs:243-278`，`TempBuffs` / `IsKeyword`）—— 读 `b.TraitRec.Trait`，`Seq = b.Seq`；
//    · ③ 支（`EffectsToShow.cs:298-326`，`TraitEffects`）—— 这就是原版 `activeEffects(+0x108)` 的**正本**。
//  ⇒ 同一件事印两遍。实跑坐实（本件改前）：`BL44 Daemonic Pact` 的 `invulnerable` ×2、
//    `Da Bigger Dey Iz...` 的 `stomp` ×2、`SW49 Fenrisian Blizzard` 的 `blind` ×6 个单位每单位 ×2。
//
//  修法（唯一一处）：`Of()` 的 ② 支里，若 `b.TraitRec` **就是** `u.TraitEffects` 里的某一个实例 ⇒ ② 不出行
//  （③ 照出）。① 支对 `StatMod` 早就有同形状的去重（`timedMods`，`EffectsToShow.cs:215-227`）—— 本件把关键词那半补齐。
//  ⛔ **不在 ③ 删行**：③ 是正本。
//
//  🔴 **身份只认 `ReferenceEquals`**（`UnitState.ApplyTraitEffect:2980-2995` 建好就 `_traitEffects.Add(rec); return rec`，
//     合并进既有记录时返回的也是同一个实例）—— 按 `Trait` 字符串比会把「同名、不同 `TimingBits` / `MarkOfChaosType` /
//     `Extrinsic` 的两条记录」误并成一条（合并键见 `UnitState.FindTraitEffect:3006-3021`），那比重复更糟。
//
// ==================== 宿主接线（一行；⛔ 本代理不改宿主）====================
//  在 `Assets/CardPresentation/Editor/BattleScene.cs` 的 `BattleScene.Run()` 里那条超长聚合行
//  （末尾是 `Debug.Log(P + $"=== 合计：…")`；本件不写死行号，按「`=== 合计：` 那一行」找）——
//  **同一物理行**追加（行数零变化）：
//
//      B113W3_Assert.Run(); pass += B113W3_Assert.Pass; fail += B113W3_Assert.Fail;
//
//  🔴 **追加必须落在该行第一个 `//` 之前**（本仓已踩过 6 次「代码被行末 `//` 吞掉」，症状是账闸全绿而断言一条没跑）。
//  ⚠️ 本件**不建 GameObject / 不用 `BattleDriver` / 不用 `BattleContext`**（纯静态投影 + 纯状态位），任何宿主都能跑。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · 把 `Of()` ② 支那半句 `|| TraitRecAlreadyShown(u, b.TraitRec)` 删掉（= 回到改前）
//    ⇒ **A2 / A2-a / A2-b / B1 / B2 / C1 / C2 全红**；而 **D1 / E1 仍然绿**（它们不走去重那条路）—— 两面独立。
//  · 把身份判据从 `ReferenceEquals` 改成比 `Trait` 字符串
//    ⇒ **C1 红**（`blind` 的两条**不同**记录被并成 1 行），A / B / D / E 仍绿 —— 结构上区分得开。
//  · 把 `TraitRecAlreadyShown` 改成「只要 `TraitRec != null` 就 true」
//    ⇒ **D1 红**（记录已不在 `_traitEffects` 里时那一行凭空消失），其余全绿。
//  · 把 ③ 支整段删掉（错误修法：改在正本上）⇒ **A2 / A2-a / D1 全红**（行没了），B2 / C1 的数字也不对。
//
// ==================== 期望值从哪来（⛔ 不许拿我们的常量证明我们的实现）====================
//  · 「应当 1 行」的期望来自**原版 `activeEffects(+0x108)` 的条目数**：生产端对一次 `addTrait` 只落**一条**
//    `CardEffect`（`decomp_full/CardScript__AddEffect.c:469-568` 的合并循环，命中既有记录就并、不新增）⇒
//    投影里就该是 1 行。⛔ 不是「因为我们去重了所以 1 行」。
//  · 夹具**照生产端的形状**搭（`ApplyTraitEffect` 真落记录 + `TempBuff{TraitRec = 那条}`），
//    ⛔ 不是手搓一个「看起来像」的 `ShownEffect`。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;

/// <summary>执行代理 `B113W3`：账 `B-126` —— 限时关键词在 `EffectsToShow.Of` 里被 ②/③ 两支各印一遍。</summary>
public static class B113W3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B113W3] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>跑过的断言里通过的条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    // ============================================================ 夹具
    static List<CardDef> _pool;

    /// <summary>卡池里按名字取 `CardDef`（找不到 ⇒ `null`；调用方必须先断前提）。</summary>
    static CardDef Def(string cardName)
    {
        if (_pool == null) _pool = CardDatabase.Load();
        var d = CardDatabase.Find(_pool, cardName);
        Tr(d != null, $"（夹具前提）卡池里找得到「{cardName}」");
        return d;
    }

    /// <summary>一张干净单位（`Deathwing Terminator`：投影里本来 0 行 —— 见 `B103W3_Assert` 的 C0，这里再断一次）。</summary>
    static UnitState NewUnit(string cardName)
    {
        var d = Def(cardName);
        return d == null ? null : new UnitState(d, false);
    }

    /// <summary>
    /// **照生产端形状**造一条限时关键词：先 `ApplyTraitEffect` 落记录（= `EffectResolver.cs:12221`），
    /// 再 `AddTempBuff{TraitRec = 那条记录}`（= `EffectResolver.cs:12454-12472`）。返回 `UnitState`。
    /// </summary>
    static UnitState TimedKeyword(UnitState u, string trait, int value, int timingBits,
                                 out UnitState.TraitEffectRecord rec, out UnitState.TempBuff buff)
    {
        rec = u.ApplyTraitEffect(trait, value, timingBits: timingBits, source: "B113W3_SRC");   // 落/并 `_traitEffects`
        buff = new UnitState.TempBuff
        {
            IsKeyword = true,
            Name = trait,
            Value = value,
            TraitRec = rec,                                   // ⚠️ 生产端就是这条（同一条记录的两个把手）
            Src = "战术卡",
            SourceCard = "B113W3_CARD",
            UntilMyNextTurn = true,
        };
        u.AddTempBuff(buff);                                  // 会给 `buff.Seq` 取一个新号（≠ `rec.Seq`）
        return u;
    }

    /// <summary>投影里某个关键词的 Trait 行（保持投影自己的顺序）。</summary>
    static List<ShownEffect> TraitRows(UnitState u, string trait)
    {
        var outp = new List<ShownEffect>();
        var rows = EffectsToShow.Of(u, true);
        for (int i = 0; i < rows.Count; i++)
            if (rows[i].Family == ShownFamily.Trait && rows[i].Trait == trait) outp.Add(rows[i]);
        return outp;
    }

    static int CountFamily(UnitState u, ShownFamily fam)
    {
        var rows = EffectsToShow.Of(u, true);
        int n = 0;
        for (int i = 0; i < rows.Count; i++) if (rows[i].Family == fam) n++;
        return n;
    }

    /// <summary>记录是不是 `_traitEffects` 里的那**一个实例**（不是「有一个同名的」）。</summary>
    static bool SameInstanceInList(UnitState u, UnitState.TraitEffectRecord rec)
    {
        var recs = u.TraitEffects;
        for (int i = 0; i < recs.Count; i++) if (object.ReferenceEquals(recs[i], rec)) return true;
        return false;
    }

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        Section_ProductionShape();
        Section_TwoDifferentTraits();
        Section_SameNameTwoRecords();
        Section_RecordNotInList();
        Section_NotNarrowed();
    }

    // ============================================================ A · 生产形状：1 条限时关键词 ⇒ 1 行
    static void Section_ProductionShape()
    {
        S("B113W3 · A：限时关键词只印 **1 行**（账 `B-126`；判据 = 原版 `activeEffects` 一次 `addTrait` 只落一条 `CardEffect`）");

        var u = NewUnit("Deathwing Terminator");
        Tr(u != null, "A0 前提：造得出一张干净单位");
        if (u == null) return;
        C(EffectsToShow.Of(u, true).Count, 0, "A0-a 前提：干净单位本来 0 行（否则下面的计数没意义）");

        UnitState.TraitEffectRecord rec; UnitState.TempBuff buff;
        TimedKeyword(u, "invulnerable", 1, 1 /*untilEndOfTurn*/, out rec, out buff);

        Tr(rec != null, "A1 前提：`ApplyTraitEffect` 真返回了一条记录（`TraitRec` 非 null 才走 ② 的去重）");
        Tr(SameInstanceInList(u, rec), "A1-a 前提：那条记录**确实在** `u.TraitEffects` 里，且是**同一个实例**（③ 会出它）");
        C(u.TempBuffs.Count, 1, "A1-b 前提：`TempBuffs` 里正好 1 条");
        Tr(u.TempBuffs.Count == 1 && object.ReferenceEquals(u.TempBuffs[0].TraitRec, rec),
           "A1-c 前提：`TempBuff.TraitRec` **就是**刚那条记录（生产端 `EffectResolver.cs:12471` 的形状）");
        Tr(buff.Seq != rec.Seq, "A1-d 前提：`TempBuff.Seq` 与 `TraitEffectRecord.Seq` 是两个不同的号（否则 A2-a 区分不了哪一支出的行）");

        var rows = TraitRows(u, "invulnerable");
        C(rows.Count, 1, "A2 `invulnerable` **只出 1 行**（改前 = 2 行：② 一条 + ③ 一条）");
        if (rows.Count >= 1)
        {
            C(rows[0].Seq, rec.Seq, "A2-a 留下的那一行是 **③ 那条记录**（`Seq == rec.Seq`），⛔ 不是 ② 的 `b.Seq`");
            C(rows[0].Timed, true, "A2-b 那一行仍是「限时」（`Timed=true`）—— 去重没把它降成永久");
            C(rows[0].TraitValue, 1, "A2-c 那一行的 `TraitValue` 没变（1）");
        }
    }

    // ============================================================ B · 两条不同关键词 ⇒ 各 1 行（防一刀切全并）
    static void Section_TwoDifferentTraits()
    {
        S("B113W3 · B：**两条不同的关键词**仍然各出 1 行（防「一刀切全并」）");

        var u = NewUnit("Deathwing Terminator");
        Tr(u != null, "B0 前提：造得出一张干净单位");
        if (u == null) return;

        UnitState.TraitEffectRecord r1, r2; UnitState.TempBuff b1, b2;
        TimedKeyword(u, "invulnerable", 1, 1, out r1, out b1);
        TimedKeyword(u, "vulnerable", 2, 1, out r2, out b2);
        Tr(r1 != null && r2 != null, "B0-a 前提：两条记录都落下来了");
        C(u.TraitEffects.Count, 2, "B0-b 前提：`_traitEffects` 里是 2 条（不同关键词不合并）");

        C(TraitRows(u, "invulnerable").Count, 1, "B1 `invulnerable` 出 1 行");
        C(TraitRows(u, "vulnerable").Count, 1, "B1-a `vulnerable` 出 1 行（⛔ 别被前一条的去重带走）");
        C(CountFamily(u, ShownFamily.Trait), 2, "B2 Trait 族合计 **2 行**（两个关键词各 1 行）");
    }

    // ============================================================ C · 同名两条不同记录 ⇒ 各 1 行（防按字符串去重）
    static void Section_SameNameTwoRecords()
    {
        S("B113W3 · C：**同名但两条不同记录**各自留 1 行（身份只认 `ReferenceEquals`；合并键见 `UnitState.FindTraitEffect:3006-3021`）");

        var u = NewUnit("Deathwing Terminator");
        Tr(u != null, "C0 前提：造得出一张干净单位");
        if (u == null) return;

        // 同 trait、不同 `TimingBits` ⇒ `FindTraitEffect` 不会合并 ⇒ 两条独立记录（= 原版两条 `CardEffect`）。
        var recTimed = u.ApplyTraitEffect("blind", 1, timingBits: 1, source: "B113W3_TIMED");
        var recPerm = u.ApplyTraitEffect("blind", 1, timingBits: 0, source: "B113W3_PERM");
        Tr(recTimed != null && recPerm != null, "C0-a 前提：两条 `blind` 记录都落下来了");
        Tr(!object.ReferenceEquals(recTimed, recPerm), "C0-b 前提：它们是**两个不同实例**（同 trait、不同 `TimingBits`）");
        C(u.TraitEffects.Count, 2, "C0-c 前提：`_traitEffects` 里是 2 条");

        var buff = new UnitState.TempBuff
        {
            IsKeyword = true, Name = "blind", Value = 1, TraitRec = recTimed,
            Src = "战术卡", SourceCard = "B113W3_CARD", UntilMyNextTurn = true,
        };
        u.AddTempBuff(buff);

        var rows = TraitRows(u, "blind");
        C(rows.Count, 2, "C1 `blind` 出 **2 行**（③ 的两条记录各占一行；只该丢掉 ② 那条重复 ⇒ 按名字比会错成 1 行）");
        bool seenTimed = false, seenPerm = false;
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Seq == recTimed.Seq) seenTimed = true;
            if (rows[i].Seq == recPerm.Seq) seenPerm = true;
        }
        Tr(seenTimed, "C2 两条里有一条的 `Seq == recTimed.Seq`（限时那条记录照出）");
        Tr(seenPerm, "C2-a 另一条的 `Seq == recPerm.Seq`（永久那条记录照出 —— ⛔ 去重不能连它一起吞）");
    }

    // ============================================================ D · 记录不在 `_traitEffects` 里 ⇒ 照旧从 ② 出行
    static void Section_RecordNotInList()
    {
        S("B113W3 · D：`TraitRec` 指向的记录**已不在** `_traitEffects` 里 ⇒ 仍然出行（防「有 `TraitRec` 就无条件吞」）");

        var u = NewUnit("Deathwing Terminator");
        Tr(u != null, "D0 前提：造得出一张干净单位");
        if (u == null) return;

        // 只造「孤儿」记录：**不**经过 `ApplyTraitEffect` ⇒ 它不在 `_traitEffects` 里（对应「记录已到期被摘、`TempBuff` 还没收」那一瞬）。
        var orphan = new UnitState.TraitEffectRecord
        {
            Trait = "blind", BuffType = 3, TraitValue = 1, TimingBits = 1,
            Source = "B113W3_ORPHAN", Seq = 777,
        };
        var buff = new UnitState.TempBuff
        {
            IsKeyword = true, Name = "blind", Value = 1, TraitRec = orphan,
            Src = "战术卡", SourceCard = "B113W3_CARD", UntilMyNextTurn = true,
        };
        u.AddTempBuff(buff);

        C(u.TraitEffects.Count, 0, "D0-a 前提：`_traitEffects` 里 **0 条**（③ 什么都不会出）");
        var rows = TraitRows(u, "blind");
        C(rows.Count, 1, "D1 `blind` 仍出 **1 行**（只能来自 ②）");
        if (rows.Count >= 1)
            C(rows[0].Seq, buff.Seq, "D1-a 那一行是 ② 出的（`Seq == b.Seq`），⛔ 不是拿 `TraitRec.Seq` 冒充");
    }

    // ============================================================ E · 不误伤：非关键词的限时属性行照旧
    static void Section_NotNarrowed()
    {
        S("B113W3 · E：**非关键词**的限时属性行没被这次去重误伤（①/② 之间原有的 `timedMods` 去重保持原状）");

        var u = NewUnit("Deathwing Terminator");
        Tr(u != null, "E0 前提：造得出一张干净单位");
        if (u == null) return;

        u.AddTempBuff(new UnitState.TempBuff { IsKeyword = false, Name = "attack", Value = 2, Src = "B113W3" });

        C(CountFamily(u, ShownFamily.Stat), 1, "E1 限时属性出 **1 行** Stat（① 跳过被 `TempBuff` 引用住的那条 `Mod`、② 出这一行 —— 与改前逐字一致）");
        C(CountFamily(u, ShownFamily.Trait), 0, "E1-a 不凭空多出 Trait 行");
    }
}
