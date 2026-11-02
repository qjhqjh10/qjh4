// B117_G10_11_Assert.cs — 执行代理 `B117-G10-11`（战斗域第 11 部分「动画」）的断言
//
// ============================ 它盯的是什么 ============================
// 本部分 7 个文件里**此前一条断言都没有**的三块（2026-11-03 现读 `Editor/*.cs` 全库 grep 确认）：
//   · `Battle/AnimGate.cs` —— 原版 `CardScript.CanContinueAnim()` 的共用闸（null / 隐藏两档）
//   · `Battle/CardAnimOrder.cs` —— `easeCurve` / 偏移曲线的求值口（`EaseAt` / `EaseAtRaw`），
//     以及一拍「要不要等」`Waits` 与时长 `Duration`
//   · `Battle/Card3DAnimationController.cs` —— 空动画 / 起播 / 收尾 / `safeResetOnEnd` 三连
//   `FlyingCardController` 与 `PopNumber` 只在 `Editor/BattleScene.cs` 里被**间接**覆盖
//   （生命期时长、两档字号），**曲线形状本身没人量过** ⇒ 本文件补上。
//
// ============================ 判据（⛔ 别在这里抄第二份）============================
//   · 反编译原文（第一权威，本会话逐句读过）：
//       `d:/2/tools/decomp_full/CardScript__CanContinueAnim.c`（`this != null && gameObject != null && activeSelf`）
//       `d:/2/tools/decomp_full/FlyingCardController__Update.c`（Flying 态：`home.y + initialHeightOffset + flyingHeight × curve(t)`）
//       `d:/2/tools/decomp_full/FlyingCardController__GoToTargetPosition.c`（**写位置之前**那个位置与 target 比 ⇒ 到位晚一帧）
//       `d:/2/tools/decomp_full/CardAnimController__Update.c`（`Evaluate((t−ts)/tm)` → **输出** clamp 到 [0,1]）
//       `d:/2/tools/decomp_full/Card3DAnimationController__DoAnim.c`（`clip == null` ⇒ 只调回调、返回 false）
//       `d:/2/tools/decomp_full/Card3DAnimationController__AnimFinished.c`（`safeResetOnEnd` 三连复位）
//   · 序列化值（`assets_full`）：
//       `MonoBehaviour_2087276408211151808.json` —— `yMovementCurve` 四键
//       （`t=0 v=0.0002289 slope 5.856887` · `0.25 → 1.0 slope 0` · `0.75 → −1.0 slope 0.02292` · `1 → 0 slope 5.561683`）、
//       `initialHeightOffset 0.4` · `flyingHeight 0.1` · `animationSpeed 0.35`
//       `AnimationClip_-3234148433819516763.json`（`InBattleDamageCounter Variation 1`）——
//       `DamageText.m_fontColor.a` 的两段关键帧**切线都是 0**（⇒ Hermite = 平滑阶跃 `3t²−2t³`，**不是直线**）
//
// ============================ 怎么用 ============================
//   `B117G1011Assert.Run();`   （静态类 + 无参 `Run()`；宿主由调度台接，本文件不改任何 `Editor/*Scene.cs`）
//   ⚠️ 本文件**不建持久资产**：临时 GameObject 在 `finally` 里 `DestroyImmediate`，
//      并把 `CardTween.Mode` 原样还原（否则会把后面所有宿主的补间一起冻住）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A2  ← 把 `AnimGate.CanContinue` 写成 `v != null`（丢掉 `activeSelf`）：同一张卡隐藏之后它仍返回 true
//            ⇒ A2 红、A3 红（本件**同一条判据投两档**：可见=true / 隐藏=false，一个写死的返回值过不了）。
//   · B2  ← 把 `yMovementCurve` 换成 `AnimationCurve.Linear(0,0,1,1)`（或任何线性近似）：
//            `Evaluate(0.125)` 从 **0.6831** 掉到 **0.5001** ⇒ B2 红（这条就是「原版不是线性」的判别式）。
//   · D2  ← 把飘字那两条透明度补间改回 `Ease.Linear`（2026-11-03 之前的写法）：
//            25% 进度处 α 从 **0.15625**（平滑阶跃）变成 **0.25** ⇒ D2 同时校验两个值（一个像、一个不像）。
//   · E1  ← 把 `EaseAt` 写成「直接返回 p」（不读曲线）⇒ `[0,1,1,1]` 那条常数曲线会算成 0.3 ⇒ E1 红。
//   · C1  ← 把空动画那一支改成「也去起播」（不 `return false`）⇒ `PlayedCount` 从 0 变 1 ⇒ C1 红。
//   · C3  ← 去掉 `safeResetOnEnd` 的三连复位 ⇒ 卡停在被挪走的位置 ⇒ C3 红。
using DG.Tweening;
using UnityEngine;
using CardPresentation;

