// B114_W36_Assert.cs — `B-273`：`WFUIBorderHighlight.shader` 的 `UNITY_UI_CLIP_RECT` 两路与原版**双重不等价**。
//   ① 软/硬：原版是 `saturate` 线性斜坡（`mul_sat`，有限宽过渡带）；旧版用 `UnityGet2DClipping`（`step`×2，0/1 突变）。
//   ② 空间：原版比 **`positionOS`**；旧版比 **世界空间**（`mul(unity_ObjectToWorld, v.vertex).xy`）。
//       🔴 这条更隐蔽 —— 软边公式抄对了、空间不对照样错。
//   ③ 旧版**没有** `_UIMaskSoftnessX/Y` 两个属性，也没有整条 `pixelSize` 链（原版属性表里有）。
//
// 写手 `W-36` · 2026-11-01 · **没跑 Unity**（红线：`-executeMethod` 全禁），只写断言；宿主由调度台接线。
//
// ============================ 怎么接（⚠️ 只由调度台改宿主）============================
//  唯一入口 = `B114_W36_Assert.Run()`。挤进**某一条既有的聚合行**（同一物理行、**第一个 `//` 之前**）：
//      B114_W36_Assert.Run(); pass += B114_W36_Assert.Pass; fail += B114_W36_Assert.Fail;
//  ⚠️ `Editor/BattleScene.cs` 那族用的是局部变量 **`pass` / `fail`**（不是 `_sink`）；若宿主用 `_sink`，照那一行的变量名写。
//  ⚠️ **追加前先看该行第一个 `//` 在哪** —— 代码落在注释之后 = 断言一条都不会跑，而 `typecheck` / 账闸 / 退出码全绿
//     （CLAUDE.md §三 2026-10-30 那条，一天踩过 6 次）。
//
// ============================ 判据 ============================
//  `判据/B114_R8_BorderHighlight_clip.md`（原版 shader 自己的 **DXBC 逐指令**还原 · PS idx 24 / VS idx 0）。
//  原版片元（5 条裁切指令）：
//      add     r0.xy, -cb0[131].xyxx, cb0[131].zwzz   // _ClipRect.zw - _ClipRect.xy = (宽,高)
//      add     r0.xy, r0.xyxx, -|v3.xyxx|             // … - |texCoord1.xy|
//      mul_sat r0.xy, r0.xyxx, v3.zwzz                // saturate( … * texCoord1.zw )
//      mul     r0.x,  r0.y, r0.x
//      mul     o0.w,  r0.x, r0.w                     // color.a *=
//  原版顶点（`o3` 无条件算；clip 关键字不影响 VS）：
//      clampedRect = clamp(_ClipRect, -2e10, 2e10)
//      o3.xy = positionOS.xy*2 - clampedRect.xy - clampedRect.zw
//      o3.zw = 0.25 / ( 0.25*_UIMaskSoftness + |pixelSize| )
//      pixelSize = 1 / |P00*_ScreenParams.x , P11*_ScreenParams.y|
//
// ============================ 怎么跑（主对话 · 串行 · 要跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B114_W36_Assert.Run -logFile -
using System.IO;
using System.Text;
using UnityEngine;

public static class B114_W36_Assert
{
    const string P = "[B114_W36] ";
    const string ShaderPath = "WarpforgeVFX/Shaders/WFUIBorderHighlight.shader";
    const string ShaderName = "WarpforgeVFX/UI/UI Border Highlight";

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
    /// 去掉 `//` 行注释与 `/* */` 块注释 —— **必须的**：文件头的判据注释里逐字引用了
    /// `UnityGet2DClipping` / `unity_ObjectToWorld`，不剥就是必然假红。
    /// （HLSL 这里没有字符串字面量，逐字符扫就够。）
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

