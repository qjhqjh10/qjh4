// B143_W5_TraitScale_Assert.cs — 执行写手 `B143-W5`（2026-11-05）· 活状态行 `B142-TRAIT-SCALE`
//
// ============================ 它盯的是什么 ============================
//  原版：`Trait Tooltip.prefab` **根**上挂 `TransformScalerBySmallScreenUI`、`menuScale = 1.25`；
//  `BasicToolTip.prefab` **没有**这颗组件 ⇒ 小屏档开着时 **trait tooltip 整颗 ×1.25、普通 tooltip 恒 1**。
//  判据全文 → `资料/完全复刻/判据/B143_四条待判据.md` §3（`TransformScalerBySmallScreenUI__Initialize.c` /
//  `__LateUpdate.c` 两份反编译全文 + prefab 的 `menuScale` 序列化值）。
//
// ============================ 逐条为什么必须这么断 ============================
//  A1 常量：`Tooltip.TraitMenuScale == 1.25`（**且 ≠ 1**）—— 这是 prefab 的序列化值，⛔ 不是我们挑的。
//  A2 源码结构：`Core/Tooltip.cs` 里 `LayoutTrait` 与 `ClearTrait` **两条路都真的调了** `ApplyTraitScale`
//     （它们是以「同一物理行内追加」落地的 ⇒ 光看行为断不出「两条路都在」：只留一条也能过 A3/A4）。
//  B1 小屏 ON + trait ⇒ 根 `localScale.x == 1.25`、缩放器 `enabled`、`menuScale == 1.25`。
//  B2 ★灭自证★ 同一个实例切回**基础档** ⇒ 根回 `1`、`menuScale == 1`（= 原版 `BasicToolTip` 没有这颗组件）。
//     🧨 把 `ClearTrait` 的那句 `ApplyTraitScale(false)` 删掉 ⇒ B2 红（根会停在 1.25）。
//  B3 小屏 **OFF** + trait ⇒ 根仍 `1`、缩放器 `enabled == false`（`Initialize` 的 `smallScreenUI` 那一半）。
//     🧨 把 `Initialize()` 换成「直接 `enabled = true`」⇒ B3 红。
//  B4 再切回小屏 ON + trait ⇒ 根回到 `1.25`（证明 B3 那一趟没把状态搞坏 —— 常驻单例必须能来回切）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主里同一物理行内追加（`Editor/BattleScene.cs` 或 `Editor/ShellScene.cs` 的汇总行）：
//      B143_W5_TraitScale_Assert.Run(); _sink.Pass += B143_W5_TraitScale_Assert.Pass; _sink.Fail += B143_W5_TraitScale_Assert.Fail;
//  ⚠️ 本段会**反复建/拆** tooltip 单例（`Tooltip.InstanceForTest` 是**静态**的）——跑完已 `DestroyImmediate` 并把
//     `SmallScreenUI` 复位（`PersistOverride` 期间**不写 `PlayerPrefs`**，⛔ 不动玩家真设置）。
//  判绿红：按行首 `B143W5TS ✓` / `B143W5TS ✗` 数（⛔ 别 `grep -c ✗`）。
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B143-W5`：`Trait Tooltip` 的 1.25 整颗缩放（小屏档门控 + 回基础档复位）。</summary>
public static class B143_W5_TraitScale_Assert
{
    const string Tag = "[B143W5TS]";
    static int _pass, _fail;

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（" + got.ToString("F4") + " ≈ " + want.ToString("F4") + " ± " + tol.ToString("F4") + "）");
    }

    /// <summary>跑全部。可在任意宿主里跑（两档都不依赖画布：tooltip 会走世界档）。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log(Tag + " ==== `B142-TRAIT-SCALE`：`Trait Tooltip` 的 1.25 整颗缩放 ====");

        bool wasPersist = SmallScreenUI.PersistOverride;
        SmallScreenUI.PersistOverride = true;      // 自检不许写玩家真设置
        try
        {
            GroupA_ConstantsAndWiring();

            if (Tooltip.TooltipsSuppressed) Tooltip.ResetSuppressionsForTest();
            Check(!Tooltip.TooltipsSuppressed, "（前提）没有 owner 抑制 tooltip（抑制中 `Show` 会直接返回 ⇒ 下面几条不算数）");

            GroupB_Applied();
        }
        catch (System.Exception e)
        {
            Check(false, "断言段抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            DestroyInstance();
            SmallScreenUI.ResetForTest();
            SmallScreenUI.PersistOverride = wasPersist;
            Tooltip.ResetSuppressionsForTest();
            Debug.Log(Tag + " ==== 合计：" + _pass + " 通过 / " + _fail + " 失败 ====");
        }
    }

    // ==================================================================
    //  A · 常量与源码结构
    // ==================================================================
    static void GroupA_ConstantsAndWiring()
    {
        Near(Tooltip.TraitMenuScale, 1.25f, 0.0001f,
             "A1 ★ `TraitMenuScale == 1.25`（= 原版 `Trait Tooltip.prefab` 根 `TransformScalerBySmallScreenUI.menuScale`）"
           + "｜🧨 改成 1 ⇒ 本红且 B1 也红");
        Check(Mathf.Abs(Tooltip.TraitMenuScale - 1f) > 0.01f,
              "A1' ★ 灭自证：`TraitMenuScale` **不是 1**（= 1 就等于「原版没这颗组件」⇒ 这一整条活没意义）");

        string p = Application.dataPath + "/CardPresentation/Core/Tooltip.cs";
        Check(File.Exists(p), "A2（前提）读得到 `Core/Tooltip.cs`（" + p + "）");
        if (!File.Exists(p)) return;
        string src = File.ReadAllText(p);
        Check(src.Contains("ApplyTraitScale(true)"),
              "A2① `LayoutTrait` 那条路真的调了 `ApplyTraitScale(true)`｜🧨 删掉 ⇒ 本红（B1 也会红）");
        Check(src.Contains("ApplyTraitScale(false)"),
              "A2② `ClearTrait` 那条路真的调了 `ApplyTraitScale(false)`｜🧨 删掉 ⇒ 本红（B2 也会红）");
        Check(src.Contains("public partial class Tooltip"),
              "A2③ `Tooltip` 是 `partial`（新成员在 `Core/Tooltip.TraitScale.cs`；那条 partial 声明本身零行数变化）");
    }

    // ==================================================================
    //  B · 实例级：小屏 ON/OFF × trait/基础 四档
    // ==================================================================
    static void GroupB_Applied()
    {
        string traitBody = TipText.Trait("armour");
        Check(!string.IsNullOrEmpty(traitBody),
              "B0（前提）`TipText.Trait(\"armour\")` 取得到文案（拿不到 ⇒ 下面几条不算数；⛔ 不静默跳过）");
        if (string.IsNullOrEmpty(traitBody)) return;

        // ---- B1：小屏 ON + trait ⇒ 1.25 ----
        DestroyInstance();
        SmallScreenUI.Set(true);
        ShowBody(traitBody);
        Check(Tooltip.TraitModeForTest, "B1①（前提）真进了 trait 档（`TraitModeForTest`）");
        Near(Tooltip.TraitMenuScaleForTest, 1.25f, 0.0001f,
             "B1② 缩放器上的 `menuScale` 被设成 1.25（trait 档）");
        Check(Tooltip.TraitScalerEnabledForTest,
              "B1③ 缩放器 `enabled`（= 原版 `Initialize`：`menuScale != 1 && smallScreenUI`，两条都成立）");
        Near(Tooltip.RootScaleForTest, 1.25f, 0.0005f,
             "B1 ★ 小屏档下 trait tooltip 的**根 localScale.x == 1.25**（原版整颗放大）｜🧨 `TraitMenuScale` 改 1 ⇒ 本红");

        // ---- B2：同一实例切回基础档 ⇒ 复位 1（灭自证）----
        ShowBody("x");
        Check(!Tooltip.TraitModeForTest, "B2①（前提）切回了基础档（`TraitModeForTest == false`）");
        Near(Tooltip.TraitMenuScaleForTest, 1f, 0.0001f,
             "B2② `menuScale` 回到 1（基础档 = 原版 `BasicToolTip` 上根本没有这颗组件）");
        Near(Tooltip.RootScaleForTest, 1f, 0.0005f,
             "B2 ★★ 灭自证：切回基础档后根回到 **1**｜🧨 把 `ClearTrait` 里那句 `ApplyTraitScale(false)` 删掉 ⇒ 根停在 1.25 ⇒ 本红");

        // ---- B3：小屏 OFF + trait ⇒ 不动 ----
        DestroyInstance();
        SmallScreenUI.Set(false);
        ShowBody(traitBody);
        Check(Tooltip.TraitModeForTest, "B3①（前提）进了 trait 档");
        Check(!Tooltip.TraitScalerEnabledForTest,
              "B3② 小屏关着 ⇒ 缩放器 `enabled == false`（= `Initialize` 的 `smallScreenUI` 那一半）｜🧨 改成无条件 `enabled = true` ⇒ 本红");
        Near(Tooltip.RootScaleForTest, 1f, 0.0005f,
             "B3 ★ 小屏关着时 trait tooltip 的根仍是 **1**（原版 `LateUpdate` 不会被 Unity 调 ⇒ 不乘）｜🧨 无条件调 `Tick()` ⇒ 本红");

        // ---- B4：再切回小屏 ON + trait ⇒ 1.25（常驻单例能来回切）----
        DestroyInstance();
        SmallScreenUI.Set(true);
        ShowBody(traitBody);
        Near(Tooltip.RootScaleForTest, 1.25f, 0.0005f,
             "B4 ★ 关→开再弹一条 trait ⇒ 根回到 **1.25**（单例跨档不残留；原版每次 `Instantiate` 都重跑 `OnEnable`）");

        DestroyInstance();
    }

    // ==================================================================
    //  小工具
    // ==================================================================

    /// <summary>弹一条并收掉淡入（批处理没有帧循环，`_target` 停在 1 也不影响 `localScale`）。</summary>
    static void ShowBody(string body)
    {
        Tooltip.Show(body, Vector3.zero, 0);
        Tooltip.FinishFade();
    }

    /// <summary>把 tooltip 单例彻底拆掉（静态单例，留一只下来后面的用例会读到过期档位）。</summary>
    static void DestroyInstance()
    {
        var go = Tooltip.InstanceForTest;
        if (go == null) return;
        Tooltip.Hide();
        Tooltip.FinishFade();
        Object.DestroyImmediate(go);
    }
}
