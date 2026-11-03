// B123_C3_Assert.cs — 执行写手 `B123-C3`（2026-11-03 · 批次 `B123`）
//   对账条目 = `B121-Q4B-02`（判据 → `资料/完全复刻/判据/B123_对账_C_表现外壳.md` §14）
//
// ============================ 它盯的是什么 ============================
// `CardFeel.HandToBoard`（手牌 → 战场那条 0.9167 s 的 clip）**中途被打断**时，它改过的 **9 项**必须
// 逐项复位到**起播前那一档**（等价做法 = `Hand/CardInteraction.cs:2742-2751` 撤销出牌那条路）：
//   ① 卡框溶解材质实例 → 还原原材质（`EndFrameDissolve`）
//   ② 3D 卡体材质 → 还原（`RestoreOriginalMaterial`）
//   ③ `_DissolveAmount` → 0
//   ④ 3D 卡体可见性 → 关
//   ⑤ `ToBoardFX/Spawn Particles` → 关
//   ⑥ `ToBoardFX/Spawn Particles/Trail` → 关
//   ⑦ 立绘缩放 → 1
//   ⑧ `2DCard.m_LocalPosition.z` → 0
//   ⑨ `body3D` / `Board Elements` 两级 y → 起播前基准
// 另外三档 α（2D 卡面 / 计数器 / 强制显示）本来就无条件复位，这里一起验。
//
// 🔴 **判据的最关键一句**：DOTween 的 `OnKill` 在**正常跑完**时也会触发（`TweenManager.DoUpdate`
//   先调 `onComplete`、再 `Despawn` → `onKill`，反编译 `DOTween.dll` 现读）⇒ 复位**必须**带
//   「是否正常跑完」的标志，否则**场上形态的卡会被关掉 3D 体**（那是新缺陷，不是修旧缺陷）。
//
// ============================ 🧨 灭自证（怎么才能红）============================
//   · N2 / N3 / N4 / N9 —— **正常跑完**那一组：把 `HandToBoard` 里的 `finished` 闸拆掉
//     （`if (!finished) ResetHandToBoardResidue(...)` 改成无条件调）⇒ 跑完那一刻 3D 体被关、
//     立绘缩放被打回 1、`2DCard.z` 被打回 0、两颗粒子被关 ⇒ 这几条**一起红**。
//   · K 组 —— **中途打断**那一组：删 `ResetHandToBoardResidue` 里任意一行 ⇒ 对应那条红
//     （逐项一格，谁漏谁红）。K3 那批「前提」是**反向钳制**：clip 中途若没真的把状态弄脏，
//     上面那些绿就是空转 ⇒ 直接红。
//   · D 组 —— 幂等：第二次 `Kill` 若还能改动状态（或抛异常）⇒ 红；第二条 clip 再被打断后
//     若回不到**同一个**基准（累积漂移）⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//   在宿主的**方法尾聚合行**上（**同一物理行、该行第一个 `//` 之前**；⛔ 追加到行尾那段 `//` 之后
//   = 永不执行，见 `报告/B102_W1_断言接线被注释吞掉.md`）加：
//       B123C3_Assert.Run(); pass += B123C3_Assert.Pass; fail += B123C3_Assert.Fail;
//   建议落点 = `CardPresentation/Editor/CardFaceAssertE5.cs` 的 `Run()` 末尾（同装配、同样会临时改
//   `CardTween.Mode` 并复原）；**接线由调度台做**，本文件不改任何宿主。
//
// ⚠️ 本宿主**不建场景、不存盘**：只在临时根下建对象，收尾全部 `DestroyImmediate`。
// ⚠️ 依赖「这台机器解析得到原版那两条溶解 shader」（`CardBaseDemo` 的 B05 那组也这么假定）：
//   `K4` 那几条前提会把它断出来 —— 取不到就**红**（如实说「这半边没验到」），⛔ 不静默跳过。
using DG.Tweening;
using CardPresentation;
using UnityEngine;

/// <summary>`B121-Q4B-02` 的断言集：`CardFeel.HandToBoard` 的 `OnKill` 复位链（正常跑完 vs 中途打断）。</summary>
public static class B123C3_Assert
{
    public static int Pass;
    public static int Fail;

