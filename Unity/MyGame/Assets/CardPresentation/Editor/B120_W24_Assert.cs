// B120_W24_Assert.cs — 执行写手 `B120-W24`（2026-11-02）· 账 `B120-A15` + `B120-C09`
//
// ============================ 它盯的是什么 ============================
//  A 段（`B120-A15`）· **原版提交不要求指针压在某一格上**
//     原版提交那一帧只问两件事：① 指针越过手牌限位线 ② 比按下点抬够 `minMovementToPlayCard`（0.3 原版世界单位）；
//     之后 `CanPlayCard` 对单位**只查「还有没有空位」**（`MinionManager.IsAvailableSlot`），
//     落格是**另一跳** `GetClosestAvailableSlot(指针屏幕点)`（最近候选格、**无任何容差**）。
//     ⇒ 指针往棋盘上方空白处一甩也**应该**出牌，落格 = 最近候选格。
//     判据（亲读）：`decomp_full/BattleManager__Update.c:249-288`（`case 2`，`MoveIntoFieldFromHand` 在 `:260`）·
//       `decomp_full/BattleManager__CanPlayCard.c:104-120`（`IsAvailableSlot` 为假 ⇒ `NotEnoughRoom`）·
//       `cpp2il/out_il2csharp/Assembly-CSharp/MinionManager.cs:321-390`（候选集 + 2D 欧氏 + 无容差）·
//       `decomp_full/MinionManager__GetClosestAvailableSlot.c:35-145`。
//
//  B 段（`B120-C09`）· **回放跳过档（C-2/C-4）的运行时断言**
//     `CardFeel.MeleeAttack` / `AttackRecover` 已接 `stepScale`（段 1/2/4/5 缩、段 3 的 `AttackHoldTime` **不缩**）；
//     `EventTiming.DelayBetween` 的 Hit 间隔 = `AttackHoldTime + 2×step`。
//     本段**逐帧 `CardTween.Advance`** 推真补间，量「跳过档 0.2 / 正常档 0.4」与「段 3 不缩」。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//   · A1 ← 把 `DropReject` 的默认（`allowNearest=false`）也做成「无容差」⇒ 旧签名会答出格号 ⇒ 红
//   · A3 ← 不把「两侧都满」判成空候选 ⇒ 答出一个格 ⇒ 红（那会绕过引擎的 `NotEnoughRoom`）
//   · A4/A5 ← 候选集不按 `IsCandidate` 收窄（9 格全算）⇒ 答 8 / 3 而不是 5 ⇒ 红
//   · A6 ← 去掉「越手牌限位线」这一道 ⇒ 手牌带里那一拖就提交 ⇒ 红
//   · A7 ← 去掉「抬够 0.3 原版世界单位」这一道 ⇒ 红
//   · A9 ← 把「最近格」这一跳接到**取消路径**上（不设 PlayCommitGate）⇒ 牌被误打出去 ⇒ 红
//   · B2 ← 段 1 不乘 `stepScale` ⇒ 0.04 s 时还没到终点 ⇒ 红
//   · B3 ← **段 3 也被 ÷3**（= 整条 0.4 一起除）⇒ 0.0833 s 时已经往回走了 ⇒ 红（B5 是它的灭自证前提）
//   · B1/B6 ← 忽略 `stepScale` ⇒ 跳过档量出 0.4 ⇒ 红
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//   · **不跑 Unity 帧循环**：B 段是编辑模式下把 `CardTween.Mode` 拨到 `Manual` 再自己 `Advance`（退出时**原样还原**）。
//   · **2D 兜底档**：A 段夹具没有 3D 战场（`BoardLayout.use3D = false`）⇒ 走的正是「原来必须命中一格」那一条
//     （3D 产品路径在 `B120-W21` 之后本来就是最近格、无容差）。
//   · **候选集边界只在 API 层断言**（A3/A4/A5）：夹具没有驱动 ⇒ `SideCount` 恒 −1（两侧按皆空），
//     「两侧都满 ⇒ 不出牌」那一档要真驱动才有，本件用 `BoardLayout.TryResolveSlot(..., 4, 4, true)` 直接钉住。
//   · **不验手感**：那要真 Play（`资料/真Play待验清单.md`）。
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using CardPresentation;
using RuleEngine;

/// <summary>`B120-W24` 的断言集：`A15`（不压格也能出牌）+ `C09`（回放跳过档的运行时时长）。</summary>
public static class B120_W24_Assert
{
    const string P = "B120_W24 ";
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

