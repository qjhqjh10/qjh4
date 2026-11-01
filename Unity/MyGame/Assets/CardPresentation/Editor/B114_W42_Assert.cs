// B114_W42_Assert.cs — `B-272`：`WFUIAdditive.shader` 补齐 b9 / b10 两个**软边 clip 变体**。
//   原版 `Everguild/UI/Additive` 有 **4 档**（`UNITY_UI_ALPHACLIP` × `UNITY_UI_CLIP_RECT`）：
//      b7 = 都没有（catch-all，我们原来就有）· b8 = 只有 ALPHACLIP（我们原来有那一句）
//      b9 = 只有 CLIP_RECT（软边 mask）· b10 = 两条都要（mask 之后再 discard）—— 这两档原来**完全没做**。
//
// 写手 `W-42` · 2026-11-01 · **没跑 Unity**（红线：`-executeMethod` 全禁），只写断言；宿主由调度台接线。
//
// ============================ 怎么接（⚠️ 只由调度台改宿主）============================
//  唯一入口 = `B114_W42_Assert.Run()`。挤进**某一条既有的聚合行**（同一物理行、**第一个 `//` 之前**）：
//      B114_W42_Assert.Run(); pass += B114_W42_Assert.Pass; fail += B114_W42_Assert.Fail;
//  ⚠️ `Editor/BattleScene.cs` 那族用局部变量 **`pass` / `fail`**；`ShellScene.cs` 那族用 **`_sink.Pass` / `_sink.Fail`**
//     —— 照那行的变量名写。**⛔ 先看该行第一个 `//` 在哪**（落在注释之后 = 一条都不跑，而 typecheck / 账闸 / 退出码全绿）。
//
// ============================ 判据 ============================
//  ① 原版资产：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/Shader/Shader_4921318991789134610.json`
//     `m_ParsedForm.m_Name = "Everguild/UI/Additive"`；`m_KeywordNames` 末两条 =
//     `UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP`，`m_KeywordFlags` 都是 **1 = local**。
//  ② 逐变体编译产物（AssetRipper 反编译）：
//     `d:/2/tools/ruri_out_1020_allshaders/ExportedProject/Assets/Shader/Everguild_UI_Additive/`
//       `Sub0_Pass0_Vertex_b1..b4.hlsl`（**四份逐字节相同** ⇒ 裁切插值器无条件算）
//       `Sub0_Pass0_Fragment_b7/b8/b9/b10.hlsl`
//  ③ 片元软边 mask（b9 `:34-40` / b10 `:45-51`）：
//       `w = rect.z-rect.x` · `h = rect.w-rect.y`
//       `mask = clamp((h-|t.y|)*t.w,0,1) * clamp((w-|t.x|)*t.z,0,1)`；`alpha *= mask`
//  ④ 顶点裁切插值器（b1 `:94-107`）：
//       `pixelSize = gl_Position.w / |P00*SP.x + P10*SP.y , P01*SP.x + P11*SP.y|`
//       `t.zw = 0.25 / (0.25*softness + |pixelSize|)`
//       `t.xy = positionOS.xy*2 - clamp(rect.xy,±2e10) - clamp(rect.zw,±2e10)`
//
// ============================ 本文件断什么（三层）============================
//   A · 运行时属性表（`Material.HasProperty`）—— 同时冒烟「shader 编得过」：
//       编译失败时 Unity 换成 error shader ⇒ HasProperty 恒 false ⇒ A1..A3 一起红。
//   B · 源码级（`typecheck` 不覆盖 shader、本波不许跑 Unity ⇒ 只能做文本级 + C# 镜像）：
//       软边公式 / 对象空间 / pragma **顺序**（ALPHACLIP 在前 —— Additive 与 BorderHighlight 相反）/ discard 位置。
//   C · **灭自证**（⛔ 别删）：
//       ① 改回硬边（`UnityGet2DClipping` 的 `step`×2）⇒ B12 红 + C2 红；
//       ② 改回世界空间（`unity_ObjectToWorld`）⇒ B13 红 + B4 红；
//       ③ 照抄 BorderHighlight 的 `1.0 / abs(...)` 或把 pragma 顺序反过来 ⇒ B7 / B3 红；
//       ④ 只算 mask 不乘进 alpha ⇒ B10 红；
//       ⑤ 把 mask 包成 0/1 突变（等同硬边）⇒ C2 红（C# 镜像在**过渡带内**取点，必须是 (0,1) 开区间）。
using System.IO;
using System.Text;
using UnityEngine;

