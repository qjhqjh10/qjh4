// B110_M3_MenuMat_Assert.cs — 执行代理 `B110_M3`（2026-10-31 · 第一百一十会话）
//   菜单 13 材质批 · **#7 UI Additive**（施工单 = `资料/完全复刻/判据/B109_R15_菜单13材质.md` §1·7 / §2）
//
// ============================ 本文件断什么（四层，逐条独立于实现）============================
//   ① **资产在、shader 名对**：`WFUI_Additive.mat` 取得到，`mat.shader.name == "WarpforgeVFX/UI/Additive"`。
//   ② **参数/死值**：期望值**手抄自原版材质 JSON**（`Material_-8464978790139598553.json`），
//      并断那三样「上一颗 shader 的残留」**不在**我们的材质上（⛔ 抄了就是假参数）。
//   ③ **混合模式（本条的【全部】差异）**：读 shader 源文本，断 `Blend One One, One One` 在、`OneMinusSrcAlpha` 不在。
//   ④ **接线到了 / 且没被行末注释吞掉**：本项目 2026-10-30 踩过「追加代码落在行末 `//` 之后 ⇒
//      typecheck 0/0、账闸全绿、断言一条没跑」⇒ 这条必须按**行内位置**判，不能只 `Contains`。
//
// ============================ C# 镜像（把 fragment 闭式算式抄成 C#，验的是【抄对了】）============================
//   `QuantA(a) = round(a*255)/255` · `MirrorA(a, texA) = QuantA(a)*texA` ·
//   `MirrorRgb(a, col, tex) = MirrorA(a,texA) * col * tex`
//   ⚠️ 这三条**不是**渲染断言（本批不跑 Unity 批处理）；真·两帧像素差交真机验收（R15 §3·4）。
//
// ============================ 「灭自证」三条（⛔ 别删）============================
//   · `OneMinusSrcAlpha` **不得出现**在 shader 代码里（任何退回 alpha 混合的改动都会红）。
//   · a=0 ⇒ 输出必须 **0**（漏乘 `outA` 的「预乘」写法会输出 texRGB，静默变亮）。
//   · 叠在**亮底**（dst = 0.8）上：加法结果必须比 alpha 混合结果**亮 > 0.2** ——
//     这就是 #7 的语义本体（「叠在亮底上应变亮，而不是盖住」），⛔ 不是「换了个写法也全绿」。
//
// ============================ 怎么接线（宿主）============================
//   本文件**没有**自己往断言宿主里接（宿主是白名单外）。宿主里加两行即可：
//
//       B110_M3_MenuMat_Assert.Run();
//       pass += B110_M3_MenuMat_Assert.Pass; fail += B110_M3_MenuMat_Assert.Fail;
//
//   ⚠️ 它**不建窗口、不动场景、不跑播放**，只读资产 + 读源码文本 ⇒ 挂到哪个宿主都安全。
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>`B110_M3`：#7 UI Additive —— 资产 / 参数 / 混合模式 / 接线 四层断言。</summary>
public static class B110_M3_MenuMat_Assert
{
    public static int Pass;
    public static int Fail;

    const string MatDir    = "Assets/CardPresentation/Resources/Art/vfx/";
    const string ShaderDir = "Assets/CardPresentation/Resources/Shaders/";
    const string ShaderFile = "WFUIAdditive.shader";
    const string MatName    = "WFUI_Additive";
    const string ShaderName = "WarpforgeVFX/UI/Additive";   // 原版 `Everguild/UI/Additive`（**没有**第二个 `UI `）

    public static void Run()
    {
        Pass = 0; Fail = 0;
        Debug.Log("[B110_M3] ===== #7 UI Additive 断言开始 =====");

        CheckMaterial();
        CheckShaderSource();
        CheckMirror();
        CheckWiring();

        Debug.Log("[B110_M3] ===== 合计 pass=" + Pass + " fail=" + Fail + " =====");
    }

    // ------------------------------------------------------------------ 工具

