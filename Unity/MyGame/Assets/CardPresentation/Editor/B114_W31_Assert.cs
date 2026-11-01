// B114_W31_Assert.cs — `B-261`（`UIBorderGlow.ChangeGraphicAlpha` 静默写 `_Color`）与
//                       `B-262`（原版有 `UNITY_UI_CLIP_RECT` / `UNITY_UI_ALPHACLIP`，本 shader 原来没有）
// 写手 `W-31` · 2026-11-01 · **没跑 Unity**（红线：`-executeMethod` 全禁），只写断言；宿主由调度台接线。
//
// ============================ 怎么接（⚠️ 只由调度台改宿主）============================
//  唯一入口 = `B114_W31_Assert.Run()`。挤进**某一条既有的聚合行**（同一物理行、**第一个 `//` 之前**）：
//      B114_W31_Assert.Run(); pass += B114_W31_Assert.Pass; fail += B114_W31_Assert.Fail;
//  ⚠️ `Editor/BattleScene.cs` 那族用的是局部变量 **`pass` / `fail`**（不是 `_sink`）；若宿主用 `_sink`，照那一行的变量名写。
//  ⚠️ **追加前先看该行第一个 `//` 在哪** —— 代码落在注释之后 = 断言一条都不会跑，而 `typecheck` / 账闸 / 退出码全绿
//     （CLAUDE.md §三 2026-10-30 那条，一天踩过 6 次）。
//
// ============================ 判据 ============================
//  `B-261`：原版 `ChangeGraphicAlpha(float target)`（原版 `UIBorderGlow.cs:262`）= 改 `MyGraphic.color`
//          —— uGUI 里那是 `CanvasRenderer` 的**顶点色**。本族 shader（`WarpforgeVFX/UI/UI Border Highlight`
//          与原版 `Everguild/UI/UI Border Highlight Appear*`）属性表里**都没有 `_Color`**
//          （判据 → `判据/B114_R5_Reveal_DXBC.md` §2·1 `o2 = v4` / §2·2 ⑧ 末句 `mul o0.xyzw, r0.xyzw, v2.xyzw`）
//          ⇒ 写 `_Color` 那一笔**静默落空**。
//  `B-262`：原版 `m_KeywordNames[18]=UNITY_UI_CLIP_RECT` / `[19]=UNITY_UI_ALPHACLIP`，`m_KeywordFlags` 同为 1
//          （= local），编译产物里确有 clip / alphaclip 两路（判据 → `报告/B114_W13_边框shader其余5处.md` §一·②）。
//          ⚠️ 本工程**没有数据源**（`ImageQuad` 是 `MeshRenderer`、壳侧裁切是几何等效）⇒ 断言只断
//          「按原版补上了、顺序/默认值对」，**不断「裁切真的生效」**（那需要一条数据源，今天不存在）。
//
// ============================ 怎么跑（主对话 · 串行 · 要跑 Unity）============================
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B114_W31_Assert.Run -logFile -
using System.IO;
using UnityEngine;
using CardPresentation;

public static class B114_W31_Assert
{
    const string P = "[B114_W31] ";
    const string ShaderPath  = "WarpforgeVFX/Shaders/WFUIBorderHighlight.shader";
    const string GlowPath    = "CardPresentation/Shell/UIBorderGlow.cs";
    const string ShaderName  = "WarpforgeVFX/UI/UI Border Highlight";

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

