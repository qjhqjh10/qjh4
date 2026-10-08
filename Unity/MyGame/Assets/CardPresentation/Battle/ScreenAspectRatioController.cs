// ScreenAspectRatioController.cs — 原版 `ScreenAspectRatioController`（`dump.cs` TypeDefIndex 3175）**整件**（§8b · 2a，2026-10-18）
//
// ============================ 这件是干什么的 ============================
// 「屏幕宽高比超出 [min, max] ⇒ 把整个渲染区缩成中间一个盒子、盒子外面涂黑」——
// 就是**黑边**（> 22:9 左右 pillarbox / < 4:3 上下 letterbox）。做法 = 给**两台**相机写 `Camera.rect`
// ＋ 在 `beginCameraRendering` 里 `GL.Clear` 一次。
// ⚠️ **黑边只在 `aspect < 1.3333` 或 `aspect > 2.4444` 时出现** —— ⛔ **不是「非 16:9」**
//    （`资料/普查产出_第五会话/查证_8b战斗视图四条.md` 甲表 2a 行自己订正过这一条）。
// 与之配套的两枚**纯黑 quad**（原版 `Aspect Ratio Filler Top/Bottom`，13/13 战场都有）在
// `BattleDriver.BuildAspectRatioFillers` —— 它们是**另一件**，本文件不碰。
//
// ============================ 判据（全是实读） ============================
// ① **类 / 字段 / 偏移 / 方法表** = `d:/2/tools/il2cpp_out/dump.cs:118398-118425`：
//    `Camera[] targetCameras +0x20` · `float maxAspectRatio +0x28` · `float minAspectRatio +0x2C` ·
//    `bool cleanFrameBuffer +0x30`；方法**只有四个**（+ `.ctor`）：
//    `Start` / `RenderPipelineManagerOnbeginCameraRendering` / `AdjustCameraViewport` / `SetCamerasRect`。
// ② **ctor 出厂值**（`ScreenAspectRatioController__.ctor.c`）：`+0x28 = 0x401c71c5` = **2.4444447** ·
//    `+0x2C = 0x3faaaaa8` = **1.3333334** · `+0x30 = 1`。
// ③ 🔴 **场景值**（`assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_5148.json`，
//    挂 GO `BattlePrefab` = pid 330）：`maxAspectRatio = 2.444443941116333` ·
//    `minAspectRatio = 1.3333330154418945` · `targetCameras = [Camera_1461, Camera_1462]`
//    ⇒ **又是一处「ctor ≠ 场景」**（同 `BattleCameraSreenSize.sensorSizeXBigScreen` 那一族，铁律 5·c）
//    ⇒ 我们取**场景那一档**。
// ④ **那两台相机分别是谁**（原版，本节回答了「查证报告 己·2」那个悬案）：
//    · `Camera_1461` = 3D 全屏 `BoardCamera`（`BattleCameraSreenSize.boardCamera` 指着它，
//      见 `BattleCameraSreenSize.cs` 文件头 ④）；
//    · `Camera_1462` = **UI 相机** —— 判据 = 主 Canvas 自己那颗组件
//      （同目录 `MonoBehaviour/MonoBehaviour_2572.json`：`m_RenderMode = 1`(ScreenSpaceCamera) ·
//       `m_Camera = {m_PathID: 1462}` · `m_PlaneDistance = 100`）；它自己 `clearFlags = 4`(Nothing) ·
//      `cullingMask = 32`（layer 5 = UI）· `m_Depth = 0` · 视口初始 `(0,0,1,1)`。
//    ⇒ 我们这边的对应 = `boardCam`（透视 Base）＋ `cam`（正交 HUD，挂在 `boardCam.cameraStack` 里）。
// ⑤ **公式**（`__Start.c` 与 `__AdjustCameraViewport.c` —— 两份除变量名外**逐字相同**，后者是提出来那一份）：
//      aspect = Screen.width / Screen.height
//      maxAspectRatio < aspect  ⇒ rect = (f, 0, 1−2f, 1)     f = (1 − maxAspectRatio / aspect) × 0.5
//      aspect < minAspectRatio  ⇒ rect = (0, f, 1, 1−2f)     f = (minAspectRatio / aspect − 1) × 0.5
//      否则                      ⇒ rect = (0, 0, 1, 1)
//    ⚠️ **窄屏那一支的分子是 `(min/aspect − 1)`，⛔ 不是 `(1 − aspect/min)`** —— 逐字节对过（两者不等）。
//    ⚠️ **`<` / `<=` 的边界**：`Start` 写的是 `aspect < min` 走 letterbox；`AdjustCameraViewport` 写的是
//      `if (min <= aspect) 整幅 else letterbox` —— **等价**，本文件按前者写。
// ⑥ **`.rdata` 常量**（`工具/read_literal.py` 直读 4 字节 float；宽度判据 = 两处比较/乘法都是 float）：
//    `0x1834b2bb4` = **0.5** · `0x1834b2bb8` = **1.0** · `0x1834b2c70..7c` = 整幅 `Rect(0,0,1,1)` ·
//    `0x1834b2e40..4c` = `GL.Clear` 的色 = **(0, 0, 0, 1)**（**不透明黑**）。
// ⑦ **`Start` 的三件事**（`__Start.c`）：① 算一次 rect ⇒ `SetCamerasRect`；
//    ② 把 **`AdjustCameraViewport`** 注册到 `ScreenResolutionChangeSignal`（`DAT_184271f50` =
//       `Scaffold.Core.Events.Signal.Register<ScreenResolutionChangeSignal>` —— 与 `BattleCameraSreenSize`
//       的 `Start` **是同一个泛型方法信息**；`DAT_184250140` / `DAT_184250240` 在本类里 = 那两个方法指针）
//       ⇒ **分辨率一变就重算**（⛔ 不是只算一次）；
//    ③ `RenderPipelineManager.beginCameraRendering += …OnbeginCameraRendering`。
// ⑧ **`SetCamerasRect`**（`__SetCamerasRect.c`）：**先把 `cleanFrameBuffer` 置 1**、再逐台
//    `if (相机 != null) 相机.rect = viewport`（`UnityEngine_Object__op_Implicit` = Unity 的「假 null」判断）。
//    ⚠️ 原版 `targetCameras == null` 时**直接走进 `FUN_1803f47a0`（空引用抛点）**；我们改成
//    **出声一次 + 早退**（同 `BattleCameraSreenSize.Initialize` 对同类抛点的处理：那条只有
//    「引用没接上」才走得到，而每帧抛会刷屏）。⛔ 不是语义差异 —— 原版走到那儿已经崩了。
// ⑨ **`GL.Clear`**（`__RenderPipelineManagerOnbeginCameraRendering.c`）：
//    `if (cleanFrameBuffer) { GL.Clear(0, 1, 色); cleanFrameBuffer = 0; }` —— 头两个实参
//    = `clearDepth: false` / `clearColor: true` ⇒ **只清颜色**；且 **每次 `SetCamerasRect` 之后只清一次**
//    （标志清完即落 0；`SetCamerasRect` 里那句置 1 是**无条件**的，整幅那一档也会置）。
//
// ============================ 🔴 落地前核对过的那一条（简报点名要写清） ============================
// 「原版对**两台**相机同时写 rect ⇒ 我们两台相机的 `rect` 语义一致吗？」
//   **答：一致 —— 而且是 URP 自己保证的**（2026-10-18 现读 `Library/PackageCache/` 里 URP 17.3.0 源码）：
//   · 我们的 `boardCam` 是 **Base**、`cam` 是挂在它 `cameraStack` 里的 **Overlay**
//     （`Editor/BattleScene.cs:18441-18445`）；
//   · `UniversalRenderPipeline.cs:1494` **`cameraData.pixelRect = baseCamera.pixelRect;`**
//     以及 `:1658` 那句注释 **"Overlay cameras inherit viewport from base."**
//     ⇒ overlay 写不写 `rect` 都由 base 决定，**写了两边也不会打架**；
//   · 紧跟其后的那条投影补丁（`:1661`：overlay 且**非正交**且 `pixelRect` 与自己不同 ⇒ 改 `m00`）
//     **对我们两台都不触发**：base 那一台不走这一支，HUD 那一台是**正交**。
//   ⇒ 写两台 = **与原版同形**（原版也是两台都写），实际生效的是 base 那一台，HUD 跟着继承同一个盒子。
//   ⚠️ **副作用如实记（不是缺陷、也不在本件范围）**：HUD 相机的 `aspect` 是建场时**写死的 16:9**
//   （`Editor/BattleScene.cs:18404`），URP 只换视口**不换投影** ⇒ 极端比例下 HUD 是被**横向挤进盒子**
//   （不是裁剪）。这是「我们 HUD 以 16:9 为基准」本来就有的性质，**本件不引入也不修**
//   （改它要动 `LayoutSpace.VisibleWidth` 的全局口径 = 另一笔账，已写进交接报告的「顺手发现」）。
//
// ============================ 我们加的东西（⛔ 不当原版） ============================
// A. **`Attach()` / `Detach()` 这一对公开口**：原版只有 `Start()`（批处理下 `Start` 跑不跑本工程
//    **没有定论** —— 同 `BattleCameraSreenSize.RegisterResolutionSignal` 的理由）⇒ 建本件的一方
//    显式调一次 `Attach()`，自检也走它。`Attach` **幂等**（`Delegate.Combine` 不幂等）。
// B. **`OnDestroy` → `Detach()`**：原版**没有** `OnDestroy`（它就那四个方法）—— 它那条信号总线的
//    生命周期由 `Scaffold.Core.Events.Signal` 自己管，**我们没有那套**（同 `BattleCameraSreenSize`
//    文件头 A）。`beginCameraRendering` 是**引擎级静态事件**，不摘 = 攥着一个已销毁组件。
// C. **自检口**（`CurrentViewport` / `AdjustedCount` / `ComputeViewport` 静态纯函数 /
//    `SetTargetCameras` / `ClearPendingForTest` / `IsAttachedForTest`）。⛔ 生产路径一律不碰。
//    `ComputeViewport` 特意做成**静态纯函数**：四档比例（4:3 / 16:9 / 21:9 / 22.5:9）的断言
//    不该靠改真屏幕尺寸去构造（那会污染别的用例）。
//
// ============================ 我们没做 / 做不到的（如实） ============================
// D. **`GL.Clear` 只在真渲染循环里有效** —— 批处理没有帧循环，`beginCameraRendering` 不会抬。
//    本件**照原版接上**（真机生效），自检只能钉「钩子接没接上 / 标志置没置」
//    （`IsHookedForTest` / `ClearPendingForTest`），⛔ 不能钉「真的涂黑了没有」。
// E. **`ScreenResolutionChangeSignal` 那条总线我们没有** —— 等价物 = `BattleCameraSreenSize` 暴露的
//    静态事件 `ScreenResolutionChanged`（它自己就是那条信号的等价物，见它文件头 A 段）
//    ⇒ 本件**订它**，⛔ 不另开第二条「谁在比屏宽高」的路（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
using UnityEngine;
using UnityEngine.Rendering;

