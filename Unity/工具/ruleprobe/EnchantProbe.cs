// EnchantProbe.cs — `enchant` 子命令：附魔「打出即挂」反查表（`CardDatabase._enchantGrants`）的**只读**读数口
//
// 【为什么要它】账 `B124-TA10COVER` 的 ruleprobe 半：`Program.LoadPool` 走 `System.Text.Json` 手读卡表、
//   **不过 `CardDatabase.Load()`** ⇒ `EnsureEnchantmentGrantsFrom` 从来没被调过 ⇒ `_enchantGrants` 恒 null
//   ⇒ `Enchantments.EnchantmentGrantsOf` 恒返 null ⇒ `play` 对那 29 张卡的「打出即挂」**永久失明**
//   （病因与「provider 是 null」不同 —— provider 早就被 `Data/CardDatabase.cs:3662` 的静态构造注入了，
//     见 判据/B148_战斗表现施工单.md §5·1）。
//   ⇒ 修法 = `Program.LoadPool` 里补一句 `CardDatabase.EnsureEnchantmentGrantsFrom(<卡表原文>)`；
//     本探针是「补完之后到底看见了多少」的读数口。
//
// 【与 `check` / `play` 的边界】⛔ 它**不进**任何指纹路径：不写基线、不改 `Pinned*Md5`、不改 `check`/`play`
//   的输出。`rc` 只在**本探针自己的反例自检红了**（或表根本没建起来）时才非 0。
//
// 【反例自检（改坏就红）】施工单 §5·5 给了三条，这里落三条里能离线跑的那几条 ——
//   ⚠️ 每条量的都是**引擎口径**（`Enchantments.GrantedEnchantmentRowsOf` / `Attachments` 的静态计数），
//     ⛔ 不是本文件自己另算一份 —— 那样是自证。
//   · 反例 1：**不建表**（= 注释掉 `LoadPool` 里那一句的效果）⇒ 可挂行数掉回白名单那 2 张（在池里）。
//   · 反例 2：把某行的 `token` 改成不存在的 id ⇒ `Enchantments.GrantedTemplateMissing` +1。
//   · 反例 3：把某行的 `trigger` 从 0 改成 655 ⇒ 那一行掉出「可挂」，引擎读出来 `Trigger == 655`。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RuleEngine;

class EnchantProbe
{
    static int _fail;

    static void Check(bool ok, string what)
    {
        Console.WriteLine((ok ? "  ✓ " : "  ✗ ") + what);
        if (!ok) _fail++;
    }

    /// <summary>一池子的汇总（逐行分类与 `Enchantments.AttachGrantedByPlayedCard` 的闸**同一档次序**）。</summary>
    struct Tally
    {
        public int CardsWithRows;      // 有数据行的卡
        public int Rows;               // 数据行总数
        public int FromDataCards;      // 其中「来自数据表」的卡（= provider 给了非空列表）
        public int AttachRows;         // 本钩子认的行（trigger==0 且 ta!=50，**去重前**）
        public int AttachCards;
        public int AttachResolvable;   // 上面那些行里，模板 id **在卡池里也有一张卡**（探针里查得到）的行数
        public int EffectiveRows;      // 再按引擎那套 (Trigger,BuffType,TemplateCardId) 去重后**真正会挂**的行
        public int SkipTrigger;        // trigger != 0
        public int SkipHand;           // ta == 50（手牌载体，走 DA42 专属链）
        public int DupSkipped;         // 同一次出牌里重复的 (Trigger,BuffType,TemplateCardId)
    }

