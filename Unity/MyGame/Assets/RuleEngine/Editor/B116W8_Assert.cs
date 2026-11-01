// B116W8_Assert.cs — 执行代理 `B116_W8`（2026-11-02 · 第一百一十六会话 · 引擎线）
//   文件所有权 = `RuleEngine/Core/EffectResolverPartial_B97W2.cs` ＋ 本文件（新建）
//
// ============================ 它盯的是什么 ============================
//  活状态表 `ENG-11`：第二张表 `enchantment_templates.json`（86 模板 / `abilityBuffs` 合计 **89 行**）
//  🔴 2026-11-02（执行代理 `B116_W28`）：收录闸放宽（名字尾 `_enchantment` ∪ 被非零 PPtr 指向）⇒ 85→86 张、
//     86→89 行（新收的 `Raging Storm_enchantment1` 带 3 条 `buffType 1` 行）。分母随之改，见 C1。
//  里还有 **37 行**落到 `ApplyAbilityBuff` 的 `default:`（= 出声、不改状态）。本批按原版 `.c` 接上其中
//  **能接的 4 行**：`8 markOfChaos` ×3 ＋ `90 addTraitRemnant` ×1。
//  剩下的 33 行（`2 changeCost` 15 · `12 enchantment` 13 · `15 singleUseEnchantment` 5）**仍不结算**，
//  理由分别写在 `case 2` 头注与 `ApplyAttachEnchantmentBuff` 的 remarks 里（⛔ 别在这里抄第二份）。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · `d:/2/tools/decomp_full/CardScript__AddEffect.c`
//       `:776-793` = `case 8`（`Enchantments.GetMarkOfChaos` → `TriggerEnchantment` → `bm.lastMarkOfChaos`
//         → `BroadcastMarkResolved`）· `:220` = `if (iVar16 == 0x5a) goto switchD_1805e5668_caseD_3;`
//         （**`90 addTraitRemnant` 与原版 `case 3` 是同一个体**）· `:826-829` / `:830-932` = `12/15` 与 `16`。
//   · `d:/2/tools/decomp_full/Enchantments__GetMarkOfChaos.c` 全文（遍历 → 命中回卡 / 走到底回 `null`）。
//   · 我们的等价位 = `RuleCore.GrantDarkPact`（`EffectResolver.cs`，与正文路 `give … a Dark Pact` 同一条）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()`。⚠️ **该文件本批由 `B116_W9` 占用**
//  （`文件所有权_当前.md` / 简报），⇒ 本代理**没有**加这一行，请调度台在任一条既有 `Guard(...)` 物理行上追加：
//      Guard("B116W8_Assert", B116W8_Assert.Run); _pass += B116W8_Assert.Pass; _fail += B116W8_Assert.Fail;
//  🔴 **没接上 = 一条都不跑**（本文件已在报告里标成「待接线」，⛔ 别当成已验收）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A 段：把 `case 3: case 90:` 的 `case 90:` 删掉 ⇒ D1/D2 红（它落回 `default:`，`shield` 挂不上、返回 false）。
//   · A 段：把 `DarkPactKindOfMarkType` 的 `30 → "excess"` 改成别的种类 ⇒ A3 红（近战/远程不是各 +2）。
//   · A 段：把 `ApplyMarkOfChaosBuff` 改成「只出声、不改状态」⇒ A1/A3 红（`darkpact` 层数仍 0）。
//   · A 段：`case 8` 分支改成恒 `true`（不调 `GrantDarkPact`）⇒ A1 红（层数没涨）。
//   · B 段：把 `ApplyAttachEnchantmentBuff` 改成「返回 true」⇒ B1/B2 红（原版这一步是「挂了才会返回值」，
//     我们没那张模板 ⇒ 必须 `false` 且**一张都不挂**）。
//   · C 段：数据分布断言 —— 生成器/表一变（`buffType` 分布不再是那 7 档）⇒ C1 红，提醒重算「剩几行」。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B116_W8`：`ENG-11` —— `CardScript.AddEffect` 的 `8 markOfChaos` / `90 addTraitRemnant`。</summary>
public static class B116W8_Assert
{
    const string P = "[B116W8] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    static bool Eq(int got, int want, string msg)
    {
        if (got == want) { Pass++; Debug.Log(P + $"   ✓ {msg}（实际 {got}）"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}｜实际 {got}，期望 {want}"); return false;
    }

    // ============================================================ 入口
    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            SectionA_MarkOfChaos();
            SectionB_EnchantmentBuffs();
            SectionC_DataDistribution();
        }
        catch (System.Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== B116W8 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ============================================================ 夹具
    //  ⚠️ 走真实 `RuleCore.NewBattle`（`shuffle: false`）—— 与 `RuleEngineTest` / `B102R1_Assert` 同口径。
    //     本文件是**独立**静态类 ⇒ 拿不到宿主那边 `private` 的夹具，自己建一份（同 `B102R1_Assert` 先例）。

    static CardDef Hero(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 0, 30, 0, null, subtype: "Warlord");
    }

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B116W8Warlord0", "SpaceWolves") };
        var d1 = new List<CardDef> { Hero("B116W8Warlord1", "Goff") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        // ⚠️ `Exhausted = false` 必须显式写：`UnitState` 的 ctor 恒 `true`（同 `B102R1_Assert.Place`）。
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = false };
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static CardDef Body(string id) => new CardDef(id, id, "unit", "", null, "Test", 1, 2, 5, 0);