namespace CardPresentation
{
    /// <summary>
    /// 「比例超出 [min, max] ⇒ 给两台相机写 <c>Camera.rect</c>、并清一次未覆盖区」。
    /// <para>等价物与判据全部逐条写在本文件头部（原版 `ScreenAspectRatioController`，TypeDefIndex 3175）。</para>
    /// </summary>
    public class ScreenAspectRatioController : MonoBehaviour
    {
        // ==================================================================
        //  原版序列化字段（偏移 / ctor 值 / 场景值都有出处，见文件头 ①②③）
        // ==================================================================

        /// <summary>原版 `+0x20 [SerializeField] Camera[] targetCameras`。
        /// 场景值 = `[Camera_1461(3D BoardCamera), Camera_1462(UI 相机)]`（文件头 ④）。
        /// 我们这边由 <see cref="SetTargetCameras"/> 填 `{boardCam, cam}`。</summary>
        [SerializeField] Camera[] targetCameras;

        /// <summary>原版 `+0x28`。🔴 **取场景那一档 `2.444443941116333`**（ctor 是 `2.4444447`，文件头 ②③）。</summary>
        [SerializeField] float maxAspectRatio = 2.444443941116333f;

        /// <summary>原版 `+0x2C`。🔴 **取场景那一档 `1.3333330154418945`**（ctor 是 `1.3333334`）。</summary>
        [SerializeField] float minAspectRatio = 1.3333330154418945f;

