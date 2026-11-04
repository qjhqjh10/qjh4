// B141_W24_Assert.cs — 执行写手 `B141-W24` 的断言宿主（2026-11-04）
//
// 覆盖 W129 波次 `4-21 手牌悬停 z 提前量`（一条**参数口径**缺陷，不是功能缺失）：
//   A. 常量本身 = 原版换算值 —— 原版 `CardScript__ShowCardInHand.c:104`
//      `local_40 = get_position().z − DAT_1834b2dbc`；该字面量 = `.rdata 0x1834B2DBC`，
//      `工具/read_literal.py` 4 字节实读 = **0.01f**（8 字节读出来是 5.37e-17 ⇒ 不是 double）。
//      它是**原版世界单位**（原版 UI 相机 fov40 / 平面 100 ⇒ 14.835 px/单位），
//      我们是 108 px/单位（`LayoutSpace` 可见高 10 单位 = 1080 px）⇒ 等价量 = 0.01 × 14.835 / 108。
//      ⚠️ 审计表写的「大 6.9 倍」是**直接拿 0.5 比 0.01**、把两个坐标帧混在一起；
//      按同一坐标帧算，改前 0.5 我们的单位 = 54 px，原版 0.14835 px ⇒ 实际大 **364 倍**。
//   B. 这个常量真的被 `HandLayout.Refresh` 施加到**悬停那张**的 z 上，且**不动其余牌**的 z。
//   C. `CardInteraction.frontZ`（同一条判据的第二处常量）**同步**，且同样必须 NonSerialized。
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B141_W24_Assert.Run -logFile -
//
// 判绿红：看 `B141W24 合计` 那一行；逐条按**行首** `B141W24 ✓` / `B141W24 ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 宿主状态要求（给调度台挂进 `Editor/BattleScene.cs` 时看）：`Run()` **零前提** ——
//   不读 `BattleDriver` / 场景 / 存档；自带临时根 + 收尾 `DestroyImmediate`；
//   自己把 `CardTween.Mode` 拨成 `Manual`、`LinkEnabled` 拨成 `false`，退出**原样还原**（批处理没有帧循环）。
using System;
using System.Collections.Generic;
using UnityEngine;
using CardPresentation;
using RuleEngine;