public static class B117G1011Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    const string Tag = "B117G1011";

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F5") + " ± " + tol.ToString("F5")
                  + "；实得 " + got.ToString("F5") + "）");
    }

    // ==========================================================================================
    //  原版字面量（⛔ 别读我们自己的常量反推 —— 那就是拿实现证明实现）
    // ==========================================================================================
    /// <summary>`yMovementCurve` 在 t=0.125 处的 Unity Hermite 值：段 [0, 0.25]、
    /// `v0 = 0.0002288818359375 slope 5.856887340545654` · `v1 = 1.0 slope 0`，`u = 0.5`、`dt = 0.25`
    /// ⇒ `0.5×v0 + 0.125×(5.856887×0.25) + 0.5×v1 + (−0.125×0) = 0.68314`。
    /// ⚠️ **线性近似会得 0.5001** ⇒ 差 0.183，这就是 B2 的判别式。</summary>
    const float CurveAt0125 = 0.683140f;
    const float LinearAt0125 = 0.500114f;

    public static void Run()
    {
        int pass0 = _pass, fail0 = _fail;
        SectionA_AnimGate();
        SectionB_FlyingCardController();
        SectionC_Card3DAnimationController();
        SectionD_PopNumber();
        SectionE_CardAnimOrder();
        Debug.Log(Tag + " —— 合计 " + (Pass - pass0) + " 通过 / " + (Fail - fail0) + " 失败");
    }

    // ============================================================ A：AnimGate（原版 `CanContinueAnim`）
    static void SectionA_AnimGate()
    {
        var go = new GameObject("B117G1011_Gate");
        try
        {
            // A1（前提）活着的对象 ⇒ 演
            Check(AnimGate.CanContinue(go.transform),
                  "A1 活着的组件 ⇒ `CanContinue` = true");

            // A2 ★ 灭自证：同一个对象隐藏之后必须变 false —— 而 `!= null` 仍为 true
            go.SetActive(false);
            bool stillNonNull = go.transform != null;
            bool can = AnimGate.CanContinue(go.transform);
            Check(stillNonNull && !can,
                  "A2 ★ 同一个对象 `SetActive(false)` ⇒ `CanContinue` = false"
                + "（而 `!= null` 仍是 true ⇒ 光写 null 检查过不了这条）"
                + "｜🧨 判据 = `CardScript__CanContinueAnim.c` 的 `gameObject.activeSelf`");

            // A3 诊断口要把「销毁 / 隐藏」两档分开
            bool alive, active;
            AnimGate.Probe(go.transform, out alive, out active);
            Check(alive && !active, "A3 `Probe` 分得开两档：alive=true / active=false（隐藏）");
            AnimGate.Probe(null, out alive, out active);
            Check(!alive && !active, "A4 `Probe(null)` = (false, false)（销毁那一档）");

            // A5 CardView 那一档：null ⇒ false（原版 `this != null`）
            Check(!AnimGate.CanContinue((CardView)null), "A5 `CanContinue((CardView)null)` = false");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ============================================================ B：FlyingCardController（四态机 + 往复曲线）
    static void SectionB_FlyingCardController()
    {
        // B1 曲线本身（静态口，不需要实例）
        Near(FlyingCardController.EvaluateCurve(0.25f), 1.0f, 1e-3f,
             "B1 `yMovementCurve(0.25)` = 最高点 **+1.0**");
        Near(FlyingCardController.EvaluateCurve(0.75f), -1.0f, 1e-3f,
             "B2 `yMovementCurve(0.75)` = 最低点 **−1.0**");
        Near(FlyingCardController.HeightOffsetAt(0.25f), 0.5f, 1e-3f,
             "B3 `HeightOffsetAt(0.25)` = 0.4 + 0.1×1 = **0.5** 世界单位");
        Near(FlyingCardController.HeightOffsetAt(0.75f), 0.3f, 1e-3f,
             "B4 `HeightOffsetAt(0.75)` = 0.4 + 0.1×(−1) = **0.3**");
        Near(FlyingCardController.HeightOffsetAt(1.0f), 0.4f, 1e-3f,
             "B5 `HeightOffsetAt(1)` = **0.4**（末键 0 ⇒ 回到初始抬升；`postWrap = Once` 之后停在这儿）");
        Near(FlyingCardController.HeightOffsetAt(3.0f), 0.4f, 1e-3f,
             "B6 `HeightOffsetAt(3)` 仍 = **0.4** —— `m_PostInfinity = 1（Once）`，⛔ 不是永久摆动");
        // ★ 灭自证：原版不是线性 —— t=0.125 处 Hermite 0.6831 vs 线性 0.5001
        {
            float got = FlyingCardController.EvaluateCurve(0.125f);
            Check(Mathf.Abs(got - CurveAt0125) < 0.01f && Mathf.Abs(got - LinearAt0125) > 0.1f,
                  "B7 ★ 曲线是 **Hermite**（t=0.125 实得 " + got.ToString("F4")
                + "；期望 " + CurveAt0125.ToString("F4") + "，线性近似会是 " + LinearAt0125.ToString("F4") + "）"
                + "｜🧨 换成 `AnimationCurve.Linear` ⇒ 红");
        }

        var go = new GameObject("B117G1011_Fly");
        go.transform.localPosition = new Vector3(1f, 2f, 3f);
        try
        {
            var f = new FlyingCardController(go.transform);
            Check(f.State == FlyingCardController.States.Landed && !f.IsFlying,
                  "B8 出厂 `Landed`、`IsFlying` = false");

            // B9（生产口径）`Toggle(true)` 默认 0.4 ⇒ 走**过渡态**，不是瞬切
            f.Toggle(true);
            Check(f.State == FlyingCardController.States.InTransitionToFly,
                  "B9 ★ `Toggle(true)` 默认 0.4 s ⇒ `InTransitionToFly`"
                + "（判据 = `BattleCardUI.ToggleFlying` 传 `DAT_1834b3158 = 0.4`）");

            // B10 过渡推进：0.4 s 那一帧**还没到位**（原版按写之前的位姿判到达 ⇒ 晚一帧）
            f.Update(0.4f);
            Check(f.State == FlyingCardController.States.InTransitionToFly,
                  "B10 ★ 恰好 0.4 s 时仍在 `InTransitionToFly`（原版 `GoToTargetPosition` 比的是**写之前**那个位置）");
            f.Update(0.01f);
            Check(f.State == FlyingCardController.States.Flying && f.IsFlying,
                  "B11 再过一帧 ⇒ `Flying`");
            Near(go.transform.localPosition.y, 2f + 0.4f, 0.02f,
                 "B12 飞起来的高度 ≈ home.y + initialHeightOffset(0.4)");

            // B13 飞回去 ⇒ 精确回 home，且回调 false
            bool? cb = null;
            f.OnFlyingToggleFinish = v => cb = v;
            f.Toggle(false, 0f);
            Check(f.State == FlyingCardController.States.Landed
                  && (go.transform.localPosition - new Vector3(1f, 2f, 3f)).sqrMagnitude < 1e-8f
                  && cb == false,
                  "B13 ★ `Toggle(false, 0)` ⇒ 立刻 `Landed` + `localPosition` 复位 + 回调 false");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ============================================================ C：Card3DAnimationController
    static void SectionC_Card3DAnimationController()
    {
        var go = new GameObject("B117G1011_3D");
        try
        {
            var ctl = new Card3DAnimationController(go.transform);
            Check(!ctl.Enabled, "C0 出厂 `enabled = false`（原版 `Start()` 第一句）");

            // C1 ★ 灭自证：空动画只回调、**不起播**
            int calls = 0;
            bool started = ctl.DoAnim(default(CardAnimPlayback), false, () => calls++);
            Check(!started && calls == 1 && ctl.PlayedCount == 0 && ctl.EmptyCount == 1,
                  "C1 ★ 空动画（既无 clip 又无 prefab）⇒ 返回 false + 回调 1 次 + **一次都没起播**"
                + "（判据 = `Card3DAnimationController__DoAnim.c` 第一段）"
                + "｜🧨 让它照常起播 ⇒ `PlayedCount` 变 1 ⇒ 红");

            // C2 起播 → 到期收尾（批处理 `targetFrameRate = -1` ⇒ 按 60 fps 的一帧裕量）
            int done = 0;
            bool ok = ctl.DoAnim(new CardAnimPlayback { Prefab = "Probe", Duration = 0.2f, Label = "probe" },
                                 false, () => done++);
            Check(ok && ctl.Enabled && ctl.PlayedCount == 1,
                  "C2 有内容 ⇒ 起播（`enabled = true`、`PlayedCount = 1`）");
            ctl.Update(0.25f);
            Check(done == 1 && ctl.FinishedCount == 1 && !ctl.Enabled,
                  "C3 ★ 超过时长（0.2 s + 一帧裕量）⇒ 收尾 1 次、`enabled = false`"
                + "（⚠️ 批处理 `Application.targetFrameRate = -1` ⇒ 裕量按 60 fps 算；"
                + "原版那一格取不到 ⇒ 见 `Card3DAnimationController.cs` 的标记）");

            // C4 ★ `safeResetOnEnd` 三连复位：先把卡挪走，收尾必须回到构造那一刻的位姿
            var go2 = new GameObject("B117G1011_3D2");
            try
            {
                go2.transform.position = new Vector3(1f, 2f, 3f);
                go2.transform.localScale = Vector3.one * 0.7f;
                var ctl2 = new Card3DAnimationController(go2.transform);
                go2.transform.position = new Vector3(9f, 9f, 9f);
                go2.transform.localScale = Vector3.one * 2f;
                ctl2.DoAnim(new CardAnimPlayback { Prefab = "Probe", Duration = 0.1f, Label = "probe2" }, false, null);
                ctl2.Update(0.25f);
                Check(ctl2.FinishedCount == 1
                      && (go2.transform.position - new Vector3(1f, 2f, 3f)).sqrMagnitude < 1e-8f
                      && Mathf.Abs(go2.transform.localScale.x - 0.7f) < 1e-5f,
                      "C4 ★ `safeResetOnEnd`（默认 true）⇒ 收尾把 position / rotation / localScale 三连复位"
                    + "（判据 = `Card3DAnimationController__AnimFinished.c`；🧨 去掉复位 ⇒ 卡停在被挪走处 ⇒ 红）");
            }
            finally { Object.DestroyImmediate(go2); }
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ============================================================ D：PopNumber（两档 + clip 曲线形状）
    static void SectionD_PopNumber()
    {
        // D1 两档有效字号比（原版字面量算出来的，⛔ 不是我们挑的基准）
        Near(PopNumber.EffectiveScaleRatio, 1.21f, 0.01f,
             "D1 Damage : Heal 有效字号比 = 0.847 / 0.700 = **1.21**");

        var prevMode = CardTween.Mode;
        CardTween.Mode = UpdateType.Manual;
        var parent = new GameObject("B117G1011_Pop");
        try
        {
            var lbl = PopNumber.PopDamage(parent.transform, Vector3.zero, 3);
            var tw = PopNumber.LastPopTween;
            Check(lbl != null && tw != null, "D2（前提）`PopDamage` 建出了飘字与补间");
            if (lbl != null && tw != null)
            {
                Near(Dur(tw), CardFeel.PopOut, 1e-3f,
                     "D3 整条序列 = **1.8333 s**（clip 的 `m_FloatCurves` 末键时刻）");

                // ★ 灭自证：clip 的两端切线都是 0 ⇒ 原版是平滑阶跃，不是线性
                CardTween.Advance(CardFeel.PopIn * 0.25f);
                float a = lbl.color.a;
                float smooth = 3f * 0.25f * 0.25f - 2f * 0.25f * 0.25f * 0.25f;   // 3u²−2u³ = 0.15625
                Check(Mathf.Abs(a - smooth) < 0.02f && Mathf.Abs(a - 0.25f) > 0.04f,
                      "D4 ★ 25% 进度处 α = " + a.ToString("F4")
                    + "（原版 Hermite 平滑阶跃 = " + smooth.ToString("F5")
                    + "；`Ease.Linear` 会得 0.25）"
                    + "｜🧨 改回 `Ease.Linear` ⇒ 红");
                // 淡出段也一样（1.6667 → 1.8333，两端切线 0）
                DG.Tweening.TweenExtensions.Kill(tw, false);
            }

            // D5 治疗档的字段与生命周期（判据在 `CardView.ShowHeal`，这里只核公开常量）
            Near(PopNumber.HealCapWorld / PopNumber.DamageCapWorld, 0.700f / 0.847f, 1e-4f,
                 "D5 治疗 / 伤害的**绝对**墨高比 = HealEm / DamageEm = 0.700 / 0.847");
        }
        finally
        {
            CardTween.Mode = prevMode;
            Object.DestroyImmediate(parent);
        }
    }

    // ============================================================ E：CardAnimOrder（曲线求值 + 一拍语义）
    static void SectionE_CardAnimOrder()
    {
        // E1 ★ 灭自证：求值必须**读曲线**，不能直接返回进度
        var linear = new float[] { 0f, 0f, 1f, 1f };
        var constant = new float[] { 0f, 1f, 1f, 1f };
        Near(CardAnimOrder.EaseAt(linear, 0.3f), 0.3f, 1e-5f,
             "E1 `(0,0)→(1,1)` 的曲线 ⇒ 逐值等于进度（与分段线性同形）");
        Near(CardAnimOrder.EaseAt(constant, 0.3f), 1f, 1e-5f,
             "E2 ★ 常数曲线 `[0,1,1,1]` ⇒ 任何进度都得 **1**"
           + "｜🧨 把 `EaseAt` 写成 `return p` ⇒ 得 0.3 ⇒ 红");
        Near(CardAnimOrder.EaseAt(null, 0.4f), 0.4f, 1e-5f,
             "E3 曲线为空 ⇒ 线性（原版默认曲线 `(0,0)→(1,1)`）");
        Near(CardAnimOrder.EaseAt(linear, -1f), 0f, 1e-6f, "E4 进度 < 0 ⇒ clamp 到 0");
        Near(CardAnimOrder.EaseAt(linear, 2f), 1f, 1e-6f,
             "E5 进度 > 1 ⇒ clamp 到 **1**（判据 = `CardAnimController__Update.c` 对 `Evaluate` 输出也 clamp）");

        // E6 偏移曲线：空曲线 = **0**（不是线性恒等），且**不** clamp 到 [0,1]
        Near(CardAnimOrder.EaseAtRaw(null, 0.3f), 0f, 1e-6f,
             "E6 空偏移曲线 ⇒ **0**（原版默认偏移曲线 `(0,0)→(1,0)` = 不偏移；与 E3 是两条不同的默认）");
        var neg = new float[] { 0f, -0.5f, 1f, -0.5f };
        Near(CardAnimOrder.EaseAtRaw(neg, 0.3f), -0.5f, 1e-5f,
             "E7 偏移曲线**不** clamp（允许负值 —— 原版 `Update` 那三句 `Evaluate` 没有 clamp）");

        // E8 一拍的时长与「要不要等」
        var beat = new CardAnimBeat { vd = 0.1f, ts = 0.2f, tm = 0.3f, te = 0.4f, w = 1 };
        Near(beat.Duration, 1.0f, 1e-6f,
             "E8 一拍时长 = `vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`（不含 `startDelay`）");
        Check(beat.Waits, "E9 `waitAnimToApplyEffect != 0` ⇒ 这一拍要等（判据 = 读**这一拍自己的** `w`，⛔ 不是「只有最后一拍」）");
        beat.w = 0;
        Check(!beat.Waits, "E10 `w = 0` ⇒ 不等（下一拍在同一时刻起）");

        // E11 偏移求值：`ao = 0` ⇒ 零；`ao != 0` 且曲线为空 ⇒ 也是零
        var zero = CardAnimOrder.OffsetAt(0, 0, neg, neg, neg, 0.5f, 0.5f, Vector3.one);
        Check(zero == Vector3.zero, "E11 `applyMovementOffsets = 0` ⇒ 偏移恒零");
        var withAo = CardAnimOrder.OffsetAt(1, 0, neg, neg, neg, 0.5f, 0.5f, new Vector3(2f, 3f, 4f));
        Check(Mathf.Abs(withAo.x + 1f) < 1e-5f && Mathf.Abs(withAo.y + 1.5f) < 1e-5f
              && Mathf.Abs(withAo.z + 2f) < 1e-5f,
              "E12 偏移 = 曲线值 × 随机幅度（−0.5 × (2,3,4) = (−1,−1.5,−2)）");

        // E13 数据表在不在（不静默：载不到时 `LastError` 有话说）
        Check(CardAnimOrder.Loaded && CardAnimOrder.Count > 0,
              "E13 `Resources/card_anim_order.json` 载入 **" + CardAnimOrder.Count + " 张卡**"
            + "（Loaded = " + CardAnimOrder.Loaded + "；载不到时 `LastError` = "
            + (CardAnimOrder.LastError ?? "<null>") + "）");
        Check(CardAnimClips.Loaded && CardAnimClips.TableCount > 0,
              "E14 `Resources/cardanim_clips.json` 载入 **" + CardAnimClips.TableCount + " 行**"
            + "（Loaded = " + CardAnimClips.Loaded + " —— 载不到会 `LogError` 出声并显式降级）");

        // E15 `AnimAnchors` 的档号必须与原版 `PositionOption` 逐一相同（36 档里抽 10 档当锚）
        Check((int)PositionOption.self == 0 && (int)PositionOption.cardTarget == 1
              && (int)PositionOption.abilityTarget == 2 && (int)PositionOption.friendlyHero == 3
              && (int)PositionOption.enemyHero == 4 && (int)PositionOption.boardCenter == 5
              && (int)PositionOption.friendlyDeck == 50 && (int)PositionOption.enemyDeck == 55
              && (int)PositionOption.friendlyHandWorld == 150 && (int)PositionOption.enemyHandWorld == 155,
              "E15 `PositionOption` 档号与原版 `dump.cs` 逐一相同（0/1/2/3/4/5/50/55/150/155）");

        // E16 取不到锚点时**出声**、返回 null（⛔ 不静默、⛔ 不猜一个默认点）
        // 🔴 **2026-11-02（`B117-FIXD`）就地订正（铁律 5）**：这一段跑在 `BattleScene.Run` 里，那一刻
        //   `BattleDriver` 已经把 `AnimAnchors.BoardCenterOf` 挂上了（`Battle/BattleDriver.cs:3760`）+ 返回非 null
        //   ⇒ 原来那条「未挂」的前提**在本宿主里不成立**，`Resolve` 正常返回、`LastMissing` 恒 null（E16 红）。
        //   做法：**临时摘掉**那个钩子造出「未挂」这一态，跑完**原样装回去**（⛔ 别改成断别的档 —— 这里要验的就是
        //   `boardCenter` 这条 `Missing(...)` 支）。
        var keepBoardCenter = AnimAnchors.BoardCenterOf;
        Transform tr;
        try
        {
            AnimAnchors.BoardCenterOf = null;
            AnimAnchors.ResetCounters();
            tr = AnimAnchors.Resolve(PositionOption.boardCenter,
                                     new AnimAnchors.Ctx { actorSeat = 0, mySeat = 0 });
        }
        finally { AnimAnchors.BoardCenterOf = keepBoardCenter; }
        Check(tr == null && AnimAnchors.LastMissing != null,
              "E16 未挂 `BoardCenterOf` ⇒ 返回 null **并**记下原因（`LastMissing` = "
            + (AnimAnchors.LastMissing ?? "<null>") + "）");
    }

    /// <summary>`Tween.Duration()`（不含 `Delay`）—— 与其他断言件同一口径。</summary>
    static float Dur(DG.Tweening.Tween t)
    {
        if (t == null) return -1f;
        return t.Duration(false);
    }
}
