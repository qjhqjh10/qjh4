// B120_W16_Assert.cs — 执行写手 `B120-W16`（2026-11-02）· 原版「**还按着就 commit**」
//
// ============================ 它盯的是什么 ============================
//   原版 `BattleManager.Update()` 的 `case 2`（`UIstate == movingHandCard`）是**逐帧**跑的：
//
//       if (HandLimitArea.y < 指针.y) {                                  // ① 越过手牌限位线
//           if (pointerCursorStart.y + VarsGlobal.minMovementToPlayCard < 指针.y) {   // ② 比起点抬高 0.3 原版世界单位
//               if (CanPlayCard(...)) {
//                   CardScript.MoveIntoFieldFromHand();                     // ← **就在这一帧提交**
//                   UIstate = playingMinion / playingTargetedSpell / playingNonTargetSpell;
//                   PlayerHand.StartMovingHandCard(...);
//               } } }
//
//   ⇒ **越过高度那一刻就提交，⛔ 不等松手。** 真正的「松手」在**另一支**（`cardTracked` 已空），
//     那一支只做 `MoveBackToHand` + `HandCardMovedToNewIndex` + `HandCardReleased`，**不会再出一次牌**。
//
//   判据（亲读，⛔ 别照抄二手描述）：
//     · `d:/2/tools/decomp_full/BattleManager__Update.c:249-288`（`case 2`；`MoveIntoFieldFromHand` 在 `:260`）
//     · `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:25720-25864`（逐行可读，`MoveIntoFieldFromHand()` 在 `:25733`）
//     · `d:/2/tools/decomp_full/BattleManager__StartMovingCardInHand.c:4`（`pointerCursorStart(+0x320) = 起拖点`）
//     · 两个常量的值：`minMovementToPlayCard` = 0.3（`GlobalVariables +0x7C`）· `HandLimitArea` 的 y（`BoardLayout.HandLimitY`）
//
//   ⚠️ 本文件的**期望值全部是手写的原版字面量 / 手摆的几何**，⛔ 不从被测实现里读期望值（那是自证）。
//
// ============================ 各组在验什么 ============================
//   A · ★核心：越过高度的**那一帧**就提交（人还按着），随后松手 / 再拖都是空操作（⛔ 不重复出牌）
//   B · 灭自证①：落点合法、线也过了，但**抬高不够 0.3 原版世界单位** ⇒ 不提交
//   C · 灭自证②：落点合法、抬高够，但**指针仍在手牌限位线以下** ⇒ 不提交
//   D · 回弹 / 取消路径不受影响：没过门时松手照旧「牌回手 + OnReturned」，日志走 `(Release)` 那一档
//
// ============================ 怎么用（宿主）============================
//   宿主里一行：`B120_W16_Assert.Run();`（**本文件未接线**，由调度台挂进宿主聚合行）
//   + 需要计数时：`_sink.Pass += B120_W16_Assert.Pass; _sink.Fail += B120_W16_Assert.Fail;`
//   · 推荐宿主 = `Editor/BattleScene.cs`（`BattleScene.Run`），挂在上一件手牌断言组**之后**。
//   · 零前提：自带临时根 + `DestroyImmediate` 收尾；`CardTween` 拨 `Manual` 并在退出时**原样还原**。
//
// 判绿红：`grep "^B120_W16"` 的**合计行**；逐条按**行首** `B120_W16 ✓` / `B120_W16 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

