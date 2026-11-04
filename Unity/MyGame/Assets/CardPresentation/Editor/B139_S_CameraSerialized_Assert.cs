// B139_S_CameraSerialized_Assert.cs — 执行写手 `B139-S`（2026-11-04）
//
// 修的那条链：`Editor/BattleScene.cs` 的 `BuildAndSaveScene()` 重建场景时，把 `Scenes/Battle.unity` 里
//   已经改好的**相机序列化值**写回旧值（包 Y 的活被下一次重生成整批覆盖）。本文件 = 「原版那一套值」的
//   **唯一一处**，由 `BuildAndSaveScene()` 存盘前调用（`:244`），存盘后回读校验（`:254`）。
//
// ============================ 判据（逐字段带出处） ============================
// 🥇 原版真身 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Battle Scenarios/Battle Arena 1.unity`
//    · BoardCamera：GO `&348`（`m_TagString: MainCamera`）· `!u!20 &1461` · `!u!114 &4374`(UACD) · `!u!81 &1594`(AudioListener)
//    · UI Camera  ：GO `&350`（`m_Name: UI Camera` / `m_TagString: UICamera`）· `!u!20 &1462` · `!u!114 &4453`(UACD)
// 🥈 我们这一侧的目标态 = `CardPresentation/Scenes/Battle.unity`（包 Y 已按原版改好，本包让它「持久」）。
//    包 Y 的逐字段报告 → `资料/完全复刻/报告/B139_Y_YAML与ProjectSettings.md` §一（6-01 / 6-02 / 6-03 / 6-05）。
//
// ============================ 为什么是「存盘前再写一遍」而不是把 HUD 相机直接改成透视 ============================
//  `BuildScene()` 建完之后那台 HUD 相机**必须是正交的**：整套布局（`LayoutSpace.VisibleHeight` / `Px()` /
//  `ScreenToWorld` / `HudCanvas.PlaneDistance = 20.3` 那套 z 推导）都按「可见高恒 10 世界单位 ⇒ 108 px/单位」算。
//  原版是「uGUI 画布 + `planeDistance 100`（1 世界单位 ≈ 14.83 px）」的另一套换算，而我们这套 2D 件是
//  **世界空间**对象（不是画布件）⇒ 相机一改透视，卡 / 棋盘 / 手牌会整体缩成 10 / 14.558 = **0.687 倍**
//  （连自检截图一起变）。所以：
//    · **运行期仍由 `LayoutSpace.Apply` 强制正交**（`BattleDriver.Start()` 每次都调）；
//    · 本类只在 `BuildAndSaveScene()` 里把**序列化值**写成原版那一套 ⇒ 之后再重生成**不会**把包 Y 的成果覆盖掉。
//  🔴 要「运行期也用透视」得先把整套 2D 世界件搬进画布（`T09` / 账 `A2098`），**不在本包**。
//     ⇒ 本文件**不动** `Core/LayoutSpace.cs`（它是全仓共用件，`DeckScene` / `ShellScene` 也在用）。
//
// ============================ HUD 相机的 `planeDistance` 为什么也不动 ============================
//  `HudCanvas.PlaneDistance = 20.3`（原版 100）**不是**「抄错的常数」，它是**我们这套分层 z 轴的承重件**：
//  `HudCanvas.cs` 文件头写着「相机在 z=−20 ⇒ `planeDistance = 目标 z + 20`」，`20.3` 让画布正好落在
//  `HudImageZ`(0.3) 那一层、与世界空间 quad 共用同一条 z 排序轴。改成 100 ⇒ 画布落到 z=80，
//  会**压在所有 HUD quad 前面**（它们全在 z ≤ 0.3）。⇒ 保留现值，理由记在账 `B139_S` 的备注列。
//
// ============================ 三组断言 ============================
//  A · 生成器源码（`Editor/BattleScene.cs`）写的是新值、旧值 **0 残留** —— 纯文本，不跑 Unity 也能跑
//  B · 存盘后**回读刚写出的那份 YAML**，逐字段比原版                —— `CheckSavedScene`（存盘后自动调）
//  C · 活相机对象逐字段读回（`ApplyToScene` 之后立刻）               —— 需要真的跑一次 `BuildAndSaveScene`
//  判绿红：`grep "^B139_S "` 的合计行；逐条按**行首** `B139_S ✓` / `B139_S ✗` 数（⛔ 别 `grep -c ✗` —— `✗` 也出现在文案里）。
//
// ============================ 怎么接 ============================
//  `Editor/BattleScene.cs:244` → `B139_S_CameraSerialized_Assert.ApplyToScene(cam);`
//  `Editor/BattleScene.cs:254` → `B139_S_CameraSerialized_Assert.CheckSavedScene(ScenePath);`
//  另外 `Run()` 是 A 组（源码扫描）的独立入口，任何宿主都可以直接调。

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>`B139-S`：把「原版相机序列化值」写成**唯一一处**判据，并在存盘前后各验一次。
/// 三组：A 源码扫描（不跑 Unity）/ B 存盘后回读 YAML / C 活相机读回。 ✅ 已复刻（2026-11-04 · B139-S · 判据 = 原版 `Battle Arena 1.unity` 的 `!u!20 &1461` / `&1462` + 两个 UACD + `!u!81 &1594`）</summary>
public static class B139_S_CameraSerialized_Assert
{
    const string P = "B139_S";
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    // ---------------------------------------------------------------- 原版值（判据 = 文件头那两个文件）
    public const string BoardTag = "MainCamera";          // 原版 GO `&348` m_TagString
    public const string HudTag   = "UICamera";            // 原版 GO `&350` m_TagString（TagManager.tags 第 4 条，包 Y 7-01 补）
    public const string HudName  = "UI Camera";           // 原版 GO `&350` m_Name