    public static void Run()
    {
        _ok = _bad = 0;
        Material mat = null;
        try
        {
            // ================================================================ A · 属性表（运行时）
            var sh = Shader.Find(ShaderName);
            Check(sh != null, "A0 前提：`Shader.Find(\"" + ShaderName + "\")` 取得到（取不到 = 本波改动没被 Unity 认到）");
            if (sh != null)
            {
                mat = new Material(sh);
                // 🔴 这条同时冒烟「shader 编得过」：编译失败时 Unity 换成 error shader ⇒ HasProperty 恒 false。
                Check(mat.HasProperty("_UIMaskSoftnessX"),
                      "A1 `_UIMaskSoftnessX` 真的在属性表里（原版有、旧版没有）"
                      + "｜🧨 只在 CGPROGRAM 里写了 `float _UIMaskSoftnessX;` 却没进 `Properties` ⇒ 这条红");
                Check(mat.HasProperty("_UIMaskSoftnessY"), "A2 `_UIMaskSoftnessY` 真的在属性表里");
                if (mat.HasProperty("_UIMaskSoftnessX"))
                    Check(Near(mat.GetFloat("_UIMaskSoftnessX"), 1f),
                          "A3 `_UIMaskSoftnessX` 默认 = 1.0（= ShaderGraph `CanvasProperties.cs` 那条属性的默认值；实际 "
                          + mat.GetFloat("_UIMaskSoftnessX") + "）");
                if (mat.HasProperty("_UIMaskSoftnessY"))
                    Check(Near(mat.GetFloat("_UIMaskSoftnessY"), 1f),
                          "A4 `_UIMaskSoftnessY` 默认 = 1.0（实际 " + mat.GetFloat("_UIMaskSoftnessY") + "）");

                // 默认值 = uGUI「不裁」档：改成 0 会在关键字被打开那一刻**整块消失**（静默）。
                // 软边公式下这一条更重要：`(max-min) - 2|posOS|`，默认档的宽度远大于任何 |2·posOS| ⇒ 恒不裁。
                if (mat.HasProperty("_ClipRect"))
                {
                    var r = mat.GetVector("_ClipRect");
                    Check(Near(r.x, -32767f, 1f) && Near(r.y, -32767f, 1f)
                       && Near(r.z, 32767f, 1f) && Near(r.w, 32767f, 1f),
                          "A5 `_ClipRect` 默认仍是「不裁」档 (-32767,-32767,32767,32767)（实际 "
                          + r.x + "," + r.y + "," + r.z + "," + r.w + "）｜🧨 是 0 = 全裁");
                }
                else Check(false, "A5 前提：`_ClipRect` 在属性表里（`B-262` 补的，⛔ 别被本波删掉）");
            }

            // ================================================================ B · 源码级
            //  ⚠️ `typecheck` 不覆盖 shader、本波也不许跑 Unity ⇒ 只能做**文本级**断言。
            //     「编译真的过」必须等 Unity 窗口（见报告 §哪些要等 Unity 验）。
            var raw = ReadSrc(ShaderPath);
            Check(raw != null, "B0 前提：读得到 `" + ShaderPath + "`");
            if (raw != null)
            {
                var src = StripComments(raw);   // ⚠️ 注释里逐字引用了旧写法，不剥必假红

                // ---- B1/B2：软边（`saturate` 线性斜坡），⛔ 不是 `UnityGet2DClipping` 的 step×2 ----
                Check(Count(src, "saturate((_ClipRect.zw - _ClipRect.xy - abs(i.clipRect.xy)) * i.clipRect.zw)") == 1,
                      "B1 片元是**软边**：`saturate((_ClipRect.zw - _ClipRect.xy - abs(i.clipRect.xy)) * i.clipRect.zw)`"
                      + "（= 原版 `mul_sat`；实际 " + Count(src, "saturate((_ClipRect.zw - _ClipRect.xy - abs(i.clipRect.xy)) * i.clipRect.zw)") + "）");
                Check(Count(src, "o.a *= mk.x * mk.y;") == 1,
                      "B2 裁切**真的乘进 alpha**：`o.a *= mk.x * mk.y;`（= 原版 `mul r0.x, r0.y, r0.x` + `mul o0.w, r0.x, r0.w`）");

                // ---- B3：顶点段 `clamp(_ClipRect, ±2e10)`（原版 `max/min l(-2e10)/l(2e10)`）----
                Check(Count(src, "clamp(_ClipRect, -2e10, 2e10)") == 1,
                      "B3 顶点段先 `clamp(_ClipRect, -2e10, 2e10)` 再送插值（原版 `max/min` 两条；PS 侧仍读**未 clamp** 的 `_ClipRect`）");

                // ---- B4：★ 对象空间（原版 `mad r1.xy, v0.xyxx, l(2,2), -r1.xyxx`）----
                Check(Count(src, "v.vertex.xy * 2.0 - clampedRect.xy - clampedRect.zw") == 1,
                      "B4 ★ 裁切插值器由 **`positionOS`** 算：`v.vertex.xy * 2.0 - clampedRect.xy - clampedRect.zw`"
                      + "（= 原版 `2*positionOS − clampedRect.xy − clampedRect.zw`）"
                      + "（实际 " + Count(src, "v.vertex.xy * 2.0 - clampedRect.xy - clampedRect.zw") + "）");

                // ---- B5：斜坡标度（原版 `div o3.zw, l(0.25), (0.25*softness + |pixelSize|)`）----
                Check(Count(src, "0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize))") == 1,
                      "B5 斜坡标度照原版：`0.25 / (0.25 * _UIMaskSoftness + abs(pixelSize))`"
                      + "（实际 " + Count(src, "0.25 / (0.25 * float2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize))") + "）");

                // ---- B6/B7：🔴 灭自证 ----
                //  ① 改回硬边（把软边换回 `UnityGet2DClipping`）⇒ B6 红；
                //  ② 改回世界空间（`mul(unity_ObjectToWorld, v.vertex)`）⇒ B7 红（B4 也会红）。
                //  两条一起，任何「两边一起改回去」的倒退都遮不住。
                Check(Count(src, "UnityGet2DClipping") == 0,
                      "B6 灭自证：代码段里**没有** `UnityGet2DClipping`（那是 `step`×2 的硬边，⛔ 原版不是它）"
                      + "（实际 " + Count(src, "UnityGet2DClipping") + "）");
                Check(Count(src, "unity_ObjectToWorld") == 0,
                      "B7 灭自证：代码段里**没有** `unity_ObjectToWorld`（裁切⛔ 不比世界空间；原版比 `positionOS`）"
                      + "（实际 " + Count(src, "unity_ObjectToWorld") + "）");

                // ---- B8：属性表（源码级复核；运行时那条在 A1/A2）----
                Check(Count(src, "_UIMaskSoftnessX (\"Mask Softness X\", Float) = 1") == 1
                   && Count(src, "_UIMaskSoftnessY (\"Mask Softness Y\", Float) = 1") == 1,
                      "B8 `Properties` 块里两条 softness 都声明了、默认都是 1");

                // ---- B9：顺序仍是「先乘裁切、再 alpha clip」（`B-262` 的骨架，⛔ 别推翻）----
                int iMask = Idx(src, "o.a *= mk.x * mk.y;");
                int iIfA = Idx(src, "#ifdef UNITY_UI_ALPHACLIP");
                int iAlpha = Idx(src, "clip (o.a - 0.001)");
                Check(iMask >= 0 && iIfA > iMask && iAlpha > iIfA,
                      "B9 顺序：**先乘裁切矩形、再 alpha clip**（原版 alphaclip 档也是这个序：先 `mad … -0.001` 再 `discard_nz`）"
                      + "（" + iMask + " / " + iIfA + " / " + iAlpha + "）");

                // ---- B10：`o3` 在 VS 里**无条件**算（原版 16 段 VS 都不受 clip 关键字影响，判据 §1·2）----
                int iVert = Idx(src, "v2f vert (appdata v)");
                int iFrag = Idx(src, "float4 frag (v2f i)");
                int iIfClip = Idx(src, "#ifdef UNITY_UI_CLIP_RECT");
                int iClipSet = Idx(src, "o.clipRect = float4(v.vertex.xy * 2.0");
                Check(iVert >= 0 && iClipSet > iVert && iFrag > iClipSet && iIfClip > iFrag,
                      "B10 `o.clipRect` 在 `vert` 里**无条件**赋值（唯一的 `#ifdef UNITY_UI_CLIP_RECT` 在 `frag` 之内）"
                      + "｜🧨 包进 `#ifdef` ⇒ VS 随关键字分叉，与原版不符"
                      + "（" + iVert + " / " + iClipSet + " / " + iFrag + " / " + iIfClip + "）");

                // ---- B11：`B-262` 的骨架不许被推翻（两条 pragma 仍各 1 条）----
                Check(Count(src, "#pragma multi_compile_local _ UNITY_UI_CLIP_RECT") == 1
                   && Count(src, "#pragma multi_compile_local _ UNITY_UI_ALPHACLIP") == 1,
                      "B11 两条 `#pragma multi_compile_local` 仍在（`B-262` 的骨架，⛔ 本波只改算式不改骨架）");

                // ---- B12：三条原版 feature 关键字仍是 `shader_feature_local`（别顺手改掉）----
                Check(Count(src, "#pragma shader_feature_local _DEBUG") == 1
                   && Count(src, "#pragma shader_feature_local _AUTOROTATION") == 1
                   && Count(src, "#pragma shader_feature_local _FLIPDIRECTION") == 1
                   && Count(src, "#pragma multi_compile_local _DEBUG") == 0,
                      "B12 灭自证：三条原版 feature 关键字仍是 `shader_feature_local`（原版出包会裁变体）");
            }
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
