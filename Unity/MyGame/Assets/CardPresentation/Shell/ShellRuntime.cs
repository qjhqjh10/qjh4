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
        /// ⇒ **左右 205.809097×2585.45996 @ x=∓960**（`pivot=(0,.5)`，y 偏置 −0.000122 ≈ 0）**出厂常开**；
        /// **下 4605.01025×146.33949 @ (−4.500122,−540)** · **上 4569.2998×146.339 @ (−4.500122,+540)**（`pivot=(.5,0)`）**出厂都关**（`m_IsActive:false`）。
        /// ⚠️ **上下两条的宽不一样**（4605.01025 ≠ 4569.2998 —— 不是同一个数，**别互推**）；两条的 `x` 都是 **−4.500122**。
        /// 🔴 **2026-10-06（A39-A1）：下面这几个常量原来写的是 TSV 的【1 位小数】**（`205.8` / `2585.5` / `146.3` /
        /// `4605` / `4569.3` / `−4.5`）—— TSV 是运行期 dump、**被四舍五入过**，原值只有 JSON 有。
        /// 已按上面 ② 那份 JSON **逐条改成全精度原值**（最大差 0.0395px，**断言容差 0.54px ⇒ 结构上抓不到**，
        /// 所以 `Editor/ShellScene.cs` 另加了一节**紧容差直接比常量本身**的断言 ⇒ 谁改回 1 位小数那里就红）。</summary>
        public const float FadeSideW = 205.809097f, FadeSideH = 2585.45996f, FadeSideX = 960f;
        /// <summary>上下两条**共用**的高度（原版 `m_SizeDelta.y`：下 `146.33949` · 上 **`146.33900`** ——
        /// ⚠️ 两条**不一样**，我们共用一份 ⇒ 上条差 0.0005px，**已写明的取舍**）与锚点 y（±540）。</summary>
        public const float FadeBarH = 146.33949f, FadeBarY = 540f;
        /// <summary>上下两条的 **x 偏置**（原版 `m_AnchoredPosition.x = −4.5001220703125`，TSV 记作 `−4.5`）
        /// —— **不是 0**（左右两条的 y 偏置 −0.000122 按 0 算）。</summary>
        public const float FadeBarX = -4.500122f;
        /// <summary>上下两条的**宽**（原版实测，**两条不一样**）：下 **4605.01025** · 上 **4569.2998**。
        /// 出处：TSV `:15` = `4605.0,146.3` · `:16` = `4569.3,146.3`（**TSV 只有 1 位小数**）＋
        /// `RectTransform_277.m_SizeDelta.x = 4605.01025390625` · `RectTransform_286.m_SizeDelta.x = 4569.2998046875`。
        /// 🔴 2026-10-03 之前**两条都写 4605**（照抄了下条）⇒ 上条**偏宽 35.7px**；**已按实测改成两条各自的数**。</summary>
        public const float FadeBottomW = 4605.01025f, FadeTopW = 4569.2998f;

        /// <summary>压暗四条（`Smooth background fade *`）的渲染队列。
        /// 🔴 **2026-10-11 修**（用户点名：「**两边的阴影看起来就像是最上面了，盖过了按钮**，观感有问题」）：
        ///   原来是**写死的 `3000`**，而它**压在整套 UI 之上** —— 主菜单那一屏的梯子是
        ///   `MainMenuRuntime.QBg 2900 … QOverlay 2930`、顶栏那条带子是 **2986–2998**、
        ///   卡组编辑那屏是 `QAreaBg 2980 … QSep/QLowest 2999`
        ///   ⇒ **`3000` 把上面这些全部盖住了** —— 而本仓的命中顺序是「**渲染队列大的先吃**」
        ///   （`PointerLayer.HitButton` 挑队列最大者当赢家）⇒ **它连点击也一并吃掉**。
        ///   改到 **2900 之下** ⇒ 阴影回到「背景压暗」该在的位置（**在全部 shell UI 之前被绘制**）。
        /// ⚠️ **这张梯子的号是【我们自己起的】**（原版没有逐件渲染队列这套机制，它靠兄弟序 + 排序层）
        ///   ⇒ 值由我们定，**约束只有一条：低过全部 shell UI**；
        ///   ⛔ `0` 以下别用 —— `Editor/MainMenuScene.cs` 里那句 `if (q.RenderQueue &lt;= 0) continue;` 是**跳过口**。
        /// </summary>
        public const int FadeQueue = 2890;
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
        /// <summary>壳侧加载黑场（原版 `Safe area Only Horizontal/LoadingMenu`）——
        /// 进战斗/切场景前 `LoadingFade(true, cb)` 淡到黑，回来 `LoadingFade(false)` 淡回透明。
        /// 建/参数判据 → `Shell/LoadingMenu.cs` 文件头。**出厂关**（场景实例就是这个状态）。</summary>
        public LoadingMenu Loading { get; private set; }
        /// <summary>壳侧转圈件（原版 `Loading Controller` = `MenuLoadingController`）。
        /// 本地唯一的原版调用点是公会搜索（`JoinAllianceMenu`）—— 与「进战斗的黑场」不是同一件。</summary>
        public LoadingController LoadingCtl { get { return Loading != null ? Loading.Controller : null; } }

        /// <summary>🆕 **2026-10-17（界面线 T16）**：离屏烘焙 + UI Blur（原版 `MainMenuWindow.screenBaker`
        /// = `UIBakeScreen`）。主菜单不在前台时显示「上一帧 UI 的模糊快照」，回前台就关。
        /// <para>建法 / 参数 / 开关时机 → `Shell/UIBakeScreen.cs` 与 `GameWindow` 那三处钩子
        /// （`ToBackground` / `Hide` / `Close` ⇒ 烘；`OpenByState` 的两支 ⇒ 关）。</para></summary>
        public UIBakeScreen Bake { get; private set; }

        /// <summary>🔴 **2026-10-17（界面线 T16）**：壳里**切场景的统一入口** ——
        /// 有 `LoadingMenu` 就**先淡到全黑再 `LoadScene`**（原版 `GeneralMenuController.ShowLoading` /
        /// `BattleManager._StartBattleSequence` 那条链的等价物），**新场景到了之后淡回**；没有就直接切。
        /// <para>判据：`U3` §二 D 把「进战斗是硬切」列为本仓缺口；黑场参数/时长 → `Shell/LoadingMenu.cs`。</para>
        /// <para>🔴 **2026-10-17（N8 前置缺陷）就地订正**：本方法原来**只淡到黑、永不淡回**
        /// （全仓零处 `LoadingFade(false)`，判据 → `资料/普查产出_1017/UI/R4_黑场挡点击诊断.md` §二·2），
        /// 而 `LoadingMenu` 是 `DontDestroyOnLoad` 的壳的后代 ⇒ 进过一次战斗之后**菜单永久黑屏**；
        /// 黑场一旦参与挡指针（`PointerLayer.InputBlocked`）就是**永久锁死输入**。现在三条路都保证收尾：
        /// ① **播放态** = `sceneLoaded`（一次性）到了 ⇒ `LoadingFade(false)`；
        /// ② **批处理/编辑态** = 没有帧循环（`LoadingFade` 内部 `Complete()` 当场落终态）⇒ 切完**立刻**淡回；
        /// ③ **切场景抛异常**（场景不在 Build Settings 等，`sceneLoaded` 永远不来）⇒ 退订 + 淡回 + `LogError`
        ///    （**不永久黑屏**）。</para>
        /// <para>⚠️ **批处理/编辑模式**：`LoadingFade` 内部 `!Application.isPlaying ⇒ Complete()`
        /// ⇒ 回调**当场**执行、`LoadScene` 照旧立即切 —— 自检行为不变（且现读两个生产调用点
        /// `PracticeModePopup.cs` / `LiveOpsEventWindow.cs` 在批处理里**都提前 return**，
        /// 全仓**没有任何自检**直调本方法）。</para></summary>
        public static void LoadSceneWithFade(string scene)
        {
            var rt = Instance;
            if (rt == null || rt.Loading == null)
            {
                // 🔴 W58·D5：原版 toast 控制器随主菜单场景销毁 ⇒ 直切这条也清（判据 → 上面那一段）
                ToastNotifications.DisposeForSceneChange();
                UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
                return;
            }

            var loading = rt.Loading;

            // ---- ① 批处理 / 编辑态：没有帧循环 ----
            if (!Application.isPlaying)
            {
                try
                {
                    // `LoadingFade(true)` 内部 `!isPlaying ⇒ Complete()` ⇒ 回调当场跑、`LoadScene` 立即切
                    //（这一句与改前**逐位相同**）
                    loading.LoadingFade(true, () =>
                    {
                        ToastNotifications.DisposeForSceneChange();       // 🔴 W58·D5
                        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
                    });
                }
                finally
                {
                    // 🔴 改前**没有这一句**（黑场停在 α=1、Panel 一直 active）。
                    //    这里同样走 `Complete()`，当场落终态：α=0、`Panel.SetActive(false)`。
                    loading.LoadingFade(false);
                }
                return;
            }

            // ---- ② 播放态：淡到全黑 → 切场景 → `sceneLoaded` 到了再淡回 ----
            loading.LoadingFade(true, () =>
            {
                if (_pendingFadeBack != null)
                {
                    // 出声：上一次订阅没等到事件（否则这次会把那条静默覆盖掉）
                    UnityEngine.SceneManagement.SceneManager.sceneLoaded -= _pendingFadeBack;
                    _pendingFadeBack = null;
                    Debug.LogWarning("[Shell] 上一次切场景**没等到** `sceneLoaded`（那次的黑场没淡回）—— 本次覆盖那次订阅");
                }

                UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene,
                                                      UnityEngine.SceneManagement.LoadSceneMode> cb = null;
                cb = (loaded, mode) =>
                {
                    UnityEngine.SceneManagement.SceneManager.sceneLoaded -= cb;      // 一次性
                    if (_pendingFadeBack == cb) _pendingFadeBack = null;
                    // ⚠️ **隔一帧**再淡回：`sceneLoaded` 是在 `LoadScene` 的**同步调用里**发出来的，
                    //    而 `LoadScene` 又在我们这次 `LoadingFade(true)` 的 **`Complete()` 回调里** ——
                    //    直接在这里再进 `LoadingFade` 会与 `Complete()` 的收尾（它读 `_show`）**重入**。
                    //    一帧之后那边已彻底返回；黑场此时是 α=1，画面看不出这一帧。
                    if (rt != null) rt.StartCoroutine(CoFadeBackAfterLoad(loading));
                    else loading.LoadingFade(false);
                };
                _pendingFadeBack = cb;
                UnityEngine.SceneManagement.SceneManager.sceneLoaded += cb;
                Debug.Log("[Shell] 黑场已淡到全黑 → 切场景 `" + scene + "`；等 `sceneLoaded` 到达后淡回" +
                          "（等不到 ⇒ 黑场会留在屏上，届时看这条日志）");

                try
                {
                    // 🔴 W58·D5：此刻黑场已是 α=1（完全遮住），先销毁 toast 再切场景 ——
                    //    与原版「控制器随主菜单场景一起销毁」同一时刻（判据 → `OnSceneLoadedCleanupToast` 上方）。
                    ToastNotifications.DisposeForSceneChange();
                    UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
                }
                catch (System.Exception e)
                {
                    // 兜底：`sceneLoaded` 永远不会来（场景不在 Build Settings / name 打错…）⇒ 不能永久黑屏
                    UnityEngine.SceneManagement.SceneManager.sceneLoaded -= cb;
                    if (_pendingFadeBack == cb) _pendingFadeBack = null;
                    loading.LoadingFade(false);
                    Debug.LogError("[Shell] 切场景 `" + scene + "` 失败：" + e.Message +
                                   " —— **黑场已淡回**（不会永久黑屏）；异常不再向上抛，免得打断 `Update`。");
                }
            });
        }

        /// <summary>`sceneLoaded` 之后**隔一帧**再淡回（重入的理由 → `LoadSceneWithFade` 里那一句）。</summary>
        static IEnumerator CoFadeBackAfterLoad(LoadingMenu loading)
        {
            yield return null;
            if (loading != null) loading.LoadingFade(false);
        }

        /// <summary>在飞的那一条「等 `sceneLoaded` ⇒ 淡回」订阅（`null` = 没有）。
        /// 留成字段只为**出声**：再次切场景时若它还非空，说明上一次没等到事件（否则那次会被静默覆盖）。</summary>
        static UnityEngine.Events.UnityAction<UnityEngine.SceneManagement.Scene,
                                              UnityEngine.SceneManagement.LoadSceneMode> _pendingFadeBack;

        /// <summary>压暗边四条（`0..3` = Left / Right / Bottom / Top）的**外侧半** —— 贴屏幕边、不透明那一块，
        /// 名字沿用原版的节点名。⚠️ 一条边是**两块**（理由见 `FadeEdge`）。给自检读顶点色用。</summary>
        public ImageQuad FadeSide(int i) { return i >= 0 && i < _fadeSides.Length ? _fadeSides[i] : null; }
        /// <summary>同一条压暗边的**内侧半**（拐点 t=0.5 → 内端全透明那一块）。</summary>
        public ImageQuad FadeSideInner(int i) { return i >= 0 && i < _fadeInners.Length ? _fadeInners[i] : null; }

        Label _loadingText, _progressText;
        /// <summary>四条压暗边：`_fadeSides` = **外侧半**（贴屏幕边）· `_fadeInners` = **内侧半**（靠屏幕中心）。</summary>
        ImageQuad[] _fadeSides = new ImageQuad[4];
        ImageQuad[] _fadeInners = new ImageQuad[4];

        // ============================================================ 安全区（原版 `UISafeAreaManager.m_safeZones`）
        //
        // 🆕 **2026-10-17（界面线 T16 / `N4`）**：这两颗原来建完就没人挂（空节点 ⇒ 安全区**完全没生效**）。
        // 原版判据（`bundle_scenes_scenes_mainmenuwarpforge` 逐级解 `m_Father`，`主菜单_原版规格.md:53,64-68`）：
        //   `Game UI`(RT1314, Canvas1085 + **MB2580 `m_safeZones=[RT1086 安全区All, RT1540 安全区OnlyHorizontal]`**)
        //   └ `Main  Canvas`(RT1514)
        //      ├ `MainMenu`(RT1114) ├ `MainMenu`(RT1087 = 窗口) ├ `Background`
        //      │                                         └ **`Safe area All`(RT1086，applyWidth 1 + applyHeight 1)**
        //      │                                            └ `GameModes`(RT1122) → …
        //      └ **`Safe area Only Horizontal`(RT1540，applyWidth 1 + applyHeight 0)** 的 12 个孩子 =
        //        `Navigation Panel`(1103) · `ChatPreview`(1169) · `1 - Below Upper Bar Holder`(1095) ·
        //        **`Upper bar`(1092)** · `Borders`(1115) · `2 - Canvas Holder Above upper bar`(1109) ·
        //        `3 - PopUp Holder`(1121) · `LoadingMenu`(1098) · `UI Error Message Controller`(1206) ·
        //        `TooltipManager`(1557) · `Toast Notification Controller`(1096) · `Loading Controller`(1083)
        // ⇒ **两个 zone 是真父节点，不是装饰**（PC 无刘海时 scale/偏移恒为 1/0，看不到差别；手机/挖孔屏上全靠它）。
        Transform _safeAll, _safeH;
        UISafeArea _safeMgr;

        /// <summary>`Safe area All`（原版的「宽高都缩」那一颗）。取的时候**保证活着**（被场景卸载带走就补建）。</summary>
        public Transform SafeAreaAll { get { EnsureSafeZones(); return _safeAll; } }
        /// <summary>`Safe area Only Horizontal`（原版的「只缩宽」那一颗）。</summary>
        public Transform SafeAreaOnlyHorizontal { get { EnsureSafeZones(); return _safeH; } }

        /// <summary>把两个 zone 从「被主菜单搬走」的状态收回壳根（幂等）。
        /// <para>🔴 **为什么必须能收**：`Safe area All` 按原版是 `MainMenuWindow` 的孩子（跟着窗口隐藏），
        /// 所以 `MainMenuRuntime.Build` 会把它 `SetParent` 到**主菜单场景**那一棵里。主菜单重建时那一棵会被清场
        /// （Play 下 `Destroy` **延迟到帧末**）—— 不先摘回来，会拿一颗**将死**的节点当安全区，挂在它下面的新模式卡
        /// 跟着一起消失（**静默**：日志里什么都没有）。</para>
        /// <para>⚠️ 节点若是随主菜单**场景卸载**而销毁的 ⇒ `_safeAll == null`（Unity 的假 null）⇒ 这里补建。</para></summary>
        public void ReclaimSafeZones()
        {
            if (_safeAll != null && _safeAll.parent != transform) _safeAll.SetParent(transform, false);
            if (_safeH != null && _safeH.parent != transform) _safeH.SetParent(transform, false);
            EnsureSafeZones();
        }

        /// <summary>建/重建两颗安全区节点并把它们登记进壳根上的 <see cref="UISafeArea"/>（幂等、可重入）。
        /// <para>判据 = 原版 `RT1086`(w+h) / `RT1540`(w only) 的 `applyWidth / applyHeight`；几何由
        /// <see cref="UISafeArea"/> 按 `Screen.safeArea` 写 `localScale` / `localPosition`。</para>
        /// <para>⚠️ 两颗都是**裸 `Transform`**（不是 `RectTransform`）：我们的壳全线是「世界坐标 + `ImageQuad`」，
        /// 容器只需承担缩放/平移；原版是 `RectTransform`(stretch, sizeDelta 0)，**画面等价**（PC 上两档恒等）。</para></summary>
        public void EnsureSafeZones()
        {
            if (_safeMgr == null) _safeMgr = GetComponent<UISafeArea>();
            if (_safeMgr == null) _safeMgr = gameObject.AddComponent<UISafeArea>();

            if (_safeAll == null)
            {
                _safeAll = NewRoot(transform, "Safe area All");
                Debug.Log("[Shell] `Safe area All` 不在（多半随主菜单场景卸载被销毁了）⇒ 已补建到壳根");
            }
            if (_safeH == null)
            {
                _safeH = NewRoot(transform, "Safe area Only Horizontal");
                Debug.Log("[Shell] `Safe area Only Horizontal` 不在 ⇒ 已补建到壳根");
            }

            _safeMgr.zones = new[]
            {
                new UISafeArea.Zone { rectTransform = _safeAll, applyWidth = true, applyHeight = true  },
                new UISafeArea.Zone { rectTransform = _safeH,   applyWidth = true, applyHeight = false },
            };
        }

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
            // 加载两件（原版 `LoadingMenu` / `MenuLoadingController`）——
            // ⚠️ 两个 dt 分开：黑场补间用 unscaled，齿轮用 `Time.deltaTime`（原版 `SimpleRotation.Update`）。
            if (Loading != null) Loading.Tick(Time.unscaledDeltaTime, Time.deltaTime);
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
            // 撤掉切场景时清 toast 的全局订阅（壳正常不会销毁，这里只做卫生）
            if (_sceneHookOn)
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoadedCleanupToast;
                _sceneHookOn = false;
            }
        }

        // ============================================================ 切场景 ⇒ 清 toast（W58 · D5）
        //
        // 🔴 **2026-10-20（W58 · D5）判据**：原版 `ToastNotificationController` **只存在于主菜单场景**
        //   （`bundle_scenes_scenes_mainmenuwarpforge`；13 个 `battlearena*` 里 0 处）⇒ **切场景即整棵销毁**。
        //   我们这一颗建在 DDOL 壳上（`MainMenuRuntime.Build` → `ToastNotifications.Build`）⇒ 必须显式清：
        //   · `LoadSceneWithFade` 里**切之前**调一次（最贴近原版「随场景销毁」的时刻）；
        //   · 另有几处**不走** `LoadSceneWithFade` 的直调 `SceneManager.LoadScene`（收藏窗→卡组编辑、
        //     开战那几扇窗、`MatchLogRow` 等）⇒ 用全局 `sceneLoaded` 兜底（覆盖**所有**切场景路径）。
        //   回主菜单时 `MainMenuRuntime.Start → Build` 会重新建一颗（`sceneLoaded` 早于 `Start`，不会误杀新的）。
        //   ⚠️ 只清 `ToastNotifications`（右下角那族飘窗）；顶部错误横幅 `MessageToast` 原版在 13 个战场里也有
        //     ⇒ **不跟着清**（判据 → `Shell/MessageToast.cs` 文件头 ②）。
        static bool _sceneHookOn;

        static void OnSceneLoadedCleanupToast(UnityEngine.SceneManagement.Scene scene,
                                              UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            ToastNotifications.DisposeForSceneChange();
        }

        /// <summary>挂上「任何场景加载完 ⇒ 清 toast」的兜底订阅（幂等；`Build` 里调）。</summary>
        static void EnsureSceneCleanupHook()
        {
            if (_sceneHookOn) return;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoadedCleanupToast;
            _sceneHookOn = true;
        }

        /// <summary>自检用：切场景清 toast 的**全局订阅**挂上了没有（`Build` 里挂、`OnDestroy` 撤）。
        /// <para>⚠️ 只读；⛔ 别拿它当「清过了」的判据 —— 它只证明订阅在，清没清看
        /// `ToastNotifications.DisposeForSceneChange()` 的那几条断言。</para></summary>
        public static bool SceneCleanupHookInstalledForTest { get { return _sceneHookOn; } }

        // ============================================================ 建

        /// <summary>建整个外壳。**自检与运行时调同一个**（`Start()` 调它）。</summary>
        public void Build()
        {
            _built = true;
            EnsureSceneCleanupHook();           // 🔴 W58·D5：任何切场景都要清 toast（判据 → 上面那一段）
            var root = transform;

            // 🆕 **2026-10-17（界面线 T16 / `N4`）：安全区不再是个空节点** —— 两个 zone 的归属（原版树判据见下面建件那一节）：
            //   · `Safe area Only Horizontal` ← **窗口三锚点**（`Window Anchors`，三个 `WindowHolder` 都在它下面）；
            //   · `Safe area All` ← **主菜单的模式卡**（`MainMenuRuntime.Build` 把它搬进 `MainMenuWindow` 那一棵）。
            // ⚠️ **先收回**（两件都可能被上一次构建挂走了；不收回就会被下面那轮清场**连根拆掉**）：
            //     `Window Anchors` 平时挂在 `safeH` 下 ⇒ 先摘回壳根，下面那一轮才认得出「独立的常驻件」；
            //     `Safe area All` 可能被搬进了主菜单场景（Play 下 `Destroy` 是**延迟**的：当场非 null、帧末才没
            //     ⇒ 不先摘走，下一次 Build 会拿一颗**将死**的节点当安全区，挂在它下面的件跟着一起消失）。
            // ① 域重载后两个字段是 null，而**存过盘的 `Shell.unity` 里这两颗就在根下** ⇒ 先按名字认领，
            //    ⛔ 别再新建一颗同名的（一帧内两颗同名 ⇒ 谁 `Find` 到哪颗全看运气）。
            if (_safeAll == null) _safeAll = transform.Find("Safe area All");
            if (_safeH == null) _safeH = transform.Find("Safe area Only Horizontal");

            // ② `Window Anchors` 可能在 safeH 下（上一次 Build 挂进去的）⇒ 深搜摘回壳根
            var anchorsOld = FindDeep(transform, "Window Anchors");
            if (anchorsOld != null && anchorsOld.parent != root) anchorsOld.SetParent(root, false);

            // ③ 两颗 zone **本身留着复用**（下面那轮清场会跳过它们），但它们的孩子是上一轮烘进去的 ⇒ 自己清
            if (_safeAll != null) for (int i = _safeAll.childCount - 1; i >= 0; i--) DestroySafe(_safeAll.GetChild(i).gameObject);
            if (_safeH != null)   for (int i = _safeH.childCount - 1;   i >= 0; i--) DestroySafe(_safeH.GetChild(i).gameObject);
            EnsureSafeZones();

            // 🔴 2026-10-10 修（真鼠标在 player 里点不动 · 见账 `A1491`）：**别把窗口层卷进这次重建**。
            //   Play 里 `DestroySafe` 走的是 `Destroy`（**帧末才生效**），而下面 `EnsureHost` 的判据是
            //   `if (Instance != null) return Instance;` ⇒ 它会**返回那台正在死的**、不建新的、也不打日志；
            //   帧末那台被销毁、`OnDestroy` 把 `Instance` 置 null ⇒ **全壳再也没有 `WindowsManager`／指针层**
            //   ⇒ 真鼠标点击无声无息（而编辑模式走 `DestroyImmediate` ⇒ 立刻干净 ⇒ 自检全绿，
            //   所以这个缺陷**只在 Play 里发作**）。
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var ch = root.GetChild(i).gameObject;
                if (ch.GetComponent<WindowsManager>() != null) continue;                  // 窗口管理器
                if (ch.GetComponentInChildren<WindowHolder>(true) != null) continue;      // `Window Anchors` 那几颗
                if (ch.transform == _safeAll || ch.transform == _safeH) continue;         // 安全区两颗（复用；孩子上面已清）
                DestroySafe(ch);
            }

            var cam = Camera.main;
            if (cam != null) LayoutSpace.Apply(cam);
            else Debug.LogWarning("[Shell] 场景里没有相机（`Main Camera`）—— 界面对不上屏，先补相机");

            // ---- 离屏烘焙 + UI Blur（原版 `Game UI ▸ Main Canvas ▸ Background Render Texture Holder`）----
            // 🆕 **2026-10-17（界面线 T16）**：主菜单不在前台时（被窗压住 / 隐藏 / 关闭）显示「上一帧 UI 的模糊快照」。
            //   位置 = **全部壳 UI 之前**建（它是那一屏的地板层）；渲染队列由 `UIBakeScreen.DisplayQueue` 定
            //   （2895：高过压暗 2890、低过全部窗 2900+，理由 → 该常量）。出厂不显示、不登记命中。
            //   判据/逐句对照 → `Shell/UIBakeScreen.cs` 文件头。
            Bake = NewRoot(root, "Background Render Texture Holder").gameObject.AddComponent<UIBakeScreen>();
            Bake.cameraToBake = cam;
            Bake.Build();     // 批处理/编辑模式里 `Awake` 不跑 ⇒ 显式调（同 `Loading.Build()` 的规矩）

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
            // 🔴 **2026-10-17（T16 / `N4`）**：原来这两颗建完就再没人碰（**空节点** ⇒ 刘海/挖孔屏上什么都不会缩）。
            //    现在它们是**真父节点**：
            //      · `Safe area Only Horizontal` ← `Window Anchors`（下面 `EnsureHost` 之后那一步）+
            //        主菜单的 `Upper bar` / `Navigation Panel` / `ChatPreview` / `Toast Notification Controller`
            //        （`MainMenuRuntime.Build` 挂进来；运行期壳一直在 ⇒ 跨场景挂是允许的）；
            //      · `Safe area All` ← 主菜单 `Safe area All` → `GameModes`（`MainMenuRuntime` 把本节点搬进
            //        `MainMenuWindow` 那一棵 —— 原版 `RT1086.m_Father = 1087`，它**跟着窗口一起隐藏**）。
            //    `EnsureSafeZones` 是可重入的：主菜单场景卸载会把被搬走的 `Safe area All` 一起销毁，
            //    下一次进主菜单时这里（或 `MainMenuRuntime.Build` 开头的 `ReclaimSafeZones`）会把它补建出来。
            EnsureSafeZones();
            var safeH = _safeH;

            // ---- 三个窗口锚点 + 窗口管理器（正本 §三 第 7 条：**缺一不可**，原版取不到会 LogError）----
            // 判据只留一处：`WindowsManager.EnsureHost`（单独打开某个界面场景时也走它）
            Windows = WindowsManager.EnsureHost(root);

            // 🆕 **2026-10-17（T16 / `N4`）：三个 Holder 挂进 `Safe area Only Horizontal`** ——
            // 原版判据（`bundle_scenes_scenes_mainmenuwarpforge`，`RectTransform_1540.m_Children` **实读**）：
            //   `Safe area Only Horizontal`(RT1540) 的 12 个孩子里就有 **`1 - Below Upper Bar Holder`(1095) ·
            //   `2 - Canvas Holder Above upper bar`(1109) · `3 - PopUp Holder`(1121)**（三颗**直接**挂在它下面）。
            // ⚠️ 我们多一层 `Window Anchors`（`WindowsManager.EnsureHost` 建的，三个 Holder 的公共宿主）——
            //    这一层是**我们自己的**，原版没有；保留它（拆掉要动 `WindowsManager`，且 `DeckScene` 等宿主按这个名字跳过）。
            // ⚠️ `PointerLayer` **不跟着进去**（它在壳根；屏幕像素 → 世界是相机换算，与父链无关，而它必须在最外层吃输入）。
            var windowAnchors = root.Find("Window Anchors");
            if (windowAnchors != null && safeH != null && windowAnchors.parent != safeH)
                windowAnchors.SetParent(safeH, false);
            else if (windowAnchors == null)
                Debug.LogWarning("[Shell] 找不到 `Window Anchors`（`WindowsManager.EnsureHost` 应该建它）⇒ " +
                                 "三个 Holder **没有**挂进 `Safe area Only Horizontal`，安全区对窗口不生效（出声，不静默）");
            // 🩺 2026-10-11 临时诊断（`A1491` 收口前删）：建完之后这两件在不在、挂在谁下面
            Debug.Log($"[Shell·自查] WM={(WindowsManager.Instance != null)} PL={(PointerLayer.Instance != null)} 父={(PointerLayer.Instance != null && PointerLayer.Instance.transform.parent != null ? PointerLayer.Instance.transform.parent.name : "<null>")}");

            // ---- 载入文案（版式实证，**文案本身是本地化词条、本地没有 ⇒ 留空并说一声**）----
            _loadingText = TextBand(root, "Loading text", LoadingY, true, 10f);      // 原版 m_fontSizeMin = 10
            _progressText = TextBand(root, "Progress text", ProgressY, false, 18f);   // 原版 m_fontSizeMin = 18
            if (_loadingText != null)
                Debug.Log("[Shell] `Loading text` 的文案是本地化词条（`EverguildLocalization`，原版走**远程**语言表）" +
                          "—— 本地没有 ⇒ **留空**，不自己编一个字（正本 §五 第 4 条）");

            // ---- 压暗 / 遮罩 ----
            Shade = NewRoot(root, "Shade").gameObject.AddComponent<Shade>();
            Blocker = NewRoot(root, "BlockingOverlay").gameObject.AddComponent<BlockingOverlay>();

            // ---- 加载两件（原版 `Safe area Only Horizontal/LoadingMenu` + `Loading Controller`）----
            // 判据/逐值对照 → `Shell/LoadingMenu.cs` 文件头。两件都挂在**安全区节点**下（原版的父级）。
            // ⚠️ `LoadingMenu.Build()` 必须显式调：批处理/编辑模式里 `Awake` 不跑，不调就存不进场景。
            Loading = NewRoot(safeH, "LoadingMenu").gameObject.AddComponent<LoadingMenu>();
            Loading.StartFaded = 0;         // 主菜单场景实例的值（`MonoBehaviour_1942.json`；prefab/战场是 1）
            Loading.Build();
            Loading.gameObject.SetActive(false);   // 原版场景里 `LoadingMenu.m_IsActive = false`（出厂不画）

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

        /// <summary>整棵子树按名字找（含 inactive）。只给 `Build` 开头的「先收回」用
        /// （`Window Anchors` 可能被上一次构建挂进 `Safe area Only Horizontal`，不再是壳根的直接子件）。</summary>
        static Transform FindDeep(Transform t, string name)
        {
            if (t == null) return null;
            foreach (var c in t.GetComponentsInChildren<Transform>(true))
                if (c.name == name) return c;
            return null;
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

        /// <summary>`Loading text` / `Progress text`：1920×48 的一条。
        /// <para>🔴 **2026-10-17（`A1213④`）就地订正（铁律 5）**：原文写「文本组件本身查不到」—— **已查到**：
        /// `工具/read_tmp_level0.py` 从原始 `level0`（无 type tree，借同类型样板的 type tree 读）解出两个 TMP：
        /// · 两颗都是 **`m_fontSize = 36` · `m_fontSizeBase = 36` · `m_enableAutoSizing = 1` · `m_TextWrappingMode = 1`** ·
        ///   **Center/Middle** · **白色**；框 1920×48（下面 `TextBandW/H`）。
        /// · 只有 `m_fontSizeMin` 不同：**`Loading text` = 10** · **`Progress text` = 18**（max 都是 36）。
        /// ⇒ 照这套值接（参数 `autoMinPx` 由调用点传）。</para></summary>
        Label TextBand(Transform root, string name, float yPx, bool active, float autoMinPx)
        {
            var t = NewRoot(root, name);
            var lb = Label.Create(t, "", new Vector3(0f, (yPx - 540f) / 108f, 0f), 5,
                                  Color.white, new Vector2(0.5f, 0.5f), name + " Text");
            if (lb != null)
            {
                lb.SetScriptHeight("", 36f, 108f);                                   // 原版 m_fontSize / Base = 36
                lb.SetAutoFitBox(TextBandW / 108f, TextBandH / 108f, autoMinPx, 36f, 36f);  // min 逐颗不同、max/base 36
                lb.SetWrappingMode(1);                                               // 原版 m_TextWrappingMode = 1
            }
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
        /// 🔴 **两个半块的名字都带后缀**（`&lt;原版名> outer` / `&lt;原版名> inner`）—— 2026-10-03 改：
        ///    原来**外侧半冒用了原版那条节点的名字**，而它**沿淡出方向只有半条**（左/右 102.9px、上/下 73.15px）
        ///    ⇒ 谁按原版名去量都拿到半条。⚠️ 自检那条「上边宽度 = 4605px」踩的是**同一个名字**、但错在**字面量**：
        ///    上下两条拆的是**高**，所以那半块的「另一维」（宽）仍是整条 —— 数字对上了，判据却是下条的 4605（上条实测 4569.3）。
        ///    现在**没有任何节点**顶着原版那个整条的名字 ⇒ 「按原版名查到的」不会被误当成「原版那条的替身」，语义一致。
        ///    ⚠️ 原版上条的名字里 **`TOP` 是三个大写字母**（`level0/GameObject/Smooth background fade TOP.json` 的
        ///    `m_Name`；运行期 dump `:16` 同）—— 我们原来写成 `Top`，已改。
        ///
        /// **两个半块各自的尺寸**（判据 = 原版 `m_SizeDelta`，即上面那几个常量；「沿淡出方向」= **渐变走的那一维**，
        /// 拆的正是这一维 —— 另一维**不拆**、两块都是整条那么长）：
        /// · `Left` / `Right`（渐变走 **x**）：沿淡出方向各 **205.809097 ÷ 2 = 102.904549px** · 另一维 **2585.45996px**
        /// · `Bottom`（渐变走 **y**）：沿淡出方向各 **146.33949 ÷ 2 = 73.169745px** · 另一维 **4605.01025px**（= 那条长边）
        /// · `TOP`（渐变走 **y**）：沿淡出方向各 **146.33949 ÷ 2 = 73.169745px** · 另一维 **4569.2998px**（= 那条长边，⚠️ **不是 4605.01025**）
        ///   （🆕 2026-10-06 A39-A1：这里原写 1 位小数，已按原版 JSON 换成全精度；`TOP` 的原版高是 `146.339` ——
        ///    我们上下**共用一份常量** ⇒ 上条差 0.0005px，取舍写在 `FadeBarH` 的注释里）
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
                q.SetRenderQueue(FadeQueue);                 // 🔴 2026-10-11：原为写死的 3000 —— **压在整套 UI 之上**，见 `FadeQueue` 的注释
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
                Debug.Log($"[Shell·自查] 切场景【前】 PL={(PointerLayer.Instance != null)} 父={(PointerLayer.Instance != null && PointerLayer.Instance.transform.parent != null ? PointerLayer.Instance.transform.parent.name : "<null>")} WM={(WindowsManager.Instance != null)}");
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
            sb.Append($" · 模糊快照 {(Bake == null ? "没建" : (Bake.Showing ? "显示中" : "关"))}");
            sb.Append(" · " + (Windows != null ? Windows.Dump() : "没有 WindowsManager"));
            sb.Append($" · 音乐 {(_musicA != null && _musicA.isPlaying ? _musicA.clip.name : "没在放")}");
            return sb.ToString();
        }
    }
}
