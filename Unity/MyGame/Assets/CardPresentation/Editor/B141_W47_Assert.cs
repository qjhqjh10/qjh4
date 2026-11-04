// B141_W47_Assert.cs — 执行写手 `B141-W47` 的断言（W129 · 11-10 `CardFeel.Dissolve`）
//
// ============================ 盯的是什么 ============================
// `Core/CardFeel.cs` 的 `Dissolve()`（**阵亡兜底**）的**补间形状** —— 不是它走哪条路，而是「动起来长什么样」。
//
// 判据（🥇 一手）：`decomp_full/CardScript__DissolveWholeCard.c:18-32`（全文 54 行，本批亲读）
//   `uVar4 = Component.get_transform(this)` → `local_38 = Vector3.one × spellPlayedScale` →
//   `DG_Tweening_ShortcutExtensions__DOScale(uVar4, &local_38, param_2, 0)` → `BattleCardUI__FadeCardItems(ui, 0, param_2, 0)`
//   · **没有 `DOMove`**（原版这一跳**没有位移**）—— 我们原来那句 `DOMove(tr.position + ToOurs(0.3), dur)` 是自设计。
//   · 缩放终点 = `Vector3.one × VarsGlobal.spellPlayedScale(+0x40)` = **`Vector3.one` × 1.0**
//     （值判据 → `资料/VarsGlobal_原版数值.md` §一 `spellPlayedScale = 1.0`）—— 我们原来是 `localScale × 0.85`。
//   · 淡的层 = `FadeCardItems` → `BasicCardUI.FadeCard` = **`fadeGroup.alpha`**
//     （判据链 → 账 `战斗逻辑驱动:730` + `B9_E2UDW` 报告 §②）⇒ 我们的等价量 = `CardView.Alpha2D`，
//     **不是** `CardView.Alpha`（后者连 3D 卡体一起淡）—— 我们原来那句是 `Alpha`。
//   · `DOScale` / `FadeCard` **都没写 `SetEase`** ⇒ DOTween 默认 = `OutQuad`
//     （`DOTweenSettings.asset:31 defaultEaseType = 6`）—— 我们原来是 `InSine` / `InQuad`。
//
// ============================ 怎么用 ============================
//   `B141_W47_Assert.Run();`   （静态类 + 无参 `Run()`；全局命名空间、类名 == 文件名）
//   ✅ **已接线（2026-11-04 `B141-W47b`）**：`Editor/BattleScene.cs:47035` 的聚合行、合计 `Debug.Log` 之前
//      ——`B141_W47_Assert.Run(); pass += B141_W47_Assert.Pass; fail += B141_W47_Assert.Fail;`（同一物理行追加 ⇒ 行数零变化）。
//   ⚠️ 本类**不建持久资产**：临时 GameObject 在 `finally` 里 `DestroyImmediate`，
//      并把 `CardTween.Mode` / `CardFeel.ForceNoDissolveForTest` / `CardFeel.ResetDeathFallbackWarnForTest` 原样还原。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2★ 无位移   ← 把 `seq.Append(tr.DOMove(...))` 加回来 ⇒ 位置变了 ⇒ 红。
//   · A3★ 缩到 1   ← 把 `DOScale(Vector3.one, dur)` 改回 `DOScale(tr.localScale * 0.85f, dur)`：探针起点 **0.36**
//                     ⇒ 终点 0.306 ≠ 1 ⇒ 红（**探针故意设成非 1 起点**，否则这条恒绿、等于没断）。
//   · A4★ 淡 Alpha2D ← 把 `SetAlpha2D` 改回 `SetAlpha`：`Alpha` 变 0、`Alpha2D` 停 1 ⇒ 红（两个方向各一条）。
//   · A5★ 默认 OutQuad ← 把 `SetEase(Ease.InQuad)` / `SetEase(Ease.InSine)` 加回缩放：u=0.25 处的进度掉到
//                     ≤ 0.08（OutQuad 是 0.4375）⇒ 红。
//   · B3★ 材质那条路同样「缩到 1」← 只改兜底支、漏改 `canDissolve` 支 ⇒ 红。
//
// ⚠️ **旧断言的连带改动已做（2026-11-04 `B141-W47b`）**：`Editor/BattleScene.cs:23121` 那条原来钉的是**旧口径**
//    （`Check(f1.Alpha < 0.05f, …)` 淡整卡）⇒ 已改 `f1.Alpha2D`；`:23124` 的 `SetAlpha(1f)` → `SetAlpha2D(1f)`；
//    `:23118` 文案「整卡淡出」→「2D 卡面淡出」。⛔ 改回 `Alpha` ⇒ 该条必红（与本文件 A4★ 同口径）。
using DG.Tweening;
using UnityEngine;
using CardPresentation;

