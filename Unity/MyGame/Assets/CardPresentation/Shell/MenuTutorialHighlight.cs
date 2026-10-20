// MenuTutorialHighlight.cs — 菜单侧 FTUE 的**聚光灯**（压暗整屏 + 在目标处挖一个软洞）。
//
// ============================ 出处（第一权威 = prefab / 反编译，全部本地现读）============================
// · 原版组件 = `ScreenHighlightPosition`（类桩 `D:/2/Warpforge_code/Scripts/Assembly-CSharp/ScreenHighlightPosition.cs`；
//   方法体 `d:/2/tools/decomp_full/ScreenHighlightPosition__*.c`）：
//     · 字段（prefab 实读，`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4252.json`）：
//         `image` · `autoFindUICamera = 1` · `cameraTagForAutoFind = "MainCamera"` ·
//         **`fadeTime = 0.10000000149011612`** · `debugTarget = null`
//     · `Initialize`：`image.material = new Material(image.material)`（**逐实例材质**）→
//         `originalColor = image.color` · `fadedColor = originalColor` **再把 a 置 0** → `image.color = originalColor`
//     · `Toggle(option, instant)`：`instant` → 直接开关；否则 `DOFade(image, option ? originalColor.a : 0, fadeTime)`
//     · `HighlightPosition(worldPos, camera)`：`if (!initialized) Initialize()` → `Toggle(true, false)` →
//         `viewport = camera.WorldToViewportPoint(worldPos)` → `material.SetVector(shaderViewportID, viewport)`
//       ⇒ **洞的位置靠材质的一个 Vector 属性走，不是挪 quad**。
// · 图与材质（`bundle_duplicateassetisolation_assets_all`，实读）：
//     · Sprite `Tutorial Highlight`：128×128 · `m_PixelsToUnits 100` · `m_Border (3,2,2,2)` · 一张**白色径向光斑**图。
//     · Material `Tutorial Highlight`（`Material_509935442048338405.json`）：`m_Colors` 里
//         **`_ViewportPoint = (0.5,0.5,0,0)`**（洞的视口坐标）· **`_UVScale = (1.48, 2.0, 0, 0)`**；
//         其余是内置 Standard 的残留死值（`_SrcBlend/_DstBlend/_ZWrite/_Surface` …，见 CLAUDE.md §三 那条）。
//     · Image（`MonoBehaviour_5145.json`）：`m_Color = (1,1,1,**0.8235294222831726**)` ·
//         `m_Type = 0 Simple` · `m_PreserveAspect = 0` · `m_PixelsPerUnitMultiplier = 0.01` ·
//         `m_RaycastTarget = 0`（**不吃点击**）· RectTransform 整屏 `(0,0)-(1920,1080)`（战斗场景那一份；
//         主菜单场景里那一颗出厂 `m_IsActive = 0` 且父链 `localScale = 0` ⇒ **原版主菜单不画它**，
//         它是运行时按需启用/实例化的）。
//
// · 🔴 **我们这一侧的等价实现（如实标注，不是原版做法）**：
//   原版靠 `Everguild/FX/Tutorial Highlight` 这个**自定义 shader** 把 uv 按 `_ViewportPoint/_UVScale` 重映射；
//   本工程**没有这个 shader**（`Assets/WarpforgeVFX/Shaders/` 里没有，且 bundle shader 落不成工程资产
//   —— 判据见记忆条目 `warpforge-bundle-shader-cannot-be-asset`），而本轮**不允许新增 `.shader`**。
//   ⇒ 用**同一份数学**、但不写 shader：
//     ① 运行时生成一张 128² 的**反相**径向遮罩（原图是白斑，我们生成「中心透明、边缘纯黑」的 alpha 图）；
//     ② 把它铺满整屏，用 `ImageQuad.SetUvRect` 把 uv 矩形按
//        `uv = (screenUV − viewportPoint) × _UVScale + 0.5` 摆到目标处；
//        uv 超出 [0,1] 时靠贴图 **`wrapMode = Clamp`** 拉边（= 原版 shader 的边界行为），所以四周仍是黑的；
//     ③ 颜色 = 原版 `m_Color`（alpha 0.8235），`DOFade` 换成自己 `Tick()` 里的线性淡入（fadeTime 0.1s，照抄）。
//   ⚠️ 径向 profile 是**我们的近似**：原图读不到像素（`Tutorial_Highlight.png` 的 `isReadable: 0`，
//      `GetPixels32` 会抛）⇒ 下面 `Profile()` 里的 (0.26, 0.47) 是**照原图目测的**，⛔ 不是实读值。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>菜单引导的聚光灯（原版 `ScreenHighlightPosition`，见文件头）。</summary>
    public class MenuTutorialHighlight : MonoBehaviour
    {
        // ============================================================ 原版实读参数
        /// <summary>`ScreenHighlightPosition.fadeTime`（prefab 实读 0.10000000149011612）。</summary>
        public const float FadeTime = 0.10000000149011612f;
        /// <summary>Image `m_Color`.a（prefab 实读 0.8235294222831726）。</summary>
        public const float ShadeAlpha = 0.8235294222831726f;
        /// <summary>Material `_UVScale`（prefab 材质实读 (1.48, 2.0)）。</summary>
        public static readonly Vector2 UvScale = new Vector2(1.48f, 2.0f);
        /// <summary>原图尺寸（Sprite `m_Rect` 实读 128×128）。</summary>
        public const int MaskSize = 128;

        // 队列：**原版 `Tutorial highlight` 的 Canvas = 排序层 `PopUps`(7) / order 601**
        //   （判据 → `资料/普查产出_1020/W58_界面线_Toast黑场次序判据.md` §2·3；Canvas_1073.json + TagManager_3.json）
        //   ⇒ 在窗（PopUps/0）与错误横幅（PopUps/1）**之上**、在 **toast（PopUps/900）与 Blocking 层之下**。
        // 🔑 相对次序 = 原版判据；**4040 这个绝对值 = [我们挑的]**（本仓外壳用显式渲染队列）：
        //   窗带最高 `AlliancePanelWindow.QText = 4030` 之上 · toast `4072` 与黑场 `4090` 之下。
        // ⛔ 别再把它抬回 4100（旧值：压住 toast、黑场淡入时浮在黑屏上），W58-D2 自检有灭自证梯子钉着。
        public const int QShade = 4040;

        /// <summary>遮罩贴图（运行时生成；**只建一次**，所有实例共用）。</summary>
        static Texture2D _mask;
        static bool _maskWarned;

        ImageQuad _quad;
        float _alpha;          // 当前 alpha（0..1，乘 ShadeAlpha）
        float _target;
        bool _visible;

        // ---- 自检 / 诊断读口
        public bool IsVisible { get { return _visible; } }
        public float CurrentAlpha { get { return _alpha; } }
        public Vector2 LastPointPx { get; private set; }
        public Rect LastUvRect { get; private set; }
        public Texture2D MaskTexture { get { return _mask; } }

        /// <summary>建一层（整屏）。**贴图为空 ⇒ 返回 null 并出声**（⛔ 不画假图）。</summary>
        public static MenuTutorialHighlight Create(Transform parent)
        {
            var tex = Mask();
            if (tex == null)
            {
                Debug.LogWarning("[MenuTutorial] 聚光灯遮罩贴图生成失败 ⇒ **这一层不建**（原版靠 shader 里的径向图，"
                               + "我们靠运行时反相遮罩；两者都拿不到时不许画一块纯黑盖上整屏）。");
                return null;
            }
            var go = new GameObject("Tutorial Highlight (Menu)");
            go.transform.SetParent(parent, false);
            var h = go.AddComponent<MenuTutorialHighlight>();
            var r = new PxRect(0f, 0f, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
            h._quad = MenuDraw.Rect(go.transform, tex, r, "Image", QShade, new Color(1f, 1f, 1f, 0f),
                                    false, MenuDraw.NoClip);
            if (h._quad == null)
            {
                Debug.LogWarning("[MenuTutorial] 聚光灯 quad 没建出来 ⇒ 这一层作废（出声，不静默）");
                DestroySafe(go);
                return null;
            }
            h._quad.gameObject.SetActive(false);
            return h;
        }

        /// <summary>在**设计像素坐标**（左上原点）处开洞，并按 `fadeTime` 淡入。
        /// <para>原版入参是 `worldPosition` + `Camera`；我们这一套没有 uGUI 相机 ⇒ 直接吃设计 px。
        /// 等价性：原版 `camera.WorldToViewportPoint(worldPos)` 得到的正是「屏幕归一化坐标」——
        /// 而我们的设计 px 除以 1920×1080 就是同一个量。</para></summary>
        public void ShowAt(float px, float py)
        {
            if (_quad == null) return;
            float w = Mathf.Max(1f, LayoutSpace.DesignPxW), h = Mathf.Max(1f, LayoutSpace.DesignPxH);
            float u = Mathf.Clamp01(px / w);
            float v = 1f - Mathf.Clamp01(py / h);      // 视口 y 自下而上
            // 原版 shader 的算式（材质实读 `_UVScale`）：`uv = (screenUV − _ViewportPoint) × _UVScale + 0.5`。
            // 整屏 quad 的自身 uv 就是 screenUV ⇒ 用 `SetUvRect` 表达成同一条仿射：
            //   `uv = uvRect.min + screenUV × uvRect.size` ⇒ `size = _UVScale` · `min = 0.5 − _UVScale × viewport`。
            // ⚠️ 别写成 `viewport − size/2`（那一版在非中心处**洞会偏**，2026-10-18 本轮自查时改掉的就是它）。
            var uv = new Rect(0.5f - u * UvScale.x, 0.5f - v * UvScale.y, UvScale.x, UvScale.y);
            _quad.SetUvRect(uv);
            LastUvRect = uv;
            LastPointPx = new Vector2(px, py);
            _quad.gameObject.SetActive(true);
            _visible = true;
            _target = 1f;
            // 原版 `Toggle(true, instant: false)` 的第一帧：alpha 从当前值开始淡入
            ApplyAlpha();
        }

        /// <summary>淡出（原版 `Toggle(false, instant: false)`）。</summary>
        public void Hide()
        {
            if (_quad == null) return;
            _target = 0f;
            if (_alpha <= 0.001f) HideImmediate();
        }

        /// <summary>立即关掉（原版 `Toggle(false, instant: true)`）。</summary>
        public void HideImmediate()
        {
            _target = _alpha = 0f;
            _visible = false;
            if (_quad != null)
            {
                ApplyAlpha();
                _quad.gameObject.SetActive(false);
            }
        }

        /// <summary>每帧推进淡入淡出（原版是 `DOFade`，本仓没有 tween 引擎 ⇒ 线性）。</summary>
        public void Tick(float dt)
        {
            if (_quad == null || !_quad.gameObject.activeSelf) return;
            if (Mathf.Approximately(_alpha, _target)) return;
            float step = dt / Mathf.Max(0.0001f, FadeTime);
            _alpha = Mathf.MoveTowards(_alpha, _target, step);
            ApplyAlpha();
            if (_alpha <= 0.001f && _target <= 0f) HideImmediate();
        }

        void ApplyAlpha()
        {
            if (_quad == null) return;
            _quad.SetTint(new Color(1f, 1f, 1f, _alpha * ShadeAlpha));
        }

        static void DestroySafe(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        // ============================================================ 遮罩贴图（运行时生成）
        /// <summary>反相径向遮罩：中心 alpha 0（透出底下的目标）、外圈 alpha 255（压暗）。
        /// <para>⚠️ 轮廓是**我们的近似** —— 原图的像素读不出来（`isReadable: 0`），
        /// 参数 (0.26, 0.47) 是照原图目测的归一化半径（内圈全透 / 外圈全黑），⛔ 不是实读值。</para></summary>
        static Texture2D Mask()
        {
            if (_mask != null) return _mask;
            try
            {
                var t = new Texture2D(MaskSize, MaskSize, TextureFormat.RGBA32, false);
                t.name = "Tutorial_Highlight_Mask_Runtime";
                t.wrapMode = TextureWrapMode.Clamp;       // 🔴 原版 shader 在 [0,1] 外拉边 ⇒ 必须 Clamp
                t.filterMode = FilterMode.Bilinear;
                var px = new Color32[MaskSize * MaskSize];
                for (int y = 0; y < MaskSize; y++)
                    for (int x = 0; x < MaskSize; x++)
                    {
                        float dx = (x + 0.5f) / MaskSize - 0.5f;
                        float dy = (y + 0.5f) / MaskSize - 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.26f, 0.47f, d));
                        px[y * MaskSize + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(a * 255f));
                    }
                t.SetPixels32(px);
                t.Apply(false, false);
                _mask = t;
            }
            catch (System.Exception e)
            {
                if (!_maskWarned)
                {
                    _maskWarned = true;
                    Debug.LogWarning("[MenuTutorial] 聚光灯遮罩生成异常：" + e.Message + " ⇒ 这一层不建（出声，不静默）");
                }
                _mask = null;
            }
            return _mask;
        }
    }
}