    /// <summary>两台相机的清屏色 —— 原版逐值相同，**alpha 0.0196 不是 1**（`m_BackGroundColor`）。</summary>
    public static readonly Color ClearColor =
        new Color(0.028763004f, 0.028794324f, 0.029411793f, 0.019607844f);

    // UI Camera（原版 `!u!20 &1462`）
    public const float HudFov = 40f, HudNear = 0.3f, HudFar = 300f, HudDepth = 0f;
    public const float HudFocal = 50f;
    public static readonly Vector2 HudSensor = new Vector2(36f, 24f);
    public const int HudProjectionMatrixMode = 1;         // Implicit（非物理相机）

    // BoardCamera（原版 `!u!20 &1461`）
    public const float BoardDepth = -1f, BoardFocal = 28f, BoardSensorY = 24f;
    public const float BoardFovFallback = 46.397182f, BoardNearFallback = 0.3f, BoardFarFallback = 300f;
    public const int BoardProjectionMatrixMode = 2;       // Physical（`usePhysicalProperties = true`）

    /// <summary>URP 的 `UniversalAdditionalCameraData` 脚本 guid（原版两台相机的 UACD `m_Script` 都是它）。</summary>
    const string UacdGuid = "a79441f348de89743a2939f4d699eac1";

    /// <summary>生成器源码（相对 `Application.dataPath`）。</summary>
    const string GeneratorRel = "CardPresentation/Editor/BattleScene.cs";

