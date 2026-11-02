// B120_W31_Assert.cs — 执行代理 `B120-W31`（2026-11-03）· 战斗域 · 设置窗「小屏档整窗 ×1.1」
//
// ============================ 它盯的是什么（三条，判据全在下面）================================
//  **原版**：`BattleSettingsWindow`（= `GameWindow` 子类）在**小屏开关打开**时，**窗口根**整体 ×1.1。
//    判据三条链：
//      ① 值：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4034.json`
//         → `"extraScaleSmallScreen": 1.100000023841858`（MB_4034 = 该窗的 `GameWindow` 基类实例）。
//      ② 机制：`GameWindow__Open.c`
//         `if (GameStaticData.smallScreenUI) { if (!Mathf.Approximately(extraScaleSmallScreen, 1f)) {
//              (GetComponent<TransformScalerBySmallScreenUI>() ?? gameObject.AddComponent<...>())
//                  .SetScale(extraScaleSmallScreen); } }`
//         （逐句转写 → `CardPresentation/Shell/TransformScalerBySmallScreenUI.cs` 文件头 ①；
//          同族实现 = `Shell/GameWindow.cs:571（ApplySmallScreenScale）`）。
//      ③ 组件本体：`TransformScalerBySmallScreenUI__{SetScale,Initialize,LateUpdate}.c`
//         `Initialize()`：`enabled = menuScale != 1f && GameStaticData.smallScreenUI`；
//         `LateUpdate()`：与「上次设过的值」**逐分量差平方和 < 0.0001** 就返回，否则
//         `localScale = localScale × menuScale` 并**回读**记账。
//  **我们**：`Battle/SettingsPanel.cs` 类尾 `ApplySmallScreenScale()`（`Show()` 里调，同一物理行追加 ——
//    文件被账按行号引用）+ `const ExtraScaleSmallScreen = 1.1f`；乘的节点 = `_panel`
//    （面板件公共父节点；`transform` 是空壳、乘它画面不动，判据见方法 doc）。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）================================
//  🧨 A（开关关）vs B（开关开）成对：A 断「连组件都不挂 + 缩放**逐位** = 1」；B 断「挂上 + 缩放 = 1.1」。
//     「无条件挂/无条件乘」⇒ A 红；「什么都不做」⇒ B 红。
//  🧨 把 `ExtraScaleSmallScreen` 改成 `1.0f`（原版语义 = **不覆盖**）⇒ `Mathf.Approximately` 那一支不成立
//     ⇒ `Initialize()` 走 `menuScale != 1` 的假支 ⇒ `enabled = false` ⇒ `Tick` 不乘 ⇒ **B3/B4 一起红**。
//     这条不需要额外断言：B1..B5 联合起来就是它的判别式（见 B4 的文案）。
//  🧨 B5（再 Tick 一次不再乘）防的是**守卫被删**：没有 0.0001 那道闸 ⇒ `Tick` 每次 ×1.1 ⇒ 1.21 / 1.331 ……
//     ⇒ 「乘一次」与「每次都乘」在单次断言下无法区分，必须成对。
//  🧨 C1（关窗再开仍是 1.1）防的是把乘法写进 `ApplySmallScreenScale`（每次 `Show()` 都乘一遍）。
//
// ============================ 怎么接（⚠️ 要主对话接 · 本代理不许改宿主）============================
//  在 `Editor/BattleScene.cs` 的方法尾**聚合行、合计那一条之前**（同一物理行追加，⛔ 行数零变化）：
//      B120_W31_Assert.Run(); pass += B120_W31_Assert.Pass; fail += B120_W31_Assert.Fail;
//  `Run()` **零前提**：不读 `BattleDriver`、不读场景、不读玩家存档；自己备相机 / 建面板，
//  `finally` 里逐样还原并销毁。
//  ⚠️ 它建的那块 `PopUps` 子画布（`SettingsPanel.Create` 自建，按名字幂等）**不销毁** —— 与战斗 HUD 共用；
//  但它自己的**面板帧**（`PanelRect`，挂在画布下、**不是** `sp.gameObject` 的子件）**必须单独销毁**，
//  否则会在画布上留一具孤儿面板（同族 `B117_G10_20_Assert` 只销毁了 `sp.gameObject`）。
//
// ============================ 两条边界（如实记）================================================
//  ① 批处理**没有帧循环** ⇒ 生产里由 `LateUpdate` 做的乘法，本件**手动调一次 `Tick()`** 复现
//     （同族先例：`Editor/ShellScene.cs` / `CollectionScene.cs` 的小屏缩放那一节）。
//  ② 本件**不动** `SmallScreenUI` 的落盘：全程 `PersistOverride = true`；退出时按原值还原内存态。
//     ⚠️ `ChosenManually` 没有写回口（`ResetForTest` 一律清 0）⇒ 若进本件前它是 `false`、出来仍是 `false`；
//     若原本就是 `true`，`Set(true)` 会把它再置一次真 —— 语义不变。