    static void Check(string what, bool ok)
    {
        if (ok) { Pass++; Debug.Log("[B110_M3] ✓ " + what); }
        else    { Fail++; Debug.LogError("[B110_M3] ✗ " + what); }
    }

    static void CheckNear(string what, float got, float want, float tol)
    {
        Check(what + "（实测 " + got.ToString("R") + " · 期望 " + want.ToString("R")
              + " ±" + tol.ToString("R") + "）", Mathf.Abs(got - want) <= tol);
    }

    static string Text(string assetRel)
    {
        var p = "Assets/CardPresentation/" + assetRel;
        return File.Exists(p) ? File.ReadAllText(p) : null;
    }

    /// <summary>去掉 `//` 行注释 —— 本文件要断的是「**代码**里用的是哪一套 blend」，
    /// 而 shader 的**文件头注释**为了讲清口径会同时提到 `Blend One One` 与各种反例名。
    /// ⛔ 不剥注释 ⇒ 那条「⛔ 不含 OneMinusSrcAlpha」会被自己的注释骗成假红（本项目栽过）。</summary>
    static string StripLineComments(string s)
    {
        if (s == null) return null;
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

    /// <summary>判 `token` 是否出现在**某一行代码的行首**（剥掉行注释之后、`Trim()` 后 `StartsWith`）。
    /// 🔴 为什么不用 `Contains`：文件头注释里也会写 `Blend One One, One One` / `ColorMask [_ColorMask]`
    /// ⇒ 只 `Contains` 的话，**把真正的 Pass 行删掉，注释还能让断言变绿**（自证）。</summary>
    static bool HasCodeLine(string stripped, string token)
    {
        if (stripped == null) return false;
        foreach (var line in stripped.Split('\n'))
            if (line.Trim().StartsWith(token, System.StringComparison.Ordinal)) return true;
        return false;
    }

    // ------------------------------------------------------------------ ① 资产 / 参数

    static void CheckMaterial()
    {
        Debug.Log("[B110_M3] ---- ① 资产 / 参数 ----");

        var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + MatName + ".mat");
        Check("材质存在：`" + MatDir + MatName + ".mat`", m != null);
        if (m == null) return;

        Check("`" + MatName + "` 的 shader 名（实测 `" + (m.shader != null ? m.shader.name : "null")
              + "` · 期望 `" + ShaderName + "`）",
              m.shader != null && m.shader.name == ShaderName);

        // 判据：assets_full/bundle_menus_assets_all/Material/Material_-8464978790139598553.json
        //   `_Color = (1,1,1,1)` · `_ColorMask 15` · `_StencilComp 8` · `_Stencil 0` · `_StencilOp 0`
        //   `_StencilReadMask / _StencilWriteMask = 255` · `_UseUIAlphaClip 0`
        var c = m.HasProperty("_Color") ? m.GetColor("_Color") : new Color(-9, -9, -9, -9);
        Check("`" + MatName + "`.`_Color` = 白 (1,1,1,1)（原版材质实读）",
              Mathf.Abs(c.r - 1f) < 1e-4f && Mathf.Abs(c.g - 1f) < 1e-4f
              && Mathf.Abs(c.b - 1f) < 1e-4f && Mathf.Abs(c.a - 1f) < 1e-4f);
        CheckNear("`" + MatName + "`.`_ColorMask`", m.HasProperty("_ColorMask") ? m.GetFloat("_ColorMask") : -1f, 15f, 1e-4f);
        CheckNear("`" + MatName + "`.`_StencilComp`", m.HasProperty("_StencilComp") ? m.GetFloat("_StencilComp") : -1f, 8f, 1e-4f);
        CheckNear("`" + MatName + "`.`_StencilReadMask`", m.HasProperty("_StencilReadMask") ? m.GetFloat("_StencilReadMask") : -1f, 255f, 1e-4f);
        CheckNear("`" + MatName + "`.`_StencilWriteMask`", m.HasProperty("_StencilWriteMask") ? m.GetFloat("_StencilWriteMask") : -1f, 255f, 1e-4f);
        CheckNear("`" + MatName + "`.`_UseUIAlphaClip`（= 0 ⇒ 走 catch-all 变体 b7）",
                  m.HasProperty("_UseUIAlphaClip") ? m.GetFloat("_UseUIAlphaClip") : -1f, 0f, 1e-4f);

        // 🔴 死值：原版那份 JSON 里还躺着 `_ColorMain 337.79…` / `_Reveal 0.723` /
        //    `_Noise_Strength_X_Speed_Y_Scale_Z 0.005/0/8.17` —— 那是**上一颗 shader 的残留**
        //    （本 shader 的 `m_PropInfo` 里没有这些名字）。抄进来就是「看着有依据」的假参数。
        Check("`" + MatName + "` ⛔ 没有死值 `_ColorMain`", !m.HasProperty("_ColorMain"));
        Check("`" + MatName + "` ⛔ 没有死值 `_Reveal`", !m.HasProperty("_Reveal"));
        Check("`" + MatName + "` ⛔ 没有死值 `_Noise_Strength_X_Speed_Y_Scale_Z`",
              !m.HasProperty("_Noise_Strength_X_Speed_Y_Scale_Z"));

        // `ImageQuad.SetMaterial` 会 `m.mainTexture = _tex` ⇒ 材质**必须**留 `_MainTex`，否则换完是空白
        Check("`" + MatName + "` 保留了 `_MainTex`（`ImageQuad.SetMaterial` 写的就是 `material.mainTexture`）",
              m.HasProperty("_MainTex"));
    }