    // ---------------------------------------------------------------- Check / 合计
    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + " ✓ " + msg); }
        else { _fail++; Debug.LogError(P + " ✗ " + msg); }
    }

    static void Group(string name, Action body)
    {
        try { body(); }
        catch (Exception e)
        {
            Check(false, name + "：**这一段抛异常**（后面的断言没跑到，⛔ 别读成通过）—— "
                       + e.GetType().Name + ": " + e.Message);
        }
    }

    static void Total(string what)
    {
        Debug.Log(P + " 合计[" + what + "]：✓ " + _pass + " / ✗ " + _fail + "（共 " + (_pass + _fail) + " 条）");
    }

    // ================================================================ 入口 ①：存盘前把原版值写上去
    /// <summary>把两台相机的**序列化值**写成原版那一套（`BuildAndSaveScene()` 里 `BuildScene()` 之后、`SaveScene()` 之前调）。
    /// <para>⚠️ **只写「序列化值」**：运行期投影仍由 `LayoutSpace.Apply` 决定（见文件头第二节）。</para></summary>
    public static void ApplyToScene(Camera hud)
    {
        _pass = 0; _fail = 0;
        var board = FindBoardCamera();

        Group("A·HUD 相机写原版值", () => ApplyHudCamera(hud));
        if (board == null) Check(false, "A 组：**场景里没有 3D 战场相机**（`depth < 0` 一台都没有）⇒ "
                                       + "BoardCamera 那一半原版值没写进去（3D 没建出来时是真的没有，见 `BuildArena3D` 的出声）");
        else Group("A·BoardCamera 写原版值", () => ApplyBoardCamera(board));

        Group("C·活相机逐字段读回", () => CheckLive(hud, board));
        Total("活对象");
    }

    static Camera FindBoardCamera()
    {
        foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (c != null && c.depth < 0f) return c;
        return null;
    }

    /// <summary>HUD 那台（原版 `UI Camera`）—— 6-01 / 6-02 / 6-03 / 6-05 + UACD 三项。</summary>
    public static void ApplyHudCamera(Camera hud)
    {
        if (hud == null) { Check(false, "`hud` 为 null ⇒ 写不了（`BuildScene` 没返回相机？）"); return; }

        hud.gameObject.name = HudName;                       // 原版 GO &350 m_Name
        hud.gameObject.tag = HudTag;                         // 原版 GO &350 m_TagString
        hud.clearFlags = CameraClearFlags.Nothing;           // 原版 m_ClearFlags: 4
        hud.backgroundColor = ClearColor;                    // 原版 m_BackGroundColor
        hud.orthographic = false;                            // 原版 orthographic: 0
        hud.usePhysicalProperties = false;                   // ⇒ m_projectionMatrixMode: 1（Implicit）
        hud.fieldOfView = HudFov;                            // 原版 field of view: 40
        hud.nearClipPlane = HudNear;                         // 原版 near clip plane: 0.3
        hud.farClipPlane = HudFar;                           // 原版 far clip plane: 300
        hud.depth = HudDepth;                                // 原版 m_Depth: 0
        hud.gateFit = Camera.GateFitMode.Horizontal;         // 原版 m_GateFitMode: 2（与 BoardCamera 同值）
        hud.focalLength = HudFocal;                          // 原版 m_FocalLength: 50
        hud.sensorSize = HudSensor;                          // 原版 m_SensorSize: (36, 24)
        hud.lensShift = Vector2.zero;                        // 原版 m_LensShift: (0, 0)
        hud.allowHDR = false;                                // 原版 m_HDR: 0
        hud.allowMSAA = false;                               // 原版 m_AllowMSAA: 0
        hud.useOcclusionCulling = false;                     // 原版 m_OcclusionCulling: 0

        var d = hud.GetUniversalAdditionalCameraData();
        if (d == null) { Check(false, "HUD 相机取不到 `UniversalAdditionalCameraData`"); return; }
        d.renderShadows = false;                                        // 原版 m_RenderShadows: 0
        d.requiresDepthOption = CameraOverrideOption.Off;               // 原版 m_RequiresDepthTextureOption: 0
        d.requiresColorOption = CameraOverrideOption.Off;               // 原版 m_RequiresOpaqueTextureOption: 0
        // ⚠️ `m_RendererIndex` 原版是 1 / 我们 -1 —— **不动**：我们只有一份 URP renderer（越界会回退 0）。
        // ⚠️ `m_CullingMask` 原版 32（只画层 5）/ 我们 4294966015 —— **不动**：整套 HUD 件都在层 0，
        //    写 32 会让整个战斗 HUD 消失（包 Y §三·2 / 包 S 报告 §三）。两格都记在账里，不是漏掉。
    }

    /// <summary>3D 战场那台（原版 `BoardCamera`）—— 6-02 tag / 6-03 清屏色 / 6-05 HDR-MSAA-Occl / AudioListener / UACD。
    /// <para>⚠️ **光学那几格（focal/sensor/gateFit/fov/near/far/lensShift）不在这里** —— 它们的判据是**逐场清单**
    /// （`ArenaBuilder.ConfigureBoardCamera`，13 场 sensorSize.x 有两个取值），这里只补「与场无关」的那几格。</para></summary>
    public static void ApplyBoardCamera(Camera board)
    {
        if (board == null) { Check(false, "`board` 为 null ⇒ 写不了"); return; }

        board.gameObject.tag = BoardTag;                     // 原版 GO &348 m_TagString（⇒ Camera.main = 3D 相机）
        board.backgroundColor = ClearColor;                  // 原版 m_BackGroundColor（与 HUD 逐值相同）
        board.allowHDR = true;                               // 原版 m_HDR: 1
        board.allowMSAA = false;                             // 原版 m_AllowMSAA: 0
        board.useOcclusionCulling = false;                   // 原版 m_OcclusionCulling: 0

        var d = board.GetUniversalAdditionalCameraData();
        if (d == null) { Check(false, "BoardCamera 取不到 `UniversalAdditionalCameraData`"); return; }
        d.renderShadows = true;                                          // 原版 m_RenderShadows: 1
        d.requiresDepthOption = CameraOverrideOption.UsePipelineSettings; // 原版 m_RequiresDepthTextureOption: 2
        d.requiresColorOption = CameraOverrideOption.UsePipelineSettings; // 原版 m_RequiresOpaqueTextureOption: 2

        EnsureBoardAudioListener(board);
    }

    /// <summary>原版**全场唯一** `AudioListener` 挂在 BoardCamera 上（`!u!81 &1594` → GO `&348`）。
    /// <para>写成场景件之后，`BattleDriver.EnsureAudioListener` 会因「已有 1 台」直接 return（不会变两台）。</para></summary>
    public static void EnsureBoardAudioListener(Camera board)
    {
        if (board == null) return;
        if (board.GetComponent<AudioListener>() == null) board.gameObject.AddComponent<AudioListener>();
    }

    // ================================================================ C 组：活相机读回
    static void CheckLive(Camera hud, Camera board)
    {
        if (hud != null)
        {
            Check(hud.orthographic == false, "C1 HUD 相机是**透视**（现读 orthographic=" + hud.orthographic + "，原版 0）");
            Check(Near(hud.fieldOfView, HudFov), "C2 HUD fov = " + hud.fieldOfView.ToString("F4", Inv) + "（原版 40）");
            Check(Near(hud.nearClipPlane, HudNear) && Near(hud.farClipPlane, HudFar),
                  "C3 HUD 裁剪面 = " + hud.nearClipPlane.ToString("F4", Inv) + " / " + hud.farClipPlane.ToString("F4", Inv)
                  + "（原版 0.3 / 300）");
            Check(hud.clearFlags == CameraClearFlags.Nothing, "C4 HUD clearFlags = " + hud.clearFlags + "（原版 4 = Nothing）");
            Check(ColorNear(hud.backgroundColor, ClearColor), "C5 HUD 清屏色 = " + hud.backgroundColor + "（原版 " + ClearColor + "）");
            Check(hud.gameObject.name == HudName && hud.gameObject.tag == HudTag,
                  "C6 HUD GO 名/tag = " + hud.gameObject.name + " / " + hud.gameObject.tag + "（原版 UI Camera / UICamera）");
            Check(!hud.allowHDR && !hud.allowMSAA && !hud.useOcclusionCulling, "C7 HUD HDR/MSAA/Occl = 三元全 false（原版 0/0/0）");
            var d = hud.GetUniversalAdditionalCameraData();
            Check(d != null && !d.renderShadows && d.requiresDepthOption == CameraOverrideOption.Off
                  && d.requiresColorOption == CameraOverrideOption.Off,
                  "C8 HUD UACD renderShadows/depth/opaque = 0/0/0（原版 m_RenderShadows 0 · 两个 OverrideOption 0）");
        }

        if (board != null)
        {
            Check(board.gameObject.tag == BoardTag,
                  "C9 BoardCamera tag = " + board.gameObject.tag + "（原版 MainCamera ⇒ Camera.main 指 3D 相机）");
            Check(ColorNear(board.backgroundColor, ClearColor), "C10 BoardCamera 清屏色 = " + board.backgroundColor + "（原版与 HUD 同值）");
            Check(board.allowHDR && !board.allowMSAA && !board.useOcclusionCulling,
                  "C11 BoardCamera HDR/MSAA/Occl = " + board.allowHDR + "/" + board.allowMSAA + "/" + board.useOcclusionCulling + "（原版 1/0/0）");
            Check(board.GetComponent<AudioListener>() != null,
                  "C12 BoardCamera 上有 `AudioListener`（原版 !u!81 &1594 挂 GO &348）");
            Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1,
                  "C13 全场 `AudioListener` 恰好 1 台（原版全场景仅 1 台；多于 1 台 Unity 会报警并静音）");
            var d = board.GetUniversalAdditionalCameraData();
            Check(d != null && d.renderShadows
                  && d.requiresDepthOption == CameraOverrideOption.UsePipelineSettings
                  && d.requiresColorOption == CameraOverrideOption.UsePipelineSettings,
                  "C14 BoardCamera UACD renderShadows/depth/opaque = 1/2/2（原版 m_RenderShadows 1 · 两个 OverrideOption 2=UsePipelineSettings）");
        }
    }

    // ================================================================ 入口 ②：存盘后回读 YAML
    /// <summary>存盘后**回读刚写出的那份 YAML**，逐字段比原版 —— 这是「重生成不再写回旧值」的**硬证据**。
    /// <para>⚠️ 它读的是**文件**不是对象：`SaveScene` 之后对象上的值可能与落盘值不一致（`SetDirty` 那类坑），
    /// 只有回读文件才证明得了「下次打开场景看到的是原版值」。</para></summary>
    public static void CheckSavedScene(string scenePath)
    {
        _pass = 0; _fail = 0;
        Group("B·存盘后的 YAML", () => CheckSavedSceneCore(scenePath));
        Total("存盘 YAML");
    }

    static void CheckSavedSceneCore(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
        { Check(false, "场景文件不存在（`" + scenePath + "`）⇒ 存盘那一步没成功"); return; }

        var blocks = ParseBlocks(scenePath);
        Block board = null, hud = null;
        foreach (var b in blocks)
        {
            if (!IsHead(b, 20)) continue;                       // !u!20 = Camera
            string cf = Field(b, "m_ClearFlags");
            if (cf == "2") board = b;                           // 原版 BoardCamera: SolidColor
            else if (cf == "4") hud = b;                        // 原版 UI Camera : Nothing
        }
        Check(board != null, "B1 存盘后的场景里有 **BoardCamera**（`!u!20` 且 `m_ClearFlags: 2`）");
        Check(hud != null, "B2 存盘后的场景里有 **UI Camera**（`!u!20` 且 `m_ClearFlags: 4`）—— 这是「生成器写回 SolidColor」那条旧缺陷的哨兵");
        if (board == null || hud == null) return;

        // ---- BoardCamera ----
        Check(ColorField(board, "m_BackGroundColor", ClearColor), "B3 BoardCamera `m_BackGroundColor` = 原版 " + ClearColor
              + "（现读 " + Field(board, "m_BackGroundColor") + "）");
        Check(BoolField(board, "orthographic", false), "B4 BoardCamera `orthographic: 0`");
        Check(IntField(board, "m_Depth", (int)BoardDepth), "B5 BoardCamera `m_Depth: -1`（先画 3D）");
        Check(IntField(board, "m_HDR", 1), "B6 BoardCamera `m_HDR: 1`");
        Check(IntField(board, "m_AllowMSAA", 0), "B7 BoardCamera `m_AllowMSAA: 0`");
        Check(IntField(board, "m_OcclusionCulling", 0), "B8 BoardCamera `m_OcclusionCulling: 0`");
        Check(IntField(board, "m_projectionMatrixMode", BoardProjectionMatrixMode), "B9 BoardCamera `m_projectionMatrixMode: 2`(Physical)");
        Check(IntField(board, "m_GateFitMode", 2), "B10 BoardCamera `m_GateFitMode: 2`（与原版同值；由 `ConfigureBoardCamera` 写）");

        // 光学那三格**逐场**取值 ⇒ 拿清单里的期望值比，⛔ 不写死 46.397182
        float wantFov = BoardFovFallback, wantNear = BoardNearFallback, wantFar = BoardFarFallback;
        try
        {
            var mf = ArenaBuilder.LoadManifest(ArenaBuilder.ArenaFromEnv());
            if (mf != null && mf.camera != null)
            {
                if (mf.camera.fov > 0f) wantFov = mf.camera.fov;
                if (mf.camera.near > 0f) wantNear = mf.camera.near;
                if (mf.camera.far > 0f) wantFar = mf.camera.far;
            }
        }
        catch (Exception e) { Debug.LogWarning(P + " 读清单失败，光学三格退回默认档：" + e.Message); }
        Check(FloatField(board, "field of view", wantFov), "B11 BoardCamera `field of view` = 清单值 " + wantFov.ToString("F6", Inv));
        Check(FloatField(board, "near clip plane", wantNear) && FloatField(board, "far clip plane", wantFar),
              "B12 BoardCamera 裁剪面 = 清单值 " + wantNear.ToString("F3", Inv) + " / " + wantFar.ToString("F3", Inv));

        // ---- UI Camera ----
        Check(ColorField(hud, "m_BackGroundColor", ClearColor), "B13 UI Camera `m_BackGroundColor` = 原版 " + ClearColor
              + "（现读 " + Field(hud, "m_BackGroundColor") + "）");
        Check(BoolField(hud, "orthographic", false), "B14 UI Camera `orthographic: 0`（**透视**）");
        Check(FloatField(hud, "field of view", HudFov), "B15 UI Camera `field of view: 40`");
        Check(FloatField(hud, "near clip plane", HudNear) && FloatField(hud, "far clip plane", HudFar),
              "B16 UI Camera 裁剪面 `0.3 / 300`");
        Check(IntField(hud, "m_Depth", (int)HudDepth), "B17 UI Camera `m_Depth: 0`");
        Check(IntField(hud, "m_projectionMatrixMode", HudProjectionMatrixMode), "B18 UI Camera `m_projectionMatrixMode: 1`(Implicit)");
        Check(IntField(hud, "m_GateFitMode", 2), "B19 UI Camera `m_GateFitMode: 2`（与原版同值）");
        Check(IntField(hud, "m_HDR", 0), "B20 UI Camera `m_HDR: 0`");
        Check(IntField(hud, "m_AllowMSAA", 0), "B21 UI Camera `m_AllowMSAA: 0`");
        Check(IntField(hud, "m_OcclusionCulling", 0), "B22 UI Camera `m_OcclusionCulling: 0`");
        Check(FloatField(hud, "m_FocalLength", HudFocal) && VectorField(hud, "m_SensorSize", HudSensor)
              && VectorField(hud, "m_LensShift", Vector2.zero),
              "B23 UI Camera 物理相机三项 `focal 50 / sensor (36,24) / lensShift (0,0)`（透视下不参与，但原版序列化值就是这套）");

        // ---- GameObject 名 / tag ----
        var boardGo = FindById(blocks, 1, RefId(Field(board, "m_GameObject")));
        var hudGo = FindById(blocks, 1, RefId(Field(hud, "m_GameObject")));
        Check(boardGo != null && Field(boardGo, "m_TagString") == BoardTag,
              "B24 BoardCamera 的 GO `m_TagString` = MainCamera（现读 " + (boardGo != null ? Field(boardGo, "m_TagString") : "<找不到 GO>") + "）");
        Check(hudGo != null && Field(hudGo, "m_TagString") == HudTag && Field(hudGo, "m_Name") == HudName,
              "B25 UI Camera 的 GO = `" + HudName + "` / `" + HudTag + "`（现读 "
              + (hudGo != null ? Field(hudGo, "m_Name") + " / " + Field(hudGo, "m_TagString") : "<找不到 GO>") + "）");

        // ---- AudioListener：全场唯一、且在 BoardCamera 上 ----
        string boardGoId = RefId(Field(board, "m_GameObject"));
        int listeners = 0; bool onBoard = false;
        foreach (var b in blocks)
        {
            if (!IsHead(b, 81)) continue;                        // !u!81 = AudioListener
            listeners++;
            if (RefId(Field(b, "m_GameObject")) == boardGoId) onBoard = true;
        }
        Check(listeners == 1 && onBoard, "B26 全场 `AudioListener` 恰好 1 台且挂在 BoardCamera 的 GO 上"
              + "（现读 " + listeners + " 台、" + (onBoard ? "其中一台在 BoardCamera" : "BoardCamera 上没有") + "；原版 !u!81 &1594 → GO &348）");

        // ---- UACD 三格 ----
        var boardData = FindUacd(blocks, boardGoId);
        var hudData = FindUacd(blocks, RefId(Field(hud, "m_GameObject")));
        Check(boardData != null && IntField(boardData, "m_RenderShadows", 1) == true
              && IntField(boardData, "m_RequiresDepthTextureOption", 2) && IntField(boardData, "m_RequiresOpaqueTextureOption", 2),
              "B27 BoardCamera 的 UACD = renderShadows 1 / depth 2 / opaque 2（原版 !u!114 &4374）");
        Check(hudData != null && IntField(hudData, "m_RenderShadows", 0)
              && IntField(hudData, "m_RequiresDepthTextureOption", 0) && IntField(hudData, "m_RequiresOpaqueTextureOption", 0),
              "B28 UI Camera 的 UACD = renderShadows 0 / depth 0 / opaque 0（原版 !u!114 &4453；改前是 1/2/2 —— 包 Y 6-01 那条）");
    }

    // ================================================================ 入口 ③：A 组（源码扫描，不用跑 Unity）
    /// <summary>扫 `Editor/BattleScene.cs` 的**活代码行**：新值在、旧值 0 残留。
    /// <para>它挡的是「有人把这几行改回去」——⛔ 挡不了「代码跑了但没生效」，那一半归 B/C 组。</para></summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Group("A·生成器源码", () =>
        {
            string path = Path.Combine(Application.dataPath, GeneratorRel);
            var lines = LiveLines(path);
            if (lines.Count == 0) { Check(false, "读不到 `" + path + "` ⇒ A 组一条都证明不了"); return; }

            Check(Has(lines, "new GameObject(\"UI Camera\")"), "A1 HUD 相机建成 `UI Camera`（原版 GO &350 m_Name）");
            Check(Has(lines, "camGo.tag = \"UICamera\";"), "A2 HUD 相机 tag = `UICamera`（原版 GO &350）");
            Check(Has(lines, "cam.clearFlags = CameraClearFlags.Nothing;"), "A3 HUD 相机 clearFlags = Nothing(4)（原版 m_ClearFlags: 4）");
            Check(Has(lines, "cam.backgroundColor = new Color(0.028763004f, 0.028794324f, 0.029411793f, 0.019607844f);"),
                  "A4 HUD 相机清屏色 = 原版四元组");
            Check(Has(lines, "c.backgroundColor = new Color(0.028763004f, 0.028794324f, 0.029411793f, 0.019607844f);"),
                  "A5 BoardCamera 清屏色 = 原版四元组（两台同值）");
            Check(Has(lines, "go.tag = \"MainCamera\";"), "A6 BoardCamera tag = `MainCamera`（原版 GO &348）");
            Check(Has(lines, "B139_S_CameraSerialized_Assert.ApplyToScene(cam);"), "A7 `BuildAndSaveScene` 存盘前**真的调**了本类（`ApplyToScene`）");
            Check(Has(lines, "B139_S_CameraSerialized_Assert.CheckSavedScene(ScenePath);"), "A8 存盘后**真的调**了回读校验（`CheckSavedScene`）");
            Check(Has(lines, "LayoutSpace.Apply(cam);"), "A9 运行期仍走 `LayoutSpace.Apply`（正交布局基准没被动掉 —— 见文件头第二节）");

            Check(CountOf(lines, "new Color(0.055f, 0.06f, 0.08f)") == 0,
                  "A10 旧清屏色 `(0.055,0.06,0.08)` **0 残留**（现读 " + CountOf(lines, "new Color(0.055f, 0.06f, 0.08f)") + " 处）");
            Check(CountOf(lines, "new GameObject(\"Main Camera\")") == 0,
                  "A11 旧相机名 `Main Camera` **0 残留**（现读 " + CountOf(lines, "new GameObject(\"Main Camera\")") + " 处）");
            Check(CountOf(lines, "camGo.tag = \"MainCamera\"") == 0,
                  "A12 旧的「HUD 相机抢 MainCamera」**0 残留**（现读 " + CountOf(lines, "camGo.tag = \"MainCamera\"") + " 处）");
        });
        Total("源码扫描");
    }

    // ================================================================ 源码行小工具
    /// <summary>读文件、**只留活代码行**（`//` 开头的整行注释不算 —— 留痕注释里提到旧值是故意的）。</summary>
    static List<string> LiveLines(string path)
    {
        var list = new List<string>();
        if (!File.Exists(path)) return list;
        foreach (var l0 in File.ReadAllLines(path))
        {
            if (l0.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
            list.Add(l0);
        }
        return list;
    }

    static bool Has(List<string> lines, string must)
    {
        foreach (var l in lines) if (l.Contains(must)) return true;
        return false;
    }

    static int CountOf(List<string> lines, string sub)
    {
        int n = 0;
        foreach (var l in lines)
        {
            int i = 0;
            while ((i = l.IndexOf(sub, i, StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        }
        return n;
    }

    // ================================================================ Unity YAML 小工具（`--- !u!<cls> &<id>` 分块）
    sealed class Block { public string Head; public List<string> Lines = new List<string>(); }

    static List<Block> ParseBlocks(string path)
    {
        var res = new List<Block>();
        Block cur = null;
        foreach (var raw in File.ReadAllLines(path))
        {
            if (raw.StartsWith("--- !u!", StringComparison.Ordinal))
            {
                cur = new Block { Head = raw.Trim() };
                res.Add(cur);
            }
            else if (cur != null) cur.Lines.Add(raw);
        }
        return res;
    }

    static bool IsHead(Block b, int cls)
    {
        return b != null && b.Head.StartsWith("--- !u!" + cls + " &", StringComparison.Ordinal);
    }

    static Block FindById(List<Block> blocks, int cls, string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var b in blocks)
        {
            if (!IsHead(b, cls)) continue;
            string tail = b.Head.Substring(("--- !u!" + cls + " &").Length).Trim();
            if (tail == id) return b;
        }
        return null;
    }

    /// <summary>`  key: value` 那一行的 value（只认**两个空格缩进**的顶格字段）。</summary>
    static string Field(Block b, string key)
    {
        if (b == null) return null;
        string pref = "  " + key + ":";
        foreach (var l in b.Lines)
            if (l.StartsWith(pref, StringComparison.Ordinal)) return l.Substring(pref.Length).Trim();
        return null;
    }

    static string RefId(string fieldValue)
    {
        if (string.IsNullOrEmpty(fieldValue)) return null;
        const string k = "fileID:";
        int i = fieldValue.IndexOf(k, StringComparison.Ordinal);
        if (i < 0) return null;
        string rest = fieldValue.Substring(i + k.Length).Trim();
        int j = rest.IndexOfAny(new[] { ' ', '}', ',' });
        return j >= 0 ? rest.Substring(0, j).Trim() : rest.Trim();
    }

    static Block FindUacd(List<Block> blocks, string goId)
    {
        if (string.IsNullOrEmpty(goId)) return null;
        foreach (var b in blocks)
        {
            if (!IsHead(b, 114)) continue;
            string s = Field(b, "m_Script");
            if (s == null || s.IndexOf(UacdGuid, StringComparison.Ordinal) < 0) continue;
            if (RefId(Field(b, "m_GameObject")) == goId) return b;
        }
        return null;
    }

    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    static bool Near(float a, float b)
    {
        return Mathf.Abs(a - b) <= Mathf.Max(1e-4f, Mathf.Abs(b) * 1e-5f);
    }

    static bool ColorNear(Color a, Color b)
    {
        return Near(a.r, b.r) && Near(a.g, b.g) && Near(a.b, b.b) && Near(a.a, b.a);
    }

    static bool FloatField(Block b, string key, float want)
    {
        string v = Field(b, key);
        float f;
        if (v == null || !float.TryParse(v, NumberStyles.Float, Inv, out f)) return false;
        return Near(f, want);
    }

    static bool IntField(Block b, string key, int want)
    {
        string v = Field(b, key);
        int i;
        if (v == null || !int.TryParse(v, NumberStyles.Integer, Inv, out i)) return false;
        return i == want;
    }

    static bool BoolField(Block b, string key, bool want) { return IntField(b, key, want ? 1 : 0); }

    static bool ColorField(Block b, string key, Color want)
    {
        string v = Field(b, key);
        if (string.IsNullOrEmpty(v)) return false;
        float r, g, bb, a;
        if (!TryKeyed(v, "r", out r) || !TryKeyed(v, "g", out g) || !TryKeyed(v, "b", out bb) || !TryKeyed(v, "a", out a)) return false;
        return ColorNear(new Color(r, g, bb, a), want);
    }

    static bool VectorField(Block b, string key, Vector2 want)
    {
        string v = Field(b, key);
        if (string.IsNullOrEmpty(v)) return false;
        float x, y;
        if (!TryKeyed(v, "x", out x) || !TryKeyed(v, "y", out y)) return false;
        return Near(x, want.x) && Near(y, want.y);
    }

    /// <summary>从 `{r: 0.028763004, g: …, b: …, a: …}` 里取一个键的浮点值。</summary>
    static bool TryKeyed(string s, string key, out float val)
    {
        val = 0f;
        int i = s.IndexOf(key + ":", StringComparison.Ordinal);
        if (i < 0) return false;
        string rest = s.Substring(i + key.Length + 1).TrimStart();
        int j = rest.IndexOfAny(new[] { ',', '}' });
        if (j >= 0) rest = rest.Substring(0, j);
        return float.TryParse(rest.Trim(), NumberStyles.Float, Inv, out val);
    }
}
