// Round1015Probe.cs — 「清空 A 表」第六轮（1015）的 Unity 腿探针
//
// 两个入口，都是一次性取事实、不改生产代码：
//
//   ① VAlignProbe.Run()  —— A712 前置项：量「我们的字墨落在哪」（WA712 §5·0）
//      为什么必须先量：WA712 §4·4 推出来「我们的行盒比原版高 1.6 倍、且不对称 ⇒
//      即使两边都 `Middle`，我们的字墨也比原版低 ≈0.13 F」，而它**是推导、不是实测**（§4·4 自己标了）。
//      若成立，「只加档位偏移」修完那 33% 仍然全错。所以先在真 Unity 里量一遍。
//      判据出处：`资料/普查产出_1013/WA712_垂直对齐普查.md` §4·4 / §4·5 / §5·0。
//
//   ② CrossCabProbe.Run() —— A231③ / A564：**多 CAB 产物在 Unity 里真加载过一次**
//      本件全部证据此前都是 UnityPy 格式级/库级的（`普查产出_1013/W230_跨CAB与repack.md` §六·1）。
//      最小验证 = `out/final_arena1.bundle` → `LoadFromFile` → `GetAllAssetNames()` 含不含 `Embers (2)`。
//
// 用法：
//   -executeMethod VAlignProbe.Run   -logFile -
//   -executeMethod CrossCabProbe.Run -logFile -
//   -executeMethod Round1015Probe.All -logFile -      （两个都跑，各自 try/catch）
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class Round1015Probe
{
    public static void All()
    {
        try { VAlignProbe.Run(); }
        catch (System.Exception e) { Debug.Log("R1015 !! VAlignProbe 抛异常: " + e); }
        try { CrossCabProbe.Run(); }
        catch (System.Exception e) { Debug.Log("R1015 !! CrossCabProbe 抛异常: " + e); }
    }
}

public static class VAlignProbe
{
    const string P = "VALIGN ";
    static readonly StringBuilder Sb = new StringBuilder();

    static void Log(string s) { Debug.Log(P + s); Sb.AppendLine(s); }

    /// <summary>把一个 TMP 的**字形四边形**（不是行盒）量出来，返回局部坐标下的 yMin/yMax。</summary>
    static bool InkBoxY(TMP_Text t, out float yMin, out float yMax, out float xMin, out float xMax)
    {
        yMin = float.MaxValue; yMax = float.MinValue;
        xMin = float.MaxValue; xMax = float.MinValue;
        var ti = t.textInfo;
        if (ti == null || ti.characterCount == 0) return false;
        for (int i = 0; i < ti.characterCount; i++)
        {
            var ci = ti.characterInfo[i];
            if (!ci.isVisible) continue;
            int vi = ci.vertexIndex;
            var verts = ti.meshInfo[ci.materialReferenceIndex].vertices;
            if (verts == null || vi + 3 >= verts.Length) continue;
            for (int k = 0; k < 4; k++)
            {
                var v = verts[vi + k];
                if (v.y < yMin) yMin = v.y;
                if (v.y > yMax) yMax = v.y;
                if (v.x < xMin) xMin = v.x;
                if (v.x > xMax) xMax = v.x;
            }
        }
        return yMax > yMin;
    }

    static TextMeshPro Make(string text, float fontSize, TextAlignmentOptions align, Vector2 box)
    {
        var go = new GameObject("valign_probe");
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = CardPresentation.TmpFont.Font;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.color = Color.white;
        tmp.alignment = align;
        tmp.fontSize = fontSize;
        tmp.rectTransform.sizeDelta = box;
        tmp.text = text;
        tmp.ForceMeshUpdate();
        return tmp;
    }

