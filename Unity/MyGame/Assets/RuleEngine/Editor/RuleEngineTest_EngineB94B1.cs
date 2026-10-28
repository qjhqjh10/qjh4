// RuleEngineTest_EngineB94B1.cs — 第九十四会话 · 波 2 · 执行代理 `B1`
//   （`账/战斗逻辑驱动.tsv` 里落在 `PlayerState.cs`(9) 与 `BattleContext.cs`(5) 的那 14 条 B/C）
//
// 为什么另开一个文件：`RuleEngineTest` 是 `public static partial class`（兄弟分册一大堆），
// 同一时刻只有一个写手改 `RuleEngineTest.cs`（归 F1）⇒ 本批断言全写在这里、**只交一个入口方法名**，
// 由调度台在 `Run()` 里加一行 `Step(CheckB94B1EngineGaps)`（⛔ 本代理不改 F1 的文件）。
// 🔴 入口名 = **`CheckB94B1EngineGaps`**（⛔ 不以 `Test`/`Report` 打头）。
//
// 本批**唯一的行为改动** = `PlayerState.UnitsByPlayOrder(BattleContext)` 重载（原版
// `MinionManager.GetMinionList` 的 `IsInPlay()` 逐条过滤，判据 `MinionManager__GetMinionList.c:35/:78`
// ＋ `CardScript__IsInPlay.c`）。其余 13 条的判据 2026-10-29 逐条现读复核，**无代码改动**
// （明细见 `资料/完全复刻/报告/B94_B1.md`；账补丁 `账/_parts/B94_B1.tsv`）。
//
// 🔴 每条断言都带 `🧨`（**什么改动能让它红**）；期望值一律写死字面量，⛔ 不从被测实现反算。
using System;
using System.Collections.Generic;
using RuleEngine;

public static partial class RuleEngineTest
{
    /// <summary>本件的**唯一入口**（调度台在 `Run()` 里加一行 `Step(CheckB94B1EngineGaps)`）。</summary>
    public static void CheckB94B1EngineGaps()
    {
        Section("🆕 B1（B94 波 2）：兵列表 IsInPlay 过滤 · 进场号发号器 · bot 对手名字接入格");
        B94B1T1MinionListInPlayFilter();
        B94B1T2PlayOrderSequencer();
        B94B1T3BotOpponentName();
    }

    // ==============================================================================================
    //  ① `MinionManager.GetMinionList` 的 `IsInPlay()` 逐条过滤（账 14 的残差 · 本批唯一改动）
    //
    //  判据（第一权威，2026-10-29 现读）：
    //   · `decomp_full/MinionManager__GetMinionList.c:35`（左表 `+0x60`）/ `:78`（右表 `+0x68`）——
    //     **逐条** `CardScript.IsInPlay(card)` 为真才收，收完才 `Enumerable.OrderBy(orderInPlay)`；
    //   · `decomp_full/CardScript__IsInPlay.c` 全文 = `cardState(+0x228) ∈ {2, 15, 3, 17}` ⇒
    //     **`5 = waitingToDie` 为假**（枚举 `CardStateOptions`）。
    //  ⇒ 我们的等价物 = `!ctx.IsPendingDeath(u)`；**不带 `ctx`** 的那份拿不到这一格 ⇒ 会多收。
    // ==============================================================================================

    static void B94B1T1MinionListInPlayFilter()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        var ps = ctx.Players[0];
        var ua = new UnitState(ctx.NewInstance(Unit("B94B1A", 1, 1, 3)), false) { PlayOrder = 1 };
        var ub = new UnitState(ctx.NewInstance(Unit("B94B1B", 1, 1, 3)), false) { PlayOrder = 2 };
        var uc = new UnitState(ctx.NewInstance(Unit("B94B1C", 1, 1, 3)), false) { PlayOrder = 3 };
        ps.Board[0] = ua;
        ps.Board[1] = ub;      // 这一格等下要标成「待死」
        ps.Board[2] = uc;

        // ── 前提：三只都在场、都没待死 ⇒ 两份口径给出同一张表 ────────────────────────────────
        Check(ps.UnitsByPlayOrder().Count, 3,
              "① （前提）不带 ctx 的那份收到 3 只（B94B1A/B/C）");
        Check(ps.UnitsByPlayOrder(ctx).Count, 3,
              "★★ ① （前提）**没有**待死单位时，`UnitsByPlayOrder(ctx)` 与不带 ctx 的那份**逐字相同**"
            + "｜🧨 一上来就少收一只（把 `IsInPlay` 写成 `IsAlive` 之类）⇒ 红");

