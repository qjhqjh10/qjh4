// B110_M1_MenuMat_Assert.cs — 菜单 13 材质批 **#1 Division Change / #2 Greyscale Add** 的自检
//
// ============================ 判据 ============================
//  `资料/完全复刻/判据/B109_R15_菜单13材质.md` §1·1 / §1·2（逐条）；
//  源码真本 = `d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/`
//             `Everguild_UI_Division Change.shader:115-186` ·
//             `Everguild_UI_Greyscale Add/Sub0_Pass0_Fragment_b7.hlsl:39-61`
//  材质真本 = `assets_full/bundle_menus_assets_all/Material/Material_-7300284622968135969.json`（`Rank Change`）·
//             `Material_-4471755909380014346.json`（`UI Greyscale Add`）
//  本批产出 = `Resources/Shaders/WFUI{DivisionChange,GreyscaleAdd}.shader` ·
//             `Resources/Art/vfx/WFUI_{DivisionChange,GreyscaleAdd}.mat` ·
//             调用点 `Shell/RankedDivisionChangeWindow.cs`（`Division Image` + `Rank Icon`）· `Shell/TitleTab.cs`（`Image Top`）
//
// ============================ 用法 ============================
//   宿主接线（调度台）：
//     unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
//       -executeMethod B110_M1_MenuMat_Assert.Run -logFile -
//   ⚠️ 判绿红看「B110M1 合计」，不看退出码。
//   从宿主里跑必须传 `false`（否则会把宿主提前结束）。
//
// ============================ 这几条断言各自挡什么 ============================
//  · 材质「有 `_Blend` 属性」—— 挡 B109_R15 §1 #1 那个原始缺陷：AMP 每帧写 `_Blend`、材质上却没这个属性
//    ⇒ `Material.SetFloat` 静默 no-op，屏幕上「什么都没发生」而且**零报错**。
//  · 混合模式**数值**（`_SrcBlend == 4` / `_DstBlend == 1`）—— **灭自证**：改回普通 alpha 混合
//    （5 / 10）就立刻判红。#2 的全部观感差异就在这一对数字上。
//  · 四维半径 `R(圆心) = √0.5 = 0.7071`（**不是 0**）—— **灭自证**：照「二维半径」写会得 0 就判红。
//    这一条同时证明 `_Blend 0→1` 的扫过范围能覆盖整张图（见 ④）。
//  · 算式常量在 **shader 源码文本**里逐个在位 —— 挡「把算式换成观感近似」。
//  · 调用点在 **C# 源码**里、且落在非注释段 —— 挡「窗口退回 `Sprites/Default`」。
//    （⚠️ 这条是文本级：先剥 `//` 行注释再数，定义行 `void X(ImageQuad quad)` 不会被 `X(quad)` 匹配到。）
//
// ⚠️ 本文件**不渲图**（edit 模式下没有可比的相机帧）。「两帧像素不同」那一步要真机/Play，
//    见 `资料/完全复刻/判据/B109_R15_菜单13材质.md` §3·4。

using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using CardPresentation;

public static class B110_M1_MenuMat_Assert
{
    static int _pass, _fail;

    static void Check(bool ok, string what)
    {
        if (ok) { _pass++; Debug.Log("B110M1 ✓ " + what); }
        else { _fail++; Debug.LogError("B110M1 ✗ " + what); }
    }

    static bool Near(float a, float b, float eps = 1e-5f) { return Mathf.Abs(a - b) <= eps; }

