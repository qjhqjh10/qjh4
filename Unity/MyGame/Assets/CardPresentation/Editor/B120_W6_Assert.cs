// B120_W6_Assert.cs — 渲染资产 / URP 设置 / GrabPass 的断言（执行代理 `B120-W6`，2026-11-02）
//
// ============================ 它盯的是什么 ============================
// ① **哪一份 RPAsset 真正生效**（`D118-83/84` 的动手前提）
//    `ProjectSettings/GraphicsSettings.asset` 的 `m_CustomRenderPipeline` 与
//    `QualitySettings.asset`「PC」档的 `customRenderPipeline` 都必须指 `Settings/PC_RPAsset.asset`；
//    `PC_RPAsset` 的 `m_RendererDataList` 必须指 `Settings/PC_Renderer.asset`。
//    ⇒ 挡「改了一份没生效的资产」。
// ② **RPAsset 的 20 格**逐格对原版 `UniversalRenderPipelineAsset High.asset`
//    （阴影三件套 / 附加光 / 反射探针 / bias / border / 软阴影档 / 混合光照 / cookie / light layer /
//      OpaqueDownsampling / LODCrossFade / 全局 VolumeProfile）。
// ③ **Renderer**：`m_RenderingMode == 0`(Forward) · SSAO feature `m_Active == 0` · GrabPass feature 在位且 active ·
//    `redrawLayerMask.m_Bits == 64`（原版 `GrabScreenFeatureU6.asset` 的 `LayerMask`）。
// ④ **GrabPass 代码常量**：`GrabScreen501.OriginalRtScale == 0.25f`，且 `ParseRtScale` 的兜底**不再**是 `1f`。
// ⑤ **灭自证**：改前的旧值（50 / 4 / 2 / 2048 / 1 …）**不许回来**。
//
// ============================ 期望值从哪来 ============================
// 全部是**原版判据里的数**（⛔ 不是「我们改完以后的值」）：
//   `d:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/UniversalRenderPipelineAsset High.asset`
//     :24 RequireOpaque 0 · :25 OpaqueDownsampling 0 · :34 LODCrossFade 0 · :49 perObject 8 · :50 附加光阴影 0
//     :51 附加光阴影贴图 512 · :55-57 反射探针 0/0/0 · :58 ShadowDistance 15 · :59 Cascade 1
//     :63 CascadeBorder 0.31556404 · :64 DepthBias 0.27 · :65 NormalBias 0.04 · :67 保守包围球 0
//     :69 SoftShadowQuality 2 · :74-76 MixedLighting/Cookies/LightLayers 0 · :95 VolumeProfile = {fileID: 0}
//   `…/UniversalRenderPipelineAsset_Renderer.asset`
//     :50 m_RenderingMode 0(=Forward) · :26-28 只有 1 个 feature（GrabScreenFeatureU6）
//   `…/GrabScreenFeatureU6.asset`
//     :20 LayerMask.m_Bits 64（层 6 AfterTransparents）
//   `GrabScreenFeature.Settings__.ctor.c` / `GrabScreenFeatureRenderGraphAPI.Settings__.ctor.c`
//     `textureSizeMultiplier` 默认 `0x3e800000` = 0.25f
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  接进 `CardPresentation/Editor/BattleScene.cs:46983` 那条**方法尾聚合行**：
//  在 `Debug.Log(P + $"=== 合计：…")` **之前**、同一物理行、该行第一个 `//` 之前追加：
//      B120W6_Assert.Run(); pass += B120W6_Assert.Pass; fail += B120W6_Assert.Fail;
//  ⚠️ 本件**只读**（读 4 份资产文本 + 1 份源码文本 + 2 个常量），不建对象、不改资产、不驱动生产对象
//     ⇒ 可以接在该聚合行任何位置；放「合计」之前是为了让它印在合计里。
using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using WarpforgeVFX;

/// <summary>`B120` D 域（渲染资产 / URP 设置 / GrabPass）的断言集（只读文本 + 常量，**不驱动生产对象**）。</summary>
public static class B120W6_Assert
{
    public static int Pass;
    public static int Fail;