        // ── 把中间那只（格 1 · PlayOrder 2）标成「待死」（原版 `CardStateOptions.waitingToDie = 5`）──
        ctx.AddPendingDeath(0, 1, -1);
        CheckTrue(ctx.IsPendingDeath(ub), "① （前提）`AddPendingDeath(0,1)` 之后 `ub` 确实进了待死队列");
        CheckTrue(ps.Board[1] == ub,
                  "① （前提）待死单位**仍然挂在棋盘上**（原版 `waitingToDie` 也是这个窗口才需要 `IsInPlay` 判）"
                + "｜🧨 `AddPendingDeath` 顺手把棋盘清掉 ⇒ 红（那就测不出这一档了）");

        // ── 结论 1：`ctx` 版摘掉待死项 ─────────────────────────────────────────────────────
        var filtered = ps.UnitsByPlayOrder(ctx);
        Check(filtered.Count, 2,
              "★★ ① `UnitsByPlayOrder(ctx)` 摘掉待死项 ⇒ 只剩 2 只"
            + "｜判据 `MinionManager__GetMinionList.c:35/:78` 逐条 `IsInPlay()`"
            + "｜🧨 把重载去掉 / `RemoveAll` 写反 ⇒ 实得 3 ⇒ 红");

        // ── 结论 2：**次序保持**（摘中间的不会把后面的提前 / 打乱）──────────────────────────
        Check(filtered[0].Name, "B94B1A",
              "★★ ① 摘掉中间那只后，第 1 项仍是 `B94B1A`（PlayOrder 1）"
            + "｜🧨 用「重建列表再排序」写错键（改成按格号/InstanceId）⇒ 红");
        Check(filtered[1].Name, "B94B1C",
              "★★ ① 第 2 项是 `B94B1C`（PlayOrder 3）—— 仍按 `PlayOrder` 升序，不是格号序"
            + "｜🧨 同上 ⇒ 红");

        // ── 脱自证 1：`ctx == null` ⇒ 逐字退回不滤（老夹具/纯数据场景）──────────────────────
        Check(ps.UnitsByPlayOrder((BattleContext)null).Count, 3,
              "★★ ① `ctx == null` ⇒ 逐字等价于不带 ctx 的那份（3 只）"
            + "｜🧨 把 `null` 当「滤掉全部」⇒ 实得 0 ⇒ 红");

