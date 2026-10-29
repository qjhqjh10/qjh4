// B97W36_Assert.cs — 执行代理 B97_W36（第九十七会话 · 波 4-C）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `:482 RemoveExtrinsicEffectsFrom` / `:64 AdjustCardEffect` 的**残余 ⓓ**：
//     原版 `CardScript.RemoveExtrinsicEffectsFrom` 倒序扫的是**一条** `activeEffects(+0x108)`，
//     `addTrait` / `addTraitRemnant`（`buffType 3 / 0x5a`）的记录**也在这张表里** ⇒ 它们同样会「按来源被撤」。
//     我们把这同一张表拆成四本账（`_statMods` / `_buffs` / `_grants` / `_traitEffects`），
//     前三本 `B97_W30` / `B97_W1` 已接进「按来源撤」，**第四本（trait）两边都没接** ⇒ 本批补。
//
//   落点：`UnitState.RevertTraitEffectsFrom(...)`（文件尾第四段）· `UnitState.TraitEffectsContainExtrinsicFrom`
//         · 接线点 `RemoveExtrinsicEffectsFrom`（2260 那一行）· `ContainsExtrinsicEffectsFrom`（末句 `return`）。
//
// 判据（⛔ 别在这里抄第二份）→
//   `资料/完全复刻/判据/B97_战斗剩余_按文件施工单.md` §1·4 / §1·7
//   ＋ `资料/完全复刻/报告/B97_W36.md`；原版方法体 `d:/2/tools/decomp_full/CardScript__RemoveExtrinsicEffectsFrom.c`
//   （全文）· `CardScript__RemoveEffect.c:63-72`（`case 3` 那两道闸）。
//
// ============================ 怎么用（宿主）============================
// `B97W36Assert.Run();`
//   + 宿主里两行（照 `B97W22Assert` 的先例）：
//     `_sink.Pass += B97W36Assert.Pass; _sink.Fail += B97W36Assert.Fail;`
//   · 推荐宿主 = `Editor/RuleEngineTest.cs`（`RuleEngineTest.Run`）—— 本文件**只碰纯引擎**
//     （`UnitState` / `CardDef`），不建 GameObject、不需要场景。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1：删掉 `RevertTraitEffectsFrom` 的 `r.Source != sourceCardName` 匹配 ⇒ A1/A1′/A1″ 红。
//   · B1：删掉 `(extrinsicOnly && !r.Extrinsic)` 那道闸 ⇒ 非 extrinsic 那条也被撤 ⇒ B1/B1′/B1″ 红。
//   · C1：把 `Source` 匹配改成「不看来源」⇒ 两条都被撤 ⇒ C1′ 红。
//   · D1：把 `ContainsExtrinsicEffectsFrom` 末句改回 `return false;` ⇒ D1 红（查说有 / 摘没有的反面）。
//   · E1：把 `RevertTraitEffectsFrom` 改成「只从 `_traitEffects` 列表删、不调 `RemoveTraitEffect`」
//        ⇒ 关键词留着 ⇒ E1 红。
//   · E2′：把 `RemoveTraitEffect` 里的 `Card != null && Card.Has(...)` 闸删掉（改成无条件 `RemoveAll`）
//        ⇒ 卡面自带的 `fast` 被抹 ⇒ E2′ 红。
//   · F1：删掉 `RemoveExtrinsicEffectsFrom` 那一行追加的 `n += RevertTraitEffectsFrom(...)`
//        ⇒ 端到端 n=0、关键词不掉 ⇒ F1/F1′ 红。
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 B97_W36：账 482 / 64 的残余 ⓓ（`_traitEffects` 按来源撤）的断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。</summary>
public static class B97W36Assert
{
    static readonly CheckSink _sink = new CheckSink("[W36] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B97_W36 账 482/64 残余 ⓓ：`_traitEffects`（第四本账）按来源撤 + 查/摘同口径");

        SectionA_按来源撤();
        SectionB_非Extrinsic不撤();
        SectionC_别的来源不动();
        SectionD_查与摘同口径();
        SectionE_关键词真掉与卡面自带保留();
        SectionF_RemoveExtrinsicEffectsFrom端到端();