        /// <summary>原版 `+0x30`（private，不在 inspector 上）。ctor 出厂 = `1`；`SetCamerasRect` 每次都置 1。</summary>
        bool cleanFrameBuffer = true;

        /// <summary>`GL.Clear` 那个颜色（原版 `_DAT_1834b2e40..4c` = 文件头 ⑥）= **不透明黑**。</summary>
        public static readonly Color ClearColor = new Color(0f, 0f, 0f, 1f);

        // ==================================================================
        //  原版的方法（逐句；出处见文件头 ⑤⑧⑨）
        // ==================================================================

        /// <summary>原版 `Start()` 的三件事（文件头 ⑦）。批处理下 `Start` 跑不跑没有定论 ⇒ 建本件的一方
        /// 显式调 <see cref="Attach"/>（我们加的，见文件头 A）。</summary>
        void Start() { Attach(); }

        /// <summary>🆕 **我们加的**（原版那三句在 `Start()` 里，见文件头 A）：算一次 + 订信号 + 挂钩子。**幂等**。</summary>
        public void Attach()
        {
            if (_attached) return;
            _attached = true;

            // ① 先算一次（原版 `Start` 第一句就是这段内联的数学）
            AdjustCameraViewport();

            // ② 订「分辨率变了」——等价物 = `BattleCameraSreenSize.ScreenResolutionChanged`
            //    （它自己就是 `ScreenResolutionChangeSignal` 的等价物，见文件头 E）
            BattleCameraSreenSize.ScreenResolutionChanged -= AdjustCameraViewport;
            BattleCameraSreenSize.ScreenResolutionChanged += AdjustCameraViewport;

            // ③ 挂「每台相机开渲之前」——原版 `RenderPipelineManager.beginCameraRendering += …`
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            _hooked = true;
        }

