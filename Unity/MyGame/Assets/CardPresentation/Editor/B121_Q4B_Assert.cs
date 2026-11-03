// B121_Q4B_Assert.cs — 执行写手 `B121-Q4B`（2026-11-03）· 账 `B120-Q04c4b`
//                        **已提交、但还按着、指针又拖回手牌线以下 ⇒ 撤销出牌**
//
// ============================ 它盯的是什么 ============================
//  原版那一刻（判据全部亲读）：
//   · `cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:25991-25998`（`case 3 = playingMinion`，
//     `FUN_180617800(...) != 0` = **还按着**）：指针 y 掉回 `HandLimitArea.y` 以下 ⇒
//     `cardTracked.OutFromPlayingAreaAnim(); RemoveSwarm(0); UIstate = movingHandCard; ReassembleMinions(0);`
//     —— `decomp_full/BattleManager__Update.c:568-590` 逐句同形；同一形状在 `case 4`(`:823`) / `case 5`(`:951`) 各一份。
//   · `decomp_full/CardScript__OutFromPlayingAreaAnim.c:11-42` = `MoveBackToHand` 那一对动作
//     + `localScale = GetCardInHandMovingScale()`（**只 `targeted` 支**）+ `cardState = 9`(`inHandMoving`)
//     + 末句 `PlayerHand.LightPlayableHand()`。
//  ⇒ 我们的等价物（唯一入口 = `CardInteraction.CancelDeployFlight`，驱动层那一半 = `BattleDriver.CancelDeployFlight`）：
//     `Kill` 掉 `DeploySequence.Play` 那条序列（撤已排程的出牌）· 撤 blocking window · 撤已排程 / 已起播的登场 clip ·
//     起飞票作废 · 换回手牌形态 + 父链 + 层 · `ApplyOutFromPlayingArea`（`B120-W28` 落的那个）
//     · `_placed` 撤登记 · `_cards` **原位**插回 · `Relayout(false)`（起拖档）· 末态 = 原版 `UIstate = movingHandCard`（`_dragging` 放回这张，卡继续跟指针）。
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//  🧨 A6 ← 去掉 `tween.Kill(false)` ⇒ 序列照常跑完 ⇒ A6 那格变 1（`OnDeployed` 被调到）⇒ 红；D1 证明「本来**会**发生」。
//  🧨 A4 ← 撤销判定不看「还按着」/ 不看「在线下」⇒ A2（还在出牌区里）或 A3（已松手）会变成 true ⇒ 红。
//  🧨 A5b ← 撤销后不清票 ⇒ 同一张能连撤两次 ⇒ 红。
//  🧨 A10 ← 不换回父链（0.95 s 交接会把卡挂到 `boardRoot`）⇒ A10 红。
//  🧨 A12 ← `_cards.Insert` 写成 `Add`（或下标算错）⇒ 顺序变成 [B, A] ⇒ 红。
//  🧨 A13 ← 撤销分支不把 `_dragging` 放回这张卡 ⇒ 卡不跟指针、要松手再按 ⇒ 红；A14/A15 是它的**行为侧**判别式（只摆标志位不驱动 ⇒ A14/A15 红、A13 假绿）。
//  🧨 A15 ← 撤回来那张被 `Relayout` 按回槽位 / `UpdateDrag` 不驱动它 ⇒ 再抬回出牌区不会重提 ⇒ 红。
//  🧨 B2 ← 撤销时不撤 blocking window ⇒ 玩家被锁 1.3 s / 撞 20 s 安全阀 ⇒ 红。
//  🧨 B4 ← 去掉 `MaybeStartHandToBoardClip` 的闸 ⇒ 0.3 s 那条登场 clip 照旧起播 ⇒ 红；B6 是它的判别式。
//  🧨 C1 ← 把撤销判定写成「指针在线下就撤」（不看有没有票）⇒ 未提交那一拖也会撤 ⇒ 红。
//
// ============================ ⚠️ 覆盖边界（如实记）============================
//  · **不跑 Unity 帧循环**：补间一律 `CardTween.Mode = Manual`（退出时原样还原），`DOTween` 由 `CardTween.Advance` 手动推。
//  · **夹具没有 `Ctx`**：`CardInteraction.SideCount` 走「数不出来」那一支（按该侧皆空处理，与产品路径同一条判据）；
//    `BattleDriver.CancelDeployFlight` 的 `SyncBoard` 那一句有 `Ctx != null` 闸 ⇒ 夹具里不跑（让位预览回滚由
//    `B115_W14` 的引擎拒支同一写法保证，本件只验「撤销那一支把它接上了」）。
//  · **驱动层那半边是「生产同一入口」**（`BattleDriver.SimulatePlayerDeployFlightStartForTest → OnPlayerDeployFlightStart`），
//    不是替身；但它跑在**没有 Ctx / 没有战场**的夹具里 ⇒ `animateFeel = true` 时排的那条登场 clip 会因为
//    取不到 effect library / 3D 网格而**出声警告**（`CardFeel.HandToBoard` / `MountSpawnParticlesUnderToBoardFx` 的守卫），
//    这是夹具边界、不是缺陷。
//  · **不验手感/观感**（撤销后卡跟手的顺滑度、倾角）：那要真 Play（`资料/真Play待验清单.md`）—— 本件只验「`_dragging` 回来了」＋「下一帧真的跟手」＋「能立刻重提」这三件**可观测**的。
//  · **本件还没接线**：入口 `Run()`（无参、零前提）。建议接在 `Editor/BattleScene.cs` 的聚合行上，
//    `B121_WB_Assert.Run();` 之后（同一族：手牌拖拽 / 出牌）。
using System;
using System.Collections.Generic;
using DG.Tweening;                 // `Transform.DOKill()`（`Fixture.Dispose` 的收尾）
using UnityEngine;
using CardPresentation;

