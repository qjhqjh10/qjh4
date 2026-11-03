// B121_E03F_DATA_Assert.cs — 执行写手 `B121_E03F_DATA`（第一百二十一会话）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `B121-E03F` 的数据/效果那一半：**13 张变身战术卡**（`TRANSFORM_<FACTION>`，13 阵营各 1 张）。
//   用户 2026-11-03 裁定：每阵营一张 · 随机变**同阵营、非当前**的另一位督军 · **允许加入卡组**
//   （否则无法测试）。卡与效果**全部是 🔶 自己设计（非原版）**（原版没有这张卡，见
//   `RuleEngine/Core/RuleCorePartial_B96CB.cs` 文件末「变身战术卡」那一大段）。
//
//   本件钉七件事：① 13 张都在池、**可进卡组**、卡池可见、四围与费用符合规格
//                ② 效果**换到同阵营、非当前**的那一位
//                ③ 随机**可复现**（同一个 seed ⇒ 同一个结果；且证明它真的走 `ctx.Rng`）
//                ④ **无候选 ⇒ 空放 + 出声 + 不崩**（不许静默、也不许抛）
//                ⑤ 反证：普通卡不触发；没打出那张卡时督军不动
//                ⑥ 立绘别名表（`CardArt.PortraitKeyAlias`）↔ 数据 13/13 一一对应（无多余、无缺失、同阵营、目标真有画）
//                ⑦ `DeckBuilder.TacticPlayable` 13/13 = true（能进卡组 / 自动组牌不跳过）且卡面**不打「未实现」星**
//
// ============================ 判据（路径，⛔ 别在这里抄第二份）============================
//   · 卡数据形状 = `工具/gen_cards_engine.py` 的 `transform_card_entry`（唯一出处）
//   · 效果码 / 谁能换 / 怎么换 = `RuleEngine/Core/RuleCorePartial_B96CB.cs`
//     （`WarlordTransformAbilityCode` / `WarlordTransformCandidates` / `ResolveWarlordTransform`）
//   · 卡面动词入口 = `EffectResolver.TacticOpsOf`（`CanPlayTactic` 与 `PlayTacticCore` 共用）
//   · 演出不归本件（执行写手 `B121_E03F_VFX` · `B121_E03F_VFX_Assert`）
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//   🧨 1：A 组「13 张都可进卡组」与 A 组「`inventoryOptions` 不是 5 / `NotDeckable` 不置位」是**同一批卡的两个面**
//         —— 把 `inventoryOptions` 落成 5（那是「方向反了」的那一档）⇒ `CannotAddToDeck` 变真 ⇒ A 红。
//   🧨 2：B 组「换到同阵营非当前」配 C 组「pick 必须等于 `ctx.Rng.Next(2)` 指向的那一位」——
//         把随机换成「取第一个候选」⇒ C 红（B 仍绿）；把 `Rng.Next` 换成 `UnityEngine.Random` ⇒ C 红。
//   🧨 3：C 组「1 个候选时**不**推进随机流」配「2 个候选时**推进**」—— 两个方向的数都钉住了，
//         把 `candidateCount == 1` 那一支删掉（一律掷骰）⇒ C 的前半红。
//   🧨 4：D 组「无候选 ⇒ 返回 false」配「返回 false 时督军**一个字没变**」——
//         把无候选那一支改成「随便挑一张」或「抛异常」⇒ D 红。
//   🧨 5：F 组「表恰好 13 条」配「13 张全命中 + 表的键一个不多」—— 少一条（那张卡退回占位图）/ 多一条（静默改写真卡立绘）都会红；
//         且 F3e「别名目标真有立绘」把「映射到一张没画的卡」也钉住（那时 13 条仍在表里，只有本行红）。
//   🧨 6：G 组「卡面正文 `IsFullyParsed` 恒假」是**前提断言** —— 撤掉 `DeckBuilder.cs:454` 那条例外 ⇒ `TacticPlayable` 红
//         （证明这个 true 是例外撑起来的，不是正文恰巧解析得出来）。
//
// ============================ 怎么接 ============================
//   宿主聚合行末尾追加（**同一物理行** ⇒ 行数零变化）：
//      B121_E03F_DATA_Assert.Run(); pass += B121_E03F_DATA_Assert.Pass; fail += B121_E03F_DATA_Assert.Fail;
//   ⚠️ **零前提**：本件自己 `RuleCore.NewBattle` 建局 + 自备 `ctx.CardPool`；
//      **不建 HUD、不跑帧、不碰表现层** ⇒ 哪个宿主都能接（也可以单独挂 `RuleEngineTest`）。
using System;
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;

