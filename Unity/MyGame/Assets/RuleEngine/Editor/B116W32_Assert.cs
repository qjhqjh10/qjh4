// B116W32_Assert.cs — 执行写手 `B116_W32`（2026-11-02 · 第一百一十六会话 · 活状态表 `B-329`）
//   文件所有权 = `工具/gen_choice_option_cards.py`（新建）· `Resources/choice_option_cards.json`（新建产物）
//              · `Data/CardDatabasePartial_B116W32.cs`（新建）· `Core/ChoiceOptionCards_B116W32.cs`（新建）
//              · `Core/EffectResolver.cs`（**只改文件尾 `TryRunStructuredOptionOrText` 内部**，账最大落点 `:21006` 之后）
//              · 本文件（新建）· `Editor/RuleEngineTest.cs`（**只加同一物理行**）
//
// ============================ 它盯的是什么 ============================
//  `B-329`：`choose one:` 一类卡的**选项卡 SO**（55 张卡的 `choicesDefinition.filterSpecificCards`
//  指向、但不在可玩池里的那 57 个 id）此前 `0/57` 查得到 ⇒ `ENG-18` 的结构化支**整条休眠**，
//  `TL20 Carnifex` 的 `+2 [Melee]` 因此走「未写主语 ⇒ 己方全体」那条**近似**（原版是 `ta = 20 self`）。
//  本件：旁表接上之后，TL20 的选项 SO「Crushing Claws」按它自己的 `ca 40 buffSelf` 结算 ⇒ **只加自己**。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · `decomp_full/BattleManager__GetChoiceFullPool.c:36-55` —— `ta == 180` 且 `filterSpecificCards`
//     非空 ⇒ 候选池**就是那张 PPtr 表**（`TargetCriteria+0x70`，`List<RawCardScript>`，`dump.cs:23870`）；
//     只有它为空才回落 `fullCardCollection`（`:57` 起）⇒ **选项 SO 不是可玩卡**。
//   · `decomp_full/AbilityLogic__PlayAbility.c:2032-2040`（`chosenAbility 360`）⇒
//     `OnTriggerAlt(0 /* ThisCardPlayed */, …)`，`thisCard = AbilityData+0x10` = 父卡所在单位。
//   · `decomp_full/AbilityLogic__PlayAbility.c:1310-1380` ⇒ `ca 40 buffSelf` **硬写 thisCard**（不读 `ta`）。
//
// ============================ 🔴 灭自证 ============================
//   · A5 vs A6：**同一副夹具 / 同一个 op**，只有「旁表里有没有那张 SO」不同 ⇒
//     A5 只有 Carnifex +2（其余友军不变）、A6 己方全体 +2。两条**结构上不可能同时成立**。
//   · A7：把 `ca 60/70/0` 也算成「有执行器」⇒ A7 红（而那会让 6 张 choose-one 卡的选项
//     **半跑 + 文本回落** = 部分双发）。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B116_W32`：`B-329` —— 选项 SO 旁表 + 结构化支（`ENG-18` 从休眠到可达）。</summary>
public static class B116W32_Assert
{
    const string P = "[B116W32] ";

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

    // ============================================================ 夹具（与 `工具/ruleprobe/PlayProbe` 同形）
    static CardDef HeroOf(string id, string fac) =>
        new CardDef(id, id, "hero", "", null, fac, 0, 2, 30, 0, null, subtype: "Warlord");

    static CardDef Fix(string id, int atk, int hp, int rng, string subtype = "Infantry") =>
        new CardDef(id, id, "unit", "", null, "Leviathan", 1, atk, hp, rng, null, subtype: subtype);

    static int HandIdx(BattleContext ctx, int p, CardDef c)
    {
        var h = ctx.Players[p].Hand;
        for (int i = 0; i < h.Count; i++) if (h[i].Card == c) return i;
        return -1;
    }

    static UnitState UnitOf(BattleContext ctx, string name)
    {
        foreach (var u in ctx.Players[0].Units())
            if (u != null && string.Equals(u.Name, name, System.StringComparison.Ordinal)) return u;
        return null;
    }