        // ── 脱自证 2：不带 ctx 的那份**没有**被改（两份口径不同，别混用）────────────────────
        Check(ps.UnitsByPlayOrder().Count, 3,
              "★★ ① 不带 ctx 的那份**仍然是 3**（它没有 ctx、拿不到待死格）"
            + "｜🧨 有人把 `IsInPlay` 过滤塞进不带 ctx 的那份（它做不到）⇒ 红；"
            + "🧨 两份被合并成一份 ⇒ 这里与上面「B94B1A/B94B1C」那条同时变化 ⇒ 红");
    }

    // ==============================================================================================
    //  ② `BattleManager.SetUnitOrderInPlay` 的发号器形状（账 9 · `BattleManager__SetUnitOrderInPlay.c`）
    //
    //  原版（`.c:21-48`）：把 `unitOrderInPlay(+0x3a8, ObscuredInt)` 的**当前值**写进 `card.orderInPlay(+0x44)`，
    //  **随后** `op_Increment` 写回计数器。⇒ 「取旧值、再 +1」= C# 的后置 `++`。
    //  ⚠️ 计数器在 `BattleManager` 上（**一局一份**，不是 static）—— 两局互不推进。
    //  ⚠️ `ObscuredInt` 那 5 个 dword 只服务反作弊探针（`op_Implicit` 只解密、`op_Increment` 只写
    //     hiddenValue/fakeValue/inited）⇒ **值上等价于我们的 `int`**（账 9 的「没核」就此闭合）。
    // ==============================================================================================

    static void B94B1T2PlayOrderSequencer()
    {
        var ctxA = Battle(new CardDef[0], new CardDef[0]);
        var ctxB = Battle(new CardDef[0], new CardDef[0]);

        int a1 = ctxA.TakePlayOrder();
        int a2 = ctxA.TakePlayOrder();

        Check(a2 - a1, 1,
              "★★ ② 同一局里连取两次 ⇒ 差 **1**（取当前值、随后自增 = 原版 `.c:30-48`）"
            + "｜🧨 改成先自增再返回（前置 `++`）⇒ 值整体偏 1 ⇒ 红；"
            + "🧨 一次跳 2（漏号）⇒ 红");

        // 两局各自一份计数器（原版 `+0x3a8` 挂在 `BattleManager` 实例上，不是静态）
        int b1 = ctxB.TakePlayOrder();
        Check(b1, a1,
              "★★ ② 两局**各自**一份发号器：同样夹具的第一号相等"
            + "｜🧨 把 `_playOrderSeq` 改成 `static` ⇒ 第二局接着第一局的号 ⇒ 实得 a1+2 ⇒ 红");

        CheckTrue(a2 > a1 && b1 == a1,
                  "★★ ② 号只增不减，且互不推进（同一条的联合判据）｜🧨 见上两条");
    }

    // ==============================================================================================
    //  ③ `BattleContext.ApplyBotOpponentName` / `BotOpponentName`（账 13 · `MatchData__SetBotOpponent.c`）
    //
    //  原版 `.c:88-101`：条件满足时 `NameGenerator.GenerateName(...)` 写进**对手那副牌**的
    //  `DeckAndWarlordData.deckName(+0x10)`。我们的等价载体 = `BotOpponentName` + 该座位 `PlayerState.Name`。
    //  ⚠️ 名字那一路用 `UnityEngine.Random` ⇒ **不进指纹**（引擎侧不生成名字，只留接入格）——
    //     本断言只锁「写哪两处 / 空串不写 / 没有 bot 座位时不碰座位名」。
    // ==============================================================================================

    static void B94B1T3BotOpponentName()
    {
        var ctx = Battle(new CardDef[0], new CardDef[0]);
        string p0 = ctx.Players[0].Name, p1 = ctx.Players[1].Name;

        // ── (a) 没有 bot 座位（`BotSeat < 0`）⇒ 只记名字、不碰座位 ─────────────────────────
        Check(ctx.BotSeat, -1, "③ （前提）这局没有 bot 座位（`BotSeat = -1`）");
        ctx.ApplyBotOpponentName("B94B1Bot");
        Check(ctx.BotOpponentName, "B94B1Bot", "★★ ③ 名字记进 `BotOpponentName`｜🧨 丢弃 ⇒ 红");
        Check(ctx.Players[0].Name, p0,
              "★★ ③ `BotSeat < 0` ⇒ **不碰**座位 0 的名字｜🧨 改成无条件写 `Players[0]` ⇒ 红");
        Check(ctx.Players[1].Name, p1,
              "★★ ③ `BotSeat < 0` ⇒ **不碰**座位 1 的名字｜🧨 无条件写 `Players[1]` ⇒ 红");

        // ── (b) 有 bot 座位 ⇒ 写进**该座位**的 `PlayerState.Name`（另一座不动）──────────────
        ctx.BotSeat = 1;
        ctx.ApplyBotOpponentName("B94B1Bot2");
        Check(ctx.Players[1].Name, "B94B1Bot2",
              "★★ ③ `BotSeat = 1` ⇒ 名字落到**座位 1** 的 `PlayerState.Name`（原版写对手那副牌的 deckName）"
            + "｜🧨 只写 `BotOpponentName` 不写座位（表现层三处不同源）⇒ 红；"
            + "🧨 写到座位 0 ⇒ 红");
        Check(ctx.Players[0].Name, p0,
              "★★ ③ 同一跳里**另一方**的名字不动｜🧨 两座都写 ⇒ 红");
        Check(ctx.BotOpponentName, "B94B1Bot2", "③ `BotOpponentName` 同步更新｜🧨 只写座位不写它 ⇒ 红");

        // ── (c) 空串 / null = 不写（原版那一跳不会写空名）──────────────────────────────────
        ctx.ApplyBotOpponentName("");
        Check(ctx.BotOpponentName, "B94B1Bot2",
              "★★ ③ 空串是 **no-op**（原版不会有「写空名」这一跳）｜🧨 把空串当合法名字 ⇒ 红");
        Check(ctx.Players[1].Name, "B94B1Bot2", "③ 空串同样不碰座位名｜🧨 见上 ⇒ 红");
        ctx.ApplyBotOpponentName(null);
        Check(ctx.BotOpponentName, "B94B1Bot2",
              "★★ ③ `null` 同样是 no-op｜🧨 `string.IsNullOrEmpty` 写成 `== null` 之类 ⇒ 红");
    }
}
