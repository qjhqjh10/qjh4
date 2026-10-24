// IconSizeProbe.cs — 标定「卡面上的图标该多大」（2026-09-15）
//
// **为什么需要它**：原版那份 TMP sprite asset 的 `m_FaceInfo` **是 0**
// （`数据/游戏数据/trait_textsprites.json` 的 `face = {pointSize:0, scale:0}`），
// TMP 遇到 pointSize=0 会退回按**当前字体**算缩放（`TMP_Text.cs`，
// 见 `资料/卡面图标_现状与缺口.md` 第三节坑 5）。可**原版卡面用的字体和我们不是同一份**
// ⇒ 照抄原版参数**不保证**渲出来一样大。所以大小**必须拿成品卡图当尺子量出来**（铁律 7）。
//
// **尺子**：`d:/2/Warpforge部队卡片/Dark Angels/3部队/Warpforge_12_Aggressor.png`（900×1200）
//   🔴 **2026-10-23 就地订正（铁律 5；与 `IconSetup.cs` 头那一段同一次实测）**：本节原写
//   「图标 ≈60 px / 大写 `G` ≈24.4 px ⇒ 图标 ÷ 大写 ≈ **2.45**」—— 两个读数都偏了：
//     · 图标墨迹（阈值 90~110 稳定）= **51 px**（不是 60）；
//     · 同一行**真正的拉丁大写**（`S` / `G`）= **31 px**；原写的 24.4 是**小写 x-height**（`a/i/n` 23~24）。
//   ⇒ **图标 ÷ 大写 = 51/31 = 1.65**；**图标 ÷ x-height ≈ 48/24 ≈ 2.0**（旧那个 2.45 哪个口径都不是）。
//   ⚠️ 图标 ÷ 卡高 = 51/1200 ≈ **4.3%**（旧写 4.0% 是按 48 px 算的）—— 那是**印刷版式**，
//   与卡面渲染值**不可比**（卡面还要过 `FitToBox`），⛔ 别拿它当标定目标（详见 `IconSetup.cs` 头）。
//   🔴 **2026-10-24 就地订正（铁律 5）**：本节那段 1.65 是 **PnP 印刷品的读数**，**已不再当标定锚** ——
//   调度台 2026-10-24 裁定按**数字客户端**，`CalibScale` 落 **1.0**，标定锚 = **1.983**
//   （`ascentLine/capLine × glyph.m_Scale` = `70/60 × 1.7`；判据 → `资料/普查产出_1024/C3_界面卡牌活动判据总表.md` §2）。
//   ⇒ 下面凡是「标定目标 = 1.65」的说法都按这条更正；1.65 只作为**历史尺子 / 交叉参考**留着。
//   ⚠️ 本文件原来**只 `Log`、没有任何 `Check`**（C3 §顺手发现 5）—— 它是复量用的探针，**别当验收口**。
//   🔴 **2026-10-24（`E9`）就地订正**：**已补断言**（见下面 ④）。现在它有**四类 `Check`（共 7 条）**：
//     ① 资产 / 字体 / `questPoints1` 三项前提（原来是 `Log` + 早退）· ② 扫描**真的随参数单调且线性地变**（量具自检）·
//     ③ 扫完**逐位还原**（探针不许留副作用）· ④ 标定锚 **图标 ÷ 拉丁大写 = 1.983**（数字客户端口径、±10%）。
//     ⚠️ 生产侧的**主断言**仍是 `Editor/IconSetup.cs` 的 ⑨（同一个锚）；本节是**探针侧的同一条判据**
//     （原来漂了没有任何人会知道）。两处的锚与容差要**一起改**。
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

    // 🔴 2026-10-24（E9）：本文件原来**一条断言都没有**（只 `Log`）⇒ 补上。
    //   判据/口径同 `Editor/IconSetup.cs` 的 ⑨（那个是**主**断言）；这里量的是**探针自己这条量具**。
    static int _pass, _fail;
    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    public static void Run()
    {
        Directory.CreateDirectory(OutDir);

        var sa = Resources.Load<TMP_SpriteAsset>(CardPresentation.CardIcons.SpriteAssetPath);
        Check(sa != null, "① 前提：卡面图标 sprite asset 加载得到（`"
                        + CardPresentation.CardIcons.SpriteAssetPath + "`）—— 找不到先跑 `IconSetup.Run`");
        if (sa == null) { Finish(); return; }

        // 图标缩放按**卡面正文面**的 faceInfo 算（`TMP_Text.cs:4164`）⇒ 字体面必须真的要得到，
        // 否则后面每一个读数都不是卡面会渲出来的值（2026-10-23 那次换面的教训，见 `Measure` 的 doc）。
        var faceBody = CardPresentation.TmpFont.FaceAsset(CardPresentation.TmpFont.Face.LatinCardBody);
        Check(faceBody != null, "① 前提：卡面正文面 `TmpFont.Face.LatinCardBody`（Pragati）加载得到");
        if (faceBody == null) { Finish(); return; }

        // ── ① 先量我们这侧的两个基准 ──
        //    `H` 的墨迹高（世界单位）+ 该卡面字号下图标现在多高
        float fsCard = CardPresentation.CardView.KeywordFontSize;   // 卡面效果文字的字号
        float fsTitle = CardPresentation.CardView.TitleFontSize;
        float fsArmy = CardPresentation.CardView.ArmyFontSize;
        float cardH = CardPresentation.CardView.Height;
        Log("卡面字号：效果文字 " + fsCard.ToString("F2") + " · 卡名 " + fsTitle.ToString("F2") +
            " · 阵营行 " + fsArmy.ToString("F2") + " · 卡高 " + cardH.ToString("F3"));

        Measure(sa, fsCard, "H", "questPoints1", "qp1");

        // ── ② 扫描候选 scale（**只做读数扫描**，不拿「÷ 卡高」当目标 —— 见文件头那条订正）──
        //    成品卡图上量出来的**印刷版式**读数：图标 51 px ÷ 卡宽 900 = 5.67% / ÷ 卡高 1200 ≈ 4.3%
        //    ⚠️ 卡面永远先过 `CardView.FitToBox` ⇒ 「÷ 卡高」逐卡不同，**不能当标定目标**；
        //       标定目标 = 图标 quad ÷ 拉丁大写 quad = **1.983**（数字客户端口径、`CalibScale = 1.0` 下
        //       `ascentLine/capLine × glyph.m_Scale` = `70/60 × 1.7`）。⚠️ PnP 的 **1.65**（51/31）是**历史尺子**，
        //       2026-10-24 起不再当锚（裁定与留痕 → `IconSetup.cs` 里 `CalibScale` 的 doc）。
        float nowScale = GlyphScale(sa, "questPoints1");
        Check(nowScale > 0f, "① 前提：`spriteCharacterTable` 里找得到 `questPoints1`（实测 glyph scale = "
                           + nowScale.ToString("F4") + "）—— 找不到 ⇒ 资产与 `CardIcons` 的键对不上");
        if (nowScale <= 0f) { Finish(); return; }
        Log("现在 `questPoints1` 的 glyph scale = " + nowScale.ToString("F3"));

        float[] mults = { 0.50f, 0.674f, 0.80f, 1.00f };
        var hs = new float[mults.Length];
        try
        {
            for (int i = 0; i < mults.Length; i++)
            {
                SetGlyphScale(sa, "questPoints1", nowScale * mults[i]);
                hs[i] = Measure(sa, fsCard, "H", "questPoints1", "sweep" + mults[i].ToString("F3").Replace(".", ""));
            }
        }
        finally
        {
            // 还原（探针不许留副作用）—— 放 `finally` 里：中途抛异常也不许把资产留在半路那一档
            // （它会 `EditorUtility.SetDirty`，真被存下去就把 `IconSetup` 的资产改坏了）。
            SetGlyphScale(sa, "questPoints1", nowScale);
        }

        // ── ③ 🔴 **量具自检**：读数必须**随我们设的那一个参数**单调、且线性地变 ──
        //   为什么必须有这条：下面 ④ 那个「绝对值落在锚上」只有在**量具本身有效**时才有意义
        //   （量错了也会得到一个数）。TMP 那边 icon 高 ∝ `fontSize/pointSize × scale × glyph.scale`
        //   ⇒ 逐档读数应 ≈ `基准 × mult`（实测偏差 <1%；这里给 5% 的余量，只抓「量具坏了」那一档）。
        float hBase = hs[mults.Length - 1];                   // mult = 1.00 ⇒ 就是 `nowScale` 那一档
        bool mono = hBase > 0f;
        for (int i = 1; i < hs.Length; i++) if (!(hs[i] > hs[i - 1])) mono = false;
        Check(mono, "② 量具自检：四档读数**严格单调增**（扫描真的改了渲染结果，不是读同一个数）"
                  + "｜实测 " + string.Join(" / ", Array.ConvertAll(hs, h => h.ToString("F2"))));
        float worstRel = 0f;
        if (hBase > 0f)
            for (int i = 0; i < hs.Length; i++) worstRel = Mathf.Max(worstRel, Mathf.Abs(hs[i] / hBase - mults[i]));
        Check(hBase > 0f && worstRel <= 0.05f,
              "② 量具自检：读数与设定的 scale 乘数**线性**（最大相对偏差 " + worstRel.ToString("F3") + " ≤ 0.05）"
            + "｜🧨 量错了字形 / 量了行盒（同一行恒高）⇒ 单调或线性那条红");

        float back = GlyphScale(sa, "questPoints1");
        Check(Mathf.Abs(back - nowScale) < 1e-6f,
              "③ 扫完**逐位还原** `questPoints1` 的 glyph scale（实测 " + back.ToString("F6")
            + " vs 原 " + nowScale.ToString("F6") + "）—— 探针不许留副作用"
            + "｜🧨 还原那句被删/写错 ⇒ 下一次 `IconSetup` 量到的是被探针改过的资产 ⇒ 红");

        // ⚠️ 这两行是**印刷版式**的读数（会随文本长短过 `FitToBox` 变）—— 拿它们当标定目标会做错，
        //    标定目标是「图标 quad ÷ **拉丁大写** quad = **1.983**」（数字客户端口径；PnP 的 1.65 已是历史尺子）。
        Log("参考（**印刷版式，不是标定目标**）：图标高 ÷ 卡高 ≈ **4.3%**（成品图 `Warpforge_12_Aggressor`：图标 51 px / 卡高 1200 px）");
        Log("     图标高 ÷ 卡宽 ≈ **5.67%**（51/900）· PnP 墨比 图标 51 px ÷ 大写 31 px = **1.65**（**历史尺子，已不锚**）" +
            " —— 数字客户端口径的标定锚 = **1.983**（`70/60 × 1.7`）");

        // ── ④ 🔴 **标定锚**（2026-10-24 `E9` 新增；本文件原来只打印、不断言）──
        //   分子 = 上面**渲出来的**那个图标墨高（`Measure` 的返回值，网格顶点 → 像素）；
        //   分母 = **拉丁大写的解析高**：`capLine / pointSize × scale × 0.1 × fontSize`
        //     —— 与 TMP 自己算字号那条式子同源（`TMP_Text.cs:7183`），**不含本项目任何常数**。
        //   ⇒ 比值应 = `ascentLine / capLine × glyph.m_Scale` = Pragati `70/60` × `1.7` = **1.983**
        //     （数字客户端口径；口径裁定 → `资料/普查产出_1024/C3_界面卡牌活动判据总表.md` §2）。
        //   ⚠️ 与 `IconSetup` ⑨ **同一把尺子、同一个容差**（±10%，抓的是结构性漂移：换面 / 资产没重建 /
        //     `m_Scale` 被改）。两处**要一起改**（主断言在那里，这里是探针侧的同一条判据）。
        {
            const float IconOverCap = 1.983f;
            const float TolRel = 0.10f;
            float capH = -1f, ratio = -1f;
            var fi = faceBody.faceInfo;
            if (fi.pointSize > 0f && hBase > 0f)
            {
                capH = fi.capLine / fi.pointSize * fi.scale * 0.1f * fsCard;
                if (capH > 0f) ratio = hBase / capH;
            }
            Log("④ 读数：图标墨高 " + hBase.ToString("F3") + " · 大写（解析）" + capH.ToString("F4")
                + " · **图标 ÷ 大写 = " + ratio.ToString("F3") + "**（目标 " + IconOverCap.ToString("F3") + "）");
            Check(ratio > 0f && Mathf.Abs(ratio / IconOverCap - 1f) <= TolRel,
                  "④ **图标墨高 ÷ 拉丁大写墨高** 落在数字客户端口径的几何值上：实测 " + ratio.ToString("F3")
                + " / 目标 " + IconOverCap.ToString("F3") + "（= `ascentLine/capLine × glyph.m_Scale` = `70/60 × 1.7`）"
                + "，容差 ±" + (TolRel * 100f).ToString("F0") + "%"
                + "｜🧨 `CalibScale` 改回旧值 0.842 并重建资产 ⇒ ≈1.67 ⇒ 红；"
                + "🧨 正文面换成 `TmpFont.Font`（CJK 面）⇒ ≈2.72 ⇒ 红；"
                + "🧨 资产没重建（盘上还是 0.842 那版 scale ≈1.4314）⇒ ≈1.67 ⇒ 红（先跑 `-executeMethod IconSetup.Run`）");
        }

        Debug.Log(P + $"=== 合计：{_pass} 通过 / {_fail} 失败 ===");
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
        // 🔴 2026-10-24（E9）：批处理下**用退出码回话**（0 = 全过 / 1 = 有失败）——
        //   原来的 `Exit(0)` 无论断言怎样都算成功（和 `CardBaseDemo` 等宿主同一条规矩）。
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    /// <summary>
    /// 渲一行 `&lt;caps>&lt;sprite>&lt;/caps>`，量各自的像素高。
    /// 只放 4 个字形，躲开折行；行内位置直接读 TMP 自己的 `characterInfo`，
    /// **不靠估算** —— 抗锯齿会把边沿算胖 1~2 px，两边同口径所以比值仍然可用。
    /// <para>🆕 2026-10-24（`E9`）：**返回值 = 那一格 `<sprite>` 的墨迹高**（= 它的字形框高，像素/局部单位；
    /// 没有 sprite 或量不到 ⇒ **0**）。扫描那四档靠它断「读数随参数线性变」。</para>
    /// </summary>
    static float Measure(TMP_SpriteAsset sa, float fontSize, string caps, string spriteName, string tag)
    {
        var go = new GameObject("isp");
        var tmp = go.AddComponent<TextMeshPro>();
        // 🔴 **2026-10-23（卡牌线 `D2`）：探针必须用【卡面效果文字那一个字体面】** ——
        //   原来是 `TmpFont.Font`（**CJK 面**：`NotoSerifCJK`），而卡面正文实际是
        //   `Face.LatinCardBody`（Pragati，`CardView.cs:4064` 的 `face: TmpFont.Face.LatinCardBody`）。
        //   为什么这个差别要命：原版那份 sprite asset 的 `m_FaceInfo` 是 0 ⇒ TMP 按**正文字体的
        //   faceInfo** 算图标缩放（`TMP_Text.cs:4164`：`spriteScale = fontSize / pointSize × scale × 0.1`
        //   与 `ascentLine / glyph.metrics.height`）⇒ **字体面一换、同一个 `m_Scale` 的图标大小就变**。
        //   现读两面的基准：CJK `ascentLine 57.55 / pointSize 50 / scale 0.9` ⇒ `0.10359`；
        //   Pragati `70 / 95 / 1` ⇒ `0.07368` ⇒ **CJK 面量出来比卡面实际大 1.406×**
        //   （反过来说卡面 = 探针读数 × 0.711）。用 CJK 面量出来的绝对数**不是卡面会渲出来的值**，
        //   拿它去定 `IconSetup.CalibScale` 会系统性偏大 ⇒ 2026-10-23 本轮改成与卡面同面。
        //   ⚠️ `CardView.KeywordFontSize`（本文件下面 `fsCard` 用的那个）本来就是**按面 em** 算的
        //   （`CardView.cs:3826` 一族的注释），所以只要字体面跟着卡面走，两边量纲就对齐了。
        tmp.font = CardPresentation.TmpFont.FaceAsset(CardPresentation.TmpFont.Face.LatinCardBody);
        if (tmp.font == null) { Log("!! 没有 TMP 字体（TmpFont.FaceAsset(LatinCardBody) == null）"); UnityEngine.Object.DestroyImmediate(go); return 0f; }
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
        float spriteH = 0f;
        for (int i = 0; i < ti.characterCount; i++)
        {
            var ci = ti.characterInfo[i];
            float h = InkHeight(ti, i, tmp.transform.lossyScale.y);
            float w = ci.xAdvance;
            // 🔴 2026-10-24（E9）：只认**真的那格 sprite**（名字写错 ⇒ TMP 不生成 sprite 字形 ⇒ 永远量不到 ⇒ 0）
            if (ci.elementType == TMP_TextElementType.Sprite && h > spriteH) spriteH = h;
            parts.Add(string.Format("#{0} '{1}' **inkH={2:F2}** w={3:F2}",
                                    i, ci.character, h, w));
        }
        Log("[" + tag + " @fontSize " + fontSize + "] " + string.Join(" | ", parts.ToArray()));

        // 真渲一张，人眼复核（断言测不出「画出来是个方块」）
        string png = Path.Combine(OutDir, tag + "_f" + fontSize.ToString("F0") + ".png");
        RenderToPng(tmp, png, 900, 260);
        UnityEngine.Object.DestroyImmediate(go);
        return spriteH;
    }

    /// <summary>
    /// 量第 i 个字的**墨迹高**（局部单位 → 乘 scale 得像素）。
    /// 顶点布局照 TMP：每个字符 4 个顶点（左下/左上/右上/右下），
    /// `characterInfo[i].vertexIndex` 是它的起点（`TMP_TextInfo.cs` 的 `vertexIndex`）。
    /// <para>🔴 **2026-10-16（A844）逐处判过：与 `ShellScene.TmpSpanPx` 是【两条口径】、有意不收** ——
    /// ① 本探针量的是**离树合成的**一个 TMP（`new GameObject` + `AddComponent&lt;TextMeshPro&gt;`，
    /// 只用来做字体标定），**不在**画布那棵树里 ⇒ 换算一律是「局部单位 × `lossyScale`」，
    /// **不是** `LayoutSpace.ToPixel`（那条把世界 → 画布 px，含 `×108` 与原点平移）。
    /// ② 粒度也不同：这里要**单个字**的墨迹高（比值「图标 ÷ 大写字高」是标定用的），
    /// 那一份给的是**一整段字**的外接框。⛔ 别为了「收口」把这里的单位换成画布 px ——
    /// 本探针那个 `new GameObject` 的 `lossyScale` = 1 ⇒ 换过去之后每个数会被乘 **108**，
    /// 而这个探针报出来的正是**和 `fontSize` 同一个量纲**的读数。</para>
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
