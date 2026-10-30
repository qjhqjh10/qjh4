// B104_CardFeel_Assert.cs — 第一百零四会话 · 执行代理 `B104_CardFeel`
//
// ============================ 它盯的是什么 ============================
//  ① **屏震 `rawSignal` 的采样算式**（R9）—— 每轴每带 `u = Frequency × T`；
//     `Constant == 1` ⇒ `A×0.5×sin(2πu)`、`Constant == 0` ⇒ `A×(PerlinNoise(u,0) − 0.5)`；
//     **三条带相加**；`T = t × frequencyGain + phase`（三条带**共享** T）。⛔ `Constant` 不是「常数」。
//  ② **`ShakeCamera` 的 `delay` 语义** —— 原版 `CameraShakerManager.DoShake` 协程在 `delay` 期间是
//     `WaitForSeconds`（相机**不动**），只有 `delay == 0` 才在同一拍生成冲量。
//     我们 2026-10-31 之前把 `apply(0)` 无条件先施加了一次 ⇒ 109 个带 delay 的模块在等待期里镜头是**歪的**。
//  ③ **相机 + HUD 根同向 = 「屏幕不动」** —— 原版 13 个战场 `useCanvasShake = 0` + HUD 由独立
//     `UI Camera` 渲染（无 Brain / 无 listener）⇒ 只有战场相机震。我们单相机 ⇒ 用「相机与 HUD 根做
//     **同一个刚体变换**」等价复刻。本件钉的是一条**不依赖实现**的不变量：HUD 在相机局部坐标里不变。
//
// ============================ 判据出处（⛔ 别在这儿抄第二份）============================
//  · 算式（机器码级）→ `资料/普查产出_1023b/R9_rawSignal_算式.md`
//    （`GameAssembly.dll`：`NoiseParams::GetValueAt` RVA 0xA10610 · `LegacySignalSource` ctor 0xA2B5E0 /
//      `GetSignal` 0xA2B350 · `GetDecayedSignal` RVA 0xA2A060）
//  · `delay` → `decomp_full/CameraShakerManager._DoShake_d__33__MoveNext.c:236-270`
//    （`delay < 0` 夹到 0；`delay == 0` 直接生成，否则 `WaitForSeconds(delay)`）
//  · HUD 不震 → `assets_full/bundle_scenes_scenes_battlearena*/MonoBehaviour/MonoBehaviour_45xx.json`
//    的 `CameraShakerManager`：13/13 `useCanvasShake = 0` · `canvas = null` · `impulseListener` 非空
//    （主菜单那份 `= 1`，不适用战斗）；battlearena1 场景件：`Cinemachine Vcam`（`ImpulseSource` + `ImpulseListener`）
//    → `BoardCamera` 的 `CinemachineBrain`；`UI Camera` 无 Brain、两块 `ScreenSpaceCamera` 画布指向它。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  需要 DOTween（`ShakeCamera` 会建 Sequence）⇒ 推荐接在 `Editor/BattleScene.cs` 的 `Run()` 里：
//      B104_CardFeel_Assert.Run(); _pass += B104_CardFeel_Assert.Pass; _fail += B104_CardFeel_Assert.Fail;
//  （本件自己 `DestroyImmediate` 掉临时相机 / HUD 根，并把 `CardTween.Mode` 原样还原 ⇒ 不污染后续断言。）
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · B2：采样轴改回 `PerlinNoise(0,u)` ⇒ 红。⚠️ **扫 24 个 u 并跳过「两支恰好重合」的点**
//        （Unity 的 Perlin 在个别 u 上 x/y 两支可撞到 <1e-3；单点判会误红，2026-10-31 实测 u=1.23）；
//        判别点 <4 也算红 ⇒ 不会退化成恒真。
//  · B1/B3：`Constant` 不分 sin/Perlin、或三条带不是相加 ⇒ 红。
//  · C1：把 `ShakeNoise01` 改回旧写法（三条全 Perlin / `×2−1` / 轴 `(0, f·t+phase)` / 不分带相位）
//        ⇒ 逐点重新相等 ⇒ **C1 红**（这条是 R9 的总体防线）。
//  · D2：把 `if (delay <= 0f) apply(0f);` 改回无条件 `apply(0f)` ⇒ 等待期里相机已被推歪 ⇒ **D2 红**。
//  · E3：HUD 只做平移不做旋转 ⇒ 相机局部坐标不再守恒（差 ~1e-3 世界单位）⇒ **E3 红**。
//  · E4：HUD 根完全不补偿（退回只震相机）⇒ E3 差一个 |d| ⇒ 红。
using CardPresentation;
using DG.Tweening;
using UnityEngine;

