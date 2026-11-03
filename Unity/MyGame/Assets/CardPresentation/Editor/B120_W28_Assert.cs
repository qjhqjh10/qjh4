// B120_W28_Assert.cs — 执行写手 `B120-W28`（2026-11-03）· 账 `B120-Q04c4` + `B120-Q04c5`
//
// ============================ 它盯的是什么 ============================
//  A 段（`Q04c4`）· **松手落在出牌区外**（指针在 `HandLimitArea` 线以下）
//     原版那一刻：
//       · 松手支的动作 = `il2csharp/BattleManager.cs:25845` `cardTracked.MoveBackToHand()`
//         （`decomp_full/CardScript__MoveBackToHand.c:12-30`：`SetSortingLayer(Hand)` · `StopAnimsInPlay` ·
//          **`cardType == 0` ⇒ `DissolveMinionCard(false)`** · `{20,40}` 且 `targeted` ⇒ **`UnDissolveWholeCard()`**）；
//       · `decomp_full/CardScript__OutFromPlayingAreaAnim.c:11-42` 全文 = 同一对动作 +
//         `transform.localScale = GetCardInHandMovingScale()`（只有 `targeted`）· `cardState = 9` ·
//         末句 **`PlayerHand.LightPlayableHand()`**。
//     ⇒ 我们的等价物（`CardInteraction.ApplyOutFromPlayingArea`）：
//        单位 ⇒ 2D 卡面淡回（`ArtAlpha` 0→1）；`targeted` 法术 ⇒ 同上 + **缩放还原成手牌移动档（×1.1）**；
//        `cardState = 9` ⇒ `CardView.SetQueue(QHandMoving)`（视图侧不存 cardState，同 `ShowCardBeingDrawn` 的口径）；
//        `LightPlayableHand` ⇒ 事件 `OnLightPlayableHand`（驱动层接 `BattleDriver.RefreshHandPlayable`）。
//
//  B 段（`Q04c5`）· **「两侧都满」的失败码**
//     原版松手持针在出牌区里、抬高够 ⇒ `CanPlayCard(cardTracked, showTips: **1**, …)`
//     （`il2csharp/BattleManager.cs:25816`；`showTips = 1` 是**唯一**让四条 `NotifyCantDoAction` 可达的实参，
//      其余 4 个调用点全传 0）⇒ 棋盘放不下那一支 = `BattleManager__CanPlayCard.c:104-120`
//     ⇒ **`Battle/Tips/NotEnoughRoom`**（`RuleCodes.ErrNotEnoughRoom`）。
//     ⇒ 我们：候选集为空（两侧都满）∧ 指针在出牌区里 ∧ 单位卡 ⇒ `DropRejectKind.EngineRefused`（**不是** `MissedSlot`），
//       并且**问了一次引擎**（`CanDropAtSlot`）好让驱动层记下 `ErrNotEnoughRoom`。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//   · A1 ← `ApplyOutFromPlayingArea` 的单位支不调 `CardFeel.MoveBackToHand` ⇒ `ArtAlpha` 停在 0 ⇒ 红
//   · A3/A4 ← `targeted` 那一支不还原缩放 / 不 `UnDissolveWholeCard` ⇒ 缩放仍是夹具那个 0.05 / `Alpha2D` 停在 0 ⇒ 红
//   · A5 ← `Release` 只在 `OutOfPlayArea` 时调（**这一步是产品行为**）：把调用那半边删掉 ⇒ A6 的计数变 0 ⇒ 红
//   · B1 ← 把「候选集为空」改回 `MissedSlot` ⇒ 红（同时 B3 会指出那不是「非法操作」）
//   · B2 ← 不把引擎问一次 ⇒ 驱动层记不到 `ErrNotEnoughRoom` ⇒ 红
//   · B4/B5 ← 把「指针不在出牌区」并进这一支 ⇒ 那两档也会变成 `EngineRefused` ⇒ 红
//   · C1 ← 证明 A1 的变化来自本方法而不是 `Relayout`（单跑 `Relayout` 时 `ArtAlpha` 不动）
//   · C2 ← 退化成旧口径（`MissedSlot`）时**没有** cantdo（`IsIllegalAction` 为假）⇒ B1 会红
//   · C3 ← 指针在出牌区里松手（抬高不够 ⇒ 取消）**不**走这一支 ⇒ 计数为 0（把 A6 那半边改成无条件调用 ⇒ 这里红）
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//   · **不跑 Unity 帧循环**：补间一律 `CardTween.Mode = Manual` 再自己 `Advance`（退出时**原样还原**）。
//   · **夹具没有 `BattleDriver`** ⇒ `SideCount` 走「没有上下文」那一支 ⇒ 用 `SetSideCountsForTest` 钉每侧已占数
//     （产品路径永远有 `Ctx`、读的仍是 `RuleEngine.BoardSlots.CountOnSide`）。
//   · **引擎那一半是替身**（`CanDropAtSlot`）：B 段只证明「我们**问了**引擎 + 失败码是 `EngineRefused`」，
//     「引擎回的就是 `ErrNotEnoughRoom`」由 `RuleCodes.TermKey` 那一条静态断言 + `B120_W24` 的候选集断言合起来钉。
//   · **不验手感/观感**（溶解与淡入在真机上好不好看）：那要真 Play（`资料/真Play待验清单.md`）。
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using CardPresentation;
using RuleEngine;

