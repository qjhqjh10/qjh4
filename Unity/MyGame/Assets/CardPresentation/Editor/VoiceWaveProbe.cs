// VoiceWaveProbe.cs — 音波线（原版 `AudioWaveEqualizer` + `Shader Graphs/Equalizer`）的定点探针
// （2026-10-23 · 特效线 `UI·音波线`）
//
// 为什么要它：全量自检一轮 20 分钟，而这条线的对错是**可以逐个像素量出来**的 ——
// 而且「看起来像」在这里特别会骗人（旧近似就是「看着像波动」通过了所有断言）。
// 本探针把原版**字节码里那几条算式**当成期望值，逐条量：
//
//   A 段（CPU，不需要 GPU）：`VoiceWaveDriver` = 原版 `AudioWaveEqualizer.Update` 的复刻
//       A1 三态·直线（全 0 频谱）→ Movement 必须停在 0，材质 `_MovementRange` 也是 0
//       A2 三态·有音频          → 饱和到 1
//       A3 三态·静音（从有声落）→ 按 `dt*speedChange` 平滑下降（**不是瞬跳**）
//       A4 灭自证①「16 个频段」 → 第 16..63 bin 灌 1000，读数必须只由**前 16** 决定（0.24，不是 1.0/15.0）
//       A5 灭自证②「clamp」     → 16 bin ×15 = 240，读数必须是 **1**（不是 240）
//       A6 灭自证③「平滑」      → 一步 k=0.25 时读数必须是 **0.25**（不是 1）
//       A7 构造默认值           → `spectrumModifier=10` / `speedChange=10` / 数组 64（原版 `.ctor`）
//       A8 `audioSource == null` 且没喂假频谱 ⇒ 早退（不抛、不动读数）
//
//   B 段（GPU，渲到 RT 读像素）：`CardPresentation/VoiceWave` = 原版 ps_4_0 那 37 条指令的复刻
//       ⚠️ 读的是 **alpha 通道**：原版 `o.a = v * color.a²`，`color.a = 1` 时 alpha **就是**公式里的 `v`。
//          （RGB 被 `Blend SrcAlpha OneMinusSrcAlpha` 预乘过，别拿它当 v 用。）
//       B1 直线态：`_MovementRange=0` ⇒ alpha == `base.b`（0.25）；换噪声/换 `_XOffsets`/`_WaveScaleRemap` 都不许变
//       B2 四次方：m=0.5、R=1、B=0 ⇒ alpha == 0.125（= m⁴×(1+1)）
//       B3 四次方（反证）：m=0.25 ⇒ alpha == 0.0078（**m² 会是 0.0625**，差 8 倍）
//       B4 v 缩放：带只在 v∈[0.82,0.84]，m=1、噪声=白 ⇒ 亮带应出现在 v≈0.5497
//          （= 0.5 + (0.83-0.5)/6.635，6.635 = `m*(0.5*13.27+0-20)+20`）
//       B5 顶点色**乘两次**：B=1、顶点色 0.5 ⇒ RGB == 0.25（乘一次会是 0.5）
//       B6 噪声通道不对称：tap1 用 `_Noise2.r`、tap2 用 `_Noise1.b`（原版指令 6 / 19）
//          ⇒ 噪声图 R=0、B=u 时，alpha 必须随 u 变（若错用 `.r` 则与 u 无关）
//
// 入口（必须 UTF-8 控制台）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod VoiceWaveProbe.Run -logFile -
//   退出码 = 0 全过 / 1 有失败（`Application.isBatchMode` 时才 Exit）。
using CardPresentation;
using UnityEditor;
using UnityEngine;

public static class VoiceWaveProbe
{
    const string P = "WAVEPROBE ";
    static int _pass, _fail;