    static Material LoadMat(string res)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/CardPresentation/Resources/" + res + ".mat");
        if (m == null) m = Resources.Load<Material>(res);
        return m;
    }

    static string ReadText(string assetsRel)
    {
        string p = Path.Combine(Application.dataPath, assetsRel);
        return File.Exists(p) ? File.ReadAllText(p) : "";
    }

    /// <summary>剥掉 `//` 行注释（够用：本仓这几份文件没有跨行的块注释夹带代码）。</summary>
    static string StripLineComments(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new System.Text.StringBuilder(s.Length);
        int i = 0;
        bool inLine = false;
        while (i < s.Length)
        {
            char c = s[i];
            if (c == '\n') { inLine = false; sb.Append(c); i++; continue; }
            if (!inLine && c == '/' && i + 1 < s.Length && s[i + 1] == '/') { inLine = true; i += 2; continue; }
            if (!inLine) sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    static int CountOccurrences(string hay, string needle)
    {
        if (string.IsNullOrEmpty(hay) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, i = 0;
        while ((i = hay.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { n++; i += needle.Length; }
        return n;
    }

    // ============================================================ 算式镜像（C# 侧纯函数）
    //  ⚠️ 镜像只是把 shader 里的算式写成可断言的形状；两者的**连接处**由下面「源码常量在位」那一组补上。
    //  —— #1 `Everguild_UI_Division Change.shader`

    /// <summary>`_120 = _Blend*0.4 + 0.6`（`:120`）。</summary>
    static float DivW(float blend) { return blend * 0.39999997615814208984375f + 0.60000002384185791015625f; }

    /// <summary>`_141 = sqrt(dot4(p,p))` where `p = float4(uv,0,0) + amt*n - center`（`:121-126`）。
    /// 🔴 **四维**：`INTERP.z = INTERP.w = 0`（uv0 是 Vector2），而 `_DistortCenter.z/.w = 0.5`
    /// ⇒ 圆心处 r = √0.5，不是 0。</summary>
    static float DivRadius(float u, float v, float n, Vector4 center, float amt)
    {
        float dx = u + amt * n - center.x;
        float dy = v + amt * n - center.y;
        float dz = 0f + amt * n - center.z;
        float dw = 0f + amt * n - center.w;
        return Mathf.Sqrt(dx * dx + dy * dy + dz * dz + dw * dw);
    }

    /// <summary>`_148 = 1 - saturate((r - w) * 11.1111145)`（`:127-129`）。</summary>
    static float DivK(float r, float w) { return 1f - Mathf.Clamp01((r - w) * 11.111114501953125f); }

    /// <summary>`_280 = 1 - saturate((r - (w - _ColorBorderWidth)) * 11.1111145)`（`:171-174`）。</summary>
    static float DivBand(float r, float w, float colorBorderWidth)
    {
        return 1f - Mathf.Clamp01((r - (w - colorBorderWidth)) * 11.111114501953125f);
    }

    // —— #2 `Everguild_UI_Greyscale Add`

    /// <summary>`_104 = dot(rgb, (0.30, 0.59, 0.11))`（`Fragment_b7.hlsl:52`）。</summary>
    static float Grey(float r, float g, float b)
    {
        return r * 0.300000011920928955078125f
             + g * 0.589999973773956298828125f
             + b * 0.10999999940395355224609375f;
    }

    /// <summary>`out.rgb = _102 * lerp(rgb, grey, _GreyScale)` · `out.a = _102`，`_102 = tex.a * round(a*255)/255`
    /// （`Fragment_b7.hlsl:46-60`）。</summary>
    static Color GreyOut(Color tex, Color tint, float greyScale)
    {
        float a8 = Mathf.Round(tint.a * 255f) / 255f;
        float ca = tex.a * a8;
        float r = tex.r * tint.r, g = tex.g * tint.g, b = tex.b * tint.b;
        float grey = Grey(r, g, b);
        return new Color(ca * Mathf.Lerp(r, grey, greyScale),
                         ca * Mathf.Lerp(g, grey, greyScale),
                         ca * Mathf.Lerp(b, grey, greyScale),
                         ca);
    }

    // ============================================================ 入口
    public static void Run(bool quitOnBatch = true)
    {
        _pass = _fail = 0;

        // =====================================================================================
        //  #1 Division Change
        // =====================================================================================
        var matD = LoadMat("Art/vfx/WFUI_DivisionChange");
        Check(matD != null, "① `Resources/Art/vfx/WFUI_DivisionChange.mat` 加载得到（原版 `Rank Change` 的等价物）");
        string shD = matD == null || matD.shader == null ? "" : matD.shader.name;
        Check(shD == "WarpforgeVFX/UI/UI Division Change",
              $"① shader = `WarpforgeVFX/UI/UI Division Change`（实得 `{shD}`；`Sprites/Default` ⇒ 本条红 = 调用点被改回旧写法）");

        // 材质必须有 `_Blend` —— 挡「AMP 每帧写、材质没这个属性 ⇒ 静默 no-op」
        Check(matD != null && matD.HasProperty("_Blend"),
              "② 材质有 `_Blend` 属性（没有它 ⇒ `AnimateUIMaterialProperty` 的 `SetFloat` **静默无效**，屏幕上什么都不动）");
        for (int i = 0; i < DivParams.Length; i++)
            Check(matD != null && matD.HasProperty(DivParams[i]),
                  $"② 材质有 `{DivParams[i]}` 属性（shader Properties 块里声明了）");

        // 浮点参数逐字段 = 原版
        Check(matD != null && Near(matD.GetFloat("_Blend"), 1f),
              $"③ `_Blend` = 原版 1.0（实得 {(matD != null ? matD.GetFloat("_Blend") : -1f):0.######}）—— 出厂满档 = 整图 `_MainTex`");
        Check(matD != null && Near(matD.GetFloat("_BorderWidth"), 0.36f, 1e-6f),
              $"③ `_BorderWidth` = 原版 0.36（实得 {(matD != null ? matD.GetFloat("_BorderWidth") : -1f):0.######}）");
        Check(matD != null && Near(matD.GetFloat("_ColorBorderWidth"), -0.01f, 1e-6f),
              $"③ `_ColorBorderWidth` = 原版 −0.01（实得 {(matD != null ? matD.GetFloat("_ColorBorderWidth") : 1f):0.######}）");
        Check(matD != null && Near(matD.GetFloat("_DissolveAmount"), 0.084f, 1e-6f),
              $"③ `_DissolveAmount` = 原版 0.084（实得 {(matD != null ? matD.GetFloat("_DissolveAmount") : -1f):0.######}）");
        Check(matD != null && Near(matD.GetFloat("_DistortAmount"), 0.02f, 1e-6f),
              $"③ `_DistortAmount` = 原版 0.02（实得 {(matD != null ? matD.GetFloat("_DistortAmount") : -1f):0.######}）");

        // 颜色 / 向量逐字段
        Check(matD != null && Near(matD.GetColor("_DissolveColor").r, 1.515716552734375f, 1e-5f)
                          && Near(matD.GetColor("_DissolveColor").g, 0.232688307762146f, 1e-5f)
                          && Near(matD.GetColor("_DissolveColor").b, 0f, 1e-6f),
              "④ `_DissolveColor` = 原版 (1.51572, 0.232688, 0, 1)");
        Check(matD != null && Near(matD.GetColor("_DissolveColorSecondary").r, 0.9245283007621765f, 1e-5f)
                          && Near(matD.GetColor("_DissolveColorSecondary").g, 0f, 1e-6f),
              "④ `_DissolveColorSecondary` = 原版 (0.924528, 0, 0, 1)");
        Check(matD != null && Near(matD.GetColor("_DistortCenter").r, 0.5f) && Near(matD.GetColor("_DistortCenter").g, 0.59f, 1e-6f)
                          && Near(matD.GetColor("_DistortCenter").b, 0.5f) && Near(matD.GetColor("_DistortCenter").a, 0.5f),
              "④ `_DistortCenter` = 原版 (0.5, 0.59, 0.5, 0.5) —— z/w 就是 ④ 里「圆心半径 = √0.5」的来源");

        // 噪声贴图 = `Noise Combined`（pathID −8214795764591836323）+ `_Noise_ST.scale = (3,3)`
        var noise = matD == null ? null : matD.GetTexture("_Noise");
        string noiseName = noise == null ? "null" : noise.name;
        Check(noise != null && noiseName.Replace("_", " ").Contains("Noise Combined"),
              $"⑤ `_Noise` = `Noise Combined`（原版 pathID −8214795764591836323；实得 `{noiseName}`）");
        Check(matD != null && Near(matD.GetTextureScale("_Noise").x, 3f) && Near(matD.GetTextureScale("_Noise").y, 3f),
              "⑤ `_Noise_ST.scale` = (3,3)（原版材质 `m_Scale`；缺了噪声就变成整屏一个常数 = 火线变正圆）");

        // ---- 算式（C# 镜像）----
        var ctr = new Vector4(0.5f, 0.59f, 0.5f, 0.5f);
        float rAtCenter = DivRadius(0.5f, 0.59f, 0f, ctr, 0.02f);
        Check(Near(rAtCenter, 0.70710678f, 1e-5f),
              $"⑥ 圆心处半径 `r = √0.5 = 0.707107`（实得 {rAtCenter:0.######}）—— **四维**半径；"
            + "照二维写会得 0（= 灭自证：半径口径被改回二维就红）");
        float rCorner = DivRadius(1f, 1f, 0f, ctr, 0f);
        Check(Near(rCorner, 0.95817f, 1e-3f),
              $"⑥ 角点 (1,1) 的半径 = {rCorner:0.######}（= √(0.25 + 0.41² + 0.5) ≈ 0.95817，画面的取值上限）");
        // 扫过全程：`_Blend` 0 时 w < 所有 r ⇒ 整屏 Secondary；1 时 w > 最大 r ⇒ 整屏 Main
        Check(DivW(0f) < rAtCenter && DivW(1f) > rCorner,
              $"⑥ 扫过全程：W(0) = {DivW(0f):0.####} < r_min {rAtCenter:0.####} 且 W(1) = {DivW(1f):0.####} > r_max {rCorner:0.####}"
            + " ⇒ 曲线 0→1.0486 能把火线从「整屏次级图」推到「整屏主图」（幅度不够就是动画没走完）");

        Check(Near(DivK(DivW(0.5f), DivW(0.5f)), 1f) && Near(DivK(DivW(0.5f) + 1f / 11.1111145f, DivW(0.5f)), 0f, 1e-5f),
              "⑦ 火线软边：`r = w` 处 k = 1，`r = w + 1/11.1111` 处 k = 0 —— 带宽 ≈ 0.09（画面单位）");
        float kB = DivK(DivW(0.5f) + 0.045f, DivW(0.5f));
        Check(kB > 0.05f && kB < 0.95f,
              $"⑦ 软边中点 k = {kB:0.####}（夹在 (0,1) 之间；=0 或 1 都说明 ramp 常数被改坏，画面上会是一圈硬边）");

        Check(Near(DivBand(DivW(1f), DivW(1f), -0.01f), 1f) && Near(DivBand(DivW(1f) + 1f, DivW(1f), -0.01f), 0f, 1e-5f),
              "⑧ 描边色带 `1 - saturate((r - (w - _ColorBorderWidth))*11.1111)`：贴内侧 band = 1（取 `_DissolveColor`）、"
            + "外侧 band = 0（取 `_DissolveColorSecondary`）—— **方向别反**（B109_R15 §1·1 那句 `lerp` 写反了，本实现按 `.shader:175-177` 走）");

        // ---- shader 源码常量在位（挡「换成观感近似」）----
        string srcD = ReadText("CardPresentation/Resources/Shaders/WFUIDivisionChange.shader");
        Check(srcD.Contains("11.111114501953125"),
              "⑨ shader 源码含 ramp 常数 `11.111114501953125`（换掉它 = 软边宽度不是原版）");
        Check(srcD.Contains("0.39999997615814208984375") && srcD.Contains("0.60000002384185791015625"),
              "⑨ shader 源码含半径式 `_Blend*0.39999998 + 0.60000002`（原版 `mad(_Blend, 0.4, 0.6)`）");
        Check(srcD.Contains("12.92321014404296875") && srcD.Contains("0.4166666567325592041"),
              "⑨ shader 源码含 linear→gamma 三件套（12.9232 / 1/2.4）—— 两枚溶解色是**线性**值，不做这步颜色会暗一档");
        Check(srcD.Contains("float4(i.uv, 0.0, 0.0)"),
              "⑨ shader 源码把 uv 扩成**四维**再算半径（灭自证：改回 `float2`/二维 length 就红）");
        Check(srcD.Contains("discard"),
              "⑨ shader 源码有 `discard`（原版 `if (_185 == 0.0f) discard;` —— 全透明像素不写深度、不留边）");
        Check(srcD.Contains("Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha"),
              "⑨ 混合模式 = 原版 `Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha`");
        Check(!StripLineComments(srcD).Contains("_Time"),
              "⑨ shader **不含 `_Time`** —— 原版这一条是静态 shader、由 legacy clip 推 `_Blend`（加了自动动画 = 多出一层原版没有的动）");

        // ---- 调用点在位（灭自证：窗口退回 `Sprites/Default` 就红）----
        string csDiv = StripLineComments(ReadText("CardPresentation/Shell/RankedDivisionChangeWindow.cs"));
        Check(CountOccurrences(csDiv, "ApplyDivisionChangeMaterial(quad)") == 2,
              $"⑩ 调用点：`BuildDivisionImage` + `BuildRankIcon` **各一处** `ApplyDivisionChangeMaterial(quad)`"
            + $"（实得 {CountOccurrences(csDiv, "ApplyDivisionChangeMaterial(quad)")} 处；0 处 = 退回 `Sprites/Default`）");
        Check(CountOccurrences(csDiv, "SyncDivisionSecondary(quad)") == 1,
              "⑩ 调用点：`FeedRankIcon` 里把 `_SecondaryTex` 跟主图同步（🔶 自己设计：原版两张图由服务端各灌一张）");

        // =====================================================================================
        //  #2 Greyscale Add
        // =====================================================================================
        var matG = LoadMat("Art/vfx/WFUI_GreyscaleAdd");
        Check(matG != null, "⑪ `Resources/Art/vfx/WFUI_GreyscaleAdd.mat` 加载得到（原版 `UI Greyscale Add` 的等价物）");
        string shG = matG == null || matG.shader == null ? "" : matG.shader.name;
        Check(shG == "WarpforgeVFX/UI/UI Greyscale Add",
              $"⑪ shader = `WarpforgeVFX/UI/UI Greyscale Add`（实得 `{shG}`）");

        Check(matG != null && matG.HasProperty("_GreyScale") && Near(matG.GetFloat("_GreyScale"), 1f),
              $"⑫ `_GreyScale` = 原版 1.0 常驻（实得 {(matG != null && matG.HasProperty("_GreyScale") ? matG.GetFloat("_GreyScale") : -1f):0.####}）"
            + " —— 全树没有任何代码写它（B109_R15 §4·6）");
        Check(matG != null && matG.HasProperty("_SrcBlend") && Near(matG.GetFloat("_SrcBlend"), 4f)
                          && matG.HasProperty("_DstBlend") && Near(matG.GetFloat("_DstBlend"), 1f),
              $"⑬ 混合 = `OneMinusDstColor(4) One(1)`（实得 SrC={(matG != null && matG.HasProperty("_SrcBlend") ? matG.GetFloat("_SrcBlend") : -1f):0} · "
            + $"Dst={(matG != null && matG.HasProperty("_DstBlend") ? matG.GetFloat("_DstBlend") : -1f):0}）"
            + " —— **灭自证**：改回普通 alpha（5 / 10）就红；这条是本条的全部观感差异");
        Check(matG != null && Near(matG.GetColor("_Color").r, 1f) && Near(matG.GetColor("_Color").a, 1f),
              "⑬ `_Color` = 白（原版 `(1,1,1,1)`）");

        // ---- 算式（C# 镜像）----
        Check(Near(Grey(1f, 0f, 0f), 0.30000001f, 1e-6f)
           && Near(Grey(0f, 1f, 0f), 0.58999997f, 1e-6f)
           && Near(Grey(0f, 0f, 1f), 0.10999999f, 1e-6f),
              "⑭ 亮度权重 = (0.30, 0.59, 0.11) —— **原版那三个数**（用的是 0.299/0.587/0.114 就红）");
        var gOut = GreyOut(new Color(1f, 0f, 0f, 1f), new Color(1f, 1f, 1f, 1f), 1f);
        Check(Near(gOut.r, gOut.g) && Near(gOut.g, gOut.b) && Near(gOut.r, 0.30000001f, 1e-6f),
              $"⑭ `_GreyScale = 1` ⇒ 输出三通道**相等**且 = 亮度（实得 {gOut.r:0.#####}, {gOut.g:0.#####}, {gOut.b:0.#####}）");
        var gOff = GreyOut(new Color(1f, 0f, 0f, 1f), new Color(1f, 1f, 1f, 1f), 0f);
        Check(Near(gOff.r, 1f) && Near(gOff.g, 0f),
              "⑭ `_GreyScale = 0` ⇒ 原色直通（实得 " + $"{gOff.r:0.####}, {gOff.g:0.####}）—— 证明 `lerp` 两端的方向没反");
        var gA = GreyOut(new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 0.5f), 1f);
        Check(Near(gA.a, 128f / 255f, 1e-6f),
              $"⑮ 顶点 α 先量化到 8 位：`round(0.5*255)/255 = 128/255`（实得 {gA.a:0.#####}）—— 原版 `_90` 那一句");
        Check(Near(gA.r, 128f / 255f, 1e-6f),
              $"⑮ 输出 RGB 也乘这同一个 a（`_102 = tex.a * _90`，实得 {gA.r:0.#####}）—— 原版不是「只乘 tex.a」");

        // ---- shader 源码常量在位 ----
        string srcG = ReadText("CardPresentation/Resources/Shaders/WFUIGreyscaleAdd.shader");
        Check(srcG.Contains("0.300000011920928955078125") && srcG.Contains("0.589999973773956298828125")
           && srcG.Contains("0.10999999940395355224609375"),
              "⑯ shader 源码含 (0.30, 0.59, 0.11) 三个原版常量（换成 0.299/0.587/0.114 就红）");
        Check(srcG.Contains("round(i.color.a * 255.0)") && srcG.Contains("0.0039215688593685626983642578125"),
              "⑯ shader 源码含 `round(a*255) * (1/255)` —— 原版 `_90` 那一跳");
        Check(srcG.Contains("Blend [_SrcBlend] [_DstBlend]"),
              "⑯ shader 的混合是 `Blend [_SrcBlend] [_DstBlend]`（数值由上一条 ⑬ 核）");
        Check(!StripLineComments(srcG).Contains("_Time"),
              "⑯ shader **不含 `_Time`** —— 原版这一条是静态的（DXBC 里 0 个 `_Time`）");

        // ---- 调用点在位 ----
        string csTitle = StripLineComments(ReadText("CardPresentation/Shell/TitleTab.cs"));
        Check(CountOccurrences(csTitle, "ApplyGreyscaleAddMaterial(facTop)") == 1,
              $"⑰ 调用点：`Shell/TitleTab.cs` 的 `Image Top` 那一层挂了这个材质（实得 "
            + $"{CountOccurrences(csTitle, "ApplyGreyscaleAddMaterial(facTop)")} 处；0 处 = 退回 `Sprites/Default`）");

        // ---- 混合语义（`OneMinusDstColor One` 到底是「滤色」还是「盖住」）----
        // 原版算式（`OneMinusDstColor One`）：`out = src*(1-dst) + dst*1`
        float dst = 0.2f, src = 0.5f;
        float screen = src * (1f - dst) + dst;
        float alpha = src * 1f + dst * (1f - 1f);   // 普通 SrcAlpha/OneMinusSrcAlpha 在 srcA=1 时 = src
        Check(Near(screen, 0.6f) && !Near(screen, alpha),
              $"⑱ 滤色语义：dst=0.2 / src=0.5 ⇒ 屏混 {screen:0.###}（不是普通 alpha 的 {alpha:0.###}）"
            + " —— 叠在亮底上**变亮**而不是盖住；这条把「Blend 改回 alpha」的后果钉成可判定的数字");

        Debug.Log($"B110M1 === 合计：{_pass} 通过 / {_fail} 失败 ===");
        if (quitOnBatch && Application.isBatchMode) Application.Quit(_fail == 0 ? 0 : 1);
    }

    static readonly string[] DivParams =
    {
        "_MainTex", "_SecondaryTex", "_Blend", "_Noise", "_DistortAmount",
        "_DissolveColor", "_DissolveColorSecondary", "_BorderWidth", "_DissolveAmount",
        "_DistortCenter", "_ColorBorderWidth", "_Color"
    };

    // 宿主聚合行读这两个口（追加到类尾、零行漂）。
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }
}
