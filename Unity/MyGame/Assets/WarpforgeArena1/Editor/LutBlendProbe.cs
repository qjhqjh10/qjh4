// LutBlendProbe.cs — R1 `Hidden/LUTBlender` 的 **A/B 专验宿主**（2026-10-23 · W65 · A1 · R1）
//
// 为什么有它：`WarpforgeVFX/Shaders/WFLUTBlender.shader` 在 2026-10-23 补了 VS 的
// `uv * _LUT1_ST.xy + _LUT1_ST.zw`（原版 `mad o0.xy, v1.xyxx, cb0[2].xyxx, cb0[2].zwzz`），
// 而那条修复**只有真渲一次才验得了**：拿自建那份和 **bundle 里的原件** `Hidden/LUTBlender`
// （`StreamingAssets/WarpforgeVFX/wf_arena_2.bundle` · `Shader.path_id = 42`）逐像素比。
//
// 判据（唯一来源，本文件不另立口径）：
//   · `资料/普查产出_1023/E4_R29分因与R1判据.md` **§④-1**（骨架 + A1–A7 七条断言 + A/B 口径）
//   · `资料/普查产出_1023b/特效线_执行报告.md` **§一 / §8·1**（本轮已修内容；`cb0[2]` 查不到 ⇒ 取 `_LUT1_ST`）
//
// 🔴🔴 色彩空间（E4 §④-1 原话：「这一条是最容易假阳/假阴的地方，写进探针文件头」）：
//   工程是 **Linear**（`ProjectSettings.asset` 的 `m_ActiveColorSpace = 1`），14 张 LUT 都是
//   `sRGBTexture: 0`。所以**合成的 LUT 与 RT 全部按 linear 建**：
//     `new Texture2D(..., linear: true)` · `RenderTextureReadWrite.Linear` · 读回的那张也 linear
//   —— 否则 sRGB↔linear 转换会让我们拿一个「恒偏亮/偏暗」的假结论。
//
// 🔴 A/B 时设的是 **`_LUT1_ST`，不是 `_MainTex_ST`**（E4 原文那句 `_MainTex_ST` 已被报告 §8·1 订正）：
//   自建那份只吃 `_LUT1_ST`。而原件的 `cb0[2]` 到底是哪一张 `_ST` **查不到**（报告 §1·3：
//   `SerializedPass.m_CommonParameters = None`、`m_ConstantBuffers` 不序列化）
//   ⇒ 本探针对原件**逐张 ST 试**（`_LUT1_ST` / `_LUT2_ST` / 声明了的话 `_MainTex_ST`），
//     用「打了翻转之后输出是否真翻」**反推**它读的是哪一张。
//   ⚠️ 那一路是**附加诊断**（日志里带 `A7 诊断`），**不改 A1–A7 的判据**。
//
// 用法：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
//     -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod LutBlendProbe.Run -logFile -
//   筛日志：grep "^LP " <log>；图在 d:/4/_tmp_view/lutblend/
//   判读：`LP === RESULT R1 <PASS|FAIL> ===` 那一行。
//
// ⛔ 本探针**不建场景、不存盘、不改工程资产**（只 `Graphics.Blit` 到临时 RT + 写 _tmp_view 的 PNG）。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class LutBlendProbe
{
    const string P = "LP ";

    // 判据尺寸：LUT 是 256×16（E4 §④-1 第 0 步）。RT 与它同尺寸 ⇒ 输出可以和 LUT 逐像素 1:1 比。
    const int W = 256, H = 16;

    /// <summary>判据口径：差 ≤ **2 个字节级**（`maxAbs ≤ 2`）。</summary>
    const int TolBytes = 2;

    const string OutDir = "d:/4/_tmp_view/lutblend";
    const string OurAssetPath = "Assets/WarpforgeVFX/Shaders/WFLUTBlender.shader";
    const string OurShaderName = "WarpforgeVFX/LUTBlender";
    const string OrigShaderName = "Hidden/LUTBlender";
    const string BundleRel = "StreamingAssets/WarpforgeVFX/wf_arena_2.bundle";

    // 合成 LUT 的字节（行主序，索引 0 = **底行**）。
    // `Texture2D.SetPixels32` 与 `RenderTexture.active + ReadPixels` 都是底行原点 ⇒ 两边索引可直接对。
    static Color32[] _A, _B;
    static Texture2D _ta, _tb;
    static AssetBundle _bundle;

    static int _pass, _near, _fail;

    /// <summary>ST 设置指令：`Tex` = 纹理属性名（`_LUT1` / `_LUT2`），`V` = 要设的 ST。
    /// `Tex == "_MainTex_ST"` 是特例（那不是纹理属性，只能直写同名向量）。</summary>
    struct ST
    {
        public string Tex; public Vector4 V;
        public ST(string tex, Vector4 v) { Tex = tex; V = v; }
    }

    static readonly ST Unit = new ST(null, new Vector4(1f, 1f, 0f, 0f));
    static readonly Vector4 FlipST = new Vector4(1f, -1f, 0f, 1f);   // uv' = (u, 1-v) ⇒ 上下翻转

    struct Cmp
    {
        // 🔴 `ok = false` = 这一档**根本没取到像素**（case 抛异常 / 尺寸不符）。
        //   此时 `bad0`/`bad2` 必须置成**必红**的哨兵值 —— 否则结构体默认的 0 会让断言**静默变绿**
        //   （本件写完自查时发现的：`badPix == 0` 这种断言，在「图都没渲出来」时反而最容易过）。
        //   ⚠️ 这一条**没在 Unity 里实测过**（本轮不跑 Unity），是设计上先堵住的。
        public bool ok;
        public float maxAbs;    // 字节级（0..255）最大单通道差
        public int bad0;        // 有任一通道差 > 0 的像素数
        public int bad2;        // 有任一通道差 > 2 的像素数（判据口径）
        public float meanAbs;   // RGB 三通道平均绝对差（字节级）
        public float magenta;   // 近似 (255,0,255) 的像素占比 %
        public int n;
    }

    // ─────────────────────────────────────────────────────────────────────
    public static void Run()
    {
        _pass = _near = _fail = 0;
        Directory.CreateDirectory(OutDir);
        Debug.Log(P + "================= R1 `Hidden/LUTBlender` A/B 专验 =================");
        Debug.Log(P + "工程色彩空间 = " + QualitySettings.activeColorSpace
                  + "（探针内 LUT/RT 一律 linear 建 —— 见文件头；判据是 Linear）");

        try
        {
            BuildLuts();

            Shader ours = ResolveOurs();
            string origInfo;
            Shader orig = LoadOriginal(out origInfo);

            Debug.Log(P + "我们这份 = `" + OurShaderName + "`（" + (ours != null ? "✓ 取到" : "✗ 取不到") + "）");
            Debug.Log(P + "原件     = `" + OrigShaderName + "`（" + (orig != null ? "✓ 取到" : "✗ 取不到") + "） · " + origInfo);
            if (ours != null) LogShaderHealth("我们这份", ours);
            if (orig != null) LogShaderHealth("原件", orig);
            if (ours == null)
            {
                Debug.LogError(P + "✗ 自建 shader 取不到（`" + OurAssetPath + "`）—— 后面的判据全部作废");
                Debug.Log(P + "=== RESULT R1 FAIL（自建 shader 取不到）===");
                return;
            }

            // ================= ① 自建那份：A1–A5 + A7 =================
            Debug.Log(P + "── ① 自建 `" + OurShaderName + "` ──");
            var oursRes = new Dictionary<string, Color32[]>();
            oursRes["b0"]    = Blit("ours_b000", ours, _ta, _tb, 0.00f, Unit);
            oursRes["b025"]  = Blit("ours_b025", ours, _ta, _tb, 0.25f, Unit);
            oursRes["b05"]   = Blit("ours_b050", ours, _ta, _tb, 0.50f, Unit);
            oursRes["b1"]    = Blit("ours_b100", ours, _ta, _tb, 1.00f, Unit);
            oursRes["same0"] = Blit("ours_same_b000", ours, _ta, _ta, 0.00f, Unit);
            oursRes["same05"]= Blit("ours_same_b050", ours, _ta, _ta, 0.50f, Unit);
            oursRes["same1"] = Blit("ours_same_b100", ours, _ta, _ta, 1.00f, Unit);
            oursRes["stflip"]= Blit("ours_stflip_lut1_b000", ours, _ta, _tb, 0.00f,
                                    new ST("_LUT1", FlipST));

            Cmp cO0  = Report("ours/b0    vs LUT1", oursRes["b0"], _A);
            Cmp cO1  = Report("ours/b1    vs LUT2", oursRes["b1"], _B);
            Cmp cO50 = Report("ours/b0.5  vs lerp(A,B,0.5)", oursRes["b05"], Fade(_A, _B, 0.5f));
            Cmp cO25 = Report("ours/b0.25 vs lerp(A,B,0.25)", oursRes["b025"], Fade(_A, _B, 0.25f));
            Cmp cS0  = Report("ours/LUT1=LUT2=X @b0", oursRes["same0"], _A);
            Cmp cS50 = Report("ours/LUT1=LUT2=X @b0.5", oursRes["same05"], _A);
            Cmp cS1  = Report("ours/LUT1=LUT2=X @b1", oursRes["same1"], _A);

            // A7-①：自建的 VS ST 那一跳 —— 打翻转 ST，输出必须真的上下翻（且**不能**和未翻转版相同 ⇒ 灭自证）
            Cmp cA7flip = Compare(oursRes["stflip"], FlipY(_A));
            Cmp cA7same = Compare(oursRes["stflip"], _A);
            Debug.Log(P + "  [ours/ST=_LUT1(1,-1,0,1)] vs 上下翻转版 " + F(cA7flip)
                      + " · vs 未翻转版 " + F(cA7same));

            // ================= ② 原件：A6（+ A5 复核）+ A7 诊断 =================
            var origRes = new Dictionary<string, Color32[]>();
            if (orig != null)
            {
                Debug.Log(P + "── ② 原件 `" + OrigShaderName + "`（同一个 `Graphics.Blit(null→rt)`）──");
                origRes["b0"]    = Blit("orig_b000", orig, _ta, _tb, 0.00f, Unit);
                origRes["b025"]  = Blit("orig_b025", orig, _ta, _tb, 0.25f, Unit);
                origRes["b05"]   = Blit("orig_b050", orig, _ta, _tb, 0.50f, Unit);
                origRes["b1"]    = Blit("orig_b100", orig, _ta, _tb, 1.00f, Unit);
                origRes["same05"]= Blit("orig_same_b050", orig, _ta, _ta, 0.50f, Unit);
                origRes["st_lut1"] = Blit("orig_stflip_lut1_b000", orig, _ta, _tb, 0.00f, new ST("_LUT1", FlipST));
                origRes["st_lut2"] = Blit("orig_stflip_lut2_b000", orig, _ta, _tb, 0.00f, new ST("_LUT2", FlipST));
                origRes["st_main"] = Blit("orig_stflip_main_b000", orig, _ta, _tb, 0.00f, new ST("_MainTex_ST", FlipST));
            }

            Cmp cR0 = default(Cmp), cR1 = default(Cmp), cR50 = default(Cmp), cR25 = default(Cmp);
            if (orig != null)
            {
                cR0   = Report("orig/b0    vs LUT1", origRes["b0"], _A);
                cR1   = Report("orig/b1    vs LUT2", origRes["b1"], _B);
                cR50  = Report("orig/b0.5  vs lerp(A,B,0.5)", origRes["b05"], Fade(_A, _B, 0.5f));
                cR25  = Report("orig/b0.25 vs lerp(A,B,0.25)", origRes["b025"], Fade(_A, _B, 0.25f));
                Report("orig/LUT1=LUT2=X @b0.5", origRes["same05"], _A);

                // 原件 vs 我们这份（同参数逐像素）—— 平台层面的 Blit 翻转/精度差也会在这里现形
                CrossReport("orig/b0   ↔ ours/b0  ", origRes["b0"], oursRes["b0"]);
                CrossReport("orig/b0.5 ↔ ours/b0.5", origRes["b05"], oursRes["b05"]);
                CrossReport("orig/b1   ↔ ours/b1  ", origRes["b1"], oursRes["b1"]);
            }

            // ================= ③ 七条断言 =================
            Debug.Log(P + "================= 断言（E4 §④-1 的 A1–A7）=================");

            Judge("A1", "_Blend=0 ⇒ 输出与 LUT1 **逐字节相同**（badPix=0）",
                  cO0.bad0 == 0, cO0.bad2 == 0, F(cO0));

            Judge("A2", "_Blend=1 ⇒ 输出与 LUT2 **逐字节相同**（灭自证：挡「恒返回 LUT1」）",
                  cO1.bad0 == 0, cO1.bad2 == 0, F(cO1));

            Judge("A3", "_Blend=0.5 ⇒ 与 (A+B)/2 每个通道 ≤ 2/255（挡「端点对、中间错」）",
                  cO50.bad2 == 0, cO50.maxAbs <= 4f, F(cO50));

            Judge("A4", "_Blend=0.25 ⇒ 与 lerp(A,B,0.25) ≤ 2/255（挡「把 _Blend 当 0/1 开关」）",
                  cO25.bad2 == 0, cO25.maxAbs <= 4f, F(cO25));

            bool sameOk = cS0.bad2 == 0 && cS50.bad2 == 0 && cS1.bad2 == 0;
            float sameWorst = Mathf.Max(Mathf.Max(cS0.maxAbs, cS50.maxAbs), cS1.maxAbs);
            Judge("A5", "_LUT1 = _LUT2 = X ⇒ 输出 == X（任意 _Blend；挡「没采纹理、自己发光」）",
                  sameOk, sameWorst <= 4f,
                  "b0 " + F(cS0) + " · b0.5 " + F(cS50) + " · b1 " + F(cS1));

            // ---- A6：原件那一路（两条可接受结局，见 E4 §④-1）----
            if (orig == null)
            {
                Judge("A6", "原件 A/B（`maxAbs≤2` ⇒ 可进 UseOriginal；整片品红 ⇒ 替换理由成立）",
                      false, false, "原件**取不到** ⇒ 判不了（见上面那行 `<原件> ✗`）");
            }
            else
            {
                float worst = Mathf.Max(Mathf.Max(cR0.maxAbs, cR1.maxAbs), Mathf.Max(cR50.maxAbs, cR25.maxAbs));
                float mag = Mathf.Max(Mathf.Max(cR0.magenta, cR1.magenta), Mathf.Max(cR50.magenta, cR25.magenta));
                if (!cR0.ok && !cR1.ok && !cR50.ok && !cR25.ok)
                    Judge("A6", "原件 A/B（`maxAbs≤2` ⇒ 可进 UseOriginal；整片品红 ⇒ 替换理由成立）",
                          false, false, "原件四档**一档都没跑出来**（见上面的 `case … 抛异常`）⇒ 判不了");
                else if (mag >= 50f)
                    Judge("A6", "原件 A/B（`maxAbs≤2` ⇒ 可进 UseOriginal；整片品红 ⇒ 替换理由成立）",
                          true, false, "原件**整片品红**（magenta " + mag.ToString("F1") + "% · maxAbs 最差 "
                          + worst.ToString("F1") + "）⇒ `WarpforgeShaderMap` 那条「批处理下不可渲染」的理由**成立**，保持自建；"
                          + "⛔ 这不是失败，是第二判据");
                else if (worst <= TolBytes)
                    Judge("A6", "原件 A/B（`maxAbs≤2` ⇒ 可进 UseOriginal；整片品红 ⇒ 替换理由成立）",
                          true, false, "原件四档 maxAbs 最差 " + worst.ToString("F2") + " ≤ 2 ⇒ **可进 `UseOriginal`**"
                          + "（改不改由主对话定；本探针只量）");
                else
                    Judge("A6", "原件 A/B（`maxAbs≤2` ⇒ 可进 UseOriginal；整片品红 ⇒ 替换理由成立）",
                          false, false, "原件可渲染但与期望**不符**（maxAbs 最差 " + worst.ToString("F1")
                          + " · magenta " + mag.ToString("F1") + "%）⇒ ⛔ **不能进 `UseOriginal`**，先看下面的翻转诊断");
            }

            // ---- A7：VS 的 ST 那一跳 ----
            bool a7ours = cA7flip.bad2 == 0 && cA7same.maxAbs > 8f;
            string origA7 = "原件取不到，无法反推";
            if (orig != null)
            {
                origA7 = DiagnoseOrigST(origRes);
            }
            Judge("A7", "VS 的 ST 那一跳：`_LUT1_ST=(1,-1,0,1)` ⇒ 输出应为 A 的**上下翻转版**",
                  a7ours, cA7flip.maxAbs <= 4f,
                  "我们这份：vs 翻转版 " + F(cA7flip) + " · vs 未翻转版 maxAbs=" + cA7same.maxAbs.ToString("F2")
                  + "（未翻转版必须差得远，挡「压根没读 ST」）· " + origA7);

            // ================= ④ 汇总 =================
            bool r1 = cO0.bad2 == 0 && cO1.bad2 == 0 && cO50.bad2 == 0 && cO25.bad2 == 0
                      && sameOk && a7ours;
            Debug.Log(P + "=== " + _pass + " 过 / " + _near + " 近似 / " + _fail + " 红 ===");
            Debug.Log(P + "=== RESULT R1 " + (r1 ? "PASS" : "FAIL") + " ==="
                      + (r1 ? "（自建那份 A1–A5 + A7 全绿 ⇒ 本轮 VS 补的 `_LUT1_ST` 那一跳成立）"
                            : "（见上面逐条 ✗/≈）"));
        }
        catch (Exception e)
        {
            Debug.LogError(P + "✗ 探针抛异常：" + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace);
            Debug.Log(P + "=== RESULT R1 FAIL（抛异常）===");
        }
        finally
        {
            if (_ta != null) { UnityEngine.Object.DestroyImmediate(_ta); _ta = null; }
            if (_tb != null) { UnityEngine.Object.DestroyImmediate(_tb); _tb = null; }
            if (_bundle != null) { _bundle.Unload(false); _bundle = null; }
        }
    }

    // ───────────────────────── shader 取件 ─────────────────────────
    static Shader ResolveOurs()
    {
        var sh = AssetDatabase.LoadAssetAtPath<Shader>(OurAssetPath);
        if (sh == null) sh = Shader.Find(OurShaderName);
        if (sh == null) Debug.LogError(P + "✗ `" + OurAssetPath + "` 与 `Shader.Find(\"" + OurShaderName + "\")` 都取不到");
        return sh;
    }

    /// <summary>从**我们自己重打的那份包**里取原件。
    /// ⚠️ 走 `LoadAllAssets&lt;Shader&gt;()` 而不是 `LoadAsset&lt;Shader&gt;(名字)` ——
    ///   后者认的是**容器键**（本仓踩过：裸名恒 null，见 `ArenaAnimsProbe` 的注释）。
    /// </summary>
    static Shader LoadOriginal(out string info)
    {
        string path = Path.Combine(Application.dataPath, BundleRel);
        info = "包 = " + path + "（存在=" + File.Exists(path) + "）";
        if (!File.Exists(path)) return null;

        AssetBundle b = null;
        try { b = AssetBundle.LoadFromFile(path); }
        catch (Exception e) { info += " · LoadFromFile 抛 " + e.GetType().Name; }
        if (b == null)
        {
            // 同一份内容可能已被别的宿主加载过（`LoadFromFile` 会返回 null）
            foreach (var ab in AssetBundle.GetAllLoadedAssetBundles())
                if (ab != null && ab.name == "wf_arena_2") { b = ab; info += " · LoadFromFile 为 null，改用已加载实例"; break; }
        }
        if (b == null) { info += " · ✗ 包载不进来"; return null; }
        _bundle = b;

        Shader[] all = null;
        try { all = b.LoadAllAssets<Shader>(); }
        catch (Exception e) { info += " · LoadAllAssets<Shader> 抛 " + e.GetType().Name; }

        var names = new List<string>();
        if (all != null)
            foreach (var s in all) names.Add(s == null ? "<null>" : ("`" + s.name + "`"));
        info += " · 包内 shader " + (all == null ? "?" : all.Length.ToString()) + " 个："
                + (names.Count == 0 ? "（无）" : string.Join(" / ", names.ToArray()));

        Shader found = null;
        if (all != null)
            foreach (var s in all)
                if (s != null && s.name == OrigShaderName) { found = s; break; }
        if (found == null && all != null)
            foreach (var s in all)
                if (s != null && !string.IsNullOrEmpty(s.name) && s.name.EndsWith("LUTBlender"))
                { found = s; info += " · ⚠ 按名字后缀匹配到"; break; }
        if (found == null && all != null && all.Length == 1 && all[0] != null)
        { found = all[0]; info += " · ⚠ 包内只有 1 个 shader，按唯一性取"; }
        if (found == null)
        {
            foreach (var k in b.GetAllAssetNames())
            {
                if (k == null || k.ToLowerInvariant().IndexOf("lutblender") < 0) continue;
                try { var s = b.LoadAsset<Shader>(k); if (s != null) { found = s; info += " · 按容器键 `" + k + "` 取到"; break; } }
                catch { }
            }
        }
        return found;
    }

    static void LogShaderHealth(string label, Shader sh)
    {
        string msg;
        try
        {
            var msgs = ShaderUtil.GetShaderMessages(sh);
            msg = msgs == null || msgs.Length == 0 ? "0 条编译消息" : (msgs.Length + " 条编译消息");
            if (msgs != null)
                foreach (var m in msgs)
                    Debug.LogWarning(P + "  [shader 编译] " + label + " · " + m.platform + " · " + m.message);
        }
        catch (Exception e) { msg = "编译消息读不出（" + e.GetType().Name + "）"; }
        Debug.Log(P + "  " + label + "：isSupported=" + sh.isSupported + " · " + msg + " · pass 数=" + sh.passCount);
    }

    // ───────────────────────── LUT ─────────────────────────
    static void BuildLuts()
    {
        _A = new Color32[W * H];
        _B = new Color32[W * H];
        for (int y = 0; y < H; y++)
        {
            byte ay = (byte)Mathf.RoundToInt(255f * y / (H - 1));   // = y/15
            for (int x = 0; x < W; x++)
            {
                byte ax = (byte)x;                                 // = x/255（x ∈ 0..255）
                _A[y * W + x] = new Color32(ax, ay, 0, 255);
                _B[y * W + x] = new Color32((byte)(255 - ax), (byte)(255 - ay), 128, 255);
            }
        }
        _ta = Upload("LUT_A", _A);
        _tb = Upload("LUT_B", _B);
        Debug.Log(P + "合成 LUT（256×16 · linear · mip 关 · Bilinear · Clamp）："
                  + "A(x,y)=(x/255, y/15, 0, 1) · B(x,y)=(1-x/255, 1-y/15, 0.5, 1)");
        Debug.Log(P + "  ⚠️ 这样「恒返回 A」「恒返回 B」「恒返回平均」都会在某一档露馅（灭自证）");
    }

    static Texture2D Upload(string name, Color32[] px)
    {
        // ⚠️ `linear: true` 是这条链的命门（见文件头）。
        var t = new Texture2D(W, H, TextureFormat.RGBA32, false, true);
        t.name = name;
        t.filterMode = FilterMode.Bilinear;
        t.wrapMode = TextureWrapMode.Clamp;
        t.SetPixels32(px);
        t.Apply(false, false);
        return t;
    }

    // ───────────────────────── 一次 blit ─────────────────────────
    /// <summary>照 `BattlePostFx.BlendTo` 那条真实路径：`SetFloat("_Blend") + Graphics.Blit(null → rt, mat)`。</summary>
    static Color32[] Blit(string name, Shader sh, Texture lut1, Texture lut2, float blend, ST st)
    {
        Material mat = null;
        RenderTexture rt = null;
        Texture2D read = null;
        Color32[] got = null;
        try
        {
            mat = new Material(sh);
            mat.hideFlags = HideFlags.DontSave;
            mat.SetTexture("_LUT1", lut1);
            mat.SetTexture("_LUT2", lut2);
            mat.SetFloat("_Blend", blend);

            // 先把两张纹理的 ST 都复位成单位（挡残留的缩放/平移）
            ApplyST(mat, "_LUT1", new Vector4(1f, 1f, 0f, 0f));
            ApplyST(mat, "_LUT2", new Vector4(1f, 1f, 0f, 0f));
            if (st.Tex != null)
            {
                if (st.Tex == "_MainTex_ST") SetVec(mat, "_MainTex_ST", st.V);
                else ApplyST(mat, st.Tex, st.V);
                // 回读：确认 ST 真的设进去了 —— 否则 A7 的「没反应」会是个假象（量具自己没生效）
                string back;
                if (st.Tex == "_MainTex_ST")
                    back = mat.HasProperty("_MainTex_ST") ? mat.GetVector("_MainTex_ST").ToString() : "（该 shader 没声明它）";
                else
                    back = "scale=" + mat.GetTextureScale(st.Tex) + " offset=" + mat.GetTextureOffset(st.Tex);
                Debug.Log(P + "  · ST 回读 [" + name + "] " + st.Tex + " ← (1,-1,0,1) ⇒ " + back);
            }

            rt = RenderTexture.GetTemporary(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(null, rt, mat);

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            read = new Texture2D(W, H, TextureFormat.RGBA32, false, true);   // 读回也要 linear，否则会被转一次
            read.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            read.Apply(false, false);
            RenderTexture.active = prev;
            got = read.GetPixels32();
        }
        catch (Exception e)
        {
            Debug.LogError(P + "✗ case `" + name + "` 抛异常：" + e.GetType().Name + ": " + e.Message);
        }
        finally
        {
            if (read != null) UnityEngine.Object.DestroyImmediate(read);
            if (rt != null)
            {
                if (RenderTexture.active == rt) RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
                rt = null;
            }
            if (mat != null) UnityEngine.Object.DestroyImmediate(mat);
        }
        if (got != null)
        {
            SavePng(got, name + ".png", 1);
            SavePng(got, name + "_x4.png", 4);
        }
        return got;
    }

    /// <summary>设置某张纹理的 ST。
    /// **权威路径 = 纹理的 scale/offset** —— Unity 会把它们写进 `<tex>_ST`（内置管线与 URP 同一条；
    /// 我们那份把 `float4 _LUT1_ST;` 声明在 `UnityPerMaterial` 里，灌的也是这里）。</summary>
    static void ApplyST(Material mat, string texProp, Vector4 st)
    {
        try
        {
            // ① 权威路径：纹理的 scale/offset —— Unity 上传材质时会把它们写进 `<tex>_ST`
            mat.SetTextureScale(texProp, new Vector2(st.x, st.y));
            mat.SetTextureOffset(texProp, new Vector2(st.z, st.w));
            // ② 再直写一次同名向量（`<tex>_ST` 若被 shader 当普通向量属性声明就吃这一条；
            //    没声明时 `HasProperty` 为 false ⇒ 跳过，不报错）
            SetVec(mat, texProp + "_ST", st, true);
        }
        catch (Exception e) { Debug.LogWarning(P + "  · ApplyST(" + texProp + ") 失败：" + e.GetType().Name); }
    }

    /// <summary>直写同名向量（只给 `_MainTex_ST` 那条诊断用；属性不存在就跳过、不出声地失败）。
    /// ⚠️ 内置管线的 `_MainTex_ST` 是**自动灌**的，直写能不能盖住由 Unity 决定 ⇒ 只当参考诊断。</summary>
    static void SetVec(Material mat, string prop, Vector4 v, bool quiet = false)
    {
        bool has;
        try { has = mat.HasProperty(prop); } catch { has = false; }
        if (!has)
        {
            if (!quiet)
                Debug.Log(P + "  · `" + prop + "` 不是这份 shader 声明的属性 ⇒ 跳过直写"
                          + "（`Graphics.Blit` 可能在内部自己绑它 —— 看输出的翻转诊断）");
            return;
        }
        mat.SetVector(prop, v);
    }

    // ───────────────────────── 比较 ─────────────────────────
    static Cmp Compare(Color32[] got, Color32[] want)
    {
        var c = new Cmp();
        if (got == null || want == null || got.Length != want.Length || got.Length == 0)
        {
            c.ok = false;
            c.maxAbs = 999f; c.bad0 = int.MaxValue; c.bad2 = int.MaxValue;
            c.meanAbs = -1f; c.magenta = -1f; c.n = 0;
            return c;
        }
        c.ok = true;
        c.n = got.Length;
        double sum = 0; int mag = 0;
        for (int i = 0; i < got.Length; i++)
        {
            int dr = Mathf.Abs(got[i].r - want[i].r);
            int dg = Mathf.Abs(got[i].g - want[i].g);
            int db = Mathf.Abs(got[i].b - want[i].b);
            int da = Mathf.Abs(got[i].a - want[i].a);
            int m = Mathf.Max(Mathf.Max(dr, dg), Mathf.Max(db, da));
            sum += (dr + dg + db) / 3.0;
            if (m > c.maxAbs) c.maxAbs = m;
            if (m > 0) c.bad0++;
            if (m > TolBytes) c.bad2++;
            var g = got[i];
            if (g.r >= 250 && g.g <= 5 && g.b >= 250) mag++;   // 近似 (255,0,255)
        }
        c.meanAbs = (float)(sum / Math.Max(1, got.Length));
        c.magenta = 100f * mag / Math.Max(1, got.Length);
        return c;
    }

    static Cmp Report(string label, Color32[] got, Color32[] want)
    {
        var c = Compare(got, want);
        var f = Compare(got, FlipY(want));
        string extra = (f.ok && c.ok && f.maxAbs < c.maxAbs)
            ? " · ⚠ 更像「上下翻转版」（flipY maxAbs=" + f.maxAbs.ToString("F2") + "）" : "";
        Debug.Log(P + "  [" + label + "] " + F(c) + extra);
        return c;
    }

    static void CrossReport(string label, Color32[] origPx, Color32[] oursPx)
    {
        var c = Compare(origPx, oursPx);
        Debug.Log(P + "  [" + label + " 原件↔我们] " + F(c));
    }

    static string F(Cmp c)
    {
        if (!c.ok) return "（取不到/尺寸不符）";
        return "maxAbs=" + c.maxAbs.ToString("F2") + " badPix0=" + c.bad0 + " badPix2=" + c.bad2
             + " meanAbs=" + c.meanAbs.ToString("F3") + " magenta=" + c.magenta.ToString("F1") + "%";
    }

    /// <summary>A7 的附加诊断（**不改判据**）：对原件逐张 ST 打翻转，看哪一张能让它真翻 ⇒ 反推 `cb0[2]` 是谁。</summary>
    static string DiagnoseOrigST(Dictionary<string, Color32[]> r)
    {
        string[] keys = { "st_lut1", "st_lut2", "st_main" };
        string[] names = { "_LUT1_ST", "_LUT2_ST", "_MainTex_ST" };
        var hit = new List<string>();
        for (int i = 0; i < keys.Length; i++)
        {
            Color32[] px;
            if (!r.TryGetValue(keys[i], out px) || px == null) { Debug.Log(P + "  A7 诊断 · 原件 + " + names[i] + "：这条没跑出来"); continue; }
            var cf = Compare(px, FlipY(_A));
            var cs = Compare(px, _A);
            bool flipped = cf.bad2 == 0;
            bool unchanged = cs.bad2 == 0;
            Debug.Log(P + "  A7 诊断 · 原件 + " + names[i] + "(1,-1,0,1)：vs 翻转版 maxAbs=" + cf.maxAbs.ToString("F2")
                      + " · vs 原版 maxAbs=" + cs.maxAbs.ToString("F2")
                      + " ⇒ " + (flipped ? "**翻了**（它读这张）" : (unchanged ? "没反应（不是这张）" : "既不是翻转版也不是原版")));
            if (flipped) hit.Add(names[i]);
        }
        if (hit.Count == 1)
            return "A7 诊断：原件真正读的是 **" + hit[0] + "** ⇒ 自建那份应改用同一张（报告 §1·3 那条「查不到」由此关闭）";
        if (hit.Count == 0)
            return "A7 诊断：三张 ST **都不响应** ⇒ 原件的 `cb0[2]` 不是这三张（可能由 `Graphics.Blit` 内部绑死）";
        return "A7 诊断：**多张**都能翻（" + string.Join(" / ", hit.ToArray()) + "）⇒ 说明它们在这份 shader 里是同一格";
    }

    // ───────────────────────── 其它 ─────────────────────────
    static void Judge(string id, string what, bool pass, bool near, string detail)
    {
        string mark;
        if (pass) { _pass++; mark = "✓"; }
        else if (near) { _near++; mark = "≈"; }
        else { _fail++; mark = "✗"; }
        Debug.Log(P + mark + " " + id + " " + what + " —— " + detail);
    }

    static Color32[] FlipY(Color32[] src)
    {
        if (src == null) return null;
        var o = new Color32[src.Length];
        for (int y = 0; y < H; y++)
            Array.Copy(src, (H - 1 - y) * W, o, y * W, W);
        return o;
    }

    /// <summary>CPU 端 `lerp(a,b,t)`（字节级；与 shader 里 `lerp(归一化 a, 归一化 b, t)` 同值）。</summary>
    static Color32[] Fade(Color32[] a, Color32[] b, float t)
    {
        var o = new Color32[a.Length];
        for (int i = 0; i < a.Length; i++)
        {
            o[i] = new Color32(
                Q(a[i].r + (b[i].r - a[i].r) * t),
                Q(a[i].g + (b[i].g - a[i].g) * t),
                Q(a[i].b + (b[i].b - a[i].b) * t),
                Q(a[i].a + (b[i].a - a[i].a) * t));
        }
        return o;
    }

    static byte Q(float v) { return (byte)Mathf.Clamp(Mathf.RoundToInt(v), 0, 255); }

    static void SavePng(Color32[] px, string name, int scale)
    {
        int w = W * scale, h = H * scale;
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        if (scale == 1)
        {
            t.SetPixels32(px);
        }
        else
        {
            var o = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    o[y * w + x] = px[(y / scale) * W + (x / scale)];
            t.SetPixels32(o);
        }
        t.Apply(false, false);
        try { File.WriteAllBytes(Path.Combine(OutDir, name), t.EncodeToPNG()); }
        catch (Exception e) { Debug.LogWarning(P + "  · 写 " + name + " 失败：" + e.GetType().Name); }
        UnityEngine.Object.DestroyImmediate(t);
    }
}