            // ⚠️ 顺序是有意的：**E/F 先跑**（它们要 `CardTween.Advance` 推时间）。
            //   A/C/D 会经 `Release` → `DeploySequence.Play` 建出**手动档**的落位补间，而夹具一 `Dispose`
            //   宿主就没了 ⇒ 再 Advance 会撞「target missing」。放在最后 ⇒ 之后没有任何一次 Advance。
            Group("E·近战跳过档的运行时时长（C09）", E_MeleeSkipRuntime);
            Group("F·收招跳过档 + 命中拍间隔（C09）", F_RecoverAndHitGap);
            Group("A·不压格也能出牌（A15）", A_NearestSlotCommit);
            Group("B·候选集边界（A15）", B_CandidateSetBoundary);
            Group("C·灭自证·两道门仍在（A15）", C_GatesStillRule);
            Group("D·取消路径不受影响（A15）", D_CancelPathUntouched);
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
        Debug.Log(P + "B120_W24 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ==================================================================
    //  夹具（与 `B120_W16_Assert` 同一打法：手摆几何 + `CanDropAtSlot` 替身）
    // ==================================================================
    sealed class Fixture : IDisposable
    {
        public GameObject root;
        public HandLayout hand;
        public BoardLayout board;
        public CardInteraction it;
        public CardView card;
        public int flights;          // `OnDeployFlightStart` 次数 = 「真的进入了打出链」
        public int returns;          // `OnReturned` 次数 = 「取消 / 回弹」
        public int flightSlot = -99; // 起飞那次报的**落点格号**
        public BoardLayout flightBoard;

        public void Dispose() { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
    }

    /// <summary>最小夹具：手牌布局 + 一块 2D 棋盘 + 交互器。
    /// <paramref name="lineY"/> = 棋盘行心（归一化 y）· <paramref name="cardNormY"/> = 那张手牌**手摆**到哪个归一化 y。</summary>
    static Fixture Make(float lineY, float cardNormY, string name)
    {
        var f = new Fixture();
        f.root = new GameObject("B120_W24_root");
        f.hand = f.root.AddComponent<HandLayout>();
        f.board = f.root.AddComponent<BoardLayout>();
        f.it = f.root.AddComponent<CardInteraction>();
        f.it.hand = f.hand;
        f.it.board = f.board;
        f.it.SetRightButtonHeldForTest(false);      // 批处理没有鼠标设备 ⇒ 把「右键按着」那道闸钉成 false
        f.it.SetBattleStateBlockedForTest(false);   // 本文件只验落点/提交 ⇒ 战斗状态闸全放行
        f.it.CanDropAtSlot = (slot, c) => true;     // 引擎那一半换成替身；本件验的是「我们这一层的落点解析」
        f.hand.animateRelayout = false;             // 夹具：位姿当场精确
        f.board.lineY = lineY;
        f.board.use3D = false;                      // ★ 本件刻意的：2D 兜底档才是「原来必须命中一格」那一条
        f.it.OnDeployFlightStart += (cv, bl, sl, dur) => { f.flights++; f.flightSlot = sl; f.flightBoard = bl; };
        f.it.OnReturned += (cv, k) => f.returns++;

        var def = CardData.Placeholder(1);
        def.id = name; def.artId = name;            // ⛔ 别撞真实卡：`TacticTargetSide` 会拿 id 回查卡表
        def.isUnit = true; def.type = "unit"; def.rarity = "common";
        f.card = CardView.Create(f.root.transform, def, name, CardFace.Hand);
        f.card.transform.position = LayoutSpace.ToWorld(0.5f, cardNormY);
        f.it.SetCards(new List<CardView> { f.card }, false);
        return f;
    }

    /// <summary>「指针甩在棋盘上方空白处」那一点 —— 归一化 x = 0.95，离最近格（槽 8 的 0.8112）差 **0.1388** ≫ `snapTolerance` 0.036。</summary>
    static Vector3 BlankAboveBoard(float lineY) { return LayoutSpace.ToWorld(0.95f, lineY); }

    // ==================================================================
    //  A · ★核心：抬高够 + 越过手牌线 ⇒ 出牌，落格 = **最近候选格**（指针没正对某一格）
    // ==================================================================
    static void A_NearestSlotCommit()
    {
        var f = Make(0.42f, 0.12f, "B120W24_A");
        try
        {
            Vector3 home = f.card.transform.position;
            Vector3 drop = BlankAboveBoard(0.42f);
            int hand0 = f.it.HandCardCountForTest;

            // ---- 前提（手摆几何 + 原版常量，⛔ 不从被测实现读「结果」）----
            int sOld;
            bool oldOk = f.board.TryResolveSlot(drop, out sOld);
            Check(!oldOk,
                "（前提）这一点**不在任何格位的容差区里** —— 旧签名 `TryResolveSlot(drop, out s)`（= `allowNearest:false`）"
                + "判 **false**（实得 " + oldOk + "，槽 " + sOld + "）—— 否则本组证明不了「不压格也能出牌」"
                + "｜🧨 把默认档也改成无容差 ⇒ 这里变 true ⇒ A1 的前提塌了（同时 A1 会红）");
            Check(LayoutSpace.ToNormalized(drop).y >= BoardLayout.HandLimitY,
                "（前提）落点在**手牌限位线以上**（归一化 y " + LayoutSpace.ToNormalized(drop).y.ToString("F4")
                + " ≥ " + BoardLayout.HandLimitY.ToString("F4") + "）");
            Check(drop.y - home.y >= CardInteraction.PlayLiftWorld,
                "（前提）抬高了 " + (drop.y - home.y).ToString("F4") + " 我们的单位 ≥ "
                + CardInteraction.PlayLiftWorld.ToString("F5") + "（= `minMovementToPlayCard` 0.3 原版世界单位）");

            // ---- A1：判定正本（`DropReject` 默认档）**仍然是** MissedSlot ⇒ 本件没动既有判据 ----
            int sDirect; BoardLayout wDirect; string whyDirect;
            int kDirect = f.it.DropRejectKindForTest(drop, f.card, out sDirect, out wDirect, out whyDirect);
            Check(kDirect == (int)CardInteraction.DropRejectKind.MissedSlot,
                "★A1 `DropReject` 的**默认档**（`allowNearest:false`）在这一点的判定**仍是 `MissedSlot`**（实得 "
                + (CardInteraction.DropRejectKind)kDirect + "）—— 新行为只加在**提交路径**上，"
                + "`ExplainDrop` / `DropRejectKindForTest` / `CardBaseDemo` 那批既有判据一个字没动"
                + "｜判据 `BoardLayout.TryResolveSlot(..., allowNearest)` 的类尾长注");

            // ---- A2：★不压格也提交（原版 `BattleManager__Update.c:249-288` 全段没有「压格」比较）----
            f.it.SimulateHover(home);
            f.it.SimulatePress(home);
            Check(f.it.Dragging == f.card, "（前提）抓起来了");

            f.it.SimulateDrag(drop, 1f / 60f);
            Check(f.it.Dragging == null,
                "★A2 指针**没压在任何格上**、只满足「抬高够 + 越过手牌线」⇒ **已经提交**（拖拽态已空）"
                + "｜判据 `BattleManager__Update.c:253-260`（`CanPlayCard` 一过就 `MoveIntoFieldFromHand()`）"
                + "＋ `BattleManager__CanPlayCard.c:104-120`（单位只查有没有空位）"
                + "｜🧨 退回「必须命中一格才算提交」⇒ 拖拽态还在、牌留在手里 ⇒ 红");
            Check(f.flights == 1,
                "★A3 提交恰好 1 次（`OnDeployFlightStart` 实得 " + f.flights + "）");
            Check(f.it.HandCardCountForTest == hand0 - 1,
                "★A4 这张牌已经**离开手牌**（" + hand0 + " → " + f.it.HandCardCountForTest + "）");
            Check(f.flightSlot == 5,
                "★A5 落格 = **最近候选格**（实得槽 " + f.flightSlot + "）—— 夹具没有驱动 ⇒ 两侧按皆空 ⇒ 候选 = {3,5}，"
                + "指针在 0.95（偏右）⇒ 最近的是 **5**"
                + "｜判据 `MinionManager.cs:321-390`（候选集 + 2D 欧氏 + 无容差）"
                + "｜🧨 候选集不收窄（9 格全算）⇒ 答 8 ⇒ 红；不取最近 ⇒ 答 3 ⇒ 红");
            Check(ReferenceEquals(f.flightBoard, f.board),
                "★A6 起飞报的棋盘 = **我方那块**（⛔ 不是另一块）—— 落格解析没有跑到对面去");
        }
        finally { f.Dispose(); }
    }

    // ==================================================================
    //  B · 候选集边界（API 层：夹具没有驱动 ⇒ 只能直接喂「每侧已占数」）
    // ==================================================================
    static void B_CandidateSetBoundary()
    {
        var f = Make(0.42f, 0.12f, "B120W24_B");
        try
        {
            Vector3 drop = BlankAboveBoard(0.42f);
            int s;

            Check(!f.board.TryResolveSlot(drop, out s, 4, 4, true),
                "★B1 **两侧都满 ⇒ 候选集为空 ⇒ false**（⛔ 不当成可出牌；实得 " + s + "）"
                + "｜判据 `MinionManager.cs:331-334`（满侧 `start = 4` / `end = 4`）＋ 原版同一情形返回 `0` = 无"
                + "｜🧨 候选集为空还给出牌 ⇒ 这里变 true ⇒ 红（那会让引擎的 `NotEnoughRoom` 形同虚设）");

            bool r4 = f.board.TryResolveSlot(drop, out s, 4, 0, true);
            Check(r4 && s == 5,
                "★B2 左侧已满（4）⇒ 候选只剩右半 ⇒ 答槽 **5**（实得 ok=" + r4 + " 槽 " + s + "）"
                + "｜🧨 不剔除满侧 ⇒ 可能答 6/7/8 ⇒ 红");

            bool r0 = f.board.TryResolveSlot(drop, out s, 0, 0, true);
            Check(r0 && s == 5,
                "★B3 两侧皆空 ⇒ 候选 = {3,5}，指针在 0.95 ⇒ 答槽 **5**（实得 ok=" + r0 + " 槽 " + s + "）"
                + "｜🧨 9 格全算 ⇒ 答 8 ⇒ 红");

            // 前提：同一点在默认档上仍然是「解不出来」—— 证明 B1..B3 都是**新那一档**的行为
            Check(!f.board.TryResolveSlot(drop, out s),
                "★B4（灭自证前提）同一点走**默认档** ⇒ false（实得 " + s + "）—— 新行为是**显式 opt-in**，没污染旧口");
        }
        finally { f.Dispose(); }
    }

    // ==================================================================
    //  C · 灭自证：`PlayCommitGate` 的两道门仍然说了算
    // ==================================================================
    static void C_GatesStillRule()
    {
        // C-1：抬高**不够** 0.3 原版世界单位（0.0038 归一化 = 0.038 我们的单位 < 0.04121）
        {
            var f = Make(0.30f, 0.2962f, "B120W24_C1");
            try
            {
                Vector3 home = f.card.transform.position;
                Vector3 drop = BlankAboveBoard(0.30f);
                int hand0 = f.it.HandCardCountForTest;

                Check(LayoutSpace.ToNormalized(drop).y >= BoardLayout.HandLimitY,
                    "（前提）C1 的落点**已越**手牌限位线 —— 本组只测「抬高」这一道");
                Check(drop.y - home.y < CardInteraction.PlayLiftWorld,
                    "（前提）C1 抬高 " + (drop.y - home.y).ToString("F5") + " < "
                    + CardInteraction.PlayLiftWorld.ToString("F5"));

                f.it.SimulateHover(home);
                f.it.SimulatePress(home);
                Check(f.it.Dragging == f.card, "（前提）抓起来了");
                f.it.SimulateDrag(drop, 1f / 60f);
                Check(f.it.Dragging == f.card && f.flights == 0 && f.it.HandCardCountForTest == hand0,
                    "★C1 抬高**不够** ⇒ 即便落点解析已经能给出最近格，**也不提交**"
                    + "（拖拽态在、起飞 " + f.flights + "、手牌 " + f.it.HandCardCountForTest + "）"
                    + "｜判据 `BattleManager__Update.c:253`（`pointerCursorStart.y + 0x7C < mouse.y`）"
                    + "｜🧨 把 `PlayCommitGate` 那一道删掉 ⇒ 这一帧就出牌 ⇒ 红");
            }
            finally { f.Dispose(); }
        }

        // C-2：抬高够，但指针**仍在手牌限位线以下**
        {
            var f = Make(0.20f, 0.12f, "B120W24_C2");
            try
            {
                Vector3 home = f.card.transform.position;
                Vector3 drop = BlankAboveBoard(0.20f);
                int hand0 = f.it.HandCardCountForTest;

                Check(LayoutSpace.ToNormalized(drop).y < BoardLayout.HandLimitY,
                    "（前提）C2 的落点仍在**手牌限位线以下**（归一化 y "
                    + LayoutSpace.ToNormalized(drop).y.ToString("F4") + " < " + BoardLayout.HandLimitY.ToString("F4") + "）");
                Check(drop.y - home.y >= CardInteraction.PlayLiftWorld,
                    "（前提）C2 抬高 " + (drop.y - home.y).ToString("F4") + " ≥ "
                    + CardInteraction.PlayLiftWorld.ToString("F5"));

                f.it.SimulateHover(home);
                f.it.SimulatePress(home);
                Check(f.it.Dragging == f.card, "（前提）抓起来了");
                f.it.SimulateDrag(drop, 1f / 60f);
                Check(f.it.Dragging == f.card && f.flights == 0 && f.it.HandCardCountForTest == hand0,
                    "★C2 指针**没越手牌限位线** ⇒ **不提交**（拖拽态在、起飞 " + f.flights + "）"
                    + "｜判据 `BattleManager__Update.c:249`（`HandLimitArea.y < mouse.y`）"
                    + "｜🧨 把「越线」这一道删掉 ⇒ 在手牌带里就被提交 ⇒ 红");
            }
            finally { f.Dispose(); }
        }
    }

    // ==================================================================
    //  D · 取消路径（本件**不许**破坏）：指针留在手牌带里 ⇒ 松手照旧牌回手
    // ==================================================================
    static void D_CancelPathUntouched()
    {
        var f = Make(0.42f, 0.12f, "B120W24_D");
        try
        {
            Vector3 home = f.card.transform.position;
            Vector3 inHand = LayoutSpace.ToWorld(0.95f, 0.12f);   // 横向甩得很远、但**仍在手牌带**
            int hand0 = f.it.HandCardCountForTest;

            f.it.SimulateHover(home);
            f.it.SimulatePress(home);
            Check(f.it.Dragging == f.card, "（前提）抓起来了");

            f.it.SimulateDrag(inHand, 1f / 60f);
            Check(f.it.Dragging == f.card && f.flights == 0,
                "（前提）拖在手牌带里（哪怕横向甩到 0.95）⇒ 不提交");

            f.it.SimulateRelease(inHand);
            Check(f.it.Dragging == null && f.it.HandCardCountForTest == hand0,
                "★D1 没过门时松手 ⇒ **牌回手**、张数不变（" + hand0 + "）—— 「最近格」那一跳**没有**污染取消路径"
                + "｜🧨 把 `allowNearest` 也接到「不过门」的那一档 ⇒ 牌被误打出去 ⇒ 红");
            Check(f.returns == 1 && f.flights == 0,
                "★D2 走的是**取消**那一支（`OnReturned` 实得 " + f.returns + "、起飞 " + f.flights + "）");
        }
        finally { f.Dispose(); }
    }

    // ==================================================================
    //  E · `B120-C09`：近战跳过档 —— 逐帧 `CardTween.Advance` 量真补间
    //     判据：段1/2 = `AttackStepTime × stepScale`（0.1 × s）· 段3 = `AttackHoldTime`（**0.1 字面量、不缩**）·
    //           段4/5 = `AttackReturnTime × stepScale`（0.2 × s）⇒ 正常 0.4 / 跳过档（s = 1/3）**0.2**。
    //     判据源：`CardScript__get_AttackStepTime.c:8-14` · `_AttackMeleeAnim_d__357__MoveNext.c:198,246,259,265,279`。
    // ==================================================================
    const float ExpStep = 0.1f;        // 原版 `attackStepTime`（卡实例字段，`CardScript+0x1A0`）
    const float ExpHold = 0.1f;        // 原版 `AppendInterval(DAT_1834b2dc4)` = **0.1 字面量**
    const float ExpReturn = 0.2f;      // 原版段4/5 = `step + step`
    const float ExpAfter = 0.65f;      // 原版 `SetDelay(DAT_1834b315c)` = 0.65 字面量（⛔ 不除）
    const float SkipDiv = 3f;          // 原版 `get_AttackStepTime` 的 `ReplaySkipAnims` 分支：÷ 3.0

    /// <summary>逐帧推进到累计 <paramref name="target"/> 秒（1/120 s 一格，最后一格截断）。</summary>
    static void StepTo(ref float now, float target)
    {
        const float Dt = 1f / 120f;
        int guard = 0;
        while (now + 1e-6f < target && guard++ < 100000)
        {
            float dt = Mathf.Min(Dt, target - now);
            CardTween.Advance(dt);
            now += dt;
        }
    }

    static void E_MeleeSkipRuntime()
    {
        Vector3 home = Vector3.zero;
        Vector3 end = new Vector3(3f, 0f, 0f);

        // ---- E1/E2：两条序列的**总时长**（DOTween 自己的时长账 = 公式）----
        var goN = new GameObject("B120W24_MeleeN");
        var goS = new GameObject("B120W24_MeleeS");
        Tween twN = null, twS = null;
        try
        {
            twN = CardFeel.MeleeAttack(goN.transform, end, home, 0f, true, 1f);
            twS = CardFeel.MeleeAttack(goS.transform, end, home, 0f, true, 1f / SkipDiv);
            Check(twN != null && twS != null, "（前提）两条序列都建出来了");
            if (twN == null || twS == null) return;

            float durN = CardFeel.SequenceSeconds(twN);
            float durS = CardFeel.SequenceSeconds(twS);
            Check(Mathf.Abs(durN - (ExpStep + ExpHold + ExpReturn)) < 1e-4f,
                "★E1 近战整条**正常档** = " + durN.ToString("F4") + " s（应 = 0.1 + **0.1** + 0.2 = 0.4）");
            Check(Mathf.Abs(durS - (ExpStep / SkipDiv + ExpHold + ExpReturn / SkipDiv)) < 1e-4f,
                "★E2 近战整条**跳过档** = " + durS.ToString("F4") + " s（应 = 0.1/3 + **0.1（段3 不除）** + 0.2/3 = 0.2）"
                + "｜🧨 把整条 0.4 一起 ÷3 ⇒ 0.1333 ⇒ 红");
            Check(Mathf.Abs(durS - (ExpStep + ExpHold + ExpReturn) / SkipDiv) > 0.05f,
                "★E3 **灭自证**：跳过档 " + durS.ToString("F4") + " s ≠ 「整条 ÷3」"
                + ((ExpStep + ExpHold + ExpReturn) / SkipDiv).ToString("F4") + " s（差 > 0.05 ⇒ 换回整条 ÷3 必红）");
            Check(Mathf.Abs(CardFeel.MeleeAttackDurationOf(1f / SkipDiv) - durS) < 1e-4f
                  && Mathf.Abs(CardFeel.MeleeAttackDurationOf(1f) - durN) < 1e-4f,
                "★E4 公式 ↔ 实际序列**一致**（`MeleeAttackDurationOf` 与 `MeleeAttack` 建出来的总长逐位相同）"
                + " —— 只改一边 ⇒ 红");

            // ---- E5/E6：跳过档**逐帧**量「段1 末」与「段3 中」 —— 段 3 【不缩】 ----
            float t = 0f;
            StepTo(ref t, ExpStep / SkipDiv + 0.0067f);           // ≈ 0.04：段1（0.0333）刚跑完
            float dSeg1 = Vector3.Distance(goS.transform.position, end);
            Check(dSeg1 < 0.02f,
                "★E5 跳过档 0.04 s（段1 名义 0.0333 s 之后）卡**已经到终点**（离终点 " + dSeg1.ToString("F4") + "）"
                + "｜🧨 段1 不乘 `stepScale`（仍用 0.1）⇒ 这里还没走完 ⇒ 红");

            float tHold = ExpStep / SkipDiv + ExpHold * 0.5f;      // ≈ 0.0833：段3 正中间
            StepTo(ref t, tHold);
            float dHold = Vector3.Distance(goS.transform.position, end);
            Check(dHold < 0.02f,
                "★E6 **段3 不缩**：跳过档在 " + tHold.ToString("F4") + " s（段3 的正中间）卡**仍停在终点**"
                + "（离终点 " + dHold.ToString("F4") + "）—— 原版 `AppendInterval(DAT_1834b2dc4)` 是 0.1 **字面量**、不走 `get_AttackStepTime()`"
                + "｜🧨 **段3 也 ÷3** ⇒ 0.0667 s 就该往回走 ⇒ 这里已离终点 ~2 世界单位 ⇒ 红");

            float tBack = ExpStep / SkipDiv + ExpHold + ExpReturn / SkipDiv * 0.25f;   // ≈ 0.15：回程 1/4 处
            StepTo(ref t, tBack);
            float xBack = goS.transform.position.x;
            Check(xBack < end.x - 0.5f && xBack > home.x + 0.05f,
                "★E7 跳过档 " + tBack.ToString("F4") + " s ⇒ 卡**正在回位**（x = " + xBack.ToString("F4")
                + "，终点是 3）—— 证明段4 真的在 0.1333 s 起跑（⛔ 不是被拉长到 0.4 那一档）");
            Tr("E 段读数：跳过档 0.04/0.0833/0.15 s 的 x = "
               + goS.transform.position.x.ToString("F4") + "（末帧）· 序列总长 " + durS.ToString("F4"));
        }
        finally
        {
            if (twN != null) DG.Tweening.TweenExtensions.Kill(twN, false);   // ⛔ 不 Kill 就销毁宿主 ⇒ 后面每次 `Advance` 都会撞「target missing」
            if (twS != null) DG.Tweening.TweenExtensions.Kill(twS, false);
            UnityEngine.Object.DestroyImmediate(goN);
            UnityEngine.Object.DestroyImmediate(goS);
        }

        // ---- E8：正常档的对照（段3 同样是 0.1，而不是被跳过档的逻辑连带缩短）----
        var goN2 = new GameObject("B120W24_MeleeN2");
        Tween twN2 = null;
        try
        {
            twN2 = CardFeel.MeleeAttack(goN2.transform, end, home, 0f, true, 1f);
            float t2 = 0f;
            StepTo(ref t2, ExpStep + 0.006f);                     // ≈ 0.106：段1 刚跑完
            float d1 = Vector3.Distance(goN2.transform.position, end);
            Check(d1 < 0.02f,
                "★E8 正常档 0.106 s（段1 名义 0.1 s 之后）卡**已经到终点**（离终点 " + d1.ToString("F4") + "）");
            float tHold2 = ExpStep + ExpHold * 0.9f;              // ≈ 0.19：段3 尾部
            StepTo(ref t2, tHold2);
            float d2 = Vector3.Distance(goN2.transform.position, end);
            Check(d2 < 0.02f,
                "★E9 正常档 " + tHold2.ToString("F4") + " s（段3 尾部）卡**仍停在终点**（离终点 " + d2.ToString("F4") + "）");
        }
        finally
        {
            if (twN2 != null) DG.Tweening.TweenExtensions.Kill(twN2, false);
            UnityEngine.Object.DestroyImmediate(goN2);
        }
    }

    // ==================================================================
    //  F · `B120-C09`：灭自证（整条 ÷3 的旧口径）+ 收招跳过档 + 命中拍间隔
    // ==================================================================
    static void F_RecoverAndHitGap()
    {
        Vector3 home = Vector3.zero;
        Vector3 end = new Vector3(3f, 0f, 0f);

        // ---- F1/F2：**旧口径快照**（整条 0.4 一起 ÷3）在同一时刻必须已经离开终点 ----
        var goOld = new GameObject("B120W24_Old");
        Sequence old = null;   // ⚠️ 类型必须是 `Sequence`（`Append`/`AppendInterval` 是它的方法，不在 `Tween` 上）
        try
        {
            old = DOTween.Sequence();
            old.SetUpdate(UpdateType.Manual);   // ⚠️ 必须 Manual，否则本件的 `CardTween.Advance` 推不动它（F2 会假绿）
            old.Append(goOld.transform.DOMove(end, ExpStep / SkipDiv).SetEase(Ease.Linear));
            old.AppendInterval(ExpHold / SkipDiv);                       // ← 旧口径：段3 也被 ÷3（**错的**）
            old.Append(goOld.transform.DOLocalMove(home, ExpReturn / SkipDiv).SetEase(Ease.OutQuart));
            float durOld = CardFeel.SequenceSeconds(old);
            Check(Mathf.Abs(durOld - (ExpStep + ExpHold + ExpReturn) / SkipDiv) < 1e-3f,
                "★F1（灭自证前提）「整条 ÷3」快照的总长 = " + durOld.ToString("F4") + " s（= 0.1333）—— "
                + "这正是 E3 说「不等」的那一档");
            float t = 0f;
            StepTo(ref t, ExpStep / SkipDiv + ExpHold * 0.5f);           // 与 E6 同一时刻 ≈ 0.0833
            float dOld = Vector3.Distance(goOld.transform.position, end);
            Check(dOld > 0.5f,
                "★F2（灭自证）同一时刻 " + t.ToString("F4") + " s 喂给**旧口径快照** ⇒ 卡已离开终点 "
                + dOld.ToString("F2") + " 世界单位 —— ⇒ 旧口径下 **E6 必红**（这一条证明 E6 有判别力）");
        }
        finally
        {
            if (old != null) DG.Tweening.TweenExtensions.Kill(old, false);
            UnityEngine.Object.DestroyImmediate(goOld);
        }

        // ---- F3/F4：收招（`AttackRecover`）两段都随跳过档缩、中间 0.65 不缩 ----
        var goR1 = new GameObject("B120W24_RecN");
        var goR2 = new GameObject("B120W24_RecS");
        Tween twRN = null, twRS = null;
        try
        {
            twRN = CardFeel.AttackRecover(goR1.transform, home, 1f, 0f, true, 1f);
            twRS = CardFeel.AttackRecover(goR2.transform, home, 1f, 0f, true, 1f / SkipDiv);
            float durRN = CardFeel.SequenceSeconds(twRN);
            float durRS = CardFeel.SequenceSeconds(twRS);
            Check(Mathf.Abs(durRN - (ExpStep + ExpAfter + ExpStep)) < 1e-4f,
                "★F3 收招**正常档** = " + durRN.ToString("F4") + " s（应 = 0.1 + **0.65** + 0.1 = 0.85）");
            Check(Mathf.Abs(durRS - (ExpStep / SkipDiv + ExpAfter + ExpStep / SkipDiv)) < 1e-4f,
                "★F4 收招**跳过档** = " + durRS.ToString("F4") + " s（应 = 0.1/3 + **0.65（SetDelay 是字面量、不除）** + 0.1/3 = 0.71667）"
                + "｜🧨 段2 写死 0.1（不乘 stepScale）⇒ 0.75 ⇒ 红");
            Check(Mathf.Abs(durRS - durRN / SkipDiv) > 0.1f,
                "★F5 **灭自证**：收招跳过档 " + durRS.ToString("F4") + " ≠ 「整条 ÷3」" + (durRN / SkipDiv).ToString("F4")
                + " s（差 > 0.1 —— 0.65 那一段不除）");
            Check(Mathf.Abs(CardFeel.AttackRecoverDurationOf(1f / SkipDiv) - durRS) < 1e-4f,
                "★F6 公式 ↔ 实际序列一致（`AttackRecoverDurationOf` 与 `AttackRecover` 逐位相同）");
        }
        finally
        {
            if (twRN != null) DG.Tweening.TweenExtensions.Kill(twRN, false);
            if (twRS != null) DG.Tweening.TweenExtensions.Kill(twRS, false);
            UnityEngine.Object.DestroyImmediate(goR1);
            UnityEngine.Object.DestroyImmediate(goR2);
        }

        // ---- F7/F8：命中拍间隔（`EventTiming.DelayBetween`）= `AttackHoldTime + 2×step` ----
        var keepSkip = EventTiming.ReplaySkipAnims;
        try
        {
            var atk = new BattleEvent { Kind = EvtKind.Attack, Ranged = false };
            var hit = new BattleEvent { Kind = EvtKind.Hit };

            EventTiming.ReplaySkipAnims = null;
            float gapN = EventTiming.DelayBetween(atk, hit);
            EventTiming.ReplaySkipAnims = () => true;
            float gapS = EventTiming.DelayBetween(atk, hit);
            EventTiming.ReplaySkipAnims = keepSkip;

            Check(Mathf.Abs(gapN - (ExpHold + 2f * ExpStep)) < 1e-4f,
                "★F7 命中拍间隔**正常档** = " + gapN.ToString("F4") + " s（应 = 0.1 + 2×0.1 = 0.3）"
                + "｜判据 `_AttackMeleeAnim_d__357__MoveNext.c:198,246,259,265,279`（段1 = 前摇，已算进 `DurationOf(Attack)`）");
            Check(Mathf.Abs(gapS - (ExpHold + 2f * ExpStep / SkipDiv)) < 1e-4f,
                "★F8 命中拍间隔**跳过档** = " + gapS.ToString("F4") + " s（应 = 0.1 + 2×0.0333 = 0.16667）"
                + "｜🧨 写回 `SkipScaled(MeleeDamageLag) − SkipScaled(AttackStepMelee)` ⇒ 0.1 ⇒ 红（那会把段3 也除 3）");
            Check(Mathf.Abs((gapS + ExpStep / SkipDiv) - (ExpStep / SkipDiv + ExpHold + ExpReturn / SkipDiv)) < 1e-4f,
                "★F9 **两条链自洽**：「跳过档命中间隔 " + gapS.ToString("F4") + "」+ 「段1 " + (ExpStep / SkipDiv).ToString("F4")
                + "」= 近战整条跳过档 0.2 s —— 事件时间轴与补间用的是**同一套** step/段3 口径"
                + "｜🧨 只改一边 ⇒ 红");
        }
        finally { EventTiming.ReplaySkipAnims = keepSkip; }
    }
}