    // ------------------------------------------------------------------ ② 混合模式 / 算式（shader 源）

    static void CheckShaderSource()
    {
        Debug.Log("[B110_M3] ---- ② 混合模式 / 算式 ----");

        var raw = Text("Resources/Shaders/" + ShaderFile);
        Check("`" + ShaderFile + "` 文件在", raw != null);
        if (raw == null) return;
        var src = StripLineComments(raw);

        // 🔴 本条的全部差异：原版 `Blend One One, One One`（RGB 与 A 两路都是纯加法）
        //    ⚠️ 必须按「**代码行**」判，不能只 `Contains` —— 文件头注释里也写着这串
        //    （B109_E33 的先例就是只剥注释、不判行首 ⇒ 真行被删掉后注释还能让它变绿）。
        Check("`" + ShaderFile + "` 的【代码行】里 `Blend One One, One One` 在（RGB + A 两路纯加法）",
              HasCodeLine(src, "Blend One One, One One"));
        // 灭自证：任何退回 uGUI 默认 alpha 混合的改动都会红
        Check("`" + ShaderFile + "` ⛔ 代码里没有 `OneMinusSrcAlpha`（退回 alpha 混合就是没复刻）",
              !src.Contains("OneMinusSrcAlpha"));

        // Fragment_b7.hlsl:31-48 —— `_34 = round(COLOR.w*255)/255` · `_82 = _34 * tex.w`
        //   `out.rgb = _82 * COLOR.rgb * tex.rgb` · `out.a = _82`
        Check("`" + ShaderFile + "` 有 8bit 量化 `round(i.color.w * 255.0)`（原版 `_34`）",
              src.Contains("round(i.color.w * 255.0)"));
        Check("`" + ShaderFile + "` 的 RGB = 预乘 alpha × 顶点色 × 贴图（原版 `_83.._85`）",
              src.Contains("outA * i.color.rgb * tx.rgb"));
        Check("`" + ShaderFile + "` 的 A = `outA`（原版 `SV_Target.w = _82`）",
              src.Contains("tx.rgb, outA)"));
        Check("`" + ShaderFile + "` 顶点色 = `v.color * _Color`（原版 Vertex_b1:80-87）",
              src.Contains("v.color * _Color"));

        // 骨架先例（URP 17.3.0）：`CGPROGRAM` + **不写 LightMode** ⇒ 走 SRPDefaultUnlit
        Check("`" + ShaderFile + "` ⛔ 没写 `LightMode`（写了就走不到 SRPDefaultUnlit，与 WFUIPulsating 先例冲突）",
              !src.Contains("LightMode"));
        Check("`" + ShaderFile + "` 带完整 Stencil 块（原版 `_Stencil*` 五个字段）",
              src.Contains("[_StencilComp]") && src.Contains("[_StencilReadMask]") && src.Contains("[_StencilWriteMask]"));
        Check("`" + ShaderFile + "` 带 `ColorMask [_ColorMask]` 与 `ZClip On`（原版渲染状态，按代码行判）",
              HasCodeLine(src, "ColorMask [_ColorMask]") && HasCodeLine(src, "ZClip On"));
        Check("`" + ShaderFile + "` 声明的 shader 名 = `" + ShaderName + "`",
              src.Contains("Shader \"" + ShaderName + "\""));
    }