    public static void Run()
    {
        Log("==== A712 前置：量「我们的字墨落在哪」====");

        var font = CardPresentation.TmpFont.Font;
        if (font == null) { Log("!! TmpFont.Font == null ⇒ 量不了（字体资产不在）"); Dump(); return; }
        Log("字体资产名 = " + font.name);

        // ── 1) 字体级度量（字体的 faceInfo，单位 = 字体自己的 pointSize）─────────────
        var fi = font.faceInfo;
        float ps = fi.pointSize;
        Log(string.Format("faceInfo: pointSize={0:F2} ascentLine={1:F2} capLine={2:F2} descentLine={3:F2} baseline={4:F2} lineHeight={5:F2}",
                          ps, fi.ascentLine, fi.capLine, fi.descentLine, fi.baseline, fi.lineHeight));
        if (ps > 0f)
        {
            float lineBox = (fi.ascentLine - fi.descentLine) / ps;
            float baselineFromLineBoxCenter = ((fi.ascentLine + fi.descentLine) * 0.5f) / ps;
            float capH = fi.capLine / ps;
            // 大写字墨中心相对**行盒中心**的偏移（正 = 在中心之上）
            float inkCenterVsLineBox = (capH * 0.5f) - baselineFromLineBoxCenter;
            Log(string.Format("  ⇒ 行盒高/em = {0:F4}（WA712 §4·4 记的是 1.437）", lineBox));
            Log(string.Format("  ⇒ 行盒心→基线 = {0:F4} em（§4·4 记 0.4325）", baselineFromLineBoxCenter));
            Log(string.Format("  ⇒ 大写高/em = {0:F4}（§4·4 记 0.72）", capH));
            Log(string.Format("  ⇒ ★ 大写字墨中心 相对 行盒中心 = {0:F4} em（负 = 偏下；§4·4 推的是 −0.0725 em）",
                              inkCenterVsLineBox));

            // 原版两个字体的同一组数（值来自解包 JSON，⚠️ 不是本次实测 —— 只为当场对比）
            CmpOriginal("Pragati-Regular SDF", 95f, 70f, 60f, -20f);
            CmpOriginal("Asar-Regular SDF", 94f, 80f, 61f, -35f);
            Log(string.Format("  ⇒ ★ 我们的字墨中心比 Pragati 低 {0:F4} em、比 Asar 低 {1:F4} em（各除以自己 pointSize 后的比）",
                              inkCenterVsLineBox - OrigInkCenter(70f, 60f, -20f, 95f),
                              inkCenterVsLineBox - OrigInkCenter(80f, 61f, -35f, 94f)));
        }

        // ── 2) 实测：TMP 各纵向档下，字墨中心 vs 框心（框 50px 高，1 单位 = 108px）────
        float px = 108f;
        var box = new Vector2(400f / px, 50f / px);          // 400×50 px 的框（照 `Single Counter` 的框高）
        var aligns = new[]
        {
            // ⚠️ 本机 TMP 里「Middle」那个档叫 `Center`（= 水平 Center | 纵向 Middle，见 TMP_Text.cs:34）
            TextAlignmentOptions.Top, TextAlignmentOptions.Center, TextAlignmentOptions.Bottom,
            TextAlignmentOptions.Capline, TextAlignmentOptions.Midline
        };
        string[] strs = { "PLAY", "Counter", "Single Counter", "Points" };
        foreach (var s in strs)
        {
            foreach (float fs in new[] { 33f, 35f })
            {
                foreach (var a in aligns)
                {
                    var tmp = Make(s, fs, a, box);
                    float inkY0, inkY1, inkX0, inkX1;
                    bool ok = InkBoxY(tmp, out inkY0, out inkY1, out inkX0, out inkX1);
                    var tb = tmp.textBounds;
                    float sc = tmp.transform.lossyScale.y * px;   // 局部单位 → 画布像素
                    if (ok)
                    {
                        float inkC = (inkY0 + inkY1) * 0.5f * sc;
                        float boxC = tb.center.y * sc;
                        Log(string.Format("  [{0,-15} fs{1,-3:F0} {2,-8}] 行盒 y∈[{3:F2},{4:F2}]px(高{5:F2}) · 字墨 y∈[{6:F2},{7:F2}]px(高{8:F2}) · 字墨心−行盒心 = {9:F2}px = {10:F4}·fs",
                                          s, fs, a.ToString(),
                                          tb.min.y * sc, tb.max.y * sc, tb.size.y * sc,
                                          inkY0 * sc, inkY1 * sc, (inkY1 - inkY0) * sc,
                                          inkC - boxC, (inkC - boxC) / fs));
                    }
                    Object.DestroyImmediate(tmp.gameObject);
                }
            }
        }

        // ── 3) 生产路（`Label.Create` + 摆位）—— 确认「行盒居中」这套确实在跑 ────────
        var parent = new GameObject("valign_parent");
        var l = CardPresentation.Label.Create(parent.transform, "Counter", Vector3.zero, 1, Color.white, new Vector2(0.5f, 0.5f));
        var lt = l.GetComponentInChildren<TextMeshPro>();
        if (lt != null)
        {
            float inkY0, inkY1, inkX0, inkX1;
            bool ok = InkBoxY(lt, out inkY0, out inkY1, out inkX0, out inkX1);
            var tb = lt.textBounds;
            float sc = lt.transform.lossyScale.y * px;
            Log(string.Format("  [生产路 Label.Create 'Counter'] fontSize={0:F2} 行盒高={1:F2}px 字墨心−行盒心={2:F4}·fs（字形 {3}）",
                              lt.fontSize, tb.size.y * sc, ok ? ((inkY0 + inkY1) * 0.5f - tb.center.y) * sc / lt.fontSize : 0f,
                              ok ? "量到" : "没量到"));
        }
        else Log("  !! 生产路没拿到 TMP（退回点阵后端？）");
        Object.DestroyImmediate(parent);

        Dump();
    }

