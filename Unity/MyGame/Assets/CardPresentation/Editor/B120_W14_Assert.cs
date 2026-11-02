// B120_W14_Assert.cs — URP 资产「剩余偏离」的断言（执行代理 `B120-W14`，2026-11-02）
//
// ============================ 它盯的是什么 ============================
// 本批 = `B120-W6` 做完 6 项主设置之后**剩下的** RPAsset 8 项 / Renderer 3 项里的**可改项**：
// ① **RPAsset 7 格**（逐格对原版 `UniversalRenderPipelineAsset High.asset`）
//    SupportsTerrainHoles · UseFastSRGBLinearConversion · SupportScreenSpaceLensFlare ·
//    VolumeFrameworkUpdateMode · Cascade2Split · Cascade4Split · 附加光阴影 resolution tier ×3
// ② **Renderer 3 格**：`m_ShadowTransparentReceive` · 模板 `stencilReference` / `stencilCompareFunction`
// ③ **灭自证**：改前的旧值（1 / 0 / 0.25 / 0.12299999 / 256/512/1024 / ref 1 / cmp 3 …）**不许回来**
//
// ⛔ **本件不断言的两项**（本批**只调查、不改**，见 `报告/B120_W14_执行.md` §三）：
//    - `m_RendererDataList` 原版 2 个 vs 我们 1 个（缺的是 `UniversalRenderPipelineAsset_Renderer UI`，
//      与 `D-1`/`B10` 的 HUD 相机专用 renderer 同源 ⇒ 结构性，等真机 A/B）
//    - 三条 layerMask `4294967295` vs 原版 `1207963447`（层方案根因，与 `D-1`/`E7` 绑死）
//    ⇒ **故意不写「必须等于 1」或「必须等于 4294967295」的断言**：那样会把「已知缺口」钉死，
//      将来补上 UI renderer / 层方案时反而报红。
//
// ============================ 期望值从哪来 ============================
// 全部是**原版判据里的数**（⛔ 不是「我们改完以后的值」）：
//   `d:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/UniversalRenderPipelineAsset High.asset`
//     :26 SupportsTerrainHoles 0 · :50 附加光阴影 0 · :52-54 tier 128/256/512 · :60 Cascade2Split 0.2265588
//     :61 Cascade3Split (0.1, 0.3) · :62 Cascade4Split (0.067, 0.2, 0.467) · :83 UseFastSRGBLinearConversion 1
//     :85 SupportScreenSpaceLensFlare 0 · :94 VolumeFrameworkUpdateMode 1 · :22 m_DefaultRendererIndex 0
//   `…/UniversalRenderPipelineAsset_Renderer.asset`
//     :44 stencilReference 0 · :45 stencilCompareFunction 8 · :43 overrideStencilState 0 · :49 ShadowTransparentReceive 0
//   「模板那两格今天不生效」的引擎判据（本机 URP 包，同 W6 那份）：
//     `UniversalRenderer.cs:285-290` 先把 `m_DefaultStencilState` 建好，
//     `:286 m_DefaultStencilState.enabled = stencilData.overrideStencilState;`（=0 ⇒ 关）
//     `Passes/DrawObjectsPass.cs:101 if (stencilState.enabled) { … stencilReference / stencilState … }`
//     ⇒ `overrideStencilState = 0` 时 **reference 与 compareFunction 都不写进 RenderStateBlock** = 死值；
//        按铁律 18·0「默认复刻原版（含死值）」照样抄成 0 / 8。
//   「VolumeFrameworkUpdateMode 改 1 不改行为」的判据：
//     相机级 `UniversalAdditionalCameraData.cs:727` 只在 `m_VolumeFrameworkUpdateModeOption == UsePipelineSettings(0)`
//     时才回落到资产值；我们的两台相机都是 **2(ViaScripting)**（`Battle.unity:1514` / `:1763` 的 `m_VolumeFrameworkUpdateModeOption`）
//     ⇒ 改资产只是把序列化值对齐，净行为不变。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  接进 `CardPresentation/Editor/BattleScene.cs:46983` 那条**方法尾聚合行**：
//  在 `Debug.Log(P + $"=== 合计：…")` **之前**、同一物理行、**该行第一个 `//` 之前**追加：
//      B120_W14_Assert.Run(); pass += B120_W14_Assert.Pass; fail += B120_W14_Assert.Fail;
//  ⚠️ 本件**只读两份资产文本**（无对象、无场景、无生产对象驱动）⇒ 位置不敏感；放「合计」之前是为了印进合计。
using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>`B120` D 域剩余项（RPAsset 7 格 + Renderer 3 格）的断言集（只读文本，**不驱动生产对象**）。</summary>
public static class B120_W14_Assert
{
    public static int Pass;
    public static int Fail;