/// <summary>`B120-W28` 的断言集：`Q04c4`（出牌区外那一支）+ `Q04c5`（两侧都满 ⇒ `NotEnoughRoom`）。</summary>
public static class B120_W28_Assert
{
    const string P = "B120_W28 ";
    static int _pass, _fail;
    /// <summary>通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败的条数。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    /// <summary>只记录、不计分（前提读数 / 判别力读数）。</summary>
    static void Tr(string msg) { Debug.Log(P + "· " + msg); }

    /// <summary>跑一组并把异常收成一条失败（⛔ 不让一组炸掉后面各组）。</summary>
    static void Group(string tag, Action body)
    {
        try { body(); }
        catch (Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepMode = CardTween.Mode;
        bool keepLink = CardTween.LinkEnabled;
        try
        {
            CardTween.Mode = UpdateType.Manual;    // 批处理没有帧循环 ⇒ 补间不许自己跑
            CardTween.LinkEnabled = false;

            // ⚠️ 顺序：A（唯一大量 `Advance` 的一组）先跑，每条补间**当场 `Kill`**；
            //   C1 还会再 `Advance` 一次（那一组自己建不出补间）。测完一律 `Dispose` 夹具，
            //   避免「夹具已销毁、手动补间还在推进」那类缺目标报错。
            Group("A·出牌区外那一支（Q04c4）", A_OutFromPlayingArea);
            Group("B·两侧都满的失败码（Q04c5）", B_NoRoomCode);
            Group("C·灭自证", C_AntiSelfProof);
        }
        catch (Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            CardTween.Mode = keepMode;
            CardTween.LinkEnabled = keepLink;
        }
        Debug.Log(P + "B120_W28 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ==================================================================
    //  夹具（与 `B120_W24_Assert` 同一打法：手摆几何 + `CanDropAtSlot` 替身）
    // ==================================================================
    sealed class Fixture : IDisposable
    {
        public GameObject root;
        public HandLayout hand;
        public BoardLayout board;
        public CardInteraction it;
        public CardView card;
        public int flights;              // `OnDeployFlightStart` 次数 = 「真的进入了打出链」
        public int returns;              // `OnReturned` 次数 = 「取消 / 回弹」
        public int lights;               // `OnLightPlayableHand` 次数 = 原版 `LightPlayableHand()` 那一句
        public int engineAsks;           // `CanDropAtSlot` 被问了几次（= 引擎码能不能被记下来）
        public int engineLastSlot = -99;

        public void Dispose()
        {
            if (card != null) { try { card.transform.DOKill(); } catch { } }
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    /// <summary>最小夹具：手牌布局 + 一块 2D 棋盘 + 交互器。</summary>
    static Fixture Make(string name)
    {
        var f = new Fixture();
        f.root = new GameObject("B120_W28_root");
        f.hand = f.root.AddComponent<HandLayout>();
        f.board = f.root.AddComponent<BoardLayout>();
        f.it = f.root.AddComponent<CardInteraction>();
        f.it.hand = f.hand;
        f.it.board = f.board;
        f.it.SetRightButtonHeldForTest(false);      // 批处理没有鼠标设备 ⇒ 把「右键按着」那道闸钉成 false
        f.it.SetBattleStateBlockedForTest(false);   // 本文件只验落点/触发 ⇒ 战斗状态闸全放行
        f.it.CanDropAtSlot = (slot, c) => { f.engineAsks++; f.engineLastSlot = slot; return true; };
        f.hand.animateRelayout = false;             // 夹具：位姿当场精确
        f.board.lineY = 0.42f;
        f.board.use3D = false;                      // 2D 兜底档（与 W24 同一档；提交路径显式 `allowNearest:true`）
        f.it.OnDeployFlightStart += (cv, bl, sl, dur) => f.flights++;
        f.it.OnReturned += (cv, k) => f.returns++;
        f.it.OnLightPlayableHand += () => f.lights++;

        var def = CardData.Placeholder(1);
        def.id = name; def.artId = name;            // ⛔ 别撞真实卡：`TacticTargetSide` / `IsTargetedCard` 会拿 id 回查卡表
        def.isUnit = true; def.type = "unit"; def.rarity = "common";
        f.card = CardView.Create(f.root.transform, def, name, CardFace.Hand);
        f.card.transform.position = LayoutSpace.ToWorld(0.5f, 0.12f);
        f.it.SetCards(new List<CardView> { f.card }, false);
        return f;
    }

    /// <summary>卡池里第一张 **`targeted == 1` 的非单位卡**（`CardDef.Targeted` 三态 `int`，`1` 才算）。</summary>
    static CardDef FindTargetedSpell()
    {
        var pool = CardDatabase.Load();
        for (int i = 0; i < pool.Count; i++)
        {
            var d = pool[i];
            if (d != null && !d.IsUnit && d.Targeted == 1) return d;
        }
        return null;
    }

    /// <summary>把夹具那张卡换成「某张真实卡的**非单位**形态」（用于 `targeted` 那一支）。</summary>
    static void SwapToSpell(Fixture f, CardDef def)
    {
        UnityEngine.Object.DestroyImmediate(f.card.gameObject);
        var d = CardData.Placeholder(0);
        d.id = def.Id; d.artId = def.Id; d.isUnit = false; d.type = "tactic"; d.rarity = "common";
        f.card = CardView.Create(f.root.transform, d, "B120W28_spell", CardFace.Hand);
        f.card.transform.position = LayoutSpace.ToWorld(0.5f, 0.12f);
        f.it.SetCards(new List<CardView> { f.card }, false);
    }

    /// <summary>「手牌移动档」的缩放 = 原版 `GetCardInHandMovingScale()` = 手牌缩放 × `cardInHandMovingScale`(1.1)。
    /// ⚠️ 这里**不抄产品那一行**：只用手牌缩放 + 原版那个 1.1 比值（判据 = `VarsGlobal.cardInHandMovingScale`）。</summary>
    static float HandMovingScale(Fixture f)
    {
        return f.hand.cardScale * LayoutSpace.Scale * 1.1f;
    }

    // ==================================================================
    //  A · `Q04c4`：出牌区外那一支
    // ==================================================================
    static void A_OutFromPlayingArea()
    {
        // ---- A0：判据线本身（前提，⛔ 不从被测实现读「结果」）----
        Check(CardInteraction.OutOfPlayArea(LayoutSpace.ToWorld(0.5f, 0.12f))
              && !CardInteraction.OutOfPlayArea(LayoutSpace.ToWorld(0.5f, 0.42f)),
            "（前提）`OutOfPlayArea` 按 `HandLimitArea` 那条线分档：归一化 y=0.12 在**线下**（出牌区外）、"
            + "y=0.42 在**线上**（判据 = `BattleManager__Update.c:248-250` 的 `line.y < mouse.y`，我们 = `BoardLayout.HandLimitY`）");

        // ---- A1/A2：单位 ⇒ `DissolveMinionCard(false)`（我们看得见的那一半 = 2D 卡面淡回）+ state9 等价物 ----
        {
            var f = Make("B120W28_A1");
            Tween tw = null;
            try
            {
                f.card.SetArtAlpha(0f);                              // 夹具：假装配色被溶掉了
                f.card.SetQueue(CardView.QCard);                     // 夹具：回到「静置手牌」那一档的队列号
                Check(Mathf.Approximately(f.card.ArtAlpha, 0f),
                    "（前提）A1 起始 `ArtAlpha` = 0（模拟「这张卡是溶着的」）—— 否则 A1 证明不了「淡回来了」");
                Check(f.card.Queue == CardView.QCard,
                    "（前提）A1 起始队列 = `QCard`（" + CardView.QCard + "，实得 " + f.card.Queue + "）—— 否则 A2 证明不了分层换过档");

                tw = f.it.ApplyOutFromPlayingArea(f.card);
                Check(tw != null,
                    "★A1a 单位卡这一支**起了一条补间**（原版 `DissolveMinionCard(false)` 的 `FadeCardItems(1, 0.2)`）"
                    + "｜🧨 不调 `CardFeel.MoveBackToHand` 的单位支 ⇒ 这里 null ⇒ 红");

                CardTween.Advance(0.4f);                             // 0.2 s 那条淡入跑完（多给一倍余量）
                Check(f.card.ArtAlpha > 0.99f,
                    "★A1b 单位卡 2D 卡面**淡回到 1**（实得 " + f.card.ArtAlpha.ToString("F3") + "）"
                    + "｜判据 `CardScript__DissolveMinionCard.c:15-49`（α → 1 · 时长 `timeToDissolveCard` 0.2）"
                    + "｜🧨 走回旧路径（只 `Relayout`）⇒ 仍是 0 ⇒ 红");
                Check(f.card.Queue == CardView.QHandMoving,
                    "★A2 `cardState = 9`(`inHandMoving`) 的等价物 = 队列换到 **`QHandMoving`**（" + CardView.QHandMoving
                    + "，实得 " + f.card.Queue + "）"
                    + "｜判据 `OutFromPlayingAreaAnim.c:30`（`cardState = 9`）+ `ShowCardBeingDrawn.c` 同一档的 `SetSortingLayer(HandMoving)`"
                    + "｜🧨 不设这一档 ⇒ 仍是 `QCard` ⇒ 红");
                Check(f.card.Body3DVisible == false,
                    "★A3 3D 卡体**是关着的**（`DissolveMinionCard` 的 `body3D.SetActive(false)`；手牌形态本来就关，这一条只防回归）");
                Check(f.lights == 1,
                    "★A4 原版末句 `PlayerHand.LightPlayableHand()` 被触发**恰好 1 次**（实得 " + f.lights + "）"
                    + "｜判据 `OutFromPlayingAreaAnim.c:34-38`｜🧨 不抬事件 ⇒ 0 ⇒ 红");
            }
            finally
            {
                if (tw != null) DG.Tweening.TweenExtensions.Kill(tw, false);
                f.Dispose();
            }
        }

        // ---- A5/A6/A7：targeted 法术 ⇒ 缩放还原 + `UnDissolveWholeCard` ----
        {
            var def = FindTargetedSpell();
            Check(def != null, "（前提）卡池里存在 `targeted == 1` 的非单位卡（否则 A5~A7 组没法构造）");
            if (def == null) return;

            var f = Make("B120W28_A5");
            Tween tw = null;
            try
            {
                SwapToSpell(f, def);
                Check(!f.card.Data.isUnit && CardInteraction.IsTargetedCard(f.card),
                    "（前提）A5 这张是**非单位卡且 `targeted == 1`**（卡表 id「" + def.Id + "」）"
                    + "｜判据 = 原版 `EntityScript.get_targeted()`；我们那份在 `CardDef.Targeted`（三态 `int`）");
                Check(f.lights == 0,
                    "（前提）A5 换卡之后**还没有**抬过事件（实得 " + f.lights + "）—— 否则下面 A4/A8 的计数没有判别力");

                // 夹具：把缩放故意摆错（0.05），再把 2D 卡面压到 0（模拟「被溶过」）
                f.card.SetPose(f.card.transform.localPosition, 0f, 0.05f);
                f.card.SetAlpha2D(0f);
                float bad = f.card.transform.localScale.x;
                Check(bad < 0.2f,
                    "（前提）A5 起始缩放是**故意摆错的** " + bad.ToString("F3") + "（≪ 手牌移动档 "
                    + HandMovingScale(f).ToString("F3") + "）—— 否则 A6 证明不了「还原过」");

                tw = f.it.ApplyOutFromPlayingArea(f.card);

                float want = HandMovingScale(f) * f.card.HighlightMul;
                float got = f.card.transform.localScale.x;
                Check(Mathf.Abs(got - want) < 1e-3f,
                    "★A5 `targeted` 法术 ⇒ `localScale` **还原成手牌移动档**（实得 " + got.ToString("F4")
                    + "，应 ≈ " + want.ToString("F4") + " = 手牌缩放 × **1.1**）"
                    + "｜判据 `CardScript__OutFromPlayingAreaAnim.c:15-27`（`transform.localScale = GetCardInHandMovingScale()`；"
                    + "`GetCardInHandScale × VarsGlobal.cardInHandMovingScale` 1.1）"
                    + "｜🧨 不做这一句 ⇒ 仍是 0.05 ⇒ 红");
                Check(Mathf.Abs(got - bad) > 0.1f,
                    "★A5b **灭自证**：还原后的缩放与夹具那个 0.05 明显不同（差 " + Mathf.Abs(got - bad).ToString("F3") + " > 0.1）");

                CardTween.Advance(0.4f);
                Check(f.card.Alpha2D > 0.99f,
                    "★A6 `targeted` 法术 ⇒ **取消溶解**（2D α 0→1，实得 " + f.card.Alpha2D.ToString("F3") + "）"
                    + "｜判据 `CardScript__UnDissolveWholeCard.c`（→ `FadeCardItems(1, 0.2)`）"
                    + "｜🧨 不走 `UnDissolveWholeCard` ⇒ 仍是 0 ⇒ 红");
                Check(f.card.Queue == CardView.QHandMoving,
                    "★A7 `cardState = 9` 的等价物同样落到 `QHandMoving`（实得 " + f.card.Queue + "）");
            }
            finally
            {
                if (tw != null) DG.Tweening.TweenExtensions.Kill(tw, false);
                f.Dispose();
            }
        }

        // ---- A8：`Release` 真的只在「出牌区外」调它（产品触发点，走 `SimulateRelease` 那条真路）----
        {
            var f = Make("B120W28_A8");
            try
            {
                Vector3 home = f.card.transform.position;
                Vector3 inHand = LayoutSpace.ToWorld(0.95f, 0.12f);   // 横甩很远、但**仍在手牌带**
                Check(CardInteraction.OutOfPlayArea(inHand), "（前提）A8 的落点在**出牌区外**");

                f.it.SimulateHover(home);
                f.it.SimulatePress(home);
                Check(f.it.Dragging == f.card, "（前提）抓起来了");
                f.it.SimulateDrag(inHand, 1f / 60f);
                f.it.SimulateRelease(inHand);

                Check(f.lights == 1,
                    "★A8 松手落在**出牌区外** ⇒ 原版那一支**真的被触发**（`OnLightPlayableHand` 实得 " + f.lights + "）"
                    + "｜判据 `BattleManager__Update.c:568-590`（`HandLimitArea.y < 指针.y` 为假的那一支）"
                    + "｜🧨 把 `Release` 里那半边调用删掉 ⇒ 0 ⇒ 红");
                Check(f.flights == 0 && f.returns == 1,
                    "★A9 这一支**没有**出牌（起飞 " + f.flights + "）、仍走取消（`OnReturned` " + f.returns + "）");
            }
            finally { f.Dispose(); }
        }
    }

    // ==================================================================
    //  B · `Q04c5`：「两侧都满」要报原版的 `NotEnoughRoom`，⛔ 不是「正常取消」
    // ==================================================================
    static void B_NoRoomCode()
    {
        // ---- B0：候选集判据（前提）----
        Check(BoardLayout.NoCandidateSlot(4, 4) && !BoardLayout.NoCandidateSlot(0, 0) && !BoardLayout.NoCandidateSlot(-1, -1),
            "（前提）`NoCandidateSlot`：两侧都满(4,4) ⇒ **true**；皆空(0,0) ⇒ false；数不出来(−1,−1) ⇒ false"
            + "（⛔ 不把「数不出来」当「满」）｜判据 `MinionManager.cs:326-342`（满侧 `start = end = 4` ⇒ 一个候选都没有）");

        // ---- B1/B2：两侧都满 + 指针在出牌区里 ⇒ `EngineRefused`（= 非法操作 ⇒ cantdo）----
        {
            var f = Make("B120W28_B1");
            try
            {
                f.it.SetSideCountsForTest(4, 4);
                f.it.CanDropAtSlot = (slot, c) => { f.engineAsks++; f.engineLastSlot = slot; return false; };   // 替身：引擎说打不了
                Vector3 inArea = LayoutSpace.ToWorld(0.5f, 0.42f);
                Check(CardInteraction.IsInPlayArea(inArea), "（前提）B1 的指针在**出牌区里**（归一化 y = 0.42 ≥ HandLimitY）");

                int slot; BoardLayout which; string why;
                int k = f.it.DropRejectKindForTest(inArea, f.card, out slot, out which, out why, true);   // `allowNearest:true` = 提交档

                Check(k == (int)CardInteraction.DropRejectKind.EngineRefused,
                    "★B1 **两侧都满 ⇒ 失败码 = `EngineRefused`**（= 原版松手那一刻 `CanPlayCard(showTips:1)` 的 "
                    + "`Battle/Tips/NotEnoughRoom` 那一支；实得 " + (CardInteraction.DropRejectKind)k + "，why=「" + why + "」）"
                    + "｜判据 `il2csharp BattleManager.cs:25816`（`showTips: 1`）+ `BattleManager__CanPlayCard.c:104-120`"
                    + "｜🧨 改回 `MissedSlot` ⇒ 红（那时松手只写一句 `DragToTarget`、**不出 cantdo**）");
                Check(f.engineAsks == 1,
                    "★B2 这一支**问了一次引擎**（`CanDropAtSlot` 实得 " + f.engineAsks + " 次，格 " + f.engineLastSlot + "）"
                    + "—— 驱动层就是在这里把 `_lastPlayRejectCode` 记成 `ErrNotEnoughRoom` 的"
                    + "｜🧨 只改 `kind` 不问引擎 ⇒ 驱动层拿不到码、提示退回 `DragToTarget` ⇒ 红");
                Check(CardInteraction.IsIllegalAction(CardInteraction.DropRejectKind.EngineRefused)
                      && !CardInteraction.IsIllegalAction(CardInteraction.DropRejectKind.MissedSlot),
                    "★B3 `EngineRefused` **算非法操作**（⇒ 松手时 `OnIllegalAction` → cantdo 语音 + 文字），"
                    + "而 `MissedSlot` **不算**（拖回手牌那档保持静默）"
                    + "｜判据 `BattleManager.cs:25816`（`showTips:1` 才让四条 `NotifyCantDoAction` 可达）"
                    + "｜🧨 把 B1 那一支改回 `MissedSlot` ⇒ 这一档就静静回手、一句都不说 ⇒ B1/B3 红");
                Check(RuleCodes.TermKey(RuleCodes.ErrNotEnoughRoom) == "Battle/Tips/NotEnoughRoom",
                    "★B4 引擎码 → 词条 1:1：`ErrNotEnoughRoom` ⇒ `Battle/Tips/NotEnoughRoom`（实得「"
                    + RuleCodes.TermKey(RuleCodes.ErrNotEnoughRoom) + "」）—— 也就是玩家看到的那句「战场上没有空位」"
                    + "｜判据 `RuleCodes.TermKey` 的 doc + `BattleManager__CanPlayCard.c:104-120`");
            }
            finally { f.Dispose(); }
        }

        // ---- B5：两侧都满、但**指针留在手牌带里** ⇒ 仍是「正常取消」（原版根本不问引擎）----
        {
            var f = Make("B120W28_B5");
            try
            {
                f.it.SetSideCountsForTest(4, 4);
                f.it.CanDropAtSlot = (slot, c) => { f.engineAsks++; f.engineLastSlot = slot; return false; };
                Vector3 inHand = LayoutSpace.ToWorld(0.5f, 0.12f);
                Check(CardInteraction.OutOfPlayArea(inHand), "（前提）B5 的指针在**手牌带里**");

                int slot; BoardLayout which; string why;
                int k = f.it.DropRejectKindForTest(inHand, f.card, out slot, out which, out why, true);
                Check(k == (int)CardInteraction.DropRejectKind.MissedSlot,
                    "★B5 **指针不在出牌区** ⇒ 仍是 `MissedSlot`（实得 " + (CardInteraction.DropRejectKind)k + "）"
                    + "—— 原版那一支连 `CanPlayCard` 都不问（`BattleManager.cs:25806` 的 `mouseCanvasPos.y > line.y` 为假）"
                    + "｜🧨 只按「两侧都满」判、不看指针 ⇒ 拖回手牌也播 cantdo ⇒ 红");
                Check(f.engineAsks == 0,
                    "★B6 这一档**一次都没问引擎**（实得 " + f.engineAsks + "）—— ⛔ 别把「指针不在棋盘上」并进 `NotEnoughRoom` 那一支");
            }
            finally { f.Dispose(); }
        }

        // ---- B7：两侧有空位 + 指针在出牌区里 ⇒ 正常可打（⛔ 别把 NoRoom 支扩成常开）----
        {
            var f = Make("B120W28_B7");
            try
            {
                f.it.SetSideCountsForTest(0, 0);
                Vector3 inArea = LayoutSpace.ToWorld(0.5f, 0.42f);
                int slot; BoardLayout which; string why;
                int k = f.it.DropRejectKindForTest(inArea, f.card, out slot, out which, out why, true);
                Check(k == (int)CardInteraction.DropRejectKind.Ok,
                    "★B7 两侧有空位 ⇒ 落点解析照样给出格号、**不是** `NotEnoughRoom`（实得 "
                    + (CardInteraction.DropRejectKind)k + "，槽 " + slot + "）"
                    + "｜🧨 把 `NoRoomBranch` 写成「只要解不出格就算满」⇒ 这一档也会变 `EngineRefused` ⇒ 红");
            }
            finally { f.Dispose(); }
        }
    }

    // ==================================================================
    //  C · 灭自证
    // ==================================================================
    static void C_AntiSelfProof()
    {
        // ---- C1：A1 的变化来自本方法，不是 `Relayout`（单跑重排，`ArtAlpha` 不动）----
        {
            var f = Make("B120W28_C1");
            try
            {
                f.card.SetArtAlpha(0f);
                f.it.Relayout();
                CardTween.Advance(0.4f);
                Check(Mathf.Approximately(f.card.ArtAlpha, 0f),
                    "★C1（灭自证）只跑 `Relayout`（不走出牌区外那一支）⇒ `ArtAlpha` **仍为 0**"
                    + "（实得 " + f.card.ArtAlpha.ToString("F3") + "）—— ⇒ A1 里那个 1 只能来自 "
                    + "`ApplyOutFromPlayingArea`（手牌重排不碰卡面 α）");
            }
            finally { f.Dispose(); }
        }

        // ---- C2：指针在出牌区里松手但**抬高不够**（取消）⇒ 不走这一支 ----
        {
            var f = Make("B120W28_C2");
            try
            {
                Vector3 home = LayoutSpace.ToWorld(0.5f, 0.30f);   // 起点就在**出牌区里**（线 0.2778 之上）
                f.card.transform.position = home;
                // 🔴 2026-11-03（`B124-W27`）：**量纲** —— `PlayLiftWorld`（原版世界单位 × 桥 = 0.0412 我们单位）与
                //   `TapThreshold`（10 真实 px ÷ 屏高；批处理 480p ⇒ 0.2083）**不是同一个量纲** ⇒ 只抬竖直永远同时满足不了
                //   「> 轻点阈值」与「< 出牌高度」。用**横向**位移补足拖距（出牌闸 `PlayCommitGate` 只看竖直 `y`）。
                float dragWorld = Mathf.Max(CardInteraction.TapThreshold * 2f, CardInteraction.PlayLiftWorld * 2f);
                Vector3 inAreaLow = home + new Vector3(dragWorld, CardInteraction.PlayLiftWorld * 0.5f, 0f);
                Check(inAreaLow.y - home.y < CardInteraction.PlayLiftWorld,
                    "（前提）C2 的落点在**出牌区里**、但抬高**不够** 0.3 原版世界单位 ⇒ `PlayCommitGate` 判假 ⇒ 取消");
                Check(CardInteraction.IsInPlayArea(inAreaLow), "（前提）C2 的落点归一化 y "
                    + LayoutSpace.ToNormalized(inAreaLow).y.ToString("F4") + " ≥ " + BoardLayout.HandLimitY.ToString("F4"));
                Check(Vector3.Distance(inAreaLow, home) >= CardInteraction.TapThreshold,
                    "（前提）C2 这一拖**超过轻点阈值**（" + Vector3.Distance(inAreaLow, home).ToString("F4") + " ≥ `TapThreshold` "
                    + CardInteraction.TapThreshold.ToString("F4") + "；其中竖直只有 " + (inAreaLow.y - home.y).ToString("F4")
                    + "，其余靠横向）—— 否则它按「轻点」处理，下面那条会因**别的原因**通过");

                f.it.SimulateHover(home);
                f.it.SimulatePress(home);
                Check(f.it.Dragging == f.card, "（前提）C2 抓起来了（⛔ 抓不起来时下面那条会空转绿，所以这条先硬断）");
                f.it.SimulateDrag(inAreaLow, 1f / 60f);
                f.it.SimulateRelease(inAreaLow);
                Check(f.lights == 0,
                    "★C2 这一档**不**走出牌区外那一支（`OnLightPlayableHand` 实得 " + f.lights + "）"
                    + "—— 原版只有「指针掉回线下」才调 `OutFromPlayingAreaAnim`"
                    + "｜🧨 把 `Release` 那半边改成无条件调用 ⇒ 这里变 1 ⇒ 红");
            }
            finally { f.Dispose(); }
        }

        // ---- C3：旧口径（`MissedSlot`）确实**没有** cantdo —— 证明 B1 有判别力 ----
        Check(!CardInteraction.IsIllegalAction(CardInteraction.DropRejectKind.MissedSlot)
              && CardInteraction.IsIllegalAction(CardInteraction.DropRejectKind.EngineRefused),
            "★C3（灭自证）旧口径 `MissedSlot` **不算非法操作**（故不会播 cantdo）、新口径 `EngineRefused` 算 —— "
            + "⇒ 把失败码改回 `MissedSlot`，B1 与这条必红");

        // ---- C4：`ApplyOutFromPlayingArea` 对 null 安全（`Release` 里 `_dragging` 可能已被别的支清空）----
        {
            var f = Make("B120W28_C4");
            try
            {
                Tween tw = null;
                try { tw = f.it.ApplyOutFromPlayingArea(null); } catch (Exception e) { Check(false, "★C4 传 null 抛异常：" + e.GetType().Name); }
                Check(tw == null && f.lights == 0,
                    "★C4 传 null ⇒ 什么都不做、也不抬事件（返回 null；实得 lights " + f.lights + "）");
            }
            finally { f.Dispose(); }
        }
    }
}