    // ════════════════════════════════════════════════════════════════════════════════════════
    // 入口
    // ════════════════════════════════════════════════════════════════════════════════════════
    public static void Run()
    {
        _pass = _fail = 0;
        Debug.Log($"{P}==== 音波线探针开始（driver=原版 AudioWaveEqualizer.Update · shader=原版 ps_4_0 37 条）====");

        ProbeDriver();
        ProbeShader();

        Debug.Log($"{P}==== 合计：通过 {_pass} · 失败 {_fail} ====");
        if (Application.isBatchMode) EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // A 段：驱动器（纯 CPU）
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void ProbeDriver()
    {
        Debug.Log($"{P}---- A 段：VoiceWaveDriver（原版 Update.c 逐句）----");

        var go = new GameObject("wave_probe_drv");
        var mr = go.AddComponent<MeshRenderer>();
        var mat = VoiceWaveMaterial.Create();
        if (mat == null) { Fail("A0 建不出材质（shader `" + VoiceWaveMaterial.ShaderName + "` 找不到）"); return; }
        mr.sharedMaterial = mat;

        var drv = go.AddComponent<VoiceWaveDriver>();
        drv.TargetRenderer = mr;
        drv.Initialize(null);                 // 原版 Initialize(AudioSource)；探针不给真音源

        // A7 构造默认值（原版 .ctor：0x41200000 / 0x41200000 / new float[64]）
        Check("A7 默认 spectrumModifier = 10", Mathf.Approximately(drv.SpectrumModifier, 10f), drv.SpectrumModifier);
        Check("A7 默认 speedChange = 10", Mathf.Approximately(drv.SpeedChange, 10f), drv.SpeedChange);
        Check("A7 频谱数组长度 = 64", VoiceWaveDriver.SpectrumLength == 64, VoiceWaveDriver.SpectrumLength);
        Check("A7 参与求和的频段数 = 16", VoiceWaveDriver.Bins == 16, VoiceWaveDriver.Bins);
        Check("A7 窗函数 = Hamming(2)", (int)VoiceWaveDriver.Window == 2, (int)VoiceWaveDriver.Window);
        Check("A7 属性名 = _MovementRange", VoiceWaveDriver.MaterialProperty == "_MovementRange",
              VoiceWaveDriver.MaterialProperty == "_MovementRange" ? 1f : 0f);

        // 🔴 `Initialize` 会把材质换成**实例**（原版 `new Material(image.material)`）——
        //    之后 driver 写的是 `drv.InstanceMaterial`，**不是**传进去那份 `mat`。
        var inst = drv.InstanceMaterial;
        if (inst == null) { Fail("A0 Initialize 之后没有实例材质"); Object.DestroyImmediate(go); return; }
        Check("A0 Initialize 建了实例材质（原版 new Material）", inst != mat, 1f);
        Check("A0 Initialize 之后组件是关的（原版 set_enabled(this,0)）", !drv.enabled, 1f);

        // 场景实例的调参（`MonoBehaviour_4115.json`：15 / 10）
        drv.SpectrumModifier = 15f;
        drv.SpeedChange = 10f;

        // A8 `audioSource == null` 且没有假频谱 ⇒ 早退
        drv.SpectrumOverride = null;
        float before8 = drv.Movement;
        drv.Tick(0.1f);
        Check("A8 audioSource=null 时早退（读数不动）", Mathf.Approximately(drv.Movement, before8), drv.Movement);

        // ── A1 三态·直线 ────────────────────────────────────────────────────────────────
        drv.SpeedChange = 10f;
        drv.SpectrumOverride = Zero();        // 全 0 = 静音/直线
        for (int i = 0; i < 30; i++) drv.Tick(0.1f);        // k = clamp01(1.0) = 1 ⇒ 一步到位
        Check("A1 直线态 Movement = 0", Mathf.Abs(drv.Movement) < 1e-6f, drv.Movement);
        Check("A1 直线态材质 _MovementRange = 0",
              Mathf.Abs(inst.GetFloat("_MovementRange")) < 1e-6f, inst.GetFloat("_MovementRange"));

        // ── A2 三态·有音频 ──────────────────────────────────────────────────────────────
        var loud = new float[64];
        for (int i = 0; i < 16; i++) loud[i] = 0.01f;        // Σ16 = 0.16；×15 = 2.4 ⇒ clamp 1
        drv.SpectrumOverride = loud;
        for (int i = 0; i < 30; i++) drv.Tick(0.1f);
        Check("A2 有音频 Movement 饱和到 1", Mathf.Abs(drv.Movement - 1f) < 1e-6f, drv.Movement);
        Check("A2 有音频材质 _MovementRange = 1",
              Mathf.Abs(inst.GetFloat("_MovementRange") - 1f) < 1e-6f, inst.GetFloat("_MovementRange"));

        // ── A3 三态·静音（从 1 落下，k=0.5 ⇒ 一步到 0.5）──────────────────────────────────
        drv.SpeedChange = 10f;
        drv.SpectrumOverride = Zero();
        drv.Tick(0.05f);                                    // k = clamp01(0.05*10) = 0.5
        Check("A3 静音一步后 Movement = 0.5（平滑，不是瞬跳 0）", Mathf.Abs(drv.Movement - 0.5f) < 1e-5f, drv.Movement);
        for (int i = 0; i < 40; i++) drv.Tick(0.05f);
        Check("A3 持续静音最终落到 0", Mathf.Abs(drv.Movement) < 1e-4f, drv.Movement);

        // ── A4 灭自证①：只加前 16 个 bin ─────────────────────────────────────────────────
        var only16 = new float[64];
        for (int i = 0; i < 16; i++) only16[i] = 0.001f;     // Σ16 = 0.016；×15 = 0.24
        for (int i = 16; i < 64; i++) only16[i] = 1000f;     // 后 48 个是**毒药**：算进去就一定 ≠ 0.24
        drv.SpeedChange = 1000f;                             // k → 1（一步到位，只测「求和口径」）
        drv.SpectrumOverride = only16;
        drv.Tick(0.1f);
        Check("A4 只求前 16 bin（=0.24；算上后 48 个会是 1.0）", Mathf.Abs(drv.Movement - 0.24f) < 1e-4f, drv.Movement);
        Check("A4 反证：不是「只取 1 个 bin」（那会是 0.015）", Mathf.Abs(drv.Movement - 0.015f) > 1e-3f, drv.Movement);

        // ── A5 灭自证②：clamp01 ─────────────────────────────────────────────────────────
        var huge = new float[64];
        for (int i = 0; i < 16; i++) huge[i] = 1f;           // Σ16 = 16；×15 = 240
        drv.SpeedChange = 1000f;
        drv.SpectrumOverride = huge;
        drv.Tick(0.1f);
        Check("A5 clamp01 后 = 1（没 clamp 会是 240）", Mathf.Abs(drv.Movement - 1f) < 1e-6f, drv.Movement);

        // ── A6 灭自证③：平滑（k = dt*speedChange）───────────────────────────────────────
        drv.SpectrumOverride = Zero(); drv.SpeedChange = 1000f; drv.Tick(0.1f);     // 先归零
        Check("A6 归零后 Movement = 0", Mathf.Abs(drv.Movement) < 1e-6f, drv.Movement);
        drv.SpectrumOverride = loud; drv.SpeedChange = 1f;                           // 目标 = 1
        drv.Tick(0.25f);                                     // k = clamp01(0.25*1) = 0.25 ⇒ 0 → 0.25
        Check("A6 一步 k=0.25 后 Movement = 0.25（去掉平滑会是 1）", Mathf.Abs(drv.Movement - 0.25f) < 1e-5f, drv.Movement);

        drv.SpectrumOverride = null;
        Object.DestroyImmediate(go);
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // B 段：shader（渲到 RT 读 alpha）
    // ════════════════════════════════════════════════════════════════════════════════════════
    static void ProbeShader()
    {
        Debug.Log($"{P}---- B 段：CardPresentation/VoiceWave（原版 ps_4_0 逐条）----");

        var white = Solid(1f, 1f, 1f, 1f);            // 噪声 = 白（n.r = n.b = 1）
        var texB025 = Solid(1f, 1f, 0.25f, 1f);        // base.b = 0.25、R = 1
        var texR1B0 = Solid(1f, 1f, 0f, 1f);           // base.b = 0、tap.r = 1

        // ── B1 直线态：alpha == base.b，且与噪声/其它属性无关 ─────────────────────────────
        {
            var m = VoiceWaveMaterial.Create(texB025);
            m.SetFloat("_MovementRange", 0f);
            m.SetTexture("_Noise", white);
            var px = Render(m, 64, 64, Color.white);
            float a = px[32, 32].a;
            Check("B1 m=0 ⇒ alpha == base.b (0.25)", Near(a, 0.25f), a);

            m.SetVector("_XOffsets", new Vector4(0f, -0.5f, 0f, 0f));
            m.SetVector("_WaveScaleRemap", new Vector4(1f, 3f, 0f, 0f));
            m.SetTexture("_Noise", Solid(0.2f, 0.2f, 0.2f, 1f));
            float a2 = Render(m, 64, 64, Color.white)[32, 32].a;
            Check("B1 m=0 时换 XOffsets/WaveScaleRemap/噪声都不许变", Near(a2, 0.25f), a2);
            Object.DestroyImmediate(m);
        }

        // ── B2/B3 四次方 ────────────────────────────────────────────────────────────────
        {
            var m = VoiceWaveMaterial.Create(texR1B0);
            m.SetTexture("_Noise", white);
            m.SetFloat("_MovementRange", 0.5f);
            float a = Render(m, 64, 64, Color.white)[32, 32].a;
            Check("B2 m=0.5 ⇒ alpha = m⁴×2 = 0.125", Near(a, 0.125f), a);

            m.SetFloat("_MovementRange", 0.25f);
            float a3 = Render(m, 64, 64, Color.white)[32, 32].a;
            Check("B3 m=0.25 ⇒ alpha = 0.0078（m² 会是 0.0625）", Near(a3, 0.0078125f, 3f / 255f), a3);
            Object.DestroyImmediate(m);
        }

        // ── B4 v 缩放：亮带位置必须符合 `m*(0.5*Δ+Wx-20)+20` ──────────────────────────────
        {
            // `_MainTex`：R=1 只在 v ∈ [0.82, 0.84]（128 行里 105..107 行），B=0
            const int S = 128;
            var band = new Texture2D(S, S, TextureFormat.RGBA32, false, true);
            var cols = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                float v = (y + 0.5f) / S;
                bool inBand = v >= 0.82f && v <= 0.84f;
                for (int x = 0; x < S; x++) cols[y * S + x] = new Color(inBand ? 1f : 0f, 0f, 0f, 1f);
            }
            band.SetPixels(cols); band.filterMode = FilterMode.Point; band.wrapMode = TextureWrapMode.Clamp; band.Apply();

            var m = VoiceWaveMaterial.Create(band);
            m.SetTexture("_Noise", white);
            m.SetFloat("_MovementRange", 1f);        // s = 1*(0.5*13.27 + 0 - 20) + 20 = 6.635
            const int H = 256;
            var px = Render(m, 16, H, Color.white);
            int peak = -1; float best = -1f;
            for (int y = 0; y < H; y++) { float a = px[8, y].a; if (a > best) { best = a; peak = y; } }
            float peakV = (peak + 0.5f) / H;
            // 期望 0.5 + (0.83 - 0.5)/6.635 = 0.5497（若缩放是 20 则是 0.5165；若没缩放是 0.83）
            Check("B4 亮带位置 v≈0.5497（缩放 = 6.635）", Mathf.Abs(peakV - 0.5497f) < 0.012f, peakV);
            Check("B4 反证：亮带不在「无缩放」的 0.83", Mathf.Abs(peakV - 0.83f) > 0.05f, peakV);
            Object.DestroyImmediate(m); Object.DestroyImmediate(band);
        }

        // ── B5 顶点色乘两次 ─────────────────────────────────────────────────────────────
        {
            var m = VoiceWaveMaterial.Create(Solid(1f, 1f, 1f, 1f));   // base.b = 1 ⇒ v = 1（m=0）
            m.SetFloat("_MovementRange", 0f);
            m.SetTexture("_Noise", white);
            var px = Render(m, 32, 32, new Color(0.5f, 0.5f, 0.5f, 1f));
            var c = px[16, 16];
            // o.rgb = v * color.rgb² = 0.25；Blend SrcAlpha ⇒ RT.rgb = 0.25*1 = 0.25
            Check("B5 顶点色乘两次 ⇒ RGB = 0.25（乘一次会是 0.5）", Near(c.r, 0.25f, 3f / 255f), c.r);
            Check("B5 顶点色 alpha=1 ⇒ alpha 仍是 1", Near(c.a, 1f, 2f / 255f), c.a);
            Object.DestroyImmediate(m);
        }

        // ── B6 噪声通道不对称：tap1 用 _Noise2.r、tap2 用 _Noise1.b ────────────────────────
        {
            // 噪声图：R 恒 0、B = u（逐列 0→1）；`_Noise1` 的 uv 缩放 = (1,0) ⇒ n1.b = u
            const int N = 256;
            var noise = new Texture2D(N, N, TextureFormat.RGBA32, false, true);
            var nc = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++) nc[y * N + x] = new Color(0f, 0f, x / (float)(N - 1), 1f);
            noise.SetPixels(nc); noise.filterMode = FilterMode.Bilinear; noise.wrapMode = TextureWrapMode.Clamp; noise.Apply();

            // 底图：R = v（逐行 0→1）、B = 0
            const int S2 = 256;
            var grad = new Texture2D(S2, S2, TextureFormat.RGBA32, false, true);
            var gc = new Color[S2 * S2];
            for (int y = 0; y < S2; y++)
            {
                float v = (y + 0.5f) / S2;
                for (int x = 0; x < S2; x++) gc[y * S2 + x] = new Color(v, 0f, 0f, 1f);
            }
            grad.SetPixels(gc); grad.filterMode = FilterMode.Bilinear; grad.wrapMode = TextureWrapMode.Clamp; grad.Apply();

            var m = VoiceWaveMaterial.Create(grad);
            m.SetTexture("_Noise", noise);
            m.SetVector("_Noise1ScaleAndSpeed", new Vector4(1f, 0f, 0f, 0f));   // n1 = 噪声(u,0) ⇒ .b = u
            m.SetVector("_Noise2ScaleAndSpeed", new Vector4(0f, 0f, 0f, 0f));   // n2 = 噪声(0,0) ⇒ .r = 0
            m.SetFloat("_MovementRange", 1f);

            const int W6 = 200, H6 = 100;
            var px = Render(m, W6, H6, Color.white);
            // 期望（v=0 那一行）：tap1: s1 = 0*(…)+20 → 但 m=1 ⇒ s1 = 1*(0.5*13.27*0 + 0 - 20) + 20 = 0 ⇒ tap1.r = 0.5
            //                     tap2: s2 = 1*(0.5*13.27*u - 20) + 20 = 6.635u ⇒ tap2.r = saturate(6.635u*(-0.5)+0.5)
            int y0 = 0;                                  // 最下面一行（v≈0.005）
            float a10 = px[Mathf.RoundToInt(0.10f * W6), y0].a;
            float a05 = px[Mathf.RoundToInt(0.05f * W6), y0].a;
            // 正确：0.5 + saturate(0.5 - 3.3175u) ⇒ u=0.1 → 0.668 ; u=0.05 → 0.834
            Check("B6 u=0.10 处 alpha ≈ 0.668（tap2 用了 _Noise1.b）", Near(a10, 0.668f, 0.03f), a10);
            Check("B6 u=0.05 处 alpha ≈ 0.834", Near(a05, 0.834f, 0.03f), a05);
            Check("B6 alpha 随 u 变（若错用 .r，两处都会是 1.0 且与 u 无关）",
                  Mathf.Abs(a10 - a05) > 0.05f, a10 - a05);
            Object.DestroyImmediate(m); Object.DestroyImmediate(noise); Object.DestroyImmediate(grad);
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════════════
    // 渲染工具：一块 1×1 的 quad 正好铺满正交相机 → 读回 Color[,]
    // ════════════════════════════════════════════════════════════════════════════════════════
    static Color[,] Render(Material mat, int W, int H, Color vertexColor)
    {
        var root = new GameObject("wave_probe_quad");
        var mf = root.AddComponent<MeshFilter>();
        var mr = root.AddComponent<MeshRenderer>();
        mf.sharedMesh = QuadMesh(vertexColor);
        // 🔴 正交相机按 `W/H` 取景；quad 是 ±0.5 的方片 ⇒ 宽屏（如 200×100）时它只占中间一半，
        //    B6 的 UV.x=0.05/0.10 会落到**quad 外**（读到 clear alpha=0）。按宽高比把 quad 横向拉满。
        root.transform.localScale = new Vector3((float)W / H, 1f, 1f);
        mr.sharedMaterial = mat;

        var camGo = new GameObject("wave_probe_cam");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.orthographic = true;
        cam.orthographicSize = 0.5f;                 // quad 恰好占满
        cam.transform.position = new Vector3(0f, 0f, -1f);
        cam.allowHDR = false;
        cam.allowMSAA = false;

        var rt = new RenderTexture(W, H, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        rt.antiAliasing = 1;
        rt.Create();
        cam.targetTexture = rt;
        cam.Render();

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false, true);   // linear：别做 sRGB 转换
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        var px = new Color[W, H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++) px[x, y] = tex.GetPixel(x, y);

        Object.DestroyImmediate(tex);
        rt.Release(); Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camGo);
        Object.DestroyImmediate(root);
        return px;
    }

    static Mesh QuadMesh(Color c)
    {
        var m = new Mesh { name = "wave_probe_quad" };
        m.vertices = new[] {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
        };
        m.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
        m.colors = new[] { c, c, c, c };
        m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        m.RecalculateBounds();
        return m;
    }

    static Texture2D Solid(float r, float g, float b, float a)
    {
        var t = new Texture2D(4, 4, TextureFormat.RGBA32, false, true);
        var c = new Color[16];
        for (int i = 0; i < 16; i++) c[i] = new Color(r, g, b, a);
        t.SetPixels(c);
        t.filterMode = FilterMode.Point;
        t.wrapMode = TextureWrapMode.Clamp;
        t.Apply();
        return t;
    }

    static float[] Zero() { return new float[64]; }

    static bool Near(float a, float b, float tol = 2f / 255f) { return Mathf.Abs(a - b) <= tol; }

    static void Check(string what, bool ok, float measured)
    {
        if (ok) { _pass++; Debug.Log($"{P}PASS {what}   [实测 {measured:F5}]"); }
        else { _fail++; Debug.LogError($"{P}FAIL {what}   [实测 {measured:F5}]"); }
    }

    static void Fail(string what) { _fail++; Debug.LogError($"{P}FAIL {what}"); }
}
