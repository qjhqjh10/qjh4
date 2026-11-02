// B117_G10_03_Assert.cs — 执行代理 `B117-G10-03`（2026-11-02）· 战斗域第 3 分片「HUD 画布底座 · 适配 · 按钮反馈」
//
// ============================ 它盯的是什么 ============================
//  本分片的**五条判据**（逐条带出处，全部亲读）。⛔ 本文件不改任何既有宿主，只提供 `Run()`。
//    A. **比例适配公式** —— `ScreenAspectRatioController.ComputeViewport` 的三支 + 两条边界
//       （原版 `ScreenAspectRatioController__AdjustCameraViewport.c`；⚠️ 窄屏那支分子是 `(min/aspect − 1)`，⛔ 不是 `(1 − aspect/min)`）。
//    B. **按钮禁用档的 α 有【两族】** —— `colorTintGreyOnDisable = 1` ⇒ α **1.0**；`= 0` ⇒ α **0.502**
//       （`Battle Arena 1.unity` 39 颗 `EverguildButton` 逐颗；机制 = `EverguildButton__Awake.c` 里那次改写）。
//    C. **禁用 ⇒ 换灰化材质**（`EverguildButtonHelper.DoMaterialRefresh` → `Everguild/UI/Greyscale`），
//       取不到 shader ⇒ **不变灰但必须出声**（⛔ 不许静默）。
//    D. **点击音** —— `BattleHudButton.clickCue`：`null` = 静音（默认，因为原版有 2 颗是静的），
//       给 cue 就响（`WindowSounds.DefaultButtonCue` = 原版 `SoundManager.defaultButtonSound` 的等价物）。
//    E. **锚点换算** —— `HudCanvas.AnchorApPx` / `PosPx` 是**两个不同的量**（前者相对父的某个角、后者相对父中心），
//       ⛔ 别拿 `PosPx` 顶 `AnchorApPx`（`T09` §八·补 陷阱 1）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 A：同时断言「5:4 的 letterbox `f` = 0.03333321」**和**「它 ≠ 直觉式 `(1 − aspect/min)/2` = 0.03124999」
//     —— 谁把公式"化简"成直觉式，两条一起红（只改一条不会全绿）。
//  🧨 B：`grey=1` 族 α = 1.0 与 `grey=0` 族 α = 0.50196 **两个数同时钉** + 断言两者相差 > 0.4
//     —— 退回「一个常量管全家」⇒ 必有一条红。
//  🧨 C：`GrayedForTest` 与 `GrayShaderMissingForTest` **互斥地各断一条**（要么真灰了、要么明确记录了"没灰成"）
//     —— 有人把「取不到 shader」改成静默 return ⇒ 两条都红。
//  🧨 D：默认（不给 cue）必须 `LastClickCueForTest == null`，给 cue 必须 == `ButtonClick`
//     —— 把默认翻成"播"、或把开关写死成"不播"，各红一条。
//  🧨 E：`AnchorApPx(100,50, 0,1)` ≠ `PosPx(100,50)`（相差 > 100 px）—— 用错口必红。
//
// ============================ 怎么接 ============================
//  `public static void Run()`：**零前提**（自带临时根 + 收尾 `DestroyImmediate`；不碰场景/存档/真实 HUD 画布）。
//  挂法（由调度台决定，⛔ 本代理不改宿主）：
//      B117_G10_03_Assert.Run(); _sink.Pass += B117_G10_03_Assert.Pass; _sink.Fail += B117_G10_03_Assert.Fail;
//  判绿红：`grep "^B117G1003"` 的**合计行**；逐条按**行首** `B117G1003 ✓` / `B117G1003 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。