/// <summary>执行写手 `B121_E03F_DATA`：`B121-E03F` 的 13 张变身战术卡（数据 / 可进卡组 / 效果 / 可复现 / 空放）。</summary>
public static class B121_E03F_DATA_Assert
{
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    const string P = "[B121E03FDATA] ";

    /// <summary>13 个阵营（与 `工具/gen_cards_engine.py` 的 `TRANSFORM_FACTIONS` 同序同值）。</summary>
    static readonly string[] Factions =
    {
        "AstraMilitarum", "BlackLegion", "DarkAngels", "EmperorsChildren", "Genestealers",
        "Goff", "Leviathan", "SaimHann", "Sautekh", "Sororitas", "SpaceWolves",
        "TauEmpire", "Ultramarines",
    };

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    static void Eq(int got, int want, string msg)
    {
        Check(got == want, msg + (got == want ? "" : $" —— 期望 [{want}]，实得 [{got}]"));
    }

    public static void Run()
    {
        _pass = _fail = 0;
        Debug.Log(P + "=== B121_E03F 数据/效果 开始（🔶 自己设计（非原版）· 用户 2026-11-03 裁定）===");
        try
        {
            GroupA_CardData();
            GroupB_Swap();
            GroupC_Random();
            GroupD_NoCandidate();
            GroupE_Negative();
            GroupF_PortraitAlias();
            GroupG_PlayableNoStar();
        }
        catch (Exception e)
        {
            _fail++;
            Debug.LogError(P + "✗ 未捕获异常（本组余下的断言不会执行）：" + e);
        }
        finally
        {
            Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
        }
    }

