// RuleEngineTest_G10.cs — 引擎线 `G10` 的断言（2026-10-24）
//
// 本件只钉【解析分类】这一层：`for each … in <谁的> hand` 的介词短语**收口到
// `EffectText.ReCountInHand` 一处**（改前那条正则内联在 `ClassifyCount` 里、硬要求
// `in **the** <谁的> hand` ⇒ 卡面写 `in your hand` 的 `GSC75` 匹配不上、静默落进「盘面」）。
//
// ⛔ 结算层 `EffectResolver.CountFor` 的 `hand` / `enemyhand` 分支是 `A1` / `G6` 的活，
//    本件**一行没改**；这里只调 `RuleCore.CountForTest` 验「分类落对之后，侧也数得对」。
//
// 判据（按权威顺序）：
//   ① **卡面原文**（`Resources/cards_engine.json` 的 `desc`，与 `d:/2/Warpforge部队卡片/` 的成品卡图同字）：
//      · `GSC75 Roaming Outriders` = `Deal 1 damage to a random enemy and Stun it.
//        Repeat for each Vehicle **in your hand** and lower their cost by 1`
//      · `GSC56 A Trap Sprung` = `Deal 3 damage to a random enemy. Repeat this effect
//        for each Sabotage **in the opponent's hand**`
//   ② 原版 `AbilityLogic__GetTargets.c:884-892` 的 `inHandFiltered(50)` 那一支 ⇒ 候选是**手牌**、
//      **侧**由 `friendlyUnits` / `enemyUnits` 两个布尔位定，卡种 / `spellType` 再筛。
//      结构化侧证：`GSC56` 的 `multiCountCriteria` = `ta 50 / enemyUnits 1 / includeSpells 1 /
//      spellType 230`；`GSC75` 的 `multiplier` = `ta 50 inHandFiltered / friendlyUnits / keywordFilter`
//      （同族，另见 `资料/普查产出_1024/C6_最后三条判据.md` §1·5、`G6_AM18与GOF4根因.md` §八·3）。
//   ③ **改前实测**（`ruleprobe card "Roaming Outriders"`）：
//      `countRef=[own|vehicle|all] countScope=[board]` ⇒ 数的是**场上**的载具（应数手牌）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 期望值一律写死字面量（`hand` / `enemyhand` / `board` / `draw` / `2` / `1`），
//    ⛔ 不从实现常量反算（那就是自证）。
//
// ⚠️ **本文件还没接线**：`RuleEngineTest.cs`（`Run()` 里加一行 `Step(...)`）与
//    `RuleEngineTest_Diag.cs`（`工具/gen_diag_runner.py` 重生成）都**不在 G10 的白名单** ⇒
//    由主对话补。补之前 `Run()` 的覆盖闸会红并点名 `TestG10HandCountScope`（那是**设计如此**：
//    它宁可红，也不要「文件在、用例从没跑过」）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本批的**唯一入口**（`Run()` 里一行 `Step(TestG10HandCountScope);`）。
    /// 四个子用例是 `G10…` 打头的**辅助方法**（⛔ 不用 `Test`/`Report` 打头 ——
    /// `RunCoverageCompiledCases()` 会把 `Test*` 当用例、要求 `Run()` 里各有一条 `Step`）。</summary>
    public static void TestG10HandCountScope()
    {
        Section("🆕 引擎线 G10：`for each … in <谁的> hand` 的计数分类（`GSC75` / `GSC56`）");
        G10RealCards();
        G10VariantShapes();
        G10NotHandNegative();
        G10CountForSide();
    }

    /// <summary>解析一段卡面文本，取**第一条带计数的 op**。
    /// 形状说明（`ruleprobe` 实测）：`Repeat for each …` 会落成一条独立的 `repeat` op；
    /// `Deal … for each …`（后置型）则把计数挂在 `deal` 那条**自己**身上 —— 两种都从
    /// `CountScope` 非空这条口取，⛔ 别按 `Verb` 找（那会把后置型漏掉）。</summary>
    static EffectOp G10FirstCounted(List<EffectOp> ops)
    {
        foreach (var o in ops) if (!string.IsNullOrEmpty(o.CountScope)) return o;
        return null;
    }

    // ==============================================================================================
    //  ① 真卡正例：两张 —— 一张**自己**的手牌（`GSC75`，改前错的那张）、一张**对手**的手牌（`GSC56`）
    // ==============================================================================================

    /// <summary>
    /// 🔴 **这一对是「灭自证」的主体**：两张卡**同一句实现、相反的两侧** ——
    /// 任何「侧一律返回 X」的实现都不可能同时满足 ①（要 `hand`）与 ②（要 `enemyhand`）；
    /// 而「退回改前那条内联正则」只能满足 ②、①必红。
    /// </summary>
    static void G10RealCards()
    {
        var pool = CardDatabase.Load();

        // ---- 正例 ① `GSC75 Roaming Outriders`：`Repeat for each Vehicle in **your** hand` ----
        {
            var c = CardDatabase.FindById(pool, "GSC75");
            CheckTrue(c != null, "(前提) GSC75 在池内");
            if (c != null)
            {
                var op = G10FirstCounted(EffectText.Parse(c.Desc, out _, out _, card: c));
                CheckTrue(op != null, "(前提) GSC75 解析出一条带 `for each` 的 op");
                if (op != null)
                {
                    Check(op.CountScope, "hand",
                          "★★ ① GSC75：计数来源 = `hand`（**自己的手牌**）"
                        + "｜🧨 退回 `board` ⇒ 红 —— 改前就是这样：`ruleprobe` 实测 `countRef=[own|vehicle|all]`，"
                        + "数的是**场上**的载具（卡面无 `in play`，一个场上载具都不该数）");
                    Check(op.CountRef, "vehicle",
                          "★★ ① GSC75：筛选词 = `vehicle`（不是 `any`、也不是整手牌）"
                        + "｜🧨 筛选词被吞成 `any` ⇒ 红（那会把手里所有牌都数进去）");
                }
            }
        }

        // ---- 正例 ② `GSC56 A Trap Sprung`：`for each Sabotage in **the opponent's** hand` ----
        //  这条**改前就对**（`in the <谁的>` 那个形状能匹配）—— 留作**另一侧**的锚点：
        //  改法若为了救 ① 而把侧一律当自己 ⇒ 这一条必红。
        {
            var c = CardDatabase.FindById(pool, "GSC56");
            CheckTrue(c != null, "(前提) GSC56 在池内");
            if (c != null)
            {
                var op = G10FirstCounted(EffectText.Parse(c.Desc, out _, out _, card: c));
                CheckTrue(op != null, "(前提) GSC56 解析出一条带 `for each` 的 op");
                if (op != null)
                {
                    Check(op.CountScope, "enemyhand",
                          "★★ ① GSC56：计数来源 = `enemyhand`（**对手手牌**）"
                        + "｜🧨 归成 `hand`（数自己手牌）或 `board` ⇒ 红");
                    Check(op.CountRef, "sabotage",
                          "★★ ① GSC56：筛选词 = `sabotage`（原版 `spellType = 230` 那一格）"
                        + "｜🧨 筛选词丢了 ⇒ 红");
                }
            }
        }
    }

    // ==============================================================================================
    //  ② 写法变体（合成句，不是真卡）：**一条一个写法** —— 钉「收口到一处」这件事本身
    // ==============================================================================================

    /// <summary>
    /// `EffectText.ReCountInHand` 的 doc 里那张「收进来的写法」表，逐条在这里钉住。
    /// ⚠️ 这几条是**合成句**：池内只有 `GSC75` / `GSC56` 两种写法（另 4 张的 `in your hand` 是
    /// **目的地**、不计手牌，见 ③），所以「同族的 `opponents'` / `their` / 不带 `the` 的
    /// `in enemy hand`」只能靠合成句守 —— 它们今天**没有真卡**，属**形状档**。
    /// </summary>
    static void G10VariantShapes()
    {
        const string head = "Deal 1 damage to an enemy for each ";
        var cases = new[]
        {
            // 写法（拼在 head 后面）, 期望 scope, 期望 reference
            new[] { "Vehicle in your hand",            "hand",      "vehicle"  },  // `GSC75` 的写法（硬条件去掉 `the` 之后仍要中）
            new[] { "Sabotage in the opponent's hand", "enemyhand", "sabotage" },  // `GSC56` 的写法（改前唯一中的那种）
            new[] { "Vehicle in the enemy hand",       "enemyhand", "vehicle"  },  // 带 `the` + 名词所有格
            new[] { "Vehicle in enemy hand",           "enemyhand", "vehicle"  },  // ⚠️ 不带 `the`（`GSC37` 是这种形状，但那句今天 unparsed）
            new[] { "Sabotage in the opponents' hand", "enemyhand", "sabotage" },  // 复数所有格
            new[] { "Sabotage in their hand",          "enemyhand", "sabotage" },  // `their`（形状档，池内 0 例）
            new[] { "card in your hand",               "hand",      "any"      },  // 通称 → `any`
            new[] { "a vehicle in your hand",          "hand",      "vehicle"  },  // 冠词 `a` 要剥掉
        };

        foreach (var k in cases)
        {
            string desc = head + k[0];
            var op = G10FirstCounted(EffectText.Parse(desc, out _, out _));
            CheckTrue(op != null, "(前提) `" + k[0] + "` 解析出计数");
            if (op == null) continue;
            Check(op.CountScope, k[1],
                  "★★ ② 写法 `… " + k[0] + "` ⇒ `" + k[1] + "`"
                + "｜🧨 少收一种写法（例如又把 `in the` 写回硬条件、或不认复数所有格）⇒ 落回 `board` ⇒ 红");
            Check(op.CountRef, k[2],
                  "★★ ② 写法 `… " + k[0] + "` 的筛选词 = `" + k[2] + "`"
                + "｜🧨 冠词没剥 / 通称没归 `any` / 筛选词丢了 ⇒ 红");
        }
    }

    // ==============================================================================================
    //  ③ 反例：**句子里出现 `hand` 但计数不该数手牌** —— 这三条挡「正则放太宽」
    // ==============================================================================================

    /// <summary>
    /// 🔴 **与 ①② 互为灭自证**：只把 ① 改绿是不够的 —— 若有人把规则放宽成「句子里有 `hand` 就数手牌」，
    /// ①② 仍全绿，但下面三条会红。**两侧都要绿才算这条改对了。**
    /// </summary>
    static void G10NotHandNegative()
    {
        var pool = CardDatabase.Load();

        // ---- 反例 ① 真卡 `GSC47 Perfect Hosts`：`Create one Neophyte Hybrid **in your hand**
        //      for each enemy unit` —— `in your hand` 是**目的地**，计数对象是 `enemy unit`。
        {
            var c = CardDatabase.FindById(pool, "GSC47");
            CheckTrue(c != null, "(前提) GSC47 在池内");
            if (c != null)
            {
                var op = G10FirstCounted(EffectText.Parse(c.Desc, out _, out _, card: c));
                CheckTrue(op != null, "(前提) GSC47 解析出一条带 `for each` 的 op");
                if (op != null)
                {
                    Check(op.CountScope, "board",
                          "★★ ③ GSC47：`in your hand` 是**造牌目的地** ⇒ 计数仍是 `board`（数敌方场上单位）"
                        + "｜🧨 把「句子里有 hand」当成手牌计数 ⇒ 红（会把「敌方单位数」数成「自己手牌数」）");
                    CheckTrue(op.CountRef != null && op.CountRef.StartsWith("enemy|"),
                              "★★ ③ GSC47：数的是**敌方**（`countRef = " + (op.CountRef ?? "(null)") + "`）"
                            + "｜🧨 侧被手牌那句带偏 ⇒ 红");
                }
            }
        }

        // ---- 反例 ② 真卡 `BL51 Hosts of Chaos`：`For each troop **drawn**, create … in your hand`
        //      —— 计数源是**本次抽到的牌**（`draw`），`in your hand` 同样只是目的地。
        {
            var c = CardDatabase.FindById(pool, "BL51");
            CheckTrue(c != null, "(前提) BL51 在池内");
            if (c != null)
            {
                var op = G10FirstCounted(EffectText.Parse(c.Desc, out _, out _, card: c));
                CheckTrue(op != null, "(前提) BL51 解析出一条带 `for each` 的 op");
                if (op != null)
                {
                    Check(op.CountScope, "draw",
                          "★★ ③ BL51：计数源仍是 `draw`（本次抽到的牌）"
                        + "｜🧨 被句尾的 `in your hand` 抢成 `hand` ⇒ 红（抽到的牌 ≠ 手里现有的牌）");
                    Check(op.CountRef, "any",
                          "★★ ③ BL51：`for each troop drawn` 的筛选词 = `any`（与改前一致，本件不该动它）"
                        + "｜🧨 这一格被本件顺手改了 ⇒ 红（说明改动面超出了「手牌介词短语」）");
                }
            }
        }

        // ---- 反例 ③ 合成句：盘面句式 `for each friendly vehicle **in play**` 必须仍是 `board` ----
        {
            var op = G10FirstCounted(
                EffectText.Parse("Deal 1 damage to an enemy for each friendly vehicle in play", out _, out _));
            CheckTrue(op != null, "(前提) 盘面句式解析出计数");
            if (op != null)
            {
                Check(op.CountScope, "board",
                      "★★ ③ `for each friendly vehicle in play` ⇒ 仍是 `board`（`in play` 与 `in hand` 是两回事）"
                    + "｜🧨 把 `in play` 也并进手牌那条正则 ⇒ 红（数错一整个维度）");
                Check(op.CountRef, "own|vehicle|all",
                      "★★ ③ `for each friendly vehicle in play` 的 `countRef` 一字不变 = `own|vehicle|all`"
                    + "｜🧨 动了盘面分支的拼法 ⇒ 红（那是 `A1`/`G6` 已收口的口径）");
            }
        }
    }

    // ==============================================================================================
    //  ④ 侧（`: 分类落对之后「数得对」）—— 同一份 `CountRef`，两种 scope 数两个手牌
    // ==============================================================================================

    /// <summary>
    /// 🔴 **为什么还要这一组**：①② 的 `CountScope` 断言只能证明「分类字面量对」；
    /// 若分类的**侧**与消费端的侧同时被写反（`hand` ↔ `enemyhand`），字面量仍能各自对上 ——
    /// 这里用**同一份 `CountRef = "vehicle"`** 走两个 scope，把「侧确实是分开的」钉死。
    /// ⚠️ 本组**不测 `CountFor` 的实现**（不在本件范围）：期望值来自「谁的手里有几张」这个夹具事实。
    /// </summary>
    static void G10CountForSide()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        ctx.Players[0].Hand.Clear();   // 起手抽到的 filler 不参与本组读数
        ctx.Players[1].Hand.Clear();

        var veh = new CardDef("G10_Vehicle", "G10 Vehicle", "unit", "", "common", "Test",
                              1, 1, 1, 0, null, subtype: "Vehicle");
        var plain = Tactic("G10_Plain", 1, "Deal 1 damage");
        ctx.Players[0].Hand.Add(ctx.NewInstance(veh));     // 自己：2 张载具
        ctx.Players[0].Hand.Add(ctx.NewInstance(veh));
        ctx.Players[0].Hand.Add(ctx.NewInstance(plain));   // 自己：1 张非载具
        ctx.Players[1].Hand.Add(ctx.NewInstance(veh));     // 对手：1 张载具

        Check(RuleCore.CountForTest(ctx, 0, null,
                  new EffectOp { CountScope = "hand", CountRef = "vehicle" }), 2,
              "★★ ④ `hand` + `vehicle`：自己手里 2 张载具 + 1 张战术 ⇒ 数出 **2**"
            + "｜🧨 数成整手牌（3）⇒ 红；数成对手的手牌（1）⇒ 红");

        Check(RuleCore.CountForTest(ctx, 0, null,
                  new EffectOp { CountScope = "enemyhand", CountRef = "vehicle" }), 1,
              "★★ ④ 同一份 `vehicle`、换成 `enemyhand` ⇒ 数出 **1**（对手那 1 张）"
            + "｜🧨 两个 scope 不分开（恒数同一侧）⇒ 这两条必有一条红 —— 这就是本组的用途");
    }
}
