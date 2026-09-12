// VideoProbe.cs — 结算「开门」视频自检
//
// 要回答两个问题，都不许猜：
//   ① **Unity 怎么认这份视频？** 源文件 `素材/Warpforge原版/视频/videos/{Victory,Defeat,Draw} Video.mp4`
//      实为 **matroska/webm + VP8**、**3840×1080 / yuv420p**（ffprobe 实测，没有 alpha 通道）；
//      左半 = 彩色正片，右半 = 同一形状的白色剪影（明度即 alpha）。
//      原版 `VideoClip` 元数据是 `Width:1920 Height:1080 m_HasSplitAlpha:true` —— 那是
//      **原版导入时已经拆好的产物**；我们自己导入同一份字节，Unity 报的是 **3840×1080**
//      （`Run()` 断言这条）：**Unity 不替我们拆**，合成得自己做（`VideoSplitAlpha.shader`）。
//   ② **批处理下 `VideoPlayer` 到底能不能出帧？** 这个工程的批处理自检是 `-executeMethod`
//      调静态方法，**没有 play 循环**（`BattleDriver.Update` 不跑）。
//      实测：**能，但不能阻塞主线程** —— 阻塞版（`Thread.Sleep` 泵）拿到的一直是
//      `isPrepared=False / frame=-1`；改成注册 `EditorApplication.update` 让编辑器环路自己转之后，
//      `Prepare` 通过、`Play` 出帧、RT 里真的解出了画面。所以 `Play()` 走**非阻塞**那条路。
//
// 用法：
//   # ① 元数据（阻塞、秒回）—— 可以带 -quit
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
//     -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod VideoProbe.Run \
//     -logFile "d:/4/_tmp_view/videoprobe.log"
//   # ② 真播 + 合成 + 像素断言（非阻塞，约 30 秒）—— **不能带 -quit**，退出自己调 EditorApplication.Exit
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
//     -batchmode -projectPath "D:\4\Unity\MyGame" -executeMethod VideoProbe.Play \
//     -logFile "d:/4/_tmp_view/videoplay.log"
//   筛输出：grep "^VP "；图在 d:/4/_tmp_view/video/
using System.IO;
using CardPresentation;
using UnityEngine;
using UnityEngine.Video;

public static class VideoProbe
{
    const string P = "VP ";
    const string Root = "Art/videos/";
    const string OutDir = @"d:/4/_tmp_view/video";

    /// <summary>源文件实际是 3840×1080（左右拼）。**不是** 1920 —— 见文件头。</summary>
    const int SourceWidth = 3840;
    const int SourceHeight = 1080;

    static int _pass, _fail;

    static void Ok(string msg) { _pass++; Debug.Log(P + "[通过] " + msg); }
    static void No(string msg) { _fail++; Debug.Log(P + "[失败] " + msg); }

    static void Assert(bool cond, string okMsg, string failMsg)
    {
        if (cond) Ok(okMsg); else No(failMsg);
    }

    // ==================================================================
    //  ① 元数据（阻塞、秒回）
    // ==================================================================
    public static void Run()
    {
        _pass = _fail = 0;

        Check("victory", BattleDoors.Result.Victory);
        Check("defeat",  BattleDoors.Result.Defeat);
        Check("draw",    BattleDoors.Result.Draw);

        Debug.Log(P + $"=== 结束：{_pass}/{_pass + _fail} 全过 {(_fail == 0 ? "✅" : "❌")} ===");
        Quit(_fail == 0 ? 0 : 1);
    }

