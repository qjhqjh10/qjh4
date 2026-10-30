// B102EFF_Assert.cs — 第一百零二会话 · 执行代理 `B102_EFF`（账 `特效动画:521` 的 5 张残差）
//
// ============================ 它盯的是什么 ============================
//  `B101_F2` 收口后 `abilityEffectTexts` 的 31 张里还剩 **5 张 / 8 行**没有原文行上屏
//  （判据与逐卡表 → `资料/完全复刻/判据/B102_R3_特效B类两条.md` §521·③）。本件把那一族**行生产者**
//  补进投影（`RuleEngine/Core/EffectsToShow.cs` 文件尾的 `AddExtraRecordRows`），并在这里逐张钉住。
//
//  两族（原版判据逐条写在实现处，⛔ 别在这儿抄第二份）：
//   ① `buffType == 2 changeCost` —— `EntityScript__GetCardEffectsToShow.c:139-145` **只跳过 `costChange == 0`**，
//      `!= 0` 的记录照样进 `activeEffects` 表。池内受影响：`GSC55 Eve of the Uprising` ·
//      `GSC45 Underground Network` · `SAU55 Immortal Pride`。
//   ② `dontReturnFerocity`（`DefinedTrait` 1270）—— `cards_engine.json` 的 `abilityBuffs` 里
//      `SW40 Bjorn's Shrine` / `SW36 Bjorn the Fell-Handed` 都是 **`buffType 3 addTrait, trait 1270`**；
//      我们把它建模成 `UnitState.FerocityStay` 布尔 + 本件新加的 `FerocityStayBy`（= 原版 `enchantingCard`）。
//
// ==================== 宿主接线（一行；⛔ 本代理不改宿主 `Editor/BattleScene.cs`）====================
//  在 `BattleScene.Run()` 里那条超长聚合行（本会话现读 = `:46701`，其末尾是
//  `Debug.Log(P + $"=== 合计：…")`）—— **同一物理行**追加（行数零变化）：
//
//      B102EFF_Assert.Run(); pass += B102EFF_Assert.Pass; fail += B102EFF_Assert.Fail;
//
//  🔴 **追加必须落在该行第一个 `//` 之前**（本会话已踩过 6 次「代码被行末 `//` 吞掉」）。
//  ⚠️ 本件**不建任何 GameObject / 不用 `BattleDriver` / 不用 `BattleContext`**（全走静态纯函数 + 纯状态位），
//     任何宿主都能跑。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · 删掉 `EffectsToShow.AddExtraRecordRows(...)` 的调用（`Of()` 里 `SortShown` 之前那一句）
//    ⇒ **A1/A2 与 A3..A5 全部红**（行根本不存在），且 B1/B2/B3 的「没有」仍然绿 —— 两者结构上不可能同时满足。
//  · ① 只删「`StatMod{Attr=="cost"}`」那一段 ⇒ **A3/A4/A5 红**，A1/A2 仍绿。
//  · ② 把 `FerocityStayBy` 的读改成 `Source`/常量（不再读那条归属）⇒ **A1/A2 红**
//    （`AttachEffectTexts` 拿不到卡名 ⇒ 不印原文），而 A3..A5 仍绿。
//  · 把 `dontReturnFerocity` 那一支的 `FerocityStay` 闸去掉（无条件出一行）⇒ **B3 红**。
//  · 把成本行塞进 `ShownFamily.Stat`（不走新族）⇒ **B4 红**（`IsNonStatAttr` 会把它当属性丢掉 ⇒ 行没了）。
//
// ==================== 期望值从哪来（⛔ 不许拿我们自己的常量证明我们自己的实现）====================
//  · 期望右列 = **卡数据里那条 `abilityEffectTexts` 的 `src`**（`CardDef.AbilityEffectTexts` 现读）→
//    键 `Card_Abilities/<src>` → `Loc.T`（缺键印原文 —— 与 `CardDisplayWindow.TryAddEffectTextRow`
//    同一条判据，`GameStaticData__GetEffectDesc.c:8-25`）。
//  · `costChange` 的值 = **卡数据 `abilityBuffs` 那一行**（`CardDatabase.AbilityBuffsOf`，按 ET 行的
//    `(Ab, Lg, IsHand)` 对齐后取 `CostChange`）—— ⛔ 本件不写死 -3/+1/-1。
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B102_EFF`：账 `特效动画:521` 的 5 张残差（`changeCost` 3 张 · `dontReturnFerocity` 2 张）。</summary>
public static class B102EFF_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B102EFF] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具
    static List<CardDef> _pool;
    static CardDef Def(string name)
    {
        if (_pool == null) _pool = CardDatabase.Load();
        return CardDatabase.Find(_pool, name);
    }

    /// <summary>一张干净单位（`Deathwing Terminator`：**没有** `abilityEffectTexts`、也没有我们要断的那两族效果）。</summary>
    static UnitState CleanUnit()
    {
        var d = Def("Deathwing Terminator");
        return d == null ? null : new UnitState(d, false);
    }

    static List<ShownEffect> Proj(UnitState u) { return EffectsToShow.Of(u, true); }
    static List<CardDisplayWindow.EffectRow> Rows(UnitState u)
    {
        return CardDisplayWindow.RowsOfEffects(Proj(u), true);
    }

    /// <summary>数据面那条 `abilityEffectTexts` 的 `src`（`null` = 整列未知 / 没有）。</summary>
    static string SrcOf(CardDef d)
    {
        var rows = d == null ? null : d.AbilityEffectTexts;
        return rows != null && rows.Length > 0 && rows[0] != null ? rows[0].Src : null;
    }

    /// <summary>数据面：与 `abilityEffectTexts` 那一行 `(Ab, Lg, IsHand)` 对齐、且 `BuffType == buffType`
    /// 的 `abilityBuffs` 行的某个值。`found == false` ⇒ 没有这一行（判不了）。
    /// <para>`pick` 选哪一格：`0` = `CostChange` · `1` = `Trait`（都来自原版卡表，⛔ 不是我们的常量）。</para></summary>
    static int BuffValueOf(CardDef d, int buffType, int pick, out bool found)
    {
        found = false;
        var buffs = CardDatabase.AbilityBuffsOf(d);
        var ets = d == null ? null : d.AbilityEffectTexts;
        if (buffs == null || ets == null || ets.Length == 0 || ets[0] == null) return 0;
        for (int i = 0; i < buffs.Count; i++)
        {
            var b = buffs[i];
            if (b == null || b.BuffType != buffType) continue;
            if (b.Ab != ets[0].Ab || b.Lg != ets[0].Lg || b.IsHandBuff != ets[0].IsHand) continue;
            found = true;
            return pick == 1 ? b.Trait : b.CostChange;
        }
        return 0;
    }

    /// <summary>期望右列 = 原版 `GetEffectDesc` 第一跳的产物（键 → 词条；缺键 ⇒ **原文**；再过 `ModifyLocalization` 的等价物）。</summary>
    static string ExpectedOf(string srcId)
    {
        if (string.IsNullOrEmpty(srcId)) return null;
        string key = "Card_Abilities/" + srcId;
        string what = Loc.T(key);
        if (string.IsNullOrEmpty(what) || what == key) what = srcId;
        return CardIcons.RewriteMarks(what, Loc.Current == AvailableLanguages.Chinese);
    }

    static int CountWhat(List<CardDisplayWindow.EffectRow> rows, string what)
    {
        int n = 0;
        if (rows == null || string.IsNullOrEmpty(what)) return 0;
        for (int i = 0; i < rows.Count; i++)
            if (string.Equals(rows[i].What, what, StringComparison.Ordinal)) n++;
        return n;
    }

    static bool HasWho(List<CardDisplayWindow.EffectRow> rows, string who)
    {
        if (rows == null || string.IsNullOrEmpty(who)) return false;
        for (int i = 0; i < rows.Count; i++)
            if (string.Equals(rows[i].Who, who, StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>5 张残差（卡名）。`src` / `costChange` 全部按数据面现读（见 <see cref="SrcOf"/> / <see cref="BuffValueOf"/>）。</summary>
    static readonly string[] FerocityCards = { "Bjorn's Shrine", "Bjorn the Fell-Handed" };
    static readonly string[] CostCards = { "Eve of the Uprising", "Underground Network", "Immortal Pride" };

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B102_EFF 账 `特效动画:521` 的 5 张残差（`changeCost` ×3 · `dontReturnFerocity` ×2）");
        SectionA_RealCards();
        SectionB_Negatives();
        Debug.Log(_sink.P + "B102EFF 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 段：真卡（原文真的上屏）
    static void SectionA_RealCards()
    {
        MenuCheck.Section(_sink, "A 真卡：5 张的原文行上屏（逐张，⛔ 不是「总数 +5」）");

        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception ex) { Debug.LogWarning("[B102EFF] `CardDatabase.Load()` 抛了：" + ex.Message); }
        Tr(pool != null && pool.Count > 0, "A0 **前提**：卡池读得到（读不到本节全部无意义）");
        if (pool == null || pool.Count == 0) return;

        // ---- ① 前提：5 张都取得到，且数据面真的挂着那两列（否则期望值无从现读）----
        for (int i = 0; i < FerocityCards.Length + CostCards.Length; i++)
        {
            string cardName = i < FerocityCards.Length ? FerocityCards[i] : CostCards[i - FerocityCards.Length];
            var def = Def(cardName);
            Tr(def != null, "A0-" + (i + 1) + " **前提**：`" + cardName + "` 在卡池里");
            if (def == null) continue;
            Tr(def.HasAbilityEffectTexts && CardDatabase.AbilityBuffsOf(def) != null,
               "A0-" + (i + 1) + "b **前提**：`" + cardName + "` 挂着 `abilityEffectTexts` + `abilityBuffs` 两列");
        }

        // ---- ② 两张 `dontReturnFerocity`：`FerocityStay` + 归属卡名 ⇒ 原文行 ----
        for (int i = 0; i < FerocityCards.Length; i++)
        {
            string cardName = FerocityCards[i];
            var def = Def(cardName);
            var u = CleanUnit();
            Tr(u != null, "A" + (i + 1) + "-0 **前提**：干净单位造得出来（否则下面两条没有载体）");
            if (u == null || def == null) continue;

            bool found;
            int trait = BuffValueOf(def, 3 /* addTrait */, 1, out found);   // trait 值（原版 1270）从数据面取
            Tr(found && trait == 1270, "A" + (i + 1) + "-1 **前提**：数据面这条 ET 行挂在 `buffType 3, trait 1270` 上（现读 " + trait + "）");

            // 生产同一条口：`EffectResolver.DoFerocityStay` 在标记上写的就是这两格
            u.FerocityStay = true; u.FerocityStayBy = cardName;

            var proj = Proj(u);
            Tr(proj.Count > 0, "A" + (i + 1) + "-2 `" + cardName + "`：投影非空（`EffectsToShow.Of` 出了行）");
            var rows = CardDisplayWindow.RowsOfEffects(proj, true);
            string want = ExpectedOf(SrcOf(def));
            Tr(!string.IsNullOrEmpty(want), "A" + (i + 1) + "-3 **前提**：`" + SrcOf(def) + "` 的期望原文取得到");
            C(CountWhat(rows, want), 1, "A" + (i + 1) + " ★ `" + cardName + "` 的原文行**恰好一行**上屏");
            Tr(HasWho(rows, cardName),
               "A" + (i + 1) + "-4 左列 = 给这条效果的那张卡（`" + cardName + "`）");
        }

        // ---- ③ 三张 `changeCost`：`cost` 记录 ⇒ 原文行 ----
        //  记录形状 = `UnitState.AddStatMod("cost", 值, source: 卡名, buffType: 2)`（生产同一条口；
        //  原版 `CardEffect{buffType 2, costChange, effectText}` 挂在那一张牌的实体上）。
        for (int i = 0; i < CostCards.Length; i++)
        {
            string cardName = CostCards[i];
            var def = Def(cardName);
            var u = CleanUnit();
            Tr(u != null, "A" + (i + 3) + "-0 **前提**：干净单位造得出来");
            if (u == null || def == null) continue;

            bool found;
            int costChange = BuffValueOf(def, 2 /* changeCost */, 0, out found);
            Tr(found && costChange != 0,
               "A" + (i + 3) + "-1 **前提**：数据面这条 ET 行挂在 `buffType 2 changeCost != 0` 上（现读 " + costChange + "）");

            var mod = u.AddStatMod("cost", costChange, source: cardName, buffType: 2);
            Tr(mod != null, "A" + (i + 3) + "-2 **前提**：`cost` 记录挂上了");

            var proj = Proj(u);
            Tr(proj.Count > 0, "A" + (i + 3) + "-3 `" + cardName + "`：投影非空");
            var rows = CardDisplayWindow.RowsOfEffects(proj, true);
            string want = ExpectedOf(SrcOf(def));
            Tr(!string.IsNullOrEmpty(want), "A" + (i + 3) + "-4 **前提**：`" + SrcOf(def) + "` 的期望原文取得到");
            C(CountWhat(rows, want), 1, "A" + (i + 3) + " ★ `" + cardName + "` 的原文行**恰好一行**上屏");
            Tr(HasWho(rows, cardName), "A" + (i + 3) + "-5 左列 = 给这条效果的那张卡（`" + cardName + "`）");
        }
    }

    // ============================================================ B 段：灭自证 / 闸
    static void SectionB_Negatives()
    {
        MenuCheck.Section(_sink, "B 灭自证：归属卡 / 记录 / 闸 各钉一条");

        string bjornSrc = null;
        {
            var d = Def("Bjorn's Shrine");
            bjornSrc = SrcOf(d);
        }

        // B1：`FerocityStayBy` 空 ⇒ 取不到卡名 ⇒ 不印原文（而 `FerocityStay` 仍为真）
        {
            var u = CleanUnit();
            if (u != null)
            {
                u.FerocityStay = true; u.FerocityStayBy = null;
                C(CountWhat(Rows(u), ExpectedOf(bjornSrc)), 0,
                  "B1 没有归属卡名 ⇒ `Bjorn's Shrine` 的原文**不**上屏（🧨 把 `FerocityStayBy` 的读改成常量/`Source` ⇒ 这条红）");
            }
            else Tr(false, "B1 **前提**：干净单位造得出来");
        }

        // B2：没有 `cost` 记录 ⇒ 三张的原文一行都不该出现
        {
            var u = CleanUnit();
            if (u != null)
            {
                var rows = Rows(u);
                int n = 0;
                for (int i = 0; i < CostCards.Length; i++) n += CountWhat(rows, ExpectedOf(SrcOf(Def(CostCards[i]))));
                C(n, 0, "B2 没有 `cost` 记录 ⇒ 3 张 `changeCost` 的原文**一行都没有**（🧨 让 `AddExtraRecordRows` 的第一段无视 `cost` 记录、无条件出一行 ⇒ 这条红）");
            }
            else Tr(false, "B2 **前提**：干净单位造得出来");
        }

        // B3：`FerocityStay == false`（标记已被消费）⇒ 那一行必须消失（生命周期与「用掉即清」对齐）
        {
            var u = CleanUnit();
            if (u != null)
            {
                u.FerocityStay = false; u.FerocityStayBy = "Bjorn's Shrine";   // 归属还在、标记已消费
                C(CountWhat(Rows(u), ExpectedOf(bjornSrc)), 0,
                  "B3 `FerocityStay` 已消费 ⇒ 那一行**消失**（🧨 去掉 `if (u.FerocityStay)` 那道闸 ⇒ 这条红）");
            }
            else Tr(false, "B3 **前提**：干净单位造得出来");
        }

        // B4：成本行必须落在**新族**上（`IsNonStatAttr` 把 `cost` 挡在属性族外 —— 别让它在 Stat 里被丢掉）
        {
            var u = CleanUnit();
            if (u != null)
            {
                var d = Def("Eve of the Uprising");
                bool found; int costChange = BuffValueOf(d, 2, 0, out found);
                u.AddStatMod("cost", found ? costChange : -3, source: "Eve of the Uprising", buffType: 2);
                var proj = Proj(u);
                int costRows = 0, statCostRows = 0;
                for (int i = 0; i < proj.Count; i++)
                {
                    if (proj[i].Family == ShownFamily.Cost && proj[i].BuffType == EffectsToShow.BuffTypeChangeCost) costRows++;
                    if (proj[i].Family == ShownFamily.Stat && RuleCore.NormalizeAttr(proj[i].Attr ?? "") == "cost") statCostRows++;
                }
                C(costRows, 1, "B4 ★ `cost` 记录投影成 **1 条 `ShownFamily.Cost`**（🧨 并进 `ShownFamily.Stat` ⇒ 这条红）");
                C(statCostRows, 0, "B4b 它**没有**同时落进属性族（`IsNonStatAttr` 的闸照旧，⛔ 别改那条）");
            }
            else Tr(false, "B4 **前提**：干净单位造得出来");
        }
    }
}