        /// <summary>🆕 **我们加的**（原版没有 `OnDestroy`，见文件头 B）：把两条订阅摘干净。
        /// 自检收工时也调它 —— 不摘的话静态事件上会挂着一个已销毁组件。</summary>
        public void Detach()
        {
            BattleCameraSreenSize.ScreenResolutionChanged -= AdjustCameraViewport;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            _attached = false;
            _hooked = false;
        }

        void OnDestroy() { Detach(); }

        /// <summary>= 原版 `AdjustCameraViewport()`（`__AdjustCameraViewport.c` 逐句）：按**当前**屏比例
        /// 算 rect 再 <see cref="SetCamerasRect"/>。**三条支**见文件头 ⑤。
        /// <para>它是那条 `ScreenResolutionChangeSignal` 的处理器（文件头 ⑦②）⇒ 分辨率一变就被调一次。</para></summary>
        public void AdjustCameraViewport()
        {
            AdjustedCount++;
            var vp = ComputeViewport((float)Screen.width / Mathf.Max(1f, Screen.height),
                                     minAspectRatio, maxAspectRatio);
            SetCamerasRect(vp);
        }

        /// <summary>**纯函数**版的公式（= 原版 `Start` / `AdjustCameraViewport` 里那段内联数学）。
        /// 🔴 特意公开成静态：四档比例的断言（4:3 / 16:9 / 21:9 / 22.5:9）直接喂比例进来，
        /// ⛔ 不用去改真屏幕尺寸（那会污染同一个进程里别的用例）。</summary>
        public static Rect ComputeViewport(float aspect, float minAspect, float maxAspect)
        {
            if (aspect <= 0f) return new Rect(0f, 0f, 1f, 1f);          // 退化输入：整幅（原版没有这一支，防除零）
            if (maxAspect < aspect)
            {
                // 宽过 max ⇒ 左右黑边（pillarbox）：rect = (f, 0, 1−2f, 1)
                float f = (1f - maxAspect / aspect) * 0.5f;
                return new Rect(f, 0f, 1f - (f + f), 1f);
            }
            if (aspect < minAspect)
            {
                // 窄过 min ⇒ 上下黑边（letterbox）：rect = (0, f, 1, 1−2f)
                float f = (minAspect / aspect - 1f) * 0.5f;
                return new Rect(0f, f, 1f, 1f - (f + f));
            }
            return new Rect(0f, 0f, 1f, 1f);                            // 其余 ⇒ 整幅
        }

