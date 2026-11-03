// B123_C1_Assert.cs — 执行代理 `B123_C1`（2026-11-03 · 批次 `B123` · 文件所有权 = `RuleEngine/Core/EffectText.cs`）
//
// ============================ 它盯的是什么 ============================
//  账 `B121-E03F-09`（判据 → `资料/完全复刻/判据/B123_对账_C_表现外壳.md` 逐条第 1 条 + §顺手发现 1）：
//  **13 张 `TRANSFORM_*` 变身战术卡**（🔶 自设计，原版不存在；判据 = `RuleCore.HasWarlordTransformAbility`）
//  的卡面正文是**说明文字**（`Transform your Warlord into another Warlord of the same faction, chosen at random.`），
//  主解析器对它 **0 op**（`ParseSegment` 落到 `:4275-4276` 兜底 `SegKind.Unknown; Ops=null`）——
//  于是在两条**口径不同**的账上各自出错：
//    · `EffectText.WillRunOps`（执行层）给**空表** ⇒ 报表把它记成「本来没正文」；
//    · `EffectText.Coverage`（卡面文本解析覆盖率）**无条件 `Cards++`** ⇒
//      分母 446→459、`SegUnknown` 0→13 ⇒ `RuleEngineTest` 的两条哨兵（`:3729` / `:3819`）一起红。
//  本批修法（⛔ 另一条路「把期望值改大」被否：那会让「原版卡面覆盖率」这个指标失去意义）：
//    ① `WillRunOps` 的 tactic/defence 支改走**唯一取值口** `RuleCore.TacticOpsOf`（变身卡 = 一条 `warlordtransform` op）；
//    ② `Coverage` 把**整张卡**从分母里排除（不是像 `IsHandTrap` 那样「计入分母、记全过」——
//       它们不是「原版卡的另一种解析形状」，而是**原版根本不存在的卡**）。
//
// ============================ 判据（⛔ 别在这儿抄第二份）============================
//   · `资料/完全复刻/判据/B123_对账_C_表现外壳.md` 逐条第 1 条 + §顺手发现 1 / 2 / 8 / 9
//   · 卡数据真值：`MyGame/Assets/RuleEngine/Resources/cards_engine.json`（现读 `tactic` **459** = 446 原版 + 13 `TRANSFORM_*`）
//   · 效果码 / 三件东西的分工：`RuleEngine/Core/RuleCorePartial_B96CB.cs` 文件末那一段
//
// ============================ 怎么接（⚠️ 宿主接线归调度台）============================
//  宿主 = `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()`，**在同一物理行**追加这两句
//  （⚠️ 本批对 `RuleEngineTest.cs` 的执行纪律是**零行数变化** —— 它 39,763 行、账按行号引用它）：
//      B123_C1_Assert.Run(); _pass += B123_C1_Assert.Pass; _fail += B123_C1_Assert.Fail;
//  建议挂在 `Step(TestTacticTextCoverage);`（`:107`）那一行行末 —— 两者量的是同一件事。
//
// ============================ 🔴 灭自证（每一条怎么才能红）============================
//   · **A 组**：把 `RuleCore.WarlordTransformAbilityCode` 挪出枚举域（或改卡表 `chosenAbility`）⇒ A 红（13 张不再被认出）。
//   · **B 组**：把 `EffectText.Coverage` 里那句 `|| RuleCore.HasWarlordTransformAbility(c)` 摘掉
//     ⇒ 分母 446→459、`SegUnknown` 0→13 ⇒ B/C **同时红**。
//   · **C 组的「摘掉豁免」反证 = D 组**：用**同一条 desc、但没有 `chosenAbility` 标记**的合成卡
//     走同一条 `Coverage` ⇒ `SegUnknown` **恰好 13**。这条**不依赖被测实现**（合成卡是数据侧造的、
//     `HasWarlordTransformAbility` 对它恒 false）⇒ 它证明「C 的 0 是豁免给的，不是解析器认得这几句」。
//   · **E 组的对照半条**：同样的合成卡 `WillRunOps` = **0 条** ⇒ 证明「收到 op」的是变身豁免，
//     ⛔ 不是 `Parse` 自己会认这句说明文（与铁律 18·0 同一条口径：原版解析器不认，我们也不许让它「认」）。
//
// ============================ 覆盖边界（如实记）============================
//   · 只验**引擎侧**（`Core/EffectText.cs` 两处 + 数据面）。卡面 `*`（`BattleDriver.UnimplementedNotes`）、
//     拖拽可打性（`RuleCore.CanPlayTactic`）、自动组牌（`DeckBuilder.TacticPlayable`）都不在本件里
//     —— 前两处已由 `B121_E03F_DATA_Assert` 覆盖。
//   · `工具/ruleprobe/WillRun.cs` 是 `WillRunOps` 的**移植副本**（不读本文件）⇒ 本批**没有**同步它
//     （派活红线：`工具/ruleprobe/**` 归 `E3`）⇒ **探针 `willrun` 与自检报表会差这 13 张**，这是已知且预期的漂移。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B123_C1`：13 张自设计变身战术卡 —— 解析层 / 覆盖率账的豁免（账 `B121-E03F-09`）。</summary>
public static class B123_C1_Assert
{
    const string P = "[B123C1] ";

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
            var pool = CardDatabase.Load();
            if (pool == null || pool.Count == 0)
            {
                Ok(false, "前提：卡池读得到（`CardDatabase.Load()` 返 0 张 ⇒ 本组全部无法验，报出来而不是静默跳过）");
                return;
            }

