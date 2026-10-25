// RuleEngineTest_EngineX6.cs — 引擎线 `X6`（2026-10-25 · 执行代理 `X6` · 口径乙）
//
// 为什么另开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册 `_EngineE9` /
// `_EngineX1` / `_EngineX2` … 都在），同一时刻只有一个写手改一个文件 ⇒ 本批测试全写在这里，
// `RuleEngineTest.cs` **只加 `Run()` 里一行 `Step(TestEngineX6)`**
// （⚠️ 另需重生成 `RuleEngineTest_Diag.cs`：`VerifyRunCaseCoverage` ② 拿「反射到的用例方法」↔
//   「生成物里的 `RunCaseNames`」对账 ⇒ 不重生成会红）。
//
// 判据全文（⛔ 不要重新论证）→ `资料/待办判据_引擎_1025b.md` §一（23 张 / 32 行的逐卡映射、
// 「19 可判 / 4 判不了」）＋ `资料/普查产出_1025/X3_引擎判据缺口_执行报告.md` §四·2（三处必须一起改）。
//
// 本批四件事（一次做完，缺一件就是半成品）：
//   ① **生成器**：`abilityLevelCriteria` 每行加 `ca`（`chosenAbility` 的家族；`""` = 判不了）；
//   ② **读列**：`CardDatabase` 读进 `CardDef.TargetCriteriaRowSpec.Family` + 独立版本位（v29）；
//   ③ **消费端**：`CardDef.AbilityLevelCriteriaForFamily` → `RuleCore.AbilityFamilyTargetAllows` →
//      `ResolveOps` 的逐 op 闸（**只在「按 trigger 定位不唯一」的卡上启用** ⇒ 其余卡行为逐字不变）；
//   ④ **断言**（本文件）。
//
// 🔴 每条都带 `🧨`（什么改动能让它红）。期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（`Run()` 里一行 `Step(TestEngineX6)`）。</summary>
    public static void TestEngineX6()
    {
        Section("🆕 引擎线 X6：卡表 v29 `ca`（能力种类族名）· 口径乙：按家族认 ability 级 criteria 行");
        X6T1DataColumn();
        X6T2NineteenResolvable();
        X6T3FourUnresolvable();
        X6T4CriteriaReallyFilters();
        X6T5ProductionPath();
    }

    static readonly string[] X6FamilyWhitelist =
    {
        "draw", "damage", "heal", "buff", "resource", "summon", "create",
        "destroy", "jam", "stun", "handmove", "trigger",
    };

    static CardDef X6Card(List<CardDef> pool, string id)
    {
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && string.Equals(pool[i].Id, id, StringComparison.Ordinal)) return pool[i];
        return null;
    }

    // ==================================================================
    //  ① 数据层：`ca` 落了多少、值合不合法
    // ==================================================================

    /// <summary>
    /// 卡表 v29 的 `ca` 列读数（现算口径 = `cards_engine.json` v29 / 池 1127 张，2026-10-25 现读）。
    /// <para>`abilityLevelCriteria` 全池 **85 行**，其中 `ca` 可判（族名）**80 行**、
    /// 家族不唯一 **3 行**、一条 `abilityLogic` 都没有 **2 行**（后两类落 `""`）。</para>
    /// </summary>
    static void X6T1DataColumn()
    {
        var pool = CardDatabase.Load();
        CheckTrue(pool != null && pool.Count > 0, "① 卡池装起来了");
        if (pool == null || pool.Count == 0) return;

        CheckTrue(pool[0].AbilityLevelCriteriaFamilyVersioned,
                  "★ ① 卡表是 v29+（`AbilityLevelCriteriaFamilyVersioned` 为真）"
                + "｜🧨 生成器版本位没 bump / `CardDatabase` 那一跳没接 ⇒ 红");

        int rows = 0, known = 0, unknownOrEmpty = 0;
        var fams = new HashSet<string>(StringComparer.Ordinal);
        var bad = new List<string>();
        foreach (var c in pool)
        {
            if (c == null) continue;
            rows += c.AbilityLevelCriteriaRowCount;
            known += c.AbilityLevelCriteriaFamilyKnownRowCount;
            for (int ab = 0; ab < 32; ab++)                       // `cardAbilities` 实测最多 3 条，32 是宽余量
            {
                var f = c.AbilityLevelCriteriaFamilyAt(ab);
                if (f == null) continue;                          // 这一格没落（别的列 / 没行）
                if (f.Length == 0) { unknownOrEmpty++; continue; }
                fams.Add(f);
                if (Array.IndexOf(X6FamilyWhitelist, f) < 0) bad.Add(c.Id + ":" + f);
            }
        }
        // 🔴 2026-10-25（`X20`）：85/5 → **86/6**（池内新增的附魔卡带 1 条 ability 级
        //   `targetCriteria` 行，它的 `chosenAbility = 340 removeThisEnchantment` 不在族表里 ⇒ 落「无 logic」那一档）。
        Check(rows, 86,
              "★ ① 全池 `abilityLevelCriteria` 行数 = **86**（原版 `CardAbility.targetCriteria` `+0x30`）"
            + "｜🧨 卡表改了 / 生成器换了 `criteria_carries_targets` 的落法 ⇒ 按新数据重钉");
        Check(known, 80,
              "★ ① 其中 `ca` 可判（族名非空）的行 = **80**"
            + "｜🧨 `ca` 生成逻辑变了（例如把「混族」也硬归一族）⇒ 红");
        Check(unknownOrEmpty, 6,
              "★ ① `ca` 为空（家族判不了）的行 = **6**（3 行混族 + 3 行无 abilityLogic）"
            + "｜🧨 同上");
        Check(bad.Count, 0,
              "★★ ① **跨语言对账**：`ca` 落出去的族名全部在 `CardDef.ActiveAbilityKindFamilyNames` 白名单里"
            + "｜🧨 生成器的 `ABILITY_KIND_FAMILY` 与 C# 的 `ActiveAbilityKindFamily` 漂了 ⇒ 红"
            + "（这正是「两处写同一条规则」那道保险）");
        if (bad.Count > 0)
            UnityEngine.Debug.LogError("[X6] 越界的族名：" + string.Join(" ", bad.ToArray()));
        Check(fams.Count, 10,
              "★ ① 池内实际出现的族名种类 = **10**（无 `jam` / `handmove`）｜🧨 同上");
    }

    // ==================================================================
    //  ② 19 张可判：逐卡逐族 → 认到哪一条 ability
    // ==================================================================

    /// <summary>
    /// 判据文档 §一 的 19 张（逐卡逐族）。**灭自证**写在旁边：这 19 张按 trigger 定位**一律不唯一**
    /// —— 也就是说没有 `ca` 这一层，那 32 行 criteria 一格都消费不到（甲 = OR 对这 19 张恒真空转）。
    /// </summary>
    static void X6T2NineteenResolvable()
    {
        var pool = CardDatabase.Load();
        var table = new (string id, string fam, int ab)[]
        {
            ("dc7e575c0a328f446be1521c23ec850b", "buff", 1),
            ("1ff4815a506d2f34e94ec2f50900d352", "draw", 1),
            ("AM75", "summon", 1),
            ("BL33", "destroy", 1),
            ("DA43", "resource", 1),
            ("DA38", "resource", 1),
            ("b9665eadf1207ff49bcf0a730b0b7f9d", "create", 1),
            ("EC50", "summon", 1),
            ("SAU40", "buff", 0),
            ("SAU40", "trigger", 1),
            ("64a657fc5ed5bbb4fa7b585cf29c1e95", "buff", 1),
            ("b4d6d186eb95a3f468013c5be649ee3b", "heal", 1),
            ("SOR39", "resource", 1),
            ("SW43", "buff", 0),
            ("SW43", "heal", 1),
            ("SW53", "buff", 0),
            ("SW53", "destroy", 1),
            ("SW42", "summon", 1),
            ("TAU58", "destroy", 0),
            ("TAU58", "buff", 1),
            ("4cdc1b5f965f99945b685bca7660e9fa", "resource", 1),
            ("EC60", "buff", 1),
            ("EC60", "draw", 2),
            ("SAU59", "create", 1),
        };
        var cards = new HashSet<string>(StringComparer.Ordinal);
        int okRows = 0;
        foreach (var t in table)
        {
            var c = X6Card(pool, t.id);
            CheckTrue(c != null, "② 找得到卡 " + t.id);
            if (c == null) continue;
            cards.Add(t.id);
            string why;
            bool amb;
            int ab;
            var set = c.AbilityLevelCriteriaForFamily("ThisCardPlayed", t.fam, out why, out amb, out ab);
            bool hit = set != null && ab == t.ab;
            if (hit) okRows++;
            CheckTrue(hit,
                  $"★ ② 「{c.Name}」（{t.id}）族 `{t.fam}` → 认到 **ab={t.ab}**"
                + $"（实得 ab={ab}，why={(why ?? "null")}）"
                + "｜🧨 家族映射 / `ca` 生成 / `AbilityLevelCriteriaForFamily` 任一处错 ⇒ 红");

            // 灭自证：按 trigger 定位这一档**认不出**（不唯一）⇒ 没有家族层就一格都消费不到
            string uwhy;
            bool uamb;
            int uab = c.UniqueAbilityIndexForTrigger("ThisCardPlayed", out uwhy, out uamb);
            CheckTrue(uab < 0 && uamb,
                  $"★★ 灭自证 ② 「{c.Name}」按 trigger 定位**不唯一**（`UniqueAbilityIndexForTrigger` = -1 且 ambiguous）"
                + "｜🧨 若这张卡其实定位唯一（= 它不该在这个清单里）⇒ 红；"
                + "若 `UniqueAbilityIndexForTrigger` 被改成「猜第一条」⇒ 也红");

            CheckTrue(set != null && set == c.AbilityLevelCriteriaAt(t.ab),
                  $"★ ② 「{c.Name}」认出来的 36 格**就是** `AbilityLevelCriteriaAt({t.ab})` 那一份"
                + "（同一对象，⛔ 不是复制品）｜🧨 家族路径另建一份 criteria ⇒ 红");
        }
        Check(cards.Count, 19, "★ ② 可判卡数 = **19**（与判据文档 §一 一致）");
        Check(okRows, table.Length, "★ ② 表里每一行都认得出来（19 张 / 24 个「卡×族」组合）");

        // 「这一族没有条件」⇒ 明确放行（`why == null`，⛔ 不是「判不了」）
        var sw43 = X6Card(pool, "SW43");
        string w2;
        bool a2;
        int ab2;
        var none = sw43.AbilityLevelCriteriaForFamily("ThisCardPlayed", "damage", out w2, out a2, out ab2);
        CheckTrue(none == null && w2 == null && ab2 == -1,
                  "★ ② 「这一族没有条件」⇒ `set=null` 且 `why=null`（明确放行；⛔ 不许当判不了/不许出声）");
    }

    // ==================================================================
    //  ③ 4 张判不了：出声一次、且仍放行
    // ==================================================================

    /// <summary>
    /// 判据文档 §一：`ASH30`/`DA33`/`GSC46`（damage 同时在两条 ability）· `SOR31`（resource 在 ab1/ab2）。
    /// ⇒ 按家族认行**不唯一** ⇒ **出声 + 放行**（⛔ 不许猜，也⛔ 不许当「不满足」把正文掐死）。
    /// </summary>
    static void X6T3FourUnresolvable()
    {
        var pool = CardDatabase.Load();
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        RuleCore.ResetAbilityFamilyGateForTest();
        var friendly = Place(ctx, 0, 3, Unit("X6F", 1, 1, 3));
        var enemy = Place(ctx, 1, 3, Unit("X6E", 1, 1, 3));

        var bad = new (string id, string fam)[]
        {
            ("ASH30", "damage"),      // 两行都是 damage
            ("DA33", "damage"),       // ab0 = damage、ab1 = damage+resource（混族 ⇒ `ca` 为空）
            ("GSC46", "damage"),      // 两行都是 damage
            ("SOR31", "resource"),    // 两行都是 resource
        };
        int before = RuleCore.AbilityFamilyGateWarningsForTest;
        foreach (var t in bad)
        {
            var c = X6Card(pool, t.id);
            CheckTrue(c != null, "③ 找得到卡 " + t.id);
            if (c == null) continue;
            string why;
            bool amb;
            int ab;
            var set = c.AbilityLevelCriteriaForFamily("ThisCardPlayed", t.fam, out why, out amb, out ab);
            CheckTrue(set == null && why != null,
                  $"★ ③ 「{c.Name}」（{t.id}）族 `{t.fam}` ⇒ **判不了**（`set=null` 且 `why` 非空）"
                + "｜🧨 谁把「同族 ≥2 行 / 有行族名为空」当成「选第一行」或「没有条件」⇒ 红");

            string gwhy;
            bool gamb;
            var v = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, c, "ThisCardPlayed", t.fam, enemy,
                                                      out gwhy, out gamb);
            CheckTrue(v != false,
                  $"★★ ③ 「{c.Name}」：闸**不返回 false**（= 放行；实得 {(v == null ? "null" : v.ToString())}）"
                + "｜🧨 判不了被当成「不满足」⇒ 这 4 张卡的正文会被整条掐死 ⇒ 红");
        }
        int after = RuleCore.AbilityFamilyGateWarningsForTest;
        Check(after - before, 4,
              "★★ ③ 4 张**各出声一次**（`AbilityFamilyGateWarningsForTest` +4）"
            + "｜🧨 判不了却不出声 ⇒ 红；出声没去重（每次调用都喊）⇒ 这里会 >4 ⇒ 也红");

        // 去重：同一张卡同一条族再判一次 ⇒ **不再出声**
        {
            string gwhy;
            bool gamb;
            var ash30 = X6Card(pool, "ASH30");
            RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, ash30, "ThisCardPlayed", "damage", enemy,
                                               out gwhy, out gamb);
        }
        Check(RuleCore.AbilityFamilyGateWarningsForTest, after,
              "★★ ③ 同卡同族第二次**不再出声**（去重表生效；计数不动）");
    }

    // ==================================================================
    //  ④ 真卡真棋盘：criteria 真的筛了 + 灭自证
    // ==================================================================

    /// <summary>
    /// 三组「同一候选、两种族」的对照 —— 任何「恒 true / 恒 false / 没接」都会有一条红：
    /// · `SW43`（`Give Armour 1 to a friendly troop or Heal 4 to your Warlord`）：
    ///   族 `buff` 的行要求**友方部队**（`friendlyUnits=1` + `includeMinions=1` + `includeHero=0`）；
    ///   同一张卡的族 `damage` **没有行** ⇒ 同一候选换族 ⇒ `true`。
    /// · `EC60`（`Deal 1. If friendly troop, give Dark Pact. If enemy, draw`）：两族的行**互补**。
    /// · `TAU58` / `AM75`：**第 ⑧ 格**（`unitConditions` 的 `role="target"` `cond=220 dealtKillDmg`）
    ///   也真的拼进来了（`ex` 取反两边都钉）。
    /// </summary>
    static void X6T4CriteriaReallyFilters()
    {
        var pool = CardDatabase.Load();
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var friendly = Place(ctx, 0, 3, Unit("X6F", 1, 1, 3));
        var enemy = Place(ctx, 1, 3, Unit("X6E", 1, 1, 3));
        var hero = ctx.Players[0].Warlord;

        // ---- SW43 ----
        var sw43 = X6Card(pool, "SW43");
        {
            string why;
            bool amb;
            var a = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, sw43, "ThisCardPlayed", "buff", friendly, out why, out amb);
            var b = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, sw43, "ThisCardPlayed", "buff", enemy, out why, out amb);
            var h = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, sw43, "ThisCardPlayed", "buff", hero, out why, out amb);
            CheckTrue(a == true, "★ ④ SW43 族 `buff` + **友方部队** ⇒ true");
            CheckTrue(b == false, "★★ ④ SW43 族 `buff` + **敌方部队** ⇒ **false**（criteria 真的筛了）"
                                + "｜🧨 闸恒真 / 没接 ⇒ 红");
            CheckTrue(h == false, "★ ④ SW43 族 `buff` + **友方督军** ⇒ false（`includeMinions=1` 且 `includeHero=0`）");
            var d = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, sw43, "ThisCardPlayed", "damage", enemy, out why, out amb);
            CheckTrue(d == true, "★★ 灭自证 ④ 同一个**敌方**候选、换到族 `damage`（这张卡没有那一行）⇒ true"
                               + "｜🧨 闸恒 false ⇒ 红（说明上面那条 false 是「真筛」不是「整档禁选」）");
        }

        // ---- EC60（两族互补）----
        var ec60 = X6Card(pool, "EC60");
        {
            string why;
            bool amb;
            bool b1 = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, ec60, "ThisCardPlayed", "buff", friendly, out why, out amb) == true;
            bool d1 = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, ec60, "ThisCardPlayed", "draw", friendly, out why, out amb) == false;
            bool b2 = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, ec60, "ThisCardPlayed", "buff", enemy, out why, out amb) == false;
            bool d2 = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, ec60, "ThisCardPlayed", "draw", enemy, out why, out amb) == true;
            CheckTrue(b1 && d1, "★★ ④ EC60 友方部队：族 `buff`=true / 族 `draw`=false（互补）");
            CheckTrue(b2 && d2, "★★ ④ EC60 敌方部队：族 `buff`=false / 族 `draw`=true（互补）");
        }

        // ---- 第 ⑧ 格（`cond 220 dealtKillDmg`）也拼进来了 ----
        var am75 = X6Card(pool, "AM75");   // ab=1：`cond 220 ex=0`（必须**造成过击杀伤害**）
        var tau58 = X6Card(pool, "TAU58"); // ab=1：`cond 220 ex=1`（必须**没**造成过击杀伤害）
        {
            string why;
            bool amb;
            var a1 = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, am75, "ThisCardPlayed", "summon", friendly, out why, out amb);
            var a2 = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, am75, "ThisCardPlayed", "summon", enemy, out why, out amb);
            CheckTrue(a1 == false, "★ ④ AM75 族 `summon` + 友方 ⇒ false（`enemyUnits=1` 的阵营闸）");
            CheckTrue(a2 == false,
                  "★★ ④ AM75 族 `summon` + **敌方** ⇒ 仍 false —— 因为 `cond 220 dealtKillDmg ex=0` 拼进来了"
                + "（那个新摆的单位没造成过击杀伤害）"
                + "｜🧨 若 `AbilityLevelConditionRowAt`（第 ⑧ 格）被拿掉 ⇒ 这条变 true ⇒ 红");

            var t1 = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, tau58, "ThisCardPlayed", "buff", friendly, out why, out amb);
            var t2 = RuleCore.AbilityFamilyTargetAllows(ctx, 0, null, tau58, "ThisCardPlayed", "buff", enemy, out why, out amb);
            CheckTrue(t1 == false && t2 == true,
                  "★★ ④ TAU58 族 `buff`：友方 false / 敌方 true（`enemyUnits=1` + `cond 220 ex=1` 的**取反**）"
                + "｜🧨 `ex` 没取反 ⇒ 两条方向都翻 ⇒ 红");
        }
    }

    // ==================================================================
    //  ⑤ 生产路径：`ResolveOps` 的逐 op 闸（真卡的 criteria + 一条自造 op）
    // ==================================================================

    /// <summary>
    /// `ResolveOps` 新增的两个形参（`abilityFamilyCard` / `abilityFamilyTrigger`）**真的在筛**。
    /// <para>做法：拿 `EC60` 的真 criteria（族 `draw` 的要求是**敌方**）+ 一条自造 op `Draw a card`
    /// ⇒ 友方候选下**带闸不抽、不带闸照抽**。两条方向相反 ⇒ 「闸没接上 / 恒真 / 恒假」都会被抓住。</para>
    /// <para>⚠️ 这正是 `RuleCore.AbilityFamilyGateSkipForTest` 存在的理由（`ctx.Log` 抓不到）。</para>
    /// </summary>
    static void X6T5ProductionPath()
    {
        var pool = CardDatabase.Load();
        var ec60 = X6Card(pool, "EC60");
        var drawOps = EffectText.Parse("Draw a card", out _, out _, card: ec60);
        Check(drawOps.Count, 1, "★ ⑤ 受控 op 表 = 1 条（`Draw a card`）");
        CheckTrue(drawOps.Count == 1 && drawOps[0].Verb == "draw", "★ ⑤ 受控 op 的动词 = `draw`");
        if (drawOps.Count != 1 || drawOps[0].Verb != "draw") return;

        // ① 带闸 + 友方候选 ⇒ 族 `draw` 的行要求敌方 ⇒ **筛掉**
        var k1 = Battle(new CardDef[0], new CardDef[0]);
        var f1 = Place(k1, 0, 3, Unit("X6F1", 1, 1, 3));
        int h0 = k1.Players[0].Hand.Count;
        RuleCore.ResetAbilityFamilyGateForTest();
        RuleCore.ResolveOps(k1, 0, null, drawOps, f1, out _,
                            sourceCard: ec60, abilityFamilyCard: ec60,
                            abilityFamilyTrigger: "ThisCardPlayed");
        Check(k1.Players[0].Hand.Count - h0, 0,
              "★★ ⑤ 生产路径·带闸：友方候选 ⇒ `draw` 被**筛掉**（手牌净增 0）"
            + "｜🧨 `ResolveOps` 不接那两个形参 / `ApplyAbilityFamilyGate` 没跑 ⇒ 会 +1 ⇒ 红");
        Check(RuleCore.AbilityFamilyGateSkipForTest, 1,
              "★★ ⑤ 生产路径·带闸：`AbilityFamilyGateSkipForTest` = **1**（真的走了一次 skip）"
            + "｜🧨 同上；或闸恒 true（根本没筛）⇒ 0 ⇒ 红");

        // ② 同一局面、不传闸 ⇒ 照抽（灭自证）
        var k2 = Battle(new CardDef[0], new CardDef[0]);
        var f2 = Place(k2, 0, 3, Unit("X6F2", 1, 1, 3));
        int h1 = k2.Players[0].Hand.Count;
        RuleCore.ResolveOps(k2, 0, null, drawOps, f2, out _, sourceCard: ec60);
        Check(k2.Players[0].Hand.Count - h1, 1,
              "★★ 灭自证 ⑤ 不传闸 ⇒ 同一局面照常抽（手牌净增 1）"
            + "｜🧨 若 `ResolveOps` 无条件套闸 ⇒ 这条变 0 ⇒ 红（说明闸只该由调用点显式打开）");

        // ③ 族↔动词表的两个边界（消费端用的就是它）
        var two = CardDef.FamiliesOfVerb("choosecard");
        CheckTrue(two.Length == 2
                  && Array.IndexOf(two, "create") >= 0 && Array.IndexOf(two, "handmove") >= 0,
                  "★ ⑤ `choosecard` 被两族同时认（`create` + `handmove`）⇒ 消费端按判不了处置（⛔ 不挑第一族）");
        var one = CardDef.FamiliesOfVerb("deal");
        CheckTrue(one.Length == 1 && one[0] == "damage", "★ ⑤ `deal` → 唯一族 `damage`");
        Check(CardDef.FamiliesOfVerb("chooseone").Length, 0,
              "★ ⑤ `chooseone`（`Give A or B` 折成的**单条** op）不属于任何族 ⇒ 不判"
            + "（如实标着的覆盖缺口：`SW43`/`SAU40` 这类卡的生产路径上，族闸碰不到它们的分支）");
    }
}
