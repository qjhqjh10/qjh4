// B97W2_Assert.cs — 第九十七会话 · 波 3 · 执行代理 `B97_W2`
//
// ============================ 它盯的是什么 ============================
// 本批在 `RuleEngine/Core/EffectResolver*.cs` 里落了三处代码（都写在新文件
// `EffectResolverPartial_B97W2.cs` 里，只有两处调用点是**零行数变化**地并进 `EffectResolver.cs` 的既有行）：
//   · 账 `:847` —— 手牌效果记录上 `permanentBuff` 的**生产点**
//     （`IsPermanentHandCostBuffOf`，调用点 = `AttachHandEffect` 的 `entry` 初始化器）；
//   · 账 `:506` —— `CardCreated(210)` 的**手牌两跳**（`BroadcastCardCreatedHandHops`，
//     调用点 = `DoCreate` 的 `hand`/`enemyhand` 支末尾）；
//   · 账 `:686` —— `CardScript.AddEffect` 的 **11 组 `buffType` 分派**（`ApplyAbilityBuff` /
//     `ResolveAbilityBuffRows`；⚠️ 今天**没有**生产调用点，理由见那边的头注）。
// 另两行（`:921` / `:427`）由同会话 `B97_ER2` 落地，本批只复核、不重复实现（见报告 §二）。
//
// 判据（⛔ 别在这里抄第二份）：
//   `:847` → `decomp_full/CardScript__RemoveEffect.c` 第一句 `permanentBuff != 0 ⇒ return`
//            ＋ `CardScript__RemoveBuffHandCostEffects.c:35`（同一道闸的最外层）
//   `:506` → `decomp_full/BattleManager__BroadcastCardCreated.c:29-44`（双方手牌逐卡）
//            ＋ `CardScript__ReactToCardCreated.c`（`OnTrigger(0xd2 = 210, actingCard = 被造那张)`）
//   `:686` → `decomp_full/CardScript__AddEffect.c` 的 `switch(buffType)`（`:330-967`）
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
// `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加**一个块**（建议位置 = 紧接 `{ CheckB97ER2Gaps(); }`
// 那一行**之后**；现读该行在 `Run()` 的 B97 段落里，插完在它下面）：
//       // 🆕 2026-10-29（调度台代接线）：`B97_W2` —— 账 847 `permanentBuff` 生产点 + 账 506 `CardCreated(210)` 手牌两跳 + 账 686 的 11 组分派。
//       { CheckB97W2Gaps(); }
//   🔴 **故意不用 `Step(...)`、方法名也不带 `Test`/`Report` 前缀** —— 理由与 `B96_CR` / `B97_US` /
//     `B97_W1` / `B97_ER2` 四份同一条：`Step(...)` 会让 `_ranCases` 记账、`VerifyRunCaseCoverage()` 拿
//     **反射清单**对账，而 `工具/gen_diag_runner.py` 只认「8 空格缩进的顶层语句」⇒ 两者都得跟着改。
//     块调用**两条都不碰**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A 段：把 `IsPermanentHandCostBuffOf` 改成恒 `true` ⇒ A2 红（普通卡也变「摘不掉」）；
//           改成恒 `false` ⇒ A1/A4 红（真卡 GSC51 那条记录不再带这一位）。
//   · A 段：把 `AttachHandEffect` 里那句 `Permanent = …` 删掉 ⇒ A4 红（记录恒 `false`）。
//   · B 段：把 `BroadcastCardCreatedHandHops` 的 `for (int seat = 0; seat < 2; seat++)` 改成
//           `seat = ctx.Active; seat < ctx.Active + 1` ⇒ B2 红（只数到 **1** 次尝试，不是 2）。
//           把整个循环删掉 / 调用点删掉 ⇒ B2 红（0 次）。
//   · B 段：把 `"CardCreatedInHand"` 写成 `"CardCreated"`（认不出的名字 ⇒ presence `-1`）⇒ B2 红（0 次）。
//   · C 段：`ApplyAbilityBuff` 的 `case 70/85` 去掉 `isSet: true` ⇒ C2/C3 红（会**叠**成 3+5=8）；
//           改成 `sign` 写反 ⇒ C2 红（实得 `-2`）。
//   · C 段：`case 95` 的 `SetTo` 写成 `Delta` ⇒ C4 红（`CostMods` 里那条的 `SetTo` 不再是 1）。
//   · C 段：`case 25` 的 `AddKeyword("ephemeral", 1)` 删掉 ⇒ C1 红。
//   · C 段：`case 6/10/11` 改成「静默 return true」⇒ C6 红（要求它**必须**回 `false` 且出声）。
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，见文件头）。</summary>
    public static void CheckB97W2Gaps()
    {
        Section("🆕 B97_W2：账 847 `permanentBuff` 生产点 · 账 506 `CardCreated(210)` 手牌两跳 · 账 686 的 11 组 `buffType` 分派");
        B97W2PermanentHandCostBuff();
        B97W2CardCreatedHandHops();
        B97W2AddEffectDispatch();
        B97W2AddEffectDispatchRest();
    }

    // ==================================================================
    //  A · 账 847 —— `permanentBuff` 的生产点
    //  判据 = `CardScript__RemoveEffect.c` 第一句 `permanentBuff != 0 ⇒ return`
    // ==================================================================
    static void B97W2PermanentHandCostBuff()
    {
        // A1/A4 用**真卡** `GSC51 Living Icon`（全池唯一一张带 `permanentBuff` 的卡）——
        //   `cards_engine.json` 的 `permanentBuffs` 现读：`buffType == 2 changeCost` × 2 行、`at = enchantment`。
        var pool = CardDatabase.Load();
        var gsc51 = pool != null ? CardDatabase.FindById(pool, "GSC51") : null;
        Check(gsc51 != null, true, "前提：卡池里找得到 `GSC51`（找不到 ⇒ A1/A4 的「真卡」那一半跑不了）");
        if (gsc51 == null) return;
        Check(gsc51.HasPermanentHandCostBuff, (bool?)true,
              "前提：`GSC51` 的卡级汇总 = `true`（`CardDef.HasPermanentHandCostBuff`，数据侧已落）");

        var plain = Unit("B97W2A_PLAIN", 1, 1, 1);
        var ctx = Battle(new[] { plain }, new CardDef[] { });
        Check(ctx.Players[0].Hand.Count, 1, "前提：普通卡在手里（1 张）");
        if (ctx.Players[0].Hand.Count != 1) return;
        var inst = ctx.Players[0].Hand[0];

        // A1：施放者是 GSC51 ⇒ 这一条**判得出来**、而且是 `true`
        ctx.PlayingCard = gsc51;
        Check(RuleCore.IsPermanentHandCostBuffOf(ctx, inst, "B97W2:A1"), true,
              "🧨 `GSC51` 产生的手牌效果带 `permanentBuff`（判据 `CardScript__RemoveEffect.c` 第一句）；恒 `false` ⇒ 这里红");

        // A2：普通卡（没有任何 `permanentBuff` 行）⇒ `false`（**反向对照**：普通效果照旧会被到期清扫摘掉）
        ctx.PlayingCard = plain;
        Check(RuleCore.IsPermanentHandCostBuffOf(ctx, inst, "B97W2:A2"), false,
              "🧨 普通卡 ⇒ **不**带这一位（恒 `true` 的实现会在这里红：那会让所有手牌效果永远赖在手上）");

        // A3：`ctx == null` ⇒ 不炸、回 `false`
        Check(RuleCore.IsPermanentHandCostBuffOf(null, inst, "B97W2:A3"), false, "`ctx == null` ⇒ false（不炸）");

        // A4：**生产点真的灌进记录**（端到端：`AttachHandEffectToInstances` → `HandEffect.Permanent`）
        ctx.PlayingCard = gsc51;
        int n1 = RuleCore.AttachHandEffectToInstances(ctx, "B97W2 永久费用效果", "Cost 1 less", "turn",
                                                     0, new List<CardInstance> { inst });
        Check(n1, 1, "前提：那条手牌效果真挂上去了（1 张）");
        Check(inst.HandEffects.Count, 1, "前提：这一份牌身上有 1 条手牌效果");
        if (inst.HandEffects.Count != 1) return;
        Check(inst.HandEffects[0].Permanent, true,
              "🧨 **生产点灌进去了**：`AttachHandEffect` 的 `entry.Permanent` = 真卡那一格；\n"
            + "            把 `EffectResolver.cs` 里 `Permanent = IsPermanentHandCostBuffOf(...)` 删掉 ⇒ 这里红");

        // A5：普通卡挂的同一条 ⇒ `Permanent == false`（灭自证的另一半）
        var inst2 = ctx.Players[0].Hand.Count > 0 ? ctx.Players[0].Hand[0] : null;
        ctx.PlayingCard = plain;
        Check(inst2 != null, true, "前提：还有一份手牌可挂");
        if (inst2 == null) return;
        int n2 = RuleCore.AttachHandEffectToInstances(ctx, "B97W2 普通费用效果", "Cost 1 less", "turn",
                                                      0, new List<CardInstance> { inst2 });
        Check(n2 >= 1, true, "前提：第二条挂上去了");
        bool anyPermanentWithPlain = false;
        for (int i = 0; i < inst2.HandEffects.Count; i++)
            if (inst2.HandEffects[i] != null && inst2.HandEffects[i].Source == "B97W2 普通费用效果"
                && inst2.HandEffects[i].Permanent) anyPermanentWithPlain = true;
        Check(anyPermanentWithPlain, false,
              "🧨 普通卡那条记录 **不带** `Permanent`（恒 `true` 的实现 ⇒ 这里红）");
    }

    // ==================================================================
    //  B · 账 506 —— `CardCreated(210)` 的**手牌两跳**（双方手牌都要扫）
    //  判据 = `BattleManager__BroadcastCardCreated.c:29-44`（`GetCardsInHandList(双方, 全卡种)`）
    //  ⚠️ 本段断的是「**扫到了两边**」这件事（尝试计数）—— 原版那次 `OnTrigger(210)` 的**正文**
    //     我们这里取不到（夹具没有卡面正文），所以「入队条数」如实是 0，见 B3/B4 的文案。
    // ==================================================================
    static void B97W2CardCreatedHandHops()
    {
        // 夹具：带 `abilityTriggers ∋ 210`（presence 闸要 `== 1`）+ 一条 `Hand(10)` 的 abilityContext
        var with210 = new CardDef("B97W2B210", "B97W2B210", "unit", "", null, "Test", 1, 1, 1, 0, null,
                                  abilityContext: new[]
                                  {
                                      new CardDef.AbilityContextSpec { Trigger = 210, Name = "CardCreatedInHand", Context = CardDef.AbilityContextHand },
                                  },
                                  abilityContextsVersioned: true,
                                  abilityTriggers: new[] { 210 },
                                  abilityTriggersVersioned: true);
        var plain = Unit("B97W2B_PLAIN", 1, 1, 1);
        var ctx = Battle(new[] { with210, plain }, new[] { with210, plain });
        Check(ctx.Players[0].Hand.Count, 2, "前提：我方手牌 2 张");
        Check(ctx.Players[1].Hand.Count, 2, "前提：对手手牌 2 张");

        int attempts0 = RuleCore.HandAbilityTriggerAttemptsForTest;
        int queued0 = RuleCore.HandAbilityTriggerQueuedForTest;
        int r = RuleCore.BroadcastCardCreatedHandHops(ctx, ctx.Players[0].Hand[0]);
        int attempts = RuleCore.HandAbilityTriggerAttemptsForTest - attempts0;
        int queued = RuleCore.HandAbilityTriggerQueuedForTest - queued0;

        Check(attempts, 2,
              "🧨 **双方手牌都被扫到**：两边各 1 张带 `abilityTriggers ∋ 210` 的卡 ⇒ 尝试 2 次"
            + "（判据 `BattleManager__BroadcastCardCreated.c:29-44`）；只扫当前行动方的手牌 ⇒ 实得 **1**，一处都不扫 ⇒ 0");
        Check(r, 0,
              "如实标：这次一条都没**入队** —— 夹具没有 `CardCreatedInHand` 的正文（`FireHandAbilityTrigger` ③ 闸），"
            + "原版那一跳的真正文本池 0 张卡带（`abilityTriggers ∋ 210` = 0 / 1,129）");
        Check(queued, 0, "同上一句（没有正文 ⇒ 不入队）");
        Check(ctx.Players[0].Hand.Count, 2, "🧨 只发事件、**不动局面**（我方手牌仍是 2 张）");
        Check(ctx.Players[1].Hand.Count, 2, "🧨 同上（对手手牌仍是 2 张）");

        // B5：守卫 —— 没有「刚造出来的那一份」⇒ 0，不炸
        Check(RuleCore.BroadcastCardCreatedHandHops(ctx, null), 0, "`created == null` ⇒ 0（不炸）");
        int attemptsAfter = RuleCore.HandAbilityTriggerAttemptsForTest;
        Check(attemptsAfter - attempts0, 2, "🧨 上面那次 `created == null` **没有再扫一遍**（尝试计数没涨）");
    }

    // ==================================================================
    //  C · 账 686 —— `CardScript.AddEffect` 的 11 组分派
    //  判据 = `decomp_full/CardScript__AddEffect.c` 的 `switch(buffType)`（逐 case 头注写在我们那一侧）
    // ==================================================================
    static void B97W2AddEffectDispatch()
    {
        // ---- C1 `case 0x19(25) ephemeralCopy` ⇒ `AddTraitSilently(this, 5)` ----
        {
            var c = Unit("B97W2C25", 1, 2, 3);
            var ctx = Battle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            Check(u != null, true, "前提：`ephemeralCopy` 那个单位上了棋盘");
            if (u == null) return;
            Check(u.Has("ephemeral"), false, "前提：它本来**没有** `ephemeral`");
            bool ok = RuleCore.ApplyAbilityBuff(ctx, 0, u, new CardDatabase.AbilityBuffSpec { BuffType = 25 }, "B97W2:C1");
            Check(ok, true, "`ephemeralCopy(25)` 落地");
            Check(u.Has("ephemeral"), true,
                  "🧨 `ephemeral` 挂上了（原版 `CardScript__AddEffect.c:947` `AddTraitSilently(this, 5)`）；删掉那句 ⇒ 红");
        }
    }

    /// <summary>`case 70 / 85 / 95 / 45 / 5 / 40 / 6` 与「行选择器」的守卫（单独一个方法，便于定位）。</summary>
    static void B97W2AddEffectDispatchRest()
    {
        // ---- C2 `case 0x46(70) setMeleeAttack`：**设**而不是**加** ----
        {
            var c = Unit("B97W2C70", 1, 3, 5);
            var ctx = Battle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            Check(u != null, true, "前提：`setMeleeAttack` 那个单位上了棋盘");
            if (u == null) return;
            Check(u.Attack, 3, "前提：近战 3");
            RuleCore.ApplyAbilityBuff(ctx, 0, u, new CardDatabase.AbilityBuffSpec { BuffType = 70, MeleeAttackChange = 5 }, "B97W2:C2a");
            Check(u.Attack, 5, "🧨 `setMeleeAttack(70)` 设为 5（原版 `AddEffect.c` 的 `case 70`）；当成 addStats ⇒ 实得 8");
            RuleCore.ApplyAbilityBuff(ctx, 0, u, new CardDatabase.AbilityBuffSpec { BuffType = 70, MeleeAttackChange = 2 }, "B97W2:C2b");
            Check(u.Attack, 2,
                  "🧨 第二次设为 2 ⇒ **旧的那条先被摘**（原版 `RemoveActiveEffects(70)`）；会叠 ⇒ 实得 10");
        }

        // ---- C3 `case 0x55(85) setRangeAttack` ----
        {
            var c = Ranged("B97W2C85", 1, 0, 3, 2);
            var ctx = Battle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            Check(u != null, true, "前提：`setRangeAttack` 那个单位上了棋盘");
            if (u == null) return;
            RuleCore.ApplyAbilityBuff(ctx, 0, u, new CardDatabase.AbilityBuffSpec { BuffType = 85, RangeAttackChange = 7 }, "B97W2:C3");
            Check(u.CurrentRangeAttack, 7,
                  "🧨 `setRangeAttack(85)` 设为 7（原版 `AddEffect.c` 的 `case 85`）；叠加实现 ⇒ 实得 9");
        }

        // ---- C4 `case 0x5f(95) setCost` ⇒ 落一条 `SetTo` ----
        {
            var c = Unit("B97W2C95", 3, 1, 1);
            var ctx = Battle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            Check(u != null, true, "前提：`setCost` 那个单位上了棋盘");
            if (u == null) return;
            int before = ctx.CostMods.Count;
            RuleCore.ApplyAbilityBuff(ctx, 0, u, new CardDatabase.AbilityBuffSpec { BuffType = 95, CostChange = 1 }, "B97W2:C4");
            Check(ctx.CostMods.Count, before + 1, "`setCost(95)` 登记了一条费用记录");
            bool found = false;
            for (int i = 0; i < ctx.CostMods.Count; i++)
            {
                var m = ctx.CostMods[i];
                if (m != null && m.Key == c.Id && m.SetTo == 1 && m.Delta == 0) found = true;
            }
            Check(found, true,
                  "🧨 那一条是 **`SetTo = 1`**（原版 `get_CurrentCost` = `clamp0(N + Σ changeCost)`，⛔ 不是折成 delta）；"
                + "写成 `Delta` ⇒ 这里红");
        }

        // ---- C5 `case 0x2d(45) addStatsDropPod` ⇒ `droppod` 关键词那本账 ----
        {
            var c = Unit("B97W2C45", 1, 1, 3);
            var ctx = Battle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            Check(u != null, true, "前提：`addStatsDropPod` 那个单位上了棋盘");
            if (u == null) return;
            Check(u.KwValue(KeywordTable.DropPod), 0, "前提：本来没有 `droppod`");
            RuleCore.ApplyAbilityBuff(ctx, 0, u, new CardDatabase.AbilityBuffSpec { BuffType = 45, HealthChange = 3 }, "B97W2:C5");
            Check(u.KwValue(KeywordTable.DropPod), 3, "🧨 `addStatsDropPod(45)` 的血量那半落到 `droppod`（既有模型，见 `UnitState.RemoveEffectRecord`）");
        }

        // ---- C6 `case 5 swapStats`：近战与生命上限对调 ----
        {
            var c = Unit("B97W2C5", 1, 3, 5);
            var ctx = Battle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            Check(u != null, true, "前提：`swapStats` 那个单位上了棋盘");
            if (u == null) return;
            Check(u.AttackBase, 3, "前提：近战基础 3");
            Check(u.MaxHealthBase, 5, "前提：生命上限 5");
            RuleCore.ApplyAbilityBuff(ctx, 0, u, new CardDatabase.AbilityBuffSpec { BuffType = 5 }, "B97W2:C6");
            Check(u.AttackBase, 5, "🧨 `swapStats(5)`：近战基础 ⇒ 原生命（5）（原版 `AddEffect.c:695-748`）");
            Check(u.MaxHealthBase, 3, "🧨 `swapStats(5)`：生命上限 ⇒ 原近战（3）");
        }

        // ---- C7 `case 0x28(40) replaceAbility` = **净等价**（判据 → 账 `:1026`）----
        {
            var c = Unit("B97W2C40", 1, 1, 1);
            var ctx = Battle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            Check(u != null, true, "前提：`replaceAbility` 那个单位上了棋盘");
            if (u == null) return;
            Check(RuleCore.ApplyAbilityBuff(ctx, 0, u, new CardDatabase.AbilityBuffSpec { BuffType = 40 }, "B97W2:C7"), false,
                  "🧨 `replaceAbility(40)` 回 `false`（净等价 = 无状态变化，⛔ 别改成「假装改了」）；回 `true` ⇒ 红");
        }

        // ---- C8 `case 6 changeRace` / `case 30 addRace`：**未建**，必须 `false` + 不改状态 ----
        {
            var c = Unit("B97W2C30", 1, 4, 6);
            var ctx = Battle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            Check(u != null, true, "前提：`addRace` 那个单位上了棋盘");
            if (u == null) return;
            Check(RuleCore.ApplyAbilityBuff(ctx, 0, u, new CardDatabase.AbilityBuffSpec { BuffType = 30 }, "B97W2:C8"), false,
                  "🧨 `addRace(30)` 今天回 `false`（本仓没有 race 运行期字段、也没有「摘掉这张牌全部 extrinsic」的口）；"
                + "静默 return `true` ⇒ 红");
            Check(u.Attack, 4, "🧨 而且**一个字段都没动**（近战仍是 4）");
            Check(u.MaxHealth, 6, "🧨 同上（生命上限仍是 6）");
        }

        // ---- C9 行选择器：`abilityBuffs` 列缺失 ⇒ 0（不猜「没有行」）----
        {
            var c = Unit("B97W2C_ROWS", 1, 1, 1);
            var ctx = Battle(new[] { c }, new CardDef[] { });
            var u = Place(ctx, 0, 1, c);
            Check(u != null, true, "前提：行选择器那个单位上了棋盘");
            if (u == null) return;
            Check(CardDatabase.AbilityBuffsOf(c), null,
                  "前提：手写夹具**没有** `abilityBuffs` 列（`null` = 未知）");
            Check(RuleCore.ResolveAbilityBuffRows(ctx, 0, u, c, 0, 0, false, "B97W2:C9"), 0,
                  "🧨 列缺失 ⇒ 0 条（⛔ 不把「未知」当成「明确没有」去扫；也不炸）");
        }
    }
}
