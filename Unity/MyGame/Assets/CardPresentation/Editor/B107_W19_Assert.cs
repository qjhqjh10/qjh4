// B107_W19_Assert.cs — 执行代理 `B107_W28`（第一百零七会话 · 账 `特效动画:N20②` 消费端接线）的断言
//
// ============================ 它盯的是什么 ============================
// `B107_W19` 给 `TraitEffectRecord` / `StatMod` / `TempBuff.Origin` / `FerocityStay` 各补了「归属四元组」
// （`EffectOwner/EffectAb/EffectLg/EffectIsHand`），生产点 = `EffectResolver.ApplyOneGain` 四支 +
// `DoFerocityStay`（`SW40` / `SW36` 那 2 张卡）。本件验证三件：
//   ① 反解器 `UnitState.DeriveEffectOrigin`：`SW40` ⇒ `(Owner="Bjorn's Shrine", Ab=0, Lg=1)`，
//      `SW36` ⇒ `(Owner="Bjorn the Fell-Handed", Ab=0, Lg=0)`，且两卡 `EffectTextOf` 分别取到
//      `EffectBjornShrine` / `EffectFellhanded`（判据 = 卡表 `abilityBuffs` + `abilityEffectTexts` 现读）。
//   ② 记录口：`ApplyTraitEffect` / `AddStatMod` 带 `origin:` 时写进四格；**缺省 ⇒ 判不到**（`Owner == null`、`Ab/Lg == -1`）。
//   ③ `RefreshForNewTurn` 的兜底复位：`FerocityStay*` 四格一起清（⛔ 只清 `FerocityStay` 会留下上一张卡的 `(ab,lg)`）。
//
// ============================ 怎么接 ============================
// `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里，把现读 `:1372` 那句改成**同一物理行**追加（零行数变化）：
//       B106W57(); B107W10(); B107W19();
// 🔴 入口**故意不叫** `Run` / 不带 `Test` 前缀（同 `B107_W10_Assert`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A3/A4（SW40 的 (0,1)）与 B3/B4（SW36 的 (0,0)）**互不相同**：把 `DeriveEffectOrigin` 改成
//    「取第一条 `buffType 3`」⇒ 两张里必有一张的 `(ab,lg)` 对不上 ⇒ 红；把 `traitDefinedId` 过滤删掉 ⇒
//    若将来池内出现同 `buffType` 不同 `trait` 的行，也会红（今天池内不触发，如实标着）。
//  · A5/B5：把 `EffectTextOf` 的 `ab/lg` 传反 / 把 `isHand` 传成 `true` ⇒ 取到 `""` ⇒ 红。
//  · C7/C8：把 `StatMod.EffectAb` 的缺省从 `-1` 改成 `0` ⇒ 红（`0` 是合法下标，两者必须分得开）。
//  · D1：删掉 `RefreshForNewTurn` 里新加的 `FerocityStayAb/Lg = -1` ⇒ 红。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由宿主 `Run()` 里一句 `B107W19();` 调；⛔ 不叫 `Run` / 不带 `Test` 前缀）。</summary>
    public static void B107W19()
    {
        Section("🆕 B107_W19 · N20②：`effectText` 归属四元组（反解器 / 记录口 / FerocityStay 复位）");

        B107W19Derive();
        B107W19Records();
        B107W19FerocityReset();
        B107W19Projection();
    }

    // ==================================================================
    //  ① 反解器 —— SW40（战术卡，PlayingCard）/ SW36（常驻能力，CreationSource）
    // ==================================================================
    static void B107W19Derive()
    {
        // ---- 前提：卡池 + 两张卡 + 数据列都在 ----
        var pool = CardDatabase.Load();
        CheckTrue(pool != null && pool.Count > 0,
                  "A0 （前提）卡池载到（`CardDatabase.Load()` 非空）—— 拿不到 ⇒ 本段后面的断言全部无对象");
        if (pool == null || pool.Count == 0) return;

        var sw40 = CardDatabase.FindById(pool, "SW40");
        var sw36 = CardDatabase.FindById(pool, "SW36");
        CheckTrue(sw40 != null, "A0 （前提）卡池里有 `SW40`");
        CheckTrue(sw36 != null, "A0 （前提）卡池里有 `SW36`");
        if (sw40 == null || sw36 == null) return;

        Check(sw40.Name, "Bjorn's Shrine", "A0 （前提）`SW40` 就是 `Bjorn's Shrine`");
        Check(sw36.Name, "Bjorn the Fell-Handed", "A0 （前提）`SW36` 就是 `Bjorn the Fell-Handed`");
        CheckTrue(sw40.HasAbilityEffectTexts,
                  "A0 （前提）`SW40` 的 `abilityEffectTexts` 列在（`null` = 旧表 ⇒ 本段无法判原文）");
        CheckTrue(sw36.HasAbilityEffectTexts,
                  "A0 （前提）`SW36` 的 `abilityEffectTexts` 列在");
        if (!sw40.HasAbilityEffectTexts || !sw36.HasAbilityEffectTexts) return;

        int tid = CardDef.DefinedTraitNumber("dontReturnFerocity");
        Check(tid, 1270,
              "A1 （前提）`dontReturnFerocity` = `DefinedTrait` **1270**（`DefinedTraitNumber` 大小写敏感；"
            + "判据 `CardDefPartial_B94B16.cs` 的 `DefinedTrait` 表）");

        // ---- A. SW40：战术卡 ⇒ `ctx.PlayingCard` ----
        var ctx = new BattleContext(0xB10719);
        ctx.PlayingCard = sw40;
        var o40 = UnitState.DeriveEffectOrigin(ctx, "dontReturnFerocity", UnitState.BuffTypeAddTrait, false, tid);
        Check(o40.Owner, "Bjorn's Shrine",
              "A2 ★★ 归属卡 = `Bjorn's Shrine`（实得 " + (o40.Owner ?? "null") + "）"
            + "｜🧨 取错卡（例如退到 `by` = 「战术卡」）⇒ 红");
        Check(o40.Ab, 0, "A3 ★ `(ab,lg)` = **(0,1)** 的 ab（实得 " + o40.Ab + "）");
        Check(o40.Lg, 1, "A4 ★ `(ab,lg)` = **(0,1)** 的 lg（实得 " + o40.Lg + "）");
        Check(sw40.EffectTextOf(o40.Ab, o40.Lg, false), "EffectBjornShrine",
              "A5 ★★ 用解出来的 `(ab,lg)` 取得到原文 = `EffectBjornShrine`（实得「" + (sw40.EffectTextOf(o40.Ab, o40.Lg, false) ?? "null") + "」）"
            + "｜🧨 `ab/lg` 传反 / `isHand` 传成 `true` ⇒ 取到 `\"\"` ⇒ 红");

        // ---- B. SW36：常驻能力 ⇒ `ctx.CreationSource` ----
        var ctx2 = new BattleContext(0xB10719);
        ctx2.CreationSource = sw36;
        var o36 = UnitState.DeriveEffectOrigin(ctx2, "dontReturnFerocity", UnitState.BuffTypeAddTrait, false, tid);
        Check(o36.Owner, "Bjorn the Fell-Handed",
              "B2 ★★ 归属卡 = `Bjorn the Fell-Handed`（实得 " + (o36.Owner ?? "null") + "）");
        Check(o36.Ab, 0, "B3 ★ `(ab,lg)` = **(0,0)** 的 ab（实得 " + o36.Ab + "）");
        Check(o36.Lg, 0, "B4 ★ `(ab,lg)` = **(0,0)** 的 lg（实得 " + o36.Lg + "）");
        Check(sw36.EffectTextOf(o36.Ab, o36.Lg, false), "EffectFellhanded",
              "B5 ★★ 取得到原文 = `EffectFellhanded`（实得「" + (sw36.EffectTextOf(o36.Ab, o36.Lg, false) ?? "null") + "」）");
        CheckTrue(o40.Ab != o36.Ab || o40.Lg != o36.Lg,
                  "B6 ★★ 灭自证：两张卡的 `(ab,lg)` **互不相同**（(0,1) vs (0,0)）"
                + "｜🧨 「取第一条 `buffType 3`」那种实现必有一张对不上 ⇒ A3/A4 或 B3/B4 红");
    }

    // ==================================================================
    //  ② 记录口 —— 写进去、读得回；缺省 ⇒ 判不到
    // ==================================================================
    static void B107W19Records()
    {
        var pool = CardDatabase.Load();
        if (pool == null || pool.Count == 0) return;   // A0 已经报过前提，这里不重复
        var sw40 = CardDatabase.FindById(pool, "SW40");
        CheckTrue(sw40 != null, "C0 （前提）`SW40` 在池内（记录口夹具要用真卡建 `UnitState`）");
        if (sw40 == null) return;

        var ctx = new BattleContext(0xB10719);
        ctx.PlayingCard = sw40;
        var o40 = UnitState.DeriveEffectOrigin(ctx, "dontReturnFerocity", UnitState.BuffTypeAddTrait, false,
                                              CardDef.DefinedTraitNumber("dontReturnFerocity"));
        CheckTrue(o40.Owner != null, "C0 （前提）反解器给出了归属（否则下面「带 origin」那两条无从谈起）");
        if (o40.Owner == null) return;

        var u = new UnitState(sw40, false);                        // 用真卡建，⛔ 别手搓 `CardDef` 构造器（形参表一改就编不过）
        var rec = u.ApplyTraitEffect("flank", 1, origin: o40);
        CheckTrue(rec != null, "C1 （前提）`ApplyTraitEffect(…, origin:)` 建出了记录");
        if (rec != null)
        {
            Check(rec.EffectOwner, "Bjorn's Shrine", "C2 ★ `TraitEffectRecord.EffectOwner` = `Bjorn's Shrine`");
            Check(rec.EffectAb, 0, "C3 ★ `TraitEffectRecord.EffectAb` = 0");
            Check(rec.EffectLg, 1, "C4 ★ `TraitEffectRecord.EffectLg` = 1");
        }

        var m = u.AddStatMod("attack", 1, origin: o40);
        CheckTrue(m != null, "C5 （前提）`AddStatMod(…, origin:)` 建出了记录");
        Check(m.EffectOwner, "Bjorn's Shrine", "C6 ★ `StatMod.EffectOwner` = `Bjorn's Shrine`");
        Check(m.EffectAb, 0, "C7 ★ `StatMod.EffectAb` = 0");
        Check(m.EffectLg, 1, "C8 ★ `StatMod.EffectLg` = 1");

        var none = u.AddStatMod("attack", 1);                       // 缺省 `origin` ⇒ 判不到
        CheckTrue(none != null, "C9 （前提）缺省 origin 也建得出记录");
        CheckTrue(none.EffectOwner == null,
                  "C10 ★★ 缺省 ⇒ **判不到**：`EffectOwner == null`（⛔ 别拿空串/占位冒充）");
        Check(none.EffectAb, -1,
              "C11 ★★★ 缺省 ⇒ `EffectAb == -1`（**⛔ 不是 0** —— 0 是合法下标，"
            + "两者混了就分不清「判不到」与「第 0 条 ability」）"
            + "｜🧨 把缺省从 `-1` 改成 `0` ⇒ 红");
        Check(none.EffectLg, -1, "C12 ★★★ 缺省 ⇒ `EffectLg == -1`（同上）");
    }

    // ==================================================================
    //  ③ FerocityStay 的兜底复位 —— 四格同生共死
    // ==================================================================
    static void B107W19FerocityReset()
    {
        var pool = CardDatabase.Load();
        if (pool == null || pool.Count == 0) return;   // A0 已经报过前提，这里不重复
        var sw36 = CardDatabase.FindById(pool, "SW36");
        CheckTrue(sw36 != null, "D0 （前提）`SW36` 在池内");
        if (sw36 == null) return;

        var u2 = new UnitState(sw36, false);
        u2.FerocityStay = true; u2.FerocityStayBy = "Bjorn's Shrine"; u2.FerocityStayAb = 0; u2.FerocityStayLg = 1;

        // ⚠️ 复位那一支在 **`RefreshForNewTurn()`** 里（`UnitState.cs` 的回合刷新），**不是** `RevertBuffs`
        //    （`RevertBuffs` 只扫 `_buffs`、不碰 `FerocityStay`）——`B107_W19` 报告的片段在这一点上写错了方法名，本件按现读代码修。
        u2.RefreshForNewTurn();

        CheckTrue(!u2.FerocityStay, "D1 ★ `RefreshForNewTurn` 兜底复位：`FerocityStay == false`");
        CheckTrue(u2.FerocityStayBy == null,
                  "D2 ★★ 归属卡名一起清（否则 `EffectsToShow` 会继续投影一行早已失效的记录）");
        Check(u2.FerocityStayAb, -1,
              "D3 ★★★ `FerocityStayAb` 一起清回 `-1`（⛔ 只清布尔、留下旧 `(ab,lg)` ⇒ 下一次狂暴会拿到上一张卡的原文）"
            + "｜🧨 删掉 `RefreshForNewTurn` 里新加的那句 ⇒ 红");
        Check(u2.FerocityStayLg, -1, "D4 ★★★ `FerocityStayLg` 一起清回 `-1`（同上）");
    }

    // ==================================================================
    //  ④ 端到端投影 —— **接线前**的旧通路（`FerocityStayBy`）是否已经把原文喂到展示行
    //     （= 判 `N20②` 前提：`B102_EFF` 的 `OwnerCard = FerocityStayBy` + `AttachEffectTexts` 是否已覆盖这 2 张卡）
    // ==================================================================
    static void B107W19Projection()
    {
        var pool = CardDatabase.Load();
        if (pool == null || pool.Count == 0) return;   // A0 已经报过前提，这里不重复
        var sw40 = CardDatabase.FindById(pool, "SW40");
        var sw36 = CardDatabase.FindById(pool, "SW36");
        CheckTrue(sw40 != null && sw36 != null, "E0 （前提）`SW40` / `SW36` 都在池内");
        if (sw40 == null || sw36 == null) return;
        CheckTrue(sw40.HasAbilityEffectTexts && sw36.HasAbilityEffectTexts,
                  "E0 （前提）两张卡的 `abilityEffectTexts` 列都在");
        if (!sw40.HasAbilityEffectTexts || !sw36.HasAbilityEffectTexts) return;

        var ctx = new BattleContext(0xB10719);
        B107W19ProjectedOne(new UnitState(sw40, false), "Bjorn's Shrine", sw40, "EffectBjornShrine", "E", ctx);
        B107W19ProjectedOne(new UnitState(sw36, false), "Bjorn the Fell-Handed", sw36, "EffectFellhanded", "F", ctx);
    }

    static void B107W19ProjectedOne(UnitState u, string by, CardDef expectCard, string expectText, string tag, BattleContext ctx)
    {
        u.FerocityStay = true; u.FerocityStayBy = by;
        var rows = EffectsToShow.Of(u, false, ctx);
        ShownEffect? hit = null;
        for (int i = 0; i < rows.Count; i++)
            if (rows[i].Family == ShownFamily.Trait && rows[i].Trait == EffectsToShow.FerocityStayTrait) { hit = rows[i]; break; }
        CheckTrue(hit.HasValue,
                  tag + "1 （前提）投影里出现了 `dontReturnFerocity` 那一行（`EffectsToShow.AddExtraRecordRows` ②）");
        if (!hit.HasValue) return;

        var r = hit.Value;
        Check(r.OwnerCard, by, tag + "2 ★ `OwnerCard` = 归属卡（= `FerocityStayBy`，**接线前的旧通路**）");
        CheckTrue(r.EffectTextCard != null,
                  tag + "3 ★★★ `AttachEffectTexts` 用「卡名 + `buffType 3`」**取到了卡**（`EffectTextCard != null`）"
                + " ⇒ `N20②` 对这张卡的「取不到原文」**在接线前就已经被 `B102_EFF` 的旧通路覆盖**");
        if (r.EffectTextCard == null) return;

        Check(r.EffectTextCard.Name, expectCard.Name, tag + "4 ★ 取到的是正确的那张卡（不是同名另一张）");
        string got = r.EffectTextCard.EffectTextOf(r.EffectAb, r.EffectLg, r.EffectIsHand);
        Check(got, expectText,
              tag + "5 ★★ 原文 = `" + expectText + "`（实得「" + (got ?? "null") + "」）"
            + "｜🧨 `AttachEffectTexts` 不读 `OwnerCard` / 按 `buffType` 匹配错行 ⇒ 取到 `\"\"` ⇒ 红");
    }
}