    static void Check(bool ok, string what)
    {
        if (ok) { Pass++; Debug.Log("[B120W6] ✓ " + what); }
        else { Fail++; Debug.LogError("[B120W6] ✗ " + what); }
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

    /// <summary>读 `MyGame/ProjectSettings/` 下的文本文件（`Application.dataPath` 的上一级）。</summary>
    static string ReadProject(string rel, out string err)
    {
        err = null;
        try
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ProjectSettings"));
            string p = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
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

    /// <summary>以某一行（如 `redrawLayerMask:` / `m_Name: ScreenSpaceAmbientOcclusion`）为界，
    /// 只在这一块里找 `key:` —— 同名键（`m_Active` / `m_Bits`）在文件里出现多次，⛔ 不能全局取第一条。</summary>
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

    static bool Near(string got, float want)
    {
        if (string.IsNullOrEmpty(got)) return false;
        float v;
        if (!float.TryParse(got, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return false;
        return Mathf.Abs(v - want) < 1e-6f;
    }

    /// <summary>在 `text` 里从 `fromLine` 之后找 `needle`（用于 QualitySettings 各档块内取值）。</summary>
    static string FindAfter(string text, string fromLine, string needle, int maxLines = 80)
    {
        if (text == null) return null;
        string[] lines = text.Split('\n');
        int start = -1;
        for (int i = 0; i < lines.Length; i++)
            if (lines[i].IndexOf(fromLine, StringComparison.Ordinal) >= 0) { start = i; break; }
        if (start < 0) return null;
        int stop = Mathf.Min(lines.Length, start + maxLines);
        for (int i = start; i < stop; i++)
            if (lines[i].IndexOf(needle, StringComparison.Ordinal) >= 0) return lines[i];
        return null;
    }

    public static void Run()
    {
        string err;

        // ==================================================================
        //  ① 哪一份 RPAsset / Renderer 真正生效（证据链，也是后面所有断言的前提）
        // ==================================================================
        string gfx = ReadProject("GraphicsSettings.asset", out err);
        Check(gfx != null, "读得到 ProjectSettings/GraphicsSettings.asset（前提）" + (err != null ? " · " + err : ""));
        string quality = ReadProject("QualitySettings.asset", out err);
        Check(quality != null, "读得到 ProjectSettings/QualitySettings.asset（前提）" + (err != null ? " · " + err : ""));
        string rpMeta = ReadAsset("Settings/PC_RPAsset.asset.meta", out err);
        Check(rpMeta != null, "读得到 Settings/PC_RPAsset.asset.meta（前提）" + (err != null ? " · " + err : ""));
        string rdMeta = ReadAsset("Settings/PC_Renderer.asset.meta", out err);
        Check(rdMeta != null, "读得到 Settings/PC_Renderer.asset.meta（前提）" + (err != null ? " · " + err : ""));

        string rpGuid = Field(rpMeta, "guid");
        string rdGuid = Field(rdMeta, "guid");
        Check(!string.IsNullOrEmpty(rpGuid), "PC_RPAsset 的 guid 读得到（前提）");
        Check(!string.IsNullOrEmpty(rdGuid), "PC_Renderer 的 guid 读得到（前提）");

        if (gfx != null && !string.IsNullOrEmpty(rpGuid))
        {
            string g = FindAfter(gfx, "m_CustomRenderPipeline:", "guid:");
            Check(g != null && g.IndexOf(rpGuid, StringComparison.Ordinal) >= 0,
                  "GraphicsSettings.m_CustomRenderPipeline = PC_RPAsset（" + rpGuid + "）");
        }
        if (quality != null && !string.IsNullOrEmpty(rpGuid))
        {
            string q = FindAfter(quality, "m_CurrentQuality:", "m_CurrentQuality:");
            Check(q != null && Eq(Field("x\n" + q, "m_CurrentQuality"), "1"),
                  "QualitySettings.m_CurrentQuality == 1（PC 档）");
            // 「PC」档块：`name: PC` 之后第一条 customRenderPipeline
            int pcIdx = quality.IndexOf("name: PC", StringComparison.Ordinal);
            Check(pcIdx >= 0, "QualitySettings 有 `name: PC` 档（前提）");
            if (pcIdx >= 0)
            {
                string tail = quality.Substring(pcIdx);
                string c = FindAfter(tail, "customRenderPipeline:", "guid:");
                Check(c != null && c.IndexOf(rpGuid, StringComparison.Ordinal) >= 0,
                      "QualitySettings「PC」档 customRenderPipeline = PC_RPAsset（" + rpGuid + "）");
            }
        }

        string rp = ReadAsset("Settings/PC_RPAsset.asset", out err);
        Check(rp != null, "读得到 Settings/PC_RPAsset.asset（前提）" + (err != null ? " · " + err : ""));
        string rd = ReadAsset("Settings/PC_Renderer.asset", out err);
        Check(rd != null, "读得到 Settings/PC_Renderer.asset（前提）" + (err != null ? " · " + err : ""));

        if (rp != null && !string.IsNullOrEmpty(rdGuid))
        {
            string list = Field(rp, "m_RendererDataList");
            // `m_RendererDataList:` 是块（下一行才是 `- {...}`）⇒ 取整个文件里该 guid 是否出现在 renderer 列表段
            int li = rp.IndexOf("m_RendererDataList:", StringComparison.Ordinal);
            bool okList = false;
            if (li >= 0)
            {
                int stop = rp.IndexOf("m_DefaultRendererIndex:", li, StringComparison.Ordinal);
                string seg = stop > li ? rp.Substring(li, stop - li) : rp.Substring(li, Mathf.Min(400, rp.Length - li));
                okList = seg.IndexOf(rdGuid, StringComparison.Ordinal) >= 0;
            }
            Check(okList, "PC_RPAsset.m_RendererDataList 含 PC_Renderer（" + rdGuid + "）" + (list == null ? "" : ""));
        }

        // ==================================================================
        //  ② RPAsset 逐格对原版 High（期望值 = 原版判据，见文件头）
        // ==================================================================
        if (rp != null)
        {
            Check(Eq(Field(rp, "m_RequireOpaqueTexture"), "0"), "RequireOpaqueTexture == 0（原版 High:24）");
            Check(Eq(Field(rp, "m_OpaqueDownsampling"), "0"), "OpaqueDownsampling == 0（原版 High:25）");
            Check(Eq(Field(rp, "m_EnableLODCrossFade"), "0"), "EnableLODCrossFade == 0（原版 High:34）");
            Check(Eq(Field(rp, "m_AdditionalLightsPerObjectLimit"), "8"), "AdditionalLightsPerObjectLimit == 8（原版 High:49）");
            Check(Eq(Field(rp, "m_AdditionalLightShadowsSupported"), "0"), "AdditionalLightShadowsSupported == 0（原版 High:50）");
            Check(Eq(Field(rp, "m_AdditionalLightsShadowmapResolution"), "512"), "AdditionalLightsShadowmapResolution == 512（原版 High:51）");
            Check(Eq(Field(rp, "m_ReflectionProbeBlending"), "0"), "ReflectionProbeBlending == 0（原版 High:55）");
            Check(Eq(Field(rp, "m_ReflectionProbeBoxProjection"), "0"), "ReflectionProbeBoxProjection == 0（原版 High:56）");
            Check(Eq(Field(rp, "m_ReflectionProbeAtlas"), "0"), "ReflectionProbeAtlas == 0（原版 High:57）");
            Check(Eq(Field(rp, "m_ShadowDistance"), "15"), "ShadowDistance == 15（原版 High:58）");
            Check(Eq(Field(rp, "m_ShadowCascadeCount"), "1"), "ShadowCascadeCount == 1（原版 High:59）");
            Check(Near(Field(rp, "m_CascadeBorder"), 0.31556404f), "CascadeBorder == 0.31556404（原版 High:63）");
            Check(Near(Field(rp, "m_ShadowDepthBias"), 0.27f), "ShadowDepthBias == 0.27（原版 High:64）");
            Check(Near(Field(rp, "m_ShadowNormalBias"), 0.04f), "ShadowNormalBias == 0.04（原版 High:65）");
            Check(Eq(Field(rp, "m_ConservativeEnclosingSphere"), "0"), "ConservativeEnclosingSphere == 0（原版 High:67）");
            Check(Eq(Field(rp, "m_SoftShadowQuality"), "2"), "SoftShadowQuality == 2（原版 High:69）");
            Check(Eq(Field(rp, "m_MixedLightingSupported"), "0"), "MixedLightingSupported == 0（原版 High:74）");
            Check(Eq(Field(rp, "m_SupportsLightCookies"), "0"), "SupportsLightCookies == 0（原版 High:75）");
            Check(Eq(Field(rp, "m_SupportsLightLayers"), "0"), "SupportsLightLayers == 0（原版 High:76）");
            Check(Eq(Field(rp, "m_VolumeProfile"), "{fileID: 0}"), "VolumeProfile == {fileID: 0}（原版 High:95，没有全局默认 profile）");
        }

        // ==================================================================
        //  ③ Renderer：渲染路径 / SSAO / GrabPass / 重画层
        // ==================================================================
        if (rd != null)
        {
            Check(Eq(Field(rd, "m_RenderingMode"), "0"),
                  "m_RenderingMode == 0（Forward；原版 UniversalRenderPipelineAsset_Renderer.asset:50）");

            string ssaoActive = BlockField(rd, "m_Name: ScreenSpaceAmbientOcclusion", "m_Active");
            Check(ssaoActive != null, "PC_Renderer 里有 ScreenSpaceAmbientOcclusion 块（前提）");
            Check(Eq(ssaoActive, "0"),
                  "SSAO feature m_Active == 0（原版主 renderer 只有 GrabScreenFeatureU6 一个 feature；URP 跳过 inactive：ScriptableRenderer.cs:1641/1661/1704）");

            string grabActive = BlockField(rd, "m_Name: GrabPassTransparent", "m_Active");
            Check(Eq(grabActive, "1"), "GrabPassTransparent feature m_Active == 1（前提：抓屏要活着）");

            string redraw = BlockField(rd, "redrawLayerMask:", "m_Bits");
            Check(Eq(redraw, "64"),
                  "redrawLayerMask.m_Bits == 64（原版 GrabScreenFeatureU6.asset:20 的 settings.LayerMask；层 6）");
        }

        // ==================================================================
        //  ④ GrabPass 代码常量 + 兜底路径
        // ==================================================================
        Check(Mathf.Abs(GrabScreen501.OriginalRtScale - 0.25f) < 1e-6f,
              "GrabScreen501.OriginalRtScale == 0.25f（原版 textureSizeMultiplier 0x3e800000）");
        {
            // 环境变量没改时，实际生效值就必须是原版 0.25
            string envRt = Environment.GetEnvironmentVariable("WF_GRAB501_RT_SCALE");
            Check(string.IsNullOrEmpty(envRt) || envRt == "0.25",
                  "前提：未把 WF_GRAB501_RT_SCALE 设成别的值（现读 `" + (envRt ?? "<未设>") + "`）");
            Check(Mathf.Abs(GrabScreen501.RtScale - 0.25f) < 1e-6f,
                  "GrabScreen501.RtScale == 0.25f（默认档就是原版值，不再退回 1.0）");
        }
        {
            string src = ReadAsset("WarpforgeVFX/Runtime/GrabPassTransparentFeature.cs", out err);
            Check(src != null, "读得到 GrabPassTransparentFeature.cs（前提）" + (err != null ? " · " + err : ""));
            if (src != null)
            {
                Check(src.Contains("return OriginalRtScale;"), "ParseRtScale 的兜底是 `return OriginalRtScale;`");
                Check(!src.Contains("return Enabled ? OriginalRtScale : 1f;"),
                      "ParseRtScale 不再退回 `Enabled ? OriginalRtScale : 1f`（旧兜底 = 1.0 全分辨率）");
                Check(src.Contains("public const float OriginalRtScale = 0.25f;"),
                      "OriginalRtScale 常量仍在（0.25f）");
                Check(src.Contains("B120-W6"), "文件内留有 `B120-W6` 的判据/改动标记（挡把注释改回去）");
            }
        }

        // ==================================================================
        //  ⑤ 灭自证：改前的旧值一个字都不许回来
        // ==================================================================
        if (rp != null)
        {
            Check(!rp.Contains("m_RequireOpaqueTexture: 1"), "旧值 RequireOpaqueTexture: 1 不在了");
            Check(!rp.Contains("m_OpaqueDownsampling: 1"), "旧值 OpaqueDownsampling: 1 不在了");
            Check(!rp.Contains("m_EnableLODCrossFade: 1"), "旧值 EnableLODCrossFade: 1 不在了");
            Check(!rp.Contains("m_AdditionalLightsPerObjectLimit: 4"), "旧值 AdditionalLightsPerObjectLimit: 4 不在了");
            Check(!rp.Contains("m_AdditionalLightShadowsSupported: 1"), "旧值 AdditionalLightShadowsSupported: 1 不在了");
            Check(!rp.Contains("m_AdditionalLightsShadowmapResolution: 2048"), "旧值 AdditionalLightsShadowmapResolution: 2048 不在了");
            Check(!rp.Contains("m_ReflectionProbeBlending: 1"), "旧值 ReflectionProbeBlending: 1 不在了");
            Check(!rp.Contains("m_ReflectionProbeBoxProjection: 1"), "旧值 ReflectionProbeBoxProjection: 1 不在了");
            Check(!rp.Contains("m_ReflectionProbeAtlas: 1"), "旧值 ReflectionProbeAtlas: 1 不在了");
            Check(!rp.Contains("m_ShadowDistance: 50"), "旧值 ShadowDistance: 50 不在了");
            Check(!rp.Contains("m_ShadowCascadeCount: 4"), "旧值 ShadowCascadeCount: 4 不在了");
            Check(!rp.Contains("m_CascadeBorder: 0.107758604"), "旧值 CascadeBorder: 0.107758604 不在了");
            Check(!rp.Contains("m_ShadowDepthBias: 0.1"), "旧值 ShadowDepthBias: 0.1 不在了");
            Check(!rp.Contains("m_ShadowNormalBias: 0.5"), "旧值 ShadowNormalBias: 0.5 不在了");
            Check(!rp.Contains("m_ConservativeEnclosingSphere: 1"), "旧值 ConservativeEnclosingSphere: 1 不在了");
            Check(!rp.Contains("m_SoftShadowQuality: 3"), "旧值 SoftShadowQuality: 3 不在了");
            Check(!rp.Contains("m_MixedLightingSupported: 1"), "旧值 MixedLightingSupported: 1 不在了");
            Check(!rp.Contains("m_SupportsLightCookies: 1"), "旧值 SupportsLightCookies: 1 不在了");
            Check(!rp.Contains("m_SupportsLightLayers: 1"), "旧值 SupportsLightLayers: 1 不在了");
            Check(!rp.Contains("guid: 10fc4df2da32a41aaa32d77bc913491c"),
                  "PC_RPAsset 不再引用 SampleSceneProfile（原版 High:95 没有全局默认 profile）");
        }
        if (rd != null)
        {
            Check(!rd.Contains("m_RenderingMode: 2"), "旧值 m_RenderingMode: 2（Forward+）不在了");
            string redraw = BlockField(rd, "redrawLayerMask:", "m_Bits");
            Check(!Eq(redraw, "0"), "旧值 redrawLayerMask.m_Bits: 0 不在了");
        }
    }
}
