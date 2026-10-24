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
//   2. ⛔ **根 canvas 不能挂在 HUD 根下**（`Canvas` 会接管自己的 `RectTransform`、`ScreenSpaceCamera` 档还会把它
//      摆到 `planeDistance` 处 ⇒ 整棵 HUD 被搬走）；又因为 `hudRoot` 是**反向补震**用的，挂它下面会补偿两次
//      ⇒ **根 canvas 建成【根对象】**。
//      🔴 **2026-10-16 补**：**面板族不建自己的根 canvas**，而是**嵌在根 canvas 下的子 canvas**
//      （`EnsureSubCanvas`）—— 子 canvas **继承根 canvas 的缩放**，又靠 `overrideSorting` 另立排序组；
//      这正是原版的做法（原版**全场景只有一个 `CanvasScaler`**，在根上）。
//      ⛔ 子 canvas **一个 `CanvasScaler` 都不许加**（加了会二次缩放）。口径 → `T09` §五·补⑨ 一。
//   3. ✅ **`EventSystem` 已于 2026-10-19（`T09` 步骤 A1）与第一颗可点件同批建出** ——
//      判据与坑位**搬到 `Battle/BattleEventSystem.cs` 文件头**（⛔ 别在这里再写一份）：
//      `EventSystem` + **`InputSystemUIInputModule`**（`activeInputHandler = 1` New-only，⛔ 不能用旧模块）、
//      编辑器「`AddComponent` 不派 `OnEnable`」的补登记（`SyncRaycasters`）。
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
        /// 相机在 `z = −20`、看 +Z ⇒ `z = planeDistance − 20` = **`HudImageZ`(0.3)**。
        /// 🔴 **2026-10-16 订正**：本行原来写「现值 **20.299** ⇒ 比 `HudImageZ` 略靠前」，
        /// **已过期** —— 图族**整族**迁上 canvas 之后，那个「0.001 的临时错位」就**没有意义了**，
        /// 已改回 **20.3**（判据 → `T09` §五·补⑦ 开头）。⛔ 别再按 20.299 推。</summary>
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

            go.AddComponent<GraphicRaycaster>();       // 射线面；可点件由 `BattleHudButton.Attach` 喂（`T09` A1 起）

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
            img.raycastTarget = raycast;               // 装饰件默认不吃射线（`T09` A1 起，可点件由 `BattleHudButton.Attach` 置 true）
            Add(rt, DefaultZ);                         // 默认那一档（与 `HudImageZ` 同值，见 `DefaultZ`）
            return rt;
        }

        /// <summary>🔴 **2026-10-16：建一块 uGUI 图，位置/尺寸【相对父节点的中心】**（px、**y 向上**）。
        /// <para>⛔ **与 `MakeImage`/`QuadFromTopPx` 的区别**：那两个把 `x/y` 当**屏幕绝对 px**
        /// （锚点 = `x/1920`、`1 − y/1080`）—— **只有当 `parent` 本身是整屏矩形时**它才等于「相对父中心」。
        /// 面板里的**子矩形容器**（`SkullsHolder` 之类）**不是**整屏 ⇒ 套那套会**静默摆到别处**。</para>
        /// <para>🔑 **为什么补齐这一个口**：批 2 有**两个写手各自撞了一次**、各造了一个权宜
        /// （`SettingsPanel` 用 `MakeSlicedPiece + border=0` 顶、`EndPanel` 自造 `ImageInParent`）——
        /// 那正是「同一条规矩的第二份写法」。⇒ 收编到这里，⛔ **别再长出第三份**。</para>
        /// <para>`xPx/yPxFromTop` = 中心点（**相对父中心**、左上原点 y 向下）；`wPx/hPx` = 尺寸；
        /// `z` = 兄弟序的层深度（见 <see cref="Add"/>）。</para></summary>
        public static HudPiece MakeImageInParent(Transform parent, Texture2D tex, string name,
                                                 float xPx, float yPxFromTop, float wPx, float hPx,
                                                 float z, Color color)
        {
            if (parent == null || tex == null) return null;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);   // 中心锚 + 中心枢轴
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xPx, -yPxFromTop);     // ⚠️ 入参 y **向下** ⇒ 取负
            rt.sizeDelta = new Vector2(wPx, hPx);

            var img = go.AddComponent<Image>();
            img.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                       new Vector2(0.5f, 0.5f), 100f);
            img.color = color;
            img.preserveAspect = false;      // 原版 `m_PreserveAspect = 0` ⇒ 拉满
            img.raycastTarget = false;
            Add(rt, z);
            return HudPiece.Of(rt, img);
        }

        /// <summary>🔴 **2026-10-16：建一块【九宫格】uGUI 图** ——
        /// `ImageQuad` 那套九宫格（`MenuDraw.Nine`，九个 quad）在 uGUI 里的对应物就是**一张
        /// `Image` + `Type.Sliced`**（原版 tooltip 面板本来就是 `Image (Sliced)`）。
        /// <para>**锚点/枢轴全在中心**（`anchorMin = anchorMax = pivot = (0.5,0.5)`）⇒
        /// 调用点之后写 `localPosition` 就是「相对父中心的 px 偏移」，与 `Label.CreateUgui` 同口径。</para>
        /// <para>⚠️ **`pixelsPerUnitMultiplier` 取默认 1** —— 原版那颗 `Image` 这个字段的值**主对话没读到**
        /// （⛔ 不猜，如实记）。它决定角块画多大（角块 = `m_Border ÷ ppuMul`）；真要逐位对齐得回去现读。</para></summary>
        public static HudPiece MakeSlicedPiece(Transform parent, Texture2D tex, string name,
                                               float wPx, float hPx, Vector4 border, Color color)
        {
            if (parent == null || tex == null) return null;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);   // 见 doc：中心锚 + 中心枢轴
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(wPx, hPx);

            var sp = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                   new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            var img = go.AddComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Sliced;       // 九宫格（原版就这么干的）
            img.color = color;
            img.raycastTarget = false;
            return HudPiece.Of(rt, img);
        }

        // ── 🆕 2026-10-16：**面板族迁移的四个换算助手**（`T09` §五·补⑩ 那张配方表的口子）────────
        //  ⚠️ **2026-10-16（`G3`）补注**：本段现在实际有 **6 个**口 —— 除了那「四个」（`FillParent` /
        //     `SetPxSize` / `PosPx` / `QuadFromTopPx`）之外，收口波又补进了 **`AnchorApPx` / `SetAnchorPx`**
        //     这对（「点锚 + 该角偏移」的共用式，见它们的 doc）。⛔ **抬头那个「四个」是 `T09` §五·补⑩
        //     那张配方表的口径、不是口数** —— 要改它得跟 `T09` 同批（本文件⛔不单独改那个数字）。
        //  🔴 **为什么必须另立**：面板那一族今天全走 `MenuDraw.SetPxSize` / `LayoutSpace.Px`，
        //     而那两处写的都是**世界单位**（`SetPxSize` 内部就是 `LayoutSpace.Px(w)` = **÷108**）——
        //     ⛔ uGUI 档的 `sizeDelta` 要的是**真 px**（canvas 单位就是设计 px）。
        //     直接沿用会把整块面板缩成 1/108（**而画面只是「小得看不见」，不报错**）。

        /// <summary>让一个节点**铺满它的父节点**（中心为原点、单位 = 设计 px）。全屏覆盖层用它 ——
        /// ⛔ 别写死 `sizeDelta = 1920×1080`：`Expand` 档下**屏比画布宽**时（21:9 等）
        /// 画布 rect 比 1920 宽 ⇒ 写死的矩形**盖不满两边**。</summary>
        public static void FillParent(RectTransform rt)
        {
            if (rt == null) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        /// <summary>🔴 **`MenuDraw.SetPxSize` 的 uGUI 版**：中心锚 + **`sizeDelta` 直接吃设计 px**
        /// （⛔ 不走 `LayoutSpace.Px` —— 那个是 ÷108）。</summary>
        public static void SetPxSize(RectTransform rt, float wPx, float hPx)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(wPx, hPx);
        }

        /// <summary>🔴 **「左上原点、y 向下」的设计 px → canvas 局部坐标**（中心原点、**y 向上**）。
        /// 就是面板族那些 `Pos(px, py, z)` / `Content(px, py)` 在 uGUI 档的对应物
        /// （`EndPanel.Pos` 的式子 ×108 即此）。</summary>
        public static Vector3 PosPx(float xPx, float yPxFromTop)
        {
            return new Vector3(xPx - ReferenceResolution.x * 0.5f,
                               ReferenceResolution.y * 0.5f - yPxFromTop, 0f);
        }

        /// <summary>🔴 **2026-10-16：一个件的【枢轴点】→ 它相对【点锚】`(ax, ay)` 那个角的
        /// `anchoredPosition`**（设计 px、**y 向上**）。
        /// <para>式子：`ap = (xPx − ax·1920, 1080·(1 − ay) − yPxFromTop)`
        /// —— 即「目标点 **减** 锚点那一点」（两轴都是减法）。</para>
        /// <para>🔑 **推导**：点锚 `(ax, ay)` 指的是**父矩形**（= 画布）的那个角，在 1920×1080 帧里
        /// 正好落在设计 px `(ax·1920, (1−ay)·1080)` ⇒ 「枢轴点落在 `(xPx, yPxFromTop)`」⇔ 上式。
        /// **与 `pivot` 无关** —— 式子只管「枢轴点摆在哪」，所以图（中心枢轴）与字（`Label` 的枢轴）
        /// **共用同一个 `(xPx, yPxFromTop, ax, ay)`**（同 `UnitChatPanel.Move` 那条 doc）。</para>
        /// <para>⛔ **别拿 `PosPx` 顶它**：那个是「相对**父件中心**」的局部坐标（且只在父件枢轴居中时才等于
        /// `anchoredPosition`）；锚点一旦不是父件中心，两者**不再同值**（`T09` §八·补 陷阱 1）。</para></summary>
        public static Vector2 AnchorApPx(float xPx, float yPxFromTop, float ax, float ay)
        {
            return new Vector2(xPx - ax * ReferenceResolution.x,
                               ReferenceResolution.y * (1f - ay) - yPxFromTop);
        }

        /// <summary>🔴 **2026-10-16：把一件摆成「点锚 `(ax, ay)` + 枢轴点落在设计 px `(xPx, yPxFromTop)`」**
        /// （左上原点、y 向下）—— 就是 <see cref="AnchorApPx"/> 的落盘那一半。
        /// <para>⛔ **必须写 `anchoredPosition`、不许写 `localPosition`**：`localPosition` 的原点是**父件枢轴**，
        /// 锚点不是父件中心时两者**不再同值**（`T09` §八·补 陷阱 1；先例 = `Alliance Panel` 那扇偏
        /// `(301.566, −154.528)` px，**16:9 下也错**）。✅ 本口写的是 `anchorMin = anchorMax = (ax, ay)`
        /// ⇒ `anchoredPosition` 本身就是「相对那个角」的 px，**与父 pivot 无关**。</para>
        /// <para>⚠️ 它**只管位置与锚点**，⛔ 不碰 `pivot` / `sizeDelta`（本件的枢轴由建件口定，
        /// 尺寸另走 `SetPxSize`）。</para>
        /// <para>🔑 **为什么补齐这一个口**（🔴 **2026-10-16 修波 `FD` 就地订正：整段改成过去时 + 留痕**）：
        /// **本口建立时**，同一式子在两个文件里各写了一遍 —— `UnitChatPanel.ApPx`（静态函数）与
        /// `ChatPopupPanel.PanelApPx`（编译期常量）＋ 同文件 `Place` 里**又内联了一遍**
        /// （`new Vector2(r.x + r.width*0.5f − PanelAnchorX*RefW, …)`）。⇒ **当时先收编到这里**
        /// （铁律：两处写同一条规则 = 迟早不一致）。
        /// ✅ **现状（2026-10-16 `G5` 收工后本波现核，`grep ApPx/PanelAnchorX/RefW/RefH` 全仓对过）**：
        /// 上面那**三个副本已全部删掉**（`UnitChatPanel.ApPx` 与 `ChatPopupPanel.PanelApPx` 都留了「【已删】」
        /// 的更正痕，调用点已改走 `AnchorApPx`/`SetAnchorPx`）⇒ **这个算式现在全仓只有一处** = 本口算 +
        /// <see cref="SetAnchorPx"/> 落盘。</para>
        /// <para>🔴 **下面那段「应当改用本口的」是【建立时的待办】—— 已办结**（`UnitChatPanel` /
        /// `ChatPopupPanel` 两扇都换过来了）；把它留在这里只为保留当时的口径与 `AlliancePanelWindow`
        /// 那半句「**不属于机械替换**」的甄别，⛔ **别照它再去做一次**。</para>
        /// <para>🔴 **应当改用本口的**：`UnitChatPanel` · `ChatPopupPanel` 两扇 —— 它们是**同形算式**，
        /// 换过来是**逐位等价的机械替换**（两处 `RefW/RefH` 都 = 1920/1080 = 本类的
        /// <see cref="ReferenceResolution"/>，已现核）。
        /// ⚠️ **`AlliancePanelWindow` 不属于「机械替换」那一档**（现核）：它的根锚是**字面常量**
        /// （`RootAnchorMin/Max` · `RootAnchoredPos`，逐值照抄原版 `RT_3052`），**没有**再写一遍这个算式；
        /// 它那个 `RootCenterLocal = (0.5 − pivot) × sizeDelta` 是**另一件事**（「枢轴 ≠ 中心」的补偿，
        /// ⛔ **不在本口范围内**）。要不要把它的字面常量改写成算式表达 = **另裁**（不改也正确）。</para>
        /// <para>⛔ **本波【不】去改上面这些文件**（那是跨文件的机械替换，另开一件；见 `G3` 报告 ③）。</para></summary>
        public static void SetAnchorPx(RectTransform rt, float ax, float ay, float xPx, float yPxFromTop)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.anchoredPosition = AnchorApPx(xPx, yPxFromTop, ax, ay);
        }

        /// <summary>🔴 **`ImageQuad.Create(parent, tex, pos, worldHeight, anchor, name)` 的 uGUI 版**
        /// （**按【高】定、宽由贴图比例来** —— ⛔ 别当成正方、也别当成 `(w,h)` 照抄）。
        /// <para>`xPx/yPxFromTop` = 中心点（**左上原点、y 向下**的设计 px）；`hPx` = 高低（设计 px）；
        /// `z` = 旧那套的层深度（本函数登记它的兄弟序，见 <see cref="Add"/>）。</para>
        /// <para>⚠️ 要**盖掉贴图比例**的（原版 `PA=0` 拉满那种）建完自己调 `HudPiece.SetAspect`。</para></summary>
        public static HudPiece QuadFromTopPx(Transform parent, Texture2D tex, string name,
                                             float xPx, float yPxFromTop, float hPx, float z, Color color)
        {
            if (parent == null || tex == null) return null;
            float wPx = hPx * ((float)tex.width / Mathf.Max(1, tex.height));
            return MakeAnchored(parent, tex, name,
                                xPx / ReferenceResolution.x, 1f - yPxFromTop / ReferenceResolution.y,
                                new Vector2(0.5f, 0.5f), hPx, wPx, z, color);
        }

        /// <summary>🔴 **2026-10-16：按名字找一块子画布**（自检 / 诊断用）。找不到 ⇒ `null`。
        /// ⚠️ 只找**根 canvas 的直接子件**（我们建的子画布都是那样挂的）。</summary>
        public static Canvas SubCanvas(string name)
        {
            if (RootRect == null || string.IsNullOrEmpty(name)) return null;
            for (int i = 0; i < RootRect.childCount; i++)
            {
                var t = RootRect.GetChild(i);
                if (t != null && t.name == name) return t.GetComponent<Canvas>();
            }
            // 🔴 **2026-10-24（`B-26`）补：也找【嵌套】的那几块。**
            //   原版 `BattleDoors/Canvas` 是 `BattleDoors` 的**子件**（不是根 canvas 的直子）——
            //   我们照原版把 `BattleDoors Canvas` 挂进 `BattleDoors` 之后，只查直子就会「找不到」，
            //   而 `EnsureSubCanvas` 的幂等**正是靠这个函数** ⇒ 会**静默建出第二块同名画布**。
            //   ⚠️ 仍然**先查直子**（原调用点的语义一个字不变），只有直子全不中才往下找。
            foreach (var cv in RootRect.GetComponentsInChildren<Canvas>(true))
                if (cv != null && cv.name == name) return cv;
            return null;
        }

        /// <summary>🔴 **2026-10-16：`MakeImage` 的 `HudPiece` 版** —— 给「字段类型已经是 `HudPiece`、
        /// 但这一件要建成 uGUI」的调用点用（先例 = `BattleDriver._loadingCover`）。
        /// 🔑 **为什么要有它**：调用点就不必 `using UnityEngine.UI` 去拿 `Image` 类型
        /// （`BattleDriver.cs` 没引那个命名空间，为一件图加一条 `using` 不划算）。</summary>
        /// <param name="raycast">🆕 2026-10-24（`W1` · 判据 `N1` §2.2）：这一件**吃不吃 uGUI 射线**。
        /// 原版 `WaitText/Dark Shade` 的 `m_RaycastTarget = 1` ⇒ 压暗层要传 `true`；
        /// 其余调用点不传（= 装饰件不吃射线，与加这个形参之前逐位相同）。</param>
        public static HudPiece MakeImagePiece(Transform parent, Texture2D tex, string name,
                                              float x, float y, float w, float h, Color color,
                                              bool raycast = false)
        {
            var rt = MakeImage(parent, tex, name, x, y, w, h, color, raycast);
            return rt != null ? HudPiece.Of(rt, rt.GetComponent<Image>()) : null;
        }

        /// <summary>从 `RectTransform` 反读 **枢轴（`pivot`）那一点**的设计 px（左上原点、y 向下）—— 自检用。
        /// 判据：锚点是归一化的 ⇒ 乘回 1920 / 1080（**这是与 `MakeImage` 成对的逆**，别各写一份）。
        /// <para>🔴 **2026-10-16 就地订正（铁律 5）：本行原来写「**中心的设计 px**」—— 那句只在
        /// 【本件 `pivot == (0.5,0.5)` 时】成立**。实现是 `ax*1920 + ap.x` / `(1−ay)*1080 − ap.y`，
        /// 而 `anchoredPosition` 摆的是**枢轴** ⇒ **读出来的是【枢轴那一点】**，不是矩形中心（⛔ 别按名字推）。</para>
        /// <para>⚠️ **对非居中 `pivot` 的节点会读成枢轴坐标** —— 先例（`A14` 现核）：`AlliancePanelWindow`
        /// 的根（原版 `RT_3052`：`aMin == aMax == (0,1)` · `pivot(0.13, 0.825)` ·
        /// `ap(-1.0444, −25.2717)` · `sd(815.044, 475.470)`）读成 **`(-1.04, +25.27)`**，
        /// 而它**真正的矩形中心是 `(300.52, 179.80)`** ⇒ 偏 `(+301.6, +154.5)` px。
        /// 出处：`资料/普查产出_1016并行/T09_AlliancePanel_锚点补正_报告.md`（`A14`）＋
        /// `资料/任务线/战斗线/任务/T09_整层换uGUI.md` §八·补 **陷阱 2**。</para>
        /// <para>🔑 **要矩形中心**：在这个返回值上再补 `(0.5 − pivot) × sizeDelta` 那一项
        /// （x 加、y 减 —— 本函数是 y 向下）即可；**先例 = `AlliancePanelWindow.RootCenterLocal`**
        /// （那一项 = `(+301.566, −154.528)`）。⚠️ 也**别拿它去量非居中 pivot 的节点**。</para></summary>
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
        //
        // 🔴 **2026-10-16 改：z 账从【一份全局静态表】改成【按容器分组】** ——
        //    `T09` 面板族要**每扇面板一块子 Canvas**（口径 → `T09` §五·补⑨ 一），
        //    一份全局表会让**第二块画布**的兄弟序按**第一块**的子件数去算 ⇒ **静默错位**。
        //    🔑 **键 = 子件的【父节点】**（不是 canvas）：兄弟序本来就只在同一个父节点内部有意义，
        //    这样嵌多深都对；而**今天**所有件都是 HUD canvas 的直接子件 ⇒ 与改之前**逐位相同**。

        /// <summary>一个**容器**（= 某个父节点）内部「z → 兄弟序」的账。</summary>
        public sealed class Group
        {
            /// <summary>这一组管的容器（所有登记过的件都是它的**直接子件**）。</summary>
            public readonly Transform Root;
            readonly List<float> _zs = new List<float>();
            readonly List<RectTransform> _kids = new List<RectTransform>();

            public Group(Transform root) { Root = root; }

            /// <summary>把一个子件按 **z 降序**插进兄弟序里（z 大的排前面 = 画在底下）。</summary>
            public void Add(RectTransform rt, float z)
            {
                if (rt == null) return;
                Prune();
                int i = 0;
                while (i < _zs.Count && _zs[i] >= z) i++;
                rt.SetSiblingIndex(i);
                _zs.Insert(i, z);
                _kids.Insert(i, rt);
            }

            /// <summary>把一个**已经建好的件**挪到另一个 z 档（= 旧那套的 `localPosition.z += dz`）。</summary>
            public void Reorder(RectTransform rt, float newZ)
            {
                if (rt == null) return;
                Prune();
                int i = _kids.IndexOf(rt);
                if (i >= 0) { _kids.RemoveAt(i); _zs.RemoveAt(i); }
                Add(rt, newZ);
            }

            /// <summary>🔴 **2026-10-17 修（`HudCanvas.Group` 僵尸账）**：把**已销毁**或**已换父**的登记踢掉。
            /// <para>为什么必须做：本账是「按 z 算下标」用的，**下标里混着死件** ⇒ 后续 `SetSiblingIndex`
            /// 会越插越靠后（**静默**、只在「反复重建同一件」时现形）。判据/先例 = `ErrorMessageBanner`
            /// 那段注释（2026-10-17 `W18` 现核）：`Tooltip` 换文案时**每次重建**底图，
            /// 第 2 次起底图就会漂到文字**之后**、把字盖死，而且**不会自愈**。</para>
            /// <para>⚠️ 「已换父」也算失效 —— 账里的 `Root` 是登记那一刻的父节点，换父后它不再归这一组管。</para></summary>
            void Prune()
            {
                for (int i = _kids.Count - 1; i >= 0; i--)
                {
                    var k = _kids[i];
                    if (k == null || k.parent != Root) { _kids.RemoveAt(i); _zs.RemoveAt(i); }
                }
            }

            /// <summary>这个件在本容器里占的 **z 档**。认不出 ⇒ `NaN`（⛔ 不是 0）。</summary>
            public float OrderZOf(RectTransform rt)
            {
                if (rt == null) return float.NaN;
                int i = _kids.IndexOf(rt);
                return i >= 0 ? _zs[i] : float.NaN;
            }
        }

        static readonly Dictionary<Transform, Group> _groups = new Dictionary<Transform, Group>();

        /// <summary>某个件所属容器的 z 账（容器 = 它的**父节点**；没有父就返回 `null`）。</summary>
        public static Group GroupOf(RectTransform rt)
        {
            var p = rt != null ? rt.parent : null;
            if (p == null) return null;
            Group g;
            if (!_groups.TryGetValue(p, out g)) { g = new Group(p); _groups[p] = g; }
            return g;
        }

        /// <summary>把一个子件按 **z 降序**插进它容器的兄弟序里（z 大的排前面 = 画在底下）。</summary>
        public static void Add(RectTransform rt, float z)
        {
            var g = GroupOf(rt);
            if (g != null) g.Add(rt, z);
        }

        /// <summary>把一个**已经建好的件**挪到另一个 z 档（见 <see cref="Group.Reorder"/>）。</summary>
        public static void Reorder(RectTransform rt, float newZ)
        {
            var g = GroupOf(rt);
            if (g != null) g.Reorder(rt, newZ);
        }

        /// <summary>这个件在当前容器里占的 **z 档**（认不出 ⇒ `NaN`）。</summary>
        public static float OrderZOf(RectTransform rt)
        {
            var g = GroupOf(rt);
            return g != null ? g.OrderZOf(rt) : float.NaN;
        }

        // ── 排序层（**判据 = 原版 `Globalgamemanagers/TagManager/TagManager_3.json`**）─────────
        //  ⚠️ 我们的 `ProjectSettings/TagManager.asset` 已按同名同 ID 建出这 9 层（2026-10-16）。
        //  🔴 **`Blocking` 那一个在 `int` 里是负数**：序列化里读出来是 `2286135553`（无符号），
        //     而 `Canvas.sortingLayerID` 是 **`int`** ⇒ 必须写 `-2008831743`
        //     （= `2286135553 − 2^32`）。⛔ 写正数会被截成另一个层（**静默跑到别的层上去**）。
        //  层从下到上：`Default < Background < Elements < OverElements < Hand < HandMoving < OverHand < PopUps < Blocking`。
        public static class SortingLayer
        {
            public const int Default = 0;
            public const int Background = 1954195551;
            public const int Elements = 229362899;
            public const int OverElements = 952166135;
            public const int Hand = 2099470215;
            public const int HandMoving = 887547619;
            public const int OverHand = 2007638115;
            public const int PopUps = 1054366423;
            public const int Blocking = -2008831743;   // = 无符号 2286135553（见上）
        }

        /// <summary>🔴 **2026-10-16（`T09` 面板族）：给一扇面板建它自己的【子 Canvas】**。
        /// <para>照原版：面板**不是**独立的根 canvas，而是**嵌在根 canvas 下的子 canvas**
        /// + **`overrideSorting = true`** + 自己的 `(排序层, order)` —— 这样它**继承根 canvas 的缩放**
        /// （原版**全场景只有一个 `CanvasScaler`**，在根上），又**脱离父层另立排序组**。</para>
        /// <para>⛔ **子 Canvas 一个 `CanvasScaler` 都不许加**（加了会二次缩放）。</para>
        /// <para>返回它的 `RectTransform`（**铺满父 canvas**、中心为原点、单位 = 设计 px）——
        /// 面板内部件的坐标就照它算。⚠️ 见 `T09` §五·补⑨ 一：**层与深度要一致地设**。</para>
        /// <para>拿不到根 canvas（`Root == null`）时**出声**并返回 `null`（调用点应跳过这一件）。</para>
        /// <para>🔴 **2026-10-16 就地订正（铁律 5）：本口原来是【不幂等】的** —— 每次调用都会 `new` 一块，
        /// 于是「建/取」这种用法会**建出两块同名子画布**（批 2 三个写手各自撞了一次，
        /// 各自在外层写了一个幂等包装：`PairCanvas` / `ReuseOrMakeCanvas` / …）。
        /// ⚠️ **2026-10-16（修波 `FD`）**：上面点名的那几个包装**本波已全删**（`PairCanvas` /
        /// `BattleLogPanel.ReuseOrMakeCanvas` / `CardChoicePanel.Build` / `SettingsPanel.Create` 四处，
        /// 直接调本口）⇒ 如今 `PairCanvas` / `ReuseOrMakeCanvas` **只剩方法名**、体内就是一句本口
        /// （⛔ 别照它们再包一层；也别再把它们当「现役包装」的例子读）。
        /// ⇒ **现在改成【按名字幂等】**：根 canvas 下已有同名子画布 ⇒ 直接返回它，并把层/order 对齐到本次传的值。
        /// ⛔ 调用点**不要再自己包一层**（那是同一个规矩的第二份写法）。</para></summary>
        public static RectTransform EnsureSubCanvas(Transform parent, string name,
                                                    int sortingLayerId, int sortingOrder)
        {
            if (RootRect == null)
            {
                Debug.LogError("[HudCanvas] 子 Canvas 要挂在根 canvas 下，但根 canvas 还没建（先调 Ensure(cam)）");
                return null;
            }
            // 🔴 **按名字幂等**（见 doc）：根 canvas 下已有同名子画布 ⇒ 复用它、对齐层/order 后返回
            var existing = SubCanvas(name);
            if (existing != null)
            {
                if (existing.overrideSorting != true) existing.overrideSorting = true;
                if (existing.sortingLayerID != sortingLayerId) existing.sortingLayerID = sortingLayerId;
                if (existing.sortingOrder != sortingOrder) existing.sortingOrder = sortingOrder;
                ApplySubCanvasRank();
                return (RectTransform)existing.transform;
            }
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent != null ? parent : RootRect, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;               // 铺满父 canvas
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;               // 四边贴合父 rect ⇒ 坐标原点 = 父的中心

            var cv = go.AddComponent<Canvas>();        // ⛔ 别再加 CanvasScaler（原版全场景只有一个）
            cv.overrideSorting = true;
            cv.sortingLayerID = sortingLayerId;
            cv.sortingOrder = sortingOrder;
            go.AddComponent<GraphicRaycaster>();       // 原版这 4 件宿主都带 GR（判据 → T09b §A·2）
            ApplySubCanvasRank();
            return rt;
        }

        /// <summary>🔴 **2026-10-16：同层同 `sortingOrder` 的子画布，先后【只由兄弟序定】**（`T09b` §五·4）
        /// ⇒ 这几块必须按这个先后排（从**先画 / 在下**到**后画 / 在上**）。
        /// <para>**① `PopUps` 层**：判据 = **原版根 `Canvas` 的子件序**里 `FrontCanvas`(**3**) 在 `BattleDoors`(**4**) **之前**
        /// ⇒ **门 / 结算压在设置窗之上**（`T09b` §A·3 那个 `m_Children` 表）。</para>
        /// <para>**② `OverHand` 层（全是 order 0）**：判据 = **原版 `Safe area FrontCanvas` 的 14 个子件序**
        /// （`T09b` §A·3；原文抄在 `T09` §八 开头）⇒ 这里按**升序**列 = 先画在下。
        /// 已迁的九格对号（**下面那张表里逐行写着序号**）：`Drag Attack Selector`(0) · `ActiveSkillDesc`(1) ·
        /// `Unit Chat`(2) ＋ 它的子件 `ChatPopup` · `Mulligan`(3) · `Generic Multi Card Display Combat`(4) ·
        /// `ChooseCardMenu`(5) · `Card Display Window`(6) · `AboveShader`(8) · `Alliance Panel`(13)。</para>
        /// <para>🔴 **2026-10-16 就地订正（铁律 5）**：本行原来写「⚠️ `SkillPanel`(1) / `Unit Chat`(2) **还没迁**，
        /// 迁了要**插到前面**」—— **已过期**：那两扇连同 `Drag Attack Selector`(0) 与 `Alliance Panel`(13)
        /// 本会话都已迁完，且**都已按序落进下面那张表**。⛔ 别再照那句去插一次。
        /// ⚠️ **这四格不是「还没迁」**（🔴 2026-10-17 就地订正，铁律 5）：`Tutorial`(11) 与 `LoadingMenu`(12)
        /// **早已迁完**（`T09` §八 有记；前者在 `HandMoving` 的三块子画布、后者在 `Blocking` 层、本就不进这张表）；
        /// `DebugButtons`(7) 是原版 `m_IsActive=false` 的调试件、**我们没建**；`PopupAnchor`(9) 是 `T09` 已裁定**不建**。
        /// ⇒ 这一格**没有「将来要迁」的东西**；若日后真要建，仍须插在 `"Alliance Panel Canvas"` 【前面】
        /// （原版子件序都 < 13 —— 这一条同样写在下面那张表的尾部注释里）。</para>
        /// <para>⚠️ 三块 `PopUps`/order 0 的画布正是靠这一格分先后的 —— 不排的话**建得早的就在下面**，
        /// 而建序**不是**判据（今天它甚至是偶然的）。</para>
        /// <para>⛔ **跨层的先后不靠这个表**（层不同 ⇒ 层说了算）⇒ 两张表并成一条链是安全的，
        /// 只要**同一组内部**的相对次序对。</para>
        /// <para>🔴 **2026-10-16（修波 `FD` · 账 `W74`）补一句 —— 跨层先后由 `(排序层, order)` 决定；
        /// 本表的前后只代表【同层之内】的先后。**
        /// ⚠️ 数组**前 4 项是 `PopUps` 层**（`order −1/0/0/1`），把整段排在数组最前**只是为了拿到最小的兄弟序**，
        /// ⛔ **别读成「它们画得最下面」**；同理下面数组尾注那句「**表尾 = 画得最上面**」**只在同层之内成立**
        /// （`Alliance Panel Canvas` 是 `OverHand` 这**一组**里的最后一名，整组还在 `PopUps` **之下**）。</para></summary>
        static readonly string[] SubCanvasRank =
        {
            // ── PopUps（order −1 → 0 → 1；同为 0 的两块按原版子件序）──
            "CemeteryLogPanel Canvas",          // PopUps / −1
            "BattleSettingsPanel Canvas",       // PopUps /  0（原版 FrontCanvas(3) 在前）
            "BattleDoors Canvas",               // PopUps /  0（原版 BattleDoors(4) 在后）
            "UI Error Message Controller Canvas",// PopUps / 1
            // ── OverHand（全 order 0，按原版 14 子件序升序）──
            //  ⚠️ **逐行的序号 = 原版 `Safe area FrontCanvas` 的 `m_Children` 下标**，完整 14 格是：
            //     `[0] Drag Attack Selector · [1] ActiveSkillDesc · [2] Unit Chat · [3] Mulligan ·
            //      [4] Generic Multi Card Display Combat · [5] ChooseCardMenu · [6] Card Display Window ·
            //      [7] DebugButtons · [8] AboveShader · [9] PopupAnchor · [10] BattleSettingsPanel ·
            //      [11] Tutorial · [12] LoadingMenu · [13] Alliance Panel`
            "Drag Attack Selector Canvas",       // 0/14（裁定「另起一块、⛔ 不并进 Mulligan」→ `T09` §八·3）
            "ActiveSkillDesc Canvas",            // 1/14
            "Unit Chat Canvas",                  // 2/14
            "ChatPopup Canvas",                  // 4/7（**在 `Unit Chat` 内部** ⇒ 按「子件画在父件之上」紧跟其后）
            "Mulligan Canvas",                   // 3/14
            "Generic Multi Card Display Combat Canvas",  // 4/14
            "ChooseCardMenu Canvas",             // 5/14
            "Card Display Window Canvas",        // 6/14
            "AboveShader Canvas",                // 8/14
            "Alliance Panel Canvas",             // 13/14
            // ⚠️ **这一节没有「将来要迁」的格了**（🔴 2026-10-17 就地订正，铁律 5；与上面 `:540` 那条同源）：
            //    `[11] Tutorial` / `[12] LoadingMenu` **早已迁完**（前者在 `HandMoving` 的子画布、
            //    后者在 `Blocking` 层，本就不进这张表）· `[7] DebugButtons` 原版 `m_IsActive=false`、**没建** ·
            //    `[9] PopupAnchor` 已裁定**不建**。若日后真要建，仍须插在上一行 `"Alliance Panel Canvas"`
            //    【前面】（它们的原版子件序都 < 13）；⛔ 别顺手加在表尾 —— 表尾 = 画得**最上面**的一格。
            //  ⚠️ 重名/漏项的自查：本表每一项都必须是**某扇面板 `SubCanvasName` 的字面量**（现核清单 →
            //     `grep -rn "SubCanvasName = " Battle/ Shell/`）；对不上的项 `RootRect.Find` 永远 null、
            //     **静默不排**（这正是本表最容易被写坏的方式）。
        };

        /// <summary>🔴 **2026-10-16（修波 `FD` · 账 `G1-5`）：只读判据口 —— 这个子画布名字在不在
        /// `SubCanvasRank` 表里。**
        /// <para>**用途 = 给「登记后读回自检」用**：`BattleDriver.EnsureRankedSubCanvas` 建/取完子画布之后
        /// 要确认「这条名字确实在排次表里」，而那张表是 `private static readonly`、那边**读不到** ⇒
        /// 在此之前**两处字符串（调用点传的串 vs 表里的串）的一致性全靠人工核**。名字对不上的后果是
        /// `RootRect.Find` 永远 `null`、**静默不排次序**（不报错、不警告）——
        /// 正是上面 doc 末段说的「本表最容易被写坏的方式」。</para>
        /// <para>⛔ 本口**只读、不改表**（表的内容与顺序以 `G3` 补全、`R1` 审过的那一份为准）。
        /// ⚠️ 表里**没有**的子画布**不等于「建错了」** —— `LoadingMenu` / `TooltipManager` 都在 `Blocking` 层、
        /// **本就不该进这张表**（见上面 doc）。</para></summary>
        public static bool IsRanked(string name)
        {
            return !string.IsNullOrEmpty(name) && System.Array.IndexOf(SubCanvasRank, name) >= 0;
        }

        /// <summary>把 `SubCanvasRank` 里**已经建出来**的那几块按表排到末尾（⛔ 只动它们、只定它们彼此的先后）。</summary>
        static void ApplySubCanvasRank()
        {
            if (RootRect == null) return;
            int idx = RootRect.childCount;
            for (int i = SubCanvasRank.Length - 1; i >= 0; i--)
            {
                var t = RootRect.Find(SubCanvasRank[i]);
                if (t == null)
                {
                    // 🔴 **2026-10-24（`B-26`）**：这块画布可能是**嵌套**的（先例 = `BattleDoors Canvas`
                    //   ⊂ `BattleDoors`，原版本来就是这棵树）⇒ 排它的**根直子祖先**（整棵子树一起挪）。
                    //   ⛔ 别直接 `cv.SetSiblingIndex` —— 那只在它自己父件里换位置，等于没排。
                    var cv = SubCanvas(SubCanvasRank[i]);
                    if (cv != null)
                    {
                        var up = cv.transform;
                        while (up.parent != null && up.parent != RootRect) up = up.parent;
                        if (up.parent == RootRect) t = up;
                    }
                }
                if (t == null) continue;
                if (--idx < 0) break;
                t.SetSiblingIndex(idx);
            }
        }

        /// <summary>🔴 **2026-10-16：建一块 uGUI 【字】** —— 面板族迁过来时走它。
        /// <para>`px/py` = **设计 px**、**相对 `parent` 的中心**（**y 向上**，与 `RectTransform` 同口径）；
        /// `z` = 那一族里的层深度（本函数登记它的兄弟序，见 <see cref="Add"/>）。</para>
        /// <para>⚠️ **别把世界坐标直接喂进来** —— 世界 → 设计 px 要 × `LayoutSpace.DesignPxH/DesignHeight`(108)。
        /// ⛔ 两档差 108 倍、**喂错不报错**（口径 → `T09` §五·补⑨ 四）。</para>
        /// <para>⛔ 不要用 `Label.Create`（那是**世界空间**档）—— 见 `Label` 文件头那条「只许二选一」。</para></summary>
        public static Label MakeText(Transform parent, string text, float px, float py, int scale,
                                     Color color, Vector2 anchor, float z, string name = null,
                                     TmpFont.Face face = TmpFont.Face.Cjk)
        {
            if (parent == null) return null;
            var l = Label.CreateUgui(parent, text, new Vector3(px, py, 0f), scale, color, anchor, name);
            if (l == null) return null;                 // 拿不到字体资产：`CreateUgui` 已出声
            // 🔴 **2026-10-17（A1024③ 第 0 批）：可选字体面** —— 默认 `Cjk` ⇒ 旧调用点零行为变化。
            //    ⚠️ 本方法**不定字号**（调方随后 `SetGlyphHeight` / `SetScriptHeight`）⇒ 只要排在它们之前即可；
            //    这里紧跟 `CreateUgui` 是**最早**的位置（`SetText` 内部按 `ActualFace` 重算字号）。
            if (face != TmpFont.Face.Cjk) l.SetFace(face);
            Add((RectTransform)l.transform, z);
            return l;
        }

        /// <summary>造一件**贴着某个归一化锚点**的 uGUI 图 ——
        /// `ImageQuad.Create(parent, tex, pos, worldHeight, anchor, name)` 的对应物。
        /// `x01/y01` = 锚点（**y 从下算**，与 `LayoutSpace` 同口径）；`pivot` = 原版那个 `anchor`（轴心）；
        /// `hPx/wPx` = **设计 px**；`z` = 旧那套里的层深度（本件按它定兄弟序，见 <see cref="Add"/>）。
        /// ⚠️ 这是**唯一**给 `BattleDriver.HudImageTex` 用的建件口（⛔ 别在别处再写一份）。
        /// <para>🔴 **2026-10-19（`T09` 步骤 A1）：补「节点 rect」口（可选，默认值 = 旧行为）。**
        /// `rectW/rectH > 0` ⇒ `sizeDelta` 写**原版节点 rect**（不是「画出来的尺寸」），
        /// 并按 `preserveAspect` 设 `Image.preserveAspect`（原版 `m_PreserveAspect = 1` 的件用它内接）。
        /// **为什么要有它**：`T09` §1·6 现核 —— `RectTransform` 现在存的是 `HudAbs` 内接后的
        /// **画出来尺寸**（例：`Offensive` 高 105.600 vs 原版节点 106.943，差 1.34 px），
        /// 而原版 `m_RaycastPadding` 是加在**节点 rect** 上的 ⇒ 不修 rect 就照抄 padding，命中框会缩水。
        /// ⚠️ A1 这一批**不传**它（那颗钮继续按 drawn rect，断言也按 drawn rect 写）；
        /// A2 才把各件的节点 rect 传进来（判据 → `T09` 迁移计划 §1·6 / §2 A2）。</para></summary>
        public static HudPiece MakeAnchored(Transform parent, Texture2D tex, string name,
                                            float x01, float y01, Vector2 pivot,
                                            float hPx, float wPx, float z, Color color,
                                            float rectW = -1f, float rectH = -1f, bool preserveAspect = false)
        {
            if (parent == null || tex == null) return null;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(x01, y01);
            rt.pivot = pivot;
            rt.anchoredPosition = Vector2.zero;
            // 🔴 2026-10-19（`T09` A1）：传了节点 rect 就写它（见 doc）；不传 ⇒ **旧的 drawn 尺寸**（逐位不变）。
            bool hasNodeRect = rectW > 0f && rectH > 0f;
            rt.sizeDelta = hasNodeRect ? new Vector2(rectW, rectH) : new Vector2(wPx, hPx);

            var img = go.AddComponent<Image>();
            img.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                       new Vector2(0.5f, 0.5f), 100f);
            img.color = color;
            // 原版 `m_PreserveAspect = 0` ⇒ 拉满；节点 rect 口按调用点传的来（A2 才会用到）。
            img.preserveAspect = hasNodeRect && preserveAspect;
            img.raycastTarget = false;       // 这一族是装饰；可点件由 `BattleHudButton.Attach` 置 true
            Add(rt, z);
            return HudPiece.Of(rt, img);
        }

        // ── 🔴 2026-10-17（T05 第 2 步）：原版 `Anim Anchors` 那一族动画锚点 ─────────────────────
        //   原版 `BattleHud` 下有一族**空锚点节点**（`Canvas/Anim Anchors` 下的 4 颗），
        //   技能（`AbilityLogic.GetPos`）在 `6 boardCenterUI / 11 nextToClock / 140 createCardPos /
        //   145 stratagemGeneralShowCase` 四档上锚到它们。判据 → `T05d_GetPos锚点缺口判据.md` §一·2 / §二。

        /// <summary>原版那一族锚点的父节点名（**唯一处** —— 建 / 找都读它）。</summary>
        public const string AnimAnchorsName = "Anim Anchors";

        /// <summary>🔴 **2026-10-17（T05 第 2 步）：建原版 `Anim Anchors` 那一族动画锚点**
        /// （`GetPos` 的档 `6/140/145/11` 要锚到这里）。
        /// <para>判据 = 原版 `bundle_scenes_scenes_battlearena2` 的 `Canvas/Anim Anchors` —— 一颗**铺满父 Canvas**
        /// 的空节点（`aMin(0,0) / aMax(1,1)`、`sd=0`、`pivot=(0.5,0.5)`），下挂 4 颗子锚点
        /// （`pivot=(0.5,0.5)`、`sd=100×100`）：</para>
        /// <list type="bullet">
        ///   <item>`Anchor: UI Center` `ap=(0, 0)` ← 档 `6 boardCenterUI`</item>
        ///   <item>`Anchor: CreateCard` `ap=(885.56, 84.27)` ← 档 `140 createCardPos`</item>
        ///   <item>`Anchor: Stratagem ShowCase` `ap=(529, 83)` ← 档 `145 stratagemGeneralShowCase`</item>
        ///   <item>`Anchor: Next To Clock` `ap=(766, 83)` ← 档 `11 nextToClock`</item>
        /// </list>
        /// <para>出处：`MonoBehaviour_5150.json` 的 `uiAnimAnchors`（7 条）＋逐节点 `RectTransform_*.json`
        /// （T05d §一·2 / §二）。⚠️ 那 4 个 `ap` 是**相对 Canvas 中心**的设计 px
        /// （`Anim Anchors` 铺满 ⇒ 原点 = Canvas 中心），与 <see cref="MakeImage"/> 那套「绝对 px」不是一回事。</para>
        /// <para>⚠️ **幂等**（按名字找/建）；⚠️ `RootRect` 还没建（`Ensure(cam)` 之前）⇒ **出声** + 返回 null
        /// （调用点应跳过）。⚠️ 这些是**空锚点节点**（无 `Image`、不吃射线、不占渲染）。</para></summary>
        public static RectTransform EnsureAnimAnchors()
        {
            if (RootRect == null)
            {
                Debug.LogError("[HudCanvas] `Anim Anchors` 要挂在根 canvas 下，但根 canvas 还没建（先调 Ensure(cam)）");
                return null;
            }
            var existing = RootRect.Find(AnimAnchorsName) as RectTransform;
            if (existing != null) return existing;

            var go = new GameObject(AnimAnchorsName, typeof(RectTransform));
            go.transform.SetParent(RootRect, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;               // 铺满父 canvas（原版 `aMin(0,0)/aMax(1,1)`）
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;        // 四边贴合 ⇒ 原点 = Canvas 中心
            rt.sizeDelta = Vector2.zero;

            MakeAnchorChild(rt, "Anchor: UI Center",         0f,      0f);
            MakeAnchorChild(rt, "Anchor: CreateCard",         885.56f, 84.27f);
            MakeAnchorChild(rt, "Anchor: Stratagem ShowCase", 529f,    83f);
            MakeAnchorChild(rt, "Anchor: Next To Clock",      766f,    83f);
            return rt;
        }

        /// <summary>一颗**空锚点**：中心锚 + 中心枢轴、`100×100`、`ap = (x, y)`（设计 px、**y 向上**）。
        /// 名字逐字照原版（`T05d` §二）。</summary>
        static void MakeAnchorChild(Transform parent, string name, float x, float y)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(100f, 100f);     // 原版那 4 颗都是 100×100
        }

        /// <summary>旧 quad 那一套里「HUD 图统一用的那个 z」（= `BattleDriver.HudImageZ`）。
        /// 🔑 **只用来定兄弟序**（本层里没有 z 这个概念了）—— 值必须与 `BattleDriver.HudImageZ` **一致**，
        /// 否则「默认那档」的件会插错位置。</summary>
        public const float DefaultZ = 0.3f;
    }
}