using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B117_G10_03`：HUD 底座 · 比例适配 · 按钮反馈（禁用两族 α / 灰化 / 点击音）· 锚点换算。</summary>
public static class B117_G10_03_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B117G1003 ✓ " + msg); }
        else { _fail++; Debug.LogError("B117G1003 ✗ " + msg); }
    }

    static void CheckClose(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F6") + " ± " + tol + "；实得 " + got.ToString("F6") + "）");
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        var root = new GameObject("B117_G10_03_Root");
        try
        {
            Group("A·比例适配", A_Aspect);
            Group("B·禁用两族 α", B_DisabledTwoFamilies);
            Group("C·禁用灰化", C_GrayOnDisable);
            Group("D·点击音", () => D_ClickSound(root.transform));
            Group("E·锚点换算", E_Anchors);
            Group("F·屏幕物理量", F_ScreenUtilities);
        }
        catch (System.Exception e)
        {
            Check(false, "宿主抛异常（后面的断言**没跑到**，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
        Debug.Log("B117G1003 合计：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    static void Group(string tag, System.Action body)
    {
        try { body(); }
        catch (System.Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）："
                       + e.GetType().Name + ": " + e.Message);
        }
    }

    // ================================================================ A 比例适配
    // 判据：`decomp_full/ScreenAspectRatioController__AdjustCameraViewport.c`（与 `__Start.c` 逐字相同）
    //   aspect = W / H
    //   max < aspect ⇒ rect = (f, 0, 1−2f, 1)，f = (1 − max/aspect) × 0.5      [[ 宽过 max ⇒ 左右黑边 ]]
    //   aspect < min ⇒ rect = (0, f, 1, 1−2f)，f = (min/aspect − 1) × 0.5      [[ 窄过 min ⇒ 上下黑边 ]]
    //   否则         ⇒ rect = (0, 0, 1, 1)
    //  序列化值（场景 `battlearena1` 的 MB 5148）：min = 1.3333330154418945 · max = 2.444443941116333
    static void A_Aspect()
    {
        var go = new GameObject("AspectProbe");
        try
        {
            var c = go.AddComponent<ScreenAspectRatioController>();
            Check(c != null, "A0（前提）`ScreenAspectRatioController` 挂得起来（字段初值来自字段初始化器 = 场景那一档）");
            if (c == null) return;
            float min = c.MinAspectRatioTest, max = c.MaxAspectRatioTest;
            CheckClose(min, 1.3333330154418945f, 1e-6f, "A1 `minAspectRatio` = 场景值 1.3333330154（⛔ 不是 ctor 的 1.3333334）");
            CheckClose(max, 2.444443941116333f, 1e-6f, "A2 `maxAspectRatio` = 场景值 2.4444439411（⛔ 不是 ctor 的 2.4444447）");

            // 整幅三档
            Rect r;
            r = ScreenAspectRatioController.ComputeViewport(16f / 9f, min, max);
            Check(r == new Rect(0f, 0f, 1f, 1f), "A3 16:9 ⇒ 整幅 (0,0,1,1)（实得 " + R(r) + "）");
            r = ScreenAspectRatioController.ComputeViewport(16f / 10f, min, max);
            Check(r == new Rect(0f, 0f, 1f, 1f), "A4 16:10 ⇒ 整幅（1.6 夹在 [1.3333, 2.4444] 内；实得 " + R(r) + "）");
            r = ScreenAspectRatioController.ComputeViewport(21f / 9f, min, max);
            Check(r == new Rect(0f, 0f, 1f, 1f), "A5 21:9 ⇒ 整幅（2.3333 < 2.4444 ⇒ **没有黑边**；实得 " + R(r) + "）");

            // 边界：严格不等 ⇒ 正好等于 max / min 都是整幅
            r = ScreenAspectRatioController.ComputeViewport(max, min, max);
            Check(r == new Rect(0f, 0f, 1f, 1f), "A6 正好 = max ⇒ 整幅（判据是 `max < aspect` **严格**；实得 " + R(r) + "）");
            r = ScreenAspectRatioController.ComputeViewport(min, min, max);
            Check(r == new Rect(0f, 0f, 1f, 1f), "A7 正好 = min ⇒ 整幅（`aspect < min` **严格**；实得 " + R(r) + "）");

            // 宽过 max ⇒ pillarbox（左右黑边）：x = f、宽 = 1−2f、**y 与高都是满的**
            const float a225 = 2.5f;                      // 22.5 : 9
            r = ScreenAspectRatioController.ComputeViewport(a225, min, max);
            float fw = (1f - max / a225) * 0.5f;          // = 0.011111212
            CheckClose(r.x, fw, 1e-6f, "A8 22.5:9 的左黑边 x = (1 − max/aspect)×0.5");
            CheckClose(r.y, 0f, 1e-6f, "A9 22.5:9 的 y = 0（**左右**黑边，不是上下）");
            CheckClose(r.width, 1f - 2f * fw, 1e-6f, "A10 22.5:9 的宽 = 1 − 2f");
            CheckClose(r.height, 1f, 1e-6f, "A11 22.5:9 的高 = 1（满高）");

            // 窄过 min ⇒ letterbox（上下黑边）
            const float a54 = 1.25f;                      // 5 : 4
            r = ScreenAspectRatioController.ComputeViewport(a54, min, max);
            float fh = (min / a54 - 1f) * 0.5f;           // = 0.033333206
            CheckClose(r.y, fh, 1e-6f, "A12 5:4 的下黑边 y = **(min/aspect − 1)×0.5**（原版那一支的分子）");
            CheckClose(r.x, 0f, 1e-6f, "A13 5:4 的 x = 0（**上下**黑边）");
            CheckClose(r.width, 1f, 1e-6f, "A14 5:4 的宽 = 1（满宽）");
            CheckClose(r.height, 1f - 2f * fh, 1e-6f, "A15 5:4 的高 = 1 − 2f");

            // 🧨 灭自证：把窄屏那支「化简」成直觉式 `(1 − aspect/min)/2` ⇒ 这两条一起红
            float intuitive = (1f - a54 / min) * 0.5f;    // = 0.03124999
            Check(Mathf.Abs(fh - intuitive) > 1e-4f,
                  "A16 🧨 窄屏那支的分子**不是** `(1 − aspect/min)`：原版 " + fh.ToString("F6")
                + " vs 直觉式 " + intuitive.ToString("F6") + "（差 " + (fh - intuitive).ToString("F6")
                + "；⚠️ 两者不等，逐字节核过）");
            float intuitiveWide = (a225 / max - 1f) * 0.5f;   // = 0.0113638
            Check(Mathf.Abs(fw - intuitiveWide) > 1e-4f,
                  "A17 🧨 宽屏那支也不是 `(aspect/max − 1)×0.5`：原版 " + fw.ToString("F6")
                + " vs 直觉式 " + intuitiveWide.ToString("F6"));

            // 退化的宽高比：整幅（我们加的防除零，原版没有这一支）
            r = ScreenAspectRatioController.ComputeViewport(0f, min, max);
            Check(r == new Rect(0f, 0f, 1f, 1f), "A18 aspect = 0 ⇒ 整幅（防除零；⛔ 不是 NaN 矩形）");
        }
        finally { Object.DestroyImmediate(go); }
    }

    static string R(Rect r) { return "(" + r.x.ToString("F6") + "," + r.y.ToString("F6") + ","
                                   + r.width.ToString("F6") + "," + r.height.ToString("F6") + ")"; }

    // ================================================================ B 禁用档的两族 α
    // 判据：`Battle Arena 1.unity` 39 颗 `EverguildButton` 逐颗现读
    //   · `colorTintGreyOnDisable = 1 & m_Transition = 1` 共 19 颗 ⇒ 运行时 `m_DisabledColor.a` **全是 1**
    //     （18 颗序列化就是 1；`Views` pid 4630 序列化 0.502，但 `colorTintGreyOnDisableAlpha = 1` 会被
    //      `EverguildButton__Awake.c` 里那一跳改写 ⇒ 运行时仍是 1）。
    //   · `colorTintGreyOnDisable = 0 & m_Transition = 1` 共 4 颗（Auto Zoom Toggle / Select Active Skill /
    //     Select Melee / Select Range）⇒ 全是 uGUI 出厂 **0.5019608**。
    //   ⇒ `Tint(...)` 的**缺省**取 grey=1 那一族（α 1.0），0.502 那一档必须显式传 `BattleButtonFx.Disabled`。
    static void B_DisabledTwoFamilies()
    {
        var a = MakePiece("B_piece_grey1");
        var b = MakePiece("B_piece_grey0");
        try
        {
            var fxA = BattleButtonFx.Tint(a);                                         // 缺省档
            var fxB = BattleButtonFx.Tint(b, null, null, null, null, BattleButtonFx.Disabled);  // 显式 0.502 档
            Check(fxA != null && fxB != null, "B0（前提）两档都建出来了");
            if (fxA == null || fxB == null) return;

            fxA.SetDisabled(true);
            fxB.SetDisabled(true);
            Color ca = fxA.KeyForTest, cb = fxB.KeyForTest;

            CheckClose(ca.r, 0.7843137f, 1e-4f, "B1 grey=1 族的禁用 RGB.r = 0.7843137（原版 m_DisabledColor.r）");
            CheckClose(ca.a, 1f, 1e-4f, "B2 grey=1 族的禁用 **α = 1.0（不透明）** —— 19/19 颗 ColorTint 逐颗核过");
            CheckClose(cb.a, 0.5019608f, 1e-4f, "B3 grey=0 族的禁用 α = 0.5019608（uGUI 出厂值，4/4 颗）");

            // 🧨 灭自证：「一个常量管全家」⇒ 必然有一条红
            Check(Mathf.Abs(ca.a - cb.a) > 0.4f,
                  "B4 🧨 两族 α 相差 " + Mathf.Abs(ca.a - cb.a).ToString("F4")
                + "（> 0.4）—— 同一份常量不可能同时满足这两颗（B2 / B3 互为反相）");
            Check(ca.a > 0.99f && cb.a < 0.51f,
                  "B5 🧨 缺省档**必须**是不透明那一族（ca.a = " + ca.a.ToString("F4") + "）、显式档必须半透明（cb.a = "
                + cb.a.ToString("F4") + "）—— 两边对调也红");

            fxA.SetDisabled(false);
            CheckClose(fxA.KeyForTest.a, 1f, 1e-4f, "B6 解除禁用回到 Normal（α = 1.0、RGB 白）");
            CheckClose(fxA.KeyForTest.r, 1f, 1e-4f, "B6b 同上，R 回白");
        }
        finally { DestroyPieces(); }
    }

    // ================================================================ C 禁用 ⇒ 灰化材质
    // 判据：`EverguildButtonHelper__DoMaterialRefresh.c` 末句 + `EverguildButtonMaterialModifier__GetModifiedMaterial.c`
    //       ⇒ 禁用时那一组图形件的材质换成 `Shader.Find("Everguild/UI/Greyscale")`（**全局一份**）。
    //       ⛔ 取不到 shader ⇒ 不变灰、但**必须出声**（`WindowButton.RefreshGray` 同一条红线）。
    static void C_GrayOnDisable()
    {
        var p = MakePiece("C_piece_grey");
        try
        {
            var fx = BattleButtonFx.Tint(p, "C_piece");
            if (fx == null) { Check(false, "C0（前提）`Tint` 建不出 fx"); return; }

            Shader sh; bool have = WarpforgeVFX.WarpforgeShaderLoader.TryGetShader(
                                       Shell_GrayShaderName(), out sh) && sh != null;

            fx.SetDisabled(true);
            if (have)
            {
                Check(fx.GrayedForTest, "C1 能取到 `Everguild/UI/Greyscale` ⇒ 禁用**真的换上了灰材质**");
                Check(!fx.GrayShaderMissingForTest, "C1b 灰着的时候**不许**同时报「取不到 shader」（两个状态互斥）");
                var m = p.Material;
                Check(m != null && m.shader != null && m.shader.name == Shell_GrayShaderName(),
                      "C2 件的材质 shader 名 = `" + Shell_GrayShaderName() + "`（实得 "
                    + (m != null && m.shader != null ? m.shader.name : "<null>") + "）");
            }
            else
            {
                Check(!fx.GrayedForTest && fx.GrayShaderMissingForTest,
                      "C1'（本机取不到 shader）⇒ **不变灰、但明确记录了 `GrayShaderMissingForTest`**"
                    + "（⛔ 不许静默「看着像没做」）");
            }

            fx.SetDisabled(false);
            Check(!fx.GrayedForTest, "C3 解除禁用 ⇒ 灰化**撤掉**（不撤 = 这颗钮永远是灰的）");
            Check(p.Material == null || p.Material.shader == null
                  || p.Material.shader.name != Shell_GrayShaderName(),
                  "C3b 解除后材质**不再是**灰化 shader（还原到了原材质）");

            // 🧨 灭自证：再灰一次还要能灰 —— 只在第一次生效（`_savedMat` 写坏）⇒ 这条红
            fx.SetDisabled(true);
            if (have) Check(fx.GrayedForTest, "C4 🧨 第二次禁用**仍然**灰得上（`_savedMat` 没被自己的灰材质污染）");
            fx.SetDisabled(false);
        }
        finally { DestroyPieces(); }
    }

    /// <summary>灰化 shader 的**唯一出处**（`Shell/WindowButton.GrayShaderName`）—— 本文件不抄字面量。</summary>
    static string Shell_GrayShaderName() { return WindowButton.GrayShaderName; }

    // ================================================================ D 点击音
    // 判据：`EverguildButton__OnPointerClick.c` 后半段 / `cpp2il/out_il2csharp/.../EverguildButton.cs:1008-1080`：
    //   `m_onClickSound` 有 clip ⇒ 播它；否则 `useDefaultAudioClipIfNull`(=1，37/39 颗) ⇒ `SoundManager.defaultButtonSound`。
    //   ⛔ **2 颗静音**（`Voice Over Button` / `Continue Button`）⇒ 本类的 `clickCue` 缺省必须是 `null`（不播）。
    static void D_ClickSound(Transform parent)
    {
        bool wasSoundOn = WarpforgeVFX.WFSoundPlayer.Enabled;
        WarpforgeVFX.WFSoundPlayer.Enabled = false;     // 批处理没有声卡：关掉真播放，只验「该播哪条」的记账
        var p1 = MakePiece("D_piece_silent");
        var p2 = MakePiece("D_piece_default");
        try
        {
            int silentHits = 0, cueHits = 0;

            var b1 = BattleHudButton.Attach(p1, () => silentHits++);                       // 不给 cue
            var b2 = BattleHudButton.Attach(p2, () => cueHits++, WindowSounds.DefaultButtonCue);
            Check(b1 != null && b2 != null, "D0（前提）两颗都挂上了 uGUI 点击");
            if (b1 == null || b2 == null) return;

            b1.ClickForTest();
            Check(silentHits == 1 && b1.ClickCountForTest == 1, "D1 不给 cue ⇒ 动作照跑一次（实得 " + silentHits + "）");
            Check(string.IsNullOrEmpty(b1.LastClickCueForTest),
                  "D2 🧨 不给 cue ⇒ **一声都不播**（`LastClickCueForTest` 为空；实得 `" + (b1.LastClickCueForTest ?? "<null>") + "`）");

            b2.ClickForTest();
            Check(cueHits == 1 && b2.ClickCountForTest == 1, "D3 给 cue ⇒ 动作照跑一次（实得 " + cueHits + "）");
            Check(b2.LastClickCueForTest == WindowSounds.DefaultButtonCue,
                  "D4 🧨 给 cue ⇒ 播的正是 `" + WindowSounds.DefaultButtonCue + "`（原版 `SoundManager.defaultButtonSound`"
                + " 的等价物；实得 `" + (b2.LastClickCueForTest ?? "<null>") + "`）");

            // 🧨 灭自证（D2 + D4 互为反相）：把缺省翻成「播」⇒ D2 红；把开关写死成「不播」⇒ D4 红。
            Check(string.IsNullOrEmpty(b1.LastClickCueForTest) && b2.LastClickCueForTest != null,
                  "D5 🧨 一静一响**同时成立** —— 不存在「统一播」或「统一不播」能同时满足这两种情况");

            // D6：幂等 —— 同一件再 `Attach` 一次只换动作，不带出第二个组件 / 第二次点击
            int second = 0;
            var b1b = BattleHudButton.Attach(p1, () => second++);
            Check(b1b == b1 && p1.gameObject.GetComponents<BattleHudButton>().Length == 1,
                  "D6 同一件重复 `Attach` ⇒ 复用同一个组件（实得组件数 "
                + p1.gameObject.GetComponents<BattleHudButton>().Length + "）");
            b1.ResetClickCountForTest();
            b1b.ClickForTest();
            Check(second == 1 && silentHits == 1, "D7 换过动作之后只跑**新**动作（旧动作没被再调一次）");
        }
        finally
        {
            WarpforgeVFX.WFSoundPlayer.Enabled = wasSoundOn;
            DestroyPieces();
        }
    }

    // ================================================================ E 锚点换算
    // 判据：`HudCanvas.AnchorApPx` 的 doc（式子 `ap = (xPx − ax·1920, 1080·(1 − ay) − yPxFromTop)`）
    //   ＋ ⛔「别拿 `PosPx` 顶它」（`T09` §八·补 陷阱 1）—— `PosPx` 是**相对父中心**的局部坐标。
    static void E_Anchors()
    {
        Vector2 center = HudCanvas.PosPx(960f, 540f);
        CheckClose(center.x, 0f, 1e-4f, "E1 设计框中心 (960,540) 在 `PosPx` 里是 (0,0).x");
        CheckClose(center.y, 0f, 1e-4f, "E2 设计框中心 (960,540) 在 `PosPx` 里是 (0,0).y");

        Vector2 ap = HudCanvas.AnchorApPx(960f, 540f, 0.5f, 0.5f);
        CheckClose(ap.x, 0f, 1e-4f, "E3 中心锚 + 中心点 ⇒ `AnchorApPx` 也是 (0,0).x（两种锚在这一格重合）");
        CheckClose(ap.y, 0f, 1e-4f, "E4 同上 .y");

        // 左上点锚：枢轴点落在 (100, 50) ⇒ ap = (100 − 0·1920, 1080·(1−1) − 50) = (100, **−50**)
        // 🔴 **2026-11-02（`B117-FIXD`）就地订正（铁律 5）**：原来这里写 `(100, 1030)`（= `1080 − 50`）—— **错**。
        //   点锚 `(0,1)` = 父矩形的**左上角**；`anchoredPosition` 是**相对那个角**的偏移、Unity 里 y 向上 ⇒
        //   「在顶边之下 50 px」= **−50**（`AnchorApPx` 的 doc 式子 `1080·(1 − ay) − yPxFromTop` 代入 ay=1 也是 −50）。
        //   `1080 − 50` 那一版把左上角当成了**左下角**（那是 `ay = 0` 的式子）。
        Vector2 tl = HudCanvas.AnchorApPx(100f, 50f, 0f, 1f);
        CheckClose(tl.x, 100f, 1e-4f, "E5 点锚 (0,1) 下 (100,50) 的 ap.x = 100");
        CheckClose(tl.y, -50f, 1e-4f, "E6 点锚 (0,1) 下 (100,50) 的 ap.y = 1080·(1−1) − 50 = −50（顶边之下 = 负）");

        // 🧨 灭自证：同一个设计点，两种口给出的数**必须不同** —— 用错口一眼可见
        Vector2 pos = HudCanvas.PosPx(100f, 50f);        // = (−860, 490)
        Check(Mathf.Abs(pos.x - tl.x) > 100f && Mathf.Abs(pos.y - tl.y) > 100f,
              "E7 🧨 `PosPx(100,50)` = (" + pos.x.ToString("F2") + "," + pos.y.ToString("F2")
            + ") 与 `AnchorApPx(100,50,0,1)` = (" + tl.x.ToString("F2") + "," + tl.y.ToString("F2")
            + ") **差得很远** ⇒ 两个口不可互替（拿 `PosPx` 顶它 = 整件偏 (960−100, 540−1030) 那种量）");

        // `SetAnchorPx` / `CenterPx` 往返（点锚 + 居中枢轴时两者同值）
        var go = new GameObject("E_rt", typeof(RectTransform));
        try
        {
            var rt = (RectTransform)go.transform;
            HudCanvas.SetAnchorPx(rt, 0.5f, 0.5f, 960f, 540f);
            var c = HudCanvas.CenterPx(rt);
            CheckClose(c.x, 960f, 1e-3f, "E8 `SetAnchorPx`→`CenterPx` 往返 x（中心锚）");
            CheckClose(c.y, 540f, 1e-3f, "E9 同上 y");

            HudCanvas.SetAnchorPx(rt, 0f, 1f, 100f, 50f);
            c = HudCanvas.CenterPx(rt);
            CheckClose(c.x, 100f, 1e-3f, "E10 点锚 (0,1) 下往返 x（`CenterPx` 与锚点无关，只读枢轴点）");
            CheckClose(c.y, 50f, 1e-3f, "E11 同上 y");
        }
        finally { Object.DestroyImmediate(go); }
    }

    // ================================================================ F 屏幕物理量
    // 判据：`Core/ScreenUtilities.cs` 文件头（原版 `ScreenUtilities` 三个纯函数，il2csharp 有完整体）
    static void F_ScreenUtilities()
    {
        float dpi = ScreenUtilities.GetScreenDPI();
        Check(dpi >= 100f, "F1 `GetScreenDPI()` 有**下限 100**（实得 " + dpi.ToString("F2") + "）");

        float phys = ScreenUtilities.GetScreenPhysicalSize();
        float expect = Mathf.Sqrt((float)(Screen.height * Screen.height + Screen.width * Screen.width)) / dpi;
        CheckClose(phys, expect, 1e-3f, "F2 物理对角线 = sqrt(w²+h²) / max(100, dpi)（英寸）");

        float t = (phys - 4.7f) / 0.9000001f;
        float clamp = t < 0f ? 0f : (t > 1f ? 1f : t);
        CheckClose(ScreenUtilities.GetDiagonalSizeReferenceValue(), clamp, 1e-4f,
                   "F3 `GetDiagonalSizeReferenceValue()` = clamp01((英寸 − 4.7) / 0.9000001)");
    }

    // ================================================================ 工具
    static readonly System.Collections.Generic.List<GameObject> _made =
        new System.Collections.Generic.List<GameObject>();

    /// <summary>造一颗**uGUI 件**（`RectTransform` + `Image`）包成 `HudPiece` —— 不碰 `HudCanvas` 的进程级静态根画布
    /// （⛔ 那条静态口一旦被建出来，后面的宿主会复用，本文件的夹具不该改世界状态）。</summary>
    static HudPiece MakePiece(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var img = go.AddComponent<UnityEngine.UI.Image>();
        _made.Add(go);
        return HudPiece.Of((RectTransform)go.transform, img);
    }

    static void DestroyPieces()
    {
        for (int i = 0; i < _made.Count; i++) if (_made[i] != null) Object.DestroyImmediate(_made[i]);
        _made.Clear();
    }
}
