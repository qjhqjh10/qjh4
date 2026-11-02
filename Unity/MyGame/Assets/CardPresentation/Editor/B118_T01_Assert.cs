// B118_T01_Assert.cs — 执行代理 `B118-W8`（2026-11-02）的断言：`B118-T01` / `G10-02b` **A 档三件**
//   判据与施工单 → `资料/完全复刻/判据/B118_T01_环境02b施工单.md`（本代理自己那份，§1 #3/#8/#14、§2·①③④⑤）
//
// ============================ 它盯的是什么 ============================
// ① **`FlareScenarioToggler` 的缓动 = 原版 `OutQuad`**（`2x − x²`）。
//    判据：原版 `FlareScenarioToggler.DoScenarioBlend` = `DOTween.To(getter, setter, target, duration)`
//    **没调 `SetEase`** ⇒ 走 `Resources/DOTweenSettings.asset` 的 `defaultEaseType: 6` = `Ease.OutQuad`
//    （同一份资产判定见 `B117-G10-02a` 的 D4）。
//    ⚠️ **期望值是手算写死的**（`2t − t²`），⛔ 不是调 `EnvironmentApplier.OutQuad` 算出来的 —— 否则就是自证。
//    ⚠️ **不只比端点**：端点 0/1 对任何单调曲线都成立，所以这里查 0.1 / 0.25 / 0.5 / 0.6 / 0.9 五个内点。
// ② **两份 Renderer 资产的 `m_UseNativeRenderPass == 0`**（原版三份渲染器全 0；我们原来是 1）。
// ③ **三处订正过的注释仍带着判据** —— 挡「把注释改回去」（注释是下个会话唯一的记忆，改回去 = 重新走一遍错路）。

// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  接进任一宿主的聚合行（**同一物理行、该行第一个 `//` 之前**）：
//      B118T01_Assert.Run(); _sink.Pass += B118T01_Assert.Pass; _sink.Fail += B118T01_Assert.Fail;
//  建议落点 = `CardPresentation/Editor/BattleScene.cs:46982` 那条聚合行（与 `B117G1002a_Assert` 相邻）。
//  ⚠️ 本件**只读**（`EnvironmentApplier.OutQuad` 是纯函数；其余读的是**源码/资产文本**）——
//     不建对象、不碰 `RenderSettings`、不改资产 ⇒ 可以接在宿主里任何位置。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>`B118-T01` A 档的断言集（纯函数 + 文本判据，**不驱动生产对象**）。</summary>
public static class B118T01_Assert
{
    public static int Pass;
    public static int Fail;

    static void Check(bool ok, string what)
    {
        if (ok) { Pass++; Debug.Log("[B118T01] ✓ " + what); }
        else { Fail++; Debug.LogError("[B118T01] ✗ " + what); }
    }

    /// <summary>读 `Assets/` 下的一份文本文件。⚠️ 读不到返回 null（调用方必须把它**当红**，⛔ 不许静默跳过）。</summary>
    static string Read(string rel, out string err)
    {
        err = null;
        try
        {
            string p = Path.Combine(Application.dataPath, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(p)) { err = "文件不存在: " + p; return null; }
            return File.ReadAllText(p);
        }
        catch (Exception e) { err = e.GetType().Name + ": " + e.Message; return null; }
    }