public static class B120_W16_Assert
{
    static int _pass, _fail;
    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B120_W16 ✓ " + msg); }
        else { _fail++; Debug.LogError("B120_W16 ✗ " + msg); }
    }

    /// <summary>跑一组并把异常收成一条失败（⛔ 不让一组炸掉后面各组）。</summary>
    static void Group(string tag, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    sealed class Fixture : System.IDisposable
    {
        public GameObject root;
        public HandLayout hand;
        public BoardLayout board;
        public CardInteraction it;
        public CardView card;
        public int flights;     // `OnDeployFlightStart` 次数 = 「真的进入了打出链」
        public int landings;    // `OnDeployed` 次数（批处理里补间不自己跑 ⇒ 一般恒 0）
        public int returns;     // `OnReturned` 次数 = 「取消 / 回弹」

        public void Dispose()
        {
            if (root != null) Object.DestroyImmediate(root);
        }
    }

    /// <summary>最小夹具：手牌布局 + 一块棋盘 + 交互器。
    /// <paramref name="lineY"/> = 棋盘行心的**归一化 y**（= 落点判据里那条线）；
    /// <paramref name="cardNormY"/> = 那张手牌被**手摆**到哪个归一化 y（⛔ 不靠实现去摆 —— 否则「按下点」会被被测代码决定）。</summary>
    static Fixture Make(float lineY, float cardNormY, string name)
    {
        var f = new Fixture();
        f.root = new GameObject("B120_W16_root");
        f.hand = f.root.AddComponent<HandLayout>();
        f.board = f.root.AddComponent<BoardLayout>();
        f.it = f.root.AddComponent<CardInteraction>();
        f.it.hand = f.hand;
        f.it.board = f.board;
        f.it.SetRightButtonHeldForTest(false);      // 批处理没有鼠标设备 ⇒ 把「右键按着」那道闸钉成 false
        f.it.SetBattleStateBlockedForTest(false);   // 本文件只验提交时机 ⇒ 战斗状态闸全放行
        f.it.CanDropAtSlot = (slot, c) => true;     // 落点合法性的**真**判据在引擎；这里换成替身，验的才是「时机」
        f.hand.animateRelayout = false;             // 夹具：位姿当场精确
        f.board.lineY = lineY;
        f.it.OnDeployFlightStart += (c, b, s, d) => f.flights++;
        f.it.OnDeployed += (c, s) => f.landings++;
        f.it.OnReturned += (c, k) => f.returns++;

        var d = CardData.Placeholder(1);
        d.id = name; d.artId = name;                // ⛔ 别撞真实卡：`TacticTargetSide` 会拿 id 回查卡表
        d.isUnit = true; d.type = "unit"; d.rarity = "common";
        f.card = CardView.Create(f.root.transform, d, name, CardFace.Hand);
        f.it.SetCards(new List<CardView> { f.card }, false);
        // 🔴 2026-11-03（`B124-W22`）：**必须在 `SetCards` 之后**再手摆几何 —— `SetCards` 会经
        //   `HandLayout.Refresh` 把这张牌重排回**手牌带**（实得归一化 y 0.1216），摆在前面的位置会被冲掉
        //   ⇒ 组 B 的「抬高不够」前提变成 1.7838（实测 `battle_b124b.log:1709149`）。
        f.card.transform.position = LayoutSpace.ToWorld(0.5f, cardNormY);
        return f;
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

            Group("A·越过高度的那一帧就提交", A_CommitsWhileHolding);
            Group("B·灭自证·抬高不够", B_NotEnoughLift);
            Group("C·灭自证·没越限位线", C_NotPastLine);
            Group("D·回弹/取消路径", D_CancelStillWorks);
        }
        catch (System.Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            CardTween.Mode = keepMode;
            CardTween.LinkEnabled = keepLink;
        }
        Debug.Log("B120_W16 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ==================================================================
    //  A · ★核心 —— 越过高度那一帧就提交（**人还按着**），松手是空操作
    // ==================================================================
    static void A_CommitsWhileHolding()
    {
        var f = Make(lineY: 0.42f, cardNormY: 0.12f, name: "B120W16_A");
        try
        {
            Vector3 home = f.card.transform.position;
            Vector3 drop = f.board.SlotPosition(3);   // 🔴 2026-11-03（`B124-W22`）：**别拿督军格 4 当落点** —— `ArenaSlots.IsCandidate` 里督军位恒非候选（`Board/ArenaSlots.cs:297`），两侧皆空时候选只有 {3,5}，3/5 到 x=0.5 等距、float32 下严格 `<` 取 5 ⇒ 「解析到 4」必假
            int hand0 = f.it.HandCardCountForTest;

            // ---- 前提（手摆几何 + 原版常量，⛔ 全从夹具/字面量来，不从被测实现读「结果」）----
            Check(LayoutSpace.ToNormalized(home).y < BoardLayout.HandLimitY,
                  "（前提）按下点在手牌限位线**以下**（归一化 y "
                + LayoutSpace.ToNormalized(home).y.ToString("F4") + " < " + BoardLayout.HandLimitY.ToString("F4") + "）");
            Check(LayoutSpace.ToNormalized(drop).y >= BoardLayout.HandLimitY,
                  "（前提）落点在限位线**以上**（归一化 y "
                + LayoutSpace.ToNormalized(drop).y.ToString("F4") + " ≥ " + BoardLayout.HandLimitY.ToString("F4") + "）"
                + "｜判据 `BattleManager__Update.c:249`（`HandLimitArea.y < mouse.y`）");
            Check(drop.y - home.y >= CardInteraction.PlayLiftWorld,
                  "（前提）这一拖抬高了 " + (drop.y - home.y).ToString("F4") + " 我们的单位 ≥ "
                + CardInteraction.PlayLiftWorld.ToString("F5") + "（= `minMovementToPlayCard` 0.3 原版世界单位 = 4.45 px）"
                + "｜判据 `BattleManager__Update.c:253`（`pointerCursorStart.y + +0x7C < mouse.y`）");
            int slot0;
            bool resolves = f.board.TryResolveSlot(drop, out slot0, 3, 3);   // 🆕 2026-11-03（`B121-WB` · `Q04c3`）：已删自定容差；显式给两侧各 3 已占 ⇒ 8 格全候选，槽 3（贴督军的内圈格）可解析
            Check(resolves && slot0 == 3,
                  "（前提）落点解析到第 3 格（实得 resolves=" + resolves + "、槽 " + slot0 + "）");
            Check(f.it.HandCardCountForTest == hand0 && hand0 == 1,
                  "（前提）开局手牌 1 张（实得 " + hand0 + "）");

            f.it.SimulateHover(home);
            f.it.SimulatePress(home);
            Check(f.it.Dragging == f.card,
                  "（前提）抓起来了（以为下面「一帧就提交」是空验）");

            // ---- ★只喂**一帧**、且**一次都没叫 Release** ----
            f.it.SimulateDrag(drop, 1f / 60f);

            Check(f.it.Dragging == null,
                  "★A1 越过高度那一帧**就已经提交**（`Dragging` 已空 —— 玩家手还按着、没松手）"
                + "｜判据 `BattleManager__Update.c:253-260`：`CanPlayCard` 一过就 `MoveIntoFieldFromHand()`，"
                + "全段**没有**任何「松手」前置｜🧨 改回「松手才提交」⇒ 这里仍是那张卡 ⇒ 红");
            Check(f.flights == 1,
                  "★A2 提交恰好 1 次（`OnDeployFlightStart` 实得 " + f.flights + "）"
                + " —— 驱动层就是靠这一下把「起飞」纳入 `WaitingForBlockingAction` 的");
            Check(f.it.HandCardCountForTest == hand0 - 1,
                  "★A3 这张牌已经**离开手牌**（" + hand0 + " → " + f.it.HandCardCountForTest + "）"
                + "｜判据 `MoveIntoFieldFromHand` ⇒ 手牌列表里不该再有它");
            Check(f.it.DeployingViewForTest == f.card,
                  "★A4 打出链闸已上膛（`DeployingViewForTest` == 这一张）"
                + " —— 否则飞行那 0.65 s 里一次 `RefreshHandPlayable` 会把蓝光刷回绿");
            Check((f.it.LastReleaseLogForTest ?? "").Contains("还按着就提交"),
                  "★A5 日志写明这一下是「还按着就提交」，⛔ 不是冒充松手｜实得：`"
                + Short(f.it.LastReleaseLogForTest, 110) + "`");

            // ---- 真松手 + 再拖一帧 ⇒ 都必须是空操作（结构上不可能重复出牌）----
            f.it.SimulateDrag(drop, 1f / 60f);
            f.it.SimulateRelease(drop);
            Check(f.flights == 1 && f.it.HandCardCountForTest == hand0 - 1,
                  "★A6 提交之后的「再拖一帧」与「真松手」都是**空操作**（起飞次数仍 " + f.flights
                + "、手牌仍 " + f.it.HandCardCountForTest + "）—— `Release` 第一句就把 `_dragging` 置空，"
                + "而 `SimulateDrag` / `SimulateRelease` 都带 `_dragging != null` 前置"
                + "｜🧨 把「提交」写成不清拖拽态 ⇒ 松手再出一次牌 ⇒ 红");
            Check((f.it.LastReleaseLogForTest ?? "").Contains("还按着就提交"),
                  "★A7 松手**没有**覆盖掉 A5 那一行日志（那一行仍是提交那次写的）"
                + " —— 反证「松手真的没走结算路径」（否则日志会换成 `(Release)`）｜实得：`"
                + Short(f.it.LastReleaseLogForTest, 110) + "`");
        }
        finally { f.Dispose(); }
    }

    // ==================================================================
    //  B · 灭自证① —— 落点合法、线也过了，但**抬高不够** ⇒ 不提交
    // ==================================================================
    static void B_NotEnoughLift()
    {
        // 手牌摆在 0.2962、棋盘线 0.30 ⇒ 抬高 = 0.0038 归一化 = **0.038 我们的单位 < 0.04121**（0.3 原版世界单位）
        var f = Make(lineY: 0.30f, cardNormY: 0.2962f, name: "B120W16_B");
        try
        {
            Vector3 home = f.card.transform.position;
            Vector3 drop = f.board.SlotPosition(3);   // 🔴 2026-11-03（`B124-W22`）：**别拿督军格 4 当落点** —— `ArenaSlots.IsCandidate` 里督军位恒非候选（`Board/ArenaSlots.cs:297`），两侧皆空时候选只有 {3,5}，3/5 到 x=0.5 等距、float32 下严格 `<` 取 5 ⇒ 「解析到 4」必假
            int hand0 = f.it.HandCardCountForTest;

            Check(LayoutSpace.ToNormalized(drop).y >= BoardLayout.HandLimitY,
                  "（前提）线那一条**已经满足**（落点归一化 y "
                + LayoutSpace.ToNormalized(drop).y.ToString("F4") + " ≥ " + BoardLayout.HandLimitY.ToString("F4")
                + "）—— 所以本组真的是在验「抬高」这一条");
            Check(drop.y - home.y < CardInteraction.PlayLiftWorld,
                  "（前提）抬高 " + (drop.y - home.y).ToString("F5") + " < "
                + CardInteraction.PlayLiftWorld.ToString("F5") + "（差这一点点）");
            int s;
            Check(f.board.TryResolveSlot(drop, out s, 3, 3) && s == 3,
                  "（前提）落点照样解析到第 3 格（实得 " + s + "）—— 落点这一条**不是**本组失败的原因");

            f.it.SimulateHover(home);
            f.it.SimulatePress(home);
            Check(f.it.Dragging == f.card, "（前提）抓起来了");

            f.it.SimulateDrag(drop, 1f / 60f);
            Check(f.it.Dragging == f.card,
                  "★B1 抬高**没到 0.3 原版世界单位** ⇒ **不提交**（拖拽态还在）"
                + "｜判据 `BattleManager__Update.c:253`（`pointerCursorStart.y + 0x7C < mouse.y`）"
                + "｜🧨 把第二条判据删掉 / 改成恒真 ⇒ 这一帧就提交 ⇒ 红");
            Check(f.flights == 0 && f.it.HandCardCountForTest == hand0,
                  "★B2 …也就没有起飞、没有离开手牌（起飞 " + f.flights + "、手牌 " + f.it.HandCardCountForTest + "）");
        }
        finally { f.Dispose(); }
    }

    // ==================================================================
    //  C · 灭自证② —— 落点合法、抬高够，但**指针仍在手牌限位线以下** ⇒ 不提交
    // ==================================================================
    static void C_NotPastLine()
    {
        // 棋盘线 0.20（**低于** HandLimitY 0.2778）⇒ 落点合格、抬高了 0.8 我们的单位（远超 0.04121），
        // 但「越线」这一条不成立。这正是原版那两道判据里**第一条**的判别式。
        var f = Make(lineY: 0.20f, cardNormY: 0.12f, name: "B120W16_C");
        try
        {
            Vector3 home = f.card.transform.position;
            Vector3 drop = f.board.SlotPosition(3);   // 🔴 2026-11-03（`B124-W22`）：**别拿督军格 4 当落点** —— `ArenaSlots.IsCandidate` 里督军位恒非候选（`Board/ArenaSlots.cs:297`），两侧皆空时候选只有 {3,5}，3/5 到 x=0.5 等距、float32 下严格 `<` 取 5 ⇒ 「解析到 4」必假
            int hand0 = f.it.HandCardCountForTest;

            Check(LayoutSpace.ToNormalized(drop).y < BoardLayout.HandLimitY,
                  "（前提）落点仍在**手牌限位线以下**（归一化 y "
                + LayoutSpace.ToNormalized(drop).y.ToString("F4") + " < " + BoardLayout.HandLimitY.ToString("F4") + "）");
            Check(drop.y - home.y >= CardInteraction.PlayLiftWorld,
                  "（前提）抬高 " + (drop.y - home.y).ToString("F4") + " ≥ "
                + CardInteraction.PlayLiftWorld.ToString("F5") + "（抬高这一条**已经满足**）");
            int s;
            Check(f.board.TryResolveSlot(drop, out s, 3, 3) && s == 3,
                  "（前提）落点照样解析到第 3 格（实得 " + s + "）");

            f.it.SimulateHover(home);
            f.it.SimulatePress(home);
            Check(f.it.Dragging == f.card, "（前提）抓起来了");

            f.it.SimulateDrag(drop, 1f / 60f);
            Check(f.it.Dragging == f.card,
                  "★C1 指针**没越手牌限位线** ⇒ **不提交**（拖拽态还在）"
                + "｜判据 `BattleManager__Update.c:249`（`HandLimitArea.y < mouse.y`）"
                + "｜🧨 把第一条判据删掉 / 改成恒真 ⇒ 这一帧就提交 ⇒ 红");
            Check(f.flights == 0 && f.it.HandCardCountForTest == hand0,
                  "★C2 …也就没有起飞、没有离开手牌（起飞 " + f.flights + "、手牌 " + f.it.HandCardCountForTest + "）");
        }
        finally { f.Dispose(); }
    }

    // ==================================================================
    //  D · 回弹 / 取消路径（本件**不许**破坏）：没过门 ⇒ 松手照旧把牌收回手牌
    // ==================================================================
    static void D_CancelStillWorks()
    {
        var f = Make(lineY: 0.42f, cardNormY: 0.12f, name: "B120W16_D");
        try
        {
            Vector3 home = f.card.transform.position;
            Vector3 inHand = home + new Vector3(0.60f, 0.05f, 0f);   // 仍留在手牌带（远低于限位线）
            int hand0 = f.it.HandCardCountForTest;

            f.it.SimulateHover(home);
            f.it.SimulatePress(home);
            Check(f.it.Dragging == f.card, "（前提）抓起来了");

            f.it.SimulateDrag(inHand, 1f / 60f);
            Check(f.it.Dragging == f.card && f.flights == 0,
                  "（前提）拖在手牌带里 ⇒ 不提交（拖拽态在、起飞 0）");

            f.it.SimulateRelease(inHand);
            Check(f.it.Dragging == null && f.it.HandCardCountForTest == hand0,
                  "★D1 没过门时松手 ⇒ **牌回手**、张数不变（" + hand0 + "）—— 「回弹 / 取消」这条路一字没动");
            Check(f.returns == 1 && f.flights == 0,
                  "★D2 走的是**取消**那一支（`OnReturned` 实得 " + f.returns + "、起飞 " + f.flights + "）");
            Check((f.it.LastReleaseLogForTest ?? "").Contains("来源=手牌层(Release)"),
                  "★D3 日志走 `(Release)` 那一档（⛔ 不是「还按着就提交」）—— 两条路径在日志上也分得开"
                + "｜实得：`" + Short(f.it.LastReleaseLogForTest, 110) + "`");
        }
        finally { f.Dispose(); }
    }

    static string Short(string s, int n)
    {
        if (string.IsNullOrEmpty(s)) return "<null>";
        return s.Length <= n ? s : s.Substring(0, n) + "…";
    }
}
