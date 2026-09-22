// ShellRuntime.cs — 游戏外壳（阶段二）的**唯一建界面处**
//
// ============================ 出处（唯一正本） ============================
// `资料/阶段二_Shell_原版规格.md`。下面每个常量后面都写了它出自哪一条。
//
// 本工程建场景的规矩（沿用 `DeckRuntime`/`BattleDriver`）：
//   · **Editor 只开场景 / 跑自检 / 存场景**，绘制全在运行时的 `Build()` 这一处；
//   · `Start()` 里 `if (!_built) Build()` —— **按 Play 的入口和自检必须是同一条路**；
//   · `Build()` 开头**先清空子节点**再建（存盘场景里已有上一次烘进去的对象）。
//
// 🔴 **原版是 UGUI Canvas(ScreenSpaceCamera, planeDistance 100) + 透视相机(fov 40 / near 0.3 / far 1000)**；
//    我们这条线**全线是「正交相机 + 世界空间 mesh(`ImageQuad`) + TMP 世界空间文字(`Label`)」**
//    （`LayoutSpace`：可见高度固定 10 个世界单位，1080 px = 10 单位）。
//    对**平面 UI** 来说画面结果等价（Canvas 本来就不受相机透视影响），差别只在实现方式。
//    原版那几个相机参数记在这里备查，**不发散**：fov 40 · near 0.3 · far 1000 · ClearFlags SolidColor 黑 · HDR+MSAA on。
//
// 🔴 **原版 level0 里 73 个组件的参数导出失败**（正本 §〇 第 1 条）⇒ 标「查不到」的地方**不许用猜测填空**。
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

namespace CardPresentation
{
    /// <summary>游戏外壳：常驻件 + 开场动画 + 进主菜单。挂在 `Shell.unity` 的根对象上。</summary>
    public class ShellRuntime : MonoBehaviour
    {
        public static ShellRuntime Instance { get; private set; }

        public enum Phase { Intro = 0, Fading, Menu }

        // ---- 出处：正本 §一「常驻件表」+ §六「落地清单」----
        /// <summary>压暗层。尺寸实证（运行期 TSV）：左右 205.8×2585.5 @ x=∓960 **常开**；上下 4605×146.3 @ y=∓540 **inactive**。</summary>
        public const float FadeSideW = 205.8f, FadeSideH = 2585.5f, FadeSideX = 960f;
        public const float FadeTopW = 4605f, FadeTopH = 146.3f, FadeTopY = 540f;
        /// <summary>`Loading text` 版式实证（TSV）：1920×48 @ y=70 **常开**。</summary>
        public const float LoadingY = 70f;
        /// <summary>`Progress text` 版式实证（TSV）：1920×48 @ y=21.8，**运行期 inactive**。</summary>
        public const float ProgressY = 21.8f;
        public const float TextBandW = 1920f, TextBandH = 48f;

        /// <summary>开场动画：`VideoPlayer_289.json` 实证 `m_AspectRatio:3`(FitVertically)、`m_PlayOnAwake:1`、`m_WaitForFirstFrame:1`、`m_SkipOnDrop:1`、`m_Looping:0`；同 GO 的 `AudioSource_261` 音量 **0.65**。</summary>
        public const float IntroVolume = 0.65f;
        /// <summary>菜单音乐。`MusicManager` 预制体实证 `musicVolumeMultiplier = 0.2`。</summary>
        public const float MusicVolumeMultiplier = 0.2f;

        public Phase Current { get; private set; } = Phase.Intro;
        public WindowsManager Windows { get; private set; }
        public Shade Shade { get; private set; }
        public BlockingOverlay Blocker { get; private set; }

        Label _loadingText, _progressText;
        ImageQuad[] _fadeSides = new ImageQuad[4];
        VideoPlayer _video;
        AudioSource _videoAudio;
        RenderTexture _videoRt;
        ImageQuad _videoQuad;
        AudioSource _musicA, _musicB;
        bool _built;
        public bool IntroPlaying { get { return Current == Phase.Intro; } }
        public float IntroTime { get { return _video != null ? (float)_video.time : 0f; } }
        public float IntroLength { get { return _video != null && _video.clip != null ? (float)_video.clip.length : 0f; } }