    static void Check(bool ok, string what)
    {
        if (ok) { Pass++; Debug.Log("[B120W14] ✓ " + what); }
        else { Fail++; Debug.LogError("[B120W14] ✗ " + what); }
    }

    /// <summary>读 `Assets/` 下的文本文件。⚠️ 读不到返回 null（调用方必须把它**当红**，⛔ 不许静默跳过）。</summary>
    static string ReadAsset(string rel, out string err)
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

    /// <summary>取 YAML 里第一条 `key: value` 的 value（按行首匹配，容忍前导空格）。</summary>
    static string Field(string text, string key)
    {
        if (text == null) return null;
        string need = key + ":";
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string s = lines[i].TrimStart();
            if (s.StartsWith(need, StringComparison.Ordinal))
                return s.Substring(need.Length).Trim();
        }
        return null;
    }

    /// <summary>以某一行（如 `m_DefaultStencilState:`）为界，只在这一块里找 `key:`。</summary>
    static string BlockField(string text, string nameLine, string key)
    {
        if (text == null) return null;
        int idx = text.IndexOf(nameLine, StringComparison.Ordinal);
        if (idx < 0) return null;
        int end = text.IndexOf("--- !u!", idx + nameLine.Length, StringComparison.Ordinal);
        string block = end < 0 ? text.Substring(idx) : text.Substring(idx, end - idx);
        return Field(block, key);
    }

    static bool Eq(string got, string want) => string.Equals(got, want, StringComparison.Ordinal);

    static bool Near(string got, float want, float eps = 1e-6f)
    {
        if (string.IsNullOrEmpty(got)) return false;
        float v;
        if (!float.TryParse(got, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return false;
        return Mathf.Abs(v - want) < eps;
    }

    /// <summary>解析 `{x: 0.067, y: 0.2, z: 0.467}` 这种 N 分量值，逐分量比（容忍 1e-6）。
    /// `want.Length` 必须等于实际分量数（`Cascade3Split` 是 2 个、`Cascade4Split` 是 3 个）。</summary>
    static bool Vec(string got, float[] want, out string detail)
    {
        detail = got ?? "<null>";
        if (string.IsNullOrEmpty(got) || want == null || want.Length == 0) return false;
        string s = got.Replace("{", "").Replace("}", "").Replace(" ", "");
        string[] parts = s.Split(',');
        if (parts.Length != want.Length) return false;
        for (int i = 0; i < parts.Length; i++)
        {
            string q = parts[i];
            int c = q.IndexOf(':');
            if (c >= 0) q = q.Substring(c + 1);
            float v;
            if (!float.TryParse(q, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return false;
            if (Mathf.Abs(v - want[i]) >= 1e-6f) return false;
        }
        return true;
    }

    /// <summary>把某个 key 的整段 `- {fileID…}` 列表取出来（`m_RendererDataList` 那种块）。</summary>
    static string ListBlock(string text, string key)
    {
        if (text == null) return null;
        int i = text.IndexOf(key + ":", StringComparison.Ordinal);
        if (i < 0) return null;
        int end = text.IndexOf("\n  m_", i + key.Length, StringComparison.Ordinal);
        return end < 0 ? text.Substring(i) : text.Substring(i, end - i);
    }

    public static void Run()
    {
        string err;
        string rp = ReadAsset("Settings/PC_RPAsset.asset", out err);
        Check(rp != null, "读得到 Settings/PC_RPAsset.asset（前提）" + (err != null ? " · " + err : ""));
        string rd = ReadAsset("Settings/PC_Renderer.asset", out err);
        Check(rd != null, "读得到 Settings/PC_Renderer.asset（前提）" + (err != null ? " · " + err : ""));

        // ==================================================================
        //  ① RPAsset 7 格（期望值 = 原版 High，见文件头）
        // ==================================================================
        if (rp != null)
        {
            Check(Eq(Field(rp, "m_SupportsTerrainHoles"), "0"),
                  "SupportsTerrainHoles == 0（原版 High:26）");
            Check(Eq(Field(rp, "m_UseFastSRGBLinearConversion"), "1"),
                  "UseFastSRGBLinearConversion == 1（原版 High:83；原版走近似 gamma→linear 快路）");
            Check(Eq(Field(rp, "m_SupportScreenSpaceLensFlare"), "0"),
                  "SupportScreenSpaceLensFlare == 0（原版 High:85）");
            Check(Eq(Field(rp, "m_VolumeFrameworkUpdateMode"), "1"),
                  "VolumeFrameworkUpdateMode == 1 = ViaScripting（原版 High:94；相机级 option=2 会盖过 ⇒ 净行为不变）");
            Check(Near(Field(rp, "m_Cascade2Split"), 0.2265588f),
                  "Cascade2Split == 0.2265588（原版 High:60）");

            string c3 = Field(rp, "m_Cascade3Split"), c4 = Field(rp, "m_Cascade4Split"), d;
            Check(Vec(c3, new float[] { 0.1f, 0.3f }, out d),
                  "Cascade3Split == {x: 0.1, y: 0.3}（原版 High:61；这一格本来就一致，防被顺手改坏）· 现值 " + d);
            Check(Vec(c4, new float[] { 0.067f, 0.2f, 0.467f }, out d),
                  "Cascade4Split == {x: 0.067, y: 0.2, z: 0.467}（原版 High:62）· 现值 " + d);

            Check(Eq(Field(rp, "m_AdditionalLightsShadowResolutionTierLow"), "128"),
                  "附加光阴影 tier Low == 128（原版 High:52）");
            Check(Eq(Field(rp, "m_AdditionalLightsShadowResolutionTierMedium"), "256"),
                  "附加光阴影 tier Medium == 256（原版 High:53）");
            Check(Eq(Field(rp, "m_AdditionalLightsShadowResolutionTierHigh"), "512"),
                  "附加光阴影 tier High == 512（原版 High:54）");

            // 配套前提：附加光阴影是关的（tier 才「只影响变体」）；这两条同时成立才算对齐
            Check(Eq(Field(rp, "m_AdditionalLightShadowsSupported"), "0"),
                  "附加光阴影开关 == 0（原版 High:50；tier 三格只在这条为 0 时无实际作用）");
            Check(Eq(Field(rp, "m_AdditionalLightsShadowmapResolution"), "512"),
                  "附加光阴影贴图 == 512（原版 High:51；与 tier 同一组）");
            Check(Eq(Field(rp, "m_DefaultRendererIndex"), "0"),
                  "m_DefaultRendererIndex == 0（原版 High:22；两台的默认 renderer 都是主 renderer）");

            // 🔎 只调查项的证据锚（**不断言数量**，见文件头）：列表非空且含我们的 renderer
            string list = ListBlock(rp, "m_RendererDataList");
            Check(!string.IsNullOrEmpty(list) && list.IndexOf("{fileID:", StringComparison.Ordinal) >= 0,
                  "m_RendererDataList 非空（前提；原版 High:19-21 是 2 个 = 主 renderer + UI renderer，"
                  + "我们 1 个 —— 缺的那份是 `D-1`/`B10` 的结构性缺口，本件不钉数量）");
        }

        // ==================================================================
        //  ② Renderer 3 格（模板两格今天不生效，仍照原版抄 —— 死值也复刻）
        // ==================================================================
        if (rd != null)
        {
            Check(Eq(Field(rd, "m_ShadowTransparentReceive"), "0"),
                  "m_ShadowTransparentReceive == 0（原版 UniversalRenderPipelineAsset_Renderer.asset:49）");

            string ovr = BlockField(rd, "m_DefaultStencilState:", "overrideStencilState");
            string refv = BlockField(rd, "m_DefaultStencilState:", "stencilReference");
            string cmp = BlockField(rd, "m_DefaultStencilState:", "stencilCompareFunction");
            Check(Eq(ovr, "0"), "m_DefaultStencilState.overrideStencilState == 0（原版 :43；"
                  + "=0 是「模板测试关」⇒ 下面两格今天是死值，引擎判据 UniversalRenderer.cs:286 + DrawObjectsPass.cs:101）");
            Check(Eq(refv, "0"), "m_DefaultStencilState.stencilReference == 0（原版 :44）");
            Check(Eq(cmp, "8"), "m_DefaultStencilState.stencilCompareFunction == 8（原版 :45；8 = Always）");

            // 前提：三条 layerMask 可读（**不钉值** —— 原版 1207963447 vs 我们 4294967295 是 D-1 层方案缺口）
            string pre = BlockField(rd, "m_PrepassLayerMask:", "m_Bits");
            string opq = BlockField(rd, "m_OpaqueLayerMask:", "m_Bits");
            string trn = BlockField(rd, "m_TransparentLayerMask:", "m_Bits");
            Check(pre != null && opq != null && trn != null,
                  "三条 layerMask 都读得到（前提 pre=" + (pre ?? "<null>") + " opaque=" + (opq ?? "<null>")
                  + " transparent=" + (trn ?? "<null>") + "；⚠️ 值本批只调查不改，见 B120_W14 报告 §三）");
        }

        // ==================================================================
        //  ③ 灭自证：改前的旧值一个字都不许回来
        // ==================================================================
        if (rp != null)
        {
            Check(!rp.Contains("m_SupportsTerrainHoles: 1"), "旧值 m_SupportsTerrainHoles: 1 不在了");
            Check(!rp.Contains("m_UseFastSRGBLinearConversion: 0"), "旧值 m_UseFastSRGBLinearConversion: 0 不在了");
            Check(!rp.Contains("m_SupportScreenSpaceLensFlare: 1"), "旧值 m_SupportScreenSpaceLensFlare: 1 不在了");
            Check(!rp.Contains("m_VolumeFrameworkUpdateMode: 0"), "旧值 m_VolumeFrameworkUpdateMode: 0 不在了");
            Check(!rp.Contains("m_Cascade2Split: 0.25"), "旧值 m_Cascade2Split: 0.25 不在了");
            Check(!rp.Contains("m_Cascade4Split: {x: 0.12299999"), "旧值 m_Cascade4Split: {x: 0.12299999…} 不在了");
            Check(!rp.Contains("m_AdditionalLightsShadowResolutionTierLow: 256"), "旧值 tier Low: 256 不在了");
            Check(!rp.Contains("m_AdditionalLightsShadowResolutionTierMedium: 512"), "旧值 tier Medium: 512 不在了");
            Check(!rp.Contains("m_AdditionalLightsShadowResolutionTierHigh: 1024"), "旧值 tier High: 1024 不在了");
        }
        if (rd != null)
        {
            Check(!rd.Contains("m_ShadowTransparentReceive: 1"), "旧值 m_ShadowTransparentReceive: 1 不在了");
            Check(!rd.Contains("stencilReference: 1"), "旧值 stencilReference: 1 不在了");
            Check(!rd.Contains("stencilCompareFunction: 3"), "旧值 stencilCompareFunction: 3 不在了");
        }
    }
}