    /// <summary>从 <paramref name="start"/> 之后找（文件头的判据注释里也引用了同样的写法 ⇒ 必须从代码段起算）。</summary>
    static int IdxFrom(string s, string sub, int start)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub) || start < 0 || start >= s.Length) return -1;
        return s.IndexOf(sub, start, System.StringComparison.Ordinal);
    }

    /// <summary>网格四角顶点色的 α 是不是都等于 <paramref name="a"/>（顶点数必须正好 4）。</summary>
    static bool AllAlpha(Mesh m, float a)
    {
        if (m == null) return false;
        var cols = m.colors;
        if (cols == null || cols.Length != 4) return false;
        for (int i = 0; i < 4; i++) if (!Near(cols[i].a, a)) return false;
        return true;
    }

    public static void Run()
    {
        _ok = _bad = 0;
        GameObject host = null;
        try
        {
            // ================================================================ A · 活 1（`B-261`）运行时
            var sh = Shader.Find(ShaderName);
            Check(sh != null, "A0 前提：`Shader.Find(\"" + ShaderName + "\")` 取得到（取不到 = 本波改动没被 Unity 认到）");
            if (sh != null)
            {
                host = new GameObject("B114_W31_Host");
                var q = ImageQuad.Create(host.transform, Texture2D.whiteTexture, Vector3.zero, 1f,
                                         new Vector2(0.5f, 0.5f), "W31Quad");
                Check(q != null, "A1 前提：`ImageQuad.Create` 建得出载体");
                if (q != null)
                {
                    var mat = new Material(sh);
                    q.SetMaterial(mat);
                    // 🔴 **前提硬检查在前**：这颗 shader 没有 `_Color` ⇒ 旧路（写 `_Color`）**必然**落空。
                    //    这条同时是「灭自证」的另一半 —— 若哪天有人给 shader 补上 `_Color`，这条会先红。
                    Check(!mat.HasProperty("_Color"),
                          "A2 前提：这颗 shader **没有 `_Color`**（判据 = 原版 `m_PropInfo` 19 条里没有它）");
                    // 🔴 `B-262`：这条比文本级断言强 —— 「pragma 写了但属性没声明」只有这里抓得住
                    //   （且它同时冒烟了「shader 编得过」；真正的编译错误仍要等 Unity 窗口）。
                    Check(mat.HasProperty("_ClipRect"),
                          "A2b `_ClipRect` 真的在 shader 的属性表里（默认值 = 不裁档）"
                          + "｜🧨 只在源码里写了 `float4 _ClipRect;` 却没进 `Properties` ⇒ 这条红");

                    var glow = q.gameObject.AddComponent<UIBorderGlow>();
                    Check(glow != null, "A3 前提：`UIBorderGlow` 挂得上（挂在**渲染件那个 GO** 上）");
                    if (glow != null)
                    {
                        glow.ChangeGraphicAlpha(0.25f);

                        var mf = q.GetComponent<MeshFilter>();
                        Check(mf != null && mf.sharedMesh != null, "A4 前提：载体有网格（`MeshFilter.sharedMesh`）");
                        if (mf != null && mf.sharedMesh != null)
                        {
                            Check(q.UsesVertexTint, "A5 `ChangeGraphicAlpha` 之后顶点色通道被打开");
                            Check(AllAlpha(mf.sharedMesh, 0.25f),
                                  "A6 **顶点色 α 真的变成 0.25**（走 `Mesh.colors`；⛔ 不是写 `_Color`）"
                                  + "（实际 " + DumpAlpha(mf.sharedMesh) + "）");
                        }
                        Check(Near(q.Tint.a, 0.25f), "A6b `Tint.a` 读回 = 0.25（顶点色那份，实际 " + q.Tint.a + "）");

                        // 灭自证：**关掉通道** ⇒ 顶点色必须回到白 ⇒ 证明 A6 量的确实是「顶点色通道」这条唯一来源，
                        //   而不是别的东西恰好也是 0.25。
                        if (mf != null)
                        {
                            q.SetVertexTintChannel(false);
                            Check(AllAlpha(mf.sharedMesh, 1f),
                                  "A7 灭自证：关掉通道后顶点色回到白（1.0）（实际 " + DumpAlpha(mf.sharedMesh) + "）"
                                  + "｜🧨 若 A6 是「别处给的 0.25」，这条不会跟着变");
                            q.SetVertexTintChannel(true);
                            Check(AllAlpha(mf.sharedMesh, 0.25f),
                                  "A8 再打开 ⇒ 回到 0.25（通道是**唯一**来源；实际 " + DumpAlpha(mf.sharedMesh) + "）");
                        }
                    }
                }

                // ---- A9：材质**有** `_Color` 时不许换路（换路会与已有 `_Color` 双乘）----
                var q2 = ImageQuad.Create(host.transform, Texture2D.whiteTexture, Vector3.zero, 1f,
                                          new Vector2(0.5f, 0.5f), "W31QuadColor");
                Check(q2 != null, "A9 前提：第二块载体建得出来");
                if (q2 != null)
                {
                    var mr2 = q2.GetComponent<MeshRenderer>();
                    Check(mr2 != null && mr2.sharedMaterial != null && mr2.sharedMaterial.HasProperty("_Color"),
                          "A9b 前提：`ImageQuad.Create` 出厂的 `Sprites/Default` **有** `_Color`");
                    var glow2 = q2.gameObject.AddComponent<UIBorderGlow>();
                    if (glow2 != null) glow2.ChangeGraphicAlpha(0.25f);
                    Check(!q2.UsesVertexTint, "A10 材质有 `_Color` 时**不换路**（旧路本来就生效，换路会双乘）");
                    Check(mr2 != null && mr2.sharedMaterial != null && Near(mr2.sharedMaterial.color.a, 0.25f),
                          "A11 旧路照旧写进 `_Color`（实际 α = "
                          + (mr2 != null && mr2.sharedMaterial != null ? mr2.sharedMaterial.color.a : -1f) + "）");
                }
            }

            // ================================================================ B · 活 2（`B-262`）源码级
            //  ⚠️ `typecheck` 不覆盖 shader、本波也不许跑 Unity ⇒ 只能做**文本级**断言。
            //     「编译真的过」必须等 Unity 窗口（见报告 §哪些要等 Unity 验）。
            var shSrc = ReadSrc(ShaderPath);
            Check(shSrc != null, "B0 前提：读得到 `" + ShaderPath + "`");
            if (shSrc != null)
            {
                Check(Count(shSrc, "#pragma multi_compile_local _ UNITY_UI_CLIP_RECT") == 1,
                      "B1 补上 `#pragma multi_compile_local _ UNITY_UI_CLIP_RECT`（uGUI `UI/Default` 的约定）"
                      + "（实际 " + Count(shSrc, "#pragma multi_compile_local _ UNITY_UI_CLIP_RECT") + "）");
                Check(Count(shSrc, "#pragma multi_compile_local _ UNITY_UI_ALPHACLIP") == 1,
                      "B2 补上 `#pragma multi_compile_local _ UNITY_UI_ALPHACLIP`"
                      + "（实际 " + Count(shSrc, "#pragma multi_compile_local _ UNITY_UI_ALPHACLIP") + "）");

                // ⚠️ 全部**从代码段起算**：文件头的判据注释里也引用了同样两句，
                //    直接 `IndexOf` 会命中注释 ⇒ 顺序断言会假红（B4/B5）。
                int iIf = Idx(shSrc, "#ifdef UNITY_UI_CLIP_RECT");
                int iClip = IdxFrom(shSrc, "o.a *= mk.x * mk.y;", iIf + 1);
                int iIfA = IdxFrom(shSrc, "#ifdef UNITY_UI_ALPHACLIP", iIf + 1);
                int iAlpha = IdxFrom(shSrc, "clip (o.a - 0.001)", iIfA + 1);

                // 🔴 「别做半截」：只有 pragma、没有算式 ⇒ 这两条会红。
                Check(iIf >= 0 && iClip > iIf,
                      "B3 裁切**真的乘进 alpha**（`o.a *= mk.x * mk.y;` 在 `#ifdef UNITY_UI_CLIP_RECT` 之内）"
                      + "（" + iIf + " / " + iClip + "）");
                Check(iIfA >= 0 && iAlpha > iIfA,
                      "B4 alpha clip **真的写了**（`clip (o.a - 0.001)` 在 `#ifdef UNITY_UI_ALPHACLIP` 之内）"
                      + "（" + iIfA + " / " + iAlpha + "）");
                Check(iClip >= 0 && iAlpha > iClip,
                      "B5 顺序照 uGUI `UI/Default`：**先乘裁切矩形、再 alpha clip**"
                      + "（" + iClip + " / " + iAlpha + "）");

                // 默认值必须是 uGUI 的「不裁」档：写成 0 会在关键字被打开的那一刻**整块消失**（静默）。
                Check(Count(shSrc, "_ClipRect (\"Clip Rect\", Vector) = (-32767,-32767,32767,32767)") == 1,
                      "B6 `_ClipRect` 的默认值是「不裁」档 `(-32767,-32767,32767,32767)`（⛔ 别是 0 = 全裁）");

                // 灭自证：三条**原版 feature 关键字**不许被顺手改成 `multi_compile_local`
                //   （原版出包会裁变体，判据 = 编译产物只有 4/8 种组合）。
                Check(Count(shSrc, "#pragma shader_feature_local _DEBUG") == 1
                   && Count(shSrc, "#pragma shader_feature_local _AUTOROTATION") == 1
                   && Count(shSrc, "#pragma shader_feature_local _FLIPDIRECTION") == 1,
                      "B7 灭自证：三条原版 feature 关键字仍是 `shader_feature_local`（⛔ 别改成 `multi_compile_local`）");
                Check(Count(shSrc, "#pragma multi_compile_local _DEBUG") == 0,
                      "B7b 灭自证：没有任何一条 `_DEBUG` 被写成 `multi_compile_local`");
            }

            // ================================================================ C · 活 1（`B-261`）源码级
            var glowSrc = ReadSrc(GlowPath);
            Check(glowSrc != null, "C0 前提：读得到 `" + GlowPath + "`");
            if (glowSrc != null)
            {
                Check(Count(glowSrc, "_quad.SetVertexTintChannel(true)") == 1,
                      "C1 `ChangeGraphicAlpha` 里**开了顶点色通道**（`_quad.SetVertexTintChannel(true)`）"
                      + "（实际 " + Count(glowSrc, "_quad.SetVertexTintChannel(true)") + "）");
                Check(Count(glowSrc, "if (!_quad.UsesVertexTint && !QuadMaterialHasColor())") == 1,
                      "C2 「材质有 `_Color` 时不换路」那道闸在（⛔ 删了它 ⇒ 与已有 `_Color` 双乘）");
                Check(Count(glowSrc, "_quad.SetTint(c);") == 1,
                      "C3 推色仍然走 `SetTint`（通道开着时它写 `Mesh.colors`，**并且**会把色转给软边子块；`SetVertexTint` 不转）");
                Check(Count(glowSrc, "WarnIfVertexTintUnreachable()") == 2,
                      "C4 【出声】口在位（1 处定义 + 1 处调用；⛔ 不让「改了 α 画面不动」静默）"
                      + "（实际 " + Count(glowSrc, "WarnIfVertexTintUnreachable()") + "）");
                Check(Count(glowSrc, "? _quad.Tint : Color.white") == 1,
                      "C5 `CurrentColor()` 对「没有 `_Color` 且通道没开」的材质兜底成白"
                      + "（否则 `graphicOriginalColor.a` 会读到 0、`Anim()` 末句把 α「还原」成 0）");
            }
        }
        catch (System.Exception e)
        {
            _bad++;
            Debug.LogError(P + "✗ 异常：" + e);
        }
        finally
        {
            if (host != null) Object.DestroyImmediate(host);
        }

        Debug.Log(P + "合计 " + _ok + " 通过 / " + _bad + " 失败");
    }

    static string DumpAlpha(Mesh m)
    {
        if (m == null) return "null";
        var c = m.colors;
        if (c == null || c.Length == 0) return "无 colors";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < c.Length && i < 4; i++) { if (i > 0) sb.Append('/'); sb.Append(c[i].a.ToString("F3")); }
        return sb.ToString();
    }
}
