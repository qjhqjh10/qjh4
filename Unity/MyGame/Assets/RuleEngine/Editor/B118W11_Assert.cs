// B118W11_Assert.cs — 执行写手 `B118_W11`（2026-11-02 · 第一百一十八会话 · 引擎域五件）
//
// ============================ 它盯的是什么 ============================
//  A. `B117-T07` 剩余 —— `RuleCore.Mulligan` 从「整批换完再一次性补抽」改成**逐张**：
//     摘一张 → 回牌库**表尾** → 补抽一张 → **原位**放回（原版 `PlayerHand._FinishMulligan_d__52__MoveNext.c`
//     逐张 `ReplaceMulliganCard` → `GetMulliganCard` → `MoveCardToMulliganPos(同一个下标)`）。
//     判据 → `资料/完全复刻/判据/B117-G10-17_换牌Mulligan.md` §二·A / §〇·4。
//  B. `B117-T13`④ —— `RuleCore.Forfeit` 的**战后重判门**（原版 `shouldLoseIfDisconnectEnd`）：
//     默认 `false` ⇒ 改前行为（战后重入被挡）；`postBattleRejudge: true` ⇒ 允许重判为负。
//     判据 → `d:/2/tools/decomp_full/BattleManager__DeadHero.c:109-121`。
//  C. `D118-09` —— 战术正文 / 触发路现在也调 `AttachAbilityTargets`；本文件钉的是那条
//     **取舍闸** `RuleCore.TriggerAbilityTargetCriteria`（只接多目标档 · 截断档不接 ⇒ 不烧随机数）。
//     判据 → `RuleEngine/Core/RuleCore.cs` 类尾那段注释块。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A3：把换牌改回「整批摘 → 整批回库 → 一次性补抽」⇒ 补到的两张落在**手牌末尾**而不是原下标 ⇒ A3 红。
//  · A4：把 `Deck.Insert(0, …)` 改回 `Deck.Add(…)`（= 加到牌堆顶）⇒ 当次就把换掉的牌抽回来 ⇒ A4 红。
//  · B2 与 B3 **互相锁死**：把门写死为「恒放行」⇒ B2 红；写死为「恒挡」⇒ B3 红。
//  · B4：把 `postBattleRejudge` 的默认值改成 `true` ⇒ B4 红（不带第 4 个实参必须仍被挡）。
//  · C2/C3：把闸放宽成「只要 `ta ∈ {0,10,110,140,150}` 就接」⇒ 截断档也被接 ⇒ C3 红；
//           把闸收窄成「恒 null」⇒ C1 红。（🔴 2026-11-03 `B124-W7` 订正：`ta=0 any` 已合法且**不看 `maxTargets`** —— 判据 `AbilityLogic__GetTargets.c:884-891` · 实现 `RuleCore.cs:18452-18453`）
//
// ============================ 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）============================
//   建议宿主 = `RuleEngineTest.Run`（纯引擎夹具；C 段用 `CardDatabase.Load()`，与 `RuleEngineTest` 里
//   `CardDatabase.Find(...)` 那几处同一条路）。
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod RuleEngineTest.Run -logFile -
//
//   判绿红：`grep "^\[B118W11\]"` 的**合计行**；逐条按**行首** `✓` / `✗` 数（⛔ 别 `grep -c ✗`）。
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