        // ============================================================ 生命周期

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);      // 壳要跨场景活着（进主菜单/战斗都不重建）
        }

        void Start()
        {
            // 按 Play 的入口 —— **和自检是同一条路**（本工程的规矩，见文件头）
            if (!_built) Build();
        }

        void Update()
        {
            if (Current == Phase.Intro) TickIntro(Time.unscaledDeltaTime);
            TickChainCheck();
        }

        // ============================================================ 批处理「真 Play」链验证
        //
        // 🔴 **为什么收尾在运行时侧、而不是编辑器侧**（2026-09-22 实测）：
        //    `ShellScene.Play` 第一版把收尾挂在 `EditorApplication.update` 上，
        //    结果**那个回调在「批处理 + Play 模式」下一次都没触发**（日志里只有运行时的输出、
        //    编辑器侧的 ①②③④ 一条都没有），进程挂到被 `timeout` 杀掉。
        //    ⇒ 收尾必须由**运行时**的 `Update()` 做（`EditorApplication.Exit` 在 `UNITY_EDITOR` 下可用）。
#if UNITY_EDITOR
        /// <summary>批处理跑链验证时置 true（`ShellScene.Play` 在进 Play 前设）。**只在编辑器里存在。**
        /// 🔴 **必须用 `SessionState` 而不是普通静态字段** —— **进 Play 会触发域重载，静态字段全被清空**
        ///    （2026-09-22 踩到：进 Play 前设的 `true` 被抹掉，钩子一次都没跑）。`SessionState` 跨域重载还在。</summary>
        public const string ChainCheckKey = "CardPresentation.ShellChainCheck";

        static bool ChainCheckOn
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.SessionState.GetBool(ChainCheckKey, false);
#else
                return false;
#endif
            }
        }
        float _chainT0 = -1f;

        void TickChainCheck()
        {
            if (!ChainCheckOn) return;
            if (_chainT0 < 0f) { _chainT0 = Time.realtimeSinceStartup; Debug.Log("[Shell] [链验证] 运行时侧已起，开始计时"); }

            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (active == "MainMenu")
            {
                Debug.Log("[Shell] [链验证] ✅ 已切到 `MainMenu` 场景 —— 整条链通了（开场 → 淡入 → 载主菜单）");
                UnityEditor.EditorApplication.Exit(0);
                return;
            }
            if (Time.realtimeSinceStartup - _chainT0 > 40f)
            {
                Debug.LogError("[Shell] [链验证] ✗ 40 秒没切到 MainMenu（当前场景 `" + active + "`）");
                UnityEditor.EditorApplication.Exit(1);
            }
        }
#else
        void TickChainCheck() { }