    public static void Run()
    {
        // ------------------------------------------------------------------
        //  ① 缓动 = OutQuad（DOTween `Ease.OutQuad` = `2x − x²`；期望值手算写死）
        // ------------------------------------------------------------------
        //  0.10 → 0.200 − 0.0100 = 0.19      0.25 → 0.500 − 0.0625 = 0.4375
        //  0.50 → 1.000 − 0.2500 = 0.75      0.60 → 1.200 − 0.3600 = 0.84
        //  0.90 → 1.800 − 0.8100 = 0.99
        Check(Mathf.Abs(EnvironmentApplier.OutQuad(0f) - 0f) < 1e-6f, "OutQuad(0) = 0（端点）");
        Check(Mathf.Abs(EnvironmentApplier.OutQuad(0.1f) - 0.19f) < 1e-6f, "OutQuad(0.1) = 0.19（内点）");
        Check(Mathf.Abs(EnvironmentApplier.OutQuad(0.25f) - 0.4375f) < 1e-6f, "OutQuad(0.25) = 0.4375（内点）");
        Check(Mathf.Abs(EnvironmentApplier.OutQuad(0.5f) - 0.75f) < 1e-6f, "OutQuad(0.5) = 0.75（内点）");
        Check(Mathf.Abs(EnvironmentApplier.OutQuad(0.6f) - 0.84f) < 1e-5f, "OutQuad(0.6) = 0.84（内点，后段）");
        Check(Mathf.Abs(EnvironmentApplier.OutQuad(0.9f) - 0.99f) < 1e-5f, "OutQuad(0.9) = 0.99（内点，尾段）");
        Check(Mathf.Abs(EnvironmentApplier.OutQuad(1f) - 1f) < 1e-6f, "OutQuad(1) = 1（端点）");
        // 单调不减（21 个采样点）—— `Ease.OutQuad` 在 [0,1] 上单调增
        {
            bool mono = true; float prev = -1f;
            for (int i = 0; i <= 20; i++)
            {
                float v = EnvironmentApplier.OutQuad(i / 20f);
                if (v < prev - 1e-6f) { mono = false; break; }
                prev = v;
            }
            Check(mono, "OutQuad 在 [0,1] 上单调不减（21 点）");
        }
        // 夹取（原版 `LUTBlender.DoBlend` 那条夹取同族；这里是纯函数边界）
        Check(EnvironmentApplier.OutQuad(-0.5f) == 0f, "OutQuad(-0.5) 夹到 0");
        Check(EnvironmentApplier.OutQuad(2f) == 1f, "OutQuad(2) 夹到 1");
        // 灭自证：改回线性（`Clamp01(x)`）⇒ 0.5 处 0.75 vs 0.5 差 0.25 ⇒ 这条红
        Check(Mathf.Abs(EnvironmentApplier.OutQuad(0.5f) - 0.5f) > 0.2f, "OutQuad ≠ 线性（0.5 处差 0.25）");

        // ② 结构：`FlareScenarioToggler.Advance` 真的走这条路（⛔ 不是只把函数写在别处）
        {
            string err;
            string src = Read("CardPresentation/Battle/FlareScenarioToggler.cs", out err);
            Check(src != null, "读得到 FlareScenarioToggler.cs（前提）" + (err != null ? " · " + err : ""));
            if (src != null)
            {
                Check(src.Contains("EnvironmentApplier.OutQuad("), "Flare.Advance 用 `EnvironmentApplier.OutQuad(`");
                Check(!src.Contains("Mathf.Lerp(_from, _to, t)"), "Flare.Advance 不许退回线性 `Mathf.Lerp(_from,_to,t)`");
                Check(!src.Contains("缓动曲线因此与原版不同"), "Flare 文件头不再写着「缓动曲线与原版不同」（已对齐）");
            }
        }

        // ------------------------------------------------------------------
        //  ③ 两份 Renderer 资产的 `m_UseNativeRenderPass == 0`（原版三份全 0）
        //     引用链：`PC_Renderer.asset` ← `PC_RPAsset.asset:20` ← `GraphicsSettings.asset:88` + `QualitySettings`「PC」档
        //             `Mobile_Renderer.asset` ← `Mobile_RPAsset.asset:20` ← `QualitySettings`「Mobile」档
        // ------------------------------------------------------------------
        CheckRenderer("Settings/PC_Renderer.asset", "PC");
        CheckRenderer("Settings/Mobile_Renderer.asset", "Mobile");

        // ------------------------------------------------------------------
        //  ④ 三处订正过的注释（挡「改回去」—— 注释是下个会话唯一的记忆）
        // ------------------------------------------------------------------
        {
            string err;
            string t = Read("WarpforgeArena1/Editor/ArenaBuilder.cs", out err);
            Check(t != null, "读得到 ArenaBuilder.cs（前提）" + (err != null ? " · " + err : ""));
            if (t != null)
            {
                Check(t.Contains("base + overlay 相机栈"), "ArenaBuilder 注释写明「base + overlay 相机栈」判据");
                Check(!t.Contains("我们是两台各自独立的 base 相机"), "ArenaBuilder 不再写「我们是两台独立 base 相机」");
            }
        }
        {
            string err;
            string t = Read("WarpforgeVFX/Runtime/GrabPassTransparentFeature.cs", out err);
            Check(t != null, "读得到 GrabPassTransparentFeature.cs（前提）" + (err != null ? " · " + err : ""));
            if (t != null)
            {
                Check(t.Contains("settings.LayerMask.m_Bits: 64"), "GrabPass 注释写明原版 `LayerMask.m_Bits: 64`（层 6）");
                Check(!t.Contains("取的是哪一层：**查不到**"), "GrabPass 不再写「原版 LayerMask 查不到」");
            }
        }
        {
            string err;
            string t = Read("CardPresentation/Battle/EnvironmentApplier.cs", out err);
            Check(t != null, "读得到 EnvironmentApplier.cs（前提）" + (err != null ? " · " + err : ""));
            if (t != null)
            {
                Check(t.Contains("调度台 2026-11-02 裁定"), "EnvironmentApplier 的 🔶 标记填了裁定人与日期");
                Check(!t.Contains("TODO(谁定的)"), "EnvironmentApplier 不再留 `TODO(谁定的)`");
                Check(t.Contains("🔶 自己设计（非原版）：`Apply(null)`"), "`Apply(null)` 的自设计标记在位");
            }
        }
    }

    /// <summary>一份 Renderer 资产：必须有 `m_UseNativeRenderPass: 0`，且**不许**再出现 `: 1`。</summary>
    static void CheckRenderer(string rel, string tag)
    {
        string err;
        string t = Read(rel, out err);
        Check(t != null, "读得到 " + tag + " Renderer（前提）" + (err != null ? " · " + err : ""));
        if (t == null) return;
        Check(t.Contains("m_UseNativeRenderPass: 0"), tag + " Renderer 的 `m_UseNativeRenderPass == 0`");
        Check(!t.Contains("m_UseNativeRenderPass: 1"), tag + " Renderer 不再写 `m_UseNativeRenderPass: 1`");
    }
}
