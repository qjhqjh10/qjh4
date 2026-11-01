// B109_E33_MenuMat_Assert.cs — 执行代理 `B109_E33`（2026-10-31 · 第一百零九会话）
//   三条原版菜单材质的第一批：**#4 Pulsating · #5 Glow SDF Noise · #9 UI Clock**
//   （施工单 = `资料/完全复刻/判据/B109_R15_菜单13材质.md` §1·4 / §1·5 / §1·9）
//
// ============================ 本文件断什么（四层，逐条独立于实现）============================
//   ① **资产在、shader 名对**：8 份 `.mat` 都取得到，且 `mat.shader.name` 是那三个工程内 shader。
//   ② **参数逐字段**：期望值**手抄自原版材质 JSON**（不是从 C# 工厂/`.mat` 自己读回来对账），
//      见各 `Check*` 里每条断言的 `// 判据：assets_full/…/Material_*.json`。
//   ③ **`_Time` 驱动真的在**：读 `.shader` 源文本，断它用的是 **`_Time.x`（Pulsating）/ `_Time.y`（另外两条）**
//      —— 这两者语义不同（`_Time = (t/20, t, 2t, 3t)`），⛔ 换错就是 20× 的速度。
//   ④ **接线到了**：逐个调用点文件断「确实调了 `MenuFxMaterials.Apply*/` + 对应材质常量」
//      （本项目栽过「断言文件建了但没人接线」「材质建了但没人挂」两种，故这条必须有）。
//
// ============================ C# 镜像（把 shader 的闭式算式抄成 C#，验的是【抄对了】）============================
//   · `Pulse01(t)`：原版 `(sin(A.y · _Time.x)+1)/2`，`_Time.x = t/20` ⇒ 在 `t = π/5`（= 0.6283185）处 = 1。
//   · `HandAngle(t)`：原版 `t · _Rotation_Speed_Seconds`，`= -1/(2π)` ⇒ 在 `t = 2π/(1/(2π)) = 4π²/…` 处 ≈ −2π。
//   · `SdfCenter(t)`：把 fragment 的 SDF 环算式抄成 C#，断「sprite α=1（环）比 α=0（中心）亮」。
//   ⚠️ 这三条**不是**渲染断言（无 Unity 批处理可跑）；真·两帧像素差交真机验收（R15 §3·4）。
//
// ============================ 怎么接线（宿主）============================
//   本文件**没有**自己往 9 个断言宿主里接（它们是 `B109_E33` 的白名单外）。宿主里加两行即可：
//
//       B109_E33_MenuMat_Assert.Run();
//       pass += B109_E33_MenuMat_Assert.Pass; fail += B109_E33_MenuMat_Assert.Fail;
//
//   ⚠️ 它**不建窗口、不动场景、不跑播放**，只读资产 + 读源码文本 ⇒ 挂到哪个宿主都安全，
//      也不需要 `-executeMethod` 之外的任何夹具。
using System.Collections.Generic;
using System.IO;
using CardPresentation;
using UnityEditor;
using UnityEngine;

/// <summary>`B109_E33`：#4 Pulsating / #5 Glow SDF Noise / #9 UI Clock 三族菜单材质的资产+参数+驱动+接线断言。</summary>
public static class B109_E33_MenuMat_Assert
{
    public static int Pass;
    public static int Fail;

    const string MatDir   = "Assets/CardPresentation/Resources/Art/vfx/";
    const string ShaderDir = "Assets/CardPresentation/Resources/Shaders/";

    // 原版 shader 名 ↔ 我们的工程内 shader 名（R15 §0·0：算式全来自 Ruri 的逐指令 HLSL）
    const string ShPulsating = "WarpforgeVFX/UI/UI Pulsating";
    const string ShClock     = "WarpforgeVFX/UI/UI Clock";
    const string ShGlowSdf   = "WarpforgeVFX/UI/UI Glow SDF Noise";