    static float OrigInkCenter(float ascent, float capLine, float descent, float pointSize)
    {
        // 行盒心 → 基线 = (ascent+descent)/2；字墨（大写）中心 = 基线 + capLine/2
        return ((capLine * 0.5f) - ((ascent + descent) * 0.5f)) / pointSize;
    }

    static void CmpOriginal(string name, float pointSize, float ascent, float capLine, float descent)
    {
        Log(string.Format("  （对照）{0}: 行盒/em={1:F4} 行盒心→基线={2:F4} 大写高/em={3:F4} ★字墨心−行盒心={4:F4} em",
                          name, (ascent - descent) / pointSize, ((ascent + descent) * 0.5f) / pointSize,
                          capLine / pointSize, OrigInkCenter(ascent, capLine, descent, pointSize)));
    }

    static void Dump()
    {
        try
        {
            System.IO.Directory.CreateDirectory(@"d:/4/_tmp_view");
            System.IO.File.WriteAllText(@"d:/4/_tmp_view/valign_probe.txt", Sb.ToString(), Encoding.UTF8);
            Debug.Log(P + "已写 d:/4/_tmp_view/valign_probe.txt");
        }
        catch (System.Exception e) { Debug.Log(P + "!! 写文件失败: " + e.Message); }
    }
}

public static class CrossCabProbe
{
    const string P = "XCAB ";
    const string BundlePath = @"d:/2/tools/w230_crosscab/out/final_arena1.bundle";

    public static void Run()
    {
        var sb = new StringBuilder();
        void Log(string s) { Debug.Log(P + s); sb.AppendLine(s); }

        Log("==== A231③：多 CAB 产物在 Unity 里真加载 ====");
        Log("包 = " + BundlePath);
        if (!System.IO.File.Exists(BundlePath)) { Log("!! 文件不存在 ⇒ 要先跑 d:/2/tools/w230_crosscab/ 那套脚本"); Dump(sb); return; }
        Log("大小 = " + new System.IO.FileInfo(BundlePath).Length + " B");

        var ab = AssetBundle.LoadFromFile(BundlePath);
        if (ab == null) { Log("🔴 LoadFromFile 返回 null ⇒ 这个包 Unity 根本不认（格式/版本不符）"); Dump(sb); return; }
        Log("✅ LoadFromFile 成功");

        var names = ab.GetAllAssetNames();
        Log("GetAllAssetNames() = " + names.Length + " 条");
        foreach (var n in names) Log("   · " + n);

        var want = "embers (2)";
        bool hit = false;
        foreach (var n in names) if (n.ToLowerInvariant().Contains(want)) { hit = true; Log("🔑 命中容器键: " + n); }
        Log(hit ? "✅ 容器表里的 `Embers (2)` 认得出来（m_FileID ≠ 0 那条 Unity 认）"
                : "🔴 没找到 `Embers (2)` ⇒ 跨 CAB 容器引用这条路 Unity 不认");

        var go = ab.LoadAsset<GameObject>("Embers (2)");
        Log("LoadAsset<GameObject>(\"Embers (2)\") = " + (go == null ? "null" : go.name));

        var all = ab.LoadAllAssets();
        Log("LoadAllAssets() = " + (all == null ? "null" : all.Length.ToString()) + " 件");

        ab.Unload(true);
        Dump(sb);
    }

    static void Dump(StringBuilder sb)
    {
        try
        {
            System.IO.Directory.CreateDirectory(@"d:/4/_tmp_view");
            System.IO.File.WriteAllText(@"d:/4/_tmp_view/crosscab_probe.txt", sb.ToString(), Encoding.UTF8);
        }
        catch (System.Exception e) { Debug.Log(P + "!! 写文件失败: " + e.Message); }
    }
}
