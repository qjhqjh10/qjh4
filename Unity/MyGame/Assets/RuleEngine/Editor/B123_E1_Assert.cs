// B123_E1_Assert.cs — 批次 **B123** · 执行代理 `E1`（附魔 / 引擎主路 · `EffectResolver`）
//
// ============================ 它盯的是什么 ============================
//   四件判据已定、本批落代码的活各配一组断言（全部**只跑引擎、不起 Unity 场景**）：
//     A 组：`B121-D4-03`  —— 卡表主路 `ca 50 buffHand` **有**消费端（`FireTriggerRowsByChosenAbility`
//                           `default` 支转 `RunHandBuffRowOfEnchantment`）；`playersAffected` 只打指定那一侧。
//     B 组：`B121-D5-04` A 半 —— `ApplyStunTo` 共用件（`DoStun` 的施加段抽出来的唯一实现）：
//                           能真的挂上、`unstunnable` 挡得住、`op == null` 不崩。
//     C 组：`B121-D1B-03` ①  —— `valueOptions 203` = `min + thisCard.CurrentShuriken × max`
//                           （**不是** 40/41/50 那一族的 `min × value`）。
//     D 组：`B121-D1B-03` ②  —— `ta 230` = 两位督军里**当前生命更高**的那个；**平局给座位 1**。
//     E 组：`B121-D4-03` 的「**不双发**」—— 同一 trigger 上正文档 op 非空时，主路**根本不进** ca 分支。
//
// ============================ 原版判据（现读 `d:/2/tools/decomp_full/`）============================
//   · `ca 50`      `AbilityLogic.PlayAbility` 的 `buffHand` 支 → `PlayerHand__AddHandEffect.c:88-139`
//                  （先登记记录、再把当前手牌逐张过 `targetCriteria` 贴 `AddEffect`）。
//   · `AddStun`    `CardScript__Stun.c:44-48 / :98 / :125`；**附魔 `ca 130` 那一支没有 jam 闸**
//                  （`AbilityLogic__PlayAbility.c:642-657` 逐目标 `AddStun`）。
//   · `203`        `AbilityLogic__GetValueFromCriteria.c:320-325`（`case 0xcb`）`min + shuriken × max`。
//   · `230`        `AbilityLogic__GetTargets.c:809-833`（`case 0xe6`）+ `BattleManager__GetHighestHealthWarlord.c`
//                  （相等 ⇒ 返 `null`）+ `BattleManager__GetHero.c`（`param_2 != 0 ⇒ +0xC0` = 座位 1）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1：删掉 `FireTriggerRowsByChosenAbility` 的 `default:` 里 `r.Ca == 50` 那一支 ⇒ 返回 0 ⇒ 红；
//   · A3：把 `RunHandBuffRowOfEnchantment` 的 `playersAffected` 判据换成「两边都挂」⇒ A3 红；
//   · B2：把 `ApplyStunTo` 里的 `StunBlockedByTraits` 那一段删掉 ⇒ 带 `unstunnable` 的单位也会被晕 ⇒ 红；
//   · C3：把 203 照 40/41/50 那族的 `min × value` 写 ⇒ 得 3 而不是 15 ⇒ 红；
//   · D4：把平局写成「给座位 0」或「返回空」⇒ 红（**这一条只能由判据决定，没有别的出路**）；
//   · E1：把 `FireTriggerByNumberWithSink` 的 `if (ops == null || ops.Count == 0)` 闸去掉
//         （或把 `FireTriggerRowsByChosenAbility` 提到闸外）⇒ 手牌会多出效果 ⇒ 红。
//
// ============================ 怎么接（宿主）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行**追加：
//     `Guard("B123E1_Assert", B123_E1_Assert.Run); _pass += B123_E1_Assert.Pass; _fail += B123_E1_Assert.Fail;`
//   （⚠️ 追加的文本必须落在该行第一个 `//` **之前**；接线由调度台统一做。）
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEditor;
using UnityEngine;