    public static void Run()
    {
        Pass = 0; Fail = 0;
        Debug.Log("[B109_E33] ===== 菜单 3 族材质断言开始 =====");

        CheckPulsating();
        CheckGlowSdf();
        CheckClock();
        CheckWiring();

        Debug.Log("[B109_E33] ===== 合计 pass=" + Pass + " fail=" + Fail + " =====");
    }

    // ------------------------------------------------------------------ 工具

    static void Check(string what, bool ok)
    {
        if (ok) { Pass++; Debug.Log("[B109_E33] ✓ " + what); }
        else    { Fail++; Debug.LogError("[B109_E33] ✗ " + what); }
    }

    static void CheckNear(string what, float got, float want, float tol)
    {
        Check(what + "（实测 " + got.ToString("R") + " · 期望 " + want.ToString("R")
              + " ±" + tol.ToString("R") + "）", Mathf.Abs(got - want) <= tol);
    }

    static void CheckStr(string what, string got, string want)
    {
        Check(what + "（实测 `" + (got ?? "null") + "` · 期望 `" + want + "`）", got == want);
    }

    static Material Mat(string name)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + name + ".mat");
        Check("材质存在：`" + MatDir + name + ".mat`", m != null);
        return m;
    }

    static void CheckShaderOf(string matName, Material m, string wantShader)
    {
        if (m == null) return;   // `Mat()` 已经记过红了
        CheckStr("`" + matName + "` 的 shader", m.shader != null ? m.shader.name : null, wantShader);
    }

    static void CheckFloat(Material m, string matName, string prop, float want)
    {
        if (m == null) return;
        Check("`" + matName + "` 有属性 `" + prop + "`", m.HasProperty(prop));
        if (m.HasProperty(prop)) CheckNear("`" + matName + "`.`" + prop + "`", m.GetFloat(prop), want, 1e-4f);
    }

    static void CheckColor(Material m, string matName, string prop, float r, float g, float b, float a, float tol = 1e-4f)
    {
        if (m == null) return;
        Check("`" + matName + "` 有属性 `" + prop + "`", m.HasProperty(prop));
        if (!m.HasProperty(prop)) return;
        var c = m.GetColor(prop);
        CheckNear("`" + matName + "`.`" + prop + "`.r", c.r, r, tol);
        CheckNear("`" + matName + "`.`" + prop + "`.g", c.g, g, tol);
        CheckNear("`" + matName + "`.`" + prop + "`.b", c.b, b, tol);
        CheckNear("`" + matName + "`.`" + prop + "`.a", c.a, a, tol);
    }

    static string ShaderText(string file)
    {
        var p = ShaderDir + file;
        return File.Exists(p) ? StripComments(File.ReadAllText(p)) : null;
    }

    /// <summary>去掉 `//` 行注释 —— 本文件要断的是「**代码**里用的是哪一份 `_Time`」，
    /// 而三份 shader 的**文件头注释**里为了讲清口径，本来就会同时提到 `_Time.x` 与 `_Time.y`。
    /// ⛔ 不剥注释 ⇒ 两条「⛔ 没用 xx」会稳定假红（本项目栽过「按字符串断言被注释骗」）。</summary>
    static string StripComments(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
            }
            else sb.Append(s[i++]);
        }
        return sb.ToString();
    }

    // ------------------------------------------------------------------ #4 Pulsating

    // 原版 vertex：`aFactor = mad((1-A.x)*(sin(A.y*_Time.x)+1), 0.5, A.x)`（Vertex_b1.hlsl:91-101）
    static float Pulse01(float aX, float speed, float t) { return aX + (1f - aX) * (0.5f + 0.5f * Mathf.Sin(speed * (t / 20f))); }

    static void CheckPulsating()
    {
        Debug.Log("[B109_E33] ---- #4 Pulsating ----");

        // 判据：assets_full/bundle_menus_assets_all/Material/Material_-2164664953476156828.json（Forge UI Light）
        var forge = Mat("WFUI_Pulsating_Forge");
        CheckShaderOf("WFUI_Pulsating_Forge", forge, ShPulsating);
        CheckColor(forge, "WFUI_Pulsating_Forge", "_AlphaPulsating", 0.2f, 50f, 0f, 0f);
        CheckColor(forge, "WFUI_Pulsating_Forge", "_SizeRotation", 1f, 0f, 0f, 0f);

        // 判据：Material_750193365382565044.json（Everguild Campaing Premium node）
        var camp = Mat("WFUI_Pulsating_Campaign");
        CheckShaderOf("WFUI_Pulsating_Campaign", camp, ShPulsating);
        CheckColor(camp, "WFUI_Pulsating_Campaign", "_AlphaPulsating", 0.9f, 50f, 0f, 0f);
        CheckColor(camp, "WFUI_Pulsating_Campaign", "_SizeRotation", 1.05f, 50f, 0f, 413.7f);

        // 死值必须**不在**材质上（DXBC 常量表没有它们 ⇒ 带上就是「看着像原版」的假参数）
        Check("`WFUI_Pulsating_Forge` 没有死值 `_Size`", forge != null && !forge.HasProperty("_Size"));
        Check("`WFUI_Pulsating_Forge` 没有死值 `_SizeRotationAlpha`", forge != null && !forge.HasProperty("_SizeRotationAlpha"));
        Check("`WFUI_Pulsating_Forge` 没有死值 `_RemapAtlasUVs`", forge != null && !forge.HasProperty("_RemapAtlasUVs"));

        // `_Time` 驱动：原版读的是 UnityPerCamera 的 `_Time`（= (t/20, t, 2t, 3t)）⇒ 必须用 `_Time.x`
        var src = ShaderText("WFUIPulsating.shader");
        Check("`WFUIPulsating.shader` 文件在", src != null);
        if (src != null)
        {
            Check("`WFUIPulsating.shader` 用 `_Time.x`（= t/20，原版口径）", src.Contains("_Time.x"));
            Check("`WFUIPulsating.shader` ⛔ 没用 `_Time.y`（那会快 20 倍）", !src.Contains("_Time.y"));
            Check("`WFUIPulsating.shader` 带 `0.01745329` 度→弧度常数", src.Contains("0.01745329"));
        }

        // C# 镜像：t = π/5 ⇒ sin(50·t/20) = sin(π/2) = 1 ⇒ aFactor = 1（两端材质都一样）
        float t = Mathf.PI / 5f;                                  // 0.6283185
        CheckNear("`Pulse01` @ Forge · t=π/5 ⇒ aFactor", Pulse01(0.2f, 50f, t), 1f, 1e-4f);
        CheckNear("`Pulse01` @ Campaign · t=π/5 ⇒ aFactor", Pulse01(0.9f, 50f, t), 1f, 1e-4f);
        // 反相半周期：t = 3π/5 ⇒ sin = −1 ⇒ aFactor = A.x（档底）
        CheckNear("`Pulse01` @ Forge · t=3π/5 ⇒ aFactor = 0.2", Pulse01(0.2f, 50f, 3f * Mathf.PI / 5f), 0.2f, 1e-4f);
        // 灭自证：50 rad/s ÷ 20 = 2.5 rad/s ⇒ **一个来回 2π/2.5 = 2.513 s**（不是 R15 原写的 8 Hz）
        CheckNear("`Pulse01` 周期（秒）= 2π / (50/20)", 2f * Mathf.PI / 2.5f, 2.513274f, 1e-4f);
    }

    // ------------------------------------------------------------------ #5 Glow SDF Noise

    // 原版 fragment 的 SDF 环（Fragment_b15.hlsl:68-78）：sprite α 越靠环外沿越亮
    static float SdfRing(float spriteA, float spriteEdgeInner, float innerEdge, float innerFallof)
    {
        float a1 = spriteEdgeInner * (1f - innerEdge) + innerEdge;      // _129
        float d1 = spriteEdgeInner - a1;                                // _130
        float b1 = innerFallof * d1 + a1;                               // _133
        float inv2 = 1f / (a1 - b1);                                    // _135
        float ss = Mathf.Clamp01(inv2 * (spriteA - b1));                // _150
        return ss * ss * (3f - 2f * ss);                                // smoothstep（噪声记 0）
    }

    static void CheckGlowSdf()
    {
        Debug.Log("[B109_E33] ---- #5 UI Glow SDF Noise ----");

        // 判据：五份 Material_*.json 的 m_Floats / m_Colors 逐字段
        var bc  = Mat("WFUI_GlowSdf_BorderChest");
        CheckShaderOf("WFUI_GlowSdf_BorderChest", bc, ShGlowSdf);
        CheckFloat(bc, "WFUI_GlowSdf_BorderChest", "_Inner_Edge", 0.897f);
        CheckFloat(bc, "WFUI_GlowSdf_BorderChest", "_Inner_Fallof", 0.81f);
        CheckFloat(bc, "WFUI_GlowSdf_BorderChest", "_SpriteAlphaEdge_Inner", 0.225f);
        CheckFloat(bc, "WFUI_GlowSdf_BorderChest", "_NoiseIntensity", 0.74f);
        CheckFloat(bc, "WFUI_GlowSdf_BorderChest", "_NoiseMaskScale", 1.18f);
        CheckFloat(bc, "WFUI_GlowSdf_BorderChest", "_NOISE_CHANNEL", 2f);
        CheckFloat(bc, "WFUI_GlowSdf_BorderChest", "_SrcBlend", 5f);
        CheckFloat(bc, "WFUI_GlowSdf_BorderChest", "_DstBlend", 1f);
        CheckColor(bc, "WFUI_GlowSdf_BorderChest", "_Noise_Speed_Scale_StepLow_StepHigh", 0.05f, -1.06f, 0.02f, 0.22f);
        CheckColor(bc, "WFUI_GlowSdf_BorderChest", "_BackgroundColor", 2.6082377f, 1.1660355f, 1.8430555f, 1f, 1e-3f);

        var bcc = Mat("WFUI_GlowSdf_BorderChestCollect");
        CheckShaderOf("WFUI_GlowSdf_BorderChestCollect", bcc, ShGlowSdf);
        CheckColor(bcc, "WFUI_GlowSdf_BorderChestCollect", "_BackgroundColor", 1.8068027f, 1.6467977f, 0.2301117f, 1f, 1e-3f);
        CheckFloat(bcc, "WFUI_GlowSdf_BorderChestCollect", "_NOISE_CHANNEL", 2f);

        var red = Mat("WFUI_GlowSdf_HighlightRed");
        CheckShaderOf("WFUI_GlowSdf_HighlightRed", red, ShGlowSdf);
        CheckFloat(red, "WFUI_GlowSdf_HighlightRed", "_Inner_Edge", 1f);
        CheckFloat(red, "WFUI_GlowSdf_HighlightRed", "_Inner_Fallof", 0.389f);
        CheckFloat(red, "WFUI_GlowSdf_HighlightRed", "_SpriteAlphaEdge_Inner", 1f);
        CheckFloat(red, "WFUI_GlowSdf_HighlightRed", "_NoiseIntensity", 1.22f);
        CheckFloat(red, "WFUI_GlowSdf_HighlightRed", "_NoiseMaskScale", 1.00999999f);
        CheckFloat(red, "WFUI_GlowSdf_HighlightRed", "_NOISE_CHANNEL", 3f);
        CheckFloat(red, "WFUI_GlowSdf_HighlightRed", "_DstBlend", 10f);
        CheckColor(red, "WFUI_GlowSdf_HighlightRed", "_Noise_Speed_Scale_StepLow_StepHigh", 0.0318309888f, 0.8f, 0f, 0.37f);
        CheckColor(red, "WFUI_GlowSdf_HighlightRed", "_BackgroundColor", 0.9528302f, 0f, 0f, 1f, 1e-3f);

        var smooth = Mat("WFUI_GlowSdf_HighlightSmoothRed");
        CheckShaderOf("WFUI_GlowSdf_HighlightSmoothRed", smooth, ShGlowSdf);
        CheckFloat(smooth, "WFUI_GlowSdf_HighlightSmoothRed", "_Inner_Edge", 1f);
        CheckFloat(smooth, "WFUI_GlowSdf_HighlightSmoothRed", "_Inner_Fallof", 1f);
        CheckFloat(smooth, "WFUI_GlowSdf_HighlightSmoothRed", "_SpriteAlphaEdge_Inner", 0f);
        CheckFloat(smooth, "WFUI_GlowSdf_HighlightSmoothRed", "_NoiseIntensity", 0.2f);
        CheckFloat(smooth, "WFUI_GlowSdf_HighlightSmoothRed", "_NOISE_CHANNEL", 0f);
        CheckColor(smooth, "WFUI_GlowSdf_HighlightSmoothRed", "_Noise_Speed_Scale_StepLow_StepHigh", 0.219999999f, 0.850000024f, 0f, 0.469999999f);

        var allo = Mat("WFUI_GlowSdf_AllianceTrophy");
        CheckShaderOf("WFUI_GlowSdf_AllianceTrophy", allo, ShGlowSdf);
        CheckFloat(allo, "WFUI_GlowSdf_AllianceTrophy", "_Inner_Edge", 0.945f);
        CheckFloat(allo, "WFUI_GlowSdf_AllianceTrophy", "_Inner_Fallof", 0.56f);
        CheckFloat(allo, "WFUI_GlowSdf_AllianceTrophy", "_SpriteAlphaEdge_Inner", 0.249f);
        CheckFloat(allo, "WFUI_GlowSdf_AllianceTrophy", "_NoiseIntensity", 0.61f);
        CheckFloat(allo, "WFUI_GlowSdf_AllianceTrophy", "_NoiseMaskScale", 1.03999996f);
        CheckFloat(allo, "WFUI_GlowSdf_AllianceTrophy", "_NOISE_CHANNEL", 0f);
        CheckColor(allo, "WFUI_GlowSdf_AllianceTrophy", "_Noise_Speed_Scale_StepLow_StepHigh", 0.050000001f, 1.15999997f, 0f, 1f);

        // 噪声图必须真的挂上去（`_Noise` 是这张效果的命根子；缺了就是一片纯色）
        foreach (var n in new[] { "WFUI_GlowSdf_BorderChest", "WFUI_GlowSdf_BorderChestCollect",
                                  "WFUI_GlowSdf_HighlightRed", "WFUI_GlowSdf_HighlightSmoothRed",
                                  "WFUI_GlowSdf_AllianceTrophy" })
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + n + ".mat");
            Check("`" + n + "`.`_Noise` 贴了噪声图", m != null && m.HasProperty("_Noise") && m.GetTexture("_Noise") != null);
        }

        // `_Time` 驱动：这两条原版读的是 `_TimeParameters.x`（= 秒）⇒ 必须用 `_Time.y`
        var src = ShaderText("WFUIGlowSdfNoise.shader");
        Check("`WFUIGlowSdfNoise.shader` 文件在", src != null);
        if (src != null)
        {
            Check("`WFUIGlowSdfNoise.shader` 用 `_Time.y`（= 秒，原版 `_TimeParameters.x` 口径）", src.Contains("_Time.y"));
            Check("`WFUIGlowSdfNoise.shader` ⛔ 没用 `_Time.x`（那会慢 20 倍）", !src.Contains("_Time.x"));
            Check("`WFUIGlowSdfNoise.shader` 带 2π 常数 `6.283185`", src.Contains("6.283185"));
        }

        // C# 镜像：环（sprite α = 1）比中心（α = 0）亮 —— 防「又画成一张静态图」
        float vRing   = SdfRing(1f, 0.225f, 0.897f, 0.81f);
        float vCenter = SdfRing(0f, 0.225f, 0.897f, 0.81f);
        CheckNear("`SdfRing`（Border Chest 参数）· α=1（外沿）", vRing, 1f, 1e-4f);
        CheckNear("`SdfRing`（Border Chest 参数）· α=0（中心）", vCenter, 0f, 1e-4f);
        Check("`SdfRing`：外沿亮于中心（" + vRing.ToString("R") + " > " + vCenter.ToString("R") + "）", vRing > vCenter + 0.5f);
        // 反例挡板（防「这条断言恒真」）：同一条式子在 α=0.5 处必须落在两者**之间**（阶梯，不是常数）
        float vMid = SdfRing(0.5f, 0.225f, 0.897f, 0.81f);
        Check("`SdfRing` 随 sprite α 单调上升（α=0 时 " + vCenter.ToString("R")
              + " < α=0.5 时 " + vMid.ToString("R") + " < α=1 时 " + vRing.ToString("R") + "）",
              vCenter < vMid && vMid < vRing);
    }

    // ------------------------------------------------------------------ #9 UI Clock

    static void CheckClock()
    {
        Debug.Log("[B109_E33] ---- #9 UI Clock ----");

        // 判据：Material_8837680948194134486.json（唯一一份，参数共享）
        var m = Mat("WFUI_Clock");
        CheckShaderOf("WFUI_Clock", m, ShClock);
        CheckColor(m, "WFUI_Clock", "_Pulse_toOn_On_toOff_Off", 2f, 0f, 2f, 0f);
        CheckColor(m, "WFUI_Clock", "_MinMaxScale", 1.1f, 1f, 0f, 0f);
        CheckFloat(m, "WFUI_Clock", "_Rotation_Speed_Seconds", -0.15915490686893463f);
        CheckFloat(m, "WFUI_Clock", "_Rotation_Speed_Minutes", -0.002652582013979554f);
        CheckColor(m, "WFUI_Clock", "_ClockHandle_Offset_Seconds", 0f, 0.17f, 0f, 0f);
        CheckColor(m, "WFUI_Clock", "_Clock_Handle_Offset_Minutes", 0f, 0.1f, 0f, 0f);
        CheckColor(m, "WFUI_Clock", "_ClockHanldeSeconds_W_H_R", 0.08f, 0.37f, 0.1f, 0f);
        CheckColor(m, "WFUI_Clock", "_ClockHanldeMinutes_W_H_R", 0.08f, 0.25f, 0.1f, 0f);
        CheckFloat(m, "WFUI_Clock", "_SecondsHandleStartOffset", 0f);
        CheckFloat(m, "WFUI_Clock", "_MinutesHandleStartOffset", 0f);

        // `_Time` 驱动：原版读 `_TimeParameters.x`（= 秒）⇒ 必须用 `_Time.y`
        var src = ShaderText("WFUIClock.shader");
        Check("`WFUIClock.shader` 文件在", src != null);
        if (src != null)
        {
            Check("`WFUIClock.shader` 用 `_Time.y`（= 秒，原版 `_TimeParameters.x` 口径）", src.Contains("_Time.y"));
            Check("`WFUIClock.shader` ⛔ 没用 `_Time.x`（那会慢 20 倍）", !src.Contains("_Time.x"));
            Check("`WFUIClock.shader` 带两枚针的宽度/高度/柔化（`_ClockHanldeSeconds_W_H_R`）",
                  src.Contains("_ClockHanldeSeconds_W_H_R"));
            Check("`WFUIClock.shader` 逐像素求导做抗锯齿（`ddx`/`ddy`，原版 `_131`/`_170`）",
                  src.Contains("ddx(") && src.Contains("ddy("));
        }

        // C# 镜像：`HandAngle(t) = t · 速度`；速度 = −1/(2π) ⇒ t = 4π² 时转满一整圈
        float speed = -0.15915490686893463f;
        float tOneTurn = 2f * Mathf.PI / Mathf.Abs(speed);
        CheckNear("秒针一圈的时长（秒）", tOneTurn, 39.478418f, 1e-3f);
        CheckNear("`HandAngle(t=一圈时长)` ≈ −2π", tOneTurn * speed, -2f * Mathf.PI, 1e-3f);
        // 4 s 呼吸：`_Pulse = (toOn 2, On 0, toOff 2, Off 0)`，周期 = 4 s
        CheckNear("脉冲周期 = sum(_Pulse)", 2f + 0f + 2f + 0f, 4f, 1e-6f);
        CheckNear("`_MinMaxScale` 两端（呼吸的幅度）", 1.1f - 1.0f, 0.1f, 1e-6f);
    }

    // ------------------------------------------------------------------ 接线（材质建了必须有人挂）

    static void CheckWiring()
    {
        Debug.Log("[B109_E33] ---- 接线 ----");

        // 判据：R15 §2「按落点文件分组」；⛔ 白名单外的三处（ParentEventWindow / GachaEventWindow / DeckRuntime）
        //   本批**没动**，只有 `待接线` 补丁交回调度台 —— 所以它们**不在**下面的清单里（别写成绿的）。
        AssertCall("Shell/ForgeTab.cs",                    "PulsatingForge");
        AssertCall("Shell/CampaignTab.cs",                 "PulsatingCampaign");
        // 🔴 2026-11-01（`B112W_B187`）：`Collectable Highlight` 那颗原版挂的是 `Border Highlight Rotating Campaign`
        //   （`UI Border Highlight Appear Pulsating Scale`），不是 `UI Glow SDF Noise` 族 ⇒ 这里跟着换口径。
        //   ⚠️ `GlowSdfHighlightSmoothRed` 常量与 `.mat` 仍保留（Social/TrophyDisplay 的 `Collectable Highlight` 用它）。
        AssertCall("Shell/CampaignTab.cs",                 "ApplyBorderHighlightRotatingCampaign");
        AssertCall("Shell/EnergySinglePlayerOnlyEventWindow.cs", "GlowSdfBorderChestCollect");
        AssertCall("Shell/AllianceMemberTab.cs",           "GlowSdfAllianceTrophy");
        AssertCall("Shell/DraftModeWindow.cs",             "GlowSdfBorderChestCollect");
        AssertCall("Shell/MainMenuRuntime.cs",             "Clock");
        AssertCall("Shell/ExpansionPassEventWindow.cs",    "Clock");
        AssertCall("Shell/OfferContainer.cs",              "Clock");

        // 时钟那张图必须换成 `for shader`（不带烤死指针），否则 shader 的针会叠成两套
        var exp = Text("Shell/ExpansionPassEventWindow.cs");
        Check("`ExpansionPassEventWindow` 的时钟图 = `WF_icon_clock_for_shader`",
              exp != null && exp.Contains("\"WF_icon_clock_for_shader\""));
        var off = Text("Shell/OfferContainer.cs");
        Check("`OfferContainer.ClockArt` = `WF_icon_clock_for_shader`",
              off != null && off.Contains("ClockArt = \"WF_icon_clock_for_shader\""));
    }

    static string Text(string csRel)
    {
        var p = "Assets/CardPresentation/" + csRel;
        return File.Exists(p) ? File.ReadAllText(p) : null;
    }

    static void AssertCall(string csRel, string token)
    {
        var t = Text(csRel);
        Check("`" + csRel + "` 文件在", t != null);
        if (t == null) return;
        Check("`" + csRel + "` 接上了 `MenuFxMaterials." + token + "`",
              t.Contains("MenuFxMaterials." + token));
    }
}
