// B105_W2_Assert.cs — 执行代理 `B105_W2`（2026-10-31）的断言：第 0 位 **0-2** + **0-3**
//
// ============================ 它盯的是什么 ============================
//  **0-2 · `disappearFromHand(56)`（`chosenAbility 162`）的 Return 表现**
//    原版 `BattleManager._ResolveDisappearFromHand_d__545__MoveNext.c:96-130`（溶解特效 + `DissolveCardInHand`）
//    → `:134` `CardScript.DestroyCard(card, 0.3f, 1, 0)` → `CardScript__DissolveWholeCard.c`。
//    我们：引擎发 `EvtKind.Return` + `Effect == "disappearFromHand"`；表现 = `BattleDriver.PlayDisappearFromHandFeel`
//    （`CardFeel.DissolveInHand`），时长 = `EventTiming.ReturnHoldOf` 的 0.3 s。
//  **0-3 · `displayFromHand(57)`（`chosenAbility 310`）的墓地动作日志 + 中央亮牌**
//    原版 `_ResolveDisplayFromHand_d__548__MoveNext.c:191`（`AddDisplayedCardActionToCemetery` = 墓地日志 `displayedCard(30)`）
//    → `:193/:195`（`SetCardState(10)` + `DisplayEnemyCardInCenter(0.3f,0.3f)`）。
//    我们：`RuleCore.ResolveDisplayFromHand` 发一条 `EvtKind.Draw` + `Effect == "display"`
//    （真容器 = `ctx.ActionLog`；日志行 = `BattleDriver.RefreshBattleLog` 的 display 支；
//     中央亮牌 = `BattleDriver.MaybeDisplayHandCardInCenter`），时长 = 2.7 s（2.5 + 0.2）。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//   · 把 `EventTiming.ReturnHoldOf` 里 `Effect == "disappearFromHand"` 那一档删/改成 0 ⇒ **A162b** 红。
//   · 把 `EventTiming` 里 **所有** `EvtKind.Return` 一刀切到一个数（例如 0.3）⇒ **A162c**（普通回手必须仍是别的数）红。
//   · 把 `ResolveDisplayFromHand` 里那句 `ctx.Emit(EvtKind.Draw, …, effect: "display", …)` 删掉 ⇒ **A310b/A310c** 红。
//   · 把 `EventTiming.DurationOf(BattleEvent)` 里 `Effect == "display"` 那一档删掉（落回抽牌的 0.3/0.15）⇒ **A310d** 红。
//   · 把 `PlayFeel` 的 `case EvtKind.Return` 改回 `PlayReturnFeel(e)`（= 关掉溶解）⇒ **A162P** 红
//     （走的是 `PlayFeelForTest`，即**真实路由**，⛔ 不是直接调那个方法）。
//   · 把 `PlayDisappearFromHandFeel` 那道「还在手牌里 ⇒ 不是它」的闸拆掉 ⇒ **A162Q** 红（会把还在手上的卡溶掉）。
//   · 把 `BattleDriver` 里挂 `MaybeDisplayHandCardInCenter` 的 else-if 拆掉 ⇒ **A310P** 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   接在 `CardPresentation/Editor/BattleScene.cs` 的**方法尾聚合行**
//   `Debug.Log(P + $"=== 合计：{pass} 通过 / {fail} 失败 ===");` **之前**，同一物理行追加：
//     `B105_W2_Assert.RunEngine(); pass += B105_W2_Assert.Pass; fail += B105_W2_Assert.Fail; if (driver != null) { B105_W2_Assert.RunPresentation(driver); pass += B105_W2_Assert.Pass; fail += B105_W2_Assert.Fail; }`
//   （同一物理行追加 ⇒ 行数零变化；⚠️ 追加前先看该行第一个 `//`，文本必须落在它**之前**。）
//   ⚠️ **`RunPresentation` 会短暂改一动手牌并 `RefreshAll()` 复原** ⇒ 只能在**最后**跑
//      （接在【合计】之前正好是最后一句）。
using System.Collections.Generic;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>账 `战斗逻辑驱动:1282`/`:1380` 的尾巴 —— 0-2 `disappearFromHand(56)` / 0-3 `displayFromHand(57)` 的表现与日志。</summary>
public static class B105_W2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B105W2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败的条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 夹具

    static CardDef Hero(string id) =>
        new CardDef(id, id, "hero", "", null, "Test", 0, 2, 30, 0, null, subtype: "Warlord");

    static CardDef PlainUnit(string id) =>
        new CardDef(id, id, "unit", "", "common", "Test", 1, 1, 1, 0, (string[])null);

    static BattleContext NewCtx()
    {
        var d0 = new List<CardDef> { Hero("B105W2_W0") };
        for (int i = 0; i < 20; i++) d0.Add(PlainUnit("B105W2_f" + i));
        var d1 = new List<CardDef> { Hero("B105W2_W1") };
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    static int CountDisplayRows(BattleContext ctx)
    {
        int n = 0;
        for (int i = 0; i < ctx.ActionLog.Count; i++)
        {
            var e = ctx.ActionLog[i];
            if (e != null && e.Kind == EvtKind.Draw && e.Effect == "display") n++;
        }
        return n;
    }

    static BattleEvent LastDisplayRow(BattleContext ctx)
    {
        for (int i = ctx.ActionLog.Count - 1; i >= 0; i--)
        {
            var e = ctx.ActionLog[i];
            if (e != null && e.Kind == EvtKind.Draw && e.Effect == "display") return e;
        }
        return null;
    }

    // ============================================================ 引擎侧入口

    public static void RunEngine()
    {
        MenuCheck.Section(_sink, "B105W2：0-2 `disappearFromHand(56)` 的时长与事件 · 0-3 `displayFromHand(57)` 的日志容器与通道");
        Case162();
        Case310();
    }

    // ------------------------------------------------------------ 0-2

    static void Case162()
    {
        Debug.Log(_sink.P + "--- 0-2：`disappearFromHand(56)`（`chosenAbility 162`）---");

        var ctx = NewCtx();
        int me = ctx.Active;
        ctx.Players[me].Hand.Clear();
        var card = ctx.NewInstance(PlainUnit("B105W2_162a"));
        ctx.Players[me].Hand.Add(card);
        int log0 = ctx.ActionLog.Count;

        // ---- ⓪ 前提（硬检查，⛔ 不是 `if (x != null)` 静默跳过）----
        Tr(ctx.Players[me].Hand.Count == 1, "（前提）A-162 夹具：手牌里正好 1 张");
        Tr(ctx.Players[me].Hand.Contains(card), "（前提）A-162 夹具：就是我们要的那一份实例");

        int rc = RuleCore.ResolveDisappearFromHandForTest(ctx, me, card);
        C(rc, 1, "★ A-162a 出队处理口：在手牌里 ⇒ 返回 1（真的把这张送走了）");

        // ---- ① 表现层唯一入口：那条事件 ----
        Tr(ctx.ActionLog.Count > log0, "★ A-162b 留档里多了条目（`ctx.Emit` 真发了）");
        BattleEvent ev = null;
        for (int i = ctx.ActionLog.Count - 1; i >= log0; i--)
        {
            var x = ctx.ActionLog[i];
            if (x != null && x.Kind == EvtKind.Return && x.Effect == "disappearFromHand") { ev = x; break; }
        }
        Tr(ev != null, "★★ A-162b `EvtKind.Return` + `Effect == \"disappearFromHand\"` 那条**真的发了**"
                     + "｜🧨 把 `RuleCore.ResolveDisappearFromHand` 里 `ctx.Emit(... effect: \"disappearFromHand\")` 改成别的 Effect ⇒ 这里取不到 ⇒ 红");
        if (ev == null) return;

        // ---- ② 时长（⛔ 不许零时长，也不许落进「回手 0.75」）----
        float hold = EventTiming.DurationOf(ev);
        C(hold, CardFeel.HandDissolveBlockSeconds,
          "★★ A-162c `EventTiming.DurationOf(162 事件)` == 0.3 s（= `_ResolveDisappearFromHand:134` 的 `DestroyCard(card, 0.3f, 1, 0)`）"
        + "｜🧨 删掉 `ReturnHoldOf` 里 `Effect == \"disappearFromHand\"` 那一档 ⇒ 落回 `ReturnGraveHold`（也是 0.3，见 A-162d 的分辨）");
        Tr(hold > 0f, "★★ A-162c 时长**不是 0**（原文「牌凭空没了」的病根之一）");
        Tr(Mathf.Abs(hold - (EventTiming.ReturnCardAnimHold + EventTiming.ReturnHandRelayoutHold)) > 1e-3f,
           "★★ A-162c 时长**不是**回手那一档 0.75 s（162 不是「回手」）");

        // ---- ③ 灭自证：**别的** Slot<0 的 Return 不许被一起改成 0.3 ----
        var ghost = new BattleEvent { Kind = EvtKind.Return, Player = me, Slot = -1,
                                      CardId = "B105W2_none", Effect = "ephemeral" };
        C(EventTiming.DurationOf(ghost), 0f,
          "★★ A-162d **灭自证（一刀切）**：`Effect == \"ephemeral\"` 的 Return 仍是 0 s"
        + "｜🧨 把 `Slot < 0` 那一支整段改成常量 ⇒ 这条红");

        // ---- ④ 牌真的离开了手牌（不是「只有事件、牌还在」）----
        Tr(!ctx.Players[me].Hand.Contains(card), "★ A-162e 手牌里**不再有**那一份实例");
    }

    // ------------------------------------------------------------ 0-3

    static void Case310()
    {
        Debug.Log(_sink.P + "--- 0-3：`displayFromHand(57)`（`chosenAbility 310`）---");

        var ctx = NewCtx();
        int me = ctx.Active;
        ctx.Players[me].Hand.Clear();
        var shown = ctx.NewInstance(PlainUnit("B105W2_310a"));
        ctx.Players[me].Hand.Add(shown);

        int disc0 = ctx.Players[me].Discard.Count, rem0 = ctx.Removed.Count;
        int rows0 = CountDisplayRows(ctx);

        // ---- ⓪ 前提 ----
        C(ctx.Players[me].Hand.Count, 1, "（前提）A-310 夹具：手牌里正好 1 张");
        C(rows0, 0, "（前提）A-310 夹具：留档里还没有 `display` 行");

        int rc = RuleCore.ResolveDisplayFromHandForTest(ctx, me, shown);
        C(rc, 1, "★ A-310a 出队处理口：在手牌里 ⇒ 返回 1（真的展示了）");

        // ---- ① **真容器**：留档里多出 `displayedCard(30)` 那一行 ----
        int rows1 = CountDisplayRows(ctx);
        C(rows1, rows0 + 1, "★★ A-310b 留档（`ctx.ActionLog`）里**多了一条** `EvtKind.Draw` + `Effect == \"display\"`"
                          + "｜🧨 删掉 `ResolveDisplayFromHand` 里那句 `ctx.Emit(EvtKind.Draw, …, effect: \"display\", …)` ⇒ 实得 " + rows0 + " ⇒ 红");
        var row = LastDisplayRow(ctx);
        Tr(row != null, "★★ A-310b 取得到那一行");
        if (row != null)
        {
            C(row.CardId, (string)"B105W2_310a", "★ A-310b 那一行带的是**被展示的那张牌**");
            C(row.Slot, -1, "★ A-310b `Slot` = -1（不在场上）");
            C(row.Player, 1 - me, "★★ A-310b **`Player` = 看牌那一方**（牌主的对面）—— `RefreshBattleLog` 用它选「你展示了敌人手中的… / 对手展示了你手中的…」两条键");
            C(row.TargetPlayer, me, "★★ A-310b **`TargetPlayer` = 牌主**（中央亮牌的起飞侧，`MaybeDisplayHandCardInCenter` 用它算 `owner`）");

            // ---- ② 时长 ----
            C(EventTiming.DurationOf(row), CardFeel.DisplayFromHandBlockSeconds,
              "★★ A-310d `EventTiming.DurationOf(display 事件)` == 2.7 s（`_ResolveDisplayFromHand:199` 的 `twoHalfSecDelay` 2.5 + `:238` 的 `twoFractDelay` 0.2）"
            + "｜🧨 删掉 `DurationOf(BattleEvent)` 里 `Effect == \"display\"` 那一档 ⇒ 落回抽牌 0.3（我方档）⇒ 红");
            Tr(EventTiming.DurationOf(row) > 2f, "★★ A-310d 它**不是**抽牌那一档（0.3 / 0.15）");
        }

        // ---- ③ 牌**自始至终留在手牌里**（⛔ 别做成弃置/移出游戏）----
        C(ctx.Players[me].Hand.Count, 1, "★★ A-310c **灭自证**：手牌**还是 1 张**（原版这一支从头到尾不摘牌）");
        Tr(ctx.Players[me].Hand.Contains(shown), "★★ A-310c 手牌里那一份**就是同一份实例**");
        C(ctx.Players[me].Discard.Count, disc0, "★★ A-310c **灭自证**：弃牌堆没动（展示 ≠ 弃置）");
        C(ctx.Removed.Count, rem0, "★★ A-310c **灭自证**：「移出游戏」没动（展示 ≠ 消失）");
    }

    // ============================================================ 表现侧入口

    /// <summary>表现侧断言。⚠️ **会短暂改一动手牌并 `RefreshAll()` 复原** ⇒ 必须在宿主最后跑。</summary>
    public static void RunPresentation(BattleDriver driver)
    {
        MenuCheck.Section(_sink, "B105W2：表现侧 —— 手牌溶解的路由 / 闸 · 中央亮牌");

        Tr(driver != null, "（前提）P-0 拿到 `BattleDriver`");
        if (driver == null) return;
        Tr(driver.Ctx != null, "（前提）P-0 驱动里有对局");
        if (driver.Ctx == null) return;

        int me = driver.MySeat;
        var views = driver.HandViewsForTest;
        Tr(views != null && views.Count > 0, "（前提）P-0 我方手牌视图 ≥ 1");
        if (views == null || views.Count == 0) return;

        // ---- ① 闸：**还在手牌里**的那张 ⇒ 不溶 ----
        int feels0 = driver.DisappearFromHandFeelsForTest;
        var keep = views[0];
        if (keep != null && !string.IsNullOrEmpty(keep.Data.id))
        {
            var evKeep = new BattleEvent { Kind = EvtKind.Return, Player = me, Slot = -1,
                                           CardId = keep.Data.id, Effect = "disappearFromHand" };
            driver.PlayFeelForTest(evKeep);
            C(driver.DisappearFromHandFeelsForTest, feels0,
              "★★ A-162Q **灭自证（闸）**：事件指着的那张**还在手牌里** ⇒ **不许**溶它（计数不动）"
            + "｜🧨 拆掉 `PlayDisappearFromHandFeel` 里「还在手牌里 ⇒ 跳过」那一句 ⇒ 计数 +1 ⇒ 红");
        }

        // ---- ② 路由 + 正例：把最后一张**真的**从引擎手牌摘掉（模拟 `ResolveDisappearFromHand` 已做的那一步）----
        int idx = views.Count - 1;
        var hit = views[idx];
        Tr(hit != null, "（前提）P-1 取到要溶的那张手牌视图");
        if (hit == null) return;
        var inst = hit.Inst;
        Tr(inst != null, "（前提）P-1 那张视图挂着引擎实例");
        if (inst == null) return;
        int handIdx = driver.Ctx.Players[me].Hand.IndexOf(inst);
        Tr(handIdx >= 0, "（前提）P-1 该实例此刻在引擎手牌里");
        if (handIdx < 0) return;

        int beforeViews = views.Count;
        driver.Ctx.Players[me].Hand.RemoveAt(handIdx);      // 引擎侧那一步（原版 `RemoveCardFromHand`）
        try
        {
            int feels1 = driver.DisappearFromHandFeelsForTest;
            var evHit = new BattleEvent { Kind = EvtKind.Return, Player = me, Slot = -1,
                                          CardId = hit.Data.id, Effect = "disappearFromHand" };
            // 🔴 **走真实路由**（`PlayFeel` 的 `case EvtKind.Return` 那一行）——
            //    改回 `PlayReturnFeel(e)` 的话它什么都不做 ⇒ 下面两条红。
            driver.PlayFeelForTest(evHit);

            C(driver.DisappearFromHandFeelsForTest, feels1 + 1,
              "★★ A-162P **路由**：`EvtKind.Return` + `Effect == \"disappearFromHand\"` 走到了 `PlayDisappearFromHandFeel`"
            + "｜🧨 把 `PlayFeel` 的 `case EvtKind.Return` 改回 `PlayReturnFeel(e)`（= 关掉溶解）⇒ 计数不动 ⇒ 红");
            C(driver.HandViewsForTest.Count, beforeViews - 1,
              "★★ A-162P 那张视图**从 `_handViews` 里摘掉了**（否则 `SyncHand` 会当场 `Kill` ⇒ 溶解一帧都看不见）");
            Tr(ReferenceEquals(driver.LastDisappearFromHandViewForTest, hit), "★ A-162P 记下来的就是这一张");
            C(driver.LastDisappearFromHandSeatForTest, me, "★ A-162P 座位记的是我方");
        }
        finally
        {
            // 复原：把实例放回引擎手牌原位并重同步（本件必须在宿主最后跑，见文件头）
            driver.Ctx.Players[me].Hand.Insert(Mathf.Clamp(handIdx, 0, driver.Ctx.Players[me].Hand.Count), inst);
            driver.RefreshAll();
        }

        // ---- ③ 中央亮牌：一条 `display` 事件 ⇒ 真的建了一张中央展示卡 ----
        var hv = driver.HandViewsForTest;
        if (hv != null && hv.Count > 0 && hv[0] != null && !string.IsNullOrEmpty(hv[0].Data.id))
        {
            int center0 = driver.PlayedCenterDisplaysForTest;
            var evShow = new BattleEvent { Kind = EvtKind.Draw, Player = 1 - me, Slot = -1,
                                           CardId = hv[0].Data.id, Effect = "display", TargetPlayer = me };
            driver.MaybeDisplayHandCardInCenterForTest(evShow);
            C(driver.PlayedCenterDisplaysForTest, center0 + 1,
              "★★ A-310P **中央亮牌**：`EvtKind.Draw` + `Effect == \"display\"` ⇒ `DisplayEnemyCardInCenter` 真的建了一张"
            + "｜🧨 删掉 `BattleDriver` 里 `MaybeDisplayHandCardInCenter` 的实现 / 让它直接 return ⇒ 计数不动 ⇒ 红");
            C(driver.LastPlayedCenterOwnerForTest, me, "★ A-310P 起飞侧 = 牌主（`TargetPlayer`）");
        }
        else
        {
            // ⛔ 不静默：「前提不成立」本身是一条失败（铁律：先写前提硬检查）
            Tr(false, "（前提）P-2 我方手牌视图里有一张带 id 的卡（拿不到 ⇒ 这条断言**红**，⛔ 不是跳过）");
        }
    }
}
