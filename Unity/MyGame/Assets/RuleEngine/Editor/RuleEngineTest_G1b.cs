// RuleEngineTest_G1b.cs — 引擎线 `G1b`（第二批）两件：`ASH80` 的 610 · `StatusApplied` 重施加
//
// 判据（⛔ 不要重新论证）→ `资料/普查产出_1024/C2_战斗线判据总表.md`：
//   · §2  `D8 ASH80 Farseer` 的 610：原版收路标石时先对收集者那张卡、再遍历**全场**在场卡
//         `OnTrigger(610)`（`BattleManagerSupport__BroadcastCollectSpiritStone.c:22-38` →
//         `CardScript__ReactToCollectSpiritStone.c:5-9` 的 `OnTrigger(0x262)`）。
//         我们的事件层已经到监听者，**只断** `TriggerNameOfWhenKind` 没有 `GainSpirit` 分支。
//   · §6  `DisplayStatusAnim` 同关键词再次施加：`Start(0)` 那一档**每次都演**、
//         `OnPlay(2)` 那一档只首次演（`BattleCardUI__DisplayStatusAnim.c:78-198`）。
//         我们原来唯一入口 = `SyncBoard` → `TraitFrames.SyncKeywords` 的**集合对差** ⇒ 第二次以后不演。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 **期望值一律写死字面量**（`4` / `1` / `"huntmark"` …）—— ⛔ 不从被测实现反算（那会自证）。
// 🔴 **成对/灭自证**：每个「新写法的观测值」都配一条「旧写法 / 反例」的观测值
//    （①② 的 `Amount = 0 / 1` 两档 · ② 的「信仰不发」反向 · ③ 用 `stun` 只钉「发得出来」）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==================================================================
    //  小工具
    // ==================================================================

    /// <summary>`ctx.Signals` 里 `EvtKind.StatusApplied` 的条数（可按关键词 / `Amount` 过滤）。
    /// ⚠️ 关键词在 `CardId` 那一格（见 `EvtKind.StatusApplied` 的 doc），**不是** `Keyword`。</summary>
    static int CountStatusApplied(BattleContext ctx, string keyword = null, int amount = -1)
    {
        int n = 0;
        foreach (var e in ctx.Signals)
            if (e.Kind == EvtKind.StatusApplied
                && (keyword == null || e.CardId == keyword)
                && (amount < 0 || e.Amount == amount)) n++;
        return n;
    }

    /// <summary>第一条 `StatusApplied`（按关键词）。</summary>
    static BattleEvent FindStatusApplied(BattleContext ctx, string keyword)
    {
        foreach (var e in ctx.Signals)
            if (e.Kind == EvtKind.StatusApplied && e.CardId == keyword) return e;
        return null;
    }

    /// <summary>摆一张「可收集的路标石残骸」（`IsRemnant` + `waystone` ⇒ `CanCollectWaystone` 认）。</summary>
    static UnitState PutWaystoneForTest(BattleContext ctx, int side, int slot)
    {
        var u = Place(ctx, side, slot, Unit("G1bWaystone", 1, 1, 1, KeywordTable.Waystone));
        u.IsRemnant = true;
        return u;
    }

    // ==================================================================
    //  §1  D8 · `OnCollectSpiritStone`(610) 真的发 `EvtKind.Trigger`
    // ==================================================================

    /// <summary>
    /// **`ASH80`/`ASH28`/`ASH78`/`ASH25` 的 610 到底能不能播** —— 端到端四条 + 件级五条。
    ///
    ///  ① 真入口 `RuleCore.CollectWaystone` ⇒ 每张在场卡**各 1 条** `EvtKind.Trigger`
    ///     （`Keyword = "OnCollectSpiritStone"`）；合计 **4 条**（⛔ 不是 5 —— `ASH78` 的 2 行是**动画**、不是事件）。
    ///  ② 事件带**格位**（`Player` / `Slot` = 监听者自己那一格）—— 表现层靠它落点。
    ///  ③ 反向（灭自证）：**加信仰**不发 610（映射只挂在 `GainSpirit`，不是「任何资源变化」）。
    ///  ④ 件级（`VfxMap` 归口）：四张合计 **5 件**，逐张 = 它自己 610 那批候选（不多不少）。
    ///  ⑤ `VfxMap.SelfCheckRoutedTriggers()` = **0 违规**（名字级 + 件级；两张正向锁同长同序）。
    /// </summary>
    public static void TestG1bSpiritStone()
    {
        Section("🆕 引擎线 `G1b` · `C2` §2（`D8`）：收集路标石 ⇒ `OnCollectSpiritStone`(610) 发 `EvtKind.Trigger`");

        var pool = CardDatabase.Load();
        CheckTrue(pool != null && pool.Count > 0, "🧪（前提）卡池载到了");
        if (pool == null || pool.Count == 0) return;
        var ash80 = CardDatabase.FindById(pool, "ASH80");
        var ash28 = CardDatabase.FindById(pool, "ASH28");
        var ash78 = CardDatabase.FindById(pool, "ASH78");
        var ash25 = CardDatabase.FindById(pool, "ASH25");
        CheckTrue(ash80 != null && ash28 != null && ash78 != null && ash25 != null,
                  "🧪（前提）`ASH80` / `ASH28` / `ASH78` / `ASH25` 都在卡池里");
        if (ash80 == null || ash28 == null || ash78 == null || ash25 == null) return;

        // 真值侧：四张的 `abilityTriggers` 都含 610（`HasTriggerSource` 的第一道闸）
        Check(ash80.AbilityTriggerPresence("OnCollectSpiritStone"), 1,
              "（前提）`ASH80 Farseer` 的 v21 真值里有 `OnCollectSpiritStone`(=610)");
        Check(ash28.AbilityTriggerPresence("OnCollectSpiritStone"), 1,
              "（前提）`ASH28 Spiritseer Qelenaris` 有 610");
        Check(ash78.AbilityTriggerPresence("OnCollectSpiritStone"), 1,
              "（前提）`ASH78 Warp Spider Exarch` 有 610");
        Check(ash25.AbilityTriggerPresence("OnCollectSpiritStone"), 1,
              "（前提）`ASH25 Warlock Skyrunner` 有 610");
        CheckTrue(ash80.HasAnimTrigger("OnCollectSpiritStone"),
                  "（前提）`ASH80` 有这一拍的动画行（`PathOfTheSeer_Self`）");

        // ---- ① 真入口：`RuleCore.CollectWaystone`（`BeginAction` 包住的那一支）----
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 1, ash80, exhausted: true);
            Place(ctx, 0, 2, ash28, exhausted: true);
            Place(ctx, 0, 3, ash78, exhausted: true);
            Place(ctx, 0, 5, ash25, exhausted: true);
            PutWaystoneForTest(ctx, 0, 6);
            Check(RuleCore.CanCollectWaystone(ctx, 0, 6), RuleCodes.OK,
                  "🧪（前提）6 号格是**可收集**的路标石残骸");

            ctx.ClearSignals();
            // 🔴 2026-10-24（`E5`）就地更正：**这一格原来断绝对值 `1`**，它暗含两条**都不成立**的前提
            //    ——「夹具进到这里灵魂石是 0」「这一收只加 1」。实际是 **+3**：
            //    · `+1` = 收集本身（`waystoneGiveMana`，`RuleCore.cs:10664`）—— 生产实现是对的；
            //    · `+2` = **本夹具 P1 场上就摆着 `ASH28 Spiritseer Qelenaris`**（`:100`，为验它的 610），
            //      它的能力是「当你收集到一颗灵魂石时，获得 **2 颗额外的**灵魂石」
            //      （`cards_engine.json` 的 `ASH28.desc`；中文 `descZh` 同义）⇒ 收集这一下它必响。
            //    同一条口径在 `RuleEngineTest.cs:20571-20593` 已经钉死（「ASH28：收 1 颗 = 5 + 1 + 2 = 8」）。
            //    ⇒ 判据改成**断增量**（先记 `before`），两个来源都写进期望；⛔ 别改生产值。
            int stoneBefore = ctx.Players[0].SpiritStones;
            CheckCode(RuleCore.CollectWaystone(ctx, 0, 6), RuleCodes.OK, "P1 收集那颗路标石（真入口）");
            Check(ctx.Players[0].SpiritStones, stoneBefore + 1 + 2,
                  $"🧪（前提）灵魂石 **+3** = 收集 `+1`（`waystoneGiveMana`）＋ `ASH28` 监听器 `+2`"
                + $"（增量；{stoneBefore} → {stoneBefore + 3}）"
                + "｜🧨 `CollectWaystone` 漏掉 `BroadcastWhen(GainSpirit)` ⇒ 只剩 +1；"
                + "`ASH28` 那条监听器递归 ⇒ 远超 +3");

            CheckTrue(FindSignal(ctx, EvtKind.Trigger, "OnCollectSpiritStone") != null,
                      "★★ **`OnCollectSpiritStone`(610) 发出来了**"
                    + "｜🧨 删掉 `TriggerNameOfWhenKind` 的 `case WhenEventKind.GainSpirit` ⇒ 本行 null"
                    + "（改前正是这一态：`EmitListenerTrigger` 在 `tn == null` 时直接 return）");

            Check(CountTriggerSignalsA30(ctx, "OnCollectSpiritStone", ash80.Name), 1,
                  "★★ `ASH80` 恰 **1 条**（它 3 行动画里只有 1 行是 610 —— 每行是动画、不是事件）"
                + "｜🧨 映射删掉 ⇒ 0");
            Check(CountTriggerSignalsA30(ctx, "OnCollectSpiritStone", ash28.Name), 1,
                  "★★ `ASH28` 恰 **1 条**（210 那一行 `PathOfTheSeerGreen`）");
            Check(CountTriggerSignalsA30(ctx, "OnCollectSpiritStone", ash78.Name), 1,
                  "★★ `ASH78` 恰 **1 条** —— 它 610 有 **2 行**动画（`Buff_Purple` + `Buff_Eldar_Warp Spider`），"
                + "但**事件只有一条**｜🧨 按动画行数发事件 ⇒ 本行 2（多播）");
            Check(CountTriggerSignalsA30(ctx, "OnCollectSpiritStone", ash25.Name), 1,
                  "★★ `ASH25` 恰 **1 条**");
            Check(CountTriggerSignalsA30(ctx, "OnCollectSpiritStone"), 4,
                  "★★ 全场合计 **4 条**（`ASH80`/`ASH28`/`ASH78`/`ASH25` 各一）"
                + "｜🧨 漏掉其中一张 / 把 `ASH78` 的 2 行各发一次 ⇒ 红");

            // ② 落点 = 监听者**自己那一格**（表现层 `PlaySignal` 的 `slot >= 0` 前提）
            var sig80 = FindSignal(ctx, EvtKind.Trigger, "OnCollectSpiritStone");
            CheckTrue(sig80 != null && sig80.Player == 0 && sig80.Slot == 1,
                      "★ 事件带**格位**（`Player=0 / Slot=1` = `ASH80` 自己那一格）—— 表现层靠它落点"
                    + $"（实得 p={sig80?.Player} s={sig80?.Slot}）"
                    + "｜🧨 把 `Slot` 写成 -1（手牌/未知锚）⇒ 表现层永远不播");
        }

        // ---- ③ 反向（灭自证）：**加信仰**不发 610（不是「任何资源变化都发」）----
        {
            var gain = Tactic("G1bFaithTactic", 0, "Gain 3 ☀");
            var ctx = ProbeBattle(new[] { gain }, new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 2, ash80, exhausted: true);
            int hi = HandIdx(ctx, 0, "G1bFaithTactic");
            CheckTrue(hi >= 0, "🧪（前提）`Gain 3 ☀` 在起手里");
            if (hi >= 0)
            {
                ctx.ClearSignals();
                CheckCode(RuleCore.PlayTactic(ctx, 0, hi, -1), RuleCodes.OK, "打出一张 `Gain 3 ☀`");
                Check(ctx.Players[0].Faith, 3, "🧪（前提）信仰 +3");
                Check(CountTriggerSignalsA30(ctx, "OnCollectSpiritStone"), 0,
                      "★★ **反向（灭自证）**：加**信仰**不发 610（映射只挂在 `WhenEventKind.GainSpirit`）"
                    + "｜🧨 把它挂到 `GainFaith` / 挂成「任何资源变化」⇒ 红");
            }
        }

        // ---- ④ 件级（`VfxMap` 归口）：610 那一拍挑出的是这张卡自己的候选 ------------
        // 与生产路径**逐参一致**（`perCardOnly: true` + 生产发出来的那个 **PascalCase** 枚举名）。
        CardPresentation.VfxMap.EnsureLoaded();
        {
            var got80 = CardPresentation.VfxMap.ResolveAllTimed(
                CardPresentation.VfxMap.Trigger, "SaimHann", "ASH80", ash80.AnimTimes,
                null, true, "OnCollectSpiritStone");
            Check(got80.Length, 1,
                  "★ VfxMap：`ASH80` 的 610 那一拍 = **1 件**"
                + "｜🧨 不放行 ⇒ `target` 恒 null ⇒ 掉回 `ability` 粗桶（3 件：`WaystoneTrigger` + "
                + "`Waystone UI Spend` + `PathOfTheSeer` = 错件 + 多播）");
            CheckTrue(got80.Length > 0 && got80[0].Name == "PathOfTheSeer",
                      $"★ …… 那一件正好是 **`PathOfTheSeer`**（实得「{(got80.Length > 0 ? got80[0].Name : "（无）")}」）");

            var got28 = CardPresentation.VfxMap.ResolveAllTimed(
                CardPresentation.VfxMap.Trigger, "SaimHann", "ASH28", ash28.AnimTimes,
                null, true, "OnCollectSpiritStone");
            CheckTrue(got28.Length == 1 && got28[0].Name == "PathOfTheSeerGreen",
                      $"★ VfxMap：`ASH28` = **1 件** `PathOfTheSeerGreen`"
                    + $"（实得 {got28.Length} 件「{(got28.Length > 0 ? got28[0].Name : "（无）")}」）");

            var got78 = CardPresentation.VfxMap.ResolveAllTimed(
                CardPresentation.VfxMap.Trigger, "SaimHann", "ASH78", ash78.AnimTimes,
                null, true, "OnCollectSpiritStone");
            var n78 = new List<string>();
            for (int i = 0; i < got78.Length; i++) n78.Add(got78[i].Name);
            Check(got78.Length, 2,
                  "★ VfxMap：`ASH78` 的 610 那一拍 = **2 件**（两行动画各一件）"
                + "｜🧨 按 prefab 名去重 / 只播首行 ⇒ 1");
            CheckTrue(n78.Contains("Buff_Purple") && n78.Contains("Buff_Eldar_Warp Spider"),
                      "★ …… 两件正好是 `Buff_Purple` + `Buff_Eldar_Warp Spider`（现读候选）");

            var got25 = CardPresentation.VfxMap.ResolveAllTimed(
                CardPresentation.VfxMap.Trigger, "SaimHann", "ASH25", ash25.AnimTimes,
                null, true, "OnCollectSpiritStone");
            CheckTrue(got25.Length == 1 && got25[0].Name == "BulletImpact_Shuriken_Short_twinlink",
                      $"★ VfxMap：`ASH25` = **1 件** `BulletImpact_Shuriken_Short_twinlink`"
                    + $"（实得 {got25.Length} 件「{(got25.Length > 0 ? got25[0].Name : "（无）")}」）");
        }

        // ---- ⑤ 白名单两条锁 + 件级自检：0 违规 -------------------------------
        // `OnCollectSpiritStone` 同时进 `MustRouteTriggerNames` 与 `MustRouteTriggerKeywords`
        // （两张表必须**同长同序**，见 `SelfCheckRoutedTriggers` 里那一句长度断言）。
        {
            var bad = CardPresentation.VfxMap.SelfCheckRoutedTriggers();
            Check(bad.Length, 0,
                  "★ `VfxMap` 白名单自检（名字级 + **件级**）= **0 违规**"
                + (bad.Length == 0 ? "" : "\n  ✗ " + string.Join("\n  ✗ ", bad)));
        }
    }

    // ==================================================================
    //  §2  `EvtKind.StatusApplied` —— 同关键词再次施加
    // ==================================================================

    /// <summary>
    /// **同一个关键词再次施加 ⇒ 引擎发 `StatusApplied`，且「首次 / 重施加」两档互斥。**
    ///
    ///  ① `huntmark` 第一次 ⇒ **1 条**，`Amount = 0`（首次那档由 `SyncKeywords` 演，事件不抢）；
    ///  ② `huntmark` 第二次 ⇒ **1 条**，`Amount = 1`（重施加 ⇒ 表现层重播 `Start(0)`）；
    ///  ③ 事件落点 = **被施加那张卡**（`Player`/`Slot`），`CardId` = **关键词本身**；
    ///  ④ 反向（灭自证）：玩家资源（信仰）**不发** `StatusApplied`（发射口挂在 `AddKeyword` 语义点，
    ///    不是「任何结算都发」）；
    ///  ⑤ `stun` 施加 ⇒ 1 条（`Amount = 0`）—— ⚠️ 只钉「发得出来」，⛔ 不拿它验重播
    ///     （`stun` **没有** `Start(0)` 行，拿它验会假绿，见 `C2` §6·2）。
    /// </summary>
    public static void TestG1bStatusApplied()
    {
        Section("🆕 引擎线 `G1b` · `C2` §6：同关键词再次施加 ⇒ `EvtKind.StatusApplied`（首次 0 / 重施加 1）");

        // ---- ① ② ③ `huntmark` 两次（真入口：`Give Hunt Mark to an enemy troop`）----
        {
            var prey = new CardDef("G1bPrey", "G1bPrey", "unit", "", "common", "Test",
                                   1, 0, 9, 0, null, subtype: "Infantry");
            var mark = Tactic("G1bMarkTactic", 0, "Give Hunt Mark to an enemy troop");
            var ctx = ProbeBattle(new[] { mark, mark }, new CardDef[0]);
            ToP1Turn(ctx, 2);
            var victim = Place(ctx, 1, 3, prey, exhausted: true);

            int hi1 = HandIdx(ctx, 0, "G1bMarkTactic");
            CheckTrue(hi1 >= 0, "🧪（前提）起手有两张 `Give Hunt Mark to an enemy troop`");
            if (hi1 >= 0)
            {
                ctx.ClearSignals();
                CheckCode(RuleCore.PlayTactic(ctx, 0, hi1, 3), RuleCodes.OK,
                          "第一次挂猎杀标记（真入口 `PlayTactic` → `ApplyOneGain` → `AddKeyword`）");
                CheckTrue(victim.Has("huntmark"), "🧪（前提）猎杀标记真的上去了");
                Check(CountStatusApplied(ctx, "huntmark", 0), 1,
                      "★★ **首次获得** ⇒ 恰 **1 条** `StatusApplied`，`Amount = 0`"
                    + "（首次那一档交给 `SyncKeywords` 的「新出现」路演）"
                    + "｜🧨 把 `Amount` 写成恒 1 ⇒ 首次会被演两遍（事件 + `SyncKeywords`）");
                Check(CountStatusApplied(ctx, "huntmark", 1), 0,
                      "★★ 首次**不许**报成「重施加」（两条路结构上互斥）"
                    + "｜🧨 `reapply` 判据写成「恒真」⇒ 本行 1");
            }

            int hi2 = HandIdx(ctx, 0, "G1bMarkTactic");
            CheckTrue(hi2 >= 0, "🧪（前提）第二张 `Give Hunt Mark` 还在手里");
            if (hi1 >= 0 && hi2 >= 0)
            {
                ctx.ClearSignals();
                CheckCode(RuleCore.PlayTactic(ctx, 0, hi2, 3), RuleCodes.OK,
                          "第二次挂猎杀标记（**同一个关键词**，施加前已经在身上）");
                Check(CountStatusApplied(ctx, "huntmark", 1), 1,
                      "★★ **重施加** ⇒ 恰 **1 条** `StatusApplied`，`Amount = 1`"
                    + "（原版 `DisplayStatusAnim` 每次施加都演 `Start(0)`，见 `C2` §6·1）"
                    + "｜🧨 把引擎那一句 emit 删掉 ⇒ 0（改前正是这一态：第二次以后一次都不演）");
                Check(CountStatusApplied(ctx, "huntmark", 0), 0,
                      "★★ 重施加**不许**报成首次"
                    + "｜🧨 `reapply` 恒假 ⇒ 本行 1、表现层永远不重播");

                var sig = FindStatusApplied(ctx, "huntmark");
                // 🔴 2026-10-24（`E5`）就地更正：这里原来写 `sig.Player == 0` —— **与夹具自相矛盾**。
                //    受害者是 `Place(ctx, 1, 3, prey)` 摆在 **P2 那一侧**的（`Give Hunt Mark to an enemy troop`
                //    打的就是敌人），而 `EvtKind.StatusApplied` 的约定是 `Player`/`Slot` = **被施加的那张卡**
                //    （`Core/BattleEvent.cs:346-348` 的 doc；发射口 `EffectResolver.cs:6756-6762` 走 `FindSlot`）
                //    ⇒ 正确答案是 **`Player=1 / Slot=3`**。改成 0 就等于要求「落点取施放者那一侧」，
                //    正是本行 🧨 里点名要防的那个错。`Slot=3` 与 `CardId="huntmark"` 两格原样保留。
                CheckTrue(sig != null && sig.Player == 1 && sig.Slot == 3 && sig.CardId == "huntmark",
                          "★ 事件落点 = **被施加那张卡**（它是 P2 的兵 ⇒ `Player=1 / Slot=3`），"
                        + "`CardId` = **关键词本身**"
                        + $"（实得 p={sig?.Player} s={sig?.Slot} c={sig?.CardId}）"
                        + "｜🧨 `CardId` 写成卡名 / `Player`·`Slot` 取成施放者 ⇒ 表现层 `ViewAt` 找不到视图");
            }
        }

        // ---- ④ 反向（灭自证）：玩家资源（信仰）**不发** `StatusApplied` ----------
        {
            var gain = Tactic("G1bFaithTactic2", 0, "Gain 3 ☀");
            var ctx = ProbeBattle(new[] { gain }, new CardDef[0]);
            ToP1Turn(ctx, 1);
            int hi = HandIdx(ctx, 0, "G1bFaithTactic2");
            CheckTrue(hi >= 0, "🧪（前提）`Gain 3 ☀` 在起手里");
            if (hi >= 0)
            {
                ctx.ClearSignals();
                CheckCode(RuleCore.PlayTactic(ctx, 0, hi, -1), RuleCodes.OK, "打出 `Gain 3 ☀`");
                Check(CountStatusApplied(ctx), 0,
                      "★★ **反向（灭自证）**：玩家资源变化（信仰）**不发** `StatusApplied`"
                    + "｜🧨 把发射口挂到 `DoGain` 那一层（任何资源结算都发）⇒ 红"
                    + "（那样 HUD 计数也会去调 `DisplayStatus`，是错件）");
            }
        }

        // ---- ⑤ `stun`：施加 ⇒ 1 条（`Amount = 0`）；⛔ 不拿它验重播 ------------
        {
            var tac = Tactic("G1bStunTac", 0, "Stun an enemy");
            var ctx = ProbeBattle(new[] { tac }, new CardDef[0]);
            ToP1Turn(ctx, 1);
            var vic = Place(ctx, 1, 3, new CardDef("G1bStunVictim", "G1bStunVictim", "unit", "",
                                                   "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry"),
                            exhausted: true);
            int hi = HandIdx(ctx, 0, "G1bStunTac");
            CheckTrue(hi >= 0, "🧪（前提）`Stun an enemy` 在起手里");
            if (hi >= 0)
            {
                ctx.ClearSignals();
                CheckCode(RuleCore.PlayTactic(ctx, 0, hi, 3), RuleCodes.OK, "「Stun an enemy」打出去了");
                CheckTrue(vic.IsStunned, "🧪（前提）它真的晕了");
                Check(CountStatusApplied(ctx, "stun", 0), 1,
                      "★★ 眩晕施加 ⇒ 恰 **1 条** `StatusApplied`（`CardId = \"stun\"`、`Amount = 0`）"
                    + "｜🧨 漏掉 `DoStun` 那一处 emit ⇒ 0");
                Check(CountStatusApplied(ctx, "stun", 1), 0,
                      "★★ 首次眩晕**不许**报成「重施加」"
                    + "（⚠️ `stun` 只有 `OnPlay(2)`、**没有 `Start(0)` 行** ⇒ 原版「再晕」也不重播 Start，"
                    + "见 `C2` §6·2 —— ⛔ 别拿它当重播的验收样本）");
            }
        }
    }
}
