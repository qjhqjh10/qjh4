// B99E4_Assert.cs — 执行代理 `B99_E4`（第九十九会话 · 波 1）的断言
//
// ============================ 它盯的是什么 ============================
//   账 `战斗逻辑驱动:1178` `PlayerHand.MoveCardToMulliganPos(CardScript, int, int)` 的**几何那一半**
//   —— 换牌区那一行的摆位，从「用户实拍量出来的常量（卡宽 292 / 中心距 414）」换成
//   **原版 `CardsHorizontalLayout.GetPosition` 的算法**（`MulliganPanel.RowStepPx` / `RowDXAt` / `RowSortRankFor`）。
//
//   判据（⛔ 别在这里抄第二份）：
//     · 机器码（唯一权威）= `d:/2/tools/decomp_full/CardsHorizontalLayout__GetPosition.c:57-63,147-149`（步长）·
//       `:201-214`（压缩支）· `:215-236`（居中 / y / z）
//     · 可读交叉源     = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CardsHorizontalLayout.cs:240-360`
//     · 调用方         = `d:/2/tools/decomp_full/PlayerHand__MoveCardToMulliganPos.c:9-101`
//     · **实况**（本文件真正的「尺子」）= 原版运行期探针直接问 `GetPosition(i, n)` 的那张表
//       → `资料/原版参照图/Unity参照管线_0825/data/panel_0914b/报告.md` §③
//   ⚠️ 那张表是**选牌行**问出来的，但它和换牌行**是同一份序列化布局**（`MB_4052` 与 `MB_4881` 逐字段相同：
//      `m_betweenElementsSpacing = 502.2` · `m_maxLayoutSize = m_maxLayoutSizeSmallScreen = 0.98` ·
//      `m_inverted = 0` · `useZOrder = 0`），而且 `GetPosition` 里**只有** `lossyScale`（两个锚点的 lossyScale 都是 0.0674）
//      与视口宽（同一个 UI 相机）两处运行态输入 ⇒ 两行的 step 序列**必然相同**（铁律 5·c：这是「同组件同状态」，不是拿别处的值冒充）。
//
// ============================ 怎么用（宿主）============================
//   宿主 = `CardPresentation/Editor/BattleScene.cs`（`BattleScene.Run`）。
//   在**合计行那一行**（现读 `:46649`，`… B97W16Assert.Run(); pass += …; fail += …;  // 🆕 …`）
//   的**同一物理行末尾**追加（⛔ 别新起一行 —— 那会让本文件下游的账行号整片漂移）：
//       ` B99E4_Assert.Run(); pass += B99E4_Assert.Pass; fail += B99E4_Assert.Fail;`
//   · 本件**不建任何对象**（纯静态算式）⇒ 放在哪儿都不污染后面的断言；
//   · 也**不需要** `BattleDriver`（⛔ 别改签名为 `CheckB99E4(driver)` —— 它一个 driver 字段都不读）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A 段：把 `RowStepPx` 改回 `return RowSpacingPx;`（去掉压缩支）⇒ A3/A4 的 n=4..7 **必红**（470.4→502.2）。
//   · A 段：把 `RowSpacingPx` 改回 414 ⇒ A2 **必红**（`RowStepPx(3)` 从 502.2 → 414）。
//   · B 段：把 `RowDXAt` 的 `− (n−1)/2` 删掉（退回「从行心往右排」）⇒ B1/B2 必红（不是居中了）。
//   · C 段：把 `isPlayerRow` 的反向那一步删掉（两条支同值）⇒ C2 的**灭自证**条必红。
//   · D 段：把 `RowSortRankFor` 写成 `return i;`（升序）⇒ D1/D2 必红。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B99_E4`：换牌行摆位的**算法**（原版 `CardsHorizontalLayout.GetPosition`）的断言。
/// 计数口径照 `CheckSink`（`Editor/MenuCheck.cs`），与其余 `B9x` 断言件同一形状。</summary>
public static class B99E4_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B99E4] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>跑过的断言里通过的条数（宿主聚合用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主聚合用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ── 原版运行期探针那张表（`panel_0914b/报告.md` §③ 逐字）—— **期望值只认它**，⛔ 别从我们自己的常量抄期望 ──
    //   n : 相邻中心距（px）
    const float ProbeSpacing2 = 502.2f;    // 报告「n=2 … 502.2」（表里写 502.2；正文另写 502.1，同一档）
    const float ProbeSpacing3 = 502.2f;    // n=3
    const float ProbeSpacing4 = 470.6f;    // n=4
    const float ProbeSpacing5 = 376.5f;    // n=5
    const float ProbeSpacing6 = 313.7f;    // n=6
    const float ProbeSpacing7 = 268.9f;    // n=7
    const float Tol = 0.3f;                // 探针表只给到 0.1 px；我们的 0.98×1920 = 1881.6 与其 1882.4 差 0.8 ⇒ 单档差 ≤0.2 px

    public static void Run()
    {
        MenuCheck.Section(_sink, "B99E4 换牌行摆位 = 原版 `CardsHorizontalLayout.GetPosition`（账 `战斗逻辑驱动:1178`）");

        SectionA_步长与压缩支();
        SectionB_居中几何();
        SectionC_非本方反向支();
        SectionD_非本方叠放序();

        Debug.Log(_sink.P + "B99E4 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 步长与压缩支
    static void SectionA_步长与压缩支()
    {
        MenuCheck.Section(_sink, "A 中心距 = m_betweenElementsSpacing(502.2) × lossyScale，超 0.98×视口宽 时按 n 压缩");

        // A0 前提：设计 px 口径（视口宽在设计 px 下恒 = 1920）—— 不成立时下面所有压缩档都会错，先把它钉住。
        Tr(Mathf.Approximately(LayoutSpace.DesignPxW, 1920f),
           $"A0（前提）我们的设计 px 口径 = 1920×1080（`LayoutSpace.DesignPxW` = {LayoutSpace.DesignPxW}）"
         + "—— 原版 `m_calculatedMaxLayoutSize` = `0.98 × 视口宽` 的换算全靠它");

        // A1 n ≤ 3 不压缩（3 × 502.2 = 1506.6 < 1881.6）
        Near(MulliganPanel.RowStepPx(3), ProbeSpacing3, Tol,
             "A1 n=3（先手）中心距 = 502.2 px（**不压缩**：3 × 502.2 = 1506.6 < 0.98 × 1920 = 1881.6）"
           + "｜判据 = 原版运行期探针表 n=3 那一格");

        // A2 n=2 同档（1004.4 < 1881.6）
        Near(MulliganPanel.RowStepPx(2), ProbeSpacing2, Tol,
             "A2 n=2 中心距 = 502.2 px（不压缩）｜🧨 把 `RowSpacingPx` 改回实拍值 414 ⇒ 这条红");

        // A3 n=4（后手）压缩到 1881.6/4 = 470.4
        Near(MulliganPanel.RowStepPx(4), ProbeSpacing4, Tol,
             "A3 n=4（后手）中心距 = 470.6 px（**压缩支**：4 × 502.2 = 2008.8 > 1881.6 ⇒ step = 1881.6 / 4 = 470.4）"
           + "｜判据 = 探针表 n=4；🧨 删掉 `RowStepPx` 的压缩支 ⇒ 这条红（会得 502.2）");

        // A4 n=5..7 逐档
        Near(MulliganPanel.RowStepPx(5), ProbeSpacing5, Tol, "A4a n=5 中心距 = 376.5 px（1881.6 / 5 = 376.32）｜判据 = 探针表 n=5");
        Near(MulliganPanel.RowStepPx(6), ProbeSpacing6, Tol, "A4b n=6 中心距 = 313.7 px（1881.6 / 6 = 313.6）｜判据 = 探针表 n=6");
        Near(MulliganPanel.RowStepPx(7), ProbeSpacing7, Tol, "A4c n=7 中心距 = 268.9 px（1881.6 / 7 = 268.8）｜判据 = 探针表 n=7");

        // A5 灭自证（结构上不可能同时成立的那一对）：压缩档必须**严格小**于未压缩档 ⇒ 证明压缩支真的接上了，
        //    而不是「我们的常量恰好等于探针表的数」。
        Tr(MulliganPanel.RowStepPx(4) < MulliganPanel.RowStepPx(3) - 1f
           && MulliganPanel.RowStepPx(7) < MulliganPanel.RowStepPx(6)
           && MulliganPanel.RowStepPx(6) < MulliganPanel.RowStepPx(5)
           && MulliganPanel.RowStepPx(5) < MulliganPanel.RowStepPx(4),
           "A5（灭自证）压缩档**严格递减**：step(3) > step(4) > step(5) > step(6) > step(7)"
         + $"（实测 {MulliganPanel.RowStepPx(3):F1} / {MulliganPanel.RowStepPx(4):F1} / {MulliganPanel.RowStepPx(5):F1} / "
         + $"{MulliganPanel.RowStepPx(6):F1} / {MulliganPanel.RowStepPx(7):F1}）"
         + "｜🧨 把压缩支删掉（恒返 502.2）⇒ 本条红");

        // A6 边界：n=0/负数 不许抛、不许返负数（生产上 n 由 `_cards.Count` 来，空批是合法输入）
        Tr(MulliganPanel.RowStepPx(0) == 0f && MulliganPanel.RowStepPx(-1) == 0f,
           "A6 n ≤ 0 ⇒ 步长 0（不抛、不返负）—— 空批是合法输入（`BuildCardButtons` 里 n==0 直接 return）");
    }

    // ============================================================ B 居中几何
    static void SectionB_居中几何()
    {
        MenuCheck.Section(_sink, "B 横向几何 = 行心 + step × (k − (n−1)/2)（`.c:200-215`；ilspy `:350-360` 的 `* 0.5f`）");

        float s3 = MulliganPanel.RowStepPx(3);

        // B0 前提：n=3 的三条偏移必须两两不同 —— 否则「居中」这条断言退化（三张叠在一处也能过）
        float d0 = MulliganPanel.RowDXAt(0, 3, s3, true);
        float d1 = MulliganPanel.RowDXAt(1, 3, s3, true);
        float d2 = MulliganPanel.RowDXAt(2, 3, s3, true);
        Tr(d0 < d1 - 1f && d1 < d2 - 1f,
           $"B0（前提）n=3 三个横向偏移互不相同且递增（{d0:F1} / {d1:F1} / {d2:F1}）");

        Near(d1, 0f, 1e-3f, "B1 n=3 中间那张**正好在行心**（原版 `x = pos.x − (n−1)·step·0.5 + k·step`，k=1 ⇒ 0）"
                          + "｜🧨 删掉 `− (n−1)/2` 那一项（退回「从行心往右排」）⇒ 这条红");
        Near(d0, -s3, 0.01f, "B2 n=3 第 0 张 = 行心 − step（−502.2）；与 B1 合起来 = **以行心对称**");
        Near(d2, +s3, 0.01f, "B3 n=3 第 2 张 = 行心 + step（+502.2）");

        // B4 整排关于行心**零和**（对称性的独立读数；⛔ 不是拿 B1..B3 重算一遍）
        float sum = 0f;
        for (int i = 0; i < 3; i++) sum += MulliganPanel.RowDXAt(i, 3, s3, true);
        Near(sum, 0f, 1e-3f, "B4（灭自证）n=3 三条偏移**和为 0** ⇒ 整排居中（不是「整体右移 / 左移」）");

        // B5 n=4（后手、压缩档 470.4）：外两张 ±705.6，内两张 ±235.2
        float s4 = MulliganPanel.RowStepPx(4);
        Near(MulliganPanel.RowDXAt(0, 4, s4, true), -1.5f * s4, 0.01f, $"B5a n=4 第 0 张 = 行心 − 1.5·step（{(-1.5f * s4):F1}）");
        Near(MulliganPanel.RowDXAt(3, 4, s4, true), +1.5f * s4, 0.01f, "B5b n=4 第 3 张 = 行心 + 1.5·step");
        Near(MulliganPanel.RowDXAt(0, 4, s4, true) + MulliganPanel.RowDXAt(3, 4, s4, true), 0f, 0.01f,
             "B5c（灭自证）n=4 外两张关于行心对称（和 = 0）");

        // B6 n=1 ⇒ 0（原版 `param_4 == 1` 那一支把 t 置 0、偏移恒 0）
        Near(MulliganPanel.RowDXAt(0, 1, MulliganPanel.RowStepPx(1), true), 0f, 1e-4f,
             "B6 n=1 ⇒ 偏移 0（原版 `.c:206` 的 `if (param_4 != 1)` 那一支不跑）");

        // B7 灭自证（防「两边一起改回去」）：n=3 的偏移必须**真的等于** compress 后的步长，
        //    而不是任何别的常数（实拍 414 / 手牌 165 / 选牌 502.2 三档都做过）；
        //    n=4 那条同时钉死「压缩档不是 502.2」。
        Tr(Mathf.Abs(d2 - 502.2f) < 0.01f && Mathf.Abs(MulliganPanel.RowDXAt(3, 4, s4, true) - 705.6f) < 0.2f,
           $"B7（灭自证）n=3 端位 = ±502.2、n=4 端位 = ±705.6（实测 {d2:F2} / {MulliganPanel.RowDXAt(3, 4, s4, true):F2}）"
         + "｜🧨 若把 RowSpacingPx 与 RowStepPx 一起改回 414 ⇒ 这条红（两端都变 414 / 621）");
    }

    // ============================================================ C 非本方反向支
    static void SectionC_非本方反向支()
    {
        MenuCheck.Section(_sink, "C 非本方支：索引**反向** `(total − cardPos) − 1`（`MoveCardToMulliganPos.c:36-46`）");

        float s = MulliganPanel.RowStepPx(3);

        // C0 前提：n=3 时正反两支**不同**（否则下面的断言没有判别力）
        Tr(!Mathf.Approximately(MulliganPanel.RowDXAt(0, 3, s, true), MulliganPanel.RowDXAt(0, 3, s, false)),
           "C0（前提）n=3 时「本方第 0 张」与「非本方第 0 张」**不在同一处**（反向支真的改了索引）");

        // C1 非本方第 i 张 = 本方第 (n−1−i) 张（逐张）
        bool all = true;
        for (int i = 0; i < 4; i++)
        {
            float sN = MulliganPanel.RowStepPx(4);
            if (Mathf.Abs(MulliganPanel.RowDXAt(i, 4, sN, false) - MulliganPanel.RowDXAt(3 - i, 4, sN, true)) > 1e-3f)
                all = false;
        }
        Tr(all, "C1 非本方第 i 张 ≡ 本方第 (n−1−i) 张 —— 逐张对（n=4 全 4 张）");

        // C2 非本方第 0 张落在**最右**（本方最后一张的位置）—— 原版 `SetSortingOrder(total−pos−1)` 让最右那张最靠前，
        //    与「敌方手牌扇形从右往左压」自洽。
        Near(MulliganPanel.RowDXAt(0, 3, s, false), MulliganPanel.RowDXAt(2, 3, s, true), 1e-3f,
             "C2 非本方第 0 张 = **最右**（+502.2）｜🧨 把 `RowDXAt` 里 `isPlayerRow ? i : (n−1−i)` 的反向那一步删掉 ⇒ 这条红");

        // C3 中间那张在反向支下**不动**（n 为奇数时中点 = 行心）—— 独立读数
        Near(MulliganPanel.RowDXAt(1, 3, s, false), 0f, 1e-3f, "C3 n=3 反向支的中间那张仍在行心（对称性不受反向影响）");
    }

    // ============================================================ D 非本方叠放序
    static void SectionD_非本方叠放序()
    {
        MenuCheck.Section(_sink, "D 非本方叠放序 = `currentHand.Count − cardPos − 1`（`MoveCardToMulliganPos.c:36-39`）");

        // D0 前提：n=4 时必须**四档互不相同**（否则「递减」这条断言退化）
        Tr(MulliganPanel.RowSortRankFor(0, 4) != MulliganPanel.RowSortRankFor(1, 4)
           && MulliganPanel.RowSortRankFor(2, 4) != MulliganPanel.RowSortRankFor(3, 4),
           "D0（前提）n=4 的叠放序四档互不相同");

        // D1 端点：第 0 张 = n−1（最靠前）、最后一张 = 0
        Tr(MulliganPanel.RowSortRankFor(0, 4) == 3 && MulliganPanel.RowSortRankFor(3, 4) == 0,
           $"D1 n=4：第 0 张序 3（最靠前）· 第 3 张序 0（最靠后）—— 实测 {MulliganPanel.RowSortRankFor(0, 4)} / "
         + $"{MulliganPanel.RowSortRankFor(3, 4)}｜🧨 写成 `return i;`（升序）⇒ 这条红");

        // D2 严格递减（逐档独立算，⛔ 不是拿 D1 推）
        bool desc = true;
        for (int i = 1; i < 5; i++)
            if (!(MulliganPanel.RowSortRankFor(i, 5) < MulliganPanel.RowSortRankFor(i - 1, 5))) desc = false;
        Tr(desc, "D2 n=5：叠放序随下标**严格递减**（越靠左的牌越靠前 —— 敌方扇形从右往左压）");
    }
}
