// TmpSetup.cs — 命令行把 TextMeshPro 装进这台工程（不用手点菜单）
//
// 背景：Unity 6 把 TMP 的**代码**并进了 `com.unity.ugui`（所以 `using TMPro;` 一直能编译），
// 但 **TMP Settings / 着色器 / 默认字体** 这些**资产**不会自动进 Assets —— 官方要你打开
// `Window > TextMeshPro > Import TMP Essential Resources` 点一下。命令行等价物就是
// `AssetDatabase.ImportPackage(path, interactive:false)`。
// **之前记的「TMP 只能手点菜单」是错的**，那条作废。
//
// Essentials 包在 UGUI 包里：`Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage`
// （Unity 6 把 TMP 并进 UGUI 了）。包**没有 .cs** —— 实测 45 条全是资产，所以导入不会触发
// 脚本重编译、批处理下不会被中断。落地清单：
//   Assets/TextMesh Pro/Resources/TMP Settings.asset                      ← 全局设置，fallback 链在这儿配
//   Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset
//   Assets/TextMesh Pro/Shaders/*.shader（含 URP 用的 `TMP_SDF-URP Unlit.shadergraph`）
//
// 用法（本机带 ELECTRON_RUN_AS_NODE，启动 Unity 前必须 unset，见 CLAUDE.md）：
//   -executeMethod TmpSetup.ImportEssentials    导 TMP Essentials（只需跑一次）
//   -executeMethod TmpSetup.CopyCjkFont         把原版那份 NotoSerifCJK 拷进工程
//   -executeMethod TmpSetup.BuildCjkFontAsset   从 TTF 生成 Dynamic 的 CJK TMP_FontAsset
//   -executeMethod TmpSetup.All                 上面三件一起做
//   -executeMethod TmpSetup.Verify              检查成果（含「真渲一张图出来」）
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class TmpSetup
{
    const string P = "TMPSETUP ";
    const string OutDir = @"d:/4/_tmp_view/tmp";

    /// <summary>Essentials 导进来之后应该出现的那个文件 —— 有它就说明 TMP 资产齐了</summary>
    const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
    const string EssentialsName = "TMP Essential Resources.unitypackage";

    /// <summary>原版那套中文字体。本机解包资源里就有（23.6 MB、完整 CJK）</summary>
    const string SrcTtf = @"d:/2/解包整理/10_字体/fonts/NotoSerifCJK-Regular.ttf";
    /// <summary>源 TTF：**不**放 Resources —— 它只给建字体资产用，不必进包（Dynamic 模式靠字体资产引着它）</summary>
    const string TtfPath = "Assets/CardPresentation/Fonts/NotoSerifCJK-Regular.ttf";
    /// <summary>字体资产：**必须放 Resources 下**，`CardPresentation.TmpFont` 靠 `Resources.Load` 拿它</summary>
    const string FaPath = "Assets/CardPresentation/Resources/Fonts/NotoSerifCJK-Regular SDF.asset";

    // ── 建字体资产的参数**照抄原版**。出处：解包资产
    //    `10_字体/字体资源/MonoBehaviour/NotoSerifCJK-Regular SDF.json`
    //      m_FaceInfo.m_PointSize        = 50
    //      m_AtlasPadding                = 5
    //      m_AtlasWidth / m_AtlasHeight  = 2048 / 2048
    //      m_AtlasRenderMode             = 4165 → GlyphRenderMode.SDFAA
    //            （在本地解包的 `UnityEngine.TextCoreFontEngineModule/UnityEngine/TextCore/LowLevel/GlyphRenderMode.cs:20`
    //              核过：`SDFAA = 4165`。整个 10_字体 里只有它和 RobotoCondensed 用 SDFAA）
    //      m_AtlasPopulationMode         = 1 → Dynamic
    //      m_IsMultiAtlasTexturesEnabled = 1
    //    ⚠️ 原版那份的 m_GlyphTable / m_CharacterTable 都是 **0 条**、图集 PNG 只有 1×1 ——
    //       原版是**运行时现栅格化**的，**拿不到烘好的汉字位图**，只能自己从 TTF 生成。
    //       所以「像原版」= 同样的参数 + 同样的 Dynamic 模式，不是同一张图集。
    const int PointSize = 50;
    const int Padding = 5;
    const int AtlasW = 2048;
    const int AtlasH = 2048;
    static readonly GlyphRenderMode RenderMode = GlyphRenderMode.SDFAA;

    // ─────────────────────────── 入口 ───────────────────────────

    public static void All()
    {
        ImportEssentials();
        CopyCjkFont();
        BuildCjkFontAsset();
        Verify();
    }

    /// <summary>
    /// 导 TMP Essential Resources.unitypackage。
    ///
    /// ⚠️ **不能用 `AssetDatabase.ImportPackage()`** —— 2026-09-12 实测：它只是把导入**排进队列**，
    /// `-batchmode -quit` 在队列被处理之前就退出了，**一个文件都不会落地**（日志里连报错都没有，
    /// 只是导入后 `TMP Settings.asset` 不存在）。所以这里自己解。
    /// </summary>
    public static void ImportEssentials()
    {
        string pkg = FindEssentials();
        if (pkg == null)
        {
            Fail("找不到 " + EssentialsName + "。找过 `Packages/com.unity.ugui/package.json` 的 resolvedPath " +
                 "和 <项目>/Library/PackageCache/com.unity.ugui@*/Package Resources/ —— 两边都没有。" +
                 "TMP 的资产（Settings/着色器/默认字体）进不了工程。");
            return;
        }

        string root = Path.GetDirectoryName(Application.dataPath);
        Debug.Log(P + "解包 " + pkg);
        int n = ExtractUnityPackage(pkg, root);
        AssetDatabase.Refresh();
        Debug.Log(P + "落地 " + n + " 个文件");

        if (!File.Exists(Path.Combine(root, TmpSettingsPath)))
        {
            Fail("解包后 " + TmpSettingsPath + " 仍不存在 —— 解包逻辑不对，去看上面的落地清单。");
            return;
        }

        var s = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        Debug.Log(P + "OK " + TmpSettingsPath + " 已就位（TMP_Settings 载入=" + (s != null) + "）");
        // 顺手报一下资源里带的默认字体，后面 Label 兜底要用（这两个都是**静态**属性）
        Debug.Log(P + "默认字体资产 = " + (TMP_Settings.defaultFontAsset == null
                  ? "<null>" : TMP_Settings.defaultFontAsset.name));
    }

    /// <summary>把原版那份 NotoSerifCJK-Regular.ttf 拷进工程</summary>
    public static void CopyCjkFont()
    {
        if (!File.Exists(SrcTtf))
        {
            Fail("源字体不在：" + SrcTtf);
            return;
        }

        string abs = Path.Combine(Application.dataPath, "CardPresentation/Fonts");
        Directory.CreateDirectory(abs);
        string dst = Path.Combine(abs, "NotoSerifCJK-Regular.ttf");

        var srcInfo = new FileInfo(SrcTtf);
        bool need = !File.Exists(dst) || new FileInfo(dst).Length != srcInfo.Length;
        if (need)
        {
            File.Copy(SrcTtf, dst, true);
            AssetDatabase.ImportAsset(TtfPath, ImportAssetOptions.ForceUpdate);
            Debug.Log(P + "拷入 " + TtfPath + "（" + (srcInfo.Length / 1024 / 1024) + " MB）");
        }
        else
        {
            Debug.Log(P + "已在工程里，跳过拷贝：" + TtfPath);
        }

        EnsureFontReadable();
    }

    /// <summary>
    /// 从 TTF 生成 Dynamic 的 CJK TMP_FontAsset。
    /// 图集和材质要**作为子资产存进去** —— 不然下次开会话 `atlasTextures` 是 null，TMP 会说
    /// 「font asset has no atlas texture」，字一个都不出。
    ///
    /// ⚠️ **存盘之后 `m_GlyphTable` / `m_CharacterTable` 就是空的，这是对的，别去「修」。**
    ///    Dynamic 模式下字形是**运行时现栅格化**进图集的 —— 原版那份 `NotoSerifCJK-Regular SDF`
    ///    盘上同样是 0 条（对照它自己的三份拉丁字体 `Asar` / `Pragati` / `RobotoCondensed`，
    ///    那三份是 **Static** 模式、分别存了 250 / 250 / 304 条字形）。
    ///    所以「像原版」= 同样的模式 + 同样的参数，不是同一张烘好的图集。
    ///    真正的覆盖检查在 `CheckCoverage`，那个是建库时就报出来的。
    /// </summary>
    public static void BuildCjkFontAsset()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
        if (font == null) { Fail("没找到 " + TtfPath + " —— 先跑 CopyCjkFont。"); return; }

        // ① 先验收字面：拿一份**用完就扔**的字体资产把语料试烘一遍，有字烘不出来就当场报错。
        //    （运行时才发现「某个字是空白」的 bug 最难查，宁可在这儿拦。）
        CheckCoverage(font);

        // ② 再建真正要存的那一份
        var fa = TMP_FontAsset.CreateFontAsset(font, PointSize, Padding, RenderMode,
                                               AtlasW, AtlasH, AtlasPopulationMode.Dynamic, true);
        if (fa == null) { Fail("TMP_FontAsset.CreateFontAsset 返回 null。"); return; }

        fa.name = "NotoSerifCJK-Regular SDF";

        // `AssetDatabase.CreateAsset` **不会替你建目录**（报 "Parent directory must exist"），
        // 而 `Resources/Fonts/` 是新建的
        string dir = FaPath.Substring(0, FaPath.LastIndexOf('/'));
        if (!Directory.Exists(Abs(dir))) { Directory.CreateDirectory(Abs(dir)); AssetDatabase.Refresh(); }

        AssetDatabase.DeleteAsset(FaPath);          // 重建 = 从干净的开始
        AssetDatabase.CreateAsset(fa, FaPath);
        SaveAtlasAndMaterial(fa);
        AssetDatabase.SaveAssets();

        Debug.Log(P + "OK 生成 " + FaPath +
                  "  [point=" + PointSize + " pad=" + Padding + " atlas=" + AtlasW + "x" + AtlasH +
                  " mode=" + (int)RenderMode + "(SDFAA) Dynamic]");
        Debug.Log(P + "盘上字形表 " + fa.characterTable.Count + " 条 / 图集 " + fa.atlasTextures.Length +
                  " 页 —— Dynamic 模式**这里就该是 0 条**（和原版一致，字形运行时现加）。");
    }

    /// <summary>检查 + 真渲一张图（截图看「有没有字」，断言看「字形表里有没有那个字」）</summary>
    public static void Verify()
    {
        Directory.CreateDirectory(OutDir);
        int bad = 0;

        // ① TMP Settings
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        Report(settings != null, "① TMP Settings " + (settings != null ? "在" : "**不在** —— 先跑 ImportEssentials"));
        bad += settings != null ? 0 : 1;

        // ② 着色器：UI 那版和世界空间那版都得能 Shader.Find 到
        Report(Shader.Find("TextMeshPro/Distance Field") != null,
               "② 着色器 TextMeshPro/Distance Field " + (Shader.Find("TextMeshPro/Distance Field") != null ? "在" : "**找不到**"));
        bad += Shader.Find("TextMeshPro/Distance Field") != null ? 0 : 1;

        // ③ 字体资产
        var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FaPath);
        Report(fa != null, "③ 字体资产 " + FaPath + (fa != null ? " 在" : " **不在** —— 先跑 BuildCjkFontAsset"));
        if (fa == null) { Finish(bad); return; }
        bad += fa != null ? 0 : 1;

        Report(fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null,
               "④ 图集已存成子资产（页数 " + (fa.atlasTextures == null ? 0 : fa.atlasTextures.Length) + "）");
        bad += (fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null) ? 0 : 1;

        // ⑤ 真挂一个世界空间 TMP 上去，看它出不出网格。
        //    **这一条才是 Dynamic 模式的关键** —— 盘上字形表是空的，字全靠这一步现加。
        const string probe = "余烬弓手·潮汐";
        var go = new GameObject("tmpprobe");
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = fa;
        tmp.fontSize = 12;
        tmp.text = probe;
        tmp.ForceMeshUpdate();                      // 批处理没有帧循环，必须手动推
        int vis = tmp.textInfo.characterCount;
        var mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
        int verts = mesh == null ? 0 : mesh.vertexCount;
        // 顶点数用「至少」而不是「等于」：TMP 在 `字数×4` 之外还会多留一块几何
        // （实测 7 个字是 32 而不是 28），钉死等于号只会天天误报。
        Report(vis == probe.Length && verts >= probe.Length * 4,
               "⑤ 世界空间 TMP 出网格：可见字形 " + vis + "/" + probe.Length + "、顶点 " + verts + "（≥ " + probe.Length * 4 + "）");
        bad += (vis == probe.Length && verts >= probe.Length * 4) ? 0 : 1;

        // ⑥ 现加的字**真的进了字形表**（不是「网格出来了但图集里没东西」）。
        //    ⚠️ 必须在 `ForceMeshUpdate()` **之后**查 —— 之前查必然全 false，那是 Dynamic 的正常状态。
        int have = 0;
        foreach (char c in probe) if (fa.HasCharacter(c)) have++;
        Report(have == probe.Length, "⑥ Dynamic 现加字形：探针「" + probe + "」命中 " + have + "/" + probe.Length);
        bad += have == probe.Length ? 0 : 1;

        // ⑦ 渲出来存张图 —— 上面几条全过也可能字是空的（材质没设对），这张图是给人看的
        string png = Path.Combine(OutDir, "cjk_probe.png");
        RenderToPng(tmp, png);
        Debug.Log(P + "⑦ 探针图 → " + png);

        // ⑧ 字号换算的基准值 —— 卡面上的字大不大全靠它。
        //    量错（量成**行盒**而不是**字形四边形**）字就会小 ~30%，
        //    而截图上看不出来「本来该多大」，所以把数直接报出来。
        float wg = CardPresentation.TmpFont.WorldGlyphPerFontSize;
        float titleH = CardPresentation.CardView.TitleFontSize * wg;
        float kwH = CardPresentation.CardView.KeywordFontSize * wg;
        bool wgOk = wg > 0.002f && wg < 0.2f;
        Report(wgOk,
               "⑧ 汉字「字形高度 ÷ 字号」= " + wg.ToString("F4") +
               "　→ 卡名字号 " + CardPresentation.CardView.TitleFontSize.ToString("F1") +
               "、字高 " + titleH.ToString("F3") + " 世界单位 = **卡高的 " +
               (titleH / CardPresentation.CardView.Height * 100f).ToString("F1") + "%**" +
               "；关键词字高 " + kwH.ToString("F3") +
               "　｜　HUD（scale 3）字号 " + CardPresentation.Label.FontSizeFor(3).ToString("F1"));
        bad += wgOk ? 0 : 1;

        Object.DestroyImmediate(go);
        Finish(bad);
    }

    // ─────────────────────────── 内部 ───────────────────────────

    static void Report(bool ok, string msg) { Debug.Log(P + (ok ? "OK " : "!! ") + msg); }

    static void Finish(int bad)
    {
        Debug.Log(P + (bad == 0 ? "全部通过" : ("**有 " + bad + " 条没过**")));
        Exit(bad == 0 ? 0 : 1);
    }

    static void Fail(string msg) { Debug.LogError(P + msg); Exit(1); }

    static void Exit(int code) { if (Application.isBatchMode) EditorApplication.Exit(code); }

    /// <summary>工程相对路径（`Assets/...`）→ 绝对路径。文件操作用，`AssetDatabase` 用不着</summary>
    static string Abs(string assetPath)
    {
        return Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath);
    }

    /// <summary>
    /// 找 Essentials 包。先走 PackageInfo（不用管缓存目录名里那串哈希），
    /// 找不到再扫 Library/PackageCache。
    /// </summary>
    static string FindEssentials()
    {
        var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui/package.json");
        if (info != null && !string.IsNullOrEmpty(info.resolvedPath))
        {
            string p = Path.Combine(info.resolvedPath, "Package Resources", EssentialsName);
            if (File.Exists(p)) return p;
        }

        string cache = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "PackageCache");
        if (Directory.Exists(cache))
            foreach (var d in Directory.GetDirectories(cache, "com.unity.ugui@*"))
            {
                string p = Path.Combine(d, "Package Resources", EssentialsName);
                if (File.Exists(p)) return p;
            }
        return null;
    }

    // ───────────────────── .unitypackage 解包 ─────────────────────
    //
    // `.unitypackage` 就是个 **gzip + tar**（ustar）。里面**每条记录是一个 GUID 目录**，
    // 目录下三个有用的文件：
    //     `pathname`   → 这个资产要落到工程的哪个路径（**长路径在这儿**，tar 里的名字只有 GUID）
    //     `asset`      → 文件内容
    //     `asset.meta` → 它的 .meta。**guid 就在里面**，必须原样写出去，否则资产之间的互相引用会断
    // （还有 `preview.png` 缩略图和 mac 写的 pax 扩展头，跳过。）
    // 目录条目没有 `asset`，只有一份带 `folderAsset: yes` 的 `asset.meta`。

    struct TarEntry { public string Name; public char Type; public byte[] Data; }

    /// <summary>
    /// 把 .unitypackage 里的资产按 `pathname` 写进工程。返回写出的资产个数。
    /// 会覆盖同名文件 —— **内容不同**才警告（重跑一次这个函数不该刷一屏）。
    /// </summary>
    static int ExtractUnityPackage(string pkgPath, string projectRoot)
    {
        // guid → (文件名 → 内容)。整个包不到 1 MB，直接全读进内存。
        var bucket = new Dictionary<string, Dictionary<string, byte[]>>();
        foreach (var e in ReadTar(pkgPath))
        {
            if (e.Type != '0' && e.Type != '\0') continue;          // 只要普通文件
            int slash = e.Name.IndexOf('/');
            if (slash <= 0) continue;
            string guid = e.Name.Substring(0, slash);
            string file = e.Name.Substring(slash + 1);
            if (file.IndexOf('/') >= 0 || file == "preview.png") continue;

            Dictionary<string, byte[]> f;
            if (!bucket.TryGetValue(guid, out f)) { f = new Dictionary<string, byte[]>(); bucket[guid] = f; }
            f[file] = e.Data;
        }

        int written = 0;
        foreach (var kv in bucket)
        {
            byte[] pn;
            if (!kv.Value.TryGetValue("pathname", out pn)) continue;
            string path = System.Text.Encoding.UTF8.GetString(pn).Trim().Replace('\\', '/');
            if (path.Length == 0 || !path.StartsWith("Assets/")) continue;

            string abs = Path.Combine(projectRoot, path);
            string parent = Path.GetDirectoryName(abs);
            if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);

            byte[] asset, meta;
            if (kv.Value.TryGetValue("asset", out asset) && asset.Length > 0)
            {
                if (File.Exists(abs) && !SameBytes(abs, asset))
                    Debug.LogWarning(P + "覆盖了内容不同的同名文件：" + path);
                File.WriteAllBytes(abs, asset);
                written++;
            }
            else if (!Directory.Exists(abs))
            {
                Directory.CreateDirectory(abs);                     // 目录条目
            }

            if (kv.Value.TryGetValue("asset.meta", out meta))
                File.WriteAllBytes(abs + ".meta", meta);
        }
        return written;
    }

    /// <summary>
    /// 极简 tar 读取（ustar）。只认普通文件；pax / GNU 扩展头一律跳过 ——
    /// 我们要的东西（GUID 目录名、`pathname` 的**内容**）都不长，用不到扩展头。
    /// </summary>
    static List<TarEntry> ReadTar(string pkgPath)
    {
        var list = new List<TarEntry>();
        using (var fs = File.OpenRead(pkgPath))
        using (var gz = new GZipStream(fs, CompressionMode.Decompress))
        {
            var hdr = new byte[512];
            while (ReadExact(gz, hdr, 512))
            {
                if (IsZeroBlock(hdr)) break;                        // 全零块 = 归档结束

                string name = CStr(hdr, 0, 100);
                string prefix = CStr(hdr, 345, 155);
                if (prefix.Length > 0) name = prefix + "/" + name;
                // 归档里条目名带 `./` 前缀（`./<GUID>/asset`）—— 不剥掉的话 guid 会被切成 "."
                while (name.StartsWith("./")) name = name.Substring(2);
                if (name.StartsWith("/")) name = name.Substring(1);

                long size = Octal(hdr, 124, 12);
                char type = (char)hdr[156];

                var data = new byte[size];
                if (size > 0 && !ReadExact(gz, data, (int)size)) break;
                long pad = (512 - size % 512) % 512;
                if (pad > 0) Skip(gz, pad);

                if (type == 'x' || type == 'g' || type == 'L' || type == 'K') continue;
                list.Add(new TarEntry { Name = name, Type = type, Data = data });
            }
        }
        return list;
    }

    static bool ReadExact(Stream s, byte[] buf, int n)
    {
        int got = 0;
        while (got < n)
        {
            int r = s.Read(buf, got, n - got);
            if (r <= 0) return false;
            got += r;
        }
        return true;
    }

    static void Skip(Stream s, long n)
    {
        var buf = new byte[4096];
        while (n > 0)
        {
            int r = s.Read(buf, 0, (int)System.Math.Min(n, buf.Length));
            if (r <= 0) return;
            n -= r;
        }
    }

    static bool IsZeroBlock(byte[] b)
    {
        for (int i = 0; i < b.Length; i++) if (b[i] != 0) return false;
        return true;
    }

    static string CStr(byte[] b, int off, int len)
    {
        int end = off;
        while (end < off + len && b[end] != 0) end++;
        return System.Text.Encoding.UTF8.GetString(b, off, end - off);
    }

    /// <summary>tar 的数字字段是**八进制 ASCII**，可能以 NUL 或空格收尾</summary>
    static long Octal(byte[] b, int off, int len)
    {
        long v = 0;
        for (int i = off; i < off + len; i++)
        {
            byte c = b[i];
            if (c == 0 || c == ' ') continue;
            if (c < '0' || c > '7') break;
            v = v * 8 + (c - '0');
        }
        return v;
    }

    static bool SameBytes(string path, byte[] expect)
    {
        var have = File.ReadAllBytes(path);
        if (have.Length != expect.Length) return false;
        for (int i = 0; i < have.Length; i++) if (have[i] != expect[i]) return false;
        return true;
    }

    /// <summary>
    /// 确保 TTF 的导入设置带字形数据 —— 不带的话 Dynamic 字体资产在**构建后**取不到源字模。
    /// 编辑器里能跑、打包出来是空的，所以现在就钉死。
    /// </summary>
    static void EnsureFontReadable()
    {
        var imp = AssetImporter.GetAtPath(TtfPath) as TrueTypeFontImporter;
        if (imp == null) { Debug.LogWarning(P + "拿不到 " + TtfPath + " 的 TrueTypeFontImporter，跳过导入设置检查。"); return; }
        if (imp.includeFontData && imp.fontRenderingMode != FontRenderingMode.Smooth)
        {
            Debug.Log(P + "TTF 导入设置已就绪（includeFontData=" + imp.includeFontData +
                      " rendering=" + imp.fontRenderingMode + "）");
            return;
        }
        imp.includeFontData = true;
        imp.fontRenderingMode = FontRenderingMode.Smooth;
        imp.SaveAndReimport();
        Debug.Log(P + "改过 TTF 导入设置：includeFontData=true / Smooth，已重导。");
    }

    /// <summary>
    /// 要检查覆盖的那批字。
    /// **只查我们会用到的** —— 3 万多个汉字全查一遍没意义，而这版文案就是
    /// `CardText` 里那张表（26 张卡的译名 + 关键词 + 效果小字）。
    /// ⚠️ 中文那部分**从 `CardText.AllChinese()` 取，不手抄** —— 以后加卡/加关键词，
    ///    字自动进语料，不会漏（「两处写同一条规则 = 迟早不一致」）。
    /// </summary>
    static string Corpus()
    {
        var sb = new System.Text.StringBuilder();

        // 拉丁 + 数字 + 中英标点：卡面数值、HUD 的 END TURN、中文顿号书名号那些
        sb.Append("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        sb.Append(" .,:;!?()[]{}<>/\\|+-=_*'\"%&#@~^$°·—…、。，；：！？（）【】《》「」『』“”‘’　");

        foreach (var s in CardPresentation.CardText.AllChinese()) sb.Append(s);

        var seen = new HashSet<char>();
        foreach (char c in sb.ToString()) seen.Add(c);
        var sorted = new List<char>(seen);
        sorted.Sort();                                   // 排序只是为了输出稳定、好比对
        return new string(sorted.ToArray());
    }

    /// <summary>
    /// 语料里的字**字体里有没有**。烘不出来 = 那个字在 TTF 里缺失，运行时渲染出来是空白/方块，
    /// 而且**不会报错** —— 所以放在建库时拦。
    /// 用一份临时字体资产试（`DestroyImmediate` 掉），不碰要存的那一份。
    /// </summary>
    static void CheckCoverage(Font font)
    {
        var probe = TMP_FontAsset.CreateFontAsset(font, PointSize, Padding, RenderMode,
                                                  512, 512, AtlasPopulationMode.Dynamic, true);
        if (probe == null) { Debug.LogError(P + "覆盖检查：CreateFontAsset 返回 null，跳过。"); return; }

        string text = Corpus();
        string missing;
        probe.TryAddCharacters(text, out missing);
        if (!string.IsNullOrEmpty(missing))
            Debug.LogError(P + "语料里有 " + missing.Length + " 个字**这份字体里没有**，渲染出来会是空白：" + missing);
        else
            Debug.Log(P + "OK 覆盖检查：语料 " + text.Length + " 个不同的字全都能烘出来，无缺字");

        Object.DestroyImmediate(probe);
    }

    /// <summary>把图集纹理和材质存成字体资产的子资产（不存的话下次开会话是 null）</summary>
    static void SaveAtlasAndMaterial(TMP_FontAsset fa)
    {
        for (int i = 0; i < fa.atlasTextures.Length; i++)
        {
            var t = fa.atlasTextures[i];
            if (t == null) continue;
            t.name = fa.name + " Atlas" + (i == 0 ? "" : " " + i);
            if (!AssetDatabase.IsSubAsset(t)) AssetDatabase.AddObjectToAsset(t, fa);
        }

        if (fa.material != null)
        {
            fa.material.name = fa.name + " Material";
            if (!AssetDatabase.IsSubAsset(fa.material)) AssetDatabase.AddObjectToAsset(fa.material, fa);
        }
    }

    /// <summary>把 TMP 摆在相机前渲一张 PNG（批处理下没有帧循环，得手动 Render）</summary>
    static void RenderToPng(TextMeshPro tmp, string path)
    {
        const int W = 640, H = 160;

        var camGo = new GameObject("tmpprobe_cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.11f, 0.13f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = 2f;
        cam.transform.position = new Vector3(0, 0, -10);
        camGo.transform.LookAt(Vector3.zero);

        tmp.transform.position = Vector3.zero;
        tmp.alignment = TextAlignmentOptions.Center;

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

        // 顺便断言「不是一张纯背景」—— 全空的话上面那条 ⑥ 是看不出来的
        var px = tex.GetPixels32();
        var bg = (Color32)cam.backgroundColor;
        int ink = 0;
        for (int i = 0; i < px.Length; i++)
            if (Mathf.Abs(px[i].r - bg.r) > 8 || Mathf.Abs(px[i].g - bg.g) > 8 || Mathf.Abs(px[i].b - bg.b) > 8) ink++;
        Report(ink > 200, "⑦ 渲出来有字：非背景像素 " + ink + " / " + px.Length);

        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);
    }
}
