// B101F2_Assert.cs — 第一百零一会话 · 波 2 · 执行代理 `B101_F2`（`effectText` 链喂到展示层）
//
// ============================ 它盯的是什么 ============================
//  `B101_Q2` 定性里**唯一一条「B · 真没做」**：卡牌详情/效果行里，那 31 张带 `abilityEffectTexts`
//  的卡要用**原版 `effectText` 原文**（`GameStaticData.GetEffectDesc` 的第一跳），而不是我们现拼的文本。
//
//  本件落地 = 两处：
//    ① `RuleEngine/Core/EffectsToShow.cs` 文件尾的 `AttachEffectTexts`（`Of()` 末尾同一物理行调用）
//       —— 给每条 `ShownEffect` 补「来源卡 + `(ab, lg, isHand)`」四元组；
//    ② `CardPresentation/Battle/CardDisplayWindow.cs` 类尾的 `TryAddEffectTextRow`
//       （`RowsOfEffects` 的循环里同一物理行调用）—— 键 `Card_Abilities/<原文>` → 缺键印原文。
//  判据 → `报告/B101_Q2_特效卡牌定性.md` §四① · `decomp_full/GameStaticData__GetEffectDesc.c:8-25`
//       · `CardDef.cs:9436 EffectTextOf`（三态）/ `:9447 EffectTextTermOf`（唯一拼键口）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/BattleScene.cs` 那条超长聚合行（本会话现读 = `:46701`，其末尾是
//  `Debug.Log(P + $"=== 合计：…")`）—— **同一物理行**追加（行数零变化）：
//      B101F2_Assert.Run(); pass += B101F2_Assert.Pass; fail += B101F2_Assert.Fail;
//  推荐插在 `B101R5_Assert.Run(); …` 之后、`Debug.Log(P + $"=== 合计：…` 之前。
//  ⚠️ 本件**不建任何 GameObject / 不用 `CardDisplayWindow` 实例**（全走静态纯函数），任何宿主都能跑。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · A 段（原文真的上屏）：把 `TryAddEffectTextRow` 的第一句改成 `return false;`（不短路）⇒ **A 全红**；
//    把 `CardDef.EffectTextOf` 短路成 `return null;` ⇒ `AttachEffectTexts` 拿不到原文 ⇒ **A 全红**
//    （而 B1/B2 仍绿 —— 它们断的正是「没有原文时保持原样」，两条结构上不可能同时满足）。
//  · A-literal（字面量）：把键拼错成 `"Card_Abilitie/"` ⇒ 缺键 ⇒ 回原文 id ⇒ 该行**不含**那句英/中文 ⇒ 红。
//  · B1（无 `effectText` 的卡保持原样）：把 `AttachEffectTexts` 改成「只要来源卡有 ET 行就套上」
//    （不看 `buffType`）⇒ **B2 红**（`Helspear Assault` 的 `buffType 12` 对不上任何行，却会被套上原文）。
//  · B3（属性行原路不变）：把 `TryAddEffectTextRow` 的守卫从 `EffectTextCard == null` 放宽成
//    「任何行都先试一次」⇒ 属性行会绕开 `AddStatRows` ⇒ **B3 红**。
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B101_F2`：`CardEffect.effectText(+0x28)` → 「受到以下影响」右列（原版 `GetEffectDesc` 第一跳）。</summary>
public static class B101F2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B101F2] ") { Near = MenuNearStyle.Compact3 };

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

    /// <summary>一张干净单位（`Deathwing Terminator`：**没有** `abilityEffectTexts`，且任何断言都不靠它自带效果）。</summary>
    static UnitState CleanUnit()
    {
        var d = Def("Deathwing Terminator");
        if (d == null) return null;
        return new UnitState(d, false);
    }

    /// <summary>夹具：干净单位 + 一条「来源 = <paramref name="srcCard"/> · `buffType` = <paramref name="buffType"/>」的附魔。
    /// <para>走的是**生产同一条口** `Enchantments.Attach`（`AttachGrantedByPlayedCard` 的 `source:` 就是**打出那张卡的卡名**）。</para></summary>
    static List<CardDisplayWindow.EffectRow> RowsFor(string srcCard, int buffType)
    {
        var u = CleanUnit();
        if (u == null) return null;
        var carrier = u.Instance;
        if (carrier == null) return null;
        Enchantments.Attach(null, carrier, u.Card, buffType, source: srcCard);
        return CardDisplayWindow.RowsOfEffects(EffectsToShow.Of(u, true), true);
    }

    /// <summary>期望右列 = 原版那一跳的产物（键 → 词条；缺键 ⇒ **原文**；最后过 `ModifyLocalization` 的等价物）。</summary>
    static string ExpectedOf(string srcId)
    {
        string key = "Card_Abilities/" + srcId;
        string what = Loc.T(key);
        if (string.IsNullOrEmpty(what) || what == key) what = srcId;
        return CardIcons.RewriteMarks(what, Loc.Current == AvailableLanguages.Chinese);
    }

    /// <summary>第一个**非空**右列（空行 = 原版那个「空文本占一槽」的洞）。</summary>
    static string FirstWhat(List<CardDisplayWindow.EffectRow> rows)
    {
        if (rows == null) return null;
        for (int i = 0; i < rows.Count; i++)
            if (!string.IsNullOrEmpty(rows[i].What)) return rows[i].What;
        return null;
    }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B101_F2 `effectText` 链喂文本（原版 `GetEffectDesc` 第一跳）");
        SectionA_RealCards();
        SectionB_NegativesAndNoRegression();
        Debug.Log(_sink.P + "B101F2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 段：真卡（原文真的上屏）
    static void SectionA_RealCards()
    {
        MenuCheck.Section(_sink, "A 真卡：`abilityEffectTexts` 的原文上屏（8 张 · 全部走附魔那一族）");

        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception ex) { Debug.LogWarning("[B101F2] `CardDatabase.Load()` 抛了：" + ex.Message); }
        Tr(pool != null && pool.Count > 0, "A0 **前提**：卡池读得到（读不到本节全部无意义）");
        if (pool == null || pool.Count == 0) return;

        // (来源卡名, buffType, 原版 `effectText` 原文, 一句**字面量**（英/中任一命中即可）)
        var cases = new string[][]
        {
            new[] { "Graceful Avoidance", "15", "EffectGracefulAvoidance", "Return to your hand", "返回到你的手中" },
            new[] { "Cadia Stands",       "12", "EffectCadiaStands",     "deploy a Shock Trooper", "部署一名震荡兵" },
            new[] { "Idolatrous Despoilers", "12", "EffectIdolatrous",   "when an enemy troop is deployed", "当一个敌方军队被部署时" },
            new[] { "Coterie of the Conceited", "12", "EffectCoetrie",   "Combat Elixir", "战斗药剂" },
            new[] { "Armoured Support",   "12", "EffectArmouredSupport", "Vehicles you put in play", "给你上场的载具" },
            new[] { "Krump da Gitz",      "12", "EffectKrump",           "flank", "flank" },
            new[] { "Hive Fleet Leviathan", "12", "EffectHiveFleet",     "Your troops cost 1 less", "你的军队的费用减少" },
            new[] { "Saviour Protocols",  "12", "SaviorProtocolsEffect", "your Drones cost 1 less", "你的无人机的花费减少" },
        };

        for (int i = 0; i < cases.Length; i++)
        {
            string name = cases[i][0];
            int buffType = int.Parse(cases[i][1]);
            string srcId = cases[i][2];
            string litEn = cases[i][3], litZh = cases[i][4];

            var def = CardDatabase.Find(pool, name);
            Tr(def != null, $"A{i + 1} **前提**：卡池里找得到「{name}」");
            if (def == null) continue;
            Tr(def.AbilityEffectTexts != null && def.AbilityEffectTexts.Length > 0,
               $"A{i + 1} **前提**：「{name}」的 `abilityEffectTexts` **有行**（`src = {srcId}`）");
            C(def.EffectTextOf(0, 0, false), srcId,
              $"A{i + 1}-src 「{name}」的 `(0,0,buffEffect)` 原文 = `{srcId}`（**三态里的非空档**；🧨 `EffectTextOf` 短路成 null ⇒ 本条与下面全红）");

            var rows = RowsFor(name, buffType);
            Tr(rows != null, $"A{i + 1} **前提**：夹具跑得起来（卡池里两张卡都在）");
            if (rows == null) continue;

            string what = FirstWhat(rows);
            C(what, ExpectedOf(srcId),
              $"A{i + 1} 【原文上屏】`buffType {buffType}` 的附魔行右列 = `Card_Abilities/{srcId}` 的词条"
              + "｜🧨 不短路 / `EffectTextOf` 返回 null ⇒ 这一行退回合成键或空串 ⇒ 本行红");
            Tr(what != null && what != srcId,
              $"A{i + 1}-id 【不是 id】右列**不是**原文 id 本身（`{srcId}`）⇒ 词条真的取到了"
              + "｜🧨 键拼错（缺键 ⇒ 回原文）⇒ 本行红");
            Tr(what != null && (what.Contains(litEn) || what.Contains(litZh)),
              $"A{i + 1}-lit 【字面量】右列含原版那句话（「{litEn}」/「{litZh}」）"
              + "｜🧨 取错键 / 张冠李戴 ⇒ 本行红（期望值不是从被测实现反算的）");
        }
    }

    // ============================================================ B 段：反例（没有原文的，一个字都不许变）
    static void SectionB_NegativesAndNoRegression()
    {
        MenuCheck.Section(_sink, "B 反例：无 `effectText` 的卡保持原样 · `buffType` 对不上时**不猜** · 属性行原路");

        // ---- B1：来源卡**没有** `abilityEffectTexts` ⇒ 附魔行仍是那个洞 ----------------------------
        {
            var def = Def("Deathwing Terminator");
            Tr(def != null, "B1 **前提**：卡池里找得到 `Deathwing Terminator`");
            if (def != null)
            {
                // ⚠️ 卡表 v44 起**每张卡都有这一列**（多数是 `[]`）⇒ `HasAbilityEffectTexts`（列在不在）
                //    **不是**这里要的反例判据；反例是「**这一张一行都没有**」（= `EffectTextOf` 的三态里的 `""`）。
                Tr(def.AbilityEffectTexts == null || def.AbilityEffectTexts.Length == 0,
                   "B1-a **前提**：`Deathwing Terminator` 的 `abilityEffectTexts` **一行都没有**（反例纯；⚠️ 不是看 `HasAbilityEffectTexts`）");
                C(def.EffectTextOf(0, 0, false), "", "B1-a2 它的 `(0,0,buffEffect)` 走的是三态里的 **`\"\"` 明确没有**（不是 `null` 判不了）");
                var rows = RowsFor("Deathwing Terminator", 12);
                Tr(rows != null, "B1-b **前提**：夹具跑得起来");
                if (rows != null)
                {
                    C(FirstWhat(rows), (string)null,
                      "B1 无原文的卡：附魔行**仍是一个洞**（右列空）—— 1,098 张无 `effectText` 的卡文本不变"
                      + "｜🧨 把 `AttachEffectTexts` 改成「有 `BuffType` 就套一个」/ 无条件短路 ⇒ 本行红");
                }
            }
        }

        // ---- B2：来源卡**有** `abilityEffectTexts`，但这一条 `buffType` 对不上任何行 ⇒ 不许猜 ----------
        {
            var def = Def("Helspear Assault");
            Tr(def != null, "B2 **前提**：卡池里找得到 `Helspear Assault`");
            if (def != null)
            {
                Tr(def.HasAbilityEffectTexts, "B2-a **前提**：`Helspear Assault` **有** `abilityEffectTexts`（原文在 `(0,1)` 的 `buffType 15` 上）");
                // `Helspear Assault` 的 buff 行只有 `buffType 3` 与 `15`；用 `12` 去匹配 ⇒ 一条都对不上。
                var rows = RowsFor("Helspear Assault", 12);
                Tr(rows != null, "B2-b **前提**：夹具跑得起来");
                if (rows != null)
                {
                    C(FirstWhat(rows), (string)null,
                      "B2 `buffType` 对不上来源卡任何 buff 行 ⇒ **判不了就不出原文**（⛔ 不按卡名一把抓）"
                      + "｜🧨 把匹配键从 `buffType` 放宽成「只看来源卡有 ET」⇒ 本行红");
                }
            }
        }

        // ---- B3：纯属性行**原路不变**（不经过 effectText 那一条）------------------------------------
        {
            var u = CleanUnit();
            Tr(u != null, "B3 **前提**：干净单位建得出来");
            if (u != null)
            {
                u.AddStatMod("attack", 2);
                var rows = CardDisplayWindow.RowsOfEffects(EffectsToShow.Of(u, true), true);
                string key = "Battle/Effect/ChangeMeleeAttack";
                string want = Loc.T(key);
                if (!string.IsNullOrEmpty(want) && want != key) want = want.Replace("{0}", "+2");
                else want = null;
                Tr(want != null, "B3-a **前提**：`Battle/Effect/ChangeMeleeAttack` 词条取得到（取不到本行无意义）");
                C(FirstWhat(rows), want,
                  "B3 永久 `+2 近战` 那条仍走原合成键（`Battle/Effect/ChangeMeleeAttack` + `+2`）"
                  + "｜🧨 让 `TryAddEffectTextRow` 对**所有**行先试一次 ⇒ 属性行会绕开 `AddStatRows` ⇒ 本行红");
            }
        }
    }
}
