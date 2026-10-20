// LoadingMenu.cs — 壳侧「加载」两件：原版 `LoadingMenu`（全屏黑场）+ `MenuLoadingController`（转圈）
//
// ============================ 出处（唯一来源，逐条实读）============================
// · **场景** `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/`
//     `GameObject/LoadingMenu.json`            → `m_IsActive = false`；组件 = RT 1098 + MB 1942 + Canvas 1081
//     `MonoBehaviour/MonoBehaviour_1942.json`  → `LoadingImage = PPtr(1960)`（挂在 `Panel` 上）· **`StartFaded = 0`**
//     `RectTransform/RectTransform_1098.json`  → 根：stretch / sizeDelta 0 / 无缩放
//     `RectTransform/RectTransform_1106.json`  → `Panel`：stretch、`m_AnchoredPosition.y = 3`、
//                                                `m_SizeDelta = 12006.41796875 × 10061.134765625`
//     `MonoBehaviour/MonoBehaviour_1960.json`  → `Panel` 的 Image：`m_Color = (0,0,0,1)` · `m_RaycastTarget = 1` · **sprite = null**
//     `Canvas/Canvas_1081.json`                → WorldSpace / overrideSorting / sortingOrder 0（原版靠排序层，我们靠渲染队列）
//     `GameObject/Loading Controller.json`     → `m_IsActive = true`；组件 = RT 1083 + MB 1920
//     `MonoBehaviour/MonoBehaviour_1920.json`  → `loadingIndicator = PPtr(41)`（= `Holder`；Awake 里把它关掉）
//     `RectTransform/RectTransform_1083.json`  → 根：stretch、`m_AnchoredPosition = (-419, -145)`、sizeDelta 0
//     `RectTransform/RectTransform_1117.json`  → `Holder`：锚/轴 (1,0)、`1920 × 1080`、pos (0,0) ⇒ 与根的矩形重合
//     `RectTransform/RectTransform_1535.json`  → `Menu Dark Background`：中心锚、偏移 (-102,-51)、`4574.60009765625 × 2572.360107421875`
//     `MonoBehaviour/MonoBehaviour_2499.json`  → 暗底 Image：`m_Color = (0,0,0,0.46666666865348816)` · raycastTarget 1 · sprite null
//     `RectTransform/RectTransform_1273.json`  → `Animated Loading Image`：中心锚、`270.4389953613281 × 270.4389953613281`
//     `MonoBehaviour/MonoBehaviour_2446.json`  → `Animated Loading Image` 的 Image：sprite `7621650145332779859` = **`40K_icon_searching_skull`**
//     `CanvasGroup/CanvasGroup_1620.json`      → 同一 GO 上的 CanvasGroup：**`m_Alpha = 0.699999988079071`**（会把子件一起乘下去）
//     `RectTransform/RectTransform_1183.json`  → `Cog`：与父同框（stretch、sizeDelta 0）
//     `MonoBehaviour/MonoBehaviour_2501.json`  → `Cog` 的 Image：sprite `5609191530438287762` = **`40K_icon_searching_cog`**
//     `MonoBehaviour/MonoBehaviour_2104.json`  → `Cog` 上的 `SimpleRotation`：**`speed = (0,0,-20)`**（度/秒）
// · **反编译** `d:/2/tools/decomp_full/`
//     `LoadingMenu__Awake.c`            → `if (StartFaded == 0) return;` 否则 `LoadingImage.gameObject.SetActive(true)` + 色 A = 常量
//     `LoadingMenu__LoadingFade.c`      → `SetActive(true)` → 立刻 α = `show ? 0 : 1` → `DOFade(show ? 1 : 0, 时长)`
//     `LoadingMenu.<>c__DisplayClass3_0.<LoadingFade>b__0.c` → 完成时**先调回调**，再 `if (show == 0) LoadingImage.gameObject.SetActive(false)`
//     `MenuLoadingController__Awake.c`  → `loadingIndicator.gameObject.SetActive(false)`
//     `MenuLoadingController__ShowLoading.c` / `__HideLoading.c` → 同一个 `loadingIndicator` 开/关（Singleton）
//     `GeneralMenuController__ShowLoading.c` → 转给 `LoadingMenu.LoadingFade`（实参被剥，方向见下）
//     `Everguild.MatchMakerManager__ChangeToBotBattle.c`      → `ShowLoading(instance, **1**, 0, 0)`（进战斗：淡到黑、停住）
//     `Everguild.MatchMakerManager__OnMatchMakingErrorScreenCloseButtonPress.c` → `ShowLoading(instance, **0**, 0, 0)`（淡回透明、自己关）
//     `SimpleRotation__Update.c`        → `transform.eulerAngles += speed * Time.deltaTime`（**缩放时间**，不是 unscaled）
// · **时长 / 常量**：`LoadingFade` 里 `DOFade` 的时长实参确实被反编译剥掉了；**1.25 s** 来自
//     `LoadingFade` 那条 `movss xmm2,[rip+…]` 的常量 `DAT_1834b2ec0` 实读 —— 战斗线 `A2109` 已落盘，
//     判据与同批旁证见 `Battle/BattleDriver.cs` 里 `LoadingFadeSeconds` 那段头注（**同一份方法体**，不是两条规则）。
//     ⚠️ 缓动：反编译里 `DOFade` 之后**只链了 `OnComplete`、没有 `SetEase`** ⇒ 走 DOTween 默认 **OutQuad**
//     （这是**推断**，依据 = 「没有 SetEase 调用」；原版若在全局设过 `DOTween.defaultEaseType` 则另说 —— 本地查不到那份设置）。
// · **本仓普查**：`资料/普查产出_1017/UI/U3_弹窗与小界面族.md` §二 D 两行 + §五·2（运行期姿态未查清）。
//
// ============================ 已知的取舍 / 查不到的 ============================
// 🔴 **`Loading Controller` 的运行期姿态没查到**：场景里整棵父链（`Game UI`）`m_LocalScale = 0`，
//    `Animated Loading Image` 的 `270.4²` 是**未缩放帧**；原版运行时先设 scale 再显示 —— **赋值点没查到**。
//    ⇒ 本件**按序列化值显式给 270.438995²**（⛔ 不按 0 缩放），并把这条记成「判据未清」。
// 🔴 **`Menu Dark Background` 在场景里 `m_IsActive = false`**（`GameObject/Menu Dark Background_478.json`）。
//    照抄 ⇒ 它的子件（骷髅 + 齿轮）整棵永不显示。`MenuLoadingController.ShowLoading` 只开 `Holder`、
//    本地反编译里**找不到**任何一处把这块暗底打开的赋值点 ⇒ 本件按「加载件要可见」实现（暗底随 `Holder` 一起开关），
//    如实标成 **判据未清的本仓取舍**。
// 🔴 **根节点的出厂 active**：场景里 `LoadingMenu` 是 `m_IsActive = false`，而 `GeneralMenuController.ShowLoading`
//    **不**激活根（只激活 `Panel`）⇒ 原版运行时根一定被别的东西开过，**那一点本地没查到**。
//    本件：根出厂**关**（安全），`LoadingFade` 第一句显式激活根 + 面板（**显式，不猜哪一处开的**）。
// 🟡 **2026-10-17（T16/N4）就地订正**：本行原来写「我们的 `Safe area Only Horizontal` 是个空节点（U1 的活）」——
//    **已过期**：安全区接线当天已落地（`ShellRuntime.EnsureSafeZones` + `MainMenuRuntime` 把顶栏/左栏/聊天/Toast/
//    `Window Anchors` 逐条挂进 `Safe area Only Horizontal`）。
//    本件**照原版挂在安全区节点下**（PC 无刘海时缩放 = 1，无可见差异；有刘海时跟着安全区走）。
//
// ============================ 分工 ============================
// · `LoadingMenu`（MonoBehaviour）：全屏黑场。`LoadingFade(bool show, Action cb)` = 原版同名方法。
// · `LoadingController`（普通类，原版是 `SingletonBehaviour`）：转圈那一件。原版靠 `Awake/Update`，
//   我们由 `ShellRuntime.Update` 两个入口各推一步（批处理没有帧循环 ⇒ 自检用 `TickForTest`）。
// · `BlockingOverlay` **保留**：它现在**没有任何运行时调用点**（全仓只 `Editor/ShellScene.cs` 的自检在用），
//   留作「图取不到时的降级路径」+ 自检探针；⛔ 别当成原版件。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `LoadingMenu`：一张铺满全屏的黑图，`LoadingFade(show, cb)` 淡入/淡出。
    ///
    /// <para>语义（逐条来自反编译，⚠️ 方向别弄反 —— 战斗线 `D1` §5 曾写成反的、真做出来是黑屏卡住）：
    /// · `show = true`  → 立刻 α=0，补间到 **α=1**，结束后**不关**（停在黑屏上；进战斗前用这一档）；
    /// · `show = false` → 立刻 α=1，补间到 **α=0**，结束后 `Panel.SetActive(false)`（从黑场回到画面）。</para>
    ///
    /// <para>⛔ 与 `Battle/BattleDriver.cs` 的 `BuildLoadingCover` 是**同一原版件的两处宿主**
    /// （战斗侧挂在 `FrontCanvas/Safe area FrontCanvas`，壳侧挂在 `Safe area Only Horizontal`）——
    /// 那边是 uGUI `HudPiece`、这边是世界空间 `ImageQuad`；**参数只有这一份**（时长/尺寸/方向），改这里要同步核那边。</para></summary>
    public class LoadingMenu : MonoBehaviour
    {
        // ---- 场景实读（出处见文件头，行号会漂 ⇒ 按路径复核）----
        /// <summary>`Panel`（= 原版 `LoadingImage`）的尺寸，**画布像素**。</summary>
        public const float PanelW = 12006.41796875f, PanelH = 10061.134765625f;
        /// <summary>`Panel` 的 `m_AnchoredPosition.y`（stretch 锚点 ⇒ 相对父中心的偏移）。</summary>
        public const float PanelY = 3f;
        /// <summary>`LoadingFade` 的补间时长（秒）。判据 → 文件头「时长 / 常量」那一条。</summary>
        public const float FadeSeconds = 1.25f;
        /// <summary>黑场渲染队列。**必须高于全部 shell UI** —— 现读最高的是
        /// `AlliancePanelWindow.QText = 4030`；本仓命中顺序也是「队列大的先吃」⇒ 取 4090。</summary>
        public const int QPanel = 4090;

        /// <summary>原版序列化字段。**主菜单场景实例 = 0**（`MonoBehaviour_1942.json`）；
        /// `bundle_menus` 的 prefab（`MonoBehaviour_-4255785419646353121.json`）与 13 个战场实例 = 1。
        /// 我们这份是主菜单那一档 ⇒ **0**（`Awake` 里那半截「立刻全黑」不会触发）。</summary>
        public int StartFaded = 0;

        /// <summary>黑图（原版 `LoadingImage` / 节点名 `Panel`）。取不到 `CardArt.Solid()` 时为 null（已报错）。</summary>
        public ImageQuad Panel { get; private set; }
        /// <summary>转圈那一件（原版 `Loading Controller`）。由本组件持有、由 `ShellRuntime.Update` 推。</summary>
        public LoadingController Controller { get; private set; }

        /// <summary>当前黑场不透明度（自检用）。</summary>
        public float Alpha { get { return _alpha; } }
        /// <summary>黑场现在是否真的可见（自检用）。</summary>
        public bool IsShowing { get { return Panel != null && Panel.gameObject.activeSelf && _alpha > 0.001f; } }
        /// <summary>正在补间（自检用）。</summary>
        public bool Fading { get { return _fadeT >= 0f; } }

        float _alpha;
        float _fadeT = -1f;       // < 0 = 没在补间
        bool _show;
        Action _onDone;

        /// <summary>当前那一台黑场（**只读口**，不改变任何补间行为）。
        /// <para>🔴 **2026-10-17（N8）为什么要有它**：`PointerLayer.InputBlocked` 要判「黑场面板在不在屏上」，
        /// 而它的第一判据 `ShellRuntime.Instance` **只在 `Awake` 里赋、批处理/编辑态恒 null**
        /// （判据 → `Editor/ShellScene.cs`）⇒ 自检宿主里那条路取不到黑场。
        /// 这里在 `Build()` 里登记（`ShellRuntime.Build()` 在批处理里显式调它）、`OnDestroy` 清掉。
        /// ⚠️ 生产态仍以 `ShellRuntime.Instance.Loading` 为准，本字段只是**兜底**。</para></summary>
        public static LoadingMenu Current { get; private set; }

        void Awake() { Build(); }

        void OnDestroy() { if (Current == this) Current = null; }

        /// <summary>建黑图 + 转圈件。**可重复调用**（幂等）；`ShellRuntime.Build()` 会显式调一次
        /// （批处理/编辑模式里 `Awake` 不跑，不调就存不进场景）。</summary>
        public void Build()
        {
            Current = this;                     // 只读口（见 `Current` 的注释）；批处理里 `ShellRuntime.Instance` 恒 null
            if (Panel == null)
            {
                var tex = CardArt.Solid();
                Panel = ImageQuad.Create(transform, tex,
                                         new Vector3(0f, PanelY / ImageQuad.PixelsPerUnit, 0f),
                                         PanelH / ImageQuad.PixelsPerUnit,
                                         new Vector2(0.5f, 0.5f), "Panel");
                if (Panel == null)
                {
                    Debug.LogError("[LoadingMenu] 黑场没建起来：`CardArt.Solid()` 取不到（它不该失败）——" +
                                   " 加载黑场本轮**不生效**，调用方会立刻走回调。");
                }
                else
                {
                    Panel.SetAspect(PanelW / PanelH);
                    _alpha = 1f;
                    Panel.SetTint(new Color(0f, 0f, 0f, _alpha));
                    Panel.SetRenderQueue(QPanel);
                    // 原版 `Panel` 在 prefab 里出厂 `m_IsActive = false`（主菜单场景里是 true，
                    // 但它的父 `LoadingMenu` 出厂关 ⇒ 两种写法**净效果相同，都不画**）。
                    // 这里按 prefab 那一档：面板出厂关，由 `StartFaded` / `LoadingFade` 打开。
                    Panel.gameObject.SetActive(StartFaded != 0);
                }
            }

            if (Controller == null)
            {
                Controller = new LoadingController();
                // 原版两件是 `Safe area Only Horizontal` 下的**兄弟**；我们的根也挂在同一个父下。
                // ⚠️ 原版 `LoadingMenu.Awake` **只动 `LoadingImage`**，不碰转圈件 ⇒ 这里也不 `Show()`。
                Controller.Build(transform.parent != null ? transform.parent : transform);
            }
        }

        /// <summary>原版 `LoadingMenu.LoadingFade(bool show, Action callback)`。</summary>
        public void LoadingFade(bool show, Action onComplete = null)
        {
            Build();
            if (Panel == null) { onComplete?.Invoke(); return; }   // 已报错，不静默

            gameObject.SetActive(true);                 // 见文件头「根节点的出厂 active」那条取舍
            Panel.gameObject.SetActive(true);
            _show = show;
            // 🔴 **2026-10-18（`N28`）**：本类只有**一个** `_onDone` 槽 —— 上一次补间还没收尾就再调一次，
            //   旧回调会被**静默覆盖**（改前就如此，今天两个调用点都在「点一次 `Battle!`」之后、没有重复触发路径）。
            //   按「不许静默失败」补一条出声：覆盖照旧发生（⛔ 不擅自把两条回调串起来 —— 旧那条多半是切场景，
            //   串起来会切两次），但至少留下证据（判据 → `资料/任务线/界面线/任务总表.md` 的 `N28`）。
            if (_fadeT >= 0f && _onDone != null)
                Debug.LogError("[LoadingMenu] 上一次 `LoadingFade` 的收尾回调还没跑完就又被调了一次 —— "
                             + "旧回调**已被覆盖**（单槽限制，`N28`；新回调=" + (onComplete != null ? "有" : "null") + "）。"
                             + " 若真出现这条日志，说明有两条并发的加载/切场景链，先查调用方。");
            _onDone = onComplete;
            _alpha = show ? 0f : 1f;                    // 原作：**立刻**设成与目标相反那个值
            Apply();
            _fadeT = 0f;
            if (!Application.isPlaying) Complete();     // 批处理/编辑模式没有帧循环 ⇒ 直接落终态
        }

        /// <summary>推进补间（`ShellRuntime.Update` 每帧调；批处理/自检直接调）。
        /// <para>两个 dt 分开是**照原版**：黑场补间用 unscaled（加载时即使 timeScale=0 也要走），
        /// 齿轮用 `SimpleRotation.Update` 的 `Time.deltaTime`（缩放时间）。</para></summary>
        public void Tick(float fadeDt, float spinDt)
        {
            if (_fadeT >= 0f)
            {
                _fadeT += Mathf.Max(0f, fadeDt);
                float p = FadeSeconds <= 0f ? 1f : Mathf.Clamp01(_fadeT / FadeSeconds);
                float e = 1f - (1f - p) * (1f - p);     // OutQuad（见文件头「缓动」）
                _alpha = _show ? e : 1f - e;
                Apply();
                if (p >= 1f) Complete();
            }
            if (Controller != null) Controller.Tick(spinDt);
        }

        /// <summary>自检用：不走帧循环推一步（两个 dt 取同一个值）。</summary>
        public void TickForTest(float dt) { Tick(dt, dt); }

        void Apply()
        {
            if (Panel != null) Panel.SetTint(new Color(0f, 0f, 0f, _alpha));
        }

        void Complete()
        {
            _fadeT = -1f;
            _alpha = _show ? 1f : 0f;
            Apply();
            var cb = _onDone; _onDone = null;
            cb?.Invoke();                               // 原作：**先**调回调
            if (!_show && Panel != null) Panel.gameObject.SetActive(false);   // 再关 LoadingImage
        }
    }

    /// <summary>原版 `MenuLoadingController`（`SingletonBehaviour`）：`Holder(1920×1080)` →
    /// `Menu Dark Background(4574.6×2572.36 · 黑 α0.4667)` → `Animated Loading Image(270.439² · 骷髅 α0.7)` → `Cog(270.439² · 齿轮 · -20°/s)`。
    ///
    /// <para>⚠️ **本地唯一的原版调用点** = `JoinAllianceMenu`（公会搜索）：`Initialize` / `FillOpenAlliances`
    /// 里 `ShowLoading()`，回调里 `HideLoading()`。其余窗口没有调用点 —— ⛔ 别以为是「进战斗的转圈」，
    /// 那是 `LoadingMenu` 那件。</para></summary>
    public class LoadingController
    {
        // ---- 场景实读（出处见文件头）----
        public const string NodeName = "Loading Controller";
        public const float RootX = -419f, RootY = -145f;               // RT 1083（stretch + anchoredPosition）
        public const float HolderW = 1920f, HolderH = 1080f;           // RT 1117
        public const float DarkW = 4574.60009765625f, DarkH = 2572.360107421875f;   // RT 1535
        public const float DarkX = -102f, DarkY = -51f;                // RT 1535 的 anchoredPosition（中心锚）
        public const float DarkAlpha = 0.46666666865348816f;           // MB 2499 的 m_Color.a
        public const float SpinnerPx = 270.4389953613281f;             // RT 1273 / 1183
        public const float SpinnerAlpha = 0.699999988079071f;          // CanvasGroup_1620（**含子件 Cog**）
        /// <summary>`Cog` 的 `SimpleRotation.speed.z`（度/秒，负 = 顺时针）。</summary>
        public const float SpinDegPerSecond = -20f;

        // 渲染队列（🔴 **2026-10-20（W58·D4）重定档** —— 原版判据见下，⛔ 别按「高于全部 shell UI」去推）：
        //   **原版判据**：`MenuLoadingController` 在 `Game UI` 画布的 **Default(0) 层**（自己**没有** Canvas；
        //   唯一子 Canvas 是 `Cog` 的 `Canvas_1078`，`m_OverrideSorting = false`），且是 `Safe area Only Horizontal`
        //   的**最后一个孩子**（兄弟序 11）⇒ **压在主窗（`windowsPlacement` 5/10）之上、在全部 PopUps
        //   （窗/横幅/教学/toast）与 Blocking（tooltip/黑场）之下**
        //   （判据 → `资料/普查产出_1020/W58_界面线_联盟搜索形态判据.md` §一·4）。
        //   **我们的梯子 [我们挑的]**：原版按排序层排，我们这张显式队列表里没有对应的号段（placement-5 活动窗
        //   已占到 3729，而 placement-15 弹窗最低 3310）⇒ 取 `TrophyInfoPopup.QHit = 3326` 与
        //   `GenericOptionsPanel.QShade = 3330` 之间的空隙 **3327–3329**：高于社交窗全部内容
        //   （页 3200–3209 · 奖杯格 3210–3299 · 聊天 3300–3308），又不落进任何窗的号段。
        //   ⚠️ **已知残留倒置（如实记）**：`TrophyInfoPopup` 是 placement-15 弹窗、原版应在转圈之上，而本档在它之上；
        //   两者在可达状态里互斥（奖杯格只在「已入盟」支，转圈只在「未入盟」Join 列表）⇒ 今天不可见。
        //   要严格消除见判据 §四「备选 5a」（动 `TrophyInfoPopup` 整带，不在本批）。
        public const int QDark = 3327, QSkull = 3328, QCog = 3329;

        public const string ArtSkull = "40K_icon_searching_skull";
        public const string ArtCog = "40K_icon_searching_cog";
        /// <summary>降级用（`BlockingOverlay` 当年那张圈）。</summary>
        public const string ArtFallback = "40K_menu_loading";

        public Transform Root { get; private set; }
        public ImageQuad Dark { get; private set; }
        public ImageQuad Skull { get; private set; }
        public ImageQuad Cog { get; private set; }
        /// <summary>现在是否显示（= 原版 `loadingIndicator.activeSelf`；出厂关）。</summary>
        public bool IsShowing { get { return _holder != null && _holder.gameObject.activeSelf; } }
        /// <summary>齿轮当前角度（度，自检用）。</summary>
        public float Angle { get { return _angle; } }

        Transform _holder;
        float _angle;

        /// <summary>建整棵（幂等）。`parent` = 原版的 `Safe area Only Horizontal`。</summary>
        public void Build(Transform parent)
        {
            if (Root != null) return;

            Root = new GameObject(NodeName).transform;
            Root.SetParent(parent, false);
            // 原版 RT 1083：stretch + anchoredPosition (-419,-145) ⇒ 整个矩形相对安全区平移那一对值。
            Root.localPosition = new Vector3(RootX / ImageQuad.PixelsPerUnit,
                                             RootY / ImageQuad.PixelsPerUnit, 0f);

            _holder = new GameObject("Holder").transform;
            _holder.SetParent(Root, false);
            _holder.localPosition = Vector3.zero;

            var darkTex = CardArt.Solid();
            Dark = ImageQuad.Create(_holder, darkTex,
                                    new Vector3(DarkX / ImageQuad.PixelsPerUnit,
                                                DarkY / ImageQuad.PixelsPerUnit, 0f),
                                    DarkH / ImageQuad.PixelsPerUnit,
                                    new Vector2(0.5f, 0.5f), "Menu Dark Background");
            if (Dark != null)
            {
                Dark.SetAspect(DarkW / DarkH);
                Dark.SetTint(new Color(0f, 0f, 0f, DarkAlpha));
                Dark.SetRenderQueue(QDark);
            }
            else
            {
                Debug.LogError("[LoadingController] 暗底没建起来：`CardArt.Solid()` 取不到 —— 转圈会**没有压暗**。");
            }

            Transform skullParent = Dark != null ? Dark.transform : _holder;
            var skullTex = CardArt.MenuUi(ArtSkull);
            Skull = ImageQuad.Create(skullParent, skullTex, Vector3.zero,
                                     SpinnerPx / ImageQuad.PixelsPerUnit,
                                     new Vector2(0.5f, 0.5f), "Animated Loading Image");
            if (Skull != null)
            {
                Skull.SetAspect(1f);
                // CanvasGroup alpha 0.7 会**乘到子件**上（原版 CanvasGroup 在 `Animated Loading Image` 上）
                Skull.SetTint(new Color(1f, 1f, 1f, SpinnerAlpha));
                Skull.SetRenderQueue(QSkull);
            }
            else
            {
                Debug.LogWarning("[LoadingController] 骷髅图取不到：`CardArt.MenuUi(\"" + ArtSkull + "\")` ——" +
                                 " 应来自 `Resources/Art/ui_menu/" + ArtSkull + ".png`" +
                                 "（导入器 `工具/import_original_art.py`）。本轮只画齿轮。");
            }

            var cogTex = CardArt.MenuUi(ArtCog);
            if (cogTex == null) cogTex = CardArt.MenuUi(ArtFallback);
            if (cogTex == null)
            {
                // 不许静默：转圈件没有图 ⇒ 只剩暗底。这正是 `BlockingOverlay` 那条降级路径的触发条件。
                Debug.LogError("[LoadingController] 齿轮图取不到：`" + ArtCog + "` 与降级图 `" + ArtFallback + "`" +
                               " 都没有（`Resources/Art/ui_menu/`）—— 加载件**只剩暗底、没有转圈**。");
            }
            else
            {
                Transform cogParent = Skull != null ? Skull.transform : skullParent;
                Cog = ImageQuad.Create(cogParent, cogTex, Vector3.zero,
                                       SpinnerPx / ImageQuad.PixelsPerUnit,
                                       new Vector2(0.5f, 0.5f), "Cog");
                if (Cog != null)
                {
                    Cog.SetAspect(1f);
                    Cog.SetTint(new Color(1f, 1f, 1f, SpinnerAlpha));
                    Cog.SetRenderQueue(QCog);
                }
            }

            // 原版 `MenuLoadingController.Awake`：`loadingIndicator.gameObject.SetActive(false)`。
            _holder.gameObject.SetActive(false);
            if (Dark != null && !Dark.gameObject.activeSelf)
            {
                // 场景里这一块是 `m_IsActive = false`，但照抄会让整棵加载件永远不显示
                // （`ShowLoading` 只开 `Holder`）。见文件头「已知的取舍」。
                Dark.gameObject.SetActive(true);
            }
        }

        /// <summary>原版 `MenuLoadingController.ShowLoading`。</summary>
        public void Show()
        {
            if (_holder == null) { Debug.LogError("[LoadingController] `Show()` 在 `Build()` 之前被调用 —— 转圈没有宿主。"); return; }
            _holder.gameObject.SetActive(true);
        }

        /// <summary>原版 `MenuLoadingController.HideLoading`。</summary>
        public void Hide()
        {
            if (_holder == null) return;
            _holder.gameObject.SetActive(false);
        }

        /// <summary>推一帧（`ShellRuntime.Update` 调；原版 `SimpleRotation.Update` 用 `Time.deltaTime`）。</summary>
        public void Tick(float dt)
        {
            if (Cog == null) return;
            _angle = Mathf.Repeat(_angle + SpinDegPerSecond * dt, 360f);
            Cog.transform.localRotation = Quaternion.Euler(0f, 0f, _angle);
        }

        /// <summary>自检用：把角度钉死（批处理没有帧循环）。</summary>
        public void SetAngleForTest(float deg)
        {
            _angle = Mathf.Repeat(deg, 360f);
            if (Cog != null) Cog.transform.localRotation = Quaternion.Euler(0f, 0f, _angle);
        }
    }
}