            var transforms = TransformCardsOf(pool);
            var nativeTactics = NativeTacticCount(pool);

            SectionA_Data(transforms, pool);
            SectionB_NotInCoverage(pool, transforms, nativeTactics);
            SectionC_SegUnknownUnaffected(pool, transforms);
            SectionD_ControlWithoutExemption(transforms, pool);
            SectionE_WillRunOps(transforms, pool);
        }
        catch (Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== B123C1 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ============================================================ 夹具 / 取数

    /// <summary>池内所有「变身战术卡」（判据 = `RuleCore.HasWarlordTransformAbility` —— **唯一判据**，⛔ 不按卡名/Id 认）。</summary>
    static List<CardDef> TransformCardsOf(List<CardDef> pool)
    {
        var list = new List<CardDef>();
        foreach (var c in pool)
            if (c != null && RuleCore.HasWarlordTransformAbility(c)) list.Add(c);
        return list;
    }

    /// <summary>池内「原版`tactic`」张数 = `tactic` 总数 − 变身卡（这张数才是覆盖率的分母）。</summary>
    static int NativeTacticCount(List<CardDef> pool)
    {
        int n = 0;
        foreach (var c in pool)
            if (c != null && c.Type == "tactic" && !RuleCore.HasWarlordTransformAbility(c)) n++;
        return n;
    }

    /// <summary>
    /// **摘掉豁免的对照组**：同一条 `desc`、同一个 `Type`，但**没有** `chosenAbility` 标记的合成卡
    /// （= 原版里「卡面写着一句说明文字」的普通战术卡）。
    /// ⚠️ 必须是**新建**的 `CardDef`，⛔ 不许去改池里真卡再还原
    /// （`RuleEngineTest.cs:38011` 那一处就地改真卡再手工还原是已知的反例：早退会污染后续用例）。
    /// </summary>
    static CardDef SyntheticPlainTactic(CardDef src)
    {
        string faction = string.IsNullOrEmpty(src.Faction) ? "AstraMilitarum" : src.Faction;
        return new CardDef("SYN_" + src.Id, "SYN_" + src.Name, "tactic", src.Desc,
                           "Common", faction, 1, 0, 0, 0);
    }

    // ============================================================ A · 数据面
    static void SectionA_Data(List<CardDef> transforms, List<CardDef> pool)
    {
        Debug.Log(P + "---- A 数据面：池内恰好 13 张、且都是 `tactic` ----");
        Eq(transforms.Count, 13, "池内 `HasWarlordTransformAbility` = true 的卡 **13 张**（豁免面不宽不窄）");

        foreach (var c in transforms)
        {
            Ok(c.Type == "tactic", $"「{c.Id}」`Type` = tactic（实际 {c.Type}）");
            Ok(RuleCore.HasWarlordTransformAbility(c), $"「{c.Id}」`chosenAbility` 含 {RuleCore.WarlordTransformAbilityCode}");
        }

        // 反例（防「判据恒真」）：至少一张普通战术卡**不**被认成变身卡
        CardDef plain = null;
        foreach (var c in pool)
            if (c != null && c.Type == "tactic" && !RuleCore.HasWarlordTransformAbility(c)) { plain = c; break; }
        Ok(plain != null, "前提：池里有普通战术卡（对照用）");
        if (plain != null)
            Ok(!RuleCore.HasWarlordTransformAbility(plain),
               $"★ 反例：普通战术卡「{plain.Id}」**不**被认成变身卡（判据不是恒真）");
    }

    // ============================================================ B · 不计入 Coverage
    static void SectionB_NotInCoverage(List<CardDef> pool, List<CardDef> transforms, int nativeTactics)
    {
        Debug.Log(P + "---- B 覆盖率分母：13 张自设计卡**不计入** ----");
        var covAll = EffectText.Coverage(pool, "tactic", pool);

        Eq(nativeTactics, 446, "池内「原版 `tactic`」= 446（459 总数 − 13 变身卡；口径同 `RuleEngineTest.cs:3729`）");
        Eq(covAll.Cards, nativeTactics,
           "★ `Coverage.Cards` = 原版战术卡张数（分母里**没有**那 13 张）");

        foreach (var c in transforms)
        {
            var one = EffectText.Coverage(new[] { c }, "tactic", pool);
            Eq(one.Cards, 0, $"「{c.Id}」单卡喂进 `Coverage` ⇒ `Cards` = 0（整张被排除）");
            Eq(one.SegTotal, 0, $"「{c.Id}」`SegTotal` = 0（它一句都不进分句账）");
            Eq(one.Full + one.Partial + one.None, 0, $"「{c.Id}」卡级桶（Full/Partial/None）全 0（⛔ 不许偷偷记「全过」）");
        }
    }

    // ============================================================ C · SegUnknown 不因它们增加
    static void SectionC_SegUnknownUnaffected(List<CardDef> pool, List<CardDef> transforms)
    {
        Debug.Log(P + "---- C 回归哨兵：`SegUnknown` 不因 13 张增加 ----");
        var covAll = EffectText.Coverage(pool, "tactic", pool);
        Eq(covAll.SegUnknown, 0,
           "★ 全池战术卡 `SegUnknown` = 0（同 `RuleEngineTest.cs:3819` 那条哨兵）");

        var cov13 = EffectText.Coverage(transforms, "tactic", pool);
        Eq(cov13.Cards, 0, "★ 只喂那 13 张 ⇒ `Cards` = 0");
        Eq(cov13.SegUnknown, 0, "★ 只喂那 13 张 ⇒ `SegUnknown` = 0（豁免生效）");
        Eq(cov13.SegTotal, 0, "★ 只喂那 13 张 ⇒ `SegTotal` = 0（连分句都不量）");

        // 「不重不漏」恒等式必须仍然成立（B 组的 continue 不能把分句账掰歪）
        var all = EffectText.Coverage(pool, "tactic", pool);
        Eq(all.SegKeyword + all.SegOk + all.SegPartial + all.SegUnknown, all.SegTotal,
           "分句分类总数 = 分句总数（不重不漏 —— 豁免不能把某一句吞掉两遍）");
    }

    // ============================================================ D · 灭自证：摘掉豁免必须红
    static void SectionD_ControlWithoutExemption(List<CardDef> transforms, List<CardDef> pool)
    {
        Debug.Log(P + "---- D 灭自证：同一条 desc + 没有变身标记 ⇒ SegUnknown 恰好 +13 ----");
        var syn = new List<CardDef>();
        foreach (var c in transforms)
        {
            CardDef s = SyntheticPlainTactic(c);

            // 前提（硬检查，⛔ 不用 `if (前提) Check(结论)` 那种静默跳过的写法）
            Ok(s != null, $"「{c.Id}」合成对照卡建得出来");
            if (s == null) continue;
            Ok(!RuleCore.HasWarlordTransformAbility(s),
               $"（前提）合成卡「{s.Id}」`HasWarlordTransformAbility` = false（它没有 `chosenAbility` 列）");
            Ok(s.Desc == c.Desc, $"（前提）合成卡与真卡 **desc 逐字相同**");
            syn.Add(s);
        }

        Eq(syn.Count, 13, "对照组 = 13 张（一张不少）");
        if (syn.Count == 0) return;

        var covSyn = EffectText.Coverage(syn, "tactic", pool);
        Eq(covSyn.Cards, 13, "★ 摘掉豁免的对照组：`Cards` = 13（⇒ 分母会从 446 变成 459）");
        Eq(covSyn.SegUnknown, 13,
           "★ 摘掉豁免的对照组：`SegUnknown` = **13** ⇒ 回归哨兵 `:3819` 会红 —— 这正是重写这句豁免的理由");
        Eq(covSyn.SegTotal, 13, "对照组分句总数 = 13（每张卡恰好一段说明句）");

        // 与真卡自己的解析结果对一眼（同一批句子）
        int unSum = 0;
        foreach (var c in transforms)
        {
            List<string> un, pa;
            EffectText.Parse(c.Desc, out un, out pa);
            unSum += un.Count;
            Eq(pa.Count, 0, $"「{c.Id}」`Parse` 的 `partial` = 0（说明句不是「半懂」）");
        }
        Eq(unSum, 13, "★ 13 张真卡自己走 `Parse` ⇒ `unparsed` 合计 = 13（与对照组逐个对得上）");
    }

    // ============================================================ E · 执行层认得（WillRunOps）
    static void SectionE_WillRunOps(List<CardDef> transforms, List<CardDef> pool)
    {
        Debug.Log(P + "---- E 执行层：`WillRunOps` 给一条 `warlordtransform` op ----");
        Ok(RuleCore.ImplementedEffectVerbs.Contains("warlordtransform"),
           "★ 动词 `warlordtransform` 在 `ImplementedEffectVerbs` 里（否则 `OpHasMechanism` 会把它判成「没机制」）");

        foreach (var c in transforms)
        {
            var ops = EffectText.WillRunOps(c);
            if (!Eq(ops.Count, 1, $"「{c.Id}」`WillRunOps` 恰好 **1 条** op（不是空表）"))
                continue;
            string verb = ops[0] != null ? ops[0].Verb : "（null op）";
            Ok(ops[0] != null && verb == "warlordtransform",
               $"「{c.Id}」这条 op 的动词 = warlordtransform（实际 {verb}）");

            string why = null; bool imprecise = false;
            Ok(ops[0] != null && EffectText.OpHasMechanism(ops[0], c.Faction, pool, out why, out imprecise),
               $"「{c.Id}」`OpHasMechanism` 放行（不会被记成「载荷没机制」；why={why}·imprecise={imprecise}）");
        }

        // 对照：同一条 desc、没有变身标记 ⇒ 仍然是空表（证明是豁免在起作用）
        var syn = new List<CardDef>();
        foreach (var c in transforms) syn.Add(SyntheticPlainTactic(c));
        int nonEmpty = 0;
        foreach (var s in syn)
        {
            var ops = EffectText.WillRunOps(s);
            if (ops != null && ops.Count > 0) nonEmpty++;
        }
        Eq(nonEmpty, 0, "★ 对照组（无变身标记）：`WillRunOps` 仍 **0 张非空** ⇒ 收 op 的是变身豁免，⛔ 不是 `Parse` 自己认了这句说明文");
    }
}