/// <summary>执行代理 `B104_CardFeel`：屏震算式 / delay 语义 / 相机+HUD 同向平移的不变量。</summary>
public static class B104_CardFeel_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B104CF] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B104_CardFeel：屏震 rawSignal 算式 + delay 语义 + 相机/HUD 同向（屏幕不动）");

        SectionA_数据();
        SectionB_算式();
        SectionC_灭自证_旧实现();
        SectionD_Delay语义();
        SectionE_相机HUD同向();

        Debug.Log(_sink.P + "B104CF 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A：数据（json → 运行时）
    static void SectionA_数据()
    {
        var dflt = WarpforgeVFX.WFModuleScreenShake.DefaultSignal;
        Tr(dflt != null, "A0（前提）`shake_presets.json` 读得到默认信号（Resources 在、生成器产物没丢）");
        if (dflt == null) return;

        C(dflt.name, "Warpforge 6D Shake", "A1 默认信号 = 场景 `CinemachineImpulseSource.m_RawSignal` = `Warpforge 6D Shake`");
        Tr(dflt.position != null && dflt.position.Length == 3, "A2 默认信号 PositionNoise 有 3 条带");
        if (dflt.position == null || dflt.position.Length != 3) return;

        // 原版真件的 Y 三带（`Warpforge 6D Shake.json` 的 PositionNoise[0..2].Y）
        Near(dflt.position[0].fy, 1.90f, 1e-4f, "A3 带 0 的 `Frequency`（原版 1.90）");
        Near(dflt.position[0].ay, 0.059f, 1e-6f, "A4 带 0 的 `Amplitude`（原版 0.059）");
        C(dflt.position[0].cy, 1, "A5 带 0 的 `Constant` = **1**（⇒ 正弦；⛔ 不是「常数 = 不带噪声」那个错读法）");
        Near(dflt.position[1].fy, 9.10f, 1e-4f, "A6 带 1 的 `Frequency`（原版 9.10）");
        C(dflt.position[1].cy, 0, "A7 带 1 的 `Constant` = **0**（⇒ Perlin）—— 默认信号是 **1/0/1**，不是三条全 Perlin");
        Near(dflt.position[2].fy, 55.54f, 1e-3f, "A8 带 2 的 `Frequency`（原版 55.54）");
        C(dflt.position[2].cy, 1, "A9 带 2 的 `Constant` = **1**（高频那条也是正弦）");

        // 出厂表（CardFeel 常量）与数据表逐位一致
        var fac = CardFeel.DefaultShakeSignal;
        bool same = fac != null && fac.Count == 3;
        if (same)
            for (int i = 0; i < 3; i++)
            {
                var b = fac[i];
                if (Mathf.Abs(b.hz - dflt.position[i].fy) > 1e-5f
                    || Mathf.Abs(b.amp - dflt.position[i].ay) > 1e-6f
                    || b.constant != dflt.position[i].cy) same = false;
            }
        Tr(same, "A10 数据表（json `Warpforge 6D Shake` Position.Y）与出厂表（`CardFeel.DefaultShakeSignal`）三条带逐位一致"
               + "｜🧨 只改一边（重生成 json / 改 Core 常量）⇒ 红");

        // 4 份信号的 Y 列逐位相同（原版 `…One Axis` / `…No rotation` 只差别的轴 / 角度）
        var sigs = WarpforgeVFX.WFModuleScreenShake.Signals;
        bool ySame = sigs != null && sigs.Count == 4;
        if (ySame)
            foreach (var s in sigs)
            {
                if (s == null || s.position == null || s.position.Length != 3) { ySame = false; break; }
                for (int i = 0; i < 3; i++)
                    if (Mathf.Abs(s.position[i].fy - dflt.position[i].fy) > 1e-5f
                        || Mathf.Abs(s.position[i].ay - dflt.position[i].ay) > 1e-6f
                        || s.position[i].cy != dflt.position[i].cy) { ySame = false; break; }
            }
        Tr(ySame, "A11 4 份 `Warpforge 6D Shake*` 的 **Position.Y 三带逐位相同**（原版实测）⇒ 2D 竖直波形对全部 preset 一致"
                + "｜🧨 数据抽错轴 / 少一份 ⇒ 红");

        // preset → 信号（数据层）
        var hs = WarpforgeVFX.WFModuleScreenShake.SignalFor("Shake Hit Small");
        Tr(hs != null && hs.name == "Warpforge 6D Shake", "A12 无 `rawSignal` 的 preset（`Shake Hit Small`）落到默认信号");
        var land = WarpforgeVFX.WFModuleScreenShake.SignalFor("Shake Land on ground");
        Tr(land != null && land.name == "Warpforge 6D Shake One Axis",
           "A13 数据里带 `rawSignal` 的 preset（`Shake Land on ground`）指向 `…One Axis`（这是**显式档**才用的那张表）");

        // preset 数值（真件）
        var req = WarpforgeVFX.WFModuleScreenShake.Resolve(
            new WarpforgeVFX.WFModuleScreenShake.ShakeEntry { preset = "Shake Hit Small" });
        Near(req.frequency, 0.05f, 1e-6f, "A14 `Shake Hit Small` frequency = 0.05");
        Near(req.attackTime, 0f, 1e-6f, "A15 attack = 0");
        Near(req.sustainTime, 0.1f, 1e-6f, "A16 sustain = 0.1");
        Near(req.decayTime, 0.3f, 1e-6f, "A17 decay = 0.3");
        Near(req.amplitude, 2.0f, 1e-5f, "A18 amplitude = 2.0");
        Tr(Mathf.Abs(req.direction.x) < 1e-6f && Mathf.Abs(req.direction.y - 0.2f) < 1e-6f
           && Mathf.Abs(req.direction.z - 0.2f) < 1e-6f, "A19 direction = (0, 0.2, 0.2)（真件）");
        // G12 池化语义：模块档恒用默认信号（`GetModifiedPreset` 从不写 rawSignal）
        Tr(req.signal != null && req.signal.name == "Warpforge 6D Shake",
           "A20 模块档（`Resolve` 默认参数）⇒ **恒播默认信号**（原版 `OverwriteCameraShakePreset` 池化副本 rawSignal 恒 null）"
         + "｜🧨 改成按 preset 名自动用自带信号 ⇒ 红");

        var landReq = WarpforgeVFX.WFModuleScreenShake.Resolve(
            new WarpforgeVFX.WFModuleScreenShake.ShakeEntry { preset = "Shake Land on ground" });
        Tr(Mathf.Abs(landReq.direction.y + 1f) < 1e-6f, "A21 `Shake Land on ground` direction = (0, −1, 0)（原版 raw −Y 指向 velocity ⇒ 屏幕朝上）");
    }

    // ============================================================ B：算式（纯函数）
    static void SectionB_算式()
    {
        // ---- B1 Constant=1 ⇒ 正弦（单带 f=1 / A=1 / gain=1 ⇒ u = t）----
        var oneSin = new CardFeel.ShakeSignal("test-sin", new[] { new CardFeel.ShakeBandSignal(1f, 1f, 1) });
        float s25 = CardFeel.ShakeNoise01(0.25f, 0f, 1f, oneSin);   // sin(π/2)  = +1
        float s50 = CardFeel.ShakeNoise01(0.50f, 0f, 1f, oneSin);   // sin(π)    = 0
        float s75 = CardFeel.ShakeNoise01(0.75f, 0f, 1f, oneSin);   // sin(3π/2) = −1
        Tr(Mathf.Abs(s25 - 0.5f / CardFeel.ShakeBandPeak) < 1e-5f
           && Mathf.Abs(s75 + 0.5f / CardFeel.ShakeBandPeak) < 1e-5f
           && Mathf.Abs(s50) < 1e-5f,
           $"B1 `Constant=1` ⇒ **正弦**：单带 f=1/A=1/gain=1 时 T=0.25→{s25:F4}（=+0.5/0.149）· T=0.5→{s50:E2}（=0）· T=0.75→{s75:F4}（=−）"
         + "｜🧨 改回 Perlin ⇒ 这三个数散掉 ⇒ 红");

        // ---- B2 Constant=0 ⇒ Perlin(u, 0)：变化在 **x** 轴（不是 `(0,u)`）----
        // ⚠️ **不能拿单点判**：Unity 的 `Mathf.PerlinNoise` 在个别 u 上两支恰好重合
        //    （2026-10-31 `BattleScene.Run` 实测：u=1.23 时 `P(u,0)` 与 `P(0,u)` 差 <1e-4
        //     ⇒ 那一点**没有判别力**，拿它判会误红）。而同一批运行里 `BattleScene` 自己那 6 个点
        //    （u=0.13…0.78）**6/6 都判得出 x/y 不同** ⇒ 两支**不是恒等**，只是会撞上重合点。
        // ⇒ 扫一组 u，**跳过重合点**（判差 ≤1e-3 的点，不计入分母），要求至少 4 个判别点且**全部**站 x 轴。
        //    重合点被排除 ≠ 放宽：判别点不够 4 个也会红（不许空转绿）。
        var oneNoise = new CardFeel.ShakeSignal("test-perlin", new[] { new CardFeel.ShakeBandSignal(1f, 1f, 0) });
        int axTested = 0, axOk = 0;
        for (int i = 1; i <= 24; i++)
        {
            float u = i * 0.11f;                                          // 0.11 … 2.64
            float wantX = (Mathf.PerlinNoise(u, 0f) - 0.5f) / CardFeel.ShakeBandPeak;
            float wantY = (Mathf.PerlinNoise(0f, u) - 0.5f) / CardFeel.ShakeBandPeak;
            if (Mathf.Abs(wantX - wantY) <= 1e-3f) continue;              // 两支重合 ⇒ 无判别力，不计
            axTested++;
            float got = CardFeel.ShakeNoise01(u, 0f, 1f, oneNoise);
            if (Mathf.Abs(got - wantX) < 1e-5f && Mathf.Abs(got - wantY) > 1e-3f) axOk++;
        }
        Tr(axTested >= 4 && axOk == axTested,
           $"B2 `Constant=0` ⇒ `PerlinNoise(u, 0)`（变化在 **x** 轴）：{axTested} 个**有判别力**的 u 上全部站 x 轴（{axOk}/{axTested}）"
         + "（两支恰好重合的 u 已跳过、不计入分母；判别点 <4 也算红，⛔ 不是空转）"
         + "｜🧨 采样轴改回 `(0,u)` ⇒ 0 个通过 ⇒ 红");

        // ---- B3 三条带【相加】（不是相乘、不是取最大）----
        var three = new CardFeel.ShakeSignal("test-sum", new[]
        {
            new CardFeel.ShakeBandSignal(1f, 1f, 1),
            new CardFeel.ShakeBandSignal(2f, 1f, 1),
            new CardFeel.ShakeBandSignal(3f, 1f, 1),
        });
        float got3 = CardFeel.ShakeNoise01(0.125f, 0f, 1f, three);   // u = 0.125 / 0.25 / 0.375
        float want3 = 0.5f * (Mathf.Sin(0.125f * 2f * Mathf.PI)
                            + Mathf.Sin(0.250f * 2f * Mathf.PI)
                            + Mathf.Sin(0.375f * 2f * Mathf.PI)) / CardFeel.ShakeBandPeak;
        float maxOne = 0.5f / CardFeel.ShakeBandPeak;
        Tr(Mathf.Abs(got3 - want3) < 1e-4f && Mathf.Abs(got3 - maxOne) > 0.5f,
           $"B3 三条带**相加**：三带同时刻 {got3:F4} = 手算和 {want3:F4}（单带最大只有 {maxOne:F4} ⇒ 不是取最大）");

        // ---- B4 frequencyGain 是【纯时间缩放】：T = t·gain ----
        float a = CardFeel.ShakeNoise01(4.0f, 0f, 0.05f, oneSin);    // T = 4 × 0.05 = 0.2
        float b = CardFeel.ShakeNoise01(0.2f, 0f, 1f, oneSin);       // T = 0.2
        Tr(Mathf.Abs(a - b) < 1e-6f,
           $"B4 `frequencyGain` 是**纯时间缩放**（`T = t × gain`，不是幅度/相位缩放）：t=4,gain=0.05 → {a:F6} == t=0.2,gain=1 → {b:F6}"
         + "｜🧨 把 gain 乘在带频/幅度上 ⇒ 两边不等 ⇒ 红");

        // ---- B5 phase 进的是【共享 T】，不是每条带各加一次 ----
        float ph = 37.5f, gain = 0.05f, t0 = 0.1f;
        float v0 = CardFeel.ShakeNoise01(t0, ph, gain, CardFeel.DefaultShakeSignal);
        float v1 = CardFeel.ShakeNoise01(t0 + ph / gain, 0f, gain, CardFeel.DefaultShakeSignal);   // (t + ph/g)·g = t·g + ph
        Tr(Mathf.Abs(v0 - v1) < 1e-3f,
           $"B5 相位在**共享 T** 里（三条带各自的相位 = 2π·f·offset）：noise(t,ph)={v0:F5} ≈ noise(t+ph/gain,0)={v1:F5}"
         + "｜🧨 写成「每条带各加一次 phase」/「先乘 f 再乘 gain」⇒ 两边不等 ⇒ 红");

        // ---- B6 值域：每带 ±0.5·A ⇒ 归一化峰值 = 0.5（分母必须保持 0.149）----
        float mn = 9f, mx = -9f;
        for (int i = 0; i <= 400; i++)
        {
            float v = CardFeel.ShakeNoise01(i * 0.011f, 13.37f);
            if (v < mn) mn = v;
            if (v > mx) mx = v;
        }
        Tr(mx <= 0.55f && mn >= -0.55f && mx - mn > 0.05f,
           $"B6 值域：默认信号 401 个采样点落在 {mn:F3}…{mx:F3}（上界 = **0.5** = 0.0745/0.149）"
         + "｜🧨 分带改成 ±A（`Perlin×2−1`）或把分母改成 0.0745 ⇒ 越界/缩半 ⇒ 红");
    }

    // ============================================================ C：灭自证（旧实现逐点对不上）
    static void SectionC_灭自证_旧实现()
    {
        // 旧实现（R9 之前）= 三条带**全 Perlin** · `Perlin×2−1`（±1）· 采样轴 `(0, f·t+phase)` ·
        // 相位**不分带**（同一个 phase 直接加进参数里）。独立写一遍，不调 `ShakeNoise01`。
        float[] hz = { 1.90f, 9.10f, 55.54f };
        float[] amp = { 0.059f, 0.040f, 0.050f };
        int differs = 0;
        for (int q = 0; q < 16; q++)
        {
            float tt = 0.05f * q;
            float ph = CardFeel.ShakePhaseOffset(2000 + q);
            float oldVal = 0f;
            for (int b = 0; b < 3; b++)
                oldVal += amp[b] * (Mathf.PerlinNoise(0f, hz[b] * CardFeel.ShakeFrequencyGain * tt + ph) * 2f - 1f);
            oldVal /= CardFeel.ShakeBandPeak;
            if (Mathf.Abs(CardFeel.ShakeNoise01(tt, ph) - oldVal) > 0.02f) differs++;
        }
        C(differs, 16,
          $"C1 **灭自证**：新算式与**旧实现**（三条全 Perlin / `Perlin×2−1` / 轴 `(0,f·t+phase)` / 相位不分带）"
        + $"在 16 个 (t,phase) 上**逐点对不上**（{differs}/16）"
        + "｜🧨 把 `ShakeNoise01` 改回旧写法 ⇒ 全相等 ⇒ 这条红（这是 R9 的总体防线）");

        // C2：只把「三条全 Perlin」这一条改回去，也必须对不上（说明 B1 的正弦支路真的在生效）
        int sinDiffers = 0;
        var three = new CardFeel.ShakeSignal("test-mix", new[]
        {
            new CardFeel.ShakeBandSignal(1.90f, 0.059f, 1),
            new CardFeel.ShakeBandSignal(9.10f, 0.040f, 0),
            new CardFeel.ShakeBandSignal(55.54f, 0.050f, 1),
        });
        for (int q = 0; q < 8; q++)
        {
            float T = 0.37f * q + 2.11f;
            float allPerlin = (0.059f * (Mathf.PerlinNoise(1.90f * T, 0f) - 0.5f)
                             + 0.040f * (Mathf.PerlinNoise(9.10f * T, 0f) - 0.5f)
                             + 0.050f * (Mathf.PerlinNoise(55.54f * T, 0f) - 0.5f)) / 0.149f;
            if (Mathf.Abs(CardFeel.ShakeNoise01(T, 0f, 1f, three) - allPerlin) > 0.02f) sinDiffers++;
        }
        C(sinDiffers, 8,
          $"C2 **灭自证**：`Constant=1` 那两条带真的走正弦 —— 同表按「三条全 Perlin」算出来的值在 8 个 T 上全不等（{sinDiffers}/8）"
        + "｜🧨 三条带全 Perlin ⇒ 全相等 ⇒ 红");
    }

    // ============================================================ D：`delay` 语义
    static void SectionD_Delay语义()
    {
        var prevMode = CardTween.Mode;
        CardTween.Mode = UpdateType.Manual;
        CardFeel.ResetShakeGateForTest();
        CardFeel.SettleShake();
        var camGo = new GameObject("B104CF_Cam");
        var hudGo = new GameObject("B104CF_Hud");
        try
        {
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            var hud = hudGo.transform;
            hud.position = new Vector3(1.5f, -0.8f, -10f);

            // D1：delay == 0 ⇒ 当场施加（批处理没有帧循环，第一帧不能漏）
            float ph0; int seed0 = PickSeed(0f, 0.2f, out ph0);
            Tr(seed0 >= 0, "D0（前提）找得到一个 t=0 噪声不接近 0 的种子（否则下面两条会空转）");
            CardFeel.SettleShake();
            Vector3 home = cam.transform.position;
            var tw0 = CardFeel.ShakeCamera(cam, hud, 0.1f, 0f, null, seed0);
            float d0 = (cam.transform.position - home).magnitude;
            Tr(d0 > 0.008f, $"D1 `delay = 0` ⇒ **当场**位移 {d0:F4}（不等 Update：批处理没有帧循环）");
            DG.Tweening.TweenExtensions.Kill(tw0, false);
            CardFeel.SettleShake();

            // D2：delay > 0 ⇒ 等待期里相机**一点都不许动**（原版是 WaitForSeconds）
            CardFeel.SettleShake();
            Vector3 home2 = cam.transform.position;
            Vector3 hud2 = hud.position;
            float ph1; int seed1 = PickSeed(0.05f, 0.2f, out ph1);
            var tw1 = CardFeel.ShakeCamera(cam, hud, 0.1f, 0.5f, null, seed1);
            float at0 = (cam.transform.position - home2).magnitude;
            Tr(at0 < 1e-6f, $"D2 ★ `delay = 0.5` ⇒ 调用的**同一拍相机不动**（实测 {at0:E1}；原版 `_DoShake…MoveNext.c:259-270` 的 `WaitForSeconds`）"
                          + "｜🧨 无条件 `apply(0)` ⇒ 这里当场歪 0.05 世界单位 ⇒ 红");
            CardTween.Advance(0.25f);
            Tr((cam.transform.position - home2).magnitude < 1e-6f,
               "D3 延迟过半（0.25 s < 0.5 s）相机**仍不动**");
            CardTween.Advance(0.30f);                       // 累计 0.55 > 0.5 ⇒ 序列已起步（t = 0.05）
            // ⚠️ 位移还要乘**屏幕方向向量的模长**：默认挨打方向 (0,0.2,0.2) 的
            //    `ShakeScreenDirOf` = (0, −0.70711, 0)（旋转后的 up 再丢掉景深 z）⇒ |screenDir| = 0.70711。
            float want1 = 0.70710678f * 0.1f * Mathf.Abs(CardFeel.ShakeNoise01(0.05f, ph1));
            float got1 = (cam.transform.position - home2).magnitude;
            Tr(Mathf.Abs(got1 - want1) < 2e-4f && got1 > 1e-4f,
               $"D4 延迟结束后**才开始震**：0.55 s 时位移 {got1:F5} = 幅度 0.1 × |screenDir| 0.70711 × |噪声(t=0.05, phase)| {want1:F5}");
            DG.Tweening.TweenExtensions.Kill(tw1, false);
            CardFeel.SettleShake();
            Tr((cam.transform.position - home2).magnitude < 1e-5f
               && (hud.position - hud2).magnitude < 1e-5f,
               "D5 收尾把相机与 HUD 根都放回原位（残留会把后面所有世界坐标断言带偏）");
        }
        finally
        {
            CardFeel.SettleShake();
            CardTween.Mode = prevMode;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(hudGo);
        }
    }

    // ============================================================ E：相机 + HUD 同向 ⇒ 屏幕上 HUD 不动
    static void SectionE_相机HUD同向()
    {
        var prevMode = CardTween.Mode;
        CardTween.Mode = UpdateType.Manual;
        CardFeel.ResetShakeGateForTest();
        CardFeel.SettleShake();
        var camGo = new GameObject("B104CF_Cam2");
        var hudGo = new GameObject("B104CF_Hud2");
        try
        {
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            var hud = hudGo.transform;
            hud.position = new Vector3(2.0f, -1.5f, -10f);      // 离相机 ~2.5 单位 ⇒ 旋转补偿漏掉时误差可测
            hud.rotation = Quaternion.identity;

            // 找一个「位置噪声 ≠ 0 且角度修正 ≠ 0」的种子（否则 E3 空转）
            int seed = -1; float qDeg = 0f;
            for (int i = 0; i < 400; i++)
            {
                float ph = CardFeel.ShakePhaseOffset(i);
                var q = CardFeel.ShakeOrientation(0f, ph, CardFeel.ShakeFrequencyGain,
                                                  CardFeel.DefaultShakeSignal, 2.0f * 0.28284f, 1f);
                float deg = AngleMath.AngleDegTiny(Quaternion.identity, q);
                if (deg > 0.02f && Mathf.Abs(CardFeel.ShakeNoise01(0f, ph)) > 0.2f)
                { seed = i; qDeg = deg; break; }
            }
            Tr(seed >= 0, $"E0（前提）找得到「位置噪声 + 旋转都非零」的种子（q = {qDeg:F4}°）");
            if (seed < 0) return;

            Vector3 camHome = cam.transform.position;
            Quaternion camHomeRot = cam.transform.rotation;
            Vector3 hudHomeW = hud.position;
            Vector3 hudLocal0 = cam.transform.InverseTransformPoint(hud.position);   // 屏幕空间里的相对位置

            var tw = CardFeel.ShakeCamera(cam, hud, 0.1f, 0f, null, seed);

            float moved = (cam.transform.position - camHome).magnitude;
            Tr(moved > 0.008f, $"E1（前提）相机真的动了（{moved:F4} 世界单位）");
            float rotDeg = AngleMath.AngleDegTiny(cam.transform.rotation, camHomeRot);
            Tr(rotDeg > 0.01f, $"E2（前提）相机真的转了（{rotDeg:F4}° —— `OrientationNoise` 那一路；`Quaternion.Angle` 在这里恒读 0，⛔ 不能换回去）");

            // 🔴 **不依赖实现的不变量**：HUD 在**相机局部坐标**里不变 ⇒ 屏幕上纹丝不动。
            //    相机 = C+d、R·q；HUD = C+d+M·(H−C)、M = R·q·R⁻¹ ⇒ (R·q)⁻¹·(HUD−cam) = R⁻¹·(H−C) = 常数。
            Vector3 hudLocal1 = cam.transform.InverseTransformPoint(hud.position);
            float drift = (hudLocal1 - hudLocal0).magnitude;
            Tr(drift < 2e-4f,
               $"E3 ★ 屏幕上 **HUD 纹丝不动**：相机局部坐标漂移 {drift:E2}（3D 内容照晃 {moved:F4}）"
             + "｜🧨 HUD 只平移不跟转（漏 `M` 的旋转那半）⇒ 漂移 ~1e-3 ⇒ 红；完全不补偿 ⇒ 漂移 = |d| ⇒ 红");

            // E4：灭自证 —— 若 HUD 根**完全不补偿**（世界位置留在 hudHomeW），它在新型相机局部坐标里
            //     会漂多少？这个「失败量级」必须**远大于** E3 的容差，否则 E3 等于没查。
            Vector3 noCompLocal = cam.transform.InverseTransformPoint(hudHomeW);
            float noCompDrift = (noCompLocal - hudLocal0).magnitude;
            Tr(noCompDrift > 20f * 2e-4f,
               $"E4 **灭自证**：完全不补偿 HUD 时本不变量会漂 {noCompDrift:F4} 世界单位（≫ 容差 2e-4 的 20 倍）"
             + " ⇒ E3 不是天然成立、漏补偿必红");

            DG.Tweening.TweenExtensions.Kill(tw, false);
            CardFeel.SettleShake();
            Tr((cam.transform.position - camHome).magnitude < 1e-5f,
               "E5 收尾把相机放回原位（`OnKill` 复位；不复位镜头会永久歪）");
        }
        finally
        {
            CardFeel.SettleShake();
            CardTween.Mode = prevMode;
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(hudGo);
        }
    }

    /// <summary>在 0..199 里挑第一个「t 处 |噪声| ≥ need」的种子（返回 −1 = 没挑到）。</summary>
    static int PickSeed(float t, float need, out float phase)
    {
        for (int seed = 0; seed < 200; seed++)
        {
            float ph = CardFeel.ShakePhaseOffset(seed);
            if (Mathf.Abs(CardFeel.ShakeNoise01(t, ph)) >= need) { phase = ph; return seed; }
        }
        phase = 0f;
        return -1;
    }
}
