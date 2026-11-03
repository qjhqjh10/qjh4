// B120_W1_Assert.cs — 执行写手 `B120-W1`（手牌几何与手感参数 · A 域量纲桥）的断言
//
// ============================ 它盯的是什么 ============================
//   `B120_A` 审计（`资料/完全复刻/报告/B120_A_手牌与场上几何.md`）判定的根因族：
//   **原版手牌的世界是 14.835 px/单位，我们的手牌世界是 108 px/单位 ⇒ 凡「以原版世界单位为值」的
//   距离 / 饱和量都差 7.28×**。本文件把每一处桥接钉死，并给「老值」留反方向断言（灭自证）。
//
//   A · 量纲桥的**唯一出处**（`HandLayout.OurUnitsPerWorldUnit` = 14.835/108）
//   B · A-6 上半：轻点阈值 = uGUI 10 **真实 px**（按屏高换世界单位）
//   C · A-6 下半：出牌高度 = `minMovementToPlayCard` 0.3 **原版世界单位** = 4.45 px（**与轻点阈值是两个量**）
//   D · `PlayCommitGate` 的三态（抬够/没抬够/没越线）
//   E · A-3 跟手硬吸附：1.48 px / 469 px
//   F · A-4 拖拽倾斜分母：148.35 px 饱和
//   G · A-2 拖起透视缩小：净 1.0137×（正交相机下显式补乘）
//   H · D118-65 拖拽目标偏移：`moveCardYOffset` 0.2 原版世界单位 = 2.97 px
//   I · A-7 `AutoCardRotation` 位移→角度的桥
//   J · A-5 **行为**：拖起来的牌世界 Z 被抹平（原版写世界 `Euler(rx,ry,0)`）
//
// 判据（⛔ 别在这里抄第二份）：
//   · `d:/2/tools/decomp_full/CardScript__{SetCardMovement,StartMovingFromHand}.c`
//   · `d:/2/tools/decomp_full/BattleManager__Update.c:250-253`（`case 2` 的「抬够才 commit」）
//   · `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs` 的 `case 2`（`moveCardYOffset` 那一句）
//   · `d:/2/tools/il2cpp_out/dump.cs:120061,120104`（`GlobalVariables` 字段偏移）
//   ⚠️ **本文件的期望值全部手写成原版字面量**（⛔ 不从被测实现里读期望值 —— 那就是自证）。
//
// ============================ 怎么用（宿主）============================
//   宿主里一行：`B120_W1_Assert.Run();`（**本文件未接线**，由调度台挂进宿主聚合行）
//   + 需要计数时：`_sink.Pass += B120_W1_Assert.Pass; _sink.Fail += B120_W1_Assert.Fail;`
//   · 推荐宿主 = `Editor/BattleScene.cs`（`BattleScene.Run`）。**零前提**：自带临时根 + `DestroyImmediate` 收尾。
//
// 判绿红：`grep "^B120_W1"` 的**合计行**；逐条按**行首** `B120_W1 ✓` / `B120_W1 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;

