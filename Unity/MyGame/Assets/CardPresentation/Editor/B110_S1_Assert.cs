// B110_S1_Assert.cs — 执行代理 `B110_S1`（2026-10-31）· 菜单 13 材质 **#3 `FX Shine For Animation Normal`**
//   施工单 = `资料/完全复刻/报告/B110_M3.md` §「#3」
//
// ============================ 本文件断什么（五层，逐条独立于实现）============================
//   ① **资产 / 参数**：`WFUI_FxShine.mat` 在、shader 名对；**七个参数逐字段照原版材质 JSON**
//      （`bundle_menus_assets_all/Material/Material_1349812993706917952.json`）；两个关键字是开的；
//      死值 `_ShineScale`（原版那份 JSON 里有、本 shader 属性表里**没有**）⛔ 不许抄进来。
//   ② **shader 源**：渲染状态三件按**代码行**判（`Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha` /
//      `ZWrite Off` / `Cull Off`）；`_ProjectionParams.x < 0` 那条镜像补偿在；sRGB 编码三常量在；
//      **没有 `LightMode`**（URP 走 `SRPDefaultUnlit`，与 `WFUIPulsating` / `WFUIAdditive` 同一条先例）。
//   ③ **贴图**：`Shine trail.png` 在工程里、128×128、且**真的被那颗材质引用**（不是「导进来了没人用」）。
//   ④ **C# 镜像 + 灭自证**：`DoShine` 的四个常量（`.rdata` 直读）· sRGB 编码的语义（HDR 编码后仍然 >1；
//      不编码会差 > 0.1 ⇒ 「把编码这一步删掉」会红）· 扫光**沿 y** 移动（起点终点 x 必须都是 0）。
//   ⑤ **接线**：`OnSelectClicked` 那一行先 `DoShine(ShineDuration)` 再 `ConfirmSelect()`（= 原版
//      `OnChoseAvatar` 的顺序）；材质挂上去时 **tint 被存/恢复**（本仓踩过两次）；`Resources.Load` 的路径
//      与材质文件路径一致；且 `AvatarTab.cs` **没有**加 `using DG.Tweening;`（加了会挪下游账落点行号）。
//
// ============================ 怎么接线（宿主）============================
//   本文件**没有**自己往断言宿主里接（宿主是白名单外）。宿主里加两行即可：
//
//       B110_S1_Assert.Run();
//       pass += B110_S1_Assert.Pass; fail += B110_S1_Assert.Fail;
//
//   ⚠️ 它**不建窗口、不动场景、不跑播放**，只读资产 + 读源码文本 ⇒ 挂到哪个宿主都安全。
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>`B110_S1`：#3 FX Shine —— 资产 / 参数 / shader 源 / 贴图 / C# 镜像 / 接线。</summary>
public static class B110_S1_Assert
{
    public static int Pass;
    public static int Fail;

    const string MatDir     = "Assets/CardPresentation/Resources/Art/vfx/";
    const string MatName    = "WFUI_FxShine";
    const string ShaderDir  = "Assets/CardPresentation/Resources/Shaders/";
    const string ShaderFile = "WFUIFxShine.shader";
    const string ShaderName = "WarpforgeVFX/UI/FX Shine For Animation Normal";   // 原版 `Everguild/FX/FX Shine For Animation Normal`

    /// <summary>原版材质里那张扫光图的属性名（ShaderGraph 的 GUID 派生名，**逐字照搬**）。</summary>
    const string ShineTexProp = "Texture2D_0bfdc50de139497fa85c0cd08848879a";
    const string ShineColProp = "Color_a142b0353b4945a49e1990789c04ea35";
    const string UvRotProp    = "Vector1_c76b369469204ddeb1ee5d707098307c";

    const string TexDir  = "Assets/CardPresentation/Resources/Art/vfx/";
    const string TexFile = "Shine trail.png";

    public static void Run()
    {
        Pass = 0; Fail = 0;
        Debug.Log("[B110_S1] ===== #3 FX Shine 断言开始 =====");

        CheckMaterial();
        CheckShaderSource();
        CheckTexture();
        CheckMirror();
        CheckWiring();

        Debug.Log("[B110_S1] ===== 合计 pass=" + Pass + " fail=" + Fail + " =====");
    }

    // ------------------------------------------------------------------ 工具