    /// <summary>打一次 TL20，回三个读数：它自己 / FIX_A / FIX_B 的近战。
    /// <para>`table` 传 `null` ⇒ 旁表空（= 改前行为）。⚠️ 调用方负责恢复 provider。</para></summary>
    static void PlayCarnifex(List<CardDef> pool, List<CardDef> table,
                             out int tlMelee, out int aMelee, out int bMelee, out bool ok)
    {
        BattleContext.ChoiceOptionCardsProvider = () => table ?? new List<CardDef>();
        var tl20 = null as CardDef;
        for (int i = 0; i < pool.Count; i++) if (pool[i] != null && pool[i].Id == "TL20") { tl20 = pool[i]; break; }
        var a = Fix("FIX_A", 2, 5, 0, "Infantry");
        var b = Fix("FIX_B", 3, 5, 0, "Vehicle");
        var d0 = new List<CardDef> { HeroOf("W0", "Leviathan"), a, b, tl20 };   // 取列表末尾 ⇒ TL20 先进手
        var d1 = new List<CardDef> { HeroOf("W1", "Leviathan"), Fix("E1", 1, 6, 0, "Infantry") };
        var vars = GameplayVariables.Classic.Clone();
        vars.startingHand = 3;
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool, vars: vars);
        ctx.Players[0].Energy = 20; ctx.Players[0].MaxEnergy = 20;
        foreach (var c in new[] { a, b })
        {
            int hi = HandIdx(ctx, 0, c);
            if (hi >= 0) RuleCore.PlayCard(ctx, 0, hi, c == a ? 4 : 5);
        }
        int h = HandIdx(ctx, 0, tl20);
        ok = h >= 0;
        if (ok)
        {
            ctx.ChoosePicks.Enqueue(0);                    // 面板选第 1 项（= 原版 `options[0]` 那一路）
            ok = RuleCore.PlayCard(ctx, 0, h, 2) == 0;
        }
        var u = UnitOf(ctx, "Carnifex");
        tlMelee = u != null ? u.Attack : -1;
        aMelee = UnitOf(ctx, "FIX_A") != null ? UnitOf(ctx, "FIX_A").Attack : -1;
        bMelee = UnitOf(ctx, "FIX_B") != null ? UnitOf(ctx, "FIX_B").Attack : -1;
    }

    /// <summary>同一张**战术卡**打一次，返回「能量 / 手牌数 / 友军两格的攻血」的可比串。
    /// <para>用来钉「带 `Oath N` 的平/誓对**整张让路**」—— 它与旁表在不在**必须逐字相同**。</para></summary>
    static string PlayTacticOnce(List<CardDef> pool, CardDef card, List<CardDef> table)
    {
        BattleContext.ChoiceOptionCardsProvider = () => table ?? new List<CardDef>();
        var a = Fix("FIX_A", 2, 5, 0, "Infantry");
        var b = Fix("FIX_B", 3, 5, 0, "Vehicle");
        var d0 = new List<CardDef> { HeroOf("W0", "Leviathan"), a, b, card };
        var d1 = new List<CardDef> { HeroOf("W1", "Leviathan"), Fix("E1", 1, 6, 0, "Infantry") };
        var vars = GameplayVariables.Classic.Clone();
        vars.startingHand = 3;
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool, vars: vars);
        ctx.Players[0].Energy = 20; ctx.Players[0].MaxEnergy = 20;
        foreach (var c in new[] { a, b })
        {
            int hi = HandIdx(ctx, 0, c);
            if (hi >= 0) RuleCore.PlayCard(ctx, 0, hi, c == a ? 4 : 5);
        }
        int h = HandIdx(ctx, 0, card);
        int rc = h < 0 ? -99 : RuleCore.PlayCard(ctx, 0, h, 2);
        var ua = UnitOf(ctx, "FIX_A");
        var ub = UnitOf(ctx, "FIX_B");
        return $"rc={rc} E={ctx.Players[0].Energy} hand={ctx.Players[0].Hand.Count}"
             + $" A={(ua != null ? ua.Attack + "/" + ua.Health : "-")} B={(ub != null ? ub.Attack + "/" + ub.Health : "-")}";
    }

    // ============================================================ 入口
    public static void Run()
    {
        Debug.Log(P + "=== B-329：选项 SO 旁表（choose one: 的选项卡 SO 接进第三张表）===");
        var pool = CardDatabase.Load();
        if (!Ok(pool != null && pool.Count > 0, "A1 前提：可玩池装起来了（" + (pool != null ? pool.Count : 0) + " 张）"))
            return;

        // ---- A2 前提：TL20 的 rally 真的挂着原版选项表（拿不到 ⇒ A5/A6 等于没验）----
        CardDef tl20 = null;
        for (int i = 0; i < pool.Count; i++) if (pool[i] != null && pool[i].Id == "TL20") { tl20 = pool[i]; break; }
        if (!Ok(tl20 != null, "A2 前提：可玩池里有 `TL20 Carnifex`")) return;
        var rally = tl20.TriggerOps("rally");
        EffectOp chooseOp = null;
        if (rally != null) for (int i = 0; i < rally.Count; i++)
            if (rally[i] != null && rally[i].Verb == "chooseone") { chooseOp = rally[i]; break; }
        if (!Ok(chooseOp != null && chooseOp.ChooseSpecificIds != null && chooseOp.ChooseSpecificIds.Count == 3
                && chooseOp.Amount == 3,
                "A2 前提：TL20 的 `chooseone` 挂着 3 个选项 id（`B116_W6` 的注入闸）"))
            return;
        var optIds = new List<string>(chooseOp.ChooseSpecificIds);

        // ---- A3：旁表真的装起来了，且**池外选项 id 一个不缺** ----
        var table = CardDatabase.ParseChoiceOptionCards(
            (Resources.Load<TextAsset>(CardDatabase.ChoiceOptionCardsResourcePath) ?? new TextAsset(string.Empty)).text);
        if (!Ok(table.Count > 0, "A3 前提：旁表 `choice_option_cards.json` 读到了（" + table.Count + " 张）")) return;
        var have = new HashSet<string>(System.StringComparer.Ordinal);
        for (int i = 0; i < table.Count; i++) if (table[i] != null) have.Add(table[i].Id);
        int need = 0, miss = 0;
        var seenIds = new HashSet<string>(System.StringComparer.Ordinal);
        var poolIds = new HashSet<string>(System.StringComparer.Ordinal);
        for (int i = 0; i < pool.Count; i++) if (pool[i] != null && !string.IsNullOrEmpty(pool[i].Id)) poolIds.Add(pool[i].Id);
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null || c.ChoicesSpecificCardIds == null) continue;
            foreach (var oid in c.ChoicesSpecificCardIds)
            {
                if (string.IsNullOrEmpty(oid) || poolIds.Contains(oid) || !seenIds.Add(oid)) continue;
                need++;
                if (!have.Contains(oid)) { miss++; Debug.LogError(P + "   ✗ 旁表缺「" + c.Name + "」的选项 " + oid); }
            }
        }
        Eq(miss, 0, "★ A3 池内卡的**全部池外选项 id** 都在旁表里（缺 " + miss + " 个）");
        Eq(need, table.Count, "★ A3b 旁表张数 == 「池内卡的池外选项 id」去重数（多/少都是口径错）");

        // ---- A4：TL20 的三个选项 SO 在旁表里，行 = `ca 40` + `ta 20` ----
        int found = 0, rowOk = 0;
        foreach (var oid in optIds)
        {
            CardDef opt = null;
            for (int i = 0; i < table.Count; i++) if (table[i] != null && table[i].Id == oid) { opt = table[i]; break; }
            if (opt == null) continue;
            found++;
            var rows = opt.TriggerAbilityRowsOf(0);
            if (rows != null && rows.Length == 1 && rows[0] != null && rows[0].Ca == 40 && rows[0].Ta == 20) rowOk++;
        }
        Eq(found, 3, "★ A4 TL20 的 3 个选项 SO 都能按 id 查到");
        Eq(rowOk, 3, "★ A4b 三行都是 `ca 40 buffSelf` + `ta 20 self`（判据 `PlayAbility.c:1310-1380`）");

        // ---- A5 / A6：灭自证那一对 ----
        var prev = BattleContext.ChoiceOptionCardsProvider;
        try
        {
            PlayCarnifex(pool, table, out int tl, out int fa, out int fb, out bool ok1);
            if (!Ok(ok1, "A5 前提：TL20 打得出去（rc=0）"))
            { /* 下面两条读数仍然要打出来 */ }
            Eq(tl, 6, "★ A5 **旁表已接**：Carnifex 自己 4 → 6（+2 melee，选项 SO 的 `ca 40 buffSelf`）");
            Eq(fa, 2, "★ A5b 友军 FIX_A **不变**（2）—— 原版 `ta 20 self`，⛔ 不是「己方全体」");
            Eq(fb, 3, "★ A5c 友军 FIX_B **不变**（3）");

            PlayCarnifex(pool, null, out int tl2, out int fa2, out int fb2, out bool ok2);
            Ok(ok2, "A6 前提：旁表为空时 TL20 照样打得出去（⛔ 不许静默不生效）");
            Eq(tl2, 6, "A6 灭自证：旁表为空 ⇒ 回落文本支，Carnifex 自己同样 +2");
            Eq(fa2, 4, "★ A6b 灭自证：回落文本 ⇒ FIX_A **也被 +2**（2 → 4）—— 与 A5b 结构上不可能同时成立");
            Eq(fb2, 5, "★ A6c 灭自证：回落文本 ⇒ FIX_B **也被 +2**（3 → 5）");
        }
        finally { BattleContext.ChoiceOptionCardsProvider = prev; }   // 🔴 必须还原：它是进程级静态

        // ---- A7：本版的非正文执行器只覆盖 `ca 30/40/50`（6 张 choose-one 卡的选项行都不在其中 ⇒ 零回归）----
        Ok(RuleCore.HasChoiceOptionRowExecutor(40) && RuleCore.HasChoiceOptionRowExecutor(30)
           && RuleCore.HasChoiceOptionRowExecutor(50), "A7 前提：`ca 30/40/50` 有执行器");
        Ok(!RuleCore.HasChoiceOptionRowExecutor(60) && !RuleCore.HasChoiceOptionRowExecutor(70)
           && !RuleCore.HasChoiceOptionRowExecutor(0) && !RuleCore.HasChoiceOptionRowExecutor(100),
           "★ A7 灭自证：`ca 60/70/0/100` **没有**执行器 ⇒ 那 6 张 `choose one:` 卡的选项表"
         + "**整张让路**、照旧回落文本（半跑 + 文本回落 = 部分双发，⛔ 不许）");

        // ---- A8：带 `Oath N` 的「平 / 誓」对**整张让路**（旁表在不在必须逐字相同）----
        //   判据：那 10 张卡的 `filterSpecificCards` 是 [平, 誓] 两张（`CardDef.OathChoiceNeeded` 的 2 张约定），
        //   不是 `choose one:` 表。放它进结构化支 ⇒ 只跑选中那一项的 `buffEffect` ⇒ **誓约加值与付费一起丢**。
        var oathCard = null as CardDef;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && pool[i].Id == "8482077f309b08943b190f18b5874871") { oathCard = pool[i]; break; }
        if (Ok(oathCard != null && oathCard.OathCost > 0,
               "A8 前提：池里有 `Catechism of Death` 且 `OathCost` > 0（拿不到 ⇒ A8b 等于没验）"))
        {
            string withTbl, withoutTbl;
            var prev2 = BattleContext.ChoiceOptionCardsProvider;
            try
            {
                withTbl = PlayTacticOnce(pool, oathCard, table);
                withoutTbl = PlayTacticOnce(pool, oathCard, null);
            }
            finally { BattleContext.ChoiceOptionCardsProvider = prev2; }
            Ok(withTbl == withoutTbl,
               "★ A8b 平/誓对（`Oath " + oathCard.OathCost + "`）**整张让路**：旁表接上 与 旁表为空 读数逐字相同"
             + $"｜接上 {withTbl}｜为空 {withoutTbl}");
        }

        Debug.Log(P + $"=== 合计：通过 {Pass} · 失败 {Fail} ===");
    }
}