public static class B114_W42_Assert
{
    const string P = "[B114_W42] ";
    const string ShaderPath = "CardPresentation/Resources/Shaders/WFUIAdditive.shader";
    const string ShaderName = "WarpforgeVFX/UI/Additive";

    static int _ok, _bad;

    /// <summary>给宿主聚合行用的读口（否则失败不会让宿主变红 = 静默失败）。</summary>
    public static int Pass { get { return _ok; } }
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(P + "✓ " + what); }
        else { _bad++; Debug.LogError(P + "✗ " + what); }
    }

    static bool Near(float a, float b, float tol = 1e-3f) { return Mathf.Abs(a - b) <= tol; }

    static string ReadSrc(string rel)
    {
        try
        {
            var p = Path.Combine(Application.dataPath, rel);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
        catch { return null; }
    }

    static int Count(string s, string sub)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }

    static int Idx(string s, string sub)
    {
        return (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) ? -1
             : s.IndexOf(sub, System.StringComparison.Ordinal);
    }

    /// <summary>
    /// 去掉 `//` 行注释与 `/* */` 块注释 —— **必须的**：文件头判据注释里逐字引用了
    /// `UnityGet2DClipping`（反例名），不剥就是**必然假红**。
    /// </summary>
    static string StripComments(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
                if (i < s.Length) sb.Append('\n');
            }
            else if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < s.Length && !(s[i] == '*' && s[i + 1] == '/')) i++;
                i++;
                sb.Append(' ');
            }
            else sb.Append(s[i]);
        }
        return sb.ToString();
    }

    // ================================================================ C# 镜像（原版算式，验的是「抄对了」）
    //  原版 b9/b10 的软边 mask；`rect` = (minX,minY,maxX,maxY)（对象空间，单位与 positionOS 相同）
    //  `ramp` = 插值器 .zw（= 1/过渡带绝对宽度），由顶点算好传下来。
    static float MirrorMask(float posOsX, float posOsY, Vector4 rect, float rampZ, float rampW)
    {
        float tx = posOsX * 2f - rect.x - rect.z;         // 原版 TEXCOORD_2.x
        float ty = posOsY * 2f - rect.y - rect.w;         // 原版 TEXCOORD_2.y
        float w = rect.z - rect.x;
        float h = rect.w - rect.y;
        float mW = Mathf.Clamp((w - Mathf.Abs(tx)) * rampZ, 0f, 1f);
        float mH = Mathf.Clamp((h - Mathf.Abs(ty)) * rampW, 0f, 1f);
        return mH * mW;
    }

    /// <summary>原版顶点：`0.25 / (0.25*softness + |pixelSize|)`。</summary>
    static float MirrorRamp(float softness, float pixelSize)
    {
        return 0.25f / (0.25f * softness + Mathf.Abs(pixelSize));
    }

    // 硬边（`UnityGet2DClipping` 的 step×2）—— 只作为**反例**出现在镜像断言里，⛔ 不进 shader
    static float MirrorHardMask(float posOsX, float posOsY, Vector4 rect)
    {
        return (posOsX >= rect.x && posOsX <= rect.z && posOsY >= rect.y && posOsY <= rect.w) ? 1f : 0f;
    }

    public static void Run()
    {
        _ok = _bad = 0;
        Material mat = null;
        try
        {
            // ============================================================ A · 属性表（运行时）
            var sh = Shader.Find(ShaderName);
            Check(sh != null, "A0 前提：`Shader.Find(\"" + ShaderName + "\")` 取得到（取不到 = 本波改动没被 Unity 认到）");
            if (sh != null)
            {
                mat = new Material(sh);
                // 🔴 这三条同时冒烟「shader 编得过」：编译失败 ⇒ error shader ⇒ HasProperty 恒 false。
                Check(mat.HasProperty("_ClipRect"),
                      "A1 `_ClipRect` 真的在属性表里（原版在 `type_Globals` c4、由 native 喂；我们没有 CanvasRenderer ⇒ 只能声明成材质属性）"
                      + "｜🧨 只在 CGPROGRAM 里写了 `float4 _ClipRect;` 却没进 `Properties` ⇒ 这条红");
                Check(mat.HasProperty("_UIMaskSoftnessX"), "A2 `_UIMaskSoftnessX` 真的在属性表里");
                Check(mat.HasProperty("_UIMaskSoftnessY"), "A3 `_UIMaskSoftnessY` 真的在属性表里");

                if (mat.HasProperty("_ClipRect"))
                {
                    var r = mat.GetVector("_ClipRect");
                    Check(Near(r.x, -32767f, 1f) && Near(r.y, -32767f, 1f)
                       && Near(r.z, 32767f, 1f) && Near(r.w, 32767f, 1f),
                          "A4 `_ClipRect` 默认 = uGUI「不裁」档 (-32767,-32767,32767,32767)（实际 "
                          + r.x + "," + r.y + "," + r.z + "," + r.w + "）｜🧨 是 0 = 关键字一打开就整块消失");
                }
                if (mat.HasProperty("_UIMaskSoftnessX"))
                    Check(Near(mat.GetFloat("_UIMaskSoftnessX"), 1f),
                          "A5 `_UIMaskSoftnessX` 默认 = 1.0（= ShaderGraph Canvas 目标那条属性的默认值；实际 "
                          + mat.GetFloat("_UIMaskSoftnessX") + "）");
                if (mat.HasProperty("_UIMaskSoftnessY"))
                    Check(Near(mat.GetFloat("_UIMaskSoftnessY"), 1f),
                          "A6 `_UIMaskSoftnessY` 默认 = 1.0（实际 " + mat.GetFloat("_UIMaskSoftnessY") + "）");

                // b7 骨架别被本波改坏：`_Color` / `_MainTex` / `_ColorMask` / `_UseUIAlphaClip` 都还在
                Check(mat.HasProperty("_MainTex") && mat.HasProperty("_Color") && mat.HasProperty("_ColorMask")
                   && mat.HasProperty("_UseUIAlphaClip") && mat.HasProperty("_TextureSampleAdd"),
                      "A7 b7 骨架的属性都还在（`_MainTex` / `_Color` / `_ColorMask` / `_UseUIAlphaClip` / `_TextureSampleAdd`）");
            }

            // ============================================================ B · 源码级
            var raw = ReadSrc(ShaderPath);
            Check(raw != null, "B0 前提：读得到 `" + ShaderPath + "`");
            if (raw != null)
            {
                var src = StripComments(raw);   // ⚠️ 注释里逐字引用了反例名，不剥必假红

                // ---- B1/B2/B3：两条 local 关键字 + **顺序**（Additive 是 ALPHACLIP 在前）----
                Check(Count(src, "#pragma multi_compile_local _ UNITY_UI_ALPHACLIP") == 1,
                      "B1 `#pragma multi_compile_local _ UNITY_UI_ALPHACLIP` 恰 1 条（原版 `m_KeywordFlags`=1=local，非 feature）");
                Check(Count(src, "#pragma multi_compile_local _ UNITY_UI_CLIP_RECT") == 1,
                      "B2 `#pragma multi_compile_local _ UNITY_UI_CLIP_RECT` 恰 1 条（b9/b10 的前提）");
                int iPragA = Idx(src, "#pragma multi_compile_local _ UNITY_UI_ALPHACLIP");
                int iPragC = Idx(src, "#pragma multi_compile_local _ UNITY_UI_CLIP_RECT");
                Check(iPragA >= 0 && iPragC > iPragA,
                      "B3 ★ pragma **顺序**：ALPHACLIP 在前、CLIP_RECT 在后（原版 `:44` → `:45`）"
                      + "｜🧨 反过来 = 照抄了 `WFUIBorderHighlight.shader`（那一族原版是相反的）"
                      + "（" + iPragA + " / " + iPragC + "）");

                // ---- B4：★ 对象空间（原版 `mad(POSITION.x, 2.0, -clamp(rect.x)) - clamp(rect.z)`）----
                Check(Count(src, "v.vertex.xy * 2.0 - clampedRect.xy - clampedRect.zw") == 1,
                      "B4 ★ 裁切插值器由 **`positionOS`** 算：`v.vertex.xy * 2.0 - clampedRect.xy - clampedRect.zw`"
                      + "（实际 " + Count(src, "v.vertex.xy * 2.0 - clampedRect.xy - clampedRect.zw") + "）");
                Check(Count(src, "clamp(_ClipRect, -2e10, 2e10)") == 1,
                      "B5 顶点段先 `clamp(_ClipRect, -2e10, 2e10)`（原版 `max/min l(±2e10)`）");

                // ---- B6：斜坡标度（原版 `0.25 / mad(softness, 0.25, |pixelSize|)`）----
                Check(Count(src, "0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize))") == 1,
                      "B6 斜坡标度照原版：`0.25 / (0.25 * _UIMaskSoftness + abs(pixelSize))`"
                      + "（实际 " + Count(src, "0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize))") + "）");

                // ---- B7：🔴 灭自证 —— `pixelSize` 的分子必须是 **`gl_Position.w`**（原版 b1 `:98`）----
                //   BorderHighlight 的原版是**字面量 1.0**；照抄过来这里就会红（正交下数值相同，但那是【形状】证据）。
                Check(Count(src, "o.pos.w / abs(proj2x2)") == 1
                   && Count(src, "1.0 / abs(float2(UNITY_MATRIX_P") == 0,
                      "B7 ★ `pixelSize = gl_Position.w / abs(P·SP)`（原版 b1 `:98` 的 `div _158, |mad(P00,SP.x,SP.y*P10)|`）"
                      + "｜🧨 写成 `1.0 / abs(...)` = 照抄了 BorderHighlight 那一族（它们原版才是字面量 1.0）");

                // ---- B8：投影那一项的形状（`P00*SP.x + SP.y*P10`，不是 `P00*SP.x` 单乘）----
                Check(Count(src, "UNITY_MATRIX_P[0][0] * scr.x + scr.y * UNITY_MATRIX_P[1][0]") == 1
                   && Count(src, "UNITY_MATRIX_P[0][1] * scr.x + scr.y * UNITY_MATRIX_P[1][1]") == 1,
                      "B8 投影项 = `P00*SP.x + SP.y*P10` / `P01*SP.x + SP.y*P11`"
                      + "（原版 `mad(glstate_matrix_projection[0u].x, SP.x, SP.y*[1u].x)`；"
                      + "`UNITY_MATRIX_P` 就 `#define` 到 `glstate_matrix_projection`）");

                // ---- B9/B10：软边 mask 公式 + 真乘进 alpha ----
                Check(Count(src, "clamp(((_ClipRect.z - _ClipRect.x) - abs(i.clipRect.x)) * i.clipRect.z, 0.0, 1.0)") == 1
                   && Count(src, "clamp(((_ClipRect.w - _ClipRect.y) - abs(i.clipRect.y)) * i.clipRect.w, 0.0, 1.0)") == 1,
                      "B9 ★ 片元是**软边**：`clamp((w - |t.x|) * t.z, 0, 1) * clamp((h - |t.y|) * t.w, 0, 1)`"
                      + "（= 原版 b9 `:34-40` 的 `_42/_43/_53/_54/_61/_62/_67`）");
                Check(Count(src, "outA *= mH * mW;") == 1,
                      "B10 ★ 裁切**真的乘进 alpha**：`outA *= mH * mW;`（= 原版 `mul_sat` 两条 + `mul r0.x, r0.y, r0.x`）"
                      + "｜🧨 算出来不乘 ⇒ 这条红");

                // ---- B11：b10 的顺序（mask 先乘、discard 判**乘完 mask** 的 alpha）----
                int iMaskMul = Idx(src, "outA *= mH * mW;");
                int iIfA = Idx(src, "#ifdef UNITY_UI_ALPHACLIP");
                int iDiscard = Idx(src, "if (outA - 0.001 < 0.0) discard;");
                int iRet = Idx(src, "return fixed4(outA * i.color.rgb * tx.rgb, outA);");
                Check(iMaskMul >= 0 && iIfA > iMaskMul && iDiscard > iIfA && iRet > iDiscard,
                      "B11 顺序：**mask 先乘进 alpha → 再 alpha clip → 再输出**"
                      + "（原版 b10 的 discard 判的是 `mad(_83, _115, -0.001)`，`_115` = mask）"
                      + "（" + iMaskMul + " / " + iIfA + " / " + iDiscard + " / " + iRet + "）");

                // ---- B12/B13：🔴 灭自证（两条一起，任何「两边一起改回去」的倒退都遮不住）----
                Check(Count(src, "UnityGet2DClipping") == 0,
                      "B12 灭自证：代码段里**没有** `UnityGet2DClipping`（`step`×2 的硬边，⛔ 原版不是它）"
                      + "（实际 " + Count(src, "UnityGet2DClipping") + "）");
                Check(Count(src, "unity_ObjectToWorld") == 0,
                      "B13 灭自证：代码段里**没有** `unity_ObjectToWorld`（裁切⛔ 不比世界空间）"
                      + "（实际 " + Count(src, "unity_ObjectToWorld") + "）");

                // ---- B14：`o.clipRect` 在 `vert` 里**无条件**算（四档 VS 逐字节相同）----
                int iVert = Idx(src, "v2f vert (appdata v)");
                int iFrag = Idx(src, "fixed4 frag (v2f i)");
                int iClipSet = Idx(src, "o.clipRect = float4(v.vertex.xy * 2.0");
                int iIfClip = Idx(src, "#ifdef UNITY_UI_CLIP_RECT");
                Check(iVert >= 0 && iClipSet > iVert && iFrag > iClipSet && iIfClip > iFrag,
                      "B14 `o.clipRect` 在 `vert` 里**无条件**赋值（唯一的 `#ifdef UNITY_UI_CLIP_RECT` 在 `frag` 之内）"
                      + "｜🧨 包进 `#ifdef` ⇒ VS 随关键字分叉，与原版（四档 VS 逐字节相同）不符"
                      + "（" + iVert + " / " + iClipSet + " / " + iFrag + " / " + iIfClip + "）");

                // ---- B15：`Properties` 里三条声明（运行时那条在 A1..A6）----
                Check(Count(src, "_ClipRect (\"Clip Rect\", Vector) = (-32767, -32767, 32767, 32767)") == 1
                   && Count(src, "_UIMaskSoftnessX (\"Mask Softness X\", Float) = 1") == 1
                   && Count(src, "_UIMaskSoftnessY (\"Mask Softness Y\", Float) = 1") == 1,
                      "B15 `Properties` 块里 `_ClipRect` / `_UIMaskSoftnessX` / `_UIMaskSoftnessY` 都声明了（默认值见 A4..A6）");

                // ---- B16：灭自证 —— 别把 `multi_compile_local` 改成 feature ----
                Check(Count(src, "shader_feature_local _ UNITY_UI_CLIP_RECT") == 0
                   && Count(src, "shader_feature UNITY_UI_CLIP_RECT") == 0,
                      "B16 灭自证：`UNITY_UI_CLIP_RECT` 仍是 `multi_compile_local`（原版四档编译产物 b7/b8/b9/b10 全在包里 ⇒ 不是 feature）");

                // ---- B17：b7 的算式没被本波改坏（五条锚点）----
                Check(Count(src, "round(i.color.w * 255.0)") == 1
                   && Count(src, "outA * i.color.rgb * tx.rgb") == 1
                   && Count(src, "tx.rgb, outA)") == 1
                   && Count(src, "v.color * _Color") == 1
                   && Count(src, "+ _TextureSampleAdd") == 1,
                      "B17 b7 骨架算式原样：8bit 量化 / 预乘 RGB / `_TextureSampleAdd` / `v.color * _Color`");
            }

            // ============================================================ C · C# 镜像 + 灭自证
            //  对象空间矩形（单位 = positionOS）：x∈[0,10] y∈[0,10]
            var rect = new Vector4(0f, 0f, 10f, 10f);
            float p = 0.5f;                                  // 正交 UI + canvas scale 1 时 |pixelSize| ≈ 0.5
            float rampZ = MirrorRamp(1f, p);                  // 默认 softness 1.0 ⇒ 1/3
            float rampW = rampZ;

            Check(Near(rampZ, 1f / 3f, 1e-5f),
                  "C0 斜坡标度 = `0.25/(0.25*1.0 + 0.5)` = 1/3（过渡带绝对宽 = 3 个对象单位；这条是「源码+默认值」推的）");

            // 裁切矩形正中 ⇒ mask = 1
            Check(Near(MirrorMask(5f, 5f, rect, rampZ, rampW), 1f, 1e-6f),
                  "C1 裁切矩形正中 ⇒ mask = 1");
            // 正好落在边上 ⇒ 0（离边距 = 0）
            Check(Near(MirrorMask(10f, 5f, rect, rampZ, rampW), 0f, 1e-6f),
                  "C1b 正好落在右边界 ⇒ mask = 0");

            // 🔴 C2 灭自证（**软边 vs 硬边**）：在**过渡带内**取点，mask 必须严格落在 (0,1) 开区间。
            //    x = 8.5 ⇒ tx = 2*8.5 - 0 - 10 = 7 ⇒ (10-7)/3 = 1.0 … 取 x 使 tx = 8.5 ⇒ x = 9.25
            float xInBand = 9.25f;                            // tx = 2*9.25 - 10 = 8.5
            float soft = MirrorMask(xInBand, 5f, rect, rampZ, rampW);
            float hard = MirrorHardMask(xInBand, 5f, rect);
            Check(soft > 1e-4f && soft < 1f - 1e-4f,
                  "C2 ★ 灭自证（软边）：过渡带内 (x=9.25) 的 mask 必须 ∈ (0,1) 开区间（实际 " + soft.ToString("R")
                  + "）｜🧨 改回 `step` 硬边 ⇒ 这里恒 1 或恒 0 ⇒ 红");
            Check(hard >= 1f && soft < 0.999f,
                  "C2b ★ 灭自证对照：同一点硬边给 " + hard.ToString("R") + "、软边给 " + soft.ToString("R")
                  + " ⇒ 两者必须可分（挡「镜像自己写成了 step」）");
            // 过渡带外（离边距 < -过渡带宽）⇒ 0。x = 10.5 ⇒ tx = 11 > w = 10 ⇒ clamp 到 0
            Check(Near(MirrorMask(10.5f, 5f, rect, rampZ, rampW), 0f, 1e-6f),
                  "C3 越过边界（x=10.5 ⇒ tx=11 > w=10）⇒ mask = 0");
            // 单调：从中心往外走，mask 不增（挡「把 h/w 两半乘反」之类）
            float prev = 2f; bool mono = true;
            for (int k = 0; k <= 20; k++)
            {
                float x = Mathf.Lerp(5f, 10f, k / 20f);
                float mk = MirrorMask(x, 5f, rect, rampZ, rampW);
                if (mk > prev + 1e-6f) { mono = false; break; }
                prev = mk;
            }
            Check(mono, "C4 从中心往外走 mask 单调不增（过渡带是线性斜坡，不是平台）");

            // ---- C5/C6：灭自证 —— mask 必须**真的参与结果**（原版 `_118 = _83 * _115`，且 RGB 预乘）----
            //    `outA = round(a*255)/255 * texA * mask`；`rgb = outA * COLOR.rgb * tex.rgb`
            float q = Mathf.Round(0.5f * 255f) / 255f;        // 原版 `_34`
            float outA1 = q * 1f * 1f;                        // texA = 1、mask = 1
            float outA2 = q * 1f * 0.5f;                      // texA = 1、mask = 0.5
            Check(Near(outA2 / outA1, 0.5f, 1e-6f),
                  "C5 灭自证：mask 从 1 降到 0.5 ⇒ outA 恰好减半（" + outA1.ToString("R") + " → " + outA2.ToString("R")
                  + "）｜🧨 算出来不乘进 alpha ⇒ 这里恒等 ⇒ 红");
            float premulAtZeroA    = 0f * 1f * 1f;            // 预乘：`rgb = outA * COLOR * tex` ⇒ 0
            float notPremulAtZeroA = 1f * 1f;                 // 非预乘：`rgb = COLOR * tex` ⇒ 1（错的那一版）
            Check(Near(premulAtZeroA, 0f, 1e-7f) && Mathf.Abs(notPremulAtZeroA - premulAtZeroA) > 0.5f,
                  "C6 灭自证：mask = 0 ⇒ outA = 0 ⇒ rgb = **0**（预乘）；对照非预乘写法会给 "
                  + notPremulAtZeroA.ToString("R") + " ⇒ 两者必须可分（漏乘 = 静默变亮）");
        }
        catch (System.Exception e)
        {
            _bad++;
            Debug.LogError(P + "✗ 异常：" + e);
        }
        finally
        {
            if (mat != null) Object.DestroyImmediate(mat);
        }

        Debug.Log(P + "合计 " + _ok + " 通过 / " + _bad + " 失败");
    }
}
