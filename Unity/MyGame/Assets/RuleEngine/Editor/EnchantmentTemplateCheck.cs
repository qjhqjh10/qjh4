// EnchantmentTemplateCheck.cs — B89 / 执行代理 `E6b`（2026-10-28）的**独立断言入口**
//
// 为什么单开一个文件：本批要钉的四条断言横跨 `CardDef`（解析口）与 `CardDatabase`（DTO + 挂载），
//   而 `RuleEngineTest.Run`（`Editor/RuleEngineTest.cs`）**不在本代理白名单** ⇒ 另起一个可单独执行的宿主。
//
// 调用方式（任选其一）：
//     ① 单独跑：`-executeMethod EnchantmentTemplateCheck.Run`
//     ② 并进某个宿主的收尾处（想并进总数就带后一行）：
//            EnchantmentTemplateCheck.Run();
//            pass += EnchantmentTemplateCheck.Pass; fail += EnchantmentTemplateCheck.Fail;
//
// **钉的是哪四条**（每条都带 `🧨` = 什么改动能让它红；⛔ 没写 `🧨` 的断言一律当没写）：
//   §A **`CardDef.HasAbilityDamage` 对「整列未知」必须是 `false`** —— v36 的
//      `AttachAbilityDamage(rows) { _abilityDamage = rows ?? new AbilityDamageSpec[0]; }` 把
//      「未知（旧表 / 手写夹具）」折成了「明确没有行」⇒ 这个属性**恒真**、与它自己的 doc 相反。
//      `E6b` 已就地订正成 `_abilityDamage = rows;`（零行数变化），本节的 A1 就是那把锁。
//   §B **`permanentBuff` 三态**（`HasPermanentBuffs` = 列在不在 / `HasPermanentHandCostBuff` = 卡级汇总，
//      🔶 自己设计）。判据 = `CardScript__RemoveEffect.c` 第一句 / `RemoveBuffHandCostEffects.c:35`。
//   §C **`whileInPlayAnim` 三态**（判据 = `EntityScript__HasEnchantmentWhileInPlay`，账 L1044）。
//   §D **真表联调**：证明 `CardDatabase.Parse` 里那两行 `Attach*` **真的被调到**（⛔ 不是只有解析口存在）。
//      🧨 把 `CardDatabase.cs:1749` 那一行里的 `AttachWhileInPlayAnim` / `AttachPermanentBuffs` 删掉 ⇒ 本节红。
//
// ⛔ 本文件只放断言：不改产品行为、不建场景、不进任何 `Editor/*Scene.cs` 宿主。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

public static class EnchantmentTemplateCheck
{
    const string P = "[B89/E6b] ";