    static CardDatabase.AbilityBuffSpec Spec(int buffType, int markType = 0, int trait = 0, int traitValue = 0)
    {
        return new CardDatabase.AbilityBuffSpec
        {
            BuffType = buffType,
            MarkOfChaosType = markType,
            Trait = trait,
            TraitValue = traitValue,
        };
    }

    // ============================================================
    //  A · `buffType 8 markOfChaos`（`.c:776-793`）—— 本批**真结算**的那一档
    // ============================================================
    static void SectionA_MarkOfChaos()
    {
        Debug.Log(P + "--- A · `case 8 markOfChaos`（原版 `.c:776-793` → 我们的 `GrantDarkPact`）---");

        // A1/A3：`markOfChaosType 30` = Slaanesh = `excess` =「+2 近战 / +2 远程」（`DarkPactFx`）
        {
            var c = Body("B116W8A1");
            var ctx = NewCtx();
            var u = Place(ctx, 0, 3, c);
            Ok(u != null, "A1 **前提**：单位摆上了棋盘");
            Eq(u.KwValue(KeywordTable.DarkPact), 0, "A1′ **前提**：它身上本来没有黑暗契约");
            Ok(!u.Has(KeywordTable.MarkOfChaos), "A1″ **前提**：它本来没有 `markofchaos`");
            int atk0 = u.Attack, rng0 = u.RangedAttack;

            bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u, Spec(8, markType: 30), "B116W8:A1");
            Ok(ok, "A1 `markOfChaos(8)` 落地（改前这一步走 `default:` ⇒ 回 false 且不改状态）");
            Eq(u.KwValue(KeywordTable.DarkPact), 1,
               "A1‴ 契约层数 1（= 原版那 4 张 `Mark of X (enchantment)` 的 500 能力第一条 `addTrait(330)`）");
            Ok(u.Has(KeywordTable.MarkOfChaos),
               "A1⁗ `markofchaos(330)` 挂上了（判据 `.c:778-786` → `TriggerEnchantment` → `addTrait(330)`）");
            Eq(u.Attack - atk0, 2,
               "A3 🧨 `markOfChaosType 30` = `excess` ⇒ 近战 **+2**（`Enchantments.json`：`30 Slaanesh`）；映射写错 ⇒ 这里红");
            Eq(u.RangedAttack - rng0, 2, "A3′ 🧨 同上，远程 **+2**（`excess` = `Give +2 Melee Attack and +2 Ranged Attack`）");
            Ok(RuleCore.LastMarkOfChaos(ctx) == "excess",
               "A3″ 🧨 `bm.lastMarkOfChaos(+0x2f0)` 被写成 `excess`（原版 `.c:788-790`；⛔ 不写槽 ⇒ 这里红）");
        }