        /// <summary>= 原版 `SetCamerasRect(Rect viewport)`（`__SetCamerasRect.c` 逐句，文件头 ⑧）：
        /// **先把 <see cref="cleanFrameBuffer"/> 置 1**，再逐台写 `rect`。
        /// <para>⚠️ 原版 `targetCameras == null` 是空引用抛点；我们出声一次 + 早退（文件头 ⑧）。</para></summary>
        public void SetCamerasRect(Rect viewport)
        {
            cleanFrameBuffer = true;                    // 原版：这一句在循环之前、且**无条件**
            CurrentViewport = viewport;
            SetCamerasRectCount++;

            if (targetCameras == null)
            {
                if (!_noCamerasNoted)
                {
                    _noCamerasNoted = true;
                    Debug.LogError("[ScreenAspectRatioController] `targetCameras` 是 null ⇒ **一个相机的 rect 都没写**"
                                 + "（黑边那一档不会出现）。原版这里是空引用抛点，见本文件头 ⑧。"
                                 + "建本件的一方要调 `SetTargetCameras(boardCam, cam)`。");
                }
                return;
            }

            for (int i = 0; i < targetCameras.Length; i++)
            {
                var c = targetCameras[i];
                if (c == null) continue;                // 原版也是 `op_Implicit` 逐台判（含 Unity 的「假 null」）
                c.rect = viewport;
            }
        }

        /// <summary>= 原版 `RenderPipelineManagerOnbeginCameraRendering(ScriptableRenderContext, Camera)`
        /// （`__RenderPipelineManagerOnbeginCameraRendering.c` 逐句，文件头 ⑨）：标志为真就
        /// `GL.Clear(clearDepth: false, clearColor: true, 黑)` 一次，然后**把标志落 0**。
        /// <para>⚠️ 批处理没有帧循环 ⇒ 这条**只在真机上抬**（文件头 D）。</para></summary>
        void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera cam)
        {
            if (!cleanFrameBuffer) return;
            GL.Clear(false, true, ClearColor);
            cleanFrameBuffer = false;
            ClearCount++;
        }

        // ==================================================================
        //  与建本件那一方的接线（🆕 我们加的；原版这两个引用是 inspector 上拖的）
        // ==================================================================

        /// <summary>填 `targetCameras`（原版是 inspector 拖的两个引用，文件头 ④）。
        /// ⚠️ **只为「真机上那两台」用**：写完之后**不会**自己重算一次 —— 要当场生效就再调
        /// <see cref="AdjustCameraViewport"/>（建本件的一方就是这么做的）。</summary>
        public void SetTargetCameras(params Camera[] cams)
        {
            if (cams == null || cams.Length == 0) { targetCameras = null; return; }
            int n = 0;
            for (int i = 0; i < cams.Length; i++) if (cams[i] != null) n++;   // 丢掉 null（`boardCam` 退回烘图那一档就是 null）
            if (n == 0) { targetCameras = null; return; }
            var arr = new Camera[n];
            int k = 0;
            for (int i = 0; i < cams.Length; i++) if (cams[i] != null) arr[k++] = cams[i];
            targetCameras = arr;
        }

