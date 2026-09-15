// IconSizeProbe.cs — 标定「卡面上的图标该多大」（2026-09-15）
//
// **为什么需要它**：原版那份 TMP sprite asset 的 `m_FaceInfo` **是 0**
// （`数据/游戏数据/trait_textsprites.json` 的 `face = {pointSize:0, scale:0}`），
// TMP 遇到 pointSize=0 会退回按**当前字体**算缩放（`TMP_Text.cs`，
// 见 `资料/卡面图标_现状与缺口.md` 第三节坑 5）。可**原版卡面用的字体和我们不是同一份**
// ⇒ 照抄原版参数**不保证**渲出来一样大。所以大小**必须拿成品卡图当尺子量出来**（铁律 7）。
//
// **尺子**：`d:/2/Warpforge部队卡片/Dark Angels/3部队/Warpforge_12_Aggressor.png`（900×1200）
//   卡面那枚 `questPoints1` 图标 —— 用模板匹配（图集切片 ↔ 卡面）量出 **≈60 px 高**，
//   同一行大写字母 `G`（`Gain`）**≈24.4 px** ⇒ **图标 ÷ 大写 ≈ 2.45**。
//   图标 ÷ 卡宽 = 60/900 = **6.7%**（卡本体 165 px 宽 ⇒ **≈ 11.0 px**）。
//
// **这个探针干什么**：把该比例**在我们自己的字体上量出来**，并给出该把
// `faceInfo.pointSize` 填多少（让图标在我方 fontSize 下恰好落在那两条线上）。
//
// 用法（本机带 ELECTRON_RUN_AS_NODE，启动前必须 unset，见 CLAUDE.md）：
//   -executeMethod IconSizeProbe.Run
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class IconSizeProbe
{
    const string P = "ISP ";
    const string OutDir = @"d:/4/_tmp_view/iconsize";

    /// <summary>我方卡面效果文字用的字号（`DescTextUnit` 的 `m_fontSize`）</summary>
    const float FontSize = 23.55f;

    static void Log(string s) { Debug.Log(P + s); }

    public static void Run()
    {
        Directory.CreateDirectory(OutDir);

        var sa = Resources.Load<TMP_SpriteAsset>(CardPresentation.CardIcons.SpriteAssetPath);
        if (sa == null) { Log("!! 找不到 sprite asset —— 先跑 IconSetup.Run"); Finish(); return; }

        // ── ① 先量我们这侧的两个基准 ──
        //    `H` 的墨迹高（世界单位）+ 该卡面字号下图标现在多高
        float fsCard = CardPresentation.CardView.KeywordFontSize;   // 卡面效果文字的字号
        float fsTitle = CardPresentation.CardView.TitleFontSize;
        float fsArmy = CardPresentation.CardView.ArmyFontSize;
        float cardH = CardPresentation.CardView.Height;
        Log("卡面字号：效果文字 " + fsCard.ToString("F2") + " · 卡名 " + fsTitle.ToString("F2") +
            " · 阵营行 " + fsArmy.ToString("F2") + " · 卡高 " + cardH.ToString("F3"));

        Measure(sa, fsCard, "H", "questPoints1", "qp1");

        // ── ② 扫描候选 scale，找「图标高 ÷ 卡高」命中成品的那个 ──
        //    成品卡图上量出来的是 **48/900 → 折成卡高是 48/1200 = 4.0%**
        //    （图标÷卡宽 5.33% 与 图标÷卡高 4.0% 是同一个量的两个写法）
        float nowScale = GlyphScale(sa, "questPoints1");
        Log("现在 `questPoints1` 的 glyph scale = " + nowScale.ToString("F3"));
        foreach (float mult in new[] { 0.50f, 0.674f, 0.80f, 1.00f })
        {
            SetGlyphScale(sa, "questPoints1", nowScale * mult);
            Measure(sa, fsCard, "H", "questPoints1", "sweep" + mult.ToString("F3").Replace(".", ""));
        }
        SetGlyphScale(sa, "questPoints1", nowScale);   // 还原（探针不许留副作用）

        Log("目标：图标高 ÷ 卡高 = **4.0%**（成品图 `Warpforge_12_Aggressor`：图标 48 px / 卡高 1200 px）");
        Log("     图标高 ÷ 卡宽 = **5.33%**（48/900）· 图标高 ÷ 大写高 ≈ **2.0**");
        Finish();
    }

    /// <summary>某 sprite 名的字形缩放（找不到返回 0）</summary>
    static float GlyphScale(TMP_SpriteAsset sa, string name)
    {
        int i = sa.GetSpriteIndexFromName(name);
        return i < 0 ? 0f : sa.spriteCharacterTable[i].glyph.scale;
    }

    static void SetGlyphScale(TMP_SpriteAsset sa, string name, float s)
    {
        int i = sa.GetSpriteIndexFromName(name);
        if (i < 0) return;
        var g = (TMP_SpriteGlyph)sa.spriteCharacterTable[i].glyph;
        g.scale = s;                             // 表里那一条和共享对象是同一个，改它即可
        EditorUtility.SetDirty(sa);
    }

    static void Finish()
    {
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }

    /// <summary>
    /// 渲一行 `<caps><sprite></caps>`，量各自的像素高。
    /// 只放 4 个字形，躲开折行；行内位置直接读 TMP 自己的 `characterInfo`，
    /// **不靠估算** —— 抗锯齿会把边沿算胖 1~2 px，两边同口径所以比值仍然可用。
    /// </summary>
    static void Measure(TMP_SpriteAsset sa, float fontSize, string caps, string spriteName, string tag)
    {
        var go = new GameObject("isp");
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = CardPresentation.TmpFont.Font;
        if (tmp.font == null) { Log("!! 没有 TMP 字体（TmpFont.Font == null）"); UnityEngine.Object.DestroyImmediate(go); return; }
        tmp.spriteAsset = sa;
        tmp.fontSize = fontSize;
        tmp.enableWordWrapping = false;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = spriteName == null ? caps : caps + "<sprite name=\"" + spriteName + "\">" + caps;
        tmp.ForceMeshUpdate();

        // ⚠️ `characterInfo.ascender/descender` 是**行盒**（整行一样高，实测 H/X/G 三个字
        //    给的都是同一个数）⇒ 量不出「这个字形多高」。真正的字形高度在**网格**里：
        //    `TMP_Text.textInfo.meshInfo[i].vertices` 里每个字 4 个顶点（TMP_Text.cs 的
        //    `characterInfo[i].vertexIndex` 指向它）。顶点是**局部单位**，× `m_Scale` 才是像素。
        var ti = tmp.textInfo;
        var parts = new List<string>();
        for (int i = 0; i < ti.characterCount; i++)
        {
            var ci = ti.characterInfo[i];
            float h = InkHeight(ti, i, tmp.transform.lossyScale.y);
            float w = ci.xAdvance;
            parts.Add(string.Format("#{0} '{1}' **inkH={2:F2}** w={3:F2}",
                                    i, ci.character, h, w));
        }
        Log("[" + tag + " @fontSize " + fontSize + "] " + string.Join(" | ", parts.ToArray()));

        // 真渲一张，人眼复核（断言测不出「画出来是个方块」）
        string png = Path.Combine(OutDir, tag + "_f" + fontSize.ToString("F0") + ".png");
        RenderToPng(tmp, png, 900, 260);
        UnityEngine.Object.DestroyImmediate(go);
    }

    /// <summary>
    /// 量第 i 个字的**墨迹高**（局部单位 → 乘 scale 得像素）。
    /// 顶点布局照 TMP：每个字符 4 个顶点（左下/左上/右上/右下），
    /// `characterInfo[i].vertexIndex` 是它的起点（`TMP_TextInfo.cs` 的 `vertexIndex`）。
    /// </summary>
    static float InkHeight(TMP_TextInfo ti, int i, float scale)
    {
        var ci = ti.characterInfo[i];
        int vi = ci.vertexIndex;
        var mi = ci.materialReferenceIndex;
        if (mi < 0 || mi >= ti.meshInfo.Length) return -1f;
        var v = ti.meshInfo[mi].vertices;
        if (vi < 0 || vi + 3 >= v.Length) return -1f;
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int k = 0; k < 4; k++)
        {
            float y = v[vi + k].y;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
        return (maxY - minY) * Mathf.Abs(scale);
    }

    static void RenderToPng(TMP_Text tmp, string path, int W, int H)
    {
        var camGo = new GameObject("isp_cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 1.6f;
        cam.transform.position = new Vector3(0, 0, -10);
        camGo.transform.LookAt(Vector3.zero);
        tmp.transform.position = Vector3.zero;

        var rt = new RenderTexture(W, H, 0, RenderTextureFormat.ARGB32);
        rt.Create();
        cam.targetTexture = rt;
        cam.Render();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(path, tex.EncodeToPNG());

        UnityEngine.Object.DestroyImmediate(tex);
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(camGo);
        Log("   图 → " + path);
    }
}