using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B120-W31`：战斗内设置窗「小屏档整窗 ×1.1」（原版 `extraScaleSmallScreen = 1.1`）。</summary>
public static class B120_W31_Assert
{
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B120W31 ✓ " + msg); }
        else { _fail++; Debug.LogError("B120W31 ✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F4") + " ± " + tol.ToString("F4")
                  + "；实得 " + got.ToString("F4") + "）");
    }

    /// <summary>四条一起跑。零前提：自己备相机与面板，收尾原样还原。</summary>
    public static void Run()
    {
        Camera camWas = LayoutSpace.Cam;
        GameObject camGo = null;
        SettingsPanel sp = null;
        bool persistWas = SmallScreenUI.PersistOverride;
        bool enabledWas = SmallScreenUI.Enabled;
        try
        {
            SmallScreenUI.PersistOverride = true;      // ⛔ 自检不许动玩家的真设置
            SmallScreenUI.ResetForTest();              // 出厂档：`Enabled = false`（原版 `cctor` 就是 0）

            // ---- 前提：uGUI 层要一块相机（`HudCanvas.Ensure` 没有相机就**不建**）----
            if (LayoutSpace.Cam == null)
            {
                var found = Object.FindObjectOfType<Camera>();
                if (found != null) LayoutSpace.Cam = found;
                else
                {
                    camGo = new GameObject("B120_W31_Cam");
                    var c = camGo.AddComponent<Camera>();
                    c.orthographic = true;
                    LayoutSpace.Cam = c;
                }
            }
            Check(LayoutSpace.Cam != null,
                  "（前提）`LayoutSpace.Cam` 有着落（否则 `HudCanvas` 建不出来，下面全白测）");
            if (LayoutSpace.Cam == null) return;

            sp = SettingsPanel.Create(null, null);
            Check(sp != null && sp.PanelRect != null,
                  "（前提）`SettingsPanel.Create` 建出面板帧"
                + "（建不出来 = 下面全白测；看上面那几行 LogError）");
            if (sp == null || sp.PanelRect == null) return;

            // ═══════════════════════════════════════════════════════════════════════════
            //  A 段：开关**关**（出厂档）—— 原版第一层判据不成立 ⇒ 连 `AddComponent` 都不做
            // ═══════════════════════════════════════════════════════════════════════════
            sp.Show();
            Check(sp.Visible, "（A 前提）面板确实开着（否则 A2 是空转）");
            Check(!SmallScreenUI.Enabled, "（A 前提）`SmallScreenUI.Enabled = false`（出厂档）");

            var scOff = sp.PanelRect.GetComponent<TransformScalerBySmallScreenUI>();
            Check(scOff == null,
                  "★ A1：开关**关**着 ⇒ 窗口根上**不挂** `TransformScalerBySmallScreenUI`"
                + "（原版 `GameWindow.Open()`：`if (GameStaticData.smallScreenUI)` 是第一层判据）"
                + " —— 写成「无条件 AddComponent」这条就红");
            var s0 = sp.PanelRect.localScale;
            Check(s0.x == 1f && s0.y == 1f && s0.z == 1f,
                  "★ A2：开关**关**着 ⇒ 窗口根缩放**逐位 = 1**（出厂行为一个数都不动；实得 "
                + s0.x.ToString("F6") + "," + s0.y.ToString("F6") + "," + s0.z.ToString("F6") + "）");

            // ═══════════════════════════════════════════════════════════════════════════
            //  B 段：开关**开** ⇒ 窗口根 ×1.1（值 = 原版 `extraScaleSmallScreen` 实读）
            // ═══════════════════════════════════════════════════════════════════════════
            SmallScreenUI.Set(true);                   // 生产里由主菜单设置的「图形」页写；这里直接置
            sp.Show();                                 // = 原版 `Open()`：每次都跑 apply 那一段
            var sc = sp.PanelRect.GetComponent<TransformScalerBySmallScreenUI>();
            Check(sc != null,
                  "★ B1：开关**开**着 ⇒ 窗口根上挂上 `TransformScalerBySmallScreenUI`"
                + "（原版 `Open()`：`GetComponent ?? AddComponent` 后 `SetScale(extraScaleSmallScreen)`）");
            if (sc == null) return;

            CheckNear(sc.menuScale, 1.1f, 1e-4f,
                      "★ B2：`menuScale` = 原版 `extraScaleSmallScreen` **1.1**"
                    + "（`MB_4034.json` 实读 1.100000023841858）");
            Check(sc.enabled,
                  "（B 前提）组件**已启用**（`Initialize()`：`menuScale != 1` 且开关开着）—— "
                + "没启用说明 B2 那个值被写成 1.0 了（原版语义：1.0 = **不覆盖**）");
            Check(sp.PanelRect.localScale.x == 1f,
                  "（B 前提）`Tick()` 之前 `localScale` 仍是 1 —— 批处理没有帧循环，"
                + "乘法只在生产里的 `LateUpdate` 发生（本件手动复现）");

            sc.Tick();     // = 原版 `LateUpdate` 的同一段逻辑（批处理没有帧循环）
            var s1 = sp.PanelRect.localScale;
            CheckNear(s1.x, 1.1f, 1e-4f, "★ B3a：小屏开 ⇒ 窗口根 `localScale.x` = 1.1");
            CheckNear(s1.y, 1.1f, 1e-4f, "★ B3b：小屏开 ⇒ 窗口根 `localScale.y` = 1.1");
            CheckNear(s1.z, 1.1f, 1e-4f, "★ B3c：小屏开 ⇒ 窗口根 `localScale.z` = 1.1");
            Check(s1.x > 1.05f,
                  "★ B4（灭自证）：缩放**确实大于** 1 —— 把 `ExtraScaleSmallScreen` 改成 `1.0f`"
                + "（= 原版「不覆盖」语义）⇒ `Initialize()` 走假支、`enabled=false`、`Tick` 不乘 ⇒ B3a–c 与 B4 一起红");

            sc.Tick();     // 第二次 —— 原版守卫：与「上次设过的值」逐分量差平方和 < 0.0001 就返回
            CheckNear(sp.PanelRect.localScale.x, 1.1f, 1e-4f,
                      "★ B5：**再 `Tick` 一次不再乘**（原版 `LateUpdate` 的 0.0001 守卫）"
                    + " —— 去掉那道闸这条就红（会变成 1.21）");

            // ═══════════════════════════════════════════════════════════════════════════
            //  C 段：关窗再开 —— 只 `SetScale`、不乘 ⇒ 仍是 1.1（不叠成 1.21）
            // ═══════════════════════════════════════════════════════════════════════════
            sp.Hide();
            sp.Show();
            sc.Tick();
            CheckNear(sp.PanelRect.localScale.x, 1.1f, 1e-4f,
                      "★ C1：关窗再开 ⇒ 窗口根仍是 **1.1**，不叠加成 1.21"
                    + "（原版每次 `Open()` 只 `SetScale`，乘法在 `LateUpdate` 且受守卫）"
                    + " —— 把乘号写进 `ApplySmallScreenScale` 这条就红");
        }
        catch (System.Exception e)
        {
            Check(false, "🧨 本件抛异常（下面那些断言**没跑**，别当它们绿了）：" + e);
        }
        finally
        {
            // 面板帧挂在**子画布**下（不是 `sp.gameObject` 的子件）⇒ 必须单独销毁，否则画布上留孤儿
            if (sp != null && sp.PanelRect != null) Object.DestroyImmediate(sp.PanelRect.gameObject);
            if (sp != null) Object.DestroyImmediate(sp.gameObject);
            // 还原内存态（`PersistOverride` 仍为 true ⇒ **不落盘**）；顺序：先 Set 再放回原旗标
            if (enabledWas) SmallScreenUI.Set(true); else SmallScreenUI.ResetForTest();
            SmallScreenUI.PersistOverride = persistWas;
            LayoutSpace.Cam = camWas;
            if (camGo != null) Object.DestroyImmediate(camGo);
        }
    }
}