/// <summary>`B121-Q4B` 的断言集：**提交后拖回手牌线以下 ⇒ 撤销出牌**（原版 `case 3/4/5` 那一支）。</summary>
public static class B121_Q4B_Assert
{
    const string P = "B121_Q4B ";
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
            CardTween.Mode = DG.Tweening.UpdateType.Manual;   // 批处理没有帧循环 ⇒ 补间不许自己跑
            CardTween.LinkEnabled = false;

            Group("A·已提交后拖回线下 ⇒ 撤销（卡片自己那一半）", A_CancelAfterCommit);
            Group("B·驱动层那一半（blocking window / 已排程 clip）", B_DriverHalf);
            Group("C·反证：未提交时拖回线下**不**走撤销", C_NotCommitted);
            Group("D·灭自证（Kill 之前它本来会真的落地）", D_AntiSelfProof);
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
        Debug.Log(P + "B121_Q4B 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ==================================================================
    //  夹具（与 `B120_W28_Assert` 同一打法：手摆几何 + `CanDropAtSlot` 替身）
    // ==================================================================
    sealed class Fixture : IDisposable
    {
        public GameObject root;
        public HandLayout hand;
        public BoardLayout board;
        public CardInteraction it;
        public BattleDriver drv;         // 只有 B/D 组要（A/C 组不需要驱动层）
        public CardView card;            // 第一张
        public int flights, returns, lights, deployed;

        public void Dispose()
        {
            // 先把还没用掉的撤销票用掉（顺带 Kill 那条落位序列 / 登场 clip），再销毁 ——
            // 否则「夹具已销毁、手动补间还在推进」会刷一堆缺目标报错。
            try { if (it != null) it.SimulateDeployCancelTickForTest(LayoutSpace.ToWorld(0.5f, 0.02f), true); } catch { }
            if (card != null) { try { card.transform.DOKill(); } catch { } }
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    /// <summary>最小夹具：手牌布局 + 一块 2D 棋盘 + 交互器（可选：一个裸 `BattleDriver`）。</summary>
    static Fixture Make(string name, bool withDriver = false)
    {
        var f = new Fixture();
        f.root = new GameObject("B121Q4B_root_" + name);
        f.hand = f.root.AddComponent<HandLayout>();
        f.board = f.root.AddComponent<BoardLayout>();
        f.it = f.root.AddComponent<CardInteraction>();
        f.it.hand = f.hand;
        f.it.board = f.board;
        f.it.SetRightButtonHeldForTest(false);      // 批处理没有鼠标设备 ⇒ 把「右键按着」那道闸钉成 false
        f.it.SetBattleStateBlockedForTest(false);   // 本文件只验撤销链 ⇒ 战斗状态闸全放行
        f.it.CanDropAtSlot = (slot, c) => true;     // 替身：引擎说这一格能打（撤销链与引擎拒绝无关）
        f.hand.animateRelayout = false;             // 夹具：位姿当场精确
        f.board.lineY = 0.42f;
        f.board.use3D = false;                      // 2D 兜底档（与 W24/W28 同一档）
        f.it.OnDeployFlightStart += (cv, bl, sl, dur) =>
        {
            f.flights++;
            // 生产里这条事件就接到驱动层那一半（`Begin()` 的接线）；夹具在这里**显式走同一个入口**。
            if (f.drv != null) f.drv.SimulatePlayerDeployFlightStartForTest(cv, dur);
        };
        f.it.OnReturned += (cv, k) => f.returns++;
        f.it.OnLightPlayableHand += () => f.lights++;
        f.it.OnDeployed += (cv, sl) => f.deployed++;
        if (withDriver)
        {
            f.drv = f.root.AddComponent<BattleDriver>();
            f.drv.animateFeel = true;                       // 真机演出档 ⇒ 起飞会按 `DeploySequence.MoveTime` 排登场 clip
            f.it.OnCancelDeployFlight += f.drv.CancelDeployFlight;
        }
        f.card = NewCard(f, name, 0.4f);
        f.it.SetCards(new List<CardView> { f.card }, false);
        return f;
    }

    static CardView NewCard(Fixture f, string name, float nx)
    {
        var def = CardData.Placeholder(1);
        def.id = name; def.artId = name;             // ⛔ 别撞真实卡：`IsTargetedCard` / `TacticTargetSide` 会拿 id 回查卡表
        def.isUnit = true; def.type = "unit"; def.rarity = "common";
        var c = CardView.Create(f.root.transform, def, name, CardFace.Hand);
        c.transform.position = LayoutSpace.ToWorld(nx, 0.12f);
        return c;
    }

    static List<CardView> HandList(Fixture f)
    {
        var list = new List<CardView>();
        for (int i = 0; i < f.it.HandCardCountForTest; i++) list.Add(f.it.HandCardForTest(i));
        return list;
    }

    /// <summary>把某一张**真的提交出去**：按下 → 拖到出牌区里、抬高过 `PlayCommitGate` ⇒ 原版「还按着就 commit」
    /// 那一跳（`CardInteraction.TryCommitWhileHolding → Release(world, true)`）。返回 true = 真的进了打出链。</summary>
    static bool Commit(Fixture f, CardView card)
    {
        Vector3 home = card.transform.position;
        Vector3 inArea = LayoutSpace.ToWorld(0.5f, 0.55f);   // 线上方 + 比按下点高得多
        f.it.SimulateHover(home);
        f.it.SimulatePress(home);
        if (!f.it.IsDragging) return false;
        f.it.SimulateDrag(inArea, 1f / 60f);
        return f.flights >= 1 && !f.it.IsDragging;
    }

    // ==================================================================
    //  A · 已提交后拖回线下 ⇒ 撤销（卡片自己那一半）
    // ==================================================================
    static void A_CancelAfterCommit()
    {
        var f = Make("B121Q4B_A", withDriver: true);
        try
        {
            // 两张手牌：验「**原位**插回」（不是简单 Add 到末尾）
            var second = NewCard(f, "B121Q4B_A_b", 0.62f);
            f.it.SetCards(new List<CardView> { f.card, second }, false);

            Check(!f.it.CanCancelDeployForTest, "（前提）提交之前**没有撤销票**");
            // 批处理补刀的起点快照（见类尾 `KillDeployInBatch`：产品那句 `tween.Kill(false)` 在批处理里是空操作）
            var mark = B124_TweenTestUtil.SnapshotActive();
            Check(Commit(f, second), "（前提）把**第二张**拖进出牌区并抬高 ⇒ 真的提交了（`OnDeployFlightStart` 实得 " + f.flights + " 次）");
            Check(f.it.CanCancelDeployForTest && ReferenceEquals(f.it.CancelDeployCardForTest, second),
                "★A1 提交后有一条「可撤销」的票，票上就是刚打出去那张");
            Check(f.it.HandCardCountForTest == 1 && ReferenceEquals(f.it.HandCardForTest(0), f.card),
                "（前提）提交后那张已从手牌列表拿掉（只剩第一张）");
            Check(f.it.Placed.Count == 1, "（前提）提交时把落点登记进 `_placed` 了（原版那半边的登记，撤销要撤掉）");

            // —— 三种时刻，只有第三种该撤 ——
            Check(!f.it.SimulateDeployCancelTickForTest(LayoutSpace.ToWorld(0.5f, 0.55f), true),
                "★A2 还按着、但指针**在出牌区里** ⇒ 不撤销（原版那一支 `local_5a8._4_4_ < 指针.y` 为假 = 「卡跟手」）");
            Check(f.it.CanCancelDeployForTest, "★A2b 票还在（上面那一下没有用掉它）");
            Check(!f.it.SimulateDeployCancelTickForTest(LayoutSpace.ToWorld(0.5f, 0.05f), false),
                "★A3 指针掉回线下、但**已经松手** ⇒ 不撤销（原版 `case 3` 的 `FUN_180617800(...) != 0` = 还按着）");

            bool cancelled = f.it.SimulateDeployCancelTickForTest(LayoutSpace.ToWorld(0.5f, 0.05f), true);
            Check(cancelled, "★A4 还按着 ∧ 指针掉回 `HandLimitY` 以下 ⇒ **撤销**"
                + "（原版 `OutFromPlayingAreaAnim` 那一支：`BattleManager.cs:25991-25998` · `Update.c:568-590`）");
            KillDeployInBatch(mark, second, "A");   // 🔴 批处理补刀：让 A6 的「0」是**真判**（序列真的死了），不是「没人推进它」

            Check(f.it.HandCardCountForTest == 2, "★A5 卡**回到手牌列表**（张数 2，实得 " + f.it.HandCardCountForTest + "）");
            Check(ReferenceEquals(f.it.HandCardForTest(1), second),
                "★A12 **原位插回**：它回到**原来那个下标 1**（顺序仍是 [第一张, 刚撤销那]）"
                + "｜🧨 `_cards.Insert` 写成 `Add` / 下标算错 ⇒ 这里对不上 ⇒ 红");
            Check(!f.it.CanCancelDeployForTest && f.it.CancelDeployCardForTest == null,
                "★A5b 票已用掉（不会连撤两次）");
            Check(f.it.CancelDeployCountForTest == 1, "★A5c 撤销真的走了 **1** 次（计数 " + f.it.CancelDeployCountForTest + "）");
            Check(f.deployed == 0, "★A6 **没有**走到 `OnDeployed`（= `DoPlay` / `RuleCore.PlayCard` 一步都没发生）"
                + "—— 这就是「撤掉已排程的出牌动作」"
                + "｜🧨 去掉序列 `Kill` ⇒ 这条在 D1 那种推进下会变 1 ⇒ 红");
            Check(f.it.Placed.Count == 0, "★A6b `_placed` 里那一格撤掉了（实得 " + f.it.Placed.Count + " 条）");
            Check(second.Queue == CardView.QHandMoving,
                "★A7 `cardState = 9`(`inHandMoving`) 的等价物 = 队列换到 `QHandMoving`（" + CardView.QHandMoving
                + "，实得 " + second.Queue + "）"
                + "｜判据 `OutFromPlayingAreaAnim.c:30`｜🧨 不调 `ApplyOutFromPlayingArea` ⇒ 仍是原队列 ⇒ 红");
            Check(f.lights == 1, "★A8 原版末句 `PlayerHand.LightPlayableHand()` 触发 **1** 次（实得 " + f.lights + "）"
                + "｜判据 `OutFromPlayingAreaAnim.c:34-38`｜🧨 不抬事件 ⇒ 0 ⇒ 红");
            Check(second.Face == CardFace.Hand, "★A9 卡面回到**手牌形态**（`CardFace.Hand`，实得 " + second.Face + "）");
            Check(second.transform.parent == f.root.transform,
                "★A10 父链回到**手牌根**（离手前那一档）"
                + "｜🧨 不换回来 ⇒ 0.95 s 交接之后它会留在 `boardRoot` 下 ⇒ 红");
            Check(second.Body3DVisible == false, "★A11 3D 卡体关着（手牌形态；撤销把 clip 中途打开的 3D 体也关掉了）");

            // ══════════════════════════════════════════════════════════════════════════════
            //  🆕 2026-11-03（`B124-W14` · 账 `B121-Q4B-01`）：撤销后**回到 `UIstate = movingHandCard`**
            //  判据：原版 `case 3` 的末句就是它（`decomp_full/BattleManager__Update.c:568-590` /
            //  il2csharp `BattleManager.cs:25991-25998`）⇒ 卡**不落回手牌槽**、继续按指针跟手，
            //  越过手牌限位线那一刻照常重提 —— 原版没有「松手再按下」这一档。
            // ══════════════════════════════════════════════════════════════════════════════
            Check(f.it.IsDragging && ReferenceEquals(f.it.Dragging, second),
                "★A13 撤销后回到 `movingHandCard`：`_dragging` 就是刚撤回来那张（不是只把卡摆回手牌槽位）"
                + "｜🧨 撤销分支不设 `_dragging` ⇒ 这里 false ⇒ 红（玩家要松手再按下才能再拖）");

            // ---- ★A14 跟手。⚠️ **必须排在 A15 之前**：A15 的「抬回去重提」会把 `_dragging` 置空，次序反了就测不到跟手 ----
            float x0 = second.transform.position.x;
            Vector3 inHandHere = LayoutSpace.ToWorld(0.2f, 0.05f);   // 手牌带内、线下 ⇒ 过不了 `PlayCommitGate`
            f.it.SimulateDrag(inHandHere, 1f / 60f);
            float x1 = second.transform.position.x;
            Check(Mathf.Abs(x1 - inHandHere.x) < Mathf.Abs(x0 - inHandHere.x) - 1e-3f,
                "★A14 还按着、指针在手牌带里挪 ⇒ 卡**朝指针走**（x: " + x0.ToString("F3") + " → " + x1.ToString("F3")
                + "，指针 x=" + inHandHere.x.ToString("F3") + "）"
                + "｜🧨 `_dragging` 设了但 `UpdateDrag` 不驱动 / 卡被 `Relayout` 按回槽位 ⇒ 位移 0 ⇒ 红");
            Check(f.flights == 1 && !f.it.CanCancelDeployForTest && f.it.HandCardCountForTest == 2,
                "★A14b（前提）手牌带里这一趟**没有**再提交（`OnDeployFlightStart` 仍是 " + f.flights
                + " 次、票「" + (f.it.CanCancelDeployForTest ? "有" : "无") + "」），且卡仍在手牌列表里（"
                + f.it.HandCardCountForTest + " 张）—— 跟手 ≠ 已落位");

            // ---- ★A15 灭自证：撤回来那张**不松手**再抬回出牌区 ⇒ 真的又提交了一次 ----
            f.it.SimulateDrag(LayoutSpace.ToWorld(0.5f, 0.55f), 1f / 60f);
            Check(f.flights == 2 && f.it.CanCancelDeployForTest,
                "★A15 灭自证：撤销后**不用松手**，再抬回出牌区 ⇒ **真的又提交了一次**（`OnDeployFlightStart` 实得 "
                + f.flights + " 次，票「" + (f.it.CanCancelDeployForTest ? "有" : "无") + "」）"
                + "｜🧨 只把 `_dragging` 摆在那儿、`UpdateDrag` 不驱动它 ⇒ 仍是 1 次 ⇒ 红（那就是「要松手再按」那个残余差）");
        }
        finally { f.Dispose(); }
    }

    // ==================================================================
    //  B · 驱动层那一半
    // ==================================================================
    static void B_DriverHalf()
    {
        // ---- B1~B5：撤销 ⇒ 闸放行 + 已排程 clip 不起播 ----
        {
            var f = Make("B121Q4B_B", withDriver: true);
            try
            {
                int clips0 = f.drv.HandToBoardClipsStartedForTest;
                Check(!f.drv.WaitingForBlockingAction, "（前提）提交之前 blocking window **是放行的**");
                var markB = B124_TweenTestUtil.SnapshotActive();   // 批处理补刀的起点（见类尾 `KillDeployInBatch`）
                Check(Commit(f, f.card), "（前提）提交了");
                Check(f.drv.WaitingForBlockingAction,
                    "★B1 提交后 blocking window **真的开着**（= 原版「牌一离手，`WaitingForBlockingAction` 就真」）"
                    + "｜🧨 起飞不 `BeginBlockingWindow` ⇒ 这里 false ⇒ 红（那也是本条链的**前提**）");
                Check(ReferenceEquals(f.drv.DeployFlightCardForTest, f.card),
                    "（前提）驱动层记下了「这一手在飞的是这张」");
                Check(f.drv.HandToBoardClipsStartedForTest == clips0,
                    "（前提）0.3 s 之前登场 clip 还没起播（实得 " + f.drv.HandToBoardClipsStartedForTest + "）");

                Check(f.it.SimulateDeployCancelTickForTest(LayoutSpace.ToWorld(0.5f, 0.05f), true), "（前提）撤销了");
                KillDeployInBatch(markB, f.card, "B");   // 🔴 批处理补刀：不留一条还在推进的落位序列给后面各组
                Check(!f.drv.WaitingForBlockingAction,
                    "★B2 撤销后 blocking window **立刻放行**（原版 `UIstate = movingHandCard` ⇒ 玩家能继续操作）"
                    + "｜🧨 不撤闸 ⇒ 玩家被锁 1.3 s（最坏撞 20 s 安全阀）⇒ 红");
                Check(f.drv.DeployFlightCardForTest == null, "★B3 驱动层把「在飞的牌」清掉了");

                f.drv.AdvanceTimeline(0.4f);   // 0.3 s 那条排期到点（`ScheduleAction(DeploySequence.MoveTime, …)`）
                Check(f.drv.HandToBoardClipsStartedForTest == clips0,
                    "★B4 撤销之后**已排程的登场 clip 不再起播**（计数仍是 " + clips0 + "）"
                    + "｜🧨 去掉 `MaybeStartHandToBoardClip` 的闸 ⇒ 变 +1 ⇒ 红（判别式见 B6）");
                Check(!f.drv.WaitingForBlockingAction, "★B5 推进 0.4 s 之后闸仍然是放行的（不会因为排队那条 cue 再拦回去）");
            }
            finally { f.Dispose(); }
        }

        // ---- B6：判别式 —— 不撤销时 0.3 s 那条 clip **确实会**起播（否则 B4 的「不变」只是排期没成功）----
        {
            var g = Make("B121Q4B_B6", withDriver: true);
            try
            {
                int c0 = g.drv.HandToBoardClipsStartedForTest;
                Check(Commit(g, g.card), "（前提）对照夹具也提交了");
                g.drv.AdvanceTimeline(0.4f);
                Check(g.drv.HandToBoardClipsStartedForTest == c0 + 1,
                    "★B6 **判别式**：不撤销时 0.3 s 那条登场 clip **确实起播**（计数 +1，实得 "
                    + (g.drv.HandToBoardClipsStartedForTest - c0) + "）"
                    + "—— 否则 B4 的「计数没变」只是「排期根本没成功」的假绿");
            }
            finally { g.Dispose(); }
        }
    }

    // ==================================================================
    //  C · 反证：**未提交**时拖回线下 不 走撤销，仍走原 `OutFromPlayingAreaAnim` 那一半
    // ==================================================================
    static void C_NotCommitted()
    {
        var f = Make("B121Q4B_C");
        try
        {
            Vector3 home = f.card.transform.position;
            Vector3 inHand = LayoutSpace.ToWorld(0.5f, 0.12f);   // 还在手牌带里（线下）⇒ `PlayCommitGate` 不过
            // 🔴 2026-11-03（`B124-W27`）：**量纲** —— `TapThreshold` = 10 真实 px ÷ 屏高（批处理 480p ⇒ 0.2083 我们单位），
            //   而单张手牌会被 `Relayout` 摆到手牌中心 ⇒ 原写法 `inHand = ToWorld(0.5, 0.12)` 与 `home` **几乎同一点**
            //   （实测位移 ≈ 0）⇒ `_travel >= TapThreshold` 闸不过（`Hand/CardInteraction.cs:1550`）⇒ `ApplyOutFromPlayingArea` 不调。
            //   ⇒ 横向补足拖距（落点 y 不变 ⇒ 仍在手牌带 / 线下，语义不变）。
            float needTravel = CardInteraction.TapThreshold * 2f;
            if (Vector3.Distance(inHand, home) < needTravel) inHand += new Vector3(needTravel, 0f, 0f);
            f.it.SimulateHover(home);
            f.it.SimulatePress(home);
            Check(f.it.IsDragging, "（前提）抓起来了");
            f.it.SimulateDrag(inHand, 1f / 60f);
            Check(Vector3.Distance(inHand, home) >= CardInteraction.TapThreshold,
                "（前提）C2 这一拖**超过轻点阈值**（" + Vector3.Distance(inHand, home).ToString("F4") + " ≥ `TapThreshold` "
                + CardInteraction.TapThreshold.ToString("F4") + "；靠横向补足）—— 否则它按「轻点」处理、下面那条会空转绿");
            Check(f.flights == 0 && !f.it.CanCancelDeployForTest,
                "（前提）一直在手牌带里 ⇒ **没有提交**、也没有撤销票（`PlayCommitGate` 那两道门）");

            f.it.SimulateRelease(inHand);
            Check(f.it.CancelDeployCountForTest == 0,
                "★C1 **未提交**时拖回线下 ⇒ **不走撤销**那一支（计数 " + f.it.CancelDeployCountForTest + "）"
                + "｜🧨 把撤销判定写成「指针在线下就撤」（不看有没有票）⇒ 这里会 +1 ⇒ 红");
            Check(f.lights == 1 && f.returns == 1,
                "★C2 仍走**原** `OutFromPlayingAreaAnim` 那一半（`OnLightPlayableHand` " + f.lights
                + " / `OnReturned` " + f.returns + "，各 1 次）");
            Check(f.it.HandCardCountForTest == 1 && ReferenceEquals(f.it.HandCardForTest(0), f.card),
                "★C3 卡一直在手牌列表里（那一支从来不动 `_cards`）");
        }
        finally { f.Dispose(); }
    }

    // ==================================================================
    //  D · 灭自证：**Kill 之前**那条序列本来会真的把牌打出去
    // ==================================================================
    static void D_AntiSelfProof()
    {
        // ---- D1：同一套夹具、**不撤销** ⇒ 序列跑完会真的走到 `OnDeployed` ----
        {
            var f = Make("B121Q4B_D1");
            try
            {
                Check(Commit(f, f.card), "（前提）提交了");
                Check(f.deployed == 0, "（前提）此刻还没落地");
                CardTween.Advance(DeploySequence.TotalTime + 0.2f);
                Check(f.deployed == 1,
                    "★D1 **灭自证**：不撤销时同一条序列跑完**真的**会调 `OnDeployed`（= `DoPlay` 的入口，实得 "
                    + f.deployed + "）⇒ A6 的 0 是「Kill 掉了」，不是「本来就不会发生」"
                    + "｜🧨 去掉 `tween.Kill(false)` ⇒ A6 变 1 ⇒ 红");
            }
            finally { f.Dispose(); }
        }

        // ---- D2：撤销之后把补间推到同一条时刻 ⇒ 仍然不落地 ----
        {
            var g = Make("B121Q4B_D2");
            try
            {
                var markD = B124_TweenTestUtil.SnapshotActive();   // 批处理补刀的起点（见类尾 `KillDeployInBatch`）
                Check(Commit(g, g.card), "（前提）提交了");
                Check(g.it.SimulateDeployCancelTickForTest(LayoutSpace.ToWorld(0.5f, 0.05f), true), "（前提）撤销了");
                // 🔴 2026-11-03（`B124_W19`）：产品那句 `tween.Kill(false)` 在批处理里是**空操作**
                //    （`DOTween.initialized == false`）⇒ 不补这一刀，下面「推到 2 s 仍然没落地」会被**活着**的序列打红
                //    （原来这就是 D2 的红），而 A6 则是「没人推进它」的**假绿**。
                KillDeployInBatch(markD, g.card, "D2");
                CardTween.Advance(DeploySequence.TotalTime + 0.2f);
                Check(g.deployed == 0,
                    "★D2 撤销之后把补间推到 2 s ⇒ **仍然没有** `OnDeployed`（序列已被 `Kill`，实得 " + g.deployed + "）");
            }
            finally { g.Dispose(); }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════════════════════════
    //  🔴 2026-11-03（执行写手 `B124_W19`）：**批处理里「杀落位序列」那一刀原来没杀成**
    //  产品那一句 = `CardInteraction.CancelDeployFlight` 的 `tween.Kill(false)`，而
    //  `TweenExtensions.Kill` 的**第一句**就是 `if (!DOTween.initialized || t == null || !t.active) return;`
    //  而 `DOTween.Init(...)` 只在 Play 模式跑（`if (!Application.isPlaying || isQuitting) return null;`）
    //  ⇒ 批处理里它是**静默空操作**，那条 `DeploySequence` 照样活着、照样被 `CardTween.Advance` 推。
    //  后果（本轮日志）：`★D2` 一推进就红（序列跑完 ⇒ `OnDeployed`），而 `★A6`（`deployed == 0`）是**假绿**
    //  —— 它当时成立只是因为「没人推进那条序列」，与「Kill 掉了」完全无关。
    //  ⇒ 自检侧补同一刀：反射调 `TweenManager.Despawn`（它会派 `onKill`，与 Play 模式 `Kill(false)` 末句同一条路）。
    //  ⚠️ 只杀**时长 ≈ `DeploySequence.DurationFor(card)`** 的那一条 —— 快照之后新起的补间还可能包括
    //     起手那一下 `PickUp`(0.1) 与 `Relayout` 的 `_animGate`(0.2)，误杀会让后面的 `Relayout` 全被拦。
    //  ⚠️ 返回 0 是**合法**的：Play 模式下产品那句 `Kill` 已经生效、没东西可补。
    // ══════════════════════════════════════════════════════════════════════════════════════════════
    static void KillDeployInBatch(Tween[] mark, CardView card, string where)
    {
        float dur = DeploySequence.DurationFor(card);
        int n = B124_TweenTestUtil.KillNewSince(mark, dur, "B121Q4B " + where);
        if (n < 0)
            Check(false, where + " 批处理补刀失败：拿不到 `TweenManager.Despawn` ⇒ 落位序列没被杀 ⇒ 本组「没落地」类断言是空转");
        else
            Debug.Log(P + "ⓘ " + where + " 批处理补刀：撤销后仍活着、时长 ≈ " + dur.ToString("F2")
                      + " s 的落位序列真杀 " + n + " 条（0 = 产品那句 `Kill` 已生效 / 本来没起过）");
    }
}