#endif

        void OnDestroy()
        {
            if (_videoRt != null) { _videoRt.Release(); Destroy(_videoRt); _videoRt = null; }
        }

        // ============================================================ 建

        /// <summary>建整个外壳。**自检与运行时调同一个**（`Start()` 调它）。</summary>
        public void Build()
        {
            _built = true;
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) DestroySafe(root.GetChild(i).gameObject);

            var cam = Camera.main;
            if (cam != null) LayoutSpace.Apply(cam);
            else Debug.LogWarning("[Shell] 场景里没有相机（`Main Camera`）—— 界面对不上屏，先补相机");

            // ---- 音频：两个双源（原版 `MusicManager{_audioSources{_first,_second}}` 的双源交叉淡入）----
            _musicA = MakeMusicSource("Music A");
            _musicB = MakeMusicSource("Music B");

            // ---- 压暗层（四边）----
            var fade = NewRoot(root, "FadeBackground");
            _fadeSides[0] = FadeEdge(fade, "Smooth background fade Left", -FadeSideX, 0f, FadeSideW, FadeSideH, true);
            _fadeSides[1] = FadeEdge(fade, "Smooth background fade Right", FadeSideX, 0f, FadeSideW, FadeSideH, true);
            _fadeSides[2] = FadeEdge(fade, "Smooth background fade Bottom", 0f, -FadeTopY, FadeTopW, FadeTopH, false);
            _fadeSides[3] = FadeEdge(fade, "Smooth background fade Top", 0f, FadeTopY, FadeTopW, FadeTopH, false);

            // ---- 安全区（原版 `UISafeAreaManager{m_safeZones[]}`，挂根上，指向这两个节点）----
            var safe = new GameObject("Safe area All").transform;
            safe.SetParent(root, false);
            var safeH = new GameObject("Safe area Only Horizontal").transform;
            safeH.SetParent(root, false);
            var safeMgr = root.gameObject.AddComponent<UISafeArea>();
            safeMgr.zones = new[]
            {
                new UISafeArea.Zone { rectTransform = safe,  applyWidth = true,  applyHeight = true  },
                new UISafeArea.Zone { rectTransform = safeH, applyWidth = true,  applyHeight = false },
            };

            // ---- 三个窗口锚点 + 窗口管理器（正本 §三 第 7 条：**缺一不可**，原版取不到会 LogError）----
            // 判据只留一处：`WindowsManager.EnsureHost`（单独打开某个界面场景时也走它）
            Windows = WindowsManager.EnsureHost(root);

            // ---- 载入文案（版式实证，**文案本身是本地化词条、本地没有 ⇒ 留空并说一声**）----
            _loadingText = TextBand(root, "Loading text", LoadingY, true);
            _progressText = TextBand(root, "Progress text", ProgressY, false);
            if (_loadingText != null)
                Debug.Log("[Shell] `Loading text` 的文案是本地化词条（`EverguildLocalization`，原版走**远程**语言表）" +
                          "—— 本地没有 ⇒ **留空**，不自己编一个字（正本 §五 第 4 条）");

            // ---- 压暗 / 遮罩 ----
            Shade = NewRoot(root, "Shade").gameObject.AddComponent<Shade>();
            Blocker = NewRoot(root, "BlockingOverlay").gameObject.AddComponent<BlockingOverlay>();

            // ---- 开场动画 ----
            BuildIntro(root);

            // ---- 菜单音乐（照 `MainMenuMusicController.Initialize`：先 main theme，再排队 idle）----
            PlayMenuMusic();
        }

        // ---------------------------------------------------------- 小件

        static Transform NewRoot(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static void DestroySafe(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) { DestroyImmediate(go); return; }
#endif
            Destroy(go);
        }

        AudioSource MakeMusicSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            // 🔴 **源音量只乘那个常量（0.2）** —— 玩家的音量滑块由 mixer 的 `VolumeMusic` 施加
            //    （`WarpforgeAudio.Ensure()`）。**两处都乘就是 0.2 × 滑块²**，滑块一拉小音乐就消失。
            src.volume = MusicVolumeMultiplier;
            if (WarpforgeAudio.Ready) src.outputAudioMixerGroup = WarpforgeAudio.MusicGroup;
            return src;
        }

        /// <summary>`Loading text` / `Progress text`：1920×48 的一条（**只有版式是实证的**，文本组件本身查不到）。</summary>
        Label TextBand(Transform root, string name, float yPx, bool active)
        {
            var t = NewRoot(root, name);
            var lb = Label.Create(t, "", new Vector3(0f, (yPx - 540f) / 108f, 0f), 5,
                                  Color.white, new Vector2(0.5f, 0.5f), name + " Text");
            if (lb != null) lb.SetCapHeight(TextBandH / 108f * 0.6f);
            t.gameObject.SetActive(active);
            return lb;
        }

        /// <summary>一条压暗边。⚠️ 原版那 4 条的**脚本导出失败**（正本 §一）⇒ 只有尺寸是实证的，图是**黑的纯色**（用 `CardArt.Solid()` + tint，和原版「Image 没 sprite 只有 m_Color」等价）。</summary>
        ImageQuad FadeEdge(Transform parent, string name, float cxPx, float cyPx, float wPx, float hPx, bool active)
        {
            var q = ImageQuad.Create(parent, CardArt.Solid(),
                                     new Vector3(cxPx / 108f, cyPx / 108f, 0f), hPx / 108f,
                                     new Vector2(0.5f, 0.5f), name);
            if (q == null) return null;
            q.SetAspect(wPx / hPx);
            q.SetTint(new Color(0f, 0f, 0f, 1f));
            q.SetRenderQueue(3000);
            q.gameObject.SetActive(active);      // 上下两条出厂是关的（实证）
            return q;
        }

        // ---------------------------------------------------------- 开场动画

        void BuildIntro(Transform root)
        {
            var clip = Resources.Load<VideoClip>("Art/videos/intro");
            if (clip == null)
            {
                // 不静默失败：资产不在（`Resources/Art/videos/` 是 gitignore 的）就说清怎么补
                Debug.LogWarning("[Shell] ⚠️ 找不到开场动画 `Resources/Art/videos/intro.mp4` —— " +
                                 "从 `d:/2/新解包资源/assets_full/sharedassets0/VideoClip/Warpforge Intro.mp4` 拷过来即可。" +
                                 "本次**跳过开场**直接进主菜单");
                Current = Phase.Menu;
                LoadMainMenu();
                return;
            }

            _videoRt = new RenderTexture(1920, 1080, 0);
            _videoRt.name = "IntroRT";
            _videoQuad = ImageQuad.Create(root, _videoRt, Vector3.zero, LayoutSpace.DesignHeight,
                                          new Vector2(0.5f, 0.5f), "Intro Video");
            if (_videoQuad != null) _videoQuad.SetRenderQueue(3100);

            var go = NewRoot(root, "Intro Videoplayer").gameObject;
            _video = go.AddComponent<VideoPlayer>();
            _video.clip = clip;
            // 原版是 `m_RenderMode:0`(CameraNearPlane) 直接由相机画；我们走 RenderTexture + 全屏 quad
            //（和结算开门同一套，`Battle/BattleDoors.cs`）—— **画面等价**，且能进我们自己的渲染队列
            _video.renderMode = VideoRenderMode.RenderTexture;
            _video.targetTexture = _videoRt;
            _video.aspectRatio = VideoAspectRatio.FitVertically;   // 实证 `m_AspectRatio:3`
            _video.isLooping = false;                             // 实证 `m_Looping:0`
            _video.skipOnDrop = true;                             // 实证 `m_SkipOnDrop:1`
            _video.waitForFirstFrame = true;                      // 实证 `m_WaitForFirstFrame:1`
            _video.playOnAwake = false;                           // 我们显式 Play（自检要能控制时机）
            _video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoAudio = go.AddComponent<AudioSource>();
            _videoAudio.playOnAwake = false;
            _videoAudio.volume = IntroVolume;                     // 实证 0.65
            _video.SetTargetAudioSource(0, _videoAudio);

            _video.loopPointReached += _ => FinishIntro("播完");
            Current = Phase.Intro;
            if (Application.isPlaying)
            {
                // ⚠️ 批处理/编辑模式**不能碰 VideoPlayer.Play**（会报不支持）⇒ 只在真运行时起播
                _video.Play();
                _video.Pause();     // 批处理下没有帧循环：先停在第一帧，由 `TickIntro` 推（`Play()` 会把它归零，同 `BattleDoors`）
            }
        }

        /// <summary>原版跳过判据：`GameBootController.Update()` 里 `Input.anyKeyDown` **或鼠标左键** ⇒ `time = clip.length - ε`。**没有按钮、没有 UI**。</summary>
        void TickIntro(float dt)
        {
            var kb = Keyboard.current;
            var ms = Mouse.current;
            bool anyKey = (kb != null && kb.anyKey.wasPressedThisFrame)
                       || (ms != null && ms.leftButton.wasPressedThisFrame);
            if (anyKey) { SkipIntro(); return; }

            if (_video == null) return;
            _video.Play();
            if (_video.isPlaying && IntroLength > 0f && _video.time >= IntroLength - 0.02f) FinishIntro("播到结尾");
        }

        /// <summary>跳到结尾（原版 `SkipToVideoToEnd`）。</summary>
        public void SkipIntro()
        {
            if (_video != null && _video.clip != null)
                _video.time = System.Math.Max(0.0, _video.clip.length - 0.05);
            FinishIntro("跳过");
        }

        void FinishIntro(string why)
        {
            if (Current != Phase.Intro) return;
            Debug.Log($"[Shell] 开场动画结束（{why}）→ 淡入主菜单");
            if (_video != null) _video.Stop();
            if (_videoQuad != null) _videoQuad.gameObject.SetActive(false);
            Current = Phase.Fading;
            // ⚠️ 编辑器里（自检那条路）**不能开协程** —— `StartCoroutine` 在 edit mode 会抛。
            //    自检只关心「阶段推进到 Menu」这个事实，淡入那 0.5 秒留给真 Play 验。
            if (!Application.isPlaying)
            {
                if (Shade != null) Shade.SetAlpha(0f);
                Current = Phase.Menu;
                LoadMainMenu();
                return;
            }
            StartCoroutine(FadeThenMenu());
        }

        IEnumerator FadeThenMenu()
        {
            // 原版 `VideoFinished()` 是 `DOFade(loginScreenManager.canvasGroup, …)` 淡入登录界面；
            // **我们不做登录**（单机没有账号）⇒ 直接淡入主菜单（正本 §二 第 6 条）
            if (Shade != null)
            {
                Shade.SetAlpha(1f);
                yield return Shade.FadeTo(0f, 0.5f);
            }
            Current = Phase.Menu;
            LoadMainMenu();
        }

        /// <summary>进主菜单。主菜单场景还没建时**说清楚**，不是静默停住。</summary>
        public void LoadMainMenu()
        {
            const string scene = "MainMenu";
            if (!Application.isPlaying)
            {
                Debug.Log("[Shell] （编辑器里不切场景）");
                return;
            }
            if (Application.CanStreamedLevelBeLoaded(scene))
            {
                // 🔴 **接法（我们挑的）**：原版 `Intro` 与主菜单**是两个场景、各有自己的 UI 相机**
                //    （主菜单场景的根里就有 `UI Camera`，见 `资料/主菜单_原版规格.md` §一）。
                //    我们这里是 additive 式的：壳的相机**先关掉**再切，免得两台 MainCamera 同时渲（画面会重）。
                //    ⚠️ `AudioListener` **留在壳上**（它 `DontDestroyOnLoad`，全场只留一个）。
                var cam = Camera.main;
                if (cam != null) cam.enabled = false;
                Debug.Log("[Shell] 载入主菜单场景（壳的相机已关，交棒给主菜单场景自己的相机）");
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
            }
            else
            {
                Debug.Log("[Shell] 主菜单场景（`MainMenu.unity`）还没建、或**没加进 Build Settings** —— 壳停在空态。" +
                          "跑一次 `MainMenuScene.BuildAndSaveScene` 会建好并自动加进去；这里**不是**静默失败");
            }
        }

        // ---------------------------------------------------------- 音乐

        /// <summary>`MainMenuMusicController.Initialize()`：第一次 `PlayMusic(main, transition 2)` 再 `PlayMusicQueued(idle)`；重复进入 `Release(main)` 后直接 `PlayMusic(idle, 2)`。</summary>
        public void PlayMenuMusic()
        {
            var main = Resources.Load<AudioClip>("Art/audio/music/main_theme");
            var idle = Resources.Load<AudioClip>("Art/audio/music/menu_idle_theme");
            if (main == null || idle == null)
            {
                Debug.LogWarning("[Shell] ⚠️ 菜单音乐没装（`Resources/Art/audio/music/{main_theme,menu_idle_theme}.ogg`）" +
                                 "—— 源在 `d:/2/新解包资源/assets_full/bundle_menumusic_assets_all/AudioClip/`");
                return;
            }
            if (_musicA == null) return;
            _musicA.clip = main; _musicA.volume = MusicVolumeMultiplier;
            _musicA.Play();
            _queuedIdle = idle;
        }

        AudioClip _queuedIdle;

        /// <summary>排队的 idle 主题在主主题放完之后接上（原版 `PlayMusicQueued`）。</summary>
        void LateUpdate()
        {
            if (_queuedIdle != null && _musicA != null && !_musicA.isPlaying)
            {
                _musicA.clip = _queuedIdle;
                _musicA.Play();
                _queuedIdle = null;
            }
        }

        // ---------------------------------------------------------- 诊断

        public string Dump()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"Shell：阶段 {Current}");
            sb.Append($" · 开场 {(IntroPlaying ? $"{IntroTime:F2}/{IntroLength:F2}s" : "已结束")}");
            sb.Append($" · 压暗层 4 条（左右常开 / 上下关：{( _fadeSides[3] != null && _fadeSides[3].gameObject.activeSelf ? "开" : "关")}）");
            sb.Append($" · 载入文案 y={LoadingY}(开)/{ProgressY}(关)");
            sb.Append(" · " + (Windows != null ? Windows.Dump() : "没有 WindowsManager"));
            sb.Append($" · 音乐 {(_musicA != null && _musicA.isPlaying ? _musicA.clip.name : "没在放")}");
            return sb.ToString();
        }
    }
}
