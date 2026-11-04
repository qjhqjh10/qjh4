// B144_R2_Light_Assert.cs — 批次 **B144** · 执行代理 `R2`（B136 光照/后处理审计：离线可改项）
//
// ============================ 它盯的是什么 ============================
//   A：**Mobile 档的后处理数据必须为空** —— 原版 `UniversalRenderPipelineAsset_Renderer  LOW.asset:30`
//      `postProcessData: {fileID: 0}`（Medium/Low 共用它）；URP 源码 `UniversalRenderer.cs:456` 注释逐字：
//      「No postProcessData means that post processes are disabled」⇒ 原版 Mobile/Low **完全没有后处理**
//      （Bloom / Vignette / ColorLookup 都不跑）。我们原来 `Mobile_Renderer.asset:31` 指 `41439944…`（URP 默认数据）
//      ⇒ Mobile 档会跑战斗场景 Volume 的 Bloom 5 / Vignette 0.297 / LUT。**本批置 0。**
//   B：**Mobile RPAsset 的全局 VolumeProfile 必须为空** —— 原版 Medium:95 `m_VolumeProfile {fileID: 0}`；
//      我们的 `Mobile_RPAsset.asset:94` 也必须是 0（2026-11-04 已修，本批**核销**）。
//   C：**DefaultVolumeProfile 的 ProbeVolumesOptions** —— 原版 `ProbeVolumesOptions.asset`：
//      `normalBias 0.05 / viewBias 0.1 / leakReductionMode 2`；我们原来是 `0.33 / 0 / 1`（APV 全关 ⇒ 无观测差）。
//   D：战场场景 13 场的烘焙/RenderSettings 常量（A3/A5/A22/C1/C2/C3/B9）—— 2026-11-05 现读已全部等于原版，
//      本断言把它们钉住（防止 ArenaBuilder 重生成时又回默认值）。
//   ⛔ 不在本断言：A30/D1–D3（**场景文件不在本批白名单**，且无观测差）· A9/A31（缺 LightingSettings 资产）·
//      PC_Renderer 的 `postProcessData`（原版 High = 自定义 `7decaff8…`，我们 = URP 默认 `41439944…`；
//      属**资源替换线** `B142-E27`，本批不动——直接改 guid 会变成 missing reference）。
//
// ============================ 判据（现读） ============================
//   · 原版渲染器：`D:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/UniversalRenderPipelineAsset_Renderer  LOW.asset:30`
//     = `postProcessData: {fileID: 0}`；`UniversalRenderPipelineAsset_Renderer.asset:31`（High/Ultra）=
//     `guid: 7decaff82c14ec546ad1a9284fbda141`。
//   · 原版 RPAsset：`UniversalRenderPipelineAsset Medium.asset:94` `m_VolumeFrameworkUpdateMode: 1` · `:95` `m_VolumeProfile: {fileID: 0}`。
//   · 原版 ProbeVolumesOptions：`ProbeVolumesOptions.asset:16-33`（0.05 / 0.1 / 2）。
//   · 原版 13 场场景（逐场同值）：`m_BakeBackend 0` · `m_PVRSampleCount 500` · `m_PVRFilteringGaussRadiusIndirect 5` ·
//     `m_PVRFilteringGaussRadiusAO 2` · `m_SubtractiveShadowColor (0.6367924,0.7275943,1,1)` ·
//     `m_AmbientEquatorColor (0.3254717,0.5862051,1,1)` · `m_AmbientGroundColor (0.5896226,0.5896226,1,1)` ·
//     `m_StitchLightmapSeams 0`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · A1：把 `Mobile_Renderer.asset` 的 `postProcessData` 改回 `41439944…` ⇒ 红。
//   · B1/B2：把 `Mobile_RPAsset` 的 `m_VolumeProfile` 改回 `10fc4df2…`（SampleSceneProfile）⇒ 红。
//   · C1–C3：把 `DefaultVolumeProfile.asset` 的 ProbeVolumesOptions 三个值改回 0.33 / 0 / 1 ⇒ 红。
//   · D1–D7：把 `battlearena1.unity` 的任一常量改回 Unity 默认 ⇒ 红。
//
// ============================ 怎么接（宿主，调度台统一做）============================
//   `Assets/RuleEngine/Editor/RuleEngineTest.cs` 的聚合行**同一物理行、第一个 `//` 之前**追加：
//     `B144_R2_Light_Assert.Run(); _pass += B144_R2_Light_Assert.Pass; _fail += B144_R2_Light_Assert.Fail;`
//   （本类在**全局命名空间**、类名 == 文件名 ⇒ 可直接 `-executeMethod B144_R2_Light_Assert.Run`。）
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>B144-R2：B136 光照/后处理审计的离线可改项（Mobile 后处理数据 / 全局 profile / APV / 战场常量）。</summary>
public static class B144_R2_Light_Assert
{
    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
    public static int RunCount { get { return _pass + _fail; } }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log("--- 🆕 B144-R2：B136 光照/后处理（Mobile 后处理数据 / profile / APV / 战场常量）---");
        GroupA_MobileRendererPostProcessData();
        GroupB_MobileRpAssetVolumeProfile();
        GroupC_ProbeVolumesOptions();
        GroupD_BattleSceneConstants();
        Debug.Log("--- B144-R2 光照合计：通过 " + _pass + " / 失败 " + _fail + " ---");
    }

    // ============================================================ 夹具：直接读资产 YAML（只读，不依赖 URP 包 API 版本）
    static string AssetText(string rel)
    {
        string p = Path.Combine(Application.dataPath, rel);
        if (!File.Exists(p)) { Debug.LogError("[B144R2] 资产不存在：" + p); return null; }
        return File.ReadAllText(p);
    }

    static bool LineIs(string text, string key, string value)
    {
        return text != null && Regex.IsMatch(text, @"^\s*" + Regex.Escape(key) + @":\s*" + Regex.Escape(value) + @"\s*$", RegexOptions.Multiline);
    }

    static bool Has(string text, string literal) { return text != null && text.Contains(literal); }

    // ============================================================ A：Mobile 渲染器的 postProcessData
    static void GroupA_MobileRendererPostProcessData()
    {
        Section("A Mobile_Renderer.postProcessData = {fileID: 0}（原版 Low/Medium = 0 ⇒ 无后处理）");
        var t = AssetText("Settings/Mobile_Renderer.asset");
        CheckTrue(t != null, "A0 前提：`Settings/Mobile_Renderer.asset` 读得到");
        CheckTrue(LineIs(t, "postProcessData", "{fileID: 0}"),
                  "A1 ★★ Mobile 渲染器 `postProcessData` = `{fileID: 0}`（URP：null = 关掉后处理 ⇒ Bloom/Vignette/LUT 全不跑）"
                + "｜🧨 改回 `41439944…` ⇒ 红（判据：原版 `UniversalRenderPipelineAsset_Renderer  LOW.asset:30`）");
    }

    // ============================================================ B：Mobile RPAsset 的全局 profile / 更新模式
    static void GroupB_MobileRpAssetVolumeProfile()
    {
        Section("B Mobile_RPAsset：m_VolumeProfile = 0 · m_VolumeFrameworkUpdateMode = 1");
        var t = AssetText("Settings/Mobile_RPAsset.asset");
        CheckTrue(t != null, "B0 前提：`Settings/Mobile_RPAsset.asset` 读得到");
        CheckTrue(LineIs(t, "m_VolumeProfile", "{fileID: 0}"),
                  "B1 ★ `m_VolumeProfile` = `{fileID: 0}`（原版 Medium:95）｜🧨 改回 SampleSceneProfile ⇒ 红");
        CheckTrue(LineIs(t, "m_VolumeFrameworkUpdateMode", "1"),
                  "B2 ★ `m_VolumeFrameworkUpdateMode` = 1（原版 Medium:94；原为 0 EveryFrame）");
    }

    // ============================================================ C：DefaultVolumeProfile 的 APV 三值
    static void GroupC_ProbeVolumesOptions()
    {
        Section("C DefaultVolumeProfile：ProbeVolumesOptions normalBias/viewBias/leakReductionMode = 0.05/0.1/2");
        var t = AssetText("Settings/DefaultVolumeProfile.asset");
        CheckTrue(t != null, "C0 前提：`Settings/DefaultVolumeProfile.asset` 读得到");
        string block = null;
        if (t != null)
        {
            int at = t.IndexOf("m_Name: ProbeVolumesOptions", StringComparison.Ordinal);
            if (at >= 0)
            {
                int next = t.IndexOf("--- !u!", at, StringComparison.Ordinal);
                block = next > at ? t.Substring(at, next - at) : t.Substring(at);
            }
        }
        CheckTrue(block != null, "C0b 前提：找到 `ProbeVolumesOptions` 组件块");
        CheckFloat(BlockValue(block, "normalBias"), 0.05f, "C1 ★ normalBias = 0.05（原版 0.05；原 0.33）");
        CheckFloat(BlockValue(block, "viewBias"), 0.1f, "C2 ★ viewBias = 0.1（原版 0.1；原 0）");
        CheckFloat(BlockValue(block, "leakReductionMode"), 2f, "C3 ★ leakReductionMode = 2（原版 2；原 1）");
    }

    static float BlockValue(string block, string key)
    {
        if (block == null) return float.NaN;
        var m = Regex.Match(block, @"^\s*" + Regex.Escape(key) + @":\s*\r?\n\s*m_OverrideState:\s*1\s*\r?\n\s*m_Value:\s*([-0-9.]+)", RegexOptions.Multiline);
        if (!m.Success) return float.NaN;
        float v; return float.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float,
                                       System.Globalization.CultureInfo.InvariantCulture, out v) ? v : float.NaN;
    }

    // ============================================================ D：战场场景常量（13 场逐场同值）
    static void GroupD_BattleSceneConstants()
    {
        Section("D battlearena*.unity：A3/A5/A22/C1/C2/C3/B9 逐场 = 原版");
        string[] arenas = { "battlearena1", "battlearena2", "battlearena3", "battlearenaaeldari",
                            "battlearenaastramilitarum", "battlearenablacklegion", "battlearenadarkangels",
                            "battlearenaemperorschildren", "battlearenagenestealers", "battlearenaleviathan",
                            "battlearenasororitas", "battlearenaspacewolves", "battlearenatauviorla" };
        int bad = 0;
        for (int i = 0; i < arenas.Length; i++)
        {
            var t = AssetText("WarpforgeArena1/Scenes/" + arenas[i] + ".unity");
            bool ok = t != null
                   && LineIs(t, "m_BakeBackend", "0")
                   && LineIs(t, "m_PVRSampleCount", "500")
                   && LineIs(t, "m_PVRFilteringGaussRadiusIndirect", "5")
                   && LineIs(t, "m_PVRFilteringGaussRadiusAO", "2")
                   && LineIs(t, "m_SubtractiveShadowColor", "{r: 0.6367924, g: 0.7275943, b: 1, a: 1}")
                   && LineIs(t, "m_AmbientEquatorColor", "{r: 0.3254717, g: 0.5862051, b: 1, a: 1}")
                   && LineIs(t, "m_AmbientGroundColor", "{r: 0.5896226, g: 0.5896226, b: 1, a: 1}")
                   && !Regex.IsMatch(t, @"^\s*m_StitchLightmapSeams:\s*1\s*$", RegexOptions.Multiline);
            if (!ok) bad++;
        }
        Check(bad, 0, "D1 ★ 13 场全部命中原版常量（BakeBackend 0 / PVRSampleCount 500 / Gauss 5,2 / " +
                     "Subtractive+Equator+Ground 原值 / StitchLightmapSeams 无 1）；不合格场数");
    }

    // ============================================================ 断言小工具
    static void Section(string title) { Debug.Log("--- " + title + " ---"); }
    static void Check<T>(T actual, T expected, string msg)
    {
        bool ok = EqualityComparer<T>.Default.Equals(actual, expected);
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }
    static void CheckTrue(bool cond, string msg) { Report(cond, msg); }
    static void CheckFloat(float actual, float expected, string msg)
    {
        bool ok = !float.IsNaN(actual) && Math.Abs(actual - expected) <= 1e-4f * Math.Max(1f, Math.Abs(expected));
        Report(ok, msg + "（实际 = " + actual + "，期望 = " + expected + "）");
    }
    static void Report(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("[B144R2L] ✓ " + msg); }
        else { _fail++; Debug.LogError("[B144R2L] ✗ " + msg); }
    }
}