public static class B141_W24_Assert
{
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    // ── 判据（**独立于被测字段**重算，⛔ 不读 `HandLayout.frontZ`）──
    const float OldFrontZ = 0.5f;              // 改前值（灭自证用）
    const float OriginalZWorld = 0.01f;        // `.rdata 0x1834B2DBC` 实读（4B float）
    const float PxPerOriginalUnit = 14.835f;   // 原版 1 世界单位 = 14.835 px
    const float PxPerOurUnit = 108f;           // 我们 1 世界单位 = 108 px
    static readonly float WantFrontZ = OriginalZWorld * PxPerOriginalUnit / PxPerOurUnit;   // = 0.00137361

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B141W24 ✓ " + msg); }
        else { _fail++; Debug.LogError("B141W24 ✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F7") + " ± " + tol.ToString("F7")
                  + "；实得 " + got.ToString("F7") + "）");
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var keepMode = CardTween.Mode;
        bool keepLink = CardTween.LinkEnabled;
        try
        {
            // 批处理没有帧循环 ⇒ 只有 `CardTween.Advance(dt)` 推得动补间（同 `B117_G10_06_Assert`）。
            CardTween.Mode = DG.Tweening.UpdateType.Manual;
            CardTween.LinkEnabled = false;

            Group("A·常量 = 原版换算值", A_Constant);
            Group("B·施加到悬停那张的 z", B_Applied);
            Group("C·CardInteraction 同步", C_InteractionCopies);
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
        Debug.Log("B141W24 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    static void Group(string tag, Action body)
    {
        try { body(); }
        catch (Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
    }

    // ==================================================================
    //  A · `HandLayout.frontZ` = 原版 0.01 原版世界单位换算到我们的单位
    // ==================================================================
    //  判据链（全部现读）：
    //   ① `d:/2/tools/decomp_full/CardScript__ShowCardInHand.c:104`
    //      `local_40 = *(float *)(lVar7 + 8) - DAT_1834b2dbc;`（`get_position()` 的 +8 = z）
    //      ⇒ 原版那句是 `set_position(x, y, z − 0.01)`，**减在 z 上**。
    //   ② `DAT_1834b2dbc` = `0x1834B2DBC`，`read_literal.py` 4B = **0.01f**（✅ 像字面量）。
    //   ③ 它是**原版世界单位**：同址常量在 `CardScript__SetCardMovement.c` 里当 `d2 < 0.01` 的近阈，
    //      而 `B120-W1` 已核过那处的 `d2` 是**世界坐标平方和**、必须乘桥²（`HandSnapNearD2`）
    //      ⇒ 同一座桥：`OurUnitsPerWorldUnit = 14.835/108 = 0.13736`。
    //  🔑 灭自证三条（结构上不可能同时满足）：
    //      A2 改回旧值 0.5 ⇒ 红；A3 漏过桥直接写 0.01 ⇒ 红；A4 物理量 0.148 px ⇒ 0.5 的 54 px 必红。
    static void A_Constant()
    {
        var root = new GameObject("B141W24_A");
        try
        {
            var layout = root.AddComponent<HandLayout>();
            Check(layout != null, "A0（前提）拿得到 HandLayout（否则下面全是空转）");
            if (layout == null) return;

            // 前提：桥本身没被改错（14.835/108）—— 桥错了 A1 的期望值就跟着错，先钉住它。
            CheckClose(HandLayout.OurUnitsPerWorldUnit, PxPerOriginalUnit / PxPerOurUnit, 1e-7f,
                       "A0b（前提）量纲桥 `OurUnitsPerWorldUnit` = 14.835/108");

            CheckClose(layout.frontZ, WantFrontZ, 1e-7f,
                       "A1 ★ 悬停提前量 = 原版 0.01 原版世界单位换算到我们的单位（0.01 × 14.835/108 ≈ 0.0013736）"
                     + "｜判据 `CardScript__ShowCardInHand.c:104` + `.rdata 0x1834B2DBC`");

            Check(Mathf.Abs(layout.frontZ - OldFrontZ) > 0.01f,
                  "A2 ★ 灭自证：⛔ 不是改前的 0.5｜🧨 把 `frontZ` 写回 0.5 ⇒ A1/A2/A4 同时红");

            Check(Mathf.Abs(layout.frontZ - OriginalZWorld) > 1e-4f,
                  "A3 ★ 灭自证：⛔ 也不是漏过桥的原样照抄 0.01（原版 1 单位 = 14.835 px、我们 1 单位 = 108 px，差 7.28 倍）");

            float px = layout.frontZ * PxPerOurUnit;
            Check(px > 0.14f && px < 0.16f,
                  "A4 ★ 物理量 " + px.ToString("F4") + " px ≈ 原版 0.01 × 14.835 = 0.14835 px"
                + "｜🧨 改回 0.5 ⇒ 54 px，必红");

            // 🔴 不静默：必须是 NonSerialized。否则场景里序列化的旧值 `frontZ: 0.5` 会**盖掉**初始化器
            //   ⇒ 改小了也真机不生效（同一个坑 `hoverLift` 2026-11-02 踩过，见那个字段的注释）。
            var f = typeof(HandLayout).GetField("frontZ");
            Check(f != null && f.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length > 0,
                  "A5 ★ `HandLayout.frontZ` 必须 `[System.NonSerialized]` —— "
                + "`Scenes/Battle.unity:1307/2544` 与 `CardBase.unity:22445` 序列化着旧 0.5"
                + "｜🧨 删掉属性 ⇒ 真机仍是 0.5（静默），本组红");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // ==================================================================
    //  B · 这个值真的作用在「悬停那张」的 z 上，且不动别的牌
    // ==================================================================
    //  落点 = `HandLayout.Refresh` 的 `float z = i * zOrderStep - (i == hoveredIndex ? frontZ : 0f);`
    //  （`HandLayout.cs` 的层序那一段）。⚠️ 层序（谁压谁）由 `queueTop` 那套 render queue 决定，
    //  **不是** z（见那段注释）⇒ 本断言只量几何纵深，不量遮挡。
    static void B_Applied()
    {
        var root = new GameObject("B141W24_B");
        var made = new List<CardView>();
        try
        {
            var layout = root.AddComponent<HandLayout>();
            Check(layout != null, "B0（前提）拿得到 HandLayout");
            if (layout == null) return;

            layout.animateRelayout = false;   // 批处理无帧循环 ⇒ 关补间，位姿当场精确
            for (int i = 0; i < 5; i++)
                made.Add(CardView.Create(root.transform, CardData.Placeholder(i + 1), "B141W24_B" + i, CardFace.Hand));
            Check(made.Count == 5 && made[0] != null && made[4] != null,
                  "B0b（前提）5 张卡建出来了（为 0 时下面的比较没有意义）");
            if (made.Count != 5 || made[0] == null || made[4] == null) return;

            // ① 没人悬停的那一趟 = 基准
            layout.Refresh(made, -1, -1);
            var zRest = new float[made.Count];
            for (int i = 0; i < made.Count; i++) zRest[i] = made[i].transform.localPosition.z;

            // ② 悬停第 2 张
            layout.Refresh(made, 2, -1);
            float zHover = made[2].transform.localPosition.z;
            float delta = zRest[2] - zHover;                  // 前移量（正值）
            float deltaPx = delta * PxPerOurUnit;

            CheckClose(delta, WantFrontZ, 1e-5f,
                       "B1 ★ 悬停那张的 z 前移量 == 原版换算值（期望值由字面量独立重算，⛔ 不读 `frontZ`）"
                     + "｜🧨 删掉 `- frontZ` 那句 / 把 frontZ 改回 0.5 ⇒ 红");

            Check(zHover < zRest[2],
                  "B2 方向：z **变小**（相机在 −Z 看 +Z ⇒ 越小越靠前）—— 原版是 `z − 0.01`");

            Check(Mathf.Abs(deltaPx - 0.14835f) < 0.01f,
                  "B3 物理量：前移 " + deltaPx.ToString("F4") + " px（原版 0.01 × 14.835 = 0.14835 px）");

            bool othersSame = true;
            string firstBad = "";
            for (int i = 0; i < made.Count; i++)
            {
                if (i == 2) continue;
                if (Mathf.Abs(made[i].transform.localPosition.z - zRest[i]) > 1e-5f)
                {
                    othersSame = false;
                    if (firstBad.Length == 0)
                        firstBad = "#" + i + " 改前 " + zRest[i].ToString("F6")
                                 + " → 改后 " + made[i].transform.localPosition.z.ToString("F6");
                }
            }
            Check(othersSame, "B4 ★ 悬停**不改其它卡位的 z**（逐张与无悬停那一趟比）" + (othersSame ? "" : "｜首差 " + firstBad));

            // 🔑 灭自证③：把同一个字段临时改回旧值 0.5，**输出必须跟着变** ——
            //   证明 z 真的由 `frontZ` 驱动，而不是公式里另写死了一个常量。
            layout.frontZ = OldFrontZ;
            layout.Refresh(made, 2, -1);
            float zHoverOld = made[2].transform.localPosition.z;
            layout.frontZ = WantFrontZ;                       // 还原（后面不再用这台布局）
            Check(Mathf.Abs(zHover - zHoverOld) > 0.4f,
                  "B5 ★ 灭自证：`frontZ` 改回 0.5 后悬停 z 必须跟着变（差值 "
                + (zHover - zHoverOld).ToString("F4") + "，应约 −0.4986）"
                + "｜🧨 公式里硬编码 −0.0013736（不用字段）⇒ 红");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // ==================================================================
    //  C · `CardInteraction.frontZ` 与 `HandLayout.frontZ` 同一条判据、同一个值
    // ==================================================================
    //  ⚠️ 现读 `CardInteraction.frontZ` **零读取方**（全仓 `.frontZ` 只命中 `HandLayout` 与两处编辑器断言）
    //    —— 悬停 z 实际由 `HandLayout.Refresh` 施加。保留本字段只为「两处常量同步」的口径一致，
    //    ⛔ 别在 `CardInteraction` 里再抬一次 z（会变成叠加两次）。
    static void C_InteractionCopies()
    {
        var root = new GameObject("B141W24_C");
        try
        {
            var it = root.AddComponent<CardInteraction>();
            Check(it != null, "C0（前提）拿得到 CardInteraction");
            if (it == null) return;

            CheckClose(it.frontZ, WantFrontZ, 1e-7f,
                       "C1 ★ `CardInteraction.frontZ` 与 `HandLayout.frontZ` 同步（同一判据 `ShowCardInHand.c:104`）");

            var f = typeof(CardInteraction).GetField("frontZ");
            Check(f != null && f.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length > 0,
                  "C2 ★ 同样必须 `[System.NonSerialized]`（`Scenes/Battle.unity:640` / `CardBase.unity:8150` 序列化着旧 0.5）");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
