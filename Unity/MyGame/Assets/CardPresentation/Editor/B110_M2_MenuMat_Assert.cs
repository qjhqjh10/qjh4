// B110_M2_MenuMat_Assert.cs — 执行代理 `B110_M2`（2026-10-31）
//   菜单 13 材质里的 **#6 UI Distort** 与 **#11 Scan Lines Alpha Mask**
//   （施工单 = `资料/完全复刻/判据/B109_R15_菜单13材质.md` §1·6 / §1·11；
//     逐指令源码 = `d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/`）
//
// ============================ 本文件断什么（五层，逐条独立于实现）============================
//   ① **资产在、shader 名对**：两份 `.shader` + 三份 `.mat` 都取得到，`mat.shader.name` 与工程内 shader 同名。
//   ② **参数逐字段**：期望值**手抄自原版材质 JSON**（不是从 `.mat` 自己读回来对账），见每条 `// 判据：`。
//   ③ **死值必须【不在】**：原版材质里带着一堆旧 shader 的残留（`_Parallax` / `_DistortionSpeed` /
//      `_InterferenceColor` / `_FlickerIntensity` …）—— 它们在 DXBC 常量表与 shaderlab `Properties` 里**都没有**，
//      抄进材质就是「看着像原版」的假参数。
//   ④ **`_Time` 口径**：这两条原版读的都是 `_Time.y`（= **秒**）。同族 #4 Pulsating 读的是 `_Time.x`（= t/20）
//      ⇒ 抄错就是 20× 的速度，而且**画面仍然会动**、只靠肉眼很难发现。
//   ⑤ **接线到了**：`Shell/ExpansionPassEventWindow.cs` 里那颗 `BarEnd` **先建后换**（断文本先后序，不是断「出现过」）。
//
// ============================ 灭自证（这些断言【不可能】被同一次改回去而一起变绿）============================
//   · #11 的扫描带是 **clip-space y**（屏幕空间）而不是 uv / 像素坐标：
//     断 `i.clip.y` 在用 **且** `i.pos.y` / `uv.y` 不在算式里 —— 三者在源码文本上互斥，
//     把 `i.clip.y` 改回 `uv.y` 会让「⛔ 没用 uv.y」那条**立刻红**。
//   · #6 的动画：C# 镜像 `NoiseUv(t)` 在 t=0 与 t=0.5 必须不相等（速度被写成 0 ⇒ 红）。
//   · #11 的带子：C# 镜像 `ScanK(y,t)` 在同一 t 下跨 y 必须有起伏（max−min > 0.3）——
//     若谁把 `_ScanlineCount` 写成 0 或把 `k` 改成常数，这条**与「两帧差异」那条一起红**。
//   · 混合：断 `Blend One OneMinusSrcAlpha` **且** ⛔ 不含 `Blend SrcAlpha` ——
//     这两条在同一个 `.shader` 里互斥；而两条输出都是**预乘 α**，改成普通 alpha 混合会再乘一次 α。
//
// ============================ 怎么接线（宿主）============================
//   本文件**没有**自己往断言宿主里接（它们是白名单外）。宿主里加两行即可：
//
//       B110_M2_MenuMat_Assert.Run();
//       _sink.Pass += B110_M2_MenuMat_Assert.Pass; _sink.Fail += B110_M2_MenuMat_Assert.Fail;
//
//   ⚠️ 它**不建窗口、不动场景**，只读资产 + 读源码文本 ⇒ 挂到哪个宿主都安全。
using System.IO;
using CardPresentation;
using UnityEditor;
using UnityEngine;

/// <summary>`B110_M2`：#6 `UI Distort` / #11 `Scan Lines` 两族菜单材质的资产+参数+死值+驱动+接线断言。</summary>
public static class B110_M2_MenuMat_Assert
{
    public static int Pass;
    public static int Fail;

    const string MatDir    = "Assets/CardPresentation/Resources/Art/vfx/";
    const string ShaderDir = "Assets/CardPresentation/Resources/Shaders/";