        /// <summary>场景里那两台相机（原版 = `[Camera_1461, Camera_1462]`，文件头 ④）。
        /// ⚠️ 顺序**照原版**：先是 3D 那台、再是 UI 那台（今天两条都没差 —— URP 里 overlay 的视口由 base 覆盖，
        /// 见文件头「落地前核对过的那一条」；留着只为与原版的数组逐格对齐）。</summary>
        public Camera[] TargetCameras { get { return targetCameras; } }

        /// <summary>当前写进去的 `targetCameras` 条数（自检用）。</summary>
        public int TargetCameraCount { get { return targetCameras == null ? 0 : targetCameras.Length; } }

        // ==================================================================
        //  等价物 / 自检口（⛔ 生产路径不碰；文件头 C）
        // ==================================================================

        bool _attached;
        bool _hooked;
        bool _noCamerasNoted;

        /// <summary>`AdjustCameraViewport` 跑了几次（自检用：钉「信号真的抬到了」）。</summary>
        public int AdjustedCount { get; private set; }
        /// <summary>`SetCamerasRect` 跑了几次（它与 `AdjustedCount` 应当同增 —— 只有 `AdjustCameraViewport` 会调它）。
        /// ⚠️ 除了自检，**别处也能直调** `SetCamerasRect`（原版它是 private，我们是 public —— 见下）。</summary>
        public int SetCamerasRectCount { get; private set; }
        /// <summary>上次写下去的视口矩形（自检用：读回真值，⛔ 不重算一遍）。</summary>
        public Rect CurrentViewport { get; private set; }
        /// <summary>`GL.Clear` 真跑了几次（真机才有读数，见文件头 D）。</summary>
        public int ClearCount { get; private set; }

        /// <summary>`cleanFrameBuffer` 现在是不是挂着的（自检用：一个 `SetCamerasRect` 之后应当为 true，
        /// 直到下一次 `beginCameraRendering` 把它清掉）。</summary>
        public bool ClearPendingForTest { get { return cleanFrameBuffer; } }

        /// <summary>那条**分辨率信号**现在挂着本件吗（自检用）。
        /// 🔴 **判据只能是我们自己的记账位** —— `BattleCameraSreenSize.ScreenResolutionChanged` 是别的类的
        /// `event`，C# 只允许在**声明它的那个类里**读它（在别处读 = `CS0070`）⇒ 外部读不到订阅表。
        /// 所以这里读的是 `Attach` / `Detach` 自己维护的 <c>_attached</c>（两处写法同源，⛔ 不是第二份判据）。</summary>
        public bool IsAttachedForTest { get { return _attached; } }

        /// <summary>本件挂在引擎级 `beginCameraRendering` 上吗（自检用；与 <see cref="IsAttachedForTest"/> 分开，
        /// 因为两条订阅的生命周期不同：信号那条是别的类的静态事件、这条是引擎的）。
        /// 🔴 同 `IsAttachedForTest`：`RenderPipelineManager.beginCameraRendering` 也是 `event`，**外部读不了**
        /// ⇒ 这里读的是 <c>_hooked</c> 这个记账位。</summary>
        public bool IsRenderingHookedForTest { get { return _hooked; } }

        /// <summary>自检用：把那个「清一次」的标志摆成给定值（真机那条路抬不起来，见文件头 D）。</summary>
        public void SetClearPendingForTest(bool v) { cleanFrameBuffer = v; }

        /// <summary>自检用：`min` / `max` 两个序列化值的只读口（断言要核**场景那一档**，见文件头 ②③）。</summary>
        public float MinAspectRatioTest { get { return minAspectRatio; } }
        /// <inheritdoc cref="MinAspectRatioTest"/>
        public float MaxAspectRatioTest { get { return maxAspectRatio; } }
    }
}