    static void Check(string name, BattleDoors.Result r)
    {
        var clip = Resources.Load<VideoClip>(Root + name);
        if (clip == null)
        {
            No($"{name}：Resources 里没有 `{Root}{name}` —— 视频是原版资产，放在 gitignore 掉的 " +
               "`Resources/Art/videos/` 下，缺了就是没拷进来");
            return;
        }

        Debug.Log(P + $"{name}: {clip.width}×{clip.height}  {clip.frameRate}fps  " +
                       $"{clip.frameCount} 帧  {clip.length:F2}s  音轨 {(int)clip.audioTrackCount}");

        // 判据：Unity 报 3840 → 它没拆左右拼，合成得我们自己来（这正是 VideoSplitAlpha 存在的理由）
        Assert(clip.width == SourceWidth && clip.height == SourceHeight,
               $"{name} 源尺寸 {SourceWidth}×{SourceHeight}（Unity 不替我们拆左右拼 → 走自建合成）",
               $"{name} 尺寸 {clip.width}×{clip.height}，期望 {SourceWidth}×{SourceHeight}");

        float expect;
        BattleDoors.ExpectedLength.TryGetValue(r, out expect);
        Assert(Mathf.Abs((float)clip.length - expect) < 0.05f,
               $"{name} 时长 {clip.length:F2}s ≈ 原版元数据 {expect}s（原版 SetupDoor 返回的就是它，用来定等待）",
               $"{name} 时长 {clip.length:F2}s，期望 {expect}s");

        // 没有音轨 → 播放时关掉 audioOutputMode 是对的，不是漏了
        Assert((int)clip.audioTrackCount == 0,
               $"{name} 音轨 0 条（所以 BattleDoors 关掉 audioOutputMode）",
               $"{name} 有 {(int)clip.audioTrackCount} 条音轨 —— 和预期不符，要重查");

        // 帧数和原版元数据对得上 → 证明确实是同一份源
        int expectFrames = r == BattleDoors.Result.Draw ? 85 : (r == BattleDoors.Result.Victory ? 89 : 163);
        Assert((int)clip.frameCount == expectFrames,
               $"{name} {clip.frameCount} 帧 = 原版元数据 {expectFrames} 帧（同一份源）",
               $"{name} {clip.frameCount} 帧，原版元数据是 {expectFrames} 帧");
    }

    // ==================================================================
    //  ② 真播 + 合成 + 像素断言（非阻塞，约 30 秒）
    // ==================================================================
    static VideoPlayer _vp;
    static RenderTexture _rt;
    static ImageQuad _screen;
    static Material _splitMat, _plainMat;
    static Camera _cam;
    static double _t0, _stageT0;
    static int _stage, _ticks;
    static string _name;
    static Camera _panelCam;
    static EndPanel _panel;
    static GameObject _panelRoot;
    static GameObject _field;

    /// <summary>相机可见高（正交 size 5.4 的 2 倍）。quad 按它铺满，量「透出多少」才准。</summary>
    const float ViewHeight = 10.8f;

    public static void Play()
    {
        _pass = _fail = 0;
        Directory.CreateDirectory(OutDir);

        _name = "victory";
        var clip = Resources.Load<VideoClip>(Root + _name);
        if (clip == null) { No("Play: 没找到 victory 视频"); Quit(1); return; }

        _rt = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32);
        _rt.Create();

        var go = new GameObject("VideoProbePlayer");
        _vp = go.AddComponent<VideoPlayer>();
        _vp.playOnAwake = false;
        _vp.isLooping = false;
        _vp.renderMode = VideoRenderMode.RenderTexture;
        _vp.targetTexture = _rt;
        _vp.audioOutputMode = VideoAudioOutputMode.None;
        _vp.clip = clip;

        BuildStage();

        Debug.Log(P + $"Play: {_name} {clip.width}×{clip.height} → RT {_rt.width}×{_rt.height}，" +
                       "开始播（非阻塞，靠编辑器环路推）");
        _vp.Prepare();