/// <summary>执行写手 `B120-W1`：手牌层量纲桥（A 域）+ 拖拽目标偏移 + 出牌高度闸。</summary>
public static class B120_W1_Assert
{
    static int _pass, _fail;
    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    // 🔴 原版侧的字面量（判据见文件头）—— 单独写一份，⛔ 不从 `HandLayout` 里读。
    const float OrigPxPerUnit = 14.835f;      // 原版 HUD 画布：相机 fov40 + planeDistance 100
    const float OurPxPerUnit = 108f;          // 我们：`LayoutSpace.DesignPxH 1080 / DesignHeight 10`
    const float Bridge = OrigPxPerUnit / OurPxPerUnit;   // 0.13736111…
    const float UGuiDragThresholdPx = 5f;     // 🔴 2026-11-03（`B130`）：原版 `EventSystem.m_DragThreshold` 的**序列化值 5**（判据 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity:5429`；uGUI 字段初值才是 10，原来写「原版资产里 0 命中 ⇒ 取初值 10」是只搜了 `assets_full` 的 bundle 树）
    const float MinMovementToPlayCard = 0.3f; // `VarsGlobal.minMovementToPlayCard`（+0x7C，`VarsGlobal_原版数值.md:44`）
    const float MoveCardYOffset = 0.2f;       // `VarsDevice.moveCardYOffset`（`VarsDesktop`）

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B120_W1 ✓ " + msg); }
        else { _fail++; Debug.LogError("B120_W1 ✗ " + msg); }
    }

    static bool Near(float a, float b, float tol) { return Mathf.Abs(a - b) <= tol; }

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

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepMode = CardTween.Mode;
        bool keepLink = CardTween.LinkEnabled;
        try
        {
            CardTween.Mode = DG.Tweening.UpdateType.Manual;   // 批处理没有帧循环 ⇒ 补间不许自己跑
            CardTween.LinkEnabled = false;

            Group("A·量纲桥", A_Bridge);
            Group("B·轻点阈值", B_Tap);
            Group("C·出牌高度", C_PlayLift);
            Group("D·PlayCommitGate", D_Gate);
            Group("E·硬吸附阈值", E_Snap);
            Group("F·倾斜分母", F_Tilt);
            Group("G·起拖缩放", G_PickUpScale);
            Group("H·拖拽目标偏移", H_DragOffset);
            Group("I·AutoCardRotation 桥", I_AutoRot);
            Group("J·行为·拖起后 Z 抹平", J_ZFlattened);
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
        Debug.Log("B120_W1 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ==================================================================
    //  A · 量纲桥 —— 两边的 px/单位各自从**自己的来源**算，桥 = 两者之比
    // ==================================================================
    static void A_Bridge()
    {
        Check(Near(LayoutSpace.DesignPxH / LayoutSpace.DesignHeight, OurPxPerUnit, 1e-4f),
              "A1 我们的世界 1 单位 = 108 px（`DesignPxH 1080 / DesignHeight 10`，实得 "
            + (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight).ToString("F4") + "）"
            + "｜判据 `Core/LayoutSpace.cs:36/79/208`");

        Check(Near(HandLayout.OurUnitsPerWorldUnit, Bridge, 1e-6f),
              "A2 `HandLayout.OurUnitsPerWorldUnit` == 14.835/108 = **0.13736**（实得 "
            + HandLayout.OurUnitsPerWorldUnit.ToString("F8") + "）"
            + "｜🧨 把它改成 1（= 自以为「1:1 可搬」）⇒ 红");

        Check(Near(HandLayout.OurUnitsPerWorldUnit * OurPxPerUnit, OrigPxPerUnit, 1e-3f),
              "A3 桥 × 108 == 14.835（= 原版 1 世界单位对应的 px，实得 "
            + (HandLayout.OurUnitsPerWorldUnit * OurPxPerUnit).ToString("F4") + "）");
    }

    // ==================================================================
    //  B · 轻点阈值（A-6 上半）：uGUI 的**真实屏幕 px**，按当前屏高换成世界单位
    // ==================================================================
    static void B_Tap()
    {
        Check(Near(CardInteraction.TapThresholdPx, UGuiDragThresholdPx, 1e-6f),
              "B1 `TapThresholdPx` == 原版 **5 px**（实得 " + CardInteraction.TapThresholdPx.ToString("F3") + "）"
            + "｜判据：`PointerInputModule.ShouldStartDrag` 用 `EventSystem.pixelDragThreshold`，原版序列化 `m_DragThreshold: 5`（`Intro.unity:5429`）；"
            + "🧨 改回 10（uGUI 字段初值）⇒ 红");

        Check(Near(CardInteraction.TapThresholdWorldAt(1080), UGuiDragThresholdPx / OurPxPerUnit, 1e-6f),
              "B2 1080p 下轻点阈值 = 5/108 = **0.0463** 世界单位（实得 "
            + CardInteraction.TapThresholdWorldAt(1080).ToString("F6") + "）");

        Check(Near(CardInteraction.TapThresholdWorldAt(2160), UGuiDragThresholdPx / (OurPxPerUnit * 2f), 1e-6f),
              "B3 4K（2160）下 = 5/216 = **0.02315**（实得 " + CardInteraction.TapThresholdWorldAt(2160).ToString("F6") + "）"
            + " —— 证明阈值是**真实屏幕 px**、⛔ 不是写死 108 的常量");

        Check(CardInteraction.TapThresholdWorldAt(1080) < 0.3f - 0.15f,
              "B4 灭自证：**不再是**旧值 0.3（我们的单位 = 32.4 px）；实得 "
            + CardInteraction.TapThresholdWorldAt(1080).ToString("F6") + " 世界单位 = "
            + (CardInteraction.TapThresholdWorldAt(1080) * OurPxPerUnit).ToString("F2") + " px"
            + "｜🧨 把 `TapThresholdPx` 改回 32.4 ⇒ 红");
    }

    // ==================================================================
    //  C · 出牌高度（A-6 下半）——「拆成两个量」的第二半
    // ==================================================================
    static void C_PlayLift()
    {
        Check(Near(CardInteraction.PlayLiftWorld, MinMovementToPlayCard * Bridge, 1e-6f),
              "C1 `PlayLiftWorld` == 0.3 × 0.13736 = **0.04121** 我们的单位（实得 "
            + CardInteraction.PlayLiftWorld.ToString("F6") + "）"
            + "｜判据 `BattleManager__Update.c:253`（`pointerCursorStart.y + globalVars+0x7c < mouse.y`）");

        Check(Near(CardInteraction.PlayLiftWorld * OurPxPerUnit, 4.4505f, 0.02f),
              "C2 换算成 px = **4.45 px**（实得 "
            + (CardInteraction.PlayLiftWorld * OurPxPerUnit).ToString("F3") + " px）"
            + "｜🧨 把 0.3 直接当我们的单位用（= 32.4 px）⇒ 红");

        Check(Mathf.Abs(CardInteraction.PlayLiftWorld - CardInteraction.TapThresholdWorldAt(1080)) > 1e-4f,
              "C3 ★「出牌高度」与「轻点阈值」是**两个量**（" + (CardInteraction.PlayLiftWorld * OurPxPerUnit).ToString("F2")
            + " px vs " + (CardInteraction.TapThresholdWorldAt(1080) * OurPxPerUnit).ToString("F2") + " px）"
            + " —— 旧实现把两件事共用一个 0.3；🧨 把两者写成同一个常量 ⇒ 红"
            + "｜🔴 2026-11-03（`B130`）：阈值订正成原版 5 px 后两者**数值上很接近**（4.45 vs 5 px，容差只能收到 1e-4），"
            + "但来源不同（`0.3 × Bridge` vs `5 ÷ 屏高`）—— 判「是不是同一个量」看 C1/B2 的来源式，⛔ 别拿这条的差值当分离度");
    }

    // ==================================================================
    //  D · `PlayCommitGate` 三态（原版：越线 **且** 比按下点抬高 0.3 世界单位）
    // ==================================================================
    static void D_Gate()
    {
        // 归一化 y ≥ HandLimitY(0.2778) ⇔ 世界 y ≥ −2.2222（可见高 10、中心 0）
        var pressLow = new Vector3(0f, -2.30f, 0f);
        Check(CardInteraction.PlayCommitGate(pressLow, new Vector3(0f, -2.20f, 0f)),
              "D1 抬够（0.10 单位 = 10.8 px ≥ 4.45）**且**越线 ⇒ true");

        // D2 要「越线成立、只有抬高不成立」⇒ 按下点更靠近指针（lift 0.01 < 0.0412），
        //    而不是把指针放到线下（那样 false 的原因就不是抬高了 —— 断言会假强）
        Check(!CardInteraction.PlayCommitGate(new Vector3(0f, -2.21f, 0f), new Vector3(0f, -2.20f, 0f)),
              "D2 越线但只抬高 0.01 单位（= **1.08 px** < 4.45）⇒ **false**"
            + "｜🧨 把这个闸删掉（旧实现没有它）⇒ 这条红");

        var pressHigh = new Vector3(0f, -5.00f, 0f);
        Check(!CardInteraction.PlayCommitGate(pressHigh, new Vector3(0f, -4.90f, 0f)),
              "D3 抬够了但**没越**手牌限位线（归一化 0.0100 < 0.2778）⇒ false"
            + "｜判据 `.c:250` 的第一道 `line.y < mouse.y`（`HandLimitArea`）");
    }

    // ==================================================================
    //  E · 跟手硬吸附（A-3）：原版 0.01 / 1000 **世界单位²** ⇒ 1.48 px / 469 px
    // ==================================================================
    static void E_Snap()
    {
        float nearPx = Mathf.Sqrt(CardInteraction.HandSnapNearD2) * OurPxPerUnit;
        Check(Near(nearPx, 1.4835f, 0.01f),
              "E1 近阈 = **1.48 px**（实得 " + nearPx.ToString("F3") + " px）"
            + "｜判据 `CardScript__SetCardMovement.c:41`（`d2 < 0.01`，`d2` 是世界坐标平方和）");

        float farPx = Mathf.Sqrt(CardInteraction.HandSnapFarD2) * OurPxPerUnit;
        Check(Near(farPx, 469.15f, 0.5f),
              "E2 远阈 = **469 px**（实得 " + farPx.ToString("F3") + " px）｜判据 同 `.c`（`d2 > 1000`）");

        Check(CardInteraction.HandSnapFarD2 < 1000f && CardInteraction.HandSnapNearD2 < 0.01f,
              "E3 灭自证：两个阈值都**小于**原版字面量（因为桥² < 1：0.01→"
            + CardInteraction.HandSnapNearD2.ToString("E3") + " / 1000→"
            + CardInteraction.HandSnapFarD2.ToString("F3") + "）"
            + "｜🧨 把阈值改回裸 `0.01f / 1000f`（= 10.8 px / 3413 px，旧实现）⇒ 红");
    }

    // ==================================================================
    //  F · 拖拽倾斜分母（A-4）：原版 10 世界单位 = 148.35 px 即饱和
    // ==================================================================
    static void F_Tilt()
    {
        float px = CardInteraction.DragTiltReductionForTest * OurPxPerUnit;
        Check(Near(px, 148.35f, 0.1f),
              "F1 分母 = 10 × 14.835 = **148.35 px**（实得 " + px.ToString("F2") + " px）"
            + "｜判据 `CardScript__SetCardMovement.c:76-96` 的 `movingRotationReduction 10`");

        Check(Mathf.Abs(CardInteraction.DragTiltReductionForTest - 10f) > 1f,
              "F2 灭自证：分母**不再是 10**（我们的单位 = 1080 px 才饱和 ⇒ 正常拖速只倾 3–7°）"
            + "｜🧨 改回 `10f` ⇒ 红");
    }

    // ==================================================================
    //  G · 起拖缩放（A-2）：原版 z 抬 10 世界单位 × 透视 = 0.9215×，与 1.1 相乘 = 净 1.0137×
    // ==================================================================
    static void G_PickUpScale()
    {
        Check(Near(CardInteraction.PickUpScaleFactorFor(1.1f, true), 1.1f * 0.9215f, 1e-4f),
              "G1 正交相机下起拖系数 = 1.1 × 0.9215 = **1.01365**（实得 "
            + CardInteraction.PickUpScaleFactorFor(1.1f, true).ToString("F5") + "）"
            + "｜判据 `CardScript__SetCardMovement.c:30-33`（`z = planeDistance − 10`，`read_literal 0x1834b2da8 = 10`）");

        Check(Near(CardInteraction.PickUpScaleFactorFor(1.1f, false), 1.1f, 1e-6f),
              "G2 透视相机下**不补乘**（= 1.1，透视自己会产生缩小）⇒ 将来 D 域换回透视模型（`D118-85`）不会双乘");

        Check(Mathf.Abs(CardInteraction.PickUpScaleFactorFor(1.1f, true) - 1.1f) > 0.08f,
              "G3 灭自证：净缩放**明显小于** 1.1（旧值 ⇒ 拖起牌大 8.5%）｜🧨 去掉 0.9215 ⇒ 红");
    }

    // ==================================================================
    //  H · 拖拽目标偏移（D118-65）：原版 target = 指针 + `moveCardYOffset`（0.2 **原版世界单位**，无抓取偏移）
    // ==================================================================
    static void H_DragOffset()
    {
        Vector3 ofs = CardInteraction.DragTargetOffsetForTest;
        Check(Near(ofs.x, 0f, 1e-6f) && Near(ofs.z, 0f, 1e-6f),
              "H1 偏移只在 y 上（实得 (" + ofs.x.ToString("F4") + "," + ofs.y.ToString("F4") + ","
            + ofs.z.ToString("F4") + ")）｜判据 `BattleManager__Update.c:312-313`");

        Check(Near(ofs.y * OurPxPerUnit, MoveCardYOffset * OrigPxPerUnit, 0.02f),
              "H2 y = 0.2 × 14.835 = **2.97 px**（实得 " + (ofs.y * OurPxPerUnit).ToString("F3") + " px）");

        Check(Mathf.Abs(ofs.y - MoveCardYOffset) > 0.1f,
              "H3 灭自证：**不再是** 0.2 我们的单位（那 = 21.6 px，7.28×）｜🧨 把 0.2 直接当我们的单位用 ⇒ 红");
    }

    // ==================================================================
    //  I · `AutoCardRotation` 的位移→角度桥（A-7）
    // ==================================================================
    static void I_AutoRot()
    {
        Check(Near(1f / AutoCardRotation.OriginalUnitsPerOurUnit, Bridge, 1e-5f),
              "I1 `OriginalUnitsPerOurUnit` 的倒数 == 桥 0.13736（实得 "
            + (1f / AutoCardRotation.OriginalUnitsPerOurUnit).ToString("F8") + "）");

        // 位移 0.13736 我们的单位（= 14.835 px = 原版 1 世界单位）⇒ 满 10°
        Quaternion qBridged = AutoCardRotation.TargetRotationFor(0.13736f * (1f / Bridge), 0f, 10f, 10f);
        float degBridged = Quaternion.Angle(qBridged, Quaternion.identity);
        Check(Near(degBridged, 10f, 0.02f),
              "I2 位移 14.835 px（过桥后 = 原版 1 单位）⇒ **满 10°**（实得 " + degBridged.ToString("F4") + "°）"
            + "｜判据 `AutoCardRotation__GetRotation.c`（`translation × maxAngle` 夹到 ±maxAngle）");

        float degRaw = Quaternion.Angle(AutoCardRotation.TargetRotationFor(0.13736f, 0f, 10f, 10f),
                                        Quaternion.identity);
        Check(Near(degRaw, 1.3736f, 0.01f) && Mathf.Abs(degBridged - degRaw) > 8f,
              "I3 灭自证：同一位移**不过桥**只有 **1.37°**（" + degRaw.ToString("F4") + "°）"
            + "｜🧨 把 `Step` 里的 `* OriginalUnitsPerOurUnit` 删掉 ⇒ 实际手感回到 1.37°");
    }

    // ==================================================================
    //  J · 行为（夹具）：拖起来的牌**世界 Z 被抹平**（A-5）
    // ==================================================================
    static void J_ZFlattened()
    {
        var root = new GameObject("B120_W1_root");
        try
        {
            var layout = root.AddComponent<HandLayout>();
            var board = root.AddComponent<BoardLayout>();
            var it = root.AddComponent<CardInteraction>();
            it.hand = layout; it.board = board;
            it.SetRightButtonHeldForTest(false);      // 批处理没有鼠标设备 ⇒ 把闸钉成「没按右键」
            it.SetBattleStateBlockedForTest(false);   // 本组只验拖拽写法 ⇒ 状态闸全放行
            it.CanDropAtSlot = (slot, card) => true;
            layout.animateRelayout = false;           // 位姿当场精确
            board.lineY = 0.30f;                      // 与手牌限位线（0.2778）分开

            Check(Near(it.dragScale, 1.1f, 1e-6f),
                  "J0（前提）`dragScale` 仍是原版 `cardInHandMovingScale` **1.1**（实得 " + it.dragScale.ToString("F3") + "）");

            var d = CardData.Placeholder(1);
            d.id = "B120_W1_CARD"; d.artId = "B120_W1_CARD";
            d.isUnit = true; d.type = "unit"; d.rarity = "common";
            var card = CardView.Create(root.transform, d, "B120_W1_CARD", CardFace.Hand);
            it.SetCards(new List<CardView> { card }, false);

            Vector3 home = card.transform.position;
            // 摆一个真实的手牌扇形角（8 张手牌 ≈ ±6°；这里取 8° 更能分辨）
            card.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            it.SimulateHover(home);
            it.SimulatePress(home);
            Check(it.IsDragging && it.Dragging == card,
                  "J1（前提）抓起来了（否则下面两条是空验；实得 "
                + (it.Dragging != null ? it.Dragging.name : "<null>") + "）");
            if (!it.IsDragging) return;

            it.SimulateDrag(home + new Vector3(1.0f, 0.5f, 0f), 1f / 60f);
            Vector3 e = card.transform.rotation.eulerAngles;

            Check(Mathf.Abs(Mathf.DeltaAngle(e.z, 0f)) < 0.5f,
                  "J2 ★拖起后**世界 Z 被抹平**（实得 Z = " + Mathf.DeltaAngle(e.z, 0f).ToString("F2") + "°，拖前 8°）"
                + "｜判据 `CardScript__StartMovingFromHand.c:78-81` 把 `rotationTarget` 写成 `Vector3.zero`，"
                + "`SetCardMovement.c:88-108` 再写世界 `Euler(rotationTarget + extra)`"
                + "｜🧨 改回 `localRotation = Euler(rx, ry, localEulerAngles.z)`（保留扇形角）⇒ 红");

            Check(Mathf.Abs(Mathf.DeltaAngle(e.y, 0f)) > 5f || Mathf.Abs(Mathf.DeltaAngle(e.x, 0f)) > 1f,
                  "J3（前提）倾摆**真的写了**（X = " + Mathf.DeltaAngle(e.x, 0f).ToString("F2")
                + "°，Y = " + Mathf.DeltaAngle(e.y, 0f).ToString("F2") + "°）—— 否则 J2 可能只是「什么都没写」");
        }
        finally
        {
            if (Application.isPlaying) Object.Destroy(root); else Object.DestroyImmediate(root);
        }
    }
}
