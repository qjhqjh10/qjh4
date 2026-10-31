// B108_A16_Assert.cs — 第一百零八会话 · 执行代理 `B108_A16` 的断言
//
// ============================ 它盯的是什么 ============================
// `RuleEngine/Core/Enchantment.cs` 的 `Enchantments.Matching(carrier, keyword)` —— 附魔参与触发派发的
// **第一道闸**（`RuleCore.FireTriggerAt` / `FireTriggerAlways` 都靠它判「这个载体身上有没有附魔要说
// 话」）。改前它**只按卡面正文**（`TriggerOps` / `Effect`），而 `Pray Enchantment`（`9218576f…`）的
// `desc` 为空 ⇒ 它**永远进不了匹配表** ⇒ `Pray_Exit`（tg 60 UnitDamaged）静默不播。
//
// ============================ 原版口径（判据）============================
//   · `decomp_full/RawCardScript__TriggerAbilities.c:59-108`：附魔那一趟逐条取
//     `enchantmentCard.cardAbilities(+0x290)`，交给
//     `CardAbility__CanTriggerAbility(ability, trigger, …)`；
//   · `decomp_full/CardAbility__CanTriggerAbility.c:71`：
//     `if (*(int *)(param_1 + 0x10) != param_2) return 0;` —— **比的是数据位 `CardAbility.trigger(+0x10)`**，
//     与卡面正文**无关**（正文只是 `abilityLogic` 被选中之后怎么跑的问题）；
//   · ⚠️ **不是** `GetAbilitiesForTrigger(…, includeEnchantments: true)` 那条路：那个形参/方法
//     `TriggerAbilities` **从不调用**，而且它附魔那一趟**只看 `cardAbilities[0]`**（`W58` 订正，
//     见 `RuleCore.cs:12721-12729`）。本断言钉的是 `TriggerAbilities` 那一条。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加**一个块**（建议紧接 `{ CheckB108A1TriggerEmits(); }` 之后）：
//       { CheckB108A16EnchantDataTriggerMatch(); }
//   🔴 故意不用 `Step(...)`、入口也不带 `Test` 前缀（理由同 `B108_A1` / `B97_ER_Assert`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · S2/S3（数据位命中）：把 `Matching` 里 `|| e.Card.AbilityTriggerPresence(keyword) == 1` 删掉
//     ⇒ 真卡 `Pray Enchantment` 的 `Matching("UnitDamaged")` 回到 0 ⇒ 红。
//   · S3（第一道闸真的打开）：同上一删 ⇒ `FireTriggerAt` 走 `ops==null && … && ench.Count==0` 的早退
//     ⇒ 返回 false、`EvtKind.Trigger` 0 条 ⇒ 红。
//   · S4（正文那 122 处不被判死）：把 OR 写成**替换**（删掉左边两项）⇒ 只带正文、没有 versioned
//     数据位的夹具 `Matching("rally")` 回到 0 ⇒ 红；池内那批「正文非空」的卡数也会掉。
//   · S5（`-1 未知 ≠ 1`）：把 `== 1` 放宽成 `!= 0` ⇒ 同一张**没 versioned** 的夹具会命中 ⇒ 红。
//   · S6（演出数据仍在）：把 `card_vfx_by_card.json` 里 `9218576f…` 的 `hit` 桶删掉 ⇒ 红。
using System;
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件唯一入口（由宿主 `Run()` 里一个 `{ … }` 块调；⛔ 不叫 `Run` / 不带 `Test` 前缀）。</summary>
    public static void CheckB108A16EnchantDataTriggerMatch()
    {
        Section("🆕 B108_A16：附魔匹配 = 数据位 OR 正文（`Pray Enchantment` 的 `Pray_Exit` 第一道闸）");

        List<CardDef> pool = null;
        try { pool = CardDatabase.Load(); }
        catch (Exception e) { Debug.LogWarning("[B108_A16] `CardDatabase.Load()` 抛了：" + e.Message); }
        CheckTrue(pool != null && pool.Count > 500, "前提：卡池读得到且规模正常（≥ 500 张）");
        if (pool == null || pool.Count <= 500) return;

        CardDef pray = null;
        for (int i = 0; i < pool.Count && pray == null; i++)
        {
            var c = pool[i];
            if (c != null && c.Id == Enchantments.PrayEnchantmentCardId) pray = c;
        }
        CheckTrue(pray != null, "前提：池里有 `Pray Enchantment`（`id " + Enchantments.PrayEnchantmentCardId + "`）");
        if (pray == null) return;

        SectionS1_DataShape(pray);
        SectionS2_MatchingRealCard(pool, pray);
        SectionS3_FirstGateOpens(pray);
        SectionS4_BodyPathKept();
        SectionS5_UnknownIsNotMatch();
        SectionS6_VfxDataStillThere();
    }

    // ==================================================================
    //  S1 —— 数据形状：数据位有 60/31、正文一个都没有（= 改前那道闸为什么会关）
    // ==================================================================
    static void SectionS1_DataShape(CardDef pray)
    {
        Section("S1 数据形状：`abilityTriggers` 有 60/31，`desc` 空 ⇒ 改前 `Matching` 恒空");

        CheckTrue(pray.IsEnchantment, "S1a 前提：它真的是附魔卡（`IsEnchantment`）");
        CheckTrue(string.IsNullOrEmpty(pray.Desc), "S1b 前提：`desc` 为空（这是「正文匹配不命中」的根因）");
        Check(pray.AbilityTriggerPresence("UnitDamaged"), 1,
              "★★ S1c 数据位：`AbilityTriggerPresence(\"UnitDamaged\") == 1`"
            + "（`cards_engine.json` 的 `abilityTriggers` 真有 60；判据 `CardAbility__CanTriggerAbility.c:71`）");
        Check(pray.AbilityTriggerPresence("TurnStartPlayer"), 1,
              "★★ S1d 数据位：`TurnStartPlayer(31)` 也在（`Pray_Success` 那一行）");
        Check(pray.AbilityTriggerPresence("UnitAttack"), 0,
              "★ S1e 灭自证/反面：`UnitAttack(50)` **明确没有**（= 0，不是 -1）");
        CheckTrue(pray.TriggerOps("UnitDamaged") == null,
                  "★★ S1f **缺口本体**：正文 `TriggerOps(\"UnitDamaged\")` 是 null（改前 `Matching` 只认它 ⇒ 永不命中）");
        CheckTrue(pray.Effect("UnitDamaged") == null,
                  "★★ S1g 同一句：`Effect(\"UnitDamaged\")` 也是 null（原版卡恒空表，`CardDef.FromOriginalPool` 那道闸）");
        Check(pray.TriggerAbilityIndices != null ? pray.TriggerAbilityIndices.Length : -1, 3,
              "★ S1h 三行 `triggerAbilityIndices`（ab0/lg0/60 · ab1/lg0/31 · ab1/lg1/31）——数据位可逐行复查");
    }

    // ==================================================================
    //  S2 —— 真卡：挂上去之后 `Matching` 命中（这就是本次修的那一格）
    // ==================================================================
    static void SectionS2_MatchingRealCard(List<CardDef> pool, CardDef pray)
    {
        Section("S2 真卡：`Pray Enchantment` 挂到载体上 ⇒ `Matching` 命中 60 / 31、不命中 50");

        var carrierCard = Unit("B108A16Carrier", 2, 5, 5);
        var ctx = ProbeBattle(new[] { carrierCard }, new[] { Unit("B108A16Foe", 1, 1, 5) });
        var carrier = Place(ctx, 0, 3, carrierCard, exhausted: true);
        CheckTrue(carrier != null, "S2a 前提：载体上场了");
        if (carrier == null) return;

        var eff = Enchantments.Attach(ctx, carrier.Instance, pray, Enchantments.BuffTypeSingleUse);
        CheckTrue(eff != null, "S2b 前提：附魔挂上了（`Enchantments.Attach` 的唯一挂载口）");
        CheckTrue(Enchantments.FindPray(carrier) != null, "S2c 前提：`FindPray` 认得它（按 id）");

        Check(Enchantments.Matching(carrier, "UnitDamaged").Count, 1,
              "★★★ S2d **本次修的那一格**：`Matching(carrier, \"UnitDamaged\").Count == 1`"
            + "｜🧨 删掉 `Enchantment.cs` 里 `|| e.Card.AbilityTriggerPresence(keyword) == 1` ⇒ 回到 0 ⇒ 红");
        Check(Enchantments.Matching(carrier, "TurnStartPlayer").Count, 1,
              "★★ S2e 同一格里 31 也命中（`Pray_Success` 那一行的派发口）");
        Check(Enchantments.Matching(carrier, "UnitAttack").Count, 0,
              "★★ S2f 灭自证/反面：`UnitAttack` **不**命中（数据位是 0，⛔ 别把 0 读成 1）");

        // 载体换一张没有附魔的 ⇒ 同样的问题问出来恒 0（前提断言，⛔ 不是 `if (前提)` 静默跳过）
        var bare = Place(ctx, 0, 4, Unit("B108A16Bare", 2, 5, 5), exhausted: true);
        CheckTrue(bare != null, "S2g 前提：第二张（没有附魔的）载体上场了");
        if (bare == null) return;
        Check(Enchantments.Matching(bare, "UnitDamaged").Count, 0,
              "S2h 对照：**没有**这条附魔的载体 ⇒ 0（把「命中」归因到附魔本身，而不是这个关键词本身）");
    }

    // ==================================================================
    //  S3 —— 第一道闸真的打开：`FireTriggerAt` 由 false 变 true，并发出 `EvtKind.Trigger`
    // ==================================================================
    static void SectionS3_FirstGateOpens(CardDef pray)
    {
        Section("S3 第一道闸：`FireTriggerAt(载体, \"UnitDamaged\")` 入队 + 发 `EvtKind.Trigger`");

        var carrierCard = Unit("B108A16GateCarrier", 2, 5, 5);
        var ctx = ProbeBattle(new[] { carrierCard }, new[] { Unit("B108A16GateFoe", 1, 1, 5) });
        var carrier = Place(ctx, 0, 2, carrierCard, exhausted: true);
        CheckTrue(carrier != null, "S3a 前提：载体上场了");
        if (carrier == null) return;
        var eff = Enchantments.Attach(ctx, carrier.Instance, pray, Enchantments.BuffTypeSingleUse);
        CheckTrue(eff != null, "S3b 前提：附魔挂上了");

        ctx.ClearSignals();
        bool fired = RuleCore.FireTriggerAtForTest(ctx, carrier, "UnitDamaged", 0, 2);
        CheckTrue(fired,
                  "★★★ S3c **第一道闸打开**：`FireTriggerAt` 返回 true（= 入队成功）"
                + "｜🧨 删掉数据位那一项 ⇒ `ops/spec/skipDraw` 全 null 且 `ench.Count == 0` ⇒ 早退 false ⇒ 红");

        int n = 0;
        BattleEvent first = null;
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e != null && e.Kind == EvtKind.Trigger && e.Keyword == "UnitDamaged") { n++; if (first == null) first = e; }
        }
        Check(n, 1, "★★ S3d 恰好一条 `EvtKind.Trigger(keyword = \"UnitDamaged\")`（引擎侧唯一的「这件事发生了」信号）");
        CheckTrue(first != null && first.Effect == "UNITDAMAGED",
                  "★★ S3e 信号带 `Effect = \"UNITDAMAGED\"`（op 为空时的兜底原文 —— 证明它走的是 `FireTriggerAt`"
                + " 而不是 `FireTriggerAlways` 的「没正文」那一支）");

        // 对照：同一张卡上换一个**数据位没有**的关键词 ⇒ 仍然 false、0 条（闸没有整体放宽）
        ctx.ClearSignals();
        bool wrong = RuleCore.FireTriggerAtForTest(ctx, carrier, "UnitAttack", 0, 2);
        int nWrong = 0;
        for (int i = 0; i < ctx.Signals.Count; i++)
        {
            var e = ctx.Signals[i];
            if (e != null && e.Kind == EvtKind.Trigger && e.Keyword == "UnitAttack") nWrong++;
        }
        CheckTrue(!wrong, "★★ S3f 灭自证/边界：`UnitAttack` 仍然 **不**入队（不是「有附魔就一律放行」）");
        Check(nWrong, 0, "★★ S3g 而且一条 `EvtKind.Trigger(\"UnitAttack\")` 都不发");
    }

    // ==================================================================
    //  S4 —— 正文那条路**没被判死**（OR 而不是替换）
    // ==================================================================
    static void SectionS4_BodyPathKept()
    {
        Section("S4 正文匹配不被判死（OR 而不是替换）—— 全池 122 处那一族");

        // ---- 夹具：只有正文、**没有** versioned 数据位 -------------------------------------
        var bodyCard = new CardDef("B108A16Body", "B108A16Body", "unit",
                                   "Rally: Deal 1 damage to an enemy.", "common", "Test", 1, 1, 1, 0,
                                   new[] { "Rally" });
        CheckTrue(bodyCard.TriggerOps("rally") != null,
                  "S4a 前提：夹具的 `Rally:` 正文真的收进 `TriggerOps`（收不进 ⇒ 下面的结论不成立）");
        Check(bodyCard.AbilityTriggerPresence("rally"), -1,
              "S4b 前提：夹具**没有** versioned 数据位 ⇒ presence = **-1 未知**（不是 0、不是 1）");
        if (bodyCard.TriggerOps("rally") == null) return;

        var ctx = ProbeBattle(new[] { bodyCard }, new[] { Unit("B108A16S4Foe", 1, 1, 5) });
        var carrierCard = Unit("B108A16S4Carrier", 2, 5, 5);
        var carrier = Place(ctx, 0, 3, carrierCard, exhausted: true);
        CheckTrue(carrier != null, "S4c 前提：载体上场了");
        if (carrier == null) return;
        Enchantments.Attach(ctx, carrier.Instance, bodyCard, Enchantments.BuffTypeSingleUse);
        Check(Enchantments.Matching(carrier, "rally").Count, 1,
              "★★★ S4d **正文路仍然命中**：只带正文（数据位未知）的附魔 ⇒ `Matching(\"rally\").Count == 1`"
            + "｜🧨 把 OR 写成替换（删掉 `TriggerOps`/`Effect` 两项）⇒ 0 ⇒ 红（= 全池 122 处被判死）");

        // ---- 关键词 → 原版 trigger 名 的换算（`rally` → `ThisCardPlayed(0)`）---------------
        var rallyData = new CardDef("B108A16RallyData", "B108A16RallyData", "unit", "", "common", "Test", 1, 1, 1, 0,
                                    null, abilityTriggers: new[] { 0 }, abilityTriggersVersioned: true);
        Check(rallyData.AbilityTriggerPresence("rally"), -1,
              "S4g 前提：拿引擎关键词 `rally` 直接查数据位 ⇒ **-1**（`AbilityTriggerNumberOfName` 里没有 `Rally`，"
            + "原版它是 `ThisCardPlayed(0)`）");
        var c3 = Place(ctx, 0, 5, Unit("B108A16S4C3", 2, 5, 5), exhausted: true);
        CheckTrue(c3 != null, "S4h 前提：载体 3 上场了");
        if (c3 != null)
        {
            Enchantments.Attach(ctx, c3.Instance, rallyData, Enchantments.BuffTypeSingleUse);
            Check(Enchantments.Matching(c3, "rally").Count, 1,
                  "★★ S4i **名字换算**：`rally → ThisCardPlayed(0)`（`RuleCore.TriggerNameOfKeyword`）⇒ 数据位命中；"
                + "这张夹具**没有正文**，命中的只可能是数据位"
                + "｜🧨 把 `RuleCore.TriggerNameOfKeyword(keyword) ?? keyword` 换回裸 `keyword` ⇒ 0 ⇒ 红");
        }

        // ---- 池内普查：正文非空的卡有多少 ------------------------------------------------
        var pool = CardDatabase.Load();
        string[] words = { "rally", "strike", "slay", "backlash", "penitence" };
        int total = 0, dataLess = 0;
        var brk = new System.Text.StringBuilder();
        for (int w = 0; w < words.Length; w++)
        {
            int cnt = 0, noData = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                var c = pool[i];
                if (c == null) continue;
                if (c.TriggerOps(words[w]) == null) continue;
                cnt++;
                if (c.AbilityTriggerPresence(words[w]) != 1) noData++;
            }
            total += cnt; dataLess += noData;
            brk.Append(words[w]).Append('=').Append(cnt).Append(' ');
        }
        Debug.Log("[B108_A16] S4 池内正文匹配：合计 " + total + " 处（" + brk.ToString().Trim() + "）"
                + " · 其中 **没有** 数据位匹配的 " + dataLess + " 处（= 只有 OR 才能救回来的那批）");
        CheckTrue(total >= 100,
                  "★★ S4e 池内「正文非空」的 (卡, 关键词) 合计 = " + total
                + "（`A1618` 2026-10-12 的读数是 **122**；本条钉的是**地板**：掉到 100 以下 ⇒ 正文匹配被削弱 ⇒ 红）");
        CheckTrue(dataLess > 0,
                  "★★ S4f 灭自证：其中 " + dataLess + " 处**只有正文、没有数据位匹配** ⇒ 改写成「替换」会把它们静默判死（本条要求 > 0）");
    }

    // ==================================================================
    //  S5 —— `-1 未知` 不许被读成「命中」
    // ==================================================================
    static void SectionS5_UnknownIsNotMatch()
    {
        Section("S5 灭自证：`-1 未知` ≠ `1 有`（旧表 / 夹具不许凭空命中）");

        var empty = new CardDef("B108A16Empty", "B108A16Empty", "unit", "", "common", "Test", 1, 1, 1, 0,
                                null, abilityTriggers: new int[0], abilityTriggersVersioned: true);
        Check(empty.AbilityTriggerPresence("UnitDamaged"), 0,
              "S5a 前提：`[] + versioned` ⇒ **明确没有** = 0（与 -1 分得开）");

        var unversioned = new CardDef("B108A16Unv", "B108A16Unv", "unit", "", "common", "Test", 1, 1, 1, 0,
                                      null, abilityTriggers: new[] { 60 }, abilityTriggersVersioned: false);
        Check(unversioned.AbilityTriggerPresence("UnitDamaged"), -1,
              "S5b 前提：没 versioned ⇒ **-1 未知**（即便行里塞了 60 也不算数）");

        var ctx = ProbeBattle(new[] { empty, unversioned }, new[] { Unit("B108A16S5Foe", 1, 1, 5) });
        var c1 = Place(ctx, 0, 2, Unit("B108A16S5C1", 2, 5, 5), exhausted: true);
        CheckTrue(c1 != null, "S5c 前提：载体 1 上场了");
        if (c1 == null) return;
        Enchantments.Attach(ctx, c1.Instance, empty, Enchantments.BuffTypeSingleUse);
        Check(Enchantments.Matching(c1, "UnitDamaged").Count, 0,
              "★★ S5d 明确没有（0）⇒ **不命中**");

        var c2 = Place(ctx, 0, 4, Unit("B108A16S5C2", 2, 5, 5), exhausted: true);
        CheckTrue(c2 != null, "S5e 前提：载体 2 上场了");
        if (c2 == null) return;
        Enchantments.Attach(ctx, c2.Instance, unversioned, Enchantments.BuffTypeSingleUse);
        Check(Enchantments.Matching(c2, "UnitDamaged").Count, 0,
              "★★★ S5f **未知（-1）不算命中**"
            + "｜🧨 把 `== 1` 放宽成 `!= 0` ⇒ 这一条命中 ⇒ 红（那会把所有旧表 / 夹具的卡凭空放行）");
    }

    // ==================================================================
    //  S6 —— 演出数据仍在（`hit → Pray_Exit` / `trigger → Pray_Success`）
    // ==================================================================
    static void SectionS6_VfxDataStillThere()
    {
        Section("S6 演出数据：`9218576f…` 的 `hit` 桶仍是 `Pray_Exit`（R6 的 ⑤a 复核）");

        var hit = VfxMap.ResolveAll(VfxMap.UnitDamaged, "Sororitas", Enchantments.PrayEnchantmentCardId);
        var trig = VfxMap.ResolveAll("trigger", "Sororitas", Enchantments.PrayEnchantmentCardId);
        CheckTrue(Contains(hit, "Pray_Exit"),
                  "★★ S6a `hit` 行 = `Pray_Exit`（2026-10-23 `T1` 起 `hit` 桶在运行期表里**搬进 `unit_damaged_anim`**，"
                + "所以这里查 `VfxMap.UnitDamaged`；`Resources/card_vfx_by_card.json` 的 `9218576f…`；"
                + "现读 " + Join(hit) + "）｜🧨 把那一行删掉 / 走 strip ⇒ 红");
        CheckTrue(Contains(trig, "Pray_Success"),
                  "★★ S6b `trigger` 桶 = `Pray_Success`（tg 31 归 `trigger` 桶，见 `gen_card_vfx_by_card.py` 的 `TRIGGER_EVENT`；"
                + "现读 " + Join(trig) + "）");
    }

    static bool Contains(string[] arr, string want)
    {
        if (arr == null) return false;
        for (int i = 0; i < arr.Length; i++) if (arr[i] == want) return true;
        return false;
    }

    static string Join(string[] arr)
    {
        if (arr == null || arr.Length == 0) return "（空）";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < arr.Length; i++) { if (i > 0) sb.Append(','); sb.Append(arr[i]); }
        return sb.ToString();
    }
}
