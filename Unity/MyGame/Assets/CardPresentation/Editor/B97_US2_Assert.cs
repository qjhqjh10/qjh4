// B97_US2_Assert.cs — 第九十七会话 · 执行代理 `B97_US2`
//
// ============================ 它盯的是什么 ============================
// 本批落在 `RuleEngine/Core/UnitState.cs` 的两件（判据 → `资料/完全复刻/判据/GetCardEffectsToShow_施工细则.md` §三 S2
// ＋ `资料/完全复刻/判据/B97_战斗剩余_按文件施工单.md` §1·4；逐条证据 → `资料/完全复刻/报告/B97_US2.md`）：
//   · **`特效动画:521` 的 S2「加序戳」** —— 原版 `activeEffects(+0x108)` 是**一条** `List<CardEffect>`，出表序 =
//     `List.Add` 序（`decomp_full/EntityScript__GetCardEffectsToShow.c:34` 按下标遍历）。我们把它拆成
//     `_statMods` / `_traitEffects` / `_tempBuff` / 光环 六七本账 ⇒ 每条记录要带一枚单调递增的 `Seq` 才能还原出表顺序。
//     记账点 = `AddStatMod` / `ApplyTraitEffect`（**只新建那一支**）/ `AddTempBuff` / `AddAuraKeyword`。
//     🔶 自己设计：`_auraSeq` / `_auraKwSeq` 两个「序戳快照表」—— 原版没有「重算」这件事，`Auras.Recompose`
//     是我们的等价物（整份摘掉→重加），不加快照则每次重算光环那几行都取新戳、跳队尾。
//   · **账 `:707` 的 `activeAbilityLoaded`(+0x224) 状态槽** —— 原版 `CardScript.activeAbilityLoaded`
//     （`dump.cs:35452` 的 backing field；写点 `SetBaseCardTraits.c:38` / `ResolveActiveAbilityPlayed.c:122,128`，
//      读点 `IsActiveAbilityTargeted.c:19` / `ResolveActiveAbilityPlayed.c:54` / `GetCurrentLocalizedAbilityDescription.c:43`）。
//     读写点散在 `EffectResolver` / `BattleDriver` / `BattleCardUI`（跨文件）⇒ 本代理只落**状态槽**，今天恒 `false`（休眠）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里那串 `{ Check...(); }` 块中，**紧接 `{ CheckB97USJam(); }`
// 那一行之后**（现读 = 第 `1297` 行；插完在它下面）加一个块：
//       // 🆕 2026-10-29（调度台代接线）：`B97_US2` —— `特效动画:521` S2 加序戳（含光环重算不换戳）＋ 账 707 `activeAbilityLoaded` 槽。
//       { CheckB97US2Seq(); }
//   🔴 **故意不用 `Step(...)`、方法名也不带 `Test`/`Report` 前缀** —— 理由与 `B97_US` / `B97_W1` 两份同一条：
//     `Step(...)` 让 `_ranCases` 记账、`VerifyRunCaseCoverage()` 拿**反射清单**对账，而 `工具/gen_diag_runner.py`
//     只认「8 空格缩进的顶层语句」⇒ 两者都得跟着改。块调用**两条都不碰**。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」都写在文案里）============================
//   · `ApplyTraitEffect` 把 `Seq` 也写进**合并分支** ⇒ 第 2 段红（合并后戳变了）。
//   · `AddAuraKeyword` 不看 `_auraKwSeq` 每次都取新戳（或删掉那行 HasKey 判断）⇒ 第 3 段红。
//   · `ClearAuraGrants` 不把光环属性那份快照进 `_auraSeq`（或 `NextSeqFor` 不看 `_auraSeq`）⇒ 第 4 段红。
//   · `AddStatMod` / `AddTempBuff` 不记账 ⇒ 第 1 段红。
//   · `ActiveAbilityLoaded` 默认值改成 `true` ⇒ 第 5 段首条红。
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，见文件头）。</summary>
    public static void CheckB97US2Seq()
    {
        Section("🆕 B97_US2：`特效动画:521` S2 加序戳（建记录取戳 / 合并不换戳 / 光环重算不换戳）· 账 707 `activeAbilityLoaded` 状态槽");
        B97US2SeqOnRecordCreation();
        B97US2MergeKeepsSeq();
        B97US2AuraKeywordSeqStable();
        B97US2AuraStatSeqStable();
        B97US2ActiveAbilityLoadedSlot();
    }

    // ==================================================================
    //  1 · 三条建记录的路都带序戳，且「先建的小、后建的大」（单调递增）
    //  判据 = `判据/GetCardEffectsToShow_施工细则.md` §三 S2（每条记录取戳那一刻 = 建记录那一刻）
    // ==================================================================
    static void B97US2SeqOnRecordCreation()
    {
        var card = Unit("B97US2A", 1, 3, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97US2B", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;

        int baseSeq = u.NextEffectSeq();
        CheckTrue(baseSeq >= 1, "前提：`NextEffectSeq` 从 1 起、单调（实得 " + baseSeq + "）");

        var mod = u.AddStatMod("attack", 2);
        CheckTrue(mod != null && mod.Seq > baseSeq, "🧨 `AddStatMod` 建的修饰带序戳，且大于取戳前（实得 " + (mod == null ? -1 : mod.Seq) + "）");

        var rec = u.ApplyTraitEffect("huntmark", 1);
        CheckTrue(rec != null, "前提：`ApplyTraitEffect` 挂上了");
        if (rec == null) return;
        CheckTrue(rec.Seq > mod.Seq, "🧨 `ApplyTraitEffect` 新建记录带序戳、且大于上一条（实得 " + rec.Seq + "）");

        var buff = new UnitState.TempBuff { Name = "health", Value = 1, Src = "B97US2A" };
        u.AddTempBuff(buff);
        CheckTrue(buff.Seq > rec.Seq, "🧨 `AddTempBuff` 建的限时增益带序戳、且大于上一条（实得 " + buff.Seq + "）");
    }

    // ==================================================================
    //  2 · 合并进**既有**记录时**不换戳**（原版 `AddEffect` `case 3` 是把新值并进旧那条 `CardEffect`，不是新加一条）
    //  判据 = `decomp_full/CardScript__AddEffect.c:492-568`（倒序扫、命中即合并进旧记录）
    // ==================================================================
    static void B97US2MergeKeepsSeq()
    {
        var card = Unit("B97US2M", 1, 3, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97US2N", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;

        var r1 = u.ApplyTraitEffect("huntmark", 1);
        CheckTrue(r1 != null, "前提：第一次 `huntmark` 挂上");
        if (r1 == null) return;
        int seq1 = r1.Seq;
        CheckTrue(seq1 > 0, "前提：第一条记录带序戳");

        var r2 = u.ApplyTraitEffect("huntmark", 2);   // 同 trait / 同三键 ⇒ 合并进 r1
        CheckTrue(ReferenceEquals(r2, r1), "前提：第二次合并进**同一条**记录（不是新建）");
        Check(r1.TraitValue, 3, "前提：`add` 档累加（1 + 2 = 3）");
        Check(r2.Seq, seq1,
              "🧨 合并进既有记录时**不换戳**（把 `Seq = NextEffectSeq()` 也写进合并分支 ⇒ 实得新戳）");
    }

    // ==================================================================
    //  3 · 光环关键词：`ClearAuraGrants`（整份摘）→ `AddAuraKeyword`（重挂）之后序戳**不换**
    //  🔶 自己设计：`_auraKwSeq` 快照（原版无「重算」这件事，`Auras.Recompose` 是我们的等价物）
    // ==================================================================
    static void B97US2AuraKeywordSeqStable()
    {
        var card = Unit("B97US2K", 1, 3, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97US2L", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;

        u.AddAuraKeyword("flank", 1);
        int k1 = u.AuraKeywordSeq("flank");
        CheckTrue(k1 > 0, "前提：光环给的关键词带序戳（实得 " + k1 + "）");

        u.ClearAuraGrants();
        Check(u.AuraKwValue("flank"), 0, "前提：`ClearAuraGrants` 把光环那份收回了");
        u.AddAuraKeyword("flank", 1);
        Check(u.AuraKeywordSeq("flank"), k1,
              "🧨 光环重算（整份摘 → 重挂）后关键词序戳**不换**（删掉 `AddAuraKeyword` 里的 `_auraKwSeq` 复用 ⇒ 实得新戳）");
    }

    // ==================================================================
    //  4 · 光环属性：`ClearAuraGrants`（撤前快照进 `_auraSeq`）→ `RecordGrant(GrantTag)`（重挂）之后序戳**复用**
    //  🔶 自己设计：`_auraSeq` / `NextSeqFor`（同上）
    // ==================================================================
    static void B97US2AuraStatSeqStable()
    {
        var card = Unit("B97US2S", 1, 3, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97US2T", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;

        var m1 = u.RecordGrant("attack", 2, Auras.GrantTag);
        CheckTrue(m1 != null && m1.Seq > 0, "前提：光环给的属性修饰带序戳");
        if (m1 == null) return;
        int ms1 = m1.Seq;

        u.ClearAuraGrants();
        Check(u.CurrentMeleeAttack, 3, "前提：`ClearAuraGrants` 把光环那份 +2 近战收回了（卡面 3）");
        var m2 = u.RecordGrant("attack", 2, Auras.GrantTag);
        CheckTrue(m2 != null, "前提：重挂成功");
        if (m2 == null) return;
        Check(m2.Seq, ms1,
              "🧨 光环重算后属性修饰序戳**复用了旧戳**（删掉 `ClearAuraGrants` 里那句 `_auraSeq[\"st:\"+Attr]=Seq` 快照 ⇒ 实得新戳）");
    }

    // ==================================================================
    //  5 · 账 `:707` 的 `activeAbilityLoaded`(+0x224) 状态槽：默认 false、可写可复位
    //  判据 = `dump.cs:35452`（`<activeAbilityLoaded>k__BackingField`）· `CardScript__SetBaseCardTraits.c:38`（`= 0` 复位）
    //  ⚠️ 本段只钉**状态槽存在 + 默认值**（读写点跨文件，不在这里）；不断言任何主动能力结算（今天 `requiresLoading` 恒假）。
    // ==================================================================
    static void B97US2ActiveAbilityLoadedSlot()
    {
        var card = Unit("B97US2AL", 1, 3, 5);
        var ctx = Battle(new[] { card }, new[] { Unit("B97US2AM", 1, 1, 5) });
        var u = Place(ctx, 0, 1, card);
        Check(u != null, true, "前提：夹具把单位放上了棋盘");
        if (u == null) return;

        Check(u.ActiveAbilityLoaded, false, "🧨 默认 `false`（`activeAbilityLoaded` 初值；改成 `true` ⇒ 本条红）");
        u.ActiveAbilityLoaded = true;
        Check(u.ActiveAbilityLoaded, true, "状态槽可写可读（原版 `+0x224`）");
        u.ActiveAbilityLoaded = false;
        Check(u.ActiveAbilityLoaded, false, "可复位（`CardScript__SetBaseCardTraits.c:38` 的 `+0x224 = 0`）");
    }
}