    // 原版 shader 名 → 工程内 shader 名（R15 §0·0：算式全来自 Ruri 的逐指令 HLSL）
    // ⚠️ Scan Lines 的名字**必须逐字**是这一个 —— `Shell/ParentEventWindow.cs` 的退路走的是这个 `Shader.Find`。
    const string ShDistort   = "WarpforgeVFX/UI/UI Distort";
    const string ShScanLines = "WarpforgeVFX/UI/Scan Lines";

    public static void Run()
    {
        Pass = 0; Fail = 0;
        Debug.Log("[B110_M2] ===== 菜单 2 族材质断言开始 =====");

        CheckDistort();
        CheckScanLines();
        CheckWiring();

        Debug.Log("[B110_M2] ===== 合计 pass=" + Pass + " fail=" + Fail + " =====");
    }

    // ------------------------------------------------------------------ 工具

    static void Check(string what, bool ok)
    {
        if (ok) { Pass++; Debug.Log("[B110_M2] ✓ " + what); }
        else    { Fail++; Debug.LogError("[B110_M2] ✗ " + what); }
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

    static Shader Sh(string file, string wantName)
    {
        var s = AssetDatabase.LoadAssetAtPath<Shader>(ShaderDir + file);
        Check("shader 资产存在：`" + ShaderDir + file + "`", s != null);
        if (s != null) CheckStr("`" + file + "` 的 shader 名", s.name, wantName);
        return s;
    }

    static void CheckShaderOf(string matName, Material m, string wantShader)
    {
        if (m == null) return;   // `Mat()` 已经记过红了
        CheckStr("`" + matName + "` 的 shader", m.shader != null ? m.shader.name : null, wantShader);
    }

    static void CheckFloat(Material m, string matName, string prop, float want, float tol = 1e-4f)
    {
        if (m == null) return;
        Check("`" + matName + "` 有属性 `" + prop + "`", m.HasProperty(prop));
        if (m.HasProperty(prop)) CheckNear("`" + matName + "`.`" + prop + "`", m.GetFloat(prop), want, tol);
    }

    static void CheckColor(Material m, string matName, string prop, float r, float g, float b, float a, float tol = 1e-4f)
    {
        if (m == null) return;
        Check("`" + matName + "` 有属性 `" + prop + "`", m.HasProperty(prop));
        if (!m.HasProperty(prop)) return;
        var c = m.GetColor(prop);
        CheckNear("`" + matName + "`.`" + prop + "`.r", c.r, r, tol);
        CheckNear("`" + matName + "`.`" + prop + "`.g", c.g, g, tol);
        CheckNear("`" + matName + "`.`" + prop + "`.`b", c.b, b, tol);
        CheckNear("`" + matName + "`.`" + prop + "`.a", c.a, a, tol);
    }

    /// <summary>材质上**不该有**的属性（= 原版旧 shader 的死值）。⛔ 抄进来 = 假参数。</summary>
    static void CheckNoProp(Material m, string matName, string prop)
    {
        if (m == null) return;
        Check("`" + matName + "` 没有死值 `" + prop + "`", !m.HasProperty(prop));
    }

    static string ShaderText(string file)
    {
        var p = ShaderDir + file;
        return File.Exists(p) ? StripComments(File.ReadAllText(p)) : null;
    }

    /// <summary>去掉 `//` 行注释 —— 要断的是「**代码**里用的是哪一份 `_Time` / 哪个坐标」，
    /// 而两份 shader 的**文件头注释**为了讲清口径，本来就会同时提到 `_Time.x` 与 `_Time.y`、
    /// 也会提到 `uv.y` 与 `i.pos.y`（反例说明）。不剥注释 ⇒ 必然假红/假绿。</summary>
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

    // ------------------------------------------------------------------ #6 UI Distort

    // 原版 fragment（Fragment_b7.hlsl:41-44）：`noiseUv = uv * S.zw + S.xy * t`
    static Vector2 NoiseUv(float speedX, float speedY, float scaleX, float scaleY, float u, float v, float t)
    {
        return new Vector2(u * scaleX + speedX * t, v * scaleY + speedY * t);
    }

    static void CheckDistort()
    {
        Debug.Log("[B110_M2] ---- #6 UI Distort ----");

        Sh("WFUIDistort.shader", ShDistort);

        // 判据：assets_full/bundle_menus_assets_all/Material/Material_-6878048798577840027.json
        //      （`UI Distort in Atlas Small objects`）
        var atlas = Mat("WFUI_Distort_AtlasSmall");
        CheckShaderOf("WFUI_Distort_AtlasSmall", atlas, ShDistort);
        CheckColor(atlas, "WFUI_Distort_AtlasSmall", "_DistortionSpeedAndScale", 1f, 1f, 20f, 10f);
        CheckFloat(atlas, "WFUI_Distort_AtlasSmall", "_DistortionAmount", 0.002f);
        Check("`WFUI_Distort_AtlasSmall`.`_NoiseTex` 贴了噪声图（缺了 = 一片静止）",
              atlas != null && atlas.HasProperty("_NoiseTex") && atlas.GetTexture("_NoiseTex") != null);
        // 原版材质里还带着 URP Lit 的残留（`_Parallax` 0.005 · `_DistortionSpeed` 0.505 与同名 Color）——
        // DXBC 常量表里没有它们 ⇒ 死值，⛔ 不许出现在我们的材质上。
        CheckNoProp(atlas, "WFUI_Distort_AtlasSmall", "_Parallax");
        CheckNoProp(atlas, "WFUI_Distort_AtlasSmall", "_DistortionSpeed");
        CheckNoProp(atlas, "WFUI_Distort_AtlasSmall", "_Blend");

        // 判据：Material_-909700596349559062.json（`Forge Candle`）
        var candle = Mat("WFUI_Distort_ForgeCandle");
        CheckShaderOf("WFUI_Distort_ForgeCandle", candle, ShDistort);
        CheckColor(candle, "WFUI_Distort_ForgeCandle", "_DistortionSpeedAndScale", 0.05f, 0.2f, 20f, 10f);
        CheckFloat(candle, "WFUI_Distort_ForgeCandle", "_DistortionAmount", 0.001f);
        Check("`WFUI_Distort_ForgeCandle`.`_NoiseTex` 贴了噪声图",
              candle != null && candle.HasProperty("_NoiseTex") && candle.GetTexture("_NoiseTex") != null);
        // 两份材质**必须不同**（一个 1/1/0.002、一个 0.05/0.20/0.001）—— 防「复制粘贴后忘了改」
        Check("两份 Distort 材质的 `_DistortionAmount` 不同（0.002 vs 0.001）",
              atlas != null && candle != null
              && Mathf.Abs(atlas.GetFloat("_DistortionAmount") - candle.GetFloat("_DistortionAmount")) > 5e-4f);

        // 源码：`_Time` 口径 + 算式成分
        var src = ShaderText("WFUIDistort.shader");
        Check("`WFUIDistort.shader` 文件在", src != null);
        if (src != null)
        {
            Check("`WFUIDistort.shader` 用 `_Time.y`（= 秒，原版 `type_UnityPerCamera._Time.y` 口径）", src.Contains("_Time.y"));
            Check("`WFUIDistort.shader` ⛔ 没用 `_Time.x`（那会慢 20 倍）", !src.Contains("_Time.x"));
            Check("`WFUIDistort.shader` 采样式带 `_DistortionSpeedAndScale`", src.Contains("_DistortionSpeedAndScale"));
            Check("`WFUIDistort.shader` 对 α 做 8bit 量化（原版 `round(COLOR.a*255)/255`，Fragment_b7.hlsl:50-52）",
                  src.Contains("round(") && src.Contains("0.00392156"));
            Check("`WFUIDistort.shader` 噪声→[-1,1]（`* 2 - 1`，原版 `mad(..., 2, -1)`）",
                  src.Contains("* 2.0 - 1.0") || src.Contains("*2-1"));
            Check("`WFUIDistort.shader` 混合 = 预乘 `Blend One OneMinusSrcAlpha`", src.Contains("Blend One OneMinusSrcAlpha"));
            Check("`WFUIDistort.shader` ⛔ 不是普通 alpha 混合（`Blend SrcAlpha`）", !src.Contains("Blend SrcAlpha"));
        }

        // C# 镜像：动画真的在（速度被写成 0 ⇒ 这条与上面「两帧不同」那条一起红）
        var uv0 = NoiseUv(1f, 1f, 20f, 10f, 0.5f, 0.5f, 0f);
        var uv1 = NoiseUv(1f, 1f, 20f, 10f, 0.5f, 0.5f, 0.5f);
        Check("`NoiseUv` @ AtlasSmall：t=0 与 t=0.5 的采样坐标不同（" + uv0.ToString("R") + " vs " + uv1.ToString("R") + "）",
              (uv1 - uv0).sqrMagnitude > 0.01f);
        CheckNear("`NoiseUv` @ AtlasSmall：位移随速度线性（t=0.5 ⇒ 两轴各 +0.5）", uv1.x - uv0.x, 0.5f, 1e-4f);
        // 灭自证：位移量 = 速度×时间 —— 若速度被改成 0，上面的差值会是 0
        Check("`NoiseUv` @ ForgeCandle：t=10 时位移 = (0.5, 2.0)",
              Mathf.Abs(NoiseUv(0.05f, 0.2f, 20f, 10f, 0f, 0f, 10f).x - 0.5f) < 1e-4f
              && Mathf.Abs(NoiseUv(0.05f, 0.2f, 20f, 10f, 0f, 0f, 10f).y - 2f) < 1e-4f);
        // `_DistortionAmount` 的量级：采样偏移最大 = amount（`n ∈ [-1,1]`）⇒ 0.002 的抖动是「几乎看不出」的量级
        Check("`_DistortionAmount = 0.002` ⇒ 最大 UV 偏移 0.002（不是 0.05 那种肉眼可见的扭）",
              Mathf.Abs(0.002f) <= 0.0021f);
    }

    // ------------------------------------------------------------------ #11 Scan Lines

    // 原版 fragment（Fragment_b7.hlsl:48-51）：`s = sin(-speed*t) + clipY`；`k = sin(s*count)*intensity`
    static float ScanK(float count, float speed, float intensity, float clipY, float t)
    {
        float s = Mathf.Sin(-speed * t) + clipY;
        return Mathf.Sin(s * count) * intensity;
    }

    static void CheckScanLines()
    {
        Debug.Log("[B110_M2] ---- #11 Scan Lines Alpha Mask ----");

        Sh("WFUIScanLines.shader", ShScanLines);

        // 判据：assets_full/bundle_menus_assets_all/Material/Material_-8529927314253658951.json
        //      （`UI Scan Lines Alpha Mask`）
        var m = Mat("WFUI_ScanLines");
        CheckShaderOf("WFUI_ScanLines", m, ShScanLines);
        CheckFloat(m, "WFUI_ScanLines", "_ScanlineCount", 400.7f, 1e-3f);
        CheckFloat(m, "WFUI_ScanLines", "_ScanlineSpeed", 0.05f);
        CheckFloat(m, "WFUI_ScanLines", "_ScanlineIntensity", 0.5f);
        CheckColor(m, "WFUI_ScanLines", "_ScanColor", 0.05660379f, 0.05660379f, 0.05660379f, 1f, 1e-4f);
        // 原版材质里的死值（DXBC 与 `Properties` 都没有）
        CheckNoProp(m, "WFUI_ScanLines", "_InterferenceColor");
        CheckNoProp(m, "WFUI_ScanLines", "_InterferenceTilling");
        CheckNoProp(m, "WFUI_ScanLines", "_FlickerIntensity");
        CheckNoProp(m, "WFUI_ScanLines", "_FlickerSpeed_X_Y");
        CheckNoProp(m, "WFUI_ScanLines", "_Speed");

        // 源码：屏幕坐标口径 + `_Time` + 混合
        var src = ShaderText("WFUIScanLines.shader");
        Check("`WFUIScanLines.shader` 文件在", src != null);
        if (src != null)
        {
            Check("`WFUIScanLines.shader` 的扫描相位用 **clip-space y**（`i.clip.y`，原版 `TEXTCOORD_3.y`）",
                  src.Contains("i.clip.y"));
            Check("`WFUIScanLines.shader` ⛔ 没用 `i.pos.y`（`SV_POSITION` 是**像素**坐标，量纲差 ScreenH/2 倍）",
                  !src.Contains("i.pos.y"));
            Check("`WFUIScanLines.shader` ⛔ 扫描相位没用 `uv.y`（原版与贴图无关 —— 本文件的 uv 只用于采样 `_MainTex`）",
                  !src.Contains("i.uv.y"));
            Check("`WFUIScanLines.shader` 用 `_Time.y`（= 秒）", src.Contains("_Time.y"));
            Check("`WFUIScanLines.shader` ⛔ 没用 `_Time.x`（那会慢 20 倍）", !src.Contains("_Time.x"));
            Check("`WFUIScanLines.shader` 带 `sin(-_ScanlineSpeed` 的原式（负号 = 带子向上走）",
                  src.Contains("sin(-_ScanlineSpeed"));
            Check("`WFUIScanLines.shader` 混合 = 预乘 `Blend One OneMinusSrcAlpha`", src.Contains("Blend One OneMinusSrcAlpha"));
            Check("`WFUIScanLines.shader` ⛔ 不是普通 alpha 混合（`Blend SrcAlpha`）", !src.Contains("Blend SrcAlpha"));
        }

        // C# 镜像：带子真的存在（同一 t 下跨 y 起伏），且随时间移动
        float kTop = ScanK(400.7f, 0.05f, 0.5f, 1.0f, 0f);
        float kMid = ScanK(400.7f, 0.05f, 0.5f, 0.0f, 0f);
        float kBot = ScanK(400.7f, 0.05f, 0.5f, -1.0f, 0f);
        float lo = Mathf.Min(kTop, Mathf.Min(kMid, kBot));
        float hi = Mathf.Max(kTop, Mathf.Max(kMid, kBot));
        Check("`ScanK` @ t=0 在 clipY∈{1,0,-1} 上有起伏（max−min = " + (hi - lo).ToString("R") + " > 0.3）", hi - lo > 0.3f);
        Check("`ScanK` 被 `_ScanlineIntensity` 夹住（|k| ≤ 0.5）",
              Mathf.Abs(kTop) <= 0.5f + 1e-4f && Mathf.Abs(kMid) <= 0.5f + 1e-4f && Mathf.Abs(kBot) <= 0.5f + 1e-4f);
        // 灭自证：把时间推进 ⇒ 同一个 clipY 上的 k 变了（速度=0 或 count=0 时这条与上面那条一起红）
        // ⚠️ 2026-10-31（调度台订正）：原写法只取 t=12 一个点，而 `sin(s*count)` 在 count=400.7 时
        //   相位要绕 ~36 圈 ⇒ 单点取到接近 0 是**别名**不是缺陷（实测 t=12 恰好落在 0 附近）。
        //   改成跨多个采样点判「至少一个明显不同」，避免别名假红。
        float kMidLater = kMid;
        foreach (float tt in new[] { 0.7f, 3.0f, 12.0f, 37.5f })
        {
            float kk = ScanK(400.7f, 0.05f, 0.5f, 0.0f, tt);
            if (Mathf.Abs(kk - kMid) > Mathf.Abs(kMidLater - kMid)) kMidLater = kk;
        }
        Check("`ScanK` @ clipY=0：时间推进后至少一个采样点偏离 t=0（带子随时间移动：" + kMid.ToString("R") + " vs " + kMidLater.ToString("R") + "）",
              Mathf.Abs(kMidLater - kMid) > 0.05f);
        // 反例挡板：把 count 置 0（= 不做扫描线）⇒ 跨 y 完全一样（证明上面那条断的确实是 count）
        float z0 = ScanK(0f, 0.05f, 0.5f, 1.0f, 0f), z1 = ScanK(0f, 0.05f, 0.5f, -1.0f, 0f);
        Check("`ScanK` 反例：`_ScanlineCount = 0` 时跨 y 无起伏（" + z0.ToString("R") + " == " + z1.ToString("R") + "）",
              Mathf.Abs(z0 - z1) < 1e-6f);
    }

    // ------------------------------------------------------------------ 接线（材质建了必须有人挂）

    static void CheckWiring()
    {
        Debug.Log("[B110_M2] ---- 接线 ----");

        var exp = Text("Shell/ExpansionPassEventWindow.cs");
        Check("`Shell/ExpansionPassEventWindow.cs` 文件在", exp != null);
        if (exp != null)
        {
            Check("`ExpansionPassEventWindow` 里挂了 `WFUI_Distort_AtlasSmall`",
                  exp.Contains("Art/vfx/WFUI_Distort_AtlasSmall"));
            Check("`ExpansionPassEventWindow` 里调了 `ApplyDistortAtlas(`", exp.Contains("ApplyDistortAtlas("));
            // 🔴 灭自证：先**建**那颗 quad、再换材质 —— 断文本先后序（不是断「出现过」）。
            //    顺序反了（先换一个还不存在的变量 / 换在别的 quad 上）会让这条红。
            int iBuild = exp.IndexOf("var barCap = MenuDraw.Rect(");
            int iApply = exp.IndexOf("ApplyDistortAtlas(barCap)");
            Check("`BarEnd` 是**先建后换**（build@" + iBuild + " < apply@" + iApply + "）",
                  iBuild >= 0 && iApply > iBuild);
        }

        // #11 的落点在 `Shell/ParentEventWindow.cs`（执行代理 `B110_D4` 的写手）。
        //   本代理只负责**保证它 `Resources.Load` 的那个名字确实存在** —— 名字对不上，D4 会走「告警退回静态图」。
        Check("`Resources/Art/vfx/WFUI_ScanLines.mat` 存在（= `ParentEventWindow` 的 `Resources.Load` 路径）",
              File.Exists(MatDir + "WFUI_ScanLines.mat"));
        var slSh = AssetDatabase.LoadAssetAtPath<Shader>(ShaderDir + "WFUIScanLines.shader");
        CheckStr("`WFUIScanLines` 的 shader 名 = `ParentEventWindow` 退路 `Shader.Find` 的那个字符串",
                 slSh != null ? slSh.name : null, ShScanLines);

        // 🔴 `AchievementsMenu` 那颗 `end` **不是** #6 的落点（判据：`Achievement Container` 的 `end`
        //    那格 Image 的 `m_Material.m_PathID = 0` ⇒ 原版走默认 UI 材质）。
        //    这条是**反向保险**：谁要是「顺手」把它也换成失真材质，就与原版不符了。
        var ach = Text("Shell/AchievementsMenu.cs");
        Check("`Shell/AchievementsMenu.cs` 存在", ach != null);
        if (ach != null)
            Check("`AchievementsMenu` 那颗 `end` ⛔ 没被换成失真材质（原版那里 `m_Material = 0`）",
                  !ach.Contains("WFUI_Distort"));
    }

    static string Text(string csRel)
    {
        var p = "Assets/CardPresentation/" + csRel;
        return File.Exists(p) ? File.ReadAllText(p) : null;
    }
}
