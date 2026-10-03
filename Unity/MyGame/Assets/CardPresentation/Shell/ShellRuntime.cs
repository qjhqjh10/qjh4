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
        /// <summary>压暗层。尺寸/位置实证（**两个独立源，逐条吻合**）：
        /// ① 运行期 dump `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Intro.tsv:13-16`；
        /// ② 原始序列化 `d:/2/新解包资源/assets_full/level0/RectTransform_{282,283,286,277}.json`。
        /// ⇒ **左右 205.8×2585.5 @ x=∓960**（`pivot=(0,.5)`，y 偏置 −0.000122 ≈ 0）**出厂常开**；
        /// **下 4605.0×146.3 @ (−4.5,−540)** · **上 4569.3×146.3 @ (−4.5,+540)**（`pivot=(.5,0)`）**出厂都关**（`m_IsActive:false`）。
        /// ⚠️ **上下两条的宽不一样**（4605.0 ≠ 4569.3 —— 不是同一个数，**别互推**）；两条的 `x` 都是 **−4.5**。</summary>
        public const float FadeSideW = 205.8f, FadeSideH = 2585.5f, FadeSideX = 960f;
        /// <summary>上下两条**共用**的高度（原版 `m_SizeDelta.y`：下 `146.33949` · 上 `146.33900`）与锚点 y（±540）。</summary>
        public const float FadeBarH = 146.3f, FadeBarY = 540f;
        /// <summary>上下两条的 **x 偏置 = −4.5**（原版 `m_AnchoredPosition.x = −4.5001220703125`，TSV 记作 `−4.5`）
        /// —— **不是 0**（左右两条的 y 偏置 −0.000122 按 0 算）。</summary>
        public const float FadeBarX = -4.5f;
        /// <summary>上下两条的**宽**（原版实测，**两条不一样**）：下 **4605.0** · 上 **4569.3**。
        /// 出处同上：TSV `:15` = `4605.0,146.3` · `:16` = `4569.3,146.3`；
        /// `RectTransform_277.m_SizeDelta.x = 4605.01025390625` · `RectTransform_286.m_SizeDelta.x = 4569.2998046875`。
        /// 🔴 2026-10-03 之前**两条都写 4605**（照抄了下条）⇒ 上条**偏宽 35.7px**；**已按实测改成两条各自的数**。</summary>
        public const float FadeBottomW = 4605f, FadeTopW = 4569.3f;
        /// <summary>压暗层四条边的**黑→透明渐变**（原版 `Gradient2` 实测，`工具/read_gradient2_level0.py` 可复现，四条一致）：
        /// `Image.m_Color` = **白 (1,1,1,1)**（颜色**全在顶点色里**，不在 tint 上）· 色键 2 个**都是纯黑** ·
        /// alpha 三键 = **外端 1 · 中点 0.709804 · 内端 0**。
        /// 🔴 「外端」= **贴着屏幕边那一头**（方向判据与出处见 `FadeEdge` 的注释 —— **别凭常识推**）。</summary>
        public const float FadeAlphaOuter = 1f, FadeAlphaMid = 0.709804f, FadeAlphaInner = 0f;
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

        /// <summary>压暗边四条（`0..3` = Left / Right / Bottom / Top）的**外侧半** —— 贴屏幕边、不透明那一块，
        /// 名字沿用原版的节点名。⚠️ 一条边是**两块**（理由见 `FadeEdge`）。给自检读顶点色用。</summary>
        public ImageQuad FadeSide(int i) { return i >= 0 && i < _fadeSides.Length ? _fadeSides[i] : null; }
        /// <summary>同一条压暗边的**内侧半**（拐点 t=0.5 → 内端全透明那一块）。</summary>
        public ImageQuad FadeSideInner(int i) { return i >= 0 && i < _fadeInners.Length ? _fadeInners[i] : null; }

        Label _loadingText, _progressText;
        /// <summary>四条压暗边：`_fadeSides` = **外侧半**（贴屏幕边）· `_fadeInners` = **内侧半**（靠屏幕中心）。</summary>
        ImageQuad[] _fadeSides = new ImageQuad[4];
        ImageQuad[] _fadeInners = new ImageQuad[4];
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
            // 🔴 **2026-10-03 订正摆法**：原来四条都按**中心 pivot** 摆在屏幕边上 ⇒ **一半在屏外**。
            //    原版 `level0/RectTransform_{282,283,286,277}.json` 是：
            //    **Left `pivot=(0,.5)` pos.x = −960** · **Right `pivot=(0,.5)` pos.x = +960 + `scale.x = −1`** ·
            //    Top/Bottom `pivot=(.5,0)`、**`pos = (−4.5, ∓540)`**。⇒ 四条都**贴着屏幕边、整条在屏内**
            //    （Left/Right 各 205.8 全可见；上下两条各占屏幕边那 146.3px 一条）。
            //    这里用「中心 = 边 + 半个宽」等效实现（`ImageQuad.Create` 的 pivot 参数在本工程是固定 .5）。
            //    ⚠️ 上下两条的 **x = −4.5**（**不是 0**）按原版照抄 —— 判据与出处见 `FadeBarX` 的注释。
            var fade = NewRoot(root, "FadeBackground");
            // 每条 = **两块**（外侧半 + 内侧半，两块名字都带 ` outer` / ` inner` 后缀），逐顶点色的**黑→透明**渐变
            // —— 为什么两块、名字为什么两块都加后缀，见 `FadeEdge` 的注释
            FadeEdge(fade, 0, "Smooth background fade Left",   -FadeSideX + FadeSideW * 0.5f, 0f, FadeSideW, FadeSideH, true);
            FadeEdge(fade, 1, "Smooth background fade Right",   FadeSideX - FadeSideW * 0.5f, 0f, FadeSideW, FadeSideH, true);
            FadeEdge(fade, 2, "Smooth background fade Bottom",  FadeBarX, -FadeBarY + FadeBarH * 0.5f, FadeBottomW, FadeBarH, false);
            FadeEdge(fade, 3, "Smooth background fade TOP",     FadeBarX,  FadeBarY - FadeBarH * 0.5f, FadeTopW,    FadeBarH, false);

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

        /// <summary>
        /// 建一条压暗边：**逐顶点色的「黑 → 透明」渐变**，照原版那条 `Gradient2`。
        ///
        /// 🔴 **原版这四条不是一块不透明黑板**。`工具/read_gradient2_level0.py`（2026-10-03，A18）从 `level0`
        /// 的原始字节里把那两个「解包时被整条跳过」的组件读出来了（四条边一致）：
        /// · `Image.m_Color = (1,1,1,**1**)`（**白** —— 颜色与透明度**全在顶点色里**）
        /// · `Gradient2`：色键 2 个**都是纯黑 (0,0,0)** · alpha 三键 **1 @ t=0 · 0.709804 @ t=0.5 · 0 @ t=1** ·
        ///   `_gradientType` **左/右 = 0 · 上/下 = 1** · `_blendMode = 2`（乘）· `_modifyVertices = 1` · `_offset = 0` · `_zoom = 1`
        /// ⇒ 原来那句「原版那 4 条的脚本导出失败 ⇒ 图是黑的纯色 + tint」**只对尺寸成立**、颜色是错的
        ///   （改之前我们画的是一整块**不透明纯黑**）。现在照 `m_Color` 白 + 顶点色写。
        ///
        /// 🔴 **哪一端是不透明的**（方向判据 —— 别凭常识推，这里给全链证据）：
        /// ① `Gradient2.ModifyMesh`（`d:/2/tools/decomp_full/UnityEngine.UI.Extensions.Gradient2__ModifyMesh.c:217-226,300-302`）
        ///    里 `t = (顶点坐标 − GetBounds 的最小值) / 尺寸` ⇒ **t=0 在坐标小的一头**；
        /// ② 走哪根轴由 `_gradientType` 选（同目录 `Gradient2__GetPositions.c:22`：`== 0` 取 x、否则取 y）；
        /// ③ `Gradient2__GetBounds.c:49-52` 返回的正是 `(minX, minY, 宽, 高)`；
        /// ④ 四条的 `RectTransform`（`d:/2/新解包资源/assets_full/level0/RectTransform_{282,283,286,277}.json`）
        ///    把**小的一头摆在屏幕边上**：Left `pivot=(0,.5)`+`pos.x=−960` · Right 同 pivot+`pos.x=+960`
        ///    且 **`m_LocalScale.x = −1`** · TOP `pivot=(.5,0)`+`pos.y=+540` 且 **`scale.y = −1`** ·
        ///    Bottom 同 pivot+`pos.y=−540` 且 **`scale.x = −1`**（**有效果吗？→ 没有，见本节末那条已核结论**）。
        /// ⇒ **贴着屏幕边那一头 alpha = 1（不透明黑），往屏幕中心淡到 0**（上下两条出厂是关的，实证）。
        ///
        /// ⚠️ **一条边为什么是两块**：`ImageQuad` 的网格只有 4 个顶点，一条只能表达**两键线性**；
        ///    而原版是**三键分段线性**（`_modifyVertices = 1` ⇒ 它还会 `SplitTrianglesAtGradientStops`
        ///    把拐点切出来 —— 这本身也是「拐点得落在顶点上」的证据）。所以按拐点 `t = 0.5` 拆成两块：
        ///    外块 **1 → 0.709804**、内块 **0.709804 → 0**，各自线性 ⇒ **合起来与原版逐点相同**。
        ///
        /// 🔴 **两个半块的名字都带后缀**（`<原版名> outer` / `<原版名> inner`）—— 2026-10-03 改：
        ///    原来**外侧半冒用了原版那条节点的名字**，而它**沿淡出方向只有半条**（左/右 102.9px、上/下 73.15px）
        ///    ⇒ 谁按原版名去量都拿到半条。⚠️ 自检那条「上边宽度 = 4605px」踩的是**同一个名字**、但错在**字面量**：
        ///    上下两条拆的是**高**，所以那半块的「另一维」（宽）仍是整条 —— 数字对上了，判据却是下条的 4605（上条实测 4569.3）。
        ///    现在**没有任何节点**顶着原版那个整条的名字 ⇒ 「按原版名查到的」不会被误当成「原版那条的替身」，语义一致。
        ///    ⚠️ 原版上条的名字里 **`TOP` 是三个大写字母**（`level0/GameObject/Smooth background fade TOP.json` 的
        ///    `m_Name`；运行期 dump `:16` 同）—— 我们原来写成 `Top`，已改。
        ///
        /// **两个半块各自的尺寸**（判据 = 原版 `m_SizeDelta`，即上面那几个常量；「沿淡出方向」= **渐变走的那一维**，
        /// 拆的正是这一维 —— 另一维**不拆**、两块都是整条那么长）：
        /// · `Left` / `Right`（渐变走 **x**）：沿淡出方向各 **205.8 ÷ 2 = 102.9px** · 另一维 **2585.5px**
        /// · `Bottom`（渐变走 **y**）：沿淡出方向各 **146.3 ÷ 2 = 73.15px** · 另一维 **4605.0px**（= 那条长边）
        /// · `TOP`（渐变走 **y**）：沿淡出方向各 **146.3 ÷ 2 = 73.15px** · 另一维 **4569.3px**（= 那条长边，⚠️ **不是 4605**）
        ///
        /// ✅ **已核、等价、不改**：原版下条 `RectTransform_277` **还带 `m_LocalScale.x = −1`**，我们没照抄 ——
        ///    2026-10-03 核过，**横向镜像在画面上恒等**（三条一起才算数，缺一条都不成立）：
        ///    ① 那条的 `pivot.x = 0.5`、宽 4605.01 ⇒ 矩形**左右对称于自己的 pivot** ⇒ 镜像后**覆盖区域一个像素都不变**；
        ///    ② 上下两条的渐变走 **y 轴**（`Gradient2._gradientType = 1`；轴的选择见 `Gradient2__GetPositions.c:22`），
        ///       而色键两个**都是纯黑**、`m_Color` 是白 ⇒ **颜色场与 x 无关** ⇒ 镜像对颜色/透明度也无效；
        ///    ③ 那一层**没有贴图** —— `Image.m_Sprite` 是空引用（`level0` 原始字节：`m_Maskable` 之后
        ///       `m_OnCullStateChanged`(空 UnityEvent 4B) + `m_Sprite`(12B) 全 0；`m_OverrideSprite` 这个字段**本 build 根本没有**；
        ///       运行期 dump 那一列的 sprite 名也是空的 —— `SceneJumpShot.cs:87-92` 读的就是 `img.sprite`）
        ///       ⇒ 不存在「非对称贴图被镜像」这回事。
        ///    ⚠️ **对照（这两条的 −1 是有效果的、必须照抄）**：`Right` 的 `scale.x = −1`、`TOP` 的 `scale.y = −1`
        ///       —— 它们的 −1 正好落在**渐变轴上**，作用就是把「t=0 的小侧」翻到屏幕边上。
        /// </summary>
        /// <param name="idx">`0` = Left · `1` = Right · `2` = Bottom · `3` = Top（与 `_fadeSides` / `_fadeInners` 同序）。
        /// **轴与「屏幕边在哪一侧」只用它推**（不另传参数，免得两处打架）。</param>
        void FadeEdge(Transform parent, int idx, string name, float cxPx, float cyPx,
                      float wPx, float hPx, bool active)
        {
            bool horizontal = idx <= 1;                      // 左/右沿 x（原版 `_gradientType` = 0）· 上/下沿 y（= 1）
            bool opaqueAtMin = idx == 0 || idx == 2;         // 屏幕边在坐标**小**的一侧（Left / Bottom）
            // 整条：沿淡出方向 [edgePx, edgePx ± spanPx]；另一维居中、长 crossLen
            float edgePx = horizontal ? (opaqueAtMin ? cxPx - wPx * 0.5f : cxPx + wPx * 0.5f)
                                      : (opaqueAtMin ? cyPx - hPx * 0.5f : cyPx + hPx * 0.5f);
            float spanPx = horizontal ? wPx : hPx;
            float crossC = horizontal ? cyPx : cxPx;
            float crossLen = horizontal ? hPx : wPx;
            float dir = opaqueAtMin ? 1f : -1f;              // 从屏幕边往屏内走

            for (int k = 0; k < 2; k++)                      // k = 0 外侧半 · k = 1 内侧半
            {
                float segPx = spanPx * 0.5f;                                  // 拐点在 t=0.5 ⇒ 两块一样长
                float cFade = edgePx + dir * segPx * (k + 0.5f);              // 这一块沿淡出方向的中心
                float aOuterEnd = k == 0 ? FadeAlphaOuter : FadeAlphaMid;     // 靠屏幕边那一端
                float aInnerEnd = k == 0 ? FadeAlphaMid : FadeAlphaInner;     // 靠屏幕中心那一端
                var q = ImageQuad.Create(parent, CardArt.Solid(),
                                         new Vector3((horizontal ? cFade : crossC) / 108f,
                                                     (horizontal ? crossC : cFade) / 108f, 0f),
                                         (horizontal ? crossLen : segPx) / 108f,
                                         new Vector2(0.5f, 0.5f),
                                         k == 0 ? name + " outer" : name + " inner");
                if (q == null) { Debug.LogWarning("[Shell] 压暗边 `" + name + "` 建不出来（`CardArt.Solid()` 没给图）"); continue; }
                q.SetAspect(horizontal ? segPx / crossLen : crossLen / segPx);
                // 顶点色 = **纯黑 + alpha 渐变**，顺序 BL · BR · TR · TL（与 `ImageQuad.RebuildMesh` 同序）。
                // **材质 tint 留白**（原版 `Image.m_Color` 就是白的）—— 渐变不许走 tint。
                var cOut = new Color(0f, 0f, 0f, aOuterEnd);
                var cIn = new Color(0f, 0f, 0f, aInnerEnd);
                if (horizontal)
                    q.SetCornerColors(opaqueAtMin ? cOut : cIn, opaqueAtMin ? cIn : cOut,     // BL · BR
                                      opaqueAtMin ? cIn : cOut, opaqueAtMin ? cOut : cIn);    // TR · TL
                else
                    q.SetCornerColors(opaqueAtMin ? cOut : cIn, opaqueAtMin ? cOut : cIn,     // BL · BR
                                      opaqueAtMin ? cIn : cOut, opaqueAtMin ? cIn : cOut);    // TR · TL
                q.SetRenderQueue(3000);                 // 实证：四条都是 3000（**别改这个值**）
                q.gameObject.SetActive(active);         // 上下两条出厂是关的（实证）；内侧半**必须跟着同状态**
                if (k == 0) _fadeSides[idx] = q; else _fadeInners[idx] = q;
            }
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