    static Tally Count(IList<CardDef> pool)
    {
        var t = new Tally();
        var prov = Enchantments.GrantedEnchantmentsProvider;
        for (int ci = 0; ci < pool.Count; ci++)
        {
            var def = pool[ci];
            if (def == null) continue;
            var rows = Enchantments.GrantedEnchantmentRowsOf(def);
            if (rows == null || rows.Count == 0) continue;
            t.CardsWithRows++;
            t.Rows += rows.Count;
            if (prov != null)
            {
                var data = prov(def.Id);
                if (data != null && data.Count > 0) t.FromDataCards++;
            }
            int attach = 0, raw = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                var g = rows[i];
                if (g == null) continue;
                if (g.Trigger != Enchantments.TriggerThisCardPlayed) { t.SkipTrigger++; continue; }
                if (g.TargetsAffected == Enchantments.TargetsAffectedInHand) { t.SkipHand++; continue; }
                raw++;                       // 「本钩子认的」= 去重**前**（施工单 §5·2 的 31 行 / 28 张就是这一档）
                if (FindCard(pool, g.TemplateCardId) != null) t.AttachResolvable++;
                bool dup = false;
                for (int k = 0; k < i; k++)
                    if (rows[k] != null && rows[k].Trigger == g.Trigger
                        && rows[k].BuffType == g.BuffType && rows[k].TemplateCardId == g.TemplateCardId)
                    { dup = true; break; }
                if (dup) { t.DupSkipped++; continue; }
                attach++;
            }
            t.AttachRows += raw;
            t.EffectiveRows += attach;
            if (raw > 0) t.AttachCards++;
        }
        return t;
    }

    static void PrintTally(string tag, Tally t)
    {
        Console.WriteLine($"  [{tag}] 有数据行的卡 {t.CardsWithRows} 张 / {t.Rows} 行"
                        + $"（其中来自数据表 {t.FromDataCards} 张）");
        Console.WriteLine($"  [{tag}] 本钩子认的（trigger==0 且 ta!=50）：{t.AttachRows} 行 / {t.AttachCards} 张"
                        + $" · 去重后真正会挂 {t.EffectiveRows} 行（重复跳过 {t.DupSkipped}）");
        Console.WriteLine($"  [{tag}] 跳过：trigger!=0 {t.SkipTrigger} 行 · ta==50（手牌载体）{t.SkipHand} 行");
        Console.WriteLine($"  [{tag}] 其中模板在探针里查得到的：{t.AttachResolvable} 行"
                        + "（探针的 `Resources` 是桩 ⇒ 第二张表 `enchantment_templates.json` 读不到，"
                        + "只有 id 恰好也是池卡的模板才查得到 —— 见施工单 §5·7；⛔ 这**不是**判据，只用来解释 `play` 的变化面）");
    }

    /// <summary>`enchant [selftest]`。返回 0 = 全绿；1 = 本探针自检红；2 = 前提不成立（卡池空 / 表没建）。</summary>
    public static int Run(IList<CardDef> pool, bool selfTest)
    {
        if (pool == null || pool.Count == 0)
        {
            Console.Error.WriteLine("!! enchant：卡池是空的（`LoadPool` 没跑？）—— 本探针**不出读数**，⛔ 别当成 0 张。");
            return 2;
        }

        Console.WriteLine("=== 附魔「打出即挂」反查表（ruleprobe · B148 W5 · 只读）===");
        bool hasProvider = Enchantments.GrantedEnchantmentsProvider != null;
        Console.WriteLine("  provider            : " + (hasProvider
            ? "非 null（`Data/CardDatabase.cs:3662` 静态构造已注入）"
            : "null（!! 未注入 —— 引擎只能退白名单 3 张）"));
        Console.WriteLine("  卡池                : " + pool.Count + " 张");

        var t = Count(pool);
        PrintTally("现读", t);

        // 前提闸：表到底建起来没有（这一条就是 `B124-TA10COVER` 的验收）
        Check(hasProvider, "前提：`GrantedEnchantmentsProvider` 非 null（不是 null 那一版病因）");
        Check(t.FromDataCards > 0,
              $"前提：反查表**已建**（来自数据表的卡 {t.FromDataCards} 张 > 0）"
            + "｜🧨 删掉 `Program.LoadPool` 里那句 `CardDatabase.EnsureEnchantmentGrantsFrom(...)` ⇒ 这里变 0 ⇒ 红");

        // 与施工单的期望值对一眼（只打印，不硬断 —— 卡表会版本化，硬断数字会变成「改数据就红」）
        Console.WriteLine($"  📌 判据/B148_战斗表现施工单.md §5·2：trigger==0 共 **32 行 / 29 张**，其中 `ta==50` 跳过 1 行"
                        + $" ⇒ **净 31 行 / 28 张**（现读 {t.AttachRows} 行 / {t.AttachCards} 张；按引擎去重后真正会挂 "
                        + $"{t.EffectiveRows} 行，重复跳过 {t.DupSkipped}）· trigger!=0 期望 **26 行**（现读 {t.SkipTrigger}）");

        // ---- 逐卡明细（只列有行的；行数不多，全打）----
        Console.WriteLine("  --- 逐卡（id / 名称 / 行数 / 每行 token,buffType,trigger,ta,ab,lg）---");
        for (int ci = 0; ci < pool.Count; ci++)
        {
            var def = pool[ci];
            if (def == null) continue;
            var rows = Enchantments.GrantedEnchantmentRowsOf(def);
            if (rows == null || rows.Count == 0) continue;
            var parts = new List<string>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                var g = rows[i];
                if (g == null) { parts.Add("null"); continue; }
                // `✔池` = 这张模板的 id **在卡池里也有一张卡** ⇒ 探针里 `ctx.FindEnchantmentTemplate` 能查到它
                // （探针的 `Resources` 是桩 ⇒ 第二张表 `enchantment_templates.json` **读不到**，见施工单 §5·7）。
                // 这一格只解释「为什么 `play` 里只有少数几张显出变化」，⛔ 不是判据。
                string inPool = FindCard(pool, g.TemplateCardId) != null ? "✔池" : "✘池";
                parts.Add($"{g.TemplateCardId}{inPool},bt{g.BuffType},tr{g.Trigger},ta{g.TargetsAffected},ab{g.AbilityIndex},lg{g.LogicIndex}");
            }
            Console.WriteLine($"    {def.Id} / {def.Name} / {rows.Count} 行 / " + string.Join(" ｜ ", parts));
        }

        // ---- 白名单那 3 张与数据的一致性（换成数据源是纯扩面，不该改这 3 张的结果）----
        Console.WriteLine("  --- 白名单 3 张 vs 数据表 ---");
        var wl = Enchantments.PlayerGrantedEnchantments;
        for (int i = 0; i < wl.Length; i++)
        {
            var w = wl[i];
            if (w == null) continue;
            var data = Enchantments.GrantedEnchantmentsProvider != null
                       ? Enchantments.GrantedEnchantmentsProvider(w.SourceCardId) : null;
            int n = data == null ? 0 : data.Count;
            bool tokenMatch = false;
            if (data != null)
                for (int k = 0; k < data.Count; k++)
                    if (data[k] != null && data[k].TemplateCardId == w.TemplateCardId) { tokenMatch = true; break; }
            Console.WriteLine($"    {w.SourceCardId}：数据 {n} 行 · 白名单模板 {(tokenMatch ? "在数据里" : "不在数据里")}"
                            + $"{(n > 1 ? "  ← 挂载面比白名单大（原版真值如此，见施工单 §5·2 的 TL47）" : "")}");
        }

        // ---- 诊断计数（本进程里没真打牌的话恒 0；真值要跑 `play`）----
        Console.WriteLine($"  --- 引擎诊断计数（本进程现值）--- SkippedNonPlayTrigger={Enchantments.GrantedSkippedNonPlayTrigger}"
                        + $" · TemplateMissing={Enchantments.GrantedTemplateMissing}"
                        + $" · SkippedHandTarget={Enchantments.GrantedSkippedHandTarget}"
                        + $" · TargetResolveFallback={Enchantments.GrantedTargetResolveFallback}"
                        + $" · CarrierNoInstance={Enchantments.GrantedCarrierNoInstance}");

        if (selfTest) RunSelfTest(pool, t);

        Console.WriteLine(_fail == 0 ? "=== enchant 合计：失败 0 条 ===" : $"=== enchant 合计：失败 {_fail} 条 ===");
        return _fail > 0 ? 1 : 0;
    }

    // ==========================================================================================
    //  反例自检（改坏就红）
    // ==========================================================================================
    static void RunSelfTest(IList<CardDef> pool, Tally baseline)
    {
        Console.WriteLine("=== 反例自检（改坏就红；判据 → 判据/B148_战斗表现施工单.md §5·5）===");
        string fullJson = File.ReadAllText(Program.PoolPath);

        // ---------- 反例 1：表没建（= 注释掉 LoadPool 里那一句的效果）⇒ 只剩白名单 ----------
        CardDatabase.InvalidateEnchantmentGrants();
        var noTable = Count(pool);
        Console.WriteLine($"    反例1 · 表没建：可挂 {noTable.AttachRows} 行 / {noTable.AttachCards} 张（现读 {baseline.AttachRows} 行）");
        Check(noTable.AttachRows < baseline.AttachRows,
              $"反例1 红点：表没建 ⇒ 可挂行数从 {baseline.AttachRows} 掉到 {noTable.AttachRows}");
        Check(noTable.FromDataCards == 0, "反例1：表没建时「来自数据表的卡」== 0");
        // 白名单兜底仍在（这 3 张里在池内的那几张）
        Check(noTable.AttachRows > 0, "反例1：表没建时仍能读到白名单兜底（不是 0 行）");
        // ⚠️ 先把真表装回去再往下走 —— 否则下面找夹具时 provider 还是 null（`Invalidate` 是全局的）
        CardDatabase.InvalidateEnchantmentGrants();
        CardDatabase.EnsureEnchantmentGrantsFrom(fullJson);
        var restoredAfterCe1 = Count(pool);
        Check(restoredAfterCe1.AttachRows == baseline.AttachRows,
              $"反例1 恢复：真表装回后可挂回到 {baseline.AttachRows} 行（实得 {restoredAfterCe1.AttachRows}）");

        // ---------- 选一张「只有 1 行、trigger==0、ta!=50」的卡做后面两条反例 ----------
        string probeId = null;
        Enchantments.GrantedEnchantmentRow probeRow = null;
        var prov = Enchantments.GrantedEnchantmentsProvider;
        if (prov != null)
            for (int ci = 0; ci < pool.Count && probeId == null; ci++)
            {
                var def = pool[ci];
                if (def == null) continue;
                var data = prov(def.Id);
                if (data == null || data.Count != 1) continue;
                if (data[0].Trigger != Enchantments.TriggerThisCardPlayed) continue;
                if (data[0].TargetsAffected == Enchantments.TargetsAffectedInHand) continue;
                probeId = def.Id; probeRow = data[0];
            }
        Check(probeId != null, "找到一张「单行、trigger==0、ta!=50」的卡做反例夹具");
        if (probeId == null) { CardDatabase.InvalidateEnchantmentGrants(); CardDatabase.EnsureEnchantmentGrantsFrom(fullJson); return; }

        Console.WriteLine($"    反例夹具：{probeId} · token={probeRow.TemplateCardId} · buffType={probeRow.BuffType} · ta={probeRow.TargetsAffected}");

        // ---------- 反例 3：trigger 0 → 655 ----------
        string trigJson = CraftJson(probeId, probeRow, trigger: 655, token: probeRow.TemplateCardId);
        CardDatabase.InvalidateEnchantmentGrants();
        CardDatabase.EnsureEnchantmentGrantsFrom(trigJson);
        var trigRows = Enchantments.GrantedEnchantmentRowsOf(FindCard(pool, probeId));
        Check(trigRows != null && trigRows.Count == 1 && trigRows[0].Trigger == 655,
              "反例3：改后的行经引擎读出来 `Trigger == 655`（不再被当成 ThisCardPlayed）");
        int trigAttach = 0;
        if (trigRows != null)
            for (int i = 0; i < trigRows.Count; i++)
                if (trigRows[i] != null && trigRows[i].Trigger == Enchantments.TriggerThisCardPlayed) trigAttach++;
        Check(trigAttach == 0, "反例3：这一行已掉出「可挂」（trigger==0 计数 = 0）");

        // ---------- 反例 2：token → 不存在的 id ⇒ GrantedTemplateMissing +1 ----------
        string bogusJson = CraftJson(probeId, probeRow, trigger: Enchantments.TriggerThisCardPlayed,
                                     token: "B148_NO_SUCH_TEMPLATE_0000");
        CardDatabase.InvalidateEnchantmentGrants();
        CardDatabase.EnsureEnchantmentGrantsFrom(bogusJson);
        var ctx = NewCtx(FindCard(pool, probeId));
        int missBefore = Enchantments.GrantedTemplateMissing;
        int attached = 0;
        if (ctx != null && ctx.Players != null && ctx.Players.Length > 0 && ctx.Players[0].Warlord != null)
            attached = Enchantments.AttachGrantedByPlayedCard(ctx, new CardInstance(FindCard(pool, probeId), 90001),
                                                              ctx.Players[0].Warlord);
        Check(Enchantments.GrantedTemplateMissing == missBefore + 1,
              $"反例2：token 指向不存在的模板 ⇒ `GrantedTemplateMissing` {missBefore} → {Enchantments.GrantedTemplateMissing}"
            + "（+1 = 闸生效；⛔ 别把这条做成静默 continue）");
        Check(attached == 0, "反例2：模板查不到时**一条都不挂**（attached == 0）");

        // ---------- 恢复真表并复核 ----------
        CardDatabase.InvalidateEnchantmentGrants();
        CardDatabase.EnsureEnchantmentGrantsFrom(fullJson);
        var back = Count(pool);
        Check(back.AttachRows == baseline.AttachRows,
              $"恢复真表后回到 {baseline.AttachRows} 行（实得 {back.AttachRows}）");
    }

    static CardDef FindCard(IList<CardDef> pool, string id)
    {
        for (int i = 0; i < pool.Count; i++) if (pool[i] != null && pool[i].Id == id) return pool[i];
        return null;
    }

    /// <summary>造一份**最小**卡表（只含这一张卡的一行）—— 用来在内存里喂「改坏」的那一行给引擎的真实装载链。
    /// ⚠️ 只走 `EnsureEnchantmentGrantsFrom`（引擎自己的 mapper），⛔ 不伪造 `_enchantGrants`。</summary>
    static string CraftJson(string id, Enchantments.GrantedEnchantmentRow row, int trigger, string token)
    {
        var obj = new
        {
            version = 50,
            cards = new[]
            {
                new
                {
                    id,
                    enchantment = new[]
                    {
                        new
                        {
                            at = "buffEffect",
                            ab = row.AbilityIndex,
                            lg = row.LogicIndex,
                            buffType = row.BuffType,
                            trigger,
                            ta = row.TargetsAffected,
                            token,
                        },
                    },
                },
            },
        };
        return JsonSerializer.Serialize(obj);
    }

    /// <summary>最小夹具：只有督军 + 被测卡（不洗牌）。⚠️ 只借 `RuleCore.NewBattle` 建一个真 `BattleContext`，
    /// 用来让 `AttachGrantedByPlayedCard` 的引擎计数真的走一遍（⛔ 不在这里另算一份）。</summary>
    static BattleContext NewCtx(CardDef source)
    {
        if (source == null) return null;
        var d0 = new List<CardDef> { new CardDef("FIXWarlord", "FIXWarlord", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord"), source };
        var d1 = new List<CardDef> { new CardDef("FIXFoe", "FIXFoe", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: Program.PoolList);
    }
}
