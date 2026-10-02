// EffectSweepBatch.cs — 批量时序采样：把一批效果在多个时间点各渲一次，只为拿到数字
//
// 为什么需要它
// ------------
// EffectCompare 只在 Simulate(1.2s) 采一帧。于是有 280 个效果（占 29%）「两边都是空的」——
// 那不是还原失败，是采样点不对（闪电/子弹命中这类瞬时特效 1.2s 早播完了）。
// **拿着一把不准的尺子逐个量，量出来的清单是错的。** 这个工具就是把尺子修好：
// 每个效果在 8 个时间点各渲一次，原版/导出各记「亮点数」和「亮度和」。
//
// 关键：两趟分开跑，不能合并
// --------------------------
// 原版那侧要从 battleprefabs 包里取 prefab，且**必须加载全部 84 个包**才能解析到原版 shader
// （只加载它一个包时原版渲染成品红，品红很亮 → 比值假性掉到 0.2）。
// 导出那侧**不能加载任何源 bundle** —— wf_shaders_extra.bundle 正是从 battleprefabs 抽出来的，
// 一旦 battleprefabs 在场，Unity 按「内容相同」判定重复，补充包加载不了，导出侧退回占位材质。
// 两边的条件互相冲突，所以必须分两个进程跑，把结果按效果名合并（见 工具/analyze_sweep.py）。
//
// Side="orig"：加载全部源 bundle，渲染原版，并把每个效果的取景缓存到 sweep_frames.tsv
// Side="exp" ：不加载源 bundle（= 真实运行时条件），读取景缓存，渲染导出
//
// ⚠️ 这两个坑都是实测踩出来的，两个方向都会把 900+ 个效果误判成「严重偏暗」。
//
// 用法（两趟都要跑，顺序不能反 —— exp 依赖 orig 产出的取景缓存）：
//   Unity.exe -batchmode -quit -projectPath ... -executeMethod EffectSweepBatch.Run -logFile -
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EffectSweepBatch
{
    // 要扫哪一侧。两趟的条件互相冲突，必须分进程跑（详见文件头）：
    //   orig：加载全部源 bundle（否则原版渲染成品红）→ 顺带产出取景缓存
    //   exp ：不加载源 bundle（否则补充 shader 包被顶掉）→ 读 orig 留下的取景缓存
    // **顺序不能反**：exp 依赖 orig 产出的 sweep_frames.tsv。
    // 2026-09-13（第三十四轮）：原来这里是写死的字面量，两趟之间要**改源码 + 让它重新编译**
    // 才能切侧 —— 跑一趟留一个脏编辑，而且「这次到底跑的哪侧」只能靠读源码。
    // 改成读环境变量 WFSWEEP_SIDE，**默认仍是 "exp"**（不设变量时行为与改之前完全一样）：
    //   第一趟：WFSWEEP_SIDE=orig   （产出取景缓存）
    //   第二趟：不设该变量         （= exp，读第一趟的缓存）
    static readonly string Side = System.Environment.GetEnvironmentVariable("WFSWEEP_SIDE") ?? "exp";  // "exp" | "orig"

    /// <summary>把「每个目标开跑前的全局 shader 关键字」打进日志（`WFSWEEP_GLOBALS=1` 才开）。
    /// 查「原版侧渲染随跑法变」用的，默认关 —— 全量 957 个目标会刷 957 行。</summary>
    static readonly bool GlobalsProbe = System.Environment.GetEnvironmentVariable("WFSWEEP_GLOBALS") == "1";
    /// <summary>🔬 实验开关：`WFORIG_NOEMIT=1` → 渲染前把实例上所有材质的 `_EMISSION` 关掉（见 `RenderAt` 里的注释）。</summary>
    static readonly bool NoEmissionOnOrig = System.Environment.GetEnvironmentVariable("WFORIG_NOEMIT") == "1";
    static bool _noEmitLogged;

    const string P = "WFSWEEP ";

    /// <summary>结果按侧分文件：sweep_orig.tsv / sweep_exp.tsv，最后在 Python 里按效果名合并。
    /// 放在 `资料/比对基线/` 而不是临时目录 —— 这是台账的原始数据，要跟着仓库走。</summary>
    static string ResultPath { get { return ResultPathFor(Side); } }
    static string ResultPathFor(string side) => $@"d:\4\Unity\资料\比对基线\sweep_{side}.tsv";

    /// <summary>🔴 **2026-09-19 晚：`WFSWEEP_SIDE=both` —— 同进程两阶段，消掉「跨进程漂移」。**
    ///
    /// 为什么要它：实测「**种子钉死 ≠ 尺子跨进程确定**」—— 只重跑 `orig` 那趟（与本轮改动无关），
    /// 新旧 `sweep_orig.tsv`（各 7657 行）**差 471 行（6.2%）**，`sum>=1000` 的行里 **17.2%** 会漂。
    /// 而 `SweepStabilityProbe` 把锅定死在**渲染的 ±1 LSB 浮点/光栅噪声**上（粒子状态含拖尾几何
    /// 三次全同、`Time` 冻结、`Pause()` 也没用，两次渲染只差 93~2821 个像素、几乎都是 ±1）。
    /// **每一侧单独看都只漂 0.001~0.16%，但两侧在**不同进程**里渲 ⇒ 比值里就留下了这个差。**
    ///
    /// 修法：**两侧在同一个进程里渲**（先把原版那侧全渲完 → `UnloadAllAssetBundles(true)` →
    /// 再渲导出那侧）。实测验证（`SweepStabilityProbe` 的 `WFPROBE_BOTH=1`，两个进程各跑一遍）：
    /// 绝对 sum 仍有 0.001~0.005% 的小漂，**但比值稳定到小数点后四位**
    /// （`EC Sword Cut Board DMC Style` 两遍都是 **0.9697**；`EnvCond Emperor's Children 2 Fumes`
    /// 是 **0.9979 / 0.9978**）—— 而这正是尺子真正用的量。
    ///
    /// ⚠️ 必须配 `WarpforgeShaderLoader.Reset()`：`_tried` 是一次性的，第一阶段（源包在场时）
    /// 已经把加载尝试用掉了，不清空的话导出侧整片退回占位材质。
    ///
    /// 🔴 **2026-09-19 深夜更正：`both` 在全量规模上**没有**减少漂移**（472/7656 = 6.2%，
    ///    与「分两趟」的 475/7656 一模一样）——**别把它当修法**。探针里那两次「比值稳定到四位小数」
    ///    没有泛化（探针只渲 2 个效果、排在进程开头，而那条「累积压暗」攒够 5 个前驱就饱和）。
    ///    **真正的结论是「判定级噪声底」**（两次完全相同的跑之间 `|Δ|ln||` 中位与 90 分位都是 0、
    ///    判定只动 1 条 ⇒ **E 组计数逐次跑 ±1**），详见上述正本 §14.4 ⑪ 与 `工具/analyze_sweep.py` 的输出。
    ///    保留本模式只是为了**少起一个进程、两侧条件更同源**，默认仍是分两趟。</summary>
    static readonly bool BothSides = (System.Environment.GetEnvironmentVariable("WFSWEEP_SIDE") == "both");

    const string BundleDir =
        @"D:\2\Warhammer 40k Warpforge\Warpforge_Data\StreamingAssets\aa\StandaloneWindows64";
    const string PrefabDir = "Assets/WarpforgeVFX/Prefabs";
    const string OutDir = @"d:\4\_tmp_view\sweep_batch";
    const string FrameCachePath = @"d:\4\Unity\资料\比对基线\sweep_frames.tsv";

    // 要扫的效果：每行一个效果名。文件不存在时用 NameFilter（子串匹配），
    // 两个都空 = 全量 958 个（很久，慎用）
    const string TargetFile = @"d:\4\_tmp_view\sweep_targets.txt";
    static readonly string[] NameFilter = { };

    static readonly float[] Times = { 0.15f, 0.3f, 0.5f, 0.75f, 1.0f, 1.5f, 2.0f, 3.0f };

    const int W = 256, H = 256;

    // 是否同时落 PNG。排查「数字和肉眼看的不一致」时必须打开：
    // tex.GetPixels() 拿到的是**线性**值，而 EncodeToPNG 写的是 sRGB ——
    // 两边亮度差 a 倍时，线性口径下的比值会变成 a^2.2（0.5 → 0.22）。
    // 只记数字很容易把这种口径差当成真缺陷。
    static readonly bool WritePng = false;

    static readonly Color Bg = new Color(0.07f, 0.08f, 0.10f, 1f);

    /// <summary>🔴 **2026-10-02：把「背景」从纯色换成一张有内容、有深度的图案底图 —— 尺子换了。**
    ///
    /// **为什么**（判据 → `项目任务.md` §三 第 4 条 · `资料/特效还原_进度与交接.md` §P1-a0·附）：
    /// 原来场景里**什么都没有**、背景 = 相机的 clear color，于是两类效果在这个尺子上**根本量不出**：
    ///   · **抓屏扭曲族**（`Particle Distortion`，233 条）抓屏幕色来扭曲，空场景**没有可扭曲的内容**
    ///     ⇒ 扭曲前=扭曲后=纯色，差异恒为 0；
    ///   · **软粒子族**按「粒子到不透明场景的距离」淡出，空场景里深度缓冲停在**远平面**
    ///     ⇒ 两侧的深度项都退化成常数，谁对谁错量不出来（E 组那批「偏亮」相当一部分就是这么来的）。
    /// 做法 = `DistortProbe.MakeCheckerboard` 那块**填满视野 · `_ZWrite=1` · `renderQueue=2000`** 的
    /// 棋盘 quad —— 它同时进 `_CameraOpaqueTexture`（抓屏族要的）**和**深度缓冲（软粒子族要的）。
    /// 摆位按**取景**算（`d2 = dist + radius*3`），所以每个效果都有自己的、贴着它尺度的一块底图。
    ///
    /// 🔴 **判读基准随之改了**：亮点判据从「和常量 `Bg` 比」改成「和**同一场景、不播效果**渲出来的
    /// 那张底图（plate）比」。**定义没变**（都是「效果真正改动了哪些像素、改了多少」），
    /// 只是基准从「空场景的常量色」换成「有内容场景的底图」。
    /// ⇒ **新旧数字不可比**（老基线留在 `资料/比对基线/sweep_{orig,exp}.tsv`；要比就得用
    /// `WFSWEEP_BACKDROP=0` 重跑旧口径）。两份取景缓存也必须删掉重生成 —— 口径变了。
    ///
    /// ⚠️ **底图必须只渲一次、且不含效果实例**（`RenderAt` 收尾会 `DestroyImmediate` 掉实例，
    /// 所以每个目标开跑前场景是干净的）；两侧（orig/exp）都各自渲自己的底图 ——
    /// 底图只有 `URP/Unlit` + 棋盘贴图，与 shader 解析无关 ⇒ 两侧逐像素同源，
    /// 但**仍会各打一行 `PLATE` 到日志**（含校验和），跨趟对不上时能当场看见。
    ///
    /// 开关：默认**开**；`WFSWEEP_BACKDROP=0` 退回旧口径（做 A/B 时用）。</summary>
    static readonly bool UseBackdrop =
        System.Environment.GetEnvironmentVariable("WFSWEEP_BACKDROP") != "0";

    /// <summary>当前这块底图（每个目标重建一块；`null` = 旧口径）。</summary>
    static GameObject _backdrop;

    /// <summary>一张图所有像素的 r+g+b 之和（只给日志里的自检数字用）。</summary>
    static double SumOf(Color[] px)
    {
        double s = 0;
        foreach (var c in px) s += c.r + c.g + c.b;
        return s;
    }

    /// <summary>一块填满取景、写深度、进不透明层的棋盘 quad。
    /// 几何与材质**照抄** `DistortProbe.MakeCheckerboard`（同一把尺子的两个工具要同源）。</summary>
    static GameObject MakeBackdrop(CamFrame f)
    {
        const int N = 256, Cell = 32;
        // 🔴 **2026-10-02：棋盘的两色从「黑/白」改成「低对比中间调」。**
        //
        // **为什么**：白格（=1.0）会把**加色**效果顶到**饱和** ⇒ 那些像素永远「没变化」、
        // 一律不计入 `lit`。实测（有底图 vs 纯色口径逐效果比 `lit`）：
        //   `BulletImpact_4shot_trail` **29 vs 87** · `BulletImpact_9shot_trail` **2 vs 65** ·
        //   `BulletImpact_Kroot_KrootRifle` **18 vs 35** ⇒ **只剩 1/3–1/2 的可测面积**，
        //   35 条**贴着噪声门限的低亮效果**因此整条掉进「两边全程空」（`C 对得上 → B 两边全程空`）。
        // 取值：**0.10 / 0.60** —— 留 0.4 的饱和余量给加色贡献，同时仍有 **6× 的对比度**
        // （抓屏族要的「有内容可扭曲」还在）。⚠️ 别调到接近 1.0，也别调到全黑（那就退回旧尺子了）。
        var dark = new Color(0.10f, 0.10f, 0.10f, 1f);
        var light = new Color(0.60f, 0.55f, 0.50f, 1f);
        var tex = new Texture2D(N, N, TextureFormat.RGB24, false);
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
                px[y * N + x] = ((x / Cell + y / Cell) % 2 == 0) ? light : dark;
        tex.SetPixels(px); tex.Apply();

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetTexture("_BaseMap", tex);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Surface", 0f);     // Opaque
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_ZWrite", 1f);
        mat.renderQueue = 2000;           // 不透明 ⇒ 会进 _CameraOpaqueTexture

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "SweepBackdrop";
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // ⚠️ `CamFrame` 只缓存 pos/look/fov/near/far（不扩 schema）⇒ `radius`/`dist` 由它反推：
        //    `FrameCamera` 的算式是 `dist = radius / tan(fov/2) * 1.15`，反过来就是下面这两行。
        //    改 `FrameCamera` 的摆位数学时**这里必须跟着改**（两处是同一把尺子）。
        float dist = Vector3.Distance(f.pos, f.look);
        float radius = Mathf.Max(0.5f, dist * Mathf.Tan(Mathf.Deg2Rad * f.fov * 0.5f) / 1.15f);
        float d2 = dist + radius * 3f;
        float h = 2f * d2 * Mathf.Tan(Mathf.Deg2Rad * f.fov * 0.5f);
        go.transform.position = f.look + new Vector3(0f, 0f, d2);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = new Vector3(h * W / H, h, 1f);
        return go;
    }

    /// <summary>把当前场景按相机的取景渲一张、把像素读回来（底图 plate 与逐时刻读数共用同一条路）。</summary>
    static Color[] RenderToColors(Camera cam)
    {
        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        var px = tex.GetPixels();
        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        return px;
    }

    public static void Run()
    {
        if (BothSides) { RunBoth(); return; }
        RunPhase(Side);
    }

    /// <summary>同进程两阶段：先原版（源包全加载）→ 卸载 → 再导出（真实运行时条件）。
    /// 这是消掉「跨进程漂移」的修法，理由与验证见 `BothSides` 的注释。</summary>
    static void RunBoth()
    {
        RunPhase("orig");
        // 卸掉全部源包 —— 原来「必须分两个进程」的唯一理由就是这一条。
        int n = AssetBundle.GetAllLoadedAssetBundles().Count();
        AssetBundle.UnloadAllAssetBundles(true);
        Resources.UnloadUnusedAssets();
        // 🔴 必须把 shader 加载器的「试过一次」标记清掉：第一阶段（源包在场）已经让它试过一次、
        //    必然是被「同内容」顶掉的那份结果，不复位的话导出侧整片退回占位材质。
        WarpforgeVFX.WarpforgeShaderLoader.Reset();
        Debug.Log(P + $"源包已全部卸载（{n} 个）· shader 加载器已复位 ⇒ 进入导出阶段");
        RunPhase("exp");
    }

    static void RunPhase(string sideName)
    {
        Debug.Log(P + $"=== 批量时序采样 开始（Side={sideName}）===");
        if (NoGrabPass) SetGrabFeatureActive(false);      // 🔬 见 NoGrabPass 的注释
        Directory.CreateDirectory(OutDir);
        bool doOrig = sideName == "orig";

        // 取景必须两边一致，否则亮度差没有可比性（踩过）。但导出侧那趟**不能**加载源 bundle，
        // 拿不到原版 prefab 去量包围盒 —— 所以由原版那趟把每个效果的取景算好缓存下来，
        // 导出侧直接读缓存。
        var frames = LoadFrames();

        Dictionary<string, GameObject> originals = null;
        if (doOrig)
        {
            // ⚠️ 原版这趟必须**加载全部 84 个包**。
            //    原版 prefab 的材质同样要从包里解析 shader，而 battleprefabs 里的 shader
            //    并不是可加载资产（LoadAllAssets<Shader>() 返回 0），只加载它一个包时
            //    解析不到 → 原版渲染成**品红** → 品红很亮 → 原版亮度和虚高 →
            //    「导出/原版」比值假性掉到 0.2，看起来像一大片「严重偏暗」。
            //    实测踩过：先渲染出品红才发现。
            AssetBundle vfx = null;
            foreach (var f in Directory.GetFiles(BundleDir, "*.bundle"))
            {
                var b = AssetBundle.LoadFromFile(f);
                if (b != null && Path.GetFileName(f) == "battleprefabs_vfxandmisc_assets_all.bundle") vfx = b;
            }
            if (vfx == null) { Debug.LogError(P + "特效 bundle 未加载"); return; }

            originals = new Dictionary<string, GameObject>();
            foreach (var n in vfx.GetAllAssetNames())
            {
                GameObject g = null;
                try { g = vfx.LoadAsset<GameObject>(n); } catch { }
                if (g != null) originals[g.name] = g;
            }
            Debug.Log(P + $"原版效果 {originals.Count} 个（已加载全部源 bundle）");
        }
        else if (frames.Count == 0)
        {
            Debug.LogError(P + "没有取景缓存 —— 必须先跑一趟 Side=\"orig\" 生成 " + FrameCachePath);
            return;
        }

        // ---- 目标清单 ----
        var targets = new List<string>();
        if (File.Exists(TargetFile))
        {
            foreach (var l in File.ReadAllLines(TargetFile))
            {
                var s = l.Trim();
                if (s.Length > 0 && !s.StartsWith("#")) targets.Add(s);
            }
            Debug.Log(P + $"从 {TargetFile} 读到 {targets.Count} 个目标");
        }
        else
        {
            var all = Directory.GetFiles(PrefabDir, "*.prefab")
                               .Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).ToList();
            targets = NameFilter.Length > 0
                ? all.Where(x => NameFilter.Any(k => x.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)).ToList()
                : all;
            Debug.Log(P + $"没有目标文件，改用 NameFilter，命中 {targets.Count} 个");
        }

        int shaderN = WarpforgeVFX.WarpforgeShaderLoader.ShaderNames.Count();
        Debug.Log(P + $"原版 shader {shaderN} 个可用");

        // ---- 一个场景一个相机，复用到底 ----
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Bg;

        // 🔴 2026-09-15 记一笔**试过但没用**的方向，别重试：
        //    怀疑「全局 shader 关键字跨效果泄漏」时，写过一版「每次渲染前把全局关键字还原成基准集」
        //    （`RestoreBaseGlobals()`；**现在只剩方法、没接进 `RenderAt`**）。实测**数值纹丝不动**
        //    （`StrikeEffect` 498/346 vs 修前 498/344）⇒ **全局关键字不是原因**。
        //    探针确实看到泄漏（`Cut Wulfen SW` 之后多出 `_SURFACE_TYPE_TRANSPARENT` /
        //    `_ALPHAPREMULTIPLY_ON` / `_ALPHAMODULATE_ON` / `_NORMALMAP` / `_ALPHATEST_ON` /
        //    `_DETAIL_MULX2` / `_MAIN_LIGHT_SHADOWS` …）—— 那是**相关，不是因果**。
        //
        //    另一条已排除：**不是「某个前驱效果」**。二分实测：`StrikeEffect` 前面**单独**垫
        //    `Cut Wulfen SW`(433) / `Cut Wulfen SW Double`(431) / `Bore Through`(435) /
        //    `Bore Through Intense`(435) —— **四个都干净**（≈ 单独跑的 437），
        //    只有攒够 **5 个前驱**（小批 13 里它排第 6）才掉到 **344**。
        //    ⇒ **随累积量变化**，不是随「谁在前面」变化。原因仍未知。
        //
        //    **实用结论（先照这个用，别再花时间追）**：要查某个效果，把它写在清单**第一个**
        //    （或单独跑一趟）最保险；小批里排在后面的数，先怀疑是不是被压暗了。
        CaptureBaseGlobals();

        var sb = new StringBuilder();
        sb.AppendLine("effect\ttime\tlit\tsum\tlit_srgb\tsum_srgb");

        int done = 0, skipped = 0;
        foreach (var name in targets)
        {
            GameObject src = null;
            if (doOrig)
            {
                if (!originals.TryGetValue(name, out src)) { Debug.LogWarning(P + $"原版里没有 {name}"); skipped++; continue; }
            }
            else
            {
                src = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{name}.prefab");
                if (src == null) { Debug.LogWarning(P + $"导出里没有 {name}"); skipped++; continue; }
            }

            CamFrame frame;
            if (doOrig)
            {
                frame = FrameCamera(src);
                frames[name] = frame;          // 缓存给导出那趟用
            }
            else if (!frames.TryGetValue(name, out frame))
            {
                Debug.LogWarning(P + $"取景缓存里没有 {name}"); skipped++; continue;
            }
            ApplyFrame(cam, frame);

            // 🔴 2026-10-02：**新尺子** —— 每个目标重建一块底图，并先渲一张「只有底图、没有效果」
            //    的**基准图**（plate）；`RenderAt` 之后就拿它当判读基准（见 `UseBackdrop` 的注释）。
            //    ⚠️ 底图**不能**挂到效果实例下面，否则会被 `FrameCamera` 的包围盒算进去（取景跑样）；
            //       摆位完全按 `frame` 算，与实例无关。
            Color[] plate = null;
            if (UseBackdrop)
            {
                if (_backdrop != null) UnityEngine.Object.DestroyImmediate(_backdrop);
                _backdrop = MakeBackdrop(frame);
                plate = RenderToColors(cam);
                // 自检一行：底图相对「拿掉底图那一版」动了多少像素 + 校验和。
                // 校验和是给**跨趟**用的（两侧各自渲自己的底图，对不上要能当场看见）。
                int bgDiff = 0; double chk = 0;
                for (int i = 0; i < plate.Length; i++)
                {
                    var c = plate[i];
                    chk += c.r * (i % 7 + 1) + c.g * (i % 13 + 1) + c.b * (i % 17 + 1);
                    if (Mathf.Abs(c.r - Bg.r) + Mathf.Abs(c.g - Bg.g) + Mathf.Abs(c.b - Bg.b) > 6f / 255f * 3f)
                        bgDiff++;
                }
                Debug.Log(P + $"PLATE\t{name}\tbgDiff={bgDiff}\tchk={chk:F1}\tsum={SumOf(plate):F1}");
            }

            // 🔴 2026-09-15 诊断：记录**进这个目标之前**还开着哪些全局 shader 关键字。
            //    为什么要它：实测「原版侧重渲同一个效果，数值会随它前面跑过什么而变」
            //    （`StrikeEffect` 单独跑 437 / 前面垫 Bore Intense 435 / 小批里排第 6 是 344，
            //     而导出侧 13/13 逐位相同）⇒ 怀疑是**全局关键字跨效果泄漏**（`_EMISSION` 这类，
            //     见 `EffectExporter.StripGlobalKeywords` 的注释）。全局状态是**进程级**的，
            //    `ApplyFrame` 之后、`RenderAt` 之前这一刻快照下来，两趟一比就知道漏的是谁。
            //    默认不开（全量 957 个目标会刷 957 行）；排查时设 `WFSWEEP_GLOBALS=1`。
            if (GlobalsProbe)
                Debug.Log(P + $"GLOBALS_BEFORE\t{name}\t" +
                          string.Join(",", System.Array.ConvertAll(Shader.globalKeywords, k => k.name)));

            var brief = new List<string>();
            foreach (var t in Times)
            {
                var png = WritePng ? $"{OutDir}/{Sanitize(name)}_{t:F2}__{sideName}.png" : null;
                var (lit, sum, litS, sumS) = RenderAt(src, t, cam, png, plate);
                sb.AppendLine($"{name}\t{t.ToString("F2", CultureInfo.InvariantCulture)}\t{lit}\t{sum:F0}\t{litS}\t{sumS:F0}");
                brief.Add($"{t:F2}:{litS}");
            }
            done++;
            // 每 20 个刷一次盘，随时可中断
            if (done % 20 == 0)
            {
                File.WriteAllText(ResultPathFor(sideName), sb.ToString());
                if (doOrig) SaveFrames(frames);
            }
        }

        File.WriteAllText(ResultPathFor(sideName), sb.ToString());
        if (doOrig) SaveFrames(frames);
        if (_backdrop != null) { UnityEngine.Object.DestroyImmediate(_backdrop); _backdrop = null; }
        if (NoGrabPass) SetGrabFeatureActive(true);       // 还原（内存里改回；批处理不落盘）
        Debug.Log(P + $"=== 结束：成功 {done}，跳过 {skipped}，结果 {ResultPathFor(sideName)} " +
                  $"[尺子={(UseBackdrop ? "有底图" : "纯色 Bg")}] ===");
    }

    /// <summary>🔬 **2026-10-01 加：`WFSWEEP_NOGRAB=1` → 扫描期间把 `GrabPassTransparentFeature` 关掉。**
    ///
    /// 为什么需要（判据 → `资料/普查产出_0918/E组根因_B3.md` §三 第 4 条）：
    /// 那个 Feature 抓的是**上一帧**的颜色缓冲，而扫描**每个时刻只渲一帧** ⇒ 抓屏族在扫描里
    /// 采到的是**别的时刻（甚至别的效果）**留下的拷贝，这一族的读数**不可解释**。
    /// 关掉之后两侧都拿不到当帧抓屏，这一族才量得出「扭曲项以外」的差。
    /// ⚠️ 这是**扫描专用**开关：正常游戏里 Feature 必须开着（`GrabPassTransparentFeature` 的注释）。</summary>
    static readonly bool NoGrabPass = System.Environment.GetEnvironmentVariable("WFSWEEP_NOGRAB") == "1";

    /// <summary>开关 URP Renderer 资产上挂的那个抓屏 Feature（改的是**内存里的**子资产，不 SaveAssets）。</summary>
    static void SetGrabFeatureActive(bool active)
    {
        string[] assets = { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" };
        int n = 0;
        foreach (var path in assets)
        {
            var data = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.ScriptableRendererData>(path);
            if (data == null) continue;
            foreach (var f in data.rendererFeatures)
                if (f is WarpforgeVFX.GrabPassTransparentFeature) { f.SetActive(active); n++; }
        }
        Debug.Log(P + $"[NOGRAB] 抓屏 Feature 已{(active ? "开" : "关")}（{n} 处）");
    }

    // ---- 取景缓存（原版那趟写，导出那趟读）----
    static Dictionary<string, CamFrame> LoadFrames()
    {
        var d = new Dictionary<string, CamFrame>();
        if (!File.Exists(FrameCachePath)) return d;
        foreach (var l in File.ReadLines(FrameCachePath).Skip(1))
        {
            var p = l.Split('\t');
            if (p.Length < 11) continue;
            d[p[0]] = new CamFrame
            {
                pos = new Vector3(float.Parse(p[1], CultureInfo.InvariantCulture),
                                  float.Parse(p[2], CultureInfo.InvariantCulture),
                                  float.Parse(p[3], CultureInfo.InvariantCulture)),
                look = new Vector3(float.Parse(p[4], CultureInfo.InvariantCulture),
                                   float.Parse(p[5], CultureInfo.InvariantCulture),
                                   float.Parse(p[6], CultureInfo.InvariantCulture)),
                fov = float.Parse(p[7], CultureInfo.InvariantCulture),
                near = float.Parse(p[8], CultureInfo.InvariantCulture),
                far = float.Parse(p[9], CultureInfo.InvariantCulture),
            };
        }
        return d;
    }

    static void SaveFrames(Dictionary<string, CamFrame> frames)
    {
        var sb = new StringBuilder();
        // 第 10 列留给将来扩展；CamFrame 是 9 个字段，读的时候按 >=11 判断会漏，
        // 所以这里补一列占位，保持列数一致
        sb.AppendLine("effect\tpx\tpy\tpz\tlx\tly\tlz\tfov\tnear\tfar\t_");
        foreach (var kv in frames)
        {
            var f = kv.Value;
            sb.AppendLine(string.Join("\t", new[] {
                kv.Key,
                f.pos.x.ToString("R", CultureInfo.InvariantCulture), f.pos.y.ToString("R", CultureInfo.InvariantCulture), f.pos.z.ToString("R", CultureInfo.InvariantCulture),
                f.look.x.ToString("R", CultureInfo.InvariantCulture), f.look.y.ToString("R", CultureInfo.InvariantCulture), f.look.z.ToString("R", CultureInfo.InvariantCulture),
                f.fov.ToString("R", CultureInfo.InvariantCulture), f.near.ToString("R", CultureInfo.InvariantCulture), f.far.ToString("R", CultureInfo.InvariantCulture),
                "0",
            }));
        }
        File.WriteAllText(FrameCachePath, sb.ToString());
    }

    static void ApplyFrame(Camera cam, CamFrame f)
    {
        cam.fieldOfView = f.fov;
        cam.nearClipPlane = f.near;
        cam.farClipPlane = f.far;
        cam.transform.position = f.pos;
        cam.transform.LookAt(f.look);
    }

    /// <summary>渲染一帧，返回（亮点数, 亮度和）—— **原始值** 和 **按 sRGB 编码后的值** 各一份。
    ///
    /// 为什么要两份：`tex.GetPixels()` 拿到的是**线性**值（ARGB32 的 RenderTexture 在
    /// 线性色彩空间工程里就是线性的），而人眼和 PNG 看到的是 sRGB。两边亮度差 a 倍时，
    /// 线性口径下比值会变成 a^2.2 —— 0.5 的差看起来像 0.22，会被误判成「严重偏暗」。
    /// 实测踩过：957 个效果里 749 个被误判成「亮度/密度不对」。</summary>
    /// <summary>进程起跑、还没渲过任何效果时的全局关键字集 —— 每个目标渲染前都还原到它。</summary>
    static string[] _baseGlobals;

    /// <summary>记下基准集。必须在**第一个目标渲染之前**调（见主流程里的说明）。</summary>
    static void CaptureBaseGlobals()
    {
        _baseGlobals = Array.ConvertAll(Shader.globalKeywords, k => k.name);
        Debug.Log(P + $"全局关键字基准集 {_baseGlobals.Length} 个（每个目标渲染前都会还原到它）");
    }

    /// <summary>把全局关键字恢复成基准集：**只关掉基准之外的**，一个都不主动打开。
    ///
    /// ⚠️ **不能反向「把基准集里的打开」** —— 2026-09-15 实测踩过：`Shader.globalKeywords` 里会列出
    /// `INSTANCING_ON` / `PROCEDURAL_INSTANCING_ON` / `DOTS_INSTANCING_ON` 这类**不允许显式打开**的
    /// 引擎关键字，一律 `SetKeyword(..., true)` 的结果是**整帧渲成一片全亮**
    /// （日志里刷 `Instancing: INSTANCING_ON keyword should not be enabled.`，
    ///  13 个效果 × 8 个时点全部 lit=65536、sum 一模一样 = 98690）。
    /// 只关不开就够了：基准集是「起跑那一刻」本来就有的，泄漏只可能往上**加**。
    ///
    /// 这也是「让每个目标都等价于进程里第一个渲的」的全部实现 —— **刻意不按名字挑关键字**
    /// （`EffectExporter.StripGlobalKeywords` 的注释原话：别凭 shader 名推断关键字，要动必须先有实测证据）。
    /// 实测证据见主流程里那段说明。</summary>
    static void RestoreBaseGlobals()
    {
        if (_baseGlobals == null) return;
        var bas = new HashSet<string>(_baseGlobals);
        foreach (var k in Shader.globalKeywords)
            if (!bas.Contains(k.name))
                Shader.SetKeyword(new UnityEngine.Rendering.GlobalKeyword(k.name), false);
    }

    /// <summary>把粒子系统的随机性钉死，再复位到 0 时刻。
    ///
    /// 为什么必须做：`useAutoRandomSeed` 默认是**开**的 —— 每次重播都换种子，
    /// 粒子的位置/大小/寿命每次都不一样。实测「同一份资产在同一个进程里连渲三次」得到
    /// 537 / 518 / 512 个亮点（亮度和却几乎不变，说明是分布变了）。
    /// 于是「同一份资产连跑两遍扫描」有 ~60% 的采样行会变，
    /// **噪声和要测的改动一样大** —— 尺子自己不确定，任何 A/B 都不成立。
    ///
    /// 三件事缺一不可：关掉自动种子、显式钉一个固定种子、先 Simulate(0, restart:true) 复位。
    /// 修完实测三次完全相同（661/661/661）。
    ///
    /// 🔴 **2026-09-19 晚补：上面那个「完全相同」只对「同一进程内连跑」成立，跨进程不成立。**
    ///    实测（`普查产出_0918/E组_共享资产筛_与EMISSION线索.md` §14.4 ⑧/⑩）：**只重跑 `orig` 那趟**
    ///    （和我们的改动无关），新旧 `sweep_orig.tsv` 各 7657 行里**差 471 行（6.2%）**，
    ///    相对差中位 1.0% / 均值 6.4%，`>10%` 的 74 行；而且**内容越多越容易漂**
    ///    （`sum<10` 是 0.7%，`sum>=1000` 是 **17.2%**）。
    ///    **根因已定**（`SweepStabilityProbe`）：**仿真其实是确定的**（粒子+拖尾几何哈希三次全同），
    ///    漂的是**渲染的 ±1 LSB 浮点/光栅噪声**（两次渲染只差 93~2821 个像素、几乎全是 ±1）。
    ///    单侧自身只漂 0.001~0.16%，**但两侧在不同进程里渲** ⇒ 这个差落进比值被放大。
    ///    ⇒ **修法 = `WFSWEEP_SIDE=both`（同进程两阶段）**，见 `BothSides` 的注释；
    ///      已验证**比值稳定到小数点后四位**。**`EffectCompare` 那条路也是同进程的，同理。**
    ///
    /// 🔴 **2026-09-19 晚改成 `public`**：`EffectCompare` 原来**自己没钉种子**（只 `Stop()` + `Simulate()`），
    ///    于是它出的 iso 数字全带着「每次重播换种子」的噪声 —— 第六～九轮那些「isoN 是 N.N×」的结论
    ///    都是在**不确定的尺子**上量的。判据只此一份，两把尺子共用这个函数。</summary>
    public static void SeedAndReset(ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.useAutoRandomSeed = false;
        ps.randomSeed = FixedSeed;
        ps.Simulate(0f, withChildren: true, restart: true, fixedTimeStep: true);
    }

    public const uint FixedSeed = 20260911;

    static (int lit, double sum, int litS, double sumS) RenderAt(GameObject prefab, float time, Camera cam, string pngPath, Color[] plate)
    {
        var inst = UnityEngine.Object.Instantiate(prefab);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;

        // 编辑器里 Awake 不跑，手动触发 binder
        var binder = inst.GetComponent<WarpforgeVFX.WarpforgeEffectBinder>();
        if (binder != null) binder.Apply();

        // 🔬 实验开关（2026-09-18）：`WFORIG_NOEMIT=1` → 把**这一份实例上**所有材质的 `_EMISSION` 强制关掉。
        //
        //    用途：量「**原版那一趟到底有没有在用 `_EMISSION`**」。
        //    为什么必须直接量：原版材质来自 bundle、关键字原样带着，但「它在渲染时到底参没参与」
        //    光看 `m_ValidKeywords` 判不出来 —— 本轮就为此撞了两次墙（先假定「全局开着⇒两者等效」被
        //    Y 条件推翻，再假定「按 Emission 模块判」被 Z≡Y 推翻）。**关掉再量、看数变不变，是唯一不靠推断的办法。**
        //    判据与数据见 `资料/普查产出_0918/E组_共享资产筛_与EMISSION线索.md` §四。
        if (NoEmissionOnOrig)
        {
            int nm = 0;
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                foreach (var mm in r.sharedMaterials)
                    if (mm != null && mm.HasProperty("_EmissionColor")) { mm.DisableKeyword("_EMISSION"); nm++; }
            if (!_noEmitLogged)
            {
                _noEmitLogged = true;
                Debug.Log(P + $"WFORIG_NOEMIT：本实例关了 {nm} 个材质的 _EMISSION；" +
                          $"Shader.IsKeywordEnabled(\"_EMISSION\")={Shader.IsKeywordEnabled("_EMISSION")}（关完）");
            }
        }

        foreach (var ps in inst.GetComponentsInChildren<ParticleSystem>(true))
        {
            SeedAndReset(ps);
            ps.Simulate(time, withChildren: true, restart: false, fixedTimeStep: true);
            // 这里不需要 ps.Play()：渲染画的是**当前**粒子状态，Simulate 已经把它摆好了。
            // （曾经怀疑 Play() 让粒子跟着墙钟走导致扫描不可复现 —— 实测**不是**这个原因，
            //   真因是随机种子，见 SeedAndReset()。留着 Play() 只是没必要，不是错。）
        }

        // ⚠️ 这里**曾经**放过一句 `RestoreBaseGlobals();`（每次渲染前把全局关键字还原成基准集）。
        //    2026-09-15 实测**没用**（数值纹丝不动），已撤 —— 原因见主流程里那段「试过但没用」。

        var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        // 🔴🔴 **2026-10-02：渲两帧、读第二帧。**
        //
        // **为什么**：`GrabPassTransparentFeature` 抓的是**上一帧**的颜色缓冲，而扫描**每个时刻只渲一帧**
        // ⇒ 抓屏族采到的是**上一个时刻**（甚至上一个效果）留下的拷贝。
        // **实测**（`工具/run_nograb_backdrop.sh`，在棋盘底图尺子上，11 条抓屏效果）：
        //   Feature **开着**时比值 **5.99–14.42** · **关掉后全部塌回 1.00–1.24**
        // ⇒ 那 42 条从「对得上」变「亮度/密度不对」**是尺子的产物，不是 shader 的缺陷**。
        // ⚠️ **旧记录（2026-10-01）说「关掉 Feature 也不行」—— 那是在【空场景】上试的**
        //    （没有可扭曲的内容，关不关都量不出东西）；换了底图尺子之后这一族第一次可判，重试才现出真相。
        //
        // 渲两帧之后：**第一帧**把抓屏填成**当前帧的不透明内容**（底图），**第二帧**才是被测的那一帧。
        // ⚠️ 粒子状态是 `Simulate` 冻结的 ⇒ 两帧**确定相同**，非抓屏效果的读数不变。
        cam.Render();
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;

        if (pngPath != null) File.WriteAllBytes(pngPath, tex.EncodeToPNG());

        var px = tex.GetPixels();
        int lit = 0, litS = 0; double sum = 0, sumS = 0;
        const float thr = 6f / 255f * 3f;
        // 🔴 2026-10-02：判读基准改成**逐像素比底图**（`plate`，有底图时）；没有底图时退回常量 `Bg`
        //    （旧口径，`WFSWEEP_BACKDROP=0`）。两个口径**同一个定义** ——「效果真正改动了哪些像素」，
        //    只是基准从「空场景的常量色」换成「有内容场景的底图」。
        //    `plate` 与 `px` **同一布局**：都是 `ReadPixels(0,0,W,H)` 之后 `GetPixels()` 读回来的。
        //
        // 🔴 同一处顺手修掉一个**一直存在、没被发现**的 bug：sRGB 那两支原来拿 `ToSrgb(像素)`
        //    去比**线性**的 `Bg` 常量 ⇒ 背景自身的 sRGB(≈0.26) 与 `Bg`(0.07) 差 0.19 > 阈值
        //    ⇒ **每一个背景像素都被算成亮点**。实据：老基线 `sweep_orig.tsv` **7664 行全部**
        //    `lit_srgb = 65536`（= 全画幅，见 `资料/比对基线/README.md`）。
        //    `工具/analyze_sweep.py` 一直读的是 `lit`/`sum` 两列（`f[2]`/`f[3]`）⇒ **台账没受影响**，
        //    但那两列是完全退化的。现在两边都按**同一色彩空间**比 ⇒ 那两列第一次可用。
        for (int i = 0; i < px.Length; i++)
        {
            var c = px[i];
            float br = (plate != null) ? plate[i].r : Bg.r;
            float bgc = (plate != null) ? plate[i].g : Bg.g;
            float bbc = (plate != null) ? plate[i].b : Bg.b;
            // 🔴 **2026-10-02 第二处口径修正：`sum` 从「亮度和」改成「相对底图的贡献和 `Σ|Δ|`」。**
            //
            //   **为什么必须改**（判据 → `资料/特效还原_进度与交接.md` §P1-a0·附一）：
            //   原来 `sum += c.r + c.g + c.b` 把**底图自己的亮度也算进去了** —— 而底图两侧**完全相同**
            //   ⇒ 它在比值里是个**常数偏置**，把比值**系统性拉向 1**。底图越亮、偏置越大：
            //   实据 `BulletImpact_AcidSpit`（同一份资产、同一个 shader）**只换底图亮度**：
            //   黑/白底图 **0.99** → 低对比底图 **0.63**。
            //   ⇒ 台账里那一簇 0.63–0.69（`Psychic_Lightning_*` / `Pulse Onslaught` /
            //     `Relentless Fusillade` …共 9 条）**是这把尺子量出来的、不是效果本身的性质**。
            //
            //   **改成什么**：`Σ|像素 − 底图|`（逐通道取绝对值再求和），**只统计同上面那批 `lit` 像素**
            //   —— 与 `EffectIso` 的 `contrib = Σ|像素−背景|/255` **同一个定义**（那边除了 255，
            //   比值里是个常数因子，不影响判定）。两把尺子口径就此统一。
            //
            //   ⚠️ **为什么取绝对值、不取有符号差**：扭曲族（抓屏）会**同时**提亮一部分、压暗一部分，
            //     有符号求和会**互相抵消**到接近 0 ⇒ 比值变成噪声。取 |Δ| 对四族都成立。
            //   ⚠️ **只统计 `lit` 像素**（不是全画幅）：全画幅会把 ±1 LSB 的光栅噪声累加 65536 次
            //     （≈260），**弱效果会被噪声淹没**（同 `sweep_frames` 那条「判定级噪声底」的教训）。
            //   ⚠️ 定义变了 ⇒ **新旧 `sweep_{orig,exp}.tsv` 不可比**，必须两趟全量重扫。
            if (Mathf.Abs(c.r - br) + Mathf.Abs(c.g - bgc) + Mathf.Abs(c.b - bbc) > thr)
            {
                lit++;
                sum += Mathf.Abs(c.r - br) + Mathf.Abs(c.g - bgc) + Mathf.Abs(c.b - bbc);
            }

            float r = ToSrgb(c.r), g = ToSrgb(c.g), b = ToSrgb(c.b);
            float sr = ToSrgb(br), sg = ToSrgb(bgc), sb = ToSrgb(bbc);
            if (Mathf.Abs(r - sr) + Mathf.Abs(g - sg) + Mathf.Abs(b - sb) > thr)
            {
                litS++;
                sumS += Mathf.Abs(r - sr) + Mathf.Abs(g - sg) + Mathf.Abs(b - sb);
            }
        }

        UnityEngine.Object.DestroyImmediate(tex);
        RenderTexture.ReleaseTemporary(rt);
        UnityEngine.Object.DestroyImmediate(inst);
        return (lit, sum, litS, sumS);
    }

    /// <summary>线性 → sRGB</summary>
    static float ToSrgb(float c)
    {
        if (c <= 0f) return 0f;
        return c <= 0.0031308f ? c * 12.92f : 1.055f * Mathf.Pow(c, 1f / 2.4f) - 0.055f;
    }

    static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Replace('/', '_').Trim();
    }

    struct CamFrame { public Vector3 pos, look; public float fov, near, far; }

    /// <summary>取景用「整个时间轴上的最大包围盒」，保证所有采样时刻都框得住。
    /// 原版/导出共用同一个取景 —— 各自构图会让亮度差失去可比性（踩过）</summary>
    static CamFrame FrameCamera(GameObject prefab)
    {
        var tmp = UnityEngine.Object.Instantiate(prefab);
        tmp.transform.position = Vector3.zero;
        tmp.transform.rotation = Quaternion.identity;

        Bounds b = new Bounds(Vector3.zero, Vector3.one); bool first = true;
        foreach (var t in Times)
        {
            foreach (var ps in tmp.GetComponentsInChildren<ParticleSystem>(true))
            {
                SeedAndReset(ps);
                ps.Simulate(t, withChildren: true, restart: false, fixedTimeStep: true);
            }
            foreach (var r in tmp.GetComponentsInChildren<Renderer>(true))
            {
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
        }
        float radius = Mathf.Max(0.5f, b.extents.magnitude);
        const float fov = 40f;
        float dist = radius / Mathf.Tan(Mathf.Deg2Rad * fov * 0.5f) * 1.15f;
        var f = new CamFrame
        {
            look = b.center,
            pos = b.center + new Vector3(0f, 0f, -dist),
            fov = fov,
            near = 0.01f,
            far = radius * 100f,
        };
        UnityEngine.Object.DestroyImmediate(tmp);
        return f;
    }
}