    static void Check(bool ok, string what)
    {
        if (ok) { Pass++; Debug.Log("[B123C3] ✓ " + what); }
        else { Fail++; Debug.LogError("[B123C3] ✗ " + what); }
    }

    static void CheckClose(float got, float want, float tol, string what)
    {
        Check(Mathf.Abs(got - want) <= tol,
              what + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4")
                  + "；实得 " + got.ToString("F4") + "）");
    }

    /// <summary>本文件**最后**造的那条补间（`finally` 里兜底清掉）。
    /// ⚠️ 不用 `DOTween.KillAll` —— 本件会被挂在**别的宿主**里跑（见文件头的接线补丁），
    /// 一个 `KillAll` 会把那个宿主上还活着的别人家的补间一起打死。</summary>
    static Tween _last;

    public static void Run()
    {
        Pass = 0; Fail = 0;
        var keepMode = CardTween.Mode;
        try
        {
            RunNormalCompletion();
            RunInterruptedKill();
            RunDoubleKill();
        }
        finally
        {
            if (_last != null) { _last.Kill(false); _last = null; }
            CardTween.Mode = keepMode;
        }
    }

    // ==============================================================================================
    //  ① 正常跑完 ⇒ **不许**做那 9 项复位（3D 体还得在场上开着）
    // ==============================================================================================
    static void RunNormalCompletion()
    {
        var root = new GameObject("B123C3_Normal");
        try
        {
            var cv = NewCard(root.transform, "b123c3_normal");
            if (cv == null) return;
            MountFx(cv);
            var body = cv.transform.Find("body3D");
            var be = cv.transform.Find("Board Elements");
            // ---- 前提（硬检查：不成立就红，⛔ 不写 `if (x != null) Check(...)` 式静默跳过）----
            Check(body != null, "N0 前提：`body3D` 建出来了（否则两级 y 那条没验到）");
            Check(be != null, "N0' 前提：`Board Elements` 建出来了");
            Check(cv.Body3DVisible == false, "N0'' 前提：起播前 3D 体是关着的（手牌态）");
            if (body == null || be == null) return;

            CardTween.Mode = UpdateType.Manual;
            var tw = CardFeel.HandToBoard(cv);
            _last = tw;
            Check(tw != null, "N1 前提：`HandToBoard` 起了一条补间");
            if (tw == null) return;
            AdvanceSeconds(1.2f);   // 跑过 0.9167 整条（autoKill ⇒ `onComplete` 之后紧接 `onKill`）

            // 场上形态的标志：3D 体开着、立绘缩放停在 clip 终点、`2DCard.z` 停在 −0.043、两颗粒子开着
            Check(cv.Body3DVisible,
                  "N2 **正常跑完 ⇒ 3D 卡体仍可见**（`OnKill` 那 9 项复位被 `finished` 闸挡住）"
                  + "｜🧨 把 `if (!finished)` 拆掉 ⇒ 场上卡当场被关体 ⇒ 红");
            CheckClose(cv.ArtLocalScale, CardFeel.HandToBoardShrink, 1e-3f,
                       "N3 正常跑完 ⇒ 立绘缩放停在 clip 终点 `HandToBoardShrink`（⛔ 没被打回 1）");
            CheckClose(cv.FaceRootLocalZ, CardFeel.HandToBoardFaceZ, 1e-3f,
                       "N4 正常跑完 ⇒ `2DCard.z` 停在 clip 终点（⛔ 没被打回 0）");
            Check(cv.SpawnParticlesActive && cv.TrailActive,
                  "N5 正常跑完 ⇒ `ToBoardFX` 两颗粒子仍开着（那是场上形态的一部分）"
                  + "（实得 Spawn=" + cv.SpawnParticlesActive + " / Trail=" + cv.TrailActive + "）");
            Check(!cv.OnFrameDissolveMaterial && cv.FrameDissolveAmount < 0f,
                  "N6 正常跑完 ⇒ 卡框已还原原材质（0.75 那条回调的活；`FrameDissolveAmount` = "
                  + cv.FrameDissolveAmount.ToString("F2") + "，−1 = 已复位）");
            CheckClose(cv.Alpha2D, 1f, 1e-3f, "N7 正常跑完 ⇒ 2D 卡面 α = 1");
            CheckClose(cv.CounterAlpha, 1f, 1e-3f, "N8 正常跑完 ⇒ 计数器 α = 1");
            Check(!cv.CountersForcedVisible, "N9 正常跑完 ⇒ 不再强制显示计数器");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ==============================================================================================
    //  ② 中途 Kill ⇒ 9 项逐项回到起播前那一档（＋ 三档 α）
    //
    //  ⚠️ **必须打两次、且落在两个不同时刻** —— 这条 clip 自己有两条收口点会把「脏」抹掉：
    //    · **0.55**（`HandToBoardDone` 那条 AnimationEvent）clip 自己就 `SetDissolveAmount(0)` +
    //      `RestoreOriginalMaterial()` ⇒ 0.55 之后「3D 溶解材质 / `_DissolveAmount`」这两项**本来就不脏**；
    //    · **0.55** 同时才把 `Trail` 打开 ⇒ 0.55 之前 `Trail` **本来就不脏**。
    //  ⇒ 0.50 那一刀验溶解那两项（Trail 尚未打开）、0.56 那一刀验 Trail（溶解已被 clip 自己还原）。
    //    只打一刀的话，必然有 2 项是「空转绿」。
    // ==============================================================================================
    static void RunInterruptedKill()
    {
        KillAt("K5 ", 0.50f, expectDissolve: true,  expectTrail: false);
        KillAt("K6 ", 0.56f, expectDissolve: false, expectTrail: true);
    }

    /// <param name="expectDissolve">这一刀处 3D 溶解材质**应当还在**（&lt; 0.55）；false ⇒ 应当已被 clip 自己还原。</param>
    /// <param name="expectTrail">这一刀处 `Trail` **应当已打开**（&gt; 0.55）。</param>
    static void KillAt(string tag, float seconds, bool expectDissolve, bool expectTrail)
    {
        var root = new GameObject("B123C3_Kill_" + tag.Trim());
        try
        {
            var cv = NewCard(root.transform, "b123c3_kill" + tag.Trim());
            if (cv == null) return;
            MountFx(cv);
            var body = cv.transform.Find("body3D");
            var be = cv.transform.Find("Board Elements");
            Check(body != null, tag + "K0 前提：`body3D` 建出来了");
            Check(be != null, tag + "K0' 前提：`Board Elements` 建出来了");
            if (body == null || be == null) return;

            var before = Capture(cv, body, be);
            // 起播前那一档（手牌态）就是逐项复位的**目标值** —— 先把它断死
            Check(!before.Body3D, tag + "K1 前提（起播前）：3D 体不可见");
            CheckClose(before.ArtScale, 1f, 1e-4f, tag + "K1 前提（起播前）：立绘缩放 = 1");
            CheckClose(before.FaceZ, 0f, 1e-5f, tag + "K1 前提（起播前）：`2DCard.z` = 0");
            Check(!before.SpawnFx && !before.Trail, tag + "K1 前提（起播前）：两颗粒子都关着");
            Check(!before.OnFrame && !before.OnDissolve, tag + "K1 前提（起播前）：两条溶解材质都没上");

            CardTween.Mode = UpdateType.Manual;
            var tw = CardFeel.HandToBoard(cv);
            _last = tw;
            Check(tw != null, tag + "K2 前提：补间起得来");
            if (tw == null) return;
            AdvanceSeconds(seconds);

            // ---- 反向钳制：这一刻状态必须真的「脏」了，否则下面那批绿就是空转 ----
            var mid = Capture(cv, body, be);
            string at = "（" + seconds.ToString("F2") + " s 处）";
            Check(mid.Body3D, tag + "K3 前提" + at + "：3D 体已打开");
            Check(mid.SpawnFx, tag + "K3' 前提" + at + "：`Spawn Particles` 已打开（0.0167 那一拍）");
            Check(mid.Trail == expectTrail,
                  tag + "K3'' 前提" + at + "：`Trail` " + (expectTrail ? "已打开（0.55 那一拍）" : "还没打开")
                  + "（实得 " + mid.Trail + "）");
            Check(Mathf.Abs(mid.ArtScale - 1f) > 0.01f,
                  tag + "K3''' 前提：立绘缩放已离开 1（实得 " + mid.ArtScale.ToString("F4") + "）");
            Check(Mathf.Abs(mid.FaceZ) > 0.01f,
                  tag + "K3'''' 前提：`2DCard.z` 已离开 0（实得 " + mid.FaceZ.ToString("F4") + "）");
            Check(Mathf.Abs(mid.BodyY - before.BodyY) > 0.01f,
                  tag + "K3''''' 前提：`body3D` 的 y 已离开基准（实得 " + mid.BodyY.ToString("F4")
                  + " vs 基准 " + before.BodyY.ToString("F4") + "）");
            Check(Mathf.Abs(mid.BoardElY - before.BoardElY) > 0.01f,
                  tag + "K3'''''' 前提：`Board Elements` 的 y 已离开基准（实得 " + mid.BoardElY.ToString("F4")
                  + " vs 基准 " + before.BoardElY.ToString("F4") + "）");
            Check(mid.Forced, tag + "K3''''''' 前提：计数器被强制显示着（0.1667 之后）");
            Check(Mathf.Abs(mid.Alpha2D - 1f) > 0.01f,
                  tag + "K3'''''''' 前提：2D 卡面 α 已离开 1（实得 " + mid.Alpha2D.ToString("F4") + "）");
            Check(Mathf.Abs(mid.CounterAlpha - 1f) > 0.01f,
                  tag + "K3''''''''' 前提：计数器 α 已离开 1（实得 " + mid.CounterAlpha.ToString("F4") + "）");
            // 卡框溶解（0.0333 → 0.75）：两刀都该在
            Check(mid.OnFrame,
                  tag + "K4 前提：卡框已换成溶解材质实例（0.0333→0.75 那一段；实得 OnFrameDissolveMaterial="
                  + mid.OnFrame + "）—— 取不到 shader ⇒ 这半边没验到");
            // 3D 卡体溶解（0.1667 → **0.55 clip 自己还原**）：只在 0.55 之前那一刀谈得上
            if (expectDissolve)
                Check(mid.OnDissolve && mid.DissolveAmt > 0.01f,
                      tag + "K4' 前提：3D 卡体已换成溶解材质、`_DissolveAmount` 已离开 0（实得 OnDissolve="
                      + mid.OnDissolve + " / amount=" + mid.DissolveAmt.ToString("F4") + "）");
            else
                Check(!mid.OnDissolve && mid.DissolveAmt <= 1e-4f,
                      tag + "K4' 前提：这一刀已经过了 0.55 ⇒ clip **自己**已把 3D 溶解材质还原（实得 OnDissolve="
                      + mid.OnDissolve + " / amount=" + mid.DissolveAmt.ToString("F4") + "）");

            // ---- 打断 ----
            tw.Kill(false);
            var after = Capture(cv, body, be);
            AssertClean(tag, after, before);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ==============================================================================================
    //  ③ 连续两次 Kill 幂等 + 第二次 clip 再被打断仍回到同一个基准
    // ==============================================================================================
    static void RunDoubleKill()
    {
        var root = new GameObject("B123C3_Kill2");
        try
        {
            var cv = NewCard(root.transform, "b123c3_kill2");
            if (cv == null) return;
            MountFx(cv);
            var body = cv.transform.Find("body3D");
            var be = cv.transform.Find("Board Elements");
            Check(body != null, "D0 前提：`body3D` 建出来了");
            Check(be != null, "D0' 前提：`Board Elements` 建出来了");
            if (body == null || be == null) return;
            var before = Capture(cv, body, be);

            CardTween.Mode = UpdateType.Manual;
            var tw1 = CardFeel.HandToBoard(cv);
            _last = tw1;
            Check(tw1 != null, "D1 前提：第一条补间起得来");
            if (tw1 == null) return;
            AdvanceSeconds(0.4f);
            tw1.Kill(false);
            var after1 = Capture(cv, body, be);
            // 第二条：对**已不活跃**的补间再 `Kill` 一次 —— DOTween 自己在 `Kill` 开头就 `!t.active` 早退，
            // 本条验的是「它确实没把事情搅乱」（不抛异常、状态一个字段都不动）。
            tw1.Kill(false);
            var after2 = Capture(cv, body, be);
            Check(SameSnap(after1, after2),
                  "D2 **连续两次 `Kill` 幂等**：第二次不改变任何残留项（3D 体 / 立绘缩放 / z / 两级 y / 粒子 / 两条溶解 / α）"
                  + "｜🧨 若复位逻辑挂在会重复触发的地方并累积 ⇒ 红");

            // 再起一条 clip、再打断一次 ⇒ 必须回到**同一个**起播前基准（不累积、不漂）
            var tw2 = CardFeel.HandToBoard(cv);
            _last = tw2;
            Check(tw2 != null, "D3 前提：第二条 `HandToBoard` 起得来（上一次 kill 落在 0.55 之前，`Face` 仍 = Hand）");
            if (tw2 == null) return;
            AdvanceSeconds(0.5f);
            tw2.Kill(false);
            var after3 = Capture(cv, body, be);
            AssertClean("D4 ", after3, before);
            CheckClose(after3.BodyY, before.BodyY, 1e-4f,
                       "D5 两次打断后 `body3D` 的 y 仍 = **同一个**起播前基准（不累积漂移）");
            CheckClose(after3.BoardElY, before.BoardElY, 1e-4f,
                       "D5' 两次打断后 `Board Elements` 的 y 仍 = **同一个**起播前基准");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ==============================================================================================
    //  夹具 / 工具
    // ==============================================================================================

    /// <summary>建一张手牌族卡，并**借场上形态把 3D 体建出来**再退回手牌（`DissolveSupported` /
    /// `body3D` 都要求体已存在；同 `CardBaseDemo` 的 B05 探针做法）。</summary>
    static CardView NewCard(Transform root, string name)
    {
        var cv = CardView.Create(root, CardData.Placeholder(4), name, CardFace.Hand);
        Check(cv != null, "P0 前提（" + name + "）：手牌族那张建得出来");
        if (cv == null) return null;
        cv.SetFace(CardFace.Board);
        cv.SetFace(CardFace.Hand);
        Check(cv.Face == CardFace.Hand, "P1 前提（" + name + "）：退回手牌形态（`HandToBoard` 只对非场上形态生效）");
        return cv;
    }

    /// <summary>把一对假的 `Spawn Particles` / `Trail` 挂上去（真件由驱动层挂，自检里自己造）。
    /// ⚠️ 不挂 ⇒ 那两项断言永远读到 `false` ⇒ 空转绿。</summary>
    static void MountFx(CardView cv)
    {
        var fx = new GameObject("B123C3_SpawnParticles");
        var trail = new GameObject("Trail");
        trail.transform.SetParent(fx.transform, false);
        bool ok = cv.MountSpawnParticles(fx);
        Check(ok && cv.SpawnParticlesMounted, "P2 前提：`Spawn Particles` / `Trail` 挂上了（否则两颗粒子那两项空转）");
    }

    /// <summary>复位断言：**9 项**（逐项一格）＋ 三档 α（无条件复位那一批）。</summary>
    static void AssertClean(string tag, Snap got, Snap want)
    {
        Check(!got.Body3D, tag + "① 3D 卡体**不可见**（回到手牌态）｜🧨 删 `SetBody3DVisible(false)` ⇒ 红");
        Check(!got.OnFrame, tag + "② 卡框**不在**溶解材质实例上（`EndFrameDissolve`）｜🧨 删 ⇒ 红");
        Check(got.FrameAmt < 0f, tag + "②' 卡框 `_DissolveAmount` = −1（实例已销毁/复位；实得 "
                                     + got.FrameAmt.ToString("F2") + "）");
        Check(!got.OnDissolve, tag + "③ 3D 卡体**不在**溶解材质上（`RestoreOriginalMaterial`）｜🧨 删 ⇒ 红");
        Check(got.DissolveAmt <= 1e-4f, tag + "④ `_DissolveAmount` 已归 0（实得 "
                                          + got.DissolveAmt.ToString("F4") + "）｜🧨 删 `SetDissolveAmount(0f)` ⇒ 红");
        CheckClose(got.ArtScale, 1f, 1e-4f, tag + "⑤ 立绘缩放 = 1（`SetArtLocalScale(1f)`）｜🧨 删 ⇒ 红");
        CheckClose(got.FaceZ, 0f, 1e-5f, tag + "⑥ `2DCard.z` = 0（`SetFaceRootLocalZ(0f)`）｜🧨 删 ⇒ 红");
        CheckClose(got.BodyY, want.BodyY, 1e-4f, tag + "⑦ `body3D` 的 y 回到起播前基准｜🧨 删 ⇒ 红");
        CheckClose(got.BoardElY, want.BoardElY, 1e-4f, tag + "⑧ `Board Elements` 的 y 回到起播前基准｜🧨 删 ⇒ 红");
        Check(!got.SpawnFx, tag + "⑨a `Spawn Particles` 关掉｜🧨 删 `SetSpawnParticlesActive(false)` ⇒ 红");
        Check(!got.Trail, tag + "⑨b `Trail` 关掉｜🧨 删 `SetTrailActive(false)` ⇒ 红");
        // 三档 α（`OnKill` 里无条件复位，正常跑完与打断都走）
        CheckClose(got.Alpha2D, 1f, 1e-3f, tag + "α1 2D 卡面 α = 1");
        CheckClose(got.CounterAlpha, 1f, 1e-3f, tag + "α2 计数器 α = 1");
        Check(!got.Forced, tag + "α3 不再强制显示计数器");
    }

    /// <summary>卡上「被这条 clip 改过的那些项」的快照。</summary>
    struct Snap
    {
        public bool Body3D, SpawnFx, Trail, Forced, OnFrame, OnDissolve;
        public float ArtScale, FaceZ, BodyY, BoardElY, Alpha2D, CounterAlpha, FrameAmt, DissolveAmt;
    }

    static Snap Capture(CardView cv, Transform body, Transform boardEl)
    {
        var s = new Snap();
        s.Body3D = cv.Body3DVisible;
        s.SpawnFx = cv.SpawnParticlesActive;
        s.Trail = cv.TrailActive;
        s.Forced = cv.CountersForcedVisible;
        s.OnFrame = cv.OnFrameDissolveMaterial;
        s.OnDissolve = cv.OnDissolveMaterial;
        s.ArtScale = cv.ArtLocalScale;
        s.FaceZ = cv.FaceRootLocalZ;
        s.BodyY = body != null ? body.localPosition.y : float.NaN;
        s.BoardElY = boardEl != null ? boardEl.localPosition.y : float.NaN;
        s.Alpha2D = cv.Alpha2D;
        s.CounterAlpha = cv.CounterAlpha;
        s.FrameAmt = cv.FrameDissolveAmount;
        s.DissolveAmt = cv.DissolveAmount;
        return s;
    }

    static bool SameSnap(Snap a, Snap b)
    {
        const float eps = 1e-4f;
        return a.Body3D == b.Body3D && a.SpawnFx == b.SpawnFx && a.Trail == b.Trail
            && a.Forced == b.Forced && a.OnFrame == b.OnFrame && a.OnDissolve == b.OnDissolve
            && Mathf.Abs(a.ArtScale - b.ArtScale) <= eps
            && Mathf.Abs(a.FaceZ - b.FaceZ) <= eps
            && Mathf.Abs(a.BodyY - b.BodyY) <= eps
            && Mathf.Abs(a.BoardElY - b.BoardElY) <= eps
            && Mathf.Abs(a.Alpha2D - b.Alpha2D) <= eps
            && Mathf.Abs(a.CounterAlpha - b.CounterAlpha) <= eps
            && Mathf.Abs(a.FrameAmt - b.FrameAmt) <= eps
            && Mathf.Abs(a.DissolveAmt - b.DissolveAmt) <= eps;
    }

    /// <summary>按 60 fps 逐帧推进（⛔ 不用一步大跳：`InsertCallback` 那几条要靠「越过关键帧」
    /// 才会触发，一步大跳在批处理里不可靠）。</summary>
    static void AdvanceSeconds(float seconds)
    {
        int steps = Mathf.CeilToInt(seconds * 60f);
        for (int i = 0; i < steps; i++) CardTween.Advance(1f / 60f);
    }
}