    // ------------------------------------------------------------------ ③ C# 镜像（含灭自证）

    /// <summary>原版 `_34 = round(COLOR.w*255)/255`。</summary>
    static float QuantA(float a) { return Mathf.Round(a * 255f) / 255f; }

    /// <summary>原版 `_82 = _34 * tex.a`（= 我们输出到 SV_Target.w 的那个量）。</summary>
    static float MirrorA(float colorA, float texA) { return QuantA(colorA) * texA; }

    /// <summary>原版 `SV_Target.rgb = _82 * COLOR.rgb * tex.rgb`。</summary>
    static float MirrorRgb(float colorA, float colorChannel, float texA, float texChannel)
    {
        return MirrorA(colorA, texA) * colorChannel * texChannel;
    }

    static void CheckMirror()
    {
        Debug.Log("[B110_M3] ---- ③ C# 镜像 / 灭自证 ----");

        // 8bit 量化真在做（0.5 → round(127.5) = 128 → 0.50196…，与 0.5 差 1/510）
        CheckNear("`QuantA(0.5)` = 128/255（原版四舍五入到 8bit）", QuantA(0.5f), 128f / 255f, 1e-6f);
        Check("`QuantA(0.5)` 与 0.5 的差 > 1e-4（挡「直接乘、没量化」）",
              Mathf.Abs(QuantA(0.5f) - 0.5f) > 1e-4f);

        // 灭自证：a = 0 ⇒ 必须输出 0（漏乘 `outA` 的「非预乘」写法会输出 texRGB）
        CheckNear("`MirrorRgb(a=0, …)` = 0（预乘；漏乘会得到 texRGB）",
                  MirrorRgb(0f, 1f, 1f, 1f), 0f, 1e-6f);
        CheckNear("`MirrorA(a=0, …)` = 0", MirrorA(0f, 1f), 0f, 1e-6f);

        // 原版算式手算：a=0.34、col=1、tex.a=1 ⇒ outA = round(86.7)/255 = 87/255 = 0.341176
        float outA = MirrorA(0.3411765f, 1f);
        CheckNear("`MirrorA(0.3411765, texA=1)` = 87/255", outA, 87f / 255f, 1e-6f);
        CheckNear("`MirrorRgb(0.3411765, col=1, tex=1)` = 87/255", MirrorRgb(0.3411765f, 1f, 1f, 1f), 87f / 255f, 1e-6f);

        // 🔴 灭自证（语义本体）：叠在**亮底**上，加法必须**更亮**、而且不是 alpha 混合那个数
        const float dst = 0.8f;                       // 亮底
        float src = MirrorRgb(0.3411765f, 1f, 1f, 1f); // 我们这颗灰描边的源色 = 87/255
        float srcA = MirrorA(0.3411765f, 1f);
        float additive = dst + src;                    // Blend One One
        float alphaBlend = src * srcA + dst * (1f - srcA);   // SrcAlpha OneMinusSrcAlpha
        Check("叠在亮底(dst=0.8)上：加法结果(" + additive.ToString("R") + ") 比 alpha 混合("
              + alphaBlend.ToString("R") + ") 亮 > 0.2", additive > alphaBlend + 0.2f);

        // 反例挡板（防「这条恒真」）：换到**暗底**上，两者必须接近（>0 但不该再差 0.2）
        const float dstDark = 0.0f;
        float additiveDark = dstDark + src;
        float alphaDark = src * srcA + dstDark * (1f - srcA);
        Check("暗底(dst=0)上：加法(" + additiveDark.ToString("R") + ") 与 alpha 混合("
              + alphaDark.ToString("R") + ") 的差 < 0.25（说明上面那条不是恒真）",
              Mathf.Abs(additiveDark - alphaDark) < 0.25f);
    }

