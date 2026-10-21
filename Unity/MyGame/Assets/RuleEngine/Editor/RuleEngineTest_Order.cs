// RuleEngineTest_Order.cs — 引擎线 `T03·c` 批 A：**`PlayOrder`（进场序）排序** 的断言
// （2026-10-21；账 `W59` = `W58-INV-ORDER` 的落地批）
//
// 为什么单开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册
// `_BoardModel` / `_S14` / `_S23_*` / `_Diag` 都在），而**同一时刻只能有一个写手改一个文件**
// ⇒ 本批一律写在这里，`RuleEngineTest.cs` **只加了 `Run()` 里一行 `Step(...)`**。
//
// 判据（⛔ 不要重新论证）：
//   · `资料/普查产出_1020/W58_战斗结算_多词条顺序判据.md` §〇 / §一·A1：
//     原版 `BattleManager.allCardsInPlay`（`+0x470`）是**进场顺序的追加列表** ⇒
//     多单位「谁先结算」= **谁先进场**，⛔ 不是棋盘格号、不是座位序、不是卡 id。
//   · 第一权威 = 反编译 `d:/2/tools/decomp_full/`（`SetupFullHero.c:43-70` ·
//     `CreateStartingTroop.c:71-100` · `_ResolvePlayCardFromHand…:709-711` ·
//     `_ResolveSummonUnit…:179-183` · `_ResolveMinionDeath…:56-59` 全是 List.Add/Remove）。
//
// 🔴 每条都带 `🧨`（**什么改动能让它红**）—— 没写 `🧨` 的断言一律当没写。
// 🔴 夹具前提也当断言写（`🧪`）：解析 / 取号没成立时，下面的次序断言是**假绿**。
//
// ⚠️ 本文件里凡**改棋盘 / 直接调 `RuleCore.*`** 的，都是**夹具直调**，
//    不走 `BattleDriver.LocalAct` / `SimpleAI.ExecuteAction` 两个记账口 ⇒ 不会被录进本地录像
//    （这是断言不是对局，同 `RuleEngineTest_S14.cs` 的口径）。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    // ==================================================================
    //  本批专用的小工具（名字一律带 `W59` 前缀，⛔ 不与既有私有辅助重名）
    // ==================================================================

    /// <summary>从 `from` 起找第一条含 `needle` 的引擎日志下标（`LogIdx` 的「只看这一段」版；
    /// 没有 ⇒ `-1`）。用途：同一句正文可能在**这一段之前**也被打过（开局 / 上一回合），
    /// 从 0 找会拿到旧的那一条 ⇒ 次序断言假绿。</summary>
    static int W59LogIdxAfter(BattleContext ctx, int from, string needle)
    {
        for (int i = Math.Max(0, from); i < ctx.Events.Count; i++)
            if (!string.IsNullOrEmpty(ctx.Events[i])
                && ctx.Events[i].IndexOf(needle, StringComparison.Ordinal) >= 0) return i;
        return -1;
    }

    /// <summary>双方棋盘上**除 <paramref name="except"/> 之外**最大的 `PlayOrder`（一个都没有 ⇒ `-1`）。</summary>
    static int W59MaxPlayOrder(BattleContext ctx, UnitState except)
    {
        int max = -1;
        for (int p = 0; p < 2; p++)
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ctx.Players[p].Board[s];
                if (u == null || ReferenceEquals(u, except)) continue;
                if (u.PlayOrder > max) max = u.PlayOrder;
            }
        return max;
    }

    // ==================================================================
    //  ① 同侧：**格号 vs 进场序**（部署广播 / `BroadcastWhen` 的棋盘趟）
    // ==================================================================
    /// <summary>
    /// **多单位同听一个事件时，先落场的先结算 —— 哪怕它的格号更大**。
    ///
    /// 夹具：两张**只差名字**的监听者（正文 `When a unit is deployed, gain +2 Attack`，
    /// `OwnerIs = -1` ⇒ 双方棋盘上都会听），A 先落 **3 号格（贴督军）**、
    /// B 后落 **2 号格**。⇒ **格号序（2 → 3）与进场序（A → B）相反**。
    /// 然后从手牌**真打**第三张（`PlayCard` ⇒ 真的发 `deploy` 广播）。
    /// </summary>
    static void TestW59PlayOrderSameSideDeploy()
    {
        const string Listen = "When a unit is deployed, gain +2 Attack";
        var listenA = new CardDef("W59ListenA", "W59ListenA", "unit", Listen,
                                  "common", "Test", 1, 0, 9, 0, null, subtype: "Infantry");
        var listenB = new CardDef("W59ListenB", "W59ListenB", "unit", Listen,
                                  "common", "Test", 1, 0, 9, 0, null, subtype: "Infantry");
        var third = Unit("W59Third", 1, 1, 5);

        // 🧪 夹具自检：两条监听器**真的注册成了 `deploy` 监听器** ——
        //    解析不出（例如主语被当成卡名）时它们**一次都不响**，下面的次序断言会变成「-1 < -1」假绿。
        CheckTrue(listenA.WhenTriggers.Count > 0 && listenB.WhenTriggers.Count > 0,
                  "🧪 夹具自检：`When a unit is deployed, …` 两条都注册成了监听器");

        var ctx = ProbeBattle(new[] { third }, new CardDef[0]);
        ToP1Turn(ctx, 2);
        var a = Place(ctx, 0, 3, listenA, exhausted: true);   // 先落：贴督军（格号 3）
        var b = Place(ctx, 0, 2, listenB, exhausted: true);   // 后落：外一格（格号 2）
        CheckTrue(a.PlayOrder >= 0 && b.PlayOrder >= 0 && a.PlayOrder < b.PlayOrder,
                  "🧪（前提）进场号 A(" + a.PlayOrder + ") < B(" + b.PlayOrder + ")；"
                + "格号 A=3 > B=2 ⇒ 两个排序键**相反**");

        int logFrom = ctx.Events.Count;
        int hi = HandIdx(ctx, 0, "W59Third");
        CheckTrue(hi >= 0, "🧪（前提）手里有那张要部署的第三张（没有 ⇒ 下面全假绿）" + LogTail(ctx));
        if (hi < 0) return;
        int landed;
        CheckCode(RuleCore.PlayCard(ctx, 0, hi, 1, out landed), RuleCodes.OK,
                  "打出手里的第三张部队（落 1 号格）");

        int ia = W59LogIdxAfter(ctx, logFrom, "「W59ListenA」的监听器");
        int ib = W59LogIdxAfter(ctx, logFrom, "「W59ListenB」的监听器");
        CheckTrue(ia >= 0 && ib >= 0,
                  "🧪（前提）两个监听器**都真的跑了**（日志各一行）" + LogTail(ctx));
        CheckTrue(ia >= 0 && ib >= 0 && ia < ib,
                  "★★ **同侧：先落场的 A 先结算**（日志下标 " + ia + " < " + ib + "）—— "
                + "两个排序键相反（进场序 A→B / 格号序 B(2)→A(3)）⇒ 这一条量的就是排序键本身；"
                + "🧨 把 `EffectResolver.BroadcastWhen` 里那句 `StableSortByPlayOrder(listeners, …)` "
                + "删掉（退回「玩家 0 整排 → 玩家 1 整排、每排格号升序」）⇒ 实得 B 先 ⇒ 本行红");
    }

    // ==================================================================
    //  ② 跨玩家：**不是座位序**
    // ==================================================================
    /// <summary>
    /// **对手先落场的监听者，会比后落场的我方监听者先结算** —— 即「谁先响」不看玩家索引。
    ///
    /// 夹具：P1（对手）先落 listeners A（3 号格），P0（我方）后落 B（3 号格）；
    /// 然后**我方**部署一张 ⇒ 双方棋盘上的监听者都收到（`OwnerIs = -1`）。
    /// 改前（`for p in 0..1`）= **P0 永远先** ⇒ 与本条相反。
    /// </summary>
    static void TestW59PlayOrderCrossPlayer()
    {
        const string Listen = "When a unit is deployed, gain +2 Attack";
        var listenA = new CardDef("W59XListenA", "W59XListenA", "unit", Listen,
                                  "common", "Test", 1, 0, 9, 0, null, subtype: "Infantry");
        var listenB = new CardDef("W59XListenB", "W59XListenB", "unit", Listen,
                                  "common", "Test", 1, 0, 9, 0, null, subtype: "Infantry");
        var third = Unit("W59XThird", 1, 1, 5);

        CheckTrue(listenA.WhenTriggers.Count > 0 && listenB.WhenTriggers.Count > 0,
                  "🧪 夹具自检：两条监听器都注册成了 `deploy` 监听器");

        var ctx = ProbeBattle(new[] { third }, new CardDef[0]);
        ToP1Turn(ctx, 2);
        var p1u = Place(ctx, 1, 3, listenA, exhausted: true);   // **对手**的先落场
        var p0u = Place(ctx, 0, 3, listenB, exhausted: true);   // **我方**的后落场
        CheckTrue(p1u.PlayOrder >= 0 && p0u.PlayOrder >= 0 && p1u.PlayOrder < p0u.PlayOrder,
                  "🧪（前提）进场号 对手A(" + p1u.PlayOrder + ") < 我方B(" + p0u.PlayOrder + ")");

        int logFrom = ctx.Events.Count;
        int hi = HandIdx(ctx, 0, "W59XThird");
        CheckTrue(hi >= 0, "🧪（前提）手里有那张要部署的第三张" + LogTail(ctx));
        if (hi < 0) return;
        int landed;
        CheckCode(RuleCore.PlayCard(ctx, 0, hi, 2, out landed), RuleCodes.OK,
                  "**我方**打出一张部队（对手的监听者也要听得到）");

        int ia = W59LogIdxAfter(ctx, logFrom, "「W59XListenA」的监听器");
        int ib = W59LogIdxAfter(ctx, logFrom, "「W59XListenB」的监听器");
        CheckTrue(ia >= 0 && ib >= 0,
                  "🧪（前提）跨方的两个监听器**都真的跑了**" + LogTail(ctx));
        CheckTrue(ia >= 0 && ib >= 0 && ia < ib,
                  "★★ **跨玩家：先落场的（P1 的 A）先结算**，与座位序无关（" + ia + " < " + ib + "）—— "
                + "判据 = 原版 `allCardsInPlay` 是**双方混排**的进场序；"
                + "🧨 退回「玩家 0 整排 → 玩家 1 整排」⇒ 我方的 B 先响 ⇒ 本行红");
    }

    // ==================================================================
    //  ③ 回合起止（`EffectResolver.ResolveAtTurn` 的棋盘那一趟）
    // ==================================================================
    /// <summary>
    /// **回合末的多个 at-turn 单位：日志序 = 落场序**。
    ///
    /// 夹具：右侧两格（5 = 贴督军、6 = 最外）—— A **先落外格 6**、B **后落内格 5**
    /// ⇒ 格号序（5 → 6）= B → A，与进场序（A → B）相反。
    /// 两条正文**故意不同**（`gain +2` / `gain +3`）⇒ 日志能分辨是谁先结算的。
    /// ⚠️ 观察口 = 每条 op 自己那行 `回合结束触发：「gain +N Attack」给了 1 个目标`
    /// （`DoGive` 的逐目标日志；夹具卡没有 `AnimTimes` ⇒ `EvtKind.Trigger` 那一拍不发）。
    /// </summary>
    static void TestW59PlayOrderTurnStartEnd()
    {
        var atA = new CardDef("W59AtA", "W59AtA", "unit",
                              "At the end of each turn, gain +2 Attack",
                              "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
        var atB = new CardDef("W59AtB", "W59AtB", "unit",
                              "At the end of each turn, gain +3 Attack",
                              "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
        CheckTrue(EffectText.AtTurnClauses(atA.Desc).Count > 0
                  && EffectText.AtTurnClauses(atB.Desc).Count > 0,
                  "🧪 夹具自检：两张的 `At the end of each turn, …` 都被 `AtTurnClauses` 认出来了");

        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 1);
        var a = Place(ctx, 0, 6, atA, exhausted: true);   // **先落、外格**（右侧最外）
        var b = Place(ctx, 0, 5, atB, exhausted: true);   // **后落、内格**（贴督军）
        CheckTrue(a.PlayOrder >= 0 && b.PlayOrder >= 0 && a.PlayOrder < b.PlayOrder,
                  "🧪（前提）进场号 A(" + a.PlayOrder + ") < B(" + b.PlayOrder + ")；"
                + "格号 A=6 > B=5 ⇒ 两个排序键**相反**");

        int logFrom = ctx.Events.Count;
        RuleCore.EndTurn(ctx);                            // 我方回合结束 ⇒ `ResolveAtTurn("turn_end")`

        int la = W59LogIdxAfter(ctx, logFrom, "gain +2 Attack");
        int lb = W59LogIdxAfter(ctx, logFrom, "gain +3 Attack");
        CheckTrue(la >= 0 && lb >= 0,
                  "🧪（前提）两条 at-turn 都结算了（日志里各一行）" + LogTail(ctx));
        CheckTrue(la >= 0 && lb >= 0 && la < lb,
                  "★★ **回合末：日志序 = 落场序**（先落的外格 A 先结算；" + la + " < " + lb + "）—— "
                + "判据 = 原版 `BroadcastTurnEnd` 的棋盘趟走 `allCardsInPlay`（进场序，双方混排）；"
                + "🧨 把 `EffectResolver.ResolveAtTurn` 里那句 `StableSortByPlayOrder(unitJobs, …)` 删掉"
                + "（退回「玩家 0 → 1、每方格号升序」）⇒ 实得 B（格号 5）先 ⇒ 本行红");
    }

    // ==================================================================
    //  ④ 重入号：**每次进场重新取号**（死 → 复活 = 新号）
    // ==================================================================
    /// <summary>
    /// **残骸翻回来 = 重新进场 ⇒ 取新号，且新号大于所有现存在场者**；
    /// 中间那一档（翻面成残骸）**沿用**被翻面那张的号（原版是否重排判据未闭合，见实现注释）。
    ///
    /// 另加一条：**`PlayCard` 之后在场单位的号严格递增**（防「号取了但没写进单位」）。
    /// </summary>
    static void TestW59PlayOrderReentry()
    {
        // ---- ④-1 死 → 复活 ----
        {
            var rem = new CardDef("W59Rem", "W59Rem", "unit", "",
                                  "common", "Test", 1, 2, 5, 0,
                                  new[] { KeywordTable.Remnant }, subtype: "Infantry");
            var kill = Tactic("W59Kill", 0, "Deal 99 damage to a friendly unit");
            var raise = Tactic("W59Raise", 0, "Reanimate a friendly Remnant");
            var ctx = ProbeBattle(new[] { rem, kill, raise }, new[] { Unit("W59Foe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var other = Place(ctx, 0, 5, Unit("W59Other", 1, 1, 9), exhausted: true);
            var u = Place(ctx, 0, 3, rem, exhausted: true);
            int oldOrder = u.PlayOrder;
            CheckTrue(oldOrder >= 0 && other.PlayOrder >= 0,
                      "🧪（前提）两个夹具单位都取到号了（" + oldOrder + " / " + other.PlayOrder + "）");

            int hiKill = HandIdx(ctx, 0, "W59Kill");
            CheckTrue(hiKill >= 0, "🧪（前提）手里有那张「打死自己人」的战术");
            if (hiKill < 0) return;
            CheckCode(RuleCore.PlayTactic(ctx, 0, hiKill, 3), RuleCodes.OK,
                      "把 3 号格那个带残骸的单位打死 ⇒ 翻面成残骸");

            var rm = Board(ctx, 0, 3);
            CheckTrue(rm != null && rm.IsRemnant, "🧪（前提）它翻面成残骸了" + LogTail(ctx));
            if (rm == null || !rm.IsRemnant) return;
            Check(rm.PlayOrder, oldOrder,
                  "残骸**沿用**被翻面那张的进场号（" + oldOrder + "）—— 原版「原地翻面」不重新进场"
                + "（⚠️ 这条原版判据**未闭合**，见 `CleanupDeaths` 里那段注释；"
                + "🧨 在残骸初始器里改成 `ctx.TakePlayOrder()` ⇒ 本行红）");

            int hiRaise = HandIdx(ctx, 0, "W59Raise");
            CheckTrue(hiRaise >= 0, "🧪（前提）手里有那张 `Reanimate a friendly Remnant`");
            if (hiRaise < 0) return;
            CheckCode(RuleCore.PlayTactic(ctx, 0, hiRaise, -1), RuleCodes.OK,
                      "把残骸翻回来（= 重新进场）");
            var back = Board(ctx, 0, 3);
            CheckTrue(back != null && !back.IsRemnant, "🧪（前提）它真的翻回来了" + LogTail(ctx));
            if (back == null) return;

            int maxOther = W59MaxPlayOrder(ctx, back);
            CheckTrue(back.PlayOrder > oldOrder,
                      "★★ **复活 = 取新号**：新号 " + back.PlayOrder + " > 翻面前的 " + oldOrder
                    + "（原版 `_ResolveMinionDeath` `List.Remove` 之后重新 `List.Add` 到**表尾**）；"
                    + "🧨 删掉 `EffectResolver.DoReanimate` 里那句 `back.PlayOrder = ctx.TakePlayOrder();` "
                    + "⇒ 沿用旧号 ⇒ 本行红");
            CheckTrue(back.PlayOrder > maxOther,
                      "★★ **新号大于所有现存在场者**（" + back.PlayOrder + " > " + maxOther + "）—— "
                    + "「谁先响」上它要排到**所有人之后**（同一条 `🧨`）");
        }

        // ---- ④-2 `PlayCard` 之后在场单位的号严格递增 ----
        {
            var ctx = ProbeBattle(new[] { Unit("W59Seq1", 1, 1, 5),
                                          Unit("W59Seq2", 1, 1, 5),
                                          Unit("W59Seq3", 1, 1, 5) },
                                  new CardDef[0]);
            ToP1Turn(ctx, 2);
            var orders = new List<int>();
            for (int k = 1; k <= 3; k++)
            {
                int hi = HandIdx(ctx, 0, "W59Seq" + k);
                CheckTrue(hi >= 0, "🧪（前提）手里有第 " + k + " 张（没有 ⇒ 后面的号对不齐）" + LogTail(ctx));
                if (hi < 0) return;
                int landed;
                CheckCode(RuleCore.PlayCard(ctx, 0, hi, 1, out landed), RuleCodes.OK,
                          "打出第 " + k + " 张部队（落 " + landed + " 号格）");
                var u = landed >= 0 ? Board(ctx, 0, landed) : null;
                CheckTrue(u != null, "🧪（前提）第 " + k + " 张真的落在 " + landed + " 号格");
                if (u == null) return;
                orders.Add(u.PlayOrder);
            }
            CheckTrue(orders[0] >= 0 && orders[0] < orders[1] && orders[1] < orders[2],
                      "★★ **`PlayCard` 的进场号严格递增**（" + orders[0] + " < " + orders[1]
                    + " < " + orders[2] + "）—— ⚠️ 按**打出次序**比，⛔ 不是按格号比"
                    + "（插入让位 ⇒ 后打出的站在小格号上）；"
                    + "🧨 删掉 `RuleCore.PlayCard` 里那句取号 ⇒ 三个都是 -1 ⇒ 本行红");
        }
    }

    // ==================================================================
    //  ⑤ 不变量：新落场的单位**一定取到号**（`>= 0`）
    // ==================================================================
    /// <summary>
    /// 防「排序静默失效」：**`PlayOrder = -1` 的单位在排序里排末尾**（见
    /// `RuleCore.StableSortByPlayOrder`），所以「某条入口忘了取号」不会报错，
    /// 只会让那一批单位的先后**退回格号序**。⇒ 把这个前提本身钉成断言。
    /// </summary>
    static void TestW59PlayOrderInvariant()
    {
        var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
        ToP1Turn(ctx, 2);

        // ① 督军：两个都要有号，且 P0 在 P1 之前（原版 `SetupBoardPhase.c:120/:145` 先 P0 再 P1）
        CheckTrue(ctx.Players[0].Warlord.PlayOrder >= 0 && ctx.Players[1].Warlord.PlayOrder >= 0,
                  "★★ **督军也取号**（P0=" + ctx.Players[0].Warlord.PlayOrder
                + " / P1=" + ctx.Players[1].Warlord.PlayOrder + "）—— 原版开场两次 `List.Add`；"
                + "🧨 删掉 `RuleCore.BuildPlayer` 里那句取号 ⇒ 两个都是 -1 ⇒ 本行红");
        CheckTrue(ctx.Players[0].Warlord.PlayOrder < ctx.Players[1].Warlord.PlayOrder,
                  "★★ 督军的先后 = **建人顺序**（P0 先、P1 后），与原版同形");

        // ② 免费部署（`DeployFree` 的卡模板重载 → 走实例重载那条实现）
        int slot;
        CheckTrue(RuleCore.DeployFree(ctx, 0, Unit("W59Free", 1, 1, 5), out slot),
                  "`DeployFree` 部署一张（落 " + slot + " 号格）");
        var free = slot >= 0 ? Board(ctx, 0, slot) : null;
        CheckTrue(free != null && free.PlayOrder >= 0,
                  "★★ **免费部署也取号**（" + (free == null ? "null" : free.PlayOrder.ToString()) + "）—— "
                + "🧨 删掉 `RuleCore.DeployFree` 里那句取号 ⇒ -1 ⇒ 本行红");

        // ③ 全场扫一遍：不许有「在场的单位却没号」
        int missing = 0;
        for (int p = 0; p < 2; p++)
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ctx.Players[p].Board[s];
                if (u != null && u.PlayOrder < 0) missing++;
            }
        Check(missing, 0,
              "★★ **场上没有「-1 号」的单位**（现读 " + missing + " 个）—— "
            + "排序时 `-1` 一律排末尾 ⇒ 漏取号不会报错、只会**静默退回格号序**（本工程红线：不许静默失败）");
    }

    // ==================================================================
    //  ⑥ 排序 2：`RuleCore.FireTriggerOnSide`（单方关键词：Artifice / Cruelty / Uprising …）
    // ==================================================================
    /// <summary>
    /// **单方关键词那一趟也按进场序** —— 用 `Artifice`（打出战术 ⇒ 我方那一排带 `Artifice` 的单位各响一次，
    /// `EffectResolver` 的 `PlayTactic` 走 `FireTriggerOnSide`）。
    ///
    /// 夹具：A 先落 **3 号格**（贴督军）、B 后落 **2 号格** ⇒ 格号序（2 → 3）与进场序（A → B）相反。
    /// </summary>
    static void TestW59PlayOrderOnSide()
    {
        var artA = new CardDef("W59ArtA", "W59ArtA", "unit", "Artifice: Gain +1 Attack",
                               "common", "Test", 1, 2, 9, 0,
                               new[] { "Artifice" }, subtype: "Infantry");
        var artB = new CardDef("W59ArtB", "W59ArtB", "unit", "Artifice: Gain +1 Attack",
                               "common", "Test", 1, 2, 9, 0,
                               new[] { "Artifice" }, subtype: "Infantry");
        var spell = Tactic("W59Spell", 0, "Deal 1 damage to an enemy");
        CheckTrue(artA.TriggerOps("artifice") != null && artB.TriggerOps("artifice") != null,
                  "🧪 夹具自检：两条 `Artifice:` 正文都收下来了（收不到 ⇒ 下面全假绿）");

        var ctx = ProbeBattle(new[] { spell }, new[] { Unit("W59EFoe", 1, 0, 30) });
        ToP1Turn(ctx, 2);
        var a = Place(ctx, 0, 3, artA, exhausted: true);   // 先落：贴督军（格号 3）
        var b = Place(ctx, 0, 2, artB, exhausted: true);   // 后落：外一格（格号 2）
        Place(ctx, 1, 3, Unit("W59EFoe", 1, 0, 30), exhausted: true);
        CheckTrue(a.PlayOrder >= 0 && b.PlayOrder >= 0 && a.PlayOrder < b.PlayOrder,
                  "🧪（前提）进场号 A(" + a.PlayOrder + ") < B(" + b.PlayOrder + ")；格号 A=3 > B=2（两键相反）");

        int logFrom = ctx.Events.Count;
        int hi = HandIdx(ctx, 0, "W59Spell");
        CheckTrue(hi >= 0, "🧪（前提）手里有那张战术卡" + LogTail(ctx));
        if (hi < 0) return;
        CheckCode(RuleCore.PlayTactic(ctx, 0, hi, 3), RuleCodes.OK, "打出一张战术卡（触发两家的 Artifice）");

        int ia = W59LogIdxAfter(ctx, logFrom, "W59ArtA 触发 ARTIFICE");
        int ib = W59LogIdxAfter(ctx, logFrom, "W59ArtB 触发 ARTIFICE");
        CheckTrue(ia >= 0 && ib >= 0,
                  "🧪（前提）两个 Artifice 都真的响了" + LogTail(ctx));
        CheckTrue(ia >= 0 && ib >= 0 && ia < ib,
                  "★★ **`FireTriggerOnSide` 也按进场序**（先落场的 A 先响；" + ia + " < " + ib + "）—— "
                + "判据 = 原版 `BroadcastTacticPlayed` / 关键词广播同样走 `allCardsInPlay`；"
                + "🧨 把 `RuleCore.FireTriggerOnSide` 里那句 `StableSortByPlayOrder(found, …)` 删掉"
                + "（退回格号升序）⇒ 实得 B（格号 2）先 ⇒ 本行红");
    }
}