    static void Check(string what, bool ok)
    {
        if (ok) { Pass++; Debug.Log("[B110_S1] ✓ " + what); }
        else    { Fail++; Debug.LogError("[B110_S1] ✗ " + what); }
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

    /// <summary>去掉 `//` 行注释 —— 要断的是「**代码**里用的是哪一套渲染状态」，
    /// 而 shader 文件头为了讲清口径会把 `Blend …` 之类**再写一遍**（只 `Contains` 会被自己的注释骗绿）。</summary>
    static string StripLineComments(string s)
    {
        if (s == null) return null;
        var sb = new System.Text.StringBuilder(s.Length);
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '/')
            { while (i < s.Length && s[i] != '\n') i++; }
            else sb.Append(s[i++]);
        }
        return sb.ToString();
    }

    /// <summary>`token` 是否出现在**某一行代码的行首**（剥注释后 `Trim().StartsWith`）。</summary>
    static bool HasCodeLine(string stripped, string token)
    {
        if (stripped == null) return false;
        foreach (var line in stripped.Split('\n'))
            if (line.Trim().StartsWith(token, System.StringComparison.Ordinal)) return true;
        return false;
    }

    /// <summary>在整份文本里找 `token` 出现的那一行，判它的**第一个 `//`** 是不是在 `token` 之后。
    /// 🔴 2026-10-30 的坑：追加代码落在**行末 `//` 之后** ⇒ 看着接上了、跑起来一行不执行。</summary>
    static bool CallBeforeLineComment(string text, string token)
    {
        if (text == null) return false;
        int idx = text.IndexOf(token, System.StringComparison.Ordinal);
        if (idx < 0) return false;
        int lineStart = text.LastIndexOf('\n', idx) + 1;
        int comment = text.IndexOf("//", lineStart, System.StringComparison.Ordinal);
        return comment < 0 || comment > idx;
    }

    // ------------------------------------------------------------------ ① 资产 / 参数

    static void CheckMaterial()
    {
        Debug.Log("[B110_S1] ---- ① 资产 / 参数 ----");

        var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + MatName + ".mat");
        Check("材质存在：`" + MatDir + MatName + ".mat`", m != null);
        if (m == null) return;

        Check("`" + MatName + "` 的 shader 名（实测 `" + (m.shader != null ? m.shader.name : "null")
              + "` · 期望 `" + ShaderName + "`）",
              m.shader != null && m.shader.name == ShaderName);

        // 判据：assets_full/bundle_menus_assets_all/Material/Material_1349812993706917952.json
        //      （`m_Name = "FX Shine UI Avatar Menu"`）
        var mv = m.HasProperty("_Shine_Movement") ? m.GetVector("_Shine_Movement") : new Vector4(-9, -9, -9, -9);
        CheckNear("`_Shine_Movement`.`y` = 出厂值 0.9994769096374512（扫光停在屏幕外）", mv.y, 0.9994769096374512f, 1e-5f);
        CheckNear("`_Shine_Movement`.`x` = 0", mv.x, 0f, 1e-6f);

        var sc = m.HasProperty("_Shine_Scale") ? m.GetVector("_Shine_Scale") : new Vector4(-9, -9, -9, -9);
        CheckNear("`_Shine_Scale`.`x` = 0（原版材质实读）", sc.x, 0f, 1e-6f);
        CheckNear("`_Shine_Scale`.`y` = 20（原版材质实读）", sc.y, 20f, 1e-6f);

        var ms = m.HasProperty("_Mask_Size") ? m.GetVector("_Mask_Size") : new Vector4(-9, -9, -9, -9);
        CheckNear("`_Mask_Size`.`x` = 1.26", ms.x, 1.2599999904632568f, 1e-5f);
        CheckNear("`_Mask_Size`.`y` = 0.56", ms.y, 0.5600000023841858f, 1e-5f);

        var ng = m.HasProperty("_NormalGenerator_Offset_Strenght")
               ? m.GetVector("_NormalGenerator_Offset_Strenght") : new Vector4(-9, -9, -9, -9);
        CheckNear("`_NormalGenerator_Offset_Strenght`.`x` = 0.5", ng.x, 0.5f, 1e-6f);
        CheckNear("`_NormalGenerator_Offset_Strenght`.`y` = 0.36（⚠️ 不是 shader 默认的 1）",
                  ng.y, 0.36000001430511475f, 1e-5f);

        CheckNear("`" + UvRotProp + "`（Uv Rotation）= 1.04",
                  m.HasProperty(UvRotProp) ? m.GetFloat(UvRotProp) : -9f, 1.0399999618530273f, 1e-5f);

        var col = m.HasProperty(ShineColProp) ? m.GetColor(ShineColProp) : new Color(-9, -9, -9, -9);
        CheckNear("`" + ShineColProp + "`.`r` = 1.1953189373016357（HDR 扫光色）", col.r, 1.1953189373016357f, 1e-5f);
        CheckNear("`" + ShineColProp + "`.`g` = 0.5485013723373413", col.g, 0.5485013723373413f, 1e-5f);
        CheckNear("`" + ShineColProp + "`.`b` = 0.1190144494175911", col.b, 0.1190144494175911f, 1e-5f);
        CheckNear("`" + ShineColProp + "`.`a` = 0", col.a, 0f, 1e-6f);

        // 关键字：原版 `m_ValidKeywords = [_USESCREENSPACE, _USETILEMASK]` ⇒ 走 b2 + b8 那一档
        Check("`" + MatName + "` 开着 `_USESCREENSPACE`（否则原版走 catch-all b7，屏幕空间与 tile mask 整段没有）",
              m.IsKeywordEnabled("_USESCREENSPACE"));
        Check("`" + MatName + "` 开着 `_USETILEMASK`（同上）", m.IsKeywordEnabled("_USETILEMASK"));

        // 🔴 死值：原版那份 JSON 里还躺着 `_ShineScale = (1.39, 1, 1.42, 1.21)` ——
        //    本 shader 的属性表里**没有**这个名字（只有带下划线的 `_Shine_Scale`）⇒ 那是**上一颗 shader 的残留**。
        Check("`" + MatName + "` ⛔ 没有死值 `_ShineScale`（原版 JSON 里的残留，属性表里没有这名字）",
              !m.HasProperty("_ShineScale"));
        // 三张 `unity_Lightmaps*` / `unity_ShadowMasks` 是 ShaderGraph 给 URP 光照加的占位（空图、算式里不采）
        Check("`" + MatName + "` ⛔ 没有 `unity_Lightmaps`（原版那是空占位，算式里一个都没采）",
              !m.HasProperty("unity_Lightmaps"));

        // 我们的 tint 载体（原版这一项来自顶点色；`ImageQuad.SetTint` 写的就是 `_Color`）
        Check("`" + MatName + "` 保留了 `_Color`（`ImageQuad.SetTint` 写它；原版这一项来自顶点色）",
              m.HasProperty("_Color"));
        // `ImageQuad.SetMaterial` 会 `material.mainTexture = _tex` ⇒ 必须留 `_MainTex`
        Check("`" + MatName + "` 保留了 `_MainTex`（`ImageQuad.SetMaterial` 写的就是 `material.mainTexture`）",
              m.HasProperty("_MainTex"));
    }

    // ------------------------------------------------------------------ ② shader 源

    static void CheckShaderSource()
    {
        Debug.Log("[B110_S1] ---- ② shader 源 ----");

        var raw = Text("Resources/Shaders/" + ShaderFile);
        Check("`" + ShaderFile + "` 文件在", raw != null);
        if (raw == null) return;
        var src = StripLineComments(raw);

        Check("`" + ShaderFile + "` 声明的 shader 名 = `" + ShaderName + "`",
              src.Contains("Shader \"" + ShaderName + "\""));

        // 原版两个 Pass 的渲染状态（`Everguild_FX_FX Shine For Animation Normal.shader:28-31`）
        Check("`" + ShaderFile + "` 的【代码行】里 `Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha` 在",
              HasCodeLine(src, "Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha"));
        Check("`" + ShaderFile + "` 的【代码行】里 `ZWrite Off` 在", HasCodeLine(src, "ZWrite Off"));
        Check("`" + ShaderFile + "` 的【代码行】里 `Cull Off` 在", HasCodeLine(src, "Cull Off"));
        Check("`" + ShaderFile + "` 的【代码行】里 `ZClip On` 在", HasCodeLine(src, "ZClip On"));

        // 骨架先例（URP 17.3.0）：CGPROGRAM + **不写 LightMode** ⇒ 走 SRPDefaultUnlit
        Check("`" + ShaderFile + "` ⛔ 没写 `LightMode`（写了就走不到 SRPDefaultUnlit，与 WFUIPulsating 先例冲突）",
              !src.Contains("LightMode"));

        // b8 档的判定特征（catch-all b7 里**没有**这三样 ⇒ 这三条就是「档位没跑偏」的闸）
        Check("`" + ShaderFile + "` 有屏幕坐标（`_ScreenParams` + `i.pos`）—— b8 档特征，b7 没有",
              src.Contains("_ScreenParams") && src.Contains("i.pos"));
        Check("`" + ShaderFile + "` 有 `_ProjectionParams.x < 0.0` 那条镜像补偿（原版 `:74`，⛔ 别按 D3D 直觉删）",
              src.Contains("_ProjectionParams.x < 0.0"));
        Check("`" + ShaderFile + "` 有 tile mask 的 `ddx/ddy` 求导（原版 `:105-106`，b7 没有这一段）",
              src.Contains("ddx(") && src.Contains("ddy("));

        // sRGB 编码三常量（原版 `:118-123`）
        Check("`" + ShaderFile + "` 有 sRGB 编码的 1/2.4（0.4166666567325592）", src.Contains("0.4166666567325592041015625"));
        Check("`" + ShaderFile + "` 有 sRGB 编码的 12.923210144042969（低段系数）", src.Contains("12.92321014404296875"));
        Check("`" + ShaderFile + "` 有 sRGB 编码的 1.055 / -0.055",
              src.Contains("1.05499994754791259765625") && src.Contains("0.054999999701976776123046875"));

        // 底图早退（原版 `:68-71`）
        Check("`" + ShaderFile + "` 有 `if (baseA == 0.0) discard;`（原版 `:68-71`）", src.Contains("if (baseA == 0.0) discard"));

        // 属性名必须逐字可对上原版材质 JSON 的字段名（否则材质 YAML 逐字段对照就断了）
        Check("`" + ShaderFile + "` 声明了原版那三个 GUID 派生属性名",
              src.Contains(ShineTexProp) && src.Contains(ShineColProp) && src.Contains(UvRotProp));
    }

    // ------------------------------------------------------------------ ③ 贴图

    static void CheckTexture()
    {
        Debug.Log("[B110_S1] ---- ③ 贴图 ----");

        var path = TexDir + TexFile;
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Check("扫光贴图在：`" + path + "`", tex != null);
        if (tex == null) return;
        Check("扫光贴图尺寸 = 128×128（原版 `Texture2D` 实读）", tex.width == 128 && tex.height == 128);

        var m = AssetDatabase.LoadAssetAtPath<Material>(MatDir + MatName + ".mat");
        if (m != null)
        {
            var bound = m.HasProperty(ShineTexProp) ? m.GetTexture(ShineTexProp) : null;
            Check("材质**真的引用**了这张贴图（不是「导进来了没人用」）", bound == tex);
        }
    }

    // ------------------------------------------------------------------ ④ C# 镜像 / 灭自证

    /// <summary>原版 `:118-123` 的 sRGB 编码（逐分量）。</summary>
    static float SrgbEncode(float c)
    {
        return c <= 0.0031308f ? c * 12.923210144042969f
                               : Mathf.Pow(Mathf.Abs(c), 1f / 2.4f) * 1.055f - 0.055f;
    }

    static void CheckMirror()
    {
        Debug.Log("[B110_S1] ---- ④ C# 镜像 / 灭自证 ----");

        // 四个常量（全部 `.rdata` 直读，见 `Shell/AvatarTab.cs` 的 `DoShine` 注释）
        CheckNear("`ShineDuration` = 4.0（`0x1834b2df0`）", CardPresentation.AvatarTab.ShineDuration, 4f, 1e-6f);
        CheckNear("`ShineStartY` = 0.2（`0x1834b32b4`）", CardPresentation.AvatarTab.ShineStartY, 0.2f, 1e-6f);
        CheckNear("`ShineEndY` = 1.0（`0x1834b2f14`）", CardPresentation.AvatarTab.ShineEndY, 1f, 1e-6f);
        CheckNear("`PunchScale` = 0.1（`0x1834b2dc4`）", CardPresentation.AvatarTab.PunchScale, 0.1f, 1e-6f);
        CheckNear("`PunchDuration` = 0.3（`0x1834b2dc8`）", CardPresentation.AvatarTab.PunchDuration, 0.3f, 1e-6f);
        CheckNear("`PunchElasticity` = 1.0（`0x1834b2bb8`）", CardPresentation.AvatarTab.PunchElasticity, 1f, 1e-6f);
        Check("`PunchVibrato` = 0（调用点第 4 实参硬编码 0）", CardPresentation.AvatarTab.PunchVibrato == 0);

        // 语义本体：扫光**沿 y** 从 0.2 扫到 1.0（换轴 = 另一条效果；终点必须比起点远）
        Check("扫光沿 y 移动（终点 > 起点）",
              CardPresentation.AvatarTab.ShineEndY > CardPresentation.AvatarTab.ShineStartY);
        Check("出厂停在屏幕外（`ShineRestY` 距终点 < 1e-3 —— ⚠️ 原版初值 0.9994769 vs 终点 1.0，差 5.23e-4 **是原版值本身**，⛔ 别拿 1e-6 判它）",
              CardPresentation.AvatarTab.ShineRestY >= CardPresentation.AvatarTab.ShineEndY - 1e-3f);

        // sRGB 编码：HDR 分量编码后**仍然 > 1**（丢掉编码会把它压到 ≤1 —— 静默变暗）
        float encHigh = SrgbEncode(1.1953189373016357f);
        Check("sRGB 编码后 HDR 分量仍 > 1（实测 " + encHigh.ToString("R") + "）", encHigh > 1f);
        Check("不编码与编码差 > 0.1（挡「把编码这一步删掉」）",
              Mathf.Abs(encHigh - 1.1953189373016357f) > 0.1f);

        // 低段（<= 0.0031308）走线性段：c*12.9232
        CheckNear("`SrgbEncode(0.001)` = 0.0129232（低段线性）", SrgbEncode(0.001f), 0.012923210144042969f, 1e-7f);
        // 中段：0.5^(1/2.4)*1.055-0.055 ≈ 0.7354
        CheckNear("`SrgbEncode(0.5)` ≈ 0.7354", SrgbEncode(0.5f), 0.73535698f, 1e-4f);

        // 材质出厂值必须与 C# 里那个「停在屏幕外」的常量一致（两处写死同一个数 ⇒ 不许各写一份）
        CheckNear("材质出厂 `_Shine_Movement.y` == `AvatarTab.ShineRestY`",
                  CardPresentation.AvatarTab.ShineRestY, 0.9994769096374512f, 1e-6f);
    }

    // ------------------------------------------------------------------ ⑤ 接线

    static void CheckWiring()
    {
        Debug.Log("[B110_S1] ---- ⑤ 接线 ----");

        var tab = Text("Shell/AvatarTab.cs");
        Check("`Shell/AvatarTab.cs` 文件在", tab != null);
        if (tab != null)
        {
            Check("`OnSelectClicked` 里先 `DoShine(ShineDuration)` 再 `ConfirmSelect()`（= 原版 `OnChoseAvatar` 的顺序）",
                  tab.Contains("DoShine(ShineDuration); ConfirmSelect();"));
            Check("`DoShine` 的调用**不在行末注释里**（行内第一个 `//` 必须在它之后）",
                  CallBeforeLineComment(tab, "DoShine(ShineDuration); ConfirmSelect()"));

            Check("`DoShine` 方法本体在（原版 `AvatarDisplay.DoShine`）", tab.Contains("public void DoShine(float time)"));
            Check("材质走 `Resources.Load<Material>(\"Art/vfx/WFUI_FxShine\")`（路径与资产一致）",
                  tab.Contains("\"Art/vfx/WFUI_FxShine\""));
            Check("`_Shine_Movement` 是**按名字**写的（`Shader.PropertyToID`）",
                  tab.Contains("Shader.PropertyToID(\"_Shine_Movement\")"));
            Check("扫光补间**只在 y 上**（x/z/w 分量写死 0：`new Vector4(0f, ShineStartY, 0f, 0f)` → `(0f, ShineEndY, 0f, 0f)`）",
                  tab.Contains("new Vector4(0f, ShineStartY, 0f, 0f)")
                  && tab.Contains("new Vector4(0f, ShineEndY, 0f, 0f)"));

            // 🔴 `ImageQuad.SetMaterial` 不搬材质色 ⇒ 必须自己存/恢复 tint（本仓踩过两次）
            Check("换材质前后**存/恢复了 tint**（`SetMaterial` 不搬 `_Color`）",
                  tab.Contains("var keep = _bigArt.Tint;") && tab.Contains("_bigArt.SetTint(keep);"));
            Check("tint 恢复那句**不在行末注释里**", CallBeforeLineComment(tab, "_bigArt.SetTint(keep);"));

            // punch 目标是**边框图**（原版 `avatarHolder`），⛔ 不是头像图
            Check("punch 打在 `_bigBorder`（原版 `avatarHolder`）而不是 `_bigArt`",
                  tab.Contains("DG.Tweening.DOTween.Kill(_bigBorder);"));

            // 账落点保护：本文件一行 `using` 都不能加（加了会挪 `:601/:613/:629/...` 那批账落点）
            Check("`Shell/AvatarTab.cs` ⛔ 没有新增 `using DG.Tweening;`（加了会挪下游账落点行号）",
                  !tab.Contains("\nusing DG.Tweening;"));
        }
    }
}