        // A2：四条「种类 → 数字」映射逐条核（`10/20/30/40`），各摆一个单位，读 `lastMarkOfChaos`
        {
            int[] types = { 10, 20, 30, 40 };
            string[] kinds = { "blood", "resilience", "excess", "fate" };
            for (int i = 0; i < types.Length; i++)
            {
                var ctx = NewCtx();
                var u = Place(ctx, 0, 3 + i, Body("B116W8A2_" + types[i]));
                RuleCore.ApplyAbilityBuff(ctx, 0, u, Spec(8, markType: types[i]), "B116W8:A2");
                Ok(RuleCore.LastMarkOfChaos(ctx) == kinds[i],
                   $"A2 🧨 `markOfChaosType {types[i]}` ⇒ 种类 `{kinds[i]}`"
                 + "（映射 = `EffectResolver.MarkOfChaosTypeOf` 的反向；两处写不同表 ⇒ 这里红）");
            }
        }

        // A4：`50` = Random（`Enchantments.json` 的同一格挂了 4 张卡 ⇒ 均匀四选一）—— 至少要落一份
        {
            var ctx = NewCtx();
            var u = Place(ctx, 0, 3, Body("B116W8A4"));
            bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u, Spec(8, markType: 50), "B116W8:A4");
            Ok(ok, "A4 `markOfChaosType 50`（Random）也落一份印记");
            Eq(u.KwValue(KeywordTable.DarkPact), 1, "A4′ 层数 +1（四选一，只落一份）");
            string k = RuleCore.LastMarkOfChaos(ctx);
            bool inSet = k == "blood" || k == "resilience" || k == "excess" || k == "fate";
            Ok(inSet, $"A4″ 选中的种类在四种之内（实得 `{k ?? "null"}`）");
        }

        // A5：「取不到附魔卡」那一档 —— 原版 `.c:782` 的 null 闸 ⇒ **整条不发生**（不是「随便挑一种」）
        {
            int[] bad = { 0, 99 };
            foreach (int mt in bad)
            {
                var ctx = NewCtx();
                var u = Place(ctx, 0, 3, Body("B116W8A5"));
                var unresolved = new List<string>();
                bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u, Spec(8, markType: mt), "B116W8:A5", unresolved);
                Ok(!ok, $"A5 🧨 `markOfChaosType {mt}` 认不出 ⇒ **返回 false**（⛔ 不猜一种顶上）");
                Eq(u.KwValue(KeywordTable.DarkPact), 0, $"A5′ 🧨 同上：层数仍是 0（`markOfChaosType {mt}`）");
                Ok(unresolved.Count == 1, $"A5″ 进 `unresolved`（实得 {unresolved.Count} 条，期望 1）");
            }
        }
    }

    // ============================================================
    //  B · `buffType 12 / 15 / 16` —— **本批仍不结算**（缺 `CardEffect.enchantment(+0x88)` 那一列）
    //   ⚠️ 这两条断的是「**出声且什么都不挂**」：⛔ 别把「没做」写成「做了」
    // ============================================================
    static void SectionB_EnchantmentBuffs()
    {
        Debug.Log(P + "--- B · `case 12/15/16`（`.c:826-829` / `:830-932`）—— 仍不结算，出声 ---");
        int[] bts = { 12, 15, 16 };
        foreach (int bt in bts)
        {
            var ctx = NewCtx();
            var u = Place(ctx, 0, 3, Body("B116W8B" + bt));
            var unresolved = new List<string>();
            int before = u.Instance != null ? u.Instance.ActiveEnchantments.Count : -1;
            bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u, Spec(bt), "B116W8:B", unresolved);
            Ok(!ok, $"B1 `buffType {bt}` **返回 false**（认不出要挂哪张模板 ⇒ 不许假装挂了）");
            Ok(unresolved.Count == 1, $"B1′ `buffType {bt}` 进 `unresolved`（实得 {unresolved.Count} 条，期望 1）");
            int after = u.Instance != null ? u.Instance.ActiveEnchantments.Count : -1;
            Eq(after - before, 0, $"B2 🧨 `buffType {bt}` **一张附魔都没挂上**（`ActiveEnchantments` 没涨）");
        }
    }

    // ============================================================
    //  C · 数据分布 + 真模板端到端（第二张表）
    // ============================================================
    static void SectionC_DataDistribution()
    {
        Debug.Log(P + "--- C · 第二张表的 89 行分布 ＋ 真模板 `Undying Legions` 的 `90` 行 ---");
        // 先把 Data 侧的两个钩子装上（`CardDatabase.Load()` 会设 `BattleContext.EnchantmentTemplatesProvider`）
        CardDatabase.Load();
        var ctx = NewCtx();

        // C0：`case 90` 的等价位 = `addTrait`（trait 80 = `shield`；`DefinedTrait` 表 `CardDefPartial_B94B16.cs`）
        {
            var u = Place(ctx, 0, 3, Body("B116W8C0"));
            Ok(!u.Has("shield"), "C0 **前提**：它本来没有 `shield`");
            bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u, Spec(90, trait: 80, traitValue: 0), "B116W8:C0");
            Ok(ok, "C0′ `addTraitRemnant(90)` 落地（原版 `.c:220` 直接跳进 `case 3` 那段；`case 90:` 被删 ⇒ false）");
            Ok(u.Has("shield"), "C0″ 🧨 `shield` 挂上了（trait 80 = `shield`，`DefinedTrait` 表）");
        }

        // C1：86 张模板 / 89 行 / 分布 —— 钉住「37 行里接上 4 行」这个口径的分母
        var tmpls = CardDatabase.LoadEnchantmentTemplates();
        Ok(tmpls != null && tmpls.Count == 86, $"C1 **前提**：第二张表 86 张模板（实得 {(tmpls != null ? tmpls.Count : -1)}）");
        if (tmpls == null) return;
        var byBt = new Dictionary<int, int>();
        int total = 0;
        for (int i = 0; i < tmpls.Count; i++)
        {
            var rows = CardDatabase.AbilityBuffsOf(tmpls[i]);
            if (rows == null) continue;
            for (int j = 0; j < rows.Count; j++)
            {
                int bt = rows[j].BuffType;
                byBt[bt] = (byBt.ContainsKey(bt) ? byBt[bt] : 0) + 1;
                total++;
            }
        }
        Eq(total, 89, "C1′ `abilityBuffs` 合计 89 行（新收的模板带 3 条 `buffType 1` 行）");
        Eq(byBt.ContainsKey(1) ? byBt[1] : 0, 28, "C1″ `1 addStats` 28 行（✅ 早已生效；`B116_W28` 新收 3 行）");
        Eq(byBt.ContainsKey(3) ? byBt[3] : 0, 24, "C1‴ `3 addTrait` 24 行（✅ 早已生效）");
        Eq(byBt.ContainsKey(2) ? byBt[2] : 0, 15, "C1⁗ `2 changeCost` 15 行（按既有裁定不接）");
        Eq(byBt.ContainsKey(12) ? byBt[12] : 0, 13, "C1⁵ `12 enchantment` 13 行（缺 `enchantment` 列 ⇒ 不接）");
        Eq(byBt.ContainsKey(15) ? byBt[15] : 0, 5, "C1⁶ `15 singleUseEnchantment` 5 行（同上）");
        Eq(byBt.ContainsKey(8) ? byBt[8] : 0, 3, "C1⁷ `8 markOfChaos` 3 行（**本批接上**）");
        Eq(byBt.ContainsKey(90) ? byBt[90] : 0, 1, "C1⁸ `90 addTraitRemnant` 1 行（**本批接上**）");

        // C2：端到端 —— 真模板 `Undying Legions`（`SAU53` 的「打出即挂」那 3 张之一）的 `90` 行
        {
            var tmpl = ctx.FindEnchantmentTemplate("eab54e4c131034e4dacf8ce72807ed07");
            Ok(tmpl != null, "C2 **前提**：`ctx.FindEnchantmentTemplate` 找得到 `Undying Legions` 模板");
            if (tmpl == null) return;
            var u = Place(ctx, 0, 4, Body("B116W8C2"));
            var unresolved = new List<string>();
            int n = RuleCore.ResolveAbilityBuffRows(ctx, 0, u, tmpl, 0, 0, false, "B116W8:C2", unresolved);
            Eq(n, 1, "C2′ 🧨 真模板的 `(ab0, lg0)` 那一行结算了 1 条（改前 = 0，落 `default:`）");
            Ok(u.Has("shield"), "C2″ 🧨 载体拿到了 `shield`（模板 `trait 80 / traitValue 0`）");
            Eq(unresolved.Count, 0, "C2‴ 没有未解析项");
        }
    }
}
