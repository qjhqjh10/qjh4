// B94Y5_Assert.cs — 执行代理 Y5（第九十四会话 · 波 1.5）的两块活之自检。
//
// ============================ 它盯的是哪一行账 ============================
//  · **`特效动画:524`** — `TutorialPointer.StartPointer`（战斗面载体 = `Battle/TutorialOverlay.cs`）→ 补两条真差：
//     ① **双点口**（原版 `clickAndDrag`：起点 + 拖拽终点）—— 我们原来是单点口（`SetPointer(bool, Vector3)`）；
//     ② **恒在闸**（原版那颗 `TutorialPointer` 恒在 + `activeAnim`/`animCount` 的永久循环）。
//  逐条的判据、做法、为什么这么裁 → `资料/完全复刻/报告/B94_Y5.md`（⛔ 别在这里抄第二份）。
//
// ============================ 怎么用（宿主）============================
// `B94Y5_Assert.Run();`
//   · **不跑 Unity 批处理**、不建持久资产；临时根在收尾 `DestroyImmediate`。
//   · ⚠️ 本文件**还没接到宿主里**（文件所有权只到 `TutorialOverlay.cs` 与本文件）⇒ 由调度台在宿主 `Run()` 里加两行：
//       `B94Y5_Assert.Run();`
//       `_sink.Pass += B94Y5_Assert.Pass; _sink.Fail += B94Y5_Assert.Fail;`
//   · 世界档（`TutorialOverlay.Create(root.transform)` 传 `uguiHost: null`）—— 几何断言按**世界单位**读，
//     所以这里**强制走世界档**（画布档会把同一组常量折成设计 px，断言口径不同，⛔ 别在画布档下跑本文件）。
//     ⚠️ 世界档 `UseHost(null)` 会**故意** `Debug.LogError` 一句（本工程红线：不许静默失败）—— 那是**预期日志**，
//        不是本文件的失败（失败只按 `CheckSink` 的 `✗` 数）。
//
// ============================ 🔴 灭自证（每条都把「改哪一处会红」写进文案）============================
//   · 双点口：把 `SetPointer(bool,Vector3,Vector3)` 改回只记 `from`（丢弃 `to`）⇒ **D6/D7/D14/D15 红**。
//   · 偏移：把 `GetAdjustedPos` 里的 `xOffset/yOffset` 去掉 ⇒ **D8/E1 红**。
//   · 点击态：把 `ShowClickedPointer` 的缩放删掉 ⇒ **D10 红**（D11 是它的判别式）。
//   · 恒在：把 `TickPointer` 改成「一轮后 `_ptrAnimActive = false`」⇒ **D18/D19 红**。
//   · 换动作归零：把 `StartPointer` 末尾的 `_ptrPhase = 0` 删掉 ⇒ **D21 红**。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 Y5：`TutorialPointer.StartPointer`（账 `特效动画:524`）在战斗面的
/// **双点口** + **恒在闸**两块的断言。计数口径照 `CheckSink`（`Editor/MenuCheck.cs`）。
/// <para>⛔ **期望值全是字面量 / 引用相等**，**不从被测实现里读**（否则就是「拿我们的实现证明我们的实现」）。
/// 唯一从实现读的只有「前提」（`PointerUsesArrowArt`）与「跨档判别式」（`To != From`）—— 那两条本就是要断「两态不同」。</para></summary>
public static class B94Y5_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B94Y5] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>跑过的断言里通过的条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Eq<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    const float Tol = 1e-3f;

    /// <summary>只比 x/y（`z` 是 `ZPointer` 私有常量，不属于本账；几何差都在 xy 上）。</summary>
    static void NearXY(Vector3 got, float wx, float wy, string msg)
    {
        Tr(Mathf.Abs(got.x - wx) < Tol && Mathf.Abs(got.y - wy) < Tol,
           msg + $"（实得 ({got.x:F3},{got.y:F3})，期望 ({wx:F3},{wy:F3})）");
    }

    /// <summary>按 0.01 秒小步推进指针状态机（批处理没有帧循环 ⇒ `Tick` 是被显式喂的）。</summary>
    static void Advance(TutorialOverlay ov, float seconds)
    {
        int n = Mathf.Max(1, Mathf.RoundToInt(seconds / 0.01f));
        for (int i = 0; i < n; i++) ov.TickPointerForTest(0.01f);
    }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B94_Y5：`TutorialPointer.StartPointer` 战斗面 —— 双点口 / 恒在闸（账 特效动画:524 · G346）");

        var root = new GameObject("B94Y5_Root");
        try
        {
            // 世界档（`uguiHost: null`）—— 几何断言按世界单位读（见文件头）。
            var ov = TutorialOverlay.Create(root.transform);
            if (ov == null)
            {
                Tr(false, "P0 前提：`TutorialOverlay.Create` 交回非空实例");
                return;
            }
            SectionPointer(ov);
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        Debug.Log(_sink.P + "B94_Y5 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ------------------------------------------------------------ 指针状态机
    static void SectionPointer(TutorialOverlay ov)
    {
        // ---- 前提（硬检查）：指针层必须建出来 —— 否则下面全部空转（本工程记过的「if(前提) Check」坑）
        Tr(ov.PointerUsesArrowArt,
           "P1 前提：`_pointer` 建出来了（`40k_general_bt_arrow` 在 `Resources/Art/ui_menu/`）"
           + "｜🧨 缺图 ⇒ 这条红，且下面每一条都会空转（先修图再跑）");

        Vector3 from = new Vector3(0f, 0f, 0f);
        Vector3 to = new Vector3(1f, 0.5f, 0f);
        // 原版 prefab（`MonoBehaviour_4791.json`，13 场同值）实读：xOffset = 2.0 · yOffset = -2.0。
        Vector3 adjFrom = new Vector3(from.x + 2f, from.y - 2f, 0f);
        Vector3 adjTo = new Vector3(to.x + 2f, to.y - 2f, 0f);
        Vector3 adjFromLeft = new Vector3(from.x - 2f, from.y - 2f, 0f);

        // ===================== ① 单点口（旧签名）= 原版 `clickInPosition` =====================
        ov.SetPointer(true, from);
        Eq(ov.PointerActionForTest, TutorialOverlay.PointerActionKind.ClickInPosition,
           "D1 单点口 ⇒ 档 = `clickInPosition`（原版 `TutorialPointerActions.clickInPosition = 0`）");
        Eq(ov.PointerFromForTest, from, "D2 单点口 ⇒ 起点 = 传入点");
        Eq(ov.PointerToForTest, from,
           "D3 ★ 单点口 ⇒ **没有终点**（`To == From`）｜🧨 把双点口的终点硬塞进单点口（或把两档合并）⇒ 本条红");
        Tr(ov.PointerVisible, "D3b 单点口 ⇒ 指针**亮着**（`PointerVisible`；旧调用点仍可用）");

        // ===================== ② 双点口 =====================
        ov.SetPointer(true, from, to);
        Eq(ov.PointerActionForTest, TutorialOverlay.PointerActionKind.ClickAndDrag,
           "D4 ★ 双点口 ⇒ 档 = `clickAndDrag`（原版 `TutorialPointerActions.clickAndDrag = 2`）");
        Eq(ov.PointerFromForTest, from, "D5 双点口 ⇒ 起点 = 传入起点");
        Eq(ov.PointerToForTest, to,
           "D6 ★★ 双点口 ⇒ **终点 = 传入终点**（原版 `StartPointer` 的第 4 参 `endPos`）"
           + "｜🧨 只记 `from`、把 `to` 丢掉 ⇒ 本条红");
        Tr(ov.PointerToForTest != ov.PointerFromForTest,
           "D7 ★★ 灭自证：`To != From`（终点真的被保存了、不是与起点同值）"
           + "｜🧨 `SetPointer(on,from,to)` 里只 `_ptrTo = from` ⇒ 本条红");

        // ---- 位置 = `GetAdjustedPos(from)` = from + (xOffset 2.0, yOffset -2.0) ----
        NearXY(ov.PointerWorldPosForTest, adjFrom.x, adjFrom.y,
           "D8 ★★ 起手态位置 = `GetAdjustedPos(from)` = `from + (xOffset 2.0, yOffset -2.0)`"
           + "（判据 `GetAdjustedPos.c`；两个偏移是 prefab 序列化字段 2.0 / -2.0）"
           + "｜🧨 漏掉偏移 ⇒ 本条红");

        // ---- 点击态（scale = clickScaleFactor 0.7）----
        Advance(ov, 0.55f);          // 过 waitBeforeClick(0.5)
        Eq(ov.PointerPhaseForTest, 1,
           "D9 ★ 过 `waitBeforeClick(0.5)` ⇒ 进点击态（相位 1，原版 `_ClickAndDrag_d__25__MoveNext.c` state1）");
        Tr(Mathf.Abs(ov.PointerScaleXForTest - 0.7f) < Tol,
           "D10 ★★ 点击态 scale = `clickScaleFactor = 0.7`（右朝向 ⇒ (0.7,0.7,0.7)）"
           + "｜🧨 不缩放 ⇒ 本条红");
        Tr(Mathf.Abs(ov.PointerScaleXForTest - 1f) > Tol,
           "D11 ★★ 灭自证：点击态 0.7 **≠** 起手态 1（两态真的不同）");

        // ---- 拖拽：state1 等 0.25 → state2 DOMove → 终点 ----
        Advance(ov, 0.30f);          // 过 waitAfterClick × 0.5 = 0.25
        Eq(ov.PointerPhaseForTest, 2,
           "D12 ★ 拖拽第 2 拍（`waitAfterClick × 0.5 = 0.25`）⇒ 进 DOMove 段（相位 2）");
        // ⚠️ 这里只断「更靠近起点」而不是「精确等起点」：进入相位 2 的那一步之后可能已多走了几毫秒，
        //   精确比会因亚像素抖动误红（D14 才是「走到终点」那条硬断言）。
        {
            var p2 = ov.PointerWorldPosForTest;
            float dStart = Vector2.Distance(new Vector2(p2.x, p2.y), new Vector2(adjFrom.x, adjFrom.y));
            float dEnd = Vector2.Distance(new Vector2(p2.x, p2.y), new Vector2(adjTo.x, adjTo.y));
            Tr(dStart < dEnd,
               $"D13 ★ DOMove 刚开始 ⇒ 指针**更靠近起点**（离起点 {dStart:F3} < 离终点 {dEnd:F3}）");
        }
        // 🔴 R4 订正（2026-10-29 第九十四会话 · 同步点 FX5 · 判据 `B94_DG2_battle_menu.md` §三·R4）：
        //   这里原来是 `Advance(0.72f)` —— 进入相位 2 后 `_ptrPhaseT` 已经 ≈0.09（D13 实测离起点 0.144），
        //   再加 0.72 = **0.81 ≥ `moveDuration(0.7) + 尾部(0.1)` = 0.8** ⇒ **跨过循环回绕点**，
        //   `ShowStartingPointer` 把位置写回**起点** ⇒ D14/D15 是**假红**（被测实现 `PlacePointer` 是对的）。
        //   ⇒ 改成「**只在相位 2 内逐拍采样、取离终点最近的一刻**」，⛔ 不赌固定步长（安全窗口只有 0.61~0.70）。
        //   ⚠️ 原 D14 文案自称「单点口那版没有终点 ⇒ 还停在起点」是**错的**诊断 —— 双点口是好的（D4/D6/D7 绿）——
        //      顺带订正，⛔ 别照那句文案去改实现。
        Vector3 best = ov.PointerWorldPosForTest;
        float bestD = float.MaxValue;
        int bestStep = 0;
        for (int i = 0; i < 80 && ov.PointerPhaseForTest == 2; i++)
        {
            ov.TickPointerForTest(0.01f);
            var p = ov.PointerWorldPosForTest;
            float d = Vector2.Distance(new Vector2(p.x, p.y), new Vector2(adjTo.x, adjTo.y));
            if (d < bestD) { bestD = d; best = p; bestStep = i + 1; }
        }
        Tr(bestStep > 0,
           "D14 前提（硬检查）：相位 2 里**至少采样到一拍**（否则下面两条无从谈起 ⇒ 本文件不静默跳过）");
        NearXY(best, adjTo.x, adjTo.y,
           "D14 ★★ 拖拽过程中指针**到达过** `GetAdjustedPos(to)`（在 `moveDuration(0.7)` 内逐拍采样、取离终点最近的一刻；"
           + "最近距 " + bestD.ToString("F4") + "）—— 原版 `DOMove` 的终点"
           + "｜🧨 `PlacePointer(t)` 不插值 / `GetAdjustedPos(to)` 丢 `to` ⇒ 本条红");
        Tr(Mathf.Abs(best.x - adjFrom.x) > 0.5f,
           "D15 ★★ 灭自证：终点位置**确实离开了起点**（x 差 > 0.5 世界单位）"
           + "｜🧨 `to` 被忽略（只记 `from`）⇒ 差 = 0 ⇒ 本条红");

        // ---- 一个完整循环后回首（原版 state2 → state0）----
        Advance(ov, 0.15f);          // 过 moveDuration + 0.1 = 0.8 ⇒ 回首
        Eq(ov.PointerPhaseForTest, 0,
           "D16 ★ 拖拽一轮（0.5 + 0.25 + 0.8 ≈ 1.55s）后回首（相位 0，原版 state2 → state0）");
        NearXY(ov.PointerWorldPosForTest, adjFrom.x, adjFrom.y,
           "D17 ★ 回首后指针回到**起点**（原版 state0 重新 `ShowStartingPointer(from)`）");

        // ===================== ③ 恒在闸（永久循环 = 原版 `activeAnim`/`animCount`）=====================
        for (int i = 0; i < 5; i++) Advance(ov, 1.6f);   // ≈ 5 轮以上
        Tr(ov.PointerAnimActiveForTest,
           "D18 ★★ **恒在闸**：连推 ≈ 8 秒（5 轮以上）后 `activeAnim` **仍为真** —— 原版协程 state2→0 回首**永不退出**"
           + "｜🧨 写成「一次性播完就停」（播一轮后 `_ptrAnimActive=false`）⇒ 本条红");
        Tr(ov.PointerVisible,
           "D19 ★★ 同一时刻指针**仍亮着**（`PointerVisible`）——「一直显形」那一半"
           + "｜🧨 播完就 `SetActive(false)` ⇒ 本条红");

        // ---- 换动作 ⇒ 作废旧相位（原版 `animCount++`）----
        ov.SetPointer(true, from, to);
        Advance(ov, 0.55f);
        Eq(ov.PointerPhaseForTest, 1,
           "D20 前提：新一轮 `StartPointer` 后相位从 0 重新走到 1");
        int countBefore = ov.PointerAnimCountForTest;
        ov.SetPointer(true, from, to);
        Eq(ov.PointerPhaseForTest, 0,
           "D21 ★★ **换动作**（再次 `StartPointer`）⇒ 相位**立刻归 0**（原版 `animCount++` 让旧协程作废）"
           + "｜🧨 把 `StartPointer` 里的 `_ptrPhase = 0` 删掉 ⇒ 相位停在 1 ⇒ 本条红");
        Eq(ov.PointerAnimCountForTest, countBefore + 1,
           "D22 ★ `animCount` 每次 `StartPointer` ++（原版 `+0x6c`）");

        // ===================== ④ 朝向 `left`（原版 `PointerRelativePos.left = 1`）=====================
        ov.SetPointer(true, from, to, TutorialOverlay.PointerOrient.Left);
        NearXY(ov.PointerWorldPosForTest, adjFromLeft.x, adjFromLeft.y,
           "E1 ★★ `left` 朝向 ⇒ `GetAdjustedPos` 的 xOffset **取反**（`from.x − 2.0`，原版符号位翻转 `^0x80000000`）"
           + "｜🧨 朝向被忽略 ⇒ 仍是 +2 ⇒ 本条红");
        Tr(Mathf.Abs(ov.PointerScaleXForTest + 1f) < Tol,
           "E2 ★★ `left` 朝向 ⇒ 起手态 scale.x = **−1**（原版 `ShowStartingPointer` 的 `(−1,1,1)`）"
           + "｜🧨 朝向被忽略 ⇒ 仍是 1 ⇒ 本条红");

        // ===================== ⑤ 收起（两态）=====================
        ov.SetPointer(false, from);
        Tr(!ov.PointerVisible,
           "E3 ★ 收起 ⇒ 指针 `activeSelf = false`（原版 `OnDisable` 的 `SetActive(false)`）");
        Tr(!ov.PointerAnimActiveForTest,
           "E4 ★ 收起 ⇒ `activeAnim = false`（原版 `OnDisable.c:12` 第一句）");
        Tr(ov.PointerShowForTest,
           "E5 前提：原版 `showPointer(+0x71)` 出厂 = 1（本件没有文字气泡 ⇒ 恒真；`SetTutorialText` 那一支不在这条链上）");

        // ===================== ⑥ 恒在闸的「另一半」＝ 建层闸（本件侧能证明的部分）=====================
        //   原版那颗 `TutorialPointer` = `BattleManager.tutorialPointerBattle`（`dump.cs` `+0x208`），**非教程局也在**。
        //   那一半的闸在 `BattleDriver.EnsureTutorialOverlay`（不在本文件白名单）—— 本件能证明的是：
        //   **`TutorialOverlay` 自身不含任何教程依赖** ⇒ 只要建层闸放开，指针层在非教程局照样工作。
        Tr(ov.PointerUsesArrowArt,
           "E6 ★ **恒在闸（本件那一半）**：`TutorialOverlay` 在**没有教程上下文**的场景里也能建出指针层"
           + "（本夹具全程 `Ctx == null`）⇒ 本件**不含**教程闸；剩下那一半 = `BattleDriver.EnsureTutorialOverlay`"
           + " 的建层闸（白名单外，接线见 `B94_Y5.md` §3）");
    }
}
