// UIBakeScreen.cs — 原版 `UIBakeScreen`：把**上一帧 UI** 烘成一张 1/4 分辨率的模糊快照
// （界面线 T16「离屏烘焙 + UI Blur」）
//
// ============================ 出处（判据逐条现核） ============================
// · 类签名桩 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/UIBakeScreen.cs`
// · 方法体   → `d:/2/tools/decomp_full/UIBakeScreen__{Start,Toggle,CreateRT,ResolutionChanged,OnDestroy}.c`
// · 原版节点 → `bundle_scenes_scenes_mainmenuwarpforge`：`Game UI ▸ Main Canvas ▸ Background Render Texture Holder`
//              （GO 42 / RT 1118；组件 = `CanvasRenderer` + `UIBakeScreen`（MB 1976）+ `RawImage`（MB 1977））
//              · `RawImage.m_Texture = null` · `m_Color` 白 · **`m_RaycastTarget = 0`** · 锚 (0,0)-(1,1) / sizeDelta 0 ⇒ 整屏
//              · `UIBakeScreen` 字段名逐字：`cameraToBake`（= UI Camera）· `image`（= 那块 RawImage）·
//                `blitBlurMaterial`（= `Material/Material_3.json` = 材质 `UI Blur`）
// · 持窗者   → `MainMenuWindow.screenBaker`（`MainMenuWindow__{MenuFocusChangeActions,ToBackground,ToFocus,Hide,Close}.c`）
// · 汇总判据 → `资料/普查产出_1017/UI/D_T16_UIBakeScreen判据.md`
//
// ---------- 原版行为（逐句读出来的，代码里每个分支都对着它写） ----------
// · `CreateRT()`：已有 RT ⇒ `DestroyImmediate`；`w = ceil(Screen.width * RENDER_TEXTURE_MULTIPLIER)`、
//   `h = ceil(Screen.height * 0.25)`；`new RenderTexture(w, h, 0x18 /*24 位深度*/, 0)`；设名字；`image.texture = RT`。
// · `Toggle(bool on)`：
//     `on == true` ⇒ ① 没 RT ⇒ `CreateRT()`；② `RenderTexture.GetTemporary(w, h, 0x10 /*16*/)`；
//                    ③ 建 URP `SingleCameraRequest`（destination = 这块 RT）并 `SubmitRenderRequest`
//                       ⇒ **把相机这一帧渲进 RT**（我们不用 URP API，等价物 = `camera.targetTexture` + `Render()`）；
//                    ④ `Graphics.Blit(RT, tmp, blurMat)`（pass 0 = 横向）；
//                    ⑤ `Graphics.Blit(tmp, RT, blurMat, 1)`（pass 1 = 纵向）；
//                    ⑥ `Release(tmp)`；
//     **两条都** ⇒ 末尾 `image.enabled = on`。
// · `Start()`：把 `ResolutionChanged` 注册到「屏幕分辨率变了」的信号；**`image.enabled = false`**（出厂不显示）。
// · `ResolutionChanged()`：`image.enabled == false`（= 没在用）⇒ `CreateRT()`；否则不动。
// · `OnDestroy()`：`Destroy(renderTexture)`。
//
// ============================ 我们这边的等价物（本仓特有） ============================
// 🔴 壳是 **正交相机 + 世界空间 `ImageQuad` + 显式渲染队列**（`Shell/ShellRuntime.cs` 文件头），**不是 uGUI**
//    ⇒ 原版的 uGUI `RawImage` 在这里 = **一块整屏 `ImageQuad`**；原版的 `image.enabled = on` 在这里 =
//      **开关那块 quad 的 GameObject**（`ImageQuad.enabled` 只关 MonoBehaviour，MeshRenderer 照样画）。
// 🔴 **不登记命中区**：整屏 quad 子树里**没有任何 `WindowButton`**（= 原版 `m_RaycastTarget = 0`）。
// 🔴 **烘之前先把快照自己藏掉**：否则会把「上一帧的模糊底」再烘进去（自反馈、越烘越糊）。
// 🔴 **渲染队列 `DisplayQueue = 2895`**（**我们自己起的号**，本仓没有原版那套排序层）：
//    约束 = **高过壳背景/压暗、低过全部窗** —— 现读梯子：压暗层 `ShellRuntime.FadeQueue = 2890`、
//    主菜单 `MainMenuRuntime.QBg = 2900 … QOverlay = 2930`、顶栏 2986–2998、卡组编辑 2980–2999。
//    ⇒ 2895 落在 **2890 < q < 2900**：压暗（含它自己的烘焙结果）在下面，任何一扇窗都盖得住它。
// 🔴 **分辨率变化**：原版走「屏幕分辨率变了」的信号；我们没有那个信号 ⇒ 简化为 `Update` 里比一次
//    `Screen.width/height`（只在**不显示**时重建，语义与 `ResolutionChanged()` 一致）。
// ⚠️ **RT 的 format**：原版实参是 `0`（= `RenderTextureFormat.ARGB32`，与 `Default` 同值）。
// ⚠️ **RT 的名字**：原版那一句 `set_name` 的字符串常量（`DAT_1842a4768`）**没有读出** ⇒ 这里用
//    节点名（`Background Render Texture`）当名字，**这是我们的选择**，⛔ 别写成「原版的名字就是这个」。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `UIBakeScreen` —— 主菜单不在前台时，屏上显示「上一帧 UI 的模糊快照」。
    /// 开关时机由 <see cref="GameWindow"/> 一族按原版 `MainMenuWindow` 那五处钩子驱动
    /// （压背景 / 隐藏 / 关闭 ⇒ 先烘再藏；回前台 ⇒ 关）。</summary>
    public class UIBakeScreen : MonoBehaviour
    {
        // ---- 原版常量（`UIBakeScreen__CreateRT.c` 的 `DAT_1834b2e7c` 实读 = 0.25f；
        //      `Material_3.json` + `Shader_339` 的属性表 = `_Sigma 2.25` / `_steps 10`）----
        /// <summary>RT 尺寸倍率（原版 `RENDER_TEXTURE_MULTIPLIER`）。</summary>
        public const float RENDER_TEXTURE_MULTIPLIER = 0.25f;
        /// <summary>RT 深度位数（原版 `new RenderTexture(w, h, 0x18, 0)`）。</summary>
        public const int RTDepthBits = 24;
        /// <summary>中间那趟 `GetTemporary` 的深度位数（原版 `0x10`）。</summary>
        public const int TempDepthBits = 0x10;
        /// <summary>模糊的高斯 σ（原版材质 `_Sigma`）。</summary>
        public const float Sigma = 2.25f;
        /// <summary>模糊的采样步数（原版材质 `_steps`）。</summary>
        public const float Steps = 10f;
        /// <summary>自建等价 shader（等价实现、未逐条核原版字节码 —— 见 `Shaders/UIBlur.shader` 文件头）。</summary>
        public const string ShaderName = "CardPresentation/UIBlur";
        /// <summary>整屏快照的渲染队列（我们起的号；理由见文件头）。</summary>
        public const int DisplayQueue = 2895;
        /// <summary>RT 名字 —— **原版实读** `"RT Background bake"`。
        /// 判据：`UIBakeScreen__CreateRT.c:16,54` 的 `DAT_1842a4768`，RVA = `0x1842a4768 − 0x180000000 = 0x42A4768`；
        /// `d:/2/tools/il2cpp_out/stringliteral.json` 里该地址 = `"RT Background bake"`
        /// （同一手法解过 `"Everguild/UI/Greyscale" @0x42AB248`，见 `Shell/MissionsTab.cs` 的 `x42AB248` 附近）。</summary>
        public const string RTName = "RT Background bake";

        // ---- 原版字段（名字逐字照抄）----
        /// <summary>烘哪台相机（原版 `cameraToBake`；我们 = `Camera.main` = `LayoutSpace.Apply` 用的那台）。</summary>
        public Camera cameraToBake;
        /// <summary>整屏那块图（原版是 uGUI `RawImage`；我们 = 世界空间 `ImageQuad`）。</summary>
        public ImageQuad image;
        /// <summary>两趟模糊用的材质（原版 `blitBlurMaterial` = `UI Blur`）。</summary>
        public Material blitBlurMaterial;

        /// <summary>离屏 RT（原版 private 字段 `renderTexture`；这里留一个只读口给自检）。</summary>
        RenderTexture renderTexture;
        /// <summary>当前 RT（没有就是 null —— 原版也是懒建）。**自检用**。</summary>
        public RenderTexture RT { get { return renderTexture; } }

        /// <summary>快照现在是否显示在屏上（= 原版 `image.enabled`）。</summary>
        public bool Showing { get { return image != null && image.gameObject.activeSelf; } }

        /// <summary>烘过几次（诊断 / 自检用）。</summary>
        public int BakeCount { get; private set; }
        /// <summary>上一趟烘焙是否因为环境原因失败（批处理没有图形设备时会这样 —— 已出声）。</summary>
        public bool LastBakeFailed { get; private set; }

        /// <summary>当前那一台（**只读口**）。批处理/编辑态 `ShellRuntime.Instance` 恒 null
        /// （`Awake` 不跑）⇒ `GameWindow` 那一族靠它找本件；生产态仍以 `ShellRuntime.Bake` 为准。
        /// 形状照 `LoadingMenu.Current`（同一条坑）。</summary>
        public static UIBakeScreen Current { get; private set; }

        int _rtW, _rtH;      // 上次建 RT 时看到的屏幕尺寸（判「分辨率变了」用）

        void Awake() { Build(); }

        /// <summary>建材质 + 整屏 quad（**幂等**）。批处理/编辑模式里 `Awake` 不跑 ⇒ 由 `ShellRuntime.Build` 显式调
        /// （同 `LoadingMenu.Build` 的规矩）。</summary>
        public void Build()
        {
            Current = this;

            if (blitBlurMaterial == null)
            {
                var sh = Shader.Find(ShaderName);
                if (sh == null)
                {
                    // 出声（红线：不许静默失败）—— 没有 shader 时两趟 Blit 会退化成「直接拷」
                    Debug.LogError("[Bake] 找不到 shader `" + ShaderName + "` —— " +
                                   "`Assets/CardPresentation/Shaders/UIBlur.shader` 不在（或没被导入）⇒ " +
                                   "模糊快照会**不模糊**（两趟管线建不出来）");
                }
                else
                {
                    blitBlurMaterial = new Material(sh);
                    blitBlurMaterial.name = "UI Blur (ours)";
                    blitBlurMaterial.SetFloat("_Sigma", Sigma);   // 原版材质实读值
                    blitBlurMaterial.SetFloat("_steps", Steps);   // 同上
                }
            }

            if (image == null)
            {
                // 整屏：可见高度固定 10 世界单位（`LayoutSpace.DesignHeight`），宽按贴图比例
                // ⇒ RT 建好、`SetTexture` 换成屏幕比例之后正好铺满整屏（与原版 stretch 锚等价）。
                image = ImageQuad.Create(transform, CardArt.Solid(), Vector3.zero,
                                         LayoutSpace.DesignHeight, new Vector2(0.5f, 0.5f),
                                         "Background Render Texture");
                if (image == null)
                {
                    Debug.LogWarning("[Bake] 整屏快照那块 quad 建不出来（`CardArt.Solid()` 没给图）—— " +
                                     "离屏烘焙**不会显示**（Toggle 会空转）");
                }
                else
                {
                    image.SetRenderQueue(DisplayQueue);
                    image.gameObject.SetActive(false);   // = 原版 `Start`: `image.enabled = false`（出厂不显示）
                }
            }

            if (cameraToBake == null) cameraToBake = Camera.main;
            _rtW = Screen.width;
            _rtH = Screen.height;
        }

        /// <summary>原版 `CreateRT()`：销毁旧 RT → `ceil(屏 × 0.25)` 建新 RT（24 位深度）→ `image.texture = RT`。</summary>
        public void CreateRT()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                // 原版是 `DestroyImmediate`；播放态用 `Destroy`（帧末），编辑态必须 `DestroyImmediate`
                //（同 `ShellRuntime.DestroySafe` 的规矩）。
                if (Application.isPlaying) Destroy(renderTexture);
                else DestroyImmediate(renderTexture);
                renderTexture = null;
            }

            int w = Mathf.CeilToInt(Screen.width * RENDER_TEXTURE_MULTIPLIER);
            int h = Mathf.CeilToInt(Screen.height * RENDER_TEXTURE_MULTIPLIER);
            if (w < 1 || h < 1)
            {
                // 原版不会看到 0（有窗口系统）；批处理/无图形环境下 Screen 可能是 0 ⇒ 出声并兜到 1
                Debug.LogWarning($"[Bake] 屏幕尺寸异常（{Screen.width}×{Screen.height}）⇒ RT 尺寸兜到至少 1×1" +
                                 "（原版没有这一档；只为别让 `new RenderTexture(0,0,…)` 抛异常）");
                w = Mathf.Max(1, w); h = Mathf.Max(1, h);
            }

            // 原版 `new RenderTexture(w, h, 24, 0)`：format 实参 0 = `RenderTextureFormat.ARGB32`（= `Default`）
            renderTexture = new RenderTexture(w, h, RTDepthBits, RenderTextureFormat.Default);
            renderTexture.name = RTName;
            _rtW = w; _rtH = h;

            if (image != null)
            {
                // 原版 `RawImage.texture = RT`（stretch 锚 ⇒ 铺满整屏）。
                // ⚠️ 我们的 `ImageQuad.SetTexture` **只改 `_aspect`、不重建网格**（`ImageQuad:210`
                //    —— 重建要另调 `SetAspect/SetWorldHeight`）⇒ 必须先「只换图」再显式 `SetAspect`，
                //    否则整屏 quad 停在 Solid(1:1) 那个 10×10 网格上，RT 被拉伸成方的。
                image.SetTexture(renderTexture, true);
                image.SetAspect(renderTexture.width / (float)renderTexture.height);
            }
            else
            {
                Debug.LogWarning("[Bake] `CreateRT()` 时整屏 quad 不在 ⇒ RT 建了但**没贴到任何东西上**");
            }
        }

        /// <summary>原版 `Toggle(bool on)` —— 见文件头逐句对照。
        /// <para>⚠️ 与原版**唯一有意的差别**：烘之前先把快照自己关掉（原版没有这一句；不关会把上一次的
        /// 模糊底再烘进去，自反馈）。</para></summary>
        public void Toggle(bool on)
        {
            if (on)
            {
                if (image == null)
                {
                    Debug.LogWarning("[Bake] `Toggle(true)`：整屏 quad 不在 ⇒ 什么都不做（不出画面）");
                    return;
                }
                if (renderTexture == null) CreateRT();
                if (renderTexture == null) return;

                // 🔴 **先藏自己再烘**（见文件头）
                image.gameObject.SetActive(false);
                Bake();
            }

            if (image != null) image.gameObject.SetActive(on);   // = 原版末尾那句 `image.enabled = on`
        }

        /// <summary>`Toggle(true)` 里「把相机渲进 RT + 两趟模糊」那一段（原版内联在 `Toggle` 里；
        /// 我们拆成一个方法只为让上面那个分支读起来像判据表）。</summary>
        void Bake()
        {
            if (blitBlurMaterial == null)
            {
                Debug.LogWarning("[Bake] 没有模糊材质 ⇒ 跳过两趟 Blit（RT 里会是没有模糊的/空的内容）");
                return;
            }

            // 🔴 原版 `cameraToBake` 是序列化字段；我们的壳相机会**随场景切换被销毁**（`ShellRuntime.LoadMainMenu`
            //    把壳相机关了、主菜单场景有自己那台）⇒ 这里取「当前那台」：字段活着就用它，否则问 `Camera.main`。
            var cam = cameraToBake;
            if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
            cameraToBake = cam;
            if (cam == null)
            {
                Debug.LogWarning("[Bake] 场上没有可用相机 ⇒ 这一趟烘不出东西（快照会是空的）");
                return;
            }

            var tmp = RenderTexture.GetTemporary(renderTexture.width, renderTexture.height, TempDepthBits);
            var prevTarget = cam.targetTexture;
            try
            {
                // ③ 把相机这一帧渲进 RT（= 原版 URP `SingleCameraRequest{destination = RT}` 的等价物）
                cam.targetTexture = renderTexture;
                cam.Render();

                // ④⑤ 两趟可分离模糊（pass 0 = 横向 · pass 1 = 纵向 —— 原版两次 Blit 的 pass 实参）
                Graphics.Blit(renderTexture, tmp, blitBlurMaterial, 0);
                Graphics.Blit(tmp, renderTexture, blitBlurMaterial, 1);

                BakeCount++;
                LastBakeFailed = false;
            }
            catch (System.Exception e)
            {
                // 批处理/无图形环境里相机渲染可能不可用 —— 出声，但别让主菜单的开/关因此炸掉
                LastBakeFailed = true;
                Debug.LogWarning("[Bake] 相机渲染 / 两趟模糊失败（批处理或无图形设备时常见）：" + e.Message);
            }
            finally
            {
                cam.targetTexture = prevTarget;          // 别把相机永久绑到 RT 上
                RenderTexture.ReleaseTemporary(tmp);     // ⑥ = 原版 `Release(tmp)`
            }
        }

        void Update()
        {
            // 原版 `Start` 是把 `ResolutionChanged` 注册到「屏幕分辨率变了」的信号上；我们没有那个信号
            // ⇒ 每帧比一次（`Screen.width/height` 是廉价读）。语义 = `ResolutionChanged()`。
            if (Screen.width != _rtW || Screen.height != _rtH) ResolutionChanged();
        }

        /// <summary>原版 `ResolutionChanged()`：**没在显示**（= `image.enabled == false`）⇒ 重建 RT；否则不动。</summary>
        public void ResolutionChanged()
        {
            if (Showing) return;
            CreateRT();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;

            if (renderTexture != null)
            {
                renderTexture.Release();
                if (Application.isPlaying) Destroy(renderTexture);
                else DestroyImmediate(renderTexture);
                renderTexture = null;
            }
            if (blitBlurMaterial != null)
            {
                // ⚠️ 原版只销毁 RT；材质是我们 `new` 的（原版是资产）⇒ 跟着一起销毁，别让它在编辑期泄漏。
                if (Application.isPlaying) Destroy(blitBlurMaterial);
                else DestroyImmediate(blitBlurMaterial);
                blitBlurMaterial = null;
            }
        }
    }
}