        Debug.Log(_sink.P + "B97W36 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 夹具
    static CardDef Troop(string name, params string[] keywords)
    {
        return new CardDef(name, name, "troop", "", null, "SpaceWolves", 1, 1, 3, 0,
                           keywords, subtype: "troop");
    }

    // ============================================================ A 按来源撤
    static void SectionA_按来源撤()
    {
        MenuCheck.Section(_sink, "A `RevertTraitEffectsFrom(key)` 只撤「那一张卡给的」addTrait 记录");

        var u = new UnitState(Troop("W36Src"), false);
        u.ApplyTraitEffect("fast", 1, source: "CardA", extrinsic: true);
        u.ApplyTraitEffect("flank", 1, source: "CardB", extrinsic: true);
        C(u.TraitEffects.Count, 2, "A0 前提：记录账里有两条（extrinsic ⇒ 不合并）");

        int n = u.RevertTraitEffectsFrom("CardA", extrinsicOnly: true);
        C(n, 1, "A1 🔴 只撤来源 = CardA 的那一条（删掉 Source 匹配 ⇒ 红）");
        Tr(!u.Has("fast"), "A1″ CardA 给的 `fast` 掉了");
        Tr(u.Has("flank"), "A1′ 另一张卡（CardB）给的 `flank` 还在");
    }

    // ============================================================ B 灭自证：非 extrinsic 不撤
    static void SectionB_非Extrinsic不撤()
    {
        MenuCheck.Section(_sink, "B `extrinsicOnly` 闸：同一来源的**非** extrinsic 记录一条都不摘（灭自证）");

        var u = new UnitState(Troop("W36Extr"), false);
        u.ApplyTraitEffect("fast", 1, source: "CardA");                   // 非 extrinsic
        u.ApplyTraitEffect("flank", 1, source: "CardA", extrinsic: true); // extrinsic
        C(u.TraitEffects.Count, 2, "B0 前提：两条记录（同来源、不同 extrinsic）");

        int n = u.RevertTraitEffectsFrom("CardA", extrinsicOnly: true);
        C(n, 1, "B1 🔴 只撤 extrinsic 那一条（删掉 extrinsicOnly 闸 ⇒ 2 ⇒ 红）");
        Tr(u.Has("fast"), "B1′ 非 extrinsic 的记录**保留**（不是「按来源整份清」）");
        C(u.TraitEffects.Count, 1, "B1″ 记录账还剩 1 条");
    }

    // ============================================================ C 别的来源不动
    static void SectionC_别的来源不动()
    {
        MenuCheck.Section(_sink, "C 不同来源的记录互不牵连");

        var u = new UnitState(Troop("W36Other"), false);
        u.ApplyTraitEffect("fast", 1, source: "CardA", extrinsic: true);
        u.ApplyTraitEffect("flank", 1, source: "CardZ", extrinsic: true);

        int n = u.RevertTraitEffectsFrom("CardZ", extrinsicOnly: true);
        C(n, 1, "C1 只撤 CardZ 那一条");
        Tr(u.Has("fast"), "C1′ CardA 的不动（把来源匹配去掉 ⇒ 两条都被撤 ⇒ 红）");
        C(u.TraitEffects.Count, 1, "C1″ 记录账还剩 1 条");
    }

    // ============================================================ D 查 / 摘同口径
    static void SectionD_查与摘同口径()
    {
        MenuCheck.Section(_sink, "D `ContainsExtrinsicEffectsFrom` 也认第四本账（查说有 ⇔ 摘得掉）");

        var u = new UnitState(Troop("W36Query"), false);
        Tr(!u.ContainsExtrinsicEffectsFrom("CardA"), "D0 前提：没有记录 ⇒ 查说没有");

        u.ApplyTraitEffect("fast", 1, source: "CardA", extrinsic: true);
        Tr(u.ContainsExtrinsicEffectsFrom("CardA"),
           "D1 🔴 第四本账也认（把末句改回 `return false;` ⇒ 红）：否则「查说没有、却能摘掉」");

        u.RevertTraitEffectsFrom("CardA", extrinsicOnly: true);
        Tr(!u.ContainsExtrinsicEffectsFrom("CardA"), "D2 摘完 ⇒ 查说没有（两条同口径）");
    }

    // ============================================================ E 关键词真掉 / 卡面自带保留
    static void SectionE_关键词真掉与卡面自带保留()
    {
        MenuCheck.Section(_sink, "E 摘记录 ⇒ 关键词真掉；但**卡面自带**的那条留着（原版 `case 3` 两道闸）");

        var u = new UnitState(Troop("W36Kw"), false);
        u.ApplyTraitEffect("fast", 1, source: "CardA", extrinsic: true);
        Tr(u.Has("fast"), "E0 前提：关键词挂上了");
        u.RevertTraitEffectsFrom("CardA", extrinsicOnly: true);
        Tr(!u.Has("fast"),
           "E1 🔴 关键词真掉（改成「只从列表删、不调 RemoveTraitEffect」⇒ 关键词留着 ⇒ 红）");

        var u2 = new UnitState(Troop("W36Def", "fast"), false);   // 卡面自带 fast
        Tr(u2.Has("fast"), "E2-前 前提：卡面自带 `fast`");
        Tr(u2.ApplyTraitEffect("fast", 1, source: "CardA", extrinsic: true) != null, "E2-前′ 前提：记录建出来了");
        int n2 = u2.RevertTraitEffectsFrom("CardA", extrinsicOnly: true);
        C(n2, 1, "E2 记录被摘");
        Tr(u2.Has("fast"),
           "E2′ 卡面自带的 `fast` **留着**（把 `RemoveTraitEffect` 的 `Card.Has` 闸删掉 ⇒ 被抹 ⇒ 红）");
    }

    // ============================================================ F 端到端
    static void SectionF_RemoveExtrinsicEffectsFrom端到端()
    {
        MenuCheck.Section(_sink, "F `RemoveExtrinsicEffectsFrom(名)` 端到端覆盖第四本账");

        var u = new UnitState(Troop("W36End"), false);
        u.ApplyTraitEffect("fast", 1, source: "CardA", extrinsic: true);

        int n = u.RemoveExtrinsicEffectsFrom("CardA");
        Tr(n >= 1,
           "F1 🔴 端到端至少撤掉 1 条（删掉 `RemoveExtrinsicEffectsFrom` 那一行追加的 `n += RevertTraitEffectsFrom(...)` ⇒ n=0 ⇒ 红）");
        Tr(!u.Has("fast"), "F1′ 关键词掉了（证明这条路真的走到第四本账）");
    }
}
