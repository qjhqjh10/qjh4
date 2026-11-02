// B121_E04B_Assert.cs — 执行写手 `B121-E04B`（第一百二十一会话）的断言
//
// ============================ 它盯的是什么 ============================
//   活状态表 `B120-E04b`：E04（`ReshuffleCemeteryIntoDeck`，墓地洗回牌库）**表现层那一串**原来没接 ——
//   原版那四段 `CardScript.UndissolveToDeck` / `BattleCardUI.ShowBoardObjects` /
//   `BattleCardUI.SetObjectVisibility` / `CardScript.MoveToDeck` 在我们这边**没有任何事件载体**。
//   本件钉的是「引擎发得对 + 表现层认得出并真的走了那条演出」。
//
// ============================ 判据（路径，⛔ 别在这里抄第二份）============================
//   · `decomp_full/BattleManager._ResolveDrawCard_d__432__MoveNext.c:89-123`（洗回那一支四段调用次序）
//     ＋ `:241` 的 `case 1`（`MoveToDeck(card, 0, 0.05, 0)` → 等它的返回值 → `case 2` 才进牌库数据）
//   · `decomp_full/CardScript__UndissolveToDeck.c` · `BattleCardUI__ShowBoardObjects.c` ·
//     `BattleCardUI__SetObjectVisibility.c` · `CardScript__MoveToDeck.c`（四段方法体全文）
//   · 时长：`DAT_1834b2dc4` = 0.1（`CreateCardInGame` 的等待）· `VarsGlobal+0x78` = 0.1 · `DAT_1834b2f94` = 0.05
//     ⇒ 一张卡 0.25 s（`CardFeel.ReshuffleToDeckSeconds` 的 doc 里逐条写着）
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//   🧨 1：A 组（`shouldReshuffle = 真`）与 B 组（`假`）是**同一个输入的两个状态** ——
//         把 `!ctx.Vars.shouldReshuffle` 那道闸删掉（恒洗回）⇒ B1 红；改成恒 false ⇒ A1/A2 红。
//   🧨 2：把发出点从「回填那一句」挪到「非开局牌只删」那一支 ⇒ A4（`FixtureJunk` 不许有条目）红。
//   🧨 3：把 `ReshuffleToDeckPlanOf` 的两道闸（`Slot < 0` / `Effect`）删掉 ⇒ C2/C3/C4 红
//         （普通回手、手牌溶解、墓地取回都会被它抢走 ⇒ 那三条链静默改道）。
//   🧨 4：把 `PlayFeel` 的路由删掉 ⇒ D2 红；把消费端的 `CardFeel.ReturnToDeck` 换成别的调用 ⇒ D3 红。
//
// ============================ 怎么接 ============================
//   宿主聚合行末尾追加（**同一物理行** ⇒ 行数零变化）：
//      B121_E04B_Assert.Run(); pass += B121_E04B_Assert.Pass; fail += B121_E04B_Assert.Fail;
//   ⚠️ **零前提**：自己 `RuleCore.NewBattle` 建局，不依赖活 driver / HUD（同族 `B120_W29_Assert`）。
//   ⚠️ 表现层那四个读口靠**源码闸**（`D` 组）覆盖 —— 真正建卡播补间要活 HUD，那一层由调度台在同步点验。
//   ⚠️ 未跑 Unity（子代理红线）：本文件的编译与运行由调度台在同步点验。
using System;
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;
using CardPresentation;