    // ==================================================================
    //  A · 13 张卡在池、可进卡组、卡池可见
    // ==================================================================
    static void GroupA_CardData()
    {
        Debug.Log(P + "--- A · 13 张变身战术卡（数据 / 可进卡组 / 卡池可见）---");
        var pool = CardDatabase.Load();
        Check(pool != null && pool.Count > 0, "[A0 前提] 卡池装得起来");
        if (pool == null) return;

        int found = 0;
        for (int i = 0; i < Factions.Length; i++)
        {
            string id = "TRANSFORM_" + Factions[i];
            var c = CardDatabase.FindById(pool, id);
            Check(c != null, $"★ A1 「{id}」在池里｜🧨 生成器没落/被别的卡挤掉 ⇒ 本行红");
            if (c == null) continue;
            found++;

            Check(c.Type == "tactic", $"A2 「{id}」类型 = tactic（实得 {c.Type}）");
            Eq(c.Cost, 2, $"A2b 「{id}」费用 = 2");
            Check(c.Attack == 0 && c.Health == 0 && c.RangedAttack == 0 && c.ArmourFromPool == 0,
                  $"A2c 「{id}」四围全 0（攻 {c.Attack} / 血 {c.Health} / 远程 {c.RangedAttack} / 甲 {c.ArmourFromPool}）");
            Check(string.Equals(c.Faction, Factions[i], StringComparison.Ordinal),
                  $"A2d 「{id}」阵营 = {Factions[i]}（实得 {c.Faction}）");

            // ---- 可进卡组：三格一起断（缺一格就会「看得见但加不进」或「方向反了」）----
            Eq(c.InventoryOptions, 0, $"★ A3 「{id}」`inventoryOptions` = 0 InInventory（⛔ 不是 5 CantAddToDeck —— 那是方向反了的那一档）");
            Eq(c.ShowInCollectionOptions, 5, $"★ A3b 「{id}」`showInCollectionOptions` = 5 Show（卡池可见）");
            Check(!c.NotDeckable, $"★ A3c 「{id}」`NotDeckable` **不置位**（用户 2026-11-03 改口：允许加入卡组）");
            Check(!DeckRules.CannotAddToDeck(c), $"★ A3d 「{id}」`DeckRules.CannotAddToDeck` = false（卡组编辑器真的能加）");
            Check(!c.IsEnchantment && !c.IsUnit, $"A3e 「{id}」不是附魔模板、不是单位（`CanPlayTactic` 那条路才认）");

            // ---- 效果码载体 ----
            Check(RuleCore.HasWarlordTransformAbility(c), $"★ A4 「{id}」`HasWarlordTransformAbility` = true（`chosenAbility` 含 900）");
            var ops = RuleCore.TacticOpsOf(c);
            Check(ops != null && ops.Count == 1 && ops[0].Verb == "warlordtransform",
                  $"★ A4b 「{id}」的 op 流 = **一条** `warlordtransform`"
                + (ops == null ? "（实得 null）" : $"（实得 {ops.Count} 条 / 首条 `{(ops.Count > 0 ? ops[0].Verb : "-")}`）"));

            // ---- 名字 / 描述中英都有（卡面无字 = 玩家看不到这张卡在干什么）----
            Check(!string.IsNullOrEmpty(c.Name) && !string.IsNullOrEmpty(c.NameZh),
                  $"A5 「{id}」中英卡名都有（{c.Name} / {c.NameZh}）");
            Check(!string.IsNullOrEmpty(c.Desc) && !string.IsNullOrEmpty(c.DescZh),
                  $"A5b 「{id}」中英效果文字都有");
        }
        Eq(found, Factions.Length, $"★ A6 池里 13 个阵营的变身卡**一张不少**（实得 {found}/{Factions.Length}）");

        // 全池扫一遍：**只有**这 13 张带效果码（多一张就说明有别的卡被误标）
        int marked = 0;
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && RuleCore.HasWarlordTransformAbility(pool[i])) marked++;
        Eq(marked, Factions.Length, "★ A7 全池带变身效果码的卡 **恰好 13 张**｜🧨 别的卡被误标 ⇒ 本行红（会凭空多出「打出就变身」）");
    }

    // ==================================================================
    //  B · 效果：换成**同阵营、非当前**的那一位
    // ==================================================================
    static void GroupB_Swap()
    {
        Debug.Log(P + "--- B · 换到同阵营 / 非当前 ---");
        var ctx = RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 0, shuffle: false);
        Check(ctx != null && ctx.Players[0].Warlord != null, "[B0 前提] 建局拿到督军");
        if (ctx == null || ctx.Players[0].Warlord == null) return;

        var cur = ctx.Players[0].Warlord.Card;
        string curId = cur != null ? cur.Id : null;
        string faction = cur != null ? cur.Faction : null;
        ctx.CardPool = CandidatePool();                       // 2 个同阵营 + 1 个别阵营 + 当前这位

        var cands = RuleCore.WarlordTransformCandidates(ctx, 0);
        Eq(cands.Count, 2, "★ B1 候选 = **2**（同阵营里除了当前这位；别阵营那位不算）");
        for (int i = 0; i < cands.Count; i++)
        {
            Check(string.Equals(cands[i].Faction, faction, StringComparison.Ordinal),
                  $"B1b 候选「{cands[i].Id}」同阵营（{cands[i].Faction}）");
            Check(!string.Equals(cands[i].Id, curId, StringComparison.Ordinal),
                  $"B1c 候选「{cands[i].Id}」**不是**当前这位（{curId}）");
        }
        Check(cands.Count == 2 && string.CompareOrdinal(cands[0].Id, cands[1].Id) < 0,
              "B1d 候选按 `Id` 序（与池子遍历顺序无关 ⇒ 可复现的前提）");

        CardDef picked; int n; string why;
        bool ok = RuleCore.ResolveWarlordTransform(ctx, 0, out picked, out n, out why);
        Check(ok, $"★ B2 换人成功（{why ?? "无失败原因"}）");
        Eq(n, 2, "B2b 回填的候选数 = 2");
        Check(picked != null && ctx.Players[0].Warlord != null && ctx.Players[0].Warlord.Card != null
              && string.Equals(ctx.Players[0].Warlord.Card.Id, picked.Id, StringComparison.Ordinal),
              "★ B2c 引擎侧**真的换了**（`Players[0].Warlord.Card` = 选中的那一位，不是空转）");
        Check(picked != null && !string.Equals(picked.Id, curId, StringComparison.Ordinal),
              "★ B2d 新督军 ≠ 旧督军");
        Check(picked != null && string.Equals(picked.Faction, faction, StringComparison.Ordinal),
              "★ B2e 新督军**同阵营**");
    }

    // ==================================================================
    //  C · 随机可复现 + 「真的走 ctx.Rng」+ 单候选不推进随机流
    // ==================================================================
    static void GroupC_Random()
    {
        Debug.Log(P + "--- C · 随机可复现 / 走 ctx.Rng / 单候选不推进随机流 ---");

        // ① 同一个 seed、同样两个候选 ⇒ 同一个结果（跑两遍）
        string a1 = PickWith(seed: 12345), a2 = PickWith(seed: 12345);
        Check(a1 != null && string.Equals(a1, a2, StringComparison.Ordinal),
              $"★ C1 同 seed 两跑结果一致（{a1} / {a2}）｜🧨 用 `UnityEngine.Random` ⇒ 本行红");

        // ② 结果**就是** `ctx.Rng.Next(2)` 指向的那一位 —— 证明随机源是 `ctx.Rng` 而不是别的流
        var ctxA = RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 777, shuffle: false);
        ctxA.CardPool = CandidatePool();
        var candsA = RuleCore.WarlordTransformCandidates(ctxA, 0);
        var ctxB = RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 777, shuffle: false);
        int idx = ctxB.Rng.Next(candsA.Count);                // 同一个 seed ⇒ 同一个流位置
        CardDef picked; int n; string why;
        bool ok = RuleCore.ResolveWarlordTransform(ctxA, 0, out picked, out n, out why);
        Check(ok && picked != null && candsA.Count == 2
              && string.Equals(picked.Id, candsA[idx].Id, StringComparison.Ordinal),
              $"★ C2 选中的那一位 = `ctx.Rng.Next(2)` = {idx} 指向的「{(candsA.Count > idx ? candsA[idx].Id : "?")}」"
            + $"（实得「{(picked != null ? picked.Id : "null")}」）｜🧨 改成「取第一个候选」或换随机源 ⇒ 本行红");

        // ③ **1 个候选 ⇒ 不掷骰**（掷了会无谓推进随机流、让后续判定漂移）
        var s1 = RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 99, shuffle: false);
        s1.CardPool = SingleCandidatePool();
        Eq(RuleCore.WarlordTransformCandidates(s1, 0).Count, 1, "C3 前提：只剩 1 个候选");
        bool ok1 = RuleCore.ResolveWarlordTransform(s1, 0, out picked, out n, out why);
        int after1 = s1.Rng.Next(5000);
        var s2 = RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 99, shuffle: false);
        int fresh = s2.Rng.Next(5000);
        Check(ok1, "★ C3a 唯一候选 ⇒ 直接换（成功）");
        Check(after1 == fresh,
              $"★ C3b 唯一候选那一支**没有推进随机流**（换人后再取 = {after1}，未换人的同 seed 局 = {fresh}）"
            + "｜🧨 无条件掷骰 ⇒ 本行红");
    }

    // ==================================================================
    //  D · 无候选：空放 + 出声 + 不崩
    // ==================================================================
    static void GroupD_NoCandidate()
    {
        Debug.Log(P + "--- D · 无候选 ⇒ 空放 / 出声 / 不崩 ---");
        var ctx = RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 5, shuffle: false);
        var before = ctx.Players[0].Warlord != null && ctx.Players[0].Warlord.Card != null
                   ? ctx.Players[0].Warlord.Card.Id : null;
        ctx.CardPool = EmptyCandidatePool();                 // 只有别阵营的督军
        Eq(RuleCore.WarlordTransformCandidates(ctx, 0).Count, 0, "D0 前提：候选 = 0");

        CardDef picked = null; int n = -1; string why = null;
        bool ok = false;
        bool threw = false;
        try { ok = RuleCore.ResolveWarlordTransform(ctx, 0, out picked, out n, out why); }
        catch (Exception e) { threw = true; Debug.LogError(P + "  异常：" + e.GetType().Name + " " + e.Message); }
        Check(!threw, "★ D1 无候选**不抛异常**｜🧨 去掉空表分支 ⇒ NRE");
        Check(!ok, "★ D2 无候选 ⇒ 返回 **false**（空放，⛔ 不是「随便挑一张」）");
        Eq(n, 0, "D2b 回填候选数 = 0");
        Check(!string.IsNullOrEmpty(why), $"★ D3 **出声**（失败原因非空：「{why}」）｜🧨 静默返回 false ⇒ 本行红");
        Check(picked == null, "D3b 什么都没选（`picked == null`）");
        Check(string.Equals(ctx.Players[0].Warlord != null && ctx.Players[0].Warlord.Card != null
                            ? ctx.Players[0].Warlord.Card.Id : null, before, StringComparison.Ordinal),
              "★ D4 督军**一个字没变**（空放不是「变成 null」）");

        // `ctx == null` / 座位越界：不崩
        Check(!RuleCore.ResolveWarlordTransform(null, 0, out picked, out n, out why) && !string.IsNullOrEmpty(why),
              "★ D5 `ctx == null` ⇒ false + 出声（不崩）");
        Check(!RuleCore.ResolveWarlordTransform(ctx, 7, out picked, out n, out why) && !string.IsNullOrEmpty(why),
              "★ D5b 座位越界（7）⇒ false + 出声（不崩）");
    }

    // ==================================================================
    //  E · 反证：普通卡不触发；没打出那张卡时督军不动
    // ==================================================================
    static void GroupE_Negative()
    {
        Debug.Log(P + "--- E · 反证 ---");
        var ctx = RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 0, shuffle: false);
        var before = ctx.Players[0].Warlord != null && ctx.Players[0].Warlord.Card != null
                   ? ctx.Players[0].Warlord.Card.Id : null;
        ctx.CardPool = CandidatePool();
        Check(string.Equals(ctx.Players[0].Warlord != null && ctx.Players[0].Warlord.Card != null
                            ? ctx.Players[0].Warlord.Card.Id : null, before, StringComparison.Ordinal),
              "★ E1 **没打出那张卡 ⇒ 督军不动**（建局不会自己变身）");

        var pool = CardDatabase.Load();
        var plain = pool != null ? CardDatabase.FindById(pool, "AM76") : null;
        Check(plain != null, "[E2 前提] 池里能取到一张普通战术卡（AM76）");
        if (plain != null)
        {
            Check(!RuleCore.HasWarlordTransformAbility(plain),
                  "★ E2 普通战术卡 `HasWarlordTransformAbility` = false（不会误触发）");
            var ops = RuleCore.TacticOpsOf(plain);
            Check(ops != null && ops.Count > 0 && ops[0].Verb != "warlordtransform",
                  $"★ E2b 普通战术卡的 op 流**没有** `warlordtransform`（首条 = `{(ops != null && ops.Count > 0 ? ops[0].Verb : "-")}`）");
        }

        // 空池：候选 0 ⇒ 也不会「凭空」换人
        var ctx2 = RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 0, shuffle: false);
        ctx2.CardPool = null;
        CardDef p = null; int n = -1; string w = null;
        Check(!RuleCore.ResolveWarlordTransform(ctx2, 0, out p, out n, out w) && n == 0,
              "★ E3 `CardPool == null` ⇒ 0 候选 + false（⛔ 不拿「牌库/空池」顶替英雄总表）");
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    static string PickWith(int seed)
    {
        var ctx = RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: seed, shuffle: false);
        ctx.CardPool = CandidatePool();
        CardDef picked; int n; string why;
        return RuleCore.ResolveWarlordTransform(ctx, 0, out picked, out n, out why) && picked != null
             ? picked.Id : null;
    }

    /// <summary>当前督军是 `FixtureWarlord`（阵营 `Test`）⇒ 候选 = 两张 `Test` 督军（⛔ 不含当前 / 别阵营）。</summary>
    static List<CardDef> CandidatePool()
    {
        return new List<CardDef>
        {
            Hero("FixtureWarlord", "Test"),        // = 当前那位（必须被排除）
            Hero("FixtureHeroA", "Test"),
            Hero("FixtureHeroB", "Test"),
            Hero("FixtureOther", "Elsewhere"),     // 别阵营（必须被排除）
        };
    }

    static List<CardDef> SingleCandidatePool()
    {
        return new List<CardDef> { Hero("FixtureWarlord", "Test"), Hero("FixtureHeroOnly", "Test") };
    }

    static List<CardDef> EmptyCandidatePool()
    {
        return new List<CardDef> { Hero("FixtureWarlord", "Test"), Hero("FixtureOther", "Elsewhere") };
    }

    static List<CardDef> FixtureDeck()
    {
        var d = new List<CardDef> { Hero("FixtureWarlord", "Test") };
        for (int i = 0; i < 20; i++) d.Add(Unit("filler" + i, 1));
        return d;
    }

    static CardDef Hero(string id, string faction)
    {
        return new CardDef(id, id, "hero", "", null, faction, 0, 2, 30, 0, null);
    }

    static CardDef Unit(string id, int cost)
    {
        return new CardDef(id, id, "unit", "", null, "Test", cost, 1, 1, 0, null);
    }

    // ==================================================================
    //  F · 立绘别名表 ↔ 13 张变身卡（一一对应 · 不漏不多 · 同阵营 · 目标真有画）
    // ==================================================================
    //  判据：表本体 = `CardPresentation/Core/CardArt.cs:1443-1459`（13 条）· 取函数 = 同文件 `:1464`。
    //  这张映射存在的理由（文件头 `:1424-1442`）：13 张变身卡是**我们自造的**、原版没有插图、
    //  本地 `art_transform_*.png` 0 张 ⇒ 不映射的话卡面走程序生成的占位图。
    //  🔴 表是**私有** static readonly（⛔ 不为了自检把它改 public）⇒ 反射取；
    //     样板 = `CardPresentation/Editor/B116W29_Assert.cs:44-49`（同一族做法）。
    static void GroupF_PortraitAlias()
    {
        Debug.Log(P + "--- F · 立绘别名表 ↔ 13 张变身卡 ---");
        var fld = typeof(CardPresentation.CardArt).GetField("_portraitAlias",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Check(fld != null, "[F0 前提] 反射拿到 `CardArt._portraitAlias`（私有静态表）");
        var table = fld != null ? fld.GetValue(null) as Dictionary<string, string> : null;
        Check(table != null && table.Count > 0, "[F0b 前提] 别名表装得起来");
        if (table == null) return;

        // 期望键**从数据侧现算**（卡池里带变身效果码的那 13 张）—— ⛔ 不抄表的副本，否则就是同义反复
        var pool = CardDatabase.Load();
        Check(pool != null && pool.Count > 0, "[F0c 前提] 卡池装得起来");
        if (pool == null) return;
        var ids = new List<string>();
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null && RuleCore.HasWarlordTransformAbility(pool[i])) ids.Add(pool[i].Id);
        Eq(ids.Count, Factions.Length, "★ F1 数据侧恰好 13 张变身卡（分母；与 A7 同源）");

        Eq(table.Count, Factions.Length,
           "★ F2 别名表恰好 13 条｜🧨 少一条 ⇒ 那张卡卡面退回占位图；多一条 ⇒ 静默改写另一张真卡的立绘");

        var idSet = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < ids.Count; i++) idSet.Add(ids[i]);

        int hit = 0;
        for (int i = 0; i < ids.Count; i++)
        {
            string id = ids[i];
            string alias;
            bool inTable = table.TryGetValue(id, out alias);
            Check(inTable, $"★ F3 「{id}」在表里｜🧨 漏一条 ⇒ 这张卡的卡面退回占位图（玩家看到白块）");
            if (!inTable) continue;
            hit++;

            Check(!string.IsNullOrEmpty(alias) && !string.Equals(alias, id, StringComparison.Ordinal),
                  $"F3b 「{id}」的别名**非空且不等于自身**（实得「{alias}」）");

            var target = CardDatabase.FindById(pool, alias);
            Check(target != null, $"★ F3c 「{id}」→ 「{alias}」在池里是**真卡**｜🧨 写成不存在的 id ⇒ 卡面没有画");
            if (target == null) continue;

            var src = CardDatabase.FindById(pool, id);
            string srcFaction = src != null ? src.Faction : null;
            Check(string.Equals(target.Faction, srcFaction, StringComparison.Ordinal),
                  $"★ F3d 「{id}」→ 「{alias}」**同阵营**（源 {srcFaction} / 目标 {target.Faction}）"
                + "｜🧨 借了别阵营的画 ⇒ 玩家看到「这张卡画的是别人家的兵」");

            // 「同阵营一张**有画**的原版卡」是表头写死的意图（`CardArt.cs:1431`）——
            // 只查 id 存在不够：池里有卡、但那张本身没导图时，玩家看到的还是占位图。
            Check(CardPresentation.CardArt.Portrait(alias) != null,
                  $"★ F3e 「{id}」→ 「{alias}」那张卡**真有立绘**（`CardArt.Portrait` 非 null）"
                + "｜🧨 映射到一张没画的原版卡 ⇒ 换成另一个白块");
        }
        Eq(hit, Factions.Length, $"★ F4 13 条**全命中**（实得 {hit}/{Factions.Length}）");

        // 反向：表里**一个多余的键都不能有** —— 多出来的键会静默改写某张真卡的立绘（表外键原样返回）
        foreach (var kv in table)
            Check(idSet.Contains(kv.Key),
                  $"★ F5 表的键「{kv.Key}」确实是一张变身卡｜🧨 多出来的键 ⇒ 它在静默篡改另一张卡的立绘");

        // 行为面：表外键**原样返回**（所有真卡与自造卡都走这条 ⇒ 引入别名不能有副作用）
        Check(string.Equals(CardPresentation.CardArt.PortraitKeyAlias("AM76"), "AM76", StringComparison.Ordinal),
              "★ F6 表外的键原样返回（AM76 → AM76 ⇒ 真卡立绘零变化）");
        Check(CardPresentation.CardArt.PortraitKeyAlias(null) == null
              && CardPresentation.CardArt.PortraitKeyAlias("") == "",
              "★ F6b `null` / 空串原样返回（不抛、不改写）");
    }

    // ==================================================================
    //  G · 13 张都能进卡组（`TacticPlayable`）+ 卡面**不打「未实现」星**
    // ==================================================================
    //  判据：例外本体 = `RuleEngine/Data/DeckBuilder.cs:454`
    //        （`return EffectText.IsFullyParsed(c.Desc) || RuleCore.HasWarlordTransformAbility(c);`）。
    //        ⚠️ 这条例外是**必须的**：变身卡的卡面是说明文字、`IsFullyParsed` 恒假 ⇒
    //        没有它的话自动组牌会整批跳过这 13 张（玩家可见的漏牌）。
    //  打星处 = `CardPresentation/Battle/BattleDriver.cs:28553`（`UnimplementedNotes(CardDef)`），
    //        tactic 那一支在 `:28576` 吃**同一条**例外（否则卡面会印「效果本版结算不了*」）。
    //  🔴 期望值来源：用户 2026-11-03 裁定「允许加入卡组」+ 上面两处例外；⛔ 不是从被测函数反算出来的。
    static void GroupG_PlayableNoStar()
    {
        Debug.Log(P + "--- G · `TacticPlayable` 13/13 + 卡面不打星 ---");
        var pool = CardDatabase.Load();
        Check(pool != null && pool.Count > 0, "[G0 前提] 卡池装得起来");
        if (pool == null) return;

        // `UnimplementedNotes` 是私有静态（样板同上：`B116W29_Assert.cs:44-46`）
        var mi = typeof(CardPresentation.BattleDriver).GetMethod("UnimplementedNotes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, null,
            new[] { typeof(CardDef) }, null);
        Check(mi != null, "[G0b 前提] 反射拿到 `BattleDriver.UnimplementedNotes(CardDef)`");

        int playable = 0, clean = 0;
        for (int i = 0; i < Factions.Length; i++)
        {
            string id = "TRANSFORM_" + Factions[i];
            var c = CardDatabase.FindById(pool, id);
            Check(c != null, $"[G1 前提] 「{id}」在池里");
            if (c == null) continue;

            // 前提断言：正文解析不出来 ⇒ 下面的 true **只能**来自 `DeckBuilder.cs:454` 那条例外
            Check(!EffectText.IsFullyParsed(c.Desc),
                  $"G1b 「{id}」卡面正文 `IsFullyParsed` = **false**（前提：它能进卡组靠 `DeckBuilder.cs:454` 的变身例外，不是正文恰巧解析得出来）");
            Check(!EffectText.IsHandTrap(c),
                  $"G1c 「{id}」不是手牌陷阱卡（`TacticPlayable` 的另一道闸不拦它）");

            bool ok = DeckBuilder.TacticPlayable(c);
            Check(ok, $"★ G2 「{id}」`DeckBuilder.TacticPlayable` = true（能进卡组 / 自动组牌不会跳过它）"
                    + "｜🧨 撤掉 `DeckBuilder.cs:454` 那条例外 ⇒ 本行红");
            if (ok) playable++;

            List<string> notes = null;
            try { if (mi != null) notes = mi.Invoke(null, new object[] { c }) as List<string>; }
            catch (Exception e) { Debug.LogError(P + "   `UnimplementedNotes` 抛了：" + e.GetType().Name + " " + e.Message); }
            Check(notes != null, $"[G3 前提] 「{id}」`UnimplementedNotes` 返回了列表（不是 null）");
            string joined = notes != null ? string.Join(" ", notes.ToArray()) : "（拿不到）";
            Check(notes != null && joined.IndexOf('*') < 0,
                  $"★ G3 「{id}」卡面**不打「未实现」星**（`UnimplementedNotes` = 「{joined}」）"
                + "｜🧨 去掉 `BattleDriver.cs:28576` 的同一条例外 ⇒ 卡面会印「效果本版结算不了*」");
            if (notes != null && joined.IndexOf('*') < 0) clean++;
        }
        Eq(playable, Factions.Length, $"★ G2b 13 张**全部可进卡组**（实得 {playable}/{Factions.Length}）");
        Eq(clean, Factions.Length, $"★ G3b 13 张**全部不打星**（实得 {clean}/{Factions.Length}）");
    }
}