    // ------------------------------------------------------------------ ④ 接线（材质建了必须有人挂，且不能被注释吞掉）

    static void CheckWiring()
    {
        Debug.Log("[B110_M3] ---- ④ 接线 ----");

        var mm = Text("Shell/MainMenuRuntime.cs");
        Check("`Shell/MainMenuRuntime.cs` 文件在", mm != null);
        if (mm != null)
        {
            Check("`Shell/MainMenuRuntime.cs` 接上了 `MenuFxMaterials.ApplyAll(<Border 九宫>, \"Art/vfx/WFUI_Additive\")`",
                  mm.Contains("MenuFxMaterials.ApplyAll(borderNine, \"Art/vfx/WFUI_Additive\")"));
            Check("`Shell/MainMenuRuntime.cs` 把 `MenuDraw.Nine(…)` 的返回值接进了 `borderNine`（否则 ApplyAll(null) 静默无效）",
                  mm.Contains("var borderNine = MenuDraw.Nine("));

            // 🔴 2026-10-30 那条坑：追加代码落在**行末 `//` 之后** ⇒ 看着接上了、跑起来一行不执行。
            //    判据 = 在**同一行**里，`ApplyAll` 必须出现在第一个 `//` **之前**。
            Check("`Shell/MainMenuRuntime.cs` 那行 `ApplyAll(…)` **不在行末注释里**（行内第一个 `//` 必须在它之后）",
                  CallBeforeLineComment(mm, "MenuFxMaterials.ApplyAll(borderNine"));
        }

        // 两条线（本条 = 主菜单卡；`B110_D4` = 两方活动窗）共用**同一份**资产与**同一个** shader 名。
        // D4 的 `ParentEventWindow.cs` 走 `MenuFxMaterials.Apply(…, Additive/ScanLines + Shader.Find 名)`（`B111_S4` 收口后），
        // 任一边改名都会让另一边**静默**退回默认材质 ⇒ 这条交叉断言就是那道闸。
        // ⚠️ `B111_S4`（2026-11-01）把路径字面量收进了 `MenuFxMaterials.Additive` 常量 ⇒ 下面第一条改成「字面量或常量」二选一。
        var pe = Text("Shell/ParentEventWindow.cs");
        Check("`Shell/ParentEventWindow.cs` 文件在", pe != null);
        if (pe != null)
        {
            Check("两方活动窗（D4）用的材质与本条同一份（字面量或 `MenuFxMaterials.Additive`）",
                  pe.Contains("\"Art/vfx/WFUI_Additive\"") || pe.Contains("MenuFxMaterials.Additive"));
            Check("两方活动窗（D4）的 `Shader.Find` 兜底名与本条 shader 名一致：" + ShaderName,
                  pe.Contains("\"" + ShaderName + "\""));
        }
    }

    /// <summary>在整份文本里找 `token` 出现的那一行，判它的**第一个 `//`** 是不是在 `token` 之后。
    /// 找不到 token ⇒ false（由上面那条 `Contains` 已经记过红了）。</summary>
    static bool CallBeforeLineComment(string text, string token)
    {
        if (text == null) return false;
        int idx = text.IndexOf(token, System.StringComparison.Ordinal);
        if (idx < 0) return false;
        int lineStart = text.LastIndexOf('\n', idx) + 1;
        int comment = text.IndexOf("//", lineStart, System.StringComparison.Ordinal);
        return comment < 0 || comment > idx;
    }
}