public static class B141_W47_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "B141W47";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol, msg + $"\n      （实得 {got:F4} · 期望 {want:F4} · 容差 {tol:G3}）");
    }

    /// <summary>探针起点缩放 —— **必须是原版场上那档 0.36**（`ArenaSlots.PlayerCardScale` = `MinionArea.desiredScale`）。
    /// 🔴 故意不用 1：起点是 1 的话「缩到 one」与「乘 0.85」都会落到 1 / 0.85，前者的区分度就没了。</summary>
    const float ProbeStartScale = 0.36f;

    public static void Run()
    {
        _pass = _fail = 0;

        var oldMode = CardTween.Mode;
        bool oldForce = CardFeel.ForceNoDissolveForTest;
        var root = new GameObject("B141W47_ROOT");
        CardView probe = null, probe2 = null;
        try
        {
            CardTween.Mode = UpdateType.Manual;   // 批处理没有帧循环 ⇒ 手动推（与 `CardTween.Advance` 配套）

            // 前提硬检查（⛔ 不写 `if (probe != null) Check(...)` —— 前提不成立时必须是红，不是静默跳过）
            probe = CardView.Create(root.transform, CardData.Placeholder(1), "W47FallbackProbe");
            Check(probe != null, "P0 **前提**：兜底支的探针卡建得出（否则本组什么都没验到）");
            if (probe == null) { Debug.LogError(Tag + " ===== 前提不成立 ⇒ 本宿主什么都没验到（不是通过）====="); return; }
            probe.SetPose(Vector3.zero, 0f, ProbeStartScale);
            Check(Mathf.Abs(probe.transform.localScale.x - ProbeStartScale) < 1e-4f,
                  $"P1 **前提**：探针起点缩放 = {ProbeStartScale}（`SetPose` 真生效 —— 否则 A3 的区分度没了）");

            FallbackBranch(probe);
            MaterialBranch(root, ref probe2);
        }
        finally
        {
            CardTween.Mode = oldMode;
            CardFeel.ForceNoDissolveForTest = oldForce;
            CardFeel.ResetDeathFallbackWarnForTest();
            if (probe != null) Object.DestroyImmediate(probe.gameObject);
            if (probe2 != null) Object.DestroyImmediate(probe2.gameObject);
            Object.DestroyImmediate(root);
        }

        Debug.Log(Tag + $" ===== 合计 {_pass} 通过 / {_fail} 失败 =====");
    }

    // ==========================================================================
    //  A 组：取不到溶解材质那一支（`FallbackPath.WholeCardAlpha`）
    // ==========================================================================
    static void FallbackBranch(CardView probe)
    {
        CardFeel.ForceNoDissolveForTest = true;
        CardFeel.ResetDeathFallbackWarnForTest();

        Vector3 pos0 = probe.transform.position;
        var tw = CardFeel.Dissolve(probe);
        Check(tw != null, "A0 **前提**：`CardFeel.Dissolve(probe)` 返回补间（不是 null）");
        if (tw == null) return;

        // 静态读口必须【紧接着】读 —— `LastDeathFallback` 是上一次结果（同 BattleScene 那条注）
        Check(CardFeel.LastDeathFallback == CardFeel.FallbackPath.WholeCardAlpha,
              $"A1 取不到溶解材质 ⇒ 走 `WholeCardAlpha` 支（实得 {CardFeel.LastDeathFallback}）");

        Near(CardTween_DurationOf(tw), CardFeel.DeathDissolveMinion, 1e-3f,
             "A1b 时长仍是原版分档 `deathTimeMinionDuration` = 0.2 s（本批只动形状、不动时长）");

        // ---- 两档采样断缓动：用 `Elapsed()` 现读真实进度再比四种缓动的**解析值** ----
        //      （⛔ 不写死「推进到 u=0.25 就应该是 0.4375」—— DOTween 首帧的 startup 会不会吃掉一拍不可靠；
        //        现读 elapsed 之后 OutQuad / Linear / InQuad 三者互差 ≥ 0.18，判别毫无歧义）
        CheckEaseAt(probe, tw, 0.25f, "A5a★");
        CheckEaseAt(probe, tw, 0.5f, "A5b★");    // 第二个相位再复验一次（u=0.5：OutQuad 0.75 / Linear 0.5 / InQuad 0.25）

        // ---- 推完：形状三项 ----
        for (int i = 0; i < 40; i++) CardTween.Advance(0.01f);

        Near(probe.transform.position.x, pos0.x, 1e-4f, "A2★ x **无位移**（原版这一跳没有 `DOMove`）");
        Near(probe.transform.position.y, pos0.y, 1e-4f, "A2★ y **无位移**（我们原来那句 `ToOurs(0.3)` 上浮已删）");
        Near(probe.transform.position.z, pos0.z, 1e-4f, "A2★ z **无位移**");

        Near(probe.transform.localScale.x, 1f, 1e-3f, "A3★ x 缩到 **`Vector3.one`**（原版 `DOScale(one × spellPlayedScale 1.0)`；⛔ 不是 ×0.85）");
        Near(probe.transform.localScale.y, 1f, 1e-3f, "A3★ y 缩到 1");
        Near(probe.transform.localScale.z, 1f, 1e-3f, "A3★ z 缩到 1");

        Near(probe.Alpha2D, 0f, 1e-3f, "A4★ 淡的是 **`Alpha2D`**（= 原版 `fadeGroup`）且到 0");
        Near(probe.Alpha, 1f, 1e-4f, "A4★ **整卡 `Alpha` 一个都不碰**（原版淡 `fadeGroup`；⛔ 不是整卡淡出）");

        // 反向覆盖：这四项里任何一项改回旧写法都会在上面的星号条上红（见文件头「灭自证」）。
    }

    // ==========================================================================
    //  B 组：正常档（材质溶解）—— 形状必须与兜底支一致，只是淡的方式不同
    // ==========================================================================
    static void MaterialBranch(GameObject root, ref CardView probe2)
    {
        CardFeel.ForceNoDissolveForTest = false;
        probe2 = CardView.Create(root.transform, CardData.Placeholder(2), "W47MaterialProbe");
        Check(probe2 != null, "B0 **前提**：正常档的探针卡建得出");
        if (probe2 == null) return;

        bool supported = probe2.DissolveSupported;
        Check(supported,
              "B1 **前提**：探针支持材质溶解（`DissolveSupported`）—— 不成立则本组验不到原版那条路（本机缺 dissolve shader）");
        if (!supported) return;

        probe2.SetPose(Vector3.zero, 0f, ProbeStartScale);
        Vector3 pos0 = probe2.transform.position;
        var tw = CardFeel.Dissolve(probe2);
        Check(tw != null, "B2 `CardFeel.Dissolve` 返回补间");
        if (tw == null) return;

        Check(CardFeel.LastDeathFallback == CardFeel.FallbackPath.MaterialDissolve,
              $"B3 支持溶解 ⇒ 走 `MaterialDissolve` 支（实得 {CardFeel.LastDeathFallback}）");

        for (int i = 0; i < 40; i++) CardTween.Advance(0.01f);

        Near(probe2.DissolveAmount, 1f, 1e-3f, "B4 `_DissolveAmount` 溶到 1（原版那条路本身没动）");
        Near(probe2.transform.position.y, pos0.y, 1e-4f, "B5 正常档同样 **无位移**");
        Near(probe2.transform.localScale.x, 1f, 1e-3f,
             "B6★ 正常档同样缩到 **`Vector3.one`**（形状是 `DissolveWholeCard` 的、与走哪条支无关）");
    }

    static float CardTween_DurationOf(DG.Tweening.Tween t)
    {
        return t == null ? -1f : DG.Tweening.TweenExtensions.Duration(t);
    }

    /// <summary>推进到 `uFrac × 时长`，用 `Elapsed()` 现读真实进度，再与 OutQuad / Linear / InQuad 三条**解析曲线**比。
    /// <para>🔴 为什么不用「写死 u=0.25 ⇒ 应该 0.4375」：DOTween 首帧的 startup 会不会吃掉一拍不可靠；
    /// 现读之后三者互差 ≥ 0.1875（u=0.25）/ 0.25（u=0.5），判别没有歧义。</para></summary>
    static void CheckEaseAt(CardView probe, DG.Tweening.Tween tw, float uFrac, string tag)
    {
        float dur = CardTween_DurationOf(tw);
        if (dur <= 1e-6f) { Check(false, tag + " 前提不成立：补间时长读不出来"); return; }
        float target = uFrac * dur;
        // 最多推 3 次：DOTween 首帧如果拿一拍做 startup（不累加 elapsed），第一次推进会是 0 —— 这里兜住
        for (int i = 0; i < 3 && DG.Tweening.TweenExtensions.Elapsed(tw) < target - 1e-5f; i++)
            CardTween.Advance(target - DG.Tweening.TweenExtensions.Elapsed(tw));

        float u = Mathf.Clamp01(DG.Tweening.TweenExtensions.Elapsed(tw) / dur);
        float prog = (probe.transform.localScale.x - ProbeStartScale) / (1f - ProbeStartScale);
        float outQ = 1f - (1f - u) * (1f - u);   // DOTween `Ease.OutQuad` = 1-(1-t)²
        float lin = u;
        float inQ = u * u;                       // `Ease.InQuad` = t²（我们原来那条）
        float dOut = Mathf.Abs(prog - outQ), dLin = Mathf.Abs(prog - lin), dInQ = Mathf.Abs(prog - inQ);

        Check(dOut <= 0.03f && dOut < dLin && dOut < dInQ,
              $"{tag} 缩放缓动 = DOTween 默认 **OutQuad**（u={u:F3}：OutQuad {outQ:F3} / Linear {lin:F3} / InQuad {inQ:F3}）"
            + $"\n      （实得进度 {prog:F4}；|ΔOut| {dOut:F4} · |ΔLinear| {dLin:F4} · |ΔInQuad| {dInQ:F4}）");
    }
}
