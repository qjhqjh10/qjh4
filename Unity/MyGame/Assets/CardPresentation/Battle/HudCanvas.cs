// HudCanvas.cs — 战斗 HUD 的 **uGUI 层**（`T09` 整层换 uGUI 的落脚点）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【为什么要有它】原版战斗 HUD 是 uGUI（`Canvas` + `CanvasScaler` + `GraphicRaycaster`），
//   我们原来是**世界空间 quad + 正交相机 + 手搓像素命中**。`T09` 要换成原版那套。
//   口径与逐值推导 → `资料/任务线/战斗线/任务/T09_整层换uGUI.md` §五·补③（**先读它**）。
//
// 🔑 **「一个 canvas 单位 = 一个设计 px」**（这是本层能直接吃我们既有常量 的原因）：
//   `ScreenSpaceCamera` 的 `Canvas` 的 `RectTransform` 由 Unity 撑成**相机的像素矩形**
//   ⇒ 1 单位 = 1 屏 px；正交相机可见高恒 10 世界单位 ⇒ 1 单位 = 10/屏高 世界单位；
//   16:9（1920×1080）下 = 1/108 —— **正是 `HudImageZ` 那一套 108 px/单位**。
//   ⇒ 我们那些 `w/h/cx/cy` 设计 px 常量**原样**搬进 `RectTransform`（锚点 + `sizeDelta`）就对。
//   ⇒ 再配 `CanvasScaler(1920×1080, **Expand**)`：`scaleFactor = min(W/1920, H/1080)`
//     · **16:9 ⇒ 1 ⇒ 与今天逐位相同**（现存断言全在 16:9）
//     · **4:3 ⇒ 0.75 ⇒ 1920 宽的设计稿正好铺满 1440 = 原版口径**（今天在 4:3 下整体偏大 1.333 倍）
//
// 🔑 **与现有 quad 怎么互相插队 —— 靠 `planeDistance`，⛔ 不是 `sortingOrder`**：
//   我们的 HUD 是**世界空间 quad**、相机在 `z = −20` 看 +Z、**分层靠 z**（`HudImageZ = 0.3`）
//   ⇒ `planeDistance` 就是 canvas 离相机的距离 ⇒ **`planeDistance = 目标 z + 20`**
//   ⇒ 它落在**同一套 z 排序轴**上、与 quad 正常插队。
//   ⛔ **别用 `sortingOrder`** —— 它会**先于** `renderQueue` 比较 ⇒ 整块 canvas 跳到所有 quad 前/后
//     （面板当场被盖住）。
//
// 【建层时的三条硬约束】（判据 → `资料/普查产出_1016战斗判据/T09b_uGUI拦路判据.md`）
//   1. 🔴 **`CanvasScaler` 只挂在【根】Canvas 上**，子 Canvas 一个都不加（原版逐场现读：全场景只一个）。
//   2. ⛔ **不能挂在 HUD 根下**（`Canvas` 会接管自己的 `RectTransform`、`ScreenSpaceCamera` 档还会把它
//      摆到 `planeDistance` 处 ⇒ 整棵 HUD 被搬走）；又因为 `hudRoot` 是**反向补震**用的，挂它下面会补偿两次
//      ⇒ **建成【根对象】**。
//   3. ⏸ **`EventSystem` 这一轮不建**：① 判据没收（T09b 没查它的字段，按铁律 2 **不猜**）
//      ② 现在没有可交互件 ⇒ 建了是死件。**等第 4 步（迁按钮族）与第一个可点件同批建。**
//      ✅ `GraphicRaycaster` 随 Canvas 一起建（它是 Canvas 的组件、不占独立对象）。
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardPresentation
{
    /// <summary>战斗 HUD 的 uGUI 层。**判据只此一处** —— 调用点别自己 new 一个 Canvas。</summary>
    public static class HudCanvas
    {
        /// <summary>`planeDistance` —— 决定这个 canvas 落在 HUD 的**哪一层 z** 上。
        /// 相机在 `z = −20`、看 +Z ⇒ `z = planeDistance − 20`。
        /// 现值 **20.299** ⇒ z = **0.299**：比 `HudImageZ`(0.3) **略靠前一点点**
        /// （⇒ 已迁过去的件排在还没迁的那批**前面**，同 z 平局不会发生）。
        /// ⚠️ 迁**整个 `HudImageZ` 族**之后，这个「0.001 的错位」可以去掉、直接取 20.3。</summary>
        public const float PlaneDistance = 20.3f;

        /// <summary>参考分辨率（原版 `CanvasScaler` 那两个字段）。</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        /// <summary>建出来的根 Canvas（没建过 / 已 `Reset` ⇒ null）。</summary>
        public static Canvas Root { get; private set; }
        /// <summary>根 Canvas 的 `RectTransform`（挂件挂它下面）。</summary>
        public static RectTransform RootRect { get; private set; }
        /// <summary>自检口：根 Canvas 所在对象的层（诊断「它归哪台相机画」）。</summary>
        public static int RootLayer { get { return Root != null ? Root.gameObject.layer : -1; } }

        // ---- 自检口：把根的字段读回来，对着**原版那张 6 行表**逐字段核 ----
        //   （原版值 → `资料/战斗复刻_正本.md` §2·1；`T09` 第 2 步「建完对着它逐字段核」就靠这几个口。
        //    ⚠️ 这几个口是**唯一能自证「层本身对」**的地方 —— 迁过去的件自己的断言只能证明「常量进得去」。）
        public static int RenderModeValue { get { return Root != null ? (int)Root.renderMode : -1; } }
        public static float PlaneDistanceValue { get { return Root != null ? Root.planeDistance : -1f; } }
        public static int SortingOrderValue { get { return Root != null ? Root.sortingOrder : -999; } }
        public static Camera WorldCamera { get { return Root != null ? Root.worldCamera : null; } }
        public static Vector2 ReferenceResolutionValue
        {
            get { var s = Scaler; return s != null ? s.referenceResolution : new Vector2(-1f, -1f); }
        }
        /// <summary>`CanvasScaler.ScreenMatchMode` 的整数值（原版 = **1 = Expand**）。</summary>
        public static int ScreenMatchModeValue { get { var s = Scaler; return s != null ? (int)s.screenMatchMode : -1; } }
        public static float ReferencePixelsPerUnitValue { get { var s = Scaler; return s != null ? s.referencePixelsPerUnit : -1f; } }
        /// <summary>子 Canvas **不许**有 `CanvasScaler`（原版逐场现读：全场景只有一个、在根上）——
        /// 自检用「根的 Scaler 有几份」之类别处很难查，这个口给的是「根这份在不在」。</summary>
        public static bool HasGraphicRaycaster
        {
            get { return Root != null && Root.GetComponent<GraphicRaycaster>() != null; }
        }
        static CanvasScaler Scaler { get { return Root != null ? Root.GetComponent<CanvasScaler>() : null; } }

        /// <summary>建（幂等）。`cam` = HUD 那台相机（= `LayoutSpace.Cam`）。
        /// ⚠️ **必须在 `LayoutSpace.Apply(cam)` 之后调** —— 本层要用它的 `orthographicSize`/`aspect`。</summary>
        public static RectTransform Ensure(Camera cam)
        {
            if (cam == null)
            {
                // ⛔ 相机为空 ⇒ **不许**建出一个「没有 worldCamera 的画布」（那样它一个像素都不画，
                //    而调用点会以为建成功了）。出声、返回 null，由调用点决定不建那一件。
                Debug.LogError("[HudCanvas] `cam` 为空 ⇒ uGUI 层**建不起来**（`ScreenSpaceCamera` 必须有相机）");
                return null;
            }
            if (RootRect != null)
            {
                if (Root != null && Root.worldCamera != cam) Root.worldCamera = cam;
                return RootRect;
            }

            var go = new GameObject("BattleHud Canvas");
            Root = go.AddComponent<Canvas>();          // 加 Canvas 会顺带把 Transform 换成 RectTransform
            Root.renderMode = RenderMode.ScreenSpaceCamera;
            Root.worldCamera = cam;
            Root.planeDistance = PlaneDistance;
            Root.sortingOrder = 0;                     // ⛔ 别靠它插队（见文件头）

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;   // 原版 `m_ScreenMatchMode = 1`
            scaler.referencePixelsPerUnit = 100f;                           // 原版那 6 行表里的值

            go.AddComponent<GraphicRaycaster>();       // 现在没有可点件；第 4 步一迁按钮就能用

            RootRect = (RectTransform)go.transform;
            return RootRect;
        }

        /// <summary>造一枚 uGUI 子件（**判据只此一处**：锚点/枢轴/尺寸/拉伸 的写法）。        /// `x/y` = **左上角**（设计 px，y 向下）；`w/h` = 尺寸（设计 px）；`stretch = false` ⇒ 原版 `PA=0`（拉满矩形）。
        /// 返回它的 `RectTransform`（拿不到贴图时返回 null，**并出声**，⛔ 不静默）。</summary>
        public static RectTransform MakeImage(Transform parent, Texture2D tex, string name,
                                             float x, float y, float w, float h,
                                             Color color, bool raycast = false)
        {
            if (parent == null) return null;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;

            float cx = x + w * 0.5f, cy = y + h * 0.5f;
            rt.anchorMin = rt.anchorMax = new Vector2(cx / 1920f, 1f - cy / 1080f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);          // 设计 px 直接进 `sizeDelta`（见文件头那条推导）

            var img = go.AddComponent<Image>();
            if (tex != null) img.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                                        new Vector2(0.5f, 0.5f), 100f);
            img.color = color;
            img.preserveAspect = false;                // 原版 `m_PreserveAspect = 0` ⇒ 拉满矩形
            img.raycastTarget = raycast;               // 装饰件默认不吃射线（第 4 步的可点件才传 true）
            Add(rt, DefaultZ);                         // 默认那一档（与 `HudImageZ` 同值，见 `DefaultZ`）
            return rt;
        }

        /// <summary>从 `RectTransform` 反读 **中心的设计 px**（左上原点、y 向下）—— 自检用。
        /// 判据：锚点是归一化的 ⇒ 乘回 1920 / 1080（**这是与 `MakeImage` 成对的逆**，别各写一份）。</summary>
        public static Vector2 CenterPx(RectTransform rt)
        {
            if (rt == null) return Vector2.zero;
            float ax = (rt.anchorMin.x + rt.anchorMax.x) * 0.5f;
            float ay = (rt.anchorMin.y + rt.anchorMax.y) * 0.5f;
            float cx = ax * 1920f + rt.anchoredPosition.x;
            float cy = (1f - ay) * 1080f - rt.anchoredPosition.y;
            return new Vector2(cx, cy);
        }

        /// <summary>从 `RectTransform` 反读 **尺寸的设计 px** —— 自检用。</summary>
        public static Vector2 SizePx(RectTransform rt)
        {
            return rt != null ? rt.sizeDelta : Vector2.zero;
        }

        // ── 子件序 = **按 z 排**（`T09` 翻件时的保真要点）────────────────────────────
        //  旧 quad 那一套**靠 z 分层**：`z` 大的**离相机远**、画在**后面**。
        //  uGUI **靠兄弟序**：排在**前面**的画在**底下**。
        //  ⇒ 把「按 z **降序**」映射成兄弟序，两种模型就**逐件对得上**。
        //  ⛔ **别只用「创建顺序」** —— 那一族里有 `HudImageZ - 0.02` / `- 0.065` 这种件，
        //    它们彼此之间是有明确先后的（谁压谁），创建顺序不保证复现它。
        static readonly List<float> _zs = new List<float>();
        static readonly List<RectTransform> _kids = new List<RectTransform>();

        /// <summary>把一个子件按 **z 降序**插进兄弟序里（z 大的排前面 = 画在底下）。</summary>
        public static void Add(RectTransform rt, float z)
        {
            if (rt == null) return;
            int i = 0;
            while (i < _zs.Count && _zs[i] >= z) i++;
            rt.SetSiblingIndex(i);
            _zs.Insert(i, z);
            _kids.Insert(i, rt);
        }

        /// <summary>造一件**贴着某个归一化锚点**的 uGUI 图 ——
        /// `ImageQuad.Create(parent, tex, pos, worldHeight, anchor, name)` 的对应物。
        /// `x01/y01` = 锚点（**y 从下算**，与 `LayoutSpace` 同口径）；`pivot` = 原版那个 `anchor`（轴心）；
        /// `hPx/wPx` = **设计 px**；`z` = 旧那套里的层深度（本件按它定兄弟序，见 <see cref="Add"/>）。
        /// ⚠️ 这是**唯一**给 `BattleDriver.HudImageTex` 用的建件口（⛔ 别在别处再写一份）。</summary>
        public static HudPiece MakeAnchored(Transform parent, Texture2D tex, string name,
                                            float x01, float y01, Vector2 pivot,
                                            float hPx, float wPx, float z, Color color)
        {
            if (parent == null || tex == null) return null;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(x01, y01);
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(wPx, hPx);

            var img = go.AddComponent<Image>();
            img.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                       new Vector2(0.5f, 0.5f), 100f);
            img.color = color;
            img.preserveAspect = false;      // 原版 `m_PreserveAspect = 0` ⇒ 拉满
            img.raycastTarget = false;       // 这一族是装饰；可点件走第 4 步
            Add(rt, z);
            return HudPiece.Of(rt, img);
        }

        /// <summary>把一个**已经建好的件**挪到另一个 z 档（= 旧那套的 `localPosition.z += dz`）。
        /// 🔑 uGUI 下 z 没有意义、它改的是**兄弟序** ⇒ 这里摘掉再按新 z 插回去（见 <see cref="Add"/>）。</summary>
        public static void Reorder(RectTransform rt, float newZ)
        {
            if (rt == null) return;
            int i = _kids.IndexOf(rt);
            if (i >= 0) { _kids.RemoveAt(i); _zs.RemoveAt(i); }
            Add(rt, newZ);
        }

        /// <summary>这个件在当前画布里占的 **z 档**（= 旧那套的 z）。认不出 ⇒ `NaN`（⛔ 不是 0）。</summary>
        public static float OrderZOf(RectTransform rt)
        {
            if (rt == null) return float.NaN;
            int i = _kids.IndexOf(rt);
            return i >= 0 ? _zs[i] : float.NaN;
        }

        /// <summary>旧 quad 那一套里「HUD 图统一用的那个 z」（= `BattleDriver.HudImageZ`）。
        /// 🔑 **只用来定兄弟序**（本层里没有 z 这个概念了）—— 值必须与 `BattleDriver.HudImageZ` **一致**，
        /// 否则「默认那档」的件会插错位置。</summary>
        public const float DefaultZ = 0.3f;
    }
}