        _t0 = _stageT0 = Now();
        _stage = 0;
        _ticks = 0;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += Tick;
#endif
    }

    static double Now()
    {
        return System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency;
    }

    static void Tick()
    {
        _ticks++;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif

        double now = Now();
        if (now - _t0 > 60.0) { No("60 秒超时 —— 视频始终没出帧"); Finish(); return; }

        switch (_stage)
        {
            case 0:   // 等 Prepare
                if (!_vp.isPrepared && now - _stageT0 < 20.0) return;
                Debug.Log(P + $"  ① Prepare ok={_vp.isPrepared}（{now - _stageT0:F2}s / {_ticks} tick）");
                Assert(_vp.isPrepared, "Prepare 通过（批处理里 VideoPlayer 可用）",
                                        "Prepare 失败 —— 批处理下视频起不来");
                _vp.Play();
                _stage = 1; _stageT0 = now;
                return;

            case 1:   // 播到徽章成形的那一帧再停（第 40 帧）
                if (_vp.frame < 40 && now - _stageT0 < 20.0) return;
                Debug.Log(P + $"  ② 播到 frame={_vp.frame} time={_vp.time:F3}");
                Assert(_vp.frame >= 40, "批处理下真的解码出帧了（frame ≥ 40）",
                                        $"只到 frame={_vp.frame}，没解码出来");
                _vp.Pause();
                _stage = 2; _stageT0 = now;
                return;

            case 2:   // 等暂停生效，读 RT 原始像素
                if (now - _stageT0 < 0.8) return;
                RawStats();
                _stage = 3; _stageT0 = now;
                return;

            case 3:   // 走我们的 shader 合成 → 截图 + 断言
                if (now - _stageT0 < 0.3) return;
                _screen.SetMaterial(_splitMat);
                Shot("split");
                _stage = 4; _stageT0 = now;
                return;

            case 4:   // 对照组：不给合成材质（=「没写这个 shader 会怎样」）
                if (now - _stageT0 < 0.3) return;
                _screen.SetMaterial(_plainMat);
                Shot("plain");
                _stage = 5; _stageT0 = now;
                return;

            case 5:   // 搭一个真的 `EndPanel`（走 `LayoutSpace` 那套相机），让它带着视频显示
                if (now - _stageT0 < 0.3) return;
                BuildPanelStage();
                _stage = 6; _stageT0 = now;
                return;

            case 6:   // 等开门视频解码到徽章成形的那一帧（第 1 帧整幅还是黑的，拍出来看不出东西）
                if (_panel.Doors != null && _panel.Doors.DecodedFrames < 40 && now - _stageT0 < 20.0) return;
                Debug.Log(P + $"  ⑤ 结算界面里的开门视频：解到第 {_panel.Doors.DecodedFrames} 帧，" +
                               $"HasVideo={_panel.Doors.HasVideo} 内容层露着={_panel.ContentVisible}");
                Assert(_panel.Doors.HasVideo && _panel.Doors.DecodedFrames >= 40,
                       "结算界面里的开门视频真的解出帧了",
                       $"结算界面里的视频没出帧（只解到 {_panel.Doors.DecodedFrames} 帧）");
                Assert(_panel.ContentVisible, "视频在播时奖励已经在了（原版 SetupDoor 里就是直接 ShowRewards）",
                                               "视频在播但奖励还没出来 —— 又做成了「播完才揭晓」");
                Assert(!_panel.TitleVisible, "结果文字没画（字在视频里，叠上去会重复）",
                                             "结果文字和视频里的 VICTORY 叠上了");
                PanelShot();
                _stage = 7; _stageT0 = now;
                return;

            case 7:
                if (now - _stageT0 < 0.3) return;
                Finish();
                return;
        }
    }

    /// <summary>搭一个最小台子：正交相机 + 一张贴 RT 的 quad（**和 `BattleDoors` 用的是同一套东西**）。</summary>
    static void BuildStage()
    {
        var camGo = new GameObject("VideoProbeCam");
        _cam = camGo.AddComponent<Camera>();
        _cam.orthographic = true;
        _cam.orthographicSize = ViewHeight * 0.5f;
        _cam.aspect = 1920f / 1080f;
        _cam.clearFlags = CameraClearFlags.SolidColor;
        _cam.backgroundColor = new Color(0f, 1f, 0f, 1f);   // 纯绿背板：哪里透出来一眼就能看出来
        camGo.transform.position = new Vector3(0f, 0f, -10f);

        var sh = Shader.Find("CardPresentation/Video Split Alpha");
        if (sh == null) No("找不到 shader `CardPresentation/Video Split Alpha`");
        else _splitMat = new Material(sh);
        _plainMat = new Material(Shader.Find("Sprites/Default"));

        // ⚠️ quad **正好铺满**视口 —— 第一版给了 10 而不是 10.8，四周留了一圈底色，
        //    对照组因此「透出 14.3%」被误判成失败（其实是尺子画错了，不是 shader 错）
        _screen = ImageQuad.Create(null, _rt, Vector3.zero, ViewHeight, new Vector2(0.5f, 0.5f), "probe_screen");
        if (_screen != null)
        {
            _screen.SetAspect(1920f / 1080f);   // RT 是 3840×1080 的拼图，显示区是 1920×1080
            _screen.SetMaterial(_splitMat);
        }
    }

    /// <summary>读 RT 原始像素 —— 判据是「RT 里 alpha 是不是恒为 255」（是的话画面在 RGB 里，右半是画不是通道）。</summary>
    static void RawStats()
    {
        var px = Read(_rt);
        int lowA = 0, nonBlack = 0;
        for (int i = 0; i < px.Length; i++)
        {
            if (px[i].a < 250) lowA++;
            if (px[i].r > 8 || px[i].g > 8 || px[i].b > 8) nonBlack++;
        }
        Debug.Log(P + $"  ③ RT 原始：非黑 {100f * nonBlack / px.Length:F1}%，alpha<250 的 {lowA} 个");
        Assert(lowA == 0, "RT 里 alpha 恒为 255 → 画面在 RGB 里，右半确实是「画」不是通道（所以要自己拆）",
                           $"RT 里有 {lowA} 个像素 alpha<255 —— 和「没有 alpha 通道」的结论矛盾，要重查");
        Assert(nonBlack > px.Length * 0.1f, $"RT 里确实有画面（非黑 {100f * nonBlack / px.Length:F1}%）",
                                            "RT 基本全黑 —— 视频没真的解出来");
    }

    /// <summary>渲染一帧、数绿色（透出来的底色）和非绿（画面）、存图。</summary>
    static void Shot(string tag)
    {
        const int W = 1920, H = 1080;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        _cam.targetTexture = rt;
        _cam.Render();

        var px = Read(rt);
        int green = 0;
        for (int i = 0; i < px.Length; i++)
        {
            bool isGreen = px[i].g > 200 && px[i].r < 60 && px[i].b < 60;
            if (isGreen) green++;
        }
        float gp = 100f * green / px.Length;

        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(Path.Combine(OutDir, "composite_" + tag + ".png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        _cam.targetTexture = null;
        rt.Release();

        Debug.Log(P + $"  [{tag}] 透出底色 {gp:F1}% / 画面 {100f - gp:F1}%   → {OutDir}/composite_{tag}.png");

        if (tag == "split")
        {
            Assert(gp > 5f, $"合成生效：徽章外面透出底色 {gp:F1}%（遮罩黑的地方真的透明了）",
                             $"合成没生效：几乎没有区域透明（透出底色只有 {gp:F1}%）");
            Assert(gp < 95f, $"合成生效：画面没被遮掉（画面占 {100f - gp:F1}%）",
                             $"画面几乎全没了（只剩 {100f - gp:F1}%），alpha 取反了？");
        }
        else
        {
            // 对照：不合成（= 没写这个 shader）时整块是不透明的黑 —— 底色一点透不出来
            Assert(gp < 1f, $"对照组成立：不合成就整块不透明（透出底色 {gp:F1}%）—— 证明合成 shader 确实必要",
                             $"对照组异常：不合成也透出 {gp:F1}% 底色，那这个尺子就没意义了");
        }
    }

    /// <summary>搭真结算界面的台子：`LayoutSpace.Apply` 那套正交相机 + 一块「战场」底板
    /// （有底板才看得出「压暗铺满没有」和「视频盖在压暗上」这两件事）。</summary>
    static void BuildPanelStage()
    {
        if (_screen != null) _screen.gameObject.SetActive(false);

        var camGo = new GameObject("PanelCam");
        _panelCam = camGo.AddComponent<Camera>();
        LayoutSpace.Apply(_panelCam);                  // 正交、可见高 10 单位、相机 z=-20 —— 和真机一致
        // ⚠️ aspect 必须显式给：批处理下没有真实的 Game 视图尺寸，`VisibleWidth` 会按一个
        //    奇怪的宽高比算（实测 4:3），压暗贴图就按 4:3 造，左右各留一条没盖住的缝。
        _panelCam.aspect = 1920f / 1080f;
        _panelCam.clearFlags = CameraClearFlags.SolidColor;
        _panelCam.backgroundColor = new Color(0.05f, 0.06f, 0.09f, 1f);

        // 「战场」底板：故意用亮色，压暗没铺满的话一眼就能看出来。
        // ⚠️ **必须放在 z>0**（面板的压暗层在 z=0，见 `EndPanel.Build` 里那个 `Vector3.zero`）——
        //    第一版放在 z=0 和压暗同层，结果底板盖在压暗上、面板整块看不见，
        //    而那条「看得见视频画面」的断言因为底板自己就是亮的，**空过了**（尺子的假象，又踩一次）。
        _field = new GameObject("fake_field");
        var mr = _field.AddComponent<MeshRenderer>();
        var mf = _field.AddComponent<MeshFilter>();
        var mesh = new Mesh();
        mesh.vertices = new[] { new Vector3(-9f, -5f, 1f), new Vector3(9f, -5f, 1f),
                                new Vector3(9f, 5f, 1f), new Vector3(-9f, 5f, 1f) };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateBounds();
        mf.sharedMesh = mesh;
        var fm = new Material(Shader.Find("Sprites/Default"));
        fm.color = new Color(0.75f, 0.6f, 0.35f, 1f);
        mr.sharedMaterial = fm;

        _panelRoot = new GameObject("EndRoot");
        _panel = EndPanel.Create(_panelRoot.transform);
        _panel.Show(1, 0, 25, 12);   // 赢家 1 = 玩家 0 → 胜利 → 播 Victory 视频
        Debug.Log(P + $"  ⑤ 结算界面：结果「{_panel.ResultText}」片 `{_panel.Doors.Clip}` " +
                       $"片长 {_panel.Doors.Length:F2}s");
    }

    static void PanelShot()
    {
        const int W = 1920, H = 1080;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        rt.Create();
        _panelCam.targetTexture = rt;
        _panelCam.Render();

        var px = Read(rt);
        int bright = 0, fieldColor = 0;   // 视频画面（亮）vs 压暗后的战场（暗）vs 没盖住的底板
        for (int i = 0; i < px.Length; i++)
        {
            if (px[i].r > 120 || px[i].g > 120 || px[i].b > 120) bright++;
            if (px[i].r > 170 && px[i].g > 130 && px[i].g < 175 && px[i].b > 60 && px[i].b < 110) fieldColor++;
        }
        float fp = 100f * fieldColor / px.Length;
        float bp = 100f * bright / px.Length;

        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes(Path.Combine(OutDir, "endpanel_video.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        _panelCam.targetTexture = null;
        rt.Release();

        Debug.Log(P + $"  [结算界面] 亮部（视频画面）占 {bp:F1}%   → {OutDir}/endpanel_video.png");

        // 两头都要卡：全是暗 = 视频没出来；全是亮 = 压暗没铺上（底板直接透出来）
        Assert(fp < 1f, $"压暗把整屏都铺满了（没盖住的底板像素只有 {fp:F1}%）",
                         $"有 {fp:F1}% 的像素是没被压暗盖住的底板 —— 压暗没铺满（宽高比算错了？）");
        Assert(bp > 2f && bp < 95f,
               $"结算界面里看得见视频画面、且战场被压暗（亮部 {bp:F1}%）",
               $"结算界面不对（亮部 {bp:F1}%）：接近 0 = 视频没出来；接近 100 = 压暗没铺上");
    }

    static Color32[] Read(RenderTexture rt)
    {
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        var px = tex.GetPixels32();
        Object.DestroyImmediate(tex);
        return px;
    }

    static void Finish()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= Tick;
#endif
        if (_vp != null) Object.DestroyImmediate(_vp.gameObject);
        if (_screen != null) Object.DestroyImmediate(_screen.gameObject);
        if (_cam != null) Object.DestroyImmediate(_cam.gameObject);
        if (_rt != null) { _rt.Release(); Object.DestroyImmediate(_rt); }
        if (_panelRoot != null) Object.DestroyImmediate(_panelRoot);
        if (_field != null) Object.DestroyImmediate(_field);
        if (_panelCam != null) Object.DestroyImmediate(_panelCam.gameObject);

        Debug.Log(P + $"=== 真播自检结束：{_pass}/{_pass + _fail} 全过 {(_fail == 0 ? "✅" : "❌")} ===");
        Quit(_fail == 0 ? 0 : 1);
    }

    static void Quit(int code)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(code);
#endif
    }
}