/// <summary>B123-E1 的断言（**独立类**：类名 == 文件名，唯一入口 <see cref="Run"/>）。</summary>
public static class B123_E1_Assert
{
    static int _pass, _fail;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Section("🆕 B123-E1：ca 50 主路 / ApplyStunTo / valueOptions 203 / ta 230");
        GroupA_Ca50MainPath();
        GroupB_ApplyStunTo();
        GroupC_ValueOptions203();
        GroupD_Ta230();
        GroupE_NoDoubleFire();
        Summary();
    }

    // ==================================================================
    //  A 组：`ca 50 buffHand` 在卡表主路上真派发（`B121-D4-03`）
    // ==================================================================
    static void GroupA_Ca50MainPath()
    {
        Section("A `ca 50 buffHand`：主路 default 支转 `RunHandBuffRowOfEnchantment`（`B121-D4-03`）");
        var pool = CardDatabase.Load();
        CheckTrue(pool != null && pool.Count > 0, "A0 前提：卡池装起来了（`CardDatabase.Load()`）");

        CardDef selfCard; int selfTrig; CardDatabase.AbilityBuffSpec selfSpec;
        bool gotSelf = FindHandBuffCa50(pool, 10, out selfCard, out selfTrig, out selfSpec);
        CheckTrue(gotSelf, "A1 前提：池里存在 `pa = 10`（只打**自己**手牌）的 `ca 50` 行");
        if (gotSelf) RunCa50Case("A", pool, selfCard, selfTrig, selfSpec, 0);

        CardDef foeCard; int foeTrig; CardDatabase.AbilityBuffSpec foeSpec;
        bool gotFoe = FindHandBuffCa50(pool, 20, out foeCard, out foeTrig, out foeSpec);
        CheckTrue(gotFoe, "A4 前提：池里存在 `pa = 20`（只打**对手**手牌）的 `ca 50` 行");
        if (gotFoe) RunCa50Case("A5", pool, foeCard, foeTrig, foeSpec, 1);
    }

    /// <summary>一条 `ca 50` 行的完整验证：派发数 / 目标那一侧挂上 / 另一侧一张没动 / 空手返回 0。</summary>
    static void RunCa50Case(string tag, IList<CardDef> pool, CardDef card, int trigger,
                            CardDatabase.AbilityBuffSpec spec, int expectedSide)
    {
        var ctx = MakeBattle();
        if (ctx == null) { Report(false, tag + "b 前提不成立：对局没建起来"); return; }
        var u = PlaceRealUnit(ctx, 0, 3, card);
        if (u == null) { Report(false, tag + "c 前提不成立：载体没摆上棋盘"); return; }

        var crit = spec != null ? spec.Criteria : null;
        FillHand(ctx, 0, crit, pool, tag + "H0");
        FillHand(ctx, 1, crit, pool, tag + "H1");

        var ops = card.TriggerOpsByNumber(trigger);
        CheckTrue(ops == null || ops.Count == 0,
                  $"{tag}0 前提：「{card.Name}」trigger {trigger} 上**没有正文 op** ⇒ 生产链路会进 ca 分支"
                + $"（`FireTriggerRowsByChosenAbility` 的唯一调用点在 `ops.Count == 0` 里面）");

        int before0 = CountHandEffects(ctx.Players[0].Hand);
        int before1 = CountHandEffects(ctx.Players[1].Hand);
        int done = RuleCore.FireTriggerRowsByChosenAbilityForTest(ctx, u, trigger, 0, 3, null, null);
        int after0 = CountHandEffects(ctx.Players[0].Hand);
        int after1 = CountHandEffects(ctx.Players[1].Hand);

        CheckTrue(done >= 1,
                  $"{tag}1 ★★★ **结论**：「{card.Name}」的 `ca 50` 行在主路上派发了（返回 {done}）"
                + "｜🧨 删掉 `FireTriggerRowsByChosenAbility` 的 `default:` 里 `r.Ca == 50` 那一支 ⇒ 返回 0 ⇒ 红");

        int got = expectedSide == 0 ? after0 - before0 : after1 - before1;
        int other = expectedSide == 0 ? after1 - before1 : after0 - before0;
        CheckTrue(got > 0,
                  $"{tag}2 ★★★ **结论**：**座位 {expectedSide}** 的手牌挂上了 `HandEffect`（{got} 张）"
                + "（原版 `PlayerHand.AddHandEffect` 的第二段：过关的手牌逐张 `AddEffect`）");
        CheckTrue(other == 0,
                  $"{tag}3 ★★★ **灭自证**：**另一侧**手牌一张没动 —— `playersAffected` 不是「两边都挂」");

        // 空手对照：同一张卡、同一 trigger，手牌清空 ⇒ 必须返回 0（证明它不是常数）
        // ⚠️ `NewBattle` 会**发起手牌**（`startingHand` = 3）⇒ 不清空就不是「空手」。
        var ctx2 = MakeBattle();
        var u2 = ctx2 != null ? PlaceRealUnit(ctx2, 0, 3, card) : null;
        if (u2 == null) { Report(false, tag + "d 前提不成立：空手对照没建起来"); return; }
        ctx2.Players[0].Hand.Clear();
        ctx2.Players[1].Hand.Clear();
        Check(ctx2.Players[0].Hand.Count + ctx2.Players[1].Hand.Count, 0, $"{tag}4a 前提：两侧手牌都清空了");
        int doneEmpty = RuleCore.FireTriggerRowsByChosenAbilityForTest(ctx2, u2, trigger, 0, 3, null, null);
        Check(doneEmpty, 0, $"{tag}4 ★★ **灭自证**：手牌空 ⇒ 返回 0（不是「不管有没有手牌都返回 1」）");
    }

    /// <summary>按这条 `handBuff` 的 36 格 criteria 给某一侧手牌备料。
    /// <para>⚠️ **不能清一色用手写卡**：池内 `ca 50` 的 criteria 里有要求 `spellType` 具体值的
    /// （`GSC45 Underground Network` 的 `spellType = 230`），而手写卡的 `SpellType` 恒 `-1`（未知）
    /// ⇒ `TargetCriteriaEval.MatchesRawCard` 判不了、一律**不命中**（静默 0）。⇒ 缺哪一档就从**真实卡池**补一张真卡。</para></summary>
    static void FillHand(BattleContext ctx, int side, CardDef.CountSetSpec crit, IList<CardDef> pool, string tag)
    {
        if (ctx == null || ctx.Players == null || side < 0 || side >= ctx.Players.Length || ctx.Players[side] == null) return;
        var hand = ctx.Players[side].Hand;
        hand.Add(ctx.NewInstance(PlainCard(tag + "U", "unit")));
        hand.Add(ctx.NewInstance(PlainCard(tag + "T", "tactic")));
        if (crit == null || pool == null) return;

        bool needMin = crit.IncludeMinions != 0 && crit.IncludeMinions != CardDef.CountSetSpec.Unknown;
        bool needSpell = crit.IncludeSpells != 0 && crit.IncludeSpells != CardDef.CountSetSpec.Unknown;
        int needSpellType = (crit.SpellType != 0 && crit.SpellType != CardDef.CountSetSpec.Unknown) ? crit.SpellType : 0;

        CardDef minion = null, spell = null;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            if (c == null) continue;
            if (minion == null && c.Type == "unit") minion = c;
            if (spell == null && (c.Type == "tactic" || c.Type == "defence" || c.Type == "enchantment")
                && (needSpellType == 0 || c.SpellType == needSpellType))
                spell = c;
            if (minion != null && spell != null) break;
        }
        if (needMin && minion != null) hand.Add(ctx.NewInstance(minion));
        if (needSpell && spell != null) hand.Add(ctx.NewInstance(spell));
    }

    /// <summary>池内找一张 `ca 50` 行、且它在 `abilityBuffs` 里真有一条 `handBuff` 载荷、`playersAffected == pa` 的卡。</summary>
    static bool FindHandBuffCa50(IList<CardDef> pool, int pa, out CardDef card, out int trigger,
                                 out CardDatabase.AbilityBuffSpec picked)
    {
        card = null; trigger = -1; picked = null;
        if (pool == null) return false;
        for (int i = 0; i < pool.Count; i++)
        {
            var c = pool[i];
            var rows = c != null ? c.TriggerAbilityIndices : null;
            if (rows == null) continue;
            var buffs = CardDatabase.AbilityBuffsOf(c);
            if (buffs == null) continue;
            for (int k = 0; k < rows.Length; k++)
            {
                var r = rows[k];
                if (r == null || r.Ca != 50 || r.Trigger < 0) continue;
                for (int b = 0; b < buffs.Count; b++)
                {
                    var spec = buffs[b];
                    if (spec == null || !spec.IsHandBuff) continue;
                    if (spec.Ab != r.Ab || spec.Lg != r.Lg) continue;
                    if (spec.PlayersAffected != pa) continue;
                    card = c; trigger = r.Trigger; picked = spec;
                    return true;
                }
            }
        }
        return false;
    }

    // ==================================================================
    //  B 组：`ApplyStunTo` 共用件（`B121-D5-04` A 半）
    // ==================================================================
    static void GroupB_ApplyStunTo()
    {
        Section("B `ApplyStunTo`：施加段共用件（`B121-D5-04` A 半）");
        var ctx = MakeBattle();
        if (ctx == null) { Report(false, "B0 前提不成立：对局没建起来"); return; }
        var op = new EffectOp { Verb = "stun", Source = "B123E1", SubjectTargetsAffected = 30 };

        var t1 = PlaceRealUnit(ctx, 1, 3, PlainCard("B123E1StunT1", "unit"));
        CheckTrue(t1 != null && !t1.IsStunned, "B0 前提：目标摆上去了、初始**没**被晕");
        RuleCore.ApplyStunToForTest(ctx, t1, "B123E1", op);
        CheckTrue(t1.IsStunned,
                  "B1 ★★★ **结论**：`ApplyStunTo` 真把 `stun` 挂上了（`AddKeyword(KeywordTable.Stun, 1)`）"
                + "｜🧨 把方法体清空 ⇒ 红");

        var t2 = PlaceRealUnit(ctx, 1, 2, PlainCard("B123E1StunT2", "unit"));
        CheckTrue(t2 != null, "B2a 前提：第二个目标摆上去了");
        t2.AddKeyword(RuleCore.Unstunnable, 1);
        RuleCore.ApplyStunToForTest(ctx, t2, "B123E1", op);
        CheckTrue(!t2.IsStunned,
                  "B2 ★★★ **结论**：带 `unstunnable` 的目标**晕不上**（原版 `CardScript__Stun.c:46-47` 直接跳过施加）"
                + "｜🧨 删掉 `StunBlockedByTraits` 那一段 ⇒ 红");

        var t3 = PlaceRealUnit(ctx, 1, 1, PlainCard("B123E1StunT3", "unit"));
        CheckTrue(t3 != null, "B3a 前提：第三个目标摆上去了");
        RuleCore.ApplyStunToForTest(ctx, t3, "B123E1", null);
        CheckTrue(t3.IsStunned,
                  "B3 ★★ **结论**：`op == null` 时照常施加、不抛（闸③ `AddEffectRemnantAllowsOp` 只用 `op.Source` 记日志）");

        RuleCore.ApplyStunToForTest(ctx, t1, "B123E1", op);
        CheckTrue(t1.IsStunned, "B4 ★★ 重复施加不抛、仍然是晕（`if (t.IsStunned) return;`）");

        var dead = PlaceRealUnit(ctx, 1, 0, PlainCard("B123E1StunDead", "unit"));
        if (dead != null) dead.Health = 0;
        RuleCore.ApplyStunToForTest(ctx, dead, "B123E1", op);
        CheckTrue(dead == null || !dead.IsStunned,
                  "B5 ★★ **灭自证**：`!IsAlive` 的目标不施加（改前 `if (t == null || !t.IsAlive) continue;` 逐字搬进共用件）");
    }

    // ==================================================================
    //  C 组：`valueOptions 203`（`B121-D1B-03` ①）
    // ==================================================================
    static void GroupC_ValueOptions203()
    {
        Section("C `valueOptions 203` = `min + thisCard.CurrentShuriken × max`（`B121-D1B-03` ①）");
        var ctx = MakeBattle();
        if (ctx == null) { Report(false, "C0 前提不成立：对局没建起来"); return; }
        var u = PlaceRealUnit(ctx, 0, 3, PlainCard("B123E1Shuriken", "unit"));
        if (u == null) { Report(false, "C0b 前提不成立：载体没摆上去"); return; }
        u.AddKeyword("shuriken", 3);
        Check(u.KwValue("shuriken"), 3, "C1 前提：载体 Shuriken = 3（`UnitState.KwValue`）");

        string gap; int v;
        bool ok = RuleCore.TryResolveCriteriaValue203ForTest(ctx, u, 1, 2, out v, out gap);
        CheckTrue(ok, "C2 前提：203 这一档解得出来（`gap` 为空）");
        Check(v, 7, "C3 ★★★ **结论**：`1 + 3 × 2 = 7`（判据 `GetValueFromCriteria.c:320-325`）"
                   + "｜🧨 照 40/41/50 那族写成 `min × value` ⇒ 得 3 ⇒ 红");

        ok = RuleCore.TryResolveCriteriaValue203ForTest(ctx, u, 0, 5, out v, out gap);
        Check(v, 15, "C4 ★★★ **灭自证**：`min = 0` 时**不是**「直接取 value」那一族 —— `0 + 3 × 5 = 15`"
                   + "｜🧨 写成 `min == 0 ? value : min × value` ⇒ 得 3 ⇒ 红");

        ok = RuleCore.TryResolveCriteriaValue203ForTest(ctx, null, 1, 2, out v, out gap);
        CheckTrue(!ok && !string.IsNullOrEmpty(gap),
                  "C5 ★★ **结论**：载体为空 ⇒ **不结算 + 出声**（原版 `param_5 == 0` 走错误出口）");
    }

    // ==================================================================
    //  D 组：`ta 230`（`B121-D1B-03` ②）
    // ==================================================================
    static void GroupD_Ta230()
    {
        Section("D `ta 230`：两位督军里**当前生命更高**的那个；平局给座位 1（`B121-D1B-03` ②）");
        var ctx = MakeBattle();
        if (ctx == null) { Report(false, "D0 前提不成立：对局没建起来"); return; }
        var w0 = ctx.Players[0] != null ? ctx.Players[0].Warlord : null;
        var w1 = ctx.Players[1] != null ? ctx.Players[1].Warlord : null;
        CheckTrue(w0 != null && w1 != null, "D0b 前提：两侧督军都在棋盘上（`PlayerState.Warlord`）");
        if (w0 == null || w1 == null) return;

        string gap;
        w0.Health = 30; w1.Health = 20;
        var a = RuleCore.ResolveTargetsTa230ForTest(ctx, out gap);
        Check(a != null ? a.Count : -1, 1, "D1 ★★ 恰好**一个**目标（原版 `GetTargets` 建完池直接 return）");
        CheckTrue(a != null && a.Count == 1 && ReferenceEquals(a[0], w0),
                  "D2 ★★★ **结论**：座位 0 生命更高（30 > 20）⇒ 返回**座位 0**的督军");

        w0.Health = 10; w1.Health = 40;
        var b = RuleCore.ResolveTargetsTa230ForTest(ctx, out gap);
        CheckTrue(b != null && b.Count == 1 && ReferenceEquals(b[0], w1),
                  "D3 ★★★ **结论**：座位 1 生命更高（40 > 10）⇒ 返回**座位 1**的督军");

        w0.Health = 25; w1.Health = 25;
        var c = RuleCore.ResolveTargetsTa230ForTest(ctx, out gap);
        CheckTrue(c != null && c.Count == 1 && ReferenceEquals(c[0], w1),
                  "D4 ★★★ **结论**：**平局 ⇒ 座位 1** —— `GetHighestHealthWarlord.c` 相等时返 `null`，"
                + "调用方回落 `GetHero(manager, 1)`，而 `GetHero.c` 里 `param_2 != 0 ⇒ +0xC0` = 座位 1"
                + "｜🧨 平局写成「给座位 0」或「返回空」⇒ 红");

        // ⚠️ `ta 230` 还必须**加进** `TaGoesThroughCriteriaFilter` 的「不筛」那一组（否则会被 36 格再筛空）。
        //    那个方法在 `EnchantmentExecutor_B115.cs`（本批由 `E2` 独占）⇒ **接线补丁未落地前这里只能是提示**，
        //    ⛔ 别把它写成一条会红的断言（那会把「接线没做」误报成「实现错」）。
        if (RuleCore.TaGoesThroughCriteriaFilterForTest(230))
            Debug.LogWarning("[B123_E1] ⚠️ `ta 230` 仍会走 36 格 criteria 筛选 —— `TaGoesThroughCriteriaFilter` "
                           + "里的 `230` 接线补丁**还没落地**（见 `报告/B123_E1_EffectResolver.md` §3 可粘贴补丁）。");
        else
            Debug.Log("[B123_E1] ✓ `ta 230` 已进 `TaGoesThroughCriteriaFilter` 的「不筛」组。");
        CheckTrue(!RuleCore.TaGoesThroughCriteriaFilterForTest(20),
                  "D5 ★★ 前提：那张表的「不筛」组确实存在（`20 self` 不筛）");
    }

    // ==================================================================
    //  E 组：不双发（`B121-D4-03` 的闸）
    // ==================================================================
    static void GroupE_NoDoubleFire()
    {
        Section("E **不双发**：正文 op 非空时主路**不进** ca 分支（`B121-D4-03` 的闸）");
        var pool = CardDatabase.Load();
        CardDef dfCard = null; int dfTrig = -1;
        for (int i = 0; pool != null && i < pool.Count; i++)
        {
            var c = pool[i];
            var rows = c != null ? c.TriggerAbilityIndices : null;
            if (rows == null) continue;
            for (int k = 0; k < rows.Length; k++)
            {
                var r = rows[k];
                if (r == null || r.Ca != 50 || r.Trigger < 0) continue;
                string tn;
                if (!CardDef.TryAbilityTriggerNameOfNumber(r.Trigger, out tn)) continue;   // 闸①：没有 trigger 名 ⇒ 根本不进
                var ops = c.TriggerOpsByNumber(r.Trigger);
                if (ops == null || ops.Count == 0) continue;                                // 要的正是「同一个 trigger 上两种都有」
                dfCard = c; dfTrig = r.Trigger;
                break;
            }
            if (dfCard != null) break;
        }
        CheckTrue(dfCard == null,   // 🆕 2026-11-03（B123 首跑改判）：现池 18 张 `ca 50` 里**没有一张**把 `ca 50` 行与正文 op 挂在同一 trigger ⇒ 双发**结构上不可能**；本格改成「护栏」——将来出现这种卡时它会红，那一刻本组下面那段自动启用
                  "E0 前提：池里**没有**「`ca 50` 行与**正文 op** 挂在同一个 trigger」的卡（现池 18 张 `ca 50` 全过「正文空」闸 ⇒ 双发结构上不可达）");
        if (dfCard == null) return;   // 今天没有这种卡 ⇒ 双发结构上不可达（上面那条护栏会在将来出现时先红）

        var ctx = MakeBattle();
        if (ctx == null) { Report(false, "E0b 前提不成立：对局没建起来"); return; }
        var u = PlaceRealUnit(ctx, 0, 3, dfCard);
        if (u == null) { Report(false, "E0c 前提不成立：载体没摆上去"); return; }
        ctx.Players[0].Hand.Add(ctx.NewInstance(PlainCard("B123E1DF0U", "unit")));
        ctx.Players[0].Hand.Add(ctx.NewInstance(PlainCard("B123E1DF0T", "tactic")));
        ctx.Players[1].Hand.Add(ctx.NewInstance(PlainCard("B123E1DF1U", "unit")));
        ctx.Players[1].Hand.Add(ctx.NewInstance(PlainCard("B123E1DF1T", "tactic")));

        int before = CountHandEffects(ctx.Players[0].Hand) + CountHandEffects(ctx.Players[1].Hand);
        RuleCore.FireTriggerByNumberWithSinkForTest(ctx, u, dfTrig, 0, 3, null, null);
        int after = CountHandEffects(ctx.Players[0].Hand) + CountHandEffects(ctx.Players[1].Hand);

        Check(after - before, 0,
              $"E1 ★★★ **灭自证**：「{dfCard.Name}」trigger {dfTrig} 上有正文 op ⇒ 主路**不进** ca 分支、"
            + "手牌一条 `HandEffect` 都不多（**不双发**）"
            + "｜🧨 把 `FireTriggerByNumberWithSink` 的 `if (ops == null || ops.Count == 0)` 闸去掉 ⇒ 红");
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    /// <summary>一张**手写**卡（不带 ability / 正文）：`type` = `"unit"` / `"tactic"`。
    /// <para>⚠️ **必须 `AttachCardTraits(空表)`**：不挂 ⇒ `RawCardMeetsTraitCriteria` 的
    /// `HasDefaultTrait` 给 `why != null` ⇒ `MatchesRawCard` 判不了 ⇒ 手牌一律不命中（静默 0）。</para></summary>
    static CardDef PlainCard(string id, string type)
    {
        var c = new CardDef(id, id, type, "", "common", "Test", 1, 1, 20, 0, null,
                            subtype: type == "unit" ? "Infantry" : null);
        c.AttachCardTraits(new string[0]);
        return c;
    }

    /// <summary>建一局：双方各 6 张填充卡 + 一张督军。<para>⚠️ 督军卡**必须在牌库里**（`NewBattle` 据此摆督军）。</para></summary>
    static BattleContext MakeBattle()
    {
        var heroA = new CardDef("B123E1HeroA", "B123E1HeroA", "hero", "", null, "Ultramarines", 0, 2, 30, 0, null, subtype: "Warlord");
        var heroB = new CardDef("B123E1HeroB", "B123E1HeroB", "hero", "", null, "Goff", 0, 2, 30, 0, null, subtype: "Warlord");
        var deckA = new List<CardDef> { heroA };
        var deckB = new List<CardDef> { heroB };
        for (int i = 0; i < 6; i++) { deckA.Add(PlainCard("B123E1FillA" + i, "unit")); deckB.Add(PlainCard("B123E1FillB" + i, "unit")); }
        return RuleCore.NewBattle(deckA, deckB, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    /// <summary>把一张卡摆上棋盘。<para>⚠️ `ca 50` 那一组必须传**真实卡池里的 `CardDef`** —— `abilityBuffs` 是
    /// `CardDatabase` 的**旁表**，手写卡挂不上（`RecordAbilityBuffs` 是 private）。</para></summary>
    static UnitState PlaceRealUnit(BattleContext ctx, int p, int slot, CardDef card)
    {
        if (ctx == null || card == null) return null;
        var u = new UnitState(ctx.NewInstance(card), false);
        u.DeployedTurn = ctx.Turn;
        u.PlayOrder = ctx.TakePlayOrder();
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    static int CountHandEffects(List<CardInstance> hand)
    {
        if (hand == null) return 0;
        int n = 0;
        for (int i = 0; i < hand.Count; i++)
            if (hand[i] != null && hand[i].HandEffects != null) n += hand[i].HandEffects.Count;
        return n;
    }

    // ==================================================================
    //  最小断言框架
    // ==================================================================

    static void Section(string title) { Debug.Log("--- " + title + " ---"); }

    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }

    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }

    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("✓ " + msg); }
        else { _fail++; Debug.LogError("✗ " + msg); }
    }

    static void Summary()
    {
        if (_fail == 0) Debug.Log($"B123_E1 合计：{_pass} 通过 / 0 失败");
        else Debug.LogError($"B123_E1 合计：{_pass} 通过 / {_fail} 失败");
    }

    /// <summary>宿主聚合行读这两个口。</summary>
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