/// <summary>执行写手 `B121-E04B`：`B120-E04b`（墓地洗回牌库的表现层事件链 + 消费端判别式）。</summary>
public static class B121_E04B_Assert
{
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    const string P = "[B121E04B] ";

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
        Debug.Log(P + "=== B121_E04B 开始（`B120-E04b` 墓地洗回牌库的表现层）===");
        try
        {
            GroupA_Emit();
            GroupB_NoEmit();
            GroupC_ConsumerPlan();
            GroupD_SourceWiring();
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
    //  A · 真档：`shouldReshuffle = 真` ⇒ 每张回填的卡**恰好一条**事件
    // ==================================================================
    static void GroupA_Emit()
    {
        Debug.Log(P + "--- A · 真档：洗回时按张发 `Return` + `Effect = reshuffleToDeck` ---");
        var ctx = NewFixtureBattle();
        var ps = ctx.Players[0];
        var k1 = TakeDeckTopToCemetery(ps);
        var k2 = TakeDeckTopToCemetery(ps);
        var junk = ctx.NewInstance(Unit("FixtureJunk", 1, 1, 1));
        ps.Discard.Add(junk);
        Check(k1 != null && k2 != null && ps.Discard.Count == 3,
              $"[A0 前提] 墓地里 3 张（2 张开局牌 + 1 张造物；实得 {ps.Discard.Count}）");
        if (k1 == null || k2 == null)
        {
            Check(false, "[A0b] 夹具没能从牌库拿两张牌 ⇒ 本组余下断言无法执行（先修夹具）");
            return;
        }
        Check(ctx.InitialDeckInstanceIds.Contains(k1.Id) && ctx.InitialDeckInstanceIds.Contains(k2.Id)
              && !ctx.InitialDeckInstanceIds.Contains(junk.Id),
              "[A0c 前提] 两张带「开局那份」章、`FixtureJunk` 不带（不带章的只删不回填）");

        ps.Deck.Clear();
        ctx.Vars.shouldReshuffle = true;                   // ← 只改这一局的副本（产品默认值仍是 false，见 B 组）
        int hand0 = ps.Hand.Count;
        ctx.DrainSignals(null);                            // 清掉建局那一段的信号

        RuleCore.Draw(ctx, 0);

        var buf = new List<BattleEvent>();
        ctx.DrainSignals(buf);
        var re = new List<BattleEvent>();
        for (int i = 0; i < buf.Count; i++)
            if (buf[i] != null && buf[i].Kind == EvtKind.Return
                && buf[i].Effect == BattleEventEffect.ReshuffleToDeck) re.Add(buf[i]);

        Eq(re.Count, 2, "★ A1 **每张回填的卡恰好一条事件**（原版那个 `while` 是「洗回一张 → 等它演完 → 下一张」；"
                      + "本局回填 2 张 ⇒ 2 条）｜🧨 闸恒关 ⇒ 本行红");
        bool allOffBoard = re.Count == 2;
        for (int i = 0; i < re.Count; i++) if (re[i].Slot != -1) allOffBoard = false;
        Check(allOffBoard, "★ A1b 两条的 `Slot` **恒 -1**（它们不在棋盘上 —— 原版那四段都作用在墓地那一份上）");

        bool player0 = re.Count == 2;
        for (int i = 0; i < re.Count; i++) if (re[i].Player != 0) player0 = false;
        Check(player0, "★ A2 两条的 `Player` 都是 0（牌库抽空、洗回的那一方）");

        bool namesOk = re.Count == 2;
        for (int i = 0; i < re.Count; i++)
            if (!string.Equals(re[i].CardId, k1.Card.Name, StringComparison.Ordinal)
                && !string.Equals(re[i].CardId, k2.Card.Name, StringComparison.Ordinal)) namesOk = false;
        Check(namesOk, "★ A2b `CardId` 是那两张开局牌之一（不是别的卡）");

        bool idsOk = re.Count == 2;
        for (int i = 0; i < re.Count; i++)
            if (re[i].InstanceId != k1.Id && re[i].InstanceId != k2.Id) idsOk = false;
        Check(idsOk, "★ A3 `InstanceId` 与墓地那两份**对得上**（同名卡不会记错到别的一份上）");

        bool junkLeaked = false;
        for (int i = 0; i < re.Count; i++)
            if (string.Equals(re[i].CardId, "FixtureJunk", StringComparison.Ordinal)) junkLeaked = true;
        Check(!junkLeaked, "★ A4 **反证**：非「开局那份」的 `FixtureJunk` **不发**事件 —— 原版那一支只"
                         + "`RemoveFromCemeteryList`、不回牌库（`:169-173`）｜🧨 把发出点挪到那一支 ⇒ 本行红");

        Eq(ps.Hand.Count, hand0 + 1, "★ A5 洗回之后照原版**继续抽**（`:197` 回到 `DrawCardFromDeck`）⇒ 手牌 +1");
        Eq(ps.Deck.Count, 1, "★ A5b 两张回填 − 抽走一张 ⇒ 牌库剩 1 张（回填落位是随机的，但张数固定）");
    }

    // ==================================================================
    //  B · 假档（产品默认值）：一条都不许发
    // ==================================================================
    static void GroupB_NoEmit()
    {
        Debug.Log(P + "--- B · 假档（产品默认值）：空库只走疲劳，不发洗回事件 ---");
        var ctx = NewFixtureBattle();
        var ps = ctx.Players[0];
        var c1 = TakeDeckTopToCemetery(ps);
        Check(c1 != null && ctx.InitialDeckInstanceIds.Contains(c1.Id),
              "[B0 前提] 墓地里那张带「开局那份」章 —— 连它都**不许**洗回（闸看的是模式，不是卡）");
        if (c1 == null) { Check(false, "[B0b] 夹具没能从牌库拿一张牌 ⇒ 本组余下断言无法执行"); return; }
        ps.Deck.Clear();
        ctx.DrainSignals(null);

        RuleCore.Draw(ctx, 0);

        var buf = new List<BattleEvent>();
        ctx.DrainSignals(buf);
        int n = 0;
        for (int i = 0; i < buf.Count; i++)
            if (buf[i] != null && buf[i].Kind == EvtKind.Return
                && buf[i].Effect == BattleEventEffect.ReshuffleToDeck) n++;
        Eq(n, 0, "★ B1 假档：**一条洗回事件都没有**（原版 `:58-68` 那一支只 `ResolveFatigue`）｜🧨 闸恒开 ⇒ 本行红");
        Eq(ps.Fatigue, 1, "★ B2 确实走了疲劳 +1（证明这一局真的抽空了、不是「没抽牌所以没事件」）");
        Check(GameplayVariables.Classic.shouldReshuffle == false && GameplayVariables.Skirmish.shouldReshuffle == false,
              "★ B3 **灭自证**：两个模式的产品默认值仍是 false（A 组证明「闸能开」、本组证明「默认关」）");
    }

    // ==================================================================
    //  C · 消费端判别式（纯函数 —— 生产与自检同一份）
    // ==================================================================
    static void GroupC_ConsumerPlan()
    {
        Debug.Log(P + "--- C · 消费端：只有「洗回那一趟」才归 `PlayReshuffleToDeckFeel` ---");
        var real = new BattleEvent { Kind = EvtKind.Return, Player = 1, Slot = -1,
                                     CardId = "FixtureCard", InstanceId = 42,
                                     Effect = BattleEventEffect.ReshuffleToDeck };
        var plan = BattleDriver.ReshuffleToDeckPlanOf(real, me: 0);
        Check(plan.Valid && plan.Player == 1 && !plan.IsLocal && plan.CardId == "FixtureCard",
              "★ C1 洗回那条 ⇒ `Valid`，且 `IsLocal = false`（座位 1 不是本机 0 ⇒ 原版敌方那一支换卡背）");
        Check(Mathf.Abs(plan.StartPos.x - CardFeel.CemeteryCardX) < 1e-4f
              && Mathf.Abs(plan.StartPos.y - CardFeel.CemeteryCardY) < 1e-4f
              && Mathf.Abs(plan.StartScale - CardFeel.CemeteryCardScale) < 1e-4f
              && Mathf.Abs(plan.StartScale - 0.65f) < 1e-4f,
              $"★ C1b 起点 = 原版墓地那张卡的位姿（{plan.StartPos.x:F2}, {plan.StartPos.y:F2}）· 缩放 {plan.StartScale:F2}"
            + "（`VarsGlobal.cardInCemeteryPos / cardInCemeteryScale`）");
        Check(Mathf.Abs(plan.Seconds - CardFeel.ReshuffleToDeckSeconds) < 1e-4f
              && Mathf.Abs(plan.Seconds - 0.25f) < 1e-4f,
              $"★ C1c 时长 = {plan.Seconds:F2}s（`CreateCardInGame` 0.1 + `MoveToDeck` 0.15，判据见那个常量的 doc）");

        var board = new BattleEvent { Kind = EvtKind.Return, Player = 0, Slot = 4, CardId = "X" };
        Check(!BattleDriver.ReshuffleToDeckPlanOf(board, 0).Valid,
              "★ C2 **反证**：棋盘上的回手/回牌库（`Slot = 4`）**不归本路**（它仍走 `PlayReturnFeel` 的倒放）"
            + "｜🧨 删掉 `Slot < 0` 那道闸 ⇒ 本行红");

        var gone = new BattleEvent { Kind = EvtKind.Return, Player = 0, Slot = -1, CardId = "X",
                                     Effect = "disappearFromHand" };
        Check(!BattleDriver.ReshuffleToDeckPlanOf(gone, 0).Valid,
              "★ C3 **反证**：`disappearFromHand`（手牌溶解，`B105_W2`）不归本路 ｜🧨 删掉 `Effect` 那道闸 ⇒ 本行红");

        var grave = new BattleEvent { Kind = EvtKind.Return, Player = 0, Slot = -1, CardId = "X" };
        Check(!BattleDriver.ReshuffleToDeckPlanOf(grave, 0).Valid,
              "★ C4 **反证**：墓地取回（`EffectResolver` 那条 `Slot = -1` 且**不带** `Effect` 的 `Return`）不归本路");

        var other = new BattleEvent { Kind = EvtKind.Death, Player = 0, Slot = 4, CardId = "X" };
        Check(!BattleDriver.ReshuffleToDeckPlanOf(other, 0).Valid && !BattleDriver.ReshuffleToDeckPlanOf(null, 0).Valid,
              "★ C5 **反证**：别的种类 / `null` 都不归本路");

        // 时间线分档：既有 `Return` 的 `Slot < 0` 一档已覆盖本趟（⛔ 不为它新开 `EvtKind` —— `EventTiming` 不在本批白名单）
        Check(CardFeel.ReshuffleToDeckSeconds <= EventTiming.ReturnGraveHold + 1e-4f,
              $"★ C6 本趟 {CardFeel.ReshuffleToDeckSeconds:F2}s ≤ 时间线给 `Return`/`Slot<0` 的分档 "
            + $"{EventTiming.ReturnGraveHold:F2}s（`ReturnGraveHold`）⇒ **装得下、不挤压下一条事件**");
        Check(EventTiming.IsRegisteredEventKind(EvtKind.Return) && EventTiming.IsSourced(EvtKind.Return)
              && Mathf.Abs(EventTiming.DurationOf(real) - EventTiming.ReturnGraveHold) < 1e-4f,
              "★ C7 载体用的是**已登记、已有出处**的 `EvtKind.Return` ⇒ 新增这一档**不改** `EventTiming` 的登记表/`unsourced` 读数（`BattleScene` A2·J2③ 不受影响）");
    }

    // ==================================================================
    //  D · 源码闸（结构性：引擎发得出、表现层接得上）
    // ==================================================================
    static void GroupD_SourceWiring()
    {
        Debug.Log(P + "--- D · 源码闸：发出点 / 路由 / 消费动作 / 静音 ---");
        string root = Application.dataPath;
        string[] lines;
        if (!LoadLines(root + "/RuleEngine/Core/RuleCore.cs", out lines)) return;
        bool emitAtInsert = false, literalInEngine = false;
        foreach (var raw in lines)
        {
            var t = raw.TrimStart();
            if (t.StartsWith("//")) continue;
            if (t.IndexOf("effect: BattleEventEffect.ReshuffleToDeck", StringComparison.Ordinal) >= 0
                && t.IndexOf("ps.Deck.Insert(", StringComparison.Ordinal) >= 0) emitAtInsert = true;
            if (t.IndexOf("\"reshuffleToDeck\"", StringComparison.Ordinal) >= 0) literalInEngine = true;
        }
        Check(emitAtInsert,
              "★ D1 `RuleCore.ReshuffleCemeteryIntoDeck` 的**回填那一句**上发 `EvtKind.Return` + `Effect = BattleEventEffect.ReshuffleToDeck`"
            + "（唯一发出点；⚠️ 与 `ps.Deck.Insert` **同一行** = 只在真回填时发）");
        Check(!literalInEngine,
              "★ D1b 引擎侧**没有**裸字面量 `\"reshuffleToDeck\"`（唯一出处 = `BattleEventEffect.ReshuffleToDeck`）"
            + "｜🧨 两边各写一遍字面量 ⇒ 改一处就静默不播");

        if (!LoadLines(root + "/CardPresentation/Battle/BattleDriver.cs", out lines)) return;
        bool routed = false, action = false, muted = false, literalInDriver = false;
        foreach (var raw in lines)
        {
            var t = raw.TrimStart();
            if (t.StartsWith("//")) continue;
            if (t.IndexOf("else if (e.Slot < 0 && e.Effect == BattleEventEffect.ReshuffleToDeck) PlayReshuffleToDeckFeel(e);",
                          StringComparison.Ordinal) >= 0) routed = true;
            if (t.IndexOf("CardFeel.ReturnToDeck(v, pile.transform, plan.Seconds", StringComparison.Ordinal) >= 0) action = true;
            if (t.IndexOf("} else if (e.Effect == BattleEventEffect.ReshuffleToDeck) {", StringComparison.Ordinal) >= 0) muted = true;
            if (t.IndexOf("\"reshuffleToDeck\"", StringComparison.Ordinal) >= 0) literalInDriver = true;
        }
        Check(routed, "★ D2 `PlayFeel` 的 `case EvtKind.Return` **真的路由**到 `PlayReshuffleToDeckFeel`"
                    + "（⛔ 不是只加了一个没人调的方法）｜🧨 删掉那段 `else if` ⇒ 本行红");
        Check(action, "★ D3 消费端**真的做了那一手**：`CardFeel.ReturnToDeck(v, pile.transform, plan.Seconds …)`"
                    + "（= 原版 `MoveToDeck` 的三条同长补间 + 收尾）｜🧨 换成别的调用 ⇒ 本行红");
        Check(muted, "★ D4 `PlayBattleSfx` 对这条**静音**（原版那四段方法体里没有 `BackToHand` 那条 cue；"
                   + "⛔ 不拿它顶上）");
        Check(!literalInDriver, "★ D4b 表现层也没有裸字面量（同 D1b）");
    }

    static bool LoadLines(string path, out string[] lines)
    {
        lines = null;
        if (!System.IO.File.Exists(path))
        {
            Check(false, "[前提] 读得到 `" + path + "` —— 读不到时本组等于没验 ⇒ 当场红");
            return false;
        }
        lines = System.IO.File.ReadAllLines(path);
        return true;
    }

    // ==================================================================
    //  夹具（⛔ 只为本件服务；与 `B120_W29_Assert` / `RuleEngineTest` 同形）
    // ==================================================================
    static BattleContext NewFixtureBattle()
    {
        return RuleCore.NewBattle(FixtureDeck(), FixtureDeck(), seed: 0, shuffle: false);
    }

    /// <summary>1 张督军 + 20 张垫牌（`shuffle:false` ⇒ 抽牌 = 从列表末尾取）。</summary>
    static List<CardDef> FixtureDeck()
    {
        var d = new List<CardDef> { Hero("FixtureWarlord", 2, 30) };
        for (int i = 0; i < 20; i++) d.Add(Unit("filler" + i, 1, 0, 1));
        return d;
    }

    /// <summary>把牌库顶那一张（我们的约定 = **列表末尾**）挪进墓地，返回它。</summary>
    static CardInstance TakeDeckTopToCemetery(PlayerState ps)
    {
        if (ps == null || ps.Deck.Count == 0) return null;
        var inst = ps.Deck[ps.Deck.Count - 1];
        ps.Deck.RemoveAt(ps.Deck.Count - 1);
        ps.Discard.Add(inst);
        return inst;
    }

    static CardDef Hero(string name, int atk, int hp)
    {
        return new CardDef(name, name, "hero", "", null, "Test", 0, atk, hp, 0, null);
    }

    static CardDef Unit(string name, int cost, int atk, int hp)
    {
        return new CardDef(name, name, "unit", "", null, "Test", cost, atk, hp, 0, null);
    }
}