    /// <summary>本趟的通过 / 失败数（想并进宿主的合计数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            Debug.Log(P + "===== §A `HasAbilityDamage` 的「未知 vs 明确没有」 =====");
            SectionA_AbilityDamage();
            Debug.Log(P + "===== §B `permanentBuff` 三态 =====");
            SectionB_PermanentBuffs();
            Debug.Log(P + "===== §C `whileInPlayAnim` 三态 =====");
            SectionC_WhileInPlayAnim();
            Debug.Log(P + "===== §D 真表联调（`CardDatabase.Parse` 的两个 Attach 真被调到） =====");
            SectionD_CardTableWiring();
            Debug.Log(P + "===== §E 第二张表 `enchantment_templates.json`（附魔模板） =====");
            SectionE_EnchantmentTemplates();
            Debug.Log(P + "===== §F 注入 + 消费「活着」的证明（账 L1044 收口） =====");
            SectionF_WiringProof();
            Debug.Log(P + "===== §G 真战斗里会不会挂上去（B89/E26：SAU53 / TL47） =====");
            SectionG_RealBattleAttach();
        }
        catch (Exception e)
        {
            Fail++;
            Debug.LogError(P + "✗ 断言过程抛异常（后面的条目没跑完）：" + e);
        }
        string verdict = Fail == 0 ? "✅ 全绿" : "❌ 有红";
        Debug.Log(P + $"===== 合计 通过 {Pass} / 失败 {Fail} —— {verdict} =====");
    }

    // ==================================================================
    //  §A `HasAbilityDamage`：`null`（未知）不许折成 `[]`（明确没有）
    // ==================================================================
    static void SectionA_AbilityDamage()
    {
        var c = BareCard("E6bA");

        // A1 · **核心**：没挂过 = 未知 ⇒ 必须是 false
        Ok(!c.HasAbilityDamage,
           "★ A1 新建的 `CardDef`（没挂 `abilityDamage`）⇒ `HasAbilityDamage == false`（= 整列未知）"
         + " —— 🧨 把 `AttachAbilityDamage` 改回 `_abilityDamage = rows ?? new AbilityDamageSpec[0];` ⇒ 本条红"
         + "（那条正是 v36 的 bug：折成空数组后它**恒真**）");
        Ok(c.AbilityDamageAt(0, 0) == null, "A1（同族）未知时 `AbilityDamageAt(0,0) == null`");

        // A2 · 「明确没有行」是另一档：`[]` ⇒ Has* = true，但仍查不到行
        c.AttachAbilityDamage(new CardDef.AbilityDamageSpec[0]);
        Ok(c.HasAbilityDamage,
           "A2 挂 `[]`（= **明确没有行**）⇒ `HasAbilityDamage == true` —— 🧨 把 `AttachAbilityDamage` 改成"
         + "「空数组也置 null」⇒ 本条红（那就把「明确没有」又折回「未知」了）");
        Ok(c.AbilityDamageAt(0, 0) == null, "A2（同族）`[]` 时 `AbilityDamageAt(0,0) == null`（没有那一行）");

        // A3 · 正面：真挂一行就查得到
        c.AttachAbilityDamage(new[]
        {
            new CardDef.AbilityDamageSpec { Ab = 0, Lg = 0, Min = 1, Max = 1, ValueOptions = 90, Type = 0 },
        });
        Ok(c.AbilityDamageAt(0, 0) != null, "A3 挂一行 `(ab0,lg0)` ⇒ `AbilityDamageAt(0,0) != null`");

        // A4 · 灭自证：**真表里每一张都必须带这一列**（v37 卡表现读 1129/1129）
        //      若某张卡缺列 ⇒ A1 那条闸会把它判成「未知」，而大家会以为是「没有行」。
        var pool = CardDatabase.Load();
        int missing = 0;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && !pool[i].HasAbilityDamage) missing++;
        Ok(missing == 0,
           $"A4 真卡池 {pool.Count} 张里**没有** `abilityDamage` 列的 = {missing} 张"
         + "（期望 0：v37 卡表每张都落这一列）—— 🧨 生成器漏列 / 有人把 `MapAbilityDamage` 改成回 `null` ⇒ 本条红");
    }

    // ==================================================================
    //  §B `permanentBuff`（账 L847）
    // ==================================================================
    static void SectionB_PermanentBuffs()
    {
        var c = BareCard("E6bB");

        Ok(!c.HasPermanentBuffs && c.HasPermanentHandCostBuff == null,
           "B1 新建 `CardDef` ⇒ `HasPermanentBuffs == false` ∧ `HasPermanentHandCostBuff == null`（判不了）"
         + " —— 🧨 把 `AttachPermanentBuffs` 改成 `rows ?? new[0]`（照抄 v36 那个错）⇒ 本条红");

        c.AttachPermanentBuffs(null);
        Ok(!c.HasPermanentBuffs && c.HasPermanentHandCostBuff == null,
           "B2 `AttachPermanentBuffs(null)`（旧表 < v37）⇒ 仍是「判不了」—— 🧨 同上");

        c.AttachPermanentBuffs(new CardDef.PermanentBuffSpec[0]);
        Ok(c.HasPermanentBuffs && c.HasPermanentHandCostBuff == false,
           "B3 挂 `[]`（= 明确没有永久效果）⇒ `HasPermanentBuffs == true` ∧ 卡级汇总 `== false`");

        c.AttachPermanentBuffs(new[]
        {
            new CardDef.PermanentBuffSpec { At = "buffEffect", Ab = 0, Lg = 0, BuffType = 2 },
        });
        Ok(c.HasPermanentHandCostBuff == true,
           "B4 一条 `buffType 2 (changeCost)` 的永久行 ⇒ 卡级汇总 `== true`（= 原版「手牌费用摘不掉」那一档）");
        Ok(c.PermanentBuffAt(0, 0) != null && c.PermanentBuffAt(0, 0).At == "buffEffect",
           "B4（同族）`PermanentBuffAt(0,0).At == \"buffEffect\"`");

        c.AttachPermanentBuffs(new[]
        {
            new CardDef.PermanentBuffSpec { At = "buffEffect", Ab = 0, Lg = 0, BuffType = 1 },
        });
        Ok(c.HasPermanentHandCostBuff == false,
           "B5 永久但是 `buffType 1 (addStats)` ⇒ 卡级**费用**汇总 `== false`（⛔ 判据是 `buffType == 2`，不是「永久就算」）");
    }

    // ==================================================================
    //  §C `whileInPlayAnim`（账 L1044）
    // ==================================================================
    static void SectionC_WhileInPlayAnim()
    {
        var c = BareCard("E6bC");
        Ok(c.WhileInPlayAnim == -1 && !c.WhileInPlayAnimKnown,
           "C1 新建 `CardDef` ⇒ `WhileInPlayAnim == -1` ∧ `WhileInPlayAnimKnown == false`（⛔ 不是 0）");

        c.AttachWhileInPlayAnim(2);
        Ok(c.WhileInPlayAnim == -1, "C2 坏值 `2` ⇒ 落 `-1`（未知）—— 🧨 改成「非 0 即 1」⇒ 本条红");
        c.AttachWhileInPlayAnim(-1);
        Ok(c.WhileInPlayAnim == -1, "C2（同族）`-1` ⇒ `-1`");
        c.AttachWhileInPlayAnim(0);
        Ok(c.WhileInPlayAnim == 0 && c.WhileInPlayAnimKnown, "C2（同族）`0` ⇒ 0 且「可判」");
        c.AttachWhileInPlayAnim(1);
        Ok(c.WhileInPlayAnim == 1 && c.WhileInPlayAnimKnown, "C2（同族）`1` ⇒ 1 且「可判」");
    }

    // ==================================================================
    //  §D 真表联调：两条 `Attach*` 在 `CardDatabase.Parse` 里**真的被调到**
    // ==================================================================
    static void SectionD_CardTableWiring()
    {
        var byId = new Dictionary<string, CardDef>(StringComparer.Ordinal);
        var pool = CardDatabase.Load();
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c != null && !string.IsNullOrEmpty(c.Id)) byId[c.Id] = c;
        }
        if (!Ok(byId.ContainsKey("GSC51") && byId.ContainsKey("ASH31") && byId.ContainsKey("ASH1"),
                $"D0 前提：卡池里有 `GSC51` / `ASH31` / `ASH1`（现读 {pool.Count} 张）")) return;

        // D1 · `permanentBuffs`：`Living Icon` 那一条（经附魔模板 PPtr 解出来的）
        var li = byId["GSC51"];
        Ok(li.HasPermanentBuffs,
           "★ D1 `GSC51 Living Icon`：`HasPermanentBuffs == true`（卡表 v37 有这一列）"
         + " —— 🧨 删掉 `CardDatabase.cs:1749` 那行里的 `AttachPermanentBuffs` ⇒ 本条红");
        Ok(li.HasPermanentHandCostBuff == true,
           "★ D1（同族）它的卡级费用汇总 `== true`（`buffType 2` + `permanentBuff 1`）");
        var pb = li.PermanentBuffAt(0, 0);
        Ok(pb != null && pb.At == "enchantment" && pb.TokenCardId == "0474b16360827b6408fb02b6820e7e63",
           "★ D1（同族）`PermanentBuffAt(0,0)` = `at=enchantment` · `token=0474b163…`"
         + "（🔴 数据在**被挂上去的那张附魔模板**身上，不在 `GSC51` 自己身上 ⇒ 生成器必须解 PPtr）");

        // D2 · `whileInPlayAnim`：两张已知卡的正反两面
        var ash31 = byId["ASH31"];
        Ok(ash31.WhileInPlayAnim == 1,
           "★ D2 `ASH31 Winged Autarch`：`WhileInPlayAnim == 1`"
         + " —— 🧨 删掉那行的 `AttachWhileInPlayAnim` ⇒ 落 `-1` ⇒ 本条红");
        var ash1 = byId["ASH1"];
        Ok(ash1.WhileInPlayAnim == 0,
           "★ D2（反面）`ASH1 Anvirr Keltoc`：`WhileInPlayAnim == 0`（= 原版明确 false，⛔ 不是 -1）"
         + " —— 🧨 同 D2；把 `-1` 与 `0` 混用也红");
        Ok(ash1.HasPermanentHandCostBuff == false,
           "D2（同族）`ASH1` 的永久费用汇总 `== false`（有列、但明确没有这一族）");

        // D3 · 全池读数（只打印 + 一条「列落没落」的下限断言；⛔ 不写死 35）
        int on = 0, known = 0, pbNonEmpty = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            if (c.WhileInPlayAnim == 1) on++;
            if (c.WhileInPlayAnimKnown) known++;
            if (c.HasPermanentBuffs && c.HasPermanentHandCostBuff == true) pbNonEmpty++;
        }
        Debug.Log(P + $"D3 读数：卡池 {pool.Count} 张 · `whileInPlayAnim == 1` **{on}** 张 · 该格可判 **{known}** 张"
                     + $" · 带「永久费用效果」**{pbNonEmpty}** 张");
        Ok(known == pool.Count,
           $"D3 `whileInPlayAnim` 在真卡池里**每张都可判**（现读 {known}/{pool.Count}）"
         + " —— 🧨 生成器漏列 / 版本守卫写错（`dto.version >= 37` 写成 `> 37`）⇒ 本条红");
        Ok(on > 0 && pbNonEmpty >= 1,
           $"D3（下限）`whileInPlayAnim == 1` 有 {on} 张 · 永久费用族有 {pbNonEmpty} 张（两者都必须 > 0）");
    }

    // ==================================================================
    //  §E 第二张表：`enchantment_templates.json`（附魔模板 · B89/E6c）
    // ==================================================================
    static void SectionE_EnchantmentTemplates()
    {
        var asset = Resources.Load<TextAsset>("enchantment_templates");
        if (!Ok(asset != null,
                "E0 前提：`Resources/enchantment_templates.json` 在工程里"
              + "（重跑 `python -I -X utf8 工具/gen_cards_engine.py --write`）")) return;

        var file = JsonUtility.FromJson<CardDatabase.EnchantmentTemplateFileDto>(asset.text);
        Ok(file != null && file.templates != null && file.templates.Length > 0,
           $"E1 表能解析且非空（现读 `version` = {(file == null ? -999 : file.version)} · "
         + $"`templates` = {(file == null || file.templates == null ? -1 : file.templates.Length)} 行）");
        if (file == null || file.templates == null) return;

        // E2 · `count` 三处一致：文件自报 / 数组长度 / 加载器返回
        int n = file.templates.Length;
        Ok(file.count == n,
           $"E2 文件自报 `count` = {file.count} 与 `templates.Length` = {n} 一致"
         + " —— 🧨 生成器把某张筛掉了却忘了改 count ⇒ 本条红");
        var loaded = CardDatabase.LoadEnchantmentTemplates();
        Ok(loaded.Count == n,
           $"E2（同族）`CardDatabase.LoadEnchantmentTemplates()` 返回 {loaded.Count} 张 = 表里 {n} 行"
         + "（⛔ 少 = 有行被丢弃：缺 `id` / `id` 重复 / 解析失败，日志里会有出声）"
         + " —— 🧨 加载器里加过滤 / DTO 字段改名 ⇒ 本条红");

        // E3 · 每张都有 id、且唯一
        int noId = 0;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        int dup = 0;
        foreach (var t in file.templates)
        {
            if (t == null || string.IsNullOrEmpty(t.id)) { noId++; continue; }
            if (!ids.Add(t.id)) dup++;
        }
        Ok(noId == 0, $"E3 每张模板都有 `id`（缺 {noId} 张）—— 🧨 生成器漏 `uniqueId` ⇒ 红");
        Ok(dup == 0, $"E3（同族）`id` 唯一（重复 {dup} 张）");

        // E4 · `whileInPlayAnim` 分布：值只允许 0/1（`-1` = 坏值，生成器会记 `rows_bad`），
        //      且**为 1 的正好 3 张**（真值：`Undying Legions` / `Hive Fleet Leviathan` / `Everstorm`）
        int ones = 0, zeros = 0, weird = 0;
        foreach (var t in file.templates)
        {
            if (t == null) continue;
            if (t.whileInPlayAnim == 1) ones++;
            else if (t.whileInPlayAnim == 0) zeros++;
            else weird++;
        }
        Ok(weird == 0,
           $"E4 `whileInPlayAnim` 只有 0/1 两档（越界 {weird} 张）—— 🧨 生成器把坏值当 0 落 ⇒ 红");
        Ok(ones == 3 && zeros == n - 3,
           $"E4（同族）全量分布 = `1` **{ones}** / `0` **{zeros}**（期望 3 / {n - 3}）"
         + " —— 🧨 数据列没落 / 生成器漏行 / 判据从 `m_Name` 尾 `_enchantment` 改成别的 ⇒ 红");

        // E5 · 那 3 张**名字**必须在（防「分布对了但换成另外 3 张」）
        var byName = new Dictionary<string, CardDatabase.EnchantmentTemplateDto>(StringComparer.Ordinal);
        foreach (var t in file.templates)
            if (t != null && !string.IsNullOrEmpty(t.name) && !byName.ContainsKey(t.name)) byName[t.name] = t;
        int hit = 0;
        foreach (string want in new[] { "Undying Legions", "Hive Fleet Leviathan", "Everstorm" })
        {
            CardDatabase.EnchantmentTemplateDto d;
            if (byName.TryGetValue(want, out d) && d.whileInPlayAnim == 1) hit++;
        }
        Ok(hit == 3,
           $"E5 真值为 1 的那 3 张（`Undying Legions` / `Hive Fleet Leviathan` / `Everstorm`）都在表里且 = 1"
         + $"（命中 {hit}/3）—— 🧨 生成器按 `cardType == 20` 或名字含 `enchant` 筛（那是**入池闸**，不是本表判据）⇒ 红");

        // E6 · **两张表互不串** —— 🔴 2026-10-28（B89_RD · 调度台裁定）**改断不变量**：
        //   ⛔ 不再断「模板 id 一个都不许出现在可玩池里」：那 6 张（`Dark Pact of *` 4 张，`cost = -1` 哨兵；
        //   `Pray Enchantment`（v30 就按决策加进池的隐藏模板）；`Reconnaissance Mission`）在**模板表出现之前**
        //   就已经在池里（HEAD 版 `cards_engine.json` 现读同样 6 个 id 全在）⇒ 那是**池的历史内容**，不是本表的副作用。
        //   真正该钉的不变量 = 「模板表**没有往池里塞新东西**」⇒ 允许既有那 6 条重叠，其余一条都不许多。
        var pool = CardDatabase.Load();
        var poolIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < pool.Count; i++) if (pool[i] != null && pool[i].Id != null) poolIds.Add(pool[i].Id);
        var knownOverlap = new HashSet<string>(StringComparer.Ordinal)
        {
            "1dfc9bc738bfc4f4d93d48f165aee510",   // Mark of Nurgle (enchantment)      ↔ 池内 `Dark Pact of Resilience`
            "4a48457c24877f345be2e96f867c1e94",   // Mark of Tzeentch (enchantment)    ↔ 池内 `Dark Pact of Fate`
            "9a2cadc64c80a6b42912441105bb1cee",   // Mark of Khorne (enchantment)      ↔ 池内 `Dark Pact of Blood`
            "b8e81b2fe85cda747a68c4d6326b669c",   // Mark of Slaanesh (enchantment)    ↔ 池内 `Dark Pact of Excess`
            "9218576fd3e26554db5b4c239a36dbc7",   // Pray Enchantment                  （v30 决策：池内隐藏附魔模板）
            "e8cf1eb55ca41534abe38efc90b302e9",   // Reconnaissance Mission            （可玩的附魔卡）
        };
        int leaked = 0, unexpected = 0;
        foreach (string id in ids) if (poolIds.Contains(id)) { leaked++; if (!knownOverlap.Contains(id)) unexpected++; }
        Ok(unexpected == 0,
           $"E6 模板表**没有往可玩池里塞新东西**（超出既有重叠的有 **{unexpected}** 张；池现读 {pool.Count} 张 · 模板 {n} 张 · 与池既有重叠 {leaked} 张）"
         + " —— 🧨 把模板塞回 `cards_engine.json` 的 `cards[]`（会改 `count`）⇒ 本条红");
        Ok(leaked == knownOverlap.Count,
           $"E6（同族）既有重叠**正好** {knownOverlap.Count} 张（现读 {leaked}）—— 🧨 池那边删卡 / 生成器换了收录口径 ⇒ 红"
         + "｜⚠️ 这条与上一条**成对**：上一条挡「多出来」，这一条挡「少掉」（只留一条会放过半边）");

        // E7 · `permanentBuffs` 在模板这一层也真接上了：`Living Icon` 那张模板（`0474b163…`）带 changeCost 永久
        CardDef li = null;
        for (int i = 0; i < loaded.Count; i++)
            if (loaded[i] != null && loaded[i].Id == "0474b16360827b6408fb02b6820e7e63") li = loaded[i];
        Ok(li != null && li.HasPermanentBuffs && li.HasPermanentHandCostBuff == true,
           "E7 模板 `0474b163…`（`Living Icon_enchantment`）⇒ `HasPermanentBuffs == true` ∧ "
         + "`HasPermanentHandCostBuff == true`（`buffType 2` + `permanentBuff 1`）"
         + " —— 🧨 加载器漏 `AttachPermanentBuffs` ⇒ 本条红");

        // E8 · 辅助读数自洽（`CountWhileInPlayAnim` 与 E4 的 ones 是同一个量）
        Ok(CardDatabase.CountWhileInPlayAnim(loaded) == ones,
           $"E8 `CardDatabase.CountWhileInPlayAnim(loaded)` = {CardDatabase.CountWhileInPlayAnim(loaded)}"
         + $" = 表里的 {ones}");

        Debug.Log(P + $"E·读数：表 v{file.version} · {n} 张 · `whileInPlayAnim==1` {ones} 张 · "
                     + $"加载器返回 {loaded.Count} 张 · 可玩池 {pool.Count} 张");
    }

    // ==================================================================
    //  §F 注入 + 消费「活着」的证明（账 L1044）
    //
    //  🔴 这一节是**行为级**的：⛔ 不是「表里有这个值」，而是「一张附魔挂上去之后，
    //     `HasEnchantmentWhileInPlay` 真的能读到 `true`」。链路四跳，每跳一条断言：
    //       ① `CardDatabase` 静态构造 → `BattleContext.EnchantmentTemplatesProvider`（注入）
    //       ② `ctx.FindEnchantmentTemplate(id)` → 第二张表里那一张（查找，**卡池里没有它**）
    //       ③ `Enchantments.Attach(ctx=null, carrier, 模板)` 的缺省 → 读 `CardDef.WhileInPlayAnim`（消费）
    //       ④ `Enchantments.HasEnchantmentWhileInPlay(carrier)` → `true`（可观察结果）
    // ==================================================================
    static void SectionF_WiringProof()
    {
        const string HiveId = "a5960475756bec144a1d6d8a9d141fc9";     // Hive Fleet Leviathan（wp=1）
        const string UndyingId = "eab54e4c131034e4dacf8ce72807ed07";  // Undying Legions（wp=1）

        // ---- ① 注入：provider 已由 `CardDatabase` 的静态构造登记 ----
        Ok(BattleContext.EnchantmentTemplatesProvider != null,
           "★ F1 `BattleContext.EnchantmentTemplatesProvider` 已注册（= `CardDatabase` 的静态构造跑过）"
         + " —— 🧨 删掉 `CardDatabase` 的静态构造 / 把登记那句删掉 ⇒ 本条红（后面 ②③④ 全部连带）");

        // ---- ② 查找：卡池里**没有**这张模板，只能从第二张表来 ----
        var ctx = new BattleContext(20261028);
        ctx.CardPool = new List<CardDef>();          // 空的可玩池（模板本来就不在池里）
        var hive = ctx.FindEnchantmentTemplate(HiveId);
        Ok(hive != null && hive.Id == HiveId,
           "★ F2 空卡池的 `BattleContext.FindEnchantmentTemplate(Hive Fleet Leviathan)` 命中了第二张表"
         + " —— 🧨 `EnsureEnchantmentTemplates()` 没被调 / provider 为 null / 表读不到 ⇒ 本条红");
        var undying = ctx.FindEnchantmentTemplate(UndyingId);
        Ok(undying != null && undying.Id == UndyingId, "F2（同族）`Undying Legions` 同样命中");
        if (hive == null || undying == null) return;

        // 前提：这两张确实是 `1`（数据侧，防止「查到了但值是 0」把后面的结论说反）
        Ok(hive.WhileInPlayAnim == 1 && undying.WhileInPlayAnim == 1,
           $"F3 前提：两张模板的 `WhileInPlayAnim` 都是 1（实得 {hive.WhileInPlayAnim} / {undying.WhileInPlayAnim}）");

        // ---- ③④ 消费：挂上去 → 记录读到了 1 → 可观察结果为真 ----
        var carrierA = CardInstance.Detached(BareCard("E6dCarrierA"));   // 分离实例（`Id < 0`）= 没有对局的场合，见 `CardInstance.Detached` 的 doc
        Enchantments.Attach(null, carrierA, hive);       // `ctx = null` ⇒ 不碰棋盘/日志，只走记录那一段
        int got = carrierA.ActiveEnchantments.Count > 0 ? carrierA.ActiveEnchantments[0].WhileInPlayAnim : -999;
        Ok(got == 1,
           $"★ F4 `Hive Fleet Leviathan` 挂上去之后，记录上的 `WhileInPlayAnim` = {got}（期望 1）"
         + " —— 🧨 把 `Attach` 里 `EnchantmentWhileInPlayAnimOf(...)` 改回直传 `whileInPlayAnim` ⇒ 落 -1 ⇒ 本条红");
        Ok(Enchantments.HasEnchantmentWhileInPlay(carrierA),
           "★ F4（同族）`Enchantments.HasEnchantmentWhileInPlay(carrier)` == **true**"
         + " —— 🧨 同上；或 `HasEnchantmentWhileInPlay` 改回看 `HasAny` ⇒ 红");
        Ok(Enchantments.CountUnknownWhileInPlayAnim(carrierA) == 0,
           "F4（同族）`CountUnknownWhileInPlayAnim(carrier)` == 0（= **不是靠「未知」蒙对的**）");

        var carrierB = CardInstance.Detached(BareCard("E6dCarrierB"));
        Enchantments.Attach(null, carrierB, undying);
        Ok(Enchantments.HasEnchantmentWhileInPlay(carrierB) && Enchantments.CountUnknownWhileInPlayAnim(carrierB) == 0,
           "★ F5 `Undying Legions` 同样读到（`HasEnchantmentWhileInPlay` 真 · 未知件数 0）");

        // ---- ⑥ 反面：`whileInPlayAnim == 0` 的模板必须判「假」而不是「未知」 ----
        var zero = ctx.FindEnchantmentTemplate("0078441d07f6d604db6c6cfb1c1fa46f");   // Ferocious Rage（wp=0）
        Ok(zero != null && zero.WhileInPlayAnim == 0,
           $"F6 前提：`Ferocious Rage` 在表里且 `WhileInPlayAnim == 0`（实得 {(zero == null ? -999 : zero.WhileInPlayAnim)}）");
        if (zero != null)
        {
            var carrierC = CardInstance.Detached(BareCard("E6dCarrierC"));
            Enchantments.Attach(null, carrierC, zero);
            Ok(!Enchantments.HasEnchantmentWhileInPlay(carrierC)
               && Enchantments.CountUnknownWhileInPlayAnim(carrierC) == 0,
               "F6（反面）`wp == 0` 的模板 ⇒ 判**假**且「未知件数 0」（⛔ 别把 0 当未知）");
        }

        // ---- ⑦ 注入前后差异（这就是「活着」的判别式）：把 provider 摘掉 ⇒ 查找落空 ----
        var saved = BattleContext.EnchantmentTemplatesProvider;
        try
        {
            BattleContext.EnchantmentTemplatesProvider = null;
            var ctx2 = new BattleContext(20261029);
            ctx2.CardPool = new List<CardDef>();
            Ok(ctx2.FindEnchantmentTemplate(HiveId) == null && ctx2.EnchantmentTemplates == null,
               "★ F7（**灭自证**：注入前后必须不同）provider 摘掉之后，同一个 id **查不到**、`EnchantmentTemplates` 仍是 null"
             + " —— 🧨 如果本条红（= 摘掉还能查到），说明命中的根本不是第二张表");
        }
        finally
        {
            BattleContext.EnchantmentTemplatesProvider = saved;   // ⛔ 一定要还回去（后面还有别的断言在用）
        }
        var ctx3 = new BattleContext(20261030);
        ctx3.CardPool = new List<CardDef>();
        Ok(ctx3.FindEnchantmentTemplate(HiveId) != null,
           "★ F7（同族）恢复 provider 之后又查得到 —— 证明上一条的红/绿确实由注入状态决定");

        Debug.Log(P + $"F·读数：`Hive Fleet Leviathan` / `Undying Legions` 挂载后 `WhileInPlayAnim` = "
                     + $"{got} / {(carrierB.ActiveEnchantments.Count > 0 ? carrierB.ActiveEnchantments[0].WhileInPlayAnim : -999)}"
                     + " · 两张的 `HasEnchantmentWhileInPlay` 都是 true");
    }

    // ==================================================================
    //  §G 「真战斗里会不会挂上去」—— B89 第 1 波 · 执行代理 `E26`
    //
    //  🔴 这一节回答的是**行为级**问题，不是「表里有没有这个值」：
    //    `SAU53 Undying Legions` / `TL47 Hive Fleet Leviathan` 这两张可玩卡在原版里
    //    会通过 `buffType 12 buffEffect.enchantment` 把一张**附魔模板**挂到目标身上
    //    （`CardScript__AddEffect.c:826-828` → `CardScript__AddEnchantment.c`），
    //    而模板自己的 `whileInPlayAnim = 1` 正是 `EntityScript.HasEnchantmentWhileInPlay` 唯一读的那一格。
    //  本节的落点 = <see cref="Enchantments.AttachGrantedByPlayedCard"/>（白名单 + 挂载端）
    //  与 `EffectResolver.PlayTacticCore` 的那一句调用。
    //
    //  🧨 每条都写了「什么改动能让它红」。
    // ==================================================================
    static void SectionG_RealBattleAttach()
    {
        const string UndyingId = "eab54e4c131034e4dacf8ce72807ed07";   // T16 Sautekh-Undying Legions_enchantment
        const string HiveId = "a5960475756bec144a1d6d8a9d141fc9";       // T18 Leviathan-Hive Fleet Leviathan_enchantment

        var pool = CardDatabase.Load();
        CardDef sau53 = FindById(pool, "SAU53");
        CardDef tl47 = FindById(pool, "TL47");
        if (!Ok(sau53 != null && tl47 != null,
                $"G0 前提：真实可玩池里有 `SAU53` / `TL47`（现读 {pool.Count} 张）—— "
              + "缺少时 G2/G4 无从谈起（⛔ 别让它们变成「0 == 0」的假绿）")) return;

        // ---- G1 · 白名单 ↔ 第二张表对得上（防 id 打错 ⇒ 静默不挂）----
        var ctx0 = new BattleContext(20261101);
        ctx0.CardPool = pool;
        var undying = ctx0.FindEnchantmentTemplate(UndyingId);
        var hive = ctx0.FindEnchantmentTemplate(HiveId);
        Ok(undying != null && undying.WhileInPlayAnim == 1,
           "★ G1 白名单里 `SAU53 → eab54e4c…` 在第二张表里查得到，且 `whileInPlayAnim == 1`"
         + " —— 🧨 把 `Enchantments.PlayerGrantedEnchantments` 里那串 id 打错一个字符 ⇒ 本条红"
         + "（那正是「硬编码 id」唯一能出事的地方）");
        Ok(hive != null && hive.WhileInPlayAnim == 1,
           "★ G1（同族）`TL47 → a5960475…` 同样查得到且 `== 1`");
        Ok(Enchantments.GrantedEnchantmentOf(sau53) != null
           && Enchantments.GrantedEnchantmentOf(tl47) != null,
           "G1（同族）按**卡对象**（不是 id 字符串）也命中登记表");

        // ---- G2 · 真战斗：打出 `SAU53` ⇒ 目标身上真的挂上 Undying Legions ----
        {
            BattleContext bctx; UnitState carrier;
            int code = PlayTacticOnFixture(sau53, 20261102, out bctx, out carrier);
            Ok(code == RuleCodes.OK,
               $"G2 前提：`SAU53` 在夹具战斗里打得出去（返回 {code}）—— 🧨 若这一条红，"
             + "下面几条全部无效（别把「没打出去」当成「没挂上」）");
            if (code == RuleCodes.OK && carrier != null)
            {
                Ok(carrier.Instance.ActiveEnchantments.Count == 1,
                   $"★ G2 打出 `SAU53` 之后，目标身上 `activeEnchantments(+0x110)` 有 "
                 + $"**{carrier.Instance.ActiveEnchantments.Count}** 条（期望 1）"
                 + " —— 🧨 删掉 `EffectResolver.PlayTacticCore` 里 `Enchantments.AttachGrantedByPlayedCard(...)` 那句 ⇒ 本条红（变 0）");
                if (carrier.Instance.ActiveEnchantments.Count == 1)
                {
                    var rec = carrier.Instance.ActiveEnchantments[0];
                    Ok(rec.Card != null && rec.Card.Id == UndyingId,
                       $"★ G2（同族）挂的是**那张模板**（实得 `{(rec.Card == null ? "null" : rec.Card.Id)}`）");
                    Ok(rec.WhileInPlayAnim == 1,
                       $"★ G2（同族）记录上的 `WhileInPlayAnim` = {rec.WhileInPlayAnim}（期望 1）"
                     + " —— 🧨 把 `Attach` 里 `EnchantmentWhileInPlayAnimOf(...)` 改回直传缺省 ⇒ 落 -1 ⇒ 红");
                }
                Ok(Enchantments.HasEnchantmentWhileInPlay(carrier.Instance),
                   "★★ G2 **可观察结果**：`Enchantments.HasEnchantmentWhileInPlay(carrier)` == true"
                 + "（= 原版 `EntityScript__HasEnchantmentWhileInPlay` 为真 ⇒ 表现层那一下应该播；"
                 + "`whileInPlayAnim` 此前**全仓没有任何真战斗路径会写它**）");
                Ok(Enchantments.CountUnknownWhileInPlayAnim(carrier.Instance) == 0,
                   "★ G2（同族）`CountUnknownWhileInPlayAnim(carrier)` == 0"
                 + "（= **不是靠「未知」蒙对的**；P-07 之前那版这里会是 1）");
            }
        }

        // ---- G3 · 灭自证：把第二张表摘掉 ⇒ 同一条链**什么都不挂** ----
        {
            var saved = BattleContext.EnchantmentTemplatesProvider;
            try
            {
                BattleContext.EnchantmentTemplatesProvider = null;
                BattleContext bctx; UnitState carrier;
                int code = PlayTacticOnFixture(sau53, 20261103, out bctx, out carrier);
                Ok(code == RuleCodes.OK && carrier != null,
                   $"★ G3 前提：摘掉 provider 之后 `SAU53` 照样打得出去（返回 {code}）");
                if (code == RuleCodes.OK && carrier != null)
                    Ok(carrier.Instance.ActiveEnchantments.Count == 0,
                       $"★ G3（**灭自证**）摘掉 provider ⇒ 目标身上挂了 "
                     + $"**{carrier.Instance.ActiveEnchantments.Count}** 条（期望 0）"
                     + " —— 🧨 如果本条红（= 摘掉还挂上了），说明命中的根本不是第二张表");
            }
            finally { BattleContext.EnchantmentTemplatesProvider = saved; }
        }

        // ---- G4 · 真战斗：`TL47 Hive Fleet Leviathan`（模板自己 `cardAbilities` 为空，只有 wp=1）----
        {
            BattleContext bctx; UnitState carrier;
            int code = PlayTacticOnFixture(tl47, 20261104, out bctx, out carrier);
            Ok(code == RuleCodes.OK, $"G4 前提：`TL47` 打得出去（返回 {code}）");
            if (code == RuleCodes.OK && carrier != null)
            {
                Ok(carrier.Instance.ActiveEnchantments.Count == 1
                   && carrier.Instance.ActiveEnchantments[0].Card != null
                   && carrier.Instance.ActiveEnchantments[0].Card.Id == HiveId,
                   "★ G4 `TL47` 打出 ⇒ 目标身上挂着 `Hive Fleet Leviathan` 模板（1 条）"
                 + " —— 🧨 白名单里删掉 `TL47` 那一行 ⇒ 红");
                Ok(Enchantments.HasEnchantmentWhileInPlay(carrier.Instance),
                   "★ G4（同族）`HasEnchantmentWhileInPlay` == true（这张模板**唯一**的语义就是它）");
            }
        }

        // ---- G5 · 反面：没有目标就没得挂（⛔ 别乱挂到别人身上）----
        {
            BattleContext bctx; UnitState carrier;
            int code = PlayTacticOnFixture(sau53, 20261105, out bctx, out carrier, placeCarrier: false);
            if (Ok(code == RuleCodes.OK, $"G5 前提：无目标也打得出去（返回 {code}）") && bctx != null)
            {
                int n = 0;
                for (int p = 0; p < 2; p++)
                    for (int s = 0; s < BoardSpec.Size; s++)
                    {
                        var u = bctx.Players[p].Board[s];
                        if (u != null && u.Instance != null) n += u.Instance.ActiveEnchantments.Count;
                    }
                Ok(n == 0,
                   $"★ G5 场上没人可挂时，**任何**单位身上都不会冒出附魔（实得 {n} 条）"
                 + " —— 🧨 把挂载端改成「找不到目标就挂给自己/督军」⇒ 本条红");
            }
        }

        Debug.Log(P + "G·读数：`SAU53`/`TL47` 两条链已在**真战斗**（`RuleCore.NewBattle` + `RuleCore.PlayCard`）里走通；"
                     + "全池 114 张挂载者里**只有这 2 张（+ 休眠的 SW64）**接了 —— 其余 111 张仍是缺口");
    }

    /// <summary>G 节夹具：**真卡池** + 一张督军 + 一张要打出的战术卡（+ 可选：4 号格摆一个己方单位当目标）。
    /// <para>⚠️ 这是**本文件自己的一份**最小夹具（`RuleEngineTest.cs` 的 `ProbeBattle` / `Place` 是 private，
    /// 跨类拿不到；而且那个文件不在本代理的所有权内）。</para></summary>
    static int PlayTacticOnFixture(CardDef tactic, int seed, out BattleContext ctx, out UnitState carrier,
                                   bool placeCarrier = true)
    {
        carrier = null;
        ctx = null;
        var pool = CardDatabase.Load();
        var d0 = new List<CardDef> { Hero("FixtureWarlord0", "Ultramarines"), tactic };
        var d1 = new List<CardDef> { Hero("FixtureWarlord1", "Goff") };
        ctx = RuleCore.NewBattle(d0, d1, seed: seed, shuffle: false, cardPool: pool);
        if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
        ctx.Players[0].Energy = 30;                       // 夹具：不测费用，只测「打出去之后挂没挂」
        if (placeCarrier) carrier = Place(ctx, 0, 4, BareCard("E26Carrier"));

        int idx = -1;
        var hand = ctx.Players[0].Hand;
        for (int i = 0; i < hand.Count; i++)
            if (hand[i] != null && hand[i].Card != null && hand[i].Card.Id == tactic.Id) { idx = i; break; }
        if (idx < 0) return -999;                          // 手牌里没抽到（G2 的前提断言会把它报出来）
        return RuleCore.PlayCard(ctx, 0, idx, placeCarrier ? 4 : 0, carrier);
    }

    static CardDef Hero(string name, string faction)
    {
        return new CardDef(name, name, "hero", "", null, faction, 0, 2, 30, 0, null, subtype: "Warlord");
    }

    /// <summary>把一个单位直接摆到场上（跳过部署流程）。口径与 `RuleEngineTest.Place` 一致：
    /// 走**对局计数器**发实例 + 写 `DeployedTurn` / `PlayOrder`。</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card)
    {
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static CardDef FindById(List<CardDef> pool, string id)
    {
        if (pool == null) return null;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && pool[i].Id == id) return pool[i];
        return null;
    }

    // ==================================================================
    //  夹具 / 断言小工具
    // ==================================================================
    static CardDef BareCard(string id)
    {
        return new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 1, 0, null, subtype: "Infantry");
    }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "✓ " + msg); }
        else { Fail++; Debug.LogError(P + "✗ " + msg); }
        return cond;
    }
}