/// <summary>执行写手 `B118_W11`：换牌逐张（`B117-T07`）· 战后重判门（`B117-T13`④）· 触发路目标表取舍闸（`D118-09`）。</summary>
public static class B118W11_Assert
{
    const string P = "[B118W11] ";

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get; private set; }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get; private set; }

    static void Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); }
        else { Fail++; Debug.LogError(P + "   ✗ " + msg); }
    }

    static void Sec(string t) { Debug.Log(P + "── " + t); }

    // ============================================================ 夹具

    static CardDef Hero(string id) { return new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0); }
    static CardDef Troop(string id) { return new CardDef(id, id, "unit", "", "common", "Test", 1, 2, 5, 0); }

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B118W11Warlord0") };
        var d1 = new List<CardDef> { Hero("B118W11Warlord1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false);
    }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        Sec("B118_W11：换牌逐张 · 战后重判门 · 触发路目标表取舍闸");
        try { SectionA_Mulligan(); }
        catch (System.Exception e) { Ok(false, "A 段抛异常（后面没跑到，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message); }
        try { SectionB_ForfeitPostBattle(); }
        catch (System.Exception e) { Ok(false, "B 段抛异常（后面没跑到，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message); }
        try { SectionC_TriggerTargetCriteria(); }
        catch (System.Exception e) { Ok(false, "C 段抛异常（后面没跑到，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message); }
        Debug.Log(P + "==== 合计：✓ " + Pass + " · ✗ " + Fail + " ====");
    }

    // ============================================================ A · 换牌逐张 + 原位补抽

    static void SectionA_Mulligan()
    {
        Sec("A · `RuleCore.Mulligan` 逐张（摘 → 回库表尾 → 补抽 → **原位**放回）");
        var ctx = NewCtx();
        var ps = ctx.Players[0];
        ps.Hand.Clear(); ps.Deck.Clear();
        ctx.MulliganOpen = true;

        // 手牌 = A B C D（下标 0..3）；要换的是下标 1(B) 与 3(D)
        var hA = ctx.NewInstance(Troop("B118W11_A"));
        var hB = ctx.NewInstance(Troop("B118W11_B"));
        var hC = ctx.NewInstance(Troop("B118W11_C"));
        var hD = ctx.NewInstance(Troop("B118W11_D"));
        ps.Hand.Add(hA); ps.Hand.Add(hB); ps.Hand.Add(hC); ps.Hand.Add(hD);
        // 牌库：`Draw` 取 `Count-1` = 顶 ⇒ 先抽 z4、后抽 z3
        var z1 = ctx.NewInstance(Troop("B118W11_z1")); var z2 = ctx.NewInstance(Troop("B118W11_z2"));
        var z3 = ctx.NewInstance(Troop("B118W11_z3")); var z4 = ctx.NewInstance(Troop("B118W11_z4"));
        ps.Deck.Add(z1); ps.Deck.Add(z2); ps.Deck.Add(z3); ps.Deck.Add(z4);

        Ok(ctx.MulliganOpen && ps.Hand.Count == 4 && ps.Deck.Count == 4,
           "A1 （前提）夹具摆好：换牌开着 · 手牌 4 · 牌库 4");

        int n = RuleCore.Mulligan(ctx, 0, new List<int> { 1, 3 });
        Ok(n == 2, "A2 换掉 2 张（实得 " + n + "）");
        Ok(ps.Hand.Count == 4, "A3a 手牌张数不变（实得 " + ps.Hand.Count + "）");
        Ok(ReferenceEquals(ps.Hand[0], hA) && ReferenceEquals(ps.Hand[2], hC),
           "A3b 没换的那两张**还在原下标**（0 = A · 2 = C）");

        Ok(ps.Hand.Count == 4 && ps.Hand[1].Card.Id == "B118W11_z4" && ps.Hand[3].Card.Id == "B118W11_z3",
           "★ A3 **补到的牌落在【原下标】**（1 ← z4 · 3 ← z3）｜🧨 退回「整批摘 → 一次性补抽」⇒ 会落在末尾 2/3 ⇒ 本行红"
         + "（实得 " + (ps.Hand.Count == 4 ? ps.Hand[1].Card.Id + " / " + ps.Hand[3].Card.Id : "(张数不对)") + "）");

        Ok(ps.Hand[1].Card.Id != "B118W11_B" && ps.Hand[1].Card.Id != "B118W11_D"
           && ps.Hand[3].Card.Id != "B118W11_B" && ps.Hand[3].Card.Id != "B118W11_D",
           "★ A4 换掉的那两张**没有当次被抽回来**（原版：回牌库表尾，补抽从表头取）"
         + "｜🧨 把 `Deck.Insert(0, …)` 改回 `Deck.Add(…)` ⇒ 回库顶 ⇒ 当次抽回 ⇒ 本行红");

        Ok(ps.Deck.Count == 4 && ps.Deck[0].Card.Id == "B118W11_D" && ps.Deck[1].Card.Id == "B118W11_B",
           "A5 换掉的两张回了**牌库表尾**（镜像 = 下标 0/1；后来那张在前：D 再 B）"
         + "（实得 " + (ps.Deck.Count >= 2 ? ps.Deck[0].Card.Id + " / " + ps.Deck[1].Card.Id : "(张数不对)") + "）");
    }

    // ============================================================ B · 战后重判门

    static void SectionB_ForfeitPostBattle()
    {
        Sec("B · `RuleCore.Forfeit` 战后重判门（原版 `shouldLoseIfDisconnectEnd`）");
        var ctx = NewCtx();
        ctx.Winner = 1;                       // 装作「已经判过了」
        Ok(ctx.Winner == 1, "B1 （前提）这一局**已经判过胜负**（Winner = 1）");

        int blocked = RuleCore.Forfeit(ctx, 0, BattleResult.Forfeit);
        Ok(blocked == 1 && ctx.Winner == 1,
           "★ B2 默认（不带第 4 个实参）⇒ **照样挡**：Winner 仍是 1（实得 " + ctx.Winner + "）"
         + "｜🧨 把默认值改成 `true` / 把门写死为「恒放行」⇒ 本行红");

        ctx.Winner = 1;
        int rejudged = RuleCore.Forfeit(ctx, 0, BattleResult.Forfeit, postBattleRejudge: true);
        Ok(rejudged == 2 && ctx.Winner == 2,
           "★ B3 `postBattleRejudge: true` ⇒ **重判**：座位 0 认负 ⇒ Winner = 2（实得 " + ctx.Winner + "）"
         + "｜🧨 把门写死为「恒挡」⇒ 本行红");

        var ctx2 = NewCtx();
        ctx2.Winner = 2;
        int blocked2 = RuleCore.Forfeit(ctx2, 1, BattleResult.Disconnect);
        Ok(blocked2 == 2 && ctx2.Winner == 2,
           "B4 除 `Forfeit` 之外的**理由码**也照同一道门挡（`Disconnect`）⇒ 与改前逐字相同（实得 " + ctx2.Winner + "）");
    }

    // ============================================================ C · 触发路目标表取舍闸

    static void SectionC_TriggerTargetCriteria()
    {
        Sec("C · `TriggerAbilityTargetCriteria`（只接多目标档 · 截断档不接 ⇒ 不烧随机数）");
        var cards = CardDatabase.Load();
        Ok(cards != null && cards.Count > 0, "C1 （前提）卡池读得到（" + (cards == null ? 0 : cards.Count) + " 张）");
        if (cards == null || cards.Count == 0) return;

        int hits = 0, accepted = 0, badTa = 0, badMax = 0, truncOnlyTriggers = 0, truncAccepted = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            var c = cards[i];
            if (c == null) continue;
            var ats = c.AbilityTargets;
            if (ats == null) continue;
            for (int ab = 0; ab < ats.Count; ab++)
            {
                var spec = ats[ab];
                if (spec == null || spec.TargetsAffected == null) continue;
                int trg = spec.Trigger;
                bool hasMultiNoTrunc = false, hasTrunc = false;
                for (int lg = 0; lg < spec.TargetsAffected.Length; lg++)
                {
                    var set = c.AbilityTargetCriteriaAt(ab, lg);
                    if (set == null) continue;
                    int ta = set.TargetsAffected;
                    if (ta != 0 && ta != 10 && ta != 110 && ta != 140 && ta != 150) continue;   // 🔴 2026-11-03（B124-W7 · α）：`ta=0 any` 自 `B121_D3`（账 `b121-d3-ta0`）起合法 —— 判据 `AbilityLogic__GetTargets.c:884-891` · 实现 `RuleCore.cs:18452`
                    if (ta != 0 && set.MaxTargets >= 1) hasTrunc = true; else hasMultiNoTrunc = true;   // `ta=0` **不看** `maxTargets`（原版建完池直接 `goto` 返回）· `RuleCore.cs:18453`
                }
                if (!hasMultiNoTrunc && !hasTrunc) continue;
                if (hasTrunc && !hasMultiNoTrunc && !TriggerHasMultiNoTruncRow(c, trg)) truncOnlyTriggers++;   // 这份 **trigger** 上**只有**截断行（🔴 2026-11-03 B124-W7：按 trigger 聚合，⛔ 不是按 ability —— 实测 `SW51` 的 trigger 0 由 ab0(`ta=0`) + ab1(`ta=10 ∧ maxTargets=1`) 共用，按 ability 分类会误判成 trunc-only）

                var got = RuleCore.TriggerAbilityTargetCriteriaByNumberForTest(c, trg);
                if (got != null)
                {
                    accepted++;
                    int gta = got.TargetsAffected;
                    if (gta != 0 && gta != 10 && gta != 110 && gta != 140 && gta != 150) badTa++;
                    if (gta != 0 && got.MaxTargets >= 1) badMax++;   // `ta=0` 的 `maxTargets` 原版不看 ⇒ 不算违规（`RuleCore.cs:18453`）
                    if (hasTrunc && !hasMultiNoTrunc && !TriggerHasMultiNoTruncRow(c, trg)) truncAccepted++;   // 只有截断行却被接住 = 闸破了（同样按 trigger 聚合）
                }
                if (hasMultiNoTrunc && got != null) hits++;
            }
        }

        Ok(hits > 0, "C2 池内**确有**多目标不截断行被接住（命中 " + hits + " 处）｜🧨 把闸收窄成「恒 null」⇒ 本行红");
        Ok(accepted > 0, "C2b 总计接住 " + accepted + " 处（= C2 的分母，防「命中 0」被读成通过）");
        Ok(badTa == 0, "★ C3 接住的那些**全是多目标档**（`ta ∈ {0,10,110,140,150}` —— `ta=0 any` 自 `B121_D3` 起合法，判据 `AbilityLogic__GetTargets.c:884-891`）；越界 " + badTa + " 处");
        Ok(badMax == 0,
           "★ C4 接住的那些**没有一条是截断档**（`ta≠0 ∧ MaxTargets >= 1` = 会推进 `ctx.GetRandomInt` 的那一档；`ta=0` 不看 `maxTargets`）；违规 " + badMax + " 处"
         + "｜🧨 把闸放宽成「只要 ta 命中就接」⇒ 本行红");
        Ok(truncAccepted == 0,
           "★ C5 只有截断行的 trigger（实测 " + truncOnlyTriggers + " 处，**按 trigger 聚合**）**一条都没接**（实得 " + truncAccepted + " 处；`ta=0` 的行算「多目标不截断」，不参与这一档）"
         + "｜🧨 同上，放宽闸 ⇒ 本行红（🔴 2026-11-03 `B124-W7` 订正：分类口径从 **ability** 改成 **trigger** —— 查找口 `RuleCore.cs:18440-18457` 是按 trigger 聚合的；`SW51` 的 trigger 0 由 `ta=0` 与 `ta=10∧maxTargets=1` 两条 ability 共用，按 ability 分类会误报 1 条）");
    }

    /// <summary>这条 **trigger** 上是否存在「多目标不截断」行（`ta ∈ {0,10,110,140,150}` 且 `ta == 0 ∨ MaxTargets == 0`）。
    /// <para>🔴 **2026-11-03（`B124-W7`）**：C5 原来按 **ability** 分类，而 `TriggerAbilityTargetCriteriaByNumber`
    /// 是**按 trigger 聚合**取行的（`RuleCore.cs:18440-18457` 遍历 `TriggerAbilityRowsOf`）——
    /// 实测 `SW51` `Hunter's Guile` 的 trigger 0 由 ab0（`ta=0`，可接）+ ab1（`ta=10 ∧ maxTargets=1`，截断）共用
    /// ⇒ 按 ability 分类会把 ab1 误判成「trunc-only 却被接住」（实得 1，而那条被接的是 ab0 的 `ta=0`——不烧随机数）。
    /// 判据 = 查找口本身按 trigger 聚合 ⇒ 分类也必须按 trigger 聚合。</para>
    /// <para>⚠️ 用的是**同一个** `TriggerAbilityRowsOf` / `AbilityTargetCriteriaAt` 读口（与实现同源，但这里的期望是
    /// 「有没有可接行」这个**结构事实**，不是抄实现常量 ⇒ 不是自证）。</para></summary>
    static bool TriggerHasMultiNoTruncRow(CardDef card, int trigger)
    {
        if (card == null) return false;
        var rows = card.TriggerAbilityRowsOf(trigger);
        if (rows == null) return false;                 // 列未知 ⇒ 判不了（⛔ 不当「有」，也不当「没有」）
        for (int i = 0; i < rows.Length; i++)
        {
            var r = rows[i];
            if (r == null || r.Ab < 0 || r.Lg < 0) continue;
            var s = card.AbilityTargetCriteriaAt(r.Ab, r.Lg);
            if (s == null) continue;
            int ta = s.TargetsAffected;
            if (ta != 0 && ta != 10 && ta != 110 && ta != 140 && ta != 150) continue;
            if (ta == 0 || s.MaxTargets < 1) return true;
        }
        return false;
    }
}
