// B109_E16_AspectClamp_Assert.cs — 执行代理 `B109_E16`（2026-10-31 · 第一百零九会话）
//
// ============================ 它盯的是什么（AUD05 S4 = N80）============================
//  原版**主菜单场景**的 UI 相机上挂着 `ScreenAspectRatioController`（`MB1987`）⇒ 屏幕比例超出
//  [4:3, 22:9] 时把整个渲染区缩成中间一个盒子、盒子外涂黑。我们壳层**原来没有这一件**
//  ⇒ 5:4 / 32:9 下不黑边、直接拉伸。
//  本文件钉三样：① 壳侧真的挂上了那件（`ShellRuntime.EnsureAspectClamp`）② 挂上之后 `Camera.rect`
//  写下去、`Camera.aspect` 跟着变 ③ **分辨率变了会重算**（原版 `ScreenResolutionChangeSignal` 的壳侧发射源
//  = `ShellRuntime.TickAspectClamp`）。
//  外加一小节「两族读像素」的复核（简报点名要求）：`LayoutSpace.IsInsideScreen`（整屏判，**不许**随 rect 缩，
//  与 `BattleManager.GetMousePerspectivePos` 同口径）与主菜单模式卡视差的归一化屏幕 x。
//
// ============================ 判据出处 ============================
//  ① `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1987.json`：
//       `m_Enabled = 1` · `targetCameras = [Camera_602]` · `maxAspectRatio = 2.444443941116333`（22:9）·
//       `minAspectRatio = 1.3333330154418945`（4:3）；`Camera_602` 与 `1987` **同属 GO pid 47 = `UI Camera`**
//       （`Camera/Camera_602.json` 的 `m_GameObject` 与 `GameObject/UI Camera.json` 的组件表，2026-10-31 本代理现读）。
//       ⚠️ 就地订正（铁律 5）：施工单/R5 里那句「场景 `cleanFrameBuffer = 1`」**不成立** —— 该字段 private、
//       **不进场景 JSON**（ctor 出厂 = 1，`SetCamerasRect` 每次置 1）。
//  ② 全解包带 `maxAspectRatio` 的只有 **14 份**（13 战场 + 主菜单这一份）⇒ `Intro`/`level0` **没有**
//     （`grep -rl maxAspectRatio d:/2/新解包资源/assets_full`）⇒ 壳场景（开场）不挂，见 `ShellRuntime` 类尾。
//  ③ 公式（逐字节对过原版 `__AdjustCameraViewport.c` / `__Start.c`）：
//       aspect > max ⇒ `rect = (f, 0, 1−2f, 1)`，`f = (1 − max/aspect) × 0.5`
//       aspect < min ⇒ `rect = (0, f, 1, 1−2f)`，`f = (min/aspect − 1) × 0.5`   ⚠️ 分子与上一支**不是**同一个式子
//       其余        ⇒ 整幅
//  ④ `Camera.aspect` 的定义（Unity）：没被显式钉死时 = `pixelRect.width / pixelRect.height`
//     ⇒ 写 `Camera.rect` 之后它**当场**跟着变 —— 这正是「相机一缩、两族读像素的地方就不对」的物理原因。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  🧨 把 `MainMenuRuntime.Build` / `DeckRuntime.Build` 里那句 `ShellRuntime.EnsureAspectClamp(...)` 删掉
//     ⇒ 本文件自己**仍绿**（夹具直接调 EnsureAspectClamp）—— 所以接线那条**由宿主侧那两句承担**，
//     这里的 B 段只证明「这件**能用**」；两者缺一不可（报告里写清了）。
//  🧨 `EnsureAspectClamp` 里删掉 `SetTargetCameras` ⇒ B2 红（`TargetCameraCount` 成 0）。
//  🧨 删掉 `Attach()` ⇒ B3/B5 红（订阅没挂 / 一次都没算）。
//  🧨 把 `EnsureAspectClamp` 改成每次都 `AddComponent`（不做 `GetComponent` 复用）⇒ B6 红（相机上 2 件）。
//  🧨 `TickAspectClamp` 里删掉「基线比对后调 `AdjustCameraViewport`」那一句 ⇒ D2/D4 红（D1 仍绿 —— 两面结构上互斥）。
//  🧨 `TickAspectClamp` 改成不看基线、每帧都重算 ⇒ D1/D3 红（幂等那条）。
//  🧨 `ScreenNormalizedXForDesignPx` 改回 `xPx / 1920` ⇒ E2 前三条红、第四条（整幅档）仍绿 —— 正是「夹取才差」。
//  🧨 把 `LayoutSpace.IsInsideScreen` 改成读 `Cam.pixelRect` ⇒ E1 红（那就**偏离**了原版 `GetMousePerspectivePos`）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  建议接在 `Editor/MainMenuScene.cs`（主菜单那颗 UI 相机的宿主）与 `Editor/DeckScene.cs`（独立场景那一档）的
//  聚合行；`ShellScene` 亦可（本文件自带夹具、不依赖宿主建的东西）。
//  追加写法（同一物理行末尾，⛔ 别加行）：
//      B109_E16_AspectClamp_Assert.Run(); _sink.Pass += B109_E16_AspectClamp_Assert.Pass; _sink.Fail += B109_E16_AspectClamp_Assert.Fail;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B109_E16`：壳侧比例夹取 [4:3, 22:9] + 黑边的**接线 / rect / aspect / 分辨率重算**。见文件头。</summary>
public static class B109_E16_AspectClamp_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B109E16] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void T(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void E<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }
    static void S(string t) { MenuCheck.Section(_sink, t); }

    // 原版场景那一档（`MonoBehaviour_1987.json` / 战场 `MonoBehaviour_5148.json` 同值）——
    // ⚠️ **不是** ctor 出厂档（1.3333334 / 2.4444447），两档差 1e-7 级，B4 专门把两档都判一次。
    const float MinAspect = 1.3333330154418945f;
    const float MaxAspect = 2.444443941116333f;

    static GameObject _camGo, _otherCamGo;
    static Camera _cam, _otherCam;
    static ScreenAspectRatioController _ctl;
    static Camera _prevLayoutCam;

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();
        S("B109_E16：壳侧比例夹取 [4:3, 22:9]（AUD05 S4 = N80；判据 MB1987）");

        BuildFixture();

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  A · 前提（硬断言；⛔ 不用 `if (x != null) Check(...)` —— 前提不成立时整段会静默跳过）
        // ═══════════════════════════════════════════════════════════════════════════════════════
        S("A 前提：一台**没钉死 aspect** 的相机 + 一个不相干相机");
        T(_cam != null && _otherCam != null, "（A0 前提）夹具两台相机都建出来了");
        if (_cam == null || _otherCam == null) { Cleanup(); S("A0 不成立 ⇒ 后面全跳过（已在上一条红）"); return; }

        // 前提：夹具相机**没**显式钉 aspect —— 钉了就测不出「写 rect ⇒ aspect 跟着变」。
        // ⚠️ 12 个自检宿主都把 `cam.aspect` 钉成 16:9（那是对的、别动它们），所以夹具必须**自己建**一台。
        float autoAspect = Screen.width / Mathf.Max(1f, (float)Screen.height);
        N(_cam.aspect, autoAspect, 1e-3f,
          "（A1 前提）夹具相机是**自动 aspect**（= 屏宽高比；没被显式钉死 ⇒ 写 rect 才能带出 aspect 变化）");
        T(Mathf.Abs(_otherCam.rect.x) < 1e-6f && Mathf.Abs(_otherCam.rect.width - 1f) < 1e-6f,
          "（A2 前提）不相干那台相机出厂 = 整幅");

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  B · 接线：`EnsureAspectClamp` 挂的是原版那一件（参数取**场景档**、只写一台、两条订阅都在）
        // ═══════════════════════════════════════════════════════════════════════════════════════
        S("B 接线：`ShellRuntime.EnsureAspectClamp(cam)`");
        _ctl = ShellRuntime.EnsureAspectClamp(_cam);
        T(_ctl != null, "★ B1 挂上了 `ScreenAspectRatioController`");
        T(_ctl != null && _cam.GetComponent<ScreenAspectRatioController>() == _ctl,
          "★ B1 组件就在**这台相机的 GameObject 上**（原版挂在 UI Camera 本体，`MB1987`）");
        if (_ctl == null) { Cleanup(); S("B1 不成立 ⇒ 后面全跳过（已在上一条红）"); return; }

        var tcs = _ctl.TargetCameras;
        T(_ctl.TargetCameraCount == 1 && tcs != null && tcs.Length == 1 && tcs[0] == _cam,
          "★ B2 `targetCameras = [这一台]`（原版 `MB1987` 只有 `Camera_602` 一台；⛔ 不是 `[boardCam, cam]` 那一档）");

        T(_ctl.IsAttachedForTest, "★ B3 分辨率信号那条订阅挂上了（= 原版 `Start` 的 ②）");
        T(_ctl.IsRenderingHookedForTest, "★ B3 `beginCameraRendering` 那条也挂上了（= 原版 `Start` 的 ③，真机才会抬）");

        T(Mathf.Abs(_ctl.MinAspectRatioTest - MinAspect) < 1e-7f
          && Mathf.Abs(_ctl.MaxAspectRatioTest - MaxAspect) < 1e-7f
          && Mathf.Abs(_ctl.MinAspectRatioTest - 1.3333334f) > 1e-7f
          && Mathf.Abs(_ctl.MaxAspectRatioTest - 2.4444447f) > 1e-7f,
          $"★ B4 取的是**场景那一档** min {_ctl.MinAspectRatioTest:F9} / max {_ctl.MaxAspectRatioTest:F9}"
          + "（`MB1987`）—— ⛔ **不是** ctor 那一档 1.3333334 / 2.4444447");

        E(_ctl.AdjustedCount, 1, "★ B5 `Attach()` 当场算了一次（= 原版 `Start` 的 ①）");
        E(_ctl.SetCamerasRectCount, 1, "★ B5……并且真的写了一次 rect");

        S("B6 幂等：第二次 `EnsureAspectClamp` 不新加组件、不重复算");
        var again = ShellRuntime.EnsureAspectClamp(_cam);
        T(ReferenceEquals(again, _ctl), "★ B6 第二次拿到**同一件**");
        E(_cam.GetComponents<ScreenAspectRatioController>().Length, 1, "★ B6 相机上仍然**只有 1 件**（`GetComponent` 复用）");
        E(_ctl.AdjustedCount, 1, "★ B6 `Attach()` 幂等 ⇒ 没有再算一次");
        T(ReferenceEquals(ShellRuntime.AspectClampForTest, _ctl) && ShellRuntime.AspectClampCameraForTest == _cam,
          "★ B7 静态读口指向这件 / 这台相机（自检读的口与生产接线同源）");

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  C · 写 rect ⇒ `Camera.aspect` 跟着变（验收点名要钉的那一条）
        // ═══════════════════════════════════════════════════════════════════════════════════════
        S("C 写 rect：`Camera.rect` 四格 + `Camera.aspect` 当场跟着变");
        var v32 = Clamp(32f / 9f);
        float f32 = (1f - MaxAspect / (32f / 9f)) * 0.5f;
        _ctl.SetCamerasRect(v32);
        var r = _cam.rect;
        N(r.x, f32, 1e-5f, "★ C1 32:9 ⇒ 左缘 f = (1 − max/aspect)/2");
        N(r.y, 0f, 1e-6f, "★ C1 ……下缘 0（宽屏走的是**左右**黑边，不是上下）");
        N(r.width, 1f - 2f * f32, 1e-5f, "★ C1 ……宽 = 1 − 2f");
        N(r.height, 1f, 1e-6f, "★ C1 ……高 = 1");
        T(Mathf.Abs(f32 - 0.1562502f) < 1e-5f,
          $"★ C1 这个 f 的实值（{f32:F7} ≈ 0.15625，= 22:9 挤进 32:9 那一条）");

        // 🔴 这条是「两族读像素」的物理根：rect 一缩，`Camera.aspect` 就不再是屏宽高比。
        //    期望值用**Unity 自己的定义**算（pixelRect 宽 ÷ 高），⛔ 不读被测实现。
        float wantAspect = (r.width * Screen.width) / Mathf.Max(1f, r.height * Screen.height);
        N(_cam.aspect, wantAspect, 1e-3f,
          "★ C2 写 rect 之后 `Camera.aspect` = pixelRect 宽 ÷ 高（**当场**变，不是下一帧）");

        S("C3 整幅那一档：rect 写回 (0,0,1,1)、aspect 回到屏宽高比");
        _ctl.SetCamerasRect(Clamp(16f / 9f));
        var rf = _cam.rect;
        T(Mathf.Abs(rf.x) < 1e-6f && Mathf.Abs(rf.y) < 1e-6f
          && Mathf.Abs(rf.width - 1f) < 1e-6f && Mathf.Abs(rf.height - 1f) < 1e-6f,
          "★ C3 16:9 在 [min,max] 内 ⇒ 整幅（黑边不出现）");
        N(_cam.aspect, Screen.width / Mathf.Max(1f, (float)Screen.height), 1e-3f,
          "★ C3 ……aspect 回到屏宽高比");

        S("C4 鉴别力：只写 `targetCameras` 里那台");
        _ctl.SetCamerasRect(Clamp(32f / 9f));
        T(Mathf.Abs(_otherCam.rect.x) < 1e-6f && Mathf.Abs(_otherCam.rect.width - 1f) < 1e-6f,
          "★ C4 不相干那台相机**一格都没被写**（原版 `SetCamerasRect` 逐台走 `targetCameras`）");
        T(Mathf.Abs(_cam.rect.width - 1f) > 1e-3f, "★ C4 ……而目标那台确实被缩了（这一条与上一条结构上互斥）");

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  D · 分辨率变了 ⇒ 重算（原版 `ScreenResolutionChangeSignal` 的壳侧发射源）
        // ═══════════════════════════════════════════════════════════════════════════════════════
        S("D 分辨率变了重算：`ShellRuntime.TickAspectClamp()`");
        int nAdj = _ctl.AdjustedCount, nSet = _ctl.SetCamerasRectCount;

        ShellRuntime.SetAspectClampResolutionBaselineForTest(Screen.width, Screen.height);
        ShellRuntime.TickAspectClamp();
        E(_ctl.AdjustedCount, nAdj, "★ D1 基线 = 真屏 ⇒ **不重算**（⛔ 不是每帧都算）");
        E(_ctl.SetCamerasRectCount, nSet, "★ D1 ……rect 也没重写");

        ShellRuntime.SetAspectClampResolutionBaselineForTest(Screen.width + 1, Screen.height);
        ShellRuntime.TickAspectClamp();
        E(_ctl.AdjustedCount, nAdj + 1, "★ D2 屏宽变了 ⇒ 发射源抬了一次（调 `AdjustCameraViewport`）");
        E(_ctl.SetCamerasRectCount, nSet + 1, "★ D2 ……并且写了一次 rect");

        var wantLive = Clamp(Screen.width / Mathf.Max(1f, (float)Screen.height));
        var live = _cam.rect;
        N(live.x, wantLive.x, 1e-5f, "★ D4 重算用的是**当前屏**比例（x）");
        N(live.y, wantLive.y, 1e-5f, "★ D4 ……（y）");
        N(live.width, wantLive.width, 1e-5f, "★ D4 ……（宽）");
        N(live.height, wantLive.height, 1e-5f, "★ D4 ……（高）");

        ShellRuntime.TickAspectClamp();
        E(_ctl.AdjustedCount, nAdj + 1, "★ D3 紧接着再调 ⇒ 不再重算（基线已对齐，⛔ 不刷屏）");

        S("D5 当前这一屏没有夹取件（Intro / 战斗 / 过渡）⇒ 发射源安静跳过，不炸");
        ShellRuntime.ResetAspectClampStateForTest();
        ShellRuntime.SetAspectClampResolutionBaselineForTest(Screen.width + 1, Screen.height);
        var beforeNoCtl = _cam.rect;
        ShellRuntime.TickAspectClamp();                     // 不抛 = 过；再核一台相机都没被动
        T(Mathf.Abs(_cam.rect.x - beforeNoCtl.x) < 1e-6f && Mathf.Abs(_cam.rect.width - beforeNoCtl.width) < 1e-6f,
          "★ D5 没有夹取件时 `TickAspectClamp` 不写任何相机（基线照更新，不报错、不静默改画面）");
        _ctl = ShellRuntime.EnsureAspectClamp(_cam);        // 后面 E 段还要用

        // ═══════════════════════════════════════════════════════════════════════════════════════
        //  E · 「两族读像素」复核（简报点名要求；⛔ 本段是**钉现状**，不是改 `LayoutSpace`）
        // ═══════════════════════════════════════════════════════════════════════════════════════
        S("E1 `LayoutSpace.IsInsideScreen` 仍按**整屏**判（= 原版 `GetMousePerspectivePos` 的口径）");
        T(LayoutSpace.IsInsideScreen(new Vector2(0f, 0f)), "★ E1 (0,0) 在屏内");
        T(!LayoutSpace.IsInsideScreen(new Vector2(Screen.width, 0f)), "★ E1 (宽,0) 在屏外");
        T(!LayoutSpace.IsInsideScreen(new Vector2(0f, Screen.height)), "★ E1 (0,高) 在屏外");
        T(LayoutSpace.IsInsideScreen(new Vector2(Screen.width - 0.5f, Screen.height - 0.5f)),
          "★ E1 (宽−0.5, 高−0.5) 在屏内 —— 判据是 **Screen.width/height**，⛔ 不是相机 pixelRect"
          + "（原版 `BattleManager.GetMousePerspectivePos` 就是整屏判；改成 pixelRect 反而是偏离）");

        S("E2 主菜单模式卡视差的归一化屏幕 x（夹取盒子里要按 viewport 折算）");
        var prevLayout = LayoutSpace.Cam;
        LayoutSpace.Apply(_cam);                            // 只为把 `LayoutSpace.Cam` 指向夹具相机
        _ctl.SetCamerasRect(Clamp(32f / 9f));
        var vr = _cam.rect;
        N(MainMenuRuntime.ScreenNormalizedXForDesignPx(0f), vr.x, 1e-6f,
          "★ E2 设计 x=0 ⇒ 归一化屏幕 x = rect.x（盒子左缘，⛔ 不是 0 —— 左边是黑边）");
        N(MainMenuRuntime.ScreenNormalizedXForDesignPx(1920f), vr.x + vr.width, 1e-6f,
          "★ E2 设计 x=1920 ⇒ rect.x + rect.width（盒子右缘）");
        N(MainMenuRuntime.ScreenNormalizedXForDesignPx(960f), 0.5f, 1e-6f,
          "★ E2 设计 x=中心 ⇒ 0.5（居中那一格不受影响）");
        _ctl.SetCamerasRect(Clamp(16f / 9f));
        N(MainMenuRuntime.ScreenNormalizedXForDesignPx(1234.5f), 1234.5f / LayoutSpace.DesignPxW, 1e-7f,
          "★ E2 整幅那一档 ⇒ 与旧式 `设计x / 1920` **逐位相同**（所以 4:3~22:9 的既有读数一条都不动）");
        LayoutSpace.Cam = prevLayout;

        S("E3 布局基准跟着**夹取后的 aspect** 走（不是停在屏比例）");
        var prevCam = LayoutSpace.Cam;
        LayoutSpace.Apply(_cam);
        _ctl.SetCamerasRect(Clamp(32f / 9f));
        var vr3 = _cam.rect;
        float aspectAfterClamp = (vr3.width * Screen.width) / Mathf.Max(1f, vr3.height * Screen.height);
        N(LayoutSpace.VisibleWidth, LayoutSpace.DesignHeight * aspectAfterClamp, 1e-3f,
          "★ E3 `VisibleWidth` = 10 × 夹取后 aspect（⇒ 主菜单几何建在**盒子里**，黑边之外不铺内容）");
        LayoutSpace.Cam = prevCam;

        Cleanup();
        MenuCheck.Section(_sink, "B109_E16 合计");
        Debug.Log(_sink.P + $"合计：通过 {_sink.Pass} / 失败 {_sink.Fail}");
    }

    // ============================================================ 夹具 / 收尾

    static void BuildFixture()
    {
        ShellRuntime.ResetAspectClampStateForTest();
        _prevLayoutCam = LayoutSpace.Cam;

        // ⚠️ **不 tag MainCamera**：不吃掉宿主的 `Camera.main`（本文件全部显式喂相机）。
        // ⚠️ **不钉 `cam.aspect`**：钉了就测不出「写 rect 带出 aspect 变化」（宿主那 12 份钉 16:9 是对的，别动它们）。
        _camGo = new GameObject("B109E16 Camera");
        _cam = _camGo.AddComponent<Camera>();
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.orthographic = true;
        _cam.orthographicSize = 5f;

        _otherCamGo = new GameObject("B109E16 Camera (not a target)");
        _otherCam = _otherCamGo.AddComponent<Camera>();
        _otherCam.clearFlags = CameraClearFlags.SolidColor;
    }

    static void Cleanup()
    {
        if (_ctl != null) { _ctl.Detach(); _ctl = null; }
        ShellRuntime.ResetAspectClampStateForTest();
        if (_prevLayoutCam != null) LayoutSpace.Cam = _prevLayoutCam;
        if (_camGo != null) { Object.DestroyImmediate(_camGo); _camGo = null; _cam = null; }
        if (_otherCamGo != null) { Object.DestroyImmediate(_otherCamGo); _otherCamGo = null; _otherCam = null; }
    }

    /// <summary>**独立实现**的夹取矩形（= 原版 `__AdjustCameraViewport.c` 那三段；⛔ 不调被测件的
    /// `ComputeViewport` —— 那会变成自证）。常量 = 判据里那两个场景值。</summary>
    static Rect Clamp(float aspect)
    {
        if (aspect <= 0f) return new Rect(0f, 0f, 1f, 1f);
        if (aspect > MaxAspect)
        {
            float f = (1f - MaxAspect / aspect) * 0.5f;
            return new Rect(f, 0f, 1f - 2f * f, 1f);
        }
        if (aspect < MinAspect)
        {
            float f = (MinAspect / aspect - 1f) * 0.5f;     // 🔴 分子与上一支**不是**同一个式子
            return new Rect(0f, f, 1f, 1f - 2f * f);
        }
        return new Rect(0f, 0f, 1f, 1f);
    }
}
